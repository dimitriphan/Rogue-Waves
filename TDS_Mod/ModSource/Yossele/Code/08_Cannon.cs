// Mushroom Cannon: lobs a spore bomb in an arc (Smacky's Bomb Tower's lob) at the nearest foe in Range, leading it by
// its walk. On landing, every foe within Radius takes Damage and Spores stacks (YosseleSpores). The flight is drawn by
// every client from one Vis ("Yossele_Lob": where it lands, how long it flies); the blast is a second Vis when it lands,
// so the hit and the look stay in step. Growth: attack speed +10% and radius +0.1 a stage; full Growth bounces each bomb
// once, a second blast at the same spot 1 s later at half radius and damage, with 1 stack. Cordyceps (tier 5) raises a
// foe that falls with spores as a Cordyfriend (YosseleFriends).
public class YosseleCannonBrain : YosseleBuilding
{
    class Lob { public Vector3 At; public float Lands, Radius, Damage; public int Spores; public bool Bounce, IsBounce; }

    readonly List<Lob> flying = new List<Lob>();
    float nextShot;
    Link_Modifier growSpeed, growRadius;
    public int Lobs, Blasts, Bounces, Hits;

    protected override void Tick()
    {
        for (int i = flying.Count - 1; i >= 0; i--)
            if (Time.time >= flying[i].Lands)
            {
                Lob l = flying[i];
                flying.RemoveAt(i);
                Land(l);
            }
        float aspd = Stat("Attackspeed", 0.3f);
        if (aspd <= 0f || Time.time < nextShot)
            return;
        List<Entity_Master> foes = Foes(Stat("Range", 6f));
        if (foes.Count == 0)
            return;
        nextShot = Time.time + 1f / aspd;
        Fire(foes[0]);
    }

    void Fire(Entity_Master foe)
    {
        Vector3 at = YosseleUtil.Pos(foe);
        float flight = YosseleData.LobBase + YosseleData.LobPerTile * YosseleUtil.Flat(Pos, at);
        // Lead the foe along its walk, as far as it gets in the flight (a held foe stays put).
        Vector3 dir = foe.Entitydirection.Value;
        dir.y = 0f;
        float speed = Mathf.Max(0f, YosseleUtil.Stat(foe, "Speed", 0f));
        if (dir.sqrMagnitude > 0.01f)
            at += dir.normalized * Mathf.Min(speed * flight, 2f);
        float radius = Stat("Radius", 1f);
        if (Has("Farout"))
            radius *= 1f + Mathf.Min(YosseleData.FaroutMax, Param("Farout_Per", 0.5f) * flight);
        flying.Add(new Lob { At = at, Lands = Time.time + flight, Radius = radius, Damage = Stat("Damage", 1.5f), Spores = Mathf.RoundToInt(Stat("Spores", 2f)), Bounce = FullGrown });
        Lobs++;
        Master.Radio.Vis_ClientRpc("Yossele_Lob", at, flight);
    }

    void Land(Lob l)
    {
        Blast(l.At, l.Radius, l.Damage, l.Spores);
        if (l.Bounce)
            flying.Add(new Lob
            {
                At = l.At, Lands = Time.time + YosseleData.BounceDelay, Radius = l.Radius * YosseleData.BounceShare,
                Damage = l.Damage * YosseleData.BounceShare, Spores = YosseleData.BounceSpores, IsBounce = true,
            });
        if (l.IsBounce)
            Bounces++;
    }

    // One spore blast: also used by Viral Outbreak (a felled foe bursting as a bomb) and the bounce.
    public void Blast(Vector3 at, float radius, float damage, int spores)
    {
        Blasts++;
        YosseleUtil.Vis(Master, "Yossele_Blast", at, radius);
        foreach (Entity_Master foe in YosseleUtil.FoesNear(Master, at, radius + 0.3f))
        {
            if (YosseleUtil.Flat(YosseleUtil.Pos(foe), at) > radius + 0.3f)
                continue;
            YosseleUtil.Hurt(Master, foe, damage);
            Hits++;
            if (YosseleUtil.Alive(foe))
                YosseleSpores.Add(foe, spores, this);
        }
    }

    protected override void OnGrowth()
    {
        growSpeed = YosseleUtil.Keep(Master, growSpeed, "Attackspeed", Helper_Entitytools.Modtypes.Multi, YosseleData.CannonGrowSpeed * Growth);
        growRadius = YosseleUtil.Keep(Master, growRadius, "Radius", Helper_Entitytools.Modtypes.Add, YosseleData.CannonGrowRadius * Growth);
    }

    // The upgrades' spore rules, read by YosseleSpores from the cannon whose bomb landed last on a foe.
    public int SporeMax { get { return Has("Deathcap") ? Mathf.RoundToInt(Param("Sporemax_Base", 8f)) : YosseleData.SporeMax; } }

