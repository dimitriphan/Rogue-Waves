#if YOSSELE_PLAY && YOSSELE_MEASURE
// Debug build only (-Define YOSSELE_PLAY,YOSSELE_SHOTS,MODKIT_PLAY,MODKIT_TEST,YOSSELE_MEASURE): the balance round's measures that take a
// Sapling or a stock gatherer (docs/reports/BALANCE_2026-11-03.md section 9, AURA_BASELINE_2026-11-03.md section 7): M1 the start wallet with no
// Cardinals, M2-M4 a Sapling beside each node, M5's line of Stonehenges, M8 a census of every node, M9 stock gatherers timed, M10 a Sapling new
// and grown, M11 The Hunt's beasts. Windows hold the Tide clock and clear the foes, at the game's own speed-up. Spends and pays for real, on
// aura given at the start; each window reports only what its gatherer brought in.
public static class YosseleMeasure
{
    static readonly string[] Maps = { "3_Opal" };
    static Entity_Master hm;
    static long owner;
    static int team;
    static readonly List<Vector3> used = new List<Vector3>();
    static readonly List<YosseleBuilding> known = new List<YosseleBuilding>();

    static void Log(string m) { YosseleLog.Info("PLAY " + m); }
    static string F(float v) { return ModKitMeasure.F(v); }

    public static void Setup()
    {
        ModKitPlay.Plans = Maps;
        ModKitPlay.Scenario = Measure;
        ModKitPlay.ReturnAtEnd = true;
        ModKitMeasure.Log = Log;
        ModKitTest.Log = m => Log(m);
        ModKitPlay.AtBoat = ModKitMeasure.EmptyLoadout;
        ModKitPlay.AtEnd = ModKitMeasure.RestoreLoadout;
    }

    static string Vec(Vector3 v) { return "{\"x\":" + F(v.x) + ",\"y\":" + F(v.y) + ",\"z\":" + F(v.z) + "}"; }

    static IEnumerator Build(string tower, Vector3 at, YosseleBuilding[] result)
    {
        result[0] = null;
        // A vessel that fell comes back as a new being.
        for (float wait = 0f; (hm == null || !ModKitPlay.Alive(hm)) && wait < 40f; wait += Time.deltaTime)
        {
            hm = ModKitPlay.LocalHero();
            ModKitPlay.Beat();
            yield return null;
        }
        if (hm == null) { Log("no vessel to build with"); yield break; }
        hm.GetComponent<Entity_Master_Remote>().Command_ServerRpc("Build", "{\"Tower\":\"" + tower + "\",\"Position\":" + Vec(at) + ",\"Orientation\":0,\"Owner\":" + owner + "}", default(Unity.Netcode.ServerRpcParams));
        float t = 0f;
        while (t < 12f && result[0] == null)
        {
            foreach (YosseleBuilding b in YosseleServer.Buildings)
                if (b != null && b.Started && b.Def.Id == tower && !known.Contains(b)) { result[0] = b; break; }
            t += Time.deltaTime;
            yield return null;
        }
        if (result[0] != null) known.Add(result[0]);
    }

    static void Remove(Entity_Master e) { if (e != null && e.Beingserver != null && !e.Beingserver.Dead) Game_Entity.Instance.Despawn(e.Beingserver, false, "Measure"); }

    static Beingobject Tower(string id) { Beingobject b; return YosseleContent.Beings.TryGetValue(id, out b) ? b : null; }

    // One Sapling beside this spot, for a window: strikes, payouts, and what the purse gained.
    static IEnumerator SaplingAt(string label, Vector3 at, float seconds, int growth)
    {
        YosseleBuilding[] got = new YosseleBuilding[1];
        yield return Build(YosseleData.Pachira, at, got);
        YosselePachiraBrain s = got[0] as YosselePachiraBrain;
        if (s == null) { Log(label + ": no Sapling at " + ModKitPlay.T(at)); yield break; }
        if (growth > 0) s.SetGrowth(growth);
        yield return new WaitForSeconds(0.5f);
        float[] before = ModKitMeasure.Purse();
        int strikes = s.Strikes, payouts = s.Payouts, fruits = s.Fruits;
        yield return ModKitMeasure.Window(seconds, team, null);
        Log(label + " at " + ModKitPlay.T(at) + ", Growth " + s.Growth + ", nodes in reach " + s.Nodes + ": in " + F(seconds) + " game s, strikes " + (s.Strikes - strikes) + ", payouts " + (s.Payouts - payouts) + ", fruits " + (s.Fruits - fruits) + ", purse " + ModKitMeasure.Gained(before));
        Remove(s.Master);
        yield return null;
    }

