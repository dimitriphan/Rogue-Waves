"""A Python port of the kit's low-poly mesh builder (TDS_Vessel_Creator template/Mummer/Editor/MummerLowpoly.cs)
and a small flat-shaded renderer for concept previews.

The builder keeps the C# names, arguments and tessellation (Tube, Prism, Stem, Ball, Dome, Block, Spike, Leaf, Disc,
Tri, Tri2, Quad, Push/Pop), so a model written here ports line by line into <Name>Models.cs: towers stay low-poly and
sleek like Sno Sno's and Buster's, never voxel (docs/03 section 4). Units: 1 = one tile; y is measured up from the
ground; the front faces -z; the footprint stays inside |x|, |z| <= ~0.42.

The renderer draws at the kit's preview angle (orthographic, camera rotation Euler(35, 45, 0)) with a soft key light
from the upper left, on a game-style ground block, and writes an anti-aliased RGBA PNG (3x supersampling).
"""
import math
from pngout import write_png

# ------------------------------------------------------------------ vectors
def add(a, b): return (a[0] + b[0], a[1] + b[1], a[2] + b[2])
def sub(a, b): return (a[0] - b[0], a[1] - b[1], a[2] - b[2])
def mul(a, k): return (a[0] * k, a[1] * k, a[2] * k)
def dot(a, b): return a[0] * b[0] + a[1] * b[1] + a[2] * b[2]
def cross(a, b): return (a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0])
def norm(a):
    l = math.sqrt(dot(a, a)) or 1e-9
    return (a[0] / l, a[1] / l, a[2] / l)
def lerp(a, b, t): return add(a, mul(sub(b, a), t))
UP, RIGHT = (0.0, 1.0, 0.0), (1.0, 0.0, 0.0)

def yaw_rot(p, deg):
    r = math.radians(deg)
    c, s = math.cos(r), math.sin(r)
    # Unity's Quaternion.Euler(0, yaw, 0): x' = x cos + z sin, z' = -x sin + z cos
    return (p[0] * c + p[2] * s, p[1], -p[0] * s + p[2] * c)


