# Smoothness in real play, 2026-11-04

Dimitri, in the project: "I am unable to test that at the moment, continue with other test features for now." "That" is the buttons' tiny stutters. `TEST_ROUND_2026-11-04.md` had already run everything else a thread could reach, so this round took the one gap left: nothing had timed frames in real play. The earlier dialog check timed pretend cards on the main menu only.

**In short:** nothing here is the mod's to fix. Play holds 144 Hz nearly all the time. In a running map, a dialog's opening and a card's hover cost the same in the mode's layout as in the game's own. VENDCINT's landing is clean. Whether this is the stutter he saw is still his to say.

Everything ran on his PC at 2560x1440 with every part enabled. No shipped code changed: the timer sits behind `ROGUEWAVESMODE_FEEL` and `ROGUEWAVESMODE_FEELMAP`. Logs are in `handoff\selftest_logs`.

## 1. A whole campaign, every frame

Two runs of the mode's flow: **148 pass, 0 fail** each. The table is from the first (`RogueWavesMode_20261104_115232`), since the second (`RogueWavesMode_20261104_121526`) added a log line at each long frame. A frame at 144 Hz takes 7 ms.

| Where | Frames | Median | p99 | p99.9 | Worst | Over 25 ms |
| --- | --- | --- | --- | --- | --- | --- |
| Main menu | 4,299 | 7 ms | 7 ms | 21 ms | 76 ms | 4 |
| Setup | 3,956 | 7 ms | 7 ms | 21 ms | 76 ms | 2 |
| A map, a break | 23,467 | 7 ms | 7 ms | 49 ms | 361 ms | 27 |
| A map, a Tide | 51,947 | 7 ms | 7 ms | 14 ms | 215 ms | 11 |

The second run reads much the same, with p99 at 14 ms in a break and a Tide and a worst frame of 368 ms. A scene's first 15 s and each dialog's first 1.8 s are counted apart from this table.

**Where the long frames fall,** from the second run's log order. That shows what came just before a frame, not what caused it:

- The longest, 320 and 368 ms, follow the game's own end-of-map line `SETTINGS END OF GAME JSONS`, and 160 ms ones sit beside it at a map's end. I read these as stock.
- 145 to 215 ms frames fall while the test's own on-screen check runs on the map vote, the shop and the vote to leave. The timed hovers in section 3 didn't reach them, so I read them as the check's.
- 48 to 90 ms frames recur near the change of a Tide and a break, where the log shows an objective posted or VENDCINT landing. Section 2 clears the landing by itself. Posting an objective is a dice roll and a log line. I didn't time a stock map's Tide change beside these, so they are unattributed.

## 2. In a running map, against the game's own dialog

The clock held, opened from the live HUD as the mode's dialogs are. "Stock" is the same menu and `Runprompt` with nothing of the mode's placing. Each line is 10 openings, and counts those with a frame over 25 ms (`RogueWavesMode_20261104_115934`, `_122342`):

| Opening | First run | Second run |
| --- | --- | --- |
| The mode's layout, test cards | 2, worst 70 ms | 1, worst 76 ms |
| Stock | 1, worst 63 ms | 1, worst 70 ms |
| VENDCINT's own shop, as its button opens it | 0, worst 14 ms | 1, worst 76 ms |

So the occasional single long frame is on both sides, at about the same rate. **VENDCINT's landing**, the host's whole call, 6 times: worst frame 14 ms.

## 3. Hovering real cards

The real shop's cards, by the pointer's own calls (`RogueWavesMode_20261104_122838`). The earlier check's hovers were on the main menu, which doesn't show this.

| Hovered | The mode's layout | The game's layout, same words |
| --- | --- | --- |
| A Cardinal, 57 to 111 letters | 21 ms | 21 ms |
| A Direction, 70 letters | 14 ms | 14 ms |
| Back, 4 letters | 7 ms | 7 ms |
| A stray long frame, anywhere among the above | 2 of 14 hovers, both 69 ms | 2 of 14, 49 and 70 ms |

Setting the words alone, with no button or sound, costs the same (21 ms, and once 90 ms), and the setting itself takes 0.01 ms, so the time goes when the frame is drawn. On the main menu the same kind of change showed no long frame. So one or two dropped frames on a changed line of text is how the game behaves in a map, in its own layout too. I didn't trace why, and I left it.

If this is what he saw, it is the base game's. I'd expect it to show more in the mode, because its cards are hovered for their words. Shorter hover lines wouldn't clear it: 70 letters still costs a dropped frame.

## 4. What this can't say

- **A big fight isn't timed.** The flow clears each Tide's foes, so these Tides are short and thin. A long fight's cost is mostly the game's, and it isn't measured here.
- **No game without mods.** "Stock" above means the game's own layout inside the same launch, with every part loaded. A launch with no mods can't run this timer.
- **One PC.** A strong machine at 144 Hz; a dropped frame at 60 Hz shows differently.
- **Frame times, not eyes.** Whether any of it is what he calls a stutter is his to say.

## 5. What is left for Dimitri

Unchanged from `TEST_ROUND_2026-11-04.md` §5: the stutter by eye, his Pin Cushion wall, Steam's Join Game and a real network.

## 6. Running it again

```bash
powershell -Command "& '.\Build-RogueWavesModeMod.ps1' -Define ROGUEWAVESMODE_SELFTEST,ROGUEWAVESMODE_FEEL"
```

That build times a whole flow. Add `ROGUEWAVESMODE_DIALOGLOOK,ROGUEWAVESMODE_FEELMAP` for section 2 and 3's comparison, which stops at the shop. Leave `ROGUEWAVESMODE_SHOTS` out, since a capture's big log lines make their own long frames.
