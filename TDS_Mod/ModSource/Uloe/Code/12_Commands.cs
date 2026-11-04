using Blockage;
using Blockage.Menu;
using UnityEngine.UI;

// A picker like the game's own Sell and Reroute: press it, point at something (it gets the selection border, and the reach
// shows as the game's range sphere), click. The server checks every pick. Sell's border and prompt ship with every vessel;
// Fie's Reroute ones only load when Fie is in the match.
public abstract class UloePicker : Entity_Command_Itemselect_Base
{
    protected GameObject ui, reach;
    protected bool keyed;

    public override void Visobjs()
    {
        Keybindable = true;
        Obj1 = Load("Vfx", "Sellborder", "Rerouteborder") ?? Placeholder();
        Obj2 = Load("Vfx", "Sellfromborder", "Rerouteborder") ?? Placeholder();
        Close();
        try
        {
            if (Master != null)
                reach = ModKitTowers.RangePreview(ModKitUtil.Pos(Master), ReachNow());
            Transform holder = Bl_Menus.Menus["Main"].Items["Command"].transform;
            Bl_Obj.Destroychildren(holder);
            GameObject prefab = Load("Ui", "Ui_Command_Sell", "Ui_Command_Tower_Fiesilo_Reroute");
            if (prefab == null)
            {
                UloeLog.Warn("picker: no command prompt to show");
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
            Label("Title", Title());
            Label("Desc", "");
        }
        catch (Exception e)
        {
            UloeLog.Warn("picker ui: " + e.Message);
        }
    }

    protected abstract string Title();
    protected abstract float ReachNow();
    protected abstract bool Valid(Entity_Master m);
    protected abstract string Hint();
    protected virtual string Pointing(Entity_Master m) { return ""; }

    public override void Running()
    {
        Entity_Master m = Game_Client_Mouseobject.Master;
        bool valid = Valid(m);
        if (Clone1 != null)
            Clone1.SetActive(valid);
        if (ui == null)
            return;
        if (!valid)
        {
            Label("Desc", Hint());
            return;
        }
        Label("Desc", ModKitUtil.Flat(ModKitUtil.Pos(m), ModKitUtil.Pos(Master)) > ReachNow() + 0.5f ? "Too far away" : Pointing(m));
    }

    public override void Send()
    {
        Entity_Master m = Game_Client_Mouseobject.Master;
        string json;
        if (Valid(m))
            json = "{\"Ref\":" + m.NetworkObjectId + "}";
        else if (keyed)
            json = "";
        else
            return;
        Tell(json);
    }

    protected void Tell(string json)
    {
        Entity_Master_Remote remote = Master != null ? Master.GetComponent<Entity_Master_Remote>() : null;
        if (remote != null)
            remote.Command_ServerRpc(Link.Name.ToString(), json);
    }

    public override void Finish() { Close(); }

    protected void Close()
    {
        if (ui != null)
            UnityEngine.Object.Destroy(ui);
        ui = null;
        if (reach != null)
            UnityEngine.Object.Destroy(reach);
        reach = null;
    }

    protected void Label(string child, string text)
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
            placeholder = new GameObject("Uloe_Picker_Border");
            placeholder.SetActive(false);
            UnityEngine.Object.DontDestroyOnLoad(placeholder);
        }
        return placeholder;
    }

    // One of this player's Uloe buildings, from what clients can see (the synced owner and being name).
    protected bool MyBuilding(Entity_Master m, string only)
    {
        if (m == null || Master == null || m == Master || m.Entitydata.Value.Owner != Master.Entitydata.Value.Owner)
            return false;
        string being = m.Being.Value.ToString();
        return only != null ? being == only : UloeData.BuildFor(being) != null;
    }

    // The icon greys out on "Off" (Argo's SONAR).
    static readonly Dictionary<Image, Color> normal = new Dictionary<Image, Color>();

    protected void Grey(GameObject button, bool grey)
    {
        Transform icon = button != null ? button.transform.Find("Icon") : null;
        Image image = icon != null ? icon.GetComponent<Image>() : null;
        if (image == null)
            return;
        Color look;
        if (!normal.TryGetValue(image, out look))
        {
            if (normal.Count > 32) normal.Clear();
            normal[image] = look = image.color;
        }
        image.color = grey ? new Color(0.5f, 0.5f, 0.5f, 0.3f) : look;
    }
}

// PLUG IN: a node or one of your Power Banks. The hotkey plugs into what is under the cursor, or the nearest in reach. While
// plugged in the button reads UNPLUG, and pressing it pulls the plug without opening the picker.
public class UloePlugSelect : UloePicker
{
    public override void Activate(bool Keybind)
    {
        keyed = Keybind;
        Getnewlink();
        if (Link.Goalstring.ToString() == "On")
        {
            Tell("{\"Unplug\":1}");
            return;
        }
        base.Activate(Keybind);
    }

    protected override string Title() { return UloeData.PlugSelectTitle; }
    protected override string Hint() { return "Point at a node or one of your Power Banks"; }
    protected override string Pointing(Entity_Master m) { return UloeData.PlugName; }

    protected override float ReachNow()
    {
        return UloeData.PlugRange + UloeData.ExtensionReach[ModKitDirections.Rank(Master, UloeData.Part, UloeData.Extension)];
    }

    protected override bool Valid(Entity_Master m) { return UloeUtil.IsNodeClient(m) || MyBuilding(m, UloeData.Bank); }

    public override void Render(GameObject Button)
    {
        base.Render(Button);
        try
        {
            Getnewlink();
            string state = Link.Goalstring.ToString();
            Grey(Button, state == "Off");
            Transform title = Button != null ? Button.transform.Find("Label") : null;
            if (title != null)
                title = title.Find("Title");
            if (title != null)
                Bla_Localization.SetText(title, state == "On" ? UloeData.PlugOffName : UloeData.PlugName);
        }
        catch (Exception) { }
    }
}

// AIM on a Flood Light: one of your buildings within its reach, not itself.
public class UloeAimSelect : UloePicker
{
    public override void Activate(bool Keybind)
    {
        keyed = false;      // a building's button always picks
        base.Activate(Keybind);
    }

    protected override string Title() { return UloeData.AimSelectTitle; }
    protected override string Hint() { return "Point at one of your buildings"; }
    protected override string Pointing(Entity_Master m) { return UloeData.AimName; }

    // Clients read the upgrade from the synced list.
    protected override float ReachNow()
    {
        if (Master != null && Master.Upgradelist != null)
            foreach (FixedString64Bytes u in Master.Upgradelist)
                if (u.ToString() == UloeData.Flood + "_Long2_Upgradeobject")
                    return 8f;
        return 6f;
    }

    protected override bool Valid(Entity_Master m) { return MyBuilding(m, null); }
}
