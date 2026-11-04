// Directions (docs/scope/06 [Directions], 11; kit docs/15 §14.1, docs/17 §11): 3 offered, 1 picked before each map, per
// player, kept on the vessel; a Direction a player holds comes back as its next rank, and VENDCINT sells the next rank for
// Pure Aura. The Direction upgrade itself goes on the vessel (ModKitDirections, kit 1.7), and the part that owns the
// mechanic reads it there: a vessel part registers its own Directions, the mode those any vessel or base vessel can take.
// What a Direction does to a player's buildings and units reaches them through this part's own spawn hook, filtered by
// owner (RogueLab R8, proven): the stock Teamapply would reach everyone's (engine study N13).
public class RWDirectionDef
{
    public string Vessel = "Any", Name = "", Display = "", Script = null, Look = "", Clash = "", Needs = "";
    public string[] Desc = new string[3];       // rank I, II, III
    public string[] Keys = new string[0];       // the stock script's keys (pitfall #117)
    public float[][] Numbers = null;            // per rank, one per key
    public string Reach = "";                   // "Tower" every building, "Mob" every unit; "" only the vessel
    public string Stat = "";                    // a stat on the vessel itself, held while the Direction is
    public bool Plain;                          // an Add to it, not a Multi
    public float[] Amounts = null;              // per rank
}

public static class RWDirections
{
    public const int Offers = 3;                // docs/scope/06: 3 are offered
    public const int RankTwoCost = 20, RankThreeCost = 30;
    public static readonly List<Upgradeproperties> List = new List<Upgradeproperties>();   // every Direction, every part's, by name: index = what crosses the network
    static readonly Dictionary<string, RWDirectionDef> defs = new Dictionary<string, RWDirectionDef>();
    static readonly Dictionary<string, Upgradeproperties> effects = new Dictionary<string, Upgradeproperties>();

