# Handoff Flow_round1 (2026-11-12)

Commit `1aafcc5` (`HEAD`). Made by `TDS_Mod/Tools/pc/make_handoff.py`; the folders mirror `C:\Users\Dimitri\TDS_Mod`.

Mode flow fix #196 on the PC: the flow twice, mouse (ROGUEWAVESMODE_SELFTEST) then virtual gamepad (ROGUEWAVESMODE_SELFTEST,ROGUEWAVESMODE_PAD)

`Apply-Handoff.ps1` (check only) should list:

- will copy: `ModSource\RogueWavesMode\Code\05_Boat.cs`
- will copy: `ModSource\RogueWavesMode\Code\96_Flow.cs`
- will copy: `ModSource\RogueWavesMode\Code\99_SelfTest.cs`
- will copy: `ModSource\RogueWavesMode\IMPLEMENTATION_NOTES.md`

## Steps

1. Only one session works in TDS_Mod at a time (`pc/QUEUE.md` in the Claude Project's files).
2. `Apply-Handoff.ps1` (check only). Stop and report on DIFFERS (the PC has edits that aren't on GitHub), MISSING, a damaged copy, a patch that doesn't fit, or a non-zero exit.
3. `Apply-Handoff.ps1 -Apply`, once Dimitri has OK'd it in the thread in his own words. It backs up what it replaces
   and writes a fresh `handoff\pc_manifest.txt`.
4. Build, test and release as the thread's brief says (`TDS_Mod/Tools/pc/README.md`).

## The round (Dimitri's OK 2026-11-12 17:23Z: "1 ok"; thread "Fix mode flow regression")

Prove PR #196 (pitfall #177) on the PC. Only RogueWavesMode changes; the other five parts stay as Art_round1 left them
(`d8cddbd`), so the runs compare with `RogueWavesMode_20261107_171452` (121/2) and `RogueWavesMode_20261106_141513` (190/0).

1. Park his release builds, apply this handoff (check, then `-Apply`), Assets/Refresh.
2. Release builds of Argo, Yossele, Spool, Uloe and RogueWavesFoes for `-Also` (as on 2026-11-07).
3. **Run A, mouse:** build RogueWavesMode with `ROGUEWAVESMODE_SELFTEST`; do not touch any gamepad;
   `Run-SelfTests.ps1 -Vessels RogueWavesMode -Also Argo,Yossele,Spool,Uloe,RogueWavesFoes -TimeoutMinutes 30`.
   Expect the `input` INFO line "input mouse", B1 ("When the party leaves ..."), B4, C3 to C6 pass.
4. **Run B, gamepad:** build with `ROGUEWAVESMODE_SELFTEST,ROGUEWAVESMODE_PAD`, the same run (save restored first).
   Expect "input gamepad", C3 lines from `arrived at ... (a gamepad steers: the island itself)`, B1 and B4 pass
   (`RWTest.Unfocus`), C5 passes.
5. Report each `*_summary.txt` in full, plus from each log: the `input` line, every `arrived at` line, every
   `TEST INFO ... sub '` line, B1/B4/C3/C5 lines.
6. Restore: release builds back hash-identical and enabled, save back, TDS closed; `Get-PcManifest.ps1`.
