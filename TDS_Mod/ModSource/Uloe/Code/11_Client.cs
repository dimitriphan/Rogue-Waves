// Runs on every player (host included). Listens to the Vis RPCs the server sends through Uloe's entities and plays Uloe's
// own sounds and moving effects: the plug cord that follows the vessel, the Flood Light's cord and ray, rings at their real
// radius, the Glass's beam and its lens turning, flashes, bursts, owner-only messages. A cord, ray or ring is told again every
// few seconds (so a late joiner sees it), and a client lets it go if it stops hearing.
public static class UloeClient
{
    static readonly List<Entity_Master_Client> hooked = new List<Entity_Master_Client>();
    static readonly Dictionary<string, Link> links = new Dictionary<string, Link>();
    static readonly List<string> gone = new List<string>();
    static Game_Entity seen;
    static float nextScan;
    static Material lineMat;
    public static int Beams, Linksdrawn;

    public static readonly Color Cord = new Color(0.18f, 0.18f, 0.18f, 1f);
    public static readonly Color Bright = new Color(0.98f, 0.95f, 0.35f, 1f);
    public static readonly Color Soft = new Color(1f, 0.98f, 0.66f, 0.28f);

    enum Kind { Plugcord, Nodecord, Ray, Longray, Ring, Sweep }

    class Link
    {
        public Kind Kind;
        public Entity_Master From;
        public Vector3 To;
        public float Radius, Until, Heard;
        public LineRenderer Line;
        public GameObject Plug;
        public Transform Lamp;      // a tower's visual is remade when its skin changes: looked up again when it goes null
    }

    public static void Update()
    {
        Game_Entity ge = Game_Entity.Instance;
        if (ge != seen)
        {
            seen = ge;
            hooked.Clear();
            ClearLinks();
        }
        if (links.Count > 0)
            DrawLinks();
        if (ge == null || ge.Entityclient == null || Time.unscaledTime < nextScan)
            return;
        nextScan = Time.unscaledTime + 0.25f;
        hooked.RemoveAll(c => c == null);
        foreach (Entity_Master master in ge.Entityclient)
        {
            if (master == null || master.Client == null || hooked.Contains(master.Client))
                continue;
            string being = master.Being.Value.ToString();
            if (being != UloeData.Hero && being != UloeData.Photon && UloeData.BuildFor(being) == null)
                continue;
            Entity_Master m = master;
            master.Client.Vis += (type, position, number) => OnVis(m, type, position, number);
            hooked.Add(master.Client);
        }
    }

