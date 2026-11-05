"""The report's two charts, as SVG with their own light and dark colours (the dataviz reference palette's first three
slots, validated all-pairs in both modes; aqua is under 3:1 on the light surface, so every series is labelled at its
end and the report's tables are the table view) and a <title> tooltip on every point.

    python3 charts.py      writes difficulty_by_map.svg and spool_one_wall.svg

difficulty_by_map.svg: D = asked / carried (mid case, with C1's ramp from power2.py) by map, 1 = the base game, for the
mode as built (the review's runs), the fit with F5 at 1.5% (exp_E2, every map) and the pick with F5 at 2% (exp_E4, maps
2, 5, 8, 9 and 10), over the target band.
spool_one_wall.svg: the Tide Spool reaches with one Pin Cushion (stage B, the wall kept), by package, on Purlest and
Woozy, against Smacky's (stage A).
"""
import json, math, os
HERE = os.path.dirname(os.path.abspath(__file__))
REVIEW = os.path.join(HERE, '..', 'mode_balance_2026-11-05')
LIGHT = dict(surface='#fcfcfb', text='#0b0b0b', text2='#52514e', grid='#e4e3df', s1='#2a78d6', s2='#eb6834', s3='#1baf7a', band='#2a78d6')
DARK = dict(surface='#1a1a19', text='#f0efec', text2='#c3c2b7', grid='#3a3936', s1='#3987e5', s2='#d95926', s3='#199e70', band='#3987e5')
FONT = 'system-ui, -apple-system, Segoe UI, sans-serif'

def head(w, h, title, desc):
    css = ''.join('--%s:%s;' % kv for kv in LIGHT.items()); dcss = ''.join('--%s:%s;' % kv for kv in DARK.items())
    return ['<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 %d %d" width="%d" height="%d" font-family="%s" role="img" aria-labelledby="t d">' % (w, h, w, h, FONT),
            '<title id="t">%s</title>' % title, '<desc id="d">%s</desc>' % desc,
            '<style>svg{%s} @media (prefers-color-scheme: dark){svg{%s}} .t{fill:var(--text)} .t2{fill:var(--text2)} .g{stroke:var(--grid)}</style>' % (css, dcss),
            '<rect width="100%" height="100%" fill="var(--surface)"/>']

def gmean(vs): return math.exp(sum(math.log(v) for v in vs) / len(vs))
BASE = {(r['v'], r['m']): r['w50'] for r in json.load(open(os.path.join(REVIEW, 'results', 'res_A.json'))) if r['kind'] == 'base'}
def asked(res, cand=None):
    """{map: asked} for the maps the runs measured."""
    by = {}
    for r in res:
        if r['kind'] == 'mode' and r['v'] == 'smacky' and (cand is None or r.get('cand') == cand):
            by.setdefault(r['map'], []).append(r['w50'] / BASE[(r['v'], r['m'])])
    return {k: gmean(v) for k, v in by.items()}

