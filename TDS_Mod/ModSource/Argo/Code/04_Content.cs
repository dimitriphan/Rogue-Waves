// Builds Argo's game data the way Blockage's own data is laid out (hero entry -> being -> entity -> visuals,
// upgrades, projectiles, commands), cloning Smacky's objects for everything the engine expects and swapping in
// Argo's art, names, stats and rules. Every player runs this identically, so online clients resolve the same names.
public static class ArgoContent
{
    public static Heroentryobject Entry;
    public static Beingobject HeroBeing;
    public static readonly Dictionary<string, Beingobject> Beings = new Dictionary<string, Beingobject>();
    public static readonly Dictionary<string, Upgradeproperties> Upgrades = new Dictionary<string, Upgradeproperties>();
    public static readonly Dictionary<string, ArgoData.Upg> UpgradeDefs = new Dictionary<string, ArgoData.Upg>();
    public static readonly Dictionary<string, Projectileobject> Projectiles = new Dictionary<string, Projectileobject>();
    public static readonly Dictionary<string, Commandobject> Commands = new Dictionary<string, Commandobject>();
    public static readonly List<Upgradeproperties> Directions = new List<Upgradeproperties>();
    public static bool IconsReady;

    static Bl_Address.Loadgroup group;
    static readonly Dictionary<string, Bl_Address.Loadlist> lists = new Dictionary<string, Bl_Address.Loadlist>();

    public static void Build(Heroentryobject template)
    {
        string folder = template.Heroloads.Objectname;
        group = new Bl_Address.Loadgroup();
        group.Items = new List<Bl_Address.Loadlist>();
        group.Loaded = true;
        group.Percent = 100;
        foreach (string label in new[] { "Hero", "Mob", "Upgrade", "Projectile", "Command" })
        {
            Bl_Address.Loadlist list = new Bl_Address.Loadlist();
            list.Loaded = true;
            list.Percent = 100;
            list.Fromgroup = group;
            group.Items.Add(list);
            lists[label] = list;
            List<Bl_Address.Loadlist> labelLists;
            if (!Bl_Address.Groups.TryGetValue(label, out labelLists))
            {
                labelLists = new List<Bl_Address.Loadlist>();
                Bl_Address.Groups[label] = labelLists;
            }
            labelLists.Add(list);
        }

        ArgoAssets.ApplyTowerMaterial(TemplateTowerMaterial());
        AddIcons();   // again, in case the early call found no icon sets yet (it skips icons already added)
        BuildProjectiles();
        BuildCommands();
        foreach (ArgoData.Build b in ArgoData.Builds)
            BuildTower(b);
        foreach (ArgoData.Crew c in ArgoData.Nauts)
            BuildNaut(c);
        BuildHero(ModKitUtil.Load<Beingobject>("Hero", folder));
        BuildEntry(template);
        BuildDirections();
        try { ArgoJournal.Build(template); }
        catch (Exception e) { ArgoLog.Warn("journal: " + e); }
        Bl_Address.Loadedfolders[ArgoData.Hero] = group;
    }

    public static string Summary()
    {
        return Beings.Count + " beings, " + Upgrades.Count + " upgrades, " + Directions.Count + " Directions, " + Projectiles.Count + " projectiles, " + Commands.Count + " commands, icons " + (IconsReady ? "ok" : "stock");
    }

    // ---------------------------------------------------------------- registration

    static void Register(string label, Bl_ScriptableObject obj)
    {
        Bl_Address.Loadlist list = lists[label];
        list.Items[obj.Objectname] = obj;
        if (!list.Index.Contains(obj))
            list.Index.Add(obj);
        FixedString64Bytes id = new FixedString64Bytes(obj.Id);
        list.Uids[id] = obj;
        obj.Loadsource = ArgoData.Hero;
        Bl_Address.Groupsname[new FixedString64Bytes(obj.Objectname)] = obj;
        Bl_Address.Groupsids[id] = obj;
    }

    // ---------------------------------------------------------------- helpers

    static Material TemplateTowerMaterial()
    {
        Beingobject archer = ModKitUtil.Load<Beingobject>("Mob", "Tower_Smackyarcher");
        if (archer == null || archer.Entity == null || archer.Entity.Defaultvisual == null)
            return null;
        GameObject prefab = archer.Entity.Defaultvisual.Default.Visprefab;
        if (prefab == null)
            return null;
        MeshRenderer r = prefab.GetComponentInChildren<MeshRenderer>(true);
        return r != null ? r.sharedMaterial : null;
    }

