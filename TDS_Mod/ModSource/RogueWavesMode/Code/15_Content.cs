// The mode's game data, built the same on every machine at boot so online clients resolve the same names (kit docs/05,
// docs/15 §3): the four None commands of RWRun, VENDCINT and its buttons, the Directions any vessel can take and the
// Cardinals and Trinkets, the way Blockage's data is laid out, cloned from Smacky's objects (his group is loaded at boot for that).
public static class RWContent
{
    public const string OrderName = "Command_RW_Order", AskName = "Command_RW_Ask", RunName = "Command_RW_Run", PriceName = "Command_RW_Price";
    public const string ShopName = "Command_RW_Shop", FundName = "Command_RW_Fund", BoardName = "Command_RW_Board";
    public const string Vendor = "Mob_Vendcint", VendorSheet = "Vendcint-Sheet";
    public static Commandobject OrderCommand, AskCommand, RunCommand, PriceCommand, ShopCommand, FundCommand, BoardCommand;
    public static readonly Commandobject[] LogCommands = new Commandobject[RWBoard.Lines];   // the MVP board's lines (RWBoard)
    public static string LogName(int i) { return "Command_RW_Log" + i; }
    public static Beingobject Vendcint;
    public static bool Built;

    // Library sprites the shop and the Directions show by name, put in the icon table at boot.
    public static readonly string[] IconNames =
    {
        "RogueWaves_Crest32", "RogueWaves_Shop", "RogueWaves_Fund", "RogueWaves_Board", "RogueWaves_Leave", "RogueWaves_Stay",
        "RogueWaves_Objective_Meteor", "RogueWaves_Objective_Hull", "RogueWaves_Objective_Clock", "RogueWaves_Objective_Hands", "RogueWaves_Objective_Nest",
    };

    static Bl_Address.Loadgroup group;
    static readonly Dictionary<string, Bl_Address.Loadlist> lists = new Dictionary<string, Bl_Address.Loadlist>();

    public static string CommandFor(string link)
    {
        return link == RWRun.OrderLink ? OrderName : link == RWRun.AskLink ? AskName : link == RWRun.PriceLink ? PriceName : RunName;
    }

    public static IEnumerator Boot()
    {
        Heroentryobject smacky = RWUtil.Item<Heroentryobject>("Hero", "Hero_Smacky");
        if (smacky == null || smacky.Heroloads == null)
        {
            RWLog.Warn("no Hero_Smacky entry: no shop, Directions, Cardinals or Trinkets");
            yield break;
        }
        Bl_Address.Loadgroup g = null;
        try { g = Bl_Address.Load(smacky.Heroloads.Objectname); }
        catch (Exception e) { RWLog.Warn("loading " + smacky.Heroloads.Objectname + ": " + e.Message); }
        float waited = 0f;
        while (g != null && !g.Loaded && waited < 180f)
        {
            waited += Time.unscaledDeltaTime;
            yield return null;
        }
        yield return RWCardinals.LoadStock();
        try { Build(); }
        catch (Exception e) { RWLog.Warn("content: " + e); }
    }

    static void Build()
    {
        group = new Bl_Address.Loadgroup();
        group.Items = new List<Bl_Address.Loadlist>();
        group.Loaded = true;
        group.Percent = 100;
        foreach (string label in new[] { "Mob", "Upgrade", "Command", "Artifact" })
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
        Bl_Address.Loadedfolders["RW_Content"] = group;
        BuildCommands();
        BuildVendcint();
        RWDirections.Build();
        RWCardinals.Build();
        RWObjectives.Build();
        RWDifficulty.BuildHealth();
        Built = true;
        RWLog.Info("content: VENDCINT " + (Vendcint != null) + ", " + RWDirections.List.Count + " Direction upgrades, " + RWCardinals.Sold.Count + " Cardinals and Trinkets on sale, " + RWObjectives.All.Length + " objectives");
    }

    public static void Register(string label, Bl_ScriptableObject obj)
    {
        Bl_Address.Loadlist list = lists[label];
        list.Items[obj.Objectname] = obj;
        if (!list.Index.Contains(obj))
            list.Index.Add(obj);
        FixedString64Bytes id = new FixedString64Bytes(obj.Id);
        list.Uids[id] = obj;
        obj.Loadsource = "RW_Content";
        Bl_Address.Groupsname[new FixedString64Bytes(obj.Objectname)] = obj;
        Bl_Address.Groupsids[id] = obj;
    }