    static void OnVis(Entity_Master master, string type, Vector3 position, float number)
    {
        if (type == null)
            return;
        try
        {
            Vector3 at = master != null ? ModKitUtil.Pos(master) : position;
            string being = master != null ? master.Being.Value.ToString() : "";
            // The stock attack AIs announce each swing with "Attack" and each shot with "Shoot".
            if (type == "Attack")
            {
                UloeAssets.Play("Uloe_Zap", at, 0.4f);
                ModKitAssets.Effect("World_Uloe_Spark", at + Vector3.up * 0.3f, 0.8f);
                return;
            }
            if (type == "Shoot")
            {
                UloeAssets.Play("Uloe_Zap", at);
                return;
            }
            if (!type.StartsWith("Uloe_"))
                return;
            int n = Mathf.RoundToInt(number);
            switch (type)
            {
                case "Uloe_Beam":
                    Beams++;
                    Run(BeamRun(master, position, number));
                    UloeAssets.Play("Uloe_Beam", at);
                    break;
                case "Uloe_Plugin":
                    UloeAssets.Play("Uloe_Plugin", at);
                    Animate(master, "Plug");
                    ModKitAssets.Effect("World_Uloe_Spark", position + Vector3.up * 0.2f, 1f);
                    break;
                case "Uloe_Unplug":
                    UloeAssets.Play("Uloe_Unplug", at, n == 1 ? 1.2f : 0.8f);
                    if (n == 1)
                        ModKitAssets.Effect("World_Uloe_Snap", at, 1.2f);
                    break;
                case "Uloe_Plugcord":
                    SetLink(master, Kind.Plugcord, position, number);
                    break;
                case "Uloe_Nodecord":
                    SetLink(master, Kind.Nodecord, position, number);
                    break;
                case "Uloe_Ray":
                    if (n != 2) RemoveLink(master, Kind.Longray);
                    if (n != 1) RemoveLink(master, Kind.Ray);
                    if (n > 0) SetLink(master, n == 2 ? Kind.Longray : Kind.Ray, position, number);
                    break;
                case "Uloe_Ring":
                    SetLink(master, Kind.Ring, position, number);
                    break;
                case "Uloe_Sweep":
                    SetLink(master, Kind.Sweep, position, number);
                    break;
                case "Uloe_Line":
                    // A beam that isn't the Glass's main one: Prism's, Focal Point's line, the Heliostat's.
                    Run(LineRun(at + Vector3.up * UloeData.BeamEye, position + Vector3.up * 0.25f, 0.18f, 0.025f * Mathf.Max(1f, number), Bright));
                    ModKitAssets.Effect("World_Uloe_Spark", position + Vector3.up * 0.3f, 0.6f);
                    break;
                case "Uloe_Zap":
                    UloeAssets.Play("Uloe_Zap", position, 0.5f);
                    ModKitAssets.Effect("World_Uloe_Spark", position + Vector3.up * 0.3f, 0.8f);
                    break;
                case "Uloe_Pop":
                    UloeAssets.Play("Uloe_Pop", at);
                    ModKitAssets.Effect("World_Uloe_Motes", at + Vector3.up * 0.2f, 1.2f);
                    break;
                case "Uloe_Down":
                    UloeAssets.Play("Uloe_Pop", position, 0.7f);
                    ModKitAssets.Effect("World_Uloe_Motes", position + Vector3.up * 0.2f, 1.2f);
                    break;
                case "Uloe_Burst":
                    UloeAssets.Play("Uloe_Burst", position);
                    GameObject ring = ModKitAssets.Effect("World_Uloe_Burst", Ground(position), 2f);
                    if (ring != null) ring.transform.localScale = Vector3.one * Mathf.Max(0.8f, number);
                    break;
                case "Uloe_Flash":
                    UloeAssets.Play("Uloe_Flash", at);
                    ModKitAssets.Effect("World_Uloe_Flash", position + Vector3.up * 0.5f, 1f);
                    Run(LineRun(at + Vector3.up * 0.4f, position + Vector3.up * 0.4f, 0.14f, 0.05f + 0.02f * n, Bright));
                    break;
                case "Uloe_Nullify":
                    UloeAssets.Play("Uloe_Nullify", at);
                    ModKitAssets.Effect("World_Uloe_Flash", at + Vector3.up * 0.6f, 1f);
                    break;
                case "Uloe_Strike":
                    UloeAssets.Play("Uloe_Strike", position, 0.6f);
                    break;
                case "Uloe_Built":
                    ModKitAssets.Effect("World_Uloe_Motes", at + Vector3.up * 0.2f, 1.2f);
                    UloeAssets.Play("Uloe_Build", at, 0.6f);
                    break;
                case "Uloe_Upgrade":
                    UloeAssets.Play("Uloe_Build", at, 0.8f);
                    break;
                case "Uloe_Herodeath":
                    ModKitAssets.Effect("World_Hero_Uloe_Death", at, 2.5f);
                    UloeAssets.Play("Uloe_Death", at);
                    break;
                case "Uloe_Warn":
                    int code = n % 100, value = n / 100;
                    if (ModKitUtil.OwnedLocally(master) && code > 0 && code < UloeData.WarnText.Length)
                        Helper_Uitools.Warning(UloeData.WarnText[code] + (value > 0 ? " (" + value + ")" : ""), "", 2f);
                    break;
            }
        }
        catch (Exception e)
        {
            UloeLog.Warn("client vis " + type + ": " + e.Message);
        }
    }

    // Same call the game's Entity_Vis_Mob_Attack makes for "Attack": Force restarts the animation and takes its priority.
    static void Animate(Entity_Master master, string anim)
    {
        if (master == null)
            return;
        foreach (Entity_Vis_Animator_Base a in master.GetComponentsInChildren<Entity_Vis_Animator_Base>(true))
            a.Animplay(anim, Game_Sprite.Animationtype.Force);
    }

    static void Run(IEnumerator routine)
    {
        if (UloeMod.Instance != null)
            UloeMod.Instance.StartCoroutine(routine);
    }

    static Vector3 Ground(Vector3 entityPosition) { return entityPosition + Vector3.down * 0.48f; }

    // ---------------------------------------------------------------- lines

    static Material LineMaterial()
    {
        if (lineMat != null)
            return lineMat;
        GameObject fx = ModKitAssets.Vfx("World_Uloe_Spark");
        ParticleSystemRenderer r = fx != null ? fx.GetComponent<ParticleSystemRenderer>() : null;
        lineMat = r != null && r.sharedMaterial != null ? new Material(r.sharedMaterial) : new Material(Shader.Find("Sprites/Default"));
        lineMat.name = "Uloe_Line";
        return lineMat;
    }

