"""2026-11-04 copy of ../balance_rerun_2026-11-03/policies.py, plus the proposed upgrades (UP, "2026-11-04") and two
policies that buy them: YosseleWeb (tune `web`) and SpoolPkg (tune `pkg`, a list of upgrade names).

How the simulated player spends: 'bot' copies the 2026-10-26 virtual player's habits, 'smart' plays the vessel the
way its sheet means (saves for key buys, walls a choke with Pin Cushions, spends Blue on Stonehenges), 'round' (Spool
only) copies the 2026-11-03 balance round's PC harness. Re-run 2026-11-03: bots put gatherers by the nearest nodes, smart
players by the richest (the round model's nodes, sim.NODES)."""
import math

# upgrade name -> (cost R,B,Y, building kind, tier, requires one of)
UP = {
    # Yossele cannon
    'Sticky': ((50, 0, 0), 'cannon', 2, None), 'Farout': ((45, 10, 0), 'cannon', 2, None),
    'Viral': ((90, 0, 0), 'cannon', 3, None), 'Deathcap': ((100, 0, 0), 'cannon', 3, None),
    'Finish': ((420, 0, 0), 'cannon', 4, None), 'Hallucinogen': ((360, 280, 0), 'cannon', 4, None),
    # tree
    'Packets': ((40, 0, 0), 'tree', 2, None), 'Demand': ((20, 0, 40), 'tree', 2, None),
    'Picky': ((100, 0, 0), 'tree', 3, None), 'Weeds': ((90, 0, 30), 'tree', 3, None), 'Dmds': ((0, 150, 40), 'tree', 3, None),
    'Rubbery': ((450, 600, 0), 'tree', 4, None), 'Storm': ((1050, 800, 2400), 'tree', 5, None),
    # henge
    'Verdant': ((0, 30, 0), 'henge', 2, None), 'Blockade': ((0, 35, 0), 'henge', 2, None),
    'Remedy': ((0, 90, 0), 'henge', 3, None), 'Carnivore': ((30, 80, 0), 'henge', 3, None), 'Easy': ((75, 40, 0), 'henge', 3, None),
    'Roots': ((300, 420, 0), 'henge', 4, None), 'Obsidian': ((0, 600, 0), 'henge', 4, None),
    # sapling
    'Focused': ((0, 0, 30), 'sapling', 2, None), 'Sustaining': ((0, 10, 25), 'sapling', 2, None),
    'Grafted': ((60, 60, 60), 'sapling', 3, None), 'Forest': ((0, 0, 140), 'sapling', 3, None), 'Mulch': ((40, 0, 30), 'sapling', 3, None),
    'Harvest': ((0, 0, 400), 'sapling', 4, None), 'Connected': ((0, 300, 120), 'sapling', 4, None),
    # Spool
    'Recycle': ((30, 0, 0), 'tin', 2, None), 'Woolen': ((30, 0, 0), 'tin', 2, None),
    'Discounted': ((90, 0, 0), 'tin', 3, None), 'Design': ((100, 0, 0), 'tin', 3, None), 'Surprise': ((400, 0, 0), 'tin', 4, None),
    'Delivery': ((30, 0, 0), 'head', 2, None), 'Patchwork': ((30, 0, 0), 'head', 2, None),
    'Sharper': ((0, 30, 0), 'cushion', 2, None), 'Unravel': ((0, 30, 0), 'cushion', 2, None),
    'Barrage': ((0, 100, 0), 'cushion', 3, None), 'Stuffing': ((0, 90, 0), 'cushion', 3, None),
    'Overstuffed': ((0, 400, 0), 'cushion', 4, None), 'Burst': ((0, 400, 0), 'cushion', 4, None),
    'Direct': ((0, 0, 30), 'loom', 2, None), 'Seam': ((0, 0, 30), 'loom', 2, None),
    # 2026-11-04: the proposed upgrades (UPGRADE_GAPS_2026-11-04.md)
    'Web': ((0, 0, 250), 'sapling', 4, None),
    'Tacking': ((110, 0, 0), 'tin', 3, None), 'Cross': ((350, 0, 0), 'tin', 4, None), 'Army': ((2500, 0, 1000), 'tin', 5, None),
    'Needle': ((80, 0, 0), 'head', 3, None), 'Stitchtime': ((300, 0, 100), 'head', 4, None), 'Class': ((2000, 0, 1500), 'head', 5, None),
    'Bobbin': ((0, 0, 60), 'head', 3, None), 'Count': ((0, 0, 250), 'head', 4, None),
    'Thimble': ((0, 300, 300), 'cushion', 4, None), 'Voodoo': ((0, 2000, 1500), 'cushion', 5, None),
    'Dyed': ((0, 0, 150), 'loom', 3, None), 'Winder': ((0, 0, 100), 'loom', 3, None),
    # Smacky
    'Rapid': ((35, 0, 0), 'archer', 2, None), 'Auto': ((45, 0, 0), 'archer', 3, None), 'Squad': ((500, 50, 0), 'archer', 4, None),
    'Repair': ((0, 20, 0), 'wall', 2, None), 'Reinforced': ((0, 25, 0), 'wall', 2, None), 'Stone': ((0, 75, 0), 'wall', 3, None),
    'Spikes': ((10, 15, 0), 'wall', 2, None), 'Steel': ((0, 325, 0), 'wall', 4, None),
    'Miner2': ((0, 0, 30), 'hut', 2, None), 'Miner3': ((0, 0, 100), 'hut', 3, None),
    'Quick': ((50, 0, 0), 'bomber', 2, None), 'Cannons': ((175, 0, 0), 'bomber', 3, None), 'Bounce': ((600, 0, 0), 'bomber', 4, None),
}
LEVEL_UP = {'wall': (150, 0), 'cannon': (25, 0.25), 'henge': (40, 0), 'sapling': (20, 0), 'tree': (25, 0.25), 'tin': (25, 0), 'cushion': (40, 0),
            'loom': (20, 0), 'archer': (25, 1.0), 'bomber': (25, 2.0), 'head': (0, 0)}
