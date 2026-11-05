"""The sweep's setups, run with the review's unchanged w50.py (../mode_balance_2026-11-05/):

    python3 make_experiments.py
    W50_HI=48 python3 ../mode_balance_2026-11-05/w50.py experiments/exp_E1.json results/res_E1.json 12 7
    W50_HI=48 python3 ../mode_balance_2026-11-05/w50.py experiments/exp_E2.json results/res_E2.json 16 8

exp_E1 (rows): Smacky on Purlest, Woozy and The Hunt for each map with Tide 15 on a few of Downpour's rows around set
S2's F(k), every other proposal as S2 has it (F2, F4, F3 x2.5 + 8% a map, F5 1% an item held at the map's start, from
power2.py's mid case with C1's ramp). W50 is close to proportional to foe health, so each row's "shape" g = asked /
health, and the health curve is then fitted to the target with the rows that give a smooth g (fit.py).

exp_E2 (the pick): the rows and the health curve fit.py chose (PICK below, written by hand from its printout), Smacky's
three maps with 16 seeds, and Yossele's and Spool's checks at maps 1, 4, 7 and 10 (their base W50 is past the ceiling).

exp_E3 (Argo, argo/w50_argo.py): the review's reference set had no Argo; the Argo review's bot on Purlest, in the base
game and on the pick's maps 1, 5 and 10.
    W50_HI=48 python3 argo/w50_argo.py experiments/exp_E3.json results/res_E3.json 8 7

exp_E4 (the share at 2%): E2 measured the pick's end low (D 1.30 on map 10 against 1.44 fitted), so F5's share goes from
1.5% to 2% an item, the knob that follows the hoard; Smacky's three maps again on maps 2, 5, 8, 9 and 10.
    W50_HI=48 python3 ../mode_balance_2026-11-05/w50.py experiments/exp_E4.json results/res_E4.json 16 8
"""
import json, os
HERE = os.path.dirname(os.path.abspath(__file__))
os.makedirs(os.path.join(HERE, 'experiments'), exist_ok=True)
POWER = json.load(open(os.path.join(HERE, 'results', 'power.json')))
ITEMS = [r['items'] for r in POWER['mid_ramp']]
SMACKY = [('smacky', 'purlest'), ('smacky', 'woozy'), ('smacky', 'thehunt')]
S2_ROWS = [15, 16, 18, 19, 20, 22, 23, 24, 26, 27]

def health(lift, per_map, share, items=ITEMS):
    return 'lambda k: (%s + %s * (k - 1)) * (1 + %s * %s[k - 1]) - 1' % (lift, per_map, share, items)

def table(rows, lift=2.5, per_map=0.08, share=0.01):
    return dict(finish='lambda k: %s[k - 1]' % rows, compound=True, mods_from=2, rotate=True,
                health_curve=health(lift, per_map, share))

TRY = {1: [15], 2: [16, 17], 3: [18, 19, 20], 4: [19, 20, 21], 5: [20, 21], 6: [21, 22, 23], 7: [23, 24, 25],
       8: [24, 25], 9: [25, 26], 10: [25, 26, 27]}
E1 = []
for k, rows in TRY.items():
    for f in rows:
        r = list(S2_ROWS); r[k - 1] = f
        for v, m in SMACKY:
            E1.append(dict(name='E1 %s %s map%d row%d' % (v, m, k, f), v=v, m=m, kind='mode', map=k, row=f, table=table(r), cand='E1'))
json.dump(E1, open(os.path.join(HERE, 'experiments', 'exp_E1.json'), 'w'), indent=1)
print(len(E1), 'setups in exp_E1')

# The pick (fit.py, step 3): Tide 15's row by map, and the health curve's lift, per-map step and share.
PICK = dict(rows=[15, 18, 20, 21, 21, 24, 24, 24, 24, 24], lift=2.6, per_map=0.08, share=0.015)
if os.environ.get('PICK'):
    PICK = json.loads(os.environ['PICK'])
P = table(PICK['rows'], PICK['lift'], PICK['per_map'], PICK['share'])
E2 = []
for v, m in SMACKY:
    for k in range(1, 11):
        E2.append(dict(name='E2 %s %s map%d' % (v, m, k), v=v, m=m, kind='mode', map=k, table=P, cand='E2'))
# Spool with its new numbers on one Pin Cushion (spool/make_spool.py's pick; Dart Range 3.5 is the sim's own)
SPOOL = dict(stitched_dmg=3, dart_dmg=3, tin_units=6, loom_aspd=0.3, seal=False, open_cushions=1)
for v, m, tune in [('yossele', 'purlest', {}), ('spool', 'purlest', SPOOL)]:
    for k in (1, 4, 7, 10):
        E2.append(dict(name='E2 %s %s map%d' % (v, m, k), v=v, m=m, kind='mode', map=k, table=P, cand='E2', tune=tune))
json.dump(E2, open(os.path.join(HERE, 'experiments', 'exp_E2.json'), 'w'), indent=1)
print(len(E2), 'setups in exp_E2', PICK)

E3 = [dict(name='E3 base argo purlest', v='argo', m='purlest', kind='base', cand='E3')]
for k in (1, 5, 10):
    E3.append(dict(name='E3 argo purlest map%d' % k, v='argo', m='purlest', kind='mode', map=k, table=P, cand='E3'))
json.dump(E3, open(os.path.join(HERE, 'experiments', 'exp_E3.json'), 'w'), indent=1)
print(len(E3), 'setups in exp_E3')

FINAL = dict(PICK, share=0.02)
P4 = table(FINAL['rows'], FINAL['lift'], FINAL['per_map'], FINAL['share'])
E4 = [dict(name='E4 %s %s map%d' % (v, m, k), v=v, m=m, kind='mode', map=k, table=P4, cand='E4') for v, m in SMACKY for k in (2, 5, 8, 9, 10)]
json.dump(E4, open(os.path.join(HERE, 'experiments', 'exp_E4.json'), 'w'), indent=1)
print(len(E4), 'setups in exp_E4', FINAL)
