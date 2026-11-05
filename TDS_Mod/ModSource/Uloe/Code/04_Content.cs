// Builds Uloe's game data the way Blockage's own data is laid out (hero entry -> being -> entity -> visuals, upgrades,
// projectiles, commands), cloning the shipped vessels' objects for everything the engine expects and swapping in Uloe's art,
// names and rules. Every player runs this identically, so online clients resolve the same names. Templates: Smacky (vessel,
// Archer Tower for the Glass, Mining Hut for the Bank and the Flood Light, Wall for the Mirror, the miner for the Photon), Sno
// Sno's snowball (the spark), Buster's Totem beam (the Glass's beam).
public static class UloeContent
{
    public static Heroentryobject Entry;
    public static Beingobject HeroBeing;
    public static readonly Dictionary<string, Beingobject> Beings = new Dictionary<string, Beingobject>();
    public static readonly Dictionary<string, Upgradeproperties> Upgrades = new Dictionary<string, Upgradeproperties>();
    public static readonly Dictionary<string, UloeData.Upg> UpgradeDefs = new Dictionary<string, UloeData.Upg>();
    public static readonly List<Upgradeproperties> Directions = new List<Upgradeproperties>();
    public static Commandobject Plug, Aim, Surge, Colour;
    public static int VfxRegistered;
    public static bool IconsReady, SparkReady, BeamReady;
    public static string SparkFrom = "", BeamFrom = "";
    public static readonly Color Accent = new Color(0.945f, 0.937f, 0.447f, 1f);   // the glints, F1EF72

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

