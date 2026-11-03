#if SPOOL_PLAY
// Debug build only (Build-SpoolMod.ps1 -Define SPOOL_PLAY,SPOOL_SHOTS,MODKIT_PLAY,MODKIT_TEST): Spool's play test
// (2026-10-26). A virtual player (kit ModKitPlay) starts each map in Plans and plays it through the HUD with a virtual
// mouse and keyboard the way a person would: the Yarn Stash from the Stash-only bar, then Tins, Pin Cushions, Looms and
// Thread Heads from the full bar; TARGET and SPOT picked on the map; RETURN and MEND pressed; PATCH picked on a hurt
// building; Spool walked by right clicks to pick Thread up and hand it in; upgrades clicked in each building's tree;
// SELL on a walking Stitched and a Thread Head; the fast-forward button. It logs "[Spool] PLAY ..." lines and in-game
// captures. A step the HUD can't do is done with the command the button would send, logged "FALLBACK".
// A "money" map gives the player aura each Tide (the Sandbox Cardinal's money button does the same) so every upgrade
// route gets bought and seen in real Tides; an honest map is played on the map's own income, for balance.
public static class SpoolPlay
{
    class Plan { public string Name; public bool Money; public int Tides; public float Minutes; }
    static readonly Plan[] Plans =
    {
#if SPOOL_SMART
        // The balance round's Step 2 (docs/reports/BALANCE_2026-11-03.md section 9): full maps on their own income, the wall first.
        new Plan { Name = "3_Opal", Money = false, Tides = 16, Minutes = 50f },
        new Plan { Name = "3_Opal", Money = false, Tides = 16, Minutes = 50f },
        new Plan { Name = "3B_Woozy", Money = false, Tides = 16, Minutes = 50f },
#else
        // Rounds P7-P9 played 3_Opal on its own income (lost at Tides 4, 1 and 2) and 3B_Woozy with money (lost at Tide 4
        // twice); this run checks the fixed Thread Head panel, MEND and selling by hand.
        new Plan { Name = "3B_Woozy", Money = true, Tides = 4, Minutes = 12f },
#endif
    };

#if SPOOL_TUNE
    // The balance study's P1-P3, in this play build only (docs/reports/BALANCE_2026-11-03.md section 8): Dart Range 4.5, 6 Crafted a Tin, Stitched 3 and
    // Dart 3.5 Damage. Called before the content is built.
    public static void Tune()
    {
        foreach (SpoolData.Unit u in SpoolData.Units)
            for (int i = 0; i < u.Stats.Length; i++)
            {
                if (u.Id == SpoolData.Stitched && u.Stats[i] == "Damage=2") u.Stats[i] = "Damage=3";
                if (u.Id == SpoolData.Dart && u.Stats[i] == "Damage=2.5") u.Stats[i] = "Damage=3.5";
                if (u.Id == SpoolData.Dart && u.Stats[i] == "Range=3.5") u.Stats[i] = "Range=4.5";
            }
        foreach (SpoolData.Build b in SpoolData.Builds)
            if (b.Id == SpoolData.Tin)
                for (int i = 0; i < b.Stats.Length; i++)
                    if (b.Stats[i] == "Units=4") b.Stats[i] = "Units=6";
        SpoolLog.Info("PLAY TUNE on: Dart Range 4.5, 6 Crafted a Tin, Stitched 3 and Dart 3.5 Damage");
    }
#endif
    const float MoneyPerTide = 1500f;

    // On a money map, the Sandbox Cardinal's other button: foes of a kind spawned at the spawner at a Tide's start, so
    // the Pin Cushion meets shooters (Pin Barrage) and heavy melee, and the Crafted meet more than Daggers.
    static readonly Dictionary<int, string[]> SandboxFoes = new Dictionary<int, string[]>
    {
        { 3, new[] { "Enemy_Ballista", "Enemy_Ballista", "Enemy_Spear" } },
        { 4, new[] { "Enemy_Maul", "Enemy_Aegis", "Enemy_Ballista" } },
        { 5, new[] { "Enemy_Onager", "Enemy_Excalibur", "Enemy_Maul" } },
        { 6, new[] { "Enemy_Claymore", "Enemy_Maul", "Enemy_Ballista", "Enemy_Aegis" } },
        { 7, new[] { "Enemy_Excalibur", "Enemy_Onager", "Enemy_Maul", "Enemy_Spear" } },
    };

    static void Spawn(string[] foes)
    {
        Entity_Master sp = null;
        foreach (Entity_Master e in ModKitPlay.Cores("Enemycore", false))
            if (sp == null || !SpoolUtil.BeingName(e).Contains("Sub")) sp = e;
        if (sp == null) { Log("sandbox: no spawner"); return; }
        string got = "";
        foreach (string f in foes)
        {
            Entity_Master e = ModKitTest.SpawnFoe(f, SpoolUtil.Pos(sp), 1f, 4f, null);
            got += " " + f.Replace("Enemy_", "") + (e != null ? "" : "(failed)");
        }
        Log("sandbox: spawned at " + SpoolUtil.BeingName(sp) + got);
    }

