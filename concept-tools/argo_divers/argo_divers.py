"""Five concept takes on Argo as a deep-sea diver who might not be entirely human (Dimitri, 2026-11-04).

Each take starts from Argo's shipped standing frame (TDS_Mod/Assets/ArgoMod/Editor/ArgoCharacter.cs, the 1.4.0 look:
round brass dome, porthole on the near half with two pale eyes, riveted breastplate, blue suit, big brass mitts on the
hips, dark boots) and changes one thing about what is inside the suit or what the sea has done to it, so the dome-diver
silhouette (docs/STYLE.md) stays his. Drawn by kit docs/18 section 4.2, not by the 1.4.0 file: opaque (the 1.4.0
half-alpha rim becomes the fill colour), no ground patch, feet on the bottom row, light from the upper left, shade keeps
the hue, nothing in the foes' magenta band (h270-320), no pure black.

    python3 concept-tools/argo_divers/argo_divers.py [out_dir]

writes, per take, <out>/<Take>/game_size/Argo_<Take>.png (32 x 32 cells: Idle 6 frames front, then the back),
<Take>/aseprite/Argo_<Take>.aseprite, <Take>/preview_8x/Argo_<Take>_8x.png and Argo_<Take>_idle.gif, plus
<out>/argo_divers_lineup.png (the shipped Argo and the five, front and back, 8x) and lineup_1x.png (game size on the
boat's grey, to judge them at 1x). Default out: docs/concepts/argo_divers_2026-11-04/art.
"""
import colorsys
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, '..', 'temp_art'))
from tart import Canvas, Mask, H, shade, preview, save_png, save_ase, sheet, gif, REPO  # noqa: E402

CELL = 32

# ------------------------------------------------------------------ Argo 1.4.0's palette (ArgoCharacter.P)
P = {
    'y': H('F2C77A'), 'Y': H('D9A048'), 'Z': H('A8722E'),          # brass: lit, main, shade
    'u': H('9CC3E0'), 'U': H('6F9DC8'), 'V': H('4C74A3'),          # suit: lit, main, shade
    'G': H('26405A'), 'g': H('3D6680'), 'e': H('BDF2F0'),          # porthole glass, glass lit, eyes
    'o': H('3E4B5C'),                                              # boots and belt
}
P['s'] = P['Y']     # the 1.4.0 half-alpha rims become the fill (docs/18 4.2: no rim but Smacky's)
P['t'] = P['U']

# ------------------------------------------------------------------ Argo 1.4.0's standing frame, as authored
HELM_F = [
    "...............syYZ.............",
    "...............yYZZ.............",
    "................ZZ..............",
    ".............ssyYYZZ............",
    "...........ssyYYYYYYZZ..........",
    "..........syYYYYYYYYYZZ.........",
    ".........syYYYYyyyyyyyZZ........",
    ".........syYYYyZZZZZZZZZ........",
    ".........syYYyZZGGGGGGZZ........",
    ".........syYYyZgGGGGGGGZ........",
    "........ZYYYYyZGGGGGGGGZ........",
    "........ZYYYYyZGGGGGGGGZ........",
    ".........YYYYyZZGGGGGGZZ........",
    ".........YYYYYyZZZZZZZZZ........",
    "..........YYYYYYYYYYYZZ.........",
    ".........syYZYYZYYZYYZZZ........",
    "........syYYYYYYYYYYYYYZZ.......",
]
HELM_B = [
    "...............syYZ.............",
    "...............yYZZ.............",
    "................ZZ..............",
    ".............ssyYYZZ............",
    "...........ssyYYYYYYZZ..........",
    "..........syYYYYYYYYYZZ.........",
    ".........syYYYYYYYYYYYZZ........",
    ".........syYYYYYYYYYYYZZ........",
    ".........syYYYYYZZYYYYZZ........",
    ".........syYYYYZZZZYYYZZ........",
    "........ZYYYYYYZZZZYYYZZ........",
    "........ZYYYYYYYZZYYYYZZ........",
    ".........YYYYYYYYYYYYYZZ........",
    ".........YYYYYYYYYYYYYZZ........",
    "..........YYYYYYYYYYYZZ.........",
    ".........syYZYYZYYZYYZZZ........",
    "........syYYYYYYYYYYYYYZZ.......",
]
TORSO_F = [
    "tuUUUUUUUUUUUUUUUVV",
    "tuUUUUUUuVUUUUUUUVV",
    ".uUUUUUUuVUUUUUUUV.",
    ".UUUUUUUuVUUUUUUUV.",
    ".ooooooooyZooooooo.",
    ".VVVVVVVVVVVVVVVVV.",
]
TORSO_B = [
    "tuUUUUUUUUUUUUUUUVV",
    "tuUUUUUUUVUUUUUUUVV",
    ".uUUUUUUUVUUUUUUUV.",
    ".UUUUUUUUVUUUUUUUV.",
    ".ooooooooZZooooooo.",
    ".VVVVVVVVVVVVVVVVV.",
]
FAR_MITT = ["..", "..", ".syZ.", "syYYZ", "yYYZZ", ".ZZZ."]
NEAR_MITT = ["..", "..", ".yYZ.", "yYYYZ", "YYYZZ", ".ZZZ."]

