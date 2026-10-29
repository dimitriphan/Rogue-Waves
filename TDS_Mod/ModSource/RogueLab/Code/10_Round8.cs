// Round 8 (0.6.0), map 1: the probes of the engine study (docs/reports/SCOPE_FIT_PLAN_2026-10-28.md §11, R1-R8) and
// the facts of docs/reports/ENGINE_STUDY_2026-10-28.md §14 that a solo host can see (N1-N5, N7, N13). Solo host,
// Boutman, Play with no Cardinal, as round 7; Smacky's group is loaded for the archers and walls the probes stand on.
// Every pack upgrade here is made at run time from a template and lists stock scripts by name, as pack data would.
//   break   R1 a stock timed buff on pack numbers (Barrel_Boost), R2 the same upgrade twice and a second asset (N1),
//           N2 two upgrades sharing one script, R8 an owner-filtered spawn hook, N13 Teamapply and owners,
//           R3 a Light stat fed by timed mods, R4 a death node with Initstats, R6 a held aura loop,
//           R5 a pack Cardinal on Vintage, N3 Coin with and without its Cardinal, N5 Melting's drained stats,
//           and the set-up of the Tide 1 probes
//   Tide 1  R7 Smackywall_Regen heals at the Tide change, N4 Coin's 30 / Totalwaves, N7 Chrono's starting speed
public static class RogueLabRound8
{
    const string Archer = "Tower_Smackyarcher", Wall = "Tower_Smackywall";
    const long Stranger = -1;                         // "no player", as map objects are owned: stands in for a teammate
    const float CoinRed = 100f;                       // N4: Red_Add, large next to Tide 1's own aura
    static readonly string[] Colours = { "Redaura", "Blueaura", "Yellowaura" };
    static readonly List<Vector3> used = new List<Vector3>();
    static readonly List<string> errors = new List<string>();
    static readonly Dictionary<string, UnityEngine.Object> carriers = new Dictionary<string, UnityEngine.Object>();
    static Upgradeproperties template, owned, coin;
    static Entity_Master hero, core, healer, hurt, chrono;
    static Artifactobject card;
    static float hurtAt;
    static bool watchErrors;

    public static IEnumerator Map1()
    {
        RogueLabMatch.freeze = true;
        yield return new WaitForSeconds(2f);
        hero = RogueLabUtil.MainHero();
        core = RogueLabMatch.PlayerCore();
        if (hero == null || core == null) { RogueLabProbe.Setup("round 8", "vessel " + (hero != null) + ", core " + (core != null)); yield break; }
        Application.logMessageReceived += OnLog;
        yield return Guarded("setup", Setup());
        if (template == null) { Application.logMessageReceived -= OnLog; yield break; }
        yield return Guarded("R1 R2 timed buff", Boost());
        yield return Guarded("N2 shared script", Shared());
        yield return Guarded("R8 owner hook", OwnerHook());
        yield return Guarded("N13 Teamapply", Teamapply());
        yield return Guarded("R3 Light stat", Light());
        yield return Guarded("R4 death node", DeathNode());
        yield return Guarded("R6 held aura", Aura());
        yield return Guarded("R5 N3 Cardinals", Cardinals());
        yield return Guarded("N5 Melting", Melting());
        yield return Guarded("Tide 1 set-up", TideSetup());
        yield return Guarded("Tide 1 probes", Tide1());
        Application.logMessageReceived -= OnLog;
    }

    static IEnumerator Guarded(string name, IEnumerator probe)
    {
        while (true)
        {
            object current;
            try
            {
                if (!probe.MoveNext()) yield break;
                current = probe.Current;
            }
            catch (Exception e) { RogueLabProbe.Check(name, false, e.ToString()); yield break; }
            yield return current;
        }
    }

    static void OnLog(string message, string stack, LogType type)
    {
        if (!watchErrors || (type != LogType.Exception && type != LogType.Error) || message.StartsWith("[RogueLab]")) return;
        if (errors.Count < 6) errors.Add(RogueLabUtil.Clip(message.Replace("\n", " ")) + " @ " + RogueLabUtil.Clip(stack.Replace("\n", " ")));
    }

    static IEnumerator Setup()
    {
        Heroentryobject smacky = RogueLabUtil.Heroentry("Hero_Smacky");
        string loaded = "Hero_Smacky missing";
        if (smacky != null && smacky.Heroloads != null)
        {
            Bl_Address.Loadgroup g = Bl_Address.Load(smacky.Heroloads.Objectname);
            float t = 0f;
            while (g != null && !g.Loaded && t < 90f) { t += Time.unscaledDeltaTime; yield return null; }
            loaded = "Hero_Smacky " + (g != null && g.Loaded) + " " + RogueLabUtil.F(t) + " s";
        }
        template = Bl_Address.Getload("Upgrade", "Hero_Boutman_Tooltip_Upgradeproperties") as Upgradeproperties;
        if (template == null) template = Bl_Address.Getload("Upgrade", "Tower_Smackyarcher_Range2_Upgradeobject") as Upgradeproperties;
        Give(1000f);
        Game_Mapvars vars = Game_Mapvars.Instance;
        RogueLabProbe.Info("round 8 setup", "group " + loaded + ", template " + (template != null ? template.Objectname : "none") + ", archer " + (Being(Archer) != null) + ", wall " + (Being(Wall) != null)
            + ", vessel " + RogueLabUtil.BeingName(hero) + ", Totalwaves " + (vars.Mode != null ? vars.Mode.Enemies.Totalwaves.ToString() : "-") + ", gain " + RogueLabUtil.F(vars.Resourcegainmultiplier) + ", wallet " + Wallet());
        if (template == null) RogueLabProbe.Setup("round 8", "no template upgrade");
    }

