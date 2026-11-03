// On the boat (the Setup scene, the game's lobby), every frame (docs/mode/SEA_CHART.md §3 steps 4-8):
//   every machine  registers the chart world named by Tds_Multiplayerinfo.World or Macro_Main.Worlded (Startup waits
//                  for it with no timeout, so this must not wait for Macro_Main.Ready); on the chart, blacklists every
//                  node but the next choices, hides the Warp button, writes the picked node's weather into the Setup
//                  panel and preloads that weather's objects
//   host           once the boat is up and a campaign is on, warps the party onto the chart the way Selectworld_ServerRpc
//                  does; notes the node picked (Selected) for the map about to load; keeps chart names out of the save.
//                  A map left while it ran counts as lost here (RWCampaign.OnBoat); after a lost map only that map can be
//                  picked (RWCampaign.Open: the retry replays it). With no campaign on, a party still on a chart is taken
//                  back to the stock world it sailed from
//   leader (host)  when RWCampaign.Choice names a column (the vote's winner, or a self-test), hovers that node so the
//                  stock boat sails there, and picks it on arrival (Selectmap_ServerRpc), which opens the Setup panel
// Until the vote piece exists (docs/mode/SEA_CHART.md §4, route V1: the in-map vote), the leader may also steer with the stock
// arrows: they only offer the next row's linked maps, and only those can be picked (on the host, only the map to replay
// after a loss; only the leader steers, so the host's blacklist is the one that counts).
public static class RWBoat
{
    static Macro_Main seenMain;
    static float upAt = -1f, slowAt, arriveSince = -1f;
    static GameObject keptWorld;
    static int keptReplay = -1;
    public static string Warped = "";        // the chart the host warped to in this boat scene
    public static string Sailing = "";       // the node the leader's boat is sailing to by itself
    public static float SailedAt = -1f;      // realtime the sail started, and when it arrived (self-test reads them)
    public static float ArrivedAt = -1f;
    public static string Picked = "";        // the node Selectmap_ServerRpc was sent for
    public static string SceneNote = "";     // where the boat and the stock world were before the first warp

    public static bool OnChart
    {
        get
        {
            Macro_Worldvisual vis = Macro_Worldvisual.Instance;
            return vis != null && vis.World != null && vis.World.name.StartsWith(RWData.WorldPrefix);
        }
    }

    public static void Update()
    {
        Macro_Main main = Macro_Main.Instance;
        Macro_Worldvisual vis = Macro_Worldvisual.Instance;
        if (main == null || vis == null || Tds_Multiplayerinfo.Instance == null || !main.IsSpawned)
        {
            seenMain = null;
            return;
        }
        if (main != seenMain)
        {
            seenMain = main;
            upAt = -1f;
            Warped = Sailing = Picked = "";
            arriveSince = SailedAt = ArrivedAt = -1f;
            keptWorld = null;
        }
        string world = Tds_Multiplayerinfo.Instance.World.Value.ToString(), worlded = main.Worlded.Value.ToString();
        if (worlded.StartsWith(RWData.WorldPrefix)) RWWorld.Ensure(worlded);
        else if (world.StartsWith(RWData.WorldPrefix)) RWWorld.Ensure(world);
        if (!main.Ready)
            return;
        if (upAt < 0f) upAt = Time.realtimeSinceStartup;
        bool slow = Time.realtimeSinceStartup - slowAt > 0.5f;
        if (slow) slowAt = Time.realtimeSinceStartup;
        if (OnChart)
            Keep(main, vis, slow);
        if (RWUtil.IsHost)
            Host(main, vis, slow);
    }

    // ---------------------------------------------------------------- every machine, on the chart

    static void Keep(Macro_Main main, Macro_Worldvisual vis, bool slow)
    {
        uint seed;
        List<int> route;
        if (!RWWorld.Parse(vis.World.name, out seed, out route))
            return;
        RWChart.Chart chart = RWWorld.Chart(seed);
        bool campaign = RWUtil.IsHost && RWCampaign.Live && vis.World.name == RWCampaign.WorldName();
        int replay = campaign ? RWCampaign.Replay : -1;
        if (keptWorld != vis.World || keptReplay != replay)
        {
            keptWorld = vis.World;
            keptReplay = replay;
            List<string> open = new List<string>();
            foreach (RWChart.Node n in campaign ? RWCampaign.Open() : RWWorld.Choices(chart, route)) open.Add(n.Name);
            if (campaign && RWCampaign.Staging) open.Add(chart.At(0, 0).Name);
            int shut = 0;
            foreach (Macro_Link_Add_Base node in vis.World.GetComponentsInChildren<Macro_Link_Add_Base>(true))
            {
                bool closed = !open.Contains(node.name), listed = main.Blacklist.Contains(node);
                if (closed && !listed) main.Blacklist.Add(node);
                else if (!closed && listed) main.Blacklist.Remove(node);   // open again (a replay was won or dropped)
                if (closed) shut++;
            }
            RWLog.Info("on " + vis.World.name + ": choices " + string.Join(", ", open.ToArray()) + (replay >= 0 ? " (the lost map, replayed)" : "") + "; " + shut + " nodes blacklisted");
        }
        if (!slow)
            return;
        try
        {
            GameObject warp = Blockage.Menu.Bl_Menus.Menus["World"].Items["Warp"];
            if (warp != null && warp.activeSelf) warp.SetActive(false);
        }
        catch (Exception) { }
        RWChart.Node picked = chart.Find(main.Selected.Value.ToString());
        RWWeather.Preload(RWChart.NestingGround);   // Raze the Nest, on any map
        if (picked != null && picked.Row == 0 && vis.Insetup)
            SetupNote("Setting out: choose your Directions, then vote on the first map");
        else if (picked != null && picked.Weather != RWChart.Calm)
        {
            RWWeather.Preload(picked.Weather);
            if (vis.Insetup)
                SetupNote(RWWeather.Display(picked.Weather) + ": " + RWWeather.Short(picked.Weather));
        }
    }

