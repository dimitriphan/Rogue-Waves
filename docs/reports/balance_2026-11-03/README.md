# Balance simulator, 2026-11-03

The simulator behind `../BALANCE_2026-11-03.md` and `../AURA_BASELINE_2026-11-03.md`. Frozen with them; copy it to a
new dated folder before changing it. The aura baseline added `aura.py`, experiment J and four knobs whose defaults
are the numbers the first study used, so A-H reproduce unchanged. Python 3, standard library only, no game code: the game's numbers come from the RogueLab dumps in
`TDS_Vessel_Creator/reference/` (`roguelab_round1.txt`, `roguelab_round2.txt`) and the vessels' `DESIGN.md`.

| File | What |
|---|---|
| `sim.py` | One map, Tide by Tide: lanes, foes, walls, every Yossele, Spool and Smacky building, unit and upgrade the report names; its docstring lists the engine facts it uses |
| `policies.py` | How the simulated player spends: `bot` (the 2026-10-26 virtual player's habits) and `smart` (walls the path, saves for key buys) |
| `tables.py` | Reads the Downpour, High Tide and Flood Tide tables from the dumps |
| `mc.py` | Runs every setup in an experiment file many times on 4 cores and writes a summary: `python3 mc.py experiments/exp_B.json 100 results/res_B.json` |
| `run1.py`, `quick.py`, `summ.py` | One run with its log; eight seeds for a quick look; Markdown tables from a result file |
| `aura.py` | The aura baseline, analytic, no simulation: how a node pays, every base vessel's gatherer and Yossele's and Spool's as the game's code has them, a 15-Tide map's start, kills and mining; `python3 aura.py [out.md]` prints its tables A-C and writes `results/aura_baseline.json`; its docstring lists the code it was read from |
| `experiments/exp_A..H.json` | The setups: A calibration, B baselines, C Spool's first Tides, D Spool's late game, E Yossele's options, F wall widths, G High Tide, H the proposal package |
| `experiments/exp_J.json` | The aura baseline's check: Smacky with the code's Hut, Yossele's Sapling at 0.25 / 0.15 / 0.10, Spool's Loom at 0.4 / 0.15, with and without P1-P3, 60 s breaks |
| `results/res_A..H.json`, `res_J.json` | What the reports quote (means over the runs; `lost_hist` counts the Tide each lost run ended on) |
| `results/aura_baseline.json` | `aura.py`'s numbers |

Tuning knobs a setup can pass in `tune` (defaults are the built numbers): `thread_start`, `free_stitched`, `tin_cost`,
`stitched_cost`, `stitched_hp`, `stitched_dmg`, `dart_dmg` (each added to the built number), `crafted_lvl` (Damage per Tin upgrade), `tin_units`,
`dart_safe` (Darts stand outside the Maul's slam: Range 4.5), `dart_all`/`dart_n` (Darts on every Tin), `cushion_cost`,
`cushion_hp`, `stitched_block`, `chase_miss` (a Crafted's miss chance on a walker, 0.9), `thread_drop_add`,
`loom_speed` (the Loom's spinning), `loom_aspd` (its mining, 0.4), `sapling_aspd` (0.25), `seam_aspd`/`seam_hit` (the
Seam's mining, 0.8 a second at 1 a hit), `hut_code` (Smacky's miner on the code's 6.8 s round trip), `cannon_cost`, `tree_cost`, `up_<Upgrade>` (an upgrade's cost), `laser_mines`, `force_dmds`,
`fruit_pickup`, `seal`/`seal_n` (the wall line and its width), `tin_back` and `tin_every` (where and how fast the smart Spool builds Tins), `ore_scale`, `front` (how far ahead of the line towers
reach, 4 tiles).

Run from anywhere; a full batch (A-H, about 13,000 runs) takes about 40 minutes on 4 cores, experiment J (1,500 runs)
about 7 minutes.