    public static void Retag(Bl_ScriptableObject obj, string name)
    {
        obj.name = name;
        obj.Objectname = name;
        obj.Id = ModKitUtil.Clip(name.ToLowerInvariant() + "-roguewaves");
    }

    static readonly Dictionary<string, UnityEngine.Object> carriers = new Dictionary<string, UnityEngine.Object>();

    // What the game's Upgrade reads from an Aiscripts entry is its name (Type.GetType), so a named carrier stands in.
    public static UnityEngine.Object Script(string className)
    {
        UnityEngine.Object carrier;
        if (carriers.TryGetValue(className, out carrier) && carrier != null)
            return carrier;
        carrier = new TextAsset("");
        carrier.name = className;
        carrier.hideFlags = HideFlags.DontUnloadUnusedAsset;
        carriers[className] = carrier;
        return carrier;
    }

    // An upgrade from any stock one as a template: no costs, no look, the pack's numbers. A stock script reads each of
    // its keys with no fallback, so a pack upgrade lists them all (pitfall #117).
    public static Upgradeproperties Upgrade(string name, string display, string desc, string script, params object[] keyValue)
    {
        Upgradeproperties template = Template();
        Upgradeproperties u = UnityEngine.Object.Instantiate(template);
        Retag(u, name);
        u.Notes = "";
        u.Display_Name = display;
        u.Display_Desc = desc;
        u.Priority = -3;
        u.Costs = new List<Link_Resourceset>();
        u.Defaultparam = new List<Upgradeproperties.Param>();
        u.Defaultparams = new Dictionary<string, float>();
        u.Aiscripts = new List<UnityEngine.Object>();
        u.Ent_Visual = null;
        u.Upgrades = new List<Upgradeproperties>();
        if (script != null)
            u.Aiscripts.Add(Script(script));
        for (int i = 0; i + 1 < keyValue.Length; i += 2)
        {
            Upgradeproperties.Param p = new Upgradeproperties.Param();
            p.Name = (string)keyValue[i];
            p.Value = Convert.ToSingle(keyValue[i + 1]);
            u.Defaultparam.Add(p);
            u.Defaultparams[p.Name] = p.Value;
        }
        Register("Upgrade", u);
        return u;
    }

    static Upgradeproperties template;

    static Upgradeproperties Template()
    {
        if (template != null)
            return template;
        foreach (string n in new[] { "Tower_Smackyarcher_Range2_Upgradeobject", "Hero_Boutman_Tooltip_Upgradeproperties" })
        {
            template = ModKitUtil.Load<Upgradeproperties>("Upgrade", n);
            if (template != null) return template;
        }
        foreach (Bl_Address.Loadlist l in Bl_Address.Groups["Upgrade"])
            foreach (UnityEngine.Object o in l.Items.Values)
                if (template == null && o is Upgradeproperties)
                    template = (Upgradeproperties)o;
        return template;
    }

    // ---------------------------------------------------------------- commands

    static Commandobject TemplateCommand()
    {
        Commandobject t = ModKitUtil.Load<Commandobject>("Command", "Command_Tower_Smackyhut_Convert");
        if (t == null)
            foreach (Bl_Address.Loadlist l in Bl_Address.Groups["Command"])
                foreach (UnityEngine.Object o in l.Items.Values)
                    if (t == null && o is Commandobject)
                        t = (Commandobject)o;
        return t;
    }

    static Commandobject Command(Commandobject template, string id, Commandobject.Styles style, string name, string desc, string icon, Entity_Command_Base funct, string key)
    {
        Commandobject c = UnityEngine.Object.Instantiate(template);
        Retag(c, id);
        c.Style = style;
        c.Display_Name = name;
        c.Display_Desc = desc;
        c.Color = RWDialog.Teal;
        c.Keybind = key != "" ? new List<string> { key } : new List<string>();
        c.Keybindstate = "Onrelease";
        c.Priority = 2;
        c.Commandfuncts = new List<Entity_Command_Base> { funct };
        c.Icon = icon;
        c.Customui = null;
        Register("Command", c);
        return c;
    }

