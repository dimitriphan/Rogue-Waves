#if ROGUEWAVESMODE_SELFTEST
// The PC round for the sea chart and sea weather (docs/SEA_CHART.md §5, probes C1-C6 and W1-W3), solo, one launch,
// logged as "[RogueWavesMode] TEST PASS|FAIL|SETUP|INFO ..." and "TEST DONE" for Run-SelfTests.ps1:
//   menu   presses Play like the player would (the game ignores injected input)
//   boat   starts campaign Seed with three maps' weather forced (Rising Water, then Quickening Current, then Nesting
//          Ground) on a route the test picks, then C1 the warp onto the chart, C2 the arrows, C3 the boat sails to the
//          chosen node by itself, C4 the node is picked and its map loads in Mode_RogueWave
//   map    W1/W2/W3 the weather's object or speed reacts at the next Tide (forced), then the map is won and the party
//          returns: C5 back on the chart with the route one longer, C6 the save holds no chart name
//   end    the campaign save round-trips through Continue, then the campaign is stopped and its keys removed
public static class RWTest
{
    public const uint Seed = 20261009;
    const int Maps = 3;
    static readonly int[] Forced = { RWChart.RisingWater, RWChart.QuickeningCurrent, RWChart.NestingGround };
    public static int Pass, Fail, Setups;
    static int errors;
    static bool freeze, forcing, done;
    static readonly List<int> route = new List<int>();

    public static void Boot()
    {
        Application.logMessageReceived += OnLog;
        ModKitTest.Log = Line;
        RWWeather.QuickTide = 1;
        RWWeather.QuickerTide = 2;
        Info("self-test", "v" + RWData.Version + ", seed " + Seed + ", " + Maps + " maps, Quickening Current from Tide 1 and 2");
        RogueWavesModeMod.Instance.StartCoroutine(Guarded());
    }

    static void Line(string message) { RWLog.Info("TEST " + message); }
    static void Info(string name, string detail) { Line("INFO " + name + ": " + detail); }
    static void Setup(string name, string why) { Setups++; Line("SETUP " + name + ": " + why); }

    static bool Check(string name, bool ok, string detail)
    {
        if (ok) Pass++; else Fail++;
        Line((ok ? "PASS " : "FAIL ") + name + (detail != "" ? ": " + detail : ""));
        return ok;
    }

    static void OnLog(string condition, string stack, LogType type)
    {
        if (type != LogType.Exception && type != LogType.Error)
            return;
        errors++;
        if (errors <= 20)
            Info("error seen", (condition ?? "").Split('\n')[0]);
    }

    // Holds the Tide timer during probes (and runs it out while forcing), as RogueLab does.
    public static void Update()
    {
        Game_Mapvars vars = Game_Mapvars.Instance;
        if (vars == null || !RWUtil.IsServer || done)
            return;
        if (forcing)
            vars.Time_Wave.Value = -1f;
        else if (freeze && vars.Time_Wave.Value < 600f && !InTide())
            vars.Time_Wave.Value = 900f;
    }

    static bool InTide()
    {
        Game_Mapvars vars = Game_Mapvars.Instance;
        int e = vars.Entry.Value;
        List<Wavesobject.Waves> table = vars.Mode != null && vars.Mode.Enemies != null ? vars.Mode.Enemies.Wave : null;
        return table != null && e >= 1 && e <= table.Count && table[e - 1].Wave;
    }

    static IEnumerator Until(Func<bool> ok, float timeout)
    {
        float t = 0f;
        while (!ok() && t < timeout) { t += Time.unscaledDeltaTime; yield return null; }
    }

    static IEnumerator Guarded()
    {
        IEnumerator run = Run();
        while (true)
        {
            object step;
            try
            {
                if (!run.MoveNext()) break;
                step = run.Current;
            }
            catch (Exception e)
            {
                Check("self-test ran to the end", false, e.ToString());
                break;
            }
            yield return step;
        }
        Finish();
    }

    static void Finish()
    {
        if (done) return;
        done = true;
        freeze = forcing = false;
        try { if (RWCampaign.Active || RWCampaign.Seed == Seed) RWCampaign.Stop(); } catch (Exception e) { Info("cleanup", e.Message); }
        Line("DONE pass " + Pass + ", fail " + Fail + ", setup " + Setups + ", errors logged " + errors);
    }

    // ---------------------------------------------------------------- the run

