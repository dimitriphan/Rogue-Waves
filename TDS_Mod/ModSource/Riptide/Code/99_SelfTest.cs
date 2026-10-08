using Blockage.Menu;
#if RIPTIDE_SELFTEST
// Debug build only (Build-RiptideMod.ps1 -Define RIPTIDE_SELFTEST[,RIPTIDE_SANDBOX][,RIPTIDE_SHOTS]). Runs the research
// report's PC probes 1 to 5 in order, solo as host, and logs one "[Riptide] TEST PASS|FAIL name: detail" line per
// check plus "TEST DONE pass=.. fail=..". It starts the match by itself (Embark, pick Smacky and a Mode_Normal node,
// turn RIPTIDE on, ready up) because the game ignores injected input, as the vessel self-tests do.
//   1. The mode is registered; the switch turns the node into Riptide; the match runs Mode_Riptide on Mode_Normal and
//      its Tides advance with Riptide's banner. Logs the stock Tide table and the foes' stats for tuning. The ending
//      (a fallen core ends the run) is checked last, because it ends the match.
//   2. The director writes the next Tide during a break, and exactly the written foes spawn; a hand-written Tide with
//      an elite spawns the elite.
//   3. Artifacts granted mid-match: every artifact's kind is logged; one of each kind is granted; core upgrades,
//      catch-up on a standing tower and the vessel, the HUD row, and the lobby list put back.
//   4. The game's choice dialog (Bla_Ui_Confirmation.Runprompt) from mod code with three real offers.
//   5. The sandbox (98_Sandbox.cs, -Define RIPTIDE_SANDBOX): a refused API refuses the whole mod, so it has its own
//      define. If the game refuses the mod with it, that is probe 5's answer: rebuild without it for probes 1-4.
// v0.2 adds the wiring the probes cleared:
//   switch:  the RIPTIDE switch on the Setup screen turns Riptide on, and the "TYPE n RIPTIDE" line shows it.
//   salvage: a Salvage end to end: the offers on the vessel's SALVAGE command, the dialog opening by itself, a pick by
//            click reaching the host and granting the artifact, the clock held meanwhile; then a timed-out Salvage
//            picking Driftwood for the player and closing the dialog. (The run's own Salvages are off until then.)
using UnityEngine.SceneManagement;
using Unity.Netcode;

public static class RiptideSelfTest
{
    const string Vessel = "Hero_Smacky";
    const string TestTower = "Tower_Smackyarcher";

    static int phase;
    static float seen = -1f, step = -1f;
    static string node = "", stockPlan = "";
    static bool freeze, forcing;
    static int pass, fail, errors, cancels;
    static readonly List<string> errorLines = new List<string>();

    static void Log(string m) { RiptideLog.Info("TEST " + m); }

    public static bool Check(string name, bool ok, string detail = "")
    {
        if (ok) pass++; else fail++;
        Log((ok ? "PASS " : "FAIL ") + name + (detail != "" ? ": " + detail : ""));
        return ok;
    }

    public static void Update()
    {
        try { Drive(); }
        catch (Exception e) { Log("driver error: " + e); phase = 9; }
    }

    // Counts exceptions and errors anywhere in the game while a probe runs. The game's own async tasks log an
    // OperationCanceledException whenever their object goes away mid-wait (a Merchant Hut's mark, the Setup screen's
    // slide): stock behaviour, counted apart.
    static void OnLog(string message, string stack, LogType type)
    {
        if (type != LogType.Exception && type != LogType.Error)
            return;
        if (message.StartsWith("[Riptide] TEST"))
            return;
        if (message.StartsWith("OperationCanceledException"))
        {
            cancels++;
            return;
        }
        errors++;
        if (errorLines.Count < 20)
            errorLines.Add(message.Length > 160 ? message.Substring(0, 160) : message);
    }

    static int ErrorsSince(int mark) { return errors - mark; }

    // ---------------------------------------------------------------- getting into a match

    static void Drive()
    {
        Game_Mapvars mv = Game_Mapvars.Instance;
        bool host = Game_Entity.Instance != null && Game_Entity.Instance.IsServer;
        if (freeze && !forcing && mv != null && host && mv.Time_Wave.Value < 600f)
            mv.Time_Wave.Value = 900f;
        if (forcing && mv != null && host)
            mv.Time_Wave.Value = -1f;
        if (phase >= 3)
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
            if (phase == 0)
            {
                phase = 1;
                seen = now;
                step = -1f;
                Application.logMessageReceived += OnLog;
                RiptideSalvage.Enabled = false;     // the run's own Salvages wait for the salvage check
                Log("on the world map (" + SceneManager.GetActiveScene().name + ")");
            }
            if (now - seen < 8f)
                return;
            if (step < 0f)
            {
                macro.Selectcharacter(Vessel);
                node = PickNode();
                Log("picked " + Vessel + " and map node '" + node + "'");
                if (node != "")
                    Tds_Multiplayer_Remote.Instance.Selectmap_ServerRpc(node, false, default(ServerRpcParams));
                step = now;
                return;
            }
            if (now - step > 2f && now - step < 3f && stockPlan == "")
            {
                stockPlan = Tds_Multiplayerinfo.Instance.Gameplan.Value.ToString();
                Probe1Registered();
                SwitchOn();
                return;
            }
            if (now - step > 4f && now - step < 5f)
            {
                Tds_Multiplayerinfo info = Tds_Multiplayerinfo.Instance;
                string twin;
                RiptideContent.Twins.TryGetValue(stockPlan, out twin);
                Check("probe 1: the switch turns the node's mode into Riptide", info.Mode.Value.ToString() == RiptideData.Mode, info.Mode.Value.ToString());
                Check("probe 1: the switch swaps the Gameplan to its Riptide twin", twin != null && info.Gameplan.Value.ToString() == twin,
                    stockPlan + " -> " + info.Gameplan.Value);
                TypeLine();
                step = now - 6f;
                RiptideMod.Instance.StartCoroutine(ShotThenPlay(macro));
            }
            if (now - step > 60f)
            {
                Log("match did not start; giving up");
                phase = 9;
            }
            return;
        }

