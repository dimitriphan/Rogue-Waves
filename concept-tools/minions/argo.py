"""Ten Nauts (Argo's small divers, docs/scope/09: sent to a tower, a Naut tunes it; Port, Starboard and Bosun from
Third Mate), Snoble-sized: 6-8 px wide, 8-10 tall. Each version is drawn in the three crew colours the code uses
(ArgoCharacter.Crew: Port red, Starboard green, Bosun gold) with Argo's brass, suit blue and porthole glass. Six sheets
per version, the code's names and layout: Naut_<Crew>-Sheet.png (walk) and NautTune_<Crew>-Sheet.png (tuning a tower
with a wrench), 96 x 64, 16 px cells, rows v0-v3, six frames. Argo itself keeps his shipped look."""
from mlib import H, Mask, oval, dot, line, feet, eyes, fill, shade

BRASS = (H('F2C77A'), H('D9A048'), H('A8722E'))
SUIT = (H('9CC3E0'), H('6F9DC8'), H('4C74A3'))
GLASS = (H('3D6680'), H('26405A'))
EYE = H('BDF2F0')
BOOT = (H('3E4B5C'), H('333F4E'))
WRENCH = (H('E4ECEE'), H('9AA8B0'))
WHITE = H('FFFFFF')
STEELW = (H('E4ECEE'), H('B9C4CA'), H('8E9BA3'))
CREWS = {
    'Port': (H('DE5456'), H('A93A3E')),
    'Starboard': (H('54BE6E'), H('3A8C50')),
    'Bosun': (H('F4CC56'), H('C99E32')),
}
FISH = {'Port': (H('F2A65A'), H('D9823A')), 'Starboard': (H('7BCF80'), H('54BE6E')), 'Bosun': (H('F6E43B'), H('C99E32'))}


def boots(c, p, cx, from_y, w=2):
    for k, x in enumerate((cx - 2, cx + 1)):
        for y in range(from_y, p.g + 1 - p.lift[k]):
            c.set(x, y, BOOT[0])
        if w == 2:
            c.set(x + (1 if k else -1) * 0 + (1 if k else 0) - (0 if k else 1) * 0, p.g - p.lift[k], BOOT[1])


def wrench(c, p, hx, hy):
    """Tuning (front views only, as the code): the wrench raised (1), half (2), down (4), down with a spark (3)."""
    if p.kind != 'tune' or not p.front:
        return
    t = p.tap
    if t == 1:
        pts = [(1, -1, 1), (2, -2, 1), (2, -3, 0), (3, -3, 0)]
    elif t == 2:
        pts = [(1, -1, 1), (2, -1, 1), (3, -2, 0), (3, -1, 0)]
    else:
        pts = [(1, 0, 1), (2, 0, 1), (3, 0, 0), (3, 1, 0)]
    for a, b, k in pts:
        c.set(hx + a, hy + b, WRENCH[k])
    if t == 3:
        c.set(hx + 4, hy - 1, WHITE)
        c.set(hx + 5, hy, H('FFF6C8'))


def mitt(c, p, x, y):
    sw = p.arm if p.kind == 'move' else 0
    c.set(x, y + sw, BRASS[1])


def port(c, p, cx, y, w=3, h=2):
    """The porthole on the near half: dark glass with the pale eyes looking out (front only)."""
    if not p.front:
        return
    for yy in range(h):
        for xx in range(w):
            c.set(cx + xx, y + yy, GLASS[1] if yy == h - 1 or xx == w - 1 else GLASS[0])
    if not p.blink:
        c.set(cx + 1, y, EYE)
        if w > 3:
            c.set(cx + 2, y, EYE)


def dome(c, p, cx, bottom, rx, ry, crew=None):
    m = oval(c, cx, bottom, rx, ry, BRASS)
    return m, min(y for _, y in m.px)


def suit(c, p, cx, top, bottom, tones, w=(3, 2)):
    m = Mask().rect(cx - w[0], top, cx + w[1], bottom)
    fill(c, m, tones)
    return m


def standard(c, p, crew, rx=3.0, ry=2.5, suit_h=3, suit_tones=None, band=True):
    """Boots, a crew-coloured suit and a brass dome; returns (cx, dome top, suit top)."""
    cx = p.cx + p.dx
    tun = p.sq if p.kind == 'tune' else 0
    sb = p.g - 2 + p.dy
    boots(c, p, cx, sb + 1)
    st = sb - suit_h + 1 + tun
    suit(c, p, cx, st, sb, suit_tones or CREWS[crew] + (CREWS[crew][1],))
    if band:
        for x in range(cx - 3, cx + 3):
            c.set(x, sb, BOOT[0] if x % 3 else BRASS[1])
    m, top = dome(c, p, cx - 0.5, st, rx, ry)
    mitt(c, p, cx + 3, st + 1)
    return cx, top, st, m


