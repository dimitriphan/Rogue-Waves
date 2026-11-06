// The sea chart as data: a harbour, then ten rows of 3-4 map nodes, each with its links to the next row, its place on
// the sea, its base map and its weather (docs/mode/SEA_CHART.md). An exact port of concept-tools/sea_chart.py: the same
// xorshift32 PRNG and the same draws in the same order, so the host, every client and the script build one chart from
// one seed. No Unity types here, so the parity check can compile this file alone (RogueWavesMode/README.md).
public static class RWChart
{
    public const int Rows = 10;
    public const int Stretch = 5;            // rows 5 and 10 close a stretch: Breaker maps
    public const int Nearest = 5;            // a node draws among this many maps nearest its row's aim
    public const int MaxLinks = 3;           // a map links to 1 to 3 maps in the next row (docs/scope/04 [Sea Chart])

    public const int Calm = 0, RisingWater = 1, QuickeningCurrent = 2, NestingGround = 3;
    public static readonly string[] WeatherNames = { "Calm", "Rising Water", "Quickening Current", "Nesting Ground" };

    // Rest and treasure stops (docs/mode/CHART_NAVIGATION.md §4, Dimitri's pick 2026-11-06): a Cove plays no map, a Wreck
    // plays its map one row harder and pays a Trinket. Both are calm. Drawn from a second stream after the chart is built,
    // so a seed's maps, links and weather stay as they were (a stop drops its weather).
    public const int NoStop = 0, Cove = 1, Wreck = 2;
    public const uint StopsSalt = 0x5EA57095u;
    public static readonly int[] CoveRows = { 4, 9 };                       // the row before each Breaker
    public static readonly int[,] WreckRows = { { 2, 2 }, { 6, 3 } };       // first row and span: rows 2-3, then 6-8

    public class Plan
    {
        public string Name, Map;
        public int Label;                    // Mapentryobject.Difficulty; the final map's 0 counts as 10
        public Plan(string name, string map, int difficulty) { Name = name; Map = map; Label = difficulty == 0 ? 10 : difficulty; }
    }

    public class Node
    {
        public int Row, Col;
        public System.Collections.Generic.List<int> Links = new System.Collections.Generic.List<int>();
        public float X, Z;                   // chart units; the world scales them
        public bool Breaker;
        public string Plan = "", Map = "";
        public int Label, Weather;
        public int Stop;                     // NoStop, Cove or Wreck
        public string Name { get { return Row == 0 ? "RW_Harbour" : "RW_Node_" + Row + "_" + Col; } }
    }

    public class Chart
    {
        public uint Seed;
        public bool Stops;                   // drawn with its Coves and Wrecks (a campaign started after 2026-11-06)
        public System.Collections.Generic.List<System.Collections.Generic.List<Node>> Rows = new System.Collections.Generic.List<System.Collections.Generic.List<Node>>();
        public Node At(int row, int col) { return row >= 0 && row < Rows.Count && col >= 0 && col < Rows[row].Count ? Rows[row][col] : null; }
        public Node Find(string name)
        {
            foreach (System.Collections.Generic.List<Node> r in Rows)
                foreach (Node n in r)
                    if (n.Name == name) return n;
            return null;
        }
    }

    class Rng
    {
        uint s;
        public Rng(uint seed) { s = seed != 0 ? seed : 0x9E3779B9u; }
        public uint Next() { s ^= s << 13; s ^= s >> 17; s ^= s << 5; return s; }
        public int Below(int n) { return (int)(Next() % (uint)n); }
    }

    // A map that already has a weather's object never gets that weather again.
    public static bool Native(int weather, string map)
    {
        switch (weather)
        {
            case RisingWater: return map == "Map_Manafold";
            case QuickeningCurrent: return map == "Map_Chrono" || map == "Map_Chrono_Alt";
            case NestingGround: return map == "Map_Vultures" || map == "Map_Vultures_Alt";
        }
        return false;
    }

