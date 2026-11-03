"""What one gatherer mines a Tide on each node the balance round measured: python3 mining.py [out.md].

Analytic, no simulation. A Tide here is the 180 s Tide and the mode's 60 s break (240 s), as in ../balance_2026-11-03/aura.py.
Each node's aura a Damage-1 strike is sim.NODES (the round's M2, M3); a striker of Damage 0.5 (the Mining Hut's miner)
earns half a strike's aura a hit, as the first study had it. Writes results/mining.json.
"""
import json, os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from sim import NODES, opal_strike

HERE = os.path.dirname(os.path.abspath(__file__))
CYCLE = 240.0

# name, strikes a second, aura a strike (1 = a Damage-1 strike), cost (R, B, Y)
GATHERERS = [
    ('Smacky: Mining Hut (the code\'s 6.8 s round trip)', 1 / 6.8, 0.5, (0, 0, 35)),
    ('Yossele: Pachira Sapling, new (Attackspeed 0.25)', 0.25, 1.0, (0, 0, 35)),
    ('Yossele: Sapling, fully grown (0.375)', 0.375, 1.0, (0, 0, 35)),
    ('Yossele: Sapling at P5, new (0.15)', 0.15, 1.0, (0, 0, 35)),
    ('Yossele: Sapling at P5, fully grown (0.225)', 0.225, 1.0, (0, 0, 35)),
    ('Spool: Loom (0.4)', 0.4, 1.0, (0, 0, 40)),
    ('Spool: Loom with its Seam (0.4 + 0.8)', 1.2, 1.0, (0, 0, 70)),
    ('Any vessel by hand, idle by the node (0.5)', 0.5, 1.0, (0, 0, 0)),
]

def per_strike(node, reading='full'):
    """(R, B, Y) a Damage-1 strike pays, averaged over a 4-Tide cycle."""
    if node.get('opal'):
        tot = [0.0, 0.0, 0.0]
        for t in range(1, 5):
            p = opal_strike(t, reading)[0]
            tot = [tot[i] + p[i] / 4 for i in range(3)]
        return tuple(tot)
    return node['pay']

def tables():
    rows = [(m, n) for m, ns in NODES.items() for n in ns]
    names = ['%s %s' % ({'purlest': 'Purlest', 'woozy': 'Woozy', 'thehunt': 'The Hunt'}[m], n['name']) for m, n in rows]
    out = ['| Gatherer | ' + ' | '.join(names) + ' | Opal, Tide colour only |', '|---' * (len(names) + 2) + '|']
    data = {}
    for g, rate, frac, cost in GATHERERS:
        cells = []
        for (m, n), nm in zip(rows, names):
            a = sum(per_strike(n)) * rate * frac * CYCLE
            data['%s|%s' % (g, nm)] = round(a)
            cells.append('%d' % round(a))
        low = sum(per_strike(NODES['purlest'][0], 'colour')) * rate * frac * CYCLE
        out.append('| %s | %s | %d |' % (g, ' | '.join(cells), round(low)))
    opal = ['| Opal, a Damage-1 strike | ' + ' | '.join('Tide %d: R %.1f B %.1f Y %.1f' % ((t,) + opal_strike(t)[0]) for t in range(1, 5)) + ' |']
    return '\n'.join(out + [''] + opal) + '\n', data

if __name__ == '__main__':
    md, data = tables()
    print(md)
    os.makedirs(os.path.join(HERE, 'results'), exist_ok=True)
    json.dump(data, open(os.path.join(HERE, 'results', 'mining.json'), 'w'), indent=1)
    if len(sys.argv) > 1:
        open(sys.argv[1], 'w').write(md)
