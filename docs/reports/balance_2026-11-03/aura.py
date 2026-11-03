"""Aura baseline (2026-11-03, second pass): what a map hands out, and what each vessel's gatherers mine on it.

Asked by Dimitri: "Calculate the average aura per map, as well as each base vessels aura mining capabilities per map and
use those calculations as a baseline to balance yossele and spool." Analytic, Python standard library only, no game
code: the numbers come from the dumps in TDS_Vessel_Creator/reference/ (vessel_data_survey.txt, roguelab_round7.txt),
the play test's ground dumps (2026-10-26) and the decompiled game read in the scratchpad (never committed):

- A node takes min(Damage, 1) + Damageresource off its Healthresource a hit (Helper_Damagetools.Dodamage); once that is
  0 the next hits take its Health of 1 at the raw Damage, and the hit that empties it pays and refills
  (Entity_Ai_Resource_Mine; kit docs/09 #106). So a node pays every ceil(H / ore hit) + ceil(1 / Damage) hits: 5 for
  any striker of Damage 1 or more, 10 for Damage 0.5, 13 for 0.4, 18 for 0.3.
- Smacky's miner (Entity_Ai_Mob_Smackyminer): walks to the node, 0.3 s, strikes, waits 1/Attackspeed, walks back to its
  Hut, waits 1/Attackspeed + (distance - 1) x Distancewait / Attackspeed; Damage 0.5, Attackspeed 0.5, Speed 1.
- Sno Box (Entity_Ai_Tower_Snosnobox.Tick): every 1/Attackspeed hits every node in Range, the k-th nearest at Damage/k.
- Silo Pipes (Entity_Ai_Tower_Fiesilo): builds its pipe at Buildspeed tiles a second, then hits its node every
  1/Attackspeed at Damage 0.3.
- Checkpoint (Entity_Ai_Trap_Bustercheckpoint): charges Damageresource x efficiency every 1/Attackspeed (efficiency 100%
  within 2 tiles of its node, 10% at 10); a same-team Mob touching it spends the charge as Damage-1 hits on the node.
- Every vessel but Boutman strikes a node it stands by when idle (Entity_Ai_Mob_Combat and Mob_Ranged query
  Resource), and so does any idle melee unit; Boutman's query has no Resource until Fishbender.

Run: python3 aura.py [out.md]  (prints the tables; writes results/aura_baseline.json beside this file)
"""
import json, math, os, sys

HERE = os.path.dirname(os.path.abspath(__file__))

# ------------------------------------------------------------------------------------------------ the mode's clock
TIDE, BREAK, OPEN, NT = 180.0, 60.0, 45.0, 15      # Downpour Tides, the mode's 60 s breaks (RWMode.BreakSeconds)
CYCLE = TIDE + BREAK                               # "a Tide" below: the Tide and the break after it, 240 s

# ------------------------------------------------------------------------------------------------ nodes
# Healthresource H and what one payout gives (Red, Blue, Yellow). Health is 1 on every node (read on Resource_Orange).
NODES = {
    'Red':    dict(H=4, pay=(5, 0, 0), note='RogueLab round 7: Healthresource 4, Redauramine 5'),
    'Blue':   dict(H=4, pay=(0, 5, 0), note='assumed the same as Red'),
    'Yellow': dict(H=4, pay=(0, 0, 5), note='assumed the same as Red'),
    'Orange': dict(H=4, pay=(3, 0, 5), note='RogueLab round 7: +3 Red +5 Yellow a payout, read twice off the wallet'),
    'Purple': dict(H=4, pay=(3, 5, 0), note='assumed shaped like Orange'),
    # Purlest's Opal: calibrated at about 9.5 aura a Damage-1 strike (47.5 a payout), one colour a Tide in the order
    # Red, Blue, all, Yellow, so about a third of each over a map.
    'Opal':   dict(H=4, pay=(47.5 / 3, 47.5 / 3, 47.5 / 3), note='calibrated on the 2026-10-26 Purlest wallet'),
}

