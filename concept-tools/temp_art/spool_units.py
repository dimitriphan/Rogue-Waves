"""Temporary animation: Spool's four Crafted, redrawn (2026-10-27) and rebased on Dimitri's own Spool sprite
(2026-10-28, "Base Spool's art on the sprite that I created"): his hood, his zipper mouth, his tongue and gloves, his
colours. The code's dolls (SpoolCharacter.DollSheet) are
flat, one tone per side, and only bob; these keep their shapes and colours but follow the house rules (kit docs/03
section 1, docs/18): light from the upper left, two or three tones per material, a real step (legs lift and swing,
the gloves swing against them) and an attack with a wind-up, a strike and a recover.

    Stitched  short and stubby, Spool in small: purple hood, the zipper mouth with the eyes looking out, white gloves
    Dart      thin and pointed, lilac, a needle in the leading glove that it throws
    Seam      round white felt with a purple seam and cross stitches down the front
    Mangled   torn white cotton with purple patches, mismatched eyes (one yellow, one a button), tufts sticking out

Each sheet is 160 x 64, 16 px cells, bottom-centre pivot: rows v0 v1 v2 v3 (front right, front left, back right,
back left); columns 0-5 Move (column 0 doubles as Idle), 6-9 Attack (SpoolAssets.Unit). Same names and sizes as the
code's sheets, so they drop in.

    python3 concept-tools/temp_art/spool_units.py   -> TDS_Mod/ModSource/Spool/Art/Temporary/
"""
import os
from tart import gif, Canvas, H, write_sheet, REPO

CELL = 16
PAL = {
    'a': H('93CCA1'), 'b': H('60A871'), 'c': H('3F7A4F'),        # plush, shade, deep
    'm': H('1F3325'), 'n': H('26422F'), 'e': H('E6D85A'),        # mouth, its edge, eyes
    'q': H('E58CC0'), 'p': H('C96CA6'),                          # tongue, tongue shade
    't': H('E4DEDA'), 'g': H('9A938E'),                          # teeth
    'v': H('F6F3F8'), 'u': H('D8D1E2'), 'U': H('ABBFB1'),        # felt, gloves and cotton
    'l': H('CBE6D1'), 'L': H('A2CFAD'), 'K': H('84BD92'),        # lilac (Dart)
    's': H('DCE3EA'), 'S': H('8C96A3'), 'w': H('FFFFFF'),        # needle, steel, flash
    'x': H('9AC9A5'),                                            # loose thread
}

