// Kit 1.13 (2026-11-05). Versions and compatibility rules: TDS_Vessel_Creator\kit\README.md
// Shared kit: a map's lighting changed while it runs (kit docs/16 §7): the fog, the three ambient colours, the
// sun, and a stock lighting group's post-processing borrowed whole. Read from the game's code, untested in game:
//   - Every map carries a Bl_Lighting_Group; Bl_Lighting.Editlighting(group) sets linear fog (colour, start, end), the
//     ambient gradient (sky, horizon, ground), a fresh "Sun" light (colour, intensity, direction), the reflection probe,
//     the skybox, and puts the group's URP volume profile on every global Volume (the game's own Betterfog post effect
//     and its grade live in that profile). The game calls it at each map's load on each machine (Game_Client_Init).
//   - The 55 stock groups sit in the master database's "Lighting" folder at all times ("Map_Woozy", "Macro_World"...).
//   - Nothing syncs lighting: a part calls these on every machine, from something every machine knows (a synced name,
//     a networked value), never from the host alone.
//   - The game's reference list for mod code has no URP assembly, so a mod can't name Volume, VolumeProfile or the game's
//     Betterfog_Volumeoverride (CS0012 in game), nor reach them by reflection (banned). Post-processing is borrowed whole:
//     Grade(name) applies a copy of a stock group, with the held look's fog, ambient and sun written over it. Never touch
//     Bl_Lighting_Group.Profile: the cloud stubs type it as object, so such a line compiles there and fails in game
//     (pitfall #167).
// Set(look, seconds) fades from what is on screen to a look and holds it, so the game's own Editlighting at a map's load
// can't undo it; the hold drops by itself when the map goes. A borrowed grade can't be held: any later Editlighting takes
// it off (the game calls it only at a map's load). Call Update() every frame on every machine, from ONE part only: each
// part compiles its own copy of this class, and two copies holding looks would overwrite each other every frame (the
// pack's lighting goes through the mode).
public static class ModKitLight
{
    public class Look
    {
        public bool Fog = true;
        public Color FogColor = Color.grey;
        public float FogStart, FogEnd = 100f;
        public bool Relative;                     // FogStart/FogEnd are multiples of the camera's distance to the ground
        public Color Top = Color.grey, Mid = Color.grey, Bottom = Color.grey;   // the ambient gradient: sky, horizon, ground
        public Color Sun = Color.white;
        public float SunIntensity = 1f;
        public Vector3 SunDirection;              // zero: the sun keeps its direction

        public Look Copy() { return (Look)MemberwiseClone(); }

        public static Look Of(Blockage.Lighting.Bl_Lighting_Group g)
        {
            Look l = new Look();
            if (g == null) return l;
            l.Fog = g.Fog; l.FogColor = g.Fogcolor; l.FogStart = g.Fogstart; l.FogEnd = g.Fogend;
            l.Top = g.Topcolor; l.Mid = g.Midcolor; l.Bottom = g.Bottomcolor;
            l.Sun = g.Suncolor; l.SunIntensity = g.Sun ? g.Sunintensity : 0f; l.SunDirection = g.Sundirection;
            return l;
        }

        // What this machine draws now (fog distances as set, never relative).
        public static Look Now()
        {
            Look l = new Look();
            l.Fog = RenderSettings.fog; l.FogColor = RenderSettings.fogColor;
            l.FogStart = RenderSettings.fogStartDistance; l.FogEnd = RenderSettings.fogEndDistance;
            l.Top = RenderSettings.ambientSkyColor; l.Mid = RenderSettings.ambientEquatorColor; l.Bottom = RenderSettings.ambientGroundColor;
            Light sun = SunLight();
            l.Sun = sun != null ? sun.color : Color.black;
            l.SunIntensity = sun != null ? sun.intensity : 0f;
            l.SunDirection = sun != null ? sun.transform.forward : Vector3.zero;
            return l;
        }

