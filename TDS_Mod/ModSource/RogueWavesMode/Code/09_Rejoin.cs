// Rejoining a running map (docs/REJOIN.md). The base game shuts the Steam lobby for the whole map (kit docs/15 §14.5),
// so a player whose connection drops can't get back in, although the game keeps their vessel and towers (owned by
// their Sessionid, which a rejoin gets back) and hands them back on return (Game_Players.Playerconnected). In every
// map, in any mode, while the pack is on:
//   host       keeps a seat for each player who was in the map when it started, with their vessel, team and aura
//              (copied four times a second). While a seat is empty, keeps the lobby open (the game's own loading
//              screen shuts it again, so this checks every second); shuts it once everyone is back or the map ends.
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
    static float nextCopy, nextDoor;
    static readonly List<long> turnedAway = new List<long>();

    static void Host()
    {
        Game_Mapvars mv = Game_Mapvars.Instance;
        if (mv != hostMap)
        {
            // A new map, or back on the boat. The boat's own scene change opens the lobby (Tds_Multiplayer_Remote),
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

        if (now < nextDoor)
            return;
        nextDoor = now + 1f;
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
    static bool inMap, dropped;
    static string hostName = "";
    public static bool Late { get { return late; } }

    static void Client()
    {
        Game_Mapvars mv = Game_Mapvars.Instance;
        if (mv != clientMap)
        {
            clientMap = mv;
            sorted = late = slowed = false;
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
            RogueWavesModeMod.Instance.StartCoroutine(CatchUp());
        }
        if (!late)
            return;
        AskHeroes();
        if (slowed && Game_Client_Init.Instance != null && Time.timeScale != 0f)
            Time.timeScale = mv.Time_Scale.Value * (mv.Time_Speedup.Value ? 1.5f : 1f);
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

    static IEnumerator CatchUp()
    {
        float t = 0f;
        while (t < 180f && !LocalLoaded()) { t += Time.unscaledDeltaTime; yield return null; }
        Game_Client_Init ci = Game_Client_Init.Instance;
        if (!LocalLoaded() || ci == null) { RWLog.Warn("rejoin: the map didn't finish loading here after " + RWUtil.F(t) + " s"); yield break; }
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
        if (!plan.StartsWith(RWData.PlanPrefix) || !RWWorld.Parse(info.World.Value.ToString(), out seed, out route))
            return RWChart.Calm;
        foreach (RWChart.Node n in RWWorld.Choices(RWWorld.Chart(seed), route))
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

    // While in a running map, remembers the host's name; if the connection ends before the map does, leaves a line
    // for the main menu's error box, which shows the last one (Bla_Ui_Multiplayererrornotif). The game adds its own
    // "Disconnected" when the client stops, after the Serverclosed callback, so the line is added a frame later.
    static void Watch(Game_Mapvars mv)
    {
        bool connected = Unity.Netcode.NetworkManager.Singleton != null && Unity.Netcode.NetworkManager.Singleton.IsClient && Blm_Server.Instance != null;
        if (inMap && (dropped || !connected))
        {
            inMap = dropped = false;
            string host = hostName != "" ? hostName : "the host";
            if (Blm_Multiplayer.Instance != null && Blm_Multiplayer.Instance.Multiplayererror != null)
                Blm_Multiplayer.Instance.Multiplayererror.Add("Lost the map. If " + host + " is still playing: Steam friends, right-click " + host + ", Join Game.");
            RWLog.Info("rejoin: left a running map (host " + host + ")");
            return;
        }
        if (mv == null || !connected)
        {
            if (Macro_Main.Instance != null) inMap = false;
            return;
        }
        string state = mv.Gamecomplete.Value.ToString();
        inMap = state == "Running" || state == "";
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