# Body above the hips (row 13), facing right; gloves and legs are drawn per frame. '.' is empty.
DOLLS = {
    # Each is drawn from Dimitri's Spool sprite (his 2026-10-23 reshade) in his own colours: the hood rising to a point
    # at the upper right, the zipper mouth (upper teeth slanting down to the left, the dark opening with the yellow eyes,
    # the straight lower teeth), the pink tongue hanging at the right, the white gloves. Backs are a plain hood, no
    # stitch line (his note of 2026-10-12).
    'Stitched': dict(            # Spool in small
        front=[
            ".........ab.....",
            ".......aaaab....",
            "......aaatgtb...",
            ".....aatgmmmgg..",
            ".....agmemmen...",
            ".....tmmmmmmm...",
            ".....gtgtgtqp...",
            "......aaaabqb...",
            "......aaaabb....",
            "......aaabbb....",
        ],
        back=[
            ".........ab.....",
            ".......aaaab....",
            "......aaaaabb...",
            ".....aaaaaabbb..",
            ".....aaaaaabb...",
            ".....aaaaaabb...",
            ".....aaaaabbb...",
            "......aaaabbb...",
            "......aaaabb....",
            "......aaabbb....",
        ],
        top=4, legs=('a', 'b'), lx=(6, 9), gloves=((4, 10), (12, 10)), glove='vu'),
    'Dart': dict(                # thin and tall, the hood drawn out to a long point
        front=[
            "..........a.....",
            ".........ab.....",
            "........aab.....",
            ".......aaab.....",
            ".......aatgb....",
            "......atgmmg....",
            "......gmemen....",
            "......tgtgqp....",
            ".......abbqb....",
            ".......aab......",
            ".......aab......",
            ".......aab......",
            ".......abb......",
        ],
        back=[
            "..........a.....",
            ".........ab.....",
            "........aab.....",
            ".......aaab.....",
            ".......aaabb....",
            "......aaaabb....",
            "......aaaabb....",
            "......aaaabb....",
            ".......aabb.....",
            ".......aab......",
            ".......aab......",
            ".......aab......",
            ".......abb......",
        ],
        top=1, legs=('a', 'b'), lx=(6, 9), gloves=((5, 9), (11, 8)), glove='vu', needle=True),
    'Seam': dict(                # Spool sewn from white felt offcuts, a purple seam across the middle
        front=[
            "................",
            ".........vu.....",
            ".......vvvvu....",
            "......vvvtgtu...",
            ".....vvtgmmmgg..",
            ".....vgmemmen...",
            ".....vtgtgtgu...",
            ".....abababaU...",
            "......vvvvuU....",
            ".......uuUU.....",
        ],
        back=[
            "................",
            ".........vu.....",
            ".......vvvvu....",
            "......vvvvvuu...",
            ".....vvvvvvuuU..",
            ".....vvvvvvuU...",
            ".....vvvvvvuU...",
            ".....abababaU...",
            "......vvvvuU....",
            ".......uuUU.....",
        ],
        top=4, legs=('u', 'U'), lx=(6, 9), gloves=((4, 10), (12, 10)), glove='uU'),
    'Mangled': dict(             # torn: a scrap of the hood and half the zipper mouth on a lump of spilled cotton
        front=[
            "......w..w......",
            ".....wvwaab.....",
            "....vvaaatgb....",
            "...wvatgmmmgg...",
            "....vgmemmmn....",
            "...vvtgtgtqpw...",
            "....vvvbb.qU....",
            "...wvvvvvvuU....",
            "....vvvuuuuU....",
            ".....uuUUUU.....",
        ],
        back=[
            "......w..w......",
            ".....wvwaab.....",
            "....vvaaaabb....",
            "...wvaaaaabbw...",
            "....vvaaabbuU...",
            "...vvvvbbbuuUw..",
            "....vvvvvvuuU...",
            "...wvvvvvvuuU...",
            "....vvvuuuuU....",
            ".....uuUUUU.....",
        ],
        top=4, legs=('u', 'U'), lx=(5, 9), gloves=((3, 9), (12, 9)), glove='uU'),
}

HIPS = 13


def body(c, rows, top, dx, dy):
    for y, row in enumerate(rows):
        for x, k in enumerate(row):
            if k != '.':
                c.set(x + dx, top + y + dy, PAL[k])


def leg(c, x, hip, lift, step, tones):
    """A 2 px leg from under the hips to the ground; a lifted leg is a pixel shorter and a pixel along the step."""
    foot = 15 - lift
    for y in range(hip + 1, foot + 1):
        sx = x + (step if lift else 0)
        c.set(sx, y, PAL[tones[0]])
        c.set(sx + 1, y, PAL[tones[1]])


def glove(c, x, y, tones):
    c.set(x, y, PAL[tones[0]])
    c.set(x, y + 1, PAL[tones[1]])
    c.set(x + 1, y + 1, PAL[tones[1]])


