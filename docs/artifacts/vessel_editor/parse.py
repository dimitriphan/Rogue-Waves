"""Parse the scope's template files (tab-indented) into vessel dicts."""
import re, json, sys

def tree(path):
    root = {'t': '', 'k': [], 'd': -1}
    stack = [root]
    for raw in open(path, encoding='utf-8'):
        raw = raw.rstrip('\r\n')
        if not raw.strip():
            continue
        d = len(raw) - len(raw.lstrip('\t'))
        n = {'t': raw.strip(), 'k': [], 'd': d}
        while stack[-1]['d'] >= d:
            stack.pop()
        stack[-1]['k'].append(n)
        stack.append(n)
    return root

NOTE = re.compile(r'^(.*?)\s*/(~?)\s*(.*?)\s*\\\s*$')
FIELD = re.compile(r"^- ((?:In-game|[A-Z][A-Za-z0-9]*)(?: [a-z0-9]+){0,3}): ?(.*)$")
UPG = re.compile(r'^% (\d):\{(~?)([^}~]*)~?\}\s*(.*)$')  # {~Name~} = Claude's placeholder name
BLD = re.compile(r'^\(([^)]+)\):\{([^}]*)\}\s*(.*)$')
SEC = re.compile(r'^\[([^\]]+)\]\s*(.*)$')

def split_note(s):
    m = NOTE.match(s)
    if m:
        return m.group(1).strip(), (m.group(3).strip(), m.group(2) == '~')
    return s.strip(), None

def clean(s):
    """{Name} -> Name, [Name] -> Name for display."""
    return re.sub(r'[{}\[\]]', '', s).replace('->', '→').strip()

class Ctx:
    def __init__(self, mine):
        self.mine = mine  # True for Dimitri's sheets: ~value~ spans are Claude defaults

    def val(self, s):
        """Return (value, note) with Claude's parts removed when mine."""
        v, note = split_note(s)
        if note and note[1]:
            note = None  # /~ \ = Claude note, never shown
        elif note:
            note = clean(note[0])
        if self.mine:
            if v.startswith('~') or v.startswith('{~'):
                return None, note
            if '~' in v:
                segs = []
                for seg in v.split(';'):
                    if '~' in seg:
                        seg = seg[:seg.index('~')].rstrip(' ,.;')
                    if seg.strip():
                        segs.append(seg.strip())
                v = '; '.join(segs)
        return (clean(v) if v else None), note

def fields(ctx, node):
    out, desc, notes = {}, [], []
    for k in node['k']:
        t = k['t']
        if t.startswith('#'):
            notes.append(clean(t[1:].strip()))
            continue
        if t.startswith('/'):
            v, n = split_note(t) if t.endswith('\\') else (t, None)
            if t.startswith('/~'):
                continue
            notes.append(clean(t.strip('/\\ ')))
            continue
        m = FIELD.match(t)
        if m:
            v, n = ctx.val(m.group(2))
            out[m.group(1)] = {'v': v, 'n': n, 'r': m.group(2)}
        elif t.startswith('- '):
            v, n = ctx.val(t[2:])
            if v:
                desc.append({'v': v, 'n': n})
        elif t.startswith('!'):
            desc.append({'needs': clean(t.strip('! '))})
    return out, desc, notes

def upgrades(ctx, node):
    ups = []
    for k in node['k']:
        m = UPG.match(k['t'])
        if m:
            lvl, tilde, name, rest = int(m.group(1)), m.group(2), m.group(3), m.group(4)
            d, n = split_note(rest)
            if n and n[1]:
                n = None
            elif n:
                n = clean(n[0])
            f, _, _ = fields(ctx, k)
            ups.append({'lvl': lvl, 'name': None if (tilde and ctx.mine) else clean(name),
                        'desc': clean(d), 'note': n,
                        'line': (f.get('In-game line') or {}).get('v'),
                        'num': (f.get('Numbers') or {}).get('v'),
                        'numnote': (f.get('Numbers') or {}).get('n'),
                        'leads': (f.get('Leads to') or {}).get('v'),
                        'cost': (f.get('Cost') or {}).get('v')})
        elif k['t'].startswith('!'):
            ups.append({'needs': clean(k['t'].strip('! '))})
    return ups

