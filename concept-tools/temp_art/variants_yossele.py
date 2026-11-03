"""Three alternative versions of each of Yossele's temporary animation sheets (2026-10-28), for Dimitri to pick one per
sheet: "For all temporary assets, create alternative versions for me to choose for each asset. There should be different
versions for every single animation included for Yossele and Spool. Create 3 versions of each asset for now."

Every version is a drop-in: same name, size and frame layout as the sheet in Art/Temporary/game_size/, which stays the
one in game until he picks. Yossele's body is never redrawn: each frame starts from the code's idle frame (his reshade,
taken from the current Yossele-Sheet.png), cut into the body, the sprout, the shoulder leaf, the eyes and the four
limbs (two knuckle arms, two root legs), and only those parts move. The Cordyfriend is drawn anew in three builds.

    Yossele-Sheet (idle + walk)  v1 Breathe and stride   v2 Sway and waddle     v3 Look about and scurry
    YosseleAttack-Sheet          v1 Charged beam         v2 Recoil blast        v3 Spit glob
    Yosselefriend-Sheet          v1 Tall clubs, hop      v2 Squat puffball, slam v3 Spore sack, sneeze

    python3 concept-tools/temp_art/variants_yossele.py   -> ModSource/Yossele/Art/Temporary/variants/v1..v3/
"""
import os
from tart import gif, Canvas, Mask, H, shade, write_sheet, REPO
from yossele_anims import load, cell, hole

TEMP = os.path.join(REPO, 'TDS_Mod', 'ModSource', 'Yossele', 'Art', 'Temporary')
CLAY, CLAY_SH = H('945743'), H('79432F')
EYE = H('99E550')
LEAF = (H('7BCF80'), H('65AD69'))
LIME, PALE, DIM, WHITE = H('99E550'), H('D9F7B5'), H('6ABE30'), H('FFFFFF')
GLOB = (H('C4F28A'), H('99E550'), H('6ABE30'))
SMOKE = (H('7BCF80'), H('65AD69'))
CUT = 28
NAMES = {
    'Yossele-Sheet': ('Breathe and stride', 'Sway and waddle', 'Look about and scurry'),
    'YosseleAttack-Sheet': ('Charged beam', 'Recoil blast', 'Spit glob'),
    'Yosselefriend-Sheet': ('Tall clubs, hop', 'Squat puffball, slam', 'Spore sack, sneeze'),
}


class Parts:
    """The code's idle frame cut into parts: body (rows above CUT without sprout, leaf, eyes), sprout, shoulder leaf,
    eyes, and four limbs found as runs of opaque pixels in the top and bottom limb rows."""

    def __init__(self, base):
        self.base = base
        self.body, self.sprout, self.leaf, self.eyes = {}, {}, {}, []
        opaque = [y for y in range(32) if any(base.get(x, y)[3] for x in range(32))]
        self.top = opaque[0]
        for y in range(CUT):
            for x in range(32):
                p = base.get(x, y)
                if not p[3]:
                    continue
                if p in LEAF and y < self.top + 3:
                    self.sprout[(x, y)] = p
                elif p in LEAF:
                    self.leaf[(x, y)] = p
                else:
                    if p == EYE:
                        self.eyes.append((x, y))
                    self.body[(x, y)] = p
        self.limbs = list(zip(self.runs(CUT), self.runs(31)))   # ((x0, x1) at the hip, (x0, x1) on the ground)

    def runs(self, y):
        out, x = [], 0
        while x < 32:
            if self.base.get(x, y)[3]:
                s = x
                while x < 32 and self.base.get(x, y)[3]:
                    x += 1
                out.append((s, x - 1))
            x += 1
        return out


def limb(c, hip, foot, top_y, lift, fdx):
    """A 2 px limb from its hip (x, top_y) to its foot; the left column lit, the right in shade; a lifted foot stops
    short and swings fdx along the step."""
    x0, y0 = hip
    x1, y1 = foot[0] + fdx, 31 - lift
    n = max(y1 - y0, 1)
    for i in range(n + 1):
        y = y0 + i
        x = round(x0 + (x1 - x0) * i / n)
        c.set(x, y, CLAY)
        c.set(x + 1, y, CLAY_SH)


