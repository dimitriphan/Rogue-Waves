"""Temporary animation: Spool's Thread Head, redrawn clean (2026-10-27). The code's version textured the ball with
dots, which read as dirt (kit docs/03 section 1: "simple shapes beat texture"). This one is a lilac ball of yarn with
three wraps, Spool's zipper mouth with its teeth, yellow eyes and tongue, white gloves on short arms, purple legs and
a loose end trailing to the ground. Same palette and layout as the code's sheet, so it drops in.

Sheet Spoolhead-Sheet.png, 240 x 96, 24 px cells, bottom-centre pivot: rows v0 v1 v2 v3; columns 0-5 Move (column 0
doubles as Idle), 6-9 Attack (SpoolAssets.Unit).

    python3 concept-tools/temp_art/spool_threadhead.py   -> TDS_Mod/ModSource/Spool/Art/Temporary/
"""
import os
from tart import gif, Canvas, Mask, H, shade, write_sheet, REPO

CELL = 24
BALL = (H('D5BEEC'), H('B292D6'), H('9A7CC4'))
WRAP = H('B292D6')
MOUTH, TEETH, TEETH2 = H('41174F'), H('E4DEDA'), H('9A938E')
EYE, TONGUE, TONGUE2 = H('E6D85A'), H('E58CC0'), H('C96CA6')
GLOVE = (H('F6F3F8'), H('D8D1E2'))
LEG = (H('9363AE'), H('784B94'))
SPARK = (H('FFFFFF'), H('E6D85A'))


def ring(cx, cy, rx, ry):
    """The one-pixel outline of an ellipse."""
    inside = Mask().ellipse(cx, cy, rx, ry).px
    return {(x, y) for (x, y) in inside if any((x + a, y + b) not in inside for a, b in ((1, 0), (-1, 0), (0, 1), (0, -1)))}


