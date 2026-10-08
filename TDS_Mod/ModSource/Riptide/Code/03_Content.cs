// Registers the Riptide mode the way the game registers its own (kit 04_Content pattern, as for a vessel's hero entry):
//   Tds_Masterdatabase "Mode"     Mode_Riptide  -> Modeentryobject (name shown in menus, which group to load)
//   Bl_Address "Mode"             Mode_Riptide  -> Modeobject (Mode_Normal's scripts and params) -> Wavesobject
//   Tds_Masterdatabase "Gameplan" Riptide_<id>  -> a twin of every Mode_Normal Gameplan, pointing at Riptide
// Game_Init/Game_Client_Init look the entry up by the synced Tds_Multiplayerinfo.Mode name, Waitload its group (ours is
// already in Loadedfolders) and Getload("Mode", name); Game_Rounds then runs our Modeobject on the stock Mode_Normal.
// The twins keep a Riptide run's end-screen records (best Tide, clears, times, medals) off the stock map's save keys.
// Everything is built the same way on every machine: no randomness, sorted iteration.
public static class RiptideContent
{
    public static Modeentryobject Entry, TemplateEntry;
    public static Modeobject Mode, Template;
    public static Wavesobject Table;
    // The table as registered: the director starts every run from it.
    public static readonly List<Wavesobject.Waves> Placeholder = new List<Wavesobject.Waves>();
    public static readonly Dictionary<string, string> Twins = new Dictionary<string, string>();   // stock -> twin
    public static readonly List<string> Artifacts = new List<string>();                         // entry ids, sorted
    public static int ArtifactsLoaded;
    public static float ArtifactLoadSeconds;

    static Bl_Address.Loadgroup group;
    static readonly Dictionary<string, Bl_Address.Loadlist> lists = new Dictionary<string, Bl_Address.Loadlist>();

    public static void Build(Modeentryobject templateEntry, Modeobject template)
    {
        TemplateEntry = templateEntry;
        Template = template;
        group = new Bl_Address.Loadgroup();
        group.Items = new List<Bl_Address.Loadlist>();
        group.Loaded = true;
        group.Percent = 100;
        Bl_Address.Loadlist list = List("Mode");

        Table = UnityEngine.Object.Instantiate(template.Enemies);
        RiptideUtil.Retag(Table, RiptideData.Tides);
        Table.Resourcesinit = new List<Link_Resourceset>(template.Enemies.Resourcesinit ?? new List<Link_Resourceset>());
        Table.Wave = BuildPlaceholder(template.Enemies.Wave);
        // Artifacts scale by 30 / Totalwaves in whole numbers (Coin, Caffeine, Opalcore, Moneybag, Voice): 30 gives 1.
        Table.Totalwaves = RiptideData.RunTides;
        Placeholder.Clear();
        foreach (Wavesobject.Waves w in Table.Wave)
            Placeholder.Add(w);

        Mode = UnityEngine.Object.Instantiate(template);
        RiptideUtil.Retag(Mode, RiptideData.Mode);
        Mode.Enemies = Table;
        if (template.Defaultparams != null)
            Mode.Defaultparams = new Dictionary<string, float>(template.Defaultparams);
        Mode.Specialspots = new List<Modeobject.Entry>(template.Specialspots ?? new List<Modeobject.Entry>());
        Register(list, Mode);

        Entry = UnityEngine.Object.Instantiate(templateEntry);
        RiptideUtil.Retag(Entry, RiptideData.Mode);
        Bl_Address_Group loads = UnityEngine.Object.Instantiate(templateEntry.Modeloads);
        RiptideUtil.Retag(loads, RiptideData.Mode);
        Entry.Modeloads = loads;
        Entry.Display_Name = RiptideData.Displayname;
        Entry.Waves = RiptideData.RunTides;
        RiptideUtil.Folder("Mode")[RiptideData.Mode] = Entry;
        Bl_Address.Loadedfolders[RiptideData.Mode] = group;

        BuildTwins();
        RiptideCommands.Build();
    }

    public static string Summary()
    {
        return Table.Wave.Count + " table entries (" + RiptideData.RunTides + " Tides), " + Twins.Count + " Gameplan twins, "
            + Artifacts.Count + " artifacts known";
    }

    // Riptide's own load list for a label ("Mode", "Command"), in its group and in the game's lists for that label.
    static Bl_Address.Loadlist List(string label)
    {
        Bl_Address.Loadlist list;
        if (lists.TryGetValue(label, out list))
            return list;
        list = new Bl_Address.Loadlist();
        list.Loaded = true;
        list.Percent = 100;
        list.Fromgroup = group;
        group.Items.Add(list);
        List<Bl_Address.Loadlist> labelLists;
        if (!Bl_Address.Groups.TryGetValue(label, out labelLists))
        {
            labelLists = new List<Bl_Address.Loadlist>();
            Bl_Address.Groups[label] = labelLists;
        }
        labelLists.Add(list);
        lists[label] = list;
        return list;
    }

    public static void Register(string label, Bl_ScriptableObject obj) { Register(List(label), obj); }

    static void Register(Bl_Address.Loadlist list, Bl_ScriptableObject obj)
    {
        list.Items[obj.Objectname] = obj;
        if (!list.Index.Contains(obj))
            list.Index.Add(obj);
        FixedString64Bytes id = new FixedString64Bytes(obj.Id);
        list.Uids[id] = obj;
        obj.Loadsource = RiptideData.Mode;
        Bl_Address.Groupsname[new FixedString64Bytes(obj.Objectname)] = obj;
        Bl_Address.Groupsids[id] = obj;
    }

