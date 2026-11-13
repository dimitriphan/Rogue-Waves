"""Runs the mode's rules here, without the game. Every file in RogueWavesMode/Code whose first line starts "// RULES FILE"
is compiled whole (never copied in pieces) with rules/Standins.cs in place of the game, then run:

    the save fuzz    RogueWavesMode/Code/98_SaveFuzz.cs, F1-F7, on a dictionary standing in for the save
    the rule checks  rules/RulesTest.cs, R1-R16: world names, routes, player lines, the difficulty's numbers;
                     C1-C12: whole campaigns played through RWCampaign's steps, and the keys they save
    chart parity     RWChart against concept-tools/sea_chart.py, seed by seed, with and without stops

    cd TDS_Mod/ModSource && python3 ../Tools/cloud-check/rules_check.py [RogueWavesMode/Code] [--seeds N]

Exits 1 on any FAIL, on a chart that differs, or when a rules file doesn't compile: a rules file that names an engine type
is a compile error here (no game assembly is referenced), which is the guard that keeps these files free of the game.
Needs mono's mcs (setup.sh's mono-devel). Ran here, not in the game: the real save (Easy Save 3) and the real map pool
are the self-test's (RWTest, ROGUEWAVESMODE_SELFTEST)."""
import glob, os, re, subprocess, sys, tempfile

H = os.path.dirname(os.path.abspath(__file__))
args = sys.argv[1:]
seeds = int(args[args.index('--seeds') + 1]) if '--seeds' in args else 300
if '--seeds' in args: del args[args.index('--seeds'):args.index('--seeds') + 2]
code = args[0] if args else 'RogueWavesMode/Code'
read = lambda path: open(path, encoding='utf-8-sig').read().replace('\r\n', '\n')

rules = [f for f in sorted(glob.glob(os.path.join(code, '*.cs'))) if read(f).startswith('// RULES FILE')]
if not rules:
    sys.exit('rules_check: no "// RULES FILE" in ' + code)
MAIN = '''public static class RulesMain {
    static int fails;
    static bool Check(string name, bool ok, string detail) { if (!ok) fails++; Console.WriteLine((ok ? "PASS " : "FAIL ") + name + (detail != "" ? ": " + detail : "")); return ok; }
    public static int Main(string[] a) {
        if (a.Length > 0) {   // chart parity: the pool's file, then the seeds; prints what sea_chart.py --check prints
            List<RWChart.Plan> pool = new List<RWChart.Plan>();
            foreach (string line in System.IO.File.ReadAllLines(a[0])) { string[] f = line.Split(' '); pool.Add(new RWChart.Plan(f[0], f[1], int.Parse(f[2]))); }
            for (int i = 1; i < a.Length; i++)
                foreach (bool stops in new[] { false, true })
                    foreach (string line in RWChart.Dump(RWChart.Generate(uint.Parse(a[i]), pool, stops))) Console.WriteLine(line);
            return 0;
        }
        RWSaveFuzz.Run((n, ok, d) => Check(n, ok, d));
        RulesTest.Run((n, ok, d) => Check(n, ok, d));
        Console.WriteLine("rules: " + (fails == 0 ? "all passed" : fails + " failed") + "; " + RWUtil.Store.Count + " keys left in the stand-in save, " + RWLog.Warns + " warnings logged");
        return fails == 0 ? 0 : 1;
    }
}'''
# Joined like the build joins a part: the using lines first, then each file under its name (so an error says which file).
sources = rules + [os.path.join(code, '98_SaveFuzz.cs'), os.path.join(H, 'rules', 'Standins.cs'), os.path.join(H, 'rules', 'RulesTest.cs')]
usings, bodies, origin = ['using System;', 'using System.Collections.Generic;'], [], []
for f in sources:
    for n, line in enumerate(read(f).split('\n')):
        if re.match(r'^\s*using\s+[\w\.]+\s*;\s*$', line):
            if line.strip() not in usings: usings.append(line.strip())
        else:
            bodies.append(line); origin.append('%s:%d' % (os.path.basename(f), n + 1))
head = ['#define ROGUEWAVESMODE_SELFTEST'] + usings
out = tempfile.mkdtemp(prefix='rules_check_')
cs, exe = os.path.join(out, 'Rules.cs'), os.path.join(out, 'Rules.exe')
open(cs, 'w', encoding='utf-8').write('\n'.join(head + bodies + [MAIN]) + '\n')
p = subprocess.run(['mcs', '-nologo', '-langversion:7', '-out:' + exe, cs], capture_output=True, text=True)
if p.returncode != 0:
    def where(m):
        i = int(m.group(1)) - len(head) - 1
        return (origin[i] if 0 <= i < len(origin) else 'joined:' + m.group(1)) + ' '
    sys.exit('rules_check: the rules files do not compile without the game:\n' + re.sub(r'^\S*Rules\.cs\((\d+),\d+\): ', where, p.stdout + p.stderr, flags=re.M))
print('rules files: ' + ', '.join(os.path.basename(f) for f in rules))
failed = subprocess.run(['mono', exe]).returncode != 0

# Chart parity: the script's own pool to the C# chart, the script's own lines to compare with.
sys.path.insert(0, os.path.join(H, '..', '..', '..', 'concept-tools'))
import sea_chart
pool = os.path.join(out, 'pool.txt')
open(pool, 'w').write(''.join('%s %s %d\n' % p for p in sea_chart.POOL))
ids = list(range(1, seeds + 1)) + [20261009]
want = []
for s in ids:
    for with_stops in (False, True):
        rows = sea_chart.chart(s)
        if with_stops: sea_chart.stops(rows, s)
        want += sea_chart.check_lines(s, rows, with_stops)
got = subprocess.run(['mono', exe, pool] + [str(s) for s in ids], capture_output=True, text=True).stdout.split('\n')
got = [l for l in got if l]
diff = next((i for i, (a, b) in enumerate(zip(want, got)) if a != b), None if len(want) == len(got) else min(len(want), len(got)))
if diff is None:
    print('PASS chart parity: RWChart and sea_chart.py draw the same chart (%d seeds, with and without stops, %d lines)' % (len(ids), len(want)))
else:
    failed = True
    print('FAIL chart parity at line %d:\n  sea_chart.py  %s\n  03_Chart.cs   %s' % (diff + 1, want[diff] if diff < len(want) else '(none)', got[diff] if diff < len(got) else '(none)'))
sys.exit(1 if failed else 0)
