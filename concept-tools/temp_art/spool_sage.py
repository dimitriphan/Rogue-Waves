"""Spool in sage green (2026-11-05): every purple and lilac colour of Spool's art moved to the sage family Dimitri picked.

Dimitri, 2026-11-05: Spool's purple reads too close to the foes; he tried yellow, then a bright mint, and picked a soft
sage for the main sprite (03:09Z, "This version works."; `ModSource/Spool/Art/Temporary/pastel_green_2026-11-05/`). Then,
03:19Z, on the follow-up: "Yes, plus UI and minions": Spool's own animations, the UI colour, the Thread Head, the four
Crafted and the lilac thread, all moved to the sage family.

One table, `MAP`, old colour -> new colour. The plush ramp is the picked sage (93CCA1 / 60A871, the mouth 1F3325 /
26422F); Spool's thread, which was a paler blue-lilac beside the purple plush, becomes a paler sage beside the sage
plush, with a darker end so the ground tuft still reads. Whites, the gloves' pale cool shade (D8D1E2, as on the picked sprite), the tongue and the
eyes stay; the felt's darker lavender-grey shade turns sage-grey.
The code takes the same values by hand (SpoolCharacter.P, the models' palette, the effects, the UI colour, the thread
colour, the icon palette); this script recolours the files: Spool's `Art/Temporary/` sheets (game_size, aseprite,
preview_8x PNGs and GIFs; the GIFs, opaque and under 20 colours, are re-quantised exactly). The version archives (`variants/`, `minions_2026-11-04/`) and the proposal folder keep their
colours as a record.

2026-11-06: the Seam's felt and the Mangled's cotton, drawn white with a lilac-grey mid and a grey-green shade, read as
Pearl, which docs/STYLE.md keeps for Pure Aura (docs/reports/BATCH_CHECKS_2026-11-06c.md section 5), so `SHEET_MAP`
moves those two bodies to the sage as well, per sheet, since the same whites are the other Crafted's gloves.

    python3 concept-tools/temp_art/spool_sage.py          -> recolours the files in place, prints what changed
    python3 concept-tools/temp_art/spool_sage.py --check  -> exits 1 if any old colour is left in those files

Re-running a Spool generator (spool_units.py, spool_threadhead.py, ui_icons.py: their palettes are sage already) is
safe; apply_picks.py copies purple versions in, so it runs this script after a Spool pick.
"""
import colorsys
import glob
import io
import os
import struct
import sys
import zlib

from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.abspath(os.path.join(HERE, '..', '..'))
ART = os.path.join(REPO, 'TDS_Mod', 'ModSource', 'Spool', 'Art', 'Temporary')

MAP = {
    # the plush: light, base, shade, deep (the picked sage and its ramp, hue 134-135)
    'A274BC': 'A6D6B1',
    '9363AE': '93CCA1',
    '784B94': '60A871',
    '613A7B': '3F7A4F',
    # the dark inside the mouth and its edge
    '41174F': '1F3325',
    '4A1D5A': '26422F',
    # the thread and the Dart's lilac: pale to dark
    'D5BEEC': 'CBE6D1',
    'B292D6': 'A2CFAD',
    'B58BC9': '9AC9A5',
    '9A7CC4': '84BD92',
    '945DAD': '6FAE7E',
    '7A4A8F': '4F8C5E',
    # the felt's lavender-grey shade (the Seam's button, the Mangled's cotton, the Crafted's deep glove shade)
    'B5ABC6': 'ABBFB1',
}

# Per sheet, after MAP: the Seam (Button) and the Mangled (Burst) bodies, light / mid / shade (felt F6F3F8 / D8D1E2 /
# ABBFB1). The Seam is the lighter of the two and its legs step down a tone to stay apart from its rim; the Mangled sits
# a tone darker, which keeps it apart from the Seam and lifts its yellow eye. A sheet's table applies only while its
# felt white (the first key) is in the file, because the Seam's legs would darken again on a second pass. In the
# crafted GIFs the four sheets stand side by side (apply_picks.refresh: stitched, dart, seam, mangled), so a table
# there covers its own strip.
SHEET_MAP = {
    'Spoolseam': {'F6F3F8': 'A6D6B1', 'D8D1E2': '93CCA1', 'ABBFB1': '60A871', '93CCA1': '60A871', '60A871': '3F7A4F'},
    'Spoolmangled': {'F6F3F8': '93CCA1', 'D8D1E2': '6FAE7E', 'ABBFB1': '4F8C5E'},
}
GIF_STRIP = {'Spoolseam': 2, 'Spoolmangled': 3}
CRAFTED_GIFS = ('Spool_crafted_move.gif', 'Spool_crafted_attack.gif')


def hx(c):
    return '%02X%02X%02X' % tuple(c[:3])


def rgb(s):
    return tuple(int(s[i:i + 2], 16) for i in (0, 2, 4))


def hsv(s):
    h, sat, v = colorsys.rgb_to_hsv(*[ch / 255.0 for ch in rgb(s)])
    return round(h * 360), round(sat * 100), round(v * 100)


MAP_RGB = {rgb(k): rgb(v) for k, v in MAP.items()}


def sheet_of(path):
    return os.path.basename(path).split('-')[0]


def table_for(name, present):
    """The lookup for a file (or a GIF strip) of sheet `name`: MAP, then the sheet's own table while its marker colour
    is `present`, composed into one pass."""
    if name not in SHEET_MAP:
        return MAP_RGB
    own = {rgb(k): rgb(v) for k, v in SHEET_MAP[name].items()}
    if not present(rgb(next(iter(SHEET_MAP[name])))):
        return MAP_RGB
    t = {k: own.get(v, v) for k, v in MAP_RGB.items()}
    t.update(own)
    return t


