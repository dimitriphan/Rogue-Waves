// Cardinals and Trinkets at VENDCINT (docs/scope/12; kit docs/15 §7, §13; docs/reports/SCOPE_FIT_PLAN_2026-10-28.md §4): bought
// with Pure Aura; one of the pack's that stacks works for Stacks (6) copies, a stock one is held once. The two kinds
// (Team and Vessel Cardinals until Dimitri renamed them on 2026-11-04):
//   Cardinal acts on the core, the foes or the clock, for everyone: the upgrade goes on the core with its Artifactobject passed
//            (nine stock scripts throw without one, RogueLab N3); it shows in the HUD's Cardinal row through the synced lobby
//            list while the row has room (8 counters, lobby picks included); past that it works and the run line counts it as
//            "+N Cardinals" (B4, docs/scope/12 "No limit"). Every stock Cardinal on sale is one
//   Trinket  acts on one player's things: the upgrade goes on that player's vessel or on what they build, by the owner hook,
//            and every held stack shows on the vessel's panel (one that reaches units by a mark with no script, B4). It is
//            built as a Cardinal is (an Artifactobject and its entry) and keeps its "Cardinal_" id, so a saved campaign
//            still finds it
// The shelf has 2 Cardinal slots and a Trinket slot; what a player can't buy more of (6 copies, a stock one once, lobby
// picks included) leaves it (B3). The lobby's picks are locked once the campaign has played a map, and never one the shop
// doesn't sell (B2, docs/scope/04 [Sea Chart] "Lobby Cardinals").
// "The same upgrade twice does nothing" (RogueLab R2), so a stack is a numbered copy of its upgrades, made at boot.
// The stock Cardinals the shop sells are granted as Riptide granted them (proven); the left-out ones fight the run's own
// rules (docs/reports/SCOPE_VS_LOOP.md #7). The list is a default for docs/scope/12's NEEDS lines.
public class RWCardinalDef
{
    public string Name = "", Display = "", Desc = "", Script = null, Look = "", Team = "Red";
    public bool Trinket, Stacks = true;    // Trinket: one player's (a Vessel Cardinal until 2026-11-04); else a Cardinal, everyone's
    public int Cost = 15;
    public object[] Numbers = new object[0];
    public string Reach = "";       // Trinket only: "Hero" (default) the vessel, "Mob" every unit they own
    public string Clash = "";       // the Name of its pair: a player who holds one is never offered the other (docs/scope/12 "Pairs")
    public bool Wall;               // never offered to a vessel with no (Defense) wall (Spare Plank)
}

public static class RWCardinals
{
    public const int Stacks = 6;     // numbered copies made at boot; buying past them still takes Pure Aura for no more effect
    public const float DenturesHeal = 0.2f;   // of the damage the vessel dealt, per stack
    public const int StockCost = 15;
    public const int OnShelf = 3;    // Cardinals and Trinkets on the shelf in a break, rolled per map and Tide, the same on every machine
    public const int TrinketSlots = 1;   // of OnShelf, kept for a Trinket while one can be bought (audit 12 row 1); a kind short fills from the other

    // 2026-11-04: Claude's seven starter Cardinals (Spiny Hull, Sea Legs, Rising Tide, Puffer Float, Tide Clock, Rusted Bell,
    // Second Wind) are gone at Dimitri's word: each copied a stock Cardinal (docs/reports/ROGUELIKE_DIRECTIONS_CARDINALS_2026-11-04.md).
    // A save that still holds one keeps the id; Def() finds no sheet for it, so it does nothing.
    // Claude's fifteen (built 2026-11-05) are scriptless: RWTrinkets (24_Trinkets.cs) does what each says. Each Look is the
    // icon's name; until it is drawn the Rogue Waves crest stands in (Picture).
    public static readonly RWCardinalDef[] Mine =
    {
        new RWCardinalDef { Name = "Dentures", Display = "Vampire Dentures", Trinket = true, Cost = 15, Look = "Cardinal_VampireDentures", Desc = "Your vessel's attacks heal it for a portion of the damage dealt" },
        new RWCardinalDef { Name = "Spyglass", Display = "Brass Spyglass", Trinket = true, Stacks = false, Cost = 12, Look = "Cardinal_BrassSpyglass", Desc = "Your vessel reaches farther" },
        new RWCardinalDef { Name = "Knife", Display = "Gutting Knife", Trinket = true, Cost = 12, Look = "Cardinal_GuttingKnife", Desc = "Your vessel hits healthy foes harder" },
        new RWCardinalDef { Name = "Oilcan", Display = "Oil Can", Trinket = true, Cost = 12, Look = "Cardinal_OilCan", Desc = "Your towers attack faster" },
        new RWCardinalDef { Name = "Tar", Display = "Tar Bucket", Trinket = true, Cost = 15, Team = "Blue", Look = "Cardinal_TarBucket", Desc = "Your towers mend when left alone" },
        new RWCardinalDef { Name = "Duck", Display = "Rubber Duck", Trinket = true, Cost = 10, Team = "Green", Look = "Cardinal_RubberDuck", Desc = "Your vessel comes back sooner after it drowns" },
        new RWCardinalDef { Name = "Plank", Display = "Spare Plank", Trinket = true, Stacks = false, Wall = true, Cost = 15, Team = "Yellow", Look = "Cardinal_SparePlank", Desc = "One of your walls is free each map, and comes back when it falls" },
        new RWCardinalDef { Name = "Punch", Display = "Punch Card", Trinket = true, Stacks = false, Clash = "Oyster", Cost = 15, Team = "Yellow", Look = "Cardinal_PunchCard", Desc = "Cardinals and Trinkets cost you less" },
        new RWCardinalDef { Name = "Oyster", Display = "Pearl Oyster", Trinket = true, Stacks = false, Clash = "Punch", Cost = 15, Team = "Blue", Look = "Cardinal_PearlOyster", Desc = "Pure Aura grows while you save it" },
        new RWCardinalDef { Name = "Storm", Display = "Bottled Storm", Trinket = true, Cost = 15, Team = "Green", Look = "Cardinal_BottledStorm", Desc = "A storm now, a reward when it passes" },
        new RWCardinalDef { Name = "Oilskin", Display = "Oilskin Coat", Trinket = true, Cost = 12, Team = "Green", Look = "Cardinal_OilskinCoat", Desc = "Some hits slide off your vessel" },
        new RWCardinalDef { Name = "Gull", Display = "Gull Feather", Trinket = true, Stacks = false, Cost = 15, Look = "Cardinal_GullFeather", Desc = "Drowning an elite foe lends your vessel its strength" },
        new RWCardinalDef { Name = "Cork", Display = "Cork Lifebuoy", Stacks = false, Cost = 25, Team = "Green", Look = "Cardinal_CorkLifebuoy", Desc = "The Basin lives through one more killing blow each map" },
        new RWCardinalDef { Name = "Flare", Display = "Signal Flare", Cost = 15, Look = "Cardinal_SignalFlare", Desc = "Now and then, a flare strikes the toughest foe near the Basin" },
        new RWCardinalDef { Name = "Crate", Display = "Cargo Crate", Cost = 20, Team = "Yellow", Look = "Cardinal_CargoCrate", Desc = "VENDCINT brings more Cardinals and Trinkets" },
        new RWCardinalDef { Name = "Whistle", Display = "Tin Whistle", Stacks = false, Cost = 15, Team = "Blue", Look = "Cardinal_TinWhistle", Desc = "The last foes of each Tide are run down" },
    };

