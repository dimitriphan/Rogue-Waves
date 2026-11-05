// Builds Spool's game data the way Blockage's own data is laid out (hero entry -> being -> entity -> visuals,
// upgrades, projectiles, commands), cloning the shipped vessels' objects for everything the engine expects and
// swapping in Spool's art, names and rules. Every player runs this identically, so online clients resolve the same
// names. Templates: Smacky (vessel, Mining Hut for the Stash, Tin and Loom, Wall for the Pin Cushion, the miner for
// every unit), Buster's Speed Pad (the walk-through block of the Thread Head's placement token and the Tin's pin),
// Sno Sno's Snoble (the block an Overstuffed Mangled holds the line with).
public static class SpoolContent
{
    public static Heroentryobject Entry;
    public static Beingobject HeroBeing;
    public static readonly Dictionary<string, Beingobject> Beings = new Dictionary<string, Beingobject>();
    public static readonly Dictionary<string, Upgradeproperties> Upgrades = new Dictionary<string, Upgradeproperties>();
    public static readonly Dictionary<string, SpoolData.Upg> UpgradeDefs = new Dictionary<string, SpoolData.Upg>();
    public static readonly List<Upgradeproperties> Directions = new List<Upgradeproperties>();
    public static Commandobject Patch, Target, Spot, Return, Mend;
    public static Commandobject HiddenMove;   // the stock MOVE, drawn as nothing: the Thread Head keeps SPOT only (B16)
    public static int VfxRegistered;
    public static bool IconsReady;
    public static string StuffedBlock = "", TokenBlock = "", PinBlock = "";

    static Bl_Address.Loadgroup group;
    static readonly Dictionary<string, Bl_Address.Loadlist> lists = new Dictionary<string, Bl_Address.Loadlist>();

    public static void Build(Heroentryobject template)
    {
        string folder = template.Heroloads.Objectname;
        group = new Bl_Address.Loadgroup();
        group.Items = new List<Bl_Address.Loadlist>();
        group.Loaded = true;
        group.Percent = 100;
        foreach (string label in new[] { "Hero", "Mob", "Upgrade", "Projectile", "Command", "Vfx" })
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

        SpoolAssets.ApplyTowerMaterial(TemplateTowerMaterial());
        RegisterVfx();
        BuildProjectile();
        BuildCommands();
        foreach (SpoolData.Build b in SpoolData.Builds)
        {
            if (b.IsUnit) BuildHead(b);
            else BuildTower(b);
        }
        foreach (SpoolData.Unit u in SpoolData.Units)
            BuildUnit(u);
        BuildToken();
        BuildPin();
        BuildHero(ModKitUtil.Load<Beingobject>("Hero", folder));
        BuildEntry(template);
        BuildDirections();
        try { SpoolJournal.Build(template); }
        catch (Exception e) { SpoolLog.Warn("journal: " + e); }
        Bl_Address.Loadedfolders[SpoolData.Hero] = group;
    }

    public static string Summary()
    {
        return Beings.Count + " beings, " + Upgrades.Count + " upgrades, " + Directions.Count + " Directions, " + VfxRegistered + " effects, icons " + (IconsReady ? "ok" : "stock")
            + ", blocks: token '" + TokenBlock + "', pin '" + PinBlock + "', Overstuffed '" + StuffedBlock + "', towers fitted " + ModKitTowers.Fitted;
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
        obj.Loadsource = SpoolData.Hero;
        Bl_Address.Groupsname[new FixedString64Bytes(obj.Objectname)] = obj;
        Bl_Address.Groupsids[id] = obj;
    }