        UloeAssets.ApplyTowerMaterial(TemplateTowerMaterial());
        RegisterVfx();
        BuildSpark();
        BuildBeam();
        BuildCommands();
        foreach (UloeData.Build b in UloeData.Builds)
            BuildTower(b);
        BuildPhoton();
        BuildHero(ModKitUtil.Load<Beingobject>("Hero", folder));
        BuildEntry(template);
        BuildDirections();
        try { UloeJournal.Build(template); }
        catch (Exception e) { UloeLog.Warn("journal: " + e); }
        Bl_Address.Loadedfolders[UloeData.Hero] = group;
    }

    public static string Summary()
    {
        return Beings.Count + " beings, " + Upgrades.Count + " upgrades, " + Directions.Count + " Directions, " + VfxRegistered + " effects, icons " + (IconsReady ? "ok" : "stock")
            + ", spark from " + SparkFrom + ", beam from " + BeamFrom + ", towers fitted " + ModKitTowers.Fitted;
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
        obj.Loadsource = UloeData.Hero;
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

    static Visualproperties.Entry ModelEntry(Visualproperties.Entry from, string model, string display, string extra)
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
        if (!string.IsNullOrEmpty(extra))
        {
            GameObject x = ModKitAssets.Model(extra);
            if (x != null) e.Additionalprefabs.Add(x);
        }
        return e;
    }

    // A building's look: its model, plus one skin per Light band (Lit1..3) that adds the ring and rays round its foot, on the
    // base model and every upgrade's. The skin is synced state, so every player sees it (docs/STYLE.md).
    static Visualproperties TowerVisual(Visualproperties template, string name, string model, string display)
    {
        Visualproperties vp = UnityEngine.Object.Instantiate(template);
        UloeUtil.Retag(vp, name);
        vp.Default = ModelEntry(template.Default, model, display, null);
        vp.Skinoverride = new List<Visualproperties.Param>();
        vp.Skinoverrides = new Dictionary<string, Visualproperties.Entry>();
        for (int band = 1; band <= UloeData.LookBands.Length; band++)
            ModKitUtil.AddSkin(vp, "Lit" + band, ModelEntry(template.Default, model, display, "Uloe_Lit" + band));
        return vp;
    }

    // "Health=25+25" -> value 25, +25 per level. Builds a stat block holding exactly these stats, each styled like the
    // template's stat of the same name, else its first stat (shown only to its own team, like every shipped tower).
    static Statproperties Stats(Statproperties template, string name, string[] spec)
    {
        Statproperties stats = UnityEngine.Object.Instantiate(template);
        UloeUtil.Retag(stats, name);
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

    // The upgrade tree, curated like the shipped ones: explicit next options per upgrade, costs from UloeData.
    static List<Upgradeproperties> Tree(UloeData.Build b, Entityobject te)
    {
        Dictionary<int, Upgradeproperties> templateTiers = new Dictionary<int, Upgradeproperties>();
        ModKitUtil.CollectUpgrades(te.Upgrades, templateTiers);
        if (templateTiers.Count == 0)
            ModKitUtil.CollectUpgrades(ModKitUtil.Load<Beingobject>("Mob", "Tower_Smackyhut").Entity.Upgrades, templateTiers);
        Dictionary<string, Upgradeproperties> byKey = new Dictionary<string, Upgradeproperties>();
        List<Upgradeproperties> roots = new List<Upgradeproperties>();
        foreach (UloeData.Upg u in b.Upgrades)
        {
            Upgradeproperties tu;
            if (!templateTiers.TryGetValue(u.Tier, out tu))
                foreach (Upgradeproperties any in templateTiers.Values) { tu = any; break; }
            if (tu == null)
                continue;
            Upgradeproperties up = UloeUtil.Clone(tu, u.Id);
            up.Notes = "";
            up.Display_Name = u.Display;
            up.Display_Desc = u.Desc;
            up.Priority = u.Tier;
            up.Aiscripts = new List<UnityEngine.Object>();
            up.Costs = ModKitUtil.Costs(u.Costs);
            up.Ent_Visual = TowerVisual(te.Defaultvisual, u.Id.Replace("_Upgradeobject", "_Visualproperties"), ModKitAssets.Quiet(u.Model) != null ? u.Model : b.Id, u.Display);
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
        foreach (UloeData.Upg u in b.Upgrades)
        {
            Upgradeproperties up, next;
            if (!byKey.TryGetValue(u.Key + u.Tier, out up))
                continue;
            foreach (string n in u.Next)
                if (byKey.TryGetValue(n.Trim(), out next))
                    up.Upgrades.Add(next);
                else
                    UloeLog.Warn(u.Id + ": unknown next upgrade " + n);
        }
        return roots;
    }

    // ---------------------------------------------------------------- icons

    // Helper_Uitools copies the Iconobjects into a private table the first time any icon is asked for, and never again
    // (Argo 1.0 found this). So Uloe adds its icons at boot, before any asset loading, and only checks them later.
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
            AddIcon(types, "Uloe_Plug", UloeAssets.PlugIcon, "Plug In", like, ModKitAssets.CommandIcon);
            AddIcon(types, "Uloe_Aim", UloeAssets.AimIcon, "Aim", like, ModKitAssets.CommandIcon);
            AddIcon(types, "Uloe_Surge", UloeAssets.SurgeIcon, "Surge", like, ModKitAssets.CommandIcon);
            AddIcon(types, "Uloe_Colour", UloeAssets.ColourIcon, "Colour", like, ModKitAssets.CommandIcon);
            AddIcon(stats, UloeData.LightStat, UloeAssets.LightIcon, UloeData.LightStat, like);
            IconsReady = true;
        }
        catch (Exception e)
        {
            UloeLog.Warn("icons: " + e.Message);
        }
    }

    // A few seconds after boot: if the icon table was frozen before Uloe's icons went in, use stock icons so the buttons are
    // never blank.
    public static void CheckIcons()
    {
        bool ok = false;
        try { ok = Helper_Uitools.Geticon("Uloe_Plug") != null; } catch (Exception) { }
        IconsReady = ok;
        if (ok)
        {
            UloeLog.Info("icons: Uloe's own");
            return;
        }
        if (Plug != null) Plug.Icon = Icon("", "Movelinear", "Move");
        if (Aim != null) Aim.Icon = Icon("", "Move");
        if (Surge != null) Surge.Icon = Icon("", "Convert", "Move");
        if (Colour != null) Colour.Icon = Icon("", "Convert", "Move");
        UloeLog.Warn("icon table was built before Uloe's icons were added; Uloe's buttons use stock icons");
    }

    static void AddIcon(Iconobject into, string name, string[] rows, string display, Iconobject.Icon like, int box = 0)
    {
        if (into == null || into.Icons == null || into.Icons.ContainsKey(name))
            return;
        Iconobject.Icon icon = new Iconobject.Icon();
        icon.Name = name;
        icon.Image = UloeAssets.Pixel(rows, "Icon_" + name, box);
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

    // ---------------------------------------------------------------- the spark and the beam

    // Uloe's shot: Sno Sno's homing snowball (the kit's shot for any ranged hero) with a yellow bolt for its look and the Archer
    // arrow's end look and sound. The Archer's arrow is the template without Sno Sno's group.
    static void BuildSpark()
    {
        Projectileobject template = ModKitUtil.Load<Projectileobject>("Projectile", UloeData.SparkTemplate);
        SparkFrom = UloeData.SparkTemplate;
        if (template == null)
        {
            template = ModKitUtil.Load<Projectileobject>("Projectile", UloeData.SparkFallback);
            SparkFrom = UloeData.SparkFallback;
        }
        if (template == null)
        {
            SparkFrom = "none";
            UloeLog.Warn("no projectile template (" + UloeData.SparkTemplate + ", " + UloeData.SparkFallback + "); Uloe shoots the Archer's arrow");
            return;
        }
        Projectileobject spark = UloeUtil.Clone(template, UloeData.Spark);
        GameObject model = ModKitAssets.Model("Uloe_Spark");
        if (model != null)
            spark.Vis = model;
        spark.Sfxstart = "";
        Projectileobject arrow = ModKitUtil.Load<Projectileobject>("Projectile", UloeData.SparkFallback);
        if (arrow != null && template != arrow)
        {
            spark.Vfxend = arrow.Vfxend;
            spark.Sfxend = arrow.Sfxend;
        }
        Register("Projectile", spark);
        SparkReady = true;
    }

    // The Glass's beam: Buster's Totem beam (Projectile_Ai_Beam) with no sound of its own and a small glow at the far end; the
    // line is drawn by every client (UloeClient.BeamRun). Without Buster's group the Glass hits directly.
    static void BuildBeam()
    {
        Projectileobject template = ModKitUtil.Load<Projectileobject>("Projectile", UloeData.BeamTemplate);
        if (template == null)
        {
            BeamFrom = "none, direct hits";
            UloeLog.Warn("no beam template " + UloeData.BeamTemplate + "; the Magnifying Glass hits directly");
            return;
        }
        BeamFrom = UloeData.BeamTemplate;
        Projectileobject beam = UloeUtil.Clone(template, UloeData.Beam);
        GameObject model = ModKitAssets.Model("Uloe_Beamtip");
        if (model != null)
            beam.Vis = model;
        beam.Sfxstart = "";
        beam.Sfxend = "";
        Register("Projectile", beam);
        BeamReady = true;
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

    static Commandobject Command(Commandobject template, string id, string name, string desc, string icon, Entity_Command_Base funct, bool ability)
    {
        Commandobject c = UloeUtil.Clone(template, id);
        c.Style = Commandobject.Styles.Button;
        c.Display_Name = name;
        c.Display_Desc = desc;
        c.Color = Accent;
        // Keybinds must be the game's own input actions: Game_Selection.Addcommand looks each one up in Bl_Inputs.Actions and
        // throws on a miss (pitfall #113).
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
            UloeLog.Warn("no command template; Uloe's buttons are disabled");
            return;
        }
        // PLUG IN: a vessel ability on Buster's Dash keys; pressing it picks a node or a Power Bank, like Sell picks a tower.
        Plug = Command(template, UloeData.PlugCommand, UloeData.PlugName, UloeData.PlugDesc, "Uloe_Plug", new UloePlugSelect(), true);
        // AIM: a building's button, the same kind of picker.
        Aim = Command(template, UloeData.AimCommand, UloeData.AimName, UloeData.AimDesc, "Uloe_Aim", new UloeAimSelect(), false);
        // SURGE (Supernova) and COLOUR (Colour Filter): one-click buttons like the Mining Hut's Convert; the building keeps
        // their state in the command's Goalstring for every player.
        Surge = Command(template, UloeData.SurgeCommand, UloeData.SurgeName, UloeData.SurgeDesc, "Uloe_Surge", new UloeSurgeButton(), false);
        Colour = Command(template, UloeData.ColourCommand, UloeData.ColourName, UloeData.ColourDesc, "Uloe_Colour", new UloeColourButton(), false);
        // A Long Throw Flood Light keeps AIM beside COLOUR: one key each (only the game's input actions, pitfall #113).
        Colour.Keybind = new List<string> { "4" };
    }

    // ---------------------------------------------------------------- buildables

    static void BuildTower(UloeData.Build b)
    {
        Beingobject tb = ModKitUtil.Load<Beingobject>("Mob", b.Template);
        if (tb == null || tb.Entity == null)
        {
            UloeLog.Error("tower template " + b.Template + " not found; " + b.Id + " skipped");
            return;
        }
        Entityobject te = tb.Entity;
        Beingobject being = UloeUtil.Clone(tb, b.Id);
        Entityobject ent = UloeUtil.Clone(te, b.Id + "_Entity");
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
        ent.Defaultvisual = TowerVisual(te.Defaultvisual, b.Id + "_Visualproperties", b.Id, b.Display);
        Register("Mob", ent.Defaultvisual);
        // Kit 1.2: the range sphere the game draws (and the stock AIs measure from) sits on this tower, at the height its
        // brain measures from (its Entityposition); the Glass's lens is its muzzle.
        bool glass = b.Id == UloeData.Glass;
        ModKitTowers.Fit(ent, b.Id, ModKitAssets.Model(b.Id), glass ? UloeData.BeamEye : 0f, glass ? UloeData.BeamEye : 0.05f, Array.Exists(b.Stats, s => s.StartsWith("Range=")));
        ent.Upgrades = Tree(b, te);
        being.Entity = ent;
        Beings[b.Id] = being;
        Register("Mob", being);
        Register("Mob", ent);
    }

    // ---------------------------------------------------------------- the Photon

    static readonly Color Tint = new Color(1f, 0.98f, 0.66f, 0f);

    static void BuildPhoton()
    {
        Beingobject tb = ModKitUtil.Load<Beingobject>("Mob", "Mob_Smackyminer");
        if (tb == null || tb.Entity == null)
        {
            UloeLog.Error("Mob_Smackyminer not found; Uloe's Photons are disabled");
            return;
        }
        Entityobject te = tb.Entity;
        string id = UloeData.Photon;
        Beingobject being = UloeUtil.Clone(tb, id);
        Entityobject ent = UloeUtil.Clone(te, id + "_Entity");
        ent.Name = id;
        ent.Display_Name = UloeData.PhotonDisplay;
        ent.Display_Desc = UloeData.PhotonDesc;
        // The game's generic melee fighter AI; the brain only walks it.
        ent.Aiscripts = Scripts("Entity_Ai_Mob_General", "Entity_Ai_Mob_Movement", "Entity_Ai_Mob_Combat");
        ent.Visualscripts = Scripts("Entity_Vis_Animator_Mob", "Entity_Vis_Mob_Attack");
        ent.Upgrades = new List<Upgradeproperties>();
        ent.Upgradesinit = new List<Upgradeproperties>();
        ent.Drops = new List<Link_Resourceset>();
        ent.Costs = new List<Link_Resourceset>();
        List<string> all = new List<string>(UloeData.PhotonStats);
        foreach (Statproperties.Defaultstatgroup g in te.Defaultstat.Stats)
            if (g.Name == "Distancewait") all.Add("Distancewait=" + g.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
        ent.Defaultstat = Stats(te.Defaultstat, id + "_Statproperties", all.ToArray());
        Register("Mob", ent.Defaultstat);
        ent.Sellable = false;
        ent.Sellhealthfactor = false;
        ent.Controllable = false;
        ent.Destroyonparent = true;
        ent.Tags = new List<string> { "Mob", "Mobfighter", "Uloephoton" };
        Visualproperties vp = UnityEngine.Object.Instantiate(te.Defaultvisual);
        UloeUtil.Retag(vp, id + "_Visualproperties");
        Visualproperties.Entry e = te.Defaultvisual.Default;
        e.Rootvisualproperty = null;
        e.Spriteobject = ModKitAssets.Unit(UloeData.PhotonSheet, UloeData.PhotonCell, id + "_Sprite", te.Defaultvisual.Default.Spriteobject);
        e.Displayname = UloeData.PhotonDisplay;
        e.Displaycolor = Tint;
        GameObject poof = ModKitAssets.Vfx("World_Uloe_Motes");
        if (poof != null) e.Vfxend = poof;
        vp.Default = e;
        vp.Skinoverride = new List<Visualproperties.Param>();
        vp.Skinoverrides = new Dictionary<string, Visualproperties.Entry>();
        ent.Defaultvisual = vp;
        Register("Mob", vp);
        being.Entity = ent;
        Beings[id] = being;
        Register("Mob", being);
        Register("Mob", ent);
    }

    // ---------------------------------------------------------------- Blind and Illuminate

    // Any shipped upgrade to clone, for the ones that aren't a tier of a tree.
    static Upgradeproperties AnyUpgrade()
    {
        foreach (Bl_Address.Loadlist l in Bl_Address.Groups["Upgrade"])
            foreach (UnityEngine.Object o in l.Items.Values)
                if (o is Upgradeproperties && !Upgrades.ContainsKey(o.name))
                    return (Upgradeproperties)o;
        return null;
    }

    // ---------------------------------------------------------------- Directions (kit 1.7, docs/scope/11)

    // One upgrade per rank, Direction_Uloe_<Name><rank>_Upgradeobject: the mode lists them by their "Direction_" names and
    // grants the pick on the vessel; the brain reads the highest rank held (UloeHeroBrain.CheckDirections), and the numbers live
    // in UloeData, indexed by rank.
    static void BuildDirections()
    {
        Upgradeproperties template = AnyUpgrade();
        if (template == null)
        {
            UloeLog.Warn("no upgrade to make the Directions from");
            return;
        }
        foreach (UloeData.Dir dir in UloeData.Directions)
            for (int r = 1; r <= 3; r++)
            {
                string display = dir.Display + (r == 2 ? " II" : r == 3 ? " III" : "");
                Upgradeproperties d = ModKitDirections.Make(template, UloeData.Part, dir.Name + r, display, dir.Desc[r - 1]);
                Register("Upgrade", d);
                Directions.Add(d);
            }
    }

    // ---------------------------------------------------------------- the vessel

    // The special rule, shown the way Sno Sno, Boutman and Buster show theirs: a tier -3 upgrade in Upgradesinit.
    static Upgradeproperties Tooltip()
    {
        Upgradeproperties template = AnyUpgrade();
        if (template == null)
            return null;
        Upgradeproperties t = UloeUtil.Clone(template, UloeData.HeroTooltip);
        t.Notes = "";
        t.Display_Name = UloeData.TooltipName;
        t.Display_Desc = UloeData.TooltipDesc;
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
        HeroBeing = UloeUtil.Clone(tb, UloeData.Hero);
        Entityobject ent = UloeUtil.Clone(te, UloeData.HeroEntity);
        ent.Name = UloeData.Hero;
        ent.Display_Name = UloeData.Displayname;
        ent.Display_Desc = UloeData.HeroDesc;
        // A ranged vessel is built like Sno Sno: Mob_General, Mob_Movement, the stock Mob_Ranged in place of Smacky's
        // Mob_Combat (targets, mines ore, fires the Attack command's projectile and sends "Shoot", which
        // Entity_Vis_Mob_Attack turns into the Attack animation) and Hero_Buildsell. The hero brain names the spark.
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
        UloeUtil.Retag(vp, UloeData.Hero + "_Visualproperties");
        Visualproperties.Entry e = te.Defaultvisual.Default;
        e.Rootvisualproperty = null;
        e.Spriteobject = UloeAssets.Hero(templateSprite, footstep, false);
        e.Displaycolor = Accent;
        vp.Default = e;
        vp.Skinoverride = new List<Visualproperties.Param>();
        vp.Skinoverrides = new Dictionary<string, Visualproperties.Entry>();
        // The bulb burning while plugged in: a skin, so every player sees it.
        Visualproperties.Entry lit = e;
        lit.Spriteobject = UloeAssets.Hero(templateSprite, footstep, true);
        ModKitUtil.AddSkin(vp, UloeData.Plugged, lit);
        ent.Defaultvisual = vp;
        Register("Hero", vp);

        ent.Defaultstat = Stats(te.Defaultstat, UloeData.Hero + "_Statproperties", UloeData.HeroStats);
        Register("Hero", ent.Defaultstat);

        HeroBeing.Entity = ent;
        Register("Hero", HeroBeing);
        Register("Hero", ent);
    }

    static void BuildEntry(Heroentryobject template)
    {
        Entry = UnityEngine.Object.Instantiate(template);
        UloeUtil.Retag(Entry, UloeData.Hero);
        Bl_Address_Group loads = UnityEngine.Object.Instantiate(template.Heroloads);
        UloeUtil.Retag(loads, UloeData.Hero);
        Entry.Heroloads = loads;
        Entry.Towerdisplay = new List<string>(UloeData.Towers);
        Entry.Displayname = UloeData.Displayname;
        Entry.Displaysub = UloeData.Displaysub;
        Entry.Displayskill = UloeData.Displayskill;
        Entry.Color = Accent;
        // The 12x12 sprite in player lists, the setup bar and leaderboards: the ready-up portrait in its brackets.
        Entry.Icon = ModKitAssets.Single("Readyup6uloe", 12) ?? template.Icon;
        Entry.Worldsplash = ModKitAssets.Single("Uloesplash", 128) ?? template.Worldsplash;
        Entry.Endscreenwinicon = ModKitAssets.Single("Uloeendstat1", 32) ?? template.Endscreenwinicon;
        Entry.Endscreenloseicon = ModKitAssets.Single("Uloeendstat2", 32) ?? template.Endscreenloseicon;
        Entry.Worldsprite = ModKitAssets.Worldsprite(template.Worldsprite);
        Texture fall = ModKitAssets.Sheet("UloeFall-Sheet");
        Texture land = ModKitAssets.Sheet("UloeIntroR-Sheet");
        Texture idle = ModKitAssets.Sheet("UloeIdlejustidle-Sheet");
        if (fall != null) Entry.Loadingsprite = fall;
        if (land != null) Entry.Landingsprite = land;
        if (idle != null) Entry.Idlesprite = idle;
        Dictionary<string, UnityEngine.Object> heroes = ModKitUtil.Folder("Hero");
        heroes[UloeData.Hero] = Entry;
    }
}
