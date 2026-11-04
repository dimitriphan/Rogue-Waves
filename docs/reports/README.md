# Reports

Dated reports and reviews. Each is frozen as written; where something changed since, a note at its top says so. Living
pages are one folder up (`docs/`); the mode's design pages are in `docs/mode/`.

Names: since 2026-11-04 (Dimitri's word) the pack's Vessel Cardinals are **Trinkets** and its Team Cardinals just
**Cardinals**. Reports written before keep the old names; each that uses them says so at its top.

| Report | Date | What |
| --- | --- | --- |
| `SIMPLIFICATION_BUILD_2026-11-04.md` | 2026-11-04 | The code simplification built: Uloe's dead route and Argo's leftovers gone, the game's own upgrade AI on pack buildings, kit 1.8's shared helpers and asset reader (about 2,200 lines less, no number changed); why finding 1 waits; the PC list T0-L1 |
| `CODE_SIMPLIFICATION_2026-11-04.md` | 2026-11-04 | The mod's code read against the base game's (decompiled again): ten ways to simplify, ranked by what they remove and their risk to the proven rounds. Armour as the stock `Resistadd` stat, the stock upgrade AI back on pack buildings, one shared scaffold; what looks reimplemented but must stay; PC checks R1-R5, G1-G3. Nothing changed |
| `SMOOTHNESS_2026-11-04.md` | 2026-11-04 | Every frame timed through a whole campaign, and the dialogs against the game's own in a map: nothing of the mod's to fix |
| `REPO_QUALITY_CHECK_2026-11-04.md` | 2026-11-04 | The repository checked for stale lines, broken paths, unused files and scope/code mismatches: 21 issues, none breaking a build; fixed in the same PR (§8) |
| `TEST_ROUND_2026-11-04.md` | 2026-11-04 | Every feature that had not been tested, run on the PC: the online half with a real second player on the loopback address, the solo gaps, the vessels' new checks, the whole pack. Ten bugs fixed; what is left needs Steam, a real network or a person |
| `ULOE_UPGRADES_2026-11-04.md` | 2026-11-04 | Uloe's upgrade trees fleshed out from 8 to 32 (8 a building, a capstone each), the five holes they fill (crowds, colour trade, a parked frail vessel, no slow, no capstones), hand numbers against Smacky's towers by Light (`uloe_upgrades_2026-11-04/`), the route each takes in the next build |
| `ARGO_REVIEW_2026-11-04.md` | 2026-11-04 | Argo's kit reviewed for balance and skill: its slows stack with their own re-ticks; with that fixed it sits between Smacky and Yossele; fourteen changes in scope `09`; simulator with Argo (`balance_argo_2026-11-04/`) |
| `ROGUELIKE_DIRECTIONS_CARDINALS_2026-11-04.md` | 2026-11-04 | The overlap check of those 13 Cardinals against the 40 stock ones (5 removed), then 11 Directions (2 of a new Duo kind) and 7 Cardinals after Risk of Rain 2, Hades, Slay the Spire, Balatro, Isaac and Monster Train |
| `DIRECTIONS_CARDINALS_2026-11-04.md` | 2026-11-04 | 21 new Directions and 13 new Cardinals from StarCraft II's campaign upgrades, at the stock Cardinals' power scale; who made every Direction and Cardinal (Dimitri 6 + 1, Claude 38 + 20); what each rides on in the game's code |
| `UPGRADE_GAPS_2026-11-04.md` | 2026-11-04 | Yossele's and Spool's upgrade trees against the base vessels': Yossele complete but for one trade upgrade (Wood Wide Web); Spool has 15 of a base vessel's ~40, no capstones; 22 proposed upgrades, the hole each fills, priced and checked in a copy of the simulator (`balance_upgrades_2026-11-04/`, 5,040 runs) |
| `BUILD_2026-11-03.md` | 2026-11-03 | Every planned feature built and tested in game: the whole campaign, the foes, Uloe, kit 1.7, the sea chart on the base game's art; self-test 139/0 and every part in one launch |
| `DIALOG_PLACEMENT_2026-11-03.md` | 2026-11-03 | The choice dialogs off-centre at 2560x1440: why, the fix (centred, hover text on screen, shrink to fit) and the stutter it caused, then fixed |
| `BALANCE_RERUN_2026-11-03.md` | 2026-11-03 | The balance simulator re-run on the PC round's measures (`balance_rerun_2026-11-03/`, 11,880 runs, analysis only): the Opal pays every colour, Woozy's first node matters most; with a wall that holds P2 and P3 carry Spool, on a line foes walk round no number helps; P5 barely matters; The Hunt is decided by Blue (fruit, aura beasts) |
| `BALANCE_ROUND_2026-11-03.md` | 2026-11-03 | The PC round behind the balance study: nodes, gatherers and Crafted measured in game (M1-M11); foes walk round a line of walls, so P1-P4 wait for a person's wall |
| `AURA_BASELINE_2026-11-03.md` | 2026-11-03 | What a map pays and what each vessel mines, read from the game's code: one stock gatherer earns 17-36 aura a Tide by a Red node, a base vessel about 4,800 a map; Yossele's Sapling mines 2-2.5× and Spool's Loom 4-5× the base average. Proposals P5-P7 (a slower Sapling, cheaper fruit, Spool's Loom line); P1-P4 stand; PC checks M8-M11 |
| `UX_FEATURES_2026-11-03.md` | 2026-11-03 | Buttons, transition screens, the lobby and other UX ideas checked against the game's own screens (decompiled and read): 33 feasible on stock UI, 5 not possible, five recommended first; ten facts new to the kit and ten solo probes |
| `BALANCE_2026-11-03.md` | 2026-11-03 | Yossele and Spool studied in a simulator calibrated on the 2026-10-26 play test (`balance_2026-11-03/`, about 13,000 runs): Yossele unchanged; Spool's early game is the Pin Cushion wall, its late game needs three numbers (proposals P1-P4); PC test plan |
| `MODE_POLISH_2026-11-03.md` | 2026-11-03 | The mode part checked line by line against the scope: 15 issues fixed as 0.1.0-dev (retries, Tide 15 wins, 60 s breaks, `RW_` keys), the readings and judgment calls for Dimitri, what is not built yet |
| `MISTAKE_CHECK_2026-10-28.md` | 2026-10-28 | Two passes over every file for mistakes (wrong facts, bugs, broken pointers): 31 found, 28 fixed, 3 listed; every mod compiles clean |
| `PRIORITIES_2026-10-28.md` | 2026-10-28 | What the project needs most now: the scope read against the state, ranked for Release 1 (Yossele alone), with the scope gaps and open `~defaults~` blocking each item |
| `ENGINE_STUDY_2026-10-28.md` | 2026-10-28 | The game's code system by system (loading, data, entities, behaviour scripts, stats, upgrades, combat, players, economy and Tides, Cardinals, looks, maps), the four rungs from stock data to new code, and 13 facts new to the kit (seven seen in RogueLab round 8, 2026-10-29; the note on top) |
| `ENGINE_STUDY_2026-10-28_BEHAVIOURS.md` | 2026-10-28 | Its appendix: all 284 stock behaviour scripts indexed with whether a pack can reuse them, how they were checked, the most reusable per group; full entries in `engine_study_2026-10-28/` |
| `SCOPE_FIT_PLAN_2026-10-28.md` | 2026-10-28 | Every idea of scope 00-16 matched to the stock mechanic, class and data path it rides on; none needs a new mechanic; eight probes, all passed in RogueLab round 8 (2026-10-29, §11) |
| `YOSSELE_TEST_PLAN_2026-10-23.md` | 2026-10-23 | Yossele 0.2.0-dev: every case not yet seen in game, which check covers it (PC round 9's self-test, play, by eye, online), and the cloud checks run |
| `PLAYTEST_2026-10-26.md` | 2026-10-26 | Yossele 0.2.0-dev and Spool 0.1.0-dev played through the HUD by a virtual player on Purlest, Woozy and The Hunt: two bugs fixed (the Thread Head's panel, Hallucinogen's look), every upgrade clicked, the balance read, what needs Dimitri's eye |
| `QUALITY_PASSES_2026-10-15.md` | 2026-10-15 | 23 passes over every file (stopped at Dimitri's word after one clean pass); what was fixed, why each pass found issues, what is left for Dimitri |
| `QUALITY_CHECK_2026-10-15.md` | 2026-10-15 | Two quality passes over every file, the open PRs included, and the repository's reorganisation |
| `BASE_VESSEL_CHECKS_2026-10-12.md` | 2026-10-12 | Kit `17` §12 and `16` §9 checked in game by RogueLab round 7 |
| `FULL_TEST_2026-10-11.md` | 2026-10-11 | Every probe set re-run on one build in game: 208 pass, 0 fail |
| `QUALITY_CHECK_2026-10-11.md` | 2026-10-11 | Quality check of the lessons and resources; fixes made in place |
| `SCOPE_REVIEW_2026-10-10.md` | 2026-10-10 | The scope files read against each other and the game; Dimitri's answers folded in |
| `SCOPE_VS_LOOP.md` | 2026-10-09 | Where the run outgrows the first scope: campaign, save, per-player Cardinals (§E, §F) |
| `MECHANICS_CHECK.md` | 2026-10-09 | The 2026-10-09 scope's new mechanics checked against the game's code (all go); the eight that needed the game proven on the PC |
| `SCOPE_GAPS.md` | 2026-10-08 | Every asset the scope names or implies, with what exists and what is missing |
| `REPO_SCAN_2026-10-08.md` | 2026-10-08 | The first scan of this repository |
| `ARGO_NEXT_2026-10-08.md` | 2026-10-08 | The handoff that carried Argo 2.0 to done (was `TDS_Mod/Handoff/Argo_next/`) |
