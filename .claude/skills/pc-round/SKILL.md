---
name: pc-round
description: Use in Rogue Waves when code must be built, self-tested or played on Dimitri's PC: a PC round, a self-test round, a two-player round or a release boot check.
effort: xhigh
---

# Run a Rogue Waves PC round

Steps from `TDS_Mod/Tools/pc/README.md` (read its "A round" when unsure) and kit `TDS_Vessel_Creator/docs/08`.
Effort xhigh. Past rounds to copy the shape of: `docs/reports/TESTING_ROUND_3_2026-11-05.md`,
`docs/reports/LIGHTING_2026-11-05.md` §4 and §6.

## Gate (stop if either fails)

- **Dimitri's OK in the thread**, one typed sentence that covers the whole round. Nothing touches the PC without it,
  and never launch TDS from the cloud.
- **The PC lock is free.** It lives in project memory ("PC LOCK"); `pc/QUEUE.md` in the project files is stale.
  Take it in memory with the thread's name and time; only one session works in `C:\Users\Dimitri\TDS_Mod` at a time.

## Cloud thread

1. Cloud-check every part the round touches (`ship-a-pr`, "Check").
2. Pack a handoff against what the PC has:
   `python3 TDS_Mod/Tools/pc/make_handoff.py <Name> --manifest <pc_manifest.txt>` (or `--since <last applied commit>`),
   `--only <Parts>` to narrow it, `--note` for the round's list. Commit and push `TDS_Mod/Handoff/<Name>/`.
3. Write the round's checklist into that handoff's `README.md`: each check with the report section it comes from.
4. `start_remote_control` with `message_id` = his OK message and folder `C:\Users\Dimitri\TDS_Mod` exactly as listed.
   The PC session cannot reach `/mnt/project-files` or GitHub and does not take `message_thread`: the coordinator
   relays, and results come back as messages under 50 KB each (`<NAME> part i/n`).
   The cloud thread reports the round to the phone widget for the PC session, which has no copy of the script:
   `StatusWidget/status/set_status.py --id <thread id> --state testing --step "<what runs now>"`, a new `--step` per
   result it hears back (`StatusWidget/README.md` "When a thread reports").

## PC session, in order (`<h>` = `Desktop\ClaudeProjects\handoff`)

1. **Back up and park.** Move his release builds out of `TDS\Mods` into `<h>\parked\release_before_<round>_<yyyymmdd>`
   and note their hashes. Back up `Desktop\RogueWave\scope` to `<h>\backup_scope_before_<round>_<yyyymmdd>`.
   `Run-SelfTests.ps1` backs up his save itself but never puts it back; `Run-TwoPlayers.ps1` backs up and restores the
   save and the registry.
2. **Apply.** `Get-Handoff.ps1 -Ref <branch> -Name <Name>`, then `<h>\<Name>\Apply-Handoff.ps1` (check only). Stop and
   report on `DIFFERS`, `MISSING FROM HANDOFF`, `HANDOFF COPY DAMAGED`, `MISSING`, `PATCH DOESN'T FIT` or a non-zero
   exit. Then `Apply-Handoff.ps1 -Apply` (backs up what it replaces, writes `pc_manifest.txt`).
3. **Compile.** Unity `execute_menu_item("Assets/Refresh")`; wait until it ends.
4. **Self-test builds.** `Build-<Part>Mod.ps1 -Define <PART>_SELFTEST` per part, write `<h>\queue.txt`, run
   `Tools/ModKit/Build Queue` once and read the `[ModKit]` lines in Editor.log (pitfall #76: not `queue_status.txt`).
5. **Self-tests.** `Run-SelfTests.ps1 -Vessels <Parts>`. The mode's whole flow:
   `-Vessels RogueWavesMode -Also <the other parts> -TimeoutMinutes 30` (the 12-minute default cuts it short). The
   `-Also` parts must be release builds (no define): a part still in its self-test build runs its own test and takes
   the lobby from the flow (`PC_FULL_2026-11-06.md` §3).
   Report each `*_summary.txt` in full, with the `*_map.txt` files.
6. **Gaps launches.** Build with `ROGUEWAVESMODE_SELFTEST,ROGUEWAVESMODE_GAPS` (add `ROGUEWAVESMODE_SHOTS` for
   pictures), then one solo launch per item: `Run-TwoPlayers.ps1 -Extra "-rwonly <rules|effects|...|light|review>"`
   as the README's table lists.
7. **Two players.** Build with `ROGUEWAVESMODE_SELFTEST,ROGUEWAVESMODE_TWOPC`, run `Run-TwoPlayers.ps1` (second copy
   `D:\TDS_second`; `-Extra "-rwshort -rwhosthero X -rwsecondhero Y"` as the list asks). Play builds:
   `Run-Play.ps1 -Vessel <Part>`.
8. **Scope copy.** Make `Desktop\RogueWave\scope` equal the repository's `docs/scope` (the 17 files).
9. **Restore.** Put the parked release builds back, hash-identical and enabled; his save back (with TDS closed, copy
   the self-test's `handoff\save_backup_<stamp>` over it) and the registry; TDS closed. If release builds were
   rebuilt: `Run-SelfTests.ps1 -Vessels <Parts> -BootCheck`. `Get-PcManifest.ps1` if anything changed after Apply.
   Leave the Mods folder as he had it.
10. Send the results, then stop the session.

## After the round (cloud)

1. A failing check: `TDS_Vessel_Creator/docs/09_PITFALLS.md` by symptom first. Fix in a diff on the newest base,
   never by copying PC files over the repository.
2. Report `docs/reports/PC_ROUND<N>_<yyyy-mm-dd>.md` (or `<TOPIC>_<date>.md`): In short; Self-tests (part, pass/fail,
   log stamp); Readings; For Dimitri; Not run; The PC as left (where the parked copies and backups are). Every result
   says the code it ran on (commit, kit version).
3. Open and merge it with `ship-a-pr`; add the report to `docs/reports/README.md`.
4. Release the PC lock in memory and update its "PC STATE" line (what the Mods folder and Unity project now hold).

## Gotchas

- A gamepad on the PC turns on the game's gamepad mode: the boat parks on the island, dialogs read the focused card's
  line. A flow that passes with the mouse can fail with a pad; run both (pitfall #177).
- Export Mod "times out" in UnityMCP and runs twice: wait for the `.info` timestamp to settle before Restore (#53).
  An export leaves the mod disabled: `Deploy-Wait.ps1` (#54).
- Built the old art: Unity hadn't recompiled the generator; check the editor assembly is newer than the edit (#55).
- Never batch deletes with unverified steps; count files after every park and restore (#56).
- The process is `TDSgame`; close it with `CloseMainWindow()` (#66). `Player.log` is overwritten each launch: copy it
  after every run (#67). Never screenshot the desktop, only in-mod captures (#65).
