"""Fit the pick from exp_E1's runs: Tide 15's row on each map (F1) and the health curve's lift (F3), per-map step and
hoard share (F5), to the target D curve.   python3 fit.py [res_E1.json]

How: W50 is close to proportional to foe health (the review's §5), so each (map, row) run gives a shape
g = asked / health, health being the curve the run used. For a candidate curve, asked(map, row) = g x its health; the
fit takes, for each map, the row whose asked is nearest the target (rows never go down from map to map), and scores the
curve by the squared log gap over the ten maps. Steps 1 and 2 take
each row's shape as measured on that map only. Step 3, the pick, smooths it first: one shape a row and one factor a map
(log g = row + b x (map - 1), least squares over all 72 setups), which the noise of 12 seeds needs (row 17 measured
easier than row 16, row 22 is the review's spike and is left out). Step 3 keeps the per-map step at Dimitri's 8% (scope
04, taken) and searches what the review's §7 hands the sweep: Tide 15's row (F1, never lower than the map before's, at
most three rows up, and map 10 on row 24), the lift (F3) and the hoard share (F5). Row 24 is Downpour's last mixed Tide
(Hardened Daggers, Aegis and Spears with Mauls and Onagers); rows 25-27 are one Superheated kind each and a third harder
in shape, so they overshoot. Fits within 5% of the best error go to the one with the later rows (F1's point: the base
game's own foes, not Daggers with more health); the best alone keeps maps 5-9 on row 21, a Tide of ten Onagers.
The target is the review's §7 range, as one line: D (mid case with C1) 0.80 on map 1, 1.00 on map 3, 1.50 on map 10.
"""
import json, math, os, sys
HERE = os.path.dirname(os.path.abspath(__file__))
REVIEW = os.path.join(HERE, '..', 'mode_balance_2026-11-05')
PW = json.load(open(os.path.join(HERE, 'results', 'power.json')))
BASE = {(r['v'], r['m']): r['w50'] for r in json.load(open(os.path.join(REVIEW, 'results', 'res_A.json'))) if r['kind'] == 'base'}
MID = [r['power'] for r in PW['mid_ramp']]
ITEMS = {c: [r['items'] for r in PW[c + '_ramp']] for c in ('low', 'mid', 'high')}
TARGET_D = [0.80, 0.88, 1.00, 1.06, 1.12, 1.19, 1.26, 1.33, 1.41, 1.50]
TARGET = [d * p for d, p in zip(TARGET_D, MID)]
S2_ROWS = [15, 16, 18, 19, 20, 22, 23, 24, 26, 27]
LAST = 24          # step 3: map 10's Tide 15 plays Downpour's row 24 (the docstring says why)

def gmean(vs): return math.exp(sum(math.log(v) for v in vs) / len(vs))
def health(k, lift, per_map, share, case='mid'):
    return (lift + per_map * (k - 1)) * (1 + share * ITEMS[case][k - 1])

def shapes(res):
    """g[(map, row)]: asked / health, asked the geometric mean over Smacky's maps of W50 / base W50."""
    by = {}
    for r in res:
        if r['kind'] == 'mode' and r['v'] == 'smacky':
            by.setdefault((r['map'], r['row']), []).append(r['w50'] / BASE[(r['v'], r['m'])])
    return {key: gmean(v) / health(key[0], 2.5, 0.08, 0.01) for key, v in by.items() if len(v) == 3}

def pick(g, lift, per_map, share):
    rows, err, prev = [], 0.0, 0
    for k in range(1, 11):
        opts = sorted(f for (m, f) in g if m == k and f >= prev)
        best = min(opts, key=lambda f: abs(math.log(g[(k, f)] * health(k, lift, per_map, share) / TARGET[k - 1])))
        rows.append(best); prev = best
        err += math.log(g[(k, best)] * health(k, lift, per_map, share) / TARGET[k - 1]) ** 2
    return rows, err

