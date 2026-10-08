# Handoff Argo_round3 (2026-10-08)

Commit `2089c1f` (`HEAD`). Made by `TDS_Mod/Tools/pc/make_handoff.py`; the folders mirror `C:\Users\Dimitri\TDS_Mod`.

Argo 2.0 PC round 3: build Argo with -Define ARGO_SELFTEST, queue line Argo (one Build Queue call; check Editor.log's [ModKit] lines), then Run-SelfTests.ps1 -Vessels Argo. Base: commit a0025e8 (round 2, applied).

`Apply-Handoff.ps1` (check only) should list:

- will copy: `ModSource\Argo\Code\07_TowerBrain.cs`
- will copy: `ModSource\Argo\Code\10_Client.cs`
- will copy: `ModSource\Argo\Code\11_Water.cs`
- will copy: `ModSource\Argo\Code\99_SelfTest.cs`
- will copy: `ModSource\Argo\IMPLEMENTATION_NOTES.md`

## Steps

1. Only one session works in TDS_Mod at a time (`pc/QUEUE.md` in the Claude Project's files).
2. `Apply-Handoff.ps1` (check only). Anything "DIFFERS" means the PC has edits that aren't on GitHub: stop and report them.
3. `Apply-Handoff.ps1 -Apply`, once Dimitri has OK'd it in the thread in his own words. It backs up what it replaces
   and writes a fresh `handoff\pc_manifest.txt`.
4. Build, test and release as the thread's brief says (`TDS_Mod/Tools/pc/README.md`).
