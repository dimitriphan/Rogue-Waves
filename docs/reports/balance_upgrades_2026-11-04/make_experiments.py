"""Writes experiments/exp_U.json, the 2026-11-04 upgrade study (../UPGRADE_GAPS_2026-11-04.md): python3 make_experiments.py

Each proposed upgrade package bought by Spool's smart player (policies.SpoolPkg) and Wood Wide Web by Yossele's
(policies.YosseleWeb), on Purlest, Woozy and The Hunt, with the gap shut and with the line walked round, no Cardinals,
the mode's 60 s breaks. Woozy uses each player's best node order from the re-run (../balance_rerun_2026-11-03, res_L).
"""
import json, os

HERE = os.path.dirname(os.path.abspath(__file__))
MAPS = ['purlest', 'woozy', 'thehunt']
WORLDS = {'shut': True, 'open': False}
BEST = {('shut', 'spool'): ['Yellow', 'Blue', 'Red'], ('open', 'spool'): ['Yellow', 'Red', 'Blue'],
        ('shut', 'yossele'): ['Yellow', 'Blue', 'Red'], ('open', 'yossele'): ['Red', 'Blue', 'Yellow']}
ALL = ['Tacking', 'Cross', 'Army', 'Needle', 'Stitchtime', 'Class', 'Thimble', 'Voodoo', 'Dyed', 'Winder', 'QuickPatch']
SPOOL = {
    'base': ([], {}),
    'tack': (['Tacking'], {}),
    'dmg': (['Cross', 'Needle', 'Stitchtime', 'Class'], {}),
    'wall': (['Thimble', 'Voodoo'], {}),
    'blue': (['Dyed', 'QuickPatch'], {}),
    'blue50': (['Dyed', 'QuickPatch'], {'dye_eff': 0.5}),
    'thread': (['Winder', 'Bobbin', 'Count'], {}),
    'army': (['Army'], {}),
    'all': (ALL, {}),
    'all50': (ALL, {'dye_eff': 0.5}),
    'allnotack': ([x for x in ALL if x != 'Tacking'], {}),
}
YOSSELE = {'base': {}, 'web': {'web': True}, 'web2': {'web': True, 'web_every': 2.0}}

def cfg(name, v, m, tune):
    t = dict(tune); t['break'] = 60
    return dict(name=name, v=v, m=m, p='smart', coins=False, n=15, tune=t)

U = []
for w, seal in WORLDS.items():
    for m in MAPS:
        for k, (pkg, extra) in SPOOL.items():
            t = dict(seal=seal, **extra)
            if pkg: t['pkg'] = pkg
            if m == 'woozy': t['smart_nodes'] = BEST[(w, 'spool')]
            U.append(cfg('U-%s-spool-%s-%s' % (w, m, k), 'spool', m, t))
        for k, extra in YOSSELE.items():
            t = dict(seal=seal, **extra)
            if m == 'woozy': t['smart_nodes'] = BEST[(w, 'yossele')]
            U.append(cfg('U-%s-yossele-%s-%s' % (w, m, k), 'yossele', m, t))
os.makedirs(os.path.join(HERE, 'experiments'), exist_ok=True)
json.dump(U, open(os.path.join(HERE, 'experiments', 'exp_U.json'), 'w'), indent=0)
print(len(U), 'setups')
