// Every name, text and tuning number for Yossele in one place (ModSource/Yossele/DESIGN.md, docs/scope/07, the
// 2026-10-20 sheet). Numbers the sheet gives are used as written; the rest are defaults, each listed in DESIGN.md
// "Changes from the concept". A ranged vessel with a green mouth laser that mends itself, four buildables (and the
// Tree's Bonsai) that grow a stage for every Tide they survive, and FERTILIZE once a Direction (Compost Bin) is held.
public static class YosseleData
{
    // The Rogue Waves pack version, "-dev" for the dev mod (docs/PACK.md section 4).
    public const string Version = "0.1.0-dev";
    public const string Part = "Yossele";
    public const bool Diagnostics = true;

    public const string TemplateHero = "Hero_Smacky";
    // Other vessels' groups Yossele clones from: Sno Sno's snow is the ground layer the vines and fruit are made of
    // (without it they fall back to Smacky's Bramble), its snowball is the laser's template, and its Snoble lends the
    // Cordyfriend its look and its mob-blocker block.
    public static readonly string[] ExtraGroups = { "Hero_Snosno" };
    public const string GroundTemplate = "Effect_Snosnosnow", GroundFallback = "Effect_Smackywall_Bramble";
    public const string LaserTemplate = "Hero_Snosno_Snoball", LaserFallback = "Tower_Smackyarcher_Arrow";
    public const string FriendTemplate = "Mob_Smackyminer", FriendLook = "Mob_Snoble";
    public const string FriendSheet = "Yosselefriend-Sheet";              // temporary art, ModSource/Yossele/Art/Temporary

    public const string Hero = "Hero_Yossele";
    public const string HeroEntity = "Hero_Yossele_Entity";
    public const string HeroTooltip = "Hero_Yossele_Tooltip_Upgradeproperties";
    public const string Laser = "Hero_Yossele_Laser";
    public const string LaserTag = "Yosselelaser";   // the laser's hits carry it (the ranged AI's Hurtdict)

    public const string Cannon = "Tower_Yosselecannon";
    public const string Dandelion = "Tower_Yosseledandelion";
    public const string Henge = "Tower_Yosselehenge";
    public const string Pachira = "Tower_Yosselepachira";
    public const string Bonsai = "Tower_Yosselebonsai";
    public static readonly string[] Towers = { Cannon, Dandelion, Henge, Pachira };
    // The Tree's build slot: the Tree while this player has none, the Bonsai while the Tree has its Bonsai upgrade and
    // no Bonsai stands, else no button (both have a limit of 1).
    public const string DandelionOption = "Buildoption2";

    public const string Vine = "Effect_Yosselevine";
    public const string Fruit = "Effect_Yosselefruit";
    public const string Friend = "Mob_Yosselefriend";

    public const string FertilizeCommand = "Command_Hero_Yossele_Fertilize";
    public const string FertilizeLink = "Fertilize";
    public const string ColourCommand = "Command_Tower_Yosselepachira_Colour";
    public const string ColourLink = "Colour";

    // Debuff looks the game attaches itself (Vfxdebuffcheck by name, registered under "Vfx").
    public const string VfxSpores = "Debuff_Yossele_Spores", VfxSporefull = "Debuff_Yossele_Sporefull";
    public const string VfxTangle = "Debuff_Yossele_Tangle", VfxBlockade = "Debuff_Yossele_Blockade";
    public const string VfxSlow = "Debuff_Slow", VfxStun = "Debuff_Stun", VfxMarked = "Debuff_Yossele_Marked", VfxDisabled = "Debuff_Disabled";
    // Hallucinogen's stun look is Yossele's own: the stock Debuff_Stun is loaded on some maps only (play test
    // 2026-10-26: on 3B_Woozy every stun logged "Could not retrieve Vfx/Debuff_Stun" and showed nothing).
    public const string VfxDaze = "Debuff_Yossele_Daze";

