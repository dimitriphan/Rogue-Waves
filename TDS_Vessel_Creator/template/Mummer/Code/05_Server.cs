// Host-only rules. Everything that changes the game state happens here or in the brains, and reaches other players
// through the engine's own networking (spawns, stats, skins, upgrades, commands, debuff effects, Vis RPCs).
public class MummerPlayer
{
    public long Owner;
    public int Thread = MummerData.ThreadStart;
    public bool Ballista, Aegis;
    public MummerHeroBrain Hero;

    public bool Has(MummerData.Costume c) { return c == MummerData.Costume.Plain || (c == MummerData.Costume.Ballista && Ballista) || (c == MummerData.Costume.Aegis && Aegis); }
}

public static class MummerServer
{
    public static int Tide;
    public static readonly List<MummerHeroBrain> Heroes = new List<MummerHeroBrain>();
    public static readonly List<MummerBuilding> Buildings = new List<MummerBuilding>();
    public static readonly List<MummerPuppetBrain> Puppets = new List<MummerPuppetBrain>();
    static readonly Dictionary<long, MummerPlayer> players = new Dictionary<long, MummerPlayer>();
    static readonly Dictionary<Entity_Master, MarkInfo> marks = new Dictionary<Entity_Master, MarkInfo>();
    public static int MarksPaid;
    static Game_Entity hookedEntity;
    static Game_Mapvars hookedMapvars;

    // Set just before a Stage spawns an Understudy; read by OnSpawn (Onspawn fires synchronously inside Spawn).
    public static MummerStageBrain PendingStage;
    public static MummerData.Costume PendingCostume;

    class MarkInfo
    {
        public long Owner;
        public float Until;
    }

    public static void Update()
    {
        Game_Entity ge = Game_Entity.Instance;
        if (ge != hookedEntity)
        {
            hookedEntity = ge;
            Heroes.Clear();
            Buildings.Clear();
            Puppets.Clear();
            players.Clear();
            marks.Clear();
            MummerUtil.ForgetLooks(true);
            if (ge != null)
            {
                ge.Onspawn += OnSpawn;
                ge.Ondespawn += OnDespawn;
            }
        }
        if (ge == null || !ge.IsServer)
            return;
        Game_Mapvars mv = Game_Mapvars.Instance;
        if (mv != hookedMapvars)
        {
            hookedMapvars = mv;
            if (mv != null)
            {
                Tide = mv.Wave.Value;
                mv.Wave.OnValueChanged += OnWave;
            }
        }
        Heroes.RemoveAll(h => h == null);
        Buildings.RemoveAll(b => b == null);
        Puppets.RemoveAll(p => p == null);
    }

    // ---------------------------------------------------------------- players and Thread

    public static MummerPlayer Player(long owner)
    {
        MummerPlayer p;
        if (!players.TryGetValue(owner, out p))
        {
            p = new MummerPlayer { Owner = owner };
            players[owner] = p;
        }
        return p;
    }

    public static void AddThread(long owner, int amount)
    {
        if (amount == 0)
            return;
        MummerPlayer p = Player(owner);
        p.Thread = Mathf.Clamp(p.Thread + amount, 0, MummerData.ThreadMax);
        if (p.Hero != null)
            p.Hero.ShowThread();
    }

    public static bool SpendThread(long owner, int amount)
    {
        MummerPlayer p = Player(owner);
        if (p.Thread < amount)
            return false;
        AddThread(owner, -amount);
        return true;
    }

    static void Unlock(long owner, MummerData.Costume c, Entity_Master at)
    {
        MummerPlayer p = Player(owner);
        if (p.Has(c))
            return;
        if (c == MummerData.Costume.Ballista) p.Ballista = true;
        if (c == MummerData.Costume.Aegis) p.Aegis = true;
        Entity_Master radio = p.Hero != null && p.Hero.Master != null ? p.Hero.Master : at;
        MummerUtil.Vis(radio, "Mummer_Costume", MummerUtil.Pos(radio), (float)c);
        MummerUtil.Warn(radio, c == MummerData.Costume.Ballista ? MummerData.WarnUnlockBallista : MummerData.WarnUnlockAegis);
        foreach (MummerBuilding b in Buildings)
            if (b is MummerStageBrain && MummerUtil.Owner(b.Master) == owner)
                ((MummerStageBrain)b).CostumesChanged();
        MummerLog.Info("player " + owner + " unlocked the " + c + " costume");
    }

    // ---------------------------------------------------------------- Marks

    public static void Mark(Entity_Master foe, long owner, float seconds, Entity_Master by)
    {
        if (foe == null || !MummerUtil.Alive(foe) || seconds <= 0f)
            return;
        marks[foe] = new MarkInfo { Owner = owner, Until = Time.time + seconds };
        MummerUtil.Debuff(by, foe, MummerData.VfxMarked, seconds);
    }

