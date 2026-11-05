"""Spool's setups (spoolsim.py runs them): stage A packages of the five numbers on one wall (and the old shut gap for
comparison), Smacky as the yardstick; stage B the Loom's speed under the chosen package.   python3 make_spool.py"""
import json, os
HERE = os.path.dirname(os.path.abspath(__file__))
os.makedirs(os.path.join(HERE, 'experiments'), exist_ok=True)
ONE = dict(seal=False, open_cushions=1, brk=None)
def cfg(name, v, m, **tune):
    t = dict(tune); t['break'] = 60
    return dict(name=name, v=v, m=m, p='smart', coins=False, n=15, tune=t)
# (Stitched Damage +, Dart Damage +, Crafted a Tin, Dart Range)
PKG = {
    'as built 2/2.5/4/3.5':  (0, 0, 4, 3.5),
    'P1-P3 3/3.5/6/4.5':     (1, 1, 6, 4.5),
    'P2-P3 3/3.5/6/3.5':     (1, 1, 6, 3.5),
    '4/4.5/6/4.5':           (2, 2, 6, 4.5),
    '3/3.5/8/4.5':           (1, 1, 8, 4.5),
    '4/4.5/8/4.5':           (2, 2, 8, 4.5),
    '5/5.5/6/4.5':           (3, 3, 6, 4.5),
    '5/5.5/8/4.5':           (3, 3, 8, 4.5),
}
A = []
for m in ('purlest', 'woozy'):
    A.append(cfg('smacky one wall %s' % m, 'smacky', m, seal=False))
    A.append(cfg('smacky shut %s' % m, 'smacky', m, seal=True))
    for name, (s, d, n, r) in PKG.items():
        A.append(cfg('spool one wall %s %s' % (m, name), 'spool', m, seal=False, open_cushions=1, stitched_dmg=s, dart_dmg=d, tin_units=n, dart_rng=r))
    for name in ('as built 2/2.5/4/3.5', 'P1-P3 3/3.5/6/4.5'):
        s, d, n, r = PKG[name]
        A.append(cfg('spool shut %s %s' % (m, name), 'spool', m, seal=True, stitched_dmg=s, dart_dmg=d, tin_units=n, dart_rng=r))
json.dump(A, open(os.path.join(HERE, 'experiments', 'spool_A.json'), 'w'), indent=1)
print(len(A), 'setups in spool_A')

# Stage B (after stage A: no package of the five numbers up to 5/5.5/8 carries Purlest on one wall, and the policy left
# the open Cushion alone): the wall kept (keep_wall), the packages past A's top, and the Loom's speed under two of them.
PKG_B = dict(PKG)
PKG_B.update({'6/6.5/6/4.5': (4, 4, 6, 4.5), '6/6.5/8/4.5': (4, 4, 8, 4.5)})
B = []
for m in ('purlest', 'woozy'):
    for name in ('as built 2/2.5/4/3.5', 'P1-P3 3/3.5/6/4.5', '4/4.5/6/4.5', '5/5.5/6/4.5', '6/6.5/6/4.5', '6/6.5/8/4.5'):
        s, d, n, r = PKG_B[name]
        B.append(cfg('spool kept wall %s %s' % (m, name), 'spool', m, seal=False, open_cushions=1, keep_wall=True, stitched_dmg=s, dart_dmg=d, tin_units=n, dart_rng=r))
    for name in ('4/4.5/6/4.5', '5/5.5/6/4.5'):
        s, d, n, r = PKG_B[name]
        for loom in (0.3, 0.2, 0.15):
            B.append(cfg('spool kept wall %s %s loom %.2f' % (m, name, loom), 'spool', m, seal=False, open_cushions=1, keep_wall=True, stitched_dmg=s, dart_dmg=d, tin_units=n, dart_rng=r, loom_aspd=loom))
json.dump(B, open(os.path.join(HERE, 'experiments', 'spool_B.json'), 'w'), indent=1)
print(len(B), 'setups in spool_B')

# Stage C (after B: the wall kept changes nothing; the gain stops at Stitched 5 / Dart 5.5; Dart Range 4.5 bought
# nothing in A; with one wall the Loom at 0.15 costs a third of a Tide): the pick, Stitched 5, Dart 5.5, 6 Crafted a Tin,
# Dart Range 3.5 as built, Loom 0.15, on all three maps, against as built, Smacky, and the pick in a shut gap.
PICK = dict(stitched_dmg=3, dart_dmg=3, tin_units=6, dart_rng=3.5, loom_aspd=0.15)
C = []
for m in ('purlest', 'woozy', 'thehunt'):
    C.append(cfg('smacky one wall %s' % m, 'smacky', m, seal=False))
    C.append(cfg('spool kept wall %s as built' % m, 'spool', m, seal=False, open_cushions=1, keep_wall=True))
    C.append(cfg('spool kept wall %s pick' % m, 'spool', m, seal=False, open_cushions=1, keep_wall=True, **PICK))
    C.append(cfg('spool kept wall %s pick, Loom as built' % m, 'spool', m, seal=False, open_cushions=1, keep_wall=True, **dict(PICK, loom_aspd=0.4)))
    C.append(cfg('spool shut %s pick' % m, 'spool', m, seal=True, **PICK))
json.dump(C, open(os.path.join(HERE, 'experiments', 'spool_C.json'), 'w'), indent=1)
print(len(C), 'setups in spool_C')

# Stage D (after C: at 0.15 the Loom costs Spool The Hunt, lost at Tide 2 against 5 with the Loom as built: on an
# Orange node it is Spool's Red for Tins): the pick with the Loom between, on The Hunt, and on Purlest and Woozy.
D = []
for m in ('thehunt', 'purlest', 'woozy'):
    for loom in ((0.3, 0.25, 0.2) if m != 'thehunt' else (0.3, 0.25, 0.2)):
        D.append(cfg('spool kept wall %s pick, Loom %.2f' % (m, loom), 'spool', m, seal=False, open_cushions=1, keep_wall=True, **dict(PICK, loom_aspd=loom)))
json.dump(D, open(os.path.join(HERE, 'experiments', 'spool_D.json'), 'w'), indent=1)
print(len(D), 'setups in spool_D')
