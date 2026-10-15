"""Tiny pixel-sprite rasteriser: shapes -> parts with z-order, auto shading, inner + outer outlines, then detail pixels.

    python3 concept-tools/sprites.py grubb|wick|mummer [scale]   -> the sample sprite as SVG <rect>s on stdout (scale 7)
"""
import math, json, sys

OUT = "#2a2230"

class Sprite:
    def __init__(s, w=32, h=32):
        s.w, s.h = w, h
        s.part = [[None]*w for _ in range(h)]   # part name
        s.col = [[None]*w for _ in range(h)]
        s.parts = {}
        s.z = 0
        s.details = []

    def defpart(s, name, base, shade=None, hi=None, outline=OUT, soft=False, rim=True):
        s.z += 1
        s.parts[name] = dict(base=base, shade=shade, hi=hi, outline=outline, soft=soft, z=s.z, rim=rim)

    def put(s, x, y, name):
        if 0 <= x < s.w and 0 <= y < s.h:
            s.part[y][x] = name

    def ellipse(s, cx, cy, rx, ry, name, clip=None):
        for y in range(s.h):
            for x in range(s.w):
                if ((x + .5 - cx) / rx) ** 2 + ((y + .5 - cy) / ry) ** 2 <= 1 and (clip is None or clip(x, y)):
                    s.put(x, y, name)

    def rect(s, x0, y0, x1, y1, name):
        for y in range(y0, y1 + 1):
            for x in range(x0, x1 + 1):
                s.put(x, y, name)

    def poly(s, pts, name):
        for y in range(s.h):
            for x in range(s.w):
                px, py = x + .5, y + .5
                inside = False
                j = len(pts) - 1
                for i in range(len(pts)):
                    xi, yi = pts[i]; xj, yj = pts[j]
                    if (yi > py) != (yj > py) and px < (xj - xi) * (py - yi) / (yj - yi) + xi:
                        inside = not inside
                    j = i
                if inside:
                    s.put(x, y, name)

    def line(s, x0, y0, x1, y1, name):
        n = max(abs(x1 - x0), abs(y1 - y0))
        for i in range(n + 1):
            s.put(round(x0 + (x1 - x0) * i / n), round(y0 + (y1 - y0) * i / n), name)

    def dot(s, pts, color):
        s.details += [(x, y, color) for x, y in pts]

    def dline(s, x0, y0, x1, y1, color):
        n = max(abs(x1 - x0), abs(y1 - y0))
        for i in range(n + 1):
            s.details.append((round(x0 + (x1 - x0) * i / n), round(y0 + (y1 - y0) * i / n), color))

    def render(s):
        W, H, P = s.w, s.h, s.part
        get = lambda x, y: P[y][x] if 0 <= x < W and 0 <= y < H else None
        # base + shading
        for y in range(H):
            for x in range(W):
                n = P[y][x]
                if not n: continue
                d = s.parts[n]; c = d['base']
                if d['rim']:
                    if d['shade'] and (get(x + 1, y) != n or get(x, y + 1) != n):
                        c = d['shade']
                    elif d['hi'] and (get(x - 1, y) != n or get(x, y - 1) != n):
                        c = d['hi']
                s.col[y][x] = c
        # inner outlines: lower-z pixel touching a higher-z non-soft part becomes that part's outline
        inner = []
        for y in range(H):
            for x in range(W):
                n = P[y][x]
                if not n: continue
                for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                    m = get(x + dx, y + dy)
                    if m and m != n and s.parts[m]['z'] > s.parts[n]['z'] and not s.parts[m]['soft']:
                        inner.append((x, y, s.parts[m]['outline'])); break
        for x, y, c in inner: s.col[y][x] = c
        # outer outline
        outer = []
        for y in range(H):
            for x in range(W):
                if P[y][x]: continue
                best = None
                for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                    m = get(x + dx, y + dy)
                    if m and (best is None or s.parts[m]['z'] > s.parts[best]['z']):
                        best = m
                if best and s.parts[best]['outline']:
                    outer.append((x, y, s.parts[best]['outline']))
        for x, y, c in outer: s.col[y][x] = c
        for x, y, c in s.details:
            if 0 <= x < W and 0 <= y < H: s.col[y][x] = c
        return s.col

    def svg_rects(s, ox, oy, k):
        col = s.render() if not any(any(r) for r in s.col) else s.col
        out = []
        for y in range(s.h):
            x = 0
            while x < s.w:
                c = col[y][x]
                if c is None: x += 1; continue
                x2 = x
                while x2 + 1 < s.w and col[y][x2 + 1] == c: x2 += 1
                out.append(f'<rect x="{ox + x*k}" y="{oy + y*k}" width="{(x2 - x + 1)*k}" height="{k}" fill="{c}"/>')
                x = x2 + 1
        return "".join(out)

