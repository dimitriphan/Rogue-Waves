// 2.0 Deep Water, host only. A Deep Water (the pump) and every pool FLOOD added next to it, or next to one of its pools,
// form a network: tiles that touch (no diagonals) on the same team act as one. Every WaterTick the networks are rebuilt
// from the standing water and each one works the foes wading in it, with the upgrades of its lead pump (the one with the
// most upgrades when two players' water joins; pools whose pump was sold keep the base slow):
//   slow (Slow stat, Undercurrent), Brine's damage over time (Salt Sting: more on pinned foes), Kelp Bed's catch on
//   entering, Crushing Depth's extra damage taken (ArgoHooks.Soak), Trench's drag-under and its free pool each Tide.
// A pin on a foe in the water lasts twice as long (ArgoUtil.Pin reads ArgoStatus.IsWet), and a Drill Rig's Tremor that
// touches any tile of a network shakes all of it (ArgoTowerBrain.TremorFoes reads Shaken).
public static class ArgoWater
{
    public class Net
    {
        public int Team;
        public ArgoTowerBrain Lead;
        public readonly List<ArgoTowerBrain> Parts = new List<ArgoTowerBrain>();
        public readonly List<Vector3> Tiles = new List<Vector3>();
        public readonly List<Entity_Master> Foes = new List<Entity_Master>();

        // Any of its tiles within the circle (a tile counts when its centre is within half a tile of the edge).
        public bool Touches(Vector3 center, float radius)
        {
            foreach (ArgoTowerBrain p in Parts)
                if (p != null && p.Master != null && ArgoUtil.Flat(ArgoUtil.Pos(p.Master), center) <= radius + 0.5f)
                    return true;
            return false;
        }

        // A point (an entity position) standing in this water.
        public bool Holds(Vector3 at)
        {
            foreach (ArgoTowerBrain p in Parts)
                if (p != null && p.Master != null && InPool(ArgoUtil.Pos(p.Master), at))
                    return true;
            return false;
        }
    }

    public static readonly List<Net> Nets = new List<Net>();
    static readonly Dictionary<Entity_Master, float> lastWet = new Dictionary<Entity_Master, float>();
    static readonly Dictionary<Entity_Master, float> wetVfx = new Dictionary<Entity_Master, float>();
    static float nextTick;
    public static int Ticks, Catches, Drags, Brined, Soaked, Floods, Freepools, Refused;

    public static void Reset()
    {
        Nets.Clear();
        lastWet.Clear();
        wetVfx.Clear();
    }

    static bool InPool(Vector3 pool, Vector3 at)
    {
        return ArgoUtil.Flat(pool, at) <= ArgoData.PoolReach && Mathf.Abs(pool.y - at.y) < 1.2f;
    }

    // The grid tile a water entity stands on (FLOOD compares tiles in the same frame as the surface the player picks).
    public static Vector3 Tile(ArgoTowerBrain part)
    {
        Link_Being b = part.Master.Beingserver;
        return b != null && b.Currentblock != null ? b.Currentblock.Position : ArgoUtil.Pos(part.Master);
    }

    static bool Touching(Vector3 a, Vector3 b)
    {
        float d = ArgoUtil.Flat(a, b);
        return d > 0.3f && d <= ArgoData.FloodLinkReach && Mathf.Abs(a.y - b.y) < 0.6f;
    }

    public static void Update()
    {
        if (Time.time < nextTick)
            return;
        nextTick = Time.time + ArgoData.WaterTick;
        Rebuild();
        Ticks++;
        foreach (Net net in Nets)
        {
            try { Work(net); }
            catch (Exception e) { ArgoLog.Warn("water: " + e.Message); }
        }
        if (lastWet.Count > 128)
        {
            List<Entity_Master> stale = new List<Entity_Master>();
            foreach (KeyValuePair<Entity_Master, float> kv in lastWet)
                if (kv.Key == null || Time.time - kv.Value > ArgoData.CatchMemory * 2f) stale.Add(kv.Key);
            foreach (Entity_Master e in stale) { lastWet.Remove(e); wetVfx.Remove(e); }
        }
    }

