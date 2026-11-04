# Uloe's upgrades, 2026-11-04: from 8 to 32

> Frozen as written. Asked by Dimitri, 2026-11-04 05:39Z: "Develop upgrades for Uloe until they're fully fleshed out."
> Cloud only: no PC, nothing built. **Scope `10` changed**; Uloe's code did not. Every new name, cost and number is a
> Claude `~default~` until Dimitri takes it; the next Uloe build implements what he keeps.

**In short.**

1. **Uloe had 8 upgrades where a base vessel has 38 to 41.** The Magnifying Glass and the Power Bank had one each, the
   Flood Light two, the Mirror four; no building reached level 3 except the Mirror, and none had a capstone (§1).
2. **Every building now has 8**, like Yossele's and Spool's trees: two level-1 roots, two or three level-2 choices,
   two level-3 choices, a level-4 capstone (the Flood Light has two), branches that cross, and one that ends early on
   each building. Prices sit in scope `15`'s bands (§3). Dimitri's eight lines keep his words and the names they were
   built with; the 24 new ones are all `~defaults~`.
3. **Light stays the point.** Most new upgrades either make Light worth more (Polished Lens, Sunspot, Searchlight,
   Heliostat, Lighthouse) or bring more of it (Bright Spark, Halogen Bulb, Stadium Lights, and every Power Bank upgrade,
   since Plug In from a Bank gives Light 4 + 2 per upgrade). The Power Bank's tree is therefore also Uloe's Light tree.
4. **Five holes are filled** (§2): no answer to crowds (Prism, Focal Point), no colour trade (Colour Filter), a frail
   vessel that must stand still to plug in (Surge Protector), nothing that slows (Searchlight, Lighthouse), and no
   capstones.
5. **The hand numbers say it sits where it should** (§4): unlit, Uloe's towers are weaker than Smacky's; at Light 8
   they match a fully upgraded Archer Tower and Wall; at Light 12, the cap, the Glass passes the Archer by a quarter or more. A Flood Light
   mines like Spool's Loom (96 aura a Tide on a Red node unlit), so it follows whatever Dimitri decides for the pack's
   gatherers (PLAN Open question 4).

## 1. Where it started

Scope `10` had eight upgrade lines, Dimitri's, with no names (`{}`) and levels Claude guessed (`~1~`, `~2~`). Uloe
0.1.0-dev built all eight with names (`TDS_Mod/ModSource/Uloe/DESIGN.md` "Upgrade trees") and says the trees are
short and his to extend. Against the base vessels (`UPGRADE_GAPS_2026-11-04.md` §1: 8 to 12 a building, a capstone
on each) and the kit (`TDS_Vessel_Creator/docs/02_DESIGN_RULES.md` §4: 7-11, 1-3 roots, cross-overs, one early end,
a tier-5 capstone):

| Building | Before | Now | By level (before → now) |
| --- | --- | --- | --- |
| Magnifying Glass | 1 | 8 | 1/0/0/0 → 2/3/2/1 |
| Power Bank | 1 | 8 | 1/0/0/0 → 2/3/2/1 |
| Mirror | 4 | 8 | 2/2/0/0 → 2/3/2/1 |
| Flood Light | 2 | 8 | 2/0/0/0 → 2/2/2/2 |

## 2. The holes, and what fills them

| Hole | Why it matters | Filled by |
| --- | --- | --- |
| **Crowds.** Uloe's only damage is one beam at one foe; the burst of a falling Photon does 3 | every base vessel has a splash tower (Bomb, Sno Cannon, Boutman's fish); Tides 10-15 are crowds | Glass: **Wide Lens → Prism → Focal Point** (the beam splits, then pierces a line); Bank: **Chain Lightning** |
| **No colour trade.** Uloe spends Red (Glass, Bank), Blue (Mirror) and Yellow (Flood Light, Bank) | the same hole Yossele (Wood Wide Web) and Spool (Dyed in the Wool) had filled; Woozy and The Hunt are short of a colour | Flood Light: **Colour Filter** (70% in the picked colour, Smacky's Processing Plant's level and price) |
| **A frail vessel parked.** Uloe has 30 health and Plug In keeps it within 5 tiles of what it plugged into | Uloe dies where its cluster is, the one spot foes are aimed at | Bank: **Surge Protector** (40% less damage and a little healing while plugged into that Bank). It ends the branch early, so a safe socket gives at most Light 8, not 12: safety or brightness |
| **No slow.** Blind lowers what foes deal and Illuminate what they take; nothing holds a foe in a tower's range | every base vessel has a slow or a stun | Flood Light: **Searchlight** (foes in its light slowed 20%, one slow a Flood Light, the lesson of `ARGO_REVIEW_2026-11-04.md`), **Lighthouse** |
| **No capstones** | the kit asks for one per building that changes how it plays | **Death Ray** (the beam keeps its heat between targets), **Supernova** (a SURGE button: every Photon bursts at once), **Heliostat** (the wall becomes a turret), **Stadium Lights** / **Lighthouse** (a lamp for the whole base, or a sweeping beam) |

