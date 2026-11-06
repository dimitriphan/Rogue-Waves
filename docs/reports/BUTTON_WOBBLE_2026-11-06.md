# The main menu buttons' wobble (2026-11-06)

Dimitri: "The buttons in game still stutter, but mainly in the main menu", then "The button itself wobbles and jitters,
including the base game buttons in the main menu." Base `262f2c6`; the fix is `RWPixels` in the mode
(`ModSource/RogueWavesMode/Code/26_Pixels.cs`), pitfall #174. No kit code change.

## 1. In short

- **It is the game's own layout, seen through a swaying camera.** At UI scale 3 (his PC: one canvas unit is 3 screen
  pixels) the main menu's button pictures stand on half pixels: Play's 48-unit picture is centred in a 75-unit tile, 13.5
  units in; the 16-unit icons of Settings, More and Exit are centred in 45-unit tiles, 14.5 in; Hero's picture sits at x
  -0.5. Every third column and row of the screen then samples the art exactly on the line between two art pixels.
- **The menu's camera sways.** The active virtual camera `Cameras/Main` carries Cinemachine noise (amplitude 0.1, frequency
  0.3), the `Ui` canvas is Screen Space - Camera on the camera `UI`, and `UI` is a child of `Main Camera`. So the canvas is
  placed a hair differently every frame, and on those lines the art pixel drawn is left to chance: the pictures wobble.
  The boat's camera sways the same way; a map's has no noise and moves only with the player, hence "mainly in the main menu".
- **Not a hitch.** The mod's per-frame work on the main menu comes to about 9 bytes of garbage a frame, and the game runs
  Unity's incremental collector (`boot.config`: `gc-max-time-slice=3`). Hovering a main-menu button moves nothing: it swaps
  the tile's sprite and tweens its colour (`Blu5_Buttonanim_Image`, `_Color`; no `_Size` anywhere in the scene).
- **The fix** moves each still picture by under half a pixel onto the screen's pixel grid, in every scene with menus, and
  leaves alone what the game slides, animates, scrolls or lays out, the choice dialog's own pictures, and anything someone
  keeps writing.

## 2. What was read

All read-only from his game folder (`D:\Steam\steamapps\common\TDS\TDS\DATA`), with UnityPy here; nothing of it is committed.

| File | Read |
| --- | --- |
| `level1` (Mainmenu) | `Ui` canvas mode 1 (Screen Space - Camera) on camera `Main Camera/UI` (overlay, no post), plane distance 100; `Cameras/Main/cm` noise 0.1 / 0.3; button rects under `Ui/Main/Buttons` (above); 54 `Blu5_Buttonanim_Image`, 5 `_Color`, no `_Size` |
| `level2` (Setup, the boat) | `Ui` on `Maincamera/Ui`; two noise channels |
| `level3` (Game) | `Ui` on `Camera/Ui`; no noise |
| `boot.config` | `gc-max-time-slice=3` (incremental GC) |
| decompiled `Assembly-CSharp.dll` | `Blu4_Button_Animator`, `Blu5_Buttonanim_*`, `Blu1_Ui_Handler.Update`, `Mainmenu_Main` (hover only sets the tip text) |

## 3. The fix

`RWPixels.Update`, every machine, every frame (`00_RogueWavesModeMod.cs`), after `RWMenu`:

- A pass collects the shown `Image` and `RawImage` of every screen-space canvas that holds one of the scene's menus, at most
  every half second, and places 64 a frame, with no garbage.
- Each picture is measured from the canvas's corner where it stood at its start (both ends in one canvas, so the camera's
  sway cancels), and moved to the screen pixel a half pixel always rounds to; a picture caught while its menu slid in is set
  right by the next pass at rest.
- Left alone: pictures with `Blu4_Scaler`, `Blu4_Transition_Animator` or an `Animator`, a layout group's cells, anything in a
  `ScrollRect` or under `Bla_Ui_Confirmation` (RWDialog places the dialog), and a picture whose position someone else wrote
  over ours twice in a row. A position someone writes becomes its new start; flipped parents move the right way; rotated
  ones are skipped.