    // A break then a Tide, RunTides times. Breaks copy the stock breaks in order (the first is the long opening build
    // time), Tides copy the stock Tides in order; past the stock's end the last one repeats. Notes are cleared: the
    // director writes its own. Content lists are copied, so the director can rewrite an entry without touching
    // Mode_Normal's own table.
    static List<Wavesobject.Waves> BuildPlaceholder(List<Wavesobject.Waves> stock)
    {
        List<Wavesobject.Waves> breaks = new List<Wavesobject.Waves>(), tides = new List<Wavesobject.Waves>();
        if (stock != null)
            foreach (Wavesobject.Waves w in stock)
                (w.Wave ? tides : breaks).Add(w);
        List<Wavesobject.Waves> table = new List<Wavesobject.Waves>();
        for (int t = 1; t <= RiptideData.RunTides; t++)
        {
            Wavesobject.Waves b = breaks.Count > 0 ? Copy(breaks[Mathf.Min(t - 1, breaks.Count - 1)]) : Empty(RiptideData.FallbackBreak);
            b.Wave = false;
            b.Note = "";
            table.Add(b);
            Wavesobject.Waves w = tides.Count > 0 ? Copy(tides[Mathf.Min(t - 1, tides.Count - 1)]) : Empty(RiptideData.FallbackTideTime);
            w.Wave = true;
            w.Note = "";
            w.Displayedwave = t;
            table.Add(w);
        }
        return table;
    }

    public static Wavesobject.Waves Copy(Wavesobject.Waves w)
    {
        Wavesobject.Waves c = w;
        c.Content = new List<Wavesobject.Wavecontent>();
        if (w.Content != null)
            foreach (Wavesobject.Wavecontent x in w.Content)
            {
                Wavesobject.Wavecontent y = x;
                y.Upgrades = new List<Upgradeproperties>(x.Upgrades ?? new List<Upgradeproperties>());
                y.Resources = new List<Link_Resourceset>(x.Resources ?? new List<Link_Resourceset>());
                c.Content.Add(y);
            }
        return c;
    }

    static Wavesobject.Waves Empty(float seconds)
    {
        Wavesobject.Waves w = new Wavesobject.Waves();
        w.Note = "";
        w.Wavetime = seconds;
        w.Content = new List<Wavesobject.Wavecontent>();
        return w;
    }

    // A Riptide twin of every Gameplan that plays Mode_Normal: the same map, soundtrack and intro, the Riptide mode, no
    // medals, not recorded for the leaderboard. The switch swaps the synced Gameplan name to its twin, so the end screen
    // saves the run as "Clear_Riptide_<id>", "Wave_Riptide_<id>"... (its own best Tide per map) instead of the stock map's.
    static void BuildTwins()
    {
        Dictionary<string, UnityEngine.Object> plans = RiptideUtil.Folder("Gameplan");
        if (plans == null)
        {
            RiptideLog.Warn("no Gameplan folder: the switch will change the mode only");
            return;
        }
        List<string> ids = new List<string>(plans.Keys);
        ids.Sort(string.CompareOrdinal);
        foreach (string id in ids)
        {
            Gameplandatabaseobject stock = plans[id] as Gameplandatabaseobject;
            if (stock == null || stock.Mode == null || System.Array.IndexOf(RiptideData.SwappableModes, stock.Mode.Objectname) < 0)
                continue;
            if (id.StartsWith(RiptideData.GameplanPrefix))
                continue;
            string twinId = RiptideUtil.Clip(RiptideData.GameplanPrefix + id);
            Gameplandatabaseobject twin = UnityEngine.Object.Instantiate(stock);
            RiptideUtil.Retag(twin, twinId);
            twin.Mode = Entry;
            twin.Record = false;
            twin.Medals = new List<Gameplandatabaseobject.Medalunlock>();
            plans[twinId] = twin;
            Twins[id] = twinId;
        }
    }

    public static string StockOf(string gameplan)
    {
        foreach (KeyValuePair<string, string> kv in Twins)
            if (kv.Value == gameplan)
                return kv.Key;
        return null;
    }

    // Every artifact group, loaded on every machine at boot (asset groups are never unloaded), so a Salvage pick can be
    // shown at once on every screen. Sorted for the same order everywhere.
    public static IEnumerator LoadArtifacts()
    {
        Dictionary<string, UnityEngine.Object> entries = RiptideUtil.Folder("Artifact");
        if (entries == null)
        {
            RiptideLog.Warn("no Artifact folder: Salvage has nothing to offer");
            yield break;
        }
        float start = Time.realtimeSinceStartup;
        List<string> ids = new List<string>(entries.Keys);
        ids.Sort(string.CompareOrdinal);
        foreach (string id in ids)
        {
            Artifactentryobject entry = entries[id] as Artifactentryobject;
            if (entry == null || entry.Artifactloads == null)
                continue;
            Bl_Address.Loadgroup g = null;
            try { g = Bl_Address.Load(entry.Artifactloads.Objectname); }
            catch (Exception e) { RiptideLog.Warn("loading " + id + ": " + e.Message); }
            float waited = 0f;
            while (g != null && !g.Loaded && waited < 60f)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }
            if (g == null || !g.Loaded)
            {
                RiptideLog.Warn("could not load " + id + "; it is never offered");
                continue;
            }
            Artifacts.Add(id);
            ArtifactsLoaded++;
        }
        ArtifactLoadSeconds = Time.realtimeSinceStartup - start;
        RiptideLog.Info("artifacts loaded: " + ArtifactsLoaded + "/" + ids.Count + " in " + RiptideUtil.F(ArtifactLoadSeconds) + " s");
    }
}
