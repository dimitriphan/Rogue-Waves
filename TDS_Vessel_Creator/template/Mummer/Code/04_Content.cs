// Builds Mummer's game data the way Blockage's own data is laid out (hero entry -> being -> entity -> visuals,
// upgrades, projectiles, commands), cloning the shipped vessels' objects for everything the engine expects and
// swapping in Mummer's art, names and rules. Every player runs this identically, so online clients resolve the same
// names. Templates: Smacky (vessel, Loom, Stage, Cross, Understudies), Buster's Speed Pad (the walk-through Curtain).
public static class MummerContent
{
    public static Heroentryobject Entry;
    public static Beingobject HeroBeing;
    public static readonly Dictionary<string, Beingobject> Beings = new Dictionary<string, Beingobject>();
    public static readonly Dictionary<string, Upgradeproperties> Upgrades = new Dictionary<string, Upgradeproperties>();
    public static readonly Dictionary<string, MummerData.Upg> UpgradeDefs = new Dictionary<string, MummerData.Upg>();
    public static Commandobject Hook, Costume;
    public static int VfxRegistered;
    public static bool IconsReady;
    public static string AegisBlock = "";

    static Bl_Address.Loadgroup group;
    static readonly Dictionary<string, Bl_Address.Loadlist> lists = new Dictionary<string, Bl_Address.Loadlist>();

    // Script names written for one shipped vessel (they hard-code its objects); everything else is generic.
    static readonly string[] VesselNames = { "Smacky", "Snosno", "Boutman", "Fie", "Buster" };

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

