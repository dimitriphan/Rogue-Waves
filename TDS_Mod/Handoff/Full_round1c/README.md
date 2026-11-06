# Handoff Full_round1c (2026-11-06)

Commit `f998558` (`HEAD`). Made by `TDS_Mod/Tools/pc/make_handoff.py`; the folders mirror `C:\Users\Dimitri\TDS_Mod`.

Full round, third part: test-only fixes for the five Argo and Uloe self-test fails of S1 (PC_FULL_2026-11-06 section 3)

`Apply-Handoff.ps1` (check only) should list:

- will copy: `ModSource\Argo\Code\99_SelfTest.cs`
- will copy: `ModSource\Uloe\Code\98_Upgrades.cs`

## Steps

1. Only one session works in TDS_Mod at a time (`pc/QUEUE.md` in the Claude Project's files).
2. `Apply-Handoff.ps1` (check only). Stop and report on DIFFERS (the PC has edits that aren't on GitHub), MISSING, a damaged copy, a patch that doesn't fit, or a non-zero exit.
3. `Apply-Handoff.ps1 -Apply`, once Dimitri has OK'd it in the thread in his own words. It backs up what it replaces
   and writes a fresh `handoff\pc_manifest.txt`.
4. Build, test and release as the thread's brief says (`TDS_Mod/Tools/pc/README.md`).

## This round

The third part of `Full_round1` (same OK, same round): test-only changes to Argo's and Uloe's self-tests for S1's five
fails (Chain Lightning, Lighthouse, a Power Bank's Photons; B13, Pay Dirt, Abyss's SETUP). After the play builds and
before the restore: fetch, check and apply it (`Get-Handoff.ps1 -Ref claude/project-thread-jry4d6 -Name Full_round1c`),
Refresh, `Build-ArgoMod.ps1 -Define ARGO_SELFTEST` and `Build-UloeMod.ps1 -Define ULOE_SELFTEST`, queue both, Build
Queue, then `Run-SelfTests.ps1 -Vessels Argo,Uloe`. Send both summaries in full, every FAIL and SETUP with 3 lines
around it, and the lines holding `Chain Lightning`, `Lighthouse`, `Photons go`, `B13`, `Pay Dirt`, `Abyss`, `65%`.
Then the restore as in `Full_round1` step 12 (his save from the first backup).
