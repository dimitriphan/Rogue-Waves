# The balance simulator sweep, 2026-11-05

> Frozen as written. Dimitri's order (2026-11-05 04:51Z): "build the unbuilt candidates, do a text audit, then do a
> balance simulator sweep". What it tunes is his: "Starting set S2, all proposals" (12:25Z) on
> `MODE_BALANCE_REVIEW_2026-11-05.md`, the principle that the mode ends harder than the base game (05:03Z), Spool
> balanced for one Pin Cushion wall and the Loom lowered if it outmines (answers 25 and 26), and the scope lines left
> to the sweep (`DECISIONS_APPLIED_2026-11-05.md` §3 S1-S2, `SPOOL_DEFAULTS_2026-11-05.md`). Simulator in
> `balance_sweep_2026-11-05/` (README there). Cloud only: **compiled here, not run on the PC**; nothing touched his PC.

**In short.** The mode now ends harder than the base game. With what a player typically carries (the mid case), map 1
is 0.8 of a whole base game run, map 3 a base game run, and maps 8-10 about 1.5 of one, measured with 16 seeds on
Smacky's three maps (§2). Built: set S2 (F1-F5, X1, C1, P1) with the sweep's numbers: map k's Tide 15 plays the base
game's row 15, 18, 20, 21, 21, then 24; foe health x2.6 on map 1, +0.08 a map, +2% for every Cardinal, Trinket copy and
Direction rank held. Spool, balanced on one Pin Cushion, gets Stitched 5, Dart 5.5 and 6 Crafted a Tin, and the Loom
mines at 0.3 a second, Uloe's Flood Light with it (§3, §4); one Cushion is still short of Smacky, and in the mode far
short, until a Stitched blocks. Argo looks easier than its base game in the mode (§2). Spring Tide was code (§5); the
weather's pay line is fixed (§6). Nothing waits on Dimitri's word before PC round 4 (§8).

## Contents

1. How the sweep measured
2. The mode: Tide 15's row, foe health and the rest of set S2
3. Spool on one Pin Cushion
4. Uloe's Flood Light
5. Spring Tide by a Fie wall
6. The weather's pay
7. What changed, file by file
8. What waits on Dimitri, and the PC checks

## 1. How the sweep measured

The mode's numbers are measured as the review measured them (its §1), with its code unchanged
(`mode_balance_2026-11-05/`: `mode.py` writes the mode's Tide table for a map, `w50.py` bisects the power a vessel
needs). **W50** is the multiplier on every hit the player's side deals at which a vessel wins half its runs. A map
**asks** the geometric mean, over Smacky on Purlest, Woozy and The Hunt, of its W50 divided by the base game's W50 on
the same map (a whole Downpour run). A player **carries** a multiplier from what they hold (`power2.py`). **D = asked /
carried**: 1 is a base game run, above 1 is harder.

What is new here:

