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
    // and maxDist from 'near' where the foe can stand; building tiles are skipped unless asked for (the vine tests ask, to spawn
    // the foe right on a vine tile).
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
    // An entity's health stats other than Healthresource, as value/max (Helper_Damagetools takes a hit from Healtharmor,
    // Healthover, Healthresource, then Health, and a node pays out only once all four are at 0).
    static string Pools(Entity_Master e)
    {
        List<string> parts = new List<string>();
        foreach (string name in new string[] { "Health", "Healthover", "Healtharmor" })
        {
            Link_Stats st;
            if (e != null && e.Statsserver != null && e.Statsserver.TryGetValue(name, out st))
                parts.Add(name + " " + F(st.Value) + "/" + F(st.Value_Calc));
        }
        return parts.Count > 0 ? string.Join(", ", parts.ToArray()) : "none";
    }

    static void Kill(Entity_Master e) { if (e != null && e.Beingserver != null) Game_Entity.Instance.Despawn(e.Beingserver, true, "Selftest"); }

    static IEnumerator Upgrade(YosseleBuilding b, string key, long owner)
    {
        string id = b.Def.Id + "_" + key + "_Upgradeobject";
        Send(b.Master, "Upgrade", "{\"Upgrade\":\"" + id + "\",\"Upgrader\":" + owner + "}");
        yield return Wait(0.6f);
    }

    static bool Has(YosseleBuilding b, string key) { return b != null && b.Master != null && b.Master.Upgradelistserver.ContainsKey(b.Def.Id + "_" + key + "_Upgradeobject"); }

    // What an upgrade costs in one colour, from the sheet's numbers in YosseleData.
    static float CostOf(string building, string key, string colour)
    {
        YosseleData.Build b = YosseleData.BuildFor(building);
        if (b != null)
            foreach (YosseleData.Upg u in b.Upgrades)
                if (u.Key + u.Tier == key)
                    foreach (Link_Resourceset r in YosseleUtil.Costs(u.Costs))
                        if (r.Name == colour) return r.Amount;
        return 0f;
    }

    // A stat's value with every modifier on it (a stat's Value can lag behind its modifiers until the game refreshes it).
    static float Calc(Entity_Master e, string stat)
    {
        Link_Stats s;
        return e != null && e.Statsserver != null && e.Statsserver.TryGetValue(stat, out s) ? s.Value_Calc : -1f;
    }

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

        // 1. Vessel: the stock ranged AI armed with the laser, tooltip, stats, four build options, no FERTILIZE yet,
        // regeneration, and a laser that hurts a foe.
        Check("vessel uses the stock ranged AI", hm.GetComponent<Entity_Ai_Mob_Ranged>() != null && hm.GetComponent<Entity_Ai_Mob_Combat>() == null && hm.GetComponent<Entity_Ai_Hero_Buildsell>() != null);
        {
            yield return Until(() => hero.Armed, 3f);
            Link_Command attack;
            hm.Commandlistserver.TryGetValue("Attack", out attack);
            Entity_Ai_Mob_Ranged ranged = hm.GetComponent<Entity_Ai_Mob_Ranged>();
            Check("the ranged AI fires Yossele's laser", hero.Armed && YosseleContent.LaserReady && attack != null && attack.Goalstring == YosseleData.Laser
                && ranged != null && ranged.Hurtdict.ContainsKey(YosseleData.LaserTag),
                "laser from " + YosseleContent.LaserFrom + ", Attack shoots '" + (attack != null ? attack.Goalstring : "-") + "'");
        }
        Check("vessel has the Patient Growth tooltip", hm.Upgradelistserver.ContainsKey(YosseleData.HeroTooltip));
        Check("vessel stats 70 / 1.75 / 5.5 / range 4 / projectile 15", Mathf.Approximately(YosseleUtil.MaxHealth(hm), 70f) && Mathf.Approximately(YosseleUtil.Stat(hm, "Speed", 0), 1.75f)
            && Mathf.Approximately(YosseleUtil.Stat(hm, "Damage", 0), 5.5f) && Mathf.Approximately(YosseleUtil.Stat(hm, "Range", 0), 4f) && Mathf.Approximately(YosseleUtil.Stat(hm, "Projspeed", 0), 15f),
            "max health " + YosseleUtil.MaxHealth(hm) + ", Speed " + YosseleUtil.Stat(hm, "Speed", 0) + ", Damage " + YosseleUtil.Stat(hm, "Damage", 0) + ", Range " + YosseleUtil.Stat(hm, "Range", 0)
            + ", Projspeed " + YosseleUtil.Stat(hm, "Projspeed", 0));
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
        {
            // A foe 2.5-3.5 tiles off: Yossele, standing idle, shoots it (the stock AI's own targeting). Right after the
            // spawn its health and maximum aren't filled in yet (both read 0 in rounds 9 and 9d), so they are read live.
            // One shot can fell it (round 9b), and a felled foe's object is gone, so who felled it comes from the server.
            Entity_Master foe = SpawnFoe("Enemy_Dagger", YosseleUtil.Pos(hm), 3.5f, 2.5f);
            bool spawned = foe != null;
            int zaps = YosseleClient.Zaps, felled = YosseleServer.FoesFelled;
            yield return Until(() => foe == null || !YosseleUtil.Alive(foe) || (YosseleUtil.MaxHealth(foe) > 0f && YosseleUtil.Health(foe) < YosseleUtil.MaxHealth(foe) - 0.01f), 8f);
            bool gone = foe == null || !YosseleUtil.Alive(foe);
            float lost = gone ? 0f : YosseleUtil.MaxHealth(foe) - YosseleUtil.Health(foe);
            string by = gone ? YosseleServer.LastFelledBy : YosseleUtil.BeingName(foe.Lasthit);
            Check("Yossele's laser hurts a foe in range", spawned && (gone || lost > 0.01f) && by == YosseleData.Hero && YosseleClient.Zaps > zaps,
                !spawned ? "no foe spawned" : (gone ? "felled (" + (YosseleServer.FoesFelled - felled) + " fell)" : "first loss " + F(lost) + " (Damage 5.5 before the foe's own armour)")
                + ", zaps " + zaps + "->" + YosseleClient.Zaps + ", last hit by " + by);
#if YOSSELE_SHOTS
            yield return YosseleShots.Close("laser", YosseleUtil.Pos(hm), 4f);
#endif
            Kill(foe);
            yield return Wait(0.4f);
        }
        // From here on Yossele holds its fire (no foe within its Range), so the laser doesn't fell the foes the building
        // checks set up (round 9 lost two that way). Taken off before the journal.
        Link_Modifier holdFire = Helper_Entitytools.Addmod(hm, "Range", 0.01f, Helper_Entitytools.Modtypes.Override, 0f);

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
                at = ModKitTest.Spot(YosseleUtil.Pos(ore), being, team, 1.2f, 2.5f, used, being.Objectname);
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
        {
            Entityobject te = YosseleContent.Beings[YosseleData.Dandelion].Entity;
            Check("the Tree has a Growth skin for each of its 15 stages", te.Defaultvisual.Skinoverrides.ContainsKey(YosseleData.GrowthStat + "15") && te.Defaultvisual.Skinoverrides[YosseleData.GrowthStat + "15"].Visprefab != null,
                te.Defaultvisual.Skinoverrides.Count + " skins");
        }
#if YOSSELE_SHOTS
        yield return Wait(1.5f);
        yield return YosseleShots.Shot("built");
