# The balance simulator re-run on the round's numbers, 2026-11-03

> Frozen as written. The re-run that `BALANCE_ROUND_2026-11-03.md` "Left" asks for: the simulator of
> `BALANCE_2026-11-03.md`, copied to `balance_rerun_2026-11-03/` and fed the round's measures M2-M11. Cloud only: no PC,
> no Remote Control. **Analysis only: no number in a mod or a scope changed.** P1-P7 stay proposals until Dimitri picks,
> and P1-P4 wait for his own try at a wall (the round's advice). 198 setups, 60 runs each (11,880 runs), no Cardinals.

**In short.**

1. **The Opal pays every colour on every payout.** The round's own log shows it: each Tide paid 50 or 60 of each colour
   on top of the Tide's own, which is 5 or 6 payouts × 10. So a strike on the Opal is worth about 21 aura (26 on the
   all-colours Tide), not 9.5, and a Sapling beside it mines about 1,300 a Tide. On Purlest Yossele ends with thousands of every colour; Smacky still
   runs short of **Red** there and Spool of **Blue** (its Pin Cushions). Woozy pays 5 a strike on Blue and Yellow and 1
   on Red, so *which node the first gatherer stands by* changes a Woozy run by up to ten Tides, more than any proposed
   number.
2. **Spool lives or dies by the wall**, as the round found. If a person can shut the gap, P2 and P3 alone take Purlest
   from Tide 14 to a win in 72% of runs and Woozy from Tide 9-10 to 11; P1 (Dart Range) adds nothing unless the Darts
   are held back out of the slam (then Purlest wins every run). **If foes walk round the line, as M5 saw, no number
   buys more than one Tide** (Purlest 5 → 5-6, Woozy 4 → 5). The one thing in the model that carries Spool past Tide 10
   on an open line is a design change, Stitched that block foes like a wall: Tide 10-11 on all three maps.
3. **Yossele: P5 costs little, the fruit decides The Hunt.** A Sapling at 0.15 ends a fifth to a quarter of Yossele's
   runs two Tides sooner on Woozy (open line) and The Hunt (shut gap), and changes nothing else; Yossele stays at or
   above Smacky wherever it was. The Hunt has no Blue
   node, so a grown Sapling's fruit is Yossele's only Blue there: picked up at half rate, it turns Tide 12 into a win;
   at × 5 (P6) it still wins 98% of runs.
4. **The Hunt is decided by Blue**: no node there pays it. Two small aura beasts hunted a Tide (M11: 15 of each
   colour each) take Spool from Tide 5 to Tide 10 with a shut gap and Smacky to a win in most runs. Whether a vessel
   can hunt them (M11's died to a despawn) is now the most useful thing to measure on that map.
5. **The model agrees with the PC within a Tide on Spool played Tin first**, and is about four Tides harsh on
   Yossele's bot on Woozy (Tide 10 against the game's 14), so Yossele's results are a floor.

Nothing here picks for him; §9 says what each answer of his hand test would mean for Open questions 3, 4 and 6.

## 1. What the round changed in the model

