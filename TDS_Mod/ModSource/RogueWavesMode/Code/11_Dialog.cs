// The game's choice dialog (Bla_Ui_Confirmation, kit docs/20 §2) for every choice the mode asks on one machine: the main
// menu's New / Continue, the Direction pick, the votes, VENDCINT. Its menu is opened with Force and awaited first
// (Runprompt's own Changemenu isn't forced, and it throws while its parent menu is inactive, kit docs/15 §14.6; RogueLab
// round 3). It has no timeout and no cancel, so a timed ask closes the menu itself and treats the pending task as no
// answer. The stock dialog is a corner of the screen, made for the pause menu's Exit / Back: the mode's choices stand in the
// middle of it instead, however many cards they have, and it is put back when they close (Dimitri 2026-11-03).
public static class RWDialog
{
    public const int MaxRow = 4;
    public static readonly Color Teal = new Color(0.247f, 0.561f, 0.588f, 1f), Swell = new Color(0.18f, 0.29f, 0.384f, 1f),
        Coral = new Color(0.886f, 0.447f, 0.357f, 1f), Pearl = new Color(0.957f, 0.933f, 0.973f, 1f), Sand = new Color(0.847f, 0.769f, 0.604f, 1f);
    static int asks, owner;
    public static bool Asking;
    static float shift = 0.5f;
    public static float Shift { get { return shift; } }   // where across the screen the last Ask's block stands: 0.5, or the right half beside the chart view
    public static bool Beside { get { return Asking && Shift > 0.5f; } }   // the chart view's own vote is the dialog up
    public static string LastTitle = "";     // the self-test reads what was asked
    public static int LastCards;

    static Blockage.Menu.Blu2_Menu_Handler Menu()
    {
        Bla_Ui_Confirmation dialog = Bla_Ui_Confirmation.Instance;
        Blockage.Menu.Blu2_Menu_Handler menu;
        return dialog != null && Blockage.Menu.Bl_Menus.Menus != null && Blockage.Menu.Bl_Menus.Menus.TryGetValue(dialog.gameObject.name, out menu) ? menu : null;
    }

    public static bool Up()
    {
        Blockage.Menu.Blu2_Menu_Handler menu = Menu();
        return menu != null && menu.Active;
    }

    // The game's cards show only an icon on a coloured tile: hovering one puts its Display_Desc in the dialog's line, so
    // that line names it, the way the stock unlock popup does ("AURA GAVEL ▪ RED"; seen on the PC, Riptide 2026-10-05).
    public static Bla_Ui_Confirmation.Entry Card(string name, string title, string desc, Sprite image, Color color)
    {
        Bla_Ui_Confirmation.Entry card = new Bla_Ui_Confirmation.Entry();
        card.Name = name;
        card.Display_Name = title;
        card.Display_Desc = title == "" ? desc : desc == "" ? title.ToUpperInvariant() : title.ToUpperInvariant() + RWCard.Separator + desc;
        card.Display_Image = image;
        card.Display_Color = color;
        card.Display_Imagecolor = Color.white;
        return card;
    }

    // The stock card draws its picture in a 16-unit box; the game's own card icons are 16 px.
    const float CardIcon = 16f;
    const int NumberSize = 24;

    // A number as a card's picture (the cards show no text of their own): 24 px, so every amount up to three digits has
    // the same size of digit, and it fills the card's 16-unit box at two screen pixels per art pixel at a UI scale of 3
    // (RWAssets.Crisp). Four to six digits are drawn half as large, and longer numbers widen the picture.
    static readonly string[] Digits = { "111101101101111", "010110010010111", "111001111100111", "111001111001111", "101101111001001", "111100111001111", "111100111101111", "111001010010010", "111101111101111", "111101111001111" };
    static readonly Dictionary<int, Sprite> numbers = new Dictionary<int, Sprite>();

