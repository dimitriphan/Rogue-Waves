// Claude's Directions for the base vessels that a held stat on the vessel can't carry (docs/scope/11, built 2026-11-05;
// compiled, not yet run in game). Host only, in a campaign map, on each holder's own things found by owner (kit docs/17
// §11.1 rule 2). RWDirections holds their sheets (16_Directions.cs); the ranks are read as they are needed.
//   Mason's Trowel (Smacky)  his walls: Health Pmulti +20 / 40 / 60%; rank III: 1 a second once 3 s pass with no hit
//   Snow Shovel (Sno Sno)    the snow on each tile he leaves: up to level 30 / 40 / 55. His own Truemove makes it 20
//                            (Entity_Ai_Hero_Snosno.cs:56-82) and runs him 75% faster on snow above 20, so his trail
//                            now speeds him the next time he crosses it.
//   Worm Tin (Boutman)       Boutbro's Fishcost, Barrelcost and Boatcost: Multi -10 / 20 / 30% (kit docs/17 §5.1, verified
//                            for Fishcost), then his own recounts, so the price follows at once
//   Open Sign (Fie)          a Rest Stop with Regulars keeps 5 / 6 / 7 regulars (the game's 4). Its count is read only when
//                            Regulars lands (Entity_Ai_Tower_Fieshop.cs:241-246), so Regulars lands once more with the count
//                            raised for that one call, its other effects put back (no System.Reflection: kit docs/01).
//   Long Leash (Buster)      rank I: +1 Range Add while he dashes (the game's -1, Entity_Ai_Hero_Buster.cs:44-55); rank II
//                            and III: his dash hits Damagemulti x1.15 / x1.3, on his side before the foe takes it
public static class RWBase
{
    public static readonly float[] TrowelHealth = { 0f, 0.2f, 0.4f, 0.6f };
    public const float TrowelMend = 1f, TrowelRest = 3f;
    public static readonly float[] ShovelLevel = { 20f, 30f, 40f, 55f };
    public static readonly float[] WormCut = { 0f, 0.1f, 0.2f, 0.3f };
    public static readonly int[] SignRegulars = { 4, 5, 6, 7 };
    public static readonly float[] LeashHit = { 0f, 0f, 0.15f, 0.3f };
    public const string Regulars = "Tower_Fieshop_Regular4_Upgradeobject", Buffet = "Tower_Fieshop_Buffet5_Upgradeobject";
    static readonly string[] WormStats = { "Fishcost", "Barrelcost", "Boatcost" };
    static readonly string[] Recounts = { "Countfish", "Countbarrel", "Countboat" };

    sealed class Wall { public float Health, Hurt, Next; }
    sealed class Left { public Genitem Block; public float Until, Level; }

    static readonly Dictionary<Entity_Master, Wall> walls = new Dictionary<Entity_Master, Wall>();
    static readonly Dictionary<long, Genitem> standing = new Dictionary<long, Genitem>();
    static readonly List<Left> left = new List<Left>();
    static readonly Dictionary<Entity_Master, int> signs = new Dictionary<Entity_Master, int>();
    static readonly Dictionary<long, int> leash = new Dictionary<long, int>();
    static readonly Entity_Master.Interacts dashHit = OnDash;
    static float nextSweep, nextWalls, nextHooks;
    public static int Topped, Opened, Recounted, Dashed;     // self-test
    public static float Mended;

    // A map starts (RWVote.MapStart). RWAny.Reset clears RWHold.
    public static void Reset()
    {
        walls.Clear();
        standing.Clear();
        left.Clear();
        signs.Clear();
        leash.Clear();
    }

    static int Top(RWPlayer p, string vessel, string name) { return Mathf.Clamp(RWDirections.Top(p, vessel, name), 0, 3); }

