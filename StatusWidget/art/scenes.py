"""The status widget's scenes: 8 states, 12 frames each, 128 x 80 px, drawn from the vessels' real sprites.

Sources (read only): Yossele's final sheets (`ModSource/Yossele/Art`), Argo's shipped idle and sonar sheets
(`ModSource/Argo/Concept/assets`), Spool's sage sprite (`Spool/Art/Temporary/pastel_green_2026-11-05`) and Uloe's concept
frame as embedded in `Assets/UloeMod/Editor/UloeCharacter.cs`. Colours: docs/themes/rogue-tide.md.

    python3 -I StatusWidget/art/scenes.py
        -> StatusWidget/app/src/main/res/drawable-nodpi/scene_<state>_<nn>.png and head_<name>.png (1x),
           StatusWidget/app/src/main/res/drawable/f_<state>_<nn>.xml and h_<name>.xml (nearest-neighbour wrappers),
           StatusWidget/art/preview/<state>.gif (3x) and contact.png
"""
import math
import os
import re
import sys

from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.abspath(os.path.join(HERE, '..', '..'))
RES = os.path.join(REPO, 'StatusWidget', 'app', 'src', 'main', 'res')
OUT = os.path.join(HERE, 'preview')
W, H, K, N = 128, 80, 3, 12
os.makedirs(OUT, exist_ok=True)


def hx(s, a=255):
    s = s.lstrip('#')
    return (int(s[0:2], 16), int(s[2:4], 16), int(s[4:6], 16), a)


SWELL, SWELL2, NIGHT = hx('2E4A62'), hx('35546D'), hx('1E3347')
TEAL, TEAL_HI, TEAL_LO = hx('3F8F96'), hx('4A9AA1'), hx('377A80')
FOAM, MIST, CORAL, SAND, SLATE = hx('E9F1EE'), hx('B4C8CF'), hx('E2725B'), hx('D8C49A'), hx('5B5D69')
WOOD, WOOD_LO, WOOD_HI = hx('7A5236'), hx('5C3D28'), hx('9A6B45')
DEEP, DEEPER = hx('245E6B'), hx('1B4756')
SAGE_PALE, SAGE_DK = hx('C9E3CF'), hx('60A871')
LASER, LASER_HI = hx('8FE36A'), hx('E6FFD0')
BULB, BULB_HI = hx('FBF236'), hx('FFFDE0')


def sheet_cells(path, cols, rows, cell=32):
    im = Image.open(path).convert('RGBA')
    return [[im.crop((c * cell, r * cell, (c + 1) * cell, (r + 1) * cell)) for c in range(cols)] for r in range(rows)]


ART = os.path.join(REPO, 'TDS_Mod', 'ModSource')
yos_idle = sheet_cells(os.path.join(ART, 'Yossele/Art/Yossele-Sheet.png'), 6, 8)[0]
yos_attack = sheet_cells(os.path.join(ART, 'Yossele/Art/YosseleAttack-Sheet.png'), 4, 4)[0]
argo_idle = sheet_cells(os.path.join(ART, 'Argo/Concept/assets/Hero_Argo_IdleMove.png'), 6, 8)[0]
argo_sonar = sheet_cells(os.path.join(ART, 'Argo/Concept/assets/Hero_Argo_Sonar.png'), 6, 4)[0]
spool = Image.open(os.path.join(ART, 'Spool/Art/Temporary/pastel_green_2026-11-05/game_size/Spool_Pastel.png')).convert('RGBA')

# Uloe: his concept frame as embedded in UloeCharacter.cs (Front + palette P)
src = open(os.path.join(REPO, 'TDS_Mod/Assets/UloeMod/Editor/UloeCharacter.cs')).read()
pal = {k: hx(v) for k, v in re.findall(r"\{ '(\w)', Col\.H\(\"([0-9A-Fa-f]{6})\"\) \}", src)}
front = re.search(r'static readonly string\[\] Front =\s*\{(.*?)\};', src, re.S).group(1)
rows = re.findall(r'"([^"]{32})"', front)
uloe = Image.new('RGBA', (32, 32))
for y, row in enumerate(rows):
    for x, k in enumerate(row):
        if k != '.':
            uloe.putpixel((x, y), pal[k])


def blank(col):
    return Image.new('RGBA', (W, H), col)


def px(im, x, y, c):
    if 0 <= x < W and 0 <= y < H:
        im.putpixel((int(x), int(y)), c)


