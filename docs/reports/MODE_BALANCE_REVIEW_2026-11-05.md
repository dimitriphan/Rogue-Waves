# The mode's balance against the base game, 2026-11-05

> Frozen as written. Asked by Dimitri, 2026-11-05 05:03Z: "Review the balance of the roguelike mode currently. With
> stacking cardinals and directions, the difficulty should be higher than base game." Read from the code on the base
> after PR #100 (`RogueWavesMode/Code/21_Difficulty.cs`, `14_Run.cs`, `16_Directions.cs`, `17_Cardinals.cs`,
> `18_Shop.cs`, `19_Objectives.cs`) and scopes `04`, `06`, `11`-`14` as they stand after PRs #95-#98, then measured in
> the balance simulator (`mode_balance_2026-11-05/`, 207 setups, about 21,000 runs). Cloud only: no PC, no Remote
> Control. **No number in a mod or a scope changed.** Every proposal below is numbered and carries Claude's default in
> `~tildes~`; scope `04`'s [Difficulty] numbers and scope `06`'s prices are Dimitri's (taken 2026-11-05), so changing
> any of them waits on his word. The balance simulator sweep thread applies what he takes (§7).

**In short.**

1. **As built, every map of the campaign is easier than the base game, and the gap widens as the campaign goes on.**
   A map is won at Tide 15 of the base game's own 30-Tide table (Downpour), with breaks twice as long. Tides 1-15 hold
   8.5% of a Downpour run's foe health; Tide 15 is a 4,000-health Tide where Downpour ends on 84,000 and 83,000 (§2).
   The per-map scaling takes map 10's Tides 1-15 to 2.1 times map 1's. In the simulator a Smacky needs about 0.4 of
   the power a whole Downpour run asks on map 1 and 0.63 on map 10, before anything it carries (§4).
2. **What a player carries grows faster than the foes do.** By the estimate in §3 a player brings about 1.3 times
   their own power into map 1 (one Direction and the first Cardinals) and about 3.5 times into map 10 (2.2 to 4.9
   across the cases). Foe pressure grows about 1.5 times over the same ten maps. So the effective difficulty, what a
   map asks over what the party carries, falls from about 0.31 of the base game on map 1 to 0.18 on map 10: the
   opposite of the principle (chart, §4).
3. **Three things in the code make it so** (§2): the finish line sits in the middle of the base game's table and
   stays there on every map; the map's extra foe health is a `Multi` mod, which adds to an elite's own instead of
   compounding, so on Tides 6-15 (nearly all elites) map 10's "+72% health" is +18% on a Hardened foe and +24% on a
   Superheated one; and a Tide of one row (Tides 12 and 15) always gets the first modifier of the pool.
4. **The fix that does the most is to move the finish line, not to inflate Daggers** (F1): map k's Tide 15 plays a
   later row of Downpour, up to Tide 30 (the Basin waking) on map 10, with Tides 1-15 spread over the rows between.
   With health compounding (F2) and modifiers from map 2 (F4) that alone lifts the effective difficulty to about 1.0
   of the base game on map 10. A map-1 lift (F3) and a health term that follows what the party holds (F5) carry it
   over the base game from map 6 on, to about 2 on map 10, with maps 1-5 near the base game (0.8-1.0) as an on-ramp (§5).
5. **On the power side one change is enough** (C1): each Cardinal or Trinket costs a little more for each one the
   buyer already holds. It keeps stacking and the caps of decision 3, and moves Pure Aura towards Direction ranks.
   Pure Aura income, Direction prices and the stack caps can stay as they are (§6).

The proposals, with the file and constant each touches, are in §5 and §6; what the sweep thread runs to set the final
numbers is §7.

## 1. How the comparison works

**The base game** is Downpour (`Mode_Normal`), the table the mode copies (`02_Mode.cs`): 30 Tides of 180 s, 30 s
breaks, won when Tide 30 (the Basin waking, an Excalibur among Superheated Mauls) is cleared. A base-game player
brings the lobby's Cardinals (one per colour and a Mixed slot, kit `docs/15` §7). The mode keeps those picks, locked
for the campaign (scope `04` [Sea Chart] "Lobby Cardinals", decision 2), so they count on both sides and drop out.

**A map of the mode** is that table as `RWDifficulty.Apply` writes it for map k, played to Tide 15 with 60 s breaks
(the opening one 45 s): foes x(1 + 0.06(k-1)), spawn intervals x(1 - 0.03(k-1)), foe health +0.08(k-1), one foe in
five of each row carrying a modifier from map 3 on (`mode.py` builds the same table for the simulator).

