// The campaign card (docs/reports/UX_FEATURES_2026-11-03.md T1-T4): one line, "Map 4 of 10 ▪ Stretch 1 ▪ Rising Water ▪ 2
// retries left", on the screens a player already passes through. No screen of its own, no new sync:
//   every machine reads the facts from the chart world's name (seed, route, retries; 04_World.cs) and the Gameplan's
//   T1  the loading screen's bar text (Game_Load's Ui/Bar/Desc, written by the stock once, about 3 s in)
//   T2  the map intro card's line (Cine's Maptitlealt/Desc, written by the stock once)
//   T3  the boat's world card: the chart world's Display_Desc (RWWorld.Build)
//   T4  the end screen's title: the host writes the synced Display_Endgame ("MAP 4 OF 10 WON", "MAP LOST ▪ 2 RETRIES LEFT",
//       "CAMPAIGN WON") after Mode_Normal's own write; the end screen reads it about 5 s later
// And the retry warnings (B4): the host's Exit Map dialog names what leaving costs, rewritten while that dialog is up.
public static class RWCard
{
    public const string Separator = " ▪ ";    // the game's own separator
    public static string LastLine = "", LastEnd = "";
    public static int LoadingWrites, CardWrites, ExitWrites;   // self-test
    static float nextLook;

    public static string Retries(int n) { return n + (n == 1 ? " retry left" : " retries left"); }

    public static int StretchOf(int row) { return row <= RWChart.Stretch ? 1 : 2; }

    // The card for the chart map being loaded or played, on any machine; "" outside a chart map.
    public static string Line()
    {
        Tds_Multiplayerinfo info = Tds_Multiplayerinfo.Instance;
        if (info == null)
            return "";
        string plan = info.Gameplan.Value.ToString(), world = info.World.Value.ToString();
        if (!plan.StartsWith(RWData.PlanPrefix))
            return "";
        uint seed;
        List<int> route;
        int retries;
        if (!RWWorld.Parse(world, out seed, out route, out retries))
            return "";
        RWChart.Chart chart = RWWorld.Chart(seed);
        if (route.Count == 0 && plan == RWMode.Twin(RWWorld.StagingPlan(chart)))
            return "Setting out" + Separator + "Choose your Directions, then vote";
        RWChart.Node node = null;
        foreach (RWChart.Node n in RWWorld.Choices(chart, route))
            if (RWMode.Twin(n.Plan) == plan) node = n;
        return node != null ? Line(node.Row, node.Weather, retries) : "";
    }

    public static string Line(int row, int weather, int retries)
    {
        string line = "Map " + row + " of " + RWChart.Rows + Separator + "Stretch " + StretchOf(row);
        if (weather != RWChart.Calm) line += Separator + RWWeather.Display(weather);
        if (retries >= 0) line += Separator + Retries(retries);
        return line;
    }

    // Every machine, every frame; cheap outside a loading screen or a map's first moments.
    public static void Update()
    {
        if (Time.unscaledTime < nextLook)
            return;
        nextLook = Time.unscaledTime + 0.1f;
        string scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (scene != seenScene)
        {
            seenScene = scene;
            sceneAt = Time.unscaledTime;
        }
        // The bar shows over the map's own scene for a while too: looked for only in the first moments of a scene.
        if (scene != "Setup" && !RWMenu.OnMainMenu && Time.unscaledTime - sceneAt < 120f) Loading();
        if (scene == "Game") { Intro(); ExitWarning(); HideScore(); HoldEnd(); }
    }

    static string seenScene = "";
    static float sceneAt;

    // Score doesn't count in this mode (docs/scope/04 [Campaign]; UX report B9): the end screens' score line goes.
    static void HideScore()
    {
        Tds_Multiplayerinfo info = Tds_Multiplayerinfo.Instance;
        if (info == null || !info.Gameplan.Value.ToString().StartsWith(RWData.PlanPrefix) || Blockage.Menu.Bl_Menus.Menus == null)
            return;
        foreach (string name in new[] { "Win", "Lose" })
        {
            Blockage.Menu.Blu2_Menu_Handler menu;
            GameObject score;
            if (Blockage.Menu.Bl_Menus.Menus.TryGetValue(name, out menu) && menu != null && menu.Items.TryGetValue("Recordscore", out score) && score != null && score.activeSelf)
                score.SetActive(false);
        }
    }

