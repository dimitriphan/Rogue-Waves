// RULES FILE: no engine types. Compiled whole and run here by Tools/cloud-check/rules_check.py (RogueWavesMode/README.md).
// The campaign's state and what each event does to it (06_Campaign.cs has the header and the game's side): a new
// campaign, Continue, a map started, won or lost, a retry, a Cove, the end, and the one place that writes its save keys.
// What it needs of the game it asks through the few calls 06_Campaign.cs ends with (DrawSeed, HoldSave, DropSave,
// SameSave, Say, TipsSailed, OweEveryone, RankEveryone, ShowVoyage); rules/Standins.cs stands in for them here.
public static partial class RWCampaign
{
    // Only this class writes the campaign's state; a self-test build (every test define needs ROGUEWAVESMODE_SELFTEST)
    // opens it, as the tests set a field to stage a case.
#if ROGUEWAVESMODE_SELFTEST
    public static bool Active;
    public static uint Seed;
    public static int Retries = RetriesPerCampaign;
    public static int Replay = -1;             // a column of the next row: the lost map the boat must replay; -1 for none
    public static int Playing = -1;            // the column of the chart map being played (not yet won or lost); -1 for none
    public static int Choice = -1;             // a column of the next row the boat sails to by itself (the vote's winner)
    public static int After = -1;              // when Choice is a Cove: the column of the row after it, the second vote's winner
    public static bool Stops;                  // the chart has its Coves and Wrecks (the name's "S", RWKeys.Stops)
    public static RWChart.Node Next;           // the node picked on the boat, whose map is loading or being played
    public static bool Staging;                // a new campaign's first vote is still to come (docs/scope/04 [Sea Chart]: the staging map)
    public static int WonStop;                 // the stop the map in hand was (a Wreck pays when won, RWVote)
    public static bool WonThisMap;             // the map in hand was won (its bonus Tides, if any, are running or it just ended)
#else
    public static bool Active { get; private set; }
    public static uint Seed { get; private set; }
    public static int Retries { get; private set; } = RetriesPerCampaign;
    public static int Replay { get; private set; } = -1;             // a column of the next row: the lost map the boat must replay; -1 for none
    public static int Playing { get; private set; } = -1;            // the column of the chart map being played (not yet won or lost); -1 for none
    public static int Choice { get; private set; } = -1;             // a column of the next row the boat sails to by itself (the vote's winner)
    public static int After { get; private set; } = -1;              // when Choice is a Cove: the column of the row after it, the second vote's winner
    public static bool Stops { get; private set; }                  // the chart has its Coves and Wrecks (the name's "S", RWKeys.Stops)
    public static RWChart.Node Next { get; private set; }           // the node picked on the boat, whose map is loading or being played
    public static bool Staging { get; private set; }                // a new campaign's first vote is still to come (docs/scope/04 [Sea Chart]: the staging map)
    public static int WonStop { get; private set; }                 // the stop the map in hand was (a Wreck pays when won, RWVote)
    public static bool WonThisMap { get; private set; }             // the map in hand was won (its bonus Tides, if any, are running or it just ended)
#endif
    public static readonly List<int> Route = new List<int>();
    public static int HighestTide;             // the highest Tide started in this map (host)
    public static bool StopsOnNew = true;      // a new campaign's chart gets them (the older self-tests turn it off)
    public static string Rested = "";          // the last Cove's gift, said and logged (self-test)
    public static string Ended = "";           // how the last campaign ended in this launch: "won" or "lost" (self-test, log)
    public static int LastMap;                 // the number of the map last in play (the end screen's title)

    public static RWChart.Chart Chart { get { return RWWorld.Chart(Seed, Stops); } }
    public static string WorldName() { return RWWorld.Name(Seed, Route, Retries, Stops); }
    public static RWChart.Node Here { get { return RWWorld.Here(Chart, Route); } }

    // The maps the boat may go to next: the next row's links of where it stands, or only the map to replay.
    public static List<RWChart.Node> Open()
    {
        List<RWChart.Node> choices = RWWorld.Choices(Chart, Route);
        if (Replay < 0)
            return choices;
        List<RWChart.Node> one = new List<RWChart.Node>();
        foreach (RWChart.Node n in choices) if (n.Col == Replay) one.Add(n);
        return one.Count > 0 ? one : choices;
    }

