"""Builds crackpot/index.html, rustle/index.html and broodie/mod/index.html: sheets for the vessels implemented in
TDS_Mod, drawn from the mod itself so they never drift from it. Names, stats, costs and upgrade trees are read from each vessel's
ModSource/<Name>/Code/02_Data.cs; the sprites and model renders are the ones its editor generators write, rendered by
TDS_Mod/Tools/cloud-check/lite/run_preview.sh.

    cd TDS_Mod/Assets/CrackpotMod/Editor && ../../../Tools/cloud-check/lite/run_preview.sh . Crackpot /tmp/vessels/crackpot sm
    cd TDS_Mod/Assets/RustleMod/Editor   && ../../../Tools/cloud-check/lite/run_preview.sh . Rustle   /tmp/vessels/rustle sm
    cd TDS_Mod/Assets/BroodieMod/Editor  && ../../../Tools/cloud-check/lite/run_preview.sh . Broodie  /tmp/vessels/broodie sm
    python3 concept-tools/build_mod_pages.py /tmp/vessels [slug ...]

Rogue Waves: the VESSELS table below still lists the ModProjects vessels it was written for; replace its rows with
the vessels of this repository as they are implemented.

A vessel that already has a concept page of its own (Broodie) gets its mod page in a subfolder ('out'), so the concept
page stays as it was drawn.
"""
import html, os, re, shutil, sys

HERE = os.path.dirname(os.path.abspath(__file__))             # <repo>/concept-tools
REPO = os.path.dirname(HERE)                                  # the Rogue-Waves repository
ROOT = os.path.join(REPO, 'docs', 'concepts')                 # where the pages are written
MOD = os.path.join(REPO, 'TDS_Mod', 'ModSource')

AURA = {'Redaura': ('Red', 'r'), 'Blueaura': ('Blue', 'b'), 'Yellowaura': ('Yellow', 'y')}
STAT_LABEL = {'Health': 'Health', 'Speed': 'Speed', 'Damage': 'Damage', 'Range': 'Range', 'Attackspeed': 'Attack speed',
              'Buildrange': 'Build range', 'Sellrate': 'Sell rate', 'Projspeed': 'Shot speed', 'Units': 'Units',
              'Respawn': 'Respawn (s)', 'Duration': 'Duration (s)', 'Rearm': 'Re-arm (s)', 'Slow': 'Slow', 'Charge': 'Static'}