def spp(node, ore_hit, dmg):
    """Hits a node takes per payout from a striker taking `ore_hit` off Healthresource and `dmg` off Health."""
    H = NODES[node]['H']
    return math.ceil(H / ore_hit - 1e-9) + math.ceil(1.0 / dmg - 1e-9)

def per_hit(node, ore_hit, dmg):
    """Aura (R, B, Y) a hit is worth on `node` (the payout spread over the hits it takes), paid to every player."""
    n = spp(node, ore_hit, dmg)
    return tuple(p / n for p in NODES[node]['pay'])

# ------------------------------------------------------------------------------------------------ maps
# The nodes the 2026-10-26 ground dumps show (tiles no building and no foe can take, matched to the nodes the bot
# named), with their distance from the player core. The Hunt also has an aura-beast Den (3 beasts at the start, one
# more every 5 Tides, at most 4) whose beasts pay on death; their pay is not known, so it is left out.
MAPS = {
    'Purlest': dict(nodes=[('Opal', 11.0)], beasts=0),
    'Woozy': dict(nodes=[('Red', 3.6), ('Blue', 4.0), ('Yellow', 8.9), ('Purple', 13.0)], beasts=0),
    'The Hunt': dict(nodes=[('Orange', 5.8)], beasts=3),
}
SAFE = 9.0   # a gatherer within 9 tiles of the core stands inside the defence (the simulator's rule)

# Red from kills a Tide on Downpour (every foe felled; the same on every map: difficulty scales nothing)
KILL_RED = [5, 12, 16, 23, 44, 22, 36, 85, 68, 60, 69, 60, 165, 100, 120]
START = (50, 50, 50)

# ------------------------------------------------------------------------------------------------ gatherers
# Each setup gives, for Tide k (1-15) and a node, the hits a second on that node (`rate`), the striker's ore hit and
# Damage, and any aura it pays its owner alone a hit (`own`: per colour the node has). `cost` is (R, B, Y) for the
# building and its path. `tier` says when the path is bought: 1 in the opening break, 3 at Tide 3, 4 at Tide 6.

def miner_cycle(walk=1.0, speed=1.0, aspd=0.5, latency=0.5):
    """Seconds between a Smacky miner's hits: out, wind-up, wait, back, rest (Entity_Ai_Mob_Smackyminer)."""
    return 2 * walk / speed + 0.3 + 1 / aspd + 1 / aspd + 0.15 * max(0.0, walk - 1) / aspd + latency

def hut(miners=1, overtime=False, scraps=False, buff=False):
    ot = 1.75 if overtime else 1.0
    if buff:     # Excavation Facility: Speed x2, Attackspeed x3, Damage +1, Damageresource +1 (Mob_Smackyminer_Buff)
        c = miner_cycle(speed=2 * ot, aspd=0.5 * 3 * ot); ore, dmg = 2.0, 1.5
    else:
        c = miner_cycle(speed=ot, aspd=0.5 * ot); ore, dmg = 0.5, 0.5
    return dict(rate=miners / c, ore=ore, dmg=dmg, own=1.0 if scraps else 0.0)

def snobox(slush=False, soft=None):
    a, d = 0.2, 0.4
    if slush: a, d = 0.4, 0.5
    if soft is not None:   # Soft Sno: (0.2 x 2 + 0.2) x (1 + 1.75 x (1 - snow level / 100) on its own tile)
        a = (0.2 * 2 + 0.2) * (1 + 1.75 * (1 - soft))
    return dict(rate=a, ore=d, dmg=d, own=0.0)

def silo(flow=False, quick=False):
    a = 0.25 * (1.5 if quick else 1.0); d = 0.55 if flow else 0.3
    return dict(rate=a, ore=d, dmg=d, own=0.0)

