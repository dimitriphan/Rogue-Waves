// Runs on every player (host included). Listens to the Vis RPCs the server sends through Argo's entities and plays
// Argo's own particles, sounds and turret motion, keeps each Drill Rig's Tremor ring drawn where AIM put it at its real
// radius, marks the towers Nauts are tuning, the foes Argo pins and soaks, bobs Deep Water's swell, and shows owner-only
// warnings. Projectiles
// are the game's own and need nothing here.
public static class ArgoClient
{
    class Drillvis { public Entity_Master Master; public GameObject Ring; public float Radius; }
    class Swellvis { public Transform Part; public Vector3 Rest; public float Phase; }

    static readonly List<Entity_Master_Client> hooked = new List<Entity_Master_Client>();
    static readonly List<Entity_Master> drills = new List<Entity_Master>();
    static readonly List<Entity_Master> nauts = new List<Entity_Master>();
    static readonly Dictionary<Entity_Master, Drillvis> rings = new Dictionary<Entity_Master, Drillvis>();
    static readonly Dictionary<Entity_Master, GameObject> tuneMarks = new Dictionary<Entity_Master, GameObject>();
    static readonly Dictionary<Transform, float> spinning = new Dictionary<Transform, float>();
    static readonly Dictionary<Entity_Master, Swellvis> swells = new Dictionary<Entity_Master, Swellvis>();
    static Game_Entity seen;
    static float nextScan;

    public static void Update()
    {
        Game_Entity ge = Game_Entity.Instance;
        if (ge != seen)
        {
            seen = ge;
            hooked.Clear();
            drills.Clear();
            nauts.Clear();
            rings.Clear();
            tuneMarks.Clear();
            spinning.Clear();
            swells.Clear();
        }
        Spin();
        Bob();
        if (ge == null || ge.Entityclient == null || Time.unscaledTime < nextScan)
            return;
        nextScan = Time.unscaledTime + 0.25f;
        try { Scan(ge); }
        catch (Exception e) { ArgoLog.Warn("client scan: " + e.Message); }
    }

    static void Scan(Game_Entity ge)
    {
        hooked.RemoveAll(c => c == null);
        drills.RemoveAll(d => d == null);
        nauts.RemoveAll(n => n == null);
        foreach (Entity_Master master in ge.Entityclient)
        {
            if (master == null || master.Client == null || hooked.Contains(master.Client))
                continue;
            string being = master.Being.Value.ToString();
            bool tower = ArgoData.BuildFor(being) != null, crew = ArgoData.CrewFor(being) != null;
            if (being != ArgoData.Hero && !tower && !crew)
                continue;
            Entity_Master m = master;
            master.Client.Vis += (type, position, number) => OnVis(m, type, position, number);
            hooked.Add(master.Client);
            if (being == ArgoData.Drill) drills.Add(master);
            if (being == ArgoData.Water || being == ArgoData.Pool) swells[master] = new Swellvis();
            if (crew) nauts.Add(master);
        }
        foreach (Entity_Master d in drills)
            Tremor(d);
        FindSwells();
        List<Entity_Master> gone = new List<Entity_Master>();
        foreach (KeyValuePair<Entity_Master, Drillvis> kv in rings)
            if (kv.Key == null) gone.Add(kv.Key);
        foreach (Entity_Master g in gone)
        {
            if (rings[g].Ring != null) UnityEngine.Object.Destroy(rings[g].Ring);
            rings.Remove(g);
        }
        foreach (Entity_Master n in nauts)
            TuneMark(n);
        gone.Clear();
        foreach (KeyValuePair<Entity_Master, GameObject> kv in tuneMarks)
            if (kv.Key == null) gone.Add(kv.Key);
        foreach (Entity_Master g in gone)
        {
            if (tuneMarks[g] != null) UnityEngine.Object.Destroy(tuneMarks[g]);
            tuneMarks.Remove(g);
        }
    }

    // ---------------------------------------------------------------- Vis RPCs

