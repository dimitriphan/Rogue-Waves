using Blockage;
using Blockage.Menu;
using UnityEngine.UI;

// HOOK picks a foe the way the game's own Sell and Reroute commands pick a tower: press it, point at a foe (it gets
// the selection border), click to cast. The hotkey casts at the foe under the cursor straight away, or at the
// nearest foe if the cursor is not on one. The server checks every pick (range, cooldown).
public class MummerHookSelect : Entity_Command_Itemselect_Base
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
                MummerLog.Warn("hook select: no command prompt to show");
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
            Label("Title", MummerData.HookSelectTitle);
            Label("Desc", "");
        }
        catch (Exception e)
        {
            MummerLog.Warn("hook select ui: " + e.Message);
        }
    }

    public override void Running()
    {
        Entity_Master m = Game_Client_Mouseobject.Master;
        bool valid = Foe(m);
        if (Clone1 != null)
            Clone1.SetActive(valid);
        if (ui == null)
            return;
        if (!valid)
        {
            Label("Desc", "Point at a foe");
            return;
        }
        float distance = Vector3.Distance(MummerUtil.Pos(m), MummerUtil.Pos(Master));
        Label("Desc", distance > MummerData.HookRange + 0.6f ? "Too far away" : "Reel it in and Mark it");
    }

    public override void Send()
    {
        Entity_Master m = Game_Client_Mouseobject.Master;
        string json;
        if (Foe(m))
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

    // The icon greys out while HOOK recoils (the server sets the command's Goalstring to "Off"), like Argo's SONAR.
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
            placeholder = new GameObject("Mummer_Hook_Border");
            placeholder.SetActive(false);
            UnityEngine.Object.DontDestroyOnLoad(placeholder);
        }
        return placeholder;
    }

    // A foe of this vessel's team (clients only have the networked tag list).
    bool Foe(Entity_Master m)
    {
        if (m == null || Master == null || m.Entitydata.Value.Team == Master.Entitydata.Value.Team)
            return false;
        foreach (FixedString64Bytes t in m.Taglist)
        {
            string s = t.ToString();
            if (s == "Enemycore" || s == "Core") return false;
        }
        foreach (FixedString64Bytes t in m.Taglist)
            if (t.ToString() == "Enemy")
                return true;
        return false;
    }
}

// COSTUME on a Puppet Stage: a one-click button like the Mining Hut's Convert. The Stage keeps the costume it stitches
// in the command's Goalstring (synced to every player), and the button draws it: label, icon tint and Thread cost.
public class MummerCostumeButton : Entity_Command_Simple
{
    public override void Render(GameObject Button)
    {
        base.Render(Button);
        try
        {
            Getnewlink();
            if (Button == null)
                return;
            string costume = Link.Goalstring.ToString();
            if (string.IsNullOrEmpty(costume))
                costume = "Plain";
            Color tint = costume == "Ballista" ? new Color(0.93f, 0.66f, 0.42f, 0.5f) : costume == "Aegis" ? new Color(0.55f, 0.72f, 0.95f, 0.5f) : new Color(0.79f, 0.68f, 0.92f, 0.5f);
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
                Bla_Localization.SetText(title, costume.ToUpperInvariant() + " " + Mathf.RoundToInt(Link.Goalvalue));
        }
        catch (Exception) { }
    }
}
