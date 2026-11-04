"""Hollow, round 3 (Dimitri, 2026-11-04 05:21Z): "None of the round 2 versions appeal to me. Try 10 new designs."

Round 2 swapped the helmet for new objects (a jar, a tank, a glass bowl) and lost Argo's detailing. Round 3 goes back to
what he picked in round 1 ("Hollow, but play around with the body shape and size more"): the same riveted brass dome
with the sea in its porthole, the same faded suit and limp gloves, drawn from parts so the helmet's size and the body's
shape, size and weight can change. Ten designs:

    Big Head      a huge helmet on a tiny empty suit
    Long Legs     a small helmet on a short body and long thin legs
    Lead Boots    the weight sinks to the bottom: huge lead boots, a suit tapering to them
    Waterlogged   the suit swollen round with seawater, leaking at the cuffs
    Broad         wide shoulders under brass weights, a narrow waist
    Air Tank      a big brass tank on his back, a hose looping to the helmet
    Stout         short and wide: the helmet sits low on a squat body
    Lopsided      the helmet sagged to one side, one cuff empty and pouring sea
    Bell Skirt    the suit flares into a diving bell with a riveted brass hem
    Long Arms     long empty sleeves to the floor, the gloves dragging

    python3 concept-tools/argo_divers/hollow_ten.py [out_dir]

Same outputs and checks as argo_divers.py (kit docs/18 4.2). Default out: docs/concepts/argo_hollow_ten_2026-11-04/art.
"""
import math
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import argo_divers as A  # noqa: E402
from argo_divers import Fig, overlay, bubble, CELL  # noqa: E402
from tart import Canvas, H, preview, save_png, save_ase, sheet, gif, REPO  # noqa: E402

PAL = dict(A.HOL)
PAL.update({
    'O': H('2F3A48'),                                              # hose rubber, lead
    'D': H('24566A'),                                              # deep water on the port's far edge
    'L': H('5C6670'), 'l': H('7A8692'),                            # lead weights: main, lit
})
BOB = [0, 0, 0, -1, -1, 0]


# ------------------------------------------------------------------ parts

def dome_rows(w, h):
    """Row spans (x offsets from the left edge) of Argo's dome: a slightly squared circle, widest just below the middle
    and drawing in again at the foot before the breastplate flares out (the shipped helmet's profile)."""
    rows = []
    p = 2.6
    for r in range(h):
        t = (r + 0.5 - h * 0.55) / (h * 0.55)
        t = max(-1.0, min(1.0, t))
        half = (w / 2.0) * (1 - abs(t) ** p) ** (1 / p)
        half = max(half, 2.0)
        a = int(round(w / 2.0 - half))
        b = int(round(w / 2.0 + half)) - 1
        rows.append((a, b))
    return rows


