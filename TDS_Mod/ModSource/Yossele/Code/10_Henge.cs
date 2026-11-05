// Overgrown Stonehenge: a sturdy block of stones that faces the way the player turned it while placing it. Its vines
// creep forward from it over the ground (building tiles too), 1 tile at first and +1 every 2 Growth stages (5 at stage
// 8); "forwards if possible": the line stops at a cliff or a gap, one level up or down is fine. Foes on its vines are
// slowed by Slow (15%). Each stage it takes 10% less damage (YosseleHooks).
//   Verdant Spread      vines spread all around: every ground tile within 2 steps, 3 at full Growth.
//   Vine Blockade       +50% max health, and a barrier of 30% of it each Tide (absorbs, never heals past that).
//   Natural Remedy      buildings on its vines mend 0.5 health a second per Growth stage (the stones included).
//   Opportunistic Carnivore  drains 5 a second from up to 5 foes on its vines, mending the stones as much.
//   Easy Target         a foe 5 s on its vines takes 15% more damage from everything while on them, and 1 s after.
//   Entangling Roots    a foe 7 s on its vines is stunned 1.5 s and loses 5% of its max health, which mends the
//                       stones; once per foe.
//   Obsidian            +200% max health.
//   Monument            20% less damage taken, and foes' debuffs on it are stripped at once (YosseleHooks).
// Creeping Ivy (a Direction its owner holds, 17_Directions.cs): +1 tile of reach; rank II: foes on the vines slow 15%
// more; rank III: +1 tile more for every 2 Tides this Stonehenge stands, at most +3 (IvyBonus).
public class YosseleHengeBrain : YosseleBuilding
{
    public Vector3 Facing = Vector3.forward;
    public int Orientation;
    public readonly List<Genitem> Tiles = new List<Genitem>();
    public readonly Dictionary<Entity_Master, float> OnSince = new Dictionary<Entity_Master, float>();
    public int Snares, Drained, Mended, Marked, IvyTides;
    int ivyShown;
    public float Barrier;
    float nextMendLook;   // the Mend look shows at most every 1.5 seconds per Stonehenge
    readonly List<Entity_Master> snared = new List<Entity_Master>();   // Entangling Roots takes each foe once
    bool faced;   // Begin sets Growth 0 before Started2 has read the facing

    public int Length { get { return 1 + Growth / 2 + IvyBonus; } }
    // How far its vines reach: the forward line, or Verdant Spread's radius (2 steps, 3 at full Growth).
    public int Reach
    {
        get
        {
            if (!Has("Verdant"))
                return Length;
            return Mathf.RoundToInt(FullGrown ? Param("Verdant_Full", 3f) : Param("Verdant_Reach", 2f)) + IvyBonus;
        }
    }

    // Creeping Ivy's extra tiles, from the rank its owner holds now.
    public int IvyBonus
    {
        get
        {
            int rank = Master != null ? YosseleServer.Player(Owner).Ivy : 0;
            if (rank <= 0)
                return 0;
            int grown = rank >= 3 ? Mathf.Min(YosseleData.IvyMax, Mathf.FloorToInt(IvyTides * YosseleData.IvyPerTide + 0.0001f)) : 0;
            return YosseleData.IvyReach + grown;
        }
    }

    protected override void Started2()
    {
        YosseleHooks.Watch(Master);
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

    // The vines spread again when Creeping Ivy's reach changes (a rank taken, or rank III's Tides).
    protected override void Tick()
    {
        int ivy = IvyBonus;
        if (!faced || ivy == ivyShown)
            return;
        ivyShown = ivy;
        Spread();
    }

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
        if (def.Key == "Monument")
        {
            Armor();
            YosseleHooks.Cleanse(Master);
        }
    }

    // Vine Blockade's barrier, renewed each Tide (a fresh one replaces what is left of the last).
    protected override void OnTide(int wave)
    {
        if (YosseleServer.Player(Owner).Ivy >= 3)
            IvyTides++;
        if (!Has("Blockade"))
            return;
        Barrier = Param("Blockade_Share", 0.3f) * ModKitUtil.MaxHealth(Master);
        YosseleHooks.Shield(Master, Barrier);
        ModKitUtil.Debuff(Master, Master, YosseleData.VfxBlockade, 600f);
    }