    // Defaults Claude picked (docs/scope/11's NEEDS line; docs/reports/SCOPE_FIT_PLAN_2026-10-28.md §7.1, docs/mode/MECHANIC_IDEAS.md §4).
    public static readonly RWDirectionDef[] Mine =
    {
        new RWDirectionDef { Name = "Tougher", Display = "Made of Tougher Stuff", Look = "Direction_Tougher", Reach = "Mob", Needs = "Minions",
            Desc = new[] { "Your minions take reduced damage from attacks", "Your minions take further reduced damage from attacks", "Your minions take further reduced damage, and can only take a max amount of damage per each hit" } },
        new RWDirectionDef { Name = "Barnacle", Display = "Barnacle Coat", Reach = "Tower", Script = "Entity_Ai_Effect_Smackywall_Bramble", Keys = new[] { "Effect_Add" },
            Numbers = new[] { new[] { 2f }, new[] { 4f }, new[] { 7f } },
            Desc = new[] { "Your towers prick what hits them", "Your towers prick harder", "Your towers prick much harder" } },
        new RWDirectionDef { Name = "Mend", Display = "Tidal Mend", Reach = "Tower", Script = "Entity_Ai_Effect_Smackywall_Regen", Keys = new[] { "Range_Base", "Effect_Add" },
            Numbers = new[] { new[] { 2f, 5f }, new[] { 2.5f, 8f }, new[] { 3f, 12f } },
            Desc = new[] { "Your towers mend what stands around them each Tide", "Your towers mend more, a little farther", "Your towers mend much more, farther still" } },
        // The stock script DIVIDES every hit's Damagemulti by Effect_Multi (Entity_Ai_Effect_Smackywall_Beacon.Oninteract): 0 made every
        // hit on the tower infinite and 0.1 made it ten times worse, until a real hit was measured on 2026-11-04 (pitfall #140). It stays
        // at 1, a no-op: a tower that has held a Beacon ignores the next one's number (Alreadyactive, so a rank bought later would do
        // nothing). Rank III's shrug is this part's own hit hook, SpringShrug.
        new RWDirectionDef { Name = "Spring", Display = "Spring Tide", Reach = "Tower", Script = "Entity_Ai_Effect_Smackywall_Beacon", Keys = new[] { "Effect_Multi", "Effect_Add" },
            Numbers = new[] { new[] { 1f, 4f }, new[] { 1f, 7f }, new[] { 1f, 10f } },
            Desc = new[] { "Your towers gain health every Tide", "Your towers gain more health every Tide", "Your towers gain more health every Tide and shrug off a little damage" } },
        new RWDirectionDef { Name = "Cost", Display = "Cut Costs",
            Desc = new[] { "Your buildings cost less", "Your buildings cost less still", "Your buildings cost much less" } },
        new RWDirectionDef { Name = "Refit", Display = "Cheap Refits",
            Desc = new[] { "Your upgrades cost less", "Your upgrades cost less still", "Your upgrades cost much less" } },
        new RWDirectionDef { Name = "Interest", Display = "Compound Interest",
            Desc = new[] { "Each Tide, a little of your unspent base aura is paid on top", "A bigger share of your unspent base aura is paid each Tide", "A bigger share still of your unspent base aura is paid each Tide" } },
        // Each base vessel's is one held number the vessel's own code reads live (kit docs/17 §8; their stock upgrades are
        // read by name, so a pack upgrade can't reach their builds' numbers).
        new RWDirectionDef { Vessel = "Buster", Name = "Gallop", Display = "Full Gallop", Stat = "Speed", Amounts = new[] { 0.1f, 0.2f, 0.3f },
            Desc = new[] { "Buster runs faster", "Buster runs faster still", "Buster runs much faster" } },
        new RWDirectionDef { Vessel = "Fie", Name = "Regulars", Display = "House Regulars", Stat = "Attackspeed", Amounts = new[] { 0.1f, 0.2f, 0.3f },
            Desc = new[] { "Fie works faster", "Fie works faster still", "Fie works much faster" } },
        new RWDirectionDef { Vessel = "Boutman", Name = "Longline", Display = "Long Line", Stat = "Fishingrange", Plain = true, Amounts = new[] { 1f, 2f, 3f },
            Desc = new[] { "Boutman reaches farther with his rod", "Boutman reaches farther still", "Boutman reaches much farther" } },
        new RWDirectionDef { Vessel = "Snosno", Name = "Packedsnow", Display = "Packed Snow", Stat = "Speed", Amounts = new[] { 0.08f, 0.16f, 0.25f },
            Desc = new[] { "Sno Sno slides faster", "Sno Sno slides faster still", "Sno Sno slides much faster" } },
        new RWDirectionDef { Vessel = "Smacky", Name = "Skipping", Display = "Skipping Stones", Stat = "Damage", Amounts = new[] { 0.15f, 0.3f, 0.5f },
            Desc = new[] { "Smacky hits harder", "Smacky hits harder still", "Smacky hits much harder" } },
    };

    public static readonly float[] CostScale = { 1f, 0.9f, 0.8f, 0.7f };
    public static readonly float[] RefitScale = { 1f, 0.9f, 0.8f, 0.7f };
    public static readonly float[] InterestShare = { 0f, 0.03f, 0.05f, 0.08f };
    public const float InterestCap = 40f;
    public static readonly float[] TougherResist = { 0f, 0.15f, 0.25f, 0.35f };
    public const float TougherMaxShare = 0.25f;     // rank III: one hit takes at most this share of the unit's max health
    public const float SpringShrug = 0.1f;          // Spring Tide rank III: a tenth off every hit on a tower

    public static void Build()
    {
        foreach (RWDirectionDef d in Mine)
        {
            string vessel = d.Vessel;
            for (int r = 1; r <= 3; r++)
            {
                string name = d.Name + (r > 1 ? r.ToString() : "");
                Upgradeproperties u = RWContent.Upgrade(ModKitDirections.Id(vessel, name), d.Display + (r == 2 ? " II" : r == 3 ? " III" : ""), d.Desc[r - 1], null);
                defs[u.Objectname] = d;
                if (d.Script != null)
                {
                    List<object> kv = new List<object>();
                    for (int k = 0; k < d.Keys.Length; k++) { kv.Add(d.Keys[k]); kv.Add(d.Numbers[r - 1][k]); }
                    effects[d.Name + r] = RWContent.Upgrade("RW_Effect_" + d.Name + r + "_Upgradeobject", d.Display, d.Desc[r - 1], d.Script, kv.ToArray());
                }
            }
        }
    }

