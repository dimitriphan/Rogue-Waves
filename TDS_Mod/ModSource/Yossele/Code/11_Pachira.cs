// Pachira Sapling: its roots strike every ore node within Range at once (Sno Sno's Sno Box does the same), Attackspeed
// times a second, each strike Damage like a miner's (Helper_Damagetools: min(Damage, 1) + Damageresource). Growth:
// mining speed +10% and range +0.4 a stage. Fully grown, each payout one of its strikes brings about drops an aura fruit
// (YosseleFruit) beside the node, worth Attackspeed x Damage x 15 of each aura colour. The game's nodes never run out:
// a node pays out and refills on the hit after its Healthresource reaches 0, the one that takes its Health of 1 (kit
// docs/09 #106), so the Sapling reads the node's Healthresource before and after each strike: a refill means it paid.
//   Focused Roots        x1.8 mining speed, but only the nearest node.
//   Sustaining Practices mends 0.5 a second per node in range per Growth stage (at least stage 1).
//   Grafted              a COLOUR button picks the fruit's colour: one colour pays 70% of the fruit's three in it.
//   Forest Project       +10% mining speed for each other Pachira Sapling within 4 tiles, anyone's, no cap.
//   Spontaneous Mulch    a foe falling within its Range: +20% mining speed for 5 s (a new one refreshes it, no stacking).
//   Connected Roots      while above half health it takes 65% of each hit on its owner's Tower-tagged buildings
//                        within its Range, other Saplings excluded (YosseleHooks).
//   Harvesting Season    fruit is worth double.
//   Wood Wide Web        once a second a player, however many Saplings have it: while the owner holds 100 or more of
//                        their most-held colour than of their least-held, 2 of the most become 1 of the least (YosseleWeb).
//   Fortifying Roots     +100% max health; its team's buildings within its Range, itself included, take 30% less
//                        damage (YosseleHooks; the strongest aura counts, two don't stack).
//   Softening Roots      every node in its Range takes one hit fewer to pay out, never fewer than 1 (YosseleSoftening).
// Speed bonuses (Growth, Focused, Forest, Mulch) add up in one Multi modifier, as the game's own percentages do.
public class YosselePachiraBrain : YosseleBuilding
{
    public int Strikes, Fruits, Nodes, Payouts, Mulched;
    public string Colour = "All";
    Link_Modifier speedMod, growRange;
    Link_Command colourCommand;
    float nextStrike, nextMend, nextMendLook, lastBonus = -1f, mulchUntil;

    protected override void Tick()
    {
        UpdateSpeed();
        List<Entity_Master> ores = Ores();
        Nodes = ores.Count;
        if (Has("Sustaining") && Time.time >= nextMend)
        {
            nextMend = Time.time + 0.5f;
            float heal = Param("Sustain_Heal", 0.5f) * ores.Count * Mathf.Max(1, Growth) * 0.5f;
            if (heal > 0f && ModKitUtil.Health(Master) < ModKitUtil.MaxHealth(Master) - 0.01f)
            {
                ModKitUtil.Heal(Master, Master, heal);
                if (Time.time >= nextMendLook)
                {
                    nextMendLook = Time.time + 2f;
                    ModKitUtil.Vis(Master, "Yossele_Mend", Pos, 0f);
                }
            }
        }
        float aspd = Stat("Attackspeed", 0.25f);
        if (aspd <= 0f || Time.time < nextStrike || ores.Count == 0)
            return;
        nextStrike = Time.time + 1f / aspd;
        if (Has("Focused"))
            ores = ores.GetRange(0, 1);
        foreach (Entity_Master ore in ores)
            Strike(ore);
    }

    public bool Mulching { get { return Has("Mulch") && Time.time < mulchUntil; } }

    void UpdateSpeed()
    {
        float bonus = YosseleData.PachiraGrowSpeed * Growth;
        if (Has("Focused"))
            bonus += Param("Focused_Multi", 0.8f);
        if (Has("Forest"))
            bonus += Neighbours() * Param("Forest_Per", 0.1f);
        if (Mulching)
            bonus += Param("Mulch_Bonus", 0.2f);
        if (Mathf.Approximately(bonus, lastBonus) && speedMod != null && !speedMod.Dead)
            return;
        lastBonus = bonus;
        speedMod = ModKitUtil.Keep(Master, speedMod, "Attackspeed", Helper_Entitytools.Modtypes.Multi, bonus);
    }

