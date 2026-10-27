#if YOSSELE_PLAY
// Debug build only (Build-YosseleMod.ps1 -Define YOSSELE_PLAY,YOSSELE_SHOTS,MODKIT_PLAY,MODKIT_TEST): Yossele's play
// test (2026-10-26). A virtual player (kit ModKitPlay) starts each map in Plans, then plays it through the HUD with a
// virtual mouse and keyboard the way a person would: F to find the vessel, the build bar, a tile (R to turn a
// Stonehenge toward the lane); a click on a building and on its upgrade entries; SELL and a click on what to sell; the
// fast-forward button. It logs "[Yossele] PLAY ..." lines (each click, what the game did, one status line a Tide with
// every building's damage and kills) and in-game captures. A step the HUD can't do is done with the command the button
// would send and logged "FALLBACK", so the map still gets played and the log shows which clicks failed.
// A "money" map gives the player aura each Tide (the Sandbox Cardinal's money button does the same) so every upgrade
// route gets bought and seen in real Tides; an honest map is played on the map's own income, for balance.
public static class YosselePlay
{
    class Plan { public string Name; public bool Money; public int Tides; public float Minutes; }
    // Round P3 played 3_Opal on its own income to Tide 8 (the balance read); these two follow it.
    static readonly Plan[] Plans =
    {
        new Plan { Name = "3B_Thehunt", Money = false, Tides = 7, Minutes = 15f },
        new Plan { Name = "3B_Woozy", Money = true, Tides = 7, Minutes = 16f },
    };
    const float MoneyPerTide = 2500f;

    // On a money map, the Sandbox Cardinal's other button: foes of a kind spawned at the spawner at a Tide's start, so
    // D-MDS meets every shooter and the Stonehenge a Maul (the regular nine, docs/13 section 1). A Bulwark comes last.
    static readonly Dictionary<int, string[]> SandboxFoes = new Dictionary<int, string[]>
    {
        { 3, new[] { "Enemy_Ballista", "Enemy_Ballista", "Enemy_Ballista" } },
        { 4, new[] { "Enemy_Onager", "Enemy_Onager", "Enemy_Ballista" } },
        { 5, new[] { "Enemy_Maul", "Enemy_Maul", "Enemy_Onager" } },
        { 6, new[] { "Enemy_Excalibur", "Enemy_Claymore" } },
        { 7, new[] { "Enemy_Ballista", "Enemy_Excalibur", "Enemy_Onager", "Enemy_Maul" } },
    };

    // Upgrade routes: the n-th building of a kind takes route n (mod the count), so both branches of every level get
    // bought on a money map.
    static readonly Dictionary<string, string[][]> Routes = new Dictionary<string, string[][]>
    {
        { YosseleData.Cannon, new[] { new[] { "Sticky", "Viral", "Finish", "Cordyceps" }, new[] { "Farout", "Deathcap", "Hallucinogen", "Cordyceps" } } },
        { YosseleData.Dandelion, new[] { new[] { "Demand", "Dmds", "Bonsai", "Storm" }, new[] { "Packets", "Weeds", "Rubbery", "Storm" } } },
        { YosseleData.Henge, new[] { new[] { "Verdant", "Remedy", "Roots", "Monument" }, new[] { "Blockade", "Carnivore", "Obsidian", "Monument" }, new[] { "Verdant", "Easy", "Obsidian" } } },
        { YosseleData.Pachira, new[] { new[] { "Focused", "Grafted", "Connected", "Fortifying" }, new[] { "Sustaining", "Forest", "Harvest", "Softening" }, new[] { "Focused", "Mulch", "Harvest" } } },
    };

    static bool setup;
    static void Log(string m) { YosseleLog.Info("PLAY " + m); }

    public static void Update()
    {
        if (!setup)
        {
            setup = true;
            ModKitPlay.Log = Log;
#if YOSSELE_SHOTS
            ModKitPlay.Shot = YosseleShots.Shot;
#endif
            ModKitPlay.Host = YosseleMod.Instance;
            ModKitPlay.Hero = YosseleData.Hero;
            List<string> names = new List<string>();
            foreach (Plan p in Plans) names.Add(p.Name);
            ModKitPlay.Plans = names.ToArray();
            ModKitPlay.Scenario = Scenario;
            Log("play build: maps " + string.Join(", ", ModKitPlay.Plans));
        }
        ModKitPlay.Update();
    }

    // ---------------------------------------------------------------- state of the match

    static Plan plan;
    static Entity_Master hm;
    static long owner;
    static int team;
    static bool probed;
    static int fallbacks, uiBuilds, uiUpgrades, uiSells, uiSelects, keySelects, felled;
    static float matchStart;
    static readonly Dictionary<string, int> trail = new Dictionary<string, int>();   // tiles foes walked, by count
    static readonly List<Vector3> used = new List<Vector3>();
    static readonly Dictionary<YosseleBuilding, int> route = new Dictionary<YosseleBuilding, int>();
    static readonly Dictionary<string, int> built = new Dictionary<string, int>();
    static readonly Dictionary<string, int> foeKinds = new Dictionary<string, int>();
    static readonly List<Entity_Master> foesSeen = new List<Entity_Master>();

    static IEnumerator Wait(float s) { yield return new WaitForSeconds(s); }

    static IEnumerator Until(Func<bool> c, float timeout)
    {
        float t = 0f;
        while (!c() && t < timeout)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    static string F(float v) { return v.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture); }
    static string Vec(Vector3 v) { return "{\"x\":" + F(v.x) + ",\"y\":" + F(v.y) + ",\"z\":" + F(v.z) + "}"; }
    static string T(Vector3 v) { return Mathf.RoundToInt(v.x) + "," + Mathf.RoundToInt(v.y) + "," + Mathf.RoundToInt(v.z); }

