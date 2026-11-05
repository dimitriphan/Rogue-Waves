"""What a player carries into map k of a campaign, as a damage-equivalent multiplier (the `power` of mode.py).

An estimate, not a measure: each Direction rank and each Cardinal is given a value (VALUE below, as a share of the
player's whole output it adds), Pure Aura comes in at INCOME a map, and the player buys greedily by value per Pure Aura
at the built prices (Directions rank II 20, III 30; stock Cardinals 15, held once; Vampire Dentures 15, six copies).
Within a group values add (the engine adds mods of one kind, kit docs/17 §2.1); the groups multiply. Map k counts what
was bought before it and half of what its own breaks buy. The lobby's own Cardinal picks are left out on both sides:
the base game has them too (kit docs/15 §7), so the shop counts only past the best five.

    python3 power.py            prints the three cases, maps 1-10
"""
import json, sys

# The built Directions a base vessel (Smacky) can draw (RogueWavesMode 16_Directions.cs), value per rank I / II / III held.
# Cut Costs -10/-20/-30% on buildings is x1.11/1.25/1.43 aura; Cheap Refits on upgrades (about half of a run's spend);
# the tower sustain three are small next to a run's damage; Skipping Stones is the vessel's own share (about a fifth).
DIRECTIONS = {
    'Cut Costs': (0.11, 0.25, 0.43), 'Cheap Refits': (0.05, 0.11, 0.18), 'Compound Interest': (0.02, 0.04, 0.06),
    'Spring Tide': (0.03, 0.06, 0.10), 'Tidal Mend': (0.03, 0.06, 0.10), 'Barnacle Coat': (0.02, 0.05, 0.08),
    'Made of Tougher Stuff': (0.01, 0.02, 0.03), 'Skipping Stones': (0.03, 0.06, 0.10),
}
# The 25 stock Cardinals on the shelf (17_Cardinals.cs NotSold left out), value when held, from their in-game lines
# (kit reference/roguelab_round1.txt). Trade-offs (Glass Core, Frail Titan, Diorama) and pets count 0.
CARDINALS = {
    'Wave Breaker': 0.15, 'Rusty Pipe': 0.10, 'Voice in a Bottle': 0.10, 'Cubed Drink': 0.10,
    'Red Coin': 0.08, 'Yellow Coin': 0.08, 'Green Coin': 0.08, 'Blue Coin': 0.08,
    'Sport Water': 0.05, 'Processor Jr': 0.05, 'Opal Core': 0.05, 'Torrid Badge': 0.05, 'Aura Gavel': 0.05,
    'Blue Paint': 0.05, 'Fragment': 0.05, 'Starter Seeds': 0.04, 'Tacklebox': 0.04, 'Spear Point': 0.04,
    'Paper Tower': 0.03, 'Fish Bait': 0.03, 'Chimera Crystals': 0.02, 'Beast Tassle': 0.02, '1st Grade Textbook': 0.02,
    'Fluffy Slippers': 0.01, 'Glass Core': 0.0, 'Frail Titan': 0.0, 'Diorama': 0.0, 'Kippy': 0.0, 'Bendigo': 0.0,
}
RAMP = 3                 # proposal C1's default
LOBBY = 5                # the base game's own picks: one per colour and a Mixed slot
DENTURES = (0.03, 6)     # value a copy, copies that work
COST = dict(rank2=20, rank3=30, stock=15, dentures=15)

# Pure Aura a map for one player: 15 Tides x 3, the break objectives (one a break, team pay, 2-4 each), weather (+1 a
# Tide on about 1 map in 3 then 1 in 2), a few bonus Tides at 2.
CASES = {
    'low':  dict(income=50, scale=0.6),     # Tides only, a little weather; values x0.6
    'mid':  dict(income=85, scale=1.0),     # + objectives won about 2 Tides in 3, 2 bonus Tides
    'high': dict(income=115, scale=1.5),    # every objective, 6 bonus Tides; values x1.5
}

def carried(case, maps=10, directions=DIRECTIONS, ramp=0):
    """ramp: Pure Aura added to a Cardinal's or Trinket's price for each one the player already holds (proposal C1)."""
    c = CASES[case]; s = c['scale']
    held = {}                                        # Direction -> rank
    cards = sorted(CARDINALS.items(), key=lambda kv: -kv[1])[LOBBY:]   # the lobby already took the best five
    lobby_sum = sum(v for _, v in sorted(CARDINALS.items(), key=lambda kv: -kv[1])[:LOBBY])
    bought = []; dentures = 0; pure = 0.0; out = []
    order = sorted(directions, key=lambda d: -directions[d][2])
    def d_sum(): return sum(directions[d][held[d] - 1] for d in held) * s
    def c_sum(): return (sum(v for n, v in bought) + DENTURES[0] * min(dentures, DENTURES[1])) * s
    def shop(budget):
        nonlocal dentures
        while True:
            opts = []
            for d, r in held.items():
                if r < 3:
                    gain = (directions[d][r] - directions[d][r - 1]) * s
                    opts.append((gain / COST['rank3' if r == 2 else 'rank2'], 'd', d, COST['rank3' if r == 2 else 'rank2']))
            rest = [(n, v) for n, v in cards if (n, v) not in bought]
            if rest:
                n, v = rest[0]
                pc = COST['stock'] + ramp * (len(bought) + dentures)
                opts.append((v * s * (1 + d_sum()) / (1 + lobby_sum * s + c_sum()) / pc, 'c', (n, v), pc))
            if dentures < DENTURES[1]:
                pd = COST['dentures'] + ramp * (len(bought) + dentures)
                opts.append((DENTURES[0] * s / pd, 'x', None, pd))
            opts = [o for o in opts if o[3] <= budget and o[0] > 0]
            if not opts: return budget
            o = max(opts)
            budget -= o[3]
            if o[1] == 'd': held[o[2]] += 1
            elif o[1] == 'c': bought.append(o[2])
            else: dentures += 1
    for k in range(1, maps + 1):
        # the pick before map k: a new Direction while any is left (best first), else a rank of the best held
        new = [d for d in order if d not in held]
        if new: held[new[0]] = 1
        else:
            up = [d for d in order if held[d] < 3]
            if up: held[up[0]] += 1
        pure = shop(pure + c['income'] / 2)          # the first half of this map's breaks
        p = (1 + d_sum()) * (1 + c_sum() / (1 + lobby_sum * s))
        out.append(dict(map=k, power=round(p, 3), directions=round(1 + d_sum(), 3), cardinals=round(1 + c_sum() / (1 + lobby_sum * s), 3),
                        held=dict(held), bought=len(bought), dentures=dentures))
        pure = shop(pure + c['income'] / 2)          # the second half
    return out

if __name__ == '__main__':
    res = {c: carried(c) for c in CASES}
    res.update({c + '_ramp': carried(c, ramp=RAMP) for c in CASES})
    for c, rows in res.items():
        print(c, ' '.join('%d:%.2f' % (r['map'], r['power']) for r in rows), '| stock bought by map 10:', rows[-1]['bought'])
    if len(sys.argv) > 1: json.dump(res, open(sys.argv[1], 'w'), indent=1)
