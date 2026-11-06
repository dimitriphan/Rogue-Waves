// Rejoining a running map (docs/mode/REJOIN.md). The base game shuts the Steam lobby for the whole map (kit docs/15 §14.5),
// so a player whose connection drops can't get back in, although the game keeps their vessel and towers (owned by
// their Sessionid, which a rejoin gets back) and hands them back on return (Game_Players.Playerconnected). In every
// map, in any mode, while the pack is on:
//   host       keeps a seat for each player who was in the map when it started, with their vessel, team and aura
//              (copied four times a second). While a seat is empty, keeps the lobby open (the game's own loading
//              screen shuts it again, so this checks every frame); shuts it once everyone is back or the map ends.
//              A returning player gets their seat back; anyone who wasn't in the map is turned away (they join on the
//              boat). Says in chat who dropped and who is back.
//   rejoiner   the game loads the running map and its things on its own (Game_Client_Init), but only the loading
//              screen starts the camera, HUD and game speed (Game_Load -> Startvisuals), and a rejoin skips that
//              scene: starts them itself; loads the vessels' groups; loads this map's sea weather objects, which
//              the others loaded on the boat.
//   dropped    back on the main menu, the game's error line says how to come back.
public static class RWRejoin
{
    public class Seat
    {
        public long Session;
        public string Name = "";
        public string Hero = "";
        public int Team = 1;
        public int Active = 1;                    // 1 if they played the map (Activeplayer above 0)
        public readonly List<Link_Resource> Aura = new List<Link_Resource>();
        public Tds_Playerdatatojson Stats;        // the end screen's numbers, kept by reference across the drop
        public string StatsJson = "";
        public Tds_Clientinfo Info;               // the one seen last: a rejoin brings a new one
        public bool Away;
        public float AwaySince;
    }

    public const string Sender = "Rogue Waves";
    public static readonly Dictionary<long, Seat> Seats = new Dictionary<long, Seat>();
    public static bool DoorOpen;                  // the host opened the lobby for an empty seat
    public static int Restored, Turned;           // this map, for the self-test and the log

    // ---------------------------------------------------------------- every machine, every frame

    public static void Update()
    {
        if (!hooked && Blm_Server.Instance != null)
        {
            hooked = true;
            Blm_Server.Serverclosed = (Blm_Server.Servercloseddel)Delegate.Combine(Blm_Server.Serverclosed, new Blm_Server.Servercloseddel(Closed));
        }
        if (RWUtil.IsHost) Host();
        else Client();
    }

    static bool hooked;

    // ---------------------------------------------------------------- the host

    static Game_Mapvars hostMap;
    static bool hostRunning;
    static float nextCopy;
    static readonly List<long> turnedAway = new List<long>();