    // A new campaign: seed 0 draws one. Host only.
    public static void New(uint seed)
    {
        if (seed == 0)
            seed = DrawSeed();
        Seed = seed;
        Stops = StopsOnNew;
        Route.Clear();
        Retries = RetriesPerCampaign;
        Replay = Playing = Choice = After = -1;
        Next = null;
        Ended = "";
        Active = true;
        WonThisMap = false;
        Staging = true;
        HoldSave();
        RWRun.Clear();
        if (RWUtil.Has(RWData.SaveFolder, RWKeys.Lobby)) RWUtil.Remove(RWData.SaveFolder, RWKeys.Lobby);   // a new campaign picks its lobby again
        Save();
        RWRun.Save();
        TipsSailed();
        RWLog.Info("new campaign, seed " + Seed + (Stops ? " with its Coves and Wrecks" : "") + ", " + Retries + " retries");
    }

    // The campaign in the host's save, if there is one. A map still in play there was left while it ran (the host quit
    // or the game closed): it counts as lost now. A campaign whose ten maps are won is completed instead.
    public static bool Continue()
    {
        uint seed;
        List<int> route;
        int retries, replay, playing;
        if (!Saved(out seed, out route, out retries, out replay, out playing))
            return false;
        Seed = seed;
        Stops = SavedStops();
        Route.Clear();
        Route.AddRange(route);
        Retries = retries;
        Replay = replay;
        Playing = playing;
        Choice = RWUtil.ReadInt(RWData.SaveFolder, RWKeys.Choice, -1);
        After = RWUtil.ReadInt(RWData.SaveFolder, RWKeys.After, -1);
        // A damaged pair: the column after a Cove only with a Cove to sail to first.
        RWChart.Node cove = null;
        foreach (RWChart.Node n in Open()) if (n.Col == Choice && n.Stop == RWChart.Cove) cove = n;
        if (cove == null || !cove.Links.Contains(After)) After = -1;
        Next = null;
        Ended = "";
        Active = true;
        WonThisMap = false;
        // Past map 1 the staging map is long over, whatever an older save says (MODE-A1: it was left on by a quit before map 1).
        Staging = Route.Count == 0 && RWUtil.ReadString(RWData.SaveFolder, RWKeys.Staged) == "1";
        HoldSave();
        RWRun.Load();
        TipsSailed();
        RWLog.Info("continued campaign, seed " + Seed + ", " + Route.Count + " maps sailed, " + Retries + " retries left" + (Replay >= 0 ? ", replaying column " + Replay : ""));
        if (Playing >= 0)
            Lost("the host left map " + (Route.Count + 1) + " while it ran, in an earlier launch");
        if (Live && Route.Count >= RWChart.Rows)
            Complete();
        return Live;
    }

    // Ends the campaign and takes it out of the save (a won or failed campaign keeps nothing, docs/scope/04 [Campaign]).
    public static void Stop()
    {
        Active = false;
        Choice = Replay = Playing = After = -1;
        Next = null;
        DropSave();
        foreach (string key in new[] { RWKeys.Seed, RWKeys.Route, RWKeys.Retries, RWKeys.Replay, RWKeys.Playing, RWKeys.Choice, RWKeys.Staged, RWKeys.Lobby, RWKeys.Stops, RWKeys.After })
            if (RWUtil.Has(RWData.SaveFolder, key)) RWUtil.Remove(RWData.SaveFolder, key);
        RWRun.Forget();
        RWUtil.Flush();
        RWLog.Info("campaign stopped");
    }

    static void Save()
    {
        if (!Live)
            return;
        string digits = "";
        foreach (int c in Route) digits += c.ToString();
        RWUtil.WriteString(RWData.SaveFolder, RWKeys.Seed, Seed.ToString());
        RWUtil.WriteString(RWData.SaveFolder, RWKeys.Route, digits);
        RWUtil.WriteString(RWData.SaveFolder, RWKeys.Retries, Retries.ToString());
        RWUtil.WriteString(RWData.SaveFolder, RWKeys.Replay, Replay >= 0 ? Replay.ToString() : "");
        RWUtil.WriteString(RWData.SaveFolder, RWKeys.Playing, Playing >= 0 ? Playing.ToString() : "");
        RWUtil.WriteString(RWData.SaveFolder, RWKeys.Choice, Choice >= 0 ? Choice.ToString() : "");
        RWUtil.WriteString(RWData.SaveFolder, RWKeys.Staged, Staging ? "1" : "");
        RWUtil.WriteString(RWData.SaveFolder, RWKeys.Stops, Stops ? "1" : "");
        RWUtil.WriteString(RWData.SaveFolder, RWKeys.After, After >= 0 ? After.ToString() : "");
        RWUtil.Flush();
    }

