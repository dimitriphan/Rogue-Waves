// Overgrown Stonehenge: a sturdy block of stones that faces the way the player turned it while placing it. Its vines
// creep forward from it over the ground (building tiles too), 1 tile at first and +1 every 2 Growth stages (5 at stage
// 8); "forwards if possible": the line stops at a cliff or a gap, one level up or down is fine. Foes on its vines are
// slowed by Slow (25%). Each stage it takes 10% less damage (YosseleHooks).
//   Verdant Spread      vines spread all around (every tile within the vine length, walking the ground).
//   Vine Blockade       +50% max health, and a barrier of 30% of it each Tide (absorbs, never heals past that).
//   Natural Remedy      buildings on its vines mend 10 health a second (the stones included).
//   Opportunistic Carnivore  drains 10 a second from up to 5 foes on its vines, mending the stones as much.
//   Entangling Spaghetti Roots  a foe 7 s on its vines is stunned 1.5 s and loses 10% of its max health, which mends
//                       the stones.
public class YosseleHengeBrain : YosseleBuilding
{
    public Vector3 Facing = Vector3.forward;
    public int Orientation;
    public readonly List<Genitem> Tiles = new List<Genitem>();
    public readonly Dictionary<Entity_Master, float> OnSince = new Dictionary<Entity_Master, float>();
    public int Snares, Drained, Mended;
    public float Barrier;
    bool faced;   // Begin sets Growth 0 before Started2 has read the facing

    public int Length { get { return 1 + Growth / 2; } }

    protected override void Started2()
    {
        Orientation = Master.Beingserver != null ? Master.Beingserver.Orientation : 0;
        try { Facing = Helper_Gridtools.Orientationdir(Vector3.forward, Vector3.up, Orientation); }
        catch (Exception) { Facing = Vector3.forward; }
        Facing.y = 0f;
        Facing = Facing.sqrMagnitude > 0.01f ? Facing.normalized : Vector3.forward;
        // Snapped to one of the four grid directions.
        Facing = Mathf.Abs(Facing.x) > Mathf.Abs(Facing.z) ? new Vector3(Mathf.Sign(Facing.x), 0f, 0f) : new Vector3(0f, 0f, Mathf.Sign(Facing.z));
        YosseleLog.Info("Stonehenge at " + Pos.ToString("F1") + " faces " + Facing + " (orientation " + Orientation + ")");
        faced = true;
        Armor();
        Spread();
    }

    protected override void Tick() { }

    protected override void OnGrowth()
    {
        if (!faced)
            return;
        Armor();
        Spread();
    }

    protected override void Upgraded(YosseleData.Upg def)
    {
        if (def.Key == "Verdant" && faced)
            Spread();
    }

    // Vine Blockade's barrier, renewed each Tide (a fresh one replaces what is left of the last).
    protected override void OnTide(int wave)
    {
        if (!Has("Blockade"))
            return;
        Barrier = Param("Blockade_Share", 0.3f) * YosseleUtil.MaxHealth(Master);
        YosseleHooks.Shield(Master, Barrier);
        YosseleUtil.Debuff(Master, Master, YosseleData.VfxBlockade, 600f);
    }

    void Armor() { YosseleHooks.Armor(Master, YosseleData.HengeArmorPerStage * Growth); }

    public override void Gone(bool killed)
    {
        YosseleVines.Release(this);
        YosseleHooks.Forget(Master);
    }

    // ---------------------------------------------------------------- the vines' tiles

    public void Spread()
    {
        Genitem home = Master.Beingserver != null ? Master.Beingserver.Currentblock : null;
        if (home == null)
            return;
        List<Genitem> want = Has("Verdant") ? Around(home, Length) : Line(home, Facing, Length);
        int added = 0;
        foreach (Genitem g in want)
            if (!Tiles.Contains(g) && YosseleVines.Claim(this, g))
            {
                Tiles.Add(g);
                added++;
            }
        if (added > 0)
            YosseleUtil.Vis(Master, "Yossele_Vines", Pos, added);
    }

    // The next ground tile one step along 'dir' from 'from': level, else one down, else one up.
    static Genitem Step(Genitem from, Vector3 dir)
    {
        Vector3 p = from.Position + dir;
        foreach (float dy in new[] { 0f, -1f, 1f })
        {
            Genitem g = YosseleUtil.Surface(p + Vector3.up * dy);
            if (g != null && g != from && YosseleUtil.Flat(g.Position, p) < 0.3f && Mathf.Abs(g.Position.y - (p.y + dy)) < 0.3f)
                return g;
        }
        return null;
    }

    static List<Genitem> Line(Genitem home, Vector3 dir, int length)
    {
        List<Genitem> tiles = new List<Genitem>();
        Genitem at = home;
        for (int i = 0; i < length; i++)
        {
            at = Step(at, dir);
            if (at == null)
                break;
            tiles.Add(at);
        }
        return tiles;
    }

