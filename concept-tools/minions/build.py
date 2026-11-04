"""Writes every minion version (2026-11-04): the sheets into the repository and the review copies, GIFs and line-ups
into the project files, plus a JSON index the picks page reads.

    python3 concept-tools/minions/build.py [--current <dir with the code-drawn Uloephoton/Naut sheets>]

Repository: TDS_Mod/ModSource/<Part>/Art/Temporary/minions_2026-11-04/<minion>/vNN/{game_size,aseprite,preview_8x}/
Project files: /mnt/project-files/concepts/minions_2026-11-04/<minion>/vNN_<slug>/ (the same three folders and gifs/).
"""
import json
import os
import shutil
import sys

from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
from mlib import Canvas, pose, opaque, bbox, frames, write, LAYOUTS, REPO  # noqa: E402
import yossele, spool, uloe, argo  # noqa: E402,E401

PF = '/mnt/project-files/concepts/minions_2026-11-04'
STAGE = (140, 155, 140)
K = 8


def slug(name):
    return ''.join(ch for ch in name.title() if ch.isalnum())


def pitch(fn):
    d = (fn.__doc__ or '').strip()
    d = d.split(':', 1)[1].strip() if ':' in d else d
    return d[:1].upper() + d[1:]


def crop_box(cell):
    return (2, 2, 22, 24) if cell == 24 else (0, 0, 16, 16)


def to_img(cv, cell):
    im = Image.frombytes('RGBA', (cv.w, cv.h), cv.rgba()).crop(crop_box(cell))
    im = im.resize((im.width * K, im.height * K), Image.NEAREST)
    o = Image.new('RGB', im.size, STAGE)
    o.paste(im, (0, 0), im)
    return o


def save_gif(cells, cell, path, ms=125):
    ims = [to_img(c, cell) for c in cells]
    os.makedirs(os.path.dirname(path), exist_ok=True)
    ims[0].save(path, save_all=True, append_images=ims[1:], duration=ms, loop=0, disposal=2)


def segment(cells, L, kind, row=0):
    """The cells of one animation of one row (v0 = front heading right, v2 = back heading right)."""
    col = 0
    for k, n in L['segs']:
        if k == kind:
            return cells[row * L['cols'] + col: row * L['cols'] + col + n]
        col += n
    return []


def stats(cells, L):
    boxes = [bbox(c) for c in segment(cells, L, 'move') + segment(cells, L, 'idle') if bbox(c)]
    rest = bbox(segment(cells, L, 'idle')[0] if segment(cells, L, 'idle') else segment(cells, L, 'move')[0])
    cols = set()
    for c in cells:
        cols |= set(c.colours())
    return dict(w=rest[2] - rest[0], h=rest[3] - rest[1], hmax=max(b[3] - b[1] for b in boxes), colours=len(cols))