def checkpoint(lap=0, quickpass=False, lap_s=10.0):
    a = [0.3, 0.45, 0.675][lap]
    r = 0.5 * a + ((1.0 / lap_s) if quickpass else 0.0)   # Damageresource 0.5 a tick at 100% efficiency
    return dict(rate=r, ore=1.0, dmg=1.0, own=0.0)

def striker(aspd, dmg=1.0):
    return dict(rate=aspd, ore=min(dmg, 1.0), dmg=dmg, own=0.0)

def sapling(tide, built=0, focused=False, focused_at=3, forest_n=0, fruit=0.0, harvest=False, aspd=0.25, fruit_k=15,
            grow=True):
    g = min(5, max(0, tide - 1 - built)) if grow else 0     # a stage for each whole Tide it stood
    bonus = 0.1 * g + (0.8 if focused and tide >= focused_at else 0.0) + 0.1 * forest_n
    a = aspd * (1 + bonus)
    s = dict(rate=a, ore=1.0, dmg=1.0, own=0.0)
    if g >= 5 and fruit > 0:                       # a fruit a payout: Attackspeed x 15 of each colour, if picked up
        s['fruit'] = max(1, round(a * fruit_k)) * 3 * (2 if harvest else 1) * fruit
    return s

NONE = dict(rate=0.0, ore=1.0, dmg=1.0, own=0.0)

# The setups compared: vessel -> list of (name, cost (R, B, Y), tier, fn(tide) -> list of strikers on one node, kind).
# kind: '' a stock or built setup, 'extra' (an upside the simulator leaves out), 'proposal' (a number to pick),
# 'proposal upside' (both).
SETUPS = {
    'Smacky': [
        ('Mining Hut', (0, 0, 35), 1, lambda k: [hut()], ''),
        ('Hut: Mining Operation, Ore Scraps', (75, 0, 190), 3, lambda k: [hut(2, scraps=k >= 3) if k >= 3 else hut()], ''),
        ('Hut: Mining Operation, Mining Union', (0, 0, 165), 3, lambda k: [hut(3) if k >= 3 else hut()], ''),
    ],
    'Sno Sno': [
        ('Sno Box', (0, 10, 25), 1, lambda k: [snobox()], ''),
        ('Sno Box: Slush', (0, 10, 60), 3, lambda k: [snobox(slush=k >= 3)], ''),
        ('Sno Box: Slush, Soft Sno (snow piled up)', (0, 35, 310), 4, lambda k: [snobox(slush=k >= 3, soft=1.0 if k >= 6 else None)], ''),
        ('Sno Box: Slush, Soft Sno (snow kept off)', (0, 35, 310), 4, lambda k: [snobox(slush=k >= 3, soft=0.0 if k >= 6 else None)], ''),
    ],
    'Fie': [
        ('Silo Pipes', (0, 0, 35), 1, lambda k: [silo()], ''),
        ('Silo: High Flow', (0, 0, 65), 3, lambda k: [silo(flow=k >= 3)], ''),
        ('Silo: High Flow, Reroute, Quick Construct', (0, 0, 190), 4, lambda k: [silo(flow=k >= 3, quick=k >= 6)], ''),
    ],
    'Buster': [
        ('Checkpoint (a dog lap every 10 s)', (0, 0, 50), 1, lambda k: [checkpoint()], ''),
        ('Checkpoint: Lap 1, Lap 2', (0, 0, 205), 3, lambda k: [checkpoint(lap=2 if k >= 3 else 0)], ''),
        ('Checkpoint: Quick Pass, Lap 2', (0, 0, 200), 3, lambda k: [checkpoint(lap=0, quickpass=k >= 3)], ''),
    ],
    'Boutman': [
        ('no gatherer before Fishbender (Yellow 1750)', (0, 0, 0), 1, lambda k: [NONE], ''),
    ],
    'Yossele': [
        ('Pachira Sapling', (0, 0, 35), 1, lambda k: [sapling(k)], ''),
        ('Sapling: Focused Roots', (0, 0, 65), 3, lambda k: [sapling(k, focused=True)], ''),
        ('Sapling: Focused Roots, every fruit picked up', (0, 0, 65), 3, lambda k: [sapling(k, focused=True, fruit=1.0)], 'extra'),
        ('Sapling at Attackspeed 0.15', (0, 0, 35), 1, lambda k: [sapling(k, aspd=0.15)], 'proposal'),
        ('Sapling at 0.15: Focused Roots', (0, 0, 65), 3, lambda k: [sapling(k, focused=True, aspd=0.15)], 'proposal'),
        ('Sapling at 0.15: Focused Roots, fruit x5, every fruit picked up', (0, 0, 65), 3,
         lambda k: [sapling(k, focused=True, aspd=0.15, fruit=1.0, fruit_k=5)], 'proposal upside'),
    ],
    'Spool': [
        ('Loom', (0, 0, 40), 1, lambda k: [striker(0.4)], ''),
        ('Loom: Seam Allowance', (0, 0, 70), 3, lambda k: [striker(0.4)] + ([striker(0.8)] if k >= 3 else []), ''),
        ('Loom at Attackspeed 0.15', (0, 0, 40), 1, lambda k: [striker(0.15)], 'proposal'),
        ('Loom at 0.15: Seam at Damage 0.5, Attackspeed 0.4', (0, 0, 70), 3,
         lambda k: [striker(0.15)] + ([striker(0.4, 0.5)] if k >= 3 else []), 'proposal'),
    ],
}
BASE = ['Smacky', 'Sno Sno', 'Fie', 'Buster']
HAND = {'Smacky': 0.5, 'Sno Sno': 0.5, 'Fie': 0.5, 'Buster': 0.5, 'Boutman': 0.0, 'Yossele': 0.5, 'Spool': 0.6}

