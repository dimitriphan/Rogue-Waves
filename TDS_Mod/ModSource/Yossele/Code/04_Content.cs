// Builds Yossele's game data the way Blockage's own data is laid out (hero entry -> being -> entity -> visuals,
// upgrades, commands), cloning the shipped vessels' objects for everything the engine expects and swapping in Yossele's
// art, names and rules. Every player runs this identically, so online clients resolve the same names. Templates:
// Smacky (vessel, Bomb Tower -> Mushroom Cannon, Archer Tower -> Dandelion Tree and Bonsai, Mining Hut -> Stonehenge
// and Pachira Sapling, Mining Hut's miner -> Cordyfriend), Sno Sno (its snow is the ground layer of vines and fruit,
// Smacky's Bramble without it; its snowball is the laser; its Snoble lends the Cordyfriend its look and block).
public static class YosseleContent
{
    public static Heroentryobject Entry;
    public static Beingobject HeroBeing;
    public static readonly Dictionary<string, Beingobject> Beings = new Dictionary<string, Beingobject>();
    public static readonly Dictionary<string, Upgradeproperties> Upgrades = new Dictionary<string, Upgradeproperties>();
    public static readonly Dictionary<string, YosseleData.Upg> UpgradeDefs = new Dictionary<string, YosseleData.Upg>();
    public static Commandobject Fertilize, Colour;
    public static readonly List<Upgradeproperties> Compostbins = new List<Upgradeproperties>();
    public static readonly List<Upgradeproperties> Treeshifts = new List<Upgradeproperties>();
    public static int VfxRegistered;
    public static bool IconsReady, LaserReady;
    public static string GroundFrom = "", LaserFrom = "", FriendFrom = "";
    public static bool FriendOwnLook;

    // Leaf green 72CC35, the accent of Yossele's sprout (docs/STYLE.md).
    public static readonly Color Accent = new Color(0x72 / 255f, 0xCC / 255f, 0x35 / 255f, 1f);

    static Bl_Address.Loadgroup group;
    static readonly Dictionary<string, Bl_Address.Loadlist> lists = new Dictionary<string, Bl_Address.Loadlist>();

    public static void Build(Heroentryobject template)
    {
        string folder = template.Heroloads.Objectname;
        group = new Bl_Address.Loadgroup();
        group.Items = new List<Bl_Address.Loadlist>();
        group.Loaded = true;
        group.Percent = 100;
        foreach (string label in new[] { "Hero", "Mob", "Upgrade", "Command", "Vfx", "Projectile" })
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

        YosseleAssets.ApplyTowerMaterial(TemplateTowerMaterial());
        RegisterVfx();
        BuildLaser();
        BuildCommands();
        foreach (YosseleData.Build b in YosseleData.Builds)
            BuildTower(b);
        BuildGround(YosseleData.Vine, "Yosselevine", "Vines", "Yossele_Vine", null);
        BuildGround(YosseleData.Fruit, "Yosselefruit", "Aura Fruit", "Yossele_Fruit", new[] { "Redaura", "Blueaura", "Yellowaura" });
        BuildFriend();
        BuildDirections();
        BuildHero(ModKitUtil.Load<Beingobject>("Hero", folder));
        BuildEntry(template);
        try { YosseleJournal.Build(template); }
        catch (Exception e) { YosseleLog.Warn("journal: " + e); }
        Bl_Address.Loadedfolders[YosseleData.Hero] = group;
    }

    public static string Summary()
    {
        return Beings.Count + " beings, " + Upgrades.Count + " upgrades, " + (Compostbins.Count + Treeshifts.Count) + " Directions, " + VfxRegistered + " effects, icons "
            + (IconsReady ? "ok" : "stock") + ", ground from " + GroundFrom + ", laser from " + LaserFrom + ", Cordyfriend from " + FriendFrom
            + ", towers fitted " + ModKitTowers.Fitted;
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
        obj.Loadsource = YosseleData.Hero;
        Bl_Address.Groupsname[new FixedString64Bytes(obj.Objectname)] = obj;
        Bl_Address.Groupsids[id] = obj;
    }

    // Effects the game plays by name (Vfxdebuffcheck / Vfxoneshotcheck look them up with Getload("Vfx", name)), so the
    // spore, daze, tangle and barrier looks are attached and timed by the game on every player's machine.
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

