// Every name, text and tuning number for Argo in one place. Structure and numbers follow the shipped vessels
// (TDS_Vessel_Creator/reference/VESSEL_SURVEY.md). 1.x was a melee vessel like Smacky and Fie with a hero ability like
// Buster's Dash, four buildables with curated upgrade trees whose costs follow the base game's tier curve, a unit tower like the
// Mining Hut and Knight's Keep, and the special rule shown as a tooltip upgrade.
//
// Balance notes against the concept (ModSource/Argo/Concept): the concept's numbers were placeholders. Argo keeps the
// trade of speed for health but stays inside the roster's band (speed 2.25, not 1.2); the Scatter Cannon's volley was
// about 3.5x an Archer Tower's damage and is now ~1.6x against a crowd and below it against one target; Tremors were
// 3-5 tiles of 25% slow on an economy building and are now 2-3 tiles of 20%; Sonar is a little smaller and slower.
//
// 2.0 (Rogue Waves, DESIGN.md): five buildables (Deep Water is new), the Harpoon Gun's damage or crowd-control paths,
// an aimed Tremor, a limit-1 Miner's Base, no "loaded guns", and no SONAR for now (its code stays, switched off).
public static class ArgoData
{
    // The Rogue Waves pack version, "-dev" for the dev mod (docs/PACK.md section 4); 1.4.0 was the standalone release.
    public const string Version = "0.1.0-dev";
    public const string Part = "Argo";
    public const bool SonarEnabled = false;   // the user, 2026-10-08: no SONAR in this design for now
    public const bool Diagnostics = true;

    public const string TemplateHero = "Hero_Smacky";
    // Other vessels' groups Argo's data clones from: Buster's Speed Pad is Deep Water's walk-through template (the kit template's
    // Curtain pattern; without it the traps fall back to Smacky's Perma-Wall block).
    public static readonly string[] ExtraGroups = { "Hero_Buster" };
    public const string TrapFallback = "Trap_Smackywall_Perma";
    public const string Hero = "Hero_Argo";
    public const string HeroEntity = "Hero_Argo_Entity";

    public const string Scatter = "Tower_Argoscatter";
    public const string Harpoon = "Tower_Argoharpoon";
    public const string Bell = "Tower_Argobell";
    public const string Drill = "Tower_Argodrill";
    public const string Water = "Trap_Argowater";
    public const string Pool = "Trap_Argowater_Pool";   // a FLOOD pool: Deep Water's frame and water, no pump
    // The five build options, in button order. The fifth is Argo's own Command_Buildoption5 (ArgoContent).
    public static readonly string[] Towers = { Scatter, Harpoon, Water, Drill, Bell };
    public const string BaseOption = "Buildoption5";     // the Miner's Base button, gone while one stands

    public const string HeroTooltip = "Hero_Argo_Tooltip_Upgradeproperties";
    public const string SonarCommand = "Command_Hero_Argo_Sonar";
    public const string SonarLink = "Sonar";
    public const string BuildOption5 = "Command_Buildoption5";
    public const string FloodCommand = "Command_Trap_Argowater_Flood";
    public const string FloodLink = "Buildoption1";      // FLOOD is Deep Water's build option: the game's placement
    public const string FloodKey = "Flood";              // its key in ArgoContent.Commands
    public const string AimCommand = "Command_Tower_Argodrill_Aim";
    public const string AimLink = "Aim";

    public const string Pellet = "Tower_Argoscatter_Pellet";
    public const string HarpoonShot = "Tower_Argoharpoon_Harpoon";
    public const string Lance = "Tower_Argoharpoon_Lance";

    // ---------------------------------------------------------------- the crew (Nauts)

    public class Crew
    {
        public string Name, Being, Command, Link, Display, Light;
        public Color Color;
    }

    public static readonly Crew[] Nauts =
    {
        new Crew { Name = "Port", Being = "Mob_Argonaut_Port", Command = "Command_Tower_Argobell_Port", Link = "Crew_Port", Display = "PORT", Light = "red light", Color = new Color(0.86f, 0.31f, 0.33f, 1f) },
        new Crew { Name = "Starboard", Being = "Mob_Argonaut_Starboard", Command = "Command_Tower_Argobell_Starboard", Link = "Crew_Starboard", Display = "STARBOARD", Light = "green light", Color = new Color(0.32f, 0.72f, 0.42f, 1f) },
        new Crew { Name = "Bosun", Being = "Mob_Argonaut_Bosun", Command = "Command_Tower_Argobell_Bosun", Link = "Crew_Bosun", Display = "BOSUN", Light = "yellow light", Color = new Color(0.95f, 0.78f, 0.32f, 1f) },
    };