    // ---------------------------------------------------------------- networks

    public static void Rebuild()
    {
        Nets.Clear();
        List<ArgoTowerBrain> water = new List<ArgoTowerBrain>();
        foreach (ArgoTowerBrain t in ArgoServer.Towers)
            if (t != null && (t.IsWater || t.IsPool) && t.Started && t.Master != null && ArgoUtil.Alive(t.Master))
                water.Add(t);
        List<ArgoTowerBrain> seen = new List<ArgoTowerBrain>();
        foreach (ArgoTowerBrain start in water)
        {
            if (seen.Contains(start))
                continue;
            Net net = new Net { Team = ArgoUtil.Team(start.Master) };
            List<ArgoTowerBrain> open = new List<ArgoTowerBrain> { start };
            seen.Add(start);
            while (open.Count > 0)
            {
                ArgoTowerBrain p = open[open.Count - 1];
                open.RemoveAt(open.Count - 1);
                net.Parts.Add(p);
                Vector3 tile = Tile(p);
                net.Tiles.Add(tile);
                foreach (ArgoTowerBrain q in water)
                    if (!seen.Contains(q) && ArgoUtil.Team(q.Master) == net.Team && Touching(tile, Tile(q)))
                    {
                        seen.Add(q);
                        open.Add(q);
                    }
            }
            foreach (ArgoTowerBrain p in net.Parts)
                if (p.IsWater && (net.Lead == null || p.UpgradeCount > net.Lead.UpgradeCount))
                    net.Lead = p;
            Nets.Add(net);
        }
    }

    public static Net NetOf(ArgoTowerBrain part)
    {
        foreach (Net n in Nets)
            if (n.Parts.Contains(part))
                return n;
        return null;
    }

    // The networks a Drill Rig's Tremor touches (its own team's water).
    public static List<Net> Shaken(ArgoTowerBrain drill)
    {
        List<Net> found = new List<Net>();
        if (drill == null || drill.Master == null)
            return found;
        int team = ArgoUtil.Team(drill.Master);
        float radius = drill.TremorRadius;
        foreach (Net n in Nets)
            if (n.Team == team && n.Touches(drill.AimCenter, radius))
                found.Add(n);
        return found;
    }

    // ---------------------------------------------------------------- the water's work