- `power2.py`: the carried power with the shelf as built today (the review counted eight Directions and the stock
  Cardinals; PRs #103-#114 added 13 Directions any vessel takes, Mason's Trowel, 11 Trinkets and 4 Cardinals), C1's
  ramp, and P1 (no objective on Tides 1-2). Mid case with the ramp: 1.31 on map 1 to 3.22 on map 10 (the review had
  1.30 to 2.99).
- `fit.py`: the first pass (`exp_E1`, 72 setups, 12 seeds) tries two or three rows for Tide 15 on each map; since W50
  is close to proportional to foe health, each run gives a shape, asked / health, and the fit picks the row and the
  health curve that meet the target.
- `exp_E2`: the pick, with 16 seeds (the review used 12), plus Yossele's and Spool's checks.
- `argo/w50_argo.py`: Argo in the same bisection, on the Argo review's simulator (`balance_argo_2026-11-04/`), so the
  reference set has a fourth vessel (`exp_E3`). Uloe has no simulator yet.
- `spool/spoolsim.py`: the 2026-11-04 Spool simulator with two knobs added by wrapping it: a Dart's range, and
  `keep_wall` (the one Pin Cushion rebuilt, PATCHed and upgraded, as a player keeps a wall). Four passes, A-D, 30-40
  runs a setup, 60 s breaks.

The target, the review's §7 default as one line: D (mid case, with C1) 0.80 on map 1, 0.88 on map 2, 1.00 on map 3,
then up by about 0.07 a map to 1.50 on map 10; in the low case, above 1 from map 3.

## 2. The mode: Tide 15's row, foe health and the rest of set S2

**What the sweep tuned.** Set S2 as the review wrote it, then the three numbers its §7 handed the sweep: F1's row for
Tide 15 on each map, F3's lift and F5's share. F2 (health as a Pmulti), F4 (modifiers from map 2), X1 (+50% foes a
player), C1 (the price ramp) and P1 (objectives from Tide 3) are built as the review wrote them. Dimitri's 8% a map
(scope `04`) stays.

**The fit.** `exp_E1` ran Smacky on Purlest, Woozy and The Hunt with Tide 15 on two or three candidate rows a map (72
setups, 12 seeds, about 45 minutes). W50 grows close to in step with foe health, so each run gives the row's shape,
asked / health, and `fit.py` picks the rows and the curve that meet the target. Measured map by map, the shapes are
noisy (row 17 came out easier than row 16), so the pick smooths them first: one shape a row and one factor a map (each
map adds x1.038 on top of its health, the counts, intervals and modifiers). Two rules keep the base game's foes in it:
Tide 15's row never goes down from one map to the next, and map 10 ends on row 24. **Row 24** is Downpour's last
mixed Tide (Hardened Daggers, Aegis and Spears with Mauls and Onagers). Rows 25-27 are each one Superheated kind,
about a third harder in shape than row 24, and every fit that reached them overshot the end. **Row 22** (50 Hardened
Aegis and 50 Superheated Spears, the review's map 6 spike) is left out. Fits within 5% of the best go to the later
rows: the best alone kept maps 5-9 on row 21, a Tide of ten Onagers.

**The fit** (`results/fit.txt`, step 3), as `fit.py` predicted it:

| Map | Tide 15 plays row | Foe health, mid case | Modifiers on | Asked | D mid | D low | D high | Target |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | 15 | x2.64 | none | 1.05 | 0.80 | 0.91 | 0.64 | 0.80 |
| 2 | 18 | x2.88 | 1 | 1.56 | 0.91 | 1.16 | 0.70 | 0.88 |
| 3 | 20 | x3.17 | 1 | 2.00 | 1.01 | 1.29 | 0.77 | 1.00 |
| 4 | 21 | x3.44 | 2 | 2.28 | 1.05 | 1.40 | 0.78 | 1.06 |
| 5 | 21 | x3.71 | 2 | 2.55 | 1.08 | 1.46 | 0.79 | 1.12 |
| 6 | 24 | x3.99 | 3 | 3.08 | 1.21 | 1.67 | 0.86 | 1.19 |
| 7 | 24 | x4.28 | 3 | 3.43 | 1.26 | 1.78 | 0.89 | 1.26 |
| 8 | 24 | x4.58 | 4 | 3.81 | 1.33 | 1.88 | 0.92 | 1.33 |
| 9 | 24 | x4.84 | 4 | 4.18 | 1.37 | 2.01 | 0.94 | 1.41 |
| 10 | 24 | x5.16 | 4 | 4.62 | 1.44 | 2.11 | 0.98 | 1.50 |

Foe health is x(2.6 + 0.08(k - 1)) x(1 + share x held), held being what the mid case holds when map k starts (1 on
map 1, 37 on map 10), and the fit's share is 1.5%.

**Measured** (`exp_E2`, 16 seeds; then `exp_E4` with F5's share at 2%):

![D by map: as built, F5 at 1.5% and the pick at 2%, over the target band](balance_sweep_2026-11-05/difficulty_by_map.svg)

| Map | D mid, share 1.5% (E2) | D mid, share 2% (E4) | D low, 2% | D high, 2% | Target |
| --- | --- | --- | --- | --- | --- |
| 1 | 0.81 | *0.82* | *0.93* | *0.65* | 0.80 |
| 2 | 0.95 | 0.97 | 1.22 | 0.75 | 0.88 |
| 3 | 0.96 | *1.00* | *1.27* | *0.77* | 1.00 |
| 4 | 1.07 | *1.13* | *1.49* | *0.85* | 1.06 |
| 5 | 1.06 | 1.13 | 1.51 | 0.83 | 1.12 |
| 6 | 1.15 | *1.24* | *1.70* | *0.90* | 1.19 |
| 7 | 1.21 | *1.32* | *1.84* | *0.94* | 1.26 |
| 8 | 1.37 | 1.54 | 2.15 | 1.08 | 1.33 |
| 9 | 1.35 | 1.53 | 2.21 | 1.07 | 1.41 |
| 10 | 1.30 | 1.49 | 2.15 | 1.02 | 1.50 |

In italics: maps E4 did not run, estimated from E2 by the change in health (W50 follows health closely, the fit's
premise). The low and high cases scale the measured mid case by their own hoard's health.

At 1.5% the fit held to map 8 and then flattened: map 10 measured 1.30 against 1.44 predicted, under the target's 1.4-1.6
(rows stop at 24 from map 6, so only health, counts and modifiers grow, and the shape model gave them a little too much).
**So the share went to 2%**, the knob that follows the hoard: maps 1-3 move by 5% or less, map 10 by 12%. Measured, the
pick is now:

- **Map 1 at 0.8 of a base game run, map 3 at 1.0, map 10 at 1.49** in the mid case, on the target but for maps 2, 8
  and 9, a little above it (0.97 against 0.88; 1.54 and 1.53 against 1.33 and 1.41): the curve rises to map 8 and
  holds there. Bringing maps 8-9 down would mean a row below 24 on them, the ten-Onager Tide the fit turned away.
- **In the low case above 1 from map 2.** In the high case (a party that takes every objective and bonus Tide, 4.85
  carried on map 10) above 1 from map 8, which the 1.5% share never reached.
- **No Smacky run reached the bisection's ceiling** (48), so, unlike the review's, the economy maps' numbers are not
  floors. Woozy and The Hunt ask much more than Purlest (W50 24-28 on map 10 against 4.4), as in the base game.

So, for the code and scope `04`:

- **F1's rows** are 15, 18, 20, 21, 21, 24, 24, 24, 24, 24 (S2's were 15, 16, 18, 19, 20, 22, 23, 24, 26, 27).
- **F3's lift is x2.6** (the review's default x2.5): map 1 is the base game's Downpour to Tide 15 with every foe at
  x2.6, about 0.8 of a whole base game run for a player holding one Direction.
- **F5's share is 2%** a held Cardinal, Trinket copy or Direction rank (the review's default 1%): +74% on map 10 in the
  mid case, so foe health there is x5.78 (x3.32 for the map, x1.74 for the hoard).