BUILD = {'cannon': (15, 0, 0), 'tree': (50, 0, 0), 'henge': (0, 25, 0), 'sapling': (0, 0, 35),
         'stash': (0, 0, 0), 'tin': (50, 0, 0), 'head': (40, 0, 0), 'cushion': (0, 20, 0), 'loom': (0, 0, 40),
         'archer': (25, 0, 0), 'bomber': (25, 25, 0), 'wall': (0, 20, 0), 'hut': (0, 0, 35)}

def sim_value(node, run):
    import sim
    return sim.node_value(node, run.tune.get('opal', 'full'))

def make(run):
    if run.policy == 'smart' and run.vessel == 'yossele' and run.tune.get('web'): return YosseleWeb(run)
    if run.policy == 'smart' and run.vessel == 'spool' and run.tune.get('pkg'): return SpoolPkg(run)
    return {('yossele', 'bot'): YosseleBot, ('yossele', 'smart'): YosseleSmart, ('spool', 'bot'): SpoolBot,
            ('spool', 'smart'): SpoolSmart, ('spool', 'round'): SpoolRound, ('smacky', 'smart'): SmackySmart, ('smacky', 'bot'): SmackySmart}[(run.vessel, run.policy)](run)

class Policy:
    def __init__(self, run):
        self.run = run; self.r = run.rng
    def cost(self, kind):
        c = list(BUILD[kind]); T = self.run.tune
        if kind == 'tin' and 'tin_cost' in T: c[0] = T['tin_cost'] if not self.run.alive('tin') or T.get('tin_cost_all') else c[0]
        if kind == 'cannon' and 'cannon_cost' in T: c[0] = T['cannon_cost']
        if kind == 'tree' and 'tree_cost' in T: c = list(T['tree_cost'])
        if kind == 'cushion' and 'cushion_cost' in T: c[1] = T['cushion_cost']
        if kind == 'henge' and 'henge_cost' in T: c = list(T['henge_cost'])
        return tuple(c)
    def build(self, kind, cl=0, **kw):
        c = self.cost(kind)
        if not self.run.afford(c): return None
        self.run.pay(c)
        b = self.run.add(kind, cl, **kw)
        self.run.say('build', kind, 'cl', cl, 'wallet', [round(w) for w in self.run.wallet])
        return b
    def upgrade(self, b, name):
        c, kind, tier, _ = UP[name]
        c = tuple(self.run.tune.get('up_' + name, c))
        if not b.alive or name in b.up or not self.run.afford(c): return False
        if len(b.up) != tier - 2: return False
        self.run.pay(c); b.up.add(name)
        hp, dmg = LEVEL_UP.get(b.kind, (0, 0))
        b.mhp += hp; b.hp += hp
        if 'dmg' in b.p: b.p['dmg'] += dmg
        if name == 'Blockade': b.mhp *= 1.5; b.hp *= 1.5
        # 2026-11-04
        if name == 'Thimble': b.mhp += 200; b.hp += 200
        if name == 'Army': b.p['units'] += 3; b.p['every'] = 2.0
        if name == 'Cross':
            for u in self.run.blds:
                if u.alive and u.tin is b and u.kind in ('stitched', 'dart'): u.p['dmg'] *= 2
        if name == 'Obsidian': b.mhp *= 3; b.hp *= 3
        if name == 'Rapid': b.p['aspd'] += 0.5
        if name == 'Auto': b.p['aspd'] *= 1.5
        if name == 'Squad': b.p['aspd'] *= 2; b.p['dmg'] += 4
        if name == 'Quick': b.p['aspd'] += 0.15
        if name == 'Repair': b.p['regen'] = 1.5
        if name == 'Reinforced': b.mhp += 50; b.hp += 50
        if name == 'Stone': b.mhp += 75; b.hp += 75
        if name == 'Steel': b.mhp += 175; b.hp += 175
        if name == 'Spikes': b.p['spikes'] = 3
        if name in ('Miner2', 'Miner3'): b.p['hit'] = b.p.get('hit', 0.5) + 0.5
        if name == 'Cannons': b.p['aspd'] *= 1.2 * 1.8
        if name == 'Bounce': b.p['dmg'] += 4
        self.run.say('upgrade', b.kind, name)
        return True
    def clusters(self):
        return list(range(self.run.m['cores']))
    def cl_for(self, kind):
        """The cluster with the fewest of this kind (round robin on two-core maps)."""
        cls = self.clusters()
        return min(cls, key=lambda c: sum(1 for b in self.run.alive(kind) if b.cl == c))
    # hooks
    def setup(self): pass
    def tick(self): pass
    def on_tide_start(self, n): pass
    def on_tide_end(self, n): pass
    def returns_heads(self): return False
    def wants_dart(self, tin): return False
    def laser_mines(self, b): return False

