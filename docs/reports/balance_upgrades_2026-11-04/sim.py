"""2026-11-04 copy of ../balance_rerun_2026-11-03/sim.py: the proposed upgrades of ../UPGRADE_GAPS_2026-11-04.md
added (search "2026-11-04" for every change; their prices and the buying order are in policies.py).

Rogue Waves balance simulator: one player, one vessel, a stock map's lanes, a stock Tide table.

A lane is a 1D walk of `L` tiles from an enemy core to a player core; `x` is the distance left to the core. Lanes into
the same core share their last `merge` tiles (the kill zone). Buildings stand at the kill zone of a core (a "cluster")
and cover every lane into that core from the core out to `merge + sqrt(R^2 - off^2)`. Melee units guard a spot `s` and
reach foes within `e` of it. A seal (walls across the kill zone) holds walkers at `xs` until one of its walls falls.

Engine facts used (decompiled game, docs/16, docs/24): a walker steps a tile per 1/Speed s and fights only when blocked
or at the core; Tempered foes turn on a Mob or Tower within 5; Ballista and Onager fire at Mob/Tower/Resource in range
while walking (Ballista slows itself 50% for 1/Attackspeed s after each shot); the Maul slams in any state; Aegis heals
2/s after 5 s unhit; a Tide ends when its spawns are out and no foe is left, or its time runs out; Coins pay
Add x Tide at each Tide start; every foe drops its Red to the wallet when it falls.

Re-run 2026-11-03 (BALANCE_RERUN_2026-11-03.md), copied from ../balance_2026-11-03/ and fed the balance round's
measures (BALANCE_ROUND_2026-11-03.md). `tune['model']` picks the ore: 'round' (the default here) or 'study' (the first
study's numbers, so its results reproduce). In the round model each gatherer stands beside one node and a Damage-1
strike pays that node's measured aura:
- Purlest's Opal (M2, and Entity_Ai_Resource_Opal: a payout pays every colour the node has, plus the Tide's own):
  Tide 1 Yellow, 2 Red, 3 Blue, 4 all, repeating. A one-colour Tide pays 17 payouts in 37 strikes of 10 of each colour
  and 15 more of its colour; the all-colours Tide 6 payouts in 14 strikes of 20 of each. `opal='colour'` keeps only the
  Tide's own colour (the round report's reading, a low bound).
- Woozy (M3, M8): Red 1 a strike, Blue 5, Yellow 5 (the Purple tile refused a Sapling, so it is left out).
- The Hunt (M3, M4): Orange 1.8 Red and 3.0 Yellow a strike; `beasts` adds hunted aura beasts (M11) a Tide.
The Mining Hut runs on the code's 6.8 s round trip (M9). Walls are still modelled both ways: `seal` False is the round's
M5 (foes walk round a three-tile line), the default True a gap a player has shut.
"""
import math, random

DT = 0.1

ENEMY = {
    'Dagger':    dict(hp=10,    spd=1.0,  dmg=3,   rate=0.5,  rng=1.5, drop=1),
    'Spear':     dict(hp=10,    spd=2.5,  dmg=5,   rate=1.4,  rng=1.5, drop=1),
    'Aegis':     dict(hp=80,    spd=0.7,  dmg=15,  rate=0.2,  rng=1.5, drop=3, heal=2.0),
    'Ballista':  dict(hp=50,    spd=0.8,  dmg=5,   rate=0.25, rng=5,   drop=3, ranged=1),
    'Maul':      dict(hp=700,   spd=0.45, dmg=50,  rate=0.15, rng=2.0, drop=10, slam=1),
    'Onager':    dict(hp=90,    spd=0.5,  dmg=40,  rate=0.1,  rng=9,   drop=10, ranged=1),
    'Excalibur': dict(hp=10000, spd=0.4,  dmg=400, rate=0.1,  rng=13,  drop=10, ranged=1),
    'Claymore':  dict(hp=10,    spd=4.25, dmg=15,  rate=1.0,  rng=1.5, drop=1),
}
ELITE = {'Health': dict(hp=4), 'Red': dict(hp=2, dmg=2, aggro=5.0), 'Hyper': dict(hp=3, dmg=3, spd=2.25, rate=2),
         'Chaos': dict(hp=15, dmg=10, spd=10, rate=10), 'Armor': dict(hp=1, armor=4)}

# ---------------------------------------------------------------------------------------------------------- maps
# ecores: list of {goal core index: lane length}. ore: what a gatherer by the base can strike.
#   yield: total aura per strike; cycle: 'opal' (Opal's colour schedule) or a fixed colour split.
MAPS = {
    'purlest': dict(name='Purlest', cores=1, merge=[3.5], ecores=[{0: 70}, {0: 51}, {0: 39}],
                    ore=dict(nodes=1, yield_=9.5, cycle='opal', dist=11, laser=False, sapling=True),
                    seal=3),
    'woozy': dict(name='Woozy', cores=1, merge=[3.0], ecores=[{0: 93}, {0: 90}],
                  ore=dict(nodes=3, yield_=1.0, cycle='rby', dist=5, laser=True, sapling=True), seal=3),
    'thehunt': dict(name='The Hunt', cores=2, merge=[2.5, 2.5], ecores=[{1: 46, 0: 54}, {1: 44, 0: 61}],
                    ore=dict(nodes=1, yield_=1.0, cycle='ry', dist=4, laser=False, sapling=True), seal=3),
}

# The round's measures: every node a gatherer can stand by, nearest first. pay: aura (R, B, Y) a Damage-1 strike pays;
# pps: payouts a strike (a fully grown Sapling drops a fruit a payout).
OPAL_SINGLE = 17 / 37      # M2: 5, 6, 6 payouts in 11, 12, 14 strikes on the one-colour Tides
OPAL_ALL = 6 / 14          # M2: 6 payouts in 14 strikes on the all-colours Tide
NODES = {
    'purlest': [dict(name='Opal', dist=11.0, opal=True, pps=OPAL_SINGLE)],
    'woozy': [dict(name='Red', dist=3.6, pay=(1.0, 0.0, 0.0), pps=0.2), dict(name='Blue', dist=4.0, pay=(0.0, 5.0, 0.0), pps=0.2),
              dict(name='Yellow', dist=8.9, pay=(0.0, 0.0, 5.0), pps=0.2)],
    'thehunt': [dict(name='Orange', dist=5.8, pay=(1.8, 0.0, 3.0), pps=0.6)],
}

def opal_strike(tide, reading='full'):
    """(R, B, Y) a Damage-1 strike on the Opal pays in Tide `tide` (0 = the opening break), and payouts a strike."""
    k = tide % 4
    if k == 0:   # all colours
        each = (20.0 if reading == 'full' else 10.0) * OPAL_ALL
        return (each, each, each), OPAL_ALL
    base = 10.0 * OPAL_SINGLE if reading == 'full' else 0.0
    pay = [base, base, base]
    pay[{1: 2, 2: 0, 3: 1}[k]] += 15.0 * OPAL_SINGLE
    return tuple(pay), OPAL_SINGLE

def node_value(node, reading='full'):
    """A node's aura a strike, averaged over a 4-Tide cycle (to rank nodes)."""
    if node.get('opal'):
        return sum(sum(opal_strike(t, reading)[0]) for t in range(4)) / 4
    return sum(node['pay'])

def opal_weights(tide):
    """Colour split of an Opal strike during Tide `tide` (from the 2026-10-26 Purlest wallet: Red, Blue, All, Yellow)."""
    k = tide % 4
    if k == 1: return (0.6, 0.2, 0.2)
    if k == 2: return (0.2, 0.6, 0.2)
    if k == 3: return (1 / 3, 1 / 3, 1 / 3)
    return (0.2, 0.2, 0.6)

