// Every name, text and tuning number for Mummer in one place. Structure and numbers follow the shipped vessels (see
// ModSource/Sprigg/Research/VESSEL_SURVEY.md and ModSource/Mummer/DESIGN.md): a melee vessel with reach like Smacky,
// four buildables with curated upgrade trees on the base game's cost curve, a vessel ability on Buster's Dash keys,
// and the special rule shown as a tooltip upgrade.
public static class MummerData
{
    public const string Version = "1.0.0";
    public const bool Diagnostics = true;

    public const string TemplateHero = "Hero_Smacky";
    // Other vessels' asset groups Mummer borrows from: Buster's walk-through trap block for the Curtain, Sno Sno's
    // mob-blocker block for the Aegis Understudy. The game never unloads a group once loaded.
    public static readonly string[] ExtraGroups = { "Hero_Buster", "Hero_Snosno" };

    public const string Hero = "Hero_Mummer";
    public const string HeroEntity = "Hero_Mummer_Entity";
    public const string HeroTooltip = "Hero_Mummer_Tooltip_Upgradeproperties";

    public const string Loom = "Tower_Mummerloom";
    public const string Stage = "Tower_Mummerstage";
    public const string Cross = "Tower_Mummercross";
    public const string Curtain = "Trap_Mummercurtain";
    public static readonly string[] Towers = { Loom, Stage, Cross, Curtain };

    public const string HookCommand = "Command_Hero_Mummer_Hook";
    public const string HookLink = "Hook";
    public const string CostumeCommand = "Command_Tower_Mummerstage_Costume";
    public const string CostumeLink = "Costume";
    public const string PuppetBolt = "Mob_Understudy_Bolt";

    // Debuff/one-shot effects the game itself plays on every player (registered under the "Vfx" label).
    public const string VfxMarked = "Debuff_Mummermarked", VfxHeld = "Debuff_Mummerheld", VfxTangled = "Debuff_Mummertangled";
    public const string VfxStun = "Debuff_Stun", VfxSlow = "Debuff_Slow";

    // ---------------------------------------------------------------- sound levels (matched to the game's world SFX)

    public const string ReferenceSound = "Upgrade";
    public static readonly Dictionary<string, float> SoundVolume = new Dictionary<string, float>
    {
        { "Mummer_Peekaboo", 0.03f }, { "Mummer_Hookthrow", 0.04f }, { "Mummer_Reel", 0.035f }, { "Mummer_Bell", 0.03f },
        { "Mummer_Scrap", 0.012f }, { "Mummer_Stitch", 0.03f }, { "Mummer_Puppetpop", 0.02f }, { "Mummer_Puppetdown", 0.025f },
        { "Mummer_Grab", 0.035f }, { "Mummer_Curtain", 0.03f }, { "Mummer_Finalact", 0.08f }, { "Mummer_Loom", 0.012f },
        { "Mummer_Unravel", 0.04f }, { "Mummer_Costume", 0.05f }, { "Mummer_Landing", 0.1f }, { "Mummer_Death", 0.03f },
        { "Mummer_Footstep", 0.008f }, { "Mummer_Bolt", 0.02f },
    };

    // ---------------------------------------------------------------- vessel texts (the game uses English text as the key)

    public const string Displayname = "Mummer";
    public const string Displaysub = "Strings Attached";
    public const string Displayskill = "Advanced";
    public const string HeroDesc = "Regular, single-target melee with reach";
    public const string TooltipName = "The Show Must Go On";
    public const string TooltipDesc = "Melee with reach, foes felled near a Loom leave Scraps of Thread, Stages stitch Understudies with Thread in the costumes of foes you've scrapped, half your Thread unravels each Tide, can HOOK foes";
    public const string Lore = "Mummer sewed the costume itself, so that someone would want to play with it. Nobody has seen what wears it.";
    public const string Origins = "Origins - Mummer mod";
    public const string HookName = "HOOK";
    public const string HookDesc = "Cast a thread at a foe: it is reeled in and Marked. Marked foes leave extra Thread wherever they fall";
    public const string HookSelectTitle = "HOOK A FOE";
    public const string CostumeName = "COSTUME";
    public const string CostumeDesc = "Change the costume this Stage stitches";

    // ---------------------------------------------------------------- vessel stats (the melee band: Smacky is 50/3/4/1.5/0.5)

    public static readonly string[] HeroStats =
    {
        "Health=45", "Speed=2.75", "Damage=3.5", "Range=2", "Attackspeed=0.6", "Buildrange=2.5", "Sellrate=0.5",
    };

