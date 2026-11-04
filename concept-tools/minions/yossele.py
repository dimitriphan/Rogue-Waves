"""Ten Cordyfriends (Yossele's minion, docs/scope/07: a tanky melee minion that rises from a fallen foe and "resembles a
Snoble", raised by the Mushroom Cannon's Cordyceps), Snoble-sized: bodies 7-9 px wide, 8-11 px tall with the growth on
top. Sheet Yosselefriend-Sheet.png, 384 x 96, 24 px cells: rows v0-v3, columns 0-5 Idle, 6-11 Move, 12-15 Attack (the
wired sheet's layout, so a pick is a copy)."""
from mlib import H, Mask, oval, dot, line, feet, eyes, fill, shade

CREAM = (H('F2E8D0'), H('E3D3AE'), H('CDB88C'))
CREAM_FOOT = (H('CDB88C'), H('B5A077'))
CLUB = (H('F7A64A'), H('E58A2E'), H('C46F22'))
TIP = H('FDC27A')
SPORE = (H('A3AC5E'), H('8A9649'))
PUFF = (H('DCDFAE'), H('C2C88A'), H('9AA552'))
EYE, EYE2 = H('333232'), H('4A4949')
SNOW = (H('FFFFFF'), H('C7E0F2'), H('ADCDE5'))
MOREL = (H('D8C49A'), H('B39E78'), H('8C7759'))
PIT = H('6E5B44')
MOSS = (H('A3AC5E'), H('8A9649'), H('6F7A3A'))
LIME = (H('C9F28F'), H('99E550'), H('72CC35'))
GLOW = H('F2FFD8')


def body(c, p, rx, ry, tones, feet_on=True, cx=12.0):
    """A puffball standing on the ground (on two nubs when feet_on); returns cx, top row, bottom row, mask."""
    bottom = p.g - (1 if feet_on else 0) + p.dy
    cx = cx + p.dx
    rxx, ryy = rx + 0.5 * p.sq, ry - 0.5 * p.sq
    if feet_on:
        feet(c, p, (int(cx) - 3, int(cx) + 1), CREAM_FOOT, h=1 - min(p.dy, 0))
    m = oval(c, cx, bottom, rxx, ryy, tones)
    top = min(y for _, y in m.px)
    return cx, top, bottom, m


def face(c, p, cx, y, dark=EYE, gap=3, tall=1):
    eyes(c, p, (int(cx), int(cx) + gap - 1), y, dark, tall=tall, blink_col=EYE2)


def club(c, x, y, tall, lean, tones=CLUB, tip=TIP, head=2):
    """A cordyceps club rising from (x, y): a 1 px stalk and a rounded head `head` px tall, leaning `lean` px at the top."""
    hx = x + lean
    top = y - tall
    line(c, x, y, hx, top + head, tones[2])
    for k in range(head):
        c.set(hx, top + k, tones[0] if k == 0 else tones[1])
    c.set(hx, top, tip)


def puff(c, p, cx, y):
    """The attack's spore puff in front: a small round cloud (fx 2) that thins as it drifts up (fx 1)."""
    if not p.fx:
        return
    r = 1.8 if p.fx == 2 else 1.4
    px, py = cx + (5.5 if p.fx == 2 else 6.5), y - (0 if p.fx == 2 else 2)
    m = Mask().ellipse(px, py, r, r * 0.9)
    if p.fx == 1:
        m.px = {q for q in m.px if (q[0] + q[1]) % 2 == 0}
    shade(c, m, PUFF, cut=(-0.2, -0.8))
    dot(c, px + 2, py - 2, PUFF[2])
    dot(c, px - 1, py - 3, PUFF[2])


def specks(c, m, pts, tones=SPORE):
    for x, y in pts:
        if (x, y) in m.px:
            c.set(x, y, tones[0] if c.get(x, y) in (CREAM[0], SNOW[0]) else tones[1])


# ---------------------------------------------------------------- the ten

def v01(c, p):
    """Puffball: today's Cordyfriend at Snoble size. A cream puff on two nubs, three orange clubs on its crown."""
    cx, top, bottom, m = body(c, p, 4.0, 3.0, CREAM)
    sw = p.sway
    for ox, tall, lean in ((-2, 3, -1), (0, 4, 0), (2, 3, 1)):
        club(c, int(cx) + ox, top, tall, lean + (sw if ox == 0 else 0))
    cx, top, bottom, m = body(c, p, 4.0, 3.0, CREAM)
    specks(c, m, [(int(cx) - 3, top + 2), (int(cx) - 2, top + 3)] if p.front else [(int(cx) - 1, top + 2), (int(cx) + 1, top + 3), (int(cx) - 2, top + 4)])
    face(c, p, cx, top + 2)
    puff(c, p, cx, top + 3)