    // The Setup panel's "State" line, which the game leaves empty (Macro_Setupui.cs Mapselected).
    static void SetupNote(string text)
    {
        try
        {
            Transform state = Blockage.Menu.Bl_Menus.Menus["Setup"].Items["Stats"].transform.Find("State");
            TMPro.TMP_Text t = state != null ? state.GetComponent<TMPro.TMP_Text>() : null;
            if (t != null && t.text != text) t.text = text;
        }
        catch (Exception) { }
    }

    // ---------------------------------------------------------------- the host

    static void Host(Macro_Main main, Macro_Worldvisual vis, bool slow)
    {
        if (slow)
            Guard();
        RWMenu.Boat();
        RWCampaign.OnBoat();
        if (!RWCampaign.Live)
        {
            Home(main, vis);
            return;
        }
        string target = RWCampaign.WorldName();
        if (main.Worlded.Value.ToString() != target && Warped != target && !vis.Inworldtransition && Time.realtimeSinceStartup - upAt > 1.5f)
            Warp(main, vis, target);
        if (!OnChart || vis.World.name != target)
            return;
        if (slow)
        {
            Lock();
            RWCardinals.RestoreLobby();
        }
        // A new campaign's first stop opens by itself: the leader's boat is already there.
        if (RWCampaign.Staging && main.Selected.Value.ToString() == "" && !vis.Inworldtransition && !vis.Insetup && Time.realtimeSinceStartup - upAt > 4f && RWUtil.IsLeader && Picked == "")
        {
            RWChart.Node home = RWCampaign.Chart.At(0, 0);
            Picked = home.Name;
            RWLog.Info("staging: the harbour opens");
            Tds_Multiplayer_Remote.Instance.Selectmap_ServerRpc(home.Name, false, default(Unity.Netcode.ServerRpcParams));
        }
        RWChart.Node picked = RWCampaign.Chart.Find(main.Selected.Value.ToString());
        if (picked != null && RWCampaign.Open().Contains(picked) && RWCampaign.Next != picked)
        {
            RWCampaign.Next = picked;
            RWCampaign.Choice = -1;
            RWLog.Info("picked " + picked.Name + ": " + picked.Plan + " (" + picked.Map + "), " + RWChart.WeatherNames[picked.Weather]);
        }
        if (RWCampaign.Choice >= 0 && RWUtil.IsLeader)
            Sail(main, vis);
    }

    // Vessels are locked in once the campaign has seen them (docs/scope/04 [Campaign]): a player's vessel goes back to theirs,
    // said once. A friend new to the campaign picks freely.
    static readonly List<string> lockSaid = new List<string>();

    static void Lock()
    {
        foreach (long s in RWRun.Sessions())
        {
            Blm_Player bp;
            if (!Blm_Server.Instance.Sessionidsplayer.TryGetValue(s, out bp) || bp == null)
                continue;
            Tds_Clientinfo info = bp.GetComponent<Tds_Clientinfo>();
            RWPlayer p = RWRun.For(s);
            if (info == null || p.Hero == "" || info.Hero.Value.ToString() == p.Hero)
                continue;
            info.Hero.Value = new FixedString64Bytes(p.Hero);
            if (!lockSaid.Contains(p.Key))
            {
                lockSaid.Add(p.Key);
                RWRejoin.Say(p.Name + "'s vessel is locked in for this campaign: " + RWRun.HeroName(p.Hero) + ".");
            }
        }
    }

    static void Warp(Macro_Main main, Macro_Worldvisual vis, string target)
    {
        if (RWWorld.Ensure(target) == null)
            return;
        if (SceneNote == "")
        {
            Bounds b = new Bounds(vis.Boat.transform.position, Vector3.zero);
            if (vis.World != null)
                foreach (Renderer r in vis.World.GetComponentsInChildren<Renderer>()) b.Encapsulate(r.bounds);
            SceneNote = "boat at " + RWUtil.V(vis.Boat.transform.position) + ", stock world " + (vis.World != null ? vis.World.name : "-") + " spans " + RWUtil.V(b.min) + " to " + RWUtil.V(b.max);
            RWLog.Info(SceneNote);
        }
        RWCampaign.RememberStock();
        Tds_Multiplayerinfo.Instance.World.Value = new FixedString64Bytes(target);
        if (main.Selected.Value.ToString() != "")
            main.Selected.Value = new FixedString64Bytes("");
        main.Worlded.Value = new FixedString64Bytes("");
        main.Worlded.Value = new FixedString64Bytes(target);
        Warped = target;
        RWCampaign.Next = null;
        RWLog.Info("warped the party onto " + target);
    }