        // Relative and the sun's direction switch at the half-way mark; a side with fog off fades as fog that starts and ends
        // far away, so fog neither pops in at the start nor out at the end.
        public static Look Lerp(Look a, Look b, float t)
        {
            t = Mathf.Clamp01(t);
            Look l = (t < 0.5f ? a : b).Copy();
            float far = Mathf.Max(a.Fog ? a.FogEnd : 0f, b.Fog ? b.FogEnd : 0f) * 20f + 1000f;
            l.FogColor = Color.Lerp(a.Fog ? a.FogColor : b.FogColor, b.Fog ? b.FogColor : a.FogColor, t);
            l.FogStart = Mathf.Lerp(a.Fog ? a.FogStart : far, b.Fog ? b.FogStart : far, t);
            l.FogEnd = Mathf.Lerp(a.Fog ? a.FogEnd : far, b.Fog ? b.FogEnd : far, t);
            l.Fog = a.Fog || b.Fog;
            l.Top = Color.Lerp(a.Top, b.Top, t); l.Mid = Color.Lerp(a.Mid, b.Mid, t); l.Bottom = Color.Lerp(a.Bottom, b.Bottom, t);
            l.Sun = Color.Lerp(a.Sun, b.Sun, t);
            l.SunIntensity = Mathf.Lerp(a.SunIntensity, b.SunIntensity, t);
            l.SunDirection = a.SunDirection == Vector3.zero || b.SunDirection == Vector3.zero ? b.SunDirection : Vector3.Slerp(a.SunDirection, b.SunDirection, t);
            return l;
        }

        public override string ToString()
        {
            return "fog " + (Fog ? Hex(FogColor) + " " + N(FogStart) + "-" + N(FogEnd) + (Relative ? " x camera" : "") : "off")
                + ", ambient " + Hex(Top) + "/" + Hex(Mid) + "/" + Hex(Bottom) + ", sun " + Hex(Sun) + " x" + N(SunIntensity)
                + (SunDirection != Vector3.zero ? " toward " + N(SunDirection.x) + "," + N(SunDirection.y) + "," + N(SunDirection.z) : "");
        }
    }

    public static float GroundY;          // the height Relative fog measures the camera to (the part sets it per map)
    public static float Reach = -1f;      // the camera's distance to the ground under the screen's centre, as last measured
    public static Look Holding { get { return want; } }
    public static string Graded { get { return graded != null ? gradedFrom : ""; } }

    static Look want, from, written;
    static float fadeStart, fadeSeconds, writtenReach, writtenEnd;
    static Light writtenSun;
    static Game_Mapvars heldMap;
    static Blockage.Lighting.Bl_Lighting_Group graded;
    static string gradedFrom = "";
    static bool restoring;

    // A stock lighting group by name; null when there is none.
    public static Blockage.Lighting.Bl_Lighting_Group Group(string name)
    {
        Dictionary<string, UnityEngine.Object> folder = ModKitUtil.Folder("Lighting");
        UnityEngine.Object found;
        return folder != null && name != null && folder.TryGetValue(name, out found) ? found as Blockage.Lighting.Bl_Lighting_Group : null;
    }

    public static List<string> Groups()
    {
        List<string> names = new List<string>();
        Dictionary<string, UnityEngine.Object> folder = ModKitUtil.Folder("Lighting");
        if (folder != null)
            foreach (KeyValuePair<string, UnityEngine.Object> kv in folder)
                if (kv.Value is Blockage.Lighting.Bl_Lighting_Group) names.Add(kv.Key);
        names.Sort(string.CompareOrdinal);
        return names;
    }

    // The running map's own lighting group, as the game applied it at the load.
    public static Blockage.Lighting.Bl_Lighting_Group MapOwn()
    {
        Game_Mapvars mv = Game_Mapvars.Instance;
        return mv != null && mv.Map != null ? mv.Map.Lighting : null;
    }

    public static Light SunLight()
    {
        if (RenderSettings.sun != null) return RenderSettings.sun;
        GameObject sun = Blockage.Lighting.Bl_Lighting.Getsun();
        return sun != null ? sun.GetComponent<Light>() : null;
    }

