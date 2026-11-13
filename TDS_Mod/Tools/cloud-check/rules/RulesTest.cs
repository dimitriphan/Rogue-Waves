// The mode's rules, checked here without the game (rules_check.py): each check names the rule and where it is written.
// These are the numbers the scope and the balance sweep set; a check that fails after a number was changed on purpose is
// changed with it, in the same commit.
using System;
using System.Collections.Generic;

public static class RulesTest
{
    public delegate bool Checker(string name, bool ok, string detail);

    public static void Run(Checker check)
    {
        Names(check);
        Routes(check);
        Lines(check);
        Difficulty(check);
    }

    static List<int> Route(params int[] cols) { return new List<int>(cols); }

    // 04_WorldNames.cs: "RW_Chart_<seed>[S]_<one column digit per row>r<retries>".
    static void Names(Checker check)
    {
        check("R1 a world's name is written as the header says", RWWorld.Name(123, Route(0, 1, 2), 2) == "RW_Chart_123_012r2"
            && RWWorld.Name(123, Route(0, 1, 2), 2, true) == "RW_Chart_123S_012r2" && RWWorld.Name(7, Route(), -1) == "RW_Chart_7_", RWWorld.Name(123, Route(0, 1, 2), 2, true));
        uint seed; List<int> route; int retries; bool stops;
        bool read = RWWorld.Parse("RW_Chart_4294967295S_3210r0", out seed, out route, out retries, out stops);
        check("R2 a name reads back: seed, stops, route, retries", read && seed == 4294967295u && stops && retries == 0 && string.Join("", route) == "3210", "");
        check("R3 a name without retries reads them as -1", RWWorld.Parse("RW_Chart_5_01", out seed, out route, out retries, out stops) && retries == -1 && !stops, "retries " + retries);
        string[] bad = { null, "", "World_Omeanchor", "RW_Chart_", "RW_Chart__01", "RW_Chart_x_01", "RW_Chart_5_0a1", "RW_Chart_5_01rx", "RW_Chart_5_01234567890", "RW_Chart_-5_01", "RW_Chart_5" };
        string took = "";
        foreach (string b in bad) if (RWWorld.Parse(b, out seed, out route, out retries, out stops)) took += " [" + b + "]";
        check("R4 a name that isn't a chart's is refused (another world, no seed, a letter in the route, more rows than the chart)", took == "", "accepted" + took);
    }

    // 04_WorldNames.cs Here and Choices, on 03_Chart.cs's charts (docs/scope/04 [Sea Chart]: ten rows, 1 to 3 links).
    static void Routes(Checker check)
    {
        string broke = "";
        int walked = 0;
        for (uint seed = 1; seed <= 200 && broke == ""; seed++)
            foreach (bool stops in new[] { false, true })
            {
                RWChart.Chart chart = RWChart.Generate(seed, RWMode.Pool, stops);
                List<int> route = new List<int>();
                Dictionary<string, bool> plans = new Dictionary<string, bool>();
                for (int row = 1; row <= RWChart.Rows; row++)
                {
                    // The harbour links to all of row 1; a map to 1 to 3 of the next row.
                    RWChart.Node at = RWWorld.Here(chart, route);
                    List<RWChart.Node> next = RWWorld.Choices(chart, route);
                    if (at == null || next.Count < 1 || next.Count > (row == 1 ? chart.Rows[1].Count : RWChart.MaxLinks)) { broke = "seed " + seed + " row " + row + ": " + next.Count + " choices"; break; }
                    RWChart.Node to = next[(int)((seed + (uint)row) % (uint)next.Count)];
                    if (to.Row != row) { broke = "seed " + seed + ": a choice from row " + (row - 1) + " is in row " + to.Row; break; }
                    if (to.Plan != "" && plans.ContainsKey(to.Plan)) { broke = "seed " + seed + ": " + to.Plan + " twice on one route"; break; }
                    plans[to.Plan] = true;
                    route.Add(to.Col);
                    walked++;
                }
                if (broke == "" && RWWorld.Choices(chart, route).Count != 0) broke = "seed " + seed + ": choices after the last row";
                if (broke == "" && !chart.At(RWChart.Rows, route[route.Count - 1]).Breaker) broke = "seed " + seed + ": row " + RWChart.Rows + " is not a Breaker";
            }
        check("R5 every route sails ten rows by 1 to 3 choices (any of row 1 from the harbour), no map twice, and ends on a Breaker (200 seeds, with and without stops)", broke == "", broke != "" ? broke : walked + " legs");
        RWChart.Chart c = RWChart.Generate(1, RWMode.Pool, false);
        int off = 0;
        while (c.At(0, 0).Links.Contains(off)) off++;
        check("R6 a route off the chart stands nowhere and offers nothing", RWWorld.Here(c, Route(off)) == null && RWWorld.Choices(c, Route(off)).Count == 0 && RWWorld.Here(c, Route(9)) == null, "column " + off);
    }

