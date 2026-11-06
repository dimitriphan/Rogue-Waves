// Every name, text and tuning number for Spool in one place, from Dimitri's sheet (docs/scope/08_Vessel_Spool.txt) and
// the defaults listed in ModSource/Spool/DESIGN.md: a melee vessel, a Yarn Stash placed first like Boutman's
// Bouthouse, four buildables (one of them a unit bought from the build menu like Boutman's fish), Crafted units paid
// for with Thread, and PATCH on Buster's Dash keys.
public static class SpoolData
{
    // A Rogue Waves part carries the pack version, "-dev" for the dev mod (docs/PACK.md section 4); Part names it in the
    // pack registry (ModKitPack.Join).
    public const string Version = "0.1.0-dev";
    public const string Part = "Spool";
    public const bool Diagnostics = true;
    public const float FirstLookDelay = 0.5f;   // a new thing's first look waits for the client's 0.25 s hook scan (pitfall #158)

    public const string TemplateHero = "Hero_Smacky";
    // Other vessels' asset groups Spool borrows from: Buster's walk-through pad block (the Thread Head's placement token
    // and the Cookie Tin's pin), Sno Sno's mob-blocker block (Overstuffed Mangled). The game never unloads a group.
    public static readonly string[] ExtraGroups = { "Hero_Buster", "Hero_Snosno" };

    public const string Hero = "Hero_Spool";
    public const string HeroEntity = "Hero_Spool_Entity";
    public const string HeroTooltip = "Hero_Spool_Tooltip_Upgradeproperties";

    public const string Stash = "Tower_Spoolstash";
    public const string Tin = "Tower_Spooltin";
    public const string Head = "Mob_Spoolhead";
    public const string HeadToken = "Mob_Spoolhead_Place";
    public const string Cushion = "Tower_Spoolcushion";
    public const string Loom = "Tower_Spoolloom";
    public const string Pin = "Trap_Spoolpin";
    // The build menu once the Stash stands (Buildoption1-4). The boat's character screen draws Visprefabs, so it shows
    // the Stash in the unit's place.
    public static readonly string[] Buildables = { Tin, HeadToken, Cushion, Loom };
    public static readonly string[] Towerdisplay = { Stash, Tin, Cushion, Loom };

    public const string PatchCommand = "Command_Hero_Spool_Patch", PatchLink = "Patch";
    public const string TargetCommand = "Command_Tower_Spooltin_Target", TargetLink = "Target";
    public const string SpotCommand = "Command_Mob_Spoolhead_Spot", SpotLink = "Spot";
    public const string MoveLink = "Move";
    public const string ReturnCommand = "Command_Mob_Spoolhead_Return", ReturnLink = "Return";
    public const string MendCommand = "Command_Mob_Spoolhead_Mend", MendLink = "Mend";
    public const string BeeCommand = "Command_Tower_Spoolstash_Bee", BeeLink = "Bee";
    public const string UnthreadCommand = "Command_Tower_Spooltin_Unthread", UnthreadLink = "Unthread";
    public const string ColourCommand = "Command_Tower_Spoolloom_Colour", ColourLink = "Colour";
    public const string Needle = "Mob_Spooldart_Needle";

    // ---------------------------------------------------------------- sound levels (matched to the game's world SFX)

    public static readonly Dictionary<string, float> SoundVolume = new Dictionary<string, float>
    {
        { "Spool_Swat", 0.03f }, { "Spool_Stitch", 0.03f }, { "Spool_Pop", 0.02f }, { "Spool_Down", 0.025f },
        { "Spool_Burst", 0.04f }, { "Spool_Tuft", 0.012f }, { "Spool_Deliver", 0.02f }, { "Spool_Prick", 0.02f },
        { "Spool_Patch", 0.035f }, { "Spool_Needle", 0.015f }, { "Spool_Landing", 0.1f }, { "Spool_Death", 0.03f },
        { "Spool_Footstep", 0.008f }, { "Spool_Spin", 0.012f },
    };

    // ---------------------------------------------------------------- vessel texts (the game uses English text as the key)

