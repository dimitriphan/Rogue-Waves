#!/usr/bin/env python3
"""Builds the Art Bench page: every mod sprite sheet cut into frames, and every model, for editing in Aseprite and
Blender (README.md). Run from anywhere once cloud-check is set up (Tools/cloud-check/setup.sh):

    python3 TDS_Mod/Tools/artbench/build.py <out_dir> [--cache <dir>]

Writes <out_dir>/index.html (publish it to the Art Bench artifact) and <out_dir>/models/<Part>.json (published with it).
Each sheet shows what the Unity builder would use: the code's drawing, a file in Art/ (final) or Art/Temporary/game_size/
(temporary) over it, then hand-edited frames from ModSource/<Part>/Aseprite/*.aseprite merged in by CRC, as
<Part>Aseprite.Apply does. Base-vessel game sprites are never read.
"""
import base64, json, os, subprocess, sys, zlib

HERE = os.path.dirname(os.path.abspath(__file__))
MOD = os.path.normpath(os.path.join(HERE, '..', '..'))           # TDS_Mod
sys.path.insert(0, os.path.join(MOD, 'Tools', 'aseprite'))
import ase_sheets as A  # noqa: E402

VESSELS = ['Yossele', 'Spool', 'Uloe', 'Argo']
MODEL_PARTS = VESSELS + ['RogueWavesMode']
V = ['v0 front, heading right', 'v1 front, heading left', 'v2 back, heading right', 'v3 back, heading left']

# Sheets ase_sheets.layout() doesn't know (no merge route in the builder): their cell size and tags per row.
def facings(cell, *tags):
    return cell, [[(t + '_v%d' % i, n, k) for t, n, k in tags] for i in range(4)]
FILE_LAYOUTS = {
    'Spoolhead-Sheet': ('Unit',) + facings(24, ('thread_head_move', 6, 'unit'), ('thread_head_attack', 4, 'unit')),
    'Yosselefriend-Sheet': ('Unit',) + facings(24, ('cordyfriend_idle', 6, 'unit'), ('cordyfriend_move', 6, 'unit'),
                                               ('cordyfriend_attack', 4, 'unit')),
    'UloeLit-Sheet': ('Vessel', 32, [[('lit_idle_v%d' % i, 6, 'idle')] for i in range(4)] +
                      [[('lit_move_v%d' % i, 6, 'move')] for i in range(4)]),
    'Vendcint-Sheet': ('Unit',) + facings(32, ('vendcint', 6, 'unit')),
}


def uri(w, h, rgba):
    return 'data:image/png;base64,' + base64.b64encode(png_bytes(w, h, rgba)).decode()


def png_bytes(w, h, rgba):
    import struct
    raw = b''.join(b'\0' + rgba[y * w * 4:(y + 1) * w * 4] for y in range(h))
    def chunk(t, c):
        return struct.pack('>I', len(c)) + t + c + struct.pack('>I', zlib.crc32(t + c) & 0xffffffff)
    return (b'\x89PNG\r\n\x1a\n' + chunk(b'IHDR', struct.pack('>IIBBBBB', w, h, 8, 6, 0, 0, 0)) +
            chunk(b'IDAT', zlib.compress(raw, 9)) + chunk(b'IEND', b''))


def run_preview(part, flags, cache):
    out = os.path.join(cache, part)
    if not os.path.isdir(os.path.join(out, 'sheets' if 's' in flags else 'meshes')):
        ed = os.path.join(MOD, 'Assets', part + 'Mod', 'Editor')
        subprocess.run([os.path.join(MOD, 'Tools', 'cloud-check', 'lite', 'run_preview.sh'), '.', part, out, flags],
                       cwd=ed, check=True, stdout=subprocess.DEVNULL)
    return out


def hand_edits(part):
    """{folder/name.png: [(tag, row, col, cols, k, edited rgba or None, mirror)]} from ModSource/<Part>/Aseprite/."""
    d, out = os.path.join(MOD, 'ModSource', part, 'Aseprite'), {}
    if not os.path.isdir(d):
        return out
    for f in sorted(os.listdir(d)):
        if not f.endswith('.aseprite'):
            continue
        ase = A.read_ase(os.path.join(d, f))
        for tag in ase['tags']:
            kv = dict(p.split('=', 1) for p in tag['data'].split(';') if '=' in p)
            if 'sheet' not in kv:
                continue
            gen = kv.get('gen', '').split(',')
            for k in range(tag['to'] - tag['from'] + 1):
                fr = A.flatten(ase, tag['from'] + k)
                mine = k >= len(gen) or '%08x' % zlib.crc32(fr) != gen[k]
                out.setdefault(kv['sheet'], []).append((tag['name'], int(kv['row']), int(kv['col']) + k, ase['w'],
                                                        k, fr if mine else None, kv.get('mirror')))
    return out


