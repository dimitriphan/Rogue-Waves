"""Builds the chooser page for the temporary art versions (2026-10-28): every temporary asset and every animation, the
current art beside its three versions, animated side by side, with a Pick button per version. Picks are kept in the
page's shared store (the Artifact `db` capability, collection `picks`, one document per item: {choice, at}) so a Claude
thread can read them back; without the store they stay in the viewer's browser and a Copy button gives them as text.

    python3 concept-tools/temp_art/variants_page.py <out.html>
Reads the sheets from TDS_Mod/ModSource/<Part>/Art/Temporary/ (game_size/ = current, variants/v1..v3/ = versions).
"""
import base64
import io
import json
import os
import sys
from PIL import Image
from tart import REPO
import variants_yossele as VY
import variants_spool as VS
import variants_icons as VI
import variants_models as VM

MS = os.path.join(REPO, 'TDS_Mod', 'ModSource')
PARTS = {'Yossele': os.path.join(MS, 'Yossele', 'Art', 'Temporary'),
         'Spool': os.path.join(MS, 'Spool', 'Art', 'Temporary'),
         'UI': os.path.join(MS, 'RogueWavesMode', 'Art', 'Temporary')}
IMG = {}


def data(path, crop=None):
    key = 'i%d' % len(IMG)
    if crop:
        im = Image.open(path).convert('RGBA').crop(crop)
        b = io.BytesIO()
        im.save(b, 'PNG', optimize=True)
        raw = b.getvalue()
    else:
        raw = open(path, 'rb').read()
    IMG[key] = 'data:image/png;base64,' + base64.b64encode(raw).decode()
    return key


def versions(part, rel, crop=None, crop_current=None):
    base = PARTS[part]
    out = {'cur': data(os.path.join(base, rel.replace('{v}/', '')), crop_current or crop)}
    for v in (1, 2, 3):
        out['v%d' % v] = data(os.path.join(base, rel.replace('{v}/', 'variants/v%d/' % v)), crop)
    return out


def anim(id, part, group, title, file, fills, sheet, cell, cols, rows, start, n, ms, names, scale, hold=0):
    return dict(id=id, part=part, group=group, title=title, file=file, fills=fills, kind='anim', sheets=sheet, cell=cell,
                cols=cols, rows=rows, start=start, n=n, ms=ms, hold=hold, names=list(names), scale=scale)


def still(id, part, group, title, file, fills, imgs, w, h, names, scale, kind='still'):
    return dict(id=id, part=part, group=group, title=title, file=file, fills=fills, kind=kind, sheets=imgs, w=w, h=h,
                names=list(names), scale=scale)