    // Keeps the engine's generic scripts and drops the ones written for Smacky or Buster (they hard-code their names;
    // Deep Water is cloned from Buster's Speed Pad).
    static List<UnityEngine.Object> Strip(List<UnityEngine.Object> scripts)
    {
        List<UnityEngine.Object> kept = new List<UnityEngine.Object>();
        if (scripts == null)
            return kept;
        foreach (UnityEngine.Object s in scripts)
            if (s != null && !s.name.Contains("Smacky") && !s.name.Contains("Buster") && s.name != "Entity_Ai_General_Upgrade")
                kept.Add(s);
        return kept;
    }

    static Visualproperties.Entry ModelEntry(Visualproperties.Entry from, string model, string display, string overlay)
    {
        Visualproperties.Entry e = from;
        e.Rootvisualproperty = null;
        e.Style = Visualproperties.Styles.Model;
        GameObject m = ModKitAssets.Quiet(model);
        if (m == null)
            ArgoLog.Warn("missing model " + model);
        e.Visprefab = m;
        e.Spriteobject = null;
        e.Displayname = display;
        e.Additionalmaterials = new List<Material>();
        e.Additionalvisualscripts = Strip(from.Additionalvisualscripts);
        e.Additionalprefabs = new List<GameObject>();
        GameObject extra = string.IsNullOrEmpty(overlay) ? null : ModKitAssets.Quiet(overlay);
        if (extra != null)
            e.Additionalprefabs.Add(extra);
        return e;
    }

    // A buildable's look: its model, plus (Drill Rig only) one skin per Fathom that adds the pipe stack sunk beside it.
    // The stack has four sizes (Fathom 2-5); deeper Fathoms keep the biggest, but get their own skin, which is how every
    // player knows the drill's Fathom (ArgoClient.TremorRadius).
    static Visualproperties TowerVisual(Visualproperties template, string name, string model, string display, bool fathoms)
    {
        Visualproperties vp = UnityEngine.Object.Instantiate(template);
        ArgoUtil.Retag(vp, name);
        vp.Default = ModelEntry(template.Default, model, display, null);
        vp.Skinoverride = new List<Visualproperties.Param>();
        vp.Skinoverrides = new Dictionary<string, Visualproperties.Entry>();
        if (fathoms)
            for (int f = 2; f <= ArgoData.FathomDeepest; f++)
                ModKitUtil.AddSkin(vp, "Fathom" + f, ModelEntry(template.Default, model, display, "Argo_Fathom" + Mathf.Min(f, 5)));
        return vp;
    }

    // Stats like the shipped data: "Health=40+25" is Health 40, +25 per level (Leveladd). Unknown stats are added with the
    // display settings of the first existing one (shown only to its own team, like nearly every shipped stat).
    public static Statproperties Stats(Statproperties template, string name, string[] specs)
    {
        Statproperties stats = UnityEngine.Object.Instantiate(template);
        ArgoUtil.Retag(stats, name);
        Statproperties.Defaultstatgroup like = stats.Stats.Count > 0 ? stats.Stats[0] : null;
        List<Statproperties.Defaultstatgroup> kept = new List<Statproperties.Defaultstatgroup>();
        foreach (string spec in specs)
        {
            string[] kv = spec.Split('=');
            string[] parts = kv[1].Split('+');
            Statproperties.Defaultstatgroup g = stats.Stats.Find(x => x.Name == kv[0]);
            if (g == null)
            {
                g = new Statproperties.Defaultstatgroup();
                g.Name = kv[0];
                g.Value_String = "";
                if (like != null)
                {
                    g.Style = like.Style;
                    g.Value_Type = like.Value_Type;
                    g.Display_Type = like.Display_Type;
                    g.Display_Exposed = like.Display_Exposed;
                }
            }
            g.Value = ModKitUtil.F(parts[0]);
            g.Leveladd = parts.Length > 1 ? ModKitUtil.F(parts[1]) : 0f;
            kept.Add(g);
        }
        // Only the listed stats: a template's leftovers (Projduration, Projcount...) would mean nothing on Argo's towers.
        stats.Stats = kept;
        return stats;
    }

    // ---------------------------------------------------------------- icons