def apply_edits(w, h, px, cell, edits):
    """The builder's merge: edited frames replace the sheet's; a mirror tag follows its source's edit."""
    px = bytearray(px)
    by = {(t, k): e for t, r, c, cw, k, e, m in edits}
    n = 0
    for t, r, c, cw, k, e, m in edits:
        if cw != cell:
            continue
        src = e if e is not None else (A.flip_x(by[(m, k)], cell) if m and by.get((m, k)) is not None else None)
        if src is None:
            continue
        n += 1
        for y in range(cell):
            o = ((r * cell + y) * w + c * cell) * 4
            px[o:o + cell * 4] = src[y * cell * 4:(y + 1) * cell * 4]
    return bytes(px), n


def sheet_entry(part, name, w, h, px, folder, cell, rows, origin, route, target, edited=0):
    out_rows = []
    for ri, row in enumerate(rows):
        col, tags = 0, []
        for tag, count, kind in row:
            tags.append({'name': tag, 'col': col, 'count': count, 'kind': kind})
            col += count
        out_rows.append({'tags': tags})
    # Facings the art draws as mirrors: v1 = v0 flipped, v3 = v2 flipped (the builder's merge follows the source).
    frames = lambda r, c: A.crop(w, h, px, c * cell, r * cell, cell, cell)
    for ri, row in enumerate(out_rows):
        for t in row['tags']:
            if t['name'].endswith(('_v1', '_v3')):
                src = t['name'][:-1] + ('0' if t['name'].endswith('_v1') else '2')
                for rj, r2 in enumerate(out_rows):
                    for t2 in r2['tags']:
                        if t2['name'] == src and t2['count'] == t['count'] and all(
                                frames(ri, t['col'] + k) == A.flip_x(frames(rj, t2['col'] + k), cell) for k in range(t['count'])):
                            t['mirrorOf'] = src
    return {'part': part, 'name': name, 'folder': folder, 'w': w, 'h': h, 'cell': cell, 'rows': out_rows,
            'origin': origin, 'route': route, 'target': target, 'edited': edited, 'src': uri(w, h, px), '_px': px}


def label_rows(sheet):
    for r in sheet['rows']:
        names = [t['name'] for t in r['tags']]
        r['label'] = ' + '.join(names)
    return sheet


def base_sheet(part, name, cache):
    """The sheet before hand edits, as the builder's FileArt gives it: (w, h, rgba, origin, path, drawn by code)."""
    code = os.path.join(run_preview(part, 's', cache), 'sheets', name + '.png')
    src = os.path.join(MOD, 'ModSource', part, 'Art')
    final, temp = os.path.join(src, name + '.png'), os.path.join(src, 'Temporary', 'game_size', name + '.png')
    path = final if os.path.exists(final) else temp if os.path.exists(temp) else code
    w, h, px = A.png_read(path)
    return w, h, px, ('final' if path == final else 'temporary' if path == temp else 'code'), path, os.path.exists(code)


def vessel_sheets(part, cache):
    code_dir = os.path.join(run_preview(part, 's', cache), 'sheets')
    src = os.path.join(MOD, 'ModSource', part, 'Art')
    names = {f[:-4] for f in os.listdir(code_dir) if f.endswith('.png')}
    files = {f[:-4] for d in (src, os.path.join(src, 'Temporary', 'game_size')) if os.path.isdir(d)
             for f in os.listdir(d) if f.endswith('.png')}
    edits, out = hand_edits(part), []
    for name in sorted(names | files):
        final, temp = os.path.join(src, name + '.png'), os.path.join(src, 'Temporary', 'game_size', name + '.png')
        path = final if os.path.exists(final) else temp if os.path.exists(temp) else os.path.join(code_dir, name + '.png')
        origin = 'final' if path == final else 'temporary' if path == temp else 'code'
        w, h, px = A.png_read(path)
        lay = A.layout(part, name, w, h, []) if name in names else None
        if lay:
            folder, cell, _, rows = lay
            route, target = 'merge', 'TDS_Mod/ModSource/%s/Aseprite/%s.aseprite' % (part, name)
        elif name in FILE_LAYOUTS:
            folder, cell, rows = FILE_LAYOUTS[name]
            route = 'file'
        elif w == h and w <= 32:
            folder, cell, rows, route = 'UI', w, [[(A.snake(name), 1, 'ui')]], 'file'
        else:
            print('skipped (no frame layout):', part, name, w, h)
            continue
        if route == 'file':
            target = os.path.relpath(path if origin != 'code' else final, os.path.dirname(MOD))
        n = 0
        if route == 'merge' and folder + '/' + name + '.png' in edits:
            px, n = apply_edits(w, h, px, cell, edits[folder + '/' + name + '.png'])
        if (w % cell) or (h % cell) or h // cell != len(rows):
            print('skipped (layout does not fit):', part, name, w, h, cell, len(rows))
            continue
        out.append(label_rows(sheet_entry(part, name, w, h, px, folder, cell, rows, origin, route, target, n)))
    return out


