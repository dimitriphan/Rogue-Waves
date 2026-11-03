# Handoff RogueLab_round8 (2026-10-29)

Commit `fb61f29` (`HEAD`). Made by `TDS_Mod/Tools/pc/make_handoff.py`; the folders mirror `C:\Users\Dimitri\TDS_Mod`.

Engine study probes (docs/reports/SCOPE_FIT_PLAN_2026-10-28.md §11). Build-RogueLabMod.ps1 -Define ROGUELAB_SELFTEST; queue.txt: RogueLab; Run-SelfTests.ps1 -Vessels RogueLab -TimeoutMinutes 15.

`Apply-Handoff.ps1` (check only) should list:

- will copy: `Assets\RogueLabMod\Editor\RogueLabModPrefab.cs`
- will copy: `ModSource\RogueLab\Code\00_RogueLabMod.cs`
- will copy: `ModSource\RogueLab\Code\01_Util.cs`
- will copy: `ModSource\RogueLab\Code\02_Boot.cs`
- will copy: `ModSource\RogueLab\Code\03_Run.cs`
- will copy: `ModSource\RogueLab\Code\04_Match.cs`
- will copy: `ModSource\RogueLab\Code\06_Round2.cs`
- will copy: `ModSource\RogueLab\Code\07_Round4Menu.cs`
- will copy: `ModSource\RogueLab\Code\08_Round4.cs`
- will copy: `ModSource\RogueLab\Code\09_Round7.cs`
- will add: `ModSource\RogueLab\Code\10_Round8.cs`
- will copy: `ModSource\RogueLab\README.md`

On the PC only (left alone): `ModSource\RogueLab\build_log.txt`

## Steps

1. Only one session works in TDS_Mod at a time (`pc/QUEUE.md` in the Claude Project's files).
2. `Apply-Handoff.ps1` (check only). Stop and report on DIFFERS (the PC has edits that aren't on GitHub), MISSING, a damaged copy, a patch that doesn't fit, or a non-zero exit.
3. `Apply-Handoff.ps1 -Apply`, once Dimitri has OK'd it in the thread in his own words. It backs up what it replaces
   and writes a fresh `handoff\pc_manifest.txt`.
4. Build, test and release as the thread's brief says (`TDS_Mod/Tools/pc/README.md`).
