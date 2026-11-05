using Blockage.Menu;
#if ULOE_SELFTEST
// Debug build only (Build-UloeMod.ps1 -Define ULOE_SELFTEST[,ULOE_SHOTS]). Starts a solo match by itself (Embark,
// pick Uloe, pick a map, ready up) because the game ignores injected input, then plays through every Uloe mechanic
// on the host the way a player would (real Build, Sell, PLUG IN, AIM and Upgrade commands through Command_ServerRpc),
// spawns and fells foes, and logs one "[Uloe] TEST PASS|FAIL name: detail" line per check plus a final "TEST DONE"
// summary in Player.log. Sections follow DESIGN.md's test plan.
using UnityEngine.SceneManagement;
using Unity.Netcode;

public static partial class UloeSelfTest
{
    static int phase;
    static float seen = -1f, step = -1f;
    static string node = "";
    static bool freeze;
    static int pass, fail;

    static void Log(string m) { UloeLog.Info("TEST " + m); }

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
        if (freeze && Game_Mapvars.Instance != null && ModKitUtil.IsServer && Game_Mapvars.Instance.Time_Wave.Value < 600f)
            Game_Mapvars.Instance.Time_Wave.Value = 900f;
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
#if ULOE_SHOTS
            if (!boatShot && now - seen >= 4f && Macro_Worldvisual.Instance != null)
            {
                boatShot = true;
                UloeMod.Instance.StartCoroutine(BoatShot());
            }
            if (now - seen < 16f)
                return;
#else
            if (now - seen < 8f)
                return;
#endif
            if (step < 0f)
            {
                macro.Selectcharacter(UloeData.Hero);
                node = PickNode();
                Log("picked Uloe and map node '" + node + "'");
                if (node != "")
                    Tds_Multiplayer_Remote.Instance.Selectmap_ServerRpc(node, false, default(ServerRpcParams));
                step = now;
                return;
            }
            if (now - step > 3f && now - step < 4f)
            {
                Tds_Clientinfo info = Blm_Server.Instance.Localplayer.GetComponent<Tds_Clientinfo>();
                Check("vessel select", info != null && info.Hero.Value.ToString() == UloeData.Hero, info != null ? info.Hero.Value.ToString() : "no clientinfo");
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

        if (phase <= 2 && UloeServer.Heroes.Count > 0 && Game_Mapvars.Instance != null && ModKitUtil.IsServer)
        {
            phase = 3;
#if ULOE_REVIEW
            UloeMod.Instance.StartCoroutine(Review());
#else
            UloeMod.Instance.StartCoroutine(Scenario());
#endif
        }
    }

#if ULOE_SHOTS
    static bool boatShot;

