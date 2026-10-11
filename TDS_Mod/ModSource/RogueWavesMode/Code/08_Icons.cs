// The weather icons for the warning popup, 12x12 in the pack's palette (docs/STYLE.md), added to the game's "Icon_Types"
// table the way Argo adds its own (kit docs/16 §5): early, before the table is first read, since it freezes then. If
// they didn't make it, For() falls back to a stock icon so the popup is never blank.
public static class RWIcons
{
    static readonly Dictionary<char, Color32> palette = new Dictionary<char, Color32>
    {
        { 'D', new Color32(0x2E, 0x4A, 0x62, 255) },   // Deep Swell
        { 'T', new Color32(0x3F, 0x8F, 0x96, 255) },   // Rogue Teal
        { 'F', new Color32(0xE9, 0xF1, 0xEE, 255) },   // Foam
        { 'C', new Color32(0xE2, 0x72, 0x5B, 255) },   // Storm Coral
        { 'c', new Color32(0xB5, 0x52, 0x40, 255) },   // Storm Coral, shade
        { 'S', new Color32(0xD8, 0xC4, 0x9A, 255) },   // Wet Sand
        { 's', new Color32(0xA8, 0x93, 0x6A, 255) },   // Wet Sand, shade
    };

    static readonly string[] Rising =
    {
        "....FF......",
        "...FFFF.....",
        "..FFFFFF....",
        "....FF......",
        "....FF....F.",
        "....FF...FTT",
        ".FF.FF..FTTT",
        "FTTTTTTTTTTT",
        "TTTTTTTTTTTT",
        "TTTDTTTTTDTT",
        "DTTDDTTDTDDT",
        "DDDDDDDDDDDD",
    };

    static readonly string[] Quickening =
    {
        "............",
        "F...F...F...",
        ".F...F...F..",
        "..F...F...F.",
        "...F...F...F",
        "..F...F...F.",
        ".F...F...F..",
        "F...F...F...",
        "............",
        "TTTTTTTTTTTT",
        "DTTDTTTDTTDT",
        "DDDDDDDDDDDD",
    };

    static readonly string[] Nesting =
    {
        "............",
        ".....CC.....",
        "....CCCC....",
        "...CCCCcC...",
        "...CCCCcc...",
        "...CCCccc...",
        ".S..CCcc..S.",
        "SsS.SSSS.SsS",
        "SSSSSSSSSSSS",
        ".sSSsSSsSSs.",
        "..SSSSSSSS..",
        "...ssSSss...",
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

    public static void Add()
    {
        try
        {
            Iconobject types = Tds_Masterdatabase.Instance != null ? Tds_Masterdatabase.Instance.Getitem("Icons", "Icon_Types") as Iconobject : null;
            if (types == null || types.Icons == null) { RWLog.Warn("icons: no Icon_Types table"); return; }
            Iconobject.Icon like = null;
            foreach (Iconobject.Icon v in types.Icons.Values) { like = v; break; }
            Add(types, RWChart.RisingWater, Rising, like);
            Add(types, RWChart.QuickeningCurrent, Quickening, like);
            Add(types, RWChart.NestingGround, Nesting, like);
        }
        catch (Exception e) { RWLog.Warn("icons: " + e.Message); }
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