def frame(name, back, kind, i):
    d = DOLLS[name]
    c = Canvas(CELL, CELL)
    rows = d['back'] if back else d['front']
    if kind == 'move':
        dy = [0, -1, -1, 0, -1, -1][i]
        lifts = [(0, 0), (1, 0), (0, 0), (0, 0), (0, 1), (0, 0)][i]
        swing = [0, -1, 0, 0, 1, 0][i]
        lean = [0, 1, 1, 0, 1, 1][i] if name != 'Seam' else 0
        reach, wind, fx = 0, 0, None
    else:
        dy = [1, 0, 0, 0][i]
        lifts = (0, 0)
        swing = 0
        lean = [-1, 1, 1, 0][i]
        wind = [1, 0, 0, 0][i]
        reach = [0, 2, 3, 1][i]
        fx = [None, 'smear', 'hit', None][i]
    hip = HIPS + dy
    # legs first (the far one darker when it lifts), then the body over the hip rows
    for k, x in enumerate(d['lx']):
        leg(c, x, hip, lifts[k], 1, d['legs'])
    body(c, rows, d['top'], lean, dy)
    # gloves: the trailing one (image left) swings against the steps; the leading one strikes
    (bx, by), (fxx, fy) = d['gloves']
    glove(c, bx + lean - wind, by + dy - swing, d['glove'])
    gx, gy = fxx + lean + reach - wind, fy + dy + swing - (1 if reach >= 2 else 0)
    if d.get('needle'):
        # the Dart: needle held up in the leading glove; wind-up raises it behind the head, the throw flings it
        if kind == 'move' or i == 3:
            for j in range(3):
                c.set(gx + 1, gy - 1 - j, PAL['s' if j else 'S'])
            c.set(gx + 1, gy - 4, PAL['w'])
        elif i == 0:
            gx, gy = 9 + lean, 4 + dy
            c.set(gx - 1, gy - 1, PAL['S'])
            c.set(gx - 2, gy - 2, PAL['s'])
        elif i == 1:
            gx, gy = 12 + lean, 6 + dy
            for j in range(3):
                c.set(gx + 1 + j, gy - 1, PAL['S' if j == 0 else 's'])
            c.set(gx + 4, gy - 1, PAL['w'])
        elif i == 2:
            gx, gy = 12 + lean, 8 + dy
            c.set(gx + 3, gy - 2, PAL['u'])
            c.set(gx + 4, gy - 2, PAL['u'])
        glove(c, gx, gy, d['glove'])
        return c
    glove(c, gx, gy, d['glove'])
    if fx == 'smear':
        for (x, y, k) in ((gx + 1, gy - 3, 'u'), (gx + 2, gy - 2, 'v'), (gx + 2, gy - 1, 'v')):
            c.over(x, y, PAL[k])
    elif fx == 'hit':
        sx, sy = gx + 3, gy
        for (x, y, k) in ((sx, sy, 'w'), (sx - 1, sy, 'e'), (sx + 1, sy, 'e'), (sx, sy - 1, 'e'), (sx, sy + 1, 'e')):
            c.set(x, y, PAL[k])
    return c


def build(out=None):
    out = out or os.path.join(REPO, 'TDS_Mod', 'ModSource', 'Spool', 'Art', 'Temporary')
    g = os.path.join(out, 'preview_8x')
    allrows = {}
    for name in DOLLS:
        cells = []
        for v in range(4):
            row = [frame(name, v >= 2, 'move', i) for i in range(6)] + [frame(name, v >= 2, 'attack', i) for i in range(4)]
            cells += [f.flip() if v in (1, 3) else f for f in row]
        tags = []
        for v in range(4):
            tags += [('move_v%d' % v, v, 0, 6), ('attack_v%d' % v, v, 6, 4)]
        sheet = 'Spool%s-Sheet' % name.lower()
        write_sheet(out, sheet, cells, 10, CELL, tags)
        allrows[name] = cells
    # one GIF per action: the four Crafted side by side, front-right view
    gif([allrows[n][0:6] for n in DOLLS], os.path.join(g, 'Spool_crafted_move.gif'), k=8, ms=110)
    gif([allrows[n][6:10] for n in DOLLS], os.path.join(g, 'Spool_crafted_attack.gif'), k=8, ms=130)
    return allrows


if __name__ == '__main__':
    build()
    print('Spoolstitched, Spooldart, Spoolseam and Spoolmangled sheets written')
