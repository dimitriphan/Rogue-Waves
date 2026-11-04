"""Minion redraw toolkit (2026-11-04): poses, small-sprite parts and the outputs for every modded minion's ten versions.

Dimitri, 2026-11-04: "Create new sprites for all modded minions. They should be closer in size to snobles. Create at
least 10 versions of each." The Snoble (Mob_Snoble, SnobleGruntIdle-Sheet) is a 12 px cell with a body 6-8 px wide and
5-8 px tall that hops; Smacky's miner is 6 x 9 in a 16 px cell (measured from the game's sheets, kit docs/18 section 2;
the game's art is never copied here). Every version here keeps its sheet's name, size and frame layout, so a pick is
a file copy: only the body inside each cell got smaller.

House rules (kit docs/03 section 1, docs/18 sections 3-4, .claude/rules/art.md): opaque, no outline, light from the
upper left, shade keeps the hue, no pure black, magenta only on foes (Spool's purple is his exception), feet on the
bottom row, body centred on the cell's middle column. Each frame is drawn facing right; the left-heading rows are its
mirror, as the code's own minion sheets do (kit docs/18 section 3.7: the stock foes' v1 is an exact mirror).
"""
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, '..', 'temp_art'))
from tart import Canvas, Mask, H, shade, save_png, save_ase, preview, sheet, REPO  # noqa: E402,F401


# ------------------------------------------------------------------ poses

class Pose:
    def __init__(self, kind, i, front, cell):
        self.kind, self.i, self.front, self.cell = kind, i, front, cell
        self.cx = cell // 2            # middle column (the body centres on cx - 0.5 .. cx + 0.5)
        self.g = cell - 1              # the ground row
        self.dy = self.sq = self.arm = self.dx = self.sway = self.fx = self.tap = 0
        self.lift = (0, 0)
        self.blink = False
        self.atk = None

    @property
    def top_shift(self):
        """Rows the body's top moves down: squash lowers it, a stretch raises it."""
        return self.sq


GAITS = {
    # two-step walk: the body rises on each passing frame, the lifted foot swings
    'walk': dict(dy=[0, -1, -1, 0, -1, -1], sq=[1, 0, 0, 1, 0, 0], lift=[(0, 0), (1, 0), (1, 0), (0, 0), (0, 1), (0, 1)],
                 arm=[0, 1, 1, 0, -1, -1], dx=[0] * 6),
    # a waddle: the same feet, the body rocks side to side instead of bobbing high
    'waddle': dict(dy=[0, 0, -1, 0, 0, -1], sq=[1, 0, 0, 1, 0, 0], lift=[(0, 0), (1, 0), (1, 0), (0, 0), (0, 1), (0, 1)],
                   arm=[0, 1, 1, 0, -1, -1], dx=[0, 0, 1, 0, 0, -1]),
    # the Snoble's hop: squash, take off, hang, land
    'hop': dict(dy=[0, -1, -3, -3, -1, 0], sq=[1, -1, -1, 0, 0, 1], lift=[(0, 0)] * 6, arm=[0, 1, 1, 0, -1, 0], dx=[0] * 6),
    # floats: a slow rise and fall, no feet on the ground
    'float': dict(dy=[0, -1, -2, -2, -1, 0], sq=[0] * 6, lift=[(0, 0)] * 6, arm=[0, 1, 0, -1, 0, 1], dx=[0] * 6),
}
IDLE = dict(dy=[0] * 6, sq=[0, 0, 1, 1, 0, 0], sway=[0, 0, 1, 1, 0, -1], blink=4)
ATTACK = dict(dx=[-1, -1, 1, 0], dy=[0, -1, 0, 0], sq=[-1, -1, 1, 0], arm=[-1, -2, 2, 1], fx=[0, 0, 2, 1])
TUNE = dict(tap=[1, 2, 4, 3, 2, 1], sq=[0, 0, 1, 1, 0, 0])


def pose(kind, i, front, cell, gait='walk'):
    p = Pose(kind, i, front, cell)
    if kind == 'idle':
        p.sq, p.sway, p.blink = IDLE['sq'][i], IDLE['sway'][i], i == IDLE['blink']
        if gait == 'float':
            p.dy = [0, -1, -1, -1, 0, 0][i] - 1
            p.sq = 0
    elif kind == 'move':
        G = GAITS[gait]
        p.dy, p.sq, p.lift, p.arm, p.dx = G['dy'][i], G['sq'][i], G['lift'][i], G['arm'][i], G['dx'][i]
        p.sway = -p.arm
        if i == 0:
            p.sq = 0            # column 0 doubles as Idle on the 16 and 24 px unit sheets: keep it at rest
        if gait == 'float':
            p.dy -= 1
    elif kind == 'attack':
        p.atk = i
        p.dx, p.dy, p.sq, p.arm, p.fx = ATTACK['dx'][i], ATTACK['dy'][i], ATTACK['sq'][i], ATTACK['arm'][i], ATTACK['fx'][i]
        p.sway = p.dx
        p.blink = i == 2
        if gait == 'float':
            p.dy -= 1
    elif kind == 'tune':
        p.tap, p.sq = TUNE['tap'][i], TUNE['sq'][i]
    return p


