using Unity.Netcode;

// The in-match probes of docs/15 §11, steps 2-8, on the host.
public static class RogueLabMatch
{
    public static bool freeze, forcing;
    static Game_Mapvars map1, last;
    static int warnings;

    public static void Tick()
    {
        if (RogueLabRun.Phase == 6 && Time.realtimeSinceStartup - RogueLabRun.Chained > RogueLabData.TourTimeout)
        {
            // Checked before the match test: a map that hangs while loading has no Game_Mapvars (round 2's Special_Custom).
            RogueLabProbe.Check("tour map " + RogueLabRun.Loading + " starts", false, "no vessel in the match " + RogueLabUtil.F(RogueLabData.TourTimeout) + " s after the chain; the tour stops here");
            RogueLabRun.Done();
            return;
        }
        Game_Mapvars vars = Game_Mapvars.Instance;
        if (vars == null || !RogueLabUtil.IsServer)
            return;
        if (forcing)
            vars.Time_Wave.Value = -1f;
        else if (freeze && vars.Time_Wave.Value < 600f && !InTide())
            vars.Time_Wave.Value = 900f;
        bool started = Game_Rounds.Instance != null && vars.Wave.Value >= 0 && RogueLabUtil.MainHero() != null;
        if (RogueLabRun.Phase == 2 && started)
        {
            RogueLabRun.Phase = 3;
            RogueLabMod.Instance.StartCoroutine(Map1());
        }
        if (RogueLabRun.Phase == 4 && started && vars != map1)
        {
            RogueLabRun.Phase = 5;
            last = vars;
            RogueLabMod.Instance.StartCoroutine(Map2());
        }
        if (RogueLabRun.Phase == 6 && started && vars != last)
        {
            RogueLabRun.Phase = 7;
            last = vars;
            RogueLabMod.Instance.StartCoroutine(TourMap());
        }
    }

    static IEnumerator Map1()
    {
        freeze = true;
        RogueLabRun.Map = 1;
        map1 = Game_Mapvars.Instance;
        yield return new WaitForSeconds(3f);
        RogueLabLog.Test("map 1: " + Tds_Multiplayerinfo.Instance.Gameplan.Value + " on " + Tds_Multiplayerinfo.Instance.Map.Value);
        Guard("mode check", ModeCheck);
        if (RogueLabData.Round == 4)
        {
            // Round 4: the mechanics of the 2026-10-09 scope (08_Round4.cs), then leave as a win and stop: no map 2, no tour.
            yield return RogueLabRound4.Map1();
            yield return Win();
            RogueLabRun.Done();
            yield break;
        }
        Guard("Cardinal check", CardinalCheck);
        RogueLabRun.ObjectId = RogueLabMod.Instance.gameObject.GetInstanceID();
        RogueLabRun.PureAura[RogueLabUtil.LocalId] = 7f;
        try { RogueLabEnemies.Dump("match"); } catch (Exception e) { RogueLabLog.Test("INFO enemy dump failed: " + e); }
        yield return Grant();
        yield return FKey();
        Guard("second prefab", RogueLabRound2.SecondPrefab);
        Guard("hard setting", () => RogueLabRound2.Treated("match"));
        yield return RogueLabRound2.Swarm(PlayerCore());
        yield return Tides();
        yield return Leave();
    }

    static IEnumerator Map2()
    {
        freeze = true;
        RogueLabRun.Map++;
        yield return new WaitForSeconds(3f);
        float aura;
        RogueLabProbe.Check("chained into map 2 without the boat", RogueLabRun.Map == 2 && Tds_Multiplayerinfo.Instance.Gameplan.Value.ToString() == RogueLabBoot.Twin(RogueLabRun.NextPlan),
            Tds_Multiplayerinfo.Instance.Gameplan.Value + " on " + Tds_Multiplayerinfo.Instance.Map.Value);
        RogueLabProbe.Check("run state survived the scene change", RogueLabRun.PureAura.TryGetValue(RogueLabUtil.LocalId, out aura) && aura == 7f
            && RogueLabMod.Instance != null && RogueLabMod.Instance.gameObject.GetInstanceID() == RogueLabRun.ObjectId, "Pure Aura " + RogueLabUtil.F(aura) + ", same mod object " + (RogueLabMod.Instance != null && RogueLabMod.Instance.gameObject.GetInstanceID() == RogueLabRun.ObjectId));
        Guard("mode check", ModeCheck);
        Guard("Cardinal check", CardinalCheck);
        yield return RogueLabRound2.Prompt8();   // round 3: replaces round 1's three-card prompt
        List<string> played = new List<string>();
        played.Add(RogueLabRun.NextPlan);
        played.Add(RogueLabRun.First);
        RogueLabRound2.PlanTour(played);
        yield return NextTourMap();
    }