**The measure is W50**: the multiplier on every hit a player's side deals at which a vessel wins half its runs. A
setup whose W50 is below the base game's on the same vessel and map is easier than the base game; the ratio says by
how much. The simulator is the one the earlier studies used (`balance_upgrades_2026-11-04/`, imported unchanged; its
calibration against the PC is in `BALANCE_RERUN_2026-11-03.md` §2), one player, one vessel, 12 seeds a setup,
bisection to about 3%.

**The reference vessel is Smacky**, the base game's own, on Purlest, Woozy and The Hunt. Smacky's bot is the one that
plays Downpour's Tides 16-30 in the model (it loses Purlest at Tide 27-28 at power 1 and wins from power 1.7). The
Yossele and Spool bots were written for 15 Tides: Yossele loses Downpour on Tides 25-28 (the Superheated and Hardened Mauls) even at eight times its power,
and Spool on Tides 13-16 with its Cookie Tins gone, so their base-game W50 is the bisection's ceiling, not a
measure. For them the report uses only how the mode's own pressure grows from map 1 to map 10.

**What a player carries** can't be simulated (the simulator has no Directions or Cardinals), so §3 estimates it as the
same kind of multiplier, from each built Direction's and each stock Cardinal's numbers, the Pure Aura income and the
built prices. It is the least certain number here, so every result is given for a low, a mid and a high case.