    static void OnVis(Entity_Master master, string type, Vector3 position, float number)
    {
        if (type == null)
            return;
        try
        {
            Vector3 at = master != null ? ArgoUtil.Pos(master) : position;
            // The stock melee AI announces each swing with "Attack" (Entity_Vis_Mob_Attack plays the wrench animation).
            if (type == "Attack" && master != null && master.Being.Value.ToString() == ArgoData.Hero)
            {
                ArgoAssets.Play("Argo_Swing", at);
                return;
            }
            if (!type.StartsWith("Argo_"))
                return;
            switch (type)
            {
                case "Argo_Clang":
                    ArgoAssets.Effect("World_Argo_Clang", position + Vector3.up * 0.2f, 1f);
                    ArgoAssets.Play("Argo_Clang", position);
                    break;
                case "Argo_Sonar":
                    Animate(master, "Sonar");
                    ArgoAssets.Play("Argo_Sonar", at);
                    Ring("World_Argo_Sonarring", at, number, 1.5f);
                    MarkPings(master, at, number);
                    break;
                case "Argo_Scatter":
                case "Argo_Broadside":
                    Aim(master, position, 0.09f);
                    ArgoAssets.Play("Argo_Scatter", at, type == "Argo_Broadside" ? 1.4f : 1f);
                    Muzzle(master, position, type == "Argo_Broadside");
                    break;
                case "Argo_Harpoon":
                    Aim(master, position, 0.05f);
                    ArgoAssets.Play("Argo_Harpoon", at, number > 0f ? 0.8f : 1f);
                    break;
                case "Argo_Lance":
                    Aim(master, position, 0.12f);
                    ArgoAssets.Play("Argo_Lance", at);
                    ArgoAssets.Effect("World_Argo_Lanceflash", Muzzlepoint(master, position), 1.2f);
                    break;
                case "Argo_Bleed": ArgoAssets.Effect("World_Argo_Bleed", position, 1f); break;
                case "Argo_Reel":
                    // the harpoon's line, from the muzzle to the foe it reels in, while the winch clicks
                    ArgoAssets.Play("Argo_Winch", at);
                    Run(Rope(master, (ulong)number, position, ArgoData.ReelTime + 0.1f));
                    break;
                case "Argo_Loaded":
                    ArgoAssets.Play("Argo_Winch", at, 0.5f);
                    break;
                case "Argo_Paydirt":
                    // a felled foe is dug up: dust where it fell, a crack from it to the drill
                    ArgoAssets.Effect("World_Argo_Drilldust", Ground(position) + Vector3.up * 0.1f, 1.2f);
                    if (master != null)
                        Run(Tether(Ground(position), Ground(at), 0.9f));
                    break;
                case "Argo_Mine":
                    ArgoAssets.Effect("World_Argo_Drilldust", Ground(position) + Vector3.up * 0.1f, 1.2f);
                    ArgoAssets.Play("Argo_Drill", at);
                    SpinFor(master, 1.6f);
                    // Which ore the drill bites: a crack runs along the ground from the drill to it.
                    if (master != null)
                        Run(Tether(Ground(at), Ground(position), 0.9f));
                    break;
                case "Argo_Gusher":
                    ArgoAssets.Effect("World_Argo_Gusher", Ground(position), 2.5f);
                    ArgoAssets.Play("Argo_Fathom", position, 0.8f);
                    break;
                case "Argo_Fathom":
                    ArgoAssets.Effect("World_Argo_Fathom", Ground(at), 2.5f);
                    ArgoAssets.Play("Argo_Fathom", at);
                    FathomNote(master, Mathf.RoundToInt(number));
                    break;
                case "Argo_Quake":
                    Ring("World_Argo_Quake", at, number, 1.4f);
                    ArgoAssets.Play("Argo_Quake", at);
                    break;
                // ---- 2.0
                case "Argo_Pin":
                    // x = the foe's id, y = seconds, z = 1 for a stun (Anchor Line beside a building)
                    OnFoe(position.z > 0.5f ? "World_Argo_Stun" : "World_Argo_Pinned", position, position.z > 0.5f ? 0.75f : 0.05f);
                    ArgoAssets.Play("Argo_Pin", FoePos(position, at));
                    break;
                case "Argo_Wet": OnFoe("World_Argo_Wet", position, 0.45f); break;
                case "Argo_Kelp": OnFoe("World_Argo_Kelp", position, 0.05f); break;
                case "Argo_Dragunder":
                    OnFoe("World_Argo_Dragunder", position, 0.05f);
                    ArgoAssets.Play("Argo_Gurgle", FoePos(position, at));
                    break;
                case "Argo_Brine": ArgoAssets.Effect("World_Argo_Brine", position + Vector3.up * 0.2f, 1.2f); break;
                case "Argo_Whirl":
                    Ring("World_Argo_Whirl", position, number, 1f);
                    break;
                case "Argo_Bedrock": ArgoAssets.Effect("World_Argo_Bedrock", Ground(position) + Vector3.up * 0.05f, 1f); break;
                case "Argo_Faultburst":
                    ArgoAssets.Effect("World_Argo_Faultburst", Ground(position) + Vector3.up * 0.05f, 1.2f);
                    ArgoAssets.Play("Argo_Quake", position, 0.4f);
                    break;
                case "Argo_Flood":
                    ArgoAssets.Effect("World_Argo_Flood", Ground(position) + Vector3.up * 0.1f, 1.5f);
                    ArgoAssets.Play("Argo_Splash", position);
                    break;
                case "Argo_Aim":
                    Ring("World_Argo_Quake", position, number, 1f);
                    ArgoAssets.Play("Argo_Drill", at, 0.6f);
                    if (master != null) Tremor(master);
                    break;
                case "Argo_Build":
                    ArgoAssets.Effect("World_Argo_Bubbles", Ground(at), 2f);
                    bool water = master != null && ArgoData.BuildFor(master.Being.Value.ToString()) != null && ArgoData.BuildFor(master.Being.Value.ToString()).Trap;
                    ArgoAssets.Play(water ? "Argo_Splash" : "Argo_Build", at);
                    break;
                case "Argo_Upgrade": ArgoAssets.Play("Argo_Upgrade", at); break;
                case "Argo_Towerdown":
                    ArgoAssets.Effect("World_Argo_Wreck", Ground(at), 2.5f);
                    ArgoAssets.Play("Argo_Death", at, 0.6f);
                    break;
                case "Argo_Herodeath":
                    ArgoAssets.Effect("World_Hero_Argo_Death", at, 3f);
                    ArgoAssets.Play("Argo_Death", at);
                    break;
                case "Argo_Naut": ArgoAssets.Play("Argo_Naut", position); break;
                case "Argo_Dive":
                    ArgoAssets.Effect("World_Argo_Bubbles", Ground(position), 2f);
                    ArgoAssets.Play("Argo_Dive", position);
                    break;
                case "Argo_Tune":
                    ArgoAssets.Effect("World_Argo_Tunespark", position + Vector3.up * 0.4f, 1.2f);
                    ArgoAssets.Play("Argo_Tune", position);
                    break;
                case "Argo_Repair": ArgoAssets.Effect("World_Argo_Tunespark", position + Vector3.up * 0.5f, 1f); break;
                case "Argo_Dry":
                    if (ArgoUtil.OwnedLocally(master))
                        Helper_Uitools.Warning("This Drill Rig has no ore within 2 tiles to mine", "", 3f);
                    break;
                case "Argo_Warn":
                    Warn(master, Mathf.RoundToInt(number));
                    break;
            }
        }
        catch (Exception e)
        {
            ArgoLog.Warn("client vis " + type + ": " + e.Message);
        }
    }