    // Every machine, every vessel part's Directions included: built after the parts booted, sorted by name, so an index
    // means the same on every machine (every player has the whole pack).
    public static void Refresh()
    {
        List.Clear();
        List.AddRange(ModKitDirections.All());
        List.Sort((a, b) => string.CompareOrdinal(a.Objectname, b.Objectname));
    }

    public static string VesselName(string hero) { return hero.StartsWith("Hero_") ? hero.Substring(5) : hero; }

    static bool HasMinions(string hero)
    {
        // The vessels that send minions (docs/scope/11 "Any Vessel with (Minions)"): all of them. Fie's Knight's Keep sends
        // knights and Buster's Garage dogs, both tagged Mob (kit docs/17 §10); both were left out until 2026-11-04.
        return hero == "Hero_Boutman" || hero == "Hero_Snosno" || hero == "Hero_Spool" || hero == "Hero_Yossele" || hero == "Hero_Argo" || hero == "Hero_Uloe" || hero == "Hero_Smacky" || hero == "Hero_Fie" || hero == "Hero_Buster";
    }

    // The offers for one player: next ranks of what they hold, and new ones for their vessel, seeded so a retry of the
    // same pick offers the same cards. Clashes (only one per Clash name) and what the vessel lacks are left out.
    public static List<int> Roll(RWPlayer p, string hero, int seed)
    {
        if (List.Count == 0) Refresh();
        string vessel = VesselName(hero);
        Dictionary<string, int> held = new Dictionary<string, int>();
        foreach (string id in p.Directions)
        {
            string v, n;
            int r;
            if (ModKitDirections.Parse(id, out v, out n, out r))
            {
                int have;
                held.TryGetValue(v + "/" + n, out have);
                if (r > have) held[v + "/" + n] = r;
            }
        }
        List<string> heldClashes = new List<string>();
        foreach (Upgradeproperties u in List)
        {
            RWDirectionDef d;
            string v, n;
            int r;
            if (ModKitDirections.Parse(u.Objectname, out v, out n, out r) && held.ContainsKey(v + "/" + n) && Clash(u.Objectname, out d) != "")
                heldClashes.Add(Clash(u.Objectname, out d));
        }
        List<int> pool = new List<int>();
        for (int i = 0; i < List.Count; i++)
        {
            string v, n;
            int r;
            if (!ModKitDirections.Parse(List[i].Objectname, out v, out n, out r) || (v != "Any" && v != vessel))
                continue;
            int have;
            held.TryGetValue(v + "/" + n, out have);
            if (r != have + 1)
                continue;
            RWDirectionDef d;
            string clash = Clash(List[i].Objectname, out d);
            if (have == 0 && clash != "" && heldClashes.Contains(clash))
                continue;
            if (d != null && d.Needs == "Minions" && !HasMinions(hero))
                continue;
            pool.Add(i);
        }
        System.Random rng = new System.Random(seed);
        List<int> offers = new List<int>();
        while (offers.Count < Offers && pool.Count > 0)
        {
            int k = rng.Next(pool.Count);
            offers.Add(pool[k]);
            pool.RemoveAt(k);
        }
        return offers;
    }

    // A vessel part declares a clash in its Direction's params: "Clash" 1 means "the Tree's Directions" for now.
    static string Clash(string id, out RWDirectionDef def)
    {
        defs.TryGetValue(id, out def);
        if (def != null)
            return def.Clash;
        Upgradeproperties u = Bl_Address.Getload("Upgrade", id) as Upgradeproperties;
        float c;
        return u != null && u.Defaultparams != null && u.Defaultparams.TryGetValue("Clash", out c) ? "Part" + Mathf.RoundToInt(c) : "";
    }

