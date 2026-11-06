#if ROGUEWAVESMODE_SELFTEST
// Save and load fuzz (quality check 4, docs/reports/QUALITY_CHECKS_4-7_2026-11-05.md §1): seeded random and damaged values
// through the campaign save's readers and writers (06_Campaign.cs, 14_Run.cs, 04_World.cs names). The same cases run
//   in game    RWTest, on the boat before its campaign starts, against the real save in memory (RWUtil.HoldFlush: no
//              write reaches the disk until every key it touches is put back, then one flush)
//   in cloud   TDS_Mod/Tools/cloud-check/save_fuzz.py, against a dictionary standing in for the save, with the methods
//              copied from this part's code at each run
// One check per invariant, with the number of cases and the first case that broke it:
//   F1 a player line round-trips (Encode, Decode); F2 a damaged player line never throws, never gives negative Pure Aura
//   or an empty id; F3 the players and Cardinals round-trip through the save (RWRun.Save, Load); F4 a damaged player
//   count or Cardinals key loads at most RWRun.MaxSaved players, no empty id, in well under a second; F5 a chart world's
//   name round-trips (RWWorld.Name, Parse); F6 a damaged campaign key never throws, and what RWCampaign.Saved accepts is
//   a route on the chart, 0 to 3 retries and a map to replay or in play that is one of the next row's; F7 a whole
//   campaign's keys round-trip through Saved.
// No System.Core here (the game's reference list): plain loops, System.Random, DateTime for the clock.
public static class RWSaveFuzz
{
    public const int Seed = 20261105, Cases = 300, ChartSeeds = 6;
    public delegate bool Checker(string name, bool ok, string detail);

    static System.Random rng;
    static readonly string[] Keys = { RWKeys.Seed, RWKeys.Route, RWKeys.Retries, RWKeys.Replay, RWKeys.Playing, RWKeys.Choice, RWKeys.Staged, RWRun.CountKey, RWRun.CardinalsKey };

    public static void Run(Checker check)
    {
        rng = new System.Random(Seed);
        // What this touches, to put back: the campaign's keys, the run's keys, and the players and Cardinals in memory.
        List<string> names = new List<string>(Keys);
        for (int i = 0; i < RWRun.MaxSaved + 8; i++) names.Add(RWRun.PlayerKey + i);
        Dictionary<string, string> kept = new Dictionary<string, string>();
        foreach (string k in names) if (RWUtil.Has(RWData.SaveFolder, k)) kept[k] = RWUtil.ReadString(RWData.SaveFolder, k);
        // The players themselves, not their saved lines: Session and Sent are never saved, and others may hold them.
        List<RWPlayer> players = new List<RWPlayer>(RWRun.Players.Values);
        List<string> cardinals = new List<string>(RWRun.Cardinals);
        RWUtil.HoldFlush = true;
        try
        {
            Lines(check);
            Store(check);
            Worlds(check);
            Campaigns(check);
        }
        finally
        {
            foreach (string k in names)
            {
                if (kept.ContainsKey(k)) RWUtil.WriteString(RWData.SaveFolder, k, kept[k]);
                else if (RWUtil.Has(RWData.SaveFolder, k)) RWUtil.Remove(RWData.SaveFolder, k);
            }
            RWUtil.HoldFlush = false;
            RWUtil.Flush();
            RWRun.Clear();
            foreach (RWPlayer p in players) RWRun.Players[p.Key] = p;
            RWRun.Cardinals.AddRange(cardinals);
        }
    }

    // ---------------------------------------------------------------- F1, F2: one player's line

