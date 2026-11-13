// RULES FILE: no engine types. Compiled whole and run here by Tools/cloud-check/rules_check.py (RogueWavesMode/README.md).
// The part's names and its save keys: constants only, so the save's readers and writers (04_WorldNames.cs,
// 06_CampaignSave.cs, 14_RunSave.cs) compile without the game. RWKeys' move of the old keys is 01_Util.cs.
public static class RWData
{
    public const string Part = "Mode";
    public const string Version = "0.1.0-dev";           // the pack version plus -dev (docs/PACK.md §4), since 2026-11-03; before it the part counted
                                                         // its own: 0.1 sea chart and sea weather, 0.2 rejoin, 0.3.0 restart rejoin, load watch, slot guard
    public const string Mode = "Mode_RogueWave";         // the mode's entry, group and Modeobject name
    public const string ModeDisplay = "Rogue Waves";
    public const string StockMode = "Mode_Normal";
    public const string PlanPrefix = "RW_";              // + the base Gameplan's name: its twin in Mode_RogueWave
    public const string SaveFolder = "RogueWaves";
    public const string KeyPrefix = "RW_";               // every key in SaveFolder (kit docs/23 §2 rule 3): RWKeys
    public const string WorldPrefix = "RW_Chart_";       // + seed + "_" + one column digit per row sailed + "r" + retries left (04_World.cs)
}

// The pack's keys in its own save folder (01_Util.cs has their rule and Migrate).
public static partial class RWKeys
{
    public const string Seed = "RW_Campaign_Seed", Route = "RW_Campaign_Route", Retries = "RW_Campaign_Retries",
        Replay = "RW_Campaign_Replay", Playing = "RW_Campaign_Playing", Choice = "RW_Campaign_Choice", Staged = "RW_Campaign_Staging",
        Lobby = "RW_Campaign_Lobby", Stops = "RW_Campaign_Stops", After = "RW_Campaign_After",
        StockWorld = "RW_Stock_World", StockGameplan = "RW_Stock_Gameplan",
        Guid = "RW_Player_Guid", Device = "RW_Player_Device";
}