    static void Host()
    {
        Game_Mapvars mv = Game_Mapvars.Instance;
        if (mv != hostMap)
        {
            // A new map (on the boat, Instance is still the old map's destroyed object, which reads as null). The boat's own scene change opens the lobby (Tds_Multiplayer_Remote),
            // so a door left open here is not shut after the map is gone.
            hostMap = mv;
            Seats.Clear();
            turnedAway.Clear();
            hostRunning = DoorOpen = false;
            Restored = Turned = 0;
        }
        if (mv == null || Game_Init.Instance == null || Blm_Server.Instance == null || Blm_Multiplayer.Instance == null)
            return;
        string state = mv.Gamecomplete.Value.ToString();
        bool running = Game_Init.Instance.Gameloaded.Value && (state == "Running" || state == "");
        if (!running)
        {
            if (hostRunning)
            {
                hostRunning = false;
                if (DoorOpen) Door(false, "the map ended");
            }
            return;
        }
        float now = Time.realtimeSinceStartup;
        if (!hostRunning)
        {
            hostRunning = true;
            foreach (KeyValuePair<long, GameObject> kv in Blm_Server.Instance.Sessionids)
            {
                Tds_Clientinfo info = kv.Value != null ? kv.Value.GetComponent<Tds_Clientinfo>() : null;
                if (info == null) continue;
                Seat seat = new Seat { Session = kv.Key, Info = info, Active = info.Activeplayer.Value > 0 ? 1 : 0 };
                Copy(seat, info, Player(kv.Key));
                Seats[kv.Key] = seat;
            }
            RWLog.Info("rejoin: " + Seats.Count + " seats kept for this map");
        }

        bool copy = now >= nextCopy;
        if (copy) nextCopy = now + 0.25f;
        // A copy: turning someone away disconnects them, which takes them out of Sessionids.
        foreach (KeyValuePair<long, GameObject> kv in new List<KeyValuePair<long, GameObject>>(Blm_Server.Instance.Sessionids))
        {
            Tds_Clientinfo info = kv.Value != null ? kv.Value.GetComponent<Tds_Clientinfo>() : null;
            if (info == null || !info.IsSpawned) continue;
            Seat seat;
            if (!Seats.TryGetValue(kv.Key, out seat))
            {
                TurnAway(kv.Key);
                continue;
            }
            if (seat.Info != info)
            {
                Restore(info, seat);
                seat.Info = info;
                seat.Away = false;
                Restored++;
                string name = Name(kv.Key);
                if (name != "") seat.Name = name;
                Say(seat.Name + " is back.");
                RWLog.Info("rejoin: " + seat.Name + " (session " + kv.Key + ") is back after " + RWUtil.F(now - seat.AwaySince) + " s: " + seat.Hero + ", team " + seat.Team + ", " + Aura(seat.Aura));
            }
            else if (copy)
                Copy(seat, info, Player(kv.Key));
        }
        // Someone turned away is forgotten once they are gone, so a second Join Game turns them away again (the game
        // gives them the same Sessionid, and a remembered one was let in, seatless).
        turnedAway.RemoveAll(s => !Blm_Server.Instance.Sessionids.ContainsKey(s));
        foreach (Seat seat in Seats.Values)
        {
            if (seat.Away || Blm_Server.Instance.Sessionids.ContainsKey(seat.Session))
                continue;
            seat.Away = true;
            seat.AwaySince = now;
            string host = Name(HostSession());
            Say(seat.Name + " lost the connection. Their vessel and towers wait for them: in Steam, right-click " + (host != "" ? host : "the host") + " and pick Join Game.");
            RWLog.Info("rejoin: " + seat.Name + " (session " + seat.Session + ") dropped, holding " + seat.Hero + ", " + Aura(seat.Aura));
        }

        // Checked every frame: the game's own loading screen shuts the lobby when it ends (Game_Load at "Loading 90%",
        // 10 s after the fly-in, PC round 5), so a door opened early in a map is reopened on the next frame.
        bool want = false;
        foreach (Seat seat in Seats.Values) if (seat.Away) want = true;
        if (want && !Blm_Multiplayer.Instance.Server_Serveropen) Door(true, "a seat is empty");
        else if (!want && DoorOpen) Door(false, "everyone is here");
    }

    // The lobby's door: the game's own switch (Steam joinable + connection approval), public and string-only, so it
    // needs no Steamworks type, which mods can't reference. Without a Steam lobby it does nothing, so approval is set
    // here as well.
    static void Door(bool open, string why)
    {
        try { Blm_Multiplayer_Remote.Instance.Changelobbystatus_ServerOnly(open ? "Open" : "Closed"); }
        catch (Exception e) { RWLog.Warn("rejoin: lobby " + (open ? "open" : "shut") + ": " + e.Message); }
        Blm_Multiplayer.Instance.Server_Serveropen = open;
        if (DoorOpen != open) RWLog.Info("rejoin: lobby " + (open ? "opened" : "shut") + " (" + why + ")");
        DoorOpen = open;
    }

    public static void Copy(Seat seat, Tds_Clientinfo info, Blm_Player player)
    {
        string hero = info.Hero.Value.ToString();
        if (hero != "") seat.Hero = hero;
        seat.Team = info.Team.Value;
        if (info.Activeplayer.Value > 0) seat.Active = 1;
        seat.Aura.Clear();
        foreach (Link_Resource r in info.Resources) seat.Aura.Add(r);
        if (info.Activeplayerdata != null) seat.Stats = info.Activeplayerdata;
        seat.StatsJson = info.Playerstatsjson.Value.ToString();
        if (player != null) seat.Name = player.Displayname.Value.ToString();
    }

