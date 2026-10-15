// Primordial Dandelion Tree (limit 1 per player): grows a seed every 1/Seedrate seconds up to Seedlimit, shown as the
// "Seeds" counter (clients float that many round its crown). When a foe comes within Range every ready seed strikes at
// once, shared out nearest first (toughest first with Picky Placement) so each foe gets enough seeds to fell it; seeds
// left over stay ready. Growth: +3 seeds and +40% of its base range a stage.
//   Seed Packets       a foe felled by a seed gives 2 seeds back.
//   Demand and Supply  grows faster the fewer are ready: up to x2 when none are.
//   Picky Placement    toughest foes first; each seed also deals 1% of the foe's max health.
//   Garden Weeds       a foe that falls within 3 s of a seed hit gives a stack (+10% seed growth, 15 s); at most one
//                      stack per Growth stage.
//   Dandelion Storm    +50% range, +10 seeds, +50% seed growth (modifiers on its stats).
public class YosseleDandelionBrain : YosseleBuilding
{
    public float Ready;          // seeds ready (fractional while one grows)
    public int Strikes, SeedsFlown, Refunded;
    readonly List<float> weeds = new List<float>();   // Garden Weeds stacks, by expiry time
    readonly Dictionary<Entity_Master, float> lastSeed = new Dictionary<Entity_Master, float>();
    readonly Dictionary<Entity_Master, float> incoming = new Dictionary<Entity_Master, float>();   // seeds in flight
    Link_Modifier growLimit, growRange;
    int shown = -1;
    float lastTick;

    public int Limit { get { return Mathf.Max(0, Mathf.RoundToInt(Stat("Seedlimit", 6f))); } }
    public int Weeds { get { weeds.RemoveAll(t => t <= Time.time); return weeds.Count; } }

    protected override void Started2()
    {
        lastTick = Time.time;
        Show();
    }

    public float Rate
    {
        get
        {
            float rate = Stat("Seedrate", 0.5f);
            float boost = 0f;
            if (Has("Demand") && Limit > 0)
                boost += Param("Demand_Max", 1f) * (1f - Mathf.Clamp01(Ready / Limit));
            if (Has("Weeds"))
                boost += Param("Weeds_Per", 0.1f) * Weeds;
            return rate * (1f + boost);
        }
    }

    protected override void Tick()
    {
        float dt = Time.time - lastTick;
        lastTick = Time.time;
        int limit = Limit;
        if (Ready < limit)
            Ready = Mathf.Min(limit, Ready + Rate * dt);
        else if (Ready > limit)
            Ready = limit;
        if (Ready >= 1f)
        {
            List<Entity_Master> foes = Foes(Stat("Range", 3f));
            if (foes.Count > 0)
                Strike(foes);
        }
        Show();
    }

    void Show()
    {
        int n = Mathf.FloorToInt(Ready);
        if (n == shown)
            return;
        shown = n;
        ModKitCounter.Set(Master, "Seeds", n);
    }

    float PerSeed(Entity_Master foe)
    {
        float d = Stat("Damage", 2f);
        if (Has("Picky"))
            d += Param("Picky_Percent", 0.01f) * YosseleUtil.MaxHealth(foe);
        return Mathf.Max(0.01f, d);
    }

    void Strike(List<Entity_Master> foes)
    {
        if (Has("Picky"))
            foes.Sort((a, b) => YosseleUtil.MaxHealth(b).CompareTo(YosseleUtil.MaxHealth(a)));
        int seeds = Mathf.FloorToInt(Ready);
        int used = 0;
        foreach (Entity_Master foe in foes)
        {
            if (used >= seeds)
                break;
            float per = PerSeed(foe), flying;
            incoming.TryGetValue(foe, out flying);
            float left = YosseleUtil.Health(foe) - flying;
            if (left <= 0f)
                continue;
            int need = Mathf.Max(1, Mathf.CeilToInt(left / per));
            int n = Mathf.Min(need, seeds - used);
            used += n;
            Send(foe, n, per);
        }
        if (used <= 0)
            return;
        Ready -= used;
        Strikes++;
        SeedsFlown += used;
        Show();
    }

    // The seeds fly (every client draws them from one Vis) and hit when they arrive.
    void Send(Entity_Master foe, int seeds, float per)
    {
        Vector3 at = YosseleUtil.Pos(foe);
        Master.Radio.Vis_ClientRpc("Yossele_Seeds", at, seeds);
        Entity_Master target = foe;
        float damage = per * seeds, before;
        incoming.TryGetValue(foe, out before);
        incoming[foe] = before + damage;
        YosseleUtil.Later(this, YosseleData.SeedFlight, () =>
        {
            float now;
            if (target != null && incoming.TryGetValue(target, out now))
            {
                if (now - damage <= 0.01f) incoming.Remove(target);
                else incoming[target] = now - damage;
            }
            if (target == null || !YosseleUtil.Alive(target) || Master == null)
                return;
            lastSeed[target] = Time.time;
            YosseleUtil.Vis(Master, "Yossele_Seedhit", YosseleUtil.Pos(target), seeds);
            YosseleUtil.Hurt(Master, target, damage);
        });
    }

    // A foe fell (YosseleServer): a seed's kill refunds seeds, a recent seed hit grows a Garden Weeds stack.
    public void FoeFell(Entity_Master foe)
    {
        float when;
        if (foe == null || !lastSeed.TryGetValue(foe, out when))
            return;
        lastSeed.Remove(foe);
        float since = Time.time - when;
        if (Has("Packets") && since <= YosseleData.PacketsWindow)
        {
            int back = Mathf.RoundToInt(Param("Packets_Refund", 2f));
            Ready = Mathf.Min(Limit, Ready + back);
            Refunded += back;
            Show();
        }
        if (Has("Weeds") && since <= Param("Weeds_Window", 3f) && Weeds < Growth)
            weeds.Add(Time.time + Param("Weeds_Last", 15f));
        if (lastSeed.Count > 64)
        {
            List<Entity_Master> stale = new List<Entity_Master>();
            foreach (KeyValuePair<Entity_Master, float> kv in lastSeed)
                if (kv.Key == null || Time.time - kv.Value > 10f) stale.Add(kv.Key);
            foreach (Entity_Master e in stale) lastSeed.Remove(e);
        }
        incoming.Remove(foe);
    }

    // Seed limit and range grow with each stage (the range as a percentage of its base, so it adds like any other).
    protected override void OnGrowth()
    {
        growLimit = YosseleUtil.Keep(Master, growLimit, "Seedlimit", Helper_Entitytools.Modtypes.Add, YosseleData.SeedsPerStage * Growth);
        growRange = YosseleUtil.Keep(Master, growRange, "Range", Helper_Entitytools.Modtypes.Multi, YosseleData.RangePerStage * Growth);
    }
}