def v02(c, p):
    """Feeler: a legless puff that hops like a Snoble, one long club bending over its head like an antenna."""
    bottom = p.g + p.dy
    cx = 12.0 + p.dx
    m = oval(c, cx, bottom, 4.0 + 0.5 * p.sq, 3.5 - 0.5 * p.sq, CREAM)
    top = min(y for _, y in m.px)
    x0 = int(cx) - 1
    lean = 1 + (p.sway if p.kind != 'move' else -p.arm)
    line(c, x0, top, x0, top - 3, CLUB[2])
    line(c, x0, top - 3, x0 + lean, top - 5, CLUB[2])
    c.set(x0 + lean, top - 6, TIP)
    c.set(x0 + lean + 1, top - 6, CLUB[1])
    c.set(x0 + lean + 1, top - 5, CLUB[2])
    oval(c, cx, bottom, 4.0 + 0.5 * p.sq, 3.5 - 0.5 * p.sq, CREAM)
    specks(c, m, [(int(cx) - 3, bottom - 2), (int(cx) - 2, bottom - 1)])
    face(c, p, cx, top + 3)
    puff(c, p, cx, top + 3)


def v03(c, p):
    """Toadstool: an orange cap with cream spots worn like a hat over a squat cream body; eyes peek under the brim."""
    cx, top, bottom, m = body(c, p, 3.0, 2.5, CREAM)
    capb = top + 2
    cap = oval(c, cx + 0.3 * p.sway, capb, 4.5, 2.5, CLUB)
    ct = min(y for _, y in cap.px)
    for x, y in ((int(cx) - 2, ct + 1), (int(cx) + 1, ct), (int(cx) + 2, ct + 2)):
        if (x, y) in cap.px:
            c.set(x, y, TIP if x < cx + 2 else CREAM[1])
    for x in range(int(cx) - 3, int(cx) + 4):
        if (x, capb) in cap.px:
            c.set(x, capb, CLUB[2])
    face(c, p, cx, capb + 1)
    puff(c, p, cx, capb + 1)


def v04(c, p):
    """Antlers: two forked orange growths like little antlers; it charges head first."""
    cx, top, bottom, m = body(c, p, 4.0, 3.0, CREAM)
    for side in (-1, 1):
        x = int(cx) + (-2 if side < 0 else 1)
        s = p.sway if side > 0 else 0
        line(c, x, top, x + side, top - 3, CLUB[2])
        c.set(x + side + s, top - 4, TIP)
        c.set(x + 2 * side, top - 3, CLUB[1])
        c.set(x + 2 * side, top - 4, CLUB[0])
        c.set(x, top - 2, CLUB[1])
        c.set(x - side, top - 3, CLUB[0])
    oval(c, cx, bottom, 4.0 + 0.5 * p.sq, 3.0 - 0.5 * p.sq, CREAM)
    specks(c, m, [(int(cx) - 3, top + 3)])
    face(c, p, cx, top + 2)
    puff(c, p, cx, top + 3)


def v05(c, p):
    """Spore Cloud: short stubby clubs and three olive spore motes circling it as it breathes."""
    cx, top, bottom, m = body(c, p, 4.0, 3.0, CREAM)
    for ox, tall in ((-1, 2), (1, 2)):
        club(c, int(cx) + ox, top, tall, 0, head=1)
    oval(c, cx, bottom, 4.0 + 0.5 * p.sq, 3.0 - 0.5 * p.sq, CREAM)
    orbit = [(-6, -1), (-5, -4), (-2, -6), (2, -6), (5, -4), (6, -1)]
    k = p.i if p.kind != 'attack' else p.i + 2
    for j in range(3):
        ox, oy = orbit[(k + 2 * j) % 6]
        c.set(int(cx) + ox, top + 3 + oy, SPORE[0] if j else PUFF[0])
    face(c, p, cx, top + 2)
    puff(c, p, cx, top + 3)


def v06(c, p):
    """Snowball Host: a snow-white puff that hops like a Snoble, orange clubs bursting out of its back."""
    bottom = p.g + p.dy
    cx = 12.0 + p.dx
    rx, ry = 4.0 + 0.5 * p.sq, 3.5 - 0.5 * p.sq
    m = oval(c, cx, bottom, rx, ry, SNOW, mask_only=True)
    top = min(y for _, y in m.px)
    back = int(cx) - 3 if p.front else int(cx) - 1
    for ox, tall, lean in ((0, 3, -1), (1, 4, -1 + p.sway), (2, 2, 0)):
        club(c, back + ox, top + 1, tall, lean)
    oval(c, cx, bottom, rx, ry, SNOW)
    for x, y in ((back, top + 1), (back + 1, top + 1)):
        c.set(x, y, CLUB[1])
    face(c, p, cx, top + 3, gap=3)
    puff(c, p, cx, top + 3)


