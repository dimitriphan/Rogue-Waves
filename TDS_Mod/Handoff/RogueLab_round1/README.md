# Handoff RogueLab_round1 (2026-10-08)

Commit `c406234` (`HEAD`). Made by `TDS_Mod/Tools/pc/make_handoff.py`; the folders mirror `C:\Users\Dimitri\TDS_Mod`.

Phase 1 PC round: build RogueLab with -Define ROGUELAB_SELFTEST, queue line RogueLab, then Run-SelfTests.ps1 -Vessels RogueLab -TimeoutMinutes 20; steps in ModSource/RogueLab/README.md

`Apply-Handoff.ps1` (check only) should list:

- will add: `Assets\RogueLabMod\Editor\RogueLabModPrefab.cs`
- will add: `ModSource\RogueLab\Build-RogueLabMod.ps1`
- will add: `ModSource\RogueLab\Code\00_RogueLabMod.cs`
- will add: `ModSource\RogueLab\Code\01_Util.cs`
- will add: `ModSource\RogueLab\Code\02_Boot.cs`
- will add: `ModSource\RogueLab\Code\03_Run.cs`
- will add: `ModSource\RogueLab\Code\04_Match.cs`
- will add: `ModSource\RogueLab\Code\05_Enemies.cs`
- will add: `ModSource\RogueLab\README.md`

## Steps

1. Only one session works in TDS_Mod at a time (`pc/QUEUE.md` in the Claude Project's files).
2. `Apply-Handoff.ps1` (check only). Anything "DIFFERS" means the PC has edits that aren't on GitHub: stop and report them.
3. `Apply-Handoff.ps1 -Apply`, once Dimitri has OK'd it in the thread in his own words. It backs up what it replaces
   and writes a fresh `handoff\pc_manifest.txt`.
4. Build, test and release as the thread's brief says (`TDS_Mod/Tools/pc/README.md`).