VESSELS = [
    {
        'slug': 'crackpot', 'name': 'Crackpot', 'accent': '#b8940f', 'accent_dark': '#8a6d06', 'accent_soft': '#f7f0cf', 'accent_line': '#e3d38b',
        'ability': ('ZapName', 'ZapDesc'), 'ability_anim': ('CrackpotZap-Sheet.png', 'ZAP'),
        'pitch': 'A small wizard whose head is a dark orb cracked open by a seam of golden light. Everything it touches is '
                 'charged with Static until it goes off.',
        'loop_title': 'Charge it, then let it go off',
        'loop': [
            ('Static', 'Every hit from Crackpot or one of its buildings charges a foe with Static, up to <b>5</b>. It fades '
                       '6 s after the last charge. Every player sees it: sparks at 1-2, a gold ring overhead at 3-4.'),
            ('Overload', 'At 5 Static the foe <b>Overloads</b>: 6 damage plus 8% of its full health (up to +16), a 1 s '
                         'stun (not for bosses), and a jolt that gives the nearest other foe +2 Static. Crowds '
                         'chain-react, capped at 8 Overloads a moment.'),
            ('ZAP', 'The vessel ability (keys 6/7): pick a foe within 5 tiles and lightning from the staff strikes it, '
                    'then hops to 3 more foes nearby, charging each one. 8 s recharge, or at once from a full Think Tank.'),
            ('Payoff', 'The Spark Coil and the Rune Circle build Static; the Thunderhead cashes it in, spending a crowd\'s '
                       'Static for extra damage. Overloads pay the Think Tank and fill it (five fill it, and a full tank '
                       'recharges ZAP), and Overloads beside a Rune Circle charge it for the next foe across.'),
        ],
    },
    {
        'slug': 'rustle', 'name': 'Rustle', 'accent': '#4f9a24', 'accent_dark': '#3a7419', 'accent_soft': '#e5f3d9', 'accent_line': '#b9dc9c',
        'ability': ('StepName', 'StepDesc'), 'ability_anim': ('RustleStep-Sheet.png', 'SHADOWSTEP'),
        'units': [('Cutpurse-Sheet.png', 'Cutpurse', '16×16 unit: walk columns 1-6, stab 7-10, four facings.')],
        'pitch': 'A woodland spirit with a carved wooden mask for a head, two curling horns and leaves fanning out '
                 'behind the mask. The red cross on the mask is his one eye. You never see him coming.',
        'loop_title': 'Get behind them',
        'loop': [
            ('Backstab', 'A hit from behind a foe (within a 110° cone opposite the way it faces), or on a helpless one '
                         '(Snared, Dazed or asleep), lands a second hit of the same damage, with a slash every player '
                         'sees. Rustle and its Cutpurses both backstab.'),
            ('SHADOWSTEP', 'The vessel ability (keys 6/7): pick a foe within 5 tiles and Rustle dashes to the tile behind '
                           'it, then strikes for triple damage and dazes it for 1 s. 10 s recharge.'),
            ('Set it up', 'The Mask Totem\'s darts poison a foe and leave it <b>Distracted</b>, staring at the Totem with '
                          'its back open on the far side (a "?" over it). The Snare holds the first foe to step on it, '
                          'helpless. The Hideout\'s Cutpurses GUARD its range or wait in AMBUSH, where their first hit '
                          'on each foe is a backstab. Master Trapper leaves snared foes Exposed: every hit on them, from '
                          'anyone, deals double.'),
            ('Pick pockets', 'Every backstab near a Stash picks the foe\'s pocket for 1 Yellow aura, a few times per foe '
                             '(more for big foes), double with Ransom Note on a helpless foe, and to every player with '
                             'Robin Hood (double, when playing alone).'),
        ],
    },
    {
        'slug': 'broodie', 'name': 'Broodie', 'out': os.path.join('broodie', 'mod'),
        'accent': '#c98208', 'accent_dark': '#94600a', 'accent_soft': '#fbf0d9', 'accent_line': '#f0cf8f',
        'ability': ('FlingName', 'FlingDesc'), 'ability_anim': ('BroodieFling-Sheet.png', 'FLING'),
        'pick': 'Keys 6 / 7 · picks a ground tile like Sell picks a tower',
        'from': 'The concept page (<a href="../index.html">broodie/index.html</a>, left: its first frame) is the design. '
                'The mod\'s generators redraw every one of its sheets pixel for pixel, with the same shading, eyes and '
                'walk, attack and %s cycles in four facings.',
        'reference_alt': 'Frame one of the concept page\'s sheet',
        'units': [('Nipper-Sheet.png', 'Nipper', '16×16 unit from the Clutch: idle and walk columns 1-6, bite 7-10, four facings.'),
                  ('Grub-Sheet.png', 'Grub', '16×16 unit from the Grub Pit: wriggle columns 1-6, nibble 7-10, four facings.')],
        'others_cap': 'Dozing models are each building\'s "Dozing" skin, shown off Loam; Burst is the Grub Pit after it pops. '
                      'The Loam tiles, Clods, horns and little pits are networked beings; the debuff meshes ride inside the '
                      'effect prefabs the game attaches to foes.',
        'pitch': 'A broody little rhinoceros beetle in a bone mask and a glossy slate shell, with one forked crescent horn. '
                 'It tills the Loam and raises a brood that buries the Tide in tiny bites.',
        'loop_title': 'Till the Loam, raise the brood',
        'loop': [
            ('Loam', 'Mounds and Clods spread <b>Loam</b>, soft tilled earth, one ring of tiles a second; it recedes '
                     'when they go. The Horn Burrow, Clutch and Grub Pit doze off it and wake the moment it reaches them. '
                     'Broodie and its Nippers are faster on it.'),
            ('FLING', 'The vessel ability (keys 6/7): pick a ground tile within 6 and Broodie flings a Clod of Loam onto '
                      'it, spreading Loam around it. Up to 3 Clods; a fourth crumbles the oldest. 20 s recharge.'),
            ('Swarmed', 'A foe bitten by two or more of the brood (Broodie, Nippers, Grubs) within 2 s is <b>Swarmed</b>: '
                        'it takes 8% more damage for each different biter, up to +40%. Amber bite marks circle over it.'),
            ('The brood', 'The Clutch hatches Nippers that guard its range, and SWARM sends them all at one foe. The Grub '
                          'Pit sticks foes in the muck and lets Grubs out to nibble; the Horn Burrow bursts up under '
                          'whoever is furthest along, harder on Loam.'),
        ],
    },
]


def read(path):
    with open(path, encoding='utf-8') as f:
        return f.read()


def consts(src):
    out = {}
    for line in src.split('\n'):
        if 'const string' in line or 'const float' in line or 'const int' in line:
            for k, v in re.findall(r'(\w+) = "((?:[^"\\]|\\.)*)"', line):
                out[k] = v.replace('\\"', '"')
    return out


