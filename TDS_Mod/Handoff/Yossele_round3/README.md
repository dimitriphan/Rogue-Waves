# Handoff Yossele_round3 (2026-10-12)

Commit `d0917a7` (`HEAD`). Made by `TDS_Mod/Tools/pc/make_handoff.py`; the folders mirror `C:\Users\Dimitri\TDS_Mod`.

Round 3 of Yossele 0.1.0-dev: round 2 passed 86/1/1. The vines now show the stock slow look, the Spaghetti foe is held on the vines, and real hits test the Stonehenge's armour, Vine Blockade's barrier and Connected Roots. Build-YosseleMod.ps1 -Define YOSSELE_SELFTEST, queue.txt 'Yossele', Build Queue once, Run-SelfTests.ps1 -Vessels Yossele -TimeoutMinutes 20. Report the summary, every [Yossele] TEST PASS/FAIL/SETUP line, the counters line and any exception not in the game's own sound or setup code. Afterwards the Mods folder is empty.

`Apply-Handoff.ps1` (check only) should list:

- will copy: `ModSource\Yossele\Code\10_Henge.cs`
- will copy: `ModSource\Yossele\Code\99_SelfTest.cs`
- will copy: `ModSource\Yossele\IMPLEMENTATION_NOTES.md`

## Steps

1. Only one session works in TDS_Mod at a time (`pc/QUEUE.md` in the Claude Project's files).
2. `Apply-Handoff.ps1` (check only). Anything "DIFFERS" means the PC has edits that aren't on GitHub: stop and report them.
3. `Apply-Handoff.ps1 -Apply`, once Dimitri has OK'd it in the thread in his own words. It backs up what it replaces
   and writes a fresh `handoff\pc_manifest.txt`.
4. Build, test and release as the thread's brief says (`TDS_Mod/Tools/pc/README.md`).
