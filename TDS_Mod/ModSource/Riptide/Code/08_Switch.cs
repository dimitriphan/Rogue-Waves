// The RIPTIDE switch on the Setup screen (the panel that opens when a node is picked on the world map). The host gets a
// copy of the game's alternate-map toggle beside it, with Riptide's icon; it sets RiptideDirector.Switch, and the
// director turns the picked Tide map into its Riptide twin. Every player sees the result on the Setup screen's
// "TYPE 3 NORMAL" line, which reads "TYPE 3 RIPTIDE" while the switch is on.
public static class RiptideSwitch
{
    public const string ItemName = "Riptide_Switch";

    public static Blu3_Toggle Toggle;
    static GameObject alt;
    static int iconParts;
    static float nextTry;

    public static void Tick()
    {
        if (Macro_Main.Instance == null || Game_Mapvars.Instance != null || Blockage.Menu.Bl_Menus.Menus == null)
            return;
        Blockage.Menu.Blu2_Menu_Handler setup;
        if (!Blockage.Menu.Bl_Menus.Menus.TryGetValue("Setup", out setup) || setup == null || setup.Items == null)
            return;
        Tds_Multiplayerinfo info = Tds_Multiplayerinfo.Instance;
        if (info == null)
            return;
        string mode = info.Mode.Value.ToString();
        bool riptide = mode == RiptideData.Mode;

        if (info.IsServer)
        {
            if (Toggle == null && Time.unscaledTime >= nextTry)
            {
                nextTry = Time.unscaledTime + 2f;
                Make(setup);
            }
            if (Toggle != null)
            {
                bool show = riptide || System.Array.IndexOf(RiptideData.SwappableModes, mode) >= 0;
                if (Toggle.gameObject.activeSelf != show)
                    Toggle.gameObject.SetActive(show);
                if (Toggle.Toggle != RiptideDirector.Switch)
                {
                    Toggle.Toggle = RiptideDirector.Switch;
                    Toggle.Updatevis();
                }
            }
        }
        TypeLine(setup, riptide);
    }

    // A copy of the alternate-map toggle, registered in the Setup menu under its own name.
    static void Make(Blockage.Menu.Blu2_Menu_Handler setup)
    {
        if (!setup.Items.TryGetValue("Altmap", out alt) || alt == null || alt.GetComponent<Blu3_Toggle>() == null)
            return;
        GameObject copy = UnityEngine.Object.Instantiate(alt, alt.transform.parent, false);
        string cloneName = copy.name;
        GameObject listed;
        if (setup.Items.TryGetValue(cloneName, out listed) && listed == copy)
            setup.Items.Remove(cloneName);      // Blu3_Item.Awake listed it under "Altmap(Clone)"
        copy.name = ItemName;
        copy.transform.SetSiblingIndex(alt.transform.GetSiblingIndex() + 1);
        RectTransform from = alt.transform as RectTransform, to = copy.transform as RectTransform;
        if (from != null && to != null && !Laidout(alt.transform.parent))
            to.anchoredPosition = from.anchoredPosition + new Vector2(-(from.rect.width + 6f), 0f);

        Transform inactive = copy.transform.Find("Inactive");
        if (inactive != null) inactive.gameObject.SetActive(false);
        Transform off = copy.transform.Find("Off");
        Transform burst = off != null ? off.Find("Burst") : null;
        if (burst != null) burst.gameObject.SetActive(false);
        Transform icon = off != null ? off.Find("Altmap") : null;
        UnityEngine.UI.Image iconImage = icon != null ? icon.GetComponent<UnityEngine.UI.Image>() : null;
        Sprite stock = iconImage != null ? iconImage.sprite : null;
        if (icon != null) icon.gameObject.SetActive(true);
        // Each state (On, Off, Inactive) has its own "Altmap" icon, and the lit one may use another sprite.
        iconParts = 0;
        if (RiptideCommands.IconSprite != null)
            foreach (UnityEngine.UI.Image image in copy.GetComponentsInChildren<UnityEngine.UI.Image>(true))
                if (image.gameObject.name == "Altmap" || (stock != null && image.sprite == stock))
                {
                    image.sprite = RiptideCommands.IconSprite;
                    iconParts++;
                }
        foreach (Blockage.Menu.Blu4_Tooltip tip in copy.GetComponentsInChildren<Blockage.Menu.Blu4_Tooltip>(true))
        {
            tip.Text = RiptideData.SwitchTip;
            tip.Localize = false;
        }

        Toggle = copy.GetComponent<Blu3_Toggle>();
        Toggle.Buttonclick = null;
        Toggle.Buttonstate = null;
        Toggle.Togglebyclick = true;
        Toggle.Buttonstate += Flip;
        Toggle.Addmenu();
        Toggle.Toggle = RiptideDirector.Switch;
        Toggle.Updatevis();
        copy.SetActive(true);
        RiptideLog.Info("the RIPTIDE switch is on the Setup screen (" + Layout() + ")");
    }