    // ---- R1 (rung B) and R2 / N1: Entity_Ai_Effect_Barrel_Boost on pack numbers, applied twice, then a second asset.
    static IEnumerator Boost()
    {
        Entity_Master t = Place(Archer, hero, hero.Entityposition.Value, 2.5f);
        if (t == null) { RogueLabProbe.Setup("R1 timed buff", "no archer"); yield break; }
        yield return new WaitForSeconds(2f);
        string script = "Entity_Ai_Effect_Barrel_Boost";
        // The fit plan's row names only Damage_Multi and Duration_Base; the script reads all five params by key.
        Upgradeproperties shortUp = Up("Boostshort", script, P("Damage_Multi", -0.3f, "Duration_Base", 5f));
        string thrown = Try(() => Helper_Entitytools.Upgrade(t, shortUp, Helper_Entitytools.Upgradecheck.Forced, null));
        Helper_Entitytools.Removeupgrade(t, shortUp);
        RogueLabProbe.Info("R1 Barrel_Boost with only Damage_Multi and Duration_Base", thrown == "" ? "no exception" : thrown);

        Dictionary<string, float> full = P("Health_Pmulti", 0f, "Speed_Multi", 0f, "Attackspeed_Multi", 0f, "Damage_Multi", -0.3f, "Duration_Base", 5f);
        Upgradeproperties a = Up("Boost", script, full), b = Up("Boost2", script, full);
        float d0 = Stat(t, "Damage");
        string e1 = Try(() => Helper_Entitytools.Upgrade(t, a, Helper_Entitytools.Upgradecheck.Forced, null));
        yield return new WaitForSeconds(0.5f);
        float d1 = Stat(t, "Damage");
        int ais1 = Count(t, script);
        Helper_Entitytools.Upgrade(t, a, Helper_Entitytools.Upgradecheck.Forced, null);
        yield return new WaitForSeconds(0.5f);
        float d2 = Stat(t, "Damage");
        int ais2 = Count(t, script);
        Helper_Entitytools.Upgrade(t, b, Helper_Entitytools.Upgradecheck.Forced, null);
        yield return new WaitForSeconds(0.5f);
        float d3 = Stat(t, "Damage");
        int ais3 = Count(t, script);
        yield return new WaitForSeconds(6f);
        float d4 = Stat(t, "Damage");
        bool gone = !t.Upgradelistserver.ContainsKey(a.Objectname) && !t.Upgradelistserver.ContainsKey(b.Objectname);
        RogueLabProbe.Check("R1 a stock timed buff runs on pack numbers: x0.7 for 5 s, then back and gone", e1 == "" && Near(d1, d0 * 0.7f) && Near(d4, d0) && gone,
            "archer Damage " + RogueLabUtil.F(d0) + " -> " + RogueLabUtil.F(d1) + " -> after 6.5 s " + RogueLabUtil.F(d4) + ", upgrades gone " + gone + (e1 != "" ? ", " + e1 : ""));
        RogueLabProbe.Check("R2 N1 the same upgrade applied twice changes nothing", Near(d2, d1) && ais2 == ais1,
            "Damage " + RogueLabUtil.F(d1) + " -> " + RogueLabUtil.F(d2) + ", " + script + " instances " + ais1 + " -> " + ais2);
        RogueLabProbe.Check("R2 a second asset with another name stacks", Near(d3, d0 * 0.4f) && ais3 == 2,
            "Damage " + RogueLabUtil.F(d3) + " (x" + RogueLabUtil.F(d3 / d0) + "), instances " + ais3);
    }

    // ---- N2: two upgrades listing the same script; removing one keeps both instances (only its Downgrade runs).
    static IEnumerator Shared()
    {
        Entity_Master t = Place(Archer, hero, hero.Entityposition.Value, 3f);
        Entity_Master w = Place(Wall, hero, hero.Entityposition.Value, 3.5f);
        if (t == null || w == null) { RogueLabProbe.Setup("N2 shared script", "archer " + (t != null) + ", wall " + (w != null)); yield break; }
        yield return new WaitForSeconds(2f);
        string script = "Entity_Ai_Effect_Smackywall_Bramble";
        Upgradeproperties a = Up("Thorna", script, P("Effect_Add", 1f)), b = Up("Thornb", script, P("Effect_Add", 2f));
        RogueLabProbe.Info("N2 the pricked wall", Pools(w));
        float l0 = Prick(w, t);
        Helper_Entitytools.Upgrade(t, a, Helper_Entitytools.Upgradecheck.Forced, null);
        float la = Prick(w, t);
        Helper_Entitytools.Upgrade(t, b, Helper_Entitytools.Upgradecheck.Forced, null);
        int n2 = Count(t, script);
        float lab = Prick(w, t);
        Helper_Entitytools.Removeupgrade(t, a);
        yield return null;
        int n1 = Count(t, script);
        float lb = Prick(w, t);
        Helper_Entitytools.Removeupgrade(t, b);
        yield return null;
        int n0 = Count(t, script);
        float lnone = Prick(w, t);
        string line = "wall lost " + RogueLabUtil.F(l0) + " with no thorns, " + RogueLabUtil.F(la) + " with A (1), " + RogueLabUtil.F(lab) + " with A and B (1+2), "
            + RogueLabUtil.F(lb) + " after A was removed, " + RogueLabUtil.F(lnone) + " after B; instances " + n2 + " -> " + n1 + " -> " + n0;
        RogueLabProbe.Check("N2 removing one of two upgrades that share a script keeps both instances running", n2 == 2 && n1 == 2 && n0 == 0 && Near(lb, lab), line);
    }