def ground(s, cx=16, cy=27.5):
    s.defpart('tile', '#eef3ef', shade='#d9e4de', hi='#ffffff', outline='#b8c9c1')
    s.poly([(cx - 14, cy), (cx, cy - 4), (cx + 14, cy), (cx, cy + 4)], 'tile')
    s.defpart('shadow', '#cdd9d3', soft=True, outline=None, rim=False)
    s.ellipse(cx, cy, 8.5, 2.2, 'shadow')

# ---------------- GRUBB ----------------
def grubb():
    s = Sprite(); ground(s)
    s.defpart('body', '#9b6b44', shade='#7a5233', hi='#b88458')
    s.ellipse(16, 16.5, 9.6, 10, 'body')
    s.defpart('face', '#e6c6a0', shade='#d6ae84', soft=True, rim=False)
    s.ellipse(16, 17, 6.4, 4.8, 'face')
    s.defpart('helmet', '#f5c533', shade='#d79f1d', hi='#ffe27a')
    s.ellipse(16, 9.5, 8.4, 6.6, 'helmet', clip=lambda x, y: y <= 8)
    s.defpart('brim', '#e0a91f', shade='#b9851a', rim=False)
    s.rect(6, 9, 25, 10, 'brim')
    s.defpart('lamp', '#fff6c8', shade='#f3dd86', hi='#ffffff')
    s.ellipse(16, 3.3, 2.5, 2.5, 'lamp')
    s.defpart('dirt', '#946236', shade='#714824', hi='#b07a46')
    s.poly([(4, 28), (6, 25), (10, 24), (13, 25.2), (16, 24.4), (19, 25.2), (22, 24), (26, 25), (28, 28), (16, 30)], 'dirt')
    s.defpart('paw', '#e7c49c', shade='#cfa679', rim=False)
    s.ellipse(8.5, 22.8, 3.4, 2.6, 'paw'); s.ellipse(23.5, 22.8, 3.4, 2.6, 'paw')
    o = OUT; c = '#fbf7ee'
    s.dot([(12, 15), (13, 14), (14, 15), (18, 15), (19, 14), (20, 15)], o)          # squint eyes
    s.dot([(15, 16), (16, 16), (17, 16), (15, 17), (16, 17), (17, 17)], '#f28fa5')     # nose
    s.dot([(15, 16)], '#ffd3dc')
    s.dot([(15, 18), (16, 19), (17, 18)], o)                                          # mouth
    s.dot([(10, 17), (11, 18), (22, 17), (21, 18)], '#8a6040')                         # whiskers
    s.dot([(5, 25), (7, 26), (9, 26), (11, 25), (27, 25), (25, 26), (23, 26), (21, 25)], c)  # claws
    s.dot([(5, 26), (7, 27), (9, 27), (27, 26), (25, 27), (23, 27)], o)
    s.dot([(15, 2), (16, 2)], '#ffffff')
    s.dot([(3, 22), (28, 21), (29, 23), (2, 25)], '#946236')
    return s

# ---------------- WICK ----------------
def wick():
    s = Sprite(); ground(s)
    s.defpart('puddle', '#fffaf0', shade='#efe3cc', soft=False)
    s.ellipse(15.5, 26.5, 8.5, 2, 'puddle')
    s.defpart('arm', '#f4ead6', shade='#dccbaa', hi='#fffaf0')
    s.rect(6, 18, 9, 19, 'arm'); s.rect(22, 16, 24, 17, 'arm')
    s.defpart('pole', '#8a5a33', shade='#6b4424', rim=True)
    s.line(25, 4, 25, 26, 'pole')
    s.defpart('wax', '#f4ead6', shade='#dccbaa', hi='#fffaf0')
    s.rect(9, 12, 21, 26, 'wax')
    s.defpart('waxtop', '#fffdf6', shade='#fffdf6', soft=True, rim=False)
    s.rect(10, 13, 20, 14, 'waxtop')
    for x, d in ((10, 3), (12, 1), (14, 4), (17, 2), (19, 5), (20, 1)):
        for y in range(15, 15 + d): s.put(x, y, 'waxtop')
    s.defpart('flame', '#f07a35', shade='#d95b22', hi='#ff9a52', outline='#b84516')
    s.ellipse(15.5, 7, 3.9, 3.6, 'flame')
    s.poly([(12, 6.5), (15.5, -0.5), (19, 6.5)], 'flame')
    s.defpart('core', '#ffd34d', shade='#ffd34d', soft=True, rim=False)
    s.ellipse(15.5, 8, 2.1, 2.3, 'core'); s.put(15, 5, 'core')
    s.defpart('lantern', '#ffd34d', shade='#f0b52a', hi='#fff1a8')
    s.rect(26, 6, 29, 10, 'lantern')
    s.defpart('cap', '#6b4424', shade='#55351b')
    s.rect(26, 5, 29, 5, 'cap'); s.rect(26, 11, 29, 11, 'cap')
    o = OUT
    s.dot([(15, 9), (16, 9)], '#fffbe6')
    s.dot([(15, 10), (15, 11)], o)                                                       # wick string
    s.dot([(12, 18), (13, 18), (12, 19), (13, 19), (12, 20), (13, 20), (18, 18), (19, 18), (18, 19), (19, 19), (18, 20), (19, 20)], o)
    s.dot([(12, 18), (18, 18)], '#ffffff')
    s.dot([(11, 22), (20, 22)], '#f5a58c'); s.dot([(10, 22), (21, 22)], '#f5a58c')
    s.dot([(14, 22), (15, 23), (16, 23), (17, 22)], o)
    s.dot([(26, 4), (27, 4), (28, 4)], '#6b4424')                                        # hook
    s.dot([(27, 8), (28, 8)], '#fffbe6')
    return s

