# RogueLab (research build, never released)

The throwaway mod of the Phase 1 PC round (`TDS_Vessel_Creator/docs/15_ROGUE_MODE_ENGINE.md` §11). It answers the
chapter's "untested" lines by doing each thing once on a solo host and logging one line per question:
`[RogueLab] TEST PASS|FAIL|SETUP|INFO ...`, plus raw `TIDE`, `GROUP`, `ELITE` and `ENEMY` lines, then `TEST DONE`.

| When | What it does | docs/15 |
| --- | --- | --- |
| Boot | dumps the database folders, `Mode_Normal`'s Tide table and the enemy groups; registers `Mode_RogueLab` (stock scripts by name, 3 Tides) with a `RogueLab_` twin of every `Mode_Normal` Gameplan, and the Cardinal `Artifact_RogueLab`; writes and reads a save key | §2, §7, §8, §11.9 |
| Boat | picks Boutman and the test map, equips the mod Cardinal in its colour slot, swaps the match to the twin Gameplan | §3, §7 |
| Map 1 | checks the mode and Cardinal, grants a core upgrade mid-match, reproduces the F-key bug with Boutbro and checks the fix, rewinds a Tide, leaves between Tides as a win, chains straight into map 2 | §3, §5, §7, §9 |
| Map 2 | checks what survived the scene change, opens a 3-option `Runprompt` for 30 s (click one), `TEST DONE` | §3, §6 |

Compiled here (`check_mod.py RogueLab/Code Shared RogueLab "ROGUELAB_SELFTEST"`, 0 errors). Never run.

## PC round

1. Fetch and apply handoff `RogueLab_round1` from branch `claude/phase-1-6lfnar` (`TDS_Mod/Tools/pc/README.md`).
2. `Build-RogueLabMod.ps1 -Define ROGUELAB_SELFTEST` (always with the define: without it the code stops on `#error`).
3. `handoff\queue.txt` = one line `RogueLab`, then Tools > ModKit > Build Queue once, until `QUEUE DONE`.
4. `Run-SelfTests.ps1 -Vessels RogueLab -TimeoutMinutes 20`. Disable other mods in the game's Mods folder first if
   they are enabled; the runner only toggles the mods it is given.
5. Send back the summary **and every `[RogueLab]` line** of the copied log in `handoff\selftest_logs`
   (`Select-String -Path <log> -Pattern '\[RogueLab\]'` into a file): the summary keeps only PASS/FAIL/SETUP lines.
6. Tools > RogueLab > Restore Parked Mods if the queue didn't, then disable `RogueLabMod` in the game.

A run that crashes before `TEST DONE` can leave the save key `Unlock/Unlock_Artifact_Artifact_RogueLab`: harmless, it
names an artifact that exists only while RogueLab is loaded. Steps 10 and 11 of §11 (online, by hand) are not here.
