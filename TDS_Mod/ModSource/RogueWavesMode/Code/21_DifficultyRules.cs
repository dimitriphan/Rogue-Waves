// RULES FILE: no engine types. Compiled whole and run here by Tools/cloud-check/rules_check.py (RogueWavesMode/README.md).
// The difficulty's numbers and formulas (21_Difficulty.cs has the header and writes them into the game's Tide table).
public static partial class RWDifficulty
{
    public const float CountPerMap = 0.06f, CountPerPlayer = 0.5f, IntervalPerMap = 0.03f, HealthPerMap = 0.08f, BonusPerTide = 0.1f;
    public const float HealthLift = 2.6f;        // F3: foe health x2.6 on map 1 (a Pmulti of +1.6), HealthPerMap more each map after
    public const float HoardShare = 0.02f;       // F5: foe health x(1 + this x held) for the Cardinals, Trinket copies and Direction ranks held
    public const int ModifiersFrom = 2;          // F4: the map the first of the campaign's modifiers comes on, then one more every second map
    public const int ModifierShare = 5;          // one foe in this many of each row carries a modifier
    public const int LastRow = 29;               // the base game's row the bonus Tides end on and replay (row 30 is the Basin waking)
    // F1: the base game's row map k's Tide 15 plays (map 1 the base game's own Tide 15). Maps 6-10 end on row 24, Downpour's
    // last mixed Tide; they grow harder by health, count, intervals and modifiers (the balance sweep's fit).
    public static readonly int[] FinishRows = { 15, 18, 20, 21, 21, 24, 24, 24, 24, 24 };
    public static readonly string[] StockElites = { "Enemy_Armor_Upgradeobject", "Enemy_Health_Upgradeobject", "Enemy_Red_Upgradeobject", "Enemy_Explode_Upgradeobject", "Enemy_Aegis_Upgradeobject" };
    public static readonly string[] PackModifiers = { "RW_Modifier_Cursed_Upgradeobject", "RW_Modifier_Berserk_Upgradeobject", "RW_Modifier_Aging_Upgradeobject", "RW_Modifier_Dazzling_Upgradeobject" };

    public static int Finish(int map) { return FinishRows[Mathf.Clamp(map, 1, FinishRows.Length) - 1]; }

    // The base game's row map Tide t plays (both 1-based): Tides 1-15 spread up to Tide 15's row (never below their own),
    // the bonus Tides on from the row after it.
    public static int RowOf(int tide, int finish)
    {
        int win = RWCampaign.WinTide;
        if (tide <= win)
            return Mathf.Max(tide, (2 * tide * finish + win) / (2 * win));    // round(t x F / 15), halves up
        return finish + tide - win;
    }

    // The map's foe health as one Pmulti: x(lift + per map) x(1 + share x hoard), less the 1 a Pmulti starts from.
    public static float HealthFor(int map, int hoard)
    {
        return (HealthLift + HealthPerMap * (map - 1)) * (1f + HoardShare * Mathf.Max(0, hoard)) - 1f;
    }

    // The campaign's modifiers (docs/scope/04 "new modifiers that change with each campaign"): two stock, two of the pack's,
    // from the seed; one is on from ModifiersFrom (map 2), then one more every second map, all four by map 8.
    public static List<string> CampaignPool(uint seed)
    {
        System.Random rng = new System.Random(unchecked((int)seed) ^ 0x4D0D);
        List<string> stock = new List<string>(StockElites), pack = new List<string>(PackModifiers), pool = new List<string>();
        for (int i = 0; i < 2; i++)
        {
            pool.Add(TakeRandom(pack, rng));
            pool.Add(TakeRandom(stock, rng));
        }
        return pool;
    }

    static string TakeRandom(List<string> from, System.Random rng)
    {
        int k = rng.Next(from.Count);
        string s = from[k];
        from.RemoveAt(k);
        return s;
    }

    public static int ModifiersOn(int map, int pool) { return map < ModifiersFrom ? 0 : Mathf.Min(pool, 1 + (map - ModifiersFrom) / 2); }
}
