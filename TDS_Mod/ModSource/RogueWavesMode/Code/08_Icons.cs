// The weather icons for the warning popup, 12x12 in the pack's palette (docs/STYLE.md); temporary art, the rows are
// Art/Temporary/game_size/RogueWaves_*.png as concept-tools/temp_art/ui_icons.py prints them (Rising Water: Dimitri's pick
// of 2026-11-03, version 2 as variants_icons.py prints it), added to the game's "Icon_Types"
// table the way Argo adds its own (kit docs/09 #20): early, before the table is first read, since it freezes then. If
// they didn't make it, For() falls back to a stock icon so the popup is never blank.
public static class RWIcons
{
    static readonly Dictionary<char, Color32> palette = new Dictionary<char, Color32>
    {
        { 'C', new Color32(0xE2, 0x72, 0x5B, 255) },   // Storm Coral
        { 'D', new Color32(0x2E, 0x4A, 0x62, 255) },   // Deep Swell
        { 'F', new Color32(0xE9, 0xF1, 0xEE, 255) },   // Foam
        { 'S', new Color32(0xD8, 0xC4, 0x9A, 255) },   // Wet Sand
        { 'T', new Color32(0x3F, 0x8F, 0x96, 255) },   // Rogue Teal
        { 'c', new Color32(0xB5, 0x52, 0x40, 255) },   // Storm Coral, shade
        { 'd', new Color32(0x22, 0x38, 0x4B, 255) },   // Deep Swell, dark
        { 'f', new Color32(0xC3, 0xD8, 0xD3, 255) },   // Foam, shade
        { 'k', new Color32(0xEE, 0x9A, 0x86, 255) },   // Storm Coral, light
        { 'l', new Color32(0x6D, 0xB4, 0xB9, 255) },   // Rogue Teal, light
        { 's', new Color32(0xA8, 0x93, 0x6A, 255) },   // Wet Sand, shade
        { 't', new Color32(0x32, 0x73, 0x78, 255) },   // Rogue Teal, shade
    };

    static readonly string[] Rising =
    {
        "..F.........",
        ".FFF........",
        "FFFFF.......",
        "..F.....SSSs",
        "..F.....Ssss",
        "......SSSsss",
        "......Ssssss",
        "....SSSsssss",
        "FfFlTTTTTTTt",
        "lTTTTTTTTTTt",
        "TTTTtTTTTtTt",
        "DDDDDDDDDDDD",
    };

    static readonly string[] Quickening =
    {
        "............",
        "FF..FF..FF..",
        ".FF..FF..FF.",
        "..Ff..Ff..Ff",
        ".Ff..Ff..Ff.",
        "Ff..Ff..Ff..",
        "............",
        "lTTTlTTTTlTT",
        "TTlTTTTlTTTt",
        "TTTTTlTTTTTt",
        "tTtTTTtTTtTt",
        "DDDDDDDDDDDD",
    };

    static readonly string[] Nesting =
    {
        "............",
        ".....kC.....",
        "....kCCC....",
        "...kCCCCc...",
        "...CCCCCc...",
        "...CCCCCc...",
        "...CCCCcc...",
        ".SsSCCcccSs.",
        "SSSSsSSSSsSs",
        "sSSSSSSSSSSs",
        ".sSsSSsSSs..",
        "..ssssssss..",
    };

    public static string Name(int w)
    {
        switch (w)
        {
            case RWChart.RisingWater: return "RogueWaves_RisingWater";
            case RWChart.QuickeningCurrent: return "RogueWaves_Quickening";
            case RWChart.NestingGround: return "RogueWaves_Nesting";
        }
        return "";
    }

    static string Fallback(int w)
    {
        switch (w)
        {
            case RWChart.RisingWater: return "Hightowerdestroyed";
            case RWChart.QuickeningCurrent: return "Map_Chrono";
            case RWChart.NestingGround: return "Highmobdestroyed";
        }
        return "Map_Chrono";
    }

    public static string For(int w)
    {
        Sprite s = null;
        try { s = Helper_Uitools.Geticon(Name(w)); } catch (Exception) { }
        return s != null ? Name(w) : Fallback(w);
    }

    public static Sprite SpriteFor(int w)
    {
        try { return Helper_Uitools.Geticon(For(w)); }
        catch (Exception) { return null; }
    }

    // The library's icons the HUD and the shop name, by the name the Icon_Types or Icon_Stats table lists them under
    // (a counter's icon is in Icon_Stats under the stat's name, kit docs/05 §9).
    public static readonly string[,] Library =
    {
        { "RogueWaves_Retry", "RogueWaves_Retry", "Retries" }, { "RogueWaves_Vote", "RogueWaves_Vote", "Vote" }, { "RogueWaves_Crest", "RogueWaves_Crest", "Rogue Waves" },
        { "RogueWaves_Depth", "RogueWaves_Depth", "Depth" }, { "Pure_Aura", "RogueWaves_PureAura", "Pure Aura" },
    };

    public static void Add()
    {
#if ROGUEWAVESMODE_GAPS
        if (Array.IndexOf(Environment.GetCommandLineArgs(), "-rwthrowicons") >= 0) throw new Exception("MODE-A2: a test's throw");
#endif
        try
        {
            Iconobject types = Tds_Masterdatabase.Instance != null ? Tds_Masterdatabase.Instance.Getitem("Icons", "Icon_Types") as Iconobject : null;
            Iconobject stats = Tds_Masterdatabase.Instance != null ? Tds_Masterdatabase.Instance.Getitem("Icons", "Icon_Stats") as Iconobject : null;
            if (types == null || types.Icons == null) { RWLog.Warn("icons: no Icon_Types table"); return; }
            Iconobject.Icon like = null;
            foreach (Iconobject.Icon v in types.Icons.Values) { like = v; break; }
            Add(types, RWChart.RisingWater, Rising, like);
            Add(types, RWChart.QuickeningCurrent, Quickening, like);
            Add(types, RWChart.NestingGround, Nesting, like);
            int added = 0;
            for (int i = 0; i < Library.GetLength(0); i++)
            {
                Sprite s = RWAssets.Sprite(Library[i, 1]);
                if (s == null) continue;
                added++;
                bool stat = Library[i, 0] == "Pure_Aura" && stats != null;
                AddSprite(stat ? stats : types, Library[i, 0], s, Library[i, 2], like, stat ? 0 : ModKitAssets.CommandIcon);
            }
            foreach (string name in RWContent.IconNames)
            {
                Sprite s = RWAssets.Sprite(name);
                if (s != null) { AddSprite(types, name, s, name, like, ModKitAssets.CommandIcon); added++; }
            }
            RWLog.Info("icons: three weathers, " + added + " from the library");   // Run-SelfTests.ps1 -BootCheck waits for this line
        }
        catch (Exception e) { RWLog.Warn("icons: " + e.Message); }
    }

    // Icon_Types is drawn by the command buttons and the warning popup in 16-unit boxes: a 12 px icon goes in as a clear
    // 16 px box, so it draws one unit per pixel there (pitfall #156). Icon_Stats' counters draw 12 px as they are.
    static void AddSprite(Iconobject into, string name, Sprite sprite, string display, Iconobject.Icon like, int box)
    {
        if (into == null || into.Icons == null || into.Icons.ContainsKey(name))
            return;
        Iconobject.Icon icon = new Iconobject.Icon();
        icon.Name = name;
        icon.Image = ModKitAssets.Boxed(sprite, box, "Icon_" + name);
        icon.Color = Color.white;
        icon.Material = like != null ? like.Material : null;
        icon.Displayname = display;
        into.Icons[name] = icon;
        if (into.Iconslist != null)
            into.Iconslist.Add(icon);
    }

    static void Add(Iconobject into, int w, string[] rows, Iconobject.Icon like)
    {
        string name = Name(w);
        if (into.Icons.ContainsKey(name))
            return;
        Iconobject.Icon icon = new Iconobject.Icon();
        icon.Name = name;
        icon.Image = ModKitAssets.Pixel(rows, "Icon_" + name, palette, ModKitAssets.CommandIcon);
        icon.Color = Color.white;
        icon.Material = like != null ? like.Material : null;
        icon.Displayname = RWWeather.Display(w);
        into.Icons[name] = icon;
        if (into.Iconslist != null)
            into.Iconslist.Add(icon);
    }

