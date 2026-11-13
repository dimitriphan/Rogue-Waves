# PC round Flow_round1 (2026-11-12): the mode flow fix (#196) proven, with the mouse and with a gamepad

Dimitri, 2026-11-12 17:23Z: "1 ok" (the PC round for the mode flow fix). It proves PR #196 (pitfall #177): the
2026-11-07 regression (`PC_ART_ROUND1_2026-11-07.md` §3) came from the game's gamepad mode, not from #163.

**Code.** Handoff `Flow_round1` from `claude/project-thread-xnob3c` at `ea564e7` (#196's merged code plus the test-only
`ROGUEWAVESMODE_PAD`), packed `--since d8cddbd --only RogueWavesMode`. The check printed "will copy" for the 4 files
(`05_Boat.cs`, `96_Flow.cs`, `99_SelfTest.cs`, `IMPLEMENTATION_NOTES.md`) and refused none. `-Apply` copied 4. The other
five parts stayed at `d8cddbd`, as on 2026-11-07, so the runs compare with that round directly. Kit 1.15. Build queue 1:
five release builds plus the mode with `ROGUEWAVESMODE_SELFTEST`, `QUEUE DONE ok=6 failed=0`. Queue 2: the mode with
`ROGUEWAVESMODE_SELFTEST,ROGUEWAVESMODE_PAD`, `ok=1`. The save before the round was restored hash-identical before each
run. Nobody touched a gamepad: in the PAD runs a virtual gamepad was held as the game's last device all through the launch.

## 1. In short

- **#196 holds.** Run A (mouse) 190/0/0, the same as 2026-11-06's 190/0. Run B (gamepad) 189/0/0. Run B2 (gamepad, rerun)
  190/0/0. On 2026-11-07 the flow failed at 121/2.
- **Gamepad mode really ran** in B and B2: the "input gamepad" line, and every "arrived at" line says "(a gamepad steers:
  the island itself)".
- **No new fault.** The exception lines are the game's own, as in every round (§3).

## 2. Self-tests

| Run | Build | Result | Log stamp |
| --- | --- | --- | --- |
| A, mouse | `ROGUEWAVESMODE_SELFTEST`, `-Also` the five release builds, 30 min | 190 / 0 / 0 | `RogueWavesMode_20261112_124755` |
| B, gamepad | `ROGUEWAVESMODE_SELFTEST,ROGUEWAVESMODE_PAD`, the same | 189 / 0 / 0 | `RogueWavesMode_20261112_130357` |
| B2, gamepad again | the same build | 190 / 0 / 0 | `RogueWavesMode_20261112_132006` |

- **Why B2:** Run B's random chart (seed 1632251655) had only one map in the next row, so no staying vote opened. B1's
  staying check passed on an empty subtitle ("only RW_Node_2_0"), and CV13's after-win vote never ran, which is the
  missing 190th check. B2's chart (seed 1896693562) had a two-card vote.
- Check names: A and B2 differ only in the random W4 map (Ulu against Gridhead), and A and 2026-11-06 only in W4.

## 3. Readings

- **The boat, with a gamepad:** C3 lands 0.09-0.1 from the island (with the mouse, 1.46-2.57 from it, at the dock). C5
  measures with room 3.5 (mouse 1.5), the boat 0.05 from where the game parks it.
- **The stay vote's line, with a gamepad (B2):** after `RWTest.Unfocus` it reads "When the party leaves, it sails to the
  map most of you pick". B1 and CV13's after-win vote pass. B4's exit dialog shows the cost line in all three runs.
- **Dialogs the test does not unfocus**, with a gamepad, show their focused card's line in place of the subtitle, as the
  stock game does for a pad player: the Direction picks ("NOTCHED OAR ▪ ...", "MAGPIE NEST ▪ ..."), the shop and the
  board ("BACK"), the vote to leave ("LEAVE ▪ Leave this map"). The main-menu dialog's B1 INFO line reads "BACK" with
  the mouse too, as on 2026-11-06.
- **Exception lines** (lines containing "Exception", the 20 "error seen" echoes counted apart): A 324, B 339, B2 343
  (2026-11-06: 331). OperationCanceledException 289-293 (the game's `Blu2_Menu_Handler.Animin`); NullReferenceException
  16 with the mouse (13 Netcode `NetworkVariableBase.GetWritePermissionError`, 3 `Animin`) and 22 with the gamepad (plus 3
  `Bla_Ambient.Turnoff` and 3 `Bla_Ambient.OnDisable` to `Bl_Sound.Stopgroup`); TaskCanceledException 8-9 with the
  gamepad only (the game's `Blu4_Group` `Dolater`). None comes from the pack's code.

## 4. For Dimitri

- Nothing is blocked. The flow passes either way, so the mode no longer blocks a release on this.
- Your call, if you want it: with a gamepad, should the mode's own dialogs keep their subtitle (the stay vote's "When
  the party leaves ...", a vote's tally) instead of the focused card's line? The stock game shows the card's line, and
  that is how a pad player learns what an icon-only card is, so the round leaves it as it is.

## 5. Not run

The other parts' self-tests (unchanged since `PC_ART_ROUND1_2026-11-07.md`), gaps launches, two players, #179's Argo
art and the leftovers of rounds 5 and 6 (`docs/PLAN.md` Part C "Next").

## 6. The PC as left

`h` = `Desktop\ClaudeProjects\handoff`.

- **TDS:** closed.
- **Mods folder:** his six release builds, 30 files, hash-identical to the parked copy
  `h\parked\release_before_Flow_round1_20261112`, all enabled.
- **Save:** hash-identical to `h\save_backup_20261112_123243`. The saves after each run are in
  `h\save_after_Flow_round1_runA`, `_runB` and `_runB2`.
- **Unity:** left open, because the round started it. Its prefabs hold the five release builds and the mode's
  `SELFTEST,PAD` build.
- **TDS_Mod:** `d8cddbd` plus the 4 Flow_round1 files, which equals #196's merged code. `pc_manifest.txt` was written
  again (1305 files). The 4 files it replaced are backed up in `h\backup_Flow_round1_20261112_122739`.
- **Logs:** in `h\selftest_logs`, with line extracts in `h\Flow_round1\results\`.
- **Scope copy:** untouched.
