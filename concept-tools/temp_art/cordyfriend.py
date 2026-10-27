"""Temporary art: Yossele's Cordyfriend (docs/scope/07: a tanky melee minion that "resembles a Snoble", raised by the
Mushroom Cannon's Cordyceps). A round, pale puffball with dot eyes like a Snoble, olive spore specks and three orange
cordyceps clubs growing from its crown. 24 px cells, bottom-centre pivot, PPU 20.

Sheet Yosselefriend-Sheet.png, 384 x 96: rows v0 v1 v2 v3 (front heading right, front heading left, back heading
right, back heading left); columns 0-5 Idle, 6-11 Move, 12-15 Attack.

    python3 concept-tools/temp_art/cordyfriend.py   -> TDS_Mod/ModSource/Yossele/Art/Temporary/
"""
import os
from tart import Canvas, Mask, H, shade, write_sheet, REPO

CELL = 24
BODY = (H('F2E8D0'), H('E3D3AE'), H('CDB88C'))
FOOT = (H('CDB88C'), H('B5A077'))
CLUB = (H('F7A64A'), H('E58A2E'), H('C46F22'))
TIP = H('FDC27A')
SPORE = (H('A3AC5E'), H('8A9649'))
PUFF = (H('DCDFAE'), H('C2C88A'), H('9AA552'))
EYE, EYE2 = H('333232'), H('4A4949')


def frame(facing, kind, i):
    """facing 0-3, kind 'idle' | 'move' | 'attack', i the frame."""
    c = Canvas(CELL, CELL)
    front = facing in (0, 1)
    d = 1 if facing in (0, 2) else -1          # heading: +1 screen right, -1 screen left
    dx = dy = 0
    sq = 0                                      # +1 squash (wider, lower), -1 stretch
    sway = 0                                    # club heads' lean, in pixels along x
    lift = (0, 0)                               # left, right foot raised
    blink = False
    puff = 0
    if kind == 'idle':
        sq = [0, 0, 1, 1, 1, 0][i]
        sway = [0, 0, 1, 1, 0, -1][i]
        blink = i == 4
    elif kind == 'move':
        dy = [0, -1, -1, 0, -1, -1][i]
        sq = [1, 0, 0, 1, 0, 0][i]
        lift = [(0, 0), (1, 0), (1, 0), (0, 0), (0, 1), (0, 1)][i]
        sway = [0, -1, -1, 0, -1, -1][i] * d
        dx = [0, 0, 1, 0, 0, 1][i] * d
    else:
        dx = [-1, -2, 2, 1][i] * d
        sq = [-1, -1, 1, 0][i]
        sway = [-1, -2, 2, 1][i] * d
        dy = [0, -1, 0, 0][i]
        puff = [0, 0, 2, 1][i]
        blink = i == 2

    cx = 12 + dx
    rx, ry = 7.0 + 0.5 * sq, 5.5 - 0.5 * sq
    bottom = 21.5 + dy
    cy = bottom - ry

    # Feet: two stubby nubs under the body.
    for k, fx in enumerate((cx - 4, cx + 2)):
        up = lift[k] if d > 0 else lift[1 - k]
        m = Mask().rect(fx, 21 - up, fx + 2, 23 - up)
        shade(c, m, FOOT[:2], cut=(-0.2, -9))

    # Clubs behind the crown's edge: left, centre (tallest), right. Each a 1 px stalk and a 2 x 3 head.
    crown = int(round(cy - ry)) + 1
    clubs = [(-3, 5, -1), (0, 7, 0), (3, 6, 1)]
    for ox, tall, lean in clubs:
        bx = cx + ox
        hx = bx + lean + sway
        top = crown - tall
        stalk = Mask().line(bx, crown + 1, hx, top + 3)
        shade(c, stalk, (CLUB[2], CLUB[2]), rim=False)
        head = Mask().rect(hx, top, hx + 1, top + 2)
        head.px.discard((hx + 1, top))           # rounded tip
        for (x, y) in head.px:
            c.set(x, y, CLUB[0] if x == hx else CLUB[1])
        c.set(hx, top, TIP)
        c.set(hx + 1, top + 2, CLUB[2])

    # The body: a puffball, shaded from the upper left.
    body = Mask().ellipse(cx, cy, rx, ry)
    shade(c, body, BODY, centre=(cx - 0.3, cy - 0.4), radii=(rx, ry), cut=(-0.12, -0.72))
    # A lighter crown where the clubs break through, and the spore specks.
    specks = [(-4, 0), (-3, 0), (-4, 1)] if front else [(-3, -1), (-2, -1), (-3, 0), (2, 2), (3, 2)]
    for sx, sy in specks:
        x, y = cx + sx * d, int(round(cy)) + sy - 1
        if (x, y) in body.px:
            c.set(x, y, SPORE[0] if c.get(x, y) == BODY[0] else SPORE[1])

    # Eyes (front facings only): 1 x 2 dark dots looking the way it heads, like the Snoble's.
    if front:
        ey = int(round(cy)) - 1
        for ex in (cx + 1 * d, cx + 4 * d):
            if blink:
                c.set(ex, ey + 1, EYE2)
            else:
                c.set(ex, ey, EYE)
                c.set(ex, ey + 1, EYE)

    # The attack's spore burst in front of it.
    if puff:
        # puff 2: a round cloud just landed in front; puff 1: the cloud drifting up, thinner.
        r = 2.6 if puff == 2 else 2.0
        pcx, pcy = cx + (6 if puff == 2 else 7) * d, cy + (1 if puff == 2 else -2)
        cloud = Mask().ellipse(pcx, pcy, r, r * 0.85)
        if puff == 1:
            cloud.px = {p for p in cloud.px if (p[0] + p[1]) % 2 == 0 or p[1] < pcy - 1}
        shade(c, cloud, PUFF, cut=(-0.2, -0.8))
        for sx, sy in ((2, -3), (-1, -4), (3, 0)):
            c.set(int(pcx + sx * d), int(pcy + sy), PUFF[2])
    return c


def build(out=None):
    out = out or os.path.join(REPO, 'TDS_Mod', 'ModSource', 'Yossele', 'Art', 'Temporary')
    cells = []
    for v in range(4):
        cells += [frame(v, 'idle', i) for i in range(6)]
        cells += [frame(v, 'move', i) for i in range(6)]
        cells += [frame(v, 'attack', i) for i in range(4)]
    tags = []
    for v in range(4):
        tags += [('idle_v%d' % v, v, 0, 6), ('move_v%d' % v, v, 6, 6), ('attack_v%d' % v, v, 12, 4)]
    return write_sheet(out, 'Yosselefriend-Sheet', cells, 16, CELL, tags)


if __name__ == '__main__':
    build()
    print('Yosselefriend-Sheet written')