def na01(c, p, crew='Port'):
    """Little Diver: today's Naut at Snoble size: a brass dome with a porthole, a crew-coloured suit, dark boots."""
    cx, top, st, m = standard(c, p, crew)
    port(c, p, cx - 1, top + 2)
    wrench(c, p, cx + 3, st + 1)


def na02(c, p, crew='Port'):
    """Big Dome: a helmet far too big for the little body under it; the porthole takes up its whole front."""
    cx = p.cx + p.dx
    tun = p.sq if p.kind == 'tune' else 0
    sb = p.g - 2 + p.dy
    boots(c, p, cx, sb + 1)
    suit(c, p, cx, sb - 1, sb, CREWS[crew] + (CREWS[crew][1],), w=(2, 1))
    m, top = dome(c, p, cx - 0.5, sb - 1 + tun, 4.0, 3.5)
    if p.front:
        port(c, p, cx - 2, top + 2, w=4, h=3)
    else:
        for x in range(cx - 2, cx + 1):
            c.set(x, top + 3, BRASS[2])
    c.set(cx - 1, top - 1, BRASS[2])
    mitt(c, p, cx + 4, sb - 1)
    wrench(c, p, cx + 4, sb - 1)


def na03(c, p, crew='Port'):
    """Running Light: a crew-coloured lamp sits on top of the dome, so Port glows red, Starboard green, Bosun gold."""
    cx, top, st, m = standard(c, p, crew, suit_tones=SUIT)
    on = p.kind != 'move' or p.i % 3 != 2
    c.set(cx - 1, top - 1, CREWS[crew][0] if on else CREWS[crew][1])
    c.set(cx, top - 1, CREWS[crew][1])
    c.set(cx - 1, top - 2, WHITE if on else CREWS[crew][0])
    port(c, p, cx - 1, top + 2)
    wrench(c, p, cx + 3, st + 1)


def na04(c, p, crew='Port'):
    """Diving Bell: the whole Naut is a little brass bell with a porthole, walking on two short legs; a crew band rings it."""
    cx = p.cx + p.dx
    tun = p.sq if p.kind == 'tune' else 0
    sb = p.g - 2 + p.dy
    boots(c, p, cx, sb + 1)
    m = Mask()
    for k in range(7 - tun):
        half = [1, 2, 2, 3, 3, 3, 4][k + tun]
        for x in range(cx - half, cx + half):
            m.px.add((x, sb - 6 + tun + k))
    shade(c, m, BRASS, centre=(cx - 1.2, sb - 4), radii=(3.5, 3.5))
    for x in range(cx - 4, cx + 4):
        c.set(x, sb - 1, CREWS[crew][0] if x < cx + 2 else CREWS[crew][1])
    top = sb - 6 + tun
    c.set(cx - 1, top - 1, BRASS[2])
    port(c, p, cx - 1, top + 2)
    mitt(c, p, cx + 4, sb - 2)
    wrench(c, p, cx + 4, sb - 2)


def na05(c, p, crew='Port'):
    """Snorkel: no helmet, a crew-coloured hood with big round goggles and a snorkel that bobs as it walks."""
    cx = p.cx + p.dx
    tun = p.sq if p.kind == 'tune' else 0
    sb = p.g - 2 + p.dy
    boots(c, p, cx, sb + 1)
    suit(c, p, cx, sb - 2 + tun, sb, SUIT)
    m = oval(c, cx - 0.5, sb - 2 + tun, 3.0, 3.0, CREWS[crew] + (CREWS[crew][1],))
    top = min(y for _, y in m.px)
    if p.front:
        for x in (cx - 1, cx + 1):
            c.set(x, top + 2, GLASS[0])
            c.set(x, top + 3, GLASS[1])
        c.set(cx, top + 2, BRASS[2])
        if not p.blink:
            c.set(cx - 1, top + 2, EYE)
            c.set(cx + 1, top + 2, EYE)
    b = [0, 1, 1, 0, 1, 1][p.i] if p.kind == 'move' else 0
    for y in range(top - 2 + b, top + 2):
        c.set(cx - 3, y, BRASS[0] if y > top - 2 + b else CREWS[crew][1])
    mitt(c, p, cx + 3, sb - 1)
    wrench(c, p, cx + 3, sb - 1)


def na06(c, p, crew='Port'):
    """Hard Suit: a white steel suit with ball joints and a small dome; a crew-coloured light glows on its chest."""
    cx = p.cx + p.dx
    tun = p.sq if p.kind == 'tune' else 0
    sb = p.g - 2 + p.dy
    boots(c, p, cx, sb + 1)
    body = oval(c, cx - 0.5, sb, 3.5, 2.5, STEELW)
    bt = min(y for _, y in body.px)
    c.set(cx, bt + 2, CREWS[crew][0])
    m, top = dome(c, p, cx - 0.5, bt + tun, 2.5, 2.0)
    port(c, p, cx - 1, top + 1, w=2, h=2)
    for x, y in ((cx - 4, bt + 2), (cx + 3, bt + 2)):
        c.set(x, y + (p.arm if p.kind == 'move' and x > cx else 0), STEELW[1])
    wrench(c, p, cx + 3, bt + 2)


