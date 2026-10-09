# Handoff Argo_round5 (2026-10-08)

Commit `4e1cf23` (`HEAD`). Made by `TDS_Mod/Tools/pc/make_handoff.py`; the folders mirror `C:\Users\Dimitri\TDS_Mod`.

Argo 2.0 PC round 5: (1) build Argo with -Define ARGO_SELFTEST,ARGO_SHOTS, queue, Run-SelfTests -Vessels Argo; decode the [ArgoDump] captures with ModSource/Argo/Tools/Decode-Dump.ps1 and send the PNGs. (2) If 0 FAIL: Build-ArgoMod.ps1 with no -Define, queue, Run-SelfTests -Vessels <all installed vessel mods> -BootCheck, and leave the playable Argo enabled. No zip or publish. Base: commit f384d4b (round 4, applied).

`Apply-Handoff.ps1` (check only) should list:

- will copy: `ModSource\Argo\Code\99_SelfTest.cs`
- will copy: `ModSource\Argo\IMPLEMENTATION_NOTES.md`

## Steps

1. Only one session works in TDS_Mod at a time (`pc/QUEUE.md` in the Claude Project's files).
2. `Apply-Handoff.ps1` (check only). Anything "DIFFERS" means the PC has edits that aren't on GitHub: stop and report them.
3. `Apply-Handoff.ps1 -Apply`, once Dimitri has OK'd it in the thread in his own words. It backs up what it replaces
   and writes a fresh `handoff\pc_manifest.txt`.
4. Build, test and release as the thread's brief says (`TDS_Mod/Tools/pc/README.md`).
