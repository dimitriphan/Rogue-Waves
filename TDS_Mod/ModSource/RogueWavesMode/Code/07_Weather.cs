// Sea weather (docs/scope/04 [Sea Weather]; docs/mode/MECHANIC_IDEAS.md §2): some chart maps carry a weather, rolled with
// the chart, shown on its island and in the Setup panel, announced when the map starts, and paid for in extra Pure Aura.
// Each rides on a stock map object the game only puts on one map (kit docs/16 §3), spawned by the host at map start:
//   Rising Water        Main_Flood (3_Manafold's): the water climbs each Tide, flooded tiles can't be built on
//                       (Block_Flood_Deter), shots through the water slow
//   Quickening Current  the host sets the networked game speed (Game_Mapvars.Time_Scale) to x1.25 from Tide 5 and x1.5
//                       from Tide 10, as Main_Chrono does on Map_Chrono, with the same warning popup
//   Nesting Ground      Tower_Halberdnest (3_Vultures'): a nest near an enemy core that grows each Tide and sends a
//                       Halberd every few Tides
// A map object's being (and every being it spawns, Needs) lives in its home Gameplan's groups, which only that map
// loads: every machine loads those groups from the boat once the weather's map is picked (RWBoat), and a map object never unsubscribes from Tides (docs/16 §3), so it
// stays for the whole map. The extra Pure Aura is data here (PureAuraPerTide); the Pure Aura counter is a later piece.
public static class RWWeather
{
    public const int PureAuraPerTide = 1;        // on top of the 3 every Tide pays (docs/scope/06 [Pure Aura])
    public static int QuickTide = 5, QuickerTide = 10;   // the self-test lowers them
    public const float Quick = 1.25f, Quicker = 1.5f;

    public static int Current;                  // the weather of the map being played (host), Calm otherwise
    public static Entity_Master Flood, Nest;
    public static float FloodStart;
    static int speedStep;
    static readonly Dictionary<int, string> loading = new Dictionary<int, string>();
    static readonly Dictionary<int, bool> tried = new Dictionary<int, bool>();

    public static string Display(int w) { return w >= 0 && w < RWChart.WeatherNames.Length ? RWChart.WeatherNames[w] : "?"; }

    // Display(w) + ": " + Short(w) is the weather's in-game line in docs/scope/04 [Sea Weather].
    public static string Short(int w)
    {
        switch (w)
        {
            case RWChart.RisingWater: return "the water climbs each tide. More Pure Aura";
            case RWChart.QuickeningCurrent: return "the game runs faster from Tide " + QuickTide + ". More Pure Aura";
            case RWChart.NestingGround: return "a halberd nest grows near the foes. More Pure Aura";
        }
        return "";
    }

    static string Being(int w) { return w == RWChart.RisingWater ? "Main_Flood" : w == RWChart.NestingGround ? "Tower_Halberdnest" : ""; }
    static string Home(int w) { return w == RWChart.RisingWater ? "3_Manafold" : w == RWChart.NestingGround ? "3_Vultures" : ""; }

    // Every being the weather's map object spawns must be loaded too. The flood fetches Block_Flood_Deter by name at
    // each Tide (Entity_Ai_Main_Flood.Rise) and throws when it is missing (PC round 1), but no group holds it, so the
    // mod makes a stand-in instead (StandIn: on the boat once the groups are loaded, else in the map's first 30 s) and
    // does not list it here.
    static string[] Needs(int w)
    {
        switch (w)
        {
            case RWChart.RisingWater: return new string[] { "Main_Flood" };   // its deter: see StandIn
            case RWChart.NestingGround: return new string[] { "Tower_Halberdnest", "Enemy_Halberd" };
        }
        return new string[0];
    }

    static string Missing(int w)
    {
        string m = "";
        foreach (string b in Needs(w)) if (RWUtil.Load<Beingobject>("Mob", b) == null) m += (m == "" ? "" : ", ") + b;
        return m;
    }

