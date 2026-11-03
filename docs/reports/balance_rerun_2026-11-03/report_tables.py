"""The re-run report's tables from the result files: python3 report_tables.py [K L M N O P ...] (default: every one found)."""
import json, os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from summ import cell, load

HERE = os.path.dirname(os.path.abspath(__file__))
R = lambda *ks: load(*[os.path.join(HERE, 'results', 'res_%s.json' % k) for k in ks if os.path.exists(os.path.join(HERE, 'results', 'res_%s.json' % k))])
MAPS = [('purlest', 'Purlest'), ('woozy', 'Woozy'), ('thehunt', 'The Hunt')]
V = [('smacky', 'Smacky'), ('yossele', 'Yossele'), ('spool', 'Spool')]

def c(d, name):
    return cell(d[name]) if name in d else '-'

def K():
    d = R('K')
    rows = [('Spool, Purlest, Tin first, as written', 'lost T4, T4', 'spool-round-tinfirst-purlest'),
            ('... with P1-P3', 'lost T5, T5', 'spool-round-tinfirst-purlest-P13'),
            ('Spool, Woozy, Tin first, as written', 'lost T2', 'spool-round-tinfirst-woozy'),
            ('... with P1-P3', 'lost T2', 'spool-round-tinfirst-woozy-P13'),
            ('Spool, Purlest, wall first, as written', 'lost T1, T1', 'spool-round-wallfirst-purlest'),
            ('... with P1-P3', 'lost T1, T2', 'spool-round-wallfirst-purlest-P13'),
            ('Spool, Woozy, wall first, as written', 'lost T2', 'spool-round-wallfirst-woozy'),
            ('... with P1-P3', 'lost T1', 'spool-round-wallfirst-woozy-P13'),
            ('Yossele, Woozy, the PC\'s virtual player', 'lost T14', 'yossele-bot-woozy'),
            ('2026-10-26: Yossele, Purlest, Coins, to Tide 8', 'clean', 'yossele-bot-purlest-coins-T8'),
            ('2026-10-26: Yossele, The Hunt, Coins, no ore, to Tide 10', 'lost T7', 'yossele-bot-thehunt-coins-noore-T10'),
            ('2026-10-26: Spool, Purlest, Coins, to Tide 6', 'lost T4, T1, T2', 'spool-bot-purlest-coins-T6')]
    out = ['| Run | The game | First study\'s model | Re-run model |', '|---|---|---|---|']
    for label, game, k in rows:
        out.append('| %s | %s | %s | %s |' % (label, game, c(d, 'K-study-' + k), c(d, 'K-round-' + k)))
    return '\n'.join(out)

def best(d, w, v, m):
    if m != 'woozy': return d.get('L-%s-%s-%s' % (w, v, m))
    cands = [s for n, s in d.items() if n.startswith('L-%s-%s-woozy-' % (w, v))]
    return max(cands, key=lambda s: (s['mean_t'], s['win'], sum(s['income_mine']))) if cands else None

def L():
    d = R('L')
    out = ['| Player | ' + ' | '.join(mn for _, mn in MAPS) + ' |', '|---|---|---|---|']
    for w, wl in (('shut', 'shut gap'), ('open', 'open line')):
        for v, vl in V:
            cells = []
            for m, _ in MAPS:
                s = best(d, w, v, m)
                x = cell(s) if s else '-'
                if m == 'woozy' and s: x += ' (%s)' % '-'.join(s['cfg']['tune']['smart_nodes'][:2 if v != 'smacky' else 1])
                cells.append(x)
            out.append('| %s, smart, %s | %s |' % (vl, wl, ' | '.join(cells)))
    for v, vl in V[1:]:
        out.append('| %s, bot | %s |' % (vl, ' | '.join(c(d, 'L-bot-%s-%s' % (v, m)) for m, _ in MAPS)))
    out.append('| Spool, the PC harness (Tin first) | %s |' % ' | '.join(c(d, 'L-round-spool-%s' % m) for m, _ in MAPS))
    return '\n'.join(out)

