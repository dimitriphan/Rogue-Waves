# Handoff RogueWavesMode_round1 (2026-10-10)

Commit `a0d1a6d` (`HEAD`). Made by `TDS_Mod/Tools/pc/make_handoff.py`; the folders mirror `C:\Users\Dimitri\TDS_Mod`.

Sea chart and sea weather, first PC round (docs/SEA_CHART.md section 9): Build-RogueWavesModeMod.ps1 -Define ROGUEWAVESMODE_SELFTEST, queue.txt line 'RogueWavesMode', Build Queue, then Run-SelfTests.ps1 -Vessels RogueWavesMode -TimeoutMinutes 20. Report the summary and every [RogueWavesMode] line (CHART, POOL, world, warped, sailing, arrived, weather). Afterwards remove RogueWavesModeMod from the game's Mods folder.

`Apply-Handoff.ps1` (check only) should list:

- will add: `Assets\RogueWavesModeMod\Editor\RogueWavesModeModPrefab.cs`
- will add: `ModSource\RogueWavesMode\Build-RogueWavesModeMod.ps1`
- will add: `ModSource\RogueWavesMode\Code\00_RogueWavesModeMod.cs`
- will add: `ModSource\RogueWavesMode\Code\01_Util.cs`
- will add: `ModSource\RogueWavesMode\Code\02_Mode.cs`
- will add: `ModSource\RogueWavesMode\Code\03_Chart.cs`
- will add: `ModSource\RogueWavesMode\Code\04_World.cs`
- will add: `ModSource\RogueWavesMode\Code\05_Boat.cs`
- will add: `ModSource\RogueWavesMode\Code\06_Campaign.cs`
- will add: `ModSource\RogueWavesMode\Code\07_Weather.cs`
- will add: `ModSource\RogueWavesMode\Code\08_Icons.cs`
- will add: `ModSource\RogueWavesMode\Code\99_SelfTest.cs`
- will add: `ModSource\RogueWavesMode\IMPLEMENTATION_NOTES.md`
- will add: `ModSource\RogueWavesMode\README.md`

## Steps

1. Only one session works in TDS_Mod at a time (`pc/QUEUE.md` in the Claude Project's files).
2. `Apply-Handoff.ps1` (check only). Anything "DIFFERS" means the PC has edits that aren't on GitHub: stop and report them.
3. `Apply-Handoff.ps1 -Apply`, once Dimitri has OK'd it in the thread in his own words. It backs up what it replaces
   and writes a fresh `handoff\pc_manifest.txt`.
4. Build, test and release as the thread's brief says (`TDS_Mod/Tools/pc/README.md`).
