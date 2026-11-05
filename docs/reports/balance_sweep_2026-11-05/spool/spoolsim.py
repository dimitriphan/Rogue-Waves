"""The Spool part of the sweep: the 2026-11-04 simulator (../../balance_upgrades_2026-11-04/, imported, not changed) with
two knobs added by wrapping it, and the runner:
- `dart_rng`: a Dart's Range (3.5 as built; the frozen sim fixes it).
- `keep_wall`: the one wall is kept as a player keeps a wall: rebuilt when it falls, PATCHed, and upgraded (Sharper
  Pins, Extra Stuffing, Overstuffed), as the smart Spool does for a shut line. Without it the frozen policy builds the
  open Cushion once and never touches it again, and Spool ends Tide 5 with 3,600 Blue it has no use for. It flags the
  Cushion as a wall for the policy only: one Cushion is fewer than the map's three, so foes still walk round it.

"One wall" (decision 25): the smart Spool builds one Pin Cushion by each core's kill zone, and foes walk round it, as the
balance round saw three Cushions walked round on the PC (BALANCE_ROUND_2026-11-03.md M5): tune seal False, open_cushions 1.
The shut gap (three Cushions that hold, the model's old default) is kept for comparison only.

    python3 spoolsim.py experiments.json runs out.json
"""
import os, sys, json
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, '..', '..', 'balance_upgrades_2026-11-04'))
import sim, mc, policies

_make = sim.make
def make(kind, cl, run, **kw):
    b = _make(kind, cl, run, **kw)
    if kind == 'dart' and 'dart_rng' in run.tune:
        b.p['rng'] = run.tune['dart_rng']; b.reach = 1.0 + run.tune['dart_rng']
    return b
sim.make = make

_setup, _tick = policies.SpoolSmart.setup, policies.SpoolSmart.tick
def setup(self):
    _setup(self)
    if self.run.tune.get('keep_wall'):
        for c in self.run.alive('cushion'): c.seal = True
def tick(self):
    run = self.run
    if run.tune.get('keep_wall') and not self.seal_on:
        n = run.tune.get('open_cushions', 1)
        for cl in self.clusters():
            if sum(1 for c in run.alive('cushion') if c.cl == cl) < n and run.wallet[1] >= self.cost('cushion')[1]:
                c = self.build('cushion', cl, spot=run.m['merge'][cl], seal=False)
                if c: c.seal = True
                return
    return _tick(self)
policies.SpoolSmart.setup, policies.SpoolSmart.tick = setup, tick

if __name__ == '__main__':
    cfgs = json.load(open(sys.argv[1]))
    summ = mc.run_configs(cfgs, int(sys.argv[2]), sys.argv[3])
    for s in summ:
        print('%-44s win %.2f  mean_t %5.2f  lost %s  mined %s' % (s['name'], s['win'], s['mean_t'], s['lost_hist'], s['income_mine']), flush=True)