**Against the review's sets** (D, mid case; the review's own columns for as built and S2):

| Map | As built | S2 | The pick |
| --- | --- | --- | --- |
| 1 | 0.31 | 0.80 | 0.82 |
| 3 | 0.22 | 0.85 | 1.00 |
| 5 | 0.19 | 0.99 | 1.13 |
| 6 | 0.18 | 1.38 | 1.24 |
| 8 | 0.18 | 1.24 | 1.54 |
| 10 | 0.18 | 1.98 | 1.49 |

S2's bumps (map 6 on row 22, maps 9-10 on Superheated rows) are gone, and the curve no longer falls back after map 6.

**The rest of set S2, as built:**

- **F2:** the map's health mod is a Pmulti (`21_Difficulty.cs` `Update`), so it multiplies an elite's own Health Multi
  instead of adding to it.
- **F4:** the first modifier comes on map 2, one more every second map, all four by map 8, and a one-row Tide takes
  them in turn from its own number. Scope `04` says so with tildes.
- **X1:** +50% foes for each player past the first (`CountPerPlayer`, was +35%). The simulator plays one player, so the
  PC's two-player run is its check (§8).
- **C1:** every Cardinal and Trinket costs 3 Pure Aura more for each Trinket copy the buyer holds and each Cardinal the
  run has bought (the lobby's picks left out): `RWCardinals.PriceFor`. The host writes the ramp on each vessel (a
  fourth None command, `RW_Price`), so the shelf on every machine shows what the host charges. Scope `12` [Rules] has
  the line.
- **P1:** the objective board opens from the break before Tide 3 (`RWObjectives.ObjectivesFrom`), as scope `06` and
  `14` now say. Pure Aura stays 3 a Tide, 2 a bonus Tide, +1 on a weather map.

