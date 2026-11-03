// The game's choice dialog (Bla_Ui_Confirmation, kit docs/20 §2) for every choice the mode asks on one machine: the main
// menu's New / Continue, the Direction pick, the votes, VENDCINT. Its menu is opened with Force and awaited first
// (Runprompt's own Changemenu isn't forced, and it throws while its parent menu is inactive, kit docs/15 §14.6; RogueLab
// round 3). It has no timeout and no cancel, so a timed ask closes the menu itself and treats the pending task as no
// answer. The stock dialog sits in a corner with two cards a row, made for the pause menu's Exit / Back: a choice of three
// or more is laid out in one centred row, and put back when it closes (Riptide's Salvage, Dimitri 2026-10-05).
public static class RWDialog
{
    public const int MaxRow = 4;
    public static readonly Color Teal = new Color(0.247f, 0.561f, 0.588f, 1f), Swell = new Color(0.18f, 0.29f, 0.384f, 1f),
        Coral = new Color(0.886f, 0.447f, 0.357f, 1f), Pearl = new Color(0.957f, 0.933f, 0.973f, 1f), Sand = new Color(0.847f, 0.769f, 0.604f, 1f);
    static int asks;
    public static bool Asking;
    public static string LastTitle = "";     // the self-test reads what was asked
    public static int LastCards;

