// The run between the Tides, on the host (docs/scope/04 [Tides and Leaving], [Sea Chart], 05; docs/mode/SEA_CHART.md §4, route V1):
//   opening   a staging map holds its opening break for the Direction picks and the map 1 vote, then returns to the boat;
//             a map with picks still owed (a death in a bonus Tide, a late friend) holds them first
//   a break   pays the Tide, VENDCINT lands, an objective is posted; from Tide 15 on, once the field is clear, the map is
//             won and the players vote to leave: a majority leaves, nobody answering stays; the first stay votes on the next
//             map there and then, so the boat has a choice however the map ends (B1)
//   leaving   Direction picks, then the vote on the next map among the chart's links, then the stock win path; on the boat
//             the host's boat sails to the winner by itself. Map 10 has no next map: leaving it wins the campaign
//   bonus     the Tide table's Tide 29 is replayed for as long as they stay, each one harder (RWDifficulty)
// Each player's question is on their own vessel (RW_Ask: Goalstring what, Goalvalue the round), and every machine opens the
// choice dialog for its own; a pick comes back as an order. A timed question closes itself and takes "no answer".
public static class RWVote
{
    public const string LeaveKind = "L", DirectionKind = "D", MapKind = "M";
    public const float PickSeconds = 30f, MapSeconds = 25f, LeaveSeconds = 20f;
    public static string State = "";     // "", "Picks", "Map", "Leave" (self-test reads it)
    public static bool HoldBreak;        // the Tide clock is held while the party answers
    public static bool Departing;        // the party voted to leave
    public static int Round;             // a question's number, never repeated in a launch
    public static string LastVote = "";  // self-test
    public static readonly Dictionary<long, string> Votes = new Dictionary<long, string>();
    static readonly Dictionary<long, List<int>> offers = new Dictionary<long, List<int>>();
    static List<RWChart.Node> choices = new List<RWChart.Node>();
    static float until, holdAt;
    static int leaveWave = -1;
    static int lastBreak = -1;           // the Tide whose break was handled (a bonus rewind reads as a second break)
    static bool clearingWatch;
    static bool staging;
    static float startPickAt = -1f;
    static bool mapVoted;                // this map's party has picked the next map (B1)
    static bool stayVote;                // the map vote open now was asked when the party stayed (B1)
    public const string StayMark = "s";  // the map question's part that says so (a client before it skips the part)

    // ---------------------------------------------------------------- ModKitTides (host)

    public static void MapStart()
    {
        State = "";
        HoldBreak = Departing = false;
        Votes.Clear();
        leaveWave = lastBreak = -1;
        clearingWatch = false;
        startPickAt = -1f;
        mapVoted = stayVote = false;
        RWHits.Reset();
        RWDirections.Reset();
        RWCardinals.Reset();
        RWAny.Reset();
        RWBase.Reset();
        // An objective left running when the last map ended would be judged at this map's opening break (MODE-D2).
        RWObjectives.Reset();
        if (!RWRun.InCampaignMap)
            return;
        RWCardinals.RecordLobby();
        RWDirections.Hook();
        RWRun.Order -= OnOrder;
        RWRun.Order += OnOrder;
        RWRun.Order -= RWShop.OnOrder;
        RWRun.Order += RWShop.OnOrder;
        staging = RWCampaign.Staging && RWCampaign.Next == null;
        foreach (long s in RWRun.Sessions())
            if (staging) RWRun.For(s).Owed = true;
        RWCardinals.GrantAllCardinals();
        startPickAt = Time.time + 3f;   // the vessels are carrying their commands by then (RWRun.Tick)
        openingShop = false;
    }

    public static int Breaks;    // self-test

    public static void Break(int wave)
    {
        Breaks++;
        if (!RWRun.InCampaignMap)
            return;
        // RWDifficulty.Bonus rewinds Entry onto another break row, which ModKitTides reports as this Tide's break again:
        // handled once, or every bonus break from Tide 29 on pays twice (MODE-D1). A real break always follows a new Tide.
        if (wave > 0 && wave == lastBreak)
            return;
        lastBreak = wave;
        RWObjectives.Resolve();
        if (wave > 0)
        {
            RWRun.PayTide(wave);
            RWDirections.Interest(wave);
            RWAny.Tide(wave);
            RWDirections.TideEffects(wave);
        }
        foreach (long s in RWRun.Sessions())
            RWRun.For(s).Sent = 0f;
        // The opening break's table entry is up as the map loads, before the Basin and the vessels exist (seen on the PC).
        if (wave == 0)
            openingShop = true;
        else if (!staging)
            OpenShop(wave);
        if (wave >= RWCampaign.WinTide)
        {
            RWDifficulty.Bonus(wave);
            clearingWatch = true;
        }
    }

    public static void Tide(int wave)
    {
        lastBreak = -1;      // a Tide played again (a stock replay) earns its break again
        if (!RWRun.InCampaignMap)
            return;
        RWShop.Leave();
        CloseQuestion();
        ClearBoard();
        RWObjectives.Begin(wave);
        Line();
    }

    public static void MapEnd(string result)
    {
        RWShop.Leave();
        RWHits.Reset();
        State = "";
        HoldBreak = false;
    }

    // ---------------------------------------------------------------- every frame (host)

    public static void Update()
    {
        if (!RWRun.InCampaignMap || !ModKitUtil.IsServer || Game_Mapvars.Instance == null)
            return;
        Game_Mapvars vars = Game_Mapvars.Instance;
        RWWeather.DropDeadNests();
        // Once every vessel stands and carries its commands, and the map's fly-in (10 s, the HUD hidden) is over: the vessels'
        // start runs on the Tide clock, so nothing holds it before that (seen on the PC).
        if (startPickAt > 0f && Time.time >= startPickAt && ((AllStanding() && IntroOver(vars)) || Time.time >= startPickAt + 90f))
        {
            startPickAt = -1f;
            // The 90 s fallback can come after the opening break's clock ran out: VENDCINT doesn't land mid-Tide (MAX MODEB-7).
            if (openingShop && !staging && ModKitTides.InBreak)
                OpenShop(0);
            openingShop = false;
            OpeningPicks();
        }
        if (clearingWatch && ModKitTides.InBreak && Foes() == 0)
        {
            clearingWatch = false;
            if (!RWCampaign.WonThisMap)
                RWCampaign.Cleared();
            if (RWCampaign.WonThisMap && State == "" && !Departing)
                AskLeave(vars.Wave.Value);
        }
        if (HoldBreak && vars.Time_Wave.Value < holdAt)
            vars.Time_Wave.Value = holdAt;
        if (State == "")
            return;
        if (Everyone() || Time.time >= until)
        {
            if (State == "Picks") ClosePicks();
            else if (State == "Map") CloseMap();
            else if (State == "Leave") CloseLeave();
        }
    }

    public static int Foes()
    {
        int n = 0;
        List<Entity_Master> foes;
        if (Game_Entity.Instance == null || !Game_Entity.Instance.Entityclienttags.TryGetValue("Enemy", out foes))
            return 0;
        foreach (Entity_Master e in foes)
            if (e != null && e.Beingserver != null && !e.Beingserver.Dead && e.Entitydata.Value.Team == 0 && !e.Taglistserver.Contains("Enemycore") && !e.Taglistserver.Contains("Tower") && e != RWWeather.Flood)
                n++;
        return n;
    }

    static bool openingShop;

    static void OpenShop(int wave)
    {
        RWShop.Land();
        RWObjectives.Post(wave);
        PostBoard();
    }

    // The opening break's timer starts as the map loads.
    static bool IntroOver(Game_Mapvars vars)
    {
        List<Wavesobject.Waves> table = vars.Mode != null && vars.Mode.Enemies != null ? vars.Mode.Enemies.Wave : null;
        return table == null || table.Count == 0 || vars.Time_Wave.Value <= table[0].Wavetime - IntroSeconds;
    }

