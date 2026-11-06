# Sea chart navigation, built (2026-11-06)

Dimitri, 05:59Z: **"Go"** on `docs/mode/CHART_NAVIGATION.md` (the chart view, Coves and Wrecks, the boat's banner and
mini chart, key M, self-tests CV1-CV12). Built with the page's §7 defaults as written; his "go" didn't name them, so
each stays his to change (scope `04` keeps them in tildes). **Compiled here, not run on the PC.** The PC was held by
another round while this was built.

## 1. What was built

| Where | What |
| --- | --- |
| `03_Chart.cs`, `concept-tools/sea_chart.py` | The stops' second stream (`seed ^ 0x5EA57095`): one Cove in rows 4 and 9, one Wreck in rows 2-3 and 6-8, all calm. The first stream is untouched, so a chart's islands, links and weather stay as they were. `sea_chart.py --stops --check` prints the C# `CHART` lines; Python and C# agree on 12 seeds and modes (`20261009`: Coves `4_0`, `9_3`; Wrecks `2_0`, `8_1`, as the mockups) |
| `04_World.cs`, `06_Campaign.cs` | `S` in the chart world's name (`RW_Chart_<seed>S_<route>r<retries>`), saved as `RW_Campaign_Stops`; an older campaign keeps its chart without stops. A Cove's and a Wreck's pixel mark on their islands |
| `06_Campaign.cs` `Rest`, `05_Boat.cs` | The Cove: the boat sails there (it is never a map: always blacklisted, the host's sail hovers it), and the rest is one save write: the route a row on, a retry back (or, with all 3 left, each player's lowest held Direction a rank up, `RWDirections.RankLowest`), and the second vote's winner as the next stop. The party warps chart-to-chart onto the new name and sails on; with no winner, the host is asked. A boat standing at a Cove's dock 1.5 s with no vote rests too |
| `20_Vote.cs` | A Cove that wins the vote asks at once where to sail after it (`M:<row>:<cols>`, `RW_Campaign_After`); "X, then to Y". The Wreck: one row harder (`21_Difficulty.cs`), and after its Tide 15 a "THE WRECK'S HOLD" pick for each player, three Trinkets the shop may sell and they don't hold, given free (`RWCardinals.Give`); a card bought meanwhile from the SHOP falls to the next |
| `26_ChartView.cs` (new) | The whole chart on the left half beside the vote's cards (moved right, `RWDialog.Ask`'s `at`); lit, lettered choices; hovering an island hovers its card; clicking it votes (the card while the dialog is up, a new vote after). Key M opens it in a break or on the boat. On the boat: a banner naming where the boat sails (clients read it from the boat's heading) and a mini chart top right |
| `11_Dialog.cs`, `13_Card.cs` | The dialog's cards placed at a given part of the screen; `Choose` and `Hover` for the islands, only for the chart's own vote; the campaign card says "Row N of 10" on charts with stops |
| `90_Stops.cs` (new), `96_Flow.cs`, `99_SelfTest.cs` | CV1, CV3, CV9 at boot; CV10-CV12 on the boat after the retries; CV2, CV4, CV5 in the flow's staging vote. Older checks play charts without stops (`RWCampaign.StopsOnNew` off at boot) |
| `98_SaveFuzz.cs`, `Tools/cloud-check/save_fuzz.py` | F5 fuzzes names with and without `S`; the script copies the stops code and two constants the player line needs (it failed on the base before: `RWTrinkets`, `RWCardinals` missing) |
| `Tools/cloud-check/stubs/ThirdParty.cs` | `EventSystem.current`, `UnityEngine.InputSystem` (`Keyboard`, `Mouse`), `RawImage.uvRect` |

Where the build differs from the page: `CHART_NAVIGATION.md` §6.1 (the title drawn, not copied; pointer and key read
through the InputSystem; the 3D brackets, the 5 s view after a loss and the lane flash not built yet).

## 2. Checks run here

From `TDS_Mod/ModSource`, after merging base `06fb34b`, then again after `8710ba8` (docs only):

- `check_mod.py RogueWavesMode/Code Shared RogueWavesMode ""`: **0 errors, 0 warnings**.
- `... "ROGUEWAVESMODE_SELFTEST"`: **0 errors**, 1 warning (`96_Flow.cs` `before`, on the base before).
- With `SHOTS`, `GAPS`, `CHARTLOOK`, `FEEL`: 0 errors. `DIALOGLOOK`: 2 errors, `PointerEventData` missing from the
  stubs, the same on the base.
- `check_editor.sh ../Assets/RogueWavesModeMod/Editor`: done.
- `save_fuzz.py RogueWavesMode/Code`: **F1-F7 pass** (F5 with stops).
- C# against Python stops: 12 of 12 equal (a scratch harness compiled with mcs; seeds 20261009, 1, 77, 4242, 123456789,
  3000000000, each with and without stops).
- `/code-review high`: 10 findings; 8 fixed (clicks through the view's half, island clicks reaching a later dialog,
  the view over a later dialog, a card left hovered, the boat's "0 s" footer, the Wreck's pay lost on a clash, the
  Cove check every frame, the dialog's shift as a global), CV5's same-frame `Find` fixed. Left: the view redraws
  whole on a hover change (watch for a hitch on the PC, §4 item 9), and `Standing` repeats `RWCard.Here`'s lookup.
- `/simplify` (four readers): fixed the world name parsed twice a frame on the boat (`Keep`, the banner) and in three
  more places, the footer rebuilt every frame, the pointer testing every island, the save's Cove columns read twice, the
  view's own copy of the dialog's teal and coral, dead fields; a gift now starts a Trinket through `RWTrinkets.OnGain`,
  the one place buys use too. Left for later, each larger than this change: one "which node is this map" helper for
  `RWCard.Here`, `Standing` and `RWAsk.Run`; a node's own "plays a map" and "plays as row" in place of the Cove and
  Wreck tests spread over the boat, save and vote; one arrival test for `Sail` and the dock rest; `RWAssets.Crisp` for
  the view's pixel icons (pitfall #156: uneven pixels; §4 item 8 checks the look first); one per-player offer question
  for the Direction picks and the Wreck. After both passes: release 0/0, self-test 0 errors, save fuzz F1-F7 pass.

## 3. What only the PC can show

Nothing here was launched; every line below is unproven until a PC round.

## 4. The PC round's list

1. The mode's self-test (`Run-SelfTests -TimeoutMinutes 30`): CV1-CV5, CV9-CV12 pass; the older flow keeps its count.
2. **Clicks on the chart beside the dialog** (CV4): a click on a lit island votes; the dialog's cards still click; the
   shade takes clicks on the left half and nothing reaches the map under it.
3. The chart-to-chart warp at a Cove with the boat up (CV10), and `Continue` at a Cove (CV11).
4. The host's sail **hovers a blacklisted Cove** and arrives (CV10); the dock rest by the stock arrows.
5. A Wreck's map: the log's difficulty line shows the next row; after Tide 15 the pick, then the vote to leave (CV12 by
   hand, then two players).
6. Two players: a client's banner names the island from the boat's heading (CV6); the Cove's second question and a
   re-vote from a client.
7. Key M on the boat and in a break; typing "m" in chat does nothing (CV7).
8. His eye (CV8): the layout at 1080p and his UI scale, beside the cards, with the Setup panel; a pad pick; the pixel
   icons.
9. A hitch when the pointer crosses islands (the view rebuilds on each hover change); if seen, keep the tiles and
   recolour only the brackets and lanes.