# Everything is drawn one row lower than 1.4.0 (which left row 31 for its ground patch), so the soles sit on row 31.
DY = 1
HELM_Y, BODY_Y = 4 + DY, 21 + DY
EYE_L, EYE_R = (16, 12 + DY), (20, 12 + DY)     # 2 x 2 each; the porthole glass spans x 15-23, rows 12-16 (+DY)


class Fig:
    """One frame being drawn: a Canvas plus helpers in Argo's palette keys or plain hex."""

    def __init__(self, pal):
        self.c = Canvas(CELL, CELL)
        self.pal = pal

    def col(self, k):
        if isinstance(k, tuple):
            return k
        return self.pal[k] if k in self.pal else H(k)

    def put(self, x, y, k):
        self.c.set(x, y, self.col(k))

    def get(self, x, y):
        return self.c.get(x, y)

    def clear(self, x, y):
        if 0 <= x < CELL and 0 <= y < CELL:
            self.c.c[y * CELL + x] = (0, 0, 0, 0)

    def stamp(self, rows, x0, y0, keep=None):
        """keep: optional set of keys to draw (others skipped)."""
        for y, row in enumerate(rows):
            for x, k in enumerate(row):
                if k in '. ' or (keep and k not in keep):
                    continue
                self.put(x0 + x, y0 + y, k)

    def rows(self, x0, y0, rows, keymap):
        """Stamp a small drawing whose chars map through keymap (to palette keys or hex)."""
        for y, row in enumerate(rows):
            for x, ch in enumerate(row):
                if ch in '. ':
                    continue
                self.put(x0 + x, y0 + y, keymap[ch])


def legs(f, suit=('u', 'U', 'V'), boot='o', cap=('Y', 'Z'), lift=(0, 0)):
    """The thighs (one row of suit under the hem) and the boots: 4 wide, 2 tall, a 2-pixel sole, a brass toe cap."""
    for x, ly in ((12, lift[0]), (18, lift[1])):
        top, by = 27 + DY, 28 + DY + ly
        for y in range(top, by):
            f.put(x, y, suit[0] if x == 12 else suit[1]); f.put(x + 1, y, suit[1]); f.put(x + 2, y, suit[2])
        for i in range(-1, 3):
            f.put(x + i, by, boot); f.put(x + i, by + 1, boot)
        f.put(x, by + 2, boot); f.put(x + 1, by + 2, boot)
        if cap:
            f.put(x + 2, by, cap[0]); f.put(x + 2, by + 1, cap[1])


def base(f, back=False, head=0, mitts=True, mitt_dy=(0, 0), torso=None, helm=None):
    """Argo's standing frame. head: the helmet's bob (pixels, negative up)."""
    legs(f)
    if back and mitts:
        f.stamp(NEAR_MITT, 22, BODY_Y + mitt_dy[1])
    f.stamp(torso or (TORSO_B if back else TORSO_F), 7, BODY_Y)
    f.stamp(helm or (HELM_B if back else HELM_F), 0, HELM_Y + head)
    if not back:
        eyes(f, head)
    if mitts:
        f.stamp(FAR_MITT, 6, BODY_Y + mitt_dy[0])
        if not back:
            f.stamp(NEAR_MITT, 22, BODY_Y + mitt_dy[1])


