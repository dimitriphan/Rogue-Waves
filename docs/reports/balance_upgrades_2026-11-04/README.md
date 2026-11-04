# Upgrade study simulator, 2026-11-04

The simulator behind `../UPGRADE_GAPS_2026-11-04.md`: a copy of `../balance_rerun_2026-11-03/` (left frozen) with the
report's proposed upgrades added as priced upgrades. Python 3, standard library only, no game code. Frozen with the
report; copy it to a new dated folder before changing it.

| File | What |
| --- | --- |
| `sim.py` | the re-run's model plus: Wood Wide Web (`Run.convert`), Dyed in the Wool (in `mine`), Tacking and Cross Stitch (Crafted creation), the Thread Head's auras (`head_dmg`, `head_dr`), Big Bobbin, High Thread Count, Head of the Class, Iron Thimble and Voodoo Doll (`damage_bld`), Bobbin Winder (`act_loom`); each marked "2026-11-04" |
| `policies.py` | the proposals' prices and tiers in `UP`; `YosseleWeb` (tune `web`, `web_every`) and `SpoolPkg` (tune `pkg`, a list of names; `dye_eff`) |
| `mc.py`, `tables.py` | as in the re-run; `mc.py` also keeps `conv`, the aura traded or dyed |
| `make_experiments.py` | writes `experiments/exp_U.json` (84 setups) |
| `results/res_U.json` | 60 runs a setup |

Run it: `python3 make_experiments.py && python3 mc.py experiments/exp_U.json 60 results/res_U.json` (about 25 minutes on 4 cores).