    // 14_RunSave.cs: nine tab-split fields; a line from before the eighth and ninth still reads.
    static void Lines(Checker check)
    {
        RWPlayer p = new RWPlayer();
        p.Key = "k"; p.Name = "a\tb"; p.Hero = "Hero_Yossele"; p.Pure = 12; p.Owed = true; p.StormLeft = 2; p.StormsDone = 1;
        p.Directions.Add("D1"); p.Trinkets.Add("T1"); p.Trinkets.Add("T2"); p.Run[RWBoard.Kills] = 40; p.Run[RWBoard.Watched] = 3;
        string line = RWRun.Encode(p);
        string[] f = line.Split('\t');
        check("R7 a player line has nine fields and a tab in a name can't add one", f.Length == 9 && f[1] == "a b" && f[3] == "12" && f[5] == "T1,T2" && f[6] == "1" && f[7] == "2/1"
            && f[8].Split(',').Length == RWBoard.Stats, line.Replace("\t", "|"));
        RWPlayer old = RWRun.Decode("k\tAnn\tHero_Spool\t7\tD1,D2\t\t0");
        bool zero = old != null;
        if (old != null) foreach (int v in old.Run) if (v != 0) zero = false;
        check("R8 a seven-field line from an older save reads: no storm, no board totals", zero && old.Pure == 7 && old.Directions.Count == 2 && old.Trinkets.Count == 0 && !old.Owed && old.StormLeft == 0 && old.StormsDone == 0, "");
        check("R9 a line with fewer than seven fields is no player", RWRun.Decode("k\tAnn\tHero_Spool\t7\tD1\t") == null && RWRun.Decode(null) == null, "");
        RWPlayer wild = RWRun.Decode("k\tAnn\tH\t-5\t\t\t1\t99/99\t-1,x,5");
        check("R10 a damaged line is held to its limits: Pure Aura 0 or more, the storm to its Tides and stacks, a total 0 or more",
            wild != null && wild.Pure == 0 && wild.StormLeft == RWTrinkets.StormTides && wild.StormsDone == RWCardinals.Stacks && wild.Run[0] == 0 && wild.Run[1] == 0 && wild.Run[2] == 5, "");
    }

