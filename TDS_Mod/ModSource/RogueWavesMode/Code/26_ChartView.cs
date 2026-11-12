// The chart view (docs/mode/CHART_NAVIGATION.md §3, Dimitri's go of 2026-11-06): the whole sea chart on one screen, the
// way Slay the Spire shows its map, drawn in the game's own UI look (§3.6). Every machine draws it from what it already has
// (the chart world's name, the loaded map, the question on its own vessel), so nothing new crosses the network.
//   the map vote      the chart on the left half, beside the stock dialog's cards (moved to the right, RWDialog.Shift); its
//                     choices lit, lettered and bracketed; clicking a lit island answers the dialog as its card would
//                     (RWDialog.Choose), or, once the dialog has closed, sends a new vote while the question is open
//                     (RWAsk.Revote: the host keeps each player's last). Hovering an island hovers its card, so the
//                     dialog's own line names it. A pad keeps the cards (kit docs/20 §1.3); the island of the focused card
//                     is lit the same. The view stays until the question closes
//   on the boat       a mini chart where the game's pieces leave room (Mini), all the time on the chart; while the boat
//                     sails a banner in the HUD's "TIDE APPROACHING" shape names where it goes, read from its heading on each machine
//                     (the winner is never in the chart's name, which would warp the party again, §3.2)
//   any time          the keys ~M~ and Tab (Dimitri 2026-11-12) open the whole chart, read only, on the chart or in a
//                     map's break; not while typing, not while a choice dialog or the MVP board is up. Where the chart
//                     can't open (a Tide running), Tab is the MVP board's (28_BoardView.cs)
// Its own Canvas (Screen Space Overlay, kit docs/20 §4) at the game's UI scale, above the dialog. Clicks and hovers are read
// from the pointer here rather than through the EventSystem, so they never depend on the dialog's layer (the PC decides the
// rest: CV4, CV8). The layout is one pure function (Lay) the self-test calls.
public static class RWChartView
{
    public const float CardsAt = 0.75f;          // where across the screen the vote's cards stand beside the view
    const int Sorting = 120;                     // above the menu layers and the choice dialog's own canvas
    const float Margin = 12f;

    // docs/STYLE.md and CHART_NAVIGATION.md §3.6.
    static readonly Color Teal = RWDialog.Teal, Coral = RWDialog.Coral,
        Slate = new Color32(0x5B, 0x5D, 0x69, 255), Frame = new Color32(0xC8, 0xCC, 0xD0, 255), Ink = new Color32(0xF2, 0xF4, 0xF3, 255);

    // ---------------------------------------------------------------- the layout (no Unity objects: the self-test's CV1-CV3)

    public const int Faint = 0, Ahead = 1, Sailed = 2, Lit = 3;

    public class Tile
    {
        public RWChart.Node Node;
        public Vector2 At;
        public bool Sailed, Reach, Choice, Voted;
        public int Letter = -1;                  // 0 A, 1 B, 2 C for a choice
    }

    public class Lane
    {
        public RWChart.Node From, To;
        public int Kind;                         // Faint, Ahead (dashed), Sailed (solid), Lit (the hovered choice's way on, brighter)
    }

    public class Layout
    {
        public readonly List<Tile> Tiles = new List<Tile>();
        public readonly List<Lane> Lanes = new List<Lane>();
        public Tile Find(RWChart.Node n) { return Tiles.Find(t => t.Node == n); }
    }

    // sailed: the islands sailed to, from the harbour to where the party stands (here); choices: the lit islands, in the
    // dialog's order; via: a Cove the party already voted for, whose leg on is being asked; hover: the island lit with its
    // way on. Positions fill box (x0, y0, x1, y1), row 10 at the top.
    public static Layout Lay(RWChart.Chart chart, List<RWChart.Node> sailed, RWChart.Node here, List<RWChart.Node> choices, RWChart.Node via, RWChart.Node hover, Rect box)
    {
        Layout lay = new Layout();
        float minX = float.MaxValue, maxX = float.MinValue, minZ = float.MaxValue, maxZ = float.MinValue;
        foreach (List<RWChart.Node> row in chart.Rows)
            foreach (RWChart.Node n in row)
            {
                minX = Mathf.Min(minX, n.X); maxX = Mathf.Max(maxX, n.X);
                minZ = Mathf.Min(minZ, n.Z); maxZ = Mathf.Max(maxZ, n.Z);
            }
        List<RWChart.Node> reach = RWChart.Ahead(chart, here);
        List<RWChart.Node> cone = hover != null ? RWChart.Ahead(chart, hover) : new List<RWChart.Node>();
        foreach (List<RWChart.Node> row in chart.Rows)
            foreach (RWChart.Node n in row)
            {
                Tile t = new Tile { Node = n };
                t.At = new Vector2(box.xMin + (maxX > minX ? (n.X - minX) / (maxX - minX) : 0.5f) * box.width,
                    box.yMin + (maxZ > minZ ? (n.Z - minZ) / (maxZ - minZ) : 0.5f) * box.height);
                t.Sailed = sailed.Contains(n);
                t.Reach = reach.Contains(n);
                int i = choices.IndexOf(n);
                t.Choice = i >= 0;
                t.Letter = i;
                t.Voted = n == via;
                lay.Tiles.Add(t);
            }
        foreach (List<RWChart.Node> row in chart.Rows)
            foreach (RWChart.Node n in row)
                foreach (int k in n.Links)
                {
                    RWChart.Node to = chart.At(n.Row + 1, k);
                    if (to == null) continue;
                    int kind = Faint;
                    if (sailed.Contains(n) && sailed.Contains(to)) kind = Sailed;
                    else if (reach.Contains(n) && reach.Contains(to))
                        kind = (cone.Contains(n) && cone.Contains(to)) || (n == here && to == hover) || (via != null && ((n == here && to == via) || (n == via && to == hover))) ? Lit : Ahead;
                    lay.Lanes.Add(new Lane { From = n, To = to, Kind = kind });
                }
        return lay;
    }

    // The island the party stands on as this map sees it: the staging map stands at the harbour; in a chart map, the map
    // played (a client's chart name is a map behind, 20_Vote.cs); on the boat, the route's last.
    public static RWChart.Node Standing(RWChart.Chart chart, List<int> route, string plan, bool staging)
    {
        RWChart.Node at = RWWorld.Here(chart, route);
        if (staging || at == null)
            return at ?? chart.At(0, 0);
        foreach (RWChart.Node n in RWWorld.Choices(chart, route))
            if (n.Stop != RWChart.Cove && RWMode.Twin(n.Plan) == plan) return n;
        return at;
    }

    // The islands sailed to: the route's, then here if it stands one further.
    public static List<RWChart.Node> Path(RWChart.Chart chart, List<int> route, RWChart.Node here)
    {
        List<RWChart.Node> path = new List<RWChart.Node> { chart.At(0, 0) };
        for (int r = 0; r < route.Count; r++)
        {
            RWChart.Node n = chart.At(r + 1, route[r]);
            if (n != null) path.Add(n);
        }
        if (here != null && !path.Contains(here)) path.Add(here);
        return path;
    }

