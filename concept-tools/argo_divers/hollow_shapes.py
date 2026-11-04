"""Hollow, round 2 (Dimitri, 2026-11-04 05:10Z): "Create more concepts based on the hollow choice, but play around more
with the body and head's shapes."

Every take keeps what made Hollow: nobody in the suit, the helmet full of seawater (a pocket of air, a bright surface
line, two pale lights adrift, a small fish), the faded suit and empty gloves, the last air bubbling out. What changes is
the silhouette of the head and the body:

    Bell Jar     tall, narrow helmet with a tall oval port; a thin sack of a suit, long sleeves to the ground
    Fish Tank    a wide, flat-topped tank of a helmet with a big square window and kelp growing inside; a stubby body
    Slump        the helmet sunk low and tipped into a pear-shaped suit that has settled on the floor, no legs showing
    Fishbowl     the whole head is a glass globe of water on a brass collar; a coat of a suit hung as if on a hanger
    Hunch        the helmet slid forward off the collar, the body bent under it, a cut air hose trailing to the floor

    python3 concept-tools/argo_divers/hollow_shapes.py [out_dir]

Same outputs and checks as argo_divers.py (kit docs/18 4.2). Default out: docs/concepts/argo_hollow_2026-11-04/art.
"""
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import argo_divers as A  # noqa: E402
from argo_divers import Fig, overlay, bubble, CELL, IDLE_HEAD  # noqa: E402
from tart import Canvas, Mask, H, shade, preview, save_png, save_ase, sheet, gif, REPO  # noqa: E402

PAL = {
    'y': H('F2C77A'), 'Y': H('D9A048'), 'Z': H('A8722E'),          # brass (Argo's): lit, main, shade
    'u': H('BACFDD'), 'U': H('93B1C8'), 'V': H('6F8DA8'),          # the suit, sun-and-salt faded
    'o': H('3E4B5C'), 'O': H('2F3A48'),                            # boots, hose rubber
    'A': H('1E3446'), 'G': H('2F6A80'), 'D': H('24566A'),          # air pocket, water, deep water (far edge)
    'g': H('4F92A8'), 'a': H('8CC8D4'),                            # water's lit edge, the surface line
    'e': H('E6FFF8'), 'x': H('F2B263'), 'X': H('D98F3F'),          # two drifting lights; a small fish
    'k': H('7BCF80'), 'K': H('65AD69'),                            # kelp in the tank
    'W': H('E9F4F6'),                                              # a glint on glass
}
BRASS = (PAL['Y'], PAL['Z'])
BRASS3 = (PAL['y'], PAL['Y'], PAL['Z'])
SUIT = (PAL['u'], PAL['U'], PAL['V'])
SUIT2 = (PAL['U'], PAL['V'])


def form(f, mask, tones, lit_rows=None, **kw):
    """Flat shading as on the shipped vessels (v3_flat): the main colour, the lit colour on the left edge of each row,
    a two-pixel shade band down the right edge and a shade row along the bottom. tones = (lit, main, shade) or
    (main, shade). lit_rows: only rows above this y get the lit edge (None: all)."""
    lit, main, sh = tones if len(tones) == 3 else (None, tones[0], tones[1])
    px = mask.px
    for x, y in px:
        k = main
        if (x + 1, y) not in px or (x + 2, y) not in px or (x, y + 1) not in px:
            k = sh
        elif lit is not None and (x - 1, y) not in px and (lit_rows is None or y < lit_rows):
            k = lit
        f.c.set(x, y, k)


def brass(f, mask, cut=None):
    """A brass form like Argo's dome: lit left edge on the upper half, shade band right and below."""
    ys = [y for _, y in mask.px]
    form(f, mask, BRASS3, lit_rows=(min(ys) + max(ys)) / 2.0 + 1)


