# Handoff Yossele_round9b (2026-10-23)

Commit `91937ad` (`HEAD`). Made by `TDS_Mod/Tools/pc/make_handoff.py`; the folders mirror `C:\Users\Dimitri\TDS_Mod`.

Round 9 fixes: self-test checks, Yossele holds its fire during the building checks, Easy Target's own look (new effect, so the Build Queue must regenerate the assets). Same round, same OK: park the play build, self-test; at 0 FAIL build the release and install it as the play build.

`Apply-Handoff.ps1` (check only) should list:

- will copy: `Assets\YosseleMod\Editor\YosseleBuilder.cs`
- will copy: `ModSource\Yossele\Code\02_Data.cs`
- will copy: `ModSource\Yossele\Code\12_Hooks.cs`
- will copy: `ModSource\Yossele\Code\99_SelfTest.cs`
- will copy: `ModSource\Yossele\DESIGN.md`
- will copy: `ModSource\Yossele\IMPLEMENTATION_NOTES.md`

## Steps

1. Only one session works in TDS_Mod at a time (`pc/QUEUE.md` in the Claude Project's files).
2. `Apply-Handoff.ps1` (check only). Stop and report on DIFFERS (the PC has edits that aren't on GitHub), MISSING, a damaged copy, a patch that doesn't fit, or a non-zero exit.
3. `Apply-Handoff.ps1 -Apply`, once Dimitri has OK'd it in the thread in his own words. It backs up what it replaces
   and writes a fresh `handoff\pc_manifest.txt`.
4. Build, test and release as the thread's brief says (`TDS_Mod/Tools/pc/README.md`).