    // Twice the label a row aims at (2 + (row-1)/2, two more on a Breaker row), so the comparisons stay integers.
    static int Aim2(int row) { return 4 + (row - 1) + (row % Stretch == 0 ? 4 : 0); }

    // Non-crossing links from a row of w nodes to a row of nw (the script's links_between explains the walk).
    static System.Collections.Generic.List<System.Collections.Generic.List<int>> LinksBetween(Rng rng, int w, int nw)
    {
        System.Collections.Generic.List<System.Collections.Generic.List<int>> kids = new System.Collections.Generic.List<System.Collections.Generic.List<int>>();
        for (int i0 = 0; i0 < w; i0++) kids.Add(new System.Collections.Generic.List<int>());
        kids[0].Add(0);
        int i = 0, k = 0;
        while (i != w - 1 || k != nw - 1)
        {
            int n = kids[i].Count;
            int spare = (nw - 1 - k) - (w - 1 - i);
            bool di, dk;
            if (i == w - 1) { di = false; dk = true; }
            else if (k == nw - 1) { di = true; dk = false; }
            else if (n == 1) { bool step = rng.Below(6) != 0; di = !step; dk = step; }
            else if (n == 2 && spare > 0)
            {
                int m = rng.Below(4);             // "ik", "ik", "k", "i"
                di = m != 2; dk = m != 3;
            }
            else if (spare > 0) { di = true; dk = true; }
            else { di = true; dk = false; }
            // The last node takes every child left, never more than MaxLinks (docs/scope/04: a map links to 1 to 3).
            if (di && i + 1 == w - 1 && nw - (k + (dk ? 1 : 0)) > MaxLinks) { di = false; dk = true; }
            if (di) i++;
            if (dk) k++;
            kids[i].Add(k);
        }
        return kids;
    }

    // pool: the base Gameplans a vote may offer, sorted by name (ordinal), as the script's POOL.
    public static Chart Generate(uint seed, System.Collections.Generic.List<Plan> pool) { return Generate(seed, pool, false); }

    public static Chart Generate(uint seed, System.Collections.Generic.List<Plan> pool, bool stops)
    {
        Rng rng = new Rng(seed);
        Chart chart = new Chart();
        chart.Seed = seed;
        int[] widths = new int[Rows + 1];
        widths[0] = 1;
        for (int r = 1; r <= Rows; r++) widths[r] = 3 + rng.Below(2);
        for (int r = 0; r <= Rows; r++)
        {
            System.Collections.Generic.List<Node> row = new System.Collections.Generic.List<Node>();
            for (int c = 0; c < widths[r]; c++) row.Add(new Node { Row = r, Col = c });
            chart.Rows.Add(row);
        }
        for (int r = 0; r < Rows; r++)
        {
            if (r == 0)
            {
                for (int c = 0; c < widths[1]; c++) chart.Rows[0][0].Links.Add(c);
                continue;
            }
            System.Collections.Generic.List<System.Collections.Generic.List<int>> kids = LinksBetween(rng, widths[r], widths[r + 1]);
            for (int c = 0; c < widths[r]; c++) chart.Rows[r][c].Links = kids[c];
        }
        for (int r = 1; r <= Rows; r++)
            for (int c = 0; c < widths[r]; c++)
            {
                Node n = chart.Rows[r][c];
                n.X = (c - (widths[r] - 1) / 2f) * 34f + (rng.Below(9) - 4) * 2;
                n.Z = r * 30 + (rng.Below(7) - 3) * 2;
                n.Breaker = r % Stretch == 0;
            }
        System.Collections.Generic.List<Plan> left = new System.Collections.Generic.List<Plan>(pool);
        for (int r = 1; r <= Rows; r++)
            foreach (Node n in chart.Rows[r])
            {
                if (left.Count == 0) break;
                int aim = Aim2(r);
                System.Collections.Generic.List<Plan> near = new System.Collections.Generic.List<Plan>(left);
                near.Sort((a, b) =>
                {
                    int da = System.Math.Abs(2 * a.Label - aim), db = System.Math.Abs(2 * b.Label - aim);
                    return da != db ? da.CompareTo(db) : string.CompareOrdinal(a.Name, b.Name);
                });
                if (near.Count > Nearest) near.RemoveRange(Nearest, near.Count - Nearest);
                Plan pick = near[rng.Below(near.Count)];
                left.Remove(pick);
                n.Plan = pick.Name; n.Map = pick.Map; n.Label = pick.Label;
            }
        for (int r = 1; r <= Rows; r++)
            foreach (Node n in chart.Rows[r])
            {
                n.Weather = Calm;
                if (r == 1 || n.Breaker) continue;
                if (rng.Below(r <= Stretch ? 3 : 2) != 0) continue;
                int first = 1 + rng.Below(3);
                for (int k = 0; k < 3; k++)
                {
                    int w = 1 + (first - 1 + k) % 3;
                    if (!Native(w, n.Map)) { n.Weather = w; break; }
                }
            }
        if (stops)
            PlaceStops(chart);
        return chart;
    }

