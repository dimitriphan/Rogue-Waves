using Blockage.Menu;
#if YOSSELE_SELFTEST
// Debug build only (Build-YosseleMod.ps1 -Define YOSSELE_SELFTEST[,YOSSELE_SHOTS]). Starts a solo match by itself (Embark,
// pick Yossele, pick a map, ready up) because the game ignores injected input, then plays through every Yossele mechanic
// on the host the way a player would (real Build, Fertilize, Colour and Upgrade commands through Command_ServerRpc),
// spawns and fells foes, forces Tides, and logs one "[Yossele] TEST PASS|FAIL name: detail" line per check plus a final
// "TEST DONE" summary in Player.log.
using UnityEngine.SceneManagement;
using Unity.Netcode;

public static class YosseleSelfTest
{
    static int phase;
    static float seen = -1f, step = -1f;
    static string node = "";
    static bool freeze, forcing;
    static int pass, fail;

    static void Log(string m) { YosseleLog.Info("TEST " + m); }

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
        if (freeze && !forcing && Game_Mapvars.Instance != null && YosseleUtil.IsServer && Game_Mapvars.Instance.Time_Wave.Value < 600f)
            Game_Mapvars.Instance.Time_Wave.Value = 900f;
        if (forcing && Game_Mapvars.Instance != null && YosseleUtil.IsServer)
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
#if YOSSELE_SHOTS
            if (!boatShot && now - seen >= 4f && Macro_Worldvisual.Instance != null)
            {
                boatShot = true;
                YosseleMod.Instance.StartCoroutine(BoatShot());
            }
            if (now - seen < 16f)
                return;
#else
            if (now - seen < 8f)
                return;
#endif
            if (step < 0f)
            {
                macro.Selectcharacter(YosseleData.Hero);
                node = PickNode();
                Log("picked Yossele and map node '" + node + "'");
                if (node != "")
                    Tds_Multiplayer_Remote.Instance.Selectmap_ServerRpc(node, false, default(ServerRpcParams));
                step = now;
                return;
            }
            if (now - step > 3f && now - step < 4f)
            {
                Tds_Clientinfo info = Blm_Server.Instance.Localplayer.GetComponent<Tds_Clientinfo>();
                Check("vessel select", info != null && info.Hero.Value.ToString() == YosseleData.Hero, info != null ? info.Hero.Value.ToString() : "no clientinfo");
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

        if (phase <= 2 && YosseleServer.Heroes.Count > 0 && Game_Mapvars.Instance != null && YosseleUtil.IsServer)
        {
            phase = 3;
            YosseleMod.Instance.StartCoroutine(Scenario());
        }
    }

#if YOSSELE_SHOTS
    static bool boatShot;

