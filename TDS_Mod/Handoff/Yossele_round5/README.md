# Handoff Yossele_round5 (2026-10-12)

Commit `32ad4ae` (`HEAD`). Made by `TDS_Mod/Tools/pc/make_handoff.py`; the folders mirror `C:\Users\Dimitri\TDS_Mod`.

Round 5 of Yossele 0.1.0-dev: a building now logs when it goes and why; the self-test clears Tide foes before each section. Build-YosseleMod.ps1 -Define YOSSELE_SELFTEST, queue.txt 'Yossele', Build Queue once, Run-SelfTests.ps1 -Vessels Yossele -TimeoutMinutes 20. Report the summary, every [Yossele] line that is not a TEST MAP line (the 'gone:' and 'buildings gone' lines especially) and any exception outside the game's own code. Afterwards the Mods folder is empty.

`Apply-Handoff.ps1` (check only) should list:

- will copy: `ModSource\Yossele\Code\05_Server.cs`
- will copy: `ModSource\Yossele\Code\99_SelfTest.cs`
- will copy: `ModSource\Yossele\IMPLEMENTATION_NOTES.md`

## Steps

1. Only one session works in TDS_Mod at a time (`pc/QUEUE.md` in the Claude Project's files).
2. `Apply-Handoff.ps1` (check only). Anything "DIFFERS" means the PC has edits that aren't on GitHub: stop and report them.
3. `Apply-Handoff.ps1 -Apply`, once Dimitri has OK'd it in the thread in his own words. It backs up what it replaces
   and writes a fresh `handoff\pc_manifest.txt`.
4. Build, test and release as the thread's brief says (`TDS_Mod/Tools/pc/README.md`).