    // The second stream (concept-tools/sea_chart.py stops()): a Cove's column in row 4, then in row 9; a Wreck's row and
    // column in the first stretch's middle, then in the second's.
    static void PlaceStops(Chart chart)
    {
        chart.Stops = true;
        Rng rng = new Rng(chart.Seed ^ StopsSalt);
        foreach (int r in CoveRows)
        {
            Node n = chart.Rows[r][rng.Below(chart.Rows[r].Count)];
            n.Stop = Cove;
            n.Weather = Calm;
        }
        for (int k = 0; k < WreckRows.GetLength(0); k++)
        {
            int r = WreckRows[k, 0] + rng.Below(WreckRows[k, 1]);
            Node n = chart.Rows[r][rng.Below(chart.Rows[r].Count)];
            n.Stop = Wreck;
            n.Weather = Calm;
        }
    }

    // Every node reachable from n (n included), for the chart view's lit and faded islands.
    public static System.Collections.Generic.List<Node> Ahead(Chart chart, Node n)
    {
        System.Collections.Generic.List<Node> seen = new System.Collections.Generic.List<Node>();
        if (n == null)
            return seen;
        seen.Add(n);
        for (int i = 0; i < seen.Count; i++)
            foreach (int k in seen[i].Links)
            {
                Node to = chart.At(seen[i].Row + 1, k);
                if (to != null && !seen.Contains(to)) seen.Add(to);
            }
        return seen;
    }

    // The lines concept-tools/sea_chart.py --check prints, for a diff of a PC log against the script.
    public static System.Collections.Generic.List<string> Dump(Chart chart)
    {
        System.Collections.Generic.List<string> lines = new System.Collections.Generic.List<string>();
        string widths = "";
        foreach (System.Collections.Generic.List<Node> r in chart.Rows) widths += (widths == "" ? "" : ",") + r.Count;
        lines.Add("CHART seed " + chart.Seed + (chart.Stops ? "S" : "") + " widths " + widths);
        foreach (System.Collections.Generic.List<Node> r in chart.Rows)
            foreach (Node n in r)
            {
                string links = "";
                foreach (int k in n.Links) links += (links == "" ? "" : ",") + k;
                lines.Add("CHART " + n.Name + " " + (int)System.Math.Round(n.X * 10) + "," + (int)System.Math.Round(n.Z * 10) + " " + (n.Plan == "" ? "-" : n.Plan)
                    + " " + (n.Breaker ? "B" : "-") + " w" + n.Weather + (n.Stop == Cove ? " cove" : n.Stop == Wreck ? " wreck" : "") + " -> " + (links == "" ? "-" : links));
            }
        return lines;
    }
}