    // A rejoin's clientinfo starts empty: no vessel name, Activeplayer 0 (so shared pay skips them), no aura
    // (Tds_Clientinfo.cs:25,37). Gives their seat back; their things come back on their own (Game_Players).
    public static void Restore(Tds_Clientinfo info, Seat seat)
    {
        if (info.Team.Value != seat.Team) info.Team.Value = seat.Team;
        if (seat.Hero != "") info.Hero.Value = new FixedString64Bytes(seat.Hero);
        if (seat.Active > 0) info.Activeplayer.Value = 1;   // before the stats: a change to 0 resets them
        if (seat.Stats != null) info.Activeplayerdata = seat.Stats;
        if (seat.StatsJson != "") info.Playerstatsjson.Value = new FixedString512Bytes(seat.StatsJson);
        info.Resources.Clear();
        foreach (Link_Resource r in seat.Aura) info.Resources.Add(r);
    }

    // Someone who wasn't in the map: the game would give them no vessel and no seat, so they join on the boat.
    static void TurnAway(long session)
    {
        if (session == HostSession() || turnedAway.Contains(session))
            return;
        Blm_Player player = Player(session);
        if (player == null)
            return;
        turnedAway.Add(session);
        Turned++;
        string name = player.Displayname.Value.ToString();
        Say(name + " tried to join mid-map. New players join on the boat.");
        RWLog.Info("rejoin: turned away " + name + " (session " + session + "): not in this map");
        Blm_Multiplayer.Instance.Kickclient(player.Clientid);
    }

    // A line in the game's chat on every machine (the list is the host's to write); it shows for a moment and fades.
    public static bool Say(string text)
    {
        Blm_Ui_Chat chat = Blm_Ui_Chat.Instance;
        if (chat == null || chat.Chat == null || !chat.IsSpawned)
        {
            RWLog.Info("rejoin (no chat): " + text);
            return false;
        }
        Blm_Ui_Chat.Link_Chatcontainer line = default(Blm_Ui_Chat.Link_Chatcontainer);
        line.Sender = new FixedString64Bytes(Sender);
        line.Text = new FixedString512Bytes(text.Length > 200 ? text.Substring(0, 200) : text);
        chat.Chat.Add(line);
        return true;
    }

    static Blm_Player Player(long session)
    {
        Blm_Player p;
        return Blm_Server.Instance != null && Blm_Server.Instance.Sessionidsplayer.TryGetValue(session, out p) ? p : null;
    }

    static string Name(long session)
    {
        Blm_Player p = Player(session);
        return p != null ? p.Displayname.Value.ToString() : "";
    }

    static long HostSession()
    {
        Blm_Player me = Blm_Server.Instance != null ? Blm_Server.Instance.Localplayer : null;
        return me != null ? me.Sessionid.Value : -1;
    }

    static string Aura(List<Link_Resource> aura)
    {
        string s = "";
        foreach (Link_Resource r in aura) s += (s == "" ? "" : ", ") + r.Name + " " + RWUtil.F(r.Amount);
        return s == "" ? "no aura" : s;
    }

    // ---------------------------------------------------------------- the rejoiner (and every client)

    static Game_Mapvars clientMap;
    static bool sorted, late, slowed;
    static readonly List<string> heroesAsked = new List<string>();
    static bool inMap, dropped, played;
    static string hostName = "";
    public static bool Late { get { return late; } }

    static void Client()
    {
        Game_Mapvars mv = Game_Mapvars.Instance;
        if (mv != clientMap)
        {
            clientMap = mv;
            sorted = late = slowed = played = false;
            heroesAsked.Clear();
        }
        Watch(mv);
        if (mv == null)
            return;
        Game_Init init = Game_Init.Instance;
        if (!sorted)
        {
            // An in-scene object's values arrive with its spawn. At the first players' spawn the map can't be loaded
            // yet (Gameloaded waits for every client to load it), so a map already loaded here is a rejoin.
            if (init == null || !init.IsSpawned)
                return;
            sorted = true;
            late = init.Gameloaded.Value;
            if (!late)
                return;
            int w = MapWeather();
            RWLog.Info("rejoin: joined a running map (Wave " + mv.Wave.Value + ", weather " + RWWeather.Display(w) + "), catching up");
            if (w != RWChart.Calm) RWWeather.Preload(w);
            RogueWavesModeMod.Instance.StartCoroutine(InMap(CatchUp()));
        }
        if (!late)
            return;
        AskHeroes();
        if (slowed && Game_Client_Init.Instance != null && Time.timeScale != 0f)
            Time.timeScale = RWWeather.GameSpeed(mv);
    }