    // ---- R8: the pack's own spawn hook gives one player's new towers a -3 upgrade with stock thorns; a teammate's don't.
    static IEnumerator OwnerHook()
    {
        owned = Up("Owned", "Entity_Ai_Effect_Smackywall_Bramble", P("Effect_Add", 2f));
        Entity_Master w = Place(Wall, hero, hero.Entityposition.Value, 4.5f);   // before the hook, so the wall has no thorns
        Game_Entity.Instance.Onspawn += Hook;
        Entity_Master mine = Place(Archer, hero, hero.Entityposition.Value, 4f);
        Entity_Master theirs = PlaceFor(Archer, hero, Stranger, hero.Entityposition.Value, 4f);
        yield return new WaitForSeconds(2f);
        if (mine == null || theirs == null || w == null) { Game_Entity.Instance.Onspawn -= Hook; RogueLabProbe.Setup("R8 owner hook", "mine " + (mine != null) + ", theirs " + (theirs != null) + ", wall " + (w != null)); yield break; }
        bool hasMine = mine.Upgradelistserver.ContainsKey(owned.Objectname), hasTheirs = theirs.Upgradelistserver.ContainsKey(owned.Objectname);
        float pm = Prick(w, mine), pt = Prick(w, theirs);
        RogueLabProbe.Check("R8 an owner-filtered spawn hook reaches only that player's towers", hasMine && !hasTheirs && pm > pt,
            "mine (owner " + RogueLabUtil.Owner(mine) + ") has it " + hasMine + ", wall lost " + RogueLabUtil.F(pm) + "; the stranger's (owner " + RogueLabUtil.Owner(theirs) + ", parent " + RogueLabUtil.BeingName(theirs.Parent) + ") has it " + hasTheirs + ", wall lost " + RogueLabUtil.F(pt));
        Vector3 at = mine.Entityposition.Value;
        Helper_Entitytools.Despawn(mine.Beingserver, false, "RogueLab");
        used.Remove(at);
        yield return new WaitForSeconds(1f);
        Entity_Master again = Place(Archer, hero, at, 0f);
        yield return new WaitForSeconds(1.5f);
        RogueLabProbe.Check("R8 a tower built again gets it again", again != null && again.Upgradelistserver.ContainsKey(owned.Objectname), "rebuilt " + (again != null));
        Game_Entity.Instance.Onspawn -= Hook;
        if (RogueLabUtil.Alive(theirs)) Helper_Entitytools.Despawn(theirs.Beingserver, false, "RogueLab");
    }

    static void Hook(Entity_Master m)
    {
        if (m == null || owned == null || m.Beingserver == null || m.Beingserver.Entity == null || !m.Beingserver.Entity.Tags.Contains("Tower")) return;
        if (RogueLabUtil.Owner(m) == RogueLabUtil.LocalId && !m.Initupgradelistserver.ContainsKey(owned.Objectname))
            m.Initupgradelistserver.Add(owned.Objectname, owned);
    }

    // ---- N13: the stock "everything a vessel builds" Cardinal script checks only that the parent is tagged Hero.
    static IEnumerator Teamapply()
    {
        Upgradeproperties gift = Up("Teamgift", null, new Dictionary<string, float>());
        Upgradeproperties apply = Up("Teamapply", "Entity_Ai_Artifact_Teamapply", P(gift.Objectname, 1f));
        string thrown = Try(() => Helper_Entitytools.Upgrade(core, apply, Helper_Entitytools.Upgradecheck.Forced, null));
        Entity_Master mine = Place(Archer, hero, hero.Entityposition.Value, 5f);
        Entity_Master theirs = PlaceFor(Archer, hero, Stranger, hero.Entityposition.Value, 5f);
        yield return new WaitForSeconds(2f);
        bool m = mine != null && mine.Upgradelistserver.ContainsKey(gift.Objectname), s = theirs != null && theirs.Upgradelistserver.ContainsKey(gift.Objectname);
        RogueLabProbe.Check("N13 Teamapply reaches a building whose owner is another player", mine != null && theirs != null && m && s,
            "the local player's archer has it " + m + ", the stranger's (owner " + (theirs != null ? RogueLabUtil.Owner(theirs).ToString() : "-") + ", parent the vessel) has it " + s + (thrown != "" ? "; " + thrown : ""));
        Helper_Entitytools.Removeupgrade(core, apply);
        if (RogueLabUtil.Alive(theirs)) Helper_Entitytools.Despawn(theirs.Beingserver, false, "RogueLab");
        yield return new WaitForSeconds(0.5f);
        Entity_Master later = Place(Archer, hero, hero.Entityposition.Value, 5.5f);
        yield return new WaitForSeconds(2f);
        RogueLabProbe.Info("N13 after Teamapply's upgrade is removed", "a new archer still gets the gift: " + (later != null && later.Upgradelistserver.ContainsKey(gift.Objectname)) + " (its spawn handler is never removed)");
    }