    public const float IntroSeconds = 14f;

    static bool AllStanding()
    {
        foreach (long s in RWRun.Sessions())
        {
            Entity_Master v = RWRun.Vessel(s);
            if (v == null || v.Commandlistserver == null || !v.Commandlistserver.ContainsKey(RWRun.AskLink))
                return false;
        }
        return true;
    }

    static bool Everyone()
    {
        foreach (long s in RWRun.Sessions())
            if (!Votes.ContainsKey(s)) return false;
        return true;
    }

    static void Hold(float seconds)
    {
        HoldBreak = true;
        holdAt = Mathf.Max(Game_Mapvars.Instance.Time_Wave.Value, seconds + 5f);
    }

    // ---------------------------------------------------------------- the picks

    // A map's opening break: a staging map and any pick still owed.
    static void OpeningPicks()
    {
        bool owed = false;
        foreach (long s in RWRun.Sessions())
            if (RWRun.For(s).Owed) owed = true;
        if (owed || staging)
            AskPicks();
        else
            Line();
    }

    static void AskPicks()
    {
        offers.Clear();
        Votes.Clear();
        State = "Picks";
        Round++;
        until = Time.time + PickSeconds;
        Hold(PickSeconds);
        foreach (long s in RWRun.Sessions())
        {
            RWPlayer p = RWRun.For(s);
            if (!p.Owed) { Votes[s] = ""; continue; }
            List<int> o = RWDirections.Roll(p, HeroOf(s), PickSeed(p));
            if (o.Count == 0) { p.Owed = false; Votes[s] = ""; continue; }
            offers[s] = o;
            AskOffers(s, p, o);
        }
        RWLog.Info("Direction picks open, round " + Round);
    }

    static string HeroOf(long s)
    {
        Blm_Player bp;
        Tds_Clientinfo info = Blm_Server.Instance.Sessionidsplayer.TryGetValue(s, out bp) && bp != null ? bp.GetComponent<Tds_Clientinfo>() : null;
        return info != null ? info.Hero.Value.ToString() : "";
    }

    static int PickSeed(RWPlayer p) { return unchecked((int)RWCampaign.Seed) ^ (RWCampaign.Route.Count * 613) ^ p.Key.GetHashCode(); }

    // The cards' indexes, and Spare Compass's redraw mark while the holder has one this map (a client before it skips the part).
    public const string RedrawMark = "r";
    public const int RedrawPick = -2;

    static void AskOffers(long s, RWPlayer p, List<int> o)
    {
        List<string> idx = new List<string>();
        foreach (int i in o) idx.Add(i.ToString());
        if (RWAny.CanRedraw(p)) idx.Add(RedrawMark);
        RWRun.Ask(s, DirectionKind + ":" + string.Join(".", idx.ToArray()), Round);
    }

    // Spare Compass rank II: the offer thrown back and drawn again, once a map; the clock gets a full pick's time back.
    static void Redraw(long s)
    {
        List<int> had;
        RWPlayer p = RWRun.For(s);
        if (!offers.TryGetValue(s, out had) || Votes.ContainsKey(s) || !RWAny.CanRedraw(p))
            return;
        // The player's dialog has closed on the click: it is always asked again, with the old cards when none other is left.
        List<int> o = RWDirections.Roll(p, HeroOf(s), PickSeed(p) ^ 7919, had);
        if (o.Count == 0)
            o = had;
        RWAny.Redrew(p);
        offers[s] = o;
        Round++;
        until = Mathf.Max(until, Time.time + PickSeconds);
        Hold(PickSeconds);
        AskOffers(s, p, o);
        Redraws++;
        RWLog.Info(p.Name + " drew the Directions again");
    }

    public static int Redraws;    // self-test

