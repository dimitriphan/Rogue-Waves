// Each map harder than the last (docs/scope/04 [Difficulty]; numbers are defaults for its blank slot): the host writes the
// mode's Tide table when a map starts, from a pristine copy, as Riptide proved live writes work (kit docs/15 §13):
//   spawn rates   more foes in each row and shorter intervals, by map and by players beyond the first
//   health        every foe gets a health upgrade on the stock Enemy_Health script with the map's number
//   modifiers     the campaign's pool (rolled from its seed: two stock elites, two of the pack's, docs/reports/SCOPE_FIT_PLAN_2026-10-28.md
//                 §5.1) adds its members one by one as the maps go on, each on a share of the foes
// and the bonus Tides: past Tide 29 the table's last break is rewound onto Tide 29's row while Wave rises, each one
// harder, so bonus Tides have no limit (a rewound Tide spends one of the enemy core's: it is topped up, kit docs/15 §4).
public static class RWDifficulty
{
    public const float CountPerMap = 0.06f, CountPerPlayer = 0.35f, IntervalPerMap = 0.03f, HealthPerMap = 0.08f, BonusPerTide = 0.1f;
    public static readonly string[] StockElites = { "Enemy_Armor_Upgradeobject", "Enemy_Health_Upgradeobject", "Enemy_Red_Upgradeobject", "Enemy_Explode_Upgradeobject", "Enemy_Aegis_Upgradeobject" };
    public static readonly string[] PackModifiers = { "RW_Modifier_Cursed_Upgradeobject", "RW_Modifier_Berserk_Upgradeobject", "RW_Modifier_Aging_Upgradeobject", "RW_Modifier_Dazzling_Upgradeobject" };
    public static float LastScale, LastHealth;       // self-test, log
    public static string LastModifiers = "";
    static List<Wavesobject.Waves> pristine;
    static List<Wavesobject.Waves> table;
    static readonly Dictionary<int, Upgradeproperties> health = new Dictionary<int, Upgradeproperties>();

    public static void MapStart()
    {
        if (RWMode.ModeObject == null)
            return;
        table = RWMode.ModeObject.Enemies.Wave;
        if (pristine == null)
            pristine = Copy(table);
        Restore();
        if (!RWRun.InCampaignMap || RWCampaign.Next == null)
            return;
        Apply(RWCampaign.Route.Count + 1, RWRun.Sessions().Count);
    }

    static List<Wavesobject.Waves> Copy(List<Wavesobject.Waves> from)
    {
        List<Wavesobject.Waves> copy = new List<Wavesobject.Waves>(from.Count);
        foreach (Wavesobject.Waves w in from)
        {
            Wavesobject.Waves n = w;
            n.Content = w.Content != null ? new List<Wavesobject.Wavecontent>(w.Content) : null;
            copy.Add(n);
        }
        return copy;
    }

    static void Restore()
    {
        List<Wavesobject.Waves> fresh = Copy(pristine);
        table.Clear();
        table.AddRange(fresh);
    }

