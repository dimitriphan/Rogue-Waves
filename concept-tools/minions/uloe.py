"""Ten Photons (Uloe's light minions, docs/scope/10: they wander where they spawn, follow Flood Light paths, and a hit
Blinds and Illuminates), Snoble-sized: 6-8 px wide, 7-10 tall. Colours from Uloe's sheet (UloeCharacter.P): the
electric yellows, the bulb glass, the eyes' ink blue, the screw steel. Uloephoton-Sheet.png, 160 x 64, 16 px cells:
rows v0-v3, columns 0-5 Move (column 0 doubles as Idle), 6-9 Attack: the code's own layout, so a pick is a copy once
UloeBuilder reads a file (see the README)."""
from mlib import H, Mask, oval, dot, line, feet, eyes, fill, shade

GLOW = (H('FFF48B'), H('F1EF72'), H('D4CD3C'))       # b c d
CORE = H('FFFDE0')                                   # a, x
ARM = (H('FBF236'), H('D4CD3C'))                     # R S
GLASS = (H('DAE6FC'), H('CBDBFC'), H('979FAD'))      # C D J
INK = H('3F3F74')                                    # G: the eyes
FIL = H('323C39')                                    # H: the filament
STEEL = (H('9BADB7'), H('757A7D'), H('595652'))      # K M O
WHITE = H('FFFFFF')


def zap(c, p, x, y):
    """The attack: a spark gathers (atk 0-1), jumps ahead (fx 2: a three-pixel bolt and a burst), fades (fx 1)."""
    if p.atk in (0, 1):
        c.set(x + 1, y - p.atk, CORE)
    if p.fx == 2:
        for k in range(3):
            c.set(x + 1 + k, y + (k % 2), CORE if k == 1 else ARM[0])
        for a, b in ((4, -1), (5, 0), (4, 1), (5, 2)):
            c.set(x + a, y + b, ARM[0] if b in (0, 1) else ARM[1])
    elif p.fx == 1:
        c.set(x + 4, y, ARM[1])


def face(c, p, x, y, gap=2):
    eyes(c, p, (x, x + gap), y, INK, blink_col=GLOW[2])


def wisps(c, p, cx, y, rx):
    a = p.arm
    c.set(int(cx - rx) - 1, y - a, ARM[0])
    c.set(int(cx - rx) - 2, y - a + 1, ARM[1])
    c.set(int(cx + rx), y + a, ARM[0])
    c.set(int(cx + rx) + 1, y + a - 1, ARM[1])


def ph01(c, p):
    """Spark: today's Photon at Snoble size, a glowing yellow orb with two electric wisps and a flickering tail."""
    cx, bottom = p.cx + p.dx, p.g - 3 + p.dy
    for k in range(3):
        c.set(cx + (0 if (k + p.i) % 2 else -1), bottom + 1 + k, ARM[k % 2])
    m = oval(c, cx, bottom, 3.0, 3.0, GLOW)
    top = min(y for _, y in m.px)
    c.set(cx - 2, top + 1, CORE)
    wisps(c, p, cx, top + 3, 3.0)
    face(c, p, cx, top + 2)
    zap(c, p, cx + 3, top + 3)


def ph02(c, p):
    """Mini Bulb: Uloe in small, a glass bulb with a glowing heart on a ribbed screw base; it hops like Sno Sno."""
    cx, bottom = p.cx + p.dx, p.g + p.dy
    for k, y in enumerate((bottom, bottom - 1)):
        for x in range(cx - 2, cx + 2):
            c.set(x, y, STEEL[1] if (k == 0 or x == cx + 1) else STEEL[0])
    m = oval(c, cx, bottom - 2, 3.0 + 0.5 * p.sq, 3.0 - 0.5 * p.sq, GLASS)
    top = min(y for _, y in m.px)
    oval(c, cx, bottom - 3, 1.5, 1.5, (CORE, GLOW[0], GLOW[1]), rim=False)
    c.set(cx - 2, top + 1, WHITE)
    face(c, p, cx - 1, top + 2, 2)
    zap(c, p, cx + 3, top + 3)