def na07(c, p, crew='Port'):
    """Air Hose: an air hose runs from the back of its dome down to the ground and drags behind it."""
    cx, top, st, m = standard(c, p, crew)
    sw = [0, 1, 1, 0, -1, -1][p.i] if p.kind == 'move' else 0
    pts = [(cx - 4, top + 2), (cx - 5, top + 3), (cx - 5, top + 4), (cx - 6, top + 5 + max(sw, 0)), (cx - 6, p.g)]
    for (x0, y0), (x1, y1) in zip(pts, pts[1:]):
        line(c, x0, y0, x1, y1, H('6E7782'))
    c.set(cx - 4, top + 2, BRASS[2])
    m2 = Mask().points(m.px)
    shade(c, m2, BRASS, centre=(cx - 1.5, top + 1.5), radii=(3, 2.5))
    port(c, p, cx - 1, top + 2)
    wrench(c, p, cx + 3, st + 1)


def na08(c, p, crew='Port'):
    """Fish Tank: the dome is clear glass, and the Naut inside is a little crew-coloured fish."""
    cx = p.cx + p.dx
    tun = p.sq if p.kind == 'tune' else 0
    sb = p.g - 2 + p.dy
    boots(c, p, cx, sb + 1)
    suit(c, p, cx, sb - 2 + tun, sb, SUIT)
    m = oval(c, cx - 0.5, sb - 2 + tun, 3.5, 3.0, (H('BFE6EE'), H('9CCBD8'), H('7FB0BF')))
    top = min(y for _, y in m.px)
    for x in range(cx - 3, cx + 3):
        c.set(x, sb - 2 + tun, BRASS[1] if x < cx + 2 else BRASS[2])
    fx = cx - 1 + ([0, 0, 1, 1, 0, 0][p.i] if p.kind == 'move' else 0)
    f = FISH[crew]
    if p.front or True:
        c.set(fx, top + 2, f[0])
        c.set(fx + 1, top + 2, f[0])
        c.set(fx - 1, top + 2, f[1])
        c.set(fx, top + 3, f[1])
        c.set(fx - 1, top + 1, f[1])
        if p.front and not p.blink:
            c.set(fx + 1, top + 2, H('2B2922'))
    c.set(cx - 2, top + 1, WHITE)
    mitt(c, p, cx + 3, sb - 1)
    wrench(c, p, cx + 3, sb - 1)


def na09(c, p, crew='Port'):
    """Flippers: big crew-coloured flippers that slap down as it waddles."""
    cx = p.cx + p.dx
    tun = p.sq if p.kind == 'tune' else 0
    sb = p.g - 1 + p.dy
    for k, x in enumerate((cx - 3, cx + 1)):
        y = p.g - p.lift[k]
        c.set(x, y, CREWS[crew][0])
        c.set(x + 1, y, CREWS[crew][0])
        c.set(x + 2, y, CREWS[crew][1])
    suit(c, p, cx, sb - 3 + tun, sb, SUIT)
    m, top = dome(c, p, cx - 0.5, sb - 3 + tun, 3.0, 2.5)
    port(c, p, cx - 1, top + 2)
    mitt(c, p, cx + 3, sb - 2)
    wrench(c, p, cx + 3, sb - 2)


def na10(c, p, crew='Port'):
    """Buoy: a round buoy for a body, striped crew colour and white, with a little brass dome on top."""
    cx = p.cx + p.dx
    tun = p.sq if p.kind == 'tune' else 0
    sb = p.g - 2 + p.dy
    boots(c, p, cx, sb + 1)
    body = Mask().ellipse(cx - 0.5, sb - 2.5, 3.5, 3.0)
    for x, y in body.px:
        band = ((y - sb) // 2) % 2 == 0
        tones = CREWS[crew] if band else (H('F6F3F0'), H('D8D4CE'))
        c.set(x, y, tones[1] if (x + 1, y) not in body.px or (x, y + 1) not in body.px or x > cx + 1 else tones[0])
    bt = min(y for _, y in body.px)
    m, top = dome(c, p, cx - 0.5, bt + tun, 2.5, 2.0)
    port(c, p, cx - 1, top + 1, w=2, h=2)
    mitt(c, p, cx + 3, bt + 3)
    wrench(c, p, cx + 3, bt + 3)


VERSIONS = [('Little Diver', na01, 'walk'), ('Big Dome', na02, 'waddle'), ('Running Light', na03, 'walk'),
            ('Diving Bell', na04, 'waddle'), ('Snorkel', na05, 'walk'), ('Hard Suit', na06, 'walk'), ('Air Hose', na07, 'walk'),
            ('Fish Tank', na08, 'walk'), ('Flippers', na09, 'waddle'), ('Buoy', na10, 'waddle')]
MINIONS = [dict(key='naut', name='Naut', part='Argo', sheet='Naut', layout='naut', versions=VERSIONS, tune=True,
                crews=['Port', 'Starboard', 'Bosun'])]
