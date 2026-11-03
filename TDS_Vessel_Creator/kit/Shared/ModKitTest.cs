#if MODKIT_TEST
// Kit 1.5 (2026-10-05). Versions and compatibility rules: TDS_Vessel_Creator\kit\README.md
// Shared vessel-mod kit: self-test helpers. Compiled only into self-test builds: the build scripts define MODKIT_TEST
// whenever a <NAME>_SELFTEST define is set, so release builds carry none of it.
// Written after Argo 1.4.0 needed nine PC rounds, seven of them for the test's own placement on ground nobody could see:
// - PickNode: every self-test plays the same map (TestPlan), so the ground is the same in every PC round, whatever map
//   the player picked last. Then the last-picked map, then the first open one.
// - MapDump: the whole map's ground, logged once per run as "TEST MAP" lines. TDS_Mod/Tools/cloud-check/map_view.py
//   draws it (kit reference/maps/), so a test writer sees the hills and lanes before the PC round.
// - Spot / FoeTile / SpawnFoe: a free building tile or a standable foe tile on every level of each column, nearest
//   first; when there is none, the reason and a Picture of the ground around it are logged, so one round shows it all.
// - Setup: a third outcome beside PASS and FAIL, for a check whose scene the test couldn't arrange (no tile, no lane,
//   the foe died first). Logged "TEST SETUP name: why", counted apart; it says nothing about the mod.
public static class ModKitTest
{
    // The map every self-test plays (a Gameplan's Objectname). The PC's save must have it open; Thehunt is.
    public const string TestPlan = "3B_Thehunt";

    // The vessel's test logger (it adds the "[<Name>] TEST " prefix). Set it before calling anything else.
    public static Action<string> Log = delegate { };
    public static int Setups;

    public static void Setup(string name, string why)
    {
        Setups++;
        Log("SETUP " + name + ": " + why);
    }

    // ---------------------------------------------------------------- the map

    // The world-map node to start: TestPlan's, else the plan the player picked last, else the first open one.
    public static string PickNode(out string why)
    {
        string last = Tds_Multiplayerinfo.Instance != null ? Tds_Multiplayerinfo.Instance.Gameplan.Value.ToString() : "";
        string pinned = "", lastNode = "", open = "";
        foreach (Macro_Maplink_Add link in UnityEngine.Object.FindObjectsOfType<Macro_Maplink_Add>())
        {
            if (link.Gameplan == null)
                continue;
            string plan = link.Gameplan.Objectname;
            if (plan == TestPlan && pinned == "") pinned = link.gameObject.name;
            if (plan == last && lastNode == "") lastNode = link.gameObject.name;
            if (open == "" && link.Unlockcondition == null) open = link.gameObject.name;
        }
        why = pinned != "" ? "the test map " + TestPlan : lastNode != "" ? TestPlan + " not found, last picked " + last : TestPlan + " not found, first open map";
        return pinned != "" ? pinned : lastNode != "" ? lastNode : open;
    }

    // Logs every ground tile of the map once: "MAP x,y,z<flag> ..." (40 per line), flag 'b' a 1x1 building can stand there
    // and so can a foe, 'B' a building only, 'f' a foe only, '-' neither (as the game answers for Smacky's Archer Tower
    // and a Dagger). Then the core, the vessel and everything else of the vessel's team with its tile. Call it once, at
    // the start of the scenario, before the test builds anything.
    public static void MapDump(int team, Entity_Master hero)
    {
        Beingobject tower = Mob("Tower_Smackyarcher"), dagger = Mob("Enemy_Dagger");
        Entity_Master core = Core();
        Link_Being probe = dagger != null ? new Link_Being { Being = dagger, Entity = dagger.Entity, Block = dagger.Block, Team = core != null ? core.Entitydata.Value.Team : 2 } : null;
        List<Entity_Master> surfaces;
        string source = "server tag list";
        if (Game_Entity.Instance == null)
            return;
        if (!Game_Entity.Instance.Entitiesserver.TryGetValue("Surface", out surfaces) || surfaces == null || surfaces.Count == 0)
        {
            source = "client list";
            surfaces = new List<Entity_Master>();
            foreach (Entity_Master e in Game_Entity.Instance.Entityclient)
                if (e != null && e.Taglistserver != null && e.Taglistserver.Contains("Surface"))
                    surfaces.Add(e);
        }
        List<string> cells = new List<string>();
        List<Vector3> seen = new List<Vector3>();
        int place = 0, stand = 0;
        foreach (Entity_Master s in surfaces)
        {
            Genitem g = s != null && s.Beingserver != null ? s.Beingserver.Currentblock : null;
            if (g == null || seen.Contains(g.Position))
                continue;
            seen.Add(g.Position);
            bool p = false, f = false;
            try { p = tower != null && Helper_Gridtools.Checkifplaceable(g, tower, 0, team); } catch (Exception) { }
            try { f = probe != null && Helper_Gridtools.Checkifstandable(g, probe) != null; } catch (Exception) { }
            if (p) place++;
            if (f) stand++;
            cells.Add(Tile(g.Position) + (p && f ? "b" : p ? "B" : f ? "f" : "-"));
        }
        string plan = Tds_Multiplayerinfo.Instance != null ? Tds_Multiplayerinfo.Instance.Gameplan.Value.ToString() : "?";
        Log("MAP plan " + plan + ": " + cells.Count + " ground tiles (" + source + "), " + place + " take a building, " + stand + " take a foe"
            + (tower == null ? ", no Archer Tower to ask" : "") + (dagger == null ? ", no Dagger to ask" : ""));
        for (int i = 0; i < cells.Count; i += 40)
            Log("MAP " + string.Join(" ", cells.GetRange(i, Math.Min(40, cells.Count - i)).ToArray()));
        if (core != null) Log("MAP core " + Tile(core.Entityposition.Value));
        if (hero != null) Log("MAP vessel " + Tile(hero.Entityposition.Value));
        List<Entity_Master> mine;
        if (Game_Entity.Instance.Entitiesserverteam.TryGetValue(team, out mine) && mine != null)
            foreach (Entity_Master e in mine)
                if (e != null && e != hero && e.Beingserver != null && e.Beingserver.Being != null && e.Beingserver.Currentblock != null && e.Taglistserver != null && !e.Taglistserver.Contains("Surface"))
                    Log("MAP own " + e.Beingserver.Being.Objectname + " " + Tile(e.Beingserver.Currentblock.Position));
        Log("MAP end");
    }