    // ---- R3: a Light stat on a pack tower fed by timed Add mods. Each source replaces its own 1.5 s mod every second;
    // the naive form (add a new one every second, never replace) is logged too.
    static IEnumerator Light()
    {
        Entity_Master t = Place(Archer, hero, hero.Entityposition.Value, 3f);
        if (t == null) { RogueLabProbe.Setup("R3 Light stat", "no archer"); yield break; }
        yield return new WaitForSeconds(1.5f);
        Link_Stats light = new Link_Stats();
        light.Name = "Light";
        light.Value_Base = 0f;
        light.Value_Type = Statproperties.Stattype.Fixed;
        light.Display_Type = Statproperties.Displaytypes.Float;
        light.Display_Exposed = false;
        Helper_Entitytools.Addstat(t, light, 0f);
        if (!t.Statsserver.ContainsKey("Light")) { RogueLabProbe.Setup("R3 Light stat", "Addstat did not add it"); yield break; }
        Link_Modifier[] held = new Link_Modifier[2];
        List<string> samples = new List<string>();
        float both = -1f, one = -1f, none = -1f;
        for (int k = 0; k < 8; k++)
        {
            for (int s = 0; s < 2; s++)
            {
                if (s == 1 && k >= 4) continue;                       // source B leaves after 4 s
                if (held[s] != null && !held[s].Dead) held[s].Remove();
                held[s] = Helper_Entitytools.Addmod(t, "Light", 1f, Helper_Entitytools.Modtypes.Add, 1.5f);
            }
            yield return new WaitForSeconds(0.6f);
            float v = Stat(t, "Light");
            samples.Add(RogueLabUtil.F(v));
            if (k == 3) both = v;
            if (k == 7) one = v;
            yield return new WaitForSeconds(0.4f);
        }
        yield return new WaitForSeconds(1.5f);
        none = Stat(t, "Light");
        RogueLabProbe.Check("R3 a Light stat sums its sources and fades when they stop", Near(both, 2f) && Near(one, 1f) && Near(none, 0f),
            "two sources " + RogueLabUtil.F(both) + ", one " + RogueLabUtil.F(one) + ", 1.5 s after the last " + RogueLabUtil.F(none) + "; each second [" + string.Join(" ", samples.ToArray()) + "]");
        List<string> naive = new List<string>();
        for (int k = 0; k < 4; k++)
        {
            Helper_Entitytools.Addmod(t, "Light", 1f, Helper_Entitytools.Modtypes.Add, 1.5f);
            naive.Add(RogueLabUtil.F(Stat(t, "Light")));
            yield return new WaitForSeconds(0.6f);
            naive.Add(RogueLabUtil.F(Stat(t, "Light")));
            yield return new WaitForSeconds(0.4f);
        }
        yield return new WaitForSeconds(1.6f);
        RogueLabProbe.Info("R3 one source adding a 1.5 s mod every second without replacing it", "right after each add and 0.6 s later [" + string.Join(" ", naive.ToArray()) + "], then " + RogueLabUtil.F(Stat(t, "Light")));
    }

    // ---- R4: a node on Entity_Ai_Resource_Death with Initstats Healthresource 12 dies after 12 hits and pays once.
    static IEnumerator DeathNode()
    {
        Beingobject stock = null;
        foreach (string n in new[] { "Resource_Red", "Resource_Blue", "Resource_Yellow" }) if (stock == null) stock = Being(n);
        Beingobject death = Being("Resource_Death");
        string made = death != null ? "the stock Resource_Death" : "";
        if (death == null && stock != null)
        {
            // Not loaded on this map: a copy of a stock ore node whose ore brain is swapped for the death node's.
            death = RogueLabUtil.Clone(stock, "RogueLab_Deathnode");
            death.Entity = RogueLabUtil.Clone(stock.Entity, "RogueLab_Deathnode_Entity");
            death.Entity.Aiscripts = new List<UnityEngine.Object>();
            List<string> swapped = new List<string>();
            foreach (UnityEngine.Object o in stock.Entity.Aiscripts)
            {
                bool ore = o != null && o.name.StartsWith("Entity_Ai_Resource_");
                death.Entity.Aiscripts.Add(ore ? Carrier("Entity_Ai_Resource_Death") : o);
                if (o != null) swapped.Add(ore ? o.name + "->Entity_Ai_Resource_Death" : o.name);
            }
            Register("Mob", death);
            made = "a copy of " + stock.Objectname + " [" + string.Join(", ", swapped.ToArray()) + "]";
        }
        if (death == null) { RogueLabProbe.Setup("R4 death node", "no ore node loaded"); yield break; }
        Entity_Master stockNode = Named("Resource_");
        int team = stockNode != null ? stockNode.Entitydata.Value.Team : 1;
        Vector3 at = ModKitTest.Spot(hero.Entityposition.Value, death, team, 2f, 8f, used, "a death node");
        if (float.IsNaN(at.x)) { RogueLabProbe.Setup("R4 death node", "no tile"); yield break; }
        Link_Spawn s = new Link_Spawn();
        s.Being = death; s.Genadd = ModKitTest.Surface(at); s.Skin = ""; s.Spawnstyle = Link_Spawn.Spawnstyles.Forced; s.Team = team; s.Owner = -1;
        Link_Being lb = Helper_Entitytools.Spawn(s, at, null, false, P("Healthresource", 12f));
        Entity_Master node = lb != null ? lb.Worldobject : null;
        yield return new WaitForSeconds(1.5f);
        if (node == null) { RogueLabProbe.Setup("R4 death node", "spawn failed (" + made + ")"); yield break; }
        string start = Pools(node) + ", " + Ore(node);
        float[] w0 = Amounts();
        int hits = 0, pays = 0;
        float[] last = Amounts();
        while (RogueLabUtil.Alive(node) && hits < 20)
        {
            Helper_Entitytools.Interacthurt(hero, node, null);
            hits++;
            yield return new WaitForSeconds(0.3f);
            float[] now = Amounts();
            if (now[0] + now[1] + now[2] > last[0] + last[1] + last[2] + 0.01f) pays++;
            last = now;
        }
        yield return new WaitForSeconds(1f);
        float[] w1 = Amounts();
        if (w1[0] + w1[1] + w1[2] > last[0] + last[1] + last[2] + 0.01f) pays++;
        bool died = !RogueLabUtil.Alive(node);
        RogueLabProbe.Check("R4 a death node with Initstats Healthresource 12 dies after about 12 hits and pays once", died && hits >= 11 && hits <= 13 && pays == 1,
            made + ": " + start + "; died " + died + " after " + hits + " hits, paid " + pays + " times, wallet R" + RogueLabUtil.F(w1[0] - w0[0]) + " B" + RogueLabUtil.F(w1[1] - w0[1]) + " Y" + RogueLabUtil.F(w1[2] - w0[2]));
    }

