"""The report's chart: power a player carries against the power a map asks for, maps 1-10, both as multiples of what
the base game asks for (1 = a whole Downpour run on the same vessel and map). One axis, log scale. Writes an SVG with its
own light and dark colours (the dataviz reference palette) and a <title> tooltip on every point.

    python3 chart.py results/res_A.json results/power.json results/res_D.json power_vs_pressure.svg S2
"""
import json, math, sys

W, H = 760, 440
L, R, T, B = 64, 150, 56, 52
YMIN, YMAX = 0.1, 8.0
LIGHT = dict(surface='#fcfcfb', text='#0b0b0b', text2='#52514e', grid='#e4e3df', s1='#2a78d6', s2='#eb6834', s3='#1baf7a', s4='#c98500', band='#eb6834')
DARK = dict(surface='#1a1a19', text='#f0efec', text2='#c3c2b7', grid='#3a3936', s1='#3987e5', s2='#d95926', s3='#199e70', s4='#c98500', band='#d95926')

def x(k): return L + (k - 1) * (W - L - R) / 9
def y(v): return T + (H - T - B) * (math.log(YMAX) - math.log(v)) / (math.log(YMAX) - math.log(YMIN))

def gmean(vs): return math.exp(sum(math.log(v) for v in vs) / len(vs))

BASE = {}

def pressure(res, vessel='smacky', cand=None):
    """Geometric mean over Smacky's three maps of W50(map k) / W50(base) on the same map: Smacky is the one vessel whose
    bot plays Downpour's Tides 16-30 in the model (report §1). BASE holds res_A's base runs."""
    for r in res:
        if r['kind'] == 'base': BASE[(r['v'], r['m'])] = r['w50']
    by = {}
    for r in res:
        if r['kind'] == 'mode' and r['v'] == vessel and (cand is None or r.get('cand') == cand):
            by.setdefault(r['map'], []).append(r['w50'] / BASE[(r['v'], r['m'])])
    return [gmean(by[k]) if k in by else None for k in range(1, 11)]

def path(pts): return 'M' + ' L'.join('%.1f,%.1f' % p for p in pts)

def main(a, p, b, out, cand=None):
    built = pressure(json.load(open(a)))
    pw = json.load(open(p))
    mid = [r['power'] for r in pw['mid']]; low = [r['power'] for r in pw['low']]; high = [r['power'] for r in pw['high']]
    prop = pressure(json.load(open(b)), cand=cand) if b != '-' else None
    carried_ramp = [r['power'] for r in pw['mid_ramp']]
    o = []
    o.append('<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 %d %d" width="%d" height="%d" font-family="system-ui, -apple-system, Segoe UI, sans-serif" role="img" aria-labelledby="t d">' % (W, H, W, H))
    o.append('<title id="t">Power carried against power asked, maps 1 to 10</title>')
    o.append('<desc id="d">Both lines are multiples of the power the base game asks for (1). Where the orange line is above a blue or green one, that map is easier than the base game.</desc>')
    css = ''.join('--%s:%s;' % kv for kv in LIGHT.items()); dcss = ''.join('--%s:%s;' % kv for kv in DARK.items())
    o.append('<style>svg{%s} @media (prefers-color-scheme: dark){svg{%s}} .t{fill:var(--text)} .t2{fill:var(--text2)} .g{stroke:var(--grid)}</style>' % (css, dcss))
    o.append('<rect width="100%" height="100%" fill="var(--surface)"/>')
    o.append('<text class="t" x="%d" y="24" font-size="15" font-weight="600">Power carried against power asked, maps 1 to 10</text>' % L)
    o.append('<text class="t2" x="%d" y="42" font-size="12">Multiples of what the base game asks (1 = Downpour to Tide 30), log scale. Shaded: carried, low to high case</text>' % L)
    for v in (0.1, 0.2, 0.5, 1, 2, 5):
        o.append('<line class="g" x1="%d" x2="%d" y1="%.1f" y2="%.1f" stroke-width="1"/>' % (L, W - R, y(v), y(v)))
        o.append('<text class="t2" x="%d" y="%.1f" font-size="11" text-anchor="end">%s</text>' % (L - 8, y(v) + 4, ('%g' % v)))
    o.append('<line x1="%d" x2="%d" y1="%.1f" y2="%.1f" stroke="var(--text2)" stroke-width="1.5" stroke-dasharray="5 4"/>' % (L, W - R, y(1), y(1)))
    o.append('<text class="t2" x="%d" y="%.1f" font-size="11">base game</text>' % (W - R + 8, y(1) + 4))
    for k in range(1, 11):
        o.append('<text class="t2" x="%.1f" y="%d" font-size="11" text-anchor="middle">%d</text>' % (x(k), H - B + 18, k))
    o.append('<text class="t2" x="%.1f" y="%d" font-size="12" text-anchor="middle">Map of the campaign</text>' % ((L + W - R) / 2, H - 10))
    band = [(x(k), y(high[k - 1])) for k in range(1, 11)] + [(x(k), y(low[k - 1])) for k in range(10, 0, -1)]
    o.append('<path d="%sZ" fill="var(--band)" fill-opacity="0.14"/>' % path(band))
    series = [('s2', mid, 'Carried (mid case)', 'carried'), ('s1', built, 'Asked, as built', 'asked')]
    if prop:
        series.insert(1, ('s4', carried_ramp, 'Carried, price ramp', 'carried'))
        series.append(('s3', prop, 'Asked, proposed %s' % (cand or ''), 'asked'))
    labels = []
    for col, vals, name, what in series:
        pts = [(x(k), y(vals[k - 1])) for k in range(1, 11)]
        dash = ' stroke-dasharray="6 4"' if col == 's4' else ''
        o.append('<path d="%s" fill="none" stroke="var(--%s)" stroke-width="2" stroke-linejoin="round"%s/>' % (path(pts), col, dash))
        for k, (px, py) in enumerate(pts, 1):
            o.append('<circle cx="%.1f" cy="%.1f" r="4" fill="var(--%s)" stroke="var(--surface)" stroke-width="2"><title>Map %d, %s: x%.2f of the base game</title></circle>' % (px, py, col, k, name.lower(), vals[k - 1]))
        labels.append([pts[-1][1] + 4, name])
    labels.sort()
    for i in range(1, len(labels)):            # keep the end labels 14 px apart
        labels[i][0] = max(labels[i][0], labels[i - 1][0] + 14)
    for ly, name in labels:
        o.append('<text class="t" x="%d" y="%.1f" font-size="12">%s</text>' % (W - R + 8, ly, name))
    o.append('</svg>')
    open(out, 'w').write('\n'.join(o))
    print('asked as built', [round(v, 2) for v in built]); print('carried mid', mid)
    if prop: print('asked proposed', [round(v, 2) for v in prop])

if __name__ == '__main__':
    main(*sys.argv[1:])