    // Stock Cardinals on sale: Riptide's whitelist of the 40, less docs/reports/SCOPE_VS_LOOP.md #7's and MECHANIC_IDEAS §7's.
    public static readonly string[] NotSold =
    {
        "Artifact_Moon", "Artifact_Mooncore", "Artifact_Loop", "Artifact_Loopedmirror", "Artifact_Vintage", "Artifact_Samedayshipping", "Artifact_Pig", "Artifact_Moneybag",
        "Artifact_Sandbox", "Artifact_Sand", "Artifact_Blurryphoto", "Artifact_Checklist", "Artifact_Computer", "Artifact_Bottomrock", "Artifact_Rock",
    };

    public static readonly List<string> Sold = new List<string>();      // ids on the shelf's list, sorted, the same on every machine
    static readonly Dictionary<string, RWCardinalDef> defs = new Dictionary<string, RWCardinalDef>();
    static readonly Dictionary<string, Upgradeproperties> copies = new Dictionary<string, Upgradeproperties>();    // "<Name><n>"
    static readonly Dictionary<string, Upgradeproperties> marks = new Dictionary<string, Upgradeproperties>();      // "<Name><n>": a unit Trinket's panel line
    static readonly Dictionary<string, Artifactobject> arts = new Dictionary<string, Artifactobject>();
    static readonly List<string> stock = new List<string>();

    public static string Id(RWCardinalDef d) { return "Cardinal_" + d.Name; }

