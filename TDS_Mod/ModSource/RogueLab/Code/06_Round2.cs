// Round 2 (the docs/RESEARCH_GAPS.md gaps named below): the questions the decompiled code could not settle.
//   at boot   every map's and Gameplan's data (A4), every other mode's Tide table and params (A3, C2), the elites
//             again once those groups are loaded (C1), the game's easier setting ("Treated") and its upgrade (A3)
//   map 1     a second Default prefab in the same bundle started (A9), a swarm's frame cost on the host (B5)
//   map 2     the choice dialog with eight long entries: where its cards land (A6)
//   the tour  after map 2, every other base map the vote could offer, played as a twin of the mod's mode: does it start
//             with a vessel, a core and an enemy core, does the game warn about the mode's spots, does leaving work (A4)
// Round 3 (0.3.0) keeps all of it and changes two things round 2 left open (docs/15 §15):
//   map 2     the dialog opens only once its menu is up (round 2's threw after one card), then is clicked by the mod
//   the tour  only the maps round 2 could not reach: every Gameplan whose stock mode isn't Mode_Normal, now twinned
// Every answer is one "[RogueLab] TEST ..." line; the raw data lines are MAP, PLAN, MODE, TIDE2, TOUR.
using System.Globalization;

public static class RogueLabRound2
{
    public static readonly List<string> Tour = new List<string>();   // stock Gameplans still to visit
    public static int ModeWarnings;                                   // the game's "Mode might not work properly" lines
    static bool listening;

    public static IEnumerator Boot()
    {
        Listen();
        try { Maps(); } catch (Exception e) { RogueLabLog.Test("INFO map dump failed: " + e); }
        yield return Modes();
        try { RogueLabEnemies.Dump("modes"); } catch (Exception e) { RogueLabLog.Test("INFO enemy dump failed: " + e); }
        try { Treated("boot"); } catch (Exception e) { RogueLabLog.Test("INFO treated dump failed: " + e); }
    }

    static void Listen()
    {
        if (listening) return;
        listening = true;
        Application.logMessageReceived += OnLog;
    }

    static void OnLog(string message, string stack, LogType type)
    {
        if (message != null && message.Contains("might not work properly"))
            ModeWarnings++;
    }

    // A4: what each map and Gameplan says about itself.
    static void Maps()
    {
        foreach (UnityEngine.Object o in RogueLabUtil.Folder("Map").Values)
        {
            Mapentryobject m = o as Mapentryobject;
            if (m == null) continue;
            RogueLabLog.Test("MAP " + m.Objectname + " '" + m.Display_Name + "' difficulty " + m.Difficulty + " loads " + (m.Maploads != null ? m.Maploads.Objectname : "-")
                + " subloads " + (m.Subloads != null ? m.Subloads.Count : 0));
        }
        foreach (UnityEngine.Object o in RogueLabUtil.Folder("Gameplan").Values)
        {
            Gameplandatabaseobject g = o as Gameplandatabaseobject;
            if (g == null || g.Objectname.StartsWith(RogueLabData.PlanPrefix)) continue;
            RogueLabLog.Test("PLAN " + g.Objectname + " map " + (g.Map != null ? g.Map.Objectname : "-") + " mode " + (g.Mode != null ? g.Mode.Objectname : "-")
                + " soundtrack " + (g.Soundtrack != null ? g.Soundtrack.Objectname : "-") + " par " + RogueLabUtil.F(g.Par) + " record " + g.Record + " altintro " + g.Alternateintro
                + " prediscover " + g.Prediscover + " medals " + (g.Medals != null ? g.Medals.Count : 0));
        }
    }

    // A3, C2: every mode's table, params and start aura (Mode_Normal's is in round 1's TIDE lines).
    static IEnumerator Modes()
    {
        List<Modeentryobject> entries = new List<Modeentryobject>();
        foreach (UnityEngine.Object o in RogueLabUtil.Folder("Mode").Values)
        {
            Modeentryobject m = o as Modeentryobject;
            if (m != null && m.Modeloads != null && m.Objectname != RogueLabData.StockMode && m.Objectname != RogueLabData.Mode)
                entries.Add(m);
        }
        foreach (Modeentryobject entry in entries)
        {
            string folder = entry.Modeloads.Objectname;
            Bl_Address.Loadgroup group = null;
            try { group = Bl_Address.Load(folder); }
            catch (Exception e) { RogueLabLog.Test("INFO loading " + folder + ": " + e.Message); }
            float waited = 0f;
            while (group != null && !group.Loaded && waited < 90f) { waited += Time.unscaledDeltaTime; yield return null; }
            Modeobject m = Bl_Address.Getload("Mode", folder) as Modeobject;
            if (m == null) { RogueLabLog.Test("MODE " + entry.Objectname + " not loaded after " + RogueLabUtil.F(waited) + " s"); continue; }
            try { Table(entry.Objectname, m); } catch (Exception e) { RogueLabLog.Test("INFO table " + entry.Objectname + " failed: " + e.Message); }
        }
        RogueLabProbe.Info("other modes dumped", entries.Count + " (MODE and TIDE2 lines)");
    }