def ore_weights(m, tide):
    c = m['ore']['cycle']
    if c == 'opal': return opal_weights(tide)
    if c == 'rby': return (1 / 3, 1 / 3, 1 / 3)
    if c == 'ry': return (0.5, 0.0, 0.5)
    return (1 / 3, 1 / 3, 1 / 3)

# ---------------------------------------------------------------------------------------------------------- entities
class Foe:
    __slots__ = ('kind', 'hp', 'mhp', 'spd', 'dmg', 'rate', 'rng', 'drop', 'elite', 'core', 'lane', 'x', 'state', 'cd',
                 'stacks', 'stack_t', 'tick', 'hit_t', 'slow_t', 'stun_t', 'target', 'aggro', 'ranged', 'slam', 'heal',
                 'large', 'vine_t', 'spore_src', 'armor', 'id', 'burst_t', 'hallu_t', 'deathcap')
    def __init__(self, kind, elite, core, lane, L, fid):
        d = ENEMY[kind]; e = ELITE.get(elite, {}) if elite else {}
        self.kind = kind; self.elite = elite; self.core = core; self.lane = lane; self.x = float(L); self.id = fid
        self.mhp = self.hp = d['hp'] * e.get('hp', 1)
        self.spd = d['spd'] * e.get('spd', 1); self.dmg = d['dmg'] * e.get('dmg', 1); self.rate = d['rate'] * e.get('rate', 1)
        self.rng = d['rng']; self.drop = d['drop']; self.ranged = d.get('ranged', 0); self.slam = d.get('slam', 0)
        self.heal = d.get('heal', 0.0); self.aggro = e.get('aggro', 0.0); self.armor = e.get('armor', 0)
        self.large = d['hp'] >= 40
        self.state = 'walk'; self.cd = 0.0; self.stacks = 0; self.stack_t = -99.0; self.tick = 0.0; self.hit_t = 0.0
        self.slow_t = 0.0; self.stun_t = 0.0; self.target = None; self.vine_t = 0.0; self.spore_src = None
        self.burst_t = 0.0; self.hallu_t = 0.0; self.deathcap = False

class Bld:
    """A building or a unit. kind decides the behaviour; p holds its stats."""
    __slots__ = ('kind', 'cl', 'hp', 'mhp', 'cd', 'p', 'up', 'growth', 'stood', 'alive', 'dead_t', 'seal', 'carry',
                 'held', 'away_t', 'tin', 'seeds', 'dmg_done', 'kills', 'spot', 'reach', 'spin_acc', 'name', 'unit',
                 'barrier', 'mangled', 'pending', 'hits', 'node', 'pays')
    def __init__(self, kind, cl, hp, p, unit=False):
        self.kind = kind; self.cl = cl; self.hp = self.mhp = hp; self.cd = 0.0; self.p = dict(p); self.up = set()
        self.growth = 0; self.stood = True; self.alive = True; self.dead_t = 0.0; self.seal = False; self.carry = 0
        self.held = 0; self.away_t = 0.0; self.tin = None; self.seeds = 0.0; self.dmg_done = 0.0; self.kills = 0
        self.spot = p.get('spot', 3.0); self.reach = p.get('reach', 0.0); self.spin_acc = 0.0; self.name = kind
        self.unit = unit; self.barrier = 0.0; self.mangled = 0; self.pending = []; self.hits = 0
        self.node = None; self.pays = 0.0