    // An effect that rides on a foe for a while: the server sends (foe id, seconds, flag) as the position.
    static void OnFoe(string vfx, Vector3 code, float height)
    {
        Entity_Master foe = ArgoUtil.ById((ulong)Mathf.RoundToInt(code.x));
        if (foe == null)
            return;
        GameObject fx = ArgoAssets.Effect(vfx, ArgoUtil.Pos(foe) + Vector3.up * height, Mathf.Max(0.3f, code.y));
        if (fx != null)
            fx.transform.SetParent(foe.transform, true);
    }

    static Vector3 FoePos(Vector3 code, Vector3 fallback)
    {
        Entity_Master foe = ArgoUtil.ById((ulong)Mathf.RoundToInt(code.x));
        return foe != null ? ArgoUtil.Pos(foe) : fallback;
    }

    static void Warn(Entity_Master master, int code)
    {
        if (!ArgoUtil.OwnedLocally(master) || code <= 0)
            return;
        int text = code >= 100 ? code / 100 : code, crew = code >= 100 ? code % 100 : -1;
        if (text >= ArgoHeroBrain.WarnText.Length)
            return;
        string name = crew >= 0 && crew < ArgoData.Nauts.Length ? ArgoData.Nauts[crew].Name : "";
        Helper_Uitools.Warning(string.Format(ArgoHeroBrain.WarnText[text], name), "", 2f);
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
        if (ArgoMod.Instance != null)
            ArgoMod.Instance.StartCoroutine(routine);
    }