    // Upgrade routes: the n-th building of a kind takes route n (mod the count).
    static readonly Dictionary<string, string[][]> Routes = new Dictionary<string, string[][]>
    {
#if SPOOL_SMART
        { SpoolData.Tin, new[] { new[] { "Woolen", "Design", "Surprise" } } },
#else
        { SpoolData.Tin, new[] { new[] { "Recycle", "Discounted", "Surprise" }, new[] { "Woolen", "Design", "Surprise" } } },
#endif
        { SpoolData.Head, new[] { new[] { "Delivery" }, new[] { "Patchwork" } } },
        { SpoolData.Cushion, new[] { new[] { "Sharper", "Barrage" }, new[] { "Unravel", "Stuffing", "Overstuffed" }, new[] { "Sharper", "Stuffing", "Burst" } } },
        { SpoolData.Loom, new[] { new[] { "Direct" }, new[] { "Seam" } } },
    };

    static bool setup;
    static void Log(string m) { SpoolLog.Info("PLAY " + m); }

    public static void Update()
    {
        if (!setup)
        {
            setup = true;
            ModKitPlay.Log = Log;
#if SPOOL_SHOTS
            ModKitPlay.Shot = SpoolShots.Shot;
#endif
            ModKitPlay.Host = SpoolMod.Instance;
            ModKitPlay.Hero = SpoolData.Hero;
            List<string> names = new List<string>();
            foreach (Plan p in Plans) names.Add(p.Name);
            ModKitPlay.Plans = names.ToArray();
            ModKitPlay.Scenario = Scenario;
#if SPOOL_MEASURE
            SpoolMeasure.Setup();
#endif
#if SPOOL_SMART
            ModKitPlay.ReturnAtEnd = true;
            ModKitMeasure.Log = Log;
            ModKitPlay.AtBoat = ModKitMeasure.EmptyLoadout;
            ModKitPlay.AtEnd = ModKitMeasure.RestoreLoadout;
#endif
            Log("play build: maps " + string.Join(", ", ModKitPlay.Plans));
        }
        ModKitPlay.Update();
    }

    // ---------------------------------------------------------------- state of the match

    static Plan plan;
    static Entity_Master hm;
    static long owner;
    static bool probed;
    static float matchStart;
    static readonly List<Vector3> used = new List<Vector3>();
    static readonly Dictionary<SpoolBuilding, int> route = new Dictionary<SpoolBuilding, int>();
    static readonly Dictionary<string, int> built = new Dictionary<string, int>();
    static int patches, targets, spots, returns, mends, walks, stitchedSold, headsSold;
    static string lastBar = "";