    static void ClosePicks()
    {
        foreach (KeyValuePair<long, List<int>> kv in offers)
        {
            RWPlayer p = RWRun.For(kv.Key);
            string pick;
            int k = -1;
            if (Votes.TryGetValue(kv.Key, out pick)) int.TryParse(pick, out k);
            if (k < 0 || k >= kv.Value.Count) k = 0;   // time ran out: the first card
            Upgradeproperties u = RWDirections.List[kv.Value[k]];
            RWDirections.Take(p, u);
            p.Owed = false;
            RWDirections.Sweep(p);
            RWRun.Ask(kv.Key, "", 0f);
        }
        State = "";
        Votes.Clear();
        if (staging || Departing)
            AskMap();
        else
        {
            HoldBreak = false;
            Line();
        }
    }

    // ---------------------------------------------------------------- the vote on the next map

    // stay: the party stayed for the bonus Tides, and votes on the next map now, so a bonus Tide lost or the host's quit
    // still leaves the boat a choice (B1, docs/scope/04 [Sea Chart]); the vote when it leaves is then skipped.
    static void AskMap(bool stay = false)
    {
        choices = RWCampaign.Open();
        if (!stay && mapVoted && RWCampaign.Choice >= 0)
        {
            RWChart.Node voted = null;
            foreach (RWChart.Node n in choices) if (n.Col == RWCampaign.Choice) voted = n;
            if (voted != null)
            {
                LastVote = "voted before: " + voted.Name;
                RWRejoin.Say("The party sails to " + Where(voted) + ", as voted.");
                Leave();
                return;
            }
        }
        if (choices.Count <= 1)
        {
            if (choices.Count == 1)
            {
                RWCampaign.Choice = choices[0].Col;
                RWCampaign.SaveChoice();
                mapVoted = true;
                LastVote = "only " + choices[0].Name;
            }
            if (stay) HoldBreak = false;
            else Leave();
            return;
        }
        Votes.Clear();
        State = "Map";
        stayVote = stay;
        Round++;
        until = Time.time + MapSeconds;
        Hold(MapSeconds);
        List<string> cols = new List<string>();
        foreach (RWChart.Node n in choices) cols.Add(n.Col.ToString());
        if (stay) cols.Add(StayMark);
        foreach (long s in RWRun.Sessions())
            RWRun.Ask(s, MapKind + ":" + string.Join(".", cols.ToArray()), Round);
        RWLog.Info("map vote open" + (stay ? " (staying)" : "") + ": " + string.Join(",", cols.ToArray()));
    }

    static string Where(RWChart.Node n)
    {
        return MapName(n) + (n.Weather != RWChart.Calm ? ", " + RWWeather.Display(n.Weather) : "");
    }

    // A map's card, as the vote shows it on every machine and the boat's ask on the host's.
    public static Bla_Ui_Confirmation.Entry MapCard(RWChart.Node n)
    {
        string weather = n.Weather != RWChart.Calm ? RWWeather.Display(n.Weather) : "Calm waters";
        return RWDialog.Card(n.Col.ToString(), MapName(n), weather + RWCard.Separator + "Danger " + n.Label, n.Weather != RWChart.Calm ? RWIcons.SpriteFor(n.Weather) : MapPicture(n), n.Breaker ? RWDialog.Coral : RWDialog.Teal);
    }

    static void CloseMap()
    {
        Dictionary<int, int> count = new Dictionary<int, int>();
        foreach (string v in Votes.Values)
        {
            int c;
            if (int.TryParse(v, out c))
            {
                int have;
                count.TryGetValue(c, out have);
                count[c] = have + 1;
            }
        }
        RWChart.Node best = choices[0];
        int top = 0;
        foreach (RWChart.Node n in choices)
        {
            int have;
            count.TryGetValue(n.Col, out have);
            if (have > top) { top = have; best = n; }
        }
        RWCampaign.Choice = best.Col;
        RWCampaign.SaveChoice();
        mapVoted = true;
        LastVote = best.Name + " with " + top;
        CloseQuestion();
        if (stayVote)
        {
            stayVote = false;
            RWRejoin.Say("When the party leaves, it sails to " + Where(best) + ".");
            HoldBreak = false;
            Line();
            return;
        }
        RWRejoin.Say("The party sails to " + Where(best) + ".");
        Leave();
    }

