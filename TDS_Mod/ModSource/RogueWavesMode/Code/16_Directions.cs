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
    public int Ranks = 3;                       // a Duo has one (docs/scope/06 [Directions] ~Duos~)
}

public static class RWDirections
{
    public const int Offers = 3;                // docs/scope/06: 3 are offered
    public const int RankTwoCost = 20, RankThreeCost = 30;
    public static readonly List<Upgradeproperties> List = new List<Upgradeproperties>();   // every Direction, every part's, by name: index = what crosses the network
    static readonly Dictionary<string, RWDirectionDef> defs = new Dictionary<string, RWDirectionDef>();
    static readonly Dictionary<string, Upgradeproperties> effects = new Dictionary<string, Upgradeproperties>();

    // Claude's sheets in docs/scope/11 (By: Claude; docs/reports/SCOPE_FIT_PLAN_2026-10-28.md §7.1, docs/mode/MECHANIC_IDEAS.md §4).
    public static readonly RWDirectionDef[] Mine =
    {
        new RWDirectionDef { Name = "Tougher", Display = "Made of Tougher Stuff", Look = "Direction_Tougher", Reach = "Mob", Needs = "Minions",
            Desc = new[] { "Your minions take reduced damage from attacks", "Your minions take further reduced damage from attacks", "Your minions take further reduced damage, and can only take a max amount of damage per hit" } },
        new RWDirectionDef { Name = "Barnacle", Display = "Barnacle Coat", Reach = "Tower", Script = "Entity_Ai_Effect_Smackywall_Bramble", Keys = new[] { "Effect_Add" },
            Numbers = new[] { new[] { 2f }, new[] { 4f }, new[] { 7f } },
            Desc = new[] { "Your towers prick what hits them", "Your towers prick harder", "Your towers prick much harder" } },
        // Tidal Mend and Spring Tide are the mode's own Tide effects (TideEffects; Dimitri's answer 14 of 2026-11-05, MODE-C2, C3, C5):
        // the stock Regen froze its heal at the level a tower had when the effect started, and the stock Beacon both cancelled Smacky's
        // own Beacon cut and, with its Resistadd, halved every hit on a Fie wall with Regen (pitfall #159; it divides by Effect_Multi too,
        // pitfall #140). Rank III's shrug is this part's own hit hook, SpringShrug.
        new RWDirectionDef { Name = "Mend", Display = "Tidal Mend", Reach = "Tower",
            Desc = new[] { "Your towers mend what stands around them each Tide", "Your towers mend more, a little farther", "Your towers mend much more, farther still" } },
        new RWDirectionDef { Name = "Spring", Display = "Spring Tide", Reach = "Tower", Clash = "Spring",
            Desc = new[] { "Your towers gain health every Tide", "Your towers gain more health every Tide", "Your towers gain more health every Tide and shrug off a little damage" } },
        new RWDirectionDef { Name = "Cost", Display = "Cut Costs",
            Desc = new[] { "Your buildings cost less", "Your buildings cost less still", "Your buildings cost much less" } },
        new RWDirectionDef { Name = "Refit", Display = "Cheap Refits",
            Desc = new[] { "Your upgrades cost less", "Your upgrades cost less still", "Your upgrades cost much less" } },
        new RWDirectionDef { Name = "Interest", Display = "Compound Interest",
            Desc = new[] { "Each Tide, a little of your unspent base aura is paid on top", "A bigger share of your unspent base aura is paid each Tide", "A bigger share still of your unspent base aura is paid each Tide" } },
        // Claude's StarCraft II and roguelike ones (docs/scope/11, built 2026-11-05): the mode's own code reads each one held,
        // RWAny (22_AnyDirections.cs), on the holder's vessel, towers and units, and on the offers. Wound Spring and Spring Tide
        // are a pair (one Clash name); a Duo is offered only to a player who holds both it names, and has one rank.
        new RWDirectionDef { Name = "Wound", Display = "Wound Spring", Clash = "Spring",
            Desc = new[] { "Your towers attack faster", "Your towers attack faster still", "Your towers attack much faster" } },
        new RWDirectionDef { Name = "Sieve", Display = "Fine Sieve",
            Desc = new[] { "Each Tide pays you a little of the colour you hold least", "Each Tide pays you more of it", "Each Tide pays you much more of it" } },
        new RWDirectionDef { Name = "Hands", Display = "Spare Hands", Needs = "Units",
            Desc = new[] { "Each of your unit buildings keeps one more unit", "Your units come back sooner", "Each of your unit buildings keeps two more units, and they come back sooner still" } },
        new RWDirectionDef { Name = "Kettle", Display = "Galley Kettle",
            Desc = new[] { "Your vessel attacks faster the longer it keeps hitting", "It builds up to a faster pace", "It builds up to a much faster pace, and hits harder at full steam" } },
        new RWDirectionDef { Name = "Chain", Display = "Chain Shot",
            Desc = new[] { "Every few hits, your vessel's attack jumps to another foe", "It jumps to two more foes", "It jumps to three more foes, more often" } },
        new RWDirectionDef { Name = "Salt", Display = "Salt Barrel",
            Desc = new[] { "Your vessel has more health", "It has more health, and mends while hurt", "It has much more health, and mends faster" } },
        new RWDirectionDef { Name = "Fog", Display = "Fog Horn",
            Desc = new[] { "Foes near your vessel move slower", "Foes near it move slower still", "Foes near it move much slower, over a wider area" } },
        new RWDirectionDef { Name = "Clam", Display = "Clam Shell",
            Desc = new[] { "Once a map, your vessel lives through a killing blow at 1 health", "After it saves you, nothing can hurt your vessel for a moment", "Twice a map" } },
        new RWDirectionDef { Name = "Oar", Display = "Notched Oar",
            Desc = new[] { "Every 10th hit of your vessel deals double damage", "Every 7th hit", "Every 5th hit" } },
        new RWDirectionDef { Name = "Lamp", Display = "Lamp Oil",
            Desc = new[] { "Foes your vessel drowns burst into flame, hurting foes around them", "The bursts are bigger", "The bursts are much bigger and reach farther" } },
        new RWDirectionDef { Name = "Hook", Display = "Fish Hook",
            Desc = new[] { "Some of your vessel's hits snag, and the foe bleeds", "More hits snag", "Most hits snag, and the bleeding stacks higher" } },
        new RWDirectionDef { Name = "Compass", Display = "Spare Compass",
            Desc = new[] { "One more Direction is offered to you before each map", "Once a map, you can throw back the offer and draw new ones", "Two more Directions are offered" } },
        new RWDirectionDef { Name = "Magpie", Display = "Magpie Nest",
            Desc = new[] { "Your vessel and towers hit harder for every Cardinal and Trinket you hold", "Harder still for each", "Much harder for each" } },
        new RWDirectionDef { Name = "Fireship", Display = "Fire Ship", Needs = "Duo:Lamp+Chain", Ranks = 1,
            Desc = new[] { "Every Chain Shot jump bursts where it lands, as Lamp Oil does", "", "" } },
        new RWDirectionDef { Name = "Pool", Display = "Tide Pool", Needs = "Duo:Mend+Spring", Ranks = 1,
            Desc = new[] { "Mending past full health stays on as extra health until the Tide ends", "", "" } },
        // Each base vessel's is one held number the vessel's own code reads live (kit docs/17 §8; their stock upgrades are
        // read by name, so a pack upgrade can't reach their builds' numbers).
        new RWDirectionDef { Vessel = "Buster", Name = "Gallop", Display = "Full Gallop", Stat = "Speed", Amounts = new[] { 0.1f, 0.2f, 0.3f },
            Desc = new[] { "Buster runs faster", "Buster runs faster still", "Buster runs much faster" } },
        new RWDirectionDef { Vessel = "Fie", Name = "Regulars", Display = "House Regulars", Stat = "Attackspeed", Amounts = new[] { 0.1f, 0.2f, 0.3f },
            Desc = new[] { "Fie works faster", "Fie works faster still", "Fie works much faster" } },
        new RWDirectionDef { Vessel = "Boutman", Name = "Longline", Display = "Spare Reel", Stat = "Fishingrange", Plain = true, Amounts = new[] { 1f, 2f, 3f },
            Desc = new[] { "Boutman reaches farther with his rod", "Boutman reaches farther still", "Boutman reaches much farther" } },
        new RWDirectionDef { Vessel = "Snosno", Name = "Packedsnow", Display = "Packed Snow", Stat = "Speed", Amounts = new[] { 0.08f, 0.16f, 0.25f },
            Desc = new[] { "Sno Sno slides faster", "Sno Sno slides faster still", "Sno Sno slides much faster" } },
        new RWDirectionDef { Vessel = "Smacky", Name = "Skipping", Display = "Skipping Stones", Stat = "Damage", Amounts = new[] { 0.15f, 0.3f, 0.5f },
            Desc = new[] { "Smacky hits harder", "Smacky hits harder still", "Smacky hits much harder" } },
        // The rest are no held stat on the vessel: 23_BaseDirections.cs (RWBase) puts them on what each one touches.
        new RWDirectionDef { Vessel = "Smacky", Name = "Trowel", Display = "Mason's Trowel",
            Desc = new[] { "Your Walls are sturdier", "Your Walls are sturdier still", "Your Walls are much sturdier, and mend when left alone" } },
        new RWDirectionDef { Vessel = "Snosno", Name = "Shovel", Display = "Snow Shovel",
            Desc = new[] { "The snow Sno Sno leaves is deeper", "The snow he leaves is deeper still", "The snow he leaves is much deeper" } },
        new RWDirectionDef { Vessel = "Boutman", Name = "Wormtin", Display = "Worm Tin",
            Desc = new[] { "Boutbro builds for less", "Boutbro builds for less still", "Boutbro builds for much less" } },
        new RWDirectionDef { Vessel = "Fie", Name = "Opensign", Display = "Open Sign",
            Desc = new[] { "Your Rest Stop keeps one more regular", "It keeps two more", "It keeps three more" } },
        new RWDirectionDef { Vessel = "Buster", Name = "Leash", Display = "Long Leash",
            Desc = new[] { "Buster keeps his full reach while dashing", "His dash hits land harder", "His dash hits land much harder" } },
    };

