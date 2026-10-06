using Blockage.Menu;
#if MUMMER_SELFTEST
// Debug build only (Build-MummerMod.ps1 -Define MUMMER_SELFTEST[,MUMMER_SHOTS]). Starts a solo match by itself (Embark,
// pick Mummer, pick a map, ready up) because the game ignores injected input, then plays through every Mummer mechanic
// on the host the way a player would (real Build, Hook, Costume and Upgrade commands through Command_ServerRpc),
// spawns and fells foes, forces Tides, and logs one "[Mummer] TEST PASS|FAIL name: detail" line per check plus a final
// "TEST DONE" summary in Player.log.
using UnityEngine.SceneManagement;
using Unity.Netcode;

public static class MummerSelfTest
{
    static int phase;
    static float seen = -1f, step = -1f;
    static string node = "";
    static bool freeze, forcing;
    static int pass, fail;

    static void Log(string m) { MummerLog.Info("TEST " + m); }

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
        if (freeze && !forcing && Game_Mapvars.Instance != null && MummerUtil.IsServer && Game_Mapvars.Instance.Time_Wave.Value < 600f)
            Game_Mapvars.Instance.Time_Wave.Value = 900f;
        if (forcing && Game_Mapvars.Instance != null && MummerUtil.IsServer)
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
#if MUMMER_SHOTS
            if (!boatShot && now - seen >= 4f && Macro_Worldvisual.Instance != null)
            {
                boatShot = true;
                MummerMod.Instance.StartCoroutine(BoatShot());
            }
            if (now - seen < 16f)
                return;
#else
            if (now - seen < 8f)
                return;
#endif
            if (step < 0f)
            {
                macro.Selectcharacter(MummerData.Hero);
                node = PickNode();
                Log("picked Mummer and map node '" + node + "'");
                if (node != "")
                    Tds_Multiplayer_Remote.Instance.Selectmap_ServerRpc(node, false, default(ServerRpcParams));
                step = now;
                return;
            }
            if (now - step > 3f && now - step < 4f)
            {
                Tds_Clientinfo info = Blm_Server.Instance.Localplayer.GetComponent<Tds_Clientinfo>();
                Check("vessel select", info != null && info.Hero.Value.ToString() == MummerData.Hero, info != null ? info.Hero.Value.ToString() : "no clientinfo");
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

        if (phase <= 2 && MummerServer.Heroes.Count > 0 && Game_Mapvars.Instance != null && MummerUtil.IsServer)
        {
            phase = 3;
            MummerMod.Instance.StartCoroutine(Scenario());
        }
    }

#if MUMMER_SHOTS
    static bool boatShot;