    public static bool IsMarked(Entity_Master foe)
    {
        MarkInfo m;
        return foe != null && marks.TryGetValue(foe, out m) && m.Until > Time.time;
    }

    // Puppets and the Cross go for Marked foes first.
    public static Entity_Master Pick(List<Entity_Master> foes)
    {
        foreach (Entity_Master f in foes)
            if (IsMarked(f))
                return f;
        return foes.Count > 0 ? foes[0] : null;
    }

    // ---------------------------------------------------------------- foe families

    public static MummerData.Costume Family(Entity_Master foe)
    {
        if (foe.GetComponent<Entity_Ai_Enemy_Ballista>() != null || foe.GetComponent<Entity_Ai_Enemy_Onager>() != null
            || foe.GetComponent<Entity_Ai_Enemy_Excalibur>() != null || foe.GetComponent<Entity_Ai_Enemy_Bulwark>() != null)
            return MummerData.Costume.Ballista;
        if (foe.GetComponent<Entity_Ai_Enemy_Aegis>() != null || foe.GetComponent<Entity_Ai_Enemy_Maul>() != null || foe.GetComponent<Entity_Ai_Enemy_Halberd>() != null)
            return MummerData.Costume.Aegis;
        string name = MummerUtil.BeingName(foe);
        foreach (string n in MummerData.RangedFoes) if (name.Contains(n)) return MummerData.Costume.Ballista;
        foreach (string n in MummerData.HeavyFoes) if (name.Contains(n)) return MummerData.Costume.Aegis;
        return MummerData.Costume.Plain;
    }

    public static bool TooHeavy(Entity_Master foe) { return MummerUtil.BaseHealth(foe) >= MummerData.BossHealth; }

    // ---------------------------------------------------------------- spawns

    static void OnSpawn(Entity_Master master)
    {
        try
        {
            if (!MummerUtil.IsServer || master == null || master.Beingserver == null || master.Beingserver.Being == null)
                return;
            string being = master.Beingserver.Being.Objectname;
            if (being == MummerData.Hero)
            {
                master.gameObject.AddComponent<MummerHeroBrain>().Master = master;
                return;
            }
            MummerData.Puppet puppet = MummerData.PuppetFor(being);
            if (puppet != null)
            {
                MummerPuppetBrain brain = master.gameObject.AddComponent<MummerPuppetBrain>();
                brain.Master = master;
                brain.Def = puppet;
                brain.Stage = PendingStage;
                PendingStage = null;
                Puppets.Add(brain);
                return;
            }
            MummerData.Build b = MummerData.BuildFor(being);
            if (b == null)
                return;
            MummerBuilding building;
            if (b.Id == MummerData.Loom) building = master.gameObject.AddComponent<MummerLoomBrain>();
            else if (b.Id == MummerData.Stage) building = master.gameObject.AddComponent<MummerStageBrain>();
            else if (b.Id == MummerData.Cross) building = master.gameObject.AddComponent<MummerCrossBrain>();
            else building = master.gameObject.AddComponent<MummerCurtainBrain>();
            building.Master = master;
            building.Def = b;
            Buildings.Add(building);
        }
        catch (Exception e)
        {
            MummerLog.Warn("spawn hook: " + e.Message);
        }
    }

    static void OnDespawn(Entity_Master master, bool killed, string reason)
    {
        try
        {
            if (!MummerUtil.IsServer || master == null)
                return;
            MummerBuilding building = master.GetComponent<MummerBuilding>();
            if (building != null)
            {
                Buildings.Remove(building);
                if (killed)
                    MummerUtil.Vis(master, "Mummer_Towerdown", MummerUtil.Pos(master), 0);
                return;
            }
            MummerPuppetBrain puppet = master.GetComponent<MummerPuppetBrain>();
            if (puppet != null)
            {
                Puppets.Remove(puppet);
                if (killed)
                    puppet.Fell();
                return;
            }
            if (master.GetComponent<MummerHeroBrain>() != null)
            {
                if (killed)
                    MummerUtil.Vis(master, "Mummer_Herodeath", MummerUtil.Pos(master), 0);
                return;
            }
            if (killed && master.Taglistserver != null && master.Taglistserver.Contains("Enemy"))
                FoeFell(master);
            marks.Remove(master);
        }
        catch (Exception e)
        {
            MummerLog.Warn("despawn hook: " + e.Message);
        }
    }

