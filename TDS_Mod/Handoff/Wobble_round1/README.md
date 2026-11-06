# Handoff Wobble_round1 (2026-11-06)

Commit `8950091` (`HEAD`). Made by `TDS_Mod/Tools/pc/make_handoff.py`; the folders mirror `C:\Users\Dimitri\TDS_Mod`.

Wobble round 1: the main menu buttons on whole pixels (RWPixels, pitfall #174)

`Apply-Handoff.ps1` (check only) should list:

- will copy: `ModSource\RogueWavesMode\Code\00_RogueWavesModeMod.cs`
- will copy: `ModSource\RogueWavesMode\Code\09_Rejoin.cs`
- will copy: `ModSource\RogueWavesMode\Code\10_Online.cs`
- will copy: `ModSource\RogueWavesMode\Code\13_Card.cs`
- will copy: `ModSource\RogueWavesMode\Code\16_Directions.cs`
- will copy: `ModSource\RogueWavesMode\Code\17_Cardinals.cs`
- will copy: `ModSource\RogueWavesMode\Code\19_Objectives.cs`
- will copy: `ModSource\RogueWavesMode\Code\24_Trinkets.cs`
- will add: `ModSource\RogueWavesMode\Code\26_Pixels.cs`
- will copy: `ModSource\RogueWavesMode\Code\92_Gaps.cs`
- will copy: `ModSource\RogueWavesMode\Code\92_GapsTrinkets.cs`
- will copy: `ModSource\RogueWavesMode\Code\92_Review.cs`
- will copy: `ModSource\RogueWavesMode\Code\93_TwoPlayers.cs`
- will add: `ModSource\RogueWavesMode\Code\94_Pixels.cs`
- will copy: `ModSource\RogueWavesMode\Code\96_Flow.cs`
- will copy: `ModSource\RogueWavesMode\Code\99_SelfTest.cs`
- will copy: `ModSource\RogueWavesMode\IMPLEMENTATION_NOTES.md`
- will copy: `ModSource\RogueWavesMode\README.md`

On the PC only (left alone): `ModSource\RogueWavesMode\Art\Temporary\game_size\Cardinal_Bell.png`, `ModSource\RogueWavesMode\Art\Temporary\game_size\Cardinal_Clock.png`, `ModSource\RogueWavesMode\Art\Temporary\game_size\Cardinal_Hull.png`, `ModSource\RogueWavesMode\Art\Temporary\game_size\Cardinal_Legs.png`, `ModSource\RogueWavesMode\Art\Temporary\game_size\Cardinal_Puffer.png`, `ModSource\RogueWavesMode\Art\Temporary\game_size\Cardinal_Rising.png`, `ModSource\RogueWavesMode\Art\Temporary\game_size\Cardinal_Wind.png`, `ModSource\RogueWavesMode\build_log.txt`

## Steps

1. Only one session works in TDS_Mod at a time (`pc/QUEUE.md` in the Claude Project's files).
2. `Apply-Handoff.ps1` (check only). Stop and report on DIFFERS (the PC has edits that aren't on GitHub), MISSING, a damaged copy, a patch that doesn't fit, or a non-zero exit.
3. `Apply-Handoff.ps1 -Apply`, once Dimitri has OK'd it in the thread in his own words. It backs up what it replaces
   and writes a fresh `handoff\pc_manifest.txt`.
4. Build, test and release as the thread's brief says (`TDS_Mod/Tools/pc/README.md`).

## This round (thread "Fix stuttering buttons in main menu")

Dimitri's OK, in the thread: "Yes" (05:57Z) to a timing round on the main menu that puts his builds and save back, and
"Yes" (06:15Z) to leaving the fixed release build of the mode in his Mods folder once it passes. Only the mode changes;
the other five parts' release builds in `TDS\Mods` stay as they are and enabled. `<h>` = `Desktop\ClaudeProjects\handoff`.

1. TDS must be closed (he may be playing: if `TDSgame` runs, stop and say so). Copy `D:\Steam\steamapps\common\TDS\Mods\RogueWavesModeMod`
   to `<h>\parked\release_before_Wobble_round1_20261106` and note its files' hashes.
2. `Get-Handoff.ps1 -Ref claude/project-thread-ioitue -Name Wobble_round1`, then `<h>\Wobble_round1\Apply-Handoff.ps1` (check
   only; stop on DIFFERS, MISSING, a damaged copy or a non-zero exit), then `Apply-Handoff.ps1 -Apply`.
3. Unity: `execute_menu_item("Assets/Refresh")`; wait until it has compiled.
4. `Build-RogueWavesModeMod.ps1 -Define ROGUEWAVESMODE_SELFTEST`; `<h>\queue.txt` = `RogueWavesMode`; Tools > ModKit > Build
   Queue once; read the `[ModKit]` lines in `%LOCALAPPDATA%\Unity\Editor\Editor.log`.
5. `Run-SelfTests.ps1 -Vessels RogueWavesMode -Also Argo,Yossele,Spool,Uloe,RogueWavesFoes -TimeoutMinutes 30`. Send the
   summary in full, and from the kept log every line holding `P1 `, `P2 `, `FINDING P2`, `whole pixels` or `TEST DONE`.
   What this round is for (`docs/reports/BUTTON_WOBBLE_2026-11-06.md`):
   - **P1** the main menu's button pictures (Play, Hero, Settings, More, Exit, the Rogue Waves crest) stand on whole screen
     pixels with `RWPixels` on;
   - **P2** over 144 drawn frames none of their squares changes with it on; the INFO and FINDING lines give the count where
     the game puts them (expected: SEEN, many frames);
   - the rest of the flow passes as in round 4 (162 pass; its two failures then were test faults fixed since).
6. If P1 and P2 pass and the flow has no new FAIL: `Build-RogueWavesModeMod.ps1` (no define), queue, Build Queue, so the
   fixed release build stands in `TDS\Mods\RogueWavesModeMod`, enabled. Then
   `Run-SelfTests.ps1 -Vessels RogueWavesMode,Argo,Yossele,Spool,Uloe,RogueWavesFoes -BootCheck` and send its lines.
   Otherwise put the parked copy from step 1 back, hash-identical, and say why.
7. Leave the six parts enabled as he had them, his save back (the script restores it), TDS closed. `Get-PcManifest.ps1`, then
   send the results and stop the session.