    // ---------------------------------------------------------------- Thread

    public const int ThreadStart = 4;
    public const int ThreadMax = 99;
    public const int ScrapSmall = 1, ScrapBig = 3, ScrapHuge = 5, ScrapGolden = 10;
    public const float BigHealth = 40f, HugeHealth = 500f;     // a foe's base health decides how many Scraps it leaves
    public const int MarkBonus = 2;                             // extra Thread for a Marked foe, wherever it falls

    // ---------------------------------------------------------------- HOOK

    public const float HookRange = 5f;
    public const float HookCooldown = 9f;
    public const int HookReel = 2;               // tiles
    public const float HookReelTime = 0.55f;     // the reel's sped-up walk, before the root
    public const float HookRoot = 0.75f;
    public const float MarkDuration = 8f;
    public const float BossHealth = 600f;        // foes this tough are Marked but not reeled

    // ---------------------------------------------------------------- costumes and Understudies

    public enum Costume { Plain, Ballista, Aegis }

    public class Puppet
    {
        public Costume Costume;
        public string Id, Display, Desc, Lore;
        public int Thread;
        public bool Ranged;
        public string[] Stats;
    }

    public static readonly Puppet[] Puppets =
    {
        new Puppet { Costume = Costume.Plain, Id = "Mob_Understudy", Display = "Understudy", Thread = 2, Desc = "Felt doll, single-target melee",
            Lore = "A felt doll on a string, stitched in a hurry. It knows its lines and nothing else.",
            Stats = new[] { "Health=24", "Speed=1.6", "Damage=3", "Range=1.5", "Attackspeed=0.8" } },
        new Puppet { Costume = Costume.Ballista, Id = "Mob_Understudy_Ballista", Display = "Ballista Understudy", Thread = 4, Ranged = true, Desc = "In costume, single-target ranged",
            Lore = "Dressed as a Ballista, crossbow and all. The bolts are felt, but it fires them like it means it.",
            Stats = new[] { "Health=16", "Speed=1.3", "Damage=3.5", "Range=4", "Attackspeed=0.5", "Projspeed=14" } },
        new Puppet { Costume = Costume.Aegis, Id = "Mob_Understudy_Aegis", Display = "Aegis Understudy", Thread = 5, Desc = "In costume, sturdy, holds the line",
            Lore = "Dressed as an Aegis, behind a quilted shield. Leave it be for a moment and it patches itself up.",
            Stats = new[] { "Health=70", "Speed=1", "Damage=5", "Range=1.5", "Attackspeed=0.4" } },
    };

    public static Puppet PuppetFor(Costume c) { foreach (Puppet p in Puppets) if (p.Costume == c) return p; return Puppets[0]; }
    public static Puppet PuppetFor(string being) { foreach (Puppet p in Puppets) if (p.Id == being) return p; return null; }

    public const float PuppetEngage = 3f;        // how far an Understudy steps out to meet a foe
    public const float AegisRegen = 3f, AegisRegenDelay = 3f;

    // Enemy families, by the enemy's own AI (names kept as a fallback): which costume a Scrap unlocks.
    public static readonly string[] RangedFoes = { "Ballista", "Onager", "Excalibur", "Bulwark" };
    public static readonly string[] HeavyFoes = { "Aegis", "Maul", "Halberd" };

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