## 3. The trees

Levels are scope `10`'s (`% 1` to `% 4`, the game's tiers 2-5). "All" means it leads to every choice of the next level.
Bold names are Dimitri's lines (his words, the built name). Costs sum: level 1 20-40, 2 60-150, 3 260-450, 4 1500-5000.

### Magnifying Glass (Offense, Red)

| L | Upgrade | Does (numbers) | Leads to | Cost |
| --- | --- | --- | --- | --- |
| 1 | **Burning Point** | +12% damage per beam on one target, at most +120% | Polished Lens, Quick Focus | Red 35 |
| 1 | Wide Lens | range +1.5 tiles | Quick Focus, Prism | Red 30 |
| 2 | Polished Lens | +40% damage per Light instead of +20% | Sunspot | Red 90 |
| 2 | Quick Focus | attack speed +50% | Sunspot, Focal Point | Red 80 |
| 2 | Prism | 2 more beams at foes within 1.5 tiles of the target, 50% damage | Focal Point | Red 100 |
| 3 | Sunspot | the target is Illuminated: level 1 from Light 4, 2 from 8, 3 at 12 | Death Ray | Red 300 + Blue 50 |
| 3 | Focal Point | full damage to every foe on the line out to its range; Prism's side beams pierce too | ends | Red 350 |
| 4 | Death Ray | ramp +12% a beam to +250%, kept across targets, lost after 3 s idle; at full heat Illuminate 3 | | Red 3000 |

Polished Lens against Quick Focus is a real choice: Quick Focus is better below Light 5, Polished Lens above it (§4).

### Power Bank (Support, Red + Yellow)

| L | Upgrade | Does (numbers) | Leads to | Cost |
| --- | --- | --- | --- | --- |
| 1 | **Short Circuit** | a falling Photon: 3 damage within 1.5 tiles, Blind 2, Illuminate 2 | Static Charge, Surge Protector | Red 40 |
| 1 | Extra Cell | units +1 | all | Yellow 35 |
| 2 | Static Charge | Photon damage +100%; hits Blind and Illuminate at level 2 | all | Red 90 |
| 2 | Bright Spark | a Photon gives Light 2 within 2.5 tiles | all | Yellow 90 |
| 2 | Surge Protector | Uloe plugged into this Bank: 40% less damage, heals 2 a second | ends | Yellow 60 + Blue 40 |
| 3 | Chain Lightning | each Photon hit jumps to 2 more foes within 2 tiles, same marks | all | Red 350 |
| 3 | Photon Swarm | units +2, leash 5 tiles | all | Red 150 + Yellow 200 |
| 4 | Supernova | SURGE, once a Tide: every Photon bursts for 20 within 2 tiles, Blind 3 + Illuminate 3, all come back at once; Light 3 for 15 s | | Red 2000 + Yellow 1000 |

### Mirror (Defense, Blue)

| L | Upgrade | Does (numbers) | Leads to | Cost |
| --- | --- | --- | --- | --- |
| 1 | **Silver Backing** | Blind one level stronger (his "Blind or Illuminate ... based on upgrade path", the Blind path) | all | Blue 30 |
| 1 | **Vanity Lights** | Illuminates what hits it instead (the same line, the other path) | all | Blue 30 |
| 2 | **Glare** | a hit from a foe Blinded at level N: N x 20% chance to do nothing | all | Blue 100 |
| 2 | **Bounce Card** | 50% of its Light to your buildings within 3 tiles | all | Blue 100 |
| 2 | Tempered Glass | health +200% of base, 20% less damage | all | Blue 90 |
| 3 | Hall of Mirrors | foes within 2 tiles of the attacker get the same mark | Heliostat | Blue 350 |
| 3 | Reflection | 30% of each hit back at the attacker, 60% from Light 6 | ends | Blue 300 + Red 100 |
| 4 | Heliostat | beams the nearest foe within 4 tiles once a second for 1.5 per Light, with its mark; still a wall | | Blue 2500 + Red 1000 |