**Yossele, Spool and Argo** (each on Purlest, with F5 at 1.5%; W50, the power a vessel needs to win half its runs):

| Map | Smacky | Yossele | Spool, one Pin Cushion | Argo |
| --- | --- | --- | --- | --- |
| Base game | 1.68 | not measured | not measured | 1.54 |
| 1 | 0.72 | 0.44 | 25 | 0.36 |
| 4 | 1.67 | 1.49 | over 48 | |
| 5 | 1.88 | | | 1.54 |
| 7 | 2.67 | 3.37 | over 48 | |
| 10 | 3.70 | 2.37 | over 48 | 1.94 |

- **Yossele** meets the curve about as Smacky does to map 7. Its map 10 says nothing: at power 1 its bot dies on
  Tide 9 of maps 7 and 10 to the Mauls it has no answer to, the limit the review named.
- **Spool on one Pin Cushion can't hold the mode's maps** at any power the curve gives: it needs 25 times a player's
  damage on map 1 and more than the bisection's ceiling from map 4. That is §3 again (one Cushion ends even the base
  game's Purlest on Tide 8): Spool's foes walk past everything it has, so more health on them costs it more than it
  costs a vessel whose towers hold a lane. The curve makes no allowance for it; a blocking Stitched (among Spool's 22
  upgrades) or a wall that holds is the answer, and Spool's play test is the check.
- **Argo** (its own simulator, 8 seeds) needs about half of Smacky's power on the same maps, while their base games
  ask about the same. Against its own base game the mode asks 0.23, 1.00 and 1.26 of Argo on maps 1, 5 and 10, so D
  is about 0.4 on map 5 and on map 10: easier than the base game, against the principle. Its slows and holds buy more
  time the tougher the foes are. The curve is Smacky's, as it was the review's reference, and the mode has one curve
  for every vessel, so this goes to the PC (one Argo map 5 by hand); if Argo plays easy there, the fix is in Argo's
  own numbers (scope `09`), not the mode's.

## 3. Spool on one Pin Cushion

Answer 25: balance Spool as if the player builds one Pin Cushion, not a wall that shuts the path (the PC saw foes walk
round three, `BALANCE_ROUND_2026-11-03.md` M5). The sweep owns four numbers for it (Stitched Damage, Dart Damage,
Crafted alive a Tin, Dart Range) and the Loom's mining (answer 26).

**What one Cushion does in the model.** Foes walk round it; a Tempered foe turns on it and gets pricked. Spool's
damage is its Crafted, which hit what passes within reach but hold nothing. With one Cushion Spool ends Tide 5 on
Purlest with about 3,600 Blue it has no use for: on one wall the Loom's Blue goes unspent, its Red pays for Tins.

**Pass A** (40 runs, the frozen policy: it builds the open Cushion once and never touches it again) and **pass B** (30
runs, the wall kept). Mean Tide reached; 16 is a won map. Package = Stitched Damage / Dart Damage / Crafted a Tin /
Dart Range.

| Setup | Purlest, A | Purlest, B (wall kept) | Woozy, A | Woozy, B (wall kept) |
| --- | --- | --- | --- | --- |
| Smacky, the same one wall | won (100%) | | 5.8 | |
| Spool as built, 2 / 2.5 / 4 / 3.5 | 5.0 | 5.0 | 3.4 | 3.4 |
| P1-P3 of 2026-11-03, 3 / 3.5 / 6 / 4.5 | 5.35 | 5.3 | 4.05 | 4.07 |
| P2-P3, 3 / 3.5 / 6 / 3.5 | 5.5 | | 4.05 | |
| 3 / 3.5 / 8 / 4.5 | 5.8 | | 4.12 | |
| 4 / 4.5 / 6 / 4.5 | 6.6 | 6.6 | 4.28 | 4.27 |
| 4 / 4.5 / 8 / 4.5 | 6.83 | | 4.53 | |
| 5 / 5.5 / 6 / 4.5 | 8.25 | 8.2 | 4.75 | 4.77 |
| 5 / 5.5 / 8 / 4.5 | 8.62 | | 4.88 | |
| 6 / 6.5 / 6 / 4.5 | | 8.83 | | 4.77 |
| 6 / 6.5 / 8 / 4.5 | | 9.4 | | 5.0 |
| Spool as built, gap shut (three Cushions that hold) | 13.9 | | 9.12 | |
| P1-P3, gap shut | 14.8 (won 40%) | | 10.93 | |