    static void Table(string name, Modeobject m)
    {
        List<string> ps = new List<string>();
        foreach (Modeobject.Param p in m.Defaultparam) ps.Add(p.Name + "=" + RogueLabUtil.F(p.Value));
        Wavesobject w = m.Enemies;
        List<string> init = new List<string>();
        if (w != null && w.Resourcesinit != null) foreach (Link_Resourceset r in w.Resourcesinit) init.Add(r.Name + " " + r.Amount);
        RogueLabLog.Test("MODE " + name + " script " + (m.Modescript != null ? m.Modescript.name : "-") + " client " + (m.Clientmodescript != null ? m.Clientmodescript.name : "-")
            + " spots " + m.Specialspots.Count + " params [" + string.Join(" ", ps.ToArray()) + "] entries " + (w != null ? w.Wave.Count : -1) + " Totalwaves " + (w != null ? w.Totalwaves : -1)
            + " start aura [" + string.Join(", ", init.ToArray()) + "]");
        if (w == null) return;
        for (int i = 0; i < w.Wave.Count; i++)
        {
            Wavesobject.Waves e = w.Wave[i];
            List<string> cs = new List<string>();
            if (e.Content != null)
                foreach (Wavesobject.Wavecontent c in e.Content)
                {
                    List<string> ups = new List<string>();
                    if (c.Upgrades != null) foreach (Upgradeproperties u in c.Upgrades) ups.Add(u != null ? u.Objectname : "null");
                    cs.Add(c.Style + " " + (c.Enemy != null ? c.Enemy.Objectname : "-") + " x" + c.Count + " every " + RogueLabUtil.F(c.Interval) + " gate " + c.Gate + " team " + c.Team
                        + (ups.Count > 0 ? " [" + string.Join(",", ups.ToArray()) + "]" : ""));
                }
            RogueLabLog.Test("TIDE2 " + name + " " + (i + 1) + " " + (e.Wave ? "tide" : "break") + " shown " + e.Displayedwave + " time " + RogueLabUtil.F(e.Wavetime) + " note '" + e.Note + "' | "
                + string.Join(" ; ", cs.ToArray()));
        }
    }

    // A3: the game's own easier setting force-applies this upgrade to the player core (Entity_Ai_Main_Core, "Spawn").
    public static void Treated(string when)
    {
        float setting = Blockage.Settings.Bl_Settings.Readcustomsetting("Difficultysetting");
        Upgradeproperties u = RogueLabUtil.Load<Upgradeproperties>("Upgrade", "Special_Treated_Upgradeproperties");
        if (u == null) { RogueLabProbe.Info("hard setting (" + when + ")", "Difficultysetting " + RogueLabUtil.F(setting) + ", Special_Treated_Upgradeproperties not loaded"); return; }
        List<string> ps = new List<string>();
        foreach (Upgradeproperties.Param p in u.Defaultparam) ps.Add(p.Name + "=" + RogueLabUtil.F(p.Value));
        List<string> ais = new List<string>();
        foreach (UnityEngine.Object a in u.Aiscripts) ais.Add(a != null ? a.name : "-");
        RogueLabProbe.Info("hard setting (" + when + ")", "Difficultysetting " + RogueLabUtil.F(setting) + ", '" + u.Display_Name + "' desc '" + u.Display_Desc + "' ais [" + string.Join(",", ais.ToArray())
            + "] params " + string.Join(" ", ps.ToArray()));
    }

    // A9: the bundle's second prefab (RogueLabMod_Second_Default, its own code) leaves this marker when it starts.
    public static void SecondPrefab()
    {
        GameObject marker = GameObject.Find(RogueLabData.SecondMarker);
        RogueLabProbe.Check("a second Default prefab in the same mod started", marker != null, marker != null ? "marker found" : "no " + RogueLabData.SecondMarker);
    }

