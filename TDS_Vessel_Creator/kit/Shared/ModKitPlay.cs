#if MODKIT_PLAY
// Kit 1.6 add-on, play builds only (Build-<Name>Mod.ps1 -Define <NAME>_PLAY,<NAME>_SHOTS,MODKIT_PLAY,MODKIT_TEST): a
// virtual player for a vessel's play test (2026-10-26). Release builds carry none of it.
// The game reads its world pointer from the Input System's Mouse.current (Game_Client_Mouseobject.Update) and its keys
// and clicks from Input System actions (Bla_Input), so state events queued on the Input System's own mouse and keyboard
// reach the same code a player's hand does: the HUD's buttons, the build bar, pickers, selection, hotkeys. Nothing here
// sends a game command itself. It also starts a match on a chosen map (the self-test's way) and logs what a player
// would see: the HUD's buttons with their screen rectangles, the wallet, the Tide.
public static class ModKitPlay
{
    public static Action<string> Log = delegate { };
    public static Func<string, IEnumerator> Shot;    // the vessel's capture (its <Name>Shots.Shot), or null
    public static MonoBehaviour Host;
    public static string Hero = "";                  // the vessel to pick on the boat
    public static string[] Plans = new string[0];    // the maps to play, in order (a Gameplan's Objectname)
    public static int PlanIndex;
    public static Func<IEnumerator> Scenario;        // runs once a match is on; when it returns, RunScenario moves to the next map
    public static bool Done;

    static int phase;                                 // 0 main menu, 1 world map, 2 match starting, 3 scenario running
    static float seen = -1f, step = -1f;
    static string node = "";
    static int shots;

    public static void Update()
    {
        try { Drive(); Hold(); Watch(); }
        catch (Exception e)
        {
            // One bad frame (a scene changing under it) shouldn't end the run; a stuck driver does after 300 errors.
            errors++;
            if (errors <= 5) Log("driver error " + errors + ": " + e);
            if (errors == 300) { Log("DONE: the driver keeps failing"); Done = true; phase = 9; }
        }
    }
    static int errors;

    public static string CurrentPlan { get { return PlanIndex < Plans.Length ? Plans[PlanIndex] : ""; } }

    // ---------------------------------------------------------------- getting into a match (the self-test's way)

    static void Drive()
    {
        if (phase >= 3 || Done)
            return;
        float now = Time.realtimeSinceStartup;
        Mainmenu_Main menu = UnityEngine.Object.FindObjectOfType<Mainmenu_Main>();
        if (phase == 0 && menu != null)
        {
            if (seen < 0f) seen = now;
            if (now - seen > 8f && step < 0f)
            {
                Log("main menu: embarking");
                menu.SendMessage("Play", SendMessageOptions.DontRequireReceiver);
                step = now;
            }
            if (step > 0f && now - step > 25f)
            {
                Log("embark did not leave the menu, retrying");
                menu.SendMessage("Transitiontomain", SendMessageOptions.DontRequireReceiver);
                step = -1f;
                seen = now;
            }
            return;
        }

        Macro_Main macro = Macro_Main.Instance;
        if (phase <= 1 && macro != null && macro.IsSpawned && Tds_Multiplayer_Remote.Instance != null && Blm_Server.Instance != null && Blm_Server.Instance.Localplayer != null)
        {
            if (phase == 0) { phase = 1; seen = now; step = -1f; Log("world map reached"); }
            if (now - seen < 10f)
                return;
            if (step < 0f)
            {
                if (CurrentPlan == "")
                {
                    Log("DONE: every map played");
                    Done = true;
                    return;
                }
                macro.Selectcharacter(Hero);
                string why;
                node = PickNode(CurrentPlan, out why);
                Log("map " + (PlanIndex + 1) + "/" + Plans.Length + ": " + why + ", node '" + node + "'");
                if (node != "")
                    Tds_Multiplayer_Remote.Instance.Selectmap_ServerRpc(node, false, default(Unity.Netcode.ServerRpcParams));
                step = now;
                return;
            }
            if (now - step > 3f)
            {
                Tds_Clientinfo info = Blm_Server.Instance.Localplayer.GetComponent<Tds_Clientinfo>();
                Log("picked " + (info != null ? info.Hero.Value.ToString() : "?") + " on " + macro.Selected.Value.ToString() + "; starting");
                macro.Playgame(true);
                phase = 2;
                step = now;
            }
            return;
        }

        if (phase == 2)
        {
            if (Game_Mapvars.Instance != null && Game_Entity.Instance != null && Game_Entity.Instance.IsServer && Scenario != null && LocalHero() != null)
            {
                phase = 3;
                Beat();
                Host.StartCoroutine(RunScenario());
            }
            else if (now - step > 150f)
            {
                Log("DONE: the match did not start within 150 s; giving up");
                phase = 9;
                Done = true;
            }
        }
    }

    // The scenario calls Beat() as it goes; three minutes without one ends the run, so a thrown coroutine still ends
    // with a DONE line instead of the launcher's timeout.
    public static float LastBeat;
    public static void Beat() { LastBeat = Time.realtimeSinceStartup; }

    static void Watch()
    {
        if (phase == 3 && !Done && Time.realtimeSinceStartup - LastBeat > 180f)
        {
            Log("DONE: the scenario stalled (no progress for 180 s)");
            Done = true;
            phase = 9;
        }
    }

    static IEnumerator RunScenario()
    {
        yield return Scenario();
        Log("map " + (PlanIndex + 1) + " scenario ended");
        PlanIndex++;
        if (CurrentPlan == "")
        {
            Log("DONE: every map played");
            Done = true;
            yield break;
        }
        // Back to the world map, as the pause menu's "leave" does, then the next map.
        try { Tds_Multiplayer_Remote.Instance.Quitmap_ServerRpc(default(Unity.Netcode.ServerRpcParams)); }
        catch (Exception e) { Log("quit map failed: " + e.Message); Done = true; yield break; }
        holding = false;
        phase = 0;
        seen = -1f;
        step = -1f;
    }

    // The world-map node whose Gameplan is 'plan'; every node is logged once, with its mode's Tide count.
    static bool listed;
    public static string PickNode(string plan, out string why)
    {
        string found = "";
        foreach (Macro_Maplink_Add link in UnityEngine.Object.FindObjectsOfType<Macro_Maplink_Add>())
        {
            if (link.Gameplan == null)
                continue;
            string p = link.Gameplan.Objectname;
            if (!listed)
                Log("MAPS " + link.gameObject.name + " plan " + p + " map '" + (link.Gameplan.Map != null ? link.Gameplan.Map.Display_Name : "?")
                    + "' difficulty " + (link.Gameplan.Map != null ? link.Gameplan.Map.Difficulty : -1) + " mode '" + (link.Gameplan.Mode != null ? link.Gameplan.Mode.Display_Name : "?")
                    + "' waves " + (link.Gameplan.Mode != null ? link.Gameplan.Mode.Waves : -1f) + (link.Unlockcondition != null ? " (unlock condition)" : ""));
            if (p == plan && found == "")
                found = link.gameObject.name;
        }
        listed = true;
        why = found != "" ? "plan " + plan : "plan " + plan + " not on this world map";
        return found;
    }

    public static long LocalSession()
    {
        Blm_Server s = Blm_Server.Instance;
        return s != null && s.Localplayer != null ? s.Localplayer.Sessionid.Value : long.MinValue;
    }