# ---------------------------------------------------------------------------------------------------------- the run
class Run:
    def __init__(self, vessel, mapname, table, policy, tune=None, coins=False, seed=0, tides=15, log=False):
        from tables import TABLES
        self.rng = random.Random(seed)
        self.vessel = vessel; self.m = MAPS[mapname]; self.mapname = mapname
        self.tides, self.breaks = TABLES[table]; self.ntides = min(tides, len(self.tides))
        self.policy = policy; self.tune = dict(tune or {}); self.coins = coins; self.log = log
        self.model = self.tune.get('model', 'round')
        if self.model == 'round':
            self.tune.setdefault('hut_code', True)
        self.nodes = NODES[mapname]
        self.wallet = [50.0, 50.0, 50.0]
        if coins: self.wallet = [56.0, 81.0, 81.0]
        self.cores = [100.0] * self.m['cores']
        self.foes = []; self.blds = []; self.t = 0.0; self.tide = 0; self.fid = 0
        self.spent = [0.0, 0.0, 0.0]; self.income = {'kills': 0.0, 'coins': [0.0] * 3, 'mine': [0.0] * 3, 'fruit': [0.0] * 3, 'beasts': [0.0] * 3}
        self.lost = 0; self.core_dmg = [0.0] * 40; self.leaks = [0] * 40; self.kills = 0
        self.thread = float(self.tune.get('thread_start', 10)) if vessel == 'spool' else 0.0; self.thread_cap = 50 + self.tune.get('stash_cap', 0); self.ground = []  # (x, cl, amount, expire)
        self.th = {'dropped': 0, 'picked': 0, 'delivered': 0, 'spun': 0, 'spent': 0, 'lost': 0}
        self.ecore_rr = [0] * len(self.m['ecores'])
        self.events = []; self.walls_up = 0; self.tide_wallet = []; self.vessel_dead_t = 0.0
        self.mine_acc = 0.0; self.stats_tide = []
        self.vb = None  # the vessel as a unit
        self.tide_len = []
        self.conv_t = 0.0; self.conv = [0.0, 0.0, 0.0]   # 2026-11-04: Wood Wide Web / Dyed in the Wool, aura gained by trading

    # ------------------------------------------------------------------ helpers
    def say(self, *a):
        if self.log: print('%7.1f T%-2d' % (self.t, self.tide), *a)

    def node(self, rank=0, rich=False):
        """The rank-th node a gatherer would stand by: nearest first (the bots), or richest first (smart players; on a
        tie the one paying Yellow, the gatherers' own colour, then the nearer); `bot_nodes` and `smart_nodes` name the
        order instead."""
        if not rich and self.tune.get('bot_nodes'):
            by = {n['name']: n for n in self.nodes}
            ns = [by[x] for x in self.tune['bot_nodes']]
        elif rich and self.tune.get('smart_nodes'):
            by = {n['name']: n for n in self.nodes}
            ns = [by[x] for x in self.tune['smart_nodes']]
        elif rich:
            ns = sorted(self.nodes, key=lambda n: (-node_value(n, self.tune.get('opal', 'full')), 0 if n.get('opal') or n['pay'][2] > 0 else 1, n['dist']))
        else:
            ns = sorted(self.nodes, key=lambda n: n['dist'])
        return ns[rank % len(ns)]

    def ore_value(self):
        """The richest node's aura a strike, or the first study's yield."""
        if self.model == 'study': return self.m['ore']['yield_']
        return max(node_value(n, self.tune.get('opal', 'full')) for n in self.nodes)

    def afford(self, cost):
        return all(self.wallet[i] >= cost[i] - 1e-9 for i in range(3))

    def pay(self, cost):
        for i in range(3):
            self.wallet[i] -= cost[i]; self.spent[i] += cost[i]

    def cover(self, cl, R, off=1.5):
        m = self.m['merge'][cl] + self.tune.get('front', 4.0)
        return m + math.sqrt(max(0.0, R * R - off * off))

    def foes_in(self, cl, lo, hi):
        return [f for f in self.foes if f.core == cl and f.hp > 0 and lo <= f.x <= hi and f.state != 'gone']

    def near(self, a, b, r):
        """Two foes within r of each other: same lane, or both in the merged stretch."""
        if a.core != b.core: return False
        m = self.m['merge'][a.core]
        if a.lane != b.lane and (a.x > m or b.x > m): return False
        return abs(a.x - b.x) <= r

    # ------------------------------------------------------------------ damage
    def hurt(self, f, dmg, src=None, kind='hit'):
        if f.hp <= 0 or dmg <= 0: return
        if f.armor and kind != 'spore':
            dmg = max(0.0, dmg - f.armor)
        mult = 1.0
        if f.vine_t >= 5.0 and self.has_up('henge', 'Easy'): mult *= 1.15
        dmg *= mult
        f.hp -= dmg; f.hit_t = self.t
        if src is not None: src.dmg_done += dmg
        if f.hp <= 0: self.fell(f, src)

    def fell(self, f, src):
        f.state = 'gone'; self.kills += 1
        self.wallet[0] += f.drop; self.income['kills'] += f.drop
        if src is not None: src.kills += 1
        # Spool: Thread on the ground where it fell
        if self.vessel == 'spool':
            amt = 3 if f.large else 1
            if f.elite: amt += 2
            amt = min(5, amt) + self.tune.get('thread_drop_add', 0)
            if any(h.alive and h.cl == f.core and 'Count' in h.up for h in self.blds if h.kind == 'head'): amt += 1   # 2026-11-04
            self.ground.append([f.x, f.core, amt, self.t + 15.0, f.lane]); self.th['dropped'] += amt
        # Yossele: Viral Outbreak, Cordyceps (friends) and Mulch
        if self.vessel == 'yossele' and f.stacks >= self.max_stacks() and f.spore_src is not None:
            c = f.spore_src
            if 'Viral' in c.up and self.rng.random() < 0.30:
                for g in self.foes:
                    if g is not f and g.hp > 0 and self.near(f, g, 2.0):
                        self.hurt(g, 15.0, c); self.add_stacks(g, 1, c)

    def max_stacks(self):
        return 10

    def add_stacks(self, f, n, src):
        cap = 10
        if src is not None and 'Deathcap' in src.up: cap = 8 if f.stacks <= 8 else f.stacks
        f.stacks = min(cap, f.stacks + n) if f.stacks < cap else f.stacks
        f.stack_t = self.t; f.spore_src = src
        if src is not None and 'Deathcap' in src.up: f.deathcap = True

    def head_lvl(self, cl):
        """2026-11-04: the best buffer Head alive at this core: 0 none, 1 Needle Point, 2 + Stitch in Time, 3 Head of the Class."""
        best = 0
        for h in self.blds:
            if h.kind == 'head' and h.alive and h.cl == cl:
                best = max(best, 3 if 'Class' in h.up else 2 if 'Stitchtime' in h.up else 1 if 'Needle' in h.up else 0)
        return best

    def head_dmg(self, cl):
        return {0: 1.0, 1: 1.25, 2: 1.25, 3: 1.5}[self.head_lvl(cl)]

    def head_dr(self, cl):
        return {0: 0.0, 1: 0.0, 2: 0.3, 3: 0.45}[self.head_lvl(cl)]

    def convert(self):
        """2026-11-04: Wood Wide Web, once a second for the player however many Saplings carry it: 2 of the colour it
        holds most become 1 of the colour it holds least, while the most stays 100 above the least (tune `web_every`
        spaces the trades)."""
        if self.t < self.conv_t: return
        if not any(b.alive and b.kind == 'sapling' and 'Web' in b.up for b in self.blds): return
        self.conv_t = self.t + self.tune.get('web_every', 1.0)
        w = self.wallet; lo = min(range(3), key=lambda i: w[i]); hi = max(range(3), key=lambda i: w[i])
        if w[hi] >= w[lo] + 100:
            w[hi] -= 2.0; w[lo] += 1.0; self.conv[lo] += 1.0

    def has_up(self, kind, up):
        for b in self.blds:
            if b.alive and b.kind == kind and up in b.up: return True
        return False

    # ------------------------------------------------------------------ buildings
    def add(self, kind, cl=0, **kw):
        b = make(kind, cl, self, **kw)
        self.blds.append(b); return b

    def alive(self, kind=None):
        return [b for b in self.blds if b.alive and (kind is None or b.kind == kind)]

    # ------------------------------------------------------------------ spawning
    def spawn_script(self, tide):
        """Yields (time offset, kind, elite) in the order the mode's Executecontent runs."""
        out, t = [], 0.0
        for s in tide['steps']:
            if s[0] == 'wait':
                t += s[1]; continue
            _, kind, n, every, el = s
            for i in range(n):
                out.append((t, kind, el)); t += every
        return out

    def spawn(self, kind, el, k):
        ecs = self.m['ecores']; ec = k % len(ecs)
        goals = sorted(ecs[ec].keys())
        g = goals[self.ecore_rr[ec] % len(goals)]; self.ecore_rr[ec] += 1
        L = ecs[ec][g] * self.tune.get('lane_scale', 1.0)
        if kind not in ENEMY: return
        self.fid += 1
        f = Foe(kind, el, g, ec, L, self.fid)
        f.spd *= 1.0 + self.tune.get('speed_jitter', 0.1) * (2 * self.rng.random() - 1)
        f.x += self.rng.random() * 2.0
        self.foes.append(f)

    # ------------------------------------------------------------------ the main loop
    def play(self):
        import policies
        self.pol = policies.make(self)
        self.pol.setup()
        # opening break
        self.simulate(self.breaks[0] if self.breaks else 45.0, spawning=None)
        for n in range(1, self.ntides + 1):
            if self.lost: break
            self.tide = n
            self.start_tide(n)
            tide = self.tides[n - 1]
            script = self.spawn_script(tide)
            t0 = self.t
            self.simulate(tide['time'], spawning=script)
            self.tide_len.append(round(self.t - t0))
            if self.lost: break
            self.end_tide(n)
            brk = self.breaks[n] if n < len(self.breaks) else 30.0
            brk = self.tune.get('break', brk)
            self.simulate(brk, spawning=None)
        return self.result()

    def start_tide(self, n):
        if self.coins:
            add = [1 * n, 7 * n, 7 * n]
            for i in range(3):
                self.wallet[i] += add[i]; self.income['coins'][i] += add[i]
        for b in self.blds:
            if not b.alive: continue
            if b.stood and b.kind in GROWTH:
                b.growth = min(GROWTH[b.kind], b.growth + 1); regrow(self, b)
            b.stood = True
            if b.kind == 'tree':
                b.seeds = seed_limit(b)
            if b.kind == 'henge' and 'Blockade' in b.up:
                b.barrier = 0.3 * b.mhp
        self.tide_wallet.append((n, [round(w) for w in self.wallet]))
        self.say('start', [round(w) for w in self.wallet], 'cores', [round(c) for c in self.cores])
        self.pol.on_tide_start(n)

    def end_tide(self, n):
        b = self.tune.get('beasts', 0) if self.mapname == 'thehunt' else 0
        if b:   # M11: aura beasts hunted in the break, paid to every colour
            for i in range(3):
                self.wallet[i] += b; self.income['beasts'][i] += b
        self.stats_tide.append(dict(tide=n, wallet=[round(w) for w in self.wallet], cores=list(self.cores),
                                    thread=self.thread, blds=len(self.alive())))
        self.pol.on_tide_end(n)

    def simulate(self, duration, spawning):
        t0 = self.t; k = 0; si = 0
        script = spawning or []
        nextpol = self.t + 1.0
        while True:
            el = self.t - t0
            if spawning is not None:
                while si < len(script) and script[si][0] <= el:
                    _, kind, elite = script[si]; self.spawn(kind, elite, si); si += 1
                done_spawning = si >= len(script)
                live = any(f.hp > 0 and f.state != 'gone' for f in self.foes)
                if done_spawning and not live: break
                if el >= duration and done_spawning: break
            else:
                if el >= duration: break
            live = any(f.hp > 0 and f.state != 'gone' for f in self.foes)
            quiet = not live and (spawning is None or si >= len(script) or script[si][0] > el + 0.5)
            self.step(0.5 if quiet else DT, quiet)
            if self.lost: return
            if self.t >= nextpol:
                nextpol = self.t + 1.0; self.pol.tick()

    def step(self, dt=DT, quiet=False):
        t = self.t
        m = self.m
        # ----- foes move / act
        seals = {}
        for b in self.blds:
            if b.alive and b.seal: seals.setdefault(b.cl, []).append(b)
        nseal = self.tune.get('seal_n', self.m['seal'])
        for f in self.foes:
            if f.hp <= 0 or f.state == 'gone': continue
            # spores
            if f.stacks > 0:
                if t - f.stack_t > 6.0:
                    f.stacks = 0
                else:
                    f.tick += dt
                    if f.tick >= 0.5:
                        f.tick -= 0.5
                        per = 0.3 * (1.25 if f.deathcap else 1.0)
                        self.hurt(f, per * 0.5 * f.stacks, f.spore_src, 'spore')
                        if f.hp <= 0: continue
            if f.heal and t - f.hit_t > 5.0 and f.hp < f.mhp:
                f.hp = min(f.mhp, f.hp + f.heal * dt)
            if t < f.stun_t: continue
            f.cd -= dt
            sl = 1.0
            if f.stacks >= 10 and f.spore_src is not None and 'Sticky' in f.spore_src.up: sl *= 0.75
            if t < f.slow_t: sl *= 0.5
            vine = vines_at(self, f)
            if vine > 0: sl *= (1 - vine); f.vine_t += dt
            else: f.vine_t = 0.0
            mrg = m['merge'][f.core]
            # ranged foes fire at buildings in reach while walking or held
            if f.ranged and f.cd <= 0:
                tgt = self.ranged_target(f)
                if tgt is not None:
                    f.cd = 1.0 / f.rate
                    if f.kind == 'Ballista': f.slow_t = t + 1.0 / f.rate
                    if not self.intercept(f):
                        self.damage_bld(tgt, f.dmg, f, melee=False)
            if f.slam and f.cd <= 0 and f.x <= mrg + 2.5:
                self.slam(f)
            # Tempered: turn on a building or unit within 5
            sealed = len(seals.get(f.core, ())) >= nseal
            if f.aggro and f.state == 'walk' and f.x <= mrg + f.aggro:
                tgt = self.pick_melee_target(f, aggro=True, sealed=sealed)
                if tgt is not None:
                    f.state = 'aggro'; f.target = tgt
            if f.state == 'aggro':
                if f.target is None or not f.target.alive:
                    tgt = self.pick_melee_target(f, aggro=True, sealed=sealed)
                    if tgt is None: f.state = 'walk'
                    else: f.target = tgt
                if f.state == 'aggro':
                    # walk up to the target (a sealed line stops it at the line), then hit it
                    tx = f.target.spot if f.target.spot is not None else mrg
                    if sealed: tx = max(tx, mrg)
                    gap = f.x - tx
                    if gap > f.rng:
                        f.x -= min(gap - f.rng, f.spd * sl * dt)
                    elif f.cd <= 0 and not f.ranged and not f.slam:
                        f.cd = 1.0 / f.rate; self.damage_bld(f.target, f.dmg, f, melee=True)
                    if f.x <= 0:
                        f.x = 0.0; f.state = 'core'; self.leaks[self.tide] += 1; f.target = None
                    continue
            if f.state == 'walk':
                xs = None
                sl_list = seals.get(f.core)
                if sl_list and len(sl_list) >= nseal:
                    xs = m['merge'][f.core]
                f.x -= f.spd * sl * dt
                if xs is not None and f.x <= xs:
                    f.x = xs; f.state = 'held'; f.target = None
                elif f.x <= 0:
                    f.x = 0.0; f.state = 'core'; self.leaks[self.tide] += 1; f.target = None
            elif f.state == 'held':
                sl_list = seals.get(f.core)
                if not sl_list or len(sl_list) < nseal:
                    f.state = 'walk'; continue
                if f.cd <= 0 and not f.ranged:
                    if f.target is None or not f.target.alive:
                        f.target = self.pick_melee_target(f, held=sl_list)
                    if f.target is not None and not f.slam:
                        f.cd = 1.0 / f.rate; self.damage_bld(f.target, f.dmg, f, melee=True)
            elif f.state == 'core':
                if f.cd <= 0 and not f.ranged and not f.slam:
                    f.cd = 1.0 / f.rate
                    tgt = None
                    if f.target is not None and f.target.alive: tgt = f.target
                    else:
                        # the core is the nearest thing to a foe at the core; a building beside it (the Yarn Stash)
                        # takes about one hit in five (2026-10-26: Stash 21-78, core 78-100)
                        cand = [b for b in self.blds if b.alive and b.cl == f.core and b.p.get('atcore')]
                        if cand and self.rng.random() < 0.2:
                            tgt = self.rng.choice(cand); f.target = tgt
                    if tgt is not None: self.damage_bld(tgt, f.dmg, f, melee=True)
                    else:
                        self.cores[f.core] -= f.dmg; self.core_dmg[self.tide] += f.dmg
                        if self.cores[f.core] <= 0: self.lost = self.tide; return
                elif f.ranged and f.cd <= 0:
                    f.cd = 1.0 / f.rate
                    self.cores[f.core] -= f.dmg; self.core_dmg[self.tide] += f.dmg
                    if self.cores[f.core] <= 0: self.lost = self.tide; return
        # ----- buildings and units act
        for b in self.blds:
            if not b.alive:
                if b.unit and b.p.get('respawn') and t >= b.dead_t:
                    b.alive = True; b.hp = b.mhp; b.carry = 0
                continue
            if quiet and b.kind in QUIET_SKIP:
                if b.cd > 0: b.cd -= dt
                if b.pending: act_cannon(self, b, 0.0)
                continue
            ACT.get(b.kind, act_none)(self, b, dt)
        if self.tune.get('web'): self.convert()   # 2026-11-04
        # ----- thread on the ground expires
        if self.ground:
            keep = []
            for g in self.ground:
                if g[3] < t: self.th['lost'] += g[2]
                else: keep.append(g)
            self.ground = keep
        # ----- tidy
        if len(self.foes) > 64:
            self.foes = [f for f in self.foes if f.state != 'gone']
        self.t += dt

    # ------------------------------------------------------------------ foes vs buildings
    def ranged_target(self, f):
        mrg = self.m['merge'][f.core]
        if f.x > mrg + f.rng: return None
        cand = [b for b in self.blds if b.alive and b.cl == f.core and not b.p.get('noshoot')]
        if not cand: return None
        return self.rng.choice(cand)

    def intercept(self, f):
        for b in self.blds:
            if b.alive and b.kind == 'tree' and 'Dmds' in b.up and b.cl == f.core:
                cost = 5 if f.kind in ('Excalibur',) else 1
                if b.seeds >= cost:
                    b.seeds -= cost; return True
        return False

    def pick_melee_target(self, f, aggro=False, held=None, sealed=False):
        mrg = self.m['merge'][f.core]
        units = [b for b in self.blds if b.alive and b.unit and b.cl == f.core and b.reach > 0 and b.p.get('rng', 1.5) <= 2.0 and abs(f.x - b.spot) <= b.reach + 0.5]
        if held:
            walls = [w for w in held if w.alive]
            # nearest of: the walls in front, the units on it
            if units and self.rng.random() < len(units) / (len(units) + 1.5):
                return self.rng.choice(units)
            return self.rng.choice(walls) if walls else None
        if aggro:
            pool = units + [b for b in self.blds if b.alive and not b.unit and b.cl == f.core and not b.p.get('noshoot') and b.kind != 'stash' and not (b.kind == 'tin' and self.tune.get('tin_back', True))]
            if sealed:   # behind a shut line only the line and what stands at it can be reached
                pool = [b for b in pool if b.seal or (b.unit and b.spot >= mrg - 0.5)]
            return self.rng.choice(pool) if pool else None
        return None

    def damage_bld(self, b, dmg, f, melee):
        if not b.alive: return
        if b.kind == 'henge':
            dmg *= max(0.2, 1 - 0.1 * b.growth)
            if 'Monument' in b.up: dmg *= 0.8
            if b.barrier > 0:
                a = min(b.barrier, dmg); b.barrier -= a; dmg -= a
        if b.kind == 'wall' and melee and f is not None and f.hp > 0 and b.p.get('spikes'):
            self.hurt(f, b.p['spikes'], b)
        if self.vessel == 'spool' and b.kind in ('stitched', 'dart', 'mangled'):   # 2026-11-04: Stitch in Time / Head of the Class
            dmg *= 1 - self.head_dr(b.cl)
        if self.vessel == 'spool' and b.kind == 'cushion' and 'Thimble' in b.up:   # 2026-11-04: Iron Thimble
            dmg *= 0.75
        if self.vessel == 'spool' and b.kind == 'cushion':
            if melee and f is not None and f.hp > 0:
                r = 0.4 if 'Sharper' in b.up else 0.2
                self.hurt(f, dmg * r, b)
            elif (not melee) and f is not None and f.hp > 0 and 'Barrage' in b.up:
                self.hurt(f, dmg * 0.2, b)
            b.spin_acc += dmg * (2 if 'Unravel' in b.up else 1)
            while b.spin_acc >= 25:
                b.spin_acc -= 25
                self.ground.append([b.spot, b.cl, 1, self.t + 15.0, -1]); self.th['dropped'] += 1; self.th['spun'] += 1
            if 'Stuffing' in b.up:
                b.p['stuff_acc'] = b.p.get('stuff_acc', 0) + dmg
                while b.p['stuff_acc'] >= 40:
                    b.p['stuff_acc'] -= 40
                    if sum(1 for u in self.blds if u.alive and u.kind == 'mangled' and u.tin is b) < 3:
                        u = self.add('mangled', b.cl, spot=b.spot, over='Overstuffed' in b.up, burst='Burst' in b.up)
                        u.tin = b
        b.hp -= dmg
        if self.vessel == 'spool' and b.kind == 'cushion' and 'Voodoo' in b.up and b.hp > 0 and b.hp < 0.5 * b.mhp and b.p.get('voodoo_tide') != self.tide:
            # 2026-11-04: Voodoo Doll, once a Tide: pins every foe within 3 tiles for 3 s and mends half its health
            b.p['voodoo_tide'] = self.tide; b.hp = min(b.mhp, b.hp + 0.5 * b.mhp)
            for g in self.foes:
                if g.hp > 0 and g.core == b.cl and g.state != 'gone' and abs(g.x - b.spot) <= 3.0: g.stun_t = self.t + 3.0
        if b.hp <= 0:
            b.alive = False; b.dead_t = self.t + b.p.get('respawn', 0)
            self.say('lost', b.kind, 'to', f.kind if f else '?')
            if b.kind in ('stitched', 'dart') and b.tin is not None and 'Recycle' in b.tin.up:
                amt = math.ceil(b.p['cost'] / 2)
                self.ground.append([b.spot, b.cl, amt, self.t + 15.0, -1]); self.th['dropped'] += amt
            if b.kind in ('stitched', 'dart') and b.tin is not None and 'Surprise' in b.tin.up:
                for g in self.foes:
                    if g.hp > 0 and g.core == b.cl and abs(g.x - b.spot) <= b.p['rng'] + 0.5:
                        self.hurt(g, 3 * b.p['dmg'], b)
            if b.kind == 'head' and b.carry:
                self.ground.append([b.spot, b.cl, b.carry, self.t + 15.0, -1]); b.carry = 0
            if b.kind == 'vessel':
                self.vessel_dead_t = self.t
            if b.kind == 'stash':
                # its Thread spills beside the core and the build menu offers a free Stash again
                if self.thread > 0:
                    self.ground.append([0.0, b.cl, self.thread, self.t + 15.0, -1]); self.thread = 0.0
                nb = self.add('stash', b.cl); self.th['stash_lost'] = self.th.get('stash_lost', 0) + 1

    def slam(self, f):
        f.cd = 1.0 / f.rate
        hit = 0
        for b in self.blds:
            if hit >= 50: break
            if b.alive and b.cl == f.core and not (b.kind == 'dart' and self.tune.get('dart_safe')) and (b.unit and b.reach > 0 and abs(b.spot - f.x) <= 2.5 + b.reach * 0.5 or b.seal and abs(b.spot - f.x) <= 2.5):
                self.damage_bld(b, f.dmg, f, melee=True); hit += 1
        if f.state == 'core' and hit == 0:
            self.cores[f.core] -= f.dmg; self.core_dmg[self.tide] += f.dmg
            if self.cores[f.core] <= 0: self.lost = self.tide

    # ------------------------------------------------------------------ result
    def result(self):
        won = not self.lost
        return dict(vessel=self.vessel, map=self.mapname, policy=self.policy, coins=self.coins,
                    reached=(self.lost or self.ntides + 1) - (0 if won else 0), lost=self.lost, won=won,
                    core=list(self.cores), core_dmg=self.core_dmg[:self.ntides + 1], leaks=self.leaks[:self.ntides + 1],
                    wallet=[round(w) for w in self.wallet], spent=[round(s) for s in self.spent],
                    income=self.income, kills=self.kills, tide_len=self.tide_len, hits={k: sum(b.hits for b in self.blds if b.kind == k) for k in ('sapling','loom','vessel','hut')}, nodes=sorted(set(b.node['name'] for b in self.blds if b.node and b.kind in ('sapling', 'loom', 'hut'))), tide_wallet=self.tide_wallet, thread=dict(self.th),
                    blds=sorted(set(b.kind for b in self.blds)), counts={k: sum(1 for b in self.blds if b.alive and b.kind == k) for k in set(b.kind for b in self.blds)},
                    dmg={k: round(sum(b.dmg_done for b in self.blds if b.kind == k)) for k in set(b.kind for b in self.blds)})

