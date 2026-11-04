"""The review's hand numbers (../ARGO_REVIEW_2026-11-04.md §2-3): Argo's buildings against the stock ones, per aura.
python3 argo_math.py prints every table. Numbers from 02_Data.cs (Argo 2.0), the stock ones from sim.py (as the
balance studies used them) and the survey; a Tide is the 180 s Tide and its 60 s break (240 s), as in the aura
baseline. A Red node pays 5 aura a payout."""
import math

TIDE = 240.0
PAY = 5.0

def hpp(ore, dmg):
    return math.ceil(4.0 / ore - 1e-9) + math.ceil(1.0 / dmg - 1e-9)

def drill(F, mantle=False, excess=False, yield_=0.75, speed=0.1, mantle_speed=False):
    below = F - 1
    dmg = 0.5 * (1 + yield_ * below) * (2 if mantle and not mantle_speed else 1)
    aspd = 0.25 * (1 + speed * below) * (2 if mantle and mantle_speed else 1)
    ore = dmg if excess else min(1.0, dmg)
    h = hpp(ore, dmg)
    return dmg, aspd, h, TIDE * aspd / h * PAY

def mining_table():
    print('## Drill Rig on a Red node, aura a Tide')
    print('| Fathom | Damage (ore a bite) | bites/s | bites a payout | as coded | if the excess counted |')
    for F in range(1, 8):
        d, a, h, coded = drill(F)
        _, _, h2, ex = drill(F, excess=True)
        print('| %d | %.3f | %.3f | %d | %.0f | %.0f (%d) |' % (F, d, a, h, coded, ex, h2))
    for F in (5, 7):
        print('| Mantle Tap, F%d | | | | %.0f | %.0f |' % (F, drill(F, mantle=True)[3], drill(F, mantle=True, excess=True)[3]))
        print('| Mantle Tap as "strikes twice as fast", F%d | | | | %.0f | |' % (F, drill(F, mantle=True, mantle_speed=True)[3]))
        print('| Mantle Tap as the review has it, strikes x1.5, F%d | | | | %.0f | |' % (F, drill(F, mantle=True, mantle_speed=True)[3] * 0.75))
    print('Stock, same node (AURA_BASELINE §3): Hut 18, Sno Box 18, Silo 17, Checkpoint 36; Sapling 60/90, Loom 96.')
    tot = 0
    for t in range(0, 16):
        F = min(5, t + 1)   # built in the opening break: Fathom 2 at Tide 1
        tot += drill(F)[3]
    print('A Drill built in the opening break, Tides 0-15: %.0f aura on a Red node (Hut: %.0f)' % (tot, 18 * 16))
    for strikes in (12, 20, 28):
        print('Gusher %d bites a Tide at Fathom 5+ (1 Damage-1 strike each): %.0f aura on a Red node, payback %.1f Tides of Yellow 275' % (strikes, strikes / 5 * PAY, 275 / (strikes / 5 * PAY)))
    print()

def dps_table():
    print('## Damage a second, per Red spent (one target / a packed crowd)')
    rows = [
        ('Archer Tower (R25)', 25, 4.0 * 0.7, 4.0 * 0.7),
        ('Bomb Tower (R25 B25), radius 1.5', 50, 3.0 * 0.35, 3.0 * 0.35 * 4),
        ('Scatter Cannon (R30), 6 pellets', 30, 1.25 * 0.6 * 1.3, 1.25 * 0.6 * 6),
        ('Scatter on slowed foes (+50%)', 30, 1.25 * 0.6 * 1.3 * 1.5, 1.25 * 0.6 * 6 * 1.5),
        ('Harpoon Gun (R35), pierce 3', 35, 5 * 0.35, 5 * 0.35 * 3),
        ('Argo (vessel), cleave 60%', 0, 4 * 0.4, 4 * 0.4 * 1.6),
        ('Smacky (vessel)', 0, 4 * 0.5, 4 * 0.5),
    ]
    print('| Building | cost | one target | crowd | crowd per 10 aura |')
    for n, c, one, crowd in rows:
        print('| %s | %d | %.2f | %.2f | %s |' % (n, c, one, crowd, ('%.2f' % (crowd / c * 10)) if c else '-'))
    print('(One target for the Scatter: about 1.3 of 6 pellets reach a lone foe 3 tiles out in a 50 degree cone.)')
    print()

