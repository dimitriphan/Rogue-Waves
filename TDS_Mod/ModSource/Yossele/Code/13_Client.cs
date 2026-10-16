// Runs on every player (host included). Listens to the Vis RPCs the server sends through Yossele's entities and plays
// Yossele's own sounds and moving pieces (spore bombs in their arc, seeds flying, the ready seeds floating round each
// Dandelion Tree), turns the Mushroom Cannon's cap toward each lob, and shows owner-only messages. The Stonehenge's
// facing needs nothing here: the game turns every placed tower's Vis by its orientation (Entity_Master_Client). Spore,
// daze, tangle and barrier looks are not here: the game attaches those itself (Vfxdebuffcheck by name). Clients read
// only synced state (each entity's being name, owner, position and Stats counters).
public static class YosseleClient
{
    class Tree { public Entity_Master Master; public readonly List<GameObject> Seeds = new List<GameObject>(); }

    static readonly List<Entity_Master_Client> hooked = new List<Entity_Master_Client>();
    static readonly List<Tree> trees = new List<Tree>();
    static Game_Entity seen;
    static float nextScan;
    public static int Lobs, Seedruns;

    public static void Update()
    {
        Game_Entity ge = Game_Entity.Instance;
        if (ge != seen)
        {
            seen = ge;
            hooked.Clear();
            foreach (Tree t in trees) Clear(t);
            trees.Clear();
        }
        try { Orbit(); }
        catch (Exception e) { YosseleLog.Warn("client seeds: " + e.Message); }
        if (ge == null || ge.Entityclient == null || Time.unscaledTime < nextScan)
            return;
        nextScan = Time.unscaledTime + 0.25f;
        try { Scan(ge); }
        catch (Exception e) { YosseleLog.Warn("client scan: " + e.Message); }
    }

    static void Scan(Game_Entity ge)
    {
        hooked.RemoveAll(c => c == null);
        for (int i = trees.Count - 1; i >= 0; i--)
            if (trees[i].Master == null)
            {
                Clear(trees[i]);
                trees.RemoveAt(i);
            }
        foreach (Entity_Master master in ge.Entityclient)
        {
            if (master == null || master.Client == null || hooked.Contains(master.Client))
                continue;
            string being = master.Being.Value.ToString();
            if (being != YosseleData.Hero && YosseleData.BuildFor(being) == null && being != YosseleData.Vine && being != YosseleData.Fruit)
                continue;
            Entity_Master m = master;
            master.Client.Vis += (type, position, number) => OnVis(m, type, position, number);
            hooked.Add(master.Client);
            if (being == YosseleData.Dandelion) trees.Add(new Tree { Master = master });
        }
    }

