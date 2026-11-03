// Primordial Dandelion Tree (limit 1 per player): grows a seed every 1/Seedrate seconds up to Seedlimit, shown as the
// "Seeds" counter (clients float that many round its crown), and is full again when each Tide begins. When a foe comes
// within Range every ready seed strikes at once, shared out nearest first (toughest first with Picky Placement) so each
// foe gets enough seeds to fell it; seeds left over stay ready. Growth: +3 seeds and +20% of its base range a stage.
//   Seed Packets       a foe felled by a seed gives 2 seeds back; seeds deal +3.
//   Demand and Supply  twice as fast while fewer than a quarter of the limit are ready.
//   Picky Placement    toughest foes first; each seed also deals 0.25% of the foe's max health.
//   Garden Weeds       a foe that falls within 3 s of a seed hit: 40% for a stack (+10% seed growth, 15 s); at most one
//                      stack per Growth stage.
//   D-MDS              seeds stop striking foes and shoot down foes' projectiles in Range instead (1 seed each, 5 for a
//                      boss's); seed growth -50%.
//   Bonsai             the Tree's build slot offers a Bonsai (YosseleBonsaiBrain).
//   Rubbery Shell      a strike bounces on twice, each time to the nearest foe within 2 tiles it hasn't hit; with D-MDS
//                      a seed goes on to 2 more projectiles within 2 tiles.
//   Dandelion Storm    +100% range, +20 seeds, +100% seed growth (modifiers on its stats).
public class YosseleDandelionBrain : YosseleBuilding
{
    public float Ready;          // seeds ready (fractional while one grows)
    public int Strikes, SeedsFlown, Refunded, Intercepted, Bounced, InterceptSeeds, Refills;
    public static int WeedsGrown;   // Garden Weeds stacks grown, every Tree (the host's Tide log line)
    readonly List<float> weeds = new List<float>();   // Garden Weeds stacks, by expiry time
    public readonly Dictionary<Entity_Master, float> LastSeed = new Dictionary<Entity_Master, float>();   // each foe's last seed hit
    readonly Dictionary<Entity_Master, float> incoming = new Dictionary<Entity_Master, float>();   // seeds in flight
    Link_Modifier growLimit, growRange;
    int shown = -1;
    float lastTick, nextIntercept;

