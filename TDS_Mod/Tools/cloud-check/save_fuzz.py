"""Runs the mode's save and load fuzz (RogueWavesMode/Code/98_SaveFuzz.cs) here, without the game: copies the save's
readers and writers out of the mode's code as they stand (RWChart whole; RWPlayer; RWRun's save methods; RWWorld's names
and routes; RWCampaign.Saved; RWKeys' and RWData's keys), puts a dictionary in place of the game's save, compiles them
with the fuzz file and runs it. A method the fuzz reaches that this copies wrongly is a compile error, never a pass.

    cd TDS_Mod/ModSource && python3 ../Tools/cloud-check/save_fuzz.py [RogueWavesMode/Code]

Prints the fuzz's PASS/FAIL lines; exits 1 on any FAIL. Needs mono's mcs (setup.sh's mono-devel). Ran here, not in the
game: the real save (Easy Save 3) and the real map pool are the self-test's (RWTest, ROGUEWAVESMODE_SELFTEST)."""
import os, re, subprocess, sys, tempfile

code = sys.argv[1] if len(sys.argv) > 1 else 'RogueWavesMode/Code'
src = lambda name: open(os.path.join(code, name), encoding='utf-8-sig').read().replace('\r\n', '\n')


def block(text, start):
    """The text from start to the brace that closes the first brace after it (strings, chars and comments skipped)."""
    i, depth, opened = start, 0, False
    while i < len(text):
        c = text[i]
        if text.startswith('//', i):
            i = text.index('\n', i)
            continue
        if c == '"':
            i += 1
            while text[i] != '"':
                i += 2 if text[i] == '\\' else 1
        elif c == "'":
            i += 1
            while text[i] != "'":
                i += 2 if text[i] == '\\' else 1
        elif c == '{':
            depth, opened = depth + 1, True
        elif c == '}':
            depth -= 1
            if opened and depth == 0:
                return text[start:i + 1]
        i += 1
    sys.exit('save_fuzz: unbalanced braces after ' + text[start:start + 60])


def members(text, pattern):
    """Every method or class whose header matches pattern (a regex at the start of a line), each whole."""
    found = [block(text, m.start()) for m in re.finditer(r'^[ \t]*' + pattern, text, re.M)]
    if not found:
        sys.exit('save_fuzz: nothing matches ' + pattern + ': the mode\'s code changed, update this script')
    return '\n'.join(found)


def statement(text, pattern):
    """A declaration up to its semicolon."""
    m = re.search(r'^[ \t]*' + pattern + r'[^;]*;', text, re.M)
    if not m:
        sys.exit('save_fuzz: no declaration ' + pattern)
    return m.group(0)