def rect(im, x0, y0, x1, y1, c):
    for y in range(int(y0), int(y1) + 1):
        for x in range(int(x0), int(x1) + 1):
            px(im, x, y, c)


def line(im, x0, y0, x1, y1, c):
    n = int(max(abs(x1 - x0), abs(y1 - y0))) or 1
    for i in range(n + 1):
        px(im, round(x0 + (x1 - x0) * i / n), round(y0 + (y1 - y0) * i / n), c)


def put(im, spr, cx, foot):
    """Paste a 32 px sprite so its bottom-centre pivot (x 16, row 31) lands at (cx, foot)."""
    im.alpha_composite(spr, (int(cx) - 16, int(foot) - 31))


def sky(im, f, night=True):
    top = NIGHT if night else SWELL
    rect(im, 0, 0, W - 1, 53, top)
    rect(im, 0, 30, W - 1, 53, SWELL if night else SWELL2)
    if night:
        for i, (sx, sy) in enumerate([(9, 6), (27, 14), (44, 4), (70, 9), (88, 17), (104, 5), (120, 12), (58, 20)]):
            px(im, sx, sy, FOAM if (f + i) % 4 else MIST)
        rect(im, 108, 4, 112, 8, SAND)          # moon
        px(im, 108, 4, NIGHT); px(im, 112, 8, SAND)


def sea(im, f, y0=54, speed=1):
    rect(im, 0, y0, W - 1, H - 1, TEAL)
    rect(im, 0, y0 + 9, W - 1, H - 1, TEAL_LO)
    for x in range(W):
        ph = (x + f * speed) % 16
        if ph < 3:
            px(im, x, y0, FOAM)
        elif ph < 6:
            px(im, x, y0, TEAL_HI)
        if (x * 7 + f * speed) % 23 == 0:
            px(im, x, y0 + 5, TEAL_HI); px(im, x + 1, y0 + 5, TEAL_HI)


def boat(im, x0, x1, deck, mast=False, f=0):
    """Hull from x0 to x1, deck row `deck`; the hull sits in the sea."""
    for i, y in enumerate(range(deck, deck + 9)):
        inset = max(0, i - 4)
        rect(im, x0 + inset, y, x1 - inset, y, WOOD if i % 3 else WOOD_LO)
    rect(im, x0, deck, x1, deck, SAND)
    for x in range(x0 + 5, x1 - 4, 9):                       # portholes
        px(im, x, deck + 3, SAND); px(im, x + 1, deck + 3, SAND)
    rect(im, x0 + 4, deck + 7, x1 - 4, deck + 8, WOOD_LO)
    if mast:
        mx = (x0 + x1) // 2
        rect(im, mx, deck - 44, mx, deck - 1, WOOD_HI)
        for i in range(17):                                   # sail, above the crew's heads
            w = 4 + i
            rect(im, mx + 1, deck - 43 + i, mx + w, deck - 43 + i, FOAM if i % 6 else TEAL_HI)
        rect(im, mx - 12, deck - 27, mx + 22, deck - 27, WOOD_HI)  # boom
        wave = [0, 1, 1, 0, -1, -1][f % 6]                    # pennant
        for i in range(6):
            px(im, mx - 1 - i, deck - 44 + (wave if i > 2 else 0), CORAL)
            if i < 4:
                px(im, mx - 1 - i, deck - 43 + (wave if i > 2 else 0), CORAL)


FONT = {
    'Y': ['101', '101', '010', '010', '010'], 'O': ['111', '101', '101', '101', '111'],
    'U': ['101', '101', '101', '101', '111'], 'R': ['110', '101', '110', '101', '101'],
    'T': ['111', '010', '010', '010', '010'], 'N': ['101', '111', '111', '101', '101'],
    ' ': ['000'] * 5, 'Z': ['111', '001', '010', '100', '111'], 'z': ['11', '01', '10', '11'],
}


def text(im, s, x, y, c):
    for ch in s:
        g = FONT[ch]
        for gy, row in enumerate(g):
            for gx, b in enumerate(row):
                if b == '1':
                    px(im, x + gx, y + gy, c)
        x += len(g[0]) + 1


def bob(f, amp=1):
    return [0, 0, 1, 1, 1, 0, 0, 0, -amp + 1, 0, 0, 0][f % 12] if amp else 0


DECK = 47
CAST = [(28, 'yos'), (50, 'spool'), (74, 'uloe'), (98, 'argo')]


