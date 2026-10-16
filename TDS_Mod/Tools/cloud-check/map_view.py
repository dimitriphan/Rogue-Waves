"""map_view.py: the self-test's map snapshot, readable in the cloud.

A vessel's self-test build (the template's, Argo's and Yossele's call kit 1.5's ModKitTest.MapDump) logs the test
map's ground once as "TEST MAP" lines in Player.log: each ground tile as x,y,z plus a flag (b: a 1x1 building and a foe
can stand there, B: a building only, f: a foe only, -: neither), then the core, the vessel and everything of the
vessel's team.

  python3 map_view.py save <Player.log or an excerpt> <out dir>     -> <plan>.txt (the snapshot) and <plan>.png
  python3 map_view.py near <snapshot.txt> <x,y,z> [radius]          -> text picture around a tile, like ModKitTest.Picture
  python3 map_view.py spots <snapshot.txt> <x,y,z> <ring> <maxDist> -> the tiles ModKitTest.Spot would try, in order

Snapshots live in TDS_Vessel_Creator/reference/maps/. The PNG is top-down, north (+z) up, one 6 px cell per column,
shaded by the top tile's height; tiles a building can't take are tinted brown (f), and hatched too where no foe can stand
either (-); the core is red, the vessel blue, the vessel's team black. Stdlib only.
"""
import sys, os, re, struct, zlib

def parse(path):
    plan, tiles, marks = '?', {}, []
    for line in open(path, encoding='utf-8', errors='replace'):
        i = line.find('TEST MAP ')
        if i < 0:
            continue
        rest = line[i + 9:].strip()
        m = re.match(r'plan (\S+):', rest)
        if m:
            plan, tiles, marks = m.group(1), {}, []   # a later dump in the same log replaces an earlier one
            continue
        if rest == 'end':
            continue
        m = re.match(r'(core|vessel|own \S+) (-?\d+),(-?\d+),(-?\d+)$', rest)
        if m:
            marks.append((m.group(1), int(m.group(2)), int(m.group(3)), int(m.group(4))))
            continue
        for tok in rest.split():
            m = re.match(r'(-?\d+),(-?\d+),(-?\d+)([bBf-])$', tok)
            if m:
                tiles[(int(m.group(1)), int(m.group(2)), int(m.group(3)))] = m.group(4)
    return plan, tiles, marks

def save_text(path, plan, tiles, marks):
    with open(path, 'w', encoding='utf-8') as f:
        f.write('# TDS self-test map snapshot (ModKitTest.MapDump). Flags: b building+foe, B building, f foe, - neither.\n')
        f.write('TEST MAP plan %s: %d ground tiles\n' % (plan, len(tiles)))
        keys = sorted(tiles, key=lambda k: (k[2], k[0], k[1]))
        for i in range(0, len(keys), 40):
            f.write('TEST MAP ' + ' '.join('%d,%d,%d%s' % (k + (tiles[k],)) for k in keys[i:i + 40]) + '\n')
        for kind, x, y, z in marks:
            f.write('TEST MAP %s %d,%d,%d\n' % (kind, x, y, z))
        f.write('TEST MAP end\n')

def png(path, w, h, rgb):
    raw = bytearray()
    for y in range(h):
        raw.append(0)
        raw += rgb[y * w * 3:(y + 1) * w * 3]
    def chunk(tag, data):
        return struct.pack('>I', len(data)) + tag + data + struct.pack('>I', zlib.crc32(tag + data) & 0xffffffff)
    with open(path, 'wb') as f:
        f.write(b'\x89PNG\r\n\x1a\n' + chunk(b'IHDR', struct.pack('>IIBBBBB', w, h, 8, 2, 0, 0, 0))
                + chunk(b'IDAT', zlib.compress(bytes(raw), 9)) + chunk(b'IEND', b''))