### Flood Light (Economy, Yellow)

| L | Upgrade | Does (numbers) | Leads to | Cost |
| --- | --- | --- | --- | --- |
| 1 | **Wide Flood** | 60% of its Light within 3.5 tiles; no AIM | all | Yellow 30 |
| 1 | **Long Throw** | 80% of its Light to every building on an 8-tile line | all | Yellow 30 |
| 2 | Halogen Bulb | Light out +2 | all | Yellow 90 |
| 2 | High Voltage | strikes +60% faster | all | Yellow 100 |
| 3 | Colour Filter | COLOUR button: payouts in the picked colour at 70% | ends | Yellow 250 |
| 3 | Searchlight | foes in its light: Illuminate 1 (2 at Light out 5+), slowed 20% | all | Yellow 250 + Red 100 |
| 4 | Stadium Lights | 100% of its Light within 5 tiles, AIM back and doubled; strikes twice as fast, and a second node | | Yellow 3000 |
| 4 | Lighthouse | the beam sweeps a full circle every 6 s, 9 tiles: buildings keep 60% of its Light for 6 s, foes Blinded and Illuminated 2; no AIM | | Yellow 2500 + Red 500 |

Colour Filter is the Flood Light's early end, as Smacky's Marketplace is the Mining Hut's: the trade or a capstone.

## 4. The numbers check

`uloe_upgrades_2026-11-04/uloe_math.py`, printout in `results/uloe_math.txt`. Smacky's numbers are scope `16`'s.

**Light a building can see.** Early, Uloe and a passing Photon: 2. Mid, plugged into a 10-aura node (6) with an aimed
Flood Light (4): 10. Late, plugged into a Power Bank with four upgrades: 12, the cap. A Surge Protector Bank stops at 8.

**One foe, damage a second** (Burning Point or Death Ray at full heat, i.e. an elite or a boss; against a stream of
small foes divide the level-3 rows by 2.2):

| | Light 0 | 4 | 8 | 12 | Aura |
| --- | --- | --- | --- | --- | --- |
| Glass, bare | 1.5 | 2.7 | 3.9 | 5.1 | 30 |
| Glass L3, Burning Point + Polished Lens | 5.0 | 12.9 | 20.8 | 28.7 | 155 |
| Glass L3, Burning Point + Quick Focus | 7.4 | 13.4 | 19.3 | 25.2 | 145 |
| Glass L5, + Sunspot + Death Ray (Polished) | 10.5 | 27.3 | 44.1 | 60.9 | 3,505 |
| Glass L5, + Sunspot + Death Ray (Quick Focus) | 15.8 | 28.3 | 40.9 | 53.6 | 3,495 |
| Archer L3, Rapid Fire + Auto Bow | 10.8 | | | | 105 |
| Archer L4, + Bow Squad | 33.0 | | | | 655 |
| Archer L5, Assassin path + Death Mark, 1,000-health foe | 43.2 | | | | 5,235 |

Unlit, the Glass does about half an Archer's damage; at Light 8 it matches the Archer's best paths; at Light 12 it
passes them by a quarter to two fifths, and Sunspot's Illuminate 3 adds 45% to every other tower hitting that foe. Light 12 needs a four-upgrade
Power Bank and Uloe parked beside the Glass, which is the vessel's whole game. Crowds: Focal Point at level 4 does
4.7 / 6.8 / 8.9 a second to every foe on its line at Light 4 / 8 / 12, against 4.0 for Smacky's best Bomb Tower path to
each of up to 15 foes.

**The Mirror** (level 5, effective health): unlit 260 (575 with Tempered Glass) against 950 for Smacky's Steel Wall
path; lit to Light 3 with Silver Backing and Glare, a Blind-3 foe needs 1,182. A dark Mirror is a thin wall on
purpose; Reflection hands back 30-60% of what it takes.

**The Flood Light** mines as often as Spool's Loom: 96 aura a Tide on a Red node unlit, 134 at Light 4, 192 with High
Voltage, about 690 with Stadium Lights on two nodes at Light 8 (`AURA_BASELINE_2026-11-03.md`: a Mining Hut 18, a Loom
96, a Sapling 60-90). It is a pack-strength gatherer, like Yossele's and Spool's, so if Dimitri takes P5-P7 (PLAN Open
question 4) the Flood Light's 0.4 strikes a second should drop with them.

