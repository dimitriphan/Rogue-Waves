"""Writes the re-run's experiment files (experiments/exp_K..P.json): python3 make_experiments.py [stage].

Stage 1 writes K (calibration against the balance round's PC runs) and L (baselines); stage 2 reads results/res_L.json
for each vessel's best Woozy node order and writes M (Spool's P1-P3 and design options), N (Yossele's P5 and P6),
O (the Opal's low reading) and P (The Hunt's aura beasts). Every setup has no Cardinals. K runs Downpour's own 30 s breaks,
as the PC played; the rest the mode's 60 s breaks.
"""
import itertools, json, os, sys

HERE = os.path.dirname(os.path.abspath(__file__))
EXP = os.path.join(HERE, 'experiments')
MAPS = ['purlest', 'woozy', 'thehunt']
VESSELS = ['smacky', 'yossele', 'spool']
WORLDS = {'shut': True, 'open': False}          # a gap the player has shut / the round's M5 line, walked round
P13 = dict(tin_units=6, stitched_dmg=1, dart_dmg=1)                       # P1-P3 as the PC played them (M7: Darts in the slam)
P13_BACK = dict(P13, dart_safe=True, dart_all=True, dart_n=4)             # P1-P3 with every Tin on Darts held back (the study's package)
P23 = dict(tin_units=6, stitched_dmg=1, dart_dmg=1, dart_n=0)             # P2 and P3 alone: no Darts, so Range is moot
ORDERS = [list(p) for p in itertools.permutations(['Red', 'Blue', 'Yellow'])]

def cfg(name, v, m, p='smart', tune=None, n=15, coins=False, brk=60):
    t = dict(tune or {})
    if brk: t['break'] = brk
    return dict(name=name, v=v, m=m, p=p, coins=coins, n=n, tune=t)

def stage1():
    K = []
    for model in ('round', 'study'):
        mt = {'model': model}
        for m in ('purlest', 'woozy'):
            for first, tf in (('tinfirst', {'tin_first': True}), ('wallfirst', {})):
                K.append(cfg('K-%s-spool-round-%s-%s' % (model, first, m), 'spool', m, 'round', dict(mt, **tf), brk=None))
                K.append(cfg('K-%s-spool-round-%s-%s-P13' % (model, first, m), 'spool', m, 'round', dict(mt, **tf, **P13), brk=None))
        K.append(cfg('K-%s-yossele-bot-woozy' % model, 'yossele', 'woozy', 'bot', mt, brk=None))
        # the 2026-10-26 play test (three Coin Cardinals on), as the first study's section 2
        K.append(cfg('K-%s-yossele-bot-purlest-coins-T8' % model, 'yossele', 'purlest', 'bot', mt, n=8, coins=True, brk=None))
        K.append(cfg('K-%s-yossele-bot-thehunt-coins-noore-T10' % model, 'yossele', 'thehunt', 'bot', dict(mt, ore_scale=0), n=10, coins=True, brk=None))
        K.append(cfg('K-%s-spool-bot-purlest-coins-T6' % model, 'spool', 'purlest', 'bot', mt, n=6, coins=True, brk=None))
    L = []
    for w, seal in WORLDS.items():
        for v in VESSELS:
            for m in MAPS:
                if m == 'woozy':
                    orders = ORDERS if v != 'smacky' else [o for o in ORDERS if o[1:] == sorted(o[1:])]  # one Hut: the first node only
                    for o in orders:
                        L.append(cfg('L-%s-%s-%s-%s' % (w, v, m, ''.join(x[0] for x in o)), v, m, 'smart', dict(seal=seal, smart_nodes=o)))
                else:
                    L.append(cfg('L-%s-%s-%s' % (w, v, m), v, m, 'smart', dict(seal=seal)))
    for v in ('yossele', 'spool'):
        for m in MAPS:
            L.append(cfg('L-bot-%s-%s' % (v, m), v, m, 'bot'))
    for m in MAPS:
        L.append(cfg('L-round-spool-%s' % m, 'spool', m, 'round', {'tin_first': True}))
    return dict(K=K, L=L)

def best_orders():
    """Each vessel's and world's best Woozy node order in L (highest mean Tide reached, then win share, then aura mined:
    a player whose orders tie keeps the richer one)."""
    res = json.load(open(os.path.join(HERE, 'results', 'res_L.json')))
    best = {}
    for s in res:
        parts = s['name'].split('-')
        if len(parts) == 5 and parts[3] == 'woozy':
            key = (parts[1], parts[2])
            score = (s['mean_t'], s['win'], sum(s['income_mine']))
            if key not in best or score > best[key][0]:
                best[key] = (score, s['cfg']['tune']['smart_nodes'])
    return {k: v[1] for k, v in best.items()}

