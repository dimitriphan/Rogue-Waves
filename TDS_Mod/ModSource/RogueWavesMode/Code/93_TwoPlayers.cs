#if ROGUEWAVESMODE_TWOPC
// Debug builds only (-Define ROGUEWAVESMODE_SELFTEST,ROGUEWAVESMODE_TWOPC; Tools/pc/Run-TwoPlayers.ps1): a real second player on
// one PC. A second copy of the game joins the first over the game's own UnityTransport on the loopback address instead of a
// Steam lobby, so everything past the join is the same netcode a friend's machine runs: the same RPCs, NetworkVariables and
// scene sync, with a real client that only sees what syncs (kit docs/19 §8's N1-N14, docs/mode/REJOIN.md's O1-O7). What this
// can't show: Steam's own join path and a real network's delays.
// "-rwclient" on the command line makes this copy the second player; it never writes the save, so it can't overwrite the host's.
// "-rwphase <word>" says why a second player was started again; the host asks for that through its log (TEST DRIVER ...).
public static class RWTwo
{
    public static readonly bool Second = HasArg("-rwclient");
    public static readonly bool NoPack = HasArg("-rwnopack");
    public static readonly bool Skip = HasArg("-rwskip");
    public static readonly bool Short = HasArg("-rwshort");   // a pair of vessels: to the first rejoin and no further
    public static readonly bool Review = HasArg("-rwreview"); // after that rejoin, the code review's online checks (93_Review.cs)
    public static readonly string Phase = Arg("-rwphase", "");
    public static readonly string HostHero = Arg("-rwhosthero", "Hero_Smacky");
    public static readonly string SecondHero = Arg("-rwsecondhero", "Hero_Yossele");
    public const string SecondGuid = "7e57c11e-0000-4000-8000-000000000002", StrangerGuid = "7e57c11e-0000-4000-8000-000000000003";
    public static string Guid { get { return Phase == "stranger" ? StrangerGuid : SecondGuid; } }
    const ushort Port = 27777;
    static Unity.Netcode.Transports.UTP.UnityTransport utp;
    static bool quiet;
    static readonly List<string> heard = new List<string>();
    public static readonly List<string> ChatSeen = new List<string>();

    static bool HasArg(string a) { return Array.IndexOf(Environment.GetCommandLineArgs(), a) >= 0; }

    static string Arg(string name, string fallback)
    {
        string[] all = Environment.GetCommandLineArgs();
        int i = Array.IndexOf(all, name);
        return i >= 0 && i + 1 < all.Length ? all[i + 1] : fallback;
    }

    // Every frame: the game's transport is swapped for the loopback one whenever nothing is connected.
    static bool chartForced;

    public static void Update()
    {
        Listen();
        // A restarted copy has lost the test's forced weather, and must read the same chart as the host before it sorts out the rejoin.
        uint chartSeed;
        List<int> chartRoute;
        if (Phase == "rejoin" && !chartForced && Tds_Multiplayerinfo.Instance != null && RWWorld.Parse(Tds_Multiplayerinfo.Instance.World.Value.ToString(), out chartSeed, out chartRoute))
        {
            chartForced = true;
            RWTest.ForceWeather(chartSeed);
        }
        Unity.Netcode.NetworkManager nm = Unity.Netcode.NetworkManager.Singleton;
        Lobby();
        if (nm == null || nm.NetworkConfig == null || nm.IsListening)
            return;
        if (utp == null)
        {
            utp = nm.gameObject.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
            if (utp == null) utp = nm.gameObject.AddComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
            utp.SetConnectionData("127.0.0.1", Port, "127.0.0.1");
            RWLog.Info("TEST INFO two players: loopback transport ready, this copy is the " + (Second ? "second player" : "host") + ", phase '" + Phase + "', args " + string.Join(" ", Environment.GetCommandLineArgs()));
        }
        if (nm.NetworkConfig.NetworkTransport != utp)
        {
            nm.NetworkConfig.NetworkTransport = utp;
            utp.enabled = true;
        }
        if (Second && !quiet && Blockage.Tags.Bla_Data.Instance != null)
        {
            quiet = true;
            Blockage.Tags.Bla_Data.Instance.Saveactive = false;
        }
    }

    // The game's own "lobby open / closed" reaches the approval flag only through a Steam lobby, and this round has none. So the
    // host writes it where the game's calls do: open on the boat, shut when a map has loaded (RWRejoin opens it again for a seat).
    static bool mapWasLoaded;

    static void Lobby()
    {
        if (Second || !RWUtil.IsHost || Blm_Multiplayer.Instance == null)
            return;
        if (Macro_Main.Instance != null && Macro_Main.Instance.Ready)
            Blm_Multiplayer.Instance.Server_Serveropen = true;
        bool loaded = Game_Init.Instance != null && Game_Init.Instance.IsSpawned && Game_Init.Instance.Gameloaded.Value;
        if (loaded && !mapWasLoaded)
            Blm_Multiplayer.Instance.Server_Serveropen = false;
        mapWasLoaded = loaded;
    }

    // RWIdentity's place on the second copy: it has its own id, which the shared save can't hold.
    public static bool Identity()
    {
        if (!Second)
            return false;
        if (Blockage.Tags.Bla_Data.Instance == null || Blockage.Tags.Bla_Data.Playerdata == null || !Blockage.Tags.Bla_Data.Playerdata.Loaded)
            return true;
        if (RWUtil.ReadString("Metadata", "Player_Guid") != Guid)
            RWUtil.WriteString("Metadata", "Player_Guid", Guid);
        RWIdentity.Guid = Guid;
        return true;
    }

    // Blm_Multiplayer.Startclientsteam, less Steam (its SteamId type isn't one a mod can name): the join scene, the game's own
    // three callbacks, the fixed payload, StartClient.
    public static IEnumerator Join()
    {
        Blm_Multiplayer mp = Blm_Multiplayer.Instance;
        Unity.Netcode.NetworkManager nm = Unity.Netcode.NetworkManager.Singleton;
        AsyncOperation load = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(mp.Clientjoinscene, UnityEngine.SceneManagement.LoadSceneMode.Single);
        while (!load.isDone) yield return null;
        if (!hooked)
        {
            hooked = true;
            nm.OnClientConnectedCallback += id => mp.SendMessage("Singleton_OnClientConnectedCallback", id, SendMessageOptions.DontRequireReceiver);
            nm.OnClientDisconnectCallback += id => mp.SendMessage("Singleton_OnClientDisconnectCallback", id, SendMessageOptions.DontRequireReceiver);
            nm.OnClientStopped += stuff => mp.SendMessage("Singleton_OnClientLocalCloseCallback", stuff, SendMessageOptions.DontRequireReceiver);
        }
        nm.NetworkConfig.ConnectionData = new byte[] { 78, 111, 110, 101 };   // "None"
        bool ok = nm.StartClient();
        RWLog.Info("TEST INFO two players: StartClient " + ok);
    }
    static bool hooked;

    // What a lost connection does in the game's own code: its disconnect callback, so the client leaves and lands on the main menu.
    // The two copies talk through the game's chat: its list is the host's to write, Sendchat_ServerRpc is any client's.
    public static void Listen()
    {
        Blm_Ui_Chat chat = Blm_Ui_Chat.Instance;
        if (chat == null || chat.Chat == null || !chat.IsSpawned)
            return;
        foreach (Blm_Ui_Chat.Link_Chatcontainer line in chat.Chat)
        {
            string text = line.Text.ToString();
            if (text.StartsWith("TWO "))
            {
                if (!heard.Contains(text)) heard.Add(text);
            }
            else if (!ChatSeen.Contains(text))
                ChatSeen.Add(text);
        }
    }

    public static string HeardStarting(string prefix) { foreach (string s in heard) if (s.StartsWith(prefix)) return s; return null; }

    public static bool Heard(string side, string word) { return heard.Contains("TWO " + side + " " + word); }
    public static bool OtherSaid(string word) { return Heard(Second ? "H" : "S", word); }

    public static bool Tell(string word)
    {
        Blm_Ui_Chat chat = Blm_Ui_Chat.Instance;
        if (chat == null || chat.Chat == null || !chat.IsSpawned)
            return false;
        return RWRejoin.Say("TWO H " + word);
    }

    // The second player's way: in a map, an order on their vessel; on the boat, the chat box as a player types in it (its
    // ServerRpc isn't one a mod can call).
    public static IEnumerator SecondTell(string word)
    {
        float t = 0f;
        while (t < 90f)
        {
            Entity_Master v = RWMine.Vessel();
            if (v != null && UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Game")
            {
                RWMine.Send("Two", 0, word);
                yield break;
            }
            Blm_Ui_Chat chat = Blm_Ui_Chat.Instance;
            if (chat != null && chat.IsSpawned && chat.Inputbox != null && chat.Inputbox.gameObject.activeInHierarchy)
            {
                chat.Inputbox.ActivateInputField();
                float f = 0f;
                while (!chat.Inputbox.isFocused && f < 1.5f) { f += Time.unscaledDeltaTime; yield return null; }
                if (chat.Inputbox.isFocused)
                {
                    chat.Inputbox.text = "TWO S " + word;
                    chat.SendMessage("Sendchat", "Onpressed", SendMessageOptions.DontRequireReceiver);
                    chat.Inputbox.DeactivateInputField();
                    yield break;
                }
                RWLog.Info("TEST INFO two players: the chat box took no focus for '" + word + "'");
            }
            t += 1f;
            yield return new WaitForSecondsRealtime(1f);
        }
        RWLog.Info("TEST INFO two players: no way to say '" + word + "'");
    }

    // Host: an order of kind "Two" is the second player talking.
    public static void Hook()
    {
        RWRun.Order -= OnOrder;
        RWRun.Order += OnOrder;
    }

    static void OnOrder(Entity_Master vessel, RWPlayer p, string json)
    {
        if (RWRun.JsonText(json, "Kind") == "Two")
        {
            string text = "TWO S " + RWRun.JsonText(json, "Text");
            if (!heard.Contains(text)) heard.Add(text);
        }
    }

    public static bool ChatHas(string part) { foreach (string s in ChatSeen) if (s.ToLowerInvariant().Contains(part.ToLowerInvariant())) return true; return false; }

    // A friend without the pack: this copy's mode part stands aside and only joins, so the host's chart maps don't exist here.
    public static IEnumerator NoPackRun()
    {
        RWLog.Info("TEST INFO two players: a friend without the pack");
        while (UnityEngine.Object.FindObjectOfType<Mainmenu_Main>() == null) yield return new WaitForSecondsRealtime(0.5f);
        yield return new WaitForSecondsRealtime(8f);
        yield return Join();
        float t = 0f;
        while (t < 240f)
        {
            t += 0.5f;
            Blm_Server server = Blm_Server.Instance;
            Tds_Clientinfo mine = server != null && server.Localplayer != null ? server.Localplayer.GetComponent<Tds_Clientinfo>() : null;
            if (mine != null && mine.IsOwner && mine.IsSpawned && mine.Mods.Value) mine.Mods.Value = false;
            if (t > 30f && !readied && Tds_Multiplayer_Remote.Instance != null)
            {
                // A boat that can't find the chart's world may never let a player click Ready: say what this machine shows, then ready up as if it did.
                readied = true;
                RWLog.Info("TEST INFO the friend without the pack on the boat: Macro_Main " + (Macro_Main.Instance != null) + ", ready " + (Macro_Main.Instance != null && Macro_Main.Instance.Ready) + ", world " + (Tds_Multiplayerinfo.Instance != null ? Tds_Multiplayerinfo.Instance.World.Value.ToString() : "-"));
#if ROGUEWAVESMODE_SHOTS
                yield return RWShots.Shot("nopack_boat");
#endif
                Tds_Multiplayer_Remote.Instance.Startgame_ServerRpc(true, default(Unity.Netcode.ServerRpcParams));
                RWLog.Info("TEST INFO the friend without the pack readies up");
            }
            if (readied && UnityEngine.Object.FindObjectOfType<Mainmenu_Main>() != null) { RWLog.Info("TEST INFO the friend without the pack is back on the main menu after " + t + " s: " + string.Join(" | ", Blm_Multiplayer.Instance.Multiplayererror.ToArray())); break; }
            yield return new WaitForSecondsRealtime(0.5f);
        }
        RWLog.Info("TEST DONE pass 0, fail 0, setup 0, errors logged 0 (the friend without the pack)");
    }
    static bool readied;
}

public static partial class RWTest
{
    static int PlayersIn() { return Blm_Server.Instance != null && Blm_Server.Instance.Sessionidsplayer != null ? Blm_Server.Instance.Sessionidsplayer.Count : 0; }

    static string PlayerList()
    {
        List<string> all = new List<string>();
        if (Blm_Server.Instance != null)
            foreach (KeyValuePair<long, Blm_Player> kv in Blm_Server.Instance.Sessionidsplayer)
            {
                Tds_Clientinfo info = kv.Value != null ? kv.Value.GetComponent<Tds_Clientinfo>() : null;
                all.Add(kv.Key + "=" + (kv.Value != null ? kv.Value.Displayname.Value.ToString() : "-") + "/" + (info != null ? info.Hero.Value.ToString() + "/mods " + info.Mods.Value + "/loaded " + info.Loaded.Value : "-"));
            }
        return string.Join(", ", all.ToArray());
    }

