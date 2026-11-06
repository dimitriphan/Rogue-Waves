"""Build the Rogue Waves Scope Editor: python3 build.py <scope dir> <out.html>
Reads every NN_*.txt in the scope dir, embeds them in template.html with the commit they came from (shown on the
overview and stamped on every sent edit). NEW lists the sheets added on 2026-11-04. See README.md."""
import json, os, re, subprocess, sys
here = os.path.dirname(os.path.abspath(__file__))
scope, out = sys.argv[1], sys.argv[2]
NEW = ["Wound Spring","Fine Sieve","Spare Hands","Galley Kettle","Chain Shot","Salt Barrel","Fog Horn","Clam Shell",
 "Chum Bucket","Pressure Gauge","Cutting Jar","Sap Bucket","Creeping Ivy","Double Needle","Button Jar","Pattern Book",
 "Mason's Trowel","Snow Shovel","Worm Tin","Open Sign","Long Leash",
 "Brass Spyglass","Gutting Knife","Oil Can","Tar Bucket","Rubber Duck","Spare Plank","Cork Lifebuoy","Signal Flare",
 # roguelikes round (2026-11-04, later): docs/reports/ROGUELIKE_DIRECTIONS_CARDINALS_2026-11-04.md
 "Notched Oar","Lamp Oil","Fish Hook","Spare Compass","Magpie Nest","Fire Ship","Tide Pool","Deep Breath","Fairy Ring",
 "Merit Badge","Odd Socks",
 "Punch Card","Pearl Oyster","Bottled Storm","Oilskin Coat","Gull Feather","Cargo Crate","Tin Whistle"]
files = []
for n in sorted(os.listdir(scope)):
    m = re.match(r'^(\d\d)_(.+)\.txt$', n)
    if not m: continue
    text = open(os.path.join(scope, n), encoding='utf-8').read()
    first = next(l.strip() for l in text.split('\n') if l.strip())
    title = re.sub(r'^\|\s*|\s*\|$', '', first)
    files.append({"key": m.group(1), "name": n, "label": m.group(2).replace('_', ' '), "title": title, "text": text})
try:
    built = subprocess.run(['git', '-C', scope, 'rev-parse', '--short', 'HEAD'], capture_output=True, text=True, check=True).stdout.strip()
except (OSError, subprocess.CalledProcessError):
    built = ''
data = json.dumps({"files": files, "newNames": NEW, "built": built}, ensure_ascii=False).replace('</', '<\\/')
t = open(os.path.join(here, 'template.html'), encoding='utf-8').read()
assert '/*__DATA__*/null' in t
open(out, 'w', encoding='utf-8').write(t.replace('/*__DATA__*/null', data))
print(len(files), 'files ->', out)
