#if ROGUEWAVESMODE_CHARTLOOK
// Debug build only (-Define ROGUEWAVESMODE_SELFTEST,ROGUEWAVESMODE_SHOTS,ROGUEWAVESMODE_CHARTLOOK): the quick round for the sea
// chart's look. Logs how the base game's world is put together beside the chart, with captures of both.
public static partial class RWTest
{
    static string V(Vector3 v) { return RWUtil.V(v); }

    static void Say(string text) { Info("look", text); }

    static void Boxes(string what, GameObject root, int most)
    {
        if (root == null) { Say(what + ": none"); return; }
        Renderer[] rs = root.GetComponentsInChildren<Renderer>(false);
        Bounds b = new Bounds(root.transform.position, Vector3.zero);
        foreach (Renderer r in rs) b.Encapsulate(r.bounds);
        Say(what + " '" + root.name + "' at " + V(root.transform.position) + " scale " + V(root.transform.lossyScale) + ": " + rs.Length + " renderers, centre " + V(b.center) + ", size " + V(b.size));
        List<Renderer> big = new List<Renderer>(rs);
        big.Sort((x, y) => y.bounds.size.sqrMagnitude.CompareTo(x.bounds.size.sqrMagnitude));
        for (int i = 0; i < big.Count && i < most; i++)
        {
            Material m = big[i].sharedMaterial;
            Say("   " + big[i].GetType().Name + " " + Path(big[i].transform, root.transform) + " size " + V(big[i].bounds.size) + " at " + V(big[i].bounds.center) + " mat " + (m != null ? m.name + " / " + (m.shader != null ? m.shader.name : "-") : "-"));
        }
    }

    static string Path(Transform t, Transform stop)
    {
        string p = t.name;
        while (t.parent != null && t != stop && t.parent != stop) { t = t.parent; p = t.name + "/" + p; }
        return p;
    }

    static void Camera_(string what)
    {
        Camera c = Camera.main;
        Game_Cameracontroller g = Game_Cameracontroller.Instance;
        Macro_Main main = Macro_Main.Instance;
        Macro_Worldvisual vis = Macro_Worldvisual.Instance;
        Say(what + " camera: " + (c != null ? V(c.transform.position) + " euler " + V(c.transform.eulerAngles) + " fov " + RWUtil.F(c.fieldOfView) + " near " + RWUtil.F(c.nearClipPlane) + " far " + RWUtil.F(c.farClipPlane) : "none")
            + (g != null ? "; Movepos " + V(g.Movepos) + " Camangle " + V(g.Camangle) + " Camoffset " + V(g.Camoffset) + " Rotation " + RWUtil.F(g.Rotation) + " Zoommulti " + RWUtil.F(g.Zoommulti) : ""));
        Say(what + " fog " + RenderSettings.fog + " " + RenderSettings.fogMode + " start " + RWUtil.F(RenderSettings.fogStartDistance) + " end " + RWUtil.F(RenderSettings.fogEndDistance) + " density " + RenderSettings.fogDensity + " colour " + RenderSettings.fogColor + "; ambient " + RenderSettings.ambientLight + " sky " + (RenderSettings.skybox != null ? RenderSettings.skybox.name : "-"));
        if (main != null && vis != null)
            Say(what + " hovering " + (main.Hovering != null ? main.Hovering.name : "-") + ", prev " + (main.Prevhovering != null ? main.Prevhovering.name : "-") + ", selected '" + main.Selected.Value + "', boat " + V(vis.Boat.transform.position) + " scale " + V(vis.Boat.transform.lossyScale) + " speed " + RWUtil.F(vis.Boatspeed) + ", Boatpos " + V(main.Boatpos.Value) + " Campos " + V(main.Campos.Value) + " Camdir " + V(main.Camdir.Value) + ", insetup " + vis.Insetup + ", transition " + vis.Inworldtransition);
        if (main != null && main.Hovering != null && main.Hovering.Cameraposition != null)
        {
            Transform p = main.Hovering.Cameraposition.transform;
            Say(what + " its Cameraposition local " + V(p.localPosition) + " world " + V(p.position) + " forward " + V(p.forward) + ", node at " + V(main.Hovering.transform.position) + ", Boat_Radius " + RWUtil.F(main.Hovering.Boat_Radius));
        }
    }

