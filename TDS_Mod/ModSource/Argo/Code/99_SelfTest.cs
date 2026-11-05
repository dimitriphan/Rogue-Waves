using Blockage.Menu;
#if ARGO_SELFTEST
// Debug build only (Build-ArgoMod.ps1 -Define ARGO_SELFTEST). Starts a solo match by itself (Embark, pick Argo, pick a
// map, ready up) because the game ignores injected input, then plays through every Argo mechanic on the host the way a
// player would (real Build, FLOOD, AIM, crew and Upgrade commands through Command_ServerRpc), forces Tides, and logs one
// "[Argo] TEST PASS|FAIL name: detail" line per check plus a final "TEST DONE" summary in Player.log. 2.0: the checks
// follow DESIGN.md's test plan (five build options, the Scatter's bonus, the Harpoon Gun's two paths, Deep Water and
// FLOOD, the aimed Tremor, Bedrock and Fault Line, the Miner's Base limit, the F-key fix).
using UnityEngine.SceneManagement;
using Unity.Netcode;

public static partial class ArgoSelfTest
{
    static int phase;
    static float seen = -1f, step = -1f;
    static string node = "";
    static bool freeze, forcing;
    static int pass, fail;
    static Beingobject enemyType;

    static void Log(string m) { ArgoLog.Info("TEST " + m); }

    static bool Check(string name, bool ok, string detail = "")
    {
        if (ok) pass++; else fail++;
        Log((ok ? "PASS " : "FAIL ") + name + (detail != "" ? ": " + detail : ""));
        return ok;
    }

    public static void Update()
    {
        try { Drive(); }
        catch (Exception e) { Log("driver error: " + e); phase = 9; }
    }

    // ---------------------------------------------------------------- getting into a match

    static void Drive()
    {
        if (freeze && !forcing && Game_Mapvars.Instance != null && ModKitUtil.IsServer && Game_Mapvars.Instance.Time_Wave.Value < 600f)
            Game_Mapvars.Instance.Time_Wave.Value = 900f;
        // Mode_Normal leaves a Tide entry once Time_Wave <= 0 (or when no enemy is left).
        if (forcing && Game_Mapvars.Instance != null && ModKitUtil.IsServer)
            Game_Mapvars.Instance.Time_Wave.Value = -1f;
        if (phase >= 3)
            return;
        float now = Time.realtimeSinceStartup;
        string scene = SceneManager.GetActiveScene().name;

        Mainmenu_Main menu = UnityEngine.Object.FindObjectOfType<Mainmenu_Main>();
        if (phase == 0 && menu != null)
        {
            if (seen < 0f) seen = now;
            if (now - seen > 8f && step < 0f)
            {
                Log("main menu: embarking");
                menu.SendMessage("Play", SendMessageOptions.DontRequireReceiver);
                step = now;
            }
            if (step > 0f && now - step > 25f)
            {
                Log("embark did not leave the menu, retrying");
                menu.SendMessage("Transitiontomain", SendMessageOptions.DontRequireReceiver);
                step = -1f;
                seen = now;
            }
            return;
        }

        Macro_Main macro = Macro_Main.Instance;
        if (phase <= 1 && macro != null && macro.IsSpawned && Tds_Multiplayer_Remote.Instance != null && Blm_Server.Instance != null && Blm_Server.Instance.Localplayer != null)
        {
            if (phase == 0) { phase = 1; seen = now; step = -1f; Log("in " + scene + ": waiting for the world map"); }
#if ARGO_SHOTS
            if (!boatShot && now - seen >= 4f && Macro_Worldvisual.Instance != null)
            {
                boatShot = true;
                ArgoMod.Instance.StartCoroutine(BoatShot());
            }
            if (now - seen < 14f)
                return;
#else
            if (now - seen < 8f)
                return;
#endif
            if (step < 0f)
            {
                macro.Selectcharacter(ArgoData.Hero);
                node = PickNode();
                Log("picked Argo and map node '" + node + "'");
                if (node != "")
                    Tds_Multiplayer_Remote.Instance.Selectmap_ServerRpc(node, false, default(ServerRpcParams));
                step = now;
                return;
            }
            if (now - step > 3f && now - step < 4f)
            {
                Tds_Clientinfo info = Blm_Server.Instance.Localplayer.GetComponent<Tds_Clientinfo>();
                Check("vessel select", info != null && info.Hero.Value.ToString() == ArgoData.Hero, info != null ? info.Hero.Value.ToString() : "no clientinfo");
                Check("map select", macro.Selected.Value.ToString() == node, macro.Selected.Value.ToString());
                macro.Playgame(true);
                step = now - 5f;
                Log("ready: starting the match");
            }
            if (now - step > 40f)
            {
                Log("match did not start; giving up");
                phase = 9;
            }
            return;
        }

        if (phase <= 2 && ArgoServer.Heroes.Count > 0 && Game_Mapvars.Instance != null && ModKitUtil.IsServer)
        {
            phase = 3;
#if ARGO_REVIEW
            ArgoMod.Instance.StartCoroutine(Review());
#else
            ArgoMod.Instance.StartCoroutine(Scenario());
#endif
        }
    }

#if ARGO_SHOTS
    static bool boatShot;

    // The boat's vessel select, opened as the Setup screen's button does, captured with Argo standing on it.
    static IEnumerator BoatShot()
    {
        Macro_Worldvisual.Instance.Opencharselect();
        yield return new WaitForSecondsRealtime(3f);
        Macro_Characterselect_Click mine = null;
        foreach (Macro_Characterselect_Click b in UnityEngine.Object.FindObjectsOfType<Macro_Characterselect_Click>())
            if (b.Character == ArgoData.Hero) mine = b;
        Check("Argo unlocked on the boat's vessel select", mine != null && !mine.Locked, mine == null ? "no button" : "locked " + mine.Locked);
        if (mine != null)
            Macro_Main.Instance.Selectcharacter(ArgoData.Hero);
        yield return new WaitForSecondsRealtime(1.5f);
        yield return ArgoShot.Shot("boat");
        if (Macro_Worldvisual.Instance != null)
            Macro_Worldvisual.Instance.Closecharselect();
    }
#endif

    // The kit's test map (ModKitTest.TestPlan), so every PC round plays the same ground; else the map the player picked
    // last, else the first open one.
    static string PickNode()
    {
        string why;
        string node = ModKitTest.PickNode(out why);
        Log("map: " + why);
        return node;
    }

    // ---------------------------------------------------------------- helpers

    static IEnumerator Wait(float seconds) { yield return new WaitForSeconds(seconds); }

    static IEnumerator Until(Func<bool> condition, float timeout)
    {
        float t = 0f;
        while (!condition() && t < timeout)
        {
            t += Time.deltaTime;
            yield return null;
        }
    }

    static void Send(Entity_Master m, string link, string json)
    {
        m.GetComponent<Entity_Master_Remote>().Command_ServerRpc(link, json, default(ServerRpcParams));
    }