def difficulty(out):
    pw = json.load(open(os.path.join(HERE, 'results', 'power.json')))
    mid = [r['power'] for r in pw['mid_ramp']]
    built_mid = [r['power'] for r in pw['mid']]
    a_built = asked(json.load(open(os.path.join(REVIEW, 'results', 'res_A.json'))))
    a_fit = asked(json.load(open(os.path.join(HERE, 'results', 'res_E2.json'))), 'E2')
    a_pick = asked(json.load(open(os.path.join(HERE, 'results', 'res_E4.json'))), 'E4')
    series = [('s1', {k: a / built_mid[k - 1] for k, a in a_built.items()}, 'As built'),
              ('s2', {k: a / mid[k - 1] for k, a in a_fit.items()}, 'F5 at 1.5%'),
              ('s3', {k: a / mid[k - 1] for k, a in a_pick.items()}, 'The pick, 2%')]
    lo = [0.70, 0.80, 0.95, 1.00, 1.05, 1.12, 1.19, 1.26, 1.33, 1.40]
    hi = [0.90, 0.95, 1.05, 1.12, 1.19, 1.26, 1.33, 1.40, 1.48, 1.60]
    W, H, L, R, T, B = 760, 420, 56, 150, 60, 52
    YMAX = 2.2
    def x(k): return L + (k - 1) * (W - L - R) / 9
    def y(v): return T + (H - T - B) * (1 - v / YMAX)
    o = head(W, H, 'How hard each map is against the base game, maps 1 to 10',
             'Effective difficulty D, the power a map asks for divided by the power a player carries into it (mid case), 1 being a whole base game run. The shaded band is the target.')
    o.append('<text class="t" x="%d" y="24" font-size="15" font-weight="600">How hard each map is, against a base game run</text>' % L)
    o.append('<text class="t2" x="%d" y="42" font-size="12">D = power the map asks / power carried in (mid case). Above 1 is harder than Downpour to Tide 30. Band: the target</text>' % L)
    for v in (0, 0.5, 1.0, 1.5, 2.0):
        o.append('<line class="g" x1="%d" x2="%d" y1="%.1f" y2="%.1f" stroke-width="1"/>' % (L, W - R, y(v), y(v)))
        o.append('<text class="t2" x="%d" y="%.1f" font-size="11" text-anchor="end">%g</text>' % (L - 8, y(v) + 4, v))
    o.append('<line x1="%d" x2="%d" y1="%.1f" y2="%.1f" stroke="var(--text2)" stroke-width="1.5" stroke-dasharray="5 4"/>' % (L, W - R, y(1), y(1)))
    for k in range(1, 11):
        o.append('<text class="t2" x="%.1f" y="%d" font-size="11" text-anchor="middle">%d</text>' % (x(k), H - B + 18, k))
    o.append('<text class="t2" x="%.1f" y="%d" font-size="12" text-anchor="middle">Map of the campaign</text>' % ((L + W - R) / 2, H - 10))
    band = [(x(k), y(hi[k - 1])) for k in range(1, 11)] + [(x(k), y(lo[k - 1])) for k in range(10, 0, -1)]
    o.append('<path d="M%sZ" fill="var(--band)" fill-opacity="0.12"><title>Target: %s</title></path>' % (' L'.join('%.1f,%.1f' % p for p in band),
             ', '.join('map %d %.2f-%.2f' % (k, lo[k - 1], hi[k - 1]) for k in range(1, 11))))
    ends = []
    for key, vals, name in series:
        maps = sorted(vals)
        pts = [(x(k), y(min(vals[k], YMAX))) for k in maps]
        o.append('<path d="M%s" fill="none" stroke="var(--%s)" stroke-width="2" stroke-linejoin="round"/>' % (' L'.join('%.1f,%.1f' % p for p in pts), key))
        for k, (px, py) in zip(maps, pts):
            o.append('<circle cx="%.1f" cy="%.1f" r="4.5" fill="var(--%s)" stroke="var(--surface)" stroke-width="2"><title>%s, map %d: D %.2f</title></circle>' % (px, py, key, name, k, vals[k]))
        ends.append([pts[-1][1], key, name, vals[maps[-1]]])
    ends.sort()
    for i in range(1, len(ends)):
        ends[i][0] = max(ends[i][0], ends[i - 1][0] + 16)
    for py, key, name, v in ends:
        o.append('<text class="t" x="%d" y="%.1f" font-size="12"><tspan fill="var(--%s)">&#9679;</tspan> %s %.2f</text>' % (W - R + 10, py + 4, key, name, v))
    o.append('</svg>')
    open(out, 'w').write('\n'.join(o))
    return series

