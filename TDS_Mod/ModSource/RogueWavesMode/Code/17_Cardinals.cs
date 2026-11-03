// Cardinals at VENDCINT (docs/scope/12; kit docs/15 §7, §13; docs/reports/SCOPE_FIT_PLAN_2026-10-28.md §4): bought with Pure
// Aura, each of two kinds, and each stacking with no limit where its sheet says so.
//   Team    acts on the core, the foes or the clock: the upgrade goes on the core with its Artifactobject passed (nine stock
//           scripts throw without one, RogueLab N3); it shows in the HUD's Cardinal row through the synced lobby list while
//           the row has room (8 counters, lobby picks included), and in the run line's count past that
//   Vessel  acts on one player's things: the upgrade goes on that player's vessel or on what they build, by the owner hook
// "The same upgrade twice does nothing" (RogueLab R2), so a stack is a numbered copy of its upgrades, made at boot.
// The stock Cardinals the shop sells are granted as Riptide granted them (proven); the left-out ones fight the run's own
// rules (docs/reports/SCOPE_VS_LOOP.md #7). The list is a default for docs/scope/12's NEEDS lines.
public class RWCardinalDef
{
    public string Name = "", Display = "", Desc = "", Script = null, Look = "", Team = "Red";
    public bool Vessel, Stacks = true;
    public int Cost = 15;
    public object[] Numbers = new object[0];
    public string Reach = "";       // Vessel only: "Hero" (default) the vessel, "Mob" every unit they own
}

public static class RWCardinals
{
    public const int Stacks = 6;     // numbered copies made at boot; buying past them still takes Pure Aura for no more effect
    public const float DenturesHeal = 0.2f;   // of the damage the vessel dealt, per stack
    public const int StockCost = 15;
    public const int OnShelf = 3;    // Cardinals on the shelf in a break, rolled per map and Tide, the same on every machine

    public static readonly RWCardinalDef[] Mine =
    {
        new RWCardinalDef { Name = "Dentures", Display = "Vampire Dentures", Vessel = true, Cost = 15, Look = "Cardinal_VampireDentures", Desc = "Your vessel attacks heal themselves for a portion of damage dealt" },
        new RWCardinalDef { Name = "Hull", Display = "Spiny Hull", Vessel = true, Cost = 12, Script = "Entity_Ai_Effect_Smackywall_Bramble", Numbers = new object[] { "Effect_Add", 4f }, Desc = "Your vessel pricks what hits it" },
        new RWCardinalDef { Name = "Legs", Display = "Sea Legs", Vessel = true, Stacks = false, Cost = 10, Script = "Entity_Ai_Effect_Artifact_Slippers", Numbers = new object[] { "Speed_Base", 0.2f }, Desc = "Your vessel speeds up while it keeps moving" },
        new RWCardinalDef { Name = "Rising", Display = "Rising Tide", Vessel = true, Stacks = false, Cost = 20, Script = "Entity_Ai_Effect_Artifact_Caffeine",
            Numbers = new object[] { "Speed_Base", 0.01f, "Damage_Base", 0.03f, "Attackspeed_Base", 0.02f, "Sellrate_Base", 0f, "Boost_Duration", 3f, "Damage_Pmulti", 0f, "Attackspeed_Pmulti", 0f, "Speed_Pmulti", 0f }, Desc = "Your vessel grows stronger every Tide" },
        new RWCardinalDef { Name = "Puffer", Display = "Puffer Float", Vessel = true, Stacks = false, Cost = 15, Reach = "Mob", Script = "Entity_Ai_Effect_Artifact_Titan",
            Numbers = new object[] { "Health_Pmulti", 0f, "Range_Base", 1.5f, "Damage_Base", 4f, "Damage2_Base", 6f, "Damage3_Base", 8f, "Damage4_Base", 10f, "Damage5_Base", 12f }, Desc = "Your units burst when they fall" },
        new RWCardinalDef { Name = "Clock", Display = "Tide Clock", Stacks = false, Cost = 15, Script = "Entity_Ai_Artifact_Vintage", Numbers = new object[] { "Timeinit_Base", 15f, "Time_Base", 15f }, Desc = "Every break lasts longer" },
        new RWCardinalDef { Name = "Bell", Display = "Rusted Bell", Stacks = false, Cost = 20, Script = "Entity_Ai_Artifact_Wavebreaker", Numbers = new object[] { "Range_Base", 6f, "Debuff_Base", -0.15f }, Desc = "Foes near the Basin lose health while they stay" },
        new RWCardinalDef { Name = "Wind", Display = "Second Wind", Stacks = false, Cost = 25, Script = "Entity_Ai_Effect_Artifact_Loop", Desc = "Once a map, the first hit on the Basin clears the field and replays the Tide" },
    };