    // Helper_Uitools copies these Iconobjects into its own (private) table the first time any icon is asked for, and
    // never again. So: add Argo's icons as early as possible (ArgoMod calls this as soon as the database exists, before
    // waiting for Smacky's assets), never ask for an icon while building, and only check a few seconds after every mod
    // has had its turn (CheckIcons). Other vessel mods that add theirs early too keep their icons whatever the load order.
    public static void AddIcons()
    {
        try
        {
            Tds_Masterdatabase db = Tds_Masterdatabase.Instance;
            if (db == null)
                return;
            Iconobject types = db.Getitem("Icons", "Icon_Types") as Iconobject;
            Iconobject stats = db.Getitem("Icons", "Icon_Stats") as Iconobject;
            Iconobject.Icon like = null;
            if (types != null && types.Icons != null)
                foreach (Iconobject.Icon v in types.Icons.Values) { like = v; break; }
            if (ArgoData.SonarEnabled)
                AddIcon(types, "Argo_Sonar", ArgoAssets.SonarIcon, "Sonar", like, ModKitAssets.CommandIcon);
            AddIcon(types, "Argo_Flood", ArgoAssets.FloodIcon, "Flood", like, ModKitAssets.CommandIcon);
            AddIcon(types, "Argo_Aim", ArgoAssets.AimIcon, "Aim", like, ModKitAssets.CommandIcon);
            foreach (ArgoData.Crew c in ArgoData.Nauts)
                AddIcon(types, "Argo_Crew_" + c.Name, ArgoAssets.IconFor(c.Color), c.Name, like, ModKitAssets.CommandIcon);
            AddIcon(stats, "Fathom", ArgoAssets.FathomIcon, "Fathom", like);
            IconsReady = true;
        }
        catch (Exception e)
        {
            ArgoLog.Warn("icons: " + e.Message);
        }
    }

    // Run a few seconds after boot: if the game's icon table was built before Argo's icons went in, fall back to stock
    // icons so the buttons are never blank.
    public static void CheckIcons()
    {
        bool ok = false;
        try { ok = Helper_Uitools.Geticon("Argo_Flood") != null; } catch (Exception) { }
        IconsReady = ok;
        if (ok)
        {
            ArgoLog.Info("icons: Argo's own");
            return;
        }
        foreach (KeyValuePair<string, Commandobject> kv in Commands)
            kv.Value.Icon = IconOr("", commandTemplate != null ? commandTemplate.Icon : "Attack");
        ArgoLog.Warn("icon table was built before Argo's icons were added; Argo's buttons use stock icons");
    }

    static void AddIcon(Iconobject into, string name, string[] rows, string display, Iconobject.Icon like, int box = 0)
    {
        if (into == null || into.Icons == null || into.Icons.ContainsKey(name))
            return;
        Iconobject.Icon icon = new Iconobject.Icon();
        icon.Name = name;
        icon.Image = ArgoAssets.Pixel(rows, "Icon_" + name, box);
        icon.Color = Color.white;
        icon.Material = like != null ? like.Material : null;
        icon.Displayname = display;
        into.Icons[name] = icon;
        if (into.Iconslist != null)
            into.Iconslist.Add(icon);
    }

    static string IconOr(string mine, string fallback)
    {
        if (IconsReady)
            return mine;
        foreach (string icon in new[] { fallback, "Attack", "Range", "Speed" })
        {
            Sprite s = null;
            try { s = Helper_Uitools.Geticon(icon); } catch (Exception) { }
            if (s != null)
                return icon;
        }
        return fallback;
    }

    // ---------------------------------------------------------------- projectiles

    static void BuildProjectiles()
    {
        foreach (string[] p in ArgoData.Projectiles)
        {
            Projectileobject template = ModKitUtil.Load<Projectileobject>("Projectile", p[1]);
            if (template == null)
            {
                ArgoLog.Warn("projectile template " + p[1] + " not found");
                continue;
            }
            Projectileobject proj = ArgoUtil.Clone(template, p[0]);
            GameObject model = ModKitAssets.Quiet(p[2]);
            if (model != null)
                proj.Vis = model;
            // Volleys are announced once by the tower (ArgoClient plays the boom); each pellet only lands.
            proj.Sfxstart = "";
            proj.Vfxstart = null;
            if (p[0] == ArgoData.Pellet)
                proj.Sfxend = "";
            // Straight flight (Projectile_Ai_Simple). Each shot starts from a copy of these Defaultparams with its own
            // parameters laid over them (pitfall #29); the brains pass every parameter with each shot anyway.
            proj.Defaultparams = new Dictionary<string, float> { { "Gravity", 0f }, { "Pierce", 1f }, { "Lifetime", 1f } };
            Projectiles[p[0]] = proj;
            Register("Projectile", proj);
        }
    }

    // ---------------------------------------------------------------- commands (SONAR and the crew)

    static Commandobject CommandTemplate()
    {
        Commandobject template = ModKitUtil.Load<Commandobject>("Command", "Command_Tower_Smackyhut_Convert");
        if (template == null)
            foreach (Bl_Address.Loadlist l in Bl_Address.Groups["Command"])
                foreach (UnityEngine.Object o in l.Items.Values)
                    if (template == null && o is Commandobject)
                        template = (Commandobject)o;
        return template;
    }

    static Commandobject commandTemplate;