# ------------------------------------------------------------------ PNG / GIF

def recolour_image(path, check):
    im = Image.open(path)
    if getattr(im, 'is_animated', False) or im.format == 'GIF':
        return recolour_gif(path, check)
    rgba = im.convert('RGBA')
    cols = {c[:3] for _, c in rgba.getcolors(rgba.width * rgba.height) if c[3]}
    table = table_for(sheet_of(path), lambda c: c in cols)
    px = rgba.load()
    n = 0
    for y in range(rgba.height):
        for x in range(rgba.width):
            r, g, b, a = px[x, y]
            if a and (r, g, b) in table:
                n += 1
                if not check:
                    px[x, y] = table[(r, g, b)] + (a,)
    if n and not check:
        rgba.save(path)
    return n


def recolour_gif(path, check):
    im = Image.open(path)
    frames, durations, n = [], [], 0
    for i in range(getattr(im, 'n_frames', 1)):
        im.seek(i)
        durations.append(im.info.get('duration', 100))
        frames.append(im.convert('RGBA'))
    strip = {}
    if os.path.basename(path) in CRAFTED_GIFS:
        sw = im.width // 4
        for name, k in GIF_STRIP.items():
            strip[k] = table_for(name, lambda c: any(f.getpixel((x, y))[:3] == c for f in frames
                                                     for y in range(f.height) for x in range(k * sw, (k + 1) * sw)))
    for f in frames:
        px = f.load()
        for y in range(f.height):
            for x in range(f.width):
                table = strip.get(x // sw, MAP_RGB) if strip else MAP_RGB
                r, g, b, a = px[x, y]
                if a and (r, g, b) in table:
                    n += 1
                    px[x, y] = table[(r, g, b)] + (a,)
    if n and not check:
        loop = im.info.get('loop', 0)
        conv = [f.convert('RGB').convert('P', palette=Image.ADAPTIVE, colors=255) for f in frames]
        conv[0].save(path, save_all=True, append_images=conv[1:], duration=durations, loop=loop, optimize=False)
    return n


# ------------------------------------------------------------------ .aseprite (RGBA), patched chunk by chunk

def recolour_ase(path, check, table=None):
    if table is None:
        table = table_for(sheet_of(path), lambda c: recolour_ase(path, True, {c: c}) > 0)
    d = bytearray(open(path, 'rb').read())
    nframes, depth = struct.unpack('<H', d[6:8])[0], struct.unpack('<H', d[12:14])[0]
    assert depth == 32, path + ': not RGBA'
    out, pos, n = bytearray(d[:128]), 128, 0
    for _ in range(nframes):
        flen, magic, old, dur, _r, new = struct.unpack('<IHHH2sI', d[pos:pos + 16])
        count = new or old
        p, chunks = pos + 16, []
        for _ in range(count):
            clen, ct = struct.unpack('<IH', d[p:p + 6])
            c = bytes(d[p + 6:p + clen])
            if ct == 0x2005:
                li, x, y, op, typ, z = struct.unpack('<HhhBHh', c[:11])
                if typ in (0, 2):
                    cw, chh = struct.unpack('<HH', c[16:20])
                    raw = bytearray(c[20:] if typ == 0 else zlib.decompress(c[20:]))
                    for i in range(0, len(raw), 4):
                        key = (raw[i], raw[i + 1], raw[i + 2])
                        if raw[i + 3] and key in table:
                            n += 1
                            raw[i:i + 3] = bytes(table[key])
                    c = c[:20] + (bytes(raw) if typ == 0 else zlib.compress(bytes(raw), 9))
            elif ct == 0x2019:
                c = bytearray(c)
                total, a, b = struct.unpack('<III', c[:12])
                q = 20
                for _k in range(a, b + 1):
                    fl = struct.unpack('<H', c[q:q + 2])[0]
                    key = tuple(c[q + 2:q + 5])
                    if key in table:
                        n += 1
                        c[q + 2:q + 5] = bytes(table[key])
                    q += 6
                    if fl & 1:
                        q += 2 + struct.unpack('<H', c[q:q + 2])[0]
                c = bytes(c)
            chunks.append(struct.pack('<IH', len(c) + 6, ct) + c)
            p += clen
        body = b''.join(chunks)
        out += struct.pack('<IHHH2sI', 16 + len(body), magic, old, dur, _r, new) + body
        pos += flen
    struct.pack_into('<I', out, 0, len(out))
    if n and not check:
        open(path, 'wb').write(bytes(out))
    return n


def files():
    for sub, pats in (('game_size', ('*.png',)), ('preview_8x', ('*.png', '*.gif')), ('aseprite', ('*.aseprite',))):
        for pat in pats:
            for f in sorted(glob.glob(os.path.join(ART, sub, pat))):
                yield f


def main():
    check = '--check' in sys.argv[1:]
    left = 0
    for f in files():
        n = recolour_ase(f, check) if f.endswith('.aseprite') else recolour_image(f, check)
        if n:
            left += n
            print('%-48s %6d px %s' % (os.path.relpath(f, ART), n, 'old colour left' if check else 'recoloured'))
    if check:
        sys.exit(1 if left else 0)
    print()
    print('| Old | New | Old h s v | New h s v |')
    for k, v in MAP.items():
        print('| `%s` | `%s` | %d %d %d | %d %d %d |' % (k, v, *hsv(k), *hsv(v)))
    for name, table in SHEET_MAP.items():
        print()
        print('%s (after MAP):' % name)
        for k, v in table.items():
            print('| `%s` | `%s` | %d %d %d | %d %d %d |' % (k, v, *hsv(k), *hsv(v)))


if __name__ == '__main__':
    main()