# ---------------------------------------------------------------------------------------------------------- kinds
GROWTH = {'cannon': 5, 'tree': 15, 'henge': 8, 'sapling': 5, 'bonsai': 15}

def seed_limit(b):
    lim = b.p['seedlimit'] + 3 * b.growth
    if 'Storm' in b.up: lim += 20
    return lim

def regrow(run, b):
    if b.kind == 'cannon':
        b.p['aspd_mult'] = 1 + 0.1 * b.growth
        b.p['radius_add'] = 0.1 * b.growth
    elif b.kind == 'tree':
        b.p['range_mult'] = 1 + 0.2 * b.growth
    elif b.kind == 'sapling':
        b.p['aspd_mult'] = 1 + 0.1 * b.growth

def gatherer_far(run, kw):
    """A gatherer more than 9 tiles out stands outside the defence (ranged foes don't reach it)."""
    if run.model == 'study': return run.m['ore']['dist'] > 9
    n = kw.get('node') or run.node(0)
    return n['dist'] > 9

def make(kind, cl, run, **kw):
    T = run.tune
    if kind == 'cannon':
        b = Bld(kind, cl, 20, dict(dmg=1.0, aspd=0.4, rng=9.0, radius=1.0, spores=2, aspd_mult=1.0, radius_add=0.0))
    elif kind == 'tree':
        b = Bld(kind, cl, 50, dict(dmg=2.0, rng=7.0, seedlimit=10, seedrate=0.5, range_mult=1.0))
        b.seeds = 10
    elif kind == 'henge':
        sealing = kw.get('seal', False)
        b = Bld(kind, cl, 80, dict(slow=0.15, vines=1.0, spot=kw.get('spot', run.m['merge'][cl] + (0.0 if sealing else 1.0))))
        b.spot = b.p['spot']; b.seal = sealing
    elif kind == 'sapling':
        b = Bld(kind, cl, 30, dict(aspd=T.get('sapling_aspd', 0.25), aspd_mult=1.0, noshoot=gatherer_far(run, kw)))
    elif kind == 'vessel_y':
        b = Bld('vessel', cl, 70, dict(dmg=5.5, aspd=0.5, rng=4.0, respawn=10.0, spot=run.m['merge'][cl]), unit=True)
        b.reach = 0.0
    # --- Spool
    elif kind == 'stash':
        b = Bld(kind, cl, 100, dict(atcore=True, noshoot=False))  # stands beside the core
    elif kind == 'tin':
        b = Bld(kind, cl, 60, dict(units=T.get('tin_units', 4), every=4.0, cost=10 + T.get('stitched_cost', 0)))
        b.p['next'] = 0.0
    elif kind == 'cushion':
        b = Bld(kind, cl, T.get('cushion_hp', 100), dict(spot=kw.get('spot', run.m['merge'][cl])))
        b.spot = b.p['spot']; b.seal = kw.get('seal', True)
    elif kind == 'loom':
        b = Bld(kind, cl, 40, dict(aspd=T.get('loom_aspd', 0.4), spin=6.0, noshoot=gatherer_far(run, kw)))
        b.p['spin_next'] = 6.0
    elif kind == 'head':
        b = Bld(kind, cl, 60, dict(dmg=2.0, aspd=0.6, rng=1.5, respawn=15.0, spot=kw.get('spot', run.m['merge'][cl])), unit=True)
        b.reach = 3.0
    elif kind == 'stitched':
        hp = 20 * (1.5 if 'Woolen' in kw['tin'].up else 1.0) + T.get('stitched_hp', 0)
        b = Bld(kind, cl, hp, dict(dmg=2.0 + T.get('stitched_dmg', 0) + T.get('crafted_lvl', 0) * len(kw['tin'].up), aspd=0.8, rng=1.5, cost=kw.get('cost', 10), spot=kw.get('spot')), unit=True)
        b.reach = 2.0 + 1.5; b.tin = kw['tin']
        if T.get('stitched_block'): b.seal = True
        if 'Tacking' in kw['tin'].up: b.seal = True; b.mhp *= 2; b.hp *= 2   # 2026-11-04: Tacking Stitch
        if 'Cross' in kw['tin'].up: b.p['dmg'] *= 2   # 2026-11-04: Cross Stitch
    elif kind == 'dart':
        hp = 12 * (1.5 if 'Woolen' in kw['tin'].up else 1.0)
        b = Bld(kind, cl, hp, dict(dmg=2.5 + T.get('dart_dmg', 0) + T.get('crafted_lvl', 0) * len(kw['tin'].up), aspd=0.6, rng=3.5, cost=15, spot=kw.get('spot')), unit=True)
        b.reach = 1.0 + 3.5; b.tin = kw['tin']
        if 'Cross' in kw['tin'].up: b.p['dmg'] *= 2   # 2026-11-04: Cross Stitch
    elif kind == 'mangled':
        b = Bld(kind, cl, 45 if kw.get('over') else 15, dict(dmg=1.5, aspd=0.8, rng=1.5, spot=kw.get('spot'), burst=kw.get('burst')), unit=True)
        b.reach = 2.0 + 1.5
    elif kind == 'vessel_s':
        b = Bld('vessel', cl, 45 + T.get('spool_hp', 0), dict(dmg=2.5 + T.get('spool_dmg', 0), aspd=0.6, rng=1.5, respawn=10.0, spot=kw.get('spot', run.m['merge'][cl])), unit=True)
        b.reach = 2.5
    # --- Smacky (baseline)
    elif kind == 'archer':
        b = Bld(kind, cl, 25, dict(dmg=4.0, aspd=0.7, rng=6.0))
    elif kind == 'bomber':
        b = Bld(kind, cl, 50, dict(dmg=3.0, aspd=0.35, rng=5.0, radius=1.5))
    elif kind == 'wall':
        b = Bld(kind, cl, 50, dict(spot=kw.get('spot', run.m['merge'][cl])))
        b.spot = b.p['spot']; b.seal = kw.get('seal', True)
    elif kind == 'hut':
        b = Bld(kind, cl, 25, dict(aspd=0.5, hit=0.5, noshoot=gatherer_far(run, kw)))
    elif kind == 'vessel_m':
        b = Bld('vessel', cl, 50, dict(dmg=4.0, aspd=0.5, rng=1.5, respawn=10.0, spot=kw.get('spot', run.m['merge'][cl])), unit=True)
        b.reach = 2.5
    else:
        raise KeyError(kind)
    if 'spot' in kw and kw['spot'] is not None and kind not in ('henge', 'cushion', 'wall'):
        b.spot = kw['spot']
    if kind in ('sapling', 'loom', 'hut', 'vessel_y'):
        b.node = kw.get('node') or run.node(0)
    return b

