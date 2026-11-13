// RULES FILE: no engine types. Compiled whole and run here by Tools/cloud-check/rules_check.py (RogueWavesMode/README.md).
// A chart world's name and what it says (04_World.cs builds the world): the name written and read, the chart of a seed,
// where a route stands on it and where it may sail next. The pool comes from RWMode.Pool.
public static partial class RWWorld
{
    static readonly Dictionary<ulong, RWChart.Chart> charts = new Dictionary<ulong, RWChart.Chart>();

    // ---------------------------------------------------------------- names

    public static string Name(uint seed, List<int> route, int retries) { return Name(seed, route, retries, false); }

    public static string Name(uint seed, List<int> route, int retries, bool stops)
    {
        string digits = "";
        foreach (int c in route) digits += c.ToString();
        return RWData.WorldPrefix + seed + (stops ? StopsMark : "") + "_" + digits + (retries >= 0 ? "r" + retries : "");
    }

    public const string StopsMark = "S";

    public static bool Parse(string name, out uint seed, out List<int> route)
    {
        int retries;
        bool stops;
        return Parse(name, out seed, out route, out retries, out stops);
    }

    public static bool Parse(string name, out uint seed, out List<int> route, out int retries)
    {
        bool stops;
        return Parse(name, out seed, out route, out retries, out stops);
    }

    // retries is -1 for a name that doesn't carry them (the save's seed and route, RWCampaign.Saved).
    public static bool Parse(string name, out uint seed, out List<int> route, out int retries, out bool stops)
    {
        seed = 0;
        route = new List<int>();
        retries = -1;
        stops = false;
        if (string.IsNullOrEmpty(name) || !name.StartsWith(RWData.WorldPrefix))
            return false;
        string rest = name.Substring(RWData.WorldPrefix.Length);
        int cut = rest.IndexOf('_');
        if (cut <= 0)
            return false;
        string head = rest.Substring(0, cut);
        if (head.EndsWith(StopsMark))
        {
            stops = true;
            head = head.Substring(0, head.Length - StopsMark.Length);
        }
        if (!uint.TryParse(head, out seed))
            return false;
        string tail = rest.Substring(cut + 1);
        int mark = tail.IndexOf('r');
        if (mark >= 0)
        {
            if (!int.TryParse(tail.Substring(mark + 1), out retries)) return false;
            tail = tail.Substring(0, mark);
        }
        foreach (char ch in tail)
        {
            if (ch < '0' || ch > '9') return false;
            route.Add(ch - '0');
        }
        return route.Count <= RWChart.Rows;
    }

    public static RWChart.Chart Chart(uint seed) { return Chart(seed, false); }

    public static RWChart.Chart Chart(uint seed, bool stops)
    {
        RWChart.Chart chart;
        ulong key = seed | (stops ? 1UL << 32 : 0UL);
        if (!charts.TryGetValue(key, out chart))
        {
            chart = RWChart.Generate(seed, RWMode.Pool, stops);
            charts[key] = chart;
            foreach (string line in RWChart.Dump(chart))
                RWLog.Info(line);
        }
        return chart;
    }

    // The chart a world's name is on (its seed, and its stops when the name has the mark), or null for another name.
    public static RWChart.Chart ChartOf(string name)
    {
        uint seed;
        List<int> route;
        int retries;
        bool stops;
        return Parse(name, out seed, out route, out retries, out stops) ? Chart(seed, stops) : null;
    }

    // The node the party stands on after sailing 'route' (the harbour before map 1), or null for a route off the chart.
    public static RWChart.Node Here(RWChart.Chart chart, List<int> route)
    {
        RWChart.Node at = chart.At(0, 0);
        for (int r = 0; r < route.Count && at != null; r++)
        {
            if (!at.Links.Contains(route[r])) return null;
            at = chart.At(r + 1, route[r]);
        }
        return at;
    }

    // The nodes the party may sail to next: the next row's links of where it stands.
    public static List<RWChart.Node> Choices(RWChart.Chart chart, List<int> route)
    {
        List<RWChart.Node> next = new List<RWChart.Node>();
        RWChart.Node at = Here(chart, route);
        if (at != null)
            foreach (int c in at.Links)
                if (chart.At(at.Row + 1, c) != null) next.Add(chart.At(at.Row + 1, c));
        return next;
    }
}