**The Power Bank** deals little itself (a Photon 0.3 a second, 1.8 with Static Charge and Chain Lightning): its damage
is Illuminate, +30% from every tower on a marked crowd, and its tree is Uloe's Plug In Light (4 / 6 / 8 / 10 / 12).

What this check cannot say: how often Light 8-12 is reached in real play, how long Burning Point stays hot, how far
Photons carry their Light. The balance simulator (`balance_argo_2026-11-04/`) has no Light model; adding Uloe to it is
a job of its own, worth doing before the Uloe build if Dimitri wants a Tide-by-Tide answer.

## 5. What else changed in scope `10`

- Every blank stat slot (vessel, Plug In, the four buildings, the Photon) now holds the number Uloe 0.1.0-dev is built
  with, as a `~default~`, copied from `DESIGN.md`; no number changed. The opening note said "Uloe hasn't reached its
  design gate, so most slots are blank", which stopped being true when it was built.
- [Light] gained four slots (Sources, Cap, Effects, Levels) holding the built Light and mark numbers, since the new
  upgrades are written against them.
- Each building gained two `#` lines: what it is for, and its roads.
- [Questions]: 1 updated (names as built), 2 new (take, rename, change or drop the 24), 3 new (Supernova as a button
  or automatic).

## 6. For the next Uloe build

None of the 24 needs a new mechanic. Each rides on a piece Uloe or another pack part already has:

| Upgrade | Route |
| --- | --- |
| Wide Lens, Quick Focus, Extra Cell, Photon Swarm, Tempered Glass (health), Halogen Bulb, High Voltage | stat `Add` / `Pmulti` params, as Burning Point's |
| Polished Lens, Bright Spark | a param read in place of `GlassPerLight` / `PhotonLight`, `PhotonRadius` (`Code/02_Data.cs`) |
| Prism, Focal Point | `09_Glass.cs` fires two more `Projectile_Ai_Beam`s at `Entityarea` neighbours; the pierce is `Helper_Entityquerytools.Entityrayall` along the beam (fit plan §8.3, the Long Throw line) |
| Sunspot, Death Ray | `UloeMarks` from the Glass's hit; Burning Point's held ramp, not reset on a new target, with a 3 s idle timer |
| Static Charge, Chain Lightning | the Photon's hit hook in `08_Bank.cs` (mark level param; `Entityarea` within 2, `Interacthurt` and marks on two more) |
| Surge Protector | a front-of-chain hook on the vessel while plugged into that Bank (the Mirror's `UloeUtil.Front`, `Damagemulti` 0.6) and a heal tick |
| Supernova | a building command like AIM (`12_Commands.cs`), Short Circuit's burst on every Photon, a once-a-Tide flag |
| Tempered Glass (damage), Hall of Mirrors, Reflection | `10_Mirror.cs`'s hook: `Damagemulti` 0.8; marks to `Entityarea` round the attacker; `Interacthurt` back (Spool's Pin Cushion reflects the same way) |
| Heliostat | the Glass's target-and-beam code on the Mirror, damage 1.5 x its Light |
| Colour Filter | Yossele's Grafted colour payout, a COLOUR command like AIM |
| Searchlight | the Flood Light's lit set each second: `UloeMarks` plus one timed Speed `Multi` a Flood Light per foe, renewed, never re-added (`ARGO_REVIEW_2026-11-04.md` §7) |
| Stadium Lights, Lighthouse | Wide Flood's radius at 100% plus AIM; Lighthouse turns a ray a step each tick (`Entityrayall`) and holds a 6 s Light `Add` on what it crosses |

Self-test sections 5-8 (`DESIGN.md` "Test plan") grow one check per upgrade. Building it is an xhigh PC round.

## 7. Files

- `docs/scope/10_Vessel_Uloe.txt`: the trees, the filled slots, the questions.
- `docs/reports/uloe_upgrades_2026-11-04/uloe_math.py` and `results/uloe_math.txt`: §4's numbers.
- `TDS_Mod/ModSource/Uloe/DESIGN.md`: a pointer section; its tables stay as built until the next build.
- `docs/scope/02_Progress.txt`, `docs/PLAN.md` Part C (Now, Log, Open question 9).
