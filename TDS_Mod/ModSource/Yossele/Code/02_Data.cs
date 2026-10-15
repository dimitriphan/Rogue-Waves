// Every name, text and tuning number for Yossele in one place (ModSource/Yossele/DESIGN.md, docs/scope/07). Numbers the
// scope gives are used as written; the rest are defaults, each listed in docs/YOSSELE_BUILD_NOTES.md. A melee vessel
// that mends itself, four buildables that grow a stage for every Tide they survive, and FERTILIZE once a Direction
// (Compost Bin) is held.
public static class YosseleData
{
    // The Rogue Waves pack version, "-dev" for the dev mod (docs/PACK.md section 4).
    public const string Version = "0.1.0-dev";
    public const string Part = "Yossele";
    public const bool Diagnostics = true;

    public const string TemplateHero = "Hero_Smacky";
    // Other vessels' groups Yossele clones from: Sno Sno's snow is the ground layer the vines and fruit are made of
    // (without it they fall back to Smacky's Bramble).
    public static readonly string[] ExtraGroups = { "Hero_Snosno" };
    public const string GroundTemplate = "Effect_Snosnosnow", GroundFallback = "Effect_Smackywall_Bramble";

    public const string Hero = "Hero_Yossele";
    public const string HeroEntity = "Hero_Yossele_Entity";
    public const string HeroTooltip = "Hero_Yossele_Tooltip_Upgradeproperties";

    public const string Cannon = "Tower_Yosselecannon";
    public const string Dandelion = "Tower_Yosseledandelion";
    public const string Henge = "Tower_Yosselehenge";
    public const string Pachira = "Tower_Yosselepachira";
    public static readonly string[] Towers = { Cannon, Dandelion, Henge, Pachira };
    public const string DandelionOption = "Buildoption2";   // its button goes while this player's tree stands (limit 1)

    public const string Vine = "Effect_Yosselevine";
    public const string Fruit = "Effect_Yosselefruit";

    public const string FertilizeCommand = "Command_Hero_Yossele_Fertilize";
    public const string FertilizeLink = "Fertilize";
    public const string ColourCommand = "Command_Tower_Yosselepachira_Colour";
    public const string ColourLink = "Colour";

    // Debuff looks the game attaches itself (Vfxdebuffcheck by name, registered under "Vfx").
    public const string VfxSpores = "Debuff_Yossele_Spores", VfxSporefull = "Debuff_Yossele_Sporefull", VfxDaze = "Debuff_Yossele_Daze";
    public const string VfxTangle = "Debuff_Yossele_Tangle", VfxBlockade = "Debuff_Yossele_Blockade";
    public const string VfxSlow = "Debuff_Slow", VfxStun = "Debuff_Stun";

    // ---------------------------------------------------------------- sound levels (matched to the game's world SFX)

    public const string ReferenceSound = "Upgrade";
    public static readonly Dictionary<string, float> SoundVolume = new Dictionary<string, float>
    {
        { "Yossele_Thud", 0.04f }, { "Yossele_Lob", 0.03f }, { "Yossele_Pop", 0.025f }, { "Yossele_Seeds", 0.02f },
        { "Yossele_Creak", 0.03f }, { "Yossele_Rustle", 0.03f }, { "Yossele_Fertilize", 0.035f }, { "Yossele_Pick", 0.03f },
        { "Yossele_Dig", 0.012f }, { "Yossele_Mend", 0.025f }, { "Yossele_Landing", 0.08f }, { "Yossele_Death", 0.06f },
        { "Yossele_Footstep", 0.008f },
    };

    // ---------------------------------------------------------------- vessel texts (the game uses English text as the key)

    public const string Displayname = "Yossele";
    public const string Displaysub = "The Patient Golem";
    public const string Displayskill = "Intermediate";
    public const string HeroDesc = "Sturdy, single-target melee that mends itself";
    public const string TooltipName = "Patient Growth";
    public const string TooltipDesc = "Sturdy melee that mends itself, buildings grow a stage for every Tide they survive and lose it all when destroyed, a Direction can unlock FERTILIZE";
    // The scope: "Lore comes once every character is fully realized". A placeholder until then (build notes).
    public const string Lore = "Yossele's story comes later. For now it waits, and its garden grows.";
    public const string FertilizeName = "FERTILIZE";
    public const string FertilizeDesc = "Spend a fertilizer on one of your buildings: it grows a stage at once";
    public const string FertilizeSelectTitle = "FERTILIZE A BUILDING";
    public const string ColourName = "COLOUR";
    public const string ColourDesc = "Pick the aura this Sapling's fruit pays";

    // ---------------------------------------------------------------- vessel stats (melee band; Smacky is 50/3/4/1.5/0.5)