    // Effects the game can play by name (Vfxoneshotcheck / Vfxdebuffcheck look them up with Getload("Vfx", name)).
    static void RegisterVfx()
    {
        Bl_Address.Loadlist list = lists["Vfx"];
        foreach (GameObject go in ModKitAssets.AllVfx())
        {
            list.Items[go.name] = go;
            list.Index.Add(go);
            VfxRegistered++;
        }
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

    static List<UnityEngine.Object> Scripts(params string[] names)
    {
        List<UnityEngine.Object> l = new List<UnityEngine.Object>();
        foreach (string n in names) l.Add(ModKitUtil.Script(n));
        return l;
    }

    static Visualproperties.Entry ModelEntry(Visualproperties.Entry from, string model, string display)
    {
        Visualproperties.Entry e = from;
        e.Rootvisualproperty = null;
        e.Style = Visualproperties.Styles.Model;
        e.Visprefab = ModKitAssets.Model(model);
        e.Spriteobject = null;
        e.Displayname = display;
        e.Additionalmaterials = new List<Material>();
        e.Additionalvisualscripts = ModKitUtil.Strip(from.Additionalvisualscripts);
        e.Additionalprefabs = new List<GameObject>();
        return e;
    }

    static Visualproperties TowerVisual(Visualproperties template, string name, string model, string display, string building = null)
    {
        Visualproperties vp = UnityEngine.Object.Instantiate(template);
        SpoolUtil.Retag(vp, name);
        vp.Default = ModelEntry(template.Default, model, display);
        vp.Skinoverride = new List<Visualproperties.Param>();
        vp.Skinoverrides = new Dictionary<string, Visualproperties.Entry>();
        // A Loom under the Sewing Machine Direction: same visual, its own name in the panel (the title is the skin
        // entry's Displayname, Game_Ui), and its own model once there is one.
        if (building == SpoolData.Loom)
            ModKitUtil.AddSkin(vp, SpoolData.SewingSkin, ModelEntry(template.Default, ModKitAssets.Quiet(SpoolData.SewingModel) != null ? SpoolData.SewingModel : model, SpoolData.SewingDisplay));
        return vp;
    }

    // A unit's look: its sheet cut into Idle, Move and Attack (ModKitAssets.Unit), on the template's sprite visual.
    static Visualproperties UnitVisual(Visualproperties template, string name, string sheet, int cell, string display, Color tint)
    {
        Visualproperties vp = UnityEngine.Object.Instantiate(template);
        SpoolUtil.Retag(vp, name + "_Visualproperties");
        Visualproperties.Entry e = template.Default;
        e.Rootvisualproperty = null;
        e.Spriteobject = ModKitAssets.Unit(sheet, cell, name + "_Sprite", template.Default.Spriteobject);
        e.Displayname = display;
        e.Displaycolor = tint;
        GameObject poof = ModKitAssets.Vfx("World_Spool_Stuffing");
        if (poof != null) e.Vfxend = poof;
        vp.Default = e;
        vp.Skinoverride = new List<Visualproperties.Param>();
        vp.Skinoverrides = new Dictionary<string, Visualproperties.Entry>();
        return vp;
    }

    // "Health=25+25" -> value 25, +25 per level. Builds a stat block holding exactly these stats, each styled like the
    // template's stat of the same name, else its first stat (shown only to its own team, like every shipped tower).
    static Statproperties Stats(Statproperties template, string name, string[] spec)
    {
        Statproperties stats = UnityEngine.Object.Instantiate(template);
        SpoolUtil.Retag(stats, name);
        Statproperties.Defaultstatgroup like = template.Stats.Count > 0 ? template.Stats[0] : null;
        List<Statproperties.Defaultstatgroup> list = new List<Statproperties.Defaultstatgroup>();
        foreach (string s in spec)
        {
            string[] kv = s.Split('=');
            string[] vl = kv[1].Split('+');
            Statproperties.Defaultstatgroup g = template.Stats.Find(x => x.Name == kv[0]);
            Statproperties.Defaultstatgroup n = new Statproperties.Defaultstatgroup();
            n.Name = kv[0];
            n.Value_String = "";
            Statproperties.Defaultstatgroup src = g ?? like;
            if (src != null)
            {
                n.Style = src.Style;
                n.Value_Type = src.Value_Type;
                n.Display_Type = src.Display_Type;
                n.Display_Exposed = src.Display_Exposed;
            }
            n.Value = ModKitUtil.F(vl[0]);
            n.Leveladd = vl.Length > 1 ? ModKitUtil.F(vl[1]) : 0f;
            list.Add(n);
        }
        stats.Stats = list;
        return stats;
    }

    // The upgrade tree, curated like the shipped ones: explicit next options per upgrade, costs from SpoolData. A unit's
    // upgrades keep its look (no Ent_Visual).
    static List<Upgradeproperties> Tree(SpoolData.Build b, Entityobject te)
    {
        Dictionary<int, Upgradeproperties> templateTiers = new Dictionary<int, Upgradeproperties>();
        ModKitUtil.CollectUpgrades(te.Upgrades, templateTiers);
        if (templateTiers.Count == 0)
            ModKitUtil.CollectUpgrades(ModKitUtil.Load<Beingobject>("Mob", "Tower_Smackyhut").Entity.Upgrades, templateTiers);
        Dictionary<string, Upgradeproperties> byKey = new Dictionary<string, Upgradeproperties>();
        List<Upgradeproperties> roots = new List<Upgradeproperties>();
        foreach (SpoolData.Upg u in b.Upgrades)
        {
            Upgradeproperties tu;
            if (!templateTiers.TryGetValue(u.Tier, out tu))
                foreach (Upgradeproperties any in templateTiers.Values) { tu = any; break; }
            if (tu == null)
                continue;
            Upgradeproperties up = SpoolUtil.Clone(tu, u.Id);
            up.Notes = "";
            up.Display_Name = u.Display;
            up.Display_Desc = u.Desc;
            up.Priority = u.Tier;
            up.Aiscripts = new List<UnityEngine.Object>();
            up.Costs = ModKitUtil.Costs(u.Costs);
            if (b.IsUnit)
                up.Ent_Visual = null;
            else
            {
                up.Ent_Visual = TowerVisual(te.Defaultvisual, u.Id.Replace("_Upgradeobject", "_Visualproperties"), ModKitAssets.Quiet(u.Model) != null ? u.Model : b.Id, u.Display, b.Id);
                Register("Upgrade", up.Ent_Visual);
            }
            up.Defaultparam = new List<Upgradeproperties.Param>();
            up.Defaultparams = new Dictionary<string, float>();
            foreach (KeyValuePair<string, float> kv in ModKitUtil.Params(u.Params))
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
        foreach (SpoolData.Upg u in b.Upgrades)
        {
            Upgradeproperties up, next;
            if (!byKey.TryGetValue(u.Key + u.Tier, out up))
                continue;
            foreach (string n in u.Next)
                if (byKey.TryGetValue(n.Trim(), out next))
                    up.Upgrades.Add(next);
                else
                    SpoolLog.Warn(u.Id + ": unknown next upgrade " + n);
        }
        return roots;
    }

    // ---------------------------------------------------------------- icons

    // Helper_Uitools copies the Iconobjects into a private table the first time any icon is asked for, and never again
    // (Argo 1.0 found this). So Spool adds its icons at boot, before any asset loading, and only checks them later.
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
            AddIcon(types, "Spool_Patch", SpoolAssets.PatchIcon, "Patch", like, ModKitAssets.CommandIcon);
            AddIcon(types, "Spool_Target", SpoolAssets.PinIcon, "Target", like, ModKitAssets.CommandIcon);
            AddIcon(types, "Spool_Spot", SpoolAssets.SpotIcon, "Spot", like, ModKitAssets.CommandIcon);
            AddIcon(types, "Spool_Return", SpoolAssets.ReturnIcon, "Return", like, ModKitAssets.CommandIcon);
            AddIcon(types, "Spool_Mend", SpoolAssets.MendIcon, "Mend", like, ModKitAssets.CommandIcon);
            AddIcon(stats, "Thread", SpoolAssets.ThreadIcon, "Thread", like);
            AddIcon(stats, "Stash", SpoolAssets.StashIcon, "Stash", like);
            AddIcon(stats, "Carried", SpoolAssets.CarriedIcon, "Carried", like);
            IconsReady = true;
        }
        catch (Exception e)
        {
            SpoolLog.Warn("icons: " + e.Message);
        }
    }

