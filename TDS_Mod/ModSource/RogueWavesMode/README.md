# RogueWavesMode: the pack's mode part

The Rogue Wave mode as a part of the pack (`docs/PACK.md`): prefab `RogueWaves_Mode_Default`, built and self-tested as
the dev mod `RogueWavesModeMod`. It grows piece by piece (`docs/PLAN.md` Part C, Phase 5 pieces). **0.1.0
(2026-10-10): the sea chart and sea weather** (`docs/SEA_CHART.md` §7-9); PC round 1 passed 34/0 (2026-10-11). 0.1.1-0.1.2
fix Rising Water's flooded tiles, which round 1 found missing (round 2: 35/3, all Rising Water; round 3 is next).

| File | What |
| --- | --- |
| `Code/00_RogueWavesModeMod.cs` | entry point, boot, the per-frame driver, `RWData` (names, version), `RWLog` |
| `Code/01_Util.cs` | database and save helpers, ground tiles, enemy cores |
| `Code/02_Mode.cs` | `Mode_RogueWave` and an `RW_` twin of each of the 45 base Gameplans; the chart's map pool |
| `Code/03_Chart.cs` | the seeded chart as data, an exact port of `concept-tools/sea_chart.py` (no Unity types) |
| `Code/04_World.cs` | the chart as a world: built on every machine from its name `RW_Chart_<seed>_<route>` |
| `Code/05_Boat.cs` | on the boat: register, warp, blacklist, sail to a choice, pick it, keep the save clean |
| `Code/06_Campaign.cs` | the host's campaign: seed and route in the save; `New`, `Continue`, `Stop` |
| `Code/07_Weather.cs` | Rising Water, Quickening Current, Nesting Ground |
| `Code/08_Icons.cs` | the weather icons for the warning popup |
| `Code/99_SelfTest.cs` | the PC round (`ROGUEWAVESMODE_SELFTEST` only) |
| `Build-RogueWavesModeMod.ps1` | joins the code with the kit (`RogueWavesModeKit`), compile-checks it on the PC |
| `../../Assets/RogueWavesModeMod/Editor/RogueWavesModeModPrefab.cs` | Tools > RogueWavesMode: Build Mod Prefab, Export Mod |

Nothing starts a campaign on its own yet: the main menu's Rogue Wave button (`docs/MAIN_MENU_START.md`) will call
`RWCampaign.New(0)` or `RWCampaign.Continue()`. Until then a release build only registers the mode and its twins.

Check here (`TDS_Mod/Tools/cloud-check/README.md`):

```bash
cd TDS_Mod/ModSource
python3 ../Tools/cloud-check/check_mod.py RogueWavesMode/Code Shared RogueWavesMode ""
python3 ../Tools/cloud-check/check_mod.py RogueWavesMode/Code Shared RogueWavesMode "ROGUEWAVESMODE_SELFTEST"
../Tools/cloud-check/check_editor.sh ../Assets/RogueWavesModeMod/Editor
```

Chart parity with the script: compile `Code/03_Chart.cs` with a small `Main` that reads the pool from
`sea_chart.py`'s `POOL` and prints `RWChart.Dump` (mono `mcs`), then diff against `python3 concept-tools/sea_chart.py
--check <seeds>`; 400 seeds matched on 2026-10-10.
