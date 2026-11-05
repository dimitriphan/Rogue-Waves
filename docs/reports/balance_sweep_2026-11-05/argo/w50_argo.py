"""W50 for Argo: ../../mode_balance_2026-11-05/w50.py and mode.py (unchanged) run on the Argo review's simulator
(../../balance_argo_2026-11-04/, frozen: the 2026-11-03 re-run's model with Argo's buildings and ArgoSmart) instead of
../../balance_upgrades_2026-11-04/. Its sim, tables and policies are imported first, so mode.py's own imports find them.

    W50_HI=48 python3 w50_argo.py <experiments.json> <out.json> [seeds] [steps]
"""
import os, sys
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, '..', '..', 'balance_argo_2026-11-04'))
import sim, tables, policies          # Argo's copies, cached before mode.py inserts its own folder
sys.path.insert(0, os.path.join(HERE, '..', '..', 'mode_balance_2026-11-05'))
import mode, w50
assert 'balance_argo' in sim.__file__, sim.__file__

if __name__ == '__main__':
    w50.main(sys.argv[1], sys.argv[2], *(int(x) for x in sys.argv[3:]))