    // Entity positions sit half a tile above the ground they stand on.
    public static Vector3 Ground(Vector3 entityPosition) { return entityPosition + Vector3.down * 0.48f; }

    // Rings are drawn at radius 1 and scaled to the server's radius (they simulate in local space, Hierarchy scaling).
    static void Ring(string vfx, Vector3 at, float radius, float life)
    {
        GameObject ring = ArgoAssets.Effect(vfx, Ground(at) + Vector3.up * 0.05f, life);
        if (ring == null)
            return;
        radius = Mathf.Max(0.3f, radius);
        ring.transform.localScale = Vector3.one * radius;
        Unscale(ring, vfx, radius);
    }

    // Hierarchy scaling grows the particles with the ring; keep them their designed size (the burst happens on the first
    // update, so this lands before any particle exists).
    static void Unscale(GameObject fx, string vfx, float scale)
    {
        ParticleSystem ps = fx.GetComponent<ParticleSystem>();
        GameObject template = ArgoAssets.Vfx(vfx);
        ParticleSystem tps = template != null ? template.GetComponent<ParticleSystem>() : null;
        if (ps == null || tps == null || scale <= 0.01f)
            return;
        ParticleSystem.MainModule main = ps.main;
        main.startSizeMultiplier = tps.main.startSizeMultiplier / scale;
    }

    // SONAR: every enemy inside the ring gets a ping marker for as long as it is Pinged on the server.
    static void MarkPings(Entity_Master hero, Vector3 at, float radius)
    {
        if (hero == null || Game_Entity.Instance == null)
            return;
        int team = ArgoUtil.Team(hero);
        foreach (Entity_Master e in Game_Entity.Instance.Entityclient)
        {
            if (e == null || e.Client == null || e.Client.Beingobject == null || e.Client.Beingobject.Entity == null)
                continue;
            List<string> tags = e.Client.Beingobject.Entity.Tags;
            if (ArgoUtil.Team(e) == team || tags == null || !tags.Contains("Mob") || ArgoUtil.Flat(ArgoUtil.Pos(e), at) > radius)
                continue;
            GameObject mark = ArgoAssets.Effect("World_Argo_Ping", ArgoUtil.Pos(e) + Vector3.up * 0.55f, ArgoData.PingDuration);
            if (mark != null)
                mark.transform.SetParent(e.transform, true);
        }
    }

    // ---------------------------------------------------------------- turrets

    static Transform Part(Entity_Master master, string name)
    {
        if (master == null)
            return null;
        foreach (Transform t in master.GetComponentsInChildren<Transform>(false))
            if (t.name == name)
                return t;
        return null;
    }

    static Vector3 Muzzlepoint(Entity_Master master, Vector3 target)
    {
        Vector3 pos = ArgoUtil.Pos(master);
        Vector3 d = target - pos;
        d.y = 0f;
        return pos + Vector3.up * 0.15f + (d.sqrMagnitude > 0.01f ? d.normalized * 0.4f : Vector3.zero);
    }