    // The local player's vessel (Game_Selection.Getplayercharacter throws before any hero has spawned, round P0).
    public static Entity_Master LocalHero()
    {
        if (Game_Entity.Instance == null)
            return null;
        long me = LocalSession();
        foreach (Entity_Master e in Game_Entity.Instance.Entityclient)
            if (e != null && e.Beingserver != null && e.Beingserver.Being != null && !e.Beingserver.Dead && e.Beingserver.Being.Objectname == Hero && e.Entitydata.Value.Owner == me)
                return e;
        return null;
    }

    // ---------------------------------------------------------------- the virtual hand

    static UnityEngine.InputSystem.Mouse mouse;
    static UnityEngine.InputSystem.Keyboard keyboard;
    public static Vector2 Pointer = new Vector2(-1f, -1f);
    static bool left, right, holding;
    static readonly List<UnityEngine.InputSystem.Key> keys = new List<UnityEngine.InputSystem.Key>();

    public static void Init()
    {
        mouse = UnityEngine.InputSystem.Mouse.current;
        keyboard = UnityEngine.InputSystem.Keyboard.current;
        string bg = "?";
        try
        {
            bg = UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior.ToString();
            UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior = UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;
        }
        catch (Exception e) { Log("input: background behaviour not set: " + e.Message); }
        bool rib = Application.runInBackground;
        Application.runInBackground = true;
        UnityEngine.EventSystems.EventSystem es = UnityEngine.EventSystems.EventSystem.current;
        Log("input: mouse " + (mouse != null ? mouse.name + " #" + mouse.deviceId : "none") + ", keyboard " + (keyboard != null ? keyboard.name : "none")
            + ", background behaviour was " + bg + ", runInBackground was " + rib + ", focused " + Application.isFocused + ", screen " + Screen.width + "x" + Screen.height
            + ", UI " + (es != null ? es.name + " module " + (es.currentInputModule != null ? es.currentInputModule.GetType().FullName : "none") : "no EventSystem")
            + ", gamepad " + Blockage.Bl_Inputs.Isgamepad);
        Pointer = new Vector2(Screen.width / 2f, Screen.height / 2f);
        holding = true;
        PushMouse();
    }

    static void PushMouse()
    {
        if (mouse == null)
            return;
        UnityEngine.InputSystem.LowLevel.MouseState s = new UnityEngine.InputSystem.LowLevel.MouseState();
        s.position = Pointer;
        s.buttons = (ushort)((left ? 1 : 0) | (right ? 2 : 0));
        UnityEngine.InputSystem.InputSystem.QueueStateEvent(mouse, s);
    }

