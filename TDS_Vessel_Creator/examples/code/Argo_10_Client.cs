// Runs on every player (host included). Listens to the Vis RPCs the server sends through Argo's entities and plays
// Argo's own particles, sounds and turret motion, keeps each Deep Drill's Tremor ring drawn at its real radius, marks
// the towers Nauts are tuning, and shows owner-only warnings. Projectiles are the game's own and need nothing here.
public static class ArgoClient
{
    class Drillvis { public Entity_Master Master; public GameObject Ring; public float Radius; }

    static readonly List<Entity_Master_Client> hooked = new List<Entity_Master_Client>();
    static readonly List<Entity_Master> drills = new List<Entity_Master>();
    static readonly List<Entity_Master> nauts = new List<Entity_Master>();
    static readonly Dictionary<Entity_Master, Drillvis> rings = new Dictionary<Entity_Master, Drillvis>();
    static readonly Dictionary<Entity_Master, GameObject> tuneMarks = new Dictionary<Entity_Master, GameObject>();
    static readonly Dictionary<Transform, float> spinning = new Dictionary<Transform, float>();
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
        }
        Spin();
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
            if (crew) nauts.Add(master);
        }
        foreach (Entity_Master d in drills)
            Tremor(d);
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
                case "Argo_Mine":
                    ArgoAssets.Effect("World_Argo_Drilldust", Ground(position) + Vector3.up * 0.1f, 1.2f);
                    ArgoAssets.Play("Argo_Drill", at);
                    SpinFor(master, 1.6f);
                    break;
                case "Argo_Gusher":
                    ArgoAssets.Effect("World_Argo_Gusher", Ground(position), 2.5f);
                    ArgoAssets.Play("Argo_Fathom", position, 0.8f);
                    break;
                case "Argo_Fathom":
                    ArgoAssets.Effect("World_Argo_Fathom", Ground(at), 2.5f);
                    ArgoAssets.Play("Argo_Fathom", at);
                    break;
                case "Argo_Quake":
                    Ring("World_Argo_Quake", at, number, 1.4f);
                    ArgoAssets.Play("Argo_Quake", at);
                    break;
                case "Argo_Pulse":
                    Ring("World_Argo_Pulse", at, number, 1.2f);
                    ArgoAssets.Play("Argo_Quake", at, 0.35f);
                    break;
                case "Argo_Build":
                    ArgoAssets.Effect("World_Argo_Bubbles", Ground(at), 2f);
                    ArgoAssets.Play("Argo_Build", at);
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
                        Helper_Uitools.Warning("This Deep Drill has no ore in reach", "", 2.5f);
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

    // Swings the model's "Turret" part toward the target (its barrel points down local -z) and kicks it back a little.
    static void Aim(Entity_Master master, Vector3 target, float kick)
    {
        Transform turret = Part(master, "Turret");
        if (turret == null)
            return;
        Vector3 d = target - turret.position;
        d.y = 0f;
        if (d.sqrMagnitude < 0.0001f)
            return;
        turret.rotation = Quaternion.LookRotation(-d.normalized, Vector3.up);
        if (kick > 0f)
            Run(Recoil(turret, d.normalized, kick));
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

    // The Deep Drill's bit ("Spinner") turns while it bites.
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

    // ---------------------------------------------------------------- Tremor rings and tuning marks

    // The Tremor radius each client draws: the same sum the server uses (the Range stat), rebuilt from what every
    // client knows - the drill's Fathom skin and its upgrade list.
    public static float TremorRadius(Entity_Master drill)
    {
        ArgoData.Build b = ArgoData.BuildFor(ArgoData.Drill);
        float r = 2f;
        foreach (string s in b.Stats)
            if (s.StartsWith("Range=")) r = ArgoUtil.F(s.Substring(6));
        if (drill.Upgradelist != null)
            foreach (FixedString64Bytes u in drill.Upgradelist)
            {
                ArgoData.Upg def;
                if (ArgoContent.UpgradeDefs.TryGetValue(u.ToString(), out def))
                    foreach (KeyValuePair<string, float> kv in ArgoUtil.Params(def.Params))
                        if (kv.Key == "Range_Add") r += kv.Value;
            }
        string skin = drill.Skin.Value.ToString();
        int fathom = skin.StartsWith("Fathom") ? Mathf.RoundToInt(ArgoUtil.F(skin.Substring(6))) : 1;
        return r + ArgoData.FathomTremor * Mathf.Max(0, fathom - 1);
    }

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
            v.Ring.transform.SetParent(drill.transform, true);
            v.Radius = -1f;
        }
        float r = TremorRadius(drill);
        if (!Mathf.Approximately(r, v.Radius))
        {
            v.Radius = r;
            v.Ring.transform.localScale = Vector3.one;
            Vector3 lossy = v.Ring.transform.lossyScale;
            v.Ring.transform.localScale = new Vector3(r / Mathf.Max(0.01f, lossy.x), r / Mathf.Max(0.01f, lossy.y), r / Mathf.Max(0.01f, lossy.z));
            Unscale(v.Ring, "World_Argo_Tremor", r);
        }
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
