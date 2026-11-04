"""Hand numbers behind ../ULOE_UPGRADES_2026-11-04.md. Python 3, standard library only.
Run: python3 uloe_math.py > results/uloe_math.txt
Every Uloe number is scope 10's (built defaults and the new ~defaults~); Smacky's are scope 16's, read from the game.
Percent bonuses ADD within a stat (kit pitfall, engine-rules); the Light multiplier and the ramp are separate held mods."""

# ---------------------------------------------------------------- the trees, as written in scope 10
TREES = {  # name: [(level, upgrade, {colour: cost}, leads_to or None = every next, ends)]
    "Magnifying Glass": [
        (1, "Burning Point", {"R": 35}), (1, "Wide Lens", {"R": 30}),
        (2, "Polished Lens", {"R": 90}), (2, "Quick Focus", {"R": 80}), (2, "Prism", {"R": 100}),
        (3, "Sunspot", {"R": 300, "B": 50}), (3, "Focal Point", {"R": 350}),
        (4, "Death Ray", {"R": 3000})],
    "Power Bank": [
        (1, "Short Circuit", {"R": 40}), (1, "Extra Cell", {"Y": 35}),
        (2, "Static Charge", {"R": 90}), (2, "Bright Spark", {"Y": 90}), (2, "Surge Protector", {"Y": 60, "B": 40}),
        (3, "Chain Lightning", {"R": 350}), (3, "Photon Swarm", {"R": 150, "Y": 200}),
        (4, "Supernova", {"R": 2000, "Y": 1000})],
    "Mirror": [
        (1, "Silver Backing", {"B": 30}), (1, "Vanity Lights", {"B": 30}),
        (2, "Glare", {"B": 100}), (2, "Bounce Card", {"B": 100}), (2, "Tempered Glass", {"B": 90}),
        (3, "Hall of Mirrors", {"B": 350}), (3, "Reflection", {"B": 300, "R": 100}),
        (4, "Heliostat", {"B": 2500, "R": 1000})],
    "Flood Light": [
        (1, "Wide Flood", {"Y": 30}), (1, "Long Throw", {"Y": 30}),
        (2, "Halogen Bulb", {"Y": 90}), (2, "High Voltage", {"Y": 100}),
        (3, "Colour Filter", {"Y": 250}), (3, "Searchlight", {"Y": 250, "R": 100}),
        (4, "Stadium Lights", {"Y": 3000}), (4, "Lighthouse", {"Y": 2500, "R": 500})],
}
BAND = {1: (20, 40), 2: (60, 150), 3: (260, 450), 4: (1500, 5000)}  # scope 15's template, sum of aura
KIT = {1: (15, 75), 2: (25, 500), 3: (75, 750), 4: (1500, 5025)}     # kit docs/02 §4, shipped range

def trees():
    print("== 1. Tree shape and prices (scope 15 band, kit docs/02 shipped range) ==")
    for b, ups in TREES.items():
        by = [sum(1 for u in ups if u[0] == l) for l in (1, 2, 3, 4)]
        print(f"{b}: {len(ups)} upgrades, by level {'/'.join(map(str, by))}")
        for l, n, c in ups:
            tot = sum(c.values()); lo, hi = BAND[l]; klo, khi = KIT[l]
            flag = "in band" if lo <= tot <= hi else ("in kit range" if klo <= tot <= khi else "OUT")
            print(f"   L{l} {n:16s} {'+'.join(f'{k}{v}' for k, v in c.items()):14s} = {tot:5d}  {flag}")
    print()

# ---------------------------------------------------------------- Light
def light():
    print("== 2. Light a building can see ==")
    print("Plug In, node: 2 + 0.4 x total aura, at most 10:", [(a, min(10, 2 + 0.4 * a)) for a in (5, 10, 15, 20)])
    print("Plug In, Power Bank: 4 + 2 per upgrade:", [(u, 4 + 2 * u) for u in range(5)], "(Surge Protector ends at 2 upgrades: 8)")
    print("Flood Light aimed: 2 + min(3, 0.2 x aura); +2 with Halogen:", [(a, 2 + min(3, .2 * a), 4 + min(3, .2 * a)) for a in (5, 10, 15)])
    print("Clusters: early (Uloe 1 + a Photon 1) = 2; mid (plugged into a 10-aura node 6 + aimed Flood 4) = 10;")
    print("          late (plugged into a 4-upgrade Bank 12) = 12, the cap; a Bounce Card passes half on.")
    print()

# ---------------------------------------------------------------- the Magnifying Glass against the Archer Tower
def glass_dps(level, L, polished=False, quick=False, ramp=0.0, wide=False):
    dmg = 2 + 0.5 * (level - 1)
    per = 0.4 if polished else 0.2
    mult = (1 + per * min(L, 12)) * (1 + ramp)
    aspd = 0.75 * (1 + (0.5 if quick else 0))
    return dmg * mult * aspd

def archer(level, aspd_add=0.0, aspd_pct=0.0, dmg_add=0.0):
    return (4 + (level - 1) + dmg_add) * (0.7 + aspd_add) * (1 + aspd_pct)