    // Every tile within 'length' steps (no diagonals) walking the ground, each step level or one up or down.
    static List<Genitem> Around(Genitem home, int length)
    {
        Vector3[] dirs = { Vector3.forward, Vector3.back, Vector3.left, Vector3.right };
        List<Genitem> tiles = new List<Genitem>();
        List<Genitem> edge = new List<Genitem> { home };
        List<Genitem> seen = new List<Genitem> { home };
        for (int i = 0; i < length; i++)
        {
            List<Genitem> next = new List<Genitem>();
            foreach (Genitem g in edge)
                foreach (Vector3 d in dirs)
                {
                    Genitem n = Step(g, d);
                    if (n == null || seen.Contains(n))
                        continue;
                    seen.Add(n);
                    next.Add(n);
                    tiles.Add(n);
                }
            edge = next;
        }
        return tiles;
    }

    public bool Covers(Vector3 at)
    {
        foreach (Genitem g in Tiles)
            if (g != null && YosseleUtil.Flat(g.Position, at) <= YosseleData.VineReach && Mathf.Abs(g.Position.y - at.y) < 1.1f)
                return true;
        if (Master.Beingserver != null && Master.Beingserver.Currentblock != null)
        {
            Vector3 home = Master.Beingserver.Currentblock.Position;
            if (YosseleUtil.Flat(home, at) <= YosseleData.VineReach && Mathf.Abs(home.y - at.y) < 1.1f)
                return true;
        }
        return false;
    }

    // ---------------------------------------------------------------- what the vines do (every VineTick)

    public void Work(List<Entity_Master> onVines, float dt)
    {
        int drained = 0;
        float drainMax = Param("Carnivore_Max", 5f);
        foreach (Entity_Master foe in onVines)
        {
            if (!YosseleUtil.Alive(foe))
                continue;
            float since;
            if (!OnSince.TryGetValue(foe, out since))
                OnSince[foe] = since = Time.time;
            if (Has("Carnivore") && drained < drainMax)
            {
                float amount = Param("Carnivore_Drain", 10f) * dt;
                YosseleUtil.Hurt(Master, foe, amount);
                YosseleUtil.Heal(Master, Master, amount);
                drained++;
                Drained++;
            }
            if (Has("Spaghetti") && Time.time - since >= Param("Spaghetti_After", 7f))
            {
                OnSince[foe] = Time.time;
                float stun = Param("Spaghetti_Stun", 1.5f);
                float sap = Param("Spaghetti_Percent", 0.1f) * YosseleUtil.MaxHealth(foe);
                YosseleUtil.Root(Master, foe, stun, true, YosseleData.VfxTangle);
                YosseleUtil.Hurt(Master, foe, sap);
                YosseleUtil.Heal(Master, Master, sap);
                Snares++;
                YosseleUtil.Vis(Master, "Yossele_Snare", YosseleUtil.Pos(foe), sap);
            }
        }
        // Foes that stepped off start their count again.
        List<Entity_Master> off = null;
        foreach (Entity_Master e in OnSince.Keys)
            if (e == null || !onVines.Contains(e))
            {
                if (off == null) off = new List<Entity_Master>();
                off.Add(e);
            }
        if (off != null)
            foreach (Entity_Master e in off)
                OnSince.Remove(e);
        if (drained > 0)
            YosseleUtil.Vis(Master, "Yossele_Drain", Pos, drained);
        if (Has("Remedy"))
            Remedy(dt);
    }

    void Remedy(float dt)
    {
        float amount = Param("Remedy_Heal", 10f) * dt;
        float reach = Length + 1.5f;
        foreach (Entity_Master b in YosseleUtil.Area(Pos, reach, YosseleUtil.Tagged("Tower")))
        {
            if (b == null || !YosseleUtil.Alive(b) || YosseleUtil.Team(b) != Team || !Covers(YosseleUtil.Pos(b)))
                continue;
            if (YosseleUtil.Health(b) >= YosseleUtil.MaxHealth(b) - 0.01f)
                continue;
            YosseleUtil.Heal(Master, b, amount);
            Mended++;
        }
    }
}

// The vine tiles of every Stonehenge (host only): one networked vine being per tile, shared when two Stonehenges'
// vines meet (it goes when the last one that claims it does). Every VineTick each foe on any vine is slowed by the
// strongest Slow among the Stonehenges whose vines it stands on (one modifier per foe, taken off when it steps off),
// and each Stonehenge works the foes on its own vines.
public static class YosseleVines
{
    class Tile { public Genitem Block; public Entity_Master Vine; public readonly List<YosseleHengeBrain> Owners = new List<YosseleHengeBrain>(); }

