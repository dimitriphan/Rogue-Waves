"""Markdown tables for the report from the results: W50 against the base game per map, and the effective difficulty
D = asked / carried (1 = the base game; above 1 harder).   python3 summary.py"""
import json, os, math
import chart
R = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'results')
A = json.load(open(os.path.join(R, 'res_A.json')))
PW = json.load(open(os.path.join(R, 'power.json')))
def load(n):
    p = os.path.join(R, n)
    return json.load(open(p)) if os.path.exists(p) else []
B, C, D = load('res_B.json'), load('res_C.json'), load('res_D.json')
asked = {'as built': chart.pressure(A)}
for cand, res in (('F2', B), ('F1', B), ('R', B), ('S', C), ('S2', D)):
    if res: asked[cand] = chart.pressure(res, cand=cand)
carried = {k: [r['power'] for r in PW[k]] for k in PW}
def f(v): return '' if v is None else '%.2f' % v
print('| Map | ' + ' | '.join('asked %s' % k for k in asked) + ' | carried low / mid / high | carried mid, price ramp |')
print('|' + ' --- |' * (len(asked) + 3))
for k in range(10):
    print('| %d | ' % (k + 1) + ' | '.join(f(asked[c][k]) for c in asked) + ' | %.2f / %.2f / %.2f | %.2f |' % (carried['low'][k], carried['mid'][k], carried['high'][k], carried['mid_ramp'][k]))
print()
print('Effective difficulty D = asked / carried (mid); 1 = the base game')
print('| Map | as built | R | S | S2 | S2, low power | S2, high power |')
print('| --- | --- | --- | --- | --- | --- | --- |')
for k in range(10):
    row = [asked['as built'][k] / carried['mid'][k]]
    for c in ('R', 'S'):
        row.append(asked[c][k] / carried['mid_ramp'][k] if c in asked and asked[c][k] else None)
    for case in ('mid_ramp', 'low_ramp', 'high_ramp'):
        row.append(asked['S2'][k] / carried[case][k] if 'S2' in asked and asked['S2'][k] else None)
    print('| %d | ' % (k + 1) + ' | '.join(f(v) for v in row) + ' |')
print()
print('Per setup (W50; base W50 in brackets)')
for res in (A, B, C, D):
    for r in res:
        if r['kind'] == 'mode' and r['map'] in (1, 4, 7, 10):
            b = chart.BASE.get((r['v'], r['m']))
            print(r['name'], r['w50'], '(%s)' % b, 'win at power 1: %.2f' % r['win'])