        if (phase <= 2 && RiptideUtil.InRun && host && Hero() != null)
        {
            phase = 3;
            RiptideMod.Instance.StartCoroutine(Scenario());
        }
    }

    static string PickNode()
    {
        string fallback = "";
        foreach (Macro_Maplink_Add link in UnityEngine.Object.FindObjectsOfType<Macro_Maplink_Add>())
        {
            Gameplandatabaseobject gp = link.Gameplan;
            if (gp == null || gp.Mode == null || gp.Mode.Objectname != RiptideData.TemplateMode || link.Unlockcondition != null)
                continue;
            if (gp.Objectname == Tds_Multiplayerinfo.Instance.Gameplan.Value.ToString())
                return link.gameObject.name;
            if (fallback == "")
                fallback = link.gameObject.name;
        }
        return fallback;
    }

    // ---------------------------------------------------------------- the switch, on the world map

    static void SwitchOn()
    {
        Blu3_Toggle t = RiptideSwitch.Toggle;
        Check("switch: the RIPTIDE switch is on the Setup screen", t != null && t.gameObject.activeSelf, RiptideSwitch.Layout());
        int layer = t != null && t.Menu != null ? t.Menu.Layer : -1, active = Bl_Menus.Ui != null ? Bl_Menus.Ui.Activelayer : -1;
        UnityEngine.UI.Button button = t != null ? t.GetComponent<UnityEngine.UI.Button>() : null;
        if (button != null && layer == active)
        {
            button.onClick.Invoke();
            Check("switch: clicking it turns RIPTIDE on", RiptideDirector.Switch && t.Toggle, "switch " + RiptideDirector.Switch + ", toggle " + t.Toggle);
        }
        else
            Log("SKIP switch: clicking it (the Setup menu's layer is " + layer + ", the active layer " + active + "); setting the switch directly");
        RiptideDirector.Switch = true;
    }

    // The Setup screen as the host sees it with the switch on, once it has slid in and the TYPE line has changed.
    static IEnumerator ShotThenPlay(Macro_Main macro)
    {
        yield return new WaitForSecondsRealtime(1f);
        Log("switch layout: " + RiptideSwitch.Layout());
#if RIPTIDE_SHOTS
        yield return RiptideShots.Shot("setup_switch");
#endif
        Log("ready: starting the match");
        macro.Playgame(true);
        yield return null;
    }

    static void TypeLine()
    {
        Blu2_Menu_Handler setup;
        GameObject stats;
        Transform line = Bl_Menus.Menus.TryGetValue("Setup", out setup) && setup.Items.TryGetValue("Stats", out stats) && stats != null ? stats.transform.Find("Difficulty") : null;
        TMPro.TMP_Text text = line != null ? line.GetComponent<TMPro.TMP_Text>() : null;
        Macro_Maplink_Add node = Macro_Main.Instance.Actualselected;
        if (text == null || node == null || node.Gameplan == null || string.IsNullOrEmpty(text.text))
        {
            Log("SKIP switch: the TYPE line (" + (text == null ? "no line" : node == null ? "no node picked" : "the node has not been played, so the game leaves it empty") + ")");
            return;
        }
        Check("switch: the Setup screen says TYPE n RIPTIDE", text.text == RiptideSwitch.Line(node.Gameplan.Map.Difficulty), "'" + text.text + "'");
    }

    // ---------------------------------------------------------------- probe 1, on the world map

    static void Probe1Registered()
    {
        Modeentryobject entry = RiptideUtil.Entry<Modeentryobject>("Mode", RiptideData.Mode);
        Check("probe 1: the mode entry is in the master database", entry != null && entry == RiptideContent.Entry && entry.Modeloads.Objectname == RiptideData.Mode,
            entry != null ? entry.Display_Name + ", loads " + entry.Modeloads.Objectname : "missing");
        UnityEngine.Object named;
        Check("probe 1: the Modeobject resolves by name like the game's loader", Bl_Address.Groupsname.TryGetValue(new FixedString64Bytes(RiptideData.Mode), out named)
            && named == RiptideContent.Mode && RiptideUtil.Load<Modeobject>("Mode", RiptideData.Mode) == RiptideContent.Mode && Bl_Address.Loadedfolders.ContainsKey(RiptideData.Mode));
        Modeobject m = RiptideContent.Mode;
        Check("probe 1: the mode keeps Mode_Normal's scripts and params", m.Modescript != null && m.Modescript.name == RiptideContent.Template.Modescript.name
            && m.Clientmodescript != null && m.Defaultparams != null && m.Defaultparams.ContainsKey("Startwait"),
            (m.Modescript != null ? m.Modescript.name : "-") + " / " + (m.Clientmodescript != null ? m.Clientmodescript.name : "-") + ", params " + Params(m));
        Check("probe 1: the table is pre-sized", m.Enemies == RiptideContent.Table && m.Enemies.Wave.Count == 2 * RiptideData.RunTides && m.Enemies.Totalwaves == RiptideData.RunTides,
            m.Enemies.Wave.Count + " entries, Totalwaves " + m.Enemies.Totalwaves);
        Check("probe 1: a Gameplan twin for every Mode_Normal node", RiptideContent.Twins.Count > 0, RiptideContent.Twins.Count + " twins");
        Check("probe 1: Mode_Normal's own table is untouched", RiptideContent.Template.Enemies.Wave != RiptideContent.Table.Wave);
        DumpStock();
    }

    static string Params(Modeobject m)
    {
        List<string> list = new List<string>();
        if (m.Defaultparams != null)
            foreach (KeyValuePair<string, float> p in m.Defaultparams)
                list.Add(p.Key + "=" + RiptideUtil.F(p.Value));
        return string.Join(", ", list.ToArray());
    }

    // The stock data the budget curve is tuned against (DESIGN "Tide budget curve").
    static void DumpStock()
    {
        Wavesobject w = RiptideContent.Template.Enemies;
        Log("stock " + RiptideData.TemplateMode + ": " + w.Wave.Count + " entries, Totalwaves " + w.Totalwaves + ", params " + Params(RiptideContent.Template));
        List<string> init = new List<string>();
        foreach (Link_Resourceset r in w.Resourcesinit) init.Add(r.Name + " " + RiptideUtil.F(r.Amount));
        Log("stock starting aura: " + string.Join(", ", init.ToArray()));
        int tide = 0;
        for (int i = 0; i < w.Wave.Count; i++)
        {
            Wavesobject.Waves e = w.Wave[i];
            if (e.Wave) tide++;
            List<string> parts = new List<string>();
            if (e.Content != null)
                foreach (Wavesobject.Wavecontent c in e.Content) parts.Add(RiptideUtil.Describe(c));
            Log("stock entry " + (i + 1) + (e.Wave ? " TIDE " + tide : " break") + ", time " + RiptideUtil.F(e.Wavetime) + ", note '" + e.Note + "': "
                + string.Join("; ", parts.ToArray()));
        }
    }

    static void DumpFoes()
    {
        foreach (RiptideData.Foe f in RiptideData.Foes)
        {
            Beingobject b = RiptideUtil.Load<Beingobject>("Mob", f.Id);
            if (b == null) { Log("foe " + f.Id + ": not loaded"); continue; }
            List<string> stats = new List<string>();
            foreach (string s in new[] { "Health", "Healtharmor", "Damage", "Attackspeed", "Speed", "Range" })
                stats.Add(s + " " + RiptideUtil.F(RiptideUtil.BaseStat(b, s, -1f)));
            List<string> drops = new List<string>();
            if (b.Entity != null && b.Entity.Drops != null)
                foreach (Link_Resourceset r in b.Entity.Drops) drops.Add(r.Name + " " + RiptideUtil.F(r.Amount));
            Log("foe " + f.Id + ": " + string.Join(", ", stats.ToArray()) + "; drops " + string.Join(", ", drops.ToArray()) + "; threat " + RiptideUtil.F(RiptideDirector.Threat(f, b)));
        }
        foreach (RiptideData.Elite e in RiptideData.Elites)
        {
            Upgradeproperties u = RiptideUtil.Load<Upgradeproperties>("Upgrade", e.Upgrade);
            List<string> ps = new List<string>();
            if (u != null && u.Defaultparams != null)
                foreach (KeyValuePair<string, float> p in u.Defaultparams) ps.Add(p.Key + "=" + RiptideUtil.F(p.Value));
            Log("elite " + e.Name + " (" + e.Upgrade + "): " + (u != null ? string.Join(", ", ps.ToArray()) : "not loaded"));
        }
    }

    // ---------------------------------------------------------------- helpers

    static IEnumerator Wait(float seconds) { yield return new WaitForSeconds(seconds); }

    static IEnumerator Until(Func<bool> condition, float timeout)
    {
        float t = 0f;
        while (!condition() && t < timeout)
        {
            t += Time.deltaTime;
            yield return null;
        }
    }

    static Entity_Master Hero()
    {
        long me = Blm_Server.Instance != null && Blm_Server.Instance.Localplayer != null ? Blm_Server.Instance.Localplayer.Sessionid.Value : long.MinValue;
        foreach (Entity_Master e in RiptideUtil.Tagged("Hero"))
            if (e.Entitydata.Value.Owner == me)
                return e;
        return null;
    }

    static void Armour(Entity_Master m)
    {
        Link_Stats s;
        if (m == null || m.Statsserver == null || !m.Statsserver.TryGetValue("Health", out s))
            return;
        Helper_Entitytools.Addmod(m, "Health", 5000f, Helper_Entitytools.Modtypes.Add, 0f);
        s.Value = s.Value_Calc;
    }

    // Removes Tide foes (not a kill: nothing is paid).
    static void ClearFoes()
    {
        foreach (Entity_Master e in new List<Entity_Master>(Game_Entity.Instance.Entityclient))
            if (RiptideUtil.Alive(e) && RiptideUtil.Team(e) == 0 && RiptideUtil.HasTag(e, "Enemy") && !RiptideUtil.HasTag(e, "Enemycore"))
                Game_Entity.Instance.Despawn(e.Beingserver, false, "Selftest");
    }

    // Lets the break run out until the next Tide starts, then holds the clock again.
    static IEnumerator NextTide()
    {
        int before = Game_Mapvars.Instance.Wave.Value;
        forcing = true;
        yield return Until(() => Game_Mapvars.Instance == null || Game_Mapvars.Instance.Wave.Value > before, 120f);
        forcing = false;
    }

    // Ends the current Tide (it ends when no foe is left) and waits for the break after it.
    static IEnumerator EndTide()
    {
        int entry = Game_Mapvars.Instance.Entry.Value;
        ClearFoes();
        yield return Until(() => Game_Mapvars.Instance.Entry.Value > entry, 30f);
        yield return Wait(0.5f);
    }

    // Foes spawned by name, and how many of them carried each upgrade (the Tide's elites).
    static readonly Dictionary<string, int> spawned = new Dictionary<string, int>();
    static readonly Dictionary<string, int> elited = new Dictionary<string, int>();
    static readonly List<Entity_Master> lastSpawns = new List<Entity_Master>();
    static bool counting;

    static void OnSpawn(Entity_Master m)
    {
        if (!counting || m == null || m.Beingserver == null || m.Beingserver.Being == null)
            return;
        string name = m.Beingserver.Being.Objectname;
        if (!name.StartsWith("Enemy_"))
            return;
        spawned[name] = (spawned.ContainsKey(name) ? spawned[name] : 0) + 1;
        if (m.Initupgradelistserver != null)
            foreach (string up in m.Initupgradelistserver.Keys)
                elited[up] = (elited.ContainsKey(up) ? elited[up] : 0) + 1;
        lastSpawns.Add(m);
    }

    static void StartCounting()
    {
        spawned.Clear();
        elited.Clear();
        lastSpawns.Clear();
        counting = true;
    }

    static int Total(Dictionary<string, int> d)
    {
        int n = 0;
        foreach (int v in d.Values) n += v;
        return n;
    }

    static string Show(Dictionary<string, int> d)
    {
        List<string> parts = new List<string>();
        foreach (KeyValuePair<string, int> kv in d) parts.Add(kv.Value + "x" + kv.Key);
        return string.Join(", ", parts.ToArray());
    }

    // What a list of contents should spawn: foes by name, and elite upgrades by name.
    static void Expect(List<Wavesobject.Wavecontent> contents, Dictionary<string, int> foes, Dictionary<string, int> ups)
    {
        foreach (Wavesobject.Wavecontent c in contents)
        {
            if (c.Enemy == null || c.Style == Wavesobject.Wavestyle.Wait)
                continue;
            foes[c.Enemy.Objectname] = (foes.ContainsKey(c.Enemy.Objectname) ? foes[c.Enemy.Objectname] : 0) + c.Count;
            foreach (Upgradeproperties u in c.Upgrades)
                if (u != null)
                    ups[u.Objectname] = (ups.ContainsKey(u.Objectname) ? ups[u.Objectname] : 0) + c.Count;
        }
    }

    static bool Same(Dictionary<string, int> want, Dictionary<string, int> got)
    {
        foreach (KeyValuePair<string, int> kv in want)
            if (!got.ContainsKey(kv.Key) || got[kv.Key] != kv.Value)
                return false;
        return true;
    }

    static float SpawnTime(List<Wavesobject.Wavecontent> contents)
    {
        float t = 0f;
        foreach (Wavesobject.Wavecontent c in contents)
            t += c.Style == Wavesobject.Wavestyle.Wait ? c.Interval : c.Count * Mathf.Max(c.Interval, 0.1f);
        return t;
    }

    static float Aura(long owner)
    {
        GameObject player;
        Tds_Clientinfo info;
        if (Blm_Server.Instance == null || !Blm_Server.Instance.Sessionids.TryGetValue(owner, out player) || player == null || !player.TryGetComponent(out info))
            return -1f;
        float sum = 0f;
        foreach (Link_Resource r in info.Resources)
            sum += r.Amount;
        return sum;
    }

    static string Note() { return Game_Mapvars.Instance.Display.Value.Display_Note.ToString(); }

    // ---------------------------------------------------------------- the scenario

    static IEnumerator Scenario()
    {
        Game_Mapvars mv = Game_Mapvars.Instance;
        Log("match started: scenario begins, " + RiptideContent.ArtifactsLoaded + " artifacts loaded in " + RiptideUtil.F(RiptideContent.ArtifactLoadSeconds) + " s");
        Game_Entity.Instance.Onspawn += OnSpawn;

        // ---- probe 1: the match runs Riptide
        Check("probe 1: the match loaded Mode_Riptide", mv.Mode == RiptideContent.Mode && mv.Modeentry == RiptideContent.Entry,
            (mv.Mode != null ? mv.Mode.Objectname : "-") + " / " + (mv.Modeentry != null ? mv.Modeentry.Objectname : "-"));
        Check("probe 1: on the stock Mode_Normal", Game_Rounds.Instance != null && Game_Rounds.Instance.GetComponent<Mode_Normal>() != null);
        Check("probe 1: the match uses the Gameplan twin", mv.Gameplan != null && mv.Gameplan.Objectname.StartsWith(RiptideData.GameplanPrefix) && mv.Gameplan.Mode == RiptideContent.Entry,
            mv.Gameplan != null ? mv.Gameplan.Objectname : "-");
        Check("probe 1: the director started a run and wrote Tide 1", RiptideDirector.Running && RiptideDirector.Written >= 1,
            "seed " + RiptideDirector.Seed + ", written " + RiptideDirector.Written);
        StartCounting();
        yield return Until(() => mv.Wave.Value >= 1, 180f);
        freeze = true;
        if (!Check("probe 1: Tide 1 starts", mv.Wave.Value >= 1, "Tide " + mv.Wave.Value + ", entry " + mv.Entry.Value))
        {
            Finish();
            yield break;
        }
        Entity_Master hero = Hero();
        Armour(hero);
        foreach (Entity_Master core in RiptideUtil.Tagged("Core"))
            Armour(core);
        yield return Wait(1f);
        string note1;
        RiptideDirector.Notes.TryGetValue(1, out note1);
        Check("probe 1: the banner shows Riptide's line", note1 != null && Note() == note1, "'" + Note() + "' (written '" + note1 + "')");
        List<Wavesobject.Wavecontent> plan1;
        RiptideDirector.Plans.TryGetValue(1, out plan1);
        DumpFoes();

        // ---- probe 2: live Tide rewrite
        Dictionary<string, int> want = new Dictionary<string, int>(), wantUps = new Dictionary<string, int>();
        if (plan1 != null) Expect(plan1, want, wantUps);
        yield return Until(() => Total(spawned) >= Total(want), SpawnTime(plan1 ?? new List<Wavesobject.Wavecontent>()) + 15f);
        Check("probe 2: Tide 1 spawns what the director wrote at the start of the run", Same(want, spawned), "wrote " + Show(want) + "; spawned " + Show(spawned));
        yield return EndTide();
        Check("probe 2: the director writes the next Tide when the break starts", RiptideDirector.Written >= 2 && RiptideDirector.Plans.ContainsKey(2),
            "entry " + mv.Entry.Value + ", written " + RiptideDirector.Written);
        List<Wavesobject.Wavecontent> plan2;
        RiptideDirector.Plans.TryGetValue(2, out plan2);
        want.Clear(); wantUps.Clear();
        if (plan2 != null) Expect(plan2, want, wantUps);
        StartCounting();
        yield return NextTide();
        yield return Until(() => Total(spawned) >= Total(want), SpawnTime(plan2 ?? new List<Wavesobject.Wavecontent>()) + 15f);
        Check("probe 2: Tide 2 spawns exactly the written foes", Same(want, spawned) && Total(spawned) == Total(want), "wrote " + Show(want) + "; spawned " + Show(spawned));
        string note2;
        RiptideDirector.Notes.TryGetValue(2, out note2);
        Check("probe 2: Tide 2 shows its own banner line", note2 != null && Note() == note2, "'" + Note() + "' (written '" + note2 + "')");
        yield return EndTide();

        // A hand-written Tide 3 with an elite, written after the director wrote its own (the break has started).
        yield return Until(() => RiptideDirector.Written >= 3, 10f);
        Beingobject dagger = RiptideUtil.Load<Beingobject>("Mob", RiptideData.Foes[0].Id);
        Upgradeproperties hardened = RiptideUtil.Load<Upgradeproperties>("Upgrade", RiptideData.Elites[0].Upgrade);
        if (Check("probe 2: a Dagger and the Hardened upgrade are loaded", dagger != null && hardened != null))
        {
            Wavesobject.Waves t3 = RiptideContent.Table.Wave[5];
            Wavesobject.Wavecontent c = new Wavesobject.Wavecontent();
            c.Style = Wavesobject.Wavestyle.Spread;
            c.Interval = 0.5f;
            c.Count = 3;
            c.Enemy = dagger;
            c.Upgrades = new List<Upgradeproperties> { hardened };
            c.Resources = new List<Link_Resourceset>();
            t3.Content = new List<Wavesobject.Wavecontent> { c };
            t3.Note = "Probe Tide";
            RiptideContent.Table.Wave[5] = t3;
            StartCounting();
            yield return NextTide();
            yield return Until(() => Total(spawned) >= 3, 15f);
            yield return Wait(0.5f);
            int skinned = 0;
            foreach (Entity_Master e in lastSpawns)
                if (e != null && e.Skin.Value.ToString() == "Health") skinned++;
            Check("probe 2: a rewritten Tide spawns its elites", spawned.ContainsKey(dagger.Objectname) && spawned[dagger.Objectname] == 3
                && elited.ContainsKey(hardened.Objectname) && elited[hardened.Objectname] == 3, "spawned " + Show(spawned) + "; upgrades " + Show(elited));
            Check("probe 2: the elites wear the Hardened skin", skinned == 3, skinned + "/3");
            foreach (Entity_Master e in lastSpawns)
                if (e != null && e.Statsserver != null && e.Statsserver.ContainsKey("Health"))
                {
                    // How the game applies an upgrade's Health_Multi (added to 1, or the factor itself) sets the elites' cost.
                    Log("a Hardened Dagger's live Health: " + RiptideUtil.F(e.Statsserver["Health"].Value_Calc) + " (base "
                        + RiptideUtil.F(RiptideUtil.BaseStat(dagger, "Health", -1f)) + ", multi " + RiptideUtil.F(e.Statsserver["Health"].Value_Multi) + ")");
                    break;
                }
            Check("probe 2: the banner shows the rewritten line", Note() == "Probe Tide", "'" + Note() + "'");
            yield return EndTide();
        }
        counting = false;

        // ---- probe 3: artifacts mid-match (in the break before Tide 4)
        yield return Probe3(hero);

        // ---- probe 4: the game's choice dialog
        yield return Probe4();

        // ---- the Salvage, wired (v0.2)
        yield return Salvage(hero);

        // ---- probe 5: the sandbox
#if RIPTIDE_SANDBOX
        yield return RiptideSandbox.Run();
#else
        Log("SKIP probe 5: build with -Define RIPTIDE_SELFTEST,RIPTIDE_SANDBOX");
#endif

        // ---- probe 1, last: a fallen core ends the run
        yield return Probe1Ending();
        Finish();
    }

    static void Finish()
    {
        freeze = false;
        forcing = false;
        counting = false;
        if (Game_Entity.Instance != null)
            Game_Entity.Instance.Onspawn -= OnSpawn;
        Application.logMessageReceived -= OnLog;
        Log("errors and exceptions logged by the game during the run: " + errors + (errorLines.Count > 0 ? " (first: " + string.Join(" | ", errorLines.ToArray()) + ")" : "")
            + "; the game's own cancelled tasks: " + cancels);
        Log("DONE pass=" + pass + " fail=" + fail);
        phase = 9;
    }

    // ---------------------------------------------------------------- probe 3

    static readonly string[] Families =
    {
        "Entity_Ai_Artifact_Coin", "Entity_Ai_Artifact_Coreapply", RiptideData.KindTower, RiptideData.KindHero, RiptideData.KindTeam, RiptideData.KindMob,
    };

    static IEnumerator Probe3(Entity_Master hero)
    {
        Game_Mapvars mv = Game_Mapvars.Instance;
        int mark = errors;
        Dictionary<string, string> kinds = new Dictionary<string, string>();
        foreach (string id in RiptideContent.Artifacts)
        {
            Artifactobject a = RiptideArtifacts.Object(id);
            Artifactentryobject e = RiptideArtifacts.Entry(id);
            kinds[id] = RiptideArtifacts.Kind(a);
            Log("artifact " + id + " '" + (e != null ? e.Display_Name : "-") + "': slot " + (e != null ? e.Type : "-") + ", leaderboard x" + (e != null ? RiptideUtil.F(e.Leaderboard_Multiplier) : "-")
                + ", stattrack " + (a != null && a.Stattrack) + ", kind " + kinds[id]);
        }
        Check("probe 3: every artifact group loaded on this machine", RiptideContent.ArtifactsLoaded > 0 && RiptideContent.ArtifactsLoaded == RiptideContent.Artifacts.Count,
            RiptideContent.ArtifactsLoaded + " loaded");

        // A standing tower and the vessel, for the catch-up checks.
        Entity_Master tower = null;
        if (hero != null)
        {
            Helper_Resources.Giveresources(new List<Link_Resourceset> { Res("Redaura", 5000f), Res("Blueaura", 5000f), Res("Yellowaura", 5000f) },
                hero.Entitydata.Value.Owner, Helper_Resources.Styles.Default);
            yield return Wait(0.3f);
            Beingobject being = RiptideUtil.Load<Beingobject>("Mob", TestTower);
            Vector3 at = being != null ? Spot(hero, being, 2.5f) : new Vector3(float.NaN, 0f, 0f);
            if (!float.IsNaN(at.x))
            {
                string json = "{\"Tower\":\"" + TestTower + "\",\"Position\":{\"x\":" + RiptideUtil.F(at.x) + ",\"y\":" + RiptideUtil.F(at.y) + ",\"z\":" + RiptideUtil.F(at.z)
                    + "},\"Orientation\":0,\"Owner\":" + hero.Entitydata.Value.Owner + "}";
                hero.GetComponent<Entity_Master_Remote>().Command_ServerRpc("Build", json, default(ServerRpcParams));
                float t = 0f;
                while (tower == null && t < 15f)
                {
                    foreach (Entity_Master e in RiptideUtil.Tagged("Tower"))
                        if (RiptideUtil.BeingName(e) == TestTower && Vector3.Distance(e.Entityposition.Value, at) < 0.75f) tower = e;
                    t += Time.deltaTime;
                    yield return null;
                }
            }
            Check("probe 3: a test tower stands for the catch-up checks", tower != null, float.IsNaN(at.x) ? "no free tile" : "at " + at);
        }

        // One artifact of each kind, in family order, then any others up to five.
        List<string> pick = new List<string>();
        foreach (string family in Families)
            foreach (string id in RiptideContent.Artifacts)
                if (kinds[id].Contains(family) && !pick.Contains(id) && System.Array.IndexOf(RiptideData.NeverOffer, id) < 0 && !RiptideArtifacts.Held(id))
                {
                    pick.Add(id);
                    break;
                }
        foreach (string id in RiptideContent.Artifacts)
            if (pick.Count < 5 && !pick.Contains(id) && System.Array.IndexOf(RiptideData.NeverOffer, id) < 0 && !RiptideArtifacts.Held(id))
                pick.Add(id);

        List<string> lobbyBefore = new List<string>();
        foreach (FixedString64Bytes a in Tds_Multiplayerinfo.Instance.Artifacts) lobbyBefore.Add(a.ToString());
        long owner = hero != null ? hero.Entitydata.Value.Owner : 0;
        bool coin = false;
        foreach (string id in pick)
        {
            Artifactobject art = RiptideArtifacts.Object(id);
            int before = errors;
            string why = RiptideArtifacts.Grant(id);
            yield return Wait(0.5f);
            bool cores = art != null;
            foreach (Entity_Master core in RiptideUtil.Tagged("Core"))
                foreach (Upgradeproperties up in art.Coreupgrades)
                    if (up != null && !core.Upgradelistserver.ContainsKey(up.Objectname)) cores = false;
            Check("probe 3: " + id + " is granted mid-match (" + kinds[id] + ")", why == "" && cores && ErrorsSince(before) == 0,
                (why == "" ? "ok" : why) + ", core upgrades " + (cores ? "on every core" : "missing") + ", " + ErrorsSince(before) + " errors");
            string k = kinds[id];
            if (k.Contains("Entity_Ai_Artifact_Coin")) coin = true;
            if (k.Contains(RiptideData.KindTower) && tower != null)
                Check("probe 3: catch-up gives " + id + "'s upgrades to a standing tower", Has(tower, art), Upgrades(tower));
            if (k.Contains(RiptideData.KindHero) && hero != null)
                Check("probe 3: catch-up gives " + id + "'s upgrades to the living vessel", Has(hero, art), Upgrades(hero));
        }
        yield return Wait(3f);
        int shown = 0, rowChildren = -1;
        foreach (string id in pick)
            if (mv.Artifactobjects.Contains(RiptideArtifacts.Object(id))) shown++;
        Check("probe 3: the salvaged artifacts joined this machine's artifact list", shown == pick.Count, shown + "/" + pick.Count);
        Blu2_Menu_Handler main;
        GameObject row;
        if (Bl_Menus.Menus.TryGetValue("Main", out main) && main.Items.TryGetValue("Artifacts", out row) && row != null)
            rowChildren = row.transform.childCount;
        Check("probe 3: the HUD artifact row shows them", rowChildren == mv.Artifactobjects.Count, rowChildren + " icons for " + mv.Artifactobjects.Count + " artifacts");
#if RIPTIDE_SHOTS
        yield return RiptideShots.Shot("artifact_row");
#endif
        float auraBefore = Aura(owner);
        yield return NextTide();
        yield return Wait(2f);
        float auraAfter = Aura(owner);
        Log("aura " + RiptideUtil.F(auraBefore) + " -> " + RiptideUtil.F(auraAfter) + " as Tide " + mv.Wave.Value + " began");
        if (coin)
            Check("probe 3: a coin artifact pays when the next Tide begins", auraAfter > auraBefore, RiptideUtil.F(auraBefore) + " -> " + RiptideUtil.F(auraAfter));
        Check("probe 3: no errors while granting", ErrorsSince(mark) == 0, ErrorsSince(mark) + " errors");
        yield return EndTide();

        // The lobby list goes back as it was (the director does this on the world map; checked here directly).
        List<string> salvaged = new List<string>(RiptideArtifacts.Salvaged);
        RiptideArtifacts.RestoreLobby();
        List<string> lobbyAfter = new List<string>();
        foreach (FixedString64Bytes a in Tds_Multiplayerinfo.Instance.Artifacts) lobbyAfter.Add(a.ToString());
        Check("probe 3: the lobby list goes back as it was", lobbyAfter.Count == lobbyBefore.Count && lobbyAfter.TrueForAll(a => lobbyBefore.Contains(a)),
            "before [" + string.Join(", ", lobbyBefore.ToArray()) + "] after [" + string.Join(", ", lobbyAfter.ToArray()) + "], took out " + salvaged.Count);
    }

    static Link_Resourceset Res(string name, float amount)
    {
        Link_Resourceset r = new Link_Resourceset();
        r.Name = name;
        r.Amount = amount;
        return r;
    }

    static bool Has(Entity_Master e, Artifactobject art)
    {
        int wanted = 0, got = 0;
        foreach (Upgradeproperties up in art.Coreupgrades)
            if (up != null && up.Defaultparams != null)
                foreach (string key in up.Defaultparams.Keys)
                    if (RiptideUtil.Load<Upgradeproperties>("Upgrade", key) != null)
                    {
                        wanted++;
                        if (e.Upgradelistserver.ContainsKey(key)) got++;
                    }
        return wanted > 0 && got > 0;
    }

    static string Upgrades(Entity_Master e)
    {
        return e != null && e.Upgradelistserver != null ? string.Join(", ", new List<string>(e.Upgradelistserver.Keys).ToArray()) : "-";
    }

    // A free tile about 'ring' tiles from the vessel where the being can be placed (from the vessel self-tests).
    static Vector3 Spot(Entity_Master hero, Beingobject being, float ring)
    {
        Vector3 c = hero.Entityposition.Value;
        List<Vector3> options = new List<Vector3>();
        for (int dx = -5; dx <= 5; dx++)
            for (int dz = -5; dz <= 5; dz++)
                options.Add(new Vector3(c.x + dx, c.y, c.z + dz));
        options.Sort((a, b) => Mathf.Abs(Vector3.Distance(a, c) - ring).CompareTo(Mathf.Abs(Vector3.Distance(b, c) - ring)));
        Link_Entityquery surface = new Link_Entityquery();
        surface.Tags = new List<string> { "Surface" };
        surface.Style = Link_Entityquery.Styles.Tag;
        foreach (Vector3 p in options)
        {
            Entity_Master s = Helper_Entityquerytools.Entitynearest(p, 0.75f, surface);
            Genitem block = s != null && s.Beingserver != null ? s.Beingserver.Currentblock : null;
            if (block != null && Helper_Gridtools.Checkifplaceable(block, being, 0, RiptideUtil.Team(hero)))
                return block.Position;
        }
        return new Vector3(float.NaN, 0f, 0f);
    }

    // ---------------------------------------------------------------- probe 4

    static IEnumerator Probe4()
    {
        Bla_Ui_Confirmation dialog = Bla_Ui_Confirmation.Instance;
        if (!Check("probe 4: the game's choice dialog exists in a match", dialog != null && dialog.List != null && dialog.Confirmbutton != null,
            dialog != null ? dialog.gameObject.name : "no Bla_Ui_Confirmation.Instance"))
            yield break;
        string menuName = dialog.gameObject.name;
        Blu2_Menu_Handler menu;
        bool registered = Bl_Menus.Menus.TryGetValue(menuName, out menu);
        Log("dialog '" + menuName + "', a menu of that name " + (registered ? "exists (layer " + menu.Layer + ")" : "does not exist"));
        List<string> offers = RiptideSalvage.Offers(new System.Random(RiptideDirector.Seed), RiptideData.Offers, null);
        Check("probe 4: Salvage offers three artifacts the team doesn't hold", offers.Count == RiptideData.Offers && offers.TrueForAll(id => !RiptideArtifacts.Held(id)),
            string.Join(", ", offers.ToArray()));
        if (offers.Count < 2)
            yield break;
        List<Bla_Ui_Confirmation.Entry> cards = new List<Bla_Ui_Confirmation.Entry>();
        foreach (string id in offers) cards.Add(RiptideSalvage.Card(id));
        float scaleBefore = Time.timeScale;
        int mark = errors;
        Cysharp.Threading.Tasks.UniTask<Bla_Ui_Confirmation.Entry> task = dialog.Runprompt(cards, RiptideData.SalvageTitle, RiptideData.SalvageSub, true);
        yield return new WaitForSecondsRealtime(1.5f);
        // The list keeps its layout-ignoring children (Bl_Obj.Destroychildrenignorelayout); each card is named after its entry.
        int shownCards = 0;
        foreach (Transform child in dialog.List.transform)
            if (child.gameObject.activeInHierarchy && offers.Contains(child.name)) shownCards++;
        bool open = registered && menu.Active;
        Check("probe 4: the dialog opens with one card per offer", shownCards == cards.Count && open,
            shownCards + " cards (" + dialog.List.transform.childCount + " children), menu active " + open);
        Log("dialog layout: " + RiptideSalvage.Layout(dialog));
        string placedBefore = RiptideSalvage.Placement(dialog);
        Check("probe 4: the dialog sits mid-screen, its cards in one row", RiptideSalvage.Fit(dialog), RiptideSalvage.Layout(dialog));
        Check("probe 4: the dialog waits for a pick", task.Status == Cysharp.Threading.Tasks.UniTaskStatus.Pending, task.Status.ToString());
        Log("while the dialog is open: time scale " + RiptideUtil.F(scaleBefore) + " -> " + RiptideUtil.F(Time.timeScale) + ", synced time scale "
            + RiptideUtil.F(Game_Mapvars.Instance.Time_Scale.Value) + ", cursor visible " + Cursor.visible);
#if RIPTIDE_SHOTS
        yield return RiptideShots.Shot("salvage_dialog");
#endif
        Transform secondCard = dialog.List.transform.Find(offers[1]);
        Blu3_Button second = secondCard != null ? secondCard.GetComponent<Blu3_Button>() : null;
        if (second != null && second.Buttonclick != null)
            second.Buttonclick();
        float t = 0f;
        while (task.Status == Cysharp.Threading.Tasks.UniTaskStatus.Pending && t < 5f)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        Bla_Ui_Confirmation.Entry chosen = task.Status == Cysharp.Threading.Tasks.UniTaskStatus.Succeeded ? task.GetAwaiter().GetResult() : null;
        Check("probe 4: clicking the second card returns it", chosen != null && chosen.Name == offers[1], chosen != null ? chosen.Name : task.Status.ToString());
        yield return new WaitForSecondsRealtime(1f);
        Check("probe 4: the dialog closes after the pick", !registered || !menu.Active, registered ? "menu active " + menu.Active : "no menu");
        RiptideSalvage.Unfit(dialog);
        string placedAfter = RiptideSalvage.Placement(dialog);
        Check("probe 4: the dialog goes back to the game's own place", placedAfter == placedBefore, placedBefore + " -> " + placedAfter);
        Check("probe 4: no errors from the dialog", ErrorsSince(mark) == 0, ErrorsSince(mark) + " errors");
    }

    // ---------------------------------------------------------------- the Salvage, end to end

    static IEnumerator Salvage(Entity_Master hero)
    {
        Game_Mapvars mv = Game_Mapvars.Instance;
        long me = Blm_Server.Instance.Localplayer.Sessionid.Value;
        int mark = errors;
        float seconds = RiptideData.SalvageSeconds;
        freeze = false;
        mv.Time_Wave.Value = 20f;
        yield return null;
        RiptideSalvage.Enabled = true;
        RiptideData.SalvageSeconds = 20f;
        int tide = RiptideData.SalvageAfter[1];
        RiptideSalvage.Begin(tide);
        List<string> offers = null;
        if (!Check("salvage: the host rolls three offers for the player", RiptideSalvage.Open == tide && RiptideSalvage.Pending.TryGetValue(me, out offers)
            && offers.Count == RiptideData.Offers, "open " + RiptideSalvage.Open + ", " + RiptideSalvage.Pending.Count + " player(s)"))
        {
            RiptideSalvage.Close();
            yield return SalvageDone(seconds);
            yield break;
        }
        string code = RiptideSalvage.Encode(offers);
        Check("salvage: the offers survive the trip through the command", string.Join(",", RiptideSalvage.Decode(code).ToArray()) == string.Join(",", offers.ToArray()),
            code + " = " + string.Join(", ", offers.ToArray()));
        float low = mv.Time_Wave.Value;
        Link_Commandref mine = default(Link_Commandref);
        Entity_Master shown = null;
        float t = 0f;
        while (t < 5f && (shown == null || mine.Goalstring.ToString() != code || !RiptideSalvage.Asking))
        {
            t += Time.deltaTime;
            low = Mathf.Min(low, mv.Time_Wave.Value);
            shown = RiptideSalvage.Mine(out mine);
            yield return null;
        }
        Link_Command server = null;
        if (hero != null && hero.Commandlistserver != null)
            hero.Commandlistserver.TryGetValue(RiptideData.SalvageLink, out server);
        Check("salvage: the SALVAGE command on the vessel carries the offers", server != null && server.Goalstring == code && (int)server.Goalvalue == tide,
            server != null ? "'" + server.Goalstring + "', " + RiptideUtil.F(server.Goalvalue) : "no command");
        Check("salvage: the player's machine sees them", shown == hero && mine.Goalstring.ToString() == code, shown != null ? "'" + mine.Goalstring + "'" : "no vessel with the command");
        Check("salvage: the dialog opens by itself", RiptideSalvage.Asking);
        yield return new WaitForSecondsRealtime(1f);
        Bla_Ui_Confirmation dialog = Bla_Ui_Confirmation.Instance;
        int cards = 0;
        if (dialog != null && dialog.List != null)
            foreach (Transform child in dialog.List.transform)
                if (child.gameObject.activeInHierarchy && offers.Contains(child.name)) cards++;
        Check("salvage: one card per offer, in a row mid-screen", cards == offers.Count && dialog != null && RiptideSalvage.Centred(dialog), cards + " cards, " + (dialog != null ? RiptideSalvage.Layout(dialog) : "no dialog"));
#if RIPTIDE_SHOTS
        yield return RiptideShots.Shot("salvage_open");
#endif
        // Another menu takes the screen while offers are open: the dialog goes, the SALVAGE button on the vessel's bar
        // stays lit and brings it back.
        if (dialog != null)
            Bl_Menus.Closemenu(dialog.gameObject, true);
        yield return new WaitForSecondsRealtime(1f);
        Game_Selection.Commandcont bar = Bar(hero);
        bool keyed = RiptideCommands.Salvage != null && RiptideCommands.Salvage.Keybind.Count > 0;
        Check("salvage: the SALVAGE button is on the vessel's bar", bar != null, Selection(hero) + ", key " + (keyed ? RiptideData.SalvageKey : "none") + ", digit keys in the game ["
            + string.Join(", ", Digits().ToArray()) + "]");
        if (keyed)
            Check("salvage: key " + RiptideData.SalvageKey + " presses it", Game_Selection.Instance.Currentkeybinds.Contains(RiptideData.SalvageKey),
                "bound [" + string.Join(", ", Game_Selection.Instance.Currentkeybinds.ToArray()) + "]");
        else
            Log("SKIP salvage: key " + RiptideData.SalvageKey + " (the game's input map has no such key; SALVAGE is click-only)");
        Log("HUD while the dialog is closed: " + Hud());
#if RIPTIDE_SHOTS
        yield return new WaitForSecondsRealtime(1f);     // the bar slides in after a selection (round 3 shot it too early)
        yield return RiptideShots.Shot("salvage_button");
#endif
        if (bar != null)
            foreach (Entity_Command_Base f in bar.Functs)
                f.Activate(false);
        yield return new WaitForSecondsRealtime(1f);
        Check("salvage: the SALVAGE button reopens the dialog", RiptideSalvage.Asking && RiptideSalvage.DialogOpen(),
            "asking " + RiptideSalvage.Asking + ", menu " + (RiptideSalvage.DialogOpen() ? "open" : "closed"));
        string pick = offers[1];
        Transform card = dialog != null && dialog.List != null ? dialog.List.transform.Find(pick) : null;
        Blu3_Button button = card != null ? card.GetComponent<Blu3_Button>() : null;
        if (button != null && button.Buttonclick != null)
            button.Buttonclick();
        t = 0f;
        while (RiptideSalvage.Open != 0 && t < 5f)
        {
            t += Time.deltaTime;
            low = Mathf.Min(low, mv.Time_Wave.Value);
            yield return null;
        }
        string got;
        RiptideSalvage.Picks.TryGetValue(me, out got);
        Check("salvage: a click reaches the host and closes the Salvage", RiptideSalvage.Open == 0 && got == pick, "picked '" + got + "', wanted '" + pick + "'");
        Check("salvage: the pick is the team's", pick == RiptideData.Driftwood || RiptideArtifacts.Held(pick), pick);
        Check("salvage: the break's clock holds while the Salvage is open", low >= 19f, "lowest " + RiptideUtil.F(low) + " of 20");
        yield return new WaitForSecondsRealtime(1f);
        server = null;
        if (hero != null && hero.Commandlistserver != null)
            hero.Commandlistserver.TryGetValue(RiptideData.SalvageLink, out server);
        Check("salvage: the SALVAGE button greys after the pick", server != null && server.Goalstring == "" && !RiptideSalvage.Asking,
            server != null ? "'" + server.Goalstring + "', dialog " + (RiptideSalvage.Asking ? "open" : "closed") : "no command");

        // A Salvage left to time out, with only Driftwood on offer: the host picks it, the aura arrives, the dialog closes.
        RiptideData.SalvageSeconds = 4f;
        float aura = Aura(me);
        RiptideSalvage.Begin(tide);
        RiptideSalvage.Pending[me] = new List<string> { RiptideData.Driftwood };
        yield return Until(() => RiptideSalvage.Asking, 3f);
        Check("salvage: a Driftwood-only offer opens too", RiptideSalvage.Asking);
        yield return Until(() => RiptideSalvage.Open == 0, 8f);
        RiptideSalvage.Picks.TryGetValue(me, out got);
        Check("salvage: time runs out and the host picks", RiptideSalvage.Open == 0 && got == RiptideData.Driftwood + " (auto)", "'" + got + "'");
        yield return new WaitForSecondsRealtime(1f);
        float gain = Aura(me) - aura;
        Check("salvage: Driftwood pays the picker", gain > 0f, "+" + RiptideUtil.F(gain) + " aura (" + RiptideUtil.F(3f * RiptideData.DriftwoodAura(tide)) + " before the game's gain multiplier)");
        Check("salvage: the dialog closes when the host picks", !RiptideSalvage.Asking && !RiptideSalvage.DialogOpen(),
            "asking " + RiptideSalvage.Asking + ", menu " + (RiptideSalvage.DialogOpen() ? "open" : "closed"));
        Check("salvage: no errors", ErrorsSince(mark) == 0, ErrorsSince(mark) + " errors");
        yield return SalvageDone(seconds);
    }

    // The vessel selected the way a click selects it, and its SALVAGE entry on the bar.
    static Game_Selection.Commandcont Bar(Entity_Master hero)
    {
        Game_Selection select = Game_Selection.Instance;
        if (select == null || hero == null)
            return null;
        try
        {
            if (select.Selected != hero)
            {
                select.Selected = hero;
                select.Select();
            }
        }
        catch (Exception e) { Log("selecting the vessel threw: " + e.Message); }
        Game_Selection.Commandcont bar;
        return select.Currentcommands.TryGetValue(RiptideData.SalvageLink, out bar) ? bar : null;
    }

    static string Selection(Entity_Master hero)
    {
        Game_Selection select = Game_Selection.Instance;
        if (select == null)
            return "no Game_Selection";
        return (select.Selected == hero ? "vessel selected" : "vessel not selected") + ", bar [" + string.Join(", ", new List<string>(select.Currentcommands.Keys).ToArray()) + "]";
    }

    static List<string> Digits()
    {
        List<string> keys = new List<string>();
        if (Blockage.Bl_Inputs.Actions != null)
            foreach (string k in Blockage.Bl_Inputs.Actions.Keys)
                if (k.Length == 1 && char.IsDigit(k[0])) keys.Add(k);
        keys.Sort();
        return keys;
    }

    // For the shots: is the HUD the menu on screen?
    static string Hud()
    {
        Blu2_Menu_Handler main, top = null;
        try { top = Bl_Menus.Getactivemenu(); } catch (Exception) { }
        string s = "top menu '" + (top != null ? top.gameObject.name : "-") + "'";
        if (!Bl_Menus.Menus.TryGetValue("Main", out main) || main == null)
            return s + ", no 'Main' menu";
        Canvas canvas = main.GetComponent<Canvas>();
        return s + ", 'Main' active " + main.Active + ", canvas " + (canvas != null ? (canvas.enabled ? "on" : "off") + " " + canvas.renderMode : "none");
    }

    static IEnumerator SalvageDone(float seconds)
    {
        RiptideSalvage.Enabled = false;
        RiptideData.SalvageSeconds = seconds;
        freeze = true;
        yield return null;
    }

    // ---------------------------------------------------------------- probe 1, the ending

    static IEnumerator Probe1Ending()
    {
        Game_Mapvars mv = Game_Mapvars.Instance;
        List<Entity_Master> cores = RiptideUtil.Tagged("Core");
        if (!Check("probe 1: a Basin core to drown", cores.Count > 0, cores.Count + " cores"))
            yield break;
        freeze = false;
        int tide = mv.Wave.Value;
        Game_Entity.Instance.Despawn(cores[0].Beingserver, true, "Selftest");
        yield return Until(() => mv.Gamecomplete.Value.ToString() == "Lose", 15f);
        Check("probe 1: a fallen core ends the run", mv.Gamecomplete.Value.ToString() == "Lose", "Gamecomplete '" + mv.Gamecomplete.Value + "', endgame '" + mv.Display.Value.Display_Endgame + "'");
        yield return Wait(1f);
        Check("probe 1: the director records the end of the run", RiptideDirector.Over && RiptideDirector.Outcome.StartsWith("the Basin drowned"), RiptideDirector.Outcome + " (Tide " + tide + ")");
    }
}
#endif
