// Every name, text and tuning number of the Riptide mode in one place (DESIGN.md explains each). Numbers marked
// "tune" were fitted to the stock Mode_Normal table and foe stats that probe 1 logged on the PC (round 1, 2026-10-04).
public static class RiptideData
{
    public const string Version = "0.2.3";

    // ---------------------------------------------------------------- ids (every one carries the mod's name, <= 60 chars)

    public const string Mode = "Mode_Riptide";
    public const string Tides = "Mode_Riptide_Tides";
    public const string GameplanPrefix = "Riptide_";       // the Riptide twin of a stock Gameplan: Riptide_<stock id>
    public const string TemplateMode = "Mode_Normal";
    public const string Displayname = "Riptide";
    public const string PitchLine = "Random Tides, salvage between them, one Basin.";

    // Nodes whose mode the switch turns into Riptide (v1: the regular mode only).
    public static readonly string[] SwappableModes = { "Mode_Normal" };

    // ---------------------------------------------------------------- the run

    public const int RunTides = 30;
    public static readonly int[] Breakers = { 10, 20, 30 };
    public static readonly int[] SalvageAfter = { 1, 5, 10, 15, 20, 25 };
    public const int Offers = 3, BreakerOffers = 4;
    public static float SalvageSeconds = 30f;            // the self-test shortens it
    public const float FallbackBreak = 20f, FallbackTideTime = 90f;   // when the stock table has no entry to copy

    public static string Stretch(int tide) { return tide <= 10 ? "Shallows" : tide <= 20 ? "Swell" : "Deep"; }
    public static bool IsBreaker(int tide) { return System.Array.IndexOf(Breakers, tide) >= 0; }
    public static bool IsSalvage(int tide) { return System.Array.IndexOf(SalvageAfter, tide) >= 0; }

    // ---------------------------------------------------------------- the budget curve (tune)

    // Threat is counted in Daggers: a foe's Health / 10, nudged for reach and speed. The stock Mode_Normal table, counted
    // the same way (elites at their Health factor), grows from 5 at Tide 1 to ~45 at Tide 5, ~250 at Tide 10, ~1200 at
    // Tide 20 and ~8000 at its finale; budget(t) = Base + Scale * t^Power follows it with the Breakers on top.
    public const float Base = 5f, Scale = 0.92f, Power = 2.4f, BreakerBudget = 1.6f, PerExtraPlayer = 0.35f;
    public const float HeadlineShare = 0.4f;
    public const int HeadlineMax = 3;
    public const int EliteHeadlineFrom = 20;
    public const int PackMin = 3, PackMax = 50;        // foes in one pack (a lone boss aside)
    public const float PackSeconds = 6f;              // a pack spreads over about this long
    public const float PackGap = 4f;                  // a Wait between packs

    public static float Budget(int tide, int players)
    {
        float b = (Base + Scale * (float)System.Math.Pow(tide, Power)) * (1f + PerExtraPlayer * System.Math.Max(0, players - 1));
        return IsBreaker(tide) ? b * BreakerBudget : b;
    }

    public static int PacksMin(int tide) { return tide <= 10 ? 1 : tide <= 20 ? 2 : 3; }
    public static int PacksMax(int tide) { return tide <= 10 ? 2 : tide <= 20 ? 3 : 4; }

    // Spawn spacing: a pack spreads over PackSeconds, 0.1-1 s apart like the stock table; heavy foes walk in slowly.
    public static float Interval(int count, float threat)
    {
        float max = threat >= 50f ? 4f : 1f;
        return System.Math.Max(0.1f, System.Math.Min(max, PackSeconds / System.Math.Max(1, count)));
    }

    // The chance that a pack is elite: none before Tide 6 (the stock table's first Hardened Tide), then rising.
    public static float EliteChance(int tide) { return tide < 6 ? 0f : System.Math.Min(0.7f, 0.2f + 0.03f * (tide - 6)); }

    // ---------------------------------------------------------------- foes (doc 13 §1 ids; threat and arrival: tune)

    // Arrivals follow the stock table (Spears at Tide 4, Aegis at 7, a Maul at 10, Ballistae at 11, Onagers at 21), a
    // little earlier since a run is random. Bosses come only as a Breaker's headline: Excalibur ends the stock table.
    public struct Foe
    {
        public string Id, Plural;
        public int From;
        public float Threat;
        public bool Boss;
        public Foe(string id, string plural, int from, float threat, bool boss = false) { Id = id; Plural = plural; From = from; Threat = threat; Boss = boss; }
    }

    public static readonly Foe[] Foes =
    {
        new Foe("Enemy_Dagger", "Daggers", 1, 1f),            // 10 Health
        new Foe("Enemy_Spear", "Spears", 3, 1.2f),            // 10 Health, fast
        new Foe("Enemy_Aegis", "Aegis", 6, 8f),               // 80
        new Foe("Enemy_Maul", "Mauls", 8, 70f),               // 700, hits for 50
        new Foe("Enemy_Ballista", "Ballistae", 10, 6f),       // 50, reach 5
        new Foe("Enemy_Claymore", "Claymores", 14, 2f),       // 10, very fast, reach 4
        new Foe("Enemy_Onager", "Onagers", 18, 12f),          // 90, reach 9
        new Foe("Enemy_Excalibur", "Excaliburs", 30, 1000f, true),   // 10000
        new Foe("Enemy_Bulwark", "Bulwarks", 30, 3000f, true),       // 30000
    };

