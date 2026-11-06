#if ROGUEWAVESMODE_SELFTEST
// The chart view's and the stops' checks (docs/mode/CHART_NAVIGATION.md §8). At boot, from data: CV1 one tile per island and
// one lane per link, CV3 the faded islands are those out of reach, CV9 500 charts' Coves and Wrecks, and the stops' second
// stream leaves every chart's first-stream lines as they were. On the boat, after T6 (Run): CV10 a Cove won in the vote
// takes the boat there, it rests (a retry back, once), warps onto the next name and sails on to the second vote's winner;
// CV11 Continue at the Cove neither gives its gift twice nor skips the leg after it; CV12 a Wreck's pay: three Trinkets
// the player may take, one given free. CV2, CV4 and CV5 run in the flow's staging vote (96_Flow.cs); CV6-CV8 are the
// two-player round's and his eye's.
// The older checks play charts without stops (StopsOnNew off at boot), as they were written for them.
public static partial class RWTest
{
    static void StopsChecks()
    {
        RWChart.Chart chart = RWChart.Generate(Seed, RWMode.Pool, true);
        int islands = 0, links = 0;
        foreach (List<RWChart.Node> row in chart.Rows)
            foreach (RWChart.Node n in row) { islands++; links += n.Links.Count; }
        RWChart.Node here = chart.At(3, 0);
        List<RWChart.Node> path = RWChartView.Path(chart, new List<int> { 0, 0, 0 }, here);
        List<RWChart.Node> choices = RWWorld.Choices(chart, new List<int> { 0, 0, 0 });
        RWChartView.Layout lay = RWChartView.Lay(chart, path, here, choices, null, choices.Count > 0 ? choices[0] : null, new Rect(0f, 0f, 400f, 400f));
        Check("CV1 the view has one tile per island and one lane per link", lay.Tiles.Count == islands && lay.Lanes.Count == links,
            lay.Tiles.Count + " tiles for " + islands + " islands, " + lay.Lanes.Count + " lanes for " + links + " links (seed " + Seed + ": 37 and 59 in the design)");

        List<RWChart.Node> reach = RWChart.Ahead(chart, here);
        int wrong = 0, faded = 0;
        foreach (RWChartView.Tile t in lay.Tiles)
        {
            bool shouldFade = !reach.Contains(t.Node) && !path.Contains(t.Node);
            bool isFaded = !t.Reach && !t.Sailed;
            if (shouldFade != isFaded) wrong++;
            if (isFaded) faded++;
        }
        string order = "";
        foreach (RWChartView.Tile t in lay.Tiles) if (t.Choice) order += "ABC".Substring(Mathf.Min(2, t.Letter), 1) + t.Node.Col;
        Check("CV3 the faded islands are exactly those out of reach from where the party stands", wrong == 0 && faded > 0, faded + " faded, " + wrong + " wrong, choices " + order);

        int bad = 0, calmBreaks = 0;
        string first = "";
        for (uint seed = 1; seed <= 500; seed++)
        {
            RWChart.Chart plain = RWChart.Generate(seed, RWMode.Pool), stops = RWChart.Generate(seed, RWMode.Pool, true);
            int coves = 0, wrecks = 0;
            for (int r = 0; r <= RWChart.Rows; r++)
                for (int c = 0; c < stops.Rows[r].Count; c++)
                {
                    RWChart.Node a = plain.Rows[r][c], b = stops.Rows[r][c];
                    if (b.Stop == RWChart.Cove) { coves++; if (r != 4 && r != 9) bad++; }
                    if (b.Stop == RWChart.Wreck) { wrecks++; if (!(r >= 2 && r <= 3) && !(r >= 6 && r <= 8)) bad++; }
                    if (b.Stop != RWChart.NoStop && b.Weather != RWChart.Calm) bad++;
                    if (a.Plan != b.Plan || a.X != b.X || a.Z != b.Z || string.Join(",", a.Links.ConvertAll(k => k.ToString()).ToArray()) != string.Join(",", b.Links.ConvertAll(k => k.ToString()).ToArray())
                        || (b.Stop == RWChart.NoStop && a.Weather != b.Weather) || a.Stop != RWChart.NoStop) bad++;
                    if (b.Breaker && b.Stop != RWChart.NoStop) calmBreaks++;
                }
            if (coves != 2 || wrecks != 2) bad++;
            if (bad > 0 && first == "") first = "seed " + seed + ": " + coves + " Coves, " + wrecks + " Wrecks";
        }
        Check("CV9 500 charts: one Cove in rows 4 and 9, one Wreck in rows 2-3 and 6-8, every stop calm, the rest of each chart as without stops",
            bad == 0 && calmBreaks == 0, bad == 0 ? "all 500" : bad + " slips, first " + first);
        RWCampaign.StopsOnNew = false;
    }