    static void BuildCommands()
    {
        BuildOption5();
        Commandobject template = commandTemplate = CommandTemplate();
        if (template == null)
        {
            ArgoLog.Warn("no command template; AIM and the crew buttons are disabled");
            return;
        }
        if (ArgoData.SonarEnabled)
        {
            // SONAR: a vessel ability on Buster's Dash keys (Command_Movelinear); one press, no target.
            Commandobject sonar = ArgoUtil.Clone(template, ArgoData.SonarCommand);
            sonar.Style = Commandobject.Styles.Button;
            sonar.Display_Name = ArgoData.SonarName;
            sonar.Display_Desc = ArgoData.SonarDesc;
            sonar.Color = new Color(0.48f, 0.9f, 0.94f, 1f);
            sonar.Keybind = new List<string> { "6", "7" };
            sonar.Keybindstate = "Onpressed";
            sonar.Priority = 6;
            sonar.Commandfuncts = new List<Entity_Command_Base> { new ArgoSonarButton() };
            sonar.Icon = IconOr("Argo_Sonar", template.Icon);
            Commands[ArgoData.SonarLink] = sonar;
            Register("Command", sonar);
        }

        // PORT / STARBOARD / BOSUN on the Miner's Base: pick a tower, like Fie's Reroute picks an ore.
        foreach (ArgoData.Crew c in ArgoData.Nauts)
        {
            Commandobject cmd = ArgoUtil.Clone(template, c.Command);
            cmd.Style = Commandobject.Styles.Button;
            cmd.Display_Name = c.Display;
            cmd.Display_Desc = "Send " + c.Name + " to tune a tower, any player's (never a trap). Pick the Miner's Base to call " + c.Name + " home";
            cmd.Color = c.Color;
            cmd.Keybind = new List<string>();
            cmd.Keybindstate = template.Keybindstate;
            cmd.Priority = template.Priority;
            cmd.Commandfuncts = new List<Entity_Command_Base> { new ArgoCrewSelect() };
            cmd.Icon = IconOr("Argo_Crew_" + c.Name, template.Icon);
            Commands[c.Link] = cmd;
            Register("Command", cmd);
        }

        // AIM on the Drill Rig: pick a spot on the ground within its Range, like the crew pick a tower (2.0).
        Commandobject aim = ArgoUtil.Clone(template, ArgoData.AimCommand);
        aim.Style = Commandobject.Styles.Button;
        aim.Display_Name = ArgoData.AimName;
        aim.Display_Desc = ArgoData.AimDesc;
        aim.Color = ArgoData.Brass;
        aim.Keybind = new List<string>();
        aim.Keybindstate = template.Keybindstate;
        aim.Priority = template.Priority;
        aim.Commandfuncts = new List<Entity_Command_Base> { new ArgoAimSelect() };
        aim.Icon = IconOr("Argo_Aim", template.Icon);
        Commands[ArgoData.AimLink] = aim;
        Register("Command", aim);

        FloodCommand();
    }

    // Command_Buildoption1-4 are not loaded when Argo boots (the PC round of 2026-10-08: "no Command_Buildoption1 to
    // clone"); the game loads them with the match, before any vessel lands. So FLOOD and the fifth build option are
    // cloned as soon as they appear: tried at boot, then once a second from Update (LateClones) until both are done.
    // Every player clones the same objects under the same names, so the ids match online.
    static bool floodDone, option5Done, clonesLogged;
    static float nextClone;

    public static void LateClones()
    {
        if ((floodDone && option5Done) || Time.unscaledTime < nextClone)
            return;
        nextClone = Time.unscaledTime + 1f;
        if (!option5Done) BuildOption5();
        if (!floodDone) FloodCommand();
    }

    // FLOOD on Deep Water and its pools: the game's own build command (placement, cost, preview) for a pool, sent to
    // the water instead of the vessel (Fie's Grow Wall pattern, 2.0).
    static void FloodCommand()
    {
        Commandobject build1 = ModKitUtil.Load<Commandobject>("Command", "Command_Buildoption1");
        if (build1 == null)
        {
            if (!clonesLogged)
                ArgoLog.Info("Command_Buildoption1 not loaded yet; FLOOD is cloned when the match loads it");
            clonesLogged = true;
            return;
        }
        floodDone = true;
        Commandobject flood = ArgoUtil.Clone(build1, ArgoData.FloodCommand);
        flood.Display_Name = ArgoData.FloodName;
        flood.Display_Desc = ArgoData.FloodDesc;
        flood.Color = new Color(0.3f, 0.66f, 0.82f, 1f);
        flood.Icon = IconOr("Argo_Flood", build1.Icon);
        Commands[ArgoData.FloodKey] = flood;
        Register("Command", flood);
        ArgoLog.Info("FLOOD: keys " + ModKitUtil.Join(flood.Keybind ?? new List<string>()) + ", " + (flood.Commandfuncts != null ? flood.Commandfuncts.Count : 0) + " command functions");
    }

