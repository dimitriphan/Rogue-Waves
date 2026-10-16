// Pachira Sapling: its roots strike every ore node within Range at once (Sno Sno's Sno Box does the same), Attackspeed
// times a second, each strike Damage like a miner's (Helper_Damagetools: min(Damage, 1) + Damageresource). Growth:
// mining speed +10% and range +0.3 a stage; fully grown, a node it mines out drops an aura fruit on its tile
// (YosseleFruit) worth 30 of each aura colour.
//   Focused Roots        x2.5 mining speed, but only the nearest node.
//   Sustaining Practices mends 0.25 a second per node in range per Growth stage (at least stage 1).
//   Grafted Colors       a COLOUR button picks the fruit's colour: one colour pays 3 x 30 x 70% = 63 of it.
//   Selfish Sapling      +20% mining speed for each other Pachira Sapling within 4 tiles, up to +100% (Dimitri,
//                        2026-10-12: reversed from "faster when alone").
//   Connected Roots      while above half health it takes 65% of each hit on its team's Tower-tagged buildings within
//                        its Range, other Saplings excluded (YosseleHooks).
//   Harvesting Season    fruit is worth double.
// Speed bonuses (Growth, Focused, Selfish) add up in one Multi modifier, as the game's own percentages do.
public class YosselePachiraBrain : YosseleBuilding
{
    public int Strikes, Fruits, Nodes;
    public string Colour = "All";
    Link_Modifier speedMod, growRange;
    Link_Command colourCommand;
    float nextStrike, nextMend, lastBonus = -1f;

    public static readonly Dictionary<Entity_Master, YosselePachiraBrain> LastHit = new Dictionary<Entity_Master, YosselePachiraBrain>();
    public static readonly Dictionary<Entity_Master, float> LastHitTime = new Dictionary<Entity_Master, float>();

    protected override void Tick()
    {
        UpdateSpeed();
        List<Entity_Master> ores = Ores();
        Nodes = ores.Count;
        if (Has("Sustaining") && Time.time >= nextMend)
        {
            nextMend = Time.time + 0.5f;
            float heal = Param("Sustain_Heal", 0.25f) * ores.Count * Mathf.Max(1, Growth) * 0.5f;
            if (heal > 0f && YosseleUtil.Health(Master) < YosseleUtil.MaxHealth(Master) - 0.01f)
                YosseleUtil.Heal(Master, Master, heal);
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

    void UpdateSpeed()
    {
        float bonus = YosseleData.PachiraGrowSpeed * Growth;
        if (Has("Focused"))
            bonus += Param("Focused_Multi", 1.5f);
        if (Has("Selfish"))
            bonus += Selfish(Neighbours(), Param("Selfish_Per", 0.2f), Param("Selfish_Max", 1f));
        if (Mathf.Approximately(bonus, lastBonus) && speedMod != null && !speedMod.Dead)
            return;
        lastBonus = bonus;
        speedMod = YosseleUtil.Keep(Master, speedMod, "Attackspeed", Helper_Entitytools.Modtypes.Multi, bonus);
    }

    public static float Selfish(int neighbours, float per, float max) { return Mathf.Min(neighbours * per, max); }

    // Other Pachira Saplings within Selfish_Range, anyone's.
    public int Neighbours()
    {
        float reach = Param("Selfish_Range", 4f);
        int n = 0;
        foreach (YosseleBuilding b in YosseleServer.Buildings)
            if (b != this && b is YosselePachiraBrain && b.Master != null && YosseleUtil.Alive(b.Master) && YosseleUtil.Flat(b.Pos, Pos) <= reach)
                n++;
        return n;
    }

    // Ore nodes within Range, nearest first.
    public List<Entity_Master> Ores()
    {
        List<Entity_Master> list = new List<Entity_Master>();
        float range = Stat("Range", 2f);
        foreach (Entity_Master e in YosseleUtil.Area(Pos, range + 0.3f, YosseleUtil.Tagged("Resource")))
            if (e != null && YosseleUtil.Alive(e) && e.Statsserver != null && e.Statsserver.ContainsKey("Healthresource") && YosseleUtil.Flat(YosseleUtil.Pos(e), Pos) <= range + 0.3f)
                list.Add(e);
        list.Sort((a, b) => YosseleUtil.Flat(YosseleUtil.Pos(a), Pos).CompareTo(YosseleUtil.Flat(YosseleUtil.Pos(b), Pos)));
        return list;
    }

    void Strike(Entity_Master ore)
    {
        // Ore takes min(Damage, 1) + Damageresource per strike and pays its miner's player when emptied.
        Dictionary<string, float> d = new Dictionary<string, float> { { "Damage", 1f }, { "Damagemulti", 1f }, { "Damageresource", Mathf.Max(0f, Stat("Damage", 1f) - 1f) } };
        LastHit[ore] = this;
        LastHitTime[ore] = Time.time;
        Helper_Entitytools.Interact(Master, ore, "Hurt", d);
        Strikes++;
        Master.Radio.Vis_ClientRpc("Yossele_Dig", YosseleUtil.Pos(ore), 0f);
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

    // What one fruit pays: 30 of each colour, or one colour at 70% of the three (Grafted Colors), doubled by Harvesting
    // Season.
    public List<Link_Resourceset> FruitPay()
    {
        float scale = 1f + Param("Harvest_Multi", 0f);
        string spec;
        if (!Has("Grafted") || Colour == "All")
        {
            float each = YosseleData.FruitEach * scale;
            spec = "Redaura=" + Num(each) + ";Blueaura=" + Num(each) + ";Yellowaura=" + Num(each);
        }
        else
            spec = Colour + "=" + Num(3f * YosseleData.FruitEach * Param("Grafted_Efficiency", 0.7f) * scale);
        return YosseleUtil.Costs(spec);
    }

    static string Num(float v) { return Mathf.Round(v).ToString(System.Globalization.CultureInfo.InvariantCulture); }

    // A node this Sapling struck last was mined out (YosseleServer): fully grown, it drops a fruit there.
    public void NodeEmptied(Vector3 at, Genitem block)
    {
        if (!FullGrown)
            return;
        Genitem tile = YosseleUtil.Tile(at) ?? block;
        string skin = Has("Grafted") && Colour != "All" ? Colour : "";
        if (YosseleFruit.Drop(this, tile, at, FruitPay(), skin))
            Fruits++;
    }

    public static void Forget(Entity_Master ore)
    {
        LastHit.Remove(ore);
        LastHitTime.Remove(ore);
        if (LastHit.Count > 256)
        {
            List<Entity_Master> stale = new List<Entity_Master>();
            foreach (KeyValuePair<Entity_Master, YosselePachiraBrain> kv in LastHit)
                if (kv.Key == null || kv.Value == null) stale.Add(kv.Key);
            foreach (Entity_Master e in stale) { LastHit.Remove(e); LastHitTime.Remove(e); }
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

    public static void Reset() { fruits.Clear(); }

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
