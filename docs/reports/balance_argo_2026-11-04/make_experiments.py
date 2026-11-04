"""Writes experiments/exp_B.json (the report's runs; exp_A.json was the first pass, before the policy built every
core's Scatter first): Argo as built, as its text says, its paths, and the review's proposals, on Purlest, Woozy (the
Yellow node first, the best order, and the Red node first) and The Hunt, with the mode's 60 s breaks.
python3 make_experiments.py && python3 mc.py experiments/exp_B.json 30 results/res_B.json"""
import json
MAPS = [('purlest', {}), ('woozy-YRB', {'smart_nodes': ['Yellow', 'Red', 'Blue']}),
        ('woozy-RBY', {'smart_nodes': ['Red', 'Blue', 'Yellow']}), ('thehunt', {})]
TEXT = {'slow_stack': False}
COL = dict(cost_base=[0, 20, 15], up_Seismic=[0, 0, 35], up_Bedrock=[30, 0, 80], up_Faultline=[0, 0, 300])
# Mantle Tap ran at x2 speed here (argo.py mantle_mult default); the sheet now says x1.5 (report §3)
PROP = dict(TEXT, prop=['kelp_pins', 'mantle_speed', 'mantle_every', 'trench_more'], scatter_pinbonus=1.0, gusher_per_fathom=4, **COL)
ANCHOR = {'harpoon': ['Tether', 'Anchor'], 'water': ['Brine', 'Salt', 'Crushing', 'Trench']}
FAULT = {'drill': ['Seismic', 'Bedrock', 'Faultline']}
SETUPS = [
    ('built', {}),                                    # the code as it stands: a slow stacks with its own re-ticks
    ('text', TEXT),                                   # one slow per source, as the sheet says
    ('text-noaim', dict(TEXT, tremor_reach=0)),       # skill: the Tremor never on the lane
    ('text-3drills', dict(TEXT, drills=3)),           # a Drill by every node in reach
    ('text-dmgharpoon', dict(TEXT, argo_paths={'harpoon': ['Longline', 'Skewer', 'Whaler', 'Leviathan']})),
    ('text-anchor', dict(TEXT, argo_paths=ANCHOR)),
    ('text-fault', dict(TEXT, argo_paths=FAULT)),
    ('prop', PROP),
    ('prop-anchor', dict(PROP, argo_paths=ANCHOR)),
    ('prop-fault', dict(PROP, argo_paths=FAULT)),
]
out = []
for sname, st in SETUPS:
    for mname, mt in MAPS:
        tune = {'break': 60}; tune.update(mt); tune.update(st)
        out.append(dict(name='B-%s-%s' % (sname, mname), v='argo', m=mname.split('-')[0], p='smart', coins=False, n=15, tune=tune))
json.dump(out, open('experiments/exp_B.json', 'w'), indent=1)
print(len(out), 'setups')