    // 21_DifficultyRules.cs (docs/scope/04 [Difficulty]; F1-F5 of docs/reports/BALANCE_SWEEP_2026-11-05.md).
    static void Difficulty(Checker check)
    {
        int win = RWCampaign.WinTide;
        check("R11 map 1 ends on the base game's own Tide 15, maps 6-10 on row 24, and a map off the list takes the nearest", RWDifficulty.Finish(1) == 15 && RWDifficulty.Finish(6) == 24
            && RWDifficulty.Finish(10) == 24 && RWDifficulty.Finish(0) == 15 && RWDifficulty.Finish(99) == 24, "");
        string broke = "";
        for (int map = 1; map <= RWChart.Rows && broke == ""; map++)
        {
            int finish = RWDifficulty.Finish(map), last = 0;
            if (map > 1 && finish < RWDifficulty.Finish(map - 1)) broke = "map " + map + " ends on an earlier row than map " + (map - 1);
            for (int t = 1; t <= win + 20 && broke == ""; t++)
            {
                int row = RWDifficulty.RowOf(t, finish);
                if (row < t || row < last || (t > 1 && t <= win && row > last + finish)) broke = "map " + map + " Tide " + t + " plays row " + row + " after row " + last;
                if (t == win && row != finish) broke = "map " + map + " Tide " + win + " plays row " + row + ", not " + finish;
                if (t > win && row != last + 1) broke = "map " + map + " bonus Tide " + t + " plays row " + row + " after row " + last;
                if (map == 1 && t <= win && row != t) broke = "map 1 Tide " + t + " plays row " + row;
                last = row;
            }
        }
        check("R12 a map's Tides play rows that never fall back or below their own number, Tide 15 its finish row, the bonus Tides one row on each; map 1 is the base game's", broke == "", broke);
        check("R13 a Tide before the fifteenth plays row round(t x F / 15)", RWDifficulty.RowOf(5, 24) == 8 && RWDifficulty.RowOf(1, 24) == 2 && RWDifficulty.RowOf(7, 18) == 8 && RWDifficulty.RowOf(1, 18) == 1,
            RWDifficulty.RowOf(5, 24) + " " + RWDifficulty.RowOf(1, 24) + " " + RWDifficulty.RowOf(7, 18) + " " + RWDifficulty.RowOf(1, 18));
        Func<float, float, bool> near = (a, b) => Math.Abs(a - b) < 0.0005f;
        check("R14 foe health: x2.6 on map 1, 0.08 more a map, x(1 + 0.02 a thing held), and a count below 0 is 0", near(RWDifficulty.HealthFor(1, 0), 1.6f) && near(RWDifficulty.HealthFor(10, 0), 2.32f)
            && near(RWDifficulty.HealthFor(1, 10), 2.6f * 1.2f - 1f) && near(RWDifficulty.HealthFor(3, -4), RWDifficulty.HealthFor(3, 0)),
            RWDifficulty.HealthFor(1, 0) + " " + RWDifficulty.HealthFor(10, 0) + " " + RWDifficulty.HealthFor(1, 10));
        int[] on = { 0, 1, 1, 2, 2, 3, 3, 4, 4, 4 };
        string got = "";
        bool ok = true;
        for (int map = 1; map <= 10; map++) { int n = RWDifficulty.ModifiersOn(map, 4); got += n; if (n != on[map - 1]) ok = false; }
        check("R15 modifiers: none on map 1, one from map 2, one more every second map, all four by map 8, never more than the pool", ok && RWDifficulty.ModifiersOn(10, 2) == 2 && RWDifficulty.ModifiersOn(5, 0) == 0, got);
        broke = "";
        for (uint seed = 0; seed < 500 && broke == ""; seed++)
        {
            List<string> pool = RWDifficulty.CampaignPool(seed), again = RWDifficulty.CampaignPool(seed);
            int stock = 0, pack = 0;
            foreach (string m in pool) { if (Array.IndexOf(RWDifficulty.StockElites, m) >= 0) stock++; if (Array.IndexOf(RWDifficulty.PackModifiers, m) >= 0) pack++; }
            if (pool.Count != 4 || stock != 2 || pack != 2 || new HashSet<string>(pool).Count != 4) broke = "seed " + seed + ": " + string.Join(",", pool);
            else if (string.Join(",", pool) != string.Join(",", again)) broke = "seed " + seed + " rolls two pools";
            else if (Array.IndexOf(RWDifficulty.PackModifiers, pool[0]) < 0) broke = "seed " + seed + ": the first modifier is not one of the pack's";
        }
        check("R16 a campaign's pool is two of the pack's modifiers and two stock elites, none twice, the pack's first, the same for one seed (500 seeds)", broke == "", broke);
    }
}
