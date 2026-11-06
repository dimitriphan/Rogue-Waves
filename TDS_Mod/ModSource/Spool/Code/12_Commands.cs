using Blockage;
using Blockage.Menu;
using UnityEngine.UI;

// PATCH picks one of your units or buildings the way the game's own Sell and Reroute commands pick a tower: press it,
// point at it (it gets the selection border), click to mend. The hotkey mends what is under the cursor straight away,
// or the most hurt of yours in reach if the cursor is not on one. The server checks every pick (range, cooldown,
// Thread).
public class SpoolPatchSelect : Entity_Command_Itemselect_Base
{
    GameObject ui;
    bool keyed;

    public override void Activate(bool Keybind)
    {
        keyed = Keybind;
        base.Activate(Keybind);
    }

    public override void Visobjs()
    {
        Keybindable = true;
        // Sell's border and prompt ship with every vessel; Fie's Reroute ones only load when Fie is in the match.
        Obj1 = Load("Vfx", "Sellborder", "Rerouteborder") ?? Placeholder();
        Obj2 = Load("Vfx", "Sellfromborder", "Rerouteborder") ?? Placeholder();
        Close();
        try
        {
            Transform holder = Bl_Menus.Menus["Main"].Items["Command"].transform;
            Bl_Obj.Destroychildren(holder);
            GameObject prefab = Load("Ui", "Ui_Command_Sell", "Ui_Command_Tower_Fiesilo_Reroute");
            if (prefab == null)
            {
                SpoolLog.Warn("patch select: no command prompt to show");
                return;
            }
            ui = UnityEngine.Object.Instantiate(prefab);
            ui.transform.SetParent(holder);
            Bl_Obj.Uifull(ui.transform);
            Blu4_Scaler scaler = ui.GetComponent<Blu4_Scaler>();
            if (scaler != null)
                scaler.Setposition(0.5f, 0.5f, false);
            Transform list = ui.transform.Find("List");
            if (list != null)
                list.gameObject.SetActive(false);
            Transform info = ui.transform.Find("Percent");
            if (info != null)
                info.gameObject.SetActive(true);
            Label("Title", SpoolData.PatchSelectTitle);
            Label("Desc", "");
        }
        catch (Exception e)
        {
            SpoolLog.Warn("patch select ui: " + e.Message);
        }
    }

    public override void Running()
    {
        Entity_Master m = Game_Client_Mouseobject.Master;
        bool valid = Mine(m);
        if (Clone1 != null)
            Clone1.SetActive(valid);
        if (ui == null)
            return;
        if (!valid)
        {
            Label("Desc", "Point at one of your units or buildings");
            return;
        }
        float distance = ModKitUtil.Flat(ModKitUtil.Pos(m), ModKitUtil.Pos(Master));
        // The price comes from the server in the command's Goalvalue (Quick Patch lowers it); 0 before it first syncs.
        Getnewlink();
        int cost = Link.Goalvalue > 0f ? Mathf.RoundToInt(Link.Goalvalue) : SpoolData.PatchCost;
        Label("Desc", distance > SpoolData.PatchRange + 0.6f ? "Too far away" : cost + " Thread: mend " + Mathf.RoundToInt(SpoolData.PatchHeal * 100f) + "%");
    }

    public override void Send()
    {
        Entity_Master m = Game_Client_Mouseobject.Master;
        string json;
        if (Mine(m))
            json = "{\"Ref\":" + m.NetworkObjectId + "}";
        else if (keyed)
            json = "";
        else
            return;
        Entity_Master_Remote remote = Master != null ? Master.GetComponent<Entity_Master_Remote>() : null;
        if (remote != null)
            remote.Command_ServerRpc(Link.Name.ToString(), json);
    }

    public override void Finish() { Close(); }

    // The icon greys out while PATCH threads its needle (the server sets the command's Goalstring to "Off"), like
    // Argo's SONAR.
    static readonly Dictionary<Image, Color> normal = new Dictionary<Image, Color>();

    public override void Render(GameObject Button)
    {
        base.Render(Button);
        Getnewlink();
        Transform icon = Button != null ? Button.transform.Find("Icon") : null;
        Image image = icon != null ? icon.GetComponent<Image>() : null;
        if (image == null)
            return;
        Color look;
        if (!normal.TryGetValue(image, out look))
        {
            if (normal.Count > 32) normal.Clear();
            normal[image] = look = image.color;
        }
        image.color = Link.Goalstring.ToString() == "Off" ? new Color(0.5f, 0.5f, 0.5f, 0.3f) : look;
    }

    void Close()
    {
        if (ui != null)
            UnityEngine.Object.Destroy(ui);
        ui = null;
    }