    public int Limit { get { return Mathf.Max(0, Mathf.RoundToInt(Stat("Seedlimit", 10f))); } }
    public int Weeds { get { weeds.RemoveAll(t => t <= Time.time); return weeds.Count; } }
    protected virtual float StageSeeds { get { return YosseleData.SeedsPerStage; } }

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
            if (Has("Demand") && Limit > 0 && Ready < Param("Demand_Below", 0.25f) * Limit)
                boost += Param("Demand_Max", 1f);
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
        if (Has("Dmds"))
        {
            if (Time.time >= nextIntercept)
            {
                nextIntercept = Time.time + YosseleData.InterceptEvery;
                Intercept();
            }
        }
        else if (Ready >= 1f)
        {
            List<Entity_Master> foes = Foes(Stat("Range", 7f));
            if (foes.Count > 0)
                Strike(foes);
        }
        Show();
    }

    // The Bonsai upgrade opens the Bonsai's button at once (the vessel also checks twice a second).
    protected override void Upgraded(YosseleData.Upg def)
    {
        if (def.Key == "Bonsai")
            YosseleServer.Limit(Owner);
    }

    // Seeds start at the maximum when each Tide begins (the sheet).
    protected override void OnTide(int wave)
    {
        Ready = Limit;
        Refills++;
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
            d += Param("Picky_Percent", 0.0025f) * YosseleUtil.MaxHealth(foe);
        return Mathf.Max(0.01f, d);
    }

    void Strike(List<Entity_Master> foes)
    {
        // Seeds still in the air for foes that left without falling (the Tide's end, a despawn).
        if (incoming.Count > 32)
        {
            List<Entity_Master> gone = new List<Entity_Master>();
            foreach (Entity_Master e in incoming.Keys) if (e == null || !YosseleUtil.Alive(e)) gone.Add(e);
            foreach (Entity_Master e in gone) incoming.Remove(e);
        }
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
            Hit(target, seeds, damage);
            if (Has("Rubbery"))
                Bounce(target, seeds, new List<Entity_Master> { target }, Mathf.RoundToInt(Param("Rubbery_Bounces", 2f)));
        });
    }

    void Hit(Entity_Master target, int seeds, float damage)
    {
        LastSeed[target] = Time.time;
        YosseleUtil.Vis(Master, "Yossele_Seedhit", YosseleUtil.Pos(target), seeds);
        YosseleUtil.Hurt(Master, target, damage);
    }

    // Rubbery Shell: the seeds go on to the nearest foe within Rubbery_Range they haven't hit, as many times as it says.
    void Bounce(Entity_Master from, int seeds, List<Entity_Master> hit, int left)
    {
        if (left <= 0 || from == null || Master == null)
            return;
        Vector3 at = YosseleUtil.Pos(from);
        Entity_Master next = null;
        foreach (Entity_Master foe in YosseleUtil.FoesNear(Master, at, Param("Rubbery_Range", 2f)))
            if (!hit.Contains(foe))
            {
                next = foe;
                break;
            }
        if (next == null)
            return;
        hit.Add(next);
        Bounced++;
        Master.Radio.Vis_ClientRpc("Yossele_Seedbounce", at, (float)next.NetworkObjectId);
        Entity_Master target = next;
        YosseleUtil.Later(this, YosseleData.SeedFlight * 0.6f, () =>
        {
            if (target == null || !YosseleUtil.Alive(target) || Master == null)
                return;
            Hit(target, seeds, PerSeed(target) * seeds);
            Bounce(target, seeds, hit, left - 1);
        });
    }

    // ---------------------------------------------------------------- D-MDS

    // Projectiles aren't networked entities: the host keeps pooled ones per name in Game_Entity.Projectiles, and only
    // its copy deals damage (Helper_Damagetools.Projectile). A threat is an active one a foe shot (not a core's hint)
    // that hurts. It is put out with Kill(false): Kill(true) would run its death handlers, and a bomb's explosion lives
    // there (Projectile_Ai_Explosion). Clients put out their look-only copy from the Vis (YosseleClient).
    public static bool Threat(Projectile_Master p, int team)
    {
        return p != null && p.gameObject.activeInHierarchy && p.Ais != null && p.Ais.Count > 0 && p.Interactiontype == "Hurt"
            && p.Master != null && YosseleUtil.IsFoe(p.Master, team);
    }

    public static List<Projectile_Master> Threats(Vector3 at, float range, int team)
    {
        List<Projectile_Master> list = new List<Projectile_Master>();
        Game_Entity ge = Game_Entity.Instance;
        if (ge == null || ge.Projectiles == null)
            return list;
        foreach (KeyValuePair<string, List<GameObject>> kv in ge.Projectiles)
        {
            if (kv.Value == null)
                continue;
            foreach (GameObject go in kv.Value)
            {
                if (go == null || !go.activeInHierarchy || YosseleUtil.Flat(go.transform.position, at) > range)
                    continue;
                Projectile_Master p = go.GetComponent<Projectile_Master>();
                if (Threat(p, team))
                    list.Add(p);
            }
        }
        list.Sort((a, b) => YosseleUtil.Flat(a.transform.position, at).CompareTo(YosseleUtil.Flat(b.transform.position, at)));
        return list;
    }

    // A boss's projectile (its shooter has a base health of 600 or more) takes Dmds_Boss seeds.
    public int Cost(Projectile_Master p) { return CostFor(p.Master != null ? YosseleUtil.BaseHealth(p.Master) : 0f); }

    public int CostFor(float shooterHealth)
    {
        return shooterHealth >= YosseleData.BossHealth ? Mathf.Max(1, Mathf.RoundToInt(Param("Dmds_Boss", 5f))) : 1;
    }

    void Intercept()
    {
        if (Ready < 1f)
            return;
        List<Projectile_Master> threats = Threats(Pos, Stat("Range", 7f), Team);
        foreach (Projectile_Master p in threats)
        {
            if (!Threat(p, Team))
                continue;   // put out by a bounce below
            int cost = Cost(p);
            if (Ready < cost)
                continue;
            Ready -= cost;
            InterceptSeeds += cost;
            Vector3 at = p.transform.position;
            Shoot(p);
            if (!Has("Rubbery"))
                continue;
            int left = Mathf.RoundToInt(Param("Rubbery_Bounces", 2f));
            float reach = Param("Rubbery_Range", 2f);
            foreach (Projectile_Master q in threats)
            {
                if (left <= 0)
                    break;
                if (q == p || !Threat(q, Team) || YosseleUtil.Flat(q.transform.position, at) > reach)
                    continue;
                at = q.transform.position;
                Shoot(q);
                Bounced++;
                left--;
            }
        }
        Show();
    }

    void Shoot(Projectile_Master p)
    {
        Vector3 at = p.transform.position;
        float shooter = p.Master != null ? (float)p.Master.NetworkObjectId : -1f;
        p.Kill(false);
        Intercepted++;
        Master.Radio.Vis_ClientRpc("Yossele_Intercept", at, shooter);
    }

    // ---------------------------------------------------------------- foes falling, Growth

    // A foe fell (YosseleServer): a seed's kill refunds seeds, a recent seed hit may grow a Garden Weeds stack.
    public void FoeFell(Entity_Master foe)
    {
        float when;
        if (foe == null)
            return;
        incoming.Remove(foe);   // a foe that falls before its seeds land
        if (!LastSeed.TryGetValue(foe, out when))
            return;
        LastSeed.Remove(foe);
        float since = Time.time - when;
        if (Has("Packets") && since <= YosseleData.PacketsWindow)
        {
            int back = Mathf.RoundToInt(Param("Packets_Refund", 2f));
            Ready = Mathf.Min(Limit, Ready + back);
            Refunded += back;
            Show();
        }
        if (Has("Weeds") && since <= Param("Weeds_Window", 3f) && Weeds < Growth && UnityEngine.Random.value < Param("Weeds_Chance", 0.4f))
        {
            weeds.Add(Time.time + Param("Weeds_Last", 15f));
            WeedsGrown++;
        }
        if (LastSeed.Count > 64)
        {
            List<Entity_Master> stale = new List<Entity_Master>();
            foreach (KeyValuePair<Entity_Master, float> kv in LastSeed)
                if (kv.Key == null || Time.time - kv.Value > 10f) stale.Add(kv.Key);
            foreach (Entity_Master e in stale) LastSeed.Remove(e);
        }
    }

    // Seed limit and range grow with each stage (the range as a percentage of its base, so it adds like any other).
    protected override void OnGrowth()
    {
        growLimit = YosseleUtil.Keep(Master, growLimit, "Seedlimit", Helper_Entitytools.Modtypes.Add, StageSeeds * Growth);
        growRange = YosseleUtil.Keep(Master, growRange, "Range", Helper_Entitytools.Modtypes.Multi, YosseleData.RangePerStage * Growth);
    }
}

