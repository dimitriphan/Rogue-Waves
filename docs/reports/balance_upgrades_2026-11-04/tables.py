"""Tide tables from the RogueLab dumps: Downpour (Mode_Normal) from round 1, High Tide (Mode_Storm) and Flood from round 2."""
import os, re
REF = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..', '..', 'TDS_Vessel_Creator', 'reference') + os.sep
ELITE = {'Health': 'Health', 'Red': 'Red', 'Hyper': 'Hyper', 'Chaos': 'Chaos', 'Armor': 'Armor'}
def _steps(content):
    steps = []
    for part in content.split(';'):
        part = part.strip()
        if not part: continue
        m = re.match(r'(Spread|Wait|Gate)\s+(\S+)?\s*(?:x(\d+))?\s*every\s+([\d.]+)', part)
        if not m: continue
        style, being, n, every = m.group(1), m.group(2), int(m.group(3) or 0), float(m.group(4))
        el = None
        e = re.search(r'\[(?:Enemy_)?(\w+?)(?:_Upgradeobject)?\]', part)
        if e: el = e.group(1)
        if style == 'Wait': steps.append(('wait', every))
        else: steps.append(('spread', being.replace('Enemy_', ''), n, every, el))
    return steps
def downpour():
    tides, breaks = [], []
    for line in open(REF + 'roguelab_round1.txt'):
        m = re.match(r'TIDE (\d+) (tide|break)(?: shown \d+)? ?(\d+)? time (\d+)(.*)', line)
        if not m: continue
        if m.group(2) == 'break': breaks.append(float(m.group(4)))
        else: tides.append(dict(n=int(m.group(3)), time=float(m.group(4)), steps=_steps(line.split('|', 1)[1] if '|' in line else '')))
    return tides, breaks
def stock(mode):
    tides, breaks = [], []
    for line in open(REF + 'roguelab_round2.txt'):
        m = re.match(r'TEST TIDE2 %s \d+ (tide|break) shown (\d+) time (\d+)' % mode, line)
        if not m: continue
        if m.group(1) == 'break': breaks.append(float(m.group(3)))
        else: tides.append(dict(n=int(m.group(2)), time=float(m.group(3)), steps=_steps(line.split('|', 1)[1])))
    return tides, breaks
TABLES = {'downpour': downpour(), 'hightide': stock('Mode_Storm'), 'flood': stock('Mode_Drought')}
if __name__ == '__main__':
    for k, (t, b) in TABLES.items():
        print(k, len(t), 'tides, breaks', b[:3], '...')
        for x in t[:16]:
            cnt = {}
            for s in x['steps']:
                if s[0] == 'spread': cnt[s[1] + (('[' + s[4] + ']') if s[4] else '')] = cnt.get(s[1] + (('[' + s[4] + ']') if s[4] else ''), 0) + s[2]
            print('  T%d' % x['n'], cnt)
