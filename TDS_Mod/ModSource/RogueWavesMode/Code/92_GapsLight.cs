#if ROGUEWAVESMODE_GAPS
// Debug build only, its own launch: -Define ROGUEWAVESMODE_SELFTEST,ROGUEWAVESMODE_GAPS,ROGUEWAVESMODE_SHOTS with -rwonly light.
// Can a map's lighting be changed a lot, fog included (Dimitri, 2026-11-05)? The kit's ModKitLight (kit 1.13) on one real map,
// a picture of each step for his eye (shot_light_*), and the 55 stock lighting groups' numbers to pick from:
//   L0 the stock groups by name, the camera, and the map's own lighting is what's on screen
//   L1 five looks made in code (sea fog, light mist, night, storm, dusk), each read back from what the game draws
//   L2 the hold: the game's own lighting call (as at a map's load) can't undo a held look
//   L3 a fade from the map's own lighting into sea fog over 4 s
//   L4 six stock groups' post-processing borrowed whole, alone and under sea fog
//   L5 a lamp (a point light) by night
//   L6 back to the map's own lighting, and what each look costs a frame
// None of this is in the release game: no weather or map uses a look yet (a design call for Dimitri).
public static partial class RWTest
{
    static readonly string[] GradeFrom = { "Map_Final", "Map_Thehunt", "Map_Crimson", "Map_Snobound", "Map_Hydragen", "Macro_World" };

    static ModKitLight.Look Make(string fog, float start, float end, string top, string mid, string bottom, string sun, float intensity, Vector3 toward)
    {
        ModKitLight.Look l = new ModKitLight.Look();
        l.Fog = true;
        l.FogColor = Hex(fog);
        l.FogStart = start;
        l.FogEnd = end;
        l.Relative = true;
        l.Top = Hex(top); l.Mid = Hex(mid); l.Bottom = Hex(bottom);
        l.Sun = Hex(sun);
        l.SunIntensity = intensity;
        l.SunDirection = toward == Vector3.zero ? Vector3.zero : toward.normalized;
        return l;
    }

    static Color Hex(string rgb)
    {
        Color c;
        return ColorUtility.TryParseHtmlString("#" + rgb, out c) ? c : Color.magenta;
    }

    static bool Near(Color a, Color b) { return Mathf.Abs(a.r - b.r) < 0.01f && Mathf.Abs(a.g - b.g) < 0.01f && Mathf.Abs(a.b - b.b) < 0.01f; }

    static bool Near(float a, float b) { return Mathf.Abs(a - b) <= 0.01f * Mathf.Max(1f, Mathf.Abs(b)); }

    // What the game draws now against what the look asks for; "" when they agree.
    static string Differs(ModKitLight.Look want)
    {
        float scale = want.Relative ? ModKitLight.Reach : 1f;
        string d = "";
        if (RenderSettings.fog != want.Fog) d += " fog " + RenderSettings.fog;
        if (!Near(RenderSettings.fogColor, want.FogColor)) d += " fog colour " + ColorUtility.ToHtmlStringRGB(RenderSettings.fogColor);
        if (want.Fog && !Near(RenderSettings.fogEndDistance, want.FogEnd * scale)) d += " fog end " + RWUtil.F(RenderSettings.fogEndDistance) + " (want " + RWUtil.F(want.FogEnd * scale) + ")";
        if (!Near(RenderSettings.ambientSkyColor, want.Top)) d += " sky " + ColorUtility.ToHtmlStringRGB(RenderSettings.ambientSkyColor);
        if (!Near(RenderSettings.ambientEquatorColor, want.Mid)) d += " horizon " + ColorUtility.ToHtmlStringRGB(RenderSettings.ambientEquatorColor);
        if (!Near(RenderSettings.ambientGroundColor, want.Bottom)) d += " ground " + ColorUtility.ToHtmlStringRGB(RenderSettings.ambientGroundColor);
        Light sun = ModKitLight.SunLight();
        if (sun == null) { if (want.SunIntensity > 0f) d += " no sun"; }
        else if (!Near(sun.intensity, want.SunIntensity) || !Near(sun.color, want.Sun)) d += " sun " + ColorUtility.ToHtmlStringRGB(sun.color) + " x" + RWUtil.F(sun.intensity);
        return d.Trim();
    }

    static IEnumerator LightShot(string name)
    {
        yield return new WaitForSecondsRealtime(1.5f);
#if ROGUEWAVESMODE_SHOTS
        yield return RWShots.Shot("light_" + name);
#endif
    }