    static IEnumerator Run()
    {
        yield return Embark();
        if (Macro_Main.Instance == null || !Macro_Main.Instance.Ready) { Setup("boat", "never reached the boat"); yield break; }
        yield return new WaitForSecondsRealtime(4f);
        if (!RWUtil.IsHost || !RWUtil.IsLeader) { Setup("boat", "not the host and leader"); yield break; }
        if (RWMode.Pool.Count == 0) { Check("C0 chart pool", false, "no base maps found"); yield break; }
        if (!Plan()) yield break;

        int before = errors;
        RWCampaign.New(Seed);
        yield return Until(() => Here(), 40f);
        Macro_Worldvisual vis = Macro_Worldvisual.Instance;
        Mapworlddatabaseobject item = RWUtil.Item<Mapworlddatabaseobject>("World", RWCampaign.WorldName());
        Check("C1 warped onto a code-built chart world", Here(), "world " + (vis.World != null ? vis.World.name : "-") + ", nodes " + (vis.World != null ? vis.World.GetComponentsInChildren<Macro_Link_Add_Base>(true).Length : 0)
            + ", title '" + (item != null ? item.Display_Name + " / " + item.Display_Desc : "-") + "', " + RWBoat.SceneNote);
        Check("C1 no error during the warp", errors == before, (errors - before) + " error lines");

        for (int m = 0; m < Maps; m++)
        {
            if (!Here()) { Setup("map " + (m + 1), "not on the chart"); yield break; }
            yield return new WaitForSecondsRealtime(3f);
            Arrows(m);
            yield return Sail(m);
            if (RWBoat.ArrivedAt < 0f) yield break;
            yield return Pick(m);
            if (Game_Mapvars.Instance == null || !Game_Mapvars.Instance.Maploaded.Value) yield break;
            yield return Weather(m);
            yield return Win();
            yield return Return(m);
        }

        uint seed;
        List<int> saved;
        bool has = RWCampaign.Saved(out seed, out saved);
        Check("campaign save holds seed and route", has && seed == Seed && Same(saved, RWCampaign.Route), "saved " + (has ? seed + " " + Digits(saved) : "nothing") + ", live " + RWCampaign.Seed + " " + Digits(RWCampaign.Route));
        RWCampaign.Active = false;
        RWCampaign.Route.Clear();
        bool back = RWCampaign.Continue();
        Check("Continue reads the campaign back", back && RWCampaign.Seed == Seed && Same(RWCampaign.Route, route), "route " + Digits(RWCampaign.Route) + ", expected " + Digits(route));
        RWCampaign.Stop();
        Check("Stop removes the campaign from the save", !RWCampaign.Saved(out seed, out saved), "");
    }

    static bool Here() { return RWBoat.OnChart && Macro_Worldvisual.Instance.World.name == RWCampaign.WorldName(); }

    // The main menu's Play, as RogueLab does it.
    static IEnumerator Embark()
    {
        yield return Until(() => UnityEngine.Object.FindObjectOfType<Mainmenu_Main>() != null || Macro_Main.Instance != null, 120f);
        for (int tries = 0; tries < 2 && Macro_Main.Instance == null; tries++)
        {
            Mainmenu_Main menu = UnityEngine.Object.FindObjectOfType<Mainmenu_Main>();
            if (menu == null) break;
            yield return new WaitForSecondsRealtime(8f);
            Line("main menu: embarking");
            menu.SendMessage(tries == 0 ? "Play" : "Transitiontomain", SendMessageOptions.DontRequireReceiver);
            yield return Until(() => Macro_Main.Instance != null, 25f);
        }
        yield return Until(() => Macro_Main.Instance != null && Macro_Main.Instance.Ready, 90f);
    }

    // A route whose maps can take the three forced weathers, then the weathers forced on its nodes (before the chart's
    // world is built, so the islands show them).
    static bool Plan()
    {
        RWChart.Chart chart = RWWorld.Chart(Seed);
        route.Clear();
        RWChart.Node at = chart.At(0, 0);
        for (int m = 0; m < Maps; m++)
        {
            RWChart.Node pick = null;
            foreach (int c in at.Links)
            {
                RWChart.Node n = chart.At(m + 1, c);
                if (n != null && !RWChart.Native(Forced[m], n.Map)) { pick = n; break; }
            }
            if (pick == null) { Setup("route", "no node in row " + (m + 1) + " can take " + RWWeather.Display(Forced[m])); return false; }
            pick.Weather = Forced[m];
            route.Add(pick.Col);
            at = pick;
        }
        List<string> names = new List<string>();
        for (int m = 0; m < Maps; m++) { RWChart.Node n = chart.At(m + 1, route[m]); names.Add(n.Name + " " + n.Plan + " " + RWWeather.Display(n.Weather)); }
        Info("route", string.Join("; ", names.ToArray()));
        return true;
    }