| Input | First study (and aura baseline) | Re-run (the round's measure) |
| --- | --- | --- |
| Purlest's Opal | 9.5 aura a strike, split Red, Blue, all, Yellow by Tide | Tide 1 Yellow, 2 Red, 3 Blue, 4 all (M2). A one-colour Tide: 17 payouts in 37 strikes, each 10 of every colour plus 15 of the Tide's; the all-colours Tide: 6 in 14, each 20 of every colour. **About 20.7 a Damage-1 strike on a one-colour Tide, 25.7 on the all Tide** |
| Woozy's nodes | 1 a strike, colours shared | one gatherer stands by one node: Red 1 a strike (3.6 tiles from the core), Blue 5 (4.0), Yellow 5 (8.9) (M3). Purple left out: its tile refused a Sapling |
| The Hunt's Orange | 1 a strike, Red and Yellow | 1.8 Red and 3.0 Yellow a strike, 0.6 payouts a strike (M3, M4) |
| Which node | the map's mix | bots take the nearest node (the PC harness's `OreSpot`), smart players the richest; on Woozy every order was tried and each player keeps its best (§4) |
| Smacky's Mining Hut | the code's 6.8 s round trip since the aura baseline | the same (M9 within a payout) |
| A grown Sapling's fruit | not counted | Attackspeed × 15 of each colour a payout (scope `07`; M10 saw about 6 a fruit), counted only in the setups that say "picked up" |
| The Hunt's beasts | not counted | small 15 of each colour, Large 75 (M11), in the setups that say so (§8) |
| Walls | hold foes until one falls | **both worlds**: "shut gap" (the first study's wall, as a person might build it) and "open line" (M5: foes walk round three tiles, the wall does nothing) |
| Mode breaks | 60 s | 60 s; the calibration (§2) runs Downpour's own 30 s, as the PC did |

The Opal's reading comes from the game's code (`Entity_Ai_Resource_Opal`: a payout pays every colour the node has a
stat for, with the Tide's `Regular` 15 or `All` 10 added) and from M2's numbers: Tide 1 paid Yellow +75 in 5 payouts
and 50 of each colour, Tide 2 Red +90 and 60 of each in 6, the all Tide 60 + 60 of each in 6. The round counted only
the Tide's colour ("6.4-7.5 a strike"); §8 runs that low reading too, and only Spool's shut-gap Purlest moves.

Every change is a `tune` knob, so `{"model": "study"}` still gives the first study's numbers
(`balance_rerun_2026-11-03/README.md`).

## 2. Calibration: the model against the PC

| Run | The game | First study's model | Re-run model |
| --- | --- | --- | --- |
| Spool, Purlest, Tin first, as written | lost T4, T4 | lost T4 (95%) | lost T4 (98%) |
| ... with P1-P3 | lost T5, T5 | lost T4 (67%), T5 | lost T5 (52%), T4 |
| Spool, Woozy, Tin first, as written | lost T2 | lost T3 (83%), T4 | lost T3 (57%), T2 |
| ... with P1-P3 | lost T2 | lost T4 (88%) | lost T3 (75%) |
| Spool, Purlest, wall first, as written | lost T1, T1 | lost T4 (95%) | lost T4 (98%) |
| ... with P1-P3 | lost T1, T2 | lost T4 (67%), T5 | lost T5 (52%), T4 |
| Spool, Woozy, wall first, as written | lost T2 | lost T3 (83%), T4 | lost T3 (57%), T2 |
| ... with P1-P3 | lost T1 | lost T4 (88%) | lost T3 (75%) |
| Yossele, Woozy, the PC's virtual player | lost T14 | lost T12 (73%), T10 | lost T10 (92%) |
| 2026-10-26: Yossele, Purlest, Coins, to Tide 8 | clean | wins 100% | wins 100% |
| 2026-10-26: Yossele, The Hunt, Coins, no ore, to Tide 10 | lost T7 | lost T6 (92%) | lost T6 (92%) |
| 2026-10-26: Spool, Purlest, Coins, to Tide 6 | lost T4, T1, T2 | lost T4 (98%) | lost T4 (98%) |

"lost T4 (98%)" means 98% of runs ended on Tide 4; a second Tide is the next most common. These runs copy the PC
harness (`98_Play.cs`, SPOOL_SMART: three Pin Cushions on the line, then a Tin, a Thread Head, Looms by the nearest
node) on the open line, since that's what M5 saw.

- **Tin first matches** within a Tide on both maps, as written and with P1-P3. The re-run is a little closer on Woozy.
- **Wall first doesn't**, and can't: with the start wallet (R50 B50 Y50) the model's copy buys two Cushions and the Tin
  within seconds either way, so both orders own the same things by Tide 1. The PC's Tide 1-2 losses come from something
  a 1D lane doesn't have: where the foes walked once the line stood, or the harness waiting for its third Cushion
  before the Tin. The round's logs would say which.
- **Yossele's bot on Woozy is four Tides harsh** (T10 against T14). The model's bot puts its Saplings by the nearest
  node, Red, which pays 1 a strike; it mines 1,100 Red in a whole map and no Blue or Yellow. The PC bot reached Tide 14,
  so it got more from Woozy than that (its log names the nodes it used). Read Yossele's numbers below as a floor.

## 3. What a node pays

One gatherer's aura a Tide (the 180 s Tide and the 60 s break) by each measured node, all colours added
(`mining.py`). A Mining Hut miner strikes for Damage 0.5, so it earns half a strike's aura a hit.

| Gatherer | Purlest Opal | Woozy Red | Woozy Blue | Woozy Yellow | The Hunt Orange | Opal, Tide colour only |
| --- | --- | --- | --- | --- | --- | --- |
| Smacky: Mining Hut (the code's 6.8 s round trip) | 387 | 18 | 88 | 88 | 85 | 148 |
| Yossele: Pachira Sapling, new (Attackspeed 0.25) | 1,316 | 60 | 300 | 300 | 288 | 503 |
| Yossele: Sapling, fully grown (0.375) | 1,974 | 90 | 450 | 450 | 432 | 754 |
| Yossele: Sapling at P5, new (0.15) | 790 | 36 | 180 | 180 | 173 | 302 |
| Yossele: Sapling at P5, fully grown (0.225) | 1,185 | 54 | 270 | 270 | 259 | 453 |
| Spool: Loom (0.4) | 2,106 | 96 | 480 | 480 | 461 | 805 |
| Spool: Loom with its Seam (0.4 + 0.8) | 6,317 | 288 | 1,440 | 1,440 | 1,382 | 2,414 |
| Any vessel by hand, idle by the node (0.5) | 2,632 | 120 | 600 | 600 | 576 | 1,006 |

The Woozy Red column is the aura baseline's table, and M10 saw it in game (a Sapling 60, a Loom 80, a Loom with its
Seam 280). The other nodes pay about 5 to 22 times as much, so **the ratios between gatherers don't change, only what
they add up to against a map's costs and a full map's 885 Red of kills.** By Woozy's Red node a Sapling adds about what
the kills do; by the Opal it is most of a vessel's income.

## 4. Where the vessels stand

Each vessel's smart player with no Cardinals, the mode's 60 s breaks; on Woozy, its best order of nodes (the next table).

| Player | Purlest | Woozy | The Hunt |
| --- | --- | --- | --- |
| Smacky, smart, shut gap | wins 100% | lost T12 (95%) (Blue) | lost T10 (60%), T12 |
| Yossele, smart, shut gap | wins 100% | wins 100% (Yellow-Blue) | lost T12 (93%) |
| Spool, smart, shut gap | lost T14 (95%) | lost T9 (53%), T10 (Yellow-Blue) | lost T5 (67%), T4 |
| Smacky, smart, open line | wins 100% | lost T6 (93%) (Red) | lost T10 (92%) |
| Yossele, smart, open line | lost T14 (100%) | lost T12 (100%) (Red-Blue) | lost T12 (100%) |
| Spool, smart, open line | lost T5 (100%) | lost T4 (100%) (Yellow-Red) | lost T5 (67%), T4 |
| Yossele, bot | lost T14 (100%) | lost T10 (75%), T12 | lost T6 (47%), T7 |
| Spool, bot | lost T4 (100%) | lost T2 (87%) | lost T1 (100%) |
| Spool, the PC harness (Tin first) | lost T4 (95%) | lost T3 (57%), T2 | lost T1 (100%) |

- **Yossele is at or above Smacky** everywhere but Purlest on an open line, where it falls at Tide 14 with 13,000 Blue
  and 14,000 Yellow in hand. That isn't money (with fruit it also has 3,000 Red left and still falls there): the
  model's Yossele stops at its cannon target and one Tree, and a person with that wallet would build more. Read it as
  the model's ceiling, not Yossele's.
- **Spool needs the wall more than anyone.** Shut, it reaches Tide 14 on Purlest; open, every map ends by Tide 5.
- **The Hunt starves Spool of Blue.** Its node pays Red and Yellow, kills pay Red, so Spool ends every Hunt run with
  10 Blue and no Cushions past its first two: Tide 5 in every world.

**Woozy: which node first** (each cell the vessel's smart player with its gatherers in that order):

| Woozy, gatherers by | Smacky shut | Yossele shut | Spool shut | Smacky open | Yossele open | Spool open |
| --- | --- | --- | --- | --- | --- | --- |
| Red-Blue | lost T10 (47%), T6 | lost T12 (92%) | lost T2 (57%), T3 | lost T6 (93%) | lost T12 (100%) | lost T2 (57%), T3 |
| Red-Yellow | - | lost T12 (92%) | lost T2 (57%), T3 | - | lost T12 (100%) | lost T2 (57%), T3 |
| Blue-Red | lost T12 (95%) | wins 100% | lost T8 (65%), T6 | lost T6 (80%), T5 | lost T6 (92%) | lost T2 (100%) |
| Blue-Yellow | - | wins 100% | lost T8 (65%), T6 | - | lost T6 (92%) | lost T2 (100%) |
| Yellow-Red | - | lost T6 (87%) | lost T4 (100%) | - | lost T6 (92%) | lost T4 (100%) |
| Yellow-Blue | lost T6 (88%) | wins 100% | lost T9 (53%), T10 | lost T6 (80%), T5 | lost T6 (92%) | lost T3 (63%), T4 |

Smacky has one Hut, so only its first node counts. The wrong order costs Yossele up to ten Tides and Spool seven,
and the best order isn't the same in both worlds (Yossele wants Blue or Yellow with a shut gap, Red on an open line,
where Red is what it runs out of). A person reads the node's colour; the PC harness takes the nearest, which on Woozy
is the poorest (Red). That alone is most of the gap between the harness and the smart player in §4.

**Money at the end** (smart player, shut gap; R / B / Y):

| Vessel | Purlest mined | Purlest left | Woozy mined | Woozy left | The Hunt mined | The Hunt left |
| --- | --- | --- | --- | --- | --- | --- |
| Smacky | 3,360 / 3,346 / 3,283 | 117 / 2,851 / 3,168 | 0 / 772 / 0 | 17 / 176 / 15 | 485 / 0 / 808 | 14 / 10 / 690 |
| Yossele | 16,123 / 16,009 / 15,879 | 2,948 / 15,369 / 15,799 | 0 / 7,437 / 7,385 | 33 / 6,917 / 7,255 | 2,731 / 0 / 4,551 | 2,106 / 0 / 4,468 |
| Spool | 12,457 / 12,130 / 13,147 | 4,529 / 159 / 13,057 | 0 / 3,394 / 3,389 | 16 / 62 / 3,267 | 808 / 0 / 1,346 | 25 / 10 / 1,256 |

The model's players leave Blue and Yellow they have no use for; a person might find one, so the model says nothing
about what that surplus is worth. What each vessel runs out of is clear: Smacky **Red** on every map; Yossele **Red**
on Woozy and **Blue** on The Hunt, nothing on Purlest; Spool **Blue** on all three, and **Thread** (on Purlest shut it
spent 12,000 Blue on Cushions and their upgrades and is down to 143 Blue at Tide 14; Thread dropped 1,488 and it spent
1,662).

## 5. Spool: P1-P3 and the open line

P1 Dart Range 4.5; P2 6 Crafted a Tin; P3 Stitched 3 and Dart 3.5 Damage. "As the PC played": the Darts stand where
the Stitched do, so the Maul's slam takes them too (M7). "Held back": every Tin makes Darts and they keep out of the
slam, which is what the first study's package assumed.

| Spool | Setup | Purlest | Woozy | The Hunt |
| --- | --- | --- | --- | --- |
| smart, shut gap | as written | lost T14 (95%) | lost T9 (53%), T10 | lost T5 (67%), T4 |
| smart, shut gap | P1-P3, Darts where the Stitched stand (as the PC played) | wins 63%, else T14 | lost T11 (95%) | lost T5 (87%) |
| smart, shut gap | P1-P3, every Tin on Darts held back out of the slam | wins 100% | lost T11 (95%) | lost T5 (87%) |
| smart, shut gap | P2 and P3 only (no Darts) | wins 72%, else T14 | lost T11 (95%) | lost T5 (87%) |
| smart, open line | as written | lost T5 (100%) | lost T4 (100%) | lost T5 (67%), T4 |
| smart, open line | P1-P3, Darts where the Stitched stand (as the PC played) | lost T5 (55%), T6 | lost T5 (92%) | lost T5 (87%) |
| smart, open line | P1-P3, every Tin on Darts held back out of the slam | lost T5 (98%) | lost T5 (92%) | lost T5 (87%) |
| smart, open line | P2 and P3 only (no Darts) | lost T6 (52%), T5 | lost T5 (92%) | lost T5 (87%) |
| the PC harness, Tin first | as written | lost T4 (95%) | lost T3 (57%), T2 | lost T1 (100%) |
| the PC harness, Tin first | P1-P3, as the PC played | lost T5 (63%), T4 | lost T3 (75%) | lost T1 (100%) |
| the PC harness, Tin first | P1-P3, Darts held back | lost T5 (55%), T4 | lost T3 (75%) | lost T1 (100%) |
| the PC harness, Tin first | P2 and P3 only (no Darts) | lost T5 (85%), T4 | lost T3 (75%) | lost T1 (100%) |
| bot | as written | lost T4 (100%) | lost T2 (87%) | lost T1 (100%) |
| bot | any of the three packages | lost T4 (97%) | lost T2 (65%), T3 | lost T1 (100%) |

- **P2 and P3 carry the package.** Without any Darts they do as well as P1-P3 as the PC played them, on every map, in
  every world.
- **P1 pays only if the Darts stand back.** That's not how the code places them now (M7 saw them where the Stitched
  were); it would need a placement change or a person who moves them. Then Purlest goes from 72% to every run.
- **On an open line no package gives more than a Tide.** The Crafted die to the Maul's slam and the walkers that
  slip past, whatever their numbers, because nothing holds the foes where the Crafted fight.
- **The Hunt doesn't move at all**: Spool has no Blue there (§4) unless it hunts beasts (§8).

**Design questions for an open line** (not proposals; nothing in the scope says Spool should do these):

| Open line | Player | Purlest | Woozy | The Hunt |
| --- | --- | --- | --- | --- |
| Stitched block foes like a wall | smart | lost T10 (73%), T11 | lost T10 (80%), T11 | lost T10 (68%), T8 |
| Stitched block foes like a wall | PC harness | lost T9 (50%), T10 | lost T3 (50%), T2 | lost T1 (100%) |
| Crafted land 4 hits in 10 on a walker (1 now) | smart | lost T9 (60%), T10 | lost T6 (82%) | lost T6 (88%) |
| Crafted land 4 hits in 10 on a walker (1 now) | PC harness | lost T5 (55%), T6 | lost T3 (75%), T2 | lost T1 (100%) |
| Stitched block, with P1-P3 | smart | lost T10 (62%), T11 | lost T11 (92%) | lost T10 (100%) |
| Stitched block, with P1-P3 | PC harness | lost T11 (100%) | lost T3 (43%), T4 | lost T1 (100%) |
| 4 in 10, with P1-P3 | smart | lost T11 (75%), T10 | lost T10 (92%) | lost T8 (62%), T6 |
| 4 in 10, with P1-P3 | PC harness | lost T8 (75%) | lost T4 (52%), T3 | lost T1 (100%) |

A Stitched that holds a foe the way a wall does is worth five or six Tides on an open line, more than every number in
P1-P3 together, and it's the only change here that lifts The Hunt much (Tide 10, without the Cushions Spool
can't afford there). Crafted that keep hitting a walker help less. The harness still loses Woozy early because of its first
node (§4).

## 6. Yossele: P5 and P6

P5 is the Sapling's Attackspeed 0.25 → 0.15. P6 is the fruit's × 15 → × 5. The fruit rows assume a person picks up
half of a grown Sapling's fruit; the others count none, as the first study did.

| Yossele | Purlest | Woozy | The Hunt |
| --- | --- | --- | --- |
| Smacky, smart, shut gap (yardstick) | wins 100% | lost T12 (95%) | lost T10 (60%), T12 |
| smart, shut gap, Sapling 0.25 (as written) | wins 100% | wins 100% | lost T12 (93%) |
| smart, shut gap, Sapling 0.15 (P5) | wins 100% | wins 100% | lost T12 (75%), T10 |
| smart, shut gap, Sapling 0.25, half the fruit picked up, fruit × 15 | wins 100% | wins 100% | wins 100% |
| smart, shut gap, Sapling 0.25, half the fruit picked up, fruit × 5 (P6) | wins 100% | wins 100% | wins 98%, else T14 |
| smart, shut gap, Sapling 0.15, half the fruit picked up, fruit × 15 | wins 100% | wins 100% | wins 100% |
| smart, shut gap, Sapling 0.15, half the fruit picked up, fruit × 5 (P5 and P6) | wins 100% | wins 100% | lost T12 (97%) |
| Smacky, smart, open line (yardstick) | wins 100% | lost T6 (93%) | lost T10 (92%) |
| smart, open line, Sapling 0.25 (as written) | lost T14 (100%) | lost T12 (100%) | lost T12 (100%) |
| smart, open line, Sapling 0.15 (P5) | lost T14 (100%) | lost T12 (83%), T10 | lost T12 (98%) |
| smart, open line, Sapling 0.25, half the fruit picked up, fruit × 15 | lost T14 (98%) | lost T13 (100%) | wins 50%, else T14 |
| smart, open line, Sapling 0.25, half the fruit picked up, fruit × 5 (P6) | lost T14 (98%) | lost T13 (98%) | lost T13 (87%) |
| smart, open line, Sapling 0.15, half the fruit picked up, fruit × 15 | lost T14 (100%) | lost T13 (75%), T12 | lost T13 (68%), T12 |
| smart, open line, Sapling 0.15, half the fruit picked up, fruit × 5 (P5 and P6) | lost T14 (100%) | lost T12 (100%) | lost T12 (60%), T13 |
| bot, Sapling 0.25 (as written) | lost T14 (100%) | lost T10 (75%), T12 | lost T6 (47%), T7 |
| bot, Sapling 0.15 (P5) | lost T14 (100%) | lost T10 (83%) | lost T6 (95%) |

- **P5 changes no map's result.** A quarter of The Hunt's runs (shut) and a fifth of Woozy's (open) end two Tides
  sooner. On Purlest it
  takes Yossele's Red from 16,100 mined to 10,100 and its Red at the end from 2,948 to 272: it still wins, at the edge.
  Yossele stays at or above Smacky wherever it was as written.
- **The fruit is the bigger lever, and only on The Hunt.** No node there pays Blue, so fruit (every colour) is
  Yossele's only Blue. Half picked up, it turns Tide 12 into a win (shut) or a coin toss (open). P6 keeps most of it
  (98% shut, Tide 13 open); P5 and P6 together put The Hunt back about where it is with no fruit at all.
- On Purlest and Woozy fruit changes nothing a run ends on: Yossele already wins shut, and on an open line it adds
  900-2,800 of each colour a map without moving the Tide (§4: there it's the model's ceiling, not money).

## 7. P7, the Loom's scope line

Unchanged by the re-run: scope `08` says the Loom mines "like Smacky's Mining Hut"; as built it mines 80 a Tide by a
Red node (M10), about 4.5× the Hut, and 2,100 by the Opal. The line should say what is built (or the Loom should be
slowed, which the aura baseline's run showed leaves Spool no Blue for its wall). The re-run adds that by the Opal
Spool's Looms keep its Blue ahead of its Cushions until about Tide 10 and then fall behind (143 Blue left at Tide 14);
on Woozy and The Hunt its Blue is short from the start. A slower Loom would make that worse everywhere.

## 8. How much the answers depend on the inputs

**The Opal's low reading** (only the Tide's own colour, as the round counted it):

| Purlest | Opal as measured (every colour a payout) | The Tide's colour only |
| --- | --- | --- |
| Smacky, smart, shut gap | wins 100% | wins 100% |
| Yossele, smart, shut gap | wins 100% | wins 100% |
| Spool, smart, shut gap | lost T14 (95%) | lost T11 (73%), T12 |
| Spool, smart, shut gap, P1-P3 as played | wins 63%, else T14 | lost T13 (80%), T12 |
| Spool, smart, shut gap, P1-P3, Darts held back | wins 100% | wins 100% |
| Yossele, smart, shut gap, P5 | wins 100% | wins 100% |
| Smacky, smart, open line | wins 100% | wins 100% |
| Yossele, smart, open line | lost T14 (100%) | lost T14 (100%) |
| Spool, smart, open line | lost T5 (100%) | lost T5 (97%) |
| Spool, smart, open line, P1-P3 as played | lost T5 (55%), T6 | lost T5 (78%), T6 |
| Spool, smart, open line, P1-P3, Darts held back | lost T5 (98%) | lost T5 (100%) |
| Yossele, smart, open line, P5 | lost T14 (100%) | lost T14 (100%) |
| Spool, the PC harness | lost T4 (95%) | lost T4 (97%) |
| ... with P1-P3 | lost T5 (63%), T4 | lost T5 (68%), T4 |

Only Spool's shut-gap Purlest moves (three Tides as written, one with P1-P3 as played). Everything else in this report
holds under either reading.

**The Hunt's beasts** (aura of each colour a Tide from hunted beasts; 30 is two small ones, 105 a Large one and two
small; M11):