def report(g, rows, lift, per_map, share):
    print('lift %.2f, per map %.3f, share %.3f -> rows %s' % (lift, per_map, share, rows))
    print('map row   g     health  asked  target  D mid  D low  D high')
    for k, f in enumerate(rows, 1):
        a = g[(k, f)] * health(k, lift, per_map, share)
        d = {c: g[(k, f)] * health(k, lift, per_map, share, c) / PW[c + '_ramp'][k - 1]['power'] for c in ('low', 'mid', 'high')}
        print('%3d %3d %6.3f %6.3f %6.2f %6.2f  %5.2f  %5.2f  %5.2f' % (k, f, g[(k, f)], health(k, lift, per_map, share), a, TARGET[k - 1], d['mid'], d['low'], d['high']))

if __name__ == '__main__':
    res = json.load(open(sys.argv[1] if len(sys.argv) > 1 else os.path.join(HERE, 'results', 'res_E1.json')))
    g = shapes(res)
    print('shape g = asked / health, by map and row:')
    for k in range(1, 11):
        print('  map %2d: ' % k + '  '.join('row %d %.3f' % (f, g[(m, f)]) for (m, f) in sorted(g) if m == k))
    print()
    first = min(((pick(g, l / 100, 0.08, 0.01)[1], l / 100) for l in range(150, 401, 5)))
    rows, err = pick(g, first[1], 0.08, 0.01)
    print('Step 1, the review\'s step and share, lift searched (error %.4f):' % err)
    report(g, rows, first[1], 0.08, 0.01)
    best = min(((pick(g, l / 100, pm / 1000, sh / 1000)[1], l / 100, pm / 1000, sh / 1000)
                for l in range(150, 401, 10) for pm in range(0, 161, 20) for sh in range(0, 21, 5)))
    rows, err = pick(g, *best[1:])
    print()
    print('Step 2, all three searched (error %.4f):' % err)
    report(g, rows, *best[1:])
    # Step 3: the smoothed shapes
    keys = sorted(g); rws = sorted(set(f for (_, f) in keys))
    a = {f: 0.0 for f in rws}; b = 0.0
    for it in range(200):
        for f in rws:
            v = [math.log(g[(m, r)]) - b * (m - 1) for (m, r) in keys if r == f]
            a[f] = sum(v) / len(v)
        b = sum((math.log(g[(m, r)]) - a[r]) * (m - 1) for (m, r) in keys) / sum((m - 1) ** 2 for (m, r) in keys)
    smooth = {(k, f): math.exp(a[f] + b * (k - 1)) for k in range(1, 11) for f in rws if f != 22}
    def pick_smooth(lift, per_map, share):
        cost = {}
        for k in range(1, 11):
            H = health(k, lift, per_map, share); new = {}
            for f in sorted(set(f for (_, f) in smooth)):
                if f > LAST or (k == 10 and f != LAST): continue
                e = math.log(smooth[(k, f)] * H / TARGET[k - 1]) ** 2
                if k == 1:
                    if f == 15: new[f] = (e, [f])
                    continue
                prev = [(c, path) for pf, (c, path) in cost.items() if pf <= f <= pf + 3]
                if prev:
                    c, path = min(prev); new[f] = (c + e, path + [f])
            cost = new
        return min(cost.values())
    found = [(pick_smooth(l / 100, 0.08, sh / 1000), l / 100, sh / 1000) for l in range(200, 361, 5) for sh in (0, 5, 10, 15, 20)]
    top = min(f[0][0] for f in found)
    # near-ties (within 5% of the best error) go to the later rows: F1's point is the base game's own foes
    (err, rows), lift, share = max((f for f in found if f[0][0] <= 1.05 * top), key=lambda f: (sum(f[0][1]), -f[0][0]))
    best = (err, lift, share)
    print()
    print('Step 3, the pick: shapes smoothed (a map adds x%.3f), per-map step 0.08 kept, map 10 on row %d, rows, lift and share searched (error %.4f):' % (math.exp(b), LAST, err))
    report(smooth, rows, best[1], 0.08, best[2])
