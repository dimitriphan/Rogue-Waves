r"""Build the Vessel Spotlight page: scope sheets + sprite sheets -> one self-contained HTML file.

Base vessels: docs/scope/16_Base_Vessels.txt (the game's numbers) + the game's own sheets
(/mnt/project-files/research/art_dump_2026-10-15/sheets, read-only, never committed).
Spool and Yossele: Dimitri's own sheets (07, 08, 11) as text, parsed in the page by sheet.js with every Claude
default (~value~, /~ note \) left out, and his own sprites (assets/, from his reshade .aseprite files). Nothing Claude
drew is used for them. Writes two pages: the spotlight, and the Vessel Editor (the same page plus editor.js).
"""
import base64, io, json, os, re, subprocess, sys
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import parse

SCOPE = os.path.normpath(os.path.join(HERE, '..', '..', 'scope')) + os.sep   # the repository's docs/scope
DUMP = '/mnt/project-files/research/art_dump_2026-10-15/sheets/'
# Dimitri's own reshade frames (his uploads): private, read from the project files, never committed
ASSETS = '/mnt/project-files/concepts/vessel_spotlight_2026-10-15/assets'

SHEETS = {}


def add_sheet(key, path):
    if key in SHEETS:
        return key
    im = Image.open(path).convert('RGBA')
    buf = io.BytesIO()
    im.save(buf, 'PNG', optimize=True)
    SHEETS[key] = {'u': 'data:image/png;base64,' + base64.b64encode(buf.getvalue()).decode(), 'w': im.width, 'h': im.height}
    return key


def game(name):
    return add_sheet(name, DUMP + name + '.png')


def spr(sheet, c=32, rows=(0,), f=6, fps=8):
    """A sprite: sheet key, cell size, row per facing, frames per row, frames a second."""
    return {'s': game(sheet), 'c': c, 'rows': list(rows), 'f': f, 'fps': fps}


# ------------------------------------------------------------------ animations of the base vessels
F4 = (0, 1, 2, 3)
M4 = (4, 5, 6, 7)
BASE_ANIMS = {
    'Smacky': {
        'Idle': spr('Smacky4-Sheet', rows=F4, fps=8), 'Move': spr('Smacky4-Sheet', rows=M4, fps=8),
        'Land': spr('SmackyIntroR-Sheet', f=12, fps=8), 'Fall': spr('Smackyfallr-Sheet', f=12, fps=8),
        'Mini': spr('SSmackySheet', c=16, rows=F4, fps=8), 'Readyup': spr('Readyup1smacky', c=12, f=1, fps=1),
    },
    'Sno Sno': {
        'Idle': spr('Snosnoidle-Sheet', rows=F4, fps=8), 'Move': spr('Snosnoidle-Sheet', rows=M4, fps=8),
        'Attack': spr('SnosnoAttack-Sheet', rows=F4, f=4, fps=24),
        'Land': spr('SnosnoIntroActiveR-Sheet', f=12, fps=8), 'Fall': spr('SnosnoIntroIdleR-Sheet', f=6, fps=8),
        'Mini': spr('SSnosnoSheet', c=16, rows=F4, fps=8),
    },
    'Boutman': {
        'Idle': spr('Boutman-Sheet', rows=F4, fps=9), 'Move': spr('Boutman-Sheet', rows=M4, fps=9),
        'Attack': spr('Boutmanattack-Sheet', rows=F4, f=3, fps=9),
        'Fishing': spr('Boutmanfish-Sheet', rows=F4, fps=9), 'KO': spr('BoutKo-Sheet', rows=F4, fps=9),
        'Land': spr('Boutintro-Sheet', f=15, fps=14), 'Fall': spr('BoutIntroIdle-Sheet', f=6, fps=9),
        'Mini': spr('SBoutmanSheet', c=16, rows=F4, fps=9),
    },
    'Fie': {
        'Idle': spr('Fie-Sheet', rows=F4, fps=8), 'Move': spr('Fie-Sheet', rows=M4, fps=8),
        'Land': spr('FieIntroR2-Sheet', f=12, fps=8), 'Fall': spr('FieIntroIdleR-Sheet', f=6, fps=8),
        'Mini': spr('SFieSheet', c=16, rows=F4, fps=8),
    },
    'Buster': {
        'Idle': spr('Buster-Sheet', rows=F4, fps=10), 'Move': spr('Buster-Sheet', rows=M4, fps=10),
        'Attack': spr('Busterattack-Sheet', rows=F4, f=2, fps=10), 'Dash': spr('Busterrun-Sheet', rows=F4, fps=10),
        'Land': spr('BusterIntroActiveR-Sheet', f=12, fps=10), 'Fall': spr('BusterIntroIdleR-Sheet', f=6, fps=10),
        'Mini': spr('SBusterSheet', c=16, rows=F4, fps=10),
    },
}