    // ---- R6: a held aura loop (Cursed's shape): towers within 3 of a source get a held Damage Multi -0.3, removed
    // when they are out of range or the source is gone. The source here is a wall that is then removed. The loop skips
    // the source and forgets towers that died (round 8 ran without both and counted the dead source as still held).
    static IEnumerator Aura()
    {
        Entity_Master src = Place(Wall, hero, hero.Entityposition.Value, 6f);
        if (src == null) { RogueLabProbe.Setup("R6 held aura", "no source"); yield break; }
        Entity_Master near = Place(Archer, hero, src.Entityposition.Value, 1.5f);
        Entity_Master far = Place(Archer, hero, src.Entityposition.Value, 7f);
        yield return new WaitForSeconds(2f);
        if (near == null || far == null) { RogueLabProbe.Setup("R6 held aura", "near " + (near != null) + ", far " + (far != null)); yield break; }
        float dn = ModKitTest.Flat(near.Entityposition.Value, src.Entityposition.Value), df = ModKitTest.Flat(far.Entityposition.Value, src.Entityposition.Value);
        float n0 = Stat(near, "Damage"), f0 = Stat(far, "Damage");
        Dictionary<Entity_Master, Link_Modifier> held = new Dictionary<Entity_Master, Link_Modifier>();
        float n1 = -1f, f1 = -1f;
        for (int k = 0; k < 8; k++)
        {
            bool alive = RogueLabUtil.Alive(src);
            List<Entity_Master> towers;
            if (Game_Entity.Instance.Entitiesserver.TryGetValue("Tower", out towers))
                foreach (Entity_Master t in new List<Entity_Master>(towers))
                {
                    if (t == src || !RogueLabUtil.Alive(t) || RogueLabUtil.Owner(t) != RogueLabUtil.LocalId) continue;
                    bool inRange = alive && ModKitTest.Flat(t.Entityposition.Value, src.Entityposition.Value) <= 3f;
                    Link_Modifier m;
                    bool has = held.TryGetValue(t, out m);
                    if (inRange && !has) held[t] = Helper_Entitytools.Addmod(t, "Damage", -0.3f, Helper_Entitytools.Modtypes.Multi, 0f);
                    if (!inRange && has) { m.Remove(); held.Remove(t); }
                }
            foreach (Entity_Master t in new List<Entity_Master>(held.Keys))
                if (!RogueLabUtil.Alive(t)) held.Remove(t);
            yield return new WaitForSeconds(0.5f);
            if (k == 3) { n1 = Stat(near, "Damage"); f1 = Stat(far, "Damage"); Helper_Entitytools.Despawn(src.Beingserver, false, "RogueLab"); }
        }
        float n2 = Stat(near, "Damage");
        bool expectNear = dn <= 3f, expectFar = df <= 3f;
        RogueLabProbe.Check("R6 a held aura loop marks towers in range and lets them go when the source leaves", (expectNear ? Near(n1, n0 * 0.7f) : Near(n1, n0)) && (expectFar ? Near(f1, f0 * 0.7f) : Near(f1, f0)) && Near(n2, n0) && held.Count == 0,
            "near archer " + RogueLabUtil.F(dn) + " away: " + RogueLabUtil.F(n0) + " -> " + RogueLabUtil.F(n1) + " -> source gone " + RogueLabUtil.F(n2) + "; far archer " + RogueLabUtil.F(df) + " away: " + RogueLabUtil.F(f0) + " -> " + RogueLabUtil.F(f1) + "; still held " + held.Count);
    }

    // ---- R5: a pack Team Cardinal on Entity_Ai_Artifact_Vintage, granted mid-map with its Artifactobject; N3: the
    // stock Coin script throws when no Cardinal is passed, and runs when one is (it stays on for N4 at Tide 1).
    static IEnumerator Cardinals()
    {
        // The Cardinal object a lobby would carry; named like the stock Red Coin so the warning finds an icon. It is not
        // in Game_Mapvars.Artifactobjects, so Editartifact writes no HUD counter for it.
        card = ScriptableObject.CreateInstance<Artifactobject>();
        card.name = "Artifact_Redcoin";
        card.Objectname = "Artifact_Redcoin";
        card.Id = "artifact_redcoin-roguelab";
        Game_Mapvars vars = Game_Mapvars.Instance;
        Upgradeproperties vintage = Up("Vintage", "Entity_Ai_Artifact_Vintage", P("Timeinit_Base", 40f, "Time_Base", 20f));
        card.Coreupgrades.Add(vintage);
        watchErrors = true; errors.Clear();
        float last = vars.Time_Wave.Value, jump = 0f;
        string state = vars.Display.Value.Display_State.ToString();
        string thrown = Try(() => Helper_Entitytools.Upgrade(core, vintage, Helper_Entitytools.Upgradecheck.Forced, card));
        for (float t = 0f; t < 3f; t += Time.deltaTime)
        {
            float v = vars.Time_Wave.Value;
            if (v - last > jump && v - last < 200f) jump = v - last;
            last = v;
            yield return null;
        }
        watchErrors = false;
        RogueLabProbe.Check("R5 a pack Cardinal on the stock Vintage script, granted mid-map with its Artifactobject, lengthens the break", thrown == "" && Mathf.Abs(jump - 40f) < 1f && errors.Count == 0,
            "break state '" + state + "', Time_Wave jumped +" + RogueLabUtil.F(jump) + " (Timeinit_Base 40)" + (thrown != "" ? ", " + thrown : "") + ", errors " + (errors.Count == 0 ? "none" : string.Join(" | ", errors.ToArray())));
        Helper_Entitytools.Removeupgrade(core, vintage);

        Dictionary<string, float> zero = P("Red_Base", 0f, "Blue_Base", 0f, "Yellow_Base", 0f, "Red_Add", 0f, "Blue_Add", 0f, "Yellow_Add", 0f);
        Upgradeproperties coinNull = Up("Coinnull", "Entity_Ai_Artifact_Coin", zero);
        string bare = Try(() => Helper_Entitytools.Upgrade(core, coinNull, Helper_Entitytools.Upgradecheck.Forced, null));
        Helper_Entitytools.Removeupgrade(core, coinNull);
        coin = Up("Coin", "Entity_Ai_Artifact_Coin", P("Red_Base", 0f, "Blue_Base", 0f, "Yellow_Base", 0f, "Red_Add", CoinRed, "Blue_Add", 0f, "Yellow_Add", 0f));
        card.Coreupgrades.Add(coin);
        string carried = Try(() => Helper_Entitytools.Upgrade(core, coin, Helper_Entitytools.Upgradecheck.Forced, card));
        yield return new WaitForSeconds(0.5f);
        RogueLabProbe.Check("N3 the stock Coin script throws without its Cardinal and runs with it", bare.Contains("NullReference") && carried == "",
            "no Cardinal: " + (bare == "" ? "no exception" : bare) + "; with the Artifactobject: " + (carried == "" ? "no exception" : carried));
    }

    // ---- N5: Melting (Entity_Ai_Effect_Enemy_Weaken) on pack numbers, 3 s; its drained Damage stays after it ends.
    static IEnumerator Melting()
    {
        Entity_Master t = Place(Archer, hero, hero.Entityposition.Value, 3.5f);
        if (t == null) { RogueLabProbe.Setup("N5 Melting", "no archer"); yield break; }
        yield return new WaitForSeconds(1.5f);
        Upgradeproperties melt = Up("Melting", "Entity_Ai_Effect_Enemy_Weaken", P("Duration_Base", 3f, "Health_Drain", 0f, "Attackspeed_Drain", 0f, "Damage_Drain", -0.1f, "Speed_Drain", 0f));
        float d0 = Stat(t, "Damage");
        string thrown = Try(() => Helper_Entitytools.Upgrade(t, melt, Helper_Entitytools.Upgradecheck.Forced, null));
        yield return new WaitForSeconds(2.5f);
        float d1 = Stat(t, "Damage");
        bool on = t.Upgradelistserver.ContainsKey(melt.Objectname);
        yield return new WaitForSeconds(4.5f);
        float d2 = Stat(t, "Damage");
        bool off = !t.Upgradelistserver.ContainsKey(melt.Objectname) && Count(t, "Entity_Ai_Effect_Enemy_Weaken") == 0;
        RogueLabProbe.Check("N5 Melting's drained stats stay after it ends", thrown == "" && d1 < d0 - 0.01f && off && d2 < d0 - 0.01f,
            "archer Damage " + RogueLabUtil.F(d0) + " -> " + RogueLabUtil.F(d1) + " while on (" + on + ") -> " + RogueLabUtil.F(d2) + " after it ended (gone " + off + ")" + (thrown != "" ? ", " + thrown : ""));
    }

    // ---- Tide 1 set-up: R7's healer and hurt neighbour, N7's Chrono brain on a wall with the stats it reads.
    static IEnumerator TideSetup()
    {
        healer = Place(Archer, hero, hero.Entityposition.Value, 2f);
        yield return new WaitForSeconds(1.5f);
        if (healer != null)
        {
            hurt = Place(Archer, hero, healer.Entityposition.Value, 1.5f);
            yield return new WaitForSeconds(1.5f);
            Helper_Entitytools.Upgrade(healer, Up("Regen", "Entity_Ai_Effect_Smackywall_Regen", P("Range_Base", 3f, "Effect_Add", 5f)), Helper_Entitytools.Upgradecheck.Forced, null);
            if (hurt != null)
            {
                Helper_Entitytools.Interacthurt(hero, hurt, P("Damage", 10f, "Damagemulti", 1f));
                yield return new WaitForSeconds(0.5f);
                hurtAt = Pool(hurt);
                RogueLabProbe.Info("R7 set-up", "neighbour " + RogueLabUtil.F(ModKitTest.Flat(hurt.Entityposition.Value, healer.Entityposition.Value)) + " from the healer, hurt to " + Pools(hurt) + ", healer Level " + healer.Entitydata.Value.Level);
            }
        }
        chrono = Place(Wall, hero, hero.Entityposition.Value, 5f);
        yield return new WaitForSeconds(1.5f);
        if (chrono != null)
        {
            foreach (string n in new[] { "Quickspeed", "Doublespeed", "Superspeed" }) AddStat(chrono, n, 99f);
            AddStat(chrono, "Lingertime", 1f);
            string thrown = Try(() => Helper_Entitytools.Upgrade(chrono, Up("Chrono", "Entity_Ai_Main_Chrono", new Dictionary<string, float>()), Helper_Entitytools.Upgradecheck.Forced, null));
            if (thrown != "") RogueLabProbe.Info("N7 set-up", thrown);
        }
    }

