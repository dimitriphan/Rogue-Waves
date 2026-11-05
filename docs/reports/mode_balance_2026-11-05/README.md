# Mode balance review simulator, 2026-11-05

Behind `../MODE_BALANCE_REVIEW_2026-11-05.md`. Python 3, standard library only, no game code. It imports the frozen
simulator of `../balance_upgrades_2026-11-04/` unchanged and wraps it; copy this folder to a new dated one before
changing it.

| File | What |
| --- | --- |
| `mode.py` | the mode's Tide table for map k as `RWDifficulty.Apply` builds it, and every proposal as a keyword (`finish` F1, `compound` F2, `health_curve` F3/F5, `mods_from` and `rotate` F4); `power` and `cost` tune knobs on the player's side |
| `w50.py` | W50: the power at which a vessel wins half its runs, by bisection (`W50_HI` sets the ceiling) |
| `power.py` | the estimate of what a player carries into each map (low, mid, high; `ramp` for C1) |
| `pressure.py` | each Tide's foe health, Downpour and the mode's maps, with no simulation |
| `make_experiments.py` | `experiments/exp_A` (as built against the base game), `exp_B` (F1, F2, R), `exp_C` (S), `exp_D` (S2) |
| `summary.py`, `chart.py` | the report's tables (`results/summary.txt`) and chart (`power_vs_pressure.svg`) |
| `results/` | `res_A`-`res_D.json` (12 seeds a setup), `power.json`, `pressure.json`, the run logs |

Run: `python3 make_experiments.py && python3 w50.py experiments/exp_A.json results/res_A.json 12 7` (about 50 minutes
on 4 cores; `exp_B` and `exp_C` with `W50_HI=16` and `24` and 8 steps, about 40 and 20; `exp_D` with `W50_HI=32`, about
15), then `python3 power.py results/power.json`, `python3 summary.py > results/summary.txt` and `python3 chart.py
results/res_A.json results/power.json results/res_D.json power_vs_pressure.svg S2`.
