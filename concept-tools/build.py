"""Rebuilds the vessel art plates in ../index.html. GRUBB and WICK come from sprites.py; MUMMER comes
from tds_mummer.py, the same drawing as its in-game sprite pack. Run: python3 build.py"""
import re, sprites, tds_mummer
import os
P = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', 'index.html')
html = open(P).read()
OX, OY, K = 20, 38, 7
DY = ' dy="16"'
def g(gx, gy): return (OX + K*gx + 3, OY + K*gy + 3)

def note(x, y, lines, ax=None):
    t = f'<text x="{x}" y="{y}">' + ''.join(f'<tspan x="{x}"{"" if i == 0 else DY}>{l}</tspan>' for i, l in enumerate(lines)) + '</text>'
    if ax:
        (sx, sy), (ex, ey) = ax
        mx, my = (sx + ex) / 2, min(sy, ey) - 6
        t += f'<path d="M{sx} {sy} Q{mx:.0f} {my:.0f} {ex} {ey}" fill="none" stroke="#1d232a" stroke-width="1.6" marker-end="url(#arrow)"/>'
    return t

spec = {
 'grubb': dict(
   title='Grubb as a 32 by 32 pixel sprite: a round brown mole in a yellow mining helmet with a lamp, pale face, pink nose and white shovel claws, half out of a dirt mound on a pale ground tile',
   notes=[(262, 40, ['helmet lamp shows', 'ore under the ground'], ((258, 50), g(18, 3))),
          (262, 140, ['shovel claws mine', 'from BELOW and', 'leave a Pit behind'], ((262, 180), g(25, 25)))],
   cap="32×32 sprite in the game's cute 2.5D pixel style. Wide and low, all claws, and the helmet lamp is the brightest pixel so GRUBB reads in a crowd."),
 'wick': dict(
   title='Wick as a 32 by 32 pixel sprite: a stubby cream candle with dripping wax, a small smiling face and an orange flame for a head, holding a pole with a yellow lantern',
   notes=[(262, 40, ['flame = head. it', 'shrinks as Wax', '(HP) runs out'], ((258, 48), g(18, 3))),
          (262, 118, ['Lure Lantern:', "Ballistas can't", 'help looking'], ((258, 122), g(29, 8))),
          (262, 196, ['casting burns', 'Wax off the body'], ((258, 200), g(21, 21)))],
   cap="32×32 sprite. Two readable states: a full flame when healthy, a guttering stub at low Wax. The candle is the palest sprite on the field, so the flame pops."),
 'mummer': dict(
   title='Mummer as a 32 by 32 pixel sprite: a lilac hand-sewn costume shaped like a soft triangle, its floppy point drooped over to one side with a brass bell on the end, a happy face drawn on the front with big crooked eyes, pink blush and a zigzag grin, a hem cut in little teeth, two white eyes peeking out of the dark under the hem, and two dark shadow hands with pale claw tips',
   notes=[(262, 30, ['a costume. the', 'point flopped over', 'and got sewn back'], ((258, 48), g(21, 7))),
          (262, 104, ['the face is drawn', 'on, one eye a', 'pixel out of line'], ((258, 112), g(21, 17))),
          (262, 190, ['the real eyes', 'are down here'], ((258, 196), g(18, 27)))],
   cap="32×32 sprite in the in-game house style. Cute on the outside, creepy underneath. Smacky is the square and SnoSno the circle, so MUMMER's costume is a soft triangle with a happy face drawn on, and something in the dark under the hem."),
}

def mummer_grid():
    """The tds_mummer front frame over the same pale ground tile the other plates use."""
    t = sprites.Sprite(); sprites.ground(t, cx=16); col = t.render()
    cv = tds_mummer.mummer('front')
    for y in range(32):
        for x in range(32):
            r, g_, b, a = cv.px[y][x]
            if a:
                col[y][x] = '#%02x%02x%02x' % (r, g_, b)
    return col

def rects(col, ox, oy, k):
    out = []
    for y, row in enumerate(col):
        x = 0
        while x < len(row):
            c = row[x]
            if c is None:
                x += 1; continue
            x2 = x
            while x2 + 1 < len(row) and row[x2 + 1] == c:
                x2 += 1
            out.append(f'<rect x="{ox + x*k}" y="{oy + y*k}" width="{(x2 - x + 1)*k}" height="{k}" fill="{c}"/>')
            x = x2 + 1
    return ''.join(out)

for name, d in spec.items():
    if name == 'mummer':
        col = mummer_grid()
        big, small = rects(col, OX, OY, K), rects(col, 330, 230, 2)
    else:
        s = getattr(sprites, name)(); s.render()
        big = s.svg_rects(OX, OY, K)
        small = s.svg_rects(330, 230, 2)
    notes = ''.join(note(*n) for n in d['notes'])
    svg = (f'<svg viewBox="0 0 400 300" role="img" aria-labelledby="{name}-art-t">\n'
           f'          <title id="{name}-art-t">{d["title"]}</title>\n'
           f'          <g shape-rendering="crispEdges">{big}</g>\n'
           f'          <g shape-rendering="crispEdges">{small}</g>\n'
           f'          <g font-family="Gochi Hand, cursive" font-size="14" fill="#1d232a">{notes}'
           f'<text x="262" y="262"><tspan x="262">in-game</tspan><tspan x="262"{DY}>size, 2×</tspan></text></g>\n'
           f'        </svg>\n'
           f'        <figcaption>{d["cap"]}</figcaption>')
    pat = re.compile(r'<svg viewBox="0 0 400 300" role="img" aria-labelledby="' + name + r'-art-t">.*?</figcaption>', re.S)
    html, n = pat.subn(lambda m: svg, html)
    assert n == 1, name
open(P, 'w').write(html)
