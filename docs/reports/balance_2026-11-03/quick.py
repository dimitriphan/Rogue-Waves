"""Eight seeds of each tuning, for a quick look: python3 quick.py spool woozy '{}' '{"tin_units":6}'."""
import sys, json, statistics as st
import os; sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from sim import Run
def q(v, m, tune, seeds=range(1, 9), p='smart', n=15):
    lost = []
    for s in seeds:
        r = Run(v, m, 'downpour', p, tune=tune, seed=s, tides=n).play()
        lost.append(r['lost'] or 99)
    return lost
if __name__ == '__main__':
    v, m = sys.argv[1], sys.argv[2]
    for t in sys.argv[3:]:
        L = q(v, m, json.loads(t))
        print(m, t, 'won %d/%d' % (sum(1 for x in L if x == 99), len(L)), 'lost at', sorted(x for x in L if x != 99))
