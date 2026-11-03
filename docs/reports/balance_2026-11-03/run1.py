"""One run with its log: python3 run1.py v=spool m=purlest p=smart seed=1 log=1 tune='{"tin_units":6}'."""
import sys, json, time; import os; sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from sim import Run
args = dict(a.split('=') for a in sys.argv[1:])
t0 = time.time()
r = Run(args.get('v', 'yossele'), args.get('m', 'purlest'), args.get('t', 'downpour'), args.get('p', 'smart'),
        coins=args.get('coins', '0') == '1', seed=int(args.get('seed', 1)), tides=int(args.get('n', 15)), log=args.get('log', '0') == '1',
        tune=json.loads(args.get('tune', '{}')))
res = r.play()
print('lost', res['lost'], 'core', [round(c) for c in res['core']], 'kills', res['kills'], 'wallet', res['wallet'], 'spent', res['spent'])
print('core_dmg', [round(x) for x in res['core_dmg']]); print('leaks', res['leaks'])
print('income kills', res['income']['kills'], 'coins', res['income']['coins'], 'mine', [round(x) for x in res['income']['mine']])
print('counts', res['counts']); print('dmg', res['dmg']); print('thread', res['thread'])
print('tide_len', res['tide_len'], 'hits', res['hits']); print('tide wallets', res['tide_wallet'])
print('%.1fs' % (time.time() - t0))