    public static bool Up()
    {
        Bla_Ui_Confirmation dialog = Bla_Ui_Confirmation.Instance;
        Blockage.Menu.Blu2_Menu_Handler menu;
        return dialog != null && Blockage.Menu.Bl_Menus.Menus != null && Blockage.Menu.Bl_Menus.Menus.TryGetValue(dialog.gameObject.name, out menu) && menu != null && menu.Active;
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

    // Asks on this machine. done(name) gets the picked card's Name, or null when the time ran out, the menu closed, or a
    // newer Ask took over. seconds <= 0 waits for as long as the dialog stays up. Run as a coroutine on the mod's object.
    public static IEnumerator Ask(List<Bla_Ui_Confirmation.Entry> cards, string title, string sub, float seconds, bool centre, Action<string> done)
    {
        Bla_Ui_Confirmation dialog = Bla_Ui_Confirmation.Instance;
        int ask = ++asks;
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
        if (centre) Place(dialog, cards.Count);
        yield return null;
        yield return null;
        if (centre) Place(dialog, cards.Count);
        float settle = Time.unscaledTime + 0.5f, end = seconds > 0f ? Time.unscaledTime + seconds : float.MaxValue;
        // A UniTask's Status can't be read after GetResult (RogueLab round 5): read it once a step, the result once.
        Cysharp.Threading.Tasks.UniTaskStatus status = task.Status;
        while (status == Cysharp.Threading.Tasks.UniTaskStatus.Pending && ask == asks && Up() && Time.unscaledTime < end)
        {
            if (centre && settle > 0f && Time.unscaledTime >= settle)
            {
                settle = 0f;
                Place(dialog, cards.Count);
            }
            yield return null;
            status = task.Status;
        }
        if (centre) Unfit(dialog);
        string picked = null;
        if (status == Cysharp.Threading.Tasks.UniTaskStatus.Succeeded)
        {
            try { Bla_Ui_Confirmation.Entry e = task.GetAwaiter().GetResult(); picked = e != null ? e.Name : null; }
            catch (Exception e) { RWLog.Warn("dialog '" + title + "' result: " + e.Message); }
        }
        else if (ask == asks && Up())
            Blockage.Menu.Bl_Menus.Closemenu(dialog.gameObject, true);
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

    // The subtitle of the dialog on screen (a vote's tally).
    public static void Subtitle(string text)
    {
        Bla_Ui_Confirmation dialog = Bla_Ui_Confirmation.Instance;
        TMPro.TMP_Text t = dialog != null && dialog.Subbox != null ? dialog.Subbox.GetComponentInChildren<TMPro.TMP_Text>() : null;
        if (t != null && t.text != text)
            t.text = text;
    }

    // ---------------------------------------------------------------- placing it (RiptideSalvage.Centre, proven)

    static readonly List<RectTransform> movedParts = new List<RectTransform>();
    static readonly List<Vector2> movedFrom = new List<Vector2>();
    static UnityEngine.UI.GridLayoutGroup grid;
    static UnityEngine.UI.GridLayoutGroup.Constraint gridConstraint;
    static int gridCount;
    static Vector2 listSize;

    static void Place(Bla_Ui_Confirmation dialog, int cards)
    {
        try { Centre(dialog, cards); }
        catch (Exception e) { RWLog.Warn("dialog layout: " + e.Message); }
    }

    static List<RectTransform> Cards(Bla_Ui_Confirmation dialog)
    {
        List<RectTransform> cards = new List<RectTransform>();
        if (dialog != null && dialog.List != null)
            foreach (Transform c in dialog.List.transform)
                if (c.gameObject.activeInHierarchy && c.GetComponent<Blu3_Button>() != null && c is RectTransform)
                    cards.Add((RectTransform)c);
        return cards;
    }

    static List<RectTransform> Parts(Bla_Ui_Confirmation dialog)
    {
        List<RectTransform> parts = new List<RectTransform>();
        foreach (GameObject piece in new GameObject[] { dialog.List, dialog.Titlebox, dialog.Subbox, dialog.Descbox })
        {
            if (piece == null || piece == dialog.gameObject)
                continue;
            Transform p = piece.transform;
            while (p.parent != null && p.parent != dialog.transform)
                p = p.parent;
            RectTransform part = (p.parent == dialog.transform ? p : piece.transform) as RectTransform;
            if (part != null && !parts.Contains(part))
                parts.Add(part);
        }
        return parts;
    }

    static Camera Cam(Canvas canvas) { return canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null; }

    static Rect OnScreen(RectTransform rt, out Canvas canvas)
    {
        canvas = rt.GetComponentInParent<Canvas>();
        Vector3[] corners = new Vector3[4];
        rt.GetWorldCorners(corners);
        return OnScreen(corners, canvas);
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

    static bool Words(GameObject box, out Rect r)
    {
        r = new Rect();
        TMPro.TMP_Text text = box != null && box.activeInHierarchy ? box.GetComponentInChildren<TMPro.TMP_Text>() : null;
        if (text == null || string.IsNullOrEmpty(text.text) || text.text.Trim() == "")
            return false;
        Bounds b = text.textBounds;
        if (b.size.x <= 0f || b.size.y <= 0f)
            return false;
        Transform t = text.transform;
        Vector3[] corners = { t.TransformPoint(new Vector3(b.min.x, b.min.y, 0f)), t.TransformPoint(new Vector3(b.min.x, b.max.y, 0f)),
            t.TransformPoint(new Vector3(b.max.x, b.max.y, 0f)), t.TransformPoint(new Vector3(b.max.x, b.min.y, 0f)) };
        r = OnScreen(corners, text.GetComponentInParent<Canvas>());
        return true;
    }

    static Rect Union(Rect a, Rect b) { return Rect.MinMaxRect(Mathf.Min(a.xMin, b.xMin), Mathf.Min(a.yMin, b.yMin), Mathf.Max(a.xMax, b.xMax), Mathf.Max(a.yMax, b.yMax)); }

    static Rect Block(Bla_Ui_Confirmation dialog, List<RectTransform> cards, out Canvas canvas)
    {
        canvas = null;
        bool any = false;
        Rect block = new Rect();
        foreach (RectTransform card in cards)
        {
            Rect r = OnScreen(card, out canvas);
            block = any ? Union(block, r) : r;
            any = true;
        }
        foreach (GameObject box in new GameObject[] { dialog.Titlebox, dialog.Subbox })
        {
            Rect r;
            if (Words(box, out r))
            {
                block = any ? Union(block, r) : r;
                any = true;
            }
        }
        return block;
    }

    static void Row(Bla_Ui_Confirmation dialog, int n)
    {
        UnityEngine.UI.GridLayoutGroup g = dialog.List.GetComponent<UnityEngine.UI.GridLayoutGroup>();
        RectTransform list = dialog.List.transform as RectTransform;
        if (g == null || list == null)
            return;
        if (grid != g)
        {
            Unrow();
            grid = g;
            gridConstraint = g.constraint;
            gridCount = g.constraintCount;
            listSize = list.sizeDelta;
        }
        int columns = Mathf.Clamp(n, 1, MaxRow);
        g.constraint = UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount;
        g.constraintCount = columns;
        float width = g.padding.left + g.padding.right + columns * g.cellSize.x + (columns - 1) * g.spacing.x;
        if (list.rect.width < width - 0.5f)
            list.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
    }

    static void Unrow()
    {
        if (grid != null)
        {
            grid.constraint = gridConstraint;
            grid.constraintCount = gridCount;
            RectTransform list = grid.transform as RectTransform;
            if (list != null)
                list.sizeDelta = listSize;
        }
        grid = null;
    }

    // With fresh > 0, only the list's last fresh buttons are this prompt's cards: the ones before them are the last
    // prompt's, which the game destroys at the end of the frame, so they are hidden now rather than taking cells.
    static void Centre(Bla_Ui_Confirmation dialog, int fresh)
    {
        if (fresh > 0)
        {
            List<RectTransform> all = Cards(dialog);
            for (int i = 0; i < all.Count - fresh; i++)
                all[i].gameObject.SetActive(false);
        }
        List<RectTransform> cards = Cards(dialog);
        if (cards.Count == 0)
            return;
        Row(dialog, cards.Count);
        Canvas.ForceUpdateCanvases();
        RectTransform root = dialog.transform as RectTransform;
        if (root != null)
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(root);
        Canvas canvas;
        Rect r = Block(dialog, cards, out canvas);
        Vector2 middle = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        if ((middle - r.center).magnitude <= 2f)
            return;
        Camera cam = Cam(canvas);
        foreach (RectTransform part in Parts(dialog))
        {
            RectTransform parent = part.parent as RectTransform;
            Vector3 from, to;
            if (parent == null || !RectTransformUtility.ScreenPointToWorldPointInRectangle(parent, r.center, cam, out from)
                || !RectTransformUtility.ScreenPointToWorldPointInRectangle(parent, middle, cam, out to))
                continue;
            if (!movedParts.Contains(part))
            {
                movedParts.Add(part);
                movedFrom.Add(part.anchoredPosition);
            }
            part.position += to - from;
        }
    }

    static void Unfit(Bla_Ui_Confirmation dialog)
    {
        for (int i = 0; i < movedParts.Count; i++)
            if (movedParts[i] != null)
                movedParts[i].anchoredPosition = movedFrom[i];
        movedParts.Clear();
        movedFrom.Clear();
        Unrow();
    }
}