    void Label(string child, string text)
    {
        Transform t = ui != null ? ui.transform.Find(child) : null;
        if (t == null && child == "Desc" && ui != null)
            t = ui.transform.Find("Percent");
        if (t != null)
            Bla_Localization.SetText(t, text);
    }

    static GameObject Load(string folder, params string[] names)
    {
        foreach (string n in names)
        {
            GameObject g = null;
            try { g = Bl_Address.Getload(folder, n) as GameObject; } catch (Exception) { }
            if (g != null)
                return g;
        }
        return null;
    }

    static GameObject placeholder;

    // The selection loop instantiates both borders every time; never hand it null.
    static GameObject Placeholder()
    {
        if (placeholder == null)
        {
            placeholder = new GameObject("Spool_Patch_Border");
            placeholder.SetActive(false);
            UnityEngine.Object.DontDestroyOnLoad(placeholder);
        }
        return placeholder;
    }

    // One of this player's own things: a Spool building or unit, not the vessel itself (clients see the synced owner
    // and being name only; the server checks health and reach).
    bool Mine(Entity_Master m)
    {
        if (m == null || Master == null || m == Master || m.Entitydata.Value.Owner != Master.Entitydata.Value.Owner)
            return false;
        string being = m.Being.Value.ToString();
        return being != SpoolData.Pin && being != SpoolData.HeadToken && (SpoolData.BuildFor(being) != null || SpoolData.UnitFor(being) != null);
    }
}

// RETURN and MEND on a Thread Head, UNTHREAD on a Cookie Tin, BEE on the Yarn Stash: one-click buttons like the Mining
// Hut's Convert. MEND's Goalstring is "On" while mending (synced to every player), and the button shows it; BEE's is
// "Off" once the Quilting Bee has sewn this Tide, and the button greys out (PATCH's way).
public class SpoolButton : Entity_Command_Simple
{
    public override void Render(GameObject Button)
    {
        base.Render(Button);
        try
        {
            Getnewlink();
            if (Button == null)
                return;
            if (Link.Name.ToString() == SpoolData.BeeLink)
            {
                Transform bee = Button.transform.Find("Icon");
                if (bee != null && bee.GetComponent<Image>() != null)
                    bee.GetComponent<Image>().color = Link.Goalstring.ToString() == "Off" ? new Color(0.5f, 0.5f, 0.5f, 0.3f) : Color.white;
                return;
            }
            if (Link.Name.ToString() != SpoolData.MendLink)
                return;
            bool on = Link.Goalstring.ToString() == "On";
            Transform icon = Button.transform.Find("Icon");
            if (icon != null && icon.GetComponent<Image>() != null)
                icon.GetComponent<Image>().color = on ? Color.white : new Color(0.6f, 0.6f, 0.6f, 0.6f);
            Transform title = Button.transform.Find("Label");
            if (title != null)
                title = title.Find("Title");
            if (title != null)
                Bla_Localization.SetText(title, on ? "MEND ON" : "MEND OFF");
        }
        catch (Exception) { }
    }
}

// COLOUR on a Loom with Dyed in the Wool: a one-click button that cycles the aura its payouts come in (All, Red, Blue,
// Yellow), kept in the command's Goalstring for every player; the button draws it, label and tint (Uloe's Colour Filter
// and Yossele's Grafted way).
public class SpoolColourButton : Entity_Command_Simple
{
    public override void Render(GameObject Button)
    {
        base.Render(Button);
        try
        {
            Getnewlink();
            if (Button == null)
                return;
            int i = Mathf.Max(0, Array.IndexOf(SpoolData.Colours, Link.Goalstring.ToString()));
            Color tint = i == 1 ? new Color(0.87f, 0.32f, 0.28f, 0.5f) : i == 2 ? new Color(0.31f, 0.55f, 0.9f, 0.5f) : i == 3 ? new Color(0.94f, 0.78f, 0.24f, 0.5f) : new Color(0.576f, 0.8f, 0.631f, 0.5f);
            Image image = Button.transform.GetComponent<Image>();
            if (image != null)
                image.color = tint;
            Transform icon = Button.transform.Find("Icon");
            if (icon != null && icon.GetComponent<Image>() != null)
                icon.GetComponent<Image>().color = i == 0 ? Color.white : new Color(tint.r, tint.g, tint.b, 1f);
            Transform title = Button.transform.Find("Label");
            if (title != null)
                title = title.Find("Title");
            if (title != null)
                Bla_Localization.SetText(title, SpoolData.ColourNames[i]);
        }
        catch (Exception) { }
    }
}