    // The game's vessels have Buildoption1-4. Argo's fifth is Command_Buildoption4 cloned under the next name and key
    // (2.0's first PC check: the server accepts any "Build*" command whose Goalstring is the tower, read in
    // Entity_Ai_Hero_Buildsell). Another mod may already have added one; then Argo uses it.
    public static string Option5Note = "";

    static void BuildOption5()
    {
        if (ModKitUtil.Load<Commandobject>("Command", ArgoData.BuildOption5) != null)
        {
            option5Done = true;
            Option5Note = "found";
            return;
        }
        Commandobject four = ModKitUtil.Load<Commandobject>("Command", "Command_Buildoption4");
        if (four == null)
        {
            Option5Note = "Command_Buildoption4 not loaded yet (cloned when the match loads it)";
            return;
        }
        option5Done = true;
        Commandobject five = ArgoUtil.Clone(four, ArgoData.BuildOption5);
        List<string> keys = new List<string>();
        if (four.Keybind != null)
            foreach (string k in four.Keybind)
                keys.Add(k.Replace("4", "5"));
        five.Keybind = keys;
        five.Priority = four.Priority + 1;
        Register("Command", five);
        Option5Note = "cloned from Command_Buildoption4 (keys " + ModKitUtil.Join(four.Keybind ?? new List<string>()) + " -> " + ModKitUtil.Join(keys) + ", priority " + five.Priority + ")";
        ArgoLog.Info("Command_Buildoption5 " + Option5Note);
    }

    // ---------------------------------------------------------------- buildables

