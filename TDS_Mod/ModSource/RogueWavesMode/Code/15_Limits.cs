// RULES FILE: no engine types. Compiled whole and run here by Tools/cloud-check/rules_check.py (RogueWavesMode/README.md).
// The three numbers a saved player line is read against (14_RunSave.cs), kept beside it so the save compiles without
// the game: the MVP board's stat ids, a Cardinal's stacks and Bottled Storm's Tides. The rest of each class is its own file.
public static partial class RWBoard
{
    // Stat ids: a card's numbers and the run's totals (RWPlayer.Run, saved) share them. Hoard is only ever a map's.
    public const int Kills = 0, Built = 1, Gained = 2, Spent = 3, Drowned = 4, Lost = 5, Sold = 6, Leaks = 7, Missed = 8,
        Drops = 9, Sent = 10, WinVotes = 11, PureSpent = 12, Mvps = 13, Hoard = 14, Maps = 15, Watched = 16, Stats = 17;
    // Watched: the crew's leaks on the maps a player sailed, charged or not (the Voyage caption's count).
}

public static partial class RWCardinals
{
    public const int Stacks = 6;     // numbered copies made at boot; buying past them still takes Pure Aura for no more effect
}

public static partial class RWTrinkets
{
    public const int StormTides = 5;   // Bottled Storm (24_Trinkets.cs)
}