    // The game loads every player's vessel group before the map (Game_Client_Init.Initalize), but a rejoiner's own
    // vessel name only arrives when the host gives the seat back, which can be after that loop; and a player object
    // that spawns late in the join misses it too. Loads each one once (a loaded group is not loaded again).
    static void AskHeroes()
    {
        if (Blm_Server.Instance == null)
            return;
        foreach (GameObject p in Blm_Server.Instance.Players)
        {
            Tds_Clientinfo info = p != null ? p.GetComponent<Tds_Clientinfo>() : null;
            string hero = info != null ? info.Hero.Value.ToString() : "";
            if (hero == "" || heroesAsked.Contains(hero))
                continue;
            heroesAsked.Add(hero);
            Heroentryobject entry = RWUtil.Item<Heroentryobject>("Hero", hero);
            if (entry == null || entry.Heroloads == null)
                continue;
            try { Bl_Address.Load(entry.Heroloads.Objectname); RWLog.Info("rejoin: loading " + hero + " (" + entry.Heroloads.Objectname + ")"); }
            catch (Exception e) { RWLog.Warn("rejoin: loading " + hero + ": " + e.Message); }
        }
    }

    // A rejoin's whole map arrives in one go, and this player's own object sets Blm_Server.Localplayer a frame or more after
    // the rest (Blm_Player.OnNetworkSpawn). Everything the map's scene starts on a client reads that field at once and dies on
    // its null: each entity's client half (no look, no state, ever), Game_Client_Init.Initalize just as it would say this
    // player has loaded, Game_Artifacts.Init and Game_Endscreen.Initalize (seen on the PC with a real second player, 2026-11-04;
    // pitfall #139). A normal map load never meets this: the player object has been there since the boat.
    static bool initDied, artifactsDied, endDied;
    public static int ClientsRestarted;      // self-test

    public static void OnLog(string condition, string stack, LogType type)
    {
        if (type != LogType.Exception || string.IsNullOrEmpty(stack) || RWUtil.IsHost)
            return;
        if (stack.Contains("Game_Client_Init.Initalize")) initDied = true;
        else if (stack.Contains("Game_Artifacts.Init")) artifactsDied = true;
        else if (stack.Contains("Game_Endscreen.Initalize")) endDied = true;
    }

    // What died gets its turn again, now that the player object is here.
    static void Repair()
    {
        Tds_Clientinfo me = Local();
        if (initDied && me != null && !me.Loaded.Value)
        {
            me.Loaded.Value = true;
            try { Game_Rounds.Instance.Startgameclient(Game_Mapvars.Instance.Mode); }
            catch (Exception e) { RWLog.Warn("rejoin: Startgameclient: " + e.Message); }
        }
        if (artifactsDied)
        {
            Game_Artifacts row = UnityEngine.Object.FindObjectOfType<Game_Artifacts>();
            if (row != null) row.SendMessage("Init", SendMessageOptions.DontRequireReceiver);
        }
        if (endDied)
        {
            Game_Endscreen end = UnityEngine.Object.FindObjectOfType<Game_Endscreen>();
            if (end != null) end.SendMessage("Initalize", SendMessageOptions.DontRequireReceiver);
        }
        // R4: the list read "the map's load the Cardinal row the end screen" (PC round 4, the friend's log): commas between.
        List<string> stopped = new List<string>();
        if (initDied) stopped.Add("the map's load");
        if (artifactsDied) stopped.Add("the Cardinal row");
        if (endDied) stopped.Add("the end screen");
        RWLog.Info("rejoin: started again what the late player object stopped: " + (stopped.Count > 0 ? string.Join(", ", stopped.ToArray()) : "nothing"));
        initDied = artifactsDied = endDied = false;
    }

    // A client half sets its Master once the local player has loaded, a frame after it looks. So two frames after that, a
    // half still without one is one the late player object stopped; a live one still waiting must not start twice (two
    // of every look and handler, MODE-B2).
    // Only halves already without a Master when the player loaded count: one that came in the two frames since is still in
    // its own Start, and starting it again gives it two of everything (MAX P1-3).
    static List<Entity_Master_Client> Stopped()
    {
        List<Entity_Master_Client> list = new List<Entity_Master_Client>();
        foreach (Entity_Master_Client c in UnityEngine.Object.FindObjectsOfType<Entity_Master_Client>())
            if (c.Master == null) list.Add(c);
        return list;
    }