def ph03(c, p):
    """Firefly: a little ink-blue beetle with a big glowing tail light and two glassy wings that flutter."""
    cx, bottom = p.cx + p.dx, p.g - 2 + p.dy
    tail = Mask().ellipse(cx - 1.0, bottom - 1.5, 2.4, 2.0)
    shade(c, tail, GLOW, centre=(cx - 1.6, bottom - 2.2), radii=(2.4, 2.0))
    c.set(cx - 2, bottom - 2, CORE)
    head = Mask().rect(cx + 1, bottom - 3, cx + 2, bottom - 2)
    head.px.add((cx + 1, bottom - 1))
    fill(c, head, (H('4E4C76'), H('3F3F74'), H('2F2F57')))
    flap = [0, 1, 0, 1, 0, 1][p.i] if p.kind != 'attack' else 0
    for x, y in ((cx - 1, bottom - 4 - flap), (cx, bottom - 4 - flap), (cx, bottom - 5 - flap), (cx + 1, bottom - 5 - flap)):
        c.set(x, y, GLASS[0] if x <= cx else GLASS[1])
    c.set(cx + 3, bottom - 4, STEEL[0])
    if p.front and not p.blink:
        c.set(cx + 2, bottom - 3, CORE)
    zap(c, p, cx + 2, bottom - 2)


