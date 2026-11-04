// The chart as a world of the game's own (docs/mode/SEA_CHART.md §3 steps 3 and 5; kit docs/15 §17): a
// Mapworlddatabaseobject in Items["World"] whose Worldobj is built here from the seed, so the stock boat, camera, arrows,
// Setup panel and map loading work on it unchanged. The world's name carries everything a client needs:
// "RW_Chart_<seed>_<one column digit per row sailed>r<retries left>". Each machine builds the same world from the name
// when it first sees it (the boat's Tds_Multiplayerinfo.World or Macro_Main.Worlded), so nothing else crosses the
// network. The retries ride on the name for the campaign card (13_Card.cs): a lost map warps the party onto the new name,
// and the world card is the right moment to say so.
// One world per route: the party stands on the last node sailed (its Defaultnode), and only that node has links, to its
// choices in the next row, so the stock arrows offer exactly the vote's choices. The template is built once per name
// under an inactive DontDestroyOnLoad holder; Macro_Worldvisual.Changeworld instantiates it and names the copy after it,
// and its Update re-swaps any machine whose world's GameObject name differs from Worlded, so the two names must match.
public static class RWWorld
{
    public const float Scale = 1f;            // chart units to world units: rows 30 apart, columns about 33, the base game's isles at their own size
    static GameObject holder;
    static readonly Dictionary<uint, RWChart.Chart> charts = new Dictionary<uint, RWChart.Chart>();
    static readonly Dictionary<string, Material> paints = new Dictionary<string, Material>();
    static bool sourceLogged;

    // docs/STYLE.md palette.
    static readonly Color32 DeepSwell = new Color32(0x2E, 0x4A, 0x62, 255), RogueTeal = new Color32(0x3F, 0x8F, 0x96, 255),
        Foam = new Color32(0xE9, 0xF1, 0xEE, 255), StormCoral = new Color32(0xE2, 0x72, 0x5B, 255), WetSand = new Color32(0xD8, 0xC4, 0x9A, 255);

    // ---------------------------------------------------------------- names

    public static string Name(uint seed, List<int> route, int retries)
    {
        string digits = "";
        foreach (int c in route) digits += c.ToString();
        return RWData.WorldPrefix + seed + "_" + digits + (retries >= 0 ? "r" + retries : "");
    }

    public static bool Parse(string name, out uint seed, out List<int> route)
    {
        int retries;
        return Parse(name, out seed, out route, out retries);
    }

    // retries is -1 for a name that doesn't carry them (the save's seed and route, RWCampaign.Saved).
    public static bool Parse(string name, out uint seed, out List<int> route, out int retries)
    {
        seed = 0;
        route = new List<int>();
        retries = -1;
        if (string.IsNullOrEmpty(name) || !name.StartsWith(RWData.WorldPrefix))
            return false;
        string rest = name.Substring(RWData.WorldPrefix.Length);
        int cut = rest.IndexOf('_');
        if (cut <= 0 || !uint.TryParse(rest.Substring(0, cut), out seed))
            return false;
        string tail = rest.Substring(cut + 1);
        int mark = tail.IndexOf('r');
        if (mark >= 0)
        {
            if (!int.TryParse(tail.Substring(mark + 1), out retries)) return false;
            tail = tail.Substring(0, mark);
        }
        foreach (char ch in tail)
        {
            if (ch < '0' || ch > '9') return false;
            route.Add(ch - '0');
        }
        return route.Count <= RWChart.Rows;
    }

    public static RWChart.Chart Chart(uint seed)
    {
        RWChart.Chart chart;
        if (!charts.TryGetValue(seed, out chart))
        {
            chart = RWChart.Generate(seed, RWMode.Pool);
            charts[seed] = chart;
            foreach (string line in RWChart.Dump(chart))
                RWLog.Info(line);
        }
        return chart;
    }

    // The node the party stands on after sailing 'route' (the harbour before map 1), or null for a route off the chart.
    public static RWChart.Node Here(RWChart.Chart chart, List<int> route)
    {
        RWChart.Node at = chart.At(0, 0);
        for (int r = 0; r < route.Count && at != null; r++)
        {
            if (!at.Links.Contains(route[r])) return null;
            at = chart.At(r + 1, route[r]);
        }
        return at;
    }