    // The self-tests' save after they set a field themselves; the mode's own code takes a step below instead.
    public static void SaveChoice()
    {
        Save();
    }

    // ---------------------------------------------------------------- the steps the boat and the vote take (host)

    // Only this class writes the campaign: the boat (05_Boat.cs) and the vote (20_Vote.cs) take one of these steps, and
    // a step that the save must hold saves itself.

    // The vote's winner or the only open map, kept so a party that stops on the boat sails to it after Continue.
    public static void Choose(int col)
    {
        Choice = col;
        Save();
    }

    // The same, with the leg after it: -1 unless the winner is a Cove whose second vote is already in.
    public static void Choose(int col, int after)
    {
        Choice = col;
        After = after;
        Save();
    }

    // The second vote's winner: the column to sail to after the Cove that Choice is on.
    public static void ChooseAfter(int col)
    {
        After = col;
        Save();
    }

    // The node picked on the boat: its map loads next, and the boat no longer sails by itself. Not saved: the boat picks again.
    public static void Pick(RWChart.Node node)
    {
        Next = node;
        Choice = -1;
    }

    // No node is picked: the harbour (the staging map) was, or the party was warped onto another chart name.
    public static void Unpick()
    {
        Next = null;
    }

    // The boat has set off for its choice, or the choice isn't open from where it stands: it sails by itself no more.
    // Not saved, as before this was a step: the save's choice goes at the next step that saves.
    public static void ClearChoice()
    {
        Choice = -1;
    }

    // The staging map is over: a quit before map 1 must not bring it back (MODE-A1).
    public static void StagingDone()
    {
        Staging = false;
        Next = null;
        Save();
    }

    // ---------------------------------------------------------------- the Cove (host, on the boat)

    public const string RetryBack = "retry", RankUp = "rank";

    // The boat reached a Cove (docs/mode/CHART_NAVIGATION.md §4.1): the route counts it as a row sailed, the party gets a retry
    // back (up to 3) or, with all 3 left, each player's lowest-ranked held Direction goes up a rank, and the second vote's
    // winner becomes the next choice. All of it in one write, so Continue after a quit here neither gives the gift twice nor
    // skips the leg after it. The chart's name changes once (route and retries together): the boat warps onto it (RWBoat).
    public static bool Rest(RWChart.Node cove)
    {
        if (!Live || cove == null || cove.Stop != RWChart.Cove || !Open().Contains(cove))
            return false;
        Route.Add(cove.Col);
        string gift;
        if (Retries < RetriesPerCampaign)
        {
            Retries++;
            gift = RetryBack;
        }
        else
        {
            gift = RankUp;
            RankEveryone();
        }
        Choice = cove.Links.Contains(After) ? After : -1;   // a stale winner from another Cove: the host is asked
        After = Replay = Playing = -1;
        Next = null;
        Save();
        RWRun.Save();
        Rested = gift;
        RWLog.Info("rested at " + cove.Name + " (row " + cove.Row + "): " + (gift == RetryBack ? "a retry back, " + Retries + " left" : "every player's lowest Direction goes up a rank") + (Choice >= 0 ? "; on to column " + Choice : "; the host picks the leg after it"));
        Say(gift == RetryBack ? "The Cove: the party rests and gets a retry back (" + Retries + " left)." : "The Cove: the party rests. With every retry in hand, each player's lowest Direction goes up a rank.");
        return true;
    }

    // ---------------------------------------------------------------- the map (host, ModKitTides)

    // A chart map starts: it is in play, and in the save, until it is won or lost.
    // 'plan' is the Gameplan the game started, 'twin' gives the mode's twin of a base plan (RWMode.Twin).
    static void MapStarted(string plan, System.Func<string, string> twin)
    {
        HighestTide = 0;
        WonThisMap = false;
        WonStop = RWChart.NoStop;
        if (!Live || Next == null)
            return;
        if (plan != twin(Next.Plan))
        {
            RWLog.Warn("map " + plan + " started, not the chart's " + twin(Next.Plan) + ": it doesn't count for the campaign");
            return;
        }
        if (Replay >= 0 && Next.Col != Replay)
            RWLog.Warn(Next.Name + " started, but the boat was to replay column " + Replay);
        Playing = Next.Col;
        LastMap = Route.Count + 1;
        // A counted chart map ends the staging map's turn, also when the party sailed past it: left on, every later boat
        // opened the harbour again and replayed it with free picks (MAX MODEA-1).
        Staging = false;
        Save();
        RWLog.Info("playing " + Next.Name + " (" + Next.Plan + "), map " + (Route.Count + 1) + " of " + RWChart.Rows + (Replay >= 0 ? ", a replay" : "") + ", " + Retries + " retries left");
    }