    // ---------------------------------------------------------------- sound levels (matched to the game's world SFX)

    public static readonly Dictionary<string, float> SoundVolume = new Dictionary<string, float>
    {
        { "Yossele_Thud", 0.04f }, { "Yossele_Lob", 0.03f }, { "Yossele_Pop", 0.025f }, { "Yossele_Seeds", 0.02f },
        { "Yossele_Creak", 0.03f }, { "Yossele_Rustle", 0.03f }, { "Yossele_Fertilize", 0.035f }, { "Yossele_Pick", 0.03f },
        { "Yossele_Dig", 0.012f }, { "Yossele_Mend", 0.025f }, { "Yossele_Death", 0.06f },
        { "Yossele_Zap", 0.03f },
    };

    // ---------------------------------------------------------------- vessel texts (the game uses English text as the key)

    public const string Displayname = "Yossele";
    public const string Displaysub = "The Patient Golem";
    public const string Displayskill = "Intermediate";
    public const string HeroDesc = "Sturdy, single-target ranged that mends itself";
    public const string TooltipName = "Patient Growth";
    public const string TooltipDesc = "Sturdy fighter that mends itself, buildings grow a stage for every Tide they survive and lose it all when destroyed, a Direction can unlock FERTILIZE";
    // The scope: "Lore comes once every character is fully realized". A placeholder until then (DESIGN.md).
    public const string Lore = "Yossele's story comes later. For now it waits, and its garden grows.";
    public const string FertilizeName = "FERTILIZE";
    public const string FertilizeDesc = "Spend a fertilizer on one of your buildings: it grows a stage at once";
    public const string FertilizeSelectTitle = "FERTILIZE A BUILDING";
    public const string ColourName = "COLOUR";
    public const string ColourDesc = "Pick the aura this Sapling's fruit pays";

    // ---------------------------------------------------------------- vessel stats (the sheet; Smacky is 50/3/4/1.5/0.5)

    public static readonly string[] HeroStats =
    {
        "Health=70", "Speed=1.75", "Damage=5.5", "Range=4", "Projspeed=15", "Attackspeed=0.5", "Buildrange=2.5", "Sellrate=0.5",
    };
    public const float Regen = 1f;              // health a second, healed by the brain (no base vessel regenerates)

    // ---------------------------------------------------------------- Growth, FERTILIZE, fertilizer, Compost Bin

    public const string GrowthStat = "Growth";
    public const string FertilizerStat = "Fertilizer";
    public const int GrowthPerTide = 1, GrowthPerLongTide = 5;
    public const int LongTides = 4;             // a map of this many Tides or fewer has long Tides (the sheet)
    public const int GrowthSkins = 10;          // sprout skins drawn; stages past it reuse the last
    public const int FertilizerMax = 10;
    public const float FertilizeReach = 6f;     // the hotkey's fallback: nearest growable building of yours this close
    public const float FertilizeGuard = 0.5f;

    // Compost Bin, one Direction per rank (written for kit 1.6, which had no ranks; kit 1.7's Rank is not used yet): foes felled within Radius of Yossele for 1 fertilizer.
    public class Compost { public string Name, Display, Desc; public int Kills; public float Radius; }
    public static readonly Compost[] Compostbins =
    {
        new Compost { Name = "Compostbin", Display = "Compost Bin", Kills = 10, Radius = 4f,
            Desc = "Unlocks FERTILIZE for Yossele, foes felled near Yossele make fertilizer" },
        new Compost { Name = "Compostbin2", Display = "Compost Bin II", Kills = 6, Radius = 4f,
            Desc = "Unlocks FERTILIZE for Yossele, foes felled near Yossele make fertilizer faster" },
        new Compost { Name = "Compostbin3", Display = "Compost Bin III", Kills = 6, Radius = 7f,
            Desc = "Unlocks FERTILIZE for Yossele, foes felled within a much larger radius of Yossele make fertilizer faster" },
    };