    static List<YosseleBuilding> Mine()
    {
        List<YosseleBuilding> l = new List<YosseleBuilding>();
        foreach (YosseleBuilding b in YosseleServer.Buildings)
            if (b != null && b.Started && b.Master != null && YosseleUtil.Alive(b.Master) && b.Owner == owner)
                l.Add(b);
        return l;
    }

    static int Count(string id)
    {
        int n = 0;
        foreach (YosseleBuilding b in Mine())
            if (b.Def.Id == id) n++;
        return n;
    }

    static List<Entity_Master> Foes()
    {
        List<Entity_Master> l = new List<Entity_Master>();
        foreach (Entity_Master e in Game_Entity.Instance.Entityclient)
            if (YosseleUtil.IsFoe(e, team) && YosseleUtil.HasTag(e, "Enemy"))
                l.Add(e);
        return l;
    }

    static List<Entity_Master> Cores(string tag, bool ownTeam)
    {
        List<Entity_Master> l = new List<Entity_Master>();
        foreach (Entity_Master e in Game_Entity.Instance.Entityclient)
            if (e != null && YosseleUtil.HasTag(e, tag) && e.Beingserver != null && !e.Beingserver.Dead && (YosseleUtil.Team(e) == team) == ownTeam)
                l.Add(e);
        return l;
    }

    static string Short(string id) { return id.Replace("Tower_Yossele", ""); }

    static string Status()
    {
        string bs = "";
        foreach (YosseleBuilding b in Mine())
            bs += " " + Short(b.Def.Id) + "(G" + b.Growth + (b.Keys.Count > 0 ? " " + string.Join("+", b.Keys.ToArray()) : "")
                + " hp " + F(YosseleUtil.Health(b.Master)) + "/" + F(YosseleUtil.MaxHealth(b.Master)) + " dmg " + F(b.Master.Damagedone) + " kills " + b.Master.Killed + ")";
        string cores = "";
        foreach (Entity_Master c in Cores("Core", true))
            cores += " " + YosseleUtil.BeingName(c).Replace("Main_", "") + " " + F(YosseleUtil.Health(c)) + "/" + F(YosseleUtil.MaxHealth(c));
        string kinds = "";
        foreach (KeyValuePair<string, int> kv in foeKinds) kinds += " " + kv.Key.Replace("Enemy_", "") + "x" + kv.Value;
        return "tide " + Game_Mapvars.Instance.Wave.Value + " timer " + F(Game_Mapvars.Instance.Time_Wave.Value) + " speed " + F(Game_Mapvars.Instance.Time_Scale.Value) + (Game_Mapvars.Instance.Time_Speedup.Value ? "ff" : "")
            + " | " + ModKitPlay.Wallet() + " | vessel " + (YosseleUtil.Alive(hm) ? F(YosseleUtil.Health(hm)) + "/" + F(YosseleUtil.MaxHealth(hm)) + " dmg " + F(hm.Damagedone) + " kills " + hm.Killed : "down")
            + " | cores" + cores + " | foes now " + Foes().Count + ", felled " + YosseleServer.FoesFelled + ", seen" + kinds
            + " | friends " + YosseleFriends.Raised + "/" + YosseleFriends.Fell + " | buildings" + (bs == "" ? " none" : bs);
    }

    // ---------------------------------------------------------------- one map

    static IEnumerator Scenario()
    {
        plan = Plans[ModKitPlay.PlanIndex];
        matchStart = Time.realtimeSinceStartup;
        hm = ModKitPlay.LocalHero();
        owner = YosseleUtil.Owner(hm);
        team = YosseleUtil.Team(hm);
        used.Clear(); trail.Clear(); route.Clear(); built.Clear(); foeKinds.Clear(); foesSeen.Clear(); lanes.Clear();
        fallbacks = uiBuilds = uiUpgrades = uiSells = uiSelects = keySelects = felled = 0;
        ModKitPlay.Init();
        if (!probed)
            ModKitPlay.DumpInputs();
        Log("match on " + plan.Name + (plan.Money ? " (money: +" + MoneyPerTide + " of each aura a Tide)" : " (own income)") + ": map Tides " + YosseleServer.TidesOnMap()
            + ", tide " + Game_Mapvars.Instance.Wave.Value + ", timer " + F(Game_Mapvars.Instance.Time_Wave.Value) + ", wallet " + ModKitPlay.Wallet());
        foreach (Entity_Master e in Game_Entity.Instance.Entityclient)
            if (e != null && e.Taglistserver != null && (e.Taglistserver.Contains("Core") || e.Taglistserver.Contains("Enemycore")))
                Log("core: " + YosseleUtil.BeingName(e) + " team " + YosseleUtil.Team(e) + " at " + T(YosseleUtil.Pos(e)) + " tags " + string.Join(",", e.Taglistserver.ToArray()));
        ModKitTest.Log = m => Log(m);
        ModKitTest.MapDump(team, hm);
        Lanes();
        ModKitPlay.Beat();
        yield return ModKitPlay.Capture("intro");
        yield return ModKitPlay.WaitForWorld(30f);
        yield return ModKitPlay.Capture("start");
        if (!probed)
        {
            yield return SelectVessel();
            ModKitPlay.DumpUi("vessel selected", ModKitPlay.HudRoot(), 160);
            probed = true;
        }
        yield return Play();
        Log("map summary: UI builds " + uiBuilds + ", UI upgrades " + uiUpgrades + ", UI sells " + uiSells + ", vessel selected by F " + keySelects + " / by click " + uiSelects
            + ", fallbacks " + fallbacks + ", pointer nudges " + ModKitPlay.Nudges);
    }