![Spool on one Pin Cushion: the Tide it reaches on Purlest and Woozy, by package (pass B), against Smacky](balance_sweep_2026-11-05/spool_one_wall.svg)

What it says:

- **No package of these four numbers carries Purlest on one Cushion.** Smacky wins it every time; Spool's best reaches
  Tide 9-10. The gain stops at Stitched 5 / Dart 5.5: from 2 to 3 it buys a third of a Tide on Purlest, from 3 to 4 a
  Tide and a third, from 4 to 5 a Tide and two thirds, from 5 to 6 half a Tide on Purlest and nothing on Woozy.
- **Woozy closes to within a Tide of Smacky** (4.8 against 5.8) at Stitched 5; more buys nothing there.
- **Dart Range 4.5 buys nothing on one wall** (5.35 against 5.5 on Purlest, 4.05 both on Woozy): the 2026-11-03 P1
  helped only Darts standing behind a wall that holds. So it stays 3.5. 8 Crafted a Tin buy a third of a Tide more
  than 6 at the same damage; 6 is the pick (more Crafted also mean more Thread to keep them).
- **Keeping the wall changes nothing** (pass B against A, within 0.1 Tide): one Cushion is walked round whether or not
  it has Sharper Pins and Extra Stuffing.
- **The rest of the gap is that nothing of Spool's holds a walker.** That is the design question the 2026-11-03 re-run
  named (Stitched that block, as Tacking Stitch makes them; it is among Spool's 22 upgrades, built after this sweep),
  not a number.

**Pass C and D: the pick and the Loom** (30 runs; the pick is Stitched 5, Dart 5.5, 6 a Tin, Range 3.5):

| Setup | Purlest | Woozy | The Hunt |
| --- | --- | --- | --- |
| Smacky, one wall | won (100%) | 5.83 | 10.0 |
| Spool as built (Loom 0.4) | 5.0 | 3.4 | 4.6 |
| The pick, Loom 0.4 as built | 7.67 | 4.77 | 5.2 |
| **The pick, Loom 0.3** | **8.1** | **4.7** | **4.5** |
| The pick, Loom 0.25 | 8.03 | 4.7 | 4.2 |
| The pick, Loom 0.2 | 7.9 | 4.63 | 3.0 |
| The pick, Loom 0.15 (the top of the base gatherers, 36 a Tide) | 8.03 | 4.57 | 2.13 |
| The pick, gap shut (three Cushions that hold), Loom 0.15 | won (100%) | 11.0 | 2.13 |

- **The Loom can come down on Purlest and Woozy for free** with one wall: from 0.4 to 0.15 Spool loses a third of a
  Tide or less there, while its mining falls to under half.
- **The Hunt says stop at 0.3.** Its Orange node pays Red and Yellow, no Blue, and Spool's Red for its Tins there is
  the Loom: at 0.2 Spool loses The Hunt at Tide 3, at 0.15 at Tide 2, against 4.6 as built. At 0.3 it holds where it
  was (4.5). So the Loom goes from 0.4 to **0.3**: about 72 aura a Tide beside a Red node, 4 times a Mining Hut and 2
  times Buster's Checkpoint, the top of the stock gatherers (`AURA_BASELINE_2026-11-03.md` §3). Answer 26's "lower it
  if it outmines" is met as far as Spool can take it.
- **In a gap that does shut, the pick is strong**: it wins Purlest and lasts to Tide 11 on Woozy, five Tides past
  Smacky. That is answer 25's trade: the numbers are for the Spool who builds one Cushion, and a map whose path a few
  Cushions really close (none seen yet) will be easier for Spool than for the base vessels.
- **The Hunt is still Spool's worst map** (Tide 4.5 against Smacky's 10): there it is short of Red, not of damage.

**P4** (taken with answer 25): the Pin Cushion's in-game line is now "Build it first: pricks melee attackers, spins
Thread when hit", and its journal text ends "Set one where the paths meet before anything else: the Crafted fight best
beside it." The model gives the one Cushion little to do, so his play test is what says whether the advice helps.

