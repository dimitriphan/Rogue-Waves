// The mode a campaign's maps play in (kit docs/15 §2, proven by RogueLab): Mode_RogueWave, a copy of Mode_Normal's
// Modeobject (so its scripts are the stock ones, found by name) with its whole Tide table, registered under one name as
// entry, group and Modeobject. Every base Gameplan gets a twin "RW_<plan>" in this mode: the chart's map nodes play the
// twins, so picking a node on the boat loads its map in Mode_RogueWave. Runs on every machine at boot.
// The Tide table, leaving after Tide 15 and bonus Tides are later pieces (docs/scope/04); today it plays Mode_Normal's.
public static class RWMode
{
    public static Modeobject ModeObject;
    public static Modeentryobject Entry;
    static readonly List<RWChart.Plan> pool = new List<RWChart.Plan>();

    // The base Gameplans a chart node can play, by name (ordinal), with their label: concept-tools/sea_chart.py's POOL.
    public static List<RWChart.Plan> Pool { get { return pool; } }

    public static void Register(Modeentryobject normal)
    {
        Modeobject stock = normal != null && normal.Modeloads != null ? Bl_Address.Getload("Mode", normal.Modeloads.Objectname) as Modeobject : null;
        if (stock == null) { RWLog.Warn("Mode_Normal's Modeobject is not loaded: no Rogue Wave mode"); return; }

        Wavesobject waves = RWUtil.Clone(stock.Enemies, RWData.Mode + "_Waves");
        waves.Wave = new List<Wavesobject.Waves>(stock.Enemies.Wave);
        ModeObject = RWUtil.Clone(stock, RWData.Mode);
        ModeObject.Enemies = waves;

        Bl_Address.Loadgroup group = new Bl_Address.Loadgroup();
        group.Items = new List<Bl_Address.Loadlist>();
        group.Loaded = true;
        group.Percent = 100;
        Bl_Address.Loadlist list = new Bl_Address.Loadlist();
        list.Loaded = true;
        list.Percent = 100;
        list.Fromgroup = group;
        group.Items.Add(list);
        List<Bl_Address.Loadlist> modeLists;
        if (!Bl_Address.Groups.TryGetValue("Mode", out modeLists))
        {
            modeLists = new List<Bl_Address.Loadlist>();
            Bl_Address.Groups["Mode"] = modeLists;
        }
        modeLists.Add(list);
        list.Items[ModeObject.Objectname] = ModeObject;
        list.Index.Add(ModeObject);
        FixedString64Bytes id = new FixedString64Bytes(ModeObject.Id);
        list.Uids[id] = ModeObject;
        ModeObject.Loadsource = RWData.Mode;
        Bl_Address.Groupsname[new FixedString64Bytes(ModeObject.Objectname)] = ModeObject;
        Bl_Address.Groupsids[id] = ModeObject;
        Bl_Address.Loadedfolders[RWData.Mode] = group;

        Entry = RWUtil.Clone(normal, RWData.Mode);
        Entry.Modeloads = RWUtil.Clone(normal.Modeloads, RWData.Mode);
        Entry.Display_Name = RWData.ModeDisplay;
        RWUtil.Folder("Mode")[RWData.Mode] = Entry;

        Dictionary<string, UnityEngine.Object> plans = RWUtil.Folder("Gameplan");
        List<UnityEngine.Object> stockPlans = new List<UnityEngine.Object>(plans.Values);
        pool.Clear();
        foreach (UnityEngine.Object o in stockPlans)
        {
            Gameplandatabaseobject g = o as Gameplandatabaseobject;
            // Special_ plans hang the load (RogueLab round 2); Test_ plans aren't maps; other mods' twins aren't base maps.
            if (g == null || g.Map == null || g.Mode == null || g.Objectname.StartsWith("Special_") || g.Objectname.StartsWith("Test_") || !IsStockMode(g.Mode.Objectname))
                continue;
            Gameplandatabaseobject twin = RWUtil.Clone(g, Twin(g.Objectname));
            twin.Mode = Entry;
            twin.Prediscover = true;      // the Setup panel shows the map's text, not "UNCLASSIFIED" (Macro_Setupui.cs Mapselected)
            plans[twin.Objectname] = twin;
            pool.Add(new RWChart.Plan(g.Objectname, g.Map.Objectname, g.Map.Difficulty));
        }
        pool.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));

        bool found = Tds_Masterdatabase.Instance.Getitem("Mode", RWData.Mode) == Entry && Bl_Address.Getload("Mode", RWData.Mode) == ModeObject;
        RWLog.Info("mode " + RWData.Mode + " registered " + found + ": " + waves.Wave.Count + " table entries, twins of " + pool.Count + " Gameplans");
        List<string> names = new List<string>();
        foreach (RWChart.Plan p in pool) names.Add(p.Name + ":" + p.Map + ":" + p.Label);
        RWLog.Info("POOL " + string.Join(" ", names.ToArray()));
    }

    // The game's six modes (RogueLab round 1); Gameplans in other mods' modes are left alone.
    static bool IsStockMode(string mode)
    {
        return mode == "Mode_Normal" || mode == "Mode_Storm" || mode == "Mode_Drought" || mode == "Mode_Tsunami" || mode == "Mode_Final" || mode == "Mode_Tutorial";
    }

    public static string Twin(string stockPlan) { return RWUtil.Clip(RWData.PlanPrefix + stockPlan); }

    public static Gameplandatabaseobject TwinPlan(string stockPlan) { return RWUtil.Item<Gameplandatabaseobject>("Gameplan", Twin(stockPlan)); }
}
