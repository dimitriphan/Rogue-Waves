"""Spool's minions at Snoble size, ten versions each, in Dimitri's Spool colours (his purple is the one exception to
"magenta means foe", 2026-10-15): the Thread Head (the larger one, docs/scope/08 "works like Boutman's fish") and the
four Crafted, Stitched ("short and stubby plushie"), Dart ("thin and small"), Seam ("round plushie, similar to
Snobles") and Mangled ("similar to Snobles but much more jagged").

Sheets keep the wired names and layouts (SpoolAssets.Unit): Spoolhead-Sheet.png 240 x 96 (24 px cells), the Crafted
160 x 64 (16 px cells); rows v0-v3, columns 0-5 Move (column 0 doubles as Idle), 6-9 Attack."""
from mlib import H, Mask, oval, dot, line, feet, eyes, fill, shade

PLUSH = (H('9363AE'), H('784B94'), H('613A7B'))
LILAC = (H('D5BEEC'), H('B292D6'), H('9A7CC4'))
FELT = (H('F6F3F8'), H('D8D1E2'), H('B5ABC6'))
MOUTH, MOUTH2, EYE = H('41174F'), H('4A1D5A'), H('E6D85A')
TONGUE = (H('E58CC0'), H('C96CA6'))
TEETH = (H('E4DEDA'), H('9A938E'))
STEEL = (H('DCE3EA'), H('8C96A3'), H('6E7782'))
WOOD = (H('826D60'), H('64544A'), H('5A4C42'))
PEARL = (H('F2D9E6'), H('C9A8BE'))
SPARK = (H('FFFFFF'), H('E6D85A'))
THREAD = H('B58BC9')
YEL = (H('F6E43B'), H('D9C52E'))
DARK = H('333232')


def zipmouth(c, p, x, y, w, gap=1, tongue=True, eyes_at=None):
    """Spool's zipper mouth (front only): upper teeth, a dark opening `gap` rows tall with two yellow eyes looking out,
    lower teeth, the pink tongue hanging at the right end."""
    if not p.front:
        return
    for k in range(w):
        c.set(x + k, y, TEETH[k % 2])
        c.set(x + k, y + gap + 1, TEETH[(k + 1) % 2])
        for r in range(gap):
            c.set(x + k, y + 1 + r, MOUTH)
    ex = eyes_at or (x + 1, x + w - 2)
    if not p.blink:
        for e in ex:
            c.set(e, y + 1, EYE)
    if tongue:
        c.set(x + w - 1, y + gap + 1, TONGUE[0])
        c.set(x + w - 1, y + gap + 2, TONGUE[1])


def legs(c, p, xs, from_y, tones=PLUSH):
    """1 px legs from the body's bottom to the ground; p.lift raises one."""
    for k, x in enumerate(xs):
        for y in range(from_y, p.g + 1 - p.lift[k]):
            c.set(x, y, tones[1] if y == p.g - p.lift[k] else tones[0])


def gloves(c, p, back, front, y, tones=FELT, size=1):
    """White gloves: the back one behind the body (drawn first by the caller), the front one swings, punches in attack."""
    sw = p.arm if p.kind != 'attack' else 0
    reach = max(p.arm, 0) if p.kind == 'attack' else 0
    for k in range(size):
        c.set(back - k, y - sw, tones[1])
        c.set(front + reach + k, y + sw - (1 if p.atk == 1 else 0), tones[0] if k == 0 else tones[1])
    if p.fx == 2:
        fx = front + reach + size
        c.set(fx, y - 1, SPARK[0])
        c.set(fx + 1, y, SPARK[1])
        c.set(fx, y + 1, SPARK[1])


def stitches(c, pts, col=TEETH[1]):
    for x, y in pts:
        c.set(x, y, col)


def base(p, lift_body=1):
    """(cx, bottom) for a body standing on legs `lift_body` rows tall."""
    return p.cx + p.dx, p.g - lift_body + p.dy


def body_oval(c, p, rx, ry, tones, legs_h=1, cx_off=0.0):
    cx, bottom = base(p, legs_h)
    m = oval(c, cx + cx_off, bottom, rx + 0.5 * p.sq, ry - 0.5 * p.sq, tones)
    top = min(y for _, y in m.px)
    return cx, top, bottom, m


# ================================================================ Thread Head (24 px cells)

def th_frame(c, p, ball=LILAC, rx=5.0, ry=4.5, legs_on=True, wraps=True):
    cx, bottom = p.cx + p.dx, p.g - (2 if legs_on else 0) + p.dy
    if legs_on:
        legs(c, p, (cx - 2, cx + 1), bottom + 1)
    m = oval(c, cx, bottom, rx + 0.5 * p.sq, ry - 0.5 * p.sq, ball)
    top = min(y for _, y in m.px)
    if wraps:
        for k, (dx0, dy0) in enumerate(((-3, 1), (-1, 0))):
            for j in range(4):
                x, y = cx + dx0 + j, top + dy0 + 1 + j
                if (x, y) in m.px and c.get(x, y) == ball[0]:
                    c.set(x, y, ball[1])
    return cx, top, bottom, m


def end_thread(c, p, x, y, length=3):
    sw = [0, 1, 1, 0, -1, -1][p.i] if p.kind == 'move' else 0
    for k in range(length):
        c.set(x - k + (sw if k == length - 1 else 0), y + k, THREAD)


def th01(c, p):
    """Yarn Ball: today's Thread Head at Snoble size: a lilac ball with wraps, the zipper mouth, gloves, a loose end."""
    end_thread(c, p, p.cx - 5 + p.dx, p.g - 4 + p.dy)
    cx, top, bottom, m = th_frame(c, p)
    gloves(c, p, cx - 6, cx + 5, top + 5)
    zipmouth(c, p, cx - 1, top + 3, 5)


def th02(c, p):
    """Pom-pom: a fluffy pom-pom with a ragged edge that bounces instead of walking; gloves bob beside it."""
    cx, bottom = p.cx + p.dx, p.g + p.dy
    m = oval(c, cx, bottom, 5.0 + 0.5 * p.sq, 5.0 - 0.5 * p.sq, LILAC, mask_only=True)
    for x, y in list(m.px):
        if (x * 7 + y * 3) % 5 == 0 and (x + 1, y) not in m.px:
            m.px.add((x + 1, y))
        if (x * 3 + y * 5) % 4 == 0 and (x, y - 1) not in m.px:
            m.px.add((x, y - 1))
    shade(c, m, LILAC, centre=(cx - 1.3, bottom - 5.5), radii=(5, 5), cut=(-0.15, -0.75))
    for x, y in m.px:
        if (x + y) % 3 == 0 and c.get(x, y) == LILAC[0]:
            c.set(x, y, H('E6D8F4'))
    top = min(y for _, y in m.px)
    gloves(c, p, cx - 6, cx + 5, top + 6)
    zipmouth(c, p, cx - 1, top + 4, 5)