| The Hunt | no beasts hunted | 30 of each colour a Tide | 105 of each a Tide |
| --- | --- | --- | --- |
| Smacky, smart, shut gap | lost T10 (60%), T12 | wins 83%, else T13 | wins 100% |
| Yossele, smart, shut gap | lost T12 (93%) | lost T14 (97%) | wins 100% |
| Spool, smart, shut gap | lost T5 (67%), T4 | lost T10 (100%) | lost T10 (98%) |
| Spool, smart, shut gap, P1-P3 | lost T5 (87%) | lost T10 (100%) | lost T10 (92%) |
| Smacky, smart, open line | lost T10 (92%) | lost T12 (97%) | lost T13 (93%) |
| Yossele, smart, open line | lost T12 (100%) | lost T13 (87%) | lost T14 (62%), wins 37% |
| Spool, smart, open line | lost T5 (67%), T4 | lost T5 (70%), T4 | lost T5 (77%), T4 |
| Spool, smart, open line, P1-P3 | lost T5 (87%) | lost T5 (97%) | lost T5 (93%) |

**The Hunt is decided by Blue.** Its node pays Red and Yellow and kills pay Red, so beasts (and Yossele's fruit, §6)
are the map's only Blue. Two small beasts a Tide take Spool from Tide 5 to Tide 10 with a shut gap (its first Cushions
past two) and Smacky from Tide 10-12 to a win in most runs; Yossele gains two Tides and, in this one cell, ends a Tide
or two under Smacky (it ends with no Blue again). With a Large beast too, Smacky and Yossele win. On an open line the
beasts don't help Spool at all: it has Blue and nothing that holds. These are aura a Tide *if* a vessel kills the
beasts; M11's died to a despawn, so whether a tower or a vessel's attack hunts them, and how often they come back, is
the measure that sets this table's column.