    static void Work(Net net)
    {
        net.Foes.Clear();
        Entity_Master by = net.Lead != null ? net.Lead.Master : net.Parts[0].Master;
        Link_Entityquery enemies = ArgoUtil.Enemies(by);
        foreach (ArgoTowerBrain p in net.Parts)
        {
            Vector3 at = ArgoUtil.Pos(p.Master);
            foreach (Entity_Master e in ArgoUtil.Area(at, ArgoData.PoolReach + 0.5f, enemies))
                if (e != null && ArgoUtil.Alive(e) && ArgoUtil.HasTag(e, "Mob") && !net.Foes.Contains(e) && InPool(at, ArgoUtil.Pos(e)))
                    net.Foes.Add(e);
        }
        ArgoTowerBrain lead = net.Lead;
        float slow = lead != null ? Mathf.Min(ArgoData.SlowCap, ArgoUtil.Stat(lead.Master, "Slow", ArgoData.WaterSlow)) : ArgoData.WaterSlow;
        float brine = lead != null ? lead.Param("Brine_Damage", 0f) : 0f;
        float salt = lead != null && lead.Has("Salt") ? lead.Param("Brinepinned_Multi", 1f) : 0f;
        float catchFor = lead != null ? lead.Param("Catch_Duration", 0f) : 0f;
        float crush = lead != null ? lead.Param("Damagetaken_Multi", 0f) : 0f;
        foreach (Entity_Master e in net.Foes)
        {
            float last;
            bool vfx = !wetVfx.TryGetValue(e, out last) || Time.time - last > 1.5f;
            if (vfx)
            {
                wetVfx[e] = Time.time;
                ArgoUtil.Vis(by, "Argo_Wet", new Vector3((float)e.NetworkObjectId, 1.6f, 0f), 1.6f);
            }
            ArgoUtil.Slow(by, e, slow, ArgoData.WaterTick + 0.25f, vfx);
            ArgoStatus.Wet(e, ArgoData.WaterTick + 0.3f);
            // Kelp Bed: caught on entering (again only after it was out of the water for CatchMemory seconds).
            if (catchFor > 0f && (!lastWet.TryGetValue(e, out last) || Time.time - last > ArgoData.CatchMemory))
            {
                ArgoUtil.Root(by, e, catchFor);
                Catches++;
                ArgoUtil.Vis(by, "Argo_Kelp", new Vector3((float)e.NetworkObjectId, catchFor, 0f), catchFor);
            }
            lastWet[e] = Time.time;
            // Brine; Salt Sting doubles it on a pinned foe (and stings with SaltBrine when Brine wasn't taken).
            float b = brine;
            if (salt > 0f && ArgoStatus.IsPinned(e))
                b = (b > 0f ? b : ArgoData.SaltBrine) * (1f + salt);
            if (b > 0f)
            {
                ModKitCombat.Hurt(by, e, b * ArgoData.WaterTick, 1f, null, ArgoUtil.Data("Argodot", 1f));
                Brined++;
                if (vfx)
                    ArgoUtil.Vis(by, "Argo_Brine", ArgoUtil.Pos(e), 0);
            }
            if (crush > 0f)
            {
                ArgoHooks.Soak(e, crush, ArgoData.WaterTick + 0.3f);
                Soaked++;
            }
        }
        // Trench: every few seconds one foe in the water is dragged under, held for a moment.
        float every = lead != null ? lead.Param("Dragunder_Every", 0f) : 0f;
        if (every > 0f && net.Foes.Count > 0 && Time.time >= lead.NextDrag)
        {
            lead.NextDrag = Time.time + every;
            Entity_Master f = net.Foes[UnityEngine.Random.Range(0, net.Foes.Count)];
            float hold = lead.Param("Dragunder_Duration", 1f);
            ArgoUtil.Root(by, f, hold);
            Drags++;
            ArgoUtil.Vis(by, "Argo_Dragunder", new Vector3((float)f.NetworkObjectId, hold, 0f), hold);
        }
    }

    // Trench: a free pool for every network led by a Trench pump, each Tide.
    public static void OnTide()
    {
        Rebuild();
        foreach (Net net in new List<Net>(Nets))
        {
            int n = net.Lead != null ? Mathf.RoundToInt(net.Lead.Param("Spread_Per_Tide", 0f)) : 0;
            for (int i = 0; i < n; i++)
                if (FreePool(net))
                    Rebuild();
        }
    }

    // ---------------------------------------------------------------- FLOOD

