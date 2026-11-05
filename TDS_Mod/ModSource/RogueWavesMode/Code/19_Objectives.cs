// Optional objectives (docs/scope/14; kit docs/15 §5): a new one is posted at VENDCINT every break, for the next Tide; any
// player can take it for the team, and it pays every player the same, full amount (the pack's list is a default for
// docs/scope/14's NEEDS line: docs/mode/MECHANIC_IDEAS.md §3, Mind the Meteor is the scope's own). The host decides; the
// board's text rides on each vessel's RW_Run line and its own None command.
public class RWObjectiveDef
{
    public string Name = "", Display = "", Line = "";
    public int Pays = 3;
}

public static class RWObjectives
{
    public const string Meteor = "RW_Meteor";
    public static readonly RWObjectiveDef[] All =
    {
        new RWObjectiveDef { Name = "Meteor", Display = "Mind the Meteor", Pays = 3, Line = "Fully mine a node that spawns in a random location on the map" },
        new RWObjectiveDef { Name = "Hull", Display = "Keep the Basin Clean", Pays = 2, Line = "No foe hurts the Basin this Tide" },
        new RWObjectiveDef { Name = "Clock", Display = "Beat the Clock", Pays = 3, Line = "Clear the Tide with time to spare" },
        new RWObjectiveDef { Name = "Hands", Display = "Mind the Vessels", Pays = 2, Line = "No vessel drowns this Tide" },
        new RWObjectiveDef { Name = "Nest", Display = "Raze the Nest", Pays = 4, Line = "A halberd nest lands near the foes: destroy it before the Tide ends" },
    };

    public static Entity_Master Target { get { return target; } }
    public static RWObjectiveDef Posted;     // this break's, on the host
    public static RWObjectiveDef Active;     // taken, running in the next Tide
    public static string Outcome = "";       // self-test
    public static int Paid, NoRoom;          // self-test
    public static Beingobject MeteorBeing;
    static Entity_Master target;
    static bool failed, targetFell;
    static float tideLength, clockLeft;
    static readonly Dictionary<Entity_Master, float> coreHurt = new Dictionary<Entity_Master, float>();   // each Basin's damage taken as the Tide began
    public const float ClockShare = 0.25f;   // of the Tide's clock left when its last foe falls

    public static void Build() { }

    // A map starts: nothing posted, taken or running carries over from the last one (MODE-D2).
    public static void Reset()
    {
        Posted = Active = null;
        target = null;
        failed = targetFell = false;
        tideLength = clockLeft = 0f;
        coreHurt.Clear();
    }

    // The ore node's group comes and goes with the map, so the copy is made again on each one, on every machine.
    static Game_Mapvars meteorMap;

    static void EnsureMeteor()
    {
        Game_Mapvars map = Game_Mapvars.Instance;
        if (map == null || ReferenceEquals(map, meteorMap) || !RWContent.Built)
            return;
        Beingobject ore = ModKitUtil.Load<Beingobject>("Mob", "Resource_Red") ?? ModKitUtil.Load<Beingobject>("Mob", "Resource_Blue") ?? ModKitUtil.Load<Beingobject>("Mob", "Resource_Yellow");
        if (ore == null || ore.Entity == null)
            return;
        meteorMap = map;
        MakeMeteor(ore);
    }

    // A copy of a stock ore node on the death node's brain (RogueLab R4, proven: it pays once, and dies, when emptied).
    static void MakeMeteor(Beingobject ore)
    {
        Beingobject death = UnityEngine.Object.Instantiate(ore);
        RWContent.Retag(death, Meteor);
        Entityobject ent = UnityEngine.Object.Instantiate(ore.Entity);
        RWContent.Retag(ent, Meteor + "_Entity");
        ent.Name = Meteor;
        ent.Display_Name = "Meteor";
        ent.Display_Desc = "A node with a white crystal. Mine it all";
        ent.Aiscripts = new List<UnityEngine.Object>();
        foreach (UnityEngine.Object o in ore.Entity.Aiscripts)
            ent.Aiscripts.Add(o != null && o.name.StartsWith("Entity_Ai_Resource_") ? RWContent.Script("Entity_Ai_Resource_Death") : o);
        Visualproperties vp = UnityEngine.Object.Instantiate(ore.Entity.Defaultvisual);
        RWContent.Retag(vp, Meteor + "_Visualproperties");
        Visualproperties.Entry e = vp.Default;
        e.Displayname = "Meteor";
        e.Displaycolor = new Color(0.957f, 0.933f, 0.973f, 0f);
        vp.Default = e;
        ent.Defaultvisual = vp;
        RWContent.Register("Mob", vp);
        death.Entity = ent;
        MeteorBeing = death;
        RWContent.Register("Mob", death);
        RWContent.Register("Mob", ent);
    }