    // The game's two facing arrows, as Sno Sno's Ice Wall (Tower_Snosnowall) has them: the build command draws an arrow
    // on the placement border when the tower's hit prefab has a "Forward" child (Entity_Command_Build), and
    // Entity_Vis_Tower_Orientation shows one under the turned model while it is selected. The marker copies the Ice
    // Wall's own place when it can be read. Call after ModKitTowers.Fit (the hit prefab is then the Stonehenge's own).
    public static string ForwardFrom = "not set";

    static void ShowFacing(Entityobject ent)
    {
        GameObject hit = ent.Defaulthit != null ? ent.Defaulthit.Default.Entityprefab : null;
        if (hit != null && Extramethods.FindDeepChild(hit.transform, "Forward") == null)
        {
            Transform mark = new GameObject("Forward").transform;
            mark.SetParent(hit.transform, false);
            Beingobject wall = ModKitUtil.Load<Beingobject>("Mob", "Tower_Snosnowall");
            GameObject wallHit = wall != null && wall.Entity != null && wall.Entity.Defaulthit != null ? wall.Entity.Defaulthit.Default.Entityprefab : null;
            Transform theirs = wallHit != null ? Extramethods.FindDeepChild(wallHit.transform, "Forward") : null;
            if (theirs != null)
            {
                mark.localPosition = theirs.localPosition;
                mark.localRotation = theirs.localRotation;
            }
            ForwardFrom = theirs != null ? "the Ice Wall's at " + theirs.localPosition.ToString("F2") : "the tile centre (the Ice Wall has none)";
        }
        if (!ent.Visualscripts.Exists(s => s != null && s.name == "Entity_Vis_Tower_Orientation"))
            ent.Visualscripts.Add(ModKitUtil.Script("Entity_Vis_Tower_Orientation"));
        YosseleLog.Info("Stonehenge facing arrows: Forward from " + ForwardFrom);
    }

    static List<UnityEngine.Object> Scripts(params string[] names)
    {
        List<UnityEngine.Object> l = new List<UnityEngine.Object>();
        foreach (string n in names) l.Add(ModKitUtil.Script(n));
        return l;
    }

    static Visualproperties.Entry ModelEntry(Visualproperties.Entry from, string model, string display, string extra, string fallback = null)
    {
        Visualproperties.Entry e = from;
        e.Rootvisualproperty = null;
        e.Style = Visualproperties.Styles.Model;
        e.Visprefab = ModKitAssets.Model(model);
        if (e.Visprefab == null && !string.IsNullOrEmpty(fallback))
            e.Visprefab = ModKitAssets.Model(fallback);
        e.Spriteobject = null;
        e.Displayname = display;
        e.Additionalmaterials = new List<Material>();
        e.Additionalvisualscripts = ModKitUtil.Strip(from.Additionalvisualscripts);
        e.Additionalprefabs = new List<GameObject>();
        if (!string.IsNullOrEmpty(extra))
        {
            GameObject x = ModKitAssets.Model(extra);
            if (x != null) e.Additionalprefabs.Add(x);
        }
        return e;
    }

    // A building's look: its model, plus one skin per Growth stage ("Growth1".."Growth<max>") that adds the sprouts and
    // leaves round its foot (Yossele_Growth<n>; stages past the last drawn one reuse it), on the base model and every
    // upgrade's. The skin is synced state, so every player sees the stage. An upgrade without a model of its own shows
    // the building's.
    static Visualproperties TowerVisual(Visualproperties template, string name, string model, string display, int stages, string fallback)
    {
        Visualproperties vp = UnityEngine.Object.Instantiate(template);
        YosseleUtil.Retag(vp, name);
        vp.Default = ModelEntry(template.Default, model, display, null, fallback);
        vp.Skinoverride = new List<Visualproperties.Param>();
        vp.Skinoverrides = new Dictionary<string, Visualproperties.Entry>();
        for (int g = 1; g <= stages; g++)
            ModKitUtil.AddSkin(vp, YosseleData.GrowthStat + g, ModelEntry(template.Default, model, display, "Yossele_Growth" + Mathf.Min(g, YosseleData.GrowthSkins), fallback));
        return vp;
    }

