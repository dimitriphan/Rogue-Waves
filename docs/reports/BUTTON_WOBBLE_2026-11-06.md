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
- The Rogue Waves tile's crest was already on whole pixels (`RWAssets.Crisp`, pitfall #156); this reaches the game's own.

## 4. Checks

Compiled here: the mode with no define, `ROGUEWAVESMODE_SELFTEST`, `+SHOTS` and `+GAPS`, 0 errors (the warnings are the
existing ones in `92_Gaps.cs` and `96_Flow.cs`); `check_editor.sh`. `TWOPC` and `DIALOGLOOK` show the same reference errors as
on the base (a stub gap, they build on the PC). Code review (high): ten findings, fixed in the same commit series.

Self-test P1-P2 (`94_Pixels.cs`), in the mode's flow on the main menu once its menu rests:

- **P1** Play's, Hero's, Settings', More's and Exit's pictures and the crest stand on whole screen pixels.
- **P2** over 144 drawn frames, no picture's square changes (a channel moving more than 48), then the same with
  `RWPixels.Restore()` putting the game's places back, then on again. The `INFO FINDING P2` line says whether the game's own
  places flicker.

## 5. PC round `Wobble_round1`

Pending.

## 6. For Dimitri

- His eye on the main menu and the boat with the fixed build.
- Any other place that still shimmers while still: it is a picture this pass leaves alone (list above).