    // CV10-CV12 on the boat: a campaign with stops, pretend wins up to row 3, then the Cove of row 4 for real.
    static IEnumerator Stops()
    {
        RWCampaign.StopsOnNew = true;
        RWCampaign.New(Seed);
        RWCampaign.StopsOnNew = false;
        RWCampaign.Staging = false;
        RWChart.Chart chart = RWCampaign.Chart;
        // A route to a node of row 3 that links to row 4's Cove.
        RWChart.Node cove = null;
        foreach (RWChart.Node n in chart.Rows[4]) if (n.Stop == RWChart.Cove) cove = n;
        RWChart.Node from = null;
        foreach (RWChart.Node n in chart.Rows[3]) if (cove != null && n.Links.Contains(cove.Col) && from == null) from = n;
        List<int> path = WayTo(chart, from);
        if (cove == null || from == null || path == null) { Setup("CV10-CV11", "no route to a Cove on seed " + Seed); yield break; }
        RWCampaign.Route.AddRange(path);
        RWCampaign.Retries = 2;
        RWCampaign.Choice = cove.Col;
        RWCampaign.After = cove.Links[cove.Links.Count - 1];
        RWCampaign.SaveChoice();
        int sailed = RWCampaign.Route.Count, after = RWCampaign.After;
        yield return Until(() => Here(), 40f);
        Check("CV10 the Cove is never picked: blacklisted on the boat", Listed(cove.Name), cove.Name);
        float since = Time.realtimeSinceStartup;
        yield return Until(() => RWBoat.RestedAt > since, 90f);
        bool rested = RWBoat.RestedAt > since;
        Check("CV10 the boat sails to the Cove and rests: a retry back, the route one row on, the second vote's winner next",
            rested && RWCampaign.Route.Count == sailed + 1 && RWCampaign.Route[sailed] == cove.Col && RWCampaign.Retries == 3 && RWCampaign.Choice == after && RWCampaign.After < 0
            && RWUtil.ReadString(RWData.SaveFolder, RWKeys.Retries) == "3" && RWUtil.ReadString(RWData.SaveFolder, RWKeys.Choice) == after.ToString(),
            (rested ? "rested at " + RWBoat.RestedAtCove : "no rest after 90 s, boat sailing '" + RWBoat.Sailing + "'") + ", route " + Digits(RWCampaign.Route) + ", retries " + RWCampaign.Retries + ", choice " + RWCampaign.Choice + ", gift " + RWCampaign.Rested);
        if (!rested) yield break;

        // CV11 before the boat sails on: a quit here, then Continue.
        RWCampaign.Active = false;
        bool back = RWCampaign.Continue();
        Check("CV11 Continue at the Cove neither gives its gift twice nor skips the leg after it", back && RWCampaign.Retries == 3 && RWCampaign.Route.Count == sailed + 1 && RWCampaign.Choice == after,
            "retries " + RWCampaign.Retries + ", route " + Digits(RWCampaign.Route) + ", choice " + RWCampaign.Choice);
        yield return Until(() => Here(), 40f);
        RWChart.Node next = chart.At(cove.Row + 1, after);
        yield return Until(() => RWBoat.ArrivedAt > since && RWBoat.Picked == (next != null ? next.Name : "?"), 90f);
        Check("CV10 the party warps onto the next name at the Cove and sails on to the second winner, whose Setup panel opens",
            Here() && next != null && RWBoat.Picked == next.Name, "world " + (Macro_Worldvisual.Instance.World != null ? Macro_Worldvisual.Instance.World.name : "-") + ", picked '" + RWBoat.Picked + "', banner '" + RWChartView.BannerTitle + "'");

        // CV12: the Wreck's pay for a pretend player (no vessel): three Trinkets they may take, one given free.
        RWPlayer p = new RWPlayer { Key = "test-wreck", Name = "Wreck test", Hero = "" };
        int pure = p.Pure;
        List<string> offer = RWVote.TreasureOffer(p, 4242);
        string why = offer.Count > 0 ? RWCardinals.Give(p, offer[0]) : "no offer";
        bool distinct = true;
        for (int i = 0; i < offer.Count; i++) if (offer.IndexOf(offer[i]) != i) distinct = false;
        Check("CV12 a Wreck pays one Trinket of three, free, held after", offer.Count == RWVote.TreasureCards && distinct && why == "" && p.Trinkets.Count == 1 && p.Pure == pure,
            "offered " + string.Join(", ", offer.ToArray()) + "; " + (why == "" ? "given " + string.Join(",", p.Trinkets.ToArray()) : why));
        RWChart.Node wreck = null;
        foreach (List<RWChart.Node> row in chart.Rows) foreach (RWChart.Node n in row) if (n.Stop == RWChart.Wreck && wreck == null) wreck = n;
        Info("CV12 a Wreck plays one row harder", wreck != null ? wreck.Name + " plays row " + (wreck.Row + 1) + "'s Tide 15 row " + RWDifficulty.Finish(wreck.Row + 1) + " (its own row's " + RWDifficulty.Finish(wreck.Row) + "); the map's own log line says it in a real map" : "no Wreck");
        RWCampaign.Stop();
    }

    // The columns from the harbour to n, or null.
    static List<int> WayTo(RWChart.Chart chart, RWChart.Node n)
    {
        if (n == null) return null;
        List<int> back = new List<int>();
        RWChart.Node at = n;
        while (at.Row > 0)
        {
            back.Insert(0, at.Col);
            RWChart.Node parent = null;
            foreach (RWChart.Node p in chart.Rows[at.Row - 1]) if (p.Links.Contains(at.Col) && parent == null) parent = p;
            if (parent == null) return null;
            at = parent;
        }
        return back;
    }
}
#endif