    // Trees are curated like the shipped ones: two tier-2 roots, branches that cross over, one branch that ends at
    // tier 4, and a single tier-5 capstone. Costs sit on the base game's tier curve in each building's aura colour.
    public static readonly Build[] Builds =
    {
        new Build
        {
            Id = Loom, Template = "Tower_Smackyhut", Display = "Loom", Tag = "Mummerloom", Tags = new[] { "Tower", "Gatherer" },
            Desc = "Spins Scraps from foes felled nearby, mines ore slowly", Costs = "Yellowaura=40",
            Lore = "Every foe that falls nearby leaves a Scrap, and the Loom spins it into Thread. Between Tides it picks at the ore.",
            Stats = new[] { "Health=25+25", "Damage=1", "Attackspeed=0.4", "Range=2.5", "Scraprange=3.5" },
            Upgrades = new[]
            {
                U(Loom, "Shuttle", 2, "Fast Shuttle", "Mines ore faster", "Attackspeed_Add=0.3", "Yellowaura=30", "Orethread3,Silk3"),
                U(Loom, "Weft", 2, "Long Weft", "Gathers Scraps and ore from farther away", "Range_Add=1.5;Scraprange_Add=2", "Yellowaura=25;Blueaura=10", "Silk3"),
                U(Loom, "Silk", 3, "Silk", "Your Thread no longer unravels", "Unravel_Base=0", "Yellowaura=110", "Patchwork4,Tapestry4"),
                U(Loom, "Orethread", 3, "Ore Thread", "Every third strike on ore spins a Thread", "Orethread_Base=1", "Yellowaura=125", "Tapestry4"),
                U(Loom, "Patchwork", 4, "Patchwork", "Sews a free Understudy at a Stage nearby each Tide", "Units_Add=1", "Yellowaura=260;Redaura=60", ""),
                U(Loom, "Tapestry", 4, "Tapestry", "Scraps felled nearby are worth double", "Scrap_Multi=1", "Yellowaura=300", "Golden5"),
                U(Loom, "Golden", 5, "Golden Thread", "Big foes and bosses leave Golden Scraps worth 10 Thread", "Golden_Base=10", "Yellowaura=3000", ""),
            }
        },
        new Build
        {
            Id = Stage, Template = "Tower_Smackyhut", Display = "Puppet Stage", Tag = "Mummerstage", Tags = new[] { "Tower", "Towerfighter" },
            Desc = "Stitches Understudies with Thread, in costumes of foes you've scrapped", Costs = "Redaura=50",
            Lore = "Pay it Thread and it stitches Understudies, each one leashed to the stage by its strings. A COSTUME button picks the part.",
            Stats = new[] { "Health=40+40", "Units=2", "Respawn=6", "Leash=4" },
            Upgrades = new[]
            {
                U(Stage, "Strings", 2, "Long Strings", "Understudies roam farther and walk faster", "Leash_Add=3;Puppetspeed_Multi=0.25", "Redaura=30", "Double3,Second3"),
                U(Stage, "Quick", 2, "Quick Change", "Stitches twice as fast, new costumes re-dress the cast", "Respawn_Multi=-0.5", "Redaura=35", "Second3"),
                U(Stage, "Double", 3, "Double Bill", "One more Understudy on stage", "Units_Add=1", "Redaura=90;Yellowaura=30", "Method4,Finale4"),
                U(Stage, "Second", 3, "Second Cast", "Understudies cost half as much Thread", "Thread_Multi=-0.5", "Redaura=80;Yellowaura=40", "Finale4"),
                U(Stage, "Method", 4, "Method Acting", "Understudies have much more health and damage", "Puppethealth_Multi=0.6;Puppetdamage_Multi=0.6", "Redaura=320", "Ovation5"),
                U(Stage, "Finale", 4, "Grand Finale", "Understudies burst when they fall, hurting foes nearby", "Burst_Base=8", "Redaura=280;Blueaura=60", ""),
                U(Stage, "Ovation", 5, "Standing Ovation", "Two more Understudies, and they heal while on stage", "Units_Add=2;Regen_Base=2", "Redaura=2800", ""),
            }
        },
        new Build
        {
            Id = Cross, Template = "Tower_Smackyarcher", Display = "Marionette Cross", Tag = "Mummercross", Tags = new[] { "Tower", "Towerfighter" },
            Desc = "Holds a foe on strings, it flails at its own Tide", Costs = "Blueaura=30;Redaura=15",
            Lore = "It lifts a foe off its feet and works its limbs. Whatever it was carrying, it now swings at its friends.",
            Stats = new[] { "Health=30+30", "Damage=2", "Attackspeed=0.14", "Range=4", "Duration=4" },
            Upgrades = new[]
            {
                U(Cross, "Quickgrab", 2, "Quick Grab", "Grabs more often", "Attackspeed_Add=0.06", "Blueaura=30", "Double3,Cut3"),
                U(Cross, "Longarm", 2, "Long Arm", "Reaches farther", "Range_Add=2", "Blueaura=25;Redaura=10", "Double3"),
                U(Cross, "Double", 3, "Double Cross", "Holds two foes at once", "Count_Add=1", "Blueaura=110;Redaura=40", "Ventriloquist4,Encore4"),
                U(Cross, "Cut", 3, "Cut Strings", "Dropped foes are stunned", "Stun_Base=2", "Blueaura=90", "Encore4"),
                U(Cross, "Ventriloquist", 4, "Ventriloquist", "Foes near a held one turn on it", "Taunt_Base=2", "Blueaura=350", "Master5"),
                U(Cross, "Encore", 4, "Encore", "A foe that falls while held is stitched into a free Understudy", "Units_Add=1", "Blueaura=300;Redaura=100", ""),
                U(Cross, "Master", 5, "Puppet Master", "Holds foes until they fall, bosses for 4 seconds", "Master_Base=1", "Blueaura=3500", ""),
            }
        },
        new Build
        {
            Id = Curtain, Template = "Trap_Busterpad", Display = "Curtain", Tag = "Mummercurtain", Tags = new[] { "Trap" },
            Desc = "Walk-through, tangles foes: slowed and too stage-struck to attack", Costs = "Blueaura=20",
            Lore = "Foes can walk right through it. They come out the other side tangled in velvet, and for a moment they forget why they came.",
            Stats = new[] { "Slow=0.35", "Duration=2", "Fright=1", "Range=0.75" },
            Upgrades = new[]
            {
                U(Curtain, "Velvet", 2, "Velvet", "Tangles harder", "Slow_Add=0.2", "Blueaura=20", "Heavy3,Trapdoor3"),
                U(Curtain, "Spotlight", 2, "Spotlight", "Marks foes that pass through", "Mark_Base=6", "Blueaura=25;Yellowaura=10", "Trapdoor3"),
                U(Curtain, "Heavy", 3, "Heavy Drape", "Tangles last longer", "Duration_Add=1.5", "Blueaura=70", "Smoke4,Iron4"),
                U(Curtain, "Trapdoor", 3, "Trapdoor", "Understudies heal near this Curtain", "Heal_Base=3", "Blueaura=60;Redaura=20", "Iron4"),
                U(Curtain, "Smoke", 4, "Smoke and Mirrors", "Tangled foes attack each other", "Confuse_Base=1", "Blueaura=320;Redaura=60", "Finalact5"),
                U(Curtain, "Iron", 4, "Iron Curtain", "Foes are held in place as they pass", "Root_Base=1", "Blueaura=280", ""),
                U(Curtain, "Finalact", 5, "Final Act", "Once each Tide the Curtain drops, stunning every foe nearby", "Stun_Base=3;Radius_Base=3", "Blueaura=3000", ""),
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

    // Owner-only messages, shown by MummerClient.
    public static readonly string[] WarnText =
    {
        "",
        "Hook is recoiling",
        "No foe in reach",
        "That foe is too heavy to reel in",
        "Not enough Thread to stitch",
        "Ballista costume unlocked",
        "Aegis costume unlocked",
        "Now stitching: Understudy (2 Thread)",
        "Now stitching: Ballista Understudy (4 Thread)",
        "Now stitching: Aegis Understudy (5 Thread)",
        "Scrap a ranged foe near a Loom to unlock more costumes",
        "Half your Thread unravelled",
        "Too far away",
    };
    public const int WarnRecoil = 1, WarnNoFoe = 2, WarnHeavy = 3, WarnNoThread = 4, WarnUnlockBallista = 5, WarnUnlockAegis = 6,
        WarnStitchPlain = 7, WarnLocked = 10, WarnUnravel = 11, WarnFar = 12;

    // EFFECTS lines in the journal: one label per parameter, null hides it.
    public static string ParamLabel(string key)
    {
        string stat = key.Contains("_") ? key.Substring(0, key.LastIndexOf('_')) : key;
        switch (stat)
        {
            case "Attackspeed": return "Attack Speed";
            case "Range": return "Range";
            case "Scraprange": return "Scrap Range";
            case "Unravel": return "Unravelling";
            case "Orethread": return "Thread per 3 Strikes";
            case "Units": return "Understudies";
            case "Scrap": return "Scraps";
            case "Golden": return "Golden Scrap";
            case "Leash": return "Leash";
            case "Puppetspeed": return "Understudy Speed";
            case "Respawn": return "Stitch Time";
            case "Thread": return "Thread Cost";
            case "Puppethealth": return "Understudy Health";
            case "Puppetdamage": return "Understudy Damage";
            case "Burst": return "Burst Damage";
            case "Regen": return "Healing";
            case "Count": return "Extra Foes";
            case "Stun": return "Stun";
            case "Taunt": return "Taunt Range";
            case "Duration": return "Duration";
            case "Slow": return "Slow";
            case "Mark": return "Mark Time";
            case "Heal": return "Healing";
            case "Confuse": return null;
            case "Root": return "Hold Time";
            case "Radius": return "Radius";
            default: return null;
        }
    }
}
