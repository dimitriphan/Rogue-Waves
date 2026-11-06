// The MVP board on every machine (docs/mode/MVP_BOARD.md §5): what the host wrote on this player's vessel (RWBoard's five
// lines), drawn in the game's own UI look with the chart view's pieces (26_ChartView.cs): no panel, the stock title with
// its rule and knot, one stock-built card per player with their one title, the MVP in white corner brackets under the
// view's one Storm Coral banner, this player's card underlined in Wet Sand.
//   live    a new board number on the vessel opens it; Continue (click, Enter, Space or the pad's south button) tells the
//           host this player is done; it closes when the host blanks the lines, or by itself a little after its time
//           (a board over the end screen has no host step to close it)
//   look    ~Tab~ opens the last board again in a map, read only; Tab, Enter or a dialog closes it
// Its own Canvas above the chart view. Pictures sit on whole canvas units, one unit an art pixel (pitfall #156).
using System;
using System.Collections.Generic;
using UnityEngine;

public static class RWBoardView
{
    const int Sorting = 125;
    static readonly Color Teal = RWDialog.Teal, Coral = RWDialog.Coral, Swell = RWDialog.Swell, Sand = RWDialog.Sand,
        Swell2 = new Color32(0x35, 0x54, 0x6D, 255), Slate = new Color32(0x5B, 0x5D, 0x69, 255),
        Ink = new Color32(0xF2, 0xF4, 0xF3, 255), Mist = new Color32(0xB4, 0xC8, 0xCF, 255), Frame = new Color32(0xC8, 0xCC, 0xD0, 255);

    static GameObject root;
    static string mode = "";                     // "", "Live", "Look"
    static float seen = -1f, closesAt, nextLook;
    static bool pressed, delivered;
    static string head = "";
    static List<RWBoard.Row> rows = new List<RWBoard.Row>();
    static string cachedHead = "";               // the last board shown, for the key
    static List<RWBoard.Row> cachedRows = new List<RWBoard.Row>();
    static RectTransform button;
    static TMPro.TMP_Text clock;
    static int clockLeft = -1;

    public static bool Up { get { return root != null; } }
    public static int CardsShown;                              // self-test
    public static string MvpShown = "", MineShown = "";

    // ---------------------------------------------------------------- every frame (every machine)

