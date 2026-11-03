"""Monte Carlo: run a config many times on 4 cores and summarise."""
import sys, json, multiprocessing as mp, statistics as st
import os; sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

def one(args):
    from sim import Run
    cfg, seed = args
    r = Run(cfg['v'], cfg['m'], cfg.get('t', 'downpour'), cfg.get('p', 'smart'), tune=cfg.get('tune', {}),
            coins=cfg.get('coins', False), seed=seed, tides=cfg.get('n', 15))
    res = r.play()
    keep = {k: res[k] for k in ('lost', 'won', 'core', 'core_dmg', 'leaks', 'wallet', 'spent', 'kills', 'tide_wallet', 'thread', 'counts', 'dmg', 'tide_len', 'hits')}
    keep['income'] = res['income']
    return keep

def summarise(cfg, rs):
    n = len(rs); N = cfg.get('n', 15)
    win = sum(1 for r in rs if r['won']) / n
    lost = [r['lost'] for r in rs if r['lost']]
    def surv(k):  # share alive at the end of Tide k
        return sum(1 for r in rs if (not r['lost']) or r['lost'] > k) / n
    out = dict(name=cfg['name'], n=n, win=round(win, 3), med_lost=(st.median(lost) if lost else None),
               s4=round(surv(4), 3), s8=round(surv(8), 3), s10=round(surv(10), 3), s15=round(surv(15), 3) if N >= 15 else None,
               core_after4=round(st.mean([max(0, 100 - sum(r['core_dmg'][:5])) for r in rs]), 1),
               core_dmg_1_4=[round(st.mean([r['core_dmg'][k] for r in rs]), 1) for k in range(1, 5)],
               kills=round(st.mean([r['kills'] for r in rs])),
               income_mine=[round(st.mean([r['income']['mine'][i] for r in rs])) for i in range(3)],
               income_kills=round(st.mean([r['income']['kills'] for r in rs])),
               spent=[round(st.mean([r['spent'][i] for r in rs])) for i in range(3)],
               wallet_end=[round(st.mean([r['wallet'][i] for r in rs])) for i in range(3)])
    # wallet at each Tide start (mean over runs that reached it)
    tw = {}
    for r in rs:
        for t, w in r['tide_wallet']:
            tw.setdefault(t, []).append(w)
    out['tide_wallet'] = {t: [round(st.mean([w[i] for w in ws])) for i in range(3)] for t, ws in sorted(tw.items()) if len(ws) >= max(3, n // 5)}
    out['lost_hist'] = {k: lost.count(k) for k in sorted(set(lost))}
    if rs[0]['thread']['dropped'] or rs[0]['thread']['spun']:
        out['thread'] = {k: round(st.mean([r['thread'].get(k, 0) for r in rs])) for k in rs[0]['thread']}
    return out

def run_configs(cfgs, runs=100, out=None):
    jobs = [(c, s) for c in cfgs for s in range(1, runs + 1)]
    with mp.Pool(4) as pool:
        res = pool.map(one, jobs, chunksize=4)
    summ = []
    for i, c in enumerate(cfgs):
        rs = res[i * runs:(i + 1) * runs]
        summ.append(summarise(c, rs))
    if out:
        json.dump(summ, open(out, 'w'), indent=1)
    return summ

if __name__ == '__main__':
    cfgs = json.load(open(sys.argv[1]))
    runs = int(sys.argv[2]) if len(sys.argv) > 2 else 100
    for s in run_configs(cfgs, runs, sys.argv[3] if len(sys.argv) > 3 else None):
        print(json.dumps({k: s[k] for k in ('name', 'win', 's4', 's8', 's10', 'med_lost', 'core_after4', 'lost_hist')}))
