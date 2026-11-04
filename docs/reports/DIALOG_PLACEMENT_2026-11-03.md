# The choice dialogs' placement, 2026-11-03

Dimitri, in the project: "The UI for VENDCINT and selecting the Rogue Waves button is too far to the side, cutting off many options."

## What was wrong

His game runs at 2560x1440. The dialog stood about 500 pixels left of centre, with a dark band across the middle of the screen and the dim stopping short of the right side. A card's hover line, which runs one line to the left of the cards, went off the left edge of the screen, so most of a Cardinal's description couldn't be read.

The cause is timing. The stock dialog slides in by `Main`'s anchors for about a second, starting some 0.55 s after it opens (kit `docs/20` §2, pitfall #137). The mode's code moved `Main` to centre the block at open and once more at 0.5 s, so it measured a dialog that hadn't arrived, and the slide then carried it past the middle. The map vote still came out centred, which hid the fault in earlier rounds; my reading, not proven, is that its menu was already up, so it had no slide. Some earlier captures, such as the shop's, were also taken before the dim had come up.

## What it does now

`RWDialog.Fit` in `ModSource/RogueWavesMode/Code/11_Dialog.cs`, for every dialog the mode asks, including the three that used to stay in the corner (the two-card main menu, New Campaign, the Board):

- `Main` is never moved: the dim covers the screen and the dark edge stays at the right, as in the base game.
- The cards and the title are anchored to the middle of `Main` and arrive with its slide. They are written again just before each frame is drawn, since the stock transition writes the list's anchors back.
- The hover line wraps at 440 units, centred under the cards, and the block keeps room for the longest line among its cards.
- Four cards or fewer stand in one row; more stand in two even rows.
- A block too large for the screen takes the shape that stays largest, and is scaled to fit.
- The corner's rule closes round the cards rather than running on to the screen's bottom.
- The cards and the title stand on whole screen pixels, as the stock corner does, and a block that shrinks does so to a whole number of screen pixels to one.
- Sizes come from letter counts, not from asking the game's text, for the reason below.
- Everything goes back once the dialog has gone, so the game's own Exit prompt keeps its corner.

## The stutter, 2026-11-04

Dimitri, after trying that: "All of the buttons have very tiny stutters."

His message doesn't say whether he meant a hitch on clicking, a roughness in the cards' edges, or both. I asked and hadn't heard back, so this fixes both things the measurements found, on my reading:

- **The freeze.** My first version asked the dialog's own text for its size (`GetPreferredValues`). That took 0.45 ms itself, but every opening then had one frame of 83 to 146 ms. The game's own dialog stayed at 7 ms a frame, with this screen's refresh at 144 Hz. Taking steps out one at a time showed the measuring caused it, and turning the title's autosize off added a 62 to 70 ms frame on 2 of 10 openings. Both are gone: 0 of 10 openings has a frame over 25 ms, the same as stock. I didn't trace why the measuring is so costly (pitfall #138).
- **Half pixels.** The cards stood half a pixel off the screen's grid, for example at y 796.485, where the stock corner stands on whole pixels. The art is drawn without smoothing. Captures at full size showed only a slight difference. They now stand on whole pixels, and a check holds them there.

What didn't turn out to be the cause: nothing moved. In the frame-by-frame measure, a button at rest stayed still, a hovered one neither moved nor scaled, and the main menu held 144 Hz with the mode's per-frame code on or off. A hover showed no long frame once my test's own big log lines were out of the way.

A launch's first opening still has one frame of about 60 ms, down from about 300 ms. I have no stock figure to compare it with, because a launch has only one first opening.

## Checks in game

Every line reads the rectangles at rest, once the slide has ended, and hovers each card.

| Round | Result | Log |
| --- | --- | --- |
| The quick round on the main menu: ten openings against stock, each card hovered, then 2, 3, 4, 5, 8, 12, 16 and 30 cards at 2560x1440, 1920x1080, 1280x720 and 1024x768, with a hover line longer than any the shop writes, each card answering a click | **70 pass, 0 fail** on the final code. An early try failed 5, at the small sizes: the fit-to-screen scale came from that | `RogueWavesMode_20261104_001449` |
| The whole campaign with every part enabled: the Rogue Waves button with and without a saved campaign, New Campaign over a saved one, the Direction pick, the map vote, VENDCINT's shop, the Board, the vote to leave, and the stock Exit prompt back in its corner | **150 pass, 0 fail**. This ran before the stutter fix; the quick round ran after it | `RogueWavesMode_20261103_230023` |
| Every part's release build, booted together | no exception, all enabled | the `-BootCheck` run at 23:05 |

Exceptions in the campaign round are the stock ones of section 7 of the build report, in the same counts as that round (265 cancelled delays in Great Spear's effect, 13 `Newscene` writes, the menu's 3 cancels on a scene change); none is from the dialog. The game's screen size came back to 2560x1440 after the quick round changed it, in the running game and in its saved settings.

Logs are in `handoff\selftest_logs`, captures in `shots_20261103\DialogLook3` and `shots_20261103\Mode11`; nothing from there is committed, because they hold base-game art. The quick round is `Code/94_DialogLook.cs`, under `ROGUEWAVESMODE_DIALOGLOOK`.

## Limits

- Sizes from letter counts are estimates, so a frame can stand a little wider than its title. A very long title shrinks to fit its box.
- A UI scale other than 3 wasn't set by hand. The screen sizes covered smaller canvases than any scale at 2560x1440 gives; a larger canvas only has more room.
- A real shelf reached only the cards the test held. More cards were tried with pretend cards on the main menu.
- Fund Transfer's dialogs need a friend online: they take the same code, but no check has seen them.