# Handoff Argo_round6 (2026-10-08)

Commit `9af4f18` (`HEAD`). Made by `TDS_Mod/Tools/pc/make_handoff.py`; the folders mirror `C:\Users\Dimitri\TDS_Mod`.

Round 6: Deep Water's bob (Swell part, new self-test check) and Deep Water's journal model. Self-test with ARGO_SELFTEST,ARGO_SHOTS, then rebuild the playable build (no defines) and leave it installed and enabled.

`Apply-Handoff.ps1` (check only) should list:

- will copy: `Assets\ArgoMod\Editor\ArgoModels.cs`
- will copy: `ModSource\Argo\Code\03_Assets.cs`
- will copy: `ModSource\Argo\Code\10_Client.cs`
- will copy: `ModSource\Argo\Code\13_Journal.cs`
- will copy: `ModSource\Argo\Code\99_SelfTest.cs`
- will copy: `ModSource\Argo\DESIGN.md`
- will copy: `ModSource\Argo\IMPLEMENTATION_NOTES.md`

## Steps

1. Only one session works in TDS_Mod at a time (`pc/QUEUE.md` in the Claude Project's files).
2. `Apply-Handoff.ps1` (check only). Anything "DIFFERS" means the PC has edits that aren't on GitHub: stop and report them.
3. `Apply-Handoff.ps1 -Apply`, once Dimitri has OK'd it in the thread in his own words. It backs up what it replaces
   and writes a fresh `handoff\pc_manifest.txt`.
4. Build, test and release as the thread's brief says (`TDS_Mod/Tools/pc/README.md`).