def string_list(src, name):
    m = re.search(name + r'\s*=\s*\{(.*?)\};', src, re.S)
    return re.findall(r'"([^"]*)"', m.group(1)) if m else []


def builds(src, c):
    out = []
    for block in re.findall(r'new Build\s*\{(.*?)\n        \},', src, re.S):
        def field(n):
            m = re.search(r'\b' + n + r' = "((?:[^"\\]|\\.)*)"', block)
            return m.group(1) if m else ''
        bid = re.search(r'\bId = (\w+)', block).group(1)
        stats = re.search(r'Stats = new\[\] \{(.*?)\}', block).group(1)
        ups = []
        for u in re.findall(r'U\(\w+, "(\w+)", (\d), "([^"]*)", "([^"]*)", "([^"]*)", "([^"]*)", "([^"]*)"\)', block):
            key, tier, disp, desc, pars, costs, nxt = u
            ups.append({'key': key, 'tier': int(tier), 'display': disp, 'desc': desc, 'costs': costs,
                        'next': [n for n in nxt.split(',') if n]})
        out.append({'id': c.get(bid, bid), 'display': field('Display'), 'desc': field('Desc'), 'lore': field('Lore'),
                    'costs': field('Costs'), 'tags': re.findall(r'"(\w+)"', re.search(r'Tags = new\[\] \{(.*?)\}', block).group(1)),
                    'stats': re.findall(r'"([^"]*)"', stats), 'upgrades': ups})
    return out


def cost_html(spec):
    chips = []
    for part in spec.split(';'):
        if '=' not in part:
            continue
        k, v = part.split('=')
        name, cls = AURA.get(k, (k, ''))
        chips.append('<span class="aura %s">%s %s</span>' % (cls, name, v))
    return ' '.join(chips)


def stat_line(stats):
    bits = []
    for s in stats:
        k, v = s.split('=')
        base, _, lvl = v.partition('+')
        bits.append('%s <b>%s</b>%s' % (STAT_LABEL.get(k, k), base, ' (+%s/level)' % lvl if lvl else ''))
    return ' · '.join(bits)


def esc(s):
    return html.escape(s, quote=True)


