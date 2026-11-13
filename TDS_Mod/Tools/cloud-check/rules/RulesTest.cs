// The mode's rules, checked here without the game (rules_check.py): each check names the rule and where it is written.
// R1-R16 the names, routes, player lines and difficulty; C1-C12 the campaign, played through its own steps.
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
        Campaign(check);
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

    // ---------------------------------------------------------------- the campaign (06_CampaignRules.cs; docs/scope/04)

    const string F = "RogueWaves/";

    static void Fresh(bool stops)
    {
        if (RWCampaign.Active) RWCampaign.Stop();
        RWUtil.Store.Clear();
        RWRun.Clear();
        RWCampaign.OtherSave = false;
        RWCampaign.StopsOnNew = stops;
        RWCampaign.AutoReplay = true;
        RWCampaign.Said.Clear();
        RWCampaign.Tips = RWCampaign.Owed = RWCampaign.Ranked = RWCampaign.Voyages = 0;
    }

    static string Key(string name) { string v; return RWUtil.Store.TryGetValue(F + name, out v) ? v : "(none)"; }

    // The whole stand-in save as "key=value" lines in key order.
    static string Dump()
    {
        List<string> lines = new List<string>();
        foreach (KeyValuePair<string, string> kv in RWUtil.Store) lines.Add(kv.Key + "=" + kv.Value);
        lines.Sort(string.CompareOrdinal);
        return string.Join(" | ", lines);
    }

    // Picks an open node on the boat and starts its map, as the game would: its twin plan.
    static RWChart.Node Start(int which)
    {
        List<RWChart.Node> open = RWCampaign.Open();
        RWChart.Node n = open[which % open.Count];
        RWCampaign.Pick(n);
        RWCampaign.TestMapStart(RWCampaign.TwinPrefix + n.Plan);
        return n;
    }

    static void Campaign(Checker check)
    {
        bool stopsWere = RWCampaign.StopsOnNew;

        // C1: the ten keys as the save has held them since 0.1.0-dev; a renamed key or a changed value fails here.
        Fresh(true);
        RWCampaign.New(77);
        string want = "RogueWaves/RW_Campaign_After= | RogueWaves/RW_Campaign_Choice= | RogueWaves/RW_Campaign_Playing= | RogueWaves/RW_Campaign_Replay= | RogueWaves/RW_Campaign_Retries=3"
            + " | RogueWaves/RW_Campaign_Route= | RogueWaves/RW_Campaign_Seed=77 | RogueWaves/RW_Campaign_Staging=1 | RogueWaves/RW_Campaign_Stops=1 | RogueWaves/RW_Run_Count=0 | RogueWaves/RW_Run_Team=";
        check("C1 a new campaign: 3 retries, no route, the staging map to come, and these keys in the save", RWCampaign.Live && RWCampaign.Staging && RWCampaign.Retries == 3 && RWCampaign.Route.Count == 0
            && RWCampaign.Stops && RWCampaign.Tips == 1 && Dump() == want, Dump());
        RWCampaign.NextSeed = 9001;
        Fresh(false);
        RWCampaign.New(0);
        check("C2 seed 0 draws one, and a campaign without stops saves none", RWCampaign.Seed == 9001 && Key("RW_Campaign_Seed") == "9001" && Key("RW_Campaign_Stops") == "", Key("RW_Campaign_Seed"));

        // C3: a map started is in the save until won; Tide 15 cleared (Tide 16 starts) wins it.
        Fresh(false);
        RWCampaign.New(5);
        RWChart.Node n = Start(1);
        bool started = RWCampaign.Playing == n.Col && Key("RW_Campaign_Playing") == n.Col.ToString() && !RWCampaign.Staging && Key("RW_Campaign_Staging") == "" && RWCampaign.LastMap == 1;
        RWCampaign.Tide(RWCampaign.WinTide);
        bool notYet = RWCampaign.Playing == n.Col && !RWCampaign.WonThisMap;
        RWPlayer ann = new RWPlayer(); ann.Key = "ann"; ann.Hero = "Hero_Spool"; RWRun.Players["ann"] = ann;
        RWCampaign.Tide(RWCampaign.WinTide + 1);
        check("C3 a started map is in the save; clearing Tide 15 wins it: the route grows, every player is owed a pick, the run is saved",
            started && notYet && RWCampaign.WonThisMap && RWCampaign.Playing == -1 && RWCampaign.Route.Count == 1 && RWCampaign.Route[0] == n.Col && Key("RW_Campaign_Route") == n.Col.ToString()
            && Key("RW_Campaign_Playing") == "" && RWCampaign.Owed == 1 && ann.Owed && Key("RW_Run_Count") == "1" && RWCampaign.HighestTide == RWCampaign.WinTide + 1 && RWCampaign.Next == null,
            "started " + started + " notYet " + notYet + " route " + Key("RW_Campaign_Route"));
        RWCampaign.TestMapStart("Some_Other_Plan");
        RWCampaign.Pick(RWCampaign.Open()[0]);
        RWCampaign.TestMapStart("Some_Other_Plan");
        check("C4 a map that isn't the picked node's doesn't count, nor one with no node picked", RWCampaign.Playing == -1 && Key("RW_Campaign_Playing") == "" && RWCampaign.Route.Count == 1, "playing " + RWCampaign.Playing);

        // C5: ten maps won, then the boat (or the map's end) completes the campaign and empties the save.
        Fresh(false);
        RWCampaign.New(11);
        string names = "";
        for (int map = 1; map <= RWChart.Rows; map++)
        {
            RWChart.Node at = Start(map);
            names += at.Row;
            RWCampaign.Tide(RWCampaign.WinTide + 1);
        }
        bool tenWon = RWCampaign.Live && RWCampaign.Route.Count == RWChart.Rows && RWCampaign.Open().Count == 0 && names == "12345678910";
        RWCampaign.OnBoat();
        check("C5 ten maps won complete the campaign: it ends won, says so, and leaves nothing of itself in the save", tenWon && !RWCampaign.Active && RWCampaign.Ended == "won" && RWUtil.Store.Count == 0
            && RWCampaign.Said[RWCampaign.Said.Count - 1].StartsWith("All 10 maps sailed"), "rows " + names + ", " + RWUtil.Store.Count + " keys left: " + Dump());

        // C6: a lost map spends a retry and is replayed; what it earned goes; with none left the campaign ends.
        Fresh(false);
        RWCampaign.New(12);
        ann = new RWPlayer(); ann.Key = "ann"; ann.Hero = "Hero_Spool"; ann.Pure = 5; RWRun.Players["ann"] = ann;
        RWRun.Save();
        string trail = "";
        bool held = true;
        int col = -1;
        for (int loss = 1; loss <= RWCampaign.RetriesPerCampaign; loss++)
        {
            n = Start(0);                                     // column 0: "0" in the save, not the empty "none"
            if (n.Col != 0 || (col >= 0 && n.Col != col)) held = false;   // only the lost map is open
            col = n.Col;
            if (Key("RW_Campaign_Playing") != "0") held = false;
            RWRun.Players["ann"].Pure = 99;
            RWCampaign.Lost("the check");
            trail += RWCampaign.Retries;
            if (!RWCampaign.Live || RWCampaign.Replay != col || RWCampaign.Choice != col || RWCampaign.Playing != -1 || RWCampaign.Open().Count != 1 || RWCampaign.Open()[0].Col != col
                || Key("RW_Campaign_Replay") != col.ToString() || Key("RW_Campaign_Retries") != RWCampaign.Retries.ToString() || Key("RW_Campaign_Choice") != col.ToString() || RWRun.Players["ann"].Pure != 5) held = false;
        }
        check("C6 a lost map spends a retry, the boat replays that map alone, and the run goes back to its last save", held && trail == "210" && RWCampaign.Route.Count == 0, "retries " + trail);
        Start(0);
        RWCampaign.Lost("the check");
        check("C7 a map lost with no retry left ends the campaign: lost, the Voyage board, nothing left in the save", !RWCampaign.Active && RWCampaign.Ended == "lost" && RWCampaign.Voyages == 1 && RWUtil.Store.Count == 0
            && RWCampaign.Said[RWCampaign.Said.Count - 1].StartsWith("Map lost with no retries left"), RWUtil.Store.Count + " keys left");

        // C8: the host quit while a map ran: Continue in a later launch counts it lost.
        Fresh(false);
        RWCampaign.New(13);
        n = Start(0);
        RWCampaign.Tide(RWCampaign.WinTide + 1);
        n = Start(1);
        string before = Dump();
        RWCampaign.OtherSave = true;                 // the launch ends: the campaign in memory goes, its save stays
        bool left = !RWCampaign.Live && RWCampaign.Seed == 0 && Dump() == before;
        RWCampaign.Choose(2);
        RWCampaign.StagingDone();
        bool untouched = Dump() == before;           // a campaign left behind writes nothing into the slot it is not in
        bool back = RWCampaign.Continue();
        check("C8 a campaign left behind keeps its save and writes nothing; Continue finds it, and the map that was running counts as lost",
            left && untouched && back && RWCampaign.Seed == 13 && RWCampaign.Route.Count == 1 && RWCampaign.Retries == 2 && RWCampaign.Replay == n.Col && n.Col > 0 && Key("RW_Campaign_Replay") == n.Col.ToString() && RWCampaign.Playing == -1 && Key("RW_Campaign_Playing") == "",
            "left " + left + " untouched " + untouched + " back " + back + " retries " + RWCampaign.Retries);
        Fresh(false);
        check("C9 Continue with no campaign in the save finds none", !RWCampaign.Continue() && !RWCampaign.Active, "");

        // C10: the steps the boat and the vote take, and which of them the save holds.
        Fresh(false);
        RWCampaign.New(14);
        RWCampaign.Choose(1);
        bool chose = RWCampaign.Choice == 1 && Key("RW_Campaign_Choice") == "1";
        RWCampaign.Choose(2, 0);
        bool both = Key("RW_Campaign_Choice") == "2" && Key("RW_Campaign_After") == "0";
        RWCampaign.ChooseAfter(1);
        bool after = RWCampaign.After == 1 && Key("RW_Campaign_After") == "1" && Key("RW_Campaign_Choice") == "2";
        RWCampaign.ClearChoice();
        bool cleared = RWCampaign.Choice == -1 && Key("RW_Campaign_Choice") == "2";      // not saved, as before the steps
        RWCampaign.Choose(0);
        RWCampaign.Pick(RWCampaign.Open()[0]);
        bool picked = RWCampaign.Next == RWCampaign.Open()[0] && RWCampaign.Choice == -1 && Key("RW_Campaign_Choice") == "0";
        RWCampaign.Unpick();
        RWCampaign.Pick(RWCampaign.Open()[0]);
        RWCampaign.StagingDone();
        check("C10 Choose and ChooseAfter save; Pick, Unpick and ClearChoice don't; StagingDone ends the staging map in the save",
            chose && both && after && cleared && picked && RWCampaign.Next == null && !RWCampaign.Staging && Key("RW_Campaign_Staging") == "",
            "chose " + chose + " both " + both + " after " + after + " cleared " + cleared + " picked " + picked);

        // C11: a Cove (docs/mode/CHART_NAVIGATION.md §4.1), on the first seed whose row 4 offers one.
        string cove = "no seed in 1-60 reaches a Cove";
        for (uint seed = 1; seed <= 60 && cove != ""; seed++)
            for (int lean = 0; lean < 3 && cove != ""; lean++)
            {
                Fresh(true);
                RWCampaign.New(seed);
                for (int map = 1; map <= 3; map++) { Start(lean); RWCampaign.Tide(RWCampaign.WinTide + 1); }
                RWChart.Node c = null, map5 = null;
                foreach (RWChart.Node o in RWCampaign.Open()) if (o.Stop == RWChart.Cove) c = o; else map5 = o;
                if (c == null) continue;
                cove = "";
                if (map5 != null && RWCampaign.Rest(map5)) cove += " rested on a map;";
                int route = RWCampaign.Route.Count;
                RWCampaign.Choose(c.Col, c.Links[0]);
                if (!RWCampaign.Rest(c) || RWCampaign.Rested != RWCampaign.RankUp || RWCampaign.Ranked != 1 || RWCampaign.Retries != 3) cove += " with 3 retries the gift is not the rank;";
                if (RWCampaign.Route.Count != route + 1 || Key("RW_Campaign_Route").Length != route + 1) cove += " the Cove is not a row sailed;";
                if (RWCampaign.Choice != c.Links[0] || RWCampaign.After != -1 || Key("RW_Campaign_Choice") != c.Links[0].ToString() || Key("RW_Campaign_After") != "") cove += " the second vote's winner is not the next choice;";
                if (RWCampaign.Rest(c)) cove += " rested twice;";
                // The same Cove with a retry spent on the way (map 2 lost once, then won): the retry comes back.
                Fresh(true);
                RWCampaign.New(seed);
                Start(lean); RWCampaign.Tide(RWCampaign.WinTide + 1);
                RWChart.Node lost = Start(lean);
                RWCampaign.Lost("the check");
                if (RWCampaign.Rest(RWCampaign.Open()[0])) cove += " rested on a map to replay;";
                if (Start(lean) != lost) cove += " the replay is another map;";
                RWCampaign.Tide(RWCampaign.WinTide + 1);
                Start(lean); RWCampaign.Tide(RWCampaign.WinTide + 1);
                c = null;
                foreach (RWChart.Node o in RWCampaign.Open()) if (o.Stop == RWChart.Cove) c = o;
                if (c == null || RWCampaign.Retries != 2 || !RWCampaign.Rest(c) || RWCampaign.Rested != RWCampaign.RetryBack || RWCampaign.Retries != 3 || RWCampaign.Ranked != 0
                    || Key("RW_Campaign_Retries") != "3" || RWCampaign.Choice != -1) cove += " with a retry spent the gift is not the retry;";
            }
        check("C11 a Cove counts as a row sailed, gives a retry back or, with all 3 in hand, a rank, hands on the second vote's winner, and no map is a rest", cove == "", cove);

        // C12: what Continue makes of a damaged or older save.
        Fresh(false);
        RWCampaign.New(15);
        n = Start(0);
        RWCampaign.Tide(RWCampaign.WinTide + 1);
        RWUtil.Store[F + "RW_Campaign_Staging"] = "1";      // an older save left it on past map 1 (MODE-A1)
        RWUtil.Store[F + "RW_Campaign_After"] = "2";        // the leg after a Cove, with no Cove chosen
        RWUtil.Store[F + "RW_Campaign_Choice"] = "0";
        RWUtil.Store[F + "RW_Campaign_Retries"] = "9";
        RWCampaign.OtherSave = true;
        bool gone = !RWCampaign.Live;
        check("C12 Continue past map 1 has no staging map, drops a leg after a Cove that isn't chosen, and holds the retries to 3",
            gone && RWCampaign.Continue() && !RWCampaign.Staging && RWCampaign.After == -1 && RWCampaign.Choice == 0 && RWCampaign.Retries == 3 && RWCampaign.Route.Count == 1,
            "staging " + RWCampaign.Staging + " after " + RWCampaign.After + " retries " + RWCampaign.Retries);

        Fresh(stopsWere);
    }
}
