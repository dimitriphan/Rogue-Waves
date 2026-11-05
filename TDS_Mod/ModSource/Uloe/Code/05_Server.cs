// Host-only rules. Everything that changes the game state happens here or in the brains, and reaches other players
// through the engine's own networking (spawns, stats, upgrades, commands, Vis RPCs).
//
// Light is a stat: each of Uloe's buildings has a Light stat, and each source re-adds its own short timed Add every second,
// removing its last first (UloeLight, pitfall #119; RogueLab R3 proved two sources sum and it fades 1.5 s after the last).
// Blind and Illuminate are per foe on the host (UloeMarks).
public class UloePlayer
{
    public long Owner;
    public int Extension, Bright, Batteries, Flash;   // Direction ranks the vessel holds: its brain refreshes them
    public UloeHeroBrain Hero;
}

// One source's share of Light: what it holds on each tower it lights. 'by' is the source's owner: a tower of another
// player on the team (a teammate's Uloe) takes at most TeamLightCap from all such sources together (scope 10 "Sharing",
// decision 18); -1 is a source of the tower's own owner.
public class UloeLight
{
    readonly Dictionary<Entity_Master, Share> held = new Dictionary<Entity_Master, Share>();
    static readonly List<Entity_Master> gone = new List<Entity_Master>();

    // What a source holds on a tower: its modifier and the Light it adds; a teammate's share is also on the tower's ledger.
    class Share { public Link_Modifier Mod; public float Amount; public bool Teammate; }
    static readonly Dictionary<Entity_Master, List<Share>> shared = new Dictionary<Entity_Master, List<Share>>();
    static readonly List<Entity_Master> sharedGone = new List<Entity_Master>();

    public void Feed(Entity_Master tower, float amount) { Feed(tower, amount, -1L, UloeData.LightHold); }
    public void Feed(Entity_Master tower, float amount, long by) { Feed(tower, amount, by, UloeData.LightHold); }

    public void Feed(Entity_Master tower, float amount, long by, float hold)
    {
        // This source's own last share goes, and leaves the ledger with its modifier, so it never counts against itself.
        Share old;
        List<Share> list;
        if (held.TryGetValue(tower, out old))
        {
            if (old.Mod != null && !old.Mod.Dead)
                old.Mod.Remove();
            if (old.Teammate && shared.TryGetValue(tower, out list))
                list.Remove(old);
            held.Remove(tower);
        }
        bool teammate = by >= 0 && by != ModKitUtil.Owner(tower);
        if (teammate)
            amount = Mathf.Min(amount, Mathf.Max(0f, UloeData.TeamLightCap - SharedOn(tower)));
        if (amount <= 0.001f)
            return;
        Share share = new Share { Mod = Helper_Entitytools.Addmod(tower, UloeData.LightStat, amount, Helper_Entitytools.Modtypes.Add, hold), Amount = amount, Teammate = teammate };
        held[tower] = share;
        if (teammate && share.Mod != null)
        {
            if (!shared.TryGetValue(tower, out list))
                shared[tower] = list = new List<Share>();
            list.Add(share);
        }
    }

    // The Light teammates' sources hold on a tower now; spent shares are dropped as it counts.
    public static float SharedOn(Entity_Master tower)
    {
        List<Share> list;
        if (tower == null || !shared.TryGetValue(tower, out list))
            return 0f;
        list.RemoveAll(x => x.Mod == null || x.Mod.Dead);
        float sum = 0f;
        foreach (Share x in list)
            sum += x.Amount;
        return sum;
    }

    // Forgets what has run out and what is gone; a source calls it once per tick.
    public void Sweep()
    {
        gone.Clear();
        foreach (KeyValuePair<Entity_Master, Share> kv in held)
            if (kv.Key == null || kv.Value.Mod == null || kv.Value.Mod.Dead)
                gone.Add(kv.Key);
        foreach (Entity_Master e in gone)
            held.Remove(e);
    }

    // The shared ledger, pruned of towers that are gone or hold nothing (UloeServer, a few times a second), or cleared.
    public static void SweepShared(bool all)
    {
        if (all)
        {
            shared.Clear();
            return;
        }
        sharedGone.Clear();
        foreach (KeyValuePair<Entity_Master, List<Share>> kv in shared)
            if (kv.Key == null || SharedOn(kv.Key) <= 0f)
                sharedGone.Add(kv.Key);
        foreach (Entity_Master e in sharedGone)
            shared.Remove(e);
    }

    public int Holding { get { return held.Count; } }
}

public static class UloeServer
{
    public static readonly List<UloeHeroBrain> Heroes = new List<UloeHeroBrain>();
    public static readonly List<UloeBuilding> Buildings = new List<UloeBuilding>();
    public static readonly List<UloePhotonBrain> Photons = new List<UloePhotonBrain>();
    static readonly Dictionary<long, UloePlayer> players = new Dictionary<long, UloePlayer>();
    static Game_Entity hookedEntity;
    static float nextMarks, nextShared;
    public static int Bursts, Shines;

    // Set just before a Power Bank spawns a Photon; read by OnSpawn (Onspawn fires synchronously inside Spawn).
    public static UloeBankBrain PendingHome;

    public static void Update()
    {
        Game_Entity ge = Game_Entity.Instance;
        if (ge != hookedEntity)
        {
            hookedEntity = ge;
            Heroes.Clear();
            Buildings.Clear();
            Photons.Clear();
            players.Clear();
            UloeMarks.Clear();
            UloeLight.SweepShared(true);
            ModKitUtil.ForgetLooks(true);
            if (ge != null)
            {
                ge.Onspawn += OnSpawn;
                ge.Ondespawn += OnDespawn;
            }
        }
        if (ge == null || !ge.IsServer)
            return;
        Heroes.RemoveAll(h => h == null);
        Buildings.RemoveAll(b => b == null);
        Photons.RemoveAll(p => p == null);
        try
        {
            if (Time.time >= nextMarks)
            {
                nextMarks = Time.time + 0.1f;
                UloeMarks.Tick();
            }
            if (Time.time >= nextShared)
            {
                nextShared = Time.time + 2f;
                UloeLight.SweepShared(false);
            }
        }
        catch (Exception e)
        {
            UloeLog.Warn("server: " + e.Message);
        }
    }

    // ---------------------------------------------------------------- players

    public static UloePlayer Player(long owner)
    {
        UloePlayer p;
        if (!players.TryGetValue(owner, out p))
        {
            p = new UloePlayer { Owner = owner };
            players[owner] = p;
        }
        return p;
    }

    public static UloeHeroBrain HeroOf(long owner)
    {
        foreach (UloeHeroBrain h in Heroes)
            if (h != null && h.Master != null && ModKitUtil.Owner(h.Master) == owner)
                return h;
        return null;
    }

    public static List<T> Owned<T>(long owner) where T : UloeBuilding
    {
        List<T> list = new List<T>();
        foreach (UloeBuilding b in Buildings)
            if (b is T && b.Started && b.Master != null && b.Owner == owner)
                list.Add((T)b);
        return list;
    }

    public static int PhotonsOf(UloeBankBrain home)
    {
        int n = 0;
        foreach (UloePhotonBrain p in Photons)
            if (p != null && p.Master != null && p.Home == home)
                n++;
        return n;
    }

    // ---------------------------------------------------------------- Light

    // One source's tick in a radius: every lightable building of its team there gets 'amount' from it, a teammate's at most
    // TeamLightCap from teammates all told (UloeLight). 'except' never does; relays skip a Mirror that relays too (two would
    // feed each other).
    public static void Shine(UloeLight light, Entity_Master from, Vector3 at, float radius, float amount, Entity_Master except, bool skipRelays = false, float hold = UloeData.LightHold)
    {
        if (from == null || amount <= 0f)
            return;
        int team = ModKitUtil.Team(from);
        long by = ModKitUtil.Owner(from);
        foreach (UloeBuilding b in Buildings)
        {
            if (b == null || !b.Started || b.Master == null || b.Master == except || ModKitUtil.Team(b.Master) != team || !ModKitUtil.Alive(b.Master))
                continue;
            if (ModKitUtil.Flat(b.Pos, at) > radius)
                continue;
            UloeMirrorBrain mirror = b as UloeMirrorBrain;
            if (skipRelays && mirror != null && mirror.Has("Bounce"))
                continue;
            light.Feed(b.Master, amount, by, hold);
            Shines++;
        }
        light.Sweep();
    }

    // The nearest of one player's buildings to a point, within a reach; 'except' left out. For a Flood Light with nothing
    // picked: its own player's, never a teammate's (ULO-3).
    public static UloeBuilding NearestBuilding(long owner, Vector3 at, float reach, Entity_Master except)
    {
        UloeBuilding best = null;
        float bestD = reach;
        foreach (UloeBuilding b in Buildings)
        {
            if (b == null || !b.Started || b.Master == null || b.Master == except || b.Owner != owner || !ModKitUtil.Alive(b.Master))
                continue;
            float d = ModKitUtil.Flat(b.Pos, at);
            if (d <= bestD)
            {
                best = b;
                bestD = d;
            }
        }
        return best;
    }