    static void Loading()
    {
        Game_Load load = UnityEngine.Object.FindObjectOfType<Game_Load>();
        Transform desc = load != null && load.Ui != null ? load.Ui.Find("Bar/Desc") : null;
        TMPro.TMP_Text t = desc != null ? desc.GetComponent<TMPro.TMP_Text>() : null;
        if (t == null || string.IsNullOrEmpty(t.text) || !t.gameObject.activeInHierarchy)
            return;
        string line = Line();
        if (line == "" || t.text == line)
            return;
        t.text = line;
        LastLine = line;
        LoadingWrites++;
    }

    static void Intro()
    {
        Blockage.Menu.Blu2_Menu_Handler cine;
        GameObject alt;
        if (Time.unscaledTime - sceneAt > 60f || Blockage.Menu.Bl_Menus.Menus == null || !Blockage.Menu.Bl_Menus.Menus.TryGetValue("Cine", out cine) || cine == null
            || !cine.Items.TryGetValue("Maptitlealt", out alt) || alt == null)
            return;
        TMPro.TMP_Text t = null;
        foreach (TMPro.TMP_Text x in alt.GetComponentsInChildren<TMPro.TMP_Text>(true))
            if (x.name == "Desc") t = x;
        if (t == null || string.IsNullOrEmpty(t.text))
            return;
        string line = Line();
        if (line == "" || t.text == line)
            return;
        t.text = line;
        LastLine = line;
        CardWrites++;
    }

    // The host's "EXIT MAP / Return to the world?" prompt is built in a private method (Tds_Pausemenu.Exitgamehost); the
    // title tells it apart. A friend's own leaving costs nothing, and their prompt has another title.
    static void ExitWarning()
    {
        if (!RWDialog.Up() || !RWUtil.IsServer || !RWCampaign.Live || RWDialog.Asking)
            return;
        Bla_Ui_Confirmation dialog = Bla_Ui_Confirmation.Instance;
        TMPro.TMP_Text title = dialog.Titlebox != null ? dialog.Titlebox.GetComponentInChildren<TMPro.TMP_Text>() : null;
        if (title == null || title.text.ToUpperInvariant() != "EXIT MAP")
            return;
        string warn = ExitLine();
        if (warn == "")
            return;
        TMPro.TMP_Text sub = dialog.Subbox != null ? dialog.Subbox.GetComponentInChildren<TMPro.TMP_Text>() : null;
        if (sub != null && sub.text != warn)
        {
            sub.text = warn;
            ExitWrites++;
        }
    }

    public static string ExitLine()
    {
        if (RWCampaign.Playing >= 0)
            return RWCampaign.Retries > 0 ? "Leaving now loses this map: " + Retries(RWCampaign.Retries - 1) : "Leaving now loses this map, and with no retries left the campaign is over";
        return RWCampaign.WonThisMap ? "The map is won: leave now?" : "";
    }

    // ---------------------------------------------------------------- the end screen's title (host)

    // Host, right after the map's end is known; every machine reads the synced title.
    public static void EndTitle(string title)
    {
        Game_Mapvars vars = Game_Mapvars.Instance;
        if (vars == null || !RWUtil.IsServer || title == "")
            return;
        endMap = vars;
        LastEnd = title;
        HoldEnd();
    }

    static Game_Mapvars endMap;

    // Mode_Normal writes the whole display from its own copy at its own moments (kit docs/20 §2), so the title is held
    // until the map goes.
    static void HoldEnd()
    {
        Game_Mapvars vars = Game_Mapvars.Instance;
        if (ReferenceEquals(endMap, null) || endMap == null || !ReferenceEquals(vars, endMap) || !RWUtil.IsServer)
            return;
        Game_Mapvars.Link_Displaydataref d = vars.Display.Value;
        if (d.Display_Endgame.ToString() != LastEnd)
        {
            d.Display_Endgame = new FixedString64Bytes(LastEnd);
            vars.Display.Value = d;
        }
        // The screen reads the display once: Mode_Normal's own write may be what it read (seen on the PC).
        foreach (string name in new[] { "Win", "Lose" })
        {
            Blockage.Menu.Blu2_Menu_Handler menu;
            GameObject shown;
            if (Blockage.Menu.Bl_Menus.Menus == null || !Blockage.Menu.Bl_Menus.Menus.TryGetValue(name, out menu) || menu == null || !menu.Items.TryGetValue("Title", out shown) || shown == null || !shown.activeInHierarchy)
                continue;
            TMPro.TMP_Text text = shown.GetComponentInChildren<TMPro.TMP_Text>();
            if (text != null && text.text != LastEnd) text.text = LastEnd;
        }
    }
}

