// Round 7 (0.5.0), map 1: the PC lists of kit docs/17 §12 (the five base vessels) and docs/16 §9 (maps), plus the
// stat rules of docs/17 §2.1-2.4 the base-vessel dive read from code. Solo host, Boutman; the other vessels' groups
// are loaded so their buildings can be placed for the local player. One line per question, as rounds 1-6.
//   boot    C1 every stock Cardinal's core upgrades (17 §12.1)
//   break   S1 stat types (17 §12.2), B1 build and upgrade price scales (16 §9.3), S2 summed percentages, S3 an outside
//           mod survives an upgrade (17 §2.1), U1 a Priority -3 upgrade on an archer, a Sno Cannon and the Bouthouse
//           (17 §12.5), W1 the Bout Way's prices (17 §12.3), P1 a blocked path tile re-routes (16 §9.2),
//           N1 unit counts on the Hut, Keep and Garage (17 §12.4), M1 a mine node and a den spawned mid-match (16 §9.1)
//   Tide 1  M1 reacts, T1 a Sno-tagged foe near a Sno Cannon (17 §4, §11.12)
//   break   R1 a respawn keeps a Priority -3 upgrade (17 §2.4), R2 a faster respawn per player (17 §12.6)
public static class RogueLabRound7
{
    static readonly string[] Vessels = { "Hero_Smacky", "Hero_Snosno", "Hero_Fie", "Hero_Buster" };
    static readonly string[] Colours = { "Redaura", "Blueaura", "Yellowaura" };
    static readonly List<Vector3> used = new List<Vector3>();
    static Upgradeproperties probe, probeHero, probeMain;
    static Entity_Master hero, archerA, archerB, cannon, house, hut, den, node;
    static readonly List<string> spawns = new List<string>();     // "frame being parent" of every spawn while watching
    static readonly List<string> errors = new List<string>();     // exceptions the game logs while watching
    static bool watchSpawns, watchErrors;
    static int beastsBefore;

    // ---------------------------------------------------------------- boot

    // C1: what each stock Cardinal puts on the core (settles which entities each one reaches).
    public static IEnumerator Cardinals()
    {
        Dictionary<string, UnityEngine.Object> arts = RogueLabUtil.Folder("Artifact");
        if (arts == null) { RogueLabProbe.Setup("C1 Cardinal core upgrades", "no Artifact folder"); yield break; }
        List<string> names = new List<string>(arts.Keys);
        names.Sort();
        int dumped = 0;
        foreach (string n in names)
        {
            Artifactentryobject a = arts[n] as Artifactentryobject;
            if (a == null || a.Artifactloads == null || n == RogueLabData.Cardinal) continue;
            Bl_Address.Loadgroup group = null;
            try { group = Bl_Address.Load(a.Artifactloads.Objectname); } catch (Exception e) { RogueLabLog.Test("CARD " + n + " load threw " + e.Message); continue; }
            float t = 0f;
            while (group != null && !group.Loaded && t < 30f) { t += Time.unscaledDeltaTime; yield return null; }
            Artifactobject art = Bl_Address.Getload("Artifact", a.Artifactloads.Objectname) as Artifactobject;
            if (art == null) { RogueLabLog.Test("CARD " + n + " '" + a.Display_Name + "': no Artifactobject " + a.Artifactloads.Objectname); continue; }
            List<string> ups = new List<string>();
            foreach (Upgradeproperties u in art.Coreupgrades) ups.Add(Describe(u));
            RogueLabLog.Test("CARD " + n + " '" + a.Display_Name + "' type " + a.Type + " | " + string.Join(" ; ", ups.ToArray()));
            dumped++;
        }
        RogueLabProbe.Info("C1 Cardinal core upgrades", dumped + " of " + names.Count + " Cardinals dumped (CARD lines)");
    }

    static string Describe(Upgradeproperties u)
    {
        if (u == null) return "null";
        List<string> ais = new List<string>(), ps = new List<string>();
        if (u.Aiscripts != null) foreach (UnityEngine.Object o in u.Aiscripts) ais.Add(o != null ? o.name : "null");
        if (u.Defaultparams != null) foreach (KeyValuePair<string, float> p in u.Defaultparams) ps.Add(p.Key + "=" + RogueLabUtil.F(p.Value));
        return u.Objectname + " prio " + u.Priority + " ais [" + string.Join(",", ais.ToArray()) + "] {" + string.Join("; ", ps.ToArray()) + "}";
    }

    // ---------------------------------------------------------------- map 1

    public static IEnumerator Map1()
    {
        RogueLabMatch.freeze = true;
        yield return new WaitForSeconds(2f);
        hero = RogueLabUtil.MainHero();
        if (hero == null) { RogueLabProbe.Setup("round 7", "no vessel"); yield break; }
        Game_Entity.Instance.Onspawn += OnSpawn;
        Application.logMessageReceived += OnLog;
        yield return Guarded("setup", Setup());
        if (RogueLabData.Rerun)
        {
            // 0.5.1: the second launch reruns only what the first left open (P1, M1, T1) and S1's hurt-vessel case.
            // 0.5.2 (third launch): only M1, with the den's Addperwave cut to 0 so it adds a beast at Tide 1.
            hut = Place(RogueLabUtil.Load<Beingobject>("Mob", "Tower_Smackyhut"), hero, hero.Entityposition.Value, 4.5f);
            yield return new WaitForSeconds(3f);
            yield return Guarded("M1 map objects mid-match", MapObjects());
            yield return Guarded("Tide 1 probes", Tide1());
            Game_Entity.Instance.Onspawn -= OnSpawn;
            Application.logMessageReceived -= OnLog;
            yield break;
        }
        RogueLabMatch.Guard("S1 stat types", StatTypes);
        yield return Guarded("B1 build and upgrade price", Prices());
        RogueLabMatch.Guard("S2 summed percentages", Stacking);
        yield return Guarded("U1 Priority -3 upgrade", Priority3());
        yield return Guarded("W1 Bout Way prices", BoutWay());
        yield return Guarded("P1 path re-route", Reroute());
        yield return Guarded("N1 unit counts", Units());
        yield return Guarded("M1 map objects mid-match", MapObjects());
        yield return Guarded("Tide 1 probes", Tide1());
        yield return Guarded("R1 R2 respawn", Respawns());
        Game_Entity.Instance.Onspawn -= OnSpawn;
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

    static void OnSpawn(Entity_Master m)
    {
        if (!watchSpawns || m == null) return;
        spawns.Add(Time.frameCount + " " + RogueLabUtil.BeingName(m) + " " + (m.Parent != null ? RogueLabUtil.BeingName(m.Parent) + "#" + m.Parent.GetInstanceID() : "-"));
    }

    static void OnLog(string message, string stack, LogType type)
    {
        if (!watchErrors || (type != LogType.Exception && type != LogType.Error) || message.StartsWith("[RogueLab]")) return;
        if (errors.Count < 6) errors.Add(RogueLabUtil.Clip(message.Replace("\n", " ")) + " @ " + RogueLabUtil.Clip(stack.Replace("\n", " ")));
    }

    // The other vessels' groups (their buildings and units), the probe upgrades, and aura for the price checks.
    static IEnumerator Setup()
    {
        List<string> loaded = new List<string>();
        foreach (string v in Vessels)
        {
            Heroentryobject h = RogueLabUtil.Heroentry(v);
            if (h == null || h.Heroloads == null) { loaded.Add(v + " missing"); continue; }
            Bl_Address.Loadgroup g = null;
            try { g = Bl_Address.Load(h.Heroloads.Objectname); } catch (Exception e) { loaded.Add(v + " threw " + e.Message); continue; }
            float t = 0f;
            while (g != null && !g.Loaded && t < 90f) { t += Time.unscaledDeltaTime; yield return null; }
            loaded.Add(v + " " + (g != null && g.Loaded) + " " + RogueLabUtil.F(t) + " s");
        }
        Upgradeproperties template = Bl_Address.Getload("Upgrade", "Hero_Boutman_Tooltip_Upgradeproperties") as Upgradeproperties;
        if (template == null) template = Bl_Address.Getload("Upgrade", "Tower_Smackyarcher_Range2_Upgradeobject") as Upgradeproperties;
        if (template != null)
        {
            probe = RogueLabKitDirections.Make(template, "RogueLab", "Probe", "RogueLab probe", "Priority -3, Range +0.5");
            probe.Defaultparams["Range_Add"] = 0.5f;
            probe.Defaultparam.Add(new Upgradeproperties.Param { Name = "Range_Add", Value = 0.5f });
            probeHero = RogueLabKitDirections.Make(template, "RogueLab", "Vessel", "RogueLab vessel probe", "Priority -3 on the vessel");
            probeMain = RogueLabKitDirections.Make(template, "RogueLab", "Main", "RogueLab priority 0", "Priority 0");
            probeMain.Priority = 0;
            Register("Upgrade", probe);
            Register("Upgrade", probeHero);
            Register("Upgrade", probeMain);
        }
        Give(1000f);
        RogueLabProbe.Info("round 7 setup", "groups [" + string.Join(", ", loaded.ToArray()) + "], probe upgrades " + (probe != null) + ", vessel " + RogueLabUtil.BeingName(hero)
            + ", aura " + Wallet() + ", loss multiplier " + RogueLabUtil.F(Game_Mapvars.Instance.Resourcelossmultiplier) + ", gain " + RogueLabUtil.F(Game_Mapvars.Instance.Resourcegainmultiplier));
    }

    // ---- S1 (17 §12.2): each stat's Value_Type, and what raising max health does to current health.
    static void StatTypes()
    {
        RogueLabProbe.Info("S1 vessel stats", Stats(hero));
        Entity_Master core = RogueLabMatch.PlayerCore();
        if (core != null) RogueLabProbe.Info("S1 core stats", Stats(core));
        Link_Stats hp;
        if (!hero.Statsserver.TryGetValue("Health", out hp)) { RogueLabProbe.Setup("S1 health raise", "no Health stat"); return; }
        float v0 = hp.Value, m0 = hp.Value_Calc;
        Link_Modifier add = Helper_Entitytools.Addmod(hero, "Health", 20f, Helper_Entitytools.Modtypes.Add, 0f);
        float v1 = hp.Value, m1 = hp.Value_Calc;
        if (add != null) add.Remove();
        float v2 = hp.Value, m2 = hp.Value_Calc;
        RogueLabProbe.Info("S1 health raise on a full vessel", "type " + hp.Value_Type + ": " + RogueLabUtil.F(v0) + "/" + RogueLabUtil.F(m0) + " -> Add +20 -> " + RogueLabUtil.F(v1) + "/" + RogueLabUtil.F(m1)
            + " -> removed -> " + RogueLabUtil.F(v2) + "/" + RogueLabUtil.F(m2) + (Mathf.Abs(v1 - m1) < 0.01f ? " (current follows max: refills)" : " (current stays)"));
    }

    // S1b: the same raise on a vessel missing 20 health (does a Health Cardinal heal, or only raise the cap?).
    static void HurtRaise()
    {
        Link_Stats hp;
        if (!hero.Statsserver.TryGetValue("Health", out hp)) { RogueLabProbe.Setup("S1 health raise on a hurt vessel", "no Health stat"); return; }
        Helper_Entitytools.Interacthurt(hero, hero, new Dictionary<string, float> { { "Damage", 20f }, { "Damagemulti", 1f } });
        float v0 = hp.Value, m0 = hp.Value_Calc;
        Link_Modifier add = Helper_Entitytools.Addmod(hero, "Health", 20f, Helper_Entitytools.Modtypes.Add, 0f);
        float v1 = hp.Value, m1 = hp.Value_Calc;
        Link_Modifier multi = Helper_Entitytools.Addmod(hero, "Health", 0.5f, Helper_Entitytools.Modtypes.Multi, 0f);
        float v2 = hp.Value, m2 = hp.Value_Calc;
        multi.Remove();
        float v3 = hp.Value, m3 = hp.Value_Calc;
        if (add != null) add.Remove();
        float v4 = hp.Value, m4 = hp.Value_Calc;
        RogueLabProbe.Info("S1 health raise on a hurt vessel", "type " + hp.Value_Type + ": " + RogueLabUtil.F(v0) + "/" + RogueLabUtil.F(m0) + " -> Add +20 -> " + RogueLabUtil.F(v1) + "/" + RogueLabUtil.F(m1)
            + " -> Multi +0.5 -> " + RogueLabUtil.F(v2) + "/" + RogueLabUtil.F(m2) + " -> Multi removed -> " + RogueLabUtil.F(v3) + "/" + RogueLabUtil.F(m3) + " -> Add removed -> " + RogueLabUtil.F(v4) + "/" + RogueLabUtil.F(m4));
    }

    static string Stats(Entity_Master m)
    {
        List<string> s = new List<string>();
        foreach (KeyValuePair<string, Link_Stats> kv in m.Statsserver)
            s.Add(kv.Key + "=" + RogueLabUtil.F(kv.Value.Value) + "/" + RogueLabUtil.F(kv.Value.Value_Calc) + " " + kv.Value.Value_Type);
        return string.Join(", ", s.ToArray());
    }

    // ---- B1 (16 §9.3): the price scale of a build option and of a building's Upgrade command.
    static IEnumerator Prices()
    {
        Link_Command opt1;
        if (!hero.Commandlistserver.TryGetValue("Buildoption1", out opt1) || opt1.Ref == null || !hero.Commandlistserver.ContainsKey("Build"))
        { RogueLabProbe.Setup("B1 build price scale", "the vessel has no Buildoption1 or Build command"); yield break; }
        Link_Command mine = Helper_Entitytools.Createcommand(hero, opt1.Ref.Objectname, "Buildoption7", false, true);
        mine.Goalstring = "Tower_Smackyarcher";
        float loss = Game_Mapvars.Instance.Resourcelossmultiplier;
        float[] paid = new float[3];
        float cost = Cost("Tower_Smackyarcher", "Redaura");
        Entity_Master[] got = new Entity_Master[1];

        mine.Goalvalue = 1f;
        yield return BuildBy(hero, "Tower_Smackyarcher", hero.Entityposition.Value, 2.5f, paid, got);
        archerA = got[0];
        float full = paid[0];
        mine.Goalvalue = 0.8f;
        yield return BuildBy(hero, "Tower_Smackyarcher", hero.Entityposition.Value, 2.5f, paid, got);
        archerB = got[0];
        float cut = paid[0];
        Helper_Entitytools.Removecommand(hero, "Buildoption7");
        bool ok = archerA != null && archerB != null && Mathf.Abs(full - cost * loss) < 0.6f && Mathf.Abs(cut - 0.8f * cost * loss) < 0.6f;
        RogueLabProbe.Check("B1 a build option's Goalvalue scales the build price", ok, "archer Red " + RogueLabUtil.F(cost) + ", loss multiplier " + RogueLabUtil.F(loss) + ": paid "
            + RogueLabUtil.F(full) + " at 1, " + RogueLabUtil.F(cut) + " at 0.8; built " + (archerA != null) + ", " + (archerB != null)
            + (archerB != null ? ", second archer's Sellvalue " + RogueLabUtil.F(archerB.Entitydata.Value.Sellvalue) : ""));
        if (archerA == null) yield break;

        // S3 (17 §2.1): an outside mod on archer A, then a stock upgrade bought through its Upgrade command at 0.8.
        Link_Modifier outside = Helper_Entitytools.Addmod(archerA, "Range", 1f, Helper_Entitytools.Modtypes.Add, 0f);
        float r0 = Stat(archerA, "Range");
        Link_Command up;
        if (!archerA.Commandlistserver.TryGetValue("Upgrade", out up)) { RogueLabProbe.Setup("B1 upgrade price scale", "the archer has no Upgrade command"); yield break; }
        up.Goalvalue = 0.8f;
        string longBow = "Tower_Smackyarcher_Range2_Upgradeobject";
        float blue = Cost(longBow, "Blueaura", true);
        yield return new WaitForSeconds(1.2f);
        float[] upPaid = new float[3];
        yield return Track(() => archerA.Upgradelistserver.ContainsKey(longBow), 5f, upPaid,
            () => Send(archerA, "Upgrade", "{\"Upgrade\":\"" + longBow + "\",\"Upgrader\":" + (int)RogueLabUtil.LocalId + "}"));
        bool has = archerA.Upgradelistserver.ContainsKey(longBow);
        RogueLabProbe.Check("B1 an Upgrade command's Goalvalue scales the upgrade price", has && Mathf.Abs(upPaid[1] - 0.8f * blue * loss) < 0.6f,
            "Long Bow Blue " + RogueLabUtil.F(blue) + ": paid " + RogueLabUtil.F(upPaid[1]) + " at 0.8, bought " + has);
        float r1 = Stat(archerA, "Range");
        bool alive = outside != null && Listed(archerA, "Range", outside);
        Helper_Entitytools.Removeupgrade(archerA, Bl_Address.Getload("Upgrade", longBow) as Upgradeproperties);
        float r2 = Stat(archerA, "Range");
        bool still = outside != null && Listed(archerA, "Range", outside);
        RogueLabProbe.Check("S3 an outside mod survives a stock upgrade and its removal", alive && still && Mathf.Abs(r1 - r0 - 2.5f) < 0.05f && Mathf.Abs(r2 - r0) < 0.05f,
            "Range with the outside +1: " + RogueLabUtil.F(r0) + " -> Long Bow " + RogueLabUtil.F(r1) + " -> removed " + RogueLabUtil.F(r2) + "; mod listed " + alive + ", " + still);
        if (outside != null) outside.Remove();
        if (has) Send(archerA, "Upgrade", "{\"Upgrade\":\"" + longBow + "\",\"Upgrader\":" + (int)RogueLabUtil.LocalId + "}");   // archer A goes on at Level 2 for U1
        yield return new WaitForSeconds(1.2f);
    }

    // ---- S2 (17 §2.1): two Multi +0.5 give x2, not x2.25; a Pmulti and a Multi compound.
    static void Stacking()
    {
        Entity_Master a = archerB ?? archerA;
        if (a == null) { RogueLabProbe.Setup("S2 summed percentages", "no archer"); return; }
        float d0 = Stat(a, "Damage");
        Link_Modifier m1 = Helper_Entitytools.Addmod(a, "Damage", 0.5f, Helper_Entitytools.Modtypes.Multi, 0f);
        float d1 = Stat(a, "Damage");
        Link_Modifier m2 = Helper_Entitytools.Addmod(a, "Damage", 0.5f, Helper_Entitytools.Modtypes.Multi, 0f);
        float d2 = Stat(a, "Damage");
        m1.Remove(); m2.Remove();
        Link_Modifier p1 = Helper_Entitytools.Addmod(a, "Damage", 0.5f, Helper_Entitytools.Modtypes.Pmulti, 0f);
        Link_Modifier p2 = Helper_Entitytools.Addmod(a, "Damage", 0.5f, Helper_Entitytools.Modtypes.Pmulti, 0f);
        float dp = Stat(a, "Damage");
        p2.Remove();
        Link_Modifier m3 = Helper_Entitytools.Addmod(a, "Damage", 0.5f, Helper_Entitytools.Modtypes.Multi, 0f);
        float dpm = Stat(a, "Damage");
        p1.Remove(); m3.Remove();
        float d3 = Stat(a, "Damage");
        float r1 = d1 / d0, r2 = d2 / d0;
        RogueLabProbe.Check("S2 two Multi +0.5 sum to x2 (not x2.25)", Mathf.Abs(r2 - 2f) < 0.02f && Mathf.Abs(r1 - 1.5f) < 0.02f,
            "archer Damage " + RogueLabUtil.F(d0) + " -> " + RogueLabUtil.F(d1) + " -> " + RogueLabUtil.F(d2) + " (x" + RogueLabUtil.F(r2) + "), back to " + RogueLabUtil.F(d3));
        RogueLabProbe.Check("S2 two Pmulti +0.5 sum too, and a Pmulti and a Multi compound", Mathf.Abs(dp / d0 - 2f) < 0.02f && Mathf.Abs(dpm / d0 - 2.25f) < 0.02f,
            "Pmulti x2 " + RogueLabUtil.F(dp) + ", Pmulti +0.5 with Multi +0.5 " + RogueLabUtil.F(dpm) + " (x" + RogueLabUtil.F(dpm / d0) + ")");
    }

    // ---- U1 (17 §12.5, §2.2): a Priority -3 upgrade leaves Level, panel and model alone; on the Bouthouse it reaches Boutman.
    static IEnumerator Priority3()
    {
        if (probe == null) { RogueLabProbe.Setup("U1 Priority -3 upgrade", "no probe upgrade"); yield break; }
        Beingobject cb = RogueLabUtil.Load<Beingobject>("Mob", "Tower_Snosnocannon");
        cannon = Place(cb, hero, hero.Entityposition.Value, 3.5f);
        house = Place(RogueLabUtil.Load<Beingobject>("Mob", "Tower_Boutmanhouse"), hero, hero.Entityposition.Value, 3f);
        yield return new WaitForSeconds(2f);
        foreach (Entity_Master t in new[] { archerA, cannon, house })
        {
            if (t == null) { RogueLabProbe.Setup("U1 Priority -3 upgrade", "a building is missing (archer " + (archerA != null) + ", cannon " + (cannon != null) + ", house " + (house != null) + ")"); continue; }
            string before = Look(t);
            float range0 = Stat(t, "Range"), heroRange0 = Stat(hero, "Range");
            Helper_Entitytools.Upgrade(t, probe, Helper_Entitytools.Upgradecheck.Forced, null);
            yield return new WaitForSeconds(1.5f);
            string after = Look(t);
            bool on = t.Upgradelistserver.ContainsKey(probe.Objectname);
            string name = RogueLabUtil.BeingName(t);
            RogueLabProbe.Check("U1 a Priority -3 upgrade leaves " + name + "'s Level, main upgrade and skin alone", on && before == after,
                "before " + before + ", after " + after + ", listed " + on + ", Range " + RogueLabUtil.F(range0) + " -> " + RogueLabUtil.F(Stat(t, "Range")));
            if (t == house)
            {
                Entity_Master h = RogueLabUtil.MainHero();
                bool leaked = h != null && h.Upgradelistserver.ContainsKey(probe.Objectname);
                RogueLabProbe.Check("U1 an upgrade on the Bouthouse lands on Boutman", leaked, "vessel has it " + leaked + ", vessel Range " + RogueLabUtil.F(heroRange0) + " -> " + RogueLabUtil.F(Stat(h, "Range")));
            }
        }
        if (archerB != null)
        {
            string before = Look(archerB);
            Helper_Entitytools.Upgrade(archerB, probeMain, Helper_Entitytools.Upgradecheck.Forced, null);
            yield return new WaitForSeconds(1.5f);
            string after = Look(archerB);
            RogueLabProbe.Info("U1 control: a Priority 0 upgrade on a fresh archer", "before " + before + ", after " + after + "; Upgradeoption1 '" + Option(archerB) + "'");
            Helper_Entitytools.Removeupgrade(archerB, probeMain);
            yield return new WaitForSeconds(0.5f);
            RogueLabProbe.Info("U1 control removed", Look(archerB) + " (Removeupgrade recomputes neither, 17 §2.2)");
        }
    }

    static string Look(Entity_Master t)
    {
        return "Level " + t.Entitydata.Value.Level + " main '" + t.Mainupgrade.Value + "' skin '" + t.Skin.Value + "' option1 '" + Option(t) + "'";
    }

    static string Option(Entity_Master t)
    {
        Link_Command c;
        return t.Commandlistserver.TryGetValue("Upgradeoption1", out c) ? c.Goalstring : "-";
    }

    // ---- W1 (17 §12.3): fish 1 and barrel 1 free; fish n costs (n-1) x Fishcost x 25; a Fishcost mod waits for a recount.
    static IEnumerator BoutWay()
    {
        Entity_Master bro = null;
        float t = 0f;
        while (bro == null && t < 25f) { bro = Mine("Mob_Boutmanbro"); t += 0.5f; yield return new WaitForSeconds(0.5f); }
        if (bro == null) { RogueLabProbe.Setup("W1 Bout Way prices", "no Boutbro from the Bouthouse after 25 s"); yield break; }
        string fish = "", barrel = "";
        List<string> opts = new List<string>();
        foreach (KeyValuePair<string, Link_Command> c in bro.Commandlistserver)
            if (c.Key.StartsWith("Buildoption"))
            {
                opts.Add(c.Key + "=" + c.Value.Goalstring + " x" + RogueLabUtil.F(c.Value.Goalvalue));
                if (c.Value.Goalstring.Contains("fish")) fish = c.Value.Goalstring;
                if (c.Value.Goalstring.Contains("barrel")) barrel = c.Value.Goalstring;
            }
        RogueLabProbe.Info("W1 Boutbro's build options at the start", string.Join(", ", opts.ToArray()) + "; Fishcost " + RogueLabUtil.F(Stat(bro, "Fishcost")));
        if (fish == "") { RogueLabProbe.Setup("W1 Bout Way prices", "no fish build option"); yield break; }
        Give(500f);
        float fishCost = Cost(fish, "Yellowaura");
        List<string> line = new List<string>();
        float[] paid = new float[3];
        Entity_Master[] got = new Entity_Master[1];
        float[] fishPaid = new float[4];
        for (int i = 0; i < 4; i++)
        {
            if (i == 2)
            {
                Helper_Entitytools.Addmod(bro, "Fishcost", -0.2f, Helper_Entitytools.Modtypes.Multi, 0f);
                line.Add("Fishcost -20% (option now x" + RogueLabUtil.F(Goal(bro, fish)) + ")");
            }
            float scale = Goal(bro, fish);
            yield return BuildBy(bro, fish, bro.Entityposition.Value, 2f, paid, got);
            fishPaid[i] = paid[2];
            line.Add("fish " + (i + 1) + " at x" + RogueLabUtil.F(scale) + " paid Yellow " + RogueLabUtil.F(paid[2]) + (got[0] != null ? "" : " (not built)"));
        }
        float barrelPaid = -1f;
        if (barrel != "")
        {
            float scale = Goal(bro, barrel);
            yield return BuildBy(bro, barrel, bro.Entityposition.Value, 2.5f, paid, got);
            barrelPaid = paid[1];
            line.Add("barrel 1 at x" + RogueLabUtil.F(scale) + " paid Blue " + RogueLabUtil.F(paid[1]) + (got[0] != null ? "" : " (not built)"));
        }
        float loss = Game_Mapvars.Instance.Resourcelossmultiplier;
        RogueLabProbe.Check("W1 Boutbro's first fish and first barrel are free", fishPaid[0] < 0.01f && barrelPaid < 0.01f && barrelPaid >= 0f, string.Join("; ", line.ToArray()));
        RogueLabProbe.Check("W1 fish 2 costs one fish price", Mathf.Abs(fishPaid[1] - fishCost * loss) < 0.6f, "fish Yellow " + RogueLabUtil.F(fishCost) + ", paid " + RogueLabUtil.F(fishPaid[1]));
        RogueLabProbe.Info("W1 a Fishcost mod and Boutbro's recount", "fish 3 paid " + RogueLabUtil.F(fishPaid[2]) + " (2 x 25 = 50 if the mod waits for a recount, 40 if read at once), fish 4 paid "
            + RogueLabUtil.F(fishPaid[3]) + " (3 x 0.8 x 25 = 60 after the recount)");
    }

    static float Goal(Entity_Master m, string being)
    {
        foreach (KeyValuePair<string, Link_Command> c in m.Commandlistserver)
            if (c.Key.StartsWith("Buildoption") && c.Value.Goalstring == being) return c.Value.Goalvalue;
        return -1f;
    }

    // ---- P1 (16 §9.2): a tile made unwalkable mid-match drops out of the foes' path once Savedpaths is cleared.
    static IEnumerator Reroute()
    {
        Entity_Master enemy = ModKitTest.Core(), core = RogueLabMatch.PlayerCore();
        Beingobject dagger = RogueLabUtil.Load<Beingobject>("Mob", "Enemy_Dagger");
        Beingobject wall = RogueLabUtil.Load<Beingobject>("Mob", "Tower_Smackywall");
        if (enemy == null || core == null || dagger == null || wall == null) { RogueLabProbe.Setup("P1 path re-route", "enemy core " + (enemy != null) + ", core " + (core != null) + ", Dagger " + (dagger != null) + ", wall " + (wall != null)); yield break; }
        Link_Being walker = new Link_Being { Being = dagger, Entity = dagger.Entity, Block = dagger.Block, Team = enemy.Entitydata.Value.Team };
        Genitem start = ModKitTest.FoeTile(dagger, enemy.Entityposition.Value, 1f, 5f, null);
        if (start == null) { RogueLabProbe.Setup("P1 path re-route", "no foe tile by the enemy core"); yield break; }
        // Round 7's first launch found no path to the core's own position; aim at a standable tile beside it, both ways.
        Genitem end = ModKitTest.FoeTile(dagger, core.Entityposition.Value, 1f, 4f, null);
        bool force = false;
        Link_Path p0 = end != null ? Helper_Navtools.Getpath(start, end, walker, false) : default(Link_Path);
        if ((!p0.Valid || p0.Path == null) && end != null) { force = true; p0 = Helper_Navtools.Getpath(start, end, walker, true); }
        RogueLabProbe.Info("P1 the foes' path", "start " + start.Position.ToString("F0") + ", end " + (end != null ? end.Position.ToString("F0") : "-") + ", force " + force + ", valid " + p0.Valid + ", " + (p0.Path != null ? p0.Path.Count : 0) + " tiles");
        if (!p0.Valid || p0.Path == null || p0.Path.Count < 8) { RogueLabProbe.Setup("P1 path re-route", "no path from the enemy core"); yield break; }
        for (int k = 0; k < 6; k++)
        {
            Genitem cut = p0.Path[p0.Path.Count / 3 + k * 2];
            Link_Spawn s = Helper_Entitytools.Createlinkspawnparent(hero.Beingserver);
            s.Being = wall; s.Genadd = cut; s.Skin = ""; s.Spawnstyle = Link_Spawn.Spawnstyles.Forced;
            Link_Being made = Helper_Entitytools.Spawn(s, cut.Position, hero.Beingserver, true, null);
            yield return new WaitForSeconds(0.5f);
            Link_Path cached = Helper_Navtools.Getpath(start, end, walker, force);
            Game_Grid.Instance.Savedpaths.Clear();
            Link_Path fresh = Helper_Navtools.Getpath(start, end, walker, force);
            bool through = fresh.Path != null && fresh.Path.Contains(cut);
            if (made != null && made.Worldobject != null) Helper_Entitytools.Despawn(made, false, "RogueLab");
            yield return new WaitForSeconds(0.3f);
            Game_Grid.Instance.Savedpaths.Clear();
            if (!fresh.Valid && k < 5) continue;                 // that tile was the only way: try one further on
            RogueLabProbe.Check("P1 a wall spawned on a path tile re-routes foes once Savedpaths is cleared", fresh.Valid && !through,
                "tile " + cut.Position.ToString("F0") + " (" + (p0.Path.IndexOf(cut) + 1) + " of " + p0.Path.Count + "); before the clear the cached path " + (cached.Path != null && cached.Path.Contains(cut) ? "still crossed it" : "already avoided it")
                + " (" + (cached.Path != null ? cached.Path.Count : 0) + " tiles); after: valid " + fresh.Valid + ", " + (fresh.Path != null ? fresh.Path.Count : 0) + " tiles, crosses it " + through);
            yield break;
        }
    }

    // ---- N1 (17 §12.4): raising Units on the Hut, the Keep and the Garage, then calling the building's own count check.
    static IEnumerator Units()
    {
        string[][] cases = {
            new[] { "Tower_Smackyhut", "Addressminercount" },
            new[] { "Tower_Fiekeep", "Addressmobcount" },
            new[] { "Tower_Bustergarage", "Addressdogcount" } };
        watchSpawns = true;
        foreach (string[] c in cases)
        {
            Entity_Master b = Place(RogueLabUtil.Load<Beingobject>("Mob", c[0]), hero, hero.Entityposition.Value, 4.5f);
            if (b == null) { RogueLabProbe.Setup("N1 " + c[0] + " unit count", "could not place it"); continue; }
            if (c[0] == "Tower_Smackyhut") hut = b;
            yield return new WaitForSeconds(6f);
            string id = "#" + b.GetInstanceID();
            int start = Count(id);
            float units0 = Stat(b, "Units");
            int f0 = Time.frameCount;
            Link_Modifier mod = Helper_Entitytools.Addmod(b, "Units", 1f, Helper_Entitytools.Modtypes.Add, 0f);
            int sameFrameMod = CountFrame(f0);
            yield return new WaitForSeconds(4f);
            int afterMod = Count(id);
            int f1 = Time.frameCount;
            foreach (Entity_Ai_Base ai in b.Ais) if (ai != null && ai.GetType().Name.StartsWith("Entity_Ai_Tower_")) ai.SendMessage(c[1], SendMessageOptions.DontRequireReceiver);
            int sameFrameCall = CountFrame(f1);
            yield return new WaitForSeconds(3f);
            int afterCall = Count(id);
            RogueLabProbe.Check("N1 " + c[0] + ": a Units mod alone spawns nothing", sameFrameMod == 0,
                "Units " + RogueLabUtil.F(units0) + " -> " + RogueLabUtil.F(Stat(b, "Units")) + "; spawned in the mod's frame " + sameFrameMod + "; spawns from it so far " + start + " -> " + afterMod + " in the next 4 s");
            RogueLabProbe.Check("N1 " + c[0] + ": " + c[1] + " after a Units mod spawns one", sameFrameCall == 1,
                "spawned in the call's frame " + sameFrameCall + ", spawns from it " + afterMod + " -> " + afterCall + "; its spawns: " + Last(id, 4));
            if (mod != null) mod.Remove();
        }
        watchSpawns = false;
    }

    static int Count(string id) { int n = 0; foreach (string s in spawns) if (s.EndsWith(id)) n++; return n; }

    // Units the building spawns in the given frame (the count checks spawn synchronously; nothing else spawns a Mob_ then).
    static int CountFrame(int frame) { int n = 0; foreach (string s in spawns) if (s.StartsWith(frame + " Mob_")) n++; return n; }

    static string Last(string id, int n)
    {
        List<string> l = spawns.FindAll(s => s.EndsWith(id));
        return string.Join("; ", l.GetRange(Mathf.Max(0, l.Count - n), Mathf.Min(n, l.Count)).ToArray());
    }

    // ---- M1 (16 §9.1): a stock ore node next to the Hut and a second den, both spawned mid-match.
    static IEnumerator MapObjects()
    {
        Entity_Master ore = Named("Resource_"), oldDen = Named("Tower_Den");   // the first launch took the den for an ore: dens are tagged Resource
        Beingobject nodeBeing = null;
        foreach (string n in new[] { "Resource_Red", "Resource_Blue", "Resource_Yellow" }) if (nodeBeing == null) nodeBeing = RogueLabUtil.Load<Beingobject>("Mob", n);
        if (nodeBeing != null && hut != null)
        {
            Vector3 at = ModKitTest.Spot(hut.Entityposition.Value, nodeBeing, ore != null ? ore.Entitydata.Value.Team : hero.Entitydata.Value.Team, 1.5f, 6f, used, nodeBeing.Objectname);
            if (!float.IsNaN(at.x))
            {
                Link_Spawn s = new Link_Spawn();
                s.Being = nodeBeing; s.Genadd = ModKitTest.Surface(at); s.Skin = ""; s.Spawnstyle = Link_Spawn.Spawnstyles.Forced;
                s.Team = ore != null ? ore.Entitydata.Value.Team : 1; s.Owner = -1;
                Link_Being made = Helper_Entitytools.Spawn(s, at, null, false, null);
                node = made != null ? made.Worldobject : null;
            }
        }
        RogueLabProbe.Info("M1 ore node spawned", (nodeBeing != null ? nodeBeing.Objectname : "no node being loaded") + ", node " + (node != null) + (node != null ? " team " + node.Entitydata.Value.Team + ", " + RogueLabUtil.F(Now(node, "Healthresource")) + " Healthresource, tags [" + string.Join(",", node.Taglistserver.ToArray()) + "], "
            + RogueLabUtil.F(ModKitTest.Flat(node.Entityposition.Value, hut.Entityposition.Value)) + " from the Hut" : "") + "; a stock node " + (ore != null ? RogueLabUtil.BeingName(ore) + " team " + ore.Entitydata.Value.Team : "none"));
        Beingobject denBeing = RogueLabUtil.Load<Beingobject>("Mob", "Tower_Den");
        if (denBeing == null) { RogueLabProbe.Setup("M1 den spawned mid-match", "Tower_Den is not loaded on this map"); yield break; }
        Entity_Master core = RogueLabMatch.PlayerCore();
        int team = oldDen != null ? oldDen.Entitydata.Value.Team : 1;
        Vector3 spot = ModKitTest.Spot(core.Entityposition.Value, denBeing, team, 8f, 14f, used, "a den");
        if (float.IsNaN(spot.x)) { RogueLabProbe.Setup("M1 den spawned mid-match", "no tile"); yield break; }
        Link_Spawn d = new Link_Spawn();
        d.Being = denBeing; d.Genadd = ModKitTest.Surface(spot); d.Skin = ""; d.Spawnstyle = Link_Spawn.Spawnstyles.Forced; d.Team = team; d.Owner = -1;
        Link_Being dm = Helper_Entitytools.Spawn(d, spot, null, false, null);
        den = dm != null ? dm.Worldobject : null;
        beastsBefore = All("Mob_Aurabeast").Count;
        yield return new WaitForSeconds(1f);
        if (den != null && den.Statsserver.ContainsKey("Addperwave")) Helper_Entitytools.Addmod(den, "Addperwave", -Now(den, "Addperwave"), Helper_Entitytools.Modtypes.Add, 0f);
        // The node by hand: five of the vessel's hits, as a miner's would land (pays every player, 17 §2.6).
        if (node != null)
        {
            float h0 = Now(node, "Healthresource"); string w0 = Wallet();
            for (int i = 0; i < 5; i++) { Helper_Entitytools.Interacthurt(hero, node, null); yield return new WaitForSeconds(0.3f); }
            RogueLabProbe.Check("M1 a mine node spawned mid-match takes hits and pays", Now(node, "Healthresource") != h0 || Wallet() != w0,
                "Healthresource " + RogueLabUtil.F(h0) + " -> " + RogueLabUtil.F(Now(node, "Healthresource")) + ", wallet " + w0 + " -> " + Wallet());
        }
        // Both watched for 40 s of the held break: the Hut's miners on the node, the den's first beasts.
        int hits = 0, refills = 0;
        float last = node != null ? Now(node, "Healthresource") : -1f;
        for (float t = 0f; t < 40f; t += 0.25f)
        {
            if (node != null && RogueLabUtil.Alive(node))
            {
                float v = Now(node, "Healthresource");
                if (last >= 0f && v < last - 0.001f) hits++;
                if (last >= 0f && v > last + 0.001f) refills++;
                last = v;
            }
            yield return new WaitForSeconds(0.25f);
        }
        RogueLabProbe.Info("M1 a mine node spawned mid-match is mined by a Hut's miners", "node " + (node != null) + (node != null ? " tags [" + string.Join(",", node.Taglistserver.ToArray()) + "], " + StatsOf(node, "Healthresource", "Redauramine", "Blueauramine", "Yellowauramine") : "")
            + "; in 40 s: " + hits + " hits, " + refills + " refills; Hut miners " + (hut != null ? Children(hut, "Mob_Smackyminer") : "-") + ", mining " + MinerTargets());
        RogueLabProbe.Info("M1 den spawned", "den " + (den != null) + (den != null ? " team " + den.Entitydata.Value.Team + ", tags [" + string.Join(",", den.Taglistserver.ToArray()) + "], " + Beasts(den) + " beasts after 40 s; " + StatsOf(den, "Maxunits", "Addperwave", "Respawn") : "")
            + "; the map's own den " + (oldDen != null ? "team " + oldDen.Entitydata.Value.Team + ", " + Beasts(oldDen) + " beasts, " + StatsOf(oldDen, "Maxunits", "Addperwave", "Respawn") : "none") + "; Aurabeasts on the map " + All("Mob_Aurabeast").Count);
    }

    static string MinerTargets()
    {
        List<string> t = new List<string>();
        foreach (Entity_Master m in All("Mob_Smackyminer")) { Link_Command a; t.Add(m.Commandlistserver.TryGetValue("Attack", out a) && a.Goalobject != null ? RogueLabUtil.BeingName(a.Goalobject) + (a.Goalobject == node ? " (ours)" : "") : "-"); }
        return "[" + string.Join(", ", t.ToArray()) + "]";
    }

    static Entity_Master Named(string prefix)
    {
        foreach (Entity_Master e in Game_Entity.Instance.Entityclient) if (e != null && RogueLabUtil.Alive(e) && RogueLabUtil.BeingName(e).StartsWith(prefix) && e != node && e != den) return e;
        return null;
    }

    static int Beasts(Entity_Master d)
    {
        int n = 0;
        List<Entity_Master> mobs;
        if (Game_Entity.Instance.Entitiesserver.TryGetValue("Mob", out mobs))
            foreach (Entity_Master m in mobs) if (RogueLabUtil.Alive(m) && m.Parent == d) n++;
        return n;
    }

    static string StatsOf(Entity_Master e, params string[] names)
    {
        List<string> s = new List<string>();
        foreach (string n in names) s.Add(n + " " + (e.Statsserver.ContainsKey(n) ? RogueLabUtil.F(e.Statsserver[n].Value) : "-"));
        return string.Join(", ", s.ToArray());
    }

    // ---- Tide 1: M1's den and node react; T1 a Sno-tagged foe beside a Sno Cannon.
    static IEnumerator Tide1()
    {
        Game_Mapvars vars = Game_Mapvars.Instance;
        int beasts0 = den != null ? Beasts(den) : -1;
        float ore0 = node != null ? Now(node, "Healthresource") : -1f;
        string wallet0 = Wallet();
        RogueLabMatch.freeze = false;
        RogueLabMatch.forcing = true;
        yield return RogueLabMatch.Until(() => vars.Wave.Value >= 1, 60f);
        RogueLabMatch.forcing = false;
        int entry = vars.Entry.Value;
        if (vars.Wave.Value < 1) { RogueLabProbe.Setup("Tide 1 probes", "Tide 1 did not start"); yield break; }

        // T1: a copy of the Dagger whose data tags add Sno, beside the cannon, inside its range.
        if (!RogueLabData.Rerun) yield return Guarded("T1 Sno tag", SnoTag());

        yield return new WaitForSeconds(4f);
        int beasts1 = All("Mob_Aurabeast").Count - beastsBefore;
        beasts0 = 0;
        RogueLabProbe.Check("M1 a den spawned mid-match has beasts by Tide 1", den != null && RogueLabUtil.Alive(den) && beasts1 >= 1,
            "beasts " + beasts0 + " in the break -> " + beasts1 + " in Tide 1");
        float ore1 = node != null && RogueLabUtil.Alive(node) ? Now(node, "Healthresource") : -1f;
        RogueLabProbe.Info("M1 the ore node during the break and Tide 1", "node alive " + (node != null && RogueLabUtil.Alive(node)) + ", Healthresource " + RogueLabUtil.F(ore0) + " -> " + RogueLabUtil.F(ore1)
            + "; wallet " + wallet0 + " -> " + Wallet() + "; miners of the Hut " + (hut != null ? Children(hut, "Mob_Smackyminer") : "-"));
        RogueLabMatch.ClearFoes();
        yield return RogueLabMatch.EndTide(entry);
        RogueLabMatch.freeze = true;
    }

    static IEnumerator SnoTag()
    {
        Beingobject dagger = RogueLabUtil.Load<Beingobject>("Mob", "Enemy_Dagger");
        Entity_Master enemy = ModKitTest.Core();
        if (cannon == null || dagger == null || enemy == null) { RogueLabProbe.Setup("T1 Sno-tagged foe and the Sno Cannon", "cannon " + (cannon != null) + ", Dagger " + (dagger != null)); yield break; }
        Beingobject sno = RogueLabUtil.Clone(dagger, "RogueLab_Snodagger");
        sno.Entity = RogueLabUtil.Clone(dagger.Entity, "RogueLab_Snodagger_Entity");
        sno.Entity.Tags = new List<string>(dagger.Entity.Tags);
        sno.Entity.Tags.Add("Sno");
        Register("Mob", sno);
        float rangeSno = Stat(cannon, "Rangesno");
        Genitem tile = ModKitTest.FoeTile(sno, cannon.Entityposition.Value, 1f, Mathf.Max(1.5f, Mathf.Min(rangeSno, Stat(cannon, "Range")) - 0.5f), used);
        if (tile == null) { RogueLabProbe.Setup("T1 Sno-tagged foe and the Sno Cannon", "no foe tile in the cannon's range"); yield break; }
        errors.Clear();
        watchErrors = true;
        string[] plain = new string[1], tagged = new string[1];
        yield return Trial(dagger, enemy, tile, plain);           // control: the stock Dagger on the same tile
        yield return Trial(sno, enemy, tile, tagged);
        watchErrors = false;
        RogueLabProbe.Info("T1 control: a stock Dagger beside the Sno Cannon", plain[0]);
        RogueLabProbe.Check("T1 the Sno Cannon takes a foe tagged Sno as ammo (despawned, not killed)", tagged[0].Contains("despawned") && tagged[0].Contains("killed False") && !plain[0].Contains("killed False"),
            tagged[0] + "; " + RogueLabUtil.F(ModKitTest.Flat(tile.Position, cannon.Entityposition.Value)) + " from the cannon (Rangesno " + RogueLabUtil.F(rangeSno) + ")");
        RogueLabProbe.Info("T1 errors logged meanwhile", errors.Count == 0 ? "none" : string.Join(" | ", errors.ToArray()));
    }

    // One foe of 'being' on 'tile' for up to 12 s: its tags, how it left, the cannon's Ammo and damage meanwhile.
    static IEnumerator Trial(Beingobject being, Entity_Master enemy, Genitem tile, string[] result)
    {
        Link_Spawn s = Helper_Entitytools.Createlinkspawnparent(enemy.Beingserver);
        s.Being = being; s.Genadd = tile; s.Skin = ""; s.Spawnstyle = Link_Spawn.Spawnstyles.Forced;
        string died = "";
        Entity_Master foe = null;
        float t = 0f;
        Game_Entity.OnDespawns watch = (m, killed, reason) => { if (m != null && m == foe && died == "") died = "despawned after " + RogueLabUtil.F(t) + " s, killed " + killed + ", reason '" + reason + "', last hit " + (m.Lasthit != null ? RogueLabUtil.BeingName(m.Lasthit) : "none"); };
        Game_Entity.Instance.Ondespawn += watch;
        float ammo0 = Stat(cannon, "Ammo"), dmg0 = cannon.Damagedone;
        Link_Being made = Helper_Entitytools.Spawn(s, tile.Position, enemy.Beingserver, true, null);
        foe = made != null ? made.Worldobject : null;
        string tags = foe != null ? string.Join(",", foe.Taglistserver.ToArray()) : "-";
        while (t < 12f && died == "" && foe != null) { t += Time.deltaTime; yield return null; }
        Game_Entity.Instance.Ondespawn -= watch;
        result[0] = being.Objectname + " spawned " + (made != null && made.Worldobject != null) + ", tags [" + tags + "]: " + (died != "" ? died : "still there after 12 s")
            + "; cannon Ammo " + RogueLabUtil.F(ammo0) + " -> " + RogueLabUtil.F(Stat(cannon, "Ammo")) + ", damage done " + RogueLabUtil.F(cannon.Damagedone - dmg0);
        if (foe != null && RogueLabUtil.Alive(foe)) Helper_Entitytools.Despawn(foe.Beingserver, false, "RogueLab");
        yield return new WaitForSeconds(1f);
    }

    static string Children(Entity_Master parent, string being)
    {
        int n = 0;
        List<Entity_Master> mobs;
        if (Game_Entity.Instance.Entitiesserver.TryGetValue("Mob", out mobs))
            foreach (Entity_Master m in mobs) if (RogueLabUtil.Alive(m) && m.Parent == parent && RogueLabUtil.BeingName(m) == being) n++;
        return n.ToString();
    }

    // ---- R1 and R2 (17 §2.4, §12.6): four deaths of the vessel: plain, with the core's Respawn cut before the death,
    // from the vessel's own Despawn handler, and from Game_Entity.Ondespawn.
    static IEnumerator Respawns()
    {
        Entity_Master core = RogueLabMatch.PlayerCore();
        hero = RogueLabUtil.MainHero();
        if (core == null || hero == null) { RogueLabProbe.Setup("R2 respawn", "core " + (core != null) + ", vessel " + (hero != null)); yield break; }
        RogueLabProbe.Info("R2 the core's Respawn stat", core.Statsserver.ContainsKey("Respawn") ? RogueLabUtil.F(core.Statsserver["Respawn"].Value) + " (" + core.Statsserver["Respawn"].Value_Type + ")" : "none; stats " + Stats(core));
        if (probeHero != null) Helper_Entitytools.Upgrade(hero, probeHero, Helper_Entitytools.Upgradecheck.Forced, null);
        float[] took = new float[4];
        string[] how = { "plain", "core Respawn -50% before the death", "added in the vessel's Despawn handler", "added in Game_Entity.Ondespawn" };
        bool kept = false;
        for (int i = 0; i < 4; i++)
        {
            Entity_Master h = RogueLabUtil.MainHero();
            if (h == null) { RogueLabProbe.Setup("R2 respawn", "no vessel before death " + (i + 1)); yield break; }
            Link_Modifier[] mod = new Link_Modifier[1];
            Entity_Master.Despawns own = null;
            Game_Entity.OnDespawns global = null;
            if (i == 1) mod[0] = Helper_Entitytools.Addmod(core, "Respawn", -0.5f, Helper_Entitytools.Modtypes.Multi, 0f);
            if (i == 2) { own = d => { if (mod[0] == null) mod[0] = Helper_Entitytools.Addmod(core, "Respawn", -0.5f, Helper_Entitytools.Modtypes.Multi, 0f); }; h.Despawn += own; }
            if (i == 3) { global = (m, k, r) => { if (m == h && mod[0] == null) mod[0] = Helper_Entitytools.Addmod(core, "Respawn", -0.5f, Helper_Entitytools.Modtypes.Multi, 0f); }; Game_Entity.Instance.Ondespawn += global; }
            Game_Entity.Instance.Despawn(h.Beingserver, true, "RogueLab");
            Entity_Master again = null;
            float t = 0f;
            while (t < 90f) { again = RogueLabUtil.MainHero(); if (again != null && again != h) break; t += Time.deltaTime; yield return null; }
            took[i] = again != null && again != h ? t : -1f;
            if (global != null) Game_Entity.Instance.Ondespawn -= global;
            if (mod[0] != null) mod[0].Remove();
            if (i == 0 && again != null && probeHero != null) kept = again.Upgradelistserver.ContainsKey(probeHero.Objectname);
            yield return new WaitForSeconds(2f);
        }
        RogueLabProbe.Check("R1 a respawned vessel keeps a Priority -3 upgrade", kept, probeHero != null ? probeHero.Objectname : "no probe");
        List<string> l = new List<string>();
        for (int i = 0; i < 4; i++) l.Add(how[i] + " " + RogueLabUtil.F(took[i]) + " s");
        bool control = took[0] > 0f && took[1] > 0f && took[1] < took[0] * 0.75f;
        RogueLabProbe.Check("R2 control: a cut on the core's Respawn before the death speeds the respawn", control, string.Join("; ", l.ToArray()));
        RogueLabProbe.Info("R2 a cut made in the vessel's Despawn handler counts", (took[2] > 0f && took[2] < took[0] * 0.75f) + "; in Game_Entity.Ondespawn " + (took[3] > 0f && took[3] < took[0] * 0.75f));
    }

    // ---------------------------------------------------------------- helpers

    // A building of 'being' for the owner of 'parent', on a free tile about 'ring' from 'near', as Hero_Buildsell makes it.
    static Entity_Master Place(Beingobject being, Entity_Master parent, Vector3 near, float ring)
    {
        if (being == null || parent == null) return null;
        Vector3 at = ModKitTest.Spot(near, being, parent.Entitydata.Value.Team, ring, 10f, used, being.Objectname);
        if (float.IsNaN(at.x)) return null;
        Genitem g = ModKitTest.Surface(at);
        Link_Spawn s = Helper_Entitytools.Createlinkspawnparent(parent.Beingserver);
        s.Being = being; s.Genadd = g; s.Skin = ""; s.Spawnstyle = Link_Spawn.Spawnstyles.Forced;
        Link_Being made = Helper_Entitytools.Spawn(s, g.Position, parent.Beingserver, true, null);
        return made != null ? made.Worldobject : null;
    }

    // The game's own build: a Build command to the builder (it walks within Buildrange and pays), and what it paid.
    static IEnumerator BuildBy(Entity_Master builder, string being, Vector3 near, float ring, float[] paid, Entity_Master[] got)
    {
        got[0] = null;
        Beingobject b = RogueLabUtil.Load<Beingobject>("Mob", being);
        if (b == null) { RogueLabLog.Test("build: no being " + being); for (int i = 0; i < 3; i++) paid[i] = 0f; yield break; }
        Vector3 at = ModKitTest.Spot(near, b, builder.Entitydata.Value.Team, ring, 8f, used, being);
        if (float.IsNaN(at.x)) { for (int i = 0; i < 3; i++) paid[i] = 0f; yield break; }
        List<Entity_Master> before = All(being);
        string json = "{\"Tower\":\"" + being + "\",\"Position\":{\"x\":" + RogueLabUtil.F(at.x) + ",\"y\":" + RogueLabUtil.F(at.y) + ",\"z\":" + RogueLabUtil.F(at.z) + "},\"Orientation\":0,\"Owner\":" + RogueLabUtil.LocalId + "}";
        yield return Track(() => { foreach (Entity_Master e in All(being)) if (!before.Contains(e)) { got[0] = e; return true; } return false; }, 30f, paid, () => Send(builder, "Build", json));
        yield return new WaitForSeconds(0.5f);
    }

    // Runs 'act', then sums every drop in each colour of the local wallet, frame by frame, until 'done' (plus a second).
    static IEnumerator Track(Func<bool> done, float timeout, float[] paid, Action act)
    {
        float[] last = Amounts();
        for (int i = 0; i < 3; i++) paid[i] = 0f;
        act();
        float t = 0f, after = -1f;
        while (t < timeout && (after < 0f || t - after < 1f))
        {
            float[] now = Amounts();
            for (int i = 0; i < 3; i++) { if (now[i] < last[i]) paid[i] += last[i] - now[i]; last[i] = now[i]; }
            if (after < 0f && done()) after = t;
            t += Time.deltaTime;
            yield return null;
        }
    }

    static List<Entity_Master> All(string being)
    {
        List<Entity_Master> l = new List<Entity_Master>();
        foreach (Entity_Master e in Game_Entity.Instance.Entityclient) if (e != null && RogueLabUtil.Alive(e) && RogueLabUtil.BeingName(e) == being) l.Add(e);
        return l;
    }

    static Entity_Master Mine(string being)
    {
        foreach (Entity_Master e in All(being)) if (RogueLabUtil.Owner(e) == RogueLabUtil.LocalId) return e;
        return null;
    }

    static Entity_Master First(string tag, string being)
    {
        List<Entity_Master> l;
        if (!Game_Entity.Instance.Entitiesserver.TryGetValue(tag, out l)) return null;
        foreach (Entity_Master e in l) if (RogueLabUtil.Alive(e) && (being == null || RogueLabUtil.BeingName(e) == being)) return e;
        return null;
    }

    static void Send(Entity_Master m, string link, string json)
    {
        m.GetComponent<Entity_Master_Remote>().Command_ServerRpc(link, json, default(ServerRpcParams));
    }

    static float Cost(string name, string colour, bool upgrade = false)
    {
        List<Link_Resourceset> costs = null;
        if (upgrade) { Upgradeproperties u = Bl_Address.Getload("Upgrade", name) as Upgradeproperties; if (u != null) costs = u.Costs; }
        else { Beingobject b = RogueLabUtil.Load<Beingobject>("Mob", name); if (b != null) costs = b.Entity.Costs; }
        if (costs != null) foreach (Link_Resourceset r in costs) if (r.Name == colour) return r.Amount;
        return 0f;
    }

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

    static float Now(Entity_Master m, string name) { Link_Stats s; return m != null && m.Statsserver.TryGetValue(name, out s) ? s.Value : -1f; }

    static float Stat(Entity_Master m, string name) { Link_Stats s; return m != null && m.Statsserver.TryGetValue(name, out s) ? s.Value_Calc : -1f; }

    static bool Listed(Entity_Master m, string stat, Link_Modifier mod)
    {
        List<Link_Modifier> l;
        return m.Modifiers.TryGetValue(stat, out l) && l.Contains(mod) && !mod.Dead;
    }

    // An object the mod makes, findable by name and id the way RogueWavesMode's flood stand-in is.
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