    public static void Update()
    {
        if (!RWRun.InCampaignMap || !ModKitUtil.IsServer || Game_Entity.Instance == null)
            return;
        float now = Time.time;
        bool hooks = now >= nextHooks;
        if (hooks) nextHooks = now + 0.1f;
        foreach (long s in RWRun.Sessions())
        {
            RWPlayer p = RWRun.For(s);
            Entity_Master v = RWRun.Vessel(s);
            int shovel = Top(p, "Snosno", "Shovel"), dash = Top(p, "Buster", "Leash");
            if (shovel > 0) Trail(s, v, shovel, now);
            leash[s] = dash;
            if (dash > 0 && ModKitUtil.Alive(v))
            {
                // Every frame: the game's -1 follows his state at each of its ticks.
                RWHold.Set(v, "Leash", "Range", Helper_Entitytools.Modtypes.Add, ModKitUtil.State(v) == "Movelinear" ? 1f : 0f);
                if (hooks && dash >= 2)
                    ModKitUtil.Listen(v, dashHit);
            }
        }
        Topping(now);
        if (now >= nextSweep)
        {
            nextSweep = now + 0.5f;
            Sweep(now);
        }
        if (now >= nextWalls)
        {
            nextWalls = now + 0.25f;
            Walls(now);
        }
    }

    // ---------------------------------------------------------------- Snow Shovel

    static void Trail(long s, Entity_Master v, int rank, float now)
    {
        if (!ModKitUtil.Alive(v) || v.Beingserver == null)
        {
            standing.Remove(s);
            return;
        }
        Genitem here = v.Beingserver.Currentblock, was;
        if (standing.TryGetValue(s, out was) && was != null && was != here)
            Leave(was, ShovelLevel[rank], now);
        standing[s] = here;
    }

    // The tile a Sno Sno left: its snow comes up to the rank's level once the snow is made (a second at most).
    public static void Leave(Genitem block, float level, float now)
    {
        if (block != null)
            left.Add(new Left { Block = block, Level = level, Until = now + 1f });
    }

    static void Topping(float now)
    {
        for (int i = left.Count - 1; i >= 0; i--)
        {
            Left l = left[i];
            Entity_Master snow = Helper_Entityai.Getfirstonblock(l.Block, "Sno");
            Link_Stats lv;
            // A new snow's Level is capped at what it was made with until its AI raises the cap to 100 (Link_Stats.Value).
            if (snow != null && snow.Statsserver != null && snow.Statsserver.TryGetValue("Level", out lv) && lv.Value_Calc >= l.Level)
            {
                if (lv.Value < l.Level)
                {
                    lv.Value = l.Level;
                    Topped++;
                }
                left.RemoveAt(i);
            }
            else if (now > l.Until)
                left.RemoveAt(i);
        }
    }

    // ---------------------------------------------------------------- Mason's Trowel, Worm Tin, Open Sign (twice a second)

    static void Sweep(float now)
    {
        foreach (long s in RWRun.Sessions())
        {
            RWPlayer p = RWRun.For(s);
            int trowel = Top(p, "Smacky", "Trowel"), worm = Top(p, "Boutman", "Wormtin"), sign = Top(p, "Fie", "Opensign");
            if (trowel > 0) Each("Smackywall", s, e => Trowel(e, trowel, now));
            if (worm > 0) Each("Boutbro", s, e => Worm(e, worm));
            if (sign > 0) Each("Tower", s, e => { if (e.Being.Value.ToString() == "Tower_Fieshop") Sign(e, sign); });
        }
    }

    static void Each(string tag, long owner, Action<Entity_Master> act)
    {
        List<Entity_Master> list;
        if (!Game_Entity.Instance.Entityclienttags.TryGetValue(tag, out list))
            return;
        foreach (Entity_Master e in new List<Entity_Master>(list))
            if (ModKitUtil.Alive(e) && e.Statsserver != null && e.Statsserver.Count > 0 && ModKitUtil.Owner(e) == owner && !e.Taglistserver.Contains("Clone") && !e.Taglistserver.Contains("Prespawn"))
                act(e);
    }

    static void Trowel(Entity_Master e, int rank, float now)
    {
        RWHold.Set(e, "Trowel", "Health", Helper_Entitytools.Modtypes.Pmulti, TrowelHealth[rank]);
        if (rank >= 3 && !walls.ContainsKey(e))
            walls[e] = new Wall { Health = ModKitUtil.Health(e), Hurt = now };
        else if (rank < 3)
            walls.Remove(e);
    }

