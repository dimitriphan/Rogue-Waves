"""Temporary art proposal (2026-11-05): Spool's main sprite in a pastel green scheme, as an example for Dimitri.

Dimitri, 2026-11-05: "Spool's current color scheme is too similar to enemy units. Swap Spool's color scheme to pastel
yellow, but only recolor the main sprite for now so I can see an example." Then, 03:00Z: "Change the color to pastel
green instead." The main sprite is his reshade, embedded
pixel for pixel as `Front` in `TDS_Mod/Assets/SpoolMod/Editor/SpoolCharacter.cs` with the palette `P` beside it. This
script reads both from that file, so the "before" is exactly what the game builds, and swaps only the purple family:
the plush (a, b) and the dark inside the mouth (m, n). Every pixel stays where it is; teeth, gloves, cotton, tongue and
eyes keep his colours. The new ramp follows kit docs/18 section 3: the shade keeps the hue (within 2 degrees) and
gains saturation and loses value; the mouth's dark is a warm near-black, not black.

Nothing here is wired: the game still draws Spool from `SpoolCharacter.P` (see the folder's README for the follow-up).

    python3 concept-tools/temp_art/spool_pastel.py
        -> TDS_Mod/ModSource/Spool/Art/Temporary/pastel_green_2026-11-05/
           game_size/Spool_Pastel.png, aseprite/Spool_Pastel.aseprite, preview_8x/Spool_Pastel_8x.png,
           preview_8x/Spool_Pastel_compare.png; prints the palette map and the colour distances for the README.
"""
import colorsys
import math
import os
import re

from PIL import Image, ImageDraw, ImageFont

from tart import Canvas, H, REPO, preview, save_ase, save_png

SRC = os.path.join(REPO, 'TDS_Mod', 'Assets', 'SpoolMod', 'Editor', 'SpoolCharacter.cs')
OUT = os.path.join(REPO, 'TDS_Mod', 'ModSource', 'Spool', 'Art', 'Temporary', 'pastel_green_2026-11-05')

# Old -> new, only the purple family. Mint h149, picked in the gap between the greens already taken: the Hardened
# elite's glow on foes (h120), the shared leaf green (h124), Yossele's leaf and eyes (h90-96) and Buster's collar
# (h173). Shade same hue, +15 saturation, -14 value; a deep green near-black for the mouth.
NEW = {
    'a': ('plush', '9363AE', 'A4EBC7'),
    'b': ('plush shade', '784B94', '6DC99A'),
    'm': ('inside the mouth', '41174F', '1F3830'),
    'n': ('mouth edge', '4A1D5A', '25473C'),
}

# What Spool must not look like (kit docs/18 section 5, reference/palettes/tds_foes.gpl): every Tide foe's crystal and
# stone. Elite skins are material tints over those (art_survey_2026-10-15.txt part 3), hue only.
FOES = ['F6BAFF', 'DA89E6', 'D168E0', 'A54FB1', '8D4497', '8C8C8C', '6D6C6C', '646464']
ELITES = [('Hardened', '2BFF2B'), ('Superheated', 'FFFFFF'), ('Tempered', 'FF000A'), ('Fractured', '1F1F1F'),
          ('Annealed', 'FFB990'), ('Molten', 'FF1400')]
FRIENDS = [('Hardened (foe)', '2BFF2B', '2BFF2B'), ('leaf green', '7BCF80', '65AD69'), ('Yossele leaf', '72CC35', '6ABE30'),
           ('Buster collar', '9FF3E9', '19DFCE')]


def read_source():
    src = open(SRC, encoding='utf-8').read()
    body = src.split('static readonly string[] Front')[1].split('};')[0]
    front = re.findall(r'"([.a-zA-Z]{32})"', body)
    pal = dict(re.findall(r"\{ '(\w)', Col\.H\(\"(\w{6})\"\) \}", src))
    assert len(front) == 32, 'Front not found in SpoolCharacter.cs'
    return front, pal


def render(front, pal):
    c = Canvas(32, 32)
    for y, row in enumerate(front):
        for x, ch in enumerate(row):
            if ch != '.':
                c.set(x, y, H(pal[ch]))
    return c


