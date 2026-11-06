# Handoff Full_round1 (2026-11-06)

Commit `8336f21` (`HEAD`). Made by `TDS_Mod/Tools/pc/make_handoff.py`; the folders mirror `C:\Users\Dimitri\TDS_Mod`.

Full test of the newest build: base 6830ba2 (chart, MVP board, wobble fix, pitfall fixes) plus the new gaps parts stock and decided; list docs/reports/PC_FULL_2026-11-06.md section 2

`Apply-Handoff.ps1` (check only) should list:

- will copy: `ModSource\Argo\Code\01_Util.cs`
- will copy: `ModSource\Argo\Code\02_Data.cs`
- will copy: `ModSource\Argo\Code\11_Water.cs`
- will copy: `ModSource\Argo\Code\99_SelfTest.cs`
- will copy: `ModSource\Argo\DESIGN.md`
- will copy: `ModSource\Argo\IMPLEMENTATION_NOTES.md`
- will copy: `ModSource\RogueWavesMode\Code\00_RogueWavesModeMod.cs`
- will copy: `ModSource\RogueWavesMode\Code\06_Campaign.cs`
- will copy: `ModSource\RogueWavesMode\Code\09_Rejoin.cs`
- will copy: `ModSource\RogueWavesMode\Code\14_Run.cs`
- will copy: `ModSource\RogueWavesMode\Code\15_Content.cs`
- will copy: `ModSource\RogueWavesMode\Code\17_Cardinals.cs`
- will copy: `ModSource\RogueWavesMode\Code\18_Shop.cs`
- will copy: `ModSource\RogueWavesMode\Code\20_Vote.cs`
- will copy: `ModSource\RogueWavesMode\Code\26_ChartView.cs`
- will add: `ModSource\RogueWavesMode\Code\27_Board.cs`
- will add: `ModSource\RogueWavesMode\Code\28_BoardView.cs`
- will add: `ModSource\RogueWavesMode\Code\90_Board.cs`
- will copy: `ModSource\RogueWavesMode\Code\90_Stops.cs`
- will copy: `ModSource\RogueWavesMode\Code\92_Gaps.cs`
- will add: `ModSource\RogueWavesMode\Code\92_GapsDecided.cs`
- will add: `ModSource\RogueWavesMode\Code\92_GapsStock.cs`
- will copy: `ModSource\RogueWavesMode\Code\93_TwoPlayers.cs`
- will copy: `ModSource\RogueWavesMode\Code\99_SelfTest.cs`
- will copy: `ModSource\RogueWavesMode\IMPLEMENTATION_NOTES.md`
- will copy: `ModSource\Spool\Code\05_Server.cs`
- will copy: `ModSource\Spool\Code\06_HeroBrain.cs`
- will copy: `ModSource\Spool\Code\07_Building.cs`
- will copy: `ModSource\Spool\Code\99_SelfTest.cs`
- will copy: `ModSource\Spool\IMPLEMENTATION_NOTES.md`
- will copy: `ModSource\Uloe\Code\02_Data.cs`
- will copy: `ModSource\Uloe\Code\06_HeroBrain.cs`
- will copy: `ModSource\Uloe\Code\98_Upgrades.cs`
- will copy: `ModSource\Uloe\Code\99_SelfTest.cs`
- will copy: `ModSource\Uloe\DESIGN.md`
- will copy: `ModSource\Uloe\IMPLEMENTATION_NOTES.md`

On the PC only (left alone): `ModSource\Archive\Argo_1.0\11_World.cs`, `ModSource\Argo\build_log.txt`, `ModSource\RogueLab\build_log.txt`, `ModSource\RogueWavesFoes\build_log.txt`, `ModSource\RogueWavesMode\Art\Temporary\game_size\Cardinal_Bell.png`, `ModSource\RogueWavesMode\Art\Temporary\game_size\Cardinal_Clock.png`, `ModSource\RogueWavesMode\Art\Temporary\game_size\Cardinal_Hull.png`, `ModSource\RogueWavesMode\Art\Temporary\game_size\Cardinal_Legs.png`, `ModSource\RogueWavesMode\Art\Temporary\game_size\Cardinal_Puffer.png`, `ModSource\RogueWavesMode\Art\Temporary\game_size\Cardinal_Rising.png`, `ModSource\RogueWavesMode\Art\Temporary\game_size\Cardinal_Wind.png`, `ModSource\RogueWavesMode\build_log.txt`, `ModSource\Spool\build_log.txt`, `ModSource\Uloe\build_log.txt`, `ModSource\Yossele\build_log.txt`