    public static Camera View()
    {
        Camera main = Camera.main;
        if (main != null && main.isActiveAndEnabled) return main;
        Camera best = null;
        foreach (Camera c in Camera.allCameras)
            if (c != null && c.isActiveAndEnabled && (best == null || c.depth > best.depth)) best = c;
        return best;
    }

    // Fades to a look over the seconds (0: at once) and holds it on this map. False when no map is running.
    public static bool Set(Look look, float seconds)
    {
        Game_Mapvars mv = Game_Mapvars.Instance;
        if (look == null || mv == null)
            return false;
        from = want != null ? Shown() : Look.Now();
        want = look.Copy();
        heldMap = mv;
        fadeStart = Time.unscaledTime;
        fadeSeconds = Mathf.Max(0f, seconds);
        restoring = false;
        Write(fadeSeconds > 0f ? from : want);
        return true;
    }

    // Borrows a stock group's post-processing (and, with sky, its skybox): a copy of it, with the held look (or what is on
    // screen) written over its fog, ambient and sun, and the map's own sun and reflection probe kept. False if no such group.
    public static bool Grade(string name, bool sky)
    {
        Blockage.Lighting.Bl_Lighting_Group stock = Group(name), own = MapOwn();
        if (stock == null || Game_Mapvars.Instance == null)
            return false;
        if (want == null) Set(Look.Now(), 0f);     // held, so the copy goes with the map
        Blockage.Lighting.Bl_Lighting_Group copy = UnityEngine.Object.Instantiate(stock);
        copy.name = stock.name + " (graded)";
        Fill(copy, Shown());
        if (own != null)
        {
            copy.Sun = own.Sun;
            copy.Sunshadowintensity = own.Sunshadowintensity;
            copy.Enviormentreflections = own.Enviormentreflections;
            copy.Reflectprobe = own.Reflectprobe; copy.Reflectposition = own.Reflectposition; copy.Reflectterrain = own.Reflectterrain;
            if (!sky) copy.Skybox = own.Skybox;
        }
        Blockage.Lighting.Bl_Lighting.Editlighting(copy);
        if (graded != null) UnityEngine.Object.Destroy(graded);
        graded = copy;
        gradedFrom = name;
        written = null;
        Write(Shown());
        return true;
    }

    // Back to the map's own lighting: fades the fog, ambient and sun there over the seconds, then the game's own call puts
    // its grade, sun and probe back and the hold drops.
    public static void Restore(float seconds)
    {
        Blockage.Lighting.Bl_Lighting_Group own = MapOwn();
        if (own == null) { Drop(); return; }
        if (seconds <= 0f) { Blockage.Lighting.Bl_Lighting.Editlighting(own); Drop(); return; }
        Set(Look.Of(own), seconds);
        restoring = true;
    }

    // Every frame, every machine.
    public static void Update()
    {
        if (want == null)
            return;
        Game_Mapvars mv = Game_Mapvars.Instance;
        if (mv == null || mv != heldMap) { Drop(); return; }
        bool fading = fadeSeconds > 0f && Time.unscaledTime - fadeStart < fadeSeconds;
        if (fading || written != want || Overwritten())     // != want: the fade's last step, or a new look
            Write(Shown());
        if (restoring && Time.unscaledTime - fadeStart >= fadeSeconds)
            Restore(0f);
    }

    // Only when the look moves, the camera's distance changes (Relative), or something else (the game's Editlighting) wrote
    // over it: each write of the ambient colours makes the engine work out the ambient light again.
    static bool Overwritten()
    {
        if (written == null || want.Relative && !Mathf.Approximately(Measure(), writtenReach))
            return true;
        Light sun = SunLight();
        return sun != writtenSun || RenderSettings.fog != written.Fog || RenderSettings.fogColor != written.FogColor || RenderSettings.fogEndDistance != writtenEnd
            || RenderSettings.ambientSkyColor != written.Top || RenderSettings.ambientEquatorColor != written.Mid
            || RenderSettings.ambientGroundColor != written.Bottom || sun != null && (sun.color != written.Sun || sun.intensity != written.SunIntensity);
    }

