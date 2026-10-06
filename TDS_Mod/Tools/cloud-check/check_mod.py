"""Port of Build-<Name>Mod.ps1 for this container: joins Code/*.cs + Shared/*.cs exactly like the PowerShell script
(ModKit -> <Prefix>Kit, using lines hoisted, #defines first), rejects System.IO/Reflection, and compiles with Roslyn
3.8 against stand-ins of the game's reference list (no System.dll, no System.Core)."""
import sys, os, re, subprocess, glob
H = os.path.dirname(os.path.abspath(__file__))
code_dir, shared_dir, prefix = sys.argv[1], sys.argv[2], sys.argv[3]
defines = [d for d in (sys.argv[4].split(',') if len(sys.argv) > 4 else []) if d]
# Like the build scripts: a <NAME>_SELFTEST define also switches on the kit's self-test helpers (ModKitTest).
if any(d.endswith('_SELFTEST') for d in defines): defines.append('MODKIT_TEST')
out_dir = sys.argv[5] if len(sys.argv) > 5 else os.path.join(H, 'out')
os.makedirs(out_dir, exist_ok=True)
usings, bodies, origin = [], [], []
files = sorted(glob.glob(os.path.join(code_dir, '*.cs')), key=lambda p: os.path.basename(p))
if os.path.isdir(shared_dir): files += sorted(glob.glob(os.path.join(shared_dir, '*.cs')), key=lambda p: os.path.basename(p))
for f in files:
    bodies.append('// ---- ' + os.path.basename(f)); origin.append((os.path.basename(f), 0))
    for ln, line in enumerate(open(f, encoding='utf-8-sig').read().replace('\r\n', '\n').split('\n')):
        line = line.replace('ModKit', prefix + 'Kit')
        if re.match(r'^\s*using\s+[\w\.]+\s*;\s*$', line):
            u = line.strip()
            if u not in usings: usings.append(u)
        else:
            bodies.append(line); origin.append((os.path.basename(f), ln + 1))
code = ''.join('#define %s\n' % d for d in defines) + '\n'.join(usings) + '\n\n' + '\n'.join(bodies) + '\n'
code_only = re.sub(r'//[^\n]*', '', code)
for banned in ('System.IO', 'System.Reflection'):
    if banned in code_only: sys.exit('code uses %s, which the game blocks' % banned)
out = os.path.join(out_dir, prefix + 'Mod_Code.cs')
open(out, 'w', encoding='utf-8').write(code)
U = os.path.join(H, 'unitymods/lib/net45')
game = ['UnityEngine.AIModule', 'UnityEngine.AnimationModule', 'UnityEngine.AssetBundleModule', 'UnityEngine.AudioModule',
        'UnityEngine.CoreModule', 'UnityEngine', 'UnityEngine.InputLegacyModule', 'UnityEngine.ParticleSystemModule',
        'UnityEngine.Physics2DModule', 'UnityEngine.PhysicsModule', 'UnityEngine.UIModule']
refs = ['/usr/lib/mono/4.5/mscorlib.dll', '/usr/lib/mono/4.5/Facades/netstandard.dll', os.path.join(H, 'stubs/Assembly-CSharp.dll'),
        os.path.join(H, 'stubs/ThirdParty.dll')] + [os.path.join(U, g + '.dll') for g in game]
cmd = ['mono', os.path.join(H, 'roslyn/tools/csc.exe'), '-nologo', '-nostdlib+', '-noconfig', '-target:library', '-langversion:9',
       '-nowarn:CS0618,CS0162,CS0414,CS0649,CS1701,CS1702', '-out:' + os.path.join(out_dir, 'check.dll')] + ['-r:' + r for r in refs] + [out]
p = subprocess.run(cmd, capture_output=True, text=True)
lines = [l for l in p.stdout.split('\n') if l.strip()]
errs = [l for l in lines if ': error ' in l]
warns = [l for l in lines if ': warning ' in l]
# Map joined-file line numbers back to source files.
head = len(defines) + len(usings) + 1
def where(n):
    i = n - head - 1
    if 0 <= i < len(origin): return '%s:%d' % origin[i]
    return 'joined:%d' % n
for l in errs + (warns if '-w' in sys.argv else []):
    m = re.search(r'\((\d+),(\d+)\)', l)
    print((where(int(m.group(1))) + ' ' if m else '') + l.split(': ', 1)[-1])
print('%s: %d errors, %d warnings, %d bytes' % (prefix, len(errs), len(warns), len(code)))
sys.exit(1 if errs or p.returncode else 0)