    // Cordyceps: the chance a foe falling with this many stacks rises as a Cordyfriend.
    public float CordyChance(int stacks) { return Has("Cordyceps") ? Param("Cordy_Chance", 0.2f) + Param("Cordy_Per", 0.05f) * stacks : 0f; }
    public float SporeDps { get { return YosseleData.SporeDps * (1f + Param("Deathcap_Multi", 0f)); } }
}

// Spore stacks on foes (host only). Each new stack refreshes the 6 s fade; every 0.5 s each spored foe takes
// stacks x 0.3 a second (x1.25 with Deathcap) from the cannon that spored it last. At full stacks (10, 8 with
// Deathcap, which keeps a foe already above 8 where it is): Sticky Spores slows it while full, Explosive Finish bursts
// it, Hallucinogen may stun it every 5 s, and a full foe that falls may burst as a spore bomb (Viral Outbreak). A foe
// that falls with any stacks may rise as a Cordyfriend (Cordyceps).
public static class YosseleSpores
{
    public class State
    {
        public int Stacks;
        public float Last, NextHallu, Guard;
        public YosseleCannonBrain Cannon;
        public Link_Modifier Sticky;
        public bool Full;
    }

    static readonly Dictionary<Entity_Master, State> spored = new Dictionary<Entity_Master, State>();
    static readonly List<Entity_Master> bursts = new List<Entity_Master>();
    static float nextTick;
    public static int Ticks, Fulls, Finishes, Stuns, Virals, Stuck, Rises;

    public static void Reset()
    {
        spored.Clear();
        bursts.Clear();
    }

    public static int StacksOf(Entity_Master foe)
    {
        State s;
        return foe != null && spored.TryGetValue(foe, out s) ? s.Stacks : 0;
    }

    public static bool IsFull(Entity_Master foe)
    {
        State s;
        return foe != null && spored.TryGetValue(foe, out s) && s.Full;
    }

    public static State Of(Entity_Master foe)
    {
        State s;
        return foe != null && spored.TryGetValue(foe, out s) ? s : null;
    }

    public static void Add(Entity_Master foe, int stacks, YosseleCannonBrain cannon)
    {
        if (foe == null || cannon == null || stacks <= 0 || !YosseleUtil.Alive(foe))
            return;
        State s;
        if (!spored.TryGetValue(foe, out s))
            spored[foe] = s = new State { NextHallu = Time.time + 1f };
        s.Cannon = cannon;
        s.Last = Time.time;
        int max = cannon.SporeMax;
        // Deathcap's lower cap never takes stacks away: a foe above it keeps its stacks (the sheet).
        if (s.Stacks < max)
            s.Stacks = Mathf.Min(max, s.Stacks + stacks);
        YosseleUtil.Debuff(cannon.Master, foe, YosseleData.VfxSpores, YosseleData.SporeFade);
        if (s.Stacks >= max)
        {
            if (!s.Full)
            {
                s.Full = true;
                Fulls++;
                s.NextHallu = Time.time + cannon.Param("Hallu_Every", 5f);
            }
            YosseleUtil.Debuff(cannon.Master, foe, YosseleData.VfxSporefull, YosseleData.SporeFade);
            if (cannon.Has("Sticky") && (s.Sticky == null || s.Sticky.Dead))
            {
                s.Sticky = YosseleUtil.SlowOn(foe, cannon.Param("Sticky_Slow", 0.25f));
                Stuck++;
            }
            if (cannon.Has("Finish") && Time.time >= s.Guard && !bursts.Contains(foe))
                bursts.Add(foe);
        }
    }

    // Takes stacks off a foe without bursting it (Mother of Mushrooms); the next tick clears a foe left with none.
    public static void Take(Entity_Master foe, int stacks)
    {
        State s;
        if (foe == null || stacks <= 0 || !spored.TryGetValue(foe, out s))
            return;
        s.Stacks = Mathf.Max(0, s.Stacks - stacks);
        if (!s.Full)
            return;
        s.Full = false;
        YosseleUtil.Unmod(s.Sticky);
        s.Sticky = null;
        if (s.Cannon != null && s.Cannon.Master != null)
            YosseleUtil.RemoveDebuff(s.Cannon.Master, foe, YosseleData.VfxSporefull);
    }