    public static readonly float[] CostScale = { 1f, 0.9f, 0.8f, 0.7f };
    public static readonly float[] RefitScale = { 1f, 0.9f, 0.8f, 0.7f };
    public static readonly float[] InterestShare = { 0f, 0.03f, 0.05f, 0.08f };
    public const float InterestCap = 40f;
    public static readonly float[] TougherResist = { 0f, 0.15f, 0.25f, 0.35f };
    public const float TougherMaxShare = 0.25f;     // rank III: one hit takes at most this share of the unit's max health
    public const float SpringShrug = 0.1f;          // Spring Tide rank III: a tenth off every hit on a tower
    public static readonly float[] SpringHealth = { 0f, 4f, 7f, 10f };        // max health a tower gains each Tide, kept for the map
    public static readonly float[] MendHeal = { 0f, 5f, 8f, 12f };            // flat at each Tide, whatever the tower's level (scope 11; Dimitri 2026-11-06)
    public static readonly float[] MendRange = { 0f, 2f, 2.5f, 3f };

    public static void Build()
    {
        foreach (RWDirectionDef d in Mine)
        {
            string vessel = d.Vessel;
            for (int r = 1; r <= d.Ranks; r++)
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
        // The vessels that send minions (docs/scope/11 "Any Vessel with (Minions)"). Each of the nine has a (Minion) block in
        // its scope (07-10, 16): Smacky's miners, Sno Sno's Snobles, Boutman's Boutbro and fish, Fie's knights, Buster's dogs,
        // Argo's Nauts, Yossele's Cordyfriends, Spool's Crafted, Uloe's Photons. So all nine are here, and Needs="Minions"
        // leaves out only a vessel the pack doesn't know, such as another mod's (checked 2026-11-05, BUILD_UNBUILT §2.7).
        // Fie's knights and Buster's dogs (tagged Mob, kit docs/17 §10) were left out until 2026-11-04.
        return hero == "Hero_Boutman" || hero == "Hero_Snosno" || hero == "Hero_Spool" || hero == "Hero_Yossele" || hero == "Hero_Argo" || hero == "Hero_Uloe" || hero == "Hero_Smacky" || hero == "Hero_Fie" || hero == "Hero_Buster";
    }

    // What a Direction needs of the vessel or of what the player holds: minions, a building that makes units (Sno Sno has
    // no unit count to raise, kit docs/17 §8, and Yossele none either: his Cordyfriends rise from foes), or both Directions
    // a Duo names.
    static bool Meets(RWDirectionDef d, string hero, Dictionary<string, int> held)
    {
        if (d == null || d.Needs == "") return true;
        if (d.Needs == "Minions") return HasMinions(hero);
        if (d.Needs == "Units") return hero != "Hero_Snosno" && hero != "Hero_Yossele";
        if (d.Needs.StartsWith("Duo:"))
        {
            foreach (string n in d.Needs.Substring(4).Split('+'))
                if (!held.ContainsKey("Any/" + n)) return false;
            return true;
        }
        return true;
    }

    // How many ranks a Direction has: 3, or 1 for a Duo (a vessel part's are all 3).
    public static int Ranks(string id)
    {
        RWDirectionDef d;
        return defs.TryGetValue(id, out d) ? d.Ranks : 3;
    }

    public static List<int> Roll(RWPlayer p, string hero, int seed) { return Roll(p, hero, seed, null); }

    // The offers for one player: next ranks of what they hold, and new ones for their vessel, seeded so a retry of the
    // same pick offers the same cards. Clashes (only one per Clash name) and what the vessel lacks are left out. Spare
    // Compass offers more; its redraw passes the cards it throws back as skip, drawn again only if nothing else is left.
    public static List<int> Roll(RWPlayer p, string hero, int seed, List<int> skip)
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
            if (!Meets(d, hero, held))
                continue;
            pool.Add(i);
        }
        System.Random rng = new System.Random(seed);
        List<int> offers = new List<int>(), thrown = new List<int>();
        if (skip != null)
            foreach (int i in skip)
                if (pool.Remove(i)) thrown.Add(i);
        int want = Offers + RWAny.MoreOffers(p);
        foreach (List<int> from in new[] { pool, thrown })
            while (offers.Count < want && from.Count > 0)
            {
                int k = rng.Next(from.Count);
                offers.Add(from[k]);
                from.RemoveAt(k);
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
        return s ?? RWAssets.Sprite("RogueWaves_Crest");
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
        float buildOff = BuildOff(p), refitOff = RefitOff(p);
        Builds(vessel, buildOff);
        // A tower has no Upgrade command yet as it spawns, and each upgrade writes a new one (seen on the PC): kept up here.
        // Boutman's whole tree is on the Bouthouse, and a Fie wall builds its next segment from its own Buildoption1 (MODE-C6).
        if ((refitOff > 0f || buildOff > 0f) && Game_Entity.Instance != null)
            foreach (Entity_Master e in Standing())
            {
                if (!ModKitUtil.Alive(e) || e.Beingserver.Entity == null || e.Entitydata.Value.Owner != p.Session)
                    continue;
                List<string> tags = e.Beingserver.Entity.Tags;
                if (tags.Contains("Tower") || tags.Contains("Bouthouse")) Refit(e, refitOff);
                if (tags.Contains("Fiewall")) Builds(e, buildOff);
            }
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

    // One copy of the entity list a frame for every player's Grant (Carry runs them back to back), kept for reuse.
    static readonly List<Entity_Master> standing = new List<Entity_Master>();
    static int standingFrame = -1;

    static List<Entity_Master> Standing()
    {
        if (standingFrame != Time.frameCount)
        {
            standingFrame = Time.frameCount;
            standing.Clear();
            standing.AddRange(Game_Entity.Instance.Entityclient);
        }
        return standing;
    }

    // A map starts (RWVote.MapStart): the last map's vessels and buildings go (MODE-C7).
    public static void Reset()
    {
        statMods.Clear();
        spring.Clear();
        cuts.Clear();
        pruneAt = 256;
    }

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
            Held h = Parsed(id);
            if (h != null && h.Rank > best && h.Vessel == vessel && h.Name == name) best = h.Rank;
        }
        return best;
    }

    // ModKitDirections.Parse kept for each id (null: no Direction's): the ids are a fixed set, and Top runs for every rank of
    // every player several times a second (RWAny, RWBase, Grant, Reach).
    sealed class Held { public string Vessel, Name; public int Rank; }
    static readonly Dictionary<string, Held> parsed = new Dictionary<string, Held>();

    static Held Parsed(string id)
    {
        Held h;
        if (id == null)
            return null;
        if (!parsed.TryGetValue(id, out h))
        {
            string v, n;
            int r;
            parsed[id] = h = ModKitDirections.Parse(id, out v, out n, out r) ? new Held { Vessel = v, Name = n, Rank = r } : null;
        }
        return h;
    }

    // Build price scale = the BuildoptionN Goalvalue of what builds (kit docs/17 §2.7). Boutbro rewrites his own on every
    // spawn; the cut goes on again within half a second, as the same amount off his larger scale, so less of his price
    // (docs/mode/MECHANIC_IDEAS.md: writing his cost stats would be exact; left for his sheet).
    static float BuildOff(RWPlayer p) { return 1f - CostScale[Mathf.Clamp(Top(p, "Any", "Cost"), 0, 3)]; }

    static float RefitOff(RWPlayer p) { return 1f - RefitScale[Mathf.Clamp(Top(p, "Any", "Refit"), 0, 3)]; }

    // A player's ranks only rise within a map, so a cut of 0 has nothing to undo.
    static void Builds(Entity_Master m, float off)
    {
        if (off <= 0f || m.Commandlistserver == null)
            return;
        options.Clear();
        // Spare Plank's free button is left alone: the cut comes back with the price the Trinket kept (RWTrinkets.Holds).
        foreach (KeyValuePair<string, Link_Command> c in m.Commandlistserver)
            if (c.Value != null && c.Key.StartsWith("Buildoption") && !RWTrinkets.Holds(c.Value))
                options.Add(c);
        foreach (KeyValuePair<string, Link_Command> c in options)
            Lower(m, c.Key, c.Value, off);
    }

    static readonly List<KeyValuePair<string, Link_Command>> options = new List<KeyValuePair<string, Link_Command>>();

    // A price cut is an amount taken off a command's Goalvalue once, and kept for that command: Voice in a Bottle's steps
    // per Tide come off on top, and its reset after an upgrade adds back only its own (MODE-C4). A price someone else
    // moved is a fresh one the game wrote, so the cut goes on again, if it is a build price (Boutbro rewrites his on every
    // spawn, kit docs/17 §5.1) or an upgrade price back at 1 or more; a price only this cut wrote is left as it is.
    sealed class Cut { public Entity_Master Owner; public string Name; public float Off, Written; }
    static readonly Dictionary<Link_Command, Cut> cuts = new Dictionary<Link_Command, Cut>();
    static int pruneAt = 256;

    static void Lower(Entity_Master m, string name, Link_Command c, float off)
    {
        Cut cut;
        if (!cuts.TryGetValue(c, out cut))
        {
            if (cuts.Count >= pruneAt)
                Prune();
            cuts[c] = cut = new Cut { Owner = m, Name = name };
        }
        else if (cut.Off > 0f && !Mathf.Approximately(c.Goalvalue, cut.Written) && (c.Goalvalue >= 0.999f || name.StartsWith("Buildoption")))
            cut.Off = 0f;
        float price = Mathf.Max(0f, c.Goalvalue + cut.Off - off);
        if (Mathf.Approximately(price, c.Goalvalue))
            return;
        cut.Off += c.Goalvalue - price;
        c.Goalvalue = price;
        Helper_Entitytools.Updatecommand(m, name, "", false, false);
        cut.Written = c.Goalvalue;
    }

    // Commands of what is gone, or that the game has since replaced; the next look waits until twice what is left.
    static void Prune()
    {
        foreach (KeyValuePair<Link_Command, Cut> k in new List<KeyValuePair<Link_Command, Cut>>(cuts))
        {
            Link_Command now;
            Entity_Master m = k.Value.Owner;
            if (!ModKitUtil.Alive(m) || m.Commandlistserver == null || !m.Commandlistserver.TryGetValue(k.Value.Name, out now) || !ReferenceEquals(now, k.Key))
                cuts.Remove(k.Key);
        }
        pruneAt = Mathf.Max(256, cuts.Count * 2);
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
        // Foes too: Tin Whistle counts a Tide's arrivals (a throw here would stop the spawn, MODE-C8).
        try { RWTrinkets.OnSpawn(m); }
        catch (Exception e) { RWLog.Once("trinket spawn", "Trinkets on a new spawn: " + e.Message); }
        long owner = m.Entitydata.Value.Owner;
        if (owner < 0)
            return;
        // A Blurry Photo copy is a Hero being the game marks "Clone" only after this spawn returns (pitfall #169), so a thing
        // tagged Hero (Boutbro, a copy) is reached once it has its stats, when a copy shows as one; the rest at once.
        if (m.Beingserver.Entity.Tags.Contains("Hero"))
        {
            RogueWavesModeMod.Instance.StartCoroutine(ReachLater(m, owner));
            return;
        }
        // Inside Game_Entity.Spawn: a throw here would stop the spawn and every Onspawn listener after this one (MODE-C8).
        try
        {
            RWPlayer p = RWRun.For(owner);
            Reach(m, p);
            RWCardinals.Reach(m, p);
        }
        catch (Exception e) { RWLog.Once("spawn reach", "Directions on a new spawn: " + e); }
    }

    static IEnumerator ReachLater(Entity_Master m, long owner)
    {
        for (int i = 0; i < 60 && m != null && (m.Statsserver == null || m.Statsserver.Count == 0); i++)
            yield return null;
        if (!RWRun.InCampaignMap || !ModKitUtil.Alive(m) || m.Statsserver == null || m.Statsserver.Count == 0 || RWRun.IsCopy(m))
            yield break;
        try
        {
            RWPlayer p = RWRun.For(owner);
            Reach(m, p);
            RWCardinals.Reach(m, p);
        }
        catch (Exception e) { RWLog.Once("spawn reach", "Directions on a new spawn: " + e); }
    }

    static void Reach(Entity_Master m, RWPlayer p)
    {
        List<string> tags = m.Beingserver.Entity.Tags;
        foreach (RWDirectionDef d in Mine)
        {
            // Only the player's own vessel is left out: Boutbro is tagged Hero too, and kit docs/17 §8 counts him among Boutman's
            // minions (MAX MODEB-8, Dimitri's answer 39 of 2026-11-05). A Blurry Photo copy is no minion either.
            if (d.Reach == "" || !tags.Contains(d.Reach) || RWRun.IsVessel(m) || RWRun.IsCopy(m))
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
            }
            else if (d.Name == "Spring" && rank >= 3)
                RWHits.Resist(m, SpringShrug, 0f);
            else if (d.Name == "Tougher")
                RWHits.Resist(m, TougherResist[rank], rank >= 3 ? TougherMaxShare : 0f);
        }
        if (tags.Contains("Tower") || tags.Contains("Bouthouse"))
            Refit(m, RefitOff(p));
    }

    // Upgrade price scale = the building's own Upgrade command's Goalvalue (kit docs/17 §2.7).
    static void Refit(Entity_Master m, float off)
    {
        Link_Command c;
        if (off > 0f && m.Commandlistserver != null && m.Commandlistserver.TryGetValue("Upgrade", out c) && c != null)
            Lower(m, "Upgrade", c, off);
    }

    // What a player already built when they took a Direction or a Trinket.
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

    // Each Tide's break, host: Spring Tide grows the health of each of the holder's towers, and Tidal Mend heals what stands
    // around each of them by its rank's flat number, whatever the tower's level (scope 11; Dimitri 2026-11-06).
    static readonly Dictionary<Entity_Master, Link_Modifier> spring = new Dictionary<Entity_Master, Link_Modifier>();
    public static float Mended, Grown;      // self-test
    public static int Mends;                // self-test: how many heals Tidal Mend gave

    public static void TideEffects(int wave)
    {
        if (Game_Entity.Instance == null || wave <= 0)
            return;
        // Tide Pool's extra health lasted the Tide that just ended.
        RWHold.Drop(PoolKey);
        List<Entity_Master> all = null;
        foreach (long s in RWRun.Sessions())
        {
            RWPlayer p = RWRun.For(s);
            int springRank = Mathf.Clamp(Top(p, "Any", "Spring"), 0, 3), mendRank = Mathf.Clamp(Top(p, "Any", "Mend"), 0, 3);
            if (springRank <= 0 && mendRank <= 0)
                continue;
            long pool = Top(p, "Any", "Pool") > 0 ? s : long.MinValue;
            if (all == null)
                all = new List<Entity_Master>(Game_Entity.Instance.Entityclient);
            foreach (Entity_Master t in all)
            {
                if (!OwnTower(t, s))
                    continue;
                if (springRank > 0)
                    Grow(t, SpringHealth[springRank]);
                if (mendRank > 0)
                    MendAround(t, all, MendHeal[mendRank], MendRange[mendRank], pool);
            }
        }
        foreach (Entity_Master t in new List<Entity_Master>(spring.Keys))
            if (!ModKitUtil.Alive(t)) spring.Remove(t);
    }

    static bool OwnTower(Entity_Master t, long session)
    {
        return ModKitUtil.Alive(t) && t.Beingserver.Entity != null && t.Entitydata.Value.Owner == session && t.Statsserver != null
            && t.Statsserver.ContainsKey("Health") && t.Beingserver.Entity.Tags.Contains("Tower") && !t.Beingserver.Entity.Tags.Contains("Hero") && !RWRun.IsCopy(t);
    }

    // One Health Add per tower, raised each Tide (Health is Variable: the tower keeps its share of it, kit docs/17 §2.1).
    static void Grow(Entity_Master t, float amount)
    {
        Link_Modifier m;
        if (!spring.TryGetValue(t, out m) || m == null || m.Dead)
            spring[t] = Helper_Entitytools.Addmod(t, "Health", amount, Helper_Entitytools.Modtypes.Add, 0f);
        else
            m.Value_Add += amount;
        Grown += amount;
    }

    // Self-test: the max health Spring Tide has given this tower so far on this map (its one modifier's Add), 0 if none.
    public static float SpringOn(Entity_Master t)
    {
        Link_Modifier m;
        return t != null && spring.TryGetValue(t, out m) && m != null && !m.Dead ? m.Value_Add : 0f;
    }

    // pool: the session whose towers keep what is mended past full (Tide Pool), or long.MinValue.
    static void MendAround(Entity_Master tower, List<Entity_Master> all, float heal, float range, long pool)
    {
        int team = ModKitUtil.Team(tower);
        Vector3 at = tower.Entityposition.Value;
        foreach (Entity_Master e in all)
        {
            Link_Stats hp;
            if (!ModKitUtil.Alive(e) || e.Beingserver.Entity == null || ModKitUtil.Team(e) != team || e.Statsserver == null || !e.Statsserver.TryGetValue("Health", out hp)
                || ModKitUtil.Flat(e.Entityposition.Value, at) > range)
                continue;
            bool pools = pool != long.MinValue && OwnTower(e, pool);
            float missing = hp.Value_Calc - hp.Value;
            if (missing <= 0f && !pools)
                continue;
            List<string> tags = e.Beingserver.Entity.Tags;
            if (!tags.Contains("Tower") && !tags.Contains("Mob"))
                continue;
            if (missing > 0f)
            {
                Helper_Entitytools.Interactheal(tower, e, new Dictionary<string, float> { { "Damage", heal }, { "Damagemulti", 1f } });
                Mended += heal;
                Mends++;
            }
            float past = heal - Mathf.Max(0f, missing);
            if (pools && past > 0f)
                Pool(e, past);
        }
    }

    // Tide Pool: what was mended past full stays on as an Add to max health, at most a fifth of the tower's own, until the
    // Tide ends (TideEffects takes it off at the next break). Health is Variable, so the tower at full stays at full.
    public const float PoolCap = 0.2f;
    const string PoolKey = "Pool";
    public static float Pooled;     // self-test

    static void Pool(Entity_Master t, float amount)
    {
        float had = RWHold.Get(t, PoolKey);
        float want = Mathf.Min(had + amount, PoolCap * (ModKitUtil.MaxHealth(t) - had));
        if (want > had + 0.001f && RWHold.Set(t, PoolKey, "Health", Helper_Entitytools.Modtypes.Add, want))
            Pooled += want - had;
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

    static void First(Entity_Master e) { ModKitUtil.Front(e, handler); }

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
            ModKitCombat.SetMulti(hit, multi);
            Softened++;
        }
        catch (Exception e) { RWLog.Once("hits", "hit hook: " + e.Message); }
    }
}