    static void BuildCommands()
    {
        Commandobject t = TemplateCommand();
        if (t == null) { RWLog.Warn("no command template: no shop, votes or Directions"); return; }
        OrderCommand = Command(t, OrderName, Commandobject.Styles.None, "", "", "Move", new RWNoButton(), "");
        AskCommand = Command(t, AskName, Commandobject.Styles.None, "", "", "Move", new RWNoButton(), "");
        for (int i = 0; i < RWBoard.Lines; i++)
            LogCommands[i] = Command(t, LogName(i), Commandobject.Styles.None, "", "", "Move", new RWNoButton(), "");
        RunCommand = Command(t, RunName, Commandobject.Styles.None, "", "", "Move", new RWNoButton(), "");
        PriceCommand = Command(t, PriceName, Commandobject.Styles.None, "", "", "Move", new RWNoButton(), "");
        ShopCommand = Command(t, ShopName, Commandobject.Styles.Button, "SHOP", "Spend Pure Aura on Cardinals, Trinkets and the next rank of your Directions", Icon("RogueWaves_Shop"), new RWShopButton(), "3");
        FundCommand = Command(t, FundName, Commandobject.Styles.Button, "FUND TRANSFER", "Send base aura to a friend", Icon("RogueWaves_Fund"), new RWFundButton(), "4");
        BoardCommand = Command(t, BoardName, Commandobject.Styles.Button, "OBJECTIVE", "An optional objective for the next Tide, for bonus Pure Aura", Icon("RogueWaves_Board"), new RWBoardButton(), "5");
    }

    public static string Icon(string name)
    {
        Sprite s = null;
        try { s = Helper_Uitools.Geticon(name); } catch (Exception) { }
        return s != null ? name : "Move";
    }

    // ---------------------------------------------------------------- VENDCINT

    static void BuildVendcint()
    {
        Beingobject miner = ModKitUtil.Load<Beingobject>("Mob", "Mob_Smackyminer");
        Texture2D sheet = RWAssets.Texture(VendorSheet);
        if (miner == null || miner.Entity == null || sheet == null)
        {
            RWLog.Warn("VENDCINT not built: miner " + (miner != null) + ", sheet " + (sheet != null));
            return;
        }
        Beingobject being = UnityEngine.Object.Instantiate(miner);
        Retag(being, Vendor);
        Entityobject te = miner.Entity;
        Entityobject ent = UnityEngine.Object.Instantiate(te);
        Retag(ent, Vendor + "_Entity");
        ent.Name = Vendor;
        ent.Display_Name = "VENDCINT";
        ent.Display_Desc = "A sentient vending machine. It sells Cardinals, Trinkets and Direction ranks for Pure Aura, moves aura between friends, and posts an objective every break";
        ent.Aiscripts = new List<UnityEngine.Object> { Script("Entity_Ai_Mob_General") };
        ent.Visualscripts = new List<UnityEngine.Object> { Script("Entity_Vis_Animator_Mob") };
        ent.Upgrades = new List<Upgradeproperties>();
        ent.Upgradesinit = new List<Upgradeproperties>();
        ent.Drops = new List<Link_Resourceset>();
        ent.Costs = new List<Link_Resourceset>();
        ent.Sellable = false;
        ent.Sellhealthfactor = false;
        ent.Controllable = true;
        ent.Targetable = false;
        ent.Destroyonparent = false;
        ent.Tags = new List<string> { "Vendcint" };
        ent.Defaultstat = VendorStats(te.Defaultstat);
        Register("Mob", ent.Defaultstat);
        ent.Defaultvisual = VendorVisual(te.Defaultvisual, sheet);
        Register("Mob", ent.Defaultvisual);
        being.Entity = ent;
        Vendcint = being;
        Register("Mob", being);
        Register("Mob", ent);
    }

    static Statproperties VendorStats(Statproperties template)
    {
        Statproperties stats = UnityEngine.Object.Instantiate(template);
        Retag(stats, Vendor + "_Statproperties");
        List<Statproperties.Defaultstatgroup> list = new List<Statproperties.Defaultstatgroup>();
        foreach (Statproperties.Defaultstatgroup g in template.Stats)
        {
            Statproperties.Defaultstatgroup n = new Statproperties.Defaultstatgroup();
            n.Name = g.Name;
            n.Value_String = g.Value_String;
            n.Style = g.Style;
            n.Value_Type = g.Value_Type;
            n.Display_Type = g.Display_Type;
            n.Display_Exposed = false;
            n.Value = g.Name == "Health" ? 1000f : g.Name == "Speed" || g.Name == "Damage" ? 0f : g.Value;
            n.Leveladd = 0f;
            list.Add(n);
        }
        stats.Stats = list;
        return stats;
    }