    static void BuildTower(ArgoData.Build b)
    {
        Beingobject tb = ModKitUtil.Load<Beingobject>("Mob", b.Template);
        if ((tb == null || tb.Entity == null) && b.Trap)
        {
            ArgoLog.Warn(b.Template + " not loaded; " + b.Id + " uses " + ArgoData.TrapFallback + "'s walk-through block");
            tb = ModKitUtil.Load<Beingobject>("Mob", ArgoData.TrapFallback);
        }
        if (tb == null || tb.Entity == null)
        {
            ArgoLog.Error("tower template " + b.Template + " not found; " + b.Id + " skipped");
            return;
        }
        Entityobject te = tb.Entity;
        Beingobject being = ArgoUtil.Clone(tb, b.Id);
        Entityobject ent = ArgoUtil.Clone(te, b.Id + "_Entity");
        ent.Name = b.Id;
        ent.Display_Name = b.Display;
        ent.Display_Desc = b.Desc;
        ent.Aiscripts = Strip(te.Aiscripts);
        if (!b.Trap && !ent.Aiscripts.Exists(s => s.name == "Entity_Ai_Tower_General"))
            ent.Aiscripts.Insert(0, ModKitUtil.Script("Entity_Ai_Tower_General"));
        // The game's own upgrade panel AI on every buildable with a tree (FLOOD's pools have none). Strip drops the
        // template's copy so no other entity gets one by accident.
        if (b.Upgrades.Length > 0)
            ent.Aiscripts.Add(ModKitUtil.Script("Entity_Ai_General_Upgrade"));
        ent.Visualscripts = Strip(te.Visualscripts);
        // Its own hit prefab: the range the game draws (while placing, when selected) centred on the tower at the point
        // its brain measures from (Def.Eye), and a collider the size of the model. Argo 1.0 kept the templates': the
        // Scatter Cannon's range sat 2 tiles underground (Smacky's Bomb Tower lobs), the others 0.75 above the tower.
        // Traps keep the template's walk-through hit prefab (the kit template's Curtain).
        bool ranged = Array.Exists(b.Stats, s => s.StartsWith("Range="));
        if (!b.Trap)
            ModKitTowers.Fit(ent, b.Id, ModKitAssets.Quiet(b.ModelName), b.Eye, b.Eye + 0.05f, ranged);
        ent.Costs = ArgoUtil.Costs(b.Costs);
        ent.Upgradesinit = new List<Upgradeproperties>();
        // 2.0: traps carry no "Tower" tag (Nauts tune towers, never traps; foes don't target them).
        ent.Tags = b.Trap ? new List<string> { b.Family } : new List<string> { "Tower", b.Family };
        foreach (string tag in b.Tags)
            if (!ent.Tags.Contains(tag)) ent.Tags.Add(tag);
        if (b.Trap)
        {
            ent.Sellable = true;
            ent.Controllable = true;
        }
        ent.Defaultstat = Stats(te.Defaultstat, b.Id + "_Statproperties", b.Stats);
        Register("Mob", ent.Defaultstat);
        bool fathoms = b.Id == ArgoData.Drill;
        ent.Defaultvisual = TowerVisual(te.Defaultvisual, b.Id + "_Visualproperties", b.ModelName, b.Display, fathoms);
        Register("Mob", ent.Defaultvisual);

        // Upgrade tree, curated like the shipped ones: explicit next options per upgrade, costs from ArgoData.
        Dictionary<int, Upgradeproperties> templateTiers = new Dictionary<int, Upgradeproperties>();
        ModKitUtil.CollectUpgrades(te.Upgrades, templateTiers);
        if (templateTiers.Count == 0 && b.Upgrades.Length > 0)
        {
            Beingobject hut = ModKitUtil.Load<Beingobject>("Mob", "Tower_Smackyhut");
            if (hut != null && hut.Entity != null)
                ModKitUtil.CollectUpgrades(hut.Entity.Upgrades, templateTiers);
        }
        Dictionary<string, Upgradeproperties> byKey = new Dictionary<string, Upgradeproperties>();
        List<Upgradeproperties> roots = new List<Upgradeproperties>();
        foreach (ArgoData.Upg u in b.Upgrades)
        {
            Upgradeproperties tu;
            if (!templateTiers.TryGetValue(u.Tier, out tu))
                foreach (Upgradeproperties any in templateTiers.Values) { tu = any; break; }
            if (tu == null)
                continue;
            Upgradeproperties up = ArgoUtil.Clone(tu, u.Id);
            up.Notes = "";
            up.Display_Name = u.Display;
            up.Display_Desc = u.Desc;
            up.Priority = u.Tier;
            up.Aiscripts = new List<UnityEngine.Object>();
            up.Costs = ArgoUtil.Costs(u.Costs);
            up.Ent_Visual = TowerVisual(te.Defaultvisual, u.Id.Replace("_Upgradeobject", "_Visualproperties"), u.Model, u.Display, fathoms);
            Register("Upgrade", up.Ent_Visual);
            up.Defaultparam = new List<Upgradeproperties.Param>();
            up.Defaultparams = new Dictionary<string, float>();
            foreach (KeyValuePair<string, float> kv in ArgoUtil.Params(u.Params))
            {
                Upgradeproperties.Param p = new Upgradeproperties.Param();
                p.Name = kv.Key;
                p.Value = kv.Value;
                up.Defaultparam.Add(p);
                up.Defaultparams[kv.Key] = kv.Value;
            }
            up.Upgrades = new List<Upgradeproperties>();
            byKey[u.Key + u.Tier] = up;
            if (u.Tier == 2)
                roots.Add(up);
            Upgrades[up.Objectname] = up;
            UpgradeDefs[up.Objectname] = u;
            Register("Upgrade", up);
        }
        foreach (ArgoData.Upg u in b.Upgrades)
        {
            Upgradeproperties up, next;
            if (!byKey.TryGetValue(u.Key + u.Tier, out up))
                continue;
            foreach (string n in u.Next)
                if (byKey.TryGetValue(n.Trim(), out next))
                    up.Upgrades.Add(next);
                else
                    ArgoLog.Warn(u.Id + ": unknown next upgrade " + n);
        }
        ent.Upgrades = roots;

        being.Entity = ent;
        Beings[b.Id] = being;
        Register("Mob", being);
        Register("Mob", ent);
    }

    // ---------------------------------------------------------------- Nauts (cloned from Smacky's Engineer, a unit that walks to towers)

    static void BuildNaut(ArgoData.Crew crew)
    {
        Beingobject tb = ModKitUtil.Load<Beingobject>("Mob", "Mob_Smackyengineer") ?? ModKitUtil.Load<Beingobject>("Mob", "Mob_Smackyminer");
        if (tb == null || tb.Entity == null)
        {
            ArgoLog.Error("no unit template; " + crew.Name + " disabled");
            return;
        }
        Entityobject te = tb.Entity;
        Beingobject being = ArgoUtil.Clone(tb, crew.Being);
        Entityobject ent = ArgoUtil.Clone(te, crew.Being + "_Entity");
        ent.Name = crew.Being;
        ent.Display_Name = crew.Name;
        ent.Display_Desc = "Naut, tunes towers";
        // Mob_General and Mob_Movement only: ArgoNautBrain decides where the Naut walks (the Engineer's own AI is dropped).
        ent.Aiscripts = Strip(te.Aiscripts);
        ent.Visualscripts = Strip(te.Visualscripts);
        ent.Upgrades = new List<Upgradeproperties>();
        ent.Upgradesinit = new List<Upgradeproperties>();
        ent.Tags = new List<string> { "Mob", "Argonaut" };
        ent.Defaultstat = Stats(te.Defaultstat, crew.Being + "_Statproperties", ArgoData.NautStats);
        Register("Mob", ent.Defaultstat);
        Spriteobject templateSprite = te.Defaultvisual.Default.Spriteobject;
        Visualproperties vp = UnityEngine.Object.Instantiate(te.Defaultvisual);
        ArgoUtil.Retag(vp, crew.Being + "_Visualproperties");
        Visualproperties.Entry e = te.Defaultvisual.Default;
        e.Rootvisualproperty = null;
        e.Spriteobject = ArgoAssets.Naut(crew.Name, templateSprite, false);
        e.Displayname = crew.Name;
        e.Displaycolor = new Color(crew.Color.r, crew.Color.g, crew.Color.b, 0f);
        vp.Default = e;
        vp.Skinoverride = new List<Visualproperties.Param>();
        vp.Skinoverrides = new Dictionary<string, Visualproperties.Entry>();
        Visualproperties.Entry tune = e;
        tune.Spriteobject = ArgoAssets.Naut(crew.Name, templateSprite, true);
        ModKitUtil.AddSkin(vp, "Tune", tune);
        ent.Defaultvisual = vp;
        Register("Mob", vp);
        being.Entity = ent;
        Beings[crew.Being] = being;
        Register("Mob", being);
        Register("Mob", ent);
    }