    // A calm map's card shows the map's own glyph, the one on its Setup panel, at the cards' 20 pixels a unit (its own sprite
    // is drawn for a larger panel, and comes out a speck on a card, seen on the PC).
    static readonly Dictionary<string, Sprite> glyphs = new Dictionary<string, Sprite>();

    public static Sprite MapPicture(RWChart.Node n)
    {
        Sprite made;
        if (glyphs.TryGetValue(n.Map, out made) && made != null)
            return made;
        Mapentryobject m = RWUtil.Item<Mapentryobject>("Map", n.Map);
        if (m == null || m.Display_Image == null || m.Display_Image.texture == null)
            return RWAssets.Sprite("RogueWaves_Vote");
        made = Sprite.Create(m.Display_Image.texture, m.Display_Image.rect, new Vector2(0.5f, 0.5f), 20f, 0, SpriteMeshType.FullRect);
        made.name = n.Map + "_Card";
        glyphs[n.Map] = made;
        return made;
    }

    public static string MapName(RWChart.Node n)
    {
        Mapentryobject m = RWUtil.Item<Mapentryobject>("Map", n.Map);
        return m != null && !string.IsNullOrEmpty(m.Display_Name) ? Bla_Localization.GetText(m.Display_Name) : n.Map.Replace("Map_", "");
    }

    // ---------------------------------------------------------------- the vote to leave

    static void AskLeave(int wave)
    {
        if (leaveWave == wave)
            return;
        leaveWave = wave;
        Votes.Clear();
        State = "Leave";
        Round++;
        until = Time.time + LeaveSeconds;
        Hold(LeaveSeconds);    // a vote that opens late in the break isn't cut off by the next Tide (MODE-D6)
        foreach (long s in RWRun.Sessions())
            RWRun.Ask(s, LeaveKind, Round);
        RWLog.Info("the vote to leave opens after Tide " + wave);
    }

    static void CloseLeave()
    {
        int yes = 0, all = 0;
        foreach (long s in RWRun.Sessions())
        {
            all++;
            string v;
            if (Votes.TryGetValue(s, out v) && v == "Leave") yes++;
        }
        LastVote = yes + " of " + all + " to leave";
        CloseQuestion();
        State = "";
        Votes.Clear();
        RWLog.Info("the vote to leave: " + LastVote);
        if (yes * 2 <= all)
        {
            if (!mapVoted && RWCampaign.Route.Count < RWChart.Rows)
                AskMap(true);
            else
                HoldBreak = false;
            return;
        }
        Departing = true;
        if (RWCampaign.Route.Count >= RWChart.Rows)
        {
            Leave();
            return;
        }
        foreach (long s in RWRun.Sessions())
            RWRun.For(s).Owed = true;
        AskPicks();
    }

    // The party leaves: the stock win path on a real map, the stock exit from a staging map.
    public static void Leave()
    {
        State = "";
        HoldBreak = false;
        RWRun.Save();
        if (staging)
        {
            staging = false;
            RWCampaign.Staging = false;
            RWCampaign.Next = null;
            RWCampaign.SaveChoice();    // a quit before map 1 must not bring the staging map back (MODE-A1)
            RWLog.Info("staging over: back to the boat, sailing to column " + RWCampaign.Choice);
            Tds_Multiplayer_Remote.Instance.Quitmap_ServerRpc(default(Unity.Netcode.ServerRpcParams));
            return;
        }
        Mode_Normal mode = Game_Rounds.Instance != null ? Game_Rounds.Instance.GetComponent<Mode_Normal>() : null;
        if (mode != null) mode.Finished = true;
        Tds_Multiplayerinfo.Instance.Gamestate.Value = new FixedString64Bytes("Win");
        Game_Mapvars.Instance.Gamecomplete.Value = new FixedString64Bytes("Win");
        Game_Rounds.Instance.Gamecompleted();
    }