    // The boat's vessel select, opened as the Setup screen's button does, captured with Yossele standing on it.
    static IEnumerator BoatShot()
    {
        Macro_Worldvisual.Instance.Opencharselect();
        yield return new WaitForSecondsRealtime(3f);
        Macro_Characterselect_Click mine = null;
        int buttons = 0;
        foreach (Macro_Characterselect_Click b in UnityEngine.Object.FindObjectsOfType<Macro_Characterselect_Click>())
        {
            buttons++;
            if (b.Character == YosseleData.Hero) mine = b;
        }
        Dictionary<string, UnityEngine.Object> heroes = YosseleUtil.Folder("Hero");
        Check("Yossele unlocked on the boat's vessel select", mine != null && !mine.Locked, mine == null ? "no button" : "locked " + mine.Locked);
        Check("every vessel has a boat spot", buttons == heroes.Count, buttons + " buttons, " + heroes.Count + " vessels");
        Transform spots = Macro_Worldvisual.Instance.Boat.transform.Find("Charselect/Spots");
        float zMin = float.MaxValue, zMax = float.MinValue, room = 0.01f;
        bool onDeck = spots != null;
        if (spots != null)
        {
            Transform home = ModKitBoat.Home(spots);
            foreach (Transform s in home) { zMin = Mathf.Min(zMin, s.localPosition.z); zMax = Mathf.Max(zMax, s.localPosition.z); }
            // Kit 1.4 stands a crowded boat's end vessels 0.2 mean shipped gaps past the outer shipped spots (ModKitBoat
            // EndRoom; 0.5 put them in the hull): up to a quarter gap is still deck.
            if (home.childCount > 1) room += 0.25f * (zMax - zMin) / (home.childCount - 1);
            foreach (Transform s in spots) if (s.localPosition.z < zMin - room || s.localPosition.z > zMax + room) onDeck = false;
        }
        Check("boat spots stand on the deck, between the shipped ends", onDeck, spots != null ? spots.childCount + " spots" : "no spots");
        int unreachable;
        string clicks = ModKitBoat.ClickReport(out unreachable);
        Check("every vessel on the boat can be clicked", unreachable == 0, clicks);
        yield return YosseleShots.Shot("boat");
        if (mine != null)
        {
            Macro_Main.Instance.Selectcharacter(YosseleData.Hero);
            yield return new WaitForSecondsRealtime(1.5f);
            yield return YosseleShots.Shot("boat_selected");
        }
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

    static readonly List<Vector3> used = new List<Vector3>();

    // A free tile at about 'ring' tiles from the vessel where this being can be placed (kit ModKitTest.Spot: every level of
    // each column out to 8 tiles; when there is none it logs why and a picture of the ground).
    static Vector3 Spot(Entity_Master hero, Beingobject being, float ring)
    {
        return ModKitTest.Spot(YosseleUtil.Pos(hero), being, YosseleUtil.Team(hero), ring, 8f, used, being.Objectname);
    }

    static T Newest<T>(string being, List<YosseleBuilding> known) where T : YosseleBuilding
    {
        foreach (YosseleBuilding b in YosseleServer.Buildings)
            if (b != null && b.Started && b.Def.Id == being && !known.Contains(b))
                return b as T;
        return null;
    }

    static IEnumerator Build(Entity_Master hero, string tower, Vector3 at, List<YosseleBuilding> known, YosseleBuilding[] result, int orientation = 0)
    {
        result[0] = null;
        string json = "{\"Tower\":\"" + tower + "\",\"Position\":" + Vec(at) + ",\"Orientation\":" + orientation + ",\"Owner\":" + YosseleUtil.Owner(hero) + "}";
        Send(hero, "Build", json);
        float t = 0f;
        while (t < 20f && result[0] == null)
        {
            result[0] = Newest<YosseleBuilding>(tower, known);
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
        ClearFoes();
    }

    static int Team() { return YosseleServer.Heroes.Count > 0 ? YosseleUtil.Team(YosseleServer.Heroes[0].Master) : 1; }

    // Removes Tide foes so they don't pile up (not a kill: no Scraps).
    static void ClearFoes()
    {
        int team = Team();
        foreach (Entity_Master e in new List<Entity_Master>(Game_Entity.Instance.Entityclient))
            if (YosseleUtil.IsFoe(e, team))
                Game_Entity.Instance.Despawn(e.Beingserver, false, "Selftest");
    }

    // Spawned as a child of the enemy core, like Tide foes (kit ModKitTest.SpawnFoe): the nearest ground tile between minDist
    // and maxDist from 'near' where the foe can stand; building tiles are skipped unless asked for (the Curtain test spawns
    // right on it).
    static Entity_Master SpawnFoe(string being, Vector3 near, float maxDist, float minDist = 0f, bool onBuildings = false)
    {
        return ModKitTest.SpawnFoe(being, near, minDist, maxDist, onBuildings ? null : used);
    }

    // Where the game attached one of Yossele's debuff effects (Helper_Vfxtools.Debuff parents it under the foe): logged so
    // the effect's placement can be checked without guessing from screenshots.
    static IEnumerator DebuffReport(Entity_Master foe, string vfx)
    {
        // The effect arrives by client RPC: give it a moment.
        float until = Time.time + 0.6f;
        while (foe != null && Time.time < until && !HasChild(foe.transform, vfx))
            yield return null;
        if (foe == null) { Log("debuff " + vfx + ": no foe"); Check("the game attaches " + vfx + " to the foe", false); yield break; }
        bool found = false;
        foreach (Transform t in foe.GetComponentsInChildren<Transform>(true))
        {
            if (t.name != vfx && !t.name.StartsWith(vfx)) continue;
            found = true;
            string path = t.name;
            for (Transform p = t.parent; p != null && p != foe.transform; p = p.parent) path = p.name + "/" + path;
            List<string> rs = new List<string>();
            foreach (Renderer r in t.GetComponentsInChildren<Renderer>(true))
                rs.Add(r.GetType().Name + (r.enabled ? "" : "(off)") + " y" + r.bounds.min.y.ToString("F2") + ".." + r.bounds.max.y.ToString("F2") + " mat " + (r.sharedMaterial != null ? r.sharedMaterial.name : "-"));
            Log("debuff " + vfx + " at " + path + " active " + t.gameObject.activeInHierarchy + " local " + t.localPosition.ToString("F2") + " scale " + t.lossyScale.ToString("F2")
                + " foe pos " + YosseleUtil.Pos(foe).ToString("F2") + " renderers: " + string.Join("; ", rs.ToArray()));
        }
        Check("the game attaches " + vfx + " to the foe", found);
    }
    static bool HasChild(Transform root, string name)
    {
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            if (t.name == name) return true;
        return false;
    }
    static float BaseStat(Entity_Master e, string name)
    {
        Entityobject ent = e != null && e.Beingserver != null ? e.Beingserver.Entity : null;
        if (ent != null && ent.Defaultstat != null)
            foreach (Statproperties.Defaultstatgroup g in ent.Defaultstat.Stats)
                if (g.Name == name) return g.Value;
        return 1f;
    }
    static void Kill(Entity_Master e) { if (e != null && e.Beingserver != null) Game_Entity.Instance.Despawn(e.Beingserver, true, "Selftest"); }

    static IEnumerator Upgrade(YosseleBuilding b, string key, long owner)
    {
        string id = b.Def.Id + "_" + key + "_Upgradeobject";
        Send(b.Master, "Upgrade", "{\"Upgrade\":\"" + id + "\",\"Upgrader\":" + owner + "}");
        yield return Wait(0.6f);
    }

    static bool Has(YosseleBuilding b, string key) { return b != null && b.Master != null && b.Master.Upgradelistserver.ContainsKey(b.Def.Id + "_" + key + "_Upgradeobject"); }

    static List<Logitems.Base> AllItems(List<Logitems.Base> items) { return ModKitVesselJournal.Flat(items); }

    // ---------------------------------------------------------------- small scenario helpers

    static void SetHealth(Entity_Master e, float value)
    {
        Link_Stats s;
        if (e != null && e.Statsserver != null && e.Statsserver.TryGetValue("Health", out s))
            s.Value = Mathf.Clamp(value, 1f, s.Value_Calc);
    }

    static string Skin(Entity_Master e) { return e != null ? e.Skin.Value.ToString() : "-"; }

    static float SpeedOf(Entity_Master e) { return YosseleUtil.Stat(e, "Speed", 1f); }

    static IEnumerator Fell(string being, Vector3 near, float maxDist, float minDist, int count, int[] felled)
    {
        felled[0] = 0;
        for (int i = 0; i < count; i++)
        {
            Entity_Master foe = SpawnFoe(being, near, maxDist, minDist);
            if (foe == null) continue;
            yield return Wait(0.3f);
            Kill(foe);
            felled[0]++;
            yield return Wait(0.2f);
        }
        yield return Wait(0.4f);
    }

    static float NearestVine(YosseleHengeBrain henge, Vector3 at)
    {
        float best = 99f;
        foreach (Genitem g in henge.Tiles) best = Mathf.Min(best, YosseleUtil.Flat(g.Position, at));
        return best;
    }

    // Tide foes keep coming while the test runs and can fell a building; each section starts by saying which still stand.
    static void Standing(Dictionary<string, YosseleBuilding> built)
    {
        List<string> gone = new List<string>();
        foreach (KeyValuePair<string, YosseleBuilding> kv in built)
            if (kv.Value == null || !YosseleUtil.Alive(kv.Value.Master)) gone.Add(kv.Key);
        if (gone.Count > 0) Log("buildings gone before this step: " + YosseleUtil.Join(gone));
    }

    static Entity_Master NearestOre(Vector3 at, float reach)
    {
        Entity_Master best = null;
        float bestD = reach;
        foreach (Entity_Master e in YosseleUtil.Area(at, reach, YosseleUtil.Tagged("Resource")))
        {
            if (e == null || !YosseleUtil.Alive(e) || e.Statsserver == null || !e.Statsserver.ContainsKey("Healthresource"))
                continue;
            float d = YosseleUtil.Flat(YosseleUtil.Pos(e), at);
            if (d < bestD) { bestD = d; best = e; }
        }
        return best;
    }

    // ---------------------------------------------------------------- the scenario

    static IEnumerator Scenario()
    {
        Log("match started: scenario begins");
        YosseleHeroBrain hero = YosseleServer.Heroes[0];
        yield return Until(() => Game_Mapvars.Instance != null && Game_Mapvars.Instance.Wave.Value >= 1, 150f);
        freeze = true;
        yield return Wait(2f);
        ClearFoes();
        Entity_Master hm = hero.Master;
        long owner = YosseleUtil.Owner(hm);
        int team = YosseleUtil.Team(hm);
        YosselePlayer player = YosseleServer.Player(owner);
        Log("tide " + Game_Mapvars.Instance.Wave.Value + ", vessel at " + YosseleUtil.Pos(hm) + ", owner " + owner + ", team " + team);
        ModKitTest.Log = Log;
        ModKitTest.MapDump(team, hm);

        // 1. Vessel: the stock melee AI, tooltip, stats, four build options, no FERTILIZE yet, regeneration.
        Check("vessel uses the stock melee AI", hm.GetComponent<Entity_Ai_Mob_Combat>() != null && hm.GetComponent<Entity_Ai_Mob_Ranged>() == null && hm.GetComponent<Entity_Ai_Hero_Buildsell>() != null);
        Check("vessel has the Patient Growth tooltip", hm.Upgradelistserver.ContainsKey(YosseleData.HeroTooltip));
        Check("vessel stats", Mathf.Approximately(YosseleUtil.MaxHealth(hm), 60f) && Mathf.Approximately(YosseleUtil.Stat(hm, "Range", 0), 1.5f) && Mathf.Approximately(YosseleUtil.Stat(hm, "Damage", 0), 4.5f),
            "max health " + YosseleUtil.MaxHealth(hm) + ", Range " + YosseleUtil.Stat(hm, "Range", 0) + ", Damage " + YosseleUtil.Stat(hm, "Damage", 0) + ", Speed " + YosseleUtil.Stat(hm, "Speed", 0));
        int options = 0;
        for (int i = 0; i < 4; i++)
        {
            Link_Command c;
            if (hm.Commandlistserver.TryGetValue("Buildoption" + (i + 1), out c) && c.Goalstring == YosseleData.Towers[i]) options++;
        }
        Check("four build options", options == 4, options + "/4");
        Check("no FERTILIZE button without a Compost Bin", !hm.Commandlistserver.ContainsKey(YosseleData.FertilizeLink) && hero.Rank == 0);
        Check("vessel visible to clients", hm.Client != null && hm.Client.Beingobject != null && hm.Client.Beingobject.Objectname == YosseleData.Hero);
        {
            float max = YosseleUtil.MaxHealth(hm);
            SetHealth(hm, max - 20f);
            float h0 = YosseleUtil.Health(hm);
            yield return Wait(3.2f);
            float h1 = YosseleUtil.Health(hm);
            Check("Yossele mends about " + YosseleData.Regen + " health a second", h1 >= h0 + 2f * YosseleData.Regen && h1 <= h0 + 4.5f * YosseleData.Regen, F(h0) + "->" + F(h1));
            SetHealth(hm, max);
        }

        // 2. Building all four through the vessel's real Build command; they start at Growth 0.
        Helper_Resources.Giveresources(YosseleUtil.Costs("Redaura=20000;Blueaura=20000;Yellowaura=20000"), owner, Helper_Resources.Styles.Default);
        yield return Wait(0.5f);
        List<YosseleBuilding> known = new List<YosseleBuilding>(YosseleServer.Buildings);
        Dictionary<string, YosseleBuilding> built = new Dictionary<string, YosseleBuilding>();
        YosseleBuilding[] got = new YosseleBuilding[1];
        float[] rings = { 3.2f, 2.2f, 2.2f, 3.2f };
        for (int i = 0; i < YosseleData.Towers.Length; i++)
        {
            string tower = YosseleData.Towers[i];
            Beingobject being = YosseleContent.Beings[tower];
            Vector3 at;
            // The Sapling goes beside an ore node when there is one nearby, so it has something to mine.
            Entity_Master ore = tower == YosseleData.Pachira ? NearestOre(YosseleUtil.Pos(hm), 14f) : null;
            if (ore != null)
                at = ModKitTest.Spot(YosseleUtil.Pos(ore), being, team, 1.2f, 3f, used, being.Objectname);
            else
                at = Spot(hm, being, rings[i]);
            if (float.IsNaN(at.x)) { ModKitTest.Setup("build " + tower, "no free tile"); continue; }
            Link_Resourceset cost = being.Entity.Costs[0];
            float before = Res(owner, cost.Name);
            yield return Build(hm, tower, at, known, got);
            if (!Check("build " + tower, got[0] != null, "at " + at))
                continue;
            built[tower] = got[0];
            yield return Wait(0.3f);
            float spent = before - Res(owner, cost.Name);
            Check(tower + " costs " + cost.Amount + " " + cost.Name, Mathf.Abs(spent - cost.Amount) < 0.01f, "spent " + spent);
            Check(tower + " visible to clients", got[0].Master.Client != null && got[0].Master.Client.Beingobject != null && got[0].Master.Client.Beingobject.Objectname == tower);
            Check(tower + " starts at Growth 0", got[0].Growth == 0 && Mathf.Approximately(YosseleUtil.Stat(got[0].Master, YosseleData.GrowthStat, -1f), 0f), "Growth " + got[0].Growth);
        }
        YosseleCannonBrain cannon = built.ContainsKey(YosseleData.Cannon) ? (YosseleCannonBrain)built[YosseleData.Cannon] : null;
        YosseleDandelionBrain tree = built.ContainsKey(YosseleData.Dandelion) ? (YosseleDandelionBrain)built[YosseleData.Dandelion] : null;
        YosseleHengeBrain henge = built.ContainsKey(YosseleData.Henge) ? (YosseleHengeBrain)built[YosseleData.Henge] : null;
        YosselePachiraBrain sapling = built.ContainsKey(YosseleData.Pachira) ? (YosselePachiraBrain)built[YosseleData.Pachira] : null;
#if YOSSELE_SHOTS
        yield return Wait(1.5f);
        yield return YosseleShots.Shot("built");
#endif

        // 3. The Dandelion Tree is one per player: its button goes while it stands, a second can't be built.
        if (tree != null)
        {
            yield return Wait(0.6f);
            Check("the Dandelion Tree's button goes while one stands", !hm.Commandlistserver.ContainsKey(YosseleData.DandelionOption), "hidden " + hero.DandelionHidden);
            Beingobject being = YosseleContent.Beings[YosseleData.Dandelion];
            Vector3 at = Spot(hm, being, 3.2f);
            if (!float.IsNaN(at.x))
            {
                yield return Build(hm, YosseleData.Dandelion, at, known, got);
                Check("a second Dandelion Tree can't be built", got[0] == null);
                if (got[0] != null) Kill(got[0].Master);
            }
        }

        // 4. Growth: a building grows a stage at the start of each Tide it stood through entirely.
        {
            int[] reached = new int[1];
            int built0 = Game_Mapvars.Instance.Wave.Value;
            yield return ForceTide(reached);
            int g1 = cannon != null ? cannon.Growth : -1;
            Check("no Growth at the first Tide after building (it was built mid-Tide)", cannon == null || g1 == 0, "Tide " + built0 + "->" + reached[0] + ", Growth " + g1);
            yield return ForceTide(reached);
            int grown = 0, total = 0;
            foreach (YosseleBuilding b in built.Values)
            {
                if (b == null) continue;
                total++;
                if (b.Growth == 1 && Skin(b.Master) == YosseleData.GrowthStat + "1" && Mathf.Approximately(YosseleUtil.Stat(b.Master, YosseleData.GrowthStat, -1f), 1f)) grown++;
                else Log(b.Def.Id + ": Growth " + b.Growth + ", skin '" + Skin(b.Master) + "', counter " + YosseleUtil.Stat(b.Master, YosseleData.GrowthStat, -1f));
            }
            Check("every building grew one stage after a whole Tide (counter and skin)", total > 0 && grown == total, grown + "/" + total + ", Tide " + reached[0]);
            if (cannon != null)
            {
                cannon.SetGrowth(cannon.Max);
                yield return ForceTide(reached);
                Check("Growth stops at the building's maximum", cannon.Growth == cannon.Max, cannon.Growth + "/" + cannon.Max);
            }
        }

        // 5. FERTILIZE: granted by the Compost Bin Direction, fed by foes felled near Yossele, grows a building a stage.
        {
            bool granted = ModKitDirections.Grant(YosseleContent.Compostbins[0]);
            yield return Until(() => hm.Commandlistserver.ContainsKey(YosseleData.FertilizeLink), 3f);
            Check("a Compost Bin unlocks FERTILIZE", granted && hero.Rank == 1 && hm.Commandlistserver.ContainsKey(YosseleData.FertilizeLink), "granted " + granted + ", rank " + hero.Rank);
            Link_Command fert;
            Check("FERTILIZE is the building picker", hm.Commandlistserver.TryGetValue(YosseleData.FertilizeLink, out fert) && fert.Controllable && YosseleContent.Fertilize.Commandfuncts[0] is YosseleFertilizeSelect);
            player.Fertilizer = 0;
            hero.ShowFertilizer();
            YosseleBuilding target = tree ?? (YosseleBuilding)henge ?? sapling;
            int f0 = hero.Fertilizes;
            if (target != null)
            {
                Send(hm, YosseleData.FertilizeLink, "{\"Ref\":" + target.Master.NetworkObjectId + "}");
                yield return Wait(0.7f);
                Check("FERTILIZE does nothing without fertilizer", hero.Fertilizes == f0);
            }
            int need = YosseleData.Compostbins[0].Kills;
            int[] felled = new int[1];
            yield return Fell("Enemy_Dagger", YosseleUtil.Pos(hm), 2.5f, 0f, need, felled);
            Check(need + " foes felled near Yossele make a fertilizer", felled[0] == need && player.Fertilizer == 1 && Mathf.Approximately(YosseleUtil.Stat(hm, YosseleData.FertilizerStat, -1f), 1f),
                felled[0] + " felled, fertilizer " + player.Fertilizer + ", counter " + YosseleUtil.Stat(hm, YosseleData.FertilizerStat, -1f));
            float radius = YosseleData.Compostbins[0].Radius;
            yield return Fell("Enemy_Dagger", YosseleUtil.Pos(hm), radius + 8f, radius + 1.5f, 3, felled);
            if (felled[0] > 0) Check("foes felled far from Yossele make none", player.Compost == 0, "compost " + player.Compost + " after " + felled[0]);
            else ModKitTest.Setup("far foes", "no ground for a foe " + F(radius + 1.5f) + "-" + F(radius + 8f) + " tiles from Yossele");
            if (target != null && player.Fertilizer > 0)
            {
                int g0 = target.Growth;
                Send(hm, YosseleData.FertilizeLink, "{\"Ref\":" + target.Master.NetworkObjectId + "}");
                yield return Wait(0.7f);
                Check("FERTILIZE grows the picked building a stage and spends a fertilizer", target.Growth == g0 + 1 && player.Fertilizer == 0 && target.Fertilized == 1,
                    "Growth " + g0 + "->" + target.Growth + ", fertilizer " + player.Fertilizer);
            }
            if (cannon != null)
            {
                player.Fertilizer = 3;
                int fz = hero.Fertilizes;
                Send(hm, YosseleData.FertilizeLink, "{\"Ref\":" + cannon.Master.NetworkObjectId + "}");
                yield return Wait(0.7f);
                Check("FERTILIZE refuses a fully grown building", hero.Fertilizes == fz && player.Fertilizer == 3);
            }
            player.Fertilizer = YosseleData.FertilizerMax;
            YosseleServer.AddFertilizer(owner, 5);
            Check("fertilizer stops at " + YosseleData.FertilizerMax, player.Fertilizer == YosseleData.FertilizerMax, "" + player.Fertilizer);
            player.Fertilizer = 0;
            hero.ShowFertilizer();
        }

        ClearFoes();
        Standing(built);
        // 6. Mushroom Cannon: lobs land, spore stacks build to their cap and rot foes; fully grown, the lobs bounce.
        if (cannon != null)
        {
            Vector3 cp = YosseleUtil.Pos(cannon.Master);
            Entity_Master foe = SpawnFoe("Enemy_Aegis", cp, 5f, 3f);
            int blasts = cannon.Blasts, bounces = cannon.Bounces;
            yield return Until(() => foe == null || !YosseleUtil.Alive(foe) || YosseleSpores.StacksOf(foe) > 0, 8f);
            Check("the Cannon lobs a spore bomb that lands", foe != null && cannon.Lobs > 0 && cannon.Blasts > blasts, "lobs " + cannon.Lobs + ", blasts " + blasts + "->" + cannon.Blasts);
            Check("a blast spores the foe", foe != null && YosseleSpores.StacksOf(foe) > 0, "stacks " + YosseleSpores.StacksOf(foe));
            yield return DebuffReport(foe, YosseleData.VfxSpores);
            if (foe != null && YosseleUtil.Alive(foe))
            {
                float h0 = YosseleUtil.Health(foe);
                int ticks = YosseleSpores.Ticks;
                yield return Wait(1.2f);
                Check("spores rot the foe over time", YosseleSpores.Ticks > ticks && (!YosseleUtil.Alive(foe) || YosseleUtil.Health(foe) < h0), F(h0) + "->" + F(YosseleUtil.Health(foe)));
                YosseleSpores.Add(foe, 50, cannon);
                Check("spores stop at " + YosseleData.SporeMax + " stacks", YosseleSpores.StacksOf(foe) == YosseleData.SporeMax && YosseleSpores.IsFull(foe), "" + YosseleSpores.StacksOf(foe));
                yield return DebuffReport(foe, YosseleData.VfxSporefull);
            }
            yield return Until(() => cannon.Bounces > bounces, 6f);
            Check("a fully grown Cannon's lobs bounce", cannon.FullGrown && cannon.Bounces > bounces, "bounces " + bounces + "->" + cannon.Bounces);
#if YOSSELE_SHOTS
            yield return YosseleShots.Close("cannon", (cp + (foe != null ? YosseleUtil.Pos(foe) : cp)) * 0.5f, 6f);
#endif
            Kill(foe);
            yield return Wait(0.4f);

            // Upgrades: Sticky Spores -> Viral Outbreak -> Explosive Finish.
            float red = Res(owner, "Redaura");
            yield return Upgrade(cannon, "Sticky2", owner);
            Check("Sticky Spores bought and paid", Has(cannon, "Sticky2") && Mathf.Abs((red - Res(owner, "Redaura")) - 30f) < 0.01f, "Red spent " + (red - Res(owner, "Redaura")));
            yield return Upgrade(cannon, "Finish4", owner);
            Check("a tier-4 upgrade is not offered from tier 2", !Has(cannon, "Finish4"));
            Entity_Master slowed = SpawnFoe("Enemy_Aegis", cp, 6f, 4f);
            yield return Wait(0.5f);
            if (slowed != null)
            {
                float s0 = SpeedOf(slowed);
                YosseleSpores.Add(slowed, YosseleData.SporeMax, cannon);
                yield return Wait(0.3f);
                Check("Sticky Spores slow a fully spored foe", YosseleSpores.Of(slowed) != null && YosseleSpores.Of(slowed).Sticky != null && SpeedOf(slowed) < s0 - 0.05f, F(s0) + "->" + F(SpeedOf(slowed)));
                Kill(slowed);
            }
            yield return Upgrade(cannon, "Viral3", owner);
            yield return Upgrade(cannon, "Finish4", owner);
            Check("Sticky -> Viral -> Explosive Finish", Has(cannon, "Viral3") && Has(cannon, "Finish4"));
            Entity_Master a = SpawnFoe("Enemy_Aegis", cp, 6f, 4f), b = a != null ? SpawnFoe("Enemy_Dagger", YosseleUtil.Pos(a), 1f) : null;
            yield return Wait(0.5f);
            if (a != null)
            {
                int finishes = YosseleSpores.Finishes;
                YosseleSpores.Add(a, YosseleData.SporeMax, cannon);
                yield return Wait(0.5f);
                Check("Explosive Finish: full spores burst", YosseleSpores.Finishes > finishes && YosseleSpores.StacksOf(a) < YosseleData.SporeMax,
                    "finishes " + finishes + "->" + YosseleSpores.Finishes + ", stacks left " + YosseleSpores.StacksOf(a) + ", neighbour " + YosseleSpores.StacksOf(b));
            }
            Kill(a); Kill(b);
            Log("cannon: Viral bursts so far " + YosseleSpores.Virals + " (a 30% chance, not checked)");
        }

        ClearFoes();
        Standing(built);
        // 7. A second Cannon down the other branch: Farout Fungus -> Deathcap Training (fewer, deadlier stacks) ->
        // Hallucinogen.
        {
            Beingobject being = YosseleContent.Beings[YosseleData.Cannon];
            Vector3 at = Spot(hm, being, 4f);
            YosseleCannonBrain second = null;
            if (!float.IsNaN(at.x))
            {
                yield return Build(hm, YosseleData.Cannon, at, known, got);
                second = got[0] as YosseleCannonBrain;
            }
            if (second != null)
            {
                float range = second.Stat("Range", 0f);
                yield return Upgrade(second, "Farout2", owner);
                yield return Upgrade(second, "Deathcap3", owner);
                yield return Upgrade(second, "Hallucinogen4", owner);
                Check("Farout Fungus adds range", Has(second, "Farout2") && second.Stat("Range", 0f) >= range + 1.9f, F(range) + "->" + F(second.Stat("Range", 0f)));
                Check("Deathcap Training: spores cap at 8 and hurt more", Has(second, "Deathcap3") && second.SporeMax == 8 && second.SporeDps > YosseleData.SporeDps, "max " + second.SporeMax + ", dps " + F(second.SporeDps));
                Check("Hallucinogen bought", Has(second, "Hallucinogen4"));
                Entity_Master foe = SpawnFoe("Enemy_Aegis", YosseleUtil.Pos(second.Master), 5f, 3f);
                yield return Wait(0.4f);
                if (foe != null)
                {
                    YosseleSpores.Add(foe, 20, second);
                    Check("Deathcap stacks stop at 8", YosseleSpores.StacksOf(foe) == 8, "" + YosseleSpores.StacksOf(foe));
                }
                Kill(foe);
                Log("cannon: Hallucinogen dazes so far " + YosseleSpores.Dazes + " (a 20% chance every 5 s, not checked)");
            }
            else
                ModKitTest.Setup("second Cannon", "could not build one");
        }

        ClearFoes();
        Standing(built);
        // 8. Primordial Dandelion Tree: seeds grow, all strike one foe at once; Growth adds seeds and range.
        if (tree != null && YosseleUtil.Alive(tree.Master))
        {
            tree.SetGrowth(0);
            yield return Wait(0.3f);
            Check("the Tree holds " + 6 + " seeds at Growth 0", tree.Limit == 6, "limit " + tree.Limit);
            Check("the Tree's range at Growth 0 is 6 tiles (twice the first build's)", Mathf.Abs(tree.Stat("Range", 0f) - 6f) < 0.01f, "range " + F(tree.Stat("Range", 0f)));
            float range0 = tree.Stat("Range", 0f);
            tree.SetGrowth(2);
            yield return Wait(0.3f);
            Check("each stage adds " + YosseleData.SeedsPerStage + " seeds and range", tree.Limit == 6 + 2 * YosseleData.SeedsPerStage && tree.Stat("Range", 0f) > range0 + 0.5f,
                "limit " + tree.Limit + ", range " + F(range0) + "->" + F(tree.Stat("Range", 0f)));
            Check("the seed counter shows on the Tree", YosseleUtil.Stat(tree.Master, "Seeds", -1f) >= 0f, "Seeds " + YosseleUtil.Stat(tree.Master, "Seeds", -1f));
            yield return Upgrade(tree, "Packets2", owner);
            tree.Ready = tree.Limit;
            Entity_Master foe = SpawnFoe("Enemy_Dagger", YosseleUtil.Pos(tree.Master), tree.Stat("Range", 3f) - 0.5f, 1f);
            int strikes = tree.Strikes;
            yield return Until(() => tree.Strikes > strikes, 5f);
            Check("the Tree lets all its seeds go at a foe in range", foe != null && tree.Strikes > strikes && tree.SeedsFlown >= 1, "strikes " + strikes + "->" + tree.Strikes + ", seeds flown " + tree.SeedsFlown + ", ready " + F(tree.Ready));
#if YOSSELE_SHOTS
            yield return YosseleShots.Close("dandelion", YosseleUtil.Pos(tree.Master), 5f);
#endif
            yield return Wait(1f);
            if (tree.Refunded > 0) Check("Seed Packets: seeds that fell a foe come back", true, "refunded " + tree.Refunded);
            else ModKitTest.Setup("Seed Packets", "no seed felled a foe this time (refunded 0)");
            Kill(foe);
            float r0 = tree.Rate;
            int strikes1 = tree.Strikes;
            tree.Ready = 0f;
            yield return Until(() => tree == null || tree.Ready >= 0.5f || tree.Strikes > strikes1, 4f);
            if (tree == null) { ModKitTest.Setup("seed regrow", "the Tree was felled during the check"); }
            else {
            Link_Stats lim;
            tree.Master.Statsserver.TryGetValue("Seedlimit", out lim);
            Check("seeds grow back over time", tree.Ready >= 0.5f || tree.Strikes > strikes1, "ready " + F(tree.Ready) + ", rate " + F(r0) + ", limit " + tree.Limit
                + " (Seedlimit " + (lim != null ? F(lim.Value) + "/" + F(lim.Value_Calc) : "none") + "), brain on " + tree.enabled + ", alive " + YosseleUtil.Alive(tree.Master)
                + ", strikes " + strikes1 + "->" + tree.Strikes);
            }
        }

        ClearFoes();
        Standing(built);
        // 9. Overgrown Stonehenge: shows its facing like the Ice Wall, vines creep the way it faces, one tile per two
        // stages, and slow foes on them.
        {
            Entityobject he = YosseleContent.Beings[YosseleData.Henge].Entity;
            GameObject hit = he.Defaulthit != null ? he.Defaulthit.Default.Entityprefab : null;
            bool marker = hit != null && Extramethods.FindDeepChild(hit.transform, "Forward") != null;
            bool arrow = he.Visualscripts.Exists(x => x != null && x.name == "Entity_Vis_Tower_Orientation");
            Beingobject wall = YosseleUtil.Load<Beingobject>("Mob", "Tower_Snosnowall");
            string theirs = wall == null || wall.Entity == null ? "Ice Wall not found" : "Ice Wall scripts: " + YosseleUtil.Join(wall.Entity.Visualscripts.FindAll(x => x != null).ConvertAll(x => x.name));
            Check("the Stonehenge shows its facing like the Ice Wall (placing arrow, selected arrow)", he.Orientable && marker && arrow, "Forward from " + YosseleContent.ForwardFrom + "; " + theirs);
        }
        if (henge != null && YosseleUtil.Alive(henge.Master))
        {
            Vector3 hp = henge.Master.Beingserver.Currentblock != null ? henge.Master.Beingserver.Currentblock.Position : henge.Pos;
            henge.SetGrowth(0);
            yield return Wait(0.6f);
            int t0 = henge.Tiles.Count;
            bool ahead = true;
            foreach (Genitem g in henge.Tiles) if (Vector3.Dot(g.Position - hp, henge.Facing) <= 0.1f) ahead = false;
            Check("vines lie the way the Stonehenge faces", t0 >= 1 && ahead, t0 + " tiles, facing " + henge.Facing + ", vines spawned " + YosseleVines.Spawned + ", failed " + YosseleVines.Failed);
            henge.SetGrowth(4);
            yield return Wait(0.6f);
            Check("Growth lengthens the vines (1 + Growth/2 tiles)", henge.Length == 3 && henge.Tiles.Count > t0, "length " + henge.Length + ", tiles " + t0 + "->" + henge.Tiles.Count);
            Check("Growth hardens the stones", Mathf.Abs(YosseleHooks.ArmorOf(henge.Master) - 4 * YosseleData.HengeArmorPerStage) < 0.01f, "armor " + F(YosseleHooks.ArmorOf(henge.Master)));
            Check("vine tiles are networked beings", YosseleVines.Count >= henge.Tiles.Count, YosseleVines.Count + " vines");
            {
                GameObject vm = YosseleAssets.Model("Yossele_Vine");
                MeshFilter mf = vm != null ? vm.GetComponentInChildren<MeshFilter>(true) : null;
                MeshRenderer mr = mf != null ? mf.GetComponent<MeshRenderer>() : null;
                Texture tex = mr != null && mr.sharedMaterial != null && mr.sharedMaterial.HasProperty("_BaseMap") ? mr.sharedMaterial.GetTexture("_BaseMap") : null;
                bool flat = mf != null && mf.sharedMesh != null && mf.sharedMesh.bounds.size.y < 0.01f && mf.sharedMesh.vertexCount == 4;
                Check("vine tiles are a flat pixel texture on the ground, like Sno Sno's snow", flat && tex != null,
                    mf == null || mf.sharedMesh == null ? "no mesh" : "verts " + mf.sharedMesh.vertexCount + ", height " + F(mf.sharedMesh.bounds.size.y) + ", texture " + (tex != null ? tex.name : "none")
                    + ", material " + (mr != null && mr.sharedMaterial != null ? mr.sharedMaterial.name + " / " + mr.sharedMaterial.shader.name : "none"));
                // What a live vine tile draws with, next to Sno Sno's snow (to match its look by eye later).
                foreach (Entity_Master e in Game_Entity.Instance.Entityclient)
                    if (e != null && e.Being.Value.ToString() == YosseleData.Vine)
                    {
                        foreach (Renderer r in e.GetComponentsInChildren<Renderer>(true))
                            Log("vine tile renderer " + r.name + " enabled " + r.enabled + " material " + (r.sharedMaterial != null ? r.sharedMaterial.name + " / " + r.sharedMaterial.shader.name : "none") + " y " + F(r.bounds.min.y) + ".." + F(r.bounds.max.y) + ", tile y " + F(YosseleUtil.Pos(e).y));
                        break;
                    }
                Beingobject snow = YosseleUtil.Load<Beingobject>("Mob", "Effect_Snosnosnow");
                GameObject sv = snow != null && snow.Entity != null && snow.Entity.Defaultvisual != null ? snow.Entity.Defaultvisual.Default.Visprefab : null;
                if (sv != null)
                    foreach (Renderer r in sv.GetComponentsInChildren<Renderer>(true))
                        Log("snow renderer " + r.name + " " + r.GetType().Name + " material " + (r.sharedMaterial != null ? r.sharedMaterial.name + " / " + r.sharedMaterial.shader.name + " / texture " + (r.sharedMaterial.mainTexture != null ? r.sharedMaterial.mainTexture.name : "none") : "none") + " size " + r.bounds.size.ToString("F2"));
                else
                    Log("snow visual not found");
            }
            Genitem vine = henge.Tiles.Count > 0 ? henge.Tiles[henge.Tiles.Count - 1] : null;
            Entity_Master foe = vine != null ? SpawnFoe("Enemy_Aegis", vine.Position, 0.6f, 0f, true) : null;
            float s0 = foe != null ? BaseStat(foe, "Speed") : 1f;
            yield return Until(() => foe == null || YosseleVines.IsSlowed(foe), 2f);
            Check("a foe on the vines is slowed", foe != null && YosseleVines.IsSlowed(foe) && SpeedOf(foe) < s0 - 0.05f, "speed " + F(s0) + "->" + F(SpeedOf(foe)));
            yield return DebuffReport(foe, YosseleData.VfxSlow);
#if YOSSELE_SHOTS
            yield return YosseleShots.Close("henge", hp + henge.Facing * 1.5f, 5f);
#endif
            // A real hit through the hit hook: 40% armour at Growth 4.
            Entity_Master hitter = foe != null && YosseleUtil.Alive(foe) ? foe : hm;
            SetHealth(henge.Master, YosseleUtil.MaxHealth(henge.Master));
            float hh0 = YosseleUtil.Health(henge.Master);
            int softened = YosseleHooks.Softened;
            YosseleUtil.Hurt(hitter, henge.Master, 10f);
            yield return Wait(0.3f);
            float lost = hh0 - YosseleUtil.Health(henge.Master);
            Check("Growth's armour softens a real hit (10 -> 6)", YosseleHooks.Softened > softened && lost > 5.5f && lost < 6.5f, "lost " + F(lost) + ", softened " + softened + "->" + YosseleHooks.Softened + ", skipped " + YosseleHooks.Skipped);
            Kill(foe);
            yield return Wait(0.6f);

            int before = henge.Tiles.Count;
            yield return Upgrade(henge, "Verdant2", owner);
            yield return Wait(0.6f);
            Check("Verdant Spread: vines all around", Has(henge, "Verdant2") && henge.Tiles.Count > before, before + "->" + henge.Tiles.Count + " tiles");
            yield return Upgrade(henge, "Carnivore3", owner);
            yield return Upgrade(henge, "Spaghetti4", owner);
            Check("Verdant -> Carnivore -> Spaghetti", Has(henge, "Carnivore3") && Has(henge, "Spaghetti4"));
            vine = henge.Tiles.Count > 0 ? henge.Tiles[0] : null;
            foe = vine != null ? SpawnFoe("Enemy_Aegis", vine.Position, 0.6f, 0f, true) : null;
            int drained = henge.Drained, snares = henge.Snares;
            // Held on the vines (no stun, no look) so it lingers the 7 s Spaghetti Roots waits for.
            if (foe != null) YosseleUtil.Root(henge.Master, foe, 10f, false, "");
            yield return Until(() => foe == null || henge.Drained > drained, 3f);
            Check("Opportunistic Carnivore drains a foe on the vines", foe != null && henge.Drained > drained, "drains " + drained + "->" + henge.Drained);
            // The 7 s wait is cut short: once the foe counts as on the vines, its start is moved back 6.5 s.
            yield return Until(() => foe == null || henge.OnSince.ContainsKey(foe), 2f);
            float since0;
            bool counted = foe != null && henge.OnSince.TryGetValue(foe, out since0);
            Log("Spaghetti: foe on the vines " + counted + ", nearest vine " + (foe != null ? F(NearestVine(henge, YosseleUtil.Pos(foe))) : "-") + " tiles, speed " + F(SpeedOf(foe)));
            if (counted) henge.OnSince[foe] = Time.time - (henge.Param("Spaghetti_After", 7f) - 0.5f);
            yield return Until(() => foe == null || !YosseleUtil.Alive(foe) || henge.Snares > snares, 4f);
            if (henge.Snares > snares) Check("Entangling Spaghetti Roots snare a lingering foe", true, "snares " + henge.Snares);
            else ModKitTest.Setup("Spaghetti Roots", "the foe left the vines before " + F(henge.Param("Spaghetti_After", 7f)) + " s (on since: " + henge.OnSince.Count + ")");
            Kill(foe);
        }

        ClearFoes();
        Standing(built);
        // 10. A second Stonehenge, turned a quarter while placing, down the other branch: its model and vines face the turn
        // once (the game turns the model), Vine Blockade's barrier each Tide, Natural Remedy.
        {
            Beingobject being = YosseleContent.Beings[YosseleData.Henge];
            Vector3 at = Spot(hm, being, 4f);
            YosseleHengeBrain second = null;
            if (!float.IsNaN(at.x))
            {
                yield return Build(hm, YosseleData.Henge, at, known, got, 90);
                second = got[0] as YosseleHengeBrain;
            }
            if (second != null)
            {
                yield return Wait(0.6f);
                Vector3 sp = second.Master.Beingserver.Currentblock != null ? second.Master.Beingserver.Currentblock.Position : second.Pos;
                bool ahead = second.Tiles.Count > 0;
                foreach (Genitem g in second.Tiles) if (Vector3.Dot(g.Position - sp, second.Facing) <= 0.1f) ahead = false;
                Check("a Stonehenge turned while placing faces the turn, vines ahead", second.Orientation == 90 && Mathf.Abs(second.Facing.x) > 0.9f && ahead,
                    "orientation " + second.Orientation + ", facing " + second.Facing + ", " + second.Tiles.Count + " tiles");
                Transform front = Extramethods.FindDeepChild(second.Master.transform, "Facing");
                Vector3 look = front != null ? Vector3.ProjectOnPlane(front.forward, Vector3.up).normalized : Vector3.zero;
                Check("the turned model's front points along its vines (turned once, not twice)", front != null && Vector3.Dot(look, second.Facing) > 0.9f,
                    front == null ? "no Facing part" : "model front " + look.ToString("F2") + ", vines " + second.Facing);

                float max0 = YosseleUtil.MaxHealth(second.Master);
                yield return Upgrade(second, "Blockade2", owner);
                yield return Upgrade(second, "Remedy3", owner);
                Check("Vine Blockade: sturdier stones", Has(second, "Blockade2") && YosseleUtil.MaxHealth(second.Master) > max0 + 1f, F(max0) + "->" + F(YosseleUtil.MaxHealth(second.Master)));
                int[] reached = new int[1];
                yield return ForceTide(reached);
                Check("Vine Blockade raises a barrier each Tide", second.Barrier > 0f && YosseleHooks.ShieldOf(second.Master) > 0f, "barrier " + F(second.Barrier));
                yield return DebuffReport(second.Master, YosseleData.VfxBlockade);
                float shield0 = YosseleHooks.ShieldOf(second.Master), sh0 = YosseleUtil.Health(second.Master);
                int soaked = YosseleHooks.Soaked;
                YosseleUtil.Hurt(hm, second.Master, 10f);
                yield return Wait(0.3f);
                Check("the barrier soaks a real hit before the stones", YosseleHooks.Soaked > soaked && YosseleHooks.ShieldOf(second.Master) < shield0 - 1f && YosseleUtil.Health(second.Master) >= sh0 - 0.01f,
                    "barrier " + F(shield0) + "->" + F(YosseleHooks.ShieldOf(second.Master)) + ", health " + F(sh0) + "->" + F(YosseleUtil.Health(second.Master)));
                SetHealth(second.Master, YosseleUtil.MaxHealth(second.Master) * 0.5f);
                float h0 = YosseleUtil.Health(second.Master);
                yield return Wait(1.6f);
                Check("Natural Remedy: vines mend the stones", second.Mended > 0 && YosseleUtil.Health(second.Master) > h0, F(h0) + "->" + F(YosseleUtil.Health(second.Master)) + ", mends " + second.Mended);
            }
            else
                ModKitTest.Setup("second Stonehenge", "could not build one");
        }

        ClearFoes();
        Standing(built);
        // 11. Pachira Sapling: its roots mine every node in reach; fully grown, a node mined out drops an aura fruit, and
        // walking over it pays.
        if (sapling != null && YosseleUtil.Alive(sapling.Master))
        {
            int strikes = sapling.Strikes;
            yield return Wait(4.5f);
            if (sapling.Nodes > 0)
                Check("the Sapling's roots mine the ore in reach", sapling.Strikes > strikes, "nodes " + sapling.Nodes + ", strikes " + strikes + "->" + sapling.Strikes);
            else
                ModKitTest.Setup("Sapling mining", "no ore node within its range here");
            List<Link_Resourceset> pay = sapling.FruitPay();
            float total = 0f;
            foreach (Link_Resourceset r in pay) total += r.Amount;
            Check("a fruit pays " + YosseleData.FruitEach + " of each aura", pay.Count == 3 && Mathf.Approximately(total, 3f * YosseleData.FruitEach), pay.Count + " colours, " + total);
            sapling.SetGrowth(0);
            int dropped = YosseleFruit.Dropped;
            Genitem heroTile = hm.Beingserver.Currentblock;
            sapling.NodeEmptied(heroTile.Position, heroTile);
            Check("a Sapling not fully grown drops no fruit", YosseleFruit.Dropped == dropped);
            sapling.SetGrowth(sapling.Max);
            float red = Res(owner, "Redaura"), blue = Res(owner, "Blueaura");
            int picked = YosseleFruit.Picked;
            sapling.NodeEmptied(heroTile.Position, heroTile);
            Check("a fully grown Sapling drops a fruit where a node ran out", YosseleFruit.Dropped == dropped + 1, "dropped " + YosseleFruit.Dropped + ", failed " + YosseleFruit.Failed);
#if YOSSELE_SHOTS
            yield return Wait(0.1f);
            yield return YosseleShots.Close("fruit", heroTile.Position, 3f);
#endif
            yield return Until(() => YosseleFruit.Picked > picked, 3f);
            yield return Wait(0.3f);
            Check("Yossele standing on the fruit picks it up and is paid", YosseleFruit.Picked > picked && Res(owner, "Redaura") - red >= YosseleData.FruitEach - 0.01f && Res(owner, "Blueaura") - blue >= YosseleData.FruitEach - 0.01f,
                "picked " + YosseleFruit.LastPick + ", Red +" + F(Res(owner, "Redaura") - red) + ", Blue +" + F(Res(owner, "Blueaura") - blue));

            yield return Upgrade(sapling, "Focused2", owner);
            yield return Upgrade(sapling, "Grafted3", owner);
            yield return Wait(0.5f);
            Check("Grafted Colors gives the Sapling a COLOUR button", Has(sapling, "Grafted3") && sapling.Master.Commandlistserver.ContainsKey(YosseleData.ColourLink));
            Send(sapling.Master, YosseleData.ColourLink, "");
            yield return Wait(0.4f);
            Send(sapling.Master, YosseleData.ColourLink, "");
            yield return Wait(0.4f);
            pay = sapling.FruitPay();
            Link_Command colour;
            sapling.Master.Commandlistserver.TryGetValue(YosseleData.ColourLink, out colour);
            Check("COLOUR picks one aura: the fruit pays 3 x 30 x 70% of it", sapling.Colour == "Blueaura" && pay.Count == 1 && pay[0].Name == "Blueaura" && Mathf.Abs(pay[0].Amount - 63f) < 0.6f && colour != null && colour.Goalstring == "Blueaura",
                sapling.Colour + ": " + (pay.Count > 0 ? pay[0].Name + " " + pay[0].Amount : "nothing") + ", button '" + (colour != null ? colour.Goalstring : "-") + "'");
            yield return Upgrade(sapling, "Connected4", owner);
            yield return Wait(0.4f);
            Check("Connected Roots bought", Has(sapling, "Connected4"));
            // Selfish Sapling sits on the other tier-3 branch, so only its rule is checked (Dimitri, 2026-10-12: +20% a
            // nearby Sapling, up to +100%).
            Check("Selfish Sapling: +20% mining speed per nearby Sapling, up to +100%",
                Mathf.Approximately(YosselePachiraBrain.Selfish(0, 0.2f, 1f), 0f) && Mathf.Approximately(YosselePachiraBrain.Selfish(2, 0.2f, 1f), 0.4f) && Mathf.Approximately(YosselePachiraBrain.Selfish(7, 0.2f, 1f), 1f),
                "0 -> " + F(YosselePachiraBrain.Selfish(0, 0.2f, 1f)) + ", 2 -> " + F(YosselePachiraBrain.Selfish(2, 0.2f, 1f)) + ", 7 -> " + F(YosselePachiraBrain.Selfish(7, 0.2f, 1f)));
            Beingobject guardBeing = YosseleContent.Beings[YosseleData.Cannon];
            Vector3 gat = ModKitTest.Spot(YosseleUtil.Pos(sapling.Master), guardBeing, team, 1.2f, sapling.Stat("Range", 2f), used, "guarded cannon");
            YosseleCannonBrain guarded = null;
            if (!float.IsNaN(gat.x))
            {
                yield return Build(hm, YosseleData.Cannon, gat, known, got);
                guarded = got[0] as YosseleCannonBrain;
            }
            if (guarded != null)
            {
                yield return Wait(0.4f);
                SetHealth(sapling.Master, YosseleUtil.MaxHealth(sapling.Master));
                float g0 = YosseleUtil.Health(guarded.Master), s0 = YosseleUtil.Health(sapling.Master);
                int shared = YosseleHooks.Shared;
                YosseleUtil.Hurt(hm, guarded.Master, 10f);
                yield return Wait(0.3f);
                float gl = g0 - YosseleUtil.Health(guarded.Master), sl = s0 - YosseleUtil.Health(sapling.Master);
                Check("Connected Roots: the Sapling takes 65% of a nearby building's hit", YosseleHooks.GuardOf(guarded.Master) == sapling && YosseleHooks.Shared > shared && gl > 3f && gl < 4f && sl > 6f && sl < 7f,
                    "cannon lost " + F(gl) + ", Sapling lost " + F(sl) + ", shared " + shared + "->" + YosseleHooks.Shared);
            }
            else
                ModKitTest.Setup("Connected Roots", "no tile for a building in the Sapling's range");
        }

        ClearFoes();
        Standing(built);
        // 12. Gone is gone: a destroyed building's replacement starts at Growth 0, and the Tree's button comes back.
        if (tree != null && YosseleUtil.Alive(tree.Master))
        {
            Kill(tree.Master);
            yield return Until(() => hm.Commandlistserver.ContainsKey(YosseleData.DandelionOption), 3f);
            Check("the Dandelion Tree's button comes back when it is gone", hm.Commandlistserver.ContainsKey(YosseleData.DandelionOption), "shown " + hero.DandelionShown);
            Beingobject being = YosseleContent.Beings[YosseleData.Dandelion];
            Vector3 at = Spot(hm, being, 3.2f);
            if (!float.IsNaN(at.x))
            {
                yield return Build(hm, YosseleData.Dandelion, at, known, got);
                Check("a rebuilt Tree starts at Growth 0", got[0] != null && got[0].Growth == 0, got[0] != null ? "Growth " + got[0].Growth : "not built");
            }
        }
        Log("counters: spores ticks " + YosseleSpores.Ticks + ", fulls " + YosseleSpores.Fulls + ", stuck " + YosseleSpores.Stuck + "; vines spawned " + YosseleVines.Spawned + ", failed " + YosseleVines.Failed
            + ", slowed " + YosseleVines.Slowed + "; hooks softened " + YosseleHooks.Softened + ", soaked " + YosseleHooks.Soaked + ", shared " + YosseleHooks.Shared + ", skipped " + YosseleHooks.Skipped
            + "; fruit dropped " + YosseleFruit.Dropped + ", picked " + YosseleFruit.Picked + "; hero regen " + F(hero.Regenerated));
#if YOSSELE_SHOTS
        yield return YosseleShots.Close("yossele", YosseleUtil.Pos(hm), 3.2f);
        yield return Wait(1f);
        yield return YosseleShots.Shot("upgraded");
#endif

        // 13. The journal: Yossele's pages, and every vessel page with exactly one tab per vessel (Smacky included).
        {
            Logobject page = YosseleContent.Entry.Log;
            bool own = page != null && page.name == "Log_" + YosseleData.Hero, being = false;
            Logobject cannonPage = null, upgradePage = null;
            if (own)
                foreach (Logitems.Base b in AllItems(page.Elements))
                {
                    if (b is Logitems.TextImageBeing && ((Logitems.TextImageBeing)b).Entity == YosseleContent.HeroBeing.Entity) being = true;
                    Logitems.Link l = b as Logitems.Link;
                    if (l != null && !l.Sidelink && l.Target != null && l.Target.name == "Log_" + YosseleData.Hero + "_" + YosseleData.Cannon) cannonPage = l.Target;
                }
            Check("journal: Yossele has their own vessel page", own && being);
            Check("journal: Yossele's page links the Mushroom Cannon's page", cannonPage != null);
            if (cannonPage != null)
                foreach (Logitems.Base b in AllItems(cannonPage.Elements))
                    if (b is Logitems.LinkUpgrade && ((Logitems.LinkUpgrade)b).Target != null && upgradePage == null) upgradePage = ((Logitems.LinkUpgrade)b).Target;
            Check("journal: the Cannon page links its upgrades", upgradePage != null && upgradePage.name.Contains("Yosselecannon"), upgradePage != null ? upgradePage.name : "none");
            List<Logobject> vesselPages = new List<Logobject>();
            foreach (UnityEngine.Object o in YosseleUtil.Folder("Hero").Values)
                if (o is Heroentryobject && ((Heroentryobject)o).Log != null) vesselPages.Add(((Heroentryobject)o).Log);
            int complete = 0;
            List<string> broken = new List<string>();
            foreach (Logobject p in vesselPages)
            {
                List<Logobject> tabs = new List<Logobject>();
                foreach (Logitems.Base b in AllItems(p.Elements))
                {
                    Logitems.Link l = b as Logitems.Link;
                    if (l != null && l.Sidelink && l.Target != null && vesselPages.Contains(l.Target)) tabs.Add(l.Target);
                }
                bool ok = tabs.Count == vesselPages.Count;
                foreach (Logobject v in vesselPages) if (!tabs.Contains(v)) ok = false;
                if (ok) complete++; else broken.Add(p.name + " " + tabs.Count);
            }
            Check("journal: every vessel page has exactly one tab per vessel", complete == vesselPages.Count, complete + "/" + vesselPages.Count + " complete " + string.Join(", ", broken.ToArray()));
#if YOSSELE_SHOTS
            Bl_Menus.Changemenu("Journal");
            List<Logobject> show = new List<Logobject> { page, cannonPage, upgradePage };
            foreach (string other in new[] { "Hero_Smacky", "Hero_Sprigg" })
            {
                Heroentryobject h = YosseleUtil.Heroentry(other);
                if (h != null && h.Log != null) show.Add(h.Log);
            }
            foreach (Logobject shown in show)
            {
                if (shown == null) continue;
                Loghandler.Log_Current = shown;
                yield return Wait(2.5f);
                yield return YosseleShots.Shot("journal_" + shown.name);
            }
            Bl_Menus.Changemenu("Main");
            yield return Wait(1f);
#endif
        }

        freeze = false;
        Log("DONE pass=" + pass + " fail=" + fail + " setup=" + ModKitTest.Setups);
        phase = 9;
    }
}
#endif
