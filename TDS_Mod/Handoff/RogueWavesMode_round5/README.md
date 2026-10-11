# Handoff RogueWavesMode_round5 (2026-10-11)

Commit `ee413d7` (`HEAD`). Made by `TDS_Mod/Tools/pc/make_handoff.py`; the folders mirror `C:\Users\Dimitri\TDS_Mod`.

Round 5 of the mode part (0.2.0): the round-4 run plus the rejoin's solo probes R1-R6 in the first map (docs/REJOIN.md section 6). Build-RogueWavesModeMod.ps1 -Define ROGUEWAVESMODE_SELFTEST, queue.txt line 'RogueWavesMode', Build Queue, then Run-SelfTests.ps1 -Vessels RogueWavesMode -TimeoutMinutes 20. Report the summary, every [RogueWavesMode] line (the 'rejoin:' ones too) and the exceptions with stacks. Afterwards move RogueWavesModeMod out of the game's Mods folder.

`Apply-Handoff.ps1` (check only) should list:

- will copy: `ModSource\RogueWavesMode\Code\00_RogueWavesModeMod.cs`
- will add: `ModSource\RogueWavesMode\Code\09_Rejoin.cs`
- will copy: `ModSource\RogueWavesMode\Code\99_SelfTest.cs`
- will copy: `ModSource\RogueWavesMode\IMPLEMENTATION_NOTES.md`
- will copy: `ModSource\RogueWavesMode\README.md`

On the PC only (left alone): `ModSource\RogueWavesMode\build_log.txt`

## Steps

1. Only one session works in TDS_Mod at a time (`pc/QUEUE.md` in the Claude Project's files).
2. `Apply-Handoff.ps1` (check only). Anything "DIFFERS" means the PC has edits that aren't on GitHub: stop and report them.
3. `Apply-Handoff.ps1 -Apply`, once Dimitri has OK'd it in the thread in his own words. It backs up what it replaces
   and writes a fresh `handoff\pc_manifest.txt`.
4. Build, test and release as the thread's brief says (`TDS_Mod/Tools/pc/README.md`).