def vessel(ctx, node):
    name, _, title = node['t'].partition(',')
    v = {'name': name.strip(), 'title': title.strip(), 'notes': [], 'identity': {}, 'stats': {},
         'ability': None, 'resource': None, 'mechanic': None, 'buildings': [], 'minions': [], 'directions': []}
    for k in node['k']:
        t = k['t']
        if t.startswith('#'):
            v['notes'].append(clean(t[1:].strip()))
            continue
        if t.startswith('/'):
            continue
        b = BLD.match(t)
        if b:
            slot, bname, rest = b.group(1), b.group(2), b.group(3)
            _, bnote = split_note(rest) if rest else ('', None)
            f, desc, notes = fields(ctx, k)
            item = {'slot': slot, 'name': clean(bname), 'note': (bnote[0] if bnote and not bnote[1] else None),
                    'fields': f, 'desc': desc, 'notes': notes, 'stats': {}, 'growth': {}, 'upgrades': [],
                    'subs': [], 'weapons': []}
            for s in k['k']:
                st = s['t']
                if st.startswith('[Stats]'):
                    item['stats'] = fields(ctx, s)[0]
                elif st.startswith('[Growth]'):
                    gn = split_note(st)[1]
                    item['growth'] = fields(ctx, s)[0]
                    item['growthnote'] = gn[0] if gn and not gn[1] else None
                elif st.startswith('[Upgrades]'):
                    item['upgrades'] = upgrades(ctx, s)
                elif st.startswith('[Weapons]'):
                    item['weapons'] = [fields(ctx, {'k': [w]})[0] for w in s['k']]
                    item['weapons'] = [{'raw': clean(split_note(w['t'][2:])[0]),
                                        'stats': (split_note(w['t'])[1] or [''])[0]} for w in s['k']]
                elif st.startswith('{'):
                    nm, _, rest2 = st.partition('}')
                    sf, sd, sn = fields(ctx, s)
                    n2 = split_note(rest2)[1]
                    item['subs'].append({'name': clean(nm), 'note': n2[0] if n2 and not n2[1] else None,
                                         'fields': sf, 'desc': sd, 'notes': sn})
            (v['minions'] if slot == 'Minion' else v['buildings']).append(item)
            continue
        s = SEC.match(t)
        if s:
            sec, rest = s.group(1), s.group(2)
            f, desc, notes = fields(ctx, k)
            if sec == 'Identity':
                v['identity'] = f
            elif sec == 'Vessel Stats':
                v['stats'] = f
            elif sec in ('Ability', 'Resource', 'Vessel Mechanic'):
                key = {'Ability': 'ability', 'Resource': 'resource', 'Vessel Mechanic': 'mechanic'}[sec]
                v[key] = {'name': clean(split_note(rest)[0]), 'fields': f, 'desc': desc}
            elif sec == 'Directions':
                for d in k['k']:
                    if d['t'].startswith('- '):
                        v['directions'] += [clean(x) for x in re.findall(r'\{([^}]*)\}', d['t'])]
    return v

def parse(path, mine):
    ctx = Ctx(mine)
    r = tree(path)
    out = []
    for top in r['k']:
        if 'Vessel:' in top['t']:
            for k in top['k']:
                if ',' in k['t'] and not k['t'].startswith(('#', '/', '-')):
                    out.append(vessel(ctx, k))
    return out

if __name__ == '__main__':
    print(json.dumps(parse(sys.argv[1], sys.argv[2] == 'mine'), indent=1)[:int(sys.argv[3]) if len(sys.argv) > 3 else None])