    public const string Displayname = "Spool";
    public const string Displaysub = "Crafty Crafted Crafter";
    public const string Displayskill = "Advanced";
    public const string HeroDesc = "Crafty melee, commands a stitched army";
    public const string TooltipName = "Hand Stitched";
    public const string TooltipDesc = "Melee, foes drop Thread that Spool or a Thread Head carries to the Yarn Stash, Crafted cost Thread, PATCH mends a unit or building";
    // The sheet: "Lore comes once the vessel is finalized".
    public const string Lore = "Spool's story is still being stitched.";
    public const string PatchName = "PATCH";
    public const string PatchDesc = "Spend 20 Thread from the Yarn Stash to mend one of your units or buildings";
    public const string PatchSelectTitle = "PATCH WHAT?";
    public const string TargetName = "TARGET";
    public const string TargetDesc = "Pick where this Tin sends its Crafted";
    public const string SpotName = "SPOT";
    public const string SpotDesc = "Pick the spot this Thread Head guards";
    public const string ReturnName = "RETURN";
    public const string ReturnDesc = "Carry the Thread home, then come back";
    public const string MendName = "MEND";
    public const string MendDesc = "Mend nearby units with carried Thread";
    public const string BeeName = "BEE";
    public const string BeeDesc = "Quilting Bee, once a Tide: every Cookie Tin stitches its whole count at once for no Thread, and the Crafted hit harder for a while";
    public const string UnthreadName = "UNTHREAD";
    public const string UnthreadDesc = "Sell every Crafted this Tin has out, for half their Thread back";
    public const string ColourName = "COLOUR";
    public const string ColourDesc = "Dyed in the Wool: pick the colour of the aura this Loom and its Seams mine";
    // Dyed in the Wool's COLOUR cycles these (Uloe's Colour Filter and Yossele's Grafted way).
    public static readonly string[] Colours = { "All", "Redaura", "Blueaura", "Yellowaura" };
    public static readonly string[] ColourNames = { "ALL", "RED", "BLUE", "YELLOW" };

    // ---------------------------------------------------------------- vessel stats (the sheet's)

    public static readonly string[] HeroStats =
    {
        "Health=45", "Speed=2.75", "Damage=2.5", "Range=1.5", "Attackspeed=0.6", "Buildrange=2.5", "Sellrate=0.5",
    };

    // ---------------------------------------------------------------- Thread

    public const int StashStart = 10;
    public const float StashCap = 50f;                     // the Stash's Threadcap stat, +25 per level
    public const int CarryHero = 20, CarryHead = 20;
    public const int DropSmall = 1, DropLarge = 3, DropElite = 2, DropMax = 5;
    public const float LargeHealth = 40f;                  // a foe's base health from which it counts as large
    public const float DropLife = 15f;
    public const float PickHero = 1f, PickHead = 3f, DropOff = 1.5f, LoomPick = 1.5f;
    public const float Merge = 0.5f;                       // a new drop this close to another joins it
    public const int TuftMax = 15;                         // one tuft carries at most this much (the Vis number packs it)

    // ---------------------------------------------------------------- PATCH

    public const int PatchCost = 20;
    public const float PatchHeal = 0.2f, PatchMax = 500f, PatchRange = 4f, PatchCooldown = 5f;

    // ---------------------------------------------------------------- Directions (docs/scope/11), one per rank

    // Efficient Recycler: selling a Crafted refunds Percent of its cost (half without it); rank III also mends around it.
    public class Recycler { public string Name, Display, Desc; public int Percent; public float Heal, Radius; }
    public static readonly Recycler[] Recyclers =
    {
        new Recycler { Name = "Recycler", Display = "Efficient Recycler", Percent = 65,
            Desc = "Selling a Crafted gives back more of its Thread" },
        new Recycler { Name = "Recycler2", Display = "Efficient Recycler II", Percent = 80,
            Desc = "Selling a Crafted gives back even more of its Thread" },
        new Recycler { Name = "Recycler3", Display = "Efficient Recycler III", Percent = 100, Radius = 2.5f,
            Heal = 10f,   // default, docs/scope/11
            Desc = "Selling a Crafted gives back all its Thread and mends units and buildings around it" },
    };

