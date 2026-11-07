#!/usr/bin/env python3
"""Turn a generated picture into a clean TDS sprite, and check it (docs/art/README.md). Needs only Pillow, so it runs
in ChatGPT's own Python as well as here.

Image models paint "pixel art" off the grid: blocks of uneven size, soft edges, a hundred near-colours, a background.
This script makes it real pixel art at game size:

  1. removes the background (the colour of the four corners, everywhere it appears, or existing transparency): ask
     for a flat background the sprite does not use, magenta FF00FF for a friend, green 00FF00 for a foe;
  2. crops to the drawing and scales it to the target height by majority vote per pixel (never by averaging, which
     makes new colours), keeping its shape;
  3. places it in the cell: feet on the bottom row and centred (--anchor bottom, sprites) or centred (--anchor
     center, icons);
  4. snaps every colour to the palette and makes every pixel fully opaque or fully clear;
  5. checks the house rules (kit docs/18 section 3, docs/STYLE.md): size, opaque, no pure black, on the palette, feet
     on the bottom row, centred, no magenta outside foes.

    python3 pixelize.py in.png out.png --cell 32 --height 26 --palette palettes.txt:yossele
    python3 pixelize.py icon.png Direction_Barnaclecoat.png --cell 12 --anchor center --palette palettes.txt:pack_icons
    python3 pixelize.py sheet.png Foo-Sheet.png --cell 32 --grid 6x4 --height 24 --palette tds_foes.gpl --foe
    python3 pixelize.py --check Foo-Sheet.png --cell 32 --palette palettes.txt:foes --foe

--grid CxR cuts the input into C x R equal frames first (a generated strip or sheet) and treats each as one frame.
--preview N also writes out_xN.png, scaled up for looking at. Prints one line per problem; exits 1 if any.
"""
import argparse
import colorsys
import os
import sys
from collections import Counter

from PIL import Image


# ------------------------------------------------------------------ palettes

def load_palette(spec):
    """'a.gpl', 'palettes.txt:name', or 'RRGGBB,RRGGBB,...' -> list of (r, g, b)."""
    if not spec:
        return None
    path, _, section = spec.partition(':')
    if os.path.exists(path):
        lines = open(path, encoding='utf-8').read().splitlines()
        if path.endswith('.gpl'):
            out = []
            for ln in lines:
                parts = ln.split()
                if len(parts) >= 3 and all(p.isdigit() for p in parts[:3]):
                    out.append(tuple(int(p) for p in parts[:3]))
            return out
        out, on = [], not section
        for ln in lines:
            s = ln.strip()
            if s.startswith('[') and s.endswith(']'):
                on = (s[1:-1].lower() == section.lower()) if section else True
                continue
            if on and s and not s.startswith('#'):
                h = s.split()[0].lstrip('#')
                if len(h) == 6:
                    out.append(tuple(int(h[i:i + 2], 16) for i in (0, 2, 4)))
        if not out:
            sys.exit('no colours found for ' + spec)
        return out
    return [tuple(int(h.strip().lstrip('#')[i:i + 2], 16) for i in (0, 2, 4)) for h in spec.split(',') if h.strip()]


def nearest(c, pal):
    # weighted RGB distance (cheap and close to what the eye sees on small sprites)
    r, g, b = c
    return min(pal, key=lambda p: 2 * (p[0] - r) ** 2 + 4 * (p[1] - g) ** 2 + 3 * (p[2] - b) ** 2)


# ------------------------------------------------------------------ cleaning

def remove_background(im, tol, inner=True):
    im = im.convert('RGBA')
    px = im.load()
    w, h = im.size
    alphas = [px[x, y][3] for x, y in ((0, 0), (w - 1, 0), (0, h - 1), (w - 1, h - 1))]
    if min(alphas) < 128:
        return im  # already transparent around the drawing
    corners = Counter(px[x, y][:3] for x, y in ((0, 0), (w - 1, 0), (0, h - 1), (w - 1, h - 1)))
    bg = corners.most_common(1)[0][0]
    # flood fill from the edges so a background colour inside the drawing survives
    seen, stack = set(), [(x, y) for x in range(w) for y in (0, h - 1)] + [(x, y) for y in range(h) for x in (0, w - 1)]
    close = lambda c: sum((a - b) ** 2 for a, b in zip(c[:3], bg)) <= tol * tol
    while stack:
        x, y = stack.pop()
        if (x, y) in seen or not (0 <= x < w and 0 <= y < h):
            continue
        seen.add((x, y))
        if close(px[x, y]):
            px[x, y] = (0, 0, 0, 0)
            stack += [(x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)]
    if inner:  # holes the fill can't reach (Yossele's see-through head) are background too
        for y in range(h):
            for x in range(w):
                if px[x, y][3] and close(px[x, y]):
                    px[x, y] = (0, 0, 0, 0)
    return im


def bbox(im):
    return im.getchannel('A').point(lambda a: 255 if a >= 128 else 0).getbbox()


def vote_resize(im, tw, th):
    """Each target pixel takes the most common opaque colour of its source block (clear if mostly clear)."""
    w, h = im.size
    src = im.load()
    out = Image.new('RGBA', (tw, th), (0, 0, 0, 0))
    dst = out.load()
    for ty in range(th):
        y0, y1 = ty * h // th, max(ty * h // th + 1, (ty + 1) * h // th)
        for tx in range(tw):
            x0, x1 = tx * w // tw, max(tx * w // tw + 1, (tx + 1) * w // tw)
            votes, clear = Counter(), 0
            for y in range(y0, y1):
                for x in range(x0, x1):
                    r, g, b, a = src[x, y]
                    if a < 128:
                        clear += 1
                    else:
                        votes[(r >> 2 << 2, g >> 2 << 2, b >> 2 << 2)] += 1
            if votes and sum(votes.values()) >= clear:
                dst[tx, ty] = votes.most_common(1)[0][0] + (255,)
    return out