    // A foe fell: every Mummer player with a Loom in range gets its Scraps (and a costume, the first time), whoever
    // Marked it gets a bonus, and a foe held by an Encore Cross is stitched into a free Understudy.
    static void FoeFell(Entity_Master foe)
    {
        Vector3 pos = MummerUtil.Pos(foe);
        float health = MummerUtil.BaseHealth(foe);
        int scrap = health >= MummerData.HugeHealth ? MummerData.ScrapHuge : health >= MummerData.BigHealth ? MummerData.ScrapBig : MummerData.ScrapSmall;
        MummerData.Costume family = Family(foe);
        Dictionary<long, MummerLoomBrain> best = new Dictionary<long, MummerLoomBrain>();
        foreach (MummerBuilding b in Buildings)
        {
            MummerLoomBrain loom = b as MummerLoomBrain;
            if (loom == null || !loom.Started || loom.Master == null)
                continue;
            float d = Vector3.Distance(MummerUtil.Pos(loom.Master), pos);
            if (d > loom.Scraprange)
                continue;
            long owner = MummerUtil.Owner(loom.Master);
            MummerLoomBrain have;
            if (!best.TryGetValue(owner, out have) || d < Vector3.Distance(MummerUtil.Pos(have.Master), pos))
                best[owner] = loom;
        }
        foreach (KeyValuePair<long, MummerLoomBrain> kv in best)
        {
            MummerLoomBrain loom = kv.Value;
            int n = scrap;
            if (loom.Has("Golden") && health >= MummerData.BigHealth)
                n = Mathf.Max(n, (int)loom.Param("Golden_Base", MummerData.ScrapGolden));
            if (loom.Has("Tapestry"))
                n *= 2;
            AddThread(kv.Key, n);
            loom.Scrapped++;
            MummerUtil.Vis(loom.Master, "Mummer_Scrap", pos, n);
            if (family != MummerData.Costume.Plain)
                Unlock(kv.Key, family, loom.Master);
        }
        MarkInfo m;
        if (marks.TryGetValue(foe, out m) && m.Until > Time.time)
        {
            AddThread(m.Owner, MummerData.MarkBonus);
            MarksPaid++;
            MummerPlayer p = Player(m.Owner);
            if (p.Hero != null && p.Hero.Master != null)
                MummerUtil.Vis(p.Hero.Master, "Mummer_Markpaid", pos, MummerData.MarkBonus);
        }
        foreach (MummerBuilding b in Buildings.ToArray())
        {
            MummerCrossBrain cross = b as MummerCrossBrain;
            if (cross != null && cross.Holds(foe) && cross.Has("Encore"))
            {
                MummerStageBrain stage = NearestStage(MummerUtil.Owner(cross.Master), pos, 12f);
                if (stage != null)
                {
                    stage.FreeStitches++;
                    MummerUtil.Vis(cross.Master, "Mummer_Encore", pos, 0);
                }
            }
        }
    }

    public static MummerStageBrain NearestStage(long owner, Vector3 pos, float range)
    {
        MummerStageBrain best = null;
        foreach (MummerBuilding b in Buildings)
        {
            MummerStageBrain s = b as MummerStageBrain;
            if (s == null || !s.Started || MummerUtil.Owner(s.Master) != owner)
                continue;
            float d = Vector3.Distance(MummerUtil.Pos(s.Master), pos);
            if (d <= range && (best == null || d < Vector3.Distance(MummerUtil.Pos(best.Master), pos)))
                best = s;
        }
        return best;
    }

    // ---------------------------------------------------------------- Tides

    static void OnWave(int previous, int current)
    {
        if (!MummerUtil.IsServer || current <= previous)
        {
            Tide = current;
            return;
        }
        Tide = current;
        try
        {
            if (previous >= 1)
                Unravel();   // not when the first Tide arrives: the starting Thread is for the first Stage
            foreach (MummerBuilding b in Buildings.ToArray())
                if (b != null) b.OnTide(current);
        }
        catch (Exception e)
        {
            MummerLog.Warn("tide " + current + ": " + e);
        }
    }

    // Half of each player's Thread unravels at the start of a Tide, unless one of their Looms spins Silk.
    static void Unravel()
    {
        foreach (MummerPlayer p in new List<MummerPlayer>(players.Values))
        {
            bool silk = false;
            foreach (MummerBuilding b in Buildings)
                if (b is MummerLoomBrain && b.Has("Silk") && MummerUtil.Owner(b.Master) == p.Owner)
                    silk = true;
            int lost = silk ? 0 : p.Thread / 2;
            if (lost <= 0)
                continue;
            AddThread(p.Owner, -lost);
            if (p.Hero != null && p.Hero.Master != null)
            {
                MummerUtil.Vis(p.Hero.Master, "Mummer_Unravel", MummerUtil.Pos(p.Hero.Master), lost);
                MummerUtil.Vis(p.Hero.Master, "Mummer_Warn", MummerUtil.Pos(p.Hero.Master), MummerData.WarnUnravel + lost * 100);
            }
        }
    }

    public static MummerHeroBrain HeroOf(long owner)
    {
        foreach (MummerHeroBrain h in Heroes)
            if (h != null && h.Master != null && MummerUtil.Owner(h.Master) == owner)
                return h;
        return null;
    }
}
