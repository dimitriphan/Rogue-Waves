// Every name, text and tuning number for Uloe in one place, from Dimitri's sheet (docs/scope/10_Vessel_Uloe.txt) and the
// defaults listed in ModSource/Uloe/DESIGN.md: a ranged vessel, Light as a stat each source re-adds, four buildables,
// Photons that wander, and Plug In on Buster's Dash keys.
public static class UloeData
{
    // A Rogue Waves part carries the pack version, "-dev" for the dev mod (docs/PACK.md section 4); Part names it in the
    // pack registry (ModKitPack.Join).
    public const string Version = "0.1.0-dev";
    public const string Part = "Uloe";
    public const bool Diagnostics = true;

    public const string TemplateHero = "Hero_Smacky";
    // Other vessels' asset groups Uloe borrows from: Buster's Totem beam (the Glass's beam), Sno Sno's snowball (the spark).
    // The game never unloads a group.
    public static readonly string[] ExtraGroups = { "Hero_Buster", "Hero_Snosno" };

    public const string Hero = "Hero_Uloe";
    public const string HeroEntity = "Hero_Uloe_Entity";
    public const string HeroTooltip = "Hero_Uloe_Tooltip_Upgradeproperties";

    public const string Glass = "Tower_Uloeglass";
    public const string Bank = "Tower_Uloebank";
    public const string Mirror = "Tower_Uloemirror";
    public const string Flood = "Tower_Uloeflood";
    public const string Photon = "Mob_Uloephoton";
    public static readonly string[] Towers = { Glass, Bank, Mirror, Flood };

    public const string PlugCommand = "Command_Hero_Uloe_Plug", PlugLink = "Plug";
    public const string AimCommand = "Command_Tower_Uloeflood_Aim", AimLink = "Aim";
    public const string SurgeCommand = "Command_Tower_Uloebank_Surge", SurgeLink = "Surge";
    public const string ColourCommand = "Command_Tower_Uloeflood_Colour", ColourLink = "Colour";
    public const string Spark = "Hero_Uloe_Spark", SparkTemplate = "Hero_Snosno_Snoball", SparkFallback = "Tower_Smackyarcher_Arrow";
    public const string Beam = "Tower_Uloeglass_Beam", BeamTemplate = "Tower_Bustertotem_Beam";
    public const string Plugged = "Plugged";       // the vessel's skin while plugged in

    // ---------------------------------------------------------------- sound levels (matched to the game's world SFX)

    public static readonly Dictionary<string, float> SoundVolume = new Dictionary<string, float>
    {
        { "Uloe_Zap", 0.02f }, { "Uloe_Beam", 0.018f }, { "Uloe_Plugin", 0.035f }, { "Uloe_Unplug", 0.025f },
        { "Uloe_Flash", 0.02f }, { "Uloe_Pop", 0.015f }, { "Uloe_Burst", 0.035f }, { "Uloe_Nullify", 0.025f },
        { "Uloe_Build", 0.025f }, { "Uloe_Strike", 0.015f }, { "Uloe_Landing", 0.1f }, { "Uloe_Death", 0.03f },
        { "Uloe_Footstep", 0.008f },
    };

    // ---------------------------------------------------------------- vessel texts (the game uses English text as the key)

    public const string Displayname = "Uloe";
    public const string Displaysub = "Light Personified";
    public const string Displayskill = "Advanced";
    public const string HeroDesc = "Frail, single-target ranged";
    public const string TooltipName = "Bright Idea";
    public const string TooltipDesc = "Ranged, towers grow stronger with the Light they receive, Uloe and Photons give Light, PLUG IN to a node or Power Bank for far more, Photons Illuminate and Blind foes";
    // The sheet: "Lore comes once the character is finalized".
    public const string Lore = "Uloe's story is still warming up.";
    public const string PlugName = "PLUG IN";
    public const string PlugDesc = "Plug into a node or a Power Bank and give off far more Light while the cord reaches";
    public const string PlugOffName = "UNPLUG";
    public const string PlugSelectTitle = "PLUG INTO WHAT?";
    public const string AimName = "AIM";
    public const string AimDesc = "Pick the building this Flood Light shines on";
    public const string AimSelectTitle = "AIM AT WHAT?";
    public const string SurgeName = "SURGE";
    public const string SurgeDesc = "Supernova: every Photon of this Power Bank bursts at once and comes straight back, brighter. Once a Tide";
    public const string ColourName = "COLOUR";
    public const string ColourDesc = "Colour Filter: pick the colour of the aura this Flood Light mines";
    public static readonly string[] Colours = { "All", "Redaura", "Blueaura", "Yellowaura" };
    public static readonly string[] ColourNames = { "ALL", "RED", "BLUE", "YELLOW" };