    // The nodes the party may sail to next: the next row's links of where it stands.
    public static List<RWChart.Node> Choices(RWChart.Chart chart, List<int> route)
    {
        List<RWChart.Node> next = new List<RWChart.Node>();
        RWChart.Node at = Here(chart, route);
        if (at != null)
            foreach (int c in at.Links)
                if (chart.At(at.Row + 1, c) != null) next.Add(chart.At(at.Row + 1, c));
        return next;
    }

    // ---------------------------------------------------------------- the database item

    // The world of this name, built and registered if needed. Null when the name isn't a chart, the pool or the boat
    // (its material) isn't there yet: callers retry next frame.
    public static Mapworlddatabaseobject Ensure(string name)
    {
        Mapworlddatabaseobject have = RWUtil.Item<Mapworlddatabaseobject>("World", name);
        if (have != null && have.Worldobj != null)
            return have;
        uint seed;
        List<int> route;
        int retries;
        if (!Parse(name, out seed, out route, out retries) || RWMode.Pool.Count == 0 || Source() == null)
            return null;
        RWChart.Chart chart = Chart(seed);
        RWChart.Node here = Here(chart, route);
        if (here == null) { RWLog.Once("route " + name, "world " + name + ": the route leaves the chart"); return null; }
        Mapworlddatabaseobject stock = StockWorld();

        Mapworlddatabaseobject world = ScriptableObject.CreateInstance<Mapworlddatabaseobject>();
        world.name = name;
        world.Objectname = name;
        world.Id = ModKitUtil.Clip(name.ToLowerInvariant());
        world.Worldobj = Build(name, chart, route, here);
        world.Defaultnode = here.Name;
        world.Defaultlighting = stock != null ? stock.Defaultlighting : null;
        world.Display_Name = "Sea Chart";
        world.Display_Desc = route.Count < RWChart.Rows ? "Map " + (route.Count + 1) + " of " + RWChart.Rows + (retries >= 0 ? RWCard.Separator + RWCard.Retries(retries) : "") : "Charted";
        world.Display_Sound = stock != null ? stock.Display_Sound : "";
        world.Display_Soundtrack = stock != null ? stock.Display_Soundtrack : "";

        // Older charts go, except a name the boat still uses (a client can see the new World before the new Worlded).
        Dictionary<string, UnityEngine.Object> worlds = ModKitUtil.Folder("World");
        string inUse = Tds_Multiplayerinfo.Instance != null ? Tds_Multiplayerinfo.Instance.World.Value.ToString() : "";
        string shown = Macro_Main.Instance != null ? Macro_Main.Instance.Worlded.Value.ToString() : "";
        foreach (string old in new List<string>(worlds.Keys))
            if (old.StartsWith(RWData.WorldPrefix) && old != name && old != inUse && old != shown)
            {
                Mapworlddatabaseobject o = worlds[old] as Mapworlddatabaseobject;
                if (o != null && o.Worldobj != null) UnityEngine.Object.Destroy(o.Worldobj);
                worlds.Remove(old);
            }
        worlds[name] = world;
        RWLog.Info("world " + name + " registered: " + Count(world.Worldobj) + " nodes, stands on " + here.Name + ", lighting " + (world.Defaultlighting != null ? world.Defaultlighting.name : "none"));
        return world;
    }

    // The stock world the boat opens on (lighting, sounds); the first stock world in the folder otherwise.
    public static Mapworlddatabaseobject StockWorld()
    {
        if (Macro_Main.Instance != null && Macro_Main.Instance.Defaultworld != null)
            return Macro_Main.Instance.Defaultworld;
        Dictionary<string, UnityEngine.Object> worlds = ModKitUtil.Folder("World");
        if (worlds != null)
            foreach (UnityEngine.Object o in worlds.Values)
            {
                Mapworlddatabaseobject w = o as Mapworlddatabaseobject;
                if (w != null && !w.Objectname.StartsWith(RWData.WorldPrefix)) return w;
            }
        return null;
    }

    static int Count(GameObject root) { return root != null ? root.GetComponentsInChildren<Macro_Link_Add_Base>(true).Length : 0; }

    // ---------------------------------------------------------------- the prefab