    // The boat's vessel select, opened as the Setup screen's button does, captured with Uloe standing on it.
    static IEnumerator BoatShot()
    {
        Macro_Worldvisual.Instance.Opencharselect();
        yield return new WaitForSecondsRealtime(3f);
        Macro_Characterselect_Click mine = null;
        int buttons = 0;
        foreach (Macro_Characterselect_Click b in UnityEngine.Object.FindObjectsOfType<Macro_Characterselect_Click>())
        {
            buttons++;
            if (b.Character == UloeData.Hero) mine = b;
        }
        Dictionary<string, UnityEngine.Object> heroes = ModKitUtil.Folder("Hero");
        Check("Uloe unlocked on the boat's vessel select", mine != null && !mine.Locked, mine == null ? "no button" : "locked " + mine.Locked);
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
        yield return UloeShots.Shot("boat");
        if (mine != null)
        {
            Macro_Main.Instance.Selectcharacter(UloeData.Hero);
            yield return new WaitForSecondsRealtime(1.5f);
            yield return UloeShots.Shot("boat_selected");
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
        return ModKitTest.Spot(ModKitUtil.Pos(hero), being, ModKitUtil.Team(hero), ring, 8f, used, being.Objectname);
    }

    static T Newest<T>(string being, List<UloeBuilding> known) where T : UloeBuilding
    {
        foreach (UloeBuilding b in UloeServer.Buildings)
            if (b != null && b.Started && b.Def.Id == being && !known.Contains(b))
                return b as T;
        return null;
    }

    static IEnumerator Build(Entity_Master hero, string tower, Vector3 at, List<UloeBuilding> known, UloeBuilding[] result)
    {
        result[0] = null;
        string json = "{\"Tower\":\"" + tower + "\",\"Position\":" + Vec(at) + ",\"Orientation\":0,\"Owner\":" + ModKitUtil.Owner(hero) + "}";
        Send(hero, "Build", json);
        float t = 0f;
        while (t < 20f && result[0] == null)
        {
            result[0] = Newest<UloeBuilding>(tower, known);
            t += Time.deltaTime;
            yield return null;
        }
        if (result[0] != null)
            known.Add(result[0]);
    }

    static int Team() { return UloeServer.Heroes.Count > 0 ? ModKitUtil.Team(UloeServer.Heroes[0].Master) : 1; }

    // Removes Tide foes so they don't pile up.
    static void ClearFoes()
    {
        int team = Team();
        foreach (Entity_Master e in new List<Entity_Master>(Game_Entity.Instance.Entityclient))
            if (ModKitUtil.IsFoe(e, team))
                Game_Entity.Instance.Despawn(e.Beingserver, false, "Selftest");
    }

    // Spawned as a child of the enemy core, like Tide foes (kit ModKitTest.SpawnFoe): the nearest ground tile between minDist
    // and maxDist from 'near' where the foe can stand; building tiles are skipped unless asked for.
    static Entity_Master SpawnFoe(string being, Vector3 near, float maxDist, float minDist = 0f, bool onBuildings = false)
    {
        return ModKitTest.SpawnFoe(being, near, minDist, maxDist, onBuildings ? null : used);
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

    // A foe's tags and stats arrive a frame after its spawn (Entity_Master.Start): killed or read in its spawn frame it is
    // not yet "Enemy" and has no Health (round 1). The checks wait for both.
    static bool Loaded(Entity_Master e) { return ModKitUtil.Alive(e) && e.Taglistserver != null && e.Taglistserver.Contains("Enemy") && e.Statsserver != null && e.Statsserver.ContainsKey("Health"); }

    // A loaded foe between minDist and maxDist from 'near': slot[0], or null when none could spawn or it fell while loading.
    static IEnumerator Foe(string being, Vector3 near, float maxDist, float minDist, Entity_Master[] slot)
    {
        slot[0] = null;
        Entity_Master e = SpawnFoe(being, near, maxDist, minDist);
        if (e == null)
            yield break;
        yield return Until(() => !ModKitUtil.Alive(e) || Loaded(e), 2f);
        if (Loaded(e))
            slot[0] = e;
    }

    // A loaded foe beside the enemy core, far from Uloe's towers, kept while it stands: the Mirror and mark checks hit with it
    // from afar (Spool round 1: units felled the Daggers beside the buildings, and a hit from a fallen foe lands nowhere).
    static IEnumerator FarFoe(string being, Entity_Master[] slot)
    {
        if (slot[0] != null && Loaded(slot[0]) && ModKitUtil.BeingName(slot[0]) == being)
            yield break;
        slot[0] = null;
        Entity_Master core = ModKitTest.Core();
        if (core != null)
            yield return Foe(being, ModKitUtil.Pos(core), 6f, 1f, slot);
    }

    static IEnumerator Upgrade(UloeBuilding b, string key, long owner)
    {
        string id = b.Def.Id + "_" + key + "_Upgradeobject";
        Send(b.Master, "Upgrade", "{\"Upgrade\":\"" + id + "\",\"Upgrader\":" + owner + "}");
        yield return Wait(0.6f);
    }

    static bool Has(UloeBuilding b, string key) { return b != null && b.Master != null && b.Master.Upgradelistserver.ContainsKey(b.Def.Id + "_" + key + "_Upgradeobject"); }

    static List<Logitems.Base> AllItems(List<Logitems.Base> items) { return ModKitVesselJournal.Flat(items); }

    // ---------------------------------------------------------------- Uloe helpers

    static IEnumerator Walk(Entity_Master m, Vector3 to, float near, float timeout)
    {
        ModKitUtil.MoveTo(m, null, to, 0f);
        yield return Until(() => m == null || ModKitUtil.Flat(ModKitUtil.Pos(m), to) <= near, timeout);
    }

    // Up to a building or node, as the vessel walks to what it builds next to (the thing's own tile isn't walkable).
    static IEnumerator Near(Entity_Master m, Entity_Master to, float within)
    {
        ModKitUtil.MoveTo(m, to, Vector3.zero, Mathf.Max(0f, within - 1f));
        yield return Until(() => m == null || to == null || ModKitUtil.Flat(ModKitUtil.Pos(m), ModKitUtil.Pos(to)) <= within, 15f);
    }

    static void Hit(Entity_Master from, Entity_Master to, float amount) { ModKitCombat.Hurt(from, to, amount, 1f, null, null); }

    static int Beings(string being)
    {
        int n = 0;
        foreach (Entity_Master e in Game_Entity.Instance.Entityclient)
            if (e != null && ModKitUtil.BeingName(e) == being) n++;
        return n;
    }

    static string Sell(Entity_Master hero, Entity_Master what)
    {
        return "{\"Tower\":\"" + what.name + "\",\"Position\":" + Vec(ModKitUtil.Pos(what)) + ",\"Owner\":" + ModKitUtil.Owner(hero) + "}";
    }

    static string Ref(Entity_Master m) { return "{\"Ref\":" + m.NetworkObjectId + "}"; }

    static List<UloePhotonBrain> PhotonsOf(UloeBankBrain bank)
    {
        List<UloePhotonBrain> l = new List<UloePhotonBrain>();
        foreach (UloePhotonBrain p in UloeServer.Photons)
            if (p != null && p.Master != null && p.Home == bank) l.Add(p);
        return l;
    }

    static bool Near(float a, float b, float within = 0.01f) { return Mathf.Abs(a - b) <= within; }

    // A stock ore node beside a point (RogueLab round 7's way: owner -1, Forced, on a free tile), for a map with none close by.
    static Entity_Master SpawnNode(Vector3 near, int team)
    {
        Beingobject node = null;
        foreach (string n in new[] { "Resource_Red", "Resource_Blue", "Resource_Yellow" })
            if (node == null) node = ModKitUtil.Load<Beingobject>("Mob", n);
        if (node == null)
        {
            Log("no ore node being is loaded on this map");
            return null;
        }
        Vector3 at = ModKitTest.Spot(near, node, team, 2f, 6f, used, node.Objectname);
        if (float.IsNaN(at.x))
            return null;
        Link_Spawn s = new Link_Spawn();
        s.Being = node;
        s.Genadd = ModKitTest.Surface(at);
        s.Skin = "";
        s.Spawnstyle = Link_Spawn.Spawnstyles.Forced;
        s.Team = team;
        s.Owner = -1;
        Link_Being made = Helper_Entitytools.Spawn(s, at, null, false, null);
        return made != null ? made.Worldobject : null;
    }

    // Ground a unit can get to. This map climbs (round 1): the kit's FoeTile stays within a level of its point, found nothing
    // far enough away, and so the vessel never walked off; and a node on a one-tile ridge cut off the tiles behind it, where
    // Build's walk gives up. These look at all the ground, nearest first, and ask the game's own path.
    static List<Genitem> GroundBetween(Vector3 c, float min, float max)
    {
        List<Genitem> tiles = new List<Genitem>();
        List<Entity_Master> surfaces;
        if (Game_Entity.Instance == null || !Game_Entity.Instance.Entitiesserver.TryGetValue("Surface", out surfaces) || surfaces == null)
            return tiles;
        foreach (Entity_Master s in surfaces)
        {
            Genitem g = s != null && s.Beingserver != null ? s.Beingserver.Currentblock : null;
            if (g == null || tiles.Contains(g) || used.Contains(g.Position))
                continue;
            float d = ModKitUtil.Flat(g.Position, c);
            if (d >= min && d <= max)
                tiles.Add(g);
        }
        tiles.Sort((a, b) => ModKitUtil.Flat(a.Position, c).CompareTo(ModKitUtil.Flat(b.Position, c)));
        return tiles;
    }

    static Genitem Walkable(Entity_Master walker, Genitem tile)
    {
        Link_Being being = walker != null ? walker.Beingserver : null;
        if (being == null || being.Currentblock == null)
            return null;
        try
        {
            Genitem stand = Helper_Gridtools.Checkifstandable(tile, being);
            return stand != null && Helper_Navtools.Getpath(being.Currentblock, stand, being, false).Valid ? stand : null;
        }
        catch (Exception) { return null; }
    }

    // The nearest tile this unit can walk to between min and max tiles (flat) of a point, on any level.
    static Genitem WalkTile(Entity_Master walker, Vector3 from, float min, float max)
    {
        foreach (Genitem g in GroundBetween(from, min, max))
        {
            Genitem stand = Walkable(walker, g);
            if (stand != null)
                return stand;
        }
        return null;
    }

    // The nearest free tile between min and max tiles of a point where 'being' can be built and the vessel can get to.
    // None: NaN (float.IsNaN(at.x)). The one found goes on the used list.
    static Vector3 ReachSpot(Entity_Master hero, Vector3 c, Beingobject being, int team, float min, float max, Func<Vector3, bool> also = null)
    {
        foreach (Genitem g in GroundBetween(c, min, max))
        {
            bool ok = false;
            try { ok = Helper_Gridtools.Checkifplaceable(g, being, 0, team); } catch (Exception) { }
            if (!ok || (also != null && !also(g.Position)) || Walkable(hero, g) == null)
                continue;
            used.Add(g.Position);
            return g.Position;
        }
        return new Vector3(float.NaN, 0f, 0f);
    }

    // ---------------------------------------------------------------- the scenario

    static IEnumerator Scenario()
    {
        Log("match started: scenario begins");
        UloeHeroBrain hero = UloeServer.Heroes[0];
        yield return Until(() => Game_Mapvars.Instance != null && Game_Mapvars.Instance.Wave.Value >= 1, 150f);
        freeze = true;
        yield return Wait(2f);
        ClearFoes();
        Entity_Master hm = hero.Master;
        long owner = ModKitUtil.Owner(hm);
        int team = ModKitUtil.Team(hm);
        UloePlayer player = UloeServer.Player(owner);
        Vector3 home = ModKitUtil.Pos(hm);
        Log("tide " + Game_Mapvars.Instance.Wave.Value + ", vessel at " + home + ", owner " + owner + ", team " + team);
        ModKitTest.Log = Log;
        ModKitTest.MapDump(team, hm);
        Helper_Resources.Giveresources(ModKitUtil.Costs("Redaura=20000;Blueaura=20000;Yellowaura=20000"), owner, Helper_Resources.Styles.Default);
        yield return Wait(0.5f);
        List<UloeBuilding> known = new List<UloeBuilding>();
        UloeBuilding[] got = new UloeBuilding[1];
        Entity_Master[] slot = new Entity_Master[1];
        Entity_Master far = null;
        Vector3 at;
        UloeLight test = new UloeLight();

        // 1. The vessel.
        Check("vessel uses the stock ranged AI", hm.GetComponent<Entity_Ai_Mob_Ranged>() != null && hm.GetComponent<Entity_Ai_Mob_Combat>() == null && hm.GetComponent<Entity_Ai_Hero_Buildsell>() != null);
        Check("vessel has the Bright Idea tooltip", hm.Upgradelistserver.ContainsKey(UloeData.HeroTooltip));
        Check("vessel stats", Near(ModKitUtil.Stat(hm, "Range", 0), 3f) && Near(ModKitUtil.Stat(hm, "Health", 0), 30f) && Near(ModKitUtil.Stat(hm, "Speed", 0), 2.25f) && Near(ModKitUtil.Stat(hm, "Damage", 0), 2f),
            "Range " + F(ModKitUtil.Stat(hm, "Range", 0)) + ", Health " + F(ModKitUtil.Stat(hm, "Health", 0)) + ", Speed " + F(ModKitUtil.Stat(hm, "Speed", 0)) + ", Damage " + F(ModKitUtil.Stat(hm, "Damage", 0)));
        {
            Link_Command attack = null;
            hm.Commandlistserver.TryGetValue("Attack", out attack);
            Check("the vessel shoots its spark", hero.Armed && attack != null && attack.Goalstring == UloeData.Spark, attack != null ? attack.Goalstring + ", from " + UloeContent.SparkFrom : "no Attack command");
            Link_Command plug;
            Check("PLUG IN command", hm.Commandlistserver.TryGetValue(UloeData.PlugLink, out plug) && plug.Controllable && UloeContent.Plug.Commandfuncts[0] is UloePlugSelect);
            // Every key a Uloe button names must be one of the game's input actions: selecting a thing looks each up in
            // Bl_Inputs.Actions (Game_Selection.Addcommand) and throws on a miss (pitfall #113).
            List<string> bad = new List<string>();
            foreach (Commandobject c in new[] { UloeContent.Plug, UloeContent.Aim })
                if (c != null && c.Keybind != null)
                    foreach (string k in c.Keybind)
                        if (!Blockage.Bl_Inputs.Actions.ContainsKey(k)) bad.Add(c.name + " " + k);
            Check("every Uloe button's keys are the game's input actions", bad.Count == 0, bad.Count == 0 ? Blockage.Bl_Inputs.Actions.Count + " actions" : string.Join(", ", bad.ToArray()));
        }
        Check("the build menu holds the four", hero.Menu() == ModKitUtil.Join(UloeData.Towers), hero.Menu());
        Check("vessel visible to clients", hm.Client != null && hm.Client.Beingobject != null && hm.Client.Beingobject.Objectname == UloeData.Hero);
        Check("the beam is the stock Projectile_Ai_Beam", UloeContent.BeamReady, UloeContent.BeamFrom);

        // 2. Light: a Glass by the vessel, then a walk away.
        at = Spot(hm, UloeContent.Beings[UloeData.Glass], 2f);
        float red = Res(owner, "Redaura");
        yield return Build(hm, UloeData.Glass, at, known, got);
        UloeGlassBrain glass = got[0] as UloeGlassBrain;
        if (!Check("build a Magnifying Glass", glass != null, "at " + at))
        {
            Log("DONE pass=" + pass + " fail=" + fail + " setup=" + ModKitTest.Setups + " (no Glass: nothing else can be tested)");
            phase = 9;
            yield break;
        }
        Check("the Glass costs Red 30", Near(red - Res(owner, "Redaura"), 30f), "spent " + F(red - Res(owner, "Redaura")));
        Check("the Glass has a Light stat", glass.Master.Statsserver.ContainsKey(UloeData.LightStat));
        Check("a Glass is visible to clients", glass.Master.Client != null && glass.Master.Client.Beingobject != null);
        yield return Walk(hm, glass.Pos + (home - glass.Pos).normalized * 1.5f, 1.6f, 8f);
        yield return Wait(1.6f);
        Check("Uloe lights what stands within 3 tiles: Light " + F(UloeData.HeroLight), Near(ModKitUtil.Stat(glass.Master, "Light", -1f), UloeData.HeroLight), "Light " + F(ModKitUtil.Stat(glass.Master, "Light", -1f)) + ", " + F(ModKitUtil.Flat(glass.Pos, ModKitUtil.Pos(hm))) + " tiles away");
        test.Feed(glass.Master, 2f);
        yield return Wait(0.4f);
        Check("two sources sum", Near(ModKitUtil.Stat(glass.Master, "Light", -1f), UloeData.HeroLight + 2f), "Light " + F(ModKitUtil.Stat(glass.Master, "Light", -1f)));
        test.Feed(glass.Master, 3f);
        yield return Wait(0.4f);
        Check("a source replaces its own last share (pitfall #119)", Near(ModKitUtil.Stat(glass.Master, "Light", -1f), UloeData.HeroLight + 3f), "Light " + F(ModKitUtil.Stat(glass.Master, "Light", -1f)));
        yield return Wait(1.5f);
        Check("a source that stops fades by itself", Near(ModKitUtil.Stat(glass.Master, "Light", -1f), UloeData.HeroLight), "Light " + F(ModKitUtil.Stat(glass.Master, "Light", -1f)));
        Genitem awayTile = WalkTile(hm, glass.Pos, UloeData.HeroRadius + 2f, UloeData.HeroRadius + 6f);
        if (awayTile != null)
            yield return Walk(hm, awayTile.Position, 0.8f, 15f);
        else
            Log("no tile the vessel can walk to beyond its light");
        yield return Wait(2f);
        Check("Light is gone about 1.5 s after Uloe leaves", Near(ModKitUtil.Stat(glass.Master, "Light", -1f), 0f), "Light " + F(ModKitUtil.Stat(glass.Master, "Light", -1f)) + ", " + F(ModKitUtil.Flat(glass.Pos, ModKitUtil.Pos(hm))) + " tiles away");
        yield return Wait(1.5f);
        Check("Light's look: no skin at no Light", glass.Master.Skin.Value.ToString() == "", glass.Master.Skin.Value.ToString());
        test.Feed(glass.Master, 5f);
        yield return Wait(0.8f);
        Check("Light's look: the band's skin", glass.Master.Skin.Value.ToString() == "Lit2", glass.Master.Skin.Value.ToString() + ", band " + glass.LookBand);
#if ULOE_SHOTS
        yield return UloeShots.Close("glass_lit", glass.Pos, 4f);
#endif
        yield return Wait(4f);
        Check("Light's look: the skin goes", glass.Master.Skin.Value.ToString() == "", glass.Master.Skin.Value.ToString());

        // 3. Plug In.
        Entity_Master node = UloeUtil.NearestNode(glass.Pos, 7f);
        if (node == null)
            node = SpawnNode(glass.Pos, team);
        yield return Wait(1f);
        if (!UloeUtil.IsNode(node))
        {
            ModKitTest.Setup("Plug In", "no node near the Glass, and none could be spawned");
            node = null;
        }
        else
        {
            float aura = UloeUtil.NodeAura(node);
            Log("node " + ModKitUtil.BeingName(node) + " " + F(ModKitUtil.Flat(ModKitUtil.Pos(node), glass.Pos)) + " from the Glass, total aura " + F(aura));
            float want = UloeHeroBrain.PlugLight(node);
            Check("a node's Light comes from its total aura", Near(want, Mathf.Min(UloeData.NodeMax, UloeData.NodeBase + UloeData.NodePerAura * aura)), "aura " + F(aura) + " -> Light " + F(want));
            Send(hm, UloeData.PlugLink, Ref(node));
            yield return Wait(0.4f);
            Check("PLUG IN refuses what the cord won't reach", hero.Plug == null && hero.LastPlug == "too far" || ModKitUtil.Flat(ModKitUtil.Pos(node), ModKitUtil.Pos(hm)) <= hero.Reach + UloeData.PlugSlack, hero.LastPlug + ", " + F(ModKitUtil.Flat(ModKitUtil.Pos(node), ModKitUtil.Pos(hm))) + " tiles");
            if (hero.Plug != null) { Send(hm, UloeData.PlugLink, "{\"Unplug\":1}"); yield return Wait(0.8f); }
            // Between the node and the Glass, in reach of both.
            Genitem midTile = ModKitTest.FoeTile(UloeContent.Beings[UloeData.Photon], Vector3.Lerp(ModKitUtil.Pos(node), glass.Pos, 0.5f), 0f, 3f, used);
            if (midTile != null)
                yield return Walk(hm, midTile.Position, 1f, 12f);
            else
                yield return Near(hm, glass.Master, 3f);
            int plugs = hero.Plugs;
            Send(hm, UloeData.PlugLink, Ref(node));
            yield return Wait(0.4f);
            Check("PLUG IN goes into a node", hero.Plug == node && hero.Plugs == plugs + 1, hero.LastPlug);
            Check("the bulb burns while plugged (a skin)", hm.Skin.Value.ToString() == UloeData.Plugged, hm.Skin.Value.ToString());
            Link_Command pc;
            Check("the button says it is plugged", hm.Commandlistserver.TryGetValue(UloeData.PlugLink, out pc) && pc.Goalstring == "On", pc != null ? pc.Goalstring : "");
            yield return Wait(1.2f);
            Check("plugged in, Uloe gives the node's Light", Near(hero.LightNow, want) && Near(hero.RadiusNow, UloeData.PlugRadius), "Light " + F(hero.LightNow) + ", radius " + F(hero.RadiusNow));
            if (ModKitUtil.Flat(glass.Pos, ModKitUtil.Pos(hm)) <= UloeData.PlugRadius)
                Check("... and the Glass receives it", Near(ModKitUtil.Stat(glass.Master, "Light", -1f), want), "Light " + F(ModKitUtil.Stat(glass.Master, "Light", -1f)));
            else ModKitTest.Setup("Plug In's Light on the Glass", F(ModKitUtil.Flat(glass.Pos, ModKitUtil.Pos(hm))) + " tiles away");
#if ULOE_SHOTS
            yield return UloeShots.Shot("plugged");
            yield return UloeShots.Close("plugged", ModKitUtil.Pos(hm), 5f);
#endif
            Send(hm, UloeData.PlugLink, "{\"Unplug\":1}");
            yield return Wait(0.1f);
            Check("pressing it again unplugs", hero.Plug == null && hm.Skin.Value.ToString() == "", hm.Skin.Value.ToString());
            int refused = hero.Refused;
            Send(hm, UloeData.PlugLink, Ref(node));
            yield return Wait(0.1f);
            Check("PLUG IN has a cooldown", hero.Refused == refused + 1 && hero.LastPlug == "cooling down", hero.LastPlug);
            yield return Wait(UloeData.PlugCooldown + 0.3f);
            Send(hm, UloeData.PlugLink, "");
            yield return Wait(0.4f);
            Check("the hotkey takes the nearest in reach", hero.Plug != null, hero.LastPlug);
            int snaps = hero.Snaps;
            Genitem farTile = WalkTile(hm, ModKitUtil.Pos(node), hero.Reach + 2f, hero.Reach + 7f);
            if (hero.Plug != null && farTile != null)
            {
                yield return Walk(hm, farTile.Position, 0.8f, 20f);
                yield return Until(() => hero.Snaps > snaps, 8f);
                Check("walking past the cord's length snaps it", hero.Snaps == snaps + 1 && hero.Plug == null, F(ModKitUtil.Flat(ModKitUtil.Pos(node), ModKitUtil.Pos(hm))) + " tiles from the node, snaps " + hero.Snaps);
            }
            else ModKitTest.Setup("the cord snapping", "no tile beyond its length");
            if (hero.Plug != null) { Send(hm, UloeData.PlugLink, "{\"Unplug\":1}"); yield return Wait(0.8f); }
            yield return Near(hm, glass.Master, 3f);
        }

        // 4. Blind and Illuminate (a timed Addmod and a hit hook), with a foe far from everything.
        ClearFoes();
        yield return FarFoe("Enemy_Dagger", slot);
        far = slot[0];
        if (far == null)
            ModKitTest.Setup("Blind and Illuminate", "no foe could stand by the enemy core");
        else
        {
            float d0 = ModKitUtil.Stat(far, "Damage", 0f);
            UloeMarks.Blind(hm, far, 1);
            yield return Wait(0.2f);
            Check("Blind 1 takes 15% of a foe's Damage", Near(ModKitUtil.Stat(far, "Damage", 0f), d0 * 0.85f), F(d0) + " -> " + F(ModKitUtil.Stat(far, "Damage", 0f)));
            UloeMarks.Blind(hm, far, 2);
            yield return Wait(0.2f);
            Check("Blind 2 replaces it: 30%", Near(ModKitUtil.Stat(far, "Damage", 0f), d0 * 0.7f) && UloeMarks.BlindLevel(far) == 2, F(ModKitUtil.Stat(far, "Damage", 0f)) + ", level " + UloeMarks.BlindLevel(far));
            UloeMarks.Blind(hm, far, 1);
            yield return Wait(0.2f);
            Check("a weaker Blind does nothing", Near(ModKitUtil.Stat(far, "Damage", 0f), d0 * 0.7f) && UloeMarks.BlindLevel(far) == 2, F(ModKitUtil.Stat(far, "Damage", 0f)) + ", level " + UloeMarks.BlindLevel(far));
            UloeMarks.Blind(hm, far, 3);
            yield return Wait(0.2f);
            Check("Blind 3: 45%", Near(ModKitUtil.Stat(far, "Damage", 0f), d0 * 0.55f) && UloeMarks.BlindLevel(far) == 3, F(ModKitUtil.Stat(far, "Damage", 0f)));
            yield return Wait(UloeData.MarkSeconds[2] + 0.5f);
            Check("Blind wears off", Near(ModKitUtil.Stat(far, "Damage", 0f), d0) && UloeMarks.BlindLevel(far) == 0, F(ModKitUtil.Stat(far, "Damage", 0f)) + ", level " + UloeMarks.BlindLevel(far));
            float h0 = ModKitUtil.Health(far);
            Hit(hm, far, 1f);
            float plain = h0 - ModKitUtil.Health(far);
            UloeMarks.Illuminate(hm, far, 2);
            yield return Wait(0.2f);
            h0 = ModKitUtil.Health(far);
            Hit(hm, far, 1f);
            float lit2 = h0 - ModKitUtil.Health(far);
            Check("Illuminate 2: 30% more damage taken", Near(plain, 1f) && Near(lit2, 1.3f), "1 damage takes " + F(plain) + ", lit " + F(lit2));
            // A spark's or a beam's hit starts with empty Data, as Projectile_Master.Interact delivers it.
            h0 = ModKitUtil.Health(far);
            Helper_Entitytools.Interacthurt(hm, far, new Dictionary<string, float>());
            Check("a spark on an Illuminated foe takes the share too", Near(h0 - ModKitUtil.Health(far), ModKitUtil.Stat(hm, "Damage", 0f) * 1.3f), F(h0 - ModKitUtil.Health(far)) + " from Damage " + F(ModKitUtil.Stat(hm, "Damage", 0f)));
            UloeMarks.Illuminate(hm, far, 1);
            yield return Wait(0.1f);
            Check("a weaker Illuminate does nothing", UloeMarks.LitLevel(far) == 2, "level " + UloeMarks.LitLevel(far));
            UloeMarks.Illuminate(hm, far, 3);
            yield return Wait(0.2f);
            h0 = ModKitUtil.Health(far);
            Hit(hm, far, 1f);
            Check("Illuminate 3 replaces it: 45%", Near(h0 - ModKitUtil.Health(far), 1.45f) && UloeMarks.LitLevel(far) == 3, F(h0 - ModKitUtil.Health(far)));
            yield return Wait(UloeData.MarkSeconds[2] + 0.5f);
            h0 = ModKitUtil.Health(far);
            if (ModKitUtil.Alive(far))
            {
                Hit(hm, far, 1f);
                Check("Illuminate wears off", Near(h0 - ModKitUtil.Health(far), 1f) && UloeMarks.LitLevel(far) == 0, F(h0 - ModKitUtil.Health(far)));
            }
            else
                ModKitTest.Setup("Illuminate wears off", "the foe fell before it wore off");   // MAX ULO-9
            yield return FarFoe("Enemy_Dagger", slot);
            far = slot[0];
            if (far != null)
            {
                UloeMarks.Blind(hm, far, 1);
                UloeMarks.Illuminate(hm, far, 1);
                yield return Wait(0.2f);
                Kill(far);
                yield return Wait(1f);
                // BlindLevel reads 0 on a destroyed foe whatever is kept: ask the table itself (MAX ULO-5).
                Check("a marked foe falling leaves no mark behind", !UloeMarks.Holds(far));
            }
        }
        ClearFoes();

        // 5. The Magnifying Glass.
        {
            yield return Wait(2f);
            float d0 = ModKitUtil.Stat(glass.Master, "Damage", 0f) / (1f + glass.Multi);
            Check("the Glass's base Damage is at least 2", d0 > 1.9f, "Damage " + F(d0) + ", Light's share " + F(glass.Multi));
            test.Feed(glass.Master, 5f);
            yield return Wait(0.6f);
            float light = glass.Light;
            Check("Light raises its Damage: +20% a Light", Near(ModKitUtil.Stat(glass.Master, "Damage", 0f), d0 * (1f + UloeData.GlassPerLight * light), 0.02f), "Light " + F(light) + ", Damage " + F(ModKitUtil.Stat(glass.Master, "Damage", 0f)));
            int beamsBefore = glass.Beams;     // before the foe: the Glass can fire as it lands
            yield return Foe("Enemy_Dagger", glass.Pos, 3.5f, 1.5f, slot);
            Entity_Master foe = slot[0];
            if (foe != null)
            {
                float h0 = ModKitUtil.Health(foe);
                yield return Until(() => glass.Beams > beamsBefore + 1 || !ModKitUtil.Alive(foe), 6f);
                yield return Wait(0.3f);
                Check("the Glass beams the foe in Range", glass.Beams > beamsBefore, glass.Beams + " beams");
                Check("a beam hurts it", !ModKitUtil.Alive(foe) || ModKitUtil.Health(foe) < h0, F(h0) + " -> " + F(ModKitUtil.Alive(foe) ? ModKitUtil.Health(foe) : 0f));
                Check("the Glass's target is the nearest foe", glass.Target == foe || !ModKitUtil.Alive(foe));
#if ULOE_SHOTS
                yield return UloeShots.Close("glass_beam", glass.Pos, 5f);
#endif
            }
            else ModKitTest.Setup("the Glass's beam", "no Dagger could spawn in its Range");
            ClearFoes();
            yield return Wait(2.2f);
            yield return Upgrade(glass, "Burn2", owner);
            Check("Burning Point", Has(glass, "Burn2"));
            yield return Foe("Enemy_Aegis", glass.Pos, 3.5f, 1.5f, slot);
            foe = slot[0];
            if (foe != null)
            {
                int beams = glass.Beams;
                yield return Until(() => glass.Beams >= beams + 4 || !ModKitUtil.Alive(foe), 10f);
                if (ModKitUtil.Alive(foe))
                {
                    float want = (glass.Beams - beams - 1) * glass.Param("Ramp_Base", 0.12f);
                    Check("Burning Point: +12% a beam on the same target", Near(glass.Ramp, want, 0.13f) && glass.Ramp > 0f, "ramp " + F(glass.Ramp) + " after " + (glass.Beams - beams) + " beams");
                    Check("... on the Damage stat", ModKitUtil.Stat(glass.Master, "Damage", 0f) > d0 * (1f + glass.Ramp * 0.9f), "Damage " + F(ModKitUtil.Stat(glass.Master, "Damage", 0f)));
                    int resets = glass.Resets;
                    Kill(foe);
                    yield return Foe("Enemy_Dagger", glass.Pos, 3.5f, 1.5f, slot);
                    Entity_Master next = slot[0];
                    int b2 = glass.Beams;
                    yield return Until(() => glass.Beams > b2 || next == null || !ModKitUtil.Alive(next), 4f);
                    Check("a new target starts over", glass.Resets == resets + 1 && glass.Ramp <= glass.Param("Ramp_Base", 0.12f) + 0.001f, "resets " + glass.Resets + ", ramp " + F(glass.Ramp));
                }
                else ModKitTest.Setup("Burning Point's ramp", "the Aegis fell first");
            }
            else ModKitTest.Setup("Burning Point's ramp", "no Aegis could spawn in its Range");
            ClearFoes();
        }

        // 6. The Power Bank and its Photons.
        at = Spot(hm, UloeContent.Beings[UloeData.Bank], 3f);
        float yellow = Res(owner, "Yellowaura");
        red = Res(owner, "Redaura");
        yield return Build(hm, UloeData.Bank, at, known, got);
        UloeBankBrain bank = got[0] as UloeBankBrain;
        UloePhotonBrain ph = null;
        if (Check("build a Power Bank", bank != null, "at " + at))
        {
            Check("a Power Bank costs Red 40 + Yellow 20", Near(red - Res(owner, "Redaura"), 40f) && Near(yellow - Res(owner, "Yellowaura"), 20f), "spent Red " + F(red - Res(owner, "Redaura")) + ", Yellow " + F(yellow - Res(owner, "Yellowaura")));
            // The vessel off to one side, so what lights the Bank is a Photon.
            Genitem offTile = WalkTile(hm, bank.Pos, 5f, 9f);
            if (offTile != null)
                yield return Walk(hm, offTile.Position, 0.8f, 15f);
            float t0 = Time.time;
            yield return Until(() => PhotonsOf(bank).Count >= 2, 20f);
            float took = Time.time - t0;
            yield return Wait(4f);
            Check("a Power Bank keeps 2 Photons, one every 8 s", PhotonsOf(bank).Count == 2, PhotonsOf(bank).Count + " alive, the second after " + F(took) + " s");
            ph = PhotonsOf(bank).Find(p => p.Started);
            if (ph != null)
            {
                Check("a Photon fights with the game's melee AI", ph.Master.GetComponent<Entity_Ai_Mob_Combat>() != null);
                Check("a Photon is visible to clients", ph.Master.Client != null && ph.Master.Client.Beingobject != null);
                Check("Photon stats", Near(ModKitUtil.MaxHealth(ph.Master), 8f) && Near(ModKitUtil.Stat(ph.Master, "Speed", 0), 1.75f) && Near(ModKitUtil.Stat(ph.Master, "Damage", 0), 0.5f),
                    "health " + F(ModKitUtil.MaxHealth(ph.Master)) + ", speed " + F(ModKitUtil.Stat(ph.Master, "Speed", 0)) + ", damage " + F(ModKitUtil.Stat(ph.Master, "Damage", 0)));
                float maxLight = 0f, maxOff = 0f;
                float until = Time.time + 16f;
                while (Time.time < until)
                {
                    maxLight = Mathf.Max(maxLight, bank.Light);
                    foreach (UloePhotonBrain p in PhotonsOf(bank))
                        maxOff = Mathf.Max(maxOff, ModKitUtil.Flat(p.Pos, bank.Pos));
                    yield return Wait(0.4f);
                }
                Check("a Photon wanders", ph.Walks > 0 && ph.Travelled > 0.5f, ph.Walks + " walks, " + ph.Visits + " visits, " + F(ph.Travelled) + " tiles");
                Check("... within its leash", maxOff <= UloeData.PhotonLeash + 1.6f, "farthest " + F(maxOff) + " tiles");
                Check("a Photon gives Light", maxLight >= UloeData.PhotonLight - 0.01f, "the Bank's Light reached " + F(maxLight));
#if ULOE_SHOTS
                yield return UloeShots.Close("bank", bank.Pos, 5f);
#endif
                // Respawn: one falls, one comes back after Respawn seconds; faster in Light.
                float r0 = bank.Stat("Respawn", 8f) / (1f + UloeData.BankPerLight * bank.Light);
                test.Feed(bank.Master, 10f);
                yield return Wait(0.3f);
                float lit = bank.RespawnNow;
                Check("Photons come back 10% faster per Light", lit < r0 && Near(lit, bank.Stat("Respawn", 8f) / (1f + UloeData.BankPerLight * bank.Light), 0.05f), F(r0) + " s -> " + F(lit) + " s at Light " + F(bank.Light));
                yield return Wait(UloeData.LightHold + 0.5f);
                Kill(ph.Master);
                yield return Wait(0.3f);
                Check("a fallen Photon leaves one", PhotonsOf(bank).Count == 1, PhotonsOf(bank).Count + " alive");
                t0 = Time.time;
                yield return Until(() => PhotonsOf(bank).Count == 2, 12f);
                Check("it comes back after its Respawn, not at once", PhotonsOf(bank).Count == 2 && Time.time - t0 >= 3f && Time.time - t0 <= 9.5f, F(Time.time - t0) + " s");
                ph = PhotonsOf(bank).Find(p => p.Started);
                // A foe next to the Bank: a Photon's hit marks it.
                yield return Foe("Enemy_Dagger", bank.Pos, 2.5f, 1f, slot);
                Entity_Master foe = slot[0];
                if (foe != null)
                {
                    // Fights counts only the walks: a foe that spawns or walks into a Photon's Range is fought where it stands
                    // (round 1), so what shows it was drawn is that the brain took it as its target.
                    int marked = 0;
                    bool drawn = false;
                    yield return Until(() =>
                    {
                        marked = 0;
                        foreach (UloePhotonBrain p in PhotonsOf(bank))
                        {
                            marked += p.Marked;
                            if (p.Target == foe) drawn = true;
                        }
                        return (marked > 0 && drawn) || !ModKitUtil.Alive(foe);
                    }, 15f);
                    if (marked > 0 && ModKitUtil.Alive(foe))
                        Check("a Photon's hit Blinds and Illuminates, level 1", UloeMarks.BlindLevel(foe) >= 1 && UloeMarks.LitLevel(foe) >= 1, "Blind " + UloeMarks.BlindLevel(foe) + ", Illuminate " + UloeMarks.LitLevel(foe));
                    else ModKitTest.Setup("a Photon's hit", "no hit in 15 s (fell " + !ModKitUtil.Alive(foe) + ")");
                    int walks = 0;
                    foreach (UloePhotonBrain p in PhotonsOf(bank)) walks += p.Fights;
                    Check("a Photon was drawn to the foe", drawn, walks + " walks toward a foe");
#if ULOE_SHOTS
                    yield return UloeShots.Close("photon_fight", bank.Pos, 4f);
#endif
                }
                else ModKitTest.Setup("a Photon's hit", "no Dagger could spawn by the Bank");
                ClearFoes();
                yield return Upgrade(bank, "Short2", owner);
                Check("Short Circuit", Has(bank, "Short2"));
                ph = PhotonsOf(bank).Find(p => p.Started);
                if (ph != null)
                {
                    yield return Foe("Enemy_Dagger", ph.Pos, 1.2f, 0f, slot);
                    foe = slot[0];
                    if (foe != null)
                        yield return Wait(0.3f);   // its hit box reaches the physics scene that Entityarea searches (Spool round 1b; round 1 here)
                    if (foe != null && ph != null && ph.Master != null && ModKitUtil.Alive(ph.Master))
                    {
                        float h0 = ModKitUtil.Health(foe);
                        int bursts = UloeServer.Bursts;
                        float apart = ModKitUtil.Flat(ModKitUtil.Pos(foe), ph.Pos);
                        bool inReach = ModKitUtil.FoesNear(bank.Master, ph.Pos, bank.Param("Burstradius_Base", 1.5f)).Contains(foe);
                        Kill(ph.Master);
                        yield return Wait(0.3f);
                        Check("a falling Photon bursts", UloeServer.Bursts == bursts + 1);
                        if (inReach && ModKitUtil.Alive(foe))
                        {
                            Check("... hurting foes around it", h0 - ModKitUtil.Health(foe) >= 3f - 0.01f, F(h0) + " -> " + F(ModKitUtil.Health(foe)) + ", " + F(apart) + " apart");
                            Check("... with Blind 2 and Illuminate 2", UloeMarks.BlindLevel(foe) == 2 && UloeMarks.LitLevel(foe) == 2, "Blind " + UloeMarks.BlindLevel(foe) + ", Illuminate " + UloeMarks.LitLevel(foe));
                        }
                        else ModKitTest.Setup("the burst's numbers", "the foe stood " + F(apart) + " from it, in its reach " + inReach + ", fell " + !ModKitUtil.Alive(foe));
                    }
                    else ModKitTest.Setup("Short Circuit", "no Dagger could spawn by the Photon");
                }
                ClearFoes();
            }
            else ModKitTest.Setup("Photons", "none started");
            // Plug In from a Power Bank.
            yield return Near(hm, bank.Master, 3f);
            yield return Wait(UloeData.PlugCooldown + 0.1f);
            Send(hm, UloeData.PlugLink, Ref(bank.Master));
            yield return Wait(1.3f);
            float bankLight = UloeData.BankBase + UloeData.BankPerUpgrade * bank.UpgradeCount;
            Check("PLUG IN goes into a Power Bank, Light by its upgrades", hero.Plug == bank.Master && Near(hero.LightNow, bankLight), hero.LastPlug + ", Light " + F(hero.LightNow) + ", " + bank.UpgradeCount + " upgrades");
            Send(hm, UloeData.PlugLink, "{\"Unplug\":1}");
            yield return Wait(0.8f);
        }

        // 7. The Mirror.
        at = Spot(hm, UloeContent.Beings[UloeData.Mirror], 3f);
        yield return Build(hm, UloeData.Mirror, at, known, got);
        UloeMirrorBrain mirror = got[0] as UloeMirrorBrain;
        // Beside the first, so the Bounce Card has a neighbour within its 3 tiles (round 3: two spots round the vessel stood 5.8 apart).
        at = mirror != null ? ModKitTest.Spot(mirror.Pos, UloeContent.Beings[UloeData.Mirror], ModKitUtil.Team(hm), 1f, 2.8f, used, "the second Mirror") : new Vector3(float.NaN, 0f, 0f);
        if (float.IsNaN(at.x)) at = Spot(hm, UloeContent.Beings[UloeData.Mirror], 3.5f);
        yield return Build(hm, UloeData.Mirror, at, known, got);
        UloeMirrorBrain mirrorB = got[0] as UloeMirrorBrain;
        if (Check("build two Mirrors", mirror != null && mirrorB != null))
        {
            // The vessel off to one side: the Light here is the test's (a wandering Photon may add a little).
            Genitem offTile = WalkTile(hm, mirror.Pos, 4f, 9f);
            if (offTile != null)
                yield return Walk(hm, offTile.Position, 0.8f, 15f);
            UloeLight lightA = new UloeLight(), lightB = new UloeLight();
            yield return FarFoe("Enemy_Dagger", slot);
            far = slot[0];
            if (far != null)
            {
                yield return Until(() => mirror.Light <= 0f, 8f);   // a wandering Photon lights it now and then
                float lightNow = mirror.Light;
                if (lightNow > 0f)
                    Log("a Photon lights the Mirror at the start: Light " + F(lightNow));
                else
                {
                    Hit(far, mirror.Master, 2f);
                    yield return Wait(0.1f);
                    Check("an unlit Mirror does nothing to what hits it", UloeMarks.BlindLevel(far) == 0 && mirror.Flashes == 0, "Blind " + UloeMarks.BlindLevel(far) + ", flashes " + mirror.Flashes);
                }
                float h0 = ModKitUtil.Health(mirror.Master);
                float[] lights = { 1f, 3f, 6f };
                int[] levels = { 1, 2, 3 };
                for (int i = 0; i < 3; i++)
                {
                    lightA.Feed(mirror.Master, lights[i]);
                    yield return Wait(0.15f);
                    Check("a Mirror at Light " + F(mirror.Light) + " has level " + levels[i], mirror.Level >= levels[i], "level " + mirror.Level);
                    if (ModKitUtil.Alive(far))
                    {
                        Hit(far, mirror.Master, 2f);
                        yield return Wait(0.1f);
                        Check("... and Blinds what hits it at that level", UloeMarks.BlindLevel(far) >= levels[i], "Blind " + UloeMarks.BlindLevel(far));
                    }
                }
                Check("a Mirror takes its hits", ModKitUtil.Health(mirror.Master) < h0, F(h0) + " -> " + F(ModKitUtil.Health(mirror.Master)));
                Kill(far);
                yield return Wait(0.3f);
            }
            else ModKitTest.Setup("the Mirror's Blind", "no foe could stand by the enemy core");
            yield return Upgrade(mirror, "Silver2", owner);
            Check("Silver Backing", Has(mirror, "Silver2"));
            yield return FarFoe("Enemy_Dagger", slot);
            far = slot[0];
            if (far != null)
            {
                lightA.Feed(mirror.Master, 1f);
                yield return Wait(0.2f);
                int level = mirror.Level;
                Hit(far, mirror.Master, 2f);
                yield return Wait(0.1f);
                Check("Silver Backing Blinds one level stronger", UloeMarks.BlindLevel(far) == Mathf.Min(3, level + 1), "level " + level + ", Blind " + UloeMarks.BlindLevel(far));
                Kill(far);
                yield return Wait(0.3f);
            }
            yield return Upgrade(mirrorB, "Vanity2", owner);
            Check("Vanity Lights", Has(mirrorB, "Vanity2"));
            yield return FarFoe("Enemy_Dagger", slot);
            far = slot[0];
            if (far != null)
            {
                lightB.Feed(mirrorB.Master, 3f);
                yield return Wait(0.2f);
                Hit(far, mirrorB.Master, 2f);
                yield return Wait(0.1f);
                Check("Vanity Lights Illuminates in place of Blind", UloeMarks.LitLevel(far) >= 2 && UloeMarks.BlindLevel(far) == 0, "Illuminate " + UloeMarks.LitLevel(far) + ", Blind " + UloeMarks.BlindLevel(far));
                Kill(far);
                yield return Wait(0.3f);
            }
            else ModKitTest.Setup("Vanity Lights", "no foe could stand by the enemy core");
            // Glare, with the dice held.
            yield return Upgrade(mirror, "Glare3", owner);
            Check("Glare", Has(mirror, "Glare3"));
            yield return FarFoe("Enemy_Dagger", slot);
            far = slot[0];
            if (far != null)
            {
                UloeMarks.Blind(hm, far, 2);
                yield return Wait(0.2f);
                float chance = UloeMarks.BlindLevel(far) * 0.2f;
                UloeMirrorBrain.ForceRoll = chance - 0.01f;
                float h1 = ModKitUtil.Health(mirror.Master);
                int nulled = mirror.Nullified;
                Hit(far, mirror.Master, 2f);
                yield return Wait(0.1f);
                Check("Glare: within N x 20% the hit does nothing", mirror.Nullified == nulled + 1 && Near(ModKitUtil.Health(mirror.Master), h1), "chance " + F(chance) + ", " + F(h1) + " -> " + F(ModKitUtil.Health(mirror.Master)));
                UloeMirrorBrain.ForceRoll = chance + 0.01f;
                h1 = ModKitUtil.Health(mirror.Master);
                nulled = mirror.Nullified;
                Hit(far, mirror.Master, 2f);
                yield return Wait(0.1f);
                Check("Glare: beyond it the hit lands", mirror.Nullified == nulled && ModKitUtil.Health(mirror.Master) < h1, F(h1) + " -> " + F(ModKitUtil.Health(mirror.Master)));
                UloeMirrorBrain.ForceRoll = -1f;
            }
            else ModKitTest.Setup("Glare", "no foe could stand by the enemy core");
            // Bounce Card: B relays; A (no Bounce Card) receives.
            yield return Upgrade(mirrorB, "Bounce3", owner);
            Check("Bounce Card", Has(mirrorB, "Bounce3"));
            if (ModKitUtil.Flat(mirror.Pos, mirrorB.Pos) <= 3f)
            {
                yield return Wait(UloeData.LightHold + 0.3f);
                float before = mirror.Light;
                lightB.Feed(mirrorB.Master, 6f);
                yield return Wait(UloeData.LightTick + 0.3f);
                // The relay is one source: its new share replaces the smaller one 'before' may already hold (round 1), so A
                // holds at least the relay, not the relay on top of 'before'.
                Check("Bounce Card passes half its Light on", mirrorB.RelayNow >= 3f - 0.01f && mirror.Light >= mirrorB.RelayNow - 0.51f, "the other Mirror " + F(before) + " -> " + F(mirror.Light) + ", relay " + F(mirrorB.RelayNow));
                // A third Mirror with a Bounce Card beside B: neither feeds the other (they would climb without end).
                Vector3 cat = ModKitTest.Spot(mirrorB.Pos, UloeContent.Beings[UloeData.Mirror], team, 1.5f, 2.9f, used, "a third Mirror");
                yield return Build(hm, UloeData.Mirror, cat, known, got);
                UloeMirrorBrain mirrorC = got[0] as UloeMirrorBrain;
                if (mirrorC != null)
                {
                    yield return Upgrade(mirrorC, "Silver2", owner);
                    yield return Upgrade(mirrorC, "Bounce3", owner);
                    Check("a third Mirror takes a Bounce Card", Has(mirrorC, "Bounce3"));
                    yield return Wait(UloeData.LightHold + 0.3f);
                    float cBefore = mirrorC.Light;
                    lightB.Feed(mirrorB.Master, 6f);
                    yield return Wait(UloeData.LightTick + 0.3f);
                    Check("a Bounce Card doesn't feed another", mirrorB.RelayNow > 0f && mirrorC.Light <= cBefore + 0.01f, "relay " + F(mirrorB.RelayNow) + ", the other Bounce Card " + F(cBefore) + " -> " + F(mirrorC.Light));
                }
                else ModKitTest.Setup("two Bounce Cards", "no third Mirror");
            }
            else ModKitTest.Setup("Bounce Card", "the Mirrors stand " + F(ModKitUtil.Flat(mirror.Pos, mirrorB.Pos)) + " apart");
#if ULOE_SHOTS
            yield return UloeShots.Close("mirrors", mirror.Pos, 5f);
#endif
            ClearFoes();
        }

        // 8. The Flood Light.
        UloeFloodBrain flood = null, flood2 = null;
        if (node != null)
        {
            yield return Near(hm, node, 3f);
            Log("the node: Health " + F(ModKitUtil.Health(node)) + ", Healthresource " + F(ModKitUtil.Stat(node, "Healthresource", 0f)));
            Vector3 fat = ReachSpot(hm, ModKitUtil.Pos(node), UloeContent.Beings[UloeData.Flood], team, 0.5f, 2.3f);
            yellow = Res(owner, "Yellowaura");
            if (float.IsNaN(fat.x))
                ModKitTest.Setup("the Flood Light", "no tile the vessel can get to beside the node");
            else
            {
                yield return Build(hm, UloeData.Flood, fat, known, got);
                flood = got[0] as UloeFloodBrain;
                Check("build a Flood Light by the node", flood != null, "at " + fat);
            }
            if (flood != null)
            {
                // Uloe stands by the node, and mining it may pay a strike or two in the same moment.
                float spentYellow = yellow - Res(owner, "Yellowaura");
                Check("a Flood Light costs Yellow 40", spentYellow > 25f && spentYellow <= 40.01f, "spent " + F(spentYellow));
                yield return Wait(UloeData.LightTick + 0.5f);
                Check("it plugs into the node", flood.Node == node || UloeUtil.IsNode(flood.Node), flood.Node != null ? ModKitUtil.BeingName(flood.Node) : "none");
                Check("its Light comes from Light out and the node's aura", flood.Node != null && Near(flood.Output, 2f + Mathf.Min(UloeData.FloodNodeMax, UloeData.FloodNodePerAura * UloeUtil.NodeAura(flood.Node))), "Output " + F(flood.Output));
                int strikes = flood.Strikes;
                yield return Wait(3.2f);
                Check("it mines the node", flood.Strikes > strikes, (flood.Strikes - strikes) + " strikes in 3.2 s");
                yield return Until(() => flood.Aimed != null, 2f);
                Check("with nothing picked it aims at the nearest building", flood.Aimed != null, flood.Aimed != null ? ModKitUtil.BeingName(flood.Aimed) + " " + F(ModKitUtil.Flat(ModKitUtil.Pos(flood.Aimed), flood.Pos)) + " away" : "none in reach");
                if (flood.Aimed != null)
                {
                    yield return Wait(UloeData.LightTick + 0.1f);
                    float aimedLight = ModKitUtil.Stat(flood.Aimed, "Light", 0f);
                    Check("... and lights it", aimedLight >= flood.Output - 0.01f, "Light " + F(aimedLight) + ", Output " + F(flood.Output));
                }
                // AIM: a pick, and two refusals.
                UloeBuilding other = null;
                foreach (UloeBuilding b in UloeServer.Buildings)
                    if (b != flood && b.Started && b.Master != flood.Aimed && ModKitUtil.Flat(b.Pos, flood.Pos) <= flood.AimReach) other = b;
                UloeBuilding tooFar = null;
                foreach (UloeBuilding b in UloeServer.Buildings)
                    if (b != flood && b.Started && ModKitUtil.Flat(b.Pos, flood.Pos) > flood.AimReach + 0.6f) tooFar = b;
                if (other != null)
                {
                    Send(flood.Master, UloeData.AimLink, Ref(other.Master));
                    yield return Wait(UloeData.LightTick + 0.4f);
                    Check("AIM picks one of your buildings", flood.Picked == other.Master && flood.Aimed == other.Master && other.Light >= flood.Output - 0.01f, flood.LastAim + ", Light " + F(other.Light));
                }
                else ModKitTest.Setup("AIM's pick", "a single building in reach");
                // A building past its reach, for the refusal: this base is small enough to hold none.
                if (tooFar == null)
                {
                    Vector3 farAt = ReachSpot(hm, flood.Pos, UloeContent.Beings[UloeData.Mirror], team, flood.AimReach + 1.5f, flood.AimReach + 8f);
                    if (!float.IsNaN(farAt.x))
                    {
                        yield return Build(hm, UloeData.Mirror, farAt, known, got);
                        tooFar = got[0];
                    }
                }
                int aimRefused = flood.Refused;
                Send(flood.Master, UloeData.AimLink, Ref(hm));
                yield return Wait(0.2f);
                Check("AIM refuses what isn't a building", flood.Refused == aimRefused + 1, flood.LastAim);
                if (tooFar != null)
                {
                    Send(flood.Master, UloeData.AimLink, Ref(tooFar.Master));
                    yield return Wait(0.2f);
                    Check("AIM refuses what is out of reach", flood.Refused == aimRefused + 2 && flood.LastAim == "too far", flood.LastAim);
                }
                else ModKitTest.Setup("AIM's reach", "no building beyond it");
                // A Photon near the line follows it.
                if (bank != null && PhotonsOf(bank).Count > 0 && ModKitUtil.Flat(bank.Pos, flood.Pos) <= flood.AimReach)
                {
                    Send(flood.Master, UloeData.AimLink, Ref(bank.Master));
                    yield return Wait(0.3f);
                    yield return Until(() => PhotonsOf(bank).Exists(p => p.Following == flood), 20f);
                    Check("a Photon near a Flood Light's line follows it", PhotonsOf(bank).Exists(p => p.Following == flood), PhotonsOf(bank).Count + " Photons");
                }
                else ModKitTest.Setup("a Photon following a line", "the Bank is out of the Flood Light's reach");
#if ULOE_SHOTS
                yield return UloeShots.Close("flood", flood.Pos, 5f);
#endif
                // Long Throw on a second one, Wide Flood on the first.
                Vector3 fat2 = ReachSpot(hm, ModKitUtil.Pos(node), UloeContent.Beings[UloeData.Flood], team, 0.5f, 2.3f);
                // That node's ring can be full on this map: any other node will do, with a building near it to aim at.
                if (float.IsNaN(fat2.x))
                    foreach (Entity_Master otherNode in new List<Entity_Master>(Game_Entity.Instance.Entityclient))
                        if (otherNode != node && float.IsNaN(fat2.x) && UloeUtil.IsNode(otherNode))
                            fat2 = ReachSpot(hm, ModKitUtil.Pos(otherNode), UloeContent.Beings[UloeData.Flood], team, 0.5f, 2.3f);
                if (!float.IsNaN(fat2.x))
                {
                    yield return Build(hm, UloeData.Flood, fat2, known, got);
                    flood2 = got[0] as UloeFloodBrain;
                    if (flood2 != null && UloeServer.NearestBuilding(flood2.Team, flood2.Pos, 6f, flood2.Master) == null)
                    {
                        Vector3 near2 = ReachSpot(hm, flood2.Pos, UloeContent.Beings[UloeData.Glass], team, 1.5f, 3.5f);
                        if (!float.IsNaN(near2.x)) yield return Build(hm, UloeData.Glass, near2, known, got);
                    }
                }
                yield return Upgrade(flood, "Wide2", owner);
                Check("Wide Flood", Has(flood, "Wide2") && flood.Wide);
                Check("Wide Flood can no longer be aimed: the button goes", !flood.Master.Commandlistserver.ContainsKey(UloeData.AimLink));
                yield return Wait(UloeData.LightTick + 0.4f);
                Check("... and it no longer directs its light", flood.Aimed == null && !flood.Directed);
                float wide = flood.Output * flood.Param("Wide_Base", 0.6f), radius = flood.Param("Wideradius_Base", 3.5f);
                List<string> dim = new List<string>();
                foreach (UloeBuilding b in UloeServer.Buildings)
                    if (b != flood && b.Started && ModKitUtil.Flat(b.Pos, flood.Pos) <= radius && ModKitUtil.Stat(b.Master, "Light", 0f) < wide - 0.01f)
                        dim.Add(b.Def.Id + " " + F(ModKitUtil.Stat(b.Master, "Light", 0f)));
                Check("Wide Flood lights all around it", dim.Count == 0, "share " + F(wide) + " within " + F(radius) + (dim.Count > 0 ? ", dim: " + string.Join(", ", dim.ToArray()) : ""));
                if (flood2 != null)
                {
                    yield return Upgrade(flood2, "Long2", owner);
                    Check("Long Throw", Has(flood2, "Long2") && flood2.Long);
                    yield return Wait(UloeData.LightTick * 2f + 0.4f);
                    if (flood2.Aimed != null)
                    {
                        float line = flood2.Output * flood2.Param("Line_Base", 0.8f);
                        List<string> off = new List<string>();
                        int onLine = 0;
                        foreach (UloeBuilding b in UloeServer.Buildings)
                            if (b != flood2 && b.Started && UloeUtil.SegmentDistance(b.Pos, flood2.Pos, flood2.LineEnd) <= flood2.Param("Linewidth_Base", 0.6f))
                            {
                                onLine++;
                                if (ModKitUtil.Stat(b.Master, "Light", 0f) < line - 0.01f) off.Add(b.Def.Id);
                            }
                        Check("Long Throw lights every building on its line", onLine > 0 && off.Count == 0, onLine + " on the line, share " + F(line) + (off.Count > 0 ? ", dim: " + string.Join(", ", off.ToArray()) : ""));
                        Check("Long Throw's line runs 8 tiles", Near(ModKitUtil.Flat(flood2.LineEnd, flood2.Pos), flood2.Param("Linereach_Base", 8f), 0.1f), F(ModKitUtil.Flat(flood2.LineEnd, flood2.Pos)));
                    }
                    else ModKitTest.Setup("Long Throw", "nothing in its reach, node " + UloeUtil.IsNode(flood2.Node));
                }
                else ModKitTest.Setup("Long Throw", "no second Flood Light");
#if ULOE_SHOTS
                yield return UloeShots.Close("floods", flood.Pos, 5f);
#endif
            }
            // No node in reach: no Light.
            Vector3 noNode = ReachSpot(hm, glass.Pos, UloeContent.Beings[UloeData.Flood], team, 3f, 14f, p => UloeUtil.NearestNode(p, 3f) == null);
            if (!float.IsNaN(noNode.x))
            {
                yield return Build(hm, UloeData.Flood, noNode, known, got);
                UloeFloodBrain lonely = got[0] as UloeFloodBrain;
                yield return Wait(UloeData.LightTick + 0.5f);
                Check("with no node in reach a Flood Light gives no Light", lonely != null && lonely.Node == null && lonely.Aimed == null && lonely.Strikes == 0, lonely != null ? "node " + (lonely.Node != null) : "not built");
            }
            else ModKitTest.Setup("a Flood Light with no node", "no tile away from the nodes");
        }
        else ModKitTest.Setup("the Flood Light", "no node");

        // 9. Directions.
        {
            List<string> names = new List<string>();
            foreach (Upgradeproperties d in ModKitDirections.All())
                if (d.Objectname.StartsWith(ModKitDirections.Prefix + UloeData.Part + "_")) names.Add(d.Objectname);
            Check("twelve Direction upgrades, in the kit 1.7 form", names.Count == 12 && names.Contains("Direction_Uloe_Extension1_Upgradeobject") && names.Contains("Direction_Uloe_Flashbulb3_Upgradeobject"), names.Count + ": " + string.Join(", ", names.ToArray()));
            Check("with none held, the numbers are the plain ones", player.Extension == 0 && player.Bright == 0 && player.Batteries == 0 && player.Flash == 0 && Near(hero.Reach, UloeData.PlugRange));
            for (int r = 1; r <= 3; r++)
            {
                foreach (UloeData.Dir dir in UloeData.Directions)
                    Check("grant " + dir.Name + " rank " + r, ModKitDirections.Grant(hm, Bl_Address.Getload("Upgrade", ModKitDirections.Id(UloeData.Part, dir.Name + r)) as Upgradeproperties));
                yield return Wait(0.8f);
                Check("rank " + r + " reads back on the vessel", ModKitDirections.Rank(hm, UloeData.Part, UloeData.Extension) == r && player.Extension == r && player.Bright == r && player.Batteries == r && player.Flash == r,
                    player.Extension + " " + player.Bright + " " + player.Batteries + " " + player.Flash);
                Check("Extension Cord rank " + r, Near(hero.Reach, UloeData.PlugRange + UloeData.ExtensionReach[r]), "reach " + F(hero.Reach));
                yield return Wait(UloeData.LightTick + 0.1f);
                Check("Brighter Bulb rank " + r, Near(hero.LightNow, UloeData.HeroLight + UloeData.BrightLight[r]), "Light " + F(hero.LightNow));
                if (bank != null)
                    Check("Spare Batteries rank " + r, bank.Slots == 2 + UloeData.BatteryUnits[r] && Near(bank.Stat("Respawn", 8f) * UloeData.BatteryRespawn[r] / (1f + UloeData.BankPerLight * bank.Light), bank.RespawnNow), bank.Slots + " Photons, respawn " + F(bank.RespawnNow));
                yield return FarFoe("Enemy_Dagger", slot);
                far = slot[0];
                if (far != null)
                {
                    // A spark landing, as Projectile_Master.Interact delivers it.
                    Helper_Entitytools.Interacthurt(hm, far, new Dictionary<string, float>());
                    yield return Wait(0.2f);
                    if (!ModKitUtil.Alive(far))
                        ModKitTest.Setup("Flash Bulb rank " + r, "the foe fell to the spark");   // dropped out without a word (MAX ULO-9)
                    else
                        Check("Flash Bulb rank " + r, UloeMarks.LitLevel(far) >= UloeData.FlashLit[r] && UloeMarks.BlindLevel(far) >= UloeData.FlashBlind[r], "Illuminate " + UloeMarks.LitLevel(far) + ", Blind " + UloeMarks.BlindLevel(far));
                    Kill(far);
                    yield return Wait(0.2f);
                }
            }
        }
        ClearFoes();

        // 10. Selling and the journal.
        {
            float red0 = Res(owner, "Redaura");
            Send(hm, "Sell", Sell(hm, glass.Master));
            yield return Until(() => glass == null || glass.Master == null, 12f);
            Check("selling a Glass refunds aura", (glass == null || glass.Master == null) && Res(owner, "Redaura") > red0, F(red0) + " -> " + F(Res(owner, "Redaura")));
            if (bank != null && bank.Master != null)
            {
                Send(hm, "Sell", Sell(hm, bank.Master));
                yield return Until(() => bank == null || bank.Master == null, 12f);
                yield return Wait(0.5f);
                Check("a Power Bank's Photons go with it", Beings(UloeData.Photon) == 0, Beings(UloeData.Photon) + " left");
            }
        }
        {
            Logobject page = UloeContent.Entry.Log;
            bool own = page != null && page.name == "Log_" + UloeData.Hero, being = false;
            Logobject bankPage = null, upgradePage = null, unitPage = null;
            if (own)
                foreach (Logitems.Base b in AllItems(page.Elements))
                {
                    if (b is Logitems.TextImageBeing && ((Logitems.TextImageBeing)b).Entity == UloeContent.HeroBeing.Entity) being = true;
                    Logitems.Link l = b as Logitems.Link;
                    if (l != null && !l.Sidelink && l.Target != null && l.Target.name == "Log_" + UloeData.Hero + "_" + UloeData.Bank) bankPage = l.Target;
                }
            Check("journal: Uloe has their own vessel page", own && being);
            Check("journal: Uloe's page links the Power Bank's page", bankPage != null);
            if (bankPage != null)
                foreach (Logitems.Base b in AllItems(bankPage.Elements))
                {
                    if (b is Logitems.LinkUpgrade && ((Logitems.LinkUpgrade)b).Target != null && upgradePage == null) upgradePage = ((Logitems.LinkUpgrade)b).Target;
                    if (b is Logitems.LinkImage && ((Logitems.LinkImage)b).Target != null && unitPage == null) unitPage = ((Logitems.LinkImage)b).Target;
                }
            Check("journal: the Bank page links its upgrade", upgradePage != null && upgradePage.name.Contains("Uloebank"), upgradePage != null ? upgradePage.name : "none");
            Check("journal: the Bank page links the Photon", unitPage != null && unitPage.name.StartsWith("Log_Mob_Uloe"), unitPage != null ? unitPage.name : "none");
            List<Logobject> vesselPages = new List<Logobject>();
            foreach (UnityEngine.Object o in ModKitUtil.Folder("Hero").Values)
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
#if ULOE_SHOTS
            Bl_Menus.Changemenu("Journal");
            List<Logobject> show = new List<Logobject> { page, bankPage, upgradePage, unitPage };
            foreach (Logobject shown in show)
            {
                if (shown == null) continue;
                Loghandler.Log_Current = shown;
                yield return Wait(2.5f);
                yield return UloeShots.Shot("journal_" + shown.name);
            }
            Bl_Menus.Changemenu("Main");
            yield return Wait(1f);
#endif
        }

        Log("counts: beams seen " + UloeClient.Beams + ", links drawn " + UloeClient.Linksdrawn + ", blinds " + UloeMarks.Blinds + ", illuminations " + UloeMarks.Illuminations + ", shines " + UloeServer.Shines + ", bursts " + UloeServer.Bursts);
        freeze = false;
        Log("DONE pass=" + pass + " fail=" + fail + " setup=" + ModKitTest.Setups);
        phase = 9;
    }
}
#endif
