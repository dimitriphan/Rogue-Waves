// Boot-time probes and registrations, run identically on every machine (online clients must resolve the same names).
public static class RogueLabBoot
{
    public static Modeobject ModeObject;
    public static string GrantArtifact = "";      // a stock Cardinal granted mid-match in map 1
    static readonly List<string> planNames = new List<string>();
    public static List<string> Plans { get { return planNames; } }   // stock Mode_Normal Gameplans with a twin, by name

    // docs/15 §11 step 1: what the game's database holds.
    public static void Database()
    {
        Tds_Masterdatabase db = Tds_Masterdatabase.Instance;
        List<string> folders = new List<string>(db.Items.Keys);
        folders.Sort();
        RogueLabProbe.Info("database folders", string.Join(", ", folders.ToArray()));
        foreach (string f in new[] { "Mode", "Gameplan", "Map", "Artifact", "Unlock", "Hero", "Soundtrack" })
        {
            Dictionary<string, UnityEngine.Object> items = RogueLabUtil.Folder(f);
            if (items == null) { RogueLabProbe.Info("folder " + f, "missing"); continue; }
            List<string> names = new List<string>(items.Keys);
            names.Sort();
            RogueLabProbe.Info("folder " + f + " (" + names.Count + ")", string.Join(", ", names.ToArray()));
        }
        foreach (UnityEngine.Object o in RogueLabUtil.Folder("Mode").Values)
        {
            Modeentryobject m = o as Modeentryobject;
            if (m != null)
                RogueLabProbe.Info("mode entry " + m.Objectname, "display '" + m.Display_Name + "' loads " + (m.Modeloads != null ? m.Modeloads.Objectname : "-") + " waves " + m.Waves);
        }
        foreach (UnityEngine.Object o in RogueLabUtil.Folder("Artifact").Values)
        {
            Artifactentryobject a = o as Artifactentryobject;
            if (a != null)
                RogueLabProbe.Info("cardinal " + a.Objectname, "'" + a.Display_Name + "' type " + a.Type + " mixed " + a.Mixed_Compatible + " loads " + (a.Artifactloads != null ? a.Artifactloads.Objectname : "-")
                    + " unlocked " + Blockage.Tags.Bla_Data.Playerdata.Readdata("Unlock", "Unlock_Artifact_" + a.Objectname).Loaded + " desc '" + a.Display_Desc + "'");
        }
        List<string> init = new List<string>();
        foreach (Bl_Address_Group g in Bla_Addressables.Instance.Database.Init) if (g != null) init.Add(g.name);
        RogueLabProbe.Info("groups loaded at boot (Database.Init)", string.Join(", ", init.ToArray()));
    }

    // docs/13 §10 Q3 for Mode_Normal, and one Gameplan's fields.
    public static void StockMode(Modeentryobject normal)
    {
        Modeobject m = normal != null && normal.Modeloads != null ? Bl_Address.Getload("Mode", normal.Modeloads.Objectname) as Modeobject : null;
        if (m == null) { RogueLabProbe.Setup("Mode_Normal data", "its Modeobject is not loaded"); return; }
        Modeobject byEntry = Bl_Address.Getload("Mode", RogueLabData.StockMode) as Modeobject;
        RogueLabProbe.Info("Mode_Normal object", "name " + m.Objectname + ", found by entry name too " + (byEntry == m) + ", script " + (m.Modescript != null ? m.Modescript.name : "-")
            + ", client script " + (m.Clientmodescript != null ? m.Clientmodescript.name : "-") + ", special spots " + m.Specialspots.Count);
        List<string> ps = new List<string>();
        foreach (Modeobject.Param p in m.Defaultparam) ps.Add(p.Name + "=" + RogueLabUtil.F(p.Value));
        RogueLabProbe.Info("Mode_Normal params", string.Join(" ", ps.ToArray()));
        Wavesobject w = m.Enemies;
        List<string> init = new List<string>();
        if (w.Resourcesinit != null) foreach (Link_Resourceset r in w.Resourcesinit) init.Add(r.Name + " " + r.Amount);
        RogueLabProbe.Info("Mode_Normal table", w.Wave.Count + " entries, Totalwaves " + w.Totalwaves + ", start aura " + string.Join(", ", init.ToArray()));
        for (int i = 0; i < w.Wave.Count; i++)
        {
            Wavesobject.Waves e = w.Wave[i];
            List<string> cs = new List<string>();
            if (e.Content != null)
                foreach (Wavesobject.Wavecontent c in e.Content)
                {
                    List<string> ups = new List<string>();
                    if (c.Upgrades != null) foreach (Upgradeproperties u in c.Upgrades) ups.Add(u != null ? u.Objectname : "null");
                    cs.Add(c.Style + " " + (c.Enemy != null ? c.Enemy.Objectname : "-") + " x" + c.Count + " every " + RogueLabUtil.F(c.Interval) + " gate " + c.Gate + " team " + c.Team + (ups.Count > 0 ? " [" + string.Join(",", ups.ToArray()) + "]" : ""));
                }
            RogueLabLog.Test("TIDE " + (i + 1) + " " + (e.Wave ? "tide" : "break") + " shown " + e.Displayedwave + " time " + RogueLabUtil.F(e.Wavetime) + " note '" + e.Note + "' | " + string.Join(" ; ", cs.ToArray()));
        }
        foreach (UnityEngine.Object o in RogueLabUtil.Folder("Gameplan").Values)
        {
            Gameplandatabaseobject g = o as Gameplandatabaseobject;
            if (g == null || g.Objectname != ModKitTest.TestPlan) continue;
            RogueLabProbe.Info("Gameplan " + g.Objectname, "map " + (g.Map != null ? g.Map.Objectname : "-") + ", mode " + (g.Mode != null ? g.Mode.Objectname : "-") + ", soundtrack "
                + (g.Soundtrack != null ? g.Soundtrack.Objectname : "-") + ", par " + RogueLabUtil.F(g.Par) + ", record " + g.Record);
        }
    }