def pose(P, bdx=0, bdy=0, sdx=0, sdy=0, ldy=0, eye='open', look=0, lifts=(0, 0, 0, 0), fdx=(0, 0, 0, 0), hipdx=None):
    """One frame: the body moved (bdx, bdy), the sprout a further (sdx, sdy), the shoulder leaf ldy, eyes 'open',
    'shut' or moved by look, each limb lifted and swung."""
    c = Canvas(32, 32)
    hipdx = hipdx if hipdx is not None else bdx
    for k, ((hx0, hx1), (fx0, fx1)) in enumerate(P.limbs):
        limb(c, (hx0 + hipdx, CUT + bdy - 1), (fx0, 31), CUT + bdy - 1, lifts[k], fdx[k])
    for (x, y), p in P.body.items():
        if (x, y) in P.eyes:
            p = CLAY
        c.set(x + bdx, y + bdy, p)
    for (x, y), p in P.leaf.items():
        c.set(x + bdx, y + bdy + ldy, p)
    for (x, y), p in P.sprout.items():
        c.set(x + bdx + sdx, y + bdy + sdy, p)
    for (x, y) in P.eyes:
        if eye == 'shut':
            c.set(x + bdx, y + bdy + 1, CLAY_SH)
        else:
            c.set(x + bdx + look, y + bdy, EYE)
    return c


# ------------------------------------------------------------------------------------------------- idle and walk

def idle(P, v, d, i):
    if v == 1:      # breathe: rises on the in-breath, the sprout lags, a blink
        return pose(P, bdy=[0, 0, -1, -1, 0, 0][i], sdy=[0, 0, 1, 0, 0, 0][i], sdx=[0, 0, 0, d, d, 0][i],
                    eye='shut' if i == 4 else 'open')
    if v == 2:      # sway: the body rocks over planted roots, the sprout swings the other way
        sw = [0, 1, 1, 0, -1, -1][i]
        return pose(P, bdx=sw, sdx=-sw, ldy=abs(sw) - 1 if sw else 0, hipdx=sw)
    # look about: eyes glance ahead, back, then behind; a knuckle taps
    return pose(P, look=[0, d, d, 0, -d, 0][i], sdx=[0, 0, d, 0, 0, -d][i],
                lifts=[(0, 0, 0, 0), (0, 0, 0, 0), (1, 0, 0, 0), (0, 0, 0, 0), (0, 0, 0, 1), (0, 0, 0, 0)][i],
                eye='shut' if i == 3 else 'open')


def walk(P, v, d, i):
    if v == 1:      # stride: legs in turn, arms against them, a bob on each push
        lifts = [(0, 0, 0, 0), (0, 1, 0, 0), (0, 1, 0, 0), (0, 0, 0, 0), (0, 0, 1, 0), (0, 0, 1, 0)][i]
        fdx = [(0, 0, 0, 0), (-d, d, 0, d), (-d, d, 0, d), (0, 0, 0, 0), (d, 0, d, -d), (d, 0, d, -d)][i]
        return pose(P, bdy=[0, -1, 0, 0, -1, 0][i], sdx=[0, -d, -d, 0, -d, -d][i], lifts=lifts, fdx=fdx)
    if v == 2:      # waddle: the body leans onto the planted side while the other root lifts
        sw = [0, -1, -1, 0, 1, 1][i]
        lifts = [(0, 0, 0, 0), (0, 0, 1, 1), (0, 0, 1, 0), (0, 0, 0, 0), (1, 1, 0, 0), (0, 1, 0, 0)][i]
        return pose(P, bdx=sw, hipdx=sw, sdx=sw, bdy=[0, 0, -1, 0, 0, -1][i], lifts=lifts,
                    ldy=[0, -1, 0, 0, -1, 0][i])
    # scurry: low and leaning in, the four limbs ripple back to front, the sprout streams behind
    lifts = [(1, 0, 0, 0), (0, 1, 0, 0), (0, 0, 1, 0), (0, 0, 0, 1), (0, 1, 0, 0), (0, 0, 1, 0)][i]
    if d < 0:
        lifts = tuple(reversed(lifts))
    fdx = tuple(d if l else 0 for l in lifts)
    return pose(P, bdx=d, bdy=1, hipdx=d, sdx=-d, sdy=[0, 1, 0, 1, 0, 1][i], lifts=lifts, fdx=fdx)


