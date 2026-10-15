using Blockage;
using Blockage.Menu;
using UnityEngine.UI;

// FERTILIZE picks one of your buildings the way the game's own Sell picks a tower: press it, point at one of your
// Yossele buildings (it gets the selection border), click to spend a fertilizer on it. The hotkey fertilizes the
// building under the cursor straight away, or (nothing under it) the nearest of yours that can still grow. The server
// checks every pick (owner, fertilizer, Growth). Mummer's HOOK picker, pointed at your own buildings.
public class YosseleFertilizeSelect : Entity_Command_Itemselect_Base
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
                YosseleLog.Warn("fertilize select: no command prompt to show");
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
            Label("Title", YosseleData.FertilizeSelectTitle);
            Label("Desc", "");
        }
        catch (Exception e)
        {
            YosseleLog.Warn("fertilize select ui: " + e.Message);
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
            Label("Desc", "Point at one of your buildings");
            return;
        }
        YosseleData.Build b = YosseleData.BuildFor(m.Being.Value.ToString());
        int growth = Mathf.RoundToInt(YosseleClient.ClientStat(m, YosseleData.GrowthStat));
        int fertilizer = Mathf.RoundToInt(YosseleClient.ClientStat(Master, YosseleData.FertilizerStat));
        if (b != null && growth >= b.Max)
            Label("Desc", "Fully grown");
        else
            Label("Desc", "Grow it a stage (" + fertilizer + " fertilizer)");
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
            placeholder = new GameObject("Yossele_Fertilize_Border");
            placeholder.SetActive(false);
            UnityEngine.Object.DontDestroyOnLoad(placeholder);
        }
        return placeholder;
    }

    // One of this player's Yossele buildings (clients have the networked being name and owner).
    bool Mine(Entity_Master m)
    {
        if (m == null || Master == null || YosseleData.BuildFor(m.Being.Value.ToString()) == null)
            return false;
        return m.Entitydata.Value.Owner == Master.Entitydata.Value.Owner;
    }
}

// COLOUR on a Pachira Sapling with Grafted Colors: a one-click button like the Mining Hut's Convert. The Sapling keeps
// the colour its fruit pays in the command's Goalstring (synced to every player), and the button draws it: label and
// tint.
public class YosseleColourButton : Entity_Command_Simple
{
    public override void Render(GameObject Button)
    {
        base.Render(Button);
        try
        {
            Getnewlink();
            if (Button == null)
                return;
            string colour = Link.Goalstring.ToString();
            int i = Mathf.Max(0, Array.IndexOf(YosseleData.Colours, colour));
            Color tint = i == 1 ? new Color(0.87f, 0.32f, 0.28f, 0.5f) : i == 2 ? new Color(0.31f, 0.55f, 0.9f, 0.5f) : i == 3 ? new Color(0.94f, 0.78f, 0.24f, 0.5f) : new Color(0.45f, 0.8f, 0.21f, 0.5f);
            Image image = Button.transform.GetComponent<Image>();
            if (image != null)
                image.color = tint;
            Transform icon = Button.transform.Find("Icon");
            if (icon != null && icon.GetComponent<Image>() != null)
                icon.GetComponent<Image>().color = new Color(tint.r, tint.g, tint.b, 1f);
            Transform title = Button.transform.Find("Label");
            if (title != null)
                title = title.Find("Title");
            if (title != null)
                Bla_Localization.SetText(title, YosseleData.ColourNames[i]);
        }
        catch (Exception) { }
    }
}
