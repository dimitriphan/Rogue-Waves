// Every still picture of the game's menus on whole screen pixels (pitfall #174). At an odd UI scale (3 on Dimitri's PC, where
// one canvas unit is 3 screen pixels) the game's own layout puts its button pictures on half pixels: Play's 48-unit picture is
// centred in its 75-unit tile, 13.5 units in, and the 16-unit icons are centred in 45-unit tiles, 14.5 units in. Every third
// column and row of the screen then samples a picture exactly on the line between two art pixels. The main menu's camera sways
// (Cinemachine noise on its virtual camera) and the UI camera is its child, so the canvas is placed a hair differently each
// frame, and on those lines the art pixel drawn is left to chance: Dimitri saw every button "wobble and jitter", the base game's
// too. The boat's camera sways the same way; a map's moves only when the player moves it.
// A pass moves each still, shown picture by less than half a pixel onto the screen's pixel grid, measured from the canvas's
// corner, where it was before ours; it runs again every half second, so a picture caught while its menu slid in is set right
// once the menu rests. It never moves a picture the game slides, animates, scrolls or lays out itself, nor the
// choice dialog's (RWDialog places those), and a position anyone writes later becomes its new start; a picture someone keeps
// writing is left alone. A pass is spread over frames, a few dozen pictures a frame, with no garbage.
public static class RWPixels
{
    public static bool On = true;           // the self-test compares the game's own places with these
    public static int Seen, Moved, Passes;  // the last whole pass: pictures looked at and moved; passes done
    const int PerFrame = 64;                // a map's HUD holds many hundred pictures
    const float Every = 0.5f;               // a new pass starts at most this often
    const int GiveUp = 2;                   // our writes undone by someone else, with none kept between, before a picture is left alone

    sealed class Seat { public Transform Parent; public bool Ours; public Vector2 Start, Wrote; public bool Written; public int Undone; }

    static readonly List<UnityEngine.UI.Graphic> found = new List<UnityEngine.UI.Graphic>();
    static readonly List<UnityEngine.UI.Graphic> pass = new List<UnityEngine.UI.Graphic>();
    static readonly List<int> passRoot = new List<int>();
    static readonly List<RectTransform> roots = new List<RectTransform>();
    static readonly List<float> rootPixels = new List<float>();
    static readonly Dictionary<RectTransform, Seat> seats = new Dictionary<RectTransform, Seat>();
    static readonly List<RectTransform> dead = new List<RectTransform>();
    static int sceneHandle = int.MinValue, at, moved, seen;
    static float nextPass;

    // Every machine, every frame.
    public static void Update()
    {
        int scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().handle;
        if (scene != sceneHandle)
        {
            sceneHandle = scene;
            seats.Clear();
            pass.Clear();
            passRoot.Clear();
            at = 0;
            nextPass = 0f;
        }
        if (!On)
            return;
        if (at >= pass.Count)
        {
            if (Time.unscaledTime < nextPass)
                return;
            nextPass = Time.unscaledTime + Every;
            Collect();
            at = moved = seen = 0;
        }
        int end = Mathf.Min(pass.Count, at + PerFrame);
        for (; at < end; at++)
            Place(pass[at], passRoot[at]);
        if (at >= pass.Count)
        {
            Seen = seen;
            Moved = moved;
            Passes++;
            Prune();
        }
    }

    // Puts every picture back where the game had it and stops (the self-test's "before"); On = true starts again.
    public static void Restore()
    {
        On = false;
        foreach (KeyValuePair<RectTransform, Seat> s in seats)
            if (s.Key != null && s.Value.Written && s.Key.anchoredPosition == s.Value.Wrote)
                s.Key.anchoredPosition = s.Value.Start;
        seats.Clear();
        at = pass.Count;
        nextPass = 0f;
    }

    // The pictures of every screen-space canvas that holds one of the scene's menus, parents before children.
    static void Collect()
    {
        pass.Clear();
        passRoot.Clear();
        roots.Clear();
        rootPixels.Clear();
        if (Blockage.Menu.Bl_Menus.Menus == null)
            return;
        foreach (Blockage.Menu.Blu2_Menu_Handler menu in Blockage.Menu.Bl_Menus.Menus.Values)
        {
            if (menu == null)
                continue;
            // The outermost canvas, found by hand: a closed menu is inactive, and GetComponentInParent skips those.
            Canvas top = null;
            for (Transform t = menu.transform; t != null; t = t.parent)
            {
                Canvas c = t.GetComponent<Canvas>();
                if (c != null) top = c;
            }
            RectTransform r = top != null ? top.transform as RectTransform : null;
            if (r == null || top.renderMode == RenderMode.WorldSpace || roots.Contains(r))
                continue;
            roots.Add(r);
            rootPixels.Add(RWAssets.ScreenPixels(top));
        }
        for (int i = 0; i < roots.Count; i++)
        {
            roots[i].GetComponentsInChildren<UnityEngine.UI.Graphic>(false, found);
            foreach (UnityEngine.UI.Graphic g in found)
                if (g is UnityEngine.UI.Image || g is UnityEngine.UI.RawImage)
                {
                    pass.Add(g);
                    passRoot.Add(i);
                }
        }
        found.Clear();
    }

