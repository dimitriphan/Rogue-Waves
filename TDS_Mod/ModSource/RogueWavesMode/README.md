# RogueWavesMode: the pack's mode part

The Rogue Waves mode as a part of the pack (`docs/PACK.md`): prefab `RogueWaves_Mode_Default`, built and self-tested as
the dev mod `RogueWavesModeMod`. It grows piece by piece (`docs/PLAN.md` Part C, Phase 5 pieces). **0.1.0
(2026-10-10): the sea chart and sea weather** (`docs/mode/SEA_CHART.md` §7-9); PC round 1 passed 34/0 (2026-10-11). 0.1.1-0.1.3
fix Rising Water's flooded tiles, which round 1 found missing; **round 4 passed 38/0** (2026-10-11). **0.2.0
(2026-10-11): rejoining a running map** (`docs/mode/REJOIN.md`), in every map while the pack is on; **0.2.1 PC round 6 passed 47/0** (2026-10-11; round 5's one
failure fixed: the lobby is reopened every frame); the drop and rejoin themselves wait for an online test. **0.3.0
(2026-10-15, compiled, not run): fixes from the decompile** (kit `docs/19`, `23` §6): a friend who restarts the game
keeps their `Sessionid` and gets their seat back, one player who can't load a map is sent back instead of everyone
going to the main menu at 120 s, and a save-slot switch leaves the campaign in its own slot. **0.1.0-dev
(2026-11-03, compiled, not run): the mode polish** (`docs/reports/MODE_POLISH_2026-11-03.md`): the part now carries the
pack version plus `-dev` (`docs/PACK.md` §4); 3 retries (a lost map is replayed, the host leaving a running map loses
it); a map is won once Tide 15 is cleared and ten won maps complete the campaign; 60 s breaks; the save keys carry `RW_`
(old keys are renamed once); a chart map links to at most 3. **Since then, all of it has run on the PC:** the
self-test passed 148/0 on the merged base (2026-11-04), and with a second game copy as a real second player the
drop and rejoin, the restart rejoin and the load watchdog passed (`docs/reports/TEST_ROUND_2026-11-04.md`); only
Steam's own Join Game is left. **2026-11-05, compiled, not yet run in game:** the code reviews' fixes, the build of the
unbuilt candidates (the Any and base vessels' Directions, the Duos, Claude's 11 Trinkets and 4 Cardinals, the decisions'
code items) and kit 1.12 (`docs/reports/BUILD_UNBUILT_2026-11-05.md`, whose §5 is the next PC round's list).

| File | What |
| --- | --- |
| `Code/00_RogueWavesModeMod.cs` | entry point, boot, the per-frame driver, `RWData` (names, version), `RWLog` |
| `Code/01_Util.cs` | database and save helpers, ground tiles, enemy cores; `RWKeys`: the save's `RW_` keys and the rename of the old ones |
| `Code/02_Mode.cs` | `Mode_RogueWave` (Mode_Normal's Tides, 60 s breaks) and an `RW_` twin of each of the 45 base Gameplans; the chart's map pool |
| `Code/03_Chart.cs` | the seeded chart as data, an exact port of `concept-tools/sea_chart.py` (no Unity types) |
| `Code/04_World.cs` | the chart as a world: built on every machine from its name `RW_Chart_<seed>_<route>` |
| `Code/05_Boat.cs` | on the boat: register, warp, blacklist (only the lost map during a replay), sail to a choice, pick it, keep the save clean, take the party home when no campaign is on |
| `Code/06_Campaign.cs` | the host's campaign: seed, route, retries, the map to replay and the map in play in the save; `New`, `Continue`, `Stop`; a map won at Tide 15, lost at its end or when the host leaves it; `Live` leaves it behind after a slot switch |
| `Code/07_Weather.cs` | Rising Water, Quickening Current, Nesting Ground; each one's look (light mist, storm grey, dusk), set by every machine (2026-11-05) |
| `Code/08_Icons.cs` | the weather icons for the warning popup |
| `Code/09_Rejoin.cs` | a dropped player rejoins the running map: the host keeps seats and opens the lobby while one is empty; the rejoiner starts its camera, HUD, vessel and weather |
| `Code/10_Online.cs` | `RWIdentity`: every machine keeps its player GUID for a restart; `RWLoad`: the host sends back a player whose map never loads |
| `Code/99_SelfTest.cs` | the PC round (`ROGUEWAVESMODE_SELFTEST` only) |
| `Build-RogueWavesModeMod.ps1` | joins the code with the kit (`RogueWavesModeKit`), compile-checks it on the PC |
| `../../Assets/RogueWavesModeMod/Editor/RogueWavesModeModPrefab.cs` | Tools > RogueWavesMode: Build Mod Prefab, Export Mod |

**2026-11-03, the whole campaign** (`docs/reports/BUILD_2026-11-03.md`): the main menu's button, the staging map's picks and vote,
the campaign card, Pure Aura, VENDCINT, Directions, Cardinals, objectives, Fund Transfer, the votes, bonus Tides, end titles and
the difficulty. **Self-test in game: 140 pass, 0 fail**, a whole campaign flow clicked through the game's own dialogs, then the older
chart, weather, rejoin and retry probes. `96_Flow.cs` is the flow, `97_Shots.cs` the captures, `95_Look.cs` a quick round for the
chart's look (`-Define ROGUEWAVESMODE_SELFTEST,ROGUEWAVESMODE_SHOTS,ROGUEWAVESMODE_CHARTLOOK`).

| File | What |
| --- | --- |
| `Code/11_Dialog.cs` | `RWDialog`: the game's choice dialog for every choice, its cards centred, four or fewer in one row, more in two |
| `Code/12_Menu.cs` | `RWMenu`: the main menu's button; `RWAssets`: the sprite library |
| `Code/13_Card.cs` | `RWCard`: the campaign card on four screens, the exit warning, the end titles; `RWLobby`: no-mods warning, rejoin line; `RWTips` |
| `Code/14_Run.cs` | `RWPlayer`, `RWRun`: each player's Pure Aura, Directions and Trinkets, and the team's Cardinals, saved with the campaign; `RWMine`: the same seen from a client |
| `Code/15_Content.cs` | `RWContent`: VENDCINT, the buttons and upgrades the part builds at boot |
| `Code/16_Directions.cs` | `RWDirections`: offers, ranks, reach through an owner hook; `RWHits`: Made of Tougher Stuff's hit hook |
| `Code/17_Cardinals.cs` | `RWCardinals`: the pack's Cardinals and Trinkets and the stock Cardinals on sale, stacks as numbered copies |
| `Code/18_Shop.cs` | `RWShop`: VENDCINT's landing, shelf, board; `RWFund`: Fund Transfer |
| `Code/19_Objectives.cs` | `RWObjectives`: the five optional objectives |
| `Code/20_Vote.cs` | `RWVote`: the flow between Tides, Direction picks, the map vote, the vote to leave; `RWAsk`: every machine's own question |
| `Code/21_Difficulty.cs` | `RWDifficulty`: each map's Tide table, modifiers, bonus Tides |
| `Code/25_Models.cs` | `RWModels`: the part's model library (`RWAssets_Library/Models`, built from `Assets/RogueWavesModeMod/Editor/RogueWavesModeModels.cs`), lit with a copy of the game's tower material; the meteor's white crystal `RW_Meteor` (2026-11-05) |
| `Code/98_SaveFuzz.cs` | `RWSaveFuzz` (`ROGUEWAVESMODE_SELFTEST` only): F1-F7, seeded random and damaged values through the campaign save, in the self-test and in `Tools/cloud-check/save_fuzz.py` |

The main menu's Rogue Waves button (`docs/mode/MAIN_MENU_START.md`) calls
`RWCampaign.New(0)` or `RWCampaign.Continue()`. Until then a release build only registers the mode and its twins.

Check here (`TDS_Mod/Tools/cloud-check/README.md`):

```bash
cd TDS_Mod/ModSource
python3 ../Tools/cloud-check/check_mod.py RogueWavesMode/Code Shared RogueWavesMode ""
python3 ../Tools/cloud-check/check_mod.py RogueWavesMode/Code Shared RogueWavesMode "ROGUEWAVESMODE_SELFTEST"
../Tools/cloud-check/check_editor.sh ../Assets/RogueWavesModeMod/Editor
python3 ../Tools/cloud-check/save_fuzz.py          # the save fuzz (98_SaveFuzz.cs), run here on a stand-in save
```

Chart parity with the script: compile `Code/03_Chart.cs` with a small `Main` that reads the pool from
`sea_chart.py`'s `POOL` and prints `RWChart.Dump` (mono `mcs`), then diff against `python3 concept-tools/sea_chart.py
--check <seeds>`; 400 seeds matched on 2026-10-10, and seeds 1-2000 plus 20261009 on 2026-11-03 (after the 3-link cap).