    // ---------------------------------------------------------------- Directions (kit 1.7)

    // One Direction per rank: the mode lists them by their "Direction_" names and grants the pick; the vessel's brain reads
    // the highest rank held (ArgoServer.DirectionRank). Each carries its number as a param.
    static void BuildDirections()
    {
        Upgradeproperties template = null;
        foreach (Upgradeproperties u in Upgrades.Values) { template = u; break; }
        if (template == null)
        {
            ArgoLog.Warn("no upgrade to make the Directions from");
            return;
        }
        foreach (ArgoData.Direction[] ranks in new[] { ArgoData.Deepcurrents, ArgoData.Heavysalvos })
            foreach (ArgoData.Direction r in ranks)
            {
                Upgradeproperties d = ModKitDirections.Make(template, "Argo", r.Name, r.Display, r.Desc);
                d.Defaultparams["Bonus"] = r.Bonus;
                foreach (KeyValuePair<string, float> kv in d.Defaultparams)
                {
                    Upgradeproperties.Param p = new Upgradeproperties.Param();
                    p.Name = kv.Key;
                    p.Value = kv.Value;
                    d.Defaultparam.Add(p);
                }
                Register("Upgrade", d);
                Directions.Add(d);
            }
    }

    // ---------------------------------------------------------------- the vessel

    // The special rule, shown the way Sno Sno, Boutman and Buster show theirs: a tier -3 upgrade in Upgradesinit.
    static Upgradeproperties Tooltip()
    {
        Upgradeproperties template = null;
        foreach (Bl_Address.Loadlist l in Bl_Address.Groups["Upgrade"])
            foreach (UnityEngine.Object o in l.Items.Values)
                if (template == null && o is Upgradeproperties && !Upgrades.ContainsKey(o.name))
                    template = (Upgradeproperties)o;
        if (template == null)
            return null;
        Upgradeproperties t = ArgoUtil.Clone(template, ArgoData.HeroTooltip);
        t.Notes = "";
        t.Display_Name = ArgoData.TooltipName;
        t.Display_Desc = ArgoData.TooltipDesc;
        t.Priority = -3;
        t.Costs = new List<Link_Resourceset>();
        t.Defaultparam = new List<Upgradeproperties.Param>();
        t.Defaultparams = new Dictionary<string, float>();
        t.Aiscripts = new List<UnityEngine.Object>();
        t.Ent_Visual = null;
        t.Upgrades = new List<Upgradeproperties>();
        Register("Upgrade", t);
        return t;
    }