    // ---------------------------------------------------------------- vessel stats

    public static readonly string[] HeroStats =
    {
        "Health=30", "Speed=2.25", "Damage=2", "Range=3", "Projspeed=15", "Attackspeed=0.5", "Buildrange=2.5", "Sellrate=0.5",
    };

    // ---------------------------------------------------------------- Light

    public const string LightStat = "Light";
    public const float LightTick = 1f, LightHold = 1.5f;   // each source re-adds its timed Add every second (pitfall #119)
    public const float LightCap = 12f;                     // what a building counts, at most
    public const float TeamLightCap = 4f;                  // Light a building takes from teammates' sources, at most (scope 10 "Sharing")
    public const float HeroLight = 1f, HeroRadius = 3f;
    public const float PhotonLight = 1f, PhotonRadius = 2f;
    // Light's look: a skin per band (Lit1..3), each change at most this often, so a Photon walking by doesn't redraw a tower.
    public static readonly float[] LookBands = { 0f, 4f, 8f };
    public const float LookHold = 2f;

    public const float GlassPerLight = 0.2f;               // +20% Damage per Light
    public const float BankPerLight = 0.1f;                // Photons back 10% faster per Light
    public const float FloodPerLight = 0.1f;               // mines 10% faster per Light
    public const float MirrorLevel2 = 3f, MirrorLevel3 = 6f;

    // ---------------------------------------------------------------- Plug In

    public const float PlugRange = 5f, PlugRadius = 4f, PlugCooldown = 0.5f, PlugSlack = 0.5f;
    public const float NodeBase = 2f, NodePerAura = 0.4f, NodeMax = 10f;
    public const float BankBase = 4f, BankPerUpgrade = 2f;
    public const float LinkResend = 5f, LinkLife = 12f;    // how often cords, rays and rings are told again; when a client lets one go

    // ---------------------------------------------------------------- Blind and Illuminate (index = level - 1)

    public static readonly float[] BlindMulti = { 0.15f, 0.3f, 0.45f };
    public static readonly float[] LitMulti = { 0.15f, 0.3f, 0.45f };
    public static readonly float[] MarkSeconds = { 3f, 4f, 5f };
    public const float MarkRefresh = 1f;                   // the same level refreshes in its last second

    // ---------------------------------------------------------------- Directions (docs/scope/11), one upgrade per rank; index = rank

    public static readonly float[] ExtensionReach = { 0f, 2f, 4f, 6f };
    public static readonly float[] BrightLight = { 0f, 1f, 2f, 3f };
    public static readonly int[] BatteryUnits = { 0, 1, 1, 2 };
    public static readonly float[] BatteryRespawn = { 1f, 1f, 0.75f, 0.75f };
    public static readonly int[] FlashLit = { 0, 1, 1, 2 };
    public static readonly int[] FlashBlind = { 0, 0, 1, 2 };