# ------------------------------------------------------------------ small parts

def oval(c, cx, bottom, rx, ry, tones, cut=(-0.15, -0.75), rim=True, mask_only=False, light_off=(-0.3, -0.4)):
    """An ellipse whose lowest row is `bottom`, centred on column cx (a float: 8.0 centres an even width on a 16 cell)."""
    cy = bottom + 1 - ry
    m = Mask().ellipse(cx, cy, rx, ry)
    if not mask_only:
        shade(c, m, tones, centre=(cx + light_off[0], cy + light_off[1]), radii=(rx, ry), cut=cut, rim=rim)
    return m


def fill(c, mask, tones, cut=(-0.15, -0.75), rim=True):
    shade(c, mask, tones, cut=cut, rim=rim)
    return mask


def rows(c, art, pal, x0, y0, flip=False):
    """Stamp string rows (top first, '.' empty) at x0, y0."""
    for y, r in enumerate(art):
        for x, ch in enumerate(r):
            if ch in '. ':
                continue
            xx = x0 + (len(r) - 1 - x if flip else x)
            c.set(xx, y0 + y, pal[ch])


def dot(c, x, y, col):
    c.set(int(round(x)), int(round(y)), col)


def line(c, x0, y0, x1, y1, col):
    n = max(abs(x1 - x0), abs(y1 - y0), 1)
    for k in range(n + 1):
        c.set(int(round(x0 + (x1 - x0) * k / n)), int(round(y0 + (y1 - y0) * k / n)), col)


def feet(c, p, xs, tones, w=2, h=1):
    """Feet on the ground row: xs are the left columns of the back and front foot; p.lift raises them."""
    for k, x in enumerate(xs):
        up = p.lift[k]
        for yy in range(h):
            for xx in range(w):
                c.set(x + xx, p.g - up - yy, tones[1] if (xx == w - 1 or yy == 0) and len(tones) > 1 else tones[0])


def eyes(c, p, xs, y, col, tall=1, blink_col=None):
    """Dark dot eyes (front only). tall=2 draws 1 x 2 eyes like the Snoble's and the vessels'."""
    if not p.front:
        return
    for x in xs:
        if p.blink:
            c.set(x, y + tall - 1, blink_col or col)
        else:
            for k in range(tall):
                c.set(x, y + k, col)


def mirror(cv):
    return cv.flip()


def opaque(cv):
    """The house rule: every pixel either full or empty (previews may blend, sheets may not)."""
    for k, px in enumerate(cv.c):
        if px[3] and px[3] < 255:
            cv.c[k] = px[:3] + (255,)
    return cv


# ------------------------------------------------------------------ sheets

LAYOUTS = {
    'cordy': dict(cell=24, cols=16, segs=[('idle', 6), ('move', 6), ('attack', 4)]),
    'head': dict(cell=24, cols=10, segs=[('move', 6), ('attack', 4)]),
    'unit16': dict(cell=16, cols=10, segs=[('move', 6), ('attack', 4)]),
    'naut': dict(cell=16, cols=6, segs=[('move', 6)]),
    'nauttune': dict(cell=16, cols=6, segs=[('tune', 6)]),
}


def frames(draw, layout, gait='walk', **kw):
    """All cells of a sheet, in sheet order, and the Aseprite tags (tag, row, col, count)."""
    L = LAYOUTS[layout]
    cell = L['cell']
    cells, tags = [], []
    for v in range(4):
        front = v in (0, 1)
        col = 0
        for kind, n in L['segs']:
            for i in range(n):
                cv = Canvas(cell, cell)
                draw(cv, pose(kind, i, front, cell, gait), **kw)
                opaque(cv)
                cells.append(mirror(cv) if v in (1, 3) else cv)
            tags.append(('%s_v%d' % (kind, v), v, col, n))
            col += n
    return cells, tags, L


def write(base, name, cells, tags, L):
    """base/game_size/<name>.png, base/aseprite/<name>.aseprite (layer "TEMP art"), base/preview_8x/<name>_8x.png."""
    cols, cell = L['cols'], L['cell']
    s = sheet(cells, cols, cell)
    save_png(s, os.path.join(base, 'game_size', name + '.png'))
    save_png(preview(s, cell, 8), os.path.join(base, 'preview_8x', name + '_8x.png'))
    fr, at = [], []
    nrows = len(cells) // cols
    for tag, row, col, n in tags:
        start = len(fr)
        fr += cells[row * cols + col: row * cols + col + n]
        at.append((tag, start, len(fr) - 1, 'TEMP;sheet=%s.png;row=%d;col=%d;cols=%d;rows=%d' % (name, row, col, cols, nrows)))
    save_ase(os.path.join(base, 'aseprite', name + '.aseprite'), cell, fr, at)
    return s


def bbox(cv):
    xs = [k % cv.w for k, px in enumerate(cv.c) if px[3]]
    ys = [k // cv.w for k, px in enumerate(cv.c) if px[3]]
    if not xs:
        return None
    return min(xs), min(ys), max(xs) + 1, max(ys) + 1