    static Vector3 Beside(Entity_Master node, Beingobject being)
    {
        Vector3 at = ModKitTest.Spot(ModKitPlay.Pos(node), being, team, 1f, 3f, new List<Vector3>(), "beside " + ModKitPlay.Name(node));
        return at;
    }

    static IEnumerator Load(string hero)
    {
        Heroentryobject e = Tds_Masterdatabase.Instance.Items["Hero"].ContainsKey(hero) ? Tds_Masterdatabase.Instance.Items["Hero"][hero] as Heroentryobject : null;
        if (e == null || e.Heroloads == null) yield break;
        Bl_Address.Loadgroup g = Bl_Address.Load(e.Heroloads.Objectname);
        float t = 0f;
        while (g != null && !g.Loaded && t < 30f) { t += Time.unscaledDeltaTime; ModKitPlay.Beat(); yield return null; }
    }

    // M9: a stock gatherer alone beside a node.
    static IEnumerator Stock(string being, string hero, Entity_Master node, float seconds)
    {
        yield return Load(hero);
        Beingobject b = null;
        for (float wait = 0f; b == null && wait < 10f; wait += Time.unscaledDeltaTime)
        {
            b = ModKitPlay.Mob(being);
            ModKitPlay.Beat();
            yield return null;
        }
        if (b == null) { Log("M9 " + being + ": not loaded (" + hero + ")"); yield break; }
        Vector3 at = ModKitTest.Spot(ModKitPlay.Pos(node), b, team, 1f, 4f, new List<Vector3>(), being);
        if (float.IsNaN(at.x)) { Log("M9 " + being + ": no tile by " + ModKitPlay.Name(node)); yield break; }
        Entity_Master made = null;
        try { made = ModKitMeasure.SpawnFor(hm, b, at); }
        catch (Exception e) { Log("M9 " + being + ": " + e.Message); }
        if (made == null) yield break;
        yield return new WaitForSeconds(1f);
        float[] before = ModKitMeasure.Purse();
        yield return ModKitMeasure.Window(seconds, team, null);
        Log("M9 " + being + " beside " + ModKitPlay.Name(node) + " at " + ModKitPlay.T(at) + ": in " + F(seconds) + " game s, purse " + ModKitMeasure.Gained(before));
        Remove(made);
        yield return null;
    }

    static Entity_Master NodeNamed(string part)
    {
        foreach (Entity_Master n in ModKitMeasure.Nodes())
            if (ModKitPlay.Name(n).Contains(part)) return n;
        return null;
    }