    static LineRenderer Line(string name, int points, Color color, float width)
    {
        GameObject go = new GameObject(name);
        LineRenderer l = go.AddComponent<LineRenderer>();
        l.sharedMaterial = LineMaterial();
        l.positionCount = points;
        l.startWidth = width;
        l.endWidth = width;
        l.startColor = color;
        l.endColor = color;
        l.useWorldSpace = true;
        l.numCapVertices = 0;
        l.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return l;
    }

    // A cord that sags between two points.
    static void Draw(LineRenderer l, Vector3 a, Vector3 b, float sag)
    {
        for (int i = 0; i < l.positionCount; i++)
        {
            float t = i / (float)(l.positionCount - 1);
            l.SetPosition(i, Vector3.Lerp(a, b, t) + Vector3.down * Mathf.Sin(t * Mathf.PI) * sag);
        }
    }

    static void DrawRing(LineRenderer l, Vector3 centre, float radius)
    {
        for (int i = 0; i < l.positionCount; i++)
        {
            float a = i / (float)(l.positionCount - 1) * Mathf.PI * 2f;
            l.SetPosition(i, centre + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * radius);
        }
    }

    static string Key(Entity_Master master, Kind kind) { return master.NetworkObjectId + "/" + (int)kind; }

    // Number below 0 takes the line away; otherwise it is made or told again (a ring's number is its radius).
    static void SetLink(Entity_Master master, Kind kind, Vector3 to, float number)
    {
        if (master == null)
            return;
        if (number < 0f)
        {
            RemoveLink(master, kind);
            return;
        }
        Link link;
        string key = Key(master, kind);
        if (!links.TryGetValue(key, out link))
        {
            link = new Link { Kind = kind, From = master };
            Linksdrawn++;
            switch (kind)
            {
                case Kind.Plugcord:
                case Kind.Nodecord:
                    link.Line = Line("Uloe_Cord", 10, Cord, 0.03f);
                    GameObject plug = ModKitAssets.Model("Uloe_Plug");
                    if (plug != null) link.Plug = UnityEngine.Object.Instantiate(plug);
                    break;
                case Kind.Ring:
                    link.Line = Line("Uloe_Ring", 41, new Color(Bright.r, Bright.g, Bright.b, 0.5f), 0.04f);
                    break;
                case Kind.Sweep:
                    link.Line = Line("Uloe_Sweep", 2, Soft, 0.3f);
                    link.Line.endWidth = 0.6f;
                    break;
                default:
                    link.Line = Line("Uloe_Ray", 2, Soft, kind == Kind.Longray ? 0.22f : 0.14f);
                    link.Line.endWidth = kind == Kind.Longray ? 0.3f : 0.2f;
                    break;
            }
            links[key] = link;
        }
        if (link.To != to)
            link.Lamp = null;
        link.To = to;
        link.Radius = number;
        link.Heard = Time.time;
        link.Until = Time.time + UloeData.LinkLife;
        if (kind == Kind.Plugcord && ModKitUtil.OwnedLocally(master))
            link.Line.startWidth = link.Line.endWidth = 0.035f;
    }

    static void RemoveLink(Entity_Master master, Kind kind)
    {
        Link link;
        string key = Key(master, kind);
        if (!links.TryGetValue(key, out link))
            return;
        Dispose(link);
        links.Remove(key);
    }

    static void Dispose(Link link)
    {
        if (link.Line != null)
            UnityEngine.Object.Destroy(link.Line.gameObject);
        if (link.Plug != null)
            UnityEngine.Object.Destroy(link.Plug);
    }

    static void ClearLinks()
    {
        foreach (Link link in links.Values)
            Dispose(link);
        links.Clear();
    }