def frame(v, kind, i):
    c = Canvas(CELL, CELL)
    front = v in (0, 1)
    d = 1 if v in (0, 2) else -1
    dy, lift, swing, end, gape, reach, spark = 0, (0, 0), 0, 0, 0, 0, 0
    if kind == 'move':
        dy = [0, -1, -1, 0, -1, -1][i]
        lift = [(0, 0), (1, 0), (0, 0), (0, 0), (0, 1), (0, 0)][i]
        swing = [0, 1, 1, 0, -1, -1][i]
        end = [0, 1, 1, 0, -1, -1][i]
    else:
        dy = [0, -1, 0, 0][i]
        gape = [1, 1, 2, 0][i]
        reach = [-1, 2, 3, 1][i]
        spark = [0, 0, 1, 0][i]
        end = [-1, 1, 1, 0][i]
    cx, cy = 12, 11.5 + dy
    rx, ry = 7.0, 7.5

    # legs: two purple stubs, 2 px wide
    for k, lx in enumerate((cx - 3, cx + 1)):
        up = lift[k]
        m = Mask().rect(lx, 19 - up, lx + 1, 23 - up)
        for (x, y) in m.px:
            c.set(x, y, LEG[1] if x == lx + 1 else LEG[0])
        c.set(lx + 2, 23 - up, LEG[1]) if k == 1 else c.set(lx - 1, 23 - up, LEG[0])

    # the loose end, behind the ball on the trailing side, curling to the ground
    side = -d
    ex = int(cx + side * 7)
    pts = [(ex, int(cy) + 4), (ex + side, int(cy) + 6), (ex + side + (end if side > 0 else -end), int(cy) + 8),
           (ex + side, 21), (ex + side, 22), (ex + 2 * side, 23), (ex + 3 * side, 23)]
    for n, (x, y) in enumerate(pts):
        c.set(x, y, BALL[1])

    # the ball and its wraps
    ball = Mask().ellipse(cx, cy, rx, ry)
    shade(c, ball, BALL, centre=(cx - 0.5, cy - 0.6), radii=(rx, ry), cut=(-0.38, -0.92))
    wraps = [ring(cx + 3, cy - 7.5, 7.5, 5.0), ring(cx - 4, cy + 7.0, 7.0, 4.5)] if front else \
        [ring(cx + 3, cy - 7.5, 7.5, 5.0), ring(cx - 4, cy + 7.0, 7.0, 4.5), ring(cx - 1, cy, 9.0, 2.5)]
    mouth_rows = set(range(int(cy) - 2, int(cy) + 2 + gape)) if front else set()
    for wr in wraps:
        for (x, y) in wr:
            if (x, y) in ball.px and y not in mouth_rows and ((x + 1, y) in ball.px and (x, y + 1) in ball.px):
                c.set(x, y, WRAP if c.get(x, y) == BALL[0] else BALL[2])

    if front:
        top, bot = int(cy) - 2, int(cy) + 1 + gape
        x0, x1 = int(cx - rx) + 1, int(cx + rx) - 1
        for y in range(top, bot + 1):
            for x in range(x0, x1 + 1):
                if (x, y) in ball.px:
                    c.set(x, y, MOUTH)
        for x in range(x0, x1 + 1):                    # the zipper's teeth, upper and lower
            if (x, top) in ball.px:
                c.set(x, top, TEETH if x % 2 == 0 else TEETH2)
            if (x, bot) in ball.px:
                c.set(x, bot, TEETH if x % 2 == 1 else TEETH2)
        ey = top + 1 + (1 if gape >= 2 else 0)
        for ex2 in (int(cx) - 2 + d, int(cx) + 2 + d):
            c.set(ex2, ey, EYE)
        tx = int(cx) + 4 * d
        c.set(tx, bot, TONGUE)
        c.set(tx, bot + 1, TONGUE2 if gape else TONGUE)

    # arms and gloves: the leading glove does the attack
    for k in (-1, 1):
        lead = k == d
        gx = int(round(cx + k * (rx - 0.5))) + (reach * k if (lead and kind == 'attack') else 0)
        gy = int(cy) + 3 - (swing * k if kind == 'move' else 0) - (3 if lead and kind == 'attack' and reach < 0 else 0)
        ax = int(cx + k * rx)
        for x in range(min(ax, gx), max(ax, gx) + 1):
            if (x, gy) not in ball.px:
                c.set(x, gy, BALL[1])
        for (x, y) in ((gx, gy - 1), (gx + k, gy - 1), (gx, gy), (gx + k, gy)):
            c.set(x, y, GLOVE[1] if (y == gy and x == gx + k) else GLOVE[0])
        if lead and spark:
            sx, sy = gx + k, gy - 3
            for (x, y, t) in ((sx, sy, 0), (sx - 1, sy, 1), (sx + 1, sy, 1), (sx, sy - 1, 1), (sx, sy + 1, 1),
                              (sx - 2 * k, sy - 2, 0), (sx + k, gy + 2, 1)):
                c.set(x, y, SPARK[t])
    return c


def build(out=None):
    out = out or os.path.join(REPO, 'TDS_Mod', 'ModSource', 'Spool', 'Art', 'Temporary')
    cells = []
    for v in range(4):
        cells += [frame(v, 'move', i) for i in range(6)] + [frame(v, 'attack', i) for i in range(4)]
    tags = []
    for v in range(4):
        tags += [('head_move_v%d' % v, v, 0, 6), ('head_attack_v%d' % v, v, 6, 4)]
    write_sheet(out, 'Spoolhead-Sheet', cells, 10, CELL, tags)
    g = os.path.join(out, 'preview_8x')
    gif([cells[v * 10:v * 10 + 6] for v in range(4)], os.path.join(g, 'Spoolhead_move.gif'), ms=110)
    gif([cells[v * 10 + 6:v * 10 + 10] for v in range(4)], os.path.join(g, 'Spoolhead_attack.gif'), ms=120)
    return cells


if __name__ == '__main__':
    build()
    print('Spoolhead-Sheet written')