def eyes(f, head=0, k='e', dx=0, closed=False):
    for y in range(12, 14):
        for x in range(16, 22):
            f.put(x, y + DY + head, 'G')
    for ex, ey in (EYE_L, EYE_R):
        if closed:
            f.put(ex + dx, ey + 1 + head, 'g'); f.put(ex + dx + 1, ey + 1 + head, 'g')
            continue
        for yy in (0, 1):
            for xx in (0, 1):
                f.put(ex + dx + xx, ey + yy + head, k)


def bubble(f, x, y, big=False):
    """Bubbles are effects, as on 1.4.0's idle: a pale ring."""
    w = 'C6E4EE'
    if big:
        for dx, dy in ((1, 0), (0, 1), (2, 1), (1, 2)):
            f.put(x + dx, y + dy, w)
    else:
        f.put(x, y, w)


IDLE_HEAD = [0, 0, 0, -1, -1, 0]       # the dome settles a beat, as 1.4.0's idle breathes


# ------------------------------------------------------------------ 0. the shipped look, for the line-up

def shipped(i, back=False):
    f = Fig(P)
    base(f, back, head=IDLE_HEAD[i])
    if not back and i in (2, 3, 4):
        bubble(f, 17, 3 - (i - 2) * 2 + DY)
    return f.c


def overlay(f, x0, y0, rows, keymap=None, mirror=False):
    """Stamp string rows at (x0, y0); keymap maps chars to palette keys (identity when None); mirror flips each row."""
    for y, row in enumerate(rows):
        if mirror:
            row = row[::-1]
        for x, ch in enumerate(row):
            if ch in '. ':
                continue
            f.put(x0 + x, y0 + y, keymap[ch] if keymap else ch)


def retone(f, pts, mapping):
    """Swap one material's tones for another's on the given pixels (keeps the shading: lit > lit, main > main...)."""
    inv = {}
    for a, b in mapping.items():
        inv[f.col(a)] = b
    for x, y in pts:
        k = f.get(x, y)
        if k[:4] in inv:
            f.put(x, y, inv[k])


def blob(rows, x0, y0):
    return [(x0 + x, y0 + y) for y, r in enumerate(rows) for x, ch in enumerate(r) if ch != '.']


def back_mirror(pts):
    return [(31 - x, y) for x, y in pts]


# ------------------------------------------------------------------ 1. Barnacled: the sea grew on him

BARN = dict(P)
BARN.update({
    'q': H('9ED6C0'), 'Q': H('72B39C'), 'R': H('52907C'),          # verdigris: lit, main, shade
    'b': H('F2ECDF'), 'B': H('CDBFA6'), 'n': H('6E6559'),          # barnacle shell, its shade, the open mouth
    'k': H('7BCF80'), 'K': H('65AD69'), 'L': H('4C8C55'),          # kelp (the shared leaf green)
    '1': H('F2955A'), '2': H('D27240'),                            # a starfish
})
BARN['s'], BARN['t'] = BARN['Y'], BARN['U']
GREEN = {'y': 'q', 'Y': 'Q', 'Z': 'R', 's': 'Q'}

KELP_F = [            # from the valve's neck, over the crown and down the far side of the dome (x 5..16, y 6..)
    ".........kK",
    ".......kKK.",
    "......kK...",
    ".....kK....",
    "....kK.....",
    "...kK......",
    "...kL......",
    "..kK.......",
    "..kL.......",
    "..kK.......",
    ".kKL.......",
    ".kL........",
    "..L........",
]


def barnacles(f, spots):
    """Each barnacle: a little shell cone, its open dark mouth on top, lit left and shaded right."""
    for x, y in spots:
        f.put(x, y, 'b'); f.put(x + 1, y, 'n')
        f.put(x, y + 1, 'b'); f.put(x + 1, y + 1, 'B')