    // ---------------------------------------------------------------- what is open

    static GameObject root;
    static RectTransform area;
    static string mode = "";                     // "", "Vote", "Look" (the key), "Boat" (mini chart and banner)
    static string question = "";                 // the vote's question this view answers
    static RWChart.Chart chart;
    static RWChart.Node here, via, hover;
    static List<RWChart.Node> sailed = new List<RWChart.Node>(), choices = new List<RWChart.Node>();
    static RWChart.Node voted;
    static float closesAt;
    static readonly Dictionary<RWChart.Node, RectTransform> spots = new Dictionary<RWChart.Node, RectTransform>();
    static TMPro.TMP_Text clock;
    public static string Shown { get { return mode; } }   // self-test
    public static int Tiles, Lanes, LitTiles;              // what the last drawing held (self-test CV1-CV2)
    public static string Letters = "";

    public const string OnTheBoat = "boat";      // the question when the host is asked on the boat (RWBoat.AskOpen)

    // The map vote opened on this machine (RWAsk.Run, or the host's on the boat with asked OnTheBoat). False when nothing is drawn (no chart): the dialog stays centred.
    public static bool OpenVote(RWChart.Chart on, RWChart.Node standing, int row, List<int> cols, string asked, float seconds)
    {
        try
        {
            if (on == null || standing == null || cols.Count == 0 || !Canvasable())
                return false;
            chart = on;
            here = standing;
            choices = new List<RWChart.Node>();
            foreach (int c in cols) if (on.At(row, c) != null) choices.Add(on.At(row, c));
            via = null;
            // The leg after a Cove: the Cove is one row past here, the party's vote already on it.
            if (row == here.Row + 2)
                foreach (int k in here.Links)
                {
                    RWChart.Node n = on.At(here.Row + 1, k);
                    if (n != null && n.Stop == RWChart.Cove && choices.TrueForAll(c => n.Links.Contains(c.Col))) via = n;
                }
            uint seed;
            List<int> route;
            string world = asked == OnTheBoat && Macro_Worldvisual.Instance != null && Macro_Worldvisual.Instance.World != null ? Macro_Worldvisual.Instance.World.name : Tds_Multiplayerinfo.Instance.World.Value.ToString();
            RWWorld.Parse(world, out seed, out route);
            sailed = Path(on, route, here);
            voted = null;
            hover = null;
            question = asked;
            closesAt = Time.unscaledTime + seconds;
            mode = "Vote";
            Draw();
            lastVote = new Vote_ { On = on, Standing = standing, Row = row, Cols = new List<int>(cols), Asked = asked, ClosesAt = closesAt };
            return true;
        }
        catch (Exception e)
        {
            RWLog.Once("chart view", "chart view: " + e);
            Close();
            return false;
        }
    }

    // The pick this machine sent, from the dialog or an island: its brackets brighter.
    public static void Picked(string name)
    {
        int col;
        if (mode != "Vote" || name == null || !int.TryParse(name, out col))
            return;
        RWChart.Node n = choices.Find(c => c.Col == col);
        if (n != null && n != voted)
        {
            voted = n;
            Draw();
        }
    }

    // The vote the view last drew, so that it comes back while its WHERE NEXT? still stands beside where it was (Dimitri
    // 2026-11-12: "WHERE NEXT should open the map automatically"; chart_round1's 021312 caught the cards with no chart).
    class Vote_ { public RWChart.Chart On; public RWChart.Node Standing; public int Row; public List<int> Cols; public string Asked; public float ClosesAt; }
    static Vote_ lastVote;

    static bool Alive(string asked, string scene)
    {
        return asked == OnTheBoat ? RWDialog.Asking : scene == "Game" && RWAsk.Open == asked;
    }

    // The keys answered this frame (open or close): the MVP board, after this view in the frame, leaves Tab alone.
    public static int KeyFrame { get; private set; }

    public static void Close()
    {
        if (root != null) UnityEngine.Object.Destroy(root);
        root = null;
        area = null;
        clock = null;
        spots.Clear();
        mode = "";
        question = "";
        hover = null;
        RWDialog.Hover(null);
    }

    // ---------------------------------------------------------------- every frame (every machine)

    static float boatLook;
    static Vector3 boatWas;
    static RWChart.Node heading;                 // where the boat sails, read from its heading
    static string boatWorld = "";
    static float bannerUntil;
    static string bannerTitle = "", bannerLine = "";

    public static void Update()
    {
        string scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (lastVote != null && !Alive(lastVote.Asked, scene))
            lastVote = null;
        // The map vote's dialog stands beside an empty half (the view closed or its canvas went while the question is
        // still asked): the chart is drawn again, with the time it has left.
        if (lastVote != null && RWDialog.Beside && (mode != "Vote" || root == null))
        {
            Vote_ v = lastVote;
            if (mode != "") Close();
            if (!OpenVote(v.On, v.Standing, v.Row, v.Cols, v.Asked, Mathf.Max(0f, v.ClosesAt - Time.unscaledTime)))
                lastVote = null;
            else
                RWLog.Info("chart view: drawn again beside the map vote");
        }
        if (mode == "Vote")
        {
            if (!Alive(question, scene))
            {
                Close();
                return;
            }
            // Another dialog over the vote (the SHOP, FUND, the game's own): the view steps aside until it closes.
            bool other = RWDialog.Up() && !RWDialog.Beside;
            if (root != null && root.activeSelf == other) root.SetActive(!other);
            if (other)
            {
                if (hover != null) { hover = null; RWDialog.Hover(null); }
                return;
            }
            Pointer();
            int left = Mathf.Max(0, Mathf.CeilToInt(closesAt - Time.unscaledTime));
            if (clock != null && (left != clockLeft || voted != clockVoted))
            {
                clockLeft = left;
                clockVoted = voted;
                string text = question == OnTheBoat ? "The host picks where the boat sails"
                    : (voted != null ? "Your vote: " + "ABC".Substring(Mathf.Max(0, choices.IndexOf(voted)), 1) + RWCard.Separator : "") + "A tie goes to A" + RWCard.Separator + "no votes take A" + RWCard.Separator + left + " s";
                clock.text = text;
            }
            return;
        }
        if (Key())
        {
            if (mode == "Look")
            {
                Close();
                KeyFrame = Time.frameCount;
                return;
            }
            OpenLook(scene);
            if (mode == "Look") KeyFrame = Time.frameCount;
            if (mode == "Look" || !TabOnly()) return;
        }
        if (mode == "Look")
        {
            if (RWDialog.Up() || (scene == "Game" ? !ModKitTides.InBreak : !RWBoat.OnChart)) Close();
            return;
        }
        Boat(scene);
    }