def ph04(c, p):
    """Star: a four-point star that turns as it drifts, flashing from a plus to a cross."""
    cx, cy = p.cx + p.dx, p.g - 4 + p.dy
    turn = (p.i // 2) % 2 if p.kind == 'move' else 0
    m = Mask().rect(cx - 1, cy - 1, cx + 1, cy + 1)
    arms = [(0, -3), (0, -2), (0, 2), (0, 3), (-3, 0), (-2, 0), (2, 0), (3, 0)] if not turn else \
        [(-2, -2), (2, -2), (-2, 2), (2, 2), (0, -2), (0, 2), (-2, 0), (2, 0)]
    m.px |= {(cx + a, cy + b) for a, b in arms}
    shade(c, m, GLOW, centre=(cx - 0.4, cy - 0.4), radii=(3, 3))
    c.set(cx, cy, CORE)
    if p.front and not p.blink:
        c.set(cx - 1, cy, INK)
        c.set(cx + 1, cy, INK)
    zap(c, p, cx + 3, cy)


def ph05(c, p):
    """Flame: a candle flame with a white heart; its tip flickers left and right."""
    cx, bottom = p.cx + p.dx, p.g - 1 + p.dy
    m = Mask().ellipse(cx, bottom - 2, 3, 3)
    fl = [0, 1, 0, -1, 0, 1][p.i] if p.kind != 'attack' else 1
    m.px |= {(cx, bottom - 5), (cx - 1, bottom - 5), (cx + fl, bottom - 6), (cx + fl, bottom - 7), (cx - 1 + fl, bottom - 6)}
    shade(c, m, (H('FFF48B'), H('F6C94A'), H('E2A83A')), centre=(cx - 0.6, bottom - 3), radii=(3, 3.5))
    for y in (bottom - 2, bottom - 3):
        c.set(cx - 1, y, CORE)
    c.set(cx, bottom - 2, CORE)
    if p.front:
        eyes(c, p, (cx - 1, cx + 1), bottom - 3, INK, blink_col=GLOW[1])
    zap(c, p, cx + 3, bottom - 3)


def ph06(c, p):
    """Filament: a drop of glass with a glowing zig-zag filament inside for a smile."""
    cx, bottom = p.cx + p.dx, p.g - 1 + p.dy
    m = oval(c, cx, bottom, 3.0, 3.5, GLASS)
    m2 = {(cx, bottom - 7), (cx - 1, bottom - 7), (cx, bottom - 8)}
    for q in m2:
        c.set(q[0], q[1], GLASS[1])
    top = min(y for _, y in m.px)
    if p.front:
        for k, x in enumerate(range(cx - 2, cx + 2)):
            c.set(x, top + 4 + (k % 2), GLOW[1] if p.i % 2 else CORE)
        eyes(c, p, (cx - 1, cx + 1), top + 2, INK, blink_col=GLASS[2])
    else:
        c.set(cx - 1, top + 4, GLOW[1])
        c.set(cx, top + 4, GLOW[1])
    c.set(cx - 2, top + 1, WHITE)
    zap(c, p, cx + 3, top + 3)


def ph07(c, p):
    """Halo: a small orb wearing a tilted ring of light that turns as it floats."""
    cx, bottom = p.cx + p.dx, p.g - 2 + p.dy
    m = oval(c, cx, bottom, 2.5, 2.5, GLOW)
    top = min(y for _, y in m.px)
    tilt = [0, 1, 1, 0, -1, -1][p.i] if p.kind == 'move' else 0
    for x in range(cx - 4, cx + 4):
        y = top + 2 + (tilt if x < cx else -tilt) // 1 * (1 if abs(x - cx) > 2 else 0)
        if (x, y) not in m.px or x in (cx - 3, cx + 2):
            c.set(x, y, ARM[0] if x < cx else ARM[1])
    face(c, p, cx, top + 2)
    zap(c, p, cx + 3, top + 3)


def ph08(c, p):
    """Comet: a bright head trailing a long tail of light that streams back as it moves."""
    cx, bottom = p.cx + p.dx + 1, p.g - 2 + p.dy
    ln = 4 if p.kind == 'move' and p.i in (1, 2, 4, 5) else 3
    for k in range(ln):
        c.set(cx - 3 - k, bottom - 2 + (k // 2), ARM[1] if k > 1 else ARM[0])
        if k < 2:
            c.set(cx - 3 - k, bottom - 1 + (k // 2), GLOW[2])
    m = oval(c, cx, bottom, 2.5, 2.5, GLOW)
    top = min(y for _, y in m.px)
    c.set(cx - 1, top + 1, CORE)
    face(c, p, cx, top + 2)
    zap(c, p, cx + 2, top + 3)


def ph09(c, p):
    """Prism: a three-sided glass shard with a beam of light caught inside it."""
    cx, bottom = p.cx + p.dx, p.g - 1 + p.dy
    m = Mask()
    for k in range(7):
        half = (k + 1) // 2
        for x in range(cx - half, cx + half + 1):
            m.px.add((x, bottom - 6 + k + p.sq * (1 if k == 0 else 0)))
    shade(c, m, GLASS, centre=(cx - 1, bottom - 3), radii=(3, 3.5))
    for k in range(4):
        c.set(cx - 1 + (k // 2), bottom - 4 + k, CORE if k < 2 else GLOW[1])
    if p.front:
        eyes(c, p, (cx - 1, cx + 1), bottom - 2, INK, blink_col=GLASS[2])
    zap(c, p, cx + 3, bottom - 3)


def ph10(c, p):
    """Plug: a little plug on two prong legs, its face lit, a cord tail trailing behind."""
    cx, bottom = p.cx + p.dx, p.g - 2 + p.dy
    for k, x in enumerate((cx - 2, cx + 1)):
        for y in range(bottom + 1, p.g + 1 - p.lift[k]):
            c.set(x, y, STEEL[0])
    m = Mask().rect(cx - 3, bottom - 4 + p.sq, cx + 2, bottom)
    m.px -= {(cx - 3, bottom - 4 + p.sq), (cx + 2, bottom - 4 + p.sq)}
    fill(c, m, (H('EDEAE0'), H('C9C6BC'), H('A8A59C')))
    if p.front:
        fill(c, Mask().rect(cx - 1, bottom - 3 + p.sq, cx + 1, bottom - 2 + p.sq), GLOW)
        eyes(c, p, (cx - 1, cx + 1), bottom - 3 + p.sq, INK, blink_col=GLOW[2])
    sw = [0, 1, 1, 0, -1, -1][p.i] if p.kind == 'move' else 0
    c.set(cx - 4, bottom - 1, H('2E2D2D'))
    c.set(cx - 5, bottom + sw // 2, H('2E2D2D'))
    c.set(cx - 6, bottom + 1, H('2E2D2D'))
    zap(c, p, cx + 2, bottom - 2)


VERSIONS = [('Spark', ph01, 'float'), ('Mini Bulb', ph02, 'hop'), ('Firefly', ph03, 'float'), ('Star', ph04, 'float'),
            ('Flame', ph05, 'float'), ('Filament', ph06, 'float'), ('Halo', ph07, 'float'), ('Comet', ph08, 'float'),
            ('Prism', ph09, 'float'), ('Plug', ph10, 'walk')]
MINIONS = [dict(key='photon', name='Photon', part='Uloe', sheet='Uloephoton-Sheet', layout='unit16', versions=VERSIONS)]