    static void Nodes(string what, GameObject world, int most)
    {
        if (world == null) return;
        int n = 0;
        foreach (Macro_Link_Add_Base l in world.GetComponentsInChildren<Macro_Link_Add_Base>(true))
        {
            if (n++ >= most) break;
            Transform dock = l.transform.Find("Dock");
            Bounds b = new Bounds(l.transform.position, Vector3.zero);
            int rc = 0;
            foreach (Renderer r in l.GetComponentsInChildren<Renderer>(true)) { b.Encapsulate(r.bounds); rc++; }
            Say(what + " node " + l.name + " (" + l.GetType().Name + ") at " + V(l.transform.position) + " scale " + V(l.transform.lossyScale) + ", radius " + RWUtil.F(l.Boat_Radius) + " warp " + l.Boat_Warp + " hidden " + l.Node_Hidden + ", cam local " + (l.Cameraposition != null ? V(l.Cameraposition.transform.localPosition) + " forward " + V(l.Cameraposition.transform.forward) : "-") + ", dock " + (dock != null ? V(dock.localPosition) : "-") + ", " + rc + " renderers, size " + V(b.size) + ", links " + (l.Links != null ? l.Links.Count : 0));
            List<string> kids = new List<string>();
            foreach (Transform c in l.transform) kids.Add(c.name);
            Say("      children " + string.Join(", ", kids.ToArray()));
        }
    }

    static void Scene_(string what)
    {
        // Everything drawn outside the world and the boat: the sea, the sky, the lighting's own objects.
        Macro_Worldvisual vis = Macro_Worldvisual.Instance;
        List<string> lines = new List<string>();
        foreach (Renderer r in UnityEngine.Object.FindObjectsOfType<Renderer>())
        {
            if (vis != null && vis.World != null && r.transform.IsChildOf(vis.World.transform)) continue;
            if (vis != null && vis.Boat != null && r.transform.IsChildOf(vis.Boat.transform)) continue;
            if (r.bounds.size.magnitude < 40f) continue;
            Material m = r.sharedMaterial;
            lines.Add(r.GetType().Name + " " + Path(r.transform, null) + " size " + V(r.bounds.size) + " at " + V(r.bounds.center) + " mat " + (m != null ? m.name + "/" + (m.shader != null ? m.shader.name : "-") : "-"));
        }
        Say(what + ": " + lines.Count + " big renderers outside the world and the boat");
        for (int i = 0; i < lines.Count && i < 25; i++) Say("   " + lines[i]);
    }

    // The stock worlds' parts that aren't nodes: the water's meshes and materials, the volume, the first levels of the art.
    static void Parts(string world)
    {
        Mapworlddatabaseobject w = RWUtil.Item<Mapworlddatabaseobject>("World", world);
        if (w == null || w.Worldobj == null) { Say(world + ": none"); return; }
        Say(world + " lighting " + (w.Defaultlighting != null ? w.Defaultlighting.name : "-") + ", default node " + w.Defaultnode);
        foreach (Transform c in w.Worldobj.transform)
        {
            List<string> comps = new List<string>();
            foreach (Component x in c.GetComponents<Component>()) if (x != null) comps.Add(x.GetType().Name);
            Say(world + " child " + c.name + " at " + V(c.position) + ": " + string.Join(",", comps.ToArray()) + "; kids " + c.childCount);
        }
        int n = 0;
        foreach (MeshRenderer r in w.Worldobj.GetComponentsInChildren<MeshRenderer>(true))
        {
            Material m = r.sharedMaterial;
            if (m == null || m.name.IndexOf("Water", StringComparison.OrdinalIgnoreCase) < 0 || n++ > 30)
                continue;
            MeshFilter f = r.GetComponent<MeshFilter>();
            Say("   water " + Path(r.transform, w.Worldobj.transform) + " at " + V(r.transform.position) + " scale " + V(r.transform.lossyScale) + " rot " + V(r.transform.eulerAngles) + " mesh " + (f != null && f.sharedMesh != null ? f.sharedMesh.name + " " + V(f.sharedMesh.bounds.size) + " centre " + V(f.sharedMesh.bounds.center) : "-") + " mat " + m.name);
        }
    }

