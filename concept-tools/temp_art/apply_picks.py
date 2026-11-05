"""Applies Dimitri's picks from the chooser page (variants_page.py) to the temporary art in game_size/.

Each pick is per animation, so a sheet can mix versions: the picked version's frames (its rows and columns) are copied
over the same frames of Art/Temporary/game_size/<sheet>.png; "cur" leaves them as they are. Icons and the tuft are
copied whole. The two models are code: their picks print the snippet to paste over the method
(variants/v<n>/models/<Method>.cs.txt). Every sheet or icon it changes gets its .aseprite copy and 8x preview rebuilt
from the result (and the GIFs of the sheets that have them), so the three always match.

    python3 concept-tools/temp_art/apply_picks.py picks.json      # {"yossele-walk": "v2", "icon-RogueWaves_Vote": "v1", ...}
"""
import json
import os
import shutil
import sys
from PIL import Image
from tart import REPO, Canvas, write_sheet, save_png, save_ase, preview, gif

MS = os.path.join(REPO, 'TDS_Mod', 'ModSource')
Y = os.path.join(MS, 'Yossele', 'Art', 'Temporary')
S = os.path.join(MS, 'Spool', 'Art', 'Temporary')
U = os.path.join(MS, 'RogueWavesMode', 'Art', 'Temporary')

# item id -> (folder, sheet, cell, rows, first column, column count)
FRAMES = {
    'yossele-idle': (Y, 'Yossele-Sheet', 32, range(0, 4), 0, 6),
    'yossele-walk': (Y, 'Yossele-Sheet', 32, range(4, 8), 0, 6),
    'yossele-attack': (Y, 'YosseleAttack-Sheet', 32, range(0, 4), 0, 4),
    'cordyfriend-idle': (Y, 'Yosselefriend-Sheet', 24, range(0, 4), 0, 6),
    'cordyfriend-walk': (Y, 'Yosselefriend-Sheet', 24, range(0, 4), 6, 6),
    'cordyfriend-attack': (Y, 'Yosselefriend-Sheet', 24, range(0, 4), 12, 4),
    'threadhead-walk': (S, 'Spoolhead-Sheet', 24, range(0, 4), 0, 6),
    'threadhead-attack': (S, 'Spoolhead-Sheet', 24, range(0, 4), 6, 4),
}
for n in ('stitched', 'dart', 'seam', 'mangled'):
    FRAMES[n + '-walk'] = (S, 'Spool%s-Sheet' % n, 16, range(0, 4), 0, 6)
    FRAMES[n + '-attack'] = (S, 'Spool%s-Sheet' % n, 16, range(0, 4), 6, 4)
MODELS = {'fruit': (Y, 'Fruit', 'TDS_Mod/Assets/YosseleMod/Editor/YosseleModels.cs'),
          'needle': (S, 'Needle', 'TDS_Mod/Assets/SpoolMod/Editor/SpoolModels.cs')}


# sheet -> (cell, columns, tags (name, row, column, count)) for rebuilding its .aseprite copy and preview
LAYOUT = {
    'Yossele-Sheet': (32, 6, [('idle_v%d' % v, v, 0, 6) for v in range(4)] + [('move_v%d' % v, 4 + v, 0, 6) for v in range(4)]),
    'YosseleAttack-Sheet': (32, 4, [('attack_v%d' % v, v, 0, 4) for v in range(4)]),
    'Yosselefriend-Sheet': (24, 16, [t for v in range(4) for t in (('idle_v%d' % v, v, 0, 6), ('move_v%d' % v, v, 6, 6), ('attack_v%d' % v, v, 12, 4))]),
    'Spoolhead-Sheet': (24, 10, [t for v in range(4) for t in (('head_move_v%d' % v, v, 0, 6), ('head_attack_v%d' % v, v, 6, 4))]),
}
for n in ('stitched', 'dart', 'seam', 'mangled'):
    LAYOUT['Spool%s-Sheet' % n] = (16, 10, [t for v in range(4) for t in (('move_v%d' % v, v, 0, 6), ('attack_v%d' % v, v, 6, 4))])
# GIF previews kept beside the sheets: file -> (sheet, first column, count, ms, scale)
GIFS = {
    'Yossele_idle.gif': ('Yossele-Sheet', 0, 6, 125, 6, range(0, 4)), 'Yossele_move.gif': ('Yossele-Sheet', 0, 6, 110, 6, range(4, 8)),
    'Yossele_attack.gif': ('YosseleAttack-Sheet', 0, 4, 110, 6, range(0, 4)),
    'Spoolhead_move.gif': ('Spoolhead-Sheet', 0, 6, 110, 6, range(0, 4)), 'Spoolhead_attack.gif': ('Spoolhead-Sheet', 6, 4, 120, 6, range(0, 4)),
}


def canvas_of(im):
    c = Canvas(im.width, im.height)
    c.c = [tuple(p) for p in list(im.convert('RGBA').getdata())]
    return c