    // docs/15 §2: a mode a mod adds. Its Modeobject is a copy of Mode_Normal's (so its script names are the stock ones)
    // playing the first three Tides; it is found by Game_Init under the entry's name and by Game_Rounds under the
    // group's name, so all three share one name. Every stock Gameplan in Mode_Normal gets a twin in this mode.
    public static void RegisterMode(Modeentryobject normal)
    {
        Modeobject stock = normal != null && normal.Modeloads != null ? Bl_Address.Getload("Mode", normal.Modeloads.Objectname) as Modeobject : null;
        if (stock == null) { RogueLabProbe.Setup("mod mode registered", "Mode_Normal's Modeobject is not loaded"); return; }

        Wavesobject waves = RogueLabUtil.Clone(stock.Enemies, RogueLabData.Mode + "_Waves");
        waves.Wave = new List<Wavesobject.Waves>();
        int tides = 0;
        foreach (Wavesobject.Waves e in stock.Enemies.Wave)
        {
            waves.Wave.Add(e);
            if (e.Wave && ++tides >= RogueLabData.Tides) break;
        }
        waves.Totalwaves = tides;
        ModeObject = RogueLabUtil.Clone(stock, RogueLabData.Mode);
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
        ModeObject.Loadsource = RogueLabData.Mode;
        Bl_Address.Groupsname[new FixedString64Bytes(ModeObject.Objectname)] = ModeObject;
        Bl_Address.Groupsids[id] = ModeObject;
        Bl_Address.Loadedfolders[RogueLabData.Mode] = group;

        Modeentryobject entry = RogueLabUtil.Clone(normal, RogueLabData.Mode);
        entry.Modeloads = RogueLabUtil.Clone(normal.Modeloads, RogueLabData.Mode);
        entry.Display_Name = "Rogue Wave (lab)";
        RogueLabUtil.Folder("Mode")[RogueLabData.Mode] = entry;

        Dictionary<string, UnityEngine.Object> plans = RogueLabUtil.Folder("Gameplan");
        List<UnityEngine.Object> stockPlans = new List<UnityEngine.Object>(plans.Values);
        foreach (UnityEngine.Object o in stockPlans)
        {
            Gameplandatabaseobject g = o as Gameplandatabaseobject;
            if (g == null || g.Mode != normal || g.Map == null) continue;
            Gameplandatabaseobject twin = RogueLabUtil.Clone(g, RogueLabUtil.Clip(RogueLabData.PlanPrefix + g.Objectname));
            twin.Mode = entry;
            plans[twin.Objectname] = twin;
            planNames.Add(g.Objectname);
        }
        planNames.Sort();

        bool found = Tds_Masterdatabase.Instance.Getitem("Mode", RogueLabData.Mode) == entry && Bl_Address.Getload("Mode", RogueLabData.Mode) == ModeObject;
        RogueLabProbe.Check("mod mode registered", found, "table " + waves.Wave.Count + " entries / " + tides + " Tides, params " + ModeObject.Defaultparams.Count
            + " (Startwait " + ModeObject.Defaultparams.ContainsKey("Startwait") + ", Basinthreshold " + ModeObject.Defaultparams.ContainsKey("Basinthreshold") + ", Tipthreshold "
            + ModeObject.Defaultparams.ContainsKey("Tipthreshold") + "), twins of " + planNames.Count + " Gameplans");
    }