CSS = r"""
:root {
  color-scheme: light;
  --bg: #eeebe5; --card: #f7f6f2; --line: #dcd6cc; --well: #e2dcd2; --check-a: #cec9c0; --check-b: #c6c1b8;
  --ink: #2f2a26; --muted: #6b645c;
  --red: #c43a3a; --blue: #3469b8; --yellow: #a07a00;
  --f-pixel: "Silkscreen", "Courier New", monospace;
  --f-body: "Nunito", "Segoe UI", system-ui, sans-serif;
  --f-mono: "IBM Plex Mono", ui-monospace, "SFMono-Regular", Menlo, monospace;
}
* { box-sizing: border-box; }
body { margin: 0; background: var(--bg); color: var(--ink); font-family: var(--f-body); font-size: 16px; line-height: 1.55; }
.page { max-width: 1200px; margin: 0 auto; padding: 40px 16px 72px; display: grid; grid-template-columns: minmax(0, 1fr); gap: 80px; }
.page > * { min-width: 0; }
h1, h2, h3, h4, .px { font-family: var(--f-pixel); font-weight: 700; letter-spacing: 0.08em; text-transform: uppercase; margin: 0; text-wrap: balance; }
p { margin: 0; }
a { color: inherit; }
.eyebrow { font-family: var(--f-mono); font-size: 13px; letter-spacing: 0.12em; text-transform: uppercase; color: var(--muted); }
.mono { font-family: var(--f-mono); font-size: 13px; color: var(--muted); overflow-wrap: anywhere; }
.card { background: var(--card); border: 1px solid var(--line); border-radius: 8px; }
.well { background: var(--well); border-radius: 8px; }
canvas, .sprite { image-rendering: pixelated; image-rendering: crisp-edges; display: block; }
.checker { background: repeating-conic-gradient(var(--check-a) 0 25%, var(--check-b) 0 50%) 0 0 / var(--cell, 64px) var(--cell, 64px); }
.section-head { display: grid; gap: 10px; max-width: 900px; margin-bottom: 24px; }
.section-head h2 { font-size: clamp(24px, 4vw, 38px); line-height: 1.1; }
.section-head p { color: var(--muted); font-size: 17px; }
.aura { display: inline-flex; align-items: center; gap: 5px; font-weight: 700; font-size: 13px; white-space: nowrap; font-variant-numeric: tabular-nums; }
.aura::before { content: ""; width: 9px; height: 9px; border-radius: 2px; transform: rotate(45deg); background: currentColor; }
.aura.r { color: var(--red); } .aura.b { color: var(--blue); } .aura.y { color: var(--yellow); }

/* vessel card */
.vessel { display: grid; grid-template-columns: minmax(0, 0.9fr) minmax(0, 1.2fr); gap: 44px; align-items: start; }
.hero { display: grid; gap: 14px; }
.hero .stage { aspect-ratio: 1 / 1; display: grid; place-items: center; }
.hero canvas { width: min(100%, 384px); height: auto; aspect-ratio: 1 / 1; }
.anim-tabs { display: flex; flex-wrap: wrap; gap: 8px; }
.anim-tabs button { font-family: var(--f-pixel); font-size: 12px; letter-spacing: 0.1em; text-transform: uppercase; padding: 8px 12px; border-radius: 4px;
  border: 2px solid var(--accent); background: transparent; color: var(--accent-dark); cursor: pointer; }
.anim-tabs button[aria-pressed="true"] { background: var(--accent); color: #fff; }
.anim-tabs button:focus-visible { outline: 3px solid var(--ink); outline-offset: 2px; }
.from { display: grid; grid-template-columns: auto auto minmax(0, 1fr); gap: 14px; align-items: center; padding: 14px 16px; }
.from img { width: 96px; height: 96px; }
.from .arrow { font-family: var(--f-pixel); color: var(--muted); }
.from p { font-size: 14px; color: var(--muted); }
.side { display: grid; gap: 22px; }
.side h1 { font-size: clamp(52px, 9vw, 92px); line-height: 0.9; letter-spacing: 0.04em; }
.title-row { display: flex; flex-wrap: wrap; align-items: center; gap: 10px 14px; }
.title-row .sub { font-size: 24px; font-weight: 700; }
.badge { font-family: var(--f-pixel); font-weight: 700; font-size: 12px; letter-spacing: 0.12em; color: #fff; background: var(--accent); border-radius: 4px; padding: 5px 10px; }
.pitch { color: var(--muted); font-size: 18px; max-width: 56ch; }
.rule { padding: 20px 24px; display: grid; gap: 8px; border-left: 6px solid var(--accent); }
.rule h3 { font-size: 20px; }
.rule p { font-size: 17px; }
.action { padding: 18px 24px; display: grid; grid-template-columns: auto minmax(0, 1fr); gap: 18px; align-items: center; }
.key { font-family: var(--f-pixel); font-weight: 700; letter-spacing: 0.1em; font-size: 16px; color: #fff; background: var(--accent);
  border-radius: 4px; padding: 14px 18px; box-shadow: inset 0 -4px 0 var(--accent-dark); }
.statgrid { display: grid; grid-template-columns: repeat(4, minmax(0, 1fr)); gap: 10px; }
.statgrid div { padding: 10px 12px; display: grid; gap: 2px; }
.statgrid b { font-family: var(--f-pixel); font-size: 20px; }
.statgrid span { font-size: 13px; color: var(--muted); }

/* kit */
.kit { display: grid; grid-template-columns: repeat(4, minmax(0, 1fr)); gap: 16px; }
.kit .card { padding: 14px; display: grid; gap: 10px; align-content: start; }
.kit img { width: 100%; height: auto; aspect-ratio: 1 / 1; border-radius: 6px; }
.kit h3 { font-size: 16px; }
.kit p { font-size: 15px; }
.kit .stats { font-size: 13px; color: var(--muted); }

/* trees */
.trees { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 18px; }
.tree { padding: 18px; display: grid; gap: 12px; align-content: start; }
.tree-head { display: grid; grid-template-columns: 64px minmax(0, 1fr); gap: 12px; align-items: center; }
.tree-head .thumb { width: 64px; height: 64px; border-radius: 6px; }
.tree-head h3 { font-size: 17px; }
.tier { display: grid; grid-template-columns: 34px minmax(0, 1fr); gap: 10px; align-items: start; }
.tier-label { font-family: var(--f-pixel); font-weight: 700; font-size: 12px; letter-spacing: 0.1em; padding-top: 14px; color: var(--muted); }
.opts { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 8px; }
.opt { background: #fbfaf7; border: 1px solid var(--line); border-radius: 6px; padding: 8px; display: grid; grid-template-columns: 56px minmax(0, 1fr); gap: 10px; align-items: start; }
.thumb { display: block; overflow: hidden; border-radius: 4px; background: #dbd6cd; }
.thumb img { width: 100%; height: 100%; transform: scale(1.7); transform-origin: 50% 52%; }
.opt .thumb { width: 56px; height: 56px; }
.opt b { font-family: var(--f-pixel); font-weight: 700; font-size: 11px; letter-spacing: 0.08em; text-transform: uppercase; display: block; }
.opt span { font-size: 13px; color: var(--muted); line-height: 1.4; display: block; }
.opt .cost { margin-top: 4px; display: flex; flex-wrap: wrap; gap: 4px 10px; }
.opt .next { font-size: 12px; margin-top: 2px; }
.opt.end { border-style: dashed; }
.opt.cap { grid-column: 1 / -1; background: var(--accent-soft); border-color: var(--accent-line); }

/* loop */
.loop { display: grid; grid-template-columns: repeat(4, minmax(0, 1fr)); gap: 16px; }
.loop .card { padding: 18px; display: grid; gap: 8px; align-content: start; }
.loop .n { font-family: var(--f-pixel); color: var(--accent-dark); font-size: 13px; }
.loop h3 { font-size: 16px; }
.loop p { font-size: 15px; }

/* pack */
.assets { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 26px 22px; }
.asset { display: grid; gap: 8px; align-content: start; min-width: 0; }
.asset h3 { font-size: 13px; }
.asset .cap { font-size: 13px; color: var(--muted); }
.sheet-wrap { overflow-x: auto; border-radius: 6px; max-width: 100%; }
.sheet-wrap img { display: block; max-width: none; }
.asset.wide { grid-column: 1 / -1; }
.ui-row { display: flex; flex-wrap: wrap; gap: 18px; align-items: end; }
.ui-row figure { margin: 0; display: grid; gap: 6px; justify-items: center; }
.ui-row figcaption { font-family: var(--f-mono); font-size: 11px; color: var(--muted); }
.gallery { display: grid; grid-template-columns: repeat(auto-fill, minmax(132px, 1fr)); gap: 12px; }
.gallery figure { margin: 0; display: grid; gap: 6px; }
.gallery .thumb { width: 100%; aspect-ratio: 1 / 1; }
.gallery .thumb img { transform: none; }
.gallery figcaption { font-family: var(--f-mono); font-size: 11px; color: var(--muted); overflow-wrap: anywhere; }
footer { font-size: 13px; color: var(--muted); border-top: 1px solid var(--line); padding-top: 16px; display: grid; gap: 6px; }

@media (max-width: 1000px) {
  .vessel { grid-template-columns: minmax(0, 1fr); }
  .kit, .loop { grid-template-columns: repeat(2, minmax(0, 1fr)); }
  .trees, .assets { grid-template-columns: minmax(0, 1fr); }
}
@media (max-width: 640px) {
  .kit, .loop { grid-template-columns: minmax(0, 1fr); }
  .statgrid { grid-template-columns: repeat(2, minmax(0, 1fr)); }
  .opts { grid-template-columns: minmax(0, 1fr); }
  .action { grid-template-columns: minmax(0, 1fr); }
  .from { grid-template-columns: auto minmax(0, 1fr); }
  .from .arrow { display: none; }
}
@media (prefers-reduced-motion: reduce) { .anim-tabs { display: none; } }
"""