    // ---------------------------------------------------------------- ground

    // The grid block under a world position (the build flow uses the nearest "Surface" entity the same way).
    public static Genitem Surface(Vector3 position)
    {
        Link_Entityquery query = new Link_Entityquery();
        query.Tags = new List<string> { "Surface" };
        query.Style = Link_Entityquery.Styles.Tag;
        Entity_Master surface = Helper_Entityquerytools.Entitynearest(position, 0.75f, query);
        return surface != null && surface.Beingserver != null ? surface.Beingserver.Currentblock : null;
    }

    // Every ground tile within maxDist (flat) of c, from 'up' levels above c to 'down' below, each column on every level.
    public static List<Genitem> Ground(Vector3 c, float maxDist, int up, int down)
    {
        int r = Mathf.CeilToInt(maxDist) + 1;
        Vector3 c0 = new Vector3(Mathf.Round(c.x), Mathf.Round(c.y), Mathf.Round(c.z));
        List<Genitem> tiles = new List<Genitem>();
        for (int dx = -r; dx <= r; dx++)
            for (int dz = -r; dz <= r; dz++)
            {
                if (Flat(c0 + new Vector3(dx, 0f, dz), c) > maxDist)
                    continue;
                for (int dy = up; dy >= -down; dy--)
                {
                    Genitem b = Surface(c0 + new Vector3(dx, dy, dz));
                    if (b != null && !tiles.Contains(b))
                        tiles.Add(b);
                }
            }
        return tiles;
    }

    // A free tile where 'being' can be built, about 'ring' tiles (flat) from c: first the ground within a level of c,
    // nearest the ring; then every level from 2 above c to 6 below, nearest c, out to maxDist. Tiles in 'used' are
    // skipped and the one found is added to it. None: logs why and a picture of the ground, returns NaN (float.IsNaN(at.x)).
    public static Vector3 Spot(Vector3 c, Beingobject being, int team, float ring, float maxDist, List<Vector3> used, string what)
    {
        List<Genitem> tiles = Ground(c, maxDist, 2, 6);
        List<Genitem> near = tiles.FindAll(b => Mathf.Abs(b.Position.y - c.y) <= 1.1f);
        near.Sort((a, b) => Mathf.Abs(Flat(a.Position, c) - ring).CompareTo(Mathf.Abs(Flat(b.Position, c) - ring)));
        tiles.Sort((a, b) => Flat(a.Position, c).CompareTo(Flat(b.Position, c)));
        near.AddRange(tiles);
        int taken = 0, unplaceable = 0;
        List<Genitem> tried = new List<Genitem>();
        foreach (Genitem b in near)
        {
            if (tried.Contains(b))
                continue;
            tried.Add(b);
            if (used.Contains(b.Position)) { taken++; continue; }
            bool ok = false;
            try { ok = Helper_Gridtools.Checkifplaceable(b, being, 0, team); } catch (Exception) { }
            if (!ok) { unplaceable++; continue; }
            used.Add(b.Position);
            return b.Position;
        }
        Log("no free tile for " + what + " within " + F(maxDist) + " of " + Tile(c) + ": " + tried.Count + " ground tiles, " + taken + " taken, " + unplaceable + " not placeable");
        Picture(c, Mathf.Min(10, Mathf.CeilToInt(maxDist)), used);
        return new Vector3(float.NaN, 0f, 0f);
    }

    public static Entity_Master Core()
    {
        if (Game_Entity.Instance == null)
            return null;
        foreach (Entity_Master e in Game_Entity.Instance.Entityclient)
            if (e != null && e.Taglistserver != null && e.Taglistserver.Contains("Enemycore") && e.Beingserver != null && !e.Beingserver.Dead)
                return e;
        return null;
    }

