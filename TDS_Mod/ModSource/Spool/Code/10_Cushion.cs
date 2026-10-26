// Pin Cushion: a wall that pricks melee attackers and shakes Thread loose when hit. Its rules ride on the game's own hit
// event (Yossele's and Argo's pattern): Helper_Entitytools.Interacthurt hands each hit to the target's Oninteract chain,
// and the Cushion's handler goes first so it can split a hit before anyone applies it.
//   Reflect          a hit from a foe whose Range is 2.25 or less (melee) pricks it for Reflect of the damage.
//   Spin             every Spin damage taken drops a Thread beside it.
//   Slow Unravel     half of each hit now, half over the next Unravel_Base seconds (a heal cancels what is still owed).
//   Pin Barrage      ranged attackers are pricked for Rangedreflect_Base too.
//   Loose Stuffing   a Mangled per Mangled_Per damage taken, Mangled_Max alive; Overstuffed and Burst Seams shape them.
// Helper_Entitytools.Updateinteract rebuilds the chain whenever an upgrade or effect lands, so the handler is moved back
// to the front ten times a second.
public class SpoolCushionBrain : SpoolBuilding
{
    public const float MeleeRange = 2.25f;

    public int Pricks, Spun, Mangled, Bursts, Splits;
    public float Taken, Reflected, Delayed, Cancelled;
    public Entity_Master LastAttacker;
    float spinDebt, stuffDebt, nextFront, nextUnravel, lastPrickVis;
    bool own;
    Entity_Master.Oninteracts handler;

    // Damage still owed by Slow Unravel: how fast it comes, how much is left.
    class Owed
    {
        public float Rate, Left;
    }

    readonly List<Owed> owed = new List<Owed>();

    protected override void Started2()
    {
        handler = OnHit;
        First();
    }

    protected override void Tick()
    {
        if (Time.time >= nextFront)
        {
            nextFront = Time.time + 0.1f;
            First();
        }
        if (owed.Count > 0 && Time.time >= nextUnravel)
        {
            nextUnravel = Time.time + 0.5f;
            Unravel();
        }
    }

    void First()
    {
        Entity_Master.Oninteracts chain = Master.Oninteract;
        if (chain != null)
        {
            Delegate[] list = chain.GetInvocationList();
            if (list.Length > 0 && list[0].Equals(handler))
                return;
        }
        Delegate rest = Delegate.Remove(chain, handler);
        Master.Oninteract = (Entity_Master.Oninteracts)Delegate.Combine(handler, rest);
    }

    void OnHit(Link_Interact hit)
    {
        try
        {
            if (own || hit == null || hit.To != Master || hit.Data == null)
                return;
            if (hit.Type == "Heal")
            {
                Heal(hit);
                return;
            }
            float multi, damage;
            if (hit.Type != "Hurt" || !hit.Data.TryGetValue("Damagemulti", out multi) || !hit.Data.TryGetValue("Damage", out damage))
                return;
            float total = damage * multi;
            if (total <= 0f)
                return;
            Taken += total;
            Entity_Master from = hit.From;
            if (from != null && from != Master && SpoolUtil.IsFoe(from, SpoolUtil.Team(Master)))
            {
                LastAttacker = from;
                bool melee = SpoolUtil.Stat(from, "Range", 1.5f) <= MeleeRange;
                float rate = melee ? Stat("Reflect", 0.2f) : Has("Barrage") ? Param("Rangedreflect_Base", 0.2f) : 0f;
                if (rate > 0f)
                    Prick(from, total * rate);
            }
            if (Has("Unravel"))
            {
                float secs = Mathf.Max(0.5f, Param("Unravel_Base", 4f));
                hit.Data = new Dictionary<string, float>(hit.Data);
                hit.Data["Damagemulti"] = multi * 0.5f;
                owed.Add(new Owed { Rate = total * 0.5f / secs, Left = total * 0.5f });
                Delayed += total * 0.5f;
                Splits++;
            }
            Spin(total);
            if (Has("Stuffing"))
                Stuff(total, from);
        }
        catch (Exception e)
        {
            SpoolLog.Warn("cushion hit: " + e.Message);
        }
    }

    void Prick(Entity_Master foe, float amount)
    {
        own = true;
        try { SpoolUtil.Hurt(Master, foe, amount); }
        finally { own = false; }
        Pricks++;
        Reflected += amount;
        if (Time.time - lastPrickVis > 0.3f)
        {
            lastPrickVis = Time.time;
            SpoolUtil.Vis(Master, "Spool_Prick", SpoolUtil.Pos(foe), 0f);
        }
    }

    // Every Spin damage taken shakes a Thread loose beside the Cushion (Slow Unravel halves Spin: twice the Thread).
    void Spin(float total)
    {
        float per = Mathf.Max(1f, Stat("Spin", 25f));
        spinDebt += total;
        int n = Mathf.FloorToInt(spinDebt / per);
        if (n <= 0)
            return;
        spinDebt -= n * per;
        Spun += n;
        float a = (Spun % 6) * 1.05f;
        SpoolServer.Drop(Pos + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 0.75f, n);
    }

    void Stuff(float total, Entity_Master from)
    {
        float per = Mathf.Max(1f, Param("Mangled_Per", 40f));
        int max = Mathf.RoundToInt(Param("Mangled_Max", 3f));
        stuffDebt += total;
        while (stuffDebt >= per)
        {
            stuffDebt -= per;
            if (SpoolServer.UnitsOf(this) >= max)
                continue;
            bool stuffed = Has("Overstuffed");
            SpoolUnitBrain u = SpawnUnit(stuffed ? SpoolData.Stuffed : SpoolData.Mangled);
            if (u == null)
                continue;
            Mangled++;
            if (stuffed)
            {
                u.Leap = Param("Leap_Base", 2f);
                u.LeapAt = from;
            }
        }
    }

    void Unravel()
    {
        float damage = 0f;
        foreach (Owed o in owed)
        {
            float d = Mathf.Min(o.Left, o.Rate * 0.5f);
            o.Left -= d;
            damage += d;
        }
        owed.RemoveAll(o => o.Left <= 0.001f);
        if (damage <= 0f)
            return;
        own = true;
        try { ModKitCombat.Hurt(Master, Master, damage, 1f, LastAttacker != null && SpoolUtil.Alive(LastAttacker) ? LastAttacker : Master, null); }
        finally { own = false; }
    }

    public float Owing()
    {
        float sum = 0f;
        foreach (Owed o in owed)
            sum += o.Left;
        return sum;
    }

    // A heal cancels Slow Unravel's damage still owed, up to the heal's amount (its Damage, Helper_Damagetools.Doheal).
    void Heal(Link_Interact hit)
    {
        float amount, multi;
        if (owed.Count == 0 || !hit.Data.TryGetValue("Damage", out amount))
            return;
        if (hit.Data.TryGetValue("Damagemulti", out multi))
            amount *= multi;
        foreach (Owed o in owed)
        {
            float cut = Mathf.Min(o.Left, amount);
            o.Left -= cut;
            amount -= cut;
            Cancelled += cut;
        }
        owed.RemoveAll(o => o.Left <= 0.001f);
    }

    public override void Gone(bool killed, string reason) { owed.Clear(); }
}