    static void RestartStopped(List<Entity_Master_Client> atLoad)
    {
        int clients = 0;
        foreach (Entity_Master_Client c in atLoad)
            if (c != null && c.Master == null)
            {
                c.StartCoroutine("Start");
                clients++;
            }
        ClientsRestarted += clients;
        // BATCH 09_Rejoin:374: the line read "started again the client halves the late player object stopped".
        RWLog.Info("rejoin: started again the client halves that the late player object stopped: " + clients + " entities");
    }

    // The same arrival can outrun the load groups: a client half that asks for its being before its group is in gets null and
    // dies on it a few lines on, with its Master set and no look or state for good (116 things on a rejoin after a restart,
    // 2026-11-04; pitfall #141). One still without its being on two looks a second apart, with its group in on both, runs again.
    // A live one waits on the game's flags for a frame or two, which the two looks leave alone.
    static IEnumerator Looks()
    {
        List<Entity_Master_Client> seen = new List<Entity_Master_Client>();
        List<string> beings = new List<string>();
        int started = 0;
        for (int i = 0; i < 90; i++)
        {
            yield return new WaitForSecondsRealtime(1f);
            List<Entity_Master_Client> now = new List<Entity_Master_Client>();
            foreach (Entity_Master_Client c in UnityEngine.Object.FindObjectsOfType<Entity_Master_Client>())
            {
                if (c.Master == null || c.Beingobject != null || !c.Master.Clientready.Value || c.Master.Being.Value.ToString() == "")
                    continue;
                // Its group must have been in on the last look too: a run that waits on the group dies as it arrives, and
                // started again in between, both runs would go through (MODE-B2).
                if (Blockage.Address.Bl_Address.Getload(c.Master.Being.Value) == null)
                    continue;
                if (!seen.Contains(c))
                {
                    now.Add(c);
                    continue;
                }
                // Its first run got as far as this list.
                if (Game_Entity.Instance != null)
                    Game_Entity.Instance.Entityclient.Remove(c.Master);
                string being = c.Master.Being.Value.ToString();
                if (!beings.Contains(being))
                    beings.Add(being);
                c.StartCoroutine("Start");
                started++;
            }
            seen = now;
        }
        ClientsRestarted += started;
        if (started > 0)
            // BATCH 09_Rejoin:416: the line read "gave N things their look once their groups had loaded".
            RWLog.Info("rejoin: started again " + started + " things that had no look, once their load groups were in: " + string.Join(", ", beings.ToArray()));
    }

    // The catch-up and the looks end with their map: a player turned away goes on to the main menu and may load the next map
    // before the host starts it, where a catch-up left running would start the fly-in under the loading screen (MODE-B3).
    // A destroyed map reads == null while Game_Mapvars.Instance still holds it (kit 1.7), so that ends it too.
    static IEnumerator InMap(IEnumerator run)
    {
        Game_Mapvars map = Game_Mapvars.Instance;
        while (map != null && ReferenceEquals(Game_Mapvars.Instance, map) && run.MoveNext())
            yield return run.Current;
    }

    static IEnumerator CatchUp()
    {
        float t = 0f;
        while (t < 90f && (Blm_Server.Instance == null || Blm_Server.Instance.Localplayer == null)) { t += Time.unscaledDeltaTime; yield return null; }
        yield return new WaitForSecondsRealtime(1f);
        Repair();
        RogueWavesModeMod.Instance.StartCoroutine(InMap(Looks()));
        t = 0f;
        while (t < 180f && !LocalLoaded()) { t += Time.unscaledDeltaTime; yield return null; }
        Game_Client_Init ci = Game_Client_Init.Instance;
        if (!LocalLoaded() || ci == null) { RWLog.Warn("rejoin: the map didn't finish loading here after " + RWUtil.F(t) + " s"); yield break; }
        List<Entity_Master_Client> atLoad = Stopped();
        yield return null;
        yield return null;
        RestartStopped(atLoad);
        yield return new WaitForSecondsRealtime(0.5f);
        try
        {
            ci.Setintroposition();
            ci.Startvisuals(false);
            RWLog.Info("rejoin: map loaded here after " + RWUtil.F(t) + " s; camera, HUD and game speed started");
        }
        catch (Exception e) { RWLog.Warn("rejoin: Startvisuals: " + e.Message); }
        yield return new WaitForSecondsRealtime(15f);
        if (ci != null && !ci.Gametimescale)
            Unstick();
    }