    public static Crew CrewFor(string beingOrLink)
    {
        foreach (Crew c in Nauts)
            if (c.Being == beingOrLink || c.Link == beingOrLink || c.Name == beingOrLink)
                return c;
        return null;
    }

    public static readonly string[] NautStats = { "Health=20+5", "Speed=1.6", "Range=1.5" };
    public const float NautRespawn = 8f;
    public const float TuneReach = 1.75f;       // a Naut tunes a tower from this far (flat distance)
    public const float TuneAttackspeed = 0.2f;  // +20% attack speed; Full Crew adds another 0.2 (Tuneboost_Base)
    public const float TuneHeal = 0.5f;         // health a second on any tuned tower; Overhaul adds Repair_Base
    public const float GiveUpAfter = 12f;       // unused: ArgoNautBrain.Travel sends a Naut home after 6 s without progress
    // 1.4 Old Salts: a Naut tunes each kind of Argo tower its own way (teammates' towers get the attack speed only).
    public const float SaltsPellets = 2f, SaltsPierce = 2f, SaltsReel = 1f, SaltsSlow = 0.1f;

    // ---------------------------------------------------------------- sound levels

    // Argo's sounds play through the same mixer group as this game sound (General_Main).

    // Target loudness (clip rms x playback volume) per clip, matched to the game's own sounds for the same job
    // (measured in game: tower shots ~0.004, hits ~0.0015, Upgrade ~0.013, vessel landings ~0.015).
    // ArgoAssets.Play measures each clip once and sets the volume to hit its target.
    public static readonly Dictionary<string, float> SoundTarget = new Dictionary<string, float>
    {
        { "Argo_Landing", 0.015f }, { "Argo_Footstep", 0.0015f }, { "Argo_Swing", 0.003f }, { "Argo_Clang", 0.002f },
        { "Argo_Sonar", 0.011f }, { "Argo_Scatter", 0.0045f }, { "Argo_Harpoon", 0.004f }, { "Argo_Lance", 0.008f },
        { "Argo_Drill", 0.0014f }, { "Argo_Fathom", 0.009f }, { "Argo_Quake", 0.011f }, { "Argo_Tune", 0.0014f },
        { "Argo_Naut", 0.002f }, { "Argo_Dive", 0.004f }, { "Argo_Death", 0.012f }, { "Argo_Upgrade", 0.006f },
        { "Argo_Build", 0.006f }, { "Argo_Winch", 0.0012f }, { "Argo_Splash", 0.004f }, { "Argo_Pin", 0.003f }, { "Argo_Gurgle", 0.004f },
    };

    // ---------------------------------------------------------------- vessel texts (the game uses English text as the key)

    public const string Displayname = "Argo";
    public const string Displaysub = "Deep Sea Diver";
    public const string Displayskill = "Intermediate";
    public const string HeroDesc = "Sturdy, slow melee, hits two at once";
    public const string TooltipName = "Fathoms";
    public const string TooltipDesc = "Swings hit two enemies, no walls of its own, Scatter Cannons punish slowed and pinned foes, Drill Rigs sink a Fathom each Tide and their aimed Tremors widen, the crew tunes towers, never traps";
    public const string LogTitle = "Argo";
    public const string LogDesc = "Argo went down to find the bottom of the Basin and never quite came back up. Heavy boots, a heavier wrench, and a crew that fixes anything that stands still long enough.";
    public const string SonarName = "SONAR";
    public const string SonarDesc = "Ping every foe around Argo: for a few seconds they take more damage from everything, and Drill Rigs inside the ping quake, holding foes in their Tremors. The button greys out while it recharges";
    public const string CrewSelectTitle = "SEND {0}";
    public const string FloodName = "FLOOD";
    public const string FloodDesc = "Add a pool next to this Deep Water or one of its pools";
    public const string AimName = "AIM";
    public const string AimDesc = "Pick where this Drill Rig's Tremor shakes the ground";

    public static readonly Color Color = new Color(0.27f, 0.56f, 0.76f, 1f);
    public static readonly Color Brass = new Color(0.84f, 0.65f, 0.3f, 1f);

    // ---------------------------------------------------------------- vessel stats (the melee band: Smacky 50/3/4/1.5/0.5)