# ------------------------------------------------------------------------------------------------------- attacks

def glow(c, pts, dx, dy, mode):
    pts = [(x + dx, y + dy) for x, y in pts]
    if not pts:
        return None
    s = set(pts)
    for x, y in pts:
        edge = (x + 1, y) not in s or (x, y + 1) not in s
        c.set(x, y, {'dim': DIM, 'lime': LIME if edge else PALE, 'white': LIME if edge else WHITE}[mode])
    return (sum(x for x, _ in pts) / len(pts), sum(y for _, y in pts) / len(pts))


def attack(P, v, d):
    pts = hole(P.base)
    fr = []
    if v == 1:      # charged beam: motes drawn in, a crouch, a beam to the edge of the cell, a fade with a drip
        f = pose(P)
        c = glow(f, pts, 0, 0, 'dim')
        for sx, sy in ((5, -4), (-5, -3), (6, 3), (-4, 4)):
            f.set(int(c[0] + sx), int(c[1] + sy), PALE)
        fr.append(f)
        f = pose(P, bdy=1, sdy=-1)
        c = glow(f, pts, 0, 1, 'lime')
        for sx, sy in ((3, -2), (-3, -2), (3, 2), (-3, 2)):
            f.set(int(c[0] + sx), int(c[1] + sy), LIME)
        fr.append(f)
        f = pose(P, bdx=-d, bdy=0, sdx=-d)
        c = glow(f, pts, -d, 0, 'white')
        ox, oy = int(round(c[0])), int(round(c[1]))
        x = ox
        while 0 <= x < 32:
            if not f.get(x, oy)[3] or f.get(x, oy) in (LIME, PALE, WHITE):
                f.set(x, oy, WHITE)
                f.set(x, oy - 1, LIME) if not f.get(x, oy - 1)[3] else None
                f.set(x, oy + 1, LIME) if not f.get(x, oy + 1)[3] else None
            x += d
        fr.append(f)
        f = pose(P)
        c = glow(f, pts, 0, 0, 'dim')
        f.set(int(c[0]), int(c[1]) + 3, DIM)
        f.set(int(c[0]), int(c[1]) + 5, LIME)
        fr.append(f)
    elif v == 2:    # recoil blast: lean in, squint, a blast ring and a hard kick back, smoke
        f = pose(P, bdx=d, hipdx=d, sdx=d)
        glow(f, pts, d, 0, 'dim')
        fr.append(f)
        f = pose(P, bdx=d, hipdx=d, bdy=1, sdx=2 * d, eye='shut')
        glow(f, pts, d, 1, 'lime')
        fr.append(f)
        f = pose(P, bdx=-2 * d, hipdx=-2 * d, bdy=-1, sdx=-2 * d, sdy=1, lifts=(0, 1, 1, 0))
        c = glow(f, pts, -2 * d, -1, 'white')
        ox, oy = c
        ring = Mask().ellipse(ox + 4 * d, oy, 4.2, 4.2).px - Mask().ellipse(ox + 4 * d, oy, 2.8, 2.8).px
        for (x, y) in ring:
            if not f.get(x, y)[3]:
                f.set(x, y, PALE if (x + y) % 2 else LIME)
        fr.append(f)
        f = pose(P, bdx=-d, hipdx=-d)
        c = glow(f, pts, -d, 0, 'dim')
        for k, (sx, sy) in enumerate(((d, -4), (2 * d, -6), (d, -8))):
            f.set(int(c[0]) + sx, int(c[1]) + sy, SMOKE[k % 2])
        fr.append(f)
    else:           # spit glob: rear back, swell, snap forward and spit a glob that flies on
        f = pose(P, bdx=-d, hipdx=-d, bdy=-1, sdx=-d)
        glow(f, pts, -d, -1, 'dim')
        fr.append(f)
        f = pose(P, bdx=-d, hipdx=-d, bdy=-1, sdx=-2 * d)
        c = glow(f, pts, -d, -1, 'lime')
        fr.append(f)
        f = pose(P, bdx=d, hipdx=d, bdy=1, sdx=d, lifts=(0, 0, 0, 0))
        c = glow(f, pts, d, 1, 'dim')
        blob(f, int(c[0]) + 6 * d, int(c[1]), 3)
        fr.append(f)
        f = pose(P)
        c = glow(f, pts, 0, 0, 'dim')
        blob(f, int(c[0]) + 11 * d, int(c[1]) - 1, 2)
        for k in range(3):
            f.set(int(c[0]) + (6 + 2 * k) * d, int(c[1]), GLOB[2] if k % 2 else GLOB[1])
        fr.append(f)
    return fr