def barnacled(i, back=False):
    f = Fig(BARN)
    h = IDLE_HEAD[i]
    base(f, back, head=h)
    hy = h
    # verdigris: a patch on the crown, one on the far cheek of the dome, one across the near collar
    crown = blob(["..xxx", ".xxxx", "xxxx.", "xx..."], 10, 9 + hy)
    cheek = blob(["xx", "xxx", "xxx", ".xx"], 9, 14 + hy)
    collar = blob(["..xxxx", "xxxxxx"], 18, 20 + hy)
    if back:
        crown, cheek, collar = back_mirror(crown), back_mirror(cheek), back_mirror(collar)
    retone(f, crown + cheek + collar, GREEN)
    # barnacle clusters break the dome's far edge, and crust the near shoulder
    if not back:
        barnacles(f, [(8, 16 + hy), (9, 18 + hy), (7, 14 + hy), (22, 22), (24, 23)])
    else:
        barnacles(f, [(22, 16 + hy), (21, 18 + hy), (23, 14 + hy), (7, 22), (9, 23)])
    # a kelp strand caught on the valve, trailing down the far side, swaying in the idle
    sway = [0, 0, 1, 1, 0, -1][i]
    for y, row in enumerate(KELP_F):
        for x, ch in enumerate(row):
            if ch == '.':
                continue
            xx = 5 + x + (sway if y >= 7 else 0)
            if back:
                xx = 31 - xx
            f.put(xx, 6 + y + hy, ch)
    # a starfish clinging to the chest
    sx = 18 if not back else 11
    overlay(f, sx, 22, [".1.", "112", "1.2"])
    if not back and i in (3, 4, 5):
        bubble(f, 25, 12 - (i - 3) * 3 + DY, big=i == 5)
    return f.c


# ------------------------------------------------------------------ 2. Angler: the deep gave him a lure

ANG = dict(P)
ANG.update({
    'f': H('94D4C4'), 'F': H('5EAE9C'), 'h': H('3F8676'),          # fins: lit, main, shade (sea teal)
    'l': H('FFF48B'), 'L': H('F6E43B'), 'w': H('FFFFFF'),          # the lure's glow (the shared yellows)
    'm': H('6E9CB8'),                                              # the mouth line in the glass
})
ANG['s'], ANG['t'] = ANG['Y'], ANG['U']

STALK = [             # from the crown, arching forward over the porthole (x 15.., y 3..)
    "..fFFh....",
    ".Fh...hF..",
    "Fh......h.",
    "Fh........",
    "YZ........",
]
DORSAL = [            # a spiny fin on the far side of the dome (x 4.., y 8..)
    "...f.",
    "..fF.",
    ".fFFh",
    "fFhFh",
    ".FFhh",
    "..Fh.",
    "...h.",
]
FIN = [               # a pectoral fin in place of each mitt: a fleshy root at the cuff, rays fanning down (x .., y 23..)
    ".fF.",
    "fFFh",
    "FhFh",
    "FhFh",
    "h.h.",
]


def angler(i, back=False):
    f = Fig(ANG)
    base(f, back, head=0, mitts=False)
    # the valve is gone: a stalk grows from the crown and arches forward to a glowing lure
    for y in range(5, 8):
        for x in range(14, 20):
            f.clear(x, y)
    overlay(f, 15, 3 + 1, STALK, mirror=back) if not back else overlay(f, 31 - 15 - 9, 4, STALK, mirror=True)
    bob = [0, 0, 1, 1, 1, 0][i]
    bright = i in (1, 2, 3)
    lx = 23 if not back else 6
    ly = 6 + bob
    lure = [".l.", "lwl", ".L."] if bright else [".l.", "lll", ".L."]
    overlay(f, lx, ly, lure)
    # dorsal fin on the far side of the dome; from behind it stands up the middle of the back plate
    if not back:
        overlay(f, 4, 9, DORSAL)
    else:
        overlay(f, 14, 7, ["..f..", ".fFh.", "fFFhh"])
    # fish face: wide-set eyes at the porthole's edges, a long mouth with two small teeth
    if not back:
        for y in range(13, 18):
            for x in range(15, 23):
                if f.get(x, y) in (ANG['g'], ANG['e']):
                    f.put(x, y, 'G')
        f.put(15, 13, 'g')
        for ex in (16, 21):
            f.put(ex, 13, 'e'); f.put(ex + 1, 13, 'e'); f.put(ex, 14, 'e'); f.put(ex + 1, 14, 'e')
        for x, y in ((17, 16), (18, 17), (19, 17), (20, 17), (21, 16)):
            f.put(x, y, 'm')
        f.put(18, 16, 'e'); f.put(20, 16, 'e')
    # fins where the mitts were, still laid against the hips; the near one flicks in the idle
    flick = [0, 0, 0, 1, 1, 0][i]
    if back:
        overlay(f, 23, 23 + flick, FIN)
        f.stamp(TORSO_B, 7, BODY_Y)
        overlay(f, 5, 23, FIN)
    else:
        overlay(f, 5, 23, FIN)
        overlay(f, 23, 23 + flick, FIN)
        for y in range(23, 28):      # the mirrored fin keeps the light on its left: retone its right column
            pass
    return f.c


