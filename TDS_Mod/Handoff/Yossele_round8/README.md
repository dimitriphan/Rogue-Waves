# Handoff Yossele_round8 (2026-10-12)

Commit `d81ce9e` (`HEAD`). Made by `TDS_Mod/Tools/pc/make_handoff.py`; the folders mirror `C:\Users\Dimitri\TDS_Mod`.

Vine quad moved down to the ground (y -0.49, tower convention); self-test checks the vine against being y - 0.5 and logs the ground block top. Self-test, then reinstall the play build.

`Apply-Handoff.ps1` (check only) should list:

- will copy: `Assets\YosseleMod\Editor\YosseleBuilder.cs`
- will copy: `ModSource\Yossele\Code\99_SelfTest.cs`
- will copy: `ModSource\Yossele\IMPLEMENTATION_NOTES.md`

## Steps

1. Only one session works in TDS_Mod at a time (`pc/QUEUE.md` in the Claude Project's files).
2. `Apply-Handoff.ps1` (check only). Anything "DIFFERS" means the PC has edits that aren't on GitHub: stop and report them.
3. `Apply-Handoff.ps1 -Apply`, once Dimitri has OK'd it in the thread in his own words. It backs up what it replaces
   and writes a fresh `handoff\pc_manifest.txt`.
4. Build, test and release as the thread's brief says (`TDS_Mod/Tools/pc/README.md`).