    // C2: the arrows the game drew from where the boat stands are the chart's links.
    static void Arrows(int m)
    {
        List<string> want = new List<string>(), got = new List<string>();
        foreach (RWChart.Node n in RWWorld.Choices(RWCampaign.Chart, RWCampaign.Route)) want.Add(n.Name);
        GameObject spot = Macro_Worldui.Instance != null ? Macro_Worldui.Instance.Tospot : null;
        if (spot == null) { Setup("C2 arrows from " + RWCampaign.Here.Name, "no hover arrows shown"); return; }
        foreach (Macro_Nodeselect_Click a in spot.GetComponentsInChildren<Macro_Nodeselect_Click>(true))
            if (a.Tonode != null) got.Add(a.Tonode.name);
        want.Sort(string.CompareOrdinal);
        got.Sort(string.CompareOrdinal);
        Check("C2 map " + (m + 1) + ": one arrow per link from " + RWCampaign.Here.Name, string.Join(",", want.ToArray()) == string.Join(",", got.ToArray()),
            "arrows " + string.Join(", ", got.ToArray()) + "; links " + string.Join(", ", want.ToArray()) + "; hovering " + (Macro_Main.Instance.Hovering != null ? Macro_Main.Instance.Hovering.name : "-"));
    }

    // C3: the boat sails to the chosen node by itself and stops at its dock.
    static IEnumerator Sail(int m)
    {
        RWChart.Node to = RWCampaign.Chart.At(m + 1, route[m]);
        RWCampaign.Choice = route[m];
        yield return Until(() => RWBoat.ArrivedAt > 0f, 60f);
        Macro_Worldvisual vis = Macro_Worldvisual.Instance;
        Check("C3 map " + (m + 1) + ": the boat sails to " + to.Name + " by itself", RWBoat.ArrivedAt > 0f,
            RWBoat.ArrivedAt > 0f ? RWUtil.F(RWBoat.ArrivedAt - RWBoat.SailedAt) + " s, " + RWUtil.F(RWUtil.Flat(vis.Boat.transform.position, RWWorld.At(to))) + " from the island" : "not there after 60 s, boat at " + RWUtil.V(vis.Boat.transform.position));
    }

    // C4: picking the node opens its Setup panel in the mode, and its map loads in Mode_RogueWave.
    static IEnumerator Pick(int m)
    {
        RWChart.Node to = RWCampaign.Chart.At(m + 1, route[m]);
        Macro_Main main = Macro_Main.Instance;
        yield return Until(() => main.Selected.Value.ToString() == to.Name && Macro_Worldvisual.Instance.Insetup, 15f);
        string plan = Tds_Multiplayerinfo.Instance.Gameplan.Value.ToString(), mode = Tds_Multiplayerinfo.Instance.Mode.Value.ToString();
        Check("C4 map " + (m + 1) + ": " + to.Name + " picked, Setup panel open in the mode", main.Selected.Value.ToString() == to.Name && plan == RWMode.Twin(to.Plan) && mode == RWData.Mode,
            "selected " + main.Selected.Value + ", setup " + Macro_Worldvisual.Instance.Insetup + ", plan " + plan + ", mode " + mode + ", campaign's next " + (RWCampaign.Next != null ? RWCampaign.Next.Name : "-"));
        // What the leader's own click on the node would write (Macro_Main.cs:633-634), for the save guard (C6).
        RWUtil.WriteString("Player", "Player_Worldselect", Tds_Multiplayerinfo.Instance.World.Value.ToString());
        RWUtil.WriteString("Player", "Player_Gameplanselect", to.Name);
        yield return Until(() => RWWeather.Loaded(to.Weather), 90f);
        Check("W0 map " + (m + 1) + ": " + RWWeather.Display(to.Weather) + "'s objects loaded from the boat", RWWeather.Loaded(to.Weather), "");
        yield return new WaitForSecondsRealtime(1.5f);
        string note = "";
        try { Transform s = Blockage.Menu.Bl_Menus.Menus["Setup"].Items["Stats"].transform.Find("State"); note = s != null && s.GetComponent<TMPro.TMP_Text>() != null ? s.GetComponent<TMPro.TMP_Text>().text : "(no State text)"; }
        catch (Exception e) { note = "(" + e.Message + ")"; }
        Info("map " + (m + 1) + " Setup panel note", "'" + note + "'");
        Game_Mapvars before = Game_Mapvars.Instance;
        main.Playgame(true);
        yield return Until(() => Game_Mapvars.Instance != null && Game_Mapvars.Instance != before && Game_Mapvars.Instance.Maploaded.Value && Game_Rounds.Instance != null && Game_Mapvars.Instance.Wave.Value >= 0, 240f);
        Game_Mapvars vars = Game_Mapvars.Instance;
        bool loaded = vars != null && vars != before && vars.Maploaded.Value;
        Check("C4 map " + (m + 1) + ": " + to.Plan + " loads in " + RWData.Mode, loaded && vars.Mode != null && vars.Mode.Objectname == RWData.Mode,
            loaded ? "map " + (vars.Map != null ? vars.Map.Objectname : "-") + ", mode " + (vars.Mode != null ? vars.Mode.Objectname : "-") + ", Gameplan " + Tds_Multiplayerinfo.Instance.Gameplan.Value : "no map after 240 s");
        freeze = true;
    }