    // No campaign on (it ended, or was left behind) but the party is on a chart: back to the stock world the boat sailed
    // from, the same way the warp onto the chart went, so no chart map is played outside a campaign.
    static void Home(Macro_Main main, Macro_Worldvisual vis)
    {
        if (!OnChart || vis.Inworldtransition || Time.realtimeSinceStartup - upAt < 1.5f)
            return;
        string world, plan;
        RWCampaign.Stock(out world, out plan);
        if (world == "" || world.StartsWith(RWData.WorldPrefix) || Warped == world || main.Worlded.Value.ToString() == world)
            return;
        if (RWUtil.Item<Mapworlddatabaseobject>("World", world) == null)
        {
            RWLog.Once("home " + world, "no campaign, but the stock world " + world + " isn't in the database: the party stays on the chart");
            return;
        }
        Tds_Multiplayerinfo.Instance.World.Value = new FixedString64Bytes(world);
        if (main.Selected.Value.ToString() != "")
            main.Selected.Value = new FixedString64Bytes("");
        main.Worlded.Value = new FixedString64Bytes("");
        main.Worlded.Value = new FixedString64Bytes(world);
        Warped = world;
        RWLog.Info("no campaign on: took the party back to " + world);
    }

    // Hovers the chosen node as the leader's click would, so the stock boat sails there; picks it on arrival.
    static void Sail(Macro_Main main, Macro_Worldvisual vis)
    {
        RWChart.Node to = null;
        foreach (RWChart.Node n in RWCampaign.Open()) if (n.Col == RWCampaign.Choice) to = n;
        if (to == null) { RWLog.Warn("choice " + RWCampaign.Choice + " is not open from where the boat stands" + (RWCampaign.Replay >= 0 ? " (the lost map, column " + RWCampaign.Replay + ", is replayed)" : "")); RWCampaign.Choice = -1; return; }
        Macro_Link_Add_Base node = null;
        foreach (Macro_Link_Add_Base l in vis.World.GetComponentsInChildren<Macro_Link_Add_Base>(true))
            if (l.name == to.Name) node = l;
        if (node == null || vis.Inworldtransition || vis.Insetup)
            return;
        if (Sailing != to.Name)
        {
            Sailing = to.Name;
            SailedAt = Time.realtimeSinceStartup;
            ArrivedAt = arriveSince = -1f;
            RWLog.Info("sailing to " + to.Name);
        }
        if (main.Hovering != node)
        {
            main.Hovering = node;
            main.Prevhovering = node;
            main.Preview = node;
            try { Macro_Worldui.Instance.Showhover(); } catch (Exception e) { RWLog.Once("showhover", "Showhover: " + e.Message); }
        }
        Transform dock = node.transform.Find("Dock");
        Vector3 at = dock != null ? dock.position : node.transform.position;
        bool there = RWUtil.Flat(vis.Boat.transform.position, at) < 1.5f && vis.Boatspeed < 1f;
        if (!there) { arriveSince = -1f; return; }
        if (arriveSince < 0f) { arriveSince = Time.realtimeSinceStartup; return; }
        if (Time.realtimeSinceStartup - arriveSince < 0.8f)
            return;
        ArrivedAt = Time.realtimeSinceStartup;
        RWLog.Info("arrived at " + to.Name + " after " + RWUtil.F(ArrivedAt - SailedAt) + " s, boat " + RWUtil.V(vis.Boat.transform.position) + ", camera " + RWUtil.V(Game_Cameracontroller.Instance.Movepos));
        Picked = to.Name;
        RWCampaign.Choice = -1;
        Tds_Multiplayer_Remote.Instance.Selectmap_ServerRpc(to.Name, false, default(Unity.Netcode.ServerRpcParams));
    }

    // A chart's name in the save would hang the boat's loading screen once the pack is gone (Macro_Main.cs:404): the
    // leader's map click writes Player_Worldselect (:633) and Player_Gameplanselect (:634), so both go back to the
    // last stock values.
    static void Guard()
    {
        string w = RWUtil.ReadString("Player", "Player_Worldselect"), g = RWUtil.ReadString("Player", "Player_Gameplanselect");
        if (!w.StartsWith(RWData.WorldPrefix) && !g.StartsWith("RW_"))
            return;
        string sw, sg;
        RWCampaign.Stock(out sw, out sg);
        if (w.StartsWith(RWData.WorldPrefix)) RWUtil.WriteString("Player", "Player_Worldselect", sw);
        if (g.StartsWith("RW_")) RWUtil.WriteString("Player", "Player_Gameplanselect", sg);
        RWUtil.Flush();
        RWLog.Info("save guard: Player_Worldselect " + w + " -> " + sw + ", Player_Gameplanselect " + g + " -> " + sg);
    }
}