    // Startvisuals never finished: free the camera and the HUD, and follow the game speed from here (its own hook is
    // private).
    static void Unstick()
    {
        Game_Cameracontroller cam = Game_Cameracontroller.Instance;
        if (cam != null) cam.Movelock = cam.Zoomlock = cam.Rotationlock = false;
        try { Blockage.Menu.Bl_Menus.Closemenu("Cine", true); } catch (Exception) { }
        if (Game_Client_Init.Instance != null) Game_Client_Init.Instance.Gametimescale = true;
        slowed = true;
        RWLog.Warn("rejoin: the intro never finished; camera and HUD freed by hand");
    }

    // This map's sea weather from the names every machine has: the chart's world (seed and route sailed) and the
    // Gameplan, whose twin is one of the next row's choices.
    public static int MapWeather()
    {
        Tds_Multiplayerinfo info = Tds_Multiplayerinfo.Instance;
        if (info == null)
            return RWChart.Calm;
        string plan = info.Gameplan.Value.ToString();
        uint seed;
        List<int> route;
        int retries;
        bool stops;
        if (!plan.StartsWith(RWData.PlanPrefix) || !RWWorld.Parse(info.World.Value.ToString(), out seed, out route, out retries, out stops))
            return RWChart.Calm;
        foreach (RWChart.Node n in RWWorld.Choices(RWWorld.Chart(seed, stops), route))
            if (RWMode.Twin(n.Plan) == plan)
                return n.Weather;
        return RWChart.Calm;
    }

    static Tds_Clientinfo Local()
    {
        Blm_Player me = Blm_Server.Instance != null ? Blm_Server.Instance.Localplayer : null;
        return me != null ? me.GetComponent<Tds_Clientinfo>() : null;
    }

    static bool LocalLoaded()
    {
        Tds_Clientinfo me = Local();
        return me != null && me.Loaded.Value && Game_Client_Init.Instance != null;
    }

    // ---------------------------------------------------------------- the dropped player

    // While in a running map, remembers its host's name (forgotten when the map ends or is left, so a later map's host is
    // named right); if the connection ends before the map does, leaves a line for the main menu's error box, which shows
    // the last one (Bla_Ui_Multiplayererrornotif). The game adds its own "Disconnected" when the client stops, after the
    // Serverclosed callback, so the line is added a frame later.
    static void Watch(Game_Mapvars mv)
    {
        bool connected = Unity.Netcode.NetworkManager.Singleton != null && Unity.Netcode.NetworkManager.Singleton.IsClient && Blm_Server.Instance != null;
        if (inMap && (dropped || !connected))
        {
            inMap = dropped = false;
            string host = hostName != "" ? hostName : "the host";
            hostName = "";
            if (Blm_Multiplayer.Instance != null && Blm_Multiplayer.Instance.Multiplayererror != null)
                Blm_Multiplayer.Instance.Multiplayererror.Add("Lost the map. If " + host + " is still playing: Steam friends, right-click " + host + ", Join Game.");
            RWLog.Info("rejoin: left a running map (host " + host + ")");
            return;
        }
        if (mv == null || !connected)
        {
            if (Macro_Main.Instance != null) inMap = false;
            if (!inMap) hostName = "";
            return;
        }
        // Only once this player is in the map's play (Activeplayer, given back to a rejoiner by Restore): a player the host
        // turns away has the map's objects for a moment too, and would be told to Join Game again (MODE-B4).
        Tds_Clientinfo me = Local();
        if (me != null && me.Activeplayer.Value > 0) played = true;
        string state = mv.Gamecomplete.Value.ToString();
        inMap = played && (state == "Running" || state == "");
        // The host of this map only: a later map may be someone else's.
        if (!inMap) hostName = "";
        if (inMap && hostName == "" && Blm_Server.Instance != null)
            foreach (GameObject p in Blm_Server.Instance.Players)
            {
                Blm_Player bp = p != null ? p.GetComponent<Blm_Player>() : null;
                if (bp != null && bp.Clientid == 0) hostName = bp.Displayname.Value.ToString();
            }
    }

    static void Closed(bool host)
    {
        if (!host && inMap) dropped = true;
    }
}
