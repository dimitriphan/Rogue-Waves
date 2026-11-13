// RULES FILE: no engine types. Compiled whole and run here by Tools/cloud-check/rules_check.py (RogueWavesMode/README.md).
// What the save holds of a campaign, read back safely (06_Campaign.cs writes it and plays it).
public static partial class RWCampaign
{
    public const int RetriesPerCampaign = 3;   // docs/scope/04 [Retries]
    public const int WinTide = 15;             // a map is won once this Tide is cleared (docs/scope/04 [Tides and Leaving])

    public static bool Saved(out uint seed, out List<int> route)
    {
        int retries, replay, playing;
        return Saved(out seed, out route, out retries, out replay, out playing);
    }

    // A damaged key can't strand the boat: a route is cut back to the chart, and a column that isn't one of the next
    // row's choices is dropped (-1).
    public static bool Saved(out uint seed, out List<int> route, out int retries, out int replay, out int playing)
    {
        route = new List<int>();
        seed = 0;
        retries = RetriesPerCampaign;
        replay = playing = -1;
        string s = RWUtil.ReadString(RWData.SaveFolder, RWKeys.Seed), r = RWUtil.ReadString(RWData.SaveFolder, RWKeys.Route);
        bool stops = SavedStops();
        if (s == "" || s.EndsWith(RWWorld.StopsMark) || !RWWorld.Parse(RWData.WorldPrefix + s + "_" + r, out seed, out route))
            return false;
        retries = Mathf.Clamp(RWUtil.ReadInt(RWData.SaveFolder, RWKeys.Retries, RetriesPerCampaign), 0, RetriesPerCampaign);
        replay = RWUtil.ReadInt(RWData.SaveFolder, RWKeys.Replay, -1);
        playing = RWUtil.ReadInt(RWData.SaveFolder, RWKeys.Playing, -1);
        // A route that leaves the chart (a damaged key) is cut back to its last charted map: the world of a route off the
        // chart is never built (RWWorld.Ensure), so Continue would wait on the boat for it forever.
        // The replay and playing columns were saved for the route's own next row, so a cut route drops them too.
        RWChart.Chart chart = RWWorld.Chart(seed, stops);
        int sailed = route.Count;
        while (route.Count > 0 && RWWorld.Here(chart, route) == null)
            route.RemoveAt(route.Count - 1);
        if (route.Count < sailed)
        {
            RWLog.Once("route " + seed + " " + r, "save: route " + r + " leaves the chart of seed " + seed + " after " + route.Count + " maps; continued from there");
            replay = playing = -1;
        }
        // A Cove is never played or lost (it has no map): a column on one is a damaged key, as one off the row.
        List<int> cols = new List<int>();
        foreach (RWChart.Node n in RWWorld.Choices(chart, route)) if (n.Stop != RWChart.Cove) cols.Add(n.Col);
        if (!cols.Contains(replay)) replay = -1;
        if (!cols.Contains(playing)) playing = -1;
        return true;
    }

    static bool SavedStops() { return RWUtil.ReadString(RWData.SaveFolder, RWKeys.Stops) == "1"; }
}