    // B5: frame time on the host with 0, 50, 100 and 200 extra friendly units (Boutbro) standing near the vessel.
    public static IEnumerator Swarm(Entity_Master core)
    {
        Entity_Master hero = RogueLabUtil.MainHero();
        Beingobject bro = RogueLabUtil.Load<Beingobject>("Mob", RogueLabData.Bro);
        if (hero == null || bro == null || core == null) { RogueLabProbe.Setup("swarm cost", "hero " + (hero != null) + ", Boutbro " + (bro != null) + ", core " + (core != null)); yield break; }
        Link_Being probe = new Link_Being { Being = bro, Entity = bro.Entity, Block = bro.Block, Team = hero.Entitydata.Value.Team };
        List<Genitem> tiles = new List<Genitem>();
        foreach (Genitem g in ModKitTest.Ground(hero.Entityposition.Value, 10f, 1, 1))
        {
            Genitem st = null;
            try { st = Helper_Gridtools.Checkifstandable(g, probe); } catch (Exception) { }
            if (st != null) tiles.Add(g);
        }
        if (tiles.Count == 0) { RogueLabProbe.Setup("swarm cost", "no tile near the vessel"); yield break; }
        List<Entity_Master> made = new List<Entity_Master>();
        int asked = 0;
        foreach (int target in new[] { 0, 50, 100, 200 })
        {
            while (asked < target)
            {
                Genitem tile = tiles[asked % tiles.Count];
                Link_Spawn spawn = Helper_Entitytools.Createlinkspawnparent(core.Beingserver);
                spawn.Being = bro; spawn.Genadd = tile; spawn.Skin = ""; spawn.Spawnstyle = Link_Spawn.Spawnstyles.Forced;
                spawn.Owner = RogueLabUtil.LocalId; spawn.Team = hero.Entitydata.Value.Team;
                try
                {
                    Link_Being b = Helper_Entitytools.Spawn(spawn, tile.Position, core.Beingserver, true, null);
                    if (b != null && b.Worldobject != null) made.Add(b.Worldobject);
                }
                catch (Exception e) { RogueLabLog.Test("INFO swarm spawn threw " + e.Message); }
                asked++;
                if (asked % 10 == 0) yield return null;
            }
            yield return new WaitForSecondsRealtime(3f);
            float t = 0f, worst = 0f;
            int frames = 0;
            while (t < 5f)
            {
                yield return null;
                float dt = Time.unscaledDeltaTime;
                t += dt;
                frames++;
                if (dt > worst) worst = dt;
            }
            int alive = 0;
            foreach (Entity_Master e in made) if (RogueLabUtil.Alive(e)) alive++;
            RogueLabProbe.Info("swarm " + target, alive + " alive of " + made.Count + " made: mean frame " + RogueLabUtil.F(1000f * t / Mathf.Max(1, frames)) + " ms, worst "
                + RogueLabUtil.F(1000f * worst) + " ms, " + RogueLabUtil.F(frames / t) + " fps (host, solo)");
        }
        foreach (Entity_Master e in made)
            if (RogueLabUtil.Alive(e)) Game_Entity.Instance.Despawn(e.Beingserver, false, "RogueLab");
        yield return new WaitForSeconds(2f);
    }

    // A6: the choice dialog with eight entries and long descriptions. Logs where the game's own layout puts the cards,
    // then the same with four cards per row (Riptide's fix), then closes it by clicking the first card.
    public static IEnumerator Prompt8()
    {
        Bla_Ui_Confirmation dialog = Bla_Ui_Confirmation.Instance;
        if (dialog == null) { RogueLabProbe.Setup("eight-card dialog", "no Bla_Ui_Confirmation"); yield break; }
        yield return MenuUp(dialog);
        List<Bla_Ui_Confirmation.Entry> options = new List<Bla_Ui_Confirmation.Entry>();
        for (int i = 1; i <= 8; i++)
        {
            Bla_Ui_Confirmation.Entry e = new Bla_Ui_Confirmation.Entry();
            e.Name = "RogueLab_Card" + i;
            e.Display_Name = "Direction " + i + " (" + (10 * i) + " Pure Aura)";
            e.Display_Desc = "A long description to see how the dialog wraps it: towers within reach gain twenty percent attack speed, and each Tide survived adds one more stack, up to five.";
            e.Display_Color = new Color(0.3568f, 0.3647f, 0.4117f, 1f);
            options.Add(e);
        }
        Cysharp.Threading.Tasks.UniTask<Bla_Ui_Confirmation.Entry> task = dialog.Runprompt(options, "RogueLab", "Eight entries (no click needed)", true);
        yield return new WaitForSecondsRealtime(2f);
        RogueLabProbe.Info("eight-card dialog, game layout", Layout(dialog));
        UnityEngine.UI.GridLayoutGroup grid = dialog.List != null ? dialog.List.GetComponent<UnityEngine.UI.GridLayoutGroup>() : null;
        if (grid != null)
        {
            grid.constraint = UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 4;
            Canvas.ForceUpdateCanvases();
            yield return null;
            RogueLabProbe.Info("eight-card dialog, four per row", Layout(dialog));
        }
        bool fits = AllOnScreen(dialog);
        RogueLabProbe.Info("eight-card dialog fits on screen (four per row)", fits.ToString());
        Transform card = dialog.List != null ? dialog.List.transform.Find("RogueLab_Card1") : null;
        Blu3_Button button = card != null ? card.GetComponent<Blu3_Button>() : null;
        if (button != null && button.Buttonclick != null) button.Buttonclick();
        float t = 0f;
        while (task.Status == Cysharp.Threading.Tasks.UniTaskStatus.Pending && t < 5f) { t += Time.unscaledDeltaTime; yield return null; }
        RogueLabProbe.Check("eight-card dialog closes when the mod clicks a card", task.Status == Cysharp.Threading.Tasks.UniTaskStatus.Succeeded,
            "status " + task.Status + (task.Status == Cysharp.Threading.Tasks.UniTaskStatus.Succeeded ? ", pick " + task.GetAwaiter().GetResult().Name : Fault(task)));
        if (grid != null) { grid.constraint = UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount = 2; }
    }