def clean_frame(im, cell_w, cell_h, height, anchor, pal, tol, inner=True):
    im = remove_background(im, tol, inner)
    box = bbox(im)
    out = Image.new('RGBA', (cell_w, cell_h), (0, 0, 0, 0))
    if not box:
        return out
    im = im.crop(box)
    w, h = im.size
    th = min(height or cell_h, cell_h)
    tw = max(1, round(w * th / h))
    if tw > cell_w:
        tw, th = cell_w, max(1, round(h * cell_w / w))
    small = vote_resize(im, tw, th)
    if pal:
        px = small.load()
        for y in range(th):
            for x in range(tw):
                if px[x, y][3]:
                    px[x, y] = nearest(px[x, y][:3], pal) + (255,)
    x = (cell_w - tw) // 2
    y = cell_h - th if anchor == 'bottom' else (cell_h - th) // 2
    out.paste(small, (x, y))
    return out


# ------------------------------------------------------------------ checks

def check(im, cell_w, cell_h, pal, anchor, foe):
    w, h = im.size
    problems = []
    if w % cell_w or h % cell_h:
        problems.append('size %dx%d is not a whole number of %dx%d cells' % (w, h, cell_w, cell_h))
    px = im.convert('RGBA').load()
    soft, black, off, magenta = 0, 0, Counter(), 0
    pal_set = set(pal or [])
    for y in range(h):
        for x in range(w):
            r, g, b, a = px[x, y]
            if 0 < a < 255:
                soft += 1
            if not a:
                continue
            if (r, g, b) == (0, 0, 0):
                black += 1
            if pal and (r, g, b) not in pal_set:
                off['%02X%02X%02X' % (r, g, b)] += 1
            hh, s, v = colorsys.rgb_to_hsv(r / 255, g / 255, b / 255)
            if not foe and s > 0.2 and v > 0.2 and 270 <= hh * 360 <= 320:
                magenta += 1
    if soft:
        problems.append('%d half-transparent pixels (the house style is opaque)' % soft)
    if black:
        problems.append('%d pure black pixels (use a near-black such as 2B2922 or 333232)' % black)
    if off:
        problems.append('%d colours off the palette: %s' % (len(off), ' '.join(c for c, _ in off.most_common(8))))
    if magenta:
        problems.append('%d pixels in the foes\' magenta band (h270-320): magenta means enemy' % magenta)
    for cy in range(h // cell_h):
        for cx in range(w // cell_w):
            fr = im.crop((cx * cell_w, cy * cell_h, (cx + 1) * cell_w, (cy + 1) * cell_h))
            box = bbox(fr)
            if not box:
                continue
            where = 'frame row %d col %d' % (cy, cx)
            if anchor == 'bottom' and box[3] != cell_h:
                problems.append('%s: feet %d px above the bottom row' % (where, cell_h - box[3]))
            mid = (box[0] + box[2]) / 2
            if abs(mid - cell_w / 2) > 1.5:
                problems.append('%s: centred on column %.1f, not %d' % (where, mid, cell_w // 2))
    return problems


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument('inp')
    ap.add_argument('out', nargs='?')
    ap.add_argument('--cell', required=True, help='N or WxH: the game cell (32 vessel or foe, 16 unit, 12 icon)')
    ap.add_argument('--grid', help='CxR: cut the input into C columns x R rows of frames first')
    ap.add_argument('--height', type=int, help='the drawing\'s height in game pixels (a vessel 19-28 in a 32 cell)')
    ap.add_argument('--anchor', choices=['bottom', 'center'], default='bottom')
    ap.add_argument('--palette', help='a .gpl, palettes.txt:<section>, or RRGGBB,RRGGBB,...')
    ap.add_argument('--tol', type=int, default=40, help='background colour tolerance (0-441)')
    ap.add_argument('--keep-inner', action='store_true', help='keep background-coloured pixels the edge fill does not reach')
    ap.add_argument('--foe', action='store_true', help='a crystal foe: magenta allowed')
    ap.add_argument('--check', action='store_true', help='only check the input (already game size)')
    ap.add_argument('--preview', type=int, default=0, help='also write an N-times preview')
    a = ap.parse_args()
    cw, _, ch = a.cell.partition('x')
    cw, ch = int(cw), int(ch or cw)
    pal = load_palette(a.palette)
    if a.check:
        im = Image.open(a.inp).convert('RGBA')
    else:
        if not a.out:
            sys.exit('give an output path, or --check')
        src = Image.open(a.inp).convert('RGBA')
        cols, rows = (int(v) for v in (a.grid or '1x1').split('x'))
        fw, fh = src.width // cols, src.height // rows
        im = Image.new('RGBA', (cols * cw, rows * ch), (0, 0, 0, 0))
        for r in range(rows):
            for c in range(cols):
                fr = src.crop((c * fw, r * fh, (c + 1) * fw, (r + 1) * fh))
                im.paste(clean_frame(fr, cw, ch, a.height, a.anchor, pal, a.tol, not a.keep_inner), (c * cw, r * ch))
        im.save(a.out)
        if a.preview:
            im.resize((im.width * a.preview, im.height * a.preview), Image.NEAREST).save(
                os.path.splitext(a.out)[0] + '_x%d.png' % a.preview)
    problems = check(im, cw, ch, pal, a.anchor, a.foe)
    name = a.inp if a.check else a.out
    for p in problems:
        print('%s: %s' % (name, p))
    print('%s: %dx%d, %d problem(s)' % (name, im.width, im.height, len(problems)))
    sys.exit(1 if problems else 0)


if __name__ == '__main__':
    main()