- Added after the review (follow-up PR, compiled here, not run on the PC): the children of a `Blu4_List_Stacker`'s `Ref`
  (the game's stacker writes them every frame and is no `LayoutGroup`), and a picture while its size isn't a whole number
  of times its canvas's, so a hover's `Blu5_Buttonanim_Size` never makes a grown tile jump; it ends on exactly 1, already
  on whole pixels. P2 now reads as a setup line, not a pass, when the reading sees no flicker at the game's half pixels.
- The Rogue Waves tile's crest was already on whole pixels (`RWAssets.Crisp`, pitfall #156); this reaches the game's own.

## 4. Checks

Compiled here, on the base with the sea chart build (`beab230`): the mode with no define, `ROGUEWAVESMODE_SELFTEST`, `+SHOTS` and `+GAPS`, 0 errors (the warnings are the
existing ones in `92_Gaps.cs` and `96_Flow.cs`); `check_editor.sh`. `TWOPC` and `DIALOGLOOK` show the same reference errors as
on the base (a stub gap, they build on the PC). Code review (high): ten findings, fixed in the same commit series.

Self-test P1-P2 (`94_Pixels.cs`), in the mode's flow on the main menu once its menu rests:

- **P1** Play's, Hero's, Settings', More's and Exit's pictures and the crest stand on whole screen pixels.
- **P2** over 144 drawn frames, no picture's square changes (a channel moving more than 48), then the same with
  `RWPixels.Restore()` putting the game's places back, then on again. The `INFO FINDING P2` line says whether the game's own
  places flicker.

## 5. PC round `Wobble_round1`

At his word ("Yes", 05:57Z, a round on the main menu that puts his builds and save back; "Yes", 06:15Z, to leaving the fixed
mode build in his Mods folder). Code: handoff `TDS_Mod/Handoff/Wobble_round1/`, fetched from `claude/project-thread-ioitue` at
`2a3d907` (the fix at `8950091`, on base `262f2c6`, before the sea chart build), mode kit 1.14.

- **Apply:** check passed, 18 to copy (16 copied, 2 added: `26_Pixels.cs`, `94_Pixels.cs`), 0 to delete, 0 patches; applied
  with its backup in `handoff\backup_Wobble_round1_20261106_012759`.
- **Self-test build:** `ROGUEWAVESMODE_SELFTEST`, compile check passed, queue `ok=1 failed=0` (01:28 local).
- **Self-test** `Run-SelfTests.ps1 -Vessels RogueWavesMode -Also Argo,Yossele,Spool,Uloe,RogueWavesFoes -TimeoutMinutes 30`,
  about 15 minutes, log `RogueWavesMode_20261106_014458`: **pass 168, fail 0, setup 0**. Errors logged 303, the same count
  as round 4 (`20261105_164338`): 305 of its 321 exception lines are `OperationCanceledException`, 16
  `NullReferenceException`; none new.

| Line | Result |
| --- | --- |
| P1 whole pixels | PASS: Play (56,371), Hero (443,444), Settings (59,269), More (59,170), Exit (59,71), the crest (274,394); the last pass looked at 23 pictures and moved 0 (all already placed) |
| P2 the game's own places | INFO: Play (55.5,370.5), Hero (442.5,444), Settings (58.5,268.5), More (58.5,169.5), Exit (58.5,70.5): 5 of 6 on half pixels; the crest (274,394) is ours already |
| P2 no flicker on whole pixels | PASS: 0 of 144 frames changed (0 pixels); at the game's places 144 of 144 (up to 759 pixels in one square); back on whole pixels after |
| FINDING P2 | **SEEN**: the game's half pixels flicker in every one of 144 frames, at 2560x1440 |
| The dialog placements (the Rogue Waves button, the Direction pick, the map vote, VENDCINT, the Board, the vote to leave, a saved campaign, a new one over it) | PASS, as before: every card and word on whole pixels, centred |

- **Release build** (no define): compile check passed; the queue stopped behind Unity's "Moving file failed ... Access is
  denied" dialog (moving `Assets/_Parked/Editor/ArgoMod`), Try Again once, as in round 3; then `ok=1 failed=0` (01:45 local).
  The prefab carries no `#define` line: the installed build is the release one.
- **Boot check** of all six parts (`20261106_015627`): every part loaded and ready, icons logged, **0 exception lines**.
  "parts disagree" is the kit-version note only (five parts kit 1.11, the mode 1.14), as before.

## 6. The PC as left

- `TDS\Mods`: the six parts, each enabled. Only `RogueWavesModeMod` changed: the fixed release build (01:45 local;
  `roguewavesmodemod.assets` `58CA2202E5DCDFA8`, 2088783 bytes). The other five were not rebuilt.
- The mode's build before this round: `Desktop\ClaudeProjects\handoff\parked\release_before_Wobble_round1_20261106`
  (`roguewavesmodemod.assets` `934BE548A9FAFC98`).
- His save: `Run-SelfTests.ps1` backs the save up and **does not put it back** (this round's handoff README said it did; it
  is corrected). With TDS closed, the pre-round backup `handoff\save_backup_20261106_012955` was copied back and matches it
  file for file; the save after the round is kept at `handoff\save_after_Wobble_round1_20261106`.
- The Unity project holds `Wobble_round1` applied; `pc_manifest.txt` rewritten (1128 files). TDS closed; Unity open, no
  dialog up. Logs: `handoff\selftest_logs\RogueWavesMode_20261106_014458.log` and `_summary.txt`.

## 7. For Dimitri

- His eye on the main menu and the boat with the fixed build, which is in his Mods folder now.
- Any other place that still shimmers while still: it is a picture this pass leaves alone (§3).

## 8. Not run

- The sea chart build (PR #148) came after this round: the Mods folder's mode build has the fix but not the chart. The
  chart's own PC round replaces it.
- The boat and a map by eye; two players (the fix runs the same on every machine and sends nothing).
