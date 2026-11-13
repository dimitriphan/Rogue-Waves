// RULES FILE: no engine types. Compiled whole and run here by Tools/cloud-check/rules_check.py (RogueWavesMode/README.md).
// A player's line and the run's save (14_Run.cs has the header and the rest of RWRun): what Continue reads back.
// RWRun.HeroName, a hero's shown name, comes from the game (14_Run.cs).
public class RWPlayer
{
    public string Key = "", Name = "", Hero = "";
    public int Pure;
    public readonly List<string> Directions = new List<string>();   // every Direction upgrade held, each rank its own id
    public readonly List<string> Trinkets = new List<string>();     // Trinkets (Vessel Cardinals until 2026-11-04), each stack its own numbered id
    public bool Owed;                                               // a Direction pick waits for this player
    public float Sent;                                              // base aura sent in this break (Fund Transfer)
    public long Session = -1;                                       // in this host session
    // What the host last wrote on this player's vessel (RW_Ask, RW_Run), kept so a vessel that respawns carries it again.
    public string AskWhat = "", RunText = "";
    public float AskRound;
    // Bottled Storm (RWTrinkets): Tides left in the storm blowing now, and storms passed; saved, one storm to each stack.
    public int StormLeft, StormsDone;
    public bool Bought;                                             // bought at VENDCINT on this map, since Pearl Oyster (not saved)
    // The MVP board (RWBoard): tallies since the last board (not saved: a lost map's go with it), and the run's totals by
    // RWBoard's stat ids (saved, the ninth field).
    public int Leaks, Missed, Drops;
    public float SentMap;
    public readonly int[] Run = new int[RWBoard.Stats];
}

public static partial class RWRun
{
    public const string CountKey = "RW_Run_Count", PlayerKey = "RW_Run_Player_", CardinalsKey = "RW_Run_Team";
    public const int MaxSaved = 64;                   // players read back from a save at most: a damaged count can't hold the game in a loop
    public static readonly Dictionary<string, RWPlayer> Players = new Dictionary<string, RWPlayer>();
    public static readonly List<string> Cardinals = new List<string>();   // Cardinals held (Team Cardinals until 2026-11-04), each stack its own id

    // ---------------------------------------------------------------- the save

    public static void Clear()
    {
        Players.Clear();
        Cardinals.Clear();
    }

    public static void Save()
    {
        int n = 0;
        foreach (RWPlayer p in Players.Values)
        {
            if (n == MaxSaved) { RWLog.Once("max saved", "save: more than " + MaxSaved + " players in the run; the rest aren't saved"); break; }
            RWUtil.WriteString(RWData.SaveFolder, PlayerKey + n, Encode(p));
            n++;
        }
        RWUtil.WriteString(RWData.SaveFolder, CountKey, n.ToString());
        RWUtil.WriteString(RWData.SaveFolder, CardinalsKey, string.Join(",", Cardinals.ToArray()));
        RWUtil.Flush();
    }

    public static void Load()
    {
        Clear();
        int n = Math.Min(RWUtil.ReadInt(RWData.SaveFolder, CountKey, 0), MaxSaved);
        for (int i = 0; i < n; i++)
        {
            RWPlayer p = Decode(RWUtil.ReadString(RWData.SaveFolder, PlayerKey + i));
            if (p != null && p.Key != "") Players[p.Key] = p;
        }
        string held = RWUtil.ReadString(RWData.SaveFolder, CardinalsKey);
        Cardinals.AddRange(Ids(held));
    }

    // A saved list of ids; an empty one (a damaged key, ",," or a trailing comma) is dropped.
    static string[] Ids(string list) { return (list ?? "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries); }

    public static void Forget()
    {
        int n = Math.Min(RWUtil.ReadInt(RWData.SaveFolder, CountKey, 0), MaxSaved);
        for (int i = 0; i < n; i++)
            if (RWUtil.Has(RWData.SaveFolder, PlayerKey + i)) RWUtil.Remove(RWData.SaveFolder, PlayerKey + i);
        foreach (string key in new[] { CountKey, CardinalsKey })
            if (RWUtil.Has(RWData.SaveFolder, key)) RWUtil.Remove(RWData.SaveFolder, key);
        Clear();
    }

    // Tabs split the fields: a Steam name may hold anything but a tab, and a tab can't be typed into one. The eighth, Bottled
    // Storm's "<left>/<passed>", came on 2026-11-05: a line without it reads as no storm, and an older build reads the first seven.
    // The ninth, the MVP board's run totals (RWBoard stat ids, comma separated), came on 2026-11-06: a line without it reads
    // as a run with no boards yet.
    public static string Encode(RWPlayer p)
    {
        return string.Join("\t", new[] { p.Key, p.Name.Replace("\t", " "), p.Hero, p.Pure.ToString(), string.Join(",", p.Directions.ToArray()), string.Join(",", p.Trinkets.ToArray()), p.Owed ? "1" : "0",
            p.StormLeft + "/" + p.StormsDone, string.Join(",", Array.ConvertAll(p.Run, x => x.ToString())) });
    }

    public static RWPlayer Decode(string line)
    {
        string[] f = (line ?? "").Split('\t');
        if (f.Length < 7)
            return null;
        RWPlayer p = new RWPlayer();
        p.Key = f[0];
        p.Name = f[1];
        p.Hero = f[2];
        int.TryParse(f[3], out p.Pure);
        if (p.Pure < 0) p.Pure = 0;     // Pure Aura is only ever spent down to 0
        p.Directions.AddRange(Ids(f[4]));
        p.Trinkets.AddRange(Ids(f[5]));
        p.Owed = f[6] == "1";
        string[] storm = f.Length > 7 ? f[7].Split('/') : new string[0];
        if (storm.Length == 2 && int.TryParse(storm[0], out p.StormLeft) && int.TryParse(storm[1], out p.StormsDone))
        {
            p.StormLeft = Mathf.Clamp(p.StormLeft, 0, RWTrinkets.StormTides);
            p.StormsDone = Mathf.Clamp(p.StormsDone, 0, RWCardinals.Stacks);
        }
        else
            p.StormLeft = p.StormsDone = 0;
        string[] run = f.Length > 8 ? f[8].Split(',') : new string[0];
        for (int i = 0; i < run.Length && i < p.Run.Length; i++)
            if (!int.TryParse(run[i], out p.Run[i]) || p.Run[i] < 0) p.Run[i] = 0;
        return p;
    }

    // Continue's card: who sailed, from the save (kit docs/20: a card's hover text).
    public static string SavedCrew()
    {
        List<string> crew = new List<string>();
        int n = Math.Min(RWUtil.ReadInt(RWData.SaveFolder, CountKey, 0), MaxSaved);
        for (int i = 0; i < n; i++)
        {
            RWPlayer p = Decode(RWUtil.ReadString(RWData.SaveFolder, PlayerKey + i));
            if (p != null && p.Hero != "") crew.Add(p.Name != "" ? p.Name + " (" + HeroName(p.Hero) + ")" : HeroName(p.Hero));
        }
        return string.Join(", ", crew.ToArray());
    }
}