    // Other Pachira Saplings within Forest_Range, anyone's.
    public int Neighbours()
    {
        float reach = Param("Forest_Range", 4f);
        int n = 0;
        foreach (YosseleBuilding b in YosseleServer.Buildings)
            if (b != this && b is YosselePachiraBrain && b.Master != null && ModKitUtil.Alive(b.Master) && ModKitUtil.Flat(b.Pos, Pos) <= reach)
                n++;
        return n;
    }

    // A foe fell (YosseleServer): Spontaneous Mulch, if it fell within Range.
    public void FoeFell(Vector3 at)
    {
        if (!Has("Mulch") || ModKitUtil.Flat(at, Pos) > Stat("Range", 2.5f) + 0.3f)
            return;
        mulchUntil = Time.time + Param("Mulch_Last", 5f);
        Mulched++;
    }

    // Ore nodes within Range, nearest first.
    public List<Entity_Master> Ores()
    {
        List<Entity_Master> list = new List<Entity_Master>();
        float range = Stat("Range", 2.5f);
        foreach (Entity_Master e in ModKitUtil.Area(Pos, range + 0.3f, ModKitUtil.Tagged("Resource")))
            if (e != null && ModKitUtil.Alive(e) && e.Statsserver != null && e.Statsserver.ContainsKey("Healthresource") && ModKitUtil.Flat(ModKitUtil.Pos(e), Pos) <= range + 0.3f)
                list.Add(e);
        list.Sort((a, b) => ModKitUtil.Flat(ModKitUtil.Pos(a), Pos).CompareTo(ModKitUtil.Flat(ModKitUtil.Pos(b), Pos)));
        return list;
    }

    void Strike(Entity_Master ore)
    {
        // Ore takes min(Damage, 1) + Damageresource per strike and pays every player when it reaches 0, then refills.
        float hit = 1f + Mathf.Max(0f, Stat("Damage", 1f) - 1f);
        Dictionary<string, float> d = new Dictionary<string, float> { { "Damage", 1f }, { "Damagemulti", 1f }, { "Damageresource", hit - 1f } };
        float before = ModKitUtil.Stat(ore, "Healthresource", -1f);
        Vector3 at = ModKitUtil.Pos(ore);
        Helper_Entitytools.Interact(Master, ore, "Hurt", d);
        Strikes++;
        Master.Radio.Vis_ClientRpc("Yossele_Dig", at, 0f);
        float after = ModKitUtil.Stat(ore, "Healthresource", -1f);
        if (Paid(before, after, hit, ModKitUtil.Alive(ore)))
        {
            Payouts++;
            NodeEmptied(ore, at);
        }
    }

    // Whether one strike paid a node out: it refilled (after > before), or it was full, the strike was enough to empty
    // it and it refilled to the same number, or it went (a node that pays once).
    public static bool Paid(float before, float after, float hit, bool alive)
    {
        if (before < 0f)
            return false;
        if (!alive)
            return true;
        if (after > before + 0.001f)
            return true;
        return Mathf.Abs(after - before) <= 0.001f && before <= hit + 0.001f;
    }

    protected override void OnGrowth()
    {
        growRange = ModKitUtil.Keep(Master, growRange, "Range", Helper_Entitytools.Modtypes.Add, YosseleData.PachiraGrowRange * Growth);
        lastBonus = -1f;
    }

    protected override void Upgraded(YosseleData.Upg def)
    {
        if (def.Key == "Grafted" && colourCommand == null && YosseleContent.Colour != null)
        {
            colourCommand = Helper_Entitytools.Createcommand(Master, YosseleData.ColourCommand, YosseleData.ColourLink, false, true);
            SyncColour();
        }
    }

    protected override void Command(string command, string json, bool server)
    {
        if (command != YosseleData.ColourLink || !Has("Grafted"))
            return;
        int i = Array.IndexOf(YosseleData.Colours, Colour);
        Colour = YosseleData.Colours[(i + 1) % YosseleData.Colours.Length];
        SyncColour();
        ModKitUtil.Vis(Master, "Yossele_Colour", Pos, Array.IndexOf(YosseleData.Colours, Colour));
    }