def crew(im, f, b, hop=None, sleepy=False):
    for i, (cx, who) in enumerate(CAST):
        dy = -2 if hop is not None and (f // 2) % 4 == i else 0
        spr = {'yos': yos_idle[f % 6], 'spool': spool, 'uloe': uloe, 'argo': argo_idle[f % 6]}[who]
        if who in ('spool', 'uloe') and not sleepy and f % 4 < 2:
            dy -= 0
        put(im, spr, cx, DECK - 1 + b + dy)
        if sleepy:                                            # Zs float up, staggered
            t = (f + i * 3) % 12
            zx, zy = cx + 5 + t // 4, DECK - 26 - t
            if t < 10:
                text(im, 'z', zx, zy + b, FOAM if t < 7 else MIST)


def s_idle(f):
    im = blank(NIGHT); sky(im, f, True); sea(im, f // 2)
    b = bob(f)
    boat(im, 12, 115, DECK + b)
    crew(im, f, b, sleepy=True)
    sea_front(im, f // 2)
    return im


def sea_front(im, f):
    for x in range(W):
        if (x + f) % 16 < 2:
            px(im, x, 58, TEAL_HI)


def s_build(f):
    im = blank(SWELL); sky(im, f, False); sea(im, f // 2)
    boat(im, 12, 115, DECK)
    put(im, spool, 52, DECK - 1 - (f % 4 < 2))
    # the patch on the deck and its stitches, one more every frame
    rect(im, 66, DECK - 9, 92, DECK - 1, SAND)
    rect(im, 66, DECK - 9, 92, DECK - 9, WOOD_HI)
    for i in range(min(f + 1, 12)):
        sx = 68 + i * 2
        px(im, sx, DECK - 5, SAGE_DK)
    nx = 68 + min(f, 11) * 2
    ny = DECK - 5 - [0, 3, 5, 3][f % 4]
    line(im, nx, ny, nx, ny - 4, FOAM)                       # needle
    line(im, 59, DECK - 14, nx, ny - 4, SAGE_PALE)            # thread from Spool's glove
    return im


def s_test(f):
    im = blank(DEEP)
    rect(im, 0, 0, W - 1, 6, SWELL)                            # air above
    for x in range(W):
        px(im, x, 7, FOAM if (x + f) % 10 < 3 else TEAL_HI)
    rect(im, 0, 8, W - 1, 40, TEAL_LO)
    rect(im, 0, 41, W - 1, 62, DEEP)
    rect(im, 0, 63, W - 1, H - 1, DEEPER)
    rect(im, 30, 0, 86, 6, WOOD); rect(im, 33, 7, 83, 9, WOOD_LO)   # hull from below
    for i, rx in enumerate((18, 52, 96)):                       # light shafts
        for y in range(10, 60, 2):
            px(im, rx + y // 6 + (f % 2), y, TEAL)
    rect(im, 0, 74, W - 1, H - 1, SAND)                         # sea floor
    for x in range(0, W, 7):
        px(im, x + 2, 73, SAND)
    dy = [0, 1, 2, 2, 1, 0, 0, 1, 2, 2, 1, 0][f]
    put(im, argo_sonar[f % 6], 64, 70 + dy - 6)
    line(im, 64, 10, 64, 70 + dy - 34, MIST)                    # the rope to the boat
    for i in range(3):                                         # bubbles
        t = (f + i * 4) % 12
        px(im, 70 + (t % 3 == 0), 70 + dy - 36 - t * 2, FOAM)
    fx = (f * 6 + 10) % (W + 20) - 10                           # a fish swims by
    rect(im, fx, 52, fx + 3, 53, CORAL); px(im, fx - 1, 52, CORAL); px(im, fx - 1, 53, CORAL); px(im, fx + 3, 52, FOAM)
    return im


def s_review(f):
    im = blank(SWELL); sky(im, f, False); sea(im, f // 2)
    boat(im, 12, 115, DECK)
    put(im, yos_attack[(f // 2) % 4 if f % 6 < 4 else 0], 34, DECK - 1)
    # the scroll: six lines, the laser runs down them, a tick per line done
    rect(im, 64, 14, 98, DECK - 2, SAND)
    rect(im, 62, 12, 100, 14, WOOD_HI); rect(im, 62, DECK - 2, 100, DECK - 1, WOOD_HI)
    done = f // 2
    for i in range(6):
        y = 18 + i * 4
        rect(im, 67, y, 67 + [24, 20, 26, 18, 22, 14][i], y, WOOD_LO)
        if i < done:
            px(im, 95, y, SAGE_DK); px(im, 96, y + 1, SAGE_DK); px(im, 97, y, SAGE_DK); px(im, 98, y - 1, SAGE_DK)
    ly = 18 + min(done, 5) * 4
    if f % 6 >= 2:
        line(im, 40, DECK - 18, 66, ly, LASER)
        px(im, 66, ly, LASER_HI); px(im, 67, ly, LASER_HI)
    return im


def s_art(f):
    im = blank(NIGHT); sky(im, f, True); sea(im, f // 2)
    boat(im, 12, 115, DECK)
    glow = f % 4 < 2
    if glow:
        for a in range(0, 360, 30):
            px(im, 40 + round(9 * math.cos(math.radians(a))), DECK - 17 + round(8 * math.sin(math.radians(a))), BULB)
    put(im, uloe, 40, DECK - 1)
    if glow:
        rect(im, 39, DECK - 18, 41, DECK - 16, BULB_HI)
    # the easel and its canvas, a wave drawn in light pixels as the frames go
    line(im, 70, DECK - 1, 76, DECK - 30, WOOD_HI); line(im, 94, DECK - 1, 88, DECK - 30, WOOD_HI)
    rect(im, 68, DECK - 30, 96, DECK - 12, FOAM)
    rect(im, 68, DECK - 12, 96, DECK - 11, WOOD)
    pts = [(70 + i, DECK - 20 + round(4 * math.sin(i / 3.2))) for i in range(25)]
    for i, (x, y) in enumerate(pts[:2 + f * 2]):
        px(im, x, y, TEAL if i % 5 else CORAL)
        px(im, x, y + 1, TEAL_LO)
    if f % 3 == 0:                                               # a spark jumps to the canvas
        line(im, 50, DECK - 9, 67, DECK - 18 - (f % 2) * 3, BULB)
    return im


def s_wait(f):
    im = blank(SWELL); sky(im, f, False); sea(im, f // 2)
    boat(im, 12, 115, DECK)
    crew(im, f, 0, hop=True)
    # the sign held up over the crew
    rect(im, 30, 4, 98, 16, FOAM)
    rect(im, 30, 4, 98, 4, CORAL); rect(im, 30, 16, 98, 16, CORAL); rect(im, 30, 4, 30, 16, CORAL); rect(im, 98, 4, 98, 16, CORAL)
    text(im, 'YOUR TURN', 46, 8, SWELL)
    for sx in (40, 88):
        rect(im, sx, 17, sx, 22, WOOD_HI)
    return im


def s_ship(f):
    im = blank(SWELL); sky(im, f, False)
    for i, (cx, cy) in enumerate([(20, 8), (70, 14), (110, 6)]):  # clouds drift left
        x = (cx - f * 2) % (W + 20) - 10
        rect(im, x, cy, x + 12, cy + 2, MIST); rect(im, x + 3, cy - 1, x + 8, cy - 1, MIST)
    sea(im, f * 2, speed=2)
    b = bob(f)
    boat(im, 12, 115, DECK + b, mast=True, f=f)
    for i, (cx, who) in enumerate(CAST):
        spr = {'yos': yos_idle[f % 6], 'spool': spool, 'uloe': uloe, 'argo': argo_idle[f % 6]}[who]
        put(im, spr, cx, DECK - 1 + b - (1 if (f + i) % 4 == 0 else 0))
    for i in range(5):                                            # spray behind the boat
        t = (f + i * 2) % 12
        px(im, 8 - t // 2, DECK + 6 - (t % 4), FOAM)
    return im


def s_fail(f):
    im = blank(NIGHT); rect(im, 0, 0, W - 1, 53, NIGHT); sea(im, f, speed=2)
    for i in range(14):                                           # rain
        x = (i * 11 + f * 3) % W
        y = (i * 17 + f * 5) % 50
        line(im, x, y, x - 1, y + 3, SLATE)
    boat(im, 12, 115, DECK + (f % 2))
    sx = 64 + [0, 1, 0, -1][f % 4]
    put(im, spool, sx, DECK - 1 + (f % 2))
    # thread wound round and round
    for k in range(3):
        r = 9 + k * 2
        for a in range(0, 360, 12):
            ang = math.radians(a + f * 30 + k * 40)
            x = sx + round(r * math.cos(ang)); y = DECK - 12 + round((r - 3) * math.sin(ang) * 0.7)
            if (a // 12 + k) % 3:
                px(im, x, y, SAGE_PALE)
    line(im, sx + 13, DECK - 14, 100, DECK - 1, SAGE_PALE)
    rect(im, 98, DECK - 4, 103, DECK - 1, SAGE_DK)                # the spool it came off
    if f % 4 < 2:                                                 # a sweat drop
        px(im, sx - 9, DECK - 24, FOAM); px(im, sx - 9, DECK - 23, FOAM); px(im, sx - 10, DECK - 23, FOAM)
    return im


SCENES = {'idle': s_idle, 'building': s_build, 'testing': s_test, 'reviewing': s_review,
          'art': s_art, 'waiting': s_wait, 'shipped': s_ship, 'failed': s_fail}

NODPI, DRAW = os.path.join(RES, 'drawable-nodpi'), os.path.join(RES, 'drawable')
os.makedirs(NODPI, exist_ok=True)
os.makedirs(DRAW, exist_ok=True)
WRAP = ('<?xml version="1.0" encoding="utf-8"?>\n<!-- Generated by StatusWidget/art/scenes.py: no filtering, so pixels stay square. -->\n'
        '<bitmap xmlns:android="http://schemas.android.com/apk/res/android" android:src="@drawable/%s" android:filter="false" android:antialias="false" android:dither="false" />\n')


def save_res(im, png_name, xml_name):
    im.save(os.path.join(NODPI, png_name + '.png'), optimize=True)
    with open(os.path.join(DRAW, xml_name + '.xml'), 'w') as fh:
        fh.write(WRAP % png_name)


for name, fn in SCENES.items():
    frames = [fn(f) for f in range(N)]
    for i, fr in enumerate(frames):
        save_res(fr.convert('RGB'), 'scene_%s_%02d' % (name, i), 'f_%s_%02d' % (name, i))
    big = [fr.convert('RGB').resize((W * K, H * K), Image.NEAREST) for fr in frames]
    big[0].save(os.path.join(OUT, name + '.gif'), save_all=True, append_images=big[1:], duration=140, loop=0, disposal=1)
    print(name, len(frames), 'frames')


def head(spr, top):
    bb = spr.getbbox()
    cx = (bb[0] + bb[2]) // 2
    return spr.crop((cx - 8, top, cx + 8, top + 16))


def sign_icon():
    im = Image.new('RGBA', (16, 16))
    for y in range(3, 10):
        for x in range(1, 15):
            im.putpixel((x, y), CORAL if y in (3, 9) or x in (1, 14) else FOAM)
    for x in (4, 6, 8, 10):
        im.putpixel((x, 6), SWELL)
    for y in range(10, 15):
        im.putpixel((4, y), WOOD_HI); im.putpixel((11, y), WOOD_HI)
    return im


def boat_icon():
    im = Image.new('RGBA', (16, 16))
    for y in range(2, 10):
        for x in range(8, 8 + (y - 1)):
            if x < 15:
                im.putpixel((x, y), FOAM)
    for y in range(1, 11):
        im.putpixel((7, y), WOOD_HI)
    for i, y in enumerate(range(11, 14)):
        for x in range(1 + i, 15 - i):
            im.putpixel((x, y), SAND if y == 11 else WOOD)
    for x in range(0, 16):
        im.putpixel((x, 14), TEAL_HI if x % 4 else FOAM)
    return im


heads = {'yossele': head(yos_idle[0], 8), 'spool': head(spool, 10), 'uloe': head(uloe, 8), 'argo': head(argo_idle[0], 4),
         'sign': sign_icon(), 'boat': boat_icon()}
for k, im in heads.items():
    save_res(im, 'head_' + k, 'h_' + k)

sheet = Image.new('RGB', (W * 4 + 30, H * 2 + 10), (20, 20, 20))
for i, name in enumerate(SCENES):
    sheet.paste(Image.open(os.path.join(NODPI, 'scene_%s_05.png' % name)).convert('RGB'), ((i % 4) * (W + 10), (i // 4) * (H + 10)))
sheet.resize((sheet.width * 3, sheet.height * 3), Image.NEAREST).save(os.path.join(OUT, 'contact.png'))

# the launcher icon: the boat on Deep Swell, 12 px to the pixel
icon = Image.new('RGBA', (16, 16), SWELL)
icon.alpha_composite(heads['boat'])
mip = os.path.join(RES, 'mipmap-xxxhdpi')
os.makedirs(mip, exist_ok=True)
icon.convert('RGB').resize((192, 192), Image.NEAREST).save(os.path.join(mip, 'ic_launcher.png'), optimize=True)
