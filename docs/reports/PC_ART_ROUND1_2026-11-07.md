# PC round Art_round1 (2026-11-07): ChatGPT's art on the PC, the Thread Heads' fix, a mode flow regression

Dimitri, 2026-11-07 20:13Z: "Take the files and replace the assets with the new updated art. Afterwards, run tests."
The art came in by PR #173 (merged `d8cddbd`); this round ran it on his PC, with what rounds 5 and 6 left
(`PC_ROUND6_2026-11-06.md` §6). Steps: `docs/skills/pc-round.md`. Ran 20:30Z-22:25Z on the PC (Remote Control).

**Code.** Handoff `Art_round1` at `1d7cc8a` (= PR #173's merged code, `d8cddbd`; packed `--since 03f807a`), kit 1.15.
Apply's check stopped once: `DIFFERS ModSource\RogueWavesMode\Code\94_DialogLook.cs` (the PC copy lacked round 6's
test-only `WaitForSecondsRealtime` line, which is in the repository); the PC copy was backed up
(`handoff\backup_Art_round1_20261107_153016`), made equal to the handoff's, then `Apply -Apply`: 341 files (backup
`handoff\backup_Art_round1_20261107_153124`), `pc_manifest.txt` 1305 files. That file is `#if ROGUEWAVESMODE_DIALOGLOOK`
only, so no build of this round carried it.

## 1. In short

- **The new art is in every build.** Each self-test build's `build_log.txt`: Yossele 13/13 sheets "from Art/ (final)",
  Uloe 14/14, Argo 17/17 (`ArgoBuilder.FileArt`, new in #173, works), Spool 19/20 (the 20th, `Spool_Tuft`, is drawn
  by `11_Client.cs` rows, not a builder sheet, as intended); "kept the code's art" nowhere. Release builds the same.
- **Every vessel's self-test passes** (§2). Spool's one fail alone passed in both runs with the pack: chance.
- **The Thread Heads' fix (#165) is proven with the whole pack**: Spool with the five others 232/0/6, the Thread Head's
  MOVE hidden.
- **The mode's flow regressed**: twice 112/11 and 121/2, both stopped at C3 on map 1 (round 5's flows: 187/0 and 190/0
  on `7e7f4e4`). Two faults repeat (§3). Not fixed here; Dimitri's word first.

## 2. Self-tests

| Run | Build | Result | Log stamp |
| --- | --- | --- | --- |
| Yossele alone | SELFTEST, SHOTS | 206 / 0 / 0 | `Yossele_20261107_154414` |
| Spool alone | SELFTEST, SHOTS | 232 / 1 / 5 | `Spool_20261107_155551` |
| Uloe alone | SELFTEST, SHOTS | 220 / 0 / 3 | `Uloe_20261107_160153` |
| Argo alone | SELFTEST, SHOTS | 162 / 0 / 0 | `Argo_20261107_160605` |
| Foes | SELFTEST | 13 / 0 / 0 | `RogueWavesFoes_20261107_160736` |
| Spool with the pack (12 min default) | others release | 215 / 0 / 4, no DONE (timed out) | `Spool_20261107_162257` |
| Spool with the pack (`-TimeoutMinutes 20`) | others release | 232 / 0 / 6 | `Spool_20261107_163859` |
| Mode flow, run 1 | `-Also` the five release builds, 30 min | 112 / 11 / 0, DONE early at C3 | `RogueWavesMode_20261107_165955` |
| Mode flow, run 2 | the same, save restored first | 121 / 2 / 0, DONE early at C3 | `RogueWavesMode_20261107_171452` |

- **Spool alone, the fail:** "every 25 damage spins a Thread beside it: spun 1 after 30, beside it 0, dropped anywhere
  1". Both pack runs pass it ("beside it 1"): where the Thread lands is chance.
- **Setups:** Spool's BEE, Voodoo Doll's next Tide and Snare Line all lose their map, "BASIN DROWNED" at Wave 5, in
  every Spool run; Loom ore and Mending Circle as before. Uloe's three as in round 6. Argo's steep Harpoon shot skipped.