    // A Flood Light of this owner whose line of light passes within PathGrab of a point, for a Photon to follow; null if none.
    public static UloeFloodBrain PathNear(long owner, Vector3 at)
    {
        foreach (UloeFloodBrain f in Owned<UloeFloodBrain>(owner))
            if (f.Directed && UloeUtil.SegmentDistance(at, f.Pos, f.LineEnd) <= UloeData.PathGrab)
                return f;
        return null;
    }

    // ---------------------------------------------------------------- spawns

    static void OnSpawn(Entity_Master master)
    {
        try
        {
            if (!ModKitUtil.IsServer || master == null || master.Beingserver == null || master.Beingserver.Being == null)
                return;
            string being = master.Beingserver.Being.Objectname;
            if (being == UloeData.Hero)
            {
                master.gameObject.AddComponent<UloeHeroBrain>().Master = master;
                return;
            }
            if (being == UloeData.Photon)
            {
                UloePhotonBrain brain = master.gameObject.AddComponent<UloePhotonBrain>();
                brain.Master = master;
                brain.Home = PendingHome;
                PendingHome = null;
                Photons.Add(brain);
                return;
            }
            UloeData.Build b = UloeData.BuildFor(being);
            if (b == null)
                return;
            UloeBuilding building;
            if (b.Id == UloeData.Glass) building = master.gameObject.AddComponent<UloeGlassBrain>();
            else if (b.Id == UloeData.Bank) building = master.gameObject.AddComponent<UloeBankBrain>();
            else if (b.Id == UloeData.Mirror) building = master.gameObject.AddComponent<UloeMirrorBrain>();
            else building = master.gameObject.AddComponent<UloeFloodBrain>();
            building.Master = master;
            building.Def = b;
            Buildings.Add(building);
        }
        catch (Exception e)
        {
            UloeLog.Warn("spawn hook: " + e.Message);
        }
    }

    static void OnDespawn(Entity_Master master, bool killed, string reason)
    {
        try
        {
            if (!ModKitUtil.IsServer || master == null)
                return;
            UloeBuilding building = master.GetComponent<UloeBuilding>();
            if (building != null)
            {
                Buildings.Remove(building);
                building.Gone(killed, reason);
                return;
            }
            UloePhotonBrain photon = master.GetComponent<UloePhotonBrain>();
            if (photon != null)
            {
                Photons.Remove(photon);
                photon.Gone(killed, reason);
                return;
            }
            UloeHeroBrain hero = master.GetComponent<UloeHeroBrain>();
            if (hero != null)
            {
                Heroes.Remove(hero);
                if (killed)
                    ModKitUtil.Vis(master, "Uloe_Herodeath", ModKitUtil.Pos(master), 0);
                return;
            }
            UloeMarks.Forget(master);
        }
        catch (Exception e)
        {
            UloeLog.Warn("despawn hook: " + e.Message);
        }
    }
}

// Blind and Illuminate on the host: one level of each per foe at a time. A stronger one replaces a weaker, a weaker does
// nothing while a stronger is on, the same level refreshes in its last second. Blind is a timed Addmod on the foe's Damage,
// Illuminate a hit hook; the stock scripts (Barrel_Boost, Merchut) logged an exception per marked foe that fell
// (IMPLEMENTATION_NOTES.md, "Decided after round 1").
public class UloeMark
{
    public Entity_Master Foe;
    public int Blind, Lit;
    public float BlindUntil, LitUntil;
    public Link_Modifier BlindMod;
    public Entity_Master.Oninteracts Hook;
    public float HookMulti;
    public bool HookOn;

    // On its own copy: a caller may hand the same Data to every foe it hits, and the share must not pile up.
    public void OnHit(Link_Interact hit)
    {
        float m;
        if (hit == null || hit.Type != "Hurt" || hit.Data == null || !hit.Data.ContainsKey("Damage"))
            return;
        if (!hit.Data.TryGetValue("Damagemulti", out m))
            m = 1f;
        ModKitCombat.SetMulti(hit, m + HookMulti);
    }
}

public static class UloeMarks
{
    public static int Blinds, Illuminations;
    static readonly Dictionary<Entity_Master, UloeMark> marks = new Dictionary<Entity_Master, UloeMark>();
    static readonly List<Entity_Master> done = new List<Entity_Master>();

    public static void Clear() { marks.Clear(); }