    // Sheet: 32 px cells, six columns of Idle, rows v0-v3; the fall is the same frames on the game's own offset.
    static Visualproperties VendorVisual(Visualproperties template, Texture2D sheet)
    {
        Visualproperties vp = UnityEngine.Object.Instantiate(template);
        Retag(vp, Vendor + "_Visualproperties");
        Visualproperties.Entry e = template.Default;
        e.Rootvisualproperty = null;
        e.Spriteobject = VendorSprites(sheet, template.Default.Spriteobject);
        e.Displayname = "VENDCINT";
        e.Displaycolor = new Color(0.847f, 0.769f, 0.604f, 0f);
        vp.Default = e;
        vp.Skinoverride = new List<Visualproperties.Param>();
        vp.Skinoverrides = new Dictionary<string, Visualproperties.Entry>();
        return vp;
    }

    static Spriteobject VendorSprites(Texture2D sheet, Spriteobject template)
    {
        sheet.filterMode = FilterMode.Point;
        const int cell = 32;
        int cols = sheet.width / cell, rows = sheet.height / cell;
        List<Sprite> frames = new List<Sprite>();
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
            {
                Sprite s = Sprite.Create(sheet, new Rect(c * cell, sheet.height - (r + 1) * cell, cell, cell), new Vector2(0.5f, 0f), 20f, 0, SpriteMeshType.FullRect);
                s.name = Vendor + "_" + frames.Count;
                frames.Add(s);
            }
        Spriteobject so = ScriptableObject.CreateInstance<Spriteobject>();
        so.name = Vendor + "_Sprite";
        so.Defaultspeedmulti = template != null ? template.Defaultspeedmulti : 4f;
        so.Defaultfolder = template != null && !string.IsNullOrEmpty(template.Defaultfolder) ? template.Defaultfolder : "Idle";
        so.Itemslist = new List<Spritesinlist>();
        so.Items = new Dictionary<string, Spritesinlist>();
        foreach (string name in new[] { "Idle", "Move", "Falling", "Land" })
        {
            Spritesinlist a = new Spritesinlist();
            a.Name = name;
            a.Dump = new List<Sprite>();
            a.Dumpsplit = cols;
            a.Sprites = new List<Spritevariantlist>();
            a.Loop = name != "Land";
            a.Reset = name == "Land";
            a.Priority = name == "Idle" || name == "Move" ? 0 : 2;
            a.Speed = 1f;
            a.Spriteto = name == "Land" ? "Idle" : "";
            for (int v = 0; v < rows; v++)
            {
                Spritevariantlist variant = new Spritevariantlist();
                variant.Variant = new List<Spritecont>();
                for (int f = 0; f < cols; f++)
                {
                    Spritecont cont = new Spritecont();
                    cont.Item = frames[v * cols + f];
                    cont.Sound = "";
                    variant.Variant.Add(cont);
                }
                a.Sprites.Add(variant);
            }
            so.Itemslist.Add(a);
            so.Items[name] = a;
        }
        so.Preview = frames.Count > 0 ? frames[0] : null;
        return so;
    }
}

// A None command needs one entry to be a command at all; it is never drawn or pressed.
public class RWNoButton : Entity_Command_Base
{
    public override void Activate(bool Keybind) { if (Select != null) Select.Activecommand = null; }
}

public class RWShopButton : Entity_Command_Base
{
    public override void Activate(bool Keybind)
    {
        RWShop.Open("Shop");
        if (Select != null) Select.Activecommand = null;
    }
}

public class RWFundButton : Entity_Command_Base
{
    public override void Activate(bool Keybind)
    {
        RWShop.Open("Fund");
        if (Select != null) Select.Activecommand = null;
    }
}

public class RWBoardButton : Entity_Command_Base
{
    public override void Activate(bool Keybind)
    {
        RWShop.Open("Board");
        if (Select != null) Select.Activecommand = null;
    }
}
