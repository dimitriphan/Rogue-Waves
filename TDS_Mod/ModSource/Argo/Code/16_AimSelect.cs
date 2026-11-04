using Blockage;
using Blockage.Menu;

// 2.0 AIM on the Drill Rig: press the button, point at the ground, click to move the drill's Tremor there. A ring the
// size of the Tremor follows the mouse (the drill's synced radius); the prompt says when the spot is out of reach. The
// server checks the pick against the drill's Range (ArgoTowerBrain.Aim). The picker is the game's item select (the crew
// buttons' pattern), reading the surface under the mouse instead of an entity.
public class ArgoAimSelect : Entity_Command_Itemselect_Base
{
    GameObject ui, ring;
    float ringRadius = -1f;

    public override void Visobjs()
    {
        Keybindable = false;
        Obj1 = Load("Vfx", "Moveborder", "Sellborder") ?? Placeholder();
        Obj2 = Load("Vfx", "Movefromborder", "Sellfromborder") ?? Placeholder();
        Close();
        try
        {
            ring = ArgoAssets.Effect("World_Argo_Tremor", Vector3.one * 10000f, 0f);
            ringRadius = -1f;
            Transform holder = Bl_Menus.Menus["Main"].Items["Command"].transform;
            Bl_Obj.Destroychildren(holder);
            GameObject prefab = Load("Ui", "Ui_Command_Sell", "Ui_Command_Tower_Fiesilo_Reroute");
            if (prefab == null)
                return;
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
            Label("Title", ArgoData.AimName);
            Label("Desc", "");
        }
        catch (Exception e)
        {
            ArgoLog.Warn("aim ui: " + e.Message);
        }
    }

    // The drill's reach, synced in the AIM command's Goalstring by the server (Range plus the slack it allows).
    float Reach
    {
        get
        {
            float r = ModKitUtil.F(Link.Goalstring.ToString());
            return r > 0f ? r : 5.5f;
        }
    }

    public override void Running()
    {
        Entity_Master s = Game_Client_Mouseobject.Surfacemaster;
        bool on = s != null && Game_Client_Mouseobject.Surfacehit && Master != null;
        Vector3 at = on ? s.Position.Value + Vector3.up : Vector3.zero;
        bool ok = on && ModKitUtil.Flat(at, ModKitUtil.Pos(Master)) <= Reach;
        if (Clone1 != null)
        {
            Clone1.SetActive(ok);
            if (on) Clone1.transform.position = s.Position.Value;
        }
        if (ring != null)
        {
            ring.SetActive(ok);
            if (ok)
            {
                ring.transform.position = ArgoClient.Ground(at) + Vector3.up * 0.04f;
                float r = Link.Goalvalue > 0f ? Link.Goalvalue : 2f;
                if (!Mathf.Approximately(r, ringRadius))
                {
                    ringRadius = r;
                    ArgoClient.Size(ring, "World_Argo_Tremor", r);
                }
            }
        }
        if (!on)
            Label("Desc", "Point at the ground");
        else if (!ok)
            Label("Desc", "Too far: the Tremor goes within " + Reach.ToString("0.#") + " tiles of the Drill Rig");
        else
            Label("Desc", "Shake the ground here");
    }

    public override void Send()
    {
        Entity_Master s = Game_Client_Mouseobject.Surfacemaster;
        if (s == null || Master == null)
            return;
        Vector3 p = s.Position.Value;
        System.Globalization.CultureInfo inv = System.Globalization.CultureInfo.InvariantCulture;
        Entity_Master_Remote remote = Master.GetComponent<Entity_Master_Remote>();
        if (remote != null)
            remote.Command_ServerRpc(ArgoData.AimLink, "{\"X\":" + p.x.ToString("0.###", inv) + ",\"Y\":" + p.y.ToString("0.###", inv) + ",\"Z\":" + p.z.ToString("0.###", inv) + "}");
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
        if (ring != null)
            UnityEngine.Object.Destroy(ring);
        ring = null;
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

    static GameObject Placeholder()
    {
        if (placeholder == null)
        {
            placeholder = new GameObject("Argo_Aim_Border");
            placeholder.SetActive(false);
            UnityEngine.Object.DontDestroyOnLoad(placeholder);
        }
        return placeholder;
    }
}