    static IEnumerator Wait(float s) { yield return new WaitForSeconds(s); }
    static string F(float v) { return v.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture); }
    static string T(Vector3 v) { return ModKitPlay.T(v); }
    static string Short(string id) { return id.Replace("Tower_Spool", "").Replace("Mob_Spool", ""); }

    static SpoolPlayer Me { get { return SpoolServer.Player(owner); } }

    static List<SpoolBuilding> Mine()
    {
        List<SpoolBuilding> l = new List<SpoolBuilding>();
        foreach (SpoolBuilding b in SpoolServer.Buildings)
            if (b != null && b.Started && b.Master != null && SpoolUtil.Alive(b.Master) && b.Owner == owner)
                l.Add(b);
        return l;
    }

    static int Count(string id)
    {
        int n = 0;
        foreach (SpoolBuilding b in Mine())
            if (b.Def.Id == id) n++;
        return n;
    }

    static List<SpoolUnitBrain> Units(string id)
    {
        List<SpoolUnitBrain> l = new List<SpoolUnitBrain>();
        foreach (SpoolUnitBrain u in SpoolServer.Units)
            if (u != null && u.Master != null && SpoolUtil.Alive(u.Master) && u.Owner == owner && (id == null || u.Def.Id == id))
                l.Add(u);
        return l;
    }

    static string Keys(SpoolBuilding b)
    {
        List<string> k = new List<string>();
        string[][] rs;
        if (Routes.TryGetValue(b.Def.Id, out rs))
            foreach (string[] r in rs)
                foreach (string key in r)
                    if (b.Has(key) && !k.Contains(key)) k.Add(key);
        return string.Join("+", k.ToArray());
    }

    static string Status()
    {
        string bs = "";
        foreach (SpoolBuilding b in Mine())
        {
            string extra = "";
            if (b is SpoolTinBrain) { SpoolTinBrain t = (SpoolTinBrain)b; extra = " crafted " + t.Alive + "/" + t.Slots + " stitched " + t.Stitched + " spent " + t.Spent + (t.HasSpot ? " spot " + T(t.Spot) : " no spot"); }
            else if (b is SpoolHeadBrain) { SpoolHeadBrain h = (SpoolHeadBrain)b; extra = " carried " + h.Carried + " collected " + h.Collected + " deliveries " + h.Deliveries + (h.Mending ? " MEND on" : ""); }
            else if (b is SpoolCushionBrain) { SpoolCushionBrain c = (SpoolCushionBrain)b; extra = " taken " + F(c.Taken) + " reflected " + F(c.Reflected) + " pricks " + c.Pricks + " spun " + c.Spun + " mangled " + c.Mangled; }
            else if (b is SpoolLoomBrain) { SpoolLoomBrain l = (SpoolLoomBrain)b; extra = " held " + l.Held + " spun " + l.Spun + " sent " + l.Sent + " strikes " + l.Strikes + " seams " + l.Seams; }
            string k = Keys(b);
            bs += " " + Short(b.Def.Id) + "(" + (k != "" ? k + " " : "") + "hp " + F(SpoolUtil.Health(b.Master)) + "/" + F(SpoolUtil.MaxHealth(b.Master)) + " dmg " + F(b.Master.Damagedone) + " kills " + b.Master.Killed + extra + ")";
        }
        int crafted = 0; float cdmg = 0f; int ckills = 0;
        foreach (SpoolUnitBrain u in Units(null)) { crafted++; cdmg += u.Master.Damagedone; ckills += u.Master.Killed; }
        return ModKitPlay.Tide() + " | " + ModKitPlay.Wallet() + " | Thread stash " + Me.Stash + "/" + Me.Cap + " carried " + Me.Carried
            + " (dropped " + SpoolServer.Dropped + ", picked " + SpoolServer.Picked + ", delivered " + SpoolServer.Delivered + ", ground " + SpoolServer.Drops.Count + ")"
            + " | vessel " + (SpoolUtil.Alive(hm) ? F(SpoolUtil.Health(hm)) + "/" + F(SpoolUtil.MaxHealth(hm)) + " dmg " + F(hm.Damagedone) + " kills " + hm.Killed : "down")
            + " | cores" + ModKitPlay.Cores() + " | foes now " + ModKitPlay.Foes().Count + ", seen" + ModKitPlay.FoesSeen()
            + " | crafted alive " + crafted + " (live dmg " + F(cdmg) + ", kills " + ckills + ") | buildings" + (bs == "" ? " none" : bs);
    }

    // ---------------------------------------------------------------- one map

    static IEnumerator Scenario()
    {
        plan = Plans[ModKitPlay.PlanIndex];
        matchStart = Time.realtimeSinceStartup;
        hm = ModKitPlay.LocalHero();
        owner = SpoolUtil.Owner(hm);
        ModKitPlay.Reset(hm);
        ModKitPlay.ResetMap();
        used.Clear(); route.Clear(); built.Clear();
        patches = targets = spots = returns = mends = walks = stitchedSold = headsSold = 0;
        spotMovedFlag = false;
        targetTries.Clear();
        ModKitPlay.Init();
        if (!probed)
            ModKitPlay.DumpInputs();
        Log("match on " + plan.Name + (plan.Money ? " (money: +" + MoneyPerTide + " of each aura a Tide)" : " (own income)") + ": " + ModKitPlay.Tide() + ", wallet " + ModKitPlay.Wallet());
        foreach (Entity_Master e in Game_Entity.Instance.Entityclient)
            if (e != null && e.Taglistserver != null && (e.Taglistserver.Contains("Core") || e.Taglistserver.Contains("Enemycore")))
                Log("core: " + SpoolUtil.BeingName(e) + " team " + SpoolUtil.Team(e) + " at " + T(SpoolUtil.Pos(e)) + " tags " + string.Join(",", e.Taglistserver.ToArray()));
        ModKitTest.Log = m => Log(m);
        if (!probed) ModKitTest.MapDump(ModKitPlay.Team, hm);
        ModKitPlay.PlanLanes();
        ModKitPlay.Beat();
        yield return ModKitPlay.Capture("intro");
        yield return ModKitPlay.WaitForWorld(30f);
        yield return ModKitPlay.Capture("start");
        yield return ModKitPlay.SelectVessel();
        lastBar = ModKitPlay.BuildBar();
        Log("build bar before the Stash:" + lastBar + "; vessel buttons:" + ModKitPlay.CommandTitles());
        yield return ModKitPlay.Capture("bar_before_stash");
        if (!probed)
        {
            ModKitPlay.DumpUi("vessel selected", ModKitPlay.HudRoot(), 160);
            probed = true;
        }
        yield return Play();
        Log("map summary: " + ModKitPlay.Counts() + "; PATCH " + patches + ", TARGET " + targets + ", SPOT " + spots + ", RETURN " + returns + ", MEND " + mends
            + ", walks for Thread " + walks + ", sold Stitched " + stitchedSold + ", sold Heads " + headsSold);
    }

    // ---------------------------------------------------------------- building

    static Beingobject Being(string id) { Beingobject b; return SpoolContent.Beings.TryGetValue(id, out b) ? b : null; }

    static SpoolData.Build Def(string id)
    {
        foreach (SpoolData.Build b in SpoolData.Builds)
            if (b.Id == id) return b;
        return null;
    }

    static int Built(string id) { int n; built.TryGetValue(id, out n); return n; }

    // The bar slot for a buildable: the Stash alone while none stands, then Tin, Thread Head, Cushion, Loom.
    static int Slot(string id)
    {
        if (id == SpoolData.Stash) return 0;
        for (int i = 0; i < SpoolData.Buildables.Length; i++)
            if (SpoolData.Buildables[i] == id || (id == SpoolData.Head && SpoolData.Buildables[i] == SpoolData.HeadToken)) return i;
        return -1;
    }

    static IEnumerator Build(string id, Vector3 at, string why)
    {
        int before = id == SpoolData.Head ? Count(SpoolData.Head) : Count(id);
        string being = id == SpoolData.Head ? SpoolData.HeadToken : id;
        built[id] = Built(id) + 1;
        yield return ModKitPlay.Build(Slot(id), being, at, 0, () => Count(id) > before, why);
        if (ModKitPlay.LastOk)
            foreach (SpoolBuilding b in Mine())
                if (b.Def.Id == id && !route.ContainsKey(b)) route[b] = Built(id) - 1;
        string bar = ModKitPlay.BuildBar();
        if (bar != lastBar)
        {
            Log("build bar now:" + bar);
            lastBar = bar;
            yield return ModKitPlay.Capture("bar_after_" + Short(id));
        }
    }

    static Vector3 StashSpot()
    {
        List<Entity_Master> cores = ModKitPlay.Cores("Core", true);
        Vector3 c = cores.Count > 0 ? ModKitPlay.Pos(cores[0]) : SpoolUtil.Pos(hm);
        // Between the core and the lanes, a few tiles out: Spool walks Thread home here.
        Vector3 toward = ModKitPlay.Lanes.Count > 0 ? ModKitPlay.LaneTile(0, 6) : c;
        Vector3 mid = float.IsNaN(toward.x) ? c : Vector3.Lerp(c, toward, 0.4f);
        return ModKitTest.Spot(mid, Being(SpoolData.Stash), ModKitPlay.Team, 1.5f, 6f, used, "Stash");
    }

    // A path tile for a Thread Head's token (walk-through), out along a lane where foes fall.
    static Vector3 HeadSpot(int n)
    {
        Beingobject token = Being(SpoolData.HeadToken);
        if (token == null) return new Vector3(float.NaN, 0f, 0f);
        for (int back = 6 + 2 * n; back < 20; back += 2)
        {
            Vector3 t = ModKitPlay.LaneTile(n, back);
            if (float.IsNaN(t.x)) break;
            Vector3 at = ModKitTest.Spot(t, token, ModKitPlay.Team, 0f, 1.5f, used, "Head");
            if (!float.IsNaN(at.x)) return at;
        }
        return ModKitPlay.LaneSpot(token, n, used, 0.5f);
    }

    // A Pin Cushion is a wall: on a lane tile a few tiles before a core, where foes have to hit it.
    static Vector3 CushionSpot(int n)
    {
        Beingobject cushion = Being(SpoolData.Cushion);
#if SPOOL_SMART
        // The smart player's wall: a line across the last stretch of the lane (BALANCE section 5).
        List<Vector3> line = ModKitMeasure.LineAcross(cushion, ModKitPlay.Team, 5, 1, used);
        if (line.Count > 0) return line[0];
#endif
        for (int back = 3 + n; back < 14; back++)
        {
            Vector3 t = ModKitPlay.LaneTile(n, back);
            if (float.IsNaN(t.x)) break;
            Vector3 at = ModKitTest.Spot(t, cushion, ModKitPlay.Team, 0f, 1.2f, used, "Cushion");
            if (!float.IsNaN(at.x)) return at;
        }
        return ModKitPlay.LaneSpot(cushion, n, used, 1f);
    }

    // ---------------------------------------------------------------- playing

    static IEnumerator Play()
    {
        int lastTide = -1;
        float nextThink = 0f, nextWatch = 0f, nextChores = 0f, tideSeen = 0f;
        bool ffDone = false, ended = false, soldEarly = false;
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
                    Helper_Resources.Giveresources(SpoolUtil.Costs("Redaura=" + MoneyPerTide + ";Blueaura=" + MoneyPerTide + ";Yellowaura=" + MoneyPerTide), owner, Helper_Resources.Styles.Default);
                    Log("money: +" + MoneyPerTide + " of each aura (sandbox)");
                    string[] foes;
                    if (SandboxFoes.TryGetValue(tide, out foes))
                        Spawn(foes);
                }
                Log("STATUS " + Status());