// No-mods warning and the rejoin line (docs/reports/UX_FEATURES_2026-11-03.md L1, L5).
//   L1  on the boat the host reads each player's networked Tds_Clientinfo.Mods (owner-written, "any mod is on"), and says
//       once in chat, with the chat shown, that a friend without mods can't load the map. It can't tell which mods.
//   L5  a friend dropped from a map lands on the main menu, whose error box shows the last line of Multiplayererror;
//       their own mod adds the way back in
public static class RWLobby
{
    public const string RejoinLine = "Rejoin: Steam friends, right-click the host, Join Game.";
    public static readonly List<string> Warned = new List<string>();  // self-test
    public static bool InMapAsClient;
    public static int RejoinLines;
    static float nextLook;

    public static void Update()
    {
        if (Time.unscaledTime < nextLook)
            return;
        nextLook = Time.unscaledTime + 1f;
        Unity.Netcode.NetworkManager net = Unity.Netcode.NetworkManager.Singleton;
        if (net != null && net.IsClient && !net.IsHost && UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Game")
            InMapAsClient = true;
        // The game writes the flag only at a scene change after the player object spawned, so a pack machine that has just
        // joined the boat still reads false (seen on the PC): every machine with the pack says so itself, at once.
        Blm_Server server = Blm_Server.Instance;
        Tds_Clientinfo mine = server != null && server.Localplayer != null ? server.Localplayer.GetComponent<Tds_Clientinfo>() : null;
        if (mine != null && mine.IsOwner && !mine.Mods.Value)
            mine.Mods.Value = true;
        if (Macro_Main.Instance != null && Macro_Main.Instance.Ready && RWUtil.IsHost)
            Check();
    }

    public const float GraceSeconds = 8f;       // how long a friend who has just joined has to say so
    static readonly Dictionary<string, float> firstSeen = new Dictionary<string, float>();

    // Host on the boat.
    public static void Check()
    {
        Blm_Server server = Blm_Server.Instance;
        if (server == null || server.Players == null)
            return;
        foreach (GameObject o in server.Players)
        {
            Blm_Player p = o != null ? o.GetComponent<Blm_Player>() : null;
            Tds_Clientinfo info = o != null ? o.GetComponent<Tds_Clientinfo>() : null;
            if (p == null || info == null || info.Mods.Value || p.IsOwner)
                continue;
            string name = p.Displayname.Value.ToString();
            float since;
            if (!firstSeen.TryGetValue(name, out since)) { firstSeen[name] = Time.unscaledTime; continue; }
            if (Time.unscaledTime - since < GraceSeconds || Warned.Contains(name))
                continue;
            Warned.Add(name);
            RWRejoin.Say(name + " has no mods on: they can't load a Rogue Waves map. Everyone needs the same pack.");
            try { if (Blm_Ui_Chat.Instance != null) Blm_Ui_Chat.Instance.Showchat(); } catch (Exception) { }
            RWLog.Info("no mods on: " + name);
        }
    }

    // RWMenu, once per main menu: a client who was in a map and didn't choose to leave gets the rejoin line last.
    public static void AtMainMenu()
    {
        Warned.Clear();
        firstSeen.Clear();
        if (!InMapAsClient)
            return;
        InMapAsClient = false;
        AddRejoinLine();
    }

    public static void AddRejoinLine()
    {
        Blm_Multiplayer mp = Blm_Multiplayer.Instance;
        if (mp == null || mp.Multiplayererror == null)
            return;
        // A friend who left through their own menu has no error to read: the box then stays as it was.
        if (mp.Multiplayererror.Count == 0)
            return;
        mp.Multiplayererror.Add(RejoinLine);
        RejoinLines++;
    }
}

// The pack's tips in the game's tip lists (docs/reports/UX_FEATURES_2026-11-03.md T6): the end screen's Earlygame_Funfacts,
// the pause menu's and the main menu's blurbs each read a Factobject (Clientmode_Normal.cs:106-131, Tds_Pausemenu.cs:255-263,
// Mainmenu_Main.cs:203-211). Each entry's Unlockconditions gates it: the pack's show only with a key the mode sets when a
// campaign starts, so a player who never sailed sees only the stock tips. The Factobjects are database and scene assets;
// a scene's own (the pause menu's, the main menu's) is added to whenever one appears.
public static class RWTips
{
    public const string SeenKey = "RW_Campaign_Sailed";
    public static readonly string[] Lines =
    {
        "Only the host leaving a map spends a retry. A friend who drops can rejoin it.",
        "A map counts as won once Tide 15 is cleared: dying in a bonus Tide costs nothing.",
        "A map with sea weather pays more Pure Aura each Tide.",
        "VENDCINT falls in at the start of every break and leaves at its end.",
        "Directions stay with a vessel for the whole campaign; buy their next rank at VENDCINT.",
        "Fund Transfer sends base aura to a friend, never Pure Aura.",
        "An objective the team takes pays every player in full.",
        "Follow the sea chart ahead when you vote: the links decide which maps come next.",
    };
    public static int Added;
    static readonly List<Factobject> done = new List<Factobject>();
    static float nextLook;

