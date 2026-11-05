// The host's campaign (docs/scope/04 [Campaign], [Retries], [Tides and Leaving]), kept in the host's save under
// "RogueWaves" (kit docs/15 §8, proven; the keys are RWKeys) so Continue finds it:
//   seed, route  the chart, and one column per map won; the boat stands on the route's last node
//   won          a map counts as won once Tide 15 is cleared (Tide 16 starts), or when it ends as a win before that, so
//                dying in a bonus Tide loses nothing. Every Tide after 15 is a bonus Tide, with no limit (RWDifficulty.Bonus)
//   retries      3 per campaign. A lost map spends one and the team replays it: the boat offers only that map (Replay)
//                until it is won. With none left, a lost map ends the campaign
//   in play      the map being played is in the save while it runs (Playing), so the host quitting during a map counts
//                as losing it: seen on the boat in the same launch, or by Continue in a later one. A friend who drops
//                spends nothing (they rejoin, RWRejoin)
//   ended        ten maps won complete the campaign (Depth, its reward, is on hold); won or lost, it leaves the save, and
//                the party is taken back to the stock world on the boat (RWBoat)
// Players' Pure Aura, Directions, Cardinals and Trinkets are RWRun's (14_Run.cs), the votes (to leave, and on the next map)
// RWVote's (20_Vote.cs) (docs/scope/04, 06, 11, 12).
// The campaign belongs to the save it was started or continued in: switching or clearing the save slot on the main
// menu gives the game a new Bla_Data.Playerdata (nothing else replaces it after launch), and the campaign in memory is
// then left behind (after a switch its own slot still holds it; clearing that slot erased it), so the next win never
// writes it into another slot (kit docs/23, #99).
public static class RWCampaign
{
    public const int RetriesPerCampaign = 3;   // docs/scope/04 [Retries]
    public const int WinTide = 15;             // a map is won once this Tide is cleared (docs/scope/04 [Tides and Leaving])
    public static bool Active;
    public static uint Seed;
    public static readonly List<int> Route = new List<int>();
    public static int Retries = RetriesPerCampaign;
    public static int Replay = -1;             // a column of the next row: the lost map the boat must replay; -1 for none
    public static int Playing = -1;            // the column of the chart map being played (not yet won or lost); -1 for none
    public static int HighestTide;             // the highest Tide started in this map (host)
    public static int Choice = -1;             // a column of the next row the boat sails to by itself (the vote's winner)
    public static RWChart.Node Next;           // the node picked on the boat, whose map is loading or being played
    public static bool Staging;                // a new campaign's first vote is still to come (docs/scope/04 [Sea Chart]: the staging map)
    public static string Ended = "";           // how the last campaign ended in this launch: "won" or "lost" (self-test, log)
    public static bool WonThisMap;             // the map in hand was won (its bonus Tides, if any, are running or it just ended)
    public static int LastMap;                 // the number of the map last in play (the end screen's title)
    static object save;                        // the Bla_Data.Playerdata the campaign lives in