#endif

        // 3. The Dandelion Tree is one per player: its slot empties while it stands, a second can't be built.
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
            Beingobject bonsai = YosseleContent.Beings[YosseleData.Bonsai];
            at = Spot(hm, bonsai, 3.2f);
            if (!float.IsNaN(at.x))
            {
                yield return Build(hm, YosseleData.Bonsai, at, known, got);
                Check("no Bonsai without the Tree's Bonsai upgrade", got[0] == null);
                if (got[0] != null) Kill(got[0].Master);
            }
        }

        // 4. Growth: a building grows a stage at the start of each Tide it stood through entirely; 5 stages on a map of
        // 4 Tides or fewer; the Tree's seeds are full when a Tide begins.
        {
            int[] reached = new int[1];
            int built0 = Game_Mapvars.Instance.Wave.Value;
            Log("Growth: this map has " + YosseleServer.TidesOnMap() + " Tides, " + YosseleServer.GrowthPerTide() + " stage(s) a Tide");
            yield return ForceTide(reached);
            int g1 = cannon != null ? cannon.Growth : -1;
            Check("no Growth at the first Tide after building (it was built mid-Tide)", cannon == null || g1 == 0, "Tide " + built0 + "->" + reached[0] + ", Growth " + g1);
            YosseleServer.TidesOverride = 10;
            int refills = tree != null ? tree.Refills : 0;
            if (tree != null) tree.Ready = 0f;
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
            if (tree != null)
                Check("the Tree's seeds are full when a Tide begins", tree.Refills > refills, "refills " + refills + "->" + tree.Refills + ", ready " + F(tree.Ready) + "/" + tree.Limit);
            if (cannon != null)
            {
                cannon.SetGrowth(cannon.Max);
                yield return ForceTide(reached);
                Check("Growth stops at the building's maximum", cannon.Growth == cannon.Max, cannon.Growth + "/" + cannon.Max);
            }
            // The test map's own count is replaced: a map of 4 Tides has long Tides.
            YosseleServer.TidesOverride = YosseleData.LongTides;
            Dictionary<YosseleBuilding, int> before = new Dictionary<YosseleBuilding, int>();
            foreach (YosseleBuilding b in built.Values) if (b != null) before[b] = b.Growth;
            yield return ForceTide(reached);
            YosseleServer.TidesOverride = 0;
            int five = 0;
            List<string> got5 = new List<string>();
            foreach (KeyValuePair<YosseleBuilding, int> kv in before)
            {
                int want = Mathf.Min(kv.Key.Max, kv.Value + YosseleData.GrowthPerLongTide);
                if (kv.Key.Growth == want) five++;
                got5.Add(kv.Key.Def.Id.Replace("Tower_Yossele", "") + " " + kv.Value + "->" + kv.Key.Growth);
            }
            Check("a map of 4 Tides or fewer grows 5 stages a Tide", before.Count > 0 && five == before.Count, YosseleUtil.Join(got5));
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

            // Upgrades: Sticky Spores -> Viral Outbreak -> Explosive Finish -> Cordyceps.
            float red = Res(owner, "Redaura");
            yield return Upgrade(cannon, "Sticky2", owner);
            float stickyCost = CostOf(YosseleData.Cannon, "Sticky2", "Redaura");
            Check("Sticky Spores bought and paid (Red " + stickyCost + ")", Has(cannon, "Sticky2") && Mathf.Abs((red - Res(owner, "Redaura")) - stickyCost) < 0.01f, "Red spent " + (red - Res(owner, "Redaura")));
            yield return Upgrade(cannon, "Finish4", owner);
            Check("a level 3 upgrade is not offered from level 1", !Has(cannon, "Finish4"));
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
            Check("Viral Outbreak: 30%, 15 damage within 2 tiles, 1 stack", Has(cannon, "Viral3") && Mathf.Approximately(cannon.Param("Viral_Chance", 0f), 0.3f)
                && Mathf.Approximately(cannon.Param("Viral_Damage", 0f), 15f) && Mathf.Approximately(cannon.Param("Viral_Radius", 0f), 2f) && Mathf.Approximately(cannon.Param("Viral_Spores", 0f), 1f));
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

            // Cordyceps: a foe that falls with spores may rise as a Cordyfriend (20% + 5% a stack).
            yield return Upgrade(cannon, "Cordyceps5", owner);
            Check("Explosive Finish -> Cordyceps", Has(cannon, "Cordyceps5"));
            Check("Cordyceps: 20% + 5% a spore stack", Mathf.Approximately(cannon.CordyChance(0), 0.2f) && Mathf.Approximately(cannon.CordyChance(10), 0.7f),
                "0 stacks " + F(cannon.CordyChance(0)) + ", 10 stacks " + F(cannon.CordyChance(10)));
            int raised = YosseleFriends.Raised;
            Vector3 rise = cp + (Spot(hm, YosseleContent.Beings[YosseleData.Cannon], 2.5f) - cp).normalized * 1.5f;
            Check("Cordyceps raises a Cordyfriend", YosseleFriends.Raise(cannon, rise), "failed " + YosseleFriends.Failed + ", capped " + YosseleFriends.Capped);
            yield return Until(() => YosseleFriends.Raised > raised, 2f);
            yield return Wait(0.5f);
            List<YosseleFriendBrain> friends = YosseleFriends.All;
            YosseleFriendBrain friend = friends.Count > 0 ? friends[friends.Count - 1] : null;
            if (Check("the Cordyfriend stands, owned by the cannon's player", friend != null && friend.Started && YosseleUtil.Owner(friend.Master) == owner && YosseleUtil.Team(friend.Master) == team,
                "raised " + raised + "->" + YosseleFriends.Raised + ", from " + YosseleContent.FriendFrom))
            {
                Entity_Master fm = friend.Master;
                Check("Cordyfriend stats 75 / 2 / range 1.5 / 0.5 / speed 2.25", Mathf.Approximately(YosseleUtil.MaxHealth(fm), 75f) && Mathf.Approximately(YosseleUtil.Stat(fm, "Damage", 0f), 2f)
                    && Mathf.Approximately(YosseleUtil.Stat(fm, "Range", 0f), 1.5f) && Mathf.Approximately(YosseleUtil.Stat(fm, "Attackspeed", 0f), 0.5f) && Mathf.Approximately(YosseleUtil.Stat(fm, "Speed", 0f), 2.25f),
                    "health " + YosseleUtil.MaxHealth(fm) + ", damage " + YosseleUtil.Stat(fm, "Damage", 0f) + ", speed " + YosseleUtil.Stat(fm, "Speed", 0f));
                Beingobject snoble = YosseleUtil.Load<Beingobject>("Mob", YosseleData.FriendLook);
                Check("the Cordyfriend fights with the stock melee AI, on the Snoble's block, never tagged Sno",
                    fm.GetComponent<Entity_Ai_Mob_Combat>() != null && !YosseleUtil.HasTag(fm, "Sno") && !YosseleUtil.HasTag(fm, "Snoble")
                    && (snoble == null || snoble.Block == null || fm.Beingserver.Being.Block == snoble.Block),
                    "block " + (fm.Beingserver.Being.Block != null ? fm.Beingserver.Being.Block.name : "-") + ", tags " + YosseleUtil.Join(fm.Taglistserver));
                Entity_Master prey = SpawnFoe("Enemy_Dagger", friend.Home, 3f, 1.5f);
                int orders = friend.Orders;
                yield return Until(() => prey == null || friend == null || friend.Target == prey || friend.Orders > orders, 3f);
                Check("the Cordyfriend walks to a foe near where it rose", prey != null && friend != null && (friend.Target == prey || friend.Orders > orders), "orders " + orders + "->" + (friend != null ? friend.Orders : -1));
#if YOSSELE_SHOTS
                yield return YosseleShots.Close("cordyfriend", YosseleUtil.Pos(fm), 3f);
#endif
                Kill(prey);
                yield return Wait(0.3f);
                Log("Cordyfriend look: " + (YosseleContent.FriendOwnLook ? "its own sheet " + YosseleData.FriendSheet : "the Snoble's, tint applied to " + YosseleClient.Tinted + " sprite renderers") + " (look by eye)");
            }
            int rises = YosseleSpores.Rises;
            for (int i = 0; i < 3; i++)
            {
                Entity_Master f = SpawnFoe("Enemy_Dagger", cp, 6f, 3f);
                yield return Wait(0.3f);
                if (f == null) continue;
                YosseleSpores.Add(f, YosseleData.SporeMax, cannon);
                Kill(f);
                yield return Wait(0.3f);
            }
            Log("cannon: Cordyceps rises from 3 fully spored foes " + (YosseleSpores.Rises - rises) + " (a 70% chance each, not checked)");
        }

        ClearFoes();
        Standing(built);
        // 7. A second Cannon down the other branch: Farout Fungus -> Deathcap Training (fewer, deadlier stacks) ->
        // Hallucinogen (a stun).
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
                    if (cannon != null)
                    {
                        YosseleSpores.Clear(foe);
                        YosseleSpores.Add(foe, YosseleData.SporeMax, cannon);
                        YosseleSpores.Add(foe, 2, second);
                        Check("Deathcap keeps a foe already above 8 at its stacks", YosseleSpores.StacksOf(foe) == YosseleData.SporeMax, "" + YosseleSpores.StacksOf(foe));
                    }
                    float s0 = SpeedOf(foe);
                    int stuns = YosseleSpores.Stuns;
                    YosseleSpores.Stun(foe, second);
                    yield return Wait(0.3f);
                    // Speed bottoms out at its lower bound (0.05 in round 9); the game's own stun leaves 0.1 (the Boutman boat's).
                    Check("Hallucinogen's stun holds the foe", YosseleSpores.Stuns > stuns && SpeedOf(foe) <= 0.1f, "speed " + F(s0) + "->" + F(SpeedOf(foe)));
                    yield return DebuffReport(foe, YosseleData.VfxDaze);
                }
                Kill(foe);
                Log("cannon: Hallucinogen stuns so far " + YosseleSpores.Stuns + " (a 20% chance every 5 s in play, not checked)");
            }
            else
                ModKitTest.Setup("second Cannon", "could not build one");
        }

        ClearFoes();
        Standing(built);
        // 8. Primordial Dandelion Tree: seeds grow, all strike when a foe comes near; Growth adds 3 seeds and 20% range a
        // stage. Seed Packets -> Picky Placement -> Rubbery Shell (bounces) -> Dandelion Storm.
        if (tree != null && YosseleUtil.Alive(tree.Master))
        {
            tree.SetGrowth(0);
            yield return Wait(0.3f);
            Check("the Tree holds 10 seeds at Growth 0", tree.Limit == 10, "limit " + tree.Limit);
            Check("the Tree's range at Growth 0 is 7 tiles", Mathf.Abs(tree.Stat("Range", 0f) - 7f) < 0.01f, "range " + F(tree.Stat("Range", 0f)));
            tree.SetGrowth(2);
            yield return Wait(0.3f);
            Check("each stage adds " + YosseleData.SeedsPerStage + " seeds and 20% range", tree.Limit == 10 + 2 * YosseleData.SeedsPerStage && Mathf.Abs(tree.Stat("Range", 0f) - 7f * 1.4f) < 0.05f,
                "limit " + tree.Limit + ", range " + F(tree.Stat("Range", 0f)));
            Check("the seed counter shows on the Tree", YosseleUtil.Stat(tree.Master, "Seeds", -1f) >= 0f, "Seeds " + YosseleUtil.Stat(tree.Master, "Seeds", -1f));
            float d0 = tree.Stat("Damage", 0f);
            yield return Upgrade(tree, "Packets2", owner);
            Check("Seed Packets: seeds deal +3", Has(tree, "Packets2") && tree.Stat("Damage", 0f) >= d0 + 2.99f, F(d0) + "->" + F(tree.Stat("Damage", 0f)));
            tree.Ready = tree.Limit;
            Entity_Master foe = SpawnFoe("Enemy_Dagger", YosseleUtil.Pos(tree.Master), tree.Stat("Range", 3f) - 0.5f, 1f);
            int strikes = tree.Strikes;
            yield return Until(() => tree.Strikes > strikes, 5f);
            Check("the Tree lets its seeds go at a foe in range", foe != null && tree.Strikes > strikes && tree.SeedsFlown >= 1, "strikes " + strikes + "->" + tree.Strikes + ", seeds flown " + tree.SeedsFlown + ", ready " + F(tree.Ready));
#if YOSSELE_SHOTS
            yield return YosseleShots.Close("dandelion", YosseleUtil.Pos(tree.Master), 5f);
#endif
            yield return Wait(1f);
            if (tree.Refunded > 0) Check("Seed Packets: seeds that fell a foe come back", true, "refunded " + tree.Refunded);
            else ModKitTest.Setup("Seed Packets", "no seed felled a foe this time (refunded 0)");
            Kill(foe);
            int strikes1 = tree.Strikes;
            tree.Ready = 0f;
            yield return Until(() => tree == null || tree.Ready >= 0.5f || tree.Strikes > strikes1, 4f);
            if (tree == null) ModKitTest.Setup("seed regrow", "the Tree was felled during the check");
            else Check("seeds grow back over time", tree.Ready >= 0.5f || tree.Strikes > strikes1, "ready " + F(tree.Ready) + ", rate " + F(tree.Rate) + ", limit " + tree.Limit);
        }
        if (tree != null && YosseleUtil.Alive(tree.Master))
        {
            yield return Upgrade(tree, "Picky3", owner);
            Check("Picky Placement: 0.25% of the foe's max health a seed", Has(tree, "Picky3") && Mathf.Approximately(tree.Param("Picky_Percent", 0f), 0.0025f));
            yield return Upgrade(tree, "Rubbery4", owner);
            Check("Seed Packets -> Picky Placement -> Rubbery Shell", Has(tree, "Rubbery4"));
            Vector3 tp = YosseleUtil.Pos(tree.Master);
            Entity_Master a = SpawnFoe("Enemy_Aegis", tp, tree.Stat("Range", 7f) - 1f, 2f);
            Entity_Master b = a != null ? SpawnFoe("Enemy_Aegis", YosseleUtil.Pos(a), 1.5f, 0.5f) : null;
            yield return Wait(0.3f);
            int bounced = tree.Bounced;
            tree.Ready = tree.Limit;
            yield return Until(() => tree == null || tree.Bounced > bounced, 4f);
            if (a != null && b != null)
                Check("Rubbery Shell: a strike bounces on to a foe within 2 tiles", tree != null && tree.Bounced > bounced,
                    "bounced " + bounced + "->" + (tree != null ? tree.Bounced : -1) + ", foes " + F(YosseleUtil.Flat(YosseleUtil.Pos(a), YosseleUtil.Pos(b))) + " apart");
            else
                ModKitTest.Setup("Rubbery Shell", "no ground for two foes side by side");
            Kill(a); Kill(b);
            yield return Wait(0.3f);
            if (tree != null)
            {
                int g = tree.Growth;
                yield return Upgrade(tree, "Storm5", owner);
                yield return Wait(0.3f);
                float wantRange = 7f * (1f + YosseleData.RangePerStage * g + 1f);
                Check("Dandelion Storm: +100% range, +20 seeds, +100% seed growth", Has(tree, "Storm5") && tree.Limit == 10 + YosseleData.SeedsPerStage * g + 20
                    && Mathf.Abs(tree.Stat("Range", 0f) - wantRange) < 0.05f && Mathf.Abs(tree.Stat("Seedrate", 0f) - 1f) < 0.01f,
                    "Growth " + g + ": limit " + tree.Limit + ", range " + F(tree.Stat("Range", 0f)) + " (want " + F(wantRange) + "), seed growth " + F(tree.Stat("Seedrate", 0f)));
            }
        }

        ClearFoes();
        Standing(built);
        // 9. Overgrown Stonehenge: shows its facing like the Ice Wall, vines creep the way it faces, one tile per two
        // stages, and slow foes on them by 15%. Verdant Spread -> Easy Target -> Entangling Roots -> Monument.
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
            // Section 4's Tides grew it already, and in play Growth never falls, so its vines are laid again from Growth 0.
            henge.SetGrowth(0);
            YosseleVines.Release(henge);
            henge.Tiles.Clear();
            henge.Spread();
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
                foreach (Entity_Master e in Game_Entity.Instance.Entityclient)
                    if (e != null && e.Being.Value.ToString() == YosseleData.Vine)
                    {
                        float vy = YosseleUtil.Pos(e).y, low = float.NaN;
                        foreach (Renderer r in e.GetComponentsInChildren<Renderer>(true))
                            if (r.enabled) low = r.bounds.min.y;
                        Check("vine tiles lie on the ground (half a tile under the being, like a tower's foot)", !float.IsNaN(low) && Mathf.Abs(low - (vy - 0.5f)) < 0.05f,
                            "vine y " + F(low) + ", being y " + F(vy));
                        break;
                    }
            }
            Genitem vine = henge.Tiles.Count > 0 ? henge.Tiles[henge.Tiles.Count - 1] : null;
            Entity_Master foe = vine != null ? SpawnFoe("Enemy_Aegis", vine.Position, 0.6f, 0f, true) : null;
            float s0 = foe != null ? BaseStat(foe, "Speed") : 1f;
            yield return Until(() => foe == null || YosseleVines.IsSlowed(foe), 2f);
            Check("a foe on the vines is slowed by 15%", foe != null && YosseleVines.IsSlowed(foe) && Mathf.Abs(SpeedOf(foe) - s0 * (1f - 0.15f)) < 0.03f + 0.02f * s0,
                "speed " + F(s0) + "->" + F(SpeedOf(foe)) + " (want " + F(s0 * 0.85f) + ")");
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
            Check("Verdant Spread: vines all around (2 tiles)", Has(henge, "Verdant2") && henge.Reach == 2 && henge.Tiles.Count > before, before + "->" + henge.Tiles.Count + " tiles, reach " + henge.Reach);
            yield return Upgrade(henge, "Easy3", owner);
            Check("Verdant Spread -> Easy Target", Has(henge, "Easy3"));
            vine = henge.Tiles.Count > 0 ? henge.Tiles[0] : null;
            foe = vine != null ? SpawnFoe("Enemy_Aegis", vine.Position, 0.6f, 0f, true) : null;
            // Held on the vines (no stun, no look) so it lingers the 5 s and 7 s the upgrades wait for.
            if (foe != null) YosseleUtil.Root(henge.Master, foe, 14f, false, "");
            yield return Until(() => foe == null || henge.OnSince.ContainsKey(foe), 2f);
            if (foe != null && henge.OnSince.ContainsKey(foe))
            {
                // Same frame before and after, so no other hit lands in between.
                float a0 = YosseleUtil.Health(foe);
                YosseleUtil.Hurt(hm, foe, 4f);
                float plain = a0 - YosseleUtil.Health(foe);
                henge.OnSince[foe] = Time.time - (henge.Param("Easy_After", 5f) - 0.3f);
                yield return Until(() => foe == null || YosseleHooks.MarkOf(foe) > 0f, 2f);
                Check("Easy Target marks a foe 5 s on the vines (+15% damage taken)", foe != null && Mathf.Approximately(YosseleHooks.MarkOf(foe), 0.15f), "mark " + F(YosseleHooks.MarkOf(foe)) + ", marked " + henge.Marked);
                if (foe != null && YosseleUtil.Alive(foe))
                {
                    float a1 = YosseleUtil.Health(foe);
                    YosseleUtil.Hurt(hm, foe, 4f);
                    float marked = a1 - YosseleUtil.Health(foe);
                    if (plain > 0.01f)
                        Check("a marked foe takes 15% more from a real hit", Mathf.Abs(marked - plain * 1.15f) < 0.02f + 0.02f * plain, "plain " + F(plain) + ", marked " + F(marked));
                    else
                        ModKitTest.Setup("Easy Target hit", "a hit didn't show on the foe's health in the same frame (" + F(plain) + ")");
                    yield return DebuffReport(foe, YosseleData.VfxMarked);
                }
                yield return Upgrade(henge, "Roots4", owner);
                Check("Easy Target -> Entangling Roots", Has(henge, "Roots4"));
                int snares = henge.Snares;
                if (foe != null && henge.OnSince.ContainsKey(foe))
                    henge.OnSince[foe] = Time.time - (henge.Param("Roots_After", 7f) - 0.3f);
                yield return Until(() => foe == null || !YosseleUtil.Alive(foe) || henge.Snares > snares, 3f);
                if (henge.Snares > snares)
                {
                    Check("Entangling Roots snare a foe 7 s on the vines", true, "snares " + henge.Snares);
                    yield return DebuffReport(foe, YosseleData.VfxTangle);
                    int once = henge.Snares;
                    if (foe != null && henge.OnSince.ContainsKey(foe))
                        henge.OnSince[foe] = Time.time - 9f;
                    yield return Wait(1.2f);
                    Check("Entangling Roots take each foe once", henge.Snares == once, "snares " + once + "->" + henge.Snares);
                }
                else
                    ModKitTest.Setup("Entangling Roots", "the foe left the vines before " + F(henge.Param("Roots_After", 7f)) + " s (on since: " + henge.OnSince.Count + ")");
            }
            else
                ModKitTest.Setup("Easy Target", "no foe stood on the vines (vine " + (vine != null) + ")");
            Kill(foe);
            yield return Wait(0.3f);

            yield return Upgrade(henge, "Monument5", owner);
            yield return Wait(0.3f);
            Check("Monument: 20% more armour on top of Growth's", Has(henge, "Monument5") && YosseleHooks.IsMonument(henge.Master) && Mathf.Abs(YosseleHooks.ArmorOf(henge.Master) - (4 * YosseleData.HengeArmorPerStage + 0.2f)) < 0.01f,
                "armor " + F(YosseleHooks.ArmorOf(henge.Master)));
            // A foe's debuff as the Bulwark's stun lays it (an Override) and a slow-like cut (a negative Multi): stripped.
            float slow0 = Calc(henge.Master, "Slow");
            int cleansed = YosseleHooks.Cleansed;
            Link_Modifier frozen = Helper_Entitytools.Addmod(henge.Master, "Slow", 0.01f, Helper_Entitytools.Modtypes.Override, 0f);
            Link_Modifier cut = Helper_Entitytools.Addmod(henge.Master, "Slow", -0.5f, Helper_Entitytools.Modtypes.Multi, 0f);
            float slowed = Calc(henge.Master, "Slow");
            yield return Wait(0.4f);
            Check("Monument strips foes' debuffs at once", YosseleHooks.Cleansed >= cleansed + 2 && frozen.Dead && cut.Dead && Mathf.Abs(Calc(henge.Master, "Slow") - slow0) < 0.001f,
                "Slow " + F(slow0) + " -> " + F(slowed) + " with the debuffs -> " + F(Calc(henge.Master, "Slow")) + ", cleansed " + cleansed + "->" + YosseleHooks.Cleansed);
        }

        ClearFoes();
        Standing(built);
        // 10. A second Stonehenge, turned a quarter while placing, down the other branch: its model and vines face the turn
        // once (the game turns the model), Vine Blockade's barrier each Tide, Natural Remedy, Obsidian.
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
                Check("Vine Blockade: sturdier stones", Has(second, "Blockade2") && YosseleUtil.MaxHealth(second.Master) > max0 * 1.45f, F(max0) + "->" + F(YosseleUtil.MaxHealth(second.Master)));
                int[] reached = new int[1];
                yield return ForceTide(reached);
                Check("Vine Blockade raises a barrier of 30% of max health each Tide", Mathf.Abs(second.Barrier - 0.3f * YosseleUtil.MaxHealth(second.Master)) < 0.5f && YosseleHooks.ShieldOf(second.Master) > 0f,
                    "barrier " + F(second.Barrier) + " of " + F(YosseleUtil.MaxHealth(second.Master)));
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
                float max1 = YosseleUtil.MaxHealth(second.Master);
                yield return Upgrade(second, "Obsidian4", owner);
                Check("Obsidian: +200% max health", Has(second, "Obsidian4") && YosseleUtil.MaxHealth(second.Master) >= max1 * 2.3f, F(max1) + "->" + F(YosseleUtil.MaxHealth(second.Master)));
            }
            else
                ModKitTest.Setup("second Stonehenge", "could not build one");
            // A third: Verdant Spread -> Opportunistic Carnivore.
            at = Spot(hm, being, 4.5f);
            YosseleHengeBrain third = null;
            if (!float.IsNaN(at.x))
            {
                yield return Build(hm, YosseleData.Henge, at, known, got);
                third = got[0] as YosseleHengeBrain;
            }
            if (third != null)
            {
                yield return Upgrade(third, "Verdant2", owner);
                yield return Upgrade(third, "Carnivore3", owner);
                yield return Wait(0.6f);
                Check("Verdant Spread -> Opportunistic Carnivore", Has(third, "Carnivore3"));
                Genitem vine = third.Tiles.Count > 0 ? third.Tiles[0] : null;
                Entity_Master foe = vine != null ? SpawnFoe("Enemy_Aegis", vine.Position, 0.6f, 0f, true) : null;
                if (foe != null) YosseleUtil.Root(third.Master, foe, 6f, false, "");
                int drained = third.Drained;
                yield return Until(() => foe == null || third.Drained > drained, 3f);
                Check("Opportunistic Carnivore drains a foe on the vines", foe != null && third.Drained > drained, "drains " + drained + "->" + third.Drained);
                Kill(foe);
            }
            else
                ModKitTest.Setup("third Stonehenge", "could not build one");
        }

        ClearFoes();
        Standing(built);
        // 11. Pachira Sapling: its roots mine every node in reach; fully grown, a strike that pays a node out drops an aura
        // fruit beside the node, worth Attackspeed x Damage x 15 of each aura, and walking over it pays.
        // Focused Roots -> Grafted -> Connected Roots -> Fortifying Roots; a second Sapling: Sustaining Practices -> Forest
        // Project -> Harvesting Season -> Softening Roots; a third: Spontaneous Mulch.
        if (sapling != null && YosseleUtil.Alive(sapling.Master))
        {
            List<Entity_Master> ores = sapling.Ores();
            Entity_Master ore = ores.Count > 0 ? ores[0] : null;
            int strikes = sapling.Strikes;
            yield return Wait(4.5f);
            if (ore != null)
                Check("the Sapling's roots mine the ore in reach", sapling.Strikes > strikes, "nodes " + sapling.Nodes + ", strikes " + strikes + "->" + sapling.Strikes);
            else
                ModKitTest.Setup("Sapling mining", "no ore node within its range here");
            sapling.SetGrowth(0);
            yield return Wait(0.3f);
            float each = sapling.FruitEach, want = sapling.Stat("Attackspeed", 0f) * sapling.Stat("Damage", 0f) * 15f;
            List<Link_Resourceset> pay = sapling.FruitPay();
            float total = 0f;
            foreach (Link_Resourceset r in pay) total += r.Amount;
            Check("a fruit is worth Attackspeed x Damage x 15 of each aura", Mathf.Abs(each - want) < 0.01f && pay.Count == 3 && Mathf.Approximately(total, 3f * Mathf.Max(1f, Mathf.Round(each))),
                "each " + F(each) + " (Attackspeed " + F(sapling.Stat("Attackspeed", 0f)) + ", Damage " + F(sapling.Stat("Damage", 0f)) + "), " + pay.Count + " colours, " + F(total));
            int dropped = YosseleFruit.Dropped;
            sapling.NodeEmptied(ore, YosseleUtil.Pos(sapling.Master));
            Check("a Sapling not fully grown drops no fruit", YosseleFruit.Dropped == dropped);
            sapling.SetGrowth(sapling.Max);
            yield return Wait(0.3f);
            if (ore != null && YosseleUtil.Alive(ore))
            {
                // A node one hit from paying out: the Sapling's next strike pays it out for real.
                Link_Stats hr;
                ore.Statsserver.TryGetValue("Healthresource", out hr);
                Log("ore node " + YosseleUtil.BeingName(ore) + ": Healthresource " + F(hr.Value) + "/" + F(hr.Value_Calc));
                // The node is held at most one hit from empty, and the Sapling gets up to three strikes to pay it out:
                // round 9d's strike took it 1 -> 0 and it didn't pay until a later hit (its other health stats are logged
                // to see why). Another hit that pays it out and refills it first is counted, and the node held again.
                int payouts = sapling.Payouts, fruits = sapling.Fruits, strikes0 = sapling.Strikes, others = 0;
                Vector3 op = YosseleUtil.Pos(ore);
                Log("ore node's other health: " + Pools(ore));
                hr.Value = 1f;
                List<Entity_Master> lying = YosseleFruit.All();
                Entity_Master fruit = null;
                float until = Time.time + 14f;
                while (Time.time < until && sapling.Payouts == payouts && sapling.Strikes < strikes0 + 3)
                {
                    if (hr.Value > 1.001f) { others++; hr.Value = 1f; }
                    yield return null;
                }
                foreach (Entity_Master f in YosseleFruit.All())
                    if (!lying.Contains(f)) fruit = f;
                Check("a fully grown Sapling drops a fruit when its strike pays a node out", sapling.Payouts > payouts && sapling.Fruits > fruits,
                    "strikes " + strikes0 + "->" + sapling.Strikes + ", payouts " + payouts + "->" + sapling.Payouts + ", fruits " + fruits + "->" + sapling.Fruits + ", failed " + YosseleFruit.Failed
                    + ", node now " + F(hr.Value) + "/" + F(hr.Value_Calc) + " (" + Pools(ore) + "), refilled by another hit " + others + "x, Attackspeed " + F(sapling.Stat("Attackspeed", 0f)));
                if (fruit != null)
                    Check("the fruit lies beside the node, off its tile", YosseleUtil.Flat(YosseleUtil.Pos(fruit), op) >= 0.9f, F(YosseleUtil.Flat(YosseleUtil.Pos(fruit), op)) + " tiles from the node");
                else if (sapling.Fruits > fruits)
                    Log("fruit: picked up before it could be measured");
#if YOSSELE_SHOTS
                yield return YosseleShots.Close("fruit", op, 3f);
#endif
            }
            else
                ModKitTest.Setup("fruit from a real payout", "no ore node within the Sapling's range");
            {
                Genitem heroTile = hm.Beingserver.Currentblock;
                float red = Res(owner, "Redaura"), blue = Res(owner, "Blueaura");
                int picked = YosseleFruit.Picked;
                float worth = Mathf.Max(1f, Mathf.Round(sapling.FruitEach));
                YosseleFruit.Drop(sapling, heroTile, heroTile.Position, sapling.FruitPay(), "");
                yield return Until(() => YosseleFruit.Picked > picked, 3f);
                yield return Wait(0.3f);
                Check("Yossele standing on a fruit picks it up and is paid", YosseleFruit.Picked > picked && Res(owner, "Redaura") - red >= worth - 0.01f && Res(owner, "Blueaura") - blue >= worth - 0.01f,
                    "picked " + YosseleFruit.LastPick + ", Red +" + F(Res(owner, "Redaura") - red) + ", Blue +" + F(Res(owner, "Blueaura") - blue));
            }

            float aspd = sapling.Stat("Attackspeed", 0f);
            yield return Upgrade(sapling, "Focused2", owner);
            yield return Wait(0.3f);
            float focused = 0.25f * (1f + YosseleData.PachiraGrowSpeed * sapling.Growth + 0.8f);
            Check("Focused Roots: +80% mining speed", Has(sapling, "Focused2") && Mathf.Abs(sapling.Stat("Attackspeed", 0f) - focused) < 0.01f, F(aspd) + "->" + F(sapling.Stat("Attackspeed", 0f)) + " (want " + F(focused) + ")");
            yield return Upgrade(sapling, "Grafted3", owner);
            yield return Wait(0.5f);
            Check("Grafted gives the Sapling a COLOUR button", Has(sapling, "Grafted3") && sapling.Master.Commandlistserver.ContainsKey(YosseleData.ColourLink));
            Send(sapling.Master, YosseleData.ColourLink, "");
            yield return Wait(0.4f);
            Send(sapling.Master, YosseleData.ColourLink, "");
            yield return Wait(0.4f);
            pay = sapling.FruitPay();
            float blueWant = Mathf.Max(1f, Mathf.Round(3f * sapling.FruitEach * 0.7f));
            Link_Command colour;
            sapling.Master.Commandlistserver.TryGetValue(YosseleData.ColourLink, out colour);
            Check("COLOUR picks one aura: the fruit pays 70% of its three in it", sapling.Colour == "Blueaura" && pay.Count == 1 && pay[0].Name == "Blueaura" && Mathf.Approximately(pay[0].Amount, blueWant) && colour != null && colour.Goalstring == "Blueaura",
                sapling.Colour + ": " + (pay.Count > 0 ? pay[0].Name + " " + pay[0].Amount : "nothing") + " (want " + F(blueWant) + "), button '" + (colour != null ? colour.Goalstring : "-") + "'");
            yield return Upgrade(sapling, "Connected4", owner);
            yield return Wait(0.4f);
            Check("Grafted -> Connected Roots", Has(sapling, "Connected4"));
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
            float smax = YosseleUtil.MaxHealth(sapling.Master);
            yield return Upgrade(sapling, "Fortifying5", owner);
            yield return Wait(0.4f);
            Check("Fortifying Roots: +100% max health", Has(sapling, "Fortifying5") && YosseleUtil.MaxHealth(sapling.Master) >= smax * 1.9f, F(smax) + "->" + F(YosseleUtil.MaxHealth(sapling.Master)));
            Check("Fortifying Roots: 30% less damage for itself and its team's buildings in range", Mathf.Approximately(YosseleHooks.AuraOf(sapling.Master), 0.3f)
                && (guarded == null || Mathf.Approximately(YosseleHooks.AuraOf(guarded.Master), 0.3f)),
                "Sapling " + F(YosseleHooks.AuraOf(sapling.Master)) + ", cannon " + (guarded != null ? F(YosseleHooks.AuraOf(guarded.Master)) : "-"));
            if (guarded != null && YosseleUtil.Alive(guarded.Master))
            {
                SetHealth(sapling.Master, YosseleUtil.MaxHealth(sapling.Master));
                SetHealth(guarded.Master, YosseleUtil.MaxHealth(guarded.Master));
                float g0 = YosseleUtil.Health(guarded.Master), s0 = YosseleUtil.Health(sapling.Master);
                YosseleUtil.Hurt(hm, guarded.Master, 10f);
                yield return Wait(0.3f);
                float gl = g0 - YosseleUtil.Health(guarded.Master), sl = s0 - YosseleUtil.Health(sapling.Master);
                Check("with Connected Roots too: the cannon loses 10 x 35% x 70%, the Sapling 6.5 x 70%", Mathf.Abs(gl - 2.45f) < 0.2f && Mathf.Abs(sl - 4.55f) < 0.2f,
                    "cannon lost " + F(gl) + ", Sapling lost " + F(sl));
            }
            ClearFoes();

            // A second Sapling beside it, down the other branch.
            Beingobject pb = YosseleContent.Beings[YosseleData.Pachira];
            Vector3 pat = ModKitTest.Spot(YosseleUtil.Pos(sapling.Master), pb, team, 1.2f, 3.5f, used, "second Sapling");
            YosselePachiraBrain second = null;
            if (!float.IsNaN(pat.x))
            {
                yield return Build(hm, YosseleData.Pachira, pat, known, got);
                second = got[0] as YosselePachiraBrain;
            }
            if (second != null)
            {
                yield return Wait(0.5f);
                yield return Upgrade(second, "Sustaining2", owner);
                if (second.Nodes > 0)
                {
                    SetHealth(second.Master, YosseleUtil.MaxHealth(second.Master) * 0.5f);
                    float h0 = YosseleUtil.Health(second.Master);
                    yield return Wait(1.2f);
                    Check("Sustaining Practices: it mends from the ore in range", YosseleUtil.Health(second.Master) > h0 + 0.2f, F(h0) + "->" + F(YosseleUtil.Health(second.Master)) + ", nodes " + second.Nodes);
                }
                else
                    ModKitTest.Setup("Sustaining Practices", "no ore node within the second Sapling's range");
                yield return Upgrade(second, "Forest3", owner);
                yield return Wait(0.4f);
                int n = second.Neighbours();
                float forest = 0.25f * (1f + YosseleData.PachiraGrowSpeed * second.Growth + 0.1f * n);
                Check("Forest Project: +10% mining speed for each other Sapling within 4 tiles", Has(second, "Forest3") && n >= 1 && Mathf.Abs(second.Stat("Attackspeed", 0f) - forest) < 0.01f,
                    n + " near, mining speed " + F(second.Stat("Attackspeed", 0f)) + " (want " + F(forest) + ")");
                float each0 = second.FruitEach;
                yield return Upgrade(second, "Harvest4", owner);
                Check("Harvesting Season: fruit is worth double", Has(second, "Harvest4") && Mathf.Abs(second.FruitEach - 2f * each0) < 0.01f, F(each0) + "->" + F(second.FruitEach));
                List<Entity_Master> near = second.Ores();
                Entity_Master node = null;
                foreach (Entity_Master o in near)
                    if (!YosseleSoftening.IsSoft(o) && Calc(o, "Healthresource") >= 2f) { node = o; break; }
                float hits = node != null ? Calc(node, "Healthresource") : 0f;
                yield return Upgrade(second, "Softening5", owner);
                yield return Wait(1.5f);
                if (node != null)
                    Check("Softening Roots: a node in range takes one hit fewer to pay out", Has(second, "Softening5") && YosseleSoftening.IsSoft(node) && Mathf.Abs(Calc(node, "Healthresource") - (hits - 1f)) < 0.01f,
                        "hits per payout " + F(hits) + "->" + F(Calc(node, "Healthresource")) + ", softened nodes " + YosseleSoftening.Count);
                else
                    ModKitTest.Setup("Softening Roots", "no node of 2 hits or more within the second Sapling's range (" + near.Count + " nodes)");
            }
            else
                ModKitTest.Setup("second Sapling", "could not build one");

            // A third: Focused Roots -> Spontaneous Mulch.
            pat = ModKitTest.Spot(YosseleUtil.Pos(sapling.Master), pb, team, 1.2f, 5f, used, "third Sapling");
            YosselePachiraBrain third = null;
            if (!float.IsNaN(pat.x))
            {
                yield return Build(hm, YosseleData.Pachira, pat, known, got);
                third = got[0] as YosselePachiraBrain;
            }
            if (third != null)
            {
                yield return Upgrade(third, "Focused2", owner);
                yield return Upgrade(third, "Mulch3", owner);
                Check("Focused Roots -> Spontaneous Mulch", Has(third, "Mulch3"));
                int mulched = third.Mulched;
                Entity_Master foe = SpawnFoe("Enemy_Dagger", third.Pos, third.Stat("Range", 2.5f) - 0.3f, 0f);
                bool spawned = foe != null;   // a felled foe's object is gone afterwards
                yield return Wait(0.3f);
                Kill(foe);
                yield return Wait(0.5f);
                float mulch = 0.25f * (1f + YosseleData.PachiraGrowSpeed * third.Growth + 0.8f + 0.2f);
                Check("Spontaneous Mulch: a foe falling in range gives +20% mining speed", spawned && third.Mulched > mulched && third.Mulching && Mathf.Abs(third.Stat("Attackspeed", 0f) - mulch) < 0.01f,
                    "mulched " + mulched + "->" + third.Mulched + ", mining speed " + F(third.Stat("Attackspeed", 0f)) + " (want " + F(mulch) + ")");
            }
            else
                ModKitTest.Setup("third Sapling", "could not build one");
        }

        ClearFoes();
        Standing(built);
        // 12. Gone is gone: a destroyed building's replacement starts at Growth 0, and the Tree's button comes back. The new
        // Tree goes Demand and Supply -> D-MDS (shoots down a foe's projectile) -> Bonsai (plants one copy at 40%).
        if (tree != null && YosseleUtil.Alive(tree.Master))
        {
            Kill(tree.Master);
            yield return Until(() => hm.Commandlistserver.ContainsKey(YosseleData.DandelionOption), 3f);
            Check("the Dandelion Tree's button comes back when it is gone", hm.Commandlistserver.ContainsKey(YosseleData.DandelionOption), "shown " + hero.DandelionShown);
        }
        YosseleDandelionBrain tree2 = null;
        if (hero.Slot == YosseleData.Dandelion)
        {
            Beingobject being = YosseleContent.Beings[YosseleData.Dandelion];
            Vector3 at = Spot(hm, being, 3.2f);
            if (!float.IsNaN(at.x))
            {
                yield return Build(hm, YosseleData.Dandelion, at, known, got);
                tree2 = got[0] as YosseleDandelionBrain;
                Check("a rebuilt Tree starts at Growth 0", tree2 != null && tree2.Growth == 0, tree2 != null ? "Growth " + tree2.Growth : "not built");
            }
        }
        if (tree2 != null)
        {
            yield return Upgrade(tree2, "Demand2", owner);
            Check("Demand and Supply bought", Has(tree2, "Demand2"));
            tree2.Ready = 0f;
            float fast = tree2.Rate;
            tree2.Ready = tree2.Limit;
            float slow = tree2.Rate;
            Check("Demand and Supply: seeds grow twice as fast while under a quarter are ready", Mathf.Abs(fast - 2f * slow) < 0.001f, "rate " + F(fast) + " empty, " + F(slow) + " full");
            yield return Upgrade(tree2, "Dmds3", owner);
            Check("Demand and Supply -> D-MDS: seed growth -50%", Has(tree2, "Dmds3") && Mathf.Abs(tree2.Stat("Seedrate", 0f) - 0.25f) < 0.01f, "seed growth " + F(tree2.Stat("Seedrate", 0f)));
            Check("D-MDS: a boss's projectile takes 5 seeds, others 1", tree2.CostFor(30000f) == 5 && tree2.CostFor(40f) == 1, "boss " + tree2.CostFor(30000f) + ", other " + tree2.CostFor(40f));
            Projectileobject bolt = YosseleUtil.Load<Projectileobject>("Projectile", "Enemy_Ballista_Bolt");
            Entity_Master shooter = SpawnFoe("Enemy_Aegis", tree2.Pos, 5f, 3f);
            yield return Wait(0.3f);
            if (shooter != null && bolt != null && YosseleUtil.Alive(shooter))
            {
                tree2.Ready = tree2.Limit;
                int intercepted = tree2.Intercepted, spent = tree2.InterceptSeeds, strikes = tree2.Strikes;
                Projdata data = default(Projdata);
                data.Params = new Dictionary<string, float> { { "Speed", 2f }, { "Accel", 0.5f }, { "Zvelocity", 1f } };
                data.Paramsobject = new Dictionary<string, Entity_Master> { { "Goal", tree2.Master } };
                Projectile_Master p = Helper_Damagetools.Projectile(shooter, "Enemy_Ballista_Bolt", YosseleUtil.Pos(shooter) + Vector3.up * 0.5f, data, "Hurt");
                bool threat = YosseleDandelionBrain.Threat(p, team);
                yield return Until(() => tree2 == null || tree2.Intercepted > intercepted, 2f);
                Check("D-MDS shoots down a foe's projectile in range", tree2 != null && tree2.Intercepted > intercepted && tree2.InterceptSeeds > spent && p != null && !p.gameObject.activeInHierarchy,
                    "a threat " + threat + ", intercepted " + intercepted + "->" + (tree2 != null ? tree2.Intercepted : -1) + ", seeds " + spent + "->" + (tree2 != null ? tree2.InterceptSeeds : -1)
                    + ", bolt active " + (p != null && p.gameObject.activeInHierarchy) + ", shooter base health " + F(YosseleUtil.BaseHealth(shooter)));
                Check("D-MDS seeds don't strike foes", tree2 != null && tree2.Strikes == strikes, "strikes " + strikes + "->" + (tree2 != null ? tree2.Strikes : -1));
                Log("D-MDS: client copies put out " + YosseleClient.Intercepts + " (a client's own count; the host skips its own)");
            }
            else
                ModKitTest.Setup("D-MDS intercept", shooter == null ? "no ground for a shooter" : bolt == null ? "Enemy_Ballista_Bolt not found" : "the shooter fell first");
            Kill(shooter);
            yield return Wait(0.3f);

            yield return Upgrade(tree2, "Bonsai4", owner);
            yield return Until(() => hero.Slot == YosseleData.Bonsai, 2f);
            Link_Command slot;
            hm.Commandlistserver.TryGetValue(YosseleData.DandelionOption, out slot);
            Check("D-MDS -> Bonsai: the Tree's build slot offers a Bonsai", Has(tree2, "Bonsai4") && slot != null && slot.Goalstring == YosseleData.Bonsai,
                "slot '" + hero.Slot + "', button '" + (slot != null ? slot.Goalstring : "-") + "'");
            Beingobject bb = YosseleContent.Beings[YosseleData.Bonsai];
            Vector3 bat = Spot(hm, bb, 3.2f);
            YosseleBonsaiBrain bonsai = null;
            float red = Res(owner, "Redaura");
            if (!float.IsNaN(bat.x))
            {
                yield return Build(hm, YosseleData.Bonsai, bat, known, got);
                bonsai = got[0] as YosseleBonsaiBrain;
            }
            if (Check("build a Bonsai", bonsai != null))
            {
                yield return Wait(0.3f);
                Check("the Bonsai costs Red 50, like the Tree", Mathf.Abs((red - Res(owner, "Redaura")) - 50f) < 0.01f, "Red spent " + F(red - Res(owner, "Redaura")));
                Check("the Bonsai sells for nothing", Mathf.Approximately(bonsai.Master.Entitydata.Value.Sellvalue, 0f) && Mathf.Approximately(bonsai.Master.Entitydata.Value.Upgradesellvalue, 0f),
                    "sell " + F(bonsai.Master.Entitydata.Value.Sellvalue) + ", upgrades " + F(bonsai.Master.Entitydata.Value.Upgradesellvalue));
                yield return Wait(1.5f);
                Check("the Bonsai has 40% of the Tree's stats", Mathf.Abs(bonsai.Stat("Range", 0f) - 2.8f) < 0.01f && bonsai.Limit == 4, "range " + F(bonsai.Stat("Range", 0f)) + ", seeds " + bonsai.Limit);
                Check("the Bonsai copies the Tree's upgrades, their effects at 40%", bonsai.Has("Demand") && bonsai.Has("Dmds") && Mathf.Abs(bonsai.Param("Demand_Max", 0f) - 0.4f) < 0.001f
                    && Mathf.Abs(bonsai.Param("Demand_Below", 0f) - 0.25f) < 0.001f && Mathf.Abs(bonsai.Stat("Seedrate", 0f) - 0.1f) < 0.005f && Mathf.Abs(bonsai.Param("Dmds_Boss", 0f) - 5f) < 0.001f,
                    "copied " + YosseleUtil.Join(bonsai.Keys) + " (" + bonsai.Copies + "), Demand " + F(bonsai.Param("Demand_Max", 0f)) + " below " + F(bonsai.Param("Demand_Below", 0f))
                    + ", seed growth " + F(bonsai.Stat("Seedrate", 0f)) + ", boss seeds " + F(bonsai.Param("Dmds_Boss", 0f)));
                Check("the Tree's slot is empty while the Bonsai stands", hero.Slot == "" && !hm.Commandlistserver.ContainsKey(YosseleData.DandelionOption), "slot '" + hero.Slot + "'");
                Vector3 bat2 = Spot(hm, bb, 3.6f);
                if (!float.IsNaN(bat2.x))
                {
                    yield return Build(hm, YosseleData.Bonsai, bat2, known, got);
                    Check("a second Bonsai can't be planted", got[0] == null);
                    if (got[0] != null) Kill(got[0].Master);
                }
                bonsai.SetGrowth(5);
                yield return Wait(0.3f);
                Check("each Bonsai stage adds 40% of the Tree's 3 seeds", bonsai.Limit == 10, "seeds at Growth 5: " + bonsai.Limit);
#if YOSSELE_SHOTS
                yield return YosseleShots.Close("bonsai", bonsai.Pos, 3f);
#endif
                Kill(bonsai.Master);
                yield return Until(() => hero.Slot == YosseleData.Bonsai, 3f);
                Check("the Bonsai's slot comes back when it is gone", hero.Slot == YosseleData.Bonsai, "slot '" + hero.Slot + "'");
            }
        }
        else
            ModKitTest.Setup("D-MDS and Bonsai", "no rebuilt Tree");

        // A third Tree: Demand and Supply -> Garden Weeds (a foe that falls within 3 s of a seed hit: 40% for a stack of
        // +10% seed growth, at most one a Growth stage). The falls are handed to the brain the way YosseleServer does.
        if (tree2 != null && YosseleUtil.Alive(tree2.Master))
        {
            Kill(tree2.Master);
            yield return Until(() => hero.Slot == YosseleData.Dandelion, 3f);
        }
        YosseleDandelionBrain tree3 = null;
        if (hero.Slot == YosseleData.Dandelion)
        {
            Beingobject being = YosseleContent.Beings[YosseleData.Dandelion];
            Vector3 at = Spot(hm, being, 3.2f);
            if (!float.IsNaN(at.x))
            {
                yield return Build(hm, YosseleData.Dandelion, at, known, got);
                tree3 = got[0] as YosseleDandelionBrain;
            }
        }
        if (tree3 != null)
        {
            yield return Upgrade(tree3, "Demand2", owner);
            yield return Upgrade(tree3, "Weeds3", owner);
            Check("Demand and Supply -> Garden Weeds", Has(tree3, "Weeds3"));
            tree3.SetGrowth(3);
            yield return Wait(0.3f);
            tree3.Ready = tree3.Limit;
            float plain = tree3.Rate;
            Entity_Master foe = SpawnFoe("Enemy_Dagger", tree3.Pos, tree3.Stat("Range", 7f) + 3f, 1f);
            if (foe != null)
            {
                tree3.LastSeed[foe] = Time.time - 3.5f;
                tree3.FoeFell(foe);
                Check("Garden Weeds: no stack from a foe hit more than 3 s before it fell", tree3.Weeds == 0, "stacks " + tree3.Weeds);
                for (int i = 0; i < 12; i++)
                {
                    tree3.LastSeed[foe] = Time.time - 1f;
                    tree3.FoeFell(foe);
                }
                int stacks = tree3.Weeds;
                Check("Garden Weeds: 12 falls give stacks, at most one a Growth stage", stacks >= 1 && stacks <= 3, "stacks " + stacks + " at Growth 3 (none from 12 tries: 0.2%)");
                tree3.Ready = tree3.Limit;
                Check("Garden Weeds: each stack adds 10% seed growth", Mathf.Abs(tree3.Rate - plain * (1f + 0.1f * stacks)) < 0.001f, "rate " + F(plain) + "->" + F(tree3.Rate) + " with " + stacks);
            }
            else
                ModKitTest.Setup("Garden Weeds", "no ground for a foe");
            Kill(foe);
        }
        else
            ModKitTest.Setup("Garden Weeds", "no third Tree");
        Log("counters: spores ticks " + YosseleSpores.Ticks + ", fulls " + YosseleSpores.Fulls + ", stuck " + YosseleSpores.Stuck + ", stuns " + YosseleSpores.Stuns + ", virals " + YosseleSpores.Virals
            + ", rises " + YosseleSpores.Rises + "; Cordyfriends raised " + YosseleFriends.Raised + ", fell " + YosseleFriends.Fell + ", capped " + YosseleFriends.Capped + ", failed " + YosseleFriends.Failed
            + "; vines spawned " + YosseleVines.Spawned + ", failed " + YosseleVines.Failed + ", slowed " + YosseleVines.Slowed + "; hooks softened " + YosseleHooks.Softened + ", soaked " + YosseleHooks.Soaked
            + ", shared " + YosseleHooks.Shared + ", skipped " + YosseleHooks.Skipped + ", marked " + YosseleHooks.Marked + ", cleansed " + YosseleHooks.Cleansed
            + "; fruit dropped " + YosseleFruit.Dropped + ", picked " + YosseleFruit.Picked + "; softened nodes " + YosseleSoftening.Made + "; client zaps " + YosseleClient.Zaps
            + ", intercepts " + YosseleClient.Intercepts + ", tinted " + YosseleClient.Tinted + "; hero regen " + F(hero.Regenerated));
#if YOSSELE_SHOTS
        yield return YosseleShots.Close("yossele", YosseleUtil.Pos(hm), 3.2f);
        yield return Wait(1f);
        yield return YosseleShots.Shot("upgraded");
#endif

        YosseleUtil.Unmod(holdFire);

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