    static GameObject Build(string name, RWChart.Chart chart, List<int> route, RWChart.Node here)
    {
        if (holder == null)
        {
            holder = new GameObject("RogueWaves_Charts");
            holder.SetActive(false);
            UnityEngine.Object.DontDestroyOnLoad(holder);
        }
        GameObject root = new GameObject(name);
        root.transform.SetParent(holder.transform, false);

        Sea(root.transform);
        uint seed;
        List<int> sailedDigits;
        Parse(name, out seed, out sailedDigits);
        Rocks(root.transform, chart, seed);

        // The route sailed so far, as a list of nodes from the harbour.
        List<RWChart.Node> sailed = new List<RWChart.Node> { chart.At(0, 0) };
        for (int r = 0; r < route.Count; r++) sailed.Add(chart.At(r + 1, route[r]));

        Dictionary<RWChart.Node, Macro_Link_Add_Base> made = new Dictionary<RWChart.Node, Macro_Link_Add_Base>();
        foreach (List<RWChart.Node> row in chart.Rows)
            foreach (RWChart.Node n in row)
                made[n] = Island(root.transform, n, sailed.Contains(n), chart);
        List<Vector3> open = new List<Vector3>(), done = new List<Vector3>();
        foreach (List<RWChart.Node> row in chart.Rows)
            foreach (RWChart.Node n in row)
                foreach (int k in n.Links)
                {
                    RWChart.Node to = chart.At(n.Row + 1, k);
                    if (to == null) continue;
                    List<Vector3> legs = sailed.Contains(n) && sailed.Contains(to) ? done : open;
                    legs.Add(At(n));
                    legs.Add(At(to));
                }
        Lanes(root.transform, "Lanes", open, Foam, 0.14f);
        Lanes(root.transform, "Lanes_Sailed", done, RogueTeal, 0.3f);
        Buoys(root.transform, open);
        Buoys(root.transform, done);
        // Only where the party stands has links: the stock arrows offer exactly the next choices.
        foreach (int k in here.Links)
        {
            RWChart.Node to = chart.At(here.Row + 1, k);
            if (to != null) made[here].Links.Add(made[to]);
        }
        return root;
    }

    public static Vector3 At(RWChart.Node n) { return new Vector3(n.X * Scale, 0f, n.Z * Scale); }

    // The game's camera stands on a node's Cameraposition and looks along its forward. A little further out than the base game's
    // worlds (about 20 units from the boat), so all of the next row's choices stand in the frame.
    static readonly Vector3 CameraSpot = new Vector3(-3f, 20f, -22f), CameraLook = new Vector3(0.1f, -0.55f, 0.83f);

    static Macro_Link_Add_Base Island(Transform root, RWChart.Node n, bool sailed, RWChart.Chart chart)
    {
        GameObject go = new GameObject(n.Name);
        go.transform.SetParent(root, false);
        go.transform.localPosition = At(n);
        Macro_Link_Add_Base link;
        if (n.Row == 0)
        {
            // A new campaign's first stop: the harbour plays the staging map (docs/scope/04 [Sea Chart]).
            Macro_Maplink_Add harbour = go.AddComponent<Macro_Maplink_Add>();
            harbour.Gameplan = RWMode.TwinPlan(StagingPlan(chart));
            link = harbour;
        }
        else
        {
            Macro_Maplink_Add map = go.AddComponent<Macro_Maplink_Add>();
            map.Gameplan = RWMode.TwinPlan(n.Plan);
            link = map;
        }
        link.Links = new List<Macro_Link_Add_Base>();
        link.Boat_Radius = 3f;
        link.Boat_Warp = true;
        link.Node_Hidden = false;
        link.Waypointspot = false;
        // The leader's boat code reads Cameraposition with no null check (Macro_Worldvisual.cs:1694): every node has one.
        GameObject cam = new GameObject("Cameraposition");
        cam.transform.SetParent(go.transform, false);
        // The camera stands high and south of the node, looking on over the next rows: the game's camera looks along this
        // transform's -forward (Macro_Worldvisual.cs:1694).
        cam.transform.localPosition = CameraSpot;
        cam.transform.localRotation = Quaternion.LookRotation(CameraLook);
        link.Cameraposition = cam;
        GameObject dock = new GameObject("Dock");
        dock.transform.SetParent(go.transform, false);
        dock.transform.localPosition = new Vector3(0f, 0f, -2.6f);

        GameObject vis = new GameObject("Vis");
        vis.transform.SetParent(go.transform, false);
        // Each map's own island from the base game's world map, turned to this chart's camera, with its own dock.
        Vector3 quay;
        float top;
        if (StockIsland(vis.transform, n.Row == 0 ? StagingPlan(chart) : n.Plan, out quay, out top))
            dock.transform.localPosition = quay;
        else
        {
            Shape(vis.transform, PrimitiveType.Cube, "Island", new Vector3(0f, -0.5f, 0f), new Vector3(5f, 1f, 5f), n.Row == 0 ? RogueTeal : WetSand);
            top = 0f;
        }
        Vector3 pole = quay + new Vector3(1.6f, 0f, 0.6f);
        if (n.Row == 0 || (sailed && n.Row > 0))
            Flag(vis.transform, pole, RogueTeal);
        else if (n.Breaker)
            Flag(vis.transform, pole, StormCoral);
        if (n.Weather != RWChart.Calm)
        {
            Mark(go.transform, RWIcons.SpriteFor(n.Weather), Mathf.Clamp(top, 1f, 9f) + 2.2f);
            Billboard(go.transform, n);
        }
        return link;
    }