    // A4: the tour, one base map after another, each left as a win between Tides as soon as it has started.
    static IEnumerator TourMap()
    {
        freeze = true;
        RogueLabRun.Map++;
        yield return new WaitForSeconds(3f);
        try { RogueLabRound2.TourMap(RogueLabRun.Loading, Time.realtimeSinceStartup - RogueLabRun.Chained, warnings); }
        catch (Exception e) { RogueLabLog.Test("TOUR " + RogueLabRun.Loading + " check threw " + e.Message); }
        yield return NextTourMap();
    }

    static IEnumerator NextTourMap()
    {
        warnings = RogueLabRound2.ModeWarnings;
        if (RogueLabRound2.Tour.Count == 0)
        {
            RogueLabProbe.Info("map tour", "done after " + RogueLabRun.Map + " maps in this run");
            freeze = false;
            RogueLabRun.Done();
            yield break;
        }
        string next = RogueLabRound2.Tour[0];
        RogueLabRound2.Tour.RemoveAt(0);
        yield return Win();
        yield return RogueLabRun.Chain(next, 6);
    }

    public static void Guard(string name, Action probe)
    {
        try { probe(); }
        catch (Exception e) { RogueLabProbe.Check(name, false, e.ToString()); }
    }

    // docs/15 §2: the match plays the mod's mode with the stock script.
    public static void ModeCheck()
    {
        Game_Mapvars vars = Game_Mapvars.Instance;
        Mode_Normal script = Game_Rounds.Instance.GetComponent<Mode_Normal>();
        RogueLabProbe.Check("map " + RogueLabRun.Map + " plays the mod mode", vars.Mode == RogueLabBoot.ModeObject && script != null,
            "mode " + (vars.Mode != null ? vars.Mode.Objectname : "null") + ", entry " + (vars.Modeentry != null ? vars.Modeentry.Objectname : "null") + ", Gameplan "
            + (vars.Gameplan != null ? vars.Gameplan.Objectname : "null") + ", Mode_Normal script " + (script != null) + ", table " + (vars.Mode != null ? vars.Mode.Enemies.Wave.Count : -1));
    }

    // docs/15 §7: the Cardinal equipped on the boat reached the match.
    static void CardinalCheck()
    {
        Artifactentryobject mine = Tds_Masterdatabase.Instance.Getitem("Artifact", RogueLabData.Cardinal) as Artifactentryobject;
        List<string> loaded = new List<string>();
        foreach (Artifactobject a in Game_Mapvars.Instance.Artifactobjects) loaded.Add(a != null ? a.Objectname : "null");
        string want = mine != null && mine.Artifactloads != null ? mine.Artifactloads.Objectname : "?";
        List<string> equipped = new List<string>();
        foreach (FixedString64Bytes n in Tds_Multiplayerinfo.Instance.Artifacts) equipped.Add(n.ToString());
        RogueLabProbe.Check("map " + RogueLabRun.Map + " has the mod Cardinal from the lobby", equipped.Contains(RogueLabData.Cardinal) && loaded.Contains(want),
            "equipped [" + string.Join(", ", equipped.ToArray()) + "], mixed '" + Tds_Multiplayerinfo.Instance.Mixedart.Value + "', artifacts loaded [" + string.Join(", ", loaded.ToArray()) + "], want " + want);
    }

    public static Entity_Master PlayerCore()
    {
        List<Entity_Master> cores;
        if (Game_Entity.Instance == null || !Game_Entity.Instance.Entityclienttags.TryGetValue("Core", out cores))
            return null;
        foreach (Entity_Master e in cores)
            if (RogueLabUtil.Alive(e) && e.Entitydata.Value.Team != 0 && !e.Taglistserver.Contains("Subcore"))
                return e;
        return null;
    }

