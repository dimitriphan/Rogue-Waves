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

    // ---------------------------------------------------------------- vessel stats

    public static readonly string[] HeroStats =
    {
        "Health=30", "Speed=2.25", "Damage=2", "Range=3", "Projspeed=15", "Attackspeed=0.5", "Buildrange=2.5", "Sellrate=0.5",
    };

    // ---------------------------------------------------------------- Light

    public const string LightStat = "Light";
    public const float LightTick = 1f, LightHold = 1.5f;   // each source re-adds its timed Add every second (pitfall #119)
    public const float LightCap = 12f;                     // what a building counts, at most
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
    public const int PhotonBlind = 1, PhotonLit = 1, BurstBlind = 2, BurstLit = 2;

    // ---------------------------------------------------------------- the Magnifying Glass

    public const float BeamLifetime = 0.2f, BeamTick = 5f;   // one pulse a beam: each pulse costs 1 / Tickrate of its Lifetime
    public const float BeamEye = 0.42f;                      // the lens's height over the tower's position (UloeModels.LensY)

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

    // The sheet's levels are the game's tiers from 2 (a tier-1 building takes one upgrade per level). Every choice of a level
    // leads to every choice of the next. The sheet's tree stops here: Dimitri's to extend. The Effects lines are the
    // journal's EFFECTS, written for the player.
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
                U(Glass, "Burn", 2, "Burning Point", "Burns hotter on the same target", "Ramp_Base=0.12;Rampmax_Base=1.2", "Redaura=35", "",
                    "+12% Damage per beam on the same target, at most +120%", "A new target starts over"),
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
                U(Bank, "Short", 2, "Short Circuit", "Photons burst when they fall", "Burstdamage_Base=3;Burstradius_Base=1.5", "Redaura=40", "",
                    "A falling Photon hurts foes within 1.5 tiles for 3", "It Blinds and Illuminates them, level 2"),
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
                U(Mirror, "Silver", 2, "Silver Backing", "Blinds harder", "Blindup_Base=1", "Blueaura=30", "Glare3,Bounce3",
                    "Blind one level stronger"),
                U(Mirror, "Vanity", 2, "Vanity Lights", "Shows foes up instead", "Illuminate_Base=1", "Blueaura=30", "Glare3,Bounce3",
                    "Illuminates what hits it, in place of Blind"),
                U(Mirror, "Glare", 3, "Glare", "Can shrug off a Blinded foe's hit", "Nullify_Base=0.2", "Blueaura=100", "",
                    "A hit from a foe with Blind level N has an N x 20% chance to do nothing"),
                U(Mirror, "Bounce", 3, "Bounce Card", "Passes its Light on to nearby towers", "Relay_Base=0.5;Relayradius_Base=3", "Blueaura=100", "",
                    "50% of the Light it receives goes to your buildings within 3 tiles"),
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
                U(Flood, "Wide", 2, "Wide Flood", "Lights all around it, can't be aimed", "Wide_Base=0.6;Wideradius_Base=3.5", "Yellowaura=30", "",
                    "60% of its Light to your buildings within 3.5 tiles", "It can no longer be aimed"),
                U(Flood, "Long", 2, "Long Throw", "Throws a line of Light at a tower", "Line_Base=0.8;Linereach_Base=8;Linewidth_Base=0.6", "Yellowaura=30", "",
                    "80% of its Light to every building on the line", "The line runs 8 tiles"),
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
    };
    public const int WarnNoPlug = 1, WarnFar = 2, WarnCooldown = 3, WarnSnapped = 4, WarnPlugged = 5, WarnUnplugged = 6, WarnNoNode = 7,
        WarnAimed = 8, WarnBadAim = 9, WarnAimFar = 10;
}
