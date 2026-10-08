// Every name, text and tuning number for Argo in one place. Structure and numbers follow the shipped vessels
// (ModSource/Sprigg/Research/VESSEL_SURVEY.md): a melee vessel like Smacky and Fie with a hero ability like Buster's
// Dash, four buildables with curated upgrade trees whose costs follow the base game's tier curve, a unit tower like the
// Mining Hut and Knight's Keep, and the special rule shown as a tooltip upgrade.
//
// Balance notes against the concept (ModSource/Argo/Concept): the concept's numbers were placeholders. Argo keeps the
// trade of speed for health but stays inside the roster's band (speed 2.25, not 1.2); the Scatter Cannon's volley was
// about 3.5x an Archer Tower's damage and is now ~1.6x against a crowd and below it against one target; Tremors were
// 3-5 tiles of 25% slow on an economy building and are now 2-3 tiles of 20%; Sonar is a little smaller and slower.
public static class ArgoData
{
    public const string Version = "1.4.0";
    public const bool Diagnostics = true;

    public const string TemplateHero = "Hero_Smacky";
    public const string Hero = "Hero_Argo";
    public const string HeroEntity = "Hero_Argo_Entity";

    public const string Scatter = "Tower_Argoscatter";
    public const string Harpoon = "Tower_Argoharpoon";
    public const string Bell = "Tower_Argobell";
    public const string Drill = "Tower_Argodrill";
    public static readonly string[] Towers = { Scatter, Harpoon, Bell, Drill };

    public const string HeroTooltip = "Hero_Argo_Tooltip_Upgradeproperties";
    public const string SonarCommand = "Command_Hero_Argo_Sonar";
    public const string SonarLink = "Sonar";

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
    public const float GiveUpAfter = 12f;       // a Naut that can't get to its tower comes home
    // 1.4 Old Salts: a Naut tunes each kind of Argo tower its own way (teammates' towers get the attack speed only).
    public const float SaltsPellets = 2f, SaltsPierce = 2f, SaltsReel = 1f, SaltsSlow = 0.1f;

    // ---------------------------------------------------------------- sound levels

    // Argo's sounds play through the same mixer group as this game sound (General_Main).
    public const string ReferenceSound = "Upgrade";

    // Target loudness (clip rms x playback volume) per clip, matched to the game's own sounds for the same job
    // (measured in game for Sprigg: tower shots ~0.004, hits ~0.0015, Upgrade ~0.013, vessel landings ~0.015).
    // ArgoAssets.Play measures each clip once and sets the volume to hit its target.
    public static readonly Dictionary<string, float> SoundTarget = new Dictionary<string, float>
    {
        { "Argo_Landing", 0.015f }, { "Argo_Footstep", 0.0015f }, { "Argo_Swing", 0.003f }, { "Argo_Clang", 0.002f },
        { "Argo_Sonar", 0.011f }, { "Argo_Scatter", 0.0045f }, { "Argo_Harpoon", 0.004f }, { "Argo_Lance", 0.008f },
        { "Argo_Drill", 0.0014f }, { "Argo_Fathom", 0.009f }, { "Argo_Quake", 0.011f }, { "Argo_Tune", 0.0014f },
        { "Argo_Naut", 0.002f }, { "Argo_Dive", 0.004f }, { "Argo_Death", 0.012f }, { "Argo_Upgrade", 0.006f },
        { "Argo_Build", 0.006f }, { "Argo_Winch", 0.0012f },
    };

    // ---------------------------------------------------------------- vessel texts (the game uses English text as the key)

    public const string Displayname = "Argo";
    public const string Displaysub = "Fathoms Below";
    public const string Displayskill = "Intermediate";
    public const string HeroDesc = "Sturdy, slow melee, hits two at once";
    public const string TooltipName = "Fathoms";
    public const string TooltipDesc = "Swings hit two enemies, Deep Drills sink a Fathom each Tide: every Fathom mines faster, widens their Tremors and loads the guns inside them, Nauts tune any tower, teammates' too";
    public const string LogTitle = "Argo";
    public const string LogDesc = "Argo went down to find the bottom of the Basin and never quite came back up. Heavy boots, a heavier wrench, and a crew that fixes anything that stands still long enough.";
    public const string SonarName = "SONAR";
    public const string SonarDesc = "Ping every foe around Argo: for a few seconds they take more damage from everything, and Deep Drills inside the ping quake, holding foes in their Tremors. The button greys out while it recharges";
    public const string CrewSelectTitle = "SEND {0}";

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