    static IEnumerator Sync(string word) { return Sync(word, 240f); }

    static IEnumerator Sync(string word, float timeout)
    {
        yield return Say(word);
        yield return Until(() => RWTwo.OtherSaid(word), timeout);
        if (!RWTwo.OtherSaid(word)) Setup("sync " + word, "the other player never said it");
    }

    // On the boat and on an end screen, where the second player has no vessel to speak through: the host leads, the second player follows.
    static IEnumerator OneWay(string word)
    {
        if (!RWTwo.Second) { yield return Say(word); yield break; }
        yield return Until(() => RWTwo.OtherSaid(word), 300f);
        if (!RWTwo.OtherSaid(word)) Setup("following " + word, "the host never said it");
    }

    static IEnumerator Say(string word)
    {
        if (RWTwo.Second) { yield return RWTwo.SecondTell(word); yield break; }
        float t = 0f;
        while (!RWTwo.Tell(word) && t < 90f) { t += Time.unscaledDeltaTime; yield return null; }
        if (t >= 90f) Info("sync", "no chat to say '" + word + "' in");
    }

    static bool InGame() { return UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Game"; }

    static bool LoadedHere()
    {
        Tds_Clientinfo me = Blm_Server.Instance != null && Blm_Server.Instance.Localplayer != null ? Blm_Server.Instance.Localplayer.GetComponent<Tds_Clientinfo>() : null;
        return me != null && me.Loaded.Value && InGame() && Game_Mapvars.Instance != null && Game_Mapvars.Instance.Maploaded.Value;
    }

    static float Wallet(string res)
    {
        Tds_Clientinfo me = Blm_Server.Instance != null && Blm_Server.Instance.Localplayer != null ? Blm_Server.Instance.Localplayer.GetComponent<Tds_Clientinfo>() : null;
        if (me != null)
            foreach (Link_Resource r in me.Resources)
                if (r.Name.ToString() == res) return r.Amount;
        return -1f;
    }

    static long OtherSession()
    {
        foreach (long s in Blm_Server.Instance.Sessionidsplayer.Keys)
            if (s != RWMine.Session) return s;
        return -1;
    }

    // Both machines force the same weather on the chart's first rows: each row's nodes one weather, the rest as rolled.
    public static void ForceWeather(uint seed)
    {
        RWChart.Chart c = RWWorld.Chart(seed);
        for (int m = 0; m < Forced.Length; m++)
            foreach (RWChart.Node n in c.Rows[m + 1]) n.Weather = Forced[m];
    }

    static IEnumerator MenuToCampaign()
    {
        yield return Until(() => UnityEngine.Object.FindObjectOfType<Mainmenu_Main>() != null, 120f);
        Mainmenu_Main menu = UnityEngine.Object.FindObjectOfType<Mainmenu_Main>();
        if (menu == null) { Setup("two players", "never reached the main menu"); yield break; }
        yield return new WaitForSecondsRealtime(8f);
        menu.Idlethreshold = 100000f;
        menu.SendMessage("Transitiontomain", SendMessageOptions.DontRequireReceiver);
        yield return Until(() => RWMenu.Button != null, 25f);
        yield return new WaitForSecondsRealtime(7f);
        Blockage.Menu.Bl_Menus.Changemenu("Main", true);
        yield return new WaitForSecondsRealtime(6f);
        RWMenu.Clicked();
        yield return WaitDialog(10f);
    }

    static IEnumerator TwoRun()
    {
        RWTwo.Hook();
        // The second player takes the objective on map 1's opening board, before P1's Tide 3.
        if (RWTwo.Second) yield return Stepped(BoardFromTide1(TwoSecond()));
        else yield return Stepped(BoardFromTide1(TwoHost()));
    }

    // ---------------------------------------------------------------- the host

    static IEnumerator TwoHost()
    {
        yield return MenuToCampaign();
        bool hadSaved = CardNames().Contains("Continue");
        ClickCard("New");
        if (hadSaved)
        {
            yield return new WaitForSecondsRealtime(1f);
            yield return WaitDialog(8f);
            ClickCard("Sure");
        }
        yield return Until(() => Macro_Main.Instance != null && Macro_Main.Instance.Ready, 120f);
        Macro_Main boat = Macro_Main.Instance;
        if (boat == null || !boat.Ready) { Setup("two players", "no boat"); yield break; }
        boat.Selectcharacter(RWTwo.HostHero);
        yield return Until(() => RWCampaign.Live && Here(), 40f);
        // A chart both machines know: three weathers on the first three maps, so every weather is seen on a client.
        RWCampaign.New(Seed);
        ForceWeather(Seed);
        yield return Until(() => RWCampaign.Live && Here(), 40f);
        Info("two players", "transport " + Unity.Netcode.NetworkManager.Singleton.NetworkConfig.NetworkTransport.GetType().Name + ", listening " + Unity.Netcode.NetworkManager.Singleton.IsListening + ", server open " + Blm_Multiplayer.Instance.Server_Serveropen + ", world " + RWCampaign.WorldName());
        if (RWTwo.Skip)
        {
            // The last phases alone: a campaign two maps along, as a real second player would have left it in the save.
            RWChart.Node at = RWCampaign.Chart.At(0, 0);
            for (int m = 0; m < 2; m++) { RWCampaign.Route.Add(at.Links[0]); at = RWCampaign.Here; }
            RWCampaign.Staging = false;
            RWPlayer other = new RWPlayer { Key = RWTwo.SecondGuid, Name = "gobbie FASTNESS", Hero = RWTwo.SecondHero, Pure = 50 };
            other.Directions.Add(RogueWavesModeKitDirections.Id("Any", "Refit"));
            other.Directions.Add(RogueWavesModeKitDirections.Id("Any", "Refit2"));
            RWRun.Players[other.Key] = other;
            RWRun.For(RWMine.Session).Hero = RWTwo.HostHero;
            RWRun.Save();
            RWCampaign.Choice = RWCampaign.Open()[0].Col;
            RWCampaign.SaveChoice();
            yield return new WaitForSecondsRealtime(3f);
            yield return TwoTail();
            yield break;
        }
        Line("WAITING for the second player");
        yield return Until(() => PlayersIn() >= 2, 400f);
        Check("N1 a second player joins the boat", PlayersIn() >= 2, PlayerList());
        if (PlayersIn() < 2) yield break;

        long me = RWMine.Session, friend = OtherSession();
        Tds_Clientinfo hisInfo = Blm_Server.Instance.Sessionidsplayer[friend].GetComponent<Tds_Clientinfo>();
        yield return Until(() => hisInfo.Hero.Value.ToString() == RWTwo.SecondHero, 90f);
        yield return new WaitForSecondsRealtime(RWLobby.GraceSeconds + 2f);
        Check("N2 the host sees the second player's vessel, and no one is warned about mods", hisInfo.Hero.Value.ToString() == RWTwo.SecondHero && hisInfo.Mods.Value && RWLobby.Warned.Count == 0, PlayerList() + ", warned " + RWLobby.Warned.Count);
        RWChart.Node harbour = RWCampaign.Chart.At(0, 0);
        yield return Until(() => boat.Selected.Value.ToString() == harbour.Name && Macro_Worldvisual.Instance.Insetup, 40f);
        Check("staging: the harbour opens with two players on the boat", boat.Selected.Value.ToString() == harbour.Name, "selected '" + boat.Selected.Value + "'");
        yield return OneWay("harbour");
        boat.Playgame(true);

        // The staging map.
        yield return Until(() => LoadedHere() && RWRun.InCampaignMap, 240f);
        Check("N3 both load the staging map", LoadedHere() && hisInfo != null, PlayerList());
        yield return Until(() => RWAsk.Open.StartsWith(RWVote.DirectionKind), 120f);
        yield return WaitDialog(10f);
        Check("Direction: the host is asked too, with " + RWDirections.Offers + " cards", CardNames().Count == RWDirections.Offers && RWVote.State == "Picks", CardTexts());
        RWPlayer mine = RWRun.For(me), his = RWRun.For(friend);
        Check("each player has their own run, keyed by the id the pack keeps", mine != his && his.Key == RWTwo.SecondGuid && mine.Key != his.Key, "mine " + mine.Key + ", his " + his.Key);
        Info("host's offers", CardTexts());
        ClickCard("0");
        yield return Until(() => mine.Directions.Count >= 1, 10f);
        Check("the host's pick waits for the second player", RWVote.State == "Picks" || his.Directions.Count >= 1, "state '" + RWVote.State + "', his " + his.Directions.Count);
        yield return Until(() => his.Directions.Count >= 1, 40f);
        Check("N5 the second player's Direction reaches the host through their vessel's order", his.Directions.Count == 1 && !his.Owed, string.Join(",", his.Directions.ToArray()));
        Entity_Master hisVessel = RWRun.Vessel(friend);
        Check("each vessel stands, the right one for each player", RWRun.Vessel(me) != null && hisVessel != null && hisVessel.Being.Value.ToString() == RWTwo.SecondHero && RWRun.Vessel(me).Being.Value.ToString() == RWTwo.HostHero,
            "mine " + (RWRun.Vessel(me) != null ? RWRun.Vessel(me).Being.Value.ToString() : "-") + ", his " + (hisVessel != null ? hisVessel.Being.Value.ToString() : "-"));
        yield return Until(() => RWAsk.Open.StartsWith(RWVote.MapKind), 15f);
        yield return WaitDialog(10f);
        List<string> mapCards = CardNames();
        Info("the map vote on the host", CardTexts());
        yield return Sync("vote");
        // A tie: the host takes the first card, the second player the last. The first of the tied maps goes.
        ClickCard(mapCards[0]);
        Macro_Main oldBoat = boat;
        yield return Until(() => RWCampaign.Choice >= 0, 40f);
        Check("Vote: with two voters split, the first of the tied maps wins, not a third", mapCards.Count < 2 || RWCampaign.Choice.ToString() == mapCards[0], "choice " + RWCampaign.Choice + ", " + RWVote.LastVote);
        int want = RWCampaign.Choice;
        yield return Until(() => Macro_Main.Instance != null && Macro_Main.Instance != oldBoat && Macro_Main.Instance.Ready, 150f);
        boat = Macro_Main.Instance;
        Check("staging: both are back on the boat", boat != null && PlayersIn() == 2, PlayerList());
        yield return Until(() => Here(), 30f);
        yield return Until(() => RWBoat.ArrivedAt > 0f, 60f);
        yield return Until(() => boat.Selected.Value.ToString() != "" && Macro_Worldvisual.Instance.Insetup, 20f);
        yield return OneWay("map1");
        boat.Playgame(true);

        // Map 1, Rising Water.
        yield return Until(() => LoadedHere() && RWRun.InCampaignMap && RWCampaign.Playing >= 0, 240f);
        yield return Until(() => RWShop.Stand != null, 120f);
        freeze = true;
        Check("VENDCINT lands with two players: both subowners", RWShop.Stand != null && RWShop.Stand.Entitydata.Value.Subowner != null && RWShop.Stand.Entitydata.Value.Subowner.Length == 2, RWShop.Stand != null ? RWUtil.V(RWShop.Stand.Entityposition.Value) : "none");
        Info("difficulty with two players", "foes x" + RWUtil.F(RWDifficulty.LastScale) + ", health x" + RWUtil.F(1f + RWDifficulty.LastHealth) + " (" + RWDifficulty.LastHoard + " held), modifiers [" + RWDifficulty.LastModifiers + "]");
        Check("Difficulty: a second player adds half as many foes again", Mathf.Abs(RWDifficulty.LastScale - (1f + RWDifficulty.CountPerPlayer)) < 0.001f, "scale " + RWUtil.F(RWDifficulty.LastScale));
        mine = RWRun.For(me); his = RWRun.For(friend);
        mine.Pure = his.Pure = 60;
        Give(me, 1000f); Give(friend, 1000f);
        yield return new WaitForSeconds(1f);
        yield return Sync("shop");

        // The second player's shop.
        yield return Until(() => RWTwo.OtherSaid("bought"), 90f);
        Check("N5 the second player's Cardinal or Trinket is paid for on the host, from their Pure Aura only", his.Pure < 60 && mine.Pure == 60, "his " + his.Pure + ", mine " + mine.Pure + ", last order " + RWShop.LastOrder + ", cardinals " + string.Join(",", RWRun.Cardinals.ToArray()) + ", his Trinkets " + string.Join(",", his.Trinkets.ToArray()));
        yield return Sync("rank");
        yield return Until(() => RWTwo.OtherSaid("rankdone"), 60f);
        int dirs = his.Directions.Count;
        Check("N5 the second player buys their Direction's next rank", dirs == 2, string.Join(",", his.Directions.ToArray()) + ", Pure " + his.Pure);
        his.Pure = 1;
        yield return Sync("broke");
        yield return Until(() => RWTwo.OtherSaid("brokedone"), 60f);
        Check("Shop: a player short of Pure Aura is refused, and nothing is taken", his.Pure == 1, "Pure " + his.Pure);
        his.Pure = 60;

        // Fund Transfer, the second player's way.
        float red0 = Purse(friend, "Redaura");
        Take(friend, "Redaura", red0 - 5f);
        float mineRed = Purse(me, "Redaura");
        yield return new WaitForSeconds(0.5f);
        yield return Sync("fund");
        yield return Until(() => RWTwo.OtherSaid("funded"), 90f);
        Check("Fund Transfer: more than the sender holds is refused", Mathf.Approximately(Purse(friend, "Redaura"), 5f) && Mathf.Approximately(Purse(me, "Redaura"), mineRed), "his " + Purse(friend, "Redaura") + ", mine " + Purse(me, "Redaura"));
        Give(friend, 200f);
        mineRed = Purse(me, "Redaura");
        float hisRed = Purse(friend, "Redaura");
        yield return new WaitForSeconds(0.5f);
        yield return Sync("fund2");
        yield return Until(() => RWTwo.OtherSaid("funded2"), 90f);
        Check("Fund Transfer: the second player sends 25 Red to the host, through the dialogs", Mathf.Abs(Purse(friend, "Redaura") - (hisRed - 25f)) < 0.01f && Mathf.Abs(Purse(me, "Redaura") - (mineRed + 25f)) < 0.01f,
            "his " + hisRed + " -> " + Purse(friend, "Redaura") + ", mine " + mineRed + " -> " + Purse(me, "Redaura") + ", sent " + his.Sent);
        yield return Sync("fund3");
        yield return Until(() => RWTwo.OtherSaid("funded3"), 90f);
        Check("Fund Transfer: an amount past this break's limit sends what's left of it", Mathf.Abs(Purse(me, "Redaura") - (mineRed + 50f)) < 0.01f && Mathf.Approximately(his.Sent, 50f), "mine " + mineRed + " -> " + Purse(me, "Redaura") + ", sent " + his.Sent + " of " + RWFund.Limit(0));
        yield return Sync("fund4");
        yield return Until(() => RWTwo.OtherSaid("funded4"), 90f);
        Check("Fund Transfer: with the limit spent nothing more moves", Mathf.Abs(Purse(me, "Redaura") - (mineRed + 50f)) < 0.01f, "mine " + Purse(me, "Redaura"));
        // And back: the host's own dialogs.
        hisRed = Purse(friend, "Blueaura");
        mineRed = Purse(me, "Blueaura");
        RWShop.Open("Fund");
        yield return WaitDialog(8f);
        Info("the host's Fund Transfer", CardTexts());
        ClickCard(friend.ToString());
        yield return new WaitForSecondsRealtime(0.8f);
        yield return WaitDialog(8f);
        ClickCard("Blueaura");
        yield return new WaitForSecondsRealtime(0.8f);
        yield return WaitDialog(8f);
        ClickCard("10");
        yield return new WaitForSeconds(1.5f);
        Check("Fund Transfer: the host sends 10 Blue to the second player", Mathf.Abs(Purse(friend, "Blueaura") - (hisRed + 10f)) < 0.01f && Mathf.Abs(Purse(me, "Blueaura") - (mineRed - 10f)) < 0.01f, "his " + hisRed + " -> " + Purse(friend, "Blueaura"));
        yield return Sync("hostfunded");

        // The board, the second player's pick.
        RWObjectives.Active = null;
        yield return Sync("board");
        yield return Until(() => RWObjectives.Active != null, 40f);
        Check("N5 the second player takes the objective for the team", RWObjectives.Active != null && RWTwo.ChatHas("took the objective"), RWObjectives.Active != null ? RWObjectives.Active.Display : "none");
        RWObjectiveDef taken = RWObjectives.Active;
        yield return Until(() => RWTwo.OtherSaid("boarddone"), 30f);

        // Tide 1, real foes for a moment, then its break: both are paid.
        int pureMine = mine.Pure, pureHis = his.Pure;
        yield return ToBreakAfter(1);
        yield return new WaitForSeconds(1f);
        Check("Pure Aura: Tide 1 pays both players the same, with Rising Water's extra", mine.Pure - pureMine == his.Pure - pureHis && his.Pure - pureHis >= RWRun.PerTide + RWWeather.PureAuraPerTide, "mine +" + (mine.Pure - pureMine) + ", his +" + (his.Pure - pureHis) + ", objective " + RWObjectives.Outcome);
        Check("Rising Water stands on this map, and climbed", RWWeather.Current == RWChart.RisingWater && RWWeather.Flood != null, "flood " + (RWWeather.Flood != null ? RWUtil.V(RWWeather.Flood.Entityposition.Value) : "none"));
        yield return Sync("tide1");

        // N7: the second player toggles speed and opens the pause menu.
        yield return Until(() => RWTwo.OtherSaid("sped"), 40f);
        Check("N7 the second player's speed button changes the game for all", Game_Mapvars.Instance.Time_Speedup.Value, "Time_Speedup " + Game_Mapvars.Instance.Time_Speedup.Value + ", Time.timeScale " + Time.timeScale);
        yield return Until(() => RWTwo.OtherSaid("unsped"), 40f);
        Check("N7 and back", !Game_Mapvars.Instance.Time_Speedup.Value, "Time_Speedup " + Game_Mapvars.Instance.Time_Speedup.Value);

        // The second player builds; a real Tide is fought, no foe cleared by the test.
        Give(friend, 3000f);
        Give(me, 3000f);
        // A client has no ground to read (RWUtil.Ground is the host's): the host names the tiles, as a player's eye would.
        {
            List<Vector3> tiles = new List<Vector3>();
            Heroentryobject sheet = RWUtil.Item<Heroentryobject>("Hero", RWTwo.SecondHero);
            Entity_Master hv = RWRun.Vessel(friend);
            foreach (string tower in sheet.Towerdisplay)
            {
                Beingobject being = ModKitUtil.Load<Beingobject>("Mob", tower);
                Vector3 at = being != null && hv != null ? RogueWavesModeKitTest.Spot(hv.Entityposition.Value, being, hv.Entitydata.Value.Team, 3f, 8f, tiles, tower) : new Vector3(float.NaN, 0f, 0f);
                if (!float.IsNaN(at.x)) yield return Say("spot " + tower + " " + RWUtil.F(at.x) + " " + RWUtil.F(at.y) + " " + RWUtil.F(at.z));
                else Info("building", "no tile for " + tower + " near the second player's vessel");
            }
        }
        yield return Sync("build");
        yield return Until(() => RWTwo.OtherSaid("built"), 120f);
        int hardBefore = hardErrors;
        freeze = false;
        forcing = false;
        Game_Mapvars.Instance.Time_Wave.Value = 0.1f;
        yield return Sync("fight");
        yield return new WaitForSeconds(8f);
        // A big fight: the game drops a look when 50 effects wait, and the vessels' own looks must still reach a friend (kit docs/19 section 4).
        {
            Entity_Master bigAt = RWRun.Vessel(friend);
            int spawned = 0;
            for (int i = 0; i < 60 && bigAt != null; i++)
            {
                if (RogueWavesModeKitTest.SpawnFoe("Enemy_Dagger", bigAt.Entityposition.Value, 1f, 7f, null) != null) spawned++;
                if (i % 6 == 0) yield return null;
            }
            Info("the big fight", spawned + " more foes by the second player's vessel, " + RWVote.Foes() + " up");
        }
        yield return Stepped(BigFightHost(friend));
        Check("N4 a real Tide with both players' things: no error on the host", hardErrors == hardBefore, (hardErrors - hardBefore) + " errors, the game's own cancels left out");
        ClearFoes();
        forcing = true;
        int breaks = RWVote.Breaks;
        yield return Until(() => RWVote.Breaks > breaks, 60f);
        forcing = false;
        ClearFoes();
        freeze = true;
        yield return Sync("fought");

        // O1-O4: a lost connection, the game left open.
        float hisRedBefore = Purse(friend, "Redaura");
        if (RWTwo.Review)
        {
            Game_Mapvars.Instance.Time_Scale.Value = 1.25f;     // what Quickening Current writes at its Tide 5
            yield return new WaitForSecondsRealtime(1.5f);
            Info("MODE-B1 before the drop", Speeds());
        }
        yield return Sync("drop");
        yield return new WaitForSecondsRealtime(3f);     // the word has to reach the friend first
        // A real disconnect, so the friend's game goes the way a lost connection does: the transport's event, not a call into the game.
        Unity.Netcode.NetworkManager.Singleton.DisconnectClient(Blm_Server.Instance.Sessionidsplayer[friend].OwnerClientId);
        yield return Until(() => !Blm_Server.Instance.Sessionids.ContainsKey(friend), 40f);
        yield return new WaitForSeconds(2f);
        Check("O1 the host says who dropped, the Tide clock runs on", RWTwo.ChatHas("lost the connection") && RWRejoin.Seats.ContainsKey(friend) && RWRejoin.Seats[friend].Away, "seats " + RWRejoin.Seats.Count);
        Check("O1 the lobby opens for the empty seat", Blm_Multiplayer.Instance.Server_Serveropen && RWRejoin.DoorOpen, "open " + Blm_Multiplayer.Instance.Server_Serveropen);
        if (RWTwo.Review) Reading("MODE-B1 the drop, on the host", Mathf.Abs(Time.timeScale - 1.25f) > 0.01f, Speeds());
        Entity_Master keptVessel = RWRun.Vessel(friend);
        Check("O1 their vessel waits for them", keptVessel != null, keptVessel != null ? keptVessel.Being.Value.ToString() : "none");
        int restored = RWRejoin.Restored;
        yield return Until(() => RWRejoin.Restored > restored, 180f);
        Check("O3 the host gives the seat back to the same session", RWRejoin.Restored > restored && Blm_Server.Instance.Sessionids.ContainsKey(friend), "restored " + RWRejoin.Restored + ", " + PlayerList());
        if (RWTwo.Review) Reading("MODE-B1 the rejoin, on the host", Mathf.Abs(Time.timeScale - 1.25f) > 0.01f, Speeds());
        yield return new WaitForSeconds(3f);
        Check("O3 their aura is as they left it, the lobby shut again", Mathf.Abs(Purse(friend, "Redaura") - hisRedBefore) < 0.01f && !RWRejoin.DoorOpen && RWTwo.ChatHas("is back"), "red " + hisRedBefore + " -> " + Purse(friend, "Redaura") + ", door " + RWRejoin.DoorOpen);
        yield return Sync("back");
        // O4: a Tide pays them again.
        pureHis = RWRun.For(friend).Pure;
        yield return ToBreakAfter(Game_Mapvars.Instance.Wave.Value + 1);
        yield return new WaitForSeconds(1f);
        Check("O4 a Tide pays the rejoined player", RWRun.For(friend).Pure > pureHis, pureHis + " -> " + RWRun.For(friend).Pure);
        yield return Sync("paid");
        if (RWTwo.Review) { yield return Stepped(ReviewHost(me, friend)); yield break; }
        if (RWTwo.Short) yield break;

        // O3b: the second copy killed as Task Manager would, started again: its id comes back, so the same seat.
        Line("DRIVER KILL");
        yield return Until(() => !Blm_Server.Instance.Sessionids.ContainsKey(friend), 40f);
        yield return new WaitForSeconds(2f);
        Check("O3b a killed second player's seat is kept", RWRejoin.Seats.ContainsKey(friend) && RWRejoin.Seats[friend].Away && RWRejoin.DoorOpen, "");
        // O5: a stranger tries to join first, twice.
        int turned = RWRejoin.Turned;
        Line("DRIVER START stranger");
        yield return Until(() => RWRejoin.Turned > turned + 1, 300f);
        Check("O5 a player who wasn't in the map is turned away, twice, with a chat line", RWRejoin.Turned >= turned + 2 && RWTwo.ChatHas("tried to join mid-map"), "turned " + RWRejoin.Turned);
        yield return new WaitForSeconds(3f);
        Line("DRIVER KILL");
        yield return new WaitForSeconds(4f);
        Check("O5 the empty seat is still kept, the lobby still open", RWRejoin.Seats[friend].Away && RWRejoin.DoorOpen, "");
        restored = RWRejoin.Restored;
        Line("DRIVER START rejoin");
        yield return Until(() => RWRejoin.Restored > restored, 300f);
        Check("O3b the restarted second player gets the same seat back", RWRejoin.Restored > restored && Blm_Server.Instance.Sessionids.ContainsKey(friend), PlayerList());
        yield return Sync("restarted", 300f);

        // On to Tide 15; the vote to leave with two voters.
        his = RWRun.For(friend);
        Game_Mapvars.Instance.Entry.Value = 2 * RWCampaign.WinTide - 1;    // the break before Tide 15
        Game_Mapvars.Instance.Wave.Value = RWCampaign.WinTide - 1;
        yield return new WaitForSeconds(0.5f);
        yield return ToBreakAfter(RWCampaign.WinTide);
        yield return Until(() => RWCampaign.WonThisMap, 15f);
        Check("Won: Tide 15 cleared wins the map for both, a pick owed to each", RWCampaign.WonThisMap && mine.Owed && his.Owed, "mine owed " + mine.Owed + ", his " + his.Owed);
        yield return Until(() => RWAsk.Open == RWVote.LeaveKind, 20f);
        yield return WaitDialog(8f);
        yield return Sync("leave1");
        LeaveVote = "";
        ClickCard("Leave");                         // the host leaves, the second player stays: one of two is no majority
        yield return Until(() => RWVote.State != "Leave", 30f);
        // R4 Leave: the vote's own result; with a single map ahead B1's map vote writes LastVote over it in the same call.
        Check("Leave: one of two isn't a majority, the party stays", !RWVote.Departing && LeaveVote == "1 of 2 to leave", "the vote to leave: '" + LeaveVote + "', last vote " + RWVote.LastVote);
        // B1: staying votes on the next map there and then, on both machines.
        yield return Until(() => RWAsk.Open.StartsWith(RWVote.MapKind) || RWVote.State == "", 10f);
        if (RWAsk.Open.StartsWith(RWVote.MapKind))
        {
            yield return WaitDialog(8f);
            List<string> stayCards = CardNames();
            if (stayCards.Count > 0) ClickCard(stayCards[0]);
        }
        yield return Until(() => RWVote.State == "", RWVote.MapSeconds + 10f);
        Check("B1 staying votes on the next map for both, and the bonus Tides go on", RWCampaign.Choice >= 0 && !RWVote.HoldBreak, RWVote.LastVote + ", choice " + RWCampaign.Choice);
        yield return ToBreakAfter(RWCampaign.WinTide + 1);
        yield return Until(() => RWAsk.Open == RWVote.LeaveKind, 20f);
        yield return WaitDialog(8f);
        yield return Sync("leave2");
        ClickCard("Leave");                         // the second player doesn't answer: the question times out
        float asked = Time.realtimeSinceStartup;
        yield return Until(() => RWVote.State == "", RWVote.LeaveSeconds + 15f);
        Check("Leave: a player who doesn't answer counts as staying, after the timer", !RWVote.Departing && RWVote.LastVote == "1 of 2 to leave" && Time.realtimeSinceStartup - asked >= RWVote.LeaveSeconds * 0.5f, RWVote.LastVote + " after " + RWUtil.F(Time.realtimeSinceStartup - asked) + " s");
        yield return ToBreakAfter(RWCampaign.WinTide + 2);
        yield return Until(() => RWAsk.Open == RWVote.LeaveKind, 20f);
        yield return WaitDialog(8f);
        yield return Sync("leave3");
        ClickCard("Leave");                         // both leave
        yield return Until(() => RWVote.Departing, 30f);
        Check("Leave: both leave", RWVote.Departing && RWVote.LastVote == "2 of 2 to leave", RWVote.LastVote);
        yield return Until(() => RWAsk.Open.StartsWith(RWVote.DirectionKind), 15f);
        yield return WaitDialog(8f);
        ClickCard("0");
        yield return Until(() => RWAsk.Open.StartsWith(RWVote.MapKind) || RWCampaign.Choice >= 0, 40f);
        if (RWAsk.Open.StartsWith(RWVote.MapKind))
        {
            yield return WaitDialog(8f);
            List<string> second = CardNames();
            yield return Sync("vote2");
            ClickCard(second[second.Count - 1]);    // both take the last card: it wins with two
            yield return Until(() => RWVote.LastVote.Contains("with 2") || RWVote.LastVote.StartsWith("only"), 40f);
            Check("Vote: a map both pick wins with two votes", RWCampaign.Choice.ToString() == second[second.Count - 1] && RWVote.LastVote.Contains("with 2"), RWVote.LastVote);
        }
        yield return Until(() => Game_Mapvars.Instance == null || Game_Mapvars.Instance.Gamecomplete.Value.ToString() == "Win", 30f);
        Check("N12 the party leaves by the stock win path", Game_Mapvars.Instance != null && Game_Mapvars.Instance.Gamecomplete.Value.ToString() == "Win", RWVote.LastVote);
        yield return new WaitForSeconds(8f);
        yield return OneWay("endscreen");
        Macro_Main old = Macro_Main.Instance;
        Tds_Multiplayer_Remote.Instance.Returnmap_ServerRpc(true, default(Unity.Netcode.ServerRpcParams));
        yield return Until(() => Macro_Main.Instance != null && Macro_Main.Instance != old && Macro_Main.Instance.Ready, 150f);
        boat = Macro_Main.Instance;
        yield return Until(() => Here(), 40f);
        Check("N12 both are back on the boat, one row on", PlayersIn() == 2 && RWCampaign.Route.Count == 1, PlayerList() + ", route " + Digits(RWCampaign.Route));
        yield return Until(() => boat.Selected.Value.ToString() != "" && Macro_Worldvisual.Instance.Insetup, 60f);
        // The save holds the second player's run.
        RWRun.Save();
        string savedHis = "";
        for (int i = 0; i < RWUtil.ReadInt(RWData.SaveFolder, RWRun.CountKey, 0); i++)
        {
            RWPlayer p = RWRun.Decode(RWUtil.ReadString(RWData.SaveFolder, RWRun.PlayerKey + i));
            if (p != null && p.Key == RWTwo.SecondGuid) savedHis = p.Hero + "/" + p.Pure + "/" + p.Directions.Count;
        }
        his = RWRun.For(friend);
        Check("A friend's vessel, Pure Aura and Directions are saved with the campaign", savedHis == his.Hero + "/" + his.Pure + "/" + his.Directions.Count && his.Directions.Count >= 2 && his.Hero == RWTwo.SecondHero, savedHis);
        yield return OneWay("map2");
        boat.Playgame(true);

        // Map 2, Quickening Current: the speed on both machines.
        yield return Until(() => LoadedHere() && RWRun.InCampaignMap && RWCampaign.Playing >= 0, 240f);
        yield return Until(() => RWShop.Stand != null, 120f);
        freeze = true;
        Check("Quickening Current: this map has it", RWWeather.Current == RWChart.QuickeningCurrent, RWWeather.Display(RWWeather.Current));
        yield return ToBreakAfter(1);
        yield return new WaitForSeconds(1f);
        yield return Sync("quick");
        yield return Until(() => RWTwo.OtherSaid("quickdone"), 40f);
        yield return Win();
        yield return new WaitForSeconds(6f);
        yield return OneWay("won2");
        old = Macro_Main.Instance;
        Tds_Multiplayer_Remote.Instance.Returnmap_ServerRpc(true, default(Unity.Netcode.ServerRpcParams));
        yield return Until(() => Macro_Main.Instance != null && Macro_Main.Instance != old && Macro_Main.Instance.Ready, 150f);
        boat = Macro_Main.Instance;
        yield return Until(() => Here(), 40f);
        RWCampaign.Choice = RWCampaign.Open()[0].Col;
        yield return Until(() => boat.Selected.Value.ToString() != "" && Macro_Worldvisual.Instance.Insetup, 60f);
        yield return OneWay("map3");
        boat.Playgame(true);

        // Map 3, Nesting Ground.
        yield return Until(() => LoadedHere() && RWRun.InCampaignMap && RWCampaign.Playing >= 0, 240f);
        yield return Until(() => RWShop.Stand != null, 120f);
        freeze = true;
        Check("Nesting Ground: this map has it, and the campaign's first modifier", RWWeather.Current == RWChart.NestingGround && RWWeather.Nest != null && RWDifficulty.LastModifiers != "", RWWeather.Display(RWWeather.Current) + ", modifiers [" + RWDifficulty.LastModifiers + "]");
        yield return ToBreakAfter(1);
        yield return new WaitForSeconds(1f);
        yield return Sync("nest");
        yield return Until(() => RWTwo.OtherSaid("nestdone"), 40f);

        // O7 on a Nesting Ground map: the friend drops and comes back.
        int nestRestored = RWRejoin.Restored;
        yield return Sync("dropnest");
        yield return new WaitForSecondsRealtime(3f);
        Unity.Netcode.NetworkManager.Singleton.DisconnectClient(Blm_Server.Instance.Sessionidsplayer[friend].OwnerClientId);
        yield return Until(() => !Blm_Server.Instance.Sessionids.ContainsKey(friend), 40f);
        yield return Until(() => RWRejoin.Restored > nestRestored, 200f);
        Check("O7 a friend dropped on a Nesting Ground map gets the seat back", RWRejoin.Restored > nestRestored && RWWeather.Current == RWChart.NestingGround && RWWeather.Nest != null, "restored " + RWRejoin.Restored);
        yield return Sync("nestback", 300f);

        // The host leaves the running map by the pause menu's Exit: a retry goes, both sail back.
        int retries = RWCampaign.Retries;
        Tds_Pausemenu.Openpausemenu();
        yield return new WaitForSecondsRealtime(1.2f);
        GameObject exitItem;
        if (Blockage.Menu.Bl_Menus.Menus["Pause"].Items.TryGetValue("Exit", out exitItem) && exitItem != null && exitItem.GetComponent<Blu3_Button>() != null && exitItem.GetComponent<Blu3_Button>().Buttonclick != null) exitItem.GetComponent<Blu3_Button>().Buttonclick();
        yield return WaitDialog(5f);
        Info("the host's exit dialog", CardTexts() + "; sub '" + TextOf(Bla_Ui_Confirmation.Instance.Subbox) + "'");
        old = Macro_Main.Instance;
        ClickCard("Exit");
        yield return Until(() => Macro_Main.Instance != null && Macro_Main.Instance != old && Macro_Main.Instance.Ready, 150f);
        boat = Macro_Main.Instance;
        yield return Until(() => Here(), 40f);
        yield return new WaitForSeconds(3f);
        Check("The host leaving a running map costs a retry, and both are back on the boat", RWCampaign.Retries == retries - 1 && PlayersIn() == 2, "retries " + RWCampaign.Retries + ", " + PlayerList());
        yield return OneWay("exited");

        yield return TwoTail();
    }

    static IEnumerator TwoTail()
    {
        Macro_Main boat;
        long friend;
        RWPlayer his;
        Entity_Master hisVessel;
        // N13: the host goes home from the boat; the second player lands on the main menu. Then Continue, and they join again.
        yield return ToMainMenu();
        Mainmenu_Main again = UnityEngine.Object.FindObjectOfType<Mainmenu_Main>();
        if (again == null) { Setup("Continue", "the boat's Exit never reached the main menu"); yield break; }
        Line("DRIVER KILL");
        yield return new WaitForSecondsRealtime(8f);
        again.Idlethreshold = 100000f;
        again.SendMessage("Transitiontomain", SendMessageOptions.DontRequireReceiver);
        yield return Until(() => RWMenu.Button != null, 25f);
        yield return new WaitForSecondsRealtime(7f);
        Blockage.Menu.Bl_Menus.Changemenu("Main", true);
        yield return new WaitForSecondsRealtime(8f);
        RWMenu.Clicked();
        yield return WaitDialog(10f);
        Check("Continue: its card names both of the crew", RWMenu.SavedLine().Contains(RWRun.HeroName(RWTwo.SecondHero)) && RWMenu.SavedLine().Contains(RWRun.HeroName(RWTwo.HostHero)), RWMenu.SavedLine());
        ClickCard("Continue");
        yield return Until(() => Macro_Main.Instance != null && Macro_Main.Instance.Ready, 120f);
        boat = Macro_Main.Instance;
        yield return Until(() => RWCampaign.Live && Here(), 40f);
        ForceWeather(Seed);
        Line("DRIVER START continue");
        yield return Until(() => PlayersIn() >= 2, 300f);
        Check("Continue: the second player joins the host's continued campaign", PlayersIn() >= 2 && RWCampaign.Route.Count == 2, PlayerList());
        yield return OneWay("continued");
        friend = OtherSession();
        yield return new WaitForSeconds(4f);
        his = RWRun.For(friend);
        Check("Continue: the friend's run comes back from the save by their id", his.Key == RWTwo.SecondGuid && his.Directions.Count >= 2 && his.Hero == RWTwo.SecondHero, "key " + his.Key + ", dirs " + his.Directions.Count + ", pure " + his.Pure + ", hero " + his.Hero);
        yield return Until(() => RWTwo.OtherSaid("swapped"), 40f);
        yield return new WaitForSeconds(2.5f);
        Check("Vessels are locked in: the friend's try at another vessel goes back, and is said", Blm_Server.Instance.Sessionidsplayer[friend].GetComponent<Tds_Clientinfo>().Hero.Value.ToString() == RWTwo.SecondHero && RWTwo.ChatHas("locked in"), PlayerList());
        yield return Until(() => boat.Selected.Value.ToString() != "" && Macro_Worldvisual.Instance.Insetup, 60f);
        yield return OneWay("map3b");
        boat.Playgame(true);
        yield return Until(() => LoadedHere() && RWRun.InCampaignMap && RWCampaign.Playing >= 0, 240f);
        yield return Until(() => RWShop.Stand != null, 120f);
        freeze = true;
        yield return new WaitForSeconds(2f);
        hisVessel = RWRun.Vessel(friend);
        Check("Continue: the friend's Directions are back on their vessel in the replayed map", hisVessel != null && hisVessel.Upgradelistserver.ContainsKey(his.Directions[0]), hisVessel != null ? string.Join(",", new List<string>(hisVessel.Upgradelistserver.Keys).ToArray()) : "no vessel");
        yield return Sync("replayed");
        yield return Until(() => RWTwo.OtherSaid("replaydone"), 40f);

        // N13 last: the host quits to the main menu in the middle of a map. The second player lands on the error screen.
        yield return Sync("hostquits");
        Blm_Multiplayer.Instance.Leaveserver();
        UnityEngine.SceneManagement.SceneManager.LoadScene(Blm_Multiplayer.Instance.Serverclosescene);
        yield return new WaitForSecondsRealtime(55f);

        // N14: a friend without the pack joins the boat; the no-mods warning; the host starts; 75 s later they're sent back.
        Line("DRIVER KILL");
        yield return TwoNoPack();
        Line("DRIVER KILL");
        yield return new WaitForSecondsRealtime(2f);
    }

    static void Give(long session, float each)
    {
        List<Link_Resourceset> pay = new List<Link_Resourceset>();
        foreach (string r in new[] { "Redaura", "Blueaura", "Yellowaura" }) pay.Add(new Link_Resourceset { Name = r, Amount = each });
        Game_Mapvars vars = Game_Mapvars.Instance;
        float gain = vars.Resourcegainmultiplier;
        vars.Resourcegainmultiplier = 1f;
        Helper_Resources.Giveresources(pay, session, Helper_Resources.Styles.Default);
        vars.Resourcegainmultiplier = gain;
    }

    static void Take(long session, string res, float amount)
    {
        Game_Mapvars vars = Game_Mapvars.Instance;
        float loss = vars.Resourcelossmultiplier;
        vars.Resourcelossmultiplier = 1f;
        Helper_Resources.Useresources(new List<Link_Resourceset> { new Link_Resourceset { Name = res, Amount = amount } }, session, 1f);
        vars.Resourcelossmultiplier = loss;
    }

    static float Purse(long session, string res)
    {
        Tds_Clientinfo info;
        if (Game_Players.Instance == null || !Game_Players.Instance.Prevsessionlist.TryGetValue(session, out info) || info == null)
            return -1f;
        foreach (Link_Resource r in info.Resources)
            if (r.Name.ToString() == res) return r.Amount;
        return 0f;
    }

    // N14, O3c, U7's real half.
    static IEnumerator TwoNoPack()
    {
        yield return Until(() => UnityEngine.Object.FindObjectOfType<Mainmenu_Main>() != null, 60f);
        Mainmenu_Main m = UnityEngine.Object.FindObjectOfType<Mainmenu_Main>();
        if (m == null) { Setup("N14", "no main menu"); yield break; }
        yield return new WaitForSecondsRealtime(8f);
        m.Idlethreshold = 100000f;
        m.SendMessage("Transitiontomain", SendMessageOptions.DontRequireReceiver);
        yield return Until(() => RWMenu.Button != null, 25f);
        yield return new WaitForSecondsRealtime(7f);
        Blockage.Menu.Bl_Menus.Changemenu("Main", true);
        yield return new WaitForSecondsRealtime(8f);
        RWMenu.Clicked();
        yield return WaitDialog(10f);
        ClickCard("Continue");
        yield return Until(() => Macro_Main.Instance != null && Macro_Main.Instance.Ready, 120f);
        Macro_Main boat = Macro_Main.Instance;
        yield return Until(() => RWCampaign.Live && Here(), 40f);
        Line("DRIVER START nopack");
        yield return Until(() => PlayersIn() >= 2, 300f);
        Check("N14 a friend without the pack is let onto the boat", PlayersIn() >= 2, PlayerList());
        yield return new WaitForSeconds(RWLobby.GraceSeconds + 12f);
        Check("L1 the host is warned that this friend has no mods on", RWLobby.Warned.Count > 0, "warned " + string.Join(",", RWLobby.Warned.ToArray()) + "; " + PlayerList());
        yield return Until(() => boat.Selected.Value.ToString() != "" && Macro_Worldvisual.Instance.Insetup, 60f);
        boat.Playgame(true);
        int kicked = RWLoad.Kicked;
        float begun = Time.realtimeSinceStartup;
        yield return Until(() => RWLoad.Kicked > kicked, 150f);
        Check("O3c the host sends back a friend who can't load the map, at 75 s", RWLoad.Kicked > kicked, "kicked " + RWLoad.Kicked + " after " + RWUtil.F(Time.realtimeSinceStartup - begun) + " s");
        yield return Until(() => LoadedHere() && Game_Init.Instance != null && Game_Init.Instance.Gameloaded.Value, 60f);
        Check("O3c the host starts the map alone", LoadedHere() && PlayersIn() == 1, PlayerList());
    }

    // ---------------------------------------------------------------- the second player

    static IEnumerator TwoSecond()
    {
        yield return Until(() => UnityEngine.Object.FindObjectOfType<Mainmenu_Main>() != null, 120f);
        yield return new WaitForSecondsRealtime(8f);
        if (RWTwo.Phase == "stranger") { yield return Stranger(); yield break; }
        yield return RWTwo.Join();
        if (RWTwo.Phase == "rejoin") { yield return AfterRestart(); yield return SecondFromVote(); yield break; }
        if (RWTwo.Phase == "continue") { yield return SecondContinue(); yield break; }

        yield return Until(() => Macro_Main.Instance != null && Macro_Main.Instance.Ready, 120f);
        Macro_Main boat = Macro_Main.Instance;
        Check("N1 the second player is on the boat", boat != null && boat.Ready, PlayerList());
        if (boat == null) yield break;
        yield return new WaitForSecondsRealtime(3f);
        Macro_Worldvisual vis = Macro_Worldvisual.Instance;
        Check("C7 the chart is built on the second player's machine from its name", vis != null && vis.World != null && vis.World.name == Tds_Multiplayerinfo.Instance.World.Value.ToString() && vis.World.name.StartsWith(RWData.WorldPrefix),
            "world " + (vis != null && vis.World != null ? vis.World.name : "-") + ", synced " + Tds_Multiplayerinfo.Instance.World.Value + ", nodes " + (vis != null && vis.World != null ? vis.World.GetComponentsInChildren<Macro_Link_Add_Base>(true).Length : 0));
        uint seed;
        List<int> sailed;
        RWWorld.Parse(Tds_Multiplayerinfo.Instance.World.Value.ToString(), out seed, out sailed);
        ForceWeather(seed);
        boat.Selectcharacter(RWTwo.SecondHero);
        yield return new WaitForSecondsRealtime(2f);
        yield return Snap("second_boat");
        yield return Until(() => Macro_Worldvisual.Instance.Insetup, 60f);
        Check("The Setup panel opens on the second player's screen as the host's boat docks", Macro_Worldvisual.Instance.Insetup && boat.Selected.Value.ToString() != "", "selected '" + boat.Selected.Value + "'");
        yield return OneWay("harbour");
        int loads = RWCard.LoadingWrites;
        boat.Playgame(true);
        yield return Until(() => RWCard.LoadingWrites > loads, 90f);
        Check("T1 the second player's loading screen says where the campaign stands", RWCard.LoadingWrites > loads && RWCard.LastLine.StartsWith("Setting out"), "'" + RWCard.LastLine + "'");
        yield return Until(() => LoadedHere(), 240f);
        Check("N3 the staging map loads on the second player's machine", LoadedHere(), "mode " + Tds_Multiplayerinfo.Instance.Mode.Value + ", plan " + Tds_Multiplayerinfo.Instance.Gameplan.Value);
        yield return Until(() => RWAsk.Open.StartsWith(RWVote.DirectionKind), 120f);
        yield return WaitDialog(10f);
        List<string> offered = CardNames();
        Check("Direction: the second player is asked, with " + RWDirections.Offers + " cards of their own", offered.Count == RWDirections.Offers, CardTexts() + " from '" + RWAsk.Open + "'");
        yield return DialogFits("The second player's Direction pick");
        yield return Snap("second_direction");
        ClickCard("0");
        Entity_Master v = null;
        yield return Until(() => (v = RWMine.Vessel()) != null && HeldDirections(v).Count >= 1, 15f);
        Check("N5 the Direction shows on the second player's own vessel", v != null && HeldDirections(v).Count >= 1, v != null ? string.Join(",", HeldDirections(v).ToArray()) : "no vessel");
        yield return Until(() => RWAsk.Open.StartsWith(RWVote.MapKind), 40f);
        yield return WaitDialog(10f);
        List<string> mapCards = CardNames();
        Check("Vote: the second player sees the first row's maps", mapCards.Count == RWWorld.Choices(RWWorld.Chart(seed), sailed).Count, CardTexts());
        yield return DialogFits("The second player's map vote");
        yield return Snap("second_vote");
        yield return Sync("vote");
        ClickCard(mapCards[mapCards.Count - 1]);
        Macro_Main oldBoat = boat;
        yield return Until(() => Macro_Main.Instance != null && Macro_Main.Instance != oldBoat && Macro_Main.Instance.Ready, 200f);
        boat = Macro_Main.Instance;
        Check("staging: the second player is back on the boat", boat != null && boat.Ready, "");
        yield return Until(() => Macro_Worldvisual.Instance != null && Macro_Worldvisual.Instance.Insetup && boat.Selected.Value.ToString() != "", 120f);
        Check("The boat sails with the host, and the Setup panel opens here too", Macro_Worldvisual.Instance.Insetup, "selected '" + boat.Selected.Value + "', boat " + RWUtil.V(Macro_Worldvisual.Instance.Boat.transform.position));
        yield return OneWay("map1");
        loads = RWCard.LoadingWrites;
        boat.Playgame(true);
        yield return Until(() => RWCard.LoadingWrites > loads, 90f);
        Check("T1 map 1's loading card on the second player's screen", RWCard.LastLine.StartsWith("Map 1 of 10"), "'" + RWCard.LastLine + "'");
        yield return Until(() => LoadedHere(), 240f);
        yield return new WaitForSeconds(5f);
        Check("U2 the map intro card on the second player's screen", RWCard.CardWrites > 0 && RWCard.LastLine.StartsWith("Map 1 of 10"), "'" + RWCard.LastLine + "'");
        yield return Until(() => Vendcint() != null, 120f);
        Entity_Master stand = Vendcint();
        Check("VENDCINT lands on the second player's screen, theirs to select", stand != null && stand.Entitydata.Value.Subowner != null && Array.IndexOf(stand.Entitydata.Value.Subowner, RWMine.Session) >= 0, stand != null ? "at " + RWUtil.V(stand.Entityposition.Value) + ", session " + RWMine.Session : "none");
        yield return new WaitForSeconds(4f);
        yield return Snap("second_vendcint");
        yield return Sync("shop");
        yield return new WaitForSeconds(1f);

        // The panel and the shop on a client.
        Game_Selection.Instance.Selected = stand;
        Game_Selection.Instance.Select();
        yield return new WaitForSeconds(1.5f);
        Blockage.Menu.Blu2_Menu_Handler hud = Blockage.Menu.Bl_Menus.Menus["Main"];
        Blu3_Button shopButton = null;
        List<string> onBar = new List<string>();
        foreach (Transform t in hud.GetComponentsInChildren<Transform>(true))
            if (t.gameObject.activeInHierarchy && t.name.StartsWith("RW_"))
            {
                onBar.Add(t.name);
                if (t.name.StartsWith("RW_Shop") && t.GetComponent<Blu3_Button>() != null) shopButton = t.GetComponent<Blu3_Button>();
            }
        Check("VENDCINT's three buttons show on the second player's HUD", onBar.Exists(n => n.StartsWith("RW_Shop")) && onBar.Exists(n => n.StartsWith("RW_Fund")) && onBar.Exists(n => n.StartsWith("RW_Board")), string.Join(",", onBar.ToArray()));
        yield return Snap("second_panel");
        Check("Pure Aura: the second player's counter and run line follow the host", Mathf.RoundToInt(RWMine.Pure()) == 60 && RWRunLabel.Shown.Contains("Pure Aura 60"), "counter " + RWMine.Pure() + ", line '" + RWRunLabel.Shown + "'");
        if (shopButton != null && shopButton.Buttonclick != null) shopButton.Buttonclick();
        else RWShop.Open("Shop");
        yield return WaitDialog(6f);
        List<string> shelf = CardNames();
        Check("The HUD's SHOP button opens the shop on the second player's machine, Cardinals or Trinkets and the Direction's next rank on it", shelf.Exists(n => n.StartsWith("C:")) && shelf.Exists(n => n.StartsWith("D:")) && shelf.Contains("Back"), string.Join(",", shelf.ToArray()));
        yield return DialogFits("The second player's shop");
        yield return Snap("second_shop");
        string buy = shelf.Find(n => n.StartsWith("C:"));
        string rank = shelf.Find(n => n.StartsWith("D:"));
        string id = buy != null ? buy.Substring(2) : "";
        float pure0 = RWMine.Pure();
        int price = buy != null ? RWCardinals.PriceMine(id) : 0;
        if (buy != null) ClickCard(buy);
        yield return Until(() => RWMine.Pure() != pure0, 10f);
        yield return new WaitForSeconds(0.5f);
        Check("Shop: the second player's counter drops by the price", Mathf.RoundToInt(pure0 - RWMine.Pure()) == price, id + " " + pure0 + " -> " + RWMine.Pure());
        if (!RWCardinals.IsTrinket(id))
        {
            yield return new WaitForSeconds(3f);
            bool listed = false;
            foreach (FixedString64Bytes a in Tds_Multiplayerinfo.Instance.Artifacts) if (a.ToString() == id) listed = true;
            Check("A Cardinal joins the HUD's row on the second player's machine", listed && Game_Mapvars.Instance.Artifactobjects != null && Game_Mapvars.Instance.Artifactobjects.Exists(x => x != null && x.Objectname != null), id + ", listed " + listed + ", row " + (Game_Mapvars.Instance.Artifactobjects != null ? Game_Mapvars.Instance.Artifactobjects.Count : -1));
        }
        yield return Say("bought");
        yield return Sync("rank");
        pure0 = RWMine.Pure();
        RWShop.Open("Shop");
        yield return WaitDialog(6f);
        Check("Shop: the next rank is on the shelf again", CardNamed(rank) != null, string.Join(",", CardNames().ToArray()));
        ClickCard(rank);
        v = RWMine.Vessel();
        yield return Until(() => HeldDirections(v).Count >= 2, 12f);
        Check("The rank shows on the second player's vessel, for its price", HeldDirections(v).Count >= 2 && Mathf.RoundToInt(pure0 - RWMine.Pure()) == RWDirections.RankTwoCost, string.Join(",", HeldDirections(v).ToArray()) + ", Pure " + pure0 + " -> " + RWMine.Pure());
        yield return Say("rankdone");
        yield return Sync("broke");
        yield return Until(() => Mathf.RoundToInt(RWMine.Pure()) == 1, 8f);
        RWShop.Open("Shop");
        yield return WaitDialog(6f);
        string dear = CardNames().Find(n => n.StartsWith("C:") || n.StartsWith("D:"));
        if (dear != null) ClickCard(dear);
        yield return Until(() => RWTwo.ChatHas("not enough pure aura"), 8f);
        Check("Shop: a refusal reaches the second player as a chat line", RWTwo.ChatHas("not enough pure aura"), string.Join(" | ", RWTwo.ChatSeen.ToArray()));
        yield return Say("brokedone");

        // Fund Transfer on a client: refused, sent, clamped, spent; each dialog on the screen.
        yield return Sync("fund");
        yield return FundDialogs("10", true);
        yield return Until(() => RWTwo.ChatHas("not enough aura"), 8f);
        Check("Fund Transfer: the refusal reaches the second player", RWTwo.ChatHas("not enough aura"), "");
        yield return Say("funded");
        yield return Sync("fund2");
        float red0 = Wallet("Redaura");
        yield return FundDialogs("25", false);
        yield return Until(() => Wallet("Redaura") != red0, 8f);
        Check("Fund Transfer: the second player's wallet drops by 25 Red", Mathf.Abs(red0 - Wallet("Redaura") - 25f) < 0.01f, red0 + " -> " + Wallet("Redaura"));
        yield return Say("funded2");
        yield return Sync("fund3");
        yield return FundDialogs("50", false);
        yield return new WaitForSeconds(1.5f);
        yield return Say("funded3");
        yield return Sync("fund4");
        yield return FundDialogs("10", false);
        yield return Until(() => RWTwo.ChatHas("limit is spent"), 8f);
        Check("Fund Transfer: the spent limit is said", RWTwo.ChatHas("limit is spent"), "");
        yield return Say("funded4");
        float blue0 = Wallet("Blueaura");
        yield return Sync("hostfunded");
        yield return Until(() => Wallet("Blueaura") != blue0, 8f);
        Check("Fund Transfer: the host's 10 Blue reach the second player's wallet", Mathf.Abs(Wallet("Blueaura") - blue0 - 10f) < 0.01f, blue0 + " -> " + Wallet("Blueaura"));

        // The board.
        yield return Sync("board");
        yield return new WaitForSeconds(1f);
        RWShop.Open("Board");
        yield return WaitDialog(6f);
        Check("The board opens on the second player's machine from the run line", CardNamed("Take") != null, CardTexts() + "; line '" + RWRunLabel.Shown + "'");
        yield return DialogFits("The second player's board");
        yield return Snap("second_board");
        ClickCard("Take");
        yield return new WaitForSeconds(1.5f);
        yield return Say("boarddone");

        // Tide 1 and its break.
        int waveWas = Game_Mapvars.Instance.Wave.Value;
        float pureBefore = RWMine.Pure();
        Entity_Master firstStand = Vendcint();
        yield return Until(() => RWTwo.OtherSaid("tide1"), 200f);
        yield return Say("tide1");
        Check("The Tide's number syncs, and the second player is paid", Game_Mapvars.Instance.Wave.Value == waveWas + 1 && RWMine.Pure() > pureBefore, "Wave " + Game_Mapvars.Instance.Wave.Value + ", Pure " + pureBefore + " -> " + RWMine.Pure());
        Check("VENDCINT falls again on the second player's screen, a new one", Vendcint() != null && Vendcint() != firstStand, "");
        Entity_Master flood = Thing("Main_Flood");
        Check("Rising Water: the flood stands on the second player's machine", flood != null, flood != null ? "at " + RWUtil.V(flood.Entityposition.Value) : "none");
        yield return Snap("second_flood");

        // N7.
        Blu3_Button speed = HudButton("Fastforward");
        if (speed != null && speed.Buttonclick != null) speed.Buttonclick();
        else Info("the speed button", "not found on the HUD");
        yield return new WaitForSeconds(1f);
        Check("N7 the speed button works from a client, here too", Game_Mapvars.Instance.Time_Speedup.Value && Time.timeScale > 1f, "Time_Speedup " + Game_Mapvars.Instance.Time_Speedup.Value + ", Time.timeScale " + Time.timeScale);
        yield return Say("sped");
        yield return new WaitForSeconds(1f);
        if (speed != null && speed.Buttonclick != null) speed.Buttonclick();
        yield return new WaitForSeconds(1f);
        yield return Say("unsped");
        Tds_Pausemenu.Openpausemenu();
        yield return new WaitForSecondsRealtime(1.2f);
        Check("N7 a client's pause menu pauses nothing", Time.timeScale > 0f, "Time.timeScale " + Time.timeScale);
        yield return Snap("second_pause");
        Tds_Pausemenu.Instance.SendMessage("Pauseclose", SendMessageOptions.DontRequireReceiver);
        yield return new WaitForSecondsRealtime(1f);

        // The second player's own buildings.
        yield return Sync("build");
        yield return SecondBuilds();
        yield return Say("built");
        yield return Sync("fight");
        int hardBefore = hardErrors;
        List<string> perSecond = new List<string>();
        // How many foes wear a debuff look on this machine, sampled through the fight.
        int mostLooks = 0, foesSeen = 0;
        List<string> lookNames = new List<string>();
        float fightEnds = Time.realtimeSinceStartup + 42f;
        bool shot1 = false;
        while (Time.realtimeSinceStartup < fightEnds)
        {
            int looks = 0, foes = 0;
            foreach (Entity_Master e in new List<Entity_Master>(Game_Entity.Instance.Entityclient))
            {
                if (e == null || e.Taglistserver == null && e.Being.Value.ToString().IndexOf("Enemy_") != 0) continue;
                if (e.Being.Value.ToString().IndexOf("Enemy_") != 0) continue;
                foes++;
                bool wears = false;
                foreach (Transform t in e.GetComponentsInChildren<Transform>(true))
                    if (t.name.StartsWith("Debuff_"))
                    {
                        wears = true;
                        string look = t.name.Replace("(Clone)", "");
                        if (!lookNames.Contains(look)) lookNames.Add(look);
                    }
                if (wears) looks++;
            }
            perSecond.Add(looks.ToString());
            if (looks > mostLooks) mostLooks = looks;
            if (foes > foesSeen) foesSeen = foes;
            if (!shot1 && looks > 0 && foes > 20) { shot1 = true; yield return Snap("second_fight1"); }
            yield return new WaitForSecondsRealtime(1f);
        }
        yield return Snap("second_fight2");
        Info("the big fight here", "up to " + foesSeen + " foes, up to " + mostLooks + " wearing a look: " + string.Join(", ", lookNames.ToArray()));
        Info("the big fight here, foes wearing a look each second", string.Join(",", perSecond.ToArray()));
        // A small fight measures nothing: a Setup, not a pass (MAX KIT-7).
        if (foesSeen < 20)
            Setup("N4 in a big fight, a look sent by the host reaches foes on the second player's machine", "only " + foesSeen + " foes seen, 20 needed");
        else
            Check("N4 in a big fight, a look sent by the host reaches foes on the second player's machine", mostLooks > 0, "up to " + foesSeen + " foes, up to " + mostLooks + " wearing a look: " + string.Join(", ", lookNames.ToArray()));
        Check("N4 a real Tide with both players' things: no error on the second player's machine", hardErrors == hardBefore, (hardErrors - hardBefore) + " errors, the game's own cancels left out");
        yield return Sync("fought");

        // O1-O4, the game left open.
        int mineCount = OwnCount();
        float red = Wallet("Redaura");
        long session = RWMine.Session;
        yield return Sync("drop");
        yield return Until(() => UnityEngine.Object.FindObjectOfType<Mainmenu_Main>() != null, 60f);
        yield return new WaitForSecondsRealtime(3f);
        Check("O2 the dropped player's main menu says how to come back", ErrorBox().Contains("Join Game"), "'" + ErrorBox() + "'");
        yield return Snap("second_dropped");
        {
            Mainmenu_Main dropped = UnityEngine.Object.FindObjectOfType<Mainmenu_Main>();
            if (dropped != null)
            {
                dropped.SendMessage("Transitiontomain", SendMessageOptions.DontRequireReceiver);
                yield return new WaitForSecondsRealtime(5f);
                Blockage.Menu.Bl_Menus.Changemenu("Main", true);
                yield return new WaitForSecondsRealtime(3f);
                Info("the error box on the main menu", ErrorBox());
                yield return Snap("second_dropped_main");
            }
        }
        yield return new WaitForSecondsRealtime(6f);
        yield return RWTwo.Join();
        yield return Until(() => LoadedHere(), 200f);
        Check("O3 the second player rejoins the running map", LoadedHere() && RWRejoin.Late, "Late " + RWRejoin.Late + ", Wave " + (Game_Mapvars.Instance != null ? Game_Mapvars.Instance.Wave.Value : -1) + ", session " + RWMine.Session);
        Check("O3 the same session comes back", RWMine.Session == session, session + " -> " + RWMine.Session);
        yield return Until(() => Game_Client_Init.Instance != null && Game_Client_Init.Instance.Gametimescale, 40f);
        yield return new WaitForSeconds(4f);
        v = RWMine.Vessel();
        Check("O3 their vessel, HUD and camera are theirs again", v != null && v.Being.Value.ToString() == RWTwo.SecondHero && Blockage.Menu.Bl_Menus.Menus.ContainsKey("Main") && Blockage.Menu.Bl_Menus.Menus["Main"].Active && Game_Cameracontroller.Instance != null && !Game_Cameracontroller.Instance.Movelock,
            "vessel " + (v != null ? v.Being.Value.ToString() : "-") + ", HUD " + (Blockage.Menu.Bl_Menus.Menus.ContainsKey("Main") ? Blockage.Menu.Bl_Menus.Menus["Main"].Active.ToString() : "-"));
        yield return new WaitForSecondsRealtime(3f);
        Check("O3 every thing on the map has its look on the rejoiner's machine", Lookless() == "", "without: " + Lookless());
        Check("O3 their towers and aura are as they left them", OwnCount() >= mineCount && Mathf.Abs(Wallet("Redaura") - red) < 0.01f, "things " + mineCount + " -> " + OwnCount() + ", red " + red + " -> " + Wallet("Redaura"));
        Check("O3 Pure Aura and Directions follow", RWMine.Pure() > 0f && v != null && HeldDirections(v).Count >= 2, "Pure " + RWMine.Pure() + ", " + (v != null ? string.Join(",", HeldDirections(v).ToArray()) : "-"));
        Check("O7 the weather's objects are there for the rejoiner", RWWeather.Loaded(RWChart.RisingWater) && Thing("Main_Flood") != null && RWRejoin.MapWeather() == RWChart.RisingWater, "weather " + RWWeather.Display(RWRejoin.MapWeather()));
        yield return Snap("second_rejoined");
        if (RWTwo.Review)
        {
            Reading("MODE-B1 the rejoiner", Mathf.Abs(Time.timeScale - 1.25f) > 0.01f, Speeds());
            int doubled;
            string doubles = Doubles(out doubled);
            Reading("MODE-B2", doubled > 0, doubles);
            Info("MODE-C10 the rejoiner's row", CardinalRow());
        }
        yield return Sync("back");
        float pureBack = RWMine.Pure();
        yield return Until(() => RWTwo.OtherSaid("paid"), 120f);
        yield return Say("paid");
        Check("O4 the rejoined player's counter rises with the Tide", RWMine.Pure() > pureBack, pureBack + " -> " + RWMine.Pure());
        if (RWTwo.Review) { yield return Stepped(ReviewSecond()); yield break; }
        // The host kills this copy here (O3b); the next launch carries on from AfterRestart.
        yield return new WaitForSecondsRealtime(600f);
    }

    // The restarted second player: same id, same seat.
    static IEnumerator AfterRestart()
    {
        yield return Until(() => LoadedHere(), 200f);
        Check("O3b the restarted game rejoins the running map", LoadedHere() && RWRejoin.Late, "Late " + RWRejoin.Late + ", session " + RWMine.Session);
        yield return Until(() => Game_Client_Init.Instance != null && Game_Client_Init.Instance.Gametimescale, 40f);
        yield return new WaitForSeconds(4f);
        Entity_Master v = RWMine.Vessel();
        yield return Until(() => Lookless() == "", 30f);
        Check("O3b every thing on the map has its look on the restarted machine", Lookless() == "", "without: " + Lookless());
        // R4 VFX: the halves already on the map when this copy joined have the stock sprite body (RWBody, 10_Online.cs).
        yield return Until(() => RWBody.Told, 15f);
        // Review: whether the boot's load was in at the join is timing (this copy rejoins as soon as it can), so it is read, not wanted.
        Check("O3b every thing on the restarted machine has the stock sprite body Vfx/Mobbase", RWBody.Told && RWBody.Things > 0 && RWBody.Missing == 0 && RWBody.Lookless == 0, RWBody.Line());
        Check("O7 the flood is there on the restarted machine", RWWeather.Loaded(RWChart.RisingWater) && Thing("Main_Flood") != null && RWRejoin.MapWeather() == RWChart.RisingWater, "weather " + RWWeather.Display(RWRejoin.MapWeather()));
        Check("O3b the same session and vessel come back, with what they held", v != null && v.Being.Value.ToString() == RWTwo.SecondHero && RWMine.Pure() > 0f && HeldDirections(v).Count >= 2 && OwnCount() > 1, "vessel " + (v != null ? v.Being.Value.ToString() : "-") + ", Pure " + RWMine.Pure() + ", things " + OwnCount());
        yield return Snap("second_restarted");
        yield return Sync("restarted", 300f);
    }

    // From the vote to leave on, with the same steps as the host's.
    static IEnumerator SecondFromVote()
    {
        yield return Until(() => RWAsk.Open == RWVote.LeaveKind, 400f);
        yield return WaitDialog(8f);
        yield return DialogFits("The second player's vote to leave");
        yield return Snap("second_leave");
        yield return Sync("leave1");
        ClickCard("Stay");
        yield return Until(() => RWAsk.Open != RWVote.LeaveKind, 40f);
        // B1: the vote on the next map, as the party stays.
        yield return Until(() => RWAsk.Open.StartsWith(RWVote.MapKind), 10f);
        if (RWAsk.Open.StartsWith(RWVote.MapKind))
        {
            yield return WaitDialog(8f);
            List<string> stayCards = CardNames();
            if (stayCards.Count > 0) ClickCard(stayCards[0]);
        }
        yield return Until(() => RWAsk.Open == RWVote.LeaveKind, 200f);
        yield return WaitDialog(8f);
        yield return Sync("leave2");
        // No answer: the dialog closes itself when the question does.
        yield return Until(() => RWAsk.Open != RWVote.LeaveKind, RWVote.LeaveSeconds + 20f);
        yield return new WaitForSeconds(1f);
        Check("A timed question closes itself on the machine that didn't answer", !RWDialog.Up(), "dialog up " + RWDialog.Up());
        yield return Until(() => RWAsk.Open == RWVote.LeaveKind, 200f);
        yield return WaitDialog(8f);
        yield return Sync("leave3");
        ClickCard("Leave");
        yield return Until(() => RWAsk.Open.StartsWith(RWVote.DirectionKind), 40f);
        yield return WaitDialog(8f);
        ClickCard("0");
        // R4 U3: the map this machine plays, read from what every machine has, for the title its end screen should show.
        int wonRow = RWCard.Row();
        // With the map voted when the party stayed (B1) no second vote comes.
        yield return Until(() => RWAsk.Open.StartsWith(RWVote.MapKind) || (Game_Mapvars.Instance != null && Game_Mapvars.Instance.Gamecomplete.Value.ToString() == "Win"), 40f);
        if (RWAsk.Open.StartsWith(RWVote.MapKind))
        {
            yield return WaitDialog(8f);
            List<string> cards = CardNames();
            yield return Sync("vote2");
            ClickCard(cards[cards.Count - 1]);
        }
        yield return Until(() => Game_Mapvars.Instance != null && Game_Mapvars.Instance.Gamecomplete.Value.ToString() == "Win", 40f);
        yield return OneWay("endscreen");
        GameObject title = null;
        Blockage.Menu.Blu2_Menu_Handler win;
        if (Blockage.Menu.Bl_Menus.Menus.TryGetValue("Win", out win) && win != null) win.Items.TryGetValue("Title", out title);
        // R4 P1-4: the title of the map won (not a fixed map 1), put on this friend's screen by its own hold (RWCard.ClientEnd)
        // from the synced Display_Endgame, whatever the stock screen read once (it read "BASIN BREACHED" in PC round 4).
        string wantEnd = WonTitle(wonRow);
        yield return Until(() => title != null && TextOf(title).ToUpperInvariant().Contains(wantEnd), 6f);
        string endSeen = "'" + TextOf(title) + "', want '" + wantEnd + "' (row " + wonRow + "); synced '" + (Game_Mapvars.Instance != null ? Game_Mapvars.Instance.Display.Value.Display_Endgame.ToString() : "-")
            + "', this machine's hold saw '" + RWCard.SeenEnd + "' and put the shown title right " + RWCard.ClientEndWrites + " times in all";
        if (wonRow < 1) Setup("U3 the second player's end screen reads the campaign's title", "this machine can't tell which chart map it plays: " + endSeen);
        else Check("U3 the second player's end screen reads the campaign's title", title != null && TextOf(title).ToUpperInvariant().Contains(wantEnd), endSeen);
        // O8: the rejoined player is on the end screen's list, with stats.
        Blockage.Menu.Blu2_Menu_Handler endstats;
        Func<int> statRows = () => Blockage.Menu.Bl_Menus.Menus.TryGetValue("Endstats", out endstats) && endstats != null && endstats.Items.ContainsKey("List") ? endstats.Items["List"].transform.childCount : -1;
        yield return Until(() => statRows() >= 2, 12f);
        Check("O8 the end screen lists both players on the rejoined machine", statRows() >= 2, statRows() + " rows, this player Activeplayer " + (Blm_Server.Instance.Localplayer != null ? Blm_Server.Instance.Localplayer.GetComponent<Tds_Clientinfo>().Activeplayer.Value.ToString() : "-"));
        yield return Snap("second_endscreen");
        Macro_Main old = Macro_Main.Instance;
        yield return Until(() => Macro_Main.Instance != null && Macro_Main.Instance != old && Macro_Main.Instance.Ready, 200f);
        Macro_Main boat = Macro_Main.Instance;
        Check("N12 the second player is back on the boat, no Disconnected", boat != null && Unity.Netcode.NetworkManager.Singleton.IsConnectedClient, "");
        yield return Until(() => Macro_Worldvisual.Instance != null && Macro_Worldvisual.Instance.World != null && Macro_Worldvisual.Instance.World.name == Tds_Multiplayerinfo.Instance.World.Value.ToString() && Macro_Worldvisual.Instance.World.name.Contains("_"), 40f);
        Check("C7 the chart one row on, on the second player's machine", Macro_Worldvisual.Instance.World.name == Tds_Multiplayerinfo.Instance.World.Value.ToString(), Macro_Worldvisual.Instance.World.name);
        yield return Until(() => Macro_Worldvisual.Instance.Insetup, 90f);
        yield return OneWay("map2");
        boat.Playgame(true);

        // Map 2, Quickening Current.
        yield return Until(() => LoadedHere(), 240f);
        yield return Until(() => RWTwo.OtherSaid("quick"), 240f);
        yield return Say("quick");
        Check("W2 Quickening Current's speed reaches the second player's machine", Mathf.Approximately(Game_Mapvars.Instance.Time_Scale.Value, RWWeather.Quick) && Mathf.Abs(Time.timeScale - RWWeather.Quick) < 0.01f, "Time_Scale " + Game_Mapvars.Instance.Time_Scale.Value + ", Time.timeScale " + Time.timeScale);
        yield return Say("quickdone");
        yield return OneWay("won2");
        old = Macro_Main.Instance;
        yield return Until(() => Macro_Main.Instance != null && Macro_Main.Instance != old && Macro_Main.Instance.Ready, 200f);
        boat = Macro_Main.Instance;
        yield return Until(() => Macro_Worldvisual.Instance != null && Macro_Worldvisual.Instance.Insetup, 120f);
        Check("W2 the speed is back to 1 on the boat", Mathf.Abs(Time.timeScale - 1f) < 0.01f || Time.timeScale == 0f, "Time.timeScale " + Time.timeScale);
        yield return OneWay("map3");
        boat.Playgame(true);

        // Map 3, Nesting Ground.
        yield return Until(() => LoadedHere(), 240f);
        yield return Until(() => RWTwo.OtherSaid("nest"), 240f);
        yield return Say("nest");
        Entity_Master nest = Thing("Tower_Halberdnest");
        Check("W3 the nest stands on the second player's machine", nest != null, nest != null ? "at " + RWUtil.V(nest.Entityposition.Value) : "none");
        yield return Snap("second_nest");
        yield return Say("nestdone");

        // O7 on a Nesting Ground map: dropped, and back.
        yield return Sync("dropnest");
        yield return Until(() => UnityEngine.Object.FindObjectOfType<Mainmenu_Main>() != null, 60f);
        yield return new WaitForSecondsRealtime(6f);
        yield return RWTwo.Join();
        yield return Until(() => LoadedHere(), 200f);
        yield return Until(() => Game_Client_Init.Instance != null && Game_Client_Init.Instance.Gametimescale, 40f);
        yield return Until(() => Lookless() == "", 30f);
        Entity_Master nestBack = Thing("Tower_Halberdnest");
        Check("O7 the nest stands for the rejoiner on a Nesting Ground map, and everything has its look", nestBack != null && RWRejoin.Late && RWWeather.Loaded(RWChart.NestingGround) && Lookless() == "", "nest " + (nestBack != null) + ", weather " + RWWeather.Display(RWRejoin.MapWeather()) + ", without: " + Lookless());
        yield return Snap("second_nest_rejoined");
        yield return Sync("nestback", 300f);
        old = Macro_Main.Instance;
        yield return Until(() => Macro_Main.Instance != null && Macro_Main.Instance != old && Macro_Main.Instance.Ready, 240f);
        Check("The host's Exit takes the second player back to the boat too", Macro_Main.Instance != null && Unity.Netcode.NetworkManager.Singleton.IsConnectedClient, "");
        yield return OneWay("exited");
        yield return Until(() => UnityEngine.Object.FindObjectOfType<Mainmenu_Main>() != null, 120f);
        yield return new WaitForSecondsRealtime(2f);
        Check("N13 the host leaving the boat lands the second player on the main menu", UnityEngine.Object.FindObjectOfType<Mainmenu_Main>() != null, string.Join(" | ", Blm_Multiplayer.Instance.Multiplayererror.ToArray()));
        yield return new WaitForSecondsRealtime(600f);
    }

    static IEnumerator SecondContinue()
    {
        yield return Until(() => Macro_Main.Instance != null && Macro_Main.Instance.Ready, 120f);
        Macro_Main boat = Macro_Main.Instance;
        Check("Continue: the second player is on the continued campaign's boat", boat != null && boat.Ready, PlayerList());
        if (boat == null) yield break;
        uint seed;
        List<int> sailed;
        RWWorld.Parse(Tds_Multiplayerinfo.Instance.World.Value.ToString(), out seed, out sailed);
        ForceWeather(seed);
        Check("Continue: the second player's chart stands where the campaign does", sailed.Count == 2 && Macro_Worldvisual.Instance.World != null && Macro_Worldvisual.Instance.World.name == Tds_Multiplayerinfo.Instance.World.Value.ToString(), Tds_Multiplayerinfo.Instance.World.Value.ToString());
        yield return OneWay("continued");
        yield return new WaitForSecondsRealtime(5f);
        boat.Selectcharacter("Hero_Smacky");
        yield return new WaitForSecondsRealtime(1f);
        yield return Say("swapped");
        yield return new WaitForSecondsRealtime(3f);
        Check("Vessels are locked in, on the second player's own HUD", Blm_Server.Instance.Localplayer.GetComponent<Tds_Clientinfo>().Hero.Value.ToString() == RWTwo.SecondHero, Blm_Server.Instance.Localplayer.GetComponent<Tds_Clientinfo>().Hero.Value.ToString());
        yield return Until(() => Macro_Worldvisual.Instance.Insetup, 90f);
        yield return OneWay("map3b");
        boat.Playgame(true);
        yield return Until(() => LoadedHere(), 240f);
        yield return Sync("replayed");
        Entity_Master v = null;
        yield return Until(() => (v = RWMine.Vessel()) != null && HeldDirections(v).Count >= 2, 30f);
        Check("Continue: the Directions show on the second player's vessel again", v != null && HeldDirections(v).Count >= 2 && RWMine.Pure() > 0f, v != null ? string.Join(",", HeldDirections(v).ToArray()) + ", Pure " + RWMine.Pure() : "no vessel");
        yield return Say("replaydone");
        yield return Sync("hostquits");
        yield return Until(() => UnityEngine.Object.FindObjectOfType<Mainmenu_Main>() != null, 90f);
        yield return new WaitForSecondsRealtime(3f);
        Check("N13 the host quitting mid-map lands the second player on the main menu with the way back", UnityEngine.Object.FindObjectOfType<Mainmenu_Main>() != null && ErrorBox().Contains("Join Game"), "'" + ErrorBox() + "'");
        yield return Snap("second_hostquit");
        yield return new WaitForSecondsRealtime(600f);
    }

    // O5: this copy wasn't in the map. Turned away, twice.
    static IEnumerator Stranger()
    {
        if (RWTwo.Review) { yield return Stepped(ReviewStranger()); yield break; }
        for (int i = 1; i <= 2; i++)
        {
            // R4 VFX: the boot's load of General_Default is in before the join, so the map's things find their body.
            yield return Until(() => RWBody.BootSeconds >= 0f, 60f);
            Check("O5 try " + i + ": Vfx/Mobbase is in before the stranger joins", RWBody.Body() != null, "boot load " + RWUtil.F(RWBody.BootSeconds) + " s");
            yield return RWTwo.Join();
            yield return Until(() => Unity.Netcode.NetworkManager.Singleton.IsConnectedClient, 30f);
            Info("the stranger", "try " + i + ": connected " + Unity.Netcode.NetworkManager.Singleton.IsConnectedClient);
            yield return new WaitForSecondsRealtime(4f);
            yield return Until(() => UnityEngine.Object.FindObjectOfType<Mainmenu_Main>() != null, 90f);
            Check("O5 try " + i + ": the stranger is back on their main menu", UnityEngine.Object.FindObjectOfType<Mainmenu_Main>() != null, string.Join(" | ", Blm_Multiplayer.Instance.Multiplayererror.ToArray()));
            yield return new WaitForSecondsRealtime(9f);
        }
        yield return new WaitForSecondsRealtime(600f);
    }

    // The main menu's error box takes the list's last line and clears the list (Bla_Ui_Multiplayererrornotif.Start).
    // What stands on the map on this machine with no client half: its being never reached it (RWRejoin.Looks).
    static string Lookless()
    {
        List<string> names = new List<string>();
        foreach (Entity_Master_Client c in UnityEngine.Object.FindObjectsOfType<Entity_Master_Client>())
            if (c.Master == null || (c.Beingobject == null && c.Master.Clientready.Value && c.Master.Being.Value.ToString() != ""))
                names.Add(c.Master != null ? c.Master.Being.Value.ToString() : c.name + " (no Master)");
        return string.Join(",", names.ToArray());
    }

    static string ErrorBox()
    {
        List<string> texts = new List<string>();
        foreach (Bla_Ui_Multiplayererrornotif box in Resources.FindObjectsOfTypeAll<Bla_Ui_Multiplayererrornotif>())
            if (box.gameObject.scene.IsValid() && box.Errormessage != null)
            {
                CanvasGroup g = box.Display != null ? box.Display.GetComponent<CanvasGroup>() : null;
                texts.Add(box.Errormessage.text + " [active " + box.gameObject.activeInHierarchy + ", scene " + box.gameObject.scene.name + ", alpha " + (g != null ? g.alpha.ToString() : "-") + "]");
            }
        return string.Join(" | ", texts.ToArray()) + "; list left: " + string.Join(" / ", Blm_Multiplayer.Instance.Multiplayererror.ToArray());
    }

    static List<string> HeldDirections(Entity_Master v)
    {
        List<string> held = new List<string>();
        if (v != null && v.Upgradelist != null)
            foreach (FixedString64Bytes u in v.Upgradelist)
                if (u.ToString().StartsWith(RogueWavesModeKitDirections.Prefix)) held.Add(u.ToString());
        return held;
    }

    static Entity_Master Vendcint() { return Thing(RWContent.Vendor); }

    static Entity_Master Thing(string being)
    {
        if (Game_Entity.Instance == null || Game_Entity.Instance.Entityclient == null) return null;
        Entity_Master last = null;
        foreach (Entity_Master e in Game_Entity.Instance.Entityclient)
            if (e != null && e.Being.Value.ToString() == being) last = e;
        return last;
    }

    static int OwnCount()
    {
        int n = 0;
        long me = RWMine.Session;
        if (Game_Entity.Instance != null && Game_Entity.Instance.Entityclient != null)
            foreach (Entity_Master e in Game_Entity.Instance.Entityclient)
                if (e != null && e.Entitydata.Value.Owner == me) n++;
        return n;
    }

    static Blu3_Button HudButton(string part)
    {
        Blockage.Menu.Blu2_Menu_Handler hud;
        if (!Blockage.Menu.Bl_Menus.Menus.TryGetValue("Main", out hud) || hud == null) return null;
        foreach (Blu3_Button b in hud.GetComponentsInChildren<Blu3_Button>(true))
            if (b.name.Contains(part)) return b;
        return null;
    }

    // The three Fund Transfer dialogs, from the second player's machine.
    static bool fundLooked;

    static IEnumerator FundDialogs(string amount, bool backFirst)
    {
        RWShop.Open("Fund");
        yield return WaitDialog(6f);
        List<string> friends = CardNames();
        string to = friends.Find(n => n != "Back");
        Check("Fund Transfer: the friend is on the first dialog", to != null && friends.Contains("Back"), CardTexts());
        if (!fundLooked) { yield return DialogFits("Fund Transfer, to whom"); yield return Snap("second_fund1"); }
        if (backFirst)
        {
            ClickCard("Back");
            yield return new WaitForSecondsRealtime(1f);
            Check("Fund Transfer: Back closes it and sends nothing", !RWDialog.Up(), "");
            RWShop.Open("Fund");
            yield return WaitDialog(6f);
        }
        ClickCard(to);
        yield return new WaitForSecondsRealtime(0.8f);
        yield return WaitDialog(6f);
        Check("Fund Transfer: the three colours are on the second dialog", CardNamed("Redaura") != null && CardNamed("Blueaura") != null && CardNamed("Yellowaura") != null, CardTexts());
        if (!fundLooked) { yield return DialogFits("Fund Transfer, which aura"); yield return Snap("second_fund2"); }
        ClickCard("Redaura");
        yield return new WaitForSecondsRealtime(0.8f);
        yield return WaitDialog(6f);
        Check("Fund Transfer: the amount " + amount + " is on the third dialog", CardNamed(amount) != null, CardTexts() + "; sub '" + TextOf(Bla_Ui_Confirmation.Instance.Subbox) + "'");
        if (!fundLooked) { yield return DialogFits("Fund Transfer, how much"); yield return Snap("second_fund3"); }
        fundLooked = true;
        ClickCard(amount);
        yield return new WaitForSecondsRealtime(0.5f);
    }

    // Every tower the vessel's sheet shows, by the Build order the HUD sends; one upgrade and one sale on the way.
    static IEnumerator SecondBuilds()
    {
        Entity_Master v = RWMine.Vessel();
        Heroentryobject entry = RWUtil.Item<Heroentryobject>("Hero", RWTwo.SecondHero);
        if (v == null || entry == null || entry.Towerdisplay == null) { Setup("the second player's buildings", "no vessel or sheet"); yield break; }
        List<Vector3> used = new List<Vector3>();
        Entity_Master_Remote remote = v.GetComponent<Entity_Master_Remote>();
        int team = v.Entitydata.Value.Team;
        long me = RWMine.Session;
        int built = 0, upgraded = 0;
        Entity_Master lastBuilt = null;
        foreach (string tower in new List<string>(entry.Towerdisplay))
        {
            Beingobject being = ModKitUtil.Load<Beingobject>("Mob", tower);
            if (being == null) { Info("building", tower + " isn't loaded on this machine"); continue; }
            string spot = RWTwo.HeardStarting("TWO H spot " + tower + " ");
            if (spot == null) { Info("building", "no tile for " + tower); continue; }
            string[] xyz = spot.Split(new[] { (char)32 });
            Vector3 at = new Vector3(float.Parse(xyz[xyz.Length - 3], System.Globalization.CultureInfo.InvariantCulture), float.Parse(xyz[xyz.Length - 2], System.Globalization.CultureInfo.InvariantCulture), float.Parse(xyz[xyz.Length - 1], System.Globalization.CultureInfo.InvariantCulture));
            float red0 = Wallet("Redaura") + Wallet("Blueaura") + Wallet("Yellowaura");
            int before = Count(tower);
            string pos = "{\"x\":" + RWUtil.F(at.x) + ",\"y\":" + RWUtil.F(at.y) + ",\"z\":" + RWUtil.F(at.z) + "}";
            remote.Command_ServerRpc("Build", "{\"Tower\":\"" + tower + "\",\"Position\":" + pos + ",\"Orientation\":0,\"Owner\":" + me + "}", default(Unity.Netcode.ServerRpcParams));
            yield return Until(() => Count(tower) > before, 15f);
            bool ok = Count(tower) > before;
            Info("building", tower + (ok ? " stands" : " did not appear") + ", wallet " + red0 + " -> " + (Wallet("Redaura") + Wallet("Blueaura") + Wallet("Yellowaura")));
            if (!ok) continue;
            built++;
            yield return new WaitForSeconds(1.5f);
            Entity_Master made = Newest(tower);
            if (made == null) continue;
            List<string> names = new List<string>();
            foreach (Link_Commandref c in made.Commandlist) names.Add(c.Name.ToString() + (c.Goalstring.ToString() != "" ? "=" + c.Goalstring : ""));
            Info("commands on " + tower, string.Join(", ", names.ToArray()));
            // Its first upgrade on sale, as its entry on the HUD sends it.
            string first = null;
            if (being.Entity != null && being.Entity.Upgrades != null)
                foreach (Upgradeproperties u in being.Entity.Upgrades)
                    if (u != null && u.Objectname.EndsWith("_Upgradeobject") && u.Priority >= 0 && first == null) first = u.Objectname;
            if (first == null) Info("upgrading", tower + ": none listed on its sheet");
            else
            {
                string id = first;
                Func<bool> holds = () => { foreach (FixedString64Bytes x in made.Upgradelist) if (x.ToString() == id) return true; return false; };
                made.GetComponent<Entity_Master_Remote>().Command_ServerRpc("Upgrade", "{\"Upgrade\":\"" + id + "\",\"Upgrader\":" + me + "}", default(Unity.Netcode.ServerRpcParams));
                yield return Until(holds, 4f);
                if (holds()) upgraded++;
                Info("upgrading", tower + " -> " + id + ": " + (holds() ? "shows on this machine" : "not taken") + ", wallet " + (Wallet("Redaura") + Wallet("Blueaura") + Wallet("Yellowaura")));
            }
            lastBuilt = made;
        }
        Check("N5 the second player builds through the order the HUD sends", built > 0, built + " of " + entry.Towerdisplay.Count + " towers of " + RWTwo.SecondHero);
        Check("N5 the second player's upgrades go through, and show on their machine", upgraded > 0, upgraded + " of " + built + " buildings upgraded");
        yield return Snap("second_built");
        if (lastBuilt != null)
        {
            float w0 = Wallet("Redaura") + Wallet("Blueaura") + Wallet("Yellowaura");
            Vector3 p = lastBuilt.Entityposition.Value;
            string what = lastBuilt.name;
            Entity_Master gone = lastBuilt;
            remote.Command_ServerRpc("Sell", "{\"Tower\":\"" + what + "\",\"Position\":" + "{\"x\":" + RWUtil.F(p.x) + ",\"y\":" + RWUtil.F(p.y) + ",\"z\":" + RWUtil.F(p.z) + "}" + ",\"Owner\":" + me + "}", default(Unity.Netcode.ServerRpcParams));
            yield return Until(() => gone == null || !gone.gameObject.activeInHierarchy, 15f);
            yield return new WaitForSeconds(0.6f);
            Check("N5 the second player sells a building, and is paid back", (gone == null || !gone.gameObject.activeInHierarchy) && Wallet("Redaura") + Wallet("Blueaura") + Wallet("Yellowaura") > w0, "sold " + what + ", wallet " + w0 + " -> " + (Wallet("Redaura") + Wallet("Blueaura") + Wallet("Yellowaura")));
        }
    }

    static int Count(string being)
    {
        int n = 0;
        long me = RWMine.Session;
        if (Game_Entity.Instance != null && Game_Entity.Instance.Entityclient != null)
            foreach (Entity_Master e in Game_Entity.Instance.Entityclient)
                if (e != null && e.Entitydata.Value.Owner == me && e.Being.Value.ToString() == being) n++;
        return n;
    }

    static Entity_Master Newest(string being)
    {
        Entity_Master last = null;
        long me = RWMine.Session;
        foreach (Entity_Master e in Game_Entity.Instance.Entityclient)
            if (e != null && e.Entitydata.Value.Owner == me && e.Being.Value.ToString() == being) last = e;
        return last;
    }
}
#endif
