# Handoff Yossele_round10 (2026-10-28)

Commit `42c765c` (`HEAD`). Made by `TDS_Mod/Tools/pc/make_handoff.py`; the folders mirror `C:\Users\Dimitri\TDS_Mod`.

Round 10: install the default branch's Yossele (0.1.0-dev) as the play build, then the first RogueWavesMod 0.1.0 pack build (Tools/pc/README.md 'The release pack').

`Apply-Handoff.ps1` (check only) should list:

- will copy: `Assets\ModKit\Editor\ModKitPipeline.cs`
- will copy: `Assets\YosseleMod\Editor\YosseleBuilder.cs`
- will copy: `Assets\YosseleMod\Editor\YosseleModPrefab.cs`
- will copy: `Assets\YosseleMod\Editor\YosseleModels.cs`
- will add: `ModSource\Pack\README.txt`
- will add: `ModSource\Pack\pack.txt`
- will copy: `ModSource\Shared\ModKitDirections.cs`
- will add: `ModSource\Yossele\Art\Temporary\README.md`
- will add: `ModSource\Yossele\Art\Temporary\aseprite\Yossele-Sheet.aseprite`
- will add: `ModSource\Yossele\Art\Temporary\aseprite\YosseleAttack-Sheet.aseprite`
- will add: `ModSource\Yossele\Art\Temporary\aseprite\Yosselefriend-Sheet.aseprite`
- will add: `ModSource\Yossele\Art\Temporary\game_size\Yossele-Sheet.png`
- will add: `ModSource\Yossele\Art\Temporary\game_size\YosseleAttack-Sheet.png`
- will add: `ModSource\Yossele\Art\Temporary\game_size\Yosselefriend-Sheet.png`
- will add: `ModSource\Yossele\Art\Temporary\preview_8x\Yossele-Sheet_8x.png`
- will add: `ModSource\Yossele\Art\Temporary\preview_8x\YosseleAttack-Sheet_8x.png`
- will add: `ModSource\Yossele\Art\Temporary\preview_8x\Yossele_attack.gif`
- will add: `ModSource\Yossele\Art\Temporary\preview_8x\Yossele_idle.gif`
- will add: `ModSource\Yossele\Art\Temporary\preview_8x\Yossele_move.gif`
- will add: `ModSource\Yossele\Art\Temporary\preview_8x\Yosselefriend-Sheet_8x.png`
- will add: `ModSource\Yossele\Art\Temporary\preview_8x\models_Fruit.png`
- will copy: `ModSource\Yossele\Code\02_Data.cs`
- will copy: `ModSource\Yossele\Code\03_Assets.cs`
- will copy: `ModSource\Yossele\Code\04_Content.cs`
- will copy: `ModSource\Yossele\Code\11_Pachira.cs`
- will copy: `ModSource\Yossele\Code\13_Client.cs`
- will copy: `ModSource\Yossele\Code\99_SelfTest.cs`
- will copy: `ModSource\Yossele\DESIGN.md`
- will copy: `ModSource\Yossele\IMPLEMENTATION_NOTES.md`

On the PC only (left alone): `ModSource\Yossele\build_log.txt`

## Steps

1. Only one session works in TDS_Mod at a time (`pc/QUEUE.md` in the Claude Project's files).
2. `Apply-Handoff.ps1` (check only). Stop and report on DIFFERS (the PC has edits that aren't on GitHub), MISSING, a damaged copy, a patch that doesn't fit, or a non-zero exit.
3. `Apply-Handoff.ps1 -Apply`, once Dimitri has OK'd it in the thread in his own words. It backs up what it replaces
   and writes a fresh `handoff\pc_manifest.txt`.
4. Build, test and release as the thread's brief says (`TDS_Mod/Tools/pc/README.md`).