    // Runprompt hands each card to its parent menu handler's gamepad focus (Bla_Ui_Confirmation.cs:190), and
    // GetComponentInParent skips inactive objects: with the menu down it throws after the first card (round 2).
    static Blockage.Menu.Blu2_Menu_Handler Handler(Bla_Ui_Confirmation dialog)
    {
        return dialog.GetComponentInParent<Blockage.Menu.Blu2_Menu_Handler>();
    }

    static string MenuState(Bla_Ui_Confirmation dialog)
    {
        Blockage.Menu.Blu2_Menu_Handler active = Handler(dialog);
        Blockage.Menu.Blu2_Menu_Handler[] any = dialog.GetComponentsInParent<Blockage.Menu.Blu2_Menu_Handler>(true);
        string name = dialog.gameObject.name;
        return "dialog '" + name + "' active " + dialog.gameObject.activeInHierarchy + ", handler " + (active != null ? "'" + active.gameObject.name + "'" : "none active")
            + (any.Length > 0 ? " (nearest, active or not: '" + any[0].gameObject.name + "')" : " (none at all)") + ", menu of that name registered " + Blockage.Menu.Bl_Menus.Menus.ContainsKey(name);
    }

    // Brings the dialog's menu up before Runprompt, the way the pause menu's own prompts find it (already up).
    static IEnumerator MenuUp(Bla_Ui_Confirmation dialog)
    {
        string before = MenuState(dialog);
        if (Handler(dialog) != null) { RogueLabProbe.Info("dialog menu", "up already: " + before); yield break; }
        Blockage.Menu.Bl_Menus.Changemenu(dialog.gameObject.name);
        float t = 0f;
        while (Handler(dialog) == null && t < 10f) { t += Time.unscaledDeltaTime; yield return null; }
        RogueLabProbe.Info("dialog menu", "before: " + before + "; " + RogueLabUtil.F(t) + " s after Changemenu: " + MenuState(dialog));
    }

    static string Fault(Cysharp.Threading.Tasks.UniTask<Bla_Ui_Confirmation.Entry> task)
    {
        if (task.Status != Cysharp.Threading.Tasks.UniTaskStatus.Faulted) return "";
        try { task.GetAwaiter().GetResult(); return ""; }
        catch (Exception e) { return ", " + e.GetType().Name + ": " + e.Message + " " + e.StackTrace; }
    }

    static List<RectTransform> Cards(Bla_Ui_Confirmation dialog)
    {
        List<RectTransform> cards = new List<RectTransform>();
        if (dialog.List != null)
            foreach (Transform c in dialog.List.transform)
                if (c.gameObject.activeInHierarchy && c.GetComponent<Blu3_Button>() != null && c is RectTransform)
                    cards.Add((RectTransform)c);
        return cards;
    }