    public static readonly string[] HeroStats =
    {
        "Health=60", "Speed=2.5", "Damage=4.5", "Range=1.5", "Attackspeed=0.5", "Buildrange=2.5", "Sellrate=0.5",
    };
    public const float Regen = 1f;              // health a second, healed by the brain (no base vessel regenerates)

    // ---------------------------------------------------------------- Growth, FERTILIZE, fertilizer, Compost Bin

    public const string GrowthStat = "Growth";
    public const string FertilizerStat = "Fertilizer";
    public const int FertilizerMax = 10;
    public const float FertilizeReach = 6f;     // the hotkey's fallback: nearest growable building of yours this close
    public const float FertilizeGuard = 0.5f;

    // Compost Bin, one Direction per rank (kit 1.6 has no ranks): foes felled within Radius of Yossele for 1 fertilizer.
    public class Compost { public string Name, Display, Desc; public int Kills; public float Radius; }
    public static readonly Compost[] Compostbins =
    {
        new Compost { Name = "Compostbin", Display = "Compost Bin", Kills = 10, Radius = 4f,
            Desc = "Unlocks FERTILIZE for Yossele, foes felled near Yossele make fertilizer" },
        new Compost { Name = "Compostbin2", Display = "Compost Bin II", Kills = 6, Radius = 4f,
            Desc = "Unlocks FERTILIZE for Yossele, foes felled near Yossele make fertilizer faster" },
        new Compost { Name = "Compostbin3", Display = "Compost Bin III", Kills = 6, Radius = 7f,
            Desc = "Unlocks FERTILIZE for Yossele, foes felled anywhere around Yossele make fertilizer faster" },
    };

    // ---------------------------------------------------------------- Mushroom Cannon and its spores

    public const float SporeDps = 0.3f;         // damage a second per stack
    public const float SporeFade = 4f;          // all stacks fall off this long after the last new one
    public const int SporeMax = 10;
    public const float SporeTick = 0.5f;
    public const float LobBase = 0.6f, LobPerTile = 0.08f;   // flight time: Bomb Tower's lobs take about a second
    public const float CannonGrowSpeed = 0.08f;              // attack speed per stage (adds, like every percentage)
    public const float CannonGrowRadius = 0.1f;
    public const float BounceDelay = 1f, BounceShare = 0.5f;
    public const int BounceSpores = 1;
    public const float FaroutMax = 0.75f;
    public const float HalluReach = 1.7f;
    public const float BurstGuard = 1f;

    // ---------------------------------------------------------------- Primordial Dandelion Tree

    public const int SeedsPerStage = 3;
    public const float RangePerStage = 0.4f;    // of its base range, added each stage
    public const float SeedFlight = 0.3f;
    public const int SeedsDrawn = 24;           // the most ready seeds a client draws round the crown
    public const float PacketsWindow = 0.6f;    // a foe that falls this soon after a seed hit was felled by it

    // ---------------------------------------------------------------- Overgrown Stonehenge and its vines

    public const float HengeArmorPerStage = 0.1f;
    public const float VineTick = 0.5f;
    public const float VineReach = 0.55f;       // a foe this close (flat) to a vine tile's centre stands on it

    // ---------------------------------------------------------------- Pachira Sapling and aura fruit

    public const float PachiraGrowSpeed = 0.1f, PachiraGrowRange = 0.3f;
    public const float FruitEach = 30f;         // of every aura colour (the scope's Grafted Colors example)
    public const float FruitReach = 0.75f;
    public const float OreMemory = 2f;          // a node emptied this soon after the Sapling's strike counts as its own
    public static readonly string[] Colours = { "All", "Redaura", "Blueaura", "Yellowaura" };
    public static readonly string[] ColourNames = { "ALL", "RED", "BLUE", "YELLOW" };

    // ---------------------------------------------------------------- buildables

    public class Upg
    {
        public string Owner, Key, Display, Desc, Params, Costs;
        public int Tier;
        public string[] Next;
        public string Id { get { return Owner + "_" + Key + Tier + "_Upgradeobject"; } }
        public string Model { get { return Owner + "_" + Key + Tier; } }
    }

    public class Build
    {
        public string Id, Template, Display, Desc, Lore, Tag, Costs;
        public string[] Tags, Stats;
        public int Max;              // the most Growth stages it reaches
        public float Muzzle;         // where its shots leave, above its tile (ranges are measured from the tile, ModKitTowers.Fit)
        public Upg[] Upgrades;
    }

    static Upg U(string owner, string key, int tier, string display, string desc, string pars, string costs, string next)
    {
        return new Upg
        {
            Owner = owner, Key = key, Tier = tier, Display = display, Desc = desc, Params = pars, Costs = costs,
            Next = string.IsNullOrEmpty(next) ? new string[0] : next.Split(','),
        };
    }