# ---------------- MUMMER ----------------
def mummer():
    s = Sprite(); ground(s, cx=15)
    cx = 13
    s.defpart('cloak', '#8e3b6e', shade='#6e2a54', hi='#a84d84')
    s.poly([(cx, 10), (cx + 5, 15), (cx + 9.5, 27.5), (cx - 9.5, 27.5), (cx - 5, 15)], 'cloak')
    s.defpart('arm', '#8e3b6e', shade='#6e2a54', rim=False)
    s.poly([(16.5, 16.5), (21.5, 11.5), (23, 13), (18, 18.5)], 'arm')
    s.defpart('hood', '#64264c', shade='#4d1b3a', hi='#7c3361')
    s.ellipse(cx, 9.5, 7, 4.2, 'hood')
    s.poly([(cx - 6, 10), (cx - 9.5, 1.5), (cx - 2, 6.5)], 'hood')
    s.poly([(cx + 6, 10), (cx + 9.5, 1.5), (cx + 2, 6.5)], 'hood')
    s.defpart('bell', '#f5c533', shade='#d79f1d', hi='#ffe27a')
    s.ellipse(cx - 9.5, 1.6, 1.6, 1.6, 'bell'); s.ellipse(cx + 9.5 + 1, 1.6, 1.6, 1.6, 'bell')
    s.defpart('mask', '#f8f3ec', shade='#e3d9cc', hi='#ffffff')
    s.ellipse(cx + .5, 11.5, 4.6, 5, 'mask')
    s.defpart('hand', '#f8f3ec', shade='#e3d9cc', rim=False)
    s.ellipse(23, 11.5, 1.6, 1.6, 'hand'); s.ellipse(8.5, 21.5, 1.4, 1.4, 'hand')
    s.defpart('cross', '#a86c3c', rim=False)
    s.rect(19, 7, 28, 8, 'cross'); s.rect(23, 4, 24, 12, 'cross')
    s.defpart('puppet', '#c88752', shade='#9a6236', hi='#e0a46c')
    s.rect(23, 20, 28, 24, 'puppet')
    s.defpart('bow', '#7a4a26', rim=False)
    s.line(22, 20, 25, 17, 'bow'); s.line(26, 17, 29, 20, 'bow')
    s.defpart('leg', '#7a4a26', rim=False)
    s.rect(24, 25, 24, 26, 'leg'); s.rect(27, 25, 27, 26, 'leg')
    o = OUT; red = '#d2405f'; st = '#b4a7c0'
    s.dot([(11, 10), (10, 11), (11, 11), (12, 11), (11, 12), (15, 10), (14, 11), (15, 11), (16, 11), (15, 12)], o)  # diamond eyes
    s.dot([(10, 14), (11, 15), (12, 15), (13, 15), (14, 15), (15, 15), (16, 14)], o)    # smile
    s.dot([(9, 13), (17, 13)], red)
    for x, y in ((13, 19), (10, 23), (16, 23)):                                          # harlequin diamonds
        s.dot([(x, y), (x - 1, y + 1), (x + 1, y + 1), (x, y + 2), (x, y + 1)], '#b8578f')
    s.dline(19, 9, 23, 19, st); s.dline(28, 9, 28, 19, st); s.dline(24, 13, 25, 19, st)                                # strings
    s.dot([(24, 21), (25, 22), (24, 23), (26, 21), (26, 23)], red)                       # stitched X
    s.dot([(25, 18), (26, 18)], '#7a4a26')
    return s

if __name__ == "__main__":
    k = int(sys.argv[2]) if len(sys.argv) > 2 else 7
    s = {'grubb': grubb, 'wick': wick, 'mummer': mummer}[sys.argv[1]]()
    s.render()
    print(s.svg_rects(0, 0, k))