def items():
    out = []
    # ---------------------------------------------------------------- Yossele
    ys = versions('Yossele', '{v}/game_size/Yossele-Sheet.png')
    ya = versions('Yossele', '{v}/game_size/YosseleAttack-Sheet.png')
    yf = versions('Yossele', '{v}/game_size/Yosselefriend-Sheet.png')
    n1 = VY.NAMES['Yossele-Sheet']
    out.append(anim('yossele-idle', 'Yossele', 'Yossele', 'Idle', 'Yossele-Sheet.png, rows 0-3',
                    'The vessel standing still. Every version moves only parts of the code\'s frame drawn from your reshade.',
                    ys, 32, 6, [0, 1, 2, 3], 0, 6, 130, [n.split(' and ')[0] for n in n1], 5))
    out.append(anim('yossele-walk', 'Yossele', 'Yossele', 'Walk', 'Yossele-Sheet.png, rows 4-7',
                    'The Move rows. The current one is the knuckle walk from 2026-10-12.',
                    ys, 32, 6, [4, 5, 6, 7], 0, 6, 110, [n.split(' and ')[1].capitalize() for n in n1], 5))
    out.append(anim('yossele-attack', 'Yossele', 'Yossele', 'Mouth laser attack', 'YosseleAttack-Sheet.png',
                    'Four beats per view: charge, aim, fire, recover.',
                    ya, 32, 4, [0, 1, 2, 3], 0, 4, 140, VY.NAMES['YosseleAttack-Sheet'], 5, hold=2))
    fn = VY.NAMES['Yosselefriend-Sheet']
    for k, (nm, st, cnt, ms) in enumerate((('Idle', 0, 6, 140), ('Walk', 6, 6, 110), ('Attack', 12, 4, 140))):
        out.append(anim('cordyfriend-' + nm.lower(), 'Yossele', 'Cordyfriend', nm, 'Yosselefriend-Sheet.png, columns %d-%d' % (st, st + cnt - 1),
                        'Cordyceps\' minion, a puffball like a Snoble with orange clubs. Each version is its own build of it, so its idle, walk and attack belong together but can be mixed.' if k == 0 else
                        ('Its walk.' if k == 1 else 'Its attack.'),
                        yf, 24, 16, [0, 1, 2, 3], st, cnt, ms, ['%s: %s' % (n, ['idle', 'walk', 'attack'][k]) for n in fn], 6, hold=2 if k == 2 else 0))
    fr = versions('Yossele', '{v}/preview_8x/models_Fruit.png', crop_current=(0, 0, 1024, 256))
    out.append(still('fruit', 'Yossele', 'Aura fruit', 'Aura fruit model', 'YosseleModels.Fruit (3D model, render)',
                     'Mixed, red, blue and yellow. A version is a C# method pasted over Fruit.',
                     fr, 1024, 256, VM.NAMES['Fruit'], 0.5, kind='model'))
    # ---------------------------------------------------------------- Spool
    sh = versions('Spool', '{v}/game_size/Spoolhead-Sheet.png')
    hn = VS.NAMES['Spoolhead-Sheet']
    out.append(anim('threadhead-walk', 'Spool', 'Thread Head', 'Walk', 'Spoolhead-Sheet.png, columns 0-5',
                    'The yarn ball wearing your zipper mouth, pixel for pixel. Column 0 is also its idle.',
                    sh, 24, 10, [0, 1, 2, 3], 0, 6, 110, [n.split(', ')[0] + ', ' + n.split(', ')[1].split(' and ')[0] for n in hn], 6))
    out.append(anim('threadhead-attack', 'Spool', 'Thread Head', 'Attack', 'Spoolhead-Sheet.png, columns 6-9',
                    'Its punch.', sh, 24, 10, [0, 1, 2, 3], 6, 4, 140, [n.split(' and ')[-1].capitalize() for n in hn], 6, hold=2))
    cn = VS.NAMES['Crafted']
    darts = ('High lob', 'Sidearm fling', 'Stab then throw')
    for name, fills in (('Stitched', 'Spool in small, from your sprite.'), ('Dart', 'The thin one with a needle.'),
                        ('Seam', 'Sewn from white felt.'), ('Mangled', 'A torn scrap on spilled cotton.')):
        s = versions('Spool', '{v}/game_size/Spool%s-Sheet.png' % name.lower())
        out.append(anim(name.lower() + '-walk', 'Spool', name, 'Walk', 'Spool%s-Sheet.png, columns 0-5' % name.lower(),
                        fills + ' The bodies are the current ones; the versions change how they move.',
                        s, 16, 10, [0, 1, 2, 3], 0, 6, 110, [n.split(' and ')[0] for n in cn], 9))
        out.append(anim(name.lower() + '-attack', 'Spool', name, 'Attack', 'Spool%s-Sheet.png, columns 6-9' % name.lower(),
                        'Wind-up, strike, recover.', s, 16, 10, [0, 1, 2, 3], 6, 4, 140,
                        list(darts) if name == 'Dart' else [n.split(' and ')[1].capitalize() for n in cn], 9, hold=2))
    out.append(still('tuft', 'Spool', 'Pickups', 'Ground Thread tuft', 'Spool_Tuft.png, 12 x 12',
                     'The Thread foes drop.', versions('Spool', '{v}/game_size/Spool_Tuft.png'), 12, 12,
                     VS.NAMES['Spool_Tuft'], 8))
    ne = versions('Spool', '{v}/preview_8x/models_Needle.png')
    out.append(still('needle', 'Spool', 'Pickups', 'Dart needle model', 'SpoolModels.Needle (3D model, render)',
                     'The Dart\'s thrown needle. A version is a C# method pasted over Needle.', ne, 256, 256,
                     VM.NAMES['Needle'], 0.75, kind='model'))
    # ---------------------------------------------------------------- UI
    groups = {'RogueWaves_RisingWater': 'Sea weather', 'RogueWaves_Quickening': 'Sea weather', 'RogueWaves_Nesting': 'Sea weather'}
    labels = {'RogueWaves_RisingWater': ('Rising Water', 'Wired in game (08_Icons.cs rows).'),
              'RogueWaves_Quickening': ('Quickening Current', 'Wired in game (08_Icons.cs rows).'),
              'RogueWaves_Nesting': ('Nesting Ground', 'Wired in game (08_Icons.cs rows).'),
              'RogueWaves_Crest': ('Crest', 'The mode\'s mark. No slot yet.'),
              'RogueWaves_PureAura': ('Pure Aura', 'The Pure Aura counter and rewards.'),
              'RogueWaves_Depth': ('Depth', 'Depth (ascension) marks.'),
              'RogueWaves_Retry': ('Retry', 'The campaign\'s retries.'),
              'RogueWaves_Vote': ('Vote', 'The chart vote.'),
              'Direction_Tougher': ('Made of Tougher Stuff', 'Direction (scope 11).'),
              'Direction_CompostBin': ('Compost Bin', 'Direction (scope 11).'),
              'Direction_DandelionSeason': ('Dandelion Season', 'Direction (scope 11).'),
              'Direction_MotherOfMushrooms': ('Mother of Mushrooms', 'Direction (scope 11).'),
              'Direction_EfficientRecycler': ('Efficient Recycler', 'Direction (scope 11).'),
              'Direction_SewingMachine': ('Sewing Machine', 'Direction (scope 11).'),
              'Cardinal_VampireDentures': ('Vampire Dentures', 'Cardinal (scope 12).')}
    for n, opts in VI.V.items():
        g = groups.get(n, 'Directions and Cardinals' if n.startswith(('Direction', 'Cardinal')) else 'Mode icons')
        t, f = labels[n]
        out.append(still('icon-' + n, 'UI', g, t, n + '.png, 12 x 12', f, versions('UI', '{v}/game_size/%s.png' % n), 12, 12,
                         [o[0] for o in opts], 7))
        if n == 'RogueWaves_Crest':
            out.append(still('icon-RogueWaves_Crest32', 'UI', 'Mode icons', 'Crest, 32 px', 'RogueWaves_Crest32.png, 32 x 32',
                             'The main menu\'s Rogue Waves button. Drawn at 32 px, not scaled.',
                             versions('UI', '{v}/game_size/RogueWaves_Crest32.png'), 32, 32, VI.CREST32_NAMES, 4))
    return out


def build(path):
    its = items()
    tpl = open(os.path.join(os.path.dirname(os.path.abspath(__file__)), 'variants_page.html')).read()
    page = tpl.replace('/*ITEMS*/[]', json.dumps(its)).replace('/*IMG*/{}', json.dumps(IMG))
    os.makedirs(os.path.dirname(os.path.abspath(path)), exist_ok=True)
    open(path, 'w').write(page)
    print('%d items, %d images, %.0f KB -> %s' % (len(its), len(IMG), len(page) / 1024, path))


if __name__ == '__main__':
    build(sys.argv[1])