    static bool Laidout(Transform parent)
    {
        return parent != null && (parent.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>() != null
            || parent.GetComponent<UnityEngine.UI.VerticalLayoutGroup>() != null || parent.GetComponent<UnityEngine.UI.GridLayoutGroup>() != null);
    }

    static void Flip(bool on)
    {
        RiptideDirector.Switch = on;
        RiptideLog.Info("RIPTIDE switch " + (on ? "on" : "off"));
    }

    // "TYPE {difficulty} RIPTIDE" on the Setup screen. The game writes the stock mode's name there whenever a node is
    // picked; this puts Riptide's back, and the stock line when the switch goes off. An empty line (a node not played
    // yet) stays empty, as the game leaves it.
    static string written = "";

    static void TypeLine(Blockage.Menu.Blu2_Menu_Handler setup, bool riptide)
    {
        if (!riptide && written == "")
            return;
        GameObject stats;
        if (!setup.Items.TryGetValue("Stats", out stats) || stats == null)
            return;
        Transform line = stats.transform.Find("Difficulty");
        TMPro.TMP_Text text = line != null ? line.GetComponent<TMPro.TMP_Text>() : null;
        Macro_Maplink_Add node = Macro_Main.Instance.Actualselected;
        if (text == null || string.IsNullOrEmpty(text.text) || node == null || node.Gameplan == null || node.Gameplan.Map == null)
            return;
        if (!riptide)
        {
            if (text.text == written && node.Gameplan.Mode != null)
                text.text = Line(node.Gameplan.Map.Difficulty, Bla_Localization.GetText(node.Gameplan.Mode.Display_Name));
            written = "";
            return;
        }
        string want = Line(node.Gameplan.Map.Difficulty);
        if (text.text != want)
            text.text = want;
        written = want;
    }

    public static string Line(int difficulty) { return Line(difficulty, RiptideData.Displayname); }

    static string Line(int difficulty, string mode)
    {
        string format = "TYPE {0} {1}";
        try
        {
            string local = Bla_Localization.GetText(format);
            if (!string.IsNullOrEmpty(local) && local.Contains("{0}") && local.Contains("{1}"))
                format = local;
        }
        catch (Exception) { }
        try { return string.Format(format, difficulty, mode.ToUpper()); }
        catch (Exception) { return "TYPE " + difficulty + " " + mode.ToUpper(); }
    }

    // For the debug log: where the switch sits next to the game's toggle.
    public static string Layout()
    {
        if (Toggle == null)
            return "no switch";
        RectTransform rt = Toggle.transform as RectTransform;
        Transform parent = Toggle.transform.parent;
        List<string> siblings = new List<string>();
        if (parent != null)
            foreach (Transform c in parent)
                siblings.Add(c.name + (c.gameObject.activeSelf ? "" : " (off)"));
        List<string> parts = new List<string>();
        foreach (Transform c in Toggle.transform)
        {
            List<string> inner = new List<string>();
            foreach (Transform g in c)
                inner.Add(g.name);
            parts.Add(c.name + (inner.Count > 0 ? "[" + string.Join(", ", inner.ToArray()) + "]" : ""));
        }
        return "at " + (rt != null ? rt.anchoredPosition + " size " + rt.rect.size : "?") + " in '" + (parent != null ? parent.name : "-")
            + "' [" + string.Join(", ", siblings.ToArray()) + "], parts [" + string.Join(", ", parts.ToArray()) + "], icon on " + iconParts
            + " parts, tooltip " + (Toggle.GetComponentInChildren<Blockage.Menu.Blu4_Tooltip>(true) != null ? "yes" : "no")
            + "; on the " + Screen.width + "x" + Screen.height + " screen: switch " + Pixels(Toggle.gameObject) + ", alternate-map toggle " + Pixels(alt);
    }

    static string Pixels(GameObject o)
    {
        RectTransform rt = o != null ? o.transform as RectTransform : null;
        if (rt == null)
            return "-";
        Canvas canvas;
        Rect r = RiptideSalvage.OnScreen(rt, out canvas);
        return "x " + RiptideUtil.F(r.xMin) + "-" + RiptideUtil.F(r.xMax) + " y " + RiptideUtil.F(r.yMin) + "-" + RiptideUtil.F(r.yMax) + (o.activeInHierarchy ? "" : " (hidden)");
    }
}