    // The weather's icon over the node while the leader hovers it (docs/reports/UX_FEATURES_2026-11-03.md O1): the game's own
    // billboard addon, found by its object's name, which must be the node's (Macro_Main.cs:609-616, kit docs/15 §18.4).
    static void Billboard(Transform node, RWChart.Node n)
    {
        Sprite icon = RWIcons.SpriteFor(n.Weather);
        if (icon == null)
            return;
        GameObject g = new GameObject(n.Name);
        g.transform.SetParent(node, false);
        Macro_Addon_Nodeshowbillboardlink addon = g.AddComponent<Macro_Addon_Nodeshowbillboardlink>();
        addon.Imagetouse = icon;
        addon.Expiry = 4f;
        addon.Offset = new Vector3(0f, 3.2f, 0f);
    }

    static Dictionary<string, GameObject> stockIslands;
    static List<GameObject> islandPool;
    public static int IslandsCopied;
    const float IsleSmallest = 2.5f, IsleBiggest = 60f;     // a smaller node is a marker post, its map drawn elsewhere in the world; a bigger one is a landscape

    // The base game's worlds hold a node per map, drawn by hand. The chart shows that node's own art: a copy with everything but
    // its look stripped, so it plays no part in the boat's logic, turned so the chart's camera meets it as the node's own camera
    // does. A very big one is shrunk to stay within its row. Every machine builds the same.
    const float IsleWidest = 22f, IsleTallest = 26f;

    // The widest drawn node of each plan, across the stock worlds, and all of them as a pool for a plan with none.
    static void FindIslands()
    {
        stockIslands = new Dictionary<string, GameObject>();
        islandPool = new List<GameObject>();
        Dictionary<string, float> widest = new Dictionary<string, float>();
        Dictionary<string, UnityEngine.Object> worlds = ModKitUtil.Folder("World");
        if (worlds == null)
            return;
        foreach (UnityEngine.Object o in worlds.Values)
        {
            Mapworlddatabaseobject w = o as Mapworlddatabaseobject;
            if (w == null || w.Worldobj == null || w.Objectname.StartsWith(RWData.WorldPrefix))
                continue;
            foreach (Macro_Maplink_Add m in w.Worldobj.GetComponentsInChildren<Macro_Maplink_Add>(true))
            {
                float width = ArtWidth(m.gameObject);
                if (width < IsleSmallest || width > IsleBiggest)
                    continue;
                if (!islandPool.Contains(m.gameObject)) islandPool.Add(m.gameObject);
                foreach (Gameplandatabaseobject plan in new[] { m.Gameplan, m.Altgameplan })
                {
                    float had;
                    if (plan != null && (!widest.TryGetValue(plan.Objectname, out had) || width > had))
                    {
                        widest[plan.Objectname] = width;
                        stockIslands[plan.Objectname] = m.gameObject;
                    }
                }
            }
        }
        islandPool.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        RWLog.Info("chart: " + stockIslands.Count + " plans have a stock island to copy, " + islandPool.Count + " islands in all");
    }