def build_minion(M, index):
    part = M['part']
    repo_base = os.path.join(REPO, 'TDS_Mod', 'ModSource', part, 'Art', 'Temporary', 'minions_2026-11-04', M['key'])
    entry = dict(key=M['key'], name=M['name'], part=part, layout=M['layout'], cell=LAYOUTS[M['layout']]['cell'],
                 versions=[])
    crews = M.get('crews', [None])
    for n, (name, fn, gait) in enumerate(M['versions'], 1):
        vid = 'v%02d' % n
        rb = os.path.join(repo_base, vid)
        pb = os.path.join(PF, M['key'], '%s_%s' % (vid, slug(name)))
        sheets, gifs = [], {}
        for crew in crews:
            kw = {'crew': crew} if crew else {}
            layouts = [(M['layout'], M['sheet'] if not crew else 'Naut_%s-Sheet' % crew)]
            if crew:
                layouts.append(('nauttune', 'NautTune_%s-Sheet' % crew))
            for layout, sheet_name in layouts:
                cells, tags, L = frames(fn, layout, gait, **kw)
                write(rb, sheet_name, cells, tags, L)
                sheets.append(sheet_name)
                if crew in (None, crews[0]):
                    st = stats(cells, L) if layout != 'nauttune' else None
                    if st:
                        entry_stats = st
                for kind, _ in L['segs']:
                    if crew not in (None, crews[0]) and kind != 'move':
                        continue
                    g = os.path.join(pb, 'gifs', '%s_%s%s.gif' % (M['key'], kind, '_' + crew if crew else ''))
                    save_gif(segment(cells, L, kind), L['cell'], g, 125 if kind != 'attack' else 140)
                    gifs['%s%s' % (kind, '_' + crew if crew and crew != crews[0] else '')] = os.path.relpath(g, PF)
                if layout == M['layout'] and crew in (None, crews[0]):
                    back = to_img(segment(cells, L, 'move', 2)[0], L['cell'])
                    bp = os.path.join(pb, 'gifs', '%s_back.png' % M['key'])
                    back.save(bp)
                    gifs['back'] = os.path.relpath(bp, PF)
        for sub in ('game_size', 'aseprite', 'preview_8x'):
            dst = os.path.join(pb, sub)
            if os.path.exists(dst):
                shutil.rmtree(dst)
            shutil.copytree(os.path.join(rb, sub), dst)
        entry['versions'].append(dict(id=vid, name=name, pitch=pitch(fn), gait=gait, sheets=sheets, gifs=gifs,
                                      repo=os.path.relpath(rb, REPO), **entry_stats))
    index.append(entry)


def lineup(entry, mods, current, out):
    """Game size, shown 3x: the current art first, then the ten versions, each at its resting frame (front)."""
    M = [m for mod in mods for m in mod.MINIONS if m['key'] == entry['key']][0]
    cell = entry['cell']
    crop = crop_box(cell)
    w, h = crop[2] - crop[0], crop[3] - crop[1]
    tiles = []
    if current is not None:
        tiles.append(current.crop(crop))
    for name, fn, gait in M['versions']:
        cv = Canvas(cell, cell)
        kw = {'crew': M['crews'][0]} if 'crews' in M else {}
        fn(cv, pose('idle' if M['layout'] == 'cordy' else 'move', 0, True, cell, gait), **kw)
        opaque(cv)
        tiles.append(Image.frombytes('RGBA', (cell, cell), cv.rgba()).crop(crop))
    im = Image.new('RGBA', (len(tiles) * (w + 2), h), STAGE + (255,))
    for k, t in enumerate(tiles):
        im.alpha_composite(t, (k * (w + 2), 0))
    im = im.resize((im.width * 3, im.height * 3), Image.NEAREST)
    im.save(out)


def current_sheets(cur_dir):
    S = os.path.join(REPO, 'TDS_Mod', 'ModSource')
    return {
        'cordyfriend': os.path.join(S, 'Yossele', 'Art', 'Temporary', 'game_size', 'Yosselefriend-Sheet.png'),
        'threadhead': os.path.join(S, 'Spool', 'Art', 'Temporary', 'game_size', 'Spoolhead-Sheet.png'),
        'stitched': os.path.join(S, 'Spool', 'Art', 'Temporary', 'game_size', 'Spoolstitched-Sheet.png'),
        'dart': os.path.join(S, 'Spool', 'Art', 'Temporary', 'game_size', 'Spooldart-Sheet.png'),
        'seam': os.path.join(S, 'Spool', 'Art', 'Temporary', 'game_size', 'Spoolseam-Sheet.png'),
        'mangled': os.path.join(S, 'Spool', 'Art', 'Temporary', 'game_size', 'Spoolmangled-Sheet.png'),
        'photon': os.path.join(cur_dir, 'Uloephoton-Sheet.png') if cur_dir else None,
        'naut': os.path.join(cur_dir, 'Naut_Port-Sheet.png') if cur_dir else None,
    }


