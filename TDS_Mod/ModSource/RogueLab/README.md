# RogueLab (research build, never released)

The throwaway mod of the Phase 1 PC round (`TDS_Vessel_Creator/docs/15_ROGUE_MODE_ENGINE.md` §11). It answers the
chapter's "untested" lines by doing each thing once on a solo host and logging one line per question:
`[RogueLab] TEST PASS|FAIL|SETUP|INFO ...`, plus raw `TIDE`, `GROUP`, `ELITE` and `ENEMY` lines, then `TEST DONE`.

| When | What it does | docs/15 |
| --- | --- | --- |
| Boot | dumps the database folders, `Mode_Normal`'s Tide table and the enemy groups; registers `Mode_RogueLab` (stock scripts by name, 3 Tides) with a `RogueLab_` twin of every `Mode_Normal` Gameplan, and the Cardinal `Artifact_RogueLab`; writes and reads a save key | §2, §7, §8, §11.9 |
| Boat | picks Boutman and the test map, equips the mod Cardinal in its colour slot, swaps the match to the twin Gameplan | §3, §7 |
| Map 1 | checks the mode and Cardinal, grants a core upgrade mid-match, reproduces the F-key bug with Boutbro and checks the fix, rewinds a Tide, leaves between Tides as a win, chains straight into map 2 | §3, §5, §7, §9 |
| Map 2 | checks what survived the scene change, opens a 3-option `Runprompt` for 30 s (round 3: the eight-card dialog instead) | §3, §6 |
| Round 2 (0.2.0, `06_Round2.cs`) | at boot every map's and Gameplan's data, every other mode's Tide table, the elites again, the easier setting's upgrade; in map 1 the bundle's second prefab (`Second/RogueLabSecond.cs`, built into `RogueLabMod_Second_Default`) and the frame cost of 50/100/200 extra units; in map 2 an eight-card dialog; then a **tour** of every other base map the vote could offer, each left as a win as soon as it starts; `TEST DONE` | `docs/RESEARCH_GAPS.md` A3, A4, A6, A7, A9, B5, C1, C2 |
| Round 3 (0.3.0) | the same, with two changes after round 2 (`docs/15` §15): map 2's dialog opens only once its menu is up, then the mod clicks a card; the tour plays only the 15 maps whose stock Gameplan uses another mode (now twinned too), the Final map last; Special_ plans are never twinned, and a map that hangs while loading ends the run after 150 s | `docs/RESEARCH_GAPS.md` A4, A6 |
| Round 4 (0.4.0) | starts from a cloned main menu button with no Cardinal (M1-M5, M7), then map 1's mechanic probes V1, V2, F1, O1 in the opening break and K1, D1, A1, E1 in Tide 1; leaves as a win and stops | `docs/mode/MAIN_MENU_START.md` §5, `docs/reports/MECHANICS_CHECK.md` §3 |
| Round 5 (0.4.1) | round 4 again with its probe bugs fixed | `docs/reports/MECHANICS_CHECK.md` §3 |
| Round 6 (0.4.2) | round 5 again with the menu probes fixed, plus M4b | `docs/mode/MAIN_MENU_START.md` §5 |
| Round 7 (0.5.x, `09_Round7.cs`) | the stock Cardinals' core upgrades at boot; in map 1 price scales, summed percentages, a −3 upgrade on three buildings, the Bout Way's prices, a re-routed path, unit counts on the Hut, Keep and Garage, a den and an ore node spawned mid-match, the `Sno` tag grab, and four timed respawns | kit `docs/17` §12, `docs/16` §9 |

Compiled here (`check_mod.py RogueLab/Code Shared RogueLab "ROGUELAB_SELFTEST"`, 0 errors). Round 1 ran on the PC on
2026-10-08: 13 pass, 0 fail; results in `docs/15` §11, every line in `TDS_Vessel_Creator/reference/roguelab_round1.txt`.
Round 2 ran on 2026-10-09: 15 pass, 1 fail (the dialog), 28 tour maps, then a hang on Special_Custom; results in
`docs/15` §15, every line in `TDS_Vessel_Creator/reference/roguelab_round2.txt`. Round 3 ran the same day: **16 pass,
0 fail**, the eight-card dialog worked with its menu up, all 15 tour maps loaded; lines in `roguelab_round3.txt`.
Rounds 4-6 (2026-10-09) checked the scope's new mechanics and the main-menu button: 11 pass, 5 fail, 1 setup; then
20 pass, 2 fail; then **22 pass, 0 fail** (the failures were probe bugs; `docs/reports/MECHANICS_CHECK.md` §3).
RogueLab's research is done; it stays in the repository as the record and is never released.

## Round 7 (2026-10-12)

RogueLab 0.5.x (`09_Round7.cs`, `RogueLabData.Round = 7`) checks kit `docs/17` §12 (the base vessels) and `docs/16` §9
items 1-3 (maps). It starts with Play and no Cardinal, plays Boutman on 3B_Thehunt, and loads the other four vessels'
groups to place their buildings for the player. At boot it dumps every Cardinal's core upgrades (`CARD` lines).
`RogueLabData.Rerun` (0.5.1 and 0.5.2) reruns only the probes a launch left open. It ran on the PC from this folder
(no handoff) in three launches: 24 pass, 2 fail, 1 setup; then 7/2; then 7/0. Every failure was the probe's own;
`docs/reports/BASE_VESSEL_CHECKS_2026-10-12.md` has the results, `TDS_Vessel_Creator/reference/roguelab_round7.txt` every line.
`Run-SelfTests.ps1 -Vessels RogueLab -TimeoutMinutes 25`; the round takes about 3 minutes. **Afterwards remove
`RogueLabMod` from the game's Mods folder.**