    // The keys, on this machine: ~M~ and Tab, not while a text field (chat) has the focus or a choice dialog or the MVP board is up.
    static bool Key()
    {
        UnityEngine.InputSystem.Keyboard k = UnityEngine.InputSystem.Keyboard.current;
        return k != null && (k[UnityEngine.InputSystem.Key.M].wasPressedThisFrame || k[UnityEngine.InputSystem.Key.Tab].wasPressedThisFrame)
            && !RWDialog.Up() && !RWBoardView.Up && !Typing();
    }

    // Only Tab was pressed (no chart opened for it): the frame goes on, so the mini chart is kept.
    static bool TabOnly()
    {
        UnityEngine.InputSystem.Keyboard k = UnityEngine.InputSystem.Keyboard.current;
        return k != null && !k[UnityEngine.InputSystem.Key.M].wasPressedThisFrame;
    }

    // A text field (the chat box) has the focus: keys are the player's words, not ours (the MVP board asks too).
    public static bool Typing()
    {
        GameObject sel = UnityEngine.EventSystems.EventSystem.current != null ? UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject : null;
        return sel != null && (sel.GetComponent<TMPro.TMP_InputField>() != null || sel.GetComponent<UnityEngine.UI.InputField>() != null);
    }

    static void OpenLook(string scene)
    {
        Tds_Multiplayerinfo info = Tds_Multiplayerinfo.Instance;
        if (info == null || !Canvasable())
            return;
        string world = info.World.Value.ToString();
        if (scene == "Game")
        {
            if (!ModKitTides.InBreak || !info.Gameplan.Value.ToString().StartsWith(RWData.PlanPrefix))
                return;
        }
        else if (!RWBoat.OnChart)
            return;
        else
            world = Macro_Worldvisual.Instance.World.name;
        RWChart.Chart on = RWWorld.ChartOf(world);
        uint seed;
        List<int> route;
        if (on == null || !RWWorld.Parse(world, out seed, out route))
            return;
        string plan = info.Gameplan.Value.ToString();
        bool staging = scene == "Game" && route.Count == 0 && plan == RWMode.Twin(RWWorld.StagingPlan(on));
        chart = on;
        here = scene == "Game" ? Standing(on, route, plan, staging) : RWWorld.Here(on, route);
        if (here == null)
            return;
        sailed = Path(on, route, here);
        choices = new List<RWChart.Node>();
        via = hover = voted = null;
        mode = "Look";
        Draw();
    }

    // On the boat: the mini chart, and while the boat sails, the banner.
    static void Boat(string scene)
    {
        Macro_Worldvisual vis = Macro_Worldvisual.Instance;
        if (!RWBoat.OnChart || vis == null || vis.Boat == null || vis.Inworldtransition)
        {
            if (mode == "Boat") Close();
            boatWorld = "";
            return;
        }
        string world = vis.World.name;
        bool redraw = false;
        if (world != boatWorld)
        {
            uint seed;
            List<int> route;
            int retries;
            bool stops;
            if (!RWWorld.Parse(world, out seed, out route, out retries, out stops))
                return;
            RWChart.Chart on = RWWorld.Chart(seed, stops);
            RWChart.Node at = RWWorld.Here(on, route);
            boatWorld = world;
            boatChart = on;
            boatAt = at;
            chart = on;
            here = at;
            sailed = Path(on, route, at);
            choices = RWWorld.Choices(on, route);
            heading = null;
            boatWas = vis.Boat.transform.position;
            redraw = true;
            // A new name that stands on a Cove: the party rested there (RWCampaign.Rest).
            if (at != null && at.Stop == RWChart.Cove)
                Banner("THE COVE", "The party rests" + RWCard.Separator + RWCard.Retries(Mathf.Max(0, retries)), 4f);
        }
        if (Time.unscaledTime >= boatLook)
        {
            boatLook = Time.unscaledTime + 0.5f;
            RWChart.Node to = Heading(vis, boatChart, boatAt);
            if (to != heading)
            {
                heading = to;
                redraw = true;
                if (to != null)
                    Banner(to.Stop == RWChart.Cove ? "SAILING TO THE COVE" : "SAILING TO " + RWVote.MapName(to).ToUpperInvariant(), Says(to), 0f);
            }
        }
        // The game's pieces on the boat screen come and go (the lobby line, a player's card, a menu sliding in): the mini chart
        // looks again each second, and moves only when its spot is no longer clear.
        if (!vis.Insetup && Time.unscaledTime >= miniLook && Canvasable())
        {
            miniLook = Time.unscaledTime + 1f;
            if (Mini(world)) redraw = true;
        }
        bool sailing = heading != null && !vis.Insetup;
        if (!sailing && Time.unscaledTime >= bannerUntil) bannerTitle = "";
        if (mode != "Boat" || redraw || vis.Insetup != drawnSetup || bannerTitle != drawnTitle)
        {
            drawnSetup = vis.Insetup;
            drawnTitle = bannerTitle;
            if (!Canvasable())
                return;
            mode = "Boat";
            Draw();
        }
    }

    static bool drawnSetup;
    static int clockLeft = -1;                   // the footer is rewritten only when its second or the vote changes
    static RWChart.Node clockVoted;
    static string drawnTitle = "";
    static RWChart.Chart boatChart;
    static RWChart.Node boatAt;

    static int RetriesOf(string world)
    {
        uint seed;
        List<int> route;
        int retries;
        RWWorld.Parse(world, out seed, out route, out retries);
        return Mathf.Max(0, retries);
    }

    static void Banner(string title, string line, float seconds)
    {
        if (title != bannerTitle) RWLog.Info("chart view: banner '" + title + "', " + line + (RWUtil.IsHost ? " (host)" : " (client, from the boat's heading)"));   // CV6's reading on every machine
        bannerTitle = title;
        bannerLine = line;
        bannerUntil = seconds > 0f ? Time.unscaledTime + seconds : 0f;
    }

    public static string BannerTitle { get { return bannerTitle; } }   // self-test CV6

    static string Says(RWChart.Node n)
    {
        if (n.Stop == RWChart.Cove)
            return "Row " + n.Row + " of " + RWChart.Rows + RWCard.Separator + "No map: the party rests";
        string s = "Row " + n.Row + " of " + RWChart.Rows + RWCard.Separator + "Danger " + n.Label;
        if (n.Stop == RWChart.Wreck) s += RWCard.Separator + "A Wreck: one row harder, pays a Trinket";
        else if (n.Weather != RWChart.Calm) s += RWCard.Separator + RWWeather.Display(n.Weather);
        if (n.Breaker) s += RWCard.Separator + "Closes stretch " + RWCard.StretchOf(n.Row);
        return s;
    }