def tide_secs(k):
    return OPEN + TIDE if k == 1 else TIDE + BREAK

def aura_on(node, strikers, secs):
    """Aura (R, B, Y) strikers earn on one node in `secs` seconds: the node's payouts (everyone's) plus their own pay.
    Strikers on one node share it; each one's hits are worth the payout spread over the hits it would take alone."""
    out = [0.0, 0.0, 0.0]
    for s in strikers:
        if not s['rate']:
            continue
        hits = s['rate'] * secs
        ph = per_hit(node, s['ore'], s['dmg'])
        own = tuple((1.0 if p > 0 else 0.0) * s['own'] for p in NODES[node]['pay'])
        for i in range(3):
            out[i] += hits * (ph[i] + own[i])
        if s.get('fruit'):
            pays = hits / spp(node, s['ore'], s['dmg'])
            for i in range(3):
                out[i] += pays * s['fruit'] / 3
    return out

def steady(vessel, i, node, k=10):
    """Aura a Tide (240 s) one building of setup i earns on `node` at Tide k (path bought, Sapling grown)."""
    return sum(aura_on(node, SETUPS[vessel][i][3](k), CYCLE))

def map_mining(vessel, i, mapname):
    """Aura (R, B, Y) over Tides 1-15 from one building of setup i beside each node within reach (or the nearest)."""
    nodes = [n for n, d in MAPS[mapname]['nodes'] if d <= SAFE]
    if not nodes:
        nodes = [min(MAPS[mapname]['nodes'], key=lambda x: x[1])[0]]
    tot = [0.0, 0.0, 0.0]
    for node in nodes:
        for k in range(1, NT + 1):
            a = aura_on(node, SETUPS[vessel][i][3](k), tide_secs(k))
            tot = [tot[j] + a[j] for j in range(3)]
    return tot, nodes

def hand_mining(vessel, mapname):
    """The vessel striking the nearest node in every break (45 s before Tide 1, then 60 s between Tides)."""
    node = min(MAPS[mapname]['nodes'], key=lambda x: x[1])[0]
    return aura_on(node, [striker(HAND[vessel])] if HAND[vessel] else [NONE], OPEN + (NT - 1) * BREAK)