    // 1.3: Damage 5 -> 4 (Smacky's, the highest shipped): every swing also cleaves a second foe, and the design rules
    // ask for lower damage when a vessel has another damage source. Health stays 65 (the bulky Nautilus build asked for).
    public static readonly string[] HeroStats =
    {
        "Health=65", "Speed=2.25", "Damage=4", "Range=1.75", "Attackspeed=0.4", "Buildrange=2", "Sellrate=0.5",
    };

    public const float CleaveReach = 1.25f;     // the second enemy must stand this close to the first
    public const float CleaveMulti = 0.6f;

    // SONAR (vessel ability, on Buster's Dash keys).
    public const float SonarRadius = 6f;
    public const float SonarCooldown = 15f;
    public const float PingDuration = 5f;
    public const float PingBonus = 0.2f;        // pinged enemies take 20% more damage from everyone
    public const float QuakeRoot = 1f;

    // ---------------------------------------------------------------- Fathoms (Drill Rig)

    // A Drill Rig sinks a Fathom at the start of every Tide it stands through, down to Fathom 5 (7 with Deep Core).
    // Every Fathom below the first adds the four lines below, so its ore per second grows about 1.9x, 3x, 4.2x, 5.6x
    // (8.8x at 7) and its Tremors widen and bite harder. 1.1 sank only to Fathom 3 and gave +50% ore and +0.5 tiles a
    // Fathom (2x ore at most): playtesters felt it barely grew from Tide to Tide. The base game's miners mine about 0.25
    // ore damage a second each; a drill does 0.125 at Fathom 1, 0.7 at Fathom 5.
    public const int FathomMax = 5;
    public const int FathomDeepest = 7;         // Deep Core's depth; the drill has a look for every Fathom down to here
    public const float FathomYield = 0.75f;     // +75% ore per bite
    public const float FathomSpeed = 0.1f;      // +10% bites a second
    public const float FathomTremor = 0.4f;     // +0.4 tiles of Tremor
    public const float FathomSlow = 0.04f;      // +4% Tremor slow
    public const float SlowCap = 0.6f;          // Tremors never slow more than this
    public const float TremorTick = 0.5f;
    public const float OreReach = 2.1f;         // ore within this flat distance can be drilled (2 tiles straight out, 1 diagonally)
    public const float BedrockEvery = 1f;       // 2.0 Bedrock: a tick of damage on each pinned foe in the Tremor

    // ---------------------------------------------------------------- 2.0: debuffs, pins, Deep Water

    // Pinned foes are held in place (Anchor Line, Kelp Bed, Trench, Mantle Tap's quake); beside a building Anchor Line stuns instead (held
    // and unable to attack). A foe pinned in Deep Water is held twice as long.
    public const float StunReach = 1.25f;       // a building this close to the pinned foe makes it a stun
    public const float WaterPinMulti = 2f;
    public const float PoolReach = 0.55f;       // a foe this close (flat) to a pool's tile centre is in the water
    public const float WaterTick = 0.25f;
    public const float CatchMemory = 3f;        // Kelp Bed catches a foe again only after it was out of the water this long
    public const float FloodLinkReach = 1.1f;   // pools join a network when their tiles touch (no diagonals)
    public const float WaterSlow = 0.3f;        // pools whose pump was sold keep Deep Water's base slow
    public const float WaterSlowCap = 0.65f;    // Deep Water never slows more than this: Abyss's pump at level 4 (0.3 + 3 x 0.05
                                                // + Undercurrent 0.1 + Abyss 0.1), so its +10% is whole (scope 09; Dimitri 2026-11-06)
    public const float SaltBrine = 0.5f;        // Salt Sting's brine on pinned foes when Brine wasn't taken (doubled)
    public const float AimSlack = 0.5f;         // AIM accepts a tile this far past the Range (tile centres)

    // ---------------------------------------------------------------- Directions (kit 1.7)