    // The next rank of each Direction a player holds, for the shop (rank I isn't sold).
    public static List<Upgradeproperties> NextRanks(List<string> held)
    {
        List<Upgradeproperties> next = new List<Upgradeproperties>();
        Dictionary<string, int> top = new Dictionary<string, int>();
        foreach (string id in held)
        {
            string v, n;
            int r, have;
            if (!ModKitDirections.Parse(id, out v, out n, out r)) continue;
            top.TryGetValue(v + "/" + n, out have);
            if (r > have) top[v + "/" + n] = r;
        }
        foreach (KeyValuePair<string, int> kv in top)
        {
            if (kv.Value >= 3) continue;
            string[] p = kv.Key.Split('/');
            Upgradeproperties u = Bl_Address.Getload("Upgrade", ModKitDirections.Id(p[0], p[1] + (kv.Value + 1))) as Upgradeproperties;
            if (u != null) next.Add(u);
        }
        return next;
    }

    public static int RankCost(int rank) { return rank >= 3 ? RankThreeCost : RankTwoCost; }

    public static Sprite Picture(string id)
    {
        string v, n;
        int r;
        Sprite s = null;
        RWDirectionDef d;
        if (defs.TryGetValue(id, out d) && d.Look != "") s = RWAssets.Sprite(d.Look);
        if (s == null && ModKitDirections.Parse(id, out v, out n, out r))
        {
            // Two art files carry the scope's whole name, where their Directions go by a shorter one.
            string file = n == "Recycler" ? "EfficientRecycler" : n == "Mushrooms" ? "MotherOfMushrooms" : n;
            s = RWAssets.Like("Direction_" + file);
        }
        return s ?? RWAssets.Sprite("RogueWaves_Crest32");
    }

    // ---------------------------------------------------------------- on the vessel (host)

    // RWRun.Carry, twice a second: everything the player holds is on this vessel. The stock grant ignores a repeat.
    public static void Grant(Entity_Master vessel, RWPlayer p)
    {
        foreach (string id in p.Directions)
        {
            Upgradeproperties u = Bl_Address.Getload("Upgrade", id) as Upgradeproperties;
            if (u == null)
            {
                RWLog.Once("no direction " + id, "held Direction " + id + " isn't loaded: its part is missing");
                continue;
            }
            if (ModKitDirections.Grant(vessel, u))
                Sweep(p);
        }
        foreach (Link_Command c in new List<Link_Command>(vessel.Commandlistserver.Values))
            if (c != null && c.Name != null && c.Name.StartsWith("Buildoption"))
                Scale(vessel, c);
        // A tower has no Upgrade command yet as it spawns, and each upgrade writes a new one (seen on the PC): kept up here.
        if (Top(p, "Any", "Refit") > 0 && Game_Entity.Instance != null)
            foreach (Entity_Master e in new List<Entity_Master>(Game_Entity.Instance.Entityclient))
                if (e != null && e.Beingserver != null && !e.Beingserver.Dead && e.Beingserver.Entity != null && e.Entitydata.Value.Owner == p.Session && e.Beingserver.Entity.Tags.Contains("Tower"))
                    Refit(e, p);
        foreach (RWDirectionDef d in Mine)
        {
            if (d.Stat == "") continue;
            int rank = Top(p, d.Vessel, d.Name);
            if (rank <= 0 || !vessel.Statsserver.ContainsKey(d.Stat)) continue;
            string key = vessel.GetInstanceID() + d.Name;
            Link_Modifier held;
            float want = d.Amounts[rank - 1];
            if (!statMods.TryGetValue(key, out held) || held == null || held.Dead)
                statMods[key] = Helper_Entitytools.Addmod(vessel, d.Stat, want, d.Plain ? Helper_Entitytools.Modtypes.Add : Helper_Entitytools.Modtypes.Multi, 0f);
            else if (d.Plain) held.Value_Add = want;
            else held.Value_Multi = want;
        }
    }

    static readonly Dictionary<string, Link_Modifier> statMods = new Dictionary<string, Link_Modifier>();