    // Swings the model's "Turret" part toward the target (its barrel points down local -z), tips it down or up toward
    // targets below or above (as far as the mount allows), and kicks it back a little along the shot.
    public const float PitchDown = 30f, PitchUp = 20f;

    static void Aim(Entity_Master master, Vector3 target, float kick)
    {
        Transform turret = Part(master, "Turret");
        if (turret == null)
            return;
        Vector3 d = target - turret.position;
        Vector3 flat = new Vector3(d.x, 0f, d.z);
        if (flat.sqrMagnitude < 0.0001f)
            return;
        float pitch = Mathf.Clamp(Mathf.Atan2(d.y, flat.magnitude) * Mathf.Rad2Deg, -PitchDown, PitchUp) * Mathf.Deg2Rad;
        Vector3 dir = flat.normalized * Mathf.Cos(pitch) + Vector3.up * Mathf.Sin(pitch);
        turret.rotation = Quaternion.LookRotation(-dir, Vector3.up);
        if (kick > 0f)
            Run(Recoil(turret, dir, kick));
    }

    static readonly Dictionary<Transform, Vector3> rest = new Dictionary<Transform, Vector3>();

    static IEnumerator Recoil(Transform t, Vector3 dir, float kick)
    {
        Vector3 home;
        if (!rest.TryGetValue(t, out home))
            rest[t] = home = t.localPosition;
        Vector3 back = t.parent != null ? t.parent.InverseTransformDirection(-dir) * kick : -dir * kick;
        for (float e = 0f; e < 0.25f && t != null; e += Time.deltaTime)
        {
            float k = e < 0.05f ? e / 0.05f : 1f - (e - 0.05f) / 0.2f;
            t.localPosition = home + back * k;
            yield return null;
        }
        if (t != null)
            t.localPosition = home;
    }

    static void Muzzle(Entity_Master master, Vector3 target, bool ring)
    {
        if (master == null)
            return;
        if (ring)
        {
            ArgoAssets.Effect("World_Argo_Smokering", ArgoUtil.Pos(master), 1.5f);
            return;
        }
        GameObject smoke = ArgoAssets.Effect("World_Argo_Muzzle", Muzzlepoint(master, target), 1.2f);
        if (smoke != null)
        {
            Vector3 d = target - ArgoUtil.Pos(master);
            d.y = 0f;
            if (d.sqrMagnitude > 0.01f)
                smoke.transform.rotation = Quaternion.LookRotation(d.normalized, Vector3.up);
        }
    }

    static Material tetherMat;

    static Material TetherMaterial()
    {
        if (tetherMat != null)
            return tetherMat;
        GameObject fx = ArgoAssets.Vfx("World_Argo_Drilldust");
        ParticleSystemRenderer r = fx != null ? fx.GetComponent<ParticleSystemRenderer>() : null;
        tetherMat = r != null && r.sharedMaterial != null ? new Material(r.sharedMaterial) : new Material(Shader.Find("Sprites/Default"));
        tetherMat.name = "Argo_Tether";
        return tetherMat;
    }

    // The harpoon's line: a taut rope from the gun's muzzle to the foe being reeled in, following it as it comes.
    static IEnumerator Rope(Entity_Master gun, ulong foeId, Vector3 fallback, float seconds)
    {
        Entity_Master foe = ArgoUtil.ById(foeId);
        GameObject go = new GameObject("Argo_Rope");
        LineRenderer l = go.AddComponent<LineRenderer>();
        l.sharedMaterial = TetherMaterial();
        l.useWorldSpace = true;
        l.positionCount = 2;
        l.numCapVertices = 0;
        l.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        Color rope = new Color(0.78f, 0.64f, 0.42f, 1f);
        for (float e = 0f; e < seconds && l != null && gun != null; e += Time.deltaTime)
        {
            Vector3 end = (foe != null ? ArgoUtil.Pos(foe) : fallback) + Vector3.up * 0.3f;
            l.SetPosition(0, Muzzlepoint(gun, end) + Vector3.up * 0.15f);
            l.SetPosition(1, end);
            float k = e < seconds - 0.1f ? 1f : (seconds - e) / 0.1f;
            l.startWidth = l.endWidth = 0.035f;
            l.startColor = l.endColor = new Color(rope.r, rope.g, rope.b, k);
            yield return null;
        }
        if (go != null) UnityEngine.Object.Destroy(go);
    }

