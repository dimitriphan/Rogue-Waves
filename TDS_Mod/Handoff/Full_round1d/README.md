# Handoff Full_round1d (2026-11-06)

Commit `7b3d95d` (`HEAD`). Made by `TDS_Mod/Tools/pc/make_handoff.py`; the folders mirror `C:\Users\Dimitri\TDS_Mod`.

Full round, fourth part: the gaps test fixes for G1 (blocked, foes), G5 (stock) and G2 (review)

`Apply-Handoff.ps1` (check only) should list:

- will copy: `ModSource\RogueWavesMode\Code\92_Gaps.cs`
- will copy: `ModSource\RogueWavesMode\Code\92_GapsStock.cs`
- will copy: `ModSource\RogueWavesMode\Code\92_Review.cs`

## Steps

1. Only one session works in TDS_Mod at a time (`pc/QUEUE.md` in the Claude Project's files).
2. `Apply-Handoff.ps1` (check only). Stop and report on DIFFERS (the PC has edits that aren't on GitHub), MISSING, a damaged copy, a patch that doesn't fit, or a non-zero exit.
3. `Apply-Handoff.ps1 -Apply`, once Dimitri has OK'd it in the thread in his own words. It backs up what it replaces
   and writes a fresh `handoff\pc_manifest.txt`.
4. Build, test and release as the thread's brief says (`TDS_Mod/Tools/pc/README.md`).

## This round

The fourth part of `Full_round1` (same OK, same round): test-only fixes found by this round's gaps launches. After the
two-player runs (or wherever the round stands, before the play builds): fetch, check and apply it
(`Get-Handoff.ps1 -Ref claude/project-thread-jry4d6 -Name Full_round1d`), Refresh, rebuild the gaps build (step 7's
defines), Build Queue, then three launches: `-rwonly foes,blocked`, `-rwonly stock`, `-rwonly review`. For each: the
counts, every FAIL and SETUP with 3 lines around it, and every INFO and FINDING line; from `foes` the "Map 7: a share
of them" line; from `stock` the Frail Titan and Aura Gavel lines; from `review` the "A later map" lines.