mod, util, world, campaign, run = src('00_RogueWavesModeMod.cs'), src('01_Util.cs'), src('04_World.cs'), src('06_Campaign.cs'), src('14_Run.cs')
data = '\n'.join(statement(mod, r'public const string ' + k) for k in ('SaveFolder', 'KeyPrefix', 'PlanPrefix', 'WorldPrefix'))
keys = statement(util, r'public const string Seed')
parts = [
    'using System;\nusing System.Collections.Generic;\n#define_placeholder',
    # The stand-ins: the game's save as a dictionary, the log as counters, the map pool as 45 made-up base maps.
    '''public static class RWUtil {
    public static readonly Dictionary<string, string> Store = new Dictionary<string, string>();
    public static int Flushes;
    public static bool HoldFlush;
    public static string ReadString(string folder, string key) { string v; return Store.TryGetValue(folder + "/" + key, out v) ? v ?? "" : ""; }
    public static void WriteString(string folder, string key, string value) { Store[folder + "/" + key] = value; }
    public static bool Has(string folder, string key) { return Store.ContainsKey(folder + "/" + key); }
    public static int ReadInt(string folder, string key, int missing) { int v; return int.TryParse(ReadString(folder, key), out v) ? v : missing; }
    public static void Remove(string folder, string key) { Store.Remove(folder + "/" + key); }
    public static void Flush() { if (!HoldFlush) Flushes++; }
}
public static class RWLog {
    public static int Infos, Warns;
    public static void Info(string m) { Infos++; }
    public static void Warn(string m) { Warns++; }
    public static void Once(string k, string m) { Warns++; }
}
public static class Mathf { public static int Clamp(int v, int lo, int hi) { return v < lo ? lo : v > hi ? hi : v; } }
public static class RWMode {
    static List<RWChart.Plan> pool;
    public static List<RWChart.Plan> Pool { get {
        if (pool == null) { pool = new List<RWChart.Plan>(); for (int i = 0; i < 45; i++) pool.Add(new RWChart.Plan("RW_Plan_" + i.ToString("00"), "Map_" + i, i % 11)); }
        return pool; } }
}''',
    'public static class RWData {\n' + data + '\n}',
    'public static class RWKeys {\n' + keys + '\n}',
    src('03_Chart.cs'),
    members(run, r'public class RWPlayer\b'),
    'public static class RWRun {\n' + '\n'.join([
        statement(run, r'public const string CountKey'),
        statement(run, r'public const int MaxSaved'),
        statement(run, r'public static readonly Dictionary<string, RWPlayer> Players'),
        statement(run, r'public static readonly List<string> Cardinals'),
        members(run, r'public static void (Clear|Save|Load|Forget)\(\)'),
        members(run, r'public static string Encode\('),
        members(run, r'public static RWPlayer Decode\('),
        members(run, r'public static string SavedCrew\('),
        members(run, r'static string\[\] Ids\('),
        'public static string HeroName(string hero) { return hero; }',
    ]) + '\n}',
    'public static class RWWorld {\n' + '\n'.join([
        statement(world, r'static readonly Dictionary<ulong, RWChart.Chart> charts'),
        statement(world, r'public const string StopsMark'),
        members(world, r'public static string Name\('),
        members(world, r'public static bool Parse\('),
        members(world, r'public static RWChart.Chart Chart\('),
        members(world, r'public static RWChart.Node Here\('),
        members(world, r'public static List<RWChart.Node> Choices\('),
    ]) + '\n}',
    'public static class RWCampaign {\n' + statement(campaign, r'public const int RetriesPerCampaign') + '\n'
    + members(campaign, r'public static bool Saved\(') + '\n' + members(campaign, r'static bool SavedStops\(') + '\n}',
    'public static class RWTrinkets { public const int StormTides = ' + re.search(r'StormTides = (\d+)', open(os.path.join(code, '24_Trinkets.cs'), encoding='utf-8').read()).group(1) + '; }',
    'public static class RWCardinals { public const int Stacks = ' + re.search(r'const int Stacks = (\d+)', open(os.path.join(code, '17_Cardinals.cs'), encoding='utf-8').read()).group(1) + '; }',
    src('98_SaveFuzz.cs'),
    '''public static class SaveFuzzMain {
    static int fails;
    public static int Main() {
        RWSaveFuzz.Run((name, ok, detail) => { if (!ok) fails++; Console.WriteLine((ok ? "PASS " : "FAIL ") + name + (detail != "" ? ": " + detail : "")); return ok; });
        Console.WriteLine("save fuzz: " + (fails == 0 ? "all passed" : fails + " failed") + "; " + RWUtil.Store.Count + " keys left in the stand-in save, " + RWLog.Warns + " warnings logged");
        return fails == 0 ? 0 : 1;
    }
}''',
]
text = '\n\n'.join(parts).replace('#define_placeholder', '')
text = '#define ROGUEWAVESMODE_SELFTEST\n' + text
out = tempfile.mkdtemp(prefix='save_fuzz_')
cs, exe = os.path.join(out, 'SaveFuzz.cs'), os.path.join(out, 'SaveFuzz.exe')
open(cs, 'w', encoding='utf-8').write(text)
p = subprocess.run(['mcs', '-nologo', '-langversion:7', '-out:' + exe, cs], capture_output=True, text=True)
if p.returncode != 0:
    sys.exit('save_fuzz: the copied code does not compile (' + cs + '):\n' + p.stdout + p.stderr)
sys.exit(subprocess.run(['mono', exe]).returncode)