    static void CloseQuestion()
    {
        if (State == "Leave" || State == "Map")
            foreach (long s in RWRun.Sessions())
                RWRun.Ask(s, "", 0f);
        if (State != "Picks")
            State = "";
    }

    // ---------------------------------------------------------------- orders (host)

    static void OnOrder(Entity_Master vessel, RWPlayer p, string json)
    {
        string kind = RWRun.JsonText(json, "Kind");
        long s = vessel.Entitydata.Value.Owner;
        if (kind == DirectionKind && State == "Picks")
        {
            int pick = RWRun.JsonInt(json, "Pick");
            if (pick == RedrawPick) Redraw(s);
            else Votes[s] = pick.ToString();
        }
        else if (kind == MapKind && State == "Map") Votes[s] = RWRun.JsonInt(json, "Pick").ToString();
        else if (kind == LeaveKind && State == "Leave") Votes[s] = RWRun.JsonText(json, "Text");
    }

    // ---------------------------------------------------------------- the board and the run line

    static void PostBoard() { Line(); }

    static void ClearBoard() { RWObjectives.Posted = null; }

    static void Line()
    {
        string line = RWObjectives.Active != null ? "Objective: " + RWObjectives.Active.Display : RWObjectives.Posted != null && ModKitTides.InBreak ? "Posted: " + RWObjectives.Posted.Display : "";
        foreach (long s in RWRun.Sessions())
            RWRun.RunLine(s, line);
    }
}

// This machine's side of the questions: a new round on its own vessel's RW_Ask opens the dialog; the pick is an order.
public static class RWAsk
{
    static float round = -1f;
    static float nextLook;
    public static string Open = "";     // what's being asked here (self-test)

