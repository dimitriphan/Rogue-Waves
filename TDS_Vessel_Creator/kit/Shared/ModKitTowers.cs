// Kit 1.2 (2026-10-03). Versions and compatibility rules: TDS_Vessel_Creator\kit\README.md
// Shared vessel-mod kit: a tower's hit prefab, which decides where the game draws its range.
//
// The game draws a tower's range as a sphere ("Rangecontainer", radius = the Range stat) centred on a child named
// "Detect" of the tower's hit prefab (Entityobject.Defaulthit -> Hitproperties.Default.Entityprefab), both when the
// tower is selected (Entity_Vis_Tower_Range.Selected) and while it is being placed (Entity_Command_Build), and the stock
// tower AIs measure their range from that same point (Helper_Entityai.Checkenemyranged, a 3D distance). A mod tower
// cloned from a shipped one keeps the template's hit prefab, so its range sat where the template's did: Smacky's Bomb
// Tower puts Detect 2 tiles under the tower (it lobs), his Archer Tower 0.75 up (Mummer 1.0, Argo 1.0 and Sprigg <= 1.4
// all showed ranges centred below or above their towers). Shipped values: Sno Sno's Heater and Box 0 (area towers),
// Sno Cannon +0.25, Archer +0.75, Buster's Totem +1.15 (muzzles), Bomb Tower -2.
//
// ModKitTowers.Fit gives a mod tower its own copy of the hit prefab: Detect at (0, eye, 0) - centred on the tower, at
// the height its brain measures range from - Shoot at (0, shoot, 0), and the main box collider fitted to the tower's
// model (what clicks and enemy shots hit). It also adds the game's range display (Entity_Vis_Tower_Range) when asked.
// Every player builds the same objects from the same data, so it is safe online.
public static class ModKitTowers
{
    static GameObject holder;
    static readonly Dictionary<string, UnityEngine.Object> carriers = new Dictionary<string, UnityEngine.Object>();
    public static int Fitted;

    // eye: height of the range centre above the tower's position (its Entityposition, half a tile above the ground).
    // Use 0 for towers whose effect is an area around them, the muzzle height for towers that shoot.
    public static void Fit(Entityobject ent, string id, GameObject model, float eye, float shoot, bool showRange)
    {
        if (ent == null || ent.Defaulthit == null)
            return;
        GameObject source = ent.Defaulthit.Default.Entityprefab;
        if (source == null)
            return;
        if (holder == null)
        {
            // Inactive, so the copies never simulate; the game instantiates them as root objects, which are active.
            holder = new GameObject("ModKit_Hitprefabs");
            holder.SetActive(false);
            UnityEngine.Object.DontDestroyOnLoad(holder);
        }
        GameObject hit = UnityEngine.Object.Instantiate(source, holder.transform);
        hit.name = id + "_Collider";
        hit.transform.localPosition = Vector3.zero;
        Place(hit.transform, "Detect", eye);
        Place(hit.transform, "Shoot", shoot);
        FitCollider(hit, model);

        Hitproperties hp = UnityEngine.Object.Instantiate(ent.Defaulthit);
        string name = id + "_Hitproperties";
        hp.name = name;
        hp.Objectname = name;
        hp.Id = (name.Length > 52 ? name.Substring(0, 52) : name).ToLowerInvariant() + "-modkit";
        Hitproperties.Entry e = hp.Default;
        e.Entityprefab = hit;
        hp.Default = e;
        // The template's per-upgrade hit prefabs are keyed by its own upgrades' names, which mod towers never have.
        hp.Upgradeoverrides = new Dictionary<string, Hitproperties.Entry>();
        hp.Upgradeoverride = new List<Hitproperties.Param>();
        ent.Defaulthit = hp;

        if (ent.Visualscripts == null)
            ent.Visualscripts = new List<UnityEngine.Object>();
        bool has = ent.Visualscripts.Exists(s => s != null && s.name == "Entity_Vis_Tower_Range");
        if (showRange && !has)
            ent.Visualscripts.Insert(0, Carrier("Entity_Vis_Tower_Range"));
        if (!showRange && has)
            ent.Visualscripts.RemoveAll(s => s != null && s.name == "Entity_Vis_Tower_Range");
        Fitted++;
    }