    // The button draws the colour from the command's Goalstring (synced); run: false, so it isn't pressed again.
    void SyncColour()
    {
        if (colourCommand == null)
            return;
        colourCommand.Goalstring = Colour;
        Helper_Entitytools.Updatecommand(Master, YosseleData.ColourLink, "", false, false);
    }

    // What one fruit is worth in each colour: Attackspeed x Damage x 15 from its live stats (the sheet), doubled by
    // Harvesting Season.
    public float FruitEach { get { return Stat("Attackspeed", 0.25f) * Stat("Damage", 1f) * YosseleData.FruitScale * (1f + Param("Harvest_Multi", 0f)); } }

    // What one fruit pays: FruitEach of each colour, or one colour at 70% of the three (Grafted). Never under 1.
    public List<Link_Resourceset> FruitPay()
    {
        float each = FruitEach;
        string spec;
        if (!Has("Grafted") || Colour == "All")
            spec = "Redaura=" + Num(each) + ";Blueaura=" + Num(each) + ";Yellowaura=" + Num(each);
        else
            spec = Colour + "=" + Num(3f * each * Param("Grafted_Efficiency", 0.7f));
        return ModKitUtil.Costs(spec);
    }

    static string Num(float v) { return Mathf.Max(1f, Mathf.Round(v)).ToString(System.Globalization.CultureInfo.InvariantCulture); }

    // One of its strikes paid a node out: fully grown, it drops a fruit on the free ground beside the node.
    public void NodeEmptied(Entity_Master ore, Vector3 at)
    {
        if (!FullGrown)
            return;
        Genitem tile = DropTile(at);
        string skin = Has("Grafted") && Colour != "All" ? Colour : "";
        if (YosseleFruit.Drop(this, tile, tile != null ? tile.Position : at, FruitPay(), skin))
            Fruits++;
    }

    // The node's own tile can't be walked on, so the fruit goes on the free ground tile next to it (then one further
    // out) that is nearest the Sapling; the node's tile only when none is free.
    public Genitem DropTile(Vector3 node)
    {
        Genitem best = null;
        float bestD = float.MaxValue;
        for (int r = 1; r <= 2 && best == null; r++)
            for (int dx = -r; dx <= r; dx++)
                for (int dz = -r; dz <= r; dz++)
                {
                    if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dz)) != r)
                        continue;
                    Genitem g = ModKitUtil.Tile(node + new Vector3(dx, 0f, dz));
                    if (g == null || Occupied(g))
                        continue;
                    float dist = ModKitUtil.Flat(g.Position, Pos) + 0.01f * (Mathf.Abs(dx) + Mathf.Abs(dz));
                    if (dist < bestD)
                    {
                        bestD = dist;
                        best = g;
                    }
                }
        return best ?? ModKitUtil.Tile(node);
    }

    // A node or a building stands on it.
    static bool Occupied(Genitem g)
    {
        Vector3 c = g.Position + Vector3.up * 0.5f;
        foreach (string tag in new[] { "Resource", "Tower" })
            foreach (Entity_Master e in ModKitUtil.Area(c, 0.8f, ModKitUtil.Tagged(tag)))
                if (e != null && ModKitUtil.Alive(e) && ModKitUtil.Flat(ModKitUtil.Pos(e), g.Position) < 0.45f)
                    return true;
        return false;
    }
}

// Softening Roots (host only): one Add -1 modifier on the Healthresource of every node within a Softening Sapling's
// Range whose maximum is 2 or more (a node's Healthresource counts its hits before the paying one, kit docs/16
// section 4), so it pays one hit sooner for every miner. One per node however many Saplings reach it; taken off when
// none does.
public static class YosseleSoftening
{
    static readonly Dictionary<Entity_Master, Link_Modifier> softened = new Dictionary<Entity_Master, Link_Modifier>();
    static float next;
    public static int Made;

    public static int Count { get { return softened.Count; } }
    public static bool IsSoft(Entity_Master ore) { return ore != null && softened.ContainsKey(ore); }

    public static void Reset()
    {
        softened.Clear();
        next = 0f;
    }

