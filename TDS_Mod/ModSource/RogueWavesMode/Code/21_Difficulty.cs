// Each map harder than the last (docs/scope/04 [Difficulty]; the balance sweep's numbers, docs/reports/BALANCE_SWEEP_2026-11-05.md):
// the host writes the mode's Tide table when a map starts, from a pristine copy (live writes work, kit docs/15 §13):
//   rows          map k's Tide 15 plays a later row of the base game's table (FinishRows, F1), and its Tides 1-14 the rows
//                 spread between, round(t x F / 15); the bonus Tides go on from the row after it. A break's note under the
//                 Tide banner goes with the row it warns of
//   spawn rates   more foes in each row and shorter intervals, by map and by players beyond the first (X1)
//   health        every foe gets the map's health upgrade, which has no script: the host adds its Health mod itself
//                 (Update), as the stock Enemy_Health script would also paint the foe as a Hardened elite. It is a Pmulti,
//                 so it compounds with an elite's own Multi (F2): x HealthLift from map 1 (F3), HealthPerMap more a map, and
//                 HoardShare more for each Cardinal, Trinket copy and Direction rank the party holds (F5)
//   modifiers     the campaign's pool (rolled from its seed: two stock elites, two of the pack's, docs/reports/SCOPE_FIT_PLAN_2026-10-28.md
//                 §5.1) adds its members one by one as the maps go on, each on a share of the foes, in turn from the Tide's
//                 own number (F4)
// and the bonus Tides: once a Tide has played the table's LastRow, each break rewinds onto that Tide while Wave rises, each
// one harder, so bonus Tides have no limit (a rewound Tide spends one of the enemy core's: it is topped up, kit docs/15 §4).
public static class RWDifficulty
{
    public const float CountPerMap = 0.06f, CountPerPlayer = 0.5f, IntervalPerMap = 0.03f, HealthPerMap = 0.08f, BonusPerTide = 0.1f;
    public const float HealthLift = 2.6f;        // F3: foe health x2.6 on map 1 (a Pmulti of +1.6), HealthPerMap more each map after
    public const float HoardShare = 0.015f;      // F5: foe health x(1 + this x held) for the Cardinals, Trinket copies and Direction ranks held
    public const int ModifiersFrom = 2;          // F4: the map the first of the campaign's modifiers comes on, then one more every second map
    public const int ModifierShare = 5;          // one foe in this many of each row carries a modifier
    public const int LastRow = 29;               // the base game's row the bonus Tides end on and replay (row 30 is the Basin waking)
    // F1: the base game's row map k's Tide 15 plays (map 1 the base game's own Tide 15). Maps 6-10 end on row 24, Downpour's
    // last mixed Tide; they grow harder by health, count, intervals and modifiers (the balance sweep's fit).
    public static readonly int[] FinishRows = { 15, 18, 20, 21, 21, 24, 24, 24, 24, 24 };
    public static readonly string[] StockElites = { "Enemy_Armor_Upgradeobject", "Enemy_Health_Upgradeobject", "Enemy_Red_Upgradeobject", "Enemy_Explode_Upgradeobject", "Enemy_Aegis_Upgradeobject" };
    public static readonly string[] PackModifiers = { "RW_Modifier_Cursed_Upgradeobject", "RW_Modifier_Berserk_Upgradeobject", "RW_Modifier_Aging_Upgradeobject", "RW_Modifier_Dazzling_Upgradeobject" };
    public static float LastScale, LastHealth;       // self-test, log: LastHealth is the Pmulti each foe gets (x(1 + it))
    public static int LastFinish, LastHoard;         // self-test, log: the row Tide 15 plays, and what F5 counted
    public static string LastModifiers = "";
    static List<Wavesobject.Waves> pristine;
    static List<Wavesobject.Waves> table;
    static int[] rowAt = new int[0];                 // per table entry: the base game's row a Tide entry plays (0 a break)
    static readonly Dictionary<int, Upgradeproperties> health = new Dictionary<int, Upgradeproperties>();
    static string healthId;                         // this map's health upgrade, null on map 1 and outside the campaign
    static readonly Dictionary<Entity_Master, bool> toughened = new Dictionary<Entity_Master, bool>();
    static float nextLook;
    static int pruneAt = 256;