    // A few seconds after boot: if the icon table was frozen before Spool's icons went in, use stock icons so the
    // buttons are never blank.
    public static void CheckIcons()
    {
        bool ok = false;
        try { ok = Helper_Uitools.Geticon("Spool_Patch") != null; } catch (Exception) { }
        IconsReady = ok;
        if (ok)
        {
            SpoolLog.Info("icons: Spool's own");
            return;
        }
        if (Patch != null) Patch.Icon = Icon("", "Heal", "Movelinear", "Move");
        if (Target != null) Target.Icon = Icon("", "Move");
        if (Spot != null) Spot.Icon = Icon("", "Move");
        if (Return != null) Return.Icon = Icon("", "Move");
        if (Mend != null) Mend.Icon = Icon("", "Heal", "Units", "Move");
        SpoolLog.Warn("icon table was built before Spool's icons were added; Spool's buttons use stock icons");
    }

    static void AddIcon(Iconobject into, string name, string[] rows, string display, Iconobject.Icon like, int box = 0)
    {
        if (into == null || into.Icons == null || into.Icons.ContainsKey(name))
            return;
        Iconobject.Icon icon = new Iconobject.Icon();
        icon.Name = name;
        icon.Image = SpoolAssets.Pixel(rows, "Icon_" + name, box);
        icon.Color = Color.white;
        icon.Material = like != null ? like.Material : null;
        icon.Displayname = display;
        into.Icons[name] = icon;
        if (into.Iconslist != null)
            into.Iconslist.Add(icon);
    }