    // Each frame: the plug cord runs from the vessel's foot to its plug; a ring goes where its owner goes.
    static void DrawLinks()
    {
        gone.Clear();
        float now = Time.time;
        foreach (KeyValuePair<string, Link> kv in links)
        {
            Link l = kv.Value;
            if (l.From == null || l.Line == null || now >= l.Until)
            {
                Dispose(l);
                gone.Add(kv.Key);
                continue;
            }
            Vector3 at = ModKitUtil.Pos(l.From);
            switch (l.Kind)
            {
                case Kind.Plugcord:
                case Kind.Nodecord:
                    Vector3 a = at + Vector3.down * (l.Kind == Kind.Plugcord ? 0.42f : 0.35f);
                    Vector3 b = Ground(l.To) + Vector3.up * 0.12f;
                    Draw(l.Line, a, b, Mathf.Min(0.1f, 0.03f * ModKitUtil.Flat(a, b)));
                    if (l.Plug != null)
                    {
                        l.Plug.transform.position = b + Vector3.up * 0.06f;
                        Vector3 d = b - a;
                        d.y = 0f;
                        if (d.sqrMagnitude > 0.001f)
                            l.Plug.transform.rotation = Quaternion.LookRotation(d, Vector3.up);
                    }
                    break;
                case Kind.Ring:
                    DrawRing(l.Line, Ground(at) + Vector3.up * 0.06f, l.Radius);
                    break;
                case Kind.Sweep:
                    DrawSweep(l, at, now);
                    break;
                default:
                    // The Flood Light's lamp turns to what it lights (its glass looks along local -z).
                    if (l.Lamp == null)
                    {
                        l.Lamp = Part(l.From, "Lamp");
                        if (l.Lamp != null)
                        {
                            Vector3 flat = l.To - l.Lamp.position;
                            flat.y = 0f;
                            if (flat.sqrMagnitude > 0.0001f)
                                l.Lamp.rotation = Quaternion.LookRotation(-flat.normalized, Vector3.up);
                        }
                    }
                    l.Line.SetPosition(0, l.Lamp != null ? l.Lamp.position + Vector3.up * 0.05f : at + Vector3.up * 0.2f);
                    l.Line.SetPosition(1, l.To + Vector3.up * 0.2f);
                    break;
            }
        }
        foreach (string key in gone)
            links.Remove(key);
    }

    // Lighthouse: the server tells where the beam's end is and how many seconds a turn takes (its Radius); the beam turns on
    // from there by itself, the same way round as the server's (Atan2 of z over x, rising), and the lamp turns with it.
    static void DrawSweep(Link l, Vector3 at, float now)
    {
        Vector3 d = l.To - at;
        d.y = 0f;
        float reach = d.magnitude, turn = Mathf.Max(0.5f, l.Radius);
        float a = Mathf.Atan2(d.z, d.x) + (now - l.Heard) / turn * Mathf.PI * 2f;
        Vector3 dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
        if (l.Lamp == null)
            l.Lamp = Part(l.From, "Lamp");
        if (l.Lamp != null)
            l.Lamp.rotation = Quaternion.LookRotation(-dir, Vector3.up);
        l.Line.SetPosition(0, l.Lamp != null ? l.Lamp.position + Vector3.up * 0.05f : at + Vector3.up * 0.2f);
        l.Line.SetPosition(1, at + dir * reach + Vector3.up * 0.2f);
    }

    static IEnumerator LineRun(Vector3 a, Vector3 b, float seconds, float width, Color color)
    {
        LineRenderer l = Line("Uloe_Beamline", 2, color, width);
        for (float e = 0f; e < seconds && l != null; e += Time.deltaTime)
        {
            float k = 1f - e / seconds;
            l.SetPosition(0, a);
            l.SetPosition(1, b);
            l.startWidth = l.endWidth = width * k;
            yield return null;
        }
        if (l != null) UnityEngine.Object.Destroy(l.gameObject);
    }

    // ---------------------------------------------------------------- the beam

    // The lens turns to the foe (the "Lens" part, its glass looks along local -z), and a bright line runs to it, thicker in
    // more Light. 'multi' is the Damage share Light and Burning Point add.
    static IEnumerator BeamRun(Entity_Master glass, Vector3 foe, float multi)
    {
        if (glass == null)
            yield break;
        Transform lens = Part(glass, "Lens");
        Vector3 start = ModKitUtil.Pos(glass) + Vector3.up * UloeData.BeamEye;
        if (lens != null)
        {
            Vector3 d = foe - lens.position;
            if (d.sqrMagnitude > 0.0001f)
                lens.rotation = Quaternion.LookRotation(-d.normalized, Vector3.up);
            start = lens.position;
        }
        float width = 0.03f + 0.02f * Mathf.Clamp(multi, 0f, 3f);
        Run(LineRun(start, foe + Vector3.up * 0.25f, 0.18f, width, Bright));
        ModKitAssets.Effect("World_Uloe_Spark", foe + Vector3.up * 0.3f, 0.8f);
    }

    static Transform Part(Entity_Master master, string name)
    {
        foreach (Transform t in master.GetComponentsInChildren<Transform>(false))
            if (t.name == name)
                return t;
        return null;
    }
}