    // Claude's four in docs/scope/11 (By: Claude), one upgrade per rank, built on what Argo already has.
    public class Direction { public string Name, Display, Desc; public float Bonus; }
    // Deep Current: Deep Water reaches this much farther (PoolReach). Default, docs/scope/11.
    public static readonly Direction[] Deepcurrents =
    {
        new Direction { Name = "Deepcurrent", Display = "Deep Current", Bonus = 0.15f,
            Desc = "Deep Water reaches farther around each pool" },
        new Direction { Name = "Deepcurrent2", Display = "Deep Current II", Bonus = 0.3f,
            Desc = "Deep Water reaches farther still around each pool" },
        new Direction { Name = "Deepcurrent3", Display = "Deep Current III", Bonus = 0.5f,
            Desc = "Deep Water reaches much farther around each pool" },
    };
    // Heavy Salvo: a held Multi on the Harpoon Gun's Damage. Default, docs/scope/11.
    public static readonly Direction[] Heavysalvos =
    {
        new Direction { Name = "Heavysalvo", Display = "Heavy Salvo", Bonus = 0.1f,
            Desc = "Harpoon Guns deal more damage" },
        new Direction { Name = "Heavysalvo2", Display = "Heavy Salvo II", Bonus = 0.2f,
            Desc = "Harpoon Guns deal even more damage" },
        new Direction { Name = "Heavysalvo3", Display = "Heavy Salvo III", Bonus = 0.35f,
            Desc = "Harpoon Guns deal much more damage" },
    };
    // Chum Bucket: a Slowed or Pinned foe takes this much more from every hit by its holder's buildings, on top of the
    // Scatter Cannon's own bonus (ArgoHooks). Docs/scope/11.
    public static readonly Direction[] Chumbuckets =
    {
        new Direction { Name = "Chumbucket", Display = "Chum Bucket", Bonus = 0.08f,
            Desc = "Slowed and pinned foes take more damage from your towers" },
        new Direction { Name = "Chumbucket2", Display = "Chum Bucket II", Bonus = 0.15f,
            Desc = "They take more still" },
        new Direction { Name = "Chumbucket3", Display = "Chum Bucket III", Bonus = 0.25f,
            Desc = "They take much more" },
    };
    // Pressure Gauge: a Drill Rig its holder builds starts this many Fathoms down (Bonus = Fathoms below the first).
    // Docs/scope/11.
    public static readonly Direction[] Pressuregauges =
    {
        new Direction { Name = "Pressuregauge", Display = "Pressure Gauge", Bonus = 1f,
            Desc = "Your Drill Rigs start a Fathom deep" },
        new Direction { Name = "Pressuregauge2", Display = "Pressure Gauge II", Bonus = 2f,
            Desc = "They start two Fathoms deep" },
        new Direction { Name = "Pressuregauge3", Display = "Pressure Gauge III", Bonus = 3f,
            Desc = "They start three Fathoms deep" },
    };

    // ---------------------------------------------------------------- the harpoon's reel (1.4)

    // 2.0: only the reel path reels (Reel, Twin Line, Undertow). The first foe each harpoon hits walks itself Reel_Tiles
    // toward the gun on its own Move command (the kit template's HOOK reel), never closer than ReelMinDist. Foes this tough (max
    // health) are hit but not moved.
    public const float ReelTiles = 1.5f, ReelTime = 0.4f, ReelMinDist = 1.2f, BossHealth = 600f;
    public const float DoubleDelay = 0.15f;     // Double Barrel: each stored volley follows the one before this long after

    // ---------------------------------------------------------------- buildables

    public class Upg
    {
        public string Owner, Key, Display, Desc, Params, Costs, Projectile;
        public int Tier;
        public string[] Next;
        public string[] Effects;   // the journal's EFFECTS lines, in the player's words
        public Upg Fx(params string[] lines) { Effects = lines; return this; }
        public string Id { get { return Owner + "_" + Key + Tier + "_Upgradeobject"; } }
        public string Model { get { return Owner + "_" + Key + Tier; } }
    }

    public class Build
    {
        public string Id, Template, Display, Desc, Family, Costs, Projectile, Journal;
        public string[] Tags;     // role tags besides "Tower" and the family tag
        public string[] Stats;    // "Stat=value" or "Stat=value+perLevel"
        public Upg[] Upgrades;
        public bool Turret;       // the model has a "Turret" part the clients swing toward each shot
        public float Eye;         // height of the range centre above the tower's position (its "Detect" point)
        public bool Trap;         // walk-through: no "Tower" tag, never tuned by Nauts
        public bool Hidden;       // no build button and no journal page (FLOOD's pools)
        public string Model;      // the library model, when it isn't named after the Id
        public string ModelName { get { return string.IsNullOrEmpty(Model) ? Id : Model; } }
    }

    // costs: "Redaura=30;Blueaura=15"; next: keys of the next tier's options ("Grapeshot3,Barnacle3"), empty for a final upgrade.
    static Upg U(string owner, string key, int tier, string display, string desc, string pars, string costs, string next, string proj = null)
    {
        return new Upg
        {
            Owner = owner, Key = key, Tier = tier, Display = display, Desc = desc, Params = pars, Costs = costs, Projectile = proj,
            Next = string.IsNullOrEmpty(next) ? new string[0] : next.Split(','),
        };
    }