def water(f, px, i, air=1, lights=None, fish_row=None, fish_from=None, kelp=None):
    """Fill a glass area with sea: `air` rows of air on top, a wavy surface line, water below with its far (right and
    bottom) edge deeper, two pale lights adrift, and a small fish swimming right to left over the idle."""
    px = set(px)
    if not px:
        return
    top = min(y for _, y in px)
    surf = top + air
    wl = [0, 1, 1, 0, 0, 1][i]
    for x, y in px:
        if y < surf:
            k = 'A'
        elif y == surf:
            k = 'a' if (x + wl) % 3 else 'g'
        elif (x + 1, y) not in px or (x, y + 1) not in px:
            k = 'D'
        elif (x - 1, y) not in px:
            k = 'g'
        else:
            k = 'G'
        f.put(x, y, k)
    if kelp:
        sway = [0, 0, 1, 1, 0, -1][i]
        for j, (x, y) in enumerate(kelp):
            xx = x + (sway if j >= len(kelp) // 2 else 0)
            if (xx, y) in px:
                f.put(xx, y, 'k' if j % 2 else 'K')
    if lights:
        drift = [(0, 1), (0, 1), (1, 1), (1, 0), (1, 0), (0, 0)][i]
        for n, (x, y) in enumerate(lights):
            for dy in (0, 1):
                if (x, y + drift[n] + dy) in px:
                    f.put(x, y + drift[n] + dy, 'e')
    if fish_row is not None:
        fx = fish_from - i * 2
        for dx, k in ((0, 'x'), (1, 'X')):
            if (fx + dx, fish_row) in px and i < 5:
                f.put(fx + dx, fish_row, k)


def ring(mask_outer, mask_inner):
    m = Mask()
    m.px = mask_outer.px - mask_inner.px
    return m


def shifted(m, dy):
    o = Mask()
    o.px = {(x, y + dy) for x, y in m.px}
    return o


def flipped(m):
    return m.flipped(CELL)


def boots(f, xs, y=29, w=4):
    for x in xs:
        form(f, Mask().rect(x, y, x + w - 1, y + 1), (PAL['o'], PAL['O']))
        f.put(x + w - 1, y, 'Y'); f.put(x + w - 1, y + 1, 'Z')
        for xx in range(x, x + w - 1):
            f.put(xx, 31, 'o')


def glove(f, x, y, rows=None):
    """An empty glove hanging limp, fingers down."""
    overlay(f, x, y, rows or ["yZ", "YZ", "YZ", "Z.Z"[:2]])


# ------------------------------------------------------------------ 1. Bell Jar

def bell_jar(i, back=False):
    f = Fig(PAL)
    h = IDLE_HEAD[i] + 1
    # thin legs and small boots
    form(f, Mask().rect(13, 27, 14, 28), SUIT2); form(f, Mask().rect(18, 27, 19, 28), SUIT2)
    boots(f, [12, 17], 29, 3)
    # a thin sack of a suit, narrow at the shoulders, slumping wider to the hem
    sack = Mask()
    for y, (x0, x1) in zip(range(21, 28), [(13, 19), (12, 20), (12, 20), (11, 21), (11, 21), (11, 21), (12, 20)]):
        sack.rect(x0, y, x1, y)
    form(f, sack, SUIT, centre=(15, 22))
    # long sleeves to the ground, the gloves dragging
    for sx in (9, 22):
        form(f, Mask().rect(sx, 21, sx + 1, 27), SUIT2)
        overlay(f, sx, 28, ["YZ", "YZ", "Z.", ".Z"] if sx < 16 else ["YZ", "YZ", ".Z", "Z."])
    # the helmet: a tall bell jar on a riveted collar
    jar = Mask().rect(10, 10 + h, 22, 18 + h)
    jar.px |= Mask().ellipse(16.5, 10.5 + h, 6.5, 5.6).px
    brass(f, jar)
    for x in range(11, 22):
        f.put(x, 20 + h - 1, 'Z' if (x - 11) % 3 == 1 else 'Y')
    f.put(16, 4 + h, 'y'); f.put(17, 4 + h, 'Z')               # a small valve on the crown
    if not back:
        port_o = Mask().ellipse(17.6, 13.6 + h, 3.9, 5.1)
        port_i = Mask().ellipse(17.6, 13.6 + h, 2.9, 4.1)
        for x, y in ring(port_o, port_i).px:
            f.put(x, y, 'Z')
        water(f, port_i.px, i, air=1, lights=[(16, 12 + h), (18, 14 + h)], fish_row=16 + h, fish_from=20)
    else:
        for x, y in Mask().ellipse(16.5, 13 + h, 2.2, 2.2).px:
            f.put(x, y, 'Z')
    if i in (2, 3, 4):
        bubble(f, 17 if not back else 15, 3 - (i - 2) + h, big=i == 3)
    return f.c


# ------------------------------------------------------------------ 2. Fish Tank

TANK_KELP = [(12, 18), (12, 17), (11, 16), (12, 15), (12, 14), (11, 13)]


def fish_tank(i, back=False):
    f = Fig(PAL)
    h = IDLE_HEAD[i]
    boots(f, [11, 18], 29, 4)
    form(f, Mask().rect(12, 27, 13, 28), SUIT2); form(f, Mask().rect(19, 27, 20, 28), SUIT2)
    # a stubby little body, wider than tall
    body = Mask().ellipse(16, 24.5, 7.2, 3.6)
    form(f, body, SUIT)
    for x in range(10, 23):
        if f.get(x, 26)[3]:
            f.put(x, 26, 'o')
    f.put(16, 26, 'y'); f.put(17, 26, 'Z')
    overlay(f, 7, 23, ["yZ", "YZ", "YZ", "Z."]); overlay(f, 24, 23, ["YZ", "YZ", "YZ", ".Z"])
    # the head: a wide, flat-topped tank with rounded corners and a lid
    tank = Mask().rect(6, 8 + h, 26, 20 + h)
    for x, y in ((6, 8), (26, 8), (6, 20), (26, 20)):
        tank.px.discard((x, y + h))
    brass(f, tank)
    for x in range(8, 25):
        f.put(x, 7 + h, 'Y' if x < 22 else 'Z')                # the lid
    f.put(9, 6 + h, 'y'); f.put(10, 6 + h, 'Z'); f.put(22, 6 + h, 'y'); f.put(23, 6 + h, 'Z')   # two bolts
    for x in (7, 25):
        f.put(x, 10 + h, 'Z'); f.put(x, 18 + h, 'Z')           # rivets on the corners
    if not back:
        frame = Mask().rect(8, 10 + h, 24, 19 + h)
        win = Mask().rect(9, 11 + h, 23, 18 + h)
        for x, y in ring(frame, win).px:
            f.put(x, y, 'Z')
        kelp = [(x, y + h) for x, y in TANK_KELP]
        water(f, win.px, i, air=1, lights=[(15, 13 + h), (19, 14 + h)], fish_row=16 + h, fish_from=22, kelp=kelp)
    else:
        for x in range(10, 23, 4):
            f.put(x, 14 + h, 'Z')
    if i in (1, 2, 3):
        bubble(f, 22 if not back else 10, 4 - (i - 1) + h, big=i == 2)
    return f.c


# ------------------------------------------------------------------ 3. Slump

def slump(i, back=False):
    f = Fig(PAL)
    h = [0, 0, 0, 1, 1, 0][i]           # the helmet sinks a little further, then settles
    # a pear of a suit settled on the floor: no legs, two boot toes poking out under the hem
    pear = Mask().ellipse(16, 25.2, 9.6, 6.0)
    pear.px = {(x, y) for x, y in pear.px if y <= 31}
    form(f, pear, SUIT, centre=(14, 22))
    for x, y in ((11, 26), (12, 27), (13, 28), (20, 26), (19, 27), (22, 28)):
        f.put(x, y, 'V')                                       # sagging folds
    for x in range(8, 25):
        if f.get(x, 23)[3] and (x < 11 or x > 21):
            pass
    f.put(12, 31, 'o'); f.put(13, 31, 'o'); f.put(19, 31, 'o'); f.put(20, 31, 'o'); f.put(21, 31, 'Y')
    # the sleeves spill onto the floor, the gloves lying open
    overlay(f, 4, 27, [".UV", "UUV", "yYZ", "YZZ"] if not back else [".UV", "UUV", "yYZ", "YZZ"])
    overlay(f, 25, 27, ["UV.", "UVV", "YZZ", "YZ."])
    # the helmet: tipped forward and sunk to its porthole's rim in the collar
    dome = Mask().ellipse(15.5, 14 + h, 6.6, 6.0)
    brass(f, dome)
    for x in range(10, 22):
        if f.get(x, 19 + h)[3]:
            f.put(x, 19 + h, 'Z' if x % 3 == 0 else 'Y')
    f.put(11, 7 + h, 'y'); f.put(12, 7 + h, 'Z'); f.put(12, 8 + h, 'Z')   # the valve, tipped back
    if not back:
        port_o = Mask().ellipse(18.4, 15.4 + h, 3.6, 3.3)
        port_i = Mask().ellipse(18.4, 15.4 + h, 2.6, 2.3)
        for x, y in ring(port_o, port_i).px:
            f.put(x, y, 'Z')
        water(f, port_i.px, i, air=1, lights=[(17, 15 + h), (19, 16 + h)], fish_row=17 + h, fish_from=21)
    if i in (1, 2, 3):
        bubble(f, 10 if not back else 20, 5 - (i - 1) + h, big=i == 2)
    return f.c


# ------------------------------------------------------------------ 4. Fishbowl

BOWL = dict(PAL)
BOWL.update({'A': H('B9DCE6'), 'G': H('3C7F96'), 'D': H('2E6880'), 'g': H('5FA3B6')})   # clear glass above the water


def fishbowl(i, back=False):
    f = Fig(BOWL)
    h = IDLE_HEAD[i] + 2
    form(f, Mask().rect(13, 27, 14, 28), SUIT2); form(f, Mask().rect(18, 27, 19, 28), SUIT2)
    boots(f, [12, 17], 29, 3)
    # a coat of a suit, hung as if on a hanger: a straight bar of shoulders, the cloth falling to a narrow hem
    coat = Mask()
    for y, (x0, x1) in zip(range(21, 28), [(9, 23), (10, 22), (11, 21), (11, 21), (12, 20), (12, 20), (12, 20)]):
        coat.rect(x0, y, x1, y)
    form(f, coat, SUIT, centre=(14, 21))
    for x, y in ((14, 23), (14, 24), (18, 23), (18, 24)):
        f.put(x, y, 'V')
    for sx in (9, 22):
        form(f, Mask().rect(sx, 22, sx + 1, 26), SUIT2)
        overlay(f, sx, 27, ["YZ", "YZ", "Z."] if sx < 16 else ["YZ", "YZ", ".Z"])
    # the brass collar the globe sits in
    for x in range(11, 22):
        f.put(x, 20, 'Y' if x < 19 else 'Z'); f.put(x, 19, 'Z' if (x - 11) % 3 == 1 else 'Y')
    f.put(11, 19, 'y')
    # (the collar is drawn again after the globe so the globe sits in it)
    collar = [(x, y, f.get(x, y)) for x in range(11, 22) for y in (19, 20)]
    # the head is a glass globe of sea, a brass cap and strap holding it
    globe = Mask().ellipse(16, 10.8 + h, 7.6, 6.8)
    globe.px = {(x, y) for x, y in globe.px if y <= 18}
    water(f, globe.px, i, air=2, lights=[(14, 11 + h), (18, 13 + h)], fish_row=15 + h, fish_from=21)
    if back:
        for x, y in globe.px:
            if f.get(x, y) == PAL['e']:
                f.put(x, y, 'g')                               # from behind the lights are only a glow
    for x in range(14, 19):
        f.put(x, 4 + h, 'Y' if x < 17 else 'Z')
    f.put(15, 3 + h, 'y'); f.put(16, 3 + h, 'Z')
    for x, y in ((11, 8), (10, 9), (10, 10)):
        f.put(x if not back else 31 - x, y + h, 'W')           # a glint on the glass
    for x, y, k in collar:
        f.put(x, y, k)
    if i in (2, 3, 4):
        bubble(f, 16, 2 - (i - 2) + h + 1, big=False)
    return f.c


# ------------------------------------------------------------------ 5. Hunch

def hunch(i, back=False):
    f = Fig(PAL)
    h = IDLE_HEAD[i]
    boots(f, [10, 17], 29, 4)
    form(f, Mask().rect(11, 27, 12, 28), SUIT2); form(f, Mask().rect(18, 27, 19, 28), SUIT2)
    # the body bent forward under the helmet's weight: the back rounds up on the far side
    torso = Mask()
    for y, (x0, x1) in zip(range(19, 28), [(8, 13), (7, 16), (7, 19), (7, 21), (8, 22), (8, 22), (9, 21), (9, 21), (10, 20)]):
        torso.rect(x0, y, x1, y)
    form(f, torso, SUIT, centre=(11, 20))
    for x in range(9, 22):
        if f.get(x, 26)[3]:
            f.put(x, 26, 'o')
    # the far arm hangs long, its glove dragging; the near one tucked under the helmet
    form(f, Mask().rect(6, 21, 7, 27), SUIT2)
    overlay(f, 6, 28, ["YZ", "YZ", "Z.", "Z."])
    # a cut air hose from the back of the helmet down to the floor
    hose = [(9, 13), (8, 13), (7, 14), (6, 15), (5, 16), (4, 17), (4, 18), (4, 19), (3, 20), (3, 21), (3, 22),
            (3, 23), (3, 24), (3, 25), (4, 26), (4, 27), (4, 28), (5, 29), (5, 30)]
    for j, (x, y) in enumerate(hose):
        f.put(x, y + (h if j < 4 else 0), 'o')
        if 3 < j < 16:
            f.put(x + 1, y, 'O')
    if i in (2, 3):
        f.put(5, 31, 'a')
    # the helmet, slid forward off its collar and nodding down
    dome = Mask().ellipse(18.6, 13.2 + h, 6.8, 6.4)
    brass(f, dome)
    for x in range(14, 25):
        if f.get(x, 19 + h)[3] or x < 22:
            f.put(x, 19 + h, 'Z' if x % 3 == 0 else 'Y')
    f.put(15, 6 + h, 'y'); f.put(16, 6 + h, 'Z'); f.put(16, 7 + h, 'Z')
    overlay(f, 23, 21, ["yZ", "YZ"])                           # the near glove, tucked
    if not back:
        port_o = Mask().ellipse(21.0, 15.0 + h, 3.6, 3.4)
        port_i = Mask().ellipse(21.0, 15.0 + h, 2.6, 2.4)
        for x, y in ring(port_o, port_i).px:
            f.put(x, y, 'Z')
        water(f, port_i.px, i, air=1, lights=[(20, 14 + h), (22, 15 + h)], fish_row=16 + h, fish_from=23)
    if i in (3, 4, 5):
        bubble(f, 21, 5 - (i - 3) + h, big=i == 4)
    c = Canvas(CELL, CELL)
    c.draw(f.c, 2, 0)                                          # recentred: the hose hangs out on the far side
    return c.flip() if back else c


TAKES = [
    ('BellJar', bell_jar),
    ('FishTank', fish_tank),
    ('Slump', slump),
    ('Fishbowl', fishbowl),
    ('Hunch', hunch),
]
LABELS = {'BellJar': 'Bell Jar', 'FishTank': 'Fish Tank', 'Slump': 'Slump', 'Fishbowl': 'Fishbowl', 'Hunch': 'Hunch'}


def main():
    out = sys.argv[1] if len(sys.argv) > 1 else os.path.join(REPO, 'docs', 'concepts', 'argo_hollow_2026-11-04', 'art')
    os.makedirs(out, exist_ok=True)
    first = [('Hollow', A.hollow)] + TAKES
    lineup_f, lineup_b, idles = [], [], []
    for name, fn in first:
        idle = [fn(i) for i in range(6)]
        back = fn(0, True)
        lineup_f.append(idle[0]); lineup_b.append(back); idles.append(idle)
        if name == 'Hollow':
            continue
        cells = idle + [back]
        base_dir = os.path.join(out, name)
        s = sheet(cells, 7, CELL)
        save_png(s, os.path.join(base_dir, 'game_size', 'Argo_Hollow_%s.png' % name))
        save_png(preview(s, CELL, 8), os.path.join(base_dir, 'preview_8x', 'Argo_Hollow_%s_8x.png' % name))
        save_ase(os.path.join(base_dir, 'aseprite', 'Argo_Hollow_%s.aseprite' % name), CELL, cells,
                 [('Idle_v0', 0, 5, 'CONCEPT;front heading right'), ('Back_v2', 6, 6, 'CONCEPT;back, standing')],
                 layer='CONCEPT art')
        gif([idle], os.path.join(base_dir, 'preview_8x', 'Argo_Hollow_%s_idle.gif' % name), k=8, ms=125)
        for r in A.check(name, cells):
            print('%-9s frame %d: %2d x %2d, centre %.1f, %2d colours %s' % ((name,) + r[:5] + (
                ('BAD ' + ', '.join(r[5])) if r[5] else '',)))
    gif(idles, os.path.join(out, 'argo_hollow_idle.gif'), k=6, ms=125)
    save_png(preview(sheet(lineup_f + lineup_b, 6, CELL), CELL, 8), os.path.join(out, 'argo_hollow_lineup_8x.png'))
    one = Canvas(6 * 40, 44)
    for y in range(one.h):
        for x in range(one.w):
            one.c[y * one.w + x] = (0x8C, 0x8A, 0x86, 255) if y < 36 else (0x6E, 0x6C, 0x69, 255)
    for k, c in enumerate(lineup_f):
        one.draw(c, 4 + k * 40, 4)
    save_png(one.scaled(3), os.path.join(out, 'argo_hollow_lineup_1x_x3.png'))


if __name__ == '__main__':
    main()
