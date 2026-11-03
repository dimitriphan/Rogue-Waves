"""Three alternative versions of each of Spool's temporary assets drawn as files (2026-10-28), for Dimitri to pick one per
asset ("Create 3 versions of each asset for now"; every animation gets its own versions).

Every version is a drop-in: same name, size and frame layout as the file in Art/Temporary/game_size/, which stays the
one wired on PR #42 until he picks. Built from his Spool sprite as the current set is (2026-10-28 rule): the Thread
Head always wears his zipper mouth pixel for pixel (spool_threadhead.MOUTH_STAMP), and the four Crafted keep their
bodies from spool_units.DOLLS (his hood, mouth, tongue, gloves and colours); the versions change how they move.

    Spoolhead-Sheet     v1 Criss-cross, hop and jab-jab     v2 Unravelling, waddle and bite   v3 Knitting needle, march and slam
    Crafted (x4)        v1 Hop and lunge                     v2 Waddle and slam                v3 Scamper and flurry
                        (Dart: v1 high lob, v2 sidearm fling, v3 stab then throw)
    Spool_Tuft          v1 Coiled loop                       v2 Snarl with two ends            v3 Cut snippets

    python3 concept-tools/temp_art/variants_spool.py   -> ModSource/Spool/Art/Temporary/variants/v1..v3/
"""
import os
from tart import gif, Canvas, Mask, H, shade, write_sheet, save_png, save_ase, preview, REPO
from spool_threadhead import BALL, WRAP, GLOVE, LEG, SPARK, MOUTH_STAMP, STAMP_PAL, ring
import spool_units as SU

TEMP = os.path.join(REPO, 'TDS_Mod', 'ModSource', 'Spool', 'Art', 'Temporary')
STEEL = (H('DCE3EA'), H('8C96A3'))
THREAD = (H('B58BC9'), H('945DAD'), H('7A4A8F'))
DUST = (H('D8D1E2'), H('B5ABC6'))
NAMES = {
    'Spoolhead-Sheet': ('Criss-cross, hop and jab-jab', 'Unravelling, waddle and bite', 'Knitting needle, march and slam'),
    'Crafted': ('Hop and lunge', 'Waddle and slam', 'Scamper and flurry'),
    'Spool_Tuft': ('Coiled loop', 'Snarl with two ends', 'Cut snippets'),
}


# ------------------------------------------------------------------------------------------------------ Thread Head

