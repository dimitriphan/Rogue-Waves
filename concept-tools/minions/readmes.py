"""Writes the README of each part's minions_2026-11-04 folder and of the project-files folder from index.json."""
import json
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from mlib import REPO  # noqa: E402

PF = '/mnt/project-files/concepts/minions_2026-11-04'
PAGE = sys.argv[1] if len(sys.argv) > 1 else '(picks page: see docs/TEMP_ART.md)'
idx = json.load(open(os.path.join(PF, 'index.json')))

HOW = {
    'Yossele': 'A pick is a copy: put the picked `game_size/Yosselefriend-Sheet.png` over `Art/Temporary/game_size/` '
               '(the wired file, `YosseleBuilder.CopySheet`) and its `.aseprite` over `aseprite/`, then Build All Assets.',
    'Spool': 'A pick is a copy: put the picked `game_size/<sheet>.png` over `Art/Temporary/game_size/` (the wired files, '
             '`SpoolBuilder` `FileArt`) and its `.aseprite` over `aseprite/`, then rebuild Spool\'s assets.',
    'Uloe': 'Not wired: `Uloephoton-Sheet` is drawn by code (`UloeCharacter.PhotonSheet`). `UloeBuilder.SaveSheet` '
            'already takes a file over it (`FileArt`: `Art/<name>.png`, else `Art/Temporary/game_size/<name>.png`, when it '
            'has the code sheet\'s size), so a pick only needs the picked sheet copied to `Art/Temporary/game_size/`. Same '
            'name, size and layout as the code\'s sheet (160 x 64).',
    'Argo': 'Not wired: the six Naut sheets are drawn by code (`ArgoCharacter.NautSheet`, `NautTuneSheet`) and ArgoBuilder '
            'has no file override yet. A pick needs Yossele\'s `FileArt` in `ArgoBuilder.SaveSheet`, then the six picked '
            'sheets copied to `Art/Temporary/game_size/`. Same names, sizes and layout as the code\'s sheets (96 x 64 each). '
            'Argo himself keeps his shipped look (Dimitri, 2026-11-04 05:31Z); only his crew is redrawn here.',
}

for part in ('Yossele', 'Spool', 'Uloe', 'Argo'):
    ms = [m for m in idx if m['part'] == part]
    lines = ['# %s: minion versions to pick from (2026-11-04)' % part, '',
             'Dimitri, 2026-11-04: "Create new sprites for all modded minions. They should be closer in size to snobles. '
             'Create at least 10 versions of each." **Everything here is temporary and nothing is wired**: the art in game '
             'stays as it is until he picks (`docs/TEMP_ART.md`, "Minion versions"). Picks page: %s.' % PAGE, '',
             'Size: the Snoble is 6-8 px wide and 5-8 px tall in a 12 px cell (measured on the game\'s sheet, kit '
             '`docs/18` section 2). Every version keeps the wired sheet\'s name, size and frame layout; only the body in '
             'each cell is smaller. Drawn by `concept-tools/minions/` (`%s.py`; `build.py` writes this folder).'
             % part.lower(), '',
             '%s' % HOW[part], '',
             'Each `<minion>/vNN/` has `game_size/` (the sheet), `aseprite/` (one frame per cell, a tag per row and '
             'animation, layer "TEMP art") and `preview_8x/` (on a checker). GIFs of every version are in the project files, '
             '`/mnt/project-files/concepts/minions_2026-11-04/`.', '']
    for m in ms:
        cur = m.get('current')
        lines += ['## %s (`%s/`)' % (m['name'], m['key']), '',
                  'Today: %s. Sheets: %s.' % ('%d x %d px at rest' % (cur['w'], cur['h']) if cur else 'drawn by code',
                                              ', '.join('`%s`' % s for s in m['versions'][0]['sheets'])), '',
                  '| Version | Name | Size at rest | Look |', '| --- | --- | --- | --- |']
        for v in m['versions']:
            lines.append('| %s | %s | %d x %d | %s |' % (v['id'], v['name'], v['w'], v['h'], v['pitch']))
        lines.append('')
    out = os.path.join(REPO, 'TDS_Mod', 'ModSource', part, 'Art', 'Temporary', 'minions_2026-11-04', 'README.md')
    open(out, 'w').write('\n'.join(lines))

top = ['# Minion versions, 2026-11-04', '',
       'Dimitri: "Create new sprites for all modded minions. They should be closer in size to snobles. Create at least 10 '
       'versions of each." Ten versions of each of the eight minions the pack adds, Snoble-sized, none wired until he picks.',
       '', 'Picks page: %s (built from `minion_picks.html` here by `concept-tools/minions/page.py`; picks save in its '
       'db collection `picks`, one document per minion key, `{choice: "cur" | "v01".."v10", note}`).' % PAGE, '',
       'Counted: Yossele\'s Cordyfriend; Spool\'s Thread Head and four Crafted (Stitched, Dart, Seam, Mangled); Uloe\'s '
       'Photon; Argo\'s Naut (each version in Port, Starboard and Bosun colours). Not counted: the RogueWavesFoes part '
       'adds no bodies (its four modifiers are rings over stock foes), VENDCINT is a shop, not a minion.', '',
       'Each `<minion>/vNN_<Name>/` holds `game_size/`, `aseprite/`, `preview_8x/` (the same files as the repository\'s '
       '`TDS_Mod/ModSource/<Part>/Art/Temporary/minions_2026-11-04/<minion>/vNN/`) and `gifs/` (walk, idle, attack or '
       'tune, back, at 8x on the grass colour). `<minion>/current/` has GIFs of the art in game today; '
       '`<minion>/<minion>_lineup_3x.png` is the current art and the ten versions at game size, 3x. `index.json` lists '
       'every version with its size and look.', '', '| Minion | Part | Today | New (h at rest) |', '| --- | --- | --- | --- |']
for m in idx:
    cur = m.get('current')
    hs = sorted(v['h'] for v in m['versions'])
    top.append('| %s | %s | %s | %d-%d px |' % (m['name'], m['part'], '%d x %d' % (cur['w'], cur['h']) if cur else '-', hs[0], hs[-1]))
open(os.path.join(PF, 'README.md'), 'w').write('\n'.join(top) + '\n')
print('readmes written')