    // The Tree's Directions (docs/scope/11), one per rank, Clash 1: a player holds one of the two.
    public class Direction { public string Name, Display, Desc; }
    // What a shifted Tree shows (decision 15): a skin per Shift, prefixed to the Growth skins, so the panel reads the new
    // name (the title is the skin entry's Displayname, Game_Ui, pitfall #164); the model is the Shift's own once there is
    // one, else the Tree's.
    public class ShiftLook { public string Skin, Display, Model; }
    public static readonly ShiftLook SaplingLook = new ShiftLook { Skin = "Sapling", Display = "Dandelion Sapling", Model = "Yossele_Dandelionsapling" };
    public static readonly ShiftLook MushboomLook = new ShiftLook { Skin = "Mushboom", Display = "Primordial Mushboom", Model = "Yossele_Mushboom" };
    public static readonly ShiftLook[] ShiftLooks = { SaplingLook, MushboomLook };
    public const float ShiftCheck = 0.5f;           // how often a Tree looks at them

    // Dandelion Season: no limit on Trees, each acts as a Dandelion Sapling. Its stages do less than a Tree's so its range
    // stays at 70% of a Tree's at any Growth; rank II's stages do 25% more than that.
    public static readonly Direction[] Seasons =
    {
        new Direction { Name = "Dandelionseason", Display = "Dandelion Season",
            Desc = "Your Primordial Dandelion Tree becomes a Dandelion Sapling, slightly weaker and slower to grow. Dandelion Saplings have no limit" },
        new Direction { Name = "Dandelionseason2", Display = "Dandelion Season II",
            Desc = "Dandelion Sapling's Growth scaling increases" },
        new Direction { Name = "Dandelionseason3", Display = "Dandelion Season III",
            Desc = "Dandelion Sapling's damage and range increase based on the number of Dandelion Saplings at max Growth" },
    };
    public const float SaplingShare = 0.7f;         // of the Tree's damage, range and stages; default, docs/scope/11
    public const float SaplingStage = 0.25f;        // rank II: each stage does this much more; default, docs/scope/11
    public const float SaplingPack = 0.05f;         // rank III: damage and range per other Tree at max Growth; default, docs/scope/11
    public const float SaplingPackMax = 0.5f;       // default, docs/scope/11

    // Mother of Mushrooms: each Tree acts as a Primordial Mushboom, bursting on spored foes instead of shooting.
    public static readonly Direction[] Mushboomkits =
    {
        new Direction { Name = "Mushrooms", Display = "Mother of Mushrooms",
            Desc = "Your Primordial Dandelion Tree is replaced by a Primordial Mushboom: it explodes in a large radius, dealing damage based on spore stacks on foes and consuming them. Growth increases its range" },
        new Direction { Name = "Mushrooms2", Display = "Mother of Mushrooms II",
            Desc = "Primordial Mushboom's range is increased" },
        new Direction { Name = "Mushrooms3", Display = "Mother of Mushrooms III",
            Desc = "Primordial Mushboom's explosion only consumes half of the spore stacks on a foe, while dealing the full damage" },
    };
    public const float MushboomEvery = 8f;          // seconds between bursts; default, docs/scope/11
    public const float MushboomPerStack = 2f;       // damage per spore stack; default, docs/scope/11
    public const float MushboomPerStage = 0.5f;     // tiles of range per Growth stage, in place of the Tree's 20%
    public const float MushboomRange = 0.25f;       // rank II

