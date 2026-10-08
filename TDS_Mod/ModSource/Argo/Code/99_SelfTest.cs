using Blockage.Menu;
#if ARGO_SELFTEST
// Debug build only (Build-ArgoMod.ps1 -Define ARGO_SELFTEST). Starts a solo match by itself (Embark, pick Argo, pick a
// map, ready up) because the game ignores injected input, then plays through every Argo mechanic on the host the way a
// player would (real Build, SONAR, crew and Upgrade commands through Command_ServerRpc), forces Tides, and logs one
// "[Argo] TEST PASS|FAIL name: detail" line per check plus a final "TEST DONE" summary in Player.log.
using UnityEngine.SceneManagement;
using Unity.Netcode;

public static class ArgoSelfTest
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
        if (freeze && !forcing && Game_Mapvars.Instance != null && ArgoUtil.IsServer && Game_Mapvars.Instance.Time_Wave.Value < 600f)
            Game_Mapvars.Instance.Time_Wave.Value = 900f;
        // Mode_Normal leaves a Tide entry once Time_Wave <= 0 (or when no enemy is left).
        if (forcing && Game_Mapvars.Instance != null && ArgoUtil.IsServer)
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

        if (phase <= 2 && ArgoServer.Heroes.Count > 0 && Game_Mapvars.Instance != null && ArgoUtil.IsServer)
        {
            phase = 3;
            ArgoMod.Instance.StartCoroutine(Scenario());
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
        return ArgoUtil.Stat(e, "Health", 0f) + ArgoUtil.Stat(e, "Healtharmor", 0f) + ArgoUtil.Stat(e, "Healthover", 0f);
    }

    static ArgoTowerBrain Newest(string being, List<ArgoTowerBrain> known)
    {
        foreach (ArgoTowerBrain t in ArgoServer.Towers)
            if (t != null && t.Started && t.Def.Id == being && !known.Contains(t))
                return t;
        return null;
    }

    static readonly List<Vector3> used = new List<Vector3>();
    // Tiles kept free of buildings: the Harpoon Tower's test lanes (HarpoonSpot).
    static readonly List<Vector3> reserved = new List<Vector3>();

    // A free tile about 'ring' tiles from the centre where this being can be placed.
    static Vector3 Spot(Vector3 c, Beingobject being, int team, float ring, float maxDist, bool anyLevel = false)
    {
        List<Vector3> options = new List<Vector3>();
        int r = Mathf.Max(5, Mathf.CeilToInt(maxDist));
        for (int dx = -r; dx <= r; dx++)
            for (int dz = -r; dz <= r; dz++)
                options.Add(new Vector3(c.x + dx, c.y, c.z + dz));
        options.Sort((a, b) => Mathf.Abs(ArgoUtil.Flat(a, c) - ring).CompareTo(Mathf.Abs(ArgoUtil.Flat(b, c) - ring)));
        foreach (Vector3 p in options)
        {
            if (ArgoUtil.Flat(p, c) > maxDist)
                continue;
            Genitem block = ArgoUtil.Surface(p) ?? ArgoUtil.Surface(p + Vector3.up) ?? ArgoUtil.Surface(p + Vector3.down);
            // anyLevel: the terraces up to 4 levels below too (the hill around the core is small and full by then).
            for (int dy = 2; block == null && anyLevel && dy <= 4; dy++)
                block = ArgoUtil.Surface(p + Vector3.down * dy);
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
    // own height or 1-4 below, found nothing for the second Deep Drill in three of seven PC runs.
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
                    Genitem b = ArgoUtil.Surface(c0 + new Vector3(dx, dy, dz));
                    if (b == null || tiles.Contains(b))
                        continue;
                    any = true;
                    tiles.Add(b);
                }
                if (!any) bare++;
            }
        tiles.Sort((a, b) => ArgoUtil.Flat(a.Position, c).CompareTo(ArgoUtil.Flat(b.Position, c)));
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
        string json = "{\"Tower\":\"" + tower + "\",\"Position\":" + Vec(at) + ",\"Orientation\":0,\"Owner\":" + ArgoUtil.Owner(hero) + "}";
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
        if (e == null || !ArgoUtil.Alive(e) || ArgoUtil.Team(e) == team || e.Taglistserver == null)
            return false;
        return e.Taglistserver.Contains("Enemy") && !e.Taglistserver.Contains("Enemycore") && !e.Taglistserver.Contains("Core");
    }

    static int Team() { return ArgoServer.Heroes.Count > 0 ? ArgoUtil.Team(ArgoServer.Heroes[0].Master) : 1; }

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

    // A Dagger (else the Tide's own enemy type) about 'dist' tiles from 'near' (then 2 closer, 2 farther, 3), spawned as
    // a child of the enemy core like Tide enemies, on the first standable tile of the ring. Not an Aegis: a held Aegis
    // reads 0.05 (Mummer's Cross check; see SpeedInfo).
    // How a foe's Speed is made up: (Base x Pmulti + Add) x Multi, clamped to Bounds.
    static string SpeedInfo(Entity_Master foe)
    {
        Link_Stats s;
        if (foe == null || foe.Statsserver == null || !foe.Statsserver.TryGetValue("Speed", out s) || s == null)
            return "no Speed stat";
        return "Speed base " + F(s.Value_Base) + " pmulti " + F(s.Value_Pmulti) + " add " + F(s.Value_Add) + " multi " + F(s.Value_Multi)
            + " bounds " + F(s.Value_Bounds.x) + ".." + F(s.Value_Bounds.y) + " calc " + F(s.Value_Calc) + " value " + F(s.Value);
    }

    static Entity_Master SpeedFoe(Vector3 near, float dist)
    {
        Beingobject being = ArgoUtil.Load<Beingobject>("Mob", "Enemy_Dagger") ?? enemyType;
        Entity_Master core = null;
        foreach (Entity_Master e in Game_Entity.Instance.Entityclient)
            if (e != null && e.Taglistserver != null && e.Taglistserver.Contains("Enemycore") && ArgoUtil.Alive(e)) { core = e; break; }
        if (being == null || core == null)
            return null;
        Link_Being probe = new Link_Being { Being = being, Entity = being.Entity, Block = being.Block, Team = ArgoUtil.Team(core) };
        foreach (float d in new[] { dist, dist - 2f, dist + 2f, 3f })
        for (int a = 0; a < 16; a++)
            foreach (float dy in new[] { 0f, -1f, 1f })
            {
                Vector3 p = near + new Vector3(Mathf.Round(Mathf.Cos(a * Mathf.PI / 8f) * d), dy, Mathf.Round(Mathf.Sin(a * Mathf.PI / 8f) * d));
                Genitem block = ArgoUtil.Surface(p);
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
            if (e != null && e.Taglistserver != null && e.Taglistserver.Contains("Enemycore") && ArgoUtil.Alive(e)) { core = e; break; }
        if (core == null)
            return null;
        // The nearest tile around 'near' an enemy can stand on.
        for (int ring = 0; ring <= 3; ring++)
            for (int dx = -ring; dx <= ring; dx++)
                for (int dz = -ring; dz <= ring; dz++)
                {
                    if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dz)) != ring) continue;
                    Vector3 p = near + new Vector3(dx, 0f, dz);
                    Genitem block = ArgoUtil.Surface(p) ?? ArgoUtil.Surface(p + Vector3.up) ?? ArgoUtil.Surface(p + Vector3.down);
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
        Beingobject b = ArgoUtil.Load<Beingobject>("Mob", being);
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
        Beingobject b = ArgoUtil.Load<Beingobject>("Mob", being);
        GameObject prefab = b != null && b.Entity != null && b.Entity.Defaultvisual != null ? b.Entity.Defaultvisual.Default.Visprefab : null;
        if (prefab == null) return null;
        GameObject g = UnityEngine.Object.Instantiate(prefab, tile, Quaternion.identity);
        g.SetActive(true);
        return g;
    }

    static IEnumerator LoadGroup(string hero)
    {
        Heroentryobject h = ArgoUtil.Heroentry(hero);
        if (h == null || h.Heroloads == null) yield break;
        Bl_Address.Loadgroup g = null;
        try { g = Bl_Address.Load(h.Heroloads.Objectname); } catch (Exception e) { Log("load " + hero + ": " + e.Message); }
        float t = 0f;
        while (g != null && !g.Loaded && t < 60f) { t += Time.unscaledDeltaTime; yield return null; }
    }

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
        float r = ArgoUtil.Stat(m, "Range", 0f);
        bool ok = rv.Rangeclone != null && Vector3.Distance(rv.Rangeclone.transform.position, t.Eye) < 0.05f && Mathf.Abs(rv.Rangeclone.transform.lossyScale.x - 2f * r) < 0.1f;
        Check(t.Def.Display + " range is centred on it and as big as its Range", ok, "circle at " + (rv.Rangeclone != null ? rv.Rangeclone.transform.position.ToString("F2") + " size " + F(rv.Rangeclone.transform.lossyScale.x) : "-")
            + ", tower " + ArgoUtil.Pos(m).ToString("F2") + " eye " + t.Eye.ToString("F2") + ", Range " + F(r));