    public static Sprite Number(int n)
    {
        Sprite made;
        if (numbers.TryGetValue(n, out made) && made != null)
            return made;
        string text = n.ToString();
        int scale = text.Length <= 3 ? 2 : 1, wide = text.Length * 3 * scale + (text.Length - 1) * scale;
        int Size = Mathf.Max(NumberSize, wide);
        Texture2D tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        Color32[] all = new Color32[Size * Size];
        int x0 = Mathf.Max(0, (Size - wide) / 2), y0 = (Size - 5 * scale) / 2;
        for (int d = 0; d < text.Length; d++)
        {
            string glyph = Digits[text[d] - (char)48];
            for (int row = 0; row < 5; row++)
                for (int col = 0; col < 3; col++)
                    if (glyph[row * 3 + col] == (char)49)
                        for (int sy = 0; sy < scale; sy++)
                            for (int sx = 0; sx < scale; sx++)
                            {
                                int x = x0 + d * 4 * scale + col * scale + sx, y = y0 + (4 - row) * scale + sy;
                                if (x >= 0 && x < Size && y >= 0 && y < Size) all[y * Size + x] = new Color32(255, 255, 255, 255);
                            }
        }
        tex.SetPixels32(all);
        tex.Apply();
        made = UnityEngine.Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), 20f, 0, SpriteMeshType.FullRect);
        made.name = "RW_Number_" + n;
        numbers[n] = made;
        return made;
    }

    // Asks on this machine. done(name) gets the picked card's Name, or null when the time ran out, the menu closed, or a
    // newer Ask took over. seconds <= 0 waits for as long as the dialog stays up. Run as a coroutine on the mod's object.
    public static IEnumerator Ask(List<Bla_Ui_Confirmation.Entry> cards, string title, string sub, float seconds, Action<string> done)
    {
        return Ask(cards, title, sub, seconds, done, 0.5f);
    }

    // at: where across the screen the block's middle stands (0.5 centred; the chart view's vote, RWChartView.CardsAt).
    public static IEnumerator Ask(List<Bla_Ui_Confirmation.Entry> cards, string title, string sub, float seconds, Action<string> done, float at)
    {
        Bla_Ui_Confirmation dialog = Bla_Ui_Confirmation.Instance;
        int ask = ++asks;
        shift = at;
        if (dialog == null || cards == null || cards.Count == 0)
        {
            done(null);
            yield break;
        }
        Asking = true;
        LastTitle = title;
        LastCards = cards.Count;
        if (!Up())
            Blockage.Menu.Bl_Menus.Changemenu(dialog.gameObject.name, true);
        float wait = Time.unscaledTime + 5f;
        while ((!Up() || dialog.GetComponentInParent<Blockage.Menu.Blu2_Menu_Handler>() == null) && Time.unscaledTime < wait)
            yield return null;
        if (!Up() || ask != asks)
        {
            if (ask == asks) Asking = false;
            done(null);
            yield break;
        }
        Cysharp.Threading.Tasks.UniTask<Bla_Ui_Confirmation.Entry> task;
        try { task = dialog.Runprompt(cards, title, sub, false); }
        catch (Exception e)
        {
            RWLog.Warn("dialog '" + title + "': " + e.Message);
            Asking = false;
            done(null);
            yield break;
        }
        owner = ask;
        Place(dialog, cards, sub);
        yield return null;
        yield return null;
        Place(dialog, cards, sub);   // the last prompt's cards are gone by now
        float end = seconds > 0f ? Time.unscaledTime + seconds : float.MaxValue;
        // A UniTask's Status can't be read after GetResult (RogueLab round 5): read it once a step, the result once.
        Cysharp.Threading.Tasks.UniTaskStatus status = task.Status;
        while (status == Cysharp.Threading.Tasks.UniTaskStatus.Pending && ask == asks && Up() && Time.unscaledTime < end)
        {
            yield return null;
            status = task.Status;
        }
        string picked = null;
        if (status == Cysharp.Threading.Tasks.UniTaskStatus.Succeeded)
        {
            try { Bla_Ui_Confirmation.Entry e = task.GetAwaiter().GetResult(); picked = e != null ? e.Name : null; }
            catch (Exception e) { RWLog.Warn("dialog '" + title + "' result: " + e.Message); }
        }
        else if (ask == asks && Up())
            Blockage.Menu.Bl_Menus.Closemenu(dialog.gameObject, true);
        RogueWavesModeMod.Instance.StartCoroutine(UnplaceLater(ask));
        if (ask == asks) Asking = false;
        done(picked);
    }

    // The host closed the question, or a newer one is coming: the running Ask gives up and closes the dialog.
    public static void Cancel()
    {
        asks++;
        Asking = false;
        if (Up())
            Blockage.Menu.Bl_Menus.Closemenu(Bla_Ui_Confirmation.Instance.gameObject, true);
    }

    // Answers the dialog on screen as a click on its card named 'name' would (the chart view's islands, 26_ChartView.cs);
    // false when no such card is up. Cancel would close the dialog with no pick.
    public static bool Choose(string name)
    {
        Blu3_Button b = CardButton(name);
        if (b == null || b.Buttonclick == null || !Beside)
            return false;
        b.Buttonclick();
        return true;
    }

    // Shows a card's hover line as the pointer on the card would, so an island hovered on the chart view names itself in
    // the dialog's own line; null leaves the card it hovered.
    static Blu3_Button hovered;

    public static void Hover(string name)
    {
        Blu3_Button b = name != null && Beside ? CardButton(name) : null;
        if (b == hovered)
            return;
        try
        {
            if (hovered != null && hovered.Buttonleave != null) hovered.Buttonleave();
            if (b != null && b.Buttonenter != null) b.Buttonenter();
        }
        catch (Exception e) { RWLog.Once("hover", "dialog hover: " + e.Message); }
        hovered = b;
    }

    static Blu3_Button CardButton(string name)
    {
        Bla_Ui_Confirmation dialog = Bla_Ui_Confirmation.Instance;
        if (dialog == null || dialog.List == null || !Up())
            return null;
        foreach (Transform c in dialog.List.transform)
            if (c.name == name && c.gameObject.activeInHierarchy) return c.GetComponent<Blu3_Button>();
        return null;
    }

    // The card under the pad's focus, or "" (the chart view lights its island).
    public static string Focused()
    {
        GameObject sel = UnityEngine.EventSystems.EventSystem.current != null ? UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject : null;
        Bla_Ui_Confirmation dialog = Bla_Ui_Confirmation.Instance;
        return sel != null && dialog != null && dialog.List != null && sel.transform.parent == dialog.List.transform && Up() ? sel.name : "";
    }

    // The subtitle of the dialog on screen (a vote's tally).
    public static void Subtitle(string text)
    {
        Bla_Ui_Confirmation dialog = Bla_Ui_Confirmation.Instance;
        TMPro.TMP_Text t = dialog != null && dialog.Subbox != null ? dialog.Subbox.GetComponentInChildren<TMPro.TMP_Text>() : null;
        if (t != null && t.text != text)
            t.text = text;
    }

    // ---------------------------------------------------------------- placing it
    //
    // The prefab, read on the PC (kit docs/20 §2): Confirmation > Main > { Fade (the dim), Back (the dark edge), List (the
    // grid, with the line Sub inside it), Title (with Line, the corner's rule) }. Main slides in by its anchors for about
    // a second, well after Intransition has gone false, and Fade and Back must stay where it takes them. So Main is never
    // moved or measured: List and Title are anchored to its middle, and come to rest there with it. Sub runs a single line
    // to the left of the cards in the stock place, off the screen for a long hover line: here it wraps, under the cards.
    // A block too big for the screen (the UI scale is the player's, whatever the resolution) is scaled to fit.

    const float Margin = 24f;       // canvas units kept clear at the screen's edges
    const float SubWidth = 440f;    // where the hover line wraps
    const float SubGap = 6f;
    const float SubSize = 10.35f;   // what the stock line's autosize comes to, in its 15 units of height
    // Sizes are worked out from letter counts: 14 units a title's letter at its stock size (13.8 at the widest of the mode's
    // titles) and 4.7 a hover line's, read on the PC. Asking the dialog's own text (GetPreferredValues) froze the game for a
    // frame of about 100 ms each time a dialog opened, which the stock dialog never does (Dimitri 2026-11-04). The title keeps
    // its stock autosize, which comes to its stock size in its 30 units of height, and shrinks a title too long for the box.
    const float HeadLetter = 14f, SubLetter = 4.7f, SubLine = 15f, HeadMargin = 28f;

    sealed class Spot
    {
        public RectTransform Rect;
        public Vector2 Min, Max, Pivot, Pos, Size;              // the stock values
        public Vector3 Scale;
        public Vector2 WantMin, WantMax, WantPivot, WantPos, WantSize;
        public Vector3 WantScale = Vector3.one;
        public bool Sized;
    }

    sealed class Words_
    {
        public TMPro.TMP_Text Text;
        public TMPro.TextAlignmentOptions Align;
        public bool Wrap, Auto;
        public float Size;
    }

    static readonly List<Spot> spots = new List<Spot>();
    static readonly List<Words_> texts = new List<Words_>();
    static UnityEngine.UI.GridLayoutGroup grid;
    static UnityEngine.UI.GridLayoutGroup.Constraint gridConstraint;
    static UnityEngine.UI.GridLayoutGroup.Axis gridAxis;
    static int gridCount;
    static bool holding;

    static void Place(Bla_Ui_Confirmation dialog, List<Bla_Ui_Confirmation.Entry> cards, string sub)
    {
        try { Fit(dialog, cards, sub); }
        catch (Exception e) { RWLog.Warn("dialog layout: " + e.Message); }
    }

    public static List<RectTransform> Cards(Bla_Ui_Confirmation dialog)
    {
        List<RectTransform> cards = new List<RectTransform>();
        if (dialog != null && dialog.List != null)
            foreach (Transform c in dialog.List.transform)
                if (c.gameObject.activeInHierarchy && c.GetComponent<Blu3_Button>() != null && c is RectTransform)
                    cards.Add((RectTransform)c);
        return cards;
    }

    static Camera Cam(Canvas canvas) { return canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null; }

    public static Rect OnScreen(RectTransform rt)
    {
        Vector3[] corners = new Vector3[4];
        rt.GetWorldCorners(corners);
        return OnScreen(corners, rt.GetComponentInParent<Canvas>());
    }

    static Rect OnScreen(Vector3[] corners, Canvas canvas)
    {
        Camera cam = Cam(canvas);
        Vector2 min = new Vector2(float.MaxValue, float.MaxValue), max = new Vector2(float.MinValue, float.MinValue);
        foreach (Vector3 c in corners)
        {
            Vector2 s = RectTransformUtility.WorldToScreenPoint(cam, c);
            min = Vector2.Min(min, s);
            max = Vector2.Max(max, s);
        }
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    // Where a box's words stand on the screen, not its rectangle: the stock boxes are far wider than their text.
    public static bool Words(GameObject box, out Rect r)
    {
        r = new Rect();
        TMPro.TMP_Text text = box != null && box.activeInHierarchy ? box.GetComponentInChildren<TMPro.TMP_Text>() : null;
        if (text == null || string.IsNullOrEmpty(text.text) || text.text.Trim() == "")
            return false;
        text.ForceMeshUpdate();
        Bounds b = text.textBounds;
        if (b.size.x <= 0f || b.size.y <= 0f)
            return false;
        Transform t = text.transform;
        Vector3[] corners = { t.TransformPoint(new Vector3(b.min.x, b.min.y, 0f)), t.TransformPoint(new Vector3(b.min.x, b.max.y, 0f)),
            t.TransformPoint(new Vector3(b.max.x, b.max.y, 0f)), t.TransformPoint(new Vector3(b.max.x, b.min.y, 0f)) };
        r = OnScreen(corners, text.GetComponentInParent<Canvas>());
        return true;
    }

    // What the block must be scaled by to fit the screen with this many columns; 1 when it fits as it is.
    static float ScaleFor(int cards, int columns, UnityEngine.UI.GridLayoutGroup g, Vector2 room, float above, float below)
    {
        int rows = (cards + columns - 1) / columns;
        float wide = g.padding.left + g.padding.right + columns * g.cellSize.x + (columns - 1) * g.spacing.x;
        float tall = g.padding.top + g.padding.bottom + rows * g.cellSize.y + (rows - 1) * g.spacing.y;
        return Mathf.Min(1f, room.x / wide, room.y / (above + tall + below));
    }

    // Four cards or fewer stand in one row, more in two even rows; if that won't fit, whichever shape stays the largest.
    public static int Columns(int cards, UnityEngine.UI.GridLayoutGroup g, Vector2 room, float above, float below, out float scale)
    {
        int best = cards <= MaxRow ? cards : (cards + 1) / 2;
        scale = ScaleFor(cards, best, g, room, above, below);
        for (int c = 1; scale < 1f && c <= cards; c++)
        {
            float k = ScaleFor(cards, c, g, room, above, below);
            if (k > scale)
            {
                scale = k;
                best = c;
            }
        }
        return best;
    }

    static Spot SpotFor(RectTransform rt)
    {
        foreach (Spot s in spots)
            if (s.Rect == rt) return s;
        Spot spot = new Spot { Rect = rt, Min = rt.anchorMin, Max = rt.anchorMax, Pivot = rt.pivot, Pos = rt.anchoredPosition, Size = rt.sizeDelta, Scale = rt.localScale };
        spots.Add(spot);
        return spot;
    }

    static void Want(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos)
    {
        Spot s = SpotFor(rt);
        s.WantMin = s.WantMax = anchor;
        s.WantPivot = pivot;
        s.WantPos = pos;
    }

    static void Want(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        Want(rt, anchor, pivot, pos);
        Spot s = SpotFor(rt);
        s.WantSize = size;
        s.Sized = true;
    }

    static void KeepWords(TMPro.TMP_Text text)
    {
        foreach (Words_ w in texts)
            if (w.Text == text) return;
        texts.Add(new Words_ { Text = text, Align = text.alignment, Wrap = text.enableWordWrapping, Auto = text.enableAutoSizing, Size = text.fontSize });
    }

    static float LinesTall(string words, float wide)
    {
        return SubLine * Mathf.Max(1, Mathf.CeilToInt((words != null ? words.Length : 0) * SubLetter / wide));
    }

    // The nearest offset from the screen's middle, in canvas units, that lands on a whole screen pixel.
    static float OnPixel(float middle, float pixels, float offset)
    {
        float at = middle + pixels * offset;
        return offset + (Mathf.Round(at) - at) / pixels;
    }

    // Only the list's last cards are this prompt's: the ones before them are the last prompt's, which the game destroys at
    // the end of the frame, so they are hidden now rather than taking cells.
    static void Fit(Bla_Ui_Confirmation dialog, List<Bla_Ui_Confirmation.Entry> entries, string words)
    {
        List<RectTransform> all = Cards(dialog);
        for (int i = 0; i < all.Count - entries.Count; i++)
            all[i].gameObject.SetActive(false);
        int count = Cards(dialog).Count;
        UnityEngine.UI.GridLayoutGroup g = dialog.List.GetComponent<UnityEngine.UI.GridLayoutGroup>();
        RectTransform list = dialog.List.transform as RectTransform, title = dialog.Titlebox.transform as RectTransform, sub = dialog.Subbox.transform as RectTransform;
        TMPro.TMP_Text head = dialog.Titlebox.GetComponent<TMPro.TMP_Text>(), line = dialog.Subbox.GetComponent<TMPro.TMP_Text>();
        Canvas canvas = dialog.GetComponentInParent<Canvas>();
        if (count == 0 || g == null || list == null || title == null || sub == null || head == null || line == null || canvas == null)
            return;
        RectTransform rootRect = (RectTransform)canvas.rootCanvas.transform;
        Vector2 room = rootRect.rect.size - new Vector2(Margin * 2f, Margin * 2f);
        // Beside the chart view the block keeps to the screen's right part, centred at Shift.
        float shift = (Mathf.Clamp(Shift, 0.5f, 0.9f) - 0.5f) * rootRect.rect.width;
        room.x = Mathf.Max(80f, room.x - shift * 2f);

        KeepWords(head);
        head.alignment = TMPro.TextAlignmentOptions.Bottom;
        head.enableWordWrapping = false;
        KeepWords(line);
        line.alignment = TMPro.TextAlignmentOptions.Top;
        line.enableWordWrapping = true;
        line.enableAutoSizing = false;
        line.fontSize = SubSize;

        // The hover line's room is its longest line's, so that no card's words run past the block.
        float subWide = Mathf.Min(room.x, SubWidth), lines = LinesTall(words, subWide);
        foreach (Bla_Ui_Confirmation.Entry e in entries)
            lines = Mathf.Max(lines, LinesTall(e.Display_Desc, subWide));
        float headTall = SpotFor(title).Size.y;

        if (grid != g)
        {
            grid = g;
            gridConstraint = g.constraint;
            gridAxis = g.startAxis;
            gridCount = g.constraintCount;
        }
        float scale;
        int columns = Columns(count, g, room, headTall, SubGap + lines, out scale), rows = (count + columns - 1) / columns;
        g.constraint = UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount;
        g.startAxis = UnityEngine.UI.GridLayoutGroup.Axis.Horizontal;
        g.constraintCount = columns;
        float wide = g.padding.left + g.padding.right + columns * g.cellSize.x + (columns - 1) * g.spacing.x;
        float tall = g.padding.top + g.padding.bottom + rows * g.cellSize.y + (rows - 1) * g.spacing.y;

        // The margin keeps the rule's end mark, at the box's left edge, clear of a long title.
        float headWide = Mathf.Max(wide, head.text.Length * HeadLetter + HeadMargin);
        scale = Mathf.Min(scale, room.x / headWide);

        // The art is pixels drawn three screen pixels to one at a UI scale of 3, sampled without smoothing. On a half pixel
        // a texture's edges fall exactly between two screen pixels, and rounding picks a side unevenly along them: fine
        // notches in every line (Dimitri 2026-11-04). The stock corner is on whole pixels, so the block is put on them too,
        // and a block that must shrink shrinks to a whole number of screen pixels to one.
        float pixels = Screen.height / rootRect.rect.height;
        if (scale < 1f && pixels * scale >= 1f)
            scale = Mathf.Floor(pixels * scale) / pixels;
        headWide = Mathf.Ceil(headWide * pixels) / pixels;

        // The block is the title, the cards and the words under them; its middle stands in the middle of Main.
        float top = OnPixel(Screen.height * 0.5f, pixels, scale * ((headTall + tall + SubGap + lines) * 0.5f - headTall));
        float listX = OnPixel(Screen.width * 0.5f, pixels, shift + wide * scale * 0.5f) - wide * scale * 0.5f;
        float headX = OnPixel(Screen.width * 0.5f, pixels, shift + headWide * scale * 0.5f) - headWide * scale * 0.5f;
        Vector2 middle = new Vector2(0.5f, 0.5f);
        Vector3 scaled = new Vector3(scale, scale, 1f);
        Want(list, middle, new Vector2(0.5f, 1f), new Vector2(listX, top));
        SpotFor(list).WantScale = scaled;
        Want(title, middle, new Vector2(0.5f, 0f), new Vector2(headX, top), new Vector2(headWide, headTall));
        SpotFor(title).WantScale = scaled;
        Want(sub, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(0f, -SubGap), new Vector2(subWide, lines));
        // The corner's rule: in the stock place it runs on to the screen's bottom; here it closes round the cards.
        RectTransform rule = title.Find("Line") as RectTransform;
        if (rule != null)
        {
            Spot s = SpotFor(rule);
            Want(rule, s.Min, s.Pivot, s.Pos, new Vector2(headWide, tall));
        }
        if (!holding)
        {
            Canvas.willRenderCanvases += Hold;
            holding = true;
        }
        Hold();
        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(list);
        // Each card's picture at a whole number of screen pixels per art pixel (pitfall #156). The cards are new with each
        // prompt, so nothing is put back afterwards.
        foreach (RectTransform card in Cards(dialog))
        {
            Transform icon = card.Find("Icon");
            RWAssets.Crisp(icon != null ? icon.GetComponent<UnityEngine.UI.Image>() : null, CardIcon, pixels * scale);
        }
    }

    // Last word before the frame is drawn, for as long as the dialog is the mode's: the game's own transition tweens may
    // write to these rectangles too.
    static void Hold()
    {
        foreach (Spot s in spots)
        {
            RectTransform r = s.Rect;
            if (r == null) continue;
            if (r.anchorMin != s.WantMin) r.anchorMin = s.WantMin;
            if (r.anchorMax != s.WantMax) r.anchorMax = s.WantMax;
            if (r.pivot != s.WantPivot) r.pivot = s.WantPivot;
            if (r.anchoredPosition != s.WantPos) r.anchoredPosition = s.WantPos;
            if (s.Sized && r.sizeDelta != s.WantSize) r.sizeDelta = s.WantSize;
            if (r.localScale != s.WantScale) r.localScale = s.WantScale;
        }
    }

    // Once the dialog has gone, not as it goes: its way out is a slide too, and the cards shouldn't jump to the corner in it.
    static IEnumerator UnplaceLater(int ask)
    {
        float cap = Time.unscaledTime + 3f;
        Blockage.Menu.Blu2_Menu_Handler menu = Menu();
        while (owner == ask && menu != null && (menu.Active || menu.Intransition) && Time.unscaledTime < cap)
            yield return null;
        float gone = Time.unscaledTime + 0.6f;
        while (owner == ask && menu != null && !menu.Active && Time.unscaledTime < gone)
            yield return null;
        if (owner == ask)
            Unplace();
    }

    static void Unplace()
    {
        if (holding)
        {
            Canvas.willRenderCanvases -= Hold;
            holding = false;
        }
        foreach (Spot s in spots)
        {
            RectTransform r = s.Rect;
            if (r == null) continue;
            r.anchorMin = s.Min;
            r.anchorMax = s.Max;
            r.pivot = s.Pivot;
            r.anchoredPosition = s.Pos;
            r.sizeDelta = s.Size;
            r.localScale = s.Scale;
        }
        spots.Clear();
        foreach (Words_ w in texts)
        {
            if (w.Text == null) continue;
            w.Text.alignment = w.Align;
            w.Text.enableWordWrapping = w.Wrap;
            w.Text.enableAutoSizing = w.Auto;
            if (!w.Auto) w.Text.fontSize = w.Size;
        }
        texts.Clear();
        if (grid != null)
        {
            grid.constraint = gridConstraint;
            grid.startAxis = gridAxis;
            grid.constraintCount = gridCount;
        }
        grid = null;
    }
}