def glass():
    print("== 3. Single-target damage a second: Magnifying Glass (by Light) against Archer Tower ==")
    rows = [
        ("Glass, no upgrade (L1)", lambda L: glass_dps(1, L), 30),
        ("Glass L3 Burning Point (full) + Polished", lambda L: glass_dps(3, L, polished=True, ramp=1.2), 30 + 35 + 90),
        ("Glass L3 Burning Point (full) + Quick Focus", lambda L: glass_dps(3, L, quick=True, ramp=1.2), 30 + 35 + 80),
        ("Glass L5 BP, Polished, Sunspot, Death Ray", lambda L: glass_dps(5, L, polished=True, ramp=2.5), 30 + 35 + 90 + 350 + 3000),
        ("Glass L5 BP, Quick Focus, Sunspot, Death Ray", lambda L: glass_dps(5, L, quick=True, ramp=2.5), 30 + 35 + 80 + 350 + 3000),
    ]
    print(f"{'':48s} {'L0':>6s} {'L4':>6s} {'L8':>6s} {'L12':>6s}   aura")
    for n, f, c in rows:
        print(f"{n:48s} " + " ".join(f"{f(L):6.1f}" for L in (0, 4, 8, 12)) + f"   {c}")
    print("Archer, no upgrade (L1)                           %.1f   25" % archer(1))
    print("Archer L3 Rapid Fire, Auto Bow                    %.1f   105" % archer(3, .5, .5))
    print("Archer L4 Rapid Fire, Auto Bow, Bow Squad         %.1f   655" % archer(4, .5, 1.5, 4))
    print("Archer L5 Rapid, Eagle, Assassin, Death Mark vs a 1,000-health foe  %.1f   5,235" % archer(5, .5, 0, 28))
    print("Sunspot / Death Ray also Illuminate the target: +15/30/45% from every source, Uloe's Photons and teammates too.")
    print()
    print("Crowds: Wide Lens, Prism, Focal Point (L4, the branch ends): damage a second to EACH foe on the line")
    for L in (0, 4, 8, 12):
        print(f"   Light {L:2d}: {glass_dps(4, L):5.1f} each, side beams {0.5*glass_dps(4, L):4.1f}")
    print("Bomb Tower L4 Dynamite, Splitting Bombs: (3+6+2.5) x 0.35 = %.1f to each of up to 15 foes" % ((3 + 6 + 2.5) * 0.35))
    print()

# ---------------------------------------------------------------- the Mirror against the Wall
def mirror():
    print("== 4. Effective health at level 5 (damage it takes before it falls) ==")
    base = 100 + 40 * 4
    tg = base + 200
    print("Smacky's Wall L5, Reinforced, Stone Wall, Steel Wall: health", 50 + 150 * 4 + 50 + 75 + 175)
    for name, hp, red in (("Mirror L5 Silver, Glare, Reflection (no Tempered)", base, 0.0),
                          ("Mirror L5 Silver, Tempered Glass, Reflection", tg, 0.2)):
        for L, blind in ((0, 0), (1, 2), (3, 3), (6, 3)):
            glare = blind * 0.2 if "Glare" in name else 0
            ehp = hp / (1 - red) / (1 - [0, .15, .3, .45][blind]) / (1 - glare)
            print(f"   {name:52s} Light {L}: Blind {blind} -> {ehp:6.0f}")
    print("Reflection hands back 30% (60% from Light 6) of what it takes: a lit Mirror that takes 1,000 returns 600.")
    print()

# ---------------------------------------------------------------- the Flood Light's mining
def flood():
    print("== 5. Mining, aura a Tide on a Red node (the Loom's strike rate: 96; a Mining Hut 18, AURA_BASELINE) ==")
    for n, m in (("Flood Light, unlit", 1.0), ("Flood Light, Light 4", 1.4), ("High Voltage, Light 4", 2.0),
                 ("High Voltage, Light 8", 2.4), ("Stadium Lights (x2, two nodes), Light 8", 2 * 2 * 1.8)):
        print(f"   {n:42s} {96*m:6.0f}")
    print("Colour Filter pays 70%% in the picked colour: on a Red node 96 x 1.4 x 0.7 = %.0f Blue or Yellow a Tide" % (96 * 1.4 * .7))
    print()

# ---------------------------------------------------------------- the Power Bank
def bank():
    print("== 6. Power Bank ==")
    print("Photon damage a second: 0.5 x 0.6 = 0.3; Static Charge 0.6; Chain Lightning 3 foes = 1.8 a Photon")
    for n, u in (("base", 2), ("Extra Cell", 3), ("Extra Cell, Photon Swarm", 5)):
        print(f"   {n:28s} Photons {u}, Light to a tower they visit 1 each (2 with Bright Spark)")
    print("Illuminate is the Bank's real damage: level 2 on a crowd = +30% from every tower, Uloe's and teammates'.")
    print("Supernova: 20 damage, Blind 3 + Illuminate 3 (+45%, -45%) on every foe within 2 tiles of every Photon, once a Tide.")
    print("Plug In from the Bank: 4 / 6 / 8 / 10 / 12 by upgrades, so the Bank's tree is also Uloe's Light tree.")
    print()

if __name__ == "__main__":
    trees(); light(); glass(); mirror(); flood(); bank()