    public static void Update()
    {
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "Game")
        {
            if (root != null || mode != "") Close();
            // A board the host writes with no map open (a map's end after it is gone, the last retry lost on the boat) has
            // nowhere to show: the host marks it seen, so the next map doesn't open it while it loads (the next map's start
            // blanks every vessel's lines for the others).
            if (RWUtil.IsHost) seen = RWBoard.Round;
            return;
        }
        if (Time.unscaledTime >= nextLook)
        {
            nextLook = Time.unscaledTime + 0.2f;
            Poll();
        }
        if (mode == "")
        {
            if (TabPressed() && cachedHead != "" && !RWDialog.Up() && !RWChartView.Up)
                Open("Look", cachedHead, cachedRows);
            return;
        }
        if (mode == "Look" && (TabPressed() || RWDialog.Up()))
        {
            Close();
            return;
        }
        Pointer();
        if (mode == "Live" && clock != null)
        {
            int left = Mathf.Max(0, Mathf.CeilToInt(closesAt - Time.unscaledTime));
            if (left != clockLeft)
            {
                clockLeft = left;
                clock.text = Clock(left);
            }
        }
    }

    // What the host wrote: on the host its own copy, elsewhere this player's vessel (the last entry of each name, pitfall #72).
    static void Poll()
    {
        if (mode == "Live" && Time.unscaledTime > closesAt + 3f)
        {
            Close();    // also with no vessel to read (a board over the end screen)
            return;
        }
        string h;
        float round;
        List<string> lines = new List<string>();
        if (RWUtil.IsHost)
        {
            h = RWBoard.Text[RWBoard.Cards];
            round = RWBoard.Round;
            for (int i = 0; i < RWBoard.Cards; i++) lines.Add(RWBoard.Text[i]);
        }
        else
        {
            Entity_Master v = RWMine.Vessel();
            Link_Commandref c;
            if (v == null || !RWMine.Command(v, RWBoard.Link(RWBoard.Cards), out c))
                return;    // no vessel standing: nothing known, nothing closed
            h = c.Goalstring.ToString();
            round = c.Goalvalue;
            for (int i = 0; i < RWBoard.Cards; i++)
                lines.Add(RWMine.Command(v, RWBoard.Link(i), out c) ? c.Goalstring.ToString() : "");
        }
        if (mode == "Live" && h == "")
        {
            Close();
            return;
        }
        if (h == "" || round == seen)
        {
            if (mode == "Live" && pressed && !delivered)
                delivered = RWMine.Send(RWBoard.Kind, Mathf.RoundToInt(seen), "");    // a Continue the vessel couldn't send, for this board only
            return;
        }
        List<RWBoard.Row> got = new List<RWBoard.Row>();
        foreach (string line in lines)
        {
            string kind;
            RWBoard.Row r = line != "" ? RWBoard.DecodeRow(line, out kind) : null;
            if (r != null) got.Add(r);
        }
        if (got.Count < 2)
            return;
        seen = round;
        pressed = delivered = false;
        cachedHead = h;
        cachedRows = got;
        closesAt = Time.unscaledTime + RWBoard.Seconds;
        Open("Live", h, got);
        RWLog.Info("MVP board " + RWUtil.F(round) + " shown here: " + got.Count + " cards");
    }

    static bool TabPressed()
    {
        UnityEngine.InputSystem.Keyboard k = UnityEngine.InputSystem.Keyboard.current;
        return k != null && k[UnityEngine.InputSystem.Key.Tab].wasPressedThisFrame && !RWChartView.Typing();
    }

    static bool ContinuePressed()
    {
        UnityEngine.InputSystem.Keyboard k = UnityEngine.InputSystem.Keyboard.current;
        UnityEngine.InputSystem.Gamepad pad = UnityEngine.InputSystem.Gamepad.current;
        bool key = k != null && !RWChartView.Typing() && (k[UnityEngine.InputSystem.Key.Enter].wasPressedThisFrame || k[UnityEngine.InputSystem.Key.NumpadEnter].wasPressedThisFrame
            || k[UnityEngine.InputSystem.Key.Space].wasPressedThisFrame);
        return key || (pad != null && pad.buttonSouth.wasPressedThisFrame);
    }

    static void Pointer()
    {
        bool click = false;
        UnityEngine.InputSystem.Mouse m = UnityEngine.InputSystem.Mouse.current;
        if (m != null && m.leftButton.wasPressedThisFrame && button != null)
            click = RectTransformUtility.RectangleContainsScreenPoint(button, m.position.ReadValue(), null);
        if (click || ContinuePressed())
            Continue();
    }

    // This player is done reading (the self-test calls it too).
    public static void Continue()
    {
        if (mode == "Look")
        {
            Close();
            return;
        }
        if (mode != "Live" || pressed)
            return;
        pressed = true;
        delivered = RWMine.Send(RWBoard.Kind, Mathf.RoundToInt(seen), "");
        if (clock != null) clock.text = Clock(Mathf.Max(0, Mathf.CeilToInt(closesAt - Time.unscaledTime)));
    }

    static string Clock(int left)
    {
        return (pressed ? "Waiting for the crew" + RWCard.Separator : "") + left + " s";
    }

    public static void Close()
    {
        if (root != null) UnityEngine.Object.Destroy(root);
        root = null;
        button = null;
        clock = null;
        mode = "";
    }

    // ---------------------------------------------------------------- drawing (canvas units, origin at the bottom left)

    static void Open(string how, string header, List<RWBoard.Row> cards)
    {
        try
        {
            if (!RWChartView.Canvasable())
                return;
            Close();
            mode = how;
            head = header;
            rows = cards;
            Draw();
        }
        catch (Exception e)
        {
            RWLog.Once("board view", "MVP board: " + e);
            Close();
        }
    }

    static void Draw()
    {
        root = new GameObject("RW_BoardView", typeof(RectTransform));
        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = Sorting;
        UnityEngine.UI.CanvasScaler scaler = root.AddComponent<UnityEngine.UI.CanvasScaler>();
        float ui = RWChartView.UiScale();
        scaler.scaleFactor = ui;
        root.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        float W = Mathf.Floor(Screen.width / ui), H = Mathf.Floor(Screen.height / ui);
        RectTransform area = RWChartView.Box(root.transform, "Area", new Rect(0f, 0f, W, H), new Color(0f, 0f, 0f, 0f));
        // The stock dialog's dim; it takes the clicks, so none reach the game under the board.
        RWChartView.Box(area, "Shade", new Rect(0f, 0f, W, H), new Color(0.11f, 0.14f, 0.17f, 0.6f)).GetComponent<UnityEngine.UI.Image>().raycastTarget = true;

        string[] h = head.Split('|');
        bool voyage = h.Length > 0 && h[0] == RWBoard.VoyageBoard;
        int map = 0, tide = 0, retries = 0, leaks = 0;
        if (h.Length >= 7) { int.TryParse(h[1], out map); int.TryParse(h[2], out tide); int.TryParse(h[3], out retries); int.TryParse(h[4], out leaks); }
        string next = h.Length >= 7 ? h[5] : "", mapName = h.Length >= 7 ? h[6] : "";

        int n = rows.Count;
        float cw = 128f, ch = 150f, gap = 14f;
        float total = n * cw + (n - 1) * gap;
        float x0 = Mathf.Round((W - total) * 0.5f), y0 = Mathf.Round(H * 0.5f - ch * 0.5f + 8f);
        // The title over the cards: capitals, the bright rule turning down the right side, the knot, the crest.
        Rect r = new Rect(x0 - 8f, y0 + ch + 18f, total + 16f, 30f);
        float ry = r.yMin + 8f;
        Label(area, voyage ? "THE VOYAGE" : "SHIP'S LOG", new Vector2(r.center.x, ry + 10f), 14f, TMPro.TextAlignmentOptions.Center, Ink, 0.95f);
        RWChartView.Line(area, "Rule", new Vector2(r.xMin, ry), new Vector2(r.xMax, ry), 1.5f, Ink, 0.95f);
        RWChartView.Line(area, "Rule", new Vector2(r.xMax, ry), new Vector2(r.xMax, y0 + ch * 0.6f), 1.5f, Ink, 0.95f);
        RWChartView.Box(area, "Knot", new Rect(r.xMax - 2f, ry - 2f, 5f, 5f), Ink);
        RWChartView.Image(area, "Crest", RWAssets.Sprite("RogueWaves_Crest"), new Rect(r.xMin + 2f, ry + 1f, 12f, 12f), 0.95f);
        string sub = voyage ? (next == RWBoard.NextEnd && map >= RWChart.Rows ? "THE CAMPAIGN IS WON" : "THE CAMPAIGN IS OVER") + RWCard.Separator + map + " OF " + RWChart.Rows + " MAPS SAILED"
            : "MAP " + map + " WON" + (mapName != "" ? RWCard.Separator + mapName.ToUpperInvariant() : "");
        Label(area, sub, new Vector2(r.center.x, ry - 7f), 5.5f, TMPro.TextAlignmentOptions.Center, Mist, 1f);

        long me = RWMine.Session;
        CardsShown = 0;
        MvpShown = MineShown = "";
        for (int i = 0; i < n; i++)
            Card(area, rows[i], x0 + i * (cw + gap), y0, cw, ch, rows[i].Session == me && me >= 0, voyage);

        // One caption line under the cards, its parts between the game's separator.
        List<string> parts = new List<string>();
        if (!voyage && tide > 0) parts.Add("Tide " + tide);
        parts.Add(RWCard.Retries(Mathf.Max(0, retries)));
        parts.Add(leaks + (leaks == 1 ? " foe slipped through" : " foes slipped through"));
        string then = next == RWBoard.NextPicks ? "Direction picks" : next == RWBoard.NextVoyage ? "The Voyage" : next == RWBoard.NextBoat ? "Back to the boat" : "";
        if (then != "") parts.Add("Next: " + then);
        Label(area, string.Join(RWCard.Separator, parts.ToArray()), new Vector2(W * 0.5f, y0 - 12f), 6f, TMPro.TextAlignmentOptions.Center, Ink, 0.9f);

        // Continue: a teal stock-built tile.
        float bw = 70f, bh = 16f;
        Rect b = new Rect(Mathf.Round(W * 0.5f - bw * 0.5f), Mathf.Round(y0 - 40f), bw, bh);
        RWChartView.Box(area, "ButtonFrame", new Rect(b.xMin - 2f, b.yMin - 2f, b.width + 4f, b.height + 4f), new Color(Frame.r, Frame.g, Frame.b, 0.85f));
        RWChartView.Box(area, "ButtonGap", new Rect(b.xMin - 1f, b.yMin - 1f, b.width + 2f, b.height + 2f), new Color(0f, 0f, 0f, 0.35f));
        button = RWChartView.Box(area, "Button", b, Teal);
        RWChartView.Box(area, "ButtonRim", new Rect(b.xMin + 1f, b.yMin + 1f, b.width - 2f, 1f), Teal * 0.8f);
        Label(area, mode == "Look" ? "CLOSE" : "CONTINUE", b.center, 7f, TMPro.TextAlignmentOptions.Center, Ink, 1f);
        clock = Label(area, mode == "Look" ? "Tab closes" : Clock(Mathf.Max(0, Mathf.CeilToInt(closesAt - Time.unscaledTime))),
            new Vector2(b.xMax + 8f, b.center.y), 5.5f, TMPro.TextAlignmentOptions.Left, Mist, 1f);
        if (mode == "Look") clock = null;
        clockLeft = -1;
    }

    static void Card(RectTransform area, RWBoard.Row row, float x, float y, float w, float h, bool mine, bool voyage)
    {
        float a = row.Away ? 0.55f : 1f;
        // The stock card: a light frame, a gap, the Deep Swell tile, a darker rim.
        RWChartView.Box(area, "CardFrame", new Rect(x - 2f, y - 2f, w + 4f, h + 4f), new Color(Frame.r, Frame.g, Frame.b, 0.85f * a));
        RWChartView.Box(area, "CardGap", new Rect(x - 1f, y - 1f, w + 2f, h + 2f), new Color(0.06f, 0.08f, 0.1f, 0.9f));
        RectTransform card = RWChartView.Box(area, "Card", new Rect(x, y, w, h), new Color(Swell.r, Swell.g, Swell.b, a));
        RWChartView.Box(card, "Rim", new Rect(1f, 1f, w - 2f, h - 2f), new Color(Swell.r * 0.8f, Swell.g * 0.8f, Swell.b * 0.8f, a));
        RWChartView.Box(card, "Fill", new Rect(2f, 2f, w - 4f, h - 4f), new Color(Swell.r, Swell.g, Swell.b, a));
        CardsShown++;
        if (row.Mvp)
        {
            MvpShown = row.Name;
            Corners(area, new Rect(x - 6f, y - 6f, w + 12f, h + 12f));
            Rect bn = new Rect(Mathf.Round(x + w * 0.5f - 18f), y + h + 3f, 36f, 11f);
            RWChartView.Box(area, "MvpFrame", new Rect(bn.xMin - 1f, bn.yMin - 1f, bn.width + 2f, bn.height + 2f), Ink);
            RWChartView.Box(area, "Mvp", bn, Coral);
            Label(area, "MVP", bn.center, 7f, TMPro.TextAlignmentOptions.Center, Color.white, 1f);
        }
        if (mine) MineShown = row.Name;
        // The vessel's own 12 px icon at two units a pixel.
        Heroentryobject hero = row.Hero != "" ? RWUtil.Item<Heroentryobject>("Hero", row.Hero) : null;
        if (hero != null && hero.Icon != null)
            RWChartView.Image(card, "Vessel", hero.Icon, new Rect(Mathf.Round(w * 0.5f - 12f), h - 34f, 24f, 24f), a);
        Label(card, row.Name, new Vector2(w * 0.5f, h - 44f), 8f, TMPro.TextAlignmentOptions.Center, Ink, a);
        string vessel = row.Hero != "" ? RWRun.HeroName(row.Hero).ToUpperInvariant() : "";
        Label(card, row.Away ? vessel + RWCard.Separator + "AWAY" : vessel, new Vector2(w * 0.5f, h - 54f), 5f, TMPro.TextAlignmentOptions.Center, Mist, a);
        if (mine) RWChartView.Box(card, "You", new Rect(Mathf.Round(w * 0.5f - 14f), h - 60f, 28f, 1f), Sand);
        // The one title: a raised row, its icon on a small stock-built tile, teal for an accolade, slate for a failure.
        RWBoard.Title t = RWBoard.Find(row.Title);
        if (t != null)
        {
            Color edge = t.Good ? Teal : Slate;
            Rect tr = new Rect(6f, 36f, w - 12f, h - 102f);
            RWChartView.Box(card, "TitleRow", tr, new Color(Swell2.r, Swell2.g, Swell2.b, a));
            RWChartView.Box(card, "TitleEdge", new Rect(tr.xMin, tr.yMin, 2f, tr.height), new Color(edge.r, edge.g, edge.b, a));
            RectTransform tile = RWChartView.Framed(card, "TitleTile", new Vector2(tr.xMin + 16f, Mathf.Round(tr.yMax - 15f)), 20f, edge, a);
            RWChartView.Image(tile, "Icon", RWBoard.Icon(t.Code), new Rect(4f, 4f, 12f, 12f), a);
            Label(card, t.Name.ToUpperInvariant(), new Vector2(tr.xMin + 30f, tr.yMax - 10f), 6.5f, TMPro.TextAlignmentOptions.Left, Ink, a);
            Wrapped(card, string.Format(t.Line, row.Value), new Rect(tr.xMin + 6f, tr.yMin + 3f, tr.width - 10f, tr.height - 28f), 5.5f, t.Good ? Ink : Mist, a);
        }
        // Four figures under the title.
        string[] labels = { "SUNK", "BUILT", "AURA", "DROWNED" };
        int[] values = { row.V[RWBoard.Kills], row.V[RWBoard.Built], row.V[RWBoard.Gained], row.V[RWBoard.Drowned] };
        float fw = (w - 12f) / 4f;
        for (int i = 0; i < 4; i++)
        {
            float cx = 6f + fw * i + fw * 0.5f;
            Label(card, values[i].ToString("N0", System.Globalization.CultureInfo.InvariantCulture), new Vector2(cx, 21f), 8f, TMPro.TextAlignmentOptions.Center, Ink, a);
            Label(card, labels[i], new Vector2(cx, 10f), 4f, TMPro.TextAlignmentOptions.Center, Mist, a);
        }
    }

    // White corner brackets round a card, the game's selection mark.
    static void Corners(Transform parent, Rect r)
    {
        const float arm = 8f;
        Vector2[] c = { new Vector2(r.xMin, r.yMin), new Vector2(r.xMax, r.yMin), new Vector2(r.xMin, r.yMax), new Vector2(r.xMax, r.yMax) };
        for (int i = 0; i < 4; i++)
        {
            float sx = (i & 1) == 0 ? 1f : -1f, sy = (i & 2) == 0 ? 1f : -1f;
            RWChartView.Line(parent, "Bracket", c[i], c[i] + new Vector2(sx * arm, 0f), 1.5f, Ink, 1f);
            RWChartView.Line(parent, "Bracket", c[i], c[i] + new Vector2(0f, sy * arm), 1.5f, Ink, 1f);
        }
    }

    static TMPro.TMP_Text Label(Transform parent, string words, Vector2 at, float size, TMPro.TextAlignmentOptions align, Color c, float alpha)
    {
        return RWChartView.Text(parent, "Label", words, at, size, align, c, alpha);
    }

    // Body text that wraps inside a box, top left.
    static void Wrapped(Transform parent, string words, Rect r, float size, Color c, float alpha)
    {
        RectTransform rt = RWChartView.Rt(parent, "Line");
        rt.pivot = Vector2.zero;
        rt.anchoredPosition = r.position;
        rt.sizeDelta = r.size;
        TMPro.TextMeshProUGUI t = rt.gameObject.AddComponent<TMPro.TextMeshProUGUI>();
        if (RWChartView.GameFont != null) { t.font = RWChartView.GameFont; t.fontSharedMaterial = RWChartView.GameFontMaterial; }
        t.fontSize = size;
        t.alignment = TMPro.TextAlignmentOptions.TopLeft;
        t.enableWordWrapping = true;
        t.overflowMode = TMPro.TextOverflowModes.Overflow;
        t.raycastTarget = false;
        t.color = new Color(c.r, c.g, c.b, alpha);
        t.text = words;
    }
}
