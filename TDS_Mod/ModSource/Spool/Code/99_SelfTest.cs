using Blockage.Menu;
#if SPOOL_SELFTEST
// Debug build only (Build-SpoolMod.ps1 -Define SPOOL_SELFTEST[,SPOOL_SHOTS]). Starts a solo match by itself (Embark,
// pick Spool, pick a map, ready up) because the game ignores injected input, then plays through every Spool mechanic
// on the host the way a player would (real Build, Sell, PATCH, TARGET, SPOT, RETURN, MEND and Upgrade commands through
// Command_ServerRpc), spawns and fells foes, and logs one "[Spool] TEST PASS|FAIL name: detail" line per check plus a
// final "TEST DONE" summary in Player.log. Sections follow DESIGN.md's test plan.
using UnityEngine.SceneManagement;
using Unity.Netcode;

public static partial class SpoolSelfTest
{
    static int phase;
    static float seen = -1f, step = -1f;
    static string node = "";
    static bool freeze;
    static int pass, fail;

    static void Log(string m) { SpoolLog.Info("TEST " + m); }

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
#if SPOOL_SHOTS
            if (!boatShot && now - seen >= 4f && Macro_Worldvisual.Instance != null)
            {
                boatShot = true;
                SpoolMod.Instance.StartCoroutine(BoatShot());
            }
            if (now - seen < 16f)
                return;
#else
            if (now - seen < 8f)
                return;
#endif
            if (step < 0f)
            {
                macro.Selectcharacter(SpoolData.Hero);
                node = PickNode();
                Log("picked Spool and map node '" + node + "'");
                if (node != "")
                    Tds_Multiplayer_Remote.Instance.Selectmap_ServerRpc(node, false, default(ServerRpcParams));
                step = now;
                return;
            }
            if (now - step > 3f && now - step < 4f)
            {
                Tds_Clientinfo info = Blm_Server.Instance.Localplayer.GetComponent<Tds_Clientinfo>();
                Check("vessel select", info != null && info.Hero.Value.ToString() == SpoolData.Hero, info != null ? info.Hero.Value.ToString() : "no clientinfo");
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

        if (phase <= 2 && SpoolServer.Heroes.Count > 0 && Game_Mapvars.Instance != null && ModKitUtil.IsServer)
        {
            phase = 3;
#if SPOOL_REVIEW
            SpoolMod.Instance.StartCoroutine(Review());
#else
            SpoolMod.Instance.StartCoroutine(Scenario());
#endif
        }
    }

#if SPOOL_SHOTS
    static bool boatShot;