    // ---------------------------------------------------------------- Fathoms (Deep Drill)

    // A Deep Drill sinks a Fathom at the start of every Tide it stands through, down to Fathom 5 (7 with Deep Core).
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
    public const float AftershockEvery = 2f;
    public const float OreReach = 2.1f;         // ore within this flat distance can be drilled (the next tile, diagonals too)

    // 1.4 Pressure: Argo's guns inside a Deep Drill's Tremors gain per Fathom below the first (the deepest Drill counts).
    public const float PressurePellets = 1f, PressurePierce = 1f;

    // ---------------------------------------------------------------- the harpoon's reel (1.4)

    // The first foe each harpoon hits walks itself ReelTiles toward the tower on its own Move command (Mummer's HOOK
    // reel), never closer than ReelMinDist. Foes this tough (max health) are hit but not moved.
    public const float ReelTiles = 1.5f, ReelTime = 0.4f, ReelMinDist = 1.2f, BossHealth = 600f;
    public const float DoubleDelay = 0.15f;     // Double Barrel: the stored volley follows this long after the first

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
            Id = Scatter, Template = "Tower_Smackybomber", Display = "Scatter Cannon", Desc = "Short range, sprays a cone of pellets", Family = "Argoscatter",
            Tags = new[] { "Towerfighter" }, Costs = "Redaura=30", Projectile = Pellet, Turret = true, Eye = 0.12f,
            Stats = new[] { "Health=40+25", "Damage=1.25+0.25", "Attackspeed=0.6", "Range=4.5", "Projspeed=16", "Projcount=6", "Spread=50", "Pierce=1" },
            Journal = "A bell-mouthed blunderbuss that sprays lead in a cone. Shreds a crowd up close, wastes most of a volley on one big target.",
            Upgrades = new[]
            {
                U(Scatter, "Widebore", 2, "Wide Bore", "A wider cone with two more pellets", "Projcount_Add=2;Spread_Add=25", "Redaura=30", "Grapeshot3,Barnacle3")
                    .Fx("+2 pellets", "Cone +25 degrees"),
                U(Scatter, "Double", 2, "Double Barrel", "Loads a second volley while idle, and fires both at once", "Double_Base=1", "Redaura=30", "Barnacle3,Grapeshot3")
                    .Fx("Stores 1 volley while no foe is in range"),
                U(Scatter, "Grapeshot", 3, "Grapeshot", "Three more pellets in every volley", "Projcount_Add=3", "Redaura=90", "Pressure4,Undertow4")
                    .Fx("+3 pellets"),
                U(Scatter, "Barnacle", 3, "Barnacle Shot", "Pellets stick and slow what they hit", "Slow_Base=0.25;Duration_Base=1.5", "Redaura=60;Blueaura=40", "Undertow4")
                    .Fx("Slows by 25% for 1.5s"),
                U(Scatter, "Pressure", 4, "Pressure Rounds", "Harder pellets that punch through one more enemy", "Pierce_Add=1;Damage_Add=0.5", "Redaura=350", "Broadside5")
                    .Fx("Pierce +1", "Pellet damage +0.5"),
                U(Scatter, "Undertow", 4, "Undertow", "Fires faster, and every pellet stops its enemy for a moment", "Attackspeed_Multi=0.2;Stagger_Base=0.35", "Redaura=250;Blueaura=150", "")
                    .Fx("Fires 20% faster", "Each hit holds for 0.35s"),
                U(Scatter, "Broadside", 5, "Broadside", "Every few volleys, a full ring of pellets all around", "Broadside_Every=4;Broadside_Count=16;Damage_Add=0.5", "Redaura=3000", "")
                    .Fx("Ring of 16 every 4th volley", "Pellet damage +0.5"),
            }
        },
        new Build
        {
            Id = Harpoon, Template = "Tower_Smackyarcher", Display = "Harpoon Tower", Desc = "Long range, harpoons pierce a line and reel the first foe in", Family = "Argoharpoon",
            Tags = new[] { "Towerfighter" }, Costs = "Redaura=35", Projectile = HarpoonShot, Turret = true, Eye = 0.3f,
            // 1.3: Damage 9+3 -> 5+2 and Range 8 -> 6.5, the top of the shipped tower bands (Damage 0.3-5, Range 3-6.5);
            // the slow, piercing shot stays (Attackspeed 0.35, Pierce 3, Projspeed 18).
            Stats = new[] { "Health=40+25", "Damage=5+2", "Attackspeed=0.35", "Range=6.5", "Projspeed=18", "Pierce=3" },
            Journal = "A whaling gun on a tripod. Slow to reload, but every harpoon runs straight through the first few enemies in a lane, and reels the first one 1.5 tiles closer. Bosses are not moved.",
            Upgrades = new[]
            {
                U(Harpoon, "Longline", 2, "Long Line", "Reaches farther, and harpoons fly faster", "Range_Add=2;Projspeed_Add=4", "Redaura=25", "Skewer3,Anchor3")
                    .Fx("Range +2 tiles", "Harpoon speed +4"),
                U(Harpoon, "Barbed", 2, "Barbed", "Harpooned enemies keep bleeding after the hit", "Bleedticks_Base=4;Bleed_Multi=0.2", "Redaura=30", "Anchor3")
                    .Fx("Bleed: 20% damage a second for 4s"),
                U(Harpoon, "Skewer", 3, "Skewer", "Harpoons run through three more enemies", "Pierce_Add=3", "Redaura=100", "Whaler4,Twinline4")
                    .Fx("Pierce +3"),
                U(Harpoon, "Anchor", 3, "Anchor Line", "The foe each harpoon reels in is pinned where it lands", "Root_Base=1", "Redaura=60;Blueaura=50", "Twinline4")
                    .Fx("Holds for 1s after the reel"),
                U(Harpoon, "Whaler", 4, "Whaler", "Hits harder, and up to twice as hard against tough enemies", "Whaler_Base=200;Damage_Add=4", "Redaura=450", "Leviathan5")
                    .Fx("Damage +4", "Up to x2 on big health pools"),
                U(Harpoon, "Twinline", 4, "Twin Line", "A second harpoon follows right after each shot", "Twin_Base=0.3", "Redaura=350", "")
                    .Fx("2 harpoons per shot"),
                U(Harpoon, "Leviathan", 5, "Leviathan Lance", "Every few shots, a huge lance that runs through everything in its path", "Lance_Every=5;Lance_Multi=2.5;Lance_Range=2", "Redaura=3500", "")
                    .Fx("Every 5th shot: x2.5 damage", "Pierces all, twice the range"),
            }
        },
        new Build
        {
            Id = Bell, Template = "Tower_Smackyhut", Display = "Diving Bell", Desc = "Houses Nauts: press PORT or STARBOARD, then click a tower to tune it", Family = "Argobell",
            Tags = new string[0], Costs = "Blueaura=35",
            Stats = new[] { "Health=50+25", "Units=2", "Respawn=8" },
            Journal = "Home of the Nauts. Pick PORT or STARBOARD, then any tower, a teammate's too: the Naut walks over and tunes it for faster attacks.",
            // 1.3: the tree is priced in the Bell's own Blue (support, like its tuning, repair and armour), with Yellow
            // only as a smaller second colour from tier 3; 1.2 priced five of the seven upgrades mostly in Yellow. Every
            // upgrade keeps its total.
            Upgrades = new[]
            {
                U(Bell, "Flippers", 2, "Flippers", "Nauts get to their towers much faster", "Nautspeed_Multi=0.5", "Blueaura=30", "Overhaul3,Calibrate3")
                    .Fx("Naut speed +50%"),
                U(Bell, "Salts", 2, "Old Salts", "Nauts tune each kind of Argo tower its own way", "Salts_Base=1", "Blueaura=30", "Overhaul3")
                    .Fx("Scatter Cannon: +2 pellets", "Harpoon Tower: +2 pierce, reels 1 tile farther", "Deep Drill: Tremor slow +10%"),
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
            Id = Drill, Template = "Tower_Smackyarcher", Display = "Deep Drill", Desc = "Mines ore nearby, its Tremors slow foes and load the guns in range, sinks deeper each Tide", Family = "Argodrill",
            Tags = new[] { "Gatherer" }, Costs = "Yellowaura=35",
            // Damage is how hard it mines ore (Sno Box 0.4 every 5 s). Range is the Tremor radius (2 at Fathom 1, +0.4 a
            // Fathom), shown while placing and when selected; the drill mines ore up to 2 tiles away (OreReach), with a
            // visible tether. Mantle Tap multiplies the base rate (Pmulti), so it keeps its full value at every Fathom (as
            // Multi it was added to the Fathom bonus and shrank with depth).
            Stats = new[] { "Health=40+25", "Damage=0.5", "Attackspeed=0.25", "Range=2", "Slow=0.2" },
            Journal = "A derrick that mines ore up to 2 tiles away, and whose Tremors slow every foe in its range. Every Tide it stands through it sinks a Fathom, down to Fathom 5: each one mines faster and harder, and widens and strengthens its Tremors. Argo's guns inside its Tremors are loaded too: +1 pellet for a Scatter Cannon, +1 pierce for a Harpoon Tower, for each Fathom below the first.",
            Upgrades = new[]
            {
                U(Drill, "Paydirt", 2, "Pay Dirt", "Foes that fall in its Tremors are dug up for ore", "Paydirt_Base=2", "Yellowaura=30", "Deepcore3,Aftershock3")
                    .Fx("2 mining strikes per foe felled in its Tremors"),
                U(Drill, "Seismic", 2, "Seismic", "Tremors slow harder and reach farther", "Slow_Add=0.1;Range_Add=0.5", "Blueaura=25;Yellowaura=10", "Aftershock3")
                    .Fx("Tremor slow +10%", "Tremors +0.5 tiles"),
                U(Drill, "Deepcore", 3, "Deep Core", "Keeps sinking two Fathoms past the usual bottom", "Fathommax_Base=7", "Yellowaura=120", "Gusher4,Faultline4")
                    .Fx("Deepest Fathom 7"),
                U(Drill, "Aftershock", 3, "Aftershock", "Tremors also hurt every foe in range", "Pulse_Base=1.5", "Blueaura=80;Redaura=30", "Faultline4")
                    .Fx("1.5 damage every 2s"),
                U(Drill, "Gusher", 4, "Gusher", "Once it is deep enough, a gush of ore bursts up at each Tide", "Gusher_Base=12", "Yellowaura=275", "Mantle5")
                    .Fx("12 mining strikes at once, each Tide", "From Fathom 3 down"),
                U(Drill, "Faultline", 4, "Fault Line", "Tremors reach much farther", "Range_Add=1.5", "Blueaura=300", "")
                    .Fx("Tremors +1.5 tiles"),
                U(Drill, "Mantle", 5, "Mantle Tap", "Mines twice as hard, and once a Tide a quake pins every foe in its Tremors", "Damage_Pmulti=1;Mantle_Base=2", "Yellowaura=3000", "")
                    .Fx("Ore per strike x2", "Quake holds foes for 2s, once a Tide"),
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

    // Projectiles: { id, template, model }. Pellets and harpoons fly straight (Projectile_Ai_Simple, like the Heavy Bow).
    public static readonly string[][] Projectiles =
    {
        new[] { Pellet, "Tower_Smackyarcher_Heavyarrow", "Argo_Pellet" },
        new[] { HarpoonShot, "Tower_Smackyarcher_Heavyarrow", "Argo_Harpoon" },
        new[] { Lance, "Tower_Smackyarcher_Heavyarrow", "Argo_Lance" },
    };

    // The units the journal lists with Argo's buildings.
    public const string NautJournal = "Small divers from the Diving Bell. Sent to a tower, a Naut tunes it: faster attacks and a slow repair. Knocked out, it swims home and is back in 8 seconds.";
}
