# Handoff Full_round1b (2026-11-06)

Commit `8b7efa0` (`HEAD`). Made by `TDS_Mod/Tools/pc/make_handoff.py`; the folders mirror `C:\Users\Dimitri\TDS_Mod`.

Full round, second part: the gaps part decided (-rwonly decided, 92_GapsDecided.cs); apply after Full_round1

`Apply-Handoff.ps1` (check only) should list:

- will copy: `ModSource\RogueWavesMode\Code\92_GapsDecided.cs`

## Steps

1. Only one session works in TDS_Mod at a time (`pc/QUEUE.md` in the Claude Project's files).
2. `Apply-Handoff.ps1` (check only). Stop and report on DIFFERS (the PC has edits that aren't on GitHub), MISSING, a damaged copy, a patch that doesn't fit, or a non-zero exit.
3. `Apply-Handoff.ps1 -Apply`, once Dimitri has OK'd it in the thread in his own words. It backs up what it replaces
   and writes a fresh `handoff\pc_manifest.txt`.
4. Build, test and release as the thread's brief says (`TDS_Mod/Tools/pc/README.md`).

## This round

The second part of `Full_round1` (same OK, same round): only the gaps part `decided`. Apply it after `Full_round1`
(`Get-Handoff.ps1 -Ref claude/project-thread-jry4d6 -Name Full_round1b`, check, `-Apply`), Refresh, rebuild the gaps
build (`Full_round1` step 7), then run G6 `-rwonly decided` as that step says. It plays five calm maps (Spool, Yossele,
Uloe, Argo, Boutman); send its counts, every FAIL and SETUP with 3 lines around it, and every INFO and FINDING line.