def save_png(path, tiles, marks, cell=6):
    top = {}
    for (x, y, z), f in tiles.items():
        if (x, z) not in top or y > top[(x, z)][0]:
            top[(x, z)] = (y, f)
    xs = [k[0] for k in top]; zs = [k[1] for k in top]; ys = [v[0] for v in top.values()]
    x0, x1, z0, z1, y0, y1 = min(xs), max(xs), min(zs), max(zs), min(ys), max(ys)
    w, h = (x1 - x0 + 1) * cell, (z1 - z0 + 1) * cell
    img = bytearray(b'\x20\x24\x30' * (w * h))
    def put(px, py, c):
        if 0 <= px < w and 0 <= py < h:
            img[(py * w + px) * 3:(py * w + px) * 3 + 3] = bytes(c)
    def fill(x, z, c, inset=0):
        cx, cy = (x - x0) * cell, (z1 - z) * cell
        for py in range(cy + inset, cy + cell - inset):
            for px in range(cx + inset, cx + cell - inset):
                put(px, py, c)
    for (x, z), (y, f) in top.items():
        t = (y - y0) / max(1, y1 - y0)
        base = (int(70 + 150 * t), int(110 + 120 * t), int(60 + 90 * t))
        if f in 'f-':
            base = (base[0] // 2 + 60, base[1] // 2, base[2] // 2)   # no building here
        fill(x, z, base)
        if f == '-':
            cx, cy = (x - x0) * cell, (z1 - z) * cell
            for d in range(cell):
                put(cx + d, cy + d, (40, 20, 20))
    for kind, x, y, z in marks:
        fill(x, z, (230, 40, 40) if kind == 'core' else (60, 120, 255) if kind == 'vessel' else (0, 0, 0), 1)
    png(path, w, h, img)
    return x0, x1, z0, z1, y0, y1

def near(tiles, cx, cy, cz, r):
    lines = ['ground around %d,%d,%d (north up; 0 same level, 1-9 above, a-h below; upper case = a building fits, '
             'lower case/x = it does not; blank = no ground; @ the centre)' % (cx, cy, cz)]
    cols = {}
    for (x, y, z), f in tiles.items():
        if -8 <= y - cy <= 4 and ((x, z) not in cols or y > cols[(x, z)][0]):
            cols[(x, z)] = (y, f)
    for z in range(cz + r, cz - r - 1, -1):
        row = ''
        for x in range(cx - r, cx + r + 1):
            if (x, z) == (cx, cz):
                row += '@'; continue
            if (x, z) not in cols:
                row += ' '; continue
            y, f = cols[(x, z)]
            hgt = y - cy
            ch = str(min(9, hgt)) if hgt >= 0 else 'abcdefgh'[min(7, -hgt - 1)]
            if f in 'f-':
                ch = 'x' if ch.isdigit() else ch.lower()
            elif not ch.isdigit():
                ch = ch.upper()
            row += ch
        lines.append('%4d | %s' % (z, row))
    return '\n'.join(lines)

def spots(tiles, c, ring, max_dist, used=()):
    # ModKitTest.Spot's order: the ground within a level of c nearest the ring, then every level (2 above to 6 below)
    # nearest c. Only tiles a 1x1 building takes (flags b, B) count.
    flat = lambda t: ((t[0] - c[0]) ** 2 + (t[2] - c[2]) ** 2) ** 0.5
    ground = [t for t in tiles if flat(t) <= max_dist and -6 <= t[1] - round(c[1]) <= 2]
    first = sorted([t for t in ground if abs(t[1] - c[1]) <= 1.1], key=lambda t: abs(flat(t) - ring))
    order, seen = [], set()
    for t in first + sorted(ground, key=flat):
        if t not in seen:
            seen.add(t); order.append(t)
    return [t for t in order if tiles[t] in 'bB' and t not in used]

def xyz(s):
    return tuple(int(round(float(v))) for v in s.split(','))

if __name__ == '__main__':
    if len(sys.argv) < 3:
        sys.exit(__doc__)
    cmd = sys.argv[1]
    plan, tiles, marks = parse(sys.argv[2])
    if not tiles:
        sys.exit('no "TEST MAP" tiles in ' + sys.argv[2])
    if cmd == 'save':
        out = sys.argv[3] if len(sys.argv) > 3 else '.'
        os.makedirs(out, exist_ok=True)
        save_text(os.path.join(out, plan + '.txt'), plan, tiles, marks)
        b = save_png(os.path.join(out, plan + '.png'), tiles, marks)
        print('%s: %d ground tiles, x %d..%d, z %d..%d, y %d..%d; %s' % (plan, len(tiles), b[0], b[1], b[2], b[3], b[4], b[5],
              ', '.join('%s %d,%d,%d' % m for m in marks if m[0] in ('core', 'vessel'))))
    elif cmd == 'near':
        c = xyz(sys.argv[3])
        print(near(tiles, c[0], c[1], c[2], int(sys.argv[4]) if len(sys.argv) > 4 else 8))
    elif cmd == 'spots':
        c = xyz(sys.argv[3])
        for t in spots(tiles, c, float(sys.argv[4]), float(sys.argv[5]))[:12]:
            print('%d,%d,%d  flat %.2f  level %+d' % (t + (((t[0] - c[0]) ** 2 + (t[2] - c[2]) ** 2) ** 0.5, t[1] - c[1])))
    else:
        sys.exit(__doc__)