# ---------------------------------------------------------------------------------------------------------- behaviours
def act_none(run, b, dt):
    pass

def target_first(run, cl, lo, hi, pos):
    best, bd = None, 1e9
    for f in run.foes:
        if f.core != cl or f.hp <= 0 or f.state == 'gone': continue
        if lo <= f.x <= hi:
            d = abs(f.x - pos)
            if d < bd: best, bd = f, d
    return best

def act_cannon(run, b, dt):
    # bombs in flight
    if b.pending:
        keep = []
        for (land, f, x, lane, core, dmg, radius, stacks, bounce) in b.pending:
            if run.t >= land:
                blast(run, b, x, lane, core, dmg, radius, stacks)
                if bounce:
                    keep.append((run.t + 1.0, None, x, lane, core, dmg * 0.5, radius * 0.5, 1, False))
            else:
                keep.append((land, f, x, lane, core, dmg, radius, stacks, bounce))
        b.pending = keep
    b.cd -= dt
    if b.cd > 0: return
    rng = b.p['rng'] + (2.0 if 'Farout' in b.up else 0.0)
    mrg = run.m['merge'][b.cl]
    f = target_first(run, b.cl, 0.0, run.cover(b.cl, rng), mrg)
    if f is None: return
    b.cd = 1.0 / (b.p['aspd'] * b.p['aspd_mult'])
    dist = abs(f.x - mrg) + 1.5
    flight = 0.6 + 0.08 * dist
    held = f.state != 'walk'
    lead = 0.0 if held else f.spd * flight
    x = max(0.0 if f.state != 'held' else f.x, f.x - lead)
    radius = b.p['radius'] + b.p['radius_add']
    if 'Farout' in b.up: radius *= 1 + min(0.75, 0.5 * flight)
    b.pending.append((run.t + flight, f, x, f.lane, f.core, b.p['dmg'], radius, b.p['spores'], b.growth >= 5))

