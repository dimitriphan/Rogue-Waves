"""The art review's automatic half (docs/STYLE.md "Art review"): every PNG named, or every PNG in a folder, checked for

  - size: a whole number of cells (--cell N, or --cell WxH for sheets), and the size it should be (--size WxH);
  - stray semi-transparent pixels (the house rule is opaque, kit docs/03 section 1), and pure black;
  - colours off the palette (--palette module.NAME: a dict of char -> (r, g, b, a) such as b11_icons.PAL);
  - frames that don't line up (--cell with --baseline): in each row of cells, the lowest opaque pixel (the feet) and the
    leftmost and rightmost columns; a frame whose feet sit off the row's most common line is listed (a walk that bobs on
    purpose shows here too, so read the list against the animation).

    python3 concept-tools/temp_art/art_check.py --cell 12 --palette b11_icons.PAL TDS_Mod/ModSource/RogueWavesMode/Art/Temporary/game_size/Direction_*.png
    python3 concept-tools/temp_art/art_check.py --cell 32x32 --baseline TDS_Mod/ModSource/Spool/Art/Temporary/game_size/

Prints one line per problem and a total; exits 1 when anything is wrong (a sparse icon is only a note).
"""
import argparse
import glob
import importlib
import os
import sys

from PIL import Image


def cells_of(spec):
    if not spec:
        return None
    w, _, h = spec.partition('x')
    return int(w), int(h or w)


def palette(spec):
    if not spec:
        return None
    mod, _, name = spec.rpartition('.')
    sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
    pal = getattr(importlib.import_module(mod), name)
    return {tuple(v[:3]) for v in pal.values()}


def check(path, cell, size, pal, baseline):
    out = []
    im = Image.open(path).convert('RGBA')
    w, h = im.size
    px = im.load()
    if size and (w, h) != size:
        out.append('size %dx%d, wanted %dx%d' % (w, h, size[0], size[1]))
    if cell and (w % cell[0] or h % cell[1]):
        out.append('size %dx%d is not whole %dx%d cells' % (w, h, cell[0], cell[1]))
    semi = black = 0
    off = set()
    filled = 0
    for y in range(h):
        for x in range(w):
            r, g, b, a = px[x, y]
            if 0 < a < 255:
                semi += 1
            if a == 255:
                filled += 1
                if (r, g, b) == (0, 0, 0):
                    black += 1
                if pal is not None and (r, g, b) not in pal:
                    off.add('%02X%02X%02X' % (r, g, b))
    if semi:
        out.append('%d semi-transparent pixels' % semi)
    if black:
        out.append('%d pure black pixels' % black)
    if off:
        out.append('%d colours off the palette: %s' % (len(off), ' '.join(sorted(off)[:8])))
    notes = []
    if cell and (w, h) == cell and filled < cell[0] * cell[1] // 5:
        notes.append('sparse: %d of %d pixels drawn' % (filled, w * h))
    if baseline and cell and not (w % cell[0] or h % cell[1]):
        for row in range(h // cell[1]):
            feet = []
            for col in range(w // cell[0]):
                low = -1
                for y in range(cell[1]):
                    for x in range(cell[0]):
                        if px[col * cell[0] + x, row * cell[1] + y][3] == 255:
                            low = y
                feet.append(low)
            drawn = [f for f in feet if f >= 0]
            if not drawn:
                continue
            common = max(set(drawn), key=drawn.count)
            odd = [(i, f) for i, f in enumerate(feet) if f >= 0 and f != common]
            if odd:
                out.append('row %d: feet on line %d, frames %s off it' % (row, common, ', '.join('%d (%+d)' % (i, f - common) for i, f in odd)))
    return out, notes


def main():
    ap = argparse.ArgumentParser(description=__doc__.split('\n')[0])
    ap.add_argument('paths', nargs='+')
    ap.add_argument('--cell')
    ap.add_argument('--size')
    ap.add_argument('--palette')
    ap.add_argument('--baseline', action='store_true')
    a = ap.parse_args()
    files = []
    for p in a.paths:
        files += sorted(glob.glob(os.path.join(p, '*.png'))) if os.path.isdir(p) else [p]
    cell, size, pal = cells_of(a.cell), cells_of(a.size), palette(a.palette)
    bad = 0
    for f in files:
        problems, notes = check(f, cell, size, pal, a.baseline)
        for p in problems:
            print('%s: %s' % (os.path.basename(f), p))
        for n in notes:
            print('%s: note, %s' % (os.path.basename(f), n))
        bad += bool(problems)
    print('%d files checked, %d with problems' % (len(files), bad))
    sys.exit(1 if bad else 0)


if __name__ == '__main__':
    main()