def v07(c, p):
    """Morel: a tall honeycombed morel cap on a pale little body; the pits catch the shade."""
    cx, top, bottom, m = body(c, p, 3.5, 2.5, CREAM)
    capb = top + 1
    cap = Mask()
    h = 6 - (1 if p.sq > 0 else 0)
    for k in range(h):
        half = [1, 2, 2, 3, 3, 3][k]
        for x in range(int(cx) - half, int(cx) + half):
            cap.px.add((x + (p.sway if k < 2 else 0), capb - h + 1 + k))
    shade(c, cap, MOREL, centre=(cx - 1, capb - 3), radii=(3, 3))
    for x, y in cap.px:
        if (x * 2 + y * 3) % 5 == 0:
            c.set(x, y, PIT)
    face(c, p, cx, capb + 2)
    puff(c, p, cx, capb + 2)


def v08(c, p):
    """Moss Lump: an olive mossy body with bright orange club tips poking out all over its back."""
    cx, top, bottom, m = body(c, p, 4.0, 3.5, MOSS)
    for x, y in ((int(cx) - 3, top + 1), (int(cx) - 1, top - 1), (int(cx) + 2, top), (int(cx) - 4, top + 3)):
        c.set(x, y, TIP)
        c.set(x, y + 1, CLUB[1])
    for x, y in m.px:
        if (x + 2 * y) % 4 == 0 and c.get(x, y) == MOSS[0]:
            c.set(x, y, H('B8C072'))
    face(c, p, cx, top + 3, dark=H('2B2922'))
    puff(c, p, cx, top + 3)


def v09(c, p):
    """Cluster: two little puffballs have budded on its shoulder and ride along, each with an orange club."""
    cx, top, bottom, m = body(c, p, 3.5, 3.5, CREAM)
    bx = int(cx) - 3 if p.front else int(cx) + 2
    BUD = (H('FBF5E6'), H('E3D3AE'), H('CDB88C'))
    for (ox, oy, r) in ((0, -1, 1.6), (2 if p.front else -2, -3, 1.3)):
        x, y = bx + ox + (p.sway if oy < -1 else 0), top + 1 + oy
        oval(c, x + 0.5, y + 1, r, r * 0.85, BUD, cut=(-0.05, -0.6))
        c.set(x, y - int(r * 1.7), TIP)
        c.set(x, y - int(r * 1.7) + 1, CLUB[2])
    oval(c, cx + 0.5 * 0, bottom, 3.5 + 0.5 * p.sq, 3.5 - 0.5 * p.sq, CREAM, mask_only=True)
    specks(c, m, [(int(cx) - 1, top + 4)])
    face(c, p, cx, top + 3)
    puff(c, p, cx, top + 3)


def v10(c, p):
    """Glowcap: lime clubs that glow like Yossele's eyes, with glowing eyes to match."""
    cx, top, bottom, m = body(c, p, 4.0, 3.0, CREAM)
    for ox, tall, lean in ((-2, 2, -1), (0, 4, p.sway), (2, 3, 1)):
        club(c, int(cx) + ox, top, tall, lean, tones=LIME, tip=GLOW)
    oval(c, cx, bottom, 4.0 + 0.5 * p.sq, 3.0 - 0.5 * p.sq, CREAM)
    if p.front:
        y = top + 2
        for x in (int(cx), int(cx) + 2):
            c.set(x, y + 1, H('3C6B24'))
            if not p.blink:
                c.set(x, y, H('4E8A2A'))
    puff(c, p, cx, top + 3)


VERSIONS = [
    ('Puffball', v01, 'walk'), ('Feeler', v02, 'hop'), ('Toadstool', v03, 'walk'), ('Antlers', v04, 'walk'),
    ('Spore Cloud', v05, 'walk'), ('Snowball Host', v06, 'hop'), ('Morel', v07, 'waddle'), ('Moss Lump', v08, 'waddle'),
    ('Cluster', v09, 'walk'), ('Glowcap', v10, 'walk'),
]
MINIONS = [dict(key='cordyfriend', name='Cordyfriend', part='Yossele', sheet='Yosselefriend-Sheet', layout='cordy',
                versions=VERSIONS)]