    // The scope's three levels per building are the game's tiers 2-4; every choice of a level leads to every choice of
    // the next. No tier 5 (the scope has none).
    public static readonly Build[] Builds =
    {
        new Build
        {
            Id = Cannon, Template = "Tower_Smackybomber", Display = "Mushroom Cannon", Tag = "Yosselecannon", Tags = new[] { "Tower", "Towerfighter" },
            Desc = "Long-range lobs that stack spores, spores rot foes over time", Costs = "Redaura=35", Max = 5, Muzzle = 0.6f,
            Lore = "It coughs up spore bombs in long, lazy arcs. Whatever they land on starts to rot, a little more with every hit.",
            Stats = new[] { "Health=35+25", "Damage=1.5+0.5", "Attackspeed=0.3", "Range=6", "Radius=1", "Spores=2" },
            Upgrades = new[]
            {
                U(Cannon, "Sticky", 2, "Sticky Spores", "Fully spored foes are slowed", "Sticky_Slow=0.25", "Redaura=30", "Viral3,Deathcap3"),
                U(Cannon, "Farout", 2, "Farout Fungus", "Longer range, bigger blasts from longer flights", "Range_Add=2;Farout_Per=0.5", "Redaura=25;Blueaura=10", "Viral3,Deathcap3"),
                U(Cannon, "Viral", 3, "Viral Outbreak", "Fully spored foes may burst when felled", "Viral_Chance=0.3", "Redaura=90", "Finish4,Hallucinogen4"),
                U(Cannon, "Deathcap", 3, "Deathcap Training", "Deadlier spores, fewer of them", "Deathcap_Multi=0.25;Sporemax_Base=8", "Redaura=100", "Finish4,Hallucinogen4"),
                U(Cannon, "Finish", 4, "Explosive Finish", "Full spores burst, spreading the rot", "Finish_Per=1;Finish_Radius=1.25;Finish_Spores=2", "Redaura=420", ""),
                U(Cannon, "Hallucinogen", 4, "Hallucinogen", "Fully spored foes may turn on their friends", "Hallu_Every=5;Hallu_Chance=0.2;Hallu_Duration=2", "Redaura=360;Blueaura=60", ""),
            }
        },
        new Build
        {
            Id = Dandelion, Template = "Tower_Smackyarcher", Display = "Primordial Dandelion Tree", Tag = "Yosseledandelion", Tags = new[] { "Tower", "Towerfighter" },
            Desc = "Grows seeds that all strike at once, one per player", Costs = "Redaura=50", Max = 10, Muzzle = 1.1f,
            Lore = "The oldest dandelion there is. It keeps its seeds close until something walks too near, then lets them all go.",
            Stats = new[] { "Health=50+25", "Damage=2+0.5", "Range=6", "Seedlimit=6", "Seedrate=0.5" },
            Upgrades = new[]
            {
                U(Dandelion, "Packets", 2, "Seed Packets", "Seeds that fell a foe come back", "Packets_Refund=2", "Redaura=30", "Picky3,Weeds3"),
                U(Dandelion, "Demand", 2, "Demand and Supply", "Grows seeds faster when few are ready", "Demand_Max=1", "Redaura=30", "Picky3,Weeds3"),
                U(Dandelion, "Picky", 3, "Picky Placement", "Seeds go for the toughest foes and bite deeper", "Picky_Percent=0.01", "Redaura=100", "Storm4"),
                U(Dandelion, "Weeds", 3, "Garden Weeds", "Foes felled after a seed hit speed up the seeds", "Weeds_Per=0.1;Weeds_Window=3;Weeds_Last=15", "Redaura=90;Yellowaura=30", "Storm4"),
                U(Dandelion, "Storm", 4, "Dandelion Storm", "Huge range, many more seeds, grown faster", "Range_Multi=0.5;Seedlimit_Add=10;Seedrate_Multi=0.5", "Redaura=450", ""),
            }
        },
        new Build
        {
            Id = Henge, Template = "Tower_Smackyhut", Display = "Overgrown Stonehenge", Tag = "Yosselehenge", Tags = new[] { "Tower" },
            Desc = "Sturdy stones, vines creep the way it faces and slow foes", Costs = "Blueaura=25", Max = 8, Muzzle = 0.5f,
            Lore = "Old stones the moss took back. Each Tide its vines creep a little further, and foes wade through them slowly.",
            Stats = new[] { "Health=80+40", "Slow=0.25" },
            Upgrades = new[]
            {
                U(Henge, "Verdant", 2, "Verdant Spread", "Vines spread all around", "Verdant_Base=1", "Blueaura=30", "Remedy3,Carnivore3"),
                U(Henge, "Blockade", 2, "Vine Blockade", "A vine barrier each Tide, sturdier stones", "Health_Multi=0.5;Blockade_Share=0.3", "Blueaura=35", "Remedy3,Carnivore3"),
                U(Henge, "Remedy", 3, "Natural Remedy", "Vines mend buildings", "Remedy_Heal=10", "Blueaura=90", "Spaghetti4"),
                U(Henge, "Carnivore", 3, "Opportunistic Carnivore", "Vines drain foes to mend the stones", "Carnivore_Drain=10;Carnivore_Max=5", "Blueaura=80;Redaura=30", "Spaghetti4"),
                U(Henge, "Spaghetti", 4, "Entangling Spaghetti Roots", "Lingering foes are snared and sapped", "Spaghetti_After=7;Spaghetti_Stun=1.5;Spaghetti_Percent=0.1", "Blueaura=420", ""),
            }
        },
        new Build
        {
            Id = Pachira, Template = "Tower_Smackyhut", Display = "Pachira Sapling", Tag = "Yosselepachira", Tags = new[] { "Tower", "Gatherer" },
            Desc = "Its roots mine every ore node in reach, fully grown it drops aura fruit", Costs = "Yellowaura=35", Max = 5, Muzzle = 0.5f,
            Lore = "A money tree, still small. Its roots find every ore node nearby and drink from all of them at once.",
            Stats = new[] { "Health=30+20", "Damage=1", "Attackspeed=0.25", "Range=2" },
            Upgrades = new[]
            {
                U(Pachira, "Focused", 2, "Focused Roots", "Mines one node much faster", "Focused_Multi=1.5", "Yellowaura=30", "Grafted3,Selfish3"),
                U(Pachira, "Sustaining", 2, "Sustaining Practices", "Mends itself from the ore around it", "Sustain_Heal=0.25", "Yellowaura=25;Blueaura=10", "Grafted3,Selfish3"),
                U(Pachira, "Grafted", 3, "Grafted Colors", "Pick the colour of its fruit", "Grafted_Efficiency=0.7", "Yellowaura=100", "Connected4,Harvest4"),
                U(Pachira, "Selfish", 3, "Selfish Sapling", "Mines 20% faster for each other Sapling nearby, up to double", "Selfish_Per=0.2;Selfish_Max=1;Selfish_Range=4", "Yellowaura=90", "Connected4,Harvest4"),
                U(Pachira, "Connected", 4, "Connected Roots", "Takes hits for buildings nearby while healthy", "Connected_Share=0.65;Connected_Above=0.5", "Yellowaura=350;Blueaura=80", ""),
                U(Pachira, "Harvest", 4, "Harvesting Season", "Fruit is worth double", "Harvest_Multi=1", "Yellowaura=400", ""),
            }
        },
    };