    // ---------------------------------------------------------------- elites (the game's own modifiers, doc 13 §4)

    // Cost = the elite's Health factor (the game adds an upgrade's _Multi to a base of 1: Hardened's Health_Multi 3 is
    // x4 Health; probe 2 logs a Hardened Dagger's live Health to confirm). Arrivals follow the stock table.

    public struct Elite
    {
        public string Upgrade, Name;
        public int From;
        public float Cost;
        public Elite(string upgrade, string name, int from, float cost) { Upgrade = upgrade; Name = name; From = from; Cost = cost; }
    }

    public static readonly Elite[] Elites =
    {
        new Elite("Enemy_Health_Upgradeobject", "Hardened", 6, 4f),      // Health x4
        new Elite("Enemy_Red_Upgradeobject", "Tempered", 9, 2f),         // Health x2, Damage x2, longer aggro
        new Elite("Enemy_Armor_Upgradeobject", "Annealed", 11, 2f),      // armour
        new Elite("Enemy_Hyper_Upgradeobject", "Superheated", 14, 3.5f), // Health x3, Damage x3, faster
        new Elite("Enemy_Explode_Upgradeobject", "Molten", 16, 4f),      // Health x4, bursts when drowned
        new Elite("Enemy_Chaos_Upgradeobject", "Fractured", 18, 15f),    // Health x15, Damage x10, very fast
    };

    // ---------------------------------------------------------------- banner lines (2-6 words)

    public const string NoteBreaker = "A Breaker rolls in";
    public const string NoteLast = "Last Tide, hold fast";
    public static string NoteFoe(Foe f) { return f.Plural + " ahead"; }
    public static string NoteElite(Elite e) { return e.Name + " foes ahead"; }

    public static string NoteStretch(int tide)
    {
        if (tide == 1) return "The Shallows";
        if (tide == 11) return "The Swell rises";
        if (tide == 21) return "The Deep calls";
        return "";
    }

    // ---------------------------------------------------------------- Salvage

    public const string SalvageTitle = "SALVAGE";
    public const string SalvageSub = "Pick one, the Tide waits";
    public const string SalvagedWarning = " SALVAGED";
    public const string SalvageWarning = "SALVAGE WASHED ASHORE";   // to everyone when a Salvage opens
    public const string SalvageCommand = "Command_Riptide_Salvage";
    public const string SalvageLink = "Riptide_Salvage";
    public const string SalvageName = "Salvage";
    public const string SalvageDesc = "Pick one washed-up artifact for the team";
    public const string SalvageKey = "8";
    public const string SalvageIcon = "Riptide_Salvage";
    public static readonly UnityEngine.Color Sea = new UnityEngine.Color(0.36f, 0.71f, 0.84f, 1f);   // the button and switch

    // Driftwood: the filler when the pool can't fill an offer. Whoever picks it gets this much of each aura (tune).
    public const string Driftwood = "Driftwood";
    public const string DriftwoodName = "DRIFTWOOD";
    public const string DriftwoodDesc = "Aura of every colour, for you";
    public static float DriftwoodAura(int tide) { return 20f + 4f * tide; }
    public static readonly string[] Auras = { "Redaura", "Blueaura", "Yellowaura" };

    // 12x12, read top row first: a crate riding a wave. Used for the SALVAGE button and the RIPTIDE switch.
    public static readonly string[] SalvageIconRows =
    {
        "............", "..wwwwwwww..", "..wl.ll.lw..", "..wwwwwwww..", "..wl.ll.lw..", "..wl.ll.lw..",
        "..wwwwwwww..", "............", ".ss..ss..ss.", "s..ss..ss..s", "............", "............"
    };

    // ---------------------------------------------------------------- the RIPTIDE switch (Setup screen, host)

    public const string SwitchName = "Riptide";
    public const string SwitchTip = "RIPTIDE: random Tides, salvage between them";

    // Never offered: test tools and artifacts that read or change the lobby list themselves (DESIGN "reward pool").
    public static readonly string[] NeverOffer = { "Artifact_Sandbox", "Artifact_Blurryphoto", "Artifact_Pig" };

    // Effect scripts (on an artifact's core upgrades) whose effect only reaches what spawns after the grant: the
    // director gives what already stands the same upgrades ("catch-up").
    public const string KindTower = "Entity_Ai_Artifact_Towerapply";
    public const string KindTeam = "Entity_Ai_Artifact_Teamapply";
    public const string KindMob = "Entity_Ai_Artifact_Mobapply";
    public const string KindHero = "Entity_Ai_Artifact_Heroapply";
}