    // ---------------------------------------------------------------- the chart view's icons (docs/mode/CHART_NAVIGATION.md §3.6)
    // 12 x 12, coloured like the stock cards' icons; the tick white like the stock "back" mark. The rows are
    // concept-tools/sea_chart_look.py's PIX, its palette PAL; temporary art (docs/TEMP_ART.md).
    static readonly Dictionary<char, Color32> chartPalette = new Dictionary<char, Color32>
    {
        { 'w', new Color32(0xF2, 0xF4, 0xF3, 255) },
        { 'g', new Color32(0xA9, 0xB1, 0xB8, 255) },
        { 'G', new Color32(0x7D, 0x86, 0x8F, 255) },
        { 'o', new Color32(0xF0, 0x9A, 0x3E, 255) },
        { 'y', new Color32(0xF6, 0xD8, 0x6B, 255) },
        { 'b', new Color32(0x7A, 0x4E, 0x33, 255) },
        { 'B', new Color32(0xA8, 0x72, 0x48, 255) },
        { 's', new Color32(0xD8, 0xC4, 0x9A, 255) }
    };

    static readonly string[] HarbourRows =
    {
        "....gggg....",
        "....g..G....",
        "....ggGG....",
        ".....gG.....",
        "...ggggGG...",
        ".....gG.....",
        ".....gG.....",
        "g....gG....G",
        "gg...gG...GG",
        ".gg..gG..GG.",
        "..ggg..GGG..",
        "....gGGG....",
    };

    static readonly string[] CoveRows =
    {
        "............",
        ".....y......",
        "....yo......",
        "....yoo.y...",
        "...yooooy...",
        "...yoyyoy...",
        "...ooyyoo...",
        "....oyyo....",
        ".b........b.",
        "..bBBbbBBb..",
        ".bBBbbbbBBb.",
        "..b......b..",
    };

    static readonly string[] WreckRows =
    {
        "............",
        "............",
        "..bbbbbbbb..",
        ".bBBBBBBBBb.",
        ".bBBBBBBBBb.",
        ".bbbbyybbbb.",
        ".bBBByyBBBb.",
        ".bBBBBBBBBb.",
        ".bBBBBBBBBb.",
        ".bbbbbbbbbb.",
        "............",
        "............",
    };

    static readonly string[] TickRows =
    {
        "............",
        "............",
        "..........w.",
        ".........ww.",
        "........ww..",
        "w......ww...",
        "ww....ww....",
        ".ww..ww.....",
        "..wwww......",
        "...ww.......",
        "............",
        "............",
    };

    static readonly string[] BoatRows =
    {
        ".....b......",
        ".....bs.....",
        ".....bss....",
        ".....bsss...",
        ".....bssss..",
        ".....bsssss.",
        ".....b......",
        "wwwwwwwwwwww",
        ".wwwwwwwwwg.",
        "..gggggggg..",
        "............",
        "............",
    };

    static readonly Dictionary<string, Sprite> chartIcons = new Dictionary<string, Sprite>();

    // "Harbour", "Cove", "Wreck", "Tick" or "Boat", at 12 px (box 0) or centred in a clear box (a card's 16).
    public static Sprite Chart(string name, int box = 0)
    {
        string key = name + "#" + box;
        Sprite made;
        if (chartIcons.TryGetValue(key, out made) && made != null)
            return made;
        string[] rows = name == "Harbour" ? HarbourRows : name == "Cove" ? CoveRows : name == "Wreck" ? WreckRows : name == "Tick" ? TickRows : name == "Boat" ? BoatRows : null;
        if (rows == null)
            return null;
        made = ModKitAssets.Pixel(rows, "RW_Chart_" + name + (box > 0 ? "_" + box : ""), chartPalette, box);
        chartIcons[key] = made;
        return made;
    }

    public static Sprite Stop(int stop, int box = 0)
    {
        return stop == RWChart.Cove ? Chart("Cove", box) : stop == RWChart.Wreck ? Chart("Wreck", box) : null;
    }
}