def helm(f, cx, top, w, h, i, back=False, port=None, lean=0, valve=True):
    """Argo's dome at any size: valve on the crown, lit streak a pixel in from the left, shade band right, a bolt on
    the far side, the porthole on the near half full of sea (Hollow), a riveted breastplate under it.
    Returns the breastplate's bottom row."""
    x0 = int(round(cx - w / 2.0))
    rows = dome_rows(w, h)
    px = set()
    for r, (a, b) in enumerate(rows):
        for x in range(x0 + a, x0 + b + 1):
            px.add((x + (lean * r) // h, top + r))
    for x, y in px:
        k = 'Y'
        if (x + 1, y) not in px or (x + 2, y) not in px:
            k = 'Z'
        elif (x - 1, y) in px and (x - 2, y) not in px and y < top + h * 0.75 and y > top + 1:
            k = 'y'
        elif (x, y - 1) not in px and (x - 1, y) in px and x < cx:
            k = 'y'
        f.put(x, y, k)
    mid = top + int(h * 0.55)
    left = min(x for x, y in px if y == mid)
    f.put(left - 1, mid, 'Z'); f.put(left - 1, mid + 1, 'Z')   # the side bolt
    if valve:
        vx = cx - 1 + lean // 2
        overlay(f, vx, top - 3, ["yYZ", "YZZ", ".Z."])
    # breastplate: a riveted row and a plain one, a pixel wider than the dome's foot
    by = top + h
    a, b = rows[-1]
    for x in range(x0 + a - 1 + lean, x0 + b + 2 + lean):
        last = x >= x0 + b + lean
        f.put(x, by, 'Z' if last or (x - x0) % 3 == 0 else 'Y')
        f.put(x, by + 1, 'Z' if last else ('y' if x == x0 + a - 1 + lean else 'Y'))
    f.put(x0 + b + 2 + lean, by + 1, 'Z')
    if not back:
        pw, ph = port or (max(7, int(w * 0.58)), max(6, int(h * 0.58)))
        px0 = x0 + w - pw - 2 + lean
        py0 = top + max(3, int(h * 0.3))
        for x in range(px0 + 1, px0 + pw - 1):
            f.put(x, py0 - 1, 'y')                              # the lit lip over the port
        glass = set()
        for y in range(py0, py0 + ph):
            for x in range(px0, px0 + pw):
                corner = (x in (px0, px0 + pw - 1)) and (y in (py0, py0 + ph - 1))
                if corner:
                    continue
                if x in (px0, px0 + pw - 1) or y in (py0, py0 + ph - 1):
                    f.put(x, y, 'Z')
                else:
                    glass.add((x, y))
        sea(f, glass, i)
    else:
        cyy = top + h // 2
        for x, y in ((cx, cyy - 1), (cx - 1, cyy), (cx, cyy), (cx + 1, cyy), (cx, cyy + 1)):
            f.put(x, y, 'Z')                                    # the bolted plate on the back
    return by + 1


def sea(f, px, i):
    """Round 1's Hollow porthole: a pocket of air, a wobbling bright surface, water, two pale lights adrift, a fish."""
    if not px:
        return
    top = min(y for _, y in px)
    xs = sorted({x for x, _ in px})
    wl = [0, 1, 1, 0, 0, 1][i]
    for x, y in px:
        if y == top:
            k = 'A'
        elif y == top + 1:
            k = 'a' if (x + wl) % 3 else 'g'
        elif (x + 1, y) not in px or (x, y + 1) not in px:
            k = 'D'
        else:
            k = 'G'
        f.put(x, y, k)
    d = [(0, 1), (0, 1), (1, 1), (1, 0), (1, 0), (0, 0)][i]
    l1, l2 = xs[len(xs) // 3], xs[(2 * len(xs)) // 3]
    for n, lx in enumerate((l1, l2)):
        for dy in (0, 1):
            y = top + 2 + n + d[n] + dy
            if (lx, y) in px:
                f.put(lx, y, 'e')
    if i < 5:
        fx = xs[-1] - i
        fy = max(y for _, y in px) - 1
        for dx, k in ((0, 'x'), (1, 'X')):
            if (fx + dx, fy) in px:
                f.put(fx + dx, fy, k)


def torso(f, spans, belt=None, seam=True, back=False):
    """spans: {y: (x0, x1)}. Lit left column, shade band right, the chest seam, a belt with a brass buckle, a hem."""
    ys = sorted(spans)
    cx = None
    for y in ys:
        x0, x1 = spans[y]
        for x in range(x0, x1 + 1):
            k = 'U'
            if x >= x1 - 1:
                k = 'V'
            elif x == x0:
                k = 'u'
            f.put(x, y, k)
        if y == ys[-1]:
            for x in range(x0, x1 + 1):
                f.put(x, y, 'V')
        if cx is None:
            cx = (x0 + x1) // 2
    if seam:
        for y in ys[:-2]:
            if belt is not None and y >= belt:
                break
            if not back:
                f.put(cx, y, 'u'); f.put(cx + 1, y, 'V')
            else:
                f.put(cx + 1, y, 'V')
    if belt is not None:
        x0, x1 = spans[belt]
        for x in range(x0, x1 + 1):
            f.put(x, belt, 'o')
        f.put(cx, belt, 'y' if not back else 'Z'); f.put(cx + 1, belt, 'Z')


def legs(f, xs, top, boot_top=29, w=4):
    """Thighs (3 wide, suit) from `top` down to the boots; boots `w` wide from boot_top to row 30; a sole on row 31."""
    for x in xs:
        for y in range(top, boot_top):
            f.put(x, y, 'u' if x == xs[0] else 'U'); f.put(x + 1, y, 'U'); f.put(x + 2, y, 'V')
        for y in range(boot_top, 31):
            for xx in range(x - 1, x - 1 + w):
                f.put(xx, y, 'o')
            f.put(x - 2 + w, y, 'Y' if y == boot_top else 'Z')  # brass toe cap
        for xx in range(x, x + w - 2):
            f.put(xx, 31, 'o')


def glove(f, x, y, length=4, near=False):
    """Round 1's empty glove: a dark cuff, the brass glove hanging limp, two fingertips."""
    f.put(x + 1, y, 'o'); f.put(x + 2, y, 'o')
    for r in range(length):
        f.put(x, y + 1 + r, 'y' if r == 0 else 'Y'); f.put(x + 1, y + 1 + r, 'Y' if r < length - 1 else 'Z')
        f.put(x + 2, y + 1 + r, 'Z')
    f.put(x, y + 1 + length, 'Z'); f.put(x + 2, y + 1 + length, 'Z')


def sleeve(f, x, y0, y1):
    for y in range(y0, y1 + 1):
        f.put(x, y, 'U'); f.put(x + 1, y, 'V')


def exhale(f, x, top, i, frames=(2, 3, 4)):
    if i in frames:
        bubble(f, x, top - 1 - (i - frames[0]), big=i == frames[1])


# ------------------------------------------------------------------ the ten

def big_head(i, back=False):
    f = Fig(PAL); h = BOB[i]
    legs(f, [13, 17], 28, 29, 3)
    torso(f, {y: (11, 21) for y in range(24, 28)}, belt=26, back=back)
    glove(f, 8, 24, 3); glove(f, 22, 24, 3)
    helm(f, 16, 8 + h, 22, 14, i, back, port=(11, 8))
    exhale(f, 16, 5 + h, i)
    return f.c


def long_legs(i, back=False):
    f = Fig(PAL); h = BOB[i]
    for x in (13, 18):                                        # long thin legs, two pixels wide
        for y in range(23, 29):
            f.put(x, y, 'U'); f.put(x + 1, y, 'V')
    for x in (12, 17):
        for xx in range(x, x + 4):
            f.put(xx, 29, 'o'); f.put(xx, 30, 'o')
        f.put(x + 3, 29, 'Y'); f.put(x + 3, 30, 'Z'); f.put(x + 1, 31, 'o'); f.put(x + 2, 31, 'o')
    torso(f, {20: (11, 21), 21: (11, 21), 22: (12, 20)}, belt=21, back=back)
    sleeve(f, 9, 20, 22); sleeve(f, 22, 20, 22)
    glove(f, 9, 23, 3); glove(f, 22, 23, 3)
    helm(f, 16, 9 + h, 15, 10, i, back, port=(8, 6))
    exhale(f, 16, 6 + h, i)
    return f.c


def lead_boots(i, back=False):
    f = Fig(PAL); h = BOB[i]
    for x in (12, 18):
        for y in range(26, 28):
            f.put(x, y, 'U'); f.put(x + 1, y, 'V')
    for bx in (8, 17):                                        # lead boots: six wide, four tall, a brass strap
        for y in range(28, 32):
            for x in range(bx, bx + 7):
                f.put(x, y, 'O' if x >= bx + 5 or y == 31 else ('L' if y > 28 else 'l'))
        for x in range(bx, bx + 7):
            f.put(x, 29, 'Z' if x >= bx + 5 else 'Y')
    spans = {21: (9, 23), 22: (10, 22), 23: (10, 22), 24: (11, 21), 25: (11, 21)}
    torso(f, spans, belt=24, back=back)
    glove(f, 7, 21, 3); glove(f, 23, 21, 3)
    helm(f, 16, 9 + h, 17, 11, i, back)
    exhale(f, 16, 6 + h, i)
    return f.c


def waterlogged(i, back=False):
    f = Fig(PAL); h = BOB[i]
    legs(f, [12, 18], 29, 29, 4)
    # the suit swollen round with seawater
    spans = {}
    for y in range(19, 29):
        t = (y - 23.8) / 5.4
        half = 10.2 * math.sqrt(max(0.0, 1 - t * t))
        spans[y] = (int(round(16 - half)), int(round(16 + half)) - 1)
    torso(f, spans, belt=None, seam=False, back=back)
    for x in range(spans[25][0] + 2, spans[25][1] - 2, 4):
        f.put(x, 25, 'V')                                       # stretched seams
    # stubby cuffs pushed out by the water, leaking
    for x, y in ((5, 22), (5, 23), (26, 22), (26, 23)):
        f.put(x, y, 'o')
    drip = [None, 25, 26, 27, None, None][i]
    if drip:
        f.put(5, drip, 'a'); f.put(26, drip + 1 if drip < 27 else drip, 'a')
    helm(f, 16, 8 + h, 15, 10, i, back, port=(8, 6))
    exhale(f, 16, 5 + h, i, (3, 4, 5))
    return f.c


def broad(i, back=False):
    f = Fig(PAL); h = BOB[i]
    legs(f, [12, 18], 28, 29, 4)
    spans = {22: (5, 27), 23: (6, 26), 24: (9, 23), 25: (10, 22), 26: (11, 21), 27: (11, 21)}
    torso(f, spans, belt=26, back=back)
    for bx in (6, 23):                                        # lead weights strapped on the shoulders
        overlay(f, bx, 22, ["lLLO", "LLOO"])
    sleeve(f, 5, 24, 25); sleeve(f, 26, 24, 25)
    glove(f, 4, 26, 2); glove(f, 26, 26, 2)
    helm(f, 16, 9 + h, 15, 11, i, back)
    exhale(f, 16, 6 + h, i)
    return f.c


def air_tank(i, back=False):
    f = Fig(PAL); h = BOB[i]
    legs(f, [13, 18], 28, 29, 4)
    # the tank on his back shows past the far shoulder; from behind it fills the back
    tx = 5 if not back else 12
    tw = 5 if not back else 9
    for y in range(12, 27):
        for x in range(tx, tx + tw):
            k = 'Y'
            if x == tx + 1:
                k = 'y'
            if x >= tx + tw - 2 or y == 26:
                k = 'Z'
            if y in (15, 22):
                k = 'Z' if x != tx + 1 else 'Y'
            f.put(x, y, k)
    f.put(tx + 1, 11, 'Y'); f.put(tx + 2, 11, 'Z'); f.put(tx + 2, 10, 'Z')
    torso(f, {y: (9, 23) for y in range(22, 28)}, belt=26, back=back)
    glove(f, 6, 23, 3); glove(f, 23, 23, 3)
    helm(f, 17 if not back else 16, 8 + h, 16, 12, i, back)
    # the hose from the tank's valve loops over to the helmet
    hose = [(7, 9), (8, 8), (9, 7), (10, 7), (11, 8)] if not back else [(14, 9), (15, 8), (16, 7), (17, 7)]
    for x, y in hose:
        f.put(x, y + h, 'O')
    exhale(f, 7 if not back else 14, 8 + h, i, (1, 2, 3))
    return f.c


def stout(i, back=False):
    f = Fig(PAL); h = BOB[i]
    for bx in (9, 18):                                        # stubby boots straight under the body
        for y in range(29, 31):
            for x in range(bx, bx + 6):
                f.put(x, y, 'o')
        f.put(bx + 5, 29, 'Y'); f.put(bx + 5, 30, 'Z')
        for x in range(bx + 1, bx + 5):
            f.put(x, 31, 'o')
    spans = {y: (6, 26) for y in range(24, 29)}
    spans[28] = (7, 25)
    torso(f, spans, belt=27, back=back)
    glove(f, 3, 24, 3); glove(f, 26, 24, 3)
    helm(f, 16, 12 + h, 20, 10, i, back)
    exhale(f, 16, 9 + h, i)
    return f.c


def lopsided(i, back=False):
    f = Fig(PAL); h = BOB[i]
    legs(f, [12, 18], 28, 29, 4)
    spans = {21: (9, 21), 22: (9, 22), 23: (9, 22), 24: (10, 22), 25: (10, 22), 26: (10, 22), 27: (10, 21)}
    torso(f, spans, belt=26, back=back)
    # far sleeve long with its glove; near cuff empty, pouring sea onto the floor
    sleeve(f, 7, 21, 26); glove(f, 6, 27, 2)
    sleeve(f, 23, 21, 24); f.put(23, 25, 'o'); f.put(24, 25, 'o')
    pour = ['a', 'g', 'a', 'g', 'a', 'g']
    for n, y in enumerate(range(26, 32)):
        f.put(24, y, pour[(n + i) % 6] if pour[(n + i) % 6] else 'a')
    f.put(25, 31, 'g'); f.put(23, 31, 'a')
    # the helmet slumped toward the far side, its valve bent over
    helm(f, 15, 9 + h, 17, 11, i, back, lean=-2, valve=False)
    overlay(f, 12, 6 + h, ["yYZZ", "..ZZ"])
    exhale(f, 10, 7 + h, i)
    c = Canvas(CELL, CELL)
    c.draw(f.c, 1, 0)                                          # recentred: the hose of water hangs on the near side
    return c


def bell_skirt(i, back=False):
    f = Fig(PAL); h = BOB[i]
    spans = {}
    for y in range(19, 30):
        half = 6 + (y - 19) * 0.62
        spans[y] = (int(round(16 - half)), int(round(16 + half)) - 1)
    torso(f, spans, belt=None, seam=True, back=back)
    for x in range(spans[29][0], spans[29][1] + 1):            # a riveted brass hem, like a diving bell's rim
        f.put(x, 29, 'Z' if x >= spans[29][1] - 1 or (x - spans[29][0]) % 3 == 1 else 'Y')
        f.put(x, 30, 'Z')
    for x in (13, 14, 18, 19):
        f.put(x, 31, 'o')                                      # only the toes show under the bell
    f.put(15, 31, 'Y'); f.put(20, 31, 'Y')
    glove(f, 6, 22, 3); glove(f, 24, 22, 3)
    helm(f, 16, 8 + h, 17, 10, i, back)
    exhale(f, 16, 5 + h, i)
    return f.c


def long_arms(i, back=False):
    f = Fig(PAL); h = BOB[i]
    legs(f, [13, 18], 26, 29, 3)
    torso(f, {y: (9, 23) for y in range(21, 26)}, belt=24, back=back)
    sway = [0, 0, 1, 1, 0, 0][i]
    sleeve(f, 6, 21, 26); sleeve(f, 25, 21, 26)
    glove(f, 5, 27, 3 - (1 if sway else 0)); glove(f, 25, 27, 3)
    helm(f, 16, 9 + h, 17, 11, i, back)
    exhale(f, 16, 6 + h, i)
    return f.c


TAKES = [
    ('BigHead', big_head), ('LongLegs', long_legs), ('LeadBoots', lead_boots), ('Waterlogged', waterlogged),
    ('Broad', broad), ('AirTank', air_tank), ('Stout', stout), ('Lopsided', lopsided), ('BellSkirt', bell_skirt),
    ('LongArms', long_arms),
]


def main():
    out = sys.argv[1] if len(sys.argv) > 1 else os.path.join(REPO, 'docs', 'concepts', 'argo_hollow_ten_2026-11-04', 'art')
    os.makedirs(out, exist_ok=True)
    lineup_f, lineup_b, idles = [A.hollow(0)], [A.hollow(0, True)], [[A.hollow(i) for i in range(6)]]
    for name, fn in TAKES:
        idle = [fn(i) for i in range(6)]
        back = fn(0, True)
        lineup_f.append(idle[0]); lineup_b.append(back); idles.append(idle)
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
            print('%-11s frame %d: %2d x %2d, centre %.1f, %2d colours %s' % ((name,) + r[:5] + (
                ('BAD ' + ', '.join(r[5])) if r[5] else '',)))
    gif(idles, os.path.join(out, 'argo_hollow_ten_idle.gif'), k=4, ms=125)
    save_png(preview(sheet(lineup_f + lineup_b, 11, CELL), CELL, 6), os.path.join(out, 'argo_hollow_ten_lineup_6x.png'))
    one = Canvas(11 * 36, 44)
    for y in range(one.h):
        for x in range(one.w):
            one.c[y * one.w + x] = (0x8C, 0x8A, 0x86, 255) if y < 36 else (0x6E, 0x6C, 0x69, 255)
    for k, c in enumerate(lineup_f):
        one.draw(c, 2 + k * 36, 4)
    save_png(one.scaled(3), os.path.join(out, 'argo_hollow_ten_lineup_1x_x3.png'))


if __name__ == '__main__':
    main()
