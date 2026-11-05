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
    public const string ReturnCommand = "Command_Mob_Spoolhead_Return", ReturnLink = "Return";
    public const string MendCommand = "Command_Mob_Spoolhead_Mend", MendLink = "Mend";
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

    // ---------------------------------------------------------------- Crafted and other units

    public const string Stitched = "Mob_Spoolstitched", Dart = "Mob_Spooldart", Seam = "Mob_Spoolseam";
    public const string Mangled = "Mob_Spoolmangled", Stuffed = "Mob_Spoolmangled_Stuffed";

    public class Unit
    {
        public string Id, Display, Desc, Lore, Sheet;
        public int Thread, Cell = 16;
        public bool Ranged, Blocker, Sellable;
        public float Leash;
        public string[] Stats;
    }

    public static readonly Unit[] Units =
    {
        new Unit { Id = Stitched, Display = "Stitched", Sheet = "Spoolstitched-Sheet", Thread = 10, Leash = 2f, Sellable = true,
            Desc = "Short and stubby, single-target melee",
            Lore = "A short, stubby plush stitched in four seconds flat. It runs where it's sent and stays there.",
            Stats = new[] { "Health=20", "Speed=1.8", "Damage=2", "Range=1.5", "Attackspeed=0.8" } },
        new Unit { Id = Dart, Display = "Dart", Sheet = "Spooldart-Sheet", Thread = 15, Leash = 1f, Ranged = true, Sellable = true,
            Desc = "Thin and small, throws needles",
            Lore = "A thin little plush with a pocket full of needles. It keeps close to its spot and throws.",
            Stats = new[] { "Health=12", "Speed=1.6", "Damage=2.5", "Range=3.5", "Attackspeed=0.6", "Projspeed=14" } },
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

    public const float CraftedEngage = 3f;       // how far a Crafted at its spot looks for foes (inside its leash)
    public const float StitchedSpeedUp = 0.3f;

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

    // The sheet's levels 1-3 are the game's tiers 2-4, one upgrade per level; every choice of a level leads to every
    // choice of the next except Pin Barrage (both level-3 Cushion upgrades shape Mangled, which only Loose Stuffing
    // makes). The sheet's Stash upgrades, the Thread Head's and Loom's levels 2-4 and the level-4 capstones (scope 08 since
    // 2026-11-04) are not built yet. The Effects lines are the journal's EFFECTS, written for the player.
    public static readonly Build[] Builds =
    {
        new Build
        {
            Id = Stash, Template = "Tower_Smackyhut", Display = "Yarn Stash", Tag = "Spoolstash", Tags = new[] { "Tower" },
            Desc = "Holds Spool's Thread, the Crafted draw from it", Costs = "",
            Lore = "Every scrap of Thread Spool and its Thread Heads bring home goes in here, and the Cookie Tins draw on it.",
            Stats = new[] { "Health=100+50", "Threadcap=50+25" },
            Upgrades = new Upg[0],
        },
        new Build
        {
            Id = Tin, Template = "Tower_Smackyhut", Display = "Repurposed Cookie Tin", Tag = "Spooltin", Tags = new[] { "Tower" },
            Desc = "Stitches Crafted from Thread, sends them where you point", Costs = "Redaura=50",
            Lore = "A cookie tin full of sewing supplies. Feed it Thread and it sends Crafted running to the spot you pick.",
            Stats = new[] { "Health=60+25", "Units=4", "Respawn=4", "Reach=8" },
            Upgrades = new[]
            {
                U(Tin, "Recycle", 2, "Reuse Reduce Recycle", "Fallen Crafted leave Thread behind", "Recycle_Base=0.5", "Redaura=30", "Discounted3,Design3",
                    "A killed Crafted drops half its cost in Thread"),
                U(Tin, "Woolen", 2, "Woolen Lining", "Sturdier Crafted", "Craftedhealth_Multi=0.5", "Redaura=30", "Discounted3,Design3",
                    "+50% Crafted Health"),
                U(Tin, "Discounted", 3, "Discounted Material", "Cheaper, faster Stitched", "Stitchedcost_Base=8;Stitchedspeed_Multi=0.3", "Redaura=90", "Surprise4",
                    "Stitched cost 8 Thread", "+30% Stitched Speed"),
                U(Tin, "Design", 3, "New Design", "Stitches Darts that throw needles", "Dart_Base=1", "Redaura=100", "Surprise4",
                    "Stitches Darts (15 Thread)"),
                U(Tin, "Surprise", 4, "One Last Surprise", "Crafted burst when they fall", "Burst_Times=3", "Redaura=400", "",
                    "A falling Crafted hurts every foe within its range for 3x its damage"),
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
                U(Head, "Delivery", 2, "Threaded Delivery", "Carries Thread home on its own", "Delivery_Base=1", "Redaura=30", "",
                    "Full, it carries its Thread home and comes back"),
                U(Head, "Patchwork", 2, "Patchwork Job", "Mends nearby units with Thread", "Mend_Base=2;Mendrange_Base=2;Mendper_Base=5", "Redaura=30", "",
                    "MEND: 2 Health a second to your units within 2 tiles", "1 Thread per 5 Health"),
            }
        },
        new Build
        {
            Id = Cushion, Template = "Tower_Smackywall", Display = "Pin Cushion", Tag = "Spoolcushion", Tags = new[] { "Tower" },
            Desc = "Sturdy, pricks melee attackers, spins Thread when hit", Costs = "Blueaura=20",
            Lore = "A wall of stuffing bristling with pins. Whatever hits it gets pricked, and every hit shakes a little Thread loose.",
            Stats = new[] { "Health=100+40", "Reflect=0.2", "Spin=25" },
            Upgrades = new[]
            {
                U(Cushion, "Sharper", 2, "Sharper Pins", "Pricks melee attackers harder", "Reflect_Add=0.2", "Blueaura=30", "Barrage3,Stuffing3",
                    "Reflects 40% of melee damage"),
                U(Cushion, "Unravel", 2, "Slow Unravel", "Takes half its damage later, spins more Thread", "Unravel_Base=4;Spin_Multi=-0.5", "Blueaura=30", "Barrage3,Stuffing3",
                    "Half of each hit now, half over 4 seconds", "Spins twice the Thread"),
                U(Cushion, "Barrage", 3, "Pin Barrage", "Pricks ranged attackers too", "Rangedreflect_Base=0.2", "Blueaura=100", "",
                    "Reflects 20% of ranged damage"),
                U(Cushion, "Stuffing", 3, "Loose Stuffing", "Spills Mangled when hit", "Mangled_Per=40;Mangled_Max=3", "Blueaura=90", "Overstuffed4,Burst4",
                    "1 Mangled per 40 damage taken", "At most 3 alive"),
                U(Cushion, "Overstuffed", 4, "Overstuffed", "Mangled leap in to block", "Leap_Base=2", "Blueaura=400", "",
                    "Mangled have 3x Health", "They leap 2 tiles in and block like a Snoble"),
                U(Cushion, "Burst", 4, "Burst Seams", "Mangled burst on foes", "Seams_Damage=5;Seams_Radius=1.25", "Blueaura=400", "",
                    "A Mangled that reaches a foe bursts for 5 damage within 1.25 tiles"),
            }
        },
        new Build
        {
            Id = Loom, Template = "Tower_Smackyhut", Display = "Loom", Tag = "Spoolloom", Tags = new[] { "Tower", "Gatherer" },
            Desc = "Mines aura, spins Thread", Costs = "Yellowaura=40",
            Lore = "It picks at the ore within reach and spins a little Thread on the side, for Spool or a Thread Head to collect.",
            Stats = new[] { "Health=40+20", "Damage=1", "Attackspeed=0.4", "Range=2.5", "Spinrate=6", "Hold=10" },
            Upgrades = new[]
            {
                U(Loom, "Direct", 2, "Direct Line", "Thread goes straight home", "Direct_Base=1", "Yellowaura=30", "",
                    "Spun Thread goes straight into the Yarn Stash"),
                U(Loom, "Seam", 2, "Seam Allowance", "Makes Seams that mine and guard", "Seam_Base=1;Seamrespawn_Base=10", "Yellowaura=30", "",
                    "One Seam per Loom, back 10 seconds after it falls"),
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
    };
    public const int WarnShort = 1, WarnNoTarget = 2, WarnFar = 3, WarnCooldown = 4, WarnFull = 5, WarnNoStash = 6, WarnStashFull = 7,
        WarnUnreachable = 8, WarnSpot = 9, WarnNoHome = 10, WarnMendOn = 11, WarnMendOff = 12, WarnCarryFull = 13, WarnStashFell = 14,
        WarnMendEmpty = 15;
}