JS = r"""
(function () {
  const canvas = document.getElementById('anim');
  const ctx = canvas.getContext('2d');
  const tabs = document.querySelectorAll('.anim-tabs button');
  const sheets = {};
  let cur = null, frame = 0, last = 0;
  const still = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
  function load(src) {
    if (!sheets[src]) { sheets[src] = new Image(); sheets[src].src = src; }
    return sheets[src];
  }
  function pick(btn) {
    tabs.forEach(b => b.setAttribute('aria-pressed', b === btn ? 'true' : 'false'));
    const d = btn.dataset;
    cur = { img: load(d.sheet), cols: +d.cols, row: +d.row, frames: +d.frames, fps: +d.fps, cell: +(d.cell || 32) };
    frame = 0; last = 0;
  }
  function draw(t) {
    if (cur && cur.img.complete && cur.img.naturalWidth) {
      if (!last || t - last > 1000 / cur.fps) {
        last = t;
        ctx.clearRect(0, 0, canvas.width, canvas.height);
        ctx.imageSmoothingEnabled = false;
        const c = cur.cell, sx = (frame % cur.cols) * c, sy = (cur.row + Math.floor(frame / cur.cols)) * c;
        const k = canvas.width / c;
        ctx.drawImage(cur.img, sx, sy, c, c, 0, 0, c * k, c * k);
        frame = still ? 0 : (frame + 1) % cur.frames;
      }
    }
    requestAnimationFrame(draw);
  }
  tabs.forEach(b => b.addEventListener('click', () => pick(b)));
  if (tabs.length) pick(tabs[0]);
  requestAnimationFrame(draw);
})();
"""