    // docs/15 §7: a Cardinal granted mid-match, the way the game applies one at the start.
    static IEnumerator Grant()
    {
        Artifactentryobject entry = RogueLabBoot.GrantArtifact != "" ? Tds_Masterdatabase.Instance.Getitem("Artifact", RogueLabBoot.GrantArtifact) as Artifactentryobject : null;
        Entity_Master core = PlayerCore();
        if (entry == null || core == null) { RogueLabProbe.Setup("Cardinal granted mid-match", "entry " + (entry != null) + ", core " + (core != null)); yield break; }
        Bl_Address.Loadgroup group = Bl_Address.Load(entry.Artifactloads.Objectname);
        float t = 0f;
        while (group != null && !group.Loaded && t < 60f) { t += Time.deltaTime; yield return null; }
        Artifactobject art = Bl_Address.Getload("Artifact", entry.Artifactloads.Objectname) as Artifactobject;
        if (art == null) { RogueLabProbe.Setup("Cardinal granted mid-match", "no Artifactobject " + entry.Artifactloads.Objectname); yield break; }
        List<string> ups = new List<string>();
        foreach (Upgradeproperties up in art.Coreupgrades)
        {
            Helper_Entitytools.Upgrade(core, up, Helper_Entitytools.Upgradecheck.Forced, art);
            ups.Add(up.Objectname);
        }
        yield return new WaitForSeconds(1f);
        int applied = 0;
        foreach (Upgradeproperties up in art.Coreupgrades) if (core.Upgradelistserver.ContainsKey(up.Objectname)) applied++;
        RogueLabProbe.Check("Cardinal granted mid-match", applied == art.Coreupgrades.Count && applied > 0, RogueLabBoot.GrantArtifact + ": " + applied + "/" + art.Coreupgrades.Count
            + " core upgrades on the core [" + string.Join(", ", ups.ToArray()) + "]; the HUD was not told");
    }

    // docs/15 §9: after the vessel respawns, F (Game_Selection.Onfocus) picks a Hero-tagged minion. Reproduce with
    // Boutbro, then apply the fix (main vessel first in the tag list, and a Focus handler after the game's).
    static IEnumerator FKey()
    {
        Entity_Master hero = RogueLabUtil.MainHero();
        Beingobject bro = RogueLabUtil.Load<Beingobject>("Mob", RogueLabData.Bro);
        Entity_Master core = PlayerCore();
        if (hero == null || bro == null || core == null || Game_Selection.Instance == null)
        {
            RogueLabProbe.Setup("F key", "hero " + (hero != null) + ", Boutbro " + (bro != null) + ", core " + (core != null) + ", selection " + (Game_Selection.Instance != null));
            yield break;
        }
        RogueLabProbe.Info("Boutbro data tags", string.Join(",", bro.Entity.Tags.ToArray()));
        Genitem tile = null;
        Link_Being probe = new Link_Being { Being = bro, Entity = bro.Entity, Block = bro.Block, Team = hero.Entitydata.Value.Team };
        foreach (Genitem g in ModKitTest.Ground(hero.Entityposition.Value, 4f, 1, 1))
        {
            Genitem st = null;
            try { st = Helper_Gridtools.Checkifstandable(g, probe); } catch (Exception) { }
            if (st != null && ModKitTest.Flat(g.Position, hero.Entityposition.Value) > 1f) { tile = g; break; }
        }
        if (tile == null) { RogueLabProbe.Setup("F key", "no tile for Boutbro near the vessel"); yield break; }
        Link_Spawn spawn = Helper_Entitytools.Createlinkspawnparent(core.Beingserver);
        spawn.Being = bro; spawn.Genadd = tile; spawn.Skin = ""; spawn.Spawnstyle = Link_Spawn.Spawnstyles.Forced;
        spawn.Owner = RogueLabUtil.LocalId; spawn.Team = hero.Entitydata.Value.Team;
        Link_Being made = null;
        try { made = Helper_Entitytools.Spawn(spawn, tile.Position, core.Beingserver, true, null); }
        catch (Exception e) { RogueLabProbe.Setup("F key", "Boutbro spawn threw " + e.Message); yield break; }
        yield return new WaitForSeconds(2f);
        Entity_Master minion = made != null ? made.Worldobject : null;
        RogueLabProbe.Info("F list before the death", RogueLabUtil.Names(RogueLabUtil.Heroes()) + "; Boutbro spawned " + (minion != null)
            + (minion != null ? ", its owner " + RogueLabUtil.Owner(minion) + " (local " + RogueLabUtil.LocalId + "), tags [" + string.Join(",", minion.Taglistserver.ToArray()) + "]" : ""));

        Game_Entity.Instance.Despawn(hero.Beingserver, true, "RogueLab");
        Entity_Master again = null;
        float t = 0f;
        while (t < 120f)
        {
            again = RogueLabUtil.MainHero();
            if (again != null && again != hero) break;
            t += Time.deltaTime;
            yield return null;
        }
        if (again == null || again == hero) { RogueLabProbe.Setup("F key", "the vessel did not respawn within 120 s"); yield break; }
        yield return new WaitForSeconds(2f);
        RogueLabProbe.Info("F list after the respawn", RogueLabUtil.Names(RogueLabUtil.Heroes()) + " after " + RogueLabUtil.F(t) + " s");

        Game_Selection sel = Game_Selection.Instance;
        string before = Focus(sel, null);
        string afterMinion = minion != null ? Focus(sel, minion) : "-";
        bool bug = before != RogueLabUtil.BeingName(again) || (minion != null && afterMinion != RogueLabUtil.BeingName(again));
        RogueLabProbe.Info("F key without the fix", "nothing selected -> " + before + ", Boutbro selected -> " + afterMinion + "; bug reproduced " + bug);

        RogueLabFocusFix.Install();
        RogueLabFocusFix.Respawned = true;
        string fixedNone = Press(sel, null);
        RogueLabFocusFix.Respawned = true;
        string fixedMinion = minion != null ? Press(sel, minion) : RogueLabUtil.BeingName(again);
        RogueLabProbe.Check("F key selects the vessel after a respawn, with the fix", fixedNone == RogueLabUtil.BeingName(again) && fixedMinion == RogueLabUtil.BeingName(again),
            "nothing selected -> " + fixedNone + ", Boutbro selected -> " + fixedMinion + ", list " + RogueLabUtil.Names(RogueLabUtil.Heroes()));
        if (minion != null) Game_Entity.Instance.Despawn(minion.Beingserver, false, "RogueLab");
    }