    // Sewing Machine: every Loom of this player mines faster, spins no Thread and mends around itself each Tide.
    public const float SewingMine = 0.5f;   // +50% mining, as a Multi on the Loom's Attackspeed; default, docs/scope/11
    // A Loom of a player holding it shows this skin: the panel reads Sewing Machine, and the model is Spool_Sewingmachine
    // once there is one (decision 15; until then the Loom's own model).
    public const string SewingSkin = "Sewing", SewingDisplay = "Sewing Machine", SewingModel = "Spool_Sewingmachine";
    public class Sewing { public string Name, Display, Desc; public float Radius, Heal; }
    public static readonly Sewing[] Sewingmachines =
    {
        new Sewing { Name = "Sewingmachine", Display = "Sewing Machine", Radius = 3f, Heal = 10f,
            Desc = "Looms become Sewing Machines: they mine aura quicker and heal nearby buildings and units each Tide, but make no Thread" },
        new Sewing { Name = "Sewingmachine2", Display = "Sewing Machine II", Radius = 3f, Heal = 20f,
            Desc = "Sewing Machines heal more" },
        new Sewing { Name = "Sewingmachine3", Display = "Sewing Machine III", Radius = 5f, Heal = 20f,
            Desc = "Sewing Machines heal in a much larger radius" },
    };

    // Claude's five (docs/scope/11, built 2026-11-05), one upgrade per rank, no clash; the numbers are the sheet's.
    public class Direction { public string Name, Display, Desc; }
    // Double Needle: a Tin's stitch time is cut; rank III: every third stitch makes one more Crafted for no Thread.
    public static readonly Direction[] Doubleneedles =
    {
        new Direction { Name = "Doubleneedle", Display = "Double Needle", Desc = "Your Repurposed Cookie Tins stitch faster" },
        new Direction { Name = "Doubleneedle2", Display = "Double Needle II", Desc = "They stitch faster still" },
        new Direction { Name = "Doubleneedle3", Display = "Double Needle III", Desc = "Every third Crafted they stitch comes out as two" },
    };
    public static readonly float[] NeedleCut = { 0.15f, 0.3f, 0.3f };
    public const int NeedleEvery = 3;
    // Button Jar (a Shift on the Tin): each stitch makes two smaller Crafted for one's Thread, each with this share of
    // the health and damage; a Tin holds twice as many.
    public static readonly Direction[] Buttonjars =
    {
        new Direction { Name = "Buttonjar", Display = "Button Jar", Desc = "Your Repurposed Cookie Tins stitch Crafted two at a time, smaller and weaker, for the Thread of one" },
        new Direction { Name = "Buttonjar2", Display = "Button Jar II", Desc = "The small Crafted are sturdier" },
        new Direction { Name = "Buttonjar3", Display = "Button Jar III", Desc = "The small Crafted are sturdier still and hit harder" },
    };
    public static readonly float[] ButtonShare = { 0.55f, 0.65f, 0.75f };
    public const int ButtonPair = 2;
    // Pattern Book: once a Tide, the first Crafted of each Tin to fall are stitched again for free (rank III at once).
    public static readonly Direction[] Patternbooks =
    {
        new Direction { Name = "Patternbook", Display = "Pattern Book", Desc = "Once a Tide, the first Crafted of each Repurposed Cookie Tin to fall is stitched again for free" },
        new Direction { Name = "Patternbook2", Display = "Pattern Book II", Desc = "The first two are" },
        new Direction { Name = "Patternbook3", Display = "Pattern Book III", Desc = "The first three are, at once" },
    };
    public static readonly int[] PatternFree = { 1, 2, 3 };
    // Merit Badge: a Crafted gains damage and health for each foe it drowns (its kills), up to a cap; lost when it falls.
    public static readonly Direction[] Meritbadges =
    {
        new Direction { Name = "Meritbadge", Display = "Merit Badge", Desc = "Your Crafted grow stronger with every foe they drown" },
        new Direction { Name = "Meritbadge2", Display = "Merit Badge II", Desc = "They grow further" },
        new Direction { Name = "Meritbadge3", Display = "Merit Badge III", Desc = "They grow much further" },
    };
    public const float MeritEach = 0.03f;
    public static readonly float[] MeritCap = { 0.15f, 0.3f, 0.45f };
    // Odd Socks: a Crafted deals more while another kind of Crafted is near; rank III: it also takes less damage.
    public static readonly Direction[] Oddsocks =
    {
        new Direction { Name = "Oddsocks", Display = "Odd Socks", Desc = "Your Crafted fight better beside a different kind of Crafted" },
        new Direction { Name = "Oddsocks2", Display = "Odd Socks II", Desc = "Better still" },
        new Direction { Name = "Oddsocks3", Display = "Odd Socks III", Desc = "Much better, and they take less damage" },
    };
    public static readonly float[] SocksBonus = { 0.08f, 0.15f, 0.25f };
    public const float SocksRange = 2f, SocksGuard = 0.1f;   // tiles; rank III: less damage taken

