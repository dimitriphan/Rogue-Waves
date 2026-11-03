# Balance simulator re-run, 2026-11-03

The simulator behind `../BALANCE_RERUN_2026-11-03.md`: a copy of `../balance_2026-11-03/` (left frozen) fed the balance
round's PC measures (`../BALANCE_ROUND_2026-11-03.md`). Python 3, standard library only, no game code. Frozen with the
report; copy it to a new dated folder before changing it.

What changed from the first study's copy (each change is a `tune` knob, so the first study's numbers still run with
`{"model": "study"}`):

| Change | Where | Knob |
|---|---|---|
| Each gatherer stands beside one node, and a Damage-1 strike pays that node's measured aura: Purlest's Opal by Tide (M2), Woozy's Red 1, Blue 5 and Yellow 5 (M3), The Hunt's Orange 1.8 Red and 3.0 Yellow (M3) | `sim.NODES`, `opal_strike`, `mine` | `model` (`round` default, `study`), `opal` (`full` default, `colour`: the Tide's colour only) |
| Bots put gatherers by the nearest nodes (the PC harness's `OreSpot`), smart players by the richest; `make_experiments.py` also tries every order on Woozy and keeps the best | `Run.node`, `policies.py` | `bot_nodes`, `smart_nodes` |
| The Mining Hut runs on the code's 6.8 s round trip by default (M9) | `act_hut` | `hut_code` |
| A fully grown Sapling's fruit is worth Attackspeed x 15 of each colour a payout (scope `07`; M10 saw about 6) | `act_sapling` | `fruit_pickup`, `fruit_k` (5 is P6) |
| Aura beasts hunted on The Hunt (M11) | `Run.end_tide` | `beasts` (aura of each colour a Tide) |
| A policy copying the round's PC harness for Spool (`98_Play.cs`, SPOOL_SMART) | `policies.SpoolRound` | `p: "round"`, `tin_first` |
| The Monte Carlo summary keeps nodes, fruit and beast income, and `mean_t` (the Tide lost on, 16 for a win) | `mc.py` | |

| File | What |
|---|---|
| `sim.py`, `policies.py`, `tables.py`, `mc.py`, `run1.py`, `quick.py` | as in the first study (its README), with the changes above |
| `make_experiments.py` | writes the setups: `python3 make_experiments.py 1` (K, L), then `2` (M-P, which read `results/res_L.json` for each player's best Woozy order) |
| `experiments/exp_K..P.json` | K calibration against the PC runs (Downpour's 30 s breaks), L baselines, M Spool's P1-P3 and design questions, N Yossele's P5 and P6, O the Opal's low reading, P The Hunt's beasts; L-P on the mode's 60 s breaks |
| `results/res_K..P.json` | 60 runs a setup; `lost_hist` counts the Tide each lost run ended on |
| `mining.py`, `results/mining.json` | one gatherer's aura a Tide on each measured node (analytic) |
| `summ.py`, `report_tables.py` | the report's tables from the result files |

Run it: `python3 make_experiments.py 1 && python3 mc.py experiments/exp_K.json 60 results/res_K.json` (and L), then
`python3 make_experiments.py 2` and M-P the same way; `python3 report_tables.py` prints every table. K and L take about
10 minutes on 4 cores, M-P about 25.