    static string Icon(string mine, params string[] fallbacks)
    {
        if (IconsReady && mine != "")
            return mine;
        foreach (string icon in fallbacks)
        {
            Sprite s = null;
            try { s = Helper_Uitools.Geticon(icon); } catch (Exception) { }
            if (s != null)
                return icon;
        }
        return fallbacks.Length > 0 ? fallbacks[0] : mine;
    }

    // ---------------------------------------------------------------- projectile (the Dart's needle)

    static void BuildProjectile()
    {
        Projectileobject template = ModKitUtil.Load<Projectileobject>("Projectile", "Tower_Smackyarcher_Arrow");
        if (template == null)
        {
            SpoolLog.Warn("projectile template Tower_Smackyarcher_Arrow not found; Darts throw arrows");
            return;
        }
        Projectileobject proj = SpoolUtil.Clone(template, SpoolData.Needle);
        GameObject model = ModKitAssets.Model("Spool_Needle");
        if (model != null)
            proj.Vis = model;
        proj.Sfxstart = "";
        Register("Projectile", proj);
    }

    // ---------------------------------------------------------------- commands

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

    // Spool's UI colour: the sage plush 93CCA1 (Dimitri 2026-11-05; was the purple 9363AE, too close to the foes).
    internal static readonly Color Sage = new Color(0.576f, 0.8f, 0.631f, 1f);

    static Commandobject Command(Commandobject template, string id, string name, string desc, string icon, Entity_Command_Base funct, bool ability)
    {
        Commandobject c = SpoolUtil.Clone(template, id);
        c.Style = Commandobject.Styles.Button;
        c.Display_Name = name;
        c.Display_Desc = desc;
        c.Color = Sage;
        c.Keybind = ability ? new List<string> { "6", "7" } : new List<string> { "Quickaction", "5" };
        c.Keybindstate = ability ? "Onpressed" : "Onrelease";
        c.Priority = ability ? 6 : 2;
        c.Commandfuncts = new List<Entity_Command_Base> { funct };
        c.Icon = icon;
        Register("Command", c);
        return c;
    }

    static void BuildCommands()
    {
        Commandobject template = CommandTemplate();
        if (template == null)
        {
            SpoolLog.Warn("no command template; Spool's buttons are disabled");
            return;
        }
        // PATCH: a vessel ability on Buster's Dash keys; pressing it picks one of your things like Sell picks a tower.
        Patch = Command(template, SpoolData.PatchCommand, SpoolData.PatchName, SpoolData.PatchDesc, "Spool_Patch", new SpoolPatchSelect(), true);
        // TARGET and SPOT: the game's own Relocate picker (Boutman's fish): click a tile, it sends {"Position":...}.
        Target = Command(template, SpoolData.TargetCommand, SpoolData.TargetName, SpoolData.TargetDesc, "Spool_Target", new Entity_Command_Mob_Boutmanfish_Relocate(), false);
        Spot = Command(template, SpoolData.SpotCommand, SpoolData.SpotName, SpoolData.SpotDesc, "Spool_Spot", new Entity_Command_Mob_Boutmanfish_Relocate(), false);
        // RETURN and MEND: one-click buttons like the Mining Hut's Convert; MEND shows ON or OFF.
        Return = Command(template, SpoolData.ReturnCommand, SpoolData.ReturnName, SpoolData.ReturnDesc, "Spool_Return", new SpoolButton(), false);
        // Keybinds must be the game's own input actions: Game_Selection.Addcommand looks each one up in Bl_Inputs.Actions,
        // and an unknown name ("Quickaction2") threw on every click on a Thread Head, so its panel never drew (play test
        // 2026-10-26).
        Return.Keybind = new List<string> { "4" };
        Mend = Command(template, SpoolData.MendCommand, SpoolData.MendName, SpoolData.MendDesc, "Spool_Mend", new SpoolButton(), false);
        Mend.Keybind = new List<string> { "3" };
        // The Thread Head's stock MOVE did SPOT's job a second way (Dimitri's default S10 of 2026-11-05, B16). Its brain walks
        // by that command, so the command stays and only its button goes: the same command, styled None (nothing drawn).
        Commandobject move = ModKitUtil.Load<Commandobject>("Command", "Command_Move");
        if (move != null)
        {
            HiddenMove = SpoolUtil.Clone(move, SpoolData.HiddenMoveCommand);
            HiddenMove.Style = Commandobject.Styles.None;
            Register("Command", HiddenMove);
        }
        else
            SpoolLog.Warn("Command_Move not loaded: the Thread Head keeps its stock MOVE button");
    }

