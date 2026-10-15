# Handoff Yossele_round1 (2026-10-12)

Commit `f464c7a` (`HEAD`). Made by `TDS_Mod/Tools/pc/make_handoff.py`; the folders mirror `C:\Users\Dimitri\TDS_Mod`.

Round 1 of Yossele 0.1.0-dev (first build ever): Unity's first compile of Assets/YosseleMod/Editor, Build-YosseleMod.ps1 -Define YOSSELE_SELFTEST, queue.txt line 'Yossele', Build Queue, then Run-SelfTests.ps1 -Vessels Yossele. Report the summary, every [Yossele] line (TEST, Stonehenge, journal, icons, warnings) and every exception with its stack. Afterwards move YosseleMod out of the game's Mods folder.

`Apply-Handoff.ps1` (check only) should list:

- will add: `Assets\YosseleMod\Editor\YosseleAseprite.cs`
- will add: `Assets\YosseleMod\Editor\YosseleAudio.cs`
- will add: `Assets\YosseleMod\Editor\YosseleBuilder.cs`
- will add: `Assets\YosseleMod\Editor\YosseleCharacter.cs`
- will add: `Assets\YosseleMod\Editor\YosseleLowpoly.cs`
- will add: `Assets\YosseleMod\Editor\YosseleModPrefab.cs`
- will add: `Assets\YosseleMod\Editor\YosseleModels.cs`
- will add: `Assets\YosseleMod\Editor\YosselePx.cs`
- will add: `ModSource\Yossele\Build-YosseleMod.ps1`
- will add: `ModSource\Yossele\Code\00_YosseleMod.cs`
- will add: `ModSource\Yossele\Code\01_Util.cs`
- will add: `ModSource\Yossele\Code\02_Data.cs`
- will add: `ModSource\Yossele\Code\03_Assets.cs`
- will add: `ModSource\Yossele\Code\04_Content.cs`
- will add: `ModSource\Yossele\Code\05_Server.cs`
- will add: `ModSource\Yossele\Code\06_HeroBrain.cs`
- will add: `ModSource\Yossele\Code\07_Building.cs`
- will add: `ModSource\Yossele\Code\08_Cannon.cs`
- will add: `ModSource\Yossele\Code\09_Dandelion.cs`
- will add: `ModSource\Yossele\Code\10_Henge.cs`
- will add: `ModSource\Yossele\Code\11_Pachira.cs`
- will add: `ModSource\Yossele\Code\12_Hooks.cs`
- will add: `ModSource\Yossele\Code\13_Client.cs`
- will add: `ModSource\Yossele\Code\14_Commands.cs`
- will add: `ModSource\Yossele\Code\15_Journal.cs`
- will add: `ModSource\Yossele\Code\97_Shots.cs`
- will add: `ModSource\Yossele\Code\99_SelfTest.cs`
- will add: `ModSource\Yossele\DESIGN.md`
- will add: `ModSource\Yossele\Deploy-Wait.ps1`
- will add: `ModSource\Yossele\IMPLEMENTATION_NOTES.md`
- will add: `ModSource\Yossele\README.md`
- will add: `ModSource\Yossele\Tools\Decode-Dump.ps1`

## Steps

1. Only one session works in TDS_Mod at a time (`pc/QUEUE.md` in the Claude Project's files).
2. `Apply-Handoff.ps1` (check only). Anything "DIFFERS" means the PC has edits that aren't on GitHub: stop and report them.
3. `Apply-Handoff.ps1 -Apply`, once Dimitri has OK'd it in the thread in his own words. It backs up what it replaces
   and writes a fresh `handoff\pc_manifest.txt`.
4. Build, test and release as the thread's brief says (`TDS_Mod/Tools/pc/README.md`).