    public class Dir { public string Name, Display; public string[] Desc; }
    public const string Extension = "Extension", Bright = "Brightbulb", Batteries = "Batteries", Flash = "Flashbulb";
    public static readonly Dir[] Directions =
    {
        new Dir { Name = Extension, Display = "Extension Cord", Desc = new[]
        {
            "PLUG IN reaches farther", "PLUG IN reaches farther still", "PLUG IN reaches much farther",
        } },
        new Dir { Name = Bright, Display = "Brighter Bulb", Desc = new[]
        {
            "Uloe gives off more Light", "Uloe gives off more Light still", "Uloe gives off much more Light",
        } },
        new Dir { Name = Batteries, Display = "Spare Batteries", Desc = new[]
        {
            "Each Power Bank keeps another Photon", "Photons come back quicker", "Each Power Bank keeps yet another Photon",
        } },
        new Dir { Name = Flash, Display = "Flash Bulb", Desc = new[]
        {
            "Uloe's sparks Illuminate foes", "Uloe's sparks also Blind foes", "Uloe's sparks Illuminate and Blind harder",
        } },
    };

    // ---------------------------------------------------------------- the Photon

    public static readonly string[] PhotonStats = { "Health=8", "Speed=1.75", "Damage=0.5", "Range=1.5", "Attackspeed=0.6" };
    public const string PhotonDisplay = "Photon";
    public const string PhotonDesc = "A little light, wanders, Illuminates and Blinds";
    public const string PhotonLore = "A loose bit of Uloe's light. It drifts about the Power Bank, brightens what it passes, and nips at anything that comes near.";
    public const string PhotonSheet = "Uloephoton-Sheet";
    public const int PhotonCell = 16;
    public const float PhotonLeash = 3.5f, PhotonEngage = 2.5f, PhotonVisit = 0.5f, WanderMin = 1.5f, WanderMax = 4f;
    public const float PathGrab = 1.25f;                   // how near a Flood Light's line a Photon must come to follow it
    public const int PhotonMark = 1, BurstMark = 2;      // the Blind and Illuminate level of a hit, of a burst

    // ---------------------------------------------------------------- the Magnifying Glass

    public const float BeamLifetime = 0.2f, BeamTick = 5f;   // one pulse a beam: each pulse costs 1 / Tickrate of its Lifetime
    public const float BeamEye = 0.42f;                      // the lens's height over the tower's position (UloeModels.LensY)
    public const float FocalWidth = 0.5f;                    // Focal Point: how near its line a foe must stand to be burned
    public static readonly float[] SunspotLight = { 4f, 8f, 12f };   // Light for Sunspot's Illuminate levels 1, 2, 3

    // ---------------------------------------------------------------- the Mirror and the Flood Light's capstones

    public const float HeliostatEvery = 1f;
    public const float SearchLine = 1f;                      // Searchlight: how near Long Throw's line a foe is in its light
    public const float LighthouseWidth = 0.6f, LighthouseStep = 0.2f;
    public const float ZapLook = 0.25f;                      // Chain Lightning's spark look, at most this often a Bank
    public const int SearchLit = 1, SearchLitBright = 2, LighthouseMark = 2, NovaMark = 3, DeathMark = 3;

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