def blast(run, b, x, lane, core, dmg, radius, stacks):
    mrg = run.m['merge'][core]
    r = radius + 0.3
    for g in run.foes:
        if g.hp <= 0 or g.core != core or g.state == 'gone': continue
        if g.lane != lane and (g.x > mrg or x > mrg): continue
        if abs(g.x - x) <= r:
            run.hurt(g, dmg, b)
            if g.hp > 0:
                run.add_stacks(g, stacks, b)
                if 'Finish' in b.up and g.stacks >= 10 and run.t >= g.burst_t:
                    g.burst_t = run.t + 1.0; n = g.stacks; g.stacks = 0
                    for h in run.foes:
                        if h.hp > 0 and h.core == core and run.near(g, h, 1.25):
                            run.hurt(h, 1.0 * n, b)
                            if h.hp > 0 and h is not g: run.add_stacks(h, 2, b)
                if 'Hallucinogen' in b.up and g.stacks >= 10 and run.t >= g.hallu_t:
                    g.hallu_t = run.t + 5.0
                    if run.rng.random() < 0.2: g.stun_t = run.t + 2.0

def act_tree(run, b, dt):
    lim = seed_limit(b)
    rate = b.p['seedrate'] * (1 + 0.1 * b.p.get('weeds', 0))
    if 'Dmds' in b.up: rate *= 0.5
    if 'Storm' in b.up: rate *= 2
    if 'Demand' in b.up and b.seeds < 0.25 * lim: rate *= 2
    b.seeds = min(lim, b.seeds + rate * dt)
    if 'Dmds' in b.up: return
    b.cd -= dt
    if b.cd > 0 or b.seeds < 1: return
    rng = b.p['rng'] * b.p['range_mult'] * (2 if 'Storm' in b.up else 1)
    cov = run.cover(b.cl, rng)
    foes = [f for f in run.foes if f.core == b.cl and f.hp > 0 and f.state != 'gone' and f.x <= cov]
    if not foes: return
    b.cd = 0.5
    dmg = b.p['dmg'] + 0.25 * len([u for u in b.up]) + (3 if 'Packets' in b.up else 0)
    foes.sort(key=lambda f: (-f.hp if 'Picky' in b.up else f.x))
    for f in foes:
        if b.seeds < 1: break
        per = dmg + (0.0025 * f.mhp if 'Picky' in b.up else 0)
        need = max(1, math.ceil(f.hp / per))
        use = min(need, int(b.seeds))
        b.seeds -= use
        bounce = 3 if 'Rubbery' in b.up else 1
        run.hurt(f, per * use, b)
        if bounce > 1:
            others = [g for g in foes if g is not f and g.hp > 0 and run.near(f, g, 2.0)][:2]
            for g in others: run.hurt(g, per, b)
        if f.hp <= 0 and 'Packets' in b.up:
            b.seeds = min(lim, b.seeds + 2)