    // Where the boat is going: of the islands it may sail to, the one its last half second of travel points at most nearly.
    // The choices are a lane apart, so a heading names one (§3.2); none while it stands. The host knows it already.
    static RWChart.Node Heading(Macro_Worldvisual vis, RWChart.Chart on, RWChart.Node at)
    {
        Vector3 now = vis.Boat.transform.position, moved = now - boatWas;
        boatWas = now;
        if (RWUtil.IsHost && RWBoat.Sailing != "")
            return on.Find(RWBoat.Sailing);
        moved.y = 0f;
        if (at == null || moved.magnitude < 0.3f || vis.Boatspeed < 1f)
            return vis.Boatspeed < 1f ? null : heading;
        RWChart.Node best = null;
        float most = 0.8f;
        foreach (RWChart.Node n in choices)
        {
            Vector3 way = RWWorld.At(n) - now;
            way.y = 0f;
            float cos = Vector3.Dot(way.normalized, moved.normalized);
            if (cos > most) { most = cos; best = n; }
        }
        return best;
    }

    // ---------------------------------------------------------------- the pointer (the vote)

    static void Pointer()
    {
        UnityEngine.InputSystem.Mouse m = UnityEngine.InputSystem.Mouse.current;
        RWChart.Node over = null;
        if (m != null)
        {
            Vector2 at = m.position.ReadValue();
            foreach (RWChart.Node c in choices)
            {
                RectTransform rt;
                if (spots.TryGetValue(c, out rt) && rt != null && RectTransformUtility.RectangleContainsScreenPoint(rt, at, null)) over = c;
            }
        }
        // A pad: the card holding the focus lights its island.
        string focused = RWDialog.Focused();
        int fc;
        RWChart.Node padNow = focused != "" && int.TryParse(focused, out fc) ? choices.Find(c => c.Col == fc) : null;
        RWChart.Node want = over ?? padNow;
        if (want != hover)
        {
            hover = want;
            RWDialog.Hover(over != null ? over.Col.ToString() : null);
            Draw();
        }
        if (over != null && m.leftButton.wasPressedThisFrame)
            Click(over);
    }

    // A click on a lit island: the dialog's card while it is up, else a new vote while the question is open.
    static void Click(RWChart.Node island)
    {
        string name = island.Col.ToString();
        if (!RWDialog.Choose(name) && question != OnTheBoat)
            RWAsk.Revote(question, island.Col);
        Picked(name);
        RWLog.Info("chart view: voted for " + island.Name + " by its island");
    }

    // The self-test's click (CV4): the lit island of this column, as the pointer would.
    public static bool Click(int col)
    {
        RWChart.Node n = mode == "Vote" ? choices.Find(c => c.Col == col) : null;
        if (n == null)
            return false;
        Click(n);
        return true;
    }

    public static bool Up { get { return root != null; } }

    // ---------------------------------------------------------------- drawing

    public static bool Canvasable() { return Blockage.Menu.Bl_Menus.Ui != null; }

    public static float UiScale()
    {
        UnityEngine.UI.CanvasScaler s = Blockage.Menu.Bl_Menus.Ui != null ? Blockage.Menu.Bl_Menus.Ui.GetComponent<UnityEngine.UI.CanvasScaler>() : null;
        return s != null && s.scaleFactor > 0f ? s.scaleFactor : 3f;
    }

    static TMPro.TMP_FontAsset font;
    static Material fontMaterial;
    public static TMPro.TMP_FontAsset GameFont { get { Font(); return font; } }      // the MVP board's text too (RWBoardView)
    public static Material GameFontMaterial { get { Font(); return fontMaterial; } }

    // The game's own font, from any label of the scene's UI (kit docs/20 §4).
    static void Font()
    {
        if (font != null)
            return;
        TMPro.TMP_Text any = Blockage.Menu.Bl_Menus.Ui != null ? Blockage.Menu.Bl_Menus.Ui.GetComponentInChildren<TMPro.TMP_Text>(true) : null;
        if (any == null) return;
        font = any.font;
        fontMaterial = any.fontSharedMaterial;
    }

    static void Draw()
    {
        if (root != null) UnityEngine.Object.Destroy(root);
        spots.Clear();
        clock = null;
        Font();
        root = new GameObject("RW_ChartView", typeof(RectTransform));
        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = Sorting;
        UnityEngine.UI.CanvasScaler scaler = root.AddComponent<UnityEngine.UI.CanvasScaler>();
        float ui = UiScale();
        scaler.scaleFactor = ui;
        root.AddComponent<UnityEngine.UI.GraphicRaycaster>();   // the shade takes the clicks on the chart's half, so none reach the game under it
        float W = Screen.width / ui, H = Screen.height / ui;
        area = Box(root.transform, "Area", new Rect(0f, 0f, W, H), new Color(0f, 0f, 0f, 0f));
        if (mode == "Vote")
        {
            Box(area, "Shade", new Rect(0f, 0f, W * 0.5f, H), new Color(0f, 0f, 0f, 0.28f)).GetComponent<UnityEngine.UI.Image>().raycastTarget = true;
            Full(new Rect(Margin, Margin, W * 0.5f - Margin * 1.5f, H - Margin * 2f), true);
        }
        else if (mode == "Look")
        {
            float wide = Mathf.Min(W - Margin * 2f, H * 0.95f);
            Box(area, "Shade", new Rect(0f, 0f, W, H), new Color(0f, 0f, 0f, 0.45f));
            Full(new Rect((W - wide) * 0.5f, Margin, wide, H - Margin * 2f), false);
        }
        else if (mode == "Boat")
            BoatLook(W, H);
    }

