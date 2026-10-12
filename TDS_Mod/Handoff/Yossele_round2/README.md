# Handoff Yossele_round2 (2026-10-12)

Commit `0f6a41f` (`HEAD`). Made by `TDS_Mod/Tools/pc/make_handoff.py`; the folders mirror `C:\Users\Dimitri\TDS_Mod`.

Round 2 of Yossele 0.1.0-dev: round 1 stopped at the export. Export() now parks the other mods first, and sheets 192 wide are sliced against the PNG's own size. Same steps as round 1 from Apply on: Build-YosseleMod.ps1 -Define YOSSELE_SELFTEST, queue.txt line 'Yossele', Build Queue once, Run-SelfTests.ps1 -Vessels Yossele -TimeoutMinutes 20. Report the summary, the map file, every [Yossele] line and every exception with its stack. Afterwards the Mods folder is empty and the other mods are back in Assets.

`Apply-Handoff.ps1` (check only) should list:

- will copy: `Assets\YosseleMod\Editor\YosseleBuilder.cs`
- will copy: `Assets\YosseleMod\Editor\YosseleModPrefab.cs`

## Steps

1. Only one session works in TDS_Mod at a time (`pc/QUEUE.md` in the Claude Project's files).
2. `Apply-Handoff.ps1` (check only). Anything "DIFFERS" means the PC has edits that aren't on GitHub: stop and report them.
3. `Apply-Handoff.ps1 -Apply`, once Dimitri has OK'd it in the thread in his own words. It backs up what it replaces
   and writes a fresh `handoff\pc_manifest.txt`.
4. Build, test and release as the thread's brief says (`TDS_Mod/Tools/pc/README.md`).