// Bonsai (Tree upgrade Bonsai, limit 1): the Tree's brain at 40%. Its own stats are the Tree's at 40%, and every
// second it copies the upgrades of its player's standing Tree, their effects at 40% (Building.Apply); with no Tree it
// keeps the last copy. Its Growth adds 40% of the Tree's seeds a stage. It sells for nothing: its sell values are set
// to 0 when it is planted (Entity_Ai_Artifact_Bottomrock's way).
public class YosseleBonsaiBrain : YosseleDandelionBrain
{
    public YosseleDandelionBrain Tree;
    public int Copies;
    float nextCopy;

    protected override float StageSeeds { get { return YosseleData.SeedsPerStage * YosseleData.BonsaiShare; } }

    protected override void Started2()
    {
        Entity_Master.Link_Entitydataref data = Master.Entitydata.Value;
        data.Sellvalue = 0f;
        data.Upgradesellvalue = 0f;
        Master.Entitydata.Value = data;
        Copy();
        base.Started2();
    }

    protected override void Tick()
    {
        if (Time.time >= nextCopy)
        {
            nextCopy = Time.time + 1f;
            Copy();
        }
        base.Tick();
    }

    // Copies the Tree's upgrades it doesn't have yet; a different set (a new Tree) is copied from scratch.
    void Copy()
    {
        YosseleDandelionBrain tree = YosseleServer.TreeOf(Owner);
        if (tree == null || !tree.Started || tree.Master == null || tree.Master.Upgradelistserver == null)
            return;
        Tree = tree;
        bool same = Keys.Count == tree.Keys.Count;
        foreach (string k in Keys)
            if (!tree.Keys.Contains(k)) same = false;
        if (same)
            return;
        foreach (string k in Keys)
            if (!tree.Keys.Contains(k))
            {
                ClearUpgrades();
                break;
            }
        foreach (Upgradeproperties up in new List<Upgradeproperties>(tree.Master.Upgradelistserver.Values))
            if (Apply(up, YosseleData.BonsaiShare) != null)
                Copies++;
    }
}
