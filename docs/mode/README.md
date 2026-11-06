# The Rogue Waves mode: design pages

One page per piece of the mode, each with its design, how the game's code allows it, and its build and test state.
The code is `TDS_Mod/ModSource/RogueWavesMode`; the engine facts behind it are kit `docs/15`.

| Page | Piece | State |
| --- | --- | --- |
| `SEA_CHART.md` | Picking maps on a generated sea chart; sea weather (§8); retries and the campaign's end (§7) | built, PC rounds 1-4 (§7-9); 0.1.0-dev's retries, Tide 15 wins, 60 s breaks, `RW_` keys and the in-map vote run in game (`docs/reports/BUILD_2026-11-03.md`, `docs/reports/TEST_ROUND_2026-11-04.md`) |
| `CHART_NAVIGATION.md` | How the players read and pick on the sea chart: a whole-chart view at every vote and on a key, the boat's banner and mini chart; the Cove (rest) and Wreck (treasure) stops (2026-11-06) | built 2026-11-06, compiled here, not yet run on the PC (`docs/reports/CHART_BUILD_2026-11-06.md`); mockups in the game's look `docs/concepts/art/chart_look_*.png` (first layout draft `chart_view_*.png`) |
| `MVP_BOARD.md` | A multiplayer board after each won map and at the campaign's end: one accolade and one failure per player, an MVP (2026-11-06) | proposal, not built; mockup `docs/concepts/art/mvp_board_map.png` |
| `REJOIN.md` | A dropped player rejoins the running map | built, solo PC round 6 47/0 (0.2.1); run with a real second player on 2026-11-04 (§6): Steam's Join Game and a real network's drop are left |
| `MAIN_MENU_START.md` | A Rogue Waves button on the main menu starts or continues a campaign | built (`Code/12_Menu.cs`) and run in game since 2026-11-03 (`docs/reports/BUILD_2026-11-03.md`) |
| `MECHANIC_IDEAS.md` | Ideas from the game's own systems; Dimitri picked sea weather | a menu to pick from |