    // ---------------------------------------------------------------- the break (host)

    public static void Post(int wave)
    {
        Posted = null;
        Active = null;
        if (!RWRun.InCampaignMap)
            return;
        System.Random rng = new System.Random(unchecked((int)RWCampaign.Seed) ^ (RWCampaign.Route.Count * 31) ^ (wave * 977));
        List<RWObjectiveDef> pool = new List<RWObjectiveDef>(All);
        if (RWWeather.Current == RWChart.NestingGround || !RWWeather.CanNest()) pool.RemoveAll(o => o.Name == "Nest");
        if (MeteorBeing == null) pool.RemoveAll(o => o.Name == "Meteor");
        Posted = pool[rng.Next(pool.Count)];
        RWLog.Info("posted: " + Posted.Display + " for Tide " + (wave + 1));
    }

    // What the board shows: the run line the host writes names it, and every machine has the list ("<name>|<line>").
    public static string PostedText(Entity_Master v)
    {
        Link_Commandref c;
        if (v == null || !RWMine.Command(v, RWRun.RunLink, out c))
            return "";
        string line = c.Goalstring.ToString();
        foreach (RWObjectiveDef o in All)
            if (line == "Posted: " + o.Display)
                return o.Display.ToUpperInvariant() + "|" + o.Line + RWCard.Separator + "Pays Pure Aura " + o.Pays + " to everyone";
        return "";
    }

    public static string Take(RWPlayer p)
    {
        if (Posted == null) return "Nothing posted";
        if (Active != null) return "Already taken";
        Active = Posted;
        RWLog.Info(p.Name + " took the objective " + Active.Display);
        RWRejoin.Say(p.Name + " took the objective: " + Active.Display + ".");
        return "";
    }

    // ---------------------------------------------------------------- the Tide (host)

    public static void Begin(int wave)
    {
        failed = false;
        targetFell = false;
        clockLeft = 0f;
        target = null;
        if (Active == null)
            return;
        coreHurt.Clear();
        foreach (Entity_Master c in RWUtil.PlayerCores())
            coreHurt[c] = c.Damagetaken;
        Game_Mapvars vars = Game_Mapvars.Instance;
        tideLength = vars != null ? vars.Time_Wave.Value : 0f;
        try
        {
            if (Active.Name == "Meteor") SpawnMeteor();
            else if (Active.Name == "Nest") SpawnNest();
        }
        catch (Exception e) { RWLog.Warn("objective " + Active.Display + ": " + e.Message); }
        if ((Active.Name == "Meteor" || Active.Name == "Nest") && target == null)
        {
            // A Tide's first foes can stand on the last free tile by the enemy core: the team is told (RWWeather.CanNest looked in the break).
            RWLog.Info(Active.Display + " could not place its target: it pays nothing and costs nothing");
            RWRejoin.Say(Active.Display + " found no room for its target this Tide. Nothing is lost.");
            NoRoom++;
            Active = null;
        }
    }

    public static void Update()
    {
        EnsureMeteor();
        // A Tide's row ends the moment its last foe falls (Mode_Normal.Run), and the break's 60 s are in Time_Wave at once, a frame
        // before ModKitTides.InBreak says so: a field clear with its clock still running never happens. So what counts is the clock's
        // last reading in the Tide's own row, read at the break (pitfall #142).
        Game_Mapvars vars = Game_Mapvars.Instance;
        if (Active != null && Active.Name == "Clock" && vars != null && TideRow(vars))
            clockLeft = vars.Time_Wave.Value;
    }

    static bool TideRow(Game_Mapvars vars)
    {
        List<Wavesobject.Waves> table = vars.Mode != null && vars.Mode.Enemies != null ? vars.Mode.Enemies.Wave : null;
        int row = vars.Entry.Value - 1;     // the row is Entry - 1 (pitfall #121)
        return table != null && row >= 0 && row < table.Count && table[row].Wave;
    }

