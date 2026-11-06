# Handoff Full_round1f (2026-11-06)

Commit `215662d` (`HEAD`). Made by `TDS_Mod/Tools/pc/make_handoff.py`; the folders mirror `C:\Users\Dimitri\TDS_Mod`.

Full round, sixth part: Uloe self-test readings for the three Uloe fails left after Full_round1c (PC_FULL_2026-11-06 section 3)

`Apply-Handoff.ps1` (check only) should list:

- will copy: `ModSource\Uloe\Code\98_Upgrades.cs`
- will copy: `ModSource\Uloe\Code\99_SelfTest.cs`

## Steps

1. Only one session works in TDS_Mod at a time (`pc/QUEUE.md` in the Claude Project's files).
2. `Apply-Handoff.ps1` (check only). Stop and report on DIFFERS (the PC has edits that aren't on GitHub), MISSING, a damaged copy, a patch that doesn't fit, or a non-zero exit.
3. `Apply-Handoff.ps1 -Apply`, once Dimitri has OK'd it in the thread in his own words. It backs up what it replaces
   and writes a fresh `handoff\pc_manifest.txt`.
4. Build, test and release as the thread's brief says (`TDS_Mod/Tools/pc/README.md`).

## This round

The sixth part of `Full_round1` (same OK, same round): test-only readings for the three Uloe fails left after
`Full_round1c` (the teammate cap's two lines read 5 and 8 once, and "a Power Bank's Photons go with it: 6 left"). After
`Full_round1e`'s two-player run and before the restore: fetch, check and apply it (`Get-Handoff.ps1 -Ref
claude/project-thread-jry4d6 -Name Full_round1f`), Refresh, `Build-UloeMod.ps1 -Define ULOE_SELFTEST`, queue it, Build
Queue, then `Run-SelfTests.ps1 -Vessels Uloe` (the other five parts' builds as they stand: only Uloe's runs). Send the
summary in full, every FAIL and SETUP with 3 lines around it, and every line holding `teammate`, `Photons go`, `the
cleanup's Sells` or `Lighthouse`. Then the restore as in `Full_round1` step 12 (his save from the first backup).