def th03(c, p):
    """Skein: a long twisted skein of yarn lying across two legs, the mouth on its twist."""
    cx, bottom = p.cx + p.dx, p.g - 2 + p.dy
    legs(c, p, (cx - 2, cx + 1), bottom + 1)
    m = oval(c, cx, bottom, 6.0 + 0.5 * p.sq, 3.5 - 0.5 * p.sq, LILAC)
    top = min(y for _, y in m.px)
    for y in range(top, bottom + 1):
        for x in (cx - 4 + (y - top) // 2, cx + 3 - (y - top) // 2):
            if (x, y) in m.px:
                c.set(x, y, LILAC[1])
    gloves(c, p, cx - 7, cx + 6, top + 4)
    zipmouth(c, p, cx - 1, top + 2, 5, gap=1)


def th04(c, p):
    """Bobbin: a wooden bobbin wound thick with purple thread; the mouth opens in the thread between the flanges."""
    cx, bottom = p.cx + p.dx, p.g - 2 + p.dy
    legs(c, p, (cx - 2, cx + 1), bottom + 1, WOOD)
    h = 9 - p.sq
    top = bottom - h + 1
    core = Mask().rect(cx - 4, top + 2, cx + 3, bottom - 2)
    fill(c, core, PLUSH)
    for y in range(top + 3, bottom - 1, 2):
        for x in range(cx - 4, cx + 4):
            if c.get(x, y) == PLUSH[0]:
                c.set(x, y, H('A274BC'))
    for y0 in (top, bottom - 1):
        fl = Mask().rect(cx - 5, y0, cx + 4, y0 + 1)
        fill(c, fl, WOOD)
    gloves(c, p, cx - 6, cx + 5, top + 5)
    zipmouth(c, p, cx - 2, top + 3, 5, tongue=True)


def th05(c, p):
    """Beanie: the ball wears a purple knitted beanie with a white pom-pom that flops as it walks."""
    cx, top, bottom, m = th_frame(c, p, wraps=False)
    hat = Mask()
    for y in range(top - 1, top + 3):
        for x in range(cx - 4, cx + 4):
            if (x, y) in m.px or y < top + 1:
                hat.px.add((x, y))
    hat.px = {q for q in hat.px if not (q[1] == top - 1 and q[0] in (cx - 4, cx + 3))}
    fill(c, hat, PLUSH)
    for x in range(cx - 4, cx + 4):
        c.set(x, top + 2, PLUSH[1] if x % 2 else PLUSH[0])
    pom = p.sway if p.kind != 'move' else -p.arm
    oval(c, cx + 0.5 + pom, top - 1, 1.5, 1.5, FELT)
    gloves(c, p, cx - 6, cx + 5, top + 6)
    zipmouth(c, p, cx - 1, top + 4, 5)


def th06(c, p):
    """Tangle: a messy ball with loops of loose thread sticking out all over it."""
    cx, top, bottom, m = th_frame(c, p)
    loops = [(-5, 2), (-4, -1), (2, -1), (5, 1), (-2, -2)]
    wig = [0, 1, 0, -1, 0, 1][p.i] if p.kind == 'move' else 0
    for ox, oy in loops:
        x, y = cx + ox, top + oy + 1
        c.set(x, y - 1 + (wig if ox < 0 else 0), THREAD)
        c.set(x + (1 if ox > 0 else -1), y, THREAD)
    end_thread(c, p, cx - 5, bottom - 1, 4)
    gloves(c, p, cx - 6, cx + 5, top + 5)
    zipmouth(c, p, cx - 1, top + 3, 5)


def th07(c, p):
    """Candy Stripe: wound from purple and white yarn in turns, so it reads like a sweet."""
    cx, top, bottom, m = th_frame(c, p, wraps=False)
    for x, y in m.px:
        if ((x - y) // 2) % 2 == 0:
            cur = c.get(x, y)
            c.set(x, y, {LILAC[0]: FELT[0], LILAC[1]: FELT[1], LILAC[2]: FELT[2]}.get(cur, cur))
    gloves(c, p, cx - 6, cx + 5, top + 5, tones=(H('FFFFFF'), FELT[1]))
    zipmouth(c, p, cx - 1, top + 3, 5)


def th08(c, p):
    """Knitting Needles: two needles stuck crossways through the top of the ball, pearl heads up."""
    k = 1 if p.kind == 'move' and p.i in (1, 2) else 0
    for x0, d in ((p.cx - 4 + p.dx, 1), (p.cx + 3 + p.dx, -1)):
        y0 = p.g - 15 + p.dy - k + p.sq
        line(c, x0, y0, x0 + 4 * d, p.g - 6 + p.dy, STEEL[1])
        c.set(x0 + d, y0 + 1, STEEL[0])
        c.set(x0, y0, PEARL[0])
        c.set(x0 - d, y0, PEARL[1])
    cx, top, bottom, m = th_frame(c, p)
    gloves(c, p, cx - 6, cx + 5, top + 5)
    zipmouth(c, p, cx - 1, top + 3, 5)


def th09(c, p):
    """Hooded: the yarn ball wears Spool's purple hood, its point rising at the upper right."""
    cx, top, bottom, m = th_frame(c, p, wraps=False)
    hood = Mask()
    for x, y in m.px:
        if y <= top + 2 or (x - cx) ** 2 + (y - top - 4) ** 2 > 16 and y < bottom - 1:
            hood.px.add((x, y))
    tipx = cx + 3 + (p.sway if p.kind != 'move' else -p.arm)
    hood.px |= {(tipx, top - 1), (tipx + 1, top - 2), (tipx - 1, top - 1), (tipx, top)}
    fill(c, hood, PLUSH)
    gloves(c, p, cx - 6, cx + 5, top + 5)
    zipmouth(c, p, cx - 1, top + 3, 5)


def th10(c, p):
    """Big Mouth: the zipper runs the whole width of the ball and the tongue lolls out; tiny gloves."""
    cx, top, bottom, m = th_frame(c, p, wraps=False)
    gape = 2 if p.atk in (1, 2) else 1
    if p.front:
        zipmouth(c, p, cx - 4, top + 3, 9, gap=gape, tongue=False, eyes_at=(cx - 2, cx + 2))
        c.set(cx + 3, top + 4 + gape, TONGUE[0])
        c.set(cx + 3, top + 5 + gape, TONGUE[0])
        c.set(cx + 4, top + 5 + gape, TONGUE[1])
    gloves(c, p, cx - 6, cx + 5, top + 7)


# ================================================================ Crafted (16 px cells)

def stitched_face(c, p, cx, top, w=4, y_off=2):
    zipmouth(c, p, cx - 1, top + y_off, w, tongue=True)


def hood_tip(c, p, x, y, tones=PLUSH):
    s = p.sway if p.kind != 'move' else -p.arm
    c.set(x + max(s, 0), y, tones[0])
    c.set(x + 1 + max(s, 0), y - 1, tones[1])


def st01(c, p):
    """Mini Spool: Spool in small, his hood point at the upper right, the zipper mouth with the eyes, white gloves."""
    cx, top, bottom, m = body_oval(c, p, 3.5, 3.5, PLUSH)
    legs(c, p, (cx - 2, cx + 1), bottom + 1)
    hood_tip(c, p, cx + 2, top)
    gloves(c, p, cx - 5, cx + 4, top + 5)
    stitched_face(c, p, cx, top)


def st02(c, p):
    """Bean: a legless purple bean that hops like a Snoble, the mouth on its front."""
    cx, bottom = p.cx + p.dx, p.g + p.dy
    m = oval(c, cx, bottom, 3.5 + 0.5 * p.sq, 3.5 - 0.5 * p.sq, PLUSH)
    top = min(y for _, y in m.px)
    gloves(c, p, cx - 5, cx + 4, top + 4)
    stitched_face(c, p, cx, top, y_off=2)


def st03(c, p):
    """Mitten: a purple mitten walking on its cuff; the thumb is its arm and swings as it goes."""
    cx, bottom = p.cx + p.dx, p.g - 1 + p.dy
    legs(c, p, (cx - 2, cx + 1), bottom + 1)
    m = Mask().ellipse(cx, bottom - 3.5 + 0.5 * p.sq, 3, 3.5 - 0.5 * p.sq)
    m.px |= Mask().rect(cx - 3, bottom - 1, cx + 2, bottom).px
    fill(c, m, PLUSH)
    for x in range(cx - 3, cx + 3):
        c.set(x, bottom, FELT[1] if x % 2 else FELT[0])
    sw = p.arm if p.kind != 'attack' else 0
    th = Mask().rect(cx + 3, bottom - 4 + sw - (p.arm if p.kind == 'attack' else 0), cx + 4, bottom - 3 + sw)
    fill(c, th, PLUSH)
    top = min(y for _, y in m.px)
    stitched_face(c, p, cx - 1, top, y_off=2)
    if p.fx == 2:
        c.set(cx + 6, top + 2, SPARK[0])
        c.set(cx + 6, top + 4, SPARK[1])


def st04(c, p):
    """Sock Puppet: a striped sock standing on its heel, the mouth at the toe."""
    cx, bottom = p.cx + p.dx, p.g + p.dy
    m = Mask().rect(cx - 3, bottom - 7 + p.sq, cx, bottom)
    m.px |= Mask().ellipse(cx + 0.5, bottom - 5 + p.sq, 3, 2.5).px
    fill(c, m, PLUSH)
    for y in range(bottom - 7 + p.sq, bottom + 1, 3):
        for x in range(cx - 3, cx + 1):
            if (x, y) in m.px:
                c.set(x, y, LILAC[1])
    top = min(y for _, y in m.px)
    zipmouth(c, p, cx - 1, top + 1, 4)
    gloves(c, p, cx - 5, cx + 4, top + 5)


def st05(c, p):
    """Teddy Ears: a round plush with two little ears sewn on top, ears flick as it walks."""
    cx, top, bottom, m = body_oval(c, p, 3.5, 3.5, PLUSH)
    legs(c, p, (cx - 2, cx + 1), bottom + 1)
    fl = 1 if p.kind == 'move' and p.i in (1, 4) else 0
    for ex in (cx - 3, cx + 2):
        c.set(ex, top - fl, PLUSH[0])
        c.set(ex + 1, top - fl, PLUSH[1])
        c.set(ex, top + 1 - fl, PLUSH[1])
    oval(c, cx, bottom, 3.5 + 0.5 * p.sq, 3.5 - 0.5 * p.sq, PLUSH)
    gloves(c, p, cx - 5, cx + 4, top + 5)
    stitched_face(c, p, cx, top)


def st06(c, p):
    """Patchwork: sewn from purple and lilac squares with the seams showing."""
    cx, top, bottom, m = body_oval(c, p, 3.5, 3.5, PLUSH)
    legs(c, p, (cx - 2, cx + 1), bottom + 1)
    for x, y in m.px:
        if (x >= cx) == (y >= top + 4) and c.get(x, y) == PLUSH[0]:
            c.set(x, y, LILAC[1])
        if x == cx - 1 or y == top + 3:
            if c.get(x, y) in (PLUSH[0], LILAC[1]):
                c.set(x, y, FELT[2] if (x + y) % 2 else PLUSH[1])
    gloves(c, p, cx - 5, cx + 4, top + 5)
    if p.front:
        zipmouth(c, p, cx - 1, top + 2, 4)


def st07(c, p):
    """Floppy Hood: the hood's point is long and flops over to the back; it bounces on every step."""
    cx, top, bottom, m = body_oval(c, p, 3.5, 3.5, PLUSH)
    legs(c, p, (cx - 2, cx + 1), bottom + 1)
    f = [0, 1, 1, 0, 1, 1][p.i] if p.kind == 'move' else (1 if p.atk in (0, 1) else 0)
    pts = [(cx, top - 1), (cx - 1, top - 2 + f), (cx - 2, top - 2 + f), (cx - 3, top - 1 + f), (cx - 4, top + f)]
    for k, (x, y) in enumerate(pts):
        c.set(x, y, PLUSH[0] if k < 2 else PLUSH[1])
    c.set(cx - 4, top + 1 + f, FELT[0])
    gloves(c, p, cx - 5, cx + 4, top + 5)
    stitched_face(c, p, cx, top)


def st08(c, p):
    """Boxer: a stubby square plush in oversized white gloves; it jabs."""
    cx, bottom = p.cx + p.dx, p.g - 1 + p.dy
    legs(c, p, (cx - 2, cx + 1), bottom + 1)
    m = Mask().rect(cx - 3, bottom - 6 + p.sq, cx + 2, bottom)
    m.px -= {(cx - 3, bottom - 6 + p.sq), (cx + 2, bottom - 6 + p.sq)}
    fill(c, m, PLUSH)
    top = bottom - 6 + p.sq
    stitched_face(c, p, cx, top, y_off=1)
    gloves(c, p, cx - 5, cx + 3, top + 4, size=2)
    gloves(c, p, cx - 5, cx + 3, top + 5, size=2)


def st09(c, p):
    """Tongue Out: round and cheerful with a long tongue that wags as it walks."""
    cx, top, bottom, m = body_oval(c, p, 3.5, 3.5, PLUSH)
    legs(c, p, (cx - 2, cx + 1), bottom + 1)
    gloves(c, p, cx - 5, cx + 4, top + 5)
    if p.front:
        zipmouth(c, p, cx - 2, top + 2, 5, tongue=False)
        w = [0, 1, 1, 0, -1, -1][p.i] if p.kind == 'move' else 0
        for k in range(3):
            c.set(cx + 1 + (w if k == 2 else 0), top + 4 + k, TONGUE[0] if k < 2 else TONGUE[1])


def st10(c, p):
    """Button Front: a big white button sewn on its chest; the eyes look out of the zipper above it."""
    cx, top, bottom, m = body_oval(c, p, 3.5, 4.0, PLUSH)
    legs(c, p, (cx - 2, cx + 1), bottom + 1)
    hood_tip(c, p, cx + 2, top)
    gloves(c, p, cx - 5, cx + 4, top + 5)
    if p.front:
        zipmouth(c, p, cx - 1, top + 1, 4, tongue=False)
        b = Mask().rect(cx - 1, top + 5, cx, top + 6)
        fill(c, b, FELT)
        c.set(cx - 1, top + 5, FELT[0])
        c.set(cx, top + 6, FELT[2])


# ---------------------------------------------------------------- Dart

def needle_throw(c, p, hx, hy):
    """The Dart's needle: held raised (atk 0-1), thrown (fx 2: flying ahead), gone (atk 3)."""
    if p.kind != 'attack':
        line(c, hx, hy + 1, hx, hy - 3, STEEL[1])
        c.set(hx, hy - 4, STEEL[0])
        return
    if p.atk in (0, 1):
        line(c, hx - 1, hy - 1 - p.atk, hx + 1, hy - 4 - p.atk, STEEL[1])
        c.set(hx + 1, hy - 4 - p.atk, STEEL[0])
    elif p.atk == 2:
        line(c, hx + 2, hy - 2, hx + 5, hy - 2, STEEL[1])
        c.set(hx + 6, hy - 2, STEEL[0])
        c.set(hx + 1, hy - 1, THREAD)


def dart_body(c, p, w=2, h=7, tones=LILAC, tip=3):
    """A thin upright body: w px wide either side of the centre, h tall plus a pointed tip."""
    cx, bottom = p.cx + p.dx, p.g - 1 + p.dy
    legs(c, p, (cx - 1, cx), bottom + 1, PLUSH)
    top = bottom - h + 1 + p.sq
    m = Mask().rect(cx - w, top, cx + w - 1, bottom)
    m.px -= {(cx - w, top), (cx - w, bottom)}
    s = p.sway if p.kind != 'move' else -p.arm
    for k in range(tip):
        m.px.add((cx + (1 if k < 2 else 2) + (s if k == tip - 1 else 0) - 1, top - 1 - k))
    fill(c, m, tones)
    return cx, top, bottom, m


def da01(c, p):
    """Long Hood: thin and lilac, the hood drawn out into a long point, a needle in its leading glove. Picked
    2026-11-05 and drawn a little larger (body 5 x 8 not 4 x 7, a 4 px hood, a 5 px zipper with the eyes a pixel apart)
    so the details read."""
    cx, bottom = p.cx + p.dx, p.g - 1 + p.dy
    legs(c, p, (cx - 1, cx + 1), bottom + 1, PLUSH)
    top = bottom - 7 + p.sq
    m = Mask().rect(cx - 2, top, cx + 2, bottom)
    m.px -= {(cx - 2, top), (cx - 2, bottom), (cx + 2, top), (cx + 2, bottom)}
    s = p.sway if p.kind != 'move' else -p.arm
    for k in range(4):
        m.px.add((cx + (0 if k < 2 else 1) + (s if k == 3 else 0), top - 1 - k))
    fill(c, m, LILAC)
    zipmouth(c, p, cx - 2, top + 2, 5, tongue=False)
    c.set(cx - 3, top + 5 - p.arm, FELT[1])
    needle_throw(c, p, cx + 3, top + 5)
    c.set(cx + 3, top + 5, FELT[0])


def da02(c, p):
    """Pinhead: a lilac body with a round pearl head like a sewing pin's, the eyes looking out of a slit."""
    cx, bottom = p.cx + p.dx, p.g - 1 + p.dy
    legs(c, p, (cx - 1, cx), bottom + 1, PLUSH)
    m = Mask().rect(cx - 1, bottom - 4 + p.sq, cx, bottom)
    fill(c, m, LILAC)
    hb = bottom - 5 + p.sq
    hm = oval(c, cx, hb, 2.5, 2.5, PEARL + (H('A8899F'),))
    top = min(y for _, y in hm.px)
    if p.front:
        for x in range(cx - 1, cx + 2):
            c.set(x, top + 2, MOUTH)
        if not p.blink:
            c.set(cx + 1, top + 2, EYE)
    c.set(cx - 2, hb + 2 - p.arm, FELT[1])
    needle_throw(c, p, cx + 1, hb + 2)
    c.set(cx + 1, hb + 2, FELT[0])


def da03(c, p):
    """Quill: thin, with a white feather plume rising from its hood that bends as it runs."""
    cx, top, bottom, m = dart_body(c, p, tip=0)
    s = -p.arm if p.kind == 'move' else p.sway
    for k in range(4):
        x = cx - 1 - (k // 2) - (s if k > 1 else 0)
        c.set(x, top - 1 - k, FELT[0] if k < 3 else FELT[1])
        c.set(x - 1, top - k, FELT[1])
    zipmouth(c, p, cx - 2, top + 1, 4, tongue=False)
    c.set(cx - 3, top + 5 - p.arm, FELT[1])
    needle_throw(c, p, cx + 2, top + 5)
    c.set(cx + 2, top + 5, FELT[0])


def da04(c, p):
    """Lancer: carries a needle longer than itself upright like a lance and hurls it."""
    cx, top, bottom, m = dart_body(c, p, h=6, tip=2)
    zipmouth(c, p, cx - 2, top + 1, 4, tongue=False)
    hx, hy = cx + 2, top + 4
    if p.kind != 'attack':
        line(c, hx, hy + 3, hx, hy - 6, STEEL[1])
        c.set(hx, hy - 7, STEEL[0])
        c.set(hx, hy + 4, THREAD)
    elif p.atk in (0, 1):
        line(c, hx - 2, hy + 1, hx + 3, hy - 4 - p.atk, STEEL[1])
        c.set(hx + 3, hy - 5 - p.atk, STEEL[0])
    elif p.atk == 2:
        line(c, hx + 1, hy - 1, hx + 7, hy - 1, STEEL[1])
        c.set(hx + 7, hy - 1, STEEL[0])
    c.set(hx, hy, FELT[0])


def da05(c, p):
    """Thimble: a thin plush wearing a steel thimble as a helmet."""
    cx, top, bottom, m = dart_body(c, p, h=7, tip=0, tones=PLUSH)
    th = Mask().rect(cx - 2, top - 2, cx + 1, top + 1)
    th.px -= {(cx - 2, top - 2), (cx + 1, top - 2)}
    fill(c, th, STEEL)
    for x in range(cx - 2, cx + 2):
        if (x + top) % 2 == 0:
            c.set(x, top - 1, STEEL[2])
    zipmouth(c, p, cx - 2, top + 2, 4, tongue=False)
    c.set(cx - 3, top + 5 - p.arm, FELT[1])
    needle_throw(c, p, cx + 2, top + 5)
    c.set(cx + 2, top + 5, FELT[0])


def da06(c, p):
    """Bobbin: a slim wooden bobbin wound with lilac thread, the mouth opening in the thread."""
    cx, bottom = p.cx + p.dx, p.g - 1 + p.dy
    legs(c, p, (cx - 1, cx), bottom + 1, WOOD)
    top = bottom - 8 + p.sq
    fill(c, Mask().rect(cx - 2, top + 1, cx + 1, bottom - 1), LILAC)
    for y in range(top + 2, bottom - 1, 2):
        for x in range(cx - 2, cx + 2):
            if c.get(x, y) == LILAC[0]:
                c.set(x, y, LILAC[1])
    for y0 in (top, bottom):
        fill(c, Mask().rect(cx - 3, y0, cx + 2, y0), WOOD)
    zipmouth(c, p, cx - 2, top + 2, 4, tongue=False)
    c.set(cx - 4, top + 5 - p.arm, FELT[1])
    needle_throw(c, p, cx + 3, top + 5)
    c.set(cx + 3, top + 5, FELT[0])


def da07(c, p):
    """Scissor Legs: a little lilac body striding on two steel scissor blades."""
    cx, bottom = p.cx + p.dx, p.g - 3 + p.dy
    st = [0, 1, 1, 0, -1, -1][p.i] if p.kind == 'move' else 0
    line(c, cx - 1, bottom + 1, cx - 2 - st, p.g, STEEL[1])
    line(c, cx, bottom + 1, cx + 1 + st, p.g, STEEL[0])
    c.set(cx, bottom + 1, STEEL[2])
    top = bottom - 5 + p.sq
    m = Mask().rect(cx - 2, top, cx + 1, bottom)
    m.px -= {(cx - 2, top), (cx + 1, bottom)}
    m.px |= {(cx + 1, top - 1), (cx + 2, top - 2)}
    fill(c, m, LILAC)
    zipmouth(c, p, cx - 2, top + 1, 4, tongue=False)
    c.set(cx - 3, top + 4 - p.arm, FELT[1])
    needle_throw(c, p, cx + 2, top + 4)
    c.set(cx + 2, top + 4, FELT[0])


def da08(c, p):
    """Upright Zip: a tall thin hood whose zipper runs top to bottom; one yellow eye looks out of the gap."""
    cx, top, bottom, m = dart_body(c, p, tones=PLUSH, tip=2)
    if p.front:
        for y in range(top + 1, bottom):
            c.set(cx, y, TEETH[y % 2])
            c.set(cx - 1, y, MOUTH if top + 2 <= y <= top + 4 else c.get(cx - 1, y))
        if not p.blink:
            c.set(cx - 1, top + 3, EYE)
    c.set(cx - 3, top + 5 - p.arm, FELT[1])
    needle_throw(c, p, cx + 2, top + 5)
    c.set(cx + 2, top + 5, FELT[0])


def da09(c, p):
    """Ribbon: wrapped in a lilac ribbon whose two tails stream behind it."""
    cx, top, bottom, m = dart_body(c, p, tones=PLUSH, tip=1)
    for y in range(top + 4, bottom + 1, 2):
        for x in range(cx - 2, cx + 2):
            c.set(x, y, LILAC[1] if x > cx - 1 else LILAC[0])
    fl = [0, 1, 1, 0, 1, 1][p.i] if p.kind == 'move' else 0
    for k in range(3):
        c.set(cx - 3 - k, top + 4 + k // 2 - fl * (k // 2), LILAC[1] if k else LILAC[0])
    zipmouth(c, p, cx - 2, top + 1, 4, tongue=False)
    needle_throw(c, p, cx + 2, top + 5)
    c.set(cx + 2, top + 5, FELT[0])


def da10(c, p):
    """Tassel: a thin plush under a pointed cap with a tassel that swings as it runs."""
    cx, top, bottom, m = dart_body(c, p, tip=0, h=6)
    cap = Mask().rect(cx - 2, top - 1, cx + 1, top)
    cap.px |= {(cx - 1, top - 2), (cx, top - 2), (cx, top - 3)}
    fill(c, cap, PLUSH)
    s = -p.arm if p.kind == 'move' else p.sway
    c.set(cx + 1 + max(0, s), top - 3, YEL[0])
    c.set(cx + 1 + max(0, s), top - 2, YEL[1])
    zipmouth(c, p, cx - 2, top + 1, 4, tongue=False)
    c.set(cx - 3, top + 4 - p.arm, FELT[1])
    needle_throw(c, p, cx + 2, top + 4)
    c.set(cx + 2, top + 4, FELT[0])


# ---------------------------------------------------------------- Seam (round white felt, purple seam; mines)

def seam_line(c, m, cx, top, bottom, front=True):
    for y in range(top + 1, bottom):
        x = cx - 1 if front else cx
        if (x, y) in m.px:
            c.set(x, y, PLUSH[1])
            if y % 2 == 0:
                c.set(x - 1, y, PLUSH[0]) if (x - 1, y) in m.px else None


def felt_eyes(c, p, cx, y, gap=3):
    """Two dark dot eyes with one pixel between them, looking the way it heads."""
    eyes(c, p, (cx - 1, cx + 1), y, MOUTH, blink_col=MOUTH2)


def slam(c, p, cx, y):
    if p.fx == 2:
        for x, yy in ((cx + 5, y), (cx + 6, y - 1), (cx + 6, y + 1)):
            c.set(x, yy, SPARK[0] if x == cx + 5 else FELT[1])


def se01(c, p):
    """Felt Ball: round white felt with a purple seam down the front and cross stitches."""
    cx, top, bottom, m = body_oval(c, p, 4.0, 3.5, FELT)
    legs(c, p, (cx - 2, cx + 1), bottom + 1, PLUSH)
    seam_line(c, m, cx, top, bottom, p.front)
    felt_eyes(c, p, cx + 1, top + 2, 2)
    slam(c, p, cx, top + 3)


def se02(c, p):
    """Snowball: a legless felt ball that hops like a Snoble, one purple seam ringing it."""
    cx, bottom = p.cx + p.dx, p.g + p.dy
    m = oval(c, cx, bottom, 4.0 + 0.5 * p.sq, 3.5 - 0.5 * p.sq, FELT)
    top = min(y for _, y in m.px)
    for x in range(cx - 4, cx + 4):
        y = top + 4 + (1 if abs(x - cx) > 2 else 0)
        if (x, y) in m.px:
            c.set(x, y, PLUSH[1] if x % 2 else PLUSH[0])
    felt_eyes(c, p, cx + 1, top + 2, 2)
    slam(c, p, cx, top + 3)


def se03(c, p):
    """Baseball: two curved purple seams with stitch ticks, like a baseball on legs."""
    cx, top, bottom, m = body_oval(c, p, 4.0, 4.0, FELT)
    legs(c, p, (cx - 2, cx + 1), bottom + 1, PLUSH)
    for y in range(top + 1, bottom):
        t = y - top
        for x in (cx - 3 + abs(t - 4) // 2, cx + 2 - abs(t - 4) // 2):
            if (x, y) in m.px:
                c.set(x, y, PLUSH[0])
    felt_eyes(c, p, cx, top + 3, 2)
    slam(c, p, cx, top + 3)


def se04(c, p):
    """Dumpling: a felt dumpling pinched into pleats on top."""
    cx, top, bottom, m = body_oval(c, p, 4.0, 3.0, FELT)
    legs(c, p, (cx - 2, cx + 1), bottom + 1, PLUSH)
    for k, x in enumerate(range(cx - 2, cx + 2)):
        c.set(x, top - 1 + (0 if k in (1, 2) else 1), FELT[0] if k < 2 else FELT[1])
    for x in (cx - 2, cx, cx + 2):
        c.set(x, top + 1, FELT[2])
    c.set(cx + p.sway, top - 2, PLUSH[0])
    felt_eyes(c, p, cx + 1, top + 3, 2)
    slam(c, p, cx, top + 3)


def se05(c, p):
    """Pillow: a little square felt pillow with a tuft at each corner and a purple seam round its edge."""
    cx, bottom = p.cx + p.dx, p.g - 1 + p.dy
    legs(c, p, (cx - 2, cx + 1), bottom + 1, PLUSH)
    top = bottom - 6 + p.sq
    m = Mask().rect(cx - 3, top, cx + 2, bottom)
    fill(c, m, FELT)
    for x, y in ((cx - 4, top - 1), (cx + 3, top - 1), (cx - 4, bottom + 0), (cx + 3, bottom)):
        c.set(x, y, FELT[1])
    for x in range(cx - 3, cx + 3):
        c.set(x, top, PLUSH[0] if x % 2 else FELT[0])
    felt_eyes(c, p, cx, top + 2, 2)
    slam(c, p, cx, top + 3)


def se06(c, p):
    """Button: a big round felt button with four holes; two are its eyes."""
    cx, top, bottom, m = body_oval(c, p, 4.0, 4.0, FELT)
    legs(c, p, (cx - 2, cx + 1), bottom + 1, PLUSH)
    rim = {(x, y) for x, y in m.px if any((x + a, y + b) not in m.px for a, b in ((1, 0), (-1, 0), (0, 1), (0, -1)))}
    for x, y in rim:
        if x >= cx or y >= bottom - 1:
            c.set(x, y, FELT[2])
    holes = [(cx - 1, top + 3), (cx + 1, top + 3), (cx - 1, top + 5), (cx + 1, top + 5)]
    for k, (x, y) in enumerate(holes):
        if not p.front and k < 2:
            continue
        c.set(x, y, MOUTH if (k < 2 and p.front and not p.blink) else FELT[2])
    slam(c, p, cx, top + 3)


def se07(c, p):
    """Beanbag: a slouchy pear of felt that sags and wobbles as it waddles."""
    cx, bottom = p.cx + p.dx, p.g - 1 + p.dy
    legs(c, p, (cx - 2, cx + 1), bottom + 1, PLUSH)
    m = Mask().ellipse(cx, bottom - 2 + 0.5 * p.sq, 4 + 0.5 * p.sq, 2.6)
    m.px |= Mask().ellipse(cx - 0.5 + 0.5 * p.sway, bottom - 5 + p.sq, 2.6, 2.2).px
    fill(c, m, FELT)
    top = min(y for _, y in m.px)
    seam_line(c, m, cx - 1, top, bottom, p.front)
    felt_eyes(c, p, cx, top + 2, 2)
    slam(c, p, cx, top + 3)


def se08(c, p):
    """Eyepatch: round felt with a purple patch stitched over one eye."""
    cx, top, bottom, m = body_oval(c, p, 4.0, 3.5, FELT)
    legs(c, p, (cx - 2, cx + 1), bottom + 1, PLUSH)
    if p.front:
        eyes(c, p, (cx + 2,), top + 2, MOUTH, blink_col=MOUTH2)
        fill(c, Mask().rect(cx - 1, top + 1, cx, top + 3), PLUSH)
        c.set(cx - 2, top + 1, PLUSH[1])
    else:
        c.set(cx - 2, top + 1, PLUSH[1])
        c.set(cx - 1, top + 2, PLUSH[1])
    slam(c, p, cx, top + 3)


def se09(c, p):
    """Little Miner: carries a felt pickaxe over its shoulder and swings it to dig."""
    cx, top, bottom, m = body_oval(c, p, 3.5, 3.5, FELT)
    legs(c, p, (cx - 2, cx + 1), bottom + 1, PLUSH)
    seam_line(c, m, cx, top, bottom, p.front)
    felt_eyes(c, p, cx + 1, top + 2, 2)
    a = p.atk if p.kind == 'attack' else None
    if a in (0, 1):
        hx, hy = cx - 2, top - 2 - a
        line(c, cx + 2, top + 3, hx + 2, hy + 1, WOOD[1])
        for x in range(hx, hx + 4):
            c.set(x, hy, STEEL[1] if x != hx else STEEL[0])
    elif a == 2:
        line(c, cx + 2, top + 3, cx + 5, top + 3, WOOD[1])
        for y in range(top + 1, top + 5):
            c.set(cx + 6, y, STEEL[1] if y != top + 1 else STEEL[0])
        c.set(cx + 7, top + 5, SPARK[0])
    else:
        line(c, cx + 2, top + 4, cx + 4, top + 1 - (1 if p.kind == 'move' and p.i in (1, 4) else 0), WOOD[1])
        c.set(cx + 3, top + 0, STEEL[0])
        c.set(cx + 5, top + 1, STEEL[1])
        c.set(cx + 4, top + 0, STEEL[1])


def se10(c, p):
    """Hard Hat: a felt ball in a yellow miner's hat with a little lamp."""
    cx, top, bottom, m = body_oval(c, p, 4.0, 3.5, FELT)
    legs(c, p, (cx - 2, cx + 1), bottom + 1, PLUSH)
    hat = Mask().rect(cx - 3, top - 1, cx + 2, top + 1)
    hat.px -= {(cx - 3, top - 1), (cx + 2, top - 1)}
    hat.px |= {(cx - 4, top + 1), (cx + 3, top + 1)}
    fill(c, hat, YEL + (H('B8A325'),))
    if p.front:
        c.set(cx + 1, top, H('FFF48B'))
    felt_eyes(c, p, cx + 1, top + 3, 2)
    seam_line(c, m, cx, top + 2, bottom, p.front)
    slam(c, p, cx, top + 3)


# ---------------------------------------------------------------- Mangled (round, jagged, torn)

def jag(m, cx, cy, every=3, seed=0):
    out = set(m.px)
    for x, y in m.px:
        if (x * 5 + y * 3 + seed) % every == 0:
            nx = x + (1 if x >= cx else -1)
            ny = y + (1 if y >= cy else -1)
            out.add((nx, y) if (x + y + seed) % 2 else (x, ny))
    m.px = out
    return m


def mangled_body(c, p, tones=FELT, rx=3.5, ry=3.5, every=3, seed=0, legs_on=True):
    cx, bottom = p.cx + p.dx, p.g - (1 if legs_on else 0) + p.dy
    if legs_on:
        legs(c, p, (cx - 2, cx + 1), bottom + 1, PLUSH)
    m = Mask().ellipse(cx, bottom + 1 - ry + 0.5 * p.sq, rx + 0.5 * p.sq, ry - 0.5 * p.sq)
    jag(m, cx, bottom - ry, every, seed)
    m.px = {q for q in m.px if q[1] <= bottom}
    shade(c, m, tones, centre=(cx - 0.8, bottom - ry - 0.5), radii=(rx, ry), cut=(-0.15, -0.75))
    top = min(y for _, y in m.px)
    return cx, top, bottom, m


def odd_eyes(c, p, cx, y):
    if not p.front:
        return
    c.set(cx - 1, y, EYE if not p.blink else MOUTH2)
    c.set(cx + 1, y, FELT[2])
    c.set(cx + 2, y, MOUTH)


def ma01(c, p):
    """Torn: torn white cotton with a purple patch and mismatched eyes, one yellow, one a button."""
    cx, top, bottom, m = mangled_body(c, p)
    fill(c, Mask().rect(cx - 3, top + 4, cx - 2, top + 5), PLUSH)
    odd_eyes(c, p, cx, top + 2)
    slam(c, p, cx, top + 3)


def ma02(c, p):
    """Burst: a spiky ball of cotton tufts standing out in every direction."""
    cx, top, bottom, m = mangled_body(c, p, every=2, seed=1, legs_on=False)
    for x, y in ((cx - 5, top + 3), (cx + 4, top + 2), (cx - 1, top - 1), (cx + 2, top - 1)):
        c.set(x, y + (1 if p.kind == 'move' and p.i % 3 == 1 else 0), FELT[1])
    odd_eyes(c, p, cx, top + 3)
    slam(c, p, cx, top + 3)


def ma03(c, p):
    """Half Hood: a scrap of Spool's purple hood still sits on a lump of spilled cotton; half a mouth shows."""
    cx, top, bottom, m = mangled_body(c, p, seed=2)
    hood = {(x, y) for x, y in m.px if y <= top + 3 and x >= cx - 1}
    hood |= {(cx + 2, top - 1), (cx + 3, top - 2)}
    hm = Mask().points(hood)
    fill(c, hm, PLUSH)
    if p.front:
        for x in range(cx, cx + 3):
            c.set(x, top + 2, TEETH[x % 2])
            c.set(x, top + 3, MOUTH)
        if not p.blink:
            c.set(cx + 1, top + 3, EYE)
        c.set(cx - 2, top + 3, FELT[2])


def ma04(c, p):
    """Frankenpatch: three scraps (purple, lilac, white) stitched into one lump with the stitches showing."""
    cx, top, bottom, m = mangled_body(c, p, seed=3)
    for x, y in m.px:
        cur = c.get(x, y)
        if x < cx - 1 and y > top + 2:
            c.set(x, y, {FELT[0]: PLUSH[0], FELT[1]: PLUSH[1], FELT[2]: PLUSH[2]}.get(cur, cur))
        elif y <= top + 2:
            c.set(x, y, {FELT[0]: LILAC[0], FELT[1]: LILAC[1], FELT[2]: LILAC[2]}.get(cur, cur))
    for y in range(top + 2, bottom):
        if (cx - 2, y) in m.px:
            c.set(cx - 2, y, TEETH[1] if y % 2 else MOUTH2)
    odd_eyes(c, p, cx, top + 3)
    slam(c, p, cx, top + 3)


def ma05(c, p):
    """Ragdoll: an X stitched over one eye and one arm hanging by a thread."""
    cx, top, bottom, m = mangled_body(c, p, seed=4)
    if p.front:
        c.set(cx - 1, top + 2, MOUTH)
        c.set(cx - 2, top + 1, MOUTH)
        c.set(cx, top + 1, MOUTH)
        c.set(cx - 2, top + 3, MOUTH)
        c.set(cx, top + 3, MOUTH)
        c.set(cx + 2, top + 2, EYE if not p.blink else MOUTH2)
    sw = [0, 1, 1, 0, -1, -1][p.i] if p.kind == 'move' else (2 if p.fx == 2 else 0)
    c.set(cx + 4, top + 4, THREAD)
    c.set(cx + 4 + max(0, sw // 2), top + 5, FELT[1])
    c.set(cx + 4 + max(0, sw // 2), top + 6, FELT[1])
    slam(c, p, cx, top + 3)


def ma06(c, p):
    """Unravelled: threads hang off its underside and trail as it moves; it sags."""
    cx, top, bottom, m = mangled_body(c, p, seed=5, rx=4.0, ry=3.0)
    w = [0, 1, 1, 0, -1, -1][p.i] if p.kind == 'move' else 0
    for x in (cx - 3, cx + 2):
        c.set(x, bottom + 1, THREAD)
        c.set(x - (w if x < cx else 0), bottom + 2, THREAD)
    odd_eyes(c, p, cx, top + 2)
    slam(c, p, cx, top + 3)


def ma07(c, p):
    """Pin-studded: pins from the Pin Cushion stuck in its back, pearl heads up."""
    cx, top, bottom, m = mangled_body(c, p, seed=6)
    for x0, d in ((cx - 2, -1), (cx, 0), (cx + 2, 1)):
        y0 = min([y for x, y in m.px if x == x0] or [top])
        c.set(x0, y0, STEEL[2])
        c.set(x0 + (d if d else 0), y0 - 1, STEEL[1])
        c.set(x0 + 2 * d if d else x0, y0 - 2, PEARL[0] if d else H('E2725B'))
    odd_eyes(c, p, cx, top + 3)
    slam(c, p, cx, top + 3)


def ma08(c, p):
    """Split Seam: its seam has burst across the middle, the cotton showing like teeth."""
    cx, top, bottom, m = mangled_body(c, p, tones=PLUSH, seed=7)
    gape = 2 if p.atk in (1, 2) else 1
    if p.front:
        for x in range(cx - 3, cx + 3):
            for g in range(gape):
                if (x, top + 3 + g) in m.px:
                    c.set(x, top + 3 + g, MOUTH)
            c.set(x, top + 3 + gape, FELT[0] if x % 2 else FELT[1])
            c.set(x, top + 2, FELT[0] if x % 2 == 0 else c.get(x, top + 2))
        if not p.blink:
            c.set(cx + 1, top + 3, EYE)
    slam(c, p, cx, top + 3)


def ma09(c, p):
    """Fuse: a felt bomb with a thread fuse sparking on top, made to burst."""
    cx, top, bottom, m = mangled_body(c, p, seed=8, every=4)
    fl = p.i % 2
    c.set(cx + 1, top - 1, THREAD)
    c.set(cx + 2, top - 2, THREAD)
    c.set(cx + 2 + fl, top - 3, SPARK[fl])
    if p.fx:
        c.set(cx + 3, top - 3, SPARK[0])
        c.set(cx + 1, top - 3, SPARK[1])
    odd_eyes(c, p, cx, top + 2)
    slam(c, p, cx, top + 3)


def ma10(c, p):
    """Sandbag: a squat lump stuffed hard, patched and buttoned, built to block."""
    cx, bottom = p.cx + p.dx, p.g - 1 + p.dy
    legs(c, p, (cx - 2, cx + 1), bottom + 1, PLUSH)
    top = bottom - 6 + p.sq
    m = Mask().rect(cx - 4, top, cx + 3, bottom)
    jag(m, cx, top + 3, 4, 9)
    m.px = {q for q in m.px if q[1] <= bottom}
    fill(c, m, FELT)
    fill(c, Mask().rect(cx - 4, top + 4, cx - 2, bottom - 1), PLUSH)
    for x, y in ((cx - 3, top + 1), (cx + 2, top + 5)):
        c.set(x, y, PLUSH[2])
    odd_eyes(c, p, cx + 1, top + 2)
    slam(c, p, cx, top + 3)


TH = [('Yarn Ball', th01, 'walk'), ('Pom-pom', th02, 'hop'), ('Skein', th03, 'waddle'), ('Bobbin', th04, 'walk'),
      ('Beanie', th05, 'walk'), ('Tangle', th06, 'walk'), ('Candy Stripe', th07, 'walk'), ('Knitting Needles', th08, 'walk'),
      ('Hooded', th09, 'walk'), ('Big Mouth', th10, 'waddle')]
ST = [('Mini Spool', st01, 'walk'), ('Bean', st02, 'hop'), ('Mitten', st03, 'walk'), ('Sock Puppet', st04, 'hop'),
      ('Teddy Ears', st05, 'walk'), ('Patchwork', st06, 'walk'), ('Floppy Hood', st07, 'walk'), ('Boxer', st08, 'walk'),
      ('Tongue Out', st09, 'waddle'), ('Button Front', st10, 'walk')]
DA = [('Long Hood', da01, 'walk'), ('Pinhead', da02, 'walk'), ('Quill', da03, 'walk'), ('Lancer', da04, 'walk'),
      ('Thimble', da05, 'walk'), ('Bobbin', da06, 'walk'), ('Scissor Legs', da07, 'walk'), ('Upright Zip', da08, 'walk'),
      ('Ribbon', da09, 'walk'), ('Tassel', da10, 'walk')]
SE = [('Felt Ball', se01, 'walk'), ('Snowball', se02, 'hop'), ('Baseball', se03, 'walk'), ('Dumpling', se04, 'waddle'),
      ('Pillow', se05, 'walk'), ('Button', se06, 'walk'), ('Beanbag', se07, 'waddle'), ('Eyepatch', se08, 'walk'),
      ('Little Miner', se09, 'walk'), ('Hard Hat', se10, 'walk')]
MA = [('Torn', ma01, 'walk'), ('Burst', ma02, 'hop'), ('Half Hood', ma03, 'waddle'), ('Frankenpatch', ma04, 'walk'),
      ('Ragdoll', ma05, 'waddle'), ('Unravelled', ma06, 'waddle'), ('Pin-studded', ma07, 'walk'), ('Split Seam', ma08, 'walk'),
      ('Fuse', ma09, 'walk'), ('Sandbag', ma10, 'walk')]
MINIONS = [
    dict(key='threadhead', name='Thread Head', part='Spool', sheet='Spoolhead-Sheet', layout='head', versions=TH),
    dict(key='stitched', name='Stitched', part='Spool', sheet='Spoolstitched-Sheet', layout='unit16', versions=ST),
    dict(key='dart', name='Dart', part='Spool', sheet='Spooldart-Sheet', layout='unit16', versions=DA),
    dict(key='seam', name='Seam', part='Spool', sheet='Spoolseam-Sheet', layout='unit16', versions=SE),
    dict(key='mangled', name='Mangled', part='Spool', sheet='Spoolmangled-Sheet', layout='unit16', versions=MA),
]