def L_orders():
    d = R('L')
    out = ['| Woozy, gatherers by | ' + ' | '.join('%s %s' % (vl, w) for w in ('shut', 'open') for _, vl in V) + ' |', '|---' * 7 + '|']
    for o in ('RBY', 'RYB', 'BRY', 'BYR', 'YRB', 'YBR'):
        cells = []
        for w in ('shut', 'open'):
            for v, _ in V:
                cells.append(c(d, 'L-%s-%s-woozy-%s' % (w, v, o)))
        out.append('| %s | %s |' % ('-'.join({'R': 'Red', 'B': 'Blue', 'Y': 'Yellow'}[x] for x in o[:2]), ' | '.join(cells)))
    return '\n'.join(out)

def money():
    d = R('L')
    out = ['| Smart player, shut gap | ' + ' | '.join('%s: mined R/B/Y | left at the end' % mn for _, mn in MAPS) + ' |', '|---' * 7 + '|']
    for v, vl in V:
        cells = []
        for m, _ in MAPS:
            s = best(d, 'shut', v, m)
            cells.append('%d / %d / %d | %d / %d / %d' % (tuple(s['income_mine']) + tuple(s['wallet_end'])))
        out.append('| %s | %s |' % (vl, ' | '.join(cells)))
    return '\n'.join(out)

def M():
    d = R('L', 'M')
    out = []
    for w, wl, p in (('shut', 'smart, shut gap', 'M-shut-spool-%s-%s'), ('open', 'smart, open line', 'M-open-spool-%s-%s'),
                     ('round', 'the PC harness, Tin first', 'M-round-spool-%s-%s'), ('bot', 'bot', 'M-bot-spool-%s-%s')):
        base = {'shut': lambda m: best(d, 'shut', 'spool', m), 'open': lambda m: best(d, 'open', 'spool', m),
                'round': lambda m: d.get('L-round-spool-%s' % m), 'bot': lambda m: d.get('L-bot-spool-%s' % m)}[w]
        for label, key in (('as written', None), ('P1-P3, Darts where the Stitched stand (as the PC played)', 'P13'),
                           ('P1-P3, every Tin on Darts held back out of the slam', 'P13back'), ('P2 and P3 only (no Darts)', 'P23')):
            cells = []
            for m, _ in MAPS:
                s = base(m) if key is None else d.get(p % (m, key))
                cells.append(cell(s) if s else '-')
            out.append('| %s | %s | %s |' % (wl, label, ' | '.join(cells)))
    return '| Spool | Setup | Purlest | Woozy | The Hunt |\n|---|---|---|---|---|\n' + '\n'.join(out)

def M_design():
    d = R('M')
    out = ['| Open line: design question (not a proposal) | Player | Purlest | Woozy | The Hunt |', '|---|---|---|---|---|']
    for key, label in (('block', 'Stitched block foes like a wall'), ('chase04', 'Crafted land 4 hits in 10 on a walker (1 now)'),
                       ('block+P13', 'Stitched block, with P1-P3'), ('chase04+P13', '4 in 10, with P1-P3')):
        for pl, p in (('smart', 'M-open-spool-%s-%s'), ('PC harness', 'M-round-spool-%s-%s')):
            out.append('| %s | %s | %s |' % (label, pl, ' | '.join(c(d, p % (m, key)) for m, _ in MAPS)))
    return '\n'.join(out)