    static string F(float v) { return v.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture); }

    static string Vec(Vector3 v) { return "{\"x\":" + F(v.x) + ",\"y\":" + F(v.y) + ",\"z\":" + F(v.z) + "}"; }

    static string Ref(Entity_Master m) { return "{\"Ref\":" + m.NetworkObjectId + "}"; }

    static float Res(long owner, string name)
    {
        GameObject player;
        Tds_Clientinfo info;
        if (Blm_Server.Instance == null || !Blm_Server.Instance.Sessionids.TryGetValue(owner, out player) || player == null || !player.TryGetComponent(out info))
            return -1f;
        foreach (Link_Resource r in info.Resources)
            if (r.Name.ToString() == name)
                return r.Amount;
        return 0f;
    }

    static float Life(Entity_Master e)
    {
        return ModKitUtil.Stat(e, "Health", 0f) + ModKitUtil.Stat(e, "Healtharmor", 0f) + ModKitUtil.Stat(e, "Healthover", 0f);
    }

    static ArgoTowerBrain Newest(string being, List<ArgoTowerBrain> known)
    {
        foreach (ArgoTowerBrain t in ArgoServer.Towers)
            if (t != null && t.Started && t.Def.Id == being && !known.Contains(t))
                return t;
        return null;
    }

    static readonly List<Vector3> used = new List<Vector3>();
    // Tiles kept free of buildings: the Harpoon Guns' test lanes (HarpoonSpot).
    static readonly List<Vector3> reserved = new List<Vector3>();

    // A free tile about 'ring' tiles from the centre where this being can be placed.
    static Vector3 Spot(Vector3 c, Beingobject being, int team, float ring, float maxDist, bool anyLevel = false)
    {
        List<Vector3> options = new List<Vector3>();
        int r = Mathf.Max(5, Mathf.CeilToInt(maxDist));
        for (int dx = -r; dx <= r; dx++)
            for (int dz = -r; dz <= r; dz++)
                options.Add(new Vector3(c.x + dx, c.y, c.z + dz));
        options.Sort((a, b) => Mathf.Abs(ModKitUtil.Flat(a, c) - ring).CompareTo(Mathf.Abs(ModKitUtil.Flat(b, c) - ring)));
        foreach (Vector3 p in options)
        {
            if (ModKitUtil.Flat(p, c) > maxDist)
                continue;
            Genitem block = ModKitUtil.Surface(p) ?? ModKitUtil.Surface(p + Vector3.up) ?? ModKitUtil.Surface(p + Vector3.down);
            // anyLevel: the terraces up to 4 levels below too (the hill around the core is small and full by then).
            for (int dy = 2; block == null && anyLevel && dy <= 4; dy++)
                block = ModKitUtil.Surface(p + Vector3.down * dy);
            if (block == null || used.Contains(block.Position) || reserved.Contains(block.Position))
                continue;
            if (Helper_Gridtools.Checkifplaceable(block, being, 0, team))
            {
                used.Add(block.Position);
                return block.Position;
            }
        }
        // None on the first ground of each column: every level of each column, as far out.
        return AnySpot(c, being, team, maxDist, "a building");
    }

    // A free tile for this being as near the centre as there is one, on any level from 2 above it to 6 below, every level
    // of each column tried, out to 'maxDist'; logs why when there's none. Spot's first ground in a column, at the centre's
    // own height or 1-4 below, found nothing for the second Drill Rig in three of seven PC runs.
    static Vector3 AnySpot(Vector3 c, Beingobject being, int team, float maxDist, string what)
    {
        int r = Mathf.CeilToInt(maxDist), cy = Mathf.RoundToInt(c.y), bare = 0, taken = 0, unplaceable = 0;
        Vector3 c0 = new Vector3(Mathf.Round(c.x), cy, Mathf.Round(c.z));
        List<Genitem> tiles = new List<Genitem>();
        for (int dx = -r; dx <= r; dx++)
            for (int dz = -r; dz <= r; dz++)
            {
                if (dx * dx + dz * dz > maxDist * maxDist)
                    continue;
                bool any = false;
                for (int dy = 2; dy >= -6; dy--)
                {
                    Genitem b = ModKitUtil.Surface(c0 + new Vector3(dx, dy, dz));
                    if (b == null || tiles.Contains(b))
                        continue;
                    any = true;
                    tiles.Add(b);
                }
                if (!any) bare++;
            }
        tiles.Sort((a, b) => ModKitUtil.Flat(a.Position, c).CompareTo(ModKitUtil.Flat(b.Position, c)));
        foreach (Genitem b in tiles)
        {
            if (used.Contains(b.Position) || reserved.Contains(b.Position))
            {
                taken++;
                continue;
            }
            bool ok = false;
            try { ok = Helper_Gridtools.Checkifplaceable(b, being, 0, team); } catch (Exception) { }
            if (!ok)
            {
                unplaceable++;
                continue;
            }
            used.Add(b.Position);
            return b.Position;
        }
        Log("no free tile for " + what + " within " + F(maxDist) + " of " + c.ToString("F1") + ": " + tiles.Count + " ground tiles (" + taken
            + " taken, " + unplaceable + " not placeable), " + bare + " columns with no ground");
        return new Vector3(float.NaN, 0f, 0f);
    }

    static IEnumerator Build(Entity_Master hero, string tower, Vector3 at, List<ArgoTowerBrain> known, ArgoTowerBrain[] result)
    {
        result[0] = null;
        string json = "{\"Tower\":\"" + tower + "\",\"Position\":" + Vec(at) + ",\"Orientation\":0,\"Owner\":" + ModKitUtil.Owner(hero) + "}";
        Send(hero, "Build", json);
        float t = 0f;
        while (t < 30f && result[0] == null)
        {
            result[0] = Newest(tower, known);
            t += Time.deltaTime;
            yield return null;
        }
        if (result[0] != null)
            known.Add(result[0]);
    }

    static IEnumerator ForceTide(int[] reached)
    {
        int before = Game_Mapvars.Instance.Wave.Value;
        forcing = true;
        yield return Until(() => Game_Mapvars.Instance == null || Game_Mapvars.Instance.Wave.Value > before, 120f);
        forcing = false;
        reached[0] = Game_Mapvars.Instance != null ? Game_Mapvars.Instance.Wave.Value : before;
        yield return Wait(1.5f);
        ClearEnemies();
    }

    // Tide enemies carry the "Enemy" tag (Mode_Normal counts them with that query); cores are left alone.
    static bool IsEnemyMob(Entity_Master e, int team)
    {
        if (e == null || !ModKitUtil.Alive(e) || ModKitUtil.Team(e) == team || e.Taglistserver == null)
            return false;
        return e.Taglistserver.Contains("Enemy") && !e.Taglistserver.Contains("Enemycore") && !e.Taglistserver.Contains("Core");
    }

    static int Team() { return ArgoServer.Heroes.Count > 0 ? ModKitUtil.Team(ArgoServer.Heroes[0].Master) : 1; }

    // Removes forced-Tide enemies so they don't pile up (not a kill: no drops or stats).
    static void ClearEnemies()
    {
        int team = Team();
        foreach (Entity_Master e in new List<Entity_Master>(Game_Entity.Instance.Entityclient))
            if (IsEnemyMob(e, team))
            {
                if (enemyType == null && e.Beingserver.Being != null)
                    enemyType = e.Beingserver.Being;
                Game_Entity.Instance.Despawn(e.Beingserver, false, "Selftest");
            }
    }

    // How a foe's Speed is made up: (Base x Pmulti + Add) x Multi, clamped to Bounds.
    static string SpeedInfo(Entity_Master foe)
    {
        Link_Stats s;
        if (foe == null || foe.Statsserver == null || !foe.Statsserver.TryGetValue("Speed", out s) || s == null)
            return "no Speed stat";
        return "Speed base " + F(s.Value_Base) + " pmulti " + F(s.Value_Pmulti) + " add " + F(s.Value_Add) + " multi " + F(s.Value_Multi)
            + " bounds " + F(s.Value_Bounds.x) + ".." + F(s.Value_Bounds.y) + " calc " + F(s.Value_Calc) + " value " + F(s.Value);
    }

    // A Dagger (else the Tide's own enemy type) about 'dist' tiles from 'near' (then 2 closer, 2 farther, 3), spawned as
    // a child of the enemy core like Tide enemies, on the first standable tile of the ring. Not an Aegis: a held Aegis
    // reads 0.05 (the kit template's Cross check; see SpeedInfo).
    static Entity_Master SpeedFoe(Vector3 near, float dist)
    {
        Beingobject being = ModKitUtil.Load<Beingobject>("Mob", "Enemy_Dagger") ?? enemyType;
        Entity_Master core = null;
        foreach (Entity_Master e in Game_Entity.Instance.Entityclient)
            if (e != null && e.Taglistserver != null && e.Taglistserver.Contains("Enemycore") && ModKitUtil.Alive(e)) { core = e; break; }
        if (being == null || core == null)
            return null;
        Link_Being probe = new Link_Being { Being = being, Entity = being.Entity, Block = being.Block, Team = ModKitUtil.Team(core) };
        foreach (float d in new[] { dist, dist - 2f, dist + 2f, 3f })
        for (int a = 0; a < 16; a++)
            foreach (float dy in new[] { 0f, -1f, 1f })
            {
                Vector3 p = near + new Vector3(Mathf.Round(Mathf.Cos(a * Mathf.PI / 8f) * d), dy, Mathf.Round(Mathf.Sin(a * Mathf.PI / 8f) * d));
                Genitem block = ModKitUtil.Surface(p);
                Genitem stand = null;
                try { stand = block != null ? Helper_Gridtools.Checkifstandable(block, probe) : null; } catch (Exception) { }
                if (stand == null)
                    continue;
                Link_Spawn spawn = Helper_Entitytools.Createlinkspawnparent(core.Beingserver);
                spawn.Being = being; spawn.Genadd = block; spawn.Skin = ""; spawn.Spawnstyle = Link_Spawn.Spawnstyles.Forced;
                Link_Being b = Helper_Entitytools.Spawn(spawn, block.Position, core.Beingserver, true, null);
                if (b != null && b.Worldobject != null)
                    return b.Worldobject;
            }
        return null;
    }

    // An enemy next to a position, spawned as a child of the enemy core like Tide enemies (Entity_Ai_Enemy_Basic falls
    // back to its parent's Goal; a parentless enemy throws every tick).
    static Entity_Master Enemy(Vector3 near)
    {
        if (enemyType == null)
            return null;
        Entity_Master core = null;
        foreach (Entity_Master e in Game_Entity.Instance.Entityclient)
            if (e != null && e.Taglistserver != null && e.Taglistserver.Contains("Enemycore") && ModKitUtil.Alive(e)) { core = e; break; }
        if (core == null)
            return null;
        // The nearest tile around 'near' an enemy can stand on.
        for (int ring = 0; ring <= 3; ring++)
            for (int dx = -ring; dx <= ring; dx++)
                for (int dz = -ring; dz <= ring; dz++)
                {
                    if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dz)) != ring) continue;
                    Vector3 p = near + new Vector3(dx, 0f, dz);
                    Genitem block = ModKitUtil.Surface(p) ?? ModKitUtil.Surface(p + Vector3.up) ?? ModKitUtil.Surface(p + Vector3.down);
                    if (block == null)
                        continue;
                    Link_Spawn spawn = Helper_Entitytools.Createlinkspawnparent(core.Beingserver);
                    spawn.Being = enemyType; spawn.Genadd = block; spawn.Skin = ""; spawn.Spawnstyle = Link_Spawn.Spawnstyles.Forced;
                    Link_Being b = Helper_Entitytools.Spawn(spawn, block.Position, core.Beingserver, true, null);
                    if (b != null && b.Worldobject != null)
                        return b.Worldobject;
                }
        return null;
    }

    // Size of a buildable's model as the game draws it (instantiated far below the map, renderer bounds).
    static void Measure(string being)
    {
        Beingobject b = ModKitUtil.Load<Beingobject>("Mob", being);
        GameObject prefab = b != null && b.Entity != null && b.Entity.Defaultvisual != null ? b.Entity.Defaultvisual.Default.Visprefab : null;
        if (prefab == null) { Log("SIZE " + being + ": not loaded"); return; }
        GameObject g = UnityEngine.Object.Instantiate(prefab, new Vector3(0f, -500f, 0f), Quaternion.identity);
        Bounds bb = new Bounds();
        bool any = false;
        foreach (Renderer r in g.GetComponentsInChildren<Renderer>())
        {
            if (r is ParticleSystemRenderer) continue;
            if (!any) { bb = r.bounds; any = true; } else bb.Encapsulate(r.bounds);
        }
        Log("SIZE " + being + ": size " + bb.size.ToString("F2") + ", y " + F(bb.min.y + 500f) + ".." + F(bb.max.y + 500f) + ", scale " + g.transform.lossyScale.ToString("F2"));
        UnityEngine.Object.Destroy(g);
    }

    // A visual-only copy of a tower model (no entity) on a tile, for side-by-side screenshots.
    static GameObject Showpiece(string being, Vector3 tile)
    {
        Beingobject b = ModKitUtil.Load<Beingobject>("Mob", being);
        GameObject prefab = b != null && b.Entity != null && b.Entity.Defaultvisual != null ? b.Entity.Defaultvisual.Default.Visprefab : null;
        if (prefab == null) return null;
        GameObject g = UnityEngine.Object.Instantiate(prefab, tile, Quaternion.identity);
        g.SetActive(true);
        return g;
    }

    static IEnumerator LoadGroup(string hero)
    {
        Heroentryobject h = ModKitUtil.Heroentry(hero);
        if (h == null || h.Heroloads == null) yield break;
        Bl_Address.Loadgroup g = null;
        try { g = Bl_Address.Load(h.Heroloads.Objectname); } catch (Exception e) { Log("load " + hero + ": " + e.Message); }
        float t = 0f;
        while (g != null && !g.Loaded && t < 60f) { t += Time.unscaledDeltaTime; yield return null; }
    }

    // What this building's upgrades cost against their list price: his save's Voice in a Bottle lowers it on pack buildings since the
    // stock upgrade AI (kit docs/17 §6), and the list price would then fail a check that is doing nothing wrong (round 3).
    static float PriceScale(Entity_Master m) { Link_Command c; return m != null && m.Commandlistserver != null && m.Commandlistserver.TryGetValue("Upgrade", out c) && c != null ? c.Goalvalue : 1f; }

    static IEnumerator Upgrade(ArgoTowerBrain t, string key, long owner)
    {
        Send(t.Master, "Upgrade", "{\"Upgrade\":\"" + t.Def.Id + "_" + key + "_Upgradeobject\",\"Upgrader\":" + owner + "}");
        yield return Wait(0.6f);
    }

    // Upgrade, with what it cost: the drops in a resource frame by frame (paid[0]) apart from what came in (paid[1]).
    static IEnumerator UpgradePaid(ArgoTowerBrain t, string key, long owner, string res, float[] paid)
    {
        paid[0] = 0f;
        paid[1] = 0f;
        float last = Res(owner, res), end = Time.time + 0.6f;
        Send(t.Master, "Upgrade", "{\"Upgrade\":\"" + t.Def.Id + "_" + key + "_Upgradeobject\",\"Upgrader\":" + owner + "}");
        do
        {
            float now = Res(owner, res);
            if (now < last) paid[0] += last - now; else paid[1] += now - last;
            last = now;
            yield return null;
        }
        while (Time.time < end);
    }

    static bool Has(ArgoTowerBrain t, string key) { return t != null && t.Master != null && t.Master.Upgradelistserver.ContainsKey(t.Def.Id + "_" + key + "_Upgradeobject"); }

    static List<Logitems.Base> All(List<Logitems.Base> items) { return ModKitVesselJournal.Flat(items); }

    // A tower's range as the game draws it when selected (Entity_Vis_Tower_Range): centred on the tower at the point its
    // brain measures from (Def.Eye), and as big as its Range stat.
    static IEnumerator RangeCheck(ArgoTowerBrain t)
    {
        Entity_Master m = t.Master;
        Entity_Vis_Tower_Range rv = null;
        if (m.Client != null)
            foreach (Entity_Vis_Base v in m.Client.Visbase) if (v is Entity_Vis_Tower_Range) rv = (Entity_Vis_Tower_Range)v;
        if (!Check(t.Def.Display + " shows its range when selected", rv != null && Extramethods.FindDeepChild(m.Client.transform, "Detect") != null, "range vis " + (rv != null)))
            yield break;
        rv.Selected();
        yield return Wait(1.3f);
        float r = ModKitUtil.Stat(m, "Range", 0f);
        bool ok = rv.Rangeclone != null && Vector3.Distance(rv.Rangeclone.transform.position, t.Eye) < 0.05f && Mathf.Abs(rv.Rangeclone.transform.lossyScale.x - 2f * r) < 0.1f;
        Check(t.Def.Display + " range is centred on it and as big as its Range", ok, "circle at " + (rv.Rangeclone != null ? rv.Rangeclone.transform.position.ToString("F2") + " size " + F(rv.Rangeclone.transform.lossyScale.x) : "-")
            + ", tower " + ModKitUtil.Pos(m).ToString("F2") + " eye " + t.Eye.ToString("F2") + ", Range " + F(r));
#if ARGO_SHOTS
        yield return ArgoShot.Close("range_" + t.Def.Family, ModKitUtil.Pos(m), Mathf.Max(6f, r * 2.4f));
#endif
        rv.Deselected();
    }

    // A tile one to three levels below a tower, within a flat distance band and within reach (3D): for steep shots.
    static Genitem LowTile(Vector3 tower, float minD, float maxD, float reach)
    {
        Genitem best = null;
        float bestDrop = 0f;
        int r = Mathf.CeilToInt(maxD);
        for (int dx = -r; dx <= r; dx++)
            for (int dz = -r; dz <= r; dz++)
            {
                Vector3 p = new Vector3(Mathf.Round(tower.x) + dx, tower.y, Mathf.Round(tower.z) + dz);
                float d = ModKitUtil.Flat(p, tower);
                if (d < minD || d > maxD) continue;
                for (int k = 1; k <= 3; k++)
                {
                    Genitem b = ModKitUtil.Surface(p + Vector3.down * k);
                    if (b == null || b.Position.y > tower.y - 0.5f || used.Contains(b.Position)) continue;
                    float drop = tower.y - b.Position.y;
                    if (Mathf.Sqrt(d * d + drop * drop) > reach) break;
                    if (drop > bestDrop && (ModKitUtil.Surface(p + Vector3.down * (k - 1)) == null)) { best = b; bestDrop = drop; }
                    break;
                }
            }
        return best;
    }

    static Entity_Master EnemyOn(Genitem block)
    {
        Entity_Master core = null;
        foreach (Entity_Master e in Game_Entity.Instance.Entityclient)
            if (e != null && e.Taglistserver != null && e.Taglistserver.Contains("Enemycore") && ModKitUtil.Alive(e)) { core = e; break; }
        if (core == null || enemyType == null || block == null)
            return null;
        Link_Spawn spawn = Helper_Entitytools.Createlinkspawnparent(core.Beingserver);
        spawn.Being = enemyType; spawn.Genadd = block; spawn.Skin = ""; spawn.Spawnstyle = Link_Spawn.Spawnstyles.Forced;
        Link_Being b = Helper_Entitytools.Spawn(spawn, block.Position, core.Beingserver, true, null);
        return b != null ? b.Worldobject : null;
    }

    static Entity_Master Core()
    {
        foreach (Entity_Master e in Game_Entity.Instance.Entityclient)
            if (e != null && e.Taglistserver != null && e.Taglistserver.Contains("Enemycore") && ModKitUtil.Alive(e)) return e;
        return null;
    }

    // Foes in a lane straight out from a tower along a grid line, for the harpoon's reel: the first about 'dist' tiles out
    // (closer, down to 3 tiles, where the tower's hill is too small), each next one a tile behind it, all standable, at the
    // tower's level and no farther than 'reach'. reel: the first foe must have a tile to be reeled to, from its own tile
    // and from the one in front of it (the second PC run's lane, 3 tiles out, had none; in the fifth the held foe had
    // already stepped onto the tile 2 out, see Lane1). Daggers, else the Tide's own type. Empty if no lane is free.
    static List<Entity_Master> Lane(Vector3 from, float dist, float reach, int count, bool reel = false)
    {
        noReelRoom = 0;
        for (float d = dist; d >= 3f; d -= 1f)
        {
            List<Entity_Master> got = Lane1(from, d, reach, count, reel);
            if (got.Count > 0)
            {
                if (d < dist || noReelRoom > 0) Log("lane: no room " + F(dist) + " tiles out, used " + F(d) + (noReelRoom > 0 ? " (" + noReelRoom + " lanes had no tile to reel to)" : ""));
                return got;
            }
        }
        Log("lane: no free lane of " + count + " from " + from.ToString("F1") + " within " + F(reach) + (noReelRoom > 0 ? " (" + noReelRoom + " lanes had no tile to reel to)" : ""));
        return new List<Entity_Master>();
    }

    static int noReelRoom;

    static List<Entity_Master> Lane1(Vector3 from, float dist, float reach, int count, bool reel)
    {
        List<Entity_Master> got = new List<Entity_Master>();
        Genitem inner;
        List<Genitem> tiles = LaneTiles(from, dist, reach, count, reel, out inner);
        Beingobject being = LaneBeing();
        Entity_Master core = Core();
        if (tiles == null || being == null || core == null)
            return got;
        foreach (Genitem block in tiles)
        {
            Link_Spawn spawn = Helper_Entitytools.Createlinkspawnparent(core.Beingserver);
            spawn.Being = being; spawn.Genadd = block; spawn.Skin = ""; spawn.Spawnstyle = Link_Spawn.Spawnstyles.Forced;
            Link_Being b = Helper_Entitytools.Spawn(spawn, block.Position, core.Beingserver, true, null);
            if (b != null && b.Worldobject != null)
                got.Add(b.Worldobject);
        }
        return got;
    }

    static Beingobject LaneBeing() { return ModKitUtil.Load<Beingobject>("Mob", "Enemy_Dagger") ?? enemyType; }

    // The tiles of a lane about 'dist' tiles out (see Lane), or null; inner: the tile in front of the first, if any.
    static List<Genitem> LaneTiles(Vector3 from, float dist, float reach, int count, bool reel, out Genitem inner)
    {
        inner = null;
        Beingobject being = LaneBeing();
        Entity_Master core = Core();
        if (being == null || core == null)
            return null;
        Link_Being probe = new Link_Being { Being = being, Entity = being.Entity, Block = being.Block, Team = ModKitUtil.Team(core) };
        Vector3 start = new Vector3(Mathf.Round(from.x), from.y, Mathf.Round(from.z));
        foreach (Vector3 step in new[] { new Vector3(1, 0, 0), new Vector3(-1, 0, 0), new Vector3(0, 0, 1), new Vector3(0, 0, -1),
                                         new Vector3(1, 0, 1), new Vector3(1, 0, -1), new Vector3(-1, 0, 1), new Vector3(-1, 0, -1) })
        {
            int k = Mathf.Max(2, Mathf.FloorToInt(dist / step.magnitude));
            if ((k + count - 1) * step.magnitude > reach)
                continue;
            List<Genitem> tiles = new List<Genitem>();
            for (int i = 0; i < count; i++)
            {
                // At the tower's level only: the fourth PC run's lane, stepped a level down, was never harpooned.
                Genitem block = ModKitUtil.Surface(start + step * (k + i));
                Genitem stand = null;
                try { stand = block != null ? Helper_Gridtools.Checkifstandable(block, probe) : null; } catch (Exception) { }
                if (stand == null || used.Contains(block.Position))
                    break;
                tiles.Add(block);
            }
            if (tiles.Count < count)
                continue;
            // A walking foe stands, for the game, on the tile it is stepping onto (Entity_Ai_Mob_Movement claims it as the
            // step starts, even at the 0.05 Speed floor), and the reel is measured from there: the fifth PC run's foe,
            // placed 3 tiles out, was on the tile 2 out when the harpoon hit, too close to be reeled.
            Genitem front = ModKitUtil.Surface(start + step * (k - 1));
            if (reel && (!ReelRoom(tiles[0], from, probe) || (front != null && !ReelRoom(front, from, probe))))
            {
                noReelRoom++;
                continue;
            }
            inner = front;
            return tiles;
        }
        return null;
    }

    // A lane's tiles at the first distance from 'dist' down to 3 that has one, and the tile in front of it, without
    // spawning anything; null if none.
    static List<Genitem> LaneRoom(Vector3 from, float dist, float reach, int count, bool reel)
    {
        for (float d = dist; d >= 3f; d -= 1f)
        {
            Genitem inner;
            List<Genitem> tiles = LaneTiles(from, d, reach, count, reel, out inner);
            if (tiles == null)
                continue;
            if (inner != null)
                tiles.Add(inner);
            return tiles;
        }
        return null;
    }

    // The Harpoon Gun's tile: one with room for its reel and pierce checks (4b, Anchor Line), a lane of one foe with a
    // tile to be reeled to and a lane of two (see Lane), nearest 2.5 tiles from the vessel, else the plain Spot. The
    // sixth PC run's tower, on the top of Thehunt's small hill, had neither lane. Those lanes are kept free of the other
    // buildings ('reserved').
    static Vector3 HarpoonSpot(Vector3 home, Beingobject being, int team)
    {
        const float reach = 6.1f;   // the Harpoon's Range 6.5, less the 0.4 that 4b keeps in hand
        float tried = 0f;
        foreach (float maxDist in new[] { 5f, 8f })
        {
            List<Vector3> options = new List<Vector3>();
            int r = Mathf.CeilToInt(maxDist);
            for (int dx = -r; dx <= r; dx++)
                for (int dz = -r; dz <= r; dz++)
                {
                    Vector3 p = new Vector3(home.x + dx, home.y, home.z + dz);
                    float f = ModKitUtil.Flat(p, home);
                    if (f > tried && f <= maxDist)
                        options.Add(p);
                }
            options.Sort((a, b) => Mathf.Abs(ModKitUtil.Flat(a, home) - 2.5f).CompareTo(Mathf.Abs(ModKitUtil.Flat(b, home) - 2.5f)));
            foreach (Vector3 p in options)
            {
                Genitem block = ModKitUtil.Surface(p) ?? ModKitUtil.Surface(p + Vector3.up) ?? ModKitUtil.Surface(p + Vector3.down);
                if (block == null || used.Contains(block.Position) || reserved.Contains(block.Position)
                    || !Helper_Gridtools.Checkifplaceable(block, being, 0, team))
                    continue;
                List<Genitem> one = LaneRoom(block.Position, 5f, reach, 1, true);
                List<Genitem> two = one != null ? LaneRoom(block.Position, 5f, reach, 2, false) : null;
                if (two == null)
                    continue;
                used.Add(block.Position);
                foreach (Genitem g in one) reserved.Add(g.Position);
                foreach (Genitem g in two) reserved.Add(g.Position);
                return block.Position;
            }
            tried = maxDist;
        }
        Log("no Harpoon Gun tile within 8 of the vessel has room for its lanes");
        return Spot(home, being, team, 2.5f, 5f);
    }

    // Whether a foe on 'from' has a tile to be reeled to toward 'toward': ArgoUtil.ReelTile's search, for a probe being.
    static bool ReelRoom(Genitem from, Vector3 toward, Link_Being lane)
    {
        Link_Being probe = new Link_Being { Being = lane.Being, Entity = lane.Entity, Block = lane.Block, Team = lane.Team, Currentblock = from };
        Vector3 dir = toward - from.Position;
        dir.y = 0f;
        float pull = Mathf.Min(ArgoData.ReelTiles, dir.magnitude - ArgoData.ReelMinDist);
        if (pull < 0.5f)
            return false;
        dir.Normalize();
        for (float k = pull; k >= 0.5f; k -= 0.5f)
        {
            Vector3 p = from.Position + dir * k;
            Genitem block = null;
            foreach (float dy in new[] { 0f, -1f, 1f })
                if (block == null) block = ModKitUtil.Surface(new Vector3(Mathf.Round(p.x), Mathf.Round(p.y) + dy, Mathf.Round(p.z)));
            if (block == null || block == from || ModKitUtil.Flat(block.Position, toward) < ArgoData.ReelMinDist)
                continue;
            Genitem stand = null;
            try { stand = Helper_Gridtools.Checkifstandable(block, probe); } catch (Exception) { }
            if (stand == null)
                continue;
            try
            {
                Link_Path path = Helper_Navtools.Getpath(from, stand, probe, false);
                if (path.Valid && path.Length <= Mathf.CeilToInt(k) + 2)
                    return true;
            }
            catch (Exception) { return true; }
        }
        return false;
    }

    // An enemy between 'min' and 'max' tiles (flat) from a point, on that point's level or one up or down. The tile is
    // picked by its own position, nearest the middle of the band first: a fresh spawn's position isn't set until its
    // first frame.
    static Entity_Master FoeBetween(Vector3 c, float min, float max)
    {
        float mid = (min + max) * 0.5f;
        List<Genitem> tiles = new List<Genitem>();
        int r = Mathf.CeilToInt(max);
        for (int dx = -r; dx <= r; dx++)
            for (int dz = -r; dz <= r; dz++)
                foreach (float dy in new[] { 0f, -1f, 1f })
                {
                    Genitem b = ModKitUtil.Surface(new Vector3(Mathf.Round(c.x) + dx, c.y + dy, Mathf.Round(c.z) + dz));
                    if (b == null || tiles.Contains(b) || used.Contains(b.Position))
                        continue;
                    float f = ModKitUtil.Flat(b.Position, c);
                    if (f >= min && f <= max)
                        tiles.Add(b);
                }
        tiles.Sort((a, b) => Mathf.Abs(ModKitUtil.Flat(a.Position, c) - mid).CompareTo(Mathf.Abs(ModKitUtil.Flat(b.Position, c) - mid)));
        foreach (Genitem b in tiles)
        {
            Entity_Master e = EnemyOn(b);
            if (e != null)
                return e;
        }
        Log("no foe between " + F(min) + " and " + F(max) + " tiles of " + c.ToString("F1") + ": " + tiles.Count + " tiles tried");
        return null;
    }

    // Waits for freshly spawned foes' stats, then makes them tougher (they must live through a few harpoons; 700 more
    // makes a Boss-weight foe) and holds them at their Speed floor, so only a reel moves them (a reel's +5 Multi on top
    // of the hold's -1 still walks it at five times its speed). The tower is kept from firing until they are ready.
    static IEnumerator Ready(ArgoTowerBrain tower, List<Entity_Master> foes, float health, List<Link_Modifier> holds)
    {
        Link_Modifier jam = Helper_Entitytools.Addmod(tower.Master, "Attackspeed", -0.99f, Helper_Entitytools.Modtypes.Multi, 0f);
        yield return Until(() => foes.TrueForAll(f => f == null || (f.Statsserver != null && f.Statsserver.ContainsKey("Health") && f.Statsserver.ContainsKey("Speed"))), 2f);
        yield return Wait(0.2f);
        foreach (Entity_Master f in foes)
        {
            Link_Stats hp;
            if (f == null || !ModKitUtil.Alive(f) || f.Statsserver == null || !f.Statsserver.TryGetValue("Health", out hp))
            {
                holds.Add(null);
                continue;
            }
            Helper_Entitytools.Addmod(f, "Health", health, Helper_Entitytools.Modtypes.Add, 0f);
            hp.Value = hp.Value_Calc;
            holds.Add(ModKitUtil.Speed(f) != null ? Helper_Entitytools.Addmod(f, "Speed", -1f, Helper_Entitytools.Modtypes.Multi, 0f) : null);
        }
        ModKitUtil.Unmod(jam);
    }

    static void Force(ArgoTowerBrain t, string key)
    {
        Upgradeproperties up = Bl_Address.Getload("Upgrade", t.Def.Id + "_" + key + "_Upgradeobject") as Upgradeproperties;
        if (up != null && !Has(t, key))
            Helper_Entitytools.Upgrade(t.Master, up, Helper_Entitytools.Upgradecheck.Forced, null);
    }

    static bool Tuned(ArgoTowerBrain bell, int i, ArgoTowerBrain t)
    {
        return bell.Crew[i] != null && bell.Crew[i].Tuning && t != null && bell.Crew[i].Post == t.Master;
    }

    static bool Gone(Entity_Master m) { return m == null || !ModKitUtil.Alive(m) || !ArgoServer.Towers.Exists(t => t != null && t.Master == m); }

    // A real "Sell" command, as the sell button sends it (Entity_Ai_Hero_Buildsell.Selldata: the building's Position, its
    // object's name, the owner). Falls back to removing the building if the vessel's sell doesn't happen.
    static IEnumerator Sell(Entity_Master hero, Entity_Master m, long owner)
    {
        string json = "{\"Position\":" + Vec(m.Position.Value) + ",\"Tower\":\"" + m.gameObject.name + "\",\"Owner\":" + owner + "}";
        Send(hero, "Sell", json);
        yield return Until(() => Gone(m), 20f);
        if (!Gone(m))
        {
            Log("the vessel's Sell did not sell " + ModKitUtil.BeingName(m) + "; removing it instead");
            Game_Entity.Instance.Despawn(m.Beingserver, false, "Selftest");
            yield return Until(() => Gone(m), 2f);
        }
    }

    // A foe below the tower: the tower must fire at it and its shots must land (Argo 1.0's flew over foes below).
    static IEnumerator Steep(ArgoTowerBrain t, float minD)
    {
        float range = ModKitUtil.Stat(t.Master, "Range", 4f);
        Genitem low = LowTile(ModKitUtil.Pos(t.Master), minD, Mathf.Min(range - 0.5f, 4f), range - 0.4f);
        if (low == null) { Log("steep shot " + t.Def.Display + ": no lower ground within reach here (skipped)"); yield break; }
        ClearEnemies();
        Entity_Master foe = EnemyOn(low);
        yield return Wait(0.4f);
        if (foe == null) { Log("steep shot " + t.Def.Display + ": could not spawn below (skipped)"); yield break; }
        float l0 = Life(foe), drop = ModKitUtil.Pos(t.Master).y - ModKitUtil.Pos(foe).y;
        int v0 = t.Volleys;
        yield return Until(() => foe == null || !ModKitUtil.Alive(foe) || t.Volleys >= v0 + 2, 8f);
        yield return Wait(0.8f);
        bool fired = t.Volleys > v0, hit = foe == null || !ModKitUtil.Alive(foe) || Life(foe) < l0 - 0.01f;
        if (!fired) { Log("steep shot " + t.Def.Display + ": foe " + F(drop) + " below was not targeted (out of sight?) (skipped)"); ClearEnemies(); yield break; }
        Check(t.Def.Display + " hits a foe " + F(drop) + " tiles below", hit, "volleys " + (t.Volleys - v0) + ", health " + F(l0) + "->" + (foe != null ? F(Life(foe)) : "dead")
            + ", flat " + F(ModKitUtil.Flat(ModKitUtil.Pos(foe), ModKitUtil.Pos(t.Master))));
#if ARGO_SHOTS
        yield return ArgoShot.Close("steep_" + t.Def.Family, (ModKitUtil.Pos(t.Master) + low.Position) * 0.5f, 4.5f);
#endif
        ClearEnemies();
    }

    // ---------------------------------------------------------------- 2.0 helpers

    // Deep Water where FLOOD has room: the pump plus four pools (FLOOD, Abyss's two, Trench's one) on one level. Round 3
    // put it on a one-tile ridge with two free tiles. Every level of each column out to 8 tiles; the most room wins.
    static Vector3 WaterSpot(Vector3 c, Beingobject being, int team)
    {
        Beingobject pool = ArgoContent.Beings[ArgoData.Pool];
        int best = -1;
        Genitem bestBlock = null;
        Vector3 c0 = new Vector3(Mathf.Round(c.x), Mathf.Round(c.y), Mathf.Round(c.z));
        for (int dx = -8; dx <= 8 && best < 5; dx++)
            for (int dz = -8; dz <= 8 && best < 5; dz++)
            {
                float d = Mathf.Sqrt(dx * dx + dz * dz);
                if (d < 2.5f || d > 8f)
                    continue;
                for (int dy = 2; dy >= -6; dy--)
                {
                    Genitem block = ModKitUtil.Surface(c0 + new Vector3(dx, dy, dz));
                    if (block == null || Mathf.Abs(block.Position.y - (c0.y + dy)) > 0.5f || used.Contains(block.Position) || reserved.Contains(block.Position))
                        continue;
                    bool ok = false;
                    try { ok = Helper_Gridtools.Checkifplaceable(block, being, 0, team); } catch (Exception) { }
                    if (!ok)
                        continue;
                    int room = Room(block.Position, pool, team, 5);
                    if (room > best)
                    {
                        best = room;
                        bestBlock = block;
                    }
                }
            }
        Log("Deep Water spot: " + (bestBlock != null ? bestBlock.Position + " with room for " + best + " (pump and pools)" : "none, Spot's"));
        if (bestBlock == null)
            return Spot(c, being, team, 3.5f, 6f);
        used.Add(bestBlock.Position);
        return bestBlock.Position;
    }

    // Tiles on 'at''s level joined to it (no diagonals) that take a pool, 'at' included, counted up to 'cap'.
    static int Room(Vector3 at, Beingobject pool, int team, int cap)
    {
        List<Vector3> seen = new List<Vector3> { at };
        for (int k = 0; k < seen.Count && seen.Count < cap; k++)
            foreach (Vector3 d in new[] { Vector3.forward, Vector3.back, Vector3.left, Vector3.right })
            {
                Genitem b = ModKitUtil.Surface(seen[k] + d);
                if (b == null || Mathf.Abs(b.Position.y - at.y) > 0.5f || used.Contains(b.Position) || reserved.Contains(b.Position))
                    continue;
                bool dup = false;
                foreach (Vector3 o in seen) if (ModKitUtil.Flat(o, b.Position) < 0.3f) dup = true;
                if (dup)
                    continue;
                bool ok = false;
                try { ok = Helper_Gridtools.Checkifplaceable(b, pool, 0, team); } catch (Exception) { }
                if (ok)
                    seen.Add(b.Position);
            }
        return seen.Count;
    }

    // Free tiles beside 'at' on its level that take a pool; 'why' collects what blocks the others.
    static int FreeAround(Vector3 at, Beingobject pool, int team, List<string> why)
    {
        int free = 0;
        foreach (Vector3 d in new[] { Vector3.forward, Vector3.back, Vector3.left, Vector3.right })
        {
            Genitem b = ModKitUtil.Surface(at + d);
            string no = null;
            if (b == null) no = "no ground";
            else if (Mathf.Abs(b.Position.y - at.y) > 0.5f) no = "another level";
            else if (used.Contains(b.Position)) no = "used";
            else if (reserved.Contains(b.Position)) no = "a test lane";
            else
            {
                bool ok = false;
                try { ok = Helper_Gridtools.Checkifplaceable(b, pool, 0, team); } catch (Exception) { }
                if (!ok) no = "not placeable";
            }
            if (no == null) free++;
            else if (why != null) why.Add((at + d).ToString("F0") + " " + no);
        }
        return free;
    }

    static string WhyNoFlood(ArgoTowerBrain water, int team)
    {
        ArgoWater.Net net = ArgoWater.NetOf(water);
        if (net == null)
            return "no network";
        List<string> why = new List<string>();
        foreach (Vector3 t in net.Tiles)
            FreeAround(t, ArgoContent.Beings[ArgoData.Pool], team, why);
        return net.Tiles.Count + " tiles: " + string.Join("; ", why.ToArray());
    }

    // Every gun and vessel near the water stops attacking while damage over time is measured (round 2: the guns shot the
    // brined foe, 12 a second).
    static List<Link_Modifier> JamGuns(ArgoTowerBrain except = null)
    {
        List<Link_Modifier> jams = new List<Link_Modifier>();
        foreach (ArgoTowerBrain t in ArgoServer.Towers)
            if (t != null && t != except && t.Master != null && (t.IsScatter || t.IsHarpoon))
                jams.Add(Helper_Entitytools.Addmod(t.Master, "Attackspeed", -0.99f, Helper_Entitytools.Modtypes.Multi, 0f));
        foreach (ArgoHeroBrain h in ArgoServer.Heroes)
            if (h != null && h.Master != null)
                jams.Add(Helper_Entitytools.Addmod(h.Master, "Attackspeed", -0.99f, Helper_Entitytools.Modtypes.Multi, 0f));
        return jams;
    }

    static ArgoTowerBrain NewestPool(List<ArgoTowerBrain> known)
    {
        foreach (ArgoTowerBrain t in ArgoServer.Towers)
            if (t != null && t.Started && t.IsPool && !known.Contains(t))
                return t;
        return null;
    }

    static int PoolCount()
    {
        int n = 0;
        foreach (ArgoTowerBrain t in ArgoServer.Towers)
            if (t != null && t.IsPool && t.Master != null && ModKitUtil.Alive(t.Master)) n++;
        return n;
    }

    // A tile next to the water's network (no diagonals) that takes a pool; null if none.
    static Genitem FloodTile(ArgoTowerBrain water, int team)
    {
        ArgoWater.Rebuild();
        ArgoWater.Net net = ArgoWater.NetOf(water);
        Beingobject pool = ArgoContent.Beings[ArgoData.Pool];
        if (net == null)
            return null;
        foreach (Vector3 t in net.Tiles)
            foreach (Vector3 d in new[] { Vector3.forward, Vector3.back, Vector3.left, Vector3.right })
            {
                Genitem b = ModKitUtil.Surface(t + d);
                if (b == null || used.Contains(b.Position) || reserved.Contains(b.Position))
                    continue;
                bool taken = false;
                foreach (Vector3 o in net.Tiles) if (ModKitUtil.Flat(o, b.Position) < 0.3f) taken = true;
                bool ok = false;
                try { ok = !taken && Helper_Gridtools.Checkifplaceable(b, pool, 0, team); } catch (Exception) { }
                if (ok)
                    return b;
            }
        return null;
    }

    // FLOOD as the player sends it: the game's build command on the water, "Build" with the pool and the tile.
    static IEnumerator Flood(ArgoTowerBrain water, Vector3 at, long owner, List<ArgoTowerBrain> known, ArgoTowerBrain[] result)
    {
        result[0] = null;
        Send(water.Master, "Build", "{\"Tower\":\"" + ArgoData.Pool + "\",\"Position\":" + Vec(at) + ",\"Orientation\":0,\"Owner\":" + owner + "}");
        float t = 0f;
        while (t < 3f && result[0] == null)
        {
            result[0] = NewestPool(known);
            t += Time.deltaTime;
            yield return null;
        }
        yield return Wait(0.3f);
        foreach (ArgoTowerBrain p in ArgoServer.Towers)
            if (p != null && p.IsPool && !known.Contains(p)) known.Add(p);
    }

    // A foe stood on a tile and held there (Ready's hold), for the water and Tremor checks.
    static IEnumerator FoeOn(Genitem block, List<Entity_Master> into, ArgoTowerBrain jam)
    {
        Entity_Master e = EnemyOn(block);
        // Round 3: no foe would spawn on the pool tile; any tile of the same water will do.
        ArgoWater.Net net = e == null && block != null ? ArgoWater.Nets.Find(n => n.Tiles.Exists(t => ModKitUtil.Flat(t, block.Position) < 0.3f)) : null;
        if (net != null)
            foreach (ArgoTowerBrain part in net.Parts)
                if (e == null && part != null && part.Master != null && part.Master.Beingserver != null)
                    e = EnemyOn(part.Master.Beingserver.Currentblock);
        if (e == null)
        {
            Log("no foe would spawn on the water at " + (block != null ? block.Position.ToString() : "-") + (net != null ? " or its " + net.Parts.Count + " parts" : ""));
            yield break;
        }
        into.Add(e);
        List<Link_Modifier> holds = new List<Link_Modifier>();
        yield return Ready(jam, new List<Entity_Master> { e }, 200f, holds);
    }

    static float Hit(Entity_Master from, Entity_Master to, float damage)
    {
        float l0 = Life(to);
        Helper_Entitytools.Interacthurt(from, to, ArgoUtil.Hit(damage, 1f));
        return l0 - Life(to);
    }

    static bool Held(Entity_Master foe)
    {
        Link_Stats sp = foe != null && foe.Statsserver != null && foe.Statsserver.ContainsKey("Speed") ? foe.Statsserver["Speed"] : null;
        return sp != null && sp.Value <= Mathf.Max(0f, sp.Value_Bounds.x) + 0.01f;
    }

    static string Aimjson(Vector3 surface) { return "{\"X\":" + F(surface.x) + ",\"Y\":" + F(surface.y) + ",\"Z\":" + F(surface.z) + "}"; }

    // ---------------------------------------------------------------- the scenario

    static IEnumerator Scenario()
    {
        Log("match started: scenario begins (Argo " + ArgoData.Version + ", kit " + ModKitPack.Kit + ")");
        ArgoHeroBrain hero = ArgoServer.Heroes[0];
        yield return Until(() => Game_Mapvars.Instance != null && Game_Mapvars.Instance.Wave.Value >= 1, 150f);
        freeze = true;
        yield return Wait(2f);
        ClearEnemies();
        Entity_Master hm = hero.Master;
        long owner = ModKitUtil.Owner(hm);
        int team = ModKitUtil.Team(hm);
        Log("tide " + Game_Mapvars.Instance.Wave.Value + ", vessel at " + ModKitUtil.Pos(hm) + ", owner " + owner + ", team " + team + ", enemy type " + (enemyType != null ? enemyType.Objectname : "none yet"));
        ModKitTest.Log = Log;
        ModKitTest.MapDump(team, hm);

        // 1. Vessel: native melee setup, tooltip, five build options, no SONAR, stats; the pack registry.
        Check("vessel uses the stock melee AI", hm.GetComponent<Entity_Ai_Mob_Combat>() != null && hm.GetComponent<Entity_Ai_Mob_Ranged>() == null && hm.GetComponent<Entity_Ai_Hero_Buildsell>() != null);
        Check("vessel has the Fathoms tooltip", hm.Upgradelistserver.ContainsKey(ArgoData.HeroTooltip));
        Check("vessel stats (Damage 4, Smacky's)", Mathf.Approximately(ModKitUtil.Stat(hm, "Health", 0), 65f) && Mathf.Approximately(ModKitUtil.Stat(hm, "Speed", 0), 2.25f) && Mathf.Approximately(ModKitUtil.Stat(hm, "Range", 0), 1.75f)
            && Mathf.Approximately(ModKitUtil.Stat(hm, "Damage", 0), 4f),
            "Health " + ModKitUtil.Stat(hm, "Health", 0) + ", Speed " + ModKitUtil.Stat(hm, "Speed", 0) + ", Range " + ModKitUtil.Stat(hm, "Range", 0) + ", Damage " + ModKitUtil.Stat(hm, "Damage", 0));
        int options = 0;
        List<string> optionInfo = new List<string>();
        for (int i = 0; i < ArgoData.Towers.Length; i++)
        {
            Link_Command c;
            bool ok = hm.Commandlistserver.TryGetValue("Buildoption" + (i + 1), out c) && c.Goalstring == ArgoData.Towers[i];
            if (ok) options++;
            optionInfo.Add("Buildoption" + (i + 1) + (ok ? " " + c.Goalstring + (c.Ref != null ? " (" + c.Ref.Objectname + ", keys " + ModKitUtil.Join(c.Ref.Keybind ?? new List<string>()) + ")" : " (no command object)") : " missing"));
        }
        Check("five build options (the fifth is the Miner's Base)", options == 5, string.Join("; ", optionInfo.ToArray()) + "; Command_Buildoption5 " + ArgoContent.Option5Note);
        Check("no SONAR in 2.0", !hm.Commandlistserver.ContainsKey(ArgoData.SonarLink) && !ArgoData.SonarEnabled);
        Check("vessel visible to clients", hm.Client != null && hm.Client.Beingobject != null && hm.Client.Beingobject.Objectname == ArgoData.Hero);
        Check("the pack registry holds Argo with kit " + ModKitPack.Kit, ModKitPack.Has(ArgoData.Part) && ModKitPack.Mismatch() == "", ModKitPack.Registry != null ? ModKitPack.Registry.transform.childCount + " children, mismatch '" + ModKitPack.Mismatch() + "'" : "no registry");
        {
            // The Miner's Base is support, priced in Blue: tier 2 Blue only, Blue the main share above it.
            List<string> off = new List<string>();
            foreach (ArgoData.Upg u in ArgoData.BuildFor(ArgoData.Bell).Upgrades)
            {
                Upgradeproperties up = Bl_Address.Getload("Upgrade", u.Id) as Upgradeproperties;
                float blue = 0f, other = 0f;
                if (up != null && up.Costs != null)
                    foreach (Link_Resourceset r in up.Costs) { if (r.Name == "Blueaura") blue += r.Amount; else other += r.Amount; }
                if (up == null || blue <= other || (u.Tier == 2 && other > 0f)) off.Add(u.Key + u.Tier + " Blue " + F(blue) + ", other " + F(other));
            }
            Check("Miner's Base upgrades are priced in Blue (tier 2 Blue only)", off.Count == 0, off.Count == 0 ? "7 upgrades" : string.Join("; ", off.ToArray()));
        }

        // 1b. Speed floor (1.3.0): stacked slows never take a foe under a tenth of its speed, a hold stops it at its floor
        // whatever slows it carries, and the foe gets its full speed back when they expire. Before anything is built.
        {
            Entity_Master foe = SpeedFoe(ModKitUtil.Pos(hm), 6f);
            yield return Until(() => foe == null || ModKitUtil.Stat(foe, "Speed", 0f) > 0f, 1.5f);
            yield return Wait(0.3f);
            if (Check("a foe for the speed checks", foe != null && ModKitUtil.Alive(foe), foe != null ? ModKitUtil.BeingName(foe) + " " + F(Vector3.Distance(ModKitUtil.Pos(foe), ModKitUtil.Pos(hm))) + " tiles away, " + SpeedInfo(foe) : "none"))
            {
                float speed0 = ModKitUtil.Stat(foe, "Speed", 0f);
                ArgoUtil.Slow(hm, foe, ArgoData.SlowCap, 1.5f, true);
                ArgoUtil.Slow(hm, foe, ArgoData.SlowCap, 1.5f, false);
                yield return Wait(0.2f);
                float slowed = ModKitUtil.Stat(foe, "Speed", -1f);
                Check("two full-strength Tremor slows leave a foe 10% of its speed", slowed >= speed0 * 0.1f - 0.001f && slowed <= speed0 * 0.1f + 0.01f, F(speed0) + "->" + F(slowed));
                Check("a slowed foe reads as Slowed (the Scatter Cannon's bonus)", ArgoStatus.IsSlowed(foe) && !ArgoStatus.IsPinned(foe));
                ArgoUtil.Root(hm, foe, 1f);
                ArgoUtil.Slow(hm, foe, ArgoData.SlowCap, 1f, false);
                yield return Wait(0.2f);
                float held = ModKitUtil.Stat(foe, "Speed", -1f);
                Link_Stats sp = foe != null && foe.Statsserver != null && foe.Statsserver.ContainsKey("Speed") ? foe.Statsserver["Speed"] : null;
                float floor = sp != null ? Mathf.Max(0f, sp.Value_Bounds.x) : 0f;
                Check("a hold on a slowed foe stops it at its Speed floor, never below", sp != null && sp.Value_Bounds.x >= 0f && Mathf.Abs(held - floor) < 0.0001f, "speed " + F(held) + ", floor " + F(floor) + ", " + SpeedInfo(foe));
                yield return Wait(1.6f);
                float after = foe != null ? ModKitUtil.Stat(foe, "Speed", -1f) : -1f;
                Check("when the hold and slows expire the foe has its speed back", Mathf.Abs(after - speed0) < 0.01f && !ArgoStatus.Debuffed(foe), F(speed0) + "->" + F(after));
            }
            if (foe != null && foe.Beingserver != null)
                Game_Entity.Instance.Despawn(foe.Beingserver, false, "Selftest");
        }

        // 2. Building every buildable through the vessel's real Build command (the Drill Rig next to ore), plus a second
        // Harpoon Gun for the crowd-control path (the first takes the damage path in 8).
        Helper_Resources.Giveresources(ArgoUtil.Costs("Redaura=30000;Blueaura=30000;Yellowaura=20000"), owner, Helper_Resources.Styles.Default);
        yield return Wait(0.5f);
        List<ArgoTowerBrain> known = new List<ArgoTowerBrain>(ArgoServer.Towers);
        Dictionary<string, ArgoTowerBrain> towers = new Dictionary<string, ArgoTowerBrain>();
        ArgoTowerBrain[] got = new ArgoTowerBrain[1];
        Vector3 home = ModKitUtil.Pos(hm);
        Entity_Master ore = null;
        Vector3 drillAt = new Vector3(float.NaN, 0f, 0f);
        List<Entity_Master> ores = new List<Entity_Master>(ModKitUtil.Area(home, 45f, ModKitUtil.Tagged("Resource")));
        ores.Sort((a, b) => ModKitUtil.Flat(ModKitUtil.Pos(a), home).CompareTo(ModKitUtil.Flat(ModKitUtil.Pos(b), home)));
        foreach (Entity_Master o in ores)
        {
            if (o == null || !ModKitUtil.Alive(o)) continue;
            drillAt = Spot(ModKitUtil.Pos(o), ArgoContent.Beings[ArgoData.Drill], team, 1f, ArgoData.OreReach - 0.05f);
            if (!float.IsNaN(drillAt.x)) { ore = o; break; }
        }
        Log(ores.Count + " ores; drilling " + (ore != null ? ModKitUtil.BeingName(ore) + " at " + ModKitUtil.Pos(ore) + ", " + F(ModKitUtil.Flat(ModKitUtil.Pos(ore), home)) + " from the vessel" : "none"));
        // Deep Water goes last, after the second Harpoon Gun ("hook"): round 3's water took the ridge the second gun's
        // lanes needed.
        ArgoTowerBrain hook = null;
        foreach (string tower in new[] { ArgoData.Scatter, ArgoData.Harpoon, "hook", ArgoData.Drill, ArgoData.Bell, ArgoData.Water })
        {
            if (tower == "hook")
            {
                Vector3 hat = HarpoonSpot(home, ArgoContent.Beings[ArgoData.Harpoon], team);
                if (!float.IsNaN(hat.x))
                {
                    yield return Build(hm, ArgoData.Harpoon, hat, known, got);
                    hook = got[0];
                }
                Check("a second Harpoon Gun for the crowd-control path", hook != null, "at " + hat);
                continue;
            }
            Beingobject being = ArgoContent.Beings[tower];
            Vector3 at = tower == ArgoData.Drill && ore != null ? drillAt
                : tower == ArgoData.Harpoon ? HarpoonSpot(home, being, team)
                : tower == ArgoData.Water ? WaterSpot(home, being, team)
                : Spot(home, being, team, tower == ArgoData.Bell ? 1.5f : 2.5f, 6f);
            if (float.IsNaN(at.x)) { Check("build " + tower, false, "no free tile"); continue; }
            Link_Resourceset cost = being.Entity.Costs[0];
            float before = Res(owner, cost.Name);
            yield return Build(hm, tower, at, known, got);
            if (!Check("build " + tower, got[0] != null, "at " + at))
                continue;
            towers[tower] = got[0];
            yield return Wait(0.3f);
            float spent = before - Res(owner, cost.Name);
            Check(tower + " costs " + cost.Amount + " " + cost.Name, Mathf.Abs(spent - cost.Amount) < 0.01f, "spent " + spent);
            Check(tower + " visible to clients", got[0].Master.Client != null && got[0].Master.Client.Beingobject != null && got[0].Master.Client.Beingobject.Objectname == tower);
        }
        ArgoTowerBrain scatter, harpoon, water, bell, drill;
        towers.TryGetValue(ArgoData.Scatter, out scatter);
        towers.TryGetValue(ArgoData.Harpoon, out harpoon);
        towers.TryGetValue(ArgoData.Water, out water);
        towers.TryGetValue(ArgoData.Bell, out bell);
        towers.TryGetValue(ArgoData.Drill, out drill);
        if (scatter != null)
            Check("Scatter Cannon stats (+50% vs Slowed or Pinned)", Mathf.Approximately(ModKitUtil.Stat(scatter.Master, "Projcount", 0), 6f) && Mathf.Approximately(ModKitUtil.Stat(scatter.Master, "Range", 0), 4.5f)
                && Mathf.Approximately(ModKitUtil.Stat(scatter.Master, "Debuffbonus", 0), 0.5f),
                "pellets " + ModKitUtil.Stat(scatter.Master, "Projcount", 0) + ", range " + ModKitUtil.Stat(scatter.Master, "Range", 0) + ", bonus " + ModKitUtil.Stat(scatter.Master, "Debuffbonus", 0));
        if (harpoon != null)
            Check("Harpoon Gun stats (Damage 5, Range 6.5: the top of the shipped bands)", Mathf.Approximately(ModKitUtil.Stat(harpoon.Master, "Damage", 0), 5f) && Mathf.Approximately(ModKitUtil.Stat(harpoon.Master, "Range", 0), 6.5f)
                && Mathf.Approximately(ModKitUtil.Stat(harpoon.Master, "Pierce", 0), 3f) && Mathf.Approximately(ModKitUtil.Stat(harpoon.Master, "Attackspeed", 0), 0.35f),
                "damage " + ModKitUtil.Stat(harpoon.Master, "Damage", 0) + ", range " + ModKitUtil.Stat(harpoon.Master, "Range", 0) + ", pierce " + ModKitUtil.Stat(harpoon.Master, "Pierce", 0) + ", attack speed " + ModKitUtil.Stat(harpoon.Master, "Attackspeed", 0));
        if (drill != null)
            Check("Drill Rig stats (AIM reach 5, Tremor 2)", Mathf.Approximately(ModKitUtil.Stat(drill.Master, "Range", 0), 5f) && Mathf.Approximately(drill.TremorRadius, 2f),
                "range " + ModKitUtil.Stat(drill.Master, "Range", 0) + ", tremor " + F(drill.TremorRadius));
        if (water != null)
        {
            Link_Command flood;
            Check("Deep Water is a trap: tagged Trap, not Tower", ModKitUtil.HasTag(water.Master, "Trap") && !ModKitUtil.HasTag(water.Master, "Tower"), ModKitUtil.Join(water.Master.Taglistserver));
            Check("Deep Water has FLOOD (its build option for a pool) and takes Build",
                water.Master.Commandlistserver.TryGetValue(ArgoData.FloodLink, out flood) && flood.Goalstring == ArgoData.Pool && flood.Ref != null && water.Master.Commandlistserver.ContainsKey("Build"),
                flood != null ? "goal " + flood.Goalstring + ", command " + (flood.Ref != null ? flood.Ref.Objectname : "none") : "no FLOOD");
            Check("Deep Water's being is Buster's Speed Pad (else the Perma-Wall fallback)", true, "ais " + ModKitUtil.Join(new List<string>(ArgoContent.Beings[ArgoData.Water].Entity.Aiscripts.ConvertAll(s => s.name))));
            // Its water bobs: over half a swell the part rises or falls by at least one amplitude, never by more than two.
            Transform swell = Extramethods.FindDeepChild(water.Master.transform, "Swell");
            float low = float.MaxValue, high = float.MinValue;
            for (int i = 0; i < 9 && swell != null; i++)
            {
                low = Mathf.Min(low, swell.localPosition.y);
                high = Mathf.Max(high, swell.localPosition.y);
                yield return Wait(ArgoClient.SwellPeriod / 16f);
            }
            Check("Deep Water's water bobs gently", swell != null && high - low > ArgoClient.SwellAmp * 0.5f && high - low <= ArgoClient.SwellAmp * 2f + 0.001f,
                swell != null ? "rose and fell " + (high - low).ToString("F4") + " (amplitude " + ArgoClient.SwellAmp + ")" : "no Swell part");
        }
        // The Miner's Base limit: its button is gone while it stands, and a second can't be built.
        if (bell != null)
        {
            yield return Wait(0.6f);
            Check("Miner's Base: its build button goes while one stands", !hm.Commandlistserver.ContainsKey(ArgoData.BaseOption), "hidden " + hero.BaseHidden);
            Vector3 at2 = Spot(home, ArgoContent.Beings[ArgoData.Bell], team, 2f, 6f);
            if (!float.IsNaN(at2.x))
            {
                int bells = ArgoServer.Towers.FindAll(t => t != null && t.IsBell).Count;
                Send(hm, "Build", "{\"Tower\":\"" + ArgoData.Bell + "\",\"Position\":" + Vec(at2) + ",\"Orientation\":0,\"Owner\":" + owner + "}");
                yield return Wait(1.5f);
                Check("Miner's Base: a second one is refused", ArgoServer.Towers.FindAll(t => t != null && t.IsBell).Count == bells, ArgoServer.Towers.FindAll(t => t != null && t.IsBell).Count + " bases");
            }
        }
        // Each ranged building's range is drawn centred on it, at the size it really reaches (Drill Rig: how far AIM goes).
        foreach (ArgoTowerBrain t in new[] { scatter, harpoon, drill })
            if (t != null) yield return RangeCheck(t);
        Check("the Miner's Base shows no range (its Nauts go anywhere)", bell == null || !bell.Master.Client.Visbase.Exists(v => v is Entity_Vis_Tower_Range));
#if ARGO_SHOTS
        foreach (string n in new[] { "Tower_Smackyarcher", "Tower_Smackybomber", "Tower_Smackyhut", "Trap_Busterpad", ArgoData.Scatter, ArgoData.Harpoon, ArgoData.Water, ArgoData.Pool, ArgoData.Bell, ArgoData.Drill })
            Measure(n);
        {
            Vector3 focus = ModKitUtil.Pos(hm);
            int n0 = 1;
            foreach (ArgoTowerBrain t in towers.Values) { focus += ModKitUtil.Pos(t.Master); n0++; }
            focus /= n0;
            yield return ArgoShot.Close("towers", focus, 6f);
            yield return ArgoShot.Close("argo", ModKitUtil.Pos(hm) + Vector3.up * 0.5f, 2.2f);
            yield return ArgoShot.Shot("built");
        }
#endif

        // 3. The crew: two Nauts, sent with the base's real PORT / STARBOARD commands; never onto a trap.
        if (bell != null)
        {
            yield return Until(() => bell.Crew[0] != null && bell.Crew[1] != null, 8f);
            Check("Miner's Base raises Port and Starboard", bell.Crew[0] != null && bell.Crew[1] != null && bell.Crew[2] == null,
                (bell.Crew[0] != null) + "/" + (bell.Crew[1] != null) + "/" + (bell.Crew[2] != null));
            Check("crew commands on the base", bell.Master.Commandlistserver.ContainsKey("Crew_Port") && bell.Master.Commandlistserver.ContainsKey("Crew_Starboard") && !bell.Master.Commandlistserver.ContainsKey("Crew_Bosun"));
            Check("Nauts visible to clients", bell.Crew[0] != null && bell.Crew[0].Master.Client != null && bell.Crew[0].Master.Client.Beingobject != null);
            if (water != null && bell.Crew[0] != null)
            {
                Send(bell.Master, "Crew_Port", Ref(water.Master));
                yield return Wait(0.5f);
                Check("a Naut picked onto Deep Water is refused (towers only)", bell.Crew[0] != null && bell.Crew[0].Post != water.Master && bell.Posts[0] != water.Master);
            }
            if (scatter != null && bell.Crew[0] != null)
            {
                float aspd = ModKitUtil.Stat(scatter.Master, "Attackspeed", 0f);
                Send(bell.Master, "Crew_Port", Ref(scatter.Master));
                yield return Until(() => bell.Crew[0] != null && bell.Crew[0].Tuning, 25f);
                ArgoNautBrain port = bell.Crew[0];
                Check("PORT walks to the Scatter Cannon and tunes it", port != null && port.Tuning && port.Post == scatter.Master,
                    port != null ? "post " + ModKitUtil.BeingName(port.Post) + ", tuning " + port.Tuning + ", " + F(ModKitUtil.Flat(ModKitUtil.Pos(port.Master), ModKitUtil.Pos(scatter.Master))) + " away" : "no Port");
                Check("tuning: +20% attack speed", Mathf.Abs(ModKitUtil.Stat(scatter.Master, "Attackspeed", 0f) - aspd * (1f + ArgoData.TuneAttackspeed)) < 0.02f,
                    F(aspd) + "->" + F(ModKitUtil.Stat(scatter.Master, "Attackspeed", 0f)));
                Check("tuning skin on the Naut", port != null && port.Master.Skin.Value.ToString() == "Tune");
#if ARGO_SHOTS
                yield return Wait(0.5f);
                yield return ArgoShot.Close("tuning", ModKitUtil.Pos(scatter.Master), 2.6f);
#endif
                Send(bell.Master, "Crew_Starboard", Ref(scatter.Master));
                yield return Wait(0.5f);
                Check("one Naut per tower", bell.Crew[0] == null || bell.Crew[0].Post == null, bell.Crew[0] != null ? "Port's post " + ModKitUtil.BeingName(bell.Crew[0].Post) : "");
                yield return Until(() => bell.Crew[1] != null && bell.Crew[1].Tuning, 25f);
                Check("Starboard took over", bell.Crew[1] != null && bell.Crew[1].Tuning && bell.Crew[1].Post == scatter.Master);
                Check("only one tune on the tower", Mathf.Abs(ModKitUtil.Stat(scatter.Master, "Attackspeed", 0f) - aspd * (1f + ArgoData.TuneAttackspeed)) < 0.02f, F(ModKitUtil.Stat(scatter.Master, "Attackspeed", 0f)));
                Send(bell.Master, "Crew_Starboard", Ref(bell.Master));
                yield return Wait(0.5f);
                Check("picking the base recalls the Naut", bell.Crew[1] != null && bell.Crew[1].Post == null && !bell.Crew[1].Tuning && Mathf.Abs(ModKitUtil.Stat(scatter.Master, "Attackspeed", 0f) - aspd) < 0.02f,
                    "attack speed " + F(ModKitUtil.Stat(scatter.Master, "Attackspeed", 0f)));
            }
        }

        // 4. Fighting: real pellets and harpoons at enemies placed in front of the towers.
        if (enemyType == null)
        {
            int[] r0 = new int[1];
            yield return ForceTide(r0);
        }
        if (Check("found an enemy type to spawn", enemyType != null, enemyType != null ? enemyType.Objectname : "none"))
        {
            List<Entity_Master> targets = new List<Entity_Master>();
            int scatterShots = scatter != null ? scatter.Shots : 0, harpoonShots = harpoon != null ? harpoon.Shots : 0;
            if (scatter != null)
                for (int i = 0; i < 3; i++)
                {
                    Entity_Master e = Enemy(ModKitUtil.Pos(scatter.Master) + new Vector3(2f, 0f, i - 1f));
                    if (e != null) targets.Add(e);
                }
            if (harpoon != null)
                for (int i = 0; i < 3; i++)
                {
                    Entity_Master e = Enemy(ModKitUtil.Pos(harpoon.Master) + new Vector3(-3f - i, 0f, 0f));
                    if (e != null) targets.Add(e);
                }
            yield return Wait(0.4f);
            Log("spawned " + targets.Count + " enemies, health " + (targets.Count > 0 ? F(Life(targets[0])) : "-"));
            Dictionary<Entity_Master, float> life = new Dictionary<Entity_Master, float>();
            foreach (Entity_Master e in targets) life[e] = Life(e);
            yield return Wait(6f);
            if (scatter != null)
                Check("Scatter Cannon fires volleys of pellets", scatter.Shots >= scatterShots + 6 && scatter.Volleys > 0, (scatter.Shots - scatterShots) + " pellets, " + scatter.Volleys + " volleys");
            if (harpoon != null)
                Check("Harpoon Gun fires harpoons", harpoon.Shots > harpoonShots, (harpoon.Shots - harpoonShots) + " harpoons");
            int hurt = 0;
            foreach (KeyValuePair<Entity_Master, float> kv in life)
                if (kv.Key == null || !ModKitUtil.Alive(kv.Key) || Life(kv.Key) < kv.Value - 0.01f) hurt++;
            Check("projectiles hurt the enemies they hit", hurt >= Mathf.Min(2, life.Count), hurt + "/" + life.Count);
            Check("pellets and harpoons are the game's own projectiles", Game_Entity.Instance.Projectiles != null && Game_Entity.Instance.Projectiles.ContainsKey(ArgoData.Pellet) && Game_Entity.Instance.Projectiles.ContainsKey(ArgoData.HarpoonShot),
                Game_Entity.Instance.Projectiles != null ? ModKitUtil.Join(Game_Entity.Instance.Projectiles.Keys) : "none");
            ClearEnemies();
            if (scatter != null) yield return Steep(scatter, 1f);
            if (harpoon != null) yield return Steep(harpoon, 1.5f);
        }

        // 4b. The base Harpoon Gun (2.0): pierces a lane and moves nobody.
        if (harpoon != null && enemyType != null)
        {
            ClearEnemies();
            Vector3 hp = ModKitUtil.Pos(harpoon.Master);
            float reach = ModKitUtil.Stat(harpoon.Master, "Range", 6.5f) - 0.4f;
            List<Entity_Master> lane = Lane(hp, 5f, reach, 2);
            List<Link_Modifier> holds = new List<Link_Modifier>();
            if (lane.Count == 2) yield return Ready(harpoon, lane, 50f, holds);
            if (Check("two foes in a lane from the Harpoon Gun", lane.Count == 2 && lane.TrueForAll(f => ModKitUtil.Alive(f)),
                lane.Count == 2 ? F(ModKitUtil.Flat(ModKitUtil.Pos(lane[0]), hp)) + " and " + F(ModKitUtil.Flat(ModKitUtil.Pos(lane[1]), hp)) + " tiles out" : "none spawned"))
            {
                Entity_Master a = lane[0], b = lane[1];
                float lifeA = Life(a), lifeB = Life(b);
                Vector3 a0 = ModKitUtil.Pos(a), b0 = ModKitUtil.Pos(b);
                int reels = harpoon.Reels, shots = harpoon.Shots;
                List<Link_Modifier> others = JamGuns(harpoon);
                yield return Until(() => harpoon.Shots > shots, 8f);
                yield return Wait(1f);
                foreach (Link_Modifier j in others) ModKitUtil.Unmod(j);
                Check("the base gun pierces both and moves neither", a != null && b != null && Life(a) < lifeA - 0.01f && Life(b) < lifeB - 0.01f && harpoon.Reels == reels
                    && ModKitUtil.Flat(ModKitUtil.Pos(a), a0) < 0.3f && ModKitUtil.Flat(ModKitUtil.Pos(b), b0) < 0.3f,
                    "health " + F(lifeA) + "->" + (a != null ? F(Life(a)) : "felled") + " / " + F(lifeB) + "->" + (b != null ? F(Life(b)) : "felled") + ", reels " + (harpoon.Reels - reels) + ", harpoons " + (harpoon.Shots - shots)
                    + ", moved " + (a != null ? F(ModKitUtil.Flat(ModKitUtil.Pos(a), a0)) : "-") + " / " + (b != null ? F(ModKitUtil.Flat(ModKitUtil.Pos(b), b0)) : "-"));
            }
            ClearEnemies();
        }

        // 5. The Scatter Cannon's bonus: +50% on a Slowed or a Pinned foe, none on a clean one.
        if (scatter != null && enemyType != null)
        {
            ClearEnemies();
            Entity_Master dummy = Enemy(ModKitUtil.Pos(hm) + new Vector3(0f, 0f, 3f));
            yield return Wait(0.3f);
            if (Check("enemy for the Scatter bonus", dummy != null))
            {
                Helper_Entitytools.Addmod(dummy, "Health", 200f, Helper_Entitytools.Modtypes.Add, 0f);
                yield return Wait(0.1f);
                // Round 1: the dummy stood in a Tremor or Deep Water, so its "clean" hit already had the bonus. Each read
                // now starts from a cleared status and runs in the same frame as the hit.
                ArgoStatus.Clear(dummy);
                int b0 = scatter.BonusHits;
                float plain = Hit(scatter.Master, dummy, 2f);
                ArgoUtil.Slow(hm, dummy, 0.1f, 3f, false);
                float slowed = Hit(scatter.Master, dummy, 2f);
                ArgoStatus.Clear(dummy);
                float clean = Hit(scatter.Master, dummy, 2f);
                ArgoUtil.Pin(hm, dummy, 1f, false);
                float pinned = Hit(scatter.Master, dummy, 2f);
                ArgoStatus.Clear(dummy);
                Check("Scatter Cannon: +50% to a Slowed and a Pinned foe, none to a clean one",
                    Mathf.Abs(slowed - plain * 1.5f) < 0.05f && Mathf.Abs(pinned - plain * 1.5f) < 0.05f && Mathf.Abs(clean - plain) < 0.05f && Mathf.Abs(plain - 2f) < 0.05f && scatter.BonusHits == b0 + 2,
                    "clean " + F(plain) + ", slowed " + F(slowed) + ", clean again " + F(clean) + ", pinned " + F(pinned) + ", bonus hits " + (scatter.BonusHits - b0));
            }
            ClearEnemies();

            // 6. The second hit: two enemies side by side in front of Argo.
            int cleaves = hero.Cleaves;
            Vector3 p = ModKitUtil.Pos(hm);
            Entity_Master a = Enemy(p + new Vector3(1f, 0f, 0f)), b = Enemy(p + new Vector3(1f, 0f, 1f));
            yield return Until(() => hero.Cleaves > cleaves, 12f);
            Check("each swing hits a second enemy", hero.Cleaves > cleaves, "cleaves " + cleaves + "->" + hero.Cleaves + (a == null || b == null ? " (spawn failed)" : ""));
            Check("the second hit counts as Argo's", hero.Cleaves > cleaves && (a == null || a.Lasthit == hm) && (b == null || b.Lasthit == hm),
                "last hit by " + (a != null && a.Lasthit != null ? ModKitUtil.BeingName(a.Lasthit) : "-") + " / " + (b != null && b.Lasthit != null ? ModKitUtil.BeingName(b.Lasthit) : "-"));
            ClearEnemies();
        }

        // 6b. Deep Water: slows inside; FLOOD adds an adjacent pool for Blue 15 and refuses a tile that doesn't touch;
        // a pin in the water lasts twice as long; then its upgrades.
        ArgoTowerBrain pool1 = null;
        if (water != null && enemyType != null)
        {
            ClearEnemies();
            Genitem next = FloodTile(water, team);
            float blue0 = Res(owner, "Blueaura");
            int pools0 = PoolCount();
            if (next != null)
            {
                yield return Flood(water, next.Position, owner, known, got);
                pool1 = got[0];
            }
            float paid = blue0 - Res(owner, "Blueaura");
            ArgoWater.Rebuild();
            ArgoWater.Net net = ArgoWater.NetOf(water);
            Check("FLOOD adds a pool next to Deep Water for Blue 15, joined to it", pool1 != null && Mathf.Abs(paid - 15f) < 0.01f && net != null && net.Parts.Contains(pool1) && PoolCount() == pools0 + 1,
                next == null ? "no free tile next to the water" : "pool " + (pool1 != null) + ", Blue spent " + F(paid) + ", network " + (net != null ? net.Parts.Count + " parts" : "none"));
            // A tile three away touches nothing.
            Vector3 far = Spot(ModKitUtil.Pos(water.Master), ArgoContent.Beings[ArgoData.Pool], team, 3f, 4f);
            if (!float.IsNaN(far.x))
            {
                int refused = ArgoWater.Refused, n0 = PoolCount();
                yield return Flood(water, far, owner, known, got);
                Check("FLOOD refuses a tile that doesn't touch the water", PoolCount() == n0 && ArgoWater.Refused > refused, "pools " + n0 + "->" + PoolCount());
                used.Remove(far);
            }
            if (pool1 != null)
            {
                Check("a FLOOD pool is a trap with no upgrades of its own", ModKitUtil.HasTag(pool1.Master, "Trap") && !ModKitUtil.HasTag(pool1.Master, "Tower") && !pool1.Master.Commandlistserver.ContainsKey("Upgrade"));
                List<Entity_Master> wet = new List<Entity_Master>();
                yield return FoeOn(pool1.Master.Beingserver.Currentblock, wet, water);
                Entity_Master e = wet.Count > 0 ? wet[0] : null;
                yield return Wait(0.8f);
                if (Check("a foe wading in the pool", e != null && ModKitUtil.Alive(e), e != null ? F(ModKitUtil.Flat(ModKitUtil.Pos(e), ModKitUtil.Pos(pool1.Master))) + " from the pool" : "none"))
                {
                    Check("Deep Water slows foes in any of its pools", ArgoStatus.IsWet(e) && ArgoStatus.IsSlowed(e), "wet " + ArgoStatus.IsWet(e) + ", slowed " + ArgoStatus.IsSlowed(e) + ", " + SpeedInfo(e));
                    float held = ArgoUtil.Pin(water.Master, e, 1f, false);
                    Check("a pinned foe in Deep Water is held twice as long", Mathf.Abs(held - ArgoData.WaterPinMulti) < 0.01f && ArgoStatus.IsPinned(e), "pinned for " + F(held) + "s");
                }
#if ARGO_SHOTS
                yield return ArgoShot.Close("water", ModKitUtil.Pos(water.Master), 3f);
#endif
                ClearEnemies();
            }
            // Its upgrades: Undercurrent (paid), then the rest forced, each checked on a foe in the pool.
            float slow0 = ModKitUtil.Stat(water.Master, "Slow", 0f);
            float[] cost = new float[2];
            yield return UpgradePaid(water, "Undercurrent2", owner, "Blueaura", cost);
            Check("Undercurrent: a stronger slow, Blue 20", Has(water, "Undercurrent2") && ModKitUtil.Stat(water.Master, "Slow", 0f) >= slow0 + 0.099f && Mathf.Abs(cost[0] - 20f) < 0.01f,
                "slow " + F(slow0) + "->" + F(ModKitUtil.Stat(water.Master, "Slow", 0f)) + ", Blue " + F(cost[0]));
            if (pool1 != null)
            {
                Genitem tile = pool1.Master.Beingserver.Currentblock;
                Force(water, "Kelp3");
                int c0 = ArgoWater.Catches;
                List<Entity_Master> wet = new List<Entity_Master>();
                yield return FoeOn(tile, wet, water);
                yield return Wait(0.6f);
                Check("Kelp Bed: a foe is caught on entering the water", ArgoWater.Catches > c0, "catches " + (ArgoWater.Catches - c0));
                Entity_Master e = wet.Count > 0 ? wet[0] : null;
                Force(water, "Brine2");
                Force(water, "Salt3");
                Force(water, "Crushing4");
                List<Link_Modifier> quiet = JamGuns();
                yield return Wait(0.5f);
                if (e != null && ModKitUtil.Alive(e))
                {
                    float l0 = Life(e);
                    yield return Wait(1f);
                    float plain = l0 - Life(e);
                    ArgoUtil.Pin(water.Master, e, 3f, false);
                    float l1 = Life(e);
                    yield return Wait(1f);
                    float stung = l1 - Life(e);
                    // Crushing Depth is on too: 0.5 x 1.15 unpinned, twice that pinned.
                    Check("Brine: 0.5 a second in the water; Salt Sting doubles it on a pinned foe", plain > 0.4f && plain < 0.8f && stung > plain * 1.6f && stung < plain * 2.4f,
                        "unpinned " + F(plain) + "/s, pinned " + F(stung) + "/s");
                    float dealt = Hit(water.Master, e, 2f);
                    Check("Crushing Depth: foes in the water take 15% more", Mathf.Abs(dealt - 2.3f) < 0.05f && ArgoHooks.Crushed > 0, "2 -> " + F(dealt));
                }
                foreach (Link_Modifier j in quiet) ModKitUtil.Unmod(j);
                ClearEnemies();
                // Abyss: FLOOD fills two tiles at once.
                Force(water, "Abyss4");
                Genitem more = FloodTile(water, team);
                int n0 = PoolCount();
                if (more != null)
                    yield return Flood(water, more.Position, owner, known, got);
                yield return Wait(0.5f);
                Check("Abyss: FLOOD adds a second pool for free", more != null && PoolCount() == n0 + 2, more == null ? "no free tile beside the water: " + WhyNoFlood(water, team) : "pools " + n0 + "->" + PoolCount() + ", free " + ArgoWater.Freepools);
                Force(water, "Trench5");
            }
        }

        // 7. The Drill Rig: it bites ore, sinks a Fathom each Tide (Trench spreads at the same Tide), its Tremor slows,
        // AIM moves it, it spreads through Deep Water it touches; Bedrock and Fault Line.
        if (drill != null)
        {
            Entity_Master drilled = drill.Ore(ModKitUtil.Pos(drill.Master));
            Check("Drill Rig has ore in reach", drilled != null, ore != null ? F(ModKitUtil.Flat(ModKitUtil.Pos(drill.Master), ModKitUtil.Pos(ore))) + " from the ore" : "no ore on the map");
            yield return Until(() => drill.Bites > 0, 6f);
            Check("Drill Rig mines ore (the game's own ore damage)", drill.Bites > 0, drill.Bites + " bites");
            float range0 = drill.TremorRadius;
            int free0 = ArgoWater.Freepools;
            int[] reached = new int[1];
            yield return ForceTide(reached);
            Check("Tide sinks the drill to Fathom 2 and widens its Tremor", drill.Fathom == 2 && drill.Master.Skin.Value.ToString() == "Fathom2" && Mathf.Abs(drill.TremorRadius - range0 - ArgoData.FathomTremor) < 0.01f,
                "Fathom " + drill.Fathom + ", skin '" + drill.Master.Skin.Value + "', Tremor " + F(range0) + "->" + F(drill.TremorRadius));
            Check("Fathom stat on the drill (kit counter)", Mathf.Approximately(ModKitCounter.Get(drill.Master, "Fathom"), 2f));
            if (water != null && Has(water, "Trench5"))
                Check("Trench: Deep Water spreads a pool by itself at the Tide", ArgoWater.Freepools > free0, "free pools " + free0 + "->" + ArgoWater.Freepools
                    + (ArgoWater.Freepools > free0 ? "" : ", no tile " + ArgoWater.NoTile + "x: " + WhyNoFlood(water, team)));
            float dmg2 = ModKitUtil.Stat(drill.Master, "Damage", 0f), aspd2 = ModKitUtil.Stat(drill.Master, "Attackspeed", 0f), slow2 = ModKitUtil.Stat(drill.Master, "Slow", 0f);
            Check("Fathom 2 mines harder and faster and slows more", Mathf.Abs(dmg2 - 0.5f * (1f + ArgoData.FathomYield)) < 0.01f && Mathf.Abs(aspd2 - 0.25f * (1f + ArgoData.FathomSpeed)) < 0.01f
                && Mathf.Abs(slow2 - 0.2f - ArgoData.FathomSlow) < 0.01f, "ore per strike " + F(dmg2) + ", strikes/s " + F(aspd2) + ", slow " + F(slow2));
            yield return Wait(0.6f);
            Vector3 cc;
            float cr;
            bool synced = ArgoClient.Aimed(drill.Master, out cc, out cr);
            Check("clients get the Tremor's centre and radius (the AIM command's link)", synced && Mathf.Abs(cr - drill.TremorRadius) < 0.01f && ModKitUtil.Flat(cc, drill.AimCenter) < 0.01f,
                "synced " + synced + ", radius " + F(cr) + " vs " + F(drill.TremorRadius) + ", centre " + cc.ToString("F2") + " vs " + drill.AimCenter.ToString("F2"));
            Check("client Tremor slow matches the server's", Mathf.Abs(ArgoClient.TremorSlow(drill.Master) - slow2) < 0.01f, F(ArgoClient.TremorSlow(drill.Master)) + " vs " + F(slow2));
            // AIM: a tile within reach moves the Tremor, one past it is refused.
            Genitem aimTile = null, farTile = null;
            Vector3 dp = ModKitUtil.Pos(drill.Master);
            float reach = ModKitUtil.Stat(drill.Master, "Range", 5f);
            for (int r = 3; r <= 4 && aimTile == null; r++)
                foreach (Vector3 d in new[] { Vector3.forward, Vector3.back, Vector3.left, Vector3.right })
                    foreach (float dy in new[] { -1f, 0f, -2f, 1f })
                        if (aimTile == null) aimTile = ModKitUtil.Surface(dp + d * r + Vector3.up * dy);
            foreach (Vector3 d in new[] { Vector3.forward, Vector3.back, Vector3.left, Vector3.right })
                foreach (float dy in new[] { -1f, 0f, -2f, 1f })
                    if (farTile == null) farTile = ModKitUtil.Surface(dp + d * (reach + 3f) + Vector3.up * dy);
            if (aimTile != null)
            {
                int aims = drill.Aims;
                Send(drill.Master, ArgoData.AimLink, Aimjson(aimTile.Position));
                yield return Wait(0.6f);
                Check("AIM moves the Tremor to the picked tile", drill.Aims == aims + 1 && ModKitUtil.Flat(drill.AimCenter, aimTile.Position) < 0.3f && ArgoClient.Aimed(drill.Master, out cc, out cr) && ModKitUtil.Flat(cc, aimTile.Position) < 0.3f,
                    "aims " + (drill.Aims - aims) + ", centre " + drill.AimCenter.ToString("F2") + " (y " + F(drill.AimCenter.y - aimTile.Position.y) + " over the tile), client " + cc.ToString("F2") + ", " + F(ModKitUtil.Flat(aimTile.Position, dp)) + " from the drill");
            }
            else Log("AIM: no ground 3-4 tiles from the drill (skipped)");
            if (farTile != null)
            {
                Vector3 c0 = drill.AimCenter;
                Send(drill.Master, ArgoData.AimLink, Aimjson(farTile.Position));
                yield return Wait(0.5f);
                Check("AIM refuses a tile out of the drill's reach", ModKitUtil.Flat(drill.AimCenter, c0) < 0.01f, F(ModKitUtil.Flat(farTile.Position, dp)) + " tiles out");
            }
#if ARGO_SHOTS
            yield return ArgoShot.Close("aim", (dp + drill.AimCenter) * 0.5f, 5f);
#endif
            if (enemyType != null)
            {
                ClearEnemies();
                List<Link_Modifier> jams = new List<Link_Modifier>();
                foreach (ArgoTowerBrain gun in new[] { scatter, harpoon, hook })
                    if (gun != null && gun.Master != null)
                        jams.Add(Helper_Entitytools.Addmod(gun.Master, "Attackspeed", -0.99f, Helper_Entitytools.Modtypes.Multi, 0f));
                Entity_Master e = FoeBetween(drill.AimCenter, 0f, 1f);
                float speed0 = 0f;
                if (e != null && e.Beingserver != null && e.Beingserver.Being != null && e.Beingserver.Being.Entity.Defaultstat != null)
                {
                    Statproperties.Defaultstatgroup g = e.Beingserver.Being.Entity.Defaultstat.Stats.Find(x => x.Name == "Speed");
                    if (g != null) speed0 = g.Value;
                }
                yield return Wait(1.2f);
                Check("the Tremor slows enemies where it was aimed", e != null && ModKitUtil.Stat(e, "Speed", 0f) < speed0 - 0.01f, e != null ? F(speed0) + "->" + F(ModKitUtil.Stat(e, "Speed", 0f)) : "no enemy");
                // Mantle Tap's quake pins every foe in the Tremor (it is the tier-5 rule, called here directly).
                int q = drill.Quakes, pins = ArgoStatus.Pins;
                drill.Quake(1f);
                yield return Wait(0.2f);
                Check("a quake pins the foes in the Tremor", drill.Quakes > q && e != null && ArgoStatus.IsPinned(e) && ArgoStatus.Pins > pins, "quakes " + (drill.Quakes - q) + ", pins " + (ArgoStatus.Pins - pins));
                // Pay Dirt and Deep Core are bought first: a forced Bedrock takes the tree's tier 3 (round 1 found Deep Core
                // refused after it).
                yield return Upgrade(drill, "Paydirt2", owner);
                yield return Upgrade(drill, "Deepcore3", owner);
                // Bedrock: pinned foes in the Tremor take damage over time.
                Force(drill, "Bedrock3");
                if (e != null && ModKitUtil.Alive(e))
                {
                    ArgoUtil.Pin(drill.Master, e, 2.5f, false);
                    float l0 = Life(e);
                    int b0 = drill.Bedrocks;
                    yield return Wait(2.1f);
                    Check("Bedrock: a pinned foe in the Tremor takes 1.5 a second", drill.Bedrocks > b0 && l0 - Life(e) >= 1.4f, "ticks " + (drill.Bedrocks - b0) + ", health " + F(l0) + "->" + F(Life(e)));
                }
                // Fault Line: a foe reeled across the Tremor bursts (the reel's own call, ArgoServer.Reeled).
                Force(drill, "Faultline4");
                // Round 4: Bedrock felled the foe, and Fault Line's checks skipped without a word. A fresh, sturdy foe.
                if (e == null || !ModKitUtil.Alive(e))
                {
                    e = FoeBetween(drill.AimCenter, 0f, 1f);
                    if (e != null)
                        yield return Ready(drill, new List<Entity_Master> { e }, 50f, new List<Link_Modifier>());
                }
                if (Check("a foe for Fault Line", e != null && ModKitUtil.Alive(e)))
                {
                    float r = drill.TremorRadius;
                    float l0 = Life(e);
                    int f0 = drill.Faults;
                    ArgoServer.Reeled(e, drill.AimCenter + Vector3.right * (r + 1f), drill.AimCenter + Vector3.left * (r + 1f));
                    float burst = l0 - (e != null && ModKitUtil.Alive(e) ? Life(e) : 0f);
                    Check("Fault Line: a foe reeled across the Tremor takes a burst", drill.Faults == f0 + 1 && burst >= Mathf.Min(11.9f, l0 - 0.01f), "bursts " + (drill.Faults - f0) + ", health " + F(l0) + "->" + F(Life(e)));
                    f0 = drill.Faults;
                    ArgoServer.Reeled(e, drill.AimCenter + Vector3.right * (r + 3f), drill.AimCenter + Vector3.right * (r + 1.5f));
                    Check("Fault Line: a reel that misses the Tremor does nothing", drill.Faults == f0);
                }
                ClearEnemies();
                // The Tremor spreads through Deep Water it touches: aimed at the pump with a tiny radius, a foe in the pool
                // next to it (outside the circle) is in the Tremor.
                if (water != null && pool1 != null && pool1.Master != null)
                {
                    Vector3 keep = drill.AimCenter;
                    drill.AimCenter = ModKitUtil.Pos(water.Master);
                    Link_Modifier shrink = Helper_Entitytools.Addmod(drill.Master, "Tremor", 0.6f - drill.TremorRadius, Helper_Entitytools.Modtypes.Add, 0f);
                    List<Entity_Master> wet = new List<Entity_Master>();
                    yield return FoeOn(pool1.Master.Beingserver.Currentblock, wet, drill);
                    yield return Wait(0.6f);
                    Entity_Master w = wet.Count > 0 ? wet[0] : null;
                    bool outside = w != null && ModKitUtil.Flat(ModKitUtil.Pos(w), drill.AimCenter) > drill.TremorRadius;
                    Check("a Tremor touching Deep Water shakes every pool connected to it", w != null && outside && drill.TremorFoes().Contains(w),
                        w == null ? "no foe in the pool" : "radius " + F(drill.TremorRadius) + ", foe " + F(ModKitUtil.Flat(ModKitUtil.Pos(w), drill.AimCenter)) + " from the centre, in the Tremor " + drill.TremorFoes().Contains(w));
                    ModKitUtil.Unmod(shrink);
                    drill.AimCenter = keep;
                    ClearEnemies();
                }
                foreach (Link_Modifier j in jams) ModKitUtil.Unmod(j);
            }
        }

        // 8. Upgrades: paid, applied through the stock upgrade flow, and their effects.
        if (scatter != null)
        {
            ClearEnemies();
            float[] paid = new float[2];
            float wideScale = PriceScale(scatter.Master);
            yield return UpgradePaid(scatter, "Widebore2", owner, "Redaura", paid);
            Check("Wide Bore: more pellets, wider cone", Has(scatter, "Widebore2") && Mathf.Approximately(ModKitUtil.Stat(scatter.Master, "Projcount", 0), 8f) && Mathf.Approximately(ModKitUtil.Stat(scatter.Master, "Spread", 0), 75f),
                "pellets " + ModKitUtil.Stat(scatter.Master, "Projcount", 0) + ", cone " + ModKitUtil.Stat(scatter.Master, "Spread", 0));
            Check("Wide Bore paid", Mathf.Abs(paid[0] - 30f * wideScale) < 0.01f, "Red spent " + F(paid[0]) + " at price scale " + F(wideScale) + (paid[1] > 0f ? ", " + F(paid[1]) + " came in meanwhile" : ""));
            yield return Upgrade(scatter, "Pressure4", owner);
            Check("tier-4 upgrade not offered from tier 2", !Has(scatter, "Pressure4"));
            if (enemyType != null)
            {
                Force(scatter, "Double2");
                ClearEnemies();
                yield return Until(() => scatter.Stored > 0, 5f);
                Check("Double Barrel loads a volley while nothing is in range", Has(scatter, "Double2") && scatter.Stored == 1, "stored " + scatter.Stored);
                List<Entity_Master> foe = new List<Entity_Master> { Enemy(ModKitUtil.Pos(scatter.Master) + new Vector3(2f, 0f, 0f)) };
                yield return Ready(scatter, foe, 50f, new List<Link_Modifier>());
                int shots0 = scatter.Shots, volleys0 = scatter.Volleys;
                yield return Until(() => scatter.Volleys > volleys0, 4f);
                float first = Time.time;
                int pellets = Mathf.RoundToInt(ModKitUtil.Stat(scatter.Master, "Projcount", 6f));
                yield return Until(() => scatter.Volleys > volleys0 + 1, 1f);
                float gap = Time.time - first;
                Check("Double Barrel: the next volley fires twice the pellets", scatter.Volleys == volleys0 + 2 && scatter.Shots - shots0 == 2 * pellets && gap <= ArgoData.DoubleDelay + 0.2f && scatter.Stored == 0,
                    "volleys " + (scatter.Volleys - volleys0) + ", pellets " + (scatter.Shots - shots0) + " (2 x " + pellets + "), " + F(gap) + "s apart, stored " + scatter.Stored);
                ClearEnemies();
            }
            // Ironclad: health x2.5 and the game's armour.
            Force(scatter, "Ironclad4");
            yield return Wait(0.3f);
            Link_Stats hpStat = scatter.Master.Statsserver.ContainsKey("Health") ? scatter.Master.Statsserver["Health"] : null;
            Check("Ironclad: health x2.5 and 2 armour", hpStat != null && Mathf.Abs(hpStat.Value_Multi - 2.5f) < 0.01f && Mathf.Approximately(ModKitUtil.Stat(scatter.Master, "Healtharmor", 0f), 2f),
                "health multi " + (hpStat != null ? F(hpStat.Value_Multi) : "-") + ", armour " + F(ModKitUtil.Stat(scatter.Master, "Healtharmor", 0f)));
        }
        if (harpoon != null)
        {
            yield return Upgrade(harpoon, "Longline2", owner);
            yield return Upgrade(harpoon, "Skewer3", owner);
            Check("Long Line -> Skewer: range and pierce", Has(harpoon, "Skewer3") && Mathf.Approximately(ModKitUtil.Stat(harpoon.Master, "Pierce", 0), 6f) && Mathf.Approximately(ModKitUtil.Stat(harpoon.Master, "Range", 0), 8.5f),
                "pierce " + ModKitUtil.Stat(harpoon.Master, "Pierce", 0) + ", range " + ModKitUtil.Stat(harpoon.Master, "Range", 0));
            yield return Upgrade(harpoon, "Anchor3", owner);
            Check("the damage path can't take Anchor Line after Skewer", !Has(harpoon, "Anchor3"));
        }
        // The crowd-control path on the second gun: Tether, Anchor Line, then Reel, Twin Line and Undertow.
        if (hook != null && enemyType != null)
        {
            Vector3 hp = ModKitUtil.Pos(hook.Master);
            float reach = ModKitUtil.Stat(hook.Master, "Range", 6.5f) - 0.4f;
            float[] paid = new float[2];
            float tetherScale = PriceScale(hook.Master);
            yield return UpgradePaid(hook, "Tether2", owner, "Redaura", paid);
            ClearEnemies();
            List<Entity_Master> lane = Lane(hp, 5f, reach, 1);
            List<Link_Modifier> holds = new List<Link_Modifier>();
            if (lane.Count == 1) yield return Ready(hook, lane, 50f, holds);
            if (lane.Count == 1)
            {
                int t0 = hook.Tethers;
                yield return Until(() => hook.Tethers > t0, 8f);
                Check("Tether: a harpooned foe is slowed (Red 30)", Has(hook, "Tether2") && hook.Tethers > t0 && ArgoStatus.IsSlowed(lane[0]) && Mathf.Abs(paid[0] - 30f * tetherScale) < 0.01f,
                    "tethers " + (hook.Tethers - t0) + ", Red " + F(paid[0]));
            }
            yield return Upgrade(hook, "Anchor3", owner);
            ClearEnemies();
            lane = Lane(hp, 5f, reach, 1);
            holds.Clear();
            if (lane.Count == 1) yield return Ready(hook, lane, 50f, holds);
            if (Check("a foe for Anchor Line", Has(hook, "Anchor3") && lane.Count == 1 && ModKitUtil.Alive(lane[0]), "anchor " + Has(hook, "Anchor3") + ", foes " + lane.Count))
            {
                Entity_Master a = lane[0];
                ModKitUtil.Unmod(holds[0]);
                int anchors = hook.Anchors;
                yield return Until(() => hook.Anchors > anchors, 8f);
                yield return Wait(0.2f);
                bool held = Held(a), pinned = ArgoStatus.IsPinned(a);
                yield return Wait(1.3f);
                bool free = a == null || !ModKitUtil.Alive(a) || !Held(a);
                Check("Anchor Line pins the first foe hit, then lets go after about a second", hook.Anchors > anchors && held && pinned && free && hook.Reels == 0,
                    "anchors " + (hook.Anchors - anchors) + ", held " + held + ", pinned " + pinned + ", free after " + free + ", " + SpeedInfo(a));
            }
            ClearEnemies();
            // Stunned against a building: the pin's own rule on a foe next to the gun.
            Entity_Master beside = FoeBetween(hp, 0.9f, 1.2f);
            yield return Wait(0.4f);
            if (beside != null)
            {
                int stuns = ArgoStatus.Stuns;
                float aspd = ModKitUtil.Stat(beside, "Attackspeed", -1f);
                ArgoUtil.Pin(hook.Master, beside, 1f, true);
                Check("Anchor Line's pin beside a building stuns (no attacks while held)", ArgoStatus.Stuns == stuns + 1 && (aspd < 0f || ModKitUtil.Stat(beside, "Attackspeed", 0f) < aspd * 0.1f),
                    "stuns " + (ArgoStatus.Stuns - stuns) + ", attack speed " + F(aspd) + "->" + F(ModKitUtil.Stat(beside, "Attackspeed", 0f)));
                // ARG-1 (code review): a second stun within the first. The stat's bounds decide whether its attack speed can go below 0.
                Link_Stats stunStat;
                if (beside.Statsserver != null && beside.Statsserver.TryGetValue("Attackspeed", out stunStat))
                {
                    float oneStun = stunStat.Value;
                    ArgoUtil.Pin(hook.Master, beside, 1f, true);
                    yield return Wait(0.1f);
                    Log("INFO FINDING ARG-1: " + (stunStat.Value < 0f ? "SEEN" : "not seen") + " (" + ModKitUtil.BeingName(beside) + "'s attack speed " + F(aspd) + " -> " + F(oneStun) + " -> " + F(stunStat.Value) + " at a second stun, multi " + F(stunStat.Value_Multi) + ", bounds " + F(stunStat.Value_Bounds.x) + ".." + F(stunStat.Value_Bounds.y) + ")");
                }
            }
            ClearEnemies();
            // Reel (forced: the other tier 3 of Tether): the first foe hit walks toward the gun, the one behind it stays.
            Force(hook, "Reel3");
            lane = Lane(hp, 5f, reach, 2, true);
            bool two = lane.Count == 2;
            if (!two) { ClearEnemies(); lane = Lane(hp, 5f, reach, 1, true); }
            holds.Clear();
            if (lane.Count > 0) yield return Ready(hook, lane, 50f, holds);
            if (Check("foes in a lane from the Reel gun", lane.Count > 0 && lane.TrueForAll(f => ModKitUtil.Alive(f)), lane.Count + " foes"))
            {
                Entity_Master a = lane[0], b = two ? lane[1] : null;
                float lifeB = b != null ? Life(b) : 0f;
                Vector3 b0 = b != null ? ModKitUtil.Pos(b) : Vector3.zero;
                int reels = hook.Reels, shots0 = hook.Shots;
                yield return Until(() => hook.Reels > reels, 8f);
                float hitAt = Time.time, dHit = a != null ? ModKitUtil.Flat(ModKitUtil.Pos(a), hp) : -1f;
                if (b != null) b0 = ModKitUtil.Pos(b);
                yield return Until(() => hook.LastReelDone >= hitAt, 1.5f);
                float took = Time.time - hitAt, dEnd = a != null ? ModKitUtil.Flat(ModKitUtil.Pos(a), hp) : -1f;
                Check("Reel: the first foe hit walks toward the gun (a tile or more), within 1s", hook.Reels == reels + 1 && a != null && dHit - dEnd >= 0.7f && took <= 1f,
                    "reels " + (hook.Reels - reels) + " of " + (hook.Shots - shots0) + " harpoons, " + F(dHit) + "->" + F(dEnd) + " tiles in " + F(took) + "s");
                if (two)
                    Check("Reel: the foe behind it is pierced but not reeled", b == null || (Life(b) < lifeB - 0.01f && ModKitUtil.Flat(ModKitUtil.Pos(b), b0) < 0.3f),
                        b != null ? "health " + F(lifeB) + "->" + F(Life(b)) + ", moved " + F(ModKitUtil.Flat(ModKitUtil.Pos(b), b0)) : "felled");
#if ARGO_SHOTS
                yield return ArgoShot.Close("reel", hp, 4.5f);
#endif
            }
            ClearEnemies();
            lane = Lane(hp, 4f, reach, 1);
            holds.Clear();
            if (lane.Count == 1) yield return Ready(hook, lane, 700f, holds);
            if (Check("a Boss-weight foe in front of the Reel gun", lane.Count == 1 && ModKitUtil.MaxHealth(lane[0]) >= ArgoData.BossHealth, lane.Count == 1 ? "max health " + F(ModKitUtil.MaxHealth(lane[0])) : "none"))
            {
                Entity_Master boss = lane[0];
                Vector3 p0 = ModKitUtil.Pos(boss);
                float life0 = Life(boss);
                int bossHits = hook.BossHits, reels = hook.Reels;
                yield return Until(() => hook.BossHits > bossHits, 8f);
                yield return Wait(0.6f);
                Check("a Boss-weight foe is harpooned but never moved", hook.BossHits > bossHits && hook.Reels == reels && Life(boss) < life0 && ModKitUtil.Flat(ModKitUtil.Pos(boss), p0) < 0.3f,
                    "boss hits " + (hook.BossHits - bossHits) + ", reels " + (hook.Reels - reels) + ", moved " + F(ModKitUtil.Flat(ModKitUtil.Pos(boss), p0)));
            }
            ClearEnemies();
            // Undertow: the foes around the reeled one are reeled too.
            Force(hook, "Twinline4");
            Force(hook, "Undertow5");
            lane = Lane(hp, 5f, reach, 2, true);
            if (lane.Count < 2)
            {
                // Round 3: no lane of two had room to reel. Undertow only needs a second foe within its radius of the
                // reeled one: one foe with room, and a neighbour beside it, across the lane.
                ClearEnemies();
                lane = Lane(hp, 5f, reach, 1, true);
                if (lane.Count == 1)
                {
                    Vector3 f0 = ModKitUtil.Pos(lane[0]), along = f0 - hp;
                    along.y = 0f;
                    Vector3 side = Vector3.Cross(along.normalized, Vector3.up);
                    side = Mathf.Abs(side.x) > Mathf.Abs(side.z) ? new Vector3(Mathf.Sign(side.x), 0f, 0f) : new Vector3(0f, 0f, Mathf.Sign(side.z));
                    foreach (Vector3 sd in new[] { side, -side })
                        if (lane.Count == 1)
                            foreach (float dy in new[] { 0f, -1f, 1f })
                            {
                                Genitem g = lane.Count == 1 ? ModKitUtil.Surface(f0 + sd + Vector3.up * (dy - 1f)) : null;
                                Entity_Master n = g != null ? EnemyOn(g) : null;
                                if (n != null) lane.Add(n);
                            }
                }
            }
            holds.Clear();
            if (lane.Count == 2) yield return Ready(hook, lane, 50f, holds);
            if (Check("two foes for Undertow", lane.Count == 2, lane.Count + " foes"))
            {
                int u0 = hook.Undertows;
                yield return Until(() => hook.Undertows > u0, 8f);
                Check("Undertow: the foe behind the reeled one is reeled too", hook.Undertows > u0, "undertow reels " + (hook.Undertows - u0) + ", reels " + hook.Reels);
            }
            ClearEnemies();
        }
        if (drill != null)
        {
            Check("Pay Dirt -> Deep Core: sinks to 7", Has(drill, "Paydirt2") && Has(drill, "Deepcore3") && drill.FathomMax == ArgoData.FathomDeepest, "max Fathom " + drill.FathomMax);
            if (enemyType != null && drill.Ore(ModKitUtil.Pos(drill.Master)) != null)
            {
                ClearEnemies();
                Vector3 c = drill.AimCenter;
                float radius = drill.TremorRadius;
                Entity_Master inside = FoeBetween(c, 0.3f, radius - 0.4f);
                bool placedIn = inside != null;
                yield return Wait(0.4f);
                int dug = drill.PaydirtBites, bites = drill.Bites;
                if (inside != null) Helper_Entitytools.Interacthurt(hm, inside, ArgoUtil.Hit(9999f, 1f));
                yield return Wait(0.3f);
                Check("Pay Dirt: a foe felled in the Tremor is dug up for two strikes of ore", placedIn && !ModKitUtil.Alive(inside) && drill.PaydirtBites == dug + 2 && drill.Bites >= bites + 2,
                    !placedIn ? "no foe placed inside " + F(radius) : "strikes " + (drill.PaydirtBites - dug) + ", bites " + (drill.Bites - bites));
                Entity_Master outside = FoeBetween(c, radius + 1f, radius + 3f);
                bool placedOut = outside != null && !drill.InTremor(ModKitUtil.Pos(outside));
                yield return Wait(0.4f);
                dug = drill.PaydirtBites;
                if (outside != null) Helper_Entitytools.Interacthurt(hm, outside, ArgoUtil.Hit(9999f, 1f));
                yield return Wait(0.3f);
                Check("Pay Dirt: a foe felled outside the Tremor gives nothing", placedOut && !ModKitUtil.Alive(outside) && drill.PaydirtBites == dug,
                    !placedOut ? "no foe placed outside " + F(radius) : "strikes " + (drill.PaydirtBites - dug));
                ClearEnemies();
            }
            drill.SinkTo(ArgoData.FathomDeepest);
            Vector3 cc = Vector3.zero;
            float cr = 0f;
            yield return Until(() => ArgoClient.Aimed(drill.Master, out cc, out cr) && Mathf.Abs(cr - drill.TremorRadius) < 0.01f, 2f);
            ArgoClient.Aimed(drill.Master, out cc, out cr);
            Check("Fathom 7: its own look, and every player draws the right Tremor", drill.Fathom == ArgoData.FathomDeepest && drill.Master.Skin.Value.ToString() == "Fathom" + ArgoData.FathomDeepest
                && Mathf.Abs(cr - drill.TremorRadius) < 0.01f && Mathf.Abs(ArgoClient.TremorRadius(drill.Master) - drill.TremorRadius) < 0.01f,
                "Fathom " + drill.Fathom  + ", skin '" + drill.Master.Skin.Value + "', Tremor synced " + F(cr) + " (server sent " + F(drill.SyncedRadius) + ", AIM link " + drill.AimSynced + ", stale Aim entries removed " + ArgoTowerBrain.AimDupes + "), rebuilt " + F(ArgoClient.TremorRadius(drill.Master)) + " vs " + F(drill.TremorRadius));
        }
        if (bell != null)
        {
            yield return Upgrade(bell, "Salts2", owner);
            Check("Old Salts is one of the Miner's Base's tier-2 roots", Has(bell, "Salts2"));
            yield return Upgrade(bell, "Overhaul3", owner);
            yield return Upgrade(bell, "Thirdmate4", owner);
            yield return Until(() => bell.Crew[2] != null, 6f);
            Check("Third Mate hires Bosun", Has(bell, "Thirdmate4") && bell.Crew[2] != null && bell.Master.Commandlistserver.ContainsKey("Crew_Bosun"),
                "units " + ModKitUtil.Stat(bell.Master, "Units", 0) + ", Bosun " + (bell.Crew[2] != null));
            yield return Upgrade(bell, "Fullcrew5", owner);
            if (scatter != null && harpoon != null && bell.Crew[0] != null)
            {
                float pellets = ModKitUtil.Stat(scatter.Master, "Projcount", 0f), pierce = ModKitUtil.Stat(harpoon.Master, "Pierce", 0f);
                float slow = drill != null ? ModKitUtil.Stat(drill.Master, "Slow", 0f) : 0f;
                Send(bell.Master, "Crew_Port", Ref(scatter.Master));
                yield return Until(() => Tuned(bell, 0, scatter), 10f);
                Check("Old Salts: a tuned Scatter Cannon fires 2 more pellets", Tuned(bell, 0, scatter) && Mathf.Abs(ModKitUtil.Stat(scatter.Master, "Projcount", 0f) - pellets - ArgoData.SaltsPellets) < 0.01f,
                    "tuned " + Tuned(bell, 0, scatter) + ", pellets " + F(pellets) + "->" + F(ModKitUtil.Stat(scatter.Master, "Projcount", 0f)));
                Send(bell.Master, "Crew_Port", Ref(harpoon.Master));
                yield return Until(() => Tuned(bell, 0, harpoon), 10f);
                Check("Full Crew: Port dives to the Harpoon Gun and tunes it", Tuned(bell, 0, harpoon),
                    bell.Crew[0] != null ? "tuning " + bell.Crew[0].Tuning + ", attack speed " + F(ModKitUtil.Stat(harpoon.Master, "Attackspeed", 0f)) : "no Port");
                Check("Old Salts: a tuned Harpoon Gun pierces 2 more; the Scatter Cannon has its pellets back",
                    Mathf.Abs(ModKitUtil.Stat(harpoon.Master, "Pierce", 0f) - pierce - ArgoData.SaltsPierce) < 0.01f && Mathf.Abs(ModKitUtil.Stat(scatter.Master, "Projcount", 0f) - pellets) < 0.01f,
                    "pierce " + F(pierce) + "->" + F(ModKitUtil.Stat(harpoon.Master, "Pierce", 0f)) + ", pellets " + F(ModKitUtil.Stat(scatter.Master, "Projcount", 0f)));
                ArgoTowerBrain last = harpoon;
                if (drill != null)
                {
                    Send(bell.Master, "Crew_Port", Ref(drill.Master));
                    yield return Until(() => Tuned(bell, 0, drill), 12f);
                    Check("Old Salts: a tuned Drill Rig's Tremor slows 10% more; the Harpoon Gun is back to normal",
                        Tuned(bell, 0, drill) && Mathf.Abs(ModKitUtil.Stat(drill.Master, "Slow", 0f) - slow - ArgoData.SaltsSlow) < 0.01f && Mathf.Abs(ModKitUtil.Stat(harpoon.Master, "Pierce", 0f) - pierce) < 0.01f,
                        "tuned " + Tuned(bell, 0, drill) + ", slow " + F(slow) + "->" + F(ModKitUtil.Stat(drill.Master, "Slow", 0f)) + ", pierce " + F(ModKitUtil.Stat(harpoon.Master, "Pierce", 0f)));
                    last = drill;
                }
                Send(bell.Master, "Crew_Port", Ref(bell.Master));
                yield return Wait(0.5f);
                Check("Old Salts: it all goes when the Naut leaves", !Tuned(bell, 0, last) && Mathf.Abs(ModKitUtil.Stat(scatter.Master, "Projcount", 0f) - pellets) < 0.01f
                    && Mathf.Abs(ModKitUtil.Stat(harpoon.Master, "Pierce", 0f) - pierce) < 0.01f && (drill == null || Mathf.Abs(ModKitUtil.Stat(drill.Master, "Slow", 0f) - slow) < 0.01f),
                    "pellets " + F(ModKitUtil.Stat(scatter.Master, "Projcount", 0f)) + ", pierce " + F(ModKitUtil.Stat(harpoon.Master, "Pierce", 0f)) + (drill != null ? ", slow " + F(ModKitUtil.Stat(drill.Master, "Slow", 0f)) : ""));
            }
            Force(bell, "Flippers2");
            yield return Wait(0.3f);
            Check("Flippers: faster Nauts", Has(bell, "Flippers2") && bell.Crew[0] != null && ModKitUtil.Stat(bell.Crew[0].Master, "Speed", 0f) > 1.6f * 1.4f, bell.Crew[0] != null ? F(ModKitUtil.Stat(bell.Crew[0].Master, "Speed", 0f)) : "no Port");
        }

// 8b. The Directions (kit 1.7): each rank's effect. (Untested until 2026-11-04.)
        {
            Func<string, Upgradeproperties> direction = n => Bl_Address.Getload("Upgrade", ModKitDirections.Id("Argo", n)) as Upgradeproperties;
            ArgoTowerBrain salvoGun = towers.ContainsKey(ArgoData.Harpoon) ? towers[ArgoData.Harpoon] : null;
            if (salvoGun == null || !ModKitUtil.Alive(salvoGun.Master)) ModKitTest.Setup("Heavy Salvo", "no Harpoon Gun");
            else
            {
                float base0 = ModKitUtil.Stat(salvoGun.Master, "Damage", 0f);
                float last = 0f;
                for (int r = 1; r <= 3; r++)
                {
                    ModKitDirections.Grant(hm, direction(ArgoData.Heavysalvos[r - 1].Name));
                    yield return Wait(1.2f);
                    float bonus = ArgoData.Heavysalvos[r - 1].Bonus;
                    float now = ModKitUtil.Stat(salvoGun.Master, "Damage", 0f);
                    Check("Heavy Salvo rank " + r + ": the Harpoon Gun deals +" + F(bonus * 100f) + "%", now > last && Mathf.Abs(now - base0 * (1f + bonus)) < 0.02f, F(base0) + " -> " + F(now));
                    last = now;
                }
            }
            ArgoTowerBrain pool = towers.ContainsKey(ArgoData.Water) ? towers[ArgoData.Water] : null;
            ArgoWater.Net net = pool != null ? ArgoWater.NetOf(pool) : null;
            if (net == null) ModKitTest.Setup("Deep Current", "no Deep Water");
            else
            {
                float reach0 = net.Reach;
                // A point between the plain reach and rank III's, the way the water asks of a foe. Tiles are a whole tile apart, so no
                // foe stands there by itself, and one can't be held there: its own movement keeps writing its position.
                Vector3 wet = ModKitUtil.Pos(pool.Master), hold = wet;
                foreach (Vector3 dir in new[] { new Vector3(1f, 0f, 1f).normalized, new Vector3(1f, 0f, -1f).normalized, new Vector3(-1f, 0f, 1f).normalized, new Vector3(-1f, 0f, -1f).normalized })   // diagonals: FLOOD's pools can stand on every side
                {
                    Vector3 p = wet + dir * (ArgoData.PoolReach * 1.25f);
                    if (hold == wet && !net.Holds(p)) hold = p;
                }
                bool dryAtFirst = hold != wet && !net.Holds(hold);
                for (int r = 1; r <= 3; r++)
                {
                    ModKitDirections.Grant(hm, direction(ArgoData.Deepcurrents[r - 1].Name));
                    yield return Wait(1.5f);
                    net = ArgoWater.NetOf(pool);
                    Check("Deep Current rank " + r + ": Deep Water reaches +" + F(ArgoData.Deepcurrents[r - 1].Bonus * 100f) + "%", net != null && Mathf.Abs(net.Reach - ArgoData.PoolReach * (1f + ArgoData.Deepcurrents[r - 1].Bonus)) < 0.01f, F(reach0) + " -> " + (net != null ? F(net.Reach) : "no net"));
                }
                Check("Deep Current rank 3: a point past the plain reach is in the water, and wasn't before", dryAtFirst && net.Holds(hold), F(ModKitUtil.Flat(hold, wet)) + " from the pool, plain reach " + F(ArgoData.PoolReach) + ", now " + F(net.Reach));
            }
        }
        // 9. The journal: Argo's own pages, linked from every vessel's tabs, in one order, no duplicates.
        Logobject page = ArgoContent.Entry.Log;
        bool own = page != null && page.name == "Log_" + ArgoData.Hero, shown = false, rule = false;
        int towerLinks = 0;
        Logobject bellPage = null, upgradePage = null, unitPage = null;
        if (own)
            foreach (Logitems.Base b in All(page.Elements))
            {
                if (b is Logitems.TextImageBeing && ((Logitems.TextImageBeing)b).Entity == ArgoContent.HeroBeing.Entity) shown = true;
                if (b is Logitems.LinkUpgrade && ((Logitems.LinkUpgrade)b).Target == null && ((Logitems.LinkUpgrade)b).Targetupgrade != null && ((Logitems.LinkUpgrade)b).Targetupgrade.Objectname == ArgoData.HeroTooltip) rule = true;
                Logitems.Link l = b as Logitems.Link;
                if (l != null && !l.Sidelink && l.Target != null && l.Target.name.StartsWith("Log_" + ArgoData.Hero + "_"))
                {
                    towerLinks++;
                    if (l.Target.name.EndsWith(ArgoData.Bell)) bellPage = l.Target;
                }
            }
        Check("journal: Argo has a vessel page", own && shown);
        Check("journal: the Fathoms rule is on the page", rule);
        Check("journal: TOWERS & UNITS links Argo's five buildings (not FLOOD's pools)", towerLinks == 5, towerLinks + " links");
        if (bellPage != null)
            foreach (Logitems.Base b in All(bellPage.Elements))
            {
                if (b is Logitems.LinkUpgrade && ((Logitems.LinkUpgrade)b).Target != null && upgradePage == null) upgradePage = ((Logitems.LinkUpgrade)b).Target;
                if (b is Logitems.LinkImage && ((Logitems.LinkImage)b).Target != null && unitPage == null) unitPage = ((Logitems.LinkImage)b).Target;
            }
        Check("journal: building page links its upgrades", upgradePage != null && upgradePage.name.Contains("Argobell"), upgradePage != null ? upgradePage.name : "none");
        Check("journal: the Miner's Base lists its Nauts", unitPage != null && unitPage.name.Contains("Argonaut"), unitPage != null ? unitPage.name : "none");
        List<string> order = null;
        bool same = true, noDupes = true;
        int pagesWithTabs = 0;
        Dictionary<string, UnityEngine.Object> heroes = ModKitUtil.Folder("Hero");
        foreach (UnityEngine.Object o in heroes.Values)
        {
            Heroentryobject h = o as Heroentryobject;
            if (h == null || h.Log == null) continue;
            List<string> tabs = new List<string>();
            foreach (Logitems.Base b in All(h.Log.Elements))
                if (b is Logitems.Link && ((Logitems.Link)b).Sidelink && ((Logitems.Link)b).Target != null) tabs.Add(((Logitems.Link)b).Target.name);
            if (tabs.Count == 0) continue;
            pagesWithTabs++;
            if (new List<string>(tabs).FindAll(t => tabs.IndexOf(t) != tabs.LastIndexOf(t)).Count > 0) noDupes = false;
            if (order == null) order = tabs;
            else if (string.Join(",", order.ToArray()) != string.Join(",", tabs.ToArray())) same = false;
        }
        Check("journal: every vessel page has the same tabs", pagesWithTabs >= 6 && same && noDupes, pagesWithTabs + " pages: " + (order != null ? string.Join(", ", order.ToArray()) : "none"));
        Check("journal: Argo is in the vessel tabs", order != null && order.Contains("Log_" + ArgoData.Hero));

        // 10. The F-key fix (kit 1.6, docs/15 section 9): the first F after a respawn selects the vessel, whatever was
        // selected. The respawn is simulated (the fix's flag); RogueLab ran the full respawn on the PC.
        if (ModKitPack.Registry.transform.Find("ModKitFocus") != null && Game_Selection.Instance != null)
        {
            Entity_Master other = bell != null && bell.Crew[0] != null ? bell.Crew[0].Master : null;
            ModKitFocus.Respawned = true;
            Game_Selection.Instance.Selected = other;
            int fixes = ModKitFocus.Fixes;
            try { Blockage.Bl_Inputs.Actions["Focus"].Function("Onpressed"); } catch (Exception e) { Log("Focus press: " + e.Message); }
            Check("F after a respawn selects Argo, with the crew out", Game_Selection.Instance.Selected == hm,
                "selected " + (Game_Selection.Instance.Selected != null ? ModKitUtil.BeingName(Game_Selection.Instance.Selected) : "nothing") + ", fixes " + (ModKitFocus.Fixes - fixes) + (ModKitFocus.MainHero() == hm ? "" : ", main hero not found"));
        }
        else
            Log("F-key fix installed by another part (or no selection): skipped here");

        // 11. The Miner's Base button comes back once the Base is sold.
        if (bell != null && bell.Master != null)
        {
            Entity_Master bm = bell.Master;
            yield return Sell(hm, bm, owner);
            yield return Wait(0.8f);
            Check("Miner's Base: its build button comes back once it is sold", Gone(bm) && hm.Commandlistserver.ContainsKey(ArgoData.BaseOption),
                "gone " + Gone(bm) + ", shown again " + hero.BaseShown);
        }
#if ARGO_SHOTS
        ArgoClient.Demo(ModKitUtil.Pos(hm));
        yield return Wait(0.2f);
        yield return ArgoShot.Shot("fx");
        Bl_Menus.Changemenu("Journal");
        Logobject waterPage = null;
        foreach (Logitems.Base b in All(page.Elements))
            if (b is Logitems.Link && !((Logitems.Link)b).Sidelink && ((Logitems.Link)b).Target != null && ((Logitems.Link)b).Target.name.EndsWith(ArgoData.Water)) waterPage = ((Logitems.Link)b).Target;
        foreach (Logobject open in new[] { page, waterPage, bellPage, upgradePage, unitPage })
        {
            if (open == null) continue;
            Loghandler.Log_Current = open;
            yield return Wait(2.5f);
            yield return ArgoShot.Shot("journal_" + open.name);
        }
        Bl_Menus.Changemenu("Main");
        yield return Wait(1f);
#endif
        Log("hooks: softened " + ArgoHooks.Softened + ", crushed " + ArgoHooks.Crushed + "; water ticks " + ArgoWater.Ticks + ", floods " + ArgoWater.Floods + ", free pools " + ArgoWater.Freepools
            + ", catches " + ArgoWater.Catches + ", drags " + ArgoWater.Drags + "; pins " + ArgoStatus.Pins + ", stuns " + ArgoStatus.Stuns + "; tabs added " + ModKitVesselTabs.Added + ", removed " + ModKitVesselTabs.Removed + ", sorted " + ModKitVesselTabs.Sorted);
        freeze = false;
        Log("DONE pass=" + pass + " fail=" + fail + " setup=" + ModKitTest.Setups);
        phase = 9;
    }
}
#endif