    static Rect Screenrect(RectTransform rt)
    {
        Canvas canvas = rt.GetComponentInParent<Canvas>();
        Camera cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        Vector3[] corners = new Vector3[4];
        rt.GetWorldCorners(corners);
        Vector2 min = new Vector2(float.MaxValue, float.MaxValue), max = new Vector2(float.MinValue, float.MinValue);
        foreach (Vector3 c in corners)
        {
            Vector2 s = RectTransformUtility.WorldToScreenPoint(cam, c);
            min = Vector2.Min(min, s);
            max = Vector2.Max(max, s);
        }
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    static bool AllOnScreen(Bla_Ui_Confirmation dialog)
    {
        List<RectTransform> cards = Cards(dialog);
        if (cards.Count == 0) return false;
        foreach (RectTransform c in cards)
        {
            Rect r = Screenrect(c);
            if (r.xMin < 0f || r.yMin < 0f || r.xMax > Screen.width || r.yMax > Screen.height) return false;
        }
        return true;
    }

    static string Layout(Bla_Ui_Confirmation dialog)
    {
        List<RectTransform> cards = Cards(dialog);
        UnityEngine.UI.GridLayoutGroup grid = dialog.List != null ? dialog.List.GetComponent<UnityEngine.UI.GridLayoutGroup>() : null;
        List<string> rects = new List<string>();
        foreach (RectTransform c in cards)
        {
            Rect r = Screenrect(c);
            rects.Add(RogueLabUtil.F(r.xMin) + "," + RogueLabUtil.F(r.yMin) + " " + RogueLabUtil.F(r.width) + "x" + RogueLabUtil.F(r.height));
        }
        return "screen " + Screen.width + "x" + Screen.height + ", " + cards.Count + " cards, grid " + (grid != null ? grid.constraint + " " + grid.constraintCount + " cell " + grid.cellSize + " spacing " + grid.spacing : "none")
            + ", scroll " + (dialog.List != null && dialog.List.GetComponentInParent<UnityEngine.UI.ScrollRect>() != null) + ", all on screen " + AllOnScreen(dialog) + " | " + string.Join(" ; ", rects.ToArray());
    }

    // A4: one line per map of the tour.
    public static void TourMap(string plan, float loadSeconds, int warningsBefore)
    {
        int cores = 0, subcores = 0, enemycores = 0, merchut = 0;
        List<Entity_Master> list;
        if (Game_Entity.Instance.Entityclienttags.TryGetValue("Core", out list))
            foreach (Entity_Master e in list)
                if (RogueLabUtil.Alive(e)) { if (e.Taglistserver.Contains("Subcore")) subcores++; else if (e.Entitydata.Value.Team != 0) cores++; }
        if (Game_Entity.Instance.Entityclienttags.TryGetValue("Enemycore", out list))
            foreach (Entity_Master e in list) if (RogueLabUtil.Alive(e)) enemycores++;
        if (Game_Entity.Instance.Entityclienttags.TryGetValue("Tower", out list))
            foreach (Entity_Master e in list) if (RogueLabUtil.BeingName(e).Contains("Merchut")) merchut++;
        Game_Mapvars vars = Game_Mapvars.Instance;
        bool mode = vars.Mode == RogueLabBoot.ModeObject && Game_Rounds.Instance.GetComponent<Mode_Normal>() != null;
        RogueLabLog.Test("TOUR " + plan + " map " + Tds_Multiplayerinfo.Instance.Map.Value + " loaded in " + RogueLabUtil.F(loadSeconds) + " s: mod mode " + mode + ", vessel "
            + (RogueLabUtil.MainHero() != null) + ", cores " + cores + ", subcores " + subcores + ", enemy cores " + enemycores + ", Merchut " + merchut + ", mode-spot warnings "
            + (ModeWarnings - warningsBefore));
    }

    // The tour's plans (round 3): every twinned Gameplan whose stock mode is not Mode_Normal (round 2 toured those), not
    // yet played. Alt_0_Tutorial is a real map (Ome Drydock δ) and is in; Special_ and Test_ plans have no twin.
    public static void PlanTour(List<string> played)
    {
        Tour.Clear();
        Dictionary<string, UnityEngine.Object> plans = RogueLabUtil.Folder("Gameplan");
        foreach (string n in RogueLabBoot.Plans)
        {
            Gameplandatabaseobject g = plans.ContainsKey(n) ? plans[n] as Gameplandatabaseobject : null;
            bool toured = g != null && g.Mode != null && g.Mode.Objectname == RogueLabData.StockMode;   // round 2 played all 28 of these
            if (!played.Contains(n) && !toured && !n.StartsWith("Special_") && !n.StartsWith("Test_"))
                Tour.Add(n);
        }
        // The Final map last: its stock plan has a special intro, and a map that hangs ends the tour (TourTimeout).
        List<string> last = Tour.FindAll(n => n.Contains("Final"));
        Tour.RemoveAll(n => n.Contains("Final"));
        Tour.AddRange(last);
        RogueLabProbe.Info("map tour", Tour.Count + " maps: " + string.Join(", ", Tour.ToArray()));
    }
}

