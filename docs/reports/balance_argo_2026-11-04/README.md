# Argo review simulator, 2026-11-04

The simulator and hand numbers behind `../ARGO_REVIEW_2026-11-04.md`: a copy of `../balance_rerun_2026-11-03/` (left
frozen) with Argo added. Python 3, standard library only, no game code. Frozen with the report; copy it to a new dated
folder before changing it.

| File | What |
| --- | --- |
| `argo.py` | Argo's buildings, its slows, holds and pins, the Drill's Fathoms and mining, and the review's proposals as `tune` knobs (its docstring lists them) |
| `sim.py` | the re-run's model with Argo's hooks (search "Argo"): zones each step, a foe's slows, Crushing Depth, Pay Dirt, the Tide's Fathom |
| `policies.py` | Argo's upgrade prices (`UP`), `ArgoSmart` (a Scatter by every core first, Deep Water flooded to six pools, a Drill by the richest node aimed at the merge when the node is close enough, the Miner's Base tuning the busiest guns, then the cheapest upgrade it can afford); `cost_<kind>` overrides a build price |
| `argo_math.py`, `results/argo_math.txt` | the report's hand tables (and their printout): mining by Fathom, damage per aura, pellets in a lane, real slows, pins, weak upgrades |
| `mc.py` | as in the re-run, plus each setup's mean core damage and leaks |
| `make_experiments.py` | writes `experiments/exp_B.json` (40 setups) |
| `experiments/exp_A.json`, `results/res_A.json` | the first pass (44 setups, 40 runs), before the policy built every core's Scatter first: The Hunt's second core fell at Tide 1 when an upgrade came first |
| `results/res_B.json` | the report's runs: 30 a setup |

Run it: `python3 argo_math.py`, then `python3 make_experiments.py && python3 mc.py experiments/exp_B.json 30 results/res_B.json`
(about 20 minutes on 4 cores). One run with its log: `python3 run1.py v=argo m=woozy log=1 tune='{"slow_stack":false}'`.
Smacky and Yossele for comparison are the re-run's `results/res_L.json` (same model).