    public static string Twin(string stockPlan) { return RogueLabUtil.Clip(RogueLabData.PlanPrefix + stockPlan); }

    // A second map for the chain: the first Mode_Normal plan (by name) on another map than 'not'.
    public static string OtherPlan(string not)
    {
        Dictionary<string, UnityEngine.Object> plans = RogueLabUtil.Folder("Gameplan");
        Gameplandatabaseobject first = plans.ContainsKey(not) ? plans[not] as Gameplandatabaseobject : null;
        foreach (string n in planNames)
        {
            Gameplandatabaseobject g = plans[n] as Gameplandatabaseobject;
            if (n != not && g != null && (first == null || g.Map != first.Map) && !n.Contains("Tutorial") && !n.Contains("Test"))
                return n;
        }
        return "";
    }

    // docs/15 §7: a Cardinal a mod adds, as data: an entry (a copy of a stock one, using its artifact) in the database
    // and its unlock key, so the lobby offers it. A second stock Cardinal is kept for the mid-match grant.
    public static void RegisterCardinal()
    {
        Dictionary<string, UnityEngine.Object> arts = RogueLabUtil.Folder("Artifact");
        List<string> names = new List<string>(arts.Keys);
        names.Sort();
        Artifactentryobject source = null;
        foreach (string n in names)
        {
            Artifactentryobject a = arts[n] as Artifactentryobject;
            if (a == null || a.Artifactloads == null || n.Contains("Sandbox")) continue;
            if (source == null) source = a;
            else if (GrantArtifact == "" && source != null) GrantArtifact = n;
        }
        if (source == null) { RogueLabProbe.Setup("mod Cardinal registered", "no stock Cardinal to copy"); return; }
        Artifactentryobject mine = RogueLabUtil.Clone(source, RogueLabData.Cardinal);
        mine.Display_Name = "RogueLab Cardinal";
        mine.Display_Desc = "A copy of " + source.Objectname + " registered by a mod";
        arts[RogueLabData.Cardinal] = mine;
        Blockage.Tags.Bla_Data.Playerdata.Adddata("Unlock", "Unlock_Artifact_" + RogueLabData.Cardinal, 1f, Blockage.Tags.Bl_Tags.Addtype.Replace);
        RogueLabProbe.Check("mod Cardinal registered", Tds_Masterdatabase.Instance.Getitem("Artifact", RogueLabData.Cardinal) == mine, "copy of " + source.Objectname + " (" + source.Type + "), grant test uses " + GrantArtifact);
    }

    // docs/15 §8: a folder of the mod's own in the live save. The launch counter shows on the next launch whether the
    // value survived a restart.
    public static void Save()
    {
        Blockage.Tags.Bl_Tags.Entry before = Blockage.Tags.Bla_Data.Playerdata.Readdata(RogueLabData.SaveFolder, "RW_Launches");
        Blockage.Tags.Bla_Data.Playerdata.Adddata(RogueLabData.SaveFolder, "RW_Launches", 1f, Blockage.Tags.Bl_Tags.Addtype.Add);
        Blockage.Tags.Bl_Tags.Entry after = Blockage.Tags.Bla_Data.Playerdata.Readdata(RogueLabData.SaveFolder, "RW_Launches");
        RogueLabProbe.Check("save write and read back", after.Loaded && after.Value == (before.Loaded ? before.Value : 0f) + 1f, "before " + (before.Loaded ? RogueLabUtil.F(before.Value) : "none") + ", after " + RogueLabUtil.F(after.Value));
        if (before.Loaded)
            RogueLabProbe.Check("save value survived a restart", before.Value >= 1f, "launch " + RogueLabUtil.F(after.Value));
        else
            RogueLabProbe.Info("save value survived a restart", "first launch with RogueLab: the next launch answers this");
        Blockage.Tags.Bla_Data.Instance.Savedata(false, "");
    }

    // The test Cardinal's unlock key leaves the save at the end of the run.
    public static void Cleanup()
    {
        Blockage.Tags.Bla_Data.Playerdata.Removedata("Unlock", "Unlock_Artifact_" + RogueLabData.Cardinal);
        Blockage.Tags.Bla_Data.Instance.Savedata(false, "");
    }
}