    static void Drop()
    {
        want = from = written = null;
        writtenSun = null;
        Reach = -1f;
        heldMap = null;
        restoring = false;
        if (graded != null) UnityEngine.Object.Destroy(graded);
        graded = null;
        gradedFrom = "";
    }

    static Look Shown()
    {
        float t = fadeSeconds > 0f ? (Time.unscaledTime - fadeStart) / fadeSeconds : 1f;
        return t >= 1f || from == null ? want : Look.Lerp(Absolute(from), Absolute(want), t);
    }

    // A fade runs in plain distances, so a relative look and a fixed one blend without a jump.
    static Look Absolute(Look l)
    {
        if (!l.Relative) return l;
        Look a = l.Copy();
        Distances(l, out a.FogStart, out a.FogEnd);
        a.Relative = false;
        return a;
    }

    static void Fill(Blockage.Lighting.Bl_Lighting_Group g, Look l)
    {
        float s, e;
        Distances(l, out s, out e);
        g.Fog = l.Fog; g.Fogcolor = l.FogColor; g.Fogstart = s; g.Fogend = e;
        g.Topcolor = l.Top; g.Midcolor = l.Mid; g.Bottomcolor = l.Bottom;
        g.Suncolor = l.Sun; g.Sunintensity = l.SunIntensity;
        if (l.SunDirection != Vector3.zero) g.Sundirection = l.SunDirection;
    }

    // The camera's distance to the ground (at GroundY) under the screen's centre; the last good one if none now.
    public static float Measure()
    {
        Camera cam = View();
        if (cam != null)
        {
            Ray r = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            float d;
            if (new Plane(Vector3.up, new Vector3(0f, GroundY, 0f)).Raycast(r, out d) && d > 0.01f)
                Reach = d;
        }
        return Reach;
    }

    // A relative look with no camera to measure yet (the map's intro) keeps its fog out of sight.
    static void Distances(Look l, out float start, out float end)
    {
        start = l.FogStart;
        end = l.FogEnd;
        if (!l.Relative)
            return;
        float reach = Measure();
        start = reach > 0f ? start * reach : 100000f;
        end = reach > 0f ? end * reach : 100001f;
    }

    // As Editlighting sets them, without making a new sun or touching the grade.
    static void Write(Look l)
    {
        float s, e;
        Distances(l, out s, out e);
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fog = l.Fog;
        RenderSettings.fogColor = l.FogColor;
        RenderSettings.fogStartDistance = s;
        RenderSettings.fogEndDistance = Mathf.Max(e, s + 0.01f);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = l.Top;
        RenderSettings.ambientEquatorColor = l.Mid;
        RenderSettings.ambientGroundColor = l.Bottom;
        Light sun = SunLight();
        if (sun != null)
        {
            sun.color = l.Sun;
            sun.intensity = l.SunIntensity;
            if (l.SunDirection != Vector3.zero) sun.transform.rotation = Quaternion.LookRotation(l.SunDirection, Vector3.up);
        }
        written = l;
        writtenEnd = RenderSettings.fogEndDistance;
        writtenSun = sun;
        writtenReach = Reach;
    }

    // A point light, for a lantern in the fog or a glow at night; made in the map's scene, so it goes with the map. Whether
    // the game's renderer draws extra lights is for the PC to show.
    public static Light Lamp(Vector3 at, Color color, float range, float intensity)
    {
        GameObject go = new GameObject("ModKitLight_Lamp");
        go.transform.position = at;
        Light l = go.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = color;
        l.range = range;
        l.intensity = intensity;
        l.shadows = LightShadows.None;
        return l;
    }

    static string N(float v) { return v.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture); }
    static string Hex(Color c) { return ColorUtility.ToHtmlStringRGB(c); }
}
