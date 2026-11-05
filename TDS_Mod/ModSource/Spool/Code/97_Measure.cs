#if SPOOL_PLAY && SPOOL_MEASURE
// Debug build only (-Define SPOOL_PLAY,SPOOL_SHOTS,MODKIT_PLAY,MODKIT_TEST,SPOOL_MEASURE): the balance round's measures
// that take Spool's own buildings (docs/reports/BALANCE_2026-11-03.md section 9, AURA_BASELINE_2026-11-03.md section 7): M5 a line of Pin Cushions, M6 do
// Crafted hit walkers, M7 the Maul against the line, M10 a Loom and a Loom with its Seam.
public static class SpoolMeasure
{
    static readonly string[] Maps = { "3_Opal", "3B_Woozy", "3B_Woozy" };
    static Entity_Master hm;
    static long owner;
    static int team;
    static readonly List<SpoolBuilding> known = new List<SpoolBuilding>();
    static readonly List<Vector3> used = new List<Vector3>();

    static void Log(string m) { SpoolLog.Info("PLAY " + m); }
    static string F(float v) { return ModKitMeasure.F(v); }
    static string Vec(Vector3 v) { return "{\"x\":" + F(v.x) + ",\"y\":" + F(v.y) + ",\"z\":" + F(v.z) + "}"; }

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

    static Beingobject Being(string id) { Beingobject b; return SpoolContent.Beings.TryGetValue(id, out b) ? b : null; }

