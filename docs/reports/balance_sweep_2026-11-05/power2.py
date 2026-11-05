"""What a player carries into map k of a campaign, as a damage-equivalent multiplier: ../mode_balance_2026-11-05/power.py
(imported, not changed) brought up to the build of 2026-11-05 and to the sweep's proposals.

What changed from the review's estimate:
- The shelf is the built one: the review's eight Directions and 25 stock Cardinals, plus the sheets built since
  (PRs #103-#114): 13 Directions any vessel can take and Smacky's Mason's Trowel (the Duos need two players and are left
  out), 11 Trinkets (six of them stack to 6) and 4 Cardinals. Their values (NEW_* below) are estimates from their
  numbers in RogueWavesMode/Code/22_AnyDirections.cs, 23_BaseDirections.cs and 24_Trinkets.cs, as the review valued the
  stock ones; Punch Card is a price cut, Pearl Oyster and Cargo Crate are left out (they buy nothing by themselves).
- P1: objectives from the break before Tide 3, so Tides 1 and 2 post none: the mid case loses about 4 Pure Aura a map
  (two objectives of 2-4, two in three won), the high case 6, the low case none (it counts no objectives).
- C1's ramp is on in every case that ends in "_ramp" (+RAMP a held Cardinal or Trinket copy).
- `items`: what F5 counts at the map's start: Cardinals and Trinket copies bought, and Direction ranks held (the pick
  before the map included, what its own breaks buy not).

    python3 power2.py [out.json]
"""
import json, os, sys
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, '..', 'mode_balance_2026-11-05'))
import power as P

RAMP = 3
DIRECTIONS = dict(P.DIRECTIONS)
# value a rank I / II / III held, as a share of the player's whole output (the review's scale: Spring Tide 3/6/10%)
NEW_DIRECTIONS = {
    'Wound Spring': (0.03, 0.06, 0.09), 'Fine Sieve': (0.02, 0.04, 0.06), 'Spare Hands': (0.02, 0.04, 0.06),
    'Galley Kettle': (0.03, 0.06, 0.09), 'Chain Shot': (0.03, 0.06, 0.09), 'Salt Barrel': (0.02, 0.04, 0.06),
    'Fog Horn': (0.02, 0.04, 0.06), 'Clam Shell': (0.01, 0.02, 0.03), 'Notched Oar': (0.03, 0.05, 0.08),
    'Lamp Oil': (0.02, 0.04, 0.06), 'Fish Hook': (0.02, 0.04, 0.06), 'Spare Compass': (0.01, 0.02, 0.03),
    'Magpie Nest': (0.02, 0.04, 0.06), "Mason's Trowel": (0.02, 0.04, 0.06),
}
DIRECTIONS.update(NEW_DIRECTIONS)
# (value a copy, copies that work, price): Oil Can +5% tower Attackspeed a copy; Gutting Knife the vessel's hits on
# healthy foes (about a fifth of output, half its hits); the rest small or sustain.
NEW_TRINKETS = {
    'Oil Can': (0.04, 6, 12), 'Gutting Knife': (0.02, 6, 12), 'Brass Spyglass': (0.02, 1, 12),
    'Tar Bucket': (0.01, 6, 15), 'Rubber Duck': (0.005, 3, 10), 'Spare Plank': (0.02, 1, 15),
    'Bottled Storm': (0.01, 6, 15), 'Oilskin Coat': (0.01, 6, 12), 'Gull Feather': (0.02, 1, 15),
    'Signal Flare': (0.015, 6, 15), 'Tin Whistle': (0.01, 1, 15), 'Cork Lifebuoy': (0.01, 1, 25),
}
CASES = {
    'low':  dict(income=50, scale=0.6),
    'mid':  dict(income=81, scale=1.0),     # 85 less P1's two early objectives
    'high': dict(income=109, scale=1.5),    # 115 less P1's
}

def carried(case, maps=10, ramp=0, shelf='built'):
    """shelf 'review': the review's eight Directions and stock Cardinals only; 'built': with the sheets built since."""
    c = CASES[case]; s = c['scale']
    dirs = P.DIRECTIONS if shelf == 'review' else DIRECTIONS
    held = {}
    ranked = sorted(P.CARDINALS.items(), key=lambda kv: -kv[1])
    lobby_sum = sum(v for _, v in ranked[:P.LOBBY])
    # every buyable copy: (name, value, price); the lobby already took the best five stock ones
    stockpile = [(n, v, P.COST['stock']) for n, v in ranked[P.LOBBY:] if v > 0]
    stockpile += [('Dentures', P.DENTURES[0], P.COST['dentures'])] * P.DENTURES[1]
    if shelf != 'review':
        for n, (v, k, price) in NEW_TRINKETS.items():
            stockpile += [(n, v, price)] * k
    bought = []; pure = 0.0; out = []
    order = sorted(dirs, key=lambda d: -dirs[d][2])
    def d_sum(): return sum(dirs[d][held[d] - 1] for d in held) * s
    def c_sum(): return sum(v for _, v, _ in bought) * s
    def items(): return len(bought) + sum(held.values())
    def shop(budget):
        while True:
            opts = []
            for d, r in held.items():
                if r < 3:
                    gain = (dirs[d][r] - dirs[d][r - 1]) * s
                    cost = P.COST['rank3' if r == 2 else 'rank2']
                    opts.append((gain / cost, 'd', d, cost))
            left = [x for x in stockpile if x not in bought or stockpile.count(x) > bought.count(x)]
            if left:
                best = max(left, key=lambda x: x[1] / (x[2] + ramp * len(bought)))
                pc = best[2] + ramp * len(bought)
                opts.append((best[1] * s * (1 + d_sum()) / (1 + lobby_sum * s + c_sum()) / pc, 'c', best, pc))
            opts = [o for o in opts if o[3] <= budget and o[0] > 0]
            if not opts: return budget
            o = max(opts, key=lambda o: o[0])
            budget -= o[3]
            if o[1] == 'd': held[o[2]] += 1
            else: bought.append(o[2])
    for k in range(1, maps + 1):
        new = [d for d in order if d not in held]
        if new: held[new[0]] = 1
        else:
            up = [d for d in order if held[d] < 3]
            if up: held[up[0]] += 1
        at_start = items()
        pure = shop(pure + c['income'] / 2)
        p = (1 + d_sum()) * (1 + c_sum() / (1 + lobby_sum * s))
        out.append(dict(map=k, power=round(p, 3), directions=round(1 + d_sum(), 3), cardinals=round(1 + c_sum() / (1 + lobby_sum * s), 3),
                        items=at_start, ranks=sum(held.values()), bought=len(bought)))
        pure = shop(pure + c['income'] / 2)
    return out

def all_cases():
    res = {}
    for case in CASES:
        res[case] = carried(case)
        res[case + '_ramp'] = carried(case, ramp=RAMP)
    res['mid_review_ramp'] = carried('mid', ramp=RAMP, shelf='review')
    return res

if __name__ == '__main__':
    res = all_cases()
    for c, rows in res.items():
        print('%-16s' % c, ' '.join('%d:%.2f' % (r['map'], r['power']) for r in rows), '| items at start', [r['items'] for r in rows])
    if len(sys.argv) > 1: json.dump(res, open(sys.argv[1], 'w'), indent=1)