    // A tile for a foe of this being, between minDist and maxDist (flat) of 'near', within a level of it: the nearest one
    // the foe can stand on, else the nearest at all. Tiles in 'skip' (buildings) are left out.
    public static Genitem FoeTile(Beingobject being, Vector3 near, float minDist, float maxDist, List<Vector3> skip)
    {
        Entity_Master core = Core();
        Link_Being probe = new Link_Being { Being = being, Entity = being.Entity, Block = being.Block, Team = core != null ? core.Entitydata.Value.Team : 2 };
        Genitem best = null, any = null;
        float bestD = float.MaxValue, anyD = float.MaxValue;
        foreach (Genitem s in Ground(near, maxDist, 1, 1))
        {
            float d = Flat(s.Position, near);
            if (d < minDist || d > maxDist || (skip != null && skip.Contains(s.Position)))
                continue;
            Genitem st = null;
            try { st = Helper_Gridtools.Checkifstandable(s, probe); } catch (Exception) { }
            if (st != null && d < bestD) { best = s; bestD = d; }
            if (d < anyD) { any = s; anyD = d; }
        }
        return best ?? any;
    }

    // Spawns a foe as a child of the enemy core, like Tide foes (a parentless enemy throws every tick). Null, with the
    // reason and a picture of the ground logged, when it can't.
    public static Entity_Master SpawnFoe(string being, Vector3 near, float minDist, float maxDist, List<Vector3> skip)
    {
        Beingobject b = Mob(being);
        Entity_Master core = Core();
        Genitem block = b != null ? FoeTile(b, near, minDist, maxDist, skip) : null;
        if (b == null || core == null || block == null)
        {
            Log("cannot spawn " + being + " " + F(minDist) + "-" + F(maxDist) + " from " + Tile(near) + ": being " + (b != null) + ", core " + (core != null) + ", tile " + (block != null));
            if (b != null && core != null)
                Picture(near, Mathf.Min(10, Mathf.CeilToInt(maxDist)), skip);
            return null;
        }
        Link_Spawn spawn = Helper_Entitytools.Createlinkspawnparent(core.Beingserver);
        spawn.Being = b; spawn.Genadd = block; spawn.Skin = ""; spawn.Spawnstyle = Link_Spawn.Spawnstyles.Forced;
        Link_Being lb = Helper_Entitytools.Spawn(spawn, block.Position, core.Beingserver, true, null);
        return lb != null ? lb.Worldobject : null;
    }

    // Logs the ground around c as text, north (+z) up: one character per column for its top ground tile within 4 levels
    // above c and 8 below: '0' at c's level, '1'-'9' that many above, 'a'-'h' 1-8 below, '#' a tile in 'used', ' ' no
    // ground, '@' c's own column.
    public static void Picture(Vector3 c, int r, List<Vector3> used)
    {
        Vector3 c0 = new Vector3(Mathf.Round(c.x), Mathf.Round(c.y), Mathf.Round(c.z));
        Log("ground around " + Tile(c0) + ", rows z " + (c0.z + r) + " down to " + (c0.z - r) + ", columns x " + (c0.x - r) + " to " + (c0.x + r)
            + ": 0 same level, 1-9 above, a-h below, # taken, blank no ground");
        for (int dz = r; dz >= -r; dz--)
        {
            char[] row = new char[2 * r + 1];
            for (int dx = -r; dx <= r; dx++)
            {
                char ch = ' ';
                for (int dy = 4; dy >= -8; dy--)
                {
                    Genitem b = Surface(c0 + new Vector3(dx, dy, dz));
                    if (b == null)
                        continue;
                    int h = Mathf.RoundToInt(b.Position.y - c0.y);
                    ch = used != null && used.Contains(b.Position) ? '#' : h >= 0 ? (char)('0' + Mathf.Min(9, h)) : (char)('a' + Mathf.Min(7, -h - 1));
                    break;
                }
                if (dx == 0 && dz == 0) ch = '@';
                row[dx + r] = ch;
            }
            Log("| " + new string(row));
        }
    }

    // ---------------------------------------------------------------- small helpers

    public static float Flat(Vector3 a, Vector3 b) { return new Vector2(a.x - b.x, a.z - b.z).magnitude; }

    static string F(float v) { return v.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture); }

    static string Tile(Vector3 p) { return Mathf.RoundToInt(p.x) + "," + Mathf.RoundToInt(p.y) + "," + Mathf.RoundToInt(p.z); }

    static Beingobject Mob(string name)
    {
        List<Bl_Address.Loadlist> lists;
        if (!Bl_Address.Groups.TryGetValue("Mob", out lists))
            return null;
        foreach (Bl_Address.Loadlist list in lists)
        {
            UnityEngine.Object item;
            if (list.Items.TryGetValue(name, out item) && item is Beingobject)
                return (Beingobject)item;
        }
        return null;
    }
}
#endif