def vines_at(run, f):
    """Slow from Stonehenge vines under foe f (0 if none)."""
    best = 0.0
    for b in run.blds:
        if b.kind != 'henge' or not b.alive or b.cl != f.core: continue
        reach = 1.0 + (b.growth // 2)
        if 'Verdant' in b.up: reach = max(reach, 3.0 if b.growth >= 8 else 2.0) + 1.0
        if abs(f.x - b.spot) <= reach:
            best = max(best, b.p['slow'])
            if 'Carnivore' in b.up:
                pass
    return best

def act_henge(run, b, dt):
    # Carnivore: drains 5/s from up to 5 foes on its vines, mending the stones
    if 'Carnivore' in b.up:
        reach = 1.0 + (b.growth // 2)
        if 'Verdant' in b.up: reach = max(reach, 3.0 if b.growth >= 8 else 2.0) + 1.0
        n = 0
        for f in run.foes:
            if n >= 5: break
            if f.hp > 0 and f.core == b.cl and f.state != 'gone' and abs(f.x - b.spot) <= reach:
                run.hurt(f, 5 * dt, b); n += 1
                b.hp = min(b.mhp, b.hp + 5 * dt)
    if 'Remedy' in b.up:
        heal = 0.5 * max(1, b.growth) * dt
        for o in run.blds:
            if o.alive and not o.unit and o.cl == b.cl and o.hp < o.mhp and o.kind != 'sapling':
                o.hp = min(o.mhp, o.hp + heal)

def mine(run, b, hits):
    """`hits` strikes on the ore by a gatherer: pays by its node (round model) or the map's ore (study model)."""
    ore = run.m['ore']
    if hits <= 0: return
    if run.model == 'round':
        n = b.node or run.node(0)
        if n.get('opal'): pay, pps = opal_strike(run.tide, run.tune.get('opal', 'full'))
        else: pay, pps = n['pay'], n['pps']
        sc = run.tune.get('ore_scale', 1.0)
        if b.kind == 'loom' and 'Dyed' in b.up:
            # 2026-11-04: Dyed in the Wool, the Loom's own payouts come in the colour picked (the one its player holds
            # least of, Red or Blue: Yellow is never Spool's short colour), 70% of what the node paid
            tgt = 0 if run.wallet[0] < run.wallet[1] else 1
            tot = sum(pay) * hits * sc * run.tune.get('dye_eff', 0.7)
            run.wallet[tgt] += tot; run.income['mine'][tgt] += tot; run.conv[tgt] += tot
            b.hits += hits; b.pays += pps * hits
            return pps * hits
        for i in range(3):
            run.wallet[i] += pay[i] * hits * sc; run.income['mine'][i] += pay[i] * hits * sc
        b.hits += hits; b.pays += pps * hits
        return pps * hits
    y = ore['yield_'] * hits * run.tune.get('ore_scale', 1.0)
    w = ore_weights(run.m, max(1, run.tide))
    for i in range(3):
        run.wallet[i] += y * w[i]; run.income['mine'][i] += y * w[i]
    b.hits += hits
    return hits / 5.0

def act_sapling(run, b, dt):
    ore = run.m['ore']
    if run.model == 'study' and not ore['sapling']: return
    sp = b.p['aspd'] * (b.p['aspd_mult'] + (0.8 if 'Focused' in b.up else 0) + 0.1 * b.p.get('forest', 0) * ('Forest' in b.up))
    b.cd -= dt
    if b.cd <= 0:
        b.cd = 1.0 / sp
        nodes = 1
        pays = mine(run, b, nodes)
        if b.growth >= 5 and run.model == 'round':
            # fruit: a payout drops one, worth Attackspeed x Damage x 15 of each colour (scope 07; M10: about 6 each), if
            # picked up; `fruit_k` 5 is P6
            if run.tune.get('fruit_pickup', 0) > 0:
                each = sp * run.tune.get('fruit_k', 15) * (2 if 'Harvest' in b.up else 1) * pays * run.tune['fruit_pickup']
                for i in range(3):
                    run.wallet[i] += each; run.income['fruit'][i] += each
        elif b.growth >= 5:
            # fruit: Attackspeed x Damage x 15 of every colour a payout (about 6 each); needs a pickup
            if run.tune.get('fruit_pickup', 0) > 0:
                payouts_per_hit = 1.0 / 5.0
                tot = 3 * 0.25 * 15 * (2 if 'Harvest' in b.up else 1) * payouts_per_hit * run.tune['fruit_pickup']
                w = (0.7, 0.15, 0.15) if run.tune.get('fruit_red') else (1 / 3, 1 / 3, 1 / 3)
                for i in range(3):
                    run.wallet[i] += tot * w[i]; run.income['fruit'][i] += tot * w[i]

def act_vessel(run, b, dt):
    b.cd -= dt
    if b.cd > 0: return
    mrg = run.m['merge'][b.cl]
    if b.reach <= 0:  # Yossele: ranged from its spot
        if run.policy_laser_mining(b):
            b.cd = 1.0 / b.p['aspd']; mine(run, b, 1); return
        cov = mrg + math.sqrt(max(0.0, b.p['rng'] ** 2 - 1.0))
        f = target_first(run, b.cl, 0.0, cov, mrg)
        if f is None: return
        b.cd = 1.0 / b.p['aspd']; run.hurt(f, b.p['dmg'], b)
        return
    act_melee(run, b, dt, cd_done=True)

def act_melee(run, b, dt, cd_done=False):
    if not cd_done:
        b.cd -= dt
        if b.cd > 0: return
    lo, hi = b.spot - b.reach, b.spot + b.reach
    # held foes first (they stand still), then walkers
    best, bd = None, 1e9
    for f in run.foes:
        if f.core != b.cl or f.hp <= 0 or f.state == 'gone': continue
        if lo <= f.x <= hi:
            d = abs(f.x - b.spot) - (5 if f.state != 'walk' else 0)
            if d < bd: best, bd = f, d
    if best is None: return
    # chasing a walker costs time: a unit slower than the foe lands fewer hits
    b.cd = 1.0 / b.p['aspd']
    miss = run.tune.get('vessel_miss', 0.3) if b.kind == 'vessel' else run.tune.get('chase_miss', 0.9)
    if best.state == 'walk' and run.rng.random() < miss:
        return
    mult = run.head_dmg(b.cl) if run.vessel == 'spool' and b.kind in ('stitched', 'dart', 'mangled') else 1.0   # 2026-11-04
    run.hurt(best, b.p['dmg'] * mult, b)
    if b.kind == 'mangled' and b.p.get('burst'):
        for g in run.foes:
            if g.hp > 0 and g.core == b.cl and abs(g.x - best.x) <= 1.25: run.hurt(g, 5, b)
        b.alive = False

def act_unit(run, b, dt):
    act_melee(run, b, dt)

def act_head(run, b, dt):
    # away delivering
    if b.away_t > 0:
        b.away_t -= dt
        if b.away_t <= 0:
            put = min(b.carry, max(0, run.thread_cap - run.thread))
            run.thread += put; b.carry -= put; run.th['delivered'] += put
        return
    # pull in Thread within 3 tiles of its spot
    if run.ground:
        for g in run.ground:
            cap = 40 if 'Bobbin' in b.up else 20; reach = 5.0 if 'Bobbin' in b.up else 3.0   # 2026-11-04: Big Bobbin
            if g[1] == b.cl and g[2] > 0 and abs(g[0] - b.spot) <= reach and b.carry < cap:
                take = min(g[2], cap - b.carry); g[2] -= take; b.carry += take; run.th['picked'] += take
        run.ground = [g for g in run.ground if g[2] > 0]
    # going home: Threaded Delivery when full, or the player's RETURN (policy)
    if 'Class' in b.up and b.carry > 0:   # 2026-11-04: Head of the Class, what it picks up reaches the Stash at once
        put = min(b.carry, max(0, run.thread_cap - run.thread)); run.thread += put; b.carry -= put; run.th['delivered'] += put
    if b.carry >= (40 if 'Bobbin' in b.up else 20) and ('Delivery' in b.up or run.pol.returns_heads()):
        b.away_t = run.tune.get('head_trip', 6.0)
        return
    act_melee(run, b, dt)

def act_spool(run, b, dt):
    if b.p.get('off'): return
    # Spool walks over Thread near its spot (the player steers it), and drops it at the Stash nearby
    if run.ground:
        for g in run.ground:
            if g[1] == b.cl and g[2] > 0 and abs(g[0] - b.spot) <= run.tune.get('spool_pick', 2.0) and run.rng.random() < dt / 2.0:
                put = min(g[2], max(0, run.thread_cap - run.thread))
                run.thread += put; g[2] -= put; run.th['picked'] += put; run.th['delivered'] += put
        run.ground = [g for g in run.ground if g[2] > 0]
    act_vessel(run, b, dt)

def act_tin(run, b, dt):
    if b.p['next'] > run.t: return
    alive = [u for u in run.blds if u.alive and u.tin is b and u.kind in ('stitched', 'dart')]
    if len(alive) >= b.p['units']: return
    dart = 'Design' in b.up and run.pol.wants_dart(b)
    cost = 15 if dart else (8 if 'Discounted' in b.up else b.p['cost'])
    if run.thread < cost: return
    run.thread -= cost; run.th['spent'] += cost
    spot = b.p.get('target', run.m['merge'][b.cl])
    if dart: spot = max(0.0, spot - 2.0)   # Darts stand behind the wall and throw over it
    u = run.add('dart' if dart else 'stitched', b.cl, tin=b, spot=spot, cost=cost)
    b.p['next'] = run.t + b.p['every']

def act_loom(run, b, dt):
    b.cd -= dt
    if b.cd <= 0 and (run.model == 'round' or run.m['ore']['sapling']):
        b.cd = 1.0 / b.p['aspd']; mine(run, b, 1)
    b.p['spin_next'] -= dt
    if b.p['spin_next'] <= 0:
        b.p['spin_next'] = b.p['spin'] / run.tune.get('loom_speed', 1.0) / (2.0 if 'Winder' in b.up else 1.0)   # 2026-11-04: Bobbin Winder
        if 'Direct' in b.up and run.thread < run.thread_cap:
            run.thread += 1; run.th['spun'] += 1; run.th['delivered'] += 1
        elif b.held < (20 if 'Winder' in b.up else 10):
            b.held += 1; run.th['spun'] += 1
    if 'Seam' in b.up:
        # the Seam mines beside its Loom (melee strikes on the ore, Damage 1 at 0.8 a second)
        b.p['seam_cd'] = b.p.get('seam_cd', 0) - dt
        if b.p['seam_cd'] <= 0 and (run.model == 'round' or run.m['ore']['sapling']):
            b.p['seam_cd'] = 1 / run.tune.get('seam_aspd', 0.8); mine(run, b, run.tune.get('seam_hit', 1.0))

def act_archer(run, b, dt):
    b.cd -= dt
    if b.cd > 0: return
    mrg = run.m['merge'][b.cl]
    f = target_first(run, b.cl, 0.0, run.cover(b.cl, b.p['rng']), mrg)
    if f is None: return
    b.cd = 1.0 / b.p['aspd']; run.hurt(f, b.p['dmg'], b)

def act_bomber(run, b, dt):
    b.cd -= dt
    if b.cd > 0: return
    mrg = run.m['merge'][b.cl]
    f = target_first(run, b.cl, 0.0, run.cover(b.cl, b.p['rng']), mrg)
    if f is None: return
    b.cd = 1.0 / b.p['aspd']
    for g in run.foes:
        if g.hp > 0 and g.state != 'gone' and run.near(f, g, b.p['radius']): run.hurt(g, b.p['dmg'], b)

def act_hut(run, b, dt):
    b.cd -= dt
    if b.cd <= 0 and run.m['ore'].get('hut', True):
        # the miner walks between strikes; hut_code: the miner's whole round trip as the game codes it (aura.py), 6.8 s
        b.cd = 6.8 if run.tune.get('hut_code') else 1.0 / b.p['aspd'] + 2.0
        mine(run, b, b.p['hit'])

QUIET_SKIP = {'cannon', 'archer', 'bomber', 'stitched', 'dart', 'mangled', 'henge'}

def act_wall(run, b, dt):
    if b.p.get('regen') and b.hp < b.mhp:
        b.hp = min(b.mhp, b.hp + b.p['regen'] * dt)

ACT = {'cannon': act_cannon, 'tree': act_tree, 'henge': act_henge, 'sapling': act_sapling, 'vessel': None,
       'stash': act_none, 'tin': act_tin, 'cushion': act_none, 'loom': act_loom, 'head': act_head, 'stitched': act_unit,
       'dart': act_unit, 'mangled': act_unit, 'archer': act_archer, 'bomber': act_bomber, 'wall': act_wall, 'hut': act_hut}

def act_vessel_dispatch(run, b, dt):
    if run.vessel == 'spool': act_spool(run, b, dt)
    else: act_vessel(run, b, dt)
ACT['vessel'] = act_vessel_dispatch

Run.policy_laser_mining = lambda self, b: self.pol.laser_mines(b)