#if ARGO_SHOTS
        yield return ArgoShot.Close("range_" + t.Def.Family, ArgoUtil.Pos(m), Mathf.Max(6f, r * 2.4f));
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
                float d = ArgoUtil.Flat(p, tower);
                if (d < minD || d > maxD) continue;
                for (int k = 1; k <= 3; k++)
                {
                    Genitem b = ArgoUtil.Surface(p + Vector3.down * k);
                    if (b == null || b.Position.y > tower.y - 0.5f || used.Contains(b.Position)) continue;
                    float drop = tower.y - b.Position.y;
                    if (Mathf.Sqrt(d * d + drop * drop) > reach) break;
                    if (drop > bestDrop && (ArgoUtil.Surface(p + Vector3.down * (k - 1)) == null)) { best = b; bestDrop = drop; }
                    break;
                }
            }
        return best;
    }

    static Entity_Master EnemyOn(Genitem block)
    {
        Entity_Master core = null;
        foreach (Entity_Master e in Game_Entity.Instance.Entityclient)
            if (e != null && e.Taglistserver != null && e.Taglistserver.Contains("Enemycore") && ArgoUtil.Alive(e)) { core = e; break; }
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
            if (e != null && e.Taglistserver != null && e.Taglistserver.Contains("Enemycore") && ArgoUtil.Alive(e)) return e;
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

    static Beingobject LaneBeing() { return ArgoUtil.Load<Beingobject>("Mob", "Enemy_Dagger") ?? enemyType; }

    // The tiles of a lane about 'dist' tiles out (see Lane), or null; inner: the tile in front of the first, if any.
    static List<Genitem> LaneTiles(Vector3 from, float dist, float reach, int count, bool reel, out Genitem inner)
    {
        inner = null;
        Beingobject being = LaneBeing();
        Entity_Master core = Core();
        if (being == null || core == null)
            return null;
        Link_Being probe = new Link_Being { Being = being, Entity = being.Entity, Block = being.Block, Team = ArgoUtil.Team(core) };
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
                Genitem block = ArgoUtil.Surface(start + step * (k + i));
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
            Genitem front = ArgoUtil.Surface(start + step * (k - 1));
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

    // The Harpoon Tower's tile: one with room for its reel and pierce checks (4b, Anchor Line), a lane of one foe with a
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
                    float f = ArgoUtil.Flat(p, home);
                    if (f > tried && f <= maxDist)
                        options.Add(p);
                }
            options.Sort((a, b) => Mathf.Abs(ArgoUtil.Flat(a, home) - 2.5f).CompareTo(Mathf.Abs(ArgoUtil.Flat(b, home) - 2.5f)));
            foreach (Vector3 p in options)
            {
                Genitem block = ArgoUtil.Surface(p) ?? ArgoUtil.Surface(p + Vector3.up) ?? ArgoUtil.Surface(p + Vector3.down);
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
        Log("no Harpoon Tower tile within 8 of the vessel has room for its lanes");
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
                if (block == null) block = ArgoUtil.Surface(new Vector3(Mathf.Round(p.x), Mathf.Round(p.y) + dy, Mathf.Round(p.z)));
            if (block == null || block == from || ArgoUtil.Flat(block.Position, toward) < ArgoData.ReelMinDist)
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
                    Genitem b = ArgoUtil.Surface(new Vector3(Mathf.Round(c.x) + dx, c.y + dy, Mathf.Round(c.z) + dz));
                    if (b == null || tiles.Contains(b) || used.Contains(b.Position))
                        continue;
                    float f = ArgoUtil.Flat(b.Position, c);
                    if (f >= min && f <= max)
                        tiles.Add(b);
                }
        tiles.Sort((a, b) => Mathf.Abs(ArgoUtil.Flat(a.Position, c) - mid).CompareTo(Mathf.Abs(ArgoUtil.Flat(b.Position, c) - mid)));
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
            if (f == null || !ArgoUtil.Alive(f) || f.Statsserver == null || !f.Statsserver.TryGetValue("Health", out hp))
            {
                holds.Add(null);
                continue;
            }
            Helper_Entitytools.Addmod(f, "Health", health, Helper_Entitytools.Modtypes.Add, 0f);
            hp.Value = hp.Value_Calc;
            holds.Add(ArgoUtil.Speed(f) != null ? Helper_Entitytools.Addmod(f, "Speed", -1f, Helper_Entitytools.Modtypes.Multi, 0f) : null);
        }
        ArgoUtil.Unmod(jam);
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

    static bool Gone(Entity_Master m) { return m == null || !ArgoUtil.Alive(m) || !ArgoServer.Towers.Exists(t => t != null && t.Master == m); }

    // A real "Sell" command, as the sell button sends it (Entity_Ai_Hero_Buildsell.Selldata: the building's Position, its
    // object's name, the owner). Falls back to removing the building if the vessel's sell doesn't happen.
    static IEnumerator Sell(Entity_Master hero, Entity_Master m, long owner)
    {
        string json = "{\"Position\":" + Vec(m.Position.Value) + ",\"Tower\":\"" + m.gameObject.name + "\",\"Owner\":" + owner + "}";
        Send(hero, "Sell", json);
        yield return Until(() => Gone(m), 20f);
        if (!Gone(m))
        {
            Log("the vessel's Sell did not sell " + ArgoUtil.BeingName(m) + "; removing it instead");
            Game_Entity.Instance.Despawn(m.Beingserver, false, "Selftest");
            yield return Until(() => Gone(m), 2f);
        }
    }

    // A foe below the tower: the tower must fire at it and its shots must land (Argo 1.0's flew over foes below).
    static IEnumerator Steep(ArgoTowerBrain t, float minD)
    {
        float range = ArgoUtil.Stat(t.Master, "Range", 4f);
        Genitem low = LowTile(ArgoUtil.Pos(t.Master), minD, Mathf.Min(range - 0.5f, 4f), range - 0.4f);
        if (low == null) { Log("steep shot " + t.Def.Display + ": no lower ground within reach here (skipped)"); yield break; }
        ClearEnemies();
        Entity_Master foe = EnemyOn(low);
        yield return Wait(0.4f);
        if (foe == null) { Log("steep shot " + t.Def.Display + ": could not spawn below (skipped)"); yield break; }
        float l0 = Life(foe), drop = ArgoUtil.Pos(t.Master).y - ArgoUtil.Pos(foe).y;
        int v0 = t.Volleys;
        yield return Until(() => foe == null || !ArgoUtil.Alive(foe) || t.Volleys >= v0 + 2, 8f);
        yield return Wait(0.8f);
        bool fired = t.Volleys > v0, hit = foe == null || !ArgoUtil.Alive(foe) || Life(foe) < l0 - 0.01f;
        if (!fired) { Log("steep shot " + t.Def.Display + ": foe " + F(drop) + " below was not targeted (out of sight?) (skipped)"); ClearEnemies(); yield break; }
        Check(t.Def.Display + " hits a foe " + F(drop) + " tiles below", hit, "volleys " + (t.Volleys - v0) + ", health " + F(l0) + "->" + (foe != null ? F(Life(foe)) : "dead")
            + ", flat " + F(ArgoUtil.Flat(ArgoUtil.Pos(foe), ArgoUtil.Pos(t.Master))));
#if ARGO_SHOTS
        yield return ArgoShot.Close("steep_" + t.Def.Family, (ArgoUtil.Pos(t.Master) + low.Position) * 0.5f, 4.5f);
#endif
        ClearEnemies();
    }

    // ---------------------------------------------------------------- the scenario

    static IEnumerator Scenario()
    {
        Log("match started: scenario begins");
        ArgoHeroBrain hero = ArgoServer.Heroes[0];
        yield return Until(() => Game_Mapvars.Instance != null && Game_Mapvars.Instance.Wave.Value >= 1, 150f);
        freeze = true;
        yield return Wait(2f);
        ClearEnemies();
        Entity_Master hm = hero.Master;
        long owner = ArgoUtil.Owner(hm);
        int team = ArgoUtil.Team(hm);
        Log("tide " + Game_Mapvars.Instance.Wave.Value + ", vessel at " + ArgoUtil.Pos(hm) + ", owner " + owner + ", team " + team + ", enemy type " + (enemyType != null ? enemyType.Objectname : "none yet"));
        ModKitTest.Log = Log;
        ModKitTest.MapDump(team, hm);

        // 1. Vessel: native melee setup, tooltip, commands, stats.
        Check("vessel uses the stock melee AI", hm.GetComponent<Entity_Ai_Mob_Combat>() != null && hm.GetComponent<Entity_Ai_Mob_Ranged>() == null && hm.GetComponent<Entity_Ai_Hero_Buildsell>() != null);
        Check("vessel has the Fathoms tooltip", hm.Upgradelistserver.ContainsKey(ArgoData.HeroTooltip));
        Check("vessel stats (Damage 4, Smacky's)", Mathf.Approximately(ArgoUtil.Stat(hm, "Health", 0), 65f) && Mathf.Approximately(ArgoUtil.Stat(hm, "Speed", 0), 2.25f) && Mathf.Approximately(ArgoUtil.Stat(hm, "Range", 0), 1.75f)
            && Mathf.Approximately(ArgoUtil.Stat(hm, "Damage", 0), 4f),
            "Health " + ArgoUtil.Stat(hm, "Health", 0) + ", Speed " + ArgoUtil.Stat(hm, "Speed", 0) + ", Range " + ArgoUtil.Stat(hm, "Range", 0) + ", Damage " + ArgoUtil.Stat(hm, "Damage", 0));
        int options = 0;
        for (int i = 0; i < 4; i++)
        {
            Link_Command c;
            if (hm.Commandlistserver.TryGetValue("Buildoption" + (i + 1), out c) && c.Goalstring == ArgoData.Towers[i]) options++;
        }
        Check("four build options", options == 4, options + "/4");
        Link_Command sonarCmd;
        Check("SONAR command", hm.Commandlistserver.TryGetValue(ArgoData.SonarLink, out sonarCmd) && sonarCmd.Controllable && ArgoContent.Commands[ArgoData.SonarLink].Commandfuncts[0] is ArgoSonarButton);
        Check("vessel visible to clients", hm.Client != null && hm.Client.Beingobject != null && hm.Client.Beingobject.Objectname == ArgoData.Hero);
        {
            // The Diving Bell is support, priced in Blue: tier 2 Blue only, Blue the main share above it.
            List<string> off = new List<string>();
            foreach (ArgoData.Upg u in ArgoData.BuildFor(ArgoData.Bell).Upgrades)
            {
                Upgradeproperties up = Bl_Address.Getload("Upgrade", u.Id) as Upgradeproperties;
                float blue = 0f, other = 0f;
                if (up != null && up.Costs != null)
                    foreach (Link_Resourceset r in up.Costs) { if (r.Name == "Blueaura") blue += r.Amount; else other += r.Amount; }
                if (up == null || blue <= other || (u.Tier == 2 && other > 0f)) off.Add(u.Key + u.Tier + " Blue " + F(blue) + ", other " + F(other));
            }
            Check("Diving Bell upgrades are priced in Blue (tier 2 Blue only)", off.Count == 0, off.Count == 0 ? "7 upgrades" : string.Join("; ", off.ToArray()));
        }

        // 1b. Speed floor: stacked slows never take a foe under a tenth of its speed, a hold stops it at its floor (the game's own 0.05 for its enemies) whatever
        // slows it carries, and the foe gets its full speed back when they expire (up to 1.2.2 two overlapping Tremor ticks
        // at full strength, or a hold plus a slow, summed past -100% and gave negative Speed). Before anything is built.
        {
            Entity_Master foe = SpeedFoe(ArgoUtil.Pos(hm), 6f);
            yield return Until(() => foe == null || ArgoUtil.Stat(foe, "Speed", 0f) > 0f, 1.5f);
            yield return Wait(0.3f);
            if (Check("a foe for the speed checks", foe != null && ArgoUtil.Alive(foe), foe != null ? ArgoUtil.BeingName(foe) + " " + F(Vector3.Distance(ArgoUtil.Pos(foe), ArgoUtil.Pos(hm))) + " tiles away, " + SpeedInfo(foe) : "none"))
            {
                float speed0 = ArgoUtil.Stat(foe, "Speed", 0f);
                ArgoUtil.Slow(hm, foe, ArgoData.SlowCap, 1.5f, true);
                ArgoUtil.Slow(hm, foe, ArgoData.SlowCap, 1.5f, false);
                yield return Wait(0.2f);
                float slowed = ArgoUtil.Stat(foe, "Speed", -1f);
                Check("two full-strength Tremor slows leave a foe 10% of its speed", slowed >= speed0 * 0.1f - 0.001f && slowed <= speed0 * 0.1f + 0.01f, F(speed0) + "->" + F(slowed));
                ArgoUtil.Root(hm, foe, 1f);
                ArgoUtil.Slow(hm, foe, ArgoData.SlowCap, 1f, false);
                yield return Wait(0.2f);
                float held = ArgoUtil.Stat(foe, "Speed", -1f);
                Link_Stats sp = foe != null && foe.Statsserver != null && foe.Statsserver.ContainsKey("Speed") ? foe.Statsserver["Speed"] : null;
                float floor = sp != null ? Mathf.Max(0f, sp.Value_Bounds.x) : 0f;
                Check("a hold on a slowed foe stops it at its Speed floor, never below", sp != null && sp.Value_Bounds.x >= 0f && Mathf.Abs(held - floor) < 0.0001f, "speed " + F(held) + ", floor " + F(floor) + ", " + SpeedInfo(foe));
                yield return Wait(1.6f);
                float after = foe != null ? ArgoUtil.Stat(foe, "Speed", -1f) : -1f;
                Check("when the hold and slows expire the foe has its speed back", Mathf.Abs(after - speed0) < 0.01f, F(speed0) + "->" + F(after));
            }
            if (foe != null && foe.Beingserver != null)
                Game_Entity.Instance.Despawn(foe.Beingserver, false, "Selftest");
        }

        // 2. Building every buildable through the vessel's real Build command (the Deep Drill next to ore).
        Helper_Resources.Giveresources(ArgoUtil.Costs("Redaura=20000;Blueaura=20000;Yellowaura=20000"), owner, Helper_Resources.Styles.Default);
        yield return Wait(0.5f);
        List<ArgoTowerBrain> known = new List<ArgoTowerBrain>(ArgoServer.Towers);
        Dictionary<string, ArgoTowerBrain> towers = new Dictionary<string, ArgoTowerBrain>();
        ArgoTowerBrain[] got = new ArgoTowerBrain[1];
        Vector3 home = ArgoUtil.Pos(hm);
        // The Deep Drill goes next to the nearest ore that has a buildable tile in reach.
        Entity_Master ore = null;
        Vector3 drillAt = new Vector3(float.NaN, 0f, 0f);
        List<Entity_Master> ores = new List<Entity_Master>(ArgoUtil.Area(home, 45f, ArgoUtil.Tagged("Resource")));
        ores.Sort((a, b) => ArgoUtil.Flat(ArgoUtil.Pos(a), home).CompareTo(ArgoUtil.Flat(ArgoUtil.Pos(b), home)));
        foreach (Entity_Master o in ores)
        {
            if (o == null || !ArgoUtil.Alive(o)) continue;
            drillAt = Spot(ArgoUtil.Pos(o), ArgoContent.Beings[ArgoData.Drill], team, 1f, ArgoData.OreReach - 0.05f);
            if (!float.IsNaN(drillAt.x)) { ore = o; break; }
        }
        Log(ores.Count + " ores; drilling " + (ore != null ? ArgoUtil.BeingName(ore) + " at " + ArgoUtil.Pos(ore) + ", " + F(ArgoUtil.Flat(ArgoUtil.Pos(ore), home)) + " from the vessel" : "none"));
        foreach (string tower in ArgoData.Towers)
        {
            Beingobject being = ArgoContent.Beings[tower];
            Vector3 at = tower == ArgoData.Drill && ore != null ? drillAt
                : tower == ArgoData.Harpoon ? HarpoonSpot(home, being, team) : Spot(home, being, team, tower == ArgoData.Bell ? 1.5f : 2.5f, 5f);
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
        ArgoTowerBrain scatter, harpoon, bell, drill;
        towers.TryGetValue(ArgoData.Scatter, out scatter);
        towers.TryGetValue(ArgoData.Harpoon, out harpoon);
        towers.TryGetValue(ArgoData.Bell, out bell);
        towers.TryGetValue(ArgoData.Drill, out drill);
        if (scatter != null)
            Check("Scatter Cannon stats", Mathf.Approximately(ArgoUtil.Stat(scatter.Master, "Projcount", 0), 6f) && Mathf.Approximately(ArgoUtil.Stat(scatter.Master, "Range", 0), 4.5f),
                "pellets " + ArgoUtil.Stat(scatter.Master, "Projcount", 0) + ", range " + ArgoUtil.Stat(scatter.Master, "Range", 0));
        if (harpoon != null)
            Check("Harpoon Tower stats (Damage 5, Range 6.5: the top of the shipped bands)", Mathf.Approximately(ArgoUtil.Stat(harpoon.Master, "Damage", 0), 5f) && Mathf.Approximately(ArgoUtil.Stat(harpoon.Master, "Range", 0), 6.5f)
                && Mathf.Approximately(ArgoUtil.Stat(harpoon.Master, "Pierce", 0), 3f) && Mathf.Approximately(ArgoUtil.Stat(harpoon.Master, "Attackspeed", 0), 0.35f),
                "damage " + ArgoUtil.Stat(harpoon.Master, "Damage", 0) + ", range " + ArgoUtil.Stat(harpoon.Master, "Range", 0) + ", pierce " + ArgoUtil.Stat(harpoon.Master, "Pierce", 0) + ", attack speed " + ArgoUtil.Stat(harpoon.Master, "Attackspeed", 0));
        // Each ranged building's range is drawn centred on it, at the size it really reaches (Deep Drill: its Tremors).
        foreach (ArgoTowerBrain t in new[] { scatter, harpoon, drill })
            if (t != null) yield return RangeCheck(t);
        Check("the Diving Bell shows no range (its Nauts go anywhere)", bell == null || !bell.Master.Client.Visbase.Exists(v => v is Entity_Vis_Tower_Range));
#if ARGO_SHOTS
        // How big the shipped towers are (Smacky's voxel ones, Sno Sno's and Buster's low-poly ones) next to Argo's,
        // and a close-up with a Sno Cannon and an Archer Tower model set down beside Argo's buildings.
        yield return LoadGroup("Hero_Snosno");
        yield return LoadGroup("Hero_Buster");
        foreach (string n in new[] { "Tower_Smackyarcher", "Tower_Smackybomber", "Tower_Smackyhut", "Tower_Smackywall", "Tower_Snosnocannon", "Tower_Snosnobox",
                                     "Tower_Snosnoheater", "Tower_Snosnowall", "Tower_Bustertotem", "Tower_Bustergarage", "Tower_Spriggpod", "Tower_Spriggcap",
                                     ArgoData.Scatter, ArgoData.Harpoon, ArgoData.Bell, ArgoData.Drill })
            Measure(n);
        List<GameObject> shows = new List<GameObject>();
        Vector3 focus = ArgoUtil.Pos(hm);
        int n0 = 1;
        foreach (ArgoTowerBrain t in towers.Values) { focus += ArgoUtil.Pos(t.Master); n0++; }
        focus /= n0;
        foreach (string show in new[] { "Tower_Snosnocannon", "Tower_Smackyarcher", "Tower_Bustertotem" })
        {
            Vector3 at = Spot(focus, ArgoContent.Beings[ArgoData.Scatter], team, 2f, 4f);
            if (!float.IsNaN(at.x)) { GameObject g = Showpiece(show, at); if (g != null) shows.Add(g); }
        }
        yield return Wait(1.5f);
        yield return ArgoShot.Close("towers", focus, 6f);
        yield return ArgoShot.Close("towers_near", focus, 3.5f);
        yield return ArgoShot.Close("argo", ArgoUtil.Pos(hm) + Vector3.up * 0.5f, 2.2f);
        yield return ArgoShot.Shot("built");
        foreach (GameObject g in shows) if (g != null) UnityEngine.Object.Destroy(g);
#endif

        // 3. The crew: two Nauts, sent with the bell's real PORT / STARBOARD commands.
        if (bell != null)
        {
            yield return Until(() => bell.Crew[0] != null && bell.Crew[1] != null, 8f);
            Check("Diving Bell raises Port and Starboard", bell.Crew[0] != null && bell.Crew[1] != null && bell.Crew[2] == null,
                (bell.Crew[0] != null) + "/" + (bell.Crew[1] != null) + "/" + (bell.Crew[2] != null));
            Check("crew commands on the bell", bell.Master.Commandlistserver.ContainsKey("Crew_Port") && bell.Master.Commandlistserver.ContainsKey("Crew_Starboard") && !bell.Master.Commandlistserver.ContainsKey("Crew_Bosun"));
            Check("Nauts visible to clients", bell.Crew[0] != null && bell.Crew[0].Master.Client != null && bell.Crew[0].Master.Client.Beingobject != null);
            if (scatter != null && bell.Crew[0] != null)
            {
                float aspd = ArgoUtil.Stat(scatter.Master, "Attackspeed", 0f);
                Send(bell.Master, "Crew_Port", Ref(scatter.Master));
                yield return Until(() => bell.Crew[0] != null && bell.Crew[0].Tuning, 25f);
                ArgoNautBrain port = bell.Crew[0];
                Check("PORT walks to the Scatter Cannon and tunes it", port != null && port.Tuning && port.Post == scatter.Master,
                    port != null ? "post " + ArgoUtil.BeingName(port.Post) + ", tuning " + port.Tuning + ", " + F(ArgoUtil.Flat(ArgoUtil.Pos(port.Master), ArgoUtil.Pos(scatter.Master))) + " away" : "no Port");
                Check("tuning: +20% attack speed", Mathf.Abs(ArgoUtil.Stat(scatter.Master, "Attackspeed", 0f) - aspd * (1f + ArgoData.TuneAttackspeed)) < 0.02f,
                    F(aspd) + "->" + F(ArgoUtil.Stat(scatter.Master, "Attackspeed", 0f)));
                Check("tuning skin on the Naut", port != null && port.Master.Skin.Value.ToString() == "Tune");
#if ARGO_SHOTS
                yield return Wait(0.5f);
                yield return ArgoShot.Close("tuning", ArgoUtil.Pos(scatter.Master), 2.6f);
#endif
                // One Naut per tower: Starboard takes over, Port heads home.
                Send(bell.Master, "Crew_Starboard", Ref(scatter.Master));
                yield return Wait(0.5f);
                Check("one Naut per tower", bell.Crew[0] == null || bell.Crew[0].Post == null, bell.Crew[0] != null ? "Port's post " + ArgoUtil.BeingName(bell.Crew[0].Post) : "");
                yield return Until(() => bell.Crew[1] != null && bell.Crew[1].Tuning, 25f);
                Check("Starboard took over", bell.Crew[1] != null && bell.Crew[1].Tuning && bell.Crew[1].Post == scatter.Master);
                Check("only one tune on the tower", Mathf.Abs(ArgoUtil.Stat(scatter.Master, "Attackspeed", 0f) - aspd * (1f + ArgoData.TuneAttackspeed)) < 0.02f, F(ArgoUtil.Stat(scatter.Master, "Attackspeed", 0f)));
                // The bell itself calls a Naut home.
                Send(bell.Master, "Crew_Starboard", Ref(bell.Master));
                yield return Wait(0.5f);
                Check("picking the bell recalls the Naut", bell.Crew[1] != null && bell.Crew[1].Post == null && !bell.Crew[1].Tuning && Mathf.Abs(ArgoUtil.Stat(scatter.Master, "Attackspeed", 0f) - aspd) < 0.02f,
                    "attack speed " + F(ArgoUtil.Stat(scatter.Master, "Attackspeed", 0f)));
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
                    Entity_Master e = Enemy(ArgoUtil.Pos(scatter.Master) + new Vector3(2f, 0f, i - 1f));
                    if (e != null) targets.Add(e);
                }
            if (harpoon != null)
                for (int i = 0; i < 3; i++)
                {
                    Entity_Master e = Enemy(ArgoUtil.Pos(harpoon.Master) + new Vector3(-3f - i, 0f, 0f));
                    if (e != null) targets.Add(e);
                }
            yield return Wait(0.4f);
            Log("spawned " + targets.Count + " enemies, health " + (targets.Count > 0 ? F(Life(targets[0])) : "-"));
            Dictionary<Entity_Master, float> life = new Dictionary<Entity_Master, float>();
            foreach (Entity_Master e in targets) life[e] = Life(e);
#if ARGO_SHOTS
            yield return Wait(2.2f);
            yield return ArgoShot.Close("fight", scatter != null && harpoon != null ? (ArgoUtil.Pos(scatter.Master) + ArgoUtil.Pos(harpoon.Master)) * 0.5f : ArgoUtil.Pos(hm), 4f);
            yield return Wait(4f);
#else
            yield return Wait(6f);
#endif
            if (scatter != null)
                Check("Scatter Cannon fires volleys of pellets", scatter.Shots >= scatterShots + 6 && scatter.Volleys > 0, (scatter.Shots - scatterShots) + " pellets, " + scatter.Volleys + " volleys");
            if (harpoon != null)
                Check("Harpoon Tower fires harpoons", harpoon.Shots > harpoonShots, (harpoon.Shots - harpoonShots) + " harpoons");
            int hurt = 0;
            foreach (KeyValuePair<Entity_Master, float> kv in life)
                if (kv.Key == null || !ArgoUtil.Alive(kv.Key) || Life(kv.Key) < kv.Value - 0.01f) hurt++;
            Check("projectiles hurt the enemies they hit", hurt >= Mathf.Min(2, life.Count), hurt + "/" + life.Count);
            Check("pellets and harpoons are the game's own projectiles", Game_Entity.Instance.Projectiles != null && Game_Entity.Instance.Projectiles.ContainsKey(ArgoData.Pellet) && Game_Entity.Instance.Projectiles.ContainsKey(ArgoData.HarpoonShot),
                Game_Entity.Instance.Projectiles != null ? ArgoUtil.Join(Game_Entity.Instance.Projectiles.Keys) : "none");
            ClearEnemies();
            // Steep shots: a foe on lower ground near each turret.
            if (scatter != null) yield return Steep(scatter, 1f);
            if (harpoon != null) yield return Steep(harpoon, 1.5f);
        }

        // 4b. The harpoon's reel (1.4): the first foe a harpoon hits walks 1.5 tiles toward the tower, the one behind it is
        // pierced but stays put, and a Boss-weight foe (600+ health) is hit but never moved.
        if (harpoon != null && enemyType != null)
        {
            ClearEnemies();
            Vector3 hp = ArgoUtil.Pos(harpoon.Master);
            float reach = ArgoUtil.Stat(harpoon.Master, "Range", 6.5f) - 0.4f;
            List<Entity_Master> lane = Lane(hp, 5f, reach, 2, true);
            // No room for two that far out: the reel is checked on one foe, the pierce in a second, closer lane.
            bool two = lane.Count == 2;
            if (!two)
            {
                ClearEnemies();
                lane = Lane(hp, 5f, reach, 1, true);
            }
            List<Link_Modifier> holds = new List<Link_Modifier>();
            if (lane.Count > 0) yield return Ready(harpoon, lane, 50f, holds);
            if (Check("foes in a lane from the Harpoon Tower", lane.Count > 0 && lane.TrueForAll(f => ArgoUtil.Alive(f)),
                lane.Count == 2 ? F(ArgoUtil.Flat(ArgoUtil.Pos(lane[0]), hp)) + " and " + F(ArgoUtil.Flat(ArgoUtil.Pos(lane[1]), hp)) + " tiles out"
                : lane.Count == 1 ? "one foe " + F(ArgoUtil.Flat(ArgoUtil.Pos(lane[0]), hp)) + " tiles out (no room for a second behind it)" : "none spawned"))
            {
                Entity_Master a = lane[0], b = two ? lane[1] : null;
                float lifeB = b != null ? Life(b) : 0f;
                Vector3 b0 = b != null ? ArgoUtil.Pos(b) : Vector3.zero;
                int reels = harpoon.Reels, shots0 = harpoon.Shots;
                yield return Until(() => harpoon.Reels > reels, 8f);
                // Measured from the hit: the held foes still crawl at the game's 0.05 Speed floor while they wait.
                float hitAt = Time.time, dHit = a != null ? ArgoUtil.Flat(ArgoUtil.Pos(a), hp) : -1f;
                if (b != null) b0 = ArgoUtil.Pos(b);
                yield return Until(() => harpoon.LastReelDone >= hitAt, 1.5f);
                float took = Time.time - hitAt, dEnd = a != null ? ArgoUtil.Flat(ArgoUtil.Pos(a), hp) : -1f;
                // A reel lands on a tile centre and stops within a quarter tile of it: a 1-tile reel reads 0.75 or more.
                Check("a harpoon reels the first foe it hits toward the tower (a tile or more), within 1s", harpoon.Reels == reels + 1 && a != null && dHit - dEnd >= 0.7f && took <= 1f,
                    "reels " + (harpoon.Reels - reels) + " of " + (harpoon.Shots - shots0) + " harpoons, " + F(dHit) + "->" + F(dEnd) + " tiles in " + F(took) + "s");
                if (two)
                    Check("the foe behind it is pierced but not reeled", b == null || (Life(b) < lifeB - 0.01f && ArgoUtil.Flat(ArgoUtil.Pos(b), b0) < 0.3f),
                        b != null ? "health " + F(lifeB) + "->" + F(Life(b)) + ", moved " + F(ArgoUtil.Flat(ArgoUtil.Pos(b), b0)) + " after the hit" : "felled");
#if ARGO_SHOTS
                yield return ArgoShot.Close("reel", (hp + (two ? b0 : a != null ? ArgoUtil.Pos(a) : hp)) * 0.5f, 4.5f);
#endif
            }
            if (!two)
            {
                // The pierce: two foes in a lane, the first close enough that it may not be reeled; the one behind it is
                // hit through it and stays put.
                ClearEnemies();
                lane = Lane(hp, 5f, reach, 2);
                holds.Clear();
                if (lane.Count == 2) yield return Ready(harpoon, lane, 50f, holds);
                if (Check("two foes in a lane from the Harpoon Tower", lane.Count == 2 && lane.TrueForAll(f => ArgoUtil.Alive(f)),
                    lane.Count == 2 ? F(ArgoUtil.Flat(ArgoUtil.Pos(lane[0]), hp)) + " and " + F(ArgoUtil.Flat(ArgoUtil.Pos(lane[1]), hp)) + " tiles out" : "none spawned"))
                {
                    Entity_Master b = lane[1];
                    float lifeB = Life(b);
                    yield return Until(() => b == null || Life(b) < lifeB - 0.01f, 8f);
                    Vector3 b0 = b != null ? ArgoUtil.Pos(b) : Vector3.zero;
                    yield return Wait(1f);
                    Check("the foe behind it is pierced but not reeled", b != null && Life(b) < lifeB - 0.01f && ArgoUtil.Flat(ArgoUtil.Pos(b), b0) < 0.3f,
                        b != null ? "health " + F(lifeB) + "->" + F(Life(b)) + ", moved " + F(ArgoUtil.Flat(ArgoUtil.Pos(b), b0)) + " after the hit" : "felled");
                }
            }
            ClearEnemies();
            lane = Lane(hp, 4f, reach, 1);
            holds.Clear();
            if (lane.Count == 1) yield return Ready(harpoon, lane, 700f, holds);
            if (Check("a Boss-weight foe in front of the Harpoon Tower", lane.Count == 1 && ArgoUtil.MaxHealth(lane[0]) >= ArgoData.BossHealth,
                lane.Count == 1 ? "max health " + F(ArgoUtil.MaxHealth(lane[0])) : "none"))
            {
                Entity_Master boss = lane[0];
                Vector3 p0 = ArgoUtil.Pos(boss);
                float life0 = Life(boss);
                int bossHits = harpoon.BossHits, reels = harpoon.Reels;
                yield return Until(() => harpoon.BossHits > bossHits, 8f);
                yield return Wait(0.6f);
                Check("a Boss-weight foe is harpooned but not moved", harpoon.BossHits > bossHits && harpoon.Reels == reels && Life(boss) < life0 && ArgoUtil.Flat(ArgoUtil.Pos(boss), p0) < 0.3f,
                    "boss hits " + (harpoon.BossHits - bossHits) + ", reels " + (harpoon.Reels - reels) + ", moved " + F(ArgoUtil.Flat(ArgoUtil.Pos(boss), p0)));
            }
            ClearEnemies();
        }

        // 5. SONAR: Pinged enemies take 20% more damage; the button greys out while it recharges.
        if (enemyType != null)
        {
            Vector3 front = ArgoUtil.Pos(hm) + new Vector3(0f, 0f, 3f);
            Entity_Master dummy = Enemy(front);
            yield return Wait(0.3f);
            if (Check("enemy for the SONAR test", dummy != null))
            {
                float l0 = Life(dummy);
                Helper_Entitytools.Interacthurt(hm, dummy, ArgoUtil.Hit(2f, 1f));
                float plain = l0 - Life(dummy);
                hero.SonarReadyAt = 0f;
                Send(hm, ArgoData.SonarLink, "");
                yield return Wait(0.3f);
                Check("SONAR pings the enemy", ArgoHooks.IsPinged(dummy) && hero.Sonars >= 1, "sonars " + hero.Sonars);
                float l1 = Life(dummy);
                Helper_Entitytools.Interacthurt(hm, dummy, ArgoUtil.Hit(2f, 1f));
                float pinged = l1 - Life(dummy);
                Check("Pinged enemies take 20% more damage", Mathf.Abs(pinged - plain * (1f + ArgoData.PingBonus)) < 0.05f, F(plain) + " -> " + F(pinged));
                int before = hero.Sonars;
                Send(hm, ArgoData.SonarLink, "");
                yield return Wait(0.3f);
                bool off = false;
                foreach (Link_Commandref r in hm.Commandlist)
                    if (r.Name.ToString() == ArgoData.SonarLink) off = r.Goalstring.ToString() == "Off";
                Check("SONAR has a cooldown and the button shows it", hero.Sonars == before && off, "sonars " + before + "->" + hero.Sonars + ", button off " + off);
                // When the recharge ends the button lights up again without firing SONAR by itself (Argo 1.0 re-ran the
                // command when it refreshed the button: SONAR went off on its own every 15 s, quaking nearby drills).
                hero.SonarReadyAt = Time.time + 0.4f;
                yield return Wait(1.2f);
                bool lit = false;
                foreach (Link_Commandref r in hm.Commandlist)
                    if (r.Name.ToString() == ArgoData.SonarLink) lit = r.Goalstring.ToString() != "Off";
                Check("SONAR recharges without going off by itself", hero.Sonars == before && lit, "sonars " + before + "->" + hero.Sonars + ", button lit " + lit);
#if ARGO_SHOTS
                yield return ArgoShot.Close("sonar", ArgoUtil.Pos(hm), 4.5f);
#endif
            }
            ClearEnemies();

            // 6. The second hit: two enemies side by side in front of Argo.
            int cleaves = hero.Cleaves;
            Vector3 p = ArgoUtil.Pos(hm);
            Entity_Master a = Enemy(p + new Vector3(1f, 0f, 0f)), b = Enemy(p + new Vector3(1f, 0f, 1f));
            yield return Until(() => hero.Cleaves > cleaves, 12f);
            Check("each swing hits a second enemy", hero.Cleaves > cleaves, "cleaves " + cleaves + "->" + hero.Cleaves + (a == null || b == null ? " (spawn failed)" : ""));
            // Both count as hits by Argo (kill credit; an Aegis stops regenerating): 1.1's second hit didn't.
            Check("the second hit counts as Argo's", hero.Cleaves > cleaves && (a == null || a.Lasthit == hm) && (b == null || b.Lasthit == hm),
                "last hit by " + (a != null && a.Lasthit != null ? ArgoUtil.BeingName(a.Lasthit) : "-") + " / " + (b != null && b.Lasthit != null ? ArgoUtil.BeingName(b.Lasthit) : "-"));
            ClearEnemies();
        }

        // 6b. Pressure (1.4): a Deep Drill's Tremors load Argo's guns inside them, +1 pellet or +1 pierce for each Fathom
        // below the first, and the load goes when the drill does. A second drill beside the guns, its Tremors widened to
        // reach both, sunk to Fathom 3 (before the Tide in 7, so the first drill is still at Fathom 1 and adds nothing).
        if (scatter != null && harpoon != null)
        {
            Vector3 mid = (ArgoUtil.Pos(scatter.Master) + ArgoUtil.Pos(harpoon.Master)) * 0.5f;
            // Any free tile within 10, nearest the guns first: its Tremors are widened below to reach both wherever it
            // stands (the second, fifth and seventh PC runs found no tile with Spot).
            Vector3 at = AnySpot(mid, ArgoContent.Beings[ArgoData.Drill], team, 10f, "a second Deep Drill");
            ArgoTowerBrain deep = null;
            if (!float.IsNaN(at.x))
            {
                yield return Build(hm, ArgoData.Drill, at, known, got);
                deep = got[0];
            }
            if (Check("a second Deep Drill beside the guns", deep != null, "at " + at))
            {
                yield return Until(() => deep.Started, 3f);
                float far = Mathf.Max(ArgoUtil.Flat(ArgoUtil.Pos(deep.Master), ArgoUtil.Pos(scatter.Master)), ArgoUtil.Flat(ArgoUtil.Pos(deep.Master), ArgoUtil.Pos(harpoon.Master)));
                Helper_Entitytools.Addmod(deep.Master, "Range", Mathf.Max(6f, far + 0.5f - deep.TremorRadius), Helper_Entitytools.Modtypes.Add, 0f);
                deep.SinkTo(3);
                yield return Until(() => scatter.Pressure == 2 && harpoon.Pressure == 2, 2.5f);
                float pellets = ArgoUtil.Stat(scatter.Master, "Projcount", 0f), pierce = ArgoUtil.Stat(harpoon.Master, "Pierce", 0f);
                Check("Pressure: a Fathom 3 Drill loads its guns, 6+2 pellets and 3+2 pierce", deep.Fathom == 3 && Mathf.Approximately(pellets, 8f) && Mathf.Approximately(pierce, 5f),
                    "Fathom " + deep.Fathom + ", pressure " + scatter.Pressure + "/" + harpoon.Pressure + ", pellets " + F(pellets) + ", pierce " + F(pierce));
#if ARGO_SHOTS
                yield return ArgoShot.Close("pressure", mid, 5f);
#endif
                Entity_Master dm = deep.Master;
                yield return Sell(hm, dm, owner);
                float soldAt = Time.time;
                yield return Until(() => scatter.Pressure == 0 && harpoon.Pressure == 0, 1.5f);
                pellets = ArgoUtil.Stat(scatter.Master, "Projcount", 0f);
                pierce = ArgoUtil.Stat(harpoon.Master, "Pierce", 0f);
                Check("with the Drill gone, the guns are back to 6 pellets and 3 pierce within 1.5s", Gone(dm) && Time.time - soldAt <= 1.5f && Mathf.Approximately(pellets, 6f) && Mathf.Approximately(pierce, 3f),
                    "gone " + Gone(dm) + ", after " + F(Time.time - soldAt) + "s, pellets " + F(pellets) + ", pierce " + F(pierce));
            }
        }

        // 7. The Deep Drill: it bites ore, sinks a Fathom each Tide, and its Tremors slow enemies.
        if (drill != null)
        {
            Entity_Master drilled = drill.Ore(ArgoUtil.Pos(drill.Master));
            Check("Deep Drill has ore in reach", drilled != null, ore != null ? F(ArgoUtil.Flat(ArgoUtil.Pos(drill.Master), ArgoUtil.Pos(ore))) + " from the ore" : "no ore on the map");
            yield return Until(() => drill.Bites > 0, 6f);
            Check("Deep Drill mines ore (the game's own ore damage)", drill.Bites > 0, drill.Bites + " bites");
            float range0 = drill.TremorRadius;
            int[] reached = new int[1];
            yield return ForceTide(reached);
            Check("Tide sinks the drill to Fathom 2", drill.Fathom == 2 && drill.Master.Skin.Value.ToString() == "Fathom2" && Mathf.Abs(drill.TremorRadius - range0 - ArgoData.FathomTremor) < 0.01f,
                "Fathom " + drill.Fathom + ", skin '" + drill.Master.Skin.Value + "', Tremors " + F(range0) + "->" + F(drill.TremorRadius));
            Check("Fathom stat on the drill", Mathf.Approximately(ArgoUtil.Stat(drill.Master, "Fathom", 0f), 2f));
            // Every Fathom: harder and faster mining, a stronger slow (1.2; 1.1 only mined harder).
            float dmg2 = ArgoUtil.Stat(drill.Master, "Damage", 0f), aspd2 = ArgoUtil.Stat(drill.Master, "Attackspeed", 0f), slow2 = ArgoUtil.Stat(drill.Master, "Slow", 0f);
            Check("Fathom 2 mines harder and faster and slows more", Mathf.Abs(dmg2 - 0.5f * (1f + ArgoData.FathomYield)) < 0.01f && Mathf.Abs(aspd2 - 0.25f * (1f + ArgoData.FathomSpeed)) < 0.01f
                && Mathf.Abs(slow2 - 0.2f - ArgoData.FathomSlow) < 0.01f, "ore per strike " + F(dmg2) + ", strikes/s " + F(aspd2) + ", slow " + F(slow2));
            Check("client Tremor radius matches the server's", Mathf.Abs(ArgoClient.TremorRadius(drill.Master) - drill.TremorRadius) < 0.01f, F(ArgoClient.TremorRadius(drill.Master)) + " vs " + F(drill.TremorRadius));
            Check("client Tremor slow matches the server's", Mathf.Abs(ArgoClient.TremorSlow(drill.Master) - slow2) < 0.01f, F(ArgoClient.TremorSlow(drill.Master)) + " vs " + F(slow2));
            if (enemyType != null)
            {
                // The guns are kept from firing meanwhile: in the fifth PC run the harpoon reeled this foe, and the reel's
                // +5 Speed hid the slow.
                List<Link_Modifier> jams = new List<Link_Modifier>();
                foreach (ArgoTowerBrain gun in new[] { scatter, harpoon })
                    if (gun != null && gun.Master != null)
                        jams.Add(Helper_Entitytools.Addmod(gun.Master, "Attackspeed", -0.99f, Helper_Entitytools.Modtypes.Multi, 0f));
                Entity_Master e = Enemy(ArgoUtil.Pos(drill.Master) + new Vector3(1f, 0f, 0f));
                // Right after a Tide the enemy core can be between spawns: try again for a moment.
                for (int tries = 0; e == null && tries < 10; tries++)
                {
                    yield return Wait(0.5f);
                    e = Enemy(ArgoUtil.Pos(drill.Master) + new Vector3(1f, 0f, 0f));
                }
                if (e == null) Log("tremor test: no enemy could be placed near the drill at " + ArgoUtil.Pos(drill.Master));
                // The enemy's own base speed (its stats are not set up yet on the frame it spawns).
                float speed0 = 0f;
                if (e != null && e.Beingserver != null && e.Beingserver.Being != null && e.Beingserver.Being.Entity.Defaultstat != null)
                {
                    Statproperties.Defaultstatgroup g = e.Beingserver.Being.Entity.Defaultstat.Stats.Find(x => x.Name == "Speed");
                    if (g != null) speed0 = g.Value;
                }
                yield return Wait(1.2f);
                Check("Tremors slow enemies", e != null && ArgoUtil.Stat(e, "Speed", 0f) < speed0 - 0.01f, e != null ? F(speed0) + "->" + F(ArgoUtil.Stat(e, "Speed", 0f)) : "no enemy");
                foreach (Link_Modifier j in jams) ArgoUtil.Unmod(j);
                int q = drill.Quakes;
                hero.SonarReadyAt = 0f;
                if (ArgoUtil.Flat(ArgoUtil.Pos(drill.Master), ArgoUtil.Pos(hm)) > ArgoData.SonarRadius)
                    Log("drill is out of SONAR range from the vessel; quaking it directly");
                if (ArgoUtil.Flat(ArgoUtil.Pos(drill.Master), ArgoUtil.Pos(hm)) <= ArgoData.SonarRadius) hero.Sonar(); else drill.Quake(ArgoData.QuakeRoot);
                yield return Wait(0.2f);
                Check("SONAR quakes the drill", drill.Quakes > q, "quakes " + q + "->" + drill.Quakes);
#if ARGO_SHOTS
                yield return ArgoShot.Close("quake", ArgoUtil.Pos(drill.Master), 4f);
#endif
                ClearEnemies();
            }
        }

        // 8. Upgrades: paid, applied through the stock upgrade flow, and their effects.
        if (scatter != null)
        {
            // No enemies about while it is paid: one felled meanwhile pays Red (the second PC run read 27 spent of 30). The
            // forced Tide's foes keep coming (the fifth read 27 again), so only the drops in Red are counted.
            ClearEnemies();
            float[] paid = new float[2];
            yield return UpgradePaid(scatter, "Widebore2", owner, "Redaura", paid);
            // (Any Pressure from the first drill, now at Fathom 2, comes on top.)
            float load = scatter.Pressure * ArgoData.PressurePellets;
            Check("Wide Bore: more pellets, wider cone", Has(scatter, "Widebore2") && Mathf.Approximately(ArgoUtil.Stat(scatter.Master, "Projcount", 0), 8f + load) && Mathf.Approximately(ArgoUtil.Stat(scatter.Master, "Spread", 0), 75f),
                "pellets " + ArgoUtil.Stat(scatter.Master, "Projcount", 0) + " (Pressure " + scatter.Pressure + "), cone " + ArgoUtil.Stat(scatter.Master, "Spread", 0));
            Check("Wide Bore paid", Mathf.Abs(paid[0] - 30f) < 0.01f, "Red spent " + F(paid[0]) + (paid[1] > 0f ? ", " + F(paid[1]) + " came in meanwhile" : ""));
            yield return Upgrade(scatter, "Pressure4", owner);
            Check("tier-4 upgrade not offered from tier 2", !Has(scatter, "Pressure4"));
            // Double Barrel (forced: the other tier-2 root): a reload with nothing in range loads a volley, and the next
            // foe gets both.
            if (enemyType != null)
            {
                Force(scatter, "Double2");
                ClearEnemies();
                yield return Until(() => scatter.Stored > 0, 5f);
                Check("Double Barrel loads a volley while nothing is in range", Has(scatter, "Double2") && scatter.Stored == 1, "stored " + scatter.Stored);
                // A tougher foe, so the first volley doesn't fell it before the stored one follows.
                List<Entity_Master> foe = new List<Entity_Master> { Enemy(ArgoUtil.Pos(scatter.Master) + new Vector3(2f, 0f, 0f)) };
                yield return Ready(scatter, foe, 50f, new List<Link_Modifier>());
                int shots0 = scatter.Shots, volleys0 = scatter.Volleys;
                yield return Until(() => scatter.Volleys > volleys0, 4f);
                float first = Time.time;
                int pellets = Mathf.RoundToInt(ArgoUtil.Stat(scatter.Master, "Projcount", 6f));
                yield return Until(() => scatter.Volleys > volleys0 + 1, 1f);
                float gap = Time.time - first;
                Check("Double Barrel: the next volley fires twice the pellets", scatter.Volleys == volleys0 + 2 && scatter.Shots - shots0 == 2 * pellets && gap <= ArgoData.DoubleDelay + 0.2f && scatter.Stored == 0,
                    "volleys " + (scatter.Volleys - volleys0) + ", pellets " + (scatter.Shots - shots0) + " (2 x " + pellets + "), " + F(gap) + "s apart, stored " + scatter.Stored);
                ClearEnemies();
            }
        }
        if (harpoon != null)
        {
            yield return Upgrade(harpoon, "Longline2", owner);
            yield return Upgrade(harpoon, "Skewer3", owner);
            float load = harpoon.Pressure * ArgoData.PressurePierce;
            Check("Long Line -> Skewer: range and pierce", Has(harpoon, "Skewer3") && Mathf.Approximately(ArgoUtil.Stat(harpoon.Master, "Pierce", 0), 6f + load) && Mathf.Approximately(ArgoUtil.Stat(harpoon.Master, "Range", 0), 8.5f),
                "pierce " + ArgoUtil.Stat(harpoon.Master, "Pierce", 0) + " (Pressure " + harpoon.Pressure + "), range " + ArgoUtil.Stat(harpoon.Master, "Range", 0));
            // Anchor Line (forced: the other tier 3): the reeled foe is held where it lands, then let go.
            if (enemyType != null)
            {
                Force(harpoon, "Anchor3");
                ClearEnemies();
                List<Entity_Master> lane = Lane(ArgoUtil.Pos(harpoon.Master), 5f, ArgoUtil.Stat(harpoon.Master, "Range", 8.5f) - 0.4f, 1, true);
                List<Link_Modifier> holds = new List<Link_Modifier>();
                if (lane.Count == 1) yield return Ready(harpoon, lane, 50f, holds);
                if (Check("a foe for Anchor Line", Has(harpoon, "Anchor3") && lane.Count == 1 && ArgoUtil.Alive(lane[0]), "anchor " + Has(harpoon, "Anchor3") + ", foes " + lane.Count))
                {
                    Entity_Master a = lane[0];
                    int reels = harpoon.Reels, shots0 = harpoon.Shots;
                    yield return Until(() => harpoon.Reels > reels, 8f);
                    if (harpoon.Reels == reels) Log("Anchor Line: no reel in 8 s, " + (harpoon.Shots - shots0) + " harpoons fired, foe " + (a != null ? F(ArgoUtil.Flat(ArgoUtil.Pos(a), ArgoUtil.Pos(harpoon.Master))) + " tiles out" : "gone"));
                    ArgoUtil.Unmod(holds[0]);
                    float hitAt = Time.time;
                    yield return Until(() => harpoon.LastReelDone >= hitAt, 1.5f);
                    float landed = Time.time;
                    yield return Wait(0.25f);
                    Link_Stats sp = a != null && a.Statsserver != null && a.Statsserver.ContainsKey("Speed") ? a.Statsserver["Speed"] : null;
                    float floor = sp != null ? Mathf.Max(0f, sp.Value_Bounds.x) : 0f, held = ArgoUtil.Stat(a, "Speed", -1f);
                    Check("Anchor Line: the reeled foe is held where it lands", harpoon.Reels > reels && sp != null && held <= floor + 0.01f, "speed " + F(held) + ", floor " + F(floor) + ", " + SpeedInfo(a));
                    yield return Until(() => Time.time > landed + ArgoData.ReelTime + 1.2f, 3f);
                    float free = ArgoUtil.Stat(a, "Speed", -1f);
                    Check("Anchor Line lets go after about a second", a == null || !ArgoUtil.Alive(a) || free > floor + 0.05f, "speed " + F(free));
                }
                ClearEnemies();
            }
        }
        if (drill != null)
        {
            yield return Upgrade(drill, "Paydirt2", owner);
            yield return Upgrade(drill, "Deepcore3", owner);
            Check("Pay Dirt -> Deep Core: sinks to 7", Has(drill, "Paydirt2") && Has(drill, "Deepcore3") && drill.FathomMax == ArgoData.FathomDeepest, "max Fathom " + drill.FathomMax);
            // Pay Dirt: a foe felled inside the Tremors is dug up, two mining strikes on the drill's ore; one felled
            // outside gives nothing. (Ore pays its Yellow only when it cracks, so the strikes are counted.)
            if (enemyType != null && drill.Ore(ArgoUtil.Pos(drill.Master)) != null)
            {
                ClearEnemies();
                Vector3 dp = ArgoUtil.Pos(drill.Master);
                float radius = drill.TremorRadius;
                // Placed is noted before the kill: a felled foe is destroyed and reads as null (the second PC run's
                // "no foe placed").
                Entity_Master inside = FoeBetween(dp, 0.8f, radius - 0.4f);
                bool placedIn = inside != null;
                yield return Wait(0.4f);
                int dug = drill.PaydirtBites, bites = drill.Bites;
                if (inside != null) Helper_Entitytools.Interacthurt(hm, inside, ArgoUtil.Hit(9999f, 1f));
                yield return Wait(0.3f);
                Check("Pay Dirt: a foe felled in the Tremors is dug up for two strikes of ore", placedIn && !ArgoUtil.Alive(inside) && drill.PaydirtBites == dug + 2 && drill.Bites >= bites + 2,
                    !placedIn ? "no foe placed inside " + F(radius) : "strikes " + (drill.PaydirtBites - dug) + ", bites " + (drill.Bites - bites));
                Entity_Master outside = FoeBetween(dp, radius + 1f, radius + 3f);
                bool placedOut = outside != null;
                yield return Wait(0.4f);
                dug = drill.PaydirtBites;
                if (outside != null) Helper_Entitytools.Interacthurt(hm, outside, ArgoUtil.Hit(9999f, 1f));
                yield return Wait(0.3f);
                Check("Pay Dirt: a foe felled outside the Tremors gives nothing", placedOut && !ArgoUtil.Alive(outside) && drill.PaydirtBites == dug,
                    !placedOut ? "no foe placed outside " + F(radius) : "strikes " + (drill.PaydirtBites - dug));
#if ARGO_SHOTS
                yield return ArgoShot.Close("paydirt", dp, 4f);
#endif
                ClearEnemies();
            }
            drill.SinkTo(ArgoData.FathomDeepest);
            yield return Wait(0.5f);
            Check("Fathom 7: its own look, and every player draws the right Tremors", drill.Fathom == ArgoData.FathomDeepest && drill.Master.Skin.Value.ToString() == "Fathom" + ArgoData.FathomDeepest
                && Mathf.Abs(ArgoClient.TremorRadius(drill.Master) - drill.TremorRadius) < 0.01f,
                "Fathom " + drill.Fathom + ", skin '" + drill.Master.Skin.Value + "', Tremors client " + F(ArgoClient.TremorRadius(drill.Master)) + " vs " + F(drill.TremorRadius));
        }
        if (bell != null)
        {
            yield return Upgrade(bell, "Salts2", owner);
            Check("Old Salts is one of the Diving Bell's tier-2 roots", Has(bell, "Salts2"));
            yield return Upgrade(bell, "Overhaul3", owner);
            yield return Upgrade(bell, "Thirdmate4", owner);
            yield return Until(() => bell.Crew[2] != null, 6f);
            Check("Third Mate hires Bosun", Has(bell, "Thirdmate4") && bell.Crew[2] != null && bell.Master.Commandlistserver.ContainsKey("Crew_Bosun"),
                "units " + ArgoUtil.Stat(bell.Master, "Units", 0) + ", Bosun " + (bell.Crew[2] != null));
            // Full Crew: the Naut dives straight to its tower.
            yield return Upgrade(bell, "Fullcrew5", owner);
            // Old Salts: Port tunes each kind of Argo tower its own way, and it all goes when Port moves on.
            if (scatter != null && harpoon != null && bell.Crew[0] != null)
            {
                float pellets = ArgoUtil.Stat(scatter.Master, "Projcount", 0f), pierce = ArgoUtil.Stat(harpoon.Master, "Pierce", 0f);
                float slow = drill != null ? ArgoUtil.Stat(drill.Master, "Slow", 0f) : 0f;
                Send(bell.Master, "Crew_Port", Ref(scatter.Master));
                yield return Until(() => Tuned(bell, 0, scatter), 10f);
                Check("Old Salts: a tuned Scatter Cannon fires 2 more pellets", Tuned(bell, 0, scatter) && Mathf.Abs(ArgoUtil.Stat(scatter.Master, "Projcount", 0f) - pellets - ArgoData.SaltsPellets) < 0.01f,
                    "tuned " + Tuned(bell, 0, scatter) + ", pellets " + F(pellets) + "->" + F(ArgoUtil.Stat(scatter.Master, "Projcount", 0f)));
                Send(bell.Master, "Crew_Port", Ref(harpoon.Master));
                yield return Until(() => Tuned(bell, 0, harpoon), 10f);
                Check("Full Crew: Port dives to the Harpoon Tower and tunes it twice as hard", Tuned(bell, 0, harpoon),
                    bell.Crew[0] != null ? "tuning " + bell.Crew[0].Tuning + ", attack speed " + F(ArgoUtil.Stat(harpoon.Master, "Attackspeed", 0f)) : "no Port");
                Check("Old Salts: a tuned Harpoon Tower pierces 2 more and reels a tile farther; the Scatter Cannon has its pellets back",
                    Mathf.Abs(ArgoUtil.Stat(harpoon.Master, "Pierce", 0f) - pierce - ArgoData.SaltsPierce) < 0.01f && Mathf.Approximately(harpoon.TunedReel, ArgoData.SaltsReel)
                    && Mathf.Abs(ArgoUtil.Stat(scatter.Master, "Projcount", 0f) - pellets) < 0.01f,
                    "pierce " + F(pierce) + "->" + F(ArgoUtil.Stat(harpoon.Master, "Pierce", 0f)) + ", reel +" + F(harpoon.TunedReel) + ", pellets " + F(ArgoUtil.Stat(scatter.Master, "Projcount", 0f)));
                ArgoTowerBrain last = harpoon;
                if (drill != null)
                {
                    Send(bell.Master, "Crew_Port", Ref(drill.Master));
                    yield return Until(() => Tuned(bell, 0, drill), 12f);
                    Check("Old Salts: a tuned Deep Drill slows 10% more; the Harpoon Tower is back to normal",
                        Tuned(bell, 0, drill) && Mathf.Abs(ArgoUtil.Stat(drill.Master, "Slow", 0f) - slow - ArgoData.SaltsSlow) < 0.01f
                        && Mathf.Abs(ArgoUtil.Stat(harpoon.Master, "Pierce", 0f) - pierce) < 0.01f && harpoon.TunedReel == 0f,
                        "tuned " + Tuned(bell, 0, drill) + ", slow " + F(slow) + "->" + F(ArgoUtil.Stat(drill.Master, "Slow", 0f)) + ", pierce " + F(ArgoUtil.Stat(harpoon.Master, "Pierce", 0f)) + ", reel +" + F(harpoon.TunedReel));
                    last = drill;
                }
                Send(bell.Master, "Crew_Port", Ref(bell.Master));
                yield return Wait(0.5f);
                Check("Old Salts: it all goes when the Naut leaves", !Tuned(bell, 0, last) && Mathf.Abs(ArgoUtil.Stat(scatter.Master, "Projcount", 0f) - pellets) < 0.01f
                    && Mathf.Abs(ArgoUtil.Stat(harpoon.Master, "Pierce", 0f) - pierce) < 0.01f && (drill == null || Mathf.Abs(ArgoUtil.Stat(drill.Master, "Slow", 0f) - slow) < 0.01f),
                    "pellets " + F(ArgoUtil.Stat(scatter.Master, "Projcount", 0f)) + ", pierce " + F(ArgoUtil.Stat(harpoon.Master, "Pierce", 0f)) + (drill != null ? ", slow " + F(ArgoUtil.Stat(drill.Master, "Slow", 0f)) : ""));
            }
            // Flippers (forced: the other tier-2 root): faster Nauts.
            Force(bell, "Flippers2");
            yield return Wait(0.3f);
            Check("Flippers: faster Nauts", Has(bell, "Flippers2") && bell.Crew[0] != null && ArgoUtil.Stat(bell.Crew[0].Master, "Speed", 0f) > 1.6f * 1.4f, bell.Crew[0] != null ? F(ArgoUtil.Stat(bell.Crew[0].Master, "Speed", 0f)) : "no Port");
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
        Check("journal: TOWERS & UNITS links Argo's four buildings", towerLinks == 4, towerLinks + " links");
        if (bellPage != null)
            foreach (Logitems.Base b in All(bellPage.Elements))
            {
                if (b is Logitems.LinkUpgrade && ((Logitems.LinkUpgrade)b).Target != null && upgradePage == null) upgradePage = ((Logitems.LinkUpgrade)b).Target;
                if (b is Logitems.LinkImage && ((Logitems.LinkImage)b).Target != null && unitPage == null) unitPage = ((Logitems.LinkImage)b).Target;
            }
        Check("journal: building page links its upgrades", upgradePage != null && upgradePage.name.Contains("Argobell"), upgradePage != null ? upgradePage.name : "none");
        Check("journal: the Diving Bell lists its Nauts", unitPage != null && unitPage.name.Contains("Argonaut"), unitPage != null ? unitPage.name : "none");
        List<string> order = null;
        bool same = true, noDupes = true;
        int pagesWithTabs = 0;
        Dictionary<string, UnityEngine.Object> heroes = ArgoUtil.Folder("Hero");
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
#if ARGO_SHOTS
        ArgoClient.Demo(ArgoUtil.Pos(hm));
        yield return Wait(0.2f);
        yield return ArgoShot.Shot("fx");
        Bl_Menus.Changemenu("Journal");
        Heroentryobject smackyHero = ArgoUtil.Heroentry("Hero_Smacky"), spriggHero = ArgoUtil.Heroentry("Hero_Sprigg");
        Logobject scatterPage = null;
        foreach (Logitems.Base b in All(page.Elements))
            if (b is Logitems.Link && !((Logitems.Link)b).Sidelink && ((Logitems.Link)b).Target != null && ((Logitems.Link)b).Target.name.EndsWith(ArgoData.Scatter)) scatterPage = ((Logitems.Link)b).Target;
        foreach (Logobject open in new[] { page, smackyHero != null ? smackyHero.Log : null, spriggHero != null ? spriggHero.Log : null, scatterPage, bellPage, upgradePage, unitPage })
        {
            if (open == null) continue;
            Loghandler.Log_Current = open;
            yield return Wait(2.5f);
            // The journal's spinning models are UIObject3D targets (renamed "Target"): log what each one shows.
            foreach (Transform t in UnityEngine.Object.FindObjectsOfType<Transform>())
                if (t.name == "Target" && t.childCount > 0)
                {
                    Renderer r = t.GetComponentInChildren<Renderer>();
                    Log("JOURNAL3D " + open.name + ": " + t.GetChild(0).name + " scale " + t.localScale.ToString("F3") + " layer " + t.gameObject.layer + " renderer " + (r != null ? r.name + " " + r.bounds.size.ToString("F2") + " enabled " + r.enabled : "none"));
                }
            yield return ArgoShot.Shot("journal_" + open.name);
        }
        Bl_Menus.Changemenu("Main");
        yield return Wait(1f);
#endif
        Log("hooks: boosted " + ArgoHooks.Boosted + ", softened " + ArgoHooks.Softened + "; tabs added " + ModKitVesselTabs.Added + ", removed " + ModKitVesselTabs.Removed + ", sorted " + ModKitVesselTabs.Sorted);
        freeze = false;
        Log("DONE pass=" + pass + " fail=" + fail + " setup=" + ModKitTest.Setups);
        phase = 9;
    }
}
#endif