    // Stock Cardinals on sale: Riptide's whitelist of the 40, less docs/reports/SCOPE_VS_LOOP.md #7's and MECHANIC_IDEAS §7's.
    public static readonly string[] NotSold =
    {
        "Artifact_Moon", "Artifact_Mooncore", "Artifact_Loop", "Artifact_Loopedmirror", "Artifact_Vintage", "Artifact_Samedayshipping", "Artifact_Pig", "Artifact_Moneybag",
        "Artifact_Sandbox", "Artifact_Sand", "Artifact_Blurryphoto", "Artifact_Checklist", "Artifact_Computer", "Artifact_Bottomrock",
    };

    public static readonly List<string> Sold = new List<string>();      // ids on the shelf's list, sorted, the same on every machine
    static readonly Dictionary<string, RWCardinalDef> defs = new Dictionary<string, RWCardinalDef>();
    static readonly Dictionary<string, Upgradeproperties> copies = new Dictionary<string, Upgradeproperties>();    // "<Name><n>"
    static readonly Dictionary<string, Artifactobject> arts = new Dictionary<string, Artifactobject>();
    static readonly List<string> stock = new List<string>();

    public static string Id(RWCardinalDef d) { return "Cardinal_" + d.Name; }

    // Every machine at boot: the stock Cardinals' groups (RiptideContent's way).
    public static IEnumerator LoadStock()
    {
        Dictionary<string, UnityEngine.Object> entries = RWUtil.Folder("Artifact");
        if (entries == null)
            yield break;
        List<string> ids = new List<string>(entries.Keys);
        ids.Sort(string.CompareOrdinal);
        foreach (string id in ids)
        {
            Artifactentryobject entry = entries[id] as Artifactentryobject;
            if (entry == null || entry.Artifactloads == null || Array.IndexOf(NotSold, id) >= 0)
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
            if (g != null && g.Loaded && RWUtil.Load<Artifactobject>("Artifact", entry.Artifactloads.Objectname) != null)
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
                Upgradeproperties u = RWContent.Upgrade(Id(d) + i + "_Upgradeobject", d.Display, d.Desc, d.Script, d.Numbers);
                copies[d.Name + i] = u;
                art.Coreupgrades.Add(u);
            }
            Entry(d);
        }
        Sold.AddRange(stock);
        Sold.Sort(string.CompareOrdinal);
    }

    // The entry the HUD's Cardinal row reads for its icon, a copy of any stock one.
    static void Entry(RWCardinalDef d)
    {
        Dictionary<string, UnityEngine.Object> entries = RWUtil.Folder("Artifact");
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

    public static bool IsVessel(string id)
    {
        RWCardinalDef d;
        return defs.TryGetValue(id, out d) && d.Vessel;
    }

    public static Sprite Picture(string id)
    {
        RWCardinalDef d;
        if (defs.TryGetValue(id, out d))
        {
            Sprite s = d.Look != "" ? RWAssets.Sprite(d.Look) : RWAssets.Like("Cardinal_" + d.Name);
            return s ?? RWAssets.Sprite("RogueWaves_Crest32");
        }
        Artifactentryobject e = RWUtil.Item<Artifactentryobject>("Artifact", id);
        return e != null ? e.Display_Icon : null;
    }

    // The shelf for a break: seeded by the campaign and the Tide, so client and host roll the same.
    public static List<string> Shelf(uint seed, int map, int wave)
    {
        System.Random rng = new System.Random(unchecked((int)seed) ^ (map * 7919) ^ (wave * 104729));
        List<string> pool = new List<string>(Sold);
        List<string> shelf = new List<string>();
        while (shelf.Count < OnShelf && pool.Count > 0)
        {
            int k = rng.Next(pool.Count);
            string id = pool[k];
            pool.RemoveAt(k);
            shelf.Add(id);
        }
        return shelf;
    }

    // ---------------------------------------------------------------- bought (host)

    public static string Buy(RWPlayer p, string id)
    {
        int cost = Cost(id);
        if (p.Pure < cost)
            return "Not enough Pure Aura";
        RWCardinalDef d;
        bool mine = defs.TryGetValue(id, out d);
        if (mine && d.Vessel)
        {
            int held = Count(p.Cardinals, id);
            if (!d.Stacks && held > 0) return "Already held";
            p.Cardinals.Add(id + (held + 1));
        }
        else
        {
            int held = Count(RWRun.Team, id);
            if ((!mine || !d.Stacks) && held > 0) return "Already held";
            RWRun.Team.Add(id + "#" + (held + 1));
            if (Game_Entity.Instance != null) GrantTeam(id, held + 1);
        }
        p.Pure -= cost;
        RWLog.Info(p.Name + " bought " + id + " for " + cost + " Pure Aura, " + p.Pure + " left");
        Warn(Display(id).ToUpperInvariant(), mine ? "RogueWaves_Crest" : id);
        RWDirections.Sweep(p);
        return "";
    }

    static int Count(List<string> held, string id)
    {
        int n = 0;
        foreach (string h in held)
            if (h.StartsWith(id) && (h.Length == id.Length || char.IsDigit(h[id.Length]) || h[id.Length] == '#'))
                n++;
        return n;
    }

    static void Warn(string text, string icon)
    {
        Entity_Master core = Core();
        if (core != null && core.Radio != null)
            core.Radio.Uiwarning_ClientRpc(text, icon, 3f);
    }

    static Entity_Master Core() { return RWUtil.PlayerCore(); }

    // ---------------------------------------------------------------- on the core (host)

    // A Team Cardinal goes on every core, with its Cardinal passed, and in the lobby list for the HUD row while it has room.
    static void GrantTeam(string id, int stack)
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
        Tds_Multiplayerinfo info = Tds_Multiplayerinfo.Instance;
        FixedString64Bytes key = new FixedString64Bytes(id);
        if (info != null && !info.Artifacts.Contains(key) && info.Artifacts.Count + (info.Mixedart.Value.Length > 0 ? 1 : 0) < 8)
        {
            info.Artifacts.Add(key);
            shown.Add(id);
        }
    }

    static Artifactobject Object(string id)
    {
        Artifactobject art;
        if (arts.TryGetValue(id, out art)) return art;
        Artifactentryobject e = RWUtil.Item<Artifactentryobject>("Artifact", id);
        return e != null && e.Artifactloads != null ? RWUtil.Load<Artifactobject>("Artifact", e.Artifactloads.Objectname) : null;
    }

    static readonly List<string> shown = new List<string>();

    // A map's first moments: the team's Cardinals go back on (a new core each map).
    public static void GrantAllTeam()
    {
        foreach (string held in RWRun.Team)
        {
            int cut = held.IndexOf('#');
            int stack = 1;
            string id = cut > 0 ? held.Substring(0, cut) : held;
            if (cut > 0) int.TryParse(held.Substring(cut + 1), out stack);
            GrantTeam(id, stack);
        }
    }

    // On the boat: the lobby list goes back to what the players picked (no Cardinal swaps there; the row is rebuilt each map).
    public static void RestoreLobby()
    {
        if (shown.Count == 0 || Tds_Multiplayerinfo.Instance == null || !Tds_Multiplayerinfo.Instance.IsServer)
            return;
        foreach (string id in shown)
            Tds_Multiplayerinfo.Instance.Artifacts.Remove(new FixedString64Bytes(id));
        shown.Clear();
    }

    // ---------------------------------------------------------------- on one player's things (host)

    // RWRun.Carry, twice a second.
    public static void Grant(Entity_Master vessel, RWPlayer p)
    {
        foreach (string held in p.Cardinals)
        {
            Upgradeproperties u;
            RWCardinalDef d = Def(held);
            if (d == null || d.Reach != "" || !copies.TryGetValue(d.Name + StackOf(held, d), out u) || u.Aiscripts.Count == 0)
                continue;
            if (vessel.Upgradelistserver != null && !vessel.Upgradelistserver.ContainsKey(u.Objectname))
                Helper_Entitytools.Upgrade(vessel, u, Helper_Entitytools.Upgradecheck.Forced, arts[Id(d)]);
        }
    }

    static RWCardinalDef Def(string held)
    {
        foreach (RWCardinalDef d in Mine)
            if (held.StartsWith(Id(d)))
                return d;
        return null;
    }

    static int StackOf(string held, RWCardinalDef d)
    {
        int n;
        return int.TryParse(held.Substring(Id(d).Length), out n) ? n : 1;
    }

    // A new thing of this player's (the owner hook), or a sweep: unit-reaching Cardinals.
    public static void Reach(Entity_Master m, RWPlayer p)
    {
        List<string> tags = m.Beingserver.Entity.Tags;
        foreach (string held in p.Cardinals)
        {
            RWCardinalDef d = Def(held);
            Upgradeproperties u;
            if (d == null || d.Reach == "" || !tags.Contains(d.Reach) || tags.Contains("Hero") || !copies.TryGetValue(d.Name + StackOf(held, d), out u))
                continue;
            if (m.Upgradelistserver != null && !m.Upgradelistserver.ContainsKey(u.Objectname))
                Helper_Entitytools.Upgrade(m, u, Helper_Entitytools.Upgradecheck.Forced, arts[Id(d)]);
        }
    }

    public static void OnDespawn(Entity_Master master, bool killed, string reason) { RWObjectives.OnDespawn(master, killed, reason); }

    // ---------------------------------------------------------------- Vampire Dentures, and the row on every machine

    static readonly Dictionary<Entity_Master, float> dealt = new Dictionary<Entity_Master, float>();
    static float nextLook;
    public static float Healed;
    static int seenCount = -1;
    static float loadedAt = -1f;

    public static void Update()
    {
        Row();
        if (!RWRun.InCampaignMap || !RWUtil.IsServer || Time.time < nextLook)
            return;
        nextLook = Time.time + 0.25f;
        foreach (long s in RWRun.Sessions())
        {
            RWPlayer p = RWRun.For(s);
            int stacks = Count(p.Cardinals, "Cardinal_Dentures");
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