    // The Tide's break began: did it hold?
    public static void Resolve()
    {
        if (Active == null)
            return;
        RWObjectiveDef a = Active;
        Active = null;
        bool ok = false;
        Entity_Master core = Core();
        switch (a.Name)
        {
            case "Meteor": case "Nest": ok = targetFell; break;
            case "Hull": ok = BasinClean(); break;
            case "Clock": ok = tideLength > 0f && clockLeft > tideLength * ClockShare; break;
            case "Hands": ok = !failed; break;
        }
        if (target != null && target.Beingserver != null && !target.Beingserver.Dead)
            Helper_Entitytools.Despawn(target.Beingserver, false, "RogueWaves");
        target = null;
        Outcome = a.Name + (ok ? " done" : " failed");
        if (!ok)
        {
            RWRejoin.Say(a.Display + " failed.");
            return;
        }
        Paid += a.Pays;
        RWRun.Pay(a.Pays, a.Display);
        if (core != null && core.Radio != null)
            core.Radio.Uiwarning_ClientRpc(a.Display.ToUpperInvariant() + " DONE", "RogueWaves_Board", 3f);
    }

    // A vessel fell: Mind the Vessels; the marked target fell: done.
    public static void OnDespawn(Entity_Master master, bool killed, string reason)
    {
        if (master == null || Active == null)
            return;
        if (master == target && killed)
            targetFell = true;
        // A player's own vessel: a Boutbro falling is part of Boutman's day (MODE-D3).
        if (killed && RWRun.IsVessel(master))
            failed = true;
    }

    static Entity_Master Core() { return RWUtil.PlayerCore(); }

    // Every Basin standing when the Tide began, unhurt and still standing: a two-Basin map (Splithead) has two to keep
    // clean (MODE-D8).
    static bool BasinClean()
    {
        if (coreHurt.Count == 0)
            return false;
        foreach (KeyValuePair<Entity_Master, float> kv in coreHurt)
            if (!ModKitUtil.Alive(kv.Key) || kv.Key.Damagetaken > kv.Value + 0.01f)
                return false;
        return true;
    }

    // Farther and tougher on later maps (docs/scope/14 Mind the Meteor's "Later maps").
    static void SpawnMeteor()
    {
        Entity_Master core = Core();
        if (core == null || MeteorBeing == null)
            return;
        int map = Mathf.Max(1, RWCampaign.Route.Count + (RWCampaign.WonThisMap ? 0 : 1));
        float want = 8f + map * 1.5f;
        Vector3 from = core.Entityposition.Value;
        Link_Being probe = new Link_Being { Being = MeteorBeing, Entity = MeteorBeing.Entity, Block = MeteorBeing.Block, Team = -1 };
        System.Random rng = new System.Random(unchecked((int)RWCampaign.Seed) ^ (map * 131) ^ (Game_Mapvars.Instance.Wave.Value * 17));
        List<Genitem> tiles = RWUtil.Ground();
        Genitem best = null;
        float bestScore = float.MaxValue;
        for (int tries = 0; tries < 120 && tiles.Count > 0; tries++)
        {
            Genitem g = tiles[rng.Next(tiles.Count)];
            Genitem stand = null;
            try { stand = Helper_Gridtools.Checkifstandable(g, probe); } catch (Exception) { }
            if (stand == null)
                continue;
            float score = Mathf.Abs(ModKitUtil.Flat(stand.Position, from) - want);
            if (score < bestScore) { bestScore = score; best = stand; }
        }
        if (best == null)
            return;
        Link_Spawn spawn = new Link_Spawn();
        spawn.Being = MeteorBeing;
        spawn.Genadd = best;
        spawn.Skin = "";
        spawn.Spawnstyle = Link_Spawn.Spawnstyles.Forced;
        spawn.Owner = -1;
        spawn.Team = -1;
        // The hit that takes it from 0 is the one that pays, so N hits need Healthresource N - 1 (pitfall #106).
        Dictionary<string, float> init = new Dictionary<string, float> { { "Healthresource", 12 + map * 3 } };
        Link_Being made = Helper_Entitytools.Spawn(spawn, best.Position, null, false, init);
        target = made != null ? made.Worldobject : null;
        RWLog.Info("meteor at " + RWUtil.V(best.Position) + ", " + RWUtil.F(ModKitUtil.Flat(best.Position, from)) + " from the Basin, " + (12 + map * 3) + " hits");
    }

    static void SpawnNest()
    {
        target = RWWeather.SpawnNestFor();
    }
}