def to_image(c):
    return Image.frombytes('RGBA', (c.w, c.h), c.rgba())


def hsv(s):
    h, sat, v = colorsys.rgb_to_hsv(*[ch / 255.0 for ch in H(s)[:3]])
    return round(h * 360), round(sat * 100), round(v * 100)


def lab(s):
    def lin(u):
        u /= 255.0
        return u / 12.92 if u <= 0.04045 else ((u + 0.055) / 1.055) ** 2.4
    r, g, b = (lin(ch) for ch in H(s)[:3])
    x = (0.4124 * r + 0.3576 * g + 0.1805 * b) / 0.95047
    y = 0.2126 * r + 0.7152 * g + 0.0722 * b
    z = (0.0193 * r + 0.1192 * g + 0.9505 * b) / 1.08883
    f = [t ** (1 / 3.0) if t > 0.008856 else 7.787 * t + 16 / 116.0 for t in (x, y, z)]
    return 116 * f[1] - 16, 500 * (f[0] - f[1]), 200 * (f[1] - f[2])


def delta_e(a, b):
    return math.dist(lab(a), lab(b))


def hue_gap(a, b):
    d = abs(hsv(a)[0] - hsv(b)[0]) % 360
    return min(d, 360 - d)


def font(size, bold=False):
    name = 'DejaVuSans-Bold.ttf' if bold else 'DejaVuSans.ttf'
    try:
        return ImageFont.truetype('/usr/share/fonts/truetype/dejavu/' + name, size)
    except OSError:
        return ImageFont.load_default()


GRASS = (140, 155, 140)
INK = (34, 38, 34)


def compare(before, after, pal_old, pal_new):
    W, Hh = 1180, 820
    im = Image.new('RGB', (W, Hh), (236, 238, 232))
    d = ImageDraw.Draw(im)
    d.text((24, 16), 'Spool, main sprite: purple (now) and pastel green (proposal)', font=font(22, True), fill=INK)
    d.text((24, 46), 'Same pixels, only the plush and the mouth colours change. Not in the game yet.', font=font(15),
           fill=INK)

    # 8x side by side on the house checker.
    for i, (c, label) in enumerate([(before, 'now'), (after, 'pastel green')]):
        x0 = 24 + i * 290
        big = to_image(preview(c, 32, 8))
        im.paste(big, (x0, 80))
        d.text((x0, 344), label, font=font(16, True), fill=INK)

    # Game size and 3x on grass, among the foe palette (a crystal block per tone) and the friends.
    x0, y0 = 620, 80
    d.rectangle([x0, y0, W - 24, y0 + 264], fill=GRASS)
    d.text((x0 + 10, y0 + 6), 'game size and 3x, on grass, beside the foe palette', font=font(13), fill=INK)
    for i, c in enumerate([before, after]):
        one = to_image(c)
        three = one.resize((96, 96), Image.NEAREST)
        im.paste(one, (x0 + 30 + i * 60, y0 + 40), one)
        im.paste(three, (x0 + 180 + i * 110, y0 + 30), three)
    for i, hx in enumerate(FOES):
        bx = x0 + 30 + i * 62
        d.rectangle([bx, y0 + 150, bx + 52, y0 + 200], fill=H(hx)[:3])
        d.text((bx, y0 + 204), hx, font=font(11), fill=INK)
    d.text((x0 + 30, y0 + 226), 'crystal F6BAFF..8D4497 (h292) and stone greys: every Tide foe', font=font(12),
           fill=INK)

    # Palette map.
    y = 380
    d.text((24, y), 'Palette map (old -> new)', font=font(18, True), fill=INK)
    y += 32
    for k, (role, old, new) in NEW.items():
        d.rectangle([24, y, 64, y + 32], fill=H(old)[:3])
        d.rectangle([170, y, 210, y + 32], fill=H(new)[:3])
        d.text((72, y + 8), old, font=font(14), fill=INK)
        d.text((138, y + 6), '->', font=font(16), fill=INK)
        d.text((218, y + 8), '%s   %s   (hsv %d %d %d -> %d %d %d)' % (new, role, *hsv(old), *hsv(new)),
               font=font(14), fill=INK)
        y += 42
    d.text((24, y + 4), 'Unchanged: teeth E4DEDA/9A938E, gloves F6F3F8/D8D1E2, cotton FFFFFF, tongue E58CC0/C96CA6, '
           'eyes E6D85A.', font=font(13), fill=INK)

    # Distances.
    y += 40
    d.text((24, y), 'Nearest foe colour (CIE76 colour distance: bigger is easier to tell apart)',
           font=font(15, True), fill=INK)
    y += 28
    for k in ('a', 'b'):
        o, n = pal_old[k], pal_new[k]
        fo = min(FOES, key=lambda f: delta_e(o, f))
        fn = min(FOES, key=lambda f: delta_e(n, f))
        d.text((24, y), '%s  now %s: %.0f from %s, %d deg of hue from the crystal   ->   green %s: %.0f from %s, '
               '%d deg from the crystal' % (NEW[k][0], o, delta_e(o, fo), fo, hue_gap(o, 'D168E0'), n,
                                            delta_e(n, fn), fn, hue_gap(n, 'D168E0')), font=font(13), fill=INK)
        y += 24
    y += 10
    d.text((24, y), 'Greens already taken: the Hardened elite glow on foes (h120), leaf green (h124), Yossele (h90-96), '
           'Buster collar (h173). Spool sits between them at h149.', font=font(13), fill=INK)
    for i, (name, a, b) in enumerate(FRIENDS):
        bx = 24 + i * 130
        d.rectangle([bx, y + 26, bx + 40, y + 56], fill=H(a)[:3])
        d.rectangle([bx + 40, y + 26, bx + 80, y + 56], fill=H(b)[:3])
        d.text((bx, y + 60), name, font=font(12), fill=INK)
    bx = 24 + 4 * 130
    d.rectangle([bx, y + 26, bx + 40, y + 56], fill=H(pal_new['a'])[:3])
    d.rectangle([bx + 40, y + 26, bx + 80, y + 56], fill=H(pal_new['b'])[:3])
    d.text((bx, y + 60), 'Spool (new)', font=font(12), fill=INK)
    return im