**Other limits.**

- One player, 1D lanes, no Cardinals; his loadout adds 25 Yellow at the start and Coin income through a map.
- The wall is modelled as all or nothing. A real wall may hold some foes some of the time; the two worlds bracket it.
- The smart players' Blue and Yellow surplus (§4) isn't spent; a person might spend it.
- 60 runs a setup: a Tide either way, or ten points of win share, is within the noise.
- Yossele's bot on Woozy is four Tides harsh (§2).

## 9. What it means for Open questions 3, 4 and 6

Nothing here picks for him. The hand test the round asked for, a wall of his own on Purlest and Woozy, decides which
world applies, and the two worlds give different answers:

| If a person's wall... | Spool (Open question 3) | Yossele (Open question 4) |
| --- | --- | --- |
| **holds** (shut gap) | P2 and P3 are worth one to two Tides on Purlest and Woozy (Purlest wins most runs); P1 only if the Darts are made to stand back; P4's "wall first" line is right. The Hunt stays at Tide 5 whatever the numbers (no Blue) unless Spool hunts beasts: two small a Tide give Tide 10 | P5 changes almost nothing; the fruit decides The Hunt, and P6 keeps most of it |
| **is walked round** (open line) | no number buys more than one Tide; P4's line is wrong as worded (the round's M5). What would carry Spool is a design change, such as Stitched that hold foes (Tide 10-11 everywhere in the model) | the same, but Yossele falls at Tide 12-14 instead of winning Purlest and Woozy (Purlest's Tide 14 is the model's ceiling, §4); still at or above Smacky on Woozy and The Hunt |

P7 stands either way. Open question 6 ("re-run the simulator before picking") is answered by this report.

## 10. Left

- **His wall by hand** on Purlest and Woozy (the round's ask): which row of §9 applies.
- **The Opal's reading**: the M2 log's payout lines (each payout should pay 10 of each colour plus the Tide's 15). The
  round's summary already shows 50 or 60 of each colour a Tide; if those were something else, §8's low reading applies.
- **Yossele's bot on Woozy**: which nodes its Saplings stood by in the round's log (the model's bot takes Red and
  reaches Tide 10; the game reached 14).
- **The Hunt's beasts**: how often they come back, and whether a vessel's own attack kills them (M11's were despawns).
- **Fruit**: how much of a grown Sapling's fruit a player picks up in a real run.
- **Woozy's first node**: the PC harness and any future bot should read a node's colour, not its distance.

## Files

`balance_rerun_2026-11-03/`: the simulator (a copy of `balance_2026-11-03/`, which stays frozen), its README (every
change and knob), the experiments (K calibration, L baselines, M Spool, N Yossele, O the Opal's low reading, P The
Hunt's beasts), the results with each run's lost Tide, `mining.py` for §3 and `report_tables.py` for every table here.