## Steps

1. Only one session works in TDS_Mod at a time (`pc/QUEUE.md` in the Claude Project's files).
2. `Apply-Handoff.ps1` (check only). Stop and report on DIFFERS (the PC has edits that aren't on GitHub), MISSING, a damaged copy, a patch that doesn't fit, or a non-zero exit.
3. `Apply-Handoff.ps1 -Apply`, once Dimitri has OK'd it in the thread in his own words. It backs up what it replaces
   and writes a fresh `handoff\pc_manifest.txt`.
4. Build, test and release as the thread's brief says (`TDS_Mod/Tools/pc/README.md`).

## This round (thread "Run full test on latest build")

Dimitri's OK, in this thread: "Once the chart and MVP board are compiled and created, run a full test using the most
up to date version." (2026-11-06 07:00Z). It covers the whole round below, nothing more: no release is left in his
Mods folder and his scope copy is left alone. The code is base `6830ba2` (the sea chart, the MVP board, the wobble fix,
the pitfall fixes) plus two new gaps parts (`-rwonly stock`, `-rwonly decided`). Every check's source is in
`docs/reports/PC_FULL_2026-11-06.md` §2 (ids S, G, T, P below). `<h>` = `Desktop\ClaudeProjects\handoff`.

General: send every compile error in full. Note the time each launch takes. If a launch hangs past its timeout, kill
TDS, note it, and go on to the next item. Pictures stay on the PC (they hold game art).

### 0. Before

1. TDS must be closed (if `TDSgame` runs, stop and say so). Unity 2022.3.41f1 open on `C:\Users\Dimitri\TDS_Mod` with
   its MCP answering; if not, open it, or stop and say so.
2. Copy all six folders in `D:\Steam\steamapps\common\TDS\Mods` to `<h>\parked\release_before_Full_round1_20261106`
   and list each file's hash (they should be the 2a3d907 release builds the chart round left).
3. `Get-Handoff.ps1 -Ref claude/project-thread-jry4d6 -Name Full_round1`, then `<h>\Full_round1\Apply-Handoff.ps1`
   (check only: 36 files, "will copy"/"will add" only; stop on DIFFERS, MISSING, a damaged copy or a non-zero exit),
   then `Apply-Handoff.ps1 -Apply`. Unity `execute_menu_item("Assets/Refresh")`; wait until it has compiled.

### 1. Self-tests (S1, S2)

4. Self-test builds of all six: `Build-ArgoMod.ps1 -Define ARGO_SELFTEST`, the same for Yossele, Spool, Uloe and
   RogueWavesFoes, and `Build-RogueWavesModeMod.ps1 -Define ROGUEWAVESMODE_SELFTEST,ROGUEWAVESMODE_SHOTS`. `<h>\queue.txt`
   one part per line (with ` twice` where round 4 used it); Tools > ModKit > Build Queue once; read the `[ModKit]`
   lines in `%LOCALAPPDATA%\Unity\Editor\Editor.log`. Each part's log line names kit 1.14.
5. **S1:** `Run-SelfTests.ps1 -Vessels Argo,Yossele,Spool,Uloe,RogueWavesFoes`. **Note the path of the save backup it
   makes: it is his save before the round.** Send each summary in full, and every FAIL and SETUP line with 3 lines
   around it. Also the lines holding `Chum Bucket`, `Abyss`, `Deep Water slows`, `Stash`, `PATCH`, `Plug In`,
   `Searchlight`, `Reflection`.
6. **S2:** `Run-SelfTests.ps1 -Vessels RogueWavesMode -Also Argo,Yossele,Spool,Uloe,RogueWavesFoes -TimeoutMinutes 30`.
   While it runs, save a full-screen capture every 3 s (PowerShell `System.Windows.Forms.Screen` +
   `Graphics.CopyFromScreen`, PNG) into `Desktop\RogueWave\full_round1\frames`. Send the summary in full and, from the
   kept log, every line holding `CV`, `P1`, `P2`, `MB`, `chart view`, `Cove`, `Wreck`, `Spring Tide`, `U3`, `R4`,
   `Leave`, `MAP 1 OF 10`, `map 1 for`, `map 2 for`, `map 6 for`, `TEST DONE`, plus every FAIL and SETUP line with 3
   lines around it. Pictures: keep in `Desktop\RogueWave\full_round1` only the frames showing (a) the main menu at rest,
   (b) the after-win WHERE NEXT? at least 6 s after it appears (is the chart view beside it; do its title and cards
   overlap), (c) the boat's mini chart under the game's "Sea Chart" title top right; delete the rest; list each kept
   file with what it shows.

### 2. Gaps launches, solo (G1-G7)

7. `Build-RogueWavesModeMod.ps1 -Define ROGUEWAVESMODE_SELFTEST,ROGUEWAVESMODE_GAPS,ROGUEWAVESMODE_SHOTS,MODKIT_PLAY`;
   queue `RogueWavesMode`; Build Queue once. Then one `Run-TwoPlayers.ps1 -Extra "-rwonly <parts>"` per row, in this
   order: G1 `rules,effects,any,base,trinkets,scales,foes,hud,blocked`; G5 `stock`; G2 `review`; G6 `decided` (only if
   this thread has sent a second handoff `Full_round1b` by then: fetch and apply it, Refresh, rebuild this build first;
   otherwise skip it and say so); G7 `light`; G3 `ten` and G4 `tour` with `-TimeoutMinutes 45`. For each: the pass,
   fail, setup counts, every FAIL and SETUP with 3 lines around it, and every `INFO` and `FINDING` line. G5 also edits
   the lobby's picks and ends with "The test put the lobby's picks back as they were": send that line.

### 3. Two players (T1, T2)

8. `Build-RogueWavesModeMod.ps1 -Define ROGUEWAVESMODE_SELFTEST,ROGUEWAVESMODE_TWOPC`; queue; Build Queue.
9. **T1:** `Run-TwoPlayers.ps1` with no `-Extra`. Capture the screen every 3 s into
   `Desktop\RogueWave\full_round1\frames2`; keep only the frames with the SHIP'S LOG board (both windows if both show
   it) and list them. Send each log's counts, every FAIL with 3 lines around it, `grep -c "Could not retrieve
   Vfx/Mobbase"` for every log, and every line holding `General_Default`, `O3b`, `O5`, `U3`, `MB9`, `MB10`, `MB11`,
   `board`, `slipped through`, `Continue`; from the second player's log also `chart view`, `banner`, `SAILING`, `Cove`.
10. **T2:** `Run-TwoPlayers.ps1 -Extra "-rwshort -rwreview -rwhosthero Hero_Argo -rwsecondhero Hero_Spool"`. The same
    lines, plus every line holding `shelf` or `price` on both machines.

### 4. Play builds (P1, P2)

11. Park the self-test builds out of the Mods folder (a play build must be the only mod there). `Build-YosseleMod.ps1
    -Define YOSSELE_PLAY,YOSSELE_SHOTS,MODKIT_PLAY,MODKIT_TEST`, queue, Build Queue, `Run-Play.ps1 -Vessel Yossele`;
    then the same for Spool. Send the PLAY lines' summary and whether `PLAY DONE` came.

### 5. Restore and report

12. Put `<h>\parked\release_before_Full_round1_20261106` back into the Mods folder, hash-identical to step 2, all six
    enabled as he had them; move this round's test builds to `<h>\Full_round1\test_builds_left`. With TDS closed,
    **copy his save back from the backup step 5 noted** (the script backs up and never restores) and check the files
    match; `Run-TwoPlayers.ps1` restores its own save and registry. `Get-PcManifest.ps1`.
13. Send the results as messages under 50 KB (`FULL part i/n`), then stop the session.

Not in this round (his time at the keyboard): Argo on a map 5 and a two-player map 5; a real mouse on the chart beside
the dialog and key M; the board's four cards at 1080p, Tab replay and the pad's south button; the max review's items
with no test lines (`PC_FULL_2026-11-06.md` §2.7).