    // ---------------------------------------------------------------- Crafted and other units

    public const string Stitched = "Mob_Spoolstitched", Dart = "Mob_Spooldart", Seam = "Mob_Spoolseam";
    public const string Mangled = "Mob_Spoolmangled", Stuffed = "Mob_Spoolmangled_Stuffed";
    // Tacking Stitch's Stitched: a Stitched on Sno Sno's mob-blocker block that holds its spot (scope 08, 2026-11-06).
    public const string Tacked = "Mob_Spoolstitched_Tacked";

    // Button Jar's small Crafted wear this skin: their SmallSheet's look (a whole one's when the library has no such sheet).
    public const string SmallSkin = "Small";

    public class Unit
    {
        public string Id, Display, Desc, Lore, Sheet, SmallSheet;
        public string Family;   // the kind it counts as (Odd Socks): a Tacked Stitched is a Stitched; null: its own Id
        public int Thread, Cell = 16;
        public bool Ranged, Blocker, Sellable;
        public float Leash;
        public string[] Stats;
    }

    public static readonly Unit[] Units =
    {
        new Unit { Id = Stitched, Display = "Stitched", Sheet = "Spoolstitched-Sheet", SmallSheet = "Spoolstitchedsmall-Sheet", Thread = 10, Leash = 2f, Sellable = true,
            Desc = "Short and stubby, single-target melee",
            Lore = "A short, stubby plush stitched in four seconds flat. It runs where it's sent and stays there.",
            Stats = new[] { "Health=20", "Speed=1.8", "Damage=5", "Range=1.5", "Attackspeed=0.8" } },     // Damage: the balance sweep 2026-11-05 (one wall)
        new Unit { Id = Tacked, Display = "Tacked Stitched", Sheet = "Spoolstitched-Sheet", SmallSheet = "Spoolstitchedsmall-Sheet", Thread = 10, Leash = 2f, Blocker = true, Sellable = true, Family = Stitched,
            Desc = "Short and stubby, holds the line",
            Lore = "A Stitched tacked down at the seams. It runs to its spot and stays put, and nothing walks through it.",
            Stats = new[] { "Health=20", "Speed=1.8", "Damage=5", "Range=1.5", "Attackspeed=0.8" } },
        new Unit { Id = Dart, Display = "Dart", Sheet = "Spooldart-Sheet", SmallSheet = "Spooldartsmall-Sheet", Thread = 15, Leash = 1f, Ranged = true, Sellable = true,
            Desc = "Thin and small, throws needles",
            Lore = "A thin little plush with a pocket full of needles. It keeps close to its spot and throws.",
            Stats = new[] { "Health=12", "Speed=1.6", "Damage=5.5", "Range=3.5", "Attackspeed=0.6", "Projspeed=14" } },     // Damage: the balance sweep
        new Unit { Id = Seam, Display = "Seam", Sheet = "Spoolseam-Sheet", Leash = 2.5f,
            Desc = "Round and weak, mines and guards its Loom",
            Lore = "A round plush the Loom sews from offcuts. It picks at the ore and pokes at anything that comes close.",
            Stats = new[] { "Health=10", "Speed=1.5", "Damage=1", "Range=1.5", "Attackspeed=0.8" } },
        new Unit { Id = Mangled, Display = "Mangled", Sheet = "Spoolmangled-Sheet", Leash = 2f,
            Desc = "Jagged scraps, single-target melee",
            Lore = "What falls out of a Pin Cushion that has been hit too often. It is not happy about it.",
            Stats = new[] { "Health=15", "Speed=2", "Damage=1.5", "Range=1.5", "Attackspeed=0.8" } },
        new Unit { Id = Stuffed, Display = "Overstuffed Mangled", Sheet = "Spoolmangled-Sheet", Leash = 0.5f, Blocker = true,
            Desc = "Overstuffed, leaps in and blocks",
            Lore = "Stuffed three times over. It leaps in front of whatever is hitting its Cushion and stays put.",
            Stats = new[] { "Health=45", "Speed=2", "Damage=1.5", "Range=1.5", "Attackspeed=0.8" } },
    };