    // W1-W3: the weather is put on the map and reacts at the next Tide.
    static IEnumerator Weather(int m)
    {
        RWChart.Node node = RWCampaign.Chart.At(m + 1, route[m]);
        Game_Mapvars vars = Game_Mapvars.Instance;
        yield return Until(() => RWWeather.Current == node.Weather && (node.Weather == RWChart.QuickeningCurrent || RWWeather.Flood != null || RWWeather.Nest != null), 75f);
        yield return new WaitForSeconds(1f);
        int e0 = errors;
        switch (node.Weather)
        {
            case RWChart.RisingWater:
            {
                Entity_Master flood = RWWeather.Flood;
                if (!Check("W1 Rising Water spawned on " + node.Plan, flood != null, "")) break;
                float y0 = flood.Entityposition.Value.y;
                int d0 = RWUtil.Count("Block_Flood_Deter");
                yield return Tide(vars, 1);
                yield return new WaitForSeconds(6.5f);
                float y1 = flood != null ? flood.Entityposition.Value.y : y0;
                int d1 = RWUtil.Count("Block_Flood_Deter");
                Check("W1 the water climbs at the next Tide", y1 > y0 + 0.05f, "y " + RWUtil.F(y0) + " -> " + RWUtil.F(y1) + ", Wave " + vars.Wave.Value + (flood != null ? RWWeather.Stats(flood, "Maxheight", "Heightspeed", "Projslow") : ""));
                Check("W1 the risen water floods the low tiles", d1 > d0, "flooded tiles " + d0 + " -> " + d1);
                break;
            }
            case RWChart.QuickeningCurrent:
            {
                float s0 = vars.Time_Scale.Value;
                yield return Tide(vars, 1);
                yield return new WaitForSeconds(1f);
                float s1 = vars.Time_Scale.Value;
                yield return Tide(vars, 2);
                yield return new WaitForSeconds(1f);
                float s2 = vars.Time_Scale.Value;
                Check("W2 Quickening Current speeds the game up at Tide 1, then Tide 2", Mathf.Approximately(s1, RWWeather.Quick) && Mathf.Approximately(s2, RWWeather.Quicker),
                    "Time_Scale " + RWUtil.F(s0) + " -> " + RWUtil.F(s1) + " -> " + RWUtil.F(s2) + ", Time.timeScale " + RWUtil.F(Time.timeScale));
                break;
            }
            case RWChart.NestingGround:
            {
                Entity_Master nest = RWWeather.Nest;
                if (!Check("W3 Nesting Ground spawned on " + node.Plan, nest != null, "")) break;
                int h0 = RWUtil.Count("Enemy_Halberd");
                yield return Tide(vars, 1);
                yield return new WaitForSeconds(3f);
                Check("W3 the nest sends a Halberd at the next Tide", RWUtil.Count("Enemy_Halberd") > h0, "Halberds " + h0 + " -> " + RWUtil.Count("Enemy_Halberd") + ", nest alive " + (nest != null && nest.Beingserver != null && !nest.Beingserver.Dead));
                break;
            }
        }
        Check("W map " + (m + 1) + ": no error while " + RWWeather.Display(node.Weather) + " ran", errors == e0, (errors - e0) + " error lines");
    }