    // A jagged brass crack along the ground from the Drill Rig to the ore it bites, fading out.
    static IEnumerator Tether(Vector3 from, Vector3 to, float seconds)
    {
        GameObject go = new GameObject("Argo_Tether");
        LineRenderer l = go.AddComponent<LineRenderer>();
        l.sharedMaterial = TetherMaterial();
        l.useWorldSpace = true;
        l.positionCount = 7;
        l.numCapVertices = 0;
        l.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        Vector3 side = Vector3.Cross(Vector3.up, to - from).normalized * 0.07f;
        for (int i = 0; i < l.positionCount; i++)
        {
            float t = i / (float)(l.positionCount - 1);
            float jag = (i == 0 || i == l.positionCount - 1) ? 0f : (i % 2 == 0 ? 1f : -1f);
            l.SetPosition(i, Vector3.Lerp(from, to, t) + side * jag + Vector3.up * 0.06f);
        }
        Color brass = new Color(ArgoData.Brass.r, ArgoData.Brass.g, ArgoData.Brass.b, 1f);
        for (float e = 0f; e < seconds && l != null; e += Time.deltaTime)
        {
            float k = 1f - e / seconds;
            l.startWidth = l.endWidth = 0.07f * (0.5f + 0.5f * k);
            l.startColor = l.endColor = new Color(brass.r, brass.g, brass.b, k);
            yield return null;
        }
        if (go != null) UnityEngine.Object.Destroy(go);
    }

    // The Drill Rig's bit ("Spinner") turns while it bites.
    static void SpinFor(Entity_Master master, float seconds)
    {
        Transform s = Part(master, "Spinner");
        if (s != null)
            spinning[s] = Time.time + seconds;
    }

    static void Spin()
    {
        if (spinning.Count == 0)
            return;
        List<Transform> done = null;
        foreach (KeyValuePair<Transform, float> kv in spinning)
        {
            if (kv.Key == null || Time.time > kv.Value)
            {
                if (done == null) done = new List<Transform>();
                done.Add(kv.Key);
                continue;
            }
            kv.Key.Rotate(0f, 540f * Time.deltaTime, 0f, Space.Self);
        }
        if (done != null)
            foreach (Transform t in done)
                spinning.Remove(t);
    }

    // Deep Water and its FLOOD pools: the water ("Swell", the model's frame stands still) rises and falls a little. The
    // phase follows the tile, so a row of pools rolls like one swell. The visual is rebuilt on every upgrade, so a part
    // that went null is found again at the next scan, and its rest position read afresh.
    public const float SwellAmp = 0.012f, SwellPeriod = 3.2f, SwellWave = 0.9f;

    static void FindSwells()
    {
        List<Entity_Master> gone = null;
        foreach (KeyValuePair<Entity_Master, Swellvis> kv in swells)
        {
            if (kv.Key == null)
            {
                if (gone == null) gone = new List<Entity_Master>();
                gone.Add(kv.Key);
                continue;
            }
            Swellvis sv = kv.Value;
            if (sv.Part != null)
                continue;
            sv.Part = Part(kv.Key, "Swell");
            if (sv.Part == null)
                continue;
            sv.Rest = sv.Part.localPosition;
            Vector3 at = ArgoUtil.Pos(kv.Key);
            sv.Phase = (at.x + at.z) * SwellWave;
        }
        if (gone != null)
            foreach (Entity_Master g in gone)
                swells.Remove(g);
    }

    static void Bob()
    {
        if (swells.Count == 0)
            return;
        float w = Time.time * (2f * Mathf.PI / SwellPeriod);
        foreach (Swellvis sv in swells.Values)
            if (sv.Part != null)
                sv.Part.localPosition = sv.Rest + Vector3.up * (SwellAmp * Mathf.Sin(w + sv.Phase));
    }