    // The whole chart: title and rule, caption, rail, lanes, islands, legend, footnote.
    static void Full(Rect r, bool vote)
    {
        float title = Mathf.Clamp(r.height * 0.04f, 9f, 16f), cap = title * 0.55f;
        RuledTitle(r, "SEA CHART", title);
        int retries = RetriesOf(Tds_Multiplayerinfo.Instance != null ? Tds_Multiplayerinfo.Instance.World.Value.ToString() : "");
        if (mode == "Look" && RWBoat.OnChart) retries = RetriesOf(Macro_Worldvisual.Instance.World.name);
        int row = Mathf.Min(RWChart.Rows, here.Row + 1);
        Text(area, "Caption", "Row " + row + " of " + RWChart.Rows + RWCard.Separator + "Stretch " + RWCard.StretchOf(row) + RWCard.Separator
            + RWCard.Retries(retries) + RWCard.Separator + "Seed " + chart.Seed, new Vector2(r.xMin + 4f, r.yMax - title * 1.45f - cap * 1.6f), cap, TMPro.TextAlignmentOptions.Left, Ink, 0.75f);
        float legend = cap * 3.2f;
        Rect map = new Rect(r.xMin + cap * 3.2f, r.yMin + legend + cap * 1.5f, r.width - cap * 3.2f - title * 1.6f, r.height - title * 1.45f - cap * 4.5f - legend - cap * 2.5f);
        Layout lay = Lay(chart, sailed, here, choices, via, hover, map);
        float gap = map.height / RWChart.Rows, T = Mathf.Clamp(gap * 0.56f, 9f, 20f);
        // The rail: row numbers, a thin rule beside each stretch, the Breaker rows coral.
        for (int rr = 0; rr <= RWChart.Rows; rr++)
        {
            float y = 0f;
            foreach (RWChart.Node n in chart.Rows[rr]) y += lay.Find(n).At.y;
            y /= chart.Rows[rr].Count;
            bool breaker = rr > 0 && rr % RWChart.Stretch == 0;
            Text(area, "Row" + rr, rr == 0 ? "H" : rr.ToString(), new Vector2(r.xMin + cap * 1.2f, y), cap * 1.1f, TMPro.TextAlignmentOptions.Center, breaker ? Coral : Ink, breaker ? 0.9f : rr == here.Row + 1 ? 0.95f : 0.5f);
        }
        for (int s = 0; s < 2; s++)
        {
            Tile a = lay.Find(chart.At(1 + s * RWChart.Stretch, 0)), b = lay.Find(chart.At(RWChart.Stretch * (s + 1), 0));
            Line(area, "Stretch" + s, new Vector2(r.xMin + 2f, a.At.y + 4f), new Vector2(r.xMin + 2f, b.At.y - 4f), 1f, Ink, 0.35f);
        }
        Tile last = lay.Find(chart.At(RWChart.Rows, 0));
        Text(area, "Last", "THE LAST MAP", new Vector2(map.center.x, last.At.y + T * 0.9f + cap), cap * 0.9f, TMPro.TextAlignmentOptions.Center, Coral, 0.85f);
        Lanes = Tiles = LitTiles = 0;
        foreach (Lane l in lay.Lanes)
        {
            Vector2 a = lay.Find(l.From).At, b = lay.Find(l.To).At;
            if (l.Kind == Sailed) Line(area, "Lane", a, b, 1.6f, Ink, 0.92f);
            else if (l.Kind == Faint) Line(area, "Lane", a, b, 1f, Ink, 0.14f);
            else Dashed(area, a, b, l.Kind == Lit ? 1.4f : 1f, l.Kind == Lit ? 0.92f : 0.45f);
            Lanes++;
        }
        List<string> letters = new List<string>();
        foreach (Tile t in lay.Tiles)
        {
            bool harbour = t.Node.Row == 0;
            Color fill = harbour ? Teal : t.Sailed ? Slate : t.Node.Breaker ? Coral : Teal;
            float a = t.Reach || t.Sailed ? 1f : 0.32f, size = T + (t.Choice ? 3f : 0f);
            RectTransform tile = Framed(area, t.Node.Name, t.At, size, fill, a);
            spots[t.Node] = tile;
            Tiles++;
            if (harbour) Icon(tile, RWIcons.Chart("Harbour"), size, a);
            else if (t.Sailed) Icon(tile, RWIcons.Chart("Tick"), size, a);
            else if (t.Node.Stop != RWChart.NoStop) Icon(tile, RWIcons.Stop(t.Node.Stop), size, a);
            else Text(tile, "Danger", t.Node.Label.ToString(), new Vector2(size * 0.5f, size * 0.5f), size * 0.5f, TMPro.TextAlignmentOptions.Center, Ink, a);
            if (t.Node.Weather != RWChart.Calm && !t.Sailed && T >= 12f)
            {
                Image(tile, "Weather", RWIcons.SpriteFor(t.Node.Weather), new Rect(size - 4f, size - 4f, 10f, 10f), a);
            }
            if (t.Choice || t.Voted)
            {
                LitTiles++;
                bool strong = t.Node == voted || t.Node == hover || t.Voted;
                float s = size * 0.5f + (strong ? 5f : 3.5f);
                Brackets(area, t.At, s, strong ? 1f : 0.7f, strong ? 5f : 3.5f);
                if (t.Letter >= 0)
                {
                    string letter = "ABC".Substring(Mathf.Min(2, t.Letter), 1);
                    letters.Add(letter + t.Node.Col);
                    Text(area, "Letter", letter, new Vector2(t.At.x - s - cap * 0.9f, t.At.y), cap * 1.2f, TMPro.TextAlignmentOptions.Center, Ink, 0.95f);
                }
            }
        }
        Letters = string.Join(",", letters.ToArray());
        // You are here: the boat beside the island the party stands on.
        Tile me = lay.Find(here);
        if (me != null)
        {
            Image(area, "Boat", RWIcons.Chart("Boat"), new Rect(me.At.x + T * 0.5f + 3f, me.At.y - 2f, 12f, 12f), 1f);
            Text(area, "Here", "YOU ARE HERE", new Vector2(me.At.x + T * 0.5f + 18f, me.At.y + 4f), cap * 0.85f, TMPro.TextAlignmentOptions.Left, Ink, 0.85f);
        }
        // The legend, one caption line: tiles and words.
        // Its words at the width this view's Text gives them (0.62 of a size a letter): at 0.5, "Sailed" ran over the
        // frame's right line (chart_round1's 020735, Dimitri 2026-11-12: "some UI overlaps"). A line too long for the
        // frame is drawn smaller. No text is measured: asking TMP froze a frame (11_Dialog.cs, HeadLetter).
        string[] words = { "Map (its Danger)", "Breaker", "Cove: rest", "Wreck: harder, a Trinket", "Sailed" };
        float wide = 0f;
        foreach (string w in words) wide += LegendWide(w, cap);
        // Only the sizes shrink, not each item's 3-unit gap; the last item's trailing room isn't drawn.
        float gaps = 3f * words.Length;
        float legendSize = cap * Mathf.Clamp(((r.xMax - 6f) - (r.xMin + 4f) - gaps) / Mathf.Max(1f, wide - gaps - cap * 1.4f), 0.5f, 1f);
        float lx = r.xMin + 4f, ly = r.yMin + legend - cap;
        LegendItem(ref lx, ly, Teal, null, words[0], legendSize);
        LegendItem(ref lx, ly, Coral, null, words[1], legendSize);
        LegendItem(ref lx, ly, Teal, RWIcons.Chart("Cove"), words[2], legendSize);
        LegendItem(ref lx, ly, Teal, RWIcons.Chart("Wreck"), words[3], legendSize);
        LegendItem(ref lx, ly, Slate, RWIcons.Chart("Tick"), words[4], legendSize);
        string foot = vote ? "A tie goes to A" + RWCard.Separator + "no votes take A" : "M or Tab closes the chart";
        TMPro.TMP_Text f = Text(area, "Foot", foot, new Vector2(r.xMin + 4f, r.yMin + cap * 0.6f), cap, TMPro.TextAlignmentOptions.Left, Ink, 0.6f);
        if (vote) { clock = f; clockLeft = -1; }
    }

