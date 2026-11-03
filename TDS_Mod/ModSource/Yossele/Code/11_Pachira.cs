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
            if (heal > 0f && YosseleUtil.Health(Master) < YosseleUtil.MaxHealth(Master) - 0.01f)
            {
                YosseleUtil.Heal(Master, Master, heal);
                if (Time.time >= nextMendLook)
                {
                    nextMendLook = Time.time + 2f;
                    YosseleUtil.Vis(Master, "Yossele_Mend", Pos, 0f);
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
        speedMod = YosseleUtil.Keep(Master, speedMod, "Attackspeed", Helper_Entitytools.Modtypes.Multi, bonus);
    }

    // Other Pachira Saplings within Forest_Range, anyone's.
    public int Neighbours()
    {
        float reach = Param("Forest_Range", 4f);
        int n = 0;
        foreach (YosseleBuilding b in YosseleServer.Buildings)
            if (b != this && b is YosselePachiraBrain && b.Master != null && YosseleUtil.Alive(b.Master) && YosseleUtil.Flat(b.Pos, Pos) <= reach)
                n++;
        return n;
    }

    // A foe fell (YosseleServer): Spontaneous Mulch, if it fell within Range.
    public void FoeFell(Vector3 at)
    {
        if (!Has("Mulch") || YosseleUtil.Flat(at, Pos) > Stat("Range", 2.5f) + 0.3f)
            return;
        mulchUntil = Time.time + Param("Mulch_Last", 5f);
        Mulched++;
    }

    // Ore nodes within Range, nearest first.
    public List<Entity_Master> Ores()
    {
        List<Entity_Master> list = new List<Entity_Master>();
        float range = Stat("Range", 2.5f);
        foreach (Entity_Master e in YosseleUtil.Area(Pos, range + 0.3f, YosseleUtil.Tagged("Resource")))
            if (e != null && YosseleUtil.Alive(e) && e.Statsserver != null && e.Statsserver.ContainsKey("Healthresource") && YosseleUtil.Flat(YosseleUtil.Pos(e), Pos) <= range + 0.3f)
                list.Add(e);
        list.Sort((a, b) => YosseleUtil.Flat(YosseleUtil.Pos(a), Pos).CompareTo(YosseleUtil.Flat(YosseleUtil.Pos(b), Pos)));
        return list;
    }

    void Strike(Entity_Master ore)
    {
        // Ore takes min(Damage, 1) + Damageresource per strike and pays every player when it reaches 0, then refills.
        float hit = 1f + Mathf.Max(0f, Stat("Damage", 1f) - 1f);
        Dictionary<string, float> d = new Dictionary<string, float> { { "Damage", 1f }, { "Damagemulti", 1f }, { "Damageresource", hit - 1f } };
        float before = YosseleUtil.Stat(ore, "Healthresource", -1f);
        Vector3 at = YosseleUtil.Pos(ore);
        Helper_Entitytools.Interact(Master, ore, "Hurt", d);
        Strikes++;
        Master.Radio.Vis_ClientRpc("Yossele_Dig", at, 0f);
        float after = YosseleUtil.Stat(ore, "Healthresource", -1f);
        if (Paid(before, after, hit, YosseleUtil.Alive(ore)))
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
        growRange = YosseleUtil.Keep(Master, growRange, "Range", Helper_Entitytools.Modtypes.Add, YosseleData.PachiraGrowRange * Growth);
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
        YosseleUtil.Vis(Master, "Yossele_Colour", Pos, Array.IndexOf(YosseleData.Colours, Colour));
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
        return YosseleUtil.Costs(spec);
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
                    Genitem g = YosseleUtil.Tile(node + new Vector3(dx, 0f, dz));
                    if (g == null || Occupied(g))
                        continue;
                    float dist = YosseleUtil.Flat(g.Position, Pos) + 0.01f * (Mathf.Abs(dx) + Mathf.Abs(dz));
                    if (dist < bestD)
                    {
                        bestD = dist;
                        best = g;
                    }
                }
        return best ?? YosseleUtil.Tile(node);
    }

    // A node or a building stands on it.
    static bool Occupied(Genitem g)
    {
        Vector3 c = g.Position + Vector3.up * 0.5f;
        foreach (string tag in new[] { "Resource", "Tower" })
            foreach (Entity_Master e in YosseleUtil.Area(c, 0.8f, YosseleUtil.Tagged(tag)))
                if (e != null && YosseleUtil.Alive(e) && YosseleUtil.Flat(YosseleUtil.Pos(e), g.Position) < 0.45f)
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
            if (p == null || !p.Started || p.Master == null || !YosseleUtil.Alive(p.Master) || !p.Has("Softening"))
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
                YosseleUtil.Unmod(softened[ore]);
            softened.Remove(ore);
        }
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
        YosseleUtil.Vis(from.Master, "Yossele_Fruitdrop", at, 0f);
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
            if (f.Master == null || !YosseleUtil.Alive(f.Master))
            {
                fruits.RemoveAt(i);
                continue;
            }
            Entity_Master picker = Picker(YosseleUtil.Pos(f.Master));
            if (picker == null)
                continue;
            fruits.RemoveAt(i);
            Pay(f, picker);
        }
    }

    // A player's vessel or minion close enough: tagged Hero or Mob, not a foe.
    static Entity_Master Picker(Vector3 at)
    {
        foreach (string tag in new[] { "Hero", "Mob" })
            foreach (Entity_Master e in YosseleUtil.Area(at, YosseleData.FruitReach + 0.3f, YosseleUtil.Tagged(tag)))
                if (e != null && YosseleUtil.Alive(e) && !YosseleUtil.HasTag(e, "Enemy") && YosseleUtil.Flat(YosseleUtil.Pos(e), at) <= YosseleData.FruitReach
                    && Mathf.Abs(YosseleUtil.Pos(e).y - at.y) < 1.2f)
                    return e;
        return null;
    }

    static void Pay(Fruit f, Entity_Master picker)
    {
        long owner = YosseleUtil.Owner(picker);
        Helper_Resources.Giveresources(f.Pay, owner, Helper_Resources.Styles.Default);
        Picked++;
        List<string> parts = new List<string>();
        foreach (Link_Resourceset r in f.Pay) parts.Add(r.Name + " " + r.Amount);
        LastPick = YosseleUtil.BeingName(picker) + " of player " + owner + ": " + YosseleUtil.Join(parts);
        Vector3 at = YosseleUtil.Pos(f.Master);
        YosseleUtil.Vis(f.Master, "Yossele_Fruitpick", at, 0f);
        // A moment later, so the pick-up's look reaches every player before the fruit goes.
        Entity_Master fruit = f.Master;
        YosseleUtil.Later(YosseleMod.Instance, 0.15f, () =>
        {
            if (fruit != null && fruit.Beingserver != null && YosseleUtil.Alive(fruit))
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