    void Armor()
    {
        float monument = Has("Monument") ? Param("Monument_Armor", 0.2f) : 0f;
        YosseleHooks.Armor(Master, YosseleData.HengeArmorPerStage * Growth + monument);
        YosseleHooks.Monument(Master, Has("Monument"));
    }

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
        List<Genitem> want = Has("Verdant") ? Around(home, Reach) : Line(home, Facing, Length);
        int added = 0;
        foreach (Genitem g in want)
            if (!Tiles.Contains(g) && YosseleVines.Claim(this, g))
            {
                Tiles.Add(g);
                added++;
            }
        if (added > 0)
            ModKitUtil.Vis(Master, "Yossele_Vines", Pos, added);
    }

    // The next ground tile one step along 'dir' from 'from': level, else one down, else one up.
    static Genitem Step(Genitem from, Vector3 dir)
    {
        Vector3 p = from.Position + dir;
        foreach (float dy in new[] { 0f, -1f, 1f })
        {
            Genitem g = ModKitUtil.Surface(p + Vector3.up * dy);
            if (g != null && g != from && ModKitUtil.Flat(g.Position, p) < 0.3f && Mathf.Abs(g.Position.y - (p.y + dy)) < 0.3f)
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
            if (g != null && ModKitUtil.Flat(g.Position, at) <= YosseleData.VineReach && Mathf.Abs(g.Position.y - at.y) < 1.1f)
                return true;
        if (Master.Beingserver != null && Master.Beingserver.Currentblock != null)
        {
            Vector3 home = Master.Beingserver.Currentblock.Position;
            if (ModKitUtil.Flat(home, at) <= YosseleData.VineReach && Mathf.Abs(home.y - at.y) < 1.1f)
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
            if (!ModKitUtil.Alive(foe))
                continue;
            float since;
            if (!OnSince.TryGetValue(foe, out since))
                OnSince[foe] = since = Time.time;
            float on = Time.time - since;
            if (Has("Carnivore") && drained < drainMax)
            {
                float amount = Param("Carnivore_Drain", 5f) * dt;
                ModKitUtil.Hurt(Master, foe, amount);
                ModKitUtil.Heal(Master, Master, amount);
                drained++;
                Drained++;
            }
            if (Has("Easy") && on >= Param("Easy_After", 5f))
            {
                YosseleHooks.Mark(Master, foe, Param("Easy_Bonus", 0.15f), dt + YosseleData.EasyLinger);
                Marked++;
            }
            if (Has("Roots") && on >= Param("Roots_After", 7f) && !snared.Contains(foe))
            {
                snared.Add(foe);
                float stun = Param("Roots_Stun", 1.5f);
                float sap = Param("Roots_Percent", 0.05f) * ModKitUtil.MaxHealth(foe);
                YosseleUtil.Root(Master, foe, stun, true, YosseleData.VfxTangle);
                ModKitUtil.Hurt(Master, foe, sap);
                ModKitUtil.Heal(Master, Master, sap);
                Snares++;
                ModKitUtil.Vis(Master, "Yossele_Snare", ModKitUtil.Pos(foe), sap);
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
        if (snared.Count > 64)
            snared.RemoveAll(e => e == null || !ModKitUtil.Alive(e));
        if (drained > 0)
            ModKitUtil.Vis(Master, "Yossele_Drain", Pos, drained);
        if (Has("Remedy"))
            Remedy(dt);
    }

    // 0.5 a second per Growth stage, counting stage 1 before it has grown (as Sustaining Practices does).
    void Remedy(float dt)
    {
        float amount = Param("Remedy_Heal", 0.5f) * Mathf.Max(1, Growth) * dt;
        float reach = Mathf.Max(Length, Reach) + 1.5f;
        foreach (Entity_Master b in ModKitUtil.Area(Pos, reach, ModKitUtil.Tagged("Tower")))
        {
            if (b == null || !ModKitUtil.Alive(b) || ModKitUtil.Team(b) != Team || !Covers(ModKitUtil.Pos(b)))
                continue;
            if (ModKitUtil.Health(b) >= ModKitUtil.MaxHealth(b) - 0.01f)
                continue;
            ModKitUtil.Heal(Master, b, amount);
            Mended++;
            if (Time.time >= nextMendLook)
            {
                nextMendLook = Time.time + 1.5f;
                ModKitUtil.Vis(Master, "Yossele_Mend", ModKitUtil.Pos(b), 0f);
            }
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
            if (t.Vine != null && t.Vine.Beingserver != null && ModKitUtil.Alive(t.Vine))
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
            if (henge == null || !henge.Started || henge.Master == null || !ModKitUtil.Alive(henge.Master))
                continue;
            List<Entity_Master> on = new List<Entity_Master>();
            foreach (Entity_Master foe in ModKitUtil.FoesNear(henge.Master, henge.Pos, Mathf.Max(henge.Length, henge.Reach) + 1.5f))
                if (henge.Covers(ModKitUtil.Pos(foe)))
                    on.Add(foe);
            float slow = henge.Stat("Slow", 0.15f) + (YosseleServer.Player(henge.Owner).Ivy >= 2 ? YosseleData.IvySlow : 0f);
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
            ModKitUtil.Debuff(by[kv.Key], kv.Key, YosseleData.VfxSlow, YosseleData.VineTick + 0.3f);
            float had;
            Link_Modifier mod;
            if (slowAmount.TryGetValue(kv.Key, out had) && Mathf.Approximately(had, kv.Value) && slowed.TryGetValue(kv.Key, out mod) && mod != null && !mod.Dead)
                continue;
            if (slowed.TryGetValue(kv.Key, out mod))
                ModKitUtil.Unmod(mod);
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
                ModKitUtil.Unmod(slowed[e]);
            slowed.Remove(e);
            slowAmount.Remove(e);
        }
    }

    public static bool IsSlowed(Entity_Master foe) { return foe != null && slowed.ContainsKey(foe); }

    // The slow the vines hold on a foe now (0: none).
    public static float SlowOf(Entity_Master foe) { float a; return foe != null && slowed.ContainsKey(foe) && slowAmount.TryGetValue(foe, out a) ? a : 0f; }
}
