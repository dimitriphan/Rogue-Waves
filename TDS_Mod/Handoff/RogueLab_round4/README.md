# Handoff RogueLab_round4 (2026-10-09)

Commit `03ec57e` (`HEAD`). Made by `TDS_Mod/Tools/pc/make_handoff.py`; the folders mirror `C:\Users\Dimitri\TDS_Mod`.

RogueLab round 4: main-menu start probes and the 2026-10-09 scope's mechanics (docs/MECHANICS_CHECK.md). Build with -Define ROGUELAB_SELFTEST; Run-SelfTests.ps1 -Vessels RogueLab -TimeoutMinutes 15; remove RogueLabMod from the Mods folder afterwards.

`Apply-Handoff.ps1` (check only) should list:

- will copy: `ModSource\RogueLab\Code\00_RogueLabMod.cs`
- will copy: `ModSource\RogueLab\Code\01_Util.cs`
- will copy: `ModSource\RogueLab\Code\03_Run.cs`
- will copy: `ModSource\RogueLab\Code\04_Match.cs`
- will add: `ModSource\RogueLab\Code\07_Round4Menu.cs`
- will add: `ModSource\RogueLab\Code\08_Round4.cs`
- will copy: `ModSource\RogueLab\README.md`

On the PC only (left alone): `ModSource\RogueLab\build_log.txt`

## Steps

1. Only one session works in TDS_Mod at a time (`pc/QUEUE.md` in the Claude Project's files).
2. `Apply-Handoff.ps1` (check only). Anything "DIFFERS" means the PC has edits that aren't on GitHub: stop and report them.
3. `Apply-Handoff.ps1 -Apply`, once Dimitri has OK'd it in the thread in his own words. It backs up what it replaces
   and writes a fresh `handoff\pc_manifest.txt`.
4. Build, test and release as the thread's brief says (`TDS_Mod/Tools/pc/README.md`).