    // Claude's Directions for Yossele (docs/scope/11, built 2026-11-05): one upgrade a rank like the Shifts above; the
    // numbers a rank are scope 11's (YosseleCuttings, YosseleBoosts, the Stonehenge).
    public static readonly Direction[] Cuttingjars =
    {
        new Direction { Name = "Cuttingjar", Display = "Cutting Jar",
            Desc = "A building that falls leaves a cutting: rebuild it on the same tile soon and it keeps some of its Growth" },
        new Direction { Name = "Cuttingjar2", Display = "Cutting Jar II", Desc = "The cutting keeps more of its Growth" },
        new Direction { Name = "Cuttingjar3", Display = "Cutting Jar III", Desc = "The cutting keeps most of its Growth, and waits longer" },
    };
    public static readonly float[] CuttingKeep = { 0.25f, 0.5f, 0.75f }, CuttingWait = { 20f, 20f, 40f };
    public static readonly Direction[] Sapbuckets =
    {
        new Direction { Name = "Sapbucket", Display = "Sap Bucket", Desc = "Yossele's laser leaves sticky sap that slows foes" },
        new Direction { Name = "Sapbucket2", Display = "Sap Bucket II", Desc = "The sap slows more" },
        new Direction { Name = "Sapbucket3", Display = "Sap Bucket III",
            Desc = "The sap slows much more, and foes stuck in it take more damage from your towers" },
    };
    public static readonly float[] SapSlow = { 0.15f, 0.25f, 0.35f };
    public const float SapLasts = 2f, SapBonus = 0.1f;   // seconds; rank III: more damage taken from your buildings
    public static readonly Direction[] Creepingivies =
    {
        new Direction { Name = "Creepingivy", Display = "Creeping Ivy", Desc = "Your vines reach farther" },
        new Direction { Name = "Creepingivy2", Display = "Creeping Ivy II", Desc = "Foes on your vines slow down" },
        new Direction { Name = "Creepingivy3", Display = "Creeping Ivy III", Desc = "Your vines grow farther every Tide" },
    };
    public const int IvyReach = 1, IvyMax = 3;               // tiles; rank III: at most this much more
    public const float IvySlow = 0.15f, IvyPerTide = 0.5f;   // rank II: on top of the stones' own; rank III a Tide
    public static readonly Direction[] Deepbreaths =
    {
        new Direction { Name = "Deepbreath", Display = "Deep Breath", Desc = "When Yossele has not fired for a moment, its next beam hits harder" },
        new Direction { Name = "Deepbreath2", Display = "Deep Breath II", Desc = "It hits harder still" },
        new Direction { Name = "Deepbreath3", Display = "Deep Breath III", Desc = "It hits much harder, and reaches farther" },
    };
    public static readonly float[] BreathBonus = { 0.4f, 0.7f, 1f };
    public const float BreathAfter = 2f, BreathRange = 1f;   // seconds without a beam; rank III: tiles for that beam
    public static readonly Direction[] Fairyrings =
    {
        new Direction { Name = "Fairyring", Display = "Fairy Ring", Desc = "When Yossele stands still, mushrooms ring it and mend what is near" },
        new Direction { Name = "Fairyring2", Display = "Fairy Ring II", Desc = "The ring mends more" },
        new Direction { Name = "Fairyring3", Display = "Fairy Ring III", Desc = "The ring mends much more, farther out" },
    };
    public static readonly float[] FairyRadius = { 1.5f, 2f, 2.5f }, FairyHeal = { 0.01f, 0.015f, 0.02f };   // tiles; of max health a second
    public const float FairyStill = 1.5f;                    // seconds standing still before the ring grows

    // ---------------------------------------------------------------- Mushroom Cannon, its spores, the Cordyfriend

    public const float SporeDps = 0.3f;         // damage a second per stack
    public const float SporeFade = 6f;          // every stack falls off this long after the last new one
    public const int SporeMax = 10;
    public const float SporeTick = 0.5f;
    public const float LobBase = 0.6f, LobPerTile = 0.08f;   // flight time: Bomb Tower's lobs take about a second
    public const float CannonGrowSpeed = 0.1f;               // attack speed per stage (adds, like every percentage)
    public const float CannonGrowRadius = 0.1f;
    public const float BounceDelay = 1f, BounceShare = 0.5f;
    public const int BounceSpores = 1;
    public const float FaroutMax = 0.75f;
    public const float BurstGuard = 1f;

