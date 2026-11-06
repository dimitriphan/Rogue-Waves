// Pin Cushion: a wall that pricks melee attackers and shakes Thread loose when hit. Its rules ride on the game's own hit
// event (Yossele's and Argo's pattern): Helper_Entitytools.Interacthurt hands each hit to the target's Oninteract chain,
// and the Cushion's handler goes first so it can split a hit before anyone applies it.
//   Reflect          a hit from a foe whose Range is 2.25 or less (melee) pricks it for Reflect of the damage.
//   Spin             every Spin damage taken drops a Thread beside it.
//   Slow Unravel     half of each hit now, half over the next Unravel_Base seconds (a heal cancels what is still owed).
//   Pin Barrage      ranged attackers are pricked for Rangedreflect_Base too.
//   Loose Stuffing   a Mangled per Mangled_Per damage taken, Mangled_Max alive; Overstuffed and Burst Seams shape them.
//   Iron Thimble     Health_Multi is its own stat's modifier; each hit lands Thimble_Base lighter. Pricks count off what
//                    the attacker dealt; Slow Unravel, Spin and Loose Stuffing off what it took.
//   Voodoo Doll      once a Tide, after the first hit that leaves it under Voodoo_Base of its maximum health: every foe
//                    within Voodoorange_Base is stunned Voodoostun_Base seconds, and it mends Voodooheal_Base of its maximum.
// Helper_Entitytools.Updateinteract rebuilds the chain whenever an upgrade or effect lands, so the handler is moved back
// to the front ten times a second.
public class SpoolCushionBrain : SpoolBuilding
{
    public const float MeleeRange = 2.25f;

    public int Pricks, Spun, Mangled, Bursts, Splits, Dolls, Stunned;
    public float Taken, Reflected, Delayed, Cancelled, Thimbled, DollHealed;
    public Entity_Master LastAttacker;
    float spinDebt, stuffDebt, nextFront, nextUnravel, lastPrickVis;
    bool own, hurt;
    int dollWave = -1;
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
        // A hit flagged in OnHit has landed by now: its health is the one after it.
        if (hurt)
        {
            hurt = false;
            if (Has("Voodoo"))
                Doll();
        }
    }

    // Keeps the cushion's handler first in the wall's hit chain (Updateinteract rebuilds it when an upgrade lands).
    void First() { ModKitUtil.Front(Master, handler); }

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
            hurt = true;
            Entity_Master from = hit.From;
            if (from != null && from != Master && ModKitUtil.IsFoe(from, ModKitUtil.Team(Master)))
            {
                LastAttacker = from;
                bool melee = ModKitUtil.Stat(from, "Range", 1.5f) <= MeleeRange;
                float rate = melee ? Stat("Reflect", 0.2f) : Has("Barrage") ? Param("Rangedreflect_Base", 0.2f) : 0f;
                if (rate > 0f)
                    Prick(from, total * rate);
            }
            // Iron Thimble: the hit lands lighter; everything below counts what it took.
            if (Has("Thimble"))
            {
                float cut = Mathf.Clamp01(Param("Thimble_Base", 0.25f));
                multi *= 1f - cut;
                ModKitCombat.SetMulti(hit, multi);
                Thimbled += total * cut;
                total *= 1f - cut;
            }
            Taken += total;
            if (Has("Unravel"))
            {
                float secs = Mathf.Max(0.5f, Param("Unravel_Base", 4f));
                ModKitCombat.SetMulti(hit, multi * 0.5f);
                if (owed.Count == 0)
                    nextUnravel = Time.time + 0.5f;
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
        try { ModKitUtil.Hurt(Master, foe, amount); }
        finally { own = false; }
        Pricks++;
        Reflected += amount;
        if (Time.time - lastPrickVis > 0.3f)
        {
            lastPrickVis = Time.time;
            ModKitUtil.Vis(Master, "Spool_Prick", ModKitUtil.Pos(foe), 0f);
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
        try { ModKitCombat.Hurt(Master, Master, damage, 1f, LastAttacker != null && ModKitUtil.Alive(LastAttacker) ? LastAttacker : Master, null); }
        finally { own = false; }
        hurt = true;
    }

    // Voodoo Doll, once a Tide (the Wave read as SpoolTinBrain.Fell reads it): under its mark after a hit, every foe in
    // reach is stunned (no walking, hardly any attacks: Argo's and Yossele's hold) and the doll mends itself.
    void Doll()
    {
        float max = ModKitUtil.MaxHealth(Master);
        if (!ModKitUtil.Alive(Master) || max <= 0f || ModKitUtil.Health(Master) >= max * Param("Voodoo_Base", 0.5f))
            return;
        int wave = ModKitTides.Wave >= 0 ? ModKitTides.Wave : (Game_Mapvars.Instance != null ? Game_Mapvars.Instance.Wave.Value : 0);
        if (wave == dollWave)
            return;
        dollWave = wave;
        Dolls++;
        float stun = Param("Voodoostun_Base", 3f);
        foreach (Entity_Master foe in Foes(Param("Voodoorange_Base", 3f)))
        {
            if (ModKitUtil.Speed(foe) == null)
                continue;
            Helper_Entitytools.Addmod(foe, "Speed", -1f, Helper_Entitytools.Modtypes.Multi, stun);
            if (foe.Statsserver.ContainsKey("Attackspeed"))
                Helper_Entitytools.Addmod(foe, "Attackspeed", -0.95f, Helper_Entitytools.Modtypes.Multi, stun);
            Stunned++;
            ModKitUtil.Vis(Master, "Spool_Voodoo", ModKitUtil.Pos(foe), stun);
        }
        float heal = max * Param("Voodooheal_Base", 0.5f);
        ModKitUtil.Heal(Master, Master, heal);
        DollHealed += heal;
        ModKitUtil.Vis(Master, "Spool_Voodoo", Pos, 0f);
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