# ------------------------------------------------------------------------------------------------- Yossele
class YosseleBot(Policy):
    """The play-test bot: cannons first with every 15 Red, gatherers by the ore, upgrades on fixed routes."""
    routes = {'cannon': [['Sticky', 'Viral', 'Finish'], ['Farout', 'Deathcap', 'Hallucinogen']],
              'tree': [['Demand', 'Dmds', 'Rubbery']], 'henge': [['Verdant', 'Remedy', 'Roots'], ['Blockade', 'Carnivore', 'Obsidian']],
              'sapling': [['Sustaining', 'Forest', 'Harvest'], ['Focused', 'Grafted', 'Connected']]}
    def setup(self):
        run = self.run
        self.v = run.add('vessel_y', 0); run.vb = self.v
        self.build('cannon', 0); self.build('cannon', self.cl_for('cannon'))
        self.build('henge', 0)
        s = self.build('sapling', 0, node=run.node(0))
        s.cd = 45.0   # the bot builds it as Tide 1 starts
        self.n = 0
    def laser_mines(self, b):
        return self.run.m['ore']['laser'] and self.r.random() < 0.5
    def tick(self):
        run = self.run
        if run.t % 5 > 1.0: return
        self.n += 1
        # the bot's order: Tree once at Tide 2-3 if it has 50 (it did on Purlest), else another cannon
        if not run.alive('tree') and run.tide >= 2 and run.wallet[0] >= 50 and self.r.random() < run.tune.get('bot_tree', 0.5):
            self.build('tree', 0); return
        if len(run.alive('sapling')) < 2 and run.wallet[2] >= 35 and run.tide >= 1:
            self.build('sapling', 0, node=run.node(len(run.alive('sapling')))); return
        if len(run.alive('henge')) < 1 + run.tide // 3 and run.wallet[1] >= 25:
            self.build('henge', self.cl_for('henge')); return
        if run.wallet[0] >= 15 and len(run.alive('cannon')) < 2 + run.tide:
            self.build('cannon', self.cl_for('cannon')); return
        # upgrades along routes, oldest first
        for b in run.alive():
            if b.kind in self.routes:
                b.p.setdefault('route', sum(1 for o in self.run.blds if o.kind == b.kind and o is not b and 'route' in o.p))
                route = self.routes[b.kind][b.p['route'] % len(self.routes[b.kind])]
                lvl = len(b.up)
                if lvl < len(route) and b.growth >= lvl and self.upgrade(b, route[lvl]): return
        if run.wallet[0] >= 15 and len(run.alive('cannon')) < run.tune.get('bot_cannons', 2) + 1.2 * run.tide:
            self.build('cannon', self.cl_for('cannon'))