def main():
    front, pal_old = read_source()
    for k, (_, old, _) in NEW.items():
        assert pal_old[k] == old, 'SpoolCharacter.P[%s] is %s, expected %s' % (k, pal_old[k], old)
    pal_new = dict(pal_old)
    pal_new.update({k: new for k, (_, _, new) in NEW.items()})
    before, after = render(front, pal_old), render(front, pal_new)

    save_png(after, os.path.join(OUT, 'game_size', 'Spool_Pastel.png'))
    save_png(preview(after, 32, 8), os.path.join(OUT, 'preview_8x', 'Spool_Pastel_8x.png'))
    save_ase(os.path.join(OUT, 'aseprite', 'Spool_Pastel.aseprite'), 32, [after],
             [('Idle', 0, 0, 'TEMP;proposal;Spool main sprite, pastel green, not wired')])
    compare(before, after, pal_old, pal_new).save(os.path.join(OUT, 'preview_8x', 'Spool_Pastel_compare.png'))

    print('| Key | Role | Old | New | Old h s v | New h s v |')
    for k, (role, old, new) in NEW.items():
        print('| `%s` | %s | `%s` | `%s` | %d %d %d | %d %d %d |' % (k, role, old, new, *hsv(old), *hsv(new)))
    print()
    for k in ('a', 'b', 'm', 'n'):
        for tag, c in (('old', pal_old[k]), ('new', pal_new[k])):
            near = min(FOES, key=lambda f: delta_e(c, f))
            print('%s %s %s: nearest foe %s dE %.1f, hue gap to crystal D168E0 %d' % (
                k, tag, c, near, delta_e(c, near), hue_gap(c, 'D168E0')))
    for name, hx in ELITES:
        print('elite %-11s %s hue %d: gap to new plush %d' % (name, hx, hsv(hx)[0], hue_gap(hx, pal_new['a'])))
    changed = sum(1 for row in front for ch in row if ch in NEW)
    print('pixels recoloured: %d of %d opaque' % (changed, sum(1 for row in front for ch in row if ch != '.')))


if __name__ == '__main__':
    main()
