# Handoff RogueWavesMode_round3 (2026-10-11)

Commit `246e24f` (`HEAD`). Made by `TDS_Mod/Tools/pc/make_handoff.py`; the folders mirror `C:\Users\Dimitri\TDS_Mod`.

Round 3 of the sea chart and sea weather (0.1.2): Build-RogueWavesModeMod.ps1 -Define ROGUEWAVESMODE_SELFTEST, queue.txt line 'RogueWavesMode', Build Queue, then Run-SelfTests.ps1 -Vessels RogueWavesMode -TimeoutMinutes 20. Report the summary, every [RogueWavesMode] line and the exceptions with stacks. Watch the 'weather Rising Water: ... stood in by a copy of ...' line and W1's flooded tiles. Afterwards move RogueWavesModeMod out of the game's Mods folder.

`Apply-Handoff.ps1` (check only) should list:

- will copy: `Assets\RogueWavesModeMod\Editor\RogueWavesModeModPrefab.cs`
- will copy: `ModSource\RogueWavesMode\Code\00_RogueWavesModeMod.cs`
- will copy: `ModSource\RogueWavesMode\Code\07_Weather.cs`
- will copy: `ModSource\RogueWavesMode\IMPLEMENTATION_NOTES.md`
- will copy: `ModSource\RogueWavesMode\README.md`

## Steps

1. Only one session works in TDS_Mod at a time (`pc/QUEUE.md` in the Claude Project's files).
2. `Apply-Handoff.ps1` (check only). Anything "DIFFERS" means the PC has edits that aren't on GitHub: stop and report them.
3. `Apply-Handoff.ps1 -Apply`, once Dimitri has OK'd it in the thread in his own words. It backs up what it replaces
   and writes a fresh `handoff\pc_manifest.txt`.
4. Build, test and release as the thread's brief says (`TDS_Mod/Tools/pc/README.md`).
