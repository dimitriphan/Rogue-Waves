"""Check sheet for drawing: python3 lineup.py <module> <minion-key> <out.png> [first] [count] [crew]
Each version is one row: move 0-5 (front), attack 0-3 (front), idle 0 back, move 2 back, tune 0-5 when it has one."""
import importlib
import sys
from PIL import Image
from mlib import Canvas, pose, opaque, bbox, LAYOUTS

mod = importlib.import_module(sys.argv[1])
key, out = sys.argv[2], sys.argv[3]
first = int(sys.argv[4]) if len(sys.argv) > 4 else 0
count = int(sys.argv[5]) if len(sys.argv) > 5 else 10
M = [m for m in mod.MINIONS if m['key'] == key][0]
cell = LAYOUTS[M['layout']]['cell']
crop = 16 if cell == 24 else cell
kw = {'crew': M['crews'][int(sys.argv[6]) if len(sys.argv) > 6 else 0]} if 'crews' in M else {}
k = 7
plan = [('move', i, True) for i in range(6)] + [('attack', i, True) for i in range(4)] + [('move', 0, False), ('move', 2, False)]
if M['layout'] == 'cordy':
    plan = [('idle', i, True) for i in (0, 2, 4)] + plan
if 'tune' in M:
    plan = [('move', i, True) for i in range(6)] + [('tune', i, True) for i in range(6)] + [('move', 0, False), ('move', 2, False)]
vers = M['versions'][first:first + count]
im = Image.new('RGB', (len(plan) * (crop + 1) * k, len(vers) * (crop + 1) * k), (140, 155, 140))
for r, (name, fn, gait) in enumerate(vers):
    for j, (kind, i, front) in enumerate(plan):
        cv = Canvas(cell, cell)
        fn(cv, pose(kind, i, front, cell, gait), **kw)
        opaque(cv)
        t = Image.frombytes('RGBA', (cell, cell), cv.rgba()).crop(((cell - crop) // 2, cell - crop, (cell + crop) // 2, cell))
        t = t.resize((crop * k, crop * k), Image.NEAREST)
        im.paste(t, (j * (crop + 1) * k, r * (crop + 1) * k), t)
        if j == 0:
            b = bbox(cv)
            print(name, 'box %dx%d' % (b[2] - b[0], b[3] - b[1]), 'colours', len(cv.colours()))
im.save(out)