    public static Unit UnitFor(string being) { foreach (Unit u in Units) if (u.Id == being) return u; return null; }
    public static string FamilyOf(Unit u) { return u == null ? "" : u.Family ?? u.Id; }

    public const float CraftedEngage = 3f;       // how far a Crafted at its spot looks for foes (inside its leash)
    public const float StitchedSpeedUp = 0.3f;
    public const float TackedNear = 1.6f;        // Tacking Stitch: a Tacked that stopped this close to its spot holds there
    public const int TackedTries = 4;            // ...and one sent this many times holds wherever it stopped

    // The Thread Head: a unit bought from the build menu, with its own upgrades (it is one of the four buildables).
    public static readonly string[] HeadStats =
    {
        "Health=60", "Speed=2.25", "Damage=2", "Range=1.5", "Attackspeed=0.6", "Carry=20", "Collect=3", "Leash=3", "Respawn=15",
    };

    // ---------------------------------------------------------------- buildables

    public class Upg
    {
        public string Owner, Key, Display, Desc, Params, Costs;
        public int Tier;
        public string[] Next, Effects;
        public string Id { get { return Owner + "_" + Key + Tier + "_Upgradeobject"; } }
        public string Model { get { return Owner + "_" + Key + Tier; } }
    }

    public class Build
    {
        public string Id, Template, Display, Desc, Lore, Tag, Costs;
        public bool IsUnit;
        public string[] Tags, Stats;
        public Upg[] Upgrades;
    }

    static Upg U(string owner, string key, int tier, string display, string desc, string pars, string costs, string next, params string[] effects)
    {
        return new Upg
        {
            Owner = owner, Key = key, Tier = tier, Display = display, Desc = desc, Params = pars, Costs = costs,
            Next = string.IsNullOrEmpty(next) ? new string[0] : next.Split(','), Effects = effects,
        };
    }