# ------------------------------------------------------------------ 3. Hollow: nobody in the suit, only sea

HOL = dict(P)
HOL.update({
    'u': H('BACFDD'), 'U': H('93B1C8'), 'V': H('6F8DA8'),          # the suit, sun-and-salt faded
    'A': H('1E3446'), 'G': H('2F6A80'), 'g': H('4F92A8'), 'a': H('8CC8D4'),   # air, water, its light, the surface
    'e': H('E6FFF8'), 'x': H('F2B263'), 'X': H('D98F3F'),          # two drifting lights; a small fish
})
HOL['s'], HOL['t'] = HOL['Y'], HOL['U']

GLOVE = [             # an empty glove hanging limp from the cuff, its fingers dangling (x .., y 24..)
    ".oo",
    "yYZ",
    "YYZ",
    "YYZ",
    "YZZ",
    "Z.Z",
]


def hollow(i, back=False):
    f = Fig(HOL)
    h = IDLE_HEAD[i]
    base(f, back, head=h, mitts=False)
    # the suit sags: no squared shoulders, and two creases where a body should fill it
    for y in (22, 23):
        f.clear(7, y); f.clear(25, y)
    f.put(8, 22, 'u'); f.put(24, 22, 'V'); f.put(24, 23, 'V')
    if not back:
        # the porthole is full of sea: a pocket of air on top, a bright surface line, two lights adrift in the water
        wl = [0, 1, 1, 0, 0, 1][i]
        for y in range(13, 18):
            for x in range(15, 23):
                k = f.get(x, y + h)
                if k in (HOL['G'], HOL['g'], HOL['e']):
                    f.put(x, y + h, 'A' if y == 13 else 'G')
        for x in range(15, 23):
            if f.get(x, 14 + h)[3]:
                f.put(x, 14 + h, 'a' if (x + wl) % 3 else 'g')
        d = [(0, 1), (0, 1), (1, 1), (1, 0), (1, 0), (0, 0)][i]
        f.put(17, 15 + h + d[0], 'e'); f.put(17, 16 + h + d[0], 'e')
        f.put(20, 15 + h + d[1], 'e'); f.put(20, 16 + h + d[1], 'e')
        fx = [22, 21, 20, 19, 18, None][i]
        if fx:
            f.put(fx, 17 + h, 'x'); f.put(fx + 1, 17 + h, 'X')
    # the gloves hang limp a row lower than his mitts did, dripping
    if back:
        overlay(f, 23, 24, GLOVE)
        f.stamp(TORSO_B, 7, BODY_Y, keep=set('UVo'))
        overlay(f, 7, 24, GLOVE)
    else:
        overlay(f, 7, 24, GLOVE)
        overlay(f, 23, 24, GLOVE)
    drip = [None, 29, 30, None, None, None][i]
    if drip:
        f.put(23 if not back else 8, drip, 'a')
    # what air is left leaves by the valve
    if i in (2, 3, 4):
        bubble(f, 17 if not back else 14, 4 - (i - 2) + h, big=i == 3)
    return f.c


# ------------------------------------------------------------------ 4. Passenger: something else wears the suit