    public static void Add(Factobject facts)
    {
        if (facts == null || done.Contains(facts) || facts.Datas == null)
            return;
        done.Add(facts);
        if (facts.Data == null) facts.Data = new Dictionary<string, Factobject.Entry>();
        // The stock pick never shows a list's last entry (list[minute % (count - 1)]): the pack's go in front of it.
        Factobject.Entry last = facts.Datas.Count > 0 ? facts.Datas[facts.Datas.Count - 1] : null;
        if (last != null) facts.Datas.RemoveAt(facts.Datas.Count - 1);
        for (int i = 0; i < Lines.Length; i++)
        {
            Factobject.Entry e = new Factobject.Entry();
            e.Name = "RW_Tip_" + i;
            e.Desc = Lines[i];
            e.Unlockconditions = new List<Blockage.Tags.Bl_Tags.Entrycompare>();
            Blockage.Tags.Bl_Tags.Entrycompare gate = default(Blockage.Tags.Bl_Tags.Entrycompare);
            gate.Item = SeenKey;
            gate.Compare = Blockage.Tags.Bl_Tags.Comparetype.Exists;
            gate.Valuestring = "";
            gate.Value = 0f;
            e.Unlockconditions.Add(gate);
            facts.Datas.Add(e);
            facts.Data[e.Name] = e;
            Added++;
        }
        if (last != null) facts.Datas.Add(last);
    }

    public static void Boot()
    {
        Add(RWUtil.Item<Factobject>("Other", "Earlygame_Funfacts"));
    }

    // Every machine: the lists a scene brings. The database's list may also only appear with its load group.
    public static void Update()
    {
        if (Time.unscaledTime < nextLook)
            return;
        nextLook = Time.unscaledTime + 2f;
        if (Tds_Pausemenu.Instance != null) Add(Tds_Pausemenu.Instance.Funfacts);
        Mainmenu_Main menu = RWMenu.OnMainMenu ? UnityEngine.Object.FindObjectOfType<Mainmenu_Main>() : null;
        if (menu != null) Add(menu.Funfacts);
        Factobject stock = RWUtil.Load<Factobject>("Other", "Earlygame_Funfacts");
        if (stock != null) Add(stock);
    }

    // A campaign began on this machine (host), or the boat shows a chart (a friend): their tips are on from now on.
    public static void MarkSailed()
    {
        if (RWUtil.Has(RWData.SaveFolder, SeenKey))
            return;
        RWUtil.WriteString(RWData.SaveFolder, SeenKey, "1");
        RWUtil.Flush();
    }
}