    public static void Update()
    {
        if (Time.unscaledTime < nextLook)
            return;
        nextLook = Time.unscaledTime + 0.2f;
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "Game")
        {
            round = -1f;
            Open = "";
            vessel = null;
            return;
        }
        Entity_Master v = RWMine.Vessel();
        Link_Commandref ask;
        if (v == null || !RWMine.Command(v, RWRun.AskLink, out ask))
            return;
        // A respawned vessel's question is blank until the host writes the open one onto it (RWRun.Carry, twice a second).
        if (!ReferenceEquals(v, vessel))
        {
            vessel = v;
            vesselSince = Time.unscaledTime;
        }
        string what = ask.Goalstring.ToString();
        if (what == "")
        {
            if (Time.unscaledTime < vesselSince + 1.5f)
                return;
            if (Open != "")
            {
                Open = "";
                // Only this question's own dialog: a SHOP or FUND opened after answering stays (MODE-D7).
                if (running) RWDialog.Cancel();
            }
            round = -1f;
            return;
        }
        // Not during the map's fly-in: the stock HUD's own menu change would close the dialog, and nothing can be clicked.
        Blockage.Menu.Blu2_Menu_Handler hud;
        if (Blockage.Menu.Bl_Menus.Menus == null || !Blockage.Menu.Bl_Menus.Menus.TryGetValue("Main", out hud) || hud == null || !(hud.Active || RWDialog.Up()))
            return;
        // A new question, or one another menu closed before it was answered.
        if (ask.Goalvalue == round && (running || answered))
            return;
        round = ask.Goalvalue;
        Open = what;
        running = true;
        answered = false;
        RogueWavesModeMod.Instance.StartCoroutine(Run(what));
    }

    static bool running, answered;
    static Entity_Master vessel;
    static float vesselSince;

    static IEnumerator Run(string what)
    {
        string kind = what.Substring(0, 1);
        string[] parts = what.Length > 2 ? what.Substring(2).Split('.') : new string[0];
        List<Bla_Ui_Confirmation.Entry> cards = new List<Bla_Ui_Confirmation.Entry>();
        string title, sub;
        float seconds;
        if (kind == RWVote.DirectionKind)
        {
            RWDirections.Refresh();
            bool redraw = false;
            foreach (string p in parts)
            {
                int i;
                if (p == RWVote.RedrawMark) { redraw = true; continue; }
                if (!int.TryParse(p, out i) || i < 0 || i >= RWDirections.List.Count) continue;
                Upgradeproperties u = RWDirections.List[i];
                cards.Add(RWDialog.Card(cards.Count.ToString(), u.Display_Name, u.Display_Desc, RWDirections.Picture(u.Objectname), RWDialog.Teal));
            }
            if (redraw && cards.Count > 0)
                cards.Add(RWDialog.Card(RWVote.RedrawPick.ToString(), "Draw again", "Spare Compass: throw these back and draw new ones, once a map", RWDirections.Picture(ModKitDirections.Id("Any", "Compass")), RWDialog.Swell));
            title = "CHOOSE A DIRECTION";
            sub = "It stays with your vessel for the whole campaign";
            seconds = RWVote.PickSeconds;
        }
        else if (kind == RWVote.MapKind)
        {
            uint seed;
            List<int> route;
            RWWorld.Parse(Tds_Multiplayerinfo.Instance.World.Value.ToString(), out seed, out route);
            // After a won map the host's route is one longer than the world this map was warped from.
            RWChart.Chart chart = RWWorld.Chart(seed);
            // The staging map is never won, so its vote is on row 1 on every machine (MAX MODEB-3; the test is RWCard.Line's).
            string plan = Tds_Multiplayerinfo.Instance.Gameplan.Value.ToString();
            bool stagingMap = route.Count == 0 && plan == RWMode.Twin(RWWorld.StagingPlan(chart));
            int row = RWCampaign.Live ? RWCampaign.Route.Count + 1 : route.Count + (stagingMap ? 1 : 2);
            foreach (string p in parts)
            {
                int c;
                if (!int.TryParse(p, out c)) continue;
                RWChart.Node n = chart.At(row, c);
                if (n != null) cards.Add(RWVote.MapCard(n));
            }
            title = "WHERE NEXT?";
            sub = Array.IndexOf(parts, RWVote.StayMark) >= 0 ? "When the party leaves, it sails to the map most of you pick" : "The party sails to the map most of you pick";
            seconds = RWVote.MapSeconds;
        }
        else
        {
            cards.Add(RWDialog.Card("Stay", "Stay", "Another bonus Tide: more Pure Aura, and harder", RWAssets.Sprite("RogueWaves_Stay"), RWDialog.Teal));
            cards.Add(RWDialog.Card("Leave", "Leave", "Leave this map", RWAssets.Sprite("RogueWaves_Leave"), RWDialog.Coral));
            title = "TIDE CLEARED";
            sub = "A majority leaves. Nobody leaves right before their demise";
            seconds = RWVote.LeaveSeconds;
        }
        if (cards.Count == 0)
        {
            running = false;
            answered = true;
            yield break;
        }
        string pick = null;
        yield return RWDialog.Ask(cards, title, sub, seconds, n => pick = n);
        running = false;
        if (pick == null)
            yield break;
        answered = true;
        int k = 0;
        if (kind != RWVote.LeaveKind && !int.TryParse(pick, out k))
            yield break;
        // A pick made while this player's vessel is down goes out once it stands again and the host has written the same
        // question back onto it: a round closed meanwhile never reads as open on the new vessel (MODE-D4).
        float asked = round, until = Time.unscaledTime + seconds;
        while (!(StillAsked(what, asked) && RWMine.Send(kind, k, kind == RWVote.LeaveKind ? pick : "")))
        {
            if (Time.unscaledTime > until)
                yield break;
            yield return new WaitForSecondsRealtime(0.2f);
        }
    }

    static bool StillAsked(string what, float asked)
    {
        Link_Commandref ask;
        return RWMine.Command(RWMine.Vessel(), RWRun.AskLink, out ask) && ask.Goalvalue == asked && ask.Goalstring.ToString() == what;
    }
}