    static void Place(UnityEngine.UI.Graphic g, int r)
    {
        RectTransform root = r < roots.Count ? roots[r] : null;
        if (g == null || root == null || !g.isActiveAndEnabled)
            return;
        RectTransform rt = g.transform as RectTransform;
        RectTransform parent = rt != null ? rt.parent as RectTransform : null;
        if (parent == null)
            return;
        Seat seat;
        if (!seats.TryGetValue(rt, out seat) || seat.Parent != parent)
            seats[rt] = seat = new Seat { Parent = parent, Ours = Ours(rt) };
        if (!seat.Ours)
            return;
        seen++;
        float pixels = rootPixels[r];
        Vector3 rootScale = root.lossyScale, parentScale = parent.lossyScale;
        if (pixels <= 0f || rootScale.x == 0f || rootScale.y == 0f || Quaternion.Angle(parent.rotation, root.rotation) > 0.01f
            || Quaternion.Angle(rt.rotation, root.rotation) > 0.01f)
            return;
        // Screen pixels per unit of the parent's space, signed, so a flipped parent moves the right way.
        Vector2 perParent = new Vector2(pixels * parentScale.x / rootScale.x, pixels * parentScale.y / rootScale.y);
        if (Mathf.Abs(perParent.x) < 1e-4f || Mathf.Abs(perParent.y) < 1e-4f)
            return;
        Vector2 now = rt.anchoredPosition;
        if (seat.Written)
        {
            if (now != seat.Wrote)
            {
                // Someone else wrote it since: theirs is the new start, and a picture someone keeps writing isn't ours to move.
                seat.Written = false;
                if (++seat.Undone >= GiveUp) { seat.Ours = false; return; }
            }
            else
                seat.Undone = 0;    // ours held for a whole pass
        }
        Vector2 from = seat.Written ? seat.Start : now;
        // Screen pixels from the canvas's corner to the picture's, where it stood at its start. Both ends are in this canvas, so
        // the canvas's own place, which the camera's sway changes every frame, cancels out.
        Vector2 corner = ((Vector2)root.InverseTransformPoint(rt.TransformPoint(rt.rect.min)) - root.rect.min) * pixels;
        Vector2 px = corner - Vector2.Scale(now - from, perParent);
        // A half pixel always goes the same way, whatever the last digits say.
        Vector2 whole = new Vector2(Mathf.Floor(px.x + 0.52f), Mathf.Floor(px.y + 0.52f));
        Vector2 want = from + new Vector2((whole.x - px.x) / perParent.x, (whole.y - px.y) / perParent.y);
        if (Mathf.Abs((want.x - now.x) * perParent.x) < 0.01f && Mathf.Abs((want.y - now.y) * perParent.y) < 0.01f)
            return;
        seat.Start = from;
        rt.anchoredPosition = want;
        seat.Wrote = rt.anchoredPosition;
        seat.Written = true;
        moved++;
    }

    // Whether the picture is ours to move: nothing of the game's moves it by itself, it isn't in a scrolling list, and it isn't
    // the choice dialog's. The whole chain up to the canvas is read, since the dialog's root sits above its own sliding parts.
    // Something moving a holder above the picture is fine: the next pass at rest puts it right.
    static bool Ours(RectTransform rt)
    {
        if (Moves(rt))
            return false;
        for (Transform t = rt.parent; t != null; t = t.parent)
        {
            if (t.GetComponent<Bla_Ui_Confirmation>() != null || t.GetComponent<UnityEngine.UI.ScrollRect>() != null)
                return false;
            if (t.GetComponent<Canvas>() != null && t.parent == null)
                break;
        }
        return true;
    }

    // The game moves it itself: its transitions, scaler or animator write it, or a layout places it.
    static bool Moves(RectTransform r)
    {
        if (r.GetComponent<Blockage.Menu.Blu4_Scaler>() != null || r.GetComponent<Blockage.Menu.Blu4_Transition_Animator>() != null
            || r.GetComponent<Animator>() != null)
            return true;
        Transform p = r.parent;
        return p != null && p.GetComponent<UnityEngine.UI.LayoutGroup>() != null;
    }

    // Seats of destroyed pictures, after each pass.
    static void Prune()
    {
        foreach (RectTransform k in seats.Keys)
            if (k == null) dead.Add(k);
        foreach (RectTransform k in dead)
            seats.Remove(k);
        dead.Clear();
    }
}