    // FLOOD's "Build" from the game's build command on a pump or a pool: {"Tower":..,"Position":{"x":..},..}. The tile
    // must touch this network and take a pool; the pool costs Blue 15 (the owner pays), Abyss adds Flood_Add free ones.
    public static void Flood(ArgoTowerBrain from, string json)
    {
        if (from == null || from.Master == null || ArgoUtil.JsonString(json, "Tower") != ArgoData.Pool)
            return;
        Beingobject pool;
        if (!ArgoContent.Beings.TryGetValue(ArgoData.Pool, out pool))
            return;
        Vector3 p = new Vector3((float)ArgoUtil.JsonNumber(json, "x"), (float)ArgoUtil.JsonNumber(json, "y"), (float)ArgoUtil.JsonNumber(json, "z"));
        Entity_Master surface = Helper_Entityquerytools.Entitynearest(p, 0.25f, ArgoUtil.Tagged("Surface"));
        Rebuild();
        Net net = NetOf(from);
        if (surface == null || surface.Beingserver == null || surface.Beingserver.Currentblock == null || net == null)
            return;
        Genitem block = surface.Beingserver.Currentblock;
        bool next = false;
        foreach (Vector3 t in net.Tiles)
            if (Touching(t, block.Position)) next = true;
        if (!next || !Helper_Gridtools.Checkifplaceable(block, pool, 0, ArgoUtil.Team(from.Master)))
        {
            Refused++;
            ArgoUtil.MustSee(from.Master, "Cannotbuild", block.Position, block.Direction);
            if (!next)
                ArgoUtil.Warn(from.Master, 11);
            return;
        }
        long owner = ArgoUtil.Owner(from.Master);
        Link_Command cmd;
        float scale = from.Master.Commandlistserver.TryGetValue(ArgoData.FloodLink, out cmd) && cmd != null ? cmd.Goalvalue : 1f;
        if (!Helper_Resources.Checkresources(pool.Entity.Costs, owner, scale))
            return;
        if (!Spawn(from, block, pool, scale))
            return;
        Helper_Resources.Useresources(pool.Entity.Costs, owner, scale);
        from.Master.Radio.Vfxoneshotcheck("Placementborder", block.Position, Vector3.forward);
        Floods++;
        ArgoUtil.Vis(from.Master, "Argo_Flood", block.Position + Vector3.up, 1f);
        Rebuild();
        net = NetOf(from);
        int extra = net != null && net.Lead != null ? Mathf.RoundToInt(net.Lead.Param("Flood_Add", 0f)) : 0;
        for (int i = 0; i < extra && net != null; i++)
            if (FreePool(net))
            {
                Rebuild();
                net = NetOf(from);
            }
    }

    // A free pool on a tile next to the network, picked at random among those that take one (Abyss, Trench).
    public static int NoTile;

    public static bool FreePool(Net net)
    {
        Beingobject pool;
        if (net == null || net.Parts.Count == 0 || !ArgoContent.Beings.TryGetValue(ArgoData.Pool, out pool))
            return false;
        Vector3[] dirs = { Vector3.forward, Vector3.back, Vector3.left, Vector3.right };
        List<Genitem> free = new List<Genitem>();
        foreach (Vector3 t in net.Tiles)
            foreach (Vector3 d in dirs)
                foreach (float dy in new[] { 0f, -1f, 1f })
                {
                    Genitem block = ArgoUtil.Surface(t + d + Vector3.up * dy);
                    if (block == null || free.Contains(block) || !Touching(t, block.Position))
                        continue;
                    bool taken = false;
                    foreach (Vector3 o in net.Tiles)
                        if (ArgoUtil.Flat(o, block.Position) < 0.3f) taken = true;
                    if (taken)
                        continue;
                    bool ok = false;
                    try { ok = Helper_Gridtools.Checkifplaceable(block, pool, 0, net.Team); } catch (Exception) { }
                    if (ok)
                        free.Add(block);
                }
        if (free.Count == 0)
        {
            NoTile++;
            return false;
        }
        Genitem at = free[UnityEngine.Random.Range(0, free.Count)];
        ArgoTowerBrain from = net.Lead ?? net.Parts[0];
        if (!Spawn(from, at, pool, 0f))
            return false;
        Freepools++;
        ArgoUtil.Vis(from.Master, "Argo_Flood", at.Position + Vector3.up, 1f);
        return true;
    }

    // Fie's Grow Wall spawn: the new pool is a child of the pump or pool it grew from (its owner and team), and sells
    // for the share it was paid (a free pool sells for nothing).
    static bool Spawn(ArgoTowerBrain from, Genitem block, Beingobject pool, float sellvalue)
    {
        Link_Spawn s = new Link_Spawn();
        s.Genadd = block;
        s.Position = block.Position;
        s.Spawnstyle = Link_Spawn.Spawnstyles.Spawn;
        s.Orientation = 0;
        s.Sellvalue = sellvalue;
        s.Being = pool;
        return Helper_Entitytools.Spawn(s, block.Position, from.Master.Beingserver, true, null) != null;
    }
}