        MummerAssets.ApplyTowerMaterial(TemplateTowerMaterial());
        RegisterVfx();
        BuildProjectile();
        BuildCommands();
        foreach (MummerData.Build b in MummerData.Builds)
            BuildTower(b);
        foreach (MummerData.Puppet p in MummerData.Puppets)
            BuildPuppet(p);
        BuildHero(MummerUtil.Load<Beingobject>("Hero", folder));
        BuildEntry(template);
        try { MummerJournal.Build(template); }
        catch (Exception e) { MummerLog.Warn("journal: " + e); }
        Bl_Address.Loadedfolders[MummerData.Hero] = group;
    }

    public static string Summary()
    {
        return Beings.Count + " beings, " + Upgrades.Count + " upgrades, " + VfxRegistered + " effects, icons " + (IconsReady ? "ok" : "stock") + ", Aegis block '" + AegisBlock + "'";
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
        obj.Loadsource = MummerData.Hero;
        Bl_Address.Groupsname[new FixedString64Bytes(obj.Objectname)] = obj;
        Bl_Address.Groupsids[id] = obj;
    }

    // Effects the game plays by name (Vfxdebuffcheck / Vfxoneshotcheck look them up with Getload("Vfx", name)), so the
    // Marked, Held and Tangled looks are attached and timed by the game on every player's machine.
    static void RegisterVfx()
    {
        Bl_Address.Loadlist list = lists["Vfx"];
        foreach (GameObject go in MummerAssets.AllVfx())
        {
            list.Items[go.name] = go;
            list.Index.Add(go);
            VfxRegistered++;
        }
    }

    // ---------------------------------------------------------------- helpers

    static Material TemplateTowerMaterial()
    {
        Beingobject archer = MummerUtil.Load<Beingobject>("Mob", "Tower_Smackyarcher");
        if (archer == null || archer.Entity == null || archer.Entity.Defaultvisual == null)
            return null;
        GameObject prefab = archer.Entity.Defaultvisual.Default.Visprefab;
        if (prefab == null)
            return null;
        MeshRenderer r = prefab.GetComponentInChildren<MeshRenderer>(true);
        return r != null ? r.sharedMaterial : null;
    }

    static bool VesselSpecific(string name)
    {
        if (name == "Entity_Ai_General_Upgrade")
            return true;
        foreach (string v in VesselNames)
            if (name.Contains(v))
                return true;
        return false;
    }

    static List<UnityEngine.Object> Strip(List<UnityEngine.Object> scripts)
    {
        List<UnityEngine.Object> kept = new List<UnityEngine.Object>();
        if (scripts == null)
            return kept;
        foreach (UnityEngine.Object s in scripts)
            if (s != null && !VesselSpecific(s.name))
                kept.Add(s);
        return kept;
    }

    static List<UnityEngine.Object> Scripts(params string[] names)
    {
        List<UnityEngine.Object> l = new List<UnityEngine.Object>();
        foreach (string n in names) l.Add(MummerUtil.Script(n));
        return l;
    }

    static Visualproperties.Entry ModelEntry(Visualproperties.Entry from, string model, string display, string extra)
    {
        Visualproperties.Entry e = from;
        e.Rootvisualproperty = null;
        e.Style = Visualproperties.Styles.Model;
        e.Visprefab = MummerAssets.Model(model);
        e.Spriteobject = null;
        e.Displayname = display;
        e.Additionalmaterials = new List<Material>();
        e.Additionalvisualscripts = Strip(from.Additionalvisualscripts);
        e.Additionalprefabs = new List<GameObject>();
        if (!string.IsNullOrEmpty(extra))
        {
            GameObject x = MummerAssets.Model(extra);
            if (x != null) e.Additionalprefabs.Add(x);
        }
        return e;
    }

    // The Stage shows the costume it stitches on a little hanger (skins "Ballista" and "Aegis"), on every upgrade model.
    static Visualproperties TowerVisual(Visualproperties template, string name, string model, string display, bool costumes)
    {
        Visualproperties vp = UnityEngine.Object.Instantiate(template);
        MummerUtil.Retag(vp, name);
        vp.Default = ModelEntry(template.Default, model, display, null);
        vp.Skinoverride = new List<Visualproperties.Param>();
        vp.Skinoverrides = new Dictionary<string, Visualproperties.Entry>();
        if (costumes)
        {
            AddSkin(vp, "Ballista", ModelEntry(template.Default, model, display, "Mummer_Hanger_Ballista"));
            AddSkin(vp, "Aegis", ModelEntry(template.Default, model, display, "Mummer_Hanger_Aegis"));
        }
        return vp;
    }

    static void AddSkin(Visualproperties vp, string skin, Visualproperties.Entry entry)
    {
        Visualproperties.Param p = new Visualproperties.Param();
        p.Name = skin;
        p.Value = entry;
        vp.Skinoverride.Add(p);
        vp.Skinoverrides[skin] = entry;
    }

    static void CollectUpgrades(List<Upgradeproperties> from, Dictionary<int, Upgradeproperties> byTier)
    {
        if (from == null)
            return;
        foreach (Upgradeproperties u in from)
        {
            if (u == null)
                continue;
            if (!byTier.ContainsKey(u.Priority))
                byTier[u.Priority] = u;
            CollectUpgrades(u.Upgrades, byTier);
        }
    }

    // "Health=25+25" -> value 25, +25 per level. Builds a stat block holding exactly these stats, each styled like the
    // template's stat of the same name, else its first stat (shown only to its own team, like every shipped tower).
    static Statproperties Stats(Statproperties template, string name, string[] spec)
    {
        Statproperties stats = UnityEngine.Object.Instantiate(template);
        MummerUtil.Retag(stats, name);
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
            n.Value = MummerUtil.F(vl[0]);
            n.Leveladd = vl.Length > 1 ? MummerUtil.F(vl[1]) : 0f;
            list.Add(n);
        }
        stats.Stats = list;
        return stats;
    }

    // ---------------------------------------------------------------- icons

    // Helper_Uitools copies the Iconobjects into a private table the first time any icon is asked for, and never again
    // (Argo 1.0 found this). So Mummer adds its icons at boot, before any asset loading, and only checks them later.
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
            AddIcon(types, "Mummer_Hook", MummerAssets.HookIcon, "Hook", like);
            AddIcon(types, "Mummer_Costume", MummerAssets.CostumeIcon, "Costume", like);
            AddIcon(stats, "Thread", MummerAssets.ThreadIcon, "Thread", like);
            IconsReady = true;
        }
        catch (Exception e)
        {
            MummerLog.Warn("icons: " + e.Message);
        }
    }

    // A few seconds after boot: if the icon table was frozen before Mummer's icons went in, use stock icons so the
    // buttons are never blank.
    public static void CheckIcons()
    {
        bool ok = false;
        try { ok = Helper_Uitools.Geticon("Mummer_Hook") != null; } catch (Exception) { }
        IconsReady = ok;
        if (ok)
        {
            MummerLog.Info("icons: Mummer's own");
            return;
        }
        if (Hook != null) Hook.Icon = Icon("", "Movelinear", "Move");
        if (Costume != null) Costume.Icon = Icon("", "Units", "Unit", "Move");
        MummerLog.Warn("icon table was built before Mummer's icons were added; HOOK and COSTUME use stock icons");
    }
    static void AddIcon(Iconobject into, string name, string[] rows, string display, Iconobject.Icon like)
    {
        if (into == null || into.Icons == null || into.Icons.ContainsKey(name))
            return;
        Iconobject.Icon icon = new Iconobject.Icon();
        icon.Name = name;
        icon.Image = MummerAssets.Pixel(rows, "Icon_" + name);
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

    // ---------------------------------------------------------------- projectile (the Ballista Understudy's felt bolt)

    static void BuildProjectile()
    {
        Projectileobject template = MummerUtil.Load<Projectileobject>("Projectile", "Tower_Smackyarcher_Arrow");
        if (template == null)
        {
            MummerLog.Warn("projectile template Tower_Smackyarcher_Arrow not found; Ballista Understudies shoot arrows");
            return;
        }
        Projectileobject proj = MummerUtil.Clone(template, MummerData.PuppetBolt);
        GameObject model = MummerAssets.Model("Mummer_Bolt");
        if (model != null)
            proj.Vis = model;
        proj.Sfxstart = "";
        Register("Projectile", proj);
    }

    // ---------------------------------------------------------------- commands

    static Commandobject CommandTemplate()
    {
        Commandobject template = MummerUtil.Load<Commandobject>("Command", "Command_Tower_Smackyhut_Convert");
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
            MummerLog.Warn("no command template; HOOK and COSTUME disabled");
            return;
        }
        // HOOK: a vessel ability on Buster's Dash keys (Command_Movelinear); pressing it picks a foe like Sell picks a tower.
        Hook = MummerUtil.Clone(template, MummerData.HookCommand);
        Hook.Style = Commandobject.Styles.Button;
        Hook.Display_Name = MummerData.HookName;
        Hook.Display_Desc = MummerData.HookDesc;
        Hook.Color = new Color(0.70f, 0.57f, 0.84f, 1f);
        Hook.Keybind = new List<string> { "6", "7" };
        Hook.Keybindstate = "Onpressed";
        Hook.Priority = 6;
        Hook.Commandfuncts = new List<Entity_Command_Base> { new MummerHookSelect() };
        Hook.Icon = "Mummer_Hook";
        Register("Command", Hook);

        // COSTUME: a tower button like the Mining Hut's Convert; it shows the costume being stitched and cycles it.
        Costume = MummerUtil.Clone(template, MummerData.CostumeCommand);
        Costume.Style = Commandobject.Styles.Button;
        Costume.Display_Name = MummerData.CostumeName;
        Costume.Display_Desc = MummerData.CostumeDesc;
        Costume.Color = new Color(0.70f, 0.57f, 0.84f, 1f);
        Costume.Keybind = new List<string> { "Quickaction", "5" };
        Costume.Keybindstate = "Onrelease";
        Costume.Priority = 2;
        Costume.Commandfuncts = new List<Entity_Command_Base> { new MummerCostumeButton() };
        Costume.Icon = "Mummer_Costume";
        Register("Command", Costume);
    }

    // ---------------------------------------------------------------- buildables

    static void BuildTower(MummerData.Build b)
    {
        Beingobject tb = MummerUtil.Load<Beingobject>("Mob", b.Template);
        if ((tb == null || tb.Entity == null) && b.Id == MummerData.Curtain)
        {
            MummerLog.Warn(b.Template + " not loaded; the Curtain uses Perma-Wall's walk-through block");
            tb = MummerUtil.Load<Beingobject>("Mob", "Trap_Smackywall_Perma");
        }
        if (tb == null || tb.Entity == null)
        {
            MummerLog.Error("tower template " + b.Template + " not found; " + b.Id + " skipped");
            return;
        }
        Entityobject te = tb.Entity;
        Beingobject being = MummerUtil.Clone(tb, b.Id);
        Entityobject ent = MummerUtil.Clone(te, b.Id + "_Entity");
        ent.Name = b.Id;
        ent.Display_Name = b.Display;
        ent.Display_Desc = b.Desc;
        ent.Aiscripts = Strip(te.Aiscripts);
        ent.Visualscripts = Strip(te.Visualscripts);
        ent.Costs = MummerUtil.Costs(b.Costs);
        ent.Upgradesinit = new List<Upgradeproperties>();
        ent.Sellable = true;
        ent.Controllable = true;
        ent.Tags = new List<string>(b.Tags);
        ent.Tags.Add(b.Tag);
        ent.Defaultstat = Stats(te.Defaultstat, b.Id + "_Statproperties", b.Stats);
        Register("Mob", ent.Defaultstat);
        bool stage = b.Id == MummerData.Stage;
        ent.Defaultvisual = TowerVisual(te.Defaultvisual, b.Id + "_Visualproperties", b.Id, b.Display, stage);
        Register("Mob", ent.Defaultvisual);

        // Upgrade tree, curated like the shipped ones: explicit next options per upgrade, costs from MummerData.
        Dictionary<int, Upgradeproperties> templateTiers = new Dictionary<int, Upgradeproperties>();
        CollectUpgrades(te.Upgrades, templateTiers);
        if (templateTiers.Count == 0)
            CollectUpgrades(MummerUtil.Load<Beingobject>("Mob", "Tower_Smackyhut").Entity.Upgrades, templateTiers);
        Dictionary<string, Upgradeproperties> byKey = new Dictionary<string, Upgradeproperties>();
        List<Upgradeproperties> roots = new List<Upgradeproperties>();
        foreach (MummerData.Upg u in b.Upgrades)
        {
            Upgradeproperties tu;
            if (!templateTiers.TryGetValue(u.Tier, out tu))
                foreach (Upgradeproperties any in templateTiers.Values) { tu = any; break; }
            if (tu == null)
                continue;
            Upgradeproperties up = MummerUtil.Clone(tu, u.Id);
            up.Notes = "";
            up.Display_Name = u.Display;
            up.Display_Desc = u.Desc;
            up.Priority = u.Tier;
            up.Aiscripts = new List<UnityEngine.Object>();
            up.Costs = MummerUtil.Costs(u.Costs);
            up.Ent_Visual = TowerVisual(te.Defaultvisual, u.Id.Replace("_Upgradeobject", "_Visualproperties"), u.Model, u.Display, stage);
            Register("Upgrade", up.Ent_Visual);
            up.Defaultparam = new List<Upgradeproperties.Param>();
            up.Defaultparams = new Dictionary<string, float>();
            foreach (KeyValuePair<string, float> kv in MummerUtil.Params(u.Params))
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
        foreach (MummerData.Upg u in b.Upgrades)
        {
            Upgradeproperties up, next;
            if (!byKey.TryGetValue(u.Key + u.Tier, out up))
                continue;
            foreach (string n in u.Next)
                if (byKey.TryGetValue(n.Trim(), out next))
                    up.Upgrades.Add(next);
                else
                    MummerLog.Warn(u.Id + ": unknown next upgrade " + n);
        }
        ent.Upgrades = roots;

        being.Entity = ent;
        Beings[b.Id] = being;
        Register("Mob", being);
        Register("Mob", ent);
    }

    // ---------------------------------------------------------------- Understudies

    static void BuildPuppet(MummerData.Puppet p)
    {
        Beingobject tb = MummerUtil.Load<Beingobject>("Mob", "Mob_Smackyminer");
        if (tb == null || tb.Entity == null)
        {
            MummerLog.Error("Mob_Smackyminer not found; Understudies disabled");
            return;
        }
        Entityobject te = tb.Entity;
        Beingobject being = MummerUtil.Clone(tb, p.Id);
        // The Aegis Understudy stands on Sno Sno's mob-blocker block (the one Snobles hold the line with).
        if (p.Costume == MummerData.Costume.Aegis)
        {
            Beingobject snoble = MummerUtil.Load<Beingobject>("Mob", "Mob_Snoble");
            if (snoble != null && snoble.Block != null)
            {
                being.Block = snoble.Block;
                AegisBlock = snoble.Block.name;
            }
        }
        Entityobject ent = MummerUtil.Clone(te, p.Id + "_Entity");
        ent.Name = p.Id;
        ent.Display_Name = p.Display;
        ent.Display_Desc = p.Desc;
        // The game's generic fighter AIs: melee for the plain and Aegis costumes, ranged for the Ballista costume.
        ent.Aiscripts = Scripts("Entity_Ai_Mob_General", "Entity_Ai_Mob_Movement", p.Ranged ? "Entity_Ai_Mob_Ranged" : "Entity_Ai_Mob_Combat");
        ent.Visualscripts = Scripts("Entity_Vis_Animator_Mob", "Entity_Vis_Mob_Attack");
        ent.Upgrades = new List<Upgradeproperties>();
        ent.Upgradesinit = new List<Upgradeproperties>();
        ent.Drops = new List<Link_Resourceset>();
        ent.Costs = new List<Link_Resourceset>();
        ent.Sellable = false;
        ent.Controllable = false;
        ent.Tags = new List<string> { "Mob", "Mobfighter", "Understudy" };
        List<string> stats = new List<string>(p.Stats);
        foreach (Statproperties.Defaultstatgroup g in te.Defaultstat.Stats)
            if (g.Name == "Distancewait") stats.Add("Distancewait=" + g.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
        ent.Defaultstat = Stats(te.Defaultstat, p.Id + "_Statproperties", stats.ToArray());
        Register("Mob", ent.Defaultstat);
        Visualproperties vp = UnityEngine.Object.Instantiate(te.Defaultvisual);
        MummerUtil.Retag(vp, p.Id + "_Visualproperties");
        Visualproperties.Entry e = te.Defaultvisual.Default;
        e.Rootvisualproperty = null;
        string sheet = p.Costume == MummerData.Costume.Plain ? "Understudy-Sheet" : "Understudy" + p.Costume + "-Sheet";
        e.Spriteobject = MummerAssets.Puppet(sheet, p.Id + "_Sprite", te.Defaultvisual.Default.Spriteobject);
        e.Displayname = p.Display;
        e.Displaycolor = new Color(0.84f, 0.75f, 0.92f, 0f);
        GameObject poof = MummerAssets.Vfx("World_Mummer_Stuffing");
        if (poof != null) e.Vfxend = poof;
        vp.Default = e;
        vp.Skinoverride = new List<Visualproperties.Param>();
        vp.Skinoverrides = new Dictionary<string, Visualproperties.Entry>();
        ent.Defaultvisual = vp;
        Register("Mob", vp);
        being.Entity = ent;
        Beings[p.Id] = being;
        Register("Mob", being);
        Register("Mob", ent);
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
        Upgradeproperties t = MummerUtil.Clone(template, MummerData.HeroTooltip);
        t.Notes = "";
        t.Display_Name = MummerData.TooltipName;
        t.Display_Desc = MummerData.TooltipDesc;
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
        HeroBeing = MummerUtil.Clone(tb, MummerData.Hero);
        Entityobject ent = MummerUtil.Clone(te, MummerData.HeroEntity);
        ent.Name = MummerData.Hero;
        ent.Display_Name = MummerData.Displayname;
        ent.Display_Desc = MummerData.HeroDesc;
        // A melee vessel is built like Smacky: Mob_General, Mob_Movement, the stock Mob_Combat (targets, mines ore and
        // sends "Attack", which Entity_Vis_Mob_Attack turns into the Peekaboo animation) and Hero_Buildsell.
        ent.Aiscripts = Strip(te.Aiscripts);
        if (!ent.Aiscripts.Exists(s => s.name == "Entity_Ai_Mob_Combat"))
            ent.Aiscripts.Insert(Mathf.Min(2, ent.Aiscripts.Count), MummerUtil.Script("Entity_Ai_Mob_Combat"));
        ent.Visualscripts = Strip(te.Visualscripts);
        if (!ent.Visualscripts.Exists(s => s.name == "Entity_Vis_Mob_Attack"))
            ent.Visualscripts.Add(MummerUtil.Script("Entity_Vis_Mob_Attack"));
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
        MummerUtil.Retag(vp, MummerData.Hero + "_Visualproperties");
        Visualproperties.Entry e = te.Defaultvisual.Default;
        e.Rootvisualproperty = null;
        e.Spriteobject = MummerAssets.Hero(templateSprite, footstep);
        e.Displaycolor = new Color(0.70f, 0.57f, 0.84f, 1f);
        vp.Default = e;
        vp.Skinoverride = new List<Visualproperties.Param>();
        vp.Skinoverrides = new Dictionary<string, Visualproperties.Entry>();
        ent.Defaultvisual = vp;
        Register("Hero", vp);

        ent.Defaultstat = Stats(te.Defaultstat, MummerData.Hero + "_Statproperties", MummerData.HeroStats);
        Register("Hero", ent.Defaultstat);

        HeroBeing.Entity = ent;
        Register("Hero", HeroBeing);
        Register("Hero", ent);
    }

    static void BuildEntry(Heroentryobject template)
    {
        Entry = UnityEngine.Object.Instantiate(template);
        MummerUtil.Retag(Entry, MummerData.Hero);
        Bl_Address_Group loads = UnityEngine.Object.Instantiate(template.Heroloads);
        MummerUtil.Retag(loads, MummerData.Hero);
        Entry.Heroloads = loads;
        Entry.Towerdisplay = new List<string>(MummerData.Towers);
        Entry.Displayname = MummerData.Displayname;
        Entry.Displaysub = MummerData.Displaysub;
        Entry.Displayskill = MummerData.Displayskill;
        Entry.Color = new Color(0.70f, 0.57f, 0.84f, 1f);
        // The 12x12 sprite in player lists, the setup bar and leaderboards: the ready-up portrait in its brackets.
        Entry.Icon = MummerAssets.Single("Readyup6mummer", 12) ?? template.Icon;
        Entry.Worldsplash = MummerAssets.Single("Mummersplash", 128) ?? template.Worldsplash;
        Entry.Endscreenwinicon = MummerAssets.Single("Mummerendstat1", 32) ?? template.Endscreenwinicon;
        Entry.Endscreenloseicon = MummerAssets.Single("Mummerendstat2", 32) ?? template.Endscreenloseicon;
        Entry.Worldsprite = MummerAssets.Worldsprite(template.Worldsprite);
        Texture fall = MummerAssets.Sheet("MummerFall-Sheet");
        Texture land = MummerAssets.Sheet("MummerIntroR-Sheet");
        Texture idle = MummerAssets.Sheet("MummerIdlejustidle-Sheet");
        if (fall != null) Entry.Loadingsprite = fall;
        if (land != null) Entry.Landingsprite = land;
        if (idle != null) Entry.Idlesprite = idle;
        Dictionary<string, UnityEngine.Object> heroes = MummerUtil.Folder("Hero");
        heroes[MummerData.Hero] = Entry;
    }
}