    // The boat's vessel select, opened as the Setup screen's button does, captured with Mummer standing on it.
    static IEnumerator BoatShot()
    {
        Macro_Worldvisual.Instance.Opencharselect();
        yield return new WaitForSecondsRealtime(3f);
        Macro_Characterselect_Click mine = null;
        int buttons = 0;
        foreach (Macro_Characterselect_Click b in UnityEngine.Object.FindObjectsOfType<Macro_Characterselect_Click>())
        {
            buttons++;
            if (b.Character == MummerData.Hero) mine = b;
        }
        Dictionary<string, UnityEngine.Object> heroes = MummerUtil.Folder("Hero");
        Check("Mummer unlocked on the boat's vessel select", mine != null && !mine.Locked, mine == null ? "no button" : "locked " + mine.Locked);
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
        yield return MummerShots.Shot("boat");
        if (mine != null)
        {
            Macro_Main.Instance.Selectcharacter(MummerData.Hero);
            yield return new WaitForSecondsRealtime(1.5f);
            yield return MummerShots.Shot("boat_selected");
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
        return ModKitTest.Spot(MummerUtil.Pos(hero), being, MummerUtil.Team(hero), ring, 8f, used, being.Objectname);
    }

    static T Newest<T>(string being, List<MummerBuilding> known) where T : MummerBuilding
    {
        foreach (MummerBuilding b in MummerServer.Buildings)
            if (b != null && b.Started && b.Def.Id == being && !known.Contains(b))
                return b as T;
        return null;
    }

    static IEnumerator Build(Entity_Master hero, string tower, Vector3 at, List<MummerBuilding> known, MummerBuilding[] result)
    {
        result[0] = null;
        string json = "{\"Tower\":\"" + tower + "\",\"Position\":" + Vec(at) + ",\"Orientation\":0,\"Owner\":" + MummerUtil.Owner(hero) + "}";
        Send(hero, "Build", json);
        float t = 0f;
        while (t < 20f && result[0] == null)
        {
            result[0] = Newest<MummerBuilding>(tower, known);
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

    static int Team() { return MummerServer.Heroes.Count > 0 ? MummerUtil.Team(MummerServer.Heroes[0].Master) : 1; }

    // Removes Tide foes so they don't pile up (not a kill: no Scraps).
    static void ClearFoes()
    {
        int team = Team();
        foreach (Entity_Master e in new List<Entity_Master>(Game_Entity.Instance.Entityclient))
            if (MummerUtil.IsFoe(e, team))
                Game_Entity.Instance.Despawn(e.Beingserver, false, "Selftest");
    }

    // Spawned as a child of the enemy core, like Tide foes (kit ModKitTest.SpawnFoe): the nearest ground tile between minDist
    // and maxDist from 'near' where the foe can stand; building tiles are skipped unless asked for (the Curtain test spawns
    // right on it).
    static Entity_Master SpawnFoe(string being, Vector3 near, float maxDist, float minDist = 0f, bool onBuildings = false)
    {
        return ModKitTest.SpawnFoe(being, near, minDist, maxDist, onBuildings ? null : used);
    }

    // Where the game attached one of Mummer's debuff effects (Helper_Vfxtools.Debuff parents it under the foe): logged so
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
                + " foe pos " + MummerUtil.Pos(foe).ToString("F2") + " renderers: " + string.Join("; ", rs.ToArray()));
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

    static IEnumerator Upgrade(MummerBuilding b, string key, long owner)
    {
        string id = b.Def.Id + "_" + key + "_Upgradeobject";
        Send(b.Master, "Upgrade", "{\"Upgrade\":\"" + id + "\",\"Upgrader\":" + owner + "}");
        yield return Wait(0.6f);
    }

    static bool Has(MummerBuilding b, string key) { return b != null && b.Master != null && b.Master.Upgradelistserver.ContainsKey(b.Def.Id + "_" + key + "_Upgradeobject"); }

    static List<Logitems.Base> AllItems(List<Logitems.Base> items) { return ModKitVesselJournal.Flat(items); }

    // ---------------------------------------------------------------- the scenario

    static IEnumerator Scenario()
    {
        Log("match started: scenario begins");
        MummerHeroBrain hero = MummerServer.Heroes[0];
        yield return Until(() => Game_Mapvars.Instance != null && Game_Mapvars.Instance.Wave.Value >= 1, 150f);
        freeze = true;
        yield return Wait(2f);
        ClearFoes();
        Entity_Master hm = hero.Master;
        long owner = MummerUtil.Owner(hm);
        int team = MummerUtil.Team(hm);
        MummerPlayer player = MummerServer.Player(owner);
        Log("tide " + Game_Mapvars.Instance.Wave.Value + ", vessel at " + MummerUtil.Pos(hm) + ", owner " + owner + ", team " + team);
        ModKitTest.Log = Log;
        ModKitTest.MapDump(team, hm);

        // 1. Vessel: native melee setup, tooltip, commands, stats, Thread.
        Check("vessel uses the stock melee AI", hm.GetComponent<Entity_Ai_Mob_Combat>() != null && hm.GetComponent<Entity_Ai_Mob_Ranged>() == null && hm.GetComponent<Entity_Ai_Hero_Buildsell>() != null);
        Check("vessel plays Peekaboo through Entity_Vis_Mob_Attack", hm.Client != null && hm.GetComponentInChildren<Entity_Vis_Mob_Attack>(true) != null || MummerContent.HeroBeing.Entity.Visualscripts.Exists(s => s.name == "Entity_Vis_Mob_Attack"));
        Check("vessel has The Show Must Go On tooltip", hm.Upgradelistserver.ContainsKey(MummerData.HeroTooltip));
        Check("vessel stats", Mathf.Approximately(MummerUtil.Stat(hm, "Range", 0), 2f) && Mathf.Approximately(MummerUtil.Stat(hm, "Health", 0), 45f),
            "Range " + MummerUtil.Stat(hm, "Range", 0) + ", Health " + MummerUtil.Stat(hm, "Health", 0) + ", Damage " + MummerUtil.Stat(hm, "Damage", 0));
        int options = 0;
        for (int i = 0; i < 4; i++)
        {
            Link_Command c;
            if (hm.Commandlistserver.TryGetValue("Buildoption" + (i + 1), out c) && c.Goalstring == MummerData.Towers[i]) options++;
        }
        Check("four build options", options == 4, options + "/4");
        Link_Command hook;
        Check("HOOK command", hm.Commandlistserver.TryGetValue(MummerData.HookLink, out hook) && hook.Controllable && MummerContent.Hook.Commandfuncts[0] is MummerHookSelect);
        Check("Thread starts at " + MummerData.ThreadStart + " (no unravelling as the first Tide arrives)", player.Thread == MummerData.ThreadStart && Mathf.Approximately(MummerUtil.Stat(hm, "Thread", -1), player.Thread),
            "Thread " + player.Thread + ", stat " + MummerUtil.Stat(hm, "Thread", -1));
        Check("vessel visible to clients", hm.Client != null && hm.Client.Beingobject != null && hm.Client.Beingobject.Objectname == MummerData.Hero);

        // 2. Building all four through the vessel's real Build command.
        Helper_Resources.Giveresources(MummerUtil.Costs("Redaura=20000;Blueaura=20000;Yellowaura=20000"), owner, Helper_Resources.Styles.Default);
        yield return Wait(0.5f);
        List<MummerBuilding> known = new List<MummerBuilding>(MummerServer.Buildings);
        Dictionary<string, MummerBuilding> built = new Dictionary<string, MummerBuilding>();
        MummerBuilding[] got = new MummerBuilding[1];
        float[] rings = { 2.2f, 3.2f, 2.2f, 3.2f };
        for (int i = 0; i < MummerData.Towers.Length; i++)
        {
            string tower = MummerData.Towers[i];
            Beingobject being = MummerContent.Beings[tower];
            Vector3 at = Spot(hm, being, rings[i]);
            if (float.IsNaN(at.x)) { ModKitTest.Setup("build " + tower, "no free tile near the vessel"); continue; }
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
        }
        MummerLoomBrain loom = built.ContainsKey(MummerData.Loom) ? (MummerLoomBrain)built[MummerData.Loom] : null;
        MummerStageBrain stage = built.ContainsKey(MummerData.Stage) ? (MummerStageBrain)built[MummerData.Stage] : null;
        MummerCrossBrain cross = built.ContainsKey(MummerData.Cross) ? (MummerCrossBrain)built[MummerData.Cross] : null;
        MummerCurtainBrain curtain = built.ContainsKey(MummerData.Curtain) ? (MummerCurtainBrain)built[MummerData.Curtain] : null;

        // 3. Stage: stitches a plain Understudy with Thread straight away; it is visible and leashed.
        if (stage != null)
        {
            int thread0 = player.Thread;
            yield return Until(() => stage.Cast.Count > 0, 4f);
            Check("Stage stitches an Understudy", stage.Cast.Count > 0, "cast " + stage.Cast.Count);
            Check("stitching spends Thread", player.Thread == thread0 - MummerData.PuppetFor(MummerData.Costume.Plain).Thread, thread0 + "->" + player.Thread);
            Link_Command costume;
            Check("Stage has the COSTUME button", stage.Master.Commandlistserver.TryGetValue(MummerData.CostumeLink, out costume) && costume.Goalstring == "Plain" && costume.Controllable,
                costume != null ? costume.Goalstring : "none");
            if (stage.Cast.Count > 0)
            {
                MummerPuppetBrain pb = stage.Cast[0];
                yield return Until(() => pb == null || pb.Started, 2f);
                Check("Understudy is the plain doll with the game's melee AI", pb != null && pb.Master.GetComponent<Entity_Ai_Mob_Combat>() != null && MummerUtil.BeingName(pb.Master) == "Mob_Understudy");
                Check("Understudy visible to clients", pb != null && pb.Master.Client != null && pb.Master.Client.Beingobject != null);
                Send(stage.Master, MummerData.CostumeLink, "");
                yield return Wait(0.4f);
                Check("COSTUME refuses costumes not yet unlocked", stage.Costume == MummerData.Costume.Plain);
            }
        }
#if MUMMER_SHOTS
        yield return Wait(1.5f);
        yield return MummerShots.Shot("built");
#endif

        // 4. Loom: foes felled near it leave Scraps; a ranged foe unlocks the Ballista costume, a heavy one the Aegis.
        if (loom != null)
        {
            Vector3 lp = MummerUtil.Pos(loom.Master);
            int t0 = player.Thread;
            Entity_Master dagger = SpawnFoe("Enemy_Dagger", lp, 2.5f);
            bool daggerSpawned = dagger != null;
            yield return Wait(0.5f);
            Kill(dagger);
            yield return Wait(0.6f);
            Check("a Dagger felled near the Loom spins 1 Thread", daggerSpawned && player.Thread == t0 + MummerData.ScrapSmall && loom.Scrapped == 1, t0 + "->" + player.Thread);
            int t1 = player.Thread;
            Entity_Master far = SpawnFoe("Enemy_Dagger", lp, 14f, loom.Scraprange + 0.8f);
            bool farSpawned = far != null;
            yield return Wait(0.5f);
            Kill(far);
            yield return Wait(0.6f);
            Check("a foe felled out of the Loom's reach leaves nothing", farSpawned && player.Thread == t1, t1 + "->" + player.Thread);
            Entity_Master ballista = SpawnFoe("Enemy_Ballista", lp, 2.5f);
            yield return Wait(0.5f);
            int t2 = player.Thread;
            Check("Ballista is a ranged foe", ballista != null && MummerServer.Family(ballista) == MummerData.Costume.Ballista);
            Kill(ballista);
            yield return Wait(0.6f);
            Check("a Ballista unlocks the Ballista costume and spins 3 Thread", player.Ballista && player.Thread == t2 + MummerData.ScrapBig, "unlocked " + player.Ballista + ", " + t2 + "->" + player.Thread);
            Entity_Master aegis = SpawnFoe("Enemy_Aegis", lp, 2.5f);
            yield return Wait(0.5f);
            Check("Aegis is a heavy foe", aegis != null && MummerServer.Family(aegis) == MummerData.Costume.Aegis);
            Kill(aegis);
            yield return Wait(0.6f);
            Check("an Aegis unlocks the Aegis costume", player.Aegis);
            // mining: the Loom strikes ore in reach
            int strikes = loom.Strikes;
            yield return Wait(3f);
            Entity_Master ore = Helper_Entityquerytools.Entitynearest(lp, MummerUtil.Stat(loom.Master, "Range", 2.5f), MummerUtil.Tagged("Resource"));
            Check("Loom picks at ore in reach", ore == null || loom.Strikes > strikes, ore == null ? "no ore in reach here (skipped)" : strikes + "->" + loom.Strikes);
        }

        // 5. COSTUME now cycles to Ballista; the next Understudy is stitched in it, with the felt bolt.
        if (stage != null && player.Ballista)
        {
            player.Thread = 20; player.Hero.ShowThread();
            Send(stage.Master, MummerData.CostumeLink, "");
            yield return Wait(0.4f);
            Link_Command costume = stage.Master.Commandlistserver[MummerData.CostumeLink];
            Check("COSTUME switches to Ballista", stage.Costume == MummerData.Costume.Ballista && costume.Goalstring == "Ballista" && stage.Master.Skin.Value.ToString() == "Ballista",
                stage.Costume + " / '" + costume.Goalstring + "' / skin '" + stage.Master.Skin.Value + "'");
            // A full Stage keeps its cast (only Quick Change re-dresses it): free a place so the next stitch is a Ballista.
            stage.Cast.RemoveAll(c => c == null || c.Master == null);
            if (stage.Cast.Count >= stage.Slots && stage.Cast.Count > 0)
                Kill(stage.Cast[0].Master);
            MummerPuppetBrain ballistaPuppet = null;
            yield return Until(() => (ballistaPuppet = stage.Cast.Find(c => c != null && c.Def.Costume == MummerData.Costume.Ballista)) != null, 9f);
            Check("a Ballista Understudy is stitched", ballistaPuppet != null, "cast " + stage.Cast.Count);
            if (ballistaPuppet != null)
            {
                yield return Wait(0.5f);
                Link_Command attack;
                Check("Ballista Understudy shoots felt bolts (stock ranged AI)", ballistaPuppet.Master.GetComponent<Entity_Ai_Mob_Ranged>() != null
                    && ballistaPuppet.Master.Commandlistserver.TryGetValue("Attack", out attack) && attack.Goalstring == MummerData.PuppetBolt);
            }
        }

        // 6. Understudies step out to meet a foe near their Stage and fight it.
        if (stage != null && stage.Cast.Count > 0)
        {
            Entity_Master foe = SpawnFoe("Enemy_Dagger", MummerUtil.Pos(stage.Master), 3f, 1.5f);
            bool foeSpawned = foe != null;
            yield return Wait(0.6f);
            float hp0 = foe != null ? MummerUtil.Stat(foe, "Health", 0f) : 0f;
            yield return Until(() => foe == null || !MummerUtil.Alive(foe) || stage.Cast.Exists(c => c != null && c.Target == foe), 5f);
            Check("an Understudy targets a foe near its Stage", foeSpawned && (foe == null || !MummerUtil.Alive(foe) || stage.Cast.Exists(c => c != null && c.Target == foe)));
#if MUMMER_SHOTS
            yield return Wait(1f);
            yield return MummerShots.Shot("puppets");
            yield return MummerShots.Close("puppets", MummerUtil.Pos(stage.Master), 6f);
#endif
            yield return Until(() => foe == null || !MummerUtil.Alive(foe) || MummerUtil.Stat(foe, "Health", hp0) < hp0, 8f);
            Check("Understudies hurt the foe", foeSpawned && hp0 > 0f && (foe == null || !MummerUtil.Alive(foe) || MummerUtil.Stat(foe, "Health", hp0) < hp0), "health " + hp0 + "->" + (foe != null ? MummerUtil.Stat(foe, "Health", 0f) : 0f));
            Kill(foe);
            yield return Wait(0.5f);
        }

        // 7. HOOK: a foe about four tiles away is reeled toward Mummer and Marked; felled Marked, it pays extra Thread.
        {
            Entity_Master foe = SpawnFoe("Enemy_Dagger", MummerUtil.Pos(hm), 5.2f, 2.6f);
            yield return Wait(0.6f);
            if (Check("a foe to hook", foe != null))
            {
                float d0 = Vector3.Distance(MummerUtil.Pos(foe), MummerUtil.Pos(hm));
                int hooks = hero.Hooks, marksPaid0 = MummerServer.MarksPaid;
                Send(hm, MummerData.HookLink, "{\"Ref\":" + foe.NetworkObjectId + "}");
                // The closest the foe comes during the reel and the root that follows (it may walk on after).
                float d1 = d0, reelEnd = Time.time + 1.1f;
#if MUMMER_SHOTS
                bool shot = false;
#endif
                while (Time.time < reelEnd && foe != null && MummerUtil.Alive(foe))
                {
                    d1 = Mathf.Min(d1, Vector3.Distance(MummerUtil.Pos(foe), MummerUtil.Pos(hm)));
#if MUMMER_SHOTS
                    if (!shot && Time.time > reelEnd - 0.6f)
                    {
                        shot = true;
                        yield return MummerShots.Close("hook", (MummerUtil.Pos(hm) + MummerUtil.Pos(foe)) * 0.5f, 6f);
                    }
#endif
                    yield return null;
                }
                Check("HOOK casts", hero.Hooks == hooks + 1);
                Check("HOOK Marks the foe", MummerServer.IsMarked(foe));
                yield return DebuffReport(foe, MummerData.VfxMarked);
                Check("HOOK reels the foe toward Mummer", d1 < d0 - 0.8f, F(d0) + "->" + F(d1) + ", reel " + hero.LastReel);
                // The reel is the foe walking on its own movement; once the hold ends its AI must take up its path again.
                Vector3 held = MummerUtil.Pos(foe);
                yield return Until(() => foe == null || !MummerUtil.Alive(foe) || Vector3.Distance(MummerUtil.Pos(foe), held) > 0.4f, 4f);
                Check("a reeled foe walks on afterwards (pathing intact)", foe != null && MummerUtil.Alive(foe) && Vector3.Distance(MummerUtil.Pos(foe), held) > 0.4f, "state " + (foe != null ? foe.State.Value.ToString() : "-"));
                Send(hm, MummerData.HookLink, "");
                yield return Wait(0.3f);
                Check("HOOK recoils (cooldown)", hero.Hooks == hooks + 1 && hm.Commandlistserver[MummerData.HookLink].Goalstring == "Off");
#if MUMMER_SHOTS
                if (foe != null && MummerUtil.Alive(foe))
                    yield return MummerShots.Close("marked", MummerUtil.Pos(foe), 4f);
#endif
                // Reeled up to Mummer, the foe may fall to Mummer's own swings before the test fells it: count the bonus
                // payments since the HOOK rather than the Thread at the kill.
                int t0 = player.Thread;
                bool nearLoom = foe != null && loom != null && Vector3.Distance(MummerUtil.Pos(foe), MummerUtil.Pos(loom.Master)) <= loom.Scraprange;
                bool fellEarly = foe == null || !MummerUtil.Alive(foe) || MummerServer.MarksPaid > marksPaid0;
                Kill(foe);
                yield return Wait(0.6f);
                int expect = t0 + MummerData.MarkBonus + (nearLoom ? MummerData.ScrapSmall : 0);
                Check("a Marked foe pays " + MummerData.MarkBonus + " extra Thread", MummerServer.MarksPaid == marksPaid0 + 1 && (fellEarly || player.Thread == Mathf.Min(MummerData.ThreadMax, expect)),
                    "bonuses " + (MummerServer.MarksPaid - marksPaid0) + ", Thread " + t0 + "->" + player.Thread + " (near a Loom: " + nearLoom + ", fell before the kill: " + fellEarly + ")");
            }
        }

        // 8. Marionette Cross: grabs a foe in reach; the held foe can't move and flails at its neighbour.
        if (cross != null)
        {
            Vector3 cp = MummerUtil.Pos(cross.Master);
            Entity_Master big = SpawnFoe("Enemy_Aegis", cp, 3.5f, 1.5f);
            yield return Until(() => big != null && cross.Holds(big), 9f);
            Check("Cross grabs a foe", big != null && cross.Holds(big), "grabs " + cross.Grabs);
            Check("a held foe can't move", big != null && MummerUtil.Stat(big, "Speed", 1f) <= 0.05f, big != null ? "speed " + F(MummerUtil.Stat(big, "Speed", 1f)) : "none");
            yield return DebuffReport(big, MummerData.VfxHeld);
            Entity_Master small = big != null ? SpawnFoe("Enemy_Dagger", MummerUtil.Pos(big), 1.6f, 0.8f) : null;
#if MUMMER_SHOTS
            yield return Wait(0.4f);
            yield return MummerShots.Close("cross", (cp + (big != null ? MummerUtil.Pos(big) : cp)) * 0.5f, 5f);
#endif
            int flails = cross.Flails;
            yield return Until(() => cross.Flails > flails, 4f);
            Check("a held foe flails at its neighbour", small != null && cross.Flails > flails, flails + "->" + cross.Flails);
            Kill(big); Kill(small);
            yield return Wait(0.5f);
        }

        // 9. Curtain: foes can walk through it, and come out tangled (slowed, no attacks for a moment).
        if (curtain != null)
        {
            Genitem tile = curtain.Master.Beingserver.Currentblock;
            Beingobject dagger = MummerUtil.Load<Beingobject>("Mob", "Enemy_Dagger");
            Check("Curtain is a walk-through tile", tile != null && dagger != null && Helper_Gridtools.Checkifmoveable(tile, new Link_Being { Being = dagger, Entity = dagger.Entity, Block = dagger.Block, Team = team + 1 }) != null);
            Entity_Master foe = SpawnFoe("Enemy_Dagger", MummerUtil.Pos(curtain.Master), 0.5f, 0f, true);
            yield return Until(() => foe == null || curtain.HasTangled(foe), 3f);
            yield return Wait(0.3f);
            float speed = foe != null ? MummerUtil.Stat(foe, "Speed", 1f) : 1f, baseSpeed = foe != null ? BaseStat(foe, "Speed") : 1f;
            Check("Curtain tangles a foe passing through", foe != null && curtain.HasTangled(foe) && speed < baseSpeed - 0.05f, "speed " + F(baseSpeed) + "->" + F(speed));
            yield return DebuffReport(foe, MummerData.VfxTangled);
#if MUMMER_SHOTS
            yield return MummerShots.Close("curtain", MummerUtil.Pos(curtain.Master), 4f);
#endif
            Kill(foe);
        }
#if MUMMER_SHOTS
        if (stage != null && loom != null && cross != null && curtain != null)
            yield return MummerShots.Close("buildings", (MummerUtil.Pos(stage.Master) + MummerUtil.Pos(loom.Master) + MummerUtil.Pos(cross.Master) + MummerUtil.Pos(curtain.Master)) * 0.25f, 7f);
        yield return MummerShots.Close("mummer", MummerUtil.Pos(hm), 3.2f);
#endif
        // 10. Upgrades: paid, applied through the stock flow, and doing what they say.
        if (loom != null)
        {
            float range = loom.Scraprange;
            float yellow = Res(owner, "Yellowaura");
            yield return Upgrade(loom, "Weft2", owner);
            Check("Long Weft widens the Loom's Scrap range", Has(loom, "Weft2") && loom.Scraprange > range + 1.5f, F(range) + "->" + F(loom.Scraprange));
            Check("Long Weft paid", Mathf.Abs((yellow - Res(owner, "Yellowaura")) - 25f) < 0.01f, "Yellow spent " + (yellow - Res(owner, "Yellowaura")));
            yield return Upgrade(loom, "Patchwork4", owner);
            Check("a tier-4 upgrade is not offered from tier 2", !Has(loom, "Patchwork4"));
        }
        if (stage != null)
        {
            int slots = stage.Slots;
            yield return Upgrade(stage, "Strings2", owner);
            yield return Upgrade(stage, "Double3", owner);
            Check("Long Strings -> Double Bill: one more Understudy", Has(stage, "Double3") && stage.Slots == slots + 1 && stage.Leash > 6.5f, "slots " + slots + "->" + stage.Slots + ", leash " + F(stage.Leash));
        }
        if (curtain != null)
        {
            yield return Upgrade(curtain, "Spotlight2", owner);
            Entity_Master foe = SpawnFoe("Enemy_Dagger", MummerUtil.Pos(curtain.Master), 0.5f, 0f, true);
            yield return Until(() => foe == null || curtain.HasTangled(foe), 3f);
            Check("Spotlight Marks foes passing through", Has(curtain, "Spotlight2") && foe != null && MummerServer.IsMarked(foe));
            Kill(foe);
        }
        if (cross != null)
        {
            float aspd = MummerUtil.Stat(cross.Master, "Attackspeed", 0f);
            yield return Upgrade(cross, "Quickgrab2", owner);
            Check("Quick Grab grabs more often", Has(cross, "Quickgrab2") && MummerUtil.Stat(cross.Master, "Attackspeed", 0f) > aspd, F(aspd) + "->" + F(MummerUtil.Stat(cross.Master, "Attackspeed", 0f)));
        }
#if MUMMER_SHOTS
        yield return Wait(1f);
        yield return MummerShots.Shot("upgraded");
#endif

        // 11. Tides: half the Thread unravels; with a Silk Loom it no longer does.
        {
            player.Thread = 20; player.Hero.ShowThread();
            int[] reached = new int[1];
            yield return ForceTide(reached);
            Check("Tide advanced", MummerServer.Tide == reached[0] && reached[0] > 1, "Tide " + MummerServer.Tide);
            Check("half the Thread unravels at a new Tide", player.Thread <= 12, "20->" + player.Thread);
            if (loom != null)
            {
                yield return Upgrade(loom, "Silk3", owner);
                int keep = player.Thread;
                yield return ForceTide(reached);
                Check("Silk: Thread no longer unravels", Has(loom, "Silk3") && player.Thread >= keep, keep + "->" + player.Thread);
            }
        }

        // 12. The journal: Mummer's pages, and every vessel page with exactly one tab per vessel (Smacky included).
        {
            Logobject page = MummerContent.Entry.Log;
            bool own = page != null && page.name == "Log_" + MummerData.Hero, being = false;
            Logobject stagePage = null, upgradePage = null, unitPage = null;
            if (own)
                foreach (Logitems.Base b in AllItems(page.Elements))
                {
                    if (b is Logitems.TextImageBeing && ((Logitems.TextImageBeing)b).Entity == MummerContent.HeroBeing.Entity) being = true;
                    Logitems.Link l = b as Logitems.Link;
                    if (l != null && !l.Sidelink && l.Target != null && l.Target.name == "Log_" + MummerData.Hero + "_" + MummerData.Stage) stagePage = l.Target;
                }
            Check("journal: Mummer has their own vessel page", own && being);
            Check("journal: Mummer's page links the Puppet Stage's page", stagePage != null);
            if (stagePage != null)
                foreach (Logitems.Base b in AllItems(stagePage.Elements))
                {
                    if (b is Logitems.LinkUpgrade && ((Logitems.LinkUpgrade)b).Target != null && upgradePage == null) upgradePage = ((Logitems.LinkUpgrade)b).Target;
                    if (b is Logitems.LinkImage && ((Logitems.LinkImage)b).Target != null && unitPage == null) unitPage = ((Logitems.LinkImage)b).Target;
                }
            Check("journal: the Stage page links its upgrades", upgradePage != null && upgradePage.name.Contains("Mummerstage"), upgradePage != null ? upgradePage.name : "none");
            Check("journal: the Stage page links the Understudies", unitPage != null && unitPage.name.StartsWith("Log_Mob_Understudy"), unitPage != null ? unitPage.name : "none");
            List<Logobject> vesselPages = new List<Logobject>();
            foreach (UnityEngine.Object o in MummerUtil.Folder("Hero").Values)
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
#if MUMMER_SHOTS
            Bl_Menus.Changemenu("Journal");
            List<Logobject> show = new List<Logobject> { page, stagePage, upgradePage, unitPage };
            foreach (string other in new[] { "Hero_Smacky", "Hero_Sprigg" })
            {
                Heroentryobject h = MummerUtil.Heroentry(other);
                if (h != null && h.Log != null) show.Add(h.Log);
            }
            foreach (Logobject shown in show)
            {
                if (shown == null) continue;
                Loghandler.Log_Current = shown;
                yield return Wait(2.5f);
                yield return MummerShots.Shot("journal_" + shown.name);
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