    static void BuildHero(Beingobject tb)
    {
        if (tb == null || tb.Entity == null)
            throw new Exception("template hero being not found");
        Entityobject te = tb.Entity;
        HeroBeing = ArgoUtil.Clone(tb, ArgoData.Hero);
        Entityobject ent = ArgoUtil.Clone(te, ArgoData.HeroEntity);
        ent.Name = ArgoData.Hero;
        ent.Display_Name = ArgoData.Displayname;
        ent.Display_Desc = ArgoData.HeroDesc;
        // A melee vessel like Smacky and Fie: the stock Entity_Ai_Mob_Combat swings, ArgoHeroBrain adds the second hit
        // (and SONAR when it is switched on). Entity_Vis_Mob_Attack turns each swing's "Attack" into the wrench animation.
        ent.Aiscripts = Strip(te.Aiscripts);
        if (!ent.Aiscripts.Exists(s => s.name == "Entity_Ai_Mob_Combat" || s.name == "Entity_Ai_Mob_Melee"))
            ent.Aiscripts.Insert(Mathf.Min(2, ent.Aiscripts.Count), ModKitUtil.Script("Entity_Ai_Mob_Combat"));
        ent.Visualscripts = Strip(te.Visualscripts);
        if (!ent.Visualscripts.Exists(s => s.name == "Entity_Vis_Mob_Attack"))
            ent.Visualscripts.Add(ModKitUtil.Script("Entity_Vis_Mob_Attack"));
        ent.Upgrades = new List<Upgradeproperties>();
        ent.Upgradesinit = new List<Upgradeproperties>();
        Upgradeproperties tooltip = Tooltip();
        if (tooltip != null)
            ent.Upgradesinit.Add(tooltip);

        Spriteobject templateSprite = te.Defaultvisual.Default.Spriteobject;
        string footstep = "";
        Spritesinlist move;
        if (templateSprite != null && templateSprite.Items != null && templateSprite.Items.TryGetValue("Move", out move))
            foreach (Spritevariantlist v in move.Sprites)
                foreach (Spritecont c in v.Variant)
                    if (string.IsNullOrEmpty(footstep) && !string.IsNullOrEmpty(c.Sound))
                        footstep = c.Sound;

        Visualproperties vp = UnityEngine.Object.Instantiate(te.Defaultvisual);
        ArgoUtil.Retag(vp, ArgoData.Hero + "_Visualproperties");
        Visualproperties.Entry e = te.Defaultvisual.Default;
        e.Rootvisualproperty = null;
        e.Spriteobject = ArgoAssets.Hero(templateSprite, footstep);
        e.Displaycolor = ArgoData.Color;
        vp.Default = e;
        vp.Skinoverride = new List<Visualproperties.Param>();
        vp.Skinoverrides = new Dictionary<string, Visualproperties.Entry>();
        ent.Defaultvisual = vp;
        Register("Hero", vp);

        // Same stat rows as Smacky's, Argo's values.
        Statproperties stats = UnityEngine.Object.Instantiate(te.Defaultstat);
        ArgoUtil.Retag(stats, ArgoData.Hero + "_Statproperties");
        foreach (KeyValuePair<string, float> kv in ArgoUtil.Params(string.Join(";", ArgoData.HeroStats)))
        {
            Statproperties.Defaultstatgroup g = stats.Stats.Find(x => x.Name == kv.Key);
            if (g == null)
                continue;
            g.Value = kv.Value;
            g.Leveladd = 0f;
        }
        ent.Defaultstat = stats;
        Register("Hero", stats);

        HeroBeing.Entity = ent;
        Register("Hero", HeroBeing);
        Register("Hero", ent);
    }

    static void BuildEntry(Heroentryobject template)
    {
        Entry = UnityEngine.Object.Instantiate(template);
        ArgoUtil.Retag(Entry, ArgoData.Hero);
        Bl_Address_Group loads = UnityEngine.Object.Instantiate(template.Heroloads);
        ArgoUtil.Retag(loads, ArgoData.Hero);
        Entry.Heroloads = loads;
        Entry.Towerdisplay = new List<string>(ArgoData.Towers);
        Entry.Displayname = ArgoData.Displayname;
        Entry.Displaysub = ArgoData.Displaysub;
        Entry.Displayskill = ArgoData.Displayskill;
        Entry.Color = ArgoData.Color;
        // Smacky's Icon is a 12x12 sprite (player lists, setup bar, leaderboards): use the matching ready-up sprite.
        Entry.Icon = ModKitAssets.Single("Readyup6argo", 12) ?? template.Icon;
        Entry.Worldsplash = ModKitAssets.Single("Argosplash", 128) ?? template.Worldsplash;
        Entry.Endscreenwinicon = ModKitAssets.Single("Argoendstat1", 32) ?? template.Endscreenwinicon;
        Entry.Endscreenloseicon = ModKitAssets.Single("Argoendstat2", 32) ?? template.Endscreenloseicon;
        Entry.Worldsprite = ModKitAssets.Worldsprite(template.Worldsprite);
        Texture fall = ModKitAssets.Sheet("ArgoFall-Sheet");
        Texture land = ModKitAssets.Sheet("ArgoIntroR-Sheet");
        Texture idle = ModKitAssets.Sheet("ArgoIdlejustidle-Sheet");
        if (fall != null) Entry.Loadingsprite = fall;
        if (land != null) Entry.Landingsprite = land;
        if (idle != null) Entry.Idlesprite = idle;
        Dictionary<string, UnityEngine.Object> heroes = ModKitUtil.Folder("Hero");
        heroes[ArgoData.Hero] = Entry;
    }
}