UNIT_SPRITES = {
    'Miner': spr('SmackyMiner-Sheet', c=16, fps=4), 'Engineer': spr('SmackyConstruction-Sheet', c=16, fps=4),
    'Snoble': spr('SnobleGruntIdle-Sheet', c=12, fps=6), 'Mini Snoble': spr('SnobleminiIdle-Sheet', c=12, fps=6),
    'Snoble Mother': spr('Megasnoble1-Sheet', fps=6),
    'Gubby': spr('Gubbyzmainz-sheet', fps=7), 'Boutbro': spr('Boutbro-Sheet', c=16, fps=7),
    'Buppy': spr('Gubbyzmainz-sheet - Copy 1', fps=7), 'Boutdad': spr('Boutdad-Sheet', fps=5),
    'Boutmom': spr('Boutmom-Sheet', fps=7), 'Knight': spr('Gray-Sheet', c=16, fps=8), 'Dog': spr('Dog1-Sheet', c=16, fps=8),
}
UNIT_VARIANTS = {
    'Snoble': [('Black Sno', spr('SnobleblackIdle-Sheet', c=12, fps=6))],
    'Mini Snoble': [('Black Sno', spr('SnobleminiblackIdle-Sheet', c=12, fps=6))],
}
# Upgrades whose unit wears its own sheet (sheet names from the game's animation tables, kit art survey)
UPGRADE_SPRITES = {
    'Barp': spr('Barpw-Sheet', fps=7), 'Batfish': spr('Batfishz', fps=7), 'Boldfish': spr('Boldfishz-Sheet', fps=7),
    'Balibut': spr('BalibutZheet', fps=7), 'Boi': spr('Boiz-Sheet', fps=7), 'Bwordfish': spr('Bwordfishz-Sheet', fps=7),
    'Bbetta': spr('Bbetta-Sheet', fps=7), 'Bar': spr('BariwZ-Sheet', fps=7), 'Baviar': spr('Baviarz-Sheet', fps=7),
    'Pubberfish': spr('Bufferfishz-Sheet', fps=7), 'Bass': spr('Bass-Sheet2', fps=7),
    'Jack Up': spr('Dog2-Sheet', c=16), 'Pit Crew': spr('Dog4-Sheet', c=16), 'Big Dog': spr('Dog3-Sheet', c=16),
    'Up Dog': spr('Dog5-Sheet', c=16), 'Lap Dog': spr('Dog7-Sheet', c=16), 'Diesel Dog': spr('Dog9-Sheet', c=16),
    "Mechanic's Friend": spr('Dog8-Sheet', c=16), 'Rev Dog': spr('Dog6-Sheet', c=16), 'Nitro Collar': spr('Dog10-Sheet', c=16),
}

ATTACK_NOTE = {
    'Smacky': 'No attack sheet: he swings in his Idle frames',
    'Fie': 'No attack sheet: she swings in her Idle frames',
}

# ------------------------------------------------------------------ helpers

def num(v):
    m = re.match(r'-?\d+(\.\d+)?', v or '')
    return float(m.group(0)) if m else None


def kv(d, skip=()):
    out = []
    for k, x in d.items():
        if k in skip:
            continue
        out.append({'k': k, 'v': x['v'], 'n': x['n']})
    return out


STAT_KEYS = [('Health', 65), ('Speed', 3.5), ('Damage', 5), ('Range', 3), ('Attack speed', 0.75),
             ('Build range', 3), ('Sell rate', 75)]
CORE = {'Attack', 'Health', 'Health regen', 'Speed', 'Damage', 'Range', 'Attack speed', 'Build range',
        'Sell rate', 'Special rule'}
UNIT_KEYS = ['Made by', 'Attack', 'Health', 'Damage', 'Range', 'Attack speed', 'Speed', 'Engage range', 'Carries',
             'Collects from', 'How many', 'Respawn', 'Leash', 'Orders', 'Cost']