    static void PushKeys()
    {
        if (keyboard == null)
            return;
        UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState(keys.ToArray()));
    }

    // While the virtual hand is on, a nudge of the real mouse would move the game's pointer: put it back.
    public static int Nudges;
    static void Hold()
    {
        if (!holding || mouse == null)
            return;
        Vector2 now = mouse.position.ReadValue();
        if ((now - Pointer).sqrMagnitude > 1f || mouse.leftButton.isPressed != left || mouse.rightButton.isPressed != right)
        {
            Nudges++;
            PushMouse();
        }
    }

    static IEnumerator Frames(int n)
    {
        for (int i = 0; i < n; i++)
            yield return null;
    }

    public static IEnumerator MoveTo(Vector2 at)
    {
        holding = true;
        Pointer = at;
        PushMouse();
        yield return Frames(3);
    }

    public static IEnumerator Click(Vector2 at, bool rightButton = false)
    {
        yield return MoveTo(at);
        if (rightButton) right = true; else left = true;
        PushMouse();
        yield return Frames(3);
        left = false;
        right = false;
        PushMouse();
        yield return Frames(3);
    }

    public static IEnumerator Press(UnityEngine.InputSystem.Key key)
    {
        if (!keys.Contains(key)) keys.Add(key);
        PushKeys();
        yield return Frames(3);
        keys.Remove(key);
        PushKeys();
        yield return Frames(3);
    }

    // The keyboard key bound to one of the game's actions (Bl_Inputs.Actions), or Key.None.
    public static UnityEngine.InputSystem.Key KeyFor(string action)
    {
        Blockage.Input input;
        if (keyboard == null || Blockage.Bl_Inputs.Actions == null || !Blockage.Bl_Inputs.Actions.TryGetValue(action, out input) || input == null || input.Systemaction == null)
            return UnityEngine.InputSystem.Key.None;
        foreach (UnityEngine.InputSystem.InputBinding b in input.Systemaction.bindings)
        {
            UnityEngine.InputSystem.InputControl c = null;
            try { c = UnityEngine.InputSystem.InputControlPath.TryFindControl(keyboard, b.effectivePath); } catch (Exception) { }
            UnityEngine.InputSystem.Controls.KeyControl k = c as UnityEngine.InputSystem.Controls.KeyControl;
            if (k != null)
                return k.keyCode;
        }
        return UnityEngine.InputSystem.Key.None;
    }

    public static IEnumerator PressAction(string action)
    {
        UnityEngine.InputSystem.Key k = KeyFor(action);
        if (k == UnityEngine.InputSystem.Key.None)
        {
            Log("no keyboard key for action " + action);
            yield break;
        }
        yield return Press(k);
    }

    // Every game action with its bindings, once.
    public static void DumpInputs()
    {
        if (Blockage.Bl_Inputs.Actions == null)
        {
            Log("KEY no actions");
            return;
        }
        foreach (KeyValuePair<string, Blockage.Input> kv in Blockage.Bl_Inputs.Actions)
        {
            string paths = "";
            UnityEngine.InputSystem.InputAction a = kv.Value != null ? kv.Value.Systemaction : null;
            if (a != null)
                foreach (UnityEngine.InputSystem.InputBinding b in a.bindings)
                    paths += b.effectivePath + " ";
            Log("KEY " + kv.Key + " shown '" + (kv.Value != null ? kv.Value.Displaykey : "") + "' | " + paths.Trim() + (a != null && !a.enabled ? " (disabled)" : ""));
        }
        Log("KEY disables now: " + string.Join(",", Blockage.Bl_Inputs.Disables.ToArray()));
    }

    // ---------------------------------------------------------------- the screen

    public static bool OnScreen(Vector3 world, out Vector2 at, float margin)
    {
        at = Vector2.zero;
        Camera cam = Camera.main;
        if (cam == null)
            return false;
        Vector3 p = cam.WorldToScreenPoint(world);
        at = new Vector2(p.x, p.y);
        return p.z > 0f && p.x > margin && p.x < Screen.width - margin && p.y > margin && p.y < Screen.height - margin;
    }

    public static Rect ScreenRect(RectTransform rt)
    {
        Vector3[] c = new Vector3[4];
        rt.GetWorldCorners(c);
        Camera cam = CanvasCamera(rt.GetComponentInParent<Canvas>());
        Vector2 a = RectTransformUtility.WorldToScreenPoint(cam, c[0]);
        Vector2 b = RectTransformUtility.WorldToScreenPoint(cam, c[2]);
        return Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
    }

    // The camera that draws a canvas: none for an overlay, else its own, else the camera whose culling mask holds the
    // canvas's layer (round P1: the HUD's world corners came out in world units with no camera named on the canvas).
    static readonly Dictionary<Canvas, Camera> canvasCams = new Dictionary<Canvas, Camera>();
    public static Camera CanvasCamera(Canvas canvas)
    {
        if (canvas == null)
            return null;
        Canvas root = canvas.rootCanvas;
        if (root.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;
        if (root.worldCamera != null)
            return root.worldCamera;
        Camera cam;
        if (canvasCams.TryGetValue(root, out cam) && cam != null)
            return cam;
        cam = null;
        int bit = 1 << root.gameObject.layer;
        foreach (Camera c in Camera.allCameras)
            if (c != null && c.enabled && (c.cullingMask & bit) != 0 && (cam == null || c.depth > cam.depth))
                cam = c;
        if (cam == null)
            cam = Camera.main;
        canvasCams[root] = cam;
        string all = "";
        foreach (Camera c in Camera.allCameras)
            all += " " + c.name + "(depth " + c.depth + (c.orthographic ? ", ortho" : "") + ", mask " + c.cullingMask + ")";
        Log("canvas " + root.name + ": mode " + root.renderMode + ", layer " + root.gameObject.layer + ", no camera named; using " + (cam != null ? cam.name : "none") + "; cameras:" + all);
        return cam;
    }

    // The match's opening title card ("Cine") lies over the world and swallows clicks; wait until the middle of the
    // screen shows the world again.
    public static IEnumerator WaitForWorld(float timeout)
    {
        float t = 0f;
        Vector2 mid = new Vector2(Screen.width / 2f, Screen.height / 2f);
        string at = UiAt(mid);
        while (at != "nothing" && t < timeout)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
            at = UiAt(mid);
        }
        Log("world clear after " + t.ToString("0.0") + " s (UI at the middle: " + at + ")");
    }

    public static Vector2 Centre(RectTransform rt) { return ScreenRect(rt).center; }

    // What the UI's own raycast finds at a screen point (topmost first).
    public static string UiAt(Vector2 at)
    {
        UnityEngine.EventSystems.EventSystem es = UnityEngine.EventSystems.EventSystem.current;
        if (es == null)
            return "no EventSystem";
        UnityEngine.EventSystems.PointerEventData data = new UnityEngine.EventSystems.PointerEventData(es);
        data.position = at;
        List<UnityEngine.EventSystems.RaycastResult> hits = new List<UnityEngine.EventSystems.RaycastResult>();
        es.RaycastAll(data, hits);
        string s = "";
        for (int i = 0; i < hits.Count && i < 3; i++)
            s += (i > 0 ? " > " : "") + Path(hits[i].gameObject.transform, 3);
        return hits.Count == 0 ? "nothing" : s;
    }

    public static string Path(Transform t, int depth)
    {
        string s = t.name;
        for (int i = 1; i < depth && t.parent != null; i++)
        {
            t = t.parent;
            s = t.name + "/" + s;
        }
        return s;
    }

    public static Transform HudRoot()
    {
        try
        {
            Blockage.Menu.Blu2_Menu_Handler main;
            if (Blockage.Menu.Bl_Menus.Menus != null && Blockage.Menu.Bl_Menus.Menus.TryGetValue("Main", out main) && main != null)
                return main.transform;
        }
        catch (Exception) { }
        return Game_Ui.Instance != null ? Game_Ui.Instance.transform : null;
    }

    static string R(Rect r) { return Mathf.RoundToInt(r.xMin) + "," + Mathf.RoundToInt(r.yMin) + " " + Mathf.RoundToInt(r.width) + "x" + Mathf.RoundToInt(r.height); }

    public static string Text(Transform t)
    {
        TMPro.TMP_Text tmp = t.GetComponent<TMPro.TMP_Text>();
        if (tmp != null)
            return tmp.text;
        UnityEngine.UI.Text ut = t.GetComponent<UnityEngine.UI.Text>();
        return ut != null ? ut.text : null;
    }

    // Every text under t (active), joined: a button's own label and cost.
    public static string Texts(Transform t)
    {
        string s = "";
        foreach (TMPro.TMP_Text tmp in t.GetComponentsInChildren<TMPro.TMP_Text>(false))
            if (tmp.text != null && tmp.text.Trim() != "")
                s += (s != "" ? " | " : "") + tmp.text.Trim();
        return s.Replace("\n", " ");
    }

    // The active HUD below root as indented lines: name, screen rectangle, texts, images, components.
    public static void DumpUi(string label, Transform root, int max)
    {
        if (root == null)
        {
            Log("UI " + label + ": no root");
            return;
        }
        int n = 0;
        Log("UI " + label + " (root " + Path(root, 3) + ")");
        Walk(root, 0, ref n, max);
        if (n >= max) Log("UI ...cut at " + max + " lines");
    }

    static void Walk(Transform t, int depth, ref int n, int max)
    {
        if (n >= max || !t.gameObject.activeInHierarchy)
            return;
        n++;
        string line = new string(' ', depth) + t.name;
        RectTransform rt = t as RectTransform;
        if (rt != null)
            line += " [" + R(ScreenRect(rt)) + "]";
        string tx = Text(t);
        if (tx != null && tx.Trim() != "")
            line += " \"" + (tx.Length > 50 ? tx.Substring(0, 50) : tx).Replace("\n", " ") + "\"";
        UnityEngine.UI.Image img = t.GetComponent<UnityEngine.UI.Image>();
        if (img != null)
            line += " img:" + (img.sprite != null ? img.sprite.name : "-") + (img.raycastTarget ? "*" : "") + (img.color.a < 0.05f ? "(clear)" : "");
        string comps = "";
        foreach (Component c in t.GetComponents<Component>())
        {
            if (c == null) continue;
            string tn = c.GetType().Name;
            if (tn == "RectTransform" || tn == "Transform" || tn == "CanvasRenderer" || tn == "Image" || tn.StartsWith("TextMeshPro") || tn == "LayoutElement")
                continue;
            comps += (comps != "" ? "," : "") + tn;
        }
        if (comps != "")
            line += " {" + comps + "}";
        Log("UI " + line);
        foreach (Transform c in t)
            Walk(c, depth + 1, ref n, max);
    }

    // Active transforms under root whose name contains 'part', in hierarchy order.
    public static List<Transform> Named(Transform root, string part)
    {
        List<Transform> l = new List<Transform>();
        if (root != null)
            foreach (Transform t in root.GetComponentsInChildren<Transform>(false))
                if (t.name.Contains(part))
                    l.Add(t);
        return l;
    }

    // The active transform under root whose own text contains 'text' (case-insensitive), topmost in order.
    public static Transform WithText(Transform root, string text)
    {
        if (root == null)
            return null;
        string want = text.ToLowerInvariant();
        foreach (TMPro.TMP_Text tmp in root.GetComponentsInChildren<TMPro.TMP_Text>(false))
            if (tmp.text != null && tmp.text.ToLowerInvariant().Contains(want))
                return tmp.transform;
        return null;
    }

    // Clicks a HUD element where a player would: its centre, with the virtual mouse. Logs what the UI raycast found
    // there first, so a click that lands on the wrong element shows in the log.
    public static IEnumerator ClickUi(Transform t, string what)
    {
        RectTransform rt = t as RectTransform;
        if (rt == null)
        {
            Log("click " + what + ": not a UI element");
            yield break;
        }
        Vector2 at = Centre(rt);
        yield return MoveTo(at);
        Log("click " + what + " at " + Mathf.RoundToInt(at.x) + "," + Mathf.RoundToInt(at.y) + ": UI there " + UiAt(at));
        yield return Click(at);
    }

    // Clicks a world point (left, or right for orders). False when it isn't on screen.
    public static bool Visible(Vector3 world) { Vector2 at; return OnScreen(world, out at, 80f); }

    public static IEnumerator ClickWorld(Vector3 world, string what, bool rightButton = false)
    {
        Vector2 at;
        if (!OnScreen(world, out at, 80f))
        {
            yield return Look(world);
            if (!OnScreen(world, out at, 80f))
            {
                Log("click " + what + ": " + world.ToString("F1") + " is off screen (" + at.ToString("F0") + ")");
                yield break;
            }
        }
        yield return MoveTo(at);
        yield return Frames(2);
        Log("click " + what + " at " + Mathf.RoundToInt(at.x) + "," + Mathf.RoundToInt(at.y) + " (world " + world.ToString("F1") + "): pointer hits " + PointerHit()
            + ", UI there " + UiAt(at));
        yield return Click(at, rightButton);
    }

    // Aiming like a player does, by looking at what the cursor is on: the pointer is walked out from a point's screen
    // spot in a small spiral until the game's own pointer (Game_Client_Mouseobject) is on the wanted tile or thing.
    // Round P5: on tall maps the spot of a tile's centre often showed a neighbour's side face, so the click built
    // nothing; a building's centre missed its collider just as often.
    static readonly Vector2[] spiral = BuildSpiral();
    static Vector2[] BuildSpiral()
    {
        List<Vector2> l = new List<Vector2> { Vector2.zero };
        for (int r = 6; r <= 48; r += 6)
            for (int a = 0; a < 8; a++)
            {
                float ang = a * Mathf.PI / 4f + (r / 6) * 0.3f;
                l.Add(new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * r);
            }
        return l.ToArray();
    }

    public static Vector3 PointerTile()
    {
        Entity_Master sm = Game_Client_Mouseobject.Surfacemaster;
        if (sm != null && sm.Beingserver != null && sm.Beingserver.Currentblock != null)
            return sm.Beingserver.Currentblock.Position;
        return new Vector3(float.NaN, 0f, 0f);
    }

    public static int Aims, AimMisses;

    static IEnumerator AimAt(Vector3 world, Func<bool> onIt, string what)
    {
        Aimed = false;
        Vector2 at;
        if (!OnScreen(world, out at, 80f))
        {
            yield return Look(world);
            if (!OnScreen(world, out at, 80f))
            {
                Log("aim " + what + ": " + world.ToString("F1") + " is off screen (" + at.ToString("F0") + ")");
                yield break;
            }
        }
        // Scale the spiral to the screen: about a tile's width at the point.
        Vector2 one;
        OnScreen(world + Vector3.right, out one, 0f);
        float tile = Mathf.Clamp((one - at).magnitude, 12f, 200f);
        float k = tile / 48f;
        foreach (Vector2 off in spiral)
        {
            Pointer = at + off * k * 1.6f;
            holding = true;
            PushMouse();
            yield return null;
            yield return null;
            if (onIt())
            {
                Aimed = true;
                Aims++;
                yield break;
            }
        }
        Pointer = at;
        PushMouse();
        yield return null;
        AimMisses++;
        Log("aim " + what + ": the pointer never got on it (on " + PointerHit() + ", tile " + T(PointerTile()) + "); clicking the centre");
    }
    public static bool Aimed;

    // Clicks a ground tile (a build spot, a picker's spot): aimed until the pointer's surface is that tile.
    public static IEnumerator ClickTile(Vector3 tile, string what, bool rightButton = false)
    {
        yield return AimAt(tile, () => { Vector3 p = PointerTile(); return !float.IsNaN(p.x) && (p - tile).sqrMagnitude < 0.3f; }, what);
        Log("click " + what + " at " + Mathf.RoundToInt(Pointer.x) + "," + Mathf.RoundToInt(Pointer.y) + " (tile " + T(tile) + (Aimed ? ", aimed" : ", NOT aimed") + "): pointer on " + T(PointerTile()) + ", UI there " + UiAt(Pointer));
        yield return Click(Pointer, rightButton);
    }

    // Clicks a thing on the map (to select it, to sell it, a picker's target): aimed until the pointer is on it.
    public static IEnumerator ClickThing(Entity_Master m, string what, bool rightButton = false)
    {
        if (!Alive(m))
        {
            Log("click " + what + ": it is gone");
            yield break;
        }
        yield return AimAt(Pos(m) + Vector3.up * 0.35f, () => Game_Client_Mouseobject.Master == m || Game_Client_Mouseobject.Surfacemaster == m, what);
        Log("click " + what + " at " + Mathf.RoundToInt(Pointer.x) + "," + Mathf.RoundToInt(Pointer.y) + (Aimed ? " (aimed)" : " (NOT aimed)") + ": pointer on " + PointerHit() + ", UI there " + UiAt(Pointer));
        yield return Click(Pointer, rightButton);
    }

    public static string PointerHit()
    {
        Entity_Master m = Game_Client_Mouseobject.Master;
        Entity_Master sm = Game_Client_Mouseobject.Surfacemaster;
        return (Game_Client_Mouseobject.Hit ? Game_Client_Mouseobject.Position.ToString("F1") : "no hit")
            + (m != null && m.Beingserver != null && m.Beingserver.Being != null ? " on " + m.Beingserver.Being.Objectname : "")
            + (sm != null && sm.Beingserver != null && sm.Beingserver.Being != null ? ", surface " + sm.Beingserver.Being.Objectname : "");
    }

    // Brings a world point to the middle of the screen by moving the camera's target (Game_Cameracontroller.Movepos),
    // the point a player's pan keys move. Logged, since it is a camera nudge, not a hand.
    // Movepos is where the camera rig stands, not what it looks at (round P3: the point under it showed far below the
    // screen), so each pass shifts it by the gap between the point and where the screen's middle meets that height.
    public static IEnumerator Look(Vector3 world)
    {
        Game_Cameracontroller cc = Game_Cameracontroller.Instance;
        Camera cam = Camera.main;
        if (cc == null || cam == null)
            yield break;
        Vector3 before = cc.Movepos;
        Vector2 at;
        int passes = 0;
        for (; passes < 4; passes++)
        {
            if (OnScreen(world, out at, Mathf.Min(Screen.width, Screen.height) * 0.25f))
                break;
            Ray ray = cam.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0f));
            Plane level = new Plane(Vector3.up, world);
            float d;
            Vector3 mid = level.Raycast(ray, out d) ? ray.GetPoint(d) : new Vector3(cc.Movepos.x, world.y, cc.Movepos.z);
            Vector3 shift = world - mid;
            shift.y = 0f;
            cc.Movepos += shift;
            yield return new WaitForSecondsRealtime(1.2f);
        }
        OnScreen(world, out at, 0f);
        Looks++;
        if (Looks <= 8)
            Log("camera: Movepos " + before.ToString("F1") + " -> " + cc.Movepos.ToString("F1") + " for " + world.ToString("F1") + " in " + passes + " passes, now on screen at " + at.ToString("F0"));
    }
    public static int Looks;

    // ---------------------------------------------------------------- what the player sees

    public static Tds_Clientinfo LocalInfo()
    {
        return Blm_Server.Instance != null && Blm_Server.Instance.Localplayer != null ? Blm_Server.Instance.Localplayer.GetComponent<Tds_Clientinfo>() : null;
    }

    public static float Aura(string name)
    {
        Tds_Clientinfo info = LocalInfo();
        if (info == null)
            return -1f;
        foreach (Link_Resource r in info.Resources)
            if (r.Name.ToString() == name)
                return r.Amount;
        return 0f;
    }

    public static string Wallet()
    {
        return "R" + Mathf.FloorToInt(Aura("Redaura")) + " B" + Mathf.FloorToInt(Aura("Blueaura")) + " Y" + Mathf.FloorToInt(Aura("Yellowaura"));
    }

    public static string Selected()
    {
        Game_Selection gs = Game_Selection.Instance;
        if (gs == null)
            return "no selection object";
        Entity_Master s = gs.Selected;
        return (s != null && s.Beingserver != null && s.Beingserver.Being != null ? s.Beingserver.Being.Objectname : "nothing")
            + (gs.Activecommand != null ? ", command " + gs.Activecommand.Objectname : "");
    }

    public static string ActiveCommand()
    {
        Game_Selection gs = Game_Selection.Instance;
        return gs != null && gs.Activecommand != null ? gs.Activecommand.Objectname : "";
    }

    public static IEnumerator Capture(string name)
    {
        if (Shot == null)
            yield break;
        shots++;
        yield return Shot(shots.ToString("000") + "_" + name);
    }

    // ---------------------------------------------------------------- a player's moves, for any vessel
    // Each move is done by hand first (the HUD, the keys, the map) and only then, if the game didn't do it, with the
    // command the button would have sent, logged FALLBACK. The counters say how many of each went by hand.

    public static Entity_Master Me;
    public static long Owner;
    public static int Team;
    public static int UiBuilds, UiUpgrades, UiSells, UiCommands, UiWalks, Fallbacks, KeySelects, ClickSelects;
    public static bool LastOk;

    public static void Reset(Entity_Master hero)
    {
        Me = hero;
        Owner = hero != null ? hero.Entitydata.Value.Owner : -1;
        Team = hero != null ? hero.Entitydata.Value.Team : 1;
        UiBuilds = UiUpgrades = UiSells = UiCommands = UiWalks = Fallbacks = KeySelects = ClickSelects = 0;
    }

    public static string Counts()
    {
        return "by hand: builds " + UiBuilds + ", upgrades " + UiUpgrades + ", sells " + UiSells + ", commands " + UiCommands + ", walks " + UiWalks
            + "; vessel found by F " + KeySelects + ", by click " + ClickSelects + "; fallbacks " + Fallbacks + "; pointer nudges " + Nudges;
    }

    public static bool Alive(Entity_Master m) { return m != null && m.Beingserver != null && !m.Beingserver.Dead; }
    public static Vector3 Pos(Entity_Master m) { return m.Entityposition.Value; }
    public static string Name(Entity_Master m) { return m != null && m.Beingserver != null && m.Beingserver.Being != null ? m.Beingserver.Being.Objectname : "?"; }
    public static string T(Vector3 v) { return Mathf.RoundToInt(v.x) + "," + Mathf.RoundToInt(v.y) + "," + Mathf.RoundToInt(v.z); }
    static string F1(float v) { return v.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture); }
    static string Json(Vector3 v) { return "{\"x\":" + F1(v.x) + ",\"y\":" + F1(v.y) + ",\"z\":" + F1(v.z) + "}"; }

    public static bool IsSelected(Entity_Master m) { return m != null && Game_Selection.Instance != null && Game_Selection.Instance.Selected == m; }

    static IEnumerator Until(Func<bool> c, float timeout)
    {
        float t = 0f;
        while (!c() && t < timeout)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    // Drops an armed command: a right click, then Esc, which drops a build or picker (rounds P2-P6; a right click
    // alone doesn't, round P8). If Esc paused the match instead, Unpause presses it again.
    public static IEnumerator Cancel()
    {
        if (ActiveCommand() == "")
            yield break;
        yield return Click(Pointer, true);
        yield return new WaitForSecondsRealtime(0.25f);
        if (ActiveCommand() != "")
        {
            yield return Press(UnityEngine.InputSystem.Key.Escape);
            yield return new WaitForSecondsRealtime(0.3f);
            yield return Unpause();
        }
        if (ActiveCommand() != "")
            Log("command still active after a right click and Esc: " + ActiveCommand());
    }
    // If the match got paused (the pause menu), say so once and unpause it with the Pause key.
    public static IEnumerator Unpause()
    {
        if (Time.timeScale > 0.001f)
            yield break;
        Log("the match is paused (Time.timeScale 0); pressing Pause to resume");
        yield return Press(UnityEngine.InputSystem.Key.Escape);
        yield return new WaitForSecondsRealtime(0.5f);
    }

    // F (the Focus key) first, as a player finds the vessel; then a click on it; then the HUD's vessel button.
    public static IEnumerator SelectVessel()
    {
        yield return Cancel();
        if (IsSelected(Me))
            yield break;
        yield return PressAction("Focus");
        yield return new WaitForSecondsRealtime(0.4f);
        if (IsSelected(Me)) { KeySelects++; yield break; }
        if (KeySelects + ClickSelects == 0) Log("F didn't select the vessel (" + Selected() + "); clicking it");
        yield return ClickThing(Me, "the vessel");
        yield return new WaitForSecondsRealtime(0.3f);
        if (IsSelected(Me)) { ClickSelects++; yield break; }
        List<Transform> vs = Named(HudRoot(), "Vesselselect");
        if (vs.Count > 0)
        {
            yield return ClickUi(vs[0], "the HUD's vessel button");
            yield return new WaitForSecondsRealtime(0.4f);
        }
        if (IsSelected(Me)) ClickSelects++;
        else Log("vessel not selected: " + Selected());
    }

    // A click on one of your things on the map (a building or a unit).
    public static IEnumerator Select(Entity_Master m, string what)
    {
        yield return Cancel();
        if (IsSelected(m) || !Alive(m))
            yield break;
        yield return ClickThing(m, what);
        yield return new WaitForSecondsRealtime(0.3f);
        if (!IsSelected(m))
        {
            // A unit walks: aim where it is now.
            yield return ClickThing(m, what + " (again)");
            yield return new WaitForSecondsRealtime(0.3f);
        }
        if (!IsSelected(m))
            Log("click didn't select the " + what + ": " + Selected());
    }

    // The build bar's entries are Selection/Buildlist/Buildoption1..4 (round P2).
    public static Transform BuildEntry(int slot)
    {
        foreach (Transform t in Named(HudRoot(), "Buildoption" + (slot + 1)))
            if (t.name == "Buildoption" + (slot + 1))
                return t;
        return null;
    }

    public static string BuildBar()
    {
        string s = "";
        for (int i = 0; i < 6; i++)
        {
            Transform t = BuildEntry(i);
            if (t != null) s += " [" + (i + 1) + ": " + Texts(t) + "]";
        }
        return s == "" ? " none" : s;
    }

    // A command button on the selected thing's HUD (Selection/Commandlist), by its title (SELL, TARGET, PATCH...).
    public static Transform CommandButton(string title)
    {
        Transform root = HudRoot();
        if (root == null)
            return null;
        string want = title.Trim().ToLowerInvariant();
        foreach (TMPro.TMP_Text tmp in root.GetComponentsInChildren<TMPro.TMP_Text>(false))
        {
            // A toggle's title carries its state ("MEND OFF", round P9).
            string have = tmp.text != null ? tmp.text.Trim().ToLowerInvariant() : "";
            if (have != want && !have.StartsWith(want + " "))
                continue;
            Transform t = tmp.transform;
            while (t != null && t.GetComponent<UnityEngine.UI.Button>() == null && t != root)
                t = t.parent;
            return t != null && t != root ? t : tmp.transform;
        }
        return null;
    }

    static int missingShots;

    // What the thing's commands are on the host and in the synced list the HUD draws from, and whose it is.
    public static string CommandState(Entity_Master m)
    {
        if (m == null) return "no thing";
        string server = m.Commandlistserver != null ? string.Join(",", new List<string>(m.Commandlistserver.Keys).ToArray()) : "-";
        int synced = -1;
        try { synced = m.Commandlist != null ? m.Commandlist.Count : -1; } catch (Exception) { }
        return "host commands [" + server + "], synced " + synced + ", owner " + m.Entitydata.Value.Owner + " (me " + LocalSession() + "), selected " + IsSelected(m);
    }

    public static string CommandTitles()
    {
        string s = "";
        foreach (Transform t in Named(HudRoot(), "Commandlist"))
            foreach (Transform c in t)
                if (c.gameObject.activeInHierarchy)
                    s += " [" + c.name + ": " + Texts(c) + "]";
        return s == "" ? " none" : s;
    }

    static UnityEngine.InputSystem.Key Digit(int n)
    {
        switch (n)
        {
            case 1: return UnityEngine.InputSystem.Key.Digit1;
            case 2: return UnityEngine.InputSystem.Key.Digit2;
            case 3: return UnityEngine.InputSystem.Key.Digit3;
            case 4: return UnityEngine.InputSystem.Key.Digit4;
            case 5: return UnityEngine.InputSystem.Key.Digit5;
            case 6: return UnityEngine.InputSystem.Key.Digit6;
            case 7: return UnityEngine.InputSystem.Key.Digit7;
            default: return UnityEngine.InputSystem.Key.Digit8;
        }
    }

    // Build through the HUD: select the vessel, click its build-bar entry (or press its number), turn it with R, point
    // at the tile and click; the vessel walks into reach by itself. 'done' says the new thing stands. LastOk is set.
    public static IEnumerator Build(int slot, string being, Vector3 at, int turns, Func<bool> done, string why)
    {
        LastOk = false;
        Vector3 from = Pos(Me);
        yield return SelectVessel();
        Transform entry = BuildEntry(slot);
        bool armed = false;
        string how = "";
        if (entry != null)
        {
            yield return ClickUi(entry, "build-bar entry " + (slot + 1) + " (" + Texts(entry) + ")");
            yield return new WaitForSecondsRealtime(0.2f);
            armed = ActiveCommand() != "";
            how = "bar";
        }
        if (!armed)
        {
            Log((entry != null ? "build-bar click armed nothing" : "no build-bar entry " + (slot + 1)) + " (" + Selected() + "); pressing " + (slot + 1));
            yield return Press(Digit(slot + 1));
            yield return new WaitForSecondsRealtime(0.2f);
            armed = ActiveCommand() != "";
            how = "key " + (slot + 1);
        }
        if (armed)
        {
            yield return Look(at);
            Vector2 sp;
            if (OnScreen(at, out sp, 80f))
                yield return MoveTo(sp);
            for (int i = 0; i < turns; i++)
                yield return PressAction("Rotate");
            yield return new WaitForSecondsRealtime(0.25f);
            if (UiBuilds < 3 || turns > 0)
                yield return Capture("placing_" + being);
            yield return ClickTile(at, "placing " + being + (turns > 0 ? " (turned " + turns + ")" : ""));
            yield return Until(done, 15f);
            yield return Cancel();
        }
        else
            Log("couldn't arm " + being + " (" + Selected() + ")");
        if (done())
        {
            UiBuilds++;
            LastOk = true;
            Log("BUILT by hand (" + how + "): " + being + " at " + T(at) + " (" + why + "); vessel walked " + F1(ModKitTest.Flat(from, Pos(Me))) + " tiles; wallet " + Wallet());
            yield break;
        }
        if (!Alive(Me)) { Log("build of " + being + " dropped: the vessel is down"); yield break; }
        Fallbacks++;
        string json = "{\"Tower\":\"" + being + "\",\"Position\":" + Json(at) + ",\"Orientation\":" + (turns % 4) + ",\"Owner\":" + Owner + "}";
        Me.GetComponent<Entity_Master_Remote>().Command_ServerRpc("Build", json, default(Unity.Netcode.ServerRpcParams));
        yield return Until(done, 6f);
        LastOk = done();
        Log("FALLBACK build " + being + " at " + T(at) + ": " + (LastOk ? "built" : "refused (vessel " + F1(ModKitTest.Flat(Pos(Me), at)) + " tiles away)") + "; wallet " + Wallet());
    }

    // Upgrade through the HUD: select the thing, click the upgrade's entry by its name (Selection/Buildlist/UpgradeoptionN).
    public static IEnumerator Upgrade(Entity_Master m, string id, string display, Func<bool> has, string what)
    {
        LastOk = false;
        yield return Select(m, what);
        Transform entry = WithText(HudRoot(), display);
        if (entry != null)
        {
            yield return ClickUi(entry, "upgrade " + display);
            yield return Until(has, 1.5f);
        }
        else
            Log("no HUD entry '" + display + "' with the " + what + " selected (" + Selected() + "); entries:" + UpgradeEntries() + "; " + CommandState(m));
        if (has())
        {
            UiUpgrades++;
            LastOk = true;
            Log("UPGRADED by hand: " + what + " at " + T(Pos(m)) + " -> " + display + "; wallet " + Wallet());
            yield break;
        }
        if (entry != null)
            Log("upgrade click on '" + display + "' didn't take; entries:" + UpgradeEntries() + "; wallet " + Wallet());
        if (!Alive(m)) yield break;
        Fallbacks++;
        m.GetComponent<Entity_Master_Remote>().Command_ServerRpc("Upgrade", "{\"Upgrade\":\"" + id + "\",\"Upgrader\":" + Owner + "}", default(Unity.Netcode.ServerRpcParams));
        yield return Until(has, 2f);
        LastOk = has();
        Log("FALLBACK upgrade " + what + " -> " + display + ": " + (LastOk ? "taken" : "refused") + "; wallet " + Wallet());
    }

    public static string UpgradeEntries()
    {
        string s = "";
        foreach (Transform t in Named(HudRoot(), "Upgradeoption"))
            if (t.name.StartsWith("Upgradeoption"))
                s += " [" + t.name + ": " + Texts(t) + "]";
        return s == "" ? " none" : s;
    }

    // Sell through the HUD: the vessel's SELL (Selection/Commandlist/Sell5), then a click on the thing.
    public static IEnumerator Sell(Entity_Master m, string what)
    {
        LastOk = false;
        string before = Wallet();
        yield return SelectVessel();
        Transform sell = CommandButton("SELL");
        if (sell != null)
        {
            yield return ClickUi(sell, "SELL");
            yield return new WaitForSecondsRealtime(0.3f);
            Log("after SELL: " + Selected());
            if (Alive(m))
                yield return ClickThing(m, "the " + what + " to sell");
            yield return Until(() => !Alive(m), 3f);
        }
        else
            Log("no SELL on the vessel's HUD (" + Selected() + "); buttons:" + CommandTitles());
        yield return Cancel();
        LastOk = !Alive(m);
        if (LastOk) UiSells++;
        Log(LastOk ? "SOLD by hand: " + what + "; wallet " + before + " -> " + Wallet() : "sell by hand failed for the " + what + " (" + Selected() + ")");
    }

    // A command button on one of your things (TARGET, SPOT, RETURN, MEND, PATCH...): select it, click the button and,
    // for a picker, click 'at' on the map (or the thing 'target'). 'done' says it worked. LastOk is set.
    public static IEnumerator Command(Entity_Master owner, string ownerWhat, string title, bool picker, Vector3 at, Entity_Master target, Func<bool> done)
    {
        LastOk = false;
        if (owner == Me) yield return SelectVessel();
        else yield return Select(owner, ownerWhat);
        Transform b = CommandButton(title);
        if (b == null)
        {
            Log("no " + title + " button on the " + ownerWhat + " (" + Selected() + "); buttons:" + CommandTitles() + "; " + CommandState(owner));
            if (missingShots++ < 3)
            {
                yield return Capture("no_" + title + "_on_" + ownerWhat.Replace(" ", ""));
                DumpUi("selected " + ownerWhat + " without " + title, HudRoot(), 90);
            }
            yield break;
        }
        yield return ClickUi(b, title + " on the " + ownerWhat);
        yield return new WaitForSecondsRealtime(0.3f);
        if (picker)
        {
            string active = ActiveCommand();
            Log(title + " armed: " + (active != "" ? active : "nothing"));
            if (UiCommands < 6) yield return Capture("picker_" + title);
            if (target != null && Alive(target))
                yield return ClickThing(target, title + " on " + Name(target));
            else
                yield return ClickTile(at, title + " at " + T(at));
        }
        yield return Until(done, 3f);
        yield return Cancel();
        LastOk = done();
        if (LastOk) UiCommands++;
        Log((LastOk ? "COMMAND by hand: " : "command by hand didn't take: ") + title + " on the " + ownerWhat + (picker ? (target != null ? " -> " + Name(target) : " -> " + T(at)) : ""));
    }

    // Walking the vessel: select it, right-click the ground (the Move command's key, Quickaction); if that doesn't
    // move it, the HUD's MOVE button and a left click on the ground.
    public static IEnumerator WalkTo(Vector3 at, string why)
    {
        yield return SelectVessel();
        Vector3 from = Pos(Me);
        if (ModKitTest.Flat(from, at) < 0.6f)
            yield break;
        Genitem g = ModKitTest.Surface(at);
        if (g != null) at = g.Position;
        yield return ClickTile(at, "walk to " + T(at) + " (" + why + ")", true);
        yield return Until(() => ModKitTest.Flat(Pos(Me), from) > 0.4f, 1.5f);
        if (ModKitTest.Flat(Pos(Me), from) > 0.4f) { UiWalks++; yield break; }
        Transform move = CommandButton("MOVE");
        if (move != null)
        {
            Log("right click didn't move the vessel (" + Selected() + "); trying MOVE");
            yield return ClickUi(move, "MOVE");
            yield return new WaitForSecondsRealtime(0.2f);
            yield return ClickTile(at, "MOVE to " + T(at));
            yield return Until(() => ModKitTest.Flat(Pos(Me), from) > 0.4f, 1.5f);
        }
        if (ModKitTest.Flat(Pos(Me), from) > 0.4f) UiWalks++;
        else Log("couldn't walk the vessel to " + T(at) + " (" + Selected() + ")");
        yield return Cancel();
    }

    public static IEnumerator FastForward()
    {
        List<Transform> ff = Named(HudRoot(), "Fastforward");
        if (ff.Count == 0) { Log("no fast-forward button"); yield break; }
        bool before = Game_Mapvars.Instance.Time_Speedup.Value;
        yield return ClickUi(ff[0], "fast-forward");
        yield return new WaitForSecondsRealtime(1f);
        Log("fast-forward: " + before + " -> " + Game_Mapvars.Instance.Time_Speedup.Value + ", Time.timeScale " + F1(Time.timeScale));
    }

    // ---------------------------------------------------------------- the map, as a player reads it

    public static List<Entity_Master> Cores(string tag, bool own)
    {
        List<Entity_Master> l = new List<Entity_Master>();
        if (Game_Entity.Instance == null) return l;
        foreach (Entity_Master e in Game_Entity.Instance.Entityclient)
            if (e != null && e.Taglistserver != null && e.Taglistserver.Contains(tag) && Alive(e) && (e.Entitydata.Value.Team == Team) == own)
                l.Add(e);
        return l;
    }

    public static Beingobject Mob(string name)
    {
        List<Blockage.Address.Bl_Address.Loadlist> lists;
        if (!Blockage.Address.Bl_Address.Groups.TryGetValue("Mob", out lists))
            return null;
        foreach (Blockage.Address.Bl_Address.Loadlist list in lists)
        {
            UnityEngine.Object item;
            if (list.Items.TryGetValue(name, out item) && item is Beingobject)
                return (Beingobject)item;
        }
        return null;
    }

    // Each foe path from a spawner to one of the team's cores, as the game plans it for a Dagger (Helper_Navtools).
    public static readonly List<List<Vector3>> Lanes = new List<List<Vector3>>();

    public static void PlanLanes()
    {
        Lanes.Clear();
        Beingobject dagger = Mob("Enemy_Dagger");
        if (dagger == null) { Log("lanes: no Dagger to ask"); return; }
        foreach (Entity_Master spawner in Cores("Enemycore", false))
            foreach (Entity_Master core in Cores("Core", true))
            {
                try
                {
                    Link_Being probe = new Link_Being { Being = dagger, Entity = dagger.Entity, Block = dagger.Block, Team = spawner.Entitydata.Value.Team };
                    Genitem start = ModKitTest.FoeTile(dagger, Pos(spawner), 0f, 3f, null);
                    Genitem end = ModKitTest.FoeTile(dagger, Pos(core), 1f, 3f, null);
                    if (start == null || end == null) { Log("lane " + Name(spawner) + " -> " + Name(core) + ": no tile"); continue; }
                    Link_Path p = Helper_Navtools.Getpath(start, end, probe, true);
                    if (!p.Valid || p.Path == null || p.Path.Count == 0)
                        p = Helper_Navtools.Getpath(start, Pos(core), probe, true);
                    if (!p.Valid || p.Path == null || p.Path.Count == 0) { Log("lane " + Name(spawner) + " " + T(start.Position) + " -> " + Name(core) + " " + T(end.Position) + ": no path (valid " + p.Valid + ", length " + p.Length + ")"); continue; }
                    List<Vector3> l = new List<Vector3>();
                    foreach (Genitem g in p.Path) l.Add(g.Position);
                    // The path comes back goal first (round P4 built at the spawners): spawner first, core last.
                    if (ModKitTest.Flat(l[0], Pos(core)) < ModKitTest.Flat(l[l.Count - 1], Pos(core)))
                        l.Reverse();
                    Lanes.Add(l);
                    string s = "";
                    for (int i = 0; i < l.Count; i += Mathf.Max(1, l.Count / 8)) s += " " + T(l[i]);
                    Log("lane " + Name(spawner) + " " + T(Pos(spawner)) + " -> " + Name(core) + " " + T(Pos(core)) + ": " + l.Count + " tiles:" + s + " ... " + T(l[l.Count - 1]));
                }
                catch (Exception e) { Log("lane error: " + e.Message); }
            }
    }

    // Tiles foes walked, by count (call Watch every half second).
    public static readonly Dictionary<string, int> Trail = new Dictionary<string, int>();
    public static readonly Dictionary<string, int> FoeKinds = new Dictionary<string, int>();
    static readonly List<Entity_Master> seenFoes = new List<Entity_Master>();

    public static List<Entity_Master> Foes()
    {
        List<Entity_Master> l = new List<Entity_Master>();
        if (Game_Entity.Instance == null) return l;
        foreach (Entity_Master e in Game_Entity.Instance.Entityclient)
            if (e != null && Alive(e) && e.Taglistserver != null && e.Taglistserver.Contains("Enemy") && e.Entitydata.Value.Team != Team)
                l.Add(e);
        return l;
    }

    public static void ResetMap()
    {
        Trail.Clear();
        FoeKinds.Clear();
        seenFoes.Clear();
        Lanes.Clear();
    }

    public static void WatchFoes()
    {
        foreach (Entity_Master f in Foes())
        {
            string k = T(Pos(f));
            int n;
            Trail.TryGetValue(k, out n);
            Trail[k] = n + 1;
            if (!seenFoes.Contains(f))
            {
                seenFoes.Add(f);
                string kind = Name(f);
                FoeKinds.TryGetValue(kind, out n);
                FoeKinds[kind] = n + 1;
            }
        }
        if (seenFoes.Count > 400) seenFoes.RemoveRange(0, 200);
    }

    public static string FoesSeen()
    {
        string s = "";
        foreach (KeyValuePair<string, int> kv in FoeKinds) s += " " + kv.Key.Replace("Enemy_", "") + "x" + kv.Value;
        return s == "" ? " none" : s;
    }

    public static string TrailText(int max)
    {
        List<KeyValuePair<string, int>> l = new List<KeyValuePair<string, int>>(Trail);
        l.Sort((a, b) => b.Value.CompareTo(a.Value));
        string s = "";
        for (int i = 0; i < l.Count && i < max; i++) s += " " + l[i].Key + "x" + l[i].Value;
        return s;
    }

    // A free tile for 'being' beside the lanes, last stretch first: about 1.5 tiles off a lane tile 3-12 tiles before a
    // core, the n-th further out and on the next lane; with no lane, beside the tiles foes walked most near a core.
    public static Vector3 LaneSpot(Beingobject being, int n, List<Vector3> used, float off)
    {
        if (Lanes.Count > 0)
            for (int tries = 0; tries < 12; tries++)
            {
                List<Vector3> lane = Lanes[(n + tries) % Lanes.Count];
                int back = Mathf.Min(lane.Count - 1, 3 + ((n + tries) / Lanes.Count) * 2);
                Vector3 at = ModKitTest.Spot(lane[lane.Count - 1 - back], being, Team, off, off + 1.5f, used, being.Objectname);
                if (!float.IsNaN(at.x)) return at;
            }
        List<KeyValuePair<string, int>> walked = new List<KeyValuePair<string, int>>(Trail);
        walked.Sort((a, b) => b.Value.CompareTo(a.Value));
        List<Entity_Master> cores = Cores("Core", true);
        foreach (KeyValuePair<string, int> kv in walked)
        {
            string[] p = kv.Key.Split(',');
            Vector3 t = new Vector3(int.Parse(p[0]), int.Parse(p[1]), int.Parse(p[2]));
            bool near = false;
            foreach (Entity_Master c in cores) if (ModKitTest.Flat(t, Pos(c)) <= 10f) near = true;
            if (!near) continue;
            Vector3 at = ModKitTest.Spot(t, being, Team, off, off + 1.5f, used, being.Objectname);
            if (!float.IsNaN(at.x)) return at;
        }
        Entity_Master core = cores.Count > 0 ? cores[n % cores.Count] : Me;
        return ModKitTest.Spot(Pos(core), being, Team, 3f, 7f, used, being.Objectname);
    }

    // A lane tile itself (for a pin, a spot to guard): 'back' tiles before the core on lane n.
    public static Vector3 LaneTile(int n, int back)
    {
        if (Lanes.Count == 0) return new Vector3(float.NaN, 0f, 0f);
        List<Vector3> lane = Lanes[n % Lanes.Count];
        return lane[Mathf.Clamp(lane.Count - 1 - back, 0, lane.Count - 1)];
    }

    // The quarter turn that faces 'at' toward the nearest lane tile (orientation 0 faces +z; how a press of R turns is
    // the vessel's to log).
    public static int TurnsToward(Vector3 at)
    {
        Vector3 best = at + Vector3.forward;
        float bd = float.MaxValue;
        foreach (List<Vector3> lane in Lanes)
            foreach (Vector3 t in lane)
            {
                float d = ModKitTest.Flat(t, at);
                if (d > 0.5f && d < bd) { bd = d; best = t; }
            }
        Vector3 dir = best - at;
        if (Mathf.Abs(dir.z) >= Mathf.Abs(dir.x)) return dir.z >= 0f ? 0 : 2;
        return dir.x >= 0f ? 1 : 3;
    }

    public static List<Entity_Master> Ore()
    {
        List<Entity_Master> ore = new List<Entity_Master>();
        if (Game_Entity.Instance == null) return ore;
        foreach (Entity_Master e in Game_Entity.Instance.Entityclient)
            if (e != null && e.Taglistserver != null && e.Taglistserver.Contains("Resource") && e.Statsserver != null && e.Statsserver.ContainsKey("Healthresource"))
                ore.Add(e);
        Vector3 home = Cores("Core", true).Count > 0 ? Pos(Cores("Core", true)[0]) : (Me != null ? Pos(Me) : Vector3.zero);
        ore.Sort((a, b) => ModKitTest.Flat(Pos(a), home).CompareTo(ModKitTest.Flat(Pos(b), home)));
        return ore;
    }

    // A free tile for a gatherer within 'reach' of an ore node, the n-th node nearest the core first.
    public static Vector3 OreSpot(Beingobject being, int n, float reach, List<Vector3> used)
    {
        List<Entity_Master> ore = Ore();
        for (int i = 0; i < ore.Count; i++)
        {
            Entity_Master node = ore[(n + i) % ore.Count];
            Vector3 at = ModKitTest.Spot(Pos(node), being, Team, 1.5f, reach - 0.3f, used, being.Objectname);
            if (!float.IsNaN(at.x))
            {
                Log("gatherer spot " + T(at) + " beside " + Name(node) + " at " + T(Pos(node)));
                return at;
            }
        }
        Log("no ore node with a free tile in reach (" + ore.Count + " nodes)");
        return new Vector3(float.NaN, 0f, 0f);
    }

    public static bool Afford(string costs, float reserveRed)
    {
        if (string.IsNullOrEmpty(costs)) return true;
        foreach (string part in costs.Split(';'))
        {
            string[] kv = part.Split('=');
            if (kv.Length != 2) continue;
            float need = float.Parse(kv[1], System.Globalization.CultureInfo.InvariantCulture) + (kv[0] == "Redaura" ? reserveRed : 0f);
            if (Aura(kv[0]) < need) return false;
        }
        return true;
    }

    public static string Cores()
    {
        string s = "";
        foreach (Entity_Master c in Cores("Core", true))
        {
            float hp = 0f, max = 0f;
            Link_Stats st;
            if (c.Statsserver != null && c.Statsserver.TryGetValue("Health", out st) && st != null) { hp = st.Value; max = st.Value_Calc; }
            s += " " + Name(c).Replace("Main_", "") + " " + F1(hp) + "/" + F1(max);
        }
        return s;
    }

    public static string Tide()
    {
        Game_Mapvars mv = Game_Mapvars.Instance;
        return "tide " + mv.Wave.Value + " timer " + F1(mv.Time_Wave.Value) + (mv.Time_Speedup.Value ? " ff" : "");
    }

    public static bool Over(out string state)
    {
        state = Game_Mapvars.Instance.Gamecomplete.Value.ToString();
        return state != "" && state != "Running";
    }
}
#endif