    // The sheet's levels are the game's tiers from 2 (a tier-1 building takes one upgrade per level): scope 10's "% 1" is tier
    // 2, "% 4" tier 5. Next is each line's "Leads to" (none written: every choice of the next level; "ends": none). Eight a
    // building, as scope 10 and docs/reports/ULOE_UPGRADES_2026-11-04.md set them. The Effects lines are the journal's
    // EFFECTS, written for the player.
    public static readonly Build[] Builds =
    {
        new Build
        {
            Id = Glass, Template = "Tower_Smackyarcher", Display = "Magnifying Glass", Tag = "Uloeglass", Tags = new[] { "Tower", "Towerfighter" },
            Desc = "Focused, single-target beam, burns brighter in Light", Costs = "Redaura=30",
            Lore = "A big lens on a screw-ring stand. Unlit, it is a warm spot on a foe's back. Lit, it is the reason the foe stopped walking.",
            Stats = new[] { "Health=30+20", "Damage=2+0.5", "Attackspeed=0.75", "Range=4.5" },
            Upgrades = new[]
            {
                U(Glass, "Burn", 2, "Burning Point", "Burns hotter on the same target", "Ramp_Base=0.12;Rampmax_Base=1.2", "Redaura=35", "Polished3,Quick3",
                    "+12% Damage per beam on the same target, at most +120%", "A new target starts over"),
                U(Glass, "Widelens", 2, "Wide Lens", "Longer reach", "Range_Add=1.5", "Redaura=30", "Quick3,Prism3",
                    "Range +1.5 tiles"),
                U(Glass, "Polished", 3, "Polished Lens", "Makes more of every Light", "Perlight_Base=0.4", "Redaura=90", "Sunspot4",
                    "+40% Damage per Light, in place of +20%"),
                U(Glass, "Quick", 3, "Quick Focus", "Beams faster", "Attackspeed_Pmulti=0.5", "Redaura=80", "Sunspot4,Focal4",
                    "Attack speed +50%"),
                U(Glass, "Prism", 3, "Prism", "Splits the beam onto nearby foes", "Prism_Base=2;Prismradius_Base=1.5;Prismshare_Base=0.5", "Redaura=100", "Focal4",
                    "2 more beams, at foes within 1.5 tiles of its target", "Each does 50% damage"),
                U(Glass, "Sunspot", 4, "Sunspot", "Shows its target up for everyone", "Sunspot_Base=1", "Redaura=300;Blueaura=50", "Deathray5",
                    "Its target is Illuminated: level 1 from Light 4, level 2 from Light 8, level 3 at Light 12"),
                U(Glass, "Focal", 4, "Focal Point", "Burns through a whole line", "Focal_Base=1", "Redaura=350", "",
                    "Full damage to every foe on the line from the lens out to its range", "With Prism, the side beams burn through too"),
                U(Glass, "Deathray", 5, "Death Ray", "Never cools down", "Deathramp_Base=0.12;Deathmax_Base=2.5;Deathidle_Base=3", "Redaura=3000", "",
                    "+12% Damage per beam, at most +250%, kept from target to target", "It cools only after 3 s with nothing to beam",
                    "At full heat its target is Illuminated, level 3", "Takes the place of Burning Point's ramp"),
            }
        },
        new Build
        {
            Id = Bank, Template = "Tower_Smackyhut", Display = "Power Bank", Tag = "Uloebank", Tags = new[] { "Tower" },
            Desc = "Makes Photons that wander and give Light", Costs = "Redaura=40;Yellowaura=20",
            Lore = "A box of stored daylight. Photons leak out of its terminals, and Uloe can plug straight into it.",
            Stats = new[] { "Health=60+25", "Units=2", "Respawn=8" },
            Upgrades = new[]
            {
                U(Bank, "Short", 2, "Short Circuit", "Photons burst when they fall", "Burstdamage_Base=3;Burstradius_Base=1.5", "Redaura=40", "Static3,Surge3",
                    "A falling Photon hurts foes within 1.5 tiles for 3", "It Blinds and Illuminates them, level 2"),
                U(Bank, "Extracell", 2, "Extra Cell", "Holds another Photon", "Units_Add=1", "Yellowaura=35", "Static3,Bright3,Surge3",
                    "+1 Photon"),
                U(Bank, "Static", 3, "Static Charge", "Photons zap harder", "Static_Base=1;Staticmark_Base=2", "Redaura=90", "Chain4,Swarm4",
                    "Photon damage +100%", "Their hits Blind and Illuminate at level 2"),
                U(Bank, "Bright", 3, "Bright Spark", "Brighter Photons", "Photonlight_Base=2;Photonradius_Base=2.5", "Yellowaura=90", "Chain4,Swarm4",
                    "A Photon gives Light 2, within 2.5 tiles"),
                U(Bank, "Surge", 3, "Surge Protector", "Shields Uloe while plugged in", "Surge_Base=0.4;Surgeheal_Base=2", "Yellowaura=60;Blueaura=40", "",
                    "While Uloe is plugged into this Power Bank, it takes 40% less damage", "and heals 2 health a second"),
                U(Bank, "Chain", 4, "Chain Lightning", "Zaps jump between foes", "Chain_Base=2;Chainradius_Base=2", "Redaura=350", "Supernova5",
                    "Each Photon hit jumps to 2 more foes within 2 tiles", "Same damage, same Blind and Illuminate"),
                U(Bank, "Swarm", 4, "Photon Swarm", "A swarm of Photons", "Units_Add=2;Leash_Base=5", "Redaura=150;Yellowaura=200", "Supernova5",
                    "+2 Photons", "They roam up to 5 tiles from the Power Bank"),
                U(Bank, "Supernova", 5, "Supernova", "Every Photon goes off at once", "Supernova_Base=20;Novaradius_Base=2;Novalight_Base=3;Novatime_Base=15", "Redaura=2000;Yellowaura=1000", "",
                    "A SURGE button, once a Tide", "Every Photon of this Power Bank bursts for 20 within 2 tiles, Blind and Illuminate level 3",
                    "They all come back at once, and give Light 3 for 15 s"),
            }
        },
        new Build
        {
            Id = Mirror, Template = "Tower_Smackywall", Display = "Mirror", Tag = "Uloemirror", Tags = new[] { "Tower" },
            Desc = "Sturdy, Blinds what hits it when lit", Costs = "Blueaura=20",
            Lore = "A wall of panes in a brass frame. In the dark it is only a wall. Put a light on it and whatever swings at it gets the light in its eyes.",
            Stats = new[] { "Health=100+40" },
            Upgrades = new[]
            {
                U(Mirror, "Silver", 2, "Silver Backing", "Blinds harder", "Blindup_Base=1", "Blueaura=30", "Glare3,Bounce3,Tempered3",
                    "Blind one level stronger"),
                U(Mirror, "Vanity", 2, "Vanity Lights", "Shows foes up instead", "Illuminate_Base=1", "Blueaura=30", "Glare3,Bounce3,Tempered3",
                    "Illuminates what hits it, in place of Blind"),
                U(Mirror, "Glare", 3, "Glare", "Can shrug off a Blinded foe's hit", "Nullify_Base=0.2", "Blueaura=100", "Hall4,Reflection4",
                    "A hit from a foe with Blind level N has an N x 20% chance to do nothing"),
                U(Mirror, "Bounce", 3, "Bounce Card", "Passes its Light on to nearby towers", "Relay_Base=0.5;Relayradius_Base=3", "Blueaura=100", "Hall4,Reflection4",
                    "50% of the Light it receives goes to your buildings within 3 tiles"),
                U(Mirror, "Tempered", 3, "Tempered Glass", "Toughened panes", "Health_Pmulti=2;Tempered_Base=0.2", "Blueaura=90", "Hall4,Reflection4",
                    "Health +200% of its base", "Takes 20% less damage"),
                U(Mirror, "Hall", 4, "Hall of Mirrors", "Its flash spreads through the crowd", "Hall_Base=2", "Blueaura=350", "Heliostat5",
                    "Every foe within 2 tiles of the attacker gets the same mark, at the same level"),
                U(Mirror, "Reflection", 4, "Reflection", "Hits come back as light", "Reflect_Base=0.3;Reflectlit_Base=0.6", "Blueaura=300;Redaura=100", "",
                    "30% of each hit's damage goes back at the attacker", "60% from Light 6"),
                U(Mirror, "Heliostat", 5, "Heliostat", "Aims its Light at foes", "Heliostat_Base=1.5;Heliorange_Base=4", "Blueaura=2500;Redaura=1000", "",
                    "Once a second it beams the nearest foe within 4 tiles for 1.5 damage per Light it receives",
                    "The foe takes its Blind, or Illuminate after Vanity Lights, at its level", "Still a wall"),
            }
        },
        new Build
        {
            Id = Flood, Template = "Tower_Smackyhut", Display = "Flood Light", Tag = "Uloeflood", Tags = new[] { "Tower", "Gatherer" },
            Desc = "Mines a node, aims its Light at a tower", Costs = "Yellowaura=40",
            Lore = "A stadium lamp on a tripod. Its cord goes into the nearest node; what the node pays, the owner keeps, and what it burns, the lamp throws at whatever you point it at.",
            Stats = new[] { "Health=40+20", "Damage=1", "Attackspeed=0.4", "Range=2.5", "Aimrange=6", "Lightout=2" },
            Upgrades = new[]
            {
                U(Flood, "Wide", 2, "Wide Flood", "Lights all around it, can't be aimed", "Wide_Base=0.6;Wideradius_Base=3.5", "Yellowaura=30", "Halogen3,Voltage3",
                    "60% of its Light to your buildings within 3.5 tiles", "It can no longer be aimed"),
                U(Flood, "Long", 2, "Long Throw", "Throws a line of Light at a tower", "Line_Base=0.8;Linereach_Base=8;Linewidth_Base=0.6", "Yellowaura=30", "Halogen3,Voltage3",
                    "80% of its Light to every building on the line", "The line runs 8 tiles"),
                U(Flood, "Halogen", 3, "Halogen Bulb", "A brighter lamp", "Lightout_Add=2", "Yellowaura=90", "Filter4,Searchlight4",
                    "Light out +2"),
                U(Flood, "Voltage", 3, "High Voltage", "Strikes its node faster", "Attackspeed_Pmulti=0.6", "Yellowaura=100", "Filter4,Searchlight4",
                    "Strikes +60% faster"),
                U(Flood, "Filter", 4, "Colour Filter", "Pick the colour of its aura", "Filter_Base=0.7", "Yellowaura=250", "",
                    "A COLOUR button", "Your share of each payout from its strikes comes in the picked colour, at 70% of its total"),
                U(Flood, "Searchlight", 4, "Searchlight", "Foes in its light are caught out", "Searchlight_Base=1;Searchslow_Base=0.2;Searchradius_Base=1.5;Searchbright_Base=5",
                    "Yellowaura=250;Redaura=100", "Stadium5,Lighthouse5",
                    "Foes in its Light are Illuminated, level 1, level 2 once its Light out is 5 or more", "and slowed 20%, one slow a Flood Light",
                    "Its Light: its ring, its line, or 1.5 tiles round what it aims at"),
                U(Flood, "Stadium", 5, "Stadium Lights", "Lights up the whole ground", "Stadium_Base=1;Stadiumradius_Base=5;Attackspeed_Multi=1", "Yellowaura=3000", "",
                    "100% of its Light to your buildings within 5 tiles", "AIM comes back, and what it aims at takes its Light twice",
                    "Strikes twice as fast, and a second node in reach as well"),
                U(Flood, "Lighthouse", 5, "Lighthouse", "Sweeps the sea with light", "Lighthouse_Base=0.6;Lighthousereach_Base=9;Lighthouseturn_Base=6;Lighthousehold_Base=6",
                    "Yellowaura=2500;Redaura=500", "",
                    "Its beam turns a full circle every 6 s, 9 tiles long", "Your buildings it crosses keep 60% of its Light for 6 s",
                    "Foes it crosses are Blinded and Illuminated, level 2", "AIM goes away"),
            }
        },
    };

    public const float FloodNodePerAura = 0.2f, FloodNodeMax = 3f;

    public static Build BuildFor(string being)
    {
        foreach (Build b in Builds)
            if (b.Id == being)
                return b;
        return null;
    }

    // Owner-only messages, shown by UloeClient.
    public static readonly string[] WarnText =
    {
        "",
        "Nothing to plug into",
        "The cord won't reach that",
        "The plug is still warm",
        "The cord snapped",
        "Plugged in",
        "Unplugged",
        "No node in reach of this Flood Light",
        "Flood Light aimed",
        "Can't aim at that",
        "Too far to aim at",
        "Supernova!",
        "SURGE comes back next Tide",
    };
    public const int WarnNoPlug = 1, WarnFar = 2, WarnCooldown = 3, WarnSnapped = 4, WarnPlugged = 5, WarnUnplugged = 6, WarnNoNode = 7,
        WarnAimed = 8, WarnBadAim = 9, WarnAimFar = 10, WarnNova = 11, WarnNovaUsed = 12;
}