def head_frame(vv, v, kind, i):
    c = Canvas(24, 24)
    front = v in (0, 1)
    d = 1 if v in (0, 2) else -1
    dx = dy = sq = gape = 0
    lift = (0, 0)
    gl = {d: (0, 0), -d: (0, 0)}     # glove offsets (x along the heading, y) from rest, lead and trail
    spark = None
    trail = 0
    if kind == 'move':
        if vv == 1:      # hop: squash, spring, airborne with the legs tucked, land
            dy = [0, 1, -2, -3, -1, 1][i]
            sq = [0, 1, -1, -1, 0, 1][i]
            lift = [(0, 0), (0, 0), (2, 2), (3, 3), (1, 1), (0, 0)][i]
            gl = {d: (0, [0, 1, -2, -3, -1, 1][i]), -d: (0, [0, 1, -2, -3, -1, 1][i])}
        elif vv == 2:    # waddle: rolls from leg to leg, the gloves out for balance, the loose end dragging
            dx = [0, -1, -1, 0, 1, 1][i]
            dy = [0, -1, 0, 0, -1, 0][i]
            lift = [(0, 0), (0, 1), (0, 0), (0, 0), (1, 0), (0, 0)][i]
            gl = {d: (1, [0, -1, 0, 0, 1, 0][i]), -d: (1, [0, 1, 0, 0, -1, 0][i])}
            trail = i % 3
        else:            # march: high knees, the gloves pumping against them
            dy = [0, -1, 0, 0, -1, 0][i]
            lift = [(0, 0), (2, 0), (1, 0), (0, 0), (0, 2), (0, 1)][i]
            gl = {d: (0, [0, 2, 1, 0, -2, -1][i]), -d: (0, [0, -2, -1, 0, 2, 1][i])}
    else:
        if vv == 1:      # jab-jab: lead glove, then the other glove crosses over, sparks each time
            dy = [0, 0, -1, 0][i]
            dx = [0, d, d, 0][i]
            gl = {d: ([-1, 4, 1, 1][i], [-1, -1, 0, 0][i]), -d: ([0, -1, 9, 0][i], [0, 0, 3, 0][i])}
            spark = [None, 'lead', 'trail', None][i]
        elif vv == 2:    # bite: rear back, then lunge with the mouth thrown wide
            dx = [-1, 2, 2, 0][i] * d
            dy = [-1, 0, 0, 0][i]
            gape = [0, 2, 3, 0][i]
            gl = {d: ([-1, -2, -2, 0][i], [-2, 0, 0, 0][i]), -d: ([1, 0, 0, 0][i], [-2, 0, 0, 0][i])}
            spark = [None, None, 'mouth', None][i]
        else:            # slam: both gloves up over the ball, down in front, a burst on the ground
            dy = [1, -2, 1, 0][i]
            sq = [1, -1, 2, 0][i]
            gl = {d: ([-2, -3, 3, 1][i], [-9, -11, 3, 1][i]), -d: ([2, 3, 3, 1][i], [-9, -11, 3, 1][i])}
            spark = [None, None, 'ground', None][i]
    cx, cy = 12 + dx, 11.5 + dy + 0.5 * sq
    rx, ry = 7.0 + 0.5 * sq, 7.5 - 0.5 * sq

    # legs
    for k, lx in enumerate((cx - 3, cx + 1)):
        up = lift[k] if d > 0 else lift[1 - k]
        top = int(cy + ry) - 1
        for y in range(top, 24 - up):
            c.set(lx, y, LEG[0])
            c.set(lx + 1, y, LEG[1])
        if up >= 2:      # a high knee: the foot kicked forward
            c.set(lx + d, 23 - up, LEG[0])
        c.set(lx + 2, 23 - up, LEG[1]) if k == 1 else c.set(lx - 1, 23 - up, LEG[0])

    # the loose end
    side = -d
    if vv == 1:          # a curl over the top on the trailing side
        bx, by = int(cx + side * 3), int(cy - ry)
        for (x, y) in ((bx, by), (bx + side, by - 1), (bx + 2 * side, by - 1), (bx + 3 * side, by), (bx + 3 * side, by + 1)):
            c.set(x, y, BALL[1])
    elif vv == 2:        # a long strand dragging on the ground behind
        ex = int(cx + side * 7)
        pts = [(ex, int(cy) + 3), (ex + side, int(cy) + 5), (ex + side, int(cy) + 7), (ex + 2 * side, 21), (ex + 2 * side, 22)]
        pts += [(ex + (3 + n) * side, 23 - (n + trail) % 4 // 3) for n in range(6)]
        for (x, y) in pts:
            c.set(x, y, BALL[1] if x % 2 else BALL[2])
    else:                # the short end of the original, curling down
        ex = int(cx + side * 7)
        for (x, y) in ((ex, int(cy) + 4), (ex + side, int(cy) + 6), (ex + side, int(cy) + 8), (ex + 2 * side, int(cy) + 9)):
            c.set(x, y, BALL[1])
    if vv == 3:          # the knitting needle behind the ball, poking out top-back
        nx, ny = int(cx - d * 4), int(cy - ry) - 2
        for n in range(5):
            c.set(nx - d * (2 - n // 2), ny + n, STEEL[1] if n == 4 else STEEL[0])
        c.set(nx - d * 3, ny - 1, H('E6D85A'))

    ball = Mask().ellipse(cx, cy, rx, ry)
    shade(c, ball, BALL, centre=(cx - 0.5, cy - 0.6), radii=(rx, ry), cut=(-0.38, -0.92))
    if vv == 1:          # criss-cross wraps: two diagonal sets
        lines = set()
        for k in range(-14, 15, 7):
            for t in range(-8, 9):
                lines.add((int(cx + t), int(cy + t * 0.8 + k)))
                lines.add((int(cx + t), int(cy - t * 0.8 + k)))
    elif vv == 2:        # loose horizontal bands, one sagging
        lines = set()
        for yy in (-5, 5):
            for t in range(-8, 9):
                lines.add((int(cx + t), int(cy + yy + (abs(t) // 4 if yy > 0 else -abs(t) // 5))))
    else:
        lines = ring(cx + 3, cy - 7.5, 7.5, 5.0) | ring(cx - 4, cy + 7.0, 7.0, 4.5)
    mouth_rows = set(range(int(cy) - 4, int(cy) + 5 + gape)) if front else set()
    for (x, y) in lines:
        if (x, y) in ball.px and y not in mouth_rows and (x + 1, y) in ball.px and (x, y + 1) in ball.px:
            c.set(x, y, WRAP if c.get(x, y) == BALL[0] else BALL[2])
    if vv == 3:          # the needle's point coming out low at the front
        px_, py_ = int(cx + d * 5), int(cy + ry) - 3
        c.set(px_, py_, STEEL[0])
        c.set(px_ + d, py_ + 1, STEEL[0])
        c.set(px_ + 2 * d, py_ + 2, H('FFFFFF'))

    if front:
        rows = list(MOUTH_STAMP)
        for _ in range(gape):
            rows.insert(4, rows[4])
        x0, y0 = int(cx) - 6, int(cy) - 4
        for yy, row in enumerate(rows):
            for xx, k in enumerate(row):
                if k in STAMP_PAL:
                    x = x0 + xx if d > 0 else x0 + (len(row) - 1 - xx) + 1
                    c.set(x, y0 + yy, STAMP_PAL[k])

    # arms and gloves
    for k in (-1, 1):
        ox, oy = gl[k]
        gx = int(round(cx + k * (rx - 0.5))) + ox * d
        gy = int(cy) + 3 + oy
        ax = int(cx + k * rx)
        if abs(gy - (int(cy) + 3)) <= 3:
            for x in range(min(ax, gx), max(ax, gx) + 1):
                if (x, gy) not in ball.px:
                    c.set(x, gy, BALL[1])
        else:
            for y in range(min(gy, int(cy)), max(gy, int(cy)) + 1):
                xx = gx + (k if k != d else 0)
                if (xx, y) not in ball.px:
                    c.set(xx, y, BALL[1])
        for (x, y) in ((gx, gy - 1), (gx + k, gy - 1), (gx, gy), (gx + k, gy)):
            c.set(x, y, GLOVE[1] if (y == gy and x == gx + k) else GLOVE[0])
        lead = k == d
        if (spark == 'lead' and lead) or (spark == 'trail' and not lead):
            sx, sy = gx + 2 * d, gy - 2
            for (x, y, t) in ((sx, sy, 0), (sx - 1, sy, 1), (sx + 1, sy, 1), (sx, sy - 1, 1), (sx, sy + 1, 1)):
                c.set(x, y, SPARK[t])
    if spark == 'mouth' and front:
        sx, sy = int(cx) + 7 * d, int(cy)
        for (x, y, t) in ((sx, sy - 2, 0), (sx + d, sy - 2, 1), (sx, sy + 2, 1), (sx + d, sy + 3, 0), (sx + 2 * d, sy, 1)):
            c.set(x, y, SPARK[t])
    if spark == 'ground':
        for (x, y, t) in ((cx + 9 * d, 22, 0), (cx + 8 * d, 21, 1), (cx + 10 * d, 21, 1), (cx + 11 * d, 23, 1),
                          (cx + 7 * d, 23, 1), (cx + 9 * d, 19, 1)):
            c.set(int(x), y, SPARK[t])
    return c


# ---------------------------------------------------------------------------------------------------------- Crafted

def crafted_frame(vv, name, back, kind, i):
    D = SU.DOLLS[name]
    c = Canvas(16, 16)
    rows = D['back'] if back else D['front']
    (bx, by), (lx, ly) = D['gloves']
    lifts, dy, lean = (0, 0), 0, 0
    trail, lead = (0, 0), (0, 0)
    fx = None
    needle = None        # (x, y, 'up' | 'flat' | 'fly' | 'lob') for the Dart
    if kind == 'move':
        if vv == 1:      # hop on both feet, gloves up in the air
            dy = [0, 1, -1, -2, -1, 0][i]
            lifts = [(0, 0), (0, 0), (1, 1), (1, 1), (0, 0), (0, 0)][i]
            trail = lead = (0, [0, 1, -2, -2, -1, 0][i])
        elif vv == 2:    # waddle: leans onto each planted foot
            lean = [0, -1, -1, 0, 1, 1][i]
            dy = [0, 0, -1, 0, 0, -1][i]
            lifts = [(0, 0), (0, 1), (0, 1), (0, 0), (1, 0), (1, 0)][i]
            trail, lead = (0, [0, -1, 0, 0, 1, 0][i]), (0, [0, 1, 0, 0, -1, 0][i])
        else:            # scamper: tipped forward, quick steps, gloves trailing back
            lean = 1
            dy = [0, -1, 0, -1, 0, -1][i]
            lifts = [(1, 0), (1, 0), (0, 1), (0, 1), (1, 0), (0, 1)][i]
            trail, lead = (-1, [0, -1, 0, -1, 0, -1][i]), (-2, [0, -1, 0, -1, 0, -1][i])
    else:
        if vv == 1:      # lunge: wind back, spring in with a long reach, the hit, recover
            lean = [-1, 1, 1, 0][i]
            dy = [1, 0, 0, 0][i]
            lead = ([-2, 1, 2, 1][i], [0, -1, -1, 0][i])
            trail = ([-1, 0, 0, 0][i], 0)
            fx = [None, 'smear', 'hit', None][i]
        elif vv == 2:    # slam: both gloves over the head, down to the ground in front, dust
            dy = [1, -1, 1, 0][i]
            lean = [0, -1, 1, 0][i]
            lead = ([-1, 0, 2, 1][i], [-4, -6, 4, 0][i])
            trail = ([0, 0, 7, 0][i], [-4, -6, 4, 0][i])
            fx = [None, None, 'dust', None][i]
        else:            # flurry: jab with one glove, then the other, a spark each
            dy = [0, 0, 0, 0][i]
            lean = [0, 1, 1, 0][i]
            lead = ([0, 3, -1, 1][i], [0, -1, 0, 0][i])
            trail = ([0, 0, 9, 0][i], [0, 0, -2, 0][i])
            fx = [None, 'hit', 'hit2', None][i]
    P0 = SU.PAL
    hip = SU.HIPS + dy
    for k, x in enumerate(D['lx']):
        SU.leg(c, x + (lean if vv == 3 and kind == 'move' else 0), hip, lifts[k], 1, D['legs'])
    SU.body(c, rows, D['top'], lean, dy)
    tg = (bx + lean + trail[0], by + dy + trail[1])
    lg = (lx + lean + lead[0], ly + dy + lead[1])
    if D.get('needle'):
        # the Dart's throw in three ways; the needle is drawn with the leading glove
        if kind == 'move' or i == 3:
            needle = (lg[0] + 1, lg[1] - 1, 'up')
        elif vv == 1:    # a high lob: needle back over the head, flung up, a lob arc
            if i == 0:
                lg = (8 + lean, 2 + dy)
                needle = (lg[0] - 1, lg[1] - 1, 'back')
            elif i == 1:
                lg = (11 + lean, 2 + dy)
                needle = (lg[0] + 1, lg[1] - 1, 'up')
            else:
                lg = (12 + lean, 6 + dy)
                needle = (14, 1, 'lob')
        elif vv == 2:    # sidearm: needle held flat behind, swung round, flung flat with a streak
            if i == 0:
                lg = (4 + lean, 9 + dy)
                needle = (lg[0] - 3, lg[1] - 1, 'flat')
            elif i == 1:
                lg = (10 + lean, 9 + dy)
                needle = (lg[0] + 1, lg[1] - 1, 'flat')
            else:
                lg = (12 + lean, 9 + dy)
                needle = (14, 8 + dy, 'fly')
        else:            # stab with the needle, then let it fly
            if i == 0:
                lg = (lx + lean - 1, ly + dy)
                needle = (lg[0] + 1, lg[1] - 1, 'up')
            elif i == 1:
                lg = (12 + lean, 8 + dy)
                needle = (lg[0] + 2, lg[1], 'flat')
                fx = 'hit3'
            else:
                lg = (11 + lean, 8 + dy)
                needle = (15, 7 + dy, 'fly')
        fx = fx if fx == 'hit3' else None
    tg = (max(0, min(13, tg[0])), tg[1])
    lg = (max(0, min(14, lg[0])), lg[1])
    for (gx, gy) in (tg, lg):      # raised gloves keep an arm down to the body
        if gy + 2 < D['top'] + dy + 3:
            y = gy + 2
            while y < min(16, gy + 6) and not c.get(gx, y)[3]:
                c.set(gx, y, P0[D['legs'][1]])
                y += 1
    SU.glove(c, tg[0], tg[1], D['glove'])
    if needle:
        nx, ny, how = needle
        P = SU.PAL
        if how == 'up':
            for j in range(3):
                c.set(nx, ny - j, P['s' if j else 'S'])
            c.set(nx, ny - 3, P['w'])
        elif how == 'back':
            c.set(nx, ny, P['S'])
            c.set(nx - 1, ny - 1, P['s'])
            c.set(nx - 2, ny - 2, P['w'])
        elif how == 'flat':
            c.set(nx, ny, P['S'])
            for j in (1, 2):
                c.set(nx + j, ny, P['s'])
            c.set(nx + 3, ny, P['w'])
        elif how == 'fly':
            c.set(nx, ny, P['w'])
            c.set(nx - 1, ny, P['s'])
            for j in (2, 3, 5):
                c.set(nx - j, ny, P['x'])
        elif how == 'lob':
            c.set(nx, ny, P['w'])
            c.set(nx - 1, ny + 1, P['s'])
            for (x, y) in ((nx - 2, ny + 3), (nx - 3, ny + 5)):
                c.set(x, y, P['x'])
    SU.glove(c, lg[0], lg[1], D['glove'])
    P = SU.PAL
    if fx == 'smear':
        for (x, y, k) in ((lg[0] - 1, lg[1] - 2, 'u'), (lg[0] - 2, lg[1] - 1, 'u')):
            c.over(x, y, P[k])
    elif fx in ('hit', 'hit3'):
        sx, sy = min(lg[0] + 3, 14), lg[1]
        for (x, y, k) in ((sx, sy, 'w'), (sx - 1, sy, 'e'), (sx + 1, sy, 'e'), (sx, sy - 1, 'e'), (sx, sy + 1, 'e')):
            c.set(x, y, P[k])
    elif fx == 'hit2':
        sx, sy = tg[0] + 2, tg[1] - 1
        for (x, y, k) in ((sx, sy, 'w'), (sx + 1, sy - 1, 'e'), (sx + 1, sy + 1, 'e'), (sx - 1, sy - 1, 'e')):
            c.set(x, y, P[k])
    elif fx == 'dust':
        for (x, y, k) in ((15, 15, 'u'), (14, 14, 'v'), (13, 15, 'U'), (15, 12, 'u'), (11, 14, 'u'), (12, 13, 'e')):
            c.set(x, y, P[k])
    return c


# ------------------------------------------------------------------------------------------------------------- Tuft

TUFTS = {
    1: [   # a coiled loop of lilac thread, the end trailing off
        "............",
        "....jjjx....",
        "...jx...xx..",
        "..jx.jjx.xX.",
        "..j.jx..x.X.",
        "..j.x.jx.xX.",
        "..x.xX..XX..",
        "..xX.xXXX.X.",
        "...xX....X..",
        "....XXXXX...",
        "..........X.",
        "...........X",
    ],
    2: [   # a snarl: a tangled knot with two loose ends
        ".j..........",
        "..j...jx....",
        "..xjjxjjx...",
        "..jxjxxjxX..",
        ".jxjxjxxjX..",
        "..xxjxXxXX..",
        "..XjxXxjXX..",
        "...XXxXXX...",
        "....XX.X....",
        ".......X....",
        "........X...",
        "........XX..",
    ],
    3: [   # snippets: three cut curls of thread lying on the ground
        "............",
        ".jx.........",
        "j..x....jx..",
        "j..X...j..x.",
        ".xX....x..X.",
        "........XX..",
        "............",
        "...jjx......",
        "..j...x.....",
        "..x...X..jx.",
        "...XXX..x..X",
        ".........XX.",
    ],
}
TUFT_PAL = {'j': H('B58BC9'), 'x': H('945DAD'), 'X': H('7A4A8F')}


# ------------------------------------------------------------------------------------------------------------ build

def build(out=None):
    base_dir = out or os.path.join(TEMP, 'variants')
    made = {}
    for vv in (1, 2, 3):
        o = os.path.join(base_dir, 'v%d' % vv)
        g = os.path.join(o, 'preview_8x')
        cells = []
        for v in range(4):
            cells += [head_frame(vv, v, 'move', i) for i in range(6)] + [head_frame(vv, v, 'attack', i) for i in range(4)]
        tags = []
        for v in range(4):
            tags += [('head_move_v%d' % v, v, 0, 6), ('head_attack_v%d' % v, v, 6, 4)]
        write_sheet(o, 'Spoolhead-Sheet', cells, 10, 24, tags)
        gif([cells[v * 10:v * 10 + 6] for v in range(4)], os.path.join(g, 'Spoolhead_move.gif'), ms=110)
        gif([cells[v * 10 + 6:v * 10 + 10] for v in range(4)], os.path.join(g, 'Spoolhead_attack.gif'), ms=130)
        allrows = {}
        for name in SU.DOLLS:
            cc = []
            for v in range(4):
                row = [crafted_frame(vv, name, v >= 2, 'move', i) for i in range(6)] + \
                      [crafted_frame(vv, name, v >= 2, 'attack', i) for i in range(4)]
                cc += [f.flip() if v in (1, 3) else f for f in row]
            t = []
            for v in range(4):
                t += [('move_v%d' % v, v, 0, 6), ('attack_v%d' % v, v, 6, 4)]
            write_sheet(o, 'Spool%s-Sheet' % name.lower(), cc, 10, 16, t)
            allrows[name] = cc
        gif([allrows[n][0:6] for n in SU.DOLLS], os.path.join(g, 'Spool_crafted_move.gif'), k=8, ms=110)
        gif([allrows[n][6:10] for n in SU.DOLLS], os.path.join(g, 'Spool_crafted_attack.gif'), k=8, ms=140)
        rows = TUFTS[vv]
        assert len(rows) == 12 and all(len(r) == 12 for r in rows), 'tuft v%d' % vv
        tc = Canvas(12, 12)
        tc.stamp(rows, TUFT_PAL)
        save_png(tc, os.path.join(o, 'game_size', 'Spool_Tuft.png'))
        save_png(preview(tc, 12, 8), os.path.join(g, 'Spool_Tuft_8x.png'))
        save_ase(os.path.join(o, 'aseprite', 'Spool_Tuft.aseprite'), 12, [tc], [('tuft', 0, 0, 'TEMP;icon=Spool_Tuft.png')])
        made[vv] = (cells, allrows, tc)
    return made


if __name__ == '__main__':
    build()
    print('Spool variants v1-v3 written')
