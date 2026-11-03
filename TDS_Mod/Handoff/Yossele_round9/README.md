# Handoff Yossele_round9 (2026-10-23)

Commit `b3cd637` (`HEAD`). Made by `TDS_Mod/Tools/pc/make_handoff.py`; the folders mirror `C:\Users\Dimitri\TDS_Mod`.

Yossele 0.2.0-dev (the 2026-10-20 sheet). Park the YosseleMod play build, self-test Yossele; at 0 FAIL build the release and install it as the new play build.

`Apply-Handoff.ps1` (check only) should list:

- will copy: `Assets\ModKit\Editor\ModKitPipeline.cs`
- will copy: `Assets\YosseleMod\Editor\YosseleAudio.cs`
- will copy: `Assets\YosseleMod\Editor\YosseleBuilder.cs`
- will copy: `Assets\YosseleMod\Editor\YosseleCharacter.cs`
- will copy: `Assets\YosseleMod\Editor\YosseleLowpoly.cs`
- will copy: `Assets\YosseleMod\Editor\YosseleModels.cs`
- will copy: `ModSource\Shared\ModKitBoat.cs`
- will copy: `ModSource\Shared\ModKitTides.cs`
- will copy: `ModSource\Yossele\Code\00_YosseleMod.cs`
- will copy: `ModSource\Yossele\Code\01_Util.cs`
- will copy: `ModSource\Yossele\Code\02_Data.cs`
- will copy: `ModSource\Yossele\Code\03_Assets.cs`
- will copy: `ModSource\Yossele\Code\04_Content.cs`
- will copy: `ModSource\Yossele\Code\05_Server.cs`
- will copy: `ModSource\Yossele\Code\06_HeroBrain.cs`
- will copy: `ModSource\Yossele\Code\07_Building.cs`
- will copy: `ModSource\Yossele\Code\08_Cannon.cs`
- will copy: `ModSource\Yossele\Code\09_Dandelion.cs`
- will copy: `ModSource\Yossele\Code\10_Henge.cs`
- will copy: `ModSource\Yossele\Code\11_Pachira.cs`
- will copy: `ModSource\Yossele\Code\12_Hooks.cs`
- will copy: `ModSource\Yossele\Code\13_Client.cs`
- will copy: `ModSource\Yossele\Code\14_Commands.cs`
- will copy: `ModSource\Yossele\Code\15_Journal.cs`
- will add: `ModSource\Yossele\Code\16_Friend.cs`
- will copy: `ModSource\Yossele\Code\99_SelfTest.cs`
- will copy: `ModSource\Yossele\DESIGN.md`
- will copy: `ModSource\Yossele\IMPLEMENTATION_NOTES.md`
- will copy: `ModSource\Yossele\README.md`
- will copy: `ModSource\Yossele\Tools\Decode-Dump.ps1`

## Steps

1. Only one session works in TDS_Mod at a time (`pc/QUEUE.md` in the Claude Project's files).
2. `Apply-Handoff.ps1` (check only). Stop and report on DIFFERS (the PC has edits that aren't on GitHub), MISSING, a damaged copy, a patch that doesn't fit, or a non-zero exit.
3. `Apply-Handoff.ps1 -Apply`, once Dimitri has OK'd it in the thread in his own words. It backs up what it replaces
   and writes a fresh `handoff\pc_manifest.txt`.
4. Build, test and release as the thread's brief says (`TDS_Mod/Tools/pc/README.md`).