    public const string FriendDisplay = "Cordyfriend";
    public const string FriendDesc = "Tanky melee minion that rises from fallen foes";
    public const string FriendLore = "It was a foe until the spores got in. Now it holds the line beside the ones it came to topple.";
    public static readonly string[] FriendStats = { "Health=75", "Damage=2", "Range=1.5", "Attackspeed=0.5", "Speed=2.25" };
    public const int FriendCap = 30;            // Cordyfriends standing at once per player
    public const float FriendEngage = 4f;       // it walks to foes this close
    public const float FriendLeash = 5f;        // ...that are this close to where it rose
    public static readonly Color FriendTint = new Color(0.72f, 0.95f, 0.62f, 1f);   // fungus green over the Snoble's sprite, only if its own sheet is missing

    // ---------------------------------------------------------------- Primordial Dandelion Tree and its Bonsai

    public const int SeedsPerStage = 3;
    public const float RangePerStage = 0.2f;    // of its base range, added each stage
    public const float SeedFlight = 0.3f;
    public const int SeedsDrawn = 24;           // the most ready seeds a client draws round the crown
    public const float PacketsWindow = 0.6f;    // a foe that falls this soon after a seed hit was felled by it
    public const float InterceptEvery = 0.1f;   // D-MDS looks for foes' projectiles this often
    public const float BossHealth = 600f;       // a shooter of this base health or more is a boss (kit docs/05 §16)
    public const float BonsaiShare = 0.4f;      // the Bonsai: 40% of the Tree (the sheet)
    // Tree upgrade params the Bonsai copies unchanged: chances, thresholds, distances, durations, costs and every
    // penalty (a negative number). The rest are effects, copied at 40%; Counts are rounded, never under 1.
    public static readonly string[] BonsaiKept = { "Demand_Below", "Weeds_Window", "Weeds_Chance", "Weeds_Last", "Dmds_Boss", "Rubbery_Range" };
    public static readonly string[] BonsaiCounts = { "Packets_Refund", "Rubbery_Bounces" };

    // ---------------------------------------------------------------- Overgrown Stonehenge and its vines

    public const float HengeArmorPerStage = 0.1f;
    public const float ArmorCap = 0.9f;
    public const float VineTick = 0.5f;
    public const float VineReach = 0.55f;       // a foe this close (flat) to a vine tile's centre stands on it
    public const float EasyLinger = 1f;         // Easy Target's mark lasts this long after a foe steps off
    public const float CleanseEvery = 0.1f;     // Monument sweeps its debuffs this often

    // ---------------------------------------------------------------- Pachira Sapling and aura fruit

    public const float PachiraGrowSpeed = 0.1f, PachiraGrowRange = 0.4f;
    public const float FruitScale = 15f;        // a fruit pays Attackspeed x Damage x 15 of every aura colour (the sheet)
    public const float FruitReach = 0.75f;
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