    public static bool Loaded(int w) { return Missing(w) == ""; }

    // ---------------------------------------------------------------- loading (every machine)

    // Once per weather per launch: a group that is loaded stays loaded, so a second pass finds nothing new (round 2
    // retried every half second while the deter was missing).
    public static void Preload(int w)
    {
        if (Loaded(w) || loading.ContainsKey(w) || tried.ContainsKey(w) || RogueWavesModeMod.Instance == null)
            return;
        loading[w] = "";
        tried[w] = true;
        RogueWavesModeMod.Instance.StartCoroutine(Load(w));
    }

    // The groups the home Gameplan loads, in the game's order: the map's sub-loads, the map, then its mode (the game
    // loads the mode's group with the map, and a stock mode's group can hold beings too); last, every stock mode's.
    static List<string> Groups(int w)
    {
        List<string> groups = new List<string>();
        Gameplandatabaseobject plan = RWUtil.Item<Gameplandatabaseobject>("Gameplan", Home(w));
        Mapentryobject map = plan != null ? plan.Map : null;
        if (map != null)
        {
            foreach (Bl_Address_Group g in map.Subloads) if (g != null && !groups.Contains(g.Objectname)) groups.Add(g.Objectname);
            if (map.Maploads != null && !groups.Contains(map.Maploads.Objectname)) groups.Add(map.Maploads.Objectname);
        }
        if (plan != null && plan.Mode != null && plan.Mode.Modeloads != null && !groups.Contains(plan.Mode.Modeloads.Objectname))
            groups.Add(plan.Mode.Modeloads.Objectname);
        foreach (string m in RWMode.StockModes)
        {
            Modeentryobject mode = RWUtil.Item<Modeentryobject>("Mode", m);
            if (mode != null && mode.Modeloads != null && !groups.Contains(mode.Modeloads.Objectname)) groups.Add(mode.Modeloads.Objectname);
        }
        return groups;
    }