class Lp:
    """Same primitives as the kit's Lp. Colours are palette indices (0-63, row * 8 + column)."""

    def __init__(self):
        self.tris = []                       # (a, b, c, normal, col)
        self.stack = []
        self.xf = (0.0, 0.0, 0.0, 0.0, 1.0)  # translate x, y, z, yaw, scale

    # ------------------------------------------------------------ transform stack (position, yaw, uniform scale)
    def Push(self, pos, yaw=0.0, scale=1.0):
        self.stack.append(self.xf)
        tx, ty, tz, yw, sc = self.xf
        p = yaw_rot(mul(pos, sc), yw)
        self.xf = (tx + p[0], ty + p[1], tz + p[2], yw + yaw, sc * scale)

    def Pop(self):
        self.xf = self.stack.pop()

    def W(self, p):
        tx, ty, tz, yw, sc = self.xf
        q = yaw_rot(mul(p, sc), yw)
        return (q[0] + tx, q[1] + ty, q[2] + tz)

    # ------------------------------------------------------------ triangles
    def Tri(self, a, b, c, col, inside):
        wa, wb, wc, wi = self.W(a), self.W(b), self.W(c), self.W(inside)
        n = cross(sub(wb, wa), sub(wc, wa))
        if dot(n, n) < 1e-14:
            return
        cen = mul(add(add(wa, wb), wc), 1 / 3)
        if dot(n, sub(cen, wi)) < 0:
            wb, wc = wc, wb
            n = mul(n, -1)
        self.tris.append((wa, wb, wc, norm(n), col))

    def Tri2(self, a, b, c, front, back):
        wa, wb, wc = self.W(a), self.W(b), self.W(c)
        n = cross(sub(wb, wa), sub(wc, wa))
        if dot(n, n) < 1e-14:
            return
        n = norm(n)
        if n[1] < 0:
            wb, wc = wc, wb
            n = mul(n, -1)
        self.tris.append((wa, wb, wc, n, front))
        self.tris.append((wa, wc, wb, mul(n, -1), back))

    def Quad(self, a, b, c, d, col, inside):
        self.Tri(a, b, c, col, inside)
        self.Tri(a, c, d, col, inside)

    # ------------------------------------------------------------ primitives
    @staticmethod
    def Basis(axis):
        axis = norm(axis)
        u = norm(cross(axis, RIGHT if abs(axis[1]) > 0.9 else UP))
        v = norm(cross(axis, u))
        return u, v

    def Tube(self, a, b, ra, rb, sides, side, capB=-1, capA=-1, twistDeg=0.0):
        u, v = self.Basis(sub(b, a))
        mid = mul(add(a, b), 0.5)
        ringA, ringB = [], []
        for i in range(sides):
            ang = (i + 0.5) / sides * math.pi * 2 + math.radians(twistDeg)
            d = add(mul(u, math.cos(ang)), mul(v, math.sin(ang)))
            ringA.append(add(a, mul(d, ra)))
            ringB.append(add(b, mul(d, rb)))
        for i in range(sides):
            j = (i + 1) % sides
            if ra > 1e-4 and rb > 1e-4:
                self.Quad(ringA[i], ringA[j], ringB[j], ringB[i], side, mid)
            elif ra > 1e-4:
                self.Tri(ringA[i], ringA[j], b, side, mid)
            else:
                self.Tri(a, ringB[j], ringB[i], side, mid)
        if capB >= 0 and rb > 1e-4:
            for i in range(1, sides - 1):
                self.Tri(ringB[0], ringB[i], ringB[i + 1], capB, mid)
        if capA >= 0 and ra > 1e-4:
            for i in range(1, sides - 1):
                self.Tri(ringA[0], ringA[i], ringA[i + 1], capA, mid)

    def Prism(self, baseCenter, rBottom, rTop, height, sides, side, top, bottom=-1, twistDeg=0.0):
        self.Tube(baseCenter, add(baseCenter, (0, height, 0)), rBottom, rTop, sides, side, top, bottom, twistDeg)

    def Stem(self, pts, r0, r1, sides, col, capCol=-1):
        n = len(pts)
        for i in range(n - 1):
            ta, tb = i / (n - 1), (i + 1) / (n - 1)
            self.Tube(pts[i], pts[i + 1], r0 + (r1 - r0) * ta, r0 + (r1 - r0) * tb, sides, col,
                      capCol if i == n - 2 else -1, capCol if i == 0 else -1)

    def Ball(self, c, r, lon, lat, col, top=-1, topFrom=0.55, bottom=-1, bottomTo=-0.6, yawDeg=0.0):
        def P(i, j):
            th, ph = math.pi * j / lat, math.pi * 2 * i / lon + math.radians(yawDeg)
            return (c[0] + math.sin(th) * math.cos(ph) * r[0], c[1] + math.cos(th) * r[1], c[2] + math.sin(th) * math.sin(ph) * r[2])
        for j in range(lat):
            for i in range(lon):
                a, b, d, e = P(i, j), P(i + 1, j), P(i, j + 1), P(i + 1, j + 1)
                ny = math.cos(math.pi * (j + 0.5) / lat)
                k = top if top >= 0 and ny > topFrom else bottom if bottom >= 0 and ny < bottomTo else col
                if j == 0:
                    self.Tri(a, d, e, k, c)
                elif j == lat - 1:
                    self.Tri(a, b, d, k, c)
                else:
                    self.Quad(a, b, e, d, k, c)

    def Dome(self, baseCenter, r, lon, lat, col, top=-1, topFrom=0.6, under=-1, yawDeg=0.0):
        inside = add(baseCenter, (0, r[1] * 0.3, 0))
        def P(i, j):
            th, ph = math.pi * 0.5 * j / lat, math.pi * 2 * i / lon + math.radians(yawDeg)
            return (baseCenter[0] + math.sin(th) * math.cos(ph) * r[0], baseCenter[1] + math.cos(th) * r[1],
                    baseCenter[2] + math.sin(th) * math.sin(ph) * r[2])
        for j in range(lat):
            for i in range(lon):
                a, b, d, e = P(i, j), P(i + 1, j), P(i, j + 1), P(i + 1, j + 1)
                ny = math.cos(math.pi * 0.5 * (j + 0.5) / lat)
                k = top if top >= 0 and ny > topFrom else col
                if j == 0:
                    self.Tri(a, d, e, k, inside)
                else:
                    self.Quad(a, b, e, d, k, inside)
        if under >= 0:
            for i in range(lon):
                self.Tri(baseCenter, P(i, lat), P(i + 1, lat), under, add(inside, (0, r[1], 0)))

    def Block(self, baseCenter, size, bevel, side, top, bevelCol=-1):
        if bevelCol < 0:
            bevelCol = top
        hx, hz = size[0] * 0.5, size[2] * 0.5
        b = min(bevel, min(hx, hz) * 0.9)
        ring = [(-hx + b, -hz), (hx - b, -hz), (hx, -hz + b), (hx, hz - b), (hx - b, hz), (-hx + b, hz), (-hx, hz - b), (-hx, -hz + b)]
        y0, y1, y2 = baseCenter[1], baseCenter[1] + size[1] - b, baseCenter[1] + size[1]
        mid = add(baseCenter, (0, size[1] * 0.5, 0))
        def Pt(q, y, s): return (baseCenter[0] + q[0] * s, y, baseCenter[2] + q[1] * s)
        inset = 1 - b / max(hx, hz)
        for i in range(len(ring)):
            j = (i + 1) % len(ring)
            self.Quad(Pt(ring[i], y0, 1), Pt(ring[j], y0, 1), Pt(ring[j], y1, 1), Pt(ring[i], y1, 1), side, mid)
            self.Quad(Pt(ring[i], y1, 1), Pt(ring[j], y1, 1), Pt(ring[j], y2, inset), Pt(ring[i], y2, inset), bevelCol, mid)
        for i in range(1, len(ring) - 1):
            self.Tri(Pt(ring[0], y2, inset), Pt(ring[i], y2, inset), Pt(ring[i + 1], y2, inset), top, mid)

    def Spike(self, basePos, direction, radius, length, col, sides=4):
        self.Tube(basePos, add(basePos, mul(norm(direction), length)), radius, 0.0, sides, col)

    def Leaf(self, basePos, direction, length, width, col, back=-1, fold=0.25, droop=0.2):
        if back < 0:
            back = col
        d = norm(direction)
        side = cross(UP, d)
        if dot(side, side) < 1e-4:
            side = RIGHT
        side = norm(side)
        up = norm(cross(d, side))
        if up[1] < 0:
            up = mul(up, -1)
        tip = sub(add(basePos, mul(d, length)), mul(up, length * droop))
        mid = add(add(basePos, mul(d, length * 0.45)), mul(up, length * 0.05))
        l = sub(sub(mid, mul(side, width * 0.5)), mul(up, width * fold))
        r = sub(add(mid, mul(side, width * 0.5)), mul(up, width * fold))
        rib = add(mid, mul(up, width * 0.12))
        self.Tri2(basePos, l, rib, col, back)
        self.Tri2(basePos, rib, r, col, back)
        self.Tri2(rib, l, tip, col, back)
        self.Tri2(rib, tip, r, col, back)

    def Disc(self, center, r, sides, col, thick=0.01, yawDeg=0.0):
        self.Prism(center, r, r, thick, sides, col, col, -1, yawDeg)

    @property
    def Triangles(self):
        return len(self.tris)