    static IEnumerator Tide1()
    {
        Game_Mapvars vars = Game_Mapvars.Instance;
        float[] w0 = Amounts();
        float scale0 = vars.Time_Scale.Value;
        RogueLabMatch.freeze = false;
        RogueLabMatch.forcing = true;
        yield return RogueLabMatch.Until(() => vars.Wave.Value >= 1, 60f);
        RogueLabMatch.forcing = false;
        int entry = vars.Entry.Value;
        if (vars.Wave.Value < 1) { RogueLabProbe.Setup("Tide 1 probes", "Tide 1 did not start"); yield break; }
        yield return new WaitForSeconds(0.5f);
        float scaleLinger = vars.Time_Scale.Value;
        yield return new WaitForSecondsRealtime(2.5f);
        float scaleAfter = vars.Time_Scale.Value;
        float[] w1 = Amounts();

        if (hurt != null && healer != null)
            RogueLabProbe.Check("R7 a pack building on the stock Smackywall_Regen heals a hurt neighbour at the Tide change", Pool(hurt) > hurtAt + 0.01f,
                "neighbour's health layers " + RogueLabUtil.F(hurtAt) + " -> " + RogueLabUtil.F(Pool(hurt)) + " (" + Pools(hurt) + ")");
        else RogueLabProbe.Setup("R7 Smackywall_Regen", "healer " + (healer != null) + ", neighbour " + (hurt != null));

        int total = vars.Mode != null ? vars.Mode.Enemies.Totalwaves : 0;
        int factor = total > 0 ? 30 / total : 0;
        // Tide 1 pays its own aura too (round 7 and 8: about R+20 B+70 Y+70 here), so Red_Add is large and the
        // tolerance absolute; round 8 ran with Red_Add 7 and a relative 0.6, which let R+90 for +70 pass.
        float expect = CoinRed * vars.Wave.Value * factor * vars.Resourcegainmultiplier;
        RogueLabProbe.Check("N4 Coin pays Red_Add x Tide x (30 / Totalwaves) in whole numbers", total > 0 && Mathf.Abs(w1[0] - w0[0] - expect) <= 40f,
            "Totalwaves " + total + ", 30 / it = " + factor + ", Tide " + vars.Wave.Value + ", gain " + RogueLabUtil.F(vars.Resourcegainmultiplier) + ": expected Red +" + RogueLabUtil.F(expect)
            + ", wallet R+" + RogueLabUtil.F(w1[0] - w0[0]) + " B+" + RogueLabUtil.F(w1[1] - w0[1]) + " Y+" + RogueLabUtil.F(w1[2] - w0[2]) + " (other income shows in B and Y)");

        if (chrono != null)
            RogueLabProbe.Check("N7 the Chrono brain runs the map at x1.25 before its first step", Near(scaleAfter, 1.25f),
                "Time_Scale " + RogueLabUtil.F(scale0) + " in the break, " + RogueLabUtil.F(scaleLinger) + " 0.5 s into Tide 1, " + RogueLabUtil.F(scaleAfter) + " 3 s in (Lingertime 1, Quickspeed 99)");
        else RogueLabProbe.Setup("N7 Chrono", "no wall");

        if (chrono != null && RogueLabUtil.Alive(chrono)) Helper_Entitytools.Despawn(chrono.Beingserver, false, "RogueLab");
        vars.Time_Scale.Value = 1f;
        if (coin != null) Helper_Entitytools.Removeupgrade(core, coin);
        RogueLabMatch.ClearFoes();
        yield return RogueLabMatch.EndTide(entry);
        RogueLabMatch.freeze = true;
        yield return new WaitForSeconds(1f);
    }

    // ---------------------------------------------------------------- helpers

    // A pack upgrade made from the template: Priority -3, no costs, the named stock script, these params.
    static Upgradeproperties Up(string name, string script, Dictionary<string, float> ps)
    {
        Upgradeproperties u = RogueLabKitDirections.Make(template, "RogueLab8", name, "RogueLab " + name, "round 8 probe");
        u.name = "RogueLab8_" + name + "_Upgradeobject";
        u.Objectname = u.name;
        u.Id = RogueLabUtil.Clip(u.name.ToLowerInvariant() + "-roguelab");
        if (script != null) u.Aiscripts.Add(Carrier(script));
        foreach (KeyValuePair<string, float> p in ps)
        {
            u.Defaultparams[p.Key] = p.Value;
            u.Defaultparam.Add(new Upgradeproperties.Param { Name = p.Key, Value = p.Value });
        }
        Register("Upgrade", u);
        return u;
    }

    static Dictionary<string, float> P(params object[] kv)
    {
        Dictionary<string, float> d = new Dictionary<string, float>();
        for (int i = 0; i + 1 < kv.Length; i += 2) d[(string)kv[i]] = Convert.ToSingle(kv[i + 1]);
        return d;
    }

    // What the game's Upgrade reads from an Aiscripts entry is its name (Type.GetType), so a named carrier stands in.
    static UnityEngine.Object Carrier(string className)
    {
        UnityEngine.Object c;
        if (carriers.TryGetValue(className, out c) && c != null) return c;
        c = new TextAsset("");
        c.name = className;
        c.hideFlags = HideFlags.DontUnloadUnusedAsset;
        carriers[className] = c;
        return c;
    }

    static string Try(Action act)
    {
        try { act(); return ""; }
        catch (Exception e) { return e.GetType().Name + ": " + RogueLabUtil.Clip(e.Message.Replace("\n", " ")); }
    }

    static int Count(Entity_Master m, string type)
    {
        int n = 0;
        foreach (Entity_Ai_Base ai in m.Ais) if (ai != null && ai.GetType().Name == type) n++;
        return n;
    }

    // One Hurt of 1 from 'from' to 'to'; returns what 'from' lost across its health layers (the thorns it got back).
    static float Prick(Entity_Master from, Entity_Master to)
    {
        float before = Pool(from);
        Helper_Entitytools.Interacthurt(from, to, P("Damage", 1f, "Damagemulti", 1f));
        return before - Pool(from);
    }

