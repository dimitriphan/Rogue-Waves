"""Three alternative versions of the two temporary 3D models (2026-10-28): Yossele's aura fruit (YosseleModels.Fruit) and
the Dart's thrown needle (SpoolModels.Needle). Each version is a whole C# method in
model_variants/<Method>_v<n>.cs.txt; picking one means pasting it over the method of the same name (Dimitri's picks
of 2026-11-03 are pasted: Fruit v2, Needle v1).

For each version this copies the vessel's Editor folder to a scratch folder, swaps the method in, renders the model
with the cloud preview (TDS_Mod/Tools/cloud-check/lite/run_preview.sh, flag m; run setup.sh once first) and writes
<Part>/Art/Temporary/variants/v<n>/preview_8x/models_<Method>.png plus the snippet under variants/v<n>/models/.

    python3 concept-tools/temp_art/variants_models.py --spool TDS_Mod/Assets/SpoolMod/Editor
"""
import argparse
import os
import shutil
import subprocess
import tempfile
from PIL import Image
from tart import REPO

HERE = os.path.dirname(os.path.abspath(__file__))
LITE = os.path.join(REPO, 'TDS_Mod', 'Tools', 'cloud-check', 'lite', 'run_preview.sh')
NAMES = {'Fruit': ('Pear on leaves', 'Berry bunch', 'Seed pod'),
         'Needle': ('Pearl-headed pin', 'Darning needle', 'Curved needle')}


def swap(src, signature, body):
    """Replaces the method that starts at signature (and the comment lines just above it) with body."""
    lines = src.split('\n')
    i = next(k for k, l in enumerate(lines) if signature in l)
    s = i
    while s > 0 and lines[s - 1].strip().startswith('//'):
        s -= 1
    depth, e, opened = 0, i, False
    for e in range(i, len(lines)):
        depth += lines[e].count('{') - lines[e].count('}')
        opened = opened or '{' in lines[e]
        if opened and depth == 0:
            break
    return '\n'.join(lines[:s] + body.rstrip('\n').split('\n') + lines[e + 1:])


def render(editor, prefix, file, signature, method, names, part, crop=None):
    for v in (1, 2, 3):
        body = open(os.path.join(HERE, 'model_variants', '%s_v%d.cs.txt' % (method, v))).read()
        w = tempfile.mkdtemp()
        ed = os.path.join(w, 'Editor')
        shutil.copytree(editor, ed)
        p = os.path.join(ed, file)
        src = open(p).read()
        open(p, 'w').write(swap(src, signature, body))
        out = os.path.join(w, 'out')
        r = subprocess.run([LITE, '.', prefix, out, 'm'], cwd=ed, capture_output=True, text=True)
        imgs = [os.path.join(out, 'models', n + '.png') for n in names]
        if not all(os.path.exists(i) for i in imgs):
            raise SystemExit('%s v%d did not render:\n%s\n%s' % (method, v, r.stdout[-2000:], r.stderr[-2000:]))
        tiles = [Image.open(i).convert('RGBA') for i in imgs]
        if crop:
            tiles = [t.crop(crop(t)) for t in tiles]
        sheet = Image.new('RGBA', (sum(t.width for t in tiles), max(t.height for t in tiles)), (221, 216, 205, 255))
        x = 0
        for t in tiles:
            sheet.alpha_composite(t, (x, 0))
            x += t.width
        dst = os.path.join(REPO, 'TDS_Mod', 'ModSource', part, 'Art', 'Temporary', 'variants', 'v%d' % v)
        os.makedirs(os.path.join(dst, 'preview_8x'), exist_ok=True)
        os.makedirs(os.path.join(dst, 'models'), exist_ok=True)
        sheet.convert('RGB').save(os.path.join(dst, 'preview_8x', 'models_%s.png' % method))
        shutil.copy(os.path.join(HERE, 'model_variants', '%s_v%d.cs.txt' % (method, v)),
                    os.path.join(dst, 'models', '%s.cs.txt' % method))
        shutil.rmtree(w)
        print('%s v%d rendered' % (method, v))


if __name__ == '__main__':
    a = argparse.ArgumentParser()
    a.add_argument('--spool', required=True, help='TDS_Mod/Assets/SpoolMod/Editor')
    o = a.parse_args()
    yed = os.path.join(REPO, 'TDS_Mod', 'Assets', 'YosseleMod', 'Editor')
    render(yed, 'Yossele', 'YosseleModels.cs', 'static Lp Fruit(int[] cols)', 'Fruit',
           ['Yossele_Fruit', 'Yossele_Fruit_Red', 'Yossele_Fruit_Blue', 'Yossele_Fruit_Yellow'], 'Yossele')
    render(o.spool, 'Spool', 'SpoolModels.cs', 'static Lp Needle()', 'Needle', ['Spool_Needle'], 'Spool')
