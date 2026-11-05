# Balance simulator sweep, 2026-11-05

The runs behind `../BALANCE_SWEEP_2026-11-05.md`. Python 3, standard library only, no game code. Frozen with the
report. It imports the earlier simulators unchanged: `../mode_balance_2026-11-05/` (the mode's Tide table and the W50
bisection), `../balance_upgrades_2026-11-04/` (the vessels, Smacky, Yossele and Spool) and `../balance_argo_2026-11-04/`
(Argo).

| File | What |
| --- | --- |
| `power2.py`, `results/power.json` | the power a player carries into each map (low, mid, high; with and without C1's ramp), with the shelf as built and P1 |
| `make_experiments.py` | `experiments/exp_E1.json` (Tide 15's row tried by map), `exp_E2.json` (the pick, 16 seeds, with Yossele's and Spool's checks), `exp_E3.json` (Argo), `exp_E4.json` (F5's share at 2%, maps 2, 5, 8, 9, 10) |
| `fit.py`, `results/fit.txt` | the fit from `res_E1.json`: Tide 15's row on each map and the health curve, to the target D |
| `results/res_E1.json` to `res_E4.json` and their `log_*.txt` | the W50 runs |
| `argo/w50_argo.py` | the bisection on the Argo review's simulator |
| `spool/spoolsim.py`, `spool/make_spool.py` | Spool on one Pin Cushion: the runner with `dart_rng` and `keep_wall` added by wrapping, and passes A-D |
| `spool/results_*.json`, `spool/log_*.txt` | Spool's runs |
| `charts.py`, `difficulty_by_map.svg`, `spool_one_wall.svg` | the report's two charts |

Run it, from this folder (4 cores):

    python3 power2.py results/power.json
    python3 make_experiments.py
    W50_HI=48 python3 ../mode_balance_2026-11-05/w50.py experiments/exp_E1.json results/res_E1.json 12 7   # about 45 minutes
    python3 fit.py
    PICK='{"rows": [...], "lift": ..., "per_map": ..., "share": ...}' python3 make_experiments.py
    W50_HI=48 python3 ../mode_balance_2026-11-05/w50.py experiments/exp_E2.json results/res_E2.json 16 8
    W50_HI=48 python3 argo/w50_argo.py experiments/exp_E3.json results/res_E3.json 8 7
    W50_HI=48 python3 ../mode_balance_2026-11-05/w50.py experiments/exp_E4.json results/res_E4.json 16 8
    cd spool && python3 make_spool.py && python3 spoolsim.py experiments/spool_A.json 40 results_A.json   # B-D at 30
    cd .. && python3 charts.py
