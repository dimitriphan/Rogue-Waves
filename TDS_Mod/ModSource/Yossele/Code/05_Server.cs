// Host-only rules. Everything that changes the game state happens here or in the brains, and reaches other players
// through the engine's own networking (spawns, stats, skins, upgrades, commands, debuff effects, Vis RPCs).
public class YosselePlayer
{
    public long Owner;
    public int Fertilizer, Compost, Made, Spent;
    public bool Unlocked;
    public YosseleHeroBrain Hero;
}

public static class YosseleServer
{
    public static readonly List<YosseleHeroBrain> Heroes = new List<YosseleHeroBrain>();
    public static readonly List<YosseleBuilding> Buildings = new List<YosseleBuilding>();
    static readonly Dictionary<long, YosselePlayer> players = new Dictionary<long, YosselePlayer>();
    static Game_Entity hookedEntity;
    static bool subscribed;
    public static int Tides, Grown, FoesFelled;

    public static void Update()
    {
        if (!subscribed)
        {
            subscribed = true;
            ModKitTides.MapStart += OnMapStart;
            ModKitTides.Tide += OnTide;
        }
        Game_Entity ge = Game_Entity.Instance;
        if (ge != hookedEntity)
        {
            hookedEntity = ge;
            Heroes.Clear();
            Buildings.Clear();
            players.Clear();
            YosseleSpores.Reset();
            YosseleVines.Reset();
            YosseleFruit.Reset();
            YosseleHooks.Reset();
            YosseleSoftening.Reset();
            YosseleFriends.Reset();
            YosseleUtil.ForgetLooks(true);
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
        Step("spores", YosseleSpores.Update);
        Step("vines", () => YosseleVines.Update(Buildings));
        Step("fruit", YosseleFruit.Update);
        Step("hooks", () => YosseleHooks.Update(Buildings));
        Step("softening", () => YosseleSoftening.Update(Buildings));
        Step("friends", YosseleFriends.Update);
    }

    static readonly YosseleFaults faults = new YosseleFaults();

    static void Step(string what, Action a)
    {
        try { a(); }
        catch (Exception e) { faults.Fail("server " + what, e); }
    }

    // ---------------------------------------------------------------- players, fertilizer, Compost Bin

    public static YosselePlayer Player(long owner)
    {
        YosselePlayer p;
        if (!players.TryGetValue(owner, out p))
        {
            p = new YosselePlayer { Owner = owner };
            players[owner] = p;
        }
        return p;
    }

    public static void AddFertilizer(long owner, int amount)
    {
        YosselePlayer p = Player(owner);
        int before = p.Fertilizer;
        p.Fertilizer = Mathf.Clamp(p.Fertilizer + amount, 0, YosseleData.FertilizerMax);
        if (p.Fertilizer > before)
            p.Made += p.Fertilizer - before;
        if (p.Hero != null)
            p.Hero.ShowFertilizer();
    }

    // The highest Compost Bin rank this vessel's player holds (0: none): on the team's core (kit 1.6, the mode grants
    // Directions there) or on the vessel itself (kit 1.7's per-player Directions).
    public static int CompostRank(Entity_Master hero)
    {
        for (int r = YosseleData.Compostbins.Length; r >= 1; r--)
        {
            string name = YosseleData.Compostbins[r - 1].Name;
            if (ModKitDirections.Has("Yossele", name))
                return r;
            if (hero != null && hero.Upgradelistserver != null && hero.Upgradelistserver.ContainsKey(ModKitDirections.Id("Yossele", name)))
                return r;
        }
        return 0;
    }

    // A foe fell: every Yossele with a Compost Bin and the foe within its radius counts it (felled by anyone).
    static void Compost(Vector3 at)
    {
        foreach (YosseleHeroBrain h in Heroes)
        {
            if (h == null || h.Master == null || !YosseleUtil.Alive(h.Master) || h.Rank <= 0)
                continue;
            YosseleData.Compost c = YosseleData.Compostbins[h.Rank - 1];
            if (YosseleUtil.Flat(YosseleUtil.Pos(h.Master), at) > c.Radius)
                continue;
            YosselePlayer p = h.Player;
            p.Compost++;
            if (p.Compost < c.Kills)
                continue;
            p.Compost = 0;
            if (p.Fertilizer >= YosseleData.FertilizerMax)
                continue;
            AddFertilizer(p.Owner, 1);
            YosseleUtil.Vis(h.Master, "Yossele_Compost", YosseleUtil.Pos(h.Master), p.Fertilizer);
        }
    }

    // ---------------------------------------------------------------- map and Tides (ModKitTides, host only)

    // Fertilizer starts at 0 each map (the campaign save doesn't carry it yet); a new map's buildings start at 0 anyway.
    static void OnMapStart()
    {
        foreach (YosselePlayer p in players.Values)
        {
            p.Fertilizer = 0;
            p.Compost = 0;
            if (p.Hero != null) p.Hero.ShowFertilizer();
        }
    }

    // The map's Tide count (its mode's Totalwaves, as Entity_Ai_Artifact_Coin reads it); 0 when unknown.
    public static int TidesOnMap()
    {
        try
        {
            Game_Mapvars mv = Game_Mapvars.Instance;
            return mv != null && mv.Mode != null && mv.Mode.Enemies != null ? mv.Mode.Enemies.Totalwaves : 0;
        }
        catch (Exception) { return 0; }
    }

    // For the self-test: a Tide count to use instead of the map's (0: the map's own).
    public static int TidesOverride;

    // Stages a Tide grants: 1, or 5 on a map of 4 Tides or fewer (the sheet's long Tides).
    public static int GrowthPerTide()
    {
        int tides = TidesOverride > 0 ? TidesOverride : TidesOnMap();
        return tides > 0 && tides <= YosseleData.LongTides ? YosseleData.GrowthPerLongTide : YosseleData.GrowthPerTide;
    }

    static void OnTide(int wave)
    {
        Tides++;
        int stages = GrowthPerTide();
        foreach (YosseleBuilding b in Buildings.ToArray())
        {
            if (b == null || !b.Started || b.Master == null || !YosseleUtil.Alive(b.Master))
                continue;
            int before = b.Growth;
            try { b.TideBegan(wave, stages); }
            catch (Exception e) { YosseleLog.Warn(b.Def.Id + " tide " + wave + ": " + e.Message); }
            if (b.Growth > before) Grown++;
        }
        // One line a Tide, so a play session's Player.log shows the short-map rule and how often the chance-based
        // upgrades fire (docs/reports/YOSSELE_TEST_PLAN_2026-10-23.md section 5).
        YosseleLog.Info("Tide " + wave + ": map Tides " + TidesOnMap() + ", +" + stages + " Growth; Viral bursts " + YosseleSpores.Virals
            + ", Cordyceps rises " + YosseleSpores.Rises + ", Hallucinogen stuns " + YosseleSpores.Stuns + ", Garden Weeds stacks " + YosseleDandelionBrain.WeedsGrown
            + "; Cordyfriends raised " + YosseleFriends.Raised + ", fell " + YosseleFriends.Fell + ", capped " + YosseleFriends.Capped);
    }

    // ---------------------------------------------------------------- spawns

    static void OnSpawn(Entity_Master master)
    {
        try
        {
            if (!YosseleUtil.IsServer || master == null || master.Beingserver == null || master.Beingserver.Being == null)
                return;
            string being = master.Beingserver.Being.Objectname;
            if (being == YosseleData.Hero)
            {
                master.gameObject.AddComponent<YosseleHeroBrain>().Master = master;
                return;
            }
            if (being == YosseleData.Friend)
            {
                YosseleFriends.Spawned(master);
                return;
            }
            YosseleData.Build b = YosseleData.BuildFor(being);
            if (b == null)
                return;
            YosseleBuilding building;
            if (b.Id == YosseleData.Cannon) building = master.gameObject.AddComponent<YosseleCannonBrain>();
            else if (b.Id == YosseleData.Dandelion) building = master.gameObject.AddComponent<YosseleDandelionBrain>();
            else if (b.Id == YosseleData.Bonsai) building = master.gameObject.AddComponent<YosseleBonsaiBrain>();
            else if (b.Id == YosseleData.Henge) building = master.gameObject.AddComponent<YosseleHengeBrain>();
            else building = master.gameObject.AddComponent<YosselePachiraBrain>();
            building.Master = master;
            building.Def = b;
            Buildings.Add(building);
            if (building is YosseleDandelionBrain)
                Limit(YosseleUtil.Owner(master));
        }
        catch (Exception e)
        {
            YosseleLog.Warn("spawn hook: " + e.Message);
        }
    }

    static void OnDespawn(Entity_Master master, bool killed, string reason)
    {
        try
        {
            if (!YosseleUtil.IsServer || master == null)
                return;
            YosseleBuilding building = master.GetComponent<YosseleBuilding>();
            if (building != null)
            {
                Buildings.Remove(building);
                YosseleLog.Info(building.Def.Id + " at " + building.Pos.ToString("F0") + " gone: " + (killed ? "destroyed" : "removed") + " (" + reason + "), Growth " + building.Growth);
                building.Gone(killed);
                if (killed)
                    YosseleUtil.Vis(master, "Yossele_Towerdown", YosseleUtil.Pos(master), 0);
                if (building is YosseleDandelionBrain)
                    Limit(YosseleUtil.Owner(master));
                return;
            }
            if (master.GetComponent<YosseleHeroBrain>() != null)
            {
                if (killed)
                    YosseleUtil.Vis(master, "Yossele_Herodeath", YosseleUtil.Pos(master), 0);
                return;
            }
            YosseleFriendBrain friend = master.GetComponent<YosseleFriendBrain>();
            if (friend != null)
            {
                YosseleFriends.Gone(friend, killed);
                return;
            }
            string being = YosseleUtil.BeingName(master);
            if (being == YosseleData.Vine)
            {
                YosseleVines.VineGone(master);
                return;
            }
            if (master.Taglistserver == null)
                return;
            if (master.Taglistserver.Contains("Enemy"))
            {
                Vector3 at = YosseleUtil.Pos(master);
                YosseleHooks.ForgetFoe(master);
                if (killed)
                {
                    FoesFelled++;
                    YosseleSpores.Fell(master, at);
                    foreach (YosseleBuilding b in Buildings.ToArray())
                    {
                        if (b is YosseleDandelionBrain)
                            ((YosseleDandelionBrain)b).FoeFell(master);
                        else if (b is YosselePachiraBrain)
                            ((YosselePachiraBrain)b).FoeFell(at);
                    }
                    Compost(at);
                }
                else
                    YosseleSpores.Clear(master);
            }
        }
        catch (Exception e)
        {
            YosseleLog.Warn("despawn hook: " + e.Message);
        }
    }

    // ---------------------------------------------------------------- the Dandelion Tree's limit

    // This player's standing Tree (never the Bonsai), or null.
    public static YosseleDandelionBrain TreeOf(long owner)
    {
        foreach (YosseleBuilding b in Buildings)
            if (b is YosseleDandelionBrain && !(b is YosseleBonsaiBrain) && b.Master != null && YosseleUtil.Alive(b.Master) && YosseleUtil.Owner(b.Master) == owner)
                return (YosseleDandelionBrain)b;
        return null;
    }

    public static bool HasDandelion(long owner) { return TreeOf(owner) != null; }

    public static bool HasBonsai(long owner)
    {
        foreach (YosseleBuilding b in Buildings)
            if (b is YosseleBonsaiBrain && b.Master != null && YosseleUtil.Alive(b.Master) && YosseleUtil.Owner(b.Master) == owner)
                return true;
        return false;
    }

    // What the Tree's build slot offers this player: the Tree while none stands, the Bonsai while the Tree has its
    // Bonsai upgrade and no Bonsai stands, else nothing ("").
    public static string TreeSlot(long owner)
    {
        YosseleDandelionBrain tree = TreeOf(owner);
        if (tree == null)
            return YosseleData.Dandelion;
        return tree.Has("Bonsai") && !HasBonsai(owner) ? YosseleData.Bonsai : "";
    }

    public static void Limit(long owner)
    {
        foreach (YosseleHeroBrain h in Heroes)
            if (h != null && h.Master != null && YosseleUtil.Owner(h.Master) == owner)
                h.CheckDandelion();
    }

    public static YosseleHeroBrain HeroOf(long owner)
    {
        foreach (YosseleHeroBrain h in Heroes)
            if (h != null && h.Master != null && YosseleUtil.Owner(h.Master) == owner)
                return h;
        return null;
    }
}