    static void Place(Transform root, string child, float height)
    {
        Transform t = Extramethods.FindDeepChild(root, child);
        if (t == null)
        {
            t = new GameObject(child).transform;
            t.SetParent(root, false);
        }
        t.localRotation = Quaternion.identity;
        // Detect/Shoot are looked up under the entity and read in world space; keep them straight above the centre.
        Vector3 p = new Vector3(0f, height, 0f);
        t.localPosition = t.parent == root ? p : t.parent.InverseTransformPoint(root.TransformPoint(p));
    }

    // The template's box collider resized to the model's bounds (in the tower's own space: origin at the tile centre,
    // half a tile above the ground). Hit prefabs with several or rotated colliders (walls) are left as they are.
    static void FitCollider(GameObject hit, GameObject model)
    {
        if (model == null)
            return;
        List<BoxCollider> boxes = new List<BoxCollider>();
        foreach (BoxCollider b in hit.GetComponentsInChildren<BoxCollider>(true))
            if (b.enabled && b.gameObject.activeSelf)
                boxes.Add(b);
        if (boxes.Count != 1 || boxes[0].transform.localRotation != Quaternion.identity || boxes[0].transform.localPosition != Vector3.zero)
            return;
        Bounds bounds;
        if (!ModelBounds(model, out bounds))
            return;
        BoxCollider box = boxes[0];
        box.center = new Vector3(0f, bounds.center.y, 0f);
        box.size = new Vector3(Mathf.Clamp(bounds.size.x, 0.3f, 0.8f), Mathf.Max(0.2f, bounds.size.y), Mathf.Clamp(bounds.size.z, 0.3f, 0.8f));
    }

    // Bounds of a model's meshes in its root's space, from the meshes themselves (works on inactive library objects).
    public static bool ModelBounds(GameObject model, out Bounds bounds)
    {
        bounds = new Bounds();
        bool any = false;
        foreach (MeshFilter mf in model.GetComponentsInChildren<MeshFilter>(true))
        {
            if (mf.sharedMesh == null)
                continue;
            MeshRenderer r = mf.GetComponent<MeshRenderer>();
            if (r != null && !r.enabled)
                continue;   // the disabled whole-model renderer the journal frames by repeats the parts
            Bounds m = mf.sharedMesh.bounds;
            for (int i = 0; i < 8; i++)
            {
                Vector3 c = m.center + Vector3.Scale(m.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                Vector3 p = model.transform.InverseTransformPoint(mf.transform.TransformPoint(c));
                if (!any) { bounds = new Bounds(p, Vector3.zero); any = true; }
                else bounds.Encapsulate(p);
            }
        }
        return any;
    }

    // The engine attaches visual scripts by the list entry's name only; a named carrier stands for the game's class.
    static UnityEngine.Object Carrier(string className)
    {
        UnityEngine.Object c;
        if (carriers.TryGetValue(className, out c) && c != null)
            return c;
        c = new TextAsset("");
        c.name = className;
        c.hideFlags = HideFlags.DontUnloadUnusedAsset;
        carriers[className] = c;
        return c;
    }

    // The game's own range look (the sphere a selected tower shows), for previews the game doesn't draw itself: a
    // vessel ability's reach while the player picks a target. Destroy it when done.
    public static GameObject RangePreview(Vector3 centre, float radius)
    {
        GameObject prefab = null;
        try { prefab = Bl_Address.Getload("Ui", "Rangecontainer") as GameObject; } catch (Exception) { }
        if (prefab == null)
            return null;
        GameObject r = UnityEngine.Object.Instantiate(prefab);
        r.name = "ModKit_Rangepreview";
        r.transform.position = centre;
        r.transform.localScale = Vector3.one * radius * 2f;
        r.SetActive(true);
        return r;
    }
}