def best(vessel, mapname, upto_tier=3, kind=''):
    """The setup with the most map mining among those of `kind` bought by `upto_tier`."""
    rows = [(sum(map_mining(vessel, i, mapname)[0]), i) for i, st in enumerate(SETUPS[vessel])
            if st[2] <= upto_tier and st[4] == kind]
    return max(rows) if rows else (0.0, None)

def r(x):
    return int(round(x))

def tables():
    out = []
    kills = sum(KILL_RED)
    A = {}
    out.append('### A. One gatherer beside one node, aura a Tide (a 180 s Tide and its 60 s break)\n')
    out.append('Steady rate at Tide 10: the path bought, a Sapling fully grown. Hits a payout: on a Red node (Healthresource 4).\n')
    out.append('| Vessel | Gatherer and path | Cost R/B/Y | Hits a payout | Red node | Orange | Opal | Tides to earn its cost (Red node) |')
    out.append('|---|---|---|---|---|---|---|---|')
    for v, rows in SETUPS.items():
        for i, (name, cost, tier, fn, kind) in enumerate(rows):
            st = [x for x in fn(10) if x['rate']]
            red, ora, opal = steady(v, i, 'Red'), steady(v, i, 'Orange'), steady(v, i, 'Opal')
            n = '/'.join(str(spp('Red', x['ore'], x['dmg'])) for x in st) or '-'
            A['%s|%s' % (v, name)] = dict(red=red, orange=ora, opal=opal, cost=cost, kind=kind)
            label = name + {'proposal': ' *(proposal)*', 'extra': ' *(upside)*', 'proposal upside': ' *(proposal, upside)*'}.get(kind, '')
            out.append('| %s | %s | %d/%d/%d | %s | %d | %d | %d | %s |' % (v, label, cost[0], cost[1], cost[2], n,
                       r(red), r(ora), r(opal), ('%.1f' % (sum(cost) / red)) if red else '-'))
    out.append('')
    out.append('By hand, any vessel but Boutman standing idle by a node strikes it at its Attackspeed (0.5; Spool 0.6) with '
               'Damage 1 or more: **%d a Tide on a Red node, %d on the Opal**, while it does nothing else. So does any idle '
               'melee unit beside a node (Fie\'s knights at 1 a second, Spool\'s Stitched at 0.8).\n'
               % (r(sum(aura_on('Red', [striker(0.5)], CYCLE))), r(sum(aura_on('Opal', [striker(0.5)], CYCLE)))))
    out.append('### B. A map, Tides 1-15: the start, kills and mining\n')
    out.append('Gatherers: one building beside every node within 9 tiles of the core (the nearest node if none is), built '
               'in the opening break, the vessel\'s best mining path through tier 3 bought at Tide 3. By hand: the vessel '
               'striking the nearest node in every break. Aura in all colours, Red in brackets.\n')
    out.append('| | ' + ' | '.join(MAPS) + ' |')
    out.append('|---' * (len(MAPS) + 1) + '|')
    out.append('| Nodes in reach | ' + ' | '.join(', '.join(n for n, d in MAPS[m]['nodes'] if d <= SAFE) or
               '%s (%.0f tiles out)' % MAPS[m]['nodes'][0] for m in MAPS) + ' |')
    out.append('| Start aura | ' + ' | '.join('150 (50)' for m in MAPS) + ' |')
    out.append('| Kills | ' + ' | '.join('%d (%d)' % (kills, kills) for m in MAPS) + ' |')
    B = {}
    rowsB = [(v, '') for v in SETUPS] + [('Yossele', 'proposal'), ('Spool', 'proposal')]
    for v, kind in rowsB:
        cells = []
        for m in MAPS:
            sc, i = best(v, m, 3, kind)
            mm, nodes = map_mining(v, i, m)
            B['%s|%s|%s' % (v, kind, m)] = dict(gatherers=mm, setup=SETUPS[v][i][0])
            cells.append('%d (%d)' % (r(sum(mm)), r(mm[0])))
        out.append('| %s gatherers%s | ' % (v, ' *(proposal)*' if kind else '') + ' | '.join(cells) + ' |')
    out.append('| By hand in the breaks (every vessel but Boutman) | ' + ' | '.join(
        '%d (%d)' % (r(sum(hand_mining('Smacky', m))), r(hand_mining('Smacky', m)[0])) for m in MAPS) + ' |')
    avg = {m: sum(sum(B['%s||%s' % (v, m)]['gatherers']) for v in BASE) / len(BASE) for m in MAPS}
    avgR = {m: sum(B['%s||%s' % (v, m)]['gatherers'][0] for v in BASE) / len(BASE) for m in MAPS}
    hand = {m: sum(hand_mining('Smacky', m)) for m in MAPS}
    out.append('| **Base vessels\' average gatherers** (Smacky, Sno Sno, Fie, Buster) | ' + ' | '.join(
        '**%d** (%d)' % (r(avg[m]), r(avgR[m])) for m in MAPS) + ' |')
    out.append('| **Average aura a map**: start + kills + that average | ' + ' | '.join(
        '**%d**' % r(150 + kills + avg[m]) for m in MAPS) + ' |')
    out.append('| ... and mining by hand in every break too | ' + ' | '.join(
        '%d' % r(150 + kills + avg[m] + hand[m]) for m in MAPS) + ' |')
    out.append('')
    allmaps = sum(150 + kills + avg[m] for m in MAPS) / len(MAPS)
    out.append('Across the three maps a base vessel ends a map with about **%d aura** earned: %d from the start, %d from '
               'kills (all Red) and %d from its gatherers (%d of it Red); mining by hand in every break would add about %d.\n'
               % (r(allmaps), 150, kills, r(sum(avg.values()) / len(MAPS)), r(sum(avgR.values()) / len(MAPS)),
                  r(sum(hand.values()) / len(MAPS))))
    out.append('### C. Yossele\'s and Spool\'s gatherers against the base vessels\'\n')
    out.append('| Map | Base: lowest / average / highest | Yossele as built | × average | Yossele, proposal | × | Spool as built | × | Spool, proposal | × |')
    out.append('|---|---|---|---|---|---|---|---|---|---|')
    C = {}
    for m in MAPS:
        vals = [sum(B['%s||%s' % (v, m)]['gatherers']) for v in BASE]
        av = sum(vals) / len(vals)
        y, yp = sum(B['Yossele||%s' % m]['gatherers']), sum(B['Yossele|proposal|%s' % m]['gatherers'])
        s_, sp = sum(B['Spool||%s' % m]['gatherers']), sum(B['Spool|proposal|%s' % m]['gatherers'])
        C[m] = dict(lo=min(vals), avg=av, hi=max(vals), yossele=y, yossele_prop=yp, spool=s_, spool_prop=sp)
        out.append('| %s | %d / %d / %d | %d | %.1f | %d | %.1f | %d | %.1f | %d | %.1f |' % (
            m, r(min(vals)), r(av), r(max(vals)), r(y), y / av, r(yp), yp / av, r(s_), s_ / av, r(sp), sp / av))
    out.append('')
    return '\n'.join(out), dict(A=A, B=B, C=C, kills=kills, avg_gatherers=avg, avg_red=avgR, hand=hand,
                                avg_aura_map={m: 150 + kills + avg[m] for m in MAPS}, avg_all=allmaps)

if __name__ == '__main__':
    md, data = tables()
    print(md)
    os.makedirs(os.path.join(HERE, 'results'), exist_ok=True)
    with open(os.path.join(HERE, 'results', 'aura_baseline.json'), 'w') as f:
        json.dump(data, f, indent=1)
    if len(sys.argv) > 1:
        open(sys.argv[1], 'w').write(md)
