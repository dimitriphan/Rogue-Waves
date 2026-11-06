# Handoff Chart_round1 (2026-11-06)

Commit `5e03648` (`HEAD`). Made by `TDS_Mod/Tools/pc/make_handoff.py`; the folders mirror `C:\Users\Dimitri\TDS_Mod`.

Chart round 1: the sea chart navigation (PR #148) on base beab230 plus the wobble fix (5e03648); checks docs/reports/CHART_BUILD_2026-11-06.md section 4

`Apply-Handoff.ps1` (check only) should list:

- will copy: `ModSource\RogueWavesMode\Code\00_RogueWavesModeMod.cs`
- will copy: `ModSource\RogueWavesMode\Code\01_Util.cs`
- will copy: `ModSource\RogueWavesMode\Code\03_Chart.cs`
- will copy: `ModSource\RogueWavesMode\Code\04_World.cs`
- will copy: `ModSource\RogueWavesMode\Code\05_Boat.cs`
- will copy: `ModSource\RogueWavesMode\Code\06_Campaign.cs`
- will copy: `ModSource\RogueWavesMode\Code\08_Icons.cs`
- will copy: `ModSource\RogueWavesMode\Code\09_Rejoin.cs`
- will copy: `ModSource\RogueWavesMode\Code\11_Dialog.cs`
- will copy: `ModSource\RogueWavesMode\Code\13_Card.cs`
- will copy: `ModSource\RogueWavesMode\Code\16_Directions.cs`
- will copy: `ModSource\RogueWavesMode\Code\17_Cardinals.cs`
- will copy: `ModSource\RogueWavesMode\Code\20_Vote.cs`
- will copy: `ModSource\RogueWavesMode\Code\21_Difficulty.cs`
- will copy: `ModSource\RogueWavesMode\Code\24_Trinkets.cs`
- will add: `ModSource\RogueWavesMode\Code\26_ChartView.cs`
- will add: `ModSource\RogueWavesMode\Code\90_Stops.cs`
- will copy: `ModSource\RogueWavesMode\Code\96_Flow.cs`
- will copy: `ModSource\RogueWavesMode\Code\98_SaveFuzz.cs`
- will copy: `ModSource\RogueWavesMode\Code\99_SelfTest.cs`
- will copy: `ModSource\RogueWavesMode\IMPLEMENTATION_NOTES.md`
- will copy: `ModSource\RogueWavesMode\README.md`

## Steps

1. Only one session works in TDS_Mod at a time (`pc/QUEUE.md` in the Claude Project's files).
2. `Apply-Handoff.ps1` (check only). Stop and report on DIFFERS (the PC has edits that aren't on GitHub), MISSING, a damaged copy, a patch that doesn't fit, or a non-zero exit.
3. `Apply-Handoff.ps1 -Apply`, once Dimitri has OK'd it in the thread in his own words. It backs up what it replaces
   and writes a fresh `handoff\pc_manifest.txt`.
4. Build, test and release as the thread's brief says (`TDS_Mod/Tools/pc/README.md`).

## This round (thread "Redesign the roguelike sea chart navigation")

Dimitri's OK, in the project chat: "Go chart round" (2026-11-06 06:59Z), for the checks in
`docs/reports/CHART_BUILD_2026-11-06.md` §4. The code is base `beab230` (the chart build, PR #148) plus the
main-menu wobble fix (`5e03648`, not merged yet), which the PC already has. Only the mode changes. His release builds
go back exactly as they were at the end (no new release is left in his Mods folder). `<h>` = `Desktop\ClaudeProjects\handoff`.

1. TDS must be closed (if `TDSgame` runs, stop and say so). Copy `D:\Steam\steamapps\common\TDS\Mods\RogueWavesModeMod`
   to `<h>\parked\release_before_Chart_round1_20261106` and note its files' hashes (the other five parts stay put).
2. `Get-Handoff.ps1 -Ref claude/project-thread-n4gjtz -Name Chart_round1`, then `<h>\Chart_round1\Apply-Handoff.ps1`
   (check only; stop on DIFFERS, MISSING, a damaged copy or a non-zero exit), then `Apply-Handoff.ps1 -Apply`.
3. Unity: `execute_menu_item("Assets/Refresh")`; wait until it has compiled. Send any compile error in full.
4. `Build-RogueWavesModeMod.ps1 -Define ROGUEWAVESMODE_SELFTEST`; `<h>\queue.txt` = `RogueWavesMode`; Tools > ModKit >
   Build Queue once; read the `[ModKit]` lines in `%LOCALAPPDATA%\Unity\Editor\Editor.log`.
5. **Self-test** (§4 items 1, 3, 4, 5): `Run-SelfTests.ps1 -Vessels RogueWavesMode -Also Argo,Yossele,Spool,Uloe,RogueWavesFoes -TimeoutMinutes 30`.
   Note the path of the save backup it makes. Send the summary in full, and from the kept log every line holding
   `CV`, `chart view`, `Cove`, `Wreck`, `rests`, `TEST DONE`, plus every FAIL and SETUP line with 3 lines around it.
   Expected: CV1-CV5 and CV9-CV12 pass; the rest of the flow as in round 4 (162 pass) plus the wobble round's P1-P2.
   **Pictures for his eye (CV8, §4 item 8):** while the flow runs, save a full-screen capture every 3 s
   (PowerShell `System.Windows.Forms.Screen` + `Graphics.CopyFromScreen`, PNG) into
   `Desktop\RogueWave\chart_round1\frames`; afterwards keep only the frames that show the chart view (the staging vote
   with the chart on the left half; the boat with the mini chart top right and a banner) in
   `Desktop\RogueWave\chart_round1`, delete the rest, and list the kept files with what each shows. These pictures stay
   on the PC (they hold game art).
6. **Two players** (§4 item 6): `Build-RogueWavesModeMod.ps1 -Define ROGUEWAVESMODE_SELFTEST,ROGUEWAVESMODE_TWOPC`,
   queue, Build Queue, then `Run-TwoPlayers.ps1` as in round 4. Send each log's pass/fail count, every FAIL with 3
   lines around it, and from the second player's log every line holding `chart view`, `banner`, `SAILING`, `Cove`.
7. **Restore:** put `<h>\parked\release_before_Chart_round1_20261106` back as `TDS\Mods\RogueWavesModeMod`,
   hash-identical to step 1, enabled; the other five parts enabled as he had them. **Copy his save back by hand from the
   backup `Run-SelfTests.ps1` made** (the script backs it up but does not restore it); `Run-TwoPlayers.ps1` restores its
   own. TDS closed. `Get-PcManifest.ps1`.
8. Send the results as messages under 50 KB (`CHART part i/n`), then stop the session.

Not in this round (by hand, his time): clicking an island with the real mouse beside the dialog, and key M on the boat
and in chat (§4 items 2 and 7). The self-test's CV4 clicks through the view's own hook, not the mouse.