    // The sheet's levels 1-4 are the game's tiers 2-5, one upgrade per level; every choice of a level leads to every
    // choice of the next except on the Pin Cushion: Pin Barrage leads to Iron Thimble only (scope 08 "Leads to"; Overstuffed
    // and Burst Seams shape Mangled, which only Loose Stuffing makes). The 22 upgrades scope 08 added on 2026-11-04 are
    // built since 2026-11-06 (Yarn Stash 7, Cookie Tin 3, Thread Head 5, Pin Cushion 2, Loom 5). The Effects lines are the
    // journal's EFFECTS, written for the player; the in-game line (Desc) carries no numbers (docs/STYLE.md).
    public static readonly Build[] Builds =
    {
        new Build
        {
            Id = Stash, Template = "Tower_Smackyhut", Display = "Yarn Stash", Tag = "Spoolstash", Tags = new[] { "Tower" },
            Desc = "Holds Spool's Thread, the Crafted draw from it", Costs = "",
            Lore = "Every scrap of Thread Spool and its Thread Heads bring home goes in here, and the Cookie Tins draw on it.",
            Stats = new[] { "Health=100+50", "Threadcap=50+25" },
            // Like Boutman's Bouthouse, several land on Spool itself (read from the player's Stash while it stands).
            Upgrades = new[]
            {
                U(Stash, "Pockets", 2, "Roomy Pockets", "Spool carries more", "Carry_Base=40;Pick_Base=2", "Yellowaura=30", "Darning3,Elbow3",
                    "Spool carries 40 Thread", "Spool picks Thread up within 2 tiles"),
                U(Stash, "Quick", 2, "Quick Patch", "Faster, cheaper PATCH", "Patchcost_Base=15;Patchcooldown_Base=2.5", "Yellowaura=25;Blueaura=10", "Darning3,Elbow3",
                    "PATCH costs 15 Thread", "PATCH is ready again after 2.5 seconds"),
                U(Stash, "Darning", 3, "Darning", "PATCH mends a whole patch", "Darning_Base=1.5", "Yellowaura=100", "Remnant4,Circle4",
                    "PATCH also mends everything of yours within 1.5 tiles of its target, as much as the target"),
                U(Stash, "Elbow", 3, "Elbow Patches", "Spool gets stuck in", "Herohealth_Multi=0.6;Herodamage_Multi=1", "Redaura=60;Yellowaura=60", "Remnant4,Circle4",
                    "+60% Spool's Health", "+100% Spool's Damage"),
                U(Stash, "Remnant", 4, "Remnant Bin", "Nothing goes to waste", "Droplife_Base=30;Remnant_Base=0.5", "Yellowaura=300", "Bee5",
                    "Thread lasts 30 seconds on the ground", "Thread that despawns comes to the Yarn Stash at 50%, rounded down"),
                U(Stash, "Circle", 4, "Mending Circle", "Everything mends between Tides", "Circle_Base=0.25", "Blueaura=150;Yellowaura=200", "Bee5",
                    "At the start of each Tide, every building of yours heals 25% of its maximum Health"),
                U(Stash, "Bee", 5, "Quilting Bee", "Everyone sews at once", "Beedamage_Base=0.5;Beetime_Base=15", "Yellowaura=2500;Redaura=1000", "",
                    "A BEE button, once a Tide: every Cookie Tin stitches up to its limit at once, for no Thread", "Crafted deal +50% damage for 15 seconds"),
            },
        },
        new Build
        {
            Id = Tin, Template = "Tower_Smackyhut", Display = "Repurposed Cookie Tin", Tag = "Spooltin", Tags = new[] { "Tower" },
            Desc = "Stitches Crafted from Thread, sends them where you point", Costs = "Redaura=50",
            Lore = "A cookie tin full of sewing supplies. Feed it Thread and it sends Crafted running to the spot you pick.",
            Stats = new[] { "Health=60+25", "Units=6", "Respawn=4", "Reach=8" },     // Units: the balance sweep 2026-11-05 (one wall)
            Upgrades = new[]
            {
                U(Tin, "Recycle", 2, "Reuse Reduce Recycle", "Fallen Crafted leave Thread behind", "Recycle_Base=0.5", "Redaura=30", "Discounted3,Design3,Tacking3",
                    "A killed Crafted drops half its cost in Thread"),
                U(Tin, "Woolen", 2, "Woolen Lining", "Sturdier Crafted", "Craftedhealth_Multi=0.5", "Redaura=30", "Discounted3,Design3,Tacking3",
                    "+50% Crafted Health"),
                U(Tin, "Discounted", 3, "Discounted Material", "Cheaper, faster Stitched", "Stitchedcost_Base=8;Stitchedspeed_Multi=0.3", "Redaura=90", "Surprise4,Cross4",
                    "Stitched cost 8 Thread", "+30% Stitched Speed"),
                U(Tin, "Design", 3, "New Design", "Stitches Darts that throw needles", "Dart_Base=1", "Redaura=100", "Surprise4,Cross4",
                    "Stitches Darts (15 Thread)"),
                U(Tin, "Tacking", 3, "Tacking Stitch", "Stitched hold the line", "Tacked_Base=1;Tackedhealth_Multi=1", "Redaura=110", "Surprise4,Cross4",
                    "Stitched hold their spot and block foes like a Snoble", "+100% Stitched Health", "This Tin makes no Darts"),
                U(Tin, "Surprise", 4, "One Last Surprise", "Crafted burst when they fall", "Burst_Times=3", "Redaura=400", "Army5",
                    "A falling Crafted hurts every foe within its range for 3x its damage"),
                U(Tin, "Cross", 4, "Cross Stitch", "Crafted hit harder", "Crafteddamage_Multi=1", "Redaura=350", "Army5",
                    "+100% Damage for this Tin's Crafted"),
                U(Tin, "Army", 5, "Patchwork Army", "A bigger, faster sewing circle", "Units_Add=3;Respawn_Multi=-0.5", "Redaura=2500;Yellowaura=1000", "",
                    "+3 Crafted alive at once", "Stitches one every 2 seconds"),
            }
        },
        new Build
        {
            Id = Head, Template = "Mob_Smackyminer", IsUnit = true, Display = "Thread Head", Tag = "Spoolhead", Tags = new[] { "Mob", "Mobfighter" },
            Desc = "Guards a spot, collects Thread, carries it home", Costs = "Redaura=40",
            Lore = "A big ball of Thread with somewhere to be. It gathers what falls around it and hauls it back to the Stash.",
            Stats = HeadStats,
            Upgrades = new[]
            {
                U(Head, "Delivery", 2, "Threaded Delivery", "Carries Thread home on its own", "Delivery_Base=1", "Redaura=30", "Bobbin3,Needle3",
                    "Full, it carries its Thread home and comes back"),
                U(Head, "Patchwork", 2, "Patchwork Job", "Mends nearby units with Thread", "Mend_Base=2;Mendrange_Base=2;Mendper_Base=5", "Redaura=30", "Bobbin3,Needle3",
                    "MEND: 2 health a second to your units within 2 tiles", "1 Thread per 5 health"),
                // Porter (Big Bobbin, High Thread Count) or buffer (Needle Point, Stitch in Time); Head of the Class caps both.
                U(Head, "Bobbin", 3, "Big Bobbin", "Carries more, reaches farther", "Carry_Add=20;Collect_Add=2", "Yellowaura=60", "Count4,Time4",
                    "Carries 40 Thread", "Collects Thread from 5 tiles around it"),
                U(Head, "Needle", 3, "Needle Point", "Points the Crafted at the weak spot", "Needle_Base=0.25;Needlerange_Base=3", "Redaura=80", "Count4,Time4",
                    "+25% Damage for your Crafted within 3 tiles"),
                U(Head, "Count", 4, "High Thread Count", "Foes near it drop more Thread", "Count_Base=1;Countrange_Base=3", "Yellowaura=250", "Class5",
                    "+1 Thread from each foe felled within 3 tiles"),
                U(Head, "Time", 4, "Stitch in Time", "Nearby Crafted shrug off hits", "Stitchtime_Base=0.3;Stitchrange_Base=3", "Redaura=300;Yellowaura=100", "Class5",
                    "Your Crafted within 3 tiles take 30% less damage"),
                U(Head, "Class", 5, "Head of the Class", "The best stitch in the house", "Classbuff_Base=0.5;Classguard_Base=0.45;Classrange_Base=4;Classhome_Base=1", "Redaura=2000;Yellowaura=1500", "",
                    "Its buffs reach 4 tiles: +50% Damage and 45% less damage taken for your Crafted", "Thread it picks up goes straight to the Yarn Stash"),
            }
        },
        new Build
        {
            Id = Cushion, Template = "Tower_Smackywall", Display = "Pin Cushion", Tag = "Spoolcushion", Tags = new[] { "Tower" },
            Desc = "Build it first: pricks melee attackers, spins Thread when hit", Costs = "Blueaura=20",
            Lore = "A wall of stuffing bristling with pins. Whatever hits it gets pricked, and every hit shakes a little Thread loose. Set one where the paths meet before anything else: the Crafted fight best beside it.",
            Stats = new[] { "Health=100+40", "Reflect=0.2", "Spin=25" },
            Upgrades = new[]
            {
                U(Cushion, "Sharper", 2, "Sharper Pins", "Pricks melee attackers harder", "Reflect_Add=0.2", "Blueaura=30", "Barrage3,Stuffing3",
                    "Reflects 40% of melee damage"),
                U(Cushion, "Unravel", 2, "Slow Unravel", "Takes half its damage later, spins more Thread", "Unravel_Base=4;Spin_Multi=-0.5", "Blueaura=30", "Barrage3,Stuffing3",
                    "Half of each hit now, half over 4 seconds", "Spins twice the Thread"),
                U(Cushion, "Barrage", 3, "Pin Barrage", "Pricks ranged attackers too", "Rangedreflect_Base=0.2", "Blueaura=100", "Thimble4",
                    "Reflects 20% of ranged damage"),
                U(Cushion, "Stuffing", 3, "Loose Stuffing", "Spills Mangled when hit", "Mangled_Per=40;Mangled_Max=3", "Blueaura=90", "Overstuffed4,Burst4,Thimble4",
                    "1 Mangled per 40 damage taken", "At most 3 alive"),
                U(Cushion, "Overstuffed", 4, "Overstuffed", "Mangled leap in to block", "Leap_Base=2", "Blueaura=400", "Voodoo5",
                    "Mangled have 3x Health", "They leap 2 tiles in and block like a Snoble"),
                U(Cushion, "Burst", 4, "Burst Seams", "Mangled burst on foes", "Seams_Damage=5;Seams_Radius=1.25", "Blueaura=400", "Voodoo5",
                    "A Mangled that reaches a foe bursts for 5 damage within 1.25 tiles"),
                U(Cushion, "Thimble", 4, "Iron Thimble", "Sturdier pins, harder to break", "Health_Multi=2;Thimble_Base=0.25", "Blueaura=300;Yellowaura=300", "Voodoo5",
                    "+200% Health", "Takes 25% less damage"),
                U(Cushion, "Voodoo", 5, "Voodoo Doll", "Prick it and everyone feels it", "Voodoo_Base=0.5;Voodoostun_Base=3;Voodoorange_Base=3;Voodooheal_Base=0.5", "Blueaura=2000;Yellowaura=1500", "",
                    "Once a Tide, the first time it falls below 50% Health: every foe within 3 tiles is stunned for 3 seconds", "It heals 50% of its maximum Health"),
            }
        },
        new Build
        {
            Id = Loom, Template = "Tower_Smackyhut", Display = "Loom", Tag = "Spoolloom", Tags = new[] { "Tower", "Gatherer" },
            Desc = "Mines aura, spins Thread", Costs = "Yellowaura=40",
            Lore = "It picks at the ore within reach and spins a little Thread on the side, for Spool or a Thread Head to collect.",
            Stats = new[] { "Health=40+20", "Damage=1", "Attackspeed=0.3", "Range=2.5", "Spinrate=6", "Hold=10" },     // Attackspeed: as low as Spool keeps The Hunt (the balance sweep 2026-11-05)
            Upgrades = new[]
            {
                U(Loom, "Direct", 2, "Direct Line", "Thread goes straight home", "Direct_Base=1", "Yellowaura=30", "Dyed3,Bobbin3",
                    "Spun Thread goes straight into the Yarn Stash"),
                U(Loom, "Seam", 2, "Seam Allowance", "Makes Seams that mine and guard", "Seam_Base=1;Seamrespawn_Base=10", "Yellowaura=30", "Dyed3,Bobbin3",
                    "One Seam per Loom, back 10 seconds after it falls"),
                U(Loom, "Dyed", 3, "Dyed in the Wool", "Pick the colour of its aura", "Dyed_Base=0.7", "Yellowaura=150", "Squad4,Snare4",
                    "A COLOUR button", "Your share of each payout from its strikes and its Seams' comes in the picked colour, at 70% of its total"),
                U(Loom, "Bobbin", 3, "Bobbin Winder", "Spins Thread twice as fast", "Spinrate_Multi=-0.5;Hold_Add=10", "Yellowaura=100", "Squad4,Snare4",
                    "Spins a Thread every 3 seconds", "Holds up to 20"),
                U(Loom, "Squad", 4, "Seam Squad", "More Seams to mine and guard", "Squad_Base=3;Seamrespawn_Base=10", "Yellowaura=350", "Golden5",
                    "3 Seams per Loom"),
                U(Loom, "Snare", 4, "Snare Line", "Seams hunt the wild aura", "Snare_Base=6;Seamrespawn_Base=10", "Yellowaura=300", "Golden5",
                    "Its Seams chase aura beasts within 6 tiles of the Loom"),
                U(Loom, "Golden", 5, "Golden Thread", "Thread spun from gold", "Golden_Base=1;Spinrate_Pmulti=-0.5", "Yellowaura=3000", "",
                    "Spool and Thread Heads can drop off Thread here, and it goes straight to the Yarn Stash", "Spins Thread twice as fast"),
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

    // Owner-only messages, shown by SpoolClient.
    public static readonly string[] WarnText =
    {
        "",
        "Not enough Thread in the Yarn Stash",
        "Nothing of yours to patch",
        "Too far away",
        "Patch is still threading",
        "Already in one piece",
        "Place your Yarn Stash first",
        "Yarn Stash is full",
        "The Crafted can't reach that spot",
        "Spot set",
        "No Yarn Stash to return to",
        "Mending on",
        "Mending off",
        "Spool can't carry more",
        "Your Yarn Stash fell, its Thread spilled",
        "Mending needs Thread: send it to collect some",
        "The Quilting Bee has sewn this Tide",
        "No Crafted out to unthread",
        "Everyone sews at once!",
    };
    public const int WarnShort = 1, WarnNoTarget = 2, WarnFar = 3, WarnCooldown = 4, WarnFull = 5, WarnNoStash = 6, WarnStashFull = 7,
        WarnUnreachable = 8, WarnSpot = 9, WarnNoHome = 10, WarnMendOn = 11, WarnMendOff = 12, WarnCarryFull = 13, WarnStashFell = 14,
        WarnMendEmpty = 15, WarnBeeUsed = 16, WarnNothingOut = 17, WarnBee = 18;
}