## Round 6 (2026-10-09)

Ran 2026-10-09: 22 pass, 0 fail, 0 setup.

RogueLab 0.4.2: round 5 again with its two menu probe bugs fixed (M1 judged against Play's state, M3's result read
once so M4 keeps its flag) and a new M4b line. Same steps with handoff `RogueLab_round6`. **Afterwards remove
`RogueLabMod` from the game's Mods folder.**

## Round 5 (2026-10-09)

Ran 2026-10-09: 20 pass, 2 fail, 0 setup; every mechanic probe passed, M1 and M4 failed on probe bugs
(`docs/reports/MECHANICS_CHECK.md` §3).

RogueLab 0.4.1: round 4's probes again, with round 4's four probe bugs fixed (the menu wait, the button name, the heal
data, the leftover Cardinal; `docs/reports/MECHANICS_CHECK.md` §3). Same steps as round 4 with handoff `RogueLab_round5`.
**Afterwards remove `RogueLabMod` from the game's Mods folder.**

## Round 4 (2026-10-09)

Ran 2026-10-09: 11 pass, 5 fail, 1 setup, all failures the probe's own (`docs/reports/MECHANICS_CHECK.md` §3).

RogueLab 0.4.0 (`07_Round4Menu.cs`, `08_Round4.cs`, `RogueLabData.Round = 4`) checks two things in one short launch:
the main menu start (probes M1-M5 and M7 of `docs/mode/MAIN_MENU_START.md` §5: the run starts from a cloned "Rogue Waves"
button and its New / Continue / Back dialog, and the boat takes the mod's mode with no Cardinal) and the mechanics of
the 2026-10-09 scope that only the game can answer (`docs/reports/MECHANICS_CHECK.md` §3: a vessel button added mid-match, a
neutral shop unit with a fall-in, Fund Transfer's moves, a minion's owner, an enemy's death data, the vessel's damage
count, Aging's stat changes, a look on an enemy). Map 1 only: no Cardinal grant, F key, swarm, map 2 or tour.

Same steps as round 1 below, with handoff `RogueLab_round4` and `Run-SelfTests.ps1 -Vessels RogueLab -TimeoutMinutes
15`. Nothing needs a click. Raw lines to send back: every `[RogueLab] TEST` line from `main menu` on (the boot dumps
repeat round 2's and can be skipped). **Afterwards remove `RogueLabMod` from the game's Mods folder.**

## Round 3 (2026-10-09)

Same steps as round 1 below, with handoff `RogueLab_round3` and `Run-SelfTests.ps1 -Vessels RogueLab -TimeoutMinutes
40`: maps 1 and 2 as before, then 15 tour maps of about a minute each. Nothing needs a click. Raw lines to send back:
`TOUR` and every `TEST` line (the boot dumps repeat round 2's and can be skipped), plus any `Runprompt` exception in
the log. **Afterwards remove `RogueLabMod` from the game's Mods folder** (kept empty until the pack ships).

## Round 2 (2026-10-09)

Same steps as round 1 below, with handoff `RogueLab_round2` and `Run-SelfTests.ps1 -Vessels RogueLab -TimeoutMinutes
75`: the tour plays about forty maps, each about a minute. Nothing needs a click. The save-restart check (A7) reads the
launch counter round 1 left in the save, so run it on the same save slot. Raw lines to send back: `MAP`, `PLAN`, `MODE`,
`TIDE2`, `TOUR`, `ELITE`, `GROUP`, plus every `TEST` line. Each tour map ends as a win, so the end screen writes a
record under each `RogueLab_` twin's name in the save (harmless; the save is backed up first). **Afterwards remove
`RogueLabMod` from the game's Mods folder**: the folder is kept empty until the pack ships (2026-10-08).

## PC round

1. Fetch and apply handoff `RogueLab_round1` from branch `claude/phase-1-6lfnar` (`TDS_Mod/Tools/pc/README.md`).
2. `Build-RogueLabMod.ps1 -Define ROGUELAB_SELFTEST` (always with the define: without it the code stops on `#error`).
3. `handoff\queue.txt` = one line `RogueLab`, then Tools > ModKit > Build Queue once, until `QUEUE DONE`.
4. `Run-SelfTests.ps1 -Vessels RogueLab -TimeoutMinutes 20`. The game's Mods folder is otherwise empty (kept so between rounds); disable any other mod there first if
   they are enabled; the runner only toggles the mods it is given.
5. Send back the summary **and every `[RogueLab]` line** of the copied log in `handoff\selftest_logs`
   (`Select-String -Path <log> -Pattern '\[RogueLab\]'` into a file): the summary keeps only PASS/FAIL/SETUP lines.
6. Tools > RogueLab > Restore Parked Mods if the queue didn't, then disable `RogueLabMod` in the game.

A run that crashes before `TEST DONE` can leave the save key `Unlock/Unlock_Artifact_Artifact_RogueLab`: harmless, it
names an artifact that exists only while RogueLab is loaded. Steps 10 and 11 of §11 (online, by hand) are not here.