    public static void MapStart()
    {
        healthId = null;
        toughened.Clear();
        pruneAt = 256;
        if (RWMode.ModeObject == null)
            return;
        table = RWMode.ModeObject.Enemies.Wave;
        if (pristine == null)
            pristine = Copy(table);
        Restore();
        if (!RWRun.InCampaignMap || RWCampaign.Next == null)
            return;
        Apply(RWCampaign.Route.Count + 1, RWRun.Sessions().Count, Hoard());
    }

    // F5: what the party holds as the map starts (the lobby's picks left out, as the base game has them too): every Cardinal
    // stack bought, and each present player's Trinket copies and Direction ranks, with a pick still owed as one more rank.
    public static int Hoard()
    {
        int n = RWRun.Cardinals.Count;
        foreach (long s in RWRun.Sessions())
        {
            RWPlayer p = RWRun.For(s);
            n += p.Trinkets.Count + p.Directions.Count + (p.Owed ? 1 : 0);
        }
        return n;
    }

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

    // The table entries that are Tides, in order: Tide t is entry tides[t - 1] (kit docs/15 §4: 2t - 1 in Mode_Normal's).
    static List<int> TideEntries(List<Wavesobject.Waves> from)
    {
        List<int> tides = new List<int>();
        for (int i = 0; i < from.Count; i++)
            if (from[i].Wave) tides.Add(i);
        return tides;
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
        rowAt = new int[table.Count];
        List<int> tides = TideEntries(table);
        for (int t = 1; t <= tides.Count; t++)
            rowAt[tides[t - 1]] = t;
    }

    // The map's foe health as one Pmulti: x(lift + per map) x(1 + share x hoard), less the 1 a Pmulti starts from.
    public static float HealthFor(int map, int hoard)
    {
        return (HealthLift + HealthPerMap * (map - 1)) * (1f + HoardShare * Mathf.Max(0, hoard)) - 1f;
    }