    // The average frame over a few seconds, in ms.
    static IEnumerator FrameCost(string what, Action<float> got)
    {
        int frames = 0;
        float begin = Time.unscaledTime;
        while (Time.unscaledTime - begin < 3f) { frames++; yield return null; }
        float ms = (Time.unscaledTime - begin) * 1000f / Mathf.Max(1, frames);
        Info("light cost", what + ": " + RWUtil.F(ms) + " ms a frame over " + frames + " frames");
        got(ms);
    }

    static IEnumerator GapsLight()
    {
        yield return ToMap(Seed, RWChart.Calm);
        Game_Mapvars mv = Game_Mapvars.Instance;
        if (mv == null || !mv.Maploaded.Value) { Setup("light", "no map"); yield break; }
        int e0 = errors;
        string map = mv.Map != null ? mv.Map.name : "-";

        // L0: the groups, the camera, the map's own.
        List<string> names = ModKitLight.Groups();
        int withFog = 0;
        foreach (string n in names)
        {
            Blockage.Lighting.Bl_Lighting_Group g = ModKitLight.Group(n);
            if (g.Fog) withFog++;
            Info("light group", n + ": " + ModKitLight.Look.Of(g) + (g.Sun ? "" : ", no sun") + (g.Skybox != null ? ", sky " + g.Skybox.name : ", no sky"));
        }
        Check("L0 the stock lighting groups can be read by name", names.Count >= 50 && ModKitLight.Group("Map_Woozy") != null, names.Count + " groups, " + withFog + " with fog on");
        List<Genitem> ground = RWUtil.Ground();
        float low = float.MaxValue, high = float.MinValue;
        foreach (Genitem g in ground) { low = Mathf.Min(low, g.Position.y); high = Mathf.Max(high, g.Position.y); }
        ModKitLight.GroundY = ground.Count > 0 ? low : 0f;
        ModKitLight.Measure();
        Camera cam = ModKitLight.View();
        Info("light camera", cam == null ? "none" : cam.name + (cam == Camera.main ? " (main)" : "") + ", " + (cam.orthographic ? "orthographic, size " + RWUtil.F(cam.orthographicSize) : "perspective, fov " + RWUtil.F(cam.fieldOfView))
            + ", at " + RWUtil.V(cam.transform.position) + " looking " + RWUtil.V(cam.transform.forward) + ", " + RWUtil.F(ModKitLight.Reach) + " to the ground at y " + RWUtil.F(low) + " (top tile " + RWUtil.F(high) + "), "
            + Camera.allCamerasCount + " cameras on");
        Blockage.Lighting.Bl_Lighting_Group own = ModKitLight.MapOwn();
        ModKitLight.Look mine = ModKitLight.Look.Of(own);
        if (!Check("L0 the map's own lighting is what the game draws", own != null && Differs(mine) == "", map + " lit by " + (own != null ? own.name : "nothing") + ": " + mine + "; on screen: " + ModKitLight.Look.Now() + " " + Differs(mine)) && own == null)
            yield break;
        yield return LightShot("0_own");
        float ownCost = 0f;
        yield return FrameCost("the map's own", ms => ownCost = ms);

        // L1: five looks made in code.
        ModKitLight.Look sea = Make("A9BCBF", 0.55f, 1.35f, "9FB3B8", "8EA3A8", "6E7F84", "D8E2E4", 0.6f, Vector3.zero);
        ModKitLight.Look mist = Make("C9D6D8", 0.8f, 2.2f, ColorUtility.ToHtmlStringRGB(mine.Top), ColorUtility.ToHtmlStringRGB(mine.Mid), ColorUtility.ToHtmlStringRGB(mine.Bottom), ColorUtility.ToHtmlStringRGB(mine.Sun), mine.SunIntensity, Vector3.zero);
        ModKitLight.Look night = Make("0E1726", 0.9f, 2.5f, "1B2A4A", "14203A", "0B1222", "8FA8FF", 0.25f, Vector3.zero);
        ModKitLight.Look storm = Make("4A5258", 0.7f, 1.8f, "5A646C", "4B545B", "30363B", "B8C0C8", 0.45f, new Vector3(0.3f, -0.5f, 0.8f));
        ModKitLight.Look dusk = Make("E09A6B", 1f, 3f, "6A5A8A", "B07A6A", "4A3A4A", "FF9A5A", 0.9f, new Vector3(-0.8f, -0.25f, 0.3f));
        string[] lookNames = { "sea_fog", "light_mist", "night", "storm", "dusk" };
        ModKitLight.Look[] looks = { sea, mist, night, storm, dusk };
        for (int i = 0; i < looks.Length; i++)
        {
            ModKitLight.Set(looks[i], 0f);
            yield return null;
            yield return null;
            string d = Differs(looks[i]);
            Check("L1 " + lookNames[i] + ": the game draws the look asked for", d == "", looks[i] + "; on screen: " + ModKitLight.Look.Now() + (d != "" ? "; differs:" + d : ""));
            yield return LightShot("1_" + lookNames[i]);
        }
        float fogCost = 0f;
        yield return FrameCost("dusk, held every frame", ms => fogCost = ms);

        // L2: the game's own call, as at a map's load, can't undo a held look.
        ModKitLight.Set(sea, 0f);
        yield return null;
        Blockage.Lighting.Bl_Lighting.Editlighting(own);
        bool undone = Differs(sea) != "";
        yield return null;
        yield return null;
        Check("L2 a held look comes back over the game's own lighting call", undone && Differs(sea) == "", "right after the call: " + (undone ? "the map's own" : "still the look") + "; two frames on: " + (Differs(sea) == "" ? "the look" : Differs(sea)));

        // L3: a fade from the map's own into sea fog.
        ModKitLight.Restore(0f);
        yield return null;
        ModKitLight.Set(sea, 4f);
        yield return new WaitForSecondsRealtime(2f);
        Color half = RenderSettings.fogColor;
        bool between = !Near(half, mine.FogColor) && !Near(half, sea.FogColor);
#if ROGUEWAVESMODE_SHOTS
        yield return RWShots.Shot("light_3_fade_half");
#endif
        yield return new WaitForSecondsRealtime(2.5f);
        Check("L3 a fade passes between the two looks and ends on the new one", between && Differs(sea) == "", "fog colour at 2 s " + ColorUtility.ToHtmlStringRGB(half) + " (from " + ColorUtility.ToHtmlStringRGB(mine.FogColor) + " to " + ColorUtility.ToHtmlStringRGB(sea.FogColor) + "); at 4.5 s " + (Differs(sea) == "" ? "the look" : Differs(sea)));

        // L4: stock post-processing borrowed whole, alone, then under sea fog.
        ModKitLight.Restore(0f);
        yield return null;
        int graded = 0;
        string missing = "";
        float gradeCost = 0f;
        foreach (string n in GradeFrom)
        {
            if (own != null && n == own.name) continue;
            int before = errors;
            bool ok = ModKitLight.Grade(n, false);
            if (!ok) { missing += n + " "; continue; }
            graded++;
            yield return null;
            Check("L4 " + n + "'s grade borrowed on " + map + " with no error", errors == before, (errors - before) + " error lines; on screen: " + ModKitLight.Look.Now());
            yield return LightShot("4_grade_" + n);
            if (graded == 1) yield return FrameCost(n + "'s grade", ms => gradeCost = ms);
        }
        Check("L4 the stock grades asked for exist", missing == "" && graded > 0, graded + " borrowed" + (missing != "" ? "; none named " + missing : ""));
        ModKitLight.Set(sea, 0f);
        ModKitLight.Grade("Map_Snobound", false);
        yield return null;
        Check("L4 a grade keeps the held look", Differs(sea) == "", ModKitLight.Graded + " under sea fog: " + (Differs(sea) == "" ? "the look" : Differs(sea)));
        yield return LightShot("4_grade_Map_Snobound_sea_fog");

        // L5: a lamp by night, by the vessel.
        ModKitLight.Restore(0f);
        yield return null;
        ModKitLight.Set(night, 0f);
        Entity_Master vessel = RWRun.Vessel(RWMine.Session);
        Light lamp = vessel != null ? ModKitLight.Lamp(vessel.Entityposition.Value + Vector3.up * 1.5f, Hex("FFB55A"), 6f, 3f) : null;
        Check("L5 a lamp stands in the map's scene, so it goes with the map", lamp != null && lamp.isActiveAndEnabled && lamp.gameObject.scene == mv.gameObject.scene,
            lamp != null ? "at " + RWUtil.V(lamp.transform.position) + " in scene " + lamp.gameObject.scene.name + ", the map's " + mv.gameObject.scene.name : "no vessel");
        yield return LightShot("5_night_lamp");
        if (lamp != null) UnityEngine.Object.Destroy(lamp.gameObject);

        // L6: back to the map's own.
        ModKitLight.Restore(2f);
        yield return new WaitForSecondsRealtime(2.5f);
        Check("L6 a fade back ends on the map's own lighting and lets go", ModKitLight.Holding == null && Differs(mine) == "", (ModKitLight.Holding == null ? "let go" : "still holding") + "; " + (Differs(mine) == "" ? "the map's own" : Differs(mine)));
        yield return LightShot("6_own_again");
        Info("light cost", "a frame: the map's own " + RWUtil.F(ownCost) + " ms, a held look " + RWUtil.F(fogCost) + " ms, a borrowed grade " + RWUtil.F(gradeCost) + " ms");
        Check("L no error while the lighting changed", errors == e0, (errors - e0) + " error lines");
    }
}
#endif