    public const float Letter = 0.62f;   // a letter's width, of its size, in the game's font (Text's box; the legend fits by it)

    static float LegendWide(string words, float cap) { return cap * 1.7f + 3f + words.Length * cap * Letter + cap * 1.4f; }

    static void LegendItem(ref float x, float y, Color fill, Sprite icon, string words, float cap)
    {
        float s = cap * 1.7f;
        RectTransform t = Framed(area, "Legend", new Vector2(x + s * 0.5f, y), s, fill, 1f);
        if (icon != null) Icon(t, icon, s, 1f);
        Text(area, "Legend", words, new Vector2(x + s + 3f, y), cap, TMPro.TextAlignmentOptions.Left, Ink, 0.8f);
        x += LegendWide(words, cap);
    }

    // The stock dialog's title: capitals centred, a bright rule under them that turns down the right side with a small knot
    // at the corner, the pack's crest standing on the rule's left end.
    static void RuledTitle(Rect r, string words, float size)
    {
        float ry = r.yMax - size * 1.45f;
        Text(area, "Title", words, new Vector2(r.center.x, ry + size * 0.75f), size, TMPro.TextAlignmentOptions.Center, Ink, 0.95f);
        Line(area, "Rule", new Vector2(r.xMin + 2f, ry), new Vector2(r.xMax, ry), 1.5f, Ink, 0.95f);
        Line(area, "Rule", new Vector2(r.xMax, ry), new Vector2(r.xMax, r.yMin), 1.5f, Ink, 0.95f);
        Box(area, "Knot", new Rect(r.xMax - 2f, ry - 2f, 5f, 5f), Ink);
        Image(area, "Crest", RWAssets.Sprite("RogueWaves_Crest"), new Rect(r.xMin + 2f, ry + 1f, 12f, 12f), 0.95f);
    }

    // On the boat: the mini chart where the game's pieces leave room (Mini); the banner top centre while it sails or after a rest.
    static void BoatLook(float W, float H)
    {
        if (bannerTitle != "")
        {
            float bw = Mathf.Min(W * 0.5f, 360f), bh = 26f;
            Rect b = new Rect((W - bw) * 0.5f, H - Margin - bh, bw, bh);
            Box(area, "BannerFrame", new Rect(b.xMin - 3f, b.yMin - 3f, b.width + 6f, b.height + 6f), new Color(Frame.r, Frame.g, Frame.b, 0.85f));
            Box(area, "BannerGap", new Rect(b.xMin - 2f, b.yMin - 2f, b.width + 4f, b.height + 4f), new Color(0f, 0f, 0f, 0.35f));
            Box(area, "Banner", b, Teal);
            Text(area, "BannerTitle", bannerTitle, b.center, 13f, TMPro.TextAlignmentOptions.Center, Ink, 1f);
            Text(area, "BannerLine", bannerLine, new Vector2(b.center.x, b.yMin - 10f), 7f, TMPro.TextAlignmentOptions.Center, Ink, 0.9f);
        }
        Macro_Worldvisual vis = Macro_Worldvisual.Instance;
        if (vis != null && vis.Insetup)
            return;
        if (MiniAt.size != MiniSize(W, H))
            Mini(boatWorld);
        Rect m = MiniAt;
        RuledTitle(m, "SEA CHART", 9f);
        Rect inner = new Rect(m.xMin + 6f, m.yMin + 14f, m.width - 14f, m.height - 34f);
        Layout lay = Lay(chart, sailed, here, new List<RWChart.Node>(), null, heading, inner);
        foreach (Lane l in lay.Lanes)
        {
            Vector2 a = lay.Find(l.From).At, b = lay.Find(l.To).At;
            if (l.Kind == Sailed) Line(area, "Lane", a, b, 1f, Ink, 0.9f);
            else if (l.Kind == Faint) Line(area, "Lane", a, b, 0.7f, Ink, 0.12f);
            else Dashed(area, a, b, 0.8f, l.Kind == Lit ? 0.9f : 0.4f);
        }
        float T = Mathf.Clamp(inner.height / RWChart.Rows * 0.5f, 5f, 10f);
        foreach (Tile t in lay.Tiles)
        {
            Color fill = t.Node.Row == 0 ? Teal : t.Sailed ? Slate : t.Node.Breaker ? Coral : Teal;
            Framed(area, t.Node.Name, t.At, T, fill, t.Reach || t.Sailed ? 1f : 0.3f);
            if (heading != null && t.Node == heading) Brackets(area, t.At, T * 0.5f + 3f, 1f, 3f);
        }
        Tile me = lay.Find(here);
        if (me != null) Image(area, "Boat", RWIcons.Chart("Boat"), new Rect(me.At.x + T * 0.5f + 1f, me.At.y - 3f, 10f, 10f), 1f);
        Text(area, "Key", "M, Tab" + RWCard.Separator + "whole chart", new Vector2(m.center.x, m.yMin + 4f), 7f, TMPro.TextAlignmentOptions.Center, Ink, 0.85f);
    }

    // ---------------------------------------------------------------- the mini chart's place
    //
    // The boat screen's top right holds the game's own title, the lobby line, the blue player card and the HUD's tool buttons,
    // and what stands there changes with the screen's size, the players and the menus open: the mini chart drawn there sat
    // under them (full test 2026-11-06 §5 item 2). So it takes the first spot that every picture and word of the game's open
    // menus leaves clear, measured on the screen: down the left edge from the top, then down the right edge; with none clear,
    // the spot they cover least. It keeps its spot while that stays clear. What the old top right spot had over it is logged
    // once a world, for the PC round's eye.
    public static Rect MiniAt { get; private set; }     // where the mini chart stands, in this view's canvas units
    public static int MiniUnder { get; private set; }   // how many of the game's pieces stand on that spot (the self-test's CV14)
    public static int MiniPieces { get; private set; }  // how many were measured
    const float MiniStep = 4f, MiniGap = 3f, Backdrop = 0.4f;
    static readonly List<Rect> pieces = new List<Rect>();
    static readonly List<string> pieceNames = new List<string>();
    static readonly List<UnityEngine.UI.Graphic> graphics = new List<UnityEngine.UI.Graphic>();
    static readonly List<Canvas> canvases = new List<Canvas>();
    static readonly Vector3[] corners = new Vector3[4];
    static float miniLook;
    static int blocked;
    static string miniWorld = "";

    static Vector2 MiniSize(float W, float H) { return new Vector2(Mathf.Min(W * 0.2f, 150f), Mathf.Min(H * 0.55f, 240f)); }

    // Where it stood before the full test: top right, a line under the screen's top.
    static Rect OldSpot(Vector2 size, float W, float H) { return new Rect(W - Margin - size.x, H - Margin - size.y - 24f, size.x, size.y); }