    // The boat's vessel select, opened as the Setup screen's button does, captured with Spool standing on it.
    static IEnumerator BoatShot()
    {
        Macro_Worldvisual.Instance.Opencharselect();
        yield return new WaitForSecondsRealtime(3f);
        Macro_Characterselect_Click mine = null;
        int buttons = 0;
        foreach (Macro_Characterselect_Click b in UnityEngine.Object.FindObjectsOfType<Macro_Characterselect_Click>())
        {
            buttons++;
            if (b.Character == SpoolData.Hero) mine = b;
        }
        Dictionary<string, UnityEngine.Object> heroes = ModKitUtil.Folder("Hero");
        Check("Spool unlocked on the boat's vessel select", mine != null && !mine.Locked, mine == null ? "no button" : "locked " + mine.Locked);
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
        yield return SpoolShots.Shot("boat");
        if (mine != null)
        {
            Macro_Main.Instance.Selectcharacter(SpoolData.Hero);
            yield return new WaitForSecondsRealtime(1.5f);
            yield return SpoolShots.Shot("boat_selected");
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

    static T Newest<T>(string being, List<SpoolBuilding> known) where T : SpoolBuilding
    {
        foreach (SpoolBuilding b in SpoolServer.Buildings)
            if (b != null && b.Started && b.Def.Id == being && !known.Contains(b))
                return b as T;
        return null;
    }

    static IEnumerator Build(Entity_Master hero, string tower, Vector3 at, List<SpoolBuilding> known, SpoolBuilding[] result)
    {
        result[0] = null;
        string json = "{\"Tower\":\"" + tower + "\",\"Position\":" + Vec(at) + ",\"Orientation\":0,\"Owner\":" + ModKitUtil.Owner(hero) + "}";
        Send(hero, "Build", json);
        float t = 0f;
        while (t < 20f && result[0] == null)
        {
            result[0] = Newest<SpoolBuilding>(tower, known);
            t += Time.deltaTime;
            yield return null;
        }
        if (result[0] != null)
            known.Add(result[0]);
    }

    static int Team() { return SpoolServer.Heroes.Count > 0 ? ModKitUtil.Team(SpoolServer.Heroes[0].Master) : 1; }

    // Removes Tide foes so they don't pile up (not a kill: no Thread).
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

    // A loaded foe beside the enemy core, far from Spool's army, kept while it stands: the Cushion checks hit with it from
    // afar (round 1: the vessel and the Crafted felled the Daggers beside the Cushions, and a hit from a fallen foe lands
    // nowhere).
    static IEnumerator FarFoe(string being, Entity_Master[] slot)
    {
        if (slot[0] != null && Loaded(slot[0]) && ModKitUtil.BeingName(slot[0]) == being)
            yield break;
        slot[0] = null;
        Entity_Master core = ModKitTest.Core();
        if (core != null)
            yield return Foe(being, ModKitUtil.Pos(core), 6f, 1f, slot);
    }

    // Thread still lying about from earlier checks would join what a check counts.
    static void ClearDrops() { SpoolServer.Drops.Clear(); }

    // The Stash's own traffic while a check runs (round 1: the Tins kept stitching from it): Thread handed over by
    // carriers and sent by Direct Line, less what the Tins spent. A check subtracts the change of this from the Stash's.
    static int Traffic(long owner)
    {
        int n = SpoolServer.Delivered;
        foreach (SpoolTinBrain t in SpoolServer.Owned<SpoolTinBrain>(owner))
            n -= t.Spent;
        foreach (SpoolLoomBrain l in SpoolServer.Owned<SpoolLoomBrain>(owner))
            n += l.Sent;
        return n;
    }

    static IEnumerator Upgrade(SpoolBuilding b, string key, long owner)
    {
        string id = b.Def.Id + "_" + key + "_Upgradeobject";
        Send(b.Master, "Upgrade", "{\"Upgrade\":\"" + id + "\",\"Upgrader\":" + owner + "}");
        yield return Wait(0.6f);
    }

    static bool Has(SpoolBuilding b, string key) { return b != null && b.Master != null && b.Master.Upgradelistserver.ContainsKey(b.Def.Id + "_" + key + "_Upgradeobject"); }

    static List<Logitems.Base> AllItems(List<Logitems.Base> items) { return ModKitVesselJournal.Flat(items); }

    // ---------------------------------------------------------------- Spool helpers

    static IEnumerator Walk(Entity_Master m, Vector3 to, float near, float timeout)
    {
        ModKitUtil.MoveTo(m, null, to, 0f);
        yield return Until(() => m == null || ModKitUtil.Flat(ModKitUtil.Pos(m), to) <= near, timeout);
    }

    static void Hit(Entity_Master from, Entity_Master to, float amount) { ModKitCombat.Hurt(from, to, amount, 1f, null, null); }

    static List<SpoolUnitBrain> UnitsOf(SpoolBuilding home)
    {
        List<SpoolUnitBrain> l = new List<SpoolUnitBrain>();
        foreach (SpoolUnitBrain u in SpoolServer.Units)
            if (u != null && u.Master != null && u.Home == home) l.Add(u);
        return l;
    }

    static SpoolHeadBrain NewHead(List<SpoolBuilding> known)
    {
        foreach (SpoolBuilding b in SpoolServer.Buildings)
            if (b is SpoolHeadBrain && b.Started && !known.Contains(b)) return (SpoolHeadBrain)b;
        return null;
    }

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

    static string At(Vector3 p) { return "{\"Position\":" + Vec(p) + "}"; }

    // ---------------------------------------------------------------- the scenario

    // One Tide on: the frozen clock let go until the Wave rises, its foes cleared, frozen again.
    static IEnumerator NextTide()
    {
        int wave = Game_Mapvars.Instance.Wave.Value;
        freeze = false;
        float t = 0f;
        while (Game_Mapvars.Instance.Wave.Value == wave && t < 60f)
        {
            Game_Mapvars.Instance.Time_Wave.Value = -1f;
            ClearFoes();
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        freeze = true;
        yield return Wait(0.6f);
        ClearFoes();
    }
    static IEnumerator Scenario()
    {
        Log("match started: scenario begins");
        SpoolHeroBrain hero = SpoolServer.Heroes[0];
        yield return Until(() => Game_Mapvars.Instance != null && Game_Mapvars.Instance.Wave.Value >= 1, 150f);
        freeze = true;
        yield return Wait(2f);
        ClearFoes();
        Entity_Master hm = hero.Master;
        long owner = ModKitUtil.Owner(hm);
        int team = ModKitUtil.Team(hm);
        SpoolPlayer player = SpoolServer.Player(owner);
        Vector3 home = ModKitUtil.Pos(hm);
        Log("tide " + Game_Mapvars.Instance.Wave.Value + ", vessel at " + home + ", owner " + owner + ", team " + team);
        ModKitTest.Log = Log;
        ModKitTest.MapDump(team, hm);

        // 1. The vessel.
        Check("vessel uses the stock melee AI", hm.GetComponent<Entity_Ai_Mob_Combat>() != null && hm.GetComponent<Entity_Ai_Mob_Ranged>() == null && hm.GetComponent<Entity_Ai_Hero_Buildsell>() != null);
        Check("vessel has the Hand Stitched tooltip", hm.Upgradelistserver.ContainsKey(SpoolData.HeroTooltip));
        Check("vessel stats", Mathf.Approximately(ModKitUtil.Stat(hm, "Range", 0), 1.5f) && Mathf.Approximately(ModKitUtil.Stat(hm, "Health", 0), 45f) && Mathf.Approximately(ModKitUtil.Stat(hm, "Speed", 0), 2.75f),
            "Range " + F(ModKitUtil.Stat(hm, "Range", 0)) + ", Health " + F(ModKitUtil.Stat(hm, "Health", 0)) + ", Speed " + F(ModKitUtil.Stat(hm, "Speed", 0)) + ", Damage " + F(ModKitUtil.Stat(hm, "Damage", 0)));
        Link_Command patch;
        Check("PATCH command", hm.Commandlistserver.TryGetValue(SpoolData.PatchLink, out patch) && patch.Controllable && SpoolContent.Patch.Commandfuncts[0] is SpoolPatchSelect);
        // Every key a Spool button names must be one of the game's input actions: selecting a thing looks each up in
        // Bl_Inputs.Actions (Game_Selection.Addcommand) and throws on a miss, and its panel never draws (play test
        // 2026-10-26: "Quickaction2" on RETURN; pitfall #113).
        {
            List<string> bad = new List<string>();
            foreach (Commandobject c in new[] { SpoolContent.Patch, SpoolContent.Target, SpoolContent.Spot, SpoolContent.Return, SpoolContent.Mend })
                if (c != null && c.Keybind != null)
                    foreach (string k in c.Keybind)
                        if (!Blockage.Bl_Inputs.Actions.ContainsKey(k)) bad.Add(c.name + " " + k);
            Check("every Spool button's keys are the game's input actions", bad.Count == 0, bad.Count == 0 ? Blockage.Bl_Inputs.Actions.Count + " actions" : string.Join(", ", bad.ToArray()));
        }
        Check("vessel shows Carried and Stash", hm.Statsserver.ContainsKey("Carried") && hm.Statsserver.ContainsKey("Stash"));
        Check("vessel visible to clients", hm.Client != null && hm.Client.Beingobject != null && hm.Client.Beingobject.Objectname == SpoolData.Hero);

        // 2. The Yarn Stash first.
        Check("build menu holds only the Yarn Stash", hero.Menu() == SpoolData.Stash, hero.Menu());
        Helper_Resources.Giveresources(ModKitUtil.Costs("Redaura=20000;Blueaura=20000;Yellowaura=20000"), owner, Helper_Resources.Styles.Default);
        yield return Wait(0.5f);
        List<SpoolBuilding> known = new List<SpoolBuilding>(SpoolServer.Buildings);
        SpoolBuilding[] got = new SpoolBuilding[1];
        Vector3 at = Spot(hm, SpoolContent.Beings[SpoolData.Stash], 2.2f);
        yield return Build(hm, SpoolData.Stash, at, known, got);
        SpoolStashBrain stash = got[0] as SpoolStashBrain;
        if (!Check("build the Yarn Stash", stash != null, "at " + at))
        {
            Log("DONE pass=" + pass + " fail=" + fail + " setup=" + ModKitTest.Setups + " (no Stash: nothing else can be tested)");
            phase = 9;
            yield break;
        }
        yield return Wait(0.8f);
        Vector3 sp = stash.Pos;
        Check("the first Stash starts with " + SpoolData.StashStart + " Thread", player.Stash == SpoolData.StashStart && Mathf.Approximately(ModKitUtil.Stat(stash.Master, "Thread", -1f), SpoolData.StashStart),
            "Stash " + player.Stash + ", stat " + F(ModKitUtil.Stat(stash.Master, "Thread", -1f)));
        Check("Stash cap " + SpoolData.StashCap, stash.Cap == (int)SpoolData.StashCap, "cap " + stash.Cap);
        Check("with the Stash standing, the menu holds the four", hero.Menu() == ModKitUtil.Join(SpoolData.Buildables), hero.Menu());
        Vector3 second = Spot(hm, SpoolContent.Beings[SpoolData.Stash], 4f);
        Send(hm, "Build", "{\"Tower\":\"" + SpoolData.Stash + "\",\"Position\":" + Vec(second) + ",\"Orientation\":0,\"Owner\":" + owner + "}");
        yield return Wait(2f);
        Check("a second Stash can't be built", SpoolServer.Owned<SpoolStashBrain>(owner).Count == 1, SpoolServer.Owned<SpoolStashBrain>(owner).Count + " Stashes");

        // 3. Thread on the ground.
        {
            Entity_Master[] slot = new Entity_Master[1];
            yield return Foe("Enemy_Dagger", home, 4f, 2f, slot);
            Entity_Master small = slot[0];
            if (small != null)
            {
                Vector3 fp = ModKitUtil.Pos(small);
                float bh = ModKitUtil.BaseHealth(small);
                int expect = Mathf.Min(SpoolData.DropMax, (bh >= SpoolData.LargeHealth ? SpoolData.DropLarge : SpoolData.DropSmall) + (ModKitUtil.Elite(small) ? SpoolData.DropElite : 0));
                int before = SpoolServer.DropsNear(fp, 0.6f), dropped = SpoolServer.Dropped;
                Kill(small);
                yield return Wait(0.3f);
                Check("a felled Dagger leaves " + expect + " Thread where it fell", SpoolServer.Dropped - dropped == expect && SpoolServer.DropsNear(fp, 0.6f) == before + expect,
                    (SpoolServer.Dropped - dropped) + " dropped, " + (SpoolServer.DropsNear(fp, 0.6f) - before) + " where it fell, base health " + F(bh));
                SpoolDrop d = SpoolServer.Drops.Find(x => ModKitUtil.Flat(x.Pos, fp) <= 0.6f);
                Check("a drop lasts " + SpoolData.DropLife + " s", d != null && Mathf.Abs(d.Until - Time.time - SpoolData.DropLife) < 1f, d != null ? F(d.Until - Time.time) : "none");
                int carried = player.Carried, delivered = SpoolServer.Delivered;
                yield return Walk(hm, fp, 0.6f, 12f);
                yield return Wait(0.6f);
                // Within reach of the Stash it hands what it picked up over in the same tick (round 1b).
                int took = player.Carried - carried + SpoolServer.Delivered - delivered;
                Check("Spool picks Thread up within a tile", took >= expect && SpoolServer.DropsNear(fp, 0.6f) == 0, "took " + took + ", carried " + carried + "->" + player.Carried);
                Check("Carried shows on the vessel", Mathf.Approximately(ModKitUtil.Stat(hm, "Carried", -1f), player.Carried), F(ModKitUtil.Stat(hm, "Carried", -1f)));
                int stash0 = player.Stash;
                int c0 = player.Carried;
                yield return Walk(hm, sp, SpoolData.DropOff - 0.2f, 12f);
                yield return Wait(0.6f);
                Check("Spool hands its Thread over at the Stash", player.Stash == Mathf.Min(stash.Cap, stash0 + c0) && player.Carried == Mathf.Max(0, stash0 + c0 - stash.Cap), stash0 + "+" + c0 + "->" + player.Stash + ", carried " + player.Carried);
            }
            else ModKitTest.Setup("Thread drop", "no Dagger could spawn near the vessel");
            yield return Foe("Enemy_Aegis", home, 6f, 3f, slot);
            Entity_Master big = slot[0];
            if (big != null)
            {
                float bh = ModKitUtil.BaseHealth(big);
                bool elite = ModKitUtil.Elite(big);
                int expect = Mathf.Min(SpoolData.DropMax, (bh >= SpoolData.LargeHealth ? SpoolData.DropLarge : SpoolData.DropSmall) + (elite ? SpoolData.DropElite : 0));
                int dropped = SpoolServer.Dropped;
                Kill(big);
                yield return Wait(0.3f);
                Check("a felled Aegis leaves " + expect + " Thread", SpoolServer.Dropped - dropped == expect, (SpoolServer.Dropped - dropped) + " dropped, base health " + F(bh) + ", elite " + elite);
            }
            else ModKitTest.Setup("Aegis drop", "no Aegis could spawn near the vessel");
            // The cap: what doesn't fit stays carried.
            player.Stash = stash.Cap - 1;
            player.Carried = 3;
            SpoolServer.Show(player);
            yield return Walk(hm, sp, SpoolData.DropOff - 0.2f, 8f);
            yield return Wait(0.6f);
            Check("the Stash takes Thread up to its cap; the rest stays carried", player.Stash == stash.Cap && player.Carried == 2, "Stash " + player.Stash + ", carried " + player.Carried);
            player.Stash = 30;
            player.Carried = 0;
            SpoolServer.Show(player);
            // Expiry (hurried: the drop's clock is set to half a second).
            Vector3 xp = home + new Vector3(6f, 0f, 0f);
            SpoolServer.Drop(xp, 2);
            SpoolDrop x = SpoolServer.Drops.Find(o => ModKitUtil.Flat(o.Pos, xp) <= 0.1f);
            if (x != null) x.Until = Time.time + 0.5f;
            yield return Wait(1.2f);
            Check("a drop is gone when its time runs out", SpoolServer.DropsNear(xp, 0.3f) == 0);
            ClearFoes();
        }

        // 4. Cookie Tins: A takes Reuse Reduce Recycle, New Design, One Last Surprise; B Woolen Lining, Discounted Material.
        SpoolTinBrain tinA = null, tinB = null;
        {
            float red = Res(owner, "Redaura");
            at = Spot(hm, SpoolContent.Beings[SpoolData.Tin], 3f);
            int s0 = player.Stash;
            yield return Build(hm, SpoolData.Tin, at, known, got);
            tinA = got[0] as SpoolTinBrain;
            if (Check("build a Cookie Tin", tinA != null, "at " + at))
            {
                Check("Cookie Tin costs Red 50", Mathf.Abs(red - Res(owner, "Redaura") - 50f) < 0.01f, "spent " + F(red - Res(owner, "Redaura")));
                yield return Until(() => UnitsOf(tinA).Count > 0, 4f);
                List<SpoolUnitBrain> cast = UnitsOf(tinA);
                Check("the Tin stitches a Stitched for 10 Thread", cast.Count == 1 && cast[0].Def.Id == SpoolData.Stitched && player.Stash == s0 - 10, cast.Count + " units, Stash " + s0 + "->" + player.Stash);
                if (cast.Count > 0)
                {
                    yield return Until(() => cast[0] == null || cast[0].Started, 2f);
                    Check("a Stitched fights with the game's melee AI", cast[0] != null && cast[0].Master.GetComponent<Entity_Ai_Mob_Combat>() != null);
                    Check("a Stitched is visible to clients", cast[0] != null && cast[0].Master.Client != null && cast[0].Master.Client.Beingobject != null);
                    Check("Stitched stats", cast[0] != null && Mathf.Approximately(ModKitUtil.MaxHealth(cast[0].Master), 20f) && Mathf.Approximately(ModKitUtil.Stat(cast[0].Master, "Speed", 0), 1.8f),
                        cast[0] != null ? "health " + F(ModKitUtil.MaxHealth(cast[0].Master)) + ", speed " + F(ModKitUtil.Stat(cast[0].Master, "Speed", 0)) : "");
#if SPOOL_SHOTS
                    if (cast[0] != null) yield return SpoolShots.Close("stitched", ModKitUtil.Pos(cast[0].Master), 3f);
#endif
                }
                player.Stash = 50;
                SpoolServer.Show(player);
                float t0 = Time.time;
                yield return Until(() => UnitsOf(tinA).Count >= 4, 20f);
                float took = Time.time - t0;
                yield return Wait(5f);
                Check("a Tin keeps at most 4 Crafted, one every 4 s", UnitsOf(tinA).Count == 4 && took > 9f, UnitsOf(tinA).Count + " alive, 3 more took " + F(took) + " s");
                // TARGET
                Genitem tile = ModKitTest.FoeTile(SpoolContent.Beings[SpoolData.Stitched], tinA.Pos, 3f, 5f, used);
                if (tile != null)
                {
                    Send(tinA.Master, SpoolData.TargetLink, At(tile.Position));
                    yield return Wait(0.6f);
                    Check("TARGET sets the Tin's spot and plants its pin", tinA.HasSpot && ModKitUtil.Flat(tinA.Spot, tile.Position) < 0.6f && tinA.Pin != null, tinA.LastTarget + ", pin " + (tinA.Pin != null));
                    Check("the pin is a walk-through marker", tinA.Pin != null && ModKitUtil.BeingName(tinA.Pin) == SpoolData.Pin && SpoolContent.PinBlock != "", SpoolContent.PinBlock);
                    List<SpoolUnitBrain> old = UnitsOf(tinA);
                    Kill(old[0].Master);
                    SpoolUnitBrain fresh = null;
                    yield return Until(() => (fresh = UnitsOf(tinA).Find(u => !old.Contains(u))) != null, 6f);
                    yield return Until(() => fresh == null || fresh.Arrived, 15f);
                    Check("a new Crafted runs to the spot", fresh != null && fresh.Arrived && ModKitUtil.Flat(ModKitUtil.Pos(fresh.Master), tinA.Spot) < 1.2f, fresh != null ? "at " + F(ModKitUtil.Flat(ModKitUtil.Pos(fresh.Master), tinA.Spot)) + " from the spot" : "none stitched");
                    Check("Crafted already out keep their spot", old.Count > 1 && old[1] != null && ModKitUtil.Flat(old[1].Spot, tinA.Pos) < 0.6f);
                    Vector3 spot = tinA.Spot;
                    Send(tinA.Master, SpoolData.TargetLink, At(tinA.Pos + new Vector3(20f, 0f, 0f)));
                    yield return Wait(0.4f);
                    Check("TARGET refuses a spot out of reach", tinA.Spot == spot && tinA.LastTarget == "far", tinA.LastTarget);
                }
                else ModKitTest.Setup("TARGET", "no standable tile 3-5 tiles from the Tin");
                // Selling a Crafted: half its Thread back.
                SpoolUnitBrain sold = UnitsOf(tinA).Find(u => u.Started);
                if (sold != null)
                {
                    player.Stash = 20;
                    SpoolServer.Show(player);
                    int refunded = SpoolServer.Refunded;
                    Send(hm, "Sell", Sell(hm, sold.Master));
                    yield return Until(() => sold == null || sold.Master == null, 12f);
                    Check("selling a Crafted refunds 5 Thread", (sold == null || sold.Master == null) && SpoolServer.Refunded == refunded + 5,
                        "refunded " + (SpoolServer.Refunded - refunded) + ", by the fallback " + hero.SoldByFallback);
                }
            }
            at = Spot(hm, SpoolContent.Beings[SpoolData.Tin], 4f);
            yield return Build(hm, SpoolData.Tin, at, known, got);
            tinB = got[0] as SpoolTinBrain;
            Check("build a second Cookie Tin", tinB != null, "at " + at);
        }

        // 5. Cookie Tin upgrades.
        if (tinA != null)
        {
            yield return Upgrade(tinA, "Recycle2", owner);
            Check("Reuse Reduce Recycle", Has(tinA, "Recycle2"));
            SpoolUnitBrain victim = UnitsOf(tinA).Find(u => u.Started);
            if (victim != null)
            {
                // Units stitched before the upgrade don't recycle; one stitched after does.
                Kill(victim.Master);
                SpoolUnitBrain next = null;
                yield return Until(() => (next = UnitsOf(tinA).Find(u => u.Started && u.Recycle > 0f)) != null, 8f);
                if (next != null)
                {
                    Vector3 np = ModKitUtil.Pos(next.Master);
                    int before = SpoolServer.DropsNear(np, 0.6f);
                    Kill(next.Master);
                    yield return Wait(0.3f);
                    Check("a killed Crafted drops half its cost", SpoolServer.DropsNear(np, 0.6f) == before + 5, (SpoolServer.DropsNear(np, 0.6f) - before) + " dropped");
                }
                else ModKitTest.Setup("Recycle drop", "no Crafted stitched after the upgrade");
            }
            yield return Upgrade(tinA, "Design3", owner);
            Check("New Design", Has(tinA, "Design3") && tinA.Kind == SpoolData.Dart);
            foreach (SpoolUnitBrain u in UnitsOf(tinA)) Kill(u.Master);
            player.Stash = 40;
            SpoolServer.Show(player);
            SpoolUnitBrain dart = null;
            yield return Until(() => (dart = UnitsOf(tinA).Find(u => u.Started)) != null, 6f);
            Link_Command attack = null;
            Check("New Design stitches Darts for 15 Thread", dart != null && dart.Def.Id == SpoolData.Dart && dart.Cost == 15, dart != null ? dart.Def.Id + " " + dart.Cost : "none");
            yield return Wait(0.3f);
            Check("a Dart throws needles (stock ranged AI)", dart != null && dart.Master.GetComponent<Entity_Ai_Mob_Ranged>() != null && dart.Master.Commandlistserver.TryGetValue("Attack", out attack) && attack.Goalstring == SpoolData.Needle,
                attack != null ? attack.Goalstring : "no Attack command");
#if SPOOL_SHOTS
            if (dart != null)
            {
                Entity_Master aimAt = ModKitTest.SpawnFoe("Enemy_Dagger", ModKitUtil.Pos(dart.Master), 1.5f, 3f, null);
                yield return Wait(1.6f);
                yield return SpoolShots.Close("dart_needle", ModKitUtil.Pos(dart.Master), 4f);
                if (aimAt != null && ModKitUtil.Alive(aimAt)) Game_Entity.Instance.Despawn(aimAt.Beingserver, false, "Selftest");
            }
#endif
            yield return Upgrade(tinA, "Surprise4", owner);
            Check("One Last Surprise", Has(tinA, "Surprise4"));
            foreach (SpoolUnitBrain u in UnitsOf(tinA)) Kill(u.Master);
            player.Stash = 50;
            SpoolServer.Show(player);
            SpoolUnitBrain bomb = null;
            yield return Until(() => (bomb = UnitsOf(tinA).Find(u => u.Started && u.Burst > 0f)) != null, 6f);
            Entity_Master[] beside = new Entity_Master[1];
            if (bomb != null)
                yield return Foe("Enemy_Dagger", ModKitUtil.Pos(bomb.Master), 2f, 0f, beside);
            Entity_Master foe = beside[0];
            if (foe != null)
                yield return Wait(0.3f);   // its hit box reaches the physics scene that Entityarea searches (round 1b)
            float apart = foe != null && bomb != null && bomb.Master != null && ModKitUtil.Alive(foe) ? ModKitUtil.Flat(ModKitUtil.Pos(foe), ModKitUtil.Pos(bomb.Master)) : -1f;
            if (apart >= 0f && apart <= ModKitUtil.Stat(bomb.Master, "Range", 1.5f) - 0.2f)
            {
                float hp = ModKitUtil.Health(foe);
                float expect = ModKitUtil.Stat(bomb.Master, "Damage", 2.5f) * 3f;
                int seen = ModKitUtil.FoesNear(tinA.Master, ModKitUtil.Pos(bomb.Master), ModKitUtil.Stat(bomb.Master, "Range", 1.5f)).Count;
                Kill(bomb.Master);
                yield return Wait(0.3f);
                Check("a falling Crafted bursts for 3x its damage", !ModKitUtil.Alive(foe) || hp - ModKitUtil.Health(foe) >= expect - 0.05f,
                    F(hp) + "->" + F(ModKitUtil.Alive(foe) ? ModKitUtil.Health(foe) : 0f) + ", expected -" + F(expect) + ", " + F(apart) + " apart, foes in its reach " + seen);
            }
            else ModKitTest.Setup("One Last Surprise", bomb == null ? "no Crafted stitched after the upgrade, Stash " + player.Stash : foe == null ? "no foe could stand beside it" : "the foe was " + F(apart) + " from it");
            ClearFoes();
        }
        if (tinB != null)
        {
            yield return Upgrade(tinB, "Woolen2", owner);
            yield return Upgrade(tinB, "Discounted3", owner);
            Check("Woolen Lining, Discounted Material", Has(tinB, "Woolen2") && Has(tinB, "Discounted3"));
            foreach (SpoolUnitBrain u in UnitsOf(tinB)) Kill(u.Master);
            yield return Wait(0.2f);
            player.Stash = 30;
            SpoolServer.Show(player);
            int s0 = player.Stash;
            SpoolUnitBrain u2 = null;
            yield return Until(() => (u2 = UnitsOf(tinB).Find(u => u.Started)) != null, 6f);
            yield return Wait(0.2f);
            Check("Discounted Material: a Stitched costs 8", u2 != null && u2.Cost == 8, u2 != null ? "cost " + u2.Cost + ", Stash " + s0 + "->" + player.Stash : "none");
            Check("Woolen Lining: Crafted health +50%", u2 != null && Mathf.Abs(ModKitUtil.MaxHealth(u2.Master) - 30f) < 0.05f && Mathf.Abs(ModKitUtil.Health(u2.Master) - 30f) < 0.05f, u2 != null ? F(ModKitUtil.Health(u2.Master)) + "/" + F(ModKitUtil.MaxHealth(u2.Master)) : "");
            Check("Discounted Material: Stitched walk 30% faster", u2 != null && Mathf.Abs(ModKitUtil.Stat(u2.Master, "Speed", 0f) - 1.8f * 1.3f) < 0.02f, u2 != null ? F(ModKitUtil.Stat(u2.Master, "Speed", 0f)) : "");
        }
#if SPOOL_SHOTS
        yield return Wait(1f);
        yield return SpoolShots.Shot("tins");
#endif

        // 6. Thread Heads: A takes Threaded Delivery, B Patchwork Job.
        SpoolHeadBrain headA = null, headB = null;
        {
            Beingobject token = SpoolContent.Beings[SpoolData.HeadToken];
            for (int i = 0; i < 2; i++)
            {
                float red = Res(owner, "Redaura");
                at = Spot(hm, token, i == 0 ? 3f : 4.5f);
                if (float.IsNaN(at.x)) { ModKitTest.Setup("place a Thread Head", "no walkable tile for its token"); continue; }
                Send(hm, "Build", "{\"Tower\":\"" + SpoolData.HeadToken + "\",\"Position\":" + Vec(at) + ",\"Orientation\":0,\"Owner\":" + owner + "}");
                SpoolHeadBrain h = null;
                yield return Until(() => (h = NewHead(known)) != null, 20f);
                if (!Check("place a Thread Head from the build menu", h != null, "at " + at + ", swapped " + SpoolServer.Swapped))
                    continue;
                known.Add(h);
                if (i == 0) headA = h; else headB = h;
                yield return Wait(0.5f);
                Check("Thread Head costs Red 40", Mathf.Abs(red - Res(owner, "Redaura") - 40f) < 0.01f, "spent " + F(red - Res(owner, "Redaura")));
                Check("its token is gone, the Thread Head guards the token's tile", Beings(SpoolData.HeadToken) == 0 && ModKitUtil.Flat(h.Spot, at) < 0.6f, "tokens " + Beings(SpoolData.HeadToken) + ", spot " + F(ModKitUtil.Flat(h.Spot, at)));
                Check("Thread Head has SPOT, RETURN and upgrades", h.Master.Commandlistserver.ContainsKey(SpoolData.SpotLink) && h.Master.Commandlistserver.ContainsKey(SpoolData.ReturnLink) && h.Master.Commandlistserver.ContainsKey("Upgrade"));
                Check("Thread Head stats", Mathf.Approximately(ModKitUtil.MaxHealth(h.Master), 60f) && Mathf.Approximately(ModKitUtil.Stat(h.Master, "Speed", 0f), 2.25f) && h.Cap == 20,
                    F(ModKitUtil.MaxHealth(h.Master)) + " health, speed " + F(ModKitUtil.Stat(h.Master, "Speed", 0f)) + ", carries " + h.Cap);
            }
            if (headA != null)
            {
                // Two tiles from its spot, on the side away from the vessel (which picks Thread up within a tile).
                ClearFoes();
                ClearDrops();
                Vector3 away = headA.Spot - ModKitUtil.Pos(hm);
                away.y = 0f;
                Vector3 dp = headA.Spot + (away.sqrMagnitude > 0.01f ? away.normalized : Vector3.right) * 2f;
                headA.Carried = 0;   // both had filled up on earlier checks' Thread (round 1b: carried 20)
                if (headB != null) headB.Carried = 0;
                int carried0 = 0;
                SpoolServer.Drop(dp, 4);
                yield return Wait(1f);
                int pulled = headA.Carried + (headB != null ? headB.Carried : 0) - carried0;
                Check("a Thread Head pulls in Thread within 3 tiles", pulled == 4 && headA.Carried >= 4 && SpoolServer.DropsNear(dp, 0.5f) == 0, "pulled " + pulled + ", carried " + headA.Carried + ", left " + SpoolServer.DropsNear(dp, 0.5f));
                player.Stash = 10;
                SpoolServer.Show(player);
                int deliveries = headA.Deliveries, delivered = SpoolServer.Delivered, carrying = headA.Carried;
                Send(headA.Master, SpoolData.ReturnLink, "");
                yield return Until(() => headA.Deliveries > deliveries, 25f);
                Check("RETURN: it carries its Thread home", headA.Deliveries > deliveries && headA.Carried == 0 && SpoolServer.Delivered - delivered >= carrying,
                    "delivered " + (SpoolServer.Delivered - delivered) + " of " + carrying + ", carried " + headA.Carried);
                yield return Until(() => ModKitUtil.Flat(headA.Pos, headA.Spot) < 1f, 25f);
                Check("then it walks back to its spot", ModKitUtil.Flat(headA.Pos, headA.Spot) < 1f, F(ModKitUtil.Flat(headA.Pos, headA.Spot)) + " from it");
                Genitem tile = ModKitTest.FoeTile(SpoolContent.Beings[SpoolData.Head], headA.Spot, 2f, 4f, used);
                if (tile != null)
                {
                    Send(headA.Master, SpoolData.SpotLink, At(tile.Position));
                    yield return Until(() => ModKitUtil.Flat(headA.Pos, tile.Position) < 1f, 15f);
                    Check("SPOT moves the tile it guards", ModKitUtil.Flat(headA.Spot, tile.Position) < 0.6f && ModKitUtil.Flat(headA.Pos, tile.Position) < 1f, F(ModKitUtil.Flat(headA.Pos, tile.Position)) + " from it");
                }
                yield return Upgrade(headA, "Delivery2", owner);
                Check("Threaded Delivery", Has(headA, "Delivery2"));
                deliveries = headA.Deliveries;
                headA.Carried = headA.Cap;
                yield return Wait(0.6f);
                Check("Threaded Delivery: full, it heads home on its own", headA.Returning || headA.Deliveries > deliveries);
                yield return Until(() => headA.Deliveries > deliveries, 25f);
                Check("... and hands the Thread over", headA.Deliveries > deliveries);
            }
            if (headB != null)
            {
                yield return Upgrade(headB, "Patchwork2", owner);
                Check("Patchwork Job adds MEND", Has(headB, "Patchwork2") && headB.Master.Commandlistserver.ContainsKey(SpoolData.MendLink));
                Send(headB.Master, SpoolData.MendLink, "");
                yield return Wait(0.3f);
                Check("MEND switches on", headB.Mending && headB.Master.Commandlistserver[SpoolData.MendLink].Goalstring == "On");
                headB.Carried = 2;
                Hit(hm, headB.Master, 6f);
                float hp = ModKitUtil.Health(headB.Master);
                yield return Wait(2.6f);
                Check("MEND heals 2 a second and spends carried Thread", ModKitUtil.Health(headB.Master) >= hp + 3.9f && headB.Carried < 2, F(hp) + "->" + F(ModKitUtil.Health(headB.Master)) + ", carried 2->" + headB.Carried);
            }
            if (headA != null)
            {
                headA.Carried = 3;
                int dropped = SpoolServer.Dropped;
                int returned = SpoolServer.Returned;
                Kill(headA.Master);
                Check("a fallen Thread Head drops what it carried", SpoolServer.Dropped - dropped == 3, (SpoolServer.Dropped - dropped) + " dropped");
                yield return Wait(0.3f);
                Check("... and waits to come back", SpoolServer.WaitingHeads(owner) == 1);
                yield return Until(() => SpoolServer.Returned > returned, 20f);
                SpoolHeadBrain back = null;
                yield return Until(() => (back = NewHead(known)) != null, 3f);
                yield return Wait(0.5f);
                Check("it comes back at the Stash after 15 s with its upgrades", back != null && Has(back, "Delivery2"),
                    back != null ? "upgrades " + back.Master.Upgradelistserver.Count : "came back " + (SpoolServer.Returned - returned) + ", none found");
                if (back != null)
                {
                    known.Add(back);
                    headA = back;
                    yield return Until(() => ModKitUtil.Flat(back.Pos, back.Spot) < 1f, 25f);
                    Check("... then walks to its spot", ModKitUtil.Flat(back.Pos, back.Spot) < 1f, F(ModKitUtil.Flat(back.Pos, back.Spot)) + " from it");
                }
            }
            if (headB != null)
            {
                float red = Res(owner, "Redaura");
                Send(hm, "Sell", Sell(hm, headB.Master));
                yield return Until(() => headB == null || headB.Master == null, 15f);
                Check("a Thread Head can be sold", headB == null || headB.Master == null, "Red back " + F(Res(owner, "Redaura") - red) + ", by the fallback " + hero.SoldByFallback);
            }
            // Thread Heads pull in what the Cushions and the Looms put out within 3 tiles: gone before those checks
            // (not a kill, so no comeback).
            foreach (SpoolHeadBrain h in SpoolServer.Owned<SpoolHeadBrain>(owner))
                Game_Entity.Instance.Despawn(h.Master.Beingserver, false, "Selftest");
        }
#if SPOOL_SHOTS
        yield return Wait(1f);
        yield return SpoolShots.Shot("heads");
#endif

        // 7. Pin Cushions: A Sharper Pins, Pin Barrage; B Slow Unravel, Loose Stuffing, Overstuffed; C Loose Stuffing, Burst Seams.
        SpoolCushionBrain[] cushions = new SpoolCushionBrain[3];
        for (int i = 0; i < 3; i++)
        {
            float blue = Res(owner, "Blueaura");
            at = Spot(hm, SpoolContent.Beings[SpoolData.Cushion], 2.5f + i);
            yield return Build(hm, SpoolData.Cushion, at, known, got);
            cushions[i] = got[0] as SpoolCushionBrain;
            if (Check("build a Pin Cushion", cushions[i] != null, "at " + at) && i == 0)
                Check("Pin Cushion costs Blue 20", Mathf.Abs(blue - Res(owner, "Blueaura") - 20f) < 0.01f, "spent " + F(blue - Res(owner, "Blueaura")));
        }
        // The checks read the Cushion's own counts; the foes that hit it stand by the enemy core (FarFoe).
        ClearFoes();
        ClearDrops();
        Entity_Master[] far = new Entity_Master[1];
        if (cushions[0] != null)
        {
            SpoolCushionBrain c = cushions[0];
            yield return FarFoe("Enemy_Dagger", far);
            if (far[0] != null)
            {
                Entity_Master dagger = far[0];
                int pricks = c.Pricks;
                float back = c.Reflected, hp = ModKitUtil.Health(dagger);
                Hit(dagger, c.Master, 10f);
                yield return Wait(0.2f);
                Check("Pin Cushion pricks a melee attacker for 20%", c.Pricks == pricks + 1 && Mathf.Abs(c.Reflected - back - 2f) < 0.05f,
                    "pricks +" + (c.Pricks - pricks) + ", " + F(c.Reflected - back) + " back, foe " + F(hp) + "->" + F(ModKitUtil.Health(dagger)) + ", its range " + F(ModKitUtil.Stat(dagger, "Range", -1f)));
                int spun = c.Spun, dropped = SpoolServer.Dropped;
                Vector3 cp = c.Pos;
                int near = SpoolServer.DropsNear(cp, 1.2f);
                Hit(dagger, c.Master, 10f);
                Hit(dagger, c.Master, 10f);
                yield return Wait(0.2f);
                Check("every 25 damage spins a Thread beside it", c.Spun == spun + 1 && SpoolServer.Dropped == dropped + 1 && SpoolServer.DropsNear(cp, 1.2f) >= Mathf.Max(1, near),   // it can join a tuft already there
                    "spun " + (c.Spun - spun) + " after " + F(c.Taken) + ", beside it " + (SpoolServer.DropsNear(cp, 1.2f) - near) + ", dropped anywhere " + (SpoolServer.Dropped - dropped));
#if SPOOL_SHOTS
                yield return SpoolShots.Close("tuft", cp, 3f);
#endif
                yield return Upgrade(c, "Sharper2", owner);
                yield return FarFoe("Enemy_Dagger", far);
                back = c.Reflected;
                if (far[0] != null)
                    Hit(far[0], c.Master, 10f);
                yield return Wait(0.2f);
                Check("Sharper Pins: 40%", Has(c, "Sharper2") && Mathf.Abs(c.Reflected - back - 4f) < 0.05f, F(c.Reflected - back) + " back");
            }
            else ModKitTest.Setup("Pin Cushion reflect", "no Dagger by the enemy core");
            Entity_Master[] ranged = new Entity_Master[1];
            yield return FarFoe("Enemy_Ballista", ranged);
            Entity_Master archer = ranged[0];
            float range = archer != null ? ModKitUtil.Stat(archer, "Range", 0f) : 0f;
            if (archer != null && range > SpoolCushionBrain.MeleeRange)
            {
                int pricks = c.Pricks;
                Hit(archer, c.Master, 10f);
                yield return Wait(0.2f);
                Check("a ranged attacker is not pricked", c.Pricks == pricks, "range " + F(range) + ", pricks +" + (c.Pricks - pricks));
                yield return Upgrade(c, "Barrage3", owner);
                yield return FarFoe("Enemy_Ballista", ranged);
                float back = c.Reflected;
                if (ranged[0] != null)
                    Hit(ranged[0], c.Master, 10f);
                yield return Wait(0.2f);
                Check("Pin Barrage: ranged attackers pricked for 20%", Has(c, "Barrage3") && Mathf.Abs(c.Reflected - back - 2f) < 0.05f, "range " + F(range) + ", " + F(c.Reflected - back) + " back");
                Check("Pin Barrage ends its branch", c.Master.Upgradelistserver[SpoolData.Cushion + "_Barrage3_Upgradeobject"].Upgrades.Count == 0);
            }
            else ModKitTest.Setup("Pin Barrage", archer == null ? "no Ballista by the enemy core" : "the Ballista's range is " + F(range) + ", melee by the Cushion's rule");
        }
        if (cushions[1] != null)
        {
            // Slow Unravel, the heal and Loose Stuffing don't care who hits: the vessel does (it isn't pricked). The foes
            // left over from the reflect checks would be walking in by now.
            ClearFoes();
            SpoolCushionBrain c = cushions[1];
            yield return Upgrade(c, "Unravel2", owner);
            float ch = ModKitUtil.Health(c.Master);
            Hit(hm, c.Master, 10f);
            yield return Wait(0.2f);
            float now = ch - ModKitUtil.Health(c.Master);
            Check("Slow Unravel: half a hit now", Has(c, "Unravel2") && Mathf.Abs(now - 5f) < 0.05f && Mathf.Abs(c.Owing() - 5f) < 0.05f, "took " + F(now) + ", owing " + F(c.Owing()));
            yield return Wait(4.6f);
            Check("... the other half over 4 seconds", Mathf.Abs(ch - ModKitUtil.Health(c.Master) - 10f) < 0.1f && c.Owing() < 0.01f, "took " + F(ch - ModKitUtil.Health(c.Master)));
            Check("Slow Unravel spins twice the Thread", Mathf.Abs(ModKitUtil.Stat(c.Master, "Spin", 0f) - 12.5f) < 0.01f, "Spin " + F(ModKitUtil.Stat(c.Master, "Spin", 0f)));
            float cancelled = c.Cancelled;
            Hit(hm, c.Master, 10f);
            yield return Wait(0.1f);
            ModKitUtil.Heal(hm, c.Master, 3f);
            yield return Wait(0.1f);
            Check("a heal cancels damage still owed", c.Cancelled - cancelled >= 2.9f, "cancelled " + F(c.Cancelled - cancelled) + ", owing " + F(c.Owing()));
            yield return Upgrade(c, "Stuffing3", owner);
            ModKitUtil.Heal(hm, c.Master, 500f);
            int mangled = c.Mangled;
            Hit(hm, c.Master, 40f);
            yield return Wait(0.5f);
            Check("Loose Stuffing: a Mangled per 40 damage", Has(c, "Stuffing3") && c.Mangled == mangled + 1 && UnitsOf(c).Exists(u => u.Def.Id == SpoolData.Mangled), "made " + (c.Mangled - mangled));
            // Three more 40s, each healed (a heal also cancels what Slow Unravel still owes), so the Cushion stands.
            for (int i = 0; i < 3; i++)
            {
                ModKitUtil.Heal(hm, c.Master, 500f);
                Hit(hm, c.Master, 40f);
                yield return Wait(0.3f);
            }
            ModKitUtil.Heal(hm, c.Master, 500f);
            Check("at most 3 Mangled alive", UnitsOf(c).Count == 3, UnitsOf(c).Count + " alive, made " + (c.Mangled - mangled));
#if SPOOL_SHOTS
            yield return SpoolShots.Close("mangled", c.Pos, 3.5f);
#endif
            yield return Upgrade(c, "Overstuffed4", owner);
            foreach (SpoolUnitBrain u in UnitsOf(c)) Kill(u.Master);
            yield return Wait(0.3f);
            ModKitUtil.Heal(hm, c.Master, 500f);
            yield return FarFoe("Enemy_Dagger", far);
            Hit(far[0] != null ? far[0] : hm, c.Master, 40f);
            yield return Wait(0.4f);
            ModKitUtil.Heal(hm, c.Master, 500f);
            SpoolUnitBrain stuffed = UnitsOf(c).Find(u => u.Def.Id == SpoolData.Stuffed);
            Check("Overstuffed: Mangled with 3x health", stuffed != null && Mathf.Abs(ModKitUtil.MaxHealth(stuffed.Master) - 45f) < 0.05f, stuffed != null ? F(ModKitUtil.MaxHealth(stuffed.Master)) : "none");
            Check("... standing on Sno Sno's blocking block", stuffed != null && stuffed.Master.Beingserver.Being.Block != null && stuffed.Master.Beingserver.Being.Block.name == SpoolContent.StuffedBlock && SpoolContent.StuffedBlock != "", SpoolContent.StuffedBlock);
            if (stuffed != null)
            {
                yield return Until(() => stuffed == null || stuffed.Holding, 4f);
                Check("... which leaps in and holds", stuffed != null && stuffed.Holding, stuffed != null ? "at " + F(ModKitUtil.Flat(ModKitUtil.Pos(stuffed.Master), c.Pos)) + " from the Cushion" : "");
            }
            ClearFoes();
        }
        if (cushions[2] != null)
        {
            SpoolCushionBrain c = cushions[2];
            yield return Upgrade(c, "Sharper2", owner);
            yield return Upgrade(c, "Stuffing3", owner);
            yield return Upgrade(c, "Burst4", owner);
            Check("Burst Seams", Has(c, "Burst4"));
            // A Dagger beside the Cushion for a Mangled to reach (another when the vessel or the Crafted fell it first).
            Entity_Master[] beside = new Entity_Master[1];
            int bursts = c.Bursts, made = c.Mangled, foes = 0;
            float t0 = Time.time;
            while (c.Bursts == bursts && Time.time - t0 < 15f)
            {
                if (!ModKitUtil.Alive(beside[0]))
                {
                    yield return Foe("Enemy_Dagger", c.Pos, 2.5f, 1f, beside);
                    if (beside[0] != null) foes++;
                }
                if (UnitsOf(c).Count == 0 && ModKitUtil.Alive(beside[0]))
                {
                    ModKitUtil.Heal(hm, c.Master, 500f);
                    Hit(hm, c.Master, 40f);
                }
                yield return Wait(0.25f);
            }
            Check("a Mangled that reaches a foe bursts", c.Bursts > bursts, "bursts " + (c.Bursts - bursts) + ", Mangled made " + (c.Mangled - made) + ", Daggers " + foes);
            ClearFoes();
        }
#if SPOOL_SHOTS
        yield return Wait(1f);
        yield return SpoolShots.Shot("cushions");
#endif

        // 8. Looms: A Direct Line, B Seam Allowance.
        SpoolLoomBrain loomA = null, loomB = null;
        for (int i = 0; i < 2; i++)
        {
            float yellow = Res(owner, "Yellowaura");
            at = Spot(hm, SpoolContent.Beings[SpoolData.Loom], 3f + i);
            yield return Build(hm, SpoolData.Loom, at, known, got);
            if (!Check("build a Loom", got[0] != null, "at " + at))
                continue;
            if (i == 0) loomA = (SpoolLoomBrain)got[0]; else loomB = (SpoolLoomBrain)got[0];
            if (i == 0)
                Check("Loom costs Yellow 40", Mathf.Abs(yellow - Res(owner, "Yellowaura") - 40f) < 0.01f, "spent " + F(yellow - Res(owner, "Yellowaura")));
        }
        if (loomA != null)
        {
            yield return Until(() => loomA.Held >= 1, 8f);
            Check("a Loom spins a Thread every 6 s", loomA.Held == 1 && Mathf.Approximately(ModKitUtil.Stat(loomA.Master, "Thread", -1f), 1f), "held " + loomA.Held);
            int strikes = loomA.Strikes;
            yield return Wait(3f);
            Entity_Master ore = Helper_Entityquerytools.Entitynearest(loomA.Pos, 2.5f, ModKitUtil.Tagged("Resource"));
            // No ore in reach tests nothing: a Setup, not a pass (MAX SPL-9).
            if (ore == null)
                ModKitTest.Setup("Loom strikes ore in reach", "no ore in reach here");
            else
                Check("Loom strikes ore in reach", loomA.Strikes > strikes, strikes + "->" + loomA.Strikes);
            player.Carried = 0;
            int held = loomA.Held, delivered = SpoolServer.Delivered;
            yield return Walk(hm, loomA.Pos, SpoolData.LoomPick - 0.3f, 10f);
            yield return Wait(0.5f);
            int took = player.Carried + SpoolServer.Delivered - delivered;
            Check("Spool takes a Loom's Thread within 1.5 tiles", held >= 1 && took >= held && loomA.Held <= 1, "held " + held + "->" + loomA.Held + ", took " + took);
            loomA.TakeHeld(100);
            yield return Walk(hm, home, 1f, 10f);
            yield return Upgrade(loomA, "Direct2", owner);
            player.Stash = 10;
            SpoolServer.Show(player);
            int sent = loomA.Sent;
            yield return Until(() => loomA.Sent > sent, 8f);
            Check("Direct Line: spun Thread goes straight to the Stash", Has(loomA, "Direct2") && loomA.Sent == sent + 1 && loomA.Held == 0, "sent " + (loomA.Sent - sent) + ", held " + loomA.Held);
        }
        if (loomB != null)
        {
            yield return Upgrade(loomB, "Seam2", owner);
            SpoolUnitBrain seam = null;
            yield return Until(() => (seam = UnitsOf(loomB).Find(u => u.Def.Id == SpoolData.Seam)) != null, 3f);
            Check("Seam Allowance: a Seam beside the Loom", seam != null);
            if (seam != null)
            {
                Kill(seam.Master);
                float t0 = Time.time;
                SpoolUnitBrain again = null;
                yield return Until(() => (again = UnitsOf(loomB).Find(u => u.Def.Id == SpoolData.Seam && u != seam)) != null, 13f);
                Check("a fallen Seam comes back after 10 s", again != null && Time.time - t0 > 9f, F(Time.time - t0) + " s");
            }
            loomB.Held = loomB.Hold;
            yield return Wait(7f);
            Check("a Loom holds at most 10", loomB.Held == 10, "held " + loomB.Held);
        }

        // 9. PATCH.
        if (tinA != null)
        {
            yield return Walk(hm, tinA.Pos, 2f, 12f);
            Hit(hm, tinA.Master, 30f);
            yield return Wait(5.5f);
            player.Stash = 40;
            SpoolServer.Show(player);
            float hp = ModKitUtil.Health(tinA.Master), max = ModKitUtil.MaxHealth(tinA.Master);
            int patches = hero.Patches, traffic = Traffic(owner);
            Send(hm, SpoolData.PatchLink, "{\"Ref\":" + tinA.Master.NetworkObjectId + "}");
            yield return Wait(0.4f);
            int patchCost = 40 - player.Stash + (Traffic(owner) - traffic);
            Check("PATCH mends 20% for 20 Thread", hero.Patches == patches + 1 && Mathf.Abs(ModKitUtil.Health(tinA.Master) - Mathf.Min(max, hp + max * 0.2f)) < 0.1f && patchCost == 20,
                F(hp) + "->" + F(ModKitUtil.Health(tinA.Master)) + " of " + F(max) + ", PATCH took " + patchCost + " (other traffic " + (Traffic(owner) - traffic) + "), " + hero.LastPatch);
            Check("PATCH greys out while it threads", hm.Commandlistserver[SpoolData.PatchLink].Goalstring == "Off");
            Send(hm, SpoolData.PatchLink, "{\"Ref\":" + tinA.Master.NetworkObjectId + "}");
            yield return Wait(0.3f);
            Check("PATCH has a 5 s cooldown", hero.Patches == patches + 1);
            yield return Wait(5.2f);
            Check("PATCH comes back", hm.Commandlistserver[SpoolData.PatchLink].Goalstring == "");
            player.Stash = 5;
            Send(hm, SpoolData.PatchLink, "{\"Ref\":" + tinA.Master.NetworkObjectId + "}");
            yield return Wait(0.3f);
            Check("PATCH needs 20 Thread", hero.Patches == patches + 1 && hero.LastPatch == "short", hero.LastPatch);
            player.Stash = 40;
            ModKitUtil.Heal(hm, tinA.Master, 500f);
            yield return Wait(0.2f);
            Send(hm, SpoolData.PatchLink, "{\"Ref\":" + tinA.Master.NetworkObjectId + "}");
            yield return Wait(0.3f);
            Check("PATCH refuses what is in one piece", hero.Patches == patches + 1 && (hero.LastPatch == "full" || hero.LastPatch == "nothing hurt in reach"), hero.LastPatch);
            if (loomB != null && ModKitUtil.Flat(loomB.Pos, ModKitUtil.Pos(hm)) > SpoolData.PatchRange + 1f)
            {
                Hit(hm, loomB.Master, 10f);
                Send(hm, SpoolData.PatchLink, "{\"Ref\":" + loomB.Master.NetworkObjectId + "}");
                yield return Wait(0.3f);
                Check("PATCH reaches 4 tiles", hero.Patches == patches + 1 && hero.LastPatch == "too far", hero.LastPatch);
            }
            Hit(hm, tinA.Master, 20f);
            yield return Wait(0.1f);
            Send(hm, SpoolData.PatchLink, "");
            yield return Wait(0.3f);
            Check("the PATCH hotkey mends the most hurt in reach", hero.Patches == patches + 2, hero.LastPatch);
        }
        else ModKitTest.Setup("PATCH", "no Cookie Tin to mend");

        // 10. The Stash falls, and is sold.
        {
            player.Stash = 30;
            SpoolServer.Show(player);
            int spilled = SpoolServer.Spilled;
            Kill(stash.Master);
            yield return Wait(0.8f);
            Check("a fallen Stash spills its Thread", SpoolServer.Spilled == spilled + 30 && player.Stash == 0 && SpoolServer.DropsNear(sp, 2.5f) >= 30, "spilled " + (SpoolServer.Spilled - spilled) + ", on the ground " + SpoolServer.DropsNear(sp, 2.5f));
            Check("... and the menu goes back to the Stash", hero.Menu() == SpoolData.Stash, hero.Menu());
            at = Spot(hm, SpoolContent.Beings[SpoolData.Stash], 2.5f);
            int traffic = Traffic(owner);
            yield return Build(hm, SpoolData.Stash, at, known, got);
            stash = got[0] as SpoolStashBrain;
            int start = player.Stash - (Traffic(owner) - traffic);
            Check("a rebuilt Stash starts empty", stash != null && start == 0, "started with " + start + ", Stash " + player.Stash);
            if (stash != null)
            {
                player.Stash = 12;
                SpoolServer.Show(player);
                Send(hm, "Sell", Sell(hm, stash.Master));
                yield return Until(() => stash == null || stash.Master == null, 12f);
                yield return Wait(0.5f);
                at = Spot(hm, SpoolContent.Beings[SpoolData.Stash], 3f);
                yield return Build(hm, SpoolData.Stash, at, known, got);
                yield return Wait(0.5f);
                Check("a sold Stash keeps its Thread for the next one", got[0] != null && player.Stash == 12, "Stash " + player.Stash);
            }
        }

// 10b. The Directions (kit 1.7): each rank's effect, as a player meets it. (Untested until 2026-11-04.)
        {
            Func<string, Upgradeproperties> direction = n => Bl_Address.Getload("Upgrade", ModKitDirections.Id("Spool", n)) as Upgradeproperties;
            SpoolTinBrain sellTin = tinB ?? tinA;
            for (int r = 1; r <= 3 && sellTin != null; r++)
            {
                SpoolData.Recycler rec = SpoolData.Recyclers[r - 1];
                ModKitDirections.Grant(hm, direction(rec.Name));
                yield return Wait(0.8f);
                Check("Efficient Recycler rank " + r + " reads back on the vessel", player.Recycler == r, "rank " + player.Recycler);
                player.Stash = 40;
                SpoolServer.Show(player);
                SpoolUnitBrain unit = null;
                yield return Until(() => (unit = UnitsOf(sellTin).Find(u => u.Started)) != null, 12f);
                if (unit == null) { ModKitTest.Setup("Efficient Recycler rank " + r, "no Crafted to sell"); continue; }
                // Rank III mends what stands within 2.5 of the sold one: a building hurt beside it.
                SpoolBuilding beside = null;
                float toBeside = rec.Radius;
                foreach (SpoolBuilding b in SpoolServer.Buildings)
                    if (b != null && b.Started && b.Master != null && ModKitUtil.Alive(b.Master) && ModKitUtil.Flat(b.Pos, ModKitUtil.Pos(unit.Master)) < toBeside) { beside = b; toBeside = ModKitUtil.Flat(b.Pos, ModKitUtil.Pos(unit.Master)); }
                float hurt0 = 0f;
                if (beside != null)
                {
                    Hit(hm, beside.Master, 15f);
                    yield return Wait(0.2f);
                    hurt0 = ModKitUtil.Health(beside.Master);
                }
                int refunded = SpoolServer.Refunded, stash0 = player.Stash, cost = unit.Cost;
                SpoolUnitBrain sold = unit;
                Send(hm, "Sell", Sell(hm, sold.Master));
                yield return Until(() => sold == null || sold.Master == null, 12f);
                int want = cost * rec.Percent / 100;
                Check("Efficient Recycler rank " + r + ": selling a Crafted refunds " + rec.Percent + "%", SpoolServer.Refunded == refunded + want, "cost " + cost + ", refunded " + (SpoolServer.Refunded - refunded) + ", wanted " + want);
                if (rec.Heal > 0f)
                {
                    yield return Wait(0.3f);
                    if (beside == null) ModKitTest.Setup("Efficient Recycler rank III's mend", "nothing stood within " + F(rec.Radius) + " of the sold Crafted");
                    else Check("Efficient Recycler rank III mends what stands around the sold one", ModKitUtil.Health(beside.Master) > hurt0, F(hurt0) + " -> " + F(ModKitUtil.Health(beside.Master)) + ", " + F(toBeside) + " away");
                }
            }

            SpoolLoomBrain sewer = loomB ?? loomA;
            if (sewer == null) ModKitTest.Setup("Sewing Machine", "no Loom");
            else
            {
                float aspd0 = ModKitUtil.Stat(sewer.Master, "Attackspeed", 0f);
                int held0 = sewer.Held, sent0 = sewer.Sent;
                sewer.Held = 0;
                ModKitDirections.Grant(hm, direction(SpoolData.Sewingmachines[0].Name));
                yield return Wait(1.2f);
                Check("Sewing Machine: the Looms mine half as fast again", player.Sewing == 1 && sewer.Sewing && Mathf.Abs(ModKitUtil.Stat(sewer.Master, "Attackspeed", 0f) - aspd0 * (1f + SpoolData.SewingMine)) < 0.01f, F(aspd0) + " -> " + F(ModKitUtil.Stat(sewer.Master, "Attackspeed", 0f)));
                sewer.Held = 0;
                sent0 = sewer.Sent;
                yield return Wait(7f);
                Check("Sewing Machine: the Looms spin no Thread", sewer.Held == 0 && sewer.Sent == sent0, "held " + sewer.Held + ", sent " + (sewer.Sent - sent0));
                int looms = 0;
                foreach (SpoolBuilding b in SpoolServer.Buildings) if (b is SpoolLoomBrain && b.Started && ModKitUtil.Alive(b.Master) && ModKitUtil.Flat(b.Pos, sewer.Pos) <= SpoolData.Sewingmachines[0].Radius) looms++;
                for (int r = 1; r <= 2; r++)
                {
                    if (r > 1)
                    {
                        ModKitDirections.Grant(hm, direction(SpoolData.Sewingmachines[r - 1].Name));
                        yield return Wait(1.2f);
                    }
                    SpoolData.Sewing s = SpoolData.Sewingmachines[r - 1];
                    Hit(hm, sewer.Master, s.Heal * looms + 5f);
                    yield return Wait(0.2f);
                    float h0 = ModKitUtil.Health(sewer.Master);
                    yield return NextTide();
                    float gain = ModKitUtil.Health(sewer.Master) - h0;
                    Check("Sewing Machine rank " + r + ": each Tide it mends " + F(s.Heal) + " around itself", player.Sewing == r && Mathf.Abs(gain - s.Heal * looms) < 0.6f, "rank " + player.Sewing + ", health +" + F(gain) + " from " + looms + " Looms");
                }
                // Rank III reaches 5: a building between 3 and 5 from every Loom heals only at rank III.
                SpoolBuilding farOne = null;
                foreach (SpoolBuilding b in SpoolServer.Buildings)
                {
                    if (b == null || !b.Started || b is SpoolLoomBrain || !ModKitUtil.Alive(b.Master)) continue;
                    float nearest = float.MaxValue;
                    foreach (SpoolBuilding l in SpoolServer.Buildings) if (l is SpoolLoomBrain && l.Started) nearest = Mathf.Min(nearest, ModKitUtil.Flat(b.Pos, l.Pos));
                    if (nearest > SpoolData.Sewingmachines[1].Radius + 0.75f && nearest < SpoolData.Sewingmachines[2].Radius - 0.1f) { farOne = b; break; }   // the engine's area query reaches a little past its radius (round 3)
                }
                if (farOne == null) ModKitTest.Setup("Sewing Machine rank III's reach", "nothing stands 3 to 5 from the Looms");
                else
                {
                    Hit(hm, farOne.Master, 25f);
                    yield return Wait(0.2f);
                    float h1 = ModKitUtil.Health(farOne.Master);
                    yield return NextTide();
                    float h2 = ModKitUtil.Health(farOne.Master);
                    ModKitDirections.Grant(hm, direction(SpoolData.Sewingmachines[2].Name));
                    yield return Wait(1.2f);
                    yield return NextTide();
                    float h3 = ModKitUtil.Health(farOne.Master);
                    Check("Sewing Machine rank III: its reach grows to 5", Mathf.Abs(h2 - h1) < 0.01f && h3 > h2 && player.Sewing == 3, F(h1) + " at rank II -> " + F(h2) + ", at rank III -> " + F(h3));
                }
            }
        }
        // 11. The journal: Spool's pages, and every vessel page with exactly one tab per vessel (Smacky included).
        {
            Logobject page = SpoolContent.Entry.Log;
            bool own = page != null && page.name == "Log_" + SpoolData.Hero, being = false;
            Logobject tinPage = null, upgradePage = null, unitPage = null;
            if (own)
                foreach (Logitems.Base b in AllItems(page.Elements))
                {
                    if (b is Logitems.TextImageBeing && ((Logitems.TextImageBeing)b).Entity == SpoolContent.HeroBeing.Entity) being = true;
                    Logitems.Link l = b as Logitems.Link;
                    if (l != null && !l.Sidelink && l.Target != null && l.Target.name == "Log_" + SpoolData.Hero + "_" + SpoolData.Tin) tinPage = l.Target;
                }
            Check("journal: Spool has their own vessel page", own && being);
            Check("journal: Spool's page links the Cookie Tin's page", tinPage != null);
            if (tinPage != null)
                foreach (Logitems.Base b in AllItems(tinPage.Elements))
                {
                    if (b is Logitems.LinkUpgrade && ((Logitems.LinkUpgrade)b).Target != null && upgradePage == null) upgradePage = ((Logitems.LinkUpgrade)b).Target;
                    if (b is Logitems.LinkImage && ((Logitems.LinkImage)b).Target != null && unitPage == null) unitPage = ((Logitems.LinkImage)b).Target;
                }
            Check("journal: the Tin page links its upgrades", upgradePage != null && upgradePage.name.Contains("Spooltin"), upgradePage != null ? upgradePage.name : "none");
            Check("journal: the Tin page links the Crafted", unitPage != null && unitPage.name.StartsWith("Log_Mob_Spool"), unitPage != null ? unitPage.name : "none");
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
#if SPOOL_SHOTS
            Bl_Menus.Changemenu("Journal");
            List<Logobject> show = new List<Logobject> { page, tinPage, upgradePage, unitPage };
            foreach (Logobject shown in show)
            {
                if (shown == null) continue;
                Loghandler.Log_Current = shown;
                yield return Wait(2.5f);
                yield return SpoolShots.Shot("journal_" + shown.name);
            }
            Bl_Menus.Changemenu("Main");
            yield return Wait(1f);
#endif
        }

        Log("counts: dropped " + SpoolServer.Dropped + ", picked " + SpoolServer.Picked + ", delivered " + SpoolServer.Delivered + ", tufts shown " + SpoolClient.TuftsShown
            + ", flights " + SpoolClient.Flights + ", heads swapped " + SpoolServer.Swapped + ", came back " + SpoolServer.Returned + ", menu changes " + hero.MenuChanges);
        freeze = false;
        Log("DONE pass=" + pass + " fail=" + fail + " setup=" + ModKitTest.Setups);
        phase = 9;
    }
}
#endif
