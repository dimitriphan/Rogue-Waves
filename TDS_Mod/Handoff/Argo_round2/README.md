# Handoff Argo_round2 (2026-10-08)

Commit `6f9c3b3` (`HEAD`). Made by `TDS_Mod/Tools/pc/make_handoff.py`; the folders mirror `C:\Users\Dimitri\TDS_Mod`.

Argo 2.0 PC round 2: build Argo with -Define ARGO_SELFTEST (the replaced Build-ArgoMod.ps1 adds MODKIT_TEST), queue line Argo, then Run-SelfTests.ps1 -Vessels Argo. Base: commit 8aae091 (round 1, applied). Build-ArgoMod.ps1 is forced: the PC's copy is older and lacks the MODKIT_TEST line.

`Apply-Handoff.ps1` (check only) should list:

- will replace: `ModSource\Argo\Build-ArgoMod.ps1`
- will copy: `ModSource\Argo\Code\00_ArgoMod.cs`
- will copy: `ModSource\Argo\Code\01_Util.cs`
- will copy: `ModSource\Argo\Code\04_Content.cs`
- will copy: `ModSource\Argo\Code\06_HeroBrain.cs`
- will copy: `ModSource\Argo\Code\07_TowerBrain.cs`
- will copy: `ModSource\Argo\Code\99_SelfTest.cs`
- will copy: `ModSource\Argo\IMPLEMENTATION_NOTES.md`

## Steps

1. Only one session works in TDS_Mod at a time (`pc/QUEUE.md` in the Claude Project's files).
2. `Apply-Handoff.ps1` (check only). Anything "DIFFERS" means the PC has edits that aren't on GitHub: stop and report them.
3. `Apply-Handoff.ps1 -Apply`, once Dimitri has OK'd it in the thread in his own words. It backs up what it replaces
   and writes a fresh `handoff\pc_manifest.txt`.
4. Build, test and release as the thread's brief says (`TDS_Mod/Tools/pc/README.md`).
