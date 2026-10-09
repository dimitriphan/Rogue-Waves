# Handoff RogueLab_round6 (2026-10-09)

Commit `ae32a1a` (`HEAD`). Made by `TDS_Mod/Tools/pc/make_handoff.py`; the folders mirror `C:\Users\Dimitri\TDS_Mod`.

RogueLab round 6: round 5 with the menu probes M1 and M4 fixed, plus M4b (docs/MAIN_MENU_START.md §5). Build with -Define ROGUELAB_SELFTEST; Run-SelfTests.ps1 -Vessels RogueLab -TimeoutMinutes 15; remove RogueLabMod from the Mods folder afterwards.

`Apply-Handoff.ps1` (check only) should list:

- will copy: `ModSource\RogueLab\Code\01_Util.cs`
- will copy: `ModSource\RogueLab\Code\07_Round4Menu.cs`
- will copy: `ModSource\RogueLab\Code\08_Round4.cs`
- will copy: `ModSource\RogueLab\README.md`

## Steps

1. Only one session works in TDS_Mod at a time (`pc/QUEUE.md` in the Claude Project's files).
2. `Apply-Handoff.ps1` (check only). Anything "DIFFERS" means the PC has edits that aren't on GitHub: stop and report them.
3. `Apply-Handoff.ps1 -Apply`, once Dimitri has OK'd it in the thread in his own words. It backs up what it replaces
   and writes a fresh `handoff\pc_manifest.txt`.
4. Build, test and release as the thread's brief says (`TDS_Mod/Tools/pc/README.md`).