    // ---------------------------------------------------------------- Tremor rings and tuning marks

    // The Tremor radius and slow each client knows: the same sums the server uses (the Tremor and Slow stats), rebuilt
    // from what every client has - the drill's Fathom skin and its upgrade list.
    public static float TremorRadius(Entity_Master drill) { return DrillStat(drill, "Tremor", 2f) + ArgoData.FathomTremor * (Fathom(drill) - 1); }

    public static float TremorSlow(Entity_Master drill)
    {
        return Mathf.Min(ArgoData.SlowCap, DrillStat(drill, "Slow", 0.2f) + ArgoData.FathomSlow * (Fathom(drill) - 1));
    }

    public static int Fathom(Entity_Master drill)
    {
        string skin = drill.Skin.Value.ToString();
        return skin.StartsWith("Fathom") ? Mathf.Max(1, Mathf.RoundToInt(ArgoUtil.F(skin.Substring(6)))) : 1;
    }

    static bool DrillHas(Entity_Master drill, string key)
    {
        if (drill.Upgradelist != null)
            foreach (FixedString64Bytes u in drill.Upgradelist)
            {
                ArgoData.Upg def;
                if (ArgoContent.UpgradeDefs.TryGetValue(u.ToString(), out def) && def.Key == key)
                    return true;
            }
        return false;
    }

    // A drill stat's base value plus its upgrades' "<stat>_Add" lines.
    static float DrillStat(Entity_Master drill, string stat, float fallback)
    {
        ArgoData.Build b = ArgoData.BuildFor(ArgoData.Drill);
        float r = fallback;
        foreach (string s in b.Stats)
            if (s.StartsWith(stat + "=")) r = ArgoUtil.F(s.Substring(stat.Length + 1));
        if (drill.Upgradelist != null)
            foreach (FixedString64Bytes u in drill.Upgradelist)
            {
                ArgoData.Upg def;
                if (ArgoContent.UpgradeDefs.TryGetValue(u.ToString(), out def))
                    foreach (KeyValuePair<string, float> kv in ArgoUtil.Params(def.Params))
                        if (kv.Key == stat + "_Add") r += kv.Value;
            }
        return r;
    }

    // Tells the drill's owner what the Tide's Fathom brought (once, however many drills sank at the same time).
    static float lastFathomNote = -99f;

    static void FathomNote(Entity_Master drill, int fathom)
    {
        if (drill == null || fathom < 2 || !ArgoUtil.OwnedLocally(drill) || Time.unscaledTime - lastFathomNote < 3f)
            return;
        lastFathomNote = Time.unscaledTime;
        // From the Fathom in the message, not the skin: the skin's new value may reach this player a moment later.
        int below = fathom - 1;
        float pace = (1f + ArgoData.FathomYield * below) * (1f + ArgoData.FathomSpeed * below);
        float radius = DrillStat(drill, "Tremor", 2f) + ArgoData.FathomTremor * below;
        float slow = Mathf.Min(ArgoData.SlowCap, DrillStat(drill, "Slow", 0.2f) + ArgoData.FathomSlow * below);
        int deepest = DrillHas(drill, "Deepcore") ? ArgoData.FathomDeepest : ArgoData.FathomMax;
        string text = "Drill Rig sank to Fathom " + fathom + (fathom >= deepest ? " (its deepest)" : "") + ": mines " + pace.ToString("0.#") + "x as fast as at first, Tremor "
            + radius.ToString("0.#") + " tiles, " + Mathf.RoundToInt(slow * 100f) + "% slow";
        Helper_Uitools.Warning(text, "", 4f);
    }