OCT = dict(P)
OCT.update({
    'c': H('F6A894'), 'C': H('E08670'), 'D': H('C2684F'),          # coral tentacles: lit, main, shade (h10-12)
    'p': H('FCDCCF'),                                              # suckers
    'E': H('F2D86A'), 'J': H('D9BC4A'), 'N': H('2B2922'), 'w': H('FFFFFF'),   # the eye: iris, its shade, pupil, glint
})
OCT['s'], OCT['t'] = OCT['Y'], OCT['U']

ARM_FAR = [           # out of the far cuff, down the side and curling back against the hip (x 4.., y 22..)
    "..cCC",
    ".cCCD",
    "cCCD.",
    "cCD..",
    "cCD.c",
    "cCpCD",
    ".DDD.",
]
ARM_NEAR = [          # the near one, drawn (not mirrored) so the light stays on the left (x 23.., y 22..)
    "cCC..",
    "cCCD.",
    ".cCCD",
    "..cCD",
    "c.cCD",
    "CpcDD",
    ".DDD.",
]
EYE_BIG = [".EEE.", "EwEEE", "ENNNE", "EEEEJ", ".JJJ."]


def passenger(i, back=False):
    f = Fig(OCT)
    h = IDLE_HEAD[i]
    base(f, back, head=h, mitts=False)
    curl = [0, 0, 1, 1, 1, 0][i]
    far, near = [r for r in ARM_FAR], [r for r in ARM_NEAR]
    if curl:      # the tips lift a row
        far = far[:4] + [far[5], far[6], "....."]
        near = near[:4] + [near[5], near[6], "....."]
    if back:
        overlay(f, 22, 22, far, mirror=True)
        f.stamp(TORSO_B, 7, BODY_Y, keep=set('uUVto'))
        overlay(f, 4, 22, near, mirror=True)
    else:
        overlay(f, 4, 22, far)
        overlay(f, 23, 22, near)
    # a third slips out from under the collar on the far side and feels about
    tip = [0, -1, -1, 0, 1, 0][i]
    third = ["...cC", ".cCD.", "cD...", "C...."] if tip <= 0 else ["...cC", ".cCD.", ".cD..", "..C.."]
    if not back:
        overlay(f, 5, 18 + h + min(tip, 0), third)
    else:
        overlay(f, 22, 18 + h + min(tip, 0), third, mirror=True)
    if not back:
        # one big eye fills the porthole, a slit pupil across it; it blinks once
        for y in range(13, 18):
            for x in range(15, 23):
                if f.get(x, y + h) in (OCT['g'], OCT['e']):
                    f.put(x, y + h, 'G')
        if i == 4:
            overlay(f, 17, 15 + h, ["CCCCC"])
            overlay(f, 18, 14 + h, ["CCC"])
        else:
            overlay(f, 17, 13 + h, EYE_BIG)
            if i == 3:          # the pupil slides toward you
                f.put(18, 15 + h, 'E'); f.put(21, 15 + h, 'N')
    return f.c


# ------------------------------------------------------------------ 5. Pressure Hull: a later, rounder hard suit

HUL = {
    'y': H('F0D08C'), 'Y': H('D9A85A'), 'Z': H('A87A38'),          # brass: lit, main, shade
    'u': H('E4ECF0'), 'U': H('BCCAD4'), 'V': H('91A2B0'),          # the hull: steel (white shades to cold blue-grey)
    'o': H('5E6873'), 'O': H('4A535D'),                            # joints, soles
    'G': H('22465C'), 'i': H('9FF3E9'), 'I': H('19DFCE'), 'W': H('E8FFFC'),   # glass; the light inside
}