    static void Lines(Checker check)
    {
        int bad = 0;
        string first = "";
        for (int i = 0; i < Cases; i++)
        {
            RWPlayer p = RandomPlayer(i);
            string line = RWRun.Encode(p);
            RWPlayer q = RWRun.Decode(line);
            string why = q == null ? "decoded to nothing" : Differs(p, q);
            if (why != "" && bad++ == 0) first = "case " + i + ": " + why + " in '" + Show(line) + "'";
        }
        check("F1 a player line round-trips through Encode and Decode (" + Cases + " cases)", bad == 0, bad == 0 ? "" : bad + " broke; first " + first);

        bad = 0;
        first = "";
        for (int i = 0; i < Cases; i++)
        {
            string line = Garbage(i);
            string why = "";
            try
            {
                RWPlayer q = RWRun.Decode(line);
                if (q != null) why = Sane(q);
            }
            catch (Exception e) { why = "threw " + e.GetType().Name; }
            if (why != "" && bad++ == 0) first = "case " + i + ": " + why + " from '" + Show(line) + "'";
        }
        check("F2 a damaged player line never throws, never gives negative Pure Aura or an empty id (" + Cases + " cases)", bad == 0, bad == 0 ? "" : bad + " broke; first " + first);
    }

    // ---------------------------------------------------------------- F3, F4: the players and Cardinals in the save

    static void Store(Checker check)
    {
        int bad = 0;
        string first = "";
        for (int i = 0; i < Cases / 3; i++)
        {
            RWRun.Clear();
            int n = rng.Next(0, 7);
            for (int k = 0; k < n; k++) { RWPlayer p = RandomPlayer(i * 7 + k); p.Key = "guid-" + i + "-" + k; RWRun.Players[p.Key] = p; }
            int c = rng.Next(0, 9);
            for (int k = 0; k < c; k++) RWRun.Cardinals.Add(Id() + "#" + (k + 1));
            List<string> want = new List<string>();
            foreach (RWPlayer p in RWRun.Players.Values) want.Add(RWRun.Encode(p));
            string wantCards = string.Join(",", RWRun.Cardinals.ToArray());
            RWRun.Save();
            RWRun.Clear();
            RWRun.Load();
            List<string> got = new List<string>();
            foreach (RWPlayer p in RWRun.Players.Values) got.Add(RWRun.Encode(p));
            want.Sort(string.CompareOrdinal);
            got.Sort(string.CompareOrdinal);
            string why = string.Join("|", want.ToArray()) != string.Join("|", got.ToArray()) ? want.Count + " players saved, " + got.Count + " loaded or changed"
                : string.Join(",", RWRun.Cardinals.ToArray()) != wantCards ? "Cardinals '" + wantCards + "' came back '" + string.Join(",", RWRun.Cardinals.ToArray()) + "'" : "";
            if (why != "" && bad++ == 0) first = "case " + i + ": " + why;
        }
        check("F3 players and Cardinals round-trip through the save (" + (Cases / 3) + " cases)", bad == 0, bad == 0 ? "" : bad + " broke; first " + first);

        bad = 0;
        first = "";
        double slowest = 0;
        string[] counts = { "-5", "0", "abc", "", "3", "2147483647", "999999", "7" };
        for (int i = 0; i < Cases / 3; i++)
        {
            int real = rng.Next(0, 5);
            for (int k = 0; k < real; k++) RWUtil.WriteString(RWData.SaveFolder, RWRun.PlayerKey + k, rng.Next(3) == 0 ? Garbage(i + k) : RWRun.Encode(RandomPlayer(i + k)));
            string count = counts[rng.Next(counts.Length)];
            RWUtil.WriteString(RWData.SaveFolder, RWRun.CountKey, count);
            string cards = DamagedList();
            RWUtil.WriteString(RWData.SaveFolder, RWRun.CardinalsKey, cards);
            string why = "";
            DateTime start = DateTime.UtcNow;
            try
            {
                RWRun.Load();
                if (RWRun.Players.Count > RWRun.MaxSaved) why = RWRun.Players.Count + " players";
                foreach (string h in RWRun.Cardinals) if (h == "") why = "an empty Cardinal id";
                foreach (RWPlayer p in RWRun.Players.Values) if (why == "") why = Sane(p);
                // Load is what keeps an empty id out (Decode alone may give one, so F2 can't ask this): MAX KIT-8.
                foreach (KeyValuePair<string, RWPlayer> kv in RWRun.Players) if (why == "" && (kv.Key == "" || kv.Value.Key == "")) why = "an empty player id";
                RWRun.SavedCrew();
            }
            catch (Exception e) { why = "threw " + e.GetType().Name; }
            double ms = (DateTime.UtcNow - start).TotalMilliseconds;
            if (ms > slowest) slowest = ms;
            if (why == "" && ms > 500) why = "took " + Math.Round(ms) + " ms";
            if (why != "" && bad++ == 0) first = "case " + i + " (count '" + count + "', Cardinals '" + Show(cards) + "'): " + why;
            RWRun.Forget();
        }
        check("F4 a damaged player count or Cardinals key loads safely and fast (" + (Cases / 3) + " cases)", bad == 0, (bad == 0 ? "" : bad + " broke; first " + first + "; ") + "slowest " + Math.Round(slowest) + " ms");
    }

