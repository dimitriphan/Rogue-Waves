using UnityEngine.UI;
// The SALVAGE button on every vessel's bar during a Riptide run (key 8), and the icon it shares with the RIPTIDE
// switch. The Commandobject is built the same way on every machine at boot, so each client can draw the button the
// host adds to its vessel (RiptideSalvage.Ensure).
public static class RiptideCommands
{
    public static Commandobject Salvage;
    public static Sprite IconSprite;
    public static bool IconsReady;

    // Helper_Uitools copies the Iconobjects into a private table the first time any icon is asked for, and never again
    // (kit docs/09): the icon goes in at the very start of boot, and CheckIcons falls back to a stock one if it missed.
    public static void AddIcons()
    {
        try
        {
            IconSprite = Pixel(RiptideData.SalvageIconRows, "Icon_" + RiptideData.SalvageIcon);
            Tds_Masterdatabase db = Tds_Masterdatabase.Instance;
            Iconobject types = db != null ? db.Getitem("Icons", "Icon_Types") as Iconobject : null;
            if (types == null || types.Icons == null)
            {
                RiptideLog.Warn("icons: the game's Icon_Types is not loaded yet");
                return;
            }
            if (types.Icons.ContainsKey(RiptideData.SalvageIcon))
                return;
            Iconobject.Icon like = null;
            foreach (Iconobject.Icon v in types.Icons.Values) { like = v; break; }
            Iconobject.Icon icon = new Iconobject.Icon();
            icon.Name = RiptideData.SalvageIcon;
            icon.Image = IconSprite;
            icon.Color = Color.white;
            icon.Material = like != null ? like.Material : null;
            icon.Displayname = RiptideData.SalvageName;
            types.Icons[RiptideData.SalvageIcon] = icon;
            if (types.Iconslist != null)
                types.Iconslist.Add(icon);
        }
        catch (Exception e) { RiptideLog.Warn("icons: " + e.Message); }
    }

    public static void CheckIcons()
    {
        Sprite s = null;
        try { s = Helper_Uitools.Geticon(RiptideData.SalvageIcon); } catch (Exception) { }
        IconsReady = s != null;
        if (IconsReady || Salvage == null)
            return;
        foreach (string stock in new[] { "Upgrade", "Move" })
        {
            try { s = Helper_Uitools.Geticon(stock); } catch (Exception) { s = null; }
            if (s != null)
            {
                Salvage.Icon = stock;
                break;
            }
        }
        RiptideLog.Warn("the icon table was built before Riptide's icon went in; SALVAGE uses '" + Salvage.Icon + "'");
    }

    // The game's bar looks up Bl_Inputs.Actions[key] for every key a command lists and throws when the input map has no
    // such action (Game_Selection.Addcommand), which would cost the vessel its whole bar. Shipped commands use 1, 4-7
    // and Quickaction, so key 8 is checked once the input map is loaded; without it SALVAGE is a click-only button.
    public static bool KeyChecked;

    public static void CheckKey()
    {
        if (KeyChecked || Salvage == null || Blockage.Bl_Inputs.Actions == null || Blockage.Bl_Inputs.Actions.Count == 0)
            return;
        KeyChecked = true;
        if (Blockage.Bl_Inputs.Actions.ContainsKey(RiptideData.SalvageKey))
            return;
        Salvage.Keybind = new List<string>();
        RiptideLog.Warn("the game has no '" + RiptideData.SalvageKey + "' key; SALVAGE is a click-only button");
    }

    public static void Build()
    {
        Salvage = ScriptableObject.CreateInstance<Commandobject>();
        RiptideUtil.Retag(Salvage, RiptideData.SalvageCommand);
        Salvage.Style = Commandobject.Styles.Button;
        Salvage.Display_Name = RiptideData.SalvageName;
        Salvage.Display_Desc = RiptideData.SalvageDesc;
        Salvage.Color = RiptideData.Sea;
        Salvage.Keybind = new List<string> { RiptideData.SalvageKey };
        Salvage.Keybindstate = "Onpressed";
        Salvage.Priority = 9;
        Salvage.Commandfuncts = new List<Entity_Command_Base> { new RiptideSalvageButton() };
        Salvage.Icon = RiptideData.SalvageIcon;
        RiptideContent.Register("Command", Salvage);
    }

    static readonly Dictionary<char, Color32> Palette = new Dictionary<char, Color32>
    {
        { 'w', new Color32(255, 255, 255, 255) },
        { 'l', new Color32(255, 255, 255, 140) },
        { 's', new Color32(150, 214, 236, 255) },
    };

    public static Sprite Pixel(string[] rows, string name)
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
                px[(h - 1 - y) * w + x] = x < rows[y].Length && Palette.TryGetValue(rows[y][x], out c) ? c : new Color32(0, 0, 0, 0);
            }
        tex.SetPixels32(px);
        tex.Apply(false);
        tex.name = name;
        Sprite s = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 20f, 0, SpriteMeshType.FullRect);
        s.name = name;
        return s;
    }
}

// SALVAGE: lit while this player has offers to pick from; pressing it reopens the dialog.
public class RiptideSalvageButton : Entity_Command_Base
{
    public override void Activate(bool Keybind)
    {
        RiptideSalvage.Reopen();
        if (Select != null)
            Select.Activecommand = null;
    }

    public override void Render(GameObject Button)
    {
        base.Render(Button);
        try
        {
            Getnewlink();
            if (Button == null)
                return;
            bool on = Link.Goalstring.ToString() != "";
            Color t = RiptideData.Sea;
            Image image = Button.transform.GetComponent<Image>();
            if (image != null)
                image.color = on ? new Color(t.r, t.g, t.b, 0.95f) : new Color(t.r * 0.5f, t.g * 0.5f, t.b * 0.5f, 0.6f);
            Transform icon = Button.transform.Find("Icon");
            if (icon != null && icon.GetComponent<Image>() != null)
                icon.GetComponent<Image>().color = on ? Color.white : new Color(1f, 1f, 1f, 0.5f);
        }
        catch (Exception) { }
    }
}
