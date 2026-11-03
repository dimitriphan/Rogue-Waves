"""Applies Dimitri's picks from the chooser page (variants_page.py) to the temporary art in game_size/.

Each pick is per animation, so a sheet can mix versions: the picked version's frames (its rows and columns) are copied
over the same frames of Art/Temporary/game_size/<sheet>.png; "cur" leaves them as they are. Icons and the tuft are
copied whole. The two models are code: their picks print the snippet to paste over the method
(variants/v<n>/models/<Method>.cs.txt). The .aseprite copies are not merged; re-export them after a mixed pick.

    python3 concept-tools/temp_art/apply_picks.py picks.json      # {"yossele-walk": "v2", "icon-RogueWaves_Vote": "v1", ...}
"""
import json
import os
import shutil
import sys
from PIL import Image
from tart import REPO

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
          'needle': (S, 'Needle', 'TDS_Mod/Assets/SpoolMod/Editor/SpoolModels.cs (PR #42)')}


def apply(picks):
    sheets = {}
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
            if name in ('RogueWaves_RisingWater', 'RogueWaves_Quickening', 'RogueWaves_Nesting'):
                print('  wired as rows in RogueWavesMode/Code/08_Icons.cs: copy the rows variants_icons.py prints for %s' % choice)
        else:
            print('unknown item %s' % item)
    for dst, im in sheets.items():
        im.save(dst)


if __name__ == '__main__':
    apply(json.load(open(sys.argv[1])))
