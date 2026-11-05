// Claude's five Directions for Yossele (docs/scope/11, built 2026-11-05; compiled, not yet run in game). The vessel's brain
// copies the ranks held to YosselePlayer twice a second; these read them there, so they hold while the vessel is down.
//   Cutting Jar   a building of the holder's that falls leaves a cutting: the same building rebuilt within half a tile
//                 of it within 20 / 20 / 40 s starts at 25 / 50 / 75% of the Growth it had, rounded down (YosseleCuttings).
//   Sap Bucket    each laser hit slows the foe 15 / 25 / 35% for 2 s (one slow a foe, renewed by the next hit); rank
//                 III: while it lasts the foe takes 10% more from the holder's buildings (YosseleSap, YosseleHooks).
//   Creeping Ivy  the Stonehenge's vines reach 1 tile farther; rank II: foes on them slow 15% more; rank III: 1 tile
//                 more every 2 Tides a Stonehenge stands, at most 3 (0.5 a Tide on whole tiles; YosseleHengeBrain).
//   Deep Breath   the first laser hit after 2 s without firing deals +40 / 70 / 100%; rank III: +1 Range while it
//                 waits (YosseleBoosts, on the attacker's side of the hit: Interacthurt calls From.Interact first). The 2 s
//                 count from when it could fire again: Yossele fires once every 1 / Attackspeed seconds (2 s at 0.5), so
//                 2 s from the last shot would charge every beam.
//   Fairy Ring    after 1.5 s standing still, once a second: Yossele and the holder's buildings within 1.5 / 2 / 2.5
//                 tiles mend 1 / 1.5 / 2% of their max health (YosseleBoosts).
public class YosseleBoosts
{
    public int Breaths, Sapped, FairyMends;
    public float FairyMended;
    YosseleHeroBrain hero;
    Entity_Master.Interacts dealt;
    Link_Modifier reach;
    Vector3 anchor = new Vector3(float.NaN, 0f, 0f);
    float lastBeam = -99f, chargedAt = -1f, stillSince, nextListen, nextMend, nextLook;

    // Deep Breath is ready: no laser hit for BreathAfter seconds past the laser's own reload.
    public bool Charged
    {
        get
        {
            float reload = hero != null ? 1f / Mathf.Max(0.05f, ModKitUtil.Stat(hero.Master, "Attackspeed", 0.5f)) : 2f;
            return Time.time - lastBeam >= reload + YosseleData.BreathAfter;
        }
    }
    public bool Still { get { return !float.IsNaN(anchor.x) && Time.time - stillSince >= YosseleData.FairyStill; } }
    public bool Reaching { get { return reach != null; } }

    public void Update(YosseleHeroBrain brain)
    {
        hero = brain;
        Entity_Master m = brain.Master;
        YosselePlayer p = brain.Player;
        if (m == null || p == null || !ModKitUtil.Alive(m))
            return;
        // Updateinteract clears the attacker's side too, so the handler is said again ten times a second.
        if (Time.time >= nextListen)
        {
            nextListen = Time.time + 0.1f;
            if (dealt == null)
                dealt = OnDealt;
            ModKitUtil.Listen(m, dealt);
        }
        bool wantReach = p.Breath >= 3 && Charged;
        if (wantReach && reach == null)
            reach = Helper_Entitytools.Addmod(m, "Range", YosseleData.BreathRange, Helper_Entitytools.Modtypes.Add, 0f);
        else if (!wantReach && reach != null)
        {
            ModKitUtil.Unmod(reach);
            reach = null;
        }
        Fairy(m, p);
    }

    // Every hit Yossele lands, before the target takes it. Only the laser's (they carry its tag).
    void OnDealt(Link_Interact hit)
    {
        try
        {
            if (hero == null || hero.Player == null || hit == null || hit.Type != "Hurt" || hit.Data == null || !hit.Data.ContainsKey(YosseleData.LaserTag)
                || !ModKitUtil.IsFoe(hit.To, ModKitUtil.Team(hero.Master)))
                return;
            YosselePlayer p = hero.Player;
            if (p.Breath > 0)
            {
                if (Charged)
                    chargedAt = Time.time;
                float multi;
                if (Mathf.Approximately(chargedAt, Time.time) && hit.Data.TryGetValue("Damagemulti", out multi))
                {
                    // A copy: some AIs reuse one dictionary for several hits (pitfall #30).
                    hit.Data = new Dictionary<string, float>(hit.Data);
                    hit.Data["Damagemulti"] = multi * (1f + YosseleData.BreathBonus[p.Breath - 1]);
                    Breaths++;
                    ModKitUtil.Vis(hero.Master, "Yossele_Breath", ModKitUtil.Pos(hit.To), p.Breath);
                }
            }
            lastBeam = Time.time;
            if (p.Sap > 0 && YosseleSap.Stick(hero.Master, hit.To, p.Sap))
                Sapped++;
        }
        catch (Exception e)
        {
            YosseleLog.Warn("laser hit: " + e.Message);
        }
    }