    // ---------------------------------------------------------------- buildables

    static void BuildTower(SpoolData.Build b)
    {
        Beingobject tb = ModKitUtil.Load<Beingobject>("Mob", b.Template);
        if (tb == null || tb.Entity == null)
        {
            SpoolLog.Error("tower template " + b.Template + " not found; " + b.Id + " skipped");
            return;
        }
        Entityobject te = tb.Entity;
        Beingobject being = SpoolUtil.Clone(tb, b.Id);
        Entityobject ent = SpoolUtil.Clone(te, b.Id + "_Entity");
        ent.Name = b.Id;
        ent.Display_Name = b.Display;
        ent.Display_Desc = b.Desc;
        ent.Aiscripts = ModKitUtil.Strip(te.Aiscripts);
        // The game's own upgrade panel AI (Strip drops the template's copy so no other entity gets one by accident).
        ent.Aiscripts.Add(ModKitUtil.Script("Entity_Ai_General_Upgrade"));
        ent.Visualscripts = ModKitUtil.Strip(te.Visualscripts);
        ent.Costs = ModKitUtil.Costs(b.Costs);
        ent.Upgradesinit = new List<Upgradeproperties>();
        ent.Sellable = true;
        ent.Controllable = true;
        ent.Tags = new List<string>(b.Tags);
        ent.Tags.Add(b.Tag);
        ent.Defaultstat = Stats(te.Defaultstat, b.Id + "_Statproperties", b.Stats);
        Register("Mob", ent.Defaultstat);
        ent.Defaultvisual = TowerVisual(te.Defaultvisual, b.Id + "_Visualproperties", b.Id, b.Display, b.Id);
        Register("Mob", ent.Defaultvisual);
        // Kit 1.2: the range sphere the game draws (and the stock AIs measure from) sits on this tower, at the height its
        // brain measures from (its Entityposition).
        ModKitTowers.Fit(ent, b.Id, ModKitAssets.Model(b.Id), 0f, 0.05f, Array.Exists(b.Stats, s => s.StartsWith("Range=")));
        ent.Upgrades = Tree(b, te);
        being.Entity = ent;
        Beings[b.Id] = being;
        Register("Mob", being);
        Register("Mob", ent);
    }

    // ---------------------------------------------------------------- units

    static readonly Color Tint = new Color(0.796f, 0.902f, 0.82f, 0f);   // pale sage CBE6D1 (pale lilac until 2026-11-05)

    static Beingobject Miner()
    {
        Beingobject tb = ModKitUtil.Load<Beingobject>("Mob", "Mob_Smackyminer");
        if (tb == null || tb.Entity == null)
            SpoolLog.Error("Mob_Smackyminer not found; Spool's units are disabled");
        return tb;
    }