    public static void Apply(int map, int players)
    {
        float scale = (1f + CountPerMap * (map - 1)) * (1f + CountPerPlayer * Mathf.Max(0, players - 1));
        float interval = Mathf.Max(0.5f, 1f - IntervalPerMap * (map - 1));
        Upgradeproperties tougher = Health(map);
        List<Upgradeproperties> mods = Pool(map);
        LastScale = scale;
        LastHealth = HealthPerMap * (map - 1);
        List<string> names = new List<string>();
        foreach (Upgradeproperties m in mods) names.Add(m.Objectname);
        LastModifiers = string.Join(", ", names.ToArray());
        for (int i = 0; i < table.Count; i++)
        {
            Wavesobject.Waves w = table[i];
            if (!w.Wave || w.Content == null)
                continue;
            List<Wavesobject.Wavecontent> rows = new List<Wavesobject.Wavecontent>();
            int nth = 0;
            foreach (Wavesobject.Wavecontent c0 in w.Content)
            {
                Wavesobject.Wavecontent c = c0;
                if (c.Enemy == null)
                {
                    rows.Add(c);
                    continue;
                }
                int total = Mathf.Max(1, Mathf.RoundToInt(c.Count * scale));
                c.Interval *= interval;
                List<Upgradeproperties> ups = c.Upgrades != null ? new List<Upgradeproperties>(c.Upgrades) : new List<Upgradeproperties>();
                if (tougher != null) ups.Add(tougher);
                c.Upgrades = ups;
                // One foe in five of a row carries one of the map's modifiers, in turn.
                int marked = mods.Count > 0 ? total / 5 : 0;
                c.Count = total - marked;
                rows.Add(c);
                if (marked > 0)
                {
                    Wavesobject.Wavecontent extra = c0;
                    extra.Count = marked;
                    extra.Interval = c.Interval;
                    List<Upgradeproperties> withMod = new List<Upgradeproperties>(ups);
                    withMod.Add(mods[nth % mods.Count]);
                    extra.Upgrades = withMod;
                    rows.Add(extra);
                    nth++;
                }
            }
            w.Content = rows;
            table[i] = w;
        }
        RWLog.Info("map " + map + " for " + players + " players: foes x" + RWUtil.F(scale) + ", intervals x" + RWUtil.F(interval) + ", health +" + RWUtil.F(LastHealth * 100f) + "%, modifiers [" + LastModifiers + "]");
    }

    static Upgradeproperties Health(int map)
    {
        if (map <= 1)
            return null;
        Upgradeproperties u;
        if (health.TryGetValue(map, out u))
            return u;
        // Elite params add to 1: Health_Multi x means health x(1 + x) (kit docs/13 §10); the stock script reads this one key.
        u = RWContent.Upgrade("RW_Foe_Health" + map + "_Upgradeobject", "", "", "Entity_Ai_Enemy_Health_Upgrade", "Health_Multi", HealthPerMap * (map - 1));
        health[map] = u;
        return u;
    }

    // The campaign's modifiers (docs/scope/04 "new modifiers that change with each campaign"): two stock, two of the pack's,
    // from the seed; one is on from map 3, then one more every second map.
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

    static List<Upgradeproperties> Pool(int map)
    {
        List<Upgradeproperties> on = new List<Upgradeproperties>();
        List<string> pool = CampaignPool(RWCampaign.Seed);
        int count = map < 3 ? 0 : Mathf.Min(pool.Count, 1 + (map - 3) / 2);
        for (int i = 0; i < count; i++)
        {
            Upgradeproperties u = RWUtil.Load<Upgradeproperties>("Upgrade", pool[i]);
            if (u != null) on.Add(u);
            else RWLog.Once("no modifier " + pool[i], "modifier " + pool[i] + " isn't loaded: this map goes without it");
        }
        return on;
    }

    // A break from Tide 15 on. Past the table's last Tide the party stays in Tide 29: rewind Entry onto its row, top the
    // enemy cores up, and write the row one step harder.
    public static void Bonus(int wave)
    {
        Game_Mapvars vars = Game_Mapvars.Instance;
        if (vars == null || table == null || wave < 29 || vars.Entry.Value < table.Count - 2)
            return;
        vars.Entry.Value = vars.Entry.Value - 2;
        foreach (Entity_Master core in RWUtil.EnemyCores())
        {
            Link_Stats s;
            if (core.Statsserver.TryGetValue("Healthresource", out s))
                s.Value += 1f;
        }
        int row = vars.Entry.Value;         // when this break's clock runs out Entry rises by one, and the row is Entry - 1
        if (row >= 0 && row < table.Count && table[row].Wave && table[row].Content != null)
        {
            Wavesobject.Waves w = table[row];
            List<Wavesobject.Wavecontent> rows = new List<Wavesobject.Wavecontent>();
            foreach (Wavesobject.Wavecontent c0 in w.Content)
            {
                Wavesobject.Wavecontent c = c0;
                if (c.Enemy != null) c.Count = Mathf.CeilToInt(c.Count * (1f + BonusPerTide));
                rows.Add(c);
            }
            w.Content = rows;
            table[row] = w;
        }
        RWLog.Info("bonus Tide " + (wave + 1) + ": Tide 29's row, one step harder");
    }
}