    // ---------------------------------------------------------------- F5: the chart world's name

    static void Worlds(Checker check)
    {
        int bad = 0;
        string first = "";
        for (int i = 0; i < Cases; i++)
        {
            uint seed = (uint)rng.Next(1, int.MaxValue) + (uint)rng.Next(0, 2) * 0x80000000u;
            List<int> route = new List<int>();
            int len = rng.Next(0, RWChart.Rows + 1);
            for (int k = 0; k < len; k++) route.Add(rng.Next(0, 4));
            int retries = rng.Next(-1, RWCampaign.RetriesPerCampaign + 1);
            bool stops = rng.Next(2) == 0;
            string name = RWWorld.Name(seed, route, retries, stops);
            uint s;
            List<int> r;
            int t;
            bool st;
            bool ok = RWWorld.Parse(name, out s, out r, out t, out st);
            string why = !ok ? "not parsed" : s != seed ? "seed " + s : Digits(r) != Digits(route) ? "route " + Digits(r) : t != retries ? "retries " + t : st != stops ? "stops " + st : "";
            if (why != "" && bad++ == 0) first = name + ": " + why;
        }
        check("F5 a chart world's name, with or without stops, round-trips through Name and Parse (" + Cases + " cases)", bad == 0, bad == 0 ? "" : bad + " broke; first " + first);
    }

    // ---------------------------------------------------------------- F6, F7: the campaign's keys