    // Measures and picks the spot; true when it moved.
    static bool Mini(string world)
    {
        float ui = UiScale(), W = Screen.width / ui, H = Screen.height / ui;
        Vector2 size = MiniSize(W, H);
        bool named = world != miniWorld;
        Measure(ui, W, H, named);
        if (named)
        {
            miniWorld = world;
            List<string> over = new List<string>();
            Rect corner = Padded(OldSpot(size, W, H));
            for (int i = 0; i < pieces.Count && over.Count < 8; i++)
                if (pieces[i].Overlaps(corner) && !over.Contains(pieceNames[i])) over.Add(pieceNames[i]);
            RWLog.Info("chart view: on the boat the old top right spot holds " + (over.Count > 0 ? string.Join(", ", over.ToArray()) : "none of the game's pieces") + " (" + pieces.Count + " measured)");
        }
        // A piece over the spot for one look only (a hover's tooltip, a slide) doesn't move it.
        Rect was = MiniAt;
        int under = was.size == size ? Under(was) : -1;
        MiniUnder = under;
        if (under > 0 && ++blocked < 2)
            return false;
        blocked = 0;
        if (under == 0)
            return false;
        Rect at = Spot(size, W, H);
        // With no spot clear it keeps its own unless the new one is clearly less covered, so it doesn't hop between two.
        if (under > 0 && Covered(at) > 0f && Covered(at) > Covered(was) * 0.9f)
            at = was;
        MiniAt = at;
        MiniUnder = Under(at);
        if (at == was)
            return false;
        RWLog.Info("chart view: mini chart at " + (at.center.x < W * 0.5f ? "the left" : "the right") + " (" + RWUtil.F(at.x) + ", " + RWUtil.F(at.y) + "), " + RWUtil.F(at.width) + " by "
            + RWUtil.F(at.height) + " of " + RWUtil.F(W) + " by " + RWUtil.F(H) + ", " + (MiniUnder == 0 ? "clear of the game's " + pieces.Count + " pieces" : "under " + MiniUnder + " of " + pieces.Count + " pieces, none clear"));
        return true;
    }

    // Down the left edge, then the right, from the top: the first clear spot, or the least covered.
    static Rect Spot(Vector2 size, float W, float H)
    {
        Rect best = OldSpot(size, W, H);
        float least = float.MaxValue;
        float[] xs = { Margin, W - Margin - size.x };
        foreach (float x in xs)
            for (float y = H - Margin - size.y; y >= Margin - 0.01f; y -= MiniStep)
            {
                Rect r = new Rect(x, y, size.x, size.y);
                float covered = Covered(r);
                if (covered <= 0f) return r;
                if (covered < least)
                {
                    least = covered;
                    best = r;
                }
            }
        return best;
    }

    static Rect Padded(Rect r) { return new Rect(r.x - MiniGap, r.y - MiniGap, r.width + MiniGap * 2f, r.height + MiniGap * 2f); }

    static float Covered(Rect r)
    {
        Rect p = Padded(r);
        float area = 0f;
        foreach (Rect q in pieces)
        {
            float w = Mathf.Min(p.xMax, q.xMax) - Mathf.Max(p.xMin, q.xMin), h = Mathf.Min(p.yMax, q.yMax) - Mathf.Max(p.yMin, q.yMin);
            if (w > 0f && h > 0f) area += w * h;
        }
        return area;
    }

    static int Under(Rect r)
    {
        if (r.width <= 0f) return int.MaxValue;
        Rect p = Padded(r);
        int n = 0;
        foreach (Rect q in pieces)
            if (q.Overlaps(p)) n++;
        return n;
    }

    // Every picture and word the game's open menus show, in this view's canvas units, from the outermost canvas of each open
    // menu (so the HUD's buttons beside a menu count too). Hidden and see-through pieces don't count, nor a backdrop over 40%
    // of the screen. A text's words are where its last drawn mesh put them: asking TMP to draw again freezes frames (#138).
    // The canvases are the outermost ones, so no piece is met twice; names only for the once-a-world log.
    static void Measure(float ui, float W, float H, bool named)
    {
        pieces.Clear();
        pieceNames.Clear();
        canvases.Clear();
        if (Blockage.Menu.Bl_Menus.Ui != null) Top(Blockage.Menu.Bl_Menus.Ui.transform);
        if (Blockage.Menu.Bl_Menus.Menus != null)
            foreach (Blockage.Menu.Blu2_Menu_Handler menu in Blockage.Menu.Bl_Menus.Menus.Values)
                if (menu != null && menu.Active && menu.gameObject.activeInHierarchy) Top(menu.transform);
        Rect screen = new Rect(0f, 0f, W, H);
        foreach (Canvas c in canvases)
        {
            c.GetComponentsInChildren<UnityEngine.UI.Graphic>(false, graphics);
            foreach (UnityEngine.UI.Graphic g in graphics)
            {
                if (g == null || !g.isActiveAndEnabled || g.canvas == null || !g.canvas.isActiveAndEnabled || g.canvas.renderMode == RenderMode.WorldSpace
                    || g.canvasRenderer.cull || g.color.a * g.canvasRenderer.GetInheritedAlpha() < 0.05f)
                    continue;
                UnityEngine.UI.Mask mask = g.GetComponent<UnityEngine.UI.Mask>();
                if (mask != null && !mask.showMaskGraphic)
                    continue;
                Rect r;
                TMPro.TMP_Text text = g as TMPro.TMP_Text;
                if (text != null)
                {
                    if (string.IsNullOrEmpty(text.text) || text.text.Trim() == "" || !RWDialog.TextOnScreen(text, out r)) continue;
                }
                else if (g is UnityEngine.UI.Image || g is UnityEngine.UI.RawImage)
                {
                    ((RectTransform)g.transform).GetWorldCorners(corners);
                    r = RWDialog.OnScreen(corners, g.canvas);
                }
                else continue;
                r = Rect.MinMaxRect(Mathf.Max(0f, r.xMin / ui), Mathf.Max(0f, r.yMin / ui), Mathf.Min(W, r.xMax / ui), Mathf.Min(H, r.yMax / ui));
                if (r.width < 1f || r.height < 1f || !r.Overlaps(screen) || r.width * r.height > W * H * Backdrop)
                    continue;
                pieces.Add(r);
                if (named) pieceNames.Add((g.transform.parent != null ? g.transform.parent.name + "/" : "") + g.name);
            }
        }
        graphics.Clear();
        MiniPieces = pieces.Count;
    }

    // The outermost canvas over t, found by hand (GetComponentInParent skips inactive parents).
    static void Top(Transform t)
    {
        Canvas top = null;
        for (; t != null; t = t.parent)
        {
            Canvas c = t.GetComponent<Canvas>();
            if (c != null) top = c;
        }
        if (top != null && top.renderMode != RenderMode.WorldSpace && !canvases.Contains(top)) canvases.Add(top);
    }

    // ---------------------------------------------------------------- pieces (canvas units, origin at the bottom left)