    static Entityobject UnitEntity(Entityobject te, string id, string display, string desc, string[] stats, bool ranged)
    {
        Entityobject ent = SpoolUtil.Clone(te, id + "_Entity");
        ent.Name = id;
        ent.Display_Name = display;
        ent.Display_Desc = desc;
        // The game's generic fighter AIs; the brain only walks them.
        ent.Aiscripts = Scripts("Entity_Ai_Mob_General", "Entity_Ai_Mob_Movement", ranged ? "Entity_Ai_Mob_Ranged" : "Entity_Ai_Mob_Combat");
        ent.Visualscripts = Scripts("Entity_Vis_Animator_Mob", "Entity_Vis_Mob_Attack");
        ent.Upgrades = new List<Upgradeproperties>();
        ent.Upgradesinit = new List<Upgradeproperties>();
        ent.Drops = new List<Link_Resourceset>();
        ent.Costs = new List<Link_Resourceset>();
        List<string> all = new List<string>(stats);
        foreach (Statproperties.Defaultstatgroup g in te.Defaultstat.Stats)
            if (g.Name == "Distancewait") all.Add("Distancewait=" + g.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
        ent.Defaultstat = Stats(te.Defaultstat, id + "_Statproperties", all.ToArray());
        Register("Mob", ent.Defaultstat);
        return ent;
    }

    static void BuildUnit(SpoolData.Unit u)
    {
        Beingobject tb = Miner();
        if (tb == null)
            return;
        Entityobject te = tb.Entity;
        Beingobject being = SpoolUtil.Clone(tb, u.Id);
        // An Overstuffed Mangled stands on Sno Sno's mob-blocker block (the one Snobles hold the line with). Never tagged
        // Sno or Snoble (the Sno Cannon eats anything tagged Sno, kit docs/17).
        if (u.Blocker)
        {
            Beingobject snoble = ModKitUtil.Load<Beingobject>("Mob", "Mob_Snoble");
            if (snoble != null && snoble.Block != null)
            {
                being.Block = snoble.Block;
                StuffedBlock = snoble.Block.name;
            }
        }
        Entityobject ent = UnitEntity(te, u.Id, u.Display, u.Desc, u.Stats, u.Ranged);
        // A Tin's Crafted are sold back for Thread; Seams and Mangled cost nothing, and go with their building.
        ent.Sellable = u.Sellable;
        ent.Sellhealthfactor = false;
        ent.Controllable = false;
        ent.Destroyonparent = !u.Sellable;
        ent.Tags = new List<string> { "Mob", "Mobfighter", "Spoolcrafted" };
        ent.Defaultvisual = UnitVisual(te.Defaultvisual, u.Id, u.Sheet, u.Cell, u.Display, Tint);
        Register("Mob", ent.Defaultvisual);
        being.Entity = ent;
        Beings[u.Id] = being;
        Register("Mob", being);
        Register("Mob", ent);
    }

    // The Thread Head: a unit with its own upgrade tree (like Boutman's fish), selectable for its SPOT, RETURN and
    // MEND buttons. It stands on the miner's plain mob block, so foes walk past it.
    static void BuildHead(SpoolData.Build b)
    {
        Beingobject tb = Miner();
        if (tb == null)
            return;
        Entityobject te = tb.Entity;
        Beingobject being = SpoolUtil.Clone(tb, b.Id);
        Entityobject ent = UnitEntity(te, b.Id, b.Display, b.Desc, b.Stats, false);
        ent.Aiscripts.Add(ModKitUtil.Script("Entity_Ai_General_Upgrade"));   // its upgrade tree, as on a building
        ent.Costs = ModKitUtil.Costs(b.Costs);
        ent.Sellable = true;
        ent.Controllable = true;
        ent.Destroyonparent = false;
        ent.Tags = new List<string>(b.Tags);
        ent.Tags.Add(b.Tag);
        ent.Defaultvisual = UnitVisual(te.Defaultvisual, b.Id, "Spoolhead-Sheet", 24, b.Display, Tint);
        Register("Mob", ent.Defaultvisual);
        ent.Upgrades = Tree(b, te);
        being.Entity = ent;
        Beings[b.Id] = being;
        Register("Mob", being);
        Register("Mob", ent);
    }

    // The build menu places the Thread Head through this token: the Thread Head's name, cost and look on Buster's
    // walk-through pad block, so it goes on a walkable tile. The host swaps it for the Thread Head at once (SpoolServer).
    static void BuildToken()
    {
        Beingobject head;
        if (!Beings.TryGetValue(SpoolData.Head, out head))
            return;
        Beingobject pad = PadBeing();
        Beingobject being = SpoolUtil.Clone(pad ?? head, SpoolData.HeadToken);
        TokenBlock = being.Block != null ? being.Block.name : "";
        Entityobject ent = SpoolUtil.Clone(head.Entity, SpoolData.HeadToken + "_Entity");
        ent.Name = SpoolData.HeadToken;
        ent.Aiscripts = new List<UnityEngine.Object>();
        ent.Upgrades = new List<Upgradeproperties>();
        ent.Upgradesinit = new List<Upgradeproperties>();
        ent.Tags = new List<string> { "Spoolheadtoken" };
        ent.Sellable = false;
        ent.Controllable = false;
        ent.Targetable = false;
        being.Entity = ent;
        Beings[SpoolData.HeadToken] = being;
        Register("Mob", being);
        Register("Mob", ent);
    }

    static Beingobject PadBeing()
    {
        Beingobject pad = ModKitUtil.Load<Beingobject>("Mob", "Trap_Busterpad");
        if (pad == null || pad.Entity == null)
        {
            SpoolLog.Warn("Trap_Busterpad not loaded; the Thread Head token and the Tin's pin use Perma-Wall's walk-through block");
            pad = ModKitUtil.Load<Beingobject>("Mob", "Trap_Smackywall_Perma");
        }
        return pad != null && pad.Entity != null ? pad : null;
    }

    // The Cookie Tin's pin: a walk-through marker every player sees on the spot the Tin sends its Crafted to.
    static void BuildPin()
    {
        Beingobject pad = PadBeing();
        if (pad == null)
            return;
        Entityobject te = pad.Entity;
        Beingobject being = SpoolUtil.Clone(pad, SpoolData.Pin);
        PinBlock = being.Block != null ? being.Block.name : "";
        Entityobject ent = SpoolUtil.Clone(te, SpoolData.Pin + "_Entity");
        ent.Name = SpoolData.Pin;
        ent.Display_Name = "Cookie Tin Spot";
        ent.Display_Desc = "Crafted from this Tin run here";
        ent.Aiscripts = new List<UnityEngine.Object>();
        ent.Visualscripts = ModKitUtil.Strip(te.Visualscripts);
        ent.Upgrades = new List<Upgradeproperties>();
        ent.Upgradesinit = new List<Upgradeproperties>();
        ent.Costs = new List<Link_Resourceset>();
        ent.Drops = new List<Link_Resourceset>();
        ent.Tags = new List<string> { "Spoolpin" };
        ent.Sellable = false;
        ent.Controllable = false;
        ent.Targetable = false;
        ent.Destroyonparent = true;
        ent.Defaultstat = Stats(te.Defaultstat, SpoolData.Pin + "_Statproperties", new[] { "Health=10" });
        Register("Mob", ent.Defaultstat);
        ent.Defaultvisual = TowerVisual(te.Defaultvisual, SpoolData.Pin + "_Visualproperties", "Spool_Pin", "Cookie Tin Spot");
        Register("Mob", ent.Defaultvisual);
        being.Entity = ent;
        Beings[SpoolData.Pin] = being;
        Register("Mob", being);
        Register("Mob", ent);
    }

    // ---------------------------------------------------------------- Directions (kit 1.7, docs/scope/11)

    // One Direction per rank: the mode lists them by their "Direction_" names and grants the pick; the vessel's brain reads
    // the highest rank held (SpoolHeroBrain.CheckDirections). Each carries its numbers as params; Clash 1 is one per group.
    static void BuildDirections()
    {
        Upgradeproperties template = null;
        foreach (Upgradeproperties u in Upgrades.Values) { template = u; break; }
        if (template == null)
        {
            SpoolLog.Warn("no upgrade to make the Directions from");
            return;
        }
        foreach (SpoolData.Recycler r in SpoolData.Recyclers)
        {
            Upgradeproperties d = ModKitDirections.Make(template, "Spool", r.Name, r.Display, r.Desc);
            d.Defaultparams["Recycler_Percent"] = r.Percent;
            if (r.Heal > 0f)
            {
                d.Defaultparams["Recycler_Heal"] = r.Heal;
                d.Defaultparams["Recycler_Radius"] = r.Radius;
            }
            AddDirection(d);
        }
        foreach (SpoolData.Sewing s in SpoolData.Sewingmachines)
        {
            Upgradeproperties d = ModKitDirections.Make(template, "Spool", s.Name, s.Display, s.Desc);
            d.Defaultparams["Clash"] = 1f;
            d.Defaultparams["Sewing_Mine"] = SpoolData.SewingMine;
            d.Defaultparams["Sewing_Radius"] = s.Radius;
            d.Defaultparams["Sewing_Heal"] = s.Heal;
            AddDirection(d);
        }
    }

    static void AddDirection(Upgradeproperties d)
    {
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
        Upgradeproperties t = SpoolUtil.Clone(template, SpoolData.HeroTooltip);
        t.Notes = "";
        t.Display_Name = SpoolData.TooltipName;
        t.Display_Desc = SpoolData.TooltipDesc;
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
        HeroBeing = SpoolUtil.Clone(tb, SpoolData.Hero);
        Entityobject ent = SpoolUtil.Clone(te, SpoolData.HeroEntity);
        ent.Name = SpoolData.Hero;
        ent.Display_Name = SpoolData.Displayname;
        ent.Display_Desc = SpoolData.HeroDesc;
        // A melee vessel is built like Smacky: Mob_General, Mob_Movement, the stock Mob_Combat (targets, mines ore and
        // sends "Attack", which Entity_Vis_Mob_Attack turns into the attack animation) and Hero_Buildsell.
        ent.Aiscripts = ModKitUtil.Strip(te.Aiscripts);
        if (!ent.Aiscripts.Exists(s => s.name == "Entity_Ai_Mob_Combat"))
            ent.Aiscripts.Insert(Mathf.Min(2, ent.Aiscripts.Count), ModKitUtil.Script("Entity_Ai_Mob_Combat"));
        ent.Visualscripts = ModKitUtil.Strip(te.Visualscripts);
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
        SpoolUtil.Retag(vp, SpoolData.Hero + "_Visualproperties");
        Visualproperties.Entry e = te.Defaultvisual.Default;
        e.Rootvisualproperty = null;
        e.Spriteobject = SpoolAssets.Hero(templateSprite, footstep);
        e.Displaycolor = Sage;
        vp.Default = e;
        vp.Skinoverride = new List<Visualproperties.Param>();
        vp.Skinoverrides = new Dictionary<string, Visualproperties.Entry>();
        ent.Defaultvisual = vp;
        Register("Hero", vp);

        ent.Defaultstat = Stats(te.Defaultstat, SpoolData.Hero + "_Statproperties", SpoolData.HeroStats);
        Register("Hero", ent.Defaultstat);

        HeroBeing.Entity = ent;
        Register("Hero", HeroBeing);
        Register("Hero", ent);
    }

    static void BuildEntry(Heroentryobject template)
    {
        Entry = UnityEngine.Object.Instantiate(template);
        SpoolUtil.Retag(Entry, SpoolData.Hero);
        Bl_Address_Group loads = UnityEngine.Object.Instantiate(template.Heroloads);
        SpoolUtil.Retag(loads, SpoolData.Hero);
        Entry.Heroloads = loads;
        Entry.Towerdisplay = new List<string>(SpoolData.Towerdisplay);
        Entry.Displayname = SpoolData.Displayname;
        Entry.Displaysub = SpoolData.Displaysub;
        Entry.Displayskill = SpoolData.Displayskill;
        Entry.Color = Sage;
        // The 12x12 sprite in player lists, the setup bar and leaderboards: the ready-up portrait in its brackets.
        Entry.Icon = ModKitAssets.Single("Readyup6spool", 12) ?? template.Icon;
        Entry.Worldsplash = ModKitAssets.Single("Spoolsplash", 128) ?? template.Worldsplash;
        Entry.Endscreenwinicon = ModKitAssets.Single("Spoolendstat1", 32) ?? template.Endscreenwinicon;
        Entry.Endscreenloseicon = ModKitAssets.Single("Spoolendstat2", 32) ?? template.Endscreenloseicon;
        Entry.Worldsprite = ModKitAssets.Worldsprite(template.Worldsprite);
        Texture fall = ModKitAssets.Sheet("SpoolFall-Sheet");
        Texture land = ModKitAssets.Sheet("SpoolIntroR-Sheet");
        Texture idle = ModKitAssets.Sheet("SpoolIdlejustidle-Sheet");
        if (fall != null) Entry.Loadingsprite = fall;
        if (land != null) Entry.Landingsprite = land;
        if (idle != null) Entry.Idlesprite = idle;
        Dictionary<string, UnityEngine.Object> heroes = ModKitUtil.Folder("Hero");
        heroes[SpoolData.Hero] = Entry;
    }
}