def stats_block(v, mine, dirs=None):
    s = v['stats']
    bars = []
    for k, mx in STAT_KEYS:
        x = s.get(k) or {'v': None, 'n': None}
        val = x['v']
        main = val.split(',')[0].strip() if val else None
        extra = val.split(',', 1)[1].strip() if val and ',' in val else None
        bars.append({'k': k, 'v': main, 'x': extra, 'num': num(main), 'max': mx,
                     'dir': (dirs or {}).get(k)})
    extras = [{'k': k, 'v': x['v'], 'n': x['n']} for k, x in s.items() if k not in CORE]
    regen = s.get('Health regen', {}).get('v')
    return bars, extras, regen


def rule(v):
    r = v['stats'].get('Special rule')
    if not r or not r['v']:
        return None
    raw = r['r']
    if raw.strip() == 'none':
        return {'none': True}
    m = re.match(r'\{([^}]*)\}\s*(.*)', raw)
    return {'name': m.group(1), 'text': m.group(2)} if m else {'name': '', 'text': r['v']}


def split_notes(lines):
    out = []
    for t in lines:
        m = re.match(r'^(.*?)\s*/\s*(.*?)\s*\\?\s*$', t)
        if m and ' / ' in t:
            out.append({'v': m.group(1).strip(), 'n': m.group(2).rstrip('\\ ').strip()})
        else:
            out.append({'v': t, 'n': None})
    return out


def building(b, mine):
    st = b['stats']
    line = (st.get('In-game line') or {}).get('v')
    cost = (st.get('Cost') or {}).get('v')
    stats = kv(st, skip=('In-game line', 'Cost'))
    growth = None
    if b['growth']:
        growth = {'rows': kv(b['growth']), 'note': b.get('growthnote')}
    ups, needs = [], []
    for u in b['upgrades']:
        if 'needs' in u:
            needs.append(u['needs'])
            continue
        u = dict(u)
        if u['name'] in UPGRADE_SPRITES and not mine:
            u['sprite'] = UPGRADE_SPRITES[u['name']]
        if u.get('leads') == 'nothing, the branch ends here':
            u['leads'] = 'end'
        ups.append(u)
    out = {'slot': b['slot'], 'name': b['name'], 'alt': b['note'], 'line': line, 'cost': cost,
           'desc': b['desc'], 'notes': split_notes(b['notes']), 'stats': stats, 'growth': growth,
           'upgrades': ups, 'needs': needs, 'unit': b['slot'] == 'Minion'}
    if b['slot'] == 'Minion':
        out['stats'] = kv(b['fields'])
    if not mine and b['name'] in UNIT_SPRITES:
        out['sprite'] = UNIT_SPRITES[b['name']]
    return out


def unit(m, mine, name=None, fields=None, note=None, notes=None, desc=None):
    name = name or m['name']
    f = fields if fields is not None else m['fields']
    rows = [{'k': k, 'v': f[k]['v'], 'n': f[k]['n']} for k in UNIT_KEYS if k in f]
    u = {'name': name, 'note': note if note is not None else m.get('note'), 'rows': rows,
         'desc': desc if desc is not None else m.get('desc', []), 'notes': notes if notes is not None else m.get('notes', [])}
    if not mine:
        u['sprite'] = UNIT_SPRITES.get(name)
        u['variants'] = [{'label': l, 'sprite': s} for l, s in UNIT_VARIANTS.get(name, [])]
        if name == 'Gubby':
            u['variants'] = [{'label': k, 'sprite': UPGRADE_SPRITES[k]} for k in
                             ['Barp', 'Batfish', 'Boldfish', 'Balibut', 'Boi', 'Bwordfish', 'Bbetta', 'Bar', 'Baviar', 'Pubberfish', 'Bass']]
        if name == 'Dog':
            u['variants'] = [{'label': k, 'sprite': UPGRADE_SPRITES[k]} for k in
                             ['Jack Up', 'Pit Crew', 'Big Dog', 'Up Dog', 'Lap Dog', 'Diesel Dog', "Mechanic's Friend", 'Rev Dog', 'Nitro Collar']]
        if m.get('weapons'):
            u['weapons'] = [w['raw'] for w in m['weapons']]
    return u


def sig(v):
    out = []
    for key, label in (('ability', 'Ability'), ('resource', 'Resource'), ('mechanic', 'Vessel rule')):
        x = v[key]
        if x:
            out.append({'type': label, 'name': x['name'], 'rows': kv(x['fields'])})
    return out


