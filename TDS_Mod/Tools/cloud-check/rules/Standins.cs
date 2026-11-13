// What the mode's rules files ask of the game, stood in for here (rules_check.py): the save as a dictionary, the log as
// counters, Mathf's few sums, the map pool, a hero's shown name. A rules file that needs more than this needs the game:
// it fails to compile here, and either the need moves out of it or its stand-in is written here, on purpose.
using System;
using System.Collections.Generic;

public static class RWUtil
{
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

public static class RWLog
{
    public static int Infos, Warns;
    public static void Info(string m) { Infos++; }
    public static void Warn(string m) { Warns++; }
    public static void Once(string k, string m) { Warns++; }
}

public static class Mathf
{
    public static int Clamp(int v, int lo, int hi) { return v < lo ? lo : v > hi ? hi : v; }
    public static int Max(int a, int b) { return a > b ? a : b; }
    public static int Min(int a, int b) { return a < b ? a : b; }
}

// The chart's map pool: 45 made-up base maps, or the script's own (rules_check.py's chart parity sets it).
public static class RWMode
{
    static List<RWChart.Plan> pool;
    public static List<RWChart.Plan> Pool
    {
        get
        {
            if (pool == null) { pool = new List<RWChart.Plan>(); for (int i = 0; i < 45; i++) pool.Add(new RWChart.Plan("RW_Plan_" + i.ToString("00"), "Map_" + i, i % 11)); }
            return pool;
        }
        set { pool = value; }
    }
}

public static partial class RWRun
{
    public static string HeroName(string hero) { return hero; }
}
