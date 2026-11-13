// RULES FILE: names no engine type; ../Tools/cloud-check/rules_check.py runs it without the game (checks K1-K8).
// Kit 1.16 (2026-11-13). Versions and compatibility rules: TDS_Vessel_Creator\kit\README.md
// Shared kit: the two rules every brain of a buildable follows, written once (they had drifted between four vessels'
// copies): when a brain that keeps throwing stops, and what one upgrade's numbers mean. ModKitBrain.cs is the engine half.

// Pitfall #80's stop rule: a bad tick is skipped, and only a brain that threw on Limit frames running (~10 s) stops.
// Several errors in one frame count once (the first copies counted each, so a brain with two faults a frame stopped in
// half the time).
public sealed class ModKitStreak
{
    public const int Limit = 600;
    int streak, last = -10;

    // An error in 'frame'. True once errors came on Limit frames running, and on every error after that run goes on.
    public bool Hit(int frame)
    {
        if (frame != last)
            streak = frame - last == 1 ? streak + 1 : 1;
        last = frame;
        return streak >= Limit;
    }
}

// The upgrades one building holds: each upgrade's key once, and the numbers that are not modifiers on its own stats,
// which the brain reads with Param(). A number met again is added to the one held, except under the endings in
// 'replace' (a "_Base" or "_Max" is a new value, not more of the old).
public sealed class ModKitUpgrades
{
    public static readonly string[] BaseMax = { "_Base", "_Max" };

    readonly Dictionary<string, float> param = new Dictionary<string, float>();
    readonly List<string> keys = new List<string>();   // no HashSet: System.Core isn't referenced by the game's compiler
    readonly string[] replace;

    public ModKitUpgrades(string[] replace) { this.replace = replace ?? BaseMax; }

    public bool Has(string key) { return keys.Contains(key); }
    public int Count { get { return keys.Count; } }
    public List<string> Keys { get { return keys; } }

    public float Param(string name, float fallback)
    {
        float v;
        return param.TryGetValue(name, out v) ? v : fallback;
    }

    // False when the building already holds this upgrade (the replay at start-up and the buy can both bring it).
    public bool Add(string key)
    {
        if (keys.Contains(key))
            return false;
        keys.Add(key);
        return true;
    }

    public void Keep(string name, float value)
    {
        float old;
        bool sum = param.TryGetValue(name, out old);
        for (int i = 0; sum && i < replace.Length; i++)
            if (name.EndsWith(replace[i]))
                sum = false;
        param[name] = sum ? old + value : value;
    }

    public void Clear()
    {
        keys.Clear();
        param.Clear();
    }

    // "Range_Add" is stat "Range", type "Add"; a name without '_' is all stat. True when the type is a modifier's
    // (Add, Multi, Pmulti): on a stat the entity has, the number becomes that modifier; otherwise it is kept.
    public static bool Split(string name, out string stat, out string type)
    {
        int cut = name.LastIndexOf('_');
        stat = cut > 0 ? name.Substring(0, cut) : name;
        type = cut > 0 ? name.Substring(cut + 1) : "";
        return type == "Add" || type == "Multi" || type == "Pmulti";
    }
}