    public static void Take(RWPlayer p, Upgradeproperties u)
    {
        if (p.Directions.Contains(u.Objectname))
            return;
        p.Directions.Add(u.Objectname);
        RWLog.Info(p.Name + " took " + u.Objectname);
    }

    public static int Top(RWPlayer p, string vessel, string name)
    {
        int best = 0;
        foreach (string id in p.Directions)
        {
            string v, n;
            int r;
            if (ModKitDirections.Parse(id, out v, out n, out r) && v == vessel && n == name && r > best) best = r;
        }
        return best;
    }

    // Build price scale = the vessel's BuildoptionN Goalvalue (kit docs/17 §6); Boutbro rewrites his own, so Cut Costs
    // doesn't reach what he builds (docs/mode/MECHANIC_IDEAS.md: write his cost stats instead; left for his sheet).
    static void Scale(Entity_Master vessel, Link_Command c)
    {
        RWPlayer p = RWRun.For(vessel.Entitydata.Value.Owner);
        float scale = CostScale[Mathf.Clamp(Top(p, "Any", "Cost"), 0, 3)];
        if (Mathf.Approximately(c.Goalvalue, scale) || scale >= 1f)
            return;
        c.Goalvalue = scale;
        Helper_Entitytools.Updatecommand(vessel, c.Name, "", false, false);
    }

    // ---------------------------------------------------------------- on what a player builds (host)

    public static void Hook()
    {
        if (Game_Entity.Instance == null)
            return;
        Game_Entity.Instance.Onspawn -= OnSpawn;
        Game_Entity.Instance.Ondespawn -= RWCardinals.OnDespawn;
        Game_Entity.Instance.Onspawn += OnSpawn;
        Game_Entity.Instance.Ondespawn += RWCardinals.OnDespawn;
    }

    static void OnSpawn(Entity_Master m)
    {
        if (!RWRun.InCampaignMap || m == null || m.Beingserver == null || m.Beingserver.Entity == null)
            return;
        long owner = m.Entitydata.Value.Owner;
        if (owner < 0)
            return;
        RWPlayer p = RWRun.For(owner);
        Reach(m, p);
        RWCardinals.Reach(m, p);
    }

    static void Reach(Entity_Master m, RWPlayer p)
    {
        List<string> tags = m.Beingserver.Entity.Tags;
        foreach (RWDirectionDef d in Mine)
        {
            if (d.Reach == "" || !tags.Contains(d.Reach) || tags.Contains("Hero"))
                continue;
            int rank = Top(p, d.Vessel, d.Name);
            if (rank <= 0)
                continue;
            if (d.Script != null)
            {
                Upgradeproperties u;
                if (!effects.TryGetValue(d.Name + rank, out u))
                    continue;
                for (int r = 1; r < rank; r++)
                {
                    Upgradeproperties lower;
                    if (effects.TryGetValue(d.Name + r, out lower) && m.Upgradelistserver != null && m.Upgradelistserver.ContainsKey(lower.Objectname))
                        Helper_Entitytools.Removeupgrade(m, lower);
                }
                if (m.Upgradelistserver != null && !m.Upgradelistserver.ContainsKey(u.Objectname))
                    Helper_Entitytools.Upgrade(m, u, Helper_Entitytools.Upgradecheck.Forced, null);
                if (d.Name == "Spring" && rank >= 3)
                    RWHits.Resist(m, SpringShrug, 0f);
            }
            else if (d.Name == "Tougher")
                RWHits.Resist(m, TougherResist[rank], rank >= 3 ? TougherMaxShare : 0f);
        }
        if (tags.Contains("Tower"))
            Refit(m, p);
    }

    // Upgrade price scale = the building's own Upgrade command's Goalvalue (kit docs/17 §6).
    static void Refit(Entity_Master m, RWPlayer p)
    {
        float scale = RefitScale[Mathf.Clamp(Top(p, "Any", "Refit"), 0, 3)];
        Link_Command c;
        if (scale >= 1f || m.Commandlistserver == null || !m.Commandlistserver.TryGetValue("Upgrade", out c) || c == null || Mathf.Approximately(c.Goalvalue, scale))
            return;
        c.Goalvalue = scale;
        Helper_Entitytools.Updatecommand(m, "Upgrade", "", false, false);
    }

