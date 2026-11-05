"""Static foe pressure: a Tide's total health (count x health x elite and map multipliers) from the same tables mode.py
builds, with no simulation. Prints Downpour's Tides 1-30 and the mode's Tide 15 and Tides 1-15 sum, maps 1-10, with the
health added (as built) and compounded (proposal F2), and the share of a Tide's health that is on an elite."""
import sys, json
import mode, sim

def tide_hp(t):
    tot = elite = 0.0
    for s in t['steps']:
        if s[0] != 'spread': continue
        _, kind, n, every, el = s
        e = sim.ELITE.get(el, {}) if el else {}
        hp = sim.ENEMY[kind]['hp'] * e.get('hp', 1)
        tot += n * hp
        base_el = el.split('|')[0] if el and '|' in el else el
        if base_el: elite += n * hp
    return tot, elite

def main(out=None):
    import tables
    base, _ = tables.TABLES['downpour']
    rows = {'base': [round(tide_hp(t)[0]) for t in base]}
    share = [round(tide_hp(t)[1] / max(1, tide_hp(t)[0]), 2) for t in base]
    print('Downpour Tide health:', rows['base'])
    print('elite share:', share)
    for comp in (False, True):
        for k in range(1, 11):
            t, _ = mode.mode_table(k, compound=comp)
            hp = [round(tide_hp(x)[0]) for x in t[:15]]
            rows['map%d%s' % (k, 'c' if comp else '')] = hp
            print('map', k, 'compound' if comp else 'added', 'T15', hp[14], 'T1-15 sum', sum(hp), 'x base T1-15', round(sum(hp) / sum(rows['base'][:15]), 2))
    if out: json.dump(dict(rows=rows, elite_share=share), open(out, 'w'), indent=1)

if __name__ == '__main__':
    main(*sys.argv[1:])
