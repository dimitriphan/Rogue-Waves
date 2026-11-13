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
//   stops        a campaign started since 2026-11-06 has rest and treasure stops on its chart (Stops; docs/mode/CHART_NAVIGATION.md
//                §4): a Cove plays no map, it gives a retry back (Rest, on the boat) and the route counts it as a row sailed; a Wreck
//                plays one row harder and pays each player a Trinket (RWVote). The route counts rows, so the campaign is still
//                won by leaving row 10's map
//   ended        ten maps won complete the campaign (Depth, its reward, is on hold); won or lost, it leaves the save, and
//                the party is taken back to the stock world on the boat (RWBoat)
// Players' Pure Aura, Directions, Cardinals and Trinkets are RWRun's (14_Run.cs), the votes (to leave, and on the next map)
// RWVote's (20_Vote.cs) (docs/scope/04, 06, 11, 12).
// The campaign belongs to the save it was started or continued in: switching or clearing the save slot on the main
// menu gives the game a new Bla_Data.Playerdata (nothing else replaces it after launch), and the campaign in memory is
// then left behind (after a switch its own slot still holds it; clearing that slot erased it), so the next win never
// writes it into another slot (kit docs/23, #99).
public static partial class RWCampaign
{
    static object save;                        // the Bla_Data.Playerdata the campaign lives in

    // The campaign's state and its steps are 06_CampaignRules.cs, which runs without the game; what it reads back from
    // the save is 06_CampaignSave.cs. Here: the game's side of a map's start and end, the slot guard, the stock world,
    // and at the end the few things the rules ask of the game.

    // ---------------------------------------------------------------- the map (host, ModKitTides)

    // A chart map starts: it is in play, and in the save, until it is won or lost.
    public static void MapStart()
    {
        MapStarted(Tds_Multiplayerinfo.Instance != null ? Tds_Multiplayerinfo.Instance.Gameplan.Value.ToString() : "", RWMode.Twin);
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
        {
            // A won map that ended in a bonus Tide: its board over the end screen (the last map's is the Voyage board).
            if (!RWBoard.ShownThisMap && Route.Count >= RWChart.Rows) { RWBoard.TakeMap(); RWBoard.ShowVoyage(RWBoard.NextEnd); }
            else RWBoard.ShowMap(RWBoard.NextBoat);
            RWRun.Save();    // what the bonus Tides earned stays
        }
        // The end screen's title (T4); a host leaving by the menu has no end screen to read it.
        RWCard.EndTitle(!Active ? (Ended == "won" ? "CAMPAIGN WON" : "CAMPAIGN LOST") : WonThisMap && Route.Count >= RWChart.Rows ? "CAMPAIGN WON"
            : WonThisMap ? "MAP " + LastMap + " OF " + RWChart.Rows + " WON" : "MAP LOST" + RWCard.Separator + RWCard.Retries(Retries).ToUpperInvariant());
        if (Live && Route.Count >= RWChart.Rows)
            Complete();
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

    // ---------------------------------------------------------------- what the rules ask of the game (06_CampaignRules.cs)

    static uint DrawSeed() { return (uint)UnityEngine.Random.Range(1, int.MaxValue); }
    static void HoldSave() { save = Blockage.Tags.Bla_Data.Playerdata; }
    static void DropSave() { save = null; }
    static bool SameSave() { return ReferenceEquals(save, Blockage.Tags.Bla_Data.Playerdata); }
    static void Say(string line) { RWRejoin.Say(line); }
    static void TipsSailed() { RWTips.MarkSailed(); }

    // A won map owes every player a Direction pick.
    static void OweEveryone()
    {
        foreach (long s in RWRun.Sessions())
            RWRun.For(s).Owed = true;
    }

    // The Cove's gift with every retry in hand: each player's lowest-ranked held Direction goes up a rank.
    static void RankEveryone()
    {
        foreach (long s in RWRun.Sessions())
            RWDirections.RankLowest(RWRun.For(s));
    }

    static void ShowVoyage() { RWBoard.ShowVoyage(RWBoard.NextEnd); }
}