    // What a player already built when they took a Direction or a Cardinal.
    public static void Sweep(RWPlayer p)
    {
        if (Game_Entity.Instance == null || p.Session < 0)
            return;
        foreach (Entity_Master e in new List<Entity_Master>(Game_Entity.Instance.Entityclient))
            if (e != null && e.Beingserver != null && !e.Beingserver.Dead && e.Beingserver.Entity != null && e.Entitydata.Value.Owner == p.Session)
            {
                Reach(e, p);
                RWCardinals.Reach(e, p);
            }
    }

    // Compound Interest, paid with the Tide's own aura (Fie's silo pattern, kit docs/16 §5).
    public static void Interest(int wave)
    {
        foreach (long s in RWRun.Sessions())
        {
            RWPlayer p = RWRun.For(s);
            float share = InterestShare[Mathf.Clamp(Top(p, "Any", "Interest"), 0, 3)];
            Tds_Clientinfo info;
            if (share <= 0f || Game_Players.Instance == null || !Game_Players.Instance.Prevsessionlist.TryGetValue(s, out info))
                continue;
            List<Link_Resourceset> pay = new List<Link_Resourceset>();
            foreach (Link_Resource r in info.Resources)
                pay.Add(new Link_Resourceset { Name = r.Name.ToString(), Amount = Mathf.Min(InterestCap, Mathf.Floor(r.Amount * share)) });
            Helper_Resources.Giveresources(pay, s, Helper_Resources.Styles.Default);
        }
    }
}

// A front-of-chain hit hook (kit docs/15 §14.7, Yossele's pattern): a unit takes less, and at most a share of its max health
// per hit. Re-put first ten times a second, because an upgrade landing rebuilds the chain.
public static class RWHits
{
    static readonly Dictionary<Entity_Master, float> resist = new Dictionary<Entity_Master, float>();
    static readonly Dictionary<Entity_Master, float> cap = new Dictionary<Entity_Master, float>();
    static readonly Entity_Master.Oninteracts handler = OnHurt;
    static float nextCheck;
    public static int Softened;

    public static void Reset() { resist.Clear(); cap.Clear(); }

    public static void Resist(Entity_Master e, float amount, float maxShare)
    {
        if (e == null) return;
        resist[e] = amount;
        cap[e] = maxShare;
        First(e);
    }

    static void First(Entity_Master e)
    {
        e.Oninteract = (Entity_Master.Oninteracts)Delegate.Remove(e.Oninteract, handler);
        e.Oninteract = (Entity_Master.Oninteracts)Delegate.Combine(handler, e.Oninteract);
    }

    public static void Update()
    {
        if (resist.Count == 0 || Time.time < nextCheck)
            return;
        nextCheck = Time.time + 0.1f;
        foreach (Entity_Master e in new List<Entity_Master>(resist.Keys))
        {
            if (e == null) { resist.Remove(e); cap.Remove(e); continue; }
            First(e);
        }
    }

    static void OnHurt(Link_Interact hit)
    {
        try
        {
            if (hit == null || hit.Type != "Hurt" || hit.To == null || hit.Data == null)
                return;
            Entity_Master m = hit.To;
            float r, c;
            if (!resist.TryGetValue(m, out r))
                return;
            float multi;
            if (!hit.Data.TryGetValue("Damagemulti", out multi))
                return;
            multi *= 1f - r;
            cap.TryGetValue(m, out c);
            float damage;
            Link_Stats hp;
            if (c > 0f && hit.Data.TryGetValue("Damage", out damage) && damage > 0f && m.Statsserver.TryGetValue("Health", out hp))
                multi = Mathf.Min(multi, c * hp.Value_Calc / damage);
            hit.Data = new Dictionary<string, float>(hit.Data);    // the same dictionary may go to the next target
            hit.Data["Damagemulti"] = multi;
            Softened++;
        }
        catch (Exception e) { RWLog.Once("hits", "hit hook: " + e.Message); }
    }
}
