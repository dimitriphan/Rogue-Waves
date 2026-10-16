# The Rogue Waves mode: design pages

One page per piece of the mode, each with its design, how the game's code allows it, and its build and test state.
The code is `TDS_Mod/ModSource/RogueWavesMode`; the engine facts behind it are kit `docs/15`.

| Page | Piece | State |
| --- | --- | --- |
| `SEA_CHART.md` | Picking maps on a generated sea chart; sea weather (§8) | built, PC rounds 1-4 (§7-9); the in-map vote still to build |
| `REJOIN.md` | A dropped player rejoins the running map | built, solo PC round 6 47/0 (0.2.1); 0.3.0's restart and load fixes compiled only; the online checks (§6) need a friend |
| `MAIN_MENU_START.md` | A Rogue Waves button on the main menu starts or continues a campaign | proven feasible by RogueLab probes (round 6); not built in the mode yet |
| `MECHANIC_IDEAS.md` | Ideas from the game's own systems; Dimitri picked sea weather | a menu to pick from |