    // 2.0: the ring stands where AIM put the Tremor. The server syncs the centre and radius in the drill's AIM command
    // (Commandlist, so late joiners have it too); before the first sync it rings the drill at the radius each client
    // can work out from the drill's Fathom skin and upgrades.
    static void Tremor(Entity_Master drill)
    {
        Drillvis v;
        if (!rings.TryGetValue(drill, out v))
            rings[drill] = v = new Drillvis { Master = drill };
        if (v.Ring == null)
        {
            v.Ring = ArgoAssets.Effect("World_Argo_Tremor", Ground(ArgoUtil.Pos(drill)) + Vector3.up * 0.04f, 0f);
            if (v.Ring == null)
                return;
            v.Radius = -1f;
        }
        Vector3 center;
        float r;
        if (!Aimed(drill, out center, out r))
        {
            center = ArgoUtil.Pos(drill);
            r = TremorRadius(drill);
        }
        v.Ring.transform.position = Ground(center) + Vector3.up * 0.04f;
        if (!Mathf.Approximately(r, v.Radius))
        {
            v.Radius = r;
            Size(v.Ring, "World_Argo_Tremor", r);
        }
    }

    public static bool Aimed(Entity_Master drill, out Vector3 center, out float radius)
    {
        center = Vector3.zero;
        radius = 0f;
        if (drill == null || drill.Commandlist == null)
            return false;
        // The last "Aim" entry: Updatecommand moves the entry it updates to the end, so with a stale duplicate in the
        // list (round 2 read 2.4 while the server sent 4.4) the last one is the newest.
        bool found = false;
        foreach (Link_Commandref c in drill.Commandlist)
            if (c.Name.ToString() == ArgoData.AimLink && c.Goalvalue > 0f)
            {
                center = c.Goalposition;
                radius = c.Goalvalue;
                found = true;
            }
        return found;
    }

    // A ring drawn at radius 1, scaled to r whatever its parent's scale, its particles kept their designed size.
    public static void Size(GameObject ring, string vfx, float r)
    {
        if (ring == null)
            return;
        ring.transform.localScale = Vector3.one;
        Vector3 lossy = ring.transform.lossyScale;
        ring.transform.localScale = new Vector3(r / Mathf.Max(0.01f, lossy.x), r / Mathf.Max(0.01f, lossy.y), r / Mathf.Max(0.01f, lossy.z));
        Unscale(ring, vfx, r);
    }

    // A Naut in its "Tune" skin marks the tower it works on with a few rising sparks.
    static void TuneMark(Entity_Master naut)
    {
        bool tuning = naut.Skin.Value.ToString() == "Tune";
        GameObject mark;
        tuneMarks.TryGetValue(naut, out mark);
        if (!tuning)
        {
            if (mark != null) UnityEngine.Object.Destroy(mark);
            tuneMarks.Remove(naut);
            return;
        }
        if (mark != null)
            return;
        Entity_Master tower = null;
        float best = 2.3f;
        foreach (Entity_Master e in Game_Entity.Instance.Entityclient)
        {
            if (e == null || e.Client == null || e.Client.Beingobject == null || e.Client.Beingobject.Entity == null || e.Client.Beingobject.Entity.Tags == null)
                continue;
            if (!e.Client.Beingobject.Entity.Tags.Contains("Tower"))
                continue;
            float d = ArgoUtil.Flat(ArgoUtil.Pos(e), ArgoUtil.Pos(naut));
            if (d < best) { best = d; tower = e; }
        }
        if (tower == null)
            return;
        mark = ArgoAssets.Effect("World_Argo_Tuned", ArgoUtil.Pos(tower) + Vector3.up * 0.3f, 0f);
        if (mark != null)
        {
            mark.transform.SetParent(tower.transform, true);
            tuneMarks[naut] = mark;
        }
    }

#if ARGO_SHOTS
    // Self-test close-up: a SONAR ring, a quake ring and a tuning mark beside the vessel.
    public static void Demo(Vector3 hero)
    {
        Ring("World_Argo_Sonarring", hero, 3f, 1.5f);
        Ring("World_Argo_Quake", hero + Vector3.right * 2f, 1.5f, 1.4f);
        ArgoAssets.Effect("World_Argo_Tunespark", hero + Vector3.left * 1.2f + Vector3.up * 0.4f, 1.2f);
    }
#endif
}
