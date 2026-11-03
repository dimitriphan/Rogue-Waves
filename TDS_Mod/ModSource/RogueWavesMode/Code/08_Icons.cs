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
                AddSprite(Library[i, 0] == "Pure_Aura" && stats != null ? stats : types, Library[i, 0], s, Library[i, 2], like);
            }
            foreach (string name in RWContent.IconNames)
            {
                Sprite s = RWAssets.Sprite(name);
                if (s != null) { AddSprite(types, name, s, name, like); added++; }
            }
            RWLog.Info("icons: three weathers, " + added + " from the library");   // Run-SelfTests.ps1 -BootCheck waits for this line
        }
        catch (Exception e) { RWLog.Warn("icons: " + e.Message); }
    }

    static void AddSprite(Iconobject into, string name, Sprite sprite, string display, Iconobject.Icon like)
    {
        if (into == null || into.Icons == null || into.Icons.ContainsKey(name))
            return;
        Iconobject.Icon icon = new Iconobject.Icon();
        icon.Name = name;
        icon.Image = sprite;
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
        icon.Image = Pixel(rows, "Icon_" + name);
        icon.Color = Color.white;
        icon.Material = like != null ? like.Material : null;
        icon.Displayname = RWWeather.Display(w);
        into.Icons[name] = icon;
        if (into.Iconslist != null)
            into.Iconslist.Add(icon);
    }

    static Sprite Pixel(string[] rows, string name)
    {
        int h = rows.Length, w = rows[0].Length;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;
        Color32[] px = new Color32[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                Color32 c;
                px[(h - 1 - y) * w + x] = x < rows[y].Length && palette.TryGetValue(rows[y][x], out c) ? c : new Color32(0, 0, 0, 0);
            }
        tex.SetPixels32(px);
        tex.Apply(false);
        tex.name = name;
        Sprite s = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 20f, 0, SpriteMeshType.FullRect);
        s.name = name;
        return s;
    }
}
