"""W50: the player power (a multiplier on every hit the player's side deals, mode.py) at which a vessel wins half its runs.
Base game: Downpour's own table, 30 s breaks, won when Tide 30 is cleared. The mode's map k: mode.mode_table(k), won when
Tide 15 is cleared. A setup is harder than the base game when its W50 is above the base game's on the same vessel and map.
Bisection in log space over [LO, HI], the same seeds at every step (common random numbers).

    python3 w50.py <experiments.json> <out.json> [seeds] [steps]
experiments: [{"name", "v", "m", "kind": "base"|"mode", "map": k, "players": 1, "table": {mode_table kwargs}, "tune": {}}]
"""
import sys, json, math, multiprocessing as mp
import mode

import os
LO, HI = 0.12, float(os.environ.get('W50_HI', 8.0))

def table_for(e):
    if e['kind'] == 'base':
        return 'downpour', 30
    kw = dict(e.get('table', {}))
    for c in ('count_curve', 'health_curve', 'finish'):
        if c in kw: kw[c] = eval(kw[c])
    name = 'mode_%s' % json.dumps([e.get('map'), e.get('players', 1), e.get('table', {})], sort_keys=True)
    mode.register(name, mode.mode_table(e['map'], e.get('players', 1), **kw))
    return name, 15

def one(args):
    e, p, seed = args
    name, tides = table_for(e)
    tune = dict(e.get('tune', {})); tune['power'] = p
    r = mode.run(e['v'], e['m'], name, tides, tune=tune, seed=seed)
    return 1 if r['won'] else 0, (r['lost'] or tides + 1)

def main(path, out, seeds=12, steps=7):
    exps = json.load(open(path))
    lo = {e['name']: math.log(LO) for e in exps}; hi = {e['name']: math.log(HI) for e in exps}
    at1 = {}
    with mp.Pool(4) as pool:
        # power 1 first: the plain win rate and mean Tide reached
        res = pool.map(one, [(e, 1.0, s) for e in exps for s in range(1, seeds + 1)], chunksize=2)
        for i, e in enumerate(exps):
            rs = res[i * seeds:(i + 1) * seeds]
            at1[e['name']] = dict(win=sum(r[0] for r in rs) / seeds, mean_t=sum(r[1] for r in rs) / seeds)
        for k in range(steps):
            mids = {e['name']: math.exp((lo[e['name']] + hi[e['name']]) / 2) for e in exps}
            res = pool.map(one, [(e, mids[e['name']], s) for e in exps for s in range(1, seeds + 1)], chunksize=2)
            for i, e in enumerate(exps):
                w = sum(r[0] for r in res[i * seeds:(i + 1) * seeds]) / seeds
                if w >= 0.5: hi[e['name']] = math.log(mids[e['name']])
                else: lo[e['name']] = math.log(mids[e['name']])
            print('step', k + 1, flush=True)
    summ = []
    for e in exps:
        w50 = math.exp((lo[e['name']] + hi[e['name']]) / 2)
        summ.append(dict(e, w50=round(w50, 3), **at1[e['name']]))
        print(json.dumps({k: summ[-1][k] for k in ('name', 'w50', 'win', 'mean_t')}), flush=True)
    json.dump(summ, open(out, 'w'), indent=1)

if __name__ == '__main__':
    main(sys.argv[1], sys.argv[2], *(int(x) for x in sys.argv[3:]))