def hull(i, back=False):
    f = Fig(HUL)
    h = IDLE_HEAD[i]
    pulse = [0, 1, 2, 2, 1, 0][i]
    c = f.c
    # legs: ball knees and wide round soles
    for lx in (12, 19):
        shade(c, Mask().rect(lx, 27, lx + 1, 28), (HUL['U'], HUL['V']))
        c.set(lx, 28, HUL['o']); c.set(lx + 1, 28, HUL['O'])
        shade(c, Mask().ellipse(lx + 1, 30.3, 3.0, 1.8), (HUL['U'], HUL['V'], HUL['o']))
        for x in range(lx - 2, lx + 4):
            if c.get(x, 31)[3]:
                c.set(x, 31, HUL['O'])
    # the body: a short steel barrel, a brass belt
    shade(c, Mask().ellipse(16, 23.6, 8.2, 4.6), (HUL['u'], HUL['U'], HUL['V']), centre=(14.5, 21.5))
    for x in range(9, 24):
        if c.get(x, 26)[3]:
            c.set(x, 26, HUL['Y'] if x < 21 else HUL['Z'])
    # shoulder balls, a segmented arm and a closed pincer resting on each hip
    for sx, near in ((8, False), (24, True)):
        if back:
            near = not near
        shade(c, Mask().ellipse(sx, 21.5, 2.2, 2.2), (HUL['u'], HUL['U'], HUL['V']))
        ax = sx - 1 if sx < 16 else sx
        shade(c, Mask().rect(ax, 23, ax + 1, 25), (HUL['U'], HUL['V']))
        c.set(ax, 24, HUL['o']); c.set(ax + 1, 24, HUL['O'])
        claw = ["yZ", ".Z", "YZ"] if sx < 16 else ["YZ", "Y.", "YZ"]
        overlay(f, ax + (1 if sx < 16 else -1), 26, claw)
    # the chest: a light seam down the middle (it breathes), a round hatch on the back
    seam = ['I', 'i', 'i', 'W', 'i', 'I'][i]
    if not back:
        for y in range(20, 26):
            c.set(16, y, HUL[seam] if y in (21, 22, 23, 24) else HUL['V'])
    else:
        shade(c, Mask().ellipse(16, 23, 2.6, 2.1), (HUL['U'], HUL['V'], HUL['o']))
        c.set(16, 22, HUL['I'] if pulse else HUL['i'])
    # the collar ring
    for x in range(9, 23):
        c.set(x, 19 + h, HUL['Y'] if x < 20 else HUL['Z'])
        c.set(x, 20 + h, HUL['Z'] if (x - 9) % 3 == 1 else (HUL['Y'] if x < 20 else HUL['Z']))
    c.set(9, 19 + h, HUL['y'])
    # the dome: smaller and rounder than the 1.4.0 helmet, a vent on top
    shade(c, Mask().ellipse(16, 13.2 + h, 7.0, 6.2), (HUL['Y'], HUL['Z'], None), cut=(-0.55, -2))
    for x, y in ((11, 9), (10, 10), (12, 8), (13, 8)):
        c.set(x, y + h, HUL['y'])
    overlay(f, 15, 6 + h, ["yZ"])
    overlay(f, 14, 5 + h, [".yY."[1:3]])
    if not back:
        # a big front port: rim, dark glass, and a light where a face should be
        port = Mask().ellipse(18.6, 13.4 + h, 3.6, 3.6)
        for x, y in port.px:
            c.set(x, y, HUL['Z'])
        glass = Mask().ellipse(18.6, 13.4 + h, 2.6, 2.6)
        for x, y in glass.px:
            c.set(x, y, HUL['G'])
        for x, y in glass.px:
            d = abs(x + .5 - 18.6) + abs(y + .5 - 13.4 - h)
            if d <= 0.9 + pulse * 0.5:
                c.set(x, y, HUL['W'] if pulse == 2 else HUL['i'])
            elif d <= 1.9 + pulse * 0.5:
                c.set(x, y, HUL['I'])
        # a small side port on the far side, lit from within
        c.set(10, 12 + h, HUL['Z']); c.set(10, 13 + h, HUL['I']); c.set(10, 14 + h, HUL['Z'])
        c.set(11, 13 + h, HUL['Z'])
    else:
        # from behind: a round bolted hatch, and the far port's glow peeking round the dome's edge
        for x, y in Mask().ellipse(16, 13 + h, 2.6, 2.6).px:
            c.set(x, y, HUL['Z'])
        for x, y in Mask().ellipse(16, 13 + h, 1.6, 1.6).px:
            c.set(x, y, HUL['Y'])
        c.set(22, 13 + h, HUL['I']); c.set(22, 14 + h, HUL['Z'])
    return c


