#!/usr/bin/env python3
"""TDS vessel sprite sheets <-> .aseprite files. Python 3 standard library only.

  python3 ase_sheets.py pack <Vessel> <OutDir> [--version X] <sheet.png>...   sheets -> <Vessel>.aseprite (32 px), _16px, _splash, _readyup
  python3 ase_sheets.py unpack <OutDir> <file.aseprite>...       .aseprite -> sheets/<Vessel|UI|Unit>/<Sheet>.png

Every tag carries user data saying where its frames sit in the game's sheet, plus a CRC-32 of each frame as written
and, for facings the art draws as mirrors, the tag they mirror:
  sheet=Vessel/Argo-Sheet.png;row=0;col=0;cols=6;rows=8;gen=1a2b3c4d,...;mirror=idle_v0
Each vessel's builder (<Name>Aseprite.cs) writes the same files and merges hand-edited frames back in on Build All
Assets. unpack (and export_sheets.lua inside Aseprite) rebuilds whole sheets from the tags instead, in the layout the
mod's sprite import expects: rows top to bottom, v0 v1 v2 v3, cells of the file's canvas size.
"""
import os
import re
import struct
import sys
import zlib

# ------------------------------------------------------------------ PNG

def png_read(path):
    data = open(path, 'rb').read()
    if data[:8] != b'\x89PNG\r\n\x1a\n':
        raise ValueError(path + ': not a PNG')
    pos, idat, plte, trns = 8, b'', None, None
    while pos < len(data):
        n, = struct.unpack('>I', data[pos:pos + 4])
        t, c = data[pos + 4:pos + 8], data[pos + 8:pos + 8 + n]
        pos += 12 + n
        if t == b'IHDR':
            w, h, bd, ct, _, _, il = struct.unpack('>IIBBBBB', c)
        elif t == b'PLTE':
            plte = c
        elif t == b'tRNS':
            trns = c
        elif t == b'IDAT':
            idat += c
        elif t == b'IEND':
            break
    if bd != 8 or il != 0:
        raise ValueError(path + ': only 8-bit, non-interlaced PNGs are supported')
    ch = {0: 1, 2: 3, 3: 1, 4: 2, 6: 4}[ct]
    raw, stride = zlib.decompress(idat), w * ch
    prev, i, rows = bytearray(stride), 0, []
    for _ in range(h):
        f = raw[i]
        line = bytearray(raw[i + 1:i + 1 + stride])
        i += 1 + stride
        for x in range(stride):
            a = line[x - ch] if x >= ch else 0
            b = prev[x]
            if f == 1:
                line[x] = (line[x] + a) & 255
            elif f == 2:
                line[x] = (line[x] + b) & 255
            elif f == 3:
                line[x] = (line[x] + ((a + b) >> 1)) & 255
            elif f == 4:
                c = prev[x - ch] if x >= ch else 0
                p = a + b - c
                pa, pb, pc = abs(p - a), abs(p - b), abs(p - c)
                line[x] = (line[x] + (a if pa <= pb and pa <= pc else b if pb <= pc else c)) & 255
        rows.append(bytes(line))
        prev = line
    out = bytearray()
    for line in rows:
        for x in range(w):
            if ct == 6:
                px = line[x * 4:x * 4 + 4]
            elif ct == 2:
                px = line[x * 3:x * 3 + 3] + b'\xff'
            elif ct == 0:
                px = bytes((line[x],) * 3) + b'\xff'
            elif ct == 4:
                px = bytes((line[x * 2],) * 3) + line[x * 2 + 1:x * 2 + 2]
            else:
                k = line[x]
                px = plte[k * 3:k * 3 + 3] + bytes((trns[k] if trns and k < len(trns) else 255,))
            out += px
    return w, h, normalise(out)


def png_write(path, w, h, rgba):
    raw = b''.join(b'\x00' + rgba[y * w * 4:(y + 1) * w * 4] for y in range(h))

    def chunk(t, c):
        return struct.pack('>I', len(c)) + t + c + struct.pack('>I', zlib.crc32(t + c) & 0xffffffff)
    os.makedirs(os.path.dirname(path) or '.', exist_ok=True)
    with open(path, 'wb') as f:
        f.write(b'\x89PNG\r\n\x1a\n' + chunk(b'IHDR', struct.pack('>IIBBBBB', w, h, 8, 6, 0, 0, 0))
                + chunk(b'IDAT', zlib.compress(raw, 9)) + chunk(b'IEND', b''))


