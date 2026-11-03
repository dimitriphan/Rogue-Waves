#if ROGUEWAVESMODE_SELFTEST
// The PC round for the sea chart and sea weather (docs/mode/SEA_CHART.md §9, probes C1-C6 and W0-W3) and the rejoin's solo
// probes (docs/mode/REJOIN.md §6, R1-R6, R2b), one launch,
// logged as "[RogueWavesMode] TEST PASS|FAIL|SETUP|INFO ..." and "TEST DONE" for Run-SelfTests.ps1:
//   menu   presses Play like the player would (the game ignores injected input)
//   boat   starts campaign Seed with three maps' weather forced (Rising Water, then Quickening Current, then Nesting
//          Ground) on a route the test picks, then C1 the warp onto the chart, C2 the arrows, C3 the boat sails to the
//          chosen node by itself, C4 the node is picked and its map loads in Mode_RogueWave
//   map    W1/W2/W3 the weather's object or speed reacts at the next Tide (forced), then the map is won and the party
//          returns: C5 back on the chart with the route one longer, C6 the save holds no chart name; in the first map
//          also R1-R5 and R2b the rejoin's host side with a pretend missing friend, R6 the lobby shut once the map ends,
//          and L1 the load watch saw the loading screen and sent nobody back
//   end    the campaign save round-trips through Continue; S1/S2 a slot switch leaves the campaign behind (pretended),
//          the same save keeps it; T0-T6 the retries and the campaign's end, on the boat with pretend results (a lost
//          map spends a retry and is the only map the boat offers, a map left while it ran counts as lost on the boat
//          and at Continue, Tide 15 cleared wins the map, no retries left ends the campaign, ten maps won complete it);
//          then the campaign is stopped and its keys removed, and H1 the party is taken back to the stock world.
//          I1 (on the boat) the player's id is kept for a restart, and says whether this launch put one back; K1 no
//          key in the save folder lacks RW_
//   boot   K0 the mode's breaks between Tides are 60 s and Mode_Normal's own table is untouched; CH every chart map of
//          500 seeds links to 1 to 3 maps
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
        try { BootChecks(); } catch (Exception e) { Check("boot checks ran", false, e.ToString()); }
        RogueWavesModeMod.Instance.StartCoroutine(Guarded());
    }

    // K0 and CH: data the mode builds at boot.
    static void BootChecks()
    {
        List<Wavesobject.Waves> mine = RWMode.ModeObject != null ? RWMode.ModeObject.Enemies.Wave : null;
        List<Wavesobject.Waves> stock = RWMode.Stock != null ? RWMode.Stock.Enemies.Wave : null;
        if (mine == null || stock == null) { Check("K0 the mode's Tide table", false, "no table: mode " + (mine != null) + ", Mode_Normal " + (stock != null)); return; }
        // Mode_Normal's breaks are 30 s, one 10 s (before Tide 19), the opening one 45 s (RogueLab round 1); none is 60 s.
        int breaks = 0, wrong = 0, stockBreaks = 0, stockAt60 = 0;
        float opening = -1f, stockOpening = -1f;
        string stockTimes = "";
        bool afterTide = false;
        for (int i = 0; i < mine.Count && i < stock.Count; i++)
        {
            if (mine[i].Wave) { afterTide = true; continue; }
            if (!afterTide) { opening = mine[i].Wavetime; stockOpening = stock[i].Wavetime; continue; }
            breaks++;
            if (!Mathf.Approximately(mine[i].Wavetime, RWMode.BreakSeconds)) wrong++;
            stockBreaks++;
            if (Mathf.Approximately(stock[i].Wavetime, RWMode.BreakSeconds)) stockAt60++;
            string t = RWUtil.F(stock[i].Wavetime);
            if (("," + stockTimes + ",").IndexOf("," + t + ",") < 0) stockTimes += (stockTimes == "" ? "" : ",") + t;
        }
        Check("K0 every break between Tides is " + RWUtil.F(RWMode.BreakSeconds) + " s, the opening break as Mode_Normal's", breaks > 0 && wrong == 0 && Mathf.Approximately(opening, stockOpening),
            breaks + " breaks, " + wrong + " not " + RWUtil.F(RWMode.BreakSeconds) + " s; opening " + RWUtil.F(opening) + " s (Mode_Normal " + RWUtil.F(stockOpening) + " s); " + mine.Count + " entries");
        Check("K0 Mode_Normal's own table is untouched", stockBreaks == breaks && stockAt60 == 0 && mine.Count == stock.Count,
            "Mode_Normal's breaks: " + stockBreaks + ", lengths " + stockTimes + " s, " + stockAt60 + " at " + RWUtil.F(RWMode.BreakSeconds) + " s");

        int worst = 0, fewest = 99, charts = 0;
        for (uint seed = 1; seed <= 500; seed++)
        {
            RWChart.Chart c = RWChart.Generate(seed, RWMode.Pool);
            charts++;
            for (int r = 1; r < RWChart.Rows; r++)
                foreach (RWChart.Node n in c.Rows[r]) { worst = Math.Max(worst, n.Links.Count); fewest = Math.Min(fewest, n.Links.Count); }
        }
        Check("CH every chart map links to 1 to " + RWChart.MaxLinks + " maps (" + charts + " seeds)", charts == 500 && fewest >= 1 && worst <= RWChart.MaxLinks, "fewest " + fewest + ", most " + worst);
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
        string kept = RWUtil.ReadString(RWData.SaveFolder, RWKeys.Guid);
        string unprefixed = "";
        foreach (string old in RWKeys.Old) if (RWUtil.Has(RWData.SaveFolder, old)) unprefixed += (unprefixed == "" ? "" : ", ") + old;
        Check("K1 no key in " + RWData.SaveFolder + " lacks " + RWData.KeyPrefix, unprefixed == "", (unprefixed == "" ? "none" : "still there: " + unprefixed) + "; " + RWKeys.Moved + " old keys renamed this launch");
        Check("I1 this player's id is kept for a restart", kept != "" && RWUtil.ReadString("Metadata", "Player_Guid") == kept
            && RWUtil.ReadString(RWData.SaveFolder, RWKeys.Device) == RWIdentity.Device(),
            (RWIdentity.Restored ? "put back from the last launch" : "first kept this launch") + ", session " + (Blm_Server.Instance.Localplayer != null ? Blm_Server.Instance.Localplayer.Sessionid.Value.ToString() : "-"));
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
            yield return Until(() => RWCampaign.Playing >= 0, 5f);
            Check("P1 map " + (m + 1) + ": the map in play is in the save while it runs", RWCampaign.Playing == route[m] && RWUtil.ReadString(RWData.SaveFolder, RWKeys.Playing) == route[m].ToString(),
                "Playing " + RWCampaign.Playing + ", saved '" + RWUtil.ReadString(RWData.SaveFolder, RWKeys.Playing) + "', route column " + route[m]);
            yield return Weather(m);
            if (m == 0) yield return Rejoin(m);
            if (m == 0) Check("L1 the load watch saw the loading screen and sent nobody back", RWLoad.Loads >= 1 && RWLoad.Kicked == 0, "loading screens " + RWLoad.Loads + ", sent back " + RWLoad.Kicked);
            yield return Win();
            if (m == 0) Check("R6 the lobby is shut once the map ends", !RWRejoin.DoorOpen && !Blm_Multiplayer.Instance.Server_Serveropen, "Server_Serveropen " + Blm_Multiplayer.Instance.Server_Serveropen);
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
        RWCampaign.SlotChanged();
        Check("S1 the same save keeps the campaign", RWCampaign.Active && RWCampaign.Live, "");
        RWCampaign.PretendOtherSave();
        RWCampaign.SlotChanged();
        Check("S2 another save leaves the campaign behind, in its own slot", !RWCampaign.Active && RWCampaign.Route.Count == 0
            && RWCampaign.Saved(out seed, out saved) && seed == Seed, "saved " + seed + " " + Digits(saved));
        // In the same frame (so the boat never sees it off): back in its own save for the retries.
        bool again = RWCampaign.Continue();
        Check("T0 Continue: " + RWCampaign.RetriesPerCampaign + " retries, no map to replay or in play", again && RWCampaign.Live && RWCampaign.Retries == RWCampaign.RetriesPerCampaign && RWCampaign.Replay < 0 && RWCampaign.Playing < 0,
            "retries " + RWCampaign.Retries + ", replay " + RWCampaign.Replay + ", playing " + RWCampaign.Playing);
        yield return Retries();
        RWCampaign.Stop();
        string left = "";
        foreach (string key in new[] { RWKeys.Seed, RWKeys.Route, RWKeys.Retries, RWKeys.Replay, RWKeys.Playing }) if (RWUtil.Has(RWData.SaveFolder, key)) left += key + " ";
        Check("Stop removes the campaign from the save", !RWCampaign.Saved(out seed, out saved) && left == "", left == "" ? "" : "left: " + left);
        string stockWorld, stockPlan;
        RWCampaign.Stock(out stockWorld, out stockPlan);
        yield return Until(() => !RWBoat.OnChart && Macro_Worldvisual.Instance.World != null && Macro_Worldvisual.Instance.World.name == stockWorld, 40f);
        Macro_Worldvisual home = Macro_Worldvisual.Instance;
        Check("H1 with no campaign on, the party is taken back to the stock world", !RWBoat.OnChart && home.World != null && home.World.name == stockWorld,
            "world " + (home.World != null ? home.World.name : "-") + ", stock " + stockWorld);
    }

    // T1-T6: the retries and the campaign's end, played on the boat with pretend map results (docs/scope/04 [Retries],
    // [Campaign]); the maps themselves are C4-C5's. Each step is what RWCampaign does at a map's end or on the boat.
    static IEnumerator Retries()
    {
        List<RWChart.Node> open = RWCampaign.Open();
        if (open.Count == 0) { Setup("T1-T6", "no choices from " + RWCampaign.Here.Name); yield break; }
        RWChart.Node lost = open[open.Count - 1];
        int c = lost.Col, sailed = RWCampaign.Route.Count;

        RWCampaign.Playing = c;
        RWCampaign.MapEnd("Lose");
        List<RWChart.Node> now = RWCampaign.Open();
        Check("T1 a lost map spends a retry, and the boat offers only that map", RWCampaign.Retries == 2 && RWCampaign.Replay == c && now.Count == 1 && now[0] == lost
            && RWCampaign.Route.Count == sailed && RWUtil.ReadString(RWData.SaveFolder, RWKeys.Retries) == "2" && RWUtil.ReadString(RWData.SaveFolder, RWKeys.Replay) == c.ToString(),
            "retries " + RWCampaign.Retries + ", replay " + RWCampaign.Replay + ", open " + Names(now) + ", saved '" + RWUtil.ReadString(RWData.SaveFolder, RWKeys.Retries) + "'/'" + RWUtil.ReadString(RWData.SaveFolder, RWKeys.Replay) + "'");
        yield return null;
        yield return null;
        if (open.Count < 2) Setup("T1 the boat lets only the lost map be picked", "one choice from " + RWCampaign.Here.Name);
        else
        {
            string wrong = "";
            foreach (RWChart.Node n in open) if (Listed(n.Name) != (n != lost)) wrong += n.Name + (n == lost ? " shut " : " open ");
            Check("T1 the boat lets only the lost map be picked", wrong == "", wrong == "" ? lost.Name + " open, " + (open.Count - 1) + " other choices blacklisted" : wrong);
        }

        RWCampaign.Playing = c;
        yield return null;
        yield return null;
        yield return null;
        Check("T2 a map left while it ran counts as lost, seen on the boat", RWCampaign.Retries == 1 && RWCampaign.Playing < 0 && RWCampaign.Replay == c,
            "retries " + RWCampaign.Retries + ", playing " + RWCampaign.Playing + ", replay " + RWCampaign.Replay);

        RWUtil.WriteString(RWData.SaveFolder, RWKeys.Playing, c.ToString());
        RWUtil.Flush();
        RWCampaign.Active = false;
        bool back = RWCampaign.Continue();
        Check("T3 Continue counts a map left while it ran as lost", back && RWCampaign.Retries == 0 && RWCampaign.Replay == c && RWCampaign.Playing < 0 && RWUtil.ReadString(RWData.SaveFolder, RWKeys.Playing) == "",
            "retries " + RWCampaign.Retries + ", replay " + RWCampaign.Replay + ", saved playing '" + RWUtil.ReadString(RWData.SaveFolder, RWKeys.Playing) + "'");

        RWCampaign.Playing = c;
        RWCampaign.Tide(RWCampaign.WinTide);
        bool notYet = RWCampaign.Route.Count == sailed;
        RWCampaign.Tide(RWCampaign.WinTide + 1);
        Check("T4 a map counts as won once Tide " + RWCampaign.WinTide + " is cleared, and the replay is over", notYet && RWCampaign.Route.Count == sailed + 1 && RWCampaign.Route[sailed] == c
            && RWCampaign.Replay < 0 && RWCampaign.Playing < 0 && RWUtil.ReadString(RWData.SaveFolder, RWKeys.Route).Length == sailed + 1,
            "route " + Digits(RWCampaign.Route) + " (was " + sailed + " long), replay " + RWCampaign.Replay);
        RWCampaign.MapEnd("Lose");
        Check("T4 dying in a bonus Tide loses nothing", RWCampaign.Live && RWCampaign.Retries == 0 && RWCampaign.Route.Count == sailed + 1, "retries " + RWCampaign.Retries + ", live " + RWCampaign.Live);

        List<RWChart.Node> next = RWCampaign.Open();
        if (next.Count == 0) { Setup("T5", "no choices after the route grew"); yield break; }
        RWCampaign.Playing = next[0].Col;
        RWCampaign.MapEnd("Lose");
        uint seed;
        List<int> saved;
        Check("T5 with no retries left, a lost map ends the campaign and keeps nothing", !RWCampaign.Active && RWCampaign.Ended == "lost" && !RWCampaign.Saved(out seed, out saved),
            "active " + RWCampaign.Active + ", ended '" + RWCampaign.Ended + "'");

        RWCampaign.New(Seed);
        RWChart.Node at = RWCampaign.Here;
        while (RWCampaign.Route.Count < RWChart.Rows - 1 && at != null && at.Links.Count > 0)
        {
            RWCampaign.Route.Add(at.Links[0]);
            at = RWCampaign.Here;
        }
        RWCampaign.Playing = RWCampaign.Open()[0].Col;
        RWCampaign.MapEnd("Win");
        Check("T6 the tenth map won completes the campaign, which leaves the save", !RWCampaign.Active && RWCampaign.Ended == "won" && !RWCampaign.Saved(out seed, out saved) && !RWCampaign.Continue(),
            "route " + Digits(RWCampaign.Route) + ", ended '" + RWCampaign.Ended + "'");
    }

    // The host's boat blacklist holds the node of this name in the world shown.
    static bool Listed(string node)
    {
        Macro_Worldvisual vis = Macro_Worldvisual.Instance;
        if (vis == null || vis.World == null || Macro_Main.Instance == null) return false;
        foreach (Macro_Link_Add_Base l in vis.World.GetComponentsInChildren<Macro_Link_Add_Base>(true))
            if (l.name == node) return Macro_Main.Instance.Blacklist.Contains(l);
        return false;
    }

    static string Names(List<RWChart.Node> nodes)
    {
        List<string> n = new List<string>();
        foreach (RWChart.Node x in nodes) n.Add(x.Name);
        return n.Count == 0 ? "(none)" : string.Join(", ", n.ToArray());
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

    // R1-R5: the rejoin's host side, solo. A pretend friend's seat stands in for a drop; a real drop and rejoin need
    // a second player (docs/mode/REJOIN.md §6, O1-O8).
    static IEnumerator Rejoin(int m)
    {
        RWChart.Node node = RWCampaign.Chart.At(m + 1, route[m]);
        yield return new WaitForSecondsRealtime(1f);
        // The loading screen shuts the lobby when it ends (Game_Load, "Loading 90%", 10 s after the fly-in; round 5
        // probed inside that window). R2b plays that close on purpose; the rest wait until it has happened.
        float t0 = Time.realtimeSinceStartup;
        while (UnityEngine.Object.FindObjectOfType<Game_Load>() != null && Time.realtimeSinceStartup - t0 < 30f)
            yield return null;
        int e0 = errors;
        Blm_Player me = Blm_Server.Instance.Localplayer;
        Tds_Clientinfo info = me.GetComponent<Tds_Clientinfo>();
        long mine = me.Sessionid.Value;
        RWRejoin.Seat seat;
        bool seated = RWRejoin.Seats.TryGetValue(mine, out seat);
        Check("R1 the host keeps a seat for each player in the map", seated && seat.Hero == info.Hero.Value.ToString() && seat.Aura.Count == info.Resources.Count,
            RWRejoin.Seats.Count + " seats" + (seated ? ", mine: " + seat.Hero + ", team " + seat.Team + ", " + seat.Aura.Count + " aura kinds" : ", none for session " + mine));

        bool open0 = Blm_Multiplayer.Instance.Server_Serveropen;
        int lines0 = Blm_Ui_Chat.Instance != null && Blm_Ui_Chat.Instance.Chat != null ? Blm_Ui_Chat.Instance.Chat.Count : -1;
        const long Pretend = 990001;
        RWRejoin.Seats[Pretend] = new RWRejoin.Seat { Session = Pretend, Name = "Test friend", Hero = info.Hero.Value.ToString(), Info = info };
        yield return new WaitForSecondsRealtime(0.5f);
        Check("R2 an empty seat opens the lobby", RWRejoin.DoorOpen && Blm_Multiplayer.Instance.Server_Serveropen, "Server_Serveropen " + open0 + " -> " + Blm_Multiplayer.Instance.Server_Serveropen);
        Blm_Multiplayer_Remote.Instance.Changelobbystatus_ServerOnly("Closed");
        Blm_Multiplayer.Instance.Server_Serveropen = false;
        yield return null;
        yield return null;
        Check("R2b the lobby opens again after the game shuts it (as its loading screen does)", RWRejoin.DoorOpen && Blm_Multiplayer.Instance.Server_Serveropen, "Server_Serveropen " + Blm_Multiplayer.Instance.Server_Serveropen + " two frames after the game's close");
        int lines1 = Blm_Ui_Chat.Instance != null && Blm_Ui_Chat.Instance.Chat != null ? Blm_Ui_Chat.Instance.Chat.Count : -1;
        if (lines0 < 0) Setup("R3 the drop is said in chat", "no chat in the map");
        else Check("R3 the drop is said in chat", lines1 == lines0 + 1, "chat lines " + lines0 + " -> " + lines1 + (lines1 > 0 ? ": '" + Blm_Ui_Chat.Instance.Chat[lines1 - 1].Text + "'" : ""));
        RWRejoin.Seats.Remove(Pretend);
        yield return new WaitForSecondsRealtime(2.5f);
        Check("R2 the lobby shuts once every seat is filled", !RWRejoin.DoorOpen && !Blm_Multiplayer.Instance.Server_Serveropen, "Server_Serveropen " + Blm_Multiplayer.Instance.Server_Serveropen);

        // R4: a seat given back to a fresh clientinfo, played on the host's own: aura, vessel name, Activeplayer.
        RWRejoin.Seat copy = new RWRejoin.Seat { Session = mine };
        RWRejoin.Copy(copy, info, me);
        float before = 0f, after = 0f;
        foreach (Link_Resource r in info.Resources) before += r.Amount;
        info.Resources.Clear();
        RWRejoin.Restore(info, copy);
        yield return new WaitForSecondsRealtime(1f);
        foreach (Link_Resource r in info.Resources) after += r.Amount;
        Check("R4 a seat given back restores aura, vessel and Activeplayer", Mathf.Approximately(before, after) && info.Hero.Value.ToString() == copy.Hero && info.Activeplayer.Value > 0,
            "aura " + RWUtil.F(before) + " -> " + RWUtil.F(after) + " over " + info.Resources.Count + " kinds, hero " + info.Hero.Value + ", Activeplayer " + info.Activeplayer.Value);

        int w = RWRejoin.MapWeather();
        Check("R5 a rejoiner finds this map's weather from the synced names", w == node.Weather, RWWeather.Display(w) + ", chart says " + RWWeather.Display(node.Weather) + " (" + Tds_Multiplayerinfo.Instance.World.Value + ", " + Tds_Multiplayerinfo.Instance.Gameplan.Value + ")");
        Check("R no error during the rejoin probes", errors == e0, (errors - e0) + " error lines");
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
        Check("C5 a won map is sailed, and is no longer in play", RWCampaign.Route.Count == before + 1 && RWCampaign.Playing < 0 && RWUtil.ReadString(RWData.SaveFolder, RWKeys.Playing) == "",
            "route " + Digits(RWCampaign.Route) + ", playing " + RWCampaign.Playing + ", Time_Scale " + RWUtil.F(Game_Mapvars.Instance != null ? Game_Mapvars.Instance.Time_Scale.Value : -1f));
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