def blob(c, x, y, r):
    m = Mask().ellipse(x + .5, y + .5, r / 2 + .3, r / 2 + .3) if r > 2 else Mask().rect(x, y, x + 1, y + 1)
    shade(c, m, GLOB, cut=(-0.2, -0.8))
    c.set(min(p[0] for p in m.px), min(p[1] for p in m.px), WHITE)


# ---------------------------------------------------------------------------------------------------- Cordyfriend

BODY = (H('F2E8D0'), H('E3D3AE'), H('CDB88C'))
FOOT = (H('CDB88C'), H('B5A077'))
CLUB = (H('F7A64A'), H('E58A2E'), H('C46F22'))
TIP = H('FDC27A')
SPORE = (H('A3AC5E'), H('8A9649'))
PUFF = (H('DCDFAE'), H('C2C88A'), H('9AA552'))
EYE_D, EYE_D2 = H('333232'), H('4A4949')

FRIEND = {
    # body radii, clubs (x offset, height, lean, head height), specks (front, back)
    1: dict(rx=6.5, ry=6.0, clubs=[(-4, 5, -1, 3), (-1, 8, 0, 3), (2, 7, 0, 3), (4, 5, 1, 2)],
            specks=([(-4, 0), (-3, 0), (3, 2)], [(-3, -1), (-2, -1), (2, 2), (3, 2), (-1, 2)])),
    2: dict(rx=8.0, ry=4.5, clubs=[(-3, 5, -1, 4), (3, 6, 1, 4)],
            specks=([(-5, 0), (-4, 0), (-5, 1), (5, 1)], [(-4, -1), (-3, -1), (0, 1), (1, 1), (4, 0)])),
    3: dict(rx=7.0, ry=5.5, clubs=[(-3, 4, -2, 2), (0, 6, -1, 3), (3, 5, 1, 2)], cap=True,
            specks=([(-4, 1)], [(2, 2)])),
}