    public static RWChart.Chart Chart { get { return RWWorld.Chart(Seed); } }
    public static string WorldName() { return RWWorld.Name(Seed, Route, Retries); }
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
            seed = (uint)UnityEngine.Random.Range(1, int.MaxValue);
        Seed = seed;
        Route.Clear();
        Retries = RetriesPerCampaign;
        Replay = Playing = Choice = -1;
        Next = null;
        Ended = "";
        Active = true;
        WonThisMap = false;
        Staging = true;
        save = Blockage.Tags.Bla_Data.Playerdata;
        RWRun.Clear();
        Save();
        RWRun.Save();
        RWTips.MarkSailed();
        RWLog.Info("new campaign, seed " + Seed + ", " + Retries + " retries");
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
        Route.Clear();
        Route.AddRange(route);
        Retries = retries;
        Replay = replay;
        Playing = playing;
        Choice = RWUtil.ReadInt(RWData.SaveFolder, RWKeys.Choice, -1);
        Next = null;
        Ended = "";
        Active = true;
        WonThisMap = false;
        // Past map 1 the staging map is long over, whatever an older save says (MODE-A1: it was left on by a quit before map 1).
        Staging = Route.Count == 0 && RWUtil.ReadString(RWData.SaveFolder, RWKeys.Staged) == "1";
        save = Blockage.Tags.Bla_Data.Playerdata;
        RWRun.Load();
        RWTips.MarkSailed();
        RWLog.Info("continued campaign, seed " + Seed + ", " + Route.Count + " maps sailed, " + Retries + " retries left" + (Replay >= 0 ? ", replaying column " + Replay : ""));
        if (Playing >= 0)
            Lost("the host left map " + (Route.Count + 1) + " while it ran, in an earlier launch");
        if (Live && Route.Count >= RWChart.Rows)
            Complete();
        return Live;
    }

    public static bool Saved(out uint seed, out List<int> route)
    {
        int retries, replay, playing;
        return Saved(out seed, out route, out retries, out replay, out playing);
    }

    // A column that isn't one of the next row's choices is dropped (-1), so a damaged key can't strand the boat.
    public static bool Saved(out uint seed, out List<int> route, out int retries, out int replay, out int playing)
    {
        route = new List<int>();
        seed = 0;
        retries = RetriesPerCampaign;
        replay = playing = -1;
        string s = RWUtil.ReadString(RWData.SaveFolder, RWKeys.Seed), r = RWUtil.ReadString(RWData.SaveFolder, RWKeys.Route);
        if (s == "" || !RWWorld.Parse(RWData.WorldPrefix + s + "_" + r, out seed, out route))
            return false;
        retries = Mathf.Clamp(RWUtil.ReadInt(RWData.SaveFolder, RWKeys.Retries, RetriesPerCampaign), 0, RetriesPerCampaign);
        replay = RWUtil.ReadInt(RWData.SaveFolder, RWKeys.Replay, -1);
        playing = RWUtil.ReadInt(RWData.SaveFolder, RWKeys.Playing, -1);
        List<int> cols = new List<int>();
        foreach (RWChart.Node n in RWWorld.Choices(RWWorld.Chart(seed), route)) cols.Add(n.Col);
        if (!cols.Contains(replay)) replay = -1;
        if (!cols.Contains(playing)) playing = -1;
        return true;
    }

    // Ends the campaign and takes it out of the save (a won or failed campaign keeps nothing, docs/scope/04 [Campaign]).
    public static void Stop()
    {
        Active = false;
        Choice = Replay = Playing = -1;
        Next = null;
        save = null;
        foreach (string key in new[] { RWKeys.Seed, RWKeys.Route, RWKeys.Retries, RWKeys.Replay, RWKeys.Playing, RWKeys.Choice, RWKeys.Staged })
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
        RWUtil.Flush();
    }

    // The vote's winner, kept so a party that stops on the boat sails to it after Continue.
    public static void SaveChoice()
    {
        Save();
    }

    // ---------------------------------------------------------------- the map (host, ModKitTides)

    // A chart map starts: it is in play, and in the save, until it is won or lost.
    public static void MapStart()
    {
        HighestTide = 0;
        WonThisMap = false;
        if (!Live || Next == null)
            return;
        string plan = Tds_Multiplayerinfo.Instance != null ? Tds_Multiplayerinfo.Instance.Gameplan.Value.ToString() : "";
        if (plan != RWMode.Twin(Next.Plan))
        {
            RWLog.Warn("map " + plan + " started, not the chart's " + RWMode.Twin(Next.Plan) + ": it doesn't count for the campaign");
            return;
        }
        if (Replay >= 0 && Next.Col != Replay)
            RWLog.Warn(Next.Name + " started, but the boat was to replay column " + Replay);
        Playing = Next.Col;
        LastMap = Route.Count + 1;
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
        RWRejoin.Say("Tide " + WinTide + " cleared: this map is won. The Tides from here are bonus Tides.");
    }

    // ModKitTides.MapEnd, host. "Win" before Tide 15 is cleared is a won map too (the self-test's, and the vote to
    // leave's); anything else ("Lose", "Leave": the host left the running map) loses it, unless Tide 15 was cleared,
    // which Tide() already counted.
    public static void MapEnd(string result)
    {
        if (!Live)
            return;
        if (Playing >= 0)
        {
            if (result == "Win")
                Won("the map ended " + result);
            else
                Lost("the map ended " + result + " in Tide " + HighestTide);
        }
        else if (!WonThisMap)
            return;
        else
            RWRun.Save();    // what the bonus Tides earned stays
        // The end screen's title (T4); a host leaving by the menu has no end screen to read it.
        RWCard.EndTitle(!Active ? (Ended == "won" ? "CAMPAIGN WON" : "CAMPAIGN LOST") : WonThisMap && Route.Count >= RWChart.Rows ? "CAMPAIGN WON"
            : WonThisMap ? "MAP " + LastMap + " OF " + RWChart.Rows + " WON" : "MAP LOST" + RWCard.Separator + RWCard.Retries(Retries).ToUpperInvariant());
        if (Live && Route.Count >= RWChart.Rows)
            Complete();
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
        Route.Add(Playing);
        Playing = Replay = -1;
        Next = null;
        WonThisMap = true;
        foreach (long s in RWRun.Sessions())
            RWRun.For(s).Owed = true;
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
            RWRejoin.Say("Map lost. The team replays it from the boat: " + Retries + (Retries == 1 ? " retry" : " retries") + " left.");
            return;
        }
        RWRejoin.Say("Map lost with no retries left: the campaign is over.");
        End("lost", "map " + (Route.Count + 1) + " lost with no retries left (" + why + ")");
    }

    static void Complete()
    {
        RWRejoin.Say("All " + RWChart.Rows + " maps sailed: the campaign is won.");
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
            if (Active && !ReferenceEquals(save, Blockage.Tags.Bla_Data.Playerdata))
                LeaveBehind("the save slot was switched or cleared");
            return Active;
        }
    }

    // Once at launch, every machine (only the host ever has a campaign). The game invokes these without a try.
    public static void Install()
    {
        Blockage.Tags.Bla_Data data = Blockage.Tags.Bla_Data.Instance;
        if (data == null)
        {
            RWLog.Warn("campaign: no Bla_Data to watch for slot switches; the check before each save still guards");
            return;
        }
        data.Onslotchanged += SlotChanged;
        data.Onslotremoved += SlotChanged;
    }

    public static void SlotChanged()
    {
        try { _ = Live; }
        catch (Exception e) { RWLog.Warn("campaign: slot change: " + e.Message); }
    }

    // Forgets the campaign here without touching any save: after a switch its own slot still holds it for Continue.
    static void LeaveBehind(string why)
    {
        Active = false;
        Seed = 0;
        Route.Clear();
        Retries = RetriesPerCampaign;
        Choice = Replay = Playing = -1;
        Next = null;
        save = null;
        RWLog.Info("campaign left behind: " + why + "; it stays in its own slot");
    }

#if ROGUEWAVESMODE_SELFTEST
    public static void PretendOtherSave() { save = new object(); }
#endif

    // The stock world and node the boat had before the party sailed onto the chart, for the save guard.
    public static void RememberStock()
    {
        string w = RWUtil.ReadString("Player", "Player_Worldselect"), g = RWUtil.ReadString("Player", "Player_Gameplanselect");
        if (w != "" && !w.StartsWith(RWData.WorldPrefix)) RWUtil.WriteString(RWData.SaveFolder, RWKeys.StockWorld, w);
        if (g != "" && !g.StartsWith(RWData.PlanPrefix)) RWUtil.WriteString(RWData.SaveFolder, RWKeys.StockGameplan, g);
    }

    public static void Stock(out string world, out string gameplan)
    {
        world = RWUtil.ReadString(RWData.SaveFolder, RWKeys.StockWorld);
        gameplan = RWUtil.ReadString(RWData.SaveFolder, RWKeys.StockGameplan);
        Macro_Main main = Macro_Main.Instance;
        if (world == "") world = main != null && main.Defaultworld != null ? main.Defaultworld.Objectname : "";
        if (gameplan == "") gameplan = main != null ? main.Defaultgameplan : "";
    }
}