**What the model leaves out:** the pack's four modifiers have no brains in the simulator (each is stood in by +50%
health, Aging's middle; Cursed and Dazzling weaken the player instead, so this is a floor on pressure); weather;
objectives' own play; a second player (§2, A5); Uloe and Argo. PR #100's two fixes (campaign Cardinals leaking into
later normal games, the staging map's free picks) closed ways round the campaign; the estimate never counted them.

## 2. Foe pressure as built

Each map's numbers (`21_Difficulty.cs`) and the foe health they put into the Tides, from `pressure.py`:

| Map | Foes | Intervals | Health | Modifiers on (1 foe in 5) | Tide 15 health | Tides 1-15 health, x map 1 |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | x1.00 | x1.00 | +0% | none | 4,000 | 1.00 |
| 2 | x1.06 | x0.97 | +8% | none | 4,368 | 1.09 |
| 3 | x1.12 | x0.94 | +16% | 1 (a pack one) | 5,085 | 1.23 |
| 4 | x1.18 | x0.91 | +24% | 1 | 5,489 | 1.35 |
| 5 | x1.24 | x0.88 | +32% | 2 (+ a stock elite) | 6,050 | 1.46 |
| 6 | x1.30 | x0.85 | +40% | 2 | 6,490 | 1.57 |
| 7 | x1.36 | x0.82 | +48% | 3 | 6,946 | 1.69 |
| 8 | x1.42 | x0.79 | +56% | 3 | 7,571 | 1.81 |
| 9 | x1.48 | x0.76 | +64% | 4 | 8,063 | 1.95 |
| 10 | x1.54 | x0.73 | +72% | 4 | 8,732 | 2.10 |

Downpour, for the comparison: Tides 1-15 hold 32,450 health, Tides 16-30 hold 349,250. Tide 15 is 40 Tempered
Ballistas (4,000); Tide 29 is 30 Hardened Mauls (84,000) and Tide 30 82,600 with the Excalibur. **Map 10's whole
Tides 1-15 (68,000) are less than Downpour's Tide 29 alone.**

What makes it so:

- **A1. The finish line never moves.** Every map is won at Tide 15, the middle of a table whose second half holds 92%
  of its health, and with 60 s breaks where Downpour has 30 s (a gatherer mines twice as long between Tides). The
  breaks are Dimitri's rule (scope `04` [Tides and Leaving]) and stay; the finish line is what F1 moves.
- **A2. The map's health adds to the elites' instead of compounding.** `RWDifficulty.Update` adds `+0.08(k-1)` as a
  `Multi` mod, and `Multi` mods add (kit `docs/13` "Stat changes are `Multi` modifiers, so two elites' `Health_Multi`
  add"; `docs/17` §2.1). On Tides 9-17 (and Tide 6) 74-100% of Downpour's health is on elites, so map 10's +72% is x4.72 / x4 = +18% on a Hardened foe, +36% on a Tempered one, +24% on a Superheated
  one. With a `Pmulti` mod it would compound: `(Base x Pmulti + Add) x Multi` (Helper_Entitytools.cs:661; RogueLab round
  7 used `Pmulti`). That is F2; on its own it takes map 10's Tides 1-15 from 2.1 to 2.8 times map 1's.
- **A3. One-row Tides always get the first modifier.** `Apply` hands modifiers out in turn per row (`mods[nth %
  mods.Count]`, `nth` from 0 in every Tide), so Tide 12 (60 Javelins) and Tide 15 (40 Ballistas) only ever get
  `mods[0]`, and the pool's order (pack, stock, pack, stock) makes that a pack modifier. F4 starts the turn at the
  Tide's number.
- **A4. Modifiers come late and stay thin.** None on maps 1-2, one on 1 foe in 5 from map 3, all four only from map 9.
  Which stock elite a campaign rolls matters more than its map number: a Hardened roll puts x4 health on a fifth of the
  foes from map 5, an Annealed roll only armour.
- **A5. A second player.** Foes rise 35% per player past the first (`CountPerPlayer`), where the base game scales
  nothing (kit `docs/15` §14.3). Two players have twice the vessels and twice the Pure Aura, and every Cardinal one buys
  works for both: the party's Cardinals grow twice as fast as one player's. X1 raises the per-player rise.
- **A6. Bonus Tides.** Tides 16-29 are Downpour's own rows with the map's scaling and from Tide 30 each rewind adds
  10% foes (scope `04` [Difficulty] now says so). They pay 2 Pure Aura each, so a strong party can farm them on early
  maps; Downpour's second half grows fast enough that this limits itself, and the simulator never needed to model it.

## 3. Player power carried into each map

`power.py` gives what a player holds at map k as a multiplier on their own output, buying greedily by value per Pure
Aura at the built prices:

- **Pure Aura a map** (one player): 15 Tides x 3 = 45, the objectives (one offered every break, team pay 2-4, free to
  take, nothing lost on a fail: about 30 if two in three are won), weather (+1 a Tide on about 1 map in 3, then 1 in 2:
  about 6), two bonus Tides (4). The cases: low 50, mid 85, high 115 a map.
- **Directions**: one pick before every map, a new one while any is left, else the next rank of one held (free); ranks
  II and III at 20 and 30 Pure Aura. A Smacky draws from eight built ones. Their value: Cut Costs -10/-20/-30% is
  x1.11/1.25/1.43 aura, the most any one item gives; Cheap Refits about half that (upgrades are about half a run's
  spend); Compound Interest, Spring Tide, Tidal Mend, Barnacle Coat, Made of Tougher Stuff and Skipping Stones 1-10%
  each. All eight at rank III: x2.1.
- **Cardinals**: 25 stock ones on the shelf at 15, each held once (decision 3), plus Vampire Dentures (six copies).
  Valued from their in-game lines: Wave Breaker 15%, Rusty Pipe, Voice in a Bottle and Cubed Drink 10%, the four Coins
  8%, eleven more at 2-5%, the trade-offs and pets 0. The lobby already took the best five, so the shop counts past
  them. The case values are x0.6 (low) and x1.5 (high).

| Map | Low | Mid | High | Mid: Directions held, ranks | Mid: Cardinals and Dentures bought |
| --- | --- | --- | --- | --- | --- |
| 1 | 1.15 | 1.31 | 1.65 | 1 (Cut Costs, at II) | 1 |
| 2 | 1.33 | 1.78 | 2.40 | 2, 5 ranks | 3 |
| 4 | 1.63 | 2.44 | 3.55 | 4, 8 ranks | 13 |
| 7 | 1.95 | 3.10 | 4.77 | 7, 16 ranks | 16 + 6 Dentures |
| 10 | 2.20 | 3.47 | 4.85 | 8, 24 ranks (all at III) | 19 + 6 Dentures |

Two things follow. **The shelf runs dry**: in the mid case a player has bought every stock Cardinal worth buying by
map 9 and every Direction rank by map 10; in the high case both by map 8, after which Pure Aura buys nothing. Claude's
unbuilt sheets (47 Direction, Trinket and Cardinal sheets to build, `SCOPE_CODE_AUDIT_2026-11-05.md`) will refill it, with six copies each of
the stacking ones, so the ceiling rises once they are built. **Directions carry about half of it** (x2.1 at map 10),
Cardinals the rest (x1.66): the price of Cardinals alone can't hold the curve down.

## 4. Power against pressure

![Power carried against power asked, maps 1 to 10](mode_balance_2026-11-05/power_vs_pressure.svg)

Both are multiples of the power a whole Downpour run asks of the same vessel on the same map (1, the dashed line).
"Asked" is W50 over the base game's W50, the geometric mean of Smacky's three maps. "Carried" is §3. **Where a
"carried" line sits above an "asked" line, that map is easier than the base game**; their ratio is the effective
difficulty D in the table below (1 = the base game, above 1 harder). The proposed set S2 assumes C1's prices (§6), so read it
against the dashed "carried, price ramp" line.

| Map | As built | R | S | S2 | S2, low case | S2, high case |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | 0.31 | 0.31 | 0.48 | 0.80 | 0.91 | 0.64 |
| 2 | 0.24 | 0.26 | 0.42 | 0.81 | 1.08 | 0.63 |
| 3 | 0.22 | 0.33 | 0.53 | 0.85 | 1.14 | 0.62 |
| 4 | 0.19 | 0.39 | 0.63 | 0.82 | 1.13 | 0.59 |
| 5 | 0.19 | 0.54 | 0.90 | 0.99 | 1.41 | 0.68 |
| 6 | 0.18 | 0.40 | 0.66 | 1.38 | 2.02 | 0.92 |
| 7 | 0.19 | 0.56 | 0.97 | 1.01 | 1.54 | 0.68 |
| 8 | 0.18 | 0.72 | 1.26 | 1.24 | 1.92 | 0.84 |
| 9 | 0.18 | 0.75 | 1.34 | 1.64 | 2.52 | 1.10 |
| 10 | 0.18 | 1.04 | 1.72 | 1.98 | 3.03 | 1.34 |

R, S and S2 are §5's proposal sets, each against the carried power with C1's price ramp (§6); "as built" against the
built prices. The low and high cases change only the carried power.

As built, D never reaches 1: about 0.31 on map 1 and 0.18 from map 4 on. Per setup (W50, base in brackets):

| Setup | Map 1 | Map 4 | Map 7 | Map 10 | Base game |
| --- | --- | --- | --- | --- | --- |
| Smacky, Purlest | 0.27 | 0.34 | 0.41 | 0.47 | 1.68 |
| Smacky, Woozy | 3.35 | 3.58 | 4.08 | 4.66 | 5.14 |
| Smacky, The Hunt | 2.05 | 2.34 | 3.25 | 3.14 | 3.14 |
| Yossele, Purlest | 0.18 | 0.22 | 0.26 | 0.35 | not measured |
| Yossele, Woozy | 0.33 | 0.40 | 0.52 | 0.61 | not measured |
| Spool, Purlest (wall shut) | 1.34 | 1.68 | 2.85 | 2.94 | not measured |

Woozy and The Hunt are economy maps in the model (Smacky runs out of Red on Woozy and of Blue on The Hunt by Tide 6-10
in the base game and the mode alike, `BALANCE_RERUN_2026-11-03.md`), so there the mode only reaches the base game's
early-Tide pressure around map 10, and only before what the player carries. Purlest, where the economy holds, shows
the finish line's effect plainly: map 10 asks a quarter of a Downpour run. Yossele's pressure grows x1.9 from map 1 to
10 and Spool's x2.2, the same shape as Smacky's x1.5-1.8.

## 5. Proposals: foe pressure

Each is a change to `RogueWavesMode/Code/21_Difficulty.cs` unless it says otherwise; scope `04` [Difficulty] gets the
matching line. Simulated (Smacky's three maps, asked as a multiple of the base game): F2 alone, F1 alone, R = F1 + F2 + F4, S = R +
F3 at x1.5 + F5, and the recommended starting set S2 = S with F3 at x2.5 and F1 ending on Tide 27.

| Map | Asked, as built | F2 | F1 | R | S | S2 |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | 0.41 | 0.40 | 0.40 | 0.40 | 0.63 | 1.05 |
| 2 | 0.43 |  |  | 0.47 | 0.74 | 1.45 |
| 3 | 0.47 |  |  | 0.66 | 1.06 | 1.70 |
| 4 | 0.47 | 0.56 | 0.67 | 0.84 | 1.36 | 1.77 |
| 5 | 0.49 |  |  | 1.28 | 2.12 | 2.34 |
| 6 | 0.54 |  |  | 1.00 | 1.67 | 3.49 |
| 7 | 0.59 | 0.74 | 1.20 | 1.53 | 2.64 | 2.74 |
| 8 | 0.59 |  |  | 2.07 | 3.60 | 3.54 |
| 9 | 0.60 |  |  | 2.20 | 3.94 | 4.80 |
| 10 | 0.63 | 0.90 | 2.12 | 3.11 | 5.15 | 5.93 |

- **F1. Move the finish line (stride).** Map k's Tide t plays Downpour's row `round(t x F(k) / 15)` (never below t),
  where its Tide 15 plays row `F(k) = ~15 + round(12(k - 1) / 9)~`: 15, 16, 18, 19, 20, 22, 23, 24, 26, 27. Map 1 is
  Downpour as it is; map 10 ends on Tide 27 (120 Superheated Aegis). Ending on the Basin waking (`15(k - 1) / 9`, row
  30 on map 10) was set S's; with F3 and F5 it overshot the end, so S2 stops three rows short. The bonus Tides go on from row F + 1
  and rewind on row 29 as now. *In code:* `Apply` copies `pristine`'s Tide rows into the table by that map (breaks
  stay 60 s); `Bonus` rewinds once the table's last row is reached; with the stride that comes before Tide 29, so its
  `wave < 29` test becomes a test on the row.
  *Why this one:* it uses the base game's own foe mix (Mauls, Onagers, Superheated rushes) instead of Daggers with
  more health, and on map 10 it asks nearly what the base game's finale asks, in half the Tides. Alone (ending on row 30) it takes
  map 10 from 0.63 to about 2.1 of the base game's power.
- **F2. Make the map's foe health compound.** The health mod as `Helper_Entitytools.Modtypes.Pmulti`, not `Multi`, in
  `RWDifficulty.Update`. Then map 10's health is x1.72 on every foe, elite or not. No number changes. *Why:* A2; alone
  it takes map 10 from 0.63 to 0.90.
- **F3. A map-1 lift.** Foe health `~x2.5~` from map 1, still `~+8%~` a map on top, compounding (`Pmulti` 1.5 +
  0.08(k - 1)). Set S tried x1.5 and stayed at 0.4-0.6 of the base game until map 4. *Why:* map 1 sits at 0.4 of the base game's ask with every other proposal, because Tide 15 is still
  Tide 15 there. The first map stays gentler than the rest (an on-ramp with one Direction held), not easier than the
  base game by a factor of three.
- **F4. Modifiers from map 2, handed out per Tide.** `Pool`: one modifier from `~map 2~`, one more every second map
  (all four by map 8); `nth` starts at the Tide's number so one-row Tides take each modifier in turn. Share stays `~1
  foe in 5~`. *Why:* A3, A4.
- **F5. Pressure that follows the hoard.** Foe health `~+1%~` (compounding with F2-F3) for every Cardinal, every Trinket
  copy and every Direction rank the party holds, the lobby's picks left out, counted when the map starts. *In code:*
  `MapStart` reads `RWRun` (each player's Directions, Trinkets and the Cardinals) and adds the share to `LastHealth` (the per-map marker
  upgrades `BuildHealth` registers since PR #100 stay as they are; only the value changes).
  *Why:* it is Dimitri's principle in one line: the more a party stacks, the harder the sea. It also keeps the curve
  right when Claude's 47 unbuilt sheets refill the shelf, whatever their real values turn out to be. Each item still
  nets power (the cheapest is worth 2-15%, it adds 1% to the foes). In the mid case it is +36% on map 10.
- **X1. Per player.** Foes `~+50%~` (was +35%) for each player past the first. *Why:* A5. Not simulated (the
  simulator plays one player); the sweep thread checks it on the PC's two-player run.

**The recommended starting set is S2** (F1 to Tide 27, F2, F3 at x2.5, F4, F5 at 1%, with C1's prices). In the mid
case it holds maps 1-5 at 0.8-1.0 of the base game, the first maps an on-ramp with one or two Directions held, and
rises above it from map 6 to about 2 on map 10 (§4's table). In the low case it is above the base game from map 2; in
the high case only from map 9, so a party that farms every objective and bonus Tide still has an easier campaign than
the base game for most of it. Set S (F3 at x1.5, F1 to row 30) crossed only at map 8 and ended at 1.7.

Three cautions for the sweep. **The numbers are noisy**: one map's row can be much harder than the next (S2's map 6
plays row 22, 50 Hardened Aegis and 50 Superheated Javelins, harder than map 7's row 23), so the curve has bumps a
row choice can smooth. **The economy maps are floors**: on Woozy and The Hunt, S2's W50 reaches the bisection's
ceiling (32) on several maps, so their real ask is higher, and a health lift hits them hardest, because slower kills
also mean less Red; the sweep should try F3 as more foes (`CountPerMap` and a map-1 count) against more health there.
**Yossele and Spool** were measured on S only (§4's per-setup table and `results/res_C.json`): Yossele's pressure
under S grows like Smacky's to map 4 and then meets the Mauls its bot can't answer (the bisection's ceiling from map
7), the same limit as its base game run, so its late maps say nothing yet.

## 6. Proposals: player power and Pure Aura

- **C1. A price ramp on Cardinals and Trinkets.** Each costs `~+3~` Pure Aura for each Cardinal or Trinket copy the
  buyer already holds (the fourth is 24, the tenth 42). *In code:* `RWCardinals.Cost(id)` takes the player and adds
  `Ramp x held`; the shelf shows the price for whoever opens it. Stacking stays, and decision 3's caps (six copies of a
  pack one, a stock one once) stay. In the mid case map 10's carried power goes from 3.47 to 2.99 and the Pure Aura
  goes to Direction ranks instead. *Why:* it slows the stack without a hard stop; a cap would only change which
  Cardinals are bought.
- **C2. Stack caps: keep decision 3's** (six copies, a stock one once), no diminishing step per copy. F5 and C1 cover
  what a per-copy step would.
- **D1. Direction prices: keep 20 and 30.** Directions are the vessel's own build; F5 counts their ranks. One note for
  Dimitri's eye: Cut Costs III (-30% on buildings, x1.43 aura) is the largest single thing a player can hold; if a
  Smacky with it feels too rich in his play, `~-25% at rank III~` is the number to try first.
- **P1. Pure Aura: keep 3 a Tide, 2 a bonus Tide, weather +1.** With C1 and F5, income is not where the curve goes
  wrong. One small thing: three objectives (Keep the Basin Clean, Beat the Clock, Mind the Vessels) are almost free on
  Tides 1-4, where nothing threatens the Basin; offering objectives `~from the break before Tide 3~` takes about 6
  Pure Aura a map off the top.

## 7. For the balance simulator sweep thread

Everything above is in `mode_balance_2026-11-05/`: `mode.py` (the mode's table for any map and any proposal, as
keyword arguments), `power.py` (the carried-power estimate, `ramp=` for C1), `w50.py` (the bisection), `pressure.py`,
`summary.py` (the tables here), `chart.py` and `make_experiments.py` (`exp_A` as built, `exp_B` F1/F2/R, `exp_C` S, `exp_D` S2).
Run, from that folder: `python3 make_experiments.py && W50_HI=32 python3 w50.py experiments/exp_D.json
results/res_D.json 12 8` (about 15 minutes on 4 cores), then `python3 summary.py` and `python3 chart.py
results/res_A.json results/power.json results/res_D.json power_vs_pressure.svg S2`. A new set is a new dict in
`make_experiments.py` with the same keys.

**The target** (Claude's default for the principle, for Dimitri to confirm): D, mid case with C1, of `~0.7-0.9~` on
maps 1-2, `~1.0~` by map 3, rising to `~1.4-1.6~` on map 10, and above 1 from map 3 on in the low case too. S2 is
close: about 0.8 on maps 1-4 and 2.0 on map 10, so the sweep wants a little more early (F3, or a count lift on maps
1-4) and a little less late (F1's map 9-10 rows, or F5 at 0.5%). It tunes F1's `F(k)`, F3's lift and F5's share in
that order (F1 sets the shape, F3 the start, F5 the slope), with more seeds than the 12 used here.

**Then, before code:** add Uloe and Argo to the reference set (`../balance_argo_2026-11-04/` has an Argo bot), give
the Yossele bot an answer to Superheated Mauls so its base-game W50 can be measured, and check the pack modifiers'
stand-in against one PC Tide each. **After code**, the PC round's checks: the log line `map k for n players: foes x…`
shows F(k) and the health with F5's share; one map 10 by hand to Tide 15; a two-player map 5 for X1.

## 8. What waits on Dimitri

His yes, no or numbers on F1-F5, X1, C1 and P1 (C2 and D1 change nothing), and on the target D curve. His
scope `04` [Difficulty] numbers and scope `06` prices are taken, so nothing changes until he answers.
