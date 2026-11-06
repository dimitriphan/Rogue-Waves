#!/usr/bin/env python3
"""Brings the Art Bench's waiting edits into ModSource (README.md, "Bringing edits in").

    python3 TDS_Mod/Tools/artbench/ingest.py <edits.json> <assets_dir> [--cache <dir>] [--dry]

<edits.json>: the page's `edits` collection as a list of documents (ArtifactData list; each {id, ...fields} or
{id, data: {...}}). <assets_dir>: each edit's asset saved as <asset id>.png (a sprite row) or <asset id>.json (a model).
Only edits with status "waiting" are applied; prints what it wrote, one line per edit, and the ids it applied.

Sprite rows. Only the frames the page marked changed are taken; the row's other frames keep an earlier hand edit, else the sheet as
it is now.
- route "merge" (sheets <Part>Aseprite.Apply merges): the tags of that row go into
  ModSource/<Part>/Aseprite/<sheet>.aseprite with gen= set to the CRCs of the sheet as the builder sees it before hand
  edits, so the builder swaps in exactly the changed frames. A facing drawn as a mirror of an edited tag (v1 of v0,
  v3 of v2) is added unedited with mirror=, so it follows the edit, unless the file already holds that tag.
- route "file": the changed frames are pasted into the file the build reads (Art/ final, else Art/Temporary/
  game_size/); a sheet drawn by code becomes ModSource/<Part>/Art/<sheet>.png.
Models: the .glb becomes ModSource/<Part>/Art/Models/<file>.json (mesh_glb.from_glb, colours snapped to the palette).
"""
import base64, json, os, sys, zlib

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import build  # noqa: E402
A = build.A
REPO = os.path.dirname(build.MOD)
KIND = {'idle': (106, 190, 48), 'move': (91, 110, 225), 'attack': (217, 87, 99), 'ability': (178, 102, 216),
        'fall': (223, 113, 38), 'land': (238, 195, 154), 'mini': (95, 205, 228), 'unit': (55, 148, 110), 'ui': (155, 173, 183)}


def frames_of(w, h, px, cell, row, col, n):
    return [A.crop(w, h, px, (col + k) * cell, row * cell, cell, cell) for k in range(n)]


def paste(w, px, cell, row, col, frame):
    px = bytearray(px)
    for y in range(cell):
        o = ((row * cell + y) * w + col * cell) * 4
        px[o:o + cell * 4] = frame[y * cell * 4:(y + 1) * cell * 4]
    return bytes(px)


def read_existing(path):
    """{tag: [frames, data, rgb]} kept in order from a hand-edit file."""
    if not os.path.exists(path):
        return {}
    ase = A.read_ase(path)
    return {t['name']: [[A.flatten(ase, i) for i in range(t['from'], t['to'] + 1)], t['data'], t.get('color', (155, 173, 183))]
            for t in ase['tags']}


def sprite(e, asset, cache, dry):
    part, name, cell, row = e['part'], e['sheet'], e['cell'], e['row']
    sw, sh, strip = A.png_read(asset)
    n = sum(t['count'] for t in e['tags'])
    if (sw, sh) != (n * cell, cell):
        return 'skipped: the row image is %dx%d, expected %dx%d' % (sw, sh, n * cell, cell)
    mine = [A.crop(sw, sh, strip, k * cell, 0, cell, cell) for k in range(n)]
    changed = set(e.get('changed') or [])
    if part == 'RogueWavesMode':
        path = os.path.join(REPO, e['target'])
        w, h, px = A.png_read(path)
        origin, is_code = 'temporary', False
    else:
        w, h, px, origin, path, is_code = build.base_sheet(part, name, cache)
    if (w, h) != (e['cols'] * cell, e['rows'] * cell):
        return 'skipped: the sheet is now %dx%d, the edit was made on %dx%d' % (w, h, e['cols'] * cell, e['rows'] * cell)
    base = frames_of(w, h, px, cell, row, 0, n)
    new = [mine[k] if k in changed else base[k] for k in range(n)]

    if e['route'] == 'file':
        if part != 'RogueWavesMode' and origin == 'code':
            path = os.path.join(build.MOD, 'ModSource', part, 'Art', name + '.png')
        for k in changed:
            px = paste(w, px, cell, row, k, new[k])
        if not dry:
            A.png_write(path, w, h, px)
        return 'wrote %d frame(s) into %s' % (len(changed), os.path.relpath(path, REPO))

    lay = A.layout(part, name, w, h, [])
    if not lay:
        return 'skipped: %s has no merge layout any more' % name
    folder, _, _, rows = lay
    target = os.path.join(build.MOD, 'ModSource', part, 'Aseprite', name + '.aseprite')
    tags = read_existing(target)
    # A frame not changed this time keeps an earlier hand edit of it (the page diffed against the merged row);
    # a frame the file holds unedited follows the code as it is now.
    col = 0
    for t in e['tags']:
        kept = tags.get(t['name'])
        if kept and len(kept[0]) == t['count']:
            old = dict(p.split('=', 1) for p in kept[1].split(';') if '=' in p).get('gen', '').split(',')
            for k in range(t['count']):  # only frames Apply already treats as edited (CRC differs from the old gen)
                if col + k not in changed and (k >= len(old) or '%08x' % zlib.crc32(kept[0][k]) != old[k]):
                    new[col + k] = kept[0][k]
        col += t['count']

    def data(r, col, count, mirror=None):
        gen = ','.join('%08x' % zlib.crc32(f) for f in frames_of(w, h, px, cell, r, col, count))
        return 'sheet=%s/%s.png;row=%d;col=%d;cols=%d;rows=%d;gen=%s' % (folder, name, r, col, w // cell, h // cell, gen) + \
            (';mirror=' + mirror if mirror else '')
    col = 0
    edited = {}
    for t in e['tags']:
        rgb = KIND.get(next((k for tn, c, k in rows[row] if tn == t['name']), 'ui'), KIND['ui'])
        tags[t['name']] = [new[col:col + t['count']], data(row, col, t['count'], t.get('mirrorOf')), rgb]
        edited[t['name']] = base[col:col + t['count']]
        col += t['count']
    # facings drawn as mirrors of an edited tag follow it
    for r, rtags in enumerate(rows):
        c = 0
        for tn, count, kind in rtags:
            src = tn[:-1] + ('0' if tn.endswith('_v1') else '2') if tn.endswith(('_v1', '_v3')) else None
            if src in edited and tn not in tags and all(
                    a == A.flip_x(b, cell) for a, b in zip(frames_of(w, h, px, cell, r, c, count), edited[src])):
                tags[tn] = [frames_of(w, h, px, cell, r, c, count), data(r, c, count, src), KIND.get(kind, KIND['ui'])]
            c += count
    frames, out = [], []
    for tn, (fr, d, rgb) in tags.items():
        out.append((tn, len(frames), len(frames) + len(fr) - 1, tuple(rgb)[:3], d))
        frames += fr
    pal = sorted({tuple(f[i:i + 4]) for f in frames for i in range(0, len(f), 4) if f[i + 3]})[:255]
    if not dry:
        os.makedirs(os.path.dirname(target), exist_ok=True)
        A.write_ase(target, cell, cell, frames, out, [(0, 0, 0, 0)] + pal, '%s hand edits' % name)
    return 'wrote %d changed frame(s) of %s into %s (%d tags)' % (len(changed), '+'.join(edited), os.path.relpath(target, REPO), len(out))


def model(e, asset, cache, dry):
    import mesh_glb
    src_path = os.path.join(build.run_preview(e['part'], 'm', cache), 'meshes', e['file'] + '.json')
    if not os.path.exists(src_path):
        return 'skipped: the code no longer makes %s' % e['file']
    src = json.load(open(src_path))
    mesh = mesh_glb.from_glb(base64.b64decode(json.load(open(asset))['glb_base64']), src['palette'],
                             [p['name'] for p in src.get('parts', [])])
    r = mesh.pop('report')
    if not r['extras']:  # Blender drops extras: keep the source's group, projectile and scale
        mesh.update({k: src[k] for k in ('group', 'projectile', 'scale') if k in src})
    lost = [p['name'] for p in src.get('parts', []) if p['name'] not in [q['name'] for q in mesh.get('parts', [])]]
    if lost:
        return 'skipped: moving part(s) missing: ' + ', '.join(lost)
    path = os.path.join(build.MOD, 'ModSource', e['part'], 'Art', 'Models', e['file'] + '.json')
    if not dry:
        os.makedirs(os.path.dirname(path), exist_ok=True)
        json.dump(mesh, open(path, 'w'), separators=(',', ':'))
    return 'wrote %s (%d triangles, was %d)' % (os.path.relpath(path, REPO), mesh['tris'], src['tris'])


def main(argv):
    docs = json.load(open(argv[1]))
    docs = docs.get('documents', docs) if isinstance(docs, dict) else docs
    assets, dry = argv[2], '--dry' in argv
    cache = argv[argv.index('--cache') + 1] if '--cache' in argv else os.path.join(assets, '_cache')
    os.makedirs(cache, exist_ok=True)
    applied = []
    for d in sorted(docs, key=lambda d: (d.get('data', d).get('at') or '')):
        e = dict(d.get('data', d), id=d.get('id'))
        if e.get('status') != 'waiting':
            continue
        ext = '.png' if e['kind'] == 'sprite' else '.json'
        asset = os.path.join(assets, e['asset'] + ext)
        if not os.path.exists(asset):
            print(e['id'], 'skipped: asset not downloaded:', asset)
            continue
        msg = sprite(e, asset, cache, dry) if e['kind'] == 'sprite' else model(e, asset, cache, dry)
        print(e['id'], e['part'], e.get('sheet') or e.get('model'), '-', msg)
        if not msg.startswith('skipped'):
            applied.append(e['id'])
    print('applied:', ' '.join(applied) or 'none')


if __name__ == '__main__':
    main(sys.argv)