# ------------------------------------------------------------------ the base vessels
base = parse.parse(SCOPE + '16_Base_Vessels.txt', False)
VESSELS = []
for v in base:
    ident = v['identity']
    bars, extras, regen = stats_block(v, False)
    anims = BASE_ANIMS[v['name']]
    vid = v['name'].lower().replace(' ', '')
    vv = {
        'id': vid, 'kind': 'base', 'name': v['name'], 'title': v['title'],
        'difficulty': ident['Difficulty']['v'], 'line': ident['In-game line']['v'],
        'look': ident['Look']['v'], 'accent': ident['Accent colour']['v'],
        'archetype': ident['Archetype']['v'], 'lore': ident['Lore']['v'],
        'attack': v['stats']['Attack']['v'], 'rule': rule(v),
        'bars': bars, 'extras': extras, 'regen': regen, 'sig': sig(v),
        'buildings': [building(b, False) for b in v['buildings']],
        'units': [], 'anims': anims, 'attackNote': ATTACK_NOTE.get(v['name']),
    }
    for m in v['minions']:
        vv['units'].append(unit(m, False))
        if m['upgrades']:
            vv['buildings'].append(building(m, False))
    VESSELS.append(vv)

# ------------------------------------------------------------------ Dimitri's vessels
# Their pages are parsed in the browser from the sheet text itself (sheet.js), so the spotlight and the editor
# read the sheet the same way and the editor can export it back unchanged.
def data_url(path):
    return 'data:image/png;base64,' + base64.b64encode(open(path, 'rb').read()).decode()


RW = [
    {'id': 'spool', 'file': '08_Vessel_Spool.txt', 'accent': '#7E4D90', 'accent2': '#BFBA3F'},
    {'id': 'yossele', 'file': '07_Vessel_Yossele.txt', 'accent': '#864B41', 'accent2': '#72CC35'},
]
for r in RW:
    r['text'] = open(SCOPE + r['file'], encoding='utf-8', newline='').read()
    r['frameUrl'] = data_url(os.path.join(ASSETS, 'dimitri_' + r['id'] + '.png'))

tpl = open(SCOPE + '15_Templates.txt', encoding='utf-8', newline='').read()
a = tpl.index('| Template: Vessel |')
b = tpl.index('| Template: Direction |')
VESSEL_TEMPLATE = tpl[a:b]


try:
    BUILT = subprocess.run(['git', '-C', HERE, 'rev-parse', '--short', 'HEAD'], capture_output=True, text=True, check=True).stdout.strip()
except (OSError, subprocess.CalledProcessError):
    BUILT = ''


def page(edit):
    data = {'vessels': VESSELS, 'sheets': SHEETS, 'rw': RW, 'edit': edit,
            'dirText': open(SCOPE + '11_Directions.txt', encoding='utf-8', newline='').read(),
            'vesselTemplate': VESSEL_TEMPLATE, 'built': BUILT}
    t = open(os.path.join(HERE, 'template.html'), encoding='utf-8').read()
    js = open(os.path.join(HERE, 'sheet.js'), encoding='utf-8').read()
    pj = ''.join(open(os.path.join(HERE, f), encoding='utf-8').read() for f in ('page.js', 'body.js'))
    if edit:
        pj += open(os.path.join(HERE, 'editor.js'), encoding='utf-8').read()
        pj += "\nshow(location.hash.slice(1) || 'lineup', false);\ninitStore();\n"
        t = t.replace('<title>Rogue Waves Vessel Spotlight</title>', '<title>Rogue Waves Vessel Editor</title>')
        t = t.replace('VESSEL ▪ SPOTLIGHT', 'VESSEL ▪ EDITOR')
        t = t.replace('<!--EDIT_ONLY', '').replace('EDIT_ONLY-->', '')
    else:
        pj += "\nshow(location.hash.slice(1) || 'lineup', false);\n"
    # sheet.js runs in its own scope; the page takes only its public functions
    js = ('const { WIPMARK, textToDoc, docToText, depthOf, isBlank, parseSheet, sheetName, vesselModel, lintSheet, slug } = (() => {\n'
          + js.replace("if (typeof module !== 'undefined') module.exports = ", 'return ') + '\n})();')
    pj = pj.replace('/*__DATA__*/null', json.dumps(data, separators=(',', ':'), ensure_ascii=False).replace('</', '<\\/'))
    return t.replace('/*__SHEET_JS__*/', js).replace('/*__PAGE_JS__*/', pj)


if len(sys.argv) < 3:
    sys.exit('usage: build.py <spotlight.html> <editor.html>  (write both outside the repository: they hold game sprites)')
out, eout = sys.argv[1], sys.argv[2]
for path, edit in ((out, False), (eout, True)):
    html = page(edit)
    open(path, 'w', encoding='utf-8').write(html)
    print('wrote', path, len(html) // 1024, 'KB;', len(SHEETS), 'sheets')