    static void Campaigns(Checker check)
    {
        uint[] seeds = new uint[ChartSeeds];
        for (int i = 0; i < ChartSeeds; i++) seeds[i] = (uint)(RWTestSeedBase + i * 7919);
        string[] seedKeys = { "", "-1", "abc", "99999999999", "12_3", " 5", "0" };
        string[] small = { "", "-1", "0", "1", "2", "3", "4", "9", "-7", "x", "2147483648" };
        int bad = 0, accepted = 0, trimmed = 0;
        string first = "";
        for (int i = 0; i < Cases; i++)
        {
            uint seed = seeds[rng.Next(seeds.Length)];
            bool damagedSeed = rng.Next(5) == 0;
            string seedKey = damagedSeed ? seedKeys[rng.Next(seedKeys.Length)] : seed.ToString();
            string routeKey = rng.Next(2) == 0 ? Digits(Walk(seed, rng.Next(0, RWChart.Rows + 1))) : RandomDigits();
            Write(RWKeys.Seed, seedKey);
            Write(RWKeys.Route, routeKey);
            Write(RWKeys.Retries, small[rng.Next(small.Length)]);
            Write(RWKeys.Replay, small[rng.Next(small.Length)]);
            Write(RWKeys.Playing, small[rng.Next(small.Length)]);
            string why = "";
            try
            {
                uint s;
                List<int> route;
                int retries, replay, playing;
                if (RWCampaign.Saved(out s, out route, out retries, out replay, out playing))
                {
                    accepted++;
                    int digits = routeKey.IndexOf('r') >= 0 ? routeKey.IndexOf('r') : routeKey.Length;   // Parse reads "r" on as retries
                    if (route.Count < digits) trimmed++;
                    RWChart.Chart chart = RWWorld.Chart(s);
                    List<int> cols = new List<int>();
                    foreach (RWChart.Node n in RWWorld.Choices(chart, route)) cols.Add(n.Col);
                    if (route.Count > RWChart.Rows) why = "a route of " + route.Count + " maps";
                    else if (RWWorld.Here(chart, route) == null) why = "route " + Digits(route) + " leaves the chart of seed " + s;
                    else if (retries < 0 || retries > RWCampaign.RetriesPerCampaign) why = retries + " retries";
                    else if (replay != -1 && !cols.Contains(replay)) why = "replay column " + replay;
                    else if (playing != -1 && !cols.Contains(playing)) why = "playing column " + playing;
                }
            }
            catch (Exception e) { why = "threw " + e.GetType().Name + ": " + e.Message; }
            if (why != "" && bad++ == 0) first = "case " + i + " (seed '" + seedKey + "', route '" + routeKey + "'): " + why;
        }
        check("F6 a damaged campaign key never throws or strands the boat (" + Cases + " cases, " + accepted + " accepted, " + trimmed + " routes cut back)", bad == 0, bad == 0 ? "" : bad + " broke; first " + first);

        bad = 0;
        first = "";
        for (int i = 0; i < Cases / 3; i++)
        {
            uint seed = seeds[rng.Next(seeds.Length)];
            List<int> route = Walk(seed, rng.Next(0, RWChart.Rows));
            List<int> cols = new List<int>();
            foreach (RWChart.Node n in RWWorld.Choices(RWWorld.Chart(seed), route)) cols.Add(n.Col);
            int retries = rng.Next(0, RWCampaign.RetriesPerCampaign + 1);
            int replay = cols.Count > 0 && rng.Next(2) == 0 ? cols[rng.Next(cols.Count)] : -1;
            int playing = cols.Count > 0 && rng.Next(2) == 0 ? cols[rng.Next(cols.Count)] : -1;
            Write(RWKeys.Seed, seed.ToString());
            Write(RWKeys.Route, Digits(route));
            Write(RWKeys.Retries, retries.ToString());
            Write(RWKeys.Replay, replay >= 0 ? replay.ToString() : "");
            Write(RWKeys.Playing, playing >= 0 ? playing.ToString() : "");
            uint s;
            List<int> r;
            int t, rp, pl;
            bool ok = RWCampaign.Saved(out s, out r, out t, out rp, out pl);
            string why = !ok ? "not read" : s != seed || Digits(r) != Digits(route) ? "came back " + s + " " + Digits(r)
                : t != retries || rp != replay || pl != playing ? "retries/replay/playing " + t + "/" + rp + "/" + pl + ", wrote " + retries + "/" + replay + "/" + playing : "";
            if (why != "" && bad++ == 0) first = "case " + i + " (seed " + seed + ", route " + Digits(route) + "): " + why;
        }
        check("F7 a whole campaign's keys round-trip through Saved (" + (Cases / 3) + " cases)", bad == 0, bad == 0 ? "" : bad + " broke; first " + first);
    }

    const int RWTestSeedBase = 20261009;    // RWTest.Seed, the campaign the self-test sails (not in the cloud harness, so copied)

    // A route along the chart's links from the harbour, len maps long.
    static List<int> Walk(uint seed, int len)
    {
        RWChart.Chart chart = RWWorld.Chart(seed);
        List<int> route = new List<int>();
        for (int k = 0; k < len; k++)
        {
            List<RWChart.Node> next = RWWorld.Choices(chart, route);
            if (next.Count == 0) break;
            route.Add(next[rng.Next(next.Count)].Col);
        }
        return route;
    }

    // ---------------------------------------------------------------- random values

    const string IdChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789_";
    static readonly string[] Odd = { ",", "\t", "\"", "\\", "'", ":", "#", " ", "é", "ß", "語", " ", "{", "}", "|" };

    static string Id()
    {
        int n = rng.Next(1, 24);
        char[] c = new char[n];
        for (int i = 0; i < n; i++) c[i] = IdChars[rng.Next(IdChars.Length)];
        return new string(c);
    }

    static string Text(bool tabs)
    {
        string s = "";
        int n = rng.Next(0, 20);
        for (int i = 0; i < n; i++)
        {
            string o = rng.Next(3) == 0 ? Odd[rng.Next(Odd.Length)] : IdChars[rng.Next(IdChars.Length)].ToString();
            if (o == "\t" && !tabs) o = " ";
            s += o;
        }
        return s;
    }