def friend(fv, facing, kind, i):
    P = FRIEND[fv]
    c = Canvas(24, 24)
    front = facing in (0, 1)
    d = 1 if facing in (0, 2) else -1
    dx = dy = sq = sway = puff = 0
    lift, blink, ring, glowtip = (0, 0), False, False, False
    if kind == 'idle':
        if fv == 1:       # the club tips pulse, a slow breath
            sq = [0, 0, 1, 1, 0, 0][i]
            glowtip = i in (2, 3)
            blink = i == 5
        elif fv == 2:     # a wobble side to side, low and heavy
            dx = [0, 1, 1, 0, -1, -1][i]
            sway = [0, 1, 1, 0, -1, -1][i]
            sq = [1, 1, 0, 1, 1, 0][i]
            blink = i == 3
        else:             # breathes out a little spore on the out-breath
            sq = [0, 1, 1, 0, -1, 0][i]
            puff = [0, 0, 3, 3, 0, 0][i]
            blink = i == 1
    elif kind == 'move':
        if fv == 1:       # a two-footed hop
            dy = [0, 1, -2, -3, -1, 1][i]
            sq = [0, 1, -1, -1, 0, 1][i]
            lift = [(0, 0), (0, 0), (2, 2), (3, 3), (1, 1), (0, 0)][i]
            sway = [0, 1, -1, -1, 0, 1][i] * d
        elif fv == 2:     # rolls its weight from foot to foot
            dx = [0, -1, -1, 0, 1, 1][i]
            dy = [0, 0, -1, 0, 0, -1][i]
            lift = [(0, 0), (0, 1), (0, 1), (0, 0), (1, 0), (1, 0)][i]
            sway = [0, -1, -1, 0, 1, 1][i]
        else:             # quick scuttle, leaning in, clubs streaming back
            dx = d
            dy = [0, -1, 0, -1, 0, -1][i]
            lift = [(1, 0), (0, 0), (0, 1), (0, 0), (1, 0), (0, 1)][i]
            sway = -d
    else:
        if fv == 1:       # headbutt with the clubs
            dx = [-1, -2, 3, 1][i] * d
            dy = [0, -1, 0, 0][i]
            sq = [1, -1, 1, 0][i]
            sway = [-2, -2, 3, 1][i] * d
            puff = [0, 0, 2, 1][i]
        elif fv == 2:     # jumps and slams, a ring of spores on the ground
            dy = [1, -4, 1, 0][i]
            sq = [1, -1, 2, 0][i]
            sway = [0, 0, 0, 0][i]
            ring = i in (2, 3)
            lift = [(0, 0), (4, 4), (0, 0), (0, 0)][i]
            blink = i == 2
        else:             # rears back and sneezes a cone of spores
            dx = [-1, -1, 1, 0][i] * d
            dy = [-1, -1, 1, 0][i]
            sq = [-1, -1, 1, 0][i]
            sway = [-1, -2, 2, 0][i] * d
            puff = [0, 0, 4, 5][i]
            blink = i in (1, 2)
    cx = 12 + dx
    rx, ry = P['rx'] + 0.5 * sq, P['ry'] - 0.5 * sq
    bottom = 21.5 + dy
    cy = bottom - ry
    for k, fx in enumerate((cx - 4, cx + 2)):
        up = lift[k] if d > 0 else lift[1 - k]
        shade(c, Mask().rect(fx, 21 - up + min(dy, 0) * 0, fx + 2, 23 - up), FOOT[:2], cut=(-0.2, -9))
    crown = int(round(cy - ry)) + 1
    for ox, tall, lean, hh in P['clubs']:
        bx = cx + ox
        hx = bx + lean + sway
        top = crown - tall
        shade(c, Mask().line(bx, crown + 1, hx, top + hh), (CLUB[2], CLUB[2]), rim=False)
        head = Mask().rect(hx, top, hx + 1, top + hh - 1)
        head.px.discard((hx + 1, top))
        for (x, y) in head.px:
            c.set(x, y, CLUB[0] if x == hx else CLUB[1])
        c.set(hx, top, H('FFFFFF') if glowtip else TIP)
        if glowtip:
            c.set(hx, top + 1, TIP)
        c.set(hx + 1, top + hh - 1, CLUB[2])
    body = Mask().ellipse(cx, cy, rx, ry)
    shade(c, body, BODY, centre=(cx - 0.3, cy - 0.4), radii=(rx, ry), cut=(-0.12, -0.72))
    if P.get('cap'):    # the spore sack: an olive cap of spores over the crown, with pale pores
        capm = {(x, y) for (x, y) in body.px if y <= cy - ry * 0.25}
        for (x, y) in capm:
            c.set(x, y, SPORE[0] if (x - cx) * -0.6 + (y - cy) * -0.8 > ry * 0.55 else SPORE[1])
        for (sx, sy) in ((-3, -3), (0, -4), (2, -2), (-1, -2)):
            x, y = int(cx + sx * d), int(round(cy + sy))
            if (x, y) in capm:
                c.set(x, y, PUFF[0])
    sp = P['specks'][0] if front else P['specks'][1]
    for sx, sy in sp:
        x, y = cx + sx * d, int(round(cy)) + sy - 1
        if (x, y) in body.px:
            c.set(x, y, SPORE[0] if c.get(x, y) == BODY[0] else SPORE[1])
    if front:
        ey = int(round(cy)) - (0 if P.get('cap') else 1)
        for ex in (cx + 1 * d, cx + 4 * d):
            if blink:
                c.set(ex, ey + 1, EYE_D2)
            else:
                c.set(ex, ey, EYE_D)
                c.set(ex, ey + 1, EYE_D)
    if puff:
        if puff == 3:     # a small breath of spores rising off the cap
            for (sx, sy, k) in ((1, -ry - 2, 0), (2, -ry - 4, 1), (0, -ry - 5, 2)):
                c.set(int(cx + sx * d), int(cy + sy), PUFF[k])
        elif puff in (4, 5):   # a sneeze: a cone of spores ahead, spreading
            n = 6 if puff == 4 else 9
            for k in range(n):
                sx = 7 + k * (1 if puff == 4 else 1.2)
                sy = ((k % 3) - 1) * (1 + k // 3)
                x, y = int(cx + sx * d), int(cy + sy)
                c.set(x, y, PUFF[(k + puff) % 3])
                if puff == 4 and k % 2 == 0:
                    c.set(x, y - 1, PUFF[0])
        else:
            r = 2.6 if puff == 2 else 2.0
            pcx, pcy = cx + (6 if puff == 2 else 7) * d, cy + (1 if puff == 2 else -2)
            cloud = Mask().ellipse(pcx, pcy, r, r * 0.85)
            if puff == 1:
                cloud.px = {p for p in cloud.px if (p[0] + p[1]) % 2 == 0 or p[1] < pcy - 1}
            shade(c, cloud, PUFF, cut=(-0.2, -0.8))
    if ring:
        for x in range(2, 22):
            if abs(x - 12) > (6 if i == 2 else 8) and (x % 2 == (i % 2)):
                c.set(x, 22 + (x % 3 == 0), PUFF[1] if x % 4 else PUFF[2])
        if i == 2:
            for x in (3, 20):
                c.set(x, 20, PUFF[0])
    return c


# ---------------------------------------------------------------------------------------------------------- build

def build(out=None):
    base_dir = out or os.path.join(TEMP, 'variants')
    main = load(os.path.join(TEMP, 'game_size', 'Yossele-Sheet.png'))
    assert (main.w, main.h) == (192, 256)
    parts = [Parts(cell(main, 0, v)) for v in range(4)]
    made = {}
    for vv in (1, 2, 3):
        out_v = os.path.join(base_dir, 'v%d' % vv)
        g = os.path.join(out_v, 'preview_8x')
        cells = []
        for v in range(4):
            d = 1 if v in (0, 2) else -1
            cells += [idle(parts[v], vv, d, i) for i in range(6)]
        for v in range(4):
            d = 1 if v in (0, 2) else -1
            cells += [walk(parts[v], vv, d, i) for i in range(6)]
        tags = [('idle_v%d' % v, v, 0, 6) for v in range(4)] + [('move_v%d' % v, 4 + v, 0, 6) for v in range(4)]
        write_sheet(out_v, 'Yossele-Sheet', cells, 6, 32, tags)
        atk = []
        for v in range(4):
            atk += attack(parts[v], vv, 1 if v in (0, 2) else -1)
        write_sheet(out_v, 'YosseleAttack-Sheet', atk, 4, 32, [('attack_v%d' % v, v, 0, 4) for v in range(4)])
        fc = []
        for v in range(4):
            fc += [friend(vv, v, 'idle', i) for i in range(6)]
            fc += [friend(vv, v, 'move', i) for i in range(6)]
            fc += [friend(vv, v, 'attack', i) for i in range(4)]
        ftags = []
        for v in range(4):
            ftags += [('idle_v%d' % v, v, 0, 6), ('move_v%d' % v, v, 6, 6), ('attack_v%d' % v, v, 12, 4)]
        write_sheet(out_v, 'Yosselefriend-Sheet', fc, 16, 24, ftags)
        gif([cells[v * 6:(v + 1) * 6] for v in range(4)], os.path.join(g, 'Yossele_idle.gif'), ms=125)
        gif([cells[24 + v * 6:24 + (v + 1) * 6] for v in range(4)], os.path.join(g, 'Yossele_move.gif'), ms=110)
        gif([atk[v * 4:(v + 1) * 4] for v in range(4)], os.path.join(g, 'Yossele_attack.gif'), ms=130)
        gif([fc[v * 16:v * 16 + 6] for v in range(4)], os.path.join(g, 'Yosselefriend_idle.gif'), k=8, ms=140)
        gif([fc[v * 16 + 6:v * 16 + 12] for v in range(4)], os.path.join(g, 'Yosselefriend_move.gif'), k=8, ms=110)
        gif([fc[v * 16 + 12:v * 16 + 16] for v in range(4)], os.path.join(g, 'Yosselefriend_attack.gif'), k=8, ms=140)
        made[vv] = (cells, atk, fc)
    return made


if __name__ == '__main__':
    build()
    print('Yossele variants v1-v3 written')