    // The sheet's four levels per building are the game's tiers 2-5; every choice of a level leads to every choice of
    // the next, except where the sheet says "Leads to" (the Tree's level 1).
    public static readonly Build[] Builds =
    {
        new Build
        {
            Id = Cannon, Template = "Tower_Smackybomber", Display = "Mushroom Cannon", Tag = "Yosselecannon", Tags = new[] { "Tower", "Towerfighter" },
            Desc = "Lobs spore bombs that stack spores, damaging foes over time", Costs = "Redaura=15", Max = 5, Muzzle = 0.6f,
            Lore = "It coughs up spore bombs in long, lazy arcs. Whatever they land on starts to rot, a little more with every hit.",
            Stats = new[] { "Health=20+25", "Damage=1+0.25", "Attackspeed=0.4", "Range=9", "Radius=1", "Spores=2" },
            Upgrades = new[]
            {
                U(Cannon, "Sticky", 2, "Sticky Spores", "Fully spored foes are slowed", "Sticky_Slow=0.25", "Redaura=50", "Viral3,Deathcap3"),
                U(Cannon, "Farout", 2, "Farout Fungus", "Spore bombs increase in potency while in the air", "Range_Add=2;Farout_Per=0.5", "Redaura=45;Blueaura=10", "Viral3,Deathcap3"),
                U(Cannon, "Viral", 3, "Viral Outbreak", "Fully spored foes may burst when felled", "Viral_Chance=0.3;Viral_Damage=15;Viral_Radius=2;Viral_Spores=1", "Redaura=90", "Finish4,Hallucinogen4"),
                U(Cannon, "Deathcap", 3, "Deathcap Training", "Deadlier spores, fewer of them", "Deathcap_Multi=0.25;Sporemax_Base=8", "Redaura=100", "Finish4,Hallucinogen4"),
                U(Cannon, "Finish", 4, "Explosive Finish", "Full spores burst, spreading the rot", "Finish_Per=1;Finish_Radius=1.25;Finish_Spores=2", "Redaura=420", "Cordyceps5"),
                U(Cannon, "Hallucinogen", 4, "Hallucinogen", "Fully spored foes are stunned momentarily", "Hallu_Every=5;Hallu_Chance=0.2;Hallu_Stun=2", "Redaura=360;Blueaura=280", "Cordyceps5"),
                U(Cannon, "Cordyceps", 5, "Cordyceps", "Once a foe, now a friend!", "Cordy_Chance=0.2;Cordy_Per=0.05", "Redaura=900;Blueaura=1250", ""),
            }
        },
        new Build
        {
            Id = Dandelion, Template = "Tower_Smackyarcher", Display = "Primordial Dandelion Tree", Tag = "Yosseledandelion", Tags = new[] { "Tower", "Towerfighter" },
            Desc = "Releases seeds that strike targets who wander too close", Costs = "Redaura=50", Max = 15, Muzzle = 1.1f,
            Lore = "The oldest dandelion there is. It keeps its seeds close until something walks too near, then lets them all go.",
            Stats = new[] { "Health=50+25", "Damage=2+0.25", "Range=7", "Seedlimit=10", "Seedrate=0.5" },
            Upgrades = new[]
            {
                U(Dandelion, "Packets", 2, "Seed Packets", "Hardened seeds that come back after felling a foe.", "Packets_Refund=2;Damage_Add=3", "Redaura=40", "Picky3,Weeds3"),
                U(Dandelion, "Demand", 2, "Demand and Supply", "Grows seeds faster when few are ready", "Demand_Max=1;Demand_Below=0.25", "Redaura=20;Yellowaura=40", "Dmds3,Picky3,Weeds3"),
                U(Dandelion, "Picky", 3, "Picky Placement", "Seeds go for the toughest foes and bite deeper", "Picky_Percent=0.0025", "Redaura=100", "Bonsai4,Rubbery4"),
                U(Dandelion, "Weeds", 3, "Garden Weeds", "Chance to recharge seeds quicker when enemies recently damaged die.", "Weeds_Per=0.1;Weeds_Window=3;Weeds_Chance=0.4;Weeds_Last=15", "Redaura=90;Yellowaura=30", "Bonsai4,Rubbery4"),
                U(Dandelion, "Dmds", 3, "D-MDS", "Can no longer damage foes, but now intercepts enemy projectiles.", "Seedrate_Multi=-0.5;Dmds_Boss=5", "Blueaura=150;Yellowaura=40", "Bonsai4,Rubbery4"),
                U(Dandelion, "Bonsai", 4, "Bonsai", "Plant a copy of the Primordial Dandelion Tree with reduced effects.", "Bonsai_Share=0.4", "Redaura=300;Blueaura=300;Yellowaura=300", "Storm5"),
                U(Dandelion, "Rubbery", 4, "Rubbery Shell", "Seeds bounce between targets.", "Rubbery_Bounces=2;Rubbery_Range=2", "Redaura=450;Blueaura=600", "Storm5"),
                U(Dandelion, "Storm", 5, "Dandelion Storm", "Huge range, many more seeds, grown faster", "Range_Multi=1;Seedlimit_Add=20;Seedrate_Multi=1", "Redaura=1050;Blueaura=800;Yellowaura=2400", ""),
            }
        },
        new Build
        {
            Id = Henge, Template = "Tower_Smackyhut", Display = "Overgrown Stonehenge", Tag = "Yosselehenge", Tags = new[] { "Tower" },
            Desc = "Sturdy stones, vines creep forward and slow foes", Costs = "Blueaura=25", Max = 8, Muzzle = 0.5f,
            Lore = "Old stones the moss took back. As they grow, their vines creep further out, and foes wade through them slowly.",
            Stats = new[] { "Health=80+40", "Slow=0.15" },
            Upgrades = new[]
            {
                U(Henge, "Verdant", 2, "Verdant Spread", "Vines spread all around", "Verdant_Reach=2;Verdant_Full=3", "Blueaura=30", "Remedy3,Carnivore3,Easy3"),
                U(Henge, "Blockade", 2, "Vine Blockade", "A vine barrier each Tide, sturdier stones", "Health_Multi=0.5;Blockade_Share=0.3", "Blueaura=35", "Remedy3,Carnivore3,Easy3"),
                U(Henge, "Remedy", 3, "Natural Remedy", "Vines mend buildings", "Remedy_Heal=0.5", "Blueaura=90", "Roots4,Obsidian4"),
                U(Henge, "Carnivore", 3, "Opportunistic Carnivore", "Vines drain foes to mend the stones", "Carnivore_Drain=5;Carnivore_Max=5", "Blueaura=80;Redaura=30", "Roots4,Obsidian4"),
                U(Henge, "Easy", 3, "Easy Target", "Vines eventually cause foes to take more damage", "Easy_After=5;Easy_Bonus=0.15", "Blueaura=40;Redaura=75", "Roots4,Obsidian4"),
                U(Henge, "Roots", 4, "Entangling Roots", "Lingering foes are snared and sapped", "Roots_After=7;Roots_Stun=1.5;Roots_Percent=0.05", "Blueaura=420;Redaura=300", "Monument5"),
                U(Henge, "Obsidian", 4, "Obsidian", "Massively increases health", "Health_Multi=2", "Blueaura=600", "Monument5"),
                U(Henge, "Monument", 5, "Monument", "Withstand the test of time", "Monument_Armor=0.2", "Blueaura=2000;Yellowaura=1500", ""),
            }
        },
        new Build
        {
            Id = Pachira, Template = "Tower_Smackyhut", Display = "Pachira Sapling", Tag = "Yosselepachira", Tags = new[] { "Tower", "Gatherer" },
            Desc = "Its roots mine every ore node in reach, fully grown it drops aura fruit", Costs = "Yellowaura=35", Max = 5, Muzzle = 0.5f,
            Lore = "A money tree, still small. Its roots find every ore node nearby and drink from all of them at once.",
            Stats = new[] { "Health=30+20", "Damage=1", "Attackspeed=0.25", "Range=2.5" },
            Upgrades = new[]
            {
                U(Pachira, "Focused", 2, "Focused Roots", "Mines one node much faster", "Focused_Multi=0.8", "Yellowaura=30", "Grafted3,Forest3,Mulch3"),
                U(Pachira, "Sustaining", 2, "Sustaining Practices", "Mends itself from the ore around it", "Sustain_Heal=0.5", "Yellowaura=25;Blueaura=10", "Grafted3,Forest3,Mulch3"),
                U(Pachira, "Grafted", 3, "Grafted", "Pick the colour of its fruit", "Grafted_Efficiency=0.7", "Redaura=60;Blueaura=60;Yellowaura=60", "Connected4,Harvest4,Web4"),
                U(Pachira, "Forest", 3, "Forest Project", "Mines faster for each other Sapling nearby", "Forest_Per=0.1;Forest_Range=4", "Yellowaura=140", "Connected4,Harvest4,Web4"),
                U(Pachira, "Mulch", 3, "Spontaneous Mulch", "Foes dying within range increase mining speed", "Mulch_Bonus=0.2;Mulch_Last=5", "Redaura=40;Yellowaura=30", "Connected4,Harvest4,Web4"),
                U(Pachira, "Connected", 4, "Connected Roots", "Takes hits for buildings nearby while healthy", "Connected_Share=0.65;Connected_Above=0.5", "Yellowaura=120;Blueaura=300", "Fortifying5,Softening5"),
                U(Pachira, "Harvest", 4, "Harvesting Season", "Fruit is worth double", "Harvest_Multi=1", "Yellowaura=400", "Fortifying5,Softening5"),
                U(Pachira, "Web", 4, "Wood Wide Web", "Trades spare aura for what you lack", "Web_Gap=100;Web_Take=2;Web_Give=1", "Yellowaura=250", "Fortifying5,Softening5"),
                U(Pachira, "Fortifying", 5, "Fortifying Roots", "Increases health and grants a damage reduction aura to nearby buildings.", "Health_Multi=1;Fortify_Armor=0.3", "Blueaura=2300;Yellowaura=1000", ""),
                U(Pachira, "Softening", 5, "Softening Roots", "Softens the ore around it", "Soften_Points=1", "Yellowaura=2300;Redaura=1000", ""),
            }
        },
        // Only with the Tree's Bonsai upgrade (the Tree's build slot), the Tree's cost, 40% of its stats, no upgrades of
        // its own: it copies the owner's Tree's (YosseleBonsaiBrain). Sells for 0.
        new Build
        {
            Id = Bonsai, Template = "Tower_Smackyarcher", Display = "Bonsai", Tag = "Yosselebonsai", Tags = new[] { "Tower", "Towerfighter" },
            Desc = "A copy of your Primordial Dandelion Tree at 40%", Costs = "Redaura=50", Max = 15, Muzzle = 0.7f,
            Lore = "A cutting of the oldest dandelion, grown in a pot. It learns whatever the great Tree learns, a little less well.",
            Stats = new[] { "Health=20+10", "Damage=0.8+0.1", "Range=2.8", "Seedlimit=4", "Seedrate=0.2" },
            Upgrades = new Upg[0],
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
        new[] { "Sticky", "Slow at full spores" }, new[] { "Farout", "Blast per second of flight" }, new[] { "Viral", "" },
        new[] { "Deathcap", "Spore damage" }, new[] { "Sporemax", "Most spores" }, new[] { "Finish", "" }, new[] { "Hallu", "" },
        new[] { "Cordy", "" }, new[] { "Packets", "Seeds back" }, new[] { "Demand", "" }, new[] { "Picky", "Of max health" },
        new[] { "Weeds", "" }, new[] { "Dmds", "" }, new[] { "Bonsai", "" }, new[] { "Rubbery", "" }, new[] { "Seedlimit", "Seeds" },
        new[] { "Seedrate", "Seed growth" }, new[] { "Verdant", "" }, new[] { "Blockade", "Barrier" }, new[] { "Remedy", "Healing per stage" },
        new[] { "Carnivore", "" }, new[] { "Easy", "" }, new[] { "Roots", "" }, new[] { "Monument", "Damage taken" },
        new[] { "Focused", "Mining speed" }, new[] { "Sustain", "Healing" }, new[] { "Grafted", "Fruit value" }, new[] { "Forest", "" },
        new[] { "Mulch", "" }, new[] { "Connected", "" }, new[] { "Harvest", "Fruit value" }, new[] { "Fortify", "Aura" },
        new[] { "Soften", "" }, new[] { "Web", "" }, new[] { "Radius", "Radius" }, new[] { "Spores", "Spores" },
    };
}