    // The game's Focus handler alone, from a given selection.
    static string Focus(Game_Selection sel, Entity_Master from)
    {
        sel.Selected = from;
        sel.Onfocus("Onpressed");
        return RogueLabUtil.BeingName(sel.Selected);
    }

    // The whole Focus input as the F key fires it: the game's handler, then the fix.
    static string Press(Game_Selection sel, Entity_Master from)
    {
        sel.Selected = from;
        Blockage.Bl_Inputs.Actions["Focus"].Function("Onpressed");
        return RogueLabUtil.BeingName(sel.Selected);
    }

    public static bool InTide()
    {
        Game_Mapvars vars = Game_Mapvars.Instance;
        int e = vars.Entry.Value;
        List<Wavesobject.Waves> table = vars.Mode != null ? vars.Mode.Enemies.Wave : null;
        return table != null && e >= 1 && e <= table.Count && table[e - 1].Wave;
    }

    static float CoreTides()
    {
        Entity_Master core = ModKitTest.Core();
        Link_Stats s;
        return core != null && core.Statsserver.TryGetValue("Healthresource", out s) ? s.Value : -99f;
    }

    public static void ClearFoes()
    {
        List<Entity_Master> foes;
        if (!Game_Entity.Instance.Entityclienttags.TryGetValue("Enemy", out foes)) return;
        foreach (Entity_Master e in new List<Entity_Master>(foes))
            if (RogueLabUtil.Alive(e) && e.Entitydata.Value.Team == 0 && !e.Taglistserver.Contains("Enemycore"))
                Game_Entity.Instance.Despawn(e.Beingserver, false, "RogueLab");
    }

    public static IEnumerator Until(Func<bool> done, float timeout)
    {
        float t = 0f;
        while (!done() && t < timeout) { t += Time.deltaTime; yield return null; }
    }