def normalise(rgba):
    """Fully transparent pixels become 0,0,0,0 (Aseprite and Unity treat them the same)."""
    out = bytearray(rgba)
    for i in range(0, len(out), 4):
        if out[i + 3] == 0:
            out[i:i + 4] = b'\0\0\0\0'
    return bytes(out)


def flip_x(frame, cell):
    return b''.join(b''.join(frame[(y * cell + x) * 4:(y * cell + x) * 4 + 4] for x in range(cell - 1, -1, -1))
                    for y in range(cell))


def crop(w, h, rgba, x, y, cw, ch):
    return b''.join(rgba[((y + r) * w + x) * 4:((y + r) * w + x + cw) * 4] for r in range(ch))

# ------------------------------------------------------------------ sheet layouts

KIND_COLOUR = {'idle': (106, 190, 48), 'move': (91, 110, 225), 'attack': (217, 87, 99), 'ability': (178, 102, 216),
               'fall': (223, 113, 38), 'land': (238, 195, 154), 'mini': (95, 205, 228), 'unit': (55, 148, 110),
               'ui': (155, 173, 183)}


def snake(s):
    return re.sub(r'_+', '_', re.sub(r'(?<=[a-z0-9])(?=[A-Z])', '_', s)).lower()


def layout(vessel, name, w, h, stages):
    """-> (folder, cell, sort key, [[(tag, frames, kind)] per row]) or None to skip."""
    V, lv = vessel, vessel.lower()

    def vrows(tag, n, kind):
        return [[(tag + '_v%d' % i, n, kind)] for i in range(4)]
    one = {V + '_Icon': 'icon', V + 'endstat1': 'end_win', V + 'endstat2': 'end_lose', V + 'tabicon': 'journal_tab'}
    if name in one and (w, h) == (32, 32):
        return 'UI', 32, (90, list(one).index(name)), [[(one[name], 1, 'ui')]]
    if name == V + 'splash':
        return 'UI', 128, (95, 0), [[('splash', 1, 'ui')]]
    if name == 'Readyup6' + lv:
        return 'UI', 12, (96, 0), [[('readyup', 1, 'ui')]]
    if name == 'S' + V + 'Sheet' and (w, h) == (96, 128):
        return 'UI', 16, (0, 0), vrows('mini_idle', 6, 'mini') + vrows('mini_move', 6, 'mini')
    if name == V + 'Idlejustidle-Sheet' and h == 32:
        return 'UI', 32, (80, 0), [[('menu_idle', w // 32, 'idle')]]
    if not name.endswith('-Sheet'):
        return None
    rem = name[:-len('-Sheet')]
    # unit: 4 rows v0-v3; the only sheets 64 high, so the name isn't checked (the scaffold names a unit <Vessel>unit)
    if h == 64 and w % 16 == 0:
        u, cols = snake(rem), w // 16
        if cols == 10:  # move 0-5 (idle is column 0), attack 6-9
            rows = [[(u + '_move_v%d' % i, 6, 'unit'), (u + '_attack_v%d' % i, 4, 'unit')] for i in range(4)]
        else:
            rows = vrows(u, cols, 'unit')
        return 'Unit', 16, (10, rem), rows
    if not rem.startswith(V):
        return None
    rem = rem[len(V):]
    stage = next((s for s in stages if rem.startswith(s)), '')
    rest = rem[len(stage):]
    pre = (snake(stage) + '_') if stage else ''
    si = stages.index(stage) if stage in stages else 0
    if rest == '' and (w, h) == (192, 256):
        return 'Vessel', 32, (si, 0), vrows(pre + 'idle', 6, 'idle') + vrows(pre + 'move', 6, 'move')
    if rest == 'Attack' and (w, h) == (128, 128):
        return 'Vessel', 32, (si, 1), vrows(pre + 'attack', 4, 'attack')
    if rest == 'Fall' and h == 32:
        return 'Vessel', 32, (si, 3), [[(pre + 'fall', w // 32, 'fall')]]
    if rest == 'IntroR' and h == 32:
        return 'Vessel', 32, (si, 4), [[(pre + 'land', w // 32, 'land')]]
    if rest and (w, h) == (192, 128):
        return 'Vessel', 32, (si, 2), vrows(pre + snake(rest), 6, 'ability')
    return None


def file_suffix(cell):
    return {32: '', 16: '_16px', 128: '_splash', 12: '_readyup'}[cell]

# ------------------------------------------------------------------ .aseprite writer

def s16(s):
    b = s.encode('utf-8')
    return struct.pack('<H', len(b)) + b


def chunk(t, data):
    return struct.pack('<IH', len(data) + 6, t) + data


def bbox(w, h, rgba):
    xs, ys = [], []
    for y in range(h):
        for x in range(w):
            if rgba[(y * w + x) * 4 + 3]:
                xs.append(x)
                ys.append(y)
    return (min(xs), min(ys), max(xs) + 1, max(ys) + 1) if xs else None


def write_ase(path, w, h, frames, tags, palette, layer_name, duration=100):
    """frames: list of RGBA bytes (w*h*4); tags: (name, from, to, rgb, text); palette: list of RGBA tuples."""
    out = []
    for i, px in enumerate(frames):
        ch = []
        if i == 0:
            ch.append(chunk(0x2007, struct.pack('<HHI8x', 1, 0, 0)))  # sRGB
            ch.append(chunk(0x2019, struct.pack('<III8x', len(palette), 0, len(palette) - 1)
                            + b''.join(struct.pack('<H4B', 0, *c) for c in palette)))
            ch.append(chunk(0x2004, struct.pack('<HHHHHHB3x', 3, 0, 0, 0, 0, 0, 255) + s16(layer_name)))
            if tags:
                t = struct.pack('<H8x', len(tags))
                for name, a, b, rgb, _ in tags:
                    t += struct.pack('<HHBH6x3Bx', a, b, 0, 0, *rgb) + s16(name)
                ch.append(chunk(0x2018, t))
                for _, _, _, rgb, text in tags:  # one user data chunk per tag, in tag order
                    ch.append(chunk(0x2020, struct.pack('<I', 3) + s16(text) + struct.pack('4B', *rgb, 255)))
        bb = bbox(w, h, px)
        if bb:
            x0, y0, x1, y1 = bb
            ch.append(chunk(0x2005, struct.pack('<HhhBHh5x', 0, x0, y0, 255, 2, 0) + struct.pack('<HH', x1 - x0, y1 - y0)
                            + zlib.compress(crop(w, h, px, x0, y0, x1 - x0, y1 - y0), 9)))
        body = b''.join(ch)
        out.append(struct.pack('<IHHH2xI', 16 + len(body), 0xF1FA, min(len(ch), 0xFFFF), duration, len(ch)) + body)
    body = b''.join(out)
    head = struct.pack('<IHHHHHIHIIB3xHBBhhHH84x', 128 + len(body), 0xA5E0, len(frames), w, h, 32, 1, duration, 0, 0,
                       0, len(palette), 1, 1, 0, 0, 16, 16)
    with open(path, 'wb') as f:
        f.write(head + body)

# ------------------------------------------------------------------ .aseprite reader

def read_ase(path):
    d = open(path, 'rb').read()
    size, magic, nframes, w, h, depth = struct.unpack('<IHHHHH', d[:14])
    if magic != 0xA5E0:
        raise ValueError(path + ': not an .aseprite file')
    if depth != 32:
        raise ValueError(path + ': colour mode must be RGBA (Sprite > Color Mode > RGB Color)')
    pos, layers, frames, tags, palette, durations = 128, [], [], [], [], []
    pending = []  # tags still waiting for their user data chunk
    for fi in range(nframes):
        flen, fmagic, old, dur, _, new = struct.unpack('<IHHH2sI', d[pos:pos + 16])
        n = new or old
        p, cels = pos + 16, {}
        for _ in range(n):
            clen, ct = struct.unpack('<IH', d[p:p + 6])
            c = d[p + 6:p + clen]
            if ct == 0x2004:
                fl, typ, lvl, _, _, blend, op = struct.unpack('<HHHHHHB', c[:13])
                ln, = struct.unpack('<H', c[16:18])
                layers.append({'visible': fl & 1, 'type': typ, 'level': lvl, 'blend': blend, 'opacity': op,
                               'name': c[18:18 + ln].decode('utf-8', 'replace')})
                pending = []
            elif ct == 0x2005:
                li, x, y, op, typ, z = struct.unpack('<HhhBHh', c[:11])
                if typ == 1:
                    cels[li] = ('link', struct.unpack('<H', c[16:18])[0])
                else:
                    cw, chh = struct.unpack('<HH', c[16:20])
                    px = c[20:] if typ == 0 else zlib.decompress(c[20:])
                    cels[li] = (x, y, cw, chh, op, px)
                pending = []
            elif ct == 0x2018:
                cnt, = struct.unpack('<H', c[:2])
                q, pending = 10, []
                for _ in range(cnt):
                    a, b, loop, rep = struct.unpack('<HHBH', c[q:q + 7])
                    q += 17
                    ln, = struct.unpack('<H', c[q:q + 2])
                    tag = {'name': c[q + 2:q + 2 + ln].decode('utf-8', 'replace'), 'from': a, 'to': b, 'data': ''}
                    q += 2 + ln
                    tags.append(tag)
                    pending.append(tag)
            elif ct == 0x2020:
                fl, = struct.unpack('<I', c[:4])
                text = ''
                if fl & 1:
                    ln, = struct.unpack('<H', c[4:6])
                    text = c[6:6 + ln].decode('utf-8', 'replace')
                if pending:
                    pending.pop(0)['data'] = text
            elif ct == 0x2019:
                total, a, b = struct.unpack('<III', c[:12])
                palette += [None] * max(0, total - len(palette))
                q = 20
                for k in range(a, b + 1):
                    fl, r, g, bl, al = struct.unpack('<H4B', c[q:q + 6])
                    q += 6
                    if fl & 1:
                        ln, = struct.unpack('<H', c[q:q + 2])
                        q += 2 + ln
                    palette[k] = (r, g, bl, al)
            p += clen
        for li, cel in list(cels.items()):
            if cel[0] == 'link':
                cels[li] = frames[cel[1]][li]
        frames.append(cels)
        durations.append(dur)
        pos += flen
    return {'w': w, 'h': h, 'layers': layers, 'frames': frames, 'tags': tags, 'palette': palette,
            'durations': durations}


def visible_layers(layers):
    """Indices of visible image layers whose parent groups are visible too."""
    out, stack = [], []
    for i, l in enumerate(layers):
        stack = stack[:l['level']]
        shown = l['visible'] and all(stack)
        stack.append(shown)
        if l['type'] == 0 and shown:
            out.append(i)
    return out


def flatten(ase, fi):
    """Composite one frame (normal blend, layer and cel opacity) -> RGBA bytes."""
    w, h = ase['w'], ase['h']
    img = bytearray(w * h * 4)
    for li in visible_layers(ase['layers']):
        cel = ase['frames'][fi].get(li)
        if not cel:
            continue
        x0, y0, cw, chh, cop, px = cel
        lop = ase['layers'][li]['opacity']
        for y in range(chh):
            for x in range(cw):
                tx, ty = x0 + x, y0 + y
                if not (0 <= tx < w and 0 <= ty < h):
                    continue
                s = px[(y * cw + x) * 4:(y * cw + x) * 4 + 4]
                sa = s[3] * cop * lop // (255 * 255)
                if sa == 0:
                    continue
                t = (ty * w + tx) * 4
                da = img[t + 3]
                if da == 0 or sa == 255:
                    img[t:t + 4] = bytes((s[0], s[1], s[2], sa))
                    continue
                oa = sa + da * (255 - sa) // 255
                for k in range(3):
                    img[t + k] = (s[k] * sa + img[t + k] * da * (255 - sa) // 255) // oa
                img[t + 3] = oa
    return bytes(img)


def sheets_from_ase(ase):
    """-> {relative sheet path: (w, h, rgba)} from the tags' user data."""
    w, h, sheets, warn = ase['w'], ase['h'], {}, []
    for tag in ase['tags']:
        kv = dict(p.split('=', 1) for p in tag['data'].split(';') if '=' in p)
        if 'sheet' not in kv:
            continue
        row, col, cols, rows = (int(kv[k]) for k in ('row', 'col', 'cols', 'rows'))
        sw, sh = cols * w, rows * h
        img = sheets.setdefault(kv['sheet'], (sw, sh, bytearray(sw * sh * 4)))[2]
        n = tag['to'] - tag['from'] + 1
        if col + n > cols:
            warn.append('%s has %d frames, its row only fits %d' % (tag['name'], n, cols - col))
        for k in range(min(n, cols - col)):
            fr = flatten(ase, tag['from'] + k)
            ox, oy = (col + k) * w, row * h
            for y in range(h):
                img[((oy + y) * sw + ox) * 4:((oy + y) * sw + ox + w) * 4] = fr[y * w * 4:(y + 1) * w * 4]
    return {k: (v[0], v[1], bytes(v[2])) for k, v in sheets.items()}, warn

# ------------------------------------------------------------------ commands

def pack(vessel, outdir, pngs, version=''):
    named = {os.path.splitext(os.path.basename(p))[0]: p for p in pngs}
    sizes = {n: png_read(p) for n, p in named.items()}
    # Growth stages (Sprigg): main sheets named <Vessel><Stage>-Sheet
    stages = sorted({n[len(vessel):-len('-Sheet')] for n, (w, h, _) in sizes.items()
                     if n.startswith(vessel) and n.endswith('-Sheet') and (w, h) == (192, 256)} - {''},
                    key=lambda s: ['Sprout', 'Bud', 'Leafy', 'Bloom'].index(s) if s in
                    ['Sprout', 'Bud', 'Leafy', 'Bloom'] else 9)
    groups, skipped = {}, []
    for n, (w, h, px) in sizes.items():
        lay = layout(vessel, n, w, h, stages)
        if not lay:
            skipped.append(n)
            continue
        folder, cell, key, rows = lay
        groups.setdefault(cell, []).append((key, n, folder, w, h, px, rows))
    colours = set()
    for _, (w, h, px) in sizes.items():
        colours |= {px[i:i + 4] for i in range(0, len(px), 4) if px[i + 3]}

    def order(c):
        r, g, b, a = c
        mx, mn = max(r, g, b), min(r, g, b)
        if mx == mn:
            hue = -1
        elif mx == r:
            hue = ((g - b) / (mx - mn)) % 6
        elif mx == g:
            hue = (b - r) / (mx - mn) + 2
        else:
            hue = (r - g) / (mx - mn) + 4
        grey = mx - mn < 24
        return (a < 255, -a, grey, round(hue * 2) if not grey else 0, 0.299 * r + 0.587 * g + 0.114 * b)
    palette = [(0, 0, 0, 0)] + sorted((tuple(c) for c in colours), key=order)
    if len(palette) > 256:
        raise ValueError('%s uses %d colours, more than a palette holds' % (vessel, len(palette)))
    written = []
    for cell, items in groups.items():
        items.sort(key=lambda t: (t[0][0] if isinstance(t[0][0], int) else 0, str(t[0][1]), t[1]))
        frames, tags = [], []
        for _, n, folder, w, h, px, rows in items:
            cols = max(sum(t[1] for t in r) for r in rows)
            mine = {}
            for ri, row in enumerate(rows):
                col = 0
                for tag, count, kind in row:
                    start = len(frames)
                    for k in range(count):
                        frames.append(crop(w, h, px, (col + k) * cell, ri * cell, cell, cell))
                    gen = ','.join('%08x' % zlib.crc32(f) for f in frames[start:])
                    mine[tag] = len(tags)
                    tags.append([tag, start, len(frames) - 1, KIND_COLOUR[kind],
                                 'sheet=%s/%s.png;row=%d;col=%d;cols=%d;rows=%d;gen=%s' % (folder, n, ri, col, cols, len(rows), gen)])
                    col += count
            # Facings the art draws as mirrors (v1 = v0 flipped, v3 = v2 flipped) follow edits to their source.
            for tag, i in mine.items():
                if not tag.endswith(('_v1', '_v3')):
                    continue
                src = tag[:-1] + ('0' if tag.endswith('_v1') else '2')
                if src in mine:
                    t, o = tags[i], tags[mine[src]]
                    if t[2] - t[1] == o[2] - o[1] and all(frames[t[1] + k] == flip_x(frames[o[1] + k], cell)
                                                          for k in range(t[2] - t[1] + 1)):
                        t[4] += ';mirror=' + src
        path = os.path.join(outdir, vessel + file_suffix(cell) + '.aseprite')
        write_ase(path, cell, cell, frames, tags, palette, vessel + (' ' + version if version else ''))
        written.append((path, len(frames), len(tags)))
    return written, skipped, len(palette)


def main(argv):
    if len(argv) >= 4 and argv[1] == 'pack':
        vessel, outdir, files, version = argv[2], argv[3], argv[4:], ''
        if files[:1] == ['--version']:
            version, files = files[1], files[2:]
        os.makedirs(outdir, exist_ok=True)
        written, skipped, ncol = pack(vessel, outdir, files, version)
        for p, nf, nt in written:
            print('%s: %d frames, %d tags' % (p, nf, nt))
        print('palette: %d colours; skipped: %s' % (ncol, ', '.join(sorted(skipped)) or 'none'))
    elif len(argv) >= 4 and argv[1] == 'unpack':
        outdir = argv[2]
        for f in argv[3:]:
            sheets, warn = sheets_from_ase(read_ase(f))
            for rel, (w, h, px) in sorted(sheets.items()):
                png_write(os.path.join(outdir, rel), w, h, px)
                print('%s -> %s (%dx%d)' % (os.path.basename(f), rel, w, h))
            for m in warn:
                print('warning: ' + m)
    else:
        print(__doc__)
        return 1
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv))