    // ---------------------------------------------------------------- the lanes

    // Each foe path from a spawner to one of the team's cores, as the game plans it for a Dagger (Helper_Navtools).
    static readonly List<List<Vector3>> lanes = new List<List<Vector3>>();

    static Beingobject Mob(string name)
    {
        List<Bl_Address.Loadlist> lists;
        if (!Bl_Address.Groups.TryGetValue("Mob", out lists))
            return null;
        foreach (Bl_Address.Loadlist list in lists)
        {
            UnityEngine.Object item;
            if (list.Items.TryGetValue(name, out item) && item is Beingobject)
                return (Beingobject)item;
        }
        return null;
    }

    static void Lanes()
    {
        Beingobject dagger = Mob("Enemy_Dagger");
        if (dagger == null) { Log("lanes: no Dagger to ask"); return; }
        foreach (Entity_Master spawner in Cores("Enemycore", false))
            foreach (Entity_Master core in Cores("Core", true))
            {
                try
                {
                    Link_Being probe = new Link_Being { Being = dagger, Entity = dagger.Entity, Block = dagger.Block, Team = YosseleUtil.Team(spawner) };
                    Genitem start = ModKitTest.FoeTile(dagger, YosseleUtil.Pos(spawner), 0f, 3f, null);
                    Genitem end = ModKitTest.FoeTile(dagger, YosseleUtil.Pos(core), 1f, 3f, null);
                    if (start == null || end == null) { Log("lane " + YosseleUtil.BeingName(spawner) + " -> " + YosseleUtil.BeingName(core) + ": no tile (start " + (start != null) + ", end " + (end != null) + ")"); continue; }
                    // Round P3: to the core's own position no path came back; to a standable tile beside it, forced.
                    Link_Path p = Helper_Navtools.Getpath(start, end, probe, true);
                    if (!p.Valid || p.Path == null || p.Path.Count == 0)
                        p = Helper_Navtools.Getpath(start, YosseleUtil.Pos(core), probe, true);
                    if (!p.Valid || p.Path == null || p.Path.Count == 0) { Log("lane " + YosseleUtil.BeingName(spawner) + " " + T(start.Position) + " -> " + YosseleUtil.BeingName(core) + " " + T(end.Position) + ": no path (valid " + p.Valid + ", length " + p.Length + ")"); continue; }
                    List<Vector3> l = new List<Vector3>();
                    foreach (Genitem g in p.Path) l.Add(g.Position);
                    // The path comes back goal first (round P4 built at the spawners): spawner first, core last.
                    if (YosseleUtil.Flat(l[0], YosseleUtil.Pos(core)) < YosseleUtil.Flat(l[l.Count - 1], YosseleUtil.Pos(core)))
                        l.Reverse();
                    lanes.Add(l);
                    string s = "";
                    for (int i = 0; i < l.Count; i += Mathf.Max(1, l.Count / 8)) s += " " + T(l[i]);
                    Log("lane " + YosseleUtil.BeingName(spawner) + " " + T(YosseleUtil.Pos(spawner)) + " -> " + YosseleUtil.BeingName(core) + " " + T(YosseleUtil.Pos(core)) + ": " + l.Count + " tiles:" + s + " ... " + T(l[l.Count - 1]));
                }
                catch (Exception e) { Log("lane error: " + e.Message); }
            }
    }

    // ---------------------------------------------------------------- the HUD, by hand

    static bool VesselSelected() { return Game_Selection.Instance != null && Game_Selection.Instance.Selected == hm; }

    // F (the Focus key) first, as a player finds the vessel; then a click on it; then the HUD's vessel button.
    static IEnumerator SelectVessel()
    {
        if (ModKitPlay.ActiveCommand() != "")
            yield return Cancel();
        if (VesselSelected())
            yield break;
        yield return ModKitPlay.PressAction("Focus");
        yield return Wait(0.4f);
        if (VesselSelected()) { keySelects++; yield break; }
        if (keySelects + uiSelects == 0) Log("F didn't select the vessel (" + ModKitPlay.Selected() + "); clicking it");
        yield return ModKitPlay.ClickThing(hm, "the vessel");
        yield return Wait(0.3f);
        if (VesselSelected()) { uiSelects++; yield break; }
        Transform vs = ModKitPlay.Named(ModKitPlay.HudRoot(), "Vesselselect").Count > 0 ? ModKitPlay.Named(ModKitPlay.HudRoot(), "Vesselselect")[0] : null;
        if (vs != null)
        {
            yield return ModKitPlay.ClickUi(vs, "the HUD's vessel button");
            yield return Wait(0.4f);
        }
        if (!VesselSelected())
            Log("vessel not selected: " + ModKitPlay.Selected());
        else
            uiSelects++;
    }

    // Drops an armed command (ModKitPlay.Cancel: a right click, then Esc).
    static IEnumerator Cancel()
    {
        yield return ModKitPlay.Cancel();
    }
    // The build bar's entries: Selection/Buildlist/Buildoption1..4 (round P2).
    static Transform BuildEntry(int slot)
    {
        foreach (Transform t in ModKitPlay.Named(ModKitPlay.HudRoot(), "Buildoption" + (slot + 1)))
            if (t.name == "Buildoption" + (slot + 1))
                return t;
        return null;
    }

    static int Slot(string tower)
    {
        if (tower == YosseleData.Bonsai) return 1;
        for (int i = 0; i < YosseleData.Towers.Length; i++)
            if (YosseleData.Towers[i] == tower) return i;
        return -1;
    }

