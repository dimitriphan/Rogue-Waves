"""Markdown tables from result files: python3 summ.py results/res_B.json."""
import json, sys
def lost_str(s):
    h = s['lost_hist']
    if not h: return '-'
    tot = sum(h.values())
    top = sorted(h.items(), key=lambda kv: -kv[1])[:2]
    return ', '.join('T%s %d%%' % (k, round(100 * v / s['n'])) for k, v in top)
def row(s):
    return '| %s | %d%% | %d%% | %d%% | %d%% | %s | %s | %s | %s |' % (
        s['name'], round(100 * s['win']), round(100 * s['s4']), round(100 * s['s8']), round(100 * s['s10']),
        s['med_lost'] if s['med_lost'] else '-', lost_str(s), s['spent'], s['wallet_end'])
for f in sys.argv[1:]:
    print('###', f)
    print('| setup | win | alive T4 | T8 | T10 | median loss | losses | spent RBY | left RBY |')
    print('|---|---|---|---|---|---|---|---|---|')
    for s in json.load(open(f)): print(row(s))