def spool(out):
    A = {r['name']: r for r in json.load(open(os.path.join(HERE, 'spool', 'results_A.json')))}
    Bres = {r['name']: r for r in json.load(open(os.path.join(HERE, 'spool', 'results_B.json')))}
    pk = ['as built 2/2.5/4/3.5', 'P1-P3 3/3.5/6/4.5', '4/4.5/6/4.5', '5/5.5/6/4.5', '6/6.5/6/4.5', '6/6.5/8/4.5']
    label = {'as built 2/2.5/4/3.5': 'As built 2 / 2.5 / 4 / 3.5', 'P1-P3 3/3.5/6/4.5': 'P1-P3 3 / 3.5 / 6 / 4.5'}
    smacky = {m: A['smacky one wall %s' % m]['mean_t'] for m in ('purlest', 'woozy')}
    W, rowh, L, R, T = 760, 34, 230, 40, 92
    H = T + rowh * len(pk) + 56
    XMAX = 16
    def x(v): return L + (W - L - R) * (v - 1) / (XMAX - 1)
    o = head(W, H, 'Spool with one Pin Cushion: the Tide it reaches by package',
             'Mean Tide reached over 30 runs (16 means won) on Purlest and Woozy, for each package of Stitched Damage, Dart Damage, Crafted a Tin and Dart Range, with the wall kept. Dashed lines: Smacky on the same map.')
    o.append('<text class="t" x="16" y="24" font-size="15" font-weight="600">Spool with one Pin Cushion: the Tide it reaches</text>')
    o.append('<text class="t2" x="16" y="42" font-size="12">Mean over 30 runs, 60 s breaks; 16 = map won. Package: Stitched Damage / Dart Damage / Crafted a Tin / Dart Range</text>')
    o.append('<text class="t" x="16" y="66" font-size="12"><tspan fill="var(--s1)">&#9679;</tspan> Purlest<tspan dx="16" fill="var(--s2)">&#9670;</tspan> Woozy<tspan dx="16">Dashed lines: Smacky on the same map</tspan></text>')
    for v in (1, 4, 7, 10, 13, 16):
        o.append('<line class="g" x1="%.1f" x2="%.1f" y1="%d" y2="%d" stroke-width="1"/>' % (x(v), x(v), T - 8, T + rowh * len(pk)))
        o.append('<text class="t2" x="%.1f" y="%d" font-size="11" text-anchor="middle">%s</text>' % (x(v), T + rowh * len(pk) + 18, 'won' if v == 16 else 'T%d' % v))
    o.append('<text class="t2" x="%.1f" y="%d" font-size="12" text-anchor="middle">Tide reached</text>' % ((L + W - R) / 2, H - 10))
    for m, key in (('purlest', 's1'), ('woozy', 's2')):
        o.append('<line x1="%.1f" x2="%.1f" y1="%d" y2="%d" stroke="var(--%s)" stroke-width="1.5" stroke-dasharray="5 4"><title>Smacky, %s: %.1f</title></line>' % (x(smacky[m]), x(smacky[m]), T - 8, T + rowh * len(pk), key, m.title(), smacky[m]))
    for i, p in enumerate(pk):
        cy = T + rowh * i + rowh / 2
        o.append('<text class="t" x="%d" y="%.1f" font-size="12" text-anchor="end">%s</text>' % (L - 12, cy + 4, label.get(p, p.replace('/', ' / '))))
        for m, key, shape in (('purlest', 's1', 'c'), ('woozy', 's2', 'd')):
            r = Bres['spool kept wall %s %s' % (m, p)]
            px = x(min(r['mean_t'], XMAX))
            tip = '<title>%s, %s: Tide %.1f, won %d%%</title>' % (p, m.title(), r['mean_t'], round(100 * r['win']))
            if shape == 'c':
                o.append('<circle cx="%.1f" cy="%.1f" r="5" fill="var(--%s)" stroke="var(--surface)" stroke-width="2">%s</circle>' % (px, cy, key, tip))
            else:
                o.append('<path d="M%.1f,%.1f l6,6 l-6,6 l-6,-6 Z" fill="var(--%s)" stroke="var(--surface)" stroke-width="2">%s</path>' % (px, cy - 6, key, tip))
    o.append('</svg>')
    open(out, 'w').write('\n'.join(o))

if __name__ == '__main__':
    s = difficulty(os.path.join(HERE, 'difficulty_by_map.svg'))
    for key, vals, name in s:
        print('%-14s' % name, ' '.join('%d:%.2f' % (k, vals[k]) for k in sorted(vals)))
    spool(os.path.join(HERE, 'spool_one_wall.svg'))
