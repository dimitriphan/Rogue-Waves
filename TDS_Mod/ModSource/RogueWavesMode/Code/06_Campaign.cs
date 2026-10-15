// The host's campaign as far as the chart goes: its seed and the route sailed (one column per map won), kept in the
// host's save under "RogueWaves" so Continue finds it (kit docs/15 §8, proven). Players' Pure Aura, Directions and
// Cardinals, retries and losing are later pieces (docs/scope/04 [Campaign], [Retries]; 06; 11; 12). A map counts
// as sailed when it is won; a lost or left map leaves the route as it was, so the party is back where it stood and
// can pick again.
// The campaign belongs to the save it was started or continued in: switching or clearing the save slot on the main
// menu gives the game a new Bla_Data.Playerdata (nothing else replaces it after launch), and the campaign in memory is
// then left behind (after a switch its own slot still holds it; clearing that slot erased it), so the next win never
// writes it into another slot (kit docs/23, #99).
public static class RWCampaign
{
    public static bool Active;
    public static uint Seed;
    public static readonly List<int> Route = new List<int>();
    public static int Choice = -1;             // a column of the next row the boat sails to by itself (the vote's winner)
    public static RWChart.Node Next;           // the node picked on the boat, whose map is loading or being played
    static object save;                        // the Bla_Data.Playerdata the campaign lives in

    public static RWChart.Chart Chart { get { return RWWorld.Chart(Seed); } }
    public static string WorldName() { return RWWorld.Name(Seed, Route); }
    public static RWChart.Node Here { get { return RWWorld.Here(Chart, Route); } }

    // A new campaign: seed 0 draws one. Host only.
    public static void New(uint seed)
    {
        if (seed == 0)
            seed = (uint)UnityEngine.Random.Range(1, int.MaxValue);
        Seed = seed;
        Route.Clear();
        Choice = -1;
        Next = null;
        Active = true;
        save = Blockage.Tags.Bla_Data.Playerdata;
        Save();
        RWLog.Info("new campaign, seed " + Seed);
    }

    // The campaign in the host's save, if there is one.
    public static bool Continue()
    {
        uint seed;
        List<int> route;
        if (!Saved(out seed, out route))
            return false;
        Seed = seed;
        Route.Clear();
        Route.AddRange(route);
        Choice = -1;
        Next = null;
        Active = true;
        save = Blockage.Tags.Bla_Data.Playerdata;
        RWLog.Info("continued campaign, seed " + Seed + ", " + Route.Count + " maps sailed");
        return true;
    }

    public static bool Saved(out uint seed, out List<int> route)
    {
        route = new List<int>();
        seed = 0;
        string s = RWUtil.ReadString(RWData.SaveFolder, "Campaign_Seed"), r = RWUtil.ReadString(RWData.SaveFolder, "Campaign_Route");
        return s != "" && RWWorld.Parse(RWData.WorldPrefix + s + "_" + r, out seed, out route);
    }

    // Ends the campaign and takes it out of the save (a won or failed campaign keeps nothing, docs/scope/04 [Campaign]).
    public static void Stop()
    {
        Active = false;
        Choice = -1;
        Next = null;
        save = null;
        RWUtil.Remove(RWData.SaveFolder, "Campaign_Seed");
        RWUtil.Remove(RWData.SaveFolder, "Campaign_Route");
        RWUtil.Flush();
        RWLog.Info("campaign stopped");
    }

    static void Save()
    {
        if (!Live)
            return;
        string digits = "";
        foreach (int c in Route) digits += c.ToString();
        RWUtil.WriteString(RWData.SaveFolder, "Campaign_Seed", Seed.ToString());
        RWUtil.WriteString(RWData.SaveFolder, "Campaign_Route", digits);
        RWUtil.Flush();
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
        Choice = -1;
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
        if (w != "" && !w.StartsWith(RWData.WorldPrefix)) RWUtil.WriteString(RWData.SaveFolder, "Stock_World", w);
        if (g != "" && !g.StartsWith("RW_")) RWUtil.WriteString(RWData.SaveFolder, "Stock_Gameplan", g);
    }

    public static void Stock(out string world, out string gameplan)
    {
        world = RWUtil.ReadString(RWData.SaveFolder, "Stock_World");
        gameplan = RWUtil.ReadString(RWData.SaveFolder, "Stock_Gameplan");
        Macro_Main main = Macro_Main.Instance;
        if (world == "") world = main != null && main.Defaultworld != null ? main.Defaultworld.Objectname : "";
        if (gameplan == "") gameplan = main != null ? main.Defaultgameplan : "";
    }

    // ModKitTides.MapEnd, host: a won map is sailed.
    public static void MapEnd(string result)
    {
        if (!Live || Next == null)
            return;
        string plan = Tds_Multiplayerinfo.Instance != null ? Tds_Multiplayerinfo.Instance.Gameplan.Value.ToString() : "";
        if (plan != RWMode.Twin(Next.Plan))
        {
            RWLog.Warn("map ended on " + plan + ", not the chart's " + RWMode.Twin(Next.Plan) + ": the route stays");
            return;
        }
        if (result == "Win")
        {
            Route.Add(Next.Col);
            Save();
            RWLog.Info("sailed " + Next.Name + ": route " + WorldName() + (Route.Count >= RWChart.Rows ? ", the chart is done" : ""));
        }
        else
            RWLog.Info("map " + Next.Name + " ended " + result + ": the route stays");
        Next = null;
    }
}