    void Fairy(Entity_Master m, YosselePlayer p)
    {
        Vector3 pos = ModKitUtil.Pos(m);
        if (float.IsNaN(anchor.x) || ModKitUtil.Flat(pos, anchor) > 0.1f)
        {
            anchor = pos;
            stillSince = Time.time;
        }
        if (p.Fairy <= 0 || !Still || Time.time < nextMend)
            return;
        nextMend = Time.time + 1f;
        float radius = YosseleData.FairyRadius[p.Fairy - 1], share = YosseleData.FairyHeal[p.Fairy - 1];
        Mend(m, m, share);
        long owner = ModKitUtil.Owner(m);
        foreach (YosseleBuilding b in YosseleServer.Buildings)
            if (b != null && b.Started && b.Master != null && b.Owner == owner && ModKitUtil.Alive(b.Master) && ModKitUtil.Flat(b.Pos, pos) <= radius + 0.01f)
                Mend(m, b.Master, share);
        if (Time.time >= nextLook)
        {
            nextLook = Time.time + 2f;
            ModKitUtil.Vis(m, "Yossele_Fairy", pos, radius);
        }
    }

    void Mend(Entity_Master by, Entity_Master target, float share)
    {
        float missing = ModKitUtil.MaxHealth(target) - ModKitUtil.Health(target);
        float amount = Mathf.Min(missing, share * ModKitUtil.MaxHealth(target));
        if (amount <= 0.001f)
            return;
        ModKitUtil.Heal(by, target, amount);
        FairyMends++;
        FairyMended += amount;
    }
}

// Sap Bucket's slows (host only): one a foe, at the strongest rank that touched it, renewed by each laser hit, gone 2 s
// after the last.
public static class YosseleSap
{
    static readonly Dictionary<Entity_Master, Link_Modifier> slowed = new Dictionary<Entity_Master, Link_Modifier>();
    static readonly Dictionary<Entity_Master, float> amount = new Dictionary<Entity_Master, float>();
    static readonly Dictionary<Entity_Master, float> until = new Dictionary<Entity_Master, float>();
    static readonly List<Entity_Master> gone = new List<Entity_Master>();
    static float nextCheck;

    public static void Reset()
    {
        slowed.Clear();
        amount.Clear();
        until.Clear();
    }

    public static bool IsSapped(Entity_Master foe)
    {
        float t;
        return foe != null && until.TryGetValue(foe, out t) && t > Time.time;
    }

    public static float SlowOf(Entity_Master foe) { float a; return IsSapped(foe) && amount.TryGetValue(foe, out a) ? a : 0f; }

    public static bool Stick(Entity_Master from, Entity_Master foe, int rank)
    {
        if (foe == null || !ModKitUtil.Alive(foe) || rank <= 0)
            return false;
        float slow = YosseleData.SapSlow[Mathf.Min(rank, YosseleData.SapSlow.Length) - 1], had;
        until[foe] = Time.time + YosseleData.SapLasts;
        Link_Modifier m;
        bool keep = amount.TryGetValue(foe, out had) && had >= slow - 0.001f && slowed.TryGetValue(foe, out m) && m != null && !m.Dead;
        if (!keep)
        {
            if (slowed.TryGetValue(foe, out m))
                ModKitUtil.Unmod(m);
            slowed[foe] = YosseleUtil.SlowOn(foe, slow);
            amount[foe] = slow;
        }
        ModKitUtil.Debuff(from, foe, YosseleData.VfxSlow, YosseleData.SapLasts);
        if (rank >= 3)
            YosseleHooks.Sap(foe, ModKitUtil.Owner(from), YosseleData.SapBonus, YosseleData.SapLasts);
        return true;
    }

    public static void Update()
    {
        if (Time.time < nextCheck)
            return;
        nextCheck = Time.time + 0.1f;
        gone.Clear();
        foreach (KeyValuePair<Entity_Master, float> kv in until)
            if (kv.Key == null || kv.Value <= Time.time || !ModKitUtil.Alive(kv.Key))
                gone.Add(kv.Key);
        foreach (Entity_Master e in gone)
        {
            Link_Modifier m;
            if (e != null && slowed.TryGetValue(e, out m))
                ModKitUtil.Unmod(m);
            slowed.Remove(e);
            amount.Remove(e);
            until.Remove(e);
        }
    }
}

// Cutting Jar's cuttings (host only): what a fallen building of a holder's leaves, taken by the same building rebuilt on
// its tile in time (YosseleBuilding.Begin).
public static class YosseleCuttings
{
    class Cutting { public long Owner; public string Id; public Vector3 At; public int Stages; public float Until; }
    static readonly List<Cutting> cuttings = new List<Cutting>();
    public static int Left, Taken;

    public static int Count { get { cuttings.RemoveAll(c => c.Until < Time.time); return cuttings.Count; } }

    public static void Reset() { cuttings.Clear(); }

    public static void Fell(YosseleBuilding b)
    {
        if (b == null || b.Def == null || b.Growth <= 0)
            return;
        int rank = YosseleServer.Player(b.Owner).Cutting;
        if (rank <= 0)
            return;
        int keep = Mathf.FloorToInt(b.Growth * YosseleData.CuttingKeep[rank - 1] + 0.0001f);
        if (keep <= 0)
            return;
        cuttings.Add(new Cutting { Owner = b.Owner, Id = b.Def.Id, At = b.Pos, Stages = keep, Until = Time.time + YosseleData.CuttingWait[rank - 1] });
        Left++;
    }

    // The stages a new building takes from a cutting on its tile (0: none), which is then used up.
    public static int Take(YosseleBuilding b)
    {
        cuttings.RemoveAll(c => c.Until < Time.time);
        for (int i = 0; i < cuttings.Count; i++)
        {
            Cutting c = cuttings[i];
            if (c.Owner != b.Owner || c.Id != b.Def.Id || ModKitUtil.Flat(c.At, b.Pos) > 0.5f)
                continue;
            cuttings.RemoveAt(i);
            Taken++;
            return c.Stages;
        }
        return 0;
    }
}
