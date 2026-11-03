"""Markdown tables from result files: python3 summ.py results/res_L.json [more files]; `cell` and `load` are used by
the re-run report's tables (one short outcome per setup: "wins 100%" or "lost T11 (95%), T12")."""
import json, sys

def cell(s):
    """One setup's outcome in a few words: wins share when most runs win, else the commonest losing Tides."""
    n = s['n']; h = {int(k): v for k, v in s['lost_hist'].items()}
    if s['win'] >= 0.5:
        out = 'wins %d%%' % round(100 * s['win'])
        if h: out += ', else T%d' % max(h, key=h.get)
        return out
    top = sorted(h.items(), key=lambda kv: -kv[1])
    out = 'lost T%d (%d%%)' % (top[0][0], round(100 * top[0][1] / n))
    if len(top) > 1 and top[1][1] / n >= 0.15: out += ', T%d' % top[1][0]
    if s['win'] >= 0.15: out += ', wins %d%%' % round(100 * s['win'])
    return out

def load(*files):
    d = {}
    for f in files:
        for s in json.load(open(f)): d[s['name']] = s
    return d

def row(s):
    return '| %s | %s | %.1f | %d%% | %d%% | %d%% | %s | %s | %s |' % (
        s['name'], cell(s), s['mean_t'], round(100 * s['s4']), round(100 * s['s8']), round(100 * s['s10']),
        s['income_mine'], s['spent'], s['wallet_end'])

if __name__ == '__main__':
    for f in sys.argv[1:]:
        print('###', f)
        print('| setup | outcome | mean Tide | alive T4 | T8 | T10 | mined RBY | spent RBY | left RBY |')
        print('|---|---|---|---|---|---|---|---|---|')
        for s in json.load(open(f)): print(row(s))