def page(v, data_src, assets_rel):
    c = consts(data_src)
    p = v['name']
    hero_stats = dict(s.split('=') for s in string_list(data_src, 'HeroStats'))
    bs = builds(data_src, c)
    ability_name, ability_desc = c[v['ability'][0]], c[v['ability'][1]]
    accent_vars = '--accent: %s; --accent-dark: %s; --accent-soft: %s; --accent-line: %s;' % (v['accent'], v['accent_dark'], v['accent_soft'], v['accent_line'])
    a = assets_rel

    def img(name, alt, w=None, h=None, cls='sprite', extra=''):
        size = (' width="%d" height="%d"' % (w, h)) if w else ''
        return '<img class="%s" src="%s/%s"%s alt="%s"%s>' % (cls, a, name, size, esc(alt), extra)

    out = []
    out.append('<!doctype html>\n<html lang="en">\n<head>\n<meta charset="utf-8">\n<meta name="viewport" content="width=device-width, initial-scale=1">')
    out.append('<title>%s Vessel Concept</title>' % p)
    out.append('<link rel="preconnect" href="https://fonts.googleapis.com">\n<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>')
    out.append('<link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=Silkscreen:wght@400;700&family=Nunito:wght@400;600;700&family=IBM+Plex+Mono:wght@400;500&display=swap">')
    out.append('<style>\n/* Generated by concept-tools/build_mod_pages.py from TDS_Mod (do not edit by hand). The same card family as the\n   Mummer page: vessel card, kit, upgrade trees, how it plays, asset pack. Light-only, like its siblings. */' + CSS + '</style>\n</head>')
    out.append('<body style="%s">\n<main class="page">' % accent_vars)

    # 1. vessel card
    sheet = p + '-Sheet.png'
    anims = [('Idle', sheet, 6, 0, 6, 6), ('Walk', sheet, 6, 4, 6, 10), ('Attack', p + 'Attack-Sheet.png', 4, 0, 4, 8),
             (v['ability_anim'][1], v['ability_anim'][0], 6, 0, 6, 8), ('Landing', p + 'IntroR-Sheet.png', 12, 0, 12, 10)]
    tabs = ''.join('<button type="button" data-sheet="%s/%s" data-cols="%d" data-row="%d" data-frames="%d" data-fps="%d" aria-pressed="false">%s</button>'
                   % (a, s, cols, row, n, fps, esc(name)) for name, s, cols, row, n, fps in anims)
    stats = [('Health', 'Health'), ('Speed', 'Speed'), ('Damage', 'Damage'), ('Range', 'Range'), ('Attackspeed', 'Attack speed'),
             ('Buildrange', 'Build range'), ('Sellrate', 'Sell rate')]
    from_text = v.get('from', "Your drawing (left) became the in-game sheet: same silhouette, palette and pose, with the game's "
                                "shading, eyes and a walk, attack and %s cycle in four facings.") % esc(v['ability_anim'][1])
    statgrid = ''.join('<div class="card"><b>%s</b><span>%s</span></div>' % (hero_stats[k], lbl) for k, lbl in stats if k in hero_stats)
    out.append('''
  <section class="vessel" aria-labelledby="name">
    <div class="hero">
      <div class="well stage checker" style="--cell: 48px"><canvas id="anim" width="384" height="384" role="img" aria-label="%s, animated in game size scaled up"></canvas></div>
      <div class="anim-tabs" role="group" aria-label="Animation">%s</div>
      <div class="card from">%s<span class="arrow" aria-hidden="true">&rarr;</span><p>%s</p></div>
    </div>
    <div class="side">
      <p class="eyebrow">TDS vessel · implemented in TDS_Mod/ModSource/%s</p>
      <h1 id="name">%s</h1>
      <div class="title-row"><span class="sub">%s</span><span class="badge">%s</span><span class="mono">%s</span></div>
      <p class="pitch">%s</p>
      <div class="card rule"><h3>%s</h3><p>%s</p></div>
      <div class="card action"><span class="key">%s</span><div><p>%s.</p><p class="mono">%s</p></div></div>
      <div class="statgrid">%s</div>
      <p class="mono">%s</p>
    </div>
  </section>''' % (esc(p), tabs, img(v['slug'] + '_reference.png', v.get('reference_alt', 'The original 32 by 32 pixel art'), 96, 96),
                     from_text, p, p, esc(c['Displaysub']), esc(c['Displayskill']), esc(c['HeroDesc']),
                     esc(v['pitch']), esc(c['TooltipName']), esc(c['TooltipDesc']), esc(ability_name), esc(ability_desc),
                     esc(v.get('pick', 'Keys 6 / 7 · picks a foe like Sell picks a tower')), statgrid, esc('"' + c['Lore'] + '"')))

    # 2. kit
    cards = []
    for b in bs:
        cards.append('<article class="card">%s<h3>%s</h3><p class="mono">%s</p><p>%s</p><p>%s</p><p class="stats">%s</p></article>'
                     % (img('models/%s.png' % b['id'], b['display'] + ' model', 256, 256, 'model well'), esc(b['display']), b['id'],
                        cost_html(b['costs']), esc(b['lore']), stat_line(b['stats'])))
    out.append('''
  <section aria-labelledby="kit-h">
    <div class="section-head"><p class="eyebrow">Buildables</p><h2 id="kit-h">The kit</h2>
      <p>Four buildings, low-poly like Sno Sno's and Buster's towers. Every model is generated by the mod's editor code; these are its own renders.</p></div>
    <div class="kit">%s</div>
  </section>''' % ''.join(cards))

    # 3. trees
    trees = []
    used = set()
    by_key = {}
    for b in bs:
        for u in b['upgrades']:
            by_key[(b['id'], u['key'] + str(u['tier']))] = u
    for b in bs:
        tiers = []
        for t in (2, 3, 4, 5):
            opts = []
            for u in [u for u in b['upgrades'] if u['tier'] == t]:
                nxt = [by_key[(b['id'], n)]['display'] for n in u['next'] if (b['id'], n) in by_key]
                cls = 'opt cap' if t == 5 else 'opt end' if t == 4 and not u['next'] else 'opt'
                nline = '<span class="next">&rarr; %s</span>' % esc(', '.join(nxt)) if nxt else ('<span class="next">ends here</span>' if t < 5 else '')
                used.add('%s_%s%d.png' % (b['id'], u['key'], t))
                opts.append('<div class="%s"><span class="thumb">%s</span><div><b>%s</b><span>%s</span><span class="cost">%s</span>%s</div></div>'
                            % (cls, img('models/%s_%s%d.png' % (b['id'], u['key'], t), u['display'] + ' model', 256, 256, 'model'),
                               esc(u['display']), esc(u['desc']), cost_html(u['costs']), nline))
            tiers.append('<div class="tier"><span class="tier-label">T%d</span><div class="opts">%s</div></div>' % (t, ''.join(opts)))
        used.add(b['id'] + '.png')
        trees.append('<article class="card tree"><div class="tree-head"><span class="thumb">%s</span><div><h3>%s</h3><p class="mono">%s</p></div></div>%s</article>'
                     % (img('models/%s.png' % b['id'], b['display'], 256, 256, 'model'), esc(b['display']), esc(b['desc']), ''.join(tiers)))
    out.append('''
  <section aria-labelledby="trees-h">
    <div class="section-head"><p class="eyebrow">Upgrades</p><h2 id="trees-h">Upgrade trees</h2>
      <p>Seven upgrades per building, curated like the shipped trees: two tier-2 roots, branches that cross over, one branch that ends at tier 4 (dashed), and one tier-5 capstone. Each upgrade has its own model.</p></div>
    <div class="trees">%s</div>
  </section>''' % ''.join(trees))

    # 4. how it plays
    loop = ''.join('<article class="card"><span class="n">%02d</span><h3>%s</h3><p>%s</p></article>' % (i + 1, esc(t), body) for i, (t, body) in enumerate(v['loop']))
    out.append('''
  <section aria-labelledby="loop-h">
    <div class="section-head"><p class="eyebrow">Core loop</p><h2 id="loop-h">%s</h2></div>
    <div class="loop">%s</div>
  </section>''' % (esc(v['loop_title']), loop))

    # 5. asset pack
    sheets = [
        (p + '-Sheet.png', 'Idle and walk', 'Idle rows 1-4, walk rows 5-8 (four facings), 6 frames, 32×32 at 20 PPU, pivot bottom-centre.', 2, 64),
        (p + 'Attack-Sheet.png', 'Attack', '4 frames × 4 facings.', 2, 64),
        (v['ability_anim'][0], v['ability_anim'][1], '6 frames × 4 facings (the ability).', 2, 64),
        ('S' + p + 'Sheet.png', 'Boat sprite', '16×16 mini sprites for the boat\'s vessel select.', 3, 48),
        (p + 'IntroR-Sheet.png', 'Landing', '12 frames, played when the vessel drops into the match.', 2, 64),
        (p + 'Fall-Sheet.png', 'Falling', '12-frame loop for the drop and the loading screen.', 2, 64),
    ]
    for f, title, cap in v.get('units', []):
        sheets.append((f, title, cap, 3, 48))
    assets = []
    for f, title, cap, k, cell in sheets:
        w, h = png_size(os.path.join(v['src'], 'sheets', f))
        assets.append('<div class="asset%s"><h3>%s</h3><div class="sheet-wrap">%s</div><p class="cap">%s <span class="mono">%s</span></p></div>'
                      % (' wide' if w >= 384 else '', esc(title), img(f, title + ' sheet', w * k, h * k, 'sprite checker', ' style="--cell: %dpx"' % cell), esc(cap), f))
    ui = [(p + '_Icon.png', 'icon', 4), (p.lower() + 'tabicon.png', 'journal tab', 4), (p + 'endstat1.png', 'win', 4), (p + 'endstat2.png', 'lose', 4),
          ('Readyup6' + p.lower() + '.png', 'ready-up', 8), (p + 'splash.png', 'splash', 2)]
    figs = []
    for f, cap, k in ui:
        real = f if os.path.exists(os.path.join(v['src'], 'sheets', f)) else f[0].upper() + f[1:]
        w, h = png_size(os.path.join(v['src'], 'sheets', real))
        figs.append('<figure>%s<figcaption>%s</figcaption></figure>' % (img(real, cap, w * k, h * k, 'sprite checker', ' style="--cell: 16px"'), esc(cap)))
    assets.append('<div class="asset wide"><h3>Menus and journal</h3><div class="ui-row">%s</div></div>' % ''.join(figs))
    # Every model not already shown in the trees: the Snare's sprung state, skin extras, effect meshes, projectiles.
    others = sorted(f for f in os.listdir(os.path.join(v['src'], 'models')) if f not in used and 'turned' not in f)
    figs = ''.join('<figure><span class="thumb">%s</span><figcaption>%s</figcaption></figure>' % (img('models/' + f, f[:-4] + ' model', 256, 256, 'model'), esc(f[:-4]))
                   for f in others)
    assets.append('<div class="asset wide"><h3>Skins, effect meshes and the rest</h3><div class="gallery">%s</div>'
                  '<p class="cap">%s</p></div>' % (figs, (v.get('others_cap', 'Sprung models are the Snare\'s "Sprung" skin on each upgrade; the debuff meshes ride inside the effect prefabs the game attaches to foes.'))))
    out.append('''
  <section aria-labelledby="pack-h">
    <div class="section-head"><p class="eyebrow">Asset pack</p><h2 id="pack-h">Every sprite and model %s ships</h2>
      <p>Written by the mod's editor generators (Tools &gt; %s &gt; Build All Assets in Unity) and shown here as they come out. Sounds are synthesised the same way.</p></div>
    <div class="assets">%s</div>
  </section>''' % (esc(p), p, ''.join(assets)))

    out.append('''
  <footer>
    <p>Design: <span class="mono">TDS_Mod/ModSource/%s/DESIGN.md</span> · notes: <span class="mono">TDS_Mod/ModSource/%s/IMPLEMENTATION_NOTES.md</span> · code: <span class="mono">TDS_Mod/ModSource/%s/Code</span></p>
    <p>Status: the code compiles against the game's API (Roslyn 3.8, release and debug builds); not yet built in Unity or played. Numbers are first-pass values for playtesting.</p>
  </footer>
</main>
<script>%s</script>
</body>
</html>
''' % (p, p, p, JS))
    return '\n'.join(out)