- **Thread Head (#165), both pack runs:** "its stock MOVE has no button but stays for the walk (B16)" PASS, hidden 1x,
  controllable False.
- Exception lines: the game's own (`docs/08` §6): OperationCanceled, TaskCanceled and NullReference lines, 3-27 a launch.

## 3. The mode's flow regression

Both runs, the same two faults:

- **B1 staying vote:** the vote's subtitle reads the map card's text, `SKYHOOK D . Calm waters . Danger 4 . Leads on to
  Purlest (4)`, not "When the party leaves, it sails to the map most of you pick" (`20_Vote.cs` l.911; passed on 6 Nov).
- **C3 map 1:** the boat never sails to `RW_Node_1_0` by itself: "not there after 60 s, boat at -30,0,32.02, hovering
  RW_Node_1_0, prev RW_Node_1_0, preview RW_Node_1_0, goto -, speed 0, setup False, transition False, selected ''"
  (6 Nov: 3.56 s). The test ends there, so the weather section never ran.

Run 1 only: a game NullReferenceException in `Bla_Ui_Confirmation.Runprompt` from `Tds_Pausemenu.Exitgamehost` (B4's
exit prompt never opened; its picture shows SETTINGS), then Bonus Tides, Leave x2, B1 leaving, T4, U3, Retries and
Continue failed after it. Run 2 passes B4 ("Leaving now loses this map: 2 retries left"): chance.

**Where to look first.** The last good flow ran on `7e7f4e4`. The mode's code since: `11_Dialog.cs` (#163, the map
cards keep their name and badge, 128 lines), `99_SelfTest.cs` and `93_TwoPlayers.cs` (#163's checks), `94_*` (test-only,
`DIALOGLOOK`), then #173's `08_Icons.cs` (weather icons from files) and `12_Menu.cs` (the menu button's picture). A
dialog that reuses its cards' name and badge fits a subtitle that shows a card's text, and a vote whose pick never
reaches `Macro_Main.Selected` fits a boat that never sails; #173's two changes touch neither path. Not yet proven.

## 4. Readings and pictures

Pictures in `Desktop\RogueWave\Art_round1` on the PC: `<Part>\` from each vessel's SHOTS run (each vessel on a map,
the boat, journal pages, HUDs), `Spool_with_pack\`, `Mode_flow\` and `Mode_flow_run2\` (31 each: the main menu with the
new button, the shop with the new icons, VENDCINT falling, landed and its panel, the break after Tide 1 with the side
icons, the exit warning, the map cards).

## 5. For Dimitri

- Whether to fix the flow regression (§3) now, in a new PR with its own PC round. It blocks any release that carries
  the mode.
- The pictures above: the new art in game for the first time, beside `ART_REVIEW_CHATGPT_2026-11-07.md`.

## 6. Not run

- The warning popup with a weather icon and the weather with foes on camera: the flow stops at C3 before its weather
  section, both runs.
- COLOUR's panel: Spool's `hud_colour_red` picture is under "BASIN DROWNED" again (every Spool map lost at Wave 5).
- A Thread Head panel picture: `SPOOL_SHOTS` takes none; the check line above stands for it.
- The gaps launches and two players: not on this round's list.

## 7. The PC as left

- TDS closed. `TDS\Mods`: his six release builds from before the round, from
  `handoff\parked\release_before_Art_round1_20261107`, 30 files hash-identical, all enabled (a duplicate copy in
  `handoff\parked\dup_copy_Art_round1`). The new-art builds are NOT installed: they are in
  `handoff\Art_round1\test_builds_left` (release Yossele, Uloe, Argo, Foes, Spool; the mode's SELFTEST+SHOTS).
- His save restored from `handoff\save_backup_20261107_153633` (0 mismatches, no extra files).
- `TDS_Mod` = Art_round1 applied; the Unity project's prefabs hold the mode's SELFTEST+SHOTS build and the others'
  release builds; Unity still open. Summaries and map files: `handoff\Art_round1\results`.
- Scope copy `Desktop\RogueWave\scope`: 16 of 17 already the repository's text (line ends only); `09_Vessel_Argo.txt`
  brought to the repository's (Argo's new look). Backup `handoff\backup_scope_before_Art_round1_20261107`.
- PC lock released (project memory).