def N():
    d = R('L', 'N')
    out = ['| Yossele | Purlest | Woozy | The Hunt |', '|---|---|---|---|']
    for w, wl in (('shut', 'shut gap'), ('open', 'open line')):
        out.append('| Smacky, smart, %s (yardstick) | %s |' % (wl, ' | '.join(cell(best(d, w, 'smacky', m)) for m, _ in MAPS)))
        out.append('| smart, %s, Sapling 0.25 (as written) | %s |' % (wl, ' | '.join(cell(best(d, w, 'yossele', m)) for m, _ in MAPS)))
        out.append('| smart, %s, Sapling 0.15 (P5) | %s |' % (wl, ' | '.join(c(d, 'N-%s-yossele-%s-P5' % (w, m)) for m, _ in MAPS)))
        for aspd, al in (('025', '0.25'), ('015', '0.15')):
            for k in (15, 5):
                out.append('| smart, %s, Sapling %s, half the fruit picked up, fruit x %d%s | %s |' % (
                    wl, al, k, ' (P6)' if k == 5 else '', ' | '.join(c(d, 'N-%s-yossele-%s-sap%s-fruit%d' % (w, m, aspd, k)) for m, _ in MAPS)))
    out.append('| bot, Sapling 0.25 (as written) | %s |' % ' | '.join(c(d, 'L-bot-yossele-%s' % m) for m, _ in MAPS))
    out.append('| bot, Sapling 0.15 (P5) | %s |' % ' | '.join(c(d, 'N-bot-yossele-%s-P5' % m) for m, _ in MAPS))
    return '\n'.join(out)

def O():
    d = R('L', 'O')
    out = ['| Purlest | Opal as measured (every colour a payout) | The Tide\'s colour only |', '|---|---|---|']
    for w, wl in (('shut', 'shut gap'), ('open', 'open line')):
        for v, vl in V:
            out.append('| %s, smart, %s | %s | %s |' % (vl, wl, cell(best(d, w, v, 'purlest')), c(d, 'O-%s-%s-purlest-opalcolour' % (w, v))))
        out.append('| Spool, smart, %s, P1-P3 as played | %s | %s |' % (wl, c(R('M'), 'M-%s-spool-purlest-P13' % w), c(d, 'O-%s-spool-purlest-opalcolour-P13' % w)))
        out.append('| Spool, smart, %s, P1-P3, Darts held back | %s | %s |' % (wl, c(R('M'), 'M-%s-spool-purlest-P13back' % w), c(d, 'O-%s-spool-purlest-opalcolour-P13back' % w)))
        out.append('| Yossele, smart, %s, P5 | %s | %s |' % (wl, c(R('N'), 'N-%s-yossele-purlest-P5' % w), c(d, 'O-%s-yossele-purlest-opalcolour-P5' % w)))
    out.append('| Spool, the PC harness | %s | %s |' % (c(d, 'L-round-spool-purlest'), c(d, 'O-round-spool-purlest-opalcolour')))
    out.append('| ... with P1-P3 | %s | %s |' % (c(R('M'), 'M-round-spool-purlest-P13'), c(d, 'O-round-spool-purlest-opalcolour-P13')))
    return '\n'.join(out)

def P():
    d = R('L', 'P', 'M')
    out = ['| The Hunt | no beasts hunted | 30 of each colour a Tide | 105 of each a Tide |', '|---|---|---|---|']
    for w, wl in (('shut', 'shut gap'), ('open', 'open line')):
        for v, vl in V:
            out.append('| %s, smart, %s | %s | %s | %s |' % (vl, wl, cell(best(d, w, v, 'thehunt')), c(d, 'P-%s-%s-thehunt-beasts30' % (w, v)), c(d, 'P-%s-%s-thehunt-beasts105' % (w, v))))
        out.append('| Spool, smart, %s, P1-P3 | %s | %s | %s |' % (wl, c(d, 'M-%s-spool-thehunt-P13' % w), c(d, 'P-%s-spool-thehunt-beasts30-P13' % w), c(d, 'P-%s-spool-thehunt-beasts105-P13' % w)))
    return '\n'.join(out)

if __name__ == '__main__':
    which = sys.argv[1:] or ['K', 'L', 'L_orders', 'money', 'M', 'M_design', 'N', 'O', 'P']
    for w in which:
        print('###', w); print(globals()[w]()); print()