    // Four times a second: a wall that lost health was hit (an uncredited hit leaves no Lasthittime, kit docs/17 §2.6);
    // one left alone 3 s mends 1 a second.
    static void Walls(float now)
    {
        if (walls.Count == 0)
            return;
        foreach (Entity_Master e in new List<Entity_Master>(walls.Keys))
        {
            Wall w = walls[e];
            if (!ModKitUtil.Alive(e))
            {
                walls.Remove(e);
                continue;
            }
            float hp = ModKitUtil.Health(e);
            if (hp < w.Health - 0.001f)
                w.Hurt = now;
            else if (now - w.Hurt >= TrowelRest && now >= w.Next)
            {
                float missing = ModKitUtil.MaxHealth(e) - hp;
                if (missing > 0.001f)
                {
                    float heal = Mathf.Min(missing, TrowelMend);
                    ModKitUtil.Heal(e, e, heal);
                    Mended += heal;
                    w.Next = now + 1f;
                }
            }
            w.Health = ModKitUtil.Health(e);
        }
    }

    static void Worm(Entity_Master bro, int rank)
    {
        bool changed = false;
        foreach (string stat in WormStats)
            changed |= RWHold.Set(bro, "Worm" + stat, stat, Helper_Entitytools.Modtypes.Multi, -WormCut[rank]);
        if (!changed || bro.Ais == null)
            return;
        foreach (Entity_Ai_Base ai in bro.Ais)
            if (ai != null && ai.GetType().Name == "Entity_Ai_Mob_Boutbro")
            {
                foreach (string count in Recounts)
                    ai.SendMessage(count, 0f, SendMessageOptions.DontRequireReceiver);
                Recounted++;
                break;
            }
    }

    static void Sign(Entity_Master shop, int rank)
    {
        int want = SignRegulars[rank], had;
        Upgradeproperties u;
        if (shop.Upgradelistserver == null || !shop.Upgradelistserver.TryGetValue(Regulars, out u) || u == null || u.Defaultparams == null
            || (signs.TryGetValue(shop, out had) && had == want))
            return;
        Entity_Ai_Tower_Fieshop ai = shop.GetComponent<Entity_Ai_Tower_Fieshop>();
        float count;
        if (ai == null || !u.Defaultparams.TryGetValue("Perm_Count", out count))
            return;
        // Regulars' Upgrade sets the count, adds its Duration hook again and sets the throw and its look: the hook chain
        // and the look are put back as they were, and Buffet's throw, which outranks Regulars', is landed again.
        Entity_Ai_Tower_Fieshop.Durations chain = ai.Duration;
        string look = ai.Effectvis;
        u.Defaultparams["Perm_Count"] = want;
        try { ai.Upgrade(u); }
        finally
        {
            u.Defaultparams["Perm_Count"] = count;
            ai.Duration = chain;
            ai.Effectvis = look;
        }
        Upgradeproperties buffet;
        if (shop.Upgradelistserver.TryGetValue(Buffet, out buffet) && buffet != null)
            ai.Upgrade(buffet);
        signs[shop] = want;
        Opened++;
        RWLog.Info("Open Sign: a Rest Stop keeps " + want + " regulars");
    }

    // ---------------------------------------------------------------- Long Leash (Buster's side of a hit)

    static void OnDash(Link_Interact hit)
    {
        try
        {
            int rank;
            if (hit == null || hit.Type != "Hurt" || hit.From == null || hit.Data == null || hit.Data.ContainsKey("Burn") || hit.Data.ContainsKey(RWAny.Extra)
                || !leash.TryGetValue(ModKitUtil.Owner(hit.From), out rank) || rank < 2 || RWRun.Vessel(ModKitUtil.Owner(hit.From)) != hit.From
                || ModKitUtil.State(hit.From) != "Movelinear")
                return;
            float multi;
            if (!hit.Data.TryGetValue("Damagemulti", out multi))
                return;
            // A copy: some AIs reuse one dictionary for several hits (pitfall #30).
            hit.Data = new Dictionary<string, float>(hit.Data);
            hit.Data["Damagemulti"] = multi * (1f + LeashHit[rank]);
            Dashed++;
        }
        catch (Exception e) { RWLog.Once("leash", "a dash hit: " + e.Message); }
    }
}
