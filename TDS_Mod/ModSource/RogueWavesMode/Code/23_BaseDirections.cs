// Claude's Directions for the base vessels that a held stat on the vessel can't carry (docs/scope/11, built 2026-11-05;
// compiled, not yet run in game). Host only, in a campaign map, on each holder's own things found by owner (kit docs/17
// §11.1 rule 2). RWDirections holds their sheets (16_Directions.cs); the holders are read four times a second from
// RWAny's crews.
//   Mason's Trowel (Smacky)  his walls: Health Pmulti +20 / 40 / 60%; rank III: 1 a second once 3 s pass with no hit
//   Snow Shovel (Sno Sno)    the snow on each tile he leaves: up to level 30 / 40 / 55. His own Truemove makes it 20
//                            (Entity_Ai_Hero_Snosno.cs:56-82) and runs him 75% faster on snow above 20, so his trail
//                            now speeds him the next time he crosses it.
//   Worm Tin (Boutman)       Boutbro's Fishcost, Barrelcost and Boatcost: Multi -10 / 20 / 30% (kit docs/17 §5.1, verified
//                            for Fishcost), then his own recounts, so the price follows at once
//   Open Sign (Fie)          a Rest Stop with Regulars keeps 5 / 6 / 7 regulars (the game's 4). Its count is read only when
//                            Regulars lands (Entity_Ai_Tower_Fieshop.cs:241-246), so a copy of Regulars with the count
//                            raised lands on it once more, its other effects put back (no System.Reflection: kit docs/01).
//   Long Leash (Buster)      rank I: the game's -1 Range while he dashes (Entity_Ai_Hero_Buster.cs:44-55) met by a +1;
//                            rank II and III: his dash hits Damagemulti x1.15 / x1.3, on his side before the foe takes it
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
    static readonly string[] WormKeys = { "WormFishcost", "WormBarrelcost", "WormBoatcost" };   // RWHold's, one a stat
    static readonly string[] Recounts = { "Countfish", "Countbarrel", "Countboat" };

    sealed class Holder { public long Session; public Entity_Master Vessel; public int Trowel, Shovel, Worm, Sign, Leash; }
    sealed class Left { public Genitem Block; public float Until, Level; }
    sealed class Sign { public int Count; public Entity_Ai_Tower_Fieshop Ai; public object Chain; }

    static readonly List<Holder> holders = new List<Holder>();
    static readonly Dictionary<Entity_Master, RWUtil.Rest> walls = new Dictionary<Entity_Master, RWUtil.Rest>();
    static readonly Dictionary<long, Genitem> standing = new Dictionary<long, Genitem>();
    static readonly List<Left> left = new List<Left>();
    static readonly Dictionary<Entity_Master, Sign> signs = new Dictionary<Entity_Master, Sign>();
    static readonly Dictionary<int, Upgradeproperties> signSheets = new Dictionary<int, Upgradeproperties>();
    static readonly Dictionary<Entity_Master, Link_Modifier> dashMods = new Dictionary<Entity_Master, Link_Modifier>();
    static readonly Entity_Master.Interacts dashHit = OnDash;
    static readonly Entity_Master.Oninteracts wallHit = OnWallHit;
    static float nextHolders, nextSweep, nextWalls, nextHooks;
    public static int Topped, Opened, Recounted, Dashed;     // self-test
    public static float Mended;

    // A map starts (RWVote.MapStart). RWAny.Reset clears RWHold.
    public static void Reset()
    {
        holders.Clear();
        walls.Clear();
        standing.Clear();
        left.Clear();
        signs.Clear();
        dashMods.Clear();
    }

    public static void Update()
    {
        if (!RWRun.InCampaignMap || !ModKitUtil.IsServer || Game_Entity.Instance == null)
            return;
        float now = Time.time;
        if (now >= nextHolders)
        {
            nextHolders = now + 0.25f;
            Holders();
        }
        if (holders.Count == 0)
            return;
        bool hooks = now >= nextHooks;
        if (hooks) nextHooks = now + 0.1f;
        foreach (Holder h in holders)
        {
            if (h.Shovel > 0) Trail(h, now);
            if (h.Leash > 0 && ModKitUtil.Alive(h.Vessel))
            {
                RWHold.Set(h.Vessel, "Leash", "Range", Helper_Entitytools.Modtypes.Add, DashCut(h.Vessel));
                if (hooks && h.Leash >= 2)
                    ModKitUtil.Listen(h.Vessel, dashHit);
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

    // Who holds one, from RWAny's crews (ranks and vessel read there four times a second).
    static void Holders()
    {
        holders.Clear();
        foreach (long s in RWRun.Sessions())
        {
            RWAny.Crew c = RWAny.CrewOf(s);
            if (c == null || c.Player == null)
                continue;
            Holder h = new Holder
            {
                Session = s, Vessel = c.Vessel,
                Trowel = RWAny.Top(c.Player, "Smacky", "Trowel"), Shovel = RWAny.Top(c.Player, "Snosno", "Shovel"),
                Worm = RWAny.Top(c.Player, "Boutman", "Wormtin"), Sign = RWAny.Top(c.Player, "Fie", "Opensign"),
                Leash = RWAny.Top(c.Player, "Buster", "Leash"),
            };
            if (h.Trowel + h.Shovel + h.Worm + h.Sign + h.Leash > 0)
                holders.Add(h);
        }
    }

    // ---------------------------------------------------------------- Snow Shovel (every frame: he crosses a tile fast)

    static void Trail(Holder h, float now)
    {
        Entity_Master v = h.Vessel;
        if (!ModKitUtil.Alive(v) || v.Beingserver == null)
        {
            standing.Remove(h.Session);
            return;
        }
        Genitem here = v.Beingserver.Currentblock, was;
        if (standing.TryGetValue(h.Session, out was) && was != null && was != here)
            Leave(was, ShovelLevel[h.Shovel], now);
        standing[h.Session] = here;
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

    // ---------------------------------------------------------------- Long Leash rank I

    // The game's own -1 (Buster's Rangemod, set at each slow tick while he dashes and still on at the first tick after,
    // when he looks for a foe to bite) is found the first time he dashes and met every frame; until then his state stands
    // in for it.
    static float DashCut(Entity_Master v)
    {
        bool dashing = ModKitUtil.State(v) == "Movelinear";
        Link_Modifier m;
        if (!dashMods.TryGetValue(v, out m) || m == null || m.Dead)
        {
            m = null;
            List<Link_Modifier> mods;
            if (dashing && v.Modifiers != null && v.Modifiers.TryGetValue("Range", out mods))
                foreach (Link_Modifier x in mods)
                    if (x != null && !x.Dead && Mathf.Approximately(x.Value_Add, -1f)) { m = x; break; }
            if (m != null)
                dashMods[v] = m;
        }
        return m != null ? Mathf.Max(0f, -m.Value_Add) : (dashing ? 1f : 0f);
    }

    // ---------------------------------------------------------------- Mason's Trowel, Worm Tin, Open Sign (twice a second)

    static void Sweep(float now)
    {
        foreach (Holder h in holders)
        {
            if (h.Trowel > 0) Each("Smackywall", h.Session, e => Trowel(e, h.Trowel, now));
            if (h.Worm > 0) Each("Boutbro", h.Session, e => Worm(e, h.Worm));
            if (h.Sign > 0) Each("Tower", h.Session, e => { if (e.Being.Value.ToString() == "Tower_Fieshop") Open(e, h.Sign); });
        }
    }

    static void Each(string tag, long owner, Action<Entity_Master> act)
    {
        List<Entity_Master> list;
        if (!Game_Entity.Instance.Entityclienttags.TryGetValue(tag, out list))
            return;
        foreach (Entity_Master e in new List<Entity_Master>(list))
            if (ModKitUtil.Alive(e) && e.Statsserver != null && e.Statsserver.Count > 0 && ModKitUtil.Owner(e) == owner && !RWRun.IsCopy(e))
                act(e);
    }

    static void Trowel(Entity_Master e, int rank, float now)
    {
        RWHold.Set(e, "Trowel", "Health", Helper_Entitytools.Modtypes.Pmulti, TrowelHealth[rank]);
        if (rank < 3)
            return;
        if (!walls.ContainsKey(e))
            walls[e] = new RWUtil.Rest { Health = ModKitUtil.Health(e), Hurt = now };
        ModKitUtil.Front(e, wallHit);
    }

    // A hit on a mending wall (RWUtil.Hit, which Tar Bucket shares).
    static void OnWallHit(Link_Interact hit) { RWUtil.Hit(walls, hit); }

    // Four times a second: one left alone 3 s mends 1 a second. A drop in health counts as a hit too, for the moments the
    // hook is off (an upgrade landing rebuilds the chain, kit docs/17 §2.3, until the next sweep puts it back).
    static void Walls(float now)
    {
        if (walls.Count == 0)
            return;
        foreach (Entity_Master e in new List<Entity_Master>(walls.Keys))
        {
            if (!ModKitUtil.Alive(e))
            {
                walls.Remove(e);
                continue;
            }
            Mended += RWUtil.Mend(walls[e], e, now, TrowelRest, TrowelMend, false);
        }
    }

    // His AIs share his GameObject (Entity_Master.cs:424), so SendMessage reaches Boutbro's private recounts.
    static void Worm(Entity_Master bro, int rank)
    {
        bool changed = false;
        for (int i = 0; i < WormStats.Length; i++)
            changed |= RWHold.Set(bro, WormKeys[i], WormStats[i], Helper_Entitytools.Modtypes.Multi, -WormCut[rank]);
        if (!changed)
            return;
        foreach (string count in Recounts)
            bro.SendMessage(count, 0f, SendMessageOptions.DontRequireReceiver);
        Recounted++;
    }

    // Landed again whenever the count, the AI or its Duration chain is not the one this set: Regulars landing anew, or
    // another upgrade adding its own hook, puts the game's 4 back.
    static void Open(Entity_Master shop, int rank)
    {
        int want = SignRegulars[rank];
        Upgradeproperties u;
        Entity_Ai_Tower_Fieshop ai = shop.GetComponent<Entity_Ai_Tower_Fieshop>();
        if (ai == null || shop.Upgradelistserver == null || !shop.Upgradelistserver.TryGetValue(Regulars, out u) || u == null || u.Defaultparams == null || !u.Defaultparams.ContainsKey("Perm_Count"))
            return;
        Sign had;
        if (signs.TryGetValue(shop, out had) && had.Count == want && had.Ai == ai && ReferenceEquals(had.Chain, ai.Duration))
            return;
        // A copy of the sheet, so the one every Rest Stop shares never changes. Regulars' Upgrade sets the count, adds its
        // Duration hook again and sets the throw and its look: the chain and the look are put back, and Buffet's throw,
        // which outranks Regulars', is landed again.
        Upgradeproperties sheet;
        if (!signSheets.TryGetValue(want, out sheet) || sheet == null)
        {
            sheet = UnityEngine.Object.Instantiate(u);
            sheet.name = u.name;
            sheet.Objectname = u.Objectname;
            sheet.Defaultparams = new Dictionary<string, float>(u.Defaultparams);
            sheet.Defaultparams["Perm_Count"] = want;
            signSheets[want] = sheet;
        }
        Entity_Ai_Tower_Fieshop.Durations chain = ai.Duration;
        string look = ai.Effectvis;
        try { ai.Upgrade(sheet); }
        finally
        {
            ai.Duration = chain;
            ai.Effectvis = look;
        }
        Upgradeproperties buffet;
        if (shop.Upgradelistserver.TryGetValue(Buffet, out buffet) && buffet != null)
            ai.Upgrade(buffet);
        signs[shop] = new Sign { Count = want, Ai = ai, Chain = ai.Duration };
        Opened++;
        RWLog.Info("Open Sign: a Rest Stop keeps " + want + " regulars");
    }

    // ---------------------------------------------------------------- Long Leash ranks II and III (Buster's side of a hit)

    static void OnDash(Link_Interact hit)
    {
        try
        {
            if (hit == null || hit.Type != "Hurt" || hit.From == null || hit.Data == null || hit.Data.ContainsKey("Burn") || hit.Data.ContainsKey(RWAny.Extra)
                || ModKitUtil.State(hit.From) != "Movelinear")
                return;
            int rank = 0;
            foreach (Holder h in holders)
                if (h.Vessel == hit.From) rank = h.Leash;
            float multi;
            if (rank < 2 || !hit.Data.TryGetValue("Damagemulti", out multi))
                return;
            ModKitCombat.SetMulti(hit, multi * (1f + LeashHit[rank]));
            Dashed++;
        }
        catch (Exception e) { RWLog.Once("leash", "a dash hit: " + e.Message); }
    }
}