    // Tide 15 cleared: the map is won, and every Tide after it is a bonus Tide.
    public static void Tide(int wave)
    {
        if (wave > HighestTide)
            HighestTide = wave;
        if (!Live || Playing < 0 || wave <= WinTide)
            return;
        Cleared();
    }

    // Tide 15 is cleared: from its break, once no foe is left, or when Tide 16 starts. The map is won now.
    public static void Cleared()
    {
        if (!Live || Playing < 0)
            return;
        Won("Tide " + WinTide + " cleared");
        Say("Tide " + WinTide + " cleared: this map is won. The Tides from here are bonus Tides.");
    }

    // The main menu appeared: the campaign in memory goes, its save stays for Continue. A map still in play there counts
    // as lost when Continue reads it.
    public static void AtMainMenu()
    {
        if (Active)
            LeaveBehind("the party went back to the main menu");
    }

    // Host, on the boat, every frame: a map still in play was left while it ran (the host quit to the main menu), and a
    // campaign whose last map is won is complete.
    public static void OnBoat()
    {
        if (!Live)
            return;
        if (Playing >= 0)
            Lost("the host left map " + (Route.Count + 1) + " while it ran");
        if (Live && Route.Count >= RWChart.Rows)
            Complete();
    }

    static void Won(string why)
    {
        RWChart.Node node = Chart.At(Route.Count + 1, Playing);
        WonStop = node != null ? node.Stop : RWChart.NoStop;
        Route.Add(Playing);
        Playing = Replay = -1;
        Next = null;
        WonThisMap = true;
        OweEveryone();
        Save();
        RWRun.Save();
        RWLog.Info("map " + Route.Count + " won (" + why + "): sailed " + (node != null ? node.Name : "?") + ", route " + WorldName() + (Route.Count >= RWChart.Rows ? ", the chart is done" : ""));
    }

    // The team replays the map (Next stays on it, so a restarted map still counts), or, with no retry left, the
    // campaign ends.
    public static bool AutoReplay = true;    // off only in the self-test's pretend losses

    public static void Lost(string why)
    {
        int col = Playing;
        Playing = -1;
        RWRun.Load();        // what the lost map earned goes: it replays from the last save (docs/scope/04 [Retries])
        if (Retries > 0)
        {
            Retries--;
            Replay = col;
            if (AutoReplay) Choice = col;   // the boat sails back to it by itself, as to a vote's winner
            Save();
            RWLog.Info("map " + (Route.Count + 1) + " lost (" + why + "): " + Retries + " retries left, the boat replays column " + col);
            Say("Map lost. The team replays it from the boat: " + Retries + (Retries == 1 ? " retry" : " retries") + " left.");
            return;
        }
        Say("Map lost with no retries left: the campaign is over.");
        ShowVoyage();    // over the end screen: the maps won before (docs/mode/MVP_BOARD.md §1)
        End("lost", "map " + (Route.Count + 1) + " lost with no retries left (" + why + ")");
    }

    static void Complete()
    {
        Say("All " + RWChart.Rows + " maps sailed: the campaign is won.");
        End("won", "all " + RWChart.Rows + " maps won; Depth, its reward, is on hold");
    }

    static void End(string how, string why)
    {
        Ended = how;
        RWLog.Info("campaign " + how + ": " + why);
        Stop();
    }

    // Active, in the save it came from. Read this, not Active, before acting on the campaign.
    public static bool Live
    {
        get
        {
            if (Active && !SameSave())
                LeaveBehind("the save slot was switched or cleared");
            return Active;
        }
    }

    // Forgets the campaign here without touching any save: after a switch its own slot still holds it for Continue.
    static void LeaveBehind(string why)
    {
        Active = false;
        Seed = 0;
        Route.Clear();
        Retries = RetriesPerCampaign;
        Choice = Replay = Playing = After = -1;
        Stops = false;
        Next = null;
        DropSave();
        RWLog.Info("campaign left behind: " + why + "; it stays in its own slot");
    }
}
