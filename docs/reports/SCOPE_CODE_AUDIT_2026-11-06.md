# Scope against code, third pass (2026-11-06)

Dimitri, in this thread (2026-11-06 00:44Z): `/scope-audit`; on the card (00:48Z) he picked "All 17 files, changes
only". This pass re-reads every row of the second audit (`SCOPE_CODE_AUDIT_2026-11-05.md`, base `99b9f3d`) against what
changed since, in both the scope and the code, and adds a row for every scope line that is new.

**Base.** `claude/phase-1-6lfnar` at `c197626` (PR #132 merged). About 35 PRs landed since the second audit: the
decisions' code (#103), Argo's review (#104), Uloe's 24 upgrades (#105), Wood Wide Web and the Shift renames (#106),
the 47 Claude-made Directions, Trinkets and Cardinals (#107-#111), their icons and models (#112-#113), the decided code
fixes (#120), the balance sweep (#123), the weather look (#127) and the stock Trinkets split (#130, #132). Code paths
are relative to `TDS_Mod/ModSource`; scope lines are `NN:line` as the scope stands after this PR (its four edits change
no line count).

**Marks** as in the second audit: MATCH, RESOLVED (a row that was queued or differed on 11-05 and now matches, with the
PR), UNBUILT-QUEUED (named item), UNBUILT-BY-DESIGN, DIFFERENT (built, does something else), MISSING (asked, not
built, not queued). A `~value~` is Claude's default and is audited like any value. The balance sweep's tildes in `04`,
`08`, `10` and `12` stay on purpose until after PC round 4 and are not findings.

**Docs only, read from the code: nothing compiled, nothing run on the PC.** The code the rows cite has itself only
compiled since PR #95; PC round 4 is the first to run it.

## 1. Summary

| File | Rows | MATCH | RESOLVED | UNBUILT-QUEUED | UNBUILT-BY-DESIGN | DIFFERENT / MISSING | Since 11-05 |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | --- |
| `00_Read_Me` | 0 | | | | | 0 | rules only |
| `01_Expansion` | 13 | 12 | | | 1 | 0 | 01:24 now MATCH (`Pack/pack.txt` is the Yossele-only 0.1.0 pack) |
| `02_Progress` | 11 | 10 | | | | 1 (N4, fixed here) | |
| `03_Bug_Fix` | 6 | 6 | | | | 0 | |
| `04_Rogue_Waves_Mode` | 51 | 46 | 2 | | 2 | 1 (N3, fixed here) | B1 map vote, B2 locked lobby Cardinals |
| `05_Gameplay_Rundown` | 10 | 7 | 1 | | 1 | 1 (N3, fixed here) | every map voted (B1) |
| `06_New_Mechanics` | 28 | 26 | 2 | | | 0 | Duos and the Direction counts built |
| `07_Vessel_Yossele` | 65 | 62 | 1 | | 2 | 0 | Wood Wide Web (#106) |
| `08_Vessel_Spool` | 74 | 45 | 3 | 24 | 2 | 0 | B6, B16, the Loom's mining; 22 upgrades + UNTHREAD (B19) and Pin Barrage's Leads to (B18) still queued |
| `09_Vessel_Argo` | 74 | 54 | 18 | | 1 | 1 (N2) | B12, B13 and the fourteen review changes |
| `10_Vessel_Uloe` | 52 | 23 | 27 | | 2 | 0 | the 24 upgrades, B10, B14; old N10's Leads to set |
| `11_Directions` | 55 | 16 | 38 | | | 1 (N1) | the 32 Claude-made Directions, B7, B8, B15, the Spring pair |
| `12_Cardinals` | 34 | 13 | 21 | | | 0 | B2-B4, the 15 Claude-made sheets, the stock split |
| `13_Enemy_Modifiers` | 13 | 12 | 1 | | | 0 | B5 Dazzling |
| `14_Optional_Objectives` | 13 | 10 | 3 | | | 0 | 12 + 3 hits (B15), the crystal (#119), the icons (#113) |
| `15_Templates` | 9 | 9 | | | | 0 (N5, fixed here) | |
| `16_Base_Vessels` | 4 | 4 | | | | 0 | the four pack rows equal each `02_Data.cs` |
| **Total** | **512** | **355** | **117** | **24** | **11** | **5** | |

So: of the second audit's 93 queued rows, every one outside Spool is now built, and every built one matches its line
except two numbers (N1, N2). What is still queued is only Spool's B18/B19. This pass found **two behaviour
differences for Dimitri's word** and **three scope texts fixed here**. Nothing is MISSING. N3 and N4 sit on slot
rows and count as DIFFERENT there; N5 is a template's guidance text, not a slot, so it adds no row.

## 2. New items

N1 and N2 are his call (a number in play). N3-N5 are scope texts fixed in this PR.

1. **N1. Tidal Mend heals x the tower's level** (11:38; `RogueWavesMode/Code/16_Directions.cs:116`, `:611`). The line
   says "mends 5 / 8 / 12 within 2 / 2.5 / 3 tiles"; the code heals `MendHeal[rank] x the tower's Level`, so a level-3
   tower mends 15 / 24 / 36. B7 (his answer 14) rebuilt it on the mode's own effect and fixed the frozen level, but kept
   the factor, and `DECISIONS_APPLIED_2026-11-05.md` B7 says "Scope 11's numbers stand". The first audit raised the same
   row (11-04 row 1). `~drop the level factor at :611, so the scope's flat 5 / 8 / 12 holds~` (the build thread, one
   line), or `~write "x the tower's level" into 11:38~`.
2. **N2. Abyss's "slow +10%" works out to +5%** (09:187; `Argo/Code/11_Water.cs:170`, `Argo/Code/02_Data.cs:154`).
   Deep Water's slow is capped by `SlowCap = 0.6`. Abyss can only be bought after Undercurrent and Kelp Bed, so its pump
   is at level 4: 0.30 + 3 x 0.05 + 0.10 + 0.10 = 0.65, held to 0.60. The self-test checks the stat, not the slow
   applied (`Argo/Code/99_SelfTest.cs:1505-1510`). `~give Deep Water a ceiling of 0.65 (the Tremor keeps 0.6), and test
   the slow applied~`, or `~write the 60% ceiling into 09:36 and make Abyss "slow +5%"~`.
3. **N3. Depth's note claimed an end-screen line** (04:16, 05:13). The note said "nothing unlocks and the end screen
   says so"; nothing unlocks, but no player sees Depth named: the title is "CAMPAIGN WON"
   (`RogueWavesMode/Code/06_Campaign.cs:248`), the chat line "All 10 maps sailed: the campaign is won." (`:313`), and
   only the log says Depth is on hold (`:314`). **Fixed here:** both notes now end at "nothing unlocks".
4. **N4. 02:9 said Yossele's 0.2.0-dev "is the play build installed for your test"**; since 2026-11-03 every part's
   release build is installed and the play build is parked (02:16, `Yossele/IMPLEMENTATION_NOTES.md:61-62`). **Fixed
   here:** "its release build is installed with every other pack part's, for your test".
5. **N5. The template's Stacks slot said "with no limit"** (15:176), against his answer 3 (caps kept:
   `DECISIONS_APPLIED_2026-11-05.md` §1 row 3; 12:18; `RogueWavesMode/Code/17_Cardinals.cs:32`). **Fixed here:**
   "up to 6 copies" (his number, so no tildes).

Small notes, not counted as rows:

- `TDS_Mod/Tools/cloud-check/scope_upgrades.py` reports Uloe's Searchlight and Lighthouse MISSING because their `U(...)`
  entries put the cost on the next line (`Uloe/Code/02_Data.cs:298-299`, `:305-306`); both are built. `~let the script
  read a U(...) across lines~` whoever next touches it.
- Two code comments still cite scope 11's NEEDS lines, which the scope dropped (`RogueWavesMode/Code/16_Directions.cs:28`,
  `Argo/Code/02_Data.cs:175`): `~update them with the next code PR~`.
- Two of Claude's notes in 08 are stale (08:13 "a default now", 08:266 "Every name with ~ is a placeholder"): with B19's
  scope pass.
- When PC round 4's docs PR clears 07:15's sprite NEEDS line, 07:17's "with temporary art" goes with it (the final
  sheets are in `Yossele/Art/`).

## 3. What the next threads take from this page

- **Dimitri:** N1 and N2, one word each (a card in the thread). Answered 01:03Z: "Code follows the scope" for both, built the same day; the PC checks are in `STOCK_TRINKETS_2026-11-05.md` §8.
- **Build thread:** whichever way N1 and N2 go (one line each), then B18/B19 as queued.
- **PC round 4:** nothing new; every RESOLVED row here is built-not-run and already on its lists.
- **Scope sync:** this PR edits 02, 04, 05 and 15, so his project-files copy and the Scope Editor are behind in those
  four (and in 12, from PRs #130/#132) until he says "sync".

## 4. File by file

### 00_Read_Me.txt

The scope's own rules; nothing for the code to match.

### 01_Expansion.txt
Summary: rows 13; MATCH 12; RESOLVED 0; UNBUILT-QUEUED 0; UNBUILT-BY-DESIGN 1; DIFFERENT 0; MISSING 0.
The file is unchanged since 99b9f3d. Every cited path still holds. 01:24 now reads as the pack definition (`Pack/pack.txt`, Yossele only, 0.1.0), which is built; only the ship step (01:27) waits.

| Line(s) | Says | Mark | Code | Note |
| --- | --- | --- | --- | --- |
| 01:5 | One pack, RogueWavesMod, every part inside, no load order; parts arrive release by release | MATCH | `Pack/pack.txt:5-6`; `Shared/ModKitPack.cs:33` (each part `Join`s, e.g. `RogueWavesFoes/Code/00_RogueWavesFoesMod.cs:29`) | |
| 01:6 | Four vessels | MATCH | `Yossele/`, `Spool/`, `Argo/`, `Uloe/` | |
| 01:7 | Cursed, Berserk, Aging, Dazzling; enemy types later | MATCH | `RogueWavesFoes/Code/00_RogueWavesFoesMod.cs:103-104` | new enemy types: later by the line itself |
| 01:8-13 | Mechanics, mode, online, Directions, Fund Transfer, Cardinals and Trinkets | MATCH | `RogueWavesMode/Code/16_Directions.cs`, `22_AnyDirections.cs`, `23_BaseDirections.cs`, `17_Cardinals.cs`, `24_Trinkets.cs`, `18_Shop.cs:401-444` | pointers; detail in 04, 06 |
| 01:14 | Bug fixes | MATCH | see 03 | |
| 01:17-19 | 2-4 players, host runs it, same pack version | MATCH | `Shared/ModKitPack.cs:6`, `:33` (local mismatch warning) | rules for players; nothing enforces 2 or a network version check (as 11-05) |
| 01:22 | First release Yossele alone | MATCH | `Pack/pack.txt:6` (`part Yossele` only) | |
| 01:23 | After it, everything else | MATCH | all parts in `ModSource/`; not in `pack.txt` yet | release plan |
| 01:24 | Pack holding only Yossele, version 0.1.0 | MATCH | `Pack/pack.txt:5-6`; parts carry `0.1.0-dev` (`Yossele/Code/02_Data.cs:8`) | the zip itself ships at 01:27 |
| 01:25 | A zip you share yourself | MATCH | `TDS_Mod/Tools/pc/Build-Pack.ps1:4`, `:12` (`-Zip`) | |
| 01:26 | Yossele outside the mode, no FERTILIZE | MATCH | `Yossele/Code/06_HeroBrain.cs:155-160` (button only with a Compost Bin, a mode Direction) | |
| 01:27 | Ready when sprites, self-test, play test, "ship" | UNBUILT-BY-DESIGN | none | Release 1 waits on his play test and sprites |
| 01:28 | Status: 0.2.0-dev built and installed for his play test | MATCH | `docs/PLAN.md` Part C "Release 1" | |

Findings: none.

### 02_Progress.txt
Summary: rows 11; MATCH 10; RESOLVED 0; UNBUILT-QUEUED 0; UNBUILT-BY-DESIGN 0; DIFFERENT 1; MISSING 0.
Since 11-05, 02:9 gained the 177/177 count, 02:13 now says what is built and what waits, and 02:16 says the Mods folder holds every part's release build. All of these match the code and reports except one stale phrase in 02:9. Only the [Now] lines (02:6-16) were read; the [Log] is history.

| Line(s) | Says | Mark | Code | Note |
| --- | --- | --- | --- | --- |
| 02:6 | Phase 1 done, PC round 13 of 13 | MATCH | `docs/reports/REPO_SCAN_2026-10-08.md:38` | |
| 02:7 | Phase 2 done | MATCH | `docs/PACK.md`, `docs/STYLE.md` | |
| 02:8 | Argo 2.0 139/139; 2.0.1 142/142 and a second player | MATCH | `Argo/IMPLEMENTATION_NOTES.md:1`, `:38` | |
| 02:9 | Yossele 0.2.0-dev, 160, 173, 177 of 177; "it is the play build installed for your test" | DIFFERENT | `docs/reports/TESTING_ROUND_3_2026-11-05.md:21` (177/0); `Yossele/IMPLEMENTATION_NOTES.md:61-62` | counts match; "play build installed" is stale (finding 1) |
| 02:10 | Mode part built and passed its PC rounds | MATCH | `docs/reports/MODE_POLISH_2026-11-03.md` | |
| 02:11 | Next: the first release | MATCH | `docs/PLAN.md` Part C "Release 1" | |
| 02:12 | Built ahead and passed on the PC: button, vote, Spool, Uloe, modifiers, Directions/Cardinals/Trinkets | MATCH | `docs/reports/TEST_ROUND_2026-11-04.md:31` | |
| 02:13 | Unbuilt: Spool's 22 and UNTHREAD; Depth on hold; the rest of 07-12 built, compiled, not run | MATCH | `scope_upgrades.py`: Yossele 33, Argo 37, Uloe 32 (Searchlight `Uloe/Code/02_Data.cs:298`, Lighthouse `:305`) built; Spool 22 none (B19); `docs/reports/BUILD_UNBUILT_2026-11-05.md` §1 | |
| 02:14 | Riptide does not ship | MATCH | `Pack/pack.txt` (no Riptide) | |
| 02:15 | Sonar left out | MATCH | `Argo/Code/02_Data.cs:18` (`SonarEnabled = false`) | |
| 02:16 | Mods folder held only the Yossele play build until 2026-11-03, then every part's release build | MATCH | `docs/reports/BUILD_2026-11-03.md:131` | |

Findings:
1. **02:9 (DIFFERENT, text).** Scope: "it is the play build installed for your test". The cited `Yossele/IMPLEMENTATION_NOTES.md:61-62` says "0.2.0-dev was Dimitri's play build (`Mods\YosseleMod`, the only mod, until 2026-11-03; since then the release build of every pack part is installed and the play build is parked)". 02:16 in the same section says the same thing. Fix: `~...177 of 177 on 2026-11-05); its release build is installed with every other pack part's, for your test~`. Owner: text (Claude keeps 02).

### 03_Bug_Fix.txt
Summary: rows 6; MATCH 6; RESOLVED 0; UNBUILT-QUEUED 0; UNBUILT-BY-DESIGN 0; DIFFERENT 0; MISSING 0.
Since 11-05, the two fix names lost their tildes (his), and 03:8 now says the F-key PC pass ran before the fix was switched on. That matches the max review's P1-1 fix, which is in the shared kit now.

| Line(s) | Says | Mark | Code | Note |
| --- | --- | --- | --- | --- |
| 03:4-6 | F Picks a Minion: problem, how to see it | MATCH | `Shared/ModKitFocus.cs` | name his since 11-05b |
| 03:7 | Fix in shared code, runs with Argo, Yossele, Spool, Uloe | MATCH | `Argo/Code/00_ArgoMod.cs:129`, `Spool/Code/00_SpoolMod.cs:127`, `Uloe/Code/00_UloeMod.cs:126`, `Yossele/Code/00_YosseleMod.cs:129` (`ModKitFocus.Update`) | `Shared` equals `kit/Shared` |
| 03:8 | Built; PC pass ran with the fix off; fixed since, compiled; next round checks | MATCH | `Shared/ModKitFocus.cs:44`, `:50` (reference test, pitfall #157); `CODE_REVIEW_MAX_2026-11-05.md:44` | PC round 4 verifies |
| 03:10-12 | No Way Back In: problem, how to see it | MATCH | `RogueWavesMode/Code/09_Rejoin.cs` | |
| 03:13 | Host keeps each seat, lobby open while one is empty; Join Game | MATCH | `RogueWavesMode/Code/09_Rejoin.cs:1-13`, `:187-198`; `13_Card.cs:198` (rejoin line) | |
| 03:14 | Passed with a real second player and a restart; Steam path waits | MATCH | `docs/reports/TEST_ROUND_2026-11-04.md:44`, `:62`, `:101-102` | |

Findings: none.

### 04_Rogue_Waves_Mode.txt
Summary: rows 51; MATCH 46; RESOLVED 2; UNBUILT-QUEUED 0; UNBUILT-BY-DESIGN 2; DIFFERENT 1; MISSING 0.
Since 11-05: B1 (every map voted on) and B2 (lobby Cardinals locked and filtered) were built in #103. The balance sweep (#123) moved modifiers to map 2 with in-turn rotation, set Tide 15's rows and the health lift, and changed players to +50%; the code matches every new number (tildes are the sweep's by design). The one difference is the new Depth note's "the end screen says so".

All paths are in `RogueWavesMode/Code/` unless named.

| Line(s) | Says | Mark | Code | Note |
| --- | --- | --- | --- | --- |
| 04:10 | 10 maps | MATCH | `03_Chart.cs:7` | |
| 04:11 | Stretches of 5; rows 5 and 10 close one | MATCH | `03_Chart.cs:8`, `:132` | |
| 04:12 | Vessels locked in | MATCH | `05_Boat.cs:266-289` | |
| 04:13 | Main-menu button, New or Continue, to the boat | MATCH | `12_Menu.cs:1-6`, `:167-169`, `:219-221` | |
| 04:14 | Saved between maps | MATCH | `06_Campaign.cs:162-176`, `:283-284` | |
| 04:15 | Continue returns vessel, Pure Aura, Directions, Trinkets, Cardinals | MATCH | `06_Campaign.cs:78-107`; `14_Run.cs:146-215` | |
| 04:16 | Won by leaving map 10 after Tide 15 or later | MATCH | `20_Vote.cs:477-481`; `06_Campaign.cs:248-251`, `:311-315` | |
| 04:16 | A new Depth level unlocks | UNBUILT-BY-DESIGN | none | Depth on hold (04:41) |
| 04:16 note | "until it is built, nothing unlocks and the end screen says so" | DIFFERENT | `06_Campaign.cs:248`, `:313-314` | nothing unlocks (true); no screen mentions Depth (finding 1) |
| 04:17 | A failed campaign keeps nothing | MATCH | `06_Campaign.cs:148-160`, `:307-308` | |
| 04:18 | Score doesn't count | MATCH | `13_Card.cs:66`, `:72-84` | |
| 04:19 | Seed picks 2 pack + 2 stock elites; none on map 1, one from map 2, one more every second map (four by map 8); one foe in five; in turn Tide to Tide; stand-in for an elite a map can't load | MATCH | `21_Difficulty.cs:21-22`, `:245-288` (`:267` ModifiersOn), `:160`, `:174-188` | NEEDS answered (11-05b item 4); from map 2 and in turn: #123 |
| 04:22 | Each map harder: modifiers, spawn, objectives, health | MATCH | `21_Difficulty.cs:120-194`; `19_Objectives.cs:278-309` (meteor farther, more hits a map) | |
| 04:23 | Tide 15 plays row ~15, 18, 20, 21, 21, 24, 24, 24, 24, 24~; Tides 1-14 spread before it | MATCH | `21_Difficulty.cs:26`, `:70-80`, `:135-152` | default (sweep tilde, by design) |
| 04:23 | Foes +6% a map, +50% a player past the first; intervals -3% a map | MATCH | `21_Difficulty.cs:18`, `:122-123`, `:169-170` | |
| 04:23 | Health x~2.6~ map 1, +0.08 a map (x~3.32~ map 10), ~+2%~ a held Cardinal, Trinket copy, Direction rank (lobby not counted); multiplying, on top of an elite's | MATCH | `21_Difficulty.cs:19-20`, `:57-68`, `:114-118`, `:204-237` (Pmulti) | default (sweep tildes); a pick still owed counts as one rank (`:65`) |
| 04:23 | Bonus Tides play the rows after Tide 15's; from row 29 each +10% foes | MATCH | `21_Difficulty.cs:23`, `:79`, `:290-320` | |
| 04:26 | 3 retries | MATCH | `06_Campaign.cs:21` | |
| 04:27 | A loss replays from the last save; none left ends it | MATCH | `06_Campaign.cs:292-309` | |
| 04:28 | Host quitting loses the map; only that spends a retry | MATCH | `06_Campaign.cs:102-103`, `:232-252`, `:264-272` | |
| 04:29 | A friend who drops spends nothing, rejoins; dropped during load comes back on the boat | MATCH | `09_Rejoin.cs:1-13`, `:187-198` | |
| 04:30 | Dying in a bonus Tide loses nothing; that Tide pays nothing | MATCH | `06_Campaign.cs:210-227`, `:243-246`; pay at the break `20_Vote.cs:82-89`, `14_Run.cs:268-279` | |
| 04:31 | After a loss the boat sails back; only that map | MATCH | `06_Campaign.cs:43-51`, `:300-301` | |
| 04:32 | Status | MATCH | `docs/reports/BUILD_2026-11-03.md` | |
| 04:35-41 | Depth: 5 levels, carry over, unlocked by a win; on hold | UNBUILT-BY-DESIGN | none | on hold (04:41) |
| 04:44 | 60 s breaks | MATCH | `02_Mode.cs:10`, `:30-38` | opening break keeps 45 s (not between Tides) |
| 04:45 | Map won once Tide 15 is cleared | MATCH | `06_Campaign.cs:22`, `:210-227`; `20_Vote.cs:143-150` | |
| 04:46 | Vote to leave, majority | MATCH | `20_Vote.cs:439-485` (`:468`) | |
| 04:47 | Only between Tides | MATCH | `20_Vote.cs:143` (`InBreak`, field clear) | |
| 04:48 | Bonus Tides without limit, each harder | MATCH | `21_Difficulty.cs:290-320` | |
| 04:49 | Status | MATCH | `docs/reports/BUILD_2026-11-03.md` | |
| 04:52 | A harbour, then 10 rows of 3 or 4 maps | MATCH | `03_Chart.cs:102-125` (`:109`) | |
| 04:53 | 1 to 3 links; rows 5 and 10 close a stretch | MATCH | `03_Chart.cs:10`, `:71-99`, `:132` | |
| 04:54 | Every base map; never the custom slot | MATCH | `02_Mode.cs:75-87` (`:79`) | |
| 04:55 | No map twice | MATCH | `03_Chart.cs:134-150` (`:148`) | |
| 04:56 | Easier first, hardest closing a stretch | MATCH | `03_Chart.cs:67-68`, `:139-147` | |
| 04:57 | Every map is voted on, the first included | RESOLVED | `20_Vote.cs:321-361`, `:468-472` (stay votes the next map, B1); `05_Boat.cs:203-263` | B1, PR #103. On the boat, when no vote ran, the host picks (no channel for a friend's pick, kit docs/19) |
| 04:58 | Vote offers the linked maps; after the vote to leave; map 1's in a staging map | MATCH | `20_Vote.cs:61-63`, `:219-229`, `:308-309`, `:321-413`; `06_Campaign.cs:43-51` | |
| 04:59 | Back to the boat, which sails to the winner; friends join there | MATCH | `20_Vote.cs:487-507`; `05_Boat.cs:178-180`, `:337-370` | |
| 04:60 | No Cardinal or Trinket swaps on the boat | MATCH | `17_Cardinals.cs:778-786`, `:731` (regrant each map) | |
| 04:61 | Lobby Cardinals picked once, locked, filtered as the shop | RESOLVED | `17_Cardinals.cs:788-858`; `05_Boat.cs:152-155`; `20_Vote.cs:55`; `06_Campaign.cs:69` | B2, PR #103 |
| 04:62 | Each map's own island; one borrowed if none | MATCH | `04_World.cs:223-268` | |
| 04:63 | Status | MATCH | `docs/mode/SEA_CHART.md` §7-9 | |
| 04:66-68 | Rolled with the chart; calm rows; 1 in 3, then 1 in 2 | MATCH | `03_Chart.cs:151-163` | |
| 04:69 | +1 Pure Aura a Tide on top of 3 or 2 | MATCH | `07_Weather.cs:16`; `14_Run.cs:34`, `:271-273` | |
| 04:70 | On the chart's island and the boat's map panel; the start announces only the name | MATCH | `04_World.cs:276-288`; `05_Boat.cs:109-113`; `07_Weather.cs:264` | Quickening Current's Tide 5/10 lines (`:283`, `:289`) are not the start |
| 04:71 | Never on a map that has it | MATCH | `03_Chart.cs:56-65`, `:161` | |
| 04:72 | Status | MATCH | as 04:63 | |
| 04:74-79 | Rising Water: line, flood, as Manafold, never Manafold, look | MATCH | `07_Weather.cs:4-5`, `:34`, `:41-42`; `03_Chart.cs:60`; `08_Icons.cs:24`, `:76` | temporary icon |
| 04:81-86 | Quickening Current: line, x1.25 from Tide 5, x1.5 from 10, never Chrono, look | MATCH | `07_Weather.cs:6-7`, `:17-18`, `:35`, `:276-291`; `03_Chart.cs:61`; `08_Icons.cs:40`, `:77` | |
| 04:88-93 | Nesting Ground: line, nest 3-7 tiles from an enemy core, never Vultures, look | MATCH | `07_Weather.cs:8-9`, `:36`, `:358-390` (`:371`); `03_Chart.cs:62`; `08_Icons.cs:56`, `:78` | |

Findings:
1. **04:16 note and 05:13 note (DIFFERENT, text).** Scope: `/~ Depth is on hold: until it is built, nothing unlocks and the end screen says so \`. The code does unlock nothing. But the end screen's title is only "CAMPAIGN WON" (`06_Campaign.cs:248`), and the chat line is "All 10 maps sailed: the campaign is won." (`06_Campaign.cs:313`). "Depth, its reward, is on hold" appears only in the log (`06_Campaign.cs:314`), and that log line is what `TEXT_AUDIT_2026-11-05.md:83` cited as "the end screen". No player-facing text mentions Depth. Fix: either reword the note to `~Depth is on hold: until it is built, nothing unlocks~`, or add the words to the chat line, e.g. `~"All 10 maps sailed: the campaign is won. Depth comes later."~`. Owner: text thread (the note), or the build thread if he wants the line on screen. Rows 04:16 note and 05:13 note are this one finding.

### 05_Gameplay_Rundown.txt
Summary: rows 10; MATCH 7; RESOLVED 1; UNBUILT-QUEUED 0; UNBUILT-BY-DESIGN 1; DIFFERENT 1; MISSING 0.
Since 11-05, three wording fixes went in, and 05:13 gained the Depth note. Step 3 (every map voted on) is RESOLVED by B1 in #103.

| Line(s) | Says | Mark | Code | Note |
| --- | --- | --- | --- | --- |
| 05:5 | Lobby, vessels locked, the button | MATCH | as 04:12-13 | |
| 05:6 | 3 Directions, pick 1 | MATCH | `RogueWavesMode/Code/16_Directions.cs:22`; `20_Vote.cs:231-249` | |
| 05:7 | Every map voted on, the first included | RESOLVED | as 04:57 | B1, PR #103 |
| 05:8-9 | Pure Aura each Tide, objectives, VENDCINT sells Cardinals, Trinkets, ranks | MATCH | as 06:23-29, 06:41-45 | |
| 05:10 | Leaving by majority, only between Tides, bonus Tides | MATCH | as 04:46-48 | |
| 05:11 | 10 maps, each harder | MATCH | as 04:10, 04:22-23 | |
| 05:12 | Saved between maps; Continue | MATCH | as 04:14-15 | |
| 05:13 | Won by leaving map 10; a failed campaign keeps nothing | MATCH | as 04:16-17 | |
| 05:13 | A new Depth level unlocks | UNBUILT-BY-DESIGN | none | Depth on hold |
| 05:13 note | "the end screen says so" | DIFFERENT | `RogueWavesMode/Code/06_Campaign.cs:248`, `:313` | same as 04 finding 1 |

Findings: 05:13 note: see 04 finding 1 (one fix covers both).

### 06_New_Mechanics.txt
Summary: rows 28; MATCH 26; RESOLVED 2; UNBUILT-QUEUED 0; UNBUILT-BY-DESIGN 0; DIFFERENT 0; MISSING 0.
Since 11-05, the Power, Pairs and Duos rules are his (tildes gone). Duos and the "about 7 Any, 4 per vessel" count were built in #107-#111 (RESOLVED). Two lines are new and match the code: the price ramp ~3~ (a sweep default) and objectives from the break before Tide 3.

All paths are in `RogueWavesMode/Code/` unless named.

| Line(s) | Says | Mark | Code | Note |
| --- | --- | --- | --- | --- |
| 06:7 | 3 offered, 1 picked before each map | MATCH | `16_Directions.cs:22`, `:189-247`; `20_Vote.cs:219-249`, `:476-484` | |
| 06:8 | A held Direction comes back as its next rank | MATCH | `16_Directions.cs:218-220` | |
| 06:8 note | About 7 for any vessel and 4 per new vessel | RESOLVED | Any: `16_Directions.cs:30-82` (20 + 2 Duos); Yossele 8, Spool 7, Argo 4, Uloe 4 | B11, PRs #107-#111 |
| 06:9 | Rank II 20, rank III 30 | MATCH | `16_Directions.cs:23`, `:283`; `18_Shop.cs:241-250` | |
| 06:10 | The shop sells only the next rank | MATCH | `16_Directions.cs:260-281` | |
| 06:11 | Kept on the vessel, back each map, saved | MATCH | `16_Directions.cs:304-325`; `14_Run.cs:146-215` | |
| 06:12 | Base vessels get their own | MATCH | `16_Directions.cs:83-105`; `23_BaseDirections.cs:16` | |
| 06:13 | Names | MATCH | rule for sheets | |
| 06:14 | Power scale | MATCH | rule for sheets | his since 11-05b |
| 06:15 | Pairs never offered together | MATCH | `16_Directions.cs:205-228`, `:250-258`; Wound Spring / Spring Tide `:42`, `:53` | the new pair built in #109 |
| 06:16 | Duos: offered only to a holder of both, one rank | RESOLVED | `16_Directions.cs:17`, `:79-82`, `:124`, `:163-175` | B11, PR #109 (Fire Ship, Tide Pool) |
| 06:17-18 | By; the list in 11 | MATCH | rules; 11 | |
| 06:19 | Status: built and passed; the ones added since compiled, not run | MATCH | `docs/reports/BUILD_UNBUILT_2026-11-05.md` §1; `docs/PLAN.md` Part C "Now" | |
| 06:23-24 | 3 a Tide, 2 a bonus Tide | MATCH | `14_Run.cs:34`, `:271-273` | |
| 06:25 | Weather pays extra | MATCH | `07_Weather.cs:16` | |
| 06:26 | Objectives pay their own | MATCH | `19_Objectives.cs:16-20`, `:240-241` | |
| 06:27 | Pearl-white counter on each vessel | MATCH | `14_Run.cs:324-325` (`ModKitCounter`), `:531` | |
| 06:28-29 | Spent at VENDCINT; status | MATCH | `18_Shop.cs:217-250`; `17_Cardinals.cs:542` (`Buy`) | |
| 06:34 | Only base aura | MATCH | `18_Shop.cs:413`; `Shared/ModKitUtil.cs:253` | |
| 06:35 | 50 x the coming Tide a break (50 before Tide 1, 250 before Tide 5) | MATCH | `18_Shop.cs:404-407`; reset `20_Vote.cs:90-91` | |
| 06:36-37 | At VENDCINT; status | MATCH | `18_Shop.cs:56`, `:282-320` | |
| 06:41 | Falls in at every break, the opening one too | MATCH | `18_Shop.cs:24-62`; `20_Vote.cs:92-96`, `:136-139` | not in the staging map |
| 06:42 | Jumps away at the end of the break | MATCH | `18_Shop.cs:12`, `:98-102` | |
| 06:43 | Sells Cardinals, Trinkets, next ranks; a base game Cardinal costs Pure Aura 15 | MATCH | `17_Cardinals.cs:34`, `:208-212`; `18_Shop.cs:217-250` | |
| 06:43 | Each ~3~ more for every Cardinal bought and every Trinket copy the buyer holds | MATCH | `17_Cardinals.cs:35`, `:451-455` (lobby picks left out) | default (sweep tilde, C1); Direction ranks not ramped (`18_Shop.cs:246`) |
| 06:44 | Fund Transfer at it | MATCH | `18_Shop.cs:56` | |
| 06:45 | A new objective every break from the one before Tide 3 | MATCH | `19_Objectives.cs:34`, `:111-123`; `20_Vote.cs:177-181` | P1, sweep |
| 06:46 | Status, temporary art | MATCH | `docs/TEMP_ART.md` | |

Findings: none.

### 07_Vessel_Yossele.txt

Summary: rows 65; MATCH 62; RESOLVED 1; UNBUILT-QUEUED 0; UNBUILT-BY-DESIGN 2; DIFFERENT 0; MISSING 0.
Since 11-05 the scope only lost its tildes (his defaults now) and its Status line moved to "177 of 177" plus "built, not
run"; the code gained Wood Wide Web (#106, the 11-05 audit's one UNBUILT-QUEUED row, now RESOLVED), the Shift panel
names (B8, #106), the five Claude-made Directions (#107) and Claude's final sprite sheets in `Yossele/Art/` (0a8ed49).
No number of the vessel, its buildings or the Cordyfriend changed in `02_Data.cs` (`git diff 99b9f3d..c197626`), so every
11-05 MATCH still holds; line numbers below are fresh at c197626.

| Line(s) | Says | Mark | Code | Note |
| --- | --- | --- | --- | --- |
| 07:8-11 | Title The Patient Golem, Intermediate, scaling archetype, in-game line | MATCH | `Yossele/Code/02_Data.cs:67-70` | in-game line was a default, now his |
| 07:12 | Look: clay head, mouth hole, sprout, green eyes, four root limbs | MATCH | `Yossele/Art/README.md:1-12` | final sheets drawn from his reshade (0a8ed49); temporary until he takes them |
| 07:13 | Towers look: holes, root feet, a stage adds leaves | MATCH | `docs/TEMP_ART.md:19`, `:42` | temporary art (code-drawn); Shift looks A1 rendered, not seen in game |
| 07:14 | Accent colour leaf green | MATCH | `Yossele/Code/04_Content.cs:24` | #72CC35 |
| 07:15 | Sprites: `! NEEDS YOUR FULL SPRITE SHEET !` | UNBUILT-BY-DESIGN | `Yossele/Art/Yossele-Sheet.png` (README `:3-5`) | NEEDS slot; Claude's final sheets wired by name; line cleared only after PC round 4 shows Yossele walking (PLAN Part C "Next") |
| 07:16 | Lore comes later | UNBUILT-BY-DESIGN | `Yossele/Code/02_Data.cs:73-74` | placeholder line, as the scope asks |
| 07:17 | Status: 177 of 177; Wood Wide Web, Shift names, new Directions built, not run | MATCH | `docs/reports/TESTING_ROUND_3_2026-11-05.md:21` | round 3 177/0; the rest compiled, not run (PC round 4 list, `BUILD_UNBUILT_2026-11-05.md` §5 item 3) |
| 07:20 | Attack: ranged green mouth laser | MATCH | `Yossele/Code/02_Data.cs:18`; `04_Content.cs:309-321` | Sno Sno's snowball as the template |
| 07:21-29 | Health 70, regen 1/s while hurt, Speed 1.75, Damage 5.5, Range 4, Projspeed 15, Attack speed 0.5, Build range 2.5, Sell 50% | MATCH | `Yossele/Code/02_Data.cs:83-87`; `06_HeroBrain.cs:98-110` | Projspeed was a default, now his |
| 07:30 | Special rule {Patient Growth} and its text | MATCH | `Yossele/Code/02_Data.cs:71-72` | |
| 07:33-34 | FERTILIZE: +1 Growth stage for 1 fertilizer | MATCH | `Yossele/Code/06_HeroBrain.cs:248-250` | |
| 07:35-37 | Target one of your buildings (hotkey: nearest growable), 6 tiles for the hotkey's pick, 0.5 s | MATCH | `Yossele/Code/02_Data.cs:97-98`; `06_HeroBrain.cs:218-221`, `:247` | |
| 07:38 | Keys 6 / 7 | MATCH | `Yossele/Code/04_Content.cs:367` | |
| 07:39-40 | Unlocked by a Direction, button hidden until then; costs 1 fertilizer | MATCH | `Yossele/Code/06_HeroBrain.cs:155-171`; `:242-248` | Compost Bin |
| 07:43, 07:46-47 | Fertilizer only with the Direction; earned by Compost Bin (numbers in 11) | MATCH | `Yossele/Code/05_Server.cs:120-135`; `02_Data.cs:104-109` | 10/4, 6/4, 6/7 = scope 11:304 |
| 07:44-45 | Start 0 each map, cap 10 | MATCH | `Yossele/Code/05_Server.cs:144-149`, `:93`; `02_Data.cs:96` | |
| 07:48-49 | Resets between maps; kept on the vessel's death | MATCH | `Yossele/Code/05_Server.cs:149`, `:93` | held on the player record, not the vessel |
| 07:52-54 | Growth +1 a Tide survived, 5 on a map of 4 Tides or fewer; lost when destroyed | MATCH | `Yossele/Code/05_Server.cs:169-174`; `02_Data.cs:93-94` | |
| 07:59-60, 07:62-74 | Mushroom Cannon: lobbed spore bombs; Health 20+25, Damage 1+0.25, AS 0.4, Range 9, AOE 1, 2 spores, 0.3/s a stack, tick 0.5, fade 6 s, max 10 (full-spore look), no limit, Red 15 | MATCH | `Yossele/Code/02_Data.cs:288-291`, `:200-204`; `08_Cannon.cs:163` | |
| 07:76-79 | Cannon Growth: max 5, +10% AS and +0.1 radius a stage; bounce at max 1 s later, half AOE, half damage, 1 stack | MATCH | `Yossele/Code/02_Data.cs:205-208`; `08_Cannon.cs:59-60` | |
| 07:116-126 | Dandelion Tree: Health 50+25, 2+0.25 a seed, Range 7, 10 seeds, 1 every 2 s, full at Tide start, 0.3 s flight shared nearest first, limit 1, Red 50 | MATCH | `Yossele/Code/02_Data.cs:305-308`, `:225`; `09_Dandelion.cs:3-4`, `:106-111`; `05_Server.cs:335-343` | flight and health were defaults, now his |
| 07:128-130 | Tree Growth: max 15, +3 seeds and +20% base range a stage, nothing at max | MATCH | `Yossele/Code/02_Data.cs:223-224`, `:306` | |
| 07:172-174 | Stonehenge: directional wall, vines creep forward, onto building tiles, slow by default | MATCH | `Yossele/Code/10_Henge.cs:136`, `:162-174` | |
| 07:176-184 | Stonehenge stats: Health 80+40, slow 15%, vine 1 tile at start, facing arrows (stops at a cliff), flat ground tiles, 0.55 tiles, no limit, Blue 25 | MATCH | `Yossele/Code/02_Data.cs:324-326`, `:241`; `10_Henge.cs:29`; `04_Content.cs:119-144` | |
| 07:186-188 | Stonehenge Growth: max 8, +10% damage reduction a stage, +1 vine every 2 stages | MATCH | `Yossele/Code/02_Data.cs:238-239`, `:324`; `10_Henge.cs:29`, `:118-119` | 90% cap `12_Hooks.cs:86` |
| 07:228 | Pachira mines every node in range through its roots | MATCH | `Yossele/Code/11_Pachira.cs:96` | |
| 07:230-238 | Pachira stats: Health 30+20, 1 a strike, AS 0.25, Range 2.5, fruit AS x Damage x 15 of all colours, pickup 0.75 tiles by any vessel or minion, stays to map end, no limit, Yellow 35 | MATCH | `Yossele/Code/02_Data.cs:342-344`, `:248-249`; `11_Pachira.cs:174`, `:431-432` | |
| 07:240-243 | Pachira Growth: max 5, +10% mining and +0.4 tiles a stage; fruit at max | MATCH | `Yossele/Code/02_Data.cs:247`; `11_Pachira.cs:62`, `:140`, `:193-197` | |
| 07:291-302 | Cordyfriend: from Cordyceps, melee, 75 / 2 / 1.5 / 0.5 / 2.25, 30 a player, no respawn, engages within 4, leash 5, free | MATCH | `Yossele/Code/02_Data.cs:212-218`; `16_Friend.cs:34`, `:55`, `:157-162` | |
| 07:303 | Look: temporary v04 Antlers at Snoble size, 24 px, idle 6 / move 6 / attack 4 | MATCH | `Yossele/Code/03_Assets.cs:28-40`; `02_Data.cs:20` | his pick, `Art/Temporary/README.md:9` |
| 07:306 | His Directions: Compost Bin, Dandelion Season, Mother of Mushrooms | MATCH | `Yossele/Code/04_Content.cs:637-659`; `02_Data.cs:102-152` | numbers audited in 11; Shift panel names B8 `02_Data.cs:117-120` (#106) |
| 07:307 | Claude's: Cutting Jar, Sap Bucket, Creeping Ivy, Deep Breath, Fairy Ring | MATCH | `Yossele/Code/04_Content.cs:661-663`; `02_Data.cs:156-196` | new since 11-05, built in #107; numbers audited in 11 |

#### Yossele: every upgrade (`scope_upgrades.py`, numbers read by hand)

Level, cost, in-game line and Leads to all agree for all 33 (script: 33 MATCH). The Numbers lines were read against each
entry's parameters: all agree (Obsidian's "200% increase" is `Health_Multi=2`; D-MDS's 1 every 4 s is
`Seedrate_Multi=-0.5` on the base 0.5/s; Garden Weeds' "debuff lasts 3 s" is `Weeds_Window=3`, its stack cap is the
Growth stage, `09_Dandelion.cs:335`, and the 15 s a stack lasts is a code default the scope doesn't set).

| Line | Building | Upgrade | Level | Mark | Code | Note |
| --- | --- | --- | ---: | --- | --- | --- |
| 07:81 | Mushroom Cannon | Sticky Spores | 1 | MATCH | `Yossele/Code/02_Data.cs:294` | 25% |
| 07:85 | Mushroom Cannon | Farout Fungus | 1 | MATCH | `Yossele/Code/02_Data.cs:295` | +2, +50%/s up to +75% (`:209`), lob 0.6+0.08 (`:204`) |
| 07:90 | Mushroom Cannon | Viral Outbreak | 2 | MATCH | `Yossele/Code/02_Data.cs:296` | 30%, 15, 2 tiles, 1 stack |
| 07:94 | Mushroom Cannon | Deathcap Training | 2 | MATCH | `Yossele/Code/02_Data.cs:297` | +25%, max 8 |
| 07:99 | Mushroom Cannon | Explosive Finish | 3 | MATCH | `Yossele/Code/02_Data.cs:298` | 1, 1.25, 2; 1 s guard (`:210`) |
| 07:103 | Mushroom Cannon | Hallucinogen | 3 | MATCH | `Yossele/Code/02_Data.cs:299` | 5 s, 20%, 2 s |
| 07:108 | Mushroom Cannon | Cordyceps | 4 | MATCH | `Yossele/Code/02_Data.cs:300` | 20% + 5% a stack |
| 07:132 | Primordial Dandelion Tree | Seed Packets | 1 | MATCH | `Yossele/Code/02_Data.cs:311` | 2 seeds, +3; leads to Picky, Weeds |
| 07:137 | Primordial Dandelion Tree | Demand and Supply | 1 | MATCH | `Yossele/Code/02_Data.cs:312` | x2 under 25%; leads to D-MDS, Picky, Weeds |
| 07:143 | Primordial Dandelion Tree | Picky Placement | 2 | MATCH | `Yossele/Code/02_Data.cs:313` | 0.25% |
| 07:147 | Primordial Dandelion Tree | Garden Weeds | 2 | MATCH | `Yossele/Code/02_Data.cs:314` | 10% a stack, 3 s, 40% |
| 07:151 | Primordial Dandelion Tree | D-MDS | 2 | MATCH | `Yossele/Code/02_Data.cs:315` | 1/4 s; 5 seeds for a 600+ boss (`09_Dandelion.cs:263-268`) |
| 07:156 | Primordial Dandelion Tree | Bonsai | 3 | MATCH | `Yossele/Code/02_Data.cs:316` | 40% (`:361-368`), sells 0 (`09_Dandelion.cs:447`), limit 1, Red 50 |
| 07:160 | Primordial Dandelion Tree | Rubbery Shell | 3 | MATCH | `Yossele/Code/02_Data.cs:317` | 2 bounces, 2 tiles |
| 07:165 | Primordial Dandelion Tree | Dandelion Storm | 4 | MATCH | `Yossele/Code/02_Data.cs:318` | +100% range, +20 seeds, +100% rate |
| 07:190 | Overgrown Stonehenge | Verdant Spread | 1 | MATCH | `Yossele/Code/02_Data.cs:329` | 2, 3 at full Growth |
| 07:194 | Overgrown Stonehenge | Vine Blockade | 1 | MATCH | `Yossele/Code/02_Data.cs:330` | +50% health, 30% barrier a Tide |
| 07:199 | Overgrown Stonehenge | Natural Remedy | 2 | MATCH | `Yossele/Code/02_Data.cs:331` | 0.5 a stage, stage 1 floor (`10_Henge.cs:276`) |
| 07:203 | Overgrown Stonehenge | Opportunistic Carnivore | 2 | MATCH | `Yossele/Code/02_Data.cs:332` | 5/s, 5 foes |
| 07:207 | Overgrown Stonehenge | Easy Target | 2 | MATCH | `Yossele/Code/02_Data.cs:333` | +15% after 5 s |
| 07:212 | Overgrown Stonehenge | Entangling Roots | 3 | MATCH | `Yossele/Code/02_Data.cs:334` | 7 s, 1.5 s, 5% |
| 07:216 | Overgrown Stonehenge | Obsidian | 3 | MATCH | `Yossele/Code/02_Data.cs:335` | +200% |
| 07:221 | Overgrown Stonehenge | Monument | 4 | MATCH | `Yossele/Code/02_Data.cs:336` | 20%; foes' debuffs only, own trade-offs kept (B9, `12_Hooks.cs:142`); 90% cap `:86` |
| 07:245 | Pachira Sapling | Focused Roots | 1 | MATCH | `Yossele/Code/02_Data.cs:347` | +80%, added (`11_Pachira.cs:62-64`) |
| 07:249 | Pachira Sapling | Sustaining Practices | 1 | MATCH | `Yossele/Code/02_Data.cs:348` | 0.5 a node a stage, stage 1 floor (`11_Pachira.cs:36`) |
| 07:254 | Pachira Sapling | Grafted | 2 | MATCH | `Yossele/Code/02_Data.cs:349` | 70% of the three summed (`11_Pachira.cs:184`) |
| 07:258 | Pachira Sapling | Forest Project | 2 | MATCH | `Yossele/Code/02_Data.cs:350` | 10% each, 4 tiles, anyone's (`11_Pachira.cs:75-84`) |
| 07:262 | Pachira Sapling | Spontaneous Mulch | 2 | MATCH | `Yossele/Code/02_Data.cs:351` | 20% for 5 s, no stack |
| 07:267 | Pachira Sapling | Connected Roots | 3 | MATCH | `Yossele/Code/02_Data.cs:352` | 65% above 50%; other Saplings left out (`12_Hooks.cs:280`) |
| 07:271 | Pachira Sapling | Harvesting Season | 3 | MATCH | `Yossele/Code/02_Data.cs:353` | +100% |
| 07:275 | Pachira Sapling | Wood Wide Web | 3 | RESOLVED | `Yossele/Code/02_Data.cs:354`; `11_Pachira.cs:317-361` | was UNBUILT-QUEUED; built #106: every 1 s, once a player, gap 100, 2 for 1; not yet run on the PC |
| 07:280 | Pachira Sapling | Fortifying Roots | 4 | MATCH | `Yossele/Code/02_Data.cs:355` | +100% health, 30% aura (`12_Hooks.cs:286-291`) |
| 07:284 | Pachira Sapling | Softening Roots | 4 | MATCH | `Yossele/Code/02_Data.cs:356` | 1 point, never below 1 (`11_Pachira.cs:269-280`) |

Findings (DIFFERENT / MISSING / SCOPE-TEXT): none.

Note for the queued docs PR after PC round 4 (not a finding): when 07:15's NEEDS line is cleared, 07:17 "with temporary
art" should change with it, since `Yossele/Art/` now holds Claude's final sheets (0a8ed49). Owner: the thread that runs PC
round 4's docs PR.

### 08_Vessel_Spool.txt

Summary: rows 74; MATCH 45; RESOLVED 3; UNBUILT-QUEUED 24; UNBUILT-BY-DESIGN 2; DIFFERENT 0; MISSING 0.
Since 11-05 the scope took the balance sweep's numbers (Stitched 5, Dart 5.5, 6 a Tin, Loom 0.3; tildes kept until after
PC round 4) and the "Build it first" line, and the code matches all of them (#123). #103 built B6 (pack modifiers count
as elite) and B16 (no stock MOVE on the Thread Head). The 22 upgrades and UNTHREAD wait on B19, and Pin Barrage's Leads to
waits on B18. Code paths are in `Spool/Code/` unless they say otherwise. Nothing here was compiled or run: it was read from the code at `c197626`.

| Line(s) | Says | Mark | Code | Note |
| --- | --- | --- | --- | --- |
| 08:8-11 | Title, Advanced, archetype, "Crafty melee, commands a stitched army" | MATCH | `Spool/Code/02_Data.cs:55-57` | archetype has no code slot; the build is it |
| 08:12, 08:14 | Look and accent: sage green 93CCA1 / 60A871 | MATCH | `Spool/Code/04_Content.cs:383-384` | art: `Spool/Art/Temporary/README.md:10` |
| 08:13 | Towers look: stitches, pins and tins | MATCH | `Spool/Art/Temporary/README.md:10` | temporary art |
| 08:15 | Full sprite sheet once past drafting | UNBUILT-BY-DESIGN | - | temporary art; his sprites (Release / his look) |
| 08:16 | Lore comes once finalized | UNBUILT-BY-DESIGN | `Spool/Code/02_Data.cs:61` | placeholder line in the journal |
| 08:17 | Status: 0.1.0-dev, 135 of 135, the listed upgrades and UNTHREAD unbuilt | MATCH | `Spool/Code/02_Data.cs:9` | round 3 is still the latest Spool round (`TESTING_ROUND_3_2026-11-05.md:22`); the list is still true (B19) |
| 08:20-28 | Health 45, regen 0, Speed 2.75, Damage 2.5, Range 1.5, attack 0.6, build 2.5, sell 50% | MATCH | `Spool/Code/02_Data.cs:76-79` | |
| 08:29 | Special rule {Hand Stitched} | MATCH | `Spool/Code/02_Data.cs:58-59` | |
| 08:32-39 | PATCH: 20 Thread, 20% up to 500, own unit/building, 4 tiles, 5 s, keys 6/7, from start | MATCH | `Spool/Code/02_Data.cs:95-96`; `Spool/Code/04_Content.cs:393`, `:411`; `Spool/Code/06_HeroBrain.cs:281` | |
| 08:42-46 | Start 10, cap 50 (+), carried 20/20, small 1 up to 5 elite; also Loom and Cushion | MATCH | `Spool/Code/02_Data.cs:83-86`; `Spool/Code/05_Server.cs:284-287`; `Spool/Code/07_Building.cs:159-163`; `Spool/Code/10_Cushion.cs:117` | large 3, elite +2: his defaults |
| 08:46 | The pack's own modifiers count as elite | RESOLVED | `Spool/Code/05_Server.cs:292-304` | B6, PR #103 (`RW_Modifier_` keys) |
| 08:47-51 | 15 s on the ground, pickup 1 tile, Spool or Head collects, carried to the Stash, drop-off 1.5 | MATCH | `Spool/Code/02_Data.cs:88-89`; `Spool/Code/06_HeroBrain.cs:138`, `:152` | |
| 08:52-53 | Spent on Crafted and PATCH; sold back half, rounded down | MATCH | `Spool/Code/08_Tin.cs:567-578` | |
| 08:54-55 | Resets between maps; kept on death | MATCH | `Spool/Code/05_Server.cs:65-75` | |
| 08:61-65 | Yarn Stash: line, Health 100 +50, cap 50 +25, 1, free at start | MATCH | `Spool/Code/02_Data.cs:265-268`; `Spool/Code/07_Building.cs:151` | |
| 08:101-111 | Stitched: Tin, melee, Health 20, Range 1.5, 0.8, Speed 1.8, orders, leash 2, Thread 10 | MATCH | `Spool/Code/02_Data.cs:196-199` | |
| 08:105 | Stitched Damage ~5~ | MATCH | `Spool/Code/02_Data.cs:199` | default; balance sweep, tilde kept until after PC round 4 |
| 08:112-122 | Dart: New Design, ranged, Health 12, 0.6, Speed 1.6, closer, leash 1, Thread 15 | MATCH | `Spool/Code/02_Data.cs:200-203` | |
| 08:116 | Dart Damage ~5.5~ | MATCH | `Spool/Code/02_Data.cs:203` | default; balance sweep |
| 08:117 | Dart Range ~3.5~ | MATCH | `Spool/Code/02_Data.cs:203` | default; the sweep kept it |
| 08:123-135 | Seam: Loom upgrade, Health 10, Damage 1, 1 per Loom, 10 s, leash 2.5, free | MATCH | `Spool/Code/02_Data.cs:204-207`, `:337` | |
| 08:136-147 | Mangled: Health 15 / 45 tanky, 1.5, Speed 2, 1 per 40 damage, 3 alive, leash 2, free | MATCH | `Spool/Code/02_Data.cs:208-215`, `:319` | |
| 08:151-155 | Tin draws on the Stash, sends Stitched to a spot, Darts by upgrade, aura vs Thread, sold by Spool | MATCH | `Spool/Code/08_Tin.cs:151-158`; `Spool/Code/06_HeroBrain.cs:186-199` | round 3's click fix (#103); B17's bigger click waits on his try |
| 08:156 | UNTHREAD button | UNBUILT-QUEUED | - | B19 |
| 08:158-164 | Tin: line, Health 60 +25, 1 every 4 s, 8 tiles, no limit, Red 50 | MATCH | `Spool/Code/02_Data.cs:274-276`; `Spool/Code/08_Tin.cs:35` | |
| 08:160 | Crafted alive at once ~6~ | MATCH | `Spool/Code/02_Data.cs:276` | default; balance sweep |
| 08:204-207 | Head: one larger minion, own spot, collects, returns to the Stash, buffer or porter | MATCH | `Spool/Code/09_Head.cs:2`, `:18-20`, `:99` | the buffer/porter paths are B19 (sub-table) |
| 08:208-221 | Head: build menu, Health 60, Damage 2, Range 1.5, 0.6, Speed 2.25, carries 20, collects 3, no limit, 15 s at the Stash, leash 3, Red 40 | MATCH | `Spool/Code/02_Data.cs:31`, `:224-227`, `:294`; `Spool/Code/09_Head.cs:270-281`; `Spool/Code/05_Server.cs:492-506` | |
| 08:204, 08:219 | Guards the spot you choose (SPOT only) | RESOLVED | `Spool/Code/04_Content.cs:423-433` | B16, PR #103: the stock MOVE's button is gone |
| 08:256-258 | Cushion reflects melee, spins Thread, Mangled tanky or bombers | MATCH | `Spool/Code/10_Cushion.cs:4-8`, `:76` | |
| 08:260 | In-game line "Build it first: ..." plus the journal sentence | MATCH | `Spool/Code/02_Data.cs:308-309` | P4 / S1, PR #123 |
| 08:261-265 | Cushion: Health 100 +40, 20%, 1 per 25, no limit, Blue 20 | MATCH | `Spool/Code/02_Data.cs:307-310` | |
| 08:305-306 | Loom mines nodes, spins Thread | MATCH | `Spool/Code/02_Data.cs:329`; `Spool/Code/07_Building.cs:206-211` | |
| 08:308-314 | Loom: line, Health 40 +20, 2.5 tiles, 1 every 6 s, holds 10, no limit, Yellow 40 | MATCH | `Spool/Code/02_Data.cs:330-332` | |
| 08:311 | Mining ~1 every 0.3 s within 2.5 tiles~ | RESOLVED | `Spool/Code/02_Data.cs:332` | 11-05 UNBUILT-QUEUED (S2; N9); the sweep set 0.3 in code and scope, PR #123; default |
| 08:349 | Efficient Recycler, Sewing Machine; Made of Tougher Stuff | MATCH | `Spool/Code/02_Data.cs:102-127`; `RogueWavesMode/Code/16_Directions.cs:31` | |
| 08:350 | Double Needle, Button Jar, Pattern Book, Merit Badge, Odd Socks | MATCH | `Spool/Code/02_Data.cs:132-175` | built #108; self-test 10c not run on the PC yet |

#### Spool: every upgrade (from `scope_upgrades.py`)

Level, cost, in-game line and Leads to were checked together. A built upgrade whose Leads to also names unbuilt upgrades is
marked MATCH here, because those links come with B19.

| Line | Building | Upgrade | Lvl | Mark | Code | Note |
| --- | --- | --- | ---: | --- | --- | --- |
| 08:67 | Yarn Stash | Roomy Pockets | 1 | UNBUILT-QUEUED | - | B19 |
| 08:71 | Yarn Stash | Quick Patch | 1 | UNBUILT-QUEUED | - | B19 |
| 08:76 | Yarn Stash | Darning | 2 | UNBUILT-QUEUED | - | B19 |
| 08:80 | Yarn Stash | Elbow Patches | 2 | UNBUILT-QUEUED | - | B19 |
| 08:85 | Yarn Stash | Remnant Bin | 3 | UNBUILT-QUEUED | - | B19 |
| 08:89 | Yarn Stash | Mending Circle | 3 | UNBUILT-QUEUED | - | B19 |
| 08:94 | Yarn Stash | Quilting Bee | 4 | UNBUILT-QUEUED | - | B19 |
| 08:166 | Cookie Tin | Reuse Reduce Recycle | 1 | MATCH | `Spool/Code/02_Data.cs:279` | leads to Tacking Stitch: B19 |
| 08:170 | Cookie Tin | Woolen Lining | 1 | MATCH | `Spool/Code/02_Data.cs:281` | leads to Tacking Stitch: B19 |
| 08:175 | Cookie Tin | Discounted Material | 2 | MATCH | `Spool/Code/02_Data.cs:283` | leads to Cross Stitch: B19 |
| 08:179 | Cookie Tin | New Design | 2 | MATCH | `Spool/Code/02_Data.cs:285` | leads to Cross Stitch: B19 |
| 08:183 | Cookie Tin | Tacking Stitch | 2 | UNBUILT-QUEUED | - | B19 |
| 08:188 | Cookie Tin | One Last Surprise | 3 | MATCH | `Spool/Code/02_Data.cs:287` | leads to Patchwork Army: B19 |
| 08:192 | Cookie Tin | Cross Stitch | 3 | UNBUILT-QUEUED | - | B19 |
| 08:197 | Cookie Tin | Patchwork Army | 4 | UNBUILT-QUEUED | - | B19 |
| 08:223 | Thread Head | Threaded Delivery | 1 | MATCH | `Spool/Code/02_Data.cs:299` | leads to level 2: B19 |
| 08:227 | Thread Head | Patchwork Job | 1 | MATCH | `Spool/Code/02_Data.cs:301` | leads to level 2: B19 |
| 08:232 | Thread Head | Big Bobbin | 2 | UNBUILT-QUEUED | - | B19 |
| 08:236 | Thread Head | Needle Point | 2 | UNBUILT-QUEUED | - | B19 |
| 08:241 | Thread Head | High Thread Count | 3 | UNBUILT-QUEUED | - | B19 |
| 08:245 | Thread Head | Stitch in Time | 3 | UNBUILT-QUEUED | - | B19 |
| 08:250 | Thread Head | Head of the Class | 4 | UNBUILT-QUEUED | - | B19 |
| 08:267 | Pin Cushion | Sharper Pins | 1 | MATCH | `Spool/Code/02_Data.cs:313` | |
| 08:271 | Pin Cushion | Slow Unravel | 1 | MATCH | `Spool/Code/02_Data.cs:315` | Spin 25 halved = Thread x2 |
| 08:276 | Pin Cushion | Pin Barrage | 2 | UNBUILT-QUEUED | `Spool/Code/02_Data.cs:317` | B18: the number, cost and line match; "Leads to: {Iron Thimble}" (08:279) waits, code `""` |
| 08:281 | Pin Cushion | Loose Stuffing | 2 | MATCH | `Spool/Code/02_Data.cs:319` | leads to Iron Thimble too (template rule): B19 |
| 08:286 | Pin Cushion | Overstuffed | 3 | MATCH | `Spool/Code/02_Data.cs:321` | Mangled 45 = 15 x3 (`:215`); leads to Voodoo Doll: B19 |
| 08:290 | Pin Cushion | Burst Seams | 3 | MATCH | `Spool/Code/02_Data.cs:323` | leads to Voodoo Doll: B19 |
| 08:294 | Pin Cushion | Iron Thimble | 3 | UNBUILT-QUEUED | - | B19 (B18 links it) |
| 08:299 | Pin Cushion | Voodoo Doll | 4 | UNBUILT-QUEUED | - | B19 |
| 08:316 | Loom | Direct Line | 1 | MATCH | `Spool/Code/02_Data.cs:335` | leads to level 2: B19 |
| 08:320 | Loom | Seam Allowance | 1 | MATCH | `Spool/Code/02_Data.cs:337` | leads to level 2: B19 |
| 08:325 | Loom | Dyed in the Wool | 2 | UNBUILT-QUEUED | - | B19 |
| 08:329 | Loom | Bobbin Winder | 2 | UNBUILT-QUEUED | - | B19 |
| 08:334 | Loom | Seam Squad | 3 | UNBUILT-QUEUED | - | B19 |
| 08:338 | Loom | Snare Line | 3 | UNBUILT-QUEUED | - | B19 |
| 08:343 | Loom | Golden Thread | 4 | UNBUILT-QUEUED | - | B19 |

Upgrades: 37; MATCH 14; UNBUILT-QUEUED 23 (22 B19, Pin Barrage's Leads to B18). The 11-05 audit marked Pin Barrage
MATCH. It counts as queued here because its Leads to is one of the checks.

Findings (DIFFERENT / MISSING / SCOPE-TEXT): none. No line contradicts a decision Dimitri has made. Two notes are stale
and are not findings. Both are Claude's `/~ \` notes, so they can wait for B19's scope pass:
- 08:266 says "Every name with ~ is a placeholder for yours". None of the names carries a tilde since he said "Use all
  defaults" (`DECISIONS_APPLIED_2026-11-05.md` §4).
- 08:13 says the towers' look "is a default now", but the value no longer carries a tilde.

### 09_Vessel_Argo.txt
Summary: rows 74; MATCH 54; RESOLVED 18; UNBUILT-QUEUED 0; UNBUILT-BY-DESIGN 1; DIFFERENT 1; MISSING 0.
All 19 of the 11-05 audit's UNBUILT-QUEUED Argo rows are now built: the slow renewal and the boss rule in PR #103 (B13,
plus the B12 Salt Sting line taken into the scope), and the fourteen review changes in PR #104 ("towards" in the Reel
line from PR #120). 18 now match; Abyss's new "+10% slow" is built but the 60% slow ceiling cuts it to +5% (finding 1).
Compiled only. None of it has run on the PC yet (scope 09:10, 09:22).

Main table (code paths in `Argo/Code/`; upgrades in the sub-table below)

| Line(s) | Says | Mark | Code | Note |
| --- | --- | --- | --- | --- |
| 09:8 | NEEDS BALANCE PASS | UNBUILT-BY-DESIGN | | his slot |
| 09:10 | review lines all built, compiled, not run | MATCH | `Argo/Code/02_Data.cs:271-417` | Claude's note; true at c197626 |
| 09:13-14 | Title Deep Sea Diver, Intermediate | MATCH | `Argo/Code/02_Data.cs:104-105` | |
| 09:15 | Archetype: no walls, control the field | MATCH | `Argo/Code/02_Data.cs:271-417` | no wall among the five builds |
| 09:16 | In-game line | MATCH | `Argo/Code/02_Data.cs:106` | |
| 09:17-20 | Look, towers look, sea blue, sprites | MATCH | `Argo/Code/02_Data.cs:119`; `Argo/Code/03_Assets.cs:66-70` | art, not judged here |
| 09:21 | Lore | MATCH | `Argo/Code/02_Data.cs:110` | |
| 09:22 | Status: 144/144, review built, not run on the PC | MATCH | | round 3: 144 (`docs/reports/CODE_REVIEW_MAX_2026-11-05.md:88`) |
| 09:25-33 | melee, Health 65, regen 0, Speed 2.25, Damage 4, Range 1.75, attack 0.4, build 2, sell 50% | MATCH | `Argo/Code/02_Data.cs:128` | regen: no stat, so 0 |
| 09:34 | Second hit 60% within 1.25 | MATCH | `Argo/Code/02_Data.cs:131-132`; `Argo/Code/06_HeroBrain.cs:211`, `:226` | |
| 09:35 | Special rule {Fathoms} | MATCH | `Argo/Code/02_Data.cs:107-108` | |
| 09:36 | One slow per source, renewed, not stacked; sources add, floor 10% | RESOLVED | `Argo/Code/01_Util.cs:319-358` | PR #103 (B13, answer 28) |
| 09:37 | Anchor Line, Kelp Bed, Trench, Mantle Tap pin; twice as long in water | RESOLVED | `Argo/Code/01_Util.cs:180-197`; `Argo/Code/11_Water.cs:191`, `:223`; `Argo/Code/07_TowerBrain.cs:858` | PR #104 |
| 09:40-41 | Scatter: close cone, debuff bonus | MATCH | `Argo/Code/02_Data.cs:279` | |
| 09:43 | Scatter's new in-game line | RESOLVED | `Argo/Code/02_Data.cs:275` | PR #104 |
| 09:44-49, 51-52 | Health 40+25, 1.25+0.25, 0.6, 4.5, 16, 6 pellets / 50 deg / pierce 1, no limit, Red 30 | MATCH | `Argo/Code/02_Data.cs:276-279` | |
| 09:50 | +50% slowed, +100% pinned | RESOLVED | `Argo/Code/02_Data.cs:279`; `Argo/Code/07_TowerBrain.cs:389-390` | PR #104; a pinned foe takes the larger only |
| 09:89-92 | Harpoon: damage or crowd-control paths | MATCH | `Argo/Code/02_Data.cs:310-327` | Leads to, sub-table |
| 09:94 | In-game line | MATCH | `Argo/Code/02_Data.cs:304` | |
| 09:95-100, 102-103 | Health 40+25, 5+2, 0.35, 6.5, 18, pierce 3, no limit, Red 35 | MATCH | `Argo/Code/02_Data.cs:305-306` | |
| 09:101 | Bosses never moved by a reel or pin | RESOLVED | `Argo/Code/01_Util.cs:168`, `:182`, `:257` | PR #103 (B13, answer 29); 600 max health (`02_Data.cs:225`) |
| 09:153-155 | Deep Water slows; FLOOD adds a joined pool (Buildoption1's keys) | MATCH | `Argo/Code/04_Content.cs:378-396`; `Argo/Code/11_Water.cs:248-288` | |
| 09:157 | In-game line | MATCH | `Argo/Code/02_Data.cs:334` | |
| 09:158-159, 162-163 | Health 30+15, slow 30% +5%, no limit, Blue 20 | MATCH | `Argo/Code/02_Data.cs:335-336` | see finding 1 for the ceiling |
| 09:160 | Pinned foes twice as long | MATCH | `Argo/Code/02_Data.cs:164`; `Argo/Code/01_Util.cs:184-185` | |
| 09:161 | FLOOD pool Blue 15 | MATCH | `Argo/Code/02_Data.cs:415` | |
| 09:201-203 | Drill: mines, AIM places the Tremor, a Fathom a Tide | MATCH | `Argo/Code/16_AimSelect.cs:8`; `Argo/Code/07_TowerBrain.cs:864-876` | |
| 09:205 | In-game line | MATCH | `Argo/Code/02_Data.cs:358` | |
| 09:206-211, 214-215 | Health 40+25, 0.5, 0.25, Range 5, ore 2, Tremor 2 / 20%, no limit, Yellow 35 | MATCH | `Argo/Code/02_Data.cs:359`, `:364`, `:156` | |
| 09:212 | Each Fathom +75% / +10% / +0.4 / +4% (review note) | RESOLVED | `Argo/Code/02_Data.cs:150-153` | the note asked for no change; PR #104 left the values as the line says |
| 09:213 | Deepest Fathom 5 | MATCH | `Argo/Code/02_Data.cs:148` | |
| 09:252-255 | Miner's Base: 2 Nauts, towers not traps, PORT and STARBOARD | MATCH | `Argo/Code/02_Data.cs:61-63`; `Argo/Code/14_CrewSelect.cs:65`, `:117-126` | |
| 09:257 | In-game line | MATCH | `Argo/Code/02_Data.cs:386` | |
| 09:258-260 | Health 50+25, 2 Nauts, limit 1 | MATCH | `Argo/Code/02_Data.cs:388`; `Argo/Code/05_Server.cs:171-176` | |
| 09:261 | Blue 20 + Yellow 15 | RESOLVED | `Argo/Code/02_Data.cs:387` | PR #104 |
| 09:298-309 | Naut: Health 20+5, range 1.5, speed 1.6, +20% / 0.5 hp/s from 1.75, 2 (3), respawn 8 | MATCH | `Argo/Code/02_Data.cs:76-80`; `Argo/Code/08_NautBrain.cs:157`, `:215` | |
| 09:312 | Directions: Deep Current, Heavy Salvo, Chum Bucket, Pressure Gauge | MATCH | `Argo/Code/02_Data.cs:177-218`; `Argo/Code/04_Content.cs:607` | PR #107; numbers belong to scope 11 |

Every upgrade (from `scope_upgrades.py` with `V=1`: level, cost, in-game line and Leads to all agree for all 37; the
Numbers were read against the code by hand)

| Scope line | Building | Upgrade | Level | Code | Mark | Note |
| --- | --- | --- | ---: | --- | --- | --- |
| 09:54 | Scatter Cannon | Wide Bore | 1 | `Argo/Code/02_Data.cs:283` | RESOLVED | PR #104: cone +10 (was +25) |
| 09:58 | Scatter Cannon | Double Barrel | 1 | `Argo/Code/02_Data.cs:285` | RESOLVED | PR #104: 3 volleys, one per 2 s idle (`07_TowerBrain.cs:210-224`) |
| 09:63 | Scatter Cannon | Grapeshot | 2 | `Argo/Code/02_Data.cs:287` | MATCH | |
| 09:67 | Scatter Cannon | Barnacle Shot | 2 | `Argo/Code/02_Data.cs:289` | MATCH | |
| 09:73 | Scatter Cannon | Pressure Rounds | 3 | `Argo/Code/02_Data.cs:291` | MATCH | |
| 09:77 | Scatter Cannon | Ironclad | 3 | `Argo/Code/02_Data.cs:296` | MATCH | Multi 1.5 = x2.5 |
| 09:83 | Scatter Cannon | Broadside | 4 | `Argo/Code/02_Data.cs:298` | MATCH | |
| 09:105 | Harpoon Gun | Long Line | 1 | `Argo/Code/02_Data.cs:310` | MATCH | |
| 09:110 | Harpoon Gun | Tether | 1 | `Argo/Code/02_Data.cs:312` | MATCH | |
| 09:116 | Harpoon Gun | Skewer | 2 | `Argo/Code/02_Data.cs:314` | MATCH | |
| 09:121 | Harpoon Gun | Anchor Line | 2 | `Argo/Code/02_Data.cs:316` | MATCH | |
| 09:126 | Harpoon Gun | Reel | 2 | `Argo/Code/02_Data.cs:318-319` | RESOLVED | PR #104: the journal line; PR #120: "towards" |
| 09:132 | Harpoon Gun | Whaler | 3 | `Argo/Code/02_Data.cs:320` | MATCH | x(1 + maxHP/200) capped at x2 (`07_TowerBrain.cs:409-413`) |
| 09:137 | Harpoon Gun | Twin Line | 3 | `Argo/Code/02_Data.cs:322` | MATCH | 0.3 s is the second harpoon's delay |
| 09:143 | Harpoon Gun | Leviathan Lance | 4 | `Argo/Code/02_Data.cs:324` | MATCH | |
| 09:147 | Harpoon Gun | Undertow | 4 | `Argo/Code/02_Data.cs:326` | MATCH | |
| 09:165 | Deep Water | Undercurrent | 1 | `Argo/Code/02_Data.cs:340` | MATCH | |
| 09:169 | Deep Water | Brine | 1 | `Argo/Code/02_Data.cs:342` | MATCH | |
| 09:175 | Deep Water | Kelp Bed | 2 | `Argo/Code/02_Data.cs:344` | RESOLVED | PR #104: a 0.5 s pin (`11_Water.cs:191`) |
| 09:179 | Deep Water | Salt Sting | 2 | `Argo/Code/02_Data.cs:346` | RESOLVED | PR #103 put answer 27 into the scope: 1 a second without Brine = `SaltBrine` 0.5 doubled (`11_Water.cs:205`) |
| 09:185 | Deep Water | Abyss | 3 | `Argo/Code/02_Data.cs:348` | DIFFERENT | PR #104 built the line and Slow_Add 0.1, but the 60% ceiling cuts it to +5% (finding 1) |
| 09:190 | Deep Water | Crushing Depth | 3 | `Argo/Code/02_Data.cs:350` | MATCH | |
| 09:195 | Deep Water | Trench | 4 | `Argo/Code/02_Data.cs:352` | RESOLVED | PR #104: 2 pools a Tide, a 1 s pin every 3 s (`11_Water.cs:217-224`, `:234-240`) |
| 09:217 | Drill Rig | Pay Dirt | 1 | `Argo/Code/02_Data.cs:368` | MATCH | |
| 09:221 | Drill Rig | Seismic | 1 | `Argo/Code/02_Data.cs:370` | RESOLVED | PR #104: Yellow 35 |
| 09:227 | Drill Rig | Deep Core | 2 | `Argo/Code/02_Data.cs:372` | MATCH | |
| 09:231 | Drill Rig | Bedrock | 2 | `Argo/Code/02_Data.cs:374` | RESOLVED | PR #104: Yellow 80 + Red 30 |
| 09:237 | Drill Rig | Gusher | 3 | `Argo/Code/02_Data.cs:376` | RESOLVED | PR #104: 4 x Fathom from Fathom 3 (`07_TowerBrain.cs:907-908`) |
| 09:241 | Drill Rig | Fault Line | 3 | `Argo/Code/02_Data.cs:378` | RESOLVED | PR #104: Yellow 300 |
| 09:247 | Drill Rig | Mantle Tap | 4 | `Argo/Code/02_Data.cs:380` | RESOLVED | PR #104: Pmulti 0.5, a 2 s pin quake every 20 s (`07_TowerBrain.cs:706-709`, `:858`) |
| 09:263 | Miner's Base | Flippers | 1 | `Argo/Code/02_Data.cs:394` | MATCH | |
| 09:267 | Miner's Base | Old Salts | 1 | `Argo/Code/02_Data.cs:396` | MATCH | values at `02_Data.cs:83` |
| 09:273 | Miner's Base | Overhaul | 2 | `Argo/Code/02_Data.cs:398` | MATCH | 0.5 + 2 = 2.5 a second |
| 09:277 | Miner's Base | Calibrate | 2 | `Argo/Code/02_Data.cs:400` | MATCH | |
| 09:283 | Miner's Base | Third Mate | 3 | `Argo/Code/02_Data.cs:402` | MATCH | |
| 09:287 | Miner's Base | Bulkhead | 3 | `Argo/Code/02_Data.cs:404` | MATCH | |
| 09:293 | Miner's Base | Full Crew | 4 | `Argo/Code/02_Data.cs:406` | MATCH | 0.2 + 0.2 = +40%; the dive loop is ARG-1, a PC check (CODE_REVIEW_MAX §5.2) |

Findings

1. DIFFERENT: Abyss's slow is cut by the ceiling. Scope 09:187 says Abyss gives "slow +10%". 09:159 gives "Slow: 30%,
   +5% per level", and nowhere does the scope set a ceiling below 09:36's "down to 10% of a foe's speed". The code is
   `Argo/Code/11_Water.cs:170` `float slow = ... Mathf.Min(ArgoData.SlowCap, ModKitUtil.Stat(lead.Master, "Slow", ...))`
   with `Argo/Code/02_Data.cs:154` `SlowCap = 0.6f; // Tremors never slow more than this`. You can only reach Abyss
   through Undercurrent and then Kelp Bed (`02_Data.cs:340`, `:344`). So an Abyss pump sits at Level 4 and its stat is
   0.30 + 3 x 0.05 + 0.10 + 0.10 = 0.65 (the Leveladd rule is in kit `docs/17` §2.1). The ceiling holds it to 0.60, so
   the "+10%" works out to +5% every time. The self-test (`99_SelfTest.cs:1505-1510`) checks the stat, not the slow
   applied, so it passes. The Tremor never reaches the ceiling on any path (at most 0.56 with Old Salts).
   Fix: ~give Deep Water its own ceiling of 0.65 (or a water-only `SlowCap`), and check the slow applied in the
   self-test~, or ~write the 60% ceiling into 09:36 and make Abyss "slow +5%"~. Owner: Dimitri's balance pass picks one;
   the build thread then makes the one-line change.

No SCOPE-TEXT findings: every review line now says "built 2026-11-05". 09:181 carries answer 27 and 09:36 answer 28.

### 10_Vessel_Uloe.txt

Summary: rows 52; MATCH 23; RESOLVED 27; UNBUILT-QUEUED 0; UNBUILT-BY-DESIGN 2; DIFFERENT 0; MISSING 0.
All 27 rows the 11-05 audit held UNBUILT-QUEUED are built: PR #105 built the 24 upgrades, the teammate Light cap 4 (B10)
and Supernova's SURGE once a Tide (B14); PR #123 set the Flood Light's mining to the swept 0.3 (S2). N10 is closed: Burning
Point and Short Circuit now lead where 10:72 and 10:127 say. Read from the code at `c197626`, nothing compiled or run here;
the scope's own Status line (10:17) says the new code is compiled and not yet run on the PC, which PLAN Part C "Now" bears out
(PC round 4 next).

All code paths in `Uloe/Code/`.

| Line(s) | Says | Mark | Code | Note |
| --- | --- | --- | --- | --- |
| 10:8-14 | Identity: Light Personified, Advanced, archetype, "Frail, single-target ranged", look, towers look, glint yellow | MATCH | `02_Data.cs:48-53` | look is temporary art (#113) |
| 10:15 | Sprites: full sheet once past drafting | UNBUILT-BY-DESIGN | - | Release 1 waits on his sprites |
| 10:16 | Lore comes once finalized | UNBUILT-BY-DESIGN | `02_Data.cs:55` | placeholder line |
| 10:17 | Status: 24 upgrades and sharing built 11-05, compiled, not run on the PC | MATCH | `02_Data.cs:197-311`; `05_Server.cs:43-45` | PLAN Part C: PC round 4 next |
| 10:20-30 | Stats: Health 30, regen 0, Speed 2.25, Damage 2, Range 3, proj 15, atk 0.5, build 2.5, sell 50%; Bright Idea | MATCH | `02_Data.cs:52-53`, `:74` | |
| 10:33-41 | Plug In: node 2 + 0.4/aura max 10, Bank 4 + 2/upgrade, within 4 tiles; range 5, cooldown 0.5, keys 6/7 | MATCH | `02_Data.cs:96-98`; `06_HeroBrain.cs:157-171`; `04_Content.cs:411-412` | |
| 10:44-48 | Light rule; Photons Illuminate and Blind; strength by source; Light added up as a stat | MATCH | `05_Server.cs:1-6`, `:355`; `02_Data.cs:103-106` | |
| 10:49 | Sources: Uloe 1 within 3, Photon 1 within 2, Plug In, Flood Light | MATCH | `02_Data.cs:83-84`; `06_HeroBrain.cs:169-171` | |
| 10:50 | Cap 12 a building | MATCH | `02_Data.cs:81`; `07_Building.cs:33` | |
| 10:51 | Sharing: teammate's Light, up to 4 a building | RESOLVED (#105, B10) | `02_Data.cs:82`; `05_Server.cs:43-45` | caps every teammate source together (Photons, Flood, relay too), per B10's by-team paths |
| 10:52 | Effects: Glass +20%/Light, Photons back 10% faster/Light, Mirror level 1/3/6, Flood mines 10% faster/Light | MATCH | `02_Data.cs:89-92`; `08_Bank.cs:25`; `10_Mirror.cs:25-31`; `07_Building.cs:317` | |
| 10:53 | Levels: 15/30/45%, 3/4/5 s, one level each, stronger replaces | MATCH | `02_Data.cs:103-106`; `05_Server.cs:329-355` | |
| 10:61-67 | Magnifying Glass: Health 30 +20, Damage 2 +0.5, 0.75, 4.5, Red 30 | MATCH | `02_Data.cs:201-204` | |
| 10:117-122 | Power Bank: Health 60 +25, 2 Photons, 8 s, Red 40 + Yellow 20 | MATCH | `02_Data.cs:228-231` | |
| 10:167-171 | Mirror: Health 100 +40, Blinds what hits it when lit, unlit nothing, Blue 20 | MATCH | `02_Data.cs:255-258`; `10_Mirror.cs:143-146` | |
| 10:216-217, 10:219-222 | Flood Light: line, Health 40 +20, Light out 2 + 0.2/aura up to 3 more, aim 6, Yellow 40 | MATCH | `02_Data.cs:282-285`, `:313`; `07_Building.cs:197-204` | |
| 10:218 | Mining: `~1 within 2.5, 0.3 a second, 10% faster per Light; no node, no Light~` | RESOLVED (#123, S2) | `02_Data.cs:285`; `07_Building.cs:317`, `:197-200` | balance-sweep tilde on purpose (N9); equals the Loom's 0.3 (`Spool/Code/02_Data.cs:332`) |
| 10:157 | Supernova: SURGE button once a Tide | RESOLVED (#105, B14) | `08_Bank.cs:15`, `:57-82`; `04_Content.cs:417` | |
| 10:263-275 | Photon: Health 8, Damage 0.5, Range 1.5, atk 0.6, Speed 1.75, 2 a Bank, 8 s, follows a directed Flood Light's line, marks level 1, leash 3.5, free | MATCH | `02_Data.cs:141`, `:147-149`; `05_Server.cs:243`; `08_Bank.cs:172-173` | |
| 10:278 | Directions: Extension Cord, Brighter Bulb, Spare Batteries, Flash Bulb | MATCH | `02_Data.cs:119-136` | numbers audited under scope 11 |

#### Uloe: every upgrade (scope_upgrades.py, level/cost/in-game line/Leads to and numbers checked by hand)

| Scope line | Building | Upgrade | Level | Code | Mark | Note |
| --- | --- | --- | ---: | --- | --- | --- |
| 10:69 | Magnifying Glass | Burning Point | 1 | `02_Data.cs:207` | MATCH | N10 closed: next `Polished3,Quick3` = 10:72 (#105) |
| 10:74 | Magnifying Glass | Wide Lens | 1 | `02_Data.cs:209` | RESOLVED (#105) | |
| 10:80 | Magnifying Glass | Polished Lens | 2 | `02_Data.cs:211` | RESOLVED (#105) | |
| 10:85 | Magnifying Glass | Quick Focus | 2 | `02_Data.cs:213` | RESOLVED (#105) | |
| 10:90 | Magnifying Glass | Prism | 2 | `02_Data.cs:215` | RESOLVED (#105) | `09_Glass.cs:161-185` |
| 10:96 | Magnifying Glass | Sunspot | 3 | `02_Data.cs:217` | RESOLVED (#105) | Light 4/8/12, `02_Data.cs:156` |
| 10:101 | Magnifying Glass | Focal Point | 3 | `02_Data.cs:219` | RESOLVED (#105) | ends, next empty |
| 10:107 | Magnifying Glass | Death Ray | 4 | `02_Data.cs:221` | RESOLVED (#105) | 3 s idle, level 3 at full heat, `09_Glass.cs:58`, `:77` |
| 10:124 | Power Bank | Short Circuit | 1 | `02_Data.cs:234` | MATCH | N10 closed: next `Static3,Surge3` = 10:127 (#105); burst `08_Bank.cs:393-394` |
| 10:129 | Power Bank | Extra Cell | 1 | `02_Data.cs:236` | RESOLVED (#105) | |
| 10:134 | Power Bank | Static Charge | 2 | `02_Data.cs:238` | RESOLVED (#105) | |
| 10:138 | Power Bank | Bright Spark | 2 | `02_Data.cs:240` | RESOLVED (#105) | |
| 10:142 | Power Bank | Surge Protector | 2 | `02_Data.cs:242` | RESOLVED (#105) | ends; `06_HeroBrain.cs:190-212` |
| 10:148 | Power Bank | Chain Lightning | 3 | `02_Data.cs:244` | RESOLVED (#105) | |
| 10:152 | Power Bank | Photon Swarm | 3 | `02_Data.cs:246` | RESOLVED (#105) | |
| 10:157 | Power Bank | Supernova | 4 | `02_Data.cs:248` | RESOLVED (#105) | `08_Bank.cs:70-82` |
| 10:173 | Mirror | Silver Backing | 1 | `02_Data.cs:261` | MATCH | |
| 10:177 | Mirror | Vanity Lights | 1 | `02_Data.cs:263` | MATCH | |
| 10:182 | Mirror | Glare | 2 | `02_Data.cs:265` | MATCH | |
| 10:186 | Mirror | Bounce Card | 2 | `02_Data.cs:267` | MATCH | never to another Bounce Card, `05_Server.cs:214` |
| 10:190 | Mirror | Tempered Glass | 2 | `02_Data.cs:269` | RESOLVED (#105) | |
| 10:195 | Mirror | Hall of Mirrors | 3 | `02_Data.cs:271` | RESOLVED (#105) | |
| 10:200 | Mirror | Reflection | 3 | `02_Data.cs:273` | RESOLVED (#105) | ends |
| 10:206 | Mirror | Heliostat | 4 | `02_Data.cs:275` | RESOLVED (#105) | `10_Mirror.cs:78` |
| 10:224 | Flood Light | Wide Flood | 1 | `02_Data.cs:288` | MATCH | |
| 10:228 | Flood Light | Long Throw | 1 | `02_Data.cs:290` | MATCH | line width 0.6 is a code default |
| 10:233 | Flood Light | Halogen Bulb | 2 | `02_Data.cs:292` | RESOLVED (#105) | |
| 10:237 | Flood Light | High Voltage | 2 | `02_Data.cs:294` | RESOLVED (#105) | |
| 10:242 | Flood Light | Colour Filter | 3 | `02_Data.cs:296` | RESOLVED (#105) | ends; trades the owner's share of each payout (`07_Building.cs:335-361`), the same as the node's total solo |
| 10:247 | Flood Light | Searchlight | 3 | `02_Data.cs:298` | RESOLVED (#105) | script says MISSING only because the `U(...)` runs onto `:299`; slow now through `SlowOn` (`07_Building.cs:458`) |
| 10:252 | Flood Light | Stadium Lights | 4 | `02_Data.cs:302` | RESOLVED (#105) | |
| 10:256 | Flood Light | Lighthouse | 4 | `02_Data.cs:305` | RESOLVED (#105) | script MISSING as Searchlight; `07_Building.cs:480-491` |

Every cost, in-game line and level (scope N = code tier N+1) agrees; every Leads to agrees with the code's `next` (none
written = every upgrade of the next level; "ends" = empty).

Findings (DIFFERENT / MISSING / SCOPE-TEXT): none.

Tool note (not a scope finding): `TDS_Mod/Tools/cloud-check/scope_upgrades.py` reports Searchlight and Lighthouse
MISSING because their `U(...)` entries (`Uloe/Code/02_Data.cs:298-299`, `:305-306`) put the cost on the next line;
`~let the script read a U(...) across lines~` (whoever next touches the script).

### 11_Directions.txt
Summary: rows 55; MATCH 16; RESOLVED 38; UNBUILT-QUEUED 0; UNBUILT-BY-DESIGN 0; DIFFERENT 1; MISSING 0.
Since 11-05: PRs #103 (B7, B15), #106 (B8 Shift panel names), #107-#110 (all 32 Claude-made Directions, the Duos and the
Wound Spring / Spring Tide clash, old N6/N7), #112-#113 (47 icons, three Shift looks) cleared every queued row. The
scope lost its five `! NEEDS ... DIRECTIONS !` lines and renamed Long Line to Spare Reel (code renamed it too, #120).
One row is new work: Tidal Mend still heals x the tower's level, which the scope's Numbers don't say.

One row per Direction sheet; name, owner, ranks I/II/III numbers, in-game line, Clashes/Duo needs and look checked
together. Look = an icon in `RogueWavesMode/Art/Temporary/game_size/Direction_<Name>.png` (all 55 present, temporary
art, #112) unless noted. Any/base Directions live in `RogueWavesMode/Code/16_Directions.cs` (sheet + in-game lines,
`:31-105`), their numbers in 16 (`:108-117`), `22_AnyDirections.cs` (`:40-66`) or `23_BaseDirections.cs` (`:18-23`).
22 and 23 are compiled, not yet run on the PC (PLAN:948).

| Line(s) | Says | Mark | Code | Note |
| --- | --- | --- | --- | --- |
| 11:9-19 | Made of Tougher Stuff: minions 15/25/35% less, rank III one hit at most 25% max health; any vessel with minions | RESOLVED | `16_Directions.cs:31`, `:112-113`, `:534`, `:556`; `:150-159` | B15 (#103): only the player's own vessel is skipped, Boutbro now reached |
| 11:20-30 | Barnacle Coat: prick 2/4/7 | MATCH | `16_Directions.cs:33-35` | |
| 11:31-41 | Tidal Mend: mends 5/8/12 within 2/2.5/3 each Tide | DIFFERENT | `16_Directions.cs:116-117`, `:611` | B7 built (#103, mode's own effect, level read each Tide), but heal = 5/8/12 x tower level; see finding 1 |
| 11:42-52 | Spring Tide: +4/+7/+10 health a Tide, rank III 10% less damage; Clashes Wound Spring | RESOLVED | `16_Directions.cs:42`, `:114-115`, `:554`, `:227` | B7 (#103) own effects; Clash "Spring" shared with Wound Spring (#109, old N7) |
| 11:53-63 | Cut Costs -10/-20/-30% | MATCH | `16_Directions.cs:44`, `:108`, `:412` | |
| 11:64-74 | Cheap Refits -10/-20/-30% | MATCH | `16_Directions.cs:46`, `:109`, `:414` | |
| 11:75-85 | Compound Interest 3/5/8%, at most 40 a colour | MATCH | `16_Directions.cs:48`, `:110-111`, `:697` | |
| 11:86-96 | Wound Spring: tower attack speed +6/12/20%; Clashes Spring Tide | RESOLVED | `16_Directions.cs:53`; `22_AnyDirections.cs:40`, `:242` | #109; reaches Tower-tagged buildings (Bouthouse left out), the rank lines say "towers" |
| 11:97-107 | Fine Sieve: 3/5/8 x Tide number of the colour held least | RESOLVED | `16_Directions.cs:55`; `22_AnyDirections.cs:41`, `:608-622` | #109 |
| 11:108-118 | Spare Hands: +1/+1/+2 units, respawn 0/-25/-40%; not Sno Sno | RESOLVED | `16_Directions.cs:57`, `:167`; `22_AnyDirections.cs:42-43`, `:288-306` | #109; also not offered to Yossele (BUILD_UNBUILT §4 reading for him) |
| 11:119-129 | Galley Kettle: +4% a hit up to 20/35/50%, 3 s, rank III +10% damage at top | RESOLVED | `16_Directions.cs:59`; `22_AnyDirections.cs:44-45`, `:196-203` | #109 |
| 11:130-140 | Chain Shot: every 4th (III: 3rd) hit jumps 1/2/3 within 2 tiles, 50% | RESOLVED | `16_Directions.cs:61`; `22_AnyDirections.cs:46-48` | #109 |
| 11:141-151 | Salt Barrel: health +15/30/45%, mends 0/0.5/1 a second | RESOLVED | `16_Directions.cs:63`; `22_AnyDirections.cs:49-50` | #109 |
| 11:152-162 | Fog Horn: -10/15/25% speed within 2/2/3 | RESOLVED | `16_Directions.cs:65`; `22_AnyDirections.cs:51-52` | #109 |
| 11:163-173 | Clam Shell: once a map (III twice) at 1 health; II-III 3 s untouchable | RESOLVED | `16_Directions.cs:67`; `22_AnyDirections.cs:53-54`, `:424-432` | #109 |
| 11:174-184 | Notched Oar: every 10th/7th/5th hit double | RESOLVED | `16_Directions.cs:69`; `22_AnyDirections.cs:55` | #109 |
| 11:185-195 | Lamp Oil: burst 15/25/35% of max health within 1/1.25/1.5 | RESOLVED | `16_Directions.cs:71`; `22_AnyDirections.cs:56-57` | #109 |
| 11:196-206 | Fish Hook: 15/25/35% snag, half the hit over 3 s, 3/3/5 bleeds | RESOLVED | `16_Directions.cs:73`; `22_AnyDirections.cs:59-62` | #109 |
| 11:207-217 | Spare Compass: 4 offered; II one redraw a map; 5 at III | RESOLVED | `16_Directions.cs:75`; `22_AnyDirections.cs:63-64`; `20_Vote.cs:267-282` | #109 |
| 11:218-228 | Magpie Nest: damage +1/1.5/2% per Trinket stack and Cardinal | RESOLVED | `16_Directions.cs:77`; `22_AnyDirections.cs:65`, `:284` | #109 |
| 11:229-239 | Fire Ship (Duo, Lamp Oil + Chain Shot): each jump bursts 10% max health within 0.75 | RESOLVED | `16_Directions.cs:79`, `:168-173`; `22_AnyDirections.cs:66`, `:470` | #109; one rank |
| 11:240-250 | Tide Pool (Duo, Tidal Mend + Spring Tide): mended past full kept, at most +20%, until the Tide ends | RESOLVED | `16_Directions.cs:81`, `:673-682` | #109; one rank |
| 11:252-262 | Deep Current: reach +15/30/50% | MATCH | `Argo/Code/02_Data.cs:178-186`; `Argo/Code/11_Water.cs:128` | |
| 11:263-273 | Heavy Salvo: Harpoon Gun damage +10/20/35% | MATCH | `Argo/Code/02_Data.cs:188-196`; `Argo/Code/07_TowerBrain.cs:286` | |
| 11:274-284 | Chum Bucket: slowed/pinned foes take +8/15/25% | RESOLVED | `Argo/Code/02_Data.cs:199-207`; `Argo/Code/09_Hooks.cs:153` | #107 |
| 11:285-295 | Pressure Gauge: Drill Rigs start 1/2/3 Fathoms down | RESOLVED | `Argo/Code/02_Data.cs:210-218`; `Argo/Code/07_TowerBrain.cs:890` | #107 |
| 11:297-307 | Compost Bin: 1 per 10 within 4; 6 within 4; 6 within 7 | RESOLVED | `Yossele/Code/02_Data.cs:102-110` | B15 (#103): rank III line now "within a much larger radius" (`:109`) |
| 11:308-318 | Dandelion Season: Sapling 70%, II +25% a stage, III +5% a Sapling up to 50%; clashes other Tree Directions | RESOLVED | `Yossele/Code/02_Data.cs:118`, `:125-137`; `Yossele/Code/04_Content.cs:648` | B8 (#106) panel reads Dandelion Sapling; look A1 (#113, `Assets/YosseleMod/Editor/YosseleModels.cs`) |
| 11:319-329 | Mother of Mushrooms: Primordial Mushboom (numbers in Mushboomkits); clashes other Tree Directions | RESOLVED | `Yossele/Code/02_Data.cs:119`, `:140-152`; `Yossele/Code/04_Content.cs:655` | B8 (#106), A1 (#113) |
| 11:330-340 | Cutting Jar: keeps 25/50/75% within 20/20/40 s | RESOLVED | `Yossele/Code/02_Data.cs:156-163`; `Yossele/Code/17_Directions.cs:209-212` | #107 |
| 11:341-351 | Sap Bucket: -15/25/35% for 2 s, III +10% from your buildings | RESOLVED | `Yossele/Code/02_Data.cs:164-172` | #107 |
| 11:352-362 | Creeping Ivy: +1 tile; II -15% on vines; III +0.5 a Tide, at most +3 | RESOLVED | `Yossele/Code/02_Data.cs:173-180`; `Yossele/Code/10_Henge.cs:53` | #107; whole tiles, so +1 tile every 2 Tides |
| 11:363-373 | Deep Breath: after 2 s, +40/70/100%, III +1 tile | RESOLVED | `Yossele/Code/02_Data.cs:181-188` | #107 |
| 11:374-384 | Fairy Ring: after 1.5 s still, 1/1.5/2% a second within 1.5/2/2.5 | RESOLVED | `Yossele/Code/02_Data.cs:189-196` | #107 |
| 11:386-396 | Efficient Recycler: Thread back 65/80/100%, III heals 10 within 2.5 | MATCH | `Spool/Code/02_Data.cs:101-111` | Unthread = sell (08:32, 08:53); the Tin's UNTHREAD button is B19 |
| 11:397-407 | Sewing Machine: mines +50%, heals 10/20/20 within 3/3/5; clashes other Loom Directions | RESOLVED | `Spool/Code/02_Data.cs:114-126`; `Spool/Code/04_Content.cs:151`, `:681` | B8 (#106) panel name; A1 (#113, `Assets/SpoolMod/Editor/SpoolModels.cs`) |
| 11:408-418 | Double Needle: stitch -15/30/30%, III every 3rd makes two | RESOLVED | `Spool/Code/02_Data.cs:132-139`; `Spool/Code/08_Tin.cs:31`, `:110` | #108 |
| 11:419-429 | Button Jar: two for one, 55/65/75%, Tin holds twice | RESOLVED | `Spool/Code/02_Data.cs:142-149`; `Spool/Code/08_Tin.cs:25`, `:128` | #108; small Crafted look #113 |
| 11:430-440 | Pattern Book: 1/2/3 a Tide per Tin, III instant | RESOLVED | `Spool/Code/02_Data.cs:151-157`; `Spool/Code/08_Tin.cs:61-70`, `:145` | #108 |
| 11:441-451 | Merit Badge: +3% a drown up to 15/30/45% | RESOLVED | `Spool/Code/02_Data.cs:159-166`; `Spool/Code/08_Tin.cs:508` | #108 |
| 11:452-462 | Odd Socks: +8/15/25% beside another kind within 2, III 10% less taken | RESOLVED | `Spool/Code/02_Data.cs:168-175`; `Spool/Code/08_Tin.cs:520-548` | #108 |
| 11:464-474 | Extension Cord: +2/+4/+6 | MATCH | `Uloe/Code/02_Data.cs:110`, `:121-124` | |
| 11:475-485 | Brighter Bulb: Light +1/+2/+3 | MATCH | `Uloe/Code/02_Data.cs:111`; `Uloe/Code/06_HeroBrain.cs:169` | |
| 11:486-496 | Spare Batteries: +1; +1 and -25%; +2 and -25% | MATCH | `Uloe/Code/02_Data.cs:112-113`, `:129-132` | |
| 11:497-507 | Flash Bulb: Illuminate 1; +Blind 1; both 2 | MATCH | `Uloe/Code/02_Data.cs:114-115`, `:133-136` | |
| 11:510-520 | Full Gallop: Buster speed +10/20/30% | MATCH | `16_Directions.cs:85` | |
| 11:521-531 | House Regulars: Fie attack speed +10/20/30% | MATCH | `16_Directions.cs:87` | |
| 11:532-542 | Spare Reel: fishing range +1/+2/+3 | MATCH | `16_Directions.cs:89` | renamed from Long Line in scope and code (#120); id and icon stay `Longline` |
| 11:543-553 | Packed Snow: speed +8/16/25% | MATCH | `16_Directions.cs:91` | |
| 11:554-564 | Skipping Stones: damage +15/30/50% | MATCH | `16_Directions.cs:93` | |
| 11:565-575 | Mason's Trowel: walls +20/40/60%, III mends 1/s after 3 s | RESOLVED | `16_Directions.cs:96`; `23_BaseDirections.cs:18-19`, `:204-212` | #110 |
| 11:576-586 | Snow Shovel: snow level 30/40/55 (game 20) | RESOLVED | `16_Directions.cs:98`; `23_BaseDirections.cs:20` | #110 |
| 11:587-597 | Worm Tin: Boutbro's costs -10/20/30% | RESOLVED | `16_Directions.cs:100`; `23_BaseDirections.cs:21`, `:239` | #110 |
| 11:598-608 | Open Sign: regulars 5/6/7 | RESOLVED | `16_Directions.cs:102`; `23_BaseDirections.cs:22`, `:251` | #110; needs the Regulars upgrade (BUILD_UNBUILT §4) |
| 11:609-619 | Long Leash: I no -1 range dashing; II/III dash hits +15/30% | RESOLVED | `16_Directions.cs:104`; `23_BaseDirections.cs:23`, `:76`, `:303` | #110 |

Findings (DIFFERENT / MISSING / SCOPE-TEXT)

1. **DIFFERENT: Tidal Mend heals x the tower's level.** Scope 11:38: "Numbers: mends 5 / 8 / 12 within 2 / 2.5 / 3
   tiles". Code `RogueWavesMode/Code/16_Directions.cs:116`: `MendHeal = { 0f, 5f, 8f, 12f }; // x the healing tower's
   level, read at each Tide`, applied at `:611` as `MendHeal[mendRank] * Mathf.Max(1, t.Entitydata.Value.Level)`. So a
   level-3 tower mends 15/24/36. Decision 14 (rebuild on the mode's own effects, a default taken) and B7 fixed the
   *frozen* level (MODE-C5). Nothing Dimitri said asked for scaling by level. The first audit (11-04 row 1) already
   asked: divide by the level, or write "x the tower's level" into Numbers. Recommended (as §2 N1): `~drop the level
   factor at 16_Directions.cs:611, so the scope's flat 5 / 8 / 12 holds~`, or `~write "x the tower's level" into 11:38~`. Take: text (scope line), or the build thread on his word; balance sweep to check it beside
   Spring Tide.

Not counted (comments only): `RogueWavesMode/Code/16_Directions.cs:28` and `Argo/Code/02_Data.cs:175` still cite scope
11's NEEDS lines, which the scope dropped in this window. `~update the comments with the next code PR~`.

### 12_Cardinals.txt
Summary: rows 34; MATCH 13; RESOLVED 21; UNBUILT-QUEUED 0; UNBUILT-BY-DESIGN 0; DIFFERENT 0; MISSING 0.
Since 11-05: B2-B4 (PR #103) built the shelf caps, the Trinket slot, Trinkets on the vessel panel, "+N" and the locked lobby; PR #111 built all 15 Claude sheets and the Punch Card / Pearl Oyster pair; PR #112 drew their icons; C1's price ramp came with PR #123; PRs #130/#132 split the stock Cardinals into 17 Trinkets and 12 Cardinals. The two NEEDS lines are gone: 12:5 is now his own value. The tildes at 12:14, 12:18 and 12:19 stay on purpose (sweep and split defaults); the code matches each one.

All paths in `RogueWavesMode/Code/17_Cardinals.cs` unless named; `24_Trinkets.cs` = `RogueWavesMode/Code/24_Trinkets.cs`.

| Line(s) | Says | Mark | Code | Note |
| --- | --- | --- | --- | --- |
| 12:5 | The shop sells every stock Cardinal except 14 named | MATCH | `:65-69` (`NotSold`; Bottom Rock is both `Artifact_Bottomrock` and `Artifact_Rock`), `:119` | was a NEEDS line on 11-05; now his value |
| 12:8 | A run modifier, the same kind as the base game's | MATCH | `:44-62`, `24_Trinkets.cs:5-41` | |
| 12:9 | Two plain words, a found object | MATCH | `:46-61` (every Display is two words) | |
| 12:10 | Bought from the shopkeeper with Pure Aura | MATCH | `:542-571`; `18_Shop.cs:234` | |
| 12:11 | Two kinds, kept apart in the shop and in the list | RESOLVED | `:36-37`, `:360-374` (2 Cardinal slots and 1 Trinket slot); `18_Shop.cs:234` (card says "Trinket" or "Cardinal") | B4, PR #103 |
| 12:12 | Trinket: the buyer's things only, shown on their vessel panel | RESOLVED | `:875-893` (`Grant`, called from `14_Run.cs:332`), `:968-991` (`Reach`) | panel is B4, PR #103 |
| 12:13 | Cardinal: core, foes or clock; in the Cardinal row; one buys it, everyone gets it | MATCH | `:625-661` | |
| 12:14 | 17 stock Cardinals sold as Trinkets; the other ~12~ stay Cardinals | MATCH | `:86-94` (`stockTable`, 17 ids), `:214-225` | default; PRs #130/#132 |
| 12:15 | Coin pays a share; Gavel fires for the buyer's units; the four companions spawn beside the buyer's vessel, one at a time | MATCH | `24_Trinkets.cs:665-678` (share = sum / players), `:714-736`; `:897-915`; `24_Trinkets.cs:683-703` | PR #132 (his answers) |
| 12:16 | A lobby pick works for everyone; the shop won't sell it | MATCH | `:321` (`StockTaken`), `:399-406`, `:516`, `:534-538` | PR #130 |
| 12:17 | No limit; past the row's 8, a +N count | RESOLVED | `:597-611` (`Overflow`), `:656`; `14_Run.cs:507` | B4, PR #103 |
| 12:18 | Six copies, then off the shelf; a stock Cardinal bought once; a stock Trinket ~once by each player~ | RESOLVED | `:32`, `:336-340`, `:363-364` (shelf skips what's maxed), `:399-406`, `:552-557` | B3, PR #103; the stock-Trinket cap is a default (PR #130) |
| 12:19 | Price ramp: +~3~ for each bought Cardinal and each Trinket copy the buyer holds; Punch Card takes 20% off the ramped price | MATCH | `:35`, `:451-471` | sweep tilde (C1, PR #123); 15 + 3 x 3 = 24 holds |
| 12:20 | Trinkets and Directions saved for each player | MATCH | `14_Run.cs:190`, `:205-206` | |
| 12:21 | No swaps on the boat | RESOLVED | `:779-786`, `:792-808`, `:814-861` | the lobby half was B2, PR #103 |
| 12:22 | Power guide | MATCH | rule for new sheets | |
| 12:23 | Pairs: a player holding one is never offered the other | RESOLVED | `:26`, `:53-54`, `:401-402`, `:409-417`, `:544-546` | B11 (N7), PR #111 |
| 12:24-26 | No copies, By, Kept names | MATCH | rules for sheets; nothing uses "Model Lighthouse" | |
| 12:30-41 | Vampire Dentures: line, 20% a stack, stacks, Red, Pure Aura 15, look | MATCH | `:33`, `:46`, `:1029-1049`; `Art/Temporary/variants/v1/game_size/Cardinal_VampireDentures.png` | the scope's line wording (text audit) equals `:46` |
| 12:42-53 | Brass Spyglass: +0.5 ranged / +0.25 melee, no stack, Red, 12 | RESOLVED | `:47`; `24_Trinkets.cs:45`, `:208-209`, `:223` | B11, PR #111; icon A2, PR #112 (same for every row below) |
| 12:54-65 | Gutting Knife: +20% a stack above half health, Red, 12 | RESOLVED | `:48`; `24_Trinkets.cs:45`, `:253-268` | |
| 12:66-77 | Oil Can: towers' attack speed +5% a stack, Red, 12 | RESOLVED | `:49`; `24_Trinkets.cs:45`, `:214-219` | |
| 12:78-89 | Tar Bucket: 1% a second a stack after 4 s, Blue, 15 | RESOLVED | `:50`; `24_Trinkets.cs:46`, `:229-249` | |
| 12:90-101 | Rubber Duck: respawn -20% a stack, at most -60%, Green, 10 | RESOLVED | `:51`; `24_Trinkets.cs:46`, `:307-349` | |
| 12:102-113 | Spare Plank: one free wall at a time, comes back; no stack; Yellow, 15; not for a vessel without a wall | RESOLVED | `:52`, `:416`; `24_Trinkets.cs:56-57`, `:366-423`, `:447-448` | |
| 12:114-125 | Punch Card: 20% off, rounded down; no stack; Yellow, 15; pairs with Pearl Oyster | RESOLVED | `:53`, `:453`, `:471`; `24_Trinkets.cs:46` | |
| 12:126-137 | Pearl Oyster: +1 each Tide until the next buy on that map; no stack; Blue, 15; pairs with Punch Card | RESOLVED | `:54`; `24_Trinkets.cs:478-485`, `:500-501`; `14_Run.cs:247` (resets each map) | |
| 12:138-149 | Bottled Storm: 5 Tides of +25% damage taken, then a Direction below rank III goes up, or Pure Aura 20; one storm at a time; Green, 15 | RESOLVED | `:55`; `24_Trinkets.cs:47-48`, `:293-294`, `:502-534`; `16_Directions.cs:275` | |
| 12:150-161 | Oilskin Coat: 10%, then a tenth of what's left for each stack; Green, 12 | RESOLVED | `:56`; `24_Trinkets.cs:272`, `:288-292` | |
| 12:162-173 | Gull Feather: +20% damage, +15% speed for 8 s after an elite drowns, extends rather than stacks; Red, 15 | RESOLVED | `:57`; `24_Trinkets.cs:193-198`, `:452-474` | elite = stock elite or `RW_Modifier_` |
| 12:175-186 | Cork Lifebuoy: one more last stand each map, after the game's own; Green, 25 | RESOLVED | `:58`; `24_Trinkets.cs:540-589` | |
| 12:187-198 | Signal Flare: every 20 s, 15 damage (+15 a stack) to the healthiest foe within 8 tiles; Red, 15 | RESOLVED | `:59`; `24_Trinkets.cs:49`, `:593-641` | |
| 12:199-210 | Cargo Crate: one more card a break for each stack, at most 6; Yellow, 20 | RESOLVED | `:60`, `:393`; `24_Trinkets.cs:50` | |
| 12:211-222 | Tin Whistle: the last 3 foes 30% slower and taking +25%; Blue, 15 | RESOLVED | `:61`; `24_Trinkets.cs:50-51`, `:743-800` | |

Findings: none.

### 13_Enemy_Modifiers.txt
Summary: rows 13; MATCH 12; RESOLVED 1; UNBUILT-QUEUED 0; UNBUILT-BY-DESIGN 0; DIFFERENT 0; MISSING 0.
B5 (PR #103) set Dazzling to 15% and made Illuminate linger 3 s; the part's header comment now names vessels (the 11-05 audit's N5 done). The only scope edit since 11-05 is the capital in Dazzling's line (13:39), which already matches the code.

All paths in `RogueWavesFoes/Code/00_RogueWavesFoesMod.cs`. `#` lines 13:2-3 are skipped.

| Line(s) | Says | Mark | Code | Note |
| --- | --- | --- | --- | --- |
| 13:8-9 | Cursed: aura; in-game line | MATCH | `:138` | |
| 13:10-11 | Vessels, towers and units: attack -25% within 3 tiles | MATCH | `:81`, `:302-314` (Tower, Mob, Hero), `:327-339` | header `:7` fixed; the Bouthouse is left out on purpose (FOES-2) |
| 13:12 | Indigo ring | MATCH | `:394` | |
| 13:18-19 | Berserk: self; in-game line | MATCH | `:139` | |
| 13:20-21 | +100% damage, +50% speed at no health | MATCH | `:82`, `:287-293` | |
| 13:22 | Hot pink ring | MATCH | `:395` | |
| 13:28-29 | Aging: self; in-game line | MATCH | `:140` | |
| 13:30-31 | -30% speed; health (scaled) and damage up to +100% over 50 s | MATCH | `:83`, `:273-275`, `:295-300` | |
| 13:32 | Barnacle blue ring | MATCH | `:396` | |
| 13:38-40 | Dazzling: aura; "Blinds and Illuminates everything nearby"; Blind and Illuminate | MATCH | `:141`, `:341-378` | |
| 13:41 | Within 3 tiles, level 1 (-15% dealt, +15% taken), lasting 3 s after leaving | RESOLVED | `:85`, `:143-145`, `:359-372` | B5, PR #103 (was 20%, no linger) |
| 13:42 | Bright yellow ring | MATCH | `:397` | |
| 13:13-15, 23-25, 33-35, 43-45 | Later maps, Look, Clashes (empty) | MATCH | nothing to match | |

Findings: none.

### 14_Optional_Objectives.txt
Summary: rows 13; MATCH 10; RESOLVED 3; UNBUILT-QUEUED 0; UNBUILT-BY-DESIGN 0; DIFFERENT 0; MISSING 0.
B15 (PR #103) set the node's hits to 12 + 3 a map; the five icons came in PR #113 and the meteor's white crystal in PR #119 (A3); P1 from the balance sweep (PR #123) moved the first objective to the break before Tide 3 in both scope and code. The NEEDS line (14:3 on 11-05) is gone.

All paths in `RogueWavesMode/Code/19_Objectives.cs` unless named.

| Line(s) | Says | Mark | Code | Note |
| --- | --- | --- | --- | --- |
| 14:5 | Names: a short order | MATCH | `:16-20` | |
| 14:6 | A new one at VENDCINT every break, from the break before Tide 3 | MATCH | `:34`, `:111-124`; `20_Vote.cs:177-181` | P1, PR #123 |
| 14:7 | Team pay: every player gets the same, full amount | MATCH | `:240-241`; `14_Run.cs:280-286` | |
| 14:8 | Like the base game's bounties | MATCH | design note | |
| 14:12-13, 15-18, 20, 22 | Mind the Meteor: Mining; line; team; offered; next Tide; nothing on failure; Pure Aura 3; no clashes | MATCH | `:16`, `:156-164`, `:168-195`, `:216-244` | the Kind is a scope label, not shown in game (the same for the four sheets below) |
| 14:14 | 12 hits + 3 a map | RESOLVED | `:278`, `:306` (`Healthresource` 11 + 3 x map = 12 + 3 x map hits) | B15, PR #103 |
| 14:14, 14:19 | About 8 tiles + 1.5 a map from the Basin; tougher and farther on later maps | MATCH | `:272-295` | |
| 14:21 | A node with a white crystal | RESOLVED | `:87-96`; `25_Models.cs:14` | A3, PR #119 (temporary art, not seen in game) |
| 14:24-32, 34 | Keep the Basin Clean: Guard; no damage to any Basin; Pure Aura 2 | MATCH | `:17`, `:176-178`, `:227`, `:262-270` | |
| 14:36-44, 46 | Beat the Clock: Rush; 25% of the clock left; Pure Aura 3 | MATCH | `:18`, `:33`, `:197-213`, `:228` | |
| 14:48-56, 58 | Mind the Vessels: Guard; no vessel falls; Pure Aura 2 | MATCH | `:19`, `:229`, `:247-256` | |
| 14:60-68, 70 | Raze the Nest: Hunt; 3-7 tiles from an enemy core; never on Nesting Ground; Pure Aura 4 | MATCH | `:20`, `:120`, `:312-315`; `07_Weather.cs:362-372` | |
| 14:33, 45, 57, 69 | Looks: mop and bucket, cracked stopwatch, life ring, burning nest | RESOLVED | `:144-154` (`RogueWaves_Objective_<Name>`); `Art/Temporary/game_size/RogueWaves_Objective_*.png` | A3, PR #113 (temporary icons) |

Findings: none.

### 15_Templates.txt
Summary: rows 9; MATCH 9; RESOLVED 0; UNBUILT-QUEUED 0; UNBUILT-BY-DESIGN 0; DIFFERENT 0; MISSING 0.
Only the template lines that state a rule the code must follow were checked, as in the earlier audits. The upgrade rules at 15:12-13 (Leads to, no numbers in the in-game line) are checked per vessel in 07-10. One line of template text is stale against a decision (SCOPE-TEXT below).

| Line(s) | Says | Mark | Code | Note |
| --- | --- | --- | --- | --- |
| 15:51 | Every new vessel's ability uses Buster's Dash keys, 6 and 7 | MATCH | `Yossele/Code/04_Content.cs:367`, `Spool/Code/04_Content.cs:393`, `Uloe/Code/04_Content.cs:394`, `Argo/Code/04_Content.cs:317` | |
| 15:144 | A Duo comes only to a player holding both, and has one rank | MATCH | `RogueWavesMode/Code/16_Directions.cs:17`, `:79-81`, `:168-173` | |
| 15:151 | A Direction's Look shows on its pick card | MATCH | `RogueWavesMode/Code/16_Directions.cs:285-291`; `20_Vote.cs:627`; `18_Shop.cs:247` | |
| 15:172 | Trinket: the buyer's things only; Cardinal: core, foes or clock, for everyone | MATCH | as 12:12-13 | |
| 15:176 | Stacks: "Yes if a player can own more than one, with no limit" | MATCH | `17_Cardinals.cs:32`, `:336-340` (6 copies, as 12:18) | the code follows 12:18; the template text is stale: SCOPE-TEXT 1 |
| 15:178 | Colour: its lobby slot; a shop card's colour says whether the buyer can pay | MATCH | `17_Cardinals.cs:185`; `18_Shop.cs:234` | |
| 15:205 | Taken by the team: every player earns the full pay | MATCH | as 14:7 | |
| 15:229 | A modifier shows as an effect, not a new skin | MATCH | `RogueWavesFoes/Code/00_RogueWavesFoesMod.cs:381-383`, `:474-490` | |
| 15:245 | Weather line shows in the boat's map panel; the map's start announces only the name | MATCH | `RogueWavesMode/Code/05_Boat.cs:113`; `07_Weather.cs:264` | |

Findings:
- **SCOPE-TEXT 1.** 15:176 (Cardinal or Trinket template, Stacks slot) says "Yes if a player (or the team) can own more than one, with no limit". Dimitri kept the caps: `docs/reports/DECISIONS_APPLIED_2026-11-05.md` row 3 ("Caps kept: a new one stacks to 6 copies, a base game Cardinal is bought once; both then leave the shop"; asked in `OPEN_DECISIONS_2026-11-05.md` item 3). 12:18 says the same, and the code makes 6 copies (`RogueWavesMode/Code/17_Cardinals.cs:32`, `:336-340`). Fixed in this PR (§2 N5): "Yes if a player (or the team) can own more than one, up to 6 copies; no if it leaves the shop once owned".

### 16_Base_Vessels.txt
Summary: rows 4; MATCH 4; RESOLVED 0; UNBUILT-QUEUED 0; UNBUILT-BY-DESIGN 0; DIFFERENT 0; MISSING 0.
Only the four pack rows of the side-by-side table changed since 99b9f3d: the tildes came off 16:16, 16:18 and 16:19 (N4). Every number still equals its vessel's `HeroStats` and `Displayskill`. The attack type is ranged exactly when the vessel has a `Projspeed`.

| Line(s) | Says | Mark | Code | Note |
| --- | --- | --- | --- | --- |
| 16:16 | Argo: Intermediate, melee, 65, 2.25, 4, 1.75, 0.4, 2, 0.5 | MATCH | `Argo/Code/02_Data.cs:105`, `:126-129` | tildes off (N4) |
| 16:17 | Yossele: Intermediate, ranged, 70, 1.75, 5.5, 4, 0.5, 2.5, 0.5 | MATCH | `Yossele/Code/02_Data.cs:69`, `:83-86` | |
| 16:18 | Spool: Advanced, melee, 45, 2.75, 2.5, 1.5, 0.6, 2.5, 0.5 | MATCH | `Spool/Code/02_Data.cs:56`, `:76-79` | tildes off (N4) |
| 16:19 | Uloe: Advanced, ranged, 30, 2.25, 2, 3, 0.5, 2.5, 0.5 | MATCH | `Uloe/Code/02_Data.cs:50`, `:72-75` | tildes off (N4) |

Findings: none.

Counts, 12-16: rows 73; MATCH 48; RESOLVED 25; UNBUILT-QUEUED 0; UNBUILT-BY-DESIGN 0; DIFFERENT 0; MISSING 0; SCOPE-TEXT 1.
