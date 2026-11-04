"""Builds the minion picks page from /mnt/project-files/concepts/minions_2026-11-04/index.json (run build.py first):
every GIF and line-up inlined as a data URI. Picks save in the page's db collection `picks`, one document per minion
(`cordyfriend`, `threadhead`, `stitched`, `dart`, `seam`, `mangled`, `photon`, `naut`): {choice: "cur" | "v01".."v10",
note, saved}.

    python3 concept-tools/minions/page.py [out.html]   (default: the project files folder, minion_picks.html)
"""
import base64
import html
import json
import os
import sys

PF = '/mnt/project-files/concepts/minions_2026-11-04'
VESSEL = {'Yossele': 'Yossele', 'Spool': 'Spool', 'Uloe': 'Uloe', 'Argo': 'Argo'}
SCOPE = {
    'cordyfriend': 'Rises from a fallen foe after the Mushroom Cannon’s Cordyceps. Scope: “resembles a Snoble”.',
    'threadhead': 'The one larger minion, bought from the build menu, carries Thread home. Scope: “works like Boutman’s fish”. Kept the biggest of Spool’s, still under half its old height.',
    'stitched': 'Made by the Repurposed Cookie Tin. Scope: “short and stubby plushie”.',
    'dart': 'The Cookie Tin’s needle thrower after New Design. Scope: “thin and small plushie”.',
    'seam': 'The Loom’s miner. Scope: “round plushie, similar to Snobles”.',
    'mangled': 'Made by the Pin Cushion when it is hit; bursts or blocks. Scope: “similar to Snobles but much more jagged”.',
    'photon': 'Uloe’s light minions from the Power Bank; they wander and follow Flood Light paths.',
    'naut': 'Argo’s divers from the Miner’s Base: Port, Starboard and Bosun walk to a tower and tune it with a wrench.',
}


def uri(rel):
    p = os.path.join(PF, rel)
    ext = os.path.splitext(p)[1][1:]
    with open(p, 'rb') as f:
        return 'data:image/%s;base64,%s' % ('gif' if ext == 'gif' else 'png', base64.b64encode(f.read()).decode())


def esc(s):
    return html.escape(s, quote=True)


def card(m, v, cur=False):
    g = v['gifs']
    second = 'tune' if 'tune' in g else 'attack' if 'attack' in g else None
    first = 'idle' if 'idle' in g else 'move'
    figs = ['<figure><img class="big" src="%s" alt="%s, %s"><figcaption>%s</figcaption></figure>'
            % (uri(g[first]), esc(v['name']), first, 'idle' if first == 'idle' else 'walk')]
    if first == 'idle' and 'move' in g:
        figs.append('<figure><img class="big" src="%s" alt="%s, walk"><figcaption>walk</figcaption></figure>' % (uri(g['move']), esc(v['name'])))
    if second:
        figs.append('<figure><img class="big" src="%s" alt="%s, %s"><figcaption>%s</figcaption></figure>'
                    % (uri(g[second]), esc(v['name']), second, 'tune a tower' if second == 'tune' else 'attack'))
    figs.append('<figure><img class="big" src="%s" alt="%s from behind"><figcaption>back</figcaption></figure>' % (uri(g['back']), esc(v['name'])))
    crews = ''
    if 'move_Starboard' in g:
        crews = ('<div class="crews"><img src="%s" alt="Starboard"><img src="%s" alt="Bosun"><span>Starboard, Bosun</span></div>'
                 % (uri(g['move_Starboard']), uri(g['move_Bosun'])))
    label = 'Keep the current art' if cur else 'Pick %s' % v['name']
    title = 'Current' if cur else v['name']
    num = 'in game now' if cur else v['id'].replace('v0', 'v').replace('v', 'version ')
    pitch = ('The art wired today, %s.' % ('drawn by code' if m['key'] in ('photon', 'naut') else 'from the temporary art pass')) if cur else v['pitch']
    return ('<article class="card%s" data-take="%s"><div class="frames">%s</div>%s<div class="txt"><div class="head"><h3>%s</h3><span class="num">%s</span></div>'
            '<p>%s</p><span class="stats">%d × %d px at rest · %d colours</span>'
            '<button class="pick" type="button" id="pick-%s-%s" aria-pressed="false">%s</button></div></article>'
            % (' cur' if cur else '', v['id'], ''.join(figs), crews, esc(title), esc(num), esc(pitch), v['w'], v['h'], v['colours'],
               m['key'], v['id'], esc(label)))


def section(m):
    cur = m.get('current')
    cards = ([card(m, cur, True)] if cur else []) + [card(m, v) for v in m['versions']]
    hs = sorted(v['h'] for v in m['versions'])
    names = ['Current'] + [v['name'] for v in m['versions']]
    return '''
<section class="minion" id="%s" data-doc="%s">
  <header class="mhead">
    <span class="eyebrow">%s's minion</span>
    <h2>%s</h2>
    <p>%s</p>
    <dl class="size"><dt>Today</dt><dd>%s</dd><dt>New</dt><dd>%d to %d px tall at rest</dd><dt>Snoble</dt><dd>6 to 8 × 5 to 8 px</dd></dl>
    <div class="lineup-wrap"><div class="lineup"><img src="%s" alt="%s: the current art and the ten versions side by side at game size, three times larger"></div>
    <div class="legend" style="grid-template-columns: repeat(%d, %dpx)">%s</div></div>
  </header>
  <div class="cards">%s</div>
  <div class="foot">
    <p class="pick-line" aria-live="polite">No pick yet.</p>
    <label for="note-%s">Anything to mix or change (“v03's cap on v01”, “a bit bigger”)</label>
    <textarea id="note-%s" class="note" placeholder="Optional"></textarea>
    <div class="row"><button class="save" type="button" id="save-%s">Save note</button><span class="status">Loading saved pick…</span></div>
  </div>
</section>''' % (m['key'], m['key'], VESSEL[m['part']], esc(m['name']), esc(SCOPE[m['key']]),
                 ('%d × %d px' % (cur['w'], cur['h'])) if cur else 'drawn by code', hs[0], hs[-1],
                 uri(m['lineup']), esc(m['name']), len(names), (22 if m['cell'] == 24 else 18) * 3, ''.join('<span>%s</span>' % esc(n) for n in names),
                 ''.join(cards), m['key'], m['key'], m['key'])


def main():
    out = sys.argv[1] if len(sys.argv) > 1 else os.path.join(PF, 'minion_picks.html')
    idx = json.load(open(os.path.join(PF, 'index.json')))
    tpl = open(os.path.join(os.path.dirname(os.path.abspath(__file__)), 'page_template.html')).read()
    nav = ''.join('<a href="#%s">%s</a>' % (m['key'], esc(m['name'])) for m in idx)
    names = {m['key']: dict([('cur', 'the current art')] + [(v['id'], v['name']) for v in m['versions']]) for m in idx}
    page = tpl.replace('{{NAV}}', nav).replace('{{SECTIONS}}', ''.join(section(m) for m in idx)) \
        .replace('{{NAMES}}', json.dumps(names))
    open(out, 'w').write(page)
    print(out, len(page) // 1024, 'KB')


if __name__ == '__main__':
    main()
