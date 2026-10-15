// EXAMPLE, frozen: Argo 1.x code from ModProjects, kept because the kit docs cite it. The live Argo 2.0 is
// TDS_Mod/ModSource/Argo (and Assets/ArgoMod/Editor); copy from there, not from here.
using Blockage;
using Blockage.Menu;

// PORT / STARBOARD / BOSUN pick a tower the way the game's own Reroute and Sell commands pick one: press the button,
// point at any tower on your team (yours or a teammate's; it gets the selection border), click to send that Naut.
// Pointing at the Diving Bell itself calls the Naut home. The server checks every pick.
public class ArgoCrewSelect : Entity_Command_Itemselect_Base
{
    GameObject ui;

    ArgoData.Crew Crew { get { return ArgoData.CrewFor(Link.Name.ToString()); } }

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
                ArgoLog.Warn("crew select: no command prompt to show");
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
            ArgoData.Crew c = Crew;
            Label("Title", string.Format(ArgoData.CrewSelectTitle, c != null ? c.Display : "NAUT"));
            Label("Desc", "");
        }
        catch (Exception e)
        {
            ArgoLog.Warn("crew select ui: " + e.Message);
        }
    }

    public override void Running()
    {
        Entity_Master m = Game_Client_Mouseobject.Master;
        bool home = m != null && m == Master;
        bool valid = home || Tower(m);
        if (Clone1 != null)
            Clone1.SetActive(valid);
        if (ui == null)
            return;
        ArgoData.Crew c = Crew;
        string name = c != null ? c.Name : "the Naut";
        if (home)
            Label("Desc", "Call " + name + " home");
        else if (!valid)
            Label("Desc", "Point at a tower, or the Diving Bell to call " + name + " home");
        else
        {
            string owner = m.Entitydata.Value.Owner == Master.Entitydata.Value.Owner ? "" : "  (a teammate's)";
            Label("Desc", "Tune " + Title(m) + owner);
        }
    }

    public override void Send()
    {
        Entity_Master m = Game_Client_Mouseobject.Master;
        if (m == null || (m != Master && !Tower(m)))
            return;
        Entity_Master_Remote remote = Master != null ? Master.GetComponent<Entity_Master_Remote>() : null;
        if (remote != null)
            remote.Command_ServerRpc(Link.Name.ToString(), "{\"Ref\":" + m.NetworkObjectId + "}");
    }

    public override void Finish()
    {
        Close();
    }

    void Close()
    {
        if (ui != null)
            UnityEngine.Object.Destroy(ui);
        ui = null;
    }

    // "Desc" on Reroute's prompt, "Percent" on Sell's.
    void Label(string child, string text)
    {
        Transform t = ui != null ? ui.transform.Find(child) : null;
        if (t == null && child == "Desc" && ui != null)
            t = ui.transform.Find("Percent");
        if (t != null)
            Bla_Localization.SetText(t, text);
    }

    static string Title(Entity_Master m)
    {
        return m.Client != null && m.Client.Beingobject != null && m.Client.Beingobject.Entity != null ? m.Client.Beingobject.Entity.Display_Name : "this tower";
    }

    // Any tower on the bell's team (the client knows tags from the being's data).
    bool Tower(Entity_Master m)
    {
        if (m == null || Master == null || m == Master || m.Client == null || m.Client.Beingobject == null || m.Client.Beingobject.Entity == null)
            return false;
        List<string> tags = m.Client.Beingobject.Entity.Tags;
        return tags != null && tags.Contains("Tower") && m.Entitydata.Value.Team == Master.Entitydata.Value.Team;
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
            placeholder = new GameObject("Argo_Crew_Border");
            placeholder.SetActive(false);
            UnityEngine.Object.DontDestroyOnLoad(placeholder);
        }
        return placeholder;
    }
}
