# Handoff RogueLab_round3 (2026-10-09)

Commit `7718f13` (`HEAD`). Made by `TDS_Mod/Tools/pc/make_handoff.py`; the folders mirror `C:\Users\Dimitri\TDS_Mod`.

RogueLab 0.3.0, round 3 of docs/RESEARCH_GAPS.md; see ModSource/RogueLab/README.md 'Round 3'

`Apply-Handoff.ps1` (check only) should list:

- will copy: `Assets\RogueLabMod\Editor\RogueLabModPrefab.cs`
- will copy: `ModSource\RogueLab\Code\00_RogueLabMod.cs`
- will copy: `ModSource\RogueLab\Code\01_Util.cs`
- will copy: `ModSource\RogueLab\Code\02_Boot.cs`
- will copy: `ModSource\RogueLab\Code\04_Match.cs`
- will copy: `ModSource\RogueLab\Code\06_Round2.cs`
- will copy: `ModSource\RogueLab\README.md`

## Steps

1. Only one session works in TDS_Mod at a time (`pc/QUEUE.md` in the Claude Project's files).
2. `Apply-Handoff.ps1` (check only). Anything "DIFFERS" means the PC has edits that aren't on GitHub: stop and report them.
3. `Apply-Handoff.ps1 -Apply`, once Dimitri has OK'd it in the thread in his own words. It backs up what it replaces
   and writes a fresh `handoff\pc_manifest.txt`.
4. Build, test and release as the thread's brief says (`TDS_Mod/Tools/pc/README.md`).
