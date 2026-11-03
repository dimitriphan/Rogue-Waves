# Handoff RogueLab_round8b (2026-10-29)

Commit `5edafee` (`HEAD`). Made by `TDS_Mod/Tools/pc/make_handoff.py`; the folders mirror `C:\Users\Dimitri\TDS_Mod`.

Round 8 rerun: only 10_Round8.cs changes (R6 bookkeeping, N4 exact). The PC applied RogueLab_round8 (1db7c33) before.

`Apply-Handoff.ps1` (check only) should list:

- will copy: `ModSource\RogueLab\Code\10_Round8.cs`

## Steps

1. Only one session works in TDS_Mod at a time (`pc/QUEUE.md` in the Claude Project's files).
2. `Apply-Handoff.ps1` (check only). Stop and report on DIFFERS (the PC has edits that aren't on GitHub), MISSING, a damaged copy, a patch that doesn't fit, or a non-zero exit.
3. `Apply-Handoff.ps1 -Apply`, once Dimitri has OK'd it in the thread in his own words. It backs up what it replaces
   and writes a fresh `handoff\pc_manifest.txt`.
4. Build, test and release as the thread's brief says (`TDS_Mod/Tools/pc/README.md`).