    static RWPlayer RandomPlayer(int i)
    {
        RWPlayer p = new RWPlayer();
        // A kept GUID, or the host's fallbacks (RWRun.KeyOf): a Steam name can't hold a tab.
        int kind = rng.Next(3);
        p.Key = kind == 0 ? NewGuid() : kind == 1 ? "name:" + Text(false) : "session:" + rng.Next(0, 1000000);
        p.Name = Text(true);
        p.Hero = rng.Next(4) == 0 ? "" : "Hero_" + Id();
        p.Pure = rng.Next(0, 1000000);
        int d = rng.Next(0, 9);
        for (int k = 0; k < d; k++) p.Directions.Add(Id());
        int t = rng.Next(0, 7);
        for (int k = 0; k < t; k++) p.Trinkets.Add(Id() + (k + 1));
        p.Owed = rng.Next(2) == 0;
        p.StormLeft = rng.Next(0, RWTrinkets.StormTides + 1);
        p.StormsDone = rng.Next(0, RWCardinals.Stacks + 1);
        return p;
    }

    static string NewGuid()
    {
        const string hex = "0123456789abcdef";
        char[] c = new char[36];
        for (int i = 0; i < 36; i++) c[i] = i == 8 || i == 13 || i == 18 || i == 23 ? '-' : hex[rng.Next(16)];
        return new string(c);
    }

    // A damaged player line: fields dropped, added, emptied or filled with text; numbers out of range.
    static string Garbage(int i)
    {
        int n = rng.Next(0, 10);
        string[] f = new string[n];
        for (int k = 0; k < n; k++)
        {
            int m = rng.Next(8);
            f[k] = m == 0 ? "" : m == 1 ? "-" + rng.Next(1, 100000) : m == 2 ? ",,," : m == 3 ? DamagedList() : m == 4 ? "99999999999999" : Text(false);
        }
        return string.Join("\t", f);
    }

    static string DamagedList()
    {
        string[] parts = new string[rng.Next(0, 6)];
        for (int k = 0; k < parts.Length; k++) parts[k] = rng.Next(3) == 0 ? "" : Id() + "#" + rng.Next(1, 7);
        return string.Join(",", parts);
    }

    static string RandomDigits()
    {
        const string chars = "0123456789r-x";
        string s = "";
        int n = rng.Next(0, RWChart.Rows + 4);
        for (int k = 0; k < n; k++) s += rng.Next(5) == 0 ? chars[rng.Next(chars.Length)] : chars[rng.Next(5)];
        return s;
    }

    static void Write(string key, string value) { RWUtil.WriteString(RWData.SaveFolder, key, value); }

    // ---------------------------------------------------------------- comparing

    static string Differs(RWPlayer p, RWPlayer q)
    {
        if (q.Key != p.Key) return "key '" + Show(q.Key) + "'";
        if (q.Name != p.Name.Replace("\t", " ")) return "name '" + Show(q.Name) + "'";
        if (q.Hero != p.Hero) return "vessel '" + q.Hero + "'";
        if (q.Pure != p.Pure) return "Pure Aura " + q.Pure;
        if (string.Join(",", q.Directions.ToArray()) != string.Join(",", p.Directions.ToArray())) return "Directions";
        if (string.Join(",", q.Trinkets.ToArray()) != string.Join(",", p.Trinkets.ToArray())) return "Trinkets";
        if (q.Owed != p.Owed) return "Owed";
        if (q.StormLeft != p.StormLeft || q.StormsDone != p.StormsDone) return "Bottled Storm " + q.StormLeft + "/" + q.StormsDone;
        return "";
    }

    static string Sane(RWPlayer p)
    {
        if (p.Key == null || p.Name == null || p.Hero == null) return "a null field";
        if (p.Pure < 0) return "Pure Aura " + p.Pure;
        foreach (string d in p.Directions) if (d == "") return "an empty Direction id";
        foreach (string t in p.Trinkets) if (t == "") return "an empty Trinket id";
        return "";
    }

    static string Digits(List<int> route)
    {
        string s = "";
        foreach (int c in route) s += c.ToString();
        return s;
    }

    static string Show(string s) { s = s ?? ""; s = s.Replace("\t", "\\t"); return s.Length > 80 ? s.Substring(0, 80) + "..." : s; }
}
#endif