    // A prefab's renderers have no bounds yet: its meshes' own, through their transforms.
    static float ArtWidth(GameObject node)
    {
        bool any = false;
        Bounds b = new Bounds();
        foreach (MeshFilter f in node.GetComponentsInChildren<MeshFilter>(true))
        {
            if (f.sharedMesh == null || f.GetComponentInParent<Macro_Addon_Base>() != null)
                continue;
            Bounds m = f.sharedMesh.bounds;
            for (int c = 0; c < 8; c++)
            {
                Vector3 at = f.transform.TransformPoint(m.center + Vector3.Scale(m.extents, new Vector3((c & 1) == 0 ? -1f : 1f, (c & 2) == 0 ? -1f : 1f, (c & 4) == 0 ? -1f : 1f)));
                if (!any) { b = new Bounds(at, Vector3.zero); any = true; }
                else b.Encapsulate(at);
            }
        }
        return any ? Mathf.Max(b.size.x, b.size.z) : 0f;
    }

    static GameObject IslandFor(string plan)
    {
        if (stockIslands == null)
            FindIslands();
        GameObject stock;
        string twin = plan.StartsWith("Alt_") ? plan.Substring(4) : "Alt_" + plan;
        if (stockIslands.TryGetValue(plan, out stock) && stock != null) return stock;
        if (stockIslands.TryGetValue(twin, out stock) && stock != null) return stock;
        // The map is drawn into its world's landscape, not on its node: any island stands for it, the same one on every machine.
        if (islandPool.Count == 0)
            return null;
        int sum = 0;
        foreach (char c in plan) sum += c;
        return islandPool[sum % islandPool.Count];
    }

    static bool StockIsland(Transform parent, string plan, out Vector3 quay, out float top)
    {
        quay = new Vector3(0f, 0f, -2.6f);
        top = 0f;
        GameObject stock = IslandFor(plan);
        if (stock == null)
            return false;
        GameObject copy = UnityEngine.Object.Instantiate(stock, parent, false);
        copy.name = "Isle";
        Strip(copy);
        Renderer[] rs = copy.GetComponentsInChildren<Renderer>(true);
        if (rs.Length == 0)
        {
            UnityEngine.Object.DestroyImmediate(copy);
            return false;
        }
        // The node's own camera looked this way; the chart's looks that way: the isle turns by the difference.
        Link stockLink = new Link(stock);
        float turn = Mathf.DeltaAngle(Yaw(stockLink.Look), Yaw(CameraLook));
        Quaternion spin = Quaternion.Euler(0f, turn, 0f);
        copy.transform.localPosition = Vector3.zero;
        copy.transform.localRotation = spin * stock.transform.rotation;
        copy.transform.localScale = Vector3.one;
        Bounds b = rs[0].bounds;
        foreach (Renderer r in rs) b.Encapsulate(r.bounds);
        float scale = Mathf.Min(1f, Mathf.Min(IsleWidest / Mathf.Max(0.01f, Mathf.Max(b.size.x, b.size.z)), IsleTallest / Mathf.Max(0.01f, b.size.y)));
        copy.transform.localScale = Vector3.one * scale;
        top = (b.max.y - parent.position.y) * scale;
        // The dock on the side the boat comes from, in the isle's new turn.
        bool found = false;
        foreach (Transform d in stock.transform)
        {
            if (d.name != "Dock")
                continue;
            Vector3 at = spin * (d.position - stock.transform.position) * scale;
            at.y = 0f;
            if (!found || at.z < quay.z) quay = at;
            found = true;
        }
        IslandsCopied++;
        RWLog.Info("chart isle " + plan + " from " + stock.name + ": size " + RWUtil.V(b.size) + " centre " + RWUtil.V(b.center - parent.position) + ", scale " + RWUtil.F(scale) + ", turned " + RWUtil.F(turn) + ", dock " + RWUtil.V(quay));
        return true;
    }

    struct Link
    {
        public Vector3 Look;
        public Link(GameObject node)
        {
            Macro_Link_Add_Base l = node.GetComponent<Macro_Link_Add_Base>();
            Look = l != null && l.Cameraposition != null ? l.Cameraposition.transform.forward : CameraLook;
        }
    }

    static float Yaw(Vector3 v) { return Mathf.Atan2(v.x, v.z) * Mathf.Rad2Deg; }

    // The staging map: the pool's first of its lowest danger that this chart doesn't play, the same on every machine. It
    // is only played for its break, and the card must be able to tell it from map 1.
    public static string StagingPlan(RWChart.Chart chart)
    {
        List<string> onChart = new List<string>();
        foreach (List<RWChart.Node> row in chart.Rows)
            foreach (RWChart.Node n in row) onChart.Add(n.Plan);
        RWChart.Plan best = null;
        foreach (RWChart.Plan p in RWMode.Pool)
            if (!onChart.Contains(p.Name) && (best == null || p.Label < best.Label)) best = p;
        return best != null ? best.Name : RWMode.Pool[0].Name;
    }

    static void Flag(Transform parent, Vector3 at, Color32 c)
    {
        Shape(parent, PrimitiveType.Cube, "Mast", at + new Vector3(0f, 1.1f, 0f), new Vector3(0.14f, 2.2f, 0.14f), Foam);
        Shape(parent, PrimitiveType.Cube, "Flag", at + new Vector3(0.45f, 1.85f, 0f), new Vector3(0.8f, 0.5f, 0.06f), c);
    }

    // A pixel icon standing over a node, square to the chart's camera, readable from a row away.
    static void Mark(Transform node, Sprite icon, float height)
    {
        if (icon == null)
            return;
        GameObject g = new GameObject("Mark");
        g.transform.SetParent(node, false);
        g.transform.localPosition = new Vector3(0f, height, 0f);
        g.transform.localRotation = Quaternion.LookRotation(CameraLook);
        g.transform.localScale = Vector3.one * (2.6f / Mathf.Max(0.01f, icon.bounds.size.x));
        g.AddComponent<SpriteRenderer>().sprite = icon;
    }

    // The sea is the home world's own: its two water meshes, moved up so their surface is the chart's y 0, where the boat floats
    // (the base game's boat rides on the top of its water).
    const string HomeWorld = "World_Omeanchor";
    static float WaterLift;

    static void Sea(Transform root)
    {
        int made = 0;
        Mapworlddatabaseobject home = RWUtil.Item<Mapworlddatabaseobject>("World", HomeWorld);
        if (home != null && home.Worldobj != null)
        {
            float lift = 0f;
            List<MeshRenderer> water = new List<MeshRenderer>();
            foreach (MeshRenderer r in home.Worldobj.GetComponentsInChildren<MeshRenderer>(true))
            {
                MeshFilter f = r.GetComponent<MeshFilter>();
                if (f == null || f.sharedMesh == null || r.sharedMaterial == null || r.transform.parent.name == "Hitbox" || !r.sharedMaterial.name.StartsWith("World_Omeanchor_Water"))
                    continue;
                water.Add(r);
                if (r.sharedMaterial.name == "World_Omeanchor_Water")
                    lift = -(r.transform.position.y + f.sharedMesh.bounds.max.y * r.transform.lossyScale.y);
            }
            WaterLift = lift;
            foreach (MeshRenderer r in water)
            {
                GameObject g = new GameObject(r.name);
                g.transform.SetParent(root, false);
                g.transform.position = r.transform.position + new Vector3(0f, lift, 0f);
                g.transform.rotation = r.transform.rotation;
                g.transform.localScale = r.transform.lossyScale;
                g.AddComponent<MeshFilter>().sharedMesh = r.GetComponent<MeshFilter>().sharedMesh;
                g.AddComponent<MeshRenderer>().sharedMaterials = r.sharedMaterials;
                made++;
            }
        }
        if (made == 0)
        {
            RWLog.Once("chart sea", "chart: no " + HomeWorld + " water to copy, a flat sea instead");
            Shape(root, PrimitiveType.Cube, "Sea", new Vector3(0f, -0.3f, 200f), new Vector3(2000f, 0.4f, 2000f), DeepSwell);
        }
    }

    // The home world's little rock islands, off the lanes' way, the same on every machine of a seed.
    const int RockTries = 40;
    const float RockClear = 16f;