**Left as it is: the Seam.** Seam Allowance's Seam strikes the node 0.8 times a second (about 192 a Tide beside a Red
node, the largest gatherer path left once the Loom is down). Its numbers are Dimitri's own (scope `08` [Crafted], no
tildes), and whether it mines at all is a PC check (`AURA_BASELINE_2026-11-03.md` M10). If it does, Attackspeed 0.3
would put Loom and Seam at about 140 a Tide, near Smacky's Ore Scraps path (106) at a third of its price.

## 4. Uloe's Flood Light

Scope `10` ties it to the Loom ("Mines its node as often as Spool's Loom"), and it struck 0.4 times a second, as the
Loom did: 96 a Tide beside a Red node, five Mining Huts. Uloe has no simulator, so the sweep moves it with the Loom, to
**0.3**, by the same reasoning: like Spool, Uloe's towers are mostly Red (Red 8,385 of its sheet's prices against Blue
3,610 and Yellow 7,695), kills pay only Red, and on an Orange node the Flood Light is the vessel's other Red. Below 0.3
Spool lost The Hunt sooner than as built, and nothing says Uloe would fare better. Light still makes it 10% faster a
point, so a lit Flood Light at Light 3 strikes about 0.39 times a second, near the old number. The scope line says
so; Uloe's play test is the check. Scope `10`'s other tilde line, Light shared up to 4 a building, was taken by
Dimitri on the second decisions page (PR #117) and needs nothing here.

## 5. Spring Tide by a Fie wall

Round 3 saw a Spring Tide taken first halve hits next to a Fie wall (10 -> 4.9). That was code, not a number: the
stock Beacon effect Spring Tide rode on gives the tower `Resistadd` 1, which does nothing alone, and once Fie's Regen
comes on (a Shield wall or Shared Strength) every hit divides by 1 + `Resistadd` (kit pitfall #159). Answer 14 rebuilt
Spring Tide and Tidal Mend on the mode's own effects in PR #103 (B7), and `RogueWavesMode/Code/96_Flow.cs` checks "the
gain is the mode's own, with no Resistadd on the tower". Nothing to tune here. The weather round of the same day
(`LIGHTING_2026-11-05.md` §6) ran that check on the PC for the first time and it failed (0 to 0.0375 where 10 is asked);
PR #122 traced that to the test's tower, built after Frail Titan and Paper Tower (kit pitfall #171), and now measures on
a plain archer. Not a balance number either way.

## 6. The weather's pay

`TEXT_AUDIT_2026-11-05.md` §4 A7: scope `04` [Sea Weather] said "1 extra Pure Aura each tide, on top of the usual 3",
which is wrong for a bonus Tide, which pays 2. The code pays the Tide's own (3, or 2 a bonus Tide) plus 1 on a weather
map (`14_Run.cs`: `PerTide`, `PerBonusTide`, `RWWeather.PureAuraPerTide`). The line now reads "1 extra [Pure Aura] each
tide, on top of what the tide pays: 3 for Tides 1-15, 2 for a bonus tide", and the code's comment says the same.

## 7. What changed, file by file

Mode (`TDS_Mod/ModSource/RogueWavesMode/Code/`):

- `21_Difficulty.cs`: F1's `FinishRows` and `RowOf` (the stride), with the bonus Tides from the row after it and the
  rewind on the row (`LastRow` 29); a Tide entry and the break before it carry the notes of the row they play; F2's
  Pmulti; F3's `HealthLift` 2.6; F5's `Hoard` and `HoardShare` 2%; F4's `ModifiersFrom` 2 and the per-Tide turn;
  X1's `CountPerPlayer` 0.5. The log line at a map's start says the row, the health and what was held.
- `17_Cardinals.cs`: C1, `Ramp` 3, `PriceFor` / `RampFor` on the host, `PriceMine` on every machine from `RW_Price`.
- `14_Run.cs`, `15_Content.cs`: the fourth None command, `RW_Price`, written in `Carry` with the other three.
- `19_Objectives.cs`: P1, `ObjectivesFrom` 3.
- `07_Weather.cs`: the comment on the weather's pay.
- Self-tests: `92_Gaps.cs` (each map's row, health and modifiers against scope `04`'s numbers written out; no board
  before Tide 3; the shelf's price against the host's after a stock Cardinal), `92_GapsTrinkets.cs` (prices with the
  ramp), `92_Review.cs`, `93_TwoPlayers.cs` (a second player adds half as many foes again), `96_Flow.cs` and
  `99_SelfTest.cs` (the flow reads map 1's opening board through `BoardFromTide1`).

Spool and Uloe:

- `Spool/Code/02_Data.cs`, `07_Building.cs`, `08_Tin.cs`: Stitched Damage 5, Dart Damage 5.5, 6 Crafted a Tin, the
  Loom 0.3 a second; the Pin Cushion's "Build it first" line and journal text. `99_SelfTest.cs` checks the Tin at 6.
  `98_Play.cs` and `00_SpoolMod.cs`: the `SPOOL_TUNE` hook the 2026-11-03 re-run used is gone (its numbers are in).
- `Uloe/Code/02_Data.cs`, `07_Building.cs`, `98_Upgrades.cs`: the Flood Light 0.3 a second.
- `Spool/DESIGN.md`, `Uloe/DESIGN.md` and both `IMPLEMENTATION_NOTES.md`.

Scope (`docs/scope/`):

- `04`: F4's line, the Numbers line (rows, +6% foes a map, +50% a player, health x2.6 + 0.08 a map + 2% a held
  item), the weather's pay (§6). Every sweep number has tildes, for Dimitri to take or change.
- `06`: the price ramp on the Sells note, and the board from the break before Tide 3.
- `08`: Stitched Damage ~5~, Dart Damage ~5.5~, Dart Range ~3.5~, Crafted ~6~ a Tin, Loom Mining ~0.3 a second~, the
  Pin Cushion's "Build it first" line.
- `10`: Flood Light ~0.3 a second~.
- `12`: [Rules] the price ramp, ~3~ a held item.
- `14`: every objective offered from the break before Tide 3.

The runs: `balance_sweep_2026-11-05/` (its README).

## 8. What waits on Dimitri, and the PC checks

Nothing here needs an answer before the next PC round; each is a default he can change by editing the tilde.

1. **The target curve**, measured: D 0.8 on map 1, 1.0 by map 3, 1.49 on map 10 in the mid case, maps 8-10 level at
   about 1.5. The high case crosses 1 from map 8.
2. **The mode's numbers** in scope `04`: Tide 15 on rows 15, 18, 20, 21, 21, 24 x5; foe health x2.6 on map 1, +0.08 a
   map, +2% a held item; modifiers from map 2.
3. **Spool**: Stitched 5 and Dart 5.5 roughly double its damage. No package of these numbers carries Purlest on one
   Cushion (Tide 8 of 15), so on one Cushion **Spool can't hold the mode's maps** at all (§2, its W50 is past the
   ceiling from map 4); in a gap a few Cushions do shut, Spool now outlasts Smacky. The rest is a blocking Stitched,
   which Spool's 22 upgrades bring: that build is where Spool's mode balance gets settled.
4. **The Loom** at 0.3 a second (was 0.4), and the **Flood Light** with it, untested for Uloe.
5. **The Seam** stays as his numbers have it; if the PC shows it mines, 0.3 a second would bring it in line.
6. **P4**: the Pin Cushion's "Build it first" line.
7. **Argo** meets the mode easier than its base game in its simulator (D about 0.4 on maps 5 and 10): one curve
   serves every vessel and it is Smacky's, so this is a PC check first (below), and Argo's own numbers after.

**For the next PC round** (cloud-compiled only; no self-test has run on this code):

- The log line at each map's start, `map k for n players: Tide 15 plays row F, foes x…, intervals x…, health x… (N
  held), modifiers […]`, on maps 1, 2 and 6.
- One map 10 by hand to Tide 15, to see row 24 with foe health near x5.8.
- A two-player map 5, for X1's +50% a player.
- Argo on a map 5 by hand: if it plays easy, Argo's numbers (scope `09`) are next.
- The shelf's prices on both machines after a stock Cardinal is bought (the self-test checks the host).
- Spool on one Pin Cushion: Stitched and Dart damage on the panel, 6 Crafted a Tin, the Loom's pace.
- Spring Tide by a Fie wall (§5), with PR #122's plain-archer lines.