    public static void Update(List<YosseleBuilding> buildings)
    {
        if (Time.time < next)
            return;
        next = Time.time + 1f;
        List<Entity_Master> want = new List<Entity_Master>();
        float points = 1f;
        foreach (YosseleBuilding b in buildings)
        {
            YosselePachiraBrain p = b as YosselePachiraBrain;
            if (p == null || !p.Started || p.Master == null || !ModKitUtil.Alive(p.Master) || !p.Has("Softening"))
                continue;
            points = p.Param("Soften_Points", 1f);
            foreach (Entity_Master ore in p.Ores())
                if (!want.Contains(ore))
                    want.Add(ore);
        }
        foreach (Entity_Master ore in want)
        {
            Link_Stats hr;
            if (softened.ContainsKey(ore) || ore.Statsserver == null || !ore.Statsserver.TryGetValue("Healthresource", out hr) || hr.Value_Calc < 2f)
                continue;
            float cut = Mathf.Min(points, hr.Value_Calc - 1f);
            softened[ore] = Helper_Entitytools.Addmod(ore, "Healthresource", -cut, Helper_Entitytools.Modtypes.Add, 0f);
            if (hr.Value > hr.Value_Calc)
                hr.Value = hr.Value_Calc;
            Made++;
        }
        List<Entity_Master> off = new List<Entity_Master>();
        foreach (Entity_Master ore in softened.Keys)
            if (ore == null || !want.Contains(ore))
                off.Add(ore);
        foreach (Entity_Master ore in off)
        {
            if (ore != null)
                ModKitUtil.Unmod(softened[ore]);
            softened.Remove(ore);
        }
    }
}

// Wood Wide Web (host only): like Smacky's Processing Plant without picking the colour. Once a second, each player with a
// standing Sapling that has it trades 2 of the colour they hold most of for 1 of the one they hold least of, while the gap
// between the two is at least 100. Both sides go through the game's own Give, as the Processing Plant's do
// (Entity_Ai_Tower_Converter: a negative Give for what it takes); Give multiplies by the map's gain, so the amounts are
// divided by it first and the purse moves by exactly 2 and 1.
public static class YosseleWeb
{
    static readonly float[] purse = new float[3];   // in ModKitUtil.Auras' order
    static readonly List<long> seen = new List<long>();
    static readonly Dictionary<long, float> nextLook = new Dictionary<long, float>();
    static float next;
    public static int Trades;

    public static void Reset()
    {
        next = 0f;
        nextLook.Clear();
    }

    public static void Update(List<YosseleBuilding> buildings)
    {
        if (Time.time < next)
            return;
        next = Time.time + 1f;
        seen.Clear();
        foreach (YosseleBuilding b in buildings)
        {
            YosselePachiraBrain p = b as YosselePachiraBrain;
            if (p == null || !p.Started || p.Master == null || !ModKitUtil.Alive(p.Master) || !p.Has("Web") || seen.Contains(p.Owner))
                continue;
            seen.Add(p.Owner);
            if (!Trade(p.Owner, p))
                continue;
            // The trade's look, a root pulse of the three colours on that Sapling, at most every 3 s a player.
            float at;
            if (!nextLook.TryGetValue(p.Owner, out at) || Time.time >= at)
            {
                nextLook[p.Owner] = Time.time + 3f;
                ModKitUtil.Vis(p.Master, "Yossele_Web", p.Pos, 0f);
            }
        }
    }

    // One trade for one player now; true when it traded. Public for the self-test.
    public static bool Trade(long owner, YosselePachiraBrain p)
    {
        if (!ModKitUtil.Purse(owner, purse))
            return false;
        string most = null, least = null;
        float hi = float.MinValue, lo = float.MaxValue;
        for (int i = 0; i < purse.Length; i++)
        {
            if (purse[i] > hi) { hi = purse[i]; most = ModKitUtil.Auras[i]; }
            if (purse[i] < lo) { lo = purse[i]; least = ModKitUtil.Auras[i]; }
        }
        float take = p.Param("Web_Take", 2f), give = p.Param("Web_Give", 1f);
        float g = ModKitUtil.Gain();
        if (most == null || least == null || most == least || hi - lo < p.Param("Web_Gap", 100f) - 0.001f || hi < take || g <= 0f)
            return false;
        ModKitUtil.Give(most, -take / g, owner);
        ModKitUtil.Give(least, give / g, owner);
        Trades++;
        return true;
    }
}

// Aura fruit on the ground (host only): a networked ground being with the colour as its skin, paying whoever's vessel
// or minion comes within 0.75 tiles first (any player's, scope). It stays until picked up or the map ends.
public static class YosseleFruit
{
    class Fruit { public Entity_Master Master; public List<Link_Resourceset> Pay; }

