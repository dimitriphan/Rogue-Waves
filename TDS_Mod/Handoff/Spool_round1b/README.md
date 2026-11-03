# Handoff Spool_round1b (2026-10-26)

Commit `447ed78` (`HEAD`). Made by `TDS_Mod/Tools/pc/make_handoff.py`; the folders mirror `C:\Users\Dimitri\TDS_Mod`.

Round 1b of Spool 0.1.0-dev, same round and same OK as Spool_round1 (Dimitri's "Go ahead", 2026-10-26 20:59Z). Fixes from round 1: Slow Unravel's delayed half starts half a second after the hit; the self-test waits for foes to load, hits the Cushions from foes by the enemy core and nets out the Stash's other traffic. Steps as in Spool_round1: park the game's Mods\YosseleMod again (handoff\parked\YosseleMod_0.2.0-dev_spool1b), apply, Build-SpoolMod.ps1 -Define SPOOL_SELFTEST, queue 'Spool', Build Queue once, Run-SelfTests.ps1 -Vessels Spool; report the summary, every [Spool] line and every exception; then park SpoolMod and put YosseleMod back as the only mod.

`Apply-Handoff.ps1` (check only) should list:

- will copy: `ModSource\Spool\Code\07_Building.cs`
- will copy: `ModSource\Spool\Code\08_Tin.cs`
- will copy: `ModSource\Spool\Code\10_Cushion.cs`
- will copy: `ModSource\Spool\Code\99_SelfTest.cs`
- will copy: `ModSource\Spool\IMPLEMENTATION_NOTES.md`

## Steps

1. Only one session works in TDS_Mod at a time (`pc/QUEUE.md` in the Claude Project's files).
2. `Apply-Handoff.ps1` (check only). Stop and report on DIFFERS (the PC has edits that aren't on GitHub), MISSING, a damaged copy, a patch that doesn't fit, or a non-zero exit.
3. `Apply-Handoff.ps1 -Apply`, once Dimitri has OK'd it in the thread in his own words. It backs up what it replaces
   and writes a fresh `handoff\pc_manifest.txt`.
4. Build, test and release as the thread's brief says (`TDS_Mod/Tools/pc/README.md`).