    static float Pool(Entity_Master m)
    {
        float sum = 0f;
        foreach (string n in new[] { "Health", "Healthover", "Healtharmor" }) { float v = Now(m, n); if (v > 0f) sum += v; }
        return sum;
    }

    static string Pools(Entity_Master m)
    {
        List<string> s = new List<string>();
        foreach (string n in new[] { "Health", "Healthover", "Healtharmor", "Healthresource" }) if (m.Statsserver.ContainsKey(n)) s.Add(n + " " + RogueLabUtil.F(Now(m, n)) + "/" + RogueLabUtil.F(Stat(m, n)));
        return string.Join(", ", s.ToArray());
    }

    static string Ore(Entity_Master m)
    {
        List<string> s = new List<string>();
        foreach (string n in new[] { "Redauramine", "Blueauramine", "Yellowauramine" }) if (m.Statsserver.ContainsKey(n)) s.Add(n + " " + RogueLabUtil.F(Now(m, n)));
        return s.Count > 0 ? string.Join(", ", s.ToArray()) : "no ore stats";
    }

    static void AddStat(Entity_Master m, string name, float value)
    {
        Link_Stats s = new Link_Stats();
        s.Name = name;
        s.Value_Base = value;
        s.Value_Type = Statproperties.Stattype.Fixed;
        s.Display_Type = Statproperties.Displaytypes.Float;
        s.Display_Exposed = false;
        Helper_Entitytools.Addstat(m, s, 0f);
    }

    static Beingobject Being(string name) { return RogueLabUtil.Load<Beingobject>("Mob", name); }

    static Entity_Master Place(string being, Entity_Master parent, Vector3 near, float ring) { return PlaceFor(being, parent, RogueLabUtil.Owner(parent), near, ring); }

    // A building of 'being' parented to 'parent' (as a vessel builds it) but owned by 'owner', on a free tile about
    // 'ring' from 'near'. The spawn is made without Sameasparent so the owner stays as given.
    static Entity_Master PlaceFor(string being, Entity_Master parent, long owner, Vector3 near, float ring)
    {
        Beingobject b = Being(being);
        if (b == null || parent == null) return null;
        Vector3 at = ModKitTest.Spot(near, b, parent.Entitydata.Value.Team, ring, 10f, used, being);
        if (float.IsNaN(at.x)) return null;
        Genitem g = ModKitTest.Surface(at);
        Link_Spawn s = Helper_Entitytools.Createlinkspawnparent(parent.Beingserver);
        s.Being = b; s.Genadd = g; s.Skin = ""; s.Spawnstyle = Link_Spawn.Spawnstyles.Forced; s.Owner = owner;
        Link_Being made = Helper_Entitytools.Spawn(s, g.Position, null, false, null);
        return made != null ? made.Worldobject : null;
    }

    static Entity_Master Named(string prefix)
    {
        foreach (Entity_Master e in Game_Entity.Instance.Entityclient) if (e != null && RogueLabUtil.Alive(e) && RogueLabUtil.BeingName(e).StartsWith(prefix)) return e;
        return null;
    }

    static bool Near(float a, float b, float tol = 0.02f) { return Mathf.Abs(a - b) <= tol * Mathf.Max(1f, Mathf.Abs(b)); }

    static float Now(Entity_Master m, string name) { Link_Stats s; return m != null && m.Statsserver.TryGetValue(name, out s) ? s.Value : -1f; }

    static float Stat(Entity_Master m, string name) { Link_Stats s; return m != null && m.Statsserver.TryGetValue(name, out s) ? s.Value_Calc : -1f; }

    static void Give(float each)
    {
        Game_Mapvars vars = Game_Mapvars.Instance;
        float gain = vars.Resourcegainmultiplier;
        vars.Resourcegainmultiplier = 1f;
        foreach (string c in Colours)
            Helper_Resources.Giveresources(new List<Link_Resourceset> { new Link_Resourceset { Name = c, Amount = each } }, RogueLabUtil.LocalId, Helper_Resources.Styles.Default);
        vars.Resourcegainmultiplier = gain;
    }

    static float[] Amounts()
    {
        float[] a = new float[3];
        GameObject p;
        Tds_Clientinfo info;
        if (Blm_Server.Instance == null || !Blm_Server.Instance.Sessionids.TryGetValue(RogueLabUtil.LocalId, out p) || p == null || !p.TryGetComponent(out info)) return a;
        foreach (Link_Resource r in info.Resources)
            for (int i = 0; i < 3; i++) if (r.Name.ToString() == Colours[i]) a[i] = r.Amount;
        return a;
    }

    static string Wallet() { float[] a = Amounts(); return "R" + RogueLabUtil.F(a[0]) + " B" + RogueLabUtil.F(a[1]) + " Y" + RogueLabUtil.F(a[2]); }

    static void Register(string label, Bl_ScriptableObject item)
    {
        List<Bl_Address.Loadlist> lists;
        if (!Bl_Address.Groups.TryGetValue(label, out lists)) { lists = new List<Bl_Address.Loadlist>(); Bl_Address.Groups[label] = lists; }
        Bl_Address.Loadlist list = new Bl_Address.Loadlist();
        list.Loaded = true;
        list.Percent = 100;
        list.Items[item.Objectname] = item;
        list.Uids[new FixedString64Bytes(item.Id)] = item;
        list.Index.Add(item);
        lists.Add(list);
        Bl_Address.Groupsids[new FixedString64Bytes(item.Id)] = item;
        Bl_Address.Groupsname[new FixedString64Bytes(item.Objectname)] = item;
    }
}