#if SPOOL_SMART
                Log("TIDE " + tide + " wallet " + ModKitPlay.Wallet() + " Thread dropped " + SpoolServer.Dropped + " picked " + SpoolServer.Picked + " delivered " + SpoolServer.Delivered + " stash " + Me.Stash + " foes seen" + ModKitPlay.FoesSeen());
#else
                yield return ModKitPlay.Capture("tide" + tide);
                if (tide >= 1)
                    yield return CloseUps("tide" + tide);
#endif
            }
            if (now >= nextWatch)
            {
                nextWatch = now + 0.5f;
                ModKitPlay.WatchFoes();
            }
            if (!SpoolUtil.Alive(hm))
            {
                // Felled, the vessel comes back as a new being: play on with that one.
                Entity_Master back = ModKitPlay.LocalHero();
                if (back != null && back != hm)
                {
                    Log("the vessel was felled and is back at " + T(SpoolUtil.Pos(back)));
                    hm = back;
                    ModKitPlay.Me = back;
                }
            }
            // Selling by hand before the heavy Tides can end the map (rounds P7-P9 never got to the end-of-map sells).
#if SPOOL_SMART
            soldEarly = true;
#endif
            if (!soldEarly && tide >= 2 && Units(SpoolData.Stitched).Count + Units(SpoolData.Dart).Count > 0 && Count(SpoolData.Head) > 0)
            {
                soldEarly = true;
                yield return SellSome();
            }
            if (!ffDone && tide >= 1 && Count(SpoolData.Tin) >= 1)
            {
                ffDone = true;
                yield return ModKitPlay.FastForward();
            }
            if (SpoolUtil.Alive(hm) && now >= nextThink)
            {
                nextThink = now + (plan.Money ? 1.5f : 2.5f);
                yield return Think(tide);
            }
            if (SpoolUtil.Alive(hm) && now >= nextChores && Me.Home != null)
            {
                nextChores = now + 4f;
                yield return Chores(tide);
            }
            string state;
            bool over = ModKitPlay.Over(out state);
            bool enough = (tide >= plan.Tides && now - tideSeen > 40f) || now - matchStart > plan.Minutes * 60f;
            if ((over || enough) && !ended)
            {
                ended = true;
                Log("stopping: " + (over ? "game over '" + state + "'" : ModKitPlay.Tide() + " after " + F((now - matchStart) / 60f) + " min"));
                Log("STATUS " + Status());
                Log("foe trail (most walked first):" + ModKitPlay.TrailText(30));
                if (!over)
                    yield return SellSome();
                yield return ModKitPlay.Capture("end");
                yield break;
            }
            yield return null;
        }
    }

    // What a Spool player does between builds: PATCH what is hurt, fetch Thread lying about and hand it in, send a full
    // Thread Head home, switch MEND on, move a Thread Head's spot once, aim each new Tin.
    static bool spotMovedFlag;

    static IEnumerator Chores(int tide)
    {
        // Aim any Tin that has no spot yet: TARGET, then a lane tile within 8 tiles of it.
        foreach (SpoolBuilding b in Mine())
        {
            SpoolTinBrain t = b as SpoolTinBrain;
            if (t == null) continue;
            int tries;
            targetTries.TryGetValue(t, out tries);
            if (t.HasSpot || tries >= 3) continue;
            Vector3 at = TargetTile(t);
            if (float.IsNaN(at.x)) continue;
            targetTries[t] = tries + 1;
            int before = t.Targets;
            yield return ModKitPlay.Command(t.Master, "Cookie Tin", SpoolData.TargetName, true, at, null, () => t.Targets > before || t.HasSpot);
            Log("TARGET result: " + (t.HasSpot ? "spot " + T(t.Spot) : "refused") + " (Tin says '" + t.LastTarget + "')");
            if (t.HasSpot) targets++;
            yield break;
        }
        // PATCH the most hurt building or Thread Head below 70%, if the Stash has the Thread.
        if (Me.Stash >= SpoolData.PatchCost && Time.time >= Me.Hero.PatchReady)
        {
            Entity_Master hurt = null;
            float worst = 0.7f;
            foreach (SpoolBuilding b in Mine())
            {
                float frac = SpoolUtil.Health(b.Master) / Mathf.Max(0.01f, SpoolUtil.MaxHealth(b.Master));
                if (frac < worst) { worst = frac; hurt = b.Master; }
            }
            if (hurt != null)
            {
                if (SpoolUtil.Flat(SpoolUtil.Pos(hurt), SpoolUtil.Pos(hm)) > SpoolData.PatchRange)
                {
                    yield return ModKitPlay.WalkTo(SpoolUtil.Pos(hurt), "to PATCH the " + Short(SpoolUtil.BeingName(hurt)));
                    yield return Wait(2f);
                }
                int before = Me.Hero.Patches;
                float hp = SpoolUtil.Health(hurt);
                yield return ModKitPlay.Command(hm, "vessel", SpoolData.PatchName, true, Vector3.zero, hurt, () => Me.Hero.Patches > before);
                Log("PATCH on " + Short(SpoolUtil.BeingName(hurt)) + ": " + (ModKitPlay.LastOk ? "hp " + F(hp) + " -> " + F(SpoolUtil.Health(hurt)) : "not done, last '" + Me.Hero.LastPatch + "'") + "; Stash " + Me.Stash);
                if (ModKitPlay.LastOk) patches++;
                yield break;
            }
        }
        // A Thread Head carrying 15 or more, without Threaded Delivery: RETURN.
        foreach (SpoolBuilding b in Mine())
        {
            SpoolHeadBrain h = b as SpoolHeadBrain;
            if (h == null || h.Has("Delivery") || h.Carried < 15 || h.Returning) continue;
            int before = h.Deliveries;
            yield return ModKitPlay.Command(h.Master, "Thread Head", SpoolData.ReturnName, false, Vector3.zero, null, () => h.Returning || h.Deliveries > before);
            if (ModKitPlay.LastOk) returns++;
            yield break;
        }
        // A Thread Head with Patchwork Job and MEND off: switch it on.
        foreach (SpoolBuilding b in Mine())
        {
            SpoolHeadBrain h = b as SpoolHeadBrain;
            if (h == null || !h.Has("Patchwork") || h.Mending) continue;
            yield return ModKitPlay.Command(h.Master, "Thread Head", SpoolData.MendName, false, Vector3.zero, null, () => h.Mending);
            if (ModKitPlay.LastOk) mends++;
            yield break;
        }
        // Once a map, from Tide 2: move a Thread Head's spot (SPOT) two tiles along its lane.
        if (!spotMovedFlag && tide >= 2)
        {
            foreach (SpoolBuilding b in Mine())
            {
                SpoolHeadBrain h = b as SpoolHeadBrain;
                if (h == null) continue;
                spotMovedFlag = true;
                Vector3 old = h.Spot;
                Vector3 to = ModKitTest.Spot(h.Spot, Being(SpoolData.HeadToken), ModKitPlay.Team, 2f, 3.5f, new List<Vector3>(used), "spot");
                if (float.IsNaN(to.x)) to = h.Spot + Vector3.right * 2f;
                yield return ModKitPlay.Command(h.Master, "Thread Head", SpoolData.SpotName, true, to, null, () => SpoolUtil.Flat(h.Spot, old) > 0.5f);
                if (ModKitPlay.LastOk) spots++;
                yield break;
            }
        }
        // Until Tide 2 Spool holds the core's doorstep and fights (round P8 lost Purlest in Tide 1 with Spool off fetching).
        if (tide <= 1)
        {
            Vector3 door = ModKitPlay.LaneTile(0, 2);
            if (!float.IsNaN(door.x) && SpoolUtil.Flat(SpoolUtil.Pos(hm), door) > 2.5f)
                yield return ModKitPlay.WalkTo(door, "to hold the core's doorstep");
            yield break;
        }
#if SPOOL_SMART
        // Thread Heads fetch the Thread on these runs: a walk through the virtual hand stalls a long run (seen on the PC).
        yield break;
#endif
        // Thread lying about within 7 tiles and room to carry: walk Spool to the biggest tuft; full, walk it home.
        if (Me.Carried >= 12 || (Me.Carried > 0 && SpoolServer.Drops.Count == 0))
        {
            if (Me.Home != null && SpoolUtil.Flat(SpoolUtil.Pos(hm), Me.Home.Pos) > SpoolData.DropOff)
            {
                int before = SpoolServer.Delivered;
                yield return ModKitPlay.WalkTo(Me.Home.Pos, "to hand in " + Me.Carried + " Thread");
                float t = 0f;
                while (t < 10f && SpoolServer.Delivered == before) { t += Time.deltaTime; yield return null; }
                Log("hand-in: " + (SpoolServer.Delivered > before ? "delivered " + (SpoolServer.Delivered - before) : "nothing delivered yet") + "; Stash " + Me.Stash + ", carried " + Me.Carried);
                walks++;
            }
            yield break;
        }
        SpoolDrop best = null;
        float bestD = 7f;
        foreach (SpoolDrop d in SpoolServer.Drops)
        {
            float dist = SpoolUtil.Flat(d.Pos, SpoolUtil.Pos(hm));
            if (dist < bestD && ModKitPlay.Foes().Count < 8) { bestD = dist; best = d; }
        }
        if (best != null && Me.Carried < Me.CarryCap)
        {
            int before = Me.Carried;
            yield return ModKitPlay.WalkTo(best.Pos, "to pick up " + best.Amount + " Thread");
            float t = 0f;
            while (t < 6f && Me.Carried == before) { t += Time.deltaTime; yield return null; }
            Log("pick-up: carried " + before + " -> " + Me.Carried);
            walks++;
        }
    }

    // A lane tile within the Tin's reach (8), a few tiles in front of the core it guards, that the Tin itself would take
    // (its own Reachable: round P7 aimed a second Tin at the first Tin's tile twenty times over).
    static readonly Dictionary<SpoolTinBrain, int> targetTries = new Dictionary<SpoolTinBrain, int>();
    static Vector3 TargetTile(SpoolTinBrain t)
    {
        List<KeyValuePair<float, Vector3>> c = new List<KeyValuePair<float, Vector3>>();
        foreach (List<Vector3> lane in ModKitPlay.Lanes)
            for (int i = 0; i < lane.Count; i++)
            {
                float d = SpoolUtil.Flat(lane[i], t.Pos);
                if (d > t.Reach - 1f || d < 2f) continue;
                int back = lane.Count - 1 - i;
#if SPOOL_SMART
                c.Add(new KeyValuePair<float, Vector3>(Mathf.Abs(back - 3) + d * 0.2f, lane[i]));   // two tiles behind the wall, on the core's side
#else
                c.Add(new KeyValuePair<float, Vector3>(Mathf.Abs(back - 6) + d * 0.2f, lane[i]));
#endif
            }
        c.Sort((a, b) => a.Key.CompareTo(b.Key));
        // A try that missed (the aim can land on a neighbour, round P8) moves on to the next candidate.
        int skip;
        targetTries.TryGetValue(t, out skip);
        foreach (KeyValuePair<float, Vector3> kv in c)
        {
            string why;
            if (t.Reachable(kv.Value, out why) == null) continue;
            if (skip-- > 0) continue;
            return kv.Value;
        }
        return new Vector3(float.NaN, 0f, 0f);
    }
    // One decision, the way a careful player spends: the Stash, a Tin, a Cushion on the lane, a Loom at ore, a Thread
    // Head out on the lane, then more; then upgrades along each building's route, cheapest tier first.
    static IEnumerator Think(int tide)
    {
        yield return ModKitPlay.Cancel();
        if (Me.Home == null)
        {
            Vector3 at = StashSpot();
            if (!float.IsNaN(at.x))
                yield return Build(SpoolData.Stash, at, Built(SpoolData.Stash) == 0 ? "the first Stash" : "a new Stash");
            yield break;
        }
        int tins = Count(SpoolData.Tin), cushions = Count(SpoolData.Cushion), looms = Count(SpoolData.Loom), heads = Count(SpoolData.Head);
        int wantTins = plan.Money ? 2 : (tide >= 3 ? 2 : 1);
        int wantCushions = plan.Money ? 3 : Mathf.Min(1 + tide / 2, 3);
#if SPOOL_SMART
        wantCushions = 3;
        wantTins = tide >= 5 ? 3 : tide >= 2 ? 2 : 1;
#if SPOOL_TINFIRST
        // A Tin before the wall: a wall first loses Tide 1 (seen on the PC).
        if (tins < 1) { if (ModKitPlay.Afford(Def(SpoolData.Tin).Costs, 0f)) { Vector3 first = ModKitPlay.LaneSpot(Being(SpoolData.Tin), 0, used, 2f); if (!float.IsNaN(first.x)) yield return Build(SpoolData.Tin, first, "the first Tin"); } yield break; }
#endif
        if (cushions < wantCushions && ModKitPlay.Afford(Def(SpoolData.Cushion).Costs, 0f))
        {
            Vector3 wall = CushionSpot(cushions);
            if (!float.IsNaN(wall.x))
                yield return Build(SpoolData.Cushion, wall, "the wall, tide " + tide);
            yield break;
        }
#endif
        // Round P7: a Loom at Tide 0 walked Spool 10 tiles off the core while the first Daggers came; a Loom waits a Tide.
        int wantLooms = plan.Money ? 2 : (tide >= 3 ? 2 : tide >= 1 ? 1 : 0);
        int wantHeads = plan.Money ? 2 : (tide >= 2 ? 2 : 1);
        string pick = "";
        if (tins < 1 && ModKitPlay.Afford(Def(SpoolData.Tin).Costs, 0f)) pick = SpoolData.Tin;
        else if (cushions < 1 && ModKitPlay.Afford(Def(SpoolData.Cushion).Costs, 0f)) pick = SpoolData.Cushion;
        else if (heads < 1 && ModKitPlay.Afford(Def(SpoolData.Head).Costs, 0f)) pick = SpoolData.Head;
        else if (looms < wantLooms && ModKitPlay.Afford(Def(SpoolData.Loom).Costs, 0f)) pick = SpoolData.Loom;
        else if (tins < wantTins && ModKitPlay.Afford(Def(SpoolData.Tin).Costs, 0f)) pick = SpoolData.Tin;
        else if (cushions < wantCushions && ModKitPlay.Afford(Def(SpoolData.Cushion).Costs, 0f)) pick = SpoolData.Cushion;
        else if (heads < wantHeads && ModKitPlay.Afford(Def(SpoolData.Head).Costs, 0f)) pick = SpoolData.Head;
        if (pick != "")
        {
            int n = Built(pick);
            Vector3 at;
            if (pick == SpoolData.Loom) at = ModKitPlay.OreSpot(Being(SpoolData.Loom), n, 2.5f, used);
            else if (pick == SpoolData.Head) at = HeadSpot(n);
            else if (pick == SpoolData.Cushion) at = CushionSpot(n);
            else at = ModKitPlay.LaneSpot(Being(pick), n, used, 2f);
            if (!float.IsNaN(at.x))
                yield return Build(pick, at, "tide " + tide);
            yield break;
        }
        SpoolBuilding bestB = null;
        SpoolData.Upg bestU = null;
        foreach (SpoolBuilding b in Mine())
        {
            SpoolData.Upg u = NextUpgrade(b);
            if (u == null || !ModKitPlay.Afford(u.Costs, 0f)) continue;
            if (bestU == null || u.Tier < bestU.Tier) { bestB = b; bestU = u; }
        }
        if (bestU != null)
        {
            SpoolBuilding b = bestB;
            SpoolData.Upg u = bestU;
            yield return ModKitPlay.Upgrade(b.Master, u.Id, u.Display, () => b.Has(u.Key), Short(b.Def.Id));
            if (ModKitPlay.LastOk && (u.Tier >= 3 || ModKitPlay.UiUpgrades <= 2))
                yield return ModKitPlay.Capture("upgraded_" + u.Key);
        }
    }

    static SpoolData.Upg NextUpgrade(SpoolBuilding b)
    {
        string[][] routes;
        if (b.Def.Upgrades == null || !Routes.TryGetValue(b.Def.Id, out routes)) return null;
        int r;
        if (!route.TryGetValue(b, out r)) { r = 0; route[b] = 0; }
        foreach (string k in routes[r % routes.Length])
        {
            if (b.Has(k)) continue;
            foreach (SpoolData.Upg u in b.Def.Upgrades)
                if (u.Key == k) return u;
            return null;
        }
        return null;
    }

    // At the end: sell a Stitched that is walking about, and a Thread Head, through SELL; the refunds are logged.
    static IEnumerator SellSome()
    {
        List<SpoolUnitBrain> st = Units(SpoolData.Stitched);
        if (st.Count == 0) st = Units(SpoolData.Dart);
        if (st.Count > 0)
        {
            SpoolUnitBrain u = st[0];
            int stash = Me.Stash;
            yield return ModKitPlay.Sell(u.Master, Short(u.Def.Id));
            if (ModKitPlay.LastOk) { stitchedSold++; Log("sold Crafted: Stash " + stash + " -> " + Me.Stash + " (refund " + (Me.Stash - stash) + " Thread)"); }
        }
        foreach (SpoolBuilding b in Mine())
            if (b is SpoolHeadBrain)
            {
                int carried = ((SpoolHeadBrain)b).Carried;
                yield return ModKitPlay.Sell(b.Master, "Thread Head (carrying " + carried + ")");
                if (ModKitPlay.LastOk) headsSold++;
                break;
            }
    }

    static IEnumerator CloseUps(string name)
    {
#if SPOOL_SHOTS
        List<string> kinds = new List<string>();
        int shown = 0;
        foreach (SpoolBuilding b in Mine())
        {
            if (kinds.Contains(b.Def.Id) || shown >= 3) continue;
            kinds.Add(b.Def.Id);
            shown++;
            yield return SpoolShots.Close("play_" + name + "_" + Short(b.Def.Id), b.Pos, 5f);
        }
        List<SpoolUnitBrain> crafted = Units(null);
        if (crafted.Count > 0)
            yield return SpoolShots.Close("play_" + name + "_crafted", SpoolUtil.Pos(crafted[0].Master), 3.5f);
        if (SpoolUtil.Alive(hm))
            yield return SpoolShots.Close("play_" + name + "_vessel", SpoolUtil.Pos(hm), 4f);
#else
        yield break;
#endif
    }
}
#endif