    static readonly List<Tile> tiles = new List<Tile>();
    static readonly Dictionary<Entity_Master, Link_Modifier> slowed = new Dictionary<Entity_Master, Link_Modifier>();
    static readonly Dictionary<Entity_Master, float> slowAmount = new Dictionary<Entity_Master, float>();
    static float nextTick;
    public static int Spawned, Failed, Ticks, Slowed;

    public static int Count { get { return tiles.Count; } }

    public static void Reset()
    {
        tiles.Clear();
        slowed.Clear();
        slowAmount.Clear();
    }

    public static bool Claim(YosseleHengeBrain henge, Genitem block)
    {
        foreach (Tile t in tiles)
            if (t.Block == block)
            {
                if (!t.Owners.Contains(henge)) t.Owners.Add(henge);
                return true;
            }
        Beingobject being;
        if (!YosseleContent.Beings.TryGetValue(YosseleData.Vine, out being))
            return false;
        Entity_Master vine = null;
        try { vine = YosseleUtil.SpawnOnTile(being, block, henge.Master, ""); }
        catch (Exception e) { YosseleLog.Warn("vine spawn: " + e.Message); }
        if (vine == null)
        {
            Failed++;
            return false;
        }
        Spawned++;
        Tile tile = new Tile { Block = block, Vine = vine };
        tile.Owners.Add(henge);
        tiles.Add(tile);
        return true;
    }

    public static void Release(YosseleHengeBrain henge)
    {
        for (int i = tiles.Count - 1; i >= 0; i--)
        {
            Tile t = tiles[i];
            t.Owners.Remove(henge);
            t.Owners.RemoveAll(o => o == null);
            if (t.Owners.Count > 0)
                continue;
            tiles.RemoveAt(i);
            if (t.Vine != null && t.Vine.Beingserver != null && YosseleUtil.Alive(t.Vine))
            {
                try { Helper_Entitytools.Despawn(t.Vine.Beingserver, false, "Yossele vines"); }
                catch (Exception e) { YosseleLog.Warn("vine despawn: " + e.Message); }
            }
        }
    }

    public static void VineGone(Entity_Master vine)
    {
        foreach (Tile t in tiles)
            if (t.Vine == vine)
                t.Vine = null;
    }

    public static void Update(List<YosseleBuilding> buildings)
    {
        if (Time.time < nextTick)
            return;
        nextTick = Time.time + YosseleData.VineTick;
        Ticks++;
        Dictionary<Entity_Master, float> want = new Dictionary<Entity_Master, float>();
        Dictionary<Entity_Master, Entity_Master> by = new Dictionary<Entity_Master, Entity_Master>();
        foreach (YosseleBuilding b in buildings.ToArray())
        {
            YosseleHengeBrain henge = b as YosseleHengeBrain;
            if (henge == null || !henge.Started || henge.Master == null || !YosseleUtil.Alive(henge.Master))
                continue;
            List<Entity_Master> on = new List<Entity_Master>();
            foreach (Entity_Master foe in YosseleUtil.FoesNear(henge.Master, henge.Pos, henge.Length + 1.5f))
                if (henge.Covers(YosseleUtil.Pos(foe)))
                    on.Add(foe);
            float slow = henge.Stat("Slow", 0.25f);
            foreach (Entity_Master foe in on)
            {
                float had;
                want[foe] = want.TryGetValue(foe, out had) ? Mathf.Max(had, slow) : slow;
                by[foe] = henge.Master;
            }
            try { henge.Work(on, YosseleData.VineTick); }
            catch (Exception e) { YosseleLog.Warn("vines: " + e.Message); }
        }
        // One slow per foe, re-made only when its strength changes.
        foreach (KeyValuePair<Entity_Master, float> kv in want)
        {
            // A modifier draws nothing: the stock slow look, renewed every tick while the foe stays on the vines.
            YosseleUtil.Debuff(by[kv.Key], kv.Key, YosseleData.VfxSlow, YosseleData.VineTick + 0.3f);
            float had;
            Link_Modifier mod;
            if (slowAmount.TryGetValue(kv.Key, out had) && Mathf.Approximately(had, kv.Value) && slowed.TryGetValue(kv.Key, out mod) && mod != null && !mod.Dead)
                continue;
            if (slowed.TryGetValue(kv.Key, out mod))
                YosseleUtil.Unmod(mod);
            slowed[kv.Key] = YosseleUtil.SlowOn(kv.Key, kv.Value);
            slowAmount[kv.Key] = kv.Value;
            Slowed++;
        }
        List<Entity_Master> off = new List<Entity_Master>();
        foreach (Entity_Master e in slowed.Keys)
            if (e == null || !want.ContainsKey(e))
                off.Add(e);
        foreach (Entity_Master e in off)
        {
            if (e != null)
                YosseleUtil.Unmod(slowed[e]);
            slowed.Remove(e);
            slowAmount.Remove(e);
        }
    }

    public static bool IsSlowed(Entity_Master foe) { return foe != null && slowed.ContainsKey(foe); }
}