CUR_LAYOUT = {  # how the current sheets are laid out: (cell, cols, {kind: (first col, count)})
    'cordyfriend': (24, 16, {'idle': (0, 6), 'move': (6, 6), 'attack': (12, 4)}),
    'threadhead': (24, 10, {'move': (0, 6), 'attack': (6, 4)}),
    'stitched': (16, 10, {'move': (0, 6), 'attack': (6, 4)}),
    'dart': (16, 10, {'move': (0, 6), 'attack': (6, 4)}),
    'seam': (16, 10, {'move': (0, 6), 'attack': (6, 4)}),
    'mangled': (16, 10, {'move': (0, 6), 'attack': (6, 4)}),
    'photon': (16, 10, {'move': (0, 6), 'attack': (6, 4)}),
    'naut': (16, 6, {'move': (0, 6)}),
}


def current_entry(key, path, cur_dir):
    """GIFs and the size of the art in game today, for the page (written to the project files only)."""
    if not path or not os.path.exists(path):
        return None, None
    cell, cols, segs = CUR_LAYOUT[key]
    im = Image.open(path).convert('RGBA')
    out = os.path.join(PF, key, 'current')
    os.makedirs(out, exist_ok=True)
    gifs = {}

    def cellimg(r, c):
        return im.crop((c * cell, r * cell, (c + 1) * cell, (r + 1) * cell))

    def staged(t):
        t = t.crop(crop_box(cell)).resize(((crop_box(cell)[2] - crop_box(cell)[0]) * K, (crop_box(cell)[3] - crop_box(cell)[1]) * K), Image.NEAREST)
        o = Image.new('RGB', t.size, STAGE)
        o.paste(t, (0, 0), t)
        return o
    for kind, (c0, n) in segs.items():
        fr = [staged(cellimg(0, c0 + i)) for i in range(n)]
        g = os.path.join(out, '%s_current_%s.gif' % (key, kind))
        fr[0].save(g, save_all=True, append_images=fr[1:], duration=125, loop=0, disposal=2)
        gifs[kind] = os.path.relpath(g, PF)
    if key == 'naut' and cur_dir:
        t = Image.open(os.path.join(cur_dir, 'NautTune_Port-Sheet.png')).convert('RGBA')
        fr = [staged(t.crop((i * 16, 0, (i + 1) * 16, 16))) for i in range(6)]
        g = os.path.join(out, 'naut_current_tune.gif')
        fr[0].save(g, save_all=True, append_images=fr[1:], duration=125, loop=0, disposal=2)
        gifs['tune'] = os.path.relpath(g, PF)
    bp = os.path.join(out, '%s_current_back.png' % key)
    staged(cellimg(2, segs.get('idle', segs['move'])[0])).save(bp)
    gifs['back'] = os.path.relpath(bp, PF)
    rest = cellimg(0, segs.get('idle', segs['move'])[0])
    b = rest.getchannel('A').getbbox()
    cols = {px for px in list(im.getdata()) if px[3]}
    return dict(id='cur', name='Current', gifs=gifs, w=b[2] - b[0], h=b[3] - b[1], colours=len(cols)), rest


def main():
    cur_dir = None
    if '--current' in sys.argv:
        cur_dir = sys.argv[sys.argv.index('--current') + 1]
    mods = [yossele, spool, uloe, argo]
    index = []
    for mod in mods:
        for M in mod.MINIONS:
            build_minion(M, index)
            print('built', M['name'])
    cur = current_sheets(cur_dir)
    for e in index:
        ce, rest = current_entry(e['key'], cur[e['key']], cur_dir)
        e['current'] = ce
        if rest is not None:
            big = Image.new('RGBA', (e['cell'], e['cell']), (0, 0, 0, 0))
            big.alpha_composite(rest, (0, 0))
        lp = os.path.join(PF, e['key'], '%s_lineup_3x.png' % e['key'])
        lineup(e, mods, big if rest is not None else None, lp)
        e['lineup'] = os.path.relpath(lp, PF)
    with open(os.path.join(PF, 'index.json'), 'w') as f:
        json.dump(index, f, indent=1)
    print('index', len(index), 'minions')


if __name__ == '__main__':
    main()