def stage2():
    bo = best_orders()
    def smart(w, v, m, extra=None):
        t = dict(seal=WORLDS[w], **(extra or {}))
        if m == 'woozy': t['smart_nodes'] = bo[(w, v)]
        return t
    M = []
    for m in MAPS:
        for label, pk in (('P13', P13), ('P13back', P13_BACK), ('P23', P23)):
            for w in WORLDS:
                M.append(cfg('M-%s-spool-%s-%s' % (w, m, label), 'spool', m, 'smart', smart(w, 'spool', m, pk)))
            M.append(cfg('M-round-spool-%s-%s' % (m, label), 'spool', m, 'round', dict(tin_first=True, **pk)))
            M.append(cfg('M-bot-spool-%s-%s' % (m, label), 'spool', m, 'bot', dict(pk)))
        # design questions for a line foes walk round (not proposals): Stitched that block like a wall; Crafted that land
        # 4 hits in 10 on a walker; both with P1-P3
        for label, pk in (('block', dict(stitched_block=True)), ('chase04', dict(chase_miss=0.6)),
                          ('block+P13', dict(stitched_block=True, **P13)), ('chase04+P13', dict(chase_miss=0.6, **P13))):
            M.append(cfg('M-open-spool-%s-%s' % (m, label), 'spool', m, 'smart', smart('open', 'spool', m, pk)))
            M.append(cfg('M-round-spool-%s-%s' % (m, label), 'spool', m, 'round', dict(tin_first=True, **pk)))
    N = []
    for m in MAPS:
        for w in WORLDS:
            N.append(cfg('N-%s-yossele-%s-P5' % (w, m), 'yossele', m, 'smart', smart(w, 'yossele', m, {'sapling_aspd': 0.15})))
            for aspd in (0.25, 0.15):
                for k in (15, 5):
                    N.append(cfg('N-%s-yossele-%s-sap%s-fruit%d' % (w, m, str(aspd).replace('.', ''), k), 'yossele', m, 'smart',
                                 smart(w, 'yossele', m, dict(sapling_aspd=aspd, fruit_pickup=0.5, fruit_k=k))))
        N.append(cfg('N-bot-yossele-%s-P5' % m, 'yossele', m, 'bot', {'sapling_aspd': 0.15}))
    O = []
    for w in WORLDS:
        for v in VESSELS:
            O.append(cfg('O-%s-%s-purlest-opalcolour' % (w, v), v, 'purlest', 'smart', smart(w, v, 'purlest', {'opal': 'colour'})))
        O.append(cfg('O-%s-spool-purlest-opalcolour-P13' % w, 'spool', 'purlest', 'smart', smart(w, 'spool', 'purlest', dict(opal='colour', **P13))))
        O.append(cfg('O-%s-spool-purlest-opalcolour-P13back' % w, 'spool', 'purlest', 'smart', smart(w, 'spool', 'purlest', dict(opal='colour', **P13_BACK))))
        O.append(cfg('O-%s-yossele-purlest-opalcolour-P5' % w, 'yossele', 'purlest', 'smart', smart(w, 'yossele', 'purlest', dict(opal='colour', sapling_aspd=0.15))))
    O.append(cfg('O-round-spool-purlest-opalcolour', 'spool', 'purlest', 'round', dict(opal='colour', tin_first=True)))
    O.append(cfg('O-round-spool-purlest-opalcolour-P13', 'spool', 'purlest', 'round', dict(opal='colour', tin_first=True, **P13)))
    P = []
    for b in (30, 105):
        for w in WORLDS:
            for v in VESSELS:
                P.append(cfg('P-%s-%s-thehunt-beasts%d' % (w, v, b), v, 'thehunt', 'smart', smart(w, v, 'thehunt', {'beasts': b})))
            P.append(cfg('P-%s-spool-thehunt-beasts%d-P13' % (w, b), 'spool', 'thehunt', 'smart', smart(w, 'spool', 'thehunt', dict(beasts=b, **P13))))
    return dict(M=M, N=N, O=O, P=P)

if __name__ == '__main__':
    stage = sys.argv[1] if len(sys.argv) > 1 else '1'
    os.makedirs(EXP, exist_ok=True)
    for k, v in (stage1() if stage == '1' else stage2()).items():
        json.dump(v, open(os.path.join(EXP, 'exp_%s.json' % k), 'w'), indent=0)
        print(k, len(v), 'setups')