    static readonly List<Fruit> fruits = new List<Fruit>();
    static float nextCheck;
    public static int Dropped, Picked, Failed;
    public static string LastPick = "";

    public static int Count { get { fruits.RemoveAll(f => f.Master == null); return fruits.Count; } }

    public static void Reset()
    {
        fruits.Clear();
        LastPick = "";
    }

    public static bool Drop(YosseleBuilding from, Genitem tile, Vector3 at, List<Link_Resourceset> pay, string skin)
    {
        Beingobject being;
        if (tile == null || !YosseleContent.Beings.TryGetValue(YosseleData.Fruit, out being))
        {
            Failed++;
            return false;
        }
        Entity_Master fruit = null;
        try { fruit = YosseleUtil.SpawnOnTile(being, tile, from.Master, skin); }
        catch (Exception e) { YosseleLog.Warn("fruit spawn: " + e.Message); }
        if (fruit == null)
        {
            Failed++;
            return false;
        }
        fruits.Add(new Fruit { Master = fruit, Pay = pay });
        Dropped++;
        ModKitUtil.Vis(from.Master, "Yossele_Fruitdrop", at, 0f);
        return true;
    }

    public static void Update()
    {
        if (Time.time < nextCheck || fruits.Count == 0)
            return;
        nextCheck = Time.time + 0.2f;
        for (int i = fruits.Count - 1; i >= 0; i--)
        {
            Fruit f = fruits[i];
            if (f.Master == null || !ModKitUtil.Alive(f.Master))
            {
                fruits.RemoveAt(i);
                continue;
            }
            Entity_Master picker = Picker(ModKitUtil.Pos(f.Master));
            if (picker == null)
                continue;
            fruits.RemoveAt(i);
            Pay(f, picker);
        }
    }

    // A player's vessel or minion close enough: tagged Hero or Mob, not a foe, and owned by a player in the game: a mad beast
    // on a hunt map (a neutral Mob, no player's) would take the pay to no one (YOS-3, Dimitri's default of 2026-11-05).
    static Entity_Master Picker(Vector3 at)
    {
        foreach (string tag in new[] { "Hero", "Mob" })
            foreach (Entity_Master e in ModKitUtil.Area(at, YosseleData.FruitReach + 0.3f, ModKitUtil.Tagged(tag)))
                if (e != null && ModKitUtil.Alive(e) && !ModKitUtil.HasTag(e, "Enemy") && PlayerOwned(e) && ModKitUtil.Flat(ModKitUtil.Pos(e), at) <= YosseleData.FruitReach
                    && Mathf.Abs(ModKitUtil.Pos(e).y - at.y) < 1.2f)
                    return e;
        return null;
    }

    static bool PlayerOwned(Entity_Master e)
    {
        Blm_Server server = Blm_Server.Instance;
        return server != null && server.Sessionidsplayer != null && server.Sessionidsplayer.ContainsKey(ModKitUtil.Owner(e));
    }

    static void Pay(Fruit f, Entity_Master picker)
    {
        long owner = ModKitUtil.Owner(picker);
        Helper_Resources.Giveresources(f.Pay, owner, Helper_Resources.Styles.Default);
        Picked++;
        List<string> parts = new List<string>();
        foreach (Link_Resourceset r in f.Pay) parts.Add(r.Name + " " + r.Amount);
        LastPick = ModKitUtil.BeingName(picker) + " of player " + owner + ": " + ModKitUtil.Join(parts);
        Vector3 at = ModKitUtil.Pos(f.Master);
        ModKitUtil.Vis(f.Master, "Yossele_Fruitpick", at, 0f);
        // A moment later, so the pick-up's look reaches every player before the fruit goes.
        Entity_Master fruit = f.Master;
        YosseleUtil.Later(YosseleMod.Instance, 0.15f, () =>
        {
            if (fruit != null && fruit.Beingserver != null && ModKitUtil.Alive(fruit))
                Helper_Entitytools.Despawn(fruit.Beingserver, false, "Yossele fruit picked");
        });
    }

    // For the self-test: the fruit currently on the ground.
    public static List<Entity_Master> All()
    {
        List<Entity_Master> list = new List<Entity_Master>();
        foreach (Fruit f in fruits) if (f.Master != null) list.Add(f.Master);
        return list;
    }
}