    static IEnumerator Load(int w)
    {
        string from = "";
        foreach (string g in Groups(w))
        {
            string before = Missing(w);
            if (before == "") break;
            Bl_Address.Loadgroup group = null;
            try { group = Bl_Address.Load(g); }
            catch (Exception e) { RWLog.Warn("loading " + g + ": " + e.Message); }
            float waited = 0f;
            while (group != null && !group.Loaded && waited < 120f)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }
            string after = Missing(w);
            from += (from == "" ? "" : ", ") + g + (group != null && group.Loaded ? "" : " (not loaded)") + (after != before ? " [found " + Found(before, after) + "]" : "");
        }
        if (w == RWChart.RisingWater && !HasDeter)
            from += ", " + StandIn();
        string missing = Missing(w);
        RWLog.Info("weather " + Display(w) + ": " + (missing == "" ? "ready" : "MISSING " + missing) + " after loading " + (from == "" ? "nothing" : from));
        loading.Remove(w);
    }

    static string Found(string before, string after)
    {
        string f = "";
        foreach (string b in before.Split(new string[] { ", " }, StringSplitOptions.RemoveEmptyEntries))
            if (("," + after.Replace(" ", "") + ",").IndexOf("," + b + ",") < 0) f += (f == "" ? "" : " ") + b;
        return f;
    }

    // Block_Flood_Deter is in none of the groups Manafold, its hero or any stock mode loads (round 2), yet the flood
    // asks for it by name at each Tide, and the throw unwinds into Mode_Normal.Run, which set the Tide. Stand in a copy
    // of a stock no-build block under that name, registered where Bl_Address.Getload looks (Groups["Mob"]) and where a
    // spawn is matched by id and name (Groupsids, Groupsname). Neither preferred block is loaded on the boat (round 3),
    // so every machine tries again in each map, once a second for its first 30 s (DeterUpdate), and any loaded
    // "Block_No..." being will do.
    const string Deter = "Block_Flood_Deter";
    static readonly string[] DeterLike = { "Block_Noplacement", "Block_Nobuild_Brittle" };
    static Game_Mapvars deterMap;
    static float deterSince, deterNext;
    static bool deterDone;

    public static bool HasDeter { get { return RWUtil.Load<Beingobject>("Mob", Deter) != null; } }

    // Every machine, every frame: in each map, once a second for its first 30 s (its groups are still loading), until
    // the stand-in exists; logs once.
    public static void DeterUpdate()
    {
        Game_Mapvars vars = Game_Mapvars.Instance;
        if (vars == null)
            return;
        float now = Time.realtimeSinceStartup;
        if (vars != deterMap) { deterMap = vars; deterSince = now; deterNext = now; deterDone = false; }
        if (deterDone || now < deterNext)
            return;
        deterNext = now + 1f;
        if (HasDeter) { deterDone = true; return; }
        string got = StandIn();
        if (HasDeter || now - deterSince > 30f)
        {
            deterDone = true;
            RWLog.Info("on the map after " + RWUtil.F(now - deterSince) + " s: " + got);
        }
    }

    static string StandIn()
    {
        if (HasDeter)
            return Deter + " already loaded";
        List<Bl_Address.Loadlist> mob;
        if (!Bl_Address.Groups.TryGetValue("Mob", out mob))
            return "no stand-in for " + Deter + " (no Mob items loaded)";
        Beingobject like = null;
        string seen = "";
        foreach (string n in DeterLike)
            if (like == null) like = RWUtil.Load<Beingobject>("Mob", n);
        foreach (Bl_Address.Loadlist l in mob)
            foreach (KeyValuePair<string, UnityEngine.Object> kv in l.Items)
            {
                if (!kv.Key.StartsWith("Block_")) continue;
                if (seen.IndexOf(kv.Key + " ") < 0) seen += kv.Key + " ";
                if (like == null && kv.Key.StartsWith("Block_No") && kv.Value is Beingobject) like = (Beingobject)kv.Value;
            }
        if (like == null)
            return "no stand-in for " + Deter + " (loaded Block_ beings: " + (seen == "" ? "none" : RWUtil.Clip(seen.Trim())) + ")";
        Beingobject copy = RWUtil.Clone(like, Deter);
        Bl_Address.Loadlist list = new Bl_Address.Loadlist();
        list.Loaded = true;
        list.Percent = 100;
        list.Items[Deter] = copy;
        list.Uids[new FixedString64Bytes(copy.Id)] = copy;
        list.Index.Add(copy);
        mob.Add(list);
        Bl_Address.Groupsids[new FixedString64Bytes(copy.Id)] = copy;
        Bl_Address.Groupsname[new FixedString64Bytes(Deter)] = copy;
        return Deter + " stood in by a copy of " + like.Objectname;
    }

    // ---------------------------------------------------------------- the map (host)

    public static void MapStart()
    {
        Current = RWChart.Calm;
        Flood = Nest = null;
        speedStep = 0;
        RWChart.Node node = RWCampaign.Next;
        if (!RWCampaign.Live || node == null)
            return;
        string plan = Tds_Multiplayerinfo.Instance.Gameplan.Value.ToString();
        if (plan != RWMode.Twin(node.Plan)) { RWLog.Warn("map " + plan + " is not the chart's " + RWMode.Twin(node.Plan) + ": no weather"); return; }
        if (node.Weather == RWChart.Calm)
            return;
        Current = node.Weather;
        RogueWavesModeMod.Instance.StartCoroutine(Begin(node));
    }

    static IEnumerator Begin(RWChart.Node node)
    {
        int w = node.Weather;
        yield return new WaitForSeconds(2f);
        Preload(w);
        for (float t = 0f; !Loaded(w) && t < 60f; t += 0.5f)
            yield return new WaitForSecondsRealtime(0.5f);
        while (w == RWChart.RisingWater && !HasDeter && !deterDone)
            yield return new WaitForSecondsRealtime(0.5f);
        try
        {
            Announce(w, Display(w).ToUpperInvariant() + " ROLLS IN", 10f);
            if (w == RWChart.RisingWater && !HasDeter)
                RWLog.Warn("Rising Water: no " + Deter + " on this machine, so no flood (it would throw at every Tide and break the Tide loop)");
            else if (w == RWChart.RisingWater) Flood = SpawnFlood();
            else if (w == RWChart.NestingGround) Nest = SpawnNest();
            RWLog.Info("weather " + Display(w) + " on " + node.Name + " (" + node.Plan + "): " + (w == RWChart.RisingWater ? "flood " + (Flood != null) : w == RWChart.NestingGround ? "nest " + (Nest != null) : "game speed from Tide " + QuickTide)
                + ", +" + PureAuraPerTide + " Pure Aura a tide");
        }
        catch (Exception e) { RWLog.Warn("weather " + Display(w) + ": " + e); }
    }

    public static void Tide(int wave)
    {
        if (Current != RWChart.QuickeningCurrent || Game_Mapvars.Instance == null)
            return;
        if (wave >= QuickerTide && speedStep < 2)
        {
            speedStep = 2;
            Game_Mapvars.Instance.Time_Scale.Value = Quicker;
            Announce(RWChart.QuickeningCurrent, "THE CURRENT RACES", 10f);
        }
        else if (wave >= QuickTide && speedStep < 1)
        {
            speedStep = 1;
            Game_Mapvars.Instance.Time_Scale.Value = Quick;
            Announce(RWChart.QuickeningCurrent, "THE CURRENT QUICKENS", 10f);
        }
    }

    public static void MapEnd(string result)
    {
        if (Current == RWChart.QuickeningCurrent && Game_Mapvars.Instance != null)
            Game_Mapvars.Instance.Time_Scale.Value = 1f;
        Current = RWChart.Calm;
    }

    // The warning popup on every machine, through an entity's radio (kit docs/15 §18.2).
    static void Announce(int w, string text, float seconds)
    {
        Entity_Master by = null;
        foreach (Entity_Master e in RWUtil.EnemyCores()) { by = e; break; }
        if (by == null || by.Radio == null) { RWLog.Warn("no enemy core to announce '" + text + "'"); return; }
        by.Radio.Uiwarning_ClientRpc(text, RWIcons.For(w), seconds);
    }

    // The water starts just under the map's lowest ground, in the middle of the map, and climbs from there.
    static Entity_Master SpawnFlood()
    {
        Beingobject being = RWUtil.Load<Beingobject>("Mob", "Main_Flood");
        List<Genitem> ground = RWUtil.Ground();
        if (being == null || ground.Count == 0) { RWLog.Warn("Rising Water: being " + (being != null) + ", " + ground.Count + " ground tiles"); return null; }
        float minY = float.MaxValue;
        Vector3 lo = new Vector3(float.MaxValue, 0f, float.MaxValue), hi = new Vector3(float.MinValue, 0f, float.MinValue);
        foreach (Genitem g in ground)
        {
            minY = Mathf.Min(minY, g.Position.y);
            lo = Vector3.Min(lo, g.Position);
            hi = Vector3.Max(hi, g.Position);
        }
        Vector3 at = new Vector3((lo.x + hi.x) / 2f, minY - 1f, (lo.z + hi.z) / 2f);
        Link_Being made = Helper_Entitytools.Spawn(being, at, null, false, Link_Spawn.Spawnstyles.Forced, null);
        Entity_Master flood = made != null ? made.Worldobject : null;
        FloodStart = at.y;
        if (flood != null)
            RWLog.Info("Rising Water at " + RWUtil.V(at) + " (map " + RWUtil.V(lo) + " to " + RWUtil.V(hi) + "), team " + flood.Entitydata.Value.Team + Stats(flood, "Maxheight", "Halfheight", "Quarterheight", "Heightspeed", "Halfheightspeed", "Quarterheightspeed", "Projslow"));
        return flood;
    }

    // The stock nest hooks the Tide counter and never lets go. With the nest dead its next growth throws inside the counter's
    // write, which ends Mode_Normal.Run: the Tides stop for good (seen on the PC). A stock map's nest too, once it can be
    // razed: the hook goes with the nest.
    public static int HooksDropped;

    public static void DropDeadNests()
    {
        Game_Mapvars vars = Game_Mapvars.Instance;
        if (vars == null || vars.Wave.OnValueChanged == null)
            return;
        foreach (Delegate d in vars.Wave.OnValueChanged.GetInvocationList())
        {
            Entity_Ai_Tower_Halberdnest nest = d.Target as Entity_Ai_Tower_Halberdnest;
            if (ReferenceEquals(nest, null))
                continue;
            if (nest != null && nest.Master != null && nest.Master.Beingserver != null && !nest.Master.Beingserver.Dead)
                continue;
            vars.Wave.OnValueChanged = (Unity.Netcode.NetworkVariable<int>.OnValueChangedDelegate)Delegate.Remove(vars.Wave.OnValueChanged, d);
            HooksDropped++;
        }
    }

    // Raze the Nest's target (19_Objectives.cs): the same nest, on order.
    public static Entity_Master SpawnNestFor() { return SpawnNest(); }

    // A nest on a free tile 3 to 7 tiles from an enemy core, on the core's team, as a child of the core like Tide foes.
    static Entity_Master SpawnNest()
    {
        Beingobject being = RWUtil.Load<Beingobject>("Mob", "Tower_Halberdnest");
        List<Entity_Master> cores = RWUtil.EnemyCores();
        if (being == null || cores.Count == 0) { RWLog.Warn("Nesting Ground: being " + (being != null) + ", " + cores.Count + " enemy cores"); return null; }
        Entity_Master core = cores[0];
        Vector3 c = core.Entityposition.Value;
        int team = core.Entitydata.Value.Team;
        List<Genitem> ground = RWUtil.Ground();
        ground.Sort((a, b) => Mathf.Abs(RWUtil.Flat(a.Position, c) - 5f).CompareTo(Mathf.Abs(RWUtil.Flat(b.Position, c) - 5f)));
        foreach (Genitem g in ground)
        {
            float d = RWUtil.Flat(g.Position, c);
            if (d < 3f || d > 7f || Mathf.Abs(g.Position.y - c.y) > 2.5f)
                continue;
            bool ok = false;
            try { ok = Helper_Gridtools.Checkifplaceable(g, being, 0, team); } catch (Exception) { }
            if (!ok)
                continue;
            Link_Spawn spawn = Helper_Entitytools.Createlinkspawnparent(core.Beingserver);
            spawn.Being = being;
            spawn.Genadd = g;
            spawn.Skin = "";
            spawn.Spawnstyle = Link_Spawn.Spawnstyles.Forced;
            Link_Being made = Helper_Entitytools.Spawn(spawn, g.Position, core.Beingserver, true, null);
            Entity_Master nest = made != null ? made.Worldobject : null;
            if (nest != null)
                RWLog.Info("Nesting Ground at " + RWUtil.V(g.Position) + ", " + RWUtil.F(d) + " from the enemy core, team " + team + Stats(nest, "Healthperwave", "Waveperunit", "Respawn"));
            return nest;
        }
        RWLog.Warn("Nesting Ground: no free tile 3-7 from the enemy core at " + RWUtil.V(c));
        return null;
    }

    public static string Stats(Entity_Master e, params string[] names)
    {
        string s = "";
        foreach (string n in names)
        {
            try { s += ", " + n + " " + RWUtil.F(e.Statsserver[n].Value); }
            catch (Exception) { s += ", " + n + " -"; }
        }
        return s;
    }
}
