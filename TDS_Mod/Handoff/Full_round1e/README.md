# Handoff Full_round1e (2026-11-06)

Commit `af9d932` (`HEAD`). Made by `TDS_Mod/Tools/pc/make_handoff.py`; the folders mirror `C:\Users\Dimitri\TDS_Mod`.

Full round, fifth part: two-player test fixes and log lines (O3b, Continue, chart banner, MVP board caption)

`Apply-Handoff.ps1` (check only) should list:

- will copy: `ModSource\RogueWavesMode\Code\26_ChartView.cs`
- will copy: `ModSource\RogueWavesMode\Code\27_Board.cs`
- will copy: `ModSource\RogueWavesMode\Code\28_BoardView.cs`
- will copy: `ModSource\RogueWavesMode\Code\93_TwoPlayers.cs`
- will copy: `ModSource\RogueWavesMode\Code\96_Flow.cs`

## Steps

1. Only one session works in TDS_Mod at a time (`pc/QUEUE.md` in the Claude Project's files).
2. `Apply-Handoff.ps1` (check only). Stop and report on DIFFERS (the PC has edits that aren't on GitHub), MISSING, a damaged copy, a patch that doesn't fit, or a non-zero exit.
3. `Apply-Handoff.ps1 -Apply`, once Dimitri has OK'd it in the thread in his own words. It backs up what it replaces
   and writes a fresh `handoff\pc_manifest.txt`.
4. Build, test and release as the thread's brief says (`TDS_Mod/Tools/pc/README.md`).

## This round

The fifth part of `Full_round1` (same OK, same round): T1's FAIL and SETUP were test timing faults, and three readings
had no log line. This adds them. After `Full_round1c`'s rerun and before the restore: fetch, check and apply it
(`Get-Handoff.ps1 -Ref claude/project-thread-jry4d6 -Name Full_round1e`), Refresh, build
`ROGUEWAVESMODE_SELFTEST,ROGUEWAVESMODE_TWOPC` (the five vessel release builds beside it, as for T1), Build Queue, then
`Run-TwoPlayers.ps1 -TimeoutMinutes 40` (no `-Extra`). Send each log's counts, every FAIL and SETUP with 3 lines around
it, and from every log the lines holding `O3b`, `exit dialog`, `chart view: banner`, `MVP board caption`,
`MVP board: map start`, `MB9`, `MB10`, `MB11`, `Continue`. Then the restore as `Full_round1` step 12 says.