    // Every machine at boot: the stock Cardinals' groups (RiptideContent's way).
    public static IEnumerator LoadStock()
    {
        Dictionary<string, UnityEngine.Object> entries = ModKitUtil.Folder("Artifact");
        if (entries == null)
            yield break;
        List<string> ids = new List<string>(entries.Keys);
        ids.Sort(string.CompareOrdinal);
        foreach (string id in ids)
        {
            Artifactentryobject entry = entries[id] as Artifactentryobject;
            bool sold = Array.IndexOf(NotSold, id) < 0;
            if (entry == null || entry.Artifactloads == null || !sold)
                continue;
            Bl_Address.Loadgroup g = null;
            try { g = Bl_Address.Load(entry.Artifactloads.Objectname); }
            catch (Exception e) { RWLog.Warn("loading " + id + ": " + e.Message); }
            float waited = 0f;
            while (g != null && !g.Loaded && waited < 60f)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }
            if (sold && g != null && g.Loaded && ModKitUtil.Load<Artifactobject>("Artifact", entry.Artifactloads.Objectname) != null)
                stock.Add(id);
        }
    }

    public static void Build()
    {
        Sold.Clear();
        foreach (RWCardinalDef d in Mine)
        {
            defs[Id(d)] = d;
            Sold.Add(Id(d));
            Artifactobject art = ScriptableObject.CreateInstance<Artifactobject>();
            RWContent.Retag(art, Id(d));
            art.Coreupgrades = new List<Upgradeproperties>();
            RWContent.Register("Artifact", art);
            arts[Id(d)] = art;
            int n = d.Stacks ? Stacks : 1;
            for (int i = 1; i <= n; i++)
            {
                string label = d.Display + (i > 1 ? " x" + i : "");     // each stack its own line on the panel
                Upgradeproperties u = RWContent.Upgrade(Id(d) + i + "_Upgradeobject", label, d.Desc, d.Script, d.Numbers);
                copies[d.Name + i] = u;
                art.Coreupgrades.Add(u);
                if (d.Trinket && d.Reach != "")
                    marks[d.Name + i] = RWContent.Upgrade(Id(d) + "Held" + i + "_Upgradeobject", label, d.Desc, null, new object[0]);
            }
            Entry(d);
        }
        Sold.AddRange(stock);
        Sold.Sort(string.CompareOrdinal);
    }

    // The entry the HUD's Cardinal row reads for its icon, a copy of any stock one.
    static void Entry(RWCardinalDef d)
    {
        Dictionary<string, UnityEngine.Object> entries = ModKitUtil.Folder("Artifact");
        Artifactentryobject like = null;
        foreach (UnityEngine.Object o in entries.Values) { like = o as Artifactentryobject; if (like != null) break; }
        if (like == null)
            return;
        Artifactentryobject e = UnityEngine.Object.Instantiate(like);
        RWContent.Retag(e, Id(d));
        e.Display_Name = d.Display;
        e.Display_Desc = d.Desc;
        e.Display_Icon = Picture(Id(d));
        e.Type = d.Team;
        e.Mixed_Compatible = false;
        entries[Id(d)] = e;
    }

    public static bool IsMine(string id) { return defs.ContainsKey(id); }

    public static string Display(string id)
    {
        RWCardinalDef d;
        if (defs.TryGetValue(id, out d)) return d.Display;
        Artifactentryobject e = RWUtil.Item<Artifactentryobject>("Artifact", id);
        return e != null ? Bla_Localization.GetText(e.Display_Name) : id;
    }

    public static string Desc(string id)
    {
        RWCardinalDef d;
        if (defs.TryGetValue(id, out d)) return d.Desc;
        Artifactentryobject e = RWUtil.Item<Artifactentryobject>("Artifact", id);
        return e != null ? Bla_Localization.GetText(e.Display_Desc) : "";
    }

    public static int Cost(string id)
    {
        RWCardinalDef d;
        return defs.TryGetValue(id, out d) ? d.Cost : StockCost;
    }

    public static bool IsTrinket(string id)
    {
        RWCardinalDef d;
        return defs.TryGetValue(id, out d) && d.Trinket;
    }

    public static Sprite Picture(string id)
    {
        RWCardinalDef d;
        if (defs.TryGetValue(id, out d))
        {
            Sprite s = d.Look != "" ? RWAssets.Sprite(d.Look) : RWAssets.Like("Cardinal_" + d.Name);
            return s ?? RWAssets.Sprite("RogueWaves_Crest");
        }
        Artifactentryobject e = RWUtil.Item<Artifactentryobject>("Artifact", id);
        return e != null ? e.Display_Icon : null;
    }

    // Copies a player can hold: a pack one that stacks 6, the rest once.
    public static int Cap(string id)
    {
        RWCardinalDef d;
        return defs.TryGetValue(id, out d) && d.Stacks ? Stacks : 1;
    }

    // The shelf for a break: the list shuffled by the campaign and the Tide, so client and host roll the same, then the first
    // Cardinals and the first Trinket not maxed for this player (maxed null: none is), and Cargo Crate's extra cards (extra)
    // from what is left in the same order, of either kind.
    public static List<string> Shelf(uint seed, int map, int wave) { return Shelf(seed, map, wave, null, 0); }

    public static List<string> Shelf(uint seed, int map, int wave, Func<string, bool> maxed) { return Shelf(seed, map, wave, maxed, 0); }

    public static List<string> Shelf(uint seed, int map, int wave, Func<string, bool> maxed, int extra)
    {
        System.Random rng = new System.Random(unchecked((int)seed) ^ (map * 7919) ^ (wave * 104729));
        List<string> order = new List<string>(Sold);
        for (int i = order.Count - 1; i > 0; i--)
        {
            int k = rng.Next(i + 1);
            string t = order[i];
            order[i] = order[k];
            order[k] = t;
        }
        List<string> cards = new List<string>(), trinkets = new List<string>(), spare = new List<string>();
        foreach (string id in order)
        {
            if (maxed != null && maxed(id))
                continue;
            bool trinket = IsTrinket(id);
            List<string> into = trinket ? trinkets : cards;
            if (into.Count < (trinket ? TrinketSlots : OnShelf - TrinketSlots)) into.Add(id);
            else spare.Add(id);
        }
        List<string> shelf = new List<string>(cards);
        shelf.AddRange(trinkets);
        for (int i = 0; shelf.Count < OnShelf + Mathf.Max(0, extra) && i < spare.Count; i++)
            shelf.Add(spare[i]);
        return shelf;
    }

    // This break's shelf for one reader (the host keys each player, a machine its own view ""): rolled once, with what was
    // maxed when it was first read, and kept to the break's end, so a buy leaves the card on it (held) and doesn't pull the
    // next one in (a buyer could otherwise empty the whole list in one break). A new map, a retry or the next break
    // (the table's Entry) rolls a new one. Cargo Crate's cards are counted at the roll too: one bought in a break adds its
    // card from the next. A machine rolls its view before the host rolls that player's (the host's at their first buy), so
    // a Crate bought in between leaves the host's list the longer, with the machine's as its start: no pick is refused.
    static readonly Dictionary<string, List<string>> shelves = new Dictionary<string, List<string>>();

    public static List<string> ShelfFor(string who, uint seed, int map, int wave, Func<string, bool> maxed)
    {
        Game_Mapvars mv = Game_Mapvars.Instance;
        string key = who + "#" + seed + "#" + map + "#" + wave + "#" + (mv != null ? mv.GetInstanceID() + "#" + mv.Entry.Value : "-");
        List<string> shelf;
        if (!shelves.TryGetValue(key, out shelf))
        {
            if (shelves.Count >= 64) shelves.Clear();
            shelves[key] = shelf = Shelf(seed, map, wave, maxed, Mathf.Min(RWTrinkets.CrateMost, CrateStacks()));
        }
        return new List<string>(shelf);
    }

    // The host's reading of what a player can't buy more of, or may not hold beside what they hold (a pair, a vessel's wall).
    public static bool MaxedFor(RWPlayer p, string id)
    {
        if (ClashFor(p, id) != "")
            return true;
        if (IsTrinket(id))
            return Count(p.Trinkets, id) >= Cap(id);
        return Count(RWRun.Cardinals, id) >= Cap(id) || (!IsMine(id) && InLobby(id));
    }

    // Why a player may not take this one, as the refusal says it, or "": the pair they hold, or no wall to spare.
    public static string ClashFor(RWPlayer p, string id)
    {
        RWCardinalDef d;
        if (p == null || !defs.TryGetValue(id, out d))
            return "";
        if (d.Clash != "" && Count(p.Trinkets, "Cardinal_" + d.Clash) > 0)
            return Display("Cardinal_" + d.Clash);
        return d.Wall && RWTrinkets.WallOf(p.Hero) == "" ? "a vessel with no wall" : "";
    }

    // The same on this machine, from its own vessel: the pair's first copy on the vessel's panel, the vessel's being.
    public static string ClashMine(string id) { return ClashHere(id, RWMine.Vessel()); }

    static string ClashHere(string id, Entity_Master v)
    {
        RWCardinalDef d;
        if (!defs.TryGetValue(id, out d))
            return "";
        if (d.Clash != "" && OnPanel(v, "Cardinal_" + d.Clash))
            return Display("Cardinal_" + d.Clash);
        // The pick, as the host reads it (p.Hero): the vessel may be down when the shelf is rolled.
        string hero = RWMine.Hero();
        if (hero == "" && v != null) hero = v.Being.Value.ToString();
        return d.Wall && hero != "" && RWTrinkets.WallOf(hero) == "" ? "a vessel with no wall" : "";
    }

    // A held Trinket's first stack on a vessel's panel, as every machine sees it (the synced upgrade list).
    static bool OnPanel(Entity_Master v, string id)
    {
        RWCardinalDef d;
        return v != null && v.Upgradelist != null && defs.TryGetValue(id, out d) && v.Upgradelist.Contains(new FixedString64Bytes(PanelId(d, 1)));
    }

    // The upgrade a held stack puts on the vessel's panel (Grant): its numbered copy, or its mark when it reaches units.
    static string PanelId(RWCardinalDef d, int stack)
    {
        Upgradeproperties u;
        return (d.Trinket && d.Reach != "" ? marks : copies).TryGetValue(d.Name + stack, out u) ? u.Objectname : Id(d) + stack + "_Upgradeobject";
    }

    // What a player pays: the price, or with Punch Card 20% off, rounded down (docs/scope/12).
    public static int PriceFor(RWPlayer p, string id) { return Price(Cost(id), p != null && Count(p.Trinkets, "Cardinal_Punch") > 0); }

    public static int PriceMine(string id)
    {
        Entity_Master v = RWMine.Vessel();
        return Price(Cost(id), OnPanel(v, "Cardinal_Punch"));
    }

    static int Price(int cost, bool punched) { return punched ? Mathf.FloorToInt(cost * (1f - RWTrinkets.PunchOff) + 0.0001f) : cost; }

    // Copies a player holds of one of the pack's Trinkets, and stacks the run holds of one of its Cardinals (host).
    public static int Held(RWPlayer p, string name) { return p != null ? Count(p.Trinkets, "Cardinal_" + name) : 0; }

    public static int Stacked(string name) { return Count(RWRun.Cardinals, "Cardinal_" + name); }

    // Cargo Crate's stacks: the host's list, or on a machine the copies on a Basin (each stack its own upgrade).
    static int CrateStacks()
    {
        if (ModKitUtil.IsServer)
            return Stacked("Crate");
        int n = 0;
        foreach (Entity_Master core in CoresHere())
        {
            int here = 0;
            for (int i = 1; i <= Stacks; i++)
                if (core.Upgradelist.Contains(new FixedString64Bytes("Cardinal_Crate" + i + "_Upgradeobject"))) here++;
            n = Mathf.Max(n, here);
        }
        return n;
    }

    // The players' Basins as this machine sees them: by the synced tag list, which a machine has where the host's isn't.
    static List<Entity_Master> CoresHere()
    {
        List<Entity_Master> cores = new List<Entity_Master>();
        if (Game_Entity.Instance != null)
            foreach (Entity_Master e in Game_Entity.Instance.Entityclient)
                if (e != null && e.Taglist != null && e.Upgradelist != null && e.Taglist.Contains(new FixedString64Bytes("Core")) && !e.Taglist.Contains(new FixedString64Bytes("Enemycore")))
                    cores.Add(e);
        return cores;
    }

    // This machine's reading, from what syncs: a held Trinket's top copy on the vessel, a Cardinal's on a core, a stock one
    // in the row's list (the lobby's picks are there) or its first upgrade on a core.
    public static Func<string, bool> MaxedHere()
    {
        List<Entity_Master> cores = CoresHere();
        Entity_Master v = RWMine.Vessel();
        return id =>
        {
            RWCardinalDef d;
            if (!defs.TryGetValue(id, out d))
            {
                if (InLobby(id)) return true;
                Artifactobject art = Object(id);
                Upgradeproperties first = art != null && art.Coreupgrades != null && art.Coreupgrades.Count > 0 ? art.Coreupgrades[0] : null;
                return first != null && cores.Exists(c => c.Upgradelist.Contains(new FixedString64Bytes(first.Objectname)));
            }
            FixedString64Bytes top = new FixedString64Bytes(PanelId(d, Cap(id)));
            if (ClashHere(id, v) != "")
                return true;
            if (d.Trinket)
                return v != null && v.Upgradelist != null && v.Upgradelist.Contains(top);
            return cores.Exists(c => c.Upgradelist.Contains(top));
        };
    }

    static bool InLobby(string id)
    {
        Tds_Multiplayerinfo info = Tds_Multiplayerinfo.Instance;
        return info != null && (info.Artifacts.Contains(new FixedString64Bytes(id)) || info.Mixedart.Value.ToString() == id);
    }

    // ---------------------------------------------------------------- bought (host)

    public static string Buy(RWPlayer p, string id)
    {
        string clash = ClashFor(p, id);
        if (clash != "")
            return "Clashes with " + clash;
        int cost = PriceFor(p, id);
        if (p.Pure < cost)
            return "Not enough Pure Aura";
        RWCardinalDef d;
        bool mine = defs.TryGetValue(id, out d);
        if (mine && d.Trinket)
        {
            int held = Count(p.Trinkets, id);
            if (held >= Cap(id)) return held > 1 ? "Held " + held + " times" : "Already held";
            p.Trinkets.Add(id + (held + 1));
        }
        else
        {
            int held = Count(RWRun.Cardinals, id);
            if (held >= Cap(id) || (!mine && InLobby(id) && !shown.Contains(id))) return held > 1 ? "Held " + held + " times" : "Already held";
            RWRun.Cardinals.Add(id + "#" + (held + 1));
            if (Game_Entity.Instance != null) GrantCardinal(id, held + 1);
        }
        p.Pure -= cost;
        RWLog.Info(p.Name + " bought " + id + " for " + cost + " Pure Aura, " + p.Pure + " left");
        Warn(Display(id).ToUpperInvariant(), mine ? "RogueWaves_Crest" : id);
        RWTrinkets.OnBuy(p, id);
        RWDirections.Sweep(p);
        return "";
    }

    static int Count(List<string> held, string id)
    {
        int n = 0;
        foreach (string h in held)
            if (h.StartsWith(id, StringComparison.Ordinal) && (h.Length == id.Length || char.IsDigit(h[id.Length]) || h[id.Length] == '#'))
                n++;
        return n;
    }

    // Every Cardinal the run holds: each bought stack, and the lobby's picks (Magpie Nest counts them).
    public static int HeldCount()
    {
        int n = RWRun.Cardinals.Count;
        Tds_Multiplayerinfo info = Tds_Multiplayerinfo.Instance;
        if (info != null)
        {
            foreach (FixedString64Bytes a in info.Artifacts)
                if (!shown.Contains(a.ToString())) n++;
            if (info.Mixedart.Value.Length > 0) n++;
        }
        return n;
    }

    // Held Cardinals the row has no room for (its 8 counters, lobby picks included): the run line shows "+N" (B4).
    public static int Overflow()
    {
        Tds_Multiplayerinfo info = Tds_Multiplayerinfo.Instance;
        if (info == null || RWRun.Cardinals.Count == 0)
            return 0;
        List<string> ids = new List<string>();
        foreach (string held in RWRun.Cardinals)
        {
            int cut = held.IndexOf('#');
            string id = cut > 0 ? held.Substring(0, cut) : held;
            if (!ids.Contains(id) && !info.Artifacts.Contains(new FixedString64Bytes(id)))
                ids.Add(id);
        }
        return ids.Count;
    }

    static void Warn(string text, string icon)
    {
        Entity_Master core = Core();
        if (core != null && core.Radio != null)
            core.Radio.Uiwarning_ClientRpc(text, icon, 3f);
    }

    static Entity_Master Core() { return RWUtil.PlayerCore(); }

    // ---------------------------------------------------------------- Cardinals: on the core (host)

    // A Cardinal goes on every core, with its Artifactobject passed, and in the lobby list for the HUD row while it has room.
    static void GrantCardinal(string id, int stack)
    {
        List<Entity_Master> cores = RWUtil.PlayerCores();
        if (cores.Count == 0)
            return;
        Artifactobject art = Object(id);
        if (art == null)
        {
            RWLog.Once("no card " + id, "Cardinal " + id + " isn't loaded");
            return;
        }
        RWCardinalDef d;
        List<Upgradeproperties> ups = new List<Upgradeproperties>();
        if (defs.TryGetValue(id, out d))
        {
            Upgradeproperties u;
            if (copies.TryGetValue(d.Name + stack, out u)) ups.Add(u);
        }
        else
            ups.AddRange(art.Coreupgrades);
        foreach (Entity_Master core in cores)
            foreach (Upgradeproperties u in ups)
                if (u != null && core.Upgradelistserver != null && !core.Upgradelistserver.ContainsKey(u.Objectname))
                    Helper_Entitytools.Upgrade(core, u, Helper_Entitytools.Upgradecheck.Forced, art);
        if (d == null)
        {
            int reached = CatchUp(art, cores[0]);
            if (reached > 0) RWLog.Info(id + " caught up on " + reached + " things already standing");
        }
        Tds_Multiplayerinfo info = Tds_Multiplayerinfo.Instance;
        FixedString64Bytes key = new FixedString64Bytes(id);
        if (info != null && !info.Artifacts.Contains(key) && info.Artifacts.Count + (info.Mixedart.Value.Length > 0 ? 1 : 0) < 8)
        {
            info.Artifacts.Add(key);
            shown.Add(id);
        }
    }

    // The stock apply-kinds hook Game_Entity.Onspawn or the core's Child, so they reach only what comes after them: a Cardinal
    // bought mid-map would do nothing to the vessel and the towers already standing (MODE-C1). They get the same upgrades
    // here, with the filters the game's scripts use (Riptide's CatchUp, proven in game; Voiceapply's filter is kit docs/17 §9).
    // A repeat is skipped, so the map-start grant runs it too. Returns how many things got something.
    public static int CatchUp(Artifactobject art, Entity_Master core)
    {
        int reached = 0;
        if (art == null || art.Coreupgrades == null || core == null || Game_Entity.Instance == null)
            return 0;
        int team = ModKitUtil.Team(core);
        foreach (Upgradeproperties up in art.Coreupgrades)
        {
            if (up == null || up.Aiscripts == null || up.Defaultparams == null)
                continue;
            string kinds = "";
            foreach (UnityEngine.Object s in up.Aiscripts)
                if (s != null) kinds += s.name + "+";
            bool tower = kinds.Contains("Entity_Ai_Artifact_Towerapply"), mob = kinds.Contains("Entity_Ai_Artifact_Mobapply"),
                teamApply = kinds.Contains("Entity_Ai_Artifact_Teamapply"), voice = kinds.Contains("Entity_Ai_Artifact_Voiceapply"),
                hero = kinds.Contains("Entity_Ai_Artifact_Heroapply");   // Heroapply_Delay too: caught up at once
            if (!tower && !mob && !teamApply && !voice && !hero)
                continue;
            List<Upgradeproperties> give = new List<Upgradeproperties>();
            foreach (KeyValuePair<string, float> p in up.Defaultparams)
            {
                Upgradeproperties u = ModKitUtil.Load<Upgradeproperties>("Upgrade", p.Key);
                if (u != null) give.Add(u);
            }
            if (give.Count == 0)
                continue;
            foreach (Entity_Master e in new List<Entity_Master>(Game_Entity.Instance.Entityclient))
            {
                if (!ModKitUtil.Alive(e) || e.Beingserver.Entity == null || e.Upgradelistserver == null)
                    continue;
                List<string> tags = e.Beingserver.Entity.Tags;
                bool heroParent = ModKitUtil.HasTag(e.Parent, "Hero");
                int n = 0;     // how many of give it gets
                if (tower && tags.Contains("Tower") && ModKitUtil.Team(e) == team)
                    n = give.Count;
                else if (mob && tags.Contains("Mob"))
                    n = give.Count;   // the game's Mobapply compares its own team with itself: every Mob gets it
                else if (teamApply && heroParent && (tags.Contains("Tower") || tags.Contains("Mob")))
                    n = 1;            // Teamapply uses its first upgrade only
                else if (voice && heroParent && (tags.Contains("Tower") || tags.Contains("Trap") || tags.Contains("Mob")))
                    n = give.Count;   // the stock Voiceapply's own filter (game code): the Bouthouse, tagged neither, never gets it (MAX MODEB-5)
                else if (hero && tags.Contains("Hero") && (ModKitUtil.HasTag(e.Parent, "Core") || tags.Contains("Boutbro")))
                    n = give.Count;   // the core's hero children, and Boutbro, who calls every core's Child (kit docs/17 §5)
                bool got = false;
                for (int i = 0; i < n; i++)
                    if (!e.Upgradelistserver.ContainsKey(give[i].Objectname))
                    {
                        Helper_Entitytools.Upgrade(e, give[i], Helper_Entitytools.Upgradecheck.Forced, null);
                        got = true;
                    }
                if (got)
                    reached++;
            }
        }
        return reached;
    }

    static Artifactobject Object(string id)
    {
        Artifactobject art;
        if (arts.TryGetValue(id, out art)) return art;
        Artifactentryobject e = RWUtil.Item<Artifactentryobject>("Artifact", id);
        return e != null && e.Artifactloads != null ? ModKitUtil.Load<Artifactobject>("Artifact", e.Artifactloads.Objectname) : null;
    }

    static readonly List<string> shown = new List<string>();

    // A map's first moments: the team's Cardinals go back on (a new core each map). A map counts as loaded before its Basin stands,
    // so they wait for it in Update: until 2026-11-04 a Cardinal did nothing after the map it was bought on (pitfall #150).
    public static void GrantAllCardinals() { regrant = true; }

    static bool regrant;

    static void GrantHeld()
    {
        foreach (string held in RWRun.Cardinals)
        {
            int cut = held.IndexOf('#');
            int stack = 1;
            string id = cut > 0 ? held.Substring(0, cut) : held;
            if (cut > 0) int.TryParse(held.Substring(cut + 1), out stack);
            GrantCardinal(id, stack);
        }
    }

    // Self-test: the held Cardinals' upgrades not on a core.
    public static int MissingOnCore()
    {
        int missing = 0;
        foreach (string held in RWRun.Cardinals)
        {
            int cut = held.IndexOf('#');
            int stack = 1;
            string id = cut > 0 ? held.Substring(0, cut) : held;
            if (cut > 0) int.TryParse(held.Substring(cut + 1), out stack);
            Artifactobject art = Object(id);
            if (art == null) { missing++; continue; }
            List<Upgradeproperties> ups = new List<Upgradeproperties>();
            RWCardinalDef d;
            if (defs.TryGetValue(id, out d))
            {
                Upgradeproperties u;
                if (copies.TryGetValue(d.Name + stack, out u)) ups.Add(u);
            }
            else
                ups.AddRange(art.Coreupgrades);
            foreach (Entity_Master core in RWUtil.PlayerCores())
                foreach (Upgradeproperties u in ups)
                    if (u != null && (core.Upgradelistserver == null || !core.Upgradelistserver.ContainsKey(u.Objectname))) missing++;
        }
        return missing;
    }

    // The main menu: the lobby the ids were written to is gone, and a later lobby's own picks must not be taken out.
    public static void AtMainMenu() { shown.Clear(); }

    // On the boat: the lobby list goes back to what the players picked (no Cardinal swaps there; the row is rebuilt each map).
    public static void RestoreLobby()
    {
        if (shown.Count == 0 || Tds_Multiplayerinfo.Instance == null || !Tds_Multiplayerinfo.Instance.IsServer)
            return;
        foreach (string id in shown)
            Tds_Multiplayerinfo.Instance.Artifacts.Remove(new FixedString64Bytes(id));
        shown.Clear();
    }

    // ---------------------------------------------------------------- the lobby's picks (host; B2, docs/scope/04 [Sea Chart])

    // The campaign's first map: the lobby's picks as they stand are the campaign's, kept in the save ("<id>,<id>|<mixed>"),
    // less any the shop doesn't sell (they fight the run's own rules, NotSold).
    public static void RecordLobby()
    {
        Tds_Multiplayerinfo info = Tds_Multiplayerinfo.Instance;
        if (info == null || !RWCampaign.Live || RWUtil.Has(RWData.SaveFolder, RWKeys.Lobby))
            return;
        List<string> ids = new List<string>();
        foreach (FixedString64Bytes a in info.Artifacts)
        {
            string id = a.ToString();
            if (!shown.Contains(id) && Array.IndexOf(NotSold, id) < 0 && !ids.Contains(id)) ids.Add(id);
        }
        string mixed = info.Mixedart.Value.ToString();
        if (Array.IndexOf(NotSold, mixed) >= 0) mixed = "";
        RWUtil.WriteString(RWData.SaveFolder, RWKeys.Lobby, string.Join(",", ids.ToArray()) + "|" + mixed);
        RWUtil.Flush();
        RWLog.Info("the lobby's Cardinals are the campaign's now: " + (ids.Count > 0 ? string.Join(", ", ids.ToArray()) : "none") + (mixed != "" ? ", mixed " + mixed : ""));
    }

    // On the boat, twice a second: before the first map a pick the shop doesn't sell comes off; after it the lobby goes back
    // to the campaign's picks. Each said once in chat.
    static readonly List<string> lobbySaid = new List<string>();

    public static void LockLobby()
    {
        Tds_Multiplayerinfo info = Tds_Multiplayerinfo.Instance;
        if (info == null || !info.IsServer || !RWCampaign.Live)
            return;
        bool locked = RWUtil.Has(RWData.SaveFolder, RWKeys.Lobby);
        List<string> want = new List<string>();
        string mixed = "";
        if (locked)
        {
            string saved = RWUtil.ReadString(RWData.SaveFolder, RWKeys.Lobby);
            int bar = saved.IndexOf('|');
            foreach (string id in (bar >= 0 ? saved.Substring(0, bar) : saved).Split(','))
                if (id != "" && Array.IndexOf(NotSold, id) < 0) want.Add(id);
            mixed = bar >= 0 ? saved.Substring(bar + 1) : "";
            if (Array.IndexOf(NotSold, mixed) >= 0) mixed = "";
        }
        List<string> drop = new List<string>();
        foreach (FixedString64Bytes a in info.Artifacts)
        {
            string id = a.ToString();
            if (locked ? !want.Contains(id) : Array.IndexOf(NotSold, id) >= 0) drop.Add(id);
        }
        int changed = 0;
        foreach (string id in drop)
        {
            info.Artifacts.Remove(new FixedString64Bytes(id));
            changed++;
            if (!locked) SayOnce(id, Display(id) + " isn't for a campaign: it came off the lobby's Cardinals.");
        }
        foreach (string id in want)
            if (!info.Artifacts.Contains(new FixedString64Bytes(id)))
            {
                info.Artifacts.Add(new FixedString64Bytes(id));
                changed++;
            }
        string m = info.Mixedart.Value.ToString();
        if (locked ? m != mixed : Array.IndexOf(NotSold, m) >= 0)
        {
            if (!locked) SayOnce(m, Display(m) + " isn't for a campaign: it came off the lobby's Cardinals.");
            info.Mixedart.Value = new FixedString64Bytes(locked ? mixed : "");
            changed++;
        }
        if (changed == 0)
            return;
        if (locked) SayOnce("locked", "The lobby's Cardinals are locked for this campaign.");
        RWLog.Info("lobby Cardinals: " + changed + " put back");
    }

    static void SayOnce(string key, string text)
    {
        if (lobbySaid.Contains(key))
            return;
        lobbySaid.Add(key);
        RWRejoin.Say(text);
    }

    // ---------------------------------------------------------------- Trinkets: on one player's things (host)

    // RWRun.Carry, twice a second: every held Trinket on the vessel, so its panel lists them (B4): one that acts on the vessel
    // as itself (a scriptless one, Vampire Dentures run by Update, as a line alone), one that reaches units by its mark.
    public static void Grant(Entity_Master vessel, RWPlayer p)
    {
        if (vessel.Upgradelistserver == null)
            return;
        foreach (string held in p.Trinkets)
        {
            Upgradeproperties u;
            RWCardinalDef d = Def(held);
            if (d == null || !(d.Reach != "" ? marks : copies).TryGetValue(d.Name + StackOf(held, d), out u))
                continue;
            if (!vessel.Upgradelistserver.ContainsKey(u.Objectname))
                Helper_Entitytools.Upgrade(vessel, u, Helper_Entitytools.Upgradecheck.Forced, arts[Id(d)]);
        }
    }

    // The sheet a held id ("Cardinal_<Name><n>") is a stack of: the name and then digits only, so one name that starts another
    // can't take its stacks.
    static RWCardinalDef Def(string held)
    {
        RWCardinalDef found;
        if (defOf.TryGetValue(held, out found))
            return found;
        foreach (RWCardinalDef d in Mine)
        {
            string id = Id(d);
            if (!held.StartsWith(id, StringComparison.Ordinal))
                continue;
            bool digits = true;
            for (int i = id.Length; i < held.Length; i++) if (!char.IsDigit(held[i])) digits = false;
            if (digits) { found = d; break; }
        }
        defOf[held] = found;
        return found;
    }

    // Def's answer for each held id (Mine is fixed at boot), read for every stack on each Grant and every new thing's Reach.
    static readonly Dictionary<string, RWCardinalDef> defOf = new Dictionary<string, RWCardinalDef>();

    static int StackOf(string held, RWCardinalDef d)
    {
        int n;
        return int.TryParse(held.Substring(Id(d).Length), out n) ? n : 1;
    }

    // A new thing of this player's (the owner hook), or a sweep: unit-reaching Trinkets. Every unit of theirs but the vessel
    // (which holds the Trinket itself, with a mark) takes it, a Hero-tagged one such as Boutbro too, as the Directions do.
    public static void Reach(Entity_Master m, RWPlayer p)
    {
        List<string> tags = m.Beingserver.Entity.Tags;
        foreach (string held in p.Trinkets)
        {
            RWCardinalDef d = Def(held);
            Upgradeproperties u;
            if (d == null || d.Reach == "" || !tags.Contains(d.Reach) || RWRun.IsVessel(m) || RWRun.IsCopy(m) || !copies.TryGetValue(d.Name + StackOf(held, d), out u))
                continue;
            if (m.Upgradelistserver != null && !m.Upgradelistserver.ContainsKey(u.Objectname))
                Helper_Entitytools.Upgrade(m, u, Helper_Entitytools.Upgradecheck.Forced, arts[Id(d)]);
        }
    }

    public static void OnDespawn(Entity_Master master, bool killed, string reason)
    {
        RWObjectives.OnDespawn(master, killed, reason);
        // Inside Game_Entity.Despawn: a throw here would stop the listeners after this one (MODE-C8).
        try { RWAny.OnDespawn(master, killed, reason); }
        catch (Exception e) { RWLog.Once("any despawn", "Directions on a death: " + e.Message); }
        try { RWTrinkets.OnDespawn(master, killed); }
        catch (Exception e) { RWLog.Once("trinket despawn", "Trinkets on a death: " + e.Message); }
    }

    // ---------------------------------------------------------------- Vampire Dentures, and the row on every machine

    static readonly Dictionary<Entity_Master, float> dealt = new Dictionary<Entity_Master, float>();
    static float nextLook;
    public static float Healed;
    static int seenCount = -1;
    static float loadedAt = -1f;

    // A map starts (RWVote.MapStart): the last map's vessels go (MODE-C7).
    public static void Reset() { dealt.Clear(); }

    public static void Update()
    {
        Row();
        if (regrant && RWRun.InCampaignMap && ModKitUtil.IsServer && RWUtil.PlayerCores().Count > 0)
        {
            regrant = false;
            GrantHeld();
        }
        if (!RWRun.InCampaignMap || !ModKitUtil.IsServer || Time.time < nextLook)
            return;
        nextLook = Time.time + 0.25f;
        foreach (long s in RWRun.Sessions())
        {
            RWPlayer p = RWRun.For(s);
            int stacks = Count(p.Trinkets, "Cardinal_Dentures");
            Entity_Master v = RWRun.Vessel(s);
            if (stacks <= 0 || v == null || v.Beingserver == null || v.Beingserver.Dead)
                continue;
            float before;
            if (dealt.TryGetValue(v, out before))
            {
                float gain = v.Damagedone - before;
                Link_Stats hp;
                if (gain > 0f && v.Statsserver.TryGetValue("Health", out hp) && hp.Value < hp.Value_Calc)
                {
                    float heal = gain * DenturesHeal * stacks;
                    Helper_Entitytools.Interactheal(v, v, new Dictionary<string, float> { { "Damage", heal }, { "Damagemulti", 1f } });
                    Healed += heal;
                }
            }
            dealt[v] = v.Damagedone;
        }
    }

    // Every machine: what is new in the lobby list joins this machine's Cardinal row (RiptideArtifacts.Sync, proven).
    static void Row()
    {
        Tds_Multiplayerinfo info = Tds_Multiplayerinfo.Instance;
        Game_Mapvars vars = Game_Mapvars.Instance;
        if (info == null || vars == null || vars.Artifactobjects == null || UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "Game"
            || !info.Gameplan.Value.ToString().StartsWith(RWData.PlanPrefix))
        {
            seenCount = -1;
            loadedAt = -1f;
            return;
        }
        if (loadedAt < 0f)
        {
            Blm_Server server = Blm_Server.Instance;
            Tds_Clientinfo me = server != null && server.Localplayer != null ? server.Localplayer.GetComponent<Tds_Clientinfo>() : null;
            if (me == null || !me.Loaded.Value)
                return;
            loadedAt = Time.realtimeSinceStartup;
        }
        if (Time.realtimeSinceStartup - loadedAt < 2f || info.Artifacts.Count == seenCount)
            return;
        seenCount = info.Artifacts.Count;
        int added = 0;
        foreach (FixedString64Bytes name in info.Artifacts)
        {
            Artifactobject art = Object(name.ToString());
            if (art != null && !vars.Artifactobjects.Contains(art))
            {
                vars.Artifactobjects.Add(art);
                added++;
            }
        }
        if (added > 0)
        {
            Game_Artifacts row = UnityEngine.Object.FindObjectOfType<Game_Artifacts>();
            if (row != null)
                row.SendMessage("Updateartifactlist", SendMessageOptions.DontRequireReceiver);
        }
    }
}