    static void Art(string world)
    {
        Mapworlddatabaseobject w = RWUtil.Item<Mapworlddatabaseobject>("World", world);
        if (w == null || w.Worldobj == null) return;
        int n = 0;
        foreach (MeshRenderer r in w.Worldobj.GetComponentsInChildren<MeshRenderer>(true))
        {
            MeshFilter f = r.GetComponent<MeshFilter>();
            if (f == null || f.sharedMesh == null || r.GetComponentInParent<Macro_Link_Add_Base>() != null || n++ > 70)
                continue;
            Say("   art " + Path(r.transform, w.Worldobj.transform) + " at " + V(r.transform.position) + " mesh " + V(Vector3.Scale(f.sharedMesh.bounds.size, r.transform.lossyScale)) + " mat " + (r.sharedMaterial != null ? r.sharedMaterial.name : "-"));
        }
    }

    public static IEnumerator Look()
    {
        yield return Until(() => UnityEngine.Object.FindObjectOfType<Mainmenu_Main>() != null, 120f);
        Mainmenu_Main title = UnityEngine.Object.FindObjectOfType<Mainmenu_Main>();
        if (title != null)
        {
            yield return new WaitForSecondsRealtime(8f);
            title.Idlethreshold = 100000f;
            title.SendMessage("Transitiontomain", SendMessageOptions.DontRequireReceiver);
            yield return Until(() => RWMenu.Button != null, 25f);
            yield return new WaitForSecondsRealtime(7f);
            Blockage.Menu.Bl_Menus.Changemenu("Main", true);
            yield return new WaitForSecondsRealtime(10f);
            yield return Snap("look_mainmenu");
            if (RWMenu.Button != null && RWMenu.Button.Buttonenter != null) RWMenu.Button.Buttonenter();
            yield return new WaitForSecondsRealtime(1f);
            yield return Snap("look_mainmenu_hover");
        }
        yield return Embark();
        Parts("World_Omeanchor");
        Art("World_Omeanchor");
        Macro_Main main = Macro_Main.Instance;
        Macro_Worldvisual vis = Macro_Worldvisual.Instance;
        if (main == null || !main.Ready) { Setup("look", "no boat"); yield break; }
        main.Selectcharacter("Hero_Smacky");
        yield return new WaitForSecondsRealtime(8f);
        Camera_("stock");
        Boxes("stock world", vis.World, 12);
        Boxes("the boat", vis.Boat, 4);
        Scene_("stock");
        Nodes("stock", vis.World, 8);
        yield return Snap("look_stock");
        // A neighbour of where the boat stands, as a player's hover would.
        Macro_Link_Add_Base at = main.Hovering, next = at != null && at.Links != null && at.Links.Count > 0 ? at.Links[0] : null;
        if (next != null)
        {
            main.Hovering = next;
            main.Prevhovering = next;
            main.Preview = next;
            try { Macro_Worldui.Instance.Showhover(); } catch (Exception e) { Say("Showhover: " + e.Message); }
            yield return new WaitForSecondsRealtime(4f);
            Camera_("stock sailing");
            yield return Snap("look_stock_sailing");
            yield return new WaitForSecondsRealtime(8f);
            Camera_("stock arrived");
            yield return Snap("look_stock_arrived");
            foreach (Macro_Maplink_Add m in vis.World.GetComponentsInChildren<Macro_Maplink_Add>(true))
                if (m.Gameplan != null && m.gameObject.activeInHierarchy)
                {
                    Tds_Multiplayer_Remote.Instance.Selectmap_ServerRpc(m.name, false, default(Unity.Netcode.ServerRpcParams));
                    yield return new WaitForSecondsRealtime(4f);
                    Camera_("stock setup");
                    yield return Snap("look_stock_setup");
                    main.Deselectcurrentmap();
                    yield return new WaitForSecondsRealtime(2f);
                    break;
                }
        }

        if (!Plan()) yield break;
        RWCampaign.New(Seed);
        RWCampaign.Staging = false;
        yield return Until(() => Here(), 40f);
        yield return new WaitForSecondsRealtime(8f);
        Camera_("chart");
        Boxes("chart world", vis.World, 8);
        Scene_("chart");
        Nodes("chart", vis.World, 3);
        yield return Snap("look_chart");
        RWCampaign.Choice = 0;
        yield return new WaitForSecondsRealtime(3f);
        yield return Snap("look_chart_sailing");
        yield return Until(() => RWBoat.ArrivedAt > 0f, 30f);
        Camera_("chart arrived");
        yield return new WaitForSecondsRealtime(3f);
        yield return Snap("look_chart_setup");
        RWCampaign.Stop();
    }
}
#endif