def cells_of(c, cell, cols, rows):
    out = []
    for r in range(rows):
        for k in range(cols):
            f = Canvas(cell, cell)
            for y in range(cell):
                for x in range(cell):
                    f.set(x, y, c.get(k * cell + x, r * cell + y))
            out.append(f)
    return out


def refresh(base, name):
    """Rebuilds <name>.aseprite and its 8x preview (and its GIFs) from game_size/<name>.png."""
    cell, cols, tags = LAYOUT[name]
    c = canvas_of(Image.open(os.path.join(base, 'game_size', name + '.png')))
    cells = cells_of(c, cell, cols, c.h // cell)
    write_sheet(base, name, cells, cols, cell, tags)
    for g, (sheet, c0, n, ms, k, rows) in GIFS.items():
        if sheet == name:
            gif([cells[r * cols + c0:r * cols + c0 + n] for r in rows], os.path.join(base, 'preview_8x', g), k=k, ms=ms)
    if name.startswith('Spool') and name != 'Spoolhead-Sheet':
        crafted = []
        for n in ('stitched', 'dart', 'seam', 'mangled'):
            cc = canvas_of(Image.open(os.path.join(S, 'game_size', 'Spool%s-Sheet.png' % n)))
            crafted.append(cells_of(cc, 16, 10, 1))
        gif([r[0:6] for r in crafted], os.path.join(S, 'preview_8x', 'Spool_crafted_move.gif'), k=8, ms=110)
        gif([r[6:10] for r in crafted], os.path.join(S, 'preview_8x', 'Spool_crafted_attack.gif'), k=8, ms=130)


def refresh_icon(base, name):
    c = canvas_of(Image.open(os.path.join(base, 'game_size', name + '.png')))
    save_png(preview(c, c.w, 8), os.path.join(base, 'preview_8x', name + '_8x.png'))
    if name == 'Spool_Tuft':
        save_ase(os.path.join(base, 'aseprite', 'Spool_Tuft.aseprite'), 12, [c], [('tuft', 0, 0, 'TEMP;icon=Spool_Tuft.png')])
    elif name == 'RogueWaves_Crest32':
        save_ase(os.path.join(base, 'aseprite', 'RogueWaves_Crest32.aseprite'), 32, [c], [('crest', 0, 0, 'TEMP;icon=RogueWaves_Crest32.png')])


def refresh_icon_set():
    """RogueWaves_Icons.aseprite holds every 12 x 12 icon, one frame and tag each, in ui_icons.py's order."""
    from ui_icons import ICONS
    names = list(ICONS)
    frames = [canvas_of(Image.open(os.path.join(U, 'game_size', n + '.png'))) for n in names]
    save_ase(os.path.join(U, 'aseprite', 'RogueWaves_Icons.aseprite'), 12, frames,
             [(n, i, i, 'TEMP;icon=%s.png' % n) for i, n in enumerate(names)])


def apply(picks):
    sheets = {}
    icons = False
    for item, choice in sorted(picks.items()):
        if choice == 'cur':
            continue
        if item in FRAMES:
            base, name, cell, rows, c0, n = FRAMES[item]
            dst = os.path.join(base, 'game_size', name + '.png')
            if dst not in sheets:
                sheets[dst] = Image.open(dst).convert('RGBA')
            src = Image.open(os.path.join(base, 'variants', choice, 'game_size', name + '.png')).convert('RGBA')
            for r in rows:
                box = (c0 * cell, r * cell, (c0 + n) * cell, (r + 1) * cell)
                sheets[dst].paste(src.crop(box), box[:2])
            print('%s: %s frames from %s' % (name, item, choice))
        elif item in MODELS:
            base, method, where = MODELS[item]
            print('%s: paste %s over %s in %s' % (item, os.path.join(base, 'variants', choice, 'models', method + '.cs.txt'), method, where))
        elif item == 'tuft' or item.startswith('icon-'):
            base, name = (S, 'Spool_Tuft') if item == 'tuft' else (U, item[5:])
            shutil.copy(os.path.join(base, 'variants', choice, 'game_size', name + '.png'), os.path.join(base, 'game_size', name + '.png'))
            print('%s.png: %s' % (name, choice))
            refresh_icon(base, name)
            icons = icons or base == U
            if name in ('RogueWaves_RisingWater', 'RogueWaves_Quickening', 'RogueWaves_Nesting'):
                print('  wired as rows in RogueWavesMode/Code/08_Icons.cs: copy the rows variants_icons.py prints for %s' % choice)
        else:
            print('unknown item %s' % item)
    for dst, im in sheets.items():
        im.save(dst)
        base = os.path.dirname(os.path.dirname(dst))
        refresh(base, os.path.basename(dst)[:-4])
    if icons:
        refresh_icon_set()
    # The versions under variants/ keep the purple they were drawn in; Spool is sage since 2026-11-05 (spool_sage.py).
    if any(d.startswith(S) for d in sheets) or picks.get('tuft', 'cur') != 'cur':
        import spool_sage
        spool_sage.main()


if __name__ == '__main__':
    apply(json.load(open(sys.argv[1])))
