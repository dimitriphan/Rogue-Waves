"""Writes experiments/exp_A.json (the mode as built against the base game) and exp_B.json (the proposals, §5 of the report)."""
import json, os
HERE = os.path.dirname(os.path.abspath(__file__))
os.makedirs(os.path.join(HERE, 'experiments'), exist_ok=True)
VM = [('smacky', 'purlest'), ('smacky', 'woozy'), ('smacky', 'thehunt'), ('yossele', 'purlest'), ('yossele', 'woozy'),
      ('yossele', 'thehunt'), ('spool', 'purlest')]
A = []
for v, m in VM:
    A.append(dict(name='base %s %s' % (v, m), v=v, m=m, kind='base'))
    for k in range(1, 11):
        A.append(dict(name='mode %s %s map%d' % (v, m, k), v=v, m=m, kind='mode', map=k))
json.dump(A, open(os.path.join(HERE, 'experiments', 'exp_A.json'), 'w'), indent=1)
print(len(A), 'setups in exp_A')

# exp_B: the proposals (report section 5). F2 alone; F1 alone; the recommended set R = F1 + F2 + F3 + F4.
FINISH = 'lambda k: 15 + round(15 * (k - 1) / 9)'
CAND = {'F2': dict(compound=True), 'F1': dict(finish=FINISH),
        'R': dict(finish=FINISH, compound=True, mods_from=2, rotate=True)}
Bx = []
for v, m in VM[:3]:
    for k in range(1, 11):
        Bx.append(dict(name='R %s %s map%d' % (v, m, k), v=v, m=m, kind='mode', map=k, table=CAND['R'], cand='R'))
    for c in ('F2', 'F1'):
        for k in (1, 4, 7, 10):
            Bx.append(dict(name='%s %s %s map%d' % (c, v, m, k), v=v, m=m, kind='mode', map=k, table=CAND[c], cand=c))
for v, m in VM[3:5]:
    for k in (1, 4, 7, 10):
        Bx.append(dict(name='R %s %s map%d' % (v, m, k), v=v, m=m, kind='mode', map=k, table=CAND['R'], cand='R'))
json.dump(Bx, open(os.path.join(HERE, 'experiments', 'exp_B.json'), 'w'), indent=1)
print(len(Bx), 'setups in exp_B')

# exp_C: the starting set S = R + a map-1 lift (foe health x1.5 from map 1, still +8% a map, compounding) + the hoard
# term at 1% foe health per Cardinal, Trinket copy and Direction rank the party holds (power.py's mid case with the price
# ramp: ITEMS below).
ITEMS = [3, 8, 12, 16, 20, 24, 28, 32, 35, 36]
S = dict(finish=FINISH, compound=True, mods_from=2, rotate=True,
         health_curve='lambda k: (1.5 + 0.08 * (k - 1)) * (1 + 0.01 * %s[k - 1]) - 1' % ITEMS)
Cx = []
for v, m in VM[:3]:
    for k in range(1, 11):
        Cx.append(dict(name='S %s %s map%d' % (v, m, k), v=v, m=m, kind='mode', map=k, table=S, cand='S'))
for v, m in VM[3:5]:
    for k in (1, 4, 7, 10):
        Cx.append(dict(name='S %s %s map%d' % (v, m, k), v=v, m=m, kind='mode', map=k, table=S, cand='S'))
json.dump(Cx, open(os.path.join(HERE, 'experiments', 'exp_C.json'), 'w'), indent=1)
print(len(Cx), 'setups in exp_C')

# exp_D: S2, the starting set retuned after exp_C crossed the base game only at map 8: a bigger map-1 lift (foe health
# x2.5) and a gentler finish (map 10 ends on Downpour's Tide 27), F4 and F5 as in S.
FINISH2 = 'lambda k: 15 + round(12 * (k - 1) / 9)'
S2 = dict(finish=FINISH2, compound=True, mods_from=2, rotate=True,
          health_curve='lambda k: (2.5 + 0.08 * (k - 1)) * (1 + 0.01 * %s[k - 1]) - 1' % ITEMS)
Dx = [dict(name='S2 %s %s map%d' % (v, m, k), v=v, m=m, kind='mode', map=k, table=S2, cand='S2')
      for v, m in VM[:3] for k in range(1, 11)]
json.dump(Dx, open(os.path.join(HERE, 'experiments', 'exp_D.json'), 'w'), indent=1)
print(len(Dx), 'setups in exp_D')