    // Ends the running Tide: its content keeps spawning until it has all been sent, so clear foes as they come and
    // hold the timer at zero until the next entry starts.
    public static IEnumerator EndTide(int entry)
    {
        float t = 0f, cleared = 0f;
        forcing = true;
        while (Game_Mapvars.Instance.Entry.Value <= entry && t < 240f)
        {
            t += Time.deltaTime;
            if (t - cleared > 1f) { cleared = t; ClearFoes(); }
            yield return null;
        }
        forcing = false;
    }

    // docs/15 §4: a bonus Tide by rewinding Entry during a break.
    static IEnumerator Tides()
    {
        Game_Mapvars vars = Game_Mapvars.Instance;
        freeze = false;
        forcing = true;
        yield return Until(() => vars.Wave.Value >= 1, 60f);
        forcing = false;
        int tideEntry = vars.Entry.Value;
        float coreAtTide1 = CoreTides();
        if (vars.Wave.Value < 1) { RogueLabProbe.Setup("bonus Tide", "Tide 1 did not start"); yield break; }
        yield return new WaitForSeconds(3f);
        yield return EndTide(tideEntry);
        freeze = true;
        yield return new WaitForSeconds(1f);
        if (InTide()) { RogueLabProbe.Setup("bonus Tide", "no break after Tide 1"); yield break; }
        vars.Entry.Value = tideEntry - 1;
        freeze = false;
        forcing = true;
        yield return Until(() => vars.Wave.Value >= 2, 30f);
        forcing = false;
        RogueLabProbe.Check("bonus Tide by rewinding Entry", vars.Wave.Value == 2 && vars.Entry.Value == tideEntry,
            "Wave " + vars.Wave.Value + ", entry " + vars.Entry.Value + " (Tide 1 was " + tideEntry + "), enemy core Tides left " + RogueLabUtil.F(coreAtTide1) + " -> " + RogueLabUtil.F(CoreTides()));
        yield return new WaitForSeconds(3f);
        yield return EndTide(tideEntry);
        freeze = true;
    }

    // docs/15 §4: leaving between Tides as a win, then the chain (docs/15 §3).
    static IEnumerator Leave()
    {
        yield return new WaitForSeconds(2f);
        if (InTide()) { RogueLabProbe.Setup("leave between Tides", "still in a Tide"); RogueLabRun.Done(); yield break; }
        yield return Win();
        RogueLabProbe.Check("leave between Tides as a win", Game_Mapvars.Instance.Gamecomplete.Value.ToString() == "Win", "Gamecomplete " + Game_Mapvars.Instance.Gamecomplete.Value + ", Wave " + Game_Mapvars.Instance.Wave.Value);
        yield return RogueLabRun.Chain(RogueLabRun.NextPlan, 4);
    }

    public static IEnumerator Win()
    {
        Mode_Normal mode = Game_Rounds.Instance.GetComponent<Mode_Normal>();
        mode.Finished = true;
        Tds_Multiplayerinfo.Instance.Gamestate.Value = new FixedString64Bytes("Win");
        Game_Mapvars.Instance.Gamecomplete.Value = new FixedString64Bytes("Win");
        Game_Rounds.Instance.Gamecompleted();
        freeze = false;
        yield return new WaitForSeconds(3f);
    }
}

// The F-key fix of docs/15 §9, as a client-side handler on the game's Focus input, after the game's own: when the
// local vessel has respawned since the last press, the press selects the vessel itself, and the vessel goes first in
// the Hero tag list so later presses cycle from it.
public static class RogueLabFocusFix
{
    public static bool Respawned;
    static bool installed;

    public static void Install()
    {
        if (installed) return;
        installed = true;
        Blockage.Input focus = Blockage.Bl_Inputs.Actions["Focus"];
        focus.Function = (Blockage.Input.Pressfunct)Delegate.Combine(focus.Function, new Blockage.Input.Pressfunct(After));
    }

    static void After(string action)
    {
        if (action != "Onpressed" || !Respawned) return;
        Respawned = false;
        Entity_Master hero = RogueLabUtil.MainHero();
        List<Entity_Master> list;
        if (hero == null || Game_Selection.Instance == null || !Game_Entity.Instance.Entityclienttags.TryGetValue("Hero", out list)) return;
        list.Remove(hero);
        list.Insert(0, hero);
        Game_Selection.Instance.Selected = hero;
        Game_Selection.Instance.Select();
    }
}
