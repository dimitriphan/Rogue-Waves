"""Mode balance review 2026-11-05 (../MODE_BALANCE_REVIEW_2026-11-05.md): the mode's per-map foe scaling, as
RogueWavesMode/Code/21_Difficulty.cs builds it, fed to the frozen simulator of ../balance_upgrades_2026-11-04/ (imported,
not changed). Python 3, standard library only.

What this adds on top of the simulator, all by wrapping it:
- mode_table(map, players, ...): Downpour's table as RWDifficulty.Apply writes it for a map: counts x(1 + 0.06(map-1))
  x(1 + 0.35 per player past the first), rounded like Mathf.RoundToInt (banker's, as Python's round); spawn intervals
  x max(0.5, 1 - 0.03(map-1)); one foe in five of each row split off with one of the map's modifiers, in turn; breaks 60 s
  (the opening one 45 s). Health is carried in the elite key (below), so it reaches every foe.
- Health: the mode adds +0.08(map-1) as a Multi mod, which ADDS to an elite's Health_Multi (kit docs/13 line 166,
  docs/17 §2.1): a Hardened foe at map 10 has x(1 + 3 + 0.72) = x4.72, not x4 x1.72. `compound=True` models a Pmulti
  instead (proposal F2): x(1 + elite) x(1 + map).
- Modifiers: the campaign pool is pack, stock, pack, stock (RWDifficulty.CampaignPool); one is on from map 3 and one
  more every second map. The sim has no pack modifier brains: a pack modifier is stood in by +0.5 Health Multi (Aging's
  mid value; Cursed, Dazzling and Berserk weaken the player instead, so this is a floor). The stock ones are Tempered
  then Hardened (`stock` picks them), each a Multi that adds to the foe's own elite.
- power: every hit the player's side deals is multiplied by `power` (before armour, as a Damage stat would be), and
  `cost` multiplies every building and upgrade price: the stand-in for what a player carries (Directions, Cardinals).
"""
import os, sys, copy
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, '..', 'balance_upgrades_2026-11-04'))
import sim, tables, policies

COUNT_MAP, COUNT_PLAYER, INTERVAL_MAP, HEALTH_MAP = 0.06, 0.35, 0.03, 0.08
BASE_ELITE = {'Health': 3.0, 'Red': 1.0, 'Hyper': 2.0, 'Chaos': 14.0, 'Armor': 0.0}   # Health_Multi of each stock elite

def pool_on(m, stock=('Red', 'Health'), mods_from=3):
    pool = ['Pack', stock[0], 'Pack2', stock[1]]
    n = 0 if m < mods_from else min(4, 1 + (m - mods_from) // 2)
    return pool[:n]

def rows_for(m, finish):
    """Proposal F1 (stride): map m's Tide t plays Downpour's row round(t x F / 15), F = finish(m) the row its Tide 15
    plays (15 = as built); the bonus Tides go on from the row after F."""
    F = finish(m) if finish else 15
    rows = [max(t, int(t * F / 15 + 0.5)) for t in range(1, 16)]
    rows += [min(30, F + i) for i in range(1, 16)]
    return rows

def mode_table(m, players=1, count_map=COUNT_MAP, interval_map=INTERVAL_MAP, health_map=HEALTH_MAP, compound=False,
               stock=('Red', 'Health'), mods=True, share=5, base='downpour', count_curve=None, health_curve=None,
               finish=None, mods_from=3, rotate=False):
    tides0, breaks0 = tables.TABLES[base]
    cm = count_curve(m) if count_curve else count_map * (m - 1)
    hm = health_curve(m) if health_curve else health_map * (m - 1)
    scale = (1 + cm) * (1 + COUNT_PLAYER * max(0, players - 1))
    interval = max(0.5, 1 - interval_map * (m - 1))
    on = pool_on(m, stock, mods_from) if mods else []
    tides = []
    for i, row in enumerate(rows_for(m, finish)):
        t = tides0[row - 1]
        steps, nth = [], (i if rotate else 0)     # rotate: proposal F4, a one-row Tide doesn't always get the first modifier
        for s in t['steps']:
            if s[0] == 'wait':
                steps.append(s); continue
            _, kind, n, every, el = s
            total = max(1, round(n * scale))
            marked = total // share if on else 0
            steps.append(('spread', kind, total - marked, every * interval, key(el, None, hm, compound)))
            if marked:
                steps.append(('spread', kind, marked, every * interval, key(el, on[nth % len(on)], hm, compound)))
                nth += 1
        tides.append(dict(n=i + 1, time=t['time'], steps=steps))
    breaks = [breaks0[0]] + [60.0] * (len(breaks0) - 1)
    return tides, breaks

def key(el, mod, hm, compound):
    """An elite key the simulator's Foe reads from sim.ELITE, registered on first use."""
    k = '%s|%s|%.3f|%d' % (el or '', mod or '', hm, compound)
    if k in sim.ELITE:
        return k
    e = dict(STOCK_ELITE.get(el, {})) if el else {}
    multi = BASE_ELITE.get(el, 0.0) if el else 0.0
    if mod in ('Pack', 'Pack2'):
        multi += 0.5
    elif mod:
        multi += BASE_ELITE[mod]
        me = STOCK_ELITE[mod]
        for stat in ('dmg', 'rate', 'spd'):
            if stat in me: e[stat] = e.get(stat, 1.0) + (me[stat] - 1.0)   # Multi mods add
        if 'aggro' in me: e['aggro'] = max(e.get('aggro', 0.0), me['aggro'])
        if 'armor' in me: e['armor'] = e.get('armor', 0) + me['armor']
    e['hp'] = (1 + multi) * (1 + hm) if compound else (1 + multi + hm)
    sim.ELITE[k] = e
    return k

STOCK_ELITE = copy.deepcopy(sim.ELITE)

# The player's side: damage x power, prices x cost.
POWER = {'power': 1.0, 'cost': 1.0}
_hurt = sim.Run.hurt
def hurt(self, f, dmg, src=None, kind='hit'):
    return _hurt(self, f, dmg * self.tune.get('power', 1.0), src, kind)
sim.Run.hurt = hurt
_cost = policies.Policy.cost
def cost(self, kind):
    c = self.run.tune.get('cost', 1.0)
    return tuple(x * c for x in _cost(self, kind))
policies.Policy.cost = cost
_upgrade = policies.Policy.upgrade
def upgrade(self, b, name):
    c = self.run.tune.get('cost', 1.0)
    if c != 1.0 and 'up_' + name not in self.run.tune:
        self.run.tune['up_' + name] = tuple(x * c for x in policies.UP[name][0])
    return _upgrade(self, b, name)
policies.Policy.upgrade = upgrade

# Elite keys reach Spool's Thread drop through f.elite: only stock elites count there (as the game's).
_fell = sim.Run.fell
def fell(self, f, src):
    if f.elite and '|' in f.elite:
        el, mod = f.elite.split('|')[:2]
        f.elite = el or (mod if mod in BASE_ELITE else None)
    return _fell(self, f, src)
sim.Run.fell = fell

def register(name, table):
    tables.TABLES[name] = table
    return name

def run(v, mapname, table, tides, tune=None, seed=1):
    r = sim.Run(v, mapname, table, 'smart', tune=tune or {}, seed=seed, tides=tides)
    return r.play()
