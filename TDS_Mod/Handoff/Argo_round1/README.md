# Handoff Argo_round1 (2026-10-08)

Commit `a8f739d` (`HEAD`). Made by `TDS_Mod/Tools/pc/make_handoff.py`; the folders mirror `C:\Users\Dimitri\TDS_Mod`.

Argo 2.0 first PC round: build Argo with -Define ARGO_SELFTEST, queue line Argo, then Run-SelfTests.ps1 -Vessels Argo. Replaces the installed ArgoMod 1.4.0 with the dev build. Base: commit bbbc77a (Argo 1.4.0 as copied from ModProjects). The PC's 99_SelfTest.cs, DESIGN.md and IMPLEMENTATION_NOTES.md are older 1.4.0 copies (2026-10-04), so they are forced (base *); Apply-Handoff backs them up.

`Apply-Handoff.ps1` (check only) should list:

- will copy: `Assets\ArgoMod\Editor\ArgoAudio.cs`
- will copy: `Assets\ArgoMod\Editor\ArgoBuilder.cs`
- will copy: `Assets\ArgoMod\Editor\ArgoModels.cs`
- will copy: `ModSource\Argo\Code\00_ArgoMod.cs`
- will copy: `ModSource\Argo\Code\01_Util.cs`
- will copy: `ModSource\Argo\Code\02_Data.cs`
- will copy: `ModSource\Argo\Code\03_Assets.cs`
- will copy: `ModSource\Argo\Code\04_Content.cs`
- will copy: `ModSource\Argo\Code\05_Server.cs`
- will copy: `ModSource\Argo\Code\06_HeroBrain.cs`
- will copy: `ModSource\Argo\Code\07_TowerBrain.cs`
- will copy: `ModSource\Argo\Code\08_NautBrain.cs`
- will copy: `ModSource\Argo\Code\09_Hooks.cs`
- will copy: `ModSource\Argo\Code\10_Client.cs`
- will add: `ModSource\Argo\Code\11_Water.cs`
- will copy: `ModSource\Argo\Code\12_Diagnostics.cs`
- will copy: `ModSource\Argo\Code\13_Journal.cs`
- will copy: `ModSource\Argo\Code\14_CrewSelect.cs`
- will add: `ModSource\Argo\Code\16_AimSelect.cs`
- will replace: `ModSource\Argo\Code\99_SelfTest.cs`
- will replace: `ModSource\Argo\DESIGN.md`
- will replace: `ModSource\Argo\IMPLEMENTATION_NOTES.md`
- will add: `ModSource\Shared\ModKitCounter.cs`
- will add: `ModSource\Shared\ModKitDirections.cs`
- will add: `ModSource\Shared\ModKitFocus.cs`
- will add: `ModSource\Shared\ModKitPack.cs`
- will add: `ModSource\Shared\ModKitTides.cs`

## Steps

1. Only one session works in TDS_Mod at a time (`pc/QUEUE.md` in the Claude Project's files).
2. `Apply-Handoff.ps1` (check only). Anything "DIFFERS" means the PC has edits that aren't on GitHub: stop and report them.
3. `Apply-Handoff.ps1 -Apply`, once Dimitri has OK'd it in the thread in his own words. It backs up what it replaces
   and writes a fresh `handoff\pc_manifest.txt`.
4. Build, test and release as the thread's brief says (`TDS_Mod/Tools/pc/README.md`).