    public static void Apply(int map, int players, int hoard)
    {
        float scale = (1f + CountPerMap * (map - 1)) * (1f + CountPerPlayer * Mathf.Max(0, players - 1));
        float interval = Mathf.Max(0.5f, 1f - IntervalPerMap * (map - 1));
        Upgradeproperties tougher = Health(map);
        List<Upgradeproperties> mods = Pool(map);
        int finish = Finish(map);
        LastScale = scale;
        LastFinish = finish;
        LastHoard = hoard;
        LastHealth = HealthFor(map, hoard);
        healthId = tougher != null ? tougher.Objectname : null;
        List<string> names = new List<string>();
        foreach (Upgradeproperties m in mods) names.Add(m.Objectname);
        LastModifiers = string.Join(", ", names.ToArray());
        List<int> tides = TideEntries(pristine);
        for (int t = 1; t <= tides.Count; t++)
        {
            int i = tides[t - 1], row = Mathf.Min(RowOf(t, finish), tides.Count);
            Wavesobject.Waves w = table[i], from = pristine[tides[row - 1]];
            rowAt[i] = row;
            // The break before this Tide warns of what the row brings: the last note of the rows it stands for.
            if (i > 0 && !table[i - 1].Wave)
            {
                Wavesobject.Waves b = table[i - 1];
                string note = "";
                for (int r = (t > 1 ? Mathf.Min(RowOf(t - 1, finish), tides.Count) : 0) + 1; r <= row; r++)
                    if (tides[r - 1] > 0 && !string.IsNullOrEmpty(pristine[tides[r - 1] - 1].Note)) note = pristine[tides[r - 1] - 1].Note;
                b.Note = note;
                table[i - 1] = b;
            }
            w.Wavetime = from.Wavetime;
            if (from.Content == null)
            {
                w.Content = null;
                table[i] = w;
                continue;
            }
            List<Wavesobject.Wavecontent> rows = new List<Wavesobject.Wavecontent>();
            int nth = t - 1;     // F4: a one-row Tide takes each modifier in turn, not always the first
            foreach (Wavesobject.Wavecontent c0 in from.Content)
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
                int marked = mods.Count > 0 ? total / ModifierShare : 0;
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
        RWLog.Info("map " + map + " for " + players + " players: Tide 15 plays row " + finish + ", foes x" + RWUtil.F(scale) + ", intervals x" + RWUtil.F(interval) + ", health x" + RWUtil.F(1f + LastHealth) + " (" + hoard + " held), modifiers [" + LastModifiers + "]");
    }

    // Every machine, with the content: a foe's synced upgrade list names the map's health upgrade, and a client that has
    // none registered logs "Could not retrieve Upgrade/..." each time the foe is selected (kit docs/13 §7, MAX P1-7).
    public static void BuildHealth()
    {
        for (int map = 1; map <= RWChart.Rows; map++)
            Health(map);
    }

    static Upgradeproperties Health(int map)
    {
        if (map < 1)
            return null;
        Upgradeproperties u;
        if (health.TryGetValue(map, out u))
            return u;
        // No script: the stock Entity_Ai_Enemy_Health_Upgrade also writes Skin "Health", the Hardened elite's look, over the
        // foe's own. The upgrade only marks the foe, and Update adds the mod; its param shows the map's part (F5 adds on).
        u = RWContent.Upgrade("RW_Foe_Health" + map + "_Upgradeobject", "", "", null, "Health_Pmulti", HealthFor(map, 0));
        health[map] = u;
        return u;
    }

    // Host, in a map: each foe that carries this map's health upgrade gets its Health Pmulti once, a quarter second after it
    // comes at the latest; a change of max Health keeps the foe's share of health (kit docs/17 §2.1), so it still comes whole.
    // A Pmulti scales the foe's base, so it compounds with an elite's Health Multi instead of adding to it (F2).
    public static void Update()
    {
        Game_Entity ge = Game_Entity.Instance;
        if (ge == null || !ge.IsServer || Game_Mapvars.Instance == null || healthId == null || Time.time < nextLook)
            return;
        nextLook = Time.time + 0.25f;
        List<Entity_Master> enemies;
        if (!ge.Entityclienttags.TryGetValue("Enemy", out enemies))
            return;
        foreach (Entity_Master e in enemies)
        {
            if (!ModKitUtil.Alive(e) || toughened.ContainsKey(e) || e.Upgradelistserver == null || !e.Upgradelistserver.ContainsKey(healthId))
                continue;
            toughened[e] = true;
            if (e.Statsserver != null && e.Statsserver.ContainsKey("Health"))
                Helper_Entitytools.Addmod(e, "Health", LastHealth, Helper_Entitytools.Modtypes.Pmulti, 0f);
        }
        if (toughened.Count < pruneAt)
            return;
        foreach (Entity_Master e in new List<Entity_Master>(toughened.Keys))
            if (!ModKitUtil.Alive(e)) toughened.Remove(e);
        pruneAt = Mathf.Max(256, toughened.Count * 2);
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

    static List<Upgradeproperties> Pool(int map)
    {
        List<Upgradeproperties> on = new List<Upgradeproperties>();
        List<string> pool = CampaignPool(RWCampaign.Seed);
        int count = ModifiersOn(map, pool.Count);
        // A stock elite's upgrade is only there on maps whose groups load it (Molten wasn't on Alt_3B_Woozy, seen on the PC 2026-11-04):
        // the next one of the campaign's, then of the rest, takes its place, so a map never goes easier than its number.
        List<string> reserve = new List<string>(pool);
        foreach (string s in StockElites) if (!reserve.Contains(s)) reserve.Add(s);
        foreach (string s in PackModifiers) if (!reserve.Contains(s)) reserve.Add(s);
        foreach (string name in reserve)
        {
            if (on.Count >= count)
                break;
            Upgradeproperties u = ModKitUtil.Load<Upgradeproperties>("Upgrade", name);
            if (u != null) on.Add(u);
            else RWLog.Once("no modifier " + name, "modifier " + name + " isn't loaded on this map: the next one stands in");
        }
        return on;
    }

    // A break from Tide 15 on. Once a Tide has played LastRow (Tide 29 on map 1, sooner where Tide 15 plays a later row)
    // the party stays in that Tide: rewind Entry onto its entry, top the enemy cores up, and write it one step harder.
    public static void Bonus(int wave)
    {
        Game_Mapvars vars = Game_Mapvars.Instance;
        int played = vars != null ? vars.Entry.Value - 2 : -1;     // the Tide entry this break follows (the row in play is Entry - 1)
        if (table == null || played < 0 || played >= rowAt.Length || played >= table.Count || rowAt[played] < LastRow)
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
        RWLog.Info("bonus Tide " + (wave + 1) + ": row " + LastRow + " again, one step harder");
    }
}