    static IEnumerator Build(string tower, Vector3 at, SpoolBuilding[] result)
    {
        result[0] = null;
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
            foreach (SpoolBuilding b in SpoolServer.Buildings)
                if (b != null && b.Started && b.Def.Id == tower && !known.Contains(b)) { result[0] = b; break; }
            t += Time.deltaTime;
            yield return null;
        }
        if (result[0] != null) known.Add(result[0]);
    }

    static void Remove(Entity_Master e) { if (e != null && e.Beingserver != null && !e.Beingserver.Dead) Game_Entity.Instance.Despawn(e.Beingserver, false, "Measure"); }

    static bool Give(SpoolBuilding b, string key)
    {
        if (b.Def.Upgrades == null) return false;
        foreach (SpoolData.Upg u in b.Def.Upgrades)
            if (u.Key == key)
            {
                Upgradeproperties up = Bl_Address.Getload("Upgrade", u.Id) as Upgradeproperties;
                if (up == null) return false;
                Helper_Entitytools.Upgrade(b.Master, up, Helper_Entitytools.Upgradecheck.Forced, null);
                return true;
            }
        return false;
    }

    static IEnumerator AimTin(SpoolTinBrain tin, Vector3 spot)
    {
        tin.Master.GetComponent<Entity_Master_Remote>().Command_ServerRpc(SpoolData.TargetLink, "{\"Position\":" + Vec(spot) + "}", default(Unity.Netcode.ServerRpcParams));
        float t = 0f;
        while (t < 5f && !tin.HasSpot) { t += Time.deltaTime; yield return null; }
        Log("the Tin's spot: " + (tin.HasSpot ? ModKitPlay.T(tin.Spot) : "refused, '" + tin.LastTarget + "'"));
    }

    static Entity_Master RedNode()
    {
        foreach (Entity_Master n in ModKitMeasure.Nodes()) if (ModKitPlay.Name(n).Contains("Red")) return n;
        return ModKitMeasure.Nodes().Count > 0 ? ModKitMeasure.Nodes()[0] : null;
    }

    static IEnumerator Measure()
    {
        string plan = ModKitPlay.CurrentPlan;
        hm = ModKitPlay.LocalHero();
        owner = ModKitUtil.Owner(hm);
        team = ModKitUtil.Team(hm);
        known.Clear();
        used.Clear();
        ModKitPlay.Reset(hm);
        ModKitPlay.ResetMap();
        ModKitPlay.Init();
        ModKitPlay.PlanLanes();
        yield return ModKitPlay.WaitForWorld(30f);
        ModKitMeasure.Give(5000f, owner);
        ModKitPlay.Beat();
        List<Entity_Master> cores = ModKitPlay.Cores("Core", true);
        Vector3 core = cores.Count > 0 ? ModKitPlay.Pos(cores[0]) : ModKitUtil.Pos(hm);
        // The Stash first: the Crafted draw from it.
        Vector3 stash = ModKitTest.Spot(core, Being(SpoolData.Stash), team, 2f, 6f, used, "Stash");
        SpoolBuilding[] got = new SpoolBuilding[1];
        yield return Build(SpoolData.Stash, stash, got);
        // The full bar opens a moment after the Stash stands: a build sent at once is refused (seen on the PC).
        for (float wait = 0f; (SpoolServer.Player(owner).Home == null || wait < 3f) && wait < 15f; wait += Time.deltaTime) { ModKitPlay.Beat(); yield return null; }
        SpoolServer.Player(owner).Stash = SpoolServer.Player(owner).Cap;

        if (plan == "3_Opal" || ModKitPlay.PlanIndex == 2)
        {
            // M5: three Pin Cushions across the last stretch, nothing else on the lane. The game refuses a tile that would shut the
            // foes' only way (seen on the PC): the next one along the line is tried.
            List<Vector3> line = ModKitMeasure.LineAcross(Being(SpoolData.Cushion), team, 5, 7, used);
            Log("M5 Pin Cushion candidates on " + plan + ": " + string.Join(" ", line.ConvertAll(ModKitPlay.T).ToArray()));
            List<Entity_Master> walls = new List<Entity_Master>();
            foreach (Vector3 at in line)
            {
                if (walls.Count >= 3) break;
                yield return Build(SpoolData.Cushion, at, got);
                Log("M5 a Pin Cushion at " + ModKitPlay.T(at) + ": " + (got[0] != null ? "stands" : "refused"));
                if (got[0] != null) walls.Add(got[0].Master);
            }
            if (walls.Count > 0)
                yield return ModKitMeasure.WatchLine("Pin Cushions on " + plan, walls, core, team, 3);
            yield break;
        }

        // M10: a Loom beside a node, then with its Seam.
        Entity_Master red = RedNode();
        if (red != null)
        {
            Vector3 at = ModKitTest.Spot(ModKitPlay.Pos(red), Being(SpoolData.Loom), team, 1f, 3f, used, "Loom");
            yield return Build(SpoolData.Loom, at, got);
            SpoolLoomBrain loom = got[0] as SpoolLoomBrain;
            if (loom == null) Log("M10 no Loom at " + ModKitPlay.T(at) + ", beside " + ModKitPlay.Name(red) + " at " + ModKitPlay.T(ModKitPlay.Pos(red)) + ", wallet " + ModKitPlay.Wallet() + ", vessel at " + ModKitPlay.T(ModKitPlay.Pos(hm)));
            if (loom != null)
            {
                float[] before = ModKitMeasure.Purse();
                int strikes = loom.Strikes, spun = loom.Spun;
                yield return ModKitMeasure.Window(60f, team, null);
                Log("M10 a Loom at " + ModKitPlay.T(at) + " beside " + ModKitPlay.Name(red) + ": in 60 game s, strikes " + (loom.Strikes - strikes) + ", Thread spun " + (loom.Spun - spun) + ", purse " + ModKitMeasure.Gained(before));
                bool seam = Give(loom, "Seam");
                yield return new WaitForSeconds(0.5f);
                before = ModKitMeasure.Purse();
                strikes = loom.Strikes;
                int seams = loom.Seams;
                yield return ModKitMeasure.Window(60f, team, null);
                Log("M10 with its Seam (" + seam + "): in 60 game s, strikes " + (loom.Strikes - strikes) + ", Seams " + (loom.Seams - seams) + ", purse " + ModKitMeasure.Gained(before));
                Remove(loom.Master);
            }
        }

        // M6: a Tin's Stitched on the open lane, in real Tides: how much damage lands on foes walking, and on foes standing.
        yield return ModKitMeasure.NextTide(team);
        Vector3 spotOpen = ModKitPlay.LaneTile(0, 7);
        Vector3 tinAt = ModKitTest.Spot(spotOpen, Being(SpoolData.Tin), team, 2f, 6f, used, "Tin");
        yield return Build(SpoolData.Tin, tinAt, got);
        SpoolTinBrain tin = got[0] as SpoolTinBrain;
        if (tin != null)
        {
            yield return AimTin(tin, spotOpen);
            yield return new WaitForSeconds(6f);
            SpoolServer.Player(owner).Stash = SpoolServer.Player(owner).Cap;
            Dictionary<Entity_Master, Vector3> last = new Dictionary<Entity_Master, Vector3>();
            Dictionary<Entity_Master, float> hurt = new Dictionary<Entity_Master, float>();
            float walking = 0f, standing = 0f, t = 0f;
            while (t < 90f && Game_Mapvars.Instance != null && Game_Mapvars.Instance.Gamecomplete.Value.ToString() == "Running")
            {
                foreach (Entity_Master e in ModKitPlay.Foes())
                {
                    Vector3 at = ModKitPlay.Pos(e), was;
                    float taken = e.Damagetaken, prior;
                    bool moved = last.TryGetValue(e, out was) && ModKitPlay.Flat(was, at) > 0.1f * Time.deltaTime;
                    if (hurt.TryGetValue(e, out prior) && taken > prior) { if (moved) walking += taken - prior; else standing += taken - prior; }
                    hurt[e] = taken;
                    last[e] = at;
                }
                t += Time.deltaTime;
                ModKitPlay.Beat();
                yield return null;
            }
            float all = Mathf.Max(0.01f, walking + standing);
            Log("M6 the Crafted's damage in 90 s on the open lane: on walking foes " + F(walking) + " (" + F(100f * walking / all) + "%), on standing foes " + F(standing) + "; Tin alive " + tin.Alive + "/" + tin.Slots);
            Remove(tin.Master);
        }

        // M7: one Maul against three Cushions with 8 Stitched and 4 Darts behind them.
        ModKitMeasure.ClearFoes(team);
        Game_Mapvars v = Game_Mapvars.Instance;
        if (v == null || v.Gamecomplete.Value.ToString() != "Running") { Log("M7: the map is over"); yield break; }
        v.Time_Wave.Value = 900f;
        List<Vector3> wallTiles = ModKitMeasure.LineAcross(Being(SpoolData.Cushion), team, 5, 3, used);
        List<Entity_Master> line2 = new List<Entity_Master>();
        foreach (Vector3 at in wallTiles)
        {
            yield return Build(SpoolData.Cushion, at, got);
            if (got[0] != null) line2.Add(got[0].Master);
        }
        Vector3 behind = ModKitPlay.LaneTile(0, 3);
        List<SpoolTinBrain> tins = new List<SpoolTinBrain>();
        for (int i = 0; i < 3; i++)
        {
            Vector3 place = ModKitTest.Spot(behind, Being(SpoolData.Tin), team, 2f, 7f, used, "Tin");
            yield return Build(SpoolData.Tin, place, got);
            SpoolTinBrain tn = got[0] as SpoolTinBrain;
            if (tn == null) continue;
            tins.Add(tn);
            if (i == 2) { Give(tn, "Woolen"); Give(tn, "Design"); }
            yield return AimTin(tn, behind);
        }
        SpoolPlayer me = SpoolServer.Player(owner);
        for (float t = 0f; t < 40f; t += Time.deltaTime)
        {
            me.Stash = me.Cap;
            v.Time_Wave.Value = 900f;
            ModKitMeasure.ClearFoes(team);
            ModKitPlay.Beat();
            yield return null;
        }
        Entity_Master spawner = null;
        foreach (Entity_Master e in ModKitPlay.Cores("Enemycore", false)) spawner = e;
        Entity_Master maul = spawner != null ? ModKitTest.SpawnFoe("Enemy_Maul", ModKitPlay.LaneTile(0, 9), 0f, 3f, null) : null;
        if (maul == null) { Log("M7: no Maul"); yield break; }
        for (float t = 0f, next = 0f; t < 90f && ModKitPlay.Alive(maul); t += Time.deltaTime)
        {
            v.Time_Wave.Value = 900f;
            ModKitPlay.Beat();
            if (t >= next)
            {
                next = t + 3f;
                int walls = 0; string hp = "";
                foreach (Entity_Master w in line2) if (ModKitPlay.Alive(w)) { walls++; hp += " " + F(ModKitUtil.Health(w)); }
                int stitched = 0, darts = 0;
                foreach (SpoolUnitBrain u in SpoolServer.Units)
                    if (u != null && u.Master != null && ModKitUtil.Alive(u.Master) && u.Owner == owner) { if (u.Def.Id == SpoolData.Dart) darts++; else stitched++; }
                Log("M7 t " + F(t) + ": Maul " + F(ModKitUtil.Health(maul)) + "/" + F(ModKitUtil.MaxHealth(maul)) + " at " + F(ModKitPlay.Flat(ModKitPlay.Pos(maul), behind)) + " from the Crafted's spot; Cushions " + walls + hp + "; Stitched " + stitched + ", Darts " + darts);
            }
            yield return null;
        }
        Log("M7 result: Maul " + (ModKitPlay.Alive(maul) ? "alive " + F(ModKitUtil.Health(maul)) : "dead"));
    }
}
#endif