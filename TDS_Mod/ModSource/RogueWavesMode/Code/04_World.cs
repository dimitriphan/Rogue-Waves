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
    public const float Scale = 0.6f;          // chart units to world units: rows 18 apart, columns about 20
    static GameObject holder;
    static readonly Dictionary<uint, RWChart.Chart> charts = new Dictionary<uint, RWChart.Chart>();
    static readonly Dictionary<string, Material> paints = new Dictionary<string, Material>();
    static bool sourceLogged;

    // docs/STYLE.md palette.
    static readonly Color32 DeepSwell = new Color32(0x2E, 0x4A, 0x62, 255), RogueTeal = new Color32(0x3F, 0x8F, 0x96, 255),
        Foam = new Color32(0xE9, 0xF1, 0xEE, 255), StormCoral = new Color32(0xE2, 0x72, 0x5B, 255), WetSand = new Color32(0xD8, 0xC4, 0x9A, 255),
        Sailed = new Color32(0xA8, 0x9A, 0x78, 255), DeepSwellDark = new Color32(0x22, 0x38, 0x4B, 255);

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
        world.Id = RWUtil.Clip(name.ToLowerInvariant());
        world.Worldobj = Build(name, chart, route, here);
        world.Defaultnode = here.Name;
        world.Defaultlighting = stock != null ? stock.Defaultlighting : null;
        world.Display_Name = "Sea Chart";
        world.Display_Desc = route.Count < RWChart.Rows ? "Map " + (route.Count + 1) + " of " + RWChart.Rows + (retries >= 0 ? RWCard.Separator + RWCard.Retries(retries) : "") : "Charted";
        world.Display_Sound = stock != null ? stock.Display_Sound : "";
        world.Display_Soundtrack = stock != null ? stock.Display_Soundtrack : "";

        // Older charts go, except a name the boat still uses (a client can see the new World before the new Worlded).
        Dictionary<string, UnityEngine.Object> worlds = RWUtil.Folder("World");
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
        Dictionary<string, UnityEngine.Object> worlds = RWUtil.Folder("World");
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

        float top = RWChart.Rows * 30 * Scale;
        Block(root.transform, "Sea", new Vector3(0f, -0.6f, top / 2f), new Vector3(130f * Scale + 40f, 0.4f, top + 50f), DeepSwell);

        // The route sailed so far, as a list of nodes from the harbour.
        List<RWChart.Node> sailed = new List<RWChart.Node> { chart.At(0, 0) };
        for (int r = 0; r < route.Count; r++) sailed.Add(chart.At(r + 1, route[r]));

        Dictionary<RWChart.Node, Macro_Link_Add_Base> made = new Dictionary<RWChart.Node, Macro_Link_Add_Base>();
        foreach (List<RWChart.Node> row in chart.Rows)
            foreach (RWChart.Node n in row)
                made[n] = Island(root.transform, n, sailed.Contains(n), chart);
        foreach (List<RWChart.Node> row in chart.Rows)
            foreach (RWChart.Node n in row)
                foreach (int k in n.Links)
                {
                    RWChart.Node to = chart.At(n.Row + 1, k);
                    if (to == null) continue;
                    bool onRoute = sailed.Contains(n) && sailed.Contains(to);
                    Line(root.transform, At(n), At(to), onRoute ? RogueTeal : Foam, onRoute ? 0.35f : 0.18f);
                }
        // Only where the party stands has links: the stock arrows offer exactly the next choices.
        foreach (int k in here.Links)
        {
            RWChart.Node to = chart.At(here.Row + 1, k);
            if (to != null) made[here].Links.Add(made[to]);
        }
        return root;
    }

    public static Vector3 At(RWChart.Node n) { return new Vector3(n.X * Scale, 0f, n.Z * Scale); }

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
        cam.transform.localPosition = new Vector3(0f, 22f, -16f);
        cam.transform.localRotation = Quaternion.LookRotation(cam.transform.localPosition - new Vector3(0f, 0f, 7f));
        link.Cameraposition = cam;
        GameObject dock = new GameObject("Dock");
        dock.transform.SetParent(go.transform, false);
        dock.transform.localPosition = new Vector3(0f, 0f, -2.6f);

        GameObject vis = new GameObject("Vis");
        vis.transform.SetParent(go.transform, false);
        Color32 land = n.Row == 0 ? RogueTeal : sailed ? Sailed : n.Breaker ? StormCoral : WetSand;
        // Each map's own island from the base game's world map, when one of its worlds holds it; a plain disc otherwise.
        if (!StockIsland(vis.transform, n.Row == 0 ? StagingPlan(chart) : n.Plan))
            Shape(vis.transform, PrimitiveType.Cylinder, "Island", new Vector3(0f, 0f, 0f), new Vector3(3.2f, 0.35f, 3.2f), land);
        Shape(vis.transform, PrimitiveType.Cylinder, "Shoal", new Vector3(0f, -0.25f, 0f), new Vector3(4.6f, 0.15f, 4.6f), n.Row == 0 ? RogueTeal : n.Breaker ? StormCoral : DeepSwellDark);
        if (sailed && n.Row > 0)
            Flag(vis.transform, RogueTeal);
        if (n.Weather != RWChart.Calm)
        {
            Weather(vis.transform, n.Weather);
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
    public static int IslandsCopied;

    // The base game's worlds hold a node per map, drawn by hand. The chart shows that node's own art at a chart's size: a copy
    // with everything but its look stripped, so it plays no part in the boat's logic. Every machine builds the same.
    static bool StockIsland(Transform parent, string plan)
    {
        if (stockIslands == null)
        {
            stockIslands = new Dictionary<string, GameObject>();
            Dictionary<string, UnityEngine.Object> worlds = RWUtil.Folder("World");
            if (worlds != null)
                foreach (UnityEngine.Object o in worlds.Values)
                {
                    Mapworlddatabaseobject w = o as Mapworlddatabaseobject;
                    if (w == null || w.Worldobj == null || w.Objectname.StartsWith(RWData.WorldPrefix))
                        continue;
                    foreach (Macro_Maplink_Add m in w.Worldobj.GetComponentsInChildren<Macro_Maplink_Add>(true))
                        if (m.Gameplan != null && !stockIslands.ContainsKey(m.Gameplan.Objectname))
                            stockIslands[m.Gameplan.Objectname] = m.gameObject;
                }
            RWLog.Info("chart: " + stockIslands.Count + " stock islands to copy");
        }
        GameObject stock;
        if (!stockIslands.TryGetValue(plan, out stock) || stock == null)
            return false;
        GameObject copy = UnityEngine.Object.Instantiate(stock, parent, false);
        copy.name = "Isle";
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
        Renderer[] rs = copy.GetComponentsInChildren<Renderer>(true);
        if (rs.Length == 0)
        {
            UnityEngine.Object.DestroyImmediate(copy);
            return false;
        }
        // Fitted to the chart's spacing, centred on the node, resting on the water.
        copy.transform.localPosition = Vector3.zero;
        copy.transform.localRotation = stock.transform.localRotation;
        Bounds b = rs[0].bounds;
        foreach (Renderer r in rs) b.Encapsulate(r.bounds);
        float size = Mathf.Max(b.size.x, b.size.z);
        float scale = size > 0.01f ? 8f / size : 1f;
        Vector3 offset = copy.transform.position - new Vector3(b.center.x, b.min.y, b.center.z);
        copy.transform.localScale = copy.transform.localScale * scale;
        copy.transform.localPosition = offset * scale;
        IslandsCopied++;
        return true;
    }

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

    static void Flag(Transform parent, Color32 c)
    {
        Shape(parent, PrimitiveType.Cylinder, "Mast", new Vector3(-1f, 0.9f, 0.6f), new Vector3(0.12f, 0.6f, 0.12f), Foam);
        Shape(parent, PrimitiveType.Cube, "Flag", new Vector3(-0.65f, 1.3f, 0.6f), new Vector3(0.7f, 0.4f, 0.06f), c);
    }

    // The weather marker on its island, readable from the boat camera: Rising Water a teal swell with a crest, Quickening
    // Current three foam chevrons, Nesting Ground a coral egg in a sand nest.
    static void Weather(Transform parent, int weather)
    {
        GameObject m = new GameObject("Weather_" + RWChart.WeatherNames[weather].Replace(" ", ""));
        m.transform.SetParent(parent, false);
        m.transform.localPosition = new Vector3(0.9f, 0.4f, 0.4f);
        Transform t = m.transform;
        switch (weather)
        {
            case RWChart.RisingWater:
                Shape(t, PrimitiveType.Cube, "Swell", new Vector3(0f, 0.35f, 0f), new Vector3(1.1f, 0.7f, 0.5f), RogueTeal);
                Shape(t, PrimitiveType.Cube, "Crest", new Vector3(0f, 0.75f, 0f), new Vector3(1.2f, 0.15f, 0.55f), Foam);
                break;
            case RWChart.QuickeningCurrent:
                for (int i = 0; i < 3; i++)
                {
                    Shape(t, PrimitiveType.Cube, "Chevron" + i + "a", new Vector3(-0.15f, 0.3f + 0.35f * i, 0.12f), new Vector3(0.12f, 0.12f, 0.45f), Foam).transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
                    Shape(t, PrimitiveType.Cube, "Chevron" + i + "b", new Vector3(-0.15f, 0.3f + 0.35f * i, -0.12f), new Vector3(0.12f, 0.12f, 0.45f), Foam).transform.localRotation = Quaternion.Euler(0f, -45f, 0f);
                }
                break;
            case RWChart.NestingGround:
                Shape(t, PrimitiveType.Cylinder, "Nest", new Vector3(0f, 0.15f, 0f), new Vector3(1f, 0.15f, 1f), Sailed);
                Shape(t, PrimitiveType.Sphere, "Egg", new Vector3(0f, 0.55f, 0f), new Vector3(0.5f, 0.65f, 0.5f), StormCoral);
                break;
        }
    }

    static void Line(Transform root, Vector3 a, Vector3 b, Color32 c, float width)
    {
        Vector3 d = b - a;
        Vector3 mid = (a + b) / 2f;
        GameObject g = Shape(root, PrimitiveType.Cube, "Route", new Vector3(mid.x, -0.3f, mid.z), new Vector3(width, 0.06f, Mathf.Max(0.1f, d.magnitude - 3.2f)), c);
        g.transform.localRotation = Quaternion.LookRotation(new Vector3(d.x, 0f, d.z));
    }

    static void Block(Transform parent, string name, Vector3 at, Vector3 size, Color32 c) { Shape(parent, PrimitiveType.Cube, name, at, size, c); }

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