    // Trees are curated like the shipped ones: two tier-2 roots, branches that cross over, one branch that ends at tier 4,
    // and a single tier-5 capstone. Costs sit on the base game's curve (t2 15-75, t3 45-450, t4 100-1750, t5 1500-5000)
    // in each building's colour: red offence, blue defence and support, yellow economy.
    // Descriptions say what the upgrade does in a few plain words (the upgrade panel shows only them); the journal's
    // EFFECTS lines (Fx) carry the exact numbers. 1.2 rewrote every one after playtesters found several vague or off
    // ("bites the ore" for mining).
    public static readonly Build[] Builds =
    {
        new Build
        {
            Id = Scatter, Template = "Tower_Smackybomber", Display = "Scatter Cannon", Desc = "Short range, sprays a cone of pellets, hits slowed foes harder and pinned foes much harder", Family = "Argoscatter",
            Tags = new[] { "Towerfighter" }, Costs = "Redaura=30", Projectile = Pellet, Turret = true, Eye = 0.12f,
            // 2.0: the scope's "bonus damage to enemies affected by debuffs" (ArgoStatus). Review 2026-11-04 (scope 09): split
            // in two, Debuffbonus on a slowed foe and Pinbonus on a pinned one; a pinned foe takes only the larger.
            Stats = new[] { "Health=40+25", "Damage=1.25+0.25", "Attackspeed=0.6", "Range=4.5", "Projspeed=16", "Projcount=6", "Spread=50", "Pierce=1", "Debuffbonus=0.5", "Pinbonus=1" },
            Journal = "A bell-mouthed blunderbuss that sprays lead in a cone. Shreds a crowd up close, wastes most of a volley on one big target. Every pellet hits 50% harder on a slowed foe, and twice as hard on a pinned one.",
            Upgrades = new[]
            {
                U(Scatter, "Widebore", 2, "Wide Bore", "A wider cone with two more pellets", "Projcount_Add=2;Spread_Add=10", "Redaura=30", "Grapeshot3,Barnacle3")
                    .Fx("+2 pellets", "Cone +10 degrees"),
                U(Scatter, "Double", 2, "Double Barrel", "Loads extra volleys while idle, and fires them all at once", "Double_Base=3;Double_Every=2", "Redaura=30", "Barnacle3,Grapeshot3")
                    .Fx("Stores a volley every 2s while no foe is in range, up to 3"),
                U(Scatter, "Grapeshot", 3, "Grapeshot", "Three more pellets in every volley", "Projcount_Add=3", "Redaura=90", "Pressure4,Ironclad4")
                    .Fx("+3 pellets"),
                U(Scatter, "Barnacle", 3, "Barnacle Shot", "Pellets stick and slow what they hit", "Slow_Base=0.25;Duration_Base=1.5", "Redaura=60;Blueaura=40", "Ironclad4")
                    .Fx("Slows by 25% for 1.5s"),
                U(Scatter, "Pressure", 4, "Pressure Rounds", "Harder pellets that punch through one more enemy", "Pierce_Add=1;Damage_Add=0.5", "Redaura=350", "Broadside5")
                    .Fx("Pierce +1", "Pellet damage +0.5"),
                // 2.0: the scope's "increases health greatly", in place of 1.4.0's Undertow. Healtharmor is the game's
                // armour: each hit, whatever its Damage, takes its Damagemulti (1 by default) from it and none from
                // health (kit docs/01); healing refills it, as on Sno Sno's Brain Freeze.
                U(Scatter, "Ironclad", 4, "Ironclad", "Health greatly increased", "Health_Multi=1.5;Healtharmor_Base=2", "Redaura=250;Blueaura=150", "")
                    .Fx("Health x2.5", "Armour: the next 2 hits take 1 each"),
                U(Scatter, "Broadside", 5, "Broadside", "Every few volleys, a full ring of pellets all around", "Broadside_Every=4;Broadside_Count=16;Damage_Add=0.5", "Redaura=3000", "")
                    .Fx("Ring of 16 every 4th volley", "Pellet damage +0.5"),
            }
        },
        new Build
        {
            Id = Harpoon, Template = "Tower_Smackyarcher", Display = "Harpoon Gun", Desc = "Long range, harpoons pierce a line; its upgrades pick damage or crowd control", Family = "Argoharpoon",
            Tags = new[] { "Towerfighter" }, Costs = "Redaura=35", Projectile = HarpoonShot, Turret = true, Eye = 0.3f,
            Stats = new[] { "Health=40+25", "Damage=5+2", "Attackspeed=0.35", "Range=6.5", "Projspeed=18", "Pierce=3" },
            Journal = "A whaling gun on a tripod. Slow to reload, but every harpoon runs straight through the first few enemies in a lane. Its upgrades choose the job: Long Line, Skewer, Whaler and the Leviathan Lance for damage; Tether, Anchor Line, Reel and Undertow to slow, pin and reel foes in. Bosses are never moved.",
            Upgrades = new[]
            {
                U(Harpoon, "Longline", 2, "Long Line", "Reaches farther, and harpoons fly faster", "Range_Add=2;Projspeed_Add=4", "Redaura=25", "Skewer3,Anchor3")
                    .Fx("Range +2 tiles", "Harpoon speed +4"),
                U(Harpoon, "Tether", 2, "Tether", "Harpooned foes are slowed", "Tetherslow_Base=0.3;Tetherslow_Duration=2", "Redaura=30", "Anchor3,Reel3")
                    .Fx("Slows every foe hit by 30% for 2s"),
                U(Harpoon, "Skewer", 3, "Skewer", "Harpoons run through three more enemies", "Pierce_Add=3", "Redaura=100", "Whaler4")
                    .Fx("Pierce +3"),
                U(Harpoon, "Anchor", 3, "Anchor Line", "Pins the foe it hits, stunned against a building", "Root_Base=1;Pinstun_Base=1", "Redaura=60;Blueaura=50", "")
                    .Fx("Pins the first foe hit for 1s", "Beside a building: stunned for 1s instead", "Twice as long in Deep Water"),
                U(Harpoon, "Reel", 3, "Reel", "Reels the first foe hit towards the gun", "Reel_Tiles=1.5", "Redaura=60;Blueaura=50", "Twinline4")
                    .Fx("Reels the first foe hit 1.5 tiles closer", "Place it beside or up the lane: a gun by your core pulls foes towards the core"),
                U(Harpoon, "Whaler", 4, "Whaler", "Hits harder, and up to twice as hard against tough enemies", "Whaler_Base=200;Damage_Add=4", "Redaura=450", "Leviathan5")
                    .Fx("Damage +4", "Up to x2 on big health pools"),
                U(Harpoon, "Twinline", 4, "Twin Line", "A second harpoon follows, reeling its own foe", "Twin_Base=0.3", "Redaura=350", "Undertow5")
                    .Fx("2 harpoons per shot, each reels"),
                U(Harpoon, "Leviathan", 5, "Leviathan Lance", "Every few shots, a huge lance that runs through everything in its path", "Lance_Every=5;Lance_Multi=2.5;Lance_Range=2", "Redaura=3500", "")
                    .Fx("Every 5th shot: x2.5 damage", "Pierces all, twice the range"),
                U(Harpoon, "Undertow", 5, "Undertow", "Every harpoon reels the foes around its target", "Undertow_Radius=1.5;Undertow_Tiles=1.5", "Redaura=1750;Blueaura=1750", "")
                    .Fx("Foes within 1.5 tiles of the reeled one are reeled 1.5 tiles too"),
            }
        },
        new Build
        {
            // Buster's Speed Pad is the template: a walk-through block (the kit template's Curtain proved it, falling back to
            // Perma-Wall's when Buster's group isn't loaded).
            Id = Water, Template = "Trap_Busterpad", Display = "Deep Water", Desc = "Walk-through pool that slows foes inside. FLOOD adds a pool next to it", Family = "Argowater",
            Tags = new[] { "Trap" }, Costs = "Blueaura=20", Trap = true,
            Stats = new[] { "Health=30+15", "Slow=0.3+0.05" },
            Journal = "A flooded pit with a brass pump. Foes wade through it slowed. FLOOD adds a pool on a tile next to it or next to one of its pools; connected pools act as one. A foe pinned in the water is held twice as long, and a Drill Rig's Tremor that touches a pool shakes every pool connected to it.",
            Upgrades = new[]
            {
                U(Water, "Undercurrent", 2, "Undercurrent", "Stronger slow", "Slow_Add=0.1", "Blueaura=20", "Kelp3,Salt3")
                    .Fx("Slow +10%"),
                U(Water, "Brine", 2, "Brine", "Foes inside take damage over time", "Brine_Damage=0.5", "Blueaura=25", "Salt3")
                    .Fx("0.5 damage a second"),
                U(Water, "Kelp", 3, "Kelp Bed", "Foes are pinned for a moment on entering", "Catch_Duration=0.5", "Blueaura=80", "Abyss4,Crushing4")
                    .Fx("Pins a foe 0.5s as it wades in, so 1s in the water"),
                U(Water, "Salt", 3, "Salt Sting", "Brine stings pinned foes harder", "Brinepinned_Multi=1", "Blueaura=70;Redaura=30", "Crushing4")
                    .Fx("Pinned foes take double brine (1 a second without Brine)"),
                U(Water, "Abyss", 4, "Abyss", "FLOOD fills two tiles at once, and the water runs deeper", "Flood_Add=1;Slow_Add=0.1", "Blueaura=260", "")
                    .Fx("FLOOD adds a second pool for free", "Slow +10%"),
                U(Water, "Crushing", 4, "Crushing Depth", "Foes inside take more damage", "Damagetaken_Multi=0.15", "Blueaura=300", "Trench5")
                    .Fx("+15% damage taken from everything"),
                U(Water, "Trench", 5, "Trench", "Spreads by itself each Tide, and drags foes under again and again", "Spread_Per_Tide=2;Dragunder_Every=3;Dragunder_Duration=1", "Blueaura=3000", "")
                    .Fx("Two free pools each Tide", "Every 3s a foe inside is pinned 1s, so 2s in the water"),
            }
        },
        new Build
        {
            Id = Drill, Template = "Tower_Smackyarcher", Display = "Drill Rig", Desc = "Mines ore nearby and sinks deeper each Tide; AIM places its Tremor, which slows foes", Family = "Argodrill",
            Tags = new[] { "Gatherer" }, Costs = "Yellowaura=35",
            // Damage is how hard it mines ore (Sno Box 0.4 every 5 s). 2.0: Range is how far AIM can place the Tremor
            // (shown while placing and when selected); Tremor is its radius (2 at Fathom 1, +0.4 a Fathom). The drill
            // mines ore up to 2 tiles away (OreReach), with a visible tether. Mantle Tap multiplies the base strike rate (Pmulti); a
            // strike breaks at most one share of ore (kit docs/01 §9), which Damage reaches at Fathom 3.
            Stats = new[] { "Health=40+25", "Damage=0.5", "Attackspeed=0.25", "Range=5", "Tremor=2", "Slow=0.2" },
            Journal = "A derrick that mines ore up to 2 tiles away. AIM places its Tremor anywhere within 5 tiles: every foe in it is slowed. Every Tide it stands through it sinks a Fathom, down to Fathom 5: each one mines faster and harder, and widens and strengthens the Tremor. A Tremor that touches Deep Water shakes every pool connected to it.",
            Upgrades = new[]
            {
                U(Drill, "Paydirt", 2, "Pay Dirt", "Foes that fall in its Tremor are dug up for ore", "Paydirt_Base=2", "Yellowaura=30", "Deepcore3,Bedrock3")
                    .Fx("2 mining strikes per foe felled in its Tremor"),
                U(Drill, "Seismic", 2, "Seismic", "The Tremor slows harder and reaches farther", "Slow_Add=0.1;Tremor_Add=0.5", "Yellowaura=35", "Bedrock3")
                    .Fx("Tremor slow +10%", "Tremor +0.5 tiles"),
                U(Drill, "Deepcore", 3, "Deep Core", "Keeps sinking two Fathoms past the usual bottom", "Fathommax_Base=7", "Yellowaura=120", "Gusher4,Faultline4")
                    .Fx("Deepest Fathom 7"),
                U(Drill, "Bedrock", 3, "Bedrock", "Pinned foes in its Tremor take damage over time", "Bedrock_Damage=1.5", "Yellowaura=80;Redaura=30", "Faultline4")
                    .Fx("1.5 damage a second to each pinned foe"),
                U(Drill, "Gusher", 4, "Gusher", "Once it is deep enough, a gush of ore bursts up at each Tide, bigger the deeper it is", "Gusher_Base=4", "Yellowaura=275", "Mantle5")
                    .Fx("4 mining strikes per Fathom at once, each Tide", "From Fathom 3 down"),
                U(Drill, "Faultline", 4, "Fault Line", "Foes reeled through its Tremor take a burst of damage", "Fault_Damage=12", "Yellowaura=300", "")
                    .Fx("12 damage to a foe reeled into or across the Tremor"),
                U(Drill, "Mantle", 5, "Mantle Tap", "Mines much faster, and every few seconds a quake pins every foe in its Tremor", "Attackspeed_Pmulti=0.5;Mantle_Base=2;Mantle_Every=20", "Yellowaura=3000", "")
                    .Fx("Strikes per second x1.5", "Every 20s a quake pins foes for 2s"),
            }
        },
        new Build
        {
            Id = Bell, Template = "Tower_Smackyhut", Display = "Miner's Base", Desc = "One per player. Houses Nauts: press PORT or STARBOARD, then click a tower to tune it", Family = "Argobell",
            Tags = new string[0], Costs = "Blueaura=20;Yellowaura=15",   // Review 2026-11-04 (scope 09): was Blue 35
            Stats = new[] { "Health=50+25", "Units=2", "Respawn=8" },
            Journal = "Home of the Nauts, one per player like Boutman's house. Pick PORT or STARBOARD, then any tower, a teammate's too: the Naut walks over and tunes it for faster attacks. Nauts tune towers, never traps.",
            // 1.3: the tree is priced in the Base's own Blue (support, like its tuning, repair and armour), with Yellow
            // only as a smaller second colour from tier 3.
            Upgrades = new[]
            {
                U(Bell, "Flippers", 2, "Flippers", "Nauts get to their towers much faster", "Nautspeed_Multi=0.5", "Blueaura=30", "Overhaul3,Calibrate3")
                    .Fx("Naut speed +50%"),
                U(Bell, "Salts", 2, "Old Salts", "Nauts tune each kind of Argo tower its own way", "Salts_Base=1", "Blueaura=30", "Overhaul3")
                    .Fx("Scatter Cannon: +2 pellets", "Harpoon Gun: +2 pierce, reels 1 tile farther", "Drill Rig: Tremor slow +10%"),
                U(Bell, "Overhaul", 3, "Overhaul", "Nauts patch up the towers they tune", "Repair_Base=2", "Blueaura=90;Yellowaura=30", "Thirdmate4,Bulkhead4")
                    .Fx("Repairs 2.5 health a second"),
                U(Bell, "Calibrate", 3, "Calibrate", "Tuned towers reach a tile farther", "Tunerange_Base=1", "Blueaura=80;Yellowaura=30", "Thirdmate4")
                    .Fx("Tuned towers: range +1 tile"),
                U(Bell, "Thirdmate", 4, "Third Mate", "Hires Bosun, a third Naut (the BOSUN button)", "Units_Add=1", "Blueaura=200;Yellowaura=100", "Fullcrew5")
                    .Fx("+1 Naut"),
                U(Bell, "Bulkhead", 4, "Bulkhead", "Tuned towers take less damage", "Bulkhead_Base=0.25", "Blueaura=250;Yellowaura=100", "")
                    .Fx("Tuned towers: -25% damage taken"),
                U(Bell, "Fullcrew", 5, "Full Crew", "Tuning is twice as strong, and Nauts dive straight to their tower", "Tuneboost_Base=0.2;Divebell_Base=1", "Blueaura=2500;Yellowaura=500", "")
                    .Fx("Tuning: +40% attack speed", "Nauts dive instead of walking"),
            }
        },
        new Build
        {
            // FLOOD's pool: Deep Water without its pump. It has no upgrades of its own; the pump it is connected to
            // decides what the water does (ArgoWater).
            Id = Pool, Template = "Trap_Busterpad", Display = "Deep Water Pool", Desc = "A pool FLOOD added; acts with the Deep Water it is connected to", Family = "Argowater",
            Tags = new[] { "Trap" }, Costs = "Blueaura=15", Trap = true, Hidden = true, Model = "Argo_Pool",
            Stats = new[] { "Health=30+15" },
            Journal = "",
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

    // Projectiles: { id, template, model }. Pellets and harpoons fly straight (Projectile_Ai_Simple, like the Heavy Bow).
    public static readonly string[][] Projectiles =
    {
        new[] { Pellet, "Tower_Smackyarcher_Heavyarrow", "Argo_Pellet" },
        new[] { HarpoonShot, "Tower_Smackyarcher_Heavyarrow", "Argo_Harpoon" },
        new[] { Lance, "Tower_Smackyarcher_Heavyarrow", "Argo_Lance" },
    };

    // The units the journal lists with Argo's buildings.
    public const string NautJournal = "Small divers from the Miner's Base. Sent to a tower, a Naut tunes it: faster attacks and a slow repair. Knocked out, it swims home and is back in 8 seconds.";
}