def cone_table():
    print('## Pellets that stay in a lane 1.5 tiles wide')
    print('| distance | 50 deg, 6 pellets | 75 deg (Wide Bore), 8 pellets | Wide Bore as reviewed, 60 deg, 8 pellets |')
    for d in (1.5, 2, 3, 4, 4.5):
        def inl(n, sp):
            w = 2 * d * math.tan(math.radians(sp) / 2)
            return n * min(1.0, 1.5 / w)
        print('| %.1f | %.1f | %.1f | %.1f |' % (d, inl(6, 50), inl(8, 75), inl(8, 60)))
    print()

def slow_table():
    print('## What a slow really is: a re-tick that outlives its tick stacks (Speed Multi mods sum, floor 10%)')
    print('| Source | written | tick / lasts | slows on a foe at once | real slow |')
    for name, s, tick, last in [('Deep Water', 0.30, 0.25, 0.5), ('Deep Water, Undercurrent and 2 levels', 0.45, 0.25, 0.5),
                                ('Tremor, Fathom 1', 0.20, 0.5, 0.75), ('Tremor, Fathom 5', 0.36, 0.5, 0.75),
                                ('Tremor, Fathom 5, Seismic, Old Salts', 0.56, 0.5, 0.75)]:
        k = last / tick
        lo, hi = math.floor(k), math.ceil(k)
        real_lo = min(0.9, lo * s); real_hi = min(0.9, hi * s)
        print('| %s | %d%% | %.2f / %.2f s | %d-%d | %d-%d%% |' % (name, s * 100, tick, last, lo, hi, real_lo * 100, real_hi * 100))
    print('Time a Dagger (speed 1) spends crossing 6 pools: written 30%%: %.1f s; as coded 60%%: %.1f s; 90%%: %.1f s' % (6 / 0.7, 6 / 0.4, 6 / 0.1))
    print()

def pin_table():
    print('## Pins: who makes them and how often (the Scatter, Salt Sting, Bedrock and "held twice as long" read them)')
    print('Anchor Line: the first foe a harpoon hits, every %.2f s (Attackspeed 0.35): 1 s, 2 s in water -> %.0f%%-%.0f%% of one foe\'s time' % (1 / 0.35, 0.35 * 100, 0.7 * 100))
    print('Mantle Tap quake: once a Tide, 2 s on what stands in the Tremor -> under 1% of a 240 s Tide')
    print('Kelp Bed catch and Trench drag hold foes but are not pins in the code (ArgoUtil.Root, not ArgoStatus.Pinned)')
    print('Bedrock on Anchor Line\'s pins: 1.5 x 0.35-0.7 = %.2f-%.2f damage a second for Blue 80 + Red 30' % (1.5 * 0.35, 1.5 * 0.7))
    print('Salt Sting: +0.5 a second on a pinned foe in water: %.2f a second for Blue 70 + Red 30' % (0.5 * 0.7))
    print()

def value_table():
    print('## Upgrades that do less than their price says')
    print('Abyss (B260): a free pool a FLOOD, worth B15 -> pays back after 17 FLOODs')
    print('Gusher (Y275): 12 bites a Tide = 2.4 payouts = 12 aura on a Red node -> 23 Tides to pay back (a map is 15)')
    print('Mantle Tap (Y3000): "x2 ore" -> +0 aura (ore a bite is capped at 1, Damage is already 1.25 at Fathom 3)')
    print('Deep Core (Y120): Fathom 7 bites 14%% faster than 5 -> +%.0f aura a Tide on a Red node, plus 0.8 tiles and 8%% Tremor' % (drill(7)[3] - drill(5)[3]))
    print('Double Barrel (R30): one stored volley after a quiet spell, about 1 extra volley a Tide')
    print()

if __name__ == '__main__':
    mining_table(); dps_table(); cone_table(); slow_table(); pin_table(); value_table()