    static Vector3 Tile(string tower, Vector3 c, float ring, float maxDist)
    {
        Beingobject being;
        if (!YosseleContent.Beings.TryGetValue(tower, out being))
            return new Vector3(float.NaN, 0f, 0f);
        return ModKitTest.Spot(c, being, team, ring, maxDist, used, tower);
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
            default: return UnityEngine.InputSystem.Key.Digit7;
        }
    }

    // Build through the HUD: select the vessel, click its build-bar entry (or press its number), turn it with R,
    // point at the tile and click; the vessel walks into reach by itself.
    static IEnumerator BuildUi(string tower, Vector3 at, int turns, string why)
    {
        int before = Count(tower);
        Vector3 heroBefore = YosseleUtil.Pos(hm);
        yield return SelectVessel();
        int slot = Slot(tower);
        Transform entry = BuildEntry(slot);
        bool armed = false;
        string how = "";
        if (entry != null)
        {
            yield return ModKitPlay.ClickUi(entry, "build-bar entry " + (slot + 1) + " (" + ModKitPlay.Texts(entry) + ")");
            yield return Wait(0.2f);
            armed = ModKitPlay.ActiveCommand() != "";
            how = "bar";
        }
        if (!armed)
        {
            if (entry != null) Log("build-bar click armed nothing (" + ModKitPlay.Selected() + "); pressing " + (slot + 1));
            yield return ModKitPlay.Press(Digit(slot + 1));
            yield return Wait(0.2f);
            armed = ModKitPlay.ActiveCommand() != "";
            how = "key " + (slot + 1);
        }
        if (armed)
        {
            yield return ModKitPlay.Look(at);
            Vector2 sp;
            if (ModKitPlay.OnScreen(at, out sp, 80f))
                yield return ModKitPlay.MoveTo(sp);
            for (int i = 0; i < turns; i++)
                yield return ModKitPlay.PressAction("Rotate");
            yield return Wait(0.2f);
            if (uiBuilds < 3 || turns > 0)
                yield return ModKitPlay.Capture("placing_" + Short(tower));
            yield return ModKitPlay.ClickTile(at, "placing " + Short(tower) + (turns > 0 ? " (turned " + turns + ")" : ""));
            yield return Until(() => Count(tower) > before, 15f);
            if (ModKitPlay.ActiveCommand() != "")
                yield return Cancel();
        }
        else
            Log("couldn't arm " + Short(tower) + " (" + ModKitPlay.Selected() + ")");
        if (Count(tower) > before)
        {
            uiBuilds++;
            Log("BUILT by hand (" + how + "): " + Short(tower) + " at " + T(at) + " (" + why + "); vessel walked " + F(YosseleUtil.Flat(heroBefore, YosseleUtil.Pos(hm))) + " tiles; wallet " + ModKitPlay.Wallet());
            yield break;
        }
        if (!YosseleUtil.Alive(hm)) { Log("build of " + Short(tower) + " dropped: the vessel is down"); yield break; }
        fallbacks++;
        string json = "{\"Tower\":\"" + tower + "\",\"Position\":" + Vec(at) + ",\"Orientation\":" + (turns % 4) + ",\"Owner\":" + owner + "}";
        hm.GetComponent<Entity_Master_Remote>().Command_ServerRpc("Build", json, default(Unity.Netcode.ServerRpcParams));
        yield return Until(() => Count(tower) > before, 6f);
        Log("FALLBACK build " + Short(tower) + " at " + T(at) + ": " + (Count(tower) > before ? "built" : "refused (vessel " + F(YosseleUtil.Flat(YosseleUtil.Pos(hm), at)) + " tiles away)") + "; wallet " + ModKitPlay.Wallet());
    }

    static IEnumerator SelectBuilding(YosseleBuilding b)
    {
        if (ModKitPlay.ActiveCommand() != "")
            yield return Cancel();
        if (Game_Selection.Instance.Selected == b.Master)
            yield break;
        yield return ModKitPlay.ClickThing(b.Master, "the " + Short(b.Def.Id));
        yield return Wait(0.3f);
        if (Game_Selection.Instance.Selected != b.Master)
            Log("click didn't select the " + Short(b.Def.Id) + ": " + ModKitPlay.Selected());
    }

    // Upgrade through the HUD: select the building, click the upgrade's entry (Selection/Buildlist/UpgradeoptionN).
    static IEnumerator UpgradeUi(YosseleBuilding b, YosseleData.Upg u)
    {
        yield return SelectBuilding(b);
        Transform entry = ModKitPlay.WithText(ModKitPlay.HudRoot(), u.Display);
        if (entry != null)
        {
            yield return ModKitPlay.ClickUi(entry, "upgrade " + u.Display);
            yield return Until(() => b.Has(u.Key), 1.5f);
        }
        else
            Log("no HUD entry '" + u.Display + "' with the " + Short(b.Def.Id) + " selected (" + ModKitPlay.Selected() + "); entries: " + Entries());
        if (b.Has(u.Key))
        {
            uiUpgrades++;
            Log("UPGRADED by hand: " + Short(b.Def.Id) + " at " + T(b.Pos) + " -> " + u.Display + "; wallet " + ModKitPlay.Wallet());
            if (u.Tier >= 4 || uiUpgrades <= 2)
                yield return ModKitPlay.Capture("upgraded_" + u.Key);
            yield break;
        }
        if (!YosseleUtil.Alive(b.Master)) yield break;
        fallbacks++;
        b.Master.GetComponent<Entity_Master_Remote>().Command_ServerRpc("Upgrade", "{\"Upgrade\":\"" + u.Id + "\",\"Upgrader\":" + owner + "}", default(Unity.Netcode.ServerRpcParams));
        yield return Until(() => b.Has(u.Key), 2f);
        Log("FALLBACK upgrade " + Short(b.Def.Id) + " -> " + u.Display + ": " + (b.Has(u.Key) ? "taken" : "refused") + "; wallet " + ModKitPlay.Wallet());
    }

    static string Entries()
    {
        string s = "";
        foreach (Transform t in ModKitPlay.Named(ModKitPlay.HudRoot(), "Upgradeoption"))
            if (t.name.StartsWith("Upgradeoption"))
                s += " [" + t.name + ": " + ModKitPlay.Texts(t) + "]";
        return s == "" ? "none" : s;
    }

    // Sell through the HUD: the vessel's SELL (Selection/Commandlist/Sell5), then a click on the building.
    static IEnumerator SellUi(YosseleBuilding b)
    {
        string what = Short(b.Def.Id);
        Entity_Master m = b.Master;
        string walletBefore = ModKitPlay.Wallet();
        yield return SelectVessel();
        Transform sell = ModKitPlay.WithText(ModKitPlay.HudRoot(), "SELL");
        if (sell != null)
        {
            yield return ModKitPlay.ClickUi(sell, "SELL");
            yield return Wait(0.3f);
            Log("after SELL: " + ModKitPlay.Selected());
            yield return ModKitPlay.ClickThing(m, "the " + what + " to sell");
            yield return Until(() => !YosseleUtil.Alive(m), 3f);
        }
        else
            Log("no SELL entry on the vessel's HUD (" + ModKitPlay.Selected() + ")");
        if (!YosseleUtil.Alive(m))
        {
            uiSells++;
            Log("SOLD by hand: " + what + " (Growth " + b.Growth + ", " + b.Keys.Count + " upgrades); wallet " + walletBefore + " -> " + ModKitPlay.Wallet());
        }
        else
            Log("sell by hand failed for the " + what + " (" + ModKitPlay.Selected() + ")");
        if (ModKitPlay.ActiveCommand() != "")
            yield return Cancel();
    }

    static IEnumerator FastForward()
    {
        List<Transform> ff = ModKitPlay.Named(ModKitPlay.HudRoot(), "Fastforward");
        if (ff.Count == 0) { Log("no fast-forward button"); yield break; }
        bool before = Game_Mapvars.Instance.Time_Speedup.Value;
        yield return ModKitPlay.ClickUi(ff[0], "fast-forward");
        yield return Wait(1f);
        Log("fast-forward: " + before + " -> " + Game_Mapvars.Instance.Time_Speedup.Value + ", game speed " + F(Game_Mapvars.Instance.Time_Scale.Value) + ", Time.timeScale " + F(Time.timeScale));
    }

    // ---------------------------------------------------------------- playing

    static IEnumerator Play()
    {
        int lastTide = -1;
        float nextThink = 0f, nextTrail = 0f, tideSeen = 0f;
        // The Tree sell and replant ran in round P6 (sold by hand, the Bonsai kept its copy); left off since.
        bool ffDone = false, soldOne = false, rebuilt = true;
        while (true)
        {
            ModKitPlay.Beat();
            yield return ModKitPlay.Unpause();
            int tide = Game_Mapvars.Instance.Wave.Value;
            float now = Time.realtimeSinceStartup;
            if (tide != lastTide)
            {
                lastTide = tide;
                tideSeen = now;
                if (plan.Money && tide >= 1)
                {
                    Helper_Resources.Giveresources(YosseleUtil.Costs("Redaura=" + MoneyPerTide + ";Blueaura=" + MoneyPerTide + ";Yellowaura=" + MoneyPerTide), owner, Helper_Resources.Styles.Default);
                    Log("money: +" + MoneyPerTide + " of each aura (sandbox)");
                    string[] foes;
                    if (SandboxFoes.TryGetValue(tide, out foes))
                        Spawn(foes);
                }
                Log("STATUS " + Status());
                Log("COUNTERS " + Counters());
                yield return ModKitPlay.Capture("tide" + tide);
                if (tide >= 1)
                    yield return CloseUps("tide" + tide);
            }
            if (now >= nextTrail)
            {
                nextTrail = now + 0.5f;
                foreach (Entity_Master f in Foes())
                {
                    string k = T(YosseleUtil.Pos(f));
                    int n;
                    trail.TryGetValue(k, out n);
                    trail[k] = n + 1;
                    if (!foesSeen.Contains(f))
                    {
                        foesSeen.Add(f);
                        string kind = YosseleUtil.BeingName(f);
                        foeKinds.TryGetValue(kind, out n);
                        foeKinds[kind] = n + 1;
                    }
                }
                if (foesSeen.Count > 400) foesSeen.RemoveRange(0, 200);
            }
            if (!ffDone && tide >= 1 && Count(YosseleData.Cannon) >= 2)
            {
                ffDone = true;
                yield return FastForward();
            }
            if (YosseleUtil.Alive(hm) && now >= nextThink)
            {
                nextThink = now + (plan.Money ? 1.5f : 2.5f);
                yield return Think(tide);
            }
            // On the money map, at Tide 5: sell the Tree (by hand) and plant it again, so the second route and the
            // Bonsai keeping its copy are seen.
            if (plan.Money && !rebuilt && tide >= 5 && YosseleServer.TreeOf(owner) != null && YosseleServer.TreeOf(owner).Keys.Count >= 3)
            {
                rebuilt = true;
                YosseleDandelionBrain tree = YosseleServer.TreeOf(owner);
                Log("selling the Tree (" + string.Join("+", tree.Keys.ToArray()) + ", Growth " + tree.Growth + ") to plant it again on its other route; Bonsai standing " + YosseleServer.HasBonsai(owner));
                yield return SellUi(tree);
                yield return Wait(2f);
                Log("after the Tree went: Bonsai " + BonsaiState());
            }
            if (!YosseleUtil.Alive(hm))
            {
                // Felled, the vessel comes back as a new being (rounds P5 and P6 lost it): play on with that one.
                Entity_Master back = ModKitPlay.LocalHero();
                if (back != null && back != hm)
                {
                    Log("the vessel was felled and is back at " + T(YosseleUtil.Pos(back)) + " (felled " + (++felled) + " times)");
                    hm = back;
                    ModKitPlay.Me = back;
                }
                else
                    yield return Wait(1f);
            }
            string state = Game_Mapvars.Instance.Gamecomplete.Value.ToString();
            bool over = state != "" && state != "Running";
            bool enough = (tide >= plan.Tides && now - tideSeen > 40f) || now - matchStart > plan.Minutes * 60f;
            if (over || enough)
            {
                Log("stopping: " + (over ? "game over '" + state + "'" : "tide " + tide + " after " + F((now - matchStart) / 60f) + " min"));
                Log("STATUS " + Status());
                Log("COUNTERS " + Counters());
                LogTrail();
                if (!over && plan.Money)
                    yield return BulwarkTest();
                if (!over && !soldOne)
                {
                    List<YosseleBuilding> mine = Mine();
                    YosseleBuilding cheap = null;
                    foreach (YosseleBuilding b in mine) if (b.Def.Id == YosseleData.Cannon) cheap = b;
                    if (cheap != null) yield return SellUi(cheap);
                    soldOne = true;
                }
                yield return ModKitPlay.Capture("end");
                yield break;
            }
            yield return null;
        }
    }

    static Entity_Master Spawner()
    {
        Entity_Master any = null;
        foreach (Entity_Master e in Cores("Enemycore", false))
        {
            if (!YosseleUtil.BeingName(e).Contains("Sub")) return e;
            if (any == null) any = e;
        }
        return any;
    }

    static void Spawn(string[] foes)
    {
        Entity_Master sp = Spawner();
        if (sp == null) { Log("sandbox: no spawner"); return; }
        string got = "";
        foreach (string f in foes)
        {
            Entity_Master e = ModKitTest.SpawnFoe(f, YosseleUtil.Pos(sp), 1f, 4f, null);
            got += " " + f.Replace("Enemy_", "") + (e != null ? "" : "(failed)");
        }
        Log("sandbox: spawned at the spawner" + got);
    }

    static string Counters()
    {
        int tStrikes = 0, tInter = 0, tInterSeeds = 0, tBounced = 0, tRefunded = 0, snares = 0, drained = 0, mended = 0, marked = 0, fruits = 0, payouts = 0, lobs = 0, bounces = 0;
        foreach (YosseleBuilding b in Mine())
        {
            YosseleDandelionBrain d = b as YosseleDandelionBrain;
            if (d != null) { tStrikes += d.Strikes; tInter += d.Intercepted; tInterSeeds += d.InterceptSeeds; tBounced += d.Bounced; tRefunded += d.Refunded; }
            YosseleHengeBrain h = b as YosseleHengeBrain;
            if (h != null) { snares += h.Snares; drained += h.Drained; mended += h.Mended; marked += h.Marked; }
            YosselePachiraBrain p = b as YosselePachiraBrain;
            if (p != null) { fruits += p.Fruits; payouts += p.Payouts; }
            YosseleCannonBrain c = b as YosseleCannonBrain;
            if (c != null) { lobs += c.Lobs; bounces += c.Bounces; }
        }
        return "spores full " + YosseleSpores.Fulls + ", finishes " + YosseleSpores.Finishes + ", stuns " + YosseleSpores.Stuns + ", virals " + YosseleSpores.Virals + ", rises " + YosseleSpores.Rises
            + " | cannons lobs " + lobs + ", bounces " + bounces
            + " | trees strikes " + tStrikes + ", intercepted " + tInter + " (seeds " + tInterSeeds + "), bounced " + tBounced + ", refunded " + tRefunded + ", weeds " + YosseleDandelionBrain.WeedsGrown
            + " | vines " + YosseleVines.Count + " tiles, slowed " + YosseleVines.Slowed + "; henges snares " + snares + ", drained " + drained + ", mended " + mended + ", marked " + marked
            + " | hooks soaked " + YosseleHooks.Soaked + ", shared " + YosseleHooks.Shared + ", marked " + YosseleHooks.Marked + ", cleansed " + YosseleHooks.Cleansed
            + " | saplings payouts " + payouts + ", fruit " + fruits + " (dropped " + YosseleFruit.Dropped + ", picked " + YosseleFruit.Picked + ", lying " + YosseleFruit.Count + "), softened nodes " + YosseleSoftening.Count
            + " | friends raised " + YosseleFriends.Raised + ", fell " + YosseleFriends.Fell + ", capped " + YosseleFriends.Capped + ", standing " + YosseleFriends.CountOf(owner);
    }

    // The money map's last act: a Bulwark (the boss whose hits stun) spawned on a lane before the core, watched for a
    // minute and a quarter: does the Monument shrug its stun off, what do D-MDS's seeds cost against its bombs.
    static IEnumerator BulwarkTest()
    {
        Vector3 at = lanes.Count > 0 ? lanes[0][Mathf.Max(0, lanes[0].Count - 11)] : YosseleUtil.Pos(Spawner());
        Entity_Master b = ModKitTest.SpawnFoe("Enemy_Bulwark", at, 0f, 3f, used);
        Log("sandbox: Bulwark " + (b != null ? "spawned at " + T(YosseleUtil.Pos(b)) : "failed") + "; hooks cleansed " + YosseleHooks.Cleansed);
        if (b == null) yield break;
        float t0 = Time.realtimeSinceStartup;
        int shots = 0;
        while (Time.realtimeSinceStartup - t0 < 75f)
        {
            ModKitPlay.Beat();
            yield return Wait(5f);
            string hs = "";
            foreach (YosseleBuilding x in Mine())
                if (x is YosseleHengeBrain && x.Has("Monument"))
                    hs += " Monument hp " + F(YosseleUtil.Health(x.Master)) + "/" + F(YosseleUtil.MaxHealth(x.Master)) + " dmg-taken-mult " + F(1f - YosseleHooks.Reduction(x.Master));
            Log("Bulwark " + (YosseleUtil.Alive(b) ? "hp " + F(YosseleUtil.Health(b)) + " at " + T(YosseleUtil.Pos(b)) : "down") + " | cleansed " + YosseleHooks.Cleansed + hs + " | " + Counters());
            if (YosseleUtil.Alive(b) && shots < 2 && Time.realtimeSinceStartup - t0 > 15f + shots * 25f)
            {
                shots++;
                yield return ModKitPlay.Look(YosseleUtil.Pos(b));
                yield return ModKitPlay.Capture("bulwark" + shots);
            }
            if (!YosseleUtil.Alive(b) || Game_Mapvars.Instance.Gamecomplete.Value.ToString() == "Lose") break;
        }
    }

    static string BonsaiState()
    {
        foreach (YosseleBuilding b in Mine())
            if (b is YosseleBonsaiBrain)
                return "standing, keys " + string.Join("+", b.Keys.ToArray()) + ", Growth " + b.Growth + ", seed limit " + F(b.Stat("Seedlimit", -1f));
        return "none";
    }

    static void LogTrail()
    {
        List<KeyValuePair<string, int>> l = new List<KeyValuePair<string, int>>(trail);
        l.Sort((a, b) => b.Value.CompareTo(a.Value));
        string s = "";
        for (int i = 0; i < l.Count && i < 30; i++) s += " " + l[i].Key + "x" + l[i].Value;
        Log("foe trail (most walked first):" + s);
    }

    // Spots beside the lanes, last stretch first: a fighting building goes about 1.5 tiles off a lane tile 3-12 tiles
    // before a core, the n-th one further out and on the next lane, so every core's approach gets covered.
    static Vector3 FightSpot(string tower, int n)
    {
        if (lanes.Count > 0)
        {
            for (int tries = 0; tries < 12; tries++)
            {
                List<Vector3> lane = lanes[(n + tries) % lanes.Count];
                int back = 3 + ((n + tries) / lanes.Count) * 2;
                if (back >= lane.Count) back = lane.Count - 1;
                Vector3 at = Tile(tower, lane[lane.Count - 1 - back], 1.5f, 3f);
                if (!float.IsNaN(at.x)) return at;
            }
        }
        // No planned lane: the tiles foes walked most within 10 of a core, once they have walked.
        List<KeyValuePair<string, int>> walked = new List<KeyValuePair<string, int>>(trail);
        walked.Sort((a, b) => b.Value.CompareTo(a.Value));
        List<Entity_Master> cores = Cores("Core", true);
        foreach (KeyValuePair<string, int> kv in walked)
        {
            string[] p = kv.Key.Split(',');
            Vector3 t = new Vector3(int.Parse(p[0]), int.Parse(p[1]), int.Parse(p[2]));
            bool near = false;
            foreach (Entity_Master c in cores) if (YosseleUtil.Flat(t, YosseleUtil.Pos(c)) <= 10f) near = true;
            if (!near) continue;
            Vector3 at = Tile(tower, t, 1.5f, 3f);
            if (!float.IsNaN(at.x)) return at;
        }
        Entity_Master core = cores.Count > 0 ? cores[n % cores.Count] : hm;
        return Tile(tower, YosseleUtil.Pos(core), 3f, 7f);
    }

    // The way a Stonehenge should face: its vines run "forward" (orientation 0 faces +z, round P2); turn toward the
    // nearest lane tile. R turns it; how far each press turns is logged by the brain's facing line.
    static int Turns(Vector3 at)
    {
        Vector3 best = at + Vector3.forward;
        float bd = float.MaxValue;
        foreach (List<Vector3> lane in lanes)
            foreach (Vector3 t in lane)
            {
                float d = YosseleUtil.Flat(t, at);
                if (d > 0.5f && d < bd) { bd = d; best = t; }
            }
        Vector3 dir = best - at;
        if (Mathf.Abs(dir.z) >= Mathf.Abs(dir.x)) return dir.z >= 0f ? 0 : 2;
        return dir.x >= 0f ? 1 : 3;
    }

    static YosseleData.Build Def(string id)
    {
        foreach (YosseleData.Build b in YosseleData.Builds)
            if (b.Id == id) return b;
        return null;
    }

    static bool Afford(string costs, float reserveRed)
    {
        foreach (string part in costs.Split(';'))
        {
            string[] kv = part.Split('=');
            if (kv.Length != 2) continue;
            float keep = kv[0] == "Redaura" ? reserveRed : 0f;
            if (ModKitPlay.Aura(kv[0]) < YosseleUtil.F(kv[1]) + keep)
                return false;
        }
        return true;
    }

    static int Built(string id) { int n; built.TryGetValue(id, out n); return n; }

    // One decision, the way a careful player spends: two cannons, then the Tree, a Stonehenge on each lane, a Sapling
    // at ore, more cannons as Tides rise; then upgrades along each building's route, cheapest tier first.
    static IEnumerator Think(int tide)
    {
        if (ModKitPlay.ActiveCommand() != "")
            yield return Cancel();
        int cannons = Count(YosseleData.Cannon), henges = Count(YosseleData.Henge), saplings = Count(YosseleData.Pachira);
        int wantCannons = plan.Money ? 4 : 2 + tide;
        int wantHenges = plan.Money ? 3 : Mathf.Min(1 + tide / 3, Mathf.Max(1, lanes.Count));
        int wantSaplings = plan.Money ? 3 : (tide >= 2 ? 2 : 1);
        string pick = "";
        if (cannons < 2 && Afford(Def(YosseleData.Cannon).Costs, 0f)) pick = YosseleData.Cannon;
        else if (YosseleServer.TreeSlot(owner) != "" && Afford(Def(YosseleData.Dandelion).Costs, 0f)) pick = YosseleServer.TreeSlot(owner);
        else if (henges < wantHenges && Afford(Def(YosseleData.Henge).Costs, 0f)) pick = YosseleData.Henge;
        else if (saplings < wantSaplings && Afford(Def(YosseleData.Pachira).Costs, 0f)) pick = YosseleData.Pachira;
        else if (cannons < wantCannons && Afford(Def(YosseleData.Cannon).Costs, 0f)) pick = YosseleData.Cannon;
        if (pick != "")
        {
            int n = Built(pick);
            Vector3 at = pick == YosseleData.Pachira ? OreSpot(n) : FightSpot(pick, Built(YosseleData.Cannon) + Built(YosseleData.Henge) + Built(YosseleData.Dandelion));
            if (!float.IsNaN(at.x))
            {
                int turns = pick == YosseleData.Henge ? Turns(at) : 0;
                built[pick] = n + 1;
                yield return BuildUi(pick, at, turns, "tide " + tide);
                // The route this building takes.
                // An honest map's Tree skips D-MDS (round P3: with no shooters on Purlest it stopped the Tree's damage).
                foreach (YosseleBuilding b in Mine())
                    if (b.Def.Id == pick && !route.ContainsKey(b)) route[b] = pick == YosseleData.Dandelion && !plan.Money ? n + 1 : n;
            }
            yield break;
        }
        // An upgrade: the next step of a route, lowest tier first; keep 15 Red for a cannon on an honest map.
        YosseleBuilding bestB = null;
        YosseleData.Upg bestU = null;
        foreach (YosseleBuilding b in Mine())
        {
            YosseleData.Upg u = NextUpgrade(b);
            if (u == null || !Afford(u.Costs, plan.Money ? 0f : (cannons < wantCannons ? 15f : 0f))) continue;
            if (bestU == null || u.Tier < bestU.Tier) { bestB = b; bestU = u; }
        }
        if (bestU != null)
            yield return UpgradeUi(bestB, bestU);
    }

    static YosseleData.Upg NextUpgrade(YosseleBuilding b)
    {
        string[][] routes;
        if (b.Def.Upgrades == null || !Routes.TryGetValue(b.Def.Id, out routes)) return null;
        int r;
        if (!route.TryGetValue(b, out r)) { r = 0; route[b] = 0; }
        string[] keys = routes[r % routes.Length];
        foreach (string k in keys)
        {
            if (b.Has(k)) continue;
            foreach (YosseleData.Upg u in b.Def.Upgrades)
                if (u.Key == k) return u;
            return null;
        }
        return null;
    }

    // A tile for the n-th Sapling with ore in its reach (2.5), the ore nodes nearest the cores first.
    static Vector3 OreSpot(int n)
    {
        List<Entity_Master> ore = new List<Entity_Master>();
        foreach (Entity_Master e in Game_Entity.Instance.Entityclient)
            if (e != null && YosseleUtil.HasTag(e, "Resource") && e.Statsserver != null && e.Statsserver.ContainsKey("Healthresource"))
                ore.Add(e);
        if (ore.Count == 0) { Log("no ore node on the map"); return new Vector3(float.NaN, 0f, 0f); }
        Vector3 home = Cores("Core", true).Count > 0 ? YosseleUtil.Pos(Cores("Core", true)[0]) : YosseleUtil.Pos(hm);
        ore.Sort((a, b) => YosseleUtil.Flat(YosseleUtil.Pos(a), home).CompareTo(YosseleUtil.Flat(YosseleUtil.Pos(b), home)));
        for (int i = 0; i < ore.Count; i++)
        {
            Entity_Master node = ore[(n + i) % ore.Count];
            Vector3 at = Tile(YosseleData.Pachira, YosseleUtil.Pos(node), 1.5f, 2.2f);
            if (!float.IsNaN(at.x))
            {
                Log("Sapling spot " + T(at) + " beside " + YosseleUtil.BeingName(node) + " at " + T(YosseleUtil.Pos(node)));
                return at;
            }
        }
        return new Vector3(float.NaN, 0f, 0f);
    }

    static IEnumerator CloseUps(string name)
    {
#if YOSSELE_SHOTS
        List<YosseleBuilding> mine = Mine();
        int shown = 0;
        List<string> kinds = new List<string>();
        foreach (YosseleBuilding b in mine)
        {
            if (kinds.Contains(b.Def.Id) || shown >= 3) continue;
            kinds.Add(b.Def.Id);
            shown++;
            yield return YosseleShots.Close("play_" + name + "_" + Short(b.Def.Id), b.Pos, 5f);
        }
        if (YosseleUtil.Alive(hm))
            yield return YosseleShots.Close("play_" + name + "_vessel", YosseleUtil.Pos(hm), 4f);
#else
        yield break;
#endif
    }
}
#endif