    static void Rocks(Transform root, RWChart.Chart chart, uint seed)
    {
        Mapworlddatabaseobject home = RWUtil.Item<Mapworlddatabaseobject>("World", HomeWorld);
        if (home == null || home.Worldobj == null)
            return;
        List<MeshRenderer> art = new List<MeshRenderer>();
        foreach (MeshRenderer r in home.Worldobj.GetComponentsInChildren<MeshRenderer>(true))
            if (r.name == "Worldmap_Omeanchor_Island1" || r.name == "Worldmap_Omeanchor_Island2") art.Add(r);
        if (art.Count == 0)
            return;
        uint s = seed * 2654435761u + 7u;
        Func<float> next = () => { s = s * 1664525u + 1013904223u; return (s >> 8) / 16777216f; };
        float top = RWChart.Rows * 30f * Scale;
        for (int i = 0; i < RockTries; i++)
        {
            Vector3 at = new Vector3(Mathf.Lerp(-120f, 120f, next()), 0f, Mathf.Lerp(-30f, top + 30f, next()));
            float turn = next() * 360f, scale = Mathf.Lerp(0.35f, 0.55f, next());
            MeshRenderer from = art[(int)(next() * art.Count) % art.Count];
            bool clear = true;
            foreach (List<RWChart.Node> row in chart.Rows)
                foreach (RWChart.Node n in row)
                    if (ModKitUtil.Flat(At(n), at) < RockClear) clear = false;
            if (!clear)
                continue;
            MeshFilter f = from.GetComponent<MeshFilter>();
            Quaternion spin = Quaternion.Euler(0f, turn, 0f) * from.transform.rotation;
            GameObject g = new GameObject("Rock");
            g.transform.SetParent(root, false);
            Vector3 middle = spin * Vector3.Scale(f.sharedMesh.bounds.center, from.transform.lossyScale) * scale;
            g.transform.position = new Vector3(at.x - middle.x, WaterLift + from.transform.position.y, at.z - middle.z);
            g.transform.rotation = spin;
            g.transform.localScale = from.transform.lossyScale * scale;
            g.AddComponent<MeshFilter>().sharedMesh = f.sharedMesh;
            g.AddComponent<MeshRenderer>().sharedMaterials = from.sharedMaterials;
        }
    }

    // A stock buoy half way along each link, as the base game marks its own routes.
    static void Buoys(Transform root, List<Vector3> legs)
    {
        Mapworlddatabaseobject home = RWUtil.Item<Mapworlddatabaseobject>("World", HomeWorld);
        Transform from = home != null && home.Worldobj != null ? home.Worldobj.transform.Find("Body/Waypoints/Link_Home/Vis/Worldmap_Node_Buoya") : null;
        if (from == null)
            return;
        for (int k = 0; k + 1 < legs.Count; k += 2)
        {
            GameObject copy = UnityEngine.Object.Instantiate(from.gameObject, root, false);
            copy.name = "Buoy";
            Strip(copy);
            copy.transform.localPosition = (legs[k] + legs[k + 1]) / 2f;
            copy.transform.localRotation = Quaternion.identity;
            copy.transform.localScale = Vector3.one;
        }
    }

    // Everything but its look goes, so a copy of stock art plays no part in the boat's logic.
    static void Strip(GameObject copy)
    {
        List<GameObject> dropped = new List<GameObject>();
        foreach (Macro_Addon_Base a in copy.GetComponentsInChildren<Macro_Addon_Base>(true)) dropped.Add(a.gameObject);
        foreach (Transform t in copy.GetComponentsInChildren<Transform>(true))
            if (t != copy.transform && (t.name == "Dock" || t.name == "Cameraposition" || t.name.StartsWith("Arrow"))) dropped.Add(t.gameObject);
        foreach (GameObject d in dropped) if (d != null && d != copy) UnityEngine.Object.DestroyImmediate(d);
        foreach (Component c in copy.GetComponentsInChildren<Component>(true))
        {
            if (c == null || c is Transform || c is MeshFilter || c is MeshRenderer || c is SpriteRenderer)
                continue;
            UnityEngine.Object.DestroyImmediate(c);
        }
    }

    // The links between the chart's maps, dashes flat on the water, one mesh per colour. 'legs' holds pairs of ends.
    const float LaneDash = 1.5f, LaneGap = 2.2f, LaneClear = 10f, LaneHeight = 0.04f;

    static void Lanes(Transform root, string name, List<Vector3> legs, Color32 c, float width)
    {
        List<Vector3> v = new List<Vector3>(), n = new List<Vector3>();
        List<Vector2> uv = new List<Vector2>();
        List<int> tri = new List<int>();
        for (int k = 0; k + 1 < legs.Count; k += 2)
        {
            Vector3 d = legs[k + 1] - legs[k];
            float length = d.magnitude;
            Vector3 dir = d / Mathf.Max(0.01f, length), side = Vector3.Cross(Vector3.up, dir) * (width / 2f);
            for (float s = LaneClear; s + LaneDash <= length - LaneClear; s += LaneDash + LaneGap)
            {
                Vector3 near = legs[k] + dir * s + Vector3.up * LaneHeight, far = near + dir * LaneDash;
                int i = v.Count;
                v.Add(near - side); v.Add(near + side); v.Add(far + side); v.Add(far - side);
                for (int j = 0; j < 4; j++) { n.Add(Vector3.up); uv.Add(new Vector2(0.5f, 0.5f)); }
                tri.Add(i); tri.Add(i + 3); tri.Add(i + 2);
                tri.Add(i); tri.Add(i + 2); tri.Add(i + 1);
            }
        }
        if (v.Count == 0)
            return;
        Mesh mesh = new Mesh();
        mesh.name = "RogueWaves_" + name;
        mesh.SetVertices(v);
        mesh.SetNormals(n);
        mesh.SetUVs(0, uv);
        mesh.SetTriangles(tri, 0);
        GameObject g = new GameObject(name);
        g.transform.SetParent(root, false);
        g.AddComponent<MeshFilter>().sharedMesh = mesh;
        g.AddComponent<MeshRenderer>().sharedMaterial = Paint(c);
    }

    static GameObject Shape(Transform parent, PrimitiveType type, string name, Vector3 at, Vector3 size, Color32 c)
    {
        GameObject g = GameObject.CreatePrimitive(type);
        g.name = name;
        Collider col = g.GetComponent<Collider>();
        if (col != null) UnityEngine.Object.DestroyImmediate(col);   // the boat's mouse ray must pass through the art
        g.transform.SetParent(parent, false);
        g.transform.localPosition = at;
        g.transform.localScale = size;
        Renderer r = g.GetComponent<Renderer>();
        if (r != null) r.sharedMaterial = Paint(c);
        return g;
    }

    // ---------------------------------------------------------------- materials

    // A copy of the boat's own material per colour, its texture swapped for one flat colour, so the chart is lit like
    // the boat whatever shader the game uses.
    static Material Paint(Color32 c)
    {
        string key = c.r + "," + c.g + "," + c.b;
        Material m;
        if (paints.TryGetValue(key, out m) && m != null)
            return m;
        Material src = Source();
        m = src != null ? new Material(src) : new Material(Shader.Find("Sprites/Default"));
        m.name = "RogueWaves_Chart_" + key;
        Texture2D tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        Color32[] px = new Color32[16];
        for (int i = 0; i < px.Length; i++) px[i] = c;
        tex.SetPixels32(px);
        tex.Apply(false);
        foreach (string p in new[] { "_MainTex", "_BaseMap" })
            if (m.HasProperty(p)) m.SetTexture(p, tex);
        foreach (string p in new[] { "_Color", "_BaseColor" })
            if (m.HasProperty(p)) m.SetColor(p, Color.white);
        if (m.HasProperty("_Color") == false && m.HasProperty("_BaseColor") == false && m.HasProperty("_MainTex") == false && m.HasProperty("_BaseMap") == false)
            m.color = c;
        paints[key] = m;
        return m;
    }

    static Material sourceMat;

    static Material Source()
    {
        if (sourceMat != null)
            return sourceMat;
        Macro_Worldvisual vis = Macro_Worldvisual.Instance;
        if (vis == null || vis.Boat == null)
            return null;
        foreach (MeshRenderer r in vis.Boat.GetComponentsInChildren<MeshRenderer>(true))
            if (r.sharedMaterial != null)
            {
                sourceMat = r.sharedMaterial;
                break;
            }
        if (!sourceLogged)
        {
            sourceLogged = true;
            RWLog.Info("chart material from the boat: " + (sourceMat != null ? sourceMat.name + " (" + (sourceMat.shader != null ? sourceMat.shader.name : "-") + ")" : "none, using Sprites/Default"));
        }
        if (sourceMat == null)
            sourceMat = new Material(Shader.Find("Sprites/Default"));
        return sourceMat;
    }
}