TAKES = [
    ('Barnacled', barnacled),
    ('Angler', angler),
    ('Hollow', hollow),
    ('Passenger', passenger),
    ('Hull', hull),
]



def check(name, frames):
    """docs/18 4.2: 19-28 tall, 15-27 wide, feet on the bottom row, centred near column 16, opaque, no magenta, no black."""
    out = []
    for n, c in enumerate(frames):
        fx = {H('C6E4EE')}
        px = [(x, y) for y in range(CELL) for x in range(CELL) if c.get(x, y)[3] and c.get(x, y) not in fx]
        xs = [x for x, _ in px]; ys = [y for _, y in px]
        w, hgt = max(xs) - min(xs) + 1, max(ys) - min(ys) + 1
        bad = []
        if max(ys) != CELL - 1: bad.append('feet off the bottom row')
        if not 19 <= hgt <= 28: bad.append('height %d' % hgt)
        cols = c.colours()
        for col in cols:
            if col[3] != 255: bad.append('half alpha')
            hh, ss, vv = colorsys.rgb_to_hsv(*(v / 255.0 for v in col[:3]))
            if 270 <= hh * 360 <= 320 and ss > 0.2: bad.append('magenta %02X%02X%02X' % col[:3])
            if col[:3] == (0, 0, 0): bad.append('pure black')
        out.append((n, w, hgt, (min(xs) + max(xs) + 1) / 2.0, len(cols), sorted(set(bad))))
    return out


def main():
    out = sys.argv[1] if len(sys.argv) > 1 else os.path.join(REPO, 'docs', 'concepts', 'argo_divers_2026-11-04', 'art')
    os.makedirs(out, exist_ok=True)
    report = []
    lineup_f, lineup_b, idles = [shipped(0)], [shipped(0, True)], [[shipped(i) for i in range(6)]]
    for name, fn in TAKES:
        idle = [fn(i) for i in range(6)]
        back = fn(0, True)
        cells = idle + [back]
        base_dir = os.path.join(out, name)
        s = sheet(cells, 7, CELL)
        save_png(s, os.path.join(base_dir, 'game_size', 'Argo_%s.png' % name))
        save_png(preview(s, CELL, 8), os.path.join(base_dir, 'preview_8x', 'Argo_%s_8x.png' % name))
        save_ase(os.path.join(base_dir, 'aseprite', 'Argo_%s.aseprite' % name), CELL, cells,
                 [('Idle_v0', 0, 5, 'CONCEPT;front heading right'), ('Back_v2', 6, 6, 'CONCEPT;back, standing')],
                 layer='CONCEPT art')
        gif([idle], os.path.join(base_dir, 'preview_8x', 'Argo_%s_idle.gif' % name), k=8, ms=125)
        lineup_f.append(idle[0]); lineup_b.append(back); idles.append(idle)
        for row in check(name, cells):
            report.append((name,) + row)
    gif(idles, os.path.join(out, 'argo_divers_idle.gif'), k=6, ms=125)
    line = sheet(lineup_f + lineup_b, 6, CELL)
    save_png(preview(line, CELL, 8), os.path.join(out, 'argo_divers_lineup_8x.png'))
    # 1x on the boat's grey, then 3x of that so it can be judged (each game pixel = 3 screen pixels, no smoothing)
    one = Canvas(6 * 40, 44)
    for y in range(one.h):
        for x in range(one.w):
            one.c[y * one.w + x] = (0x8C, 0x8A, 0x86, 255) if y < 36 else (0x6E, 0x6C, 0x69, 255)
    for k, c in enumerate(lineup_f):
        one.draw(c, 4 + k * 40, 4)
    save_png(one.scaled(3), os.path.join(out, 'argo_divers_lineup_1x_x3.png'))
    for r in report:
        print('%-10s frame %d: %2d x %2d, centre %.1f, %2d colours %s' % (r[0], r[1], r[2], r[3], r[4], r[5],
                                                                          ('BAD ' + ', '.join(r[6])) if r[6] else ''))


if __name__ == '__main__':
    main()
