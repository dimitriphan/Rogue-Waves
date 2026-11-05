# Scope against code, second pass (2026-11-05)

Dimitri, in the project (05:08Z): "code-review at max and do another Scope-to-code audit". This is the audit. The max
code review runs in its own thread and looks for engine-rule bugs; this page stays on one question: does the code do
what `docs/scope/00`-`16` says?

**Base.** `claude/phase-1-6lfnar` at `99b9f3d` (PR #97 merged). No game code changed since PC round 3 (PR #91); the
scope changed in PR #95 (Dimitri's answers, 1,192 tildes removed in 04 and 09-14) PR #96 and PR #97 (Spool's defaults taken, B16-B19). Code paths below are
relative to `TDS_Mod/ModSource`; scope lines are `file:line` in the scope as it stands after this PR (only the lines
this PR edits moved, and none moved by more than the one Log line in 02).

**How each line is marked.**

| Mark | Means |
| --- | --- |
| MATCH | the code does what the line says, at the location given |
| RESOLVED | a row of the first audit (`SCOPE_CODE_AUDIT_2026-11-04.md`) that no longer disagrees, and why |
| UNBUILT-QUEUED | not built, and already on the queued build list: `DECISIONS_APPLIED_2026-11-05.md` §3 B1-B15, §4 B16-B19 and A1-A4, the scope 07/08 upgrades, the new Directions, Trinkets and Cardinals, Argo's review, Uloe's 24 upgrades; or the balance sweep's S1-S2 |
| UNBUILT-BY-DESIGN | not built, and the scope itself says so (Depth on hold, a `! NEEDS !` slot) |
| DIFFERENT | built, and the code does something else. Only rows here are new work |
| MISSING | the scope asks for it, nothing builds it, and nothing queues it |

**Nothing in the game's code changed.** Six scope files got text fixes that follow Dimitri's own decisions or the
latest PC round (§3, N1-N4); everything else is a hand-off. Nothing ran on the PC and nothing was compiled: this is a
docs-only change, read from the code.

## 1. Summary

"Slot lines" counts every `- Field:` and `% N:{Upgrade}` line in the file; "rows" are the table rows of §5, one per
checkable statement (an upgrade counts once, with its level, cost, in-game line and Leads to checked together; a
"see" row and the upgrade summary rows are not counted, and 02's ten [Now] lines and 15:245 count as rows though
they sit in prose).

| File | Slot lines | Rows | MATCH | UNBUILT-QUEUED | UNBUILT-BY-DESIGN | DIFFERENT / MISSING (new) | First audit's rows | Now |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | --- |
| `00_Read_Me` | 53 | 0 | | | | 0 | 0 | rules only |
| `01_Expansion` | 20 | 8 | 7 | | 1 | 0 | 0 | |
| `02_Progress` | 44 | 10 | 10 | | | 0 (1 fixed here) | 0 | |
| `03_Bug_Fix` | 8 | 4 | 4 | | | 0 | 0 | |
| `04_Rogue_Waves_Mode` | 67 | 46 | 41 | 2 | 3 | 0 | 5 | 3 RESOLVED, 2 queued (B1, B2) |
| `05_Gameplay_Rundown` | 9 | 8 | 7 | 1 | | 0 | 0 | |
| `06_New_Mechanics` | 31 | 25 | 23 | 2 | | 0 | 0 | |
| `07_Vessel_Yossele` | 249 | 48 | 46 | 1 | 1 | 0 (2 fixed here) | 4 | 4 RESOLVED |
| `08_Vessel_Spool` | 289 | 52 | 26 | 26 | | 0 (3 fixed here) | 1 | queued (B6) |
| `09_Vessel_Argo` | 258 | 53 | 34 | 19 | | 0 (2 fixed here) | 6 | 1 RESOLVED, 5 queued (B12-B14) |
| `10_Vessel_Uloe` | 224 | 45 | 18 | 27 | | 0 | 1 | RESOLVED |
| `11_Directions` | 550 | 29 | 23 | 6 | | 0 | 10 | 4 RESOLVED, 6 queued (B7, B8, B15) |
| `12_Cardinals` | 192 | 16 | 10 | 5 | 1 | 0 | 4 | 4 queued (B3, B4) |
| `13_Enemy_Modifiers` | 32 | 10 | 9 | 1 | | 0 | 4 | 2 RESOLVED, 2 queued (B5) |
| `14_Optional_Objectives` | 59 | 12 | 8 | 3 | 1 | 0 | 4 | 1 RESOLVED (PR #89), 3 queued (B15, A3) |
| `15_Templates` | 181 | 1 | 1 | | | 0 (1 fixed here) | 1 | RESOLVED |
| `16_Base_Vessels` | 1,197 | 2 | 2 | | | 0 (1 fixed here) | 1 | RESOLVED |
| **Total** | **3,763** | **369** | **269** | **93** | **7** | **0** | **41** | **18 RESOLVED, 23 queued** |

So: of the first audit's 41 rows, 18 no longer disagree and the other 23 all sit on the queued lists. This pass found
**no new behaviour mismatch** in the built game. It found **ten new items** (§3): four scope texts that had fallen
behind Dimitri's newest words or the latest PC round (fixed in this PR), and six corrections or additions to the
queued hand-off that the build, art and balance threads should take with them.

Scope 16's 1,195 base-vessel lines are the game's own data and were checked against it in the first audit; nothing in
the game or in those lines changed since, so only the side-by-side table (where the pack's vessels sit) was re-read.
The first audit's §5, 44 base-vessel lines only the decompiled game can settle, still stands.

## 2. The first audit's 41 rows, now

| First audit row | What | Now | Where it stands |
| --- | --- | --- | --- |
| 04 row 1 | No map vote after a bonus-Tide loss or a host quit | UNBUILT-QUEUED (B1) | `RogueWavesMode/Code/20_Vote.cs:353-397` still asks only after the leave vote; `05_Boat.cs:167-168` lets the leader steer |
| 04 row 2 | Lobby Cardinal picker free and unfiltered between maps | UNBUILT-QUEUED (B2) | scope 04:62 now says "picked once, then locked"; `17_Cardinals.cs:387-395` still only restores the list |
| 04 row 3 | Bonus Tides' +10% only from Tide 30 | RESOLVED | scope 04:24 now says so; `21_Difficulty.cs:199-226` |
| 04 row 4 | The modifier schedule and the stand-in elite | RESOLVED | scope 04:20 now says both; `21_Difficulty.cs:156-195`, `:93` |
| 04 row 5 | The weather's line shows on the boat, not at the map's start | RESOLVED | scope 04:71 and, in this PR, the template 15:245; `07_Weather.cs:29-38`, `:263` |
| 07 row 1 | FERTILIZE's 6 tiles is only the hotkey's pick | RESOLVED | scope 07:36 note; `Yossele/Code/06_HeroBrain.cs` unchanged |
| 07 row 2 | Natural Remedy counts stage 1 before growing | RESOLVED | scope 07:201 note; `Yossele/Code/10_Henge.cs` |
| 07 row 3 | The 90% damage-reduction cap | RESOLVED | scope 07:223 note; `Yossele/Code/02_Data.cs:188` |
| 07 row 4 | Focused Roots +80%, added to the other bonuses | RESOLVED | scope 07:247; `Yossele/Code/02_Data.cs:296` |
| 08 row 1 | The pack's modifiers aren't elite for Spool's Thread | UNBUILT-QUEUED (B6) | scope 08:46 now says they are; `Shared/ModKitUtil.cs:495-503` still matches `Enemy_` only (used only by `Spool/Code/05_Server.cs:283`, so widening it touches nothing else) |
| 09 row 1 | Bosses can be pinned and held | UNBUILT-QUEUED (B13) | `Argo/Code/01_Util.cs` Pin and Root, no health check |
| 09 rows 2-4 | Slows stack with their own re-ticks | UNBUILT-QUEUED (B13) | scope 09:36 says one slow per source; unchanged code |
| 09 row 5 | Salt Sting 1 a second without Brine | UNBUILT-QUEUED (B12) | the scope line 09:179 and the in-game EFFECTS still say 0.5; B12 changes both (see N7) |
| 09 row 6 | Seven review lines not marked | RESOLVED | each of 09:43, 59, 176, 186, 196, 238, 248 now carries "a new line; the build still shows the earlier one" |
| 10 row 1 | A Mirror's level 1 from any Light | RESOLVED | scope 10:52; `Uloe/Code/10_Mirror.cs` |
| 11 row 1 | Tidal Mend heals x the tower's level | UNBUILT-QUEUED (B7) | `RogueWavesMode/Code/16_Directions.cs:35-37` still rides `Smackywall_Regen` |
| 11 row 2 | Cut Costs offered to Boutman does little | RESOLVED | Dimitri's answer 16: stays as built (the same amount off his fish price, `16_Directions.cs:308-312`) |
| 11 row 3 | Cheap Refits cancels Voice in a Bottle | RESOLVED | PR #89 (MODE-C4): a cut is an amount kept per command, `16_Directions.cs:330-357` |
| 11 row 4 | Made of Tougher Stuff misses Boutbro | UNBUILT-QUEUED (B15) | `16_Directions.cs:406` still skips every `Hero` |
| 11 row 5 | Full Gallop also grows Buster's hits | RESOLVED | scope 11:522 note |
| 11 rows 6, 7, 9 | The Shift Directions don't rename the building | UNBUILT-QUEUED (B8, art A1) | `Yossele/Code/02_Data.cs:118-141`, `Spool/Code/02_Data.cs:115-122` change numbers only |
| 11 row 8 | Compost Bin III says "anywhere" | UNBUILT-QUEUED (B15) | `Yossele/Code/02_Data.cs:109` |
| 11 row 10 | Flash Bulb's look | RESOLVED | scope 11:510 "flash bulb, still smoking" |
| 12 row 1 | One mixed shelf; no Trinket slot | UNBUILT-QUEUED (B4) | `17_Cardinals.cs:167-180` |
| 12 row 2 | A Trinket shows nowhere | UNBUILT-QUEUED (B4) | `17_Cardinals.cs:406` skips a scriptless upgrade |
| 12 row 3 | No "+N" past the row's 8 | UNBUILT-QUEUED (B4) | `17_Cardinals.cs:263` |
| 12 row 4 | A held stock Cardinal still on the shelf | UNBUILT-QUEUED (B3) | `17_Cardinals.cs:167-180` draws from all of `Sold`; `:194`, `:200` refuse the buy |
| 13 row 1 | Cursed also weakens vessels | RESOLVED | Dimitri's answer 10; scope 13:10 now names vessels, and the code already does it (N5) |
| 13 row 2 | Dazzling's 20% vs Light level 1 | UNBUILT-QUEUED (B5) | scope 13:41 says 15%; `RogueWavesFoes/Code/00_RogueWavesFoesMod.cs:84` still 0.2 |
| 13 row 3 | Illuminate lingers 0 s | UNBUILT-QUEUED (B5) | `00_RogueWavesFoesMod.cs:355-360` removes it at the next check |
| 13 row 4 | Empty in-game lines | RESOLVED | scope 13:9, 19, 29, 39 = `00_RogueWavesFoesMod.cs:137-140` |
| 14 row 1 | Mind the Meteor isn't a white crystal | UNBUILT-QUEUED (A3) | `RogueWavesMode/Code/19_Objectives.cs:63-87` copies a stock ore node |
| 14 row 2 | 16 hits on map 1, not 15 | UNBUILT-QUEUED (B15) | `19_Objectives.cs:268` writes 12 + 3 x map; its own comment at `:267` says N hits need N - 1 |
| 14 row 3 | Mind the Vessels failed on a Boutbro | RESOLVED | PR #89 (MODE-D3): `19_Objectives.cs:215-217` counts only `RWRun.IsVessel` |
| 14 row 4 | The objectives' looks aren't drawn | UNBUILT-QUEUED (A3) | one board icon for all, `19_Objectives.cs:205` |
| 15 row 1 | A Cardinal's Colour is its lobby slot | RESOLVED | scope 15:178 |
| 16 row 1 | Uloe missing from the side-by-side table | RESOLVED | scope 16:19 (and this PR takes its tildes off, N4) |

## 3. New items (numbered for the queued threads)

None of these is a bug in what plays. N1-N4 are fixed in this PR's scope text; N5-N10 are for the threads that come
next. Each fix is Claude's recommendation, written `~like this~`.

1. **N1. Spool is still purple in the scope** (08:12 Look, 08:14 Accent colour `~plush purple~`). Dimitri made it sage
   green on 2026-11-05 (PR #90; `docs/STYLE.md` §"Vessels", `93CCA1` / `60A871`), and every Spool sprite and UI colour
   in the build is sage. **Fixed here:** `~"hooded sage green plush"; Accent colour: sage green~`, with the hex in a
   Claude note.
2. **N2. The Cordyfriend's temporary look** (07:303) still described the old puffball; his pick of 2026-11-05 is v04
   Antlers (`docs/TEMP_ART.md` "Picked"), the sheet the build copies. **Fixed here** in the Claude note; the 24 px cells
   it states are still the build's (`Yossele/Code/03_Assets.cs:32`).
3. **N3. Self-test counts behind** in 02:9, 07:17 (173), 08:17 (133) and 09:22 (142): PC round 3 read Yossele 177/0,
   Spool 135/0, Argo 144/0 (`docs/reports/TESTING_ROUND_3_2026-11-05.md`). **Fixed here.** 09:9 also still spoke of
   "every ~ value" after PR #95 took Argo's tildes off; reworded so the balance pass it waits for stays named.
4. **N4. 16's side-by-side table** kept tildes on Argo's, Uloe's and Spool's rows after their own sheets lost them in PRs
   #95 and #97, and
   15:245 (the weather template) still called the in-game line "the line announced when the map starts" after his
   answer 8. **Fixed here:** the three rows plain, and the table's note no longer points at Spool's tildes; 15:245 reads `~the
   line in the boat's map panel when the map is picked; the map's start announces only the weather's name~`.
5. **N5. B5's first half is already built.** Cursed already cuts the Damage of every vessel, tower and unit of the
   players' teams within 3 tiles by 25% (`RogueWavesFoes/Code/00_RogueWavesFoesMod.cs:299-310` reaches `Tower`, `Mob`
   and `Hero`; `:324-336`). Only the Bouthouse is left out on purpose (FOES-2, pitfall #85). **For the build thread:**
   `~drop "Cursed's aura also cuts vessels' attack 25%" from B5; keep Dazzling's 15% and Illuminate's 3 s linger~`. The
   part's header comment (`:7`, "nearby towers and units") is the only place that still says otherwise; `~fix it with
   B5's change~` (comments are code, so this PR leaves it).
6. **N6. B11's size.** `DECISIONS_APPLIED_2026-11-05.md` §1 row 19 says "the 15 new Directions"; scope 11 holds **32**
   Claude-made Directions that nothing builds: 15 for any vessel (Wound Spring, Fine Sieve, Spare Hands, Galley Kettle,
   Chain Shot, Salt Barrel, Fog Horn, Clam Shell, Notched Oar, Lamp Oil, Fish Hook, Spare Compass, Magpie Nest, and the
   Duos Fire Ship and Tide Pool), 2 for Argo (Chum Bucket, Pressure Gauge), 5 for Yossele (Cutting Jar, Sap Bucket,
   Creeping Ivy, Deep Breath, Fairy Ring), 5 for Spool (Double Needle, Button Jar, Pattern Book, Merit Badge, Odd Socks)
   and 5 for base vessels (Mason's Trowel, Snow Shovel, Worm Tin, Open Sign, Long Leash); plus 11 Trinkets and 4
   Cardinals in 12. B11's own text ("every Claude-made Direction, Trinket and Cardinal") covers all of them. **For the
   build thread:** `~plan for 32 + 11 + 4 = 47 sheets, and A2 for 47 icons~`.
7. **N7. Pairs come with B11, and one built Direction already names its pair.** Spring Tide (built) says "Clashes:
   {Wound Spring}: a player holds one or the other" (11:52), but its definition has no Clash (`16_Directions.cs:42-44`)
   because Wound Spring isn't built. **For the build thread:** `~when Wound Spring is built, give both the same Clash
   name in RWDirectionDef~`; `Roll` (`16_Directions.cs:113-165`, `:151`) already keeps a held clash from being offered. The same
   holds for every other pair in 11 and 12 (12:21 "Pairs").
8. **N8. Salt Sting's text change touches the code too.** B12 says "the in-game text and scope 09's line say 1 a
   second without Brine". The in-game EFFECTS line is code (`Argo/Code/02_Data.cs:323-324`, "0.5 a second"), so B12 is
   a build item, not a text-audit one; scope 09:181 changes in the same PR. `~B12 stays with the build thread~`.
9. **N9. The scope lines the balance sweep owns still carry tildes on purpose:** in 08 the Stitched's and the Dart's
   Damage and the Dart's Range (08:105, 08:116-117), Crafted alive a Tin (08:160) and the Loom's mining (08:311, S1-S2);
   10:218 (the Flood Light's mining) and 10:51's Light-sharing limit `~4~` (B10). **For the balance sweep:** `~write
   the measured numbers into these lines without tildes once Dimitri sees them~`.
10. **N10. Leads to lines that point at unbuilt upgrades.** Pin Barrage leads only to Iron Thimble (08:279; already B18), Burning
    Point to Polished Lens and Quick Focus (10:72), Short Circuit to Static Charge and Surge Protector (10:127). The
    code's `next` for each is empty today (`Spool/Code/02_Data.cs:262`, `Uloe/Code/02_Data.cs:187`, `:199`). **For the
    build thread:** `~set each one's next when its targets are built, and check the other upgrades' Leads to the same
    way~` (every other built upgrade's Leads to matches, §5).

## 4. What the queued threads take from this page

- **Build thread** (`DECISIONS_APPLIED_2026-11-05.md` §3): B1-B19 as written, with N5 (B5 shrinks), N6 (47 sheets),
  N7 (pairs), N8 (B12 is code) and N10 (Leads to). Every UNBUILT-QUEUED row in §5 names its B item or list.
- **Art (A1-A4):** A2 is 47 icons (N6); A3 and the Shift looks unchanged.
- **Balance sweep (S1-S2):** N9.
- **Text audit:** nothing new from this pass; N1-N4 are done here.

## 5. File by file

### 00_Read_Me.txt

The scope's own rules; nothing for the code to match.

### 01_Expansion.txt

| Line(s) | Says | Code | Mark |
| --- | --- | --- | --- |
| 01:5 | One pack, every part inside, no load order | `Pack/pack.txt`; `Shared/ModKitPack.cs` (each part joins, `RogueWavesFoes/Code/00_RogueWavesFoesMod.cs:29`) | MATCH |
| 01:6 | Four vessels | `Yossele/`, `Spool/`, `Argo/`, `Uloe/` | MATCH |
| 01:7 | Four modifiers, enemy types later | `RogueWavesFoes/Code/00_RogueWavesFoesMod.cs:103-104` | MATCH |
| 01:9-13 | Mode, Directions, Fund Transfer, Cardinals and Trinkets | `RogueWavesMode/Code/16_Directions.cs`, `17_Cardinals.cs`, `18_Shop.cs:387-430` | MATCH |
| 01:14 | Bug fixes | see 03 | MATCH |
| 01:17-19 | 2-4 players, host runs it, same pack | rules for players; nothing enforces 2 or the version (first audit 01 notes) | MATCH |
| 01:22-26 | First release Yossele alone; no FERTILIZE outside the mode | `Yossele/Code/06_HeroBrain.cs` shows FERTILIZE only with a Compost Bin, a mode Direction | MATCH |
| 01:24, 27-28 | Version 0.1.0 pack, ready when he says ship | parts carry `0.1.0-dev` (`RogueWavesFoes/Code/00_RogueWavesFoesMod.cs:79`); Release 1 waits on his sprites and play test | UNBUILT-BY-DESIGN |

### 02_Progress.txt

The [Now] lines (02:6-15) read against the code and the reports they cite: all MATCH after N3 (02:9's count). The [Log]
is history and was not audited.

### 03_Bug_Fix.txt

| Line(s) | Says | Code | Mark |
| --- | --- | --- | --- |
| 03:33-35 | F picks the vessel, in every pack vessel | `Shared/ModKitFocus.cs`, started by `Argo`, `Spool`, `Uloe` and `Yossele`'s `Code/00_*Mod.cs` | MATCH |
| 03:36 | Passed on a real death | `docs/reports/TEST_ROUND_2026-11-04.md` | MATCH |
| 03:41 | The host keeps each seat; the lobby opens while one is empty | `RogueWavesMode/Code/09_Rejoin.cs` | MATCH |
| 03:42 | Passed with a second player; Steam's Join Game left | `TEST_ROUND_2026-11-04.md` | MATCH |

### 04_Rogue_Waves_Mode.txt

All paths in `RogueWavesMode/Code/`.

| Line(s) | Says | Code | Mark |
| --- | --- | --- | --- |
| 04:10 | 10 maps | `03_Chart.cs:7` | MATCH |
| 04:11 | Rows 5 and 10 close a stretch | `03_Chart.cs:8`, `:132` | MATCH |
| 04:12 | Vessels locked in | `05_Boat.cs:171-193` | MATCH |
| 04:13 | Main-menu button, New or Continue, to the boat | `12_Menu.cs:1-60` | MATCH |
| 04:14 | Saved between maps | `06_Campaign.cs:161-175` | MATCH |
| 04:15 | Continue brings back vessel, Pure Aura, Directions, Trinkets, Cardinals | `06_Campaign.cs:77-107`; `14_Run.cs:145-189` | MATCH |
| 04:16 | Won by leaving map 10 after Tide 15 | `06_Campaign.cs:228-250`, `:307-311` | MATCH |
| 04:16 | A new Depth level unlocks | Depth on hold (04:42) | UNBUILT-BY-DESIGN |
| 04:17 | A failed campaign keeps nothing | `06_Campaign.cs:148-159` | MATCH |
| 04:18 | Score doesn't count | `13_Card.cs:66`, `:72-85` | MATCH |
| 04:19 | Which modifiers change per campaign | `! NEEDS !` | UNBUILT-BY-DESIGN |
| 04:20 | 2 pack + 2 stock from the seed; none on maps 1-2; one from map 3, one more every second map; one foe in five; the stand-in | `21_Difficulty.cs:156-166`, `:176-195`, `:93` | MATCH (RESOLVED, row 4) |
| 04:23-24 | Foes +6% a map and +35% a player; intervals -3%; health +8%; bonus Tides | `21_Difficulty.cs:12`, `:60-112`, `:114-152`, `:199-226` | MATCH (RESOLVED, row 3) |
| 04:27 | 3 retries | `06_Campaign.cs:21` | MATCH |
| 04:28 | A loss replays from the last save; none left ends it | `06_Campaign.cs:288-305` | MATCH |
| 04:29 | The host quitting loses the map | `06_Campaign.cs:228-265` | MATCH |
| 04:30 | A friend who drops spends nothing, rejoins | `09_Rejoin.cs` | MATCH |
| 04:31 | Dying in a bonus Tide loses nothing; that Tide pays nothing | `06_Campaign.cs:206-226`; pay at the break, `20_Vote.cs:73-77` | MATCH |
| 04:32 | After a loss, only that map | `06_Campaign.cs:43-53`, `:294-297` | MATCH |
| 04:33 | Status | `BUILD_2026-11-03.md` | MATCH |
| 04:36-42 | Depth | on hold | UNBUILT-BY-DESIGN |
| 04:45 | 60 s breaks | `02_Mode.cs:10`, `:30-38` | MATCH |
| 04:46 | Won once Tide 15 is cleared | `06_Campaign.cs:22`, `:206-226`; `20_Vote.cs:130-137` | MATCH |
| 04:47 | The vote to leave, by majority | `20_Vote.cs:353-397` (`:382`) | MATCH |
| 04:48 | Only between Tides | `20_Vote.cs:130-137` (`InBreak`, field clear) | MATCH |
| 04:49 | Bonus Tides without limit | `21_Difficulty.cs:199-226` | MATCH |
| 04:50 | Status | `BUILD_2026-11-03.md` | MATCH |
| 04:53 | A harbour, then 10 rows of 3 or 4 maps | `03_Chart.cs:102-126` (`:109` 3 or 4 a row) | MATCH |
| 04:54 | 1 to 3 links | `03_Chart.cs:10`, `:71-98` | MATCH |
| 04:55 | Every base map, never the custom slot | `02_Mode.cs:75-86` | MATCH |
| 04:56 | No map twice | `03_Chart.cs:134-150` (`:148` takes each map out) | MATCH |
| 04:57 | Easier first, hardest closing a stretch | `03_Chart.cs:67-68`, `:139-147` | MATCH |
| 04:58 | Every map is voted on | only after the vote to leave, `20_Vote.cs:368-397` | UNBUILT-QUEUED (B1) |
| 04:59 | The vote offers the linked maps; map 1's in a staging map | `20_Vote.cs:270-296`; `06_Campaign.cs:43-53`; `20_Vote.cs:52` | MATCH |
| 04:60 | Back to the boat, which sails to the winner | `20_Vote.cs:399-420`; `05_Boat.cs:167-168`, `:246-247` | MATCH |
| 04:61 | No Cardinal or Trinket swaps on the boat | bought ones regranted each map, `17_Cardinals.cs:343-357`, `:387-395` | MATCH (lobby picks: 04:62) |
| 04:62 | Lobby Cardinals picked once, locked, filtered | nothing locks or filters the lobby picker | UNBUILT-QUEUED (B2) |
| 04:63 | Each map's own island | `04_World.cs` | MATCH |
| 04:64 | Status | `BUILD_2026-11-03.md` | MATCH |
| 04:67-69 | Weather rolled with the chart; calm rows; 1 in 3 then 1 in 2 | `03_Chart.cs:151-163` | MATCH |
| 04:70 | +1 Pure Aura a Tide | `07_Weather.cs:15`; `14_Run.cs:247` | MATCH |
| 04:71 | Shown on the chart and the boat's panel; the start announces the name | `07_Weather.cs:29-38`, `:263`; Quickening Current also announces at Tides 5 and 10 (`:282`, `:288`) | MATCH (RESOLVED, row 5) |
| 04:72 | Never on a map that has it | `03_Chart.cs:55-64`, `:161` | MATCH |
| 04:76-80 | Rising Water | `07_Weather.cs:33`, `:40-41` (Manafold's Main_Flood); icon rows `08_Icons.cs` (temporary art) | MATCH |
| 04:83-87 | Quickening Current x1.25 from Tide 5, x1.5 from 10 | `07_Weather.cs:17`, `:34`, `:282-288` | MATCH |
| 04:90-94 | Nesting Ground 3-7 tiles from an enemy core | `07_Weather.cs:35`, `:358-389` (`:370`) | MATCH |

### 05_Gameplay_Rundown.txt

| Line | Says | Code | Mark |
| --- | --- | --- | --- |
| 05:4 (step 1) | Lobby, vessels locked, the button | as 04:12-13 | MATCH |
| 05:5 (step 2) | 3 Directions, pick 1 | `16_Directions.cs:21`; `20_Vote.cs:218-268` | MATCH |
| 05:6 (step 3) | Every map voted on | as 04:58 | UNBUILT-QUEUED (B1) |
| 05:7-8 (steps 4-5) | Pure Aura each Tide, objectives, VENDCINT | as 06 and 14 | MATCH |
| 05:9 (step 6) | Leaving | as 04:47-49 | MATCH |
| 05:10 (step 7) | 10 maps, each harder | as 04:10, 04:24 | MATCH |
| 05:11 (step 8) | Saved between maps | as 04:14-15 | MATCH |
| 05:12 (step 9) | Won, Depth unlocked; a loss keeps nothing | as 04:16-17 | MATCH; the Depth part UNBUILT-BY-DESIGN |

### 06_New_Mechanics.txt

All paths in `RogueWavesMode/Code/`.

| Line | Says | Code | Mark |
| --- | --- | --- | --- |
| 06:7 | 3 offered, 1 picked before each map | `16_Directions.cs:21`, `:113-165`; `20_Vote.cs:200-268` | MATCH |
| 06:8 | Held ones come back as their next rank | `16_Directions.cs:140-150` | MATCH |
| 06:9 | Rank II Pure Aura 20, III 30 | `16_Directions.cs:22`, `:202` | MATCH |
| 06:10 | The shop sells only the next rank | `16_Directions.cs:180-200` | MATCH |
| 06:11 | Kept on the vessel, back each map, saved | `16_Directions.cs:223-260`; `14_Run.cs:174`, `:186-189` | MATCH |
| 06:12 | Base vessels get their own | `16_Directions.cs:53-62` | MATCH |
| 06:14 | Power scale | a design note for new sheets | MATCH |
| 06:15 | Pairs never offered together | `16_Directions.cs:113-155` (Clash, `:151`) | MATCH (the new pairs: N7) |
| 06:16 | Duos | none built | UNBUILT-QUEUED (B11) |
| 06:17-18 | By; the list | 11 | MATCH |
| 06:19 | Status | `BUILD_2026-11-03.md` | MATCH |
| 06:23-24 | 3 a Tide, 2 a bonus Tide | `14_Run.cs:30`, `:245-250` | MATCH |
| 06:25 | Weather pays extra | as 04:70 | MATCH |
| 06:26 | Objectives pay their own | `19_Objectives.cs:14-21`, `:202-203` | MATCH |
| 06:27 | A pearl counter on each vessel | `14_Run.cs:1-5` (`ModKitCounter`) | MATCH |
| 06:28-29 | Spent at VENDCINT; status | `18_Shop.cs`; `17_Cardinals.cs:184-209` | MATCH |
| 06:34 | Base aura only | `18_Shop.cs:387-430` | MATCH |
| 06:35 | 50 x the coming Tide a break | `18_Shop.cs:390-393` | MATCH |
| 06:36-37 | At VENDCINT; status | `18_Shop.cs:55`, `:272-310` | MATCH |
| 06:41 | Lands at the start of every break, the opening one too | `18_Shop.cs:23-60`; `20_Vote.cs:80-84`, `:122-127` | MATCH |
| 06:42 | Leaves at the end of the break | `18_Shop.cs:12`, `:97` | MATCH |
| 06:43 | Sells Cardinals, Trinkets, next ranks; base game Cardinal Pure Aura 15 | `17_Cardinals.cs:27`, `:142-146`; `18_Shop.cs` | MATCH |
| 06:44-45 | Fund Transfer and objectives there | `18_Shop.cs:55`; `20_Vote.cs:164-169` | MATCH |
| 06:46 | Status, temporary art | `docs/TEMP_ART.md` | MATCH |
| 06:8 note | "about 7 for any vessel and 4 per new vessel" | 7 Any, Yossele 3, Spool 2, Argo 2, Uloe 4 built (11); the rest UNBUILT-QUEUED | UNBUILT-QUEUED (B11) |

### 07_Vessel_Yossele.txt

All paths in `Yossele/Code/`. Every number of the vessel, its buildings and its Cordyfriend is unchanged in code
since the first audit (`git diff 5d9b09e HEAD -- Yossele/Code/02_Data.cs` drops one unused constant), and the rows
below were re-read against it.

| Line(s) | Says | Code | Mark |
| --- | --- | --- | --- |
| 07:8-11 | Title, Difficulty, in-game line | `02_Data.cs:70-88` | MATCH |
| 07:12-14 | Look, towers, accent | temporary art, `docs/TEMP_ART.md` | MATCH |
| 07:15 | His sprite sheet | `! NEEDS !` | UNBUILT-BY-DESIGN |
| 07:17 | Status | round 3: 177/0 | MATCH (N3 fixed here) |
| 07:20-29 | Stats: Health 70, regen 1 while hurt, Speed 1.75, Damage 5.5, Range 4, projectile 15, attack 0.5, build 2.5, sell 50% | `02_Data.cs:83-88` | MATCH |
| 07:30 | Patient Growth | `05_Server.cs`, `07_Building.cs` | MATCH |
| 07:33-40 | FERTILIZE: +1 stage, 1 fertilizer, 6 tiles for the hotkey, 0.5 s, keys 6/7 | `02_Data.cs:89-99`; `06_HeroBrain.cs`; `14_Commands.cs` | MATCH (RESOLVED, row 1) |
| 07:43-49 | Fertilizer 0, cap 10, from Compost Bin, reset each map, kept on death | `02_Data.cs:100-110`; `05_Server.cs` | MATCH |
| 07:52-54 | Growth +1 a Tide, 5 on a map of 4 Tides or fewer | `05_Server.cs`, `07_Building.cs` | MATCH |
| 07:62-79 | Mushroom Cannon stats and Growth | `02_Data.cs:236-241`; `08_Cannon.cs` | MATCH |
| 07:118-131 | Dandelion Tree stats (Range 7, seed limit 10, 1 every 2 s, limit 1, Red 50) and Growth | `02_Data.cs:253-258`, `:171-179` | MATCH |
| 07:172-185 | Stonehenge stats and Growth | `02_Data.cs:271-276`; `10_Henge.cs` | MATCH |
| 07:230-243 | Pachira Sapling stats and Growth | `02_Data.cs:289-294`; `11_Pachira.cs` | MATCH |
| 07:290-302 | Cordyfriend: 75 / 2 / 1.5 / 0.5 / 2.25, up to 30 a player, leash 5, engages within 4 | `02_Data.cs:160-167`; `16_Friend.cs:34`, `:55` | MATCH |
| 07:303 | Look (temporary) | the picked Antlers sheet, `03_Assets.cs:32` | MATCH (N2 fixed here) |
| 07:306-307 | Directions | 11 | counted in 11 |
| upgrades | 33 upgrades | table below | 32 MATCH, 1 UNBUILT-QUEUED (Wood Wide Web) |

### 08_Vessel_Spool.txt

All paths in `Spool/Code/`.

| Line(s) | Says | Code | Mark |
| --- | --- | --- | --- |
| 08:12, 08:14 | Look and accent colour | sage green everywhere since PR #90 | MATCH (N1 fixed here) |
| 08:17 | Status | round 3: 135/0 | MATCH (N3 fixed here) |
| 08:20-29 | Stats: Health 45, Speed 2.75, Damage 2.5, Range 1.5, attack 0.6, build 2.5, sell 50% | `02_Data.cs:75-78` | MATCH |
| 08:33-40 | Patch | `02_Data.cs`, `06_HeroBrain.cs` | MATCH |
| 08:44-50 | Thread: small 1, large 3, elite +2, at most 5 | `02_Data.cs:85`; `05_Server.cs:278-286` | MATCH |
| 08:46 | The pack's modifiers count as elite | `Shared/ModKitUtil.cs:495-503` (`Enemy_` only) | UNBUILT-QUEUED (B6) |
| 08:62-65 | Yarn Stash: Health 100 +50, cap 50 +25, 1, free | `02_Data.cs:210-213` | MATCH |
| 08:67-98 | The Stash's seven upgrades | none | UNBUILT-QUEUED (scope 08 upgrades) |
| 08:159-164 | Cookie Tin: Health 60 +25, 4 alive, 1 every 4 s, 8 tiles, Red 50 | `02_Data.cs:218-221` | MATCH |
| 08:205-220 | Thread Head: Red 40, no limit, carries 20; guards the spot you choose | `02_Data.cs:238-240`, SPOT `:66-67` | MATCH; the stock MOVE it also has goes in B16 |
| 08:261-265 | Pin Cushion: Health 100 +40, reflects 20%, 1 Thread per 25 damage, Blue 20 | `02_Data.cs:252-255` | MATCH |
| 08:309-314 | Loom: Health 40 +20, 2.5 tiles, 1 Thread every 6 s, holds 10, Yellow 40 | `02_Data.cs:274-277` | MATCH |
| 08:311 | Mining "like a Mining Hut" (the build: about 5 Huts) | `02_Data.cs:277` | UNBUILT-QUEUED (S2; N9) |
| 08 UNTHREAD | The UNTHREAD button | none | UNBUILT-QUEUED (scope 08 upgrades) |
| minions | Stitched, Dart, Seam, Mangled, Overstuffed Mangled | `02_Data.cs:128-160` | MATCH |
| upgrades | 37 upgrades | table below | 15 MATCH, 22 UNBUILT-QUEUED |

### 09_Vessel_Argo.txt

All paths in `Argo/Code/`.

| Line(s) | Says | Code | Mark |
| --- | --- | --- | --- |
| 09:9, 09:22 | Note and status | round 3: 144/0 | MATCH (N3 fixed here) |
| 09:24-33 | Stats: Health 65, Speed 2.25, Damage 4, Range 1.75, attack 0.4, build 2, sell 50%, second hit 60% within 1.25 | `02_Data.cs:126-132` | MATCH |
| 09:36 | One slow per source | slows stack with their re-ticks | UNBUILT-QUEUED (B13) |
| 09:37 | Pins; Kelp Bed and Trench pin | Kelp Bed and Trench hold, `11_Water.cs` | UNBUILT-QUEUED (B14, review) |
| 09:43 | Scatter's new in-game line | the earlier one | UNBUILT-QUEUED (B14, review) |
| 09:44-52 | Scatter: Health 40 +25, 1.25 +0.25, 0.6, 4.5, 16, 6 pellets in 50 degrees, pierce 1, Red 30 | `02_Data.cs:254-256` | MATCH |
| 09:50 | +100% to a pinned foe | +50% to either (`Debuffbonus=0.5`) | UNBUILT-QUEUED (B14, review) |
| 09:95-103 | Harpoon: Health 40 +25, 5 +2, 0.35, 6.5, 18, pierce 3, Red 35 | `02_Data.cs:282-283` | MATCH |
| 09:101 | Bosses never moved by a reel or pin | the reel refuses; Pin and Root don't (`01_Util.cs`) | UNBUILT-QUEUED (B13) |
| 09:158-163 | Deep Water: Health 30 +15, slow 30% +5%, Blue 20 | `02_Data.cs:312-313` | MATCH (the stacking: B13) |
| 09:181 | Salt Sting 0.5 without Brine | 1.0 (`11_Water.cs`) | UNBUILT-QUEUED (B12; N8) |
| 09:206-215 | Drill Rig: Health 40 +25, 0.5, 0.25, 5, ore 2, Tremor 2 tiles 20%, Yellow 35 | `02_Data.cs:336-340` | MATCH |
| 09:212 | Each Fathom (review note) | | UNBUILT-QUEUED (B14, review) |
| 09:258-261 | Miner's Base: Health 50 +25, 2 Nauts, limit 1 | `02_Data.cs:362-364` | MATCH |
| 09:261 | Cost Blue 20 + Yellow 15 | Blue 35 (`02_Data.cs:363`) | UNBUILT-QUEUED (B14, review) |
| 09 Naut | Naut stats, tuning +20% attack speed, 0.5 health a second | `02_Data.cs:79-80`; `08_NautBrain.cs:157`, `:215` | MATCH |
| upgrades | 37 upgrades | table below | 26 MATCH, 11 UNBUILT-QUEUED (review) |

### 10_Vessel_Uloe.txt

All paths in `Uloe/Code/`.

| Line(s) | Says | Code | Mark |
| --- | --- | --- | --- |
| 10:8-14 | Identity | `02_Data.cs:49-70` | MATCH |
| 10:20-30 | Stats: Health 30, regen 0, Speed 2.25, Damage 2, Range 3, projectile 15, attack 0.5, build 2.5, sell 50% | `02_Data.cs:64-67` | MATCH |
| 10:33-40 | PLUG IN: 5 tiles, and its Light | `02_Data.cs:87`; `06_HeroBrain.cs` | MATCH |
| 10:44-50 | Light: from Uloe, Photons, Plug In | `05_Server.cs`, `07_Building.cs` | MATCH |
| 10:51 | A teammate's Light, up to 4 a building | by owner only | UNBUILT-QUEUED (B10; N9) |
| 10:52 | Effects; a Mirror's level 1 from any Light | `10_Mirror.cs` | MATCH (RESOLVED, row 1) |
| 10:62-67 | Magnifying Glass: Health 30 +20, 2 +0.5, 0.75, 4.5, Red 30 | `02_Data.cs:181-184` | MATCH |
| 10:118-122 | Power Bank: Health 60 +25, 2 Photons, 8 s, Red 40 + Yellow 20 | `02_Data.cs:193-196` | MATCH |
| 10:168-171 | Mirror: Health 100 +40, Blue 20 | `02_Data.cs:205-208` | MATCH |
| 10:217-222 | Flood Light: Health 40 +20, Light out 2 (+0.2 a point, up to 3 more), aim 6, Yellow 40 | `02_Data.cs:223-226` | MATCH |
| 10:218 | Mining | `02_Data.cs:226` | UNBUILT-QUEUED (S2; N9) |
| 10 Photon | Health 8, Speed 1.75, Damage 0.5, Range 1.5, attack 0.6; leash 3.5 | `02_Data.cs:132-140` | MATCH |
| 10:157 | Supernova a SURGE button once a Tide | not built | UNBUILT-QUEUED (B14) |
| upgrades | 32 upgrades | table below | 8 MATCH, 24 UNBUILT-QUEUED |

### 11_Directions.txt

The mode's own (any vessel and base vessels) are in `RogueWavesMode/Code/16_Directions.cs`; the vessels' in their
part's `02_Data.cs`.

| Line(s) | Direction | Code | Mark |
| --- | --- | --- | --- |
| 11:10-20 | Made of Tougher Stuff 15/25/35%, rank III 25% cap; any vessel with minions | `16_Directions.cs:30-31`, `:69-70`, `:104-109`, `:406`, `:428` | MATCH; Boutbro skipped: UNBUILT-QUEUED (B15) |
| 11:21-31 | Barnacle Coat 2/4/7 | `16_Directions.cs:32-34` | MATCH |
| 11:32-42 | Tidal Mend 5/8/12 within 2/2.5/3 | `16_Directions.cs:35-37` (x the tower's level) | UNBUILT-QUEUED (B7) |
| 11:43-53 | Spring Tide +4/+7/+10, rank III 10% less | `16_Directions.cs:42-44`, `:71` | MATCH (rebuild B7); its Clash with Wound Spring: UNBUILT-QUEUED (B11; N7) |
| 11:54-64 | Cut Costs -10/-20/-30% | `16_Directions.cs:45-46`, `:65`, `:312`, `:335-357` | MATCH (Boutman as built, answer 16) |
| 11:65-75 | Cheap Refits -10/-20/-30% | `16_Directions.cs:47-48`, `:66`, `:314`, `:435-440` | MATCH (RESOLVED, row 3) |
| 11:76-86 | Compound Interest 3/5/8%, at most 40 | `16_Directions.cs:49-50`, `:67-68`, `:456-470` | MATCH |
| 11:87-251 | 15 new for any vessel, 2 of them Duos | none | UNBUILT-QUEUED (B11; N6) |
| 11:254-264 | Deep Current +15/30/50% | `Argo/Code/02_Data.cs:180-185` | MATCH |
| 11:265-275 | Heavy Salvo +10/20/35% | `Argo/Code/02_Data.cs:190-195` | MATCH |
| 11:276-297 | Chum Bucket, Pressure Gauge | none | UNBUILT-QUEUED (B11) |
| 11:300-310 | Compost Bin 10 in 4, 6 in 4, 6 in 7 | `Yossele/Code/02_Data.cs:102-110` | MATCH; rank III's "anywhere" text UNBUILT-QUEUED (B15) |
| 11:311-321 | Dandelion Season 70%, rank II +25%, rank III +5% a Sapling up to 50% | `Yossele/Code/02_Data.cs:118-130` | MATCH; the rename UNBUILT-QUEUED (B8, A1) |
| 11:322-332 | Mother of Mushrooms (numbers in `Mushboomkits`) | `Yossele/Code/02_Data.cs:133-145` | MATCH; the rename UNBUILT-QUEUED (B8, A1) |
| 11:333-387 | Cutting Jar, Sap Bucket, Creeping Ivy, Deep Breath, Fairy Ring | none | UNBUILT-QUEUED (B11) |
| 11:390-400 | Efficient Recycler 65/80/100%, rank III heals 10 within 2.5 | `Spool/Code/02_Data.cs:99-110` | MATCH |
| 11:401-411 | Sewing Machine +50% mining, heals 10/20/20 within 3/3/5 | `Spool/Code/02_Data.cs:112-122` | MATCH; the rename UNBUILT-QUEUED (B8, A1) |
| 11:412-466 | Double Needle, Button Jar, Pattern Book, Merit Badge, Odd Socks | none | UNBUILT-QUEUED (B11) |
| 11:469-479 | Extension Cord +2/+4/+6 | `Uloe/Code/02_Data.cs:101` | MATCH |
| 11:480-490 | Brighter Bulb +1/+2/+3 | `Uloe/Code/02_Data.cs:102` | MATCH |
| 11:491-501 | Spare Batteries +1; +1 and -25%; +2 and -25% | `Uloe/Code/02_Data.cs:103-104` | MATCH |
| 11:502-512 | Flash Bulb Illuminate 1; +Blind 1; both 2 | `Uloe/Code/02_Data.cs:105-106` | MATCH |
| 11:515-525 | Full Gallop +10/20/30% | `16_Directions.cs:53-54` | MATCH (RESOLVED, row 5) |
| 11:526-536 | House Regulars +10/20/30% | `16_Directions.cs:55-56` | MATCH |
| 11:537-547 | Long Line +1/+2/+3 | `16_Directions.cs:57-58` | MATCH |
| 11:548-558 | Packed Snow +8/16/25% | `16_Directions.cs:59-60` | MATCH |
| 11:559-569 | Skipping Stones +15/30/50% | `16_Directions.cs:61-62` | MATCH |
| 11:570-624 | Mason's Trowel, Snow Shovel, Worm Tin, Open Sign, Long Leash | none | UNBUILT-QUEUED (B11) |
| 11 Looks | Every built Direction's card | `RogueWavesMode/Code/16_Directions.cs:204-217` and each part's icons; the three Shift looks: A1 | MATCH; Shift looks UNBUILT-QUEUED (A1) |

Counted per Direction and per rank group: 23 built Directions read (18 MATCH outright, 5 MATCH with a queued part),
32 new ones queued.

### 12_Cardinals.txt

All paths in `RogueWavesMode/Code/17_Cardinals.cs` unless named.

| Line(s) | Says | Code | Mark |
| --- | --- | --- | --- |
| 12:5-6 | Which new ones; which stock ones the shop sells | `! NEEDS !` | UNBUILT-BY-DESIGN |
| 12:7 | The shop's stock list leaves out 14 | `:39-43` (Bottom Rock is both `Artifact_Bottomrock` and `Artifact_Rock`) | MATCH |
| 12:10-12 | A run modifier; two plain words; bought with Pure Aura | `:184-209` | MATCH |
| 12:13 | Kept apart in the shop and the list | one mixed shelf, `:167-180` | UNBUILT-QUEUED (B4) |
| 12:14 | A Trinket acts on one player's things | `:400-440` | MATCH |
| 12:14 | ...and shows on their vessel's panel | `:406` skips it | UNBUILT-QUEUED (B4) |
| 12:15 | A Cardinal on the core, in the row, for everyone | `:232-268` | MATCH |
| 12:16 | Past 8, a +N count | `:263` | UNBUILT-QUEUED (B4) |
| 12:17 | 6 copies, then off the shelf; a stock one once, then off | 6 copies `:25`, `:93`; once `:200`; the shelf still offers both | MATCH for the caps; UNBUILT-QUEUED (B3) for the shelf |
| 12:18 | Saved per player | `14_Run.cs:174`, `:186-189` | MATCH |
| 12:19 | No swaps on the boat | as 04:61-62 | MATCH; lobby UNBUILT-QUEUED (B2) |
| 12:20-24 | Power, pairs, no copies, By, kept names | rules for new sheets | MATCH; pairs UNBUILT-QUEUED (B11; N7) |
| 12:28-39 | Vampire Dentures: 20% a stack, stacks, Red, Pure Aura 15 | `:26`, `:35`, `:455-487` | MATCH |
| 12:37 | Its look | `RWAssets` `Cardinal_VampireDentures` (temporary) | MATCH |
| 12:40-171 | 11 Trinkets by Claude | none | UNBUILT-QUEUED (B11) |
| 12:172-221 | 4 Cardinals by Claude | none | UNBUILT-QUEUED (B11) |

### 13_Enemy_Modifiers.txt

All paths in `RogueWavesFoes/Code/00_RogueWavesFoesMod.cs`.

| Line(s) | Says | Code | Mark |
| --- | --- | --- | --- |
| 13:3 | An effect, not a skin | `:368-476` (`RWFLooks`) | MATCH |
| 13:8-9 | Cursed: aura; in-game line | `:137` | MATCH |
| 13:10-11 | Vessels, towers and units: attack -25% within 3 tiles | `:81`, `:299-310`, `:324-336` | MATCH (RESOLVED; N5) |
| 13:12 | Indigo ring | `:379` | MATCH |
| 13:18-22 | Berserk: line; +100% damage, +50% speed at no health; hot pink | `:82`, `:138`, `:284-290`, `:380` | MATCH |
| 13:28-32 | Aging: line; -30% speed, up to +100% over 50 s, health scaled; barnacle blue | `:83`, `:139`, `:268-273`, `:292-297`, `:381` | MATCH |
| 13:38-40 | Dazzling: aura; line; Blind and Illuminate | `:140`, `:338-362` | MATCH |
| 13:41 | Level 1 (15%), lasting 3 s after leaving | 20%, Illuminate gone at the next check | UNBUILT-QUEUED (B5) |
| 13:42 | Bright yellow ring | `:382` | MATCH |
| 13:13-15, 23-25, 33-35, 43-45 | Later maps, Look, Clashes empty | nothing to match | MATCH |

### 14_Optional_Objectives.txt

All paths in `RogueWavesMode/Code/19_Objectives.cs`.

| Line(s) | Says | Code | Mark |
| --- | --- | --- | --- |
| 14:3 | The list and pay | `! NEEDS !` (the five are Claude's, taken by answer 13) | UNBUILT-BY-DESIGN |
| 14:6-9 | A short order; a new one every break at VENDCINT; full pay for every player | `:91-103`; `20_Vote.cs:164-169`; `14_Run.cs` (`Pay`) | MATCH |
| 14:12-23 | Mind the Meteor: line, team, next Tide, nothing on a failure, farther on later maps, Pure Aura 3 | `:16`, `:234-272` | MATCH |
| 14:15 | 12 hits + 3 a map | 13 + 3 a map (`:268`) | UNBUILT-QUEUED (B15) |
| 14:15 | about 8 tiles + 1.5 a map | `:241` | MATCH |
| 14:22 | A white crystal | a stock ore node's look | UNBUILT-QUEUED (A3) |
| 14:24-35 | Keep the Basin Clean: no damage at all, every Basin, Pure Aura 2 | `:17`, `:139-140`, `:222-232` | MATCH |
| 14:36-47 | Beat the Clock: 25% of the clock left, Pure Aura 3 | `:18`, `:33`, `:159-168`, `:190` | MATCH |
| 14:48-59 | Mind the Vessels: no vessel falls, Pure Aura 2 | `:19`, `:208-218` | MATCH (RESOLVED, row 3) |
| 14:60-71 | Raze the Nest: 3-7 tiles, never on Nesting Ground, Pure Aura 4 | `:20`, `:99`, `:274-277`; `07_Weather.cs:370` | MATCH |
| 14:34, 46, 58, 70 | Looks | one board icon | UNBUILT-QUEUED (A3) |
| 14:13, 25, 37, 49, 61 | Kinds (Mining, Guard, Rush, Guard, Hunt) | not shown in game; a scope label | MATCH |

### 15_Templates.txt

Templates hold no values the code builds from. 15:245 now matches 04:71 (N4 fixed here).

### 16_Base_Vessels.txt

| Line(s) | Says | Code | Mark |
| --- | --- | --- | --- |
| 16:16 | Argo's row | `Argo/Code/02_Data.cs:126-129` | MATCH (tildes off, N4) |
| 16:17-19 | Yossele's, Spool's (tildes off, N4) and Uloe's rows | `Yossele/Code/02_Data.cs:83-86`, `Spool/Code/02_Data.cs:75-78`, `Uloe/Code/02_Data.cs:64-67` | MATCH |

The five base vessels' sheets are the game's data, unchanged; first audit §5 still lists the 44 lines only the
decompiled game can settle.

### Every pack upgrade, by script

`TDS_Mod/Tools/cloud-check/scope_upgrades.py` (added by this audit, rerun it any time) read each `% N:{Name}` block and matched it to the `U(...)` entry with the
same name in the part's `02_Data.cs`, checking the level (scope level N is the code's tier N+1), the cost, the in-game
line and the Leads to line. Numbers lines were then read against each entry's parameters by hand; every one not
named below agrees (Overhaul's 2.5 is the Naut's 0.5 plus `Repair_Base` 2, Full Crew's +40% is 0.2 plus
`Tuneboost_Base` 0.2, Sharper Pins' 40% is 0.2 plus `Reflect_Add` 0.2, Ironclad's x2.5 is `Health_Multi` 1.5).


#### Yossele: every upgrade

| Scope line | Building | Upgrade | Level | Code | Status |
| --- | --- | --- | ---: | --- | --- |
| 07:81 | Mushroom Cannon | Sticky Spores | 1 | `Yossele/Code/02_Data.cs:243` | MATCH |
| 07:85 | Mushroom Cannon | Farout Fungus | 1 | `Yossele/Code/02_Data.cs:244` | MATCH |
| 07:90 | Mushroom Cannon | Viral Outbreak | 2 | `Yossele/Code/02_Data.cs:245` | MATCH |
| 07:94 | Mushroom Cannon | Deathcap Training | 2 | `Yossele/Code/02_Data.cs:246` | MATCH |
| 07:99 | Mushroom Cannon | Explosive Finish | 3 | `Yossele/Code/02_Data.cs:247` | MATCH |
| 07:103 | Mushroom Cannon | Hallucinogen | 3 | `Yossele/Code/02_Data.cs:248` | MATCH |
| 07:108 | Mushroom Cannon | Cordyceps | 4 | `Yossele/Code/02_Data.cs:249` | MATCH |
| 07:132 | Primordial Dandelion Tree | Seed Packets | 1 | `Yossele/Code/02_Data.cs:260` | MATCH |
| 07:137 | Primordial Dandelion Tree | Demand and Supply | 1 | `Yossele/Code/02_Data.cs:261` | MATCH |
| 07:143 | Primordial Dandelion Tree | Picky Placement | 2 | `Yossele/Code/02_Data.cs:262` | MATCH |
| 07:147 | Primordial Dandelion Tree | Garden Weeds | 2 | `Yossele/Code/02_Data.cs:263` | MATCH |
| 07:151 | Primordial Dandelion Tree | D-MDS | 2 | `Yossele/Code/02_Data.cs:264` | MATCH |
| 07:156 | Primordial Dandelion Tree | Bonsai | 3 | `Yossele/Code/02_Data.cs:265` | MATCH |
| 07:160 | Primordial Dandelion Tree | Rubbery Shell | 3 | `Yossele/Code/02_Data.cs:266` | MATCH |
| 07:165 | Primordial Dandelion Tree | Dandelion Storm | 4 | `Yossele/Code/02_Data.cs:267` | MATCH |
| 07:190 | Overgrown Stonehenge | Verdant Spread | 1 | `Yossele/Code/02_Data.cs:278` | MATCH |
| 07:194 | Overgrown Stonehenge | Vine Blockade | 1 | `Yossele/Code/02_Data.cs:279` | MATCH |
| 07:199 | Overgrown Stonehenge | Natural Remedy | 2 | `Yossele/Code/02_Data.cs:280` | MATCH |
| 07:203 | Overgrown Stonehenge | Opportunistic Carnivore | 2 | `Yossele/Code/02_Data.cs:281` | MATCH |
| 07:207 | Overgrown Stonehenge | Easy Target | 2 | `Yossele/Code/02_Data.cs:282` | MATCH |
| 07:212 | Overgrown Stonehenge | Entangling Roots | 3 | `Yossele/Code/02_Data.cs:283` | MATCH |
| 07:216 | Overgrown Stonehenge | Obsidian | 3 | `Yossele/Code/02_Data.cs:284` | MATCH |
| 07:221 | Overgrown Stonehenge | Monument | 4 | `Yossele/Code/02_Data.cs:285` | MATCH |
| 07:245 | Pachira Sapling | Focused Roots | 1 | `Yossele/Code/02_Data.cs:296` | MATCH |
| 07:249 | Pachira Sapling | Sustaining Practices | 1 | `Yossele/Code/02_Data.cs:297` | MATCH |
| 07:254 | Pachira Sapling | Grafted | 2 | `Yossele/Code/02_Data.cs:298` | MATCH |
| 07:258 | Pachira Sapling | Forest Project | 2 | `Yossele/Code/02_Data.cs:299` | MATCH |
| 07:262 | Pachira Sapling | Spontaneous Mulch | 2 | `Yossele/Code/02_Data.cs:300` | MATCH |
| 07:267 | Pachira Sapling | Connected Roots | 3 | `Yossele/Code/02_Data.cs:301` | MATCH |
| 07:271 | Pachira Sapling | Harvesting Season | 3 | `Yossele/Code/02_Data.cs:302` | MATCH |
| 07:275 | Pachira Sapling | Wood Wide Web | 3 | `none yet` | UNBUILT-QUEUED |
| 07:280 | Pachira Sapling | Fortifying Roots | 4 | `Yossele/Code/02_Data.cs:303` | MATCH |
| 07:284 | Pachira Sapling | Softening Roots | 4 | `Yossele/Code/02_Data.cs:304` | MATCH |

#### Spool: every upgrade

| Scope line | Building | Upgrade | Level | Code | Status |
| --- | --- | --- | ---: | --- | --- |
| 08:67 | Yarn Stash | Roomy Pockets | 1 | `none yet` | UNBUILT-QUEUED |
| 08:71 | Yarn Stash | Quick Patch | 1 | `none yet` | UNBUILT-QUEUED |
| 08:76 | Yarn Stash | Darning | 2 | `none yet` | UNBUILT-QUEUED |
| 08:80 | Yarn Stash | Elbow Patches | 2 | `none yet` | UNBUILT-QUEUED |
| 08:85 | Yarn Stash | Remnant Bin | 3 | `none yet` | UNBUILT-QUEUED |
| 08:89 | Yarn Stash | Mending Circle | 3 | `none yet` | UNBUILT-QUEUED |
| 08:94 | Yarn Stash | Quilting Bee | 4 | `none yet` | UNBUILT-QUEUED |
| 08:166 | Repurposed Cookie Tin | Reuse Reduce Recycle | 1 | `Spool/Code/02_Data.cs:224` | MATCH |
| 08:170 | Repurposed Cookie Tin | Woolen Lining | 1 | `Spool/Code/02_Data.cs:226` | MATCH |
| 08:175 | Repurposed Cookie Tin | Discounted Material | 2 | `Spool/Code/02_Data.cs:228` | MATCH |
| 08:179 | Repurposed Cookie Tin | New Design | 2 | `Spool/Code/02_Data.cs:230` | MATCH |
| 08:183 | Repurposed Cookie Tin | Tacking Stitch | 2 | `none yet` | UNBUILT-QUEUED |
| 08:188 | Repurposed Cookie Tin | One Last Surprise | 3 | `Spool/Code/02_Data.cs:232` | MATCH |
| 08:192 | Repurposed Cookie Tin | Cross Stitch | 3 | `none yet` | UNBUILT-QUEUED |
| 08:197 | Repurposed Cookie Tin | Patchwork Army | 4 | `none yet` | UNBUILT-QUEUED |
| 08:223 | Thread Head | Threaded Delivery | 1 | `Spool/Code/02_Data.cs:244` | MATCH |
| 08:227 | Thread Head | Patchwork Job | 1 | `Spool/Code/02_Data.cs:246` | MATCH |
| 08:232 | Thread Head | Big Bobbin | 2 | `none yet` | UNBUILT-QUEUED |
| 08:236 | Thread Head | Needle Point | 2 | `none yet` | UNBUILT-QUEUED |
| 08:241 | Thread Head | High Thread Count | 3 | `none yet` | UNBUILT-QUEUED |
| 08:245 | Thread Head | Stitch in Time | 3 | `none yet` | UNBUILT-QUEUED |
| 08:250 | Thread Head | Head of the Class | 4 | `none yet` | UNBUILT-QUEUED |
| 08:267 | Pin Cushion | Sharper Pins | 1 | `Spool/Code/02_Data.cs:258` | MATCH |
| 08:271 | Pin Cushion | Slow Unravel | 1 | `Spool/Code/02_Data.cs:260` | MATCH |
| 08:276 | Pin Cushion | Pin Barrage | 2 | `Spool/Code/02_Data.cs:262` | MATCH (leads to an unbuilt upgrade, which comes with it) |
| 08:281 | Pin Cushion | Loose Stuffing | 2 | `Spool/Code/02_Data.cs:264` | MATCH |
| 08:286 | Pin Cushion | Overstuffed | 3 | `Spool/Code/02_Data.cs:266` | MATCH |
| 08:290 | Pin Cushion | Burst Seams | 3 | `Spool/Code/02_Data.cs:268` | MATCH |
| 08:294 | Pin Cushion | Iron Thimble | 3 | `none yet` | UNBUILT-QUEUED |
| 08:299 | Pin Cushion | Voodoo Doll | 4 | `none yet` | UNBUILT-QUEUED |
| 08:316 | Loom | Direct Line | 1 | `Spool/Code/02_Data.cs:280` | MATCH |
| 08:320 | Loom | Seam Allowance | 1 | `Spool/Code/02_Data.cs:282` | MATCH |
| 08:325 | Loom | Dyed in the Wool | 2 | `none yet` | UNBUILT-QUEUED |
| 08:329 | Loom | Bobbin Winder | 2 | `none yet` | UNBUILT-QUEUED |
| 08:334 | Loom | Seam Squad | 3 | `none yet` | UNBUILT-QUEUED |
| 08:338 | Loom | Snare Line | 3 | `none yet` | UNBUILT-QUEUED |
| 08:343 | Loom | Golden Thread | 4 | `none yet` | UNBUILT-QUEUED |

#### Argo: every upgrade

| Scope line | Building | Upgrade | Level | Code | Status |
| --- | --- | --- | ---: | --- | --- |
| 09:54 | Scatter Cannon | Wide Bore | 1 | `Argo/Code/02_Data.cs:260` | UNBUILT-QUEUED: review 2026-11-04 change (numbers), B14 |
| 09:58 | Scatter Cannon | Double Barrel | 1 | `Argo/Code/02_Data.cs:262` | UNBUILT-QUEUED: review 2026-11-04 change (in-game line), B14 |
| 09:63 | Scatter Cannon | Grapeshot | 2 | `Argo/Code/02_Data.cs:264` | MATCH |
| 09:67 | Scatter Cannon | Barnacle Shot | 2 | `Argo/Code/02_Data.cs:266` | MATCH |
| 09:73 | Scatter Cannon | Pressure Rounds | 3 | `Argo/Code/02_Data.cs:268` | MATCH |
| 09:77 | Scatter Cannon | Ironclad | 3 | `Argo/Code/02_Data.cs:273` | MATCH |
| 09:83 | Scatter Cannon | Broadside | 4 | `Argo/Code/02_Data.cs:275` | MATCH |
| 09:105 | Harpoon Gun | Long Line | 1 | `Argo/Code/02_Data.cs:287` | MATCH |
| 09:110 | Harpoon Gun | Tether | 1 | `Argo/Code/02_Data.cs:289` | MATCH |
| 09:116 | Harpoon Gun | Skewer | 2 | `Argo/Code/02_Data.cs:291` | MATCH |
| 09:121 | Harpoon Gun | Anchor Line | 2 | `Argo/Code/02_Data.cs:293` | MATCH |
| 09:126 | Harpoon Gun | Reel | 2 | `Argo/Code/02_Data.cs:295` | UNBUILT-QUEUED: review 2026-11-04 change (numbers), B14 |
| 09:132 | Harpoon Gun | Whaler | 3 | `Argo/Code/02_Data.cs:297` | MATCH |
| 09:137 | Harpoon Gun | Twin Line | 3 | `Argo/Code/02_Data.cs:299` | MATCH |
| 09:143 | Harpoon Gun | Leviathan Lance | 4 | `Argo/Code/02_Data.cs:301` | MATCH |
| 09:147 | Harpoon Gun | Undertow | 4 | `Argo/Code/02_Data.cs:303` | MATCH |
| 09:165 | Deep Water | Undercurrent | 1 | `Argo/Code/02_Data.cs:317` | MATCH |
| 09:169 | Deep Water | Brine | 1 | `Argo/Code/02_Data.cs:319` | MATCH |
| 09:175 | Deep Water | Kelp Bed | 2 | `Argo/Code/02_Data.cs:321` | UNBUILT-QUEUED: review 2026-11-04 change (in-game line), B14 |
| 09:179 | Deep Water | Salt Sting | 2 | `Argo/Code/02_Data.cs:323` | MATCH |
| 09:185 | Deep Water | Abyss | 3 | `Argo/Code/02_Data.cs:325` | UNBUILT-QUEUED: review 2026-11-04 change (in-game line), B14 |
| 09:190 | Deep Water | Crushing Depth | 3 | `Argo/Code/02_Data.cs:327` | MATCH |
| 09:195 | Deep Water | Trench | 4 | `Argo/Code/02_Data.cs:329` | UNBUILT-QUEUED: review 2026-11-04 change (in-game line), B14 |
| 09:217 | Drill Rig | Pay Dirt | 1 | `Argo/Code/02_Data.cs:344` | MATCH |
| 09:221 | Drill Rig | Seismic | 1 | `Argo/Code/02_Data.cs:346` | UNBUILT-QUEUED: review 2026-11-04 change (cost), B14 |
| 09:227 | Drill Rig | Deep Core | 2 | `Argo/Code/02_Data.cs:348` | MATCH |
| 09:231 | Drill Rig | Bedrock | 2 | `Argo/Code/02_Data.cs:350` | UNBUILT-QUEUED: review 2026-11-04 change (cost), B14 |
| 09:237 | Drill Rig | Gusher | 3 | `Argo/Code/02_Data.cs:352` | UNBUILT-QUEUED: review 2026-11-04 change (in-game line), B14 |
| 09:241 | Drill Rig | Fault Line | 3 | `Argo/Code/02_Data.cs:354` | UNBUILT-QUEUED: review 2026-11-04 change (cost), B14 |
| 09:247 | Drill Rig | Mantle Tap | 4 | `Argo/Code/02_Data.cs:356` | UNBUILT-QUEUED: review 2026-11-04 change (in-game line), B14 |
| 09:263 | Miner's Base | Flippers | 1 | `Argo/Code/02_Data.cs:370` | MATCH |
| 09:267 | Miner's Base | Old Salts | 1 | `Argo/Code/02_Data.cs:372` | MATCH |
| 09:273 | Miner's Base | Overhaul | 2 | `Argo/Code/02_Data.cs:374` | MATCH |
| 09:277 | Miner's Base | Calibrate | 2 | `Argo/Code/02_Data.cs:376` | MATCH |
| 09:283 | Miner's Base | Third Mate | 3 | `Argo/Code/02_Data.cs:378` | MATCH |
| 09:287 | Miner's Base | Bulkhead | 3 | `Argo/Code/02_Data.cs:380` | MATCH |
| 09:293 | Miner's Base | Full Crew | 4 | `Argo/Code/02_Data.cs:382` | MATCH |

#### Uloe: every upgrade

| Scope line | Building | Upgrade | Level | Code | Status |
| --- | --- | --- | ---: | --- | --- |
| 10:69 | Magnifying Glass | Burning Point | 1 | `Uloe/Code/02_Data.cs:187` | MATCH (leads to an unbuilt upgrade, which comes with it) |
| 10:74 | Magnifying Glass | Wide Lens | 1 | `none yet` | UNBUILT-QUEUED |
| 10:80 | Magnifying Glass | Polished Lens | 2 | `none yet` | UNBUILT-QUEUED |
| 10:85 | Magnifying Glass | Quick Focus | 2 | `none yet` | UNBUILT-QUEUED |
| 10:90 | Magnifying Glass | Prism | 2 | `none yet` | UNBUILT-QUEUED |
| 10:96 | Magnifying Glass | Sunspot | 3 | `none yet` | UNBUILT-QUEUED |
| 10:101 | Magnifying Glass | Focal Point | 3 | `none yet` | UNBUILT-QUEUED |
| 10:107 | Magnifying Glass | Death Ray | 4 | `none yet` | UNBUILT-QUEUED |
| 10:124 | Power Bank | Short Circuit | 1 | `Uloe/Code/02_Data.cs:199` | MATCH (leads to an unbuilt upgrade, which comes with it) |
| 10:129 | Power Bank | Extra Cell | 1 | `none yet` | UNBUILT-QUEUED |
| 10:134 | Power Bank | Static Charge | 2 | `none yet` | UNBUILT-QUEUED |
| 10:138 | Power Bank | Bright Spark | 2 | `none yet` | UNBUILT-QUEUED |
| 10:142 | Power Bank | Surge Protector | 2 | `none yet` | UNBUILT-QUEUED |
| 10:148 | Power Bank | Chain Lightning | 3 | `none yet` | UNBUILT-QUEUED |
| 10:152 | Power Bank | Photon Swarm | 3 | `none yet` | UNBUILT-QUEUED |
| 10:157 | Power Bank | Supernova | 4 | `none yet` | UNBUILT-QUEUED |
| 10:173 | Mirror | Silver Backing | 1 | `Uloe/Code/02_Data.cs:211` | MATCH |
| 10:177 | Mirror | Vanity Lights | 1 | `Uloe/Code/02_Data.cs:213` | MATCH |
| 10:182 | Mirror | Glare | 2 | `Uloe/Code/02_Data.cs:215` | MATCH |
| 10:186 | Mirror | Bounce Card | 2 | `Uloe/Code/02_Data.cs:217` | MATCH |
| 10:190 | Mirror | Tempered Glass | 2 | `none yet` | UNBUILT-QUEUED |
| 10:195 | Mirror | Hall of Mirrors | 3 | `none yet` | UNBUILT-QUEUED |
| 10:200 | Mirror | Reflection | 3 | `none yet` | UNBUILT-QUEUED |
| 10:206 | Mirror | Heliostat | 4 | `none yet` | UNBUILT-QUEUED |
| 10:224 | Flood Light | Wide Flood | 1 | `Uloe/Code/02_Data.cs:229` | MATCH |
| 10:228 | Flood Light | Long Throw | 1 | `Uloe/Code/02_Data.cs:231` | MATCH |
| 10:233 | Flood Light | Halogen Bulb | 2 | `none yet` | UNBUILT-QUEUED |
| 10:237 | Flood Light | High Voltage | 2 | `none yet` | UNBUILT-QUEUED |
| 10:242 | Flood Light | Colour Filter | 3 | `none yet` | UNBUILT-QUEUED |
| 10:247 | Flood Light | Searchlight | 3 | `none yet` | UNBUILT-QUEUED |
| 10:252 | Flood Light | Stadium Lights | 4 | `none yet` | UNBUILT-QUEUED |
| 10:256 | Flood Light | Lighthouse | 4 | `none yet` | UNBUILT-QUEUED |