    public static RectTransform Rt(Transform parent, string name)
    {
        GameObject g = new GameObject(name, typeof(RectTransform));
        g.transform.SetParent(parent, false);
        RectTransform rt = (RectTransform)g.transform;
        rt.anchorMin = rt.anchorMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);
        return rt;
    }

    public static RectTransform Box(Transform parent, string name, Rect r, Color c)
    {
        RectTransform rt = Rt(parent, name);
        rt.pivot = Vector2.zero;
        rt.anchoredPosition = r.position;
        rt.sizeDelta = r.size;
        if (c.a > 0f)
        {
            UnityEngine.UI.Image img = rt.gameObject.AddComponent<UnityEngine.UI.Image>();
            img.color = c;
            img.raycastTarget = false;
        }
        return rt;
    }

    public static void Image(Transform parent, string name, Sprite sprite, Rect r, float alpha)
    {
        if (sprite == null)
            return;
        RectTransform rt = Rt(parent, name);
        rt.pivot = Vector2.zero;
        rt.anchoredPosition = r.position;
        rt.sizeDelta = r.size;
        UnityEngine.UI.Image img = rt.gameObject.AddComponent<UnityEngine.UI.Image>();
        img.sprite = sprite;
        img.color = new Color(1f, 1f, 1f, alpha);
        img.raycastTarget = false;
    }

    static void Icon(RectTransform tile, Sprite sprite, float size, float alpha)
    {
        float s = Mathf.Min(12f, size * 0.75f);
        Image(tile, "Icon", sprite, new Rect((size - s) * 0.5f, (size - s) * 0.5f, s, s), alpha);
    }

    public static TMPro.TMP_Text Text(Transform parent, string name, string words, Vector2 at, float size, TMPro.TextAlignmentOptions align, Color c, float alpha)
    {
        RectTransform rt = Rt(parent, name);
        float wide = Mathf.Max(20f, words.Length * size * Letter + 8f);
        rt.pivot = align == TMPro.TextAlignmentOptions.Left ? new Vector2(0f, 0.5f) : align == TMPro.TextAlignmentOptions.Right ? new Vector2(1f, 0.5f) : new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = at;
        rt.sizeDelta = new Vector2(wide, size * 1.4f);
        TMPro.TextMeshProUGUI t = rt.gameObject.AddComponent<TMPro.TextMeshProUGUI>();
        if (font != null) { t.font = font; t.fontSharedMaterial = fontMaterial; }
        t.fontSize = size;
        t.alignment = align;
        t.enableWordWrapping = false;
        t.overflowMode = TMPro.TextOverflowModes.Overflow;
        t.raycastTarget = false;
        t.color = new Color(c.r, c.g, c.b, alpha);
        t.text = words;
        return t;
    }

    public static void Line(Transform parent, string name, Vector2 a, Vector2 b, float width, Color c, float alpha)
    {
        RectTransform rt = Rt(parent, name);
        Vector2 d = b - a;
        rt.pivot = new Vector2(0f, 0.5f);
        rt.anchoredPosition = a;
        rt.sizeDelta = new Vector2(d.magnitude, width);
        rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
        UnityEngine.UI.Image img = rt.gameObject.AddComponent<UnityEngine.UI.Image>();
        img.color = new Color(c.r, c.g, c.b, alpha);
        img.raycastTarget = false;
    }

    // A dashed lane: one strip whose texture repeats a dash and a gap, cut back clear of the tiles at its ends.
    static Texture2D dash;

    static void Dashed(Transform parent, Vector2 a, Vector2 b, float width, float alpha)
    {
        if (dash == null)
        {
            dash = new Texture2D(8, 1, TextureFormat.RGBA32, false);
            dash.filterMode = FilterMode.Point;
            dash.wrapMode = TextureWrapMode.Repeat;
            Color32[] px = new Color32[8];
            for (int i = 0; i < 8; i++) px[i] = i < 4 ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
            dash.SetPixels32(px);
            dash.Apply(false);
        }
        RectTransform rt = Rt(parent, "Lane");
        Vector2 d = b - a;
        rt.pivot = new Vector2(0f, 0.5f);
        rt.anchoredPosition = a;
        rt.sizeDelta = new Vector2(d.magnitude, width);
        rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
        UnityEngine.UI.RawImage img = rt.gameObject.AddComponent<UnityEngine.UI.RawImage>();
        img.texture = dash;
        img.uvRect = new Rect(0f, 0f, d.magnitude / 8f, 1f);
        img.color = new Color(Ink.r, Ink.g, Ink.b, alpha);
        img.raycastTarget = false;
    }

    // White corner brackets, the game's selection mark.
    static void Brackets(Transform parent, Vector2 c, float half, float alpha, float arm)
    {
        for (int i = 0; i < 4; i++)
        {
            float sx = (i & 1) == 0 ? -1f : 1f, sy = (i & 2) == 0 ? -1f : 1f;
            Vector2 corner = c + new Vector2(sx * half, sy * half);
            Line(parent, "Bracket", corner, corner - new Vector2(sx * arm, 0f), 1.2f, Ink, alpha);
            Line(parent, "Bracket", corner, corner - new Vector2(0f, sy * arm), 1.2f, Ink, alpha);
        }
    }

    // A tile built like the stock cards: a light grey outer frame, a gap, the tile and a darker inner rim (20 x 20 px).
    static readonly Dictionary<string, Sprite> frames = new Dictionary<string, Sprite>();

    public static RectTransform Framed(Transform parent, string name, Vector2 at, float size, Color fill, float alpha)
    {
        string key = ColorUtility.ToHtmlStringRGB(fill);
        Sprite s;
        if (!frames.TryGetValue(key, out s) || s == null)
        {
            const int N = 20;
            Texture2D tex = new Texture2D(N, N, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            Color32[] px = new Color32[N * N];
            Color32 f = fill, rim = fill * 0.8f, fr = new Color(Frame.r, Frame.g, Frame.b, 0.85f);
            rim.a = 255;
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    int d = Mathf.Min(Mathf.Min(x, y), Mathf.Min(N - 1 - x, N - 1 - y));
                    px[y * N + x] = d == 0 ? fr : d == 1 ? new Color32(0, 0, 0, 0) : d == 4 ? rim : f;
                }
            tex.SetPixels32(px);
            tex.Apply(false);
            s = UnityEngine.Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), 20f, 0, SpriteMeshType.FullRect);
            s.name = "RW_ChartTile_" + key;
            frames[key] = s;
        }
        RectTransform rt = Rt(parent, name);
        rt.anchoredPosition = at;
        rt.sizeDelta = new Vector2(size, size);
        UnityEngine.UI.Image img = rt.gameObject.AddComponent<UnityEngine.UI.Image>();
        img.sprite = s;
        img.color = new Color(1f, 1f, 1f, alpha);
        img.raycastTarget = false;
        return rt;
    }
}
