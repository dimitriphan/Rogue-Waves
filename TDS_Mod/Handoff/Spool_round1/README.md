# Handoff Spool_round1 (2026-10-26)

Commit `9141555` (`HEAD`). Made by `TDS_Mod/Tools/pc/make_handoff.py`; the folders mirror `C:\Users\Dimitri\TDS_Mod`.

Round 1 of Spool 0.1.0-dev (first build ever; Dimitri's OK in the thread 2026-10-26 20:59Z, "Go ahead", to my question about running Spool's first round with Yossele parked). Only Spool's files: the PC's Yossele, Argo, Shared and ModKit stay as round 9e left them. Steps: (1) move the game's Mods\YosseleMod (0.2.0-dev, Dimitri's play build) to handoff\parked\YosseleMod_0.2.0-dev_spool1, so SpoolMod is the only mod; (2) check, then apply this handoff; (3) Assets/Refresh and wait for Unity's first compile of Assets/SpoolMod/Editor; (4) Build-SpoolMod.ps1 -Define SPOOL_SELFTEST, queue.txt line 'Spool', Build Queue once, then the [ModKit] lines in Editor.log; (5) Run-SelfTests.ps1 -Vessels Spool. Report the summary in full, every [Spool] line (TEST, icons, journal, warnings), every exception with its stack, and the Spool_map.txt file. (6) Afterwards remove SpoolMod from the game's Mods folder (move it to handoff\parked\SpoolMod_0.1.0-dev_round1) and put YosseleMod back, so Dimitri's play build is the only mod again.

`Apply-Handoff.ps1` (check only) should list:

- will add: `Assets\SpoolMod\Editor\SpoolAseprite.cs`
- will add: `Assets\SpoolMod\Editor\SpoolAudio.cs`
- will add: `Assets\SpoolMod\Editor\SpoolBuilder.cs`
- will add: `Assets\SpoolMod\Editor\SpoolCharacter.cs`
- will add: `Assets\SpoolMod\Editor\SpoolLowpoly.cs`
- will add: `Assets\SpoolMod\Editor\SpoolModPrefab.cs`
- will add: `Assets\SpoolMod\Editor\SpoolModels.cs`
- will add: `Assets\SpoolMod\Editor\SpoolPx.cs`
- will add: `ModSource\Spool\Build-SpoolMod.ps1`
- will add: `ModSource\Spool\Code\00_SpoolMod.cs`
- will add: `ModSource\Spool\Code\01_Util.cs`
- will add: `ModSource\Spool\Code\02_Data.cs`
- will add: `ModSource\Spool\Code\03_Assets.cs`
- will add: `ModSource\Spool\Code\04_Content.cs`
- will add: `ModSource\Spool\Code\05_Server.cs`
- will add: `ModSource\Spool\Code\06_HeroBrain.cs`
- will add: `ModSource\Spool\Code\07_Building.cs`
- will add: `ModSource\Spool\Code\08_Tin.cs`
- will add: `ModSource\Spool\Code\09_Head.cs`
- will add: `ModSource\Spool\Code\10_Cushion.cs`
- will add: `ModSource\Spool\Code\11_Client.cs`
- will add: `ModSource\Spool\Code\12_Commands.cs`
- will add: `ModSource\Spool\Code\13_Journal.cs`
- will add: `ModSource\Spool\Code\97_Shots.cs`
- will add: `ModSource\Spool\Code\99_SelfTest.cs`
- will add: `ModSource\Spool\DESIGN.md`
- will add: `ModSource\Spool\Deploy-Wait.ps1`
- will add: `ModSource\Spool\IMPLEMENTATION_NOTES.md`
- will add: `ModSource\Spool\Tools\Decode-Dump.ps1`

## Steps

1. Only one session works in TDS_Mod at a time (`pc/QUEUE.md` in the Claude Project's files).
2. `Apply-Handoff.ps1` (check only). Stop and report on DIFFERS (the PC has edits that aren't on GitHub), MISSING, a damaged copy, a patch that doesn't fit, or a non-zero exit.
3. `Apply-Handoff.ps1 -Apply`, once Dimitri has OK'd it in the thread in his own words. It backs up what it replaces
   and writes a fresh `handoff\pc_manifest.txt`.
4. Build, test and release as the thread's brief says (`TDS_Mod/Tools/pc/README.md`).