# ------------------------------------------------------------------ rendering
def hexrgb(h):
    h = h.lstrip('#')
    return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16))

def camera(pitch=35.0, yaw=45.0):
    """Orthographic camera for Unity Quaternion.Euler(pitch, yaw, 0): forward, screen right, screen up, key light.
    The default is the kit's Preview Models angle."""
    p, y = math.radians(pitch), math.radians(yaw)
    fwd = (math.sin(y) * math.cos(p), -math.sin(p), math.cos(y) * math.cos(p))
    right = (math.cos(y), 0.0, -math.sin(y))
    up = norm(cross(fwd, right))
    up = up if up[1] > 0 else mul(up, -1)
    key = norm(add(add(mul(right, -0.55), mul(up, 0.75)), mul(fwd, -0.45)))     # from the upper left, toward the camera
    return fwd, right, up, key

FWD, SCR_R, SCR_U, KEY = camera()

def ground_block(lp, top_col, side_col, side_dark, size=1.0, depth=0.22):
    """A game-style ground block under the model (not part of the model)."""
    h = size / 2
    corners = [(-h, 0, -h), (h, 0, -h), (h, 0, h), (-h, 0, h)]
    low = [(x, -depth, z) for x, _, z in corners]
    c = (0, -depth / 2, 0)
    lp.Quad(corners[0], corners[1], corners[2], corners[3], top_col, c)
    for i in range(4):
        j = (i + 1) % 4
        col = side_col if i in (0, 3) else side_dark
        lp.Quad(corners[i], corners[j], low[j], low[i], col, c)