    // "Health=25+25" -> value 25, +25 per level. Builds a stat block holding exactly these stats, each styled like the
    // template's stat of the same name, else its first stat (shown only to its own team, like every shipped tower).
    static Statproperties Stats(Statproperties template, string name, string[] spec)
    {
        Statproperties stats = UnityEngine.Object.Instantiate(template);
        YosseleUtil.Retag(stats, name);
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

    // ---------------------------------------------------------------- icons

    // Helper_Uitools copies the Iconobjects into a private table the first time any icon is asked for, and never again
    // (Argo 1.0 found this). So Yossele adds its icons at boot, before any asset loading, and only checks them later.
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
            AddIcon(types, "Yossele_Fertilize", YosseleAssets.FertilizeIcon, "Fertilize", like);
            AddIcon(types, "Yossele_Colour", YosseleAssets.ColourIcon, "Colour", like);
            AddIcon(stats, YosseleData.FertilizerStat, YosseleAssets.FertilizerIcon, "Fertilizer", like);
            AddIcon(stats, YosseleData.GrowthStat, YosseleAssets.GrowthIcon, "Growth", like);
            AddIcon(stats, "Seeds", YosseleAssets.SeedIcon, "Seeds", like);
            IconsReady = true;
        }
        catch (Exception e)
        {
            YosseleLog.Warn("icons: " + e.Message);
        }
    }

    // A few seconds after boot: if the icon table was frozen before Yossele's icons went in, use stock icons so the
    // buttons are never blank.
    public static void CheckIcons()
    {
        bool ok = false;
        try { ok = Helper_Uitools.Geticon("Yossele_Fertilize") != null; } catch (Exception) { }
        IconsReady = ok;
        if (ok)
        {
            YosseleLog.Info("icons: Yossele's own");
            return;
        }
        if (Fertilize != null) Fertilize.Icon = Icon("", "Heal", "Upgrade", "Move");
        if (Colour != null) Colour.Icon = Icon("", "Convert", "Upgrade", "Move");
        YosseleLog.Warn("icon table was built before Yossele's icons were added; FERTILIZE and COLOUR use stock icons");
    }
    static void AddIcon(Iconobject into, string name, string[] rows, string display, Iconobject.Icon like)
    {
        if (into == null || into.Icons == null || into.Icons.ContainsKey(name))
            return;
        Iconobject.Icon icon = new Iconobject.Icon();
        icon.Name = name;
        icon.Image = YosseleAssets.Pixel(rows, "Icon_" + name);
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

    // ---------------------------------------------------------------- the laser

    // Yossele's green mouth laser: Sno Sno's homing snowball (the kit's shot for any ranged hero) with a green bolt for
    // its look and the Archer arrow's end look and sound. The Archer's arrow is the template without Sno Sno's group.
    static void BuildLaser()
    {
        Projectileobject template = ModKitUtil.Load<Projectileobject>("Projectile", YosseleData.LaserTemplate);
        LaserFrom = YosseleData.LaserTemplate;
        if (template == null)
        {
            template = ModKitUtil.Load<Projectileobject>("Projectile", YosseleData.LaserFallback);
            LaserFrom = YosseleData.LaserFallback;
        }
        if (template == null)
        {
            LaserFrom = "none";
            YosseleLog.Warn("no projectile template (" + YosseleData.LaserTemplate + ", " + YosseleData.LaserFallback + "); Yossele shoots the Archer's arrow");
            return;
        }
        Projectileobject laser = YosseleUtil.Clone(template, YosseleData.Laser);
        GameObject model = ModKitAssets.Model("Yossele_Laser");
        if (model != null)
            laser.Vis = model;
        laser.Sfxstart = "";
        Projectileobject arrow = ModKitUtil.Load<Projectileobject>("Projectile", YosseleData.LaserFallback);
        if (arrow != null && template != arrow)
        {
            laser.Vfxend = arrow.Vfxend;
            laser.Sfxend = arrow.Sfxend;
        }
        Register("Projectile", laser);
        LaserReady = true;
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

    static void BuildCommands()
    {
        Commandobject template = CommandTemplate();
        if (template == null)
        {
            YosseleLog.Warn("no command template; FERTILIZE and COLOUR disabled");
            return;
        }
        // FERTILIZE: a vessel ability on Buster's Dash keys; pressing it picks one of your buildings like Sell picks a
        // tower. The vessel only gets the command once a Compost Bin is held (YosseleHeroBrain.CheckFertilize).
        Fertilize = YosseleUtil.Clone(template, YosseleData.FertilizeCommand);
        Fertilize.Style = Commandobject.Styles.Button;
        Fertilize.Display_Name = YosseleData.FertilizeName;
        Fertilize.Display_Desc = YosseleData.FertilizeDesc;
        Fertilize.Color = Accent;
        Fertilize.Keybind = new List<string> { "6", "7" };
        Fertilize.Keybindstate = "Onpressed";
        Fertilize.Priority = 6;
        Fertilize.Commandfuncts = new List<Entity_Command_Base> { new YosseleFertilizeSelect() };
        Fertilize.Icon = "Yossele_Fertilize";
        Register("Command", Fertilize);

        // COLOUR on a Pachira Sapling with Grafted: a one-click button like the Mining Hut's Convert that cycles
        // the aura its fruit pays (All, Red, Blue, Yellow), kept in the command's Goalstring for every player.
        Colour = YosseleUtil.Clone(template, YosseleData.ColourCommand);
        Colour.Style = Commandobject.Styles.Button;
        Colour.Display_Name = YosseleData.ColourName;
        Colour.Display_Desc = YosseleData.ColourDesc;
        Colour.Color = Accent;
        Colour.Keybind = new List<string> { "Quickaction", "5" };
        Colour.Keybindstate = "Onrelease";
        Colour.Priority = 2;
        Colour.Commandfuncts = new List<Entity_Command_Base> { new YosseleColourButton() };
        Colour.Icon = "Yossele_Colour";
        Register("Command", Colour);
    }

    // ---------------------------------------------------------------- buildables

    static void BuildTower(YosseleData.Build b)
    {
        Beingobject tb = ModKitUtil.Load<Beingobject>("Mob", b.Template);
        if (tb == null || tb.Entity == null)
        {
            YosseleLog.Warn("tower template " + b.Template + " not found; " + b.Id + " uses the Mining Hut");
            tb = ModKitUtil.Load<Beingobject>("Mob", "Tower_Smackyhut");
        }
        if (tb == null || tb.Entity == null)
        {
            YosseleLog.Error("no tower template for " + b.Id + "; skipped");
            return;
        }
        Entityobject te = tb.Entity;
        Beingobject being = YosseleUtil.Clone(tb, b.Id);
        Entityobject ent = YosseleUtil.Clone(te, b.Id + "_Entity");
        ent.Name = b.Id;
        ent.Display_Name = b.Display;
        ent.Display_Desc = b.Desc;
        ent.Aiscripts = ModKitUtil.Strip(te.Aiscripts);
        if (!ent.Aiscripts.Exists(s => s.name == "Entity_Ai_Tower_General"))
            ent.Aiscripts.Insert(0, ModKitUtil.Script("Entity_Ai_Tower_General"));
        // The game's own upgrade panel AI (Strip drops the template's copy so no other entity gets one by accident).
        ent.Aiscripts.Add(ModKitUtil.Script("Entity_Ai_General_Upgrade"));
        ent.Visualscripts = ModKitUtil.Strip(te.Visualscripts);
        // Its own hit prefab: the range the game draws centred on the tower's tile, where its brain measures from, and
        // a collider the size of its model (the templates' sat 2 tiles under the Bomb Tower, Argo 1.0).
        bool ranged = Array.Exists(b.Stats, x => x.StartsWith("Range="));
        // The Bonsai shows the Tree's model if its own is missing.
        string fallback = b.Id == YosseleData.Bonsai ? YosseleData.Dandelion : null;
        ModKitTowers.Fit(ent, b.Id, ModKitAssets.Model(b.Id) ?? (fallback != null ? ModKitAssets.Model(fallback) : null), 0f, b.Muzzle, ranged);
        ent.Costs = ModKitUtil.Costs(b.Costs);
        ent.Upgradesinit = new List<Upgradeproperties>();
        ent.Sellable = true;
        ent.Controllable = true;
        // The Stonehenge faces the way the player turns it while placing it (every shipped tower can be turned), and
        // shows that facing the way Sno Sno's Ice Wall does.
        if (b.Id == YosseleData.Henge)
        {
            ent.Orientable = true;
            ShowFacing(ent);
        }
        ent.Tags = new List<string>(b.Tags);
        ent.Tags.Add(b.Tag);
        ent.Defaultstat = Stats(te.Defaultstat, b.Id + "_Statproperties", b.Stats);
        Register("Mob", ent.Defaultstat);
        ent.Defaultvisual = TowerVisual(te.Defaultvisual, b.Id + "_Visualproperties", b.Id, b.Display, b.Max, fallback);
        Register("Mob", ent.Defaultvisual);

        // Upgrade tree, curated like the shipped ones: explicit next options per upgrade, costs from YosseleData.
        Dictionary<int, Upgradeproperties> templateTiers = new Dictionary<int, Upgradeproperties>();
        ModKitUtil.CollectUpgrades(te.Upgrades, templateTiers);
        if (templateTiers.Count == 0)
        {
            Beingobject hut = ModKitUtil.Load<Beingobject>("Mob", "Tower_Smackyhut");
            if (hut != null && hut.Entity != null)
                ModKitUtil.CollectUpgrades(hut.Entity.Upgrades, templateTiers);
        }
        Dictionary<string, Upgradeproperties> byKey = new Dictionary<string, Upgradeproperties>();
        List<Upgradeproperties> roots = new List<Upgradeproperties>();
        foreach (YosseleData.Upg u in b.Upgrades)
        {
            Upgradeproperties tu;
            if (!templateTiers.TryGetValue(u.Tier, out tu))
                foreach (Upgradeproperties any in templateTiers.Values) { tu = any; break; }
            if (tu == null)
                continue;
            Upgradeproperties up = YosseleUtil.Clone(tu, u.Id);
            up.Notes = "";
            up.Display_Name = u.Display;
            up.Display_Desc = u.Desc;
            up.Priority = u.Tier;
            up.Aiscripts = new List<UnityEngine.Object>();
            up.Costs = ModKitUtil.Costs(u.Costs);
            up.Ent_Visual = TowerVisual(te.Defaultvisual, u.Id.Replace("_Upgradeobject", "_Visualproperties"), u.Model, u.Display, b.Max, b.Id);
            Register("Upgrade", up.Ent_Visual);
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
        foreach (YosseleData.Upg u in b.Upgrades)
        {
            Upgradeproperties up, next;
            if (!byKey.TryGetValue(u.Key + u.Tier, out up))
                continue;
            foreach (string n in u.Next)
                if (byKey.TryGetValue(n.Trim(), out next))
                    up.Upgrades.Add(next);
                else
                    YosseleLog.Warn(u.Id + ": unknown next upgrade " + n);
        }
        ent.Upgrades = roots;

        being.Entity = ent;
        Beings[b.Id] = being;
        Register("Mob", being);
        Register("Mob", ent);
    }

    // ---------------------------------------------------------------- vines and aura fruit (networked ground beings)

    // Cloned from Sno Sno's snow (a ground-level block that sits under buildings) without its tag, AI or upgrades: the
    // Sno Cannon eats anything tagged Sno (docs/17), and the snow's AI drains and despawns it. Nothing in it acts; the
    // server's vine and fruit code reads where they stand. Fruit has one skin per single colour it can pay.
    static void BuildGround(string id, string tag, string display, string model, string[] colourSkins)
    {
        Beingobject tb = ModKitUtil.Load<Beingobject>("Mob", YosseleData.GroundTemplate);
        string from = YosseleData.GroundTemplate;
        if (tb == null || tb.Entity == null)
        {
            tb = ModKitUtil.Load<Beingobject>("Mob", YosseleData.GroundFallback);
            from = YosseleData.GroundFallback;
        }
        if (tb == null || tb.Entity == null)
        {
            YosseleLog.Error("no ground template for " + id + " (neither " + YosseleData.GroundTemplate + " nor " + YosseleData.GroundFallback + ")");
            return;
        }
        GroundFrom = from + (tb.Block != null ? " (" + tb.Block.name + ")" : "");
        Entityobject te = tb.Entity;
        Beingobject being = YosseleUtil.Clone(tb, id);
        Entityobject ent = YosseleUtil.Clone(te, id + "_Entity");
        ent.Name = tag;
        ent.Display_Name = display;
        ent.Display_Desc = "";
        ent.Aiscripts = new List<UnityEngine.Object>();
        ent.Visualscripts = ModKitUtil.Strip(te.Visualscripts);
        ent.Upgrades = new List<Upgradeproperties>();
        ent.Upgradesinit = new List<Upgradeproperties>();
        ent.Costs = new List<Link_Resourceset>();
        ent.Drops = new List<Link_Resourceset>();
        ent.Sellable = false;
        ent.Controllable = false;
        // Kept when the Stonehenge or Sapling that made it goes: a shared vine is released by code (YosseleVines), and
        // fruit stays until picked up.
        ent.Destroyonparent = false;
        ent.Tags = new List<string> { tag };
        ent.Defaultstat = Stats(te.Defaultstat, id + "_Statproperties", new[] { "Level=1" });
        Register("Mob", ent.Defaultstat);
        Visualproperties vp = UnityEngine.Object.Instantiate(te.Defaultvisual);
        YosseleUtil.Retag(vp, id + "_Visualproperties");
        vp.Default = ModelEntry(te.Defaultvisual.Default, model, display, null);
        vp.Skinoverride = new List<Visualproperties.Param>();
        vp.Skinoverrides = new Dictionary<string, Visualproperties.Entry>();
        if (colourSkins != null)
            foreach (string c in colourSkins)
                ModKitUtil.AddSkin(vp, c, ModelEntry(te.Defaultvisual.Default, model + "_" + c.Replace("aura", ""), display, null));
        ent.Defaultvisual = vp;
        Register("Mob", vp);
        being.Entity = ent;
        Beings[id] = being;
        Register("Mob", being);
        Register("Mob", ent);
    }

    // ---------------------------------------------------------------- the Cordyfriend (Cordyceps)

    // The template's Understudy recipe: the Mining Hut's miner (a walking fighter with Mob_General, Mob_Movement and the stock
    // melee AI), standing on the Snoble's mob-blocker block so foes stop at it. Its look is its own sheet on the miner's
    // sprite visual (temporary art, YosseleAssets.Friend); without the sheet, the Snoble's look tinted on every client by
    // YosseleClient, as before. Never tagged Sno or Snoble (docs/17: the Sno Cannon eats anything Sno).
    static void BuildFriend()
    {
        Beingobject tb = ModKitUtil.Load<Beingobject>("Mob", YosseleData.FriendTemplate);
        if (tb == null || tb.Entity == null)
        {
            FriendFrom = "none";
            YosseleLog.Error(YosseleData.FriendTemplate + " not found; Cordyceps raises nothing");
            return;
        }
        Beingobject snoble = ModKitUtil.Load<Beingobject>("Mob", YosseleData.FriendLook);
        Entityobject te = tb.Entity;
        Beingobject being = YosseleUtil.Clone(tb, YosseleData.Friend);
        if (snoble != null && snoble.Block != null)
            being.Block = snoble.Block;
        Entityobject ent = YosseleUtil.Clone(te, YosseleData.Friend + "_Entity");
        ent.Name = YosseleData.Friend;
        ent.Display_Name = YosseleData.FriendDisplay;
        ent.Display_Desc = YosseleData.FriendDesc;
        ent.Aiscripts = Scripts("Entity_Ai_Mob_General", "Entity_Ai_Mob_Movement", "Entity_Ai_Mob_Combat");
        ent.Upgrades = new List<Upgradeproperties>();
        ent.Upgradesinit = new List<Upgradeproperties>();
        ent.Drops = new List<Link_Resourceset>();
        ent.Costs = new List<Link_Resourceset>();
        ent.Sellable = false;
        ent.Controllable = false;
        ent.Destroyonparent = false;
        ent.Tags = new List<string> { "Mob", "Mobfighter", "Yosselefriend" };
        List<string> stats = new List<string>(YosseleData.FriendStats);
        foreach (Statproperties.Defaultstatgroup g in te.Defaultstat.Stats)
            if (g.Name == "Distancewait") stats.Add("Distancewait=" + g.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
        ent.Defaultstat = Stats(te.Defaultstat, YosseleData.Friend + "_Statproperties", stats.ToArray());
        Register("Mob", ent.Defaultstat);
        Spriteobject own = te.Defaultvisual != null ? YosseleAssets.Friend(te.Defaultvisual.Default.Spriteobject) : null;
        FriendOwnLook = own != null;
        Entityobject look = own == null && snoble != null && snoble.Entity != null && snoble.Entity.Defaultvisual != null ? snoble.Entity : te;
        FriendFrom = (look == te ? YosseleData.FriendTemplate : YosseleData.FriendLook) + (own != null ? " with " + YosseleData.FriendSheet : "")
            + (being.Block != null ? " (block " + being.Block.name + ")" : "");
        // Its own sheet plays on the miner's animator (Idle, Move, Attack); the Snoble's sheet has only Idle, with its own
        // simple animator and no attack animation.
        ent.Visualscripts = look == te ? ModKitUtil.Strip(te.Visualscripts) : ModKitUtil.Strip(look.Visualscripts);
        Visualproperties vp = UnityEngine.Object.Instantiate(look.Defaultvisual);
        YosseleUtil.Retag(vp, YosseleData.Friend + "_Visualproperties");
        Visualproperties.Entry e = look.Defaultvisual.Default;
        e.Rootvisualproperty = null;
        if (own != null)
            e.Spriteobject = own;
        e.Displayname = YosseleData.FriendDisplay;
        vp.Default = e;
        vp.Skinoverride = new List<Visualproperties.Param>();
        vp.Skinoverrides = new Dictionary<string, Visualproperties.Entry>();
        ent.Defaultvisual = vp;
        Register("Mob", vp);
        being.Entity = ent;
        Beings[YosseleData.Friend] = being;
        Register("Mob", being);
        Register("Mob", ent);
    }

    // ---------------------------------------------------------------- Directions (docs/scope/11)

    // One Direction per rank (written for kit 1.6, which had no ranks; kit 1.7's Rank is not used yet): the mode lists them by their "Direction_" names and grants the pick;
    // the vessel reads the highest rank held (YosseleServer.CompostRank, DirectionRank). Each carries its numbers as params.
    static void BuildDirections()
    {
        Upgradeproperties template = null;
        foreach (Upgradeproperties u in Upgrades.Values) { template = u; break; }
        if (template == null)
        {
            YosseleLog.Warn("no upgrade to make the Directions from");
            return;
        }
        foreach (YosseleData.Compost c in YosseleData.Compostbins)
        {
            Upgradeproperties d = ModKitDirections.Make(template, "Yossele", c.Name, c.Display, c.Desc);
            d.Defaultparams["Compost_Kills"] = c.Kills;
            d.Defaultparams["Compost_Radius"] = c.Radius;
            Compostbins.Add(RegisterDirection(d));
        }
        // The Tree's two clash: Clash 1 lets the mode offer a player only one of them.
        foreach (YosseleData.Shift s in YosseleData.Seasons)
        {
            Upgradeproperties d = ModKitDirections.Make(template, "Yossele", s.Name, s.Display, s.Desc);
            d.Defaultparams["Clash"] = 1f;
            d.Defaultparams["Season_Share"] = YosseleData.SaplingShare;
            Treeshifts.Add(RegisterDirection(d));
        }
        foreach (YosseleData.Shift s in YosseleData.Mushboomkits)
        {
            Upgradeproperties d = ModKitDirections.Make(template, "Yossele", s.Name, s.Display, s.Desc);
            d.Defaultparams["Clash"] = 1f;
            d.Defaultparams["Mushboom_Every"] = YosseleData.MushboomEvery;
            d.Defaultparams["Mushboom_Per"] = YosseleData.MushboomPerStack;
            Treeshifts.Add(RegisterDirection(d));
        }
    }

    static Upgradeproperties RegisterDirection(Upgradeproperties d)
    {
        foreach (KeyValuePair<string, float> kv in d.Defaultparams)
        {
            Upgradeproperties.Param p = new Upgradeproperties.Param();
            p.Name = kv.Key;
            p.Value = kv.Value;
            d.Defaultparam.Add(p);
        }
        Register("Upgrade", d);
        return d;
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
        Upgradeproperties t = YosseleUtil.Clone(template, YosseleData.HeroTooltip);
        t.Notes = "";
        t.Display_Name = YosseleData.TooltipName;
        t.Display_Desc = YosseleData.TooltipDesc;
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
        HeroBeing = YosseleUtil.Clone(tb, YosseleData.Hero);
        Entityobject ent = YosseleUtil.Clone(te, YosseleData.HeroEntity);
        ent.Name = YosseleData.Hero;
        ent.Display_Name = YosseleData.Displayname;
        ent.Display_Desc = YosseleData.HeroDesc;
        // A ranged vessel is built like Sno Sno: Mob_General, Mob_Movement, the stock Mob_Ranged in place of Smacky's
        // Mob_Combat (targets, mines ore, fires the Attack command's projectile and sends "Shoot", which
        // Entity_Vis_Mob_Attack turns into the Attack animation) and Hero_Buildsell. The hero brain names the laser.
        ent.Aiscripts = ModKitUtil.Strip(te.Aiscripts);
        int melee = ent.Aiscripts.FindIndex(s => s.name == "Entity_Ai_Mob_Combat");
        if (melee >= 0)
            ent.Aiscripts.RemoveAt(melee);
        if (!ent.Aiscripts.Exists(s => s.name == "Entity_Ai_Mob_Ranged"))
            ent.Aiscripts.Insert(melee >= 0 ? melee : Mathf.Min(2, ent.Aiscripts.Count), ModKitUtil.Script("Entity_Ai_Mob_Ranged"));
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
        YosseleUtil.Retag(vp, YosseleData.Hero + "_Visualproperties");
        Visualproperties.Entry e = te.Defaultvisual.Default;
        e.Rootvisualproperty = null;
        e.Spriteobject = YosseleAssets.Hero(templateSprite, footstep);
        e.Displaycolor = Accent;
        vp.Default = e;
        vp.Skinoverride = new List<Visualproperties.Param>();
        vp.Skinoverrides = new Dictionary<string, Visualproperties.Entry>();
        ent.Defaultvisual = vp;
        Register("Hero", vp);

        ent.Defaultstat = Stats(te.Defaultstat, YosseleData.Hero + "_Statproperties", YosseleData.HeroStats);
        Register("Hero", ent.Defaultstat);

        HeroBeing.Entity = ent;
        Register("Hero", HeroBeing);
        Register("Hero", ent);
    }

    static void BuildEntry(Heroentryobject template)
    {
        Entry = UnityEngine.Object.Instantiate(template);
        YosseleUtil.Retag(Entry, YosseleData.Hero);
        Bl_Address_Group loads = UnityEngine.Object.Instantiate(template.Heroloads);
        YosseleUtil.Retag(loads, YosseleData.Hero);
        Entry.Heroloads = loads;
        Entry.Towerdisplay = new List<string>(YosseleData.Towers);
        Entry.Displayname = YosseleData.Displayname;
        Entry.Displaysub = YosseleData.Displaysub;
        Entry.Displayskill = YosseleData.Displayskill;
        Entry.Color = Accent;
        // The 12x12 sprite in player lists, the setup bar and leaderboards: the ready-up portrait in its brackets.
        Entry.Icon = ModKitAssets.Single("Readyup6yossele", 12) ?? template.Icon;
        Entry.Worldsplash = ModKitAssets.Single("Yosselesplash", 128) ?? template.Worldsplash;
        Entry.Endscreenwinicon = ModKitAssets.Single("Yosseleendstat1", 32) ?? template.Endscreenwinicon;
        Entry.Endscreenloseicon = ModKitAssets.Single("Yosseleendstat2", 32) ?? template.Endscreenloseicon;
        Entry.Worldsprite = ModKitAssets.Worldsprite(template.Worldsprite);
        Texture fall = ModKitAssets.Sheet("YosseleFall-Sheet");
        Texture land = ModKitAssets.Sheet("YosseleIntroR-Sheet");
        Texture idle = ModKitAssets.Sheet("YosseleIdlejustidle-Sheet");
        if (fall != null) Entry.Loadingsprite = fall;
        if (land != null) Entry.Landingsprite = land;
        if (idle != null) Entry.Idlesprite = idle;
        Dictionary<string, UnityEngine.Object> heroes = ModKitUtil.Folder("Hero");
        heroes[YosseleData.Hero] = Entry;
    }
}