def png_size(path):
    with open(path, 'rb') as f:
        d = f.read(24)
    return int.from_bytes(d[16:20], 'big'), int.from_bytes(d[20:24], 'big')


def main():
    src_root = sys.argv[1] if len(sys.argv) > 1 else sys.exit(__doc__)
    only = sys.argv[2:]
    for v in VESSELS:
        if only and v['slug'] not in only:
            continue
        v['src'] = os.path.join(src_root, v['slug'])
        dst = os.path.join(ROOT, v.get('out', v['slug']))
        assets = os.path.join(dst, 'assets')
        shutil.rmtree(assets, ignore_errors=True)
        os.makedirs(os.path.join(assets, 'models'))
        for f in os.listdir(os.path.join(v['src'], 'sheets')):
            shutil.copy(os.path.join(v['src'], 'sheets', f), assets)
        for f in os.listdir(os.path.join(v['src'], 'models')):
            if 'turned' not in f:
                shutil.copy(os.path.join(v['src'], 'models', f), os.path.join(assets, 'models'))
        shutil.copy(os.path.join(MOD, v['name'], 'Concept', v['slug'] + '_reference.png'), assets)
        data = read(os.path.join(MOD, v['name'], 'Code', '02_Data.cs'))
        with open(os.path.join(dst, 'index.html'), 'w', encoding='utf-8') as f:
            f.write(page(v, data, 'assets'))
        print('wrote', os.path.join(dst, 'index.html'))


if __name__ == '__main__':
    main()