    static void OnVis(Entity_Master master, string type, Vector3 position, float number)
    {
        if (type == null)
            return;
        try
        {
            Vector3 at = master != null ? YosseleUtil.Pos(master) : position;
            // The stock melee AI announces each swing with "Attack".
            if (type == "Attack")
            {
                YosseleAssets.Play("Yossele_Thud", at);
                return;
            }
            if (!type.StartsWith("Yossele_"))
                return;
            int n = Mathf.RoundToInt(number);
            switch (type)
            {
                case "Yossele_Lob":
                    Lobs++;
                    Aim(master, position);
                    YosseleAssets.Play("Yossele_Lob", at);
                    Run(LobRun(Muzzle(master, position), position, Mathf.Max(0.2f, number)));
                    break;
                case "Yossele_Blast":
                    YosseleAssets.Play("Yossele_Pop", position);
                    Scaled("World_Yossele_Sporeblast", Ground(position), number, 1.5f);
                    break;
                case "Yossele_Finish":
                    YosseleAssets.Play("Yossele_Pop", position, 1.3f);
                    Scaled("World_Yossele_Sporeblast", Ground(position), number, 1.5f);
                    break;
                case "Yossele_Dazehit":
                    YosseleAssets.Play("Yossele_Thud", position, 0.5f);
                    break;
                case "Yossele_Seeds":
                    Seedruns++;
                    YosseleAssets.Play("Yossele_Seeds", at);
                    Run(SeedRun(master, position, n));
                    break;
                case "Yossele_Seedhit":
                    YosseleAssets.Effect("World_Yossele_Seedhit", position + Vector3.up * 0.2f, 1.2f);
                    break;
                case "Yossele_Vines":
                    YosseleAssets.Play("Yossele_Creak", at);
                    break;
                case "Yossele_Grow":
                    YosseleAssets.Play("Yossele_Rustle", at);
                    YosseleAssets.Effect("World_Yossele_Grow", Ground(at) + Vector3.up * 0.1f, 2f);
                    if (n >= 100)
                        YosseleAssets.Effect("World_Yossele_Fertilize", Ground(at) + Vector3.up * 0.1f, 2f);
                    break;
                case "Yossele_Fertilize":
                    Animate(master, "Fertilize");
                    YosseleAssets.Play("Yossele_Fertilize", position);
                    break;
                case "Yossele_Compost":
                    YosseleAssets.Effect("World_Yossele_Compost", at + Vector3.up * 0.3f, 2f);
                    YosseleAssets.Play("Yossele_Pick", at, 0.6f);
                    if (YosseleUtil.OwnedLocally(master))
                        Helper_Uitools.Warning(YosseleData.WarnText[YosseleData.WarnFertilizer] + " (" + n + ")", "", 2f);
                    break;
                case "Yossele_Dig":
                    YosseleAssets.Play("Yossele_Dig", position, 0.7f);
                    YosseleAssets.Effect("World_Yossele_Dig", Ground(position) + Vector3.up * 0.1f, 1.2f);
                    break;
                case "Yossele_Fruitdrop":
                    YosseleAssets.Play("Yossele_Pick", position, 0.6f);
                    YosseleAssets.Effect("World_Yossele_Grow", Ground(position) + Vector3.up * 0.1f, 2f);
                    break;
                case "Yossele_Fruitpick":
                    YosseleAssets.Play("Yossele_Pick", position);
                    YosseleAssets.Effect("World_Yossele_Fruitpick", position + Vector3.up * 0.2f, 2f);
                    break;
                case "Yossele_Drain":
                    YosseleAssets.Effect("World_Yossele_Drain", at + Vector3.up * 0.3f, 1.5f);
                    break;
                case "Yossele_Snare":
                    YosseleAssets.Play("Yossele_Creak", position, 0.8f);
                    YosseleAssets.Effect("World_Yossele_Drain", position + Vector3.up * 0.3f, 1.5f);
                    break;
                case "Yossele_Blockadebreak":
                    YosseleAssets.Play("Yossele_Creak", at, 1.2f);
                    break;
                case "Yossele_Colour":
                    YosseleAssets.Play("Yossele_Rustle", at, 0.7f);
                    if (YosseleUtil.OwnedLocally(master) && n >= 0 && n < YosseleData.Colours.Length)
                        Helper_Uitools.Warning(YosseleData.WarnText[YosseleData.WarnColour + n], "", 2f);
                    break;
                case "Yossele_Built":
                    YosseleAssets.Effect("World_Yossele_Grow", Ground(at) + Vector3.up * 0.1f, 2f);
                    YosseleAssets.Play("Yossele_Rustle", at, 0.6f);
                    break;
                case "Yossele_Upgrade":
                    YosseleAssets.Play("Yossele_Rustle", at, 0.8f);
                    break;
                case "Yossele_Towerdown":
                    YosseleAssets.Effect("World_Yossele_Towerdown", at, 2f);
                    YosseleAssets.Play("Yossele_Creak", at, 1.3f);
                    break;
                case "Yossele_Herodeath":
                    YosseleAssets.Effect("World_Hero_Yossele_Death", at, 2.5f);
                    YosseleAssets.Play("Yossele_Death", at);
                    break;
                case "Yossele_Warn":
                    int code = n % 100, value = n / 100;
                    if (YosseleUtil.OwnedLocally(master) && code > 0 && code < YosseleData.WarnText.Length)
                        Helper_Uitools.Warning(YosseleData.WarnText[code] + (value > 0 ? " (" + value + ")" : ""), "", 2f);
                    break;
            }
        }
        catch (Exception e)
        {
            YosseleLog.Warn("client vis " + type + ": " + e.Message);
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
        if (YosseleMod.Instance != null)
            YosseleMod.Instance.StartCoroutine(routine);
    }

    static Vector3 Ground(Vector3 entityPosition) { return entityPosition + Vector3.down * 0.48f; }

    static void Scaled(string vfx, Vector3 at, float radius, float life)
    {
        GameObject go = YosseleAssets.Effect(vfx, at, life);
        if (go != null)
            go.transform.localScale = Vector3.one * Mathf.Max(0.3f, radius);
    }

    static Transform Part(Entity_Master master, string name)
    {
        if (master == null)
            return null;
        foreach (Transform t in master.GetComponentsInChildren<Transform>(false))
            if (t.name == name)
                return t;
        return null;
    }

    // A synced stat as every client sees it (Statsserver is the host's only).
    public static float ClientStat(Entity_Master master, string name)
    {
        if (master == null || master.Stats == null)
            return 0f;
        for (int i = 0; i < master.Stats.Count; i++)
            if (master.Stats[i].Name.ToString() == name)
                return master.Stats[i].Value;
        return 0f;
    }

    // ---------------------------------------------------------------- the Mushroom Cannon

    // Swings the model's "Turret" part (the cap) toward where the bomb lands; its mouth points down local -z.
    static void Aim(Entity_Master master, Vector3 target)
    {
        Transform turret = Part(master, "Turret");
        if (turret == null)
            return;
        Vector3 d = target - turret.position;
        d.y = 0f;
        if (d.sqrMagnitude < 0.0001f)
            return;
        turret.rotation = Quaternion.LookRotation(-d.normalized, Vector3.up);
    }

    static Vector3 Muzzle(Entity_Master master, Vector3 target)
    {
        Vector3 pos = master != null ? YosseleUtil.Pos(master) : target;
        YosseleData.Build b = master != null ? YosseleData.BuildFor(master.Being.Value.ToString()) : null;
        Vector3 d = target - pos;
        d.y = 0f;
        return pos + Vector3.up * (b != null ? b.Muzzle : 0.6f) + (d.sqrMagnitude > 0.01f ? d.normalized * 0.25f : Vector3.zero);
    }

    // The spore bomb's arc: a parabola peaking a little higher the farther it flies.
    static IEnumerator LobRun(Vector3 from, Vector3 to, float flight)
    {
        GameObject model = YosseleAssets.Model("Yossele_Sporebomb");
        GameObject bomb = model != null ? UnityEngine.Object.Instantiate(model, from, Quaternion.identity) : null;
        if (bomb == null)
            yield break;
        bomb.SetActive(true);
        float peak = 1f + 0.25f * YosseleUtil.Flat(from, to);
        for (float e = 0f; e < flight && bomb != null; e += Time.deltaTime)
        {
            float k = e / flight;
            bomb.transform.position = Vector3.Lerp(from, to, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * peak;
            bomb.transform.Rotate(0f, 360f * Time.deltaTime, 0f);
            yield return null;
        }
        if (bomb != null)
            UnityEngine.Object.Destroy(bomb);
    }

    // ---------------------------------------------------------------- the Dandelion Tree

    static Vector3 Crown(Entity_Master tree)
    {
        YosseleData.Build b = YosseleData.BuildFor(YosseleData.Dandelion);
        return YosseleUtil.Pos(tree) + Vector3.up * (b != null ? b.Muzzle : 1.1f);
    }

    // The ready seeds float round the crown: as many as its synced "Seeds" counter (up to SeedsDrawn).
    static void Orbit()
    {
        foreach (Tree t in trees)
        {
            if (t.Master == null)
                continue;
            int want = Mathf.Clamp(Mathf.FloorToInt(ClientStat(t.Master, "Seeds")), 0, YosseleData.SeedsDrawn);
            while (t.Seeds.Count < want)
            {
                GameObject model = YosseleAssets.Model("Yossele_Seed");
                if (model == null)
                    return;
                GameObject seed = UnityEngine.Object.Instantiate(model);
                seed.SetActive(true);
                t.Seeds.Add(seed);
            }
            while (t.Seeds.Count > want)
            {
                GameObject last = t.Seeds[t.Seeds.Count - 1];
                t.Seeds.RemoveAt(t.Seeds.Count - 1);
                if (last != null) UnityEngine.Object.Destroy(last);
            }
            Vector3 c = Crown(t.Master);
            float count = Mathf.Max(1, t.Seeds.Count);
            for (int i = 0; i < t.Seeds.Count; i++)
            {
                if (t.Seeds[i] == null)
                    continue;
                float a = Time.time * 0.6f + i * Mathf.PI * 2f / count;
                float ring = 0.45f + 0.12f * (i % 3);
                t.Seeds[i].transform.position = c + new Vector3(Mathf.Cos(a) * ring, 0.1f * Mathf.Sin(Time.time * 1.7f + i) + 0.08f * (i % 3), Mathf.Sin(a) * ring);
            }
        }
    }

    static void Clear(Tree t)
    {
        foreach (GameObject s in t.Seeds)
            if (s != null) UnityEngine.Object.Destroy(s);
        t.Seeds.Clear();
    }

    // A strike: up to 8 seeds drawn flying from the crown to where the foe stood.
    static IEnumerator SeedRun(Entity_Master tree, Vector3 to, int count)
    {
        GameObject model = YosseleAssets.Model("Yossele_Seed");
        if (model == null || tree == null)
            yield break;
        Vector3 c = Crown(tree);
        List<GameObject> flying = new List<GameObject>();
        List<Vector3> starts = new List<Vector3>();
        int n = Mathf.Clamp(count, 1, 8);
        for (int i = 0; i < n; i++)
        {
            float a = i * Mathf.PI * 2f / n;
            Vector3 s = c + new Vector3(Mathf.Cos(a) * 0.45f, 0f, Mathf.Sin(a) * 0.45f);
            GameObject g = UnityEngine.Object.Instantiate(model, s, Quaternion.identity);
            g.SetActive(true);
            flying.Add(g);
            starts.Add(s);
        }
        Vector3 end = to + Vector3.up * 0.25f;
        float time = YosseleData.SeedFlight;
        for (float e = 0f; e < time; e += Time.deltaTime)
        {
            float k = e / time;
            for (int i = 0; i < flying.Count; i++)
                if (flying[i] != null)
                    flying[i].transform.position = Vector3.Lerp(starts[i], end, k * k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 0.3f;
            yield return null;
        }
        foreach (GameObject g in flying)
            if (g != null) UnityEngine.Object.Destroy(g);
    }
}
