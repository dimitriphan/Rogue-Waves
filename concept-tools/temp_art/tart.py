"""Temporary-art toolkit: pixel canvases with parts, light from the upper left, and the outputs every temporary asset
ships with (the game-size PNG, an .aseprite copy, an 8x preview on a checker).

House rules it follows (kit docs/03 section 1, docs/18 sections 3-4): opaque, no outline, light from the upper left
(the right and bottom of each form take its shade tone, the left and top stay the fill), shade keeps the hue, no pure
black, two or three tones per material. Used by the generators beside it; see Art/Temporary READMEs for what each makes.
"""
import math
import os
import struct
import sys
import zlib

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.abspath(os.path.join(HERE, '..', '..'))
sys.path.insert(0, os.path.join(REPO, 'TDS_Mod', 'Tools', 'aseprite'))
import ase_sheets  # noqa: E402  (png_write, write_ase)


def H(s):
    """'F2E8D0' or 'F2E8D080' -> (r, g, b, a)."""
    s = s.lstrip('#')
    a = int(s[6:8], 16) if len(s) == 8 else 255
    return (int(s[0:2], 16), int(s[2:4], 16), int(s[4:6], 16), a)


class Canvas:
    def __init__(self, w, h):
        self.w, self.h = w, h
        self.c = [(0, 0, 0, 0)] * (w * h)

    def get(self, x, y):
        return self.c[y * self.w + x] if 0 <= x < self.w and 0 <= y < self.h else (0, 0, 0, 0)

    def set(self, x, y, col):
        if 0 <= x < self.w and 0 <= y < self.h and col is not None:
            self.c[y * self.w + x] = col if isinstance(col, tuple) else H(col)

    def over(self, x, y, col):
        """Alpha-composite one pixel (only used for previews; art stays opaque)."""
        col = col if isinstance(col, tuple) else H(col)
        if col[3] == 255:
            self.set(x, y, col)
            return
        b = self.get(x, y)
        a = col[3] / 255.0
        self.set(x, y, tuple(int(round(col[i] * a + b[i] * (1 - a))) for i in range(3)) + (max(b[3], col[3]),))

    def stamp(self, rows, pal, x0=0, y0=0, flip=False):
        """String rows (top first); '.' or ' ' transparent; pal maps a char to a colour."""
        for y, row in enumerate(rows):
            for x, ch in enumerate(row):
                if ch in '. ':
                    continue
                xx = x0 + (len(row) - 1 - x if flip else x)
                self.set(xx, y0 + y, pal[ch])

    def draw(self, other, x0, y0):
        for y in range(other.h):
            for x in range(other.w):
                p = other.get(x, y)
                if p[3]:
                    self.set(x0 + x, y0 + y, p)

    def flip(self):
        o = Canvas(self.w, self.h)
        for y in range(self.h):
            for x in range(self.w):
                o.set(self.w - 1 - x, y, self.get(x, y))
        return o

    def rgba(self):
        return b''.join(bytes(p) for p in self.c)

    def scaled(self, k):
        o = Canvas(self.w * k, self.h * k)
        for y in range(o.h):
            for x in range(o.w):
                o.c[y * o.w + x] = self.c[(y // k) * self.w + x // k]
        return o

    def colours(self):
        return sorted({p for p in self.c if p[3]})


class Mask:
    """A set of pixels for one part, shaded as one form."""

    def __init__(self):
        self.px = set()

    def ellipse(self, cx, cy, rx, ry):
        for y in range(int(cy - ry - 1), int(cy + ry + 2)):
            for x in range(int(cx - rx - 1), int(cx + rx + 2)):
                if ((x + .5 - cx) / rx) ** 2 + ((y + .5 - cy) / ry) ** 2 <= 1.0:
                    self.px.add((x, y))
        return self

    def rect(self, x0, y0, x1, y1):
        for y in range(y0, y1 + 1):
            for x in range(x0, x1 + 1):
                self.px.add((x, y))
        return self

    def line(self, x0, y0, x1, y1, width=1):
        n = max(abs(x1 - x0), abs(y1 - y0), 1)
        for i in range(n + 1):
            x = round(x0 + (x1 - x0) * i / n)
            y = round(y0 + (y1 - y0) * i / n)
            for k in range(width):
                self.px.add((x + k, y))
        return self

    def points(self, pts):
        self.px |= set(pts)
        return self

    def minus(self, other):
        self.px -= other.px
        return self

    def flipped(self, w):
        m = Mask()
        m.px = {(w - 1 - x, y) for x, y in self.px}
        return m


def shade(canvas, mask, tones, light=(-0.62, -0.78), centre=None, radii=None, cut=(-0.30, -0.78), rim=True):
    """Paint a mask in up to three tones: light (fill), shade, deep. With a centre and radii the form is treated as a
    rounded body (a normal from the centre); every pixel whose right or lower neighbour is outside takes at least the
    shade tone (rim) so the right and bottom edges read darker, as on the game's sprites."""
    fill, sh, deep = (list(tones) + [None, None])[:3]
    if not mask.px:
        return
    xs = [x for x, _ in mask.px]
    ys = [y for _, y in mask.px]
    if centre is None:
        centre = ((min(xs) + max(xs) + 1) / 2.0, (min(ys) + max(ys) + 1) / 2.0)
    if radii is None:
        radii = (max(1.0, (max(xs) - min(xs) + 1) / 2.0), max(1.0, (max(ys) - min(ys) + 1) / 2.0))
    lx, ly = light
    for (x, y) in mask.px:
        nx = (x + .5 - centre[0]) / radii[0]
        ny = (y + .5 - centre[1]) / radii[1]
        lit = nx * lx + ny * ly
        col = fill
        if sh is not None and lit < cut[0]:
            col = sh
        if deep is not None and lit < cut[1]:
            col = deep
        if rim and sh is not None and col == fill and ((x + 1, y) not in mask.px or (x, y + 1) not in mask.px):
            col = sh
        canvas.set(x, y, col)


# ------------------------------------------------------------------ outputs

CHECK = ((150, 165, 150, 255), (138, 153, 138, 255))


def preview(canvas, cell, k=8, bg=CHECK):
    o = Canvas(canvas.w * k, canvas.h * k)
    for y in range(o.h):
        for x in range(o.w):
            o.c[y * o.w + x] = bg[((x // (cell * k)) + (y // (cell * k))) % 2]
    big = canvas.scaled(k)
    for i, p in enumerate(big.c):
        if p[3] == 255:
            o.c[i] = p
        elif p[3]:
            b = o.c[i]
            a = p[3] / 255.0
            o.c[i] = tuple(int(round(p[j] * a + b[j] * (1 - a))) for j in range(3)) + (255,)
    return o


def save_png(canvas, path):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    ase_sheets.png_write(path, canvas.w, canvas.h, canvas.rgba())


def save_ase(path, cell, frames, tags, layer='TEMP art'):
    """frames: list of Canvas (cell x cell); tags: list of (name, first, last, text). The layer name marks it temporary."""
    os.makedirs(os.path.dirname(path), exist_ok=True)
    cols = set()
    for f in frames:
        cols |= set(f.colours())
    palette = [(0, 0, 0, 0)] + sorted(cols, key=lambda c: (c[3] < 255, sum(c[:3])))
    rgb = [(0x3F, 0x8F, 0x96), (0xE2, 0x72, 0x5B), (0xD8, 0xC4, 0x9A), (0x72, 0xCC, 0x35)]
    t = [(n, a, b, rgb[i % len(rgb)], text) for i, (n, a, b, text) in enumerate(tags)]
    ase_sheets.write_ase(path, cell, cell, [f.rgba() for f in frames], t, palette, layer)


def sheet(cells, cols, cell):
    rows = (len(cells) + cols - 1) // cols
    s = Canvas(cols * cell, rows * cell)
    for i, c in enumerate(cells):
        if c is not None:
            s.draw(c, (i % cols) * cell, (i // cols) * cell)
    return s


def write_sheet(base, name, cells, cols, cell, tags, game_dir='game_size', k=None):
    """Writes base/game_size/<name>.png, base/aseprite/<name>.aseprite, base/preview_8x/<name>_8x.png.
    tags: (tag, row, col, count) in cells; each becomes an Aseprite tag with user data naming its place."""
    s = sheet(cells, cols, cell)
    save_png(s, os.path.join(base, game_dir, name + '.png'))
    k = k or (8 if cell <= 32 else 4)
    save_png(preview(s, cell, k), os.path.join(base, 'preview_8x', name + '_8x.png'))
    frames, atags = [], []
    rows = (len(cells) + cols - 1) // cols
    for tag, row, col, count in tags:
        start = len(frames)
        for i in range(count):
            frames.append(cells[row * cols + col + i])
        atags.append((tag, start, len(frames) - 1,
                      'TEMP;sheet=%s.png;row=%d;col=%d;cols=%d;rows=%d' % (name, row, col, cols, rows)))
    save_ase(os.path.join(base, 'aseprite', name + '.aseprite'), cell, frames, atags)
    return s