class YosseleSmart(Policy):
    """Saves for the Tree at Tide 2, two Saplings on rich ore, cannons to a target count, Farout then Deathcap, Blue into
    Stonehenges with Verdant and Carnivore, never D-MDS."""
    def setup(self):
        run = self.run
        self.v = run.add('vessel_y', 0); run.vb = self.v
        for cl in self.clusters():
            self.build('cannon', cl)
        if len(self.clusters()) == 1: self.build('cannon', 0)
        self.seal_on = run.tune.get('seal', True)
        if self.seal_on:
            for cl in self.clusters():
                for i in range(run.tune.get('seal_n', run.m['seal'])):
                    self.build('henge', cl, seal=True)
        else:
            self.build('henge', 0)
        if run.m['ore']['sapling'] and run.ore_value() >= run.tune.get('sapling_min_yield', 1.0):
            self.build('sapling', 0, node=run.node(0, rich=True))
    def laser_mines(self, b):
        return self.run.tune.get('laser_mines', False) and self.run.m['ore']['laser']
    def tick(self):
        run = self.run; W = run.wallet; tide = run.tide
        ncl = len(self.clusters())
        if self.seal_on:
            for cl in self.clusters():
                walls = [b for b in run.alive('henge') if b.cl == cl and b.seal]
                if len(walls) < run.tune.get('seal_n', run.m['seal']) and W[1] >= self.cost('henge')[1]:
                    self.build('henge', cl, seal=True); return
            for h in run.alive('henge'):
                if h.seal and (self.upgrade(h, 'Blockade') or self.upgrade(h, 'Remedy')): return
        if run.t % 2 > 1.0: return
        cannons = run.alive('cannon'); trees = run.alive('tree'); henges = run.alive('henge'); saps = run.alive('sapling')
        target = min(3 + 2 * tide, 16) * ncl if tide <= 4 else min(4 + tide, 18) * ncl
        # 1. Tree from Tide 2 (save for it)
        if not trees and tide >= run.tune.get('tree_tide', 2):
            if W[0] >= self.cost('tree')[0]: self.build('tree', self.cl_for('tree'))
            return
        # 2. Saplings on rich ore
        nxt = run.node(len(saps), rich=True)
        rich2 = run.ore_value() >= 2 if run.model == 'study' else sim_value(nxt, run) >= 2
        if run.m['ore']['sapling'] and rich2 and len(saps) < 2 and W[2] >= 35:
            self.build('sapling', 0, node=nxt); return
        if saps:
            for s in saps:
                if self.upgrade(s, 'Focused'): return
        # 3. Blue sink: one Stonehenge per cluster at first, with Verdant then Carnivore
        if not self.seal_on and len(henges) < ncl * (1 + tide // 5) and W[1] >= 25:
            self.build('henge', self.cl_for('henge')); return
        for h in henges:
            if not h.seal and (self.upgrade(h, 'Verdant') or self.upgrade(h, 'Carnivore')): return
        # 4. Tree upgrades (the D-MDS trap is a tune switch: the bot took it at level 3)
        for t in trees:
            if self.upgrade(t, 'Packets'): return
            if len(t.up) == 1 and self.upgrade(t, 'Dmds' if run.tune.get('force_dmds') else 'Picky'): return
        if run.tune.get('fruit_pickup') and run.tune.get('fruit_red'):
            for s in saps:
                if len(s.up) == 1 and self.upgrade(s, 'Grafted'): return
        # 5. cannons to the target count, then upgrades on the oldest
        if len(cannons) < target and W[0] >= self.cost('cannon')[0]:
            self.build('cannon', self.cl_for('cannon')); return
        for c in cannons:
            if c.growth >= 1 and (self.upgrade(c, 'Farout') or self.upgrade(c, 'Deathcap')): return
        if W[0] >= self.cost('cannon')[0] + 40 and len(cannons) < 24 * ncl:
            self.build('cannon', self.cl_for('cannon'))
        for c in cannons:
            if c.growth >= 3 and len(c.up) == 2 and self.upgrade(c, 'Finish'): return

# ------------------------------------------------------------------------------------------------- Spool
class SpoolBot(Policy):
    """The play-test bot: Stash, Tin, a Pin Cushion on the path (walked round), Loom by the ore, a Thread Head at Tide 1,
    upgrades on fixed routes; the vessel stays busy building."""
    routes = {'tin': [['Recycle', 'Discounted', 'Surprise'], ['Woolen', 'Design', 'Surprise']],
              'cushion': [['Sharper', 'Barrage'], ['Unravel', 'Stuffing', 'Overstuffed'], ['Sharper', 'Stuffing', 'Burst']],
              'loom': [['Direct'], ['Seam']], 'head': [['Patchwork']]}
    def setup(self):
        run = self.run
        self.v = run.add('vessel_s', 0); self.v.p['off'] = True; run.vb = self.v   # off the lane: building and walking
        run.add('stash', 0)
        t = self.build('tin', 0); t.p['target'] = run.m['merge'][0] - 1.0
        for i in range(run.tune.get('free_stitched', 0)):
            run.add('stitched', 0, tin=t, spot=t.p['target'], cost=10)
        self.build('cushion', 0, seal=False)
        l = self.build('loom', 0, node=run.node(0))
        l.cd = 40.0; l.p['spin_next'] = 40.0   # the bot walks 10 tiles and aims before it stands
    def tick(self):
        run = self.run
        if run.t % 5 > 1.0: return
        if run.tide >= 1 and not run.alive('head') and run.wallet[0] >= 40:
            self.build('head', 0, spot=run.m['merge'][0] - 0.5); return
        if len(run.alive('loom')) < min(2, run.tide) and run.wallet[2] >= 40:
            self.build('loom', 0, node=run.node(len(run.alive('loom')))); return
        if len(run.alive('cushion')) < 1 + run.tide and run.wallet[1] >= 20:
            self.build('cushion', 0, seal=False); return
        for b in run.alive():
            if b.kind in self.routes:
                b.p.setdefault('route', sum(1 for o in self.run.blds if o.kind == b.kind and o is not b and 'route' in o.p))
                route = self.routes[b.kind][b.p['route'] % len(self.routes[b.kind])]
                lvl = len(b.up)
                if lvl < len(route) and self.upgrade(b, route[lvl]): return
        if run.tide >= 2 and len(run.alive('tin')) < 1 + run.tide // 2 and run.wallet[0] >= 50:
            t = self.build('tin', 0)
            if t: t.p['target'] = run.m['merge'][0] - 1.0

class SpoolSmart(Policy):
    """Walls the kill zone shut with Pin Cushions (the sheet's 'main line of defense'), stands Spool and the Crafted at
    the wall, Loom with Direct Line, Thread Head at the wall with Threaded Delivery, PATCH on a failing Cushion."""
    def setup(self):
        run = self.run; T = run.tune
        self.patch_cd = 0.0
        self.seal_on = T.get('seal', True)
        for cl in self.clusters():
            spot = run.m['merge'][cl]
            if cl == 0:
                run.add('stash', cl)
            t = self.build('tin', cl)
            if t:
                t.p['target'] = spot
                for i in range(T.get('free_stitched', 0)):
                    run.add('stitched', cl, tin=t, spot=spot, cost=10)
        # Spool stands where no Tin could be afforded (the second core on a two-core map), else at the wall
        bare = [cl for cl in self.clusters() if not any(t.cl == cl for t in run.alive('tin'))]
        vcl = bare[0] if bare else 0
        self.v = run.add('vessel_s', vcl, spot=run.m['merge'][vcl]); run.vb = self.v
        for cl in self.clusters():
            for i in range(T.get('seal_n', run.m['seal']) if self.seal_on else T.get('open_cushions', 0)):
                self.build('cushion', cl, spot=run.m['merge'][cl], seal=self.seal_on)
        if run.m['ore']['sapling'] and T.get('loom_first', True):
            self.build('loom', 0, node=run.node(0, rich=True))
    def returns_heads(self):
        return True   # the player presses RETURN on a full Head
    def wants_dart(self, tin):
        n = sum(1 for u in self.run.alive('dart') if u.tin is tin)
        return n < self.run.tune.get('dart_n', 2)
    def tick(self):
        run = self.run; W = run.wallet; tide = run.tide; T = run.tune
        cls = self.clusters()
        # PATCH a failing wall (20 Thread, 20% of its health, every 5 s)
        if self.patch_cd <= run.t and run.thread >= 20:
            hurt = [b for b in run.alive() if b.seal and b.hp < 0.5 * b.mhp]
            if hurt:
                b = min(hurt, key=lambda b: b.hp / b.mhp)
                b.hp = min(b.mhp, b.hp + min(500, 0.2 * b.mhp)); run.thread -= 20; run.th['spent'] += 20
                self.patch_cd = run.t + 5.0
        # rebuild a fallen wall at once
        if self.seal_on:
            for cl in cls:
                walls = [b for b in run.alive('cushion') if b.cl == cl and b.seal]
                need = T.get('seal_n', run.m['seal'])
                if len(walls) < need and W[1] >= self.cost('cushion')[1]:
                    self.build('cushion', cl, spot=run.m['merge'][cl], seal=True); return
        if run.t % 2 > 1.0: return
        # Loom and Direct Line (Yellow is spare)
        looms = run.alive('loom')
        if run.m['ore']['sapling']:
            if not looms and W[2] >= 40: self.build('loom', 0, node=run.node(0, rich=True)); return
            for l in looms:
                if self.upgrade(l, 'Direct'): return
        # Thread Head at the wall
        heads = run.alive('head')
        if len([h for h in run.blds if h.kind == 'head']) < len(cls) and W[0] >= 40 and tide >= T.get('head_tide', 1):
            cl = min(cls, key=lambda c: sum(1 for h in run.blds if h.kind == 'head' and h.cl == c))
            self.build('head', cl, spot=run.m['merge'][cl]); return
        for h in heads:
            if self.upgrade(h, 'Delivery'): return
        tins = run.alive('tin')
        for t in tins:
            if self.upgrade(t, 'Woolen'): return
        # a second layer of Cushions behind is just health: upgrade the wall instead
        for c in run.alive('cushion'):
            if c.seal and self.upgrade(c, 'Sharper'): return
        # more Tins as Red allows (4 Crafted each)
        if len(tins) < len(cls) * (1 + tide // T.get('tin_every', 1)) and W[0] >= self.cost('tin')[0]:
            cl = min(cls, key=lambda c: sum(1 for b in tins if b.cl == c))
            t = self.build('tin', cl)
            if t: t.p['target'] = run.m['merge'][cl]
            return
        for i, t in enumerate(tins):
            if self.upgrade(t, 'Design' if ((i % 2 == 1 or T.get('dart_all')) and T.get('darts', True)) else 'Discounted'): return
        if looms and len(looms) < 2 and W[2] >= 40:
            self.build('loom', 0, node=run.node(len(looms), rich=True)); return
        for l in looms:
            if self.upgrade(l, 'Seam'): return
        for c in run.alive('cushion'):
            if c.seal and len(c.up) == 1 and self.upgrade(c, 'Stuffing'): return
        for t in tins:
            if len(t.up) == 2 and self.upgrade(t, 'Surprise'): return
        for c in run.alive('cushion'):
            if c.seal and len(c.up) == 2 and self.upgrade(c, 'Overstuffed'): return

class SpoolRound(Policy):
    """The balance round's PC harness (Spool `98_Play.cs` with SPOOL_SMART, 2026-11-03): three Pin Cushions on a line
    across the last stretch, 5 tiles out (walked round unless `seal`), first, or after one Tin with `tin_first`; then in
    order a Tin, a Thread Head, Looms by the nearest nodes (one from Tide 1, two from Tide 3), Tins (two from Tide 2, three
    from Tide 5), Thread Heads (two from Tide 2); then the lowest-tier upgrade it can afford on each building's route.
    Crafted stand two tiles behind the line, Spool holds the core's doorstep, the Heads fetch the Thread."""
    routes = {'tin': [['Woolen', 'Design', 'Surprise']], 'head': [['Delivery'], ['Patchwork']],
              'cushion': [['Sharper', 'Barrage'], ['Unravel', 'Stuffing', 'Overstuffed'], ['Sharper', 'Stuffing', 'Burst']],
              'loom': [['Direct'], ['Seam']]}
    LINE, CRAFTED = 5.0, 3.0
    def setup(self):
        run = self.run
        self.v = run.add('vessel_s', 0, spot=0.5); run.vb = self.v
        run.add('stash', 0)
        self.spend()
    def wants_dart(self, tin):
        return sum(1 for u in self.run.alive('dart') if u.tin is tin) < self.run.tune.get('dart_n', 2)
    def tick(self):
        if self.run.t % 5 > 1.0: return   # the virtual player walks to each build
        self.spend()
    def made(self, kind):
        return len([b for b in self.run.blds if b.kind == kind and b.alive])
    def spend(self):
        run = self.run; T = run.tune; tide = run.tide
        tins, cush, looms, heads = self.made('tin'), self.made('cushion'), self.made('loom'), self.made('head')
        want_tins = 3 if tide >= 5 else 2 if tide >= 2 else 1
        seal = T.get('seal', False)
        def tin():
            t = self.build('tin', 0)
            if t: t.p['target'] = self.CRAFTED
            return t
        if T.get('tin_first') and tins < 1:
            tin(); return
        if cush < 3 and self.run.afford(self.cost('cushion')):
            self.build('cushion', 0, spot=self.LINE, seal=seal); return
        want_looms = 2 if tide >= 3 else 1 if tide >= 1 else 0
        want_heads = 2 if tide >= 2 else 1
        if tins < 1 and run.afford(self.cost('tin')): tin(); return
        if heads < 1 and run.afford(self.cost('head')): self.build('head', 0, spot=self.LINE - 1.0); return
        if looms < want_looms and run.afford(self.cost('loom')): self.build('loom', 0, node=run.node(looms)); return
        if tins < want_tins and run.afford(self.cost('tin')): tin(); return
        if heads < want_heads and run.afford(self.cost('head')): self.build('head', 0, spot=self.LINE - 1.0); return
        best = None
        for b in run.alive():
            if b.kind in self.routes:
                b.p.setdefault('route', sum(1 for o in run.blds if o.kind == b.kind and o is not b and 'route' in o.p))
                route = self.routes[b.kind][b.p['route'] % len(self.routes[b.kind])]
                lvl = len(b.up)
                if lvl < len(route):
                    name = route[lvl]
                    if run.afford(tuple(T.get('up_' + name, UP[name][0]))) and (best is None or UP[name][2] < UP[best[1]][2]):
                        best = (b, name)
        if best: self.upgrade(*best)

# ------------------------------------------------------------------------------------------------- Smacky (a stock yardstick)
class SmackySmart(Policy):
    """Archers first, walls across the kill zone (its Blue), a Mining Hut with more miners, Rapid then Auto Bow."""
    def setup(self):
        run = self.run; T = run.tune
        self.v = run.add('vessel_m', 0); run.vb = self.v
        self.seal_on = T.get('seal', True)
        for cl in self.clusters(): self.build('archer', cl)
        if len(self.clusters()) == 1: self.build('archer', 0)
        if self.seal_on:
            for cl in self.clusters():
                for i in range(T.get('seal_n', run.m['seal'])):
                    self.build('wall', cl, spot=run.m['merge'][cl], seal=True)
    def tick(self):
        run = self.run; W = run.wallet; tide = run.tide; T = run.tune
        if self.seal_on:
            for cl in self.clusters():
                walls = [b for b in run.alive('wall') if b.cl == cl and b.seal]
                if len(walls) < T.get('seal_n', run.m['seal']) and W[1] >= 20:
                    self.build('wall', cl, spot=run.m['merge'][cl], seal=True); return
        if run.t % 2 > 1.0: return
        archers = run.alive('archer'); bombers = run.alive('bomber'); huts = run.alive('hut')
        if run.m['ore']['sapling'] and len(huts) < 1 and W[2] >= 35: self.build('hut', 0, node=run.node(0, rich=True)); return
        for w in run.alive('wall'):
            if self.upgrade(w, 'Repair') or self.upgrade(w, 'Stone'): return
        for h in huts:
            if self.upgrade(h, 'Miner2') or self.upgrade(h, 'Miner3'): return
        for a in archers:
            if self.upgrade(a, 'Rapid') or self.upgrade(a, 'Auto'): return
        if len(bombers) < len(archers) // 3 and self.run.afford((25, 25, 0)):
            self.build('bomber', self.cl_for('bomber')); return
        for b in bombers:
            if self.upgrade(b, 'Quick') or self.upgrade(b, 'Cannons'): return
        if W[0] >= 25 and len(archers) < 24 * len(self.clusters()): self.build('archer', self.cl_for('archer'))
        for a in archers:
            if len(a.up) == 2 and self.upgrade(a, 'Squad'): return


# ------------------------------------------------------------------------------------------------- 2026-11-04
class YosseleWeb(YosseleSmart):
    """YosseleSmart, and its first Sapling goes Focused Roots -> Forest Project -> Wood Wide Web (the trade, sim.Run.convert)."""
    def tick(self):
        saps = self.run.alive('sapling')
        if saps:
            s = saps[0]
            if len(s.up) == 1 and self.upgrade(s, 'Forest'): return
            if len(s.up) == 2 and self.upgrade(s, 'Web'): return
        super().tick()

class SpoolPkg(SpoolSmart):
    """SpoolSmart, buying first whichever of the proposed upgrades `tune['pkg']` lists, each on its building's next tier:
    Tins Tacking -> Cross -> Army; a Head Needle -> Stitchtime -> Class (buffer) or Bobbin -> Count (porter); the wall
    Thimble -> Voodoo after Sharper Pins and Loose Stuffing; the first Loom Dyed, the others Winder; QuickPatch on the
    Stash (PATCH every 2.5 s for 15 Thread)."""
    def setup(self):
        super().setup()
        self.pkg = set(self.run.tune['pkg'])
        self.qp = False
    def tick(self):
        run = self.run; P = self.pkg
        if 'QuickPatch' in P and not self.qp and run.afford((0, 0, 40)):
            run.pay((0, 0, 40)); self.qp = True
        if self.qp and self.patch_cd > run.t + 2.5: self.patch_cd = run.t + 2.5
        if self.qp and self.patch_cd <= run.t and run.thread >= 15:
            hurt = [b for b in run.alive() if b.seal and not b.unit and b.hp < 0.5 * b.mhp]
            if hurt:
                b = min(hurt, key=lambda b: b.hp / b.mhp)
                b.hp = min(b.mhp, b.hp + min(500, 0.2 * b.mhp)); run.thread -= 15; run.th['spent'] += 15
                self.patch_cd = run.t + 2.5
        for t in run.alive('tin'):
            for name, lvl in (('Tacking', 1), ('Cross', 2), ('Army', 3)):
                if name in P and len(t.up) == lvl and self.upgrade(t, name): return
        for h in run.alive('head'):
            for name, lvl in (('Needle', 1), ('Stitchtime', 2), ('Class', 3), ('Bobbin', 1), ('Count', 2)):
                if name in P and len(h.up) == lvl and self.upgrade(h, name): return
        for c in run.alive('cushion'):
            if not c.seal: continue
            if 'Thimble' in P and len(c.up) == 1 and self.upgrade(c, 'Stuffing'): return
            if 'Thimble' in P and len(c.up) == 2 and self.upgrade(c, 'Thimble'): return
            if 'Voodoo' in P and len(c.up) == 3 and self.upgrade(c, 'Voodoo'): return
        for i, l in enumerate(run.alive('loom')):
            name = 'Dyed' if i == 0 and 'Dyed' in P else 'Winder' if 'Winder' in P else None
            if name and len(l.up) == 1 and self.upgrade(l, name): return
        super().tick()