    public static Build BuildFor(string being)
    {
        foreach (Build b in Builds)
            if (b.Id == being)
                return b;
        return null;
    }

    // Owner-only messages, shown by YosseleClient ("code + 100 x value" carries a number).
    public static readonly string[] WarnText =
    {
        "",
        "Point at one of your buildings",
        "No fertilizer",
        "Already fully grown",
        "Not one of your buildings",
        "FERTILIZE unlocked",
        "Fertilizer",
        "Fruit: ALL",
        "Fruit: RED",
        "Fruit: BLUE",
        "Fruit: YELLOW",
    };
    public const int WarnNoTarget = 1, WarnNoFertilizer = 2, WarnFullGrown = 3, WarnNotYours = 4, WarnUnlocked = 5, WarnFertilizer = 6,
        WarnColour = 7;

    // EFFECTS lines in the journal: one label per parameter, "" hides it.
    public static readonly string[][] ParamLabels =
    {
        new[] { "Sticky", "Slow at full spores" }, new[] { "Farout", "Blast per second of flight" }, new[] { "Viral", "Burst chance" },
        new[] { "Deathcap", "Spore damage" }, new[] { "Sporemax", "Most spores" }, new[] { "Finish", "" }, new[] { "Hallu", "" },
        new[] { "Packets", "Seeds back" }, new[] { "Demand", "Faster when low" }, new[] { "Picky", "Of max health" },
        new[] { "Weeds", "" }, new[] { "Seedlimit", "Seeds" }, new[] { "Seedrate", "Seed growth" }, new[] { "Verdant", "" },
        new[] { "Blockade", "Barrier" }, new[] { "Remedy", "Healing" }, new[] { "Carnivore", "" }, new[] { "Spaghetti", "" },
        new[] { "Focused", "Mining speed" }, new[] { "Sustain", "Healing" }, new[] { "Grafted", "Fruit value" },
        new[] { "Selfish", "" }, new[] { "Connected", "" }, new[] { "Harvest", "Fruit value" }, new[] { "Radius", "Radius" },
        new[] { "Spores", "Spores" },
    };
}