    static IEnumerator Measure()
    {
        string plan = ModKitPlay.CurrentPlan;
        hm = ModKitPlay.LocalHero();
        owner = YosseleUtil.Owner(hm);
        team = YosseleUtil.Team(hm);
        used.Clear();
        known.Clear();
        ModKitPlay.Reset(hm);
        ModKitPlay.ResetMap();
        ModKitPlay.Init();
        ModKitPlay.PlanLanes();
        Log("M1 " + plan + " at the map's start: wallet " + ModKitPlay.Wallet() + ", lobby Cardinals " + Tds_Multiplayerinfo.Instance.Artifacts.Count + ", tide " + Game_Mapvars.Instance.Wave.Value);
        yield return ModKitPlay.WaitForWorld(30f);
        ModKitMeasure.Census(plan);
        Log("M1 " + plan + " before any build: wallet " + ModKitPlay.Wallet());
        ModKitPlay.Beat();
        if (plan != "3_Opal" && plan != "3B_Woozy" && plan != "3B_Thehunt")
            yield break;
        ModKitMeasure.Give(5000f, owner);
        Game_Mapvars v = Game_Mapvars.Instance;
        Beingobject sapling = Tower(YosseleData.Pachira);

        if (plan == "3_Opal")
        {
            // M2: a Sapling beside the Opal, four Tides.
            Entity_Master opal = null;   // measured: only M5 is left to this launch
            if (opal != null)
            {
                Vector3 at = Beside(opal, sapling);
                YosseleBuilding[] got = new YosseleBuilding[1];
                yield return Build(YosseleData.Pachira, at, got);
                YosselePachiraBrain s = got[0] as YosselePachiraBrain;
                for (int tide = 1; tide <= 4 && s != null; tide++)
                {
                    yield return ModKitMeasure.NextTide(team);
                    float[] before = ModKitMeasure.Purse();
                    int strikes = s.Strikes, payouts = s.Payouts;
                    yield return ModKitMeasure.Window(45f, team, null);
                    Log("M2 the Opal in Tide " + v.Wave.Value + ": 45 game s, strikes " + (s.Strikes - strikes) + ", payouts " + (s.Payouts - payouts) + ", purse " + ModKitMeasure.Gained(before));
                }
                if (s != null) Remove(s.Master);
            }
            // M5: three Stonehenges across the last stretch, nothing else.
            List<Vector3> line = ModKitMeasure.LineAcross(Tower(YosseleData.Henge), team, 5, 7, used);
            Log("M5 Stonehenge candidates: " + string.Join(" ", line.ConvertAll(ModKitPlay.T).ToArray()) + "; lanes " + ModKitPlay.Lanes.Count + ", tile 5 back " + ModKitPlay.T(ModKitPlay.LaneTile(0, 5)));
            List<Entity_Master> walls = new List<Entity_Master>();
            foreach (Vector3 at in line)
            {
                if (walls.Count >= 3) break;
                YosseleBuilding[] got = new YosseleBuilding[1];
                yield return Build(YosseleData.Henge, at, got);
                Log("M5 a Stonehenge at " + ModKitPlay.T(at) + ": " + (got[0] != null ? "stands" : "refused"));
                if (got[0] != null) walls.Add(got[0].Master);
            }
            List<Entity_Master> cores = ModKitPlay.Cores("Core", true);
            if (walls.Count > 0 && cores.Count > 0)
                yield return ModKitMeasure.WatchLine("Stonehenges", walls, ModKitPlay.Pos(cores[0]), team, 3);
        }
        else
        {
            // M3: each node, by itself; only as many as fit.
            int n = 0;
            foreach (Entity_Master node in new List<Entity_Master>(ModKitMeasure.Nodes()))
            {
                if (!ModKitPlay.Name(node).StartsWith("Resource_") || n++ >= 6) continue;
                yield return SaplingAt("M3 " + plan + " beside " + ModKitPlay.Name(node), Beside(node, sapling), 40f, 0);
            }
            if (plan == "3B_Thehunt")
            {
                // M4: the tile the 2026-10-26 Sapling stood on, and one tile nearer the Orange node.
                yield return SaplingAt("M4 the old tile", new Vector3(-7f, 7f, -27f), 40f, 0);
                Entity_Master orange = NodeNamed("Orange");
                if (orange != null)
                {
                    Vector3 toward = Vector3.MoveTowards(new Vector3(-7f, 7f, -27f), ModKitPlay.Pos(orange), 1f);
                    yield return SaplingAt("M4 a tile nearer " + ModKitPlay.T(ModKitPlay.Pos(orange)), toward, 40f, 0);
                }
                // M11: a beast of each size, killed; the wallet before and after.
                foreach (string beast in new[] { "Mob_Aurabeast", "Mob_Largeaurabeast" })
                {
                    float[] before = ModKitMeasure.Purse();
                    Entity_Master b = null;
                    try { b = ModKitTest.SpawnFoe(beast, YosseleUtil.Pos(hm), 2f, 8f, null); }
                    catch (Exception e) { Log("M11 " + beast + ": " + e.Message); }
                    if (b == null) { Log("M11 " + beast + ": could not spawn it"); continue; }
                    yield return new WaitForSeconds(1f);
                    float hp = YosseleUtil.MaxHealth(b);
                    if (b.Beingserver != null && !b.Beingserver.Dead) Game_Entity.Instance.Despawn(b.Beingserver, true, "Measure");
                    yield return new WaitForSeconds(5f);
                    Log("M11 " + beast + " (health " + F(hp) + ") killed: purse " + ModKitMeasure.Gained(before));
                }
            }
            if (plan == "3B_Woozy")
            {
                Entity_Master red = NodeNamed("Resource_Red") ?? NodeNamed("Red");
                if (red != null)
                {
                    // M10: a Sapling new and grown; M9: the stock gatherers.
                    yield return SaplingAt("M10 a new Sapling", Beside(red, sapling), 60f, 0);
                    yield return SaplingAt("M10 a grown Sapling", Beside(red, sapling), 60f, 99);
                    yield return Stock("Tower_Smackyhut", "Hero_Smacky", red, 90f);
                    yield return Stock("Tower_Snosnobox", "Hero_Snosno", red, 90f);
                    yield return Stock("Tower_Fiesilo", "Hero_Fie", red, 90f);
                    yield return Stock("Tower_Bustercheckpoint", "Hero_Buster", red, 90f);
                }
                else Log("M9 no red node on " + plan);
            }
        }
    }
}
#endif