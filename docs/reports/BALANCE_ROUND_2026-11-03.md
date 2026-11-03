# The balance round, 2026-11-03: what the PC says about the model

The PC round that `BALANCE_2026-11-03.md` §9 asks for, with M8-M11 of `AURA_BASELINE_2026-11-03.md` §7. **No shipped number changed.** The picked numbers went
into play builds only, behind `SPOOL_TUNE`. Logs are in `handoff\play_logs` (`Spool_20261103_*`, `Yossele_20261103_*`). The harness is `Code/97_Measure.cs` of
Yossele and Spool, and `ModKitMeasure` at the end of the kit's `ModKitPlay.cs`.

**In short.** The model's numbers for a node, a gatherer and a Crafted hold up well. **Its wall doesn't.** Foes walk round three Pin Cushions or three
Stonehenges across the last stretch, on Purlest and on Woozy, and a slam takes the line's Crafted just as predicted. So the "smart player" the study's biggest
predictions rest on doesn't exist yet: walling first lost Tide 1-2 in every run, not Tide 11. Played Tin first, by a bot, P1-P3 give Purlest one more Tide (5 against 4)
and Woozy nothing (2 against 2). I'd hold P1-P3 and P4 until you've tried a wall by hand, since a person places walls better than this bot does.

## Step 1: the measures

| # | Measure | The model's guess | The game | Verdict |
| --- | --- | --- | --- | --- |
| M1 | No Cardinals, the start wallet | R50 B50 Y50 | **R50 B50 Y50** on all three maps. With your loadout (Voice, Green Coin, Blue Coin, mixed Yellow Coin) it's Y75 | holds |
| M2 | Purlest's Opal, a Sapling beside it, 45 game s a Tide | about 9.5 a strike; Tide 1 mostly Red, 2 Blue, 3 all, 4 Yellow | Tide 1 Yellow +75 (11 strikes), 2 Red +90 (12), 3 Blue +90 (14), 4 all +60 each (14): **6.4-7.5 a strike, 12.9 on the all-colours Tide**. Each Tide also pays 50 (Tide 1) or 60 (Tides 2-4) of each colour | about right; **the colour order is Yellow, Red, Blue, all** |
| M3 | Woozy's nodes | about 1 a strike | Red: **1** a strike (10 in 10). Yellow and Blue: **5** a strike (50 in 10). Purple: its tile refused the Sapling | **Yellow and Blue pay 5×** |
| M3 | The Hunt's node | about 1 a strike | Orange: R18 Y30 in 10 strikes, **4.8 a strike** | **more than the guess** |
| M4 | The Hunt's Orange from (-7,7,-27), where the 2026-10-26 Sapling struck 0 times | a query or position fault | **10 strikes in 40 s**, node in reach. The old 0 didn't come back; the tile one nearer refuses a building | no fault now |
| M5 | Does a line of walls hold? | foes stand at it while all three stand | **No.** Three Pin Cushions on Purlest: 4 of 5 foes past, the walls untouched, map lost in Tide 1. On Woozy: 5 of 5 past. Three Stonehenges on Purlest: 3 past | **the model is wrong here** |
| M6 | Do Crafted hit walkers? | about 1 in 10 | **5-10%** of their damage lands on walkers as written, 19% with P1-P3 | holds |
| M7 | The Maul against 3 Cushions, 12 Stitched, 4 Darts | one slam kills every Crafted within 2.5, a Cushion dies to two | first slam: 12+4 down to 2+1, and every Cushion from 100 to 50. Second: 2 Cushions gone, no Crafted left. **The Maul lost 146 of 700 before the slam.** With P1-P3 (18+6, Range 4.5): 207 before it, and the same slam took the same share; the Darts stood where the Stitched did, so Range 4.5 alone didn't save them | holds; P1 doesn't help a Dart that keeps to the same spot |
| M8 | Nodes on the world map's six maps | Purlest 1, Woozy 4, The Hunt 1 + beasts | Purlest 1 (Opal, Healthresource 5, 20 of each, Regular 15, All 10). Woozy 4. The Hunt 1 + 2 Dens + 2 Large Dens + their beasts. Kong 2. Manafold 4. Teri Tori 4. Red and Blue/Yellow have Healthresource 4 or 5 | holds |
| M9 | Stock gatherers alone by a Red node, 90 game s | 18, 18, 17 a Tide | Hut, Sno Box, Silo: **one payout of 5 each** in 90 s, or 13 a Tide, within one payout of the guess. The windows are too short to separate them. The Checkpoint didn't load in a Yossele launch | holds, roughly |
| M10 | A Sapling | 60 / 90 a Tide | new: 15 aura in 60 game s, **60 a Tide**. Grown: 25 in 60 s, **100 a Tide**, plus 5 fruits | holds |
| M10 | A Loom | 96 a Tide | 20 aura in 60 s, **80 a Tide**; 23 strikes, 9 Thread | holds |
| M10 | A Loom with its Seam | 288 a Tide | 70 in 60 s, **280 a Tide**. The Seam does hit the node | holds |
| M11 | The Hunt's beasts | unknown | `Mob_Aurabeast` (30 health): **15 of each colour**. `Mob_Largeaurabeast` (200): **75 of each colour**. Killed by a despawn, not by the laser | new |

## Step 2: the package on full maps

A bot played these, on each map's own income with no Cardinals, through the HUD. The wall-first order is the study's "smart" player.

| Run | The study's prediction | As written | With P1-P3 |
| --- | --- | --- | --- |
| Spool, Purlest, wall first | lost Tide 10-12 / wins | lost **Tide 1, Tide 1** | lost **Tide 1, Tide 2** |
| Spool, Woozy, wall first | lost Tide 7-8 / Tide 10-12 | lost **Tide 2** | lost **Tide 1** |
| Spool, Purlest, Tin first (the study's "bot") | lost Tide 4 / Tide 4 | lost **Tide 4, Tide 4** | lost **Tide 5, Tide 5** |
| Spool, Woozy, Tin first (the study's "bot") | lost Tide 2 / Tide 3 | lost **Tide 2** | lost **Tide 2** |
| Yossele, Woozy, as written | wins; passes past Tide 12 | lost **Tide 14**: passes | |

**Pass or fail, by §9's own lines.** "The package clears Tide 11 in both runs, and as written loses at Tide 10-12": **fails**, on the wall. "A miss by more than two Tides means the model is off on that map: read the logs (M5-M7 first) before changing a number": that's what happened, and M5 says why. The bot's own order matches the study's bot within a Tide, and Yossele's check passes, so the rest of the model stands.

## What it means for his picks

Nothing here picks for him. PLAN's Open questions 3 and 4 stand, with this to weigh:

- **P1-P3** bought one Tide on Purlest for a bot and none on Woozy. The study's "wins Purlest" rested on a wall that foes walked round. Hold them until a person has tried it.
- **P4**, "wall the path with Pin Cushions first", isn't true as worded in these runs. Walling first lost faster than anything else. A wall in a choke, or one that closes the path, may behave differently; this round tried three tiles across the last stretch of each lane.
- **P5** (Sapling Attackspeed 0.15): M10's 60 and 100 a Tide confirm what it's measured against. But The Hunt and Woozy's Yellow and Blue pay about 5× a Red strike, so a Sapling on those is worth far more than the Red-based table says. The simulator should be re-run before he decides.
- **P6** (fruit): a grown Sapling's 5 fruits in 60 s are the part of its worth this round couldn't price cleanly: left on the ground, the next window's Hut brought in 30 Blue and 30 Yellow from them.
- **P7** (scope `08`'s Loom line): the Loom's 23 strikes in 60 s is what's built, so the line should say so, as P7 proposes.
- **His loadout** adds 25 Yellow at the start and more through a map. The study ran with no Cardinals, as his players may not.

## Left

- **Re-running the simulator** with these numbers (`docs/reports/balance_2026-11-03/`, §9 Step 3): it's Python and the PC has none, so it's a job for a cloud thread. This report has what it needs: M2, M3 and M5 change its inputs most.
- **Writing the picked numbers** into scope `08` and Spool's `DESIGN.md`: waits for his word.
- **Limits of this round:** a bot, not a person; two to three runs a line, so a Tide either way means little; M9's windows are short; M11's kills are despawns.
- **The Cardinal loadout** on his save was emptied for each launch and put back at its end; the last launch logged Voice, Green Coin, Blue Coin and mixed Yellow Coin back in place. A run that stalled emptied it once and the next run's end restored it.