    public static void Update()
    {
        // Explosive Finish bursts are queued, not recursive: a burst that fills its neighbours queues theirs.
        for (int guard = 0; bursts.Count > 0 && guard < 64; guard++)
        {
            Entity_Master foe = bursts[0];
            bursts.RemoveAt(0);
            try { Burst(foe); }
            catch (Exception e) { YosseleLog.Warn("spore burst: " + e.Message); }
        }
        if (Time.time < nextTick)
            return;
        nextTick = Time.time + YosseleData.SporeTick;
        Ticks++;
        List<Entity_Master> drop = null;
        // A copy: a spore tick can fell a foe, and its despawn (YosseleSpores.Fell) changes the table.
        foreach (KeyValuePair<Entity_Master, State> kv in new List<KeyValuePair<Entity_Master, State>>(spored))
        {
            Entity_Master foe = kv.Key;
            State s = kv.Value;
            if (foe == null || !YosseleUtil.Alive(foe) || s.Cannon == null || s.Cannon.Master == null || s.Stacks <= 0
                || Time.time - s.Last > YosseleData.SporeFade)
            {
                if (drop == null) drop = new List<Entity_Master>();
                drop.Add(foe);
                continue;
            }
            YosseleUtil.Hurt(s.Cannon.Master, foe, s.Stacks * s.Cannon.SporeDps * YosseleData.SporeTick);
            if (s.Full && s.Cannon.Has("Hallucinogen") && Time.time >= s.NextHallu)
            {
                s.NextHallu = Time.time + s.Cannon.Param("Hallu_Every", 5f);
                if (UnityEngine.Random.value < s.Cannon.Param("Hallu_Chance", 0.2f))
                    Stun(foe, s.Cannon);
            }
        }
        if (drop != null)
            foreach (Entity_Master foe in drop)
                Clear(foe);
    }

    public static void Clear(Entity_Master foe)
    {
        State s;
        if (!spored.TryGetValue(foe, out s))
            return;
        spored.Remove(foe);
        YosseleUtil.Unmod(s.Sticky);
        if (foe != null && s.Cannon != null && s.Cannon.Master != null && YosseleUtil.Alive(foe))
        {
            YosseleUtil.RemoveDebuff(s.Cannon.Master, foe, YosseleData.VfxSpores);
            YosseleUtil.RemoveDebuff(s.Cannon.Master, foe, YosseleData.VfxSporefull);
        }
    }

    // Explosive Finish: the foe's stacks are consumed; it and every foe within Finish_Radius take Finish_Per per stack
    // consumed, and the others get Finish_Spores stacks. A foe can't burst again for 1 s.
    static void Burst(Entity_Master foe)
    {
        State s;
        if (foe == null || !spored.TryGetValue(foe, out s) || s.Cannon == null || s.Cannon.Master == null || !YosseleUtil.Alive(foe))
            return;
        YosseleCannonBrain cannon = s.Cannon;
        int consumed = s.Stacks;
        s.Stacks = 0;
        s.Full = false;
        s.Guard = Time.time + YosseleData.BurstGuard;
        YosseleUtil.Unmod(s.Sticky);
        s.Sticky = null;
        YosseleUtil.RemoveDebuff(cannon.Master, foe, YosseleData.VfxSporefull);
        Finishes++;
        Vector3 at = YosseleUtil.Pos(foe);
        float radius = cannon.Param("Finish_Radius", 1.25f);
        float damage = cannon.Param("Finish_Per", 1f) * consumed;
        YosseleUtil.Vis(cannon.Master, "Yossele_Finish", at, radius);
        foreach (Entity_Master other in YosseleUtil.FoesNear(cannon.Master, at, radius))
        {
            YosseleUtil.Hurt(cannon.Master, other, damage);
            if (other != foe && YosseleUtil.Alive(other))
                Add(other, Mathf.RoundToInt(cannon.Param("Finish_Spores", 2f)), cannon);
        }
    }

    // Hallucinogen: stunned (held, no attacks) for Hallu_Stun seconds, with Yossele's own daze look (a purple swirl and
    // stars; the stock Debuff_Stun isn't loaded on every map).
    public static void Stun(Entity_Master foe, YosseleCannonBrain cannon)
    {
        YosseleUtil.Root(cannon.Master, foe, cannon.Param("Hallu_Stun", 2f), true, YosseleData.VfxDaze);
        Stuns++;
    }

    // A spored foe fell (YosseleServer): Viral Outbreak may burst it as a spore bomb (15 damage within 2 tiles, 1
    // stack), and Cordyceps may raise it as a Cordyfriend.
    public static void Fell(Entity_Master foe, Vector3 at)
    {
        State s;
        if (foe == null || !spored.TryGetValue(foe, out s))
            return;
        spored.Remove(foe);
        YosseleCannonBrain cannon = s.Cannon;
        if (cannon == null || cannon.Master == null || s.Stacks <= 0)
            return;
        if (s.Full && cannon.Has("Viral") && UnityEngine.Random.value < cannon.Param("Viral_Chance", 0.3f))
        {
            Virals++;
            cannon.Blast(at, cannon.Param("Viral_Radius", 2f), cannon.Param("Viral_Damage", 15f), Mathf.RoundToInt(cannon.Param("Viral_Spores", 1f)));
        }
        if (UnityEngine.Random.value < cannon.CordyChance(s.Stacks) && YosseleFriends.Raise(cannon, at))
            Rises++;
    }
}