def render(lp, palette, path, size=(170, 190), ppu=100.0, origin=(85, 140), ss=3, under=None, shadow=True, cam=None):
    """Flat-shaded, z-buffered, supersampled. palette: 64 hex strings. under: extra Lp drawn first (ground, Goop).
    The screen origin is the tile centre on the ground."""
    FWD, SCR_R, SCR_U, KEY = cam or camera()
    W, H = size[0] * ss, size[1] * ss
    zbuf = [[1e9] * W for _ in range(H)]
    cbuf = [[None] * W for _ in range(H)]
    pal = [hexrgb(h) for h in palette]
    def proj(p):
        return ((origin[0] + dot(p, SCR_R) * ppu) * ss, (origin[1] - dot(p, SCR_U) * ppu) * ss, dot(p, FWD))
    tris = (under.tris if under else []) + lp.tris
    n_under = len(under.tris) if under else 0
    # contact shadow: darken ground pixels under the model's footprint
    foot = None
    if shadow and lp.tris:
        xs = [v[0] for t in lp.tris for v in t[:3] if v[1] < 0.15]
        zs = [v[2] for t in lp.tris for v in t[:3] if v[1] < 0.15]
        if xs:
            foot = ((min(xs) + max(xs)) / 2, (min(zs) + max(zs)) / 2, max(0.05, (max(xs) - min(xs)) / 2 + 0.04), max(0.05, (max(zs) - min(zs)) / 2 + 0.04))
    for idx, (a, b, c, n, col) in enumerate(tris):
        if dot(n, FWD) >= 0:
            continue
        lit = 0.62 + 0.48 * max(0.0, dot(n, KEY)) + 0.06 * n[1]
        base = pal[col]
        rgb = tuple(min(255, int(ch * lit)) for ch in base)
        (ax, ay, az), (bx, by, bz), (cx, cy, cz) = proj(a), proj(b), proj(c)
        area = (bx - ax) * (cy - ay) - (by - ay) * (cx - ax)
        if abs(area) < 1e-9:
            continue
        x0, x1 = max(0, int(min(ax, bx, cx))), min(W - 1, int(max(ax, bx, cx)) + 1)
        y0, y1 = max(0, int(min(ay, by, cy))), min(H - 1, int(max(ay, by, cy)) + 1)
        is_ground = idx < n_under
        for y in range(y0, y1 + 1):
            py = y + 0.5
            for x in range(x0, x1 + 1):
                px = x + 0.5
                w0 = ((bx - px) * (cy - py) - (by - py) * (cx - px)) / area
                w1 = ((cx - px) * (ay - py) - (cy - py) * (ax - px)) / area
                w2 = 1 - w0 - w1
                if w0 < -1e-6 or w1 < -1e-6 or w2 < -1e-6:
                    continue
                z = w0 * az + w1 * bz + w2 * cz - (0.0 if not is_ground else -0.002)
                if z < zbuf[y][x]:
                    zbuf[y][x] = z
                    if is_ground and foot and n[1] > 0.9:
                        # world point on the ground plane from the barycentric mix
                        wx = w0 * a[0] + w1 * b[0] + w2 * c[0]
                        wz = w0 * a[2] + w1 * b[2] + w2 * c[2]
                        wy = w0 * a[1] + w1 * b[1] + w2 * c[1]
                        d = ((wx - foot[0]) / foot[2]) ** 2 + ((wz - foot[1]) / foot[3]) ** 2
                        if d < 1.0 and wy > -0.01:
                            k = 0.80 + 0.20 * d
                            cbuf[y][x] = tuple(int(ch * k) for ch in rgb)
                            continue
                    cbuf[y][x] = rgb
    out = []
    for y in range(size[1]):
        row = []
        for x in range(size[0]):
            r = g = b = cnt = 0
            for sy in range(ss):
                for sx in range(ss):
                    c = cbuf[y * ss + sy][x * ss + sx]
                    if c:
                        r += c[0]; g += c[1]; b += c[2]; cnt += 1
            if cnt:
                row.append((r // cnt, g // cnt, b // cnt, int(255 * cnt / (ss * ss))))
            else:
                row.append((0, 0, 0, 0))
        out.append(row)
    if path:
        write_png(path, out)
    return out
