# Handoff Yossele_round6 (2026-10-12)

Commit `ad28162` (`HEAD`). Made by `TDS_Mod/Tools/pc/make_handoff.py`; the folders mirror `C:\Users\Dimitri\TDS_Mod`.

Dimitri's test notes: Stonehenge facing arrows (Ice Wall's Forward marker + Entity_Vis_Tower_Orientation), flat vine texture, Dandelion range 6, Selfish reversed; client no longer turns the henge (the game turns Vis). Self-test then reinstall the play build.

`Apply-Handoff.ps1` (check only) should list:

- will copy: `Assets\YosseleMod\Editor\YosseleBuilder.cs`
- will copy: `Assets\YosseleMod\Editor\YosseleModels.cs`
- will copy: `ModSource\Yossele\Code\02_Data.cs`
- will copy: `ModSource\Yossele\Code\04_Content.cs`
- will copy: `ModSource\Yossele\Code\11_Pachira.cs`
- will copy: `ModSource\Yossele\Code\13_Client.cs`
- will copy: `ModSource\Yossele\Code\99_SelfTest.cs`
- will copy: `ModSource\Yossele\DESIGN.md`
- will copy: `ModSource\Yossele\IMPLEMENTATION_NOTES.md`
- will copy: `ModSource\Yossele\README.md`

## Steps

1. Only one session works in TDS_Mod at a time (`pc/QUEUE.md` in the Claude Project's files).
2. `Apply-Handoff.ps1` (check only). Anything "DIFFERS" means the PC has edits that aren't on GitHub: stop and report them.
3. `Apply-Handoff.ps1 -Apply`, once Dimitri has OK'd it in the thread in his own words. It backs up what it replaces
   and writes a fresh `handoff\pc_manifest.txt`.
4. Build, test and release as the thread's brief says (`TDS_Mod/Tools/pc/README.md`).