    // Runs the timer out until the game reaches Wave 'wave', then holds it.
    static IEnumerator Tide(Game_Mapvars vars, int wave)
    {
        forcing = true;
        yield return Until(() => vars.Wave.Value >= wave, 60f);
        forcing = false;
        if (vars.Wave.Value < wave) Setup("Tide " + wave, "the timer ran out but Wave is " + vars.Wave.Value);
    }

    // Leaves the map as a win (RogueLab's proven route): the route grows by the node.
    static IEnumerator Win()
    {
        yield return new WaitForSeconds(1f);
        ClearFoes();
        int before = RWCampaign.Route.Count;
        Mode_Normal mode = Game_Rounds.Instance.GetComponent<Mode_Normal>();
        if (mode != null) mode.Finished = true;
        Tds_Multiplayerinfo.Instance.Gamestate.Value = new FixedString64Bytes("Win");
        Game_Mapvars.Instance.Gamecomplete.Value = new FixedString64Bytes("Win");
        Game_Rounds.Instance.Gamecompleted();
        freeze = false;
        yield return new WaitForSeconds(3f);
        Check("C5 a won map is sailed", RWCampaign.Route.Count == before + 1, "route " + Digits(RWCampaign.Route) + ", Time_Scale " + RWUtil.F(Game_Mapvars.Instance != null ? Game_Mapvars.Instance.Time_Scale.Value : -1f));
    }

    static void ClearFoes()
    {
        List<Entity_Master> foes;
        if (Game_Entity.Instance == null || !Game_Entity.Instance.Entityclienttags.TryGetValue("Enemy", out foes)) return;
        foreach (Entity_Master e in new List<Entity_Master>(foes))
            if (e != null && e.Beingserver != null && !e.Beingserver.Dead && e.Entitydata.Value.Team == 0 && !e.Taglistserver.Contains("Enemycore")
                && e != RWWeather.Nest && e != RWWeather.Flood)
                Game_Entity.Instance.Despawn(e.Beingserver, false, "RogueWaves");
    }

    // C5/C6: the end screen's return; back on the boat, warped onto the chart one row on, the save clean.
    static IEnumerator Return(int m)
    {
        Macro_Main old = Macro_Main.Instance;
        Tds_Multiplayer_Remote.Instance.Returnmap_ServerRpc(true, default(Unity.Netcode.ServerRpcParams));
        yield return Until(() => Macro_Main.Instance != null && Macro_Main.Instance != old && Macro_Main.Instance.Ready, 150f);
        yield return Until(() => Here(), 40f);
        yield return new WaitForSecondsRealtime(6f);
        Macro_Worldvisual vis = Macro_Worldvisual.Instance;
        RWChart.Node here = RWCampaign.Here;
        Transform dock = null;
        if (vis.World != null && here != null)
            foreach (Macro_Link_Add_Base l in vis.World.GetComponentsInChildren<Macro_Link_Add_Base>(true)) if (l.name == here.Name) dock = l.transform.Find("Dock");
        float off = dock != null ? RWUtil.Flat(vis.Boat.transform.position, dock.position) : -1f;
        Check("C5 map " + (m + 1) + ": back on the chart one row on, the boat at " + (here != null ? here.Name : "-"), Here() && RWCampaign.Route.Count == m + 1 && dock != null && off < 3f,
            "world " + (vis.World != null ? vis.World.name : "-") + ", boat " + RWUtil.F(off) + " from the dock");
        string w = RWUtil.ReadString("Player", "Player_Worldselect"), g = RWUtil.ReadString("Player", "Player_Gameplanselect");
        Check("C6 map " + (m + 1) + ": the save holds no chart name", !w.StartsWith(RWData.WorldPrefix) && !g.StartsWith("RW_"), "Player_Worldselect " + w + ", Player_Gameplanselect " + g);
    }

    static bool Same(List<int> a, List<int> b) { return Digits(a) == Digits(b); }

    static string Digits(List<int> r)
    {
        string s = "";
        foreach (int c in r) s += c.ToString();
        return s == "" ? "(none)" : s;
    }
}
#endif