def mode_sheets():
    d = os.path.join(MOD, 'ModSource', 'RogueWavesMode', 'Art', 'Temporary', 'game_size')
    sheets, icons = [], []
    for f in sorted(os.listdir(d)):
        if not f.endswith('.png'):
            continue
        name, path = f[:-4], os.path.join(d, f)
        w, h, px = A.png_read(path)
        target = os.path.relpath(path, os.path.dirname(MOD))
        if name in FILE_LAYOUTS:
            folder, cell, rows = FILE_LAYOUTS[name]
            sheets.append(label_rows(sheet_entry('RogueWavesMode', name, w, h, px, folder, cell, rows, 'temporary', 'file', target)))
        else:
            kind = name.split('_')[0]
            icons.append({'part': 'RogueWavesMode', 'name': name, 'kind': kind, 'w': w, 'h': h, 'cell': w,
                          'route': 'file', 'target': target, 'origin': 'temporary', 'src': uri(w, h, px), '_px': px,
                          'rows': [{'tags': [{'name': A.snake(name), 'col': 0, 'count': 1, 'kind': 'ui'}], 'label': name}]})
    return sheets, icons


def part_palette(entries):
    """Every colour the part's sheets use, ordered like ase_sheets.pack (the .aseprite palette)."""
    cols = set()
    for e in entries:
        px = e.pop('_px')
        cols |= {px[i:i + 4] for i in range(0, len(px), 4) if px[i + 3]}
    def order(c):
        r, g, b, a = c
        mx, mn = max(r, g, b), min(r, g, b)
        hue = -1 if mx == mn else (((g - b) / (mx - mn)) % 6 if mx == r else (b - r) / (mx - mn) + 2 if mx == g
                                   else (r - g) / (mx - mn) + 4)
        grey = mx - mn < 24
        return (a < 255, -a, grey, round(hue * 2) if not grey else 0, 0.299 * r + 0.587 * g + 0.114 * b)
    return [[0, 0, 0, 0]] + [list(c) for c in sorted((tuple(c) for c in cols), key=order)][:255]


def models(cache, out_dir):
    """models/<Part>.json for the page; empty when the preview can't dump meshes."""
    sys.path.insert(0, HERE)
    try:
        import mesh_glb
    except ImportError:
        return []
    index = []
    os.makedirs(os.path.join(out_dir, 'models'), exist_ok=True)
    for part in MODEL_PARTS:
        try:
            mdir = os.path.join(run_preview(part, 'm', cache), 'meshes')
        except subprocess.CalledProcessError:
            print('models: preview failed for', part)
            continue
        if not os.path.isdir(mdir):
            continue
        items = []
        for f in sorted(os.listdir(mdir)):
            if not f.endswith('.json'):
                continue
            m = json.load(open(os.path.join(mdir, f)))
            glb = mesh_glb.to_glb(m)
            items.append({'name': m['name'], 'group': m['group'], 'tris': m['tris'], 'projectile': m.get('projectile', False),
                          'parts': [p['name'] for p in m.get('parts', [])], 'file': f[:-5],
                          'glb': base64.b64encode(glb).decode()})
            index.append({'part': part, 'name': m['name'], 'group': m['group'], 'tris': m['tris'], 'file': f[:-5],
                          'parts': [p['name'] for p in m.get('parts', [])]})
        pal = json.load(open(os.path.join(mdir, os.listdir(mdir)[0])))['palette'] if items else []
        json.dump({'part': part, 'palette': pal, 'models': items}, open(os.path.join(out_dir, 'models', part + '.json'), 'w'))
    return index


def main(argv):
    global CACHE
    out_dir = argv[1]
    CACHE = argv[argv.index('--cache') + 1] if '--cache' in argv else os.path.join(out_dir, '_cache')
    os.makedirs(CACHE, exist_ok=True)
    os.makedirs(out_dir, exist_ok=True)
    sheets, palettes = [], {}
    for part in VESSELS:
        mine = vessel_sheets(part, CACHE)
        palettes[part] = part_palette(mine)
        sheets += mine
    msheets, icons = mode_sheets()
    sheets += msheets
    palettes['RogueWavesMode'] = part_palette(msheets + icons)
    model_index = models(CACHE, out_dir)
    commit = subprocess.run(['git', 'rev-parse', '--short', 'HEAD'], cwd=MOD, capture_output=True, text=True).stdout.strip()
    data = {'sheets': sheets, 'icons': icons, 'palettes': palettes, 'models': model_index, 'commit': commit}
    web = os.path.join(HERE, 'web')
    page = open(os.path.join(web, 'page.html')).read()
    for js in ('ase.js', 'glb.js'):
        p = os.path.join(web, js)
        page = page.replace('/*%s*/' % js, open(p).read() if os.path.exists(p) else '')
    page = page.replace('/*DATA*/null', json.dumps(data, separators=(',', ':')))
    open(os.path.join(out_dir, 'index.html'), 'w').write(page)
    frames = sum(s['w'] // s['cell'] * (s['h'] // s['cell']) for s in sheets)
    print('sheets %d (%d frames), icons %d, models %d, page %d KB' % (len(sheets), frames, len(icons), len(model_index),
                                                                      len(page) // 1024))


if __name__ == '__main__':
    main(sys.argv)