    // Self-test: whether an entry is still kept for this foe, destroyed or not (BlindLevel reads 0 on a destroyed one).
    public static bool Holds(Entity_Master foe) { return !ReferenceEquals(foe, null) && marks.ContainsKey(foe); }

    static UloeMark Get(Entity_Master foe)
    {
        UloeMark m;
        if (!marks.TryGetValue(foe, out m))
            marks[foe] = m = new UloeMark { Foe = foe };
        return m;
    }

    public static int BlindLevel(Entity_Master foe)
    {
        UloeMark m;
        return foe != null && marks.TryGetValue(foe, out m) && m.BlindUntil > Time.time ? m.Blind : 0;
    }

    public static int LitLevel(Entity_Master foe)
    {
        UloeMark m;
        return foe != null && marks.TryGetValue(foe, out m) && m.LitUntil > Time.time ? m.Lit : 0;
    }

    static bool Takes(int have, float until, int level)
    {
        float now = Time.time;
        if (have <= 0 || until <= now)
            return true;
        if (have > level)
            return false;
        return have < level || until - now <= UloeData.MarkRefresh;
    }

    // Blinds a foe at a level (1-3). 'radio' is whoever carries the look to every player.
    public static void Blind(Entity_Master radio, Entity_Master foe, int level)
    {
        if (!ModKitUtil.Alive(foe) || foe.Statsserver == null || !foe.Statsserver.ContainsKey("Damage"))
            return;
        level = Mathf.Clamp(level, 1, UloeData.BlindMulti.Length);
        UloeMark m = Get(foe);
        if (!Takes(m.Blind, m.BlindUntil, level))
            return;
        float secs = UloeData.MarkSeconds[level - 1];
        TakeOffBlind(m);
        m.BlindMod = Helper_Entitytools.Addmod(foe, "Damage", -UloeData.BlindMulti[level - 1], Helper_Entitytools.Modtypes.Multi, secs);
        m.Blind = level;
        m.BlindUntil = Time.time + secs;
        Blinds++;
        ModKitUtil.Debuff(radio, foe, "Debuff_Uloe_Blind", secs);
    }

    // Illuminates a foe at a level (1-3).
    public static void Illuminate(Entity_Master radio, Entity_Master foe, int level)
    {
        if (!ModKitUtil.Alive(foe))
            return;
        level = Mathf.Clamp(level, 1, UloeData.LitMulti.Length);
        UloeMark m = Get(foe);
        if (!Takes(m.Lit, m.LitUntil, level))
            return;
        float secs = UloeData.MarkSeconds[level - 1];
        TakeOffLit(m);
        if (m.Hook == null) m.Hook = m.OnHit;
        m.HookMulti = UloeData.LitMulti[level - 1];
        m.HookOn = true;
        ModKitUtil.Front(foe, m.Hook);
        m.Lit = level;
        m.LitUntil = Time.time + secs;
        Illuminations++;
        ModKitUtil.Debuff(radio, foe, "Debuff_Uloe_Lit", secs);
    }

    static void TakeOffBlind(UloeMark m)
    {
        if (m.BlindMod != null && !m.BlindMod.Dead)
            m.BlindMod.Remove();
        m.BlindMod = null;
    }

    static void TakeOffLit(UloeMark m)
    {
        if (m.HookOn)
        {
            ModKitUtil.Unfront(m.Foe, m.Hook);
            m.HookOn = false;
        }
    }

    // Ten times a second: Illuminate is taken off when its time is up, its hook stays at the front of its chain, and Blind's
    // level is forgotten when it wore off (its Addmod removes itself).
    public static void Tick()
    {
        float now = Time.time;
        done.Clear();
        foreach (KeyValuePair<Entity_Master, UloeMark> kv in marks)
        {
            UloeMark m = kv.Value;
            if (kv.Key == null || !ModKitUtil.Alive(kv.Key))
            {
                done.Add(kv.Key);
                continue;
            }
            if (m.Lit > 0 && m.LitUntil <= now)
            {
                TakeOffLit(m);
                m.Lit = 0;
            }
            else if (m.HookOn)
                ModKitUtil.Front(kv.Key, m.Hook);
            if (m.Blind > 0 && m.BlindUntil <= now)
                m.Blind = 0;
            if (m.Lit == 0 && m.Blind == 0)
                done.Add(kv.Key);
        }
        foreach (Entity_Master e in done)
            marks.Remove(e);
    }

    public static void Forget(Entity_Master foe)
    {
        if (foe != null)
            marks.Remove(foe);
    }
}
