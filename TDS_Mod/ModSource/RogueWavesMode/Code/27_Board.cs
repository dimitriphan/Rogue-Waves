// The MVP board (docs/mode/MVP_BOARD.md; Dimitri 2026-11-06: "just the most notable thing of the player, balanced so that
// getting a failure is as common as getting an accolade"): after a won map's vote to leave, every player's card with the
// one title that stands out most for them, an accolade or a failure, and one MVP. Two or more players only.
//   host       snapshots each player's match statistics (Tds_Clientinfo.Activeplayerdata, kit docs/16 §5) at the map's start
//              and boards the difference; keeps the tallies the game doesn't (leaks, unanswered votes, drops, aura sent);
//              hands out the titles (Give, a pure function the self-test calls) and writes the board as five None
//              commands on every vessel (RW_Log0-3 a card each, RW_Log4 the header; Goalvalue the board's number)
//   every one  sees a new number on its own vessel's RW_Log4 and draws the board (RWBoardView); Continue is an order
//   RWVote     holds the break for it (State "Board") between the vote to leave and the Direction picks; after map 10 the
//              Voyage board (the run's totals) follows, then the stock win path
// A won map that ends in a bonus Tide gets its board late, over the end screen, and the campaign's last lost map gets
// the Voyage board there (RWCampaign); nothing holds the flow for those.
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public static class RWBoard
{
    // Stat ids: a card's numbers and the run's totals (RWPlayer.Run, saved) share them. Hoard is only ever a map's.
    public const int Kills = 0, Built = 1, Gained = 2, Spent = 3, Drowned = 4, Lost = 5, Sold = 6, Leaks = 7, Missed = 8,
        Drops = 9, Sent = 10, WinVotes = 11, PureSpent = 12, Mvps = 13, Hoard = 14, Maps = 15, Watched = 16, Stats = 17;
    // Watched: the crew's leaks on the maps a player sailed, charged or not (the Voyage caption's count).
    public const float Seconds = 12f;            // ~12 s~ or until everyone presses Continue (§1)
    public const string Kind = "B";              // the Continue order
    public const string MapBoard = "M", VoyageBoard = "V";
    public const string NextPicks = "P", NextVoyage = "V", NextBoat = "B", NextEnd = "E";
    public const int Cards = 4, Lines = Cards + 1;   // the stock Steam lobby holds four (kit docs/19 §1)
    public const string LinkPrefix = "RW_Log";
    public const string Deckhand = "DH", Spotless = "SP";

    public class Title
    {
        public string Code, Name, Line;
        public bool Good, Map, Voyage;
        public int Stat;
        public Title(string code, string name, bool good, int stat, bool map, bool voyage, string line)
        {
            Code = code; Name = name; Good = good; Stat = stat; Map = map; Voyage = voyage; Line = line;
        }
    }

    // §3: "{0}" is the title's own count. The order is the tie-break's (§4).
    public static readonly List<Title> Titles = new List<Title>
    {
        new Title("TB", "Tidebreaker", true, Kills, true, true, "Sent {0} foes to the bottom."),
        new Title("SW", "Shipwright", true, Built, true, true, "Built half the harbour. Upgraded the other half."),
        new Title("TR", "Treasurer", true, Gained, true, true, "Brought in {0} aura. Kept the lights on."),
        new Title("QM", "Quartermaster", true, Spent, true, true, "Every coin went to work."),
        new Title("US", "Unsinkable", true, Drowned, true, true, "Never touched the seabed."),
        new Title("OH", "Open Hand", true, Sent, true, true, "Gave away {0} aura. A saint, or a smuggler."),
        new Title("OS", "Old Salt", true, Mvps, false, true, "MVP of {0} maps. The sea knows the name."),
        new Title("FM", "First Mate", true, WinVotes, false, true, "Always knew where we were going."),
        new Title("PD", "Pearl Diver", true, PureSpent, false, true, "Spent {0} Pure Aura. Never came up empty."),
        new Title(Deckhand, "Deckhand", true, -1, true, true, "Kept the ship afloat. Someone had to."),
        new Title("AN", "Anchor", false, Drowned, true, false, "Spent more time on the seabed than on deck."),
        new Title("BA", "Barnacle", false, Drowned, false, true, "Stuck to the seabed {0} times this voyage."),
        new Title("DW", "Driftwood", false, Lost, true, true, "Their towers keep washing up on the beach."),
        new Title("HO", "Dragon's Hoard", false, Hoard, true, false, "Died rich. Well, sailed rich."),
        new Title("ST", "Second Thoughts", false, Sold, true, true, "Built it. Regretted it. Sold it. Built it again."),
        new Title("LH", "Leaky Hull", false, Leaks, true, true, "Had it on the hook and let it slip."),
        new Title("FL", "Fashionably Late", false, Missed, true, true, "Still reading the first card."),
        new Title("GF", "Gone Fishing", false, Drops, true, true, "Went for bait mid-Tide. Came back for the glory."),
        new Title(Spotless, "Spotless", false, -1, true, true, "Nothing went wrong. Suspicious."),
    };

    public static Title Find(string code) { return Titles.Find(t => t.Code == code); }

    public class Row
    {
        public string Key = "", Name = "", Hero = "", Title = "";
        public long Session = -1;
        public bool Away, Mvp;
        public int Value;
        public readonly int[] V = new int[Stats];
    }

    // ---------------------------------------------------------------- the hand-out (pure: the self-test's MB checks)

    // A title's count for a player; Unsinkable counts finishing blows + 1 for a player who never drowned, else 0.
    public static int Count(Title t, Row r)
    {
        if (t.Stat < 0) return 0;
        if (t.Code == "US") return r.V[Drowned] == 0 ? r.V[Kills] + 1 : 0;
        return Math.Max(0, r.V[t.Stat]);
    }

    static bool On(Title t, bool voyage) { return voyage ? t.Voyage : t.Map; }

    // How far a player's count stands above an even share of the crew's total; -1 when it doesn't stand out at all.
    static float Note(Title t, Row r, Dictionary<Title, int> totals, int n)
    {
        int v = Count(t, r), total;
        if (v <= 0 || !totals.TryGetValue(t, out total) || total <= 0)
            return -1f;
        float note = (float)v / total - 1f / n;
        return note > 0f ? note : -1f;
    }

    // How many of n players take a failure: half; with an odd crew, one more on every other board (parity), so over the
    // run a failure is exactly as common as an accolade.
    public static int Failures(int n, int parity) { return n / 2 + (n % 2 == 1 && (parity & 1) == 1 ? 1 : 0); }

    // The parity for a board of n: an odd board takes the next of a count kept over the boards given (map and Voyage),
    // so odd boards alternate even when the crew's size changes between maps. Not saved: a Continue starts it again,
    // which shifts the balance by one board at most.
    static int oddBoards;
    static int Parity(int n) { return n % 2 == 1 ? oddBoards++ : 0; }

    // §4: each player gets the one title that stands out most for them. The players whose best failure stands out most
    // past their best accolade take the failures, as many as Failures says; within each side the largest lead takes its
    // title first, one player a title; a player left with none gets Deckhand or Spotless. The MVP: the largest summed share
    // of the accolades' counts. Every tie ends on the key, so every order of rows gives the same board.
    public static void Give(List<Row> rows, bool voyage, int parity)
    {
        int n = rows.Count;
        foreach (Row r in rows) { r.Title = ""; r.Value = 0; r.Mvp = false; }
        if (n == 0)
            return;
        List<Title> good = new List<Title>(), bad = new List<Title>();
        Dictionary<Title, int> totals = new Dictionary<Title, int>();
        foreach (Title t in Titles)
        {
            if (!On(t, voyage) || t.Stat < 0) continue;
            (t.Good ? good : bad).Add(t);
            int total = 0;
            foreach (Row r in rows) total += Count(t, r);
            totals[t] = total;
        }
        Dictionary<Row, float> lean = new Dictionary<Row, float>();
        foreach (Row r in rows)
            lean[r] = Best(bad, r, totals, n) - Best(good, r, totals, n);
        List<Row> order = new List<Row>(rows);
        order.Sort((a, b) =>
        {
            int c = lean[b].CompareTo(lean[a]);
            return c != 0 ? c : string.CompareOrdinal(a.Key, b.Key);
        });
        int failures = Math.Min(n, Failures(n, parity));
        Assign(order.GetRange(0, failures), bad, totals, n, Spotless);
        Assign(order.GetRange(failures, n - failures), good, totals, n, Deckhand);
        // The MVP: Unsinkable's count is finishing blows again, so it isn't summed twice.
        Row mvp = null;
        float top = 0f;
        List<Row> byKey = new List<Row>(rows);
        byKey.Sort((a, b) => string.CompareOrdinal(a.Key, b.Key));
        foreach (Row r in byKey)
        {
            float score = 0f;
            foreach (Title t in good)
                if (t.Code != "US" && totals[t] > 0) score += (float)Count(t, r) / totals[t];
            bool better = mvp == null ? score > 0f : score > top + 1e-6f
                || (Mathf.Abs(score - top) <= 1e-6f && (r.V[Kills] > mvp.V[Kills] || (r.V[Kills] == mvp.V[Kills] && r.V[Gained] > mvp.V[Gained])));
            if (better) { mvp = r; top = score; }
        }
        if (mvp != null) mvp.Mvp = true;
    }

    static float Best(List<Title> titles, Row r, Dictionary<Title, int> totals, int n)
    {
        float best = -1f;
        foreach (Title t in titles) best = Mathf.Max(best, Note(t, r, totals, n));
        return best;
    }

    static void Assign(List<Row> side, List<Title> titles, Dictionary<Title, int> totals, int n, string fallback)
    {
        List<KeyValuePair<Title, Row>> pairs = new List<KeyValuePair<Title, Row>>();
        foreach (Title t in titles)
            foreach (Row r in side)
                if (Note(t, r, totals, n) > 0f) pairs.Add(new KeyValuePair<Title, Row>(t, r));
        pairs.Sort((a, b) =>
        {
            int c = Note(b.Key, b.Value, totals, n).CompareTo(Note(a.Key, a.Value, totals, n));
            if (c != 0) return c;
            c = Count(b.Key, b.Value).CompareTo(Count(a.Key, a.Value));
            if (c != 0) return c;
            c = Titles.IndexOf(a.Key).CompareTo(Titles.IndexOf(b.Key));
            return c != 0 ? c : string.CompareOrdinal(a.Value.Key, b.Value.Key);
        });
        List<Title> used = new List<Title>();
        foreach (KeyValuePair<Title, Row> p in pairs)
        {
            if (used.Contains(p.Key) || p.Value.Title != "") continue;
            used.Add(p.Key);
            p.Value.Title = p.Key.Code;
            p.Value.Value = p.Key.Code == "US" ? 0 : Count(p.Key, p.Value);
        }
        foreach (Row r in side)
            if (r.Title == "") r.Title = fallback;
    }

    // ---------------------------------------------------------------- the lines on the vessels (61 bytes each, kit docs/20 §3.1)

    public const int MaxBytes = 61;

    // A card: kind|title|mvp|away|session|sunk,built,aura,drowned|value|hero|name, the name cut to fit.
    public static string EncodeRow(string kind, Row r)
    {
        string hero = r.Hero.StartsWith("Hero_") ? r.Hero.Substring(5) : r.Hero;
        string head = kind + "|" + r.Title + "|" + (r.Mvp ? 1 : 0) + "|" + (r.Away ? 1 : 0) + "|" + r.Session + "|" + r.V[Kills] + "," + r.V[Built] + ","
            + r.V[Gained] + "," + r.V[Drowned] + "|" + r.Value + "|" + Clean(hero) + "|";
        return head + Fit(Clean(r.Name), MaxBytes - Encoding.UTF8.GetByteCount(head));
    }

    public static Row DecodeRow(string line, out string kind)
    {
        kind = "";
        string[] f = (line ?? "").Split('|');
        if (f.Length < 9)
            return null;
        Row r = new Row();
        kind = f[0];
        r.Title = f[1];
        r.Mvp = f[2] == "1";
        r.Away = f[3] == "1";
        long.TryParse(f[4], out r.Session);
        string[] n = f[5].Split(',');
        if (n.Length == 4)
        {
            int.TryParse(n[0], out r.V[Kills]);
            int.TryParse(n[1], out r.V[Built]);
            int.TryParse(n[2], out r.V[Gained]);
            int.TryParse(n[3], out r.V[Drowned]);
        }
        int.TryParse(f[6], out r.Value);
        r.Hero = f[7] == "" ? "" : "Hero_" + f[7];
        r.Name = f[8];
        return r;
    }

    // The header: kind|map|tide|retries|leaks|next|map name.
    public static string EncodeHead(string kind, int map, int tide, int retries, int leaks, string next, string name)
    {
        string head = kind + "|" + map + "|" + tide + "|" + retries + "|" + leaks + "|" + next + "|";
        return head + Fit(Clean(name), MaxBytes - Encoding.UTF8.GetByteCount(head));
    }

    static string Clean(string s) { return (s ?? "").Replace("|", "/").Replace("\t", " "); }

    // Cut to a byte count on a whole character (a Steam name may hold any script).
    public static string Fit(string s, int bytes)
    {
        if (bytes <= 0) return "";
        if (Encoding.UTF8.GetByteCount(s) <= bytes) return s;
        StringBuilder b = new StringBuilder();
        int used = 0;
        for (int i = 0; i < s.Length; i++)
        {
            int len = char.IsHighSurrogate(s[i]) && i + 1 < s.Length ? 2 : 1;
            int cost = Encoding.UTF8.GetByteCount(s.Substring(i, len));
            if (used + cost > bytes) break;
            b.Append(s, i, len);
            used += cost;
            i += len - 1;
        }
        return b.ToString();
    }

    // ---------------------------------------------------------------- the host's tallies

    public static readonly string[] Text = new string[Lines];   // what every vessel carries now ("" none)
    public static float Round;                                  // the board's number, on every line's Goalvalue
    public static bool ShownThisMap;
    public static bool InMap;                                   // a campaign map's start was seen, and no other map since
    public static int TeamLeaks;
    public static List<Row> LastRows = new List<Row>();         // the last board given (self-test)
    static readonly Dictionary<string, int[]> baseline = new Dictionary<string, int[]>();
    static readonly List<Entity_Master> leaked = new List<Entity_Master>();    // no HashSet: the game's compiler lacks System.Core
    static readonly Entity_Master.Oninteracts onCore = OnCore;
    static Entity_Master core;
    static float nextHook;

    public static string Link(int i) { return LinkPrefix + i; }

    // RWVote.MapStart, host, every map: no board yet; a campaign map snapshots everyone's numbers.
    public static void MapStart()
    {
        Clear();    // every vessel's lines too: a board written after the last map was gone never reached a screen
        ShownThisMap = false;
        TeamLeaks = 0;
        leaked.Clear();
        baseline.Clear();
        core = null;
        foreach (RWPlayer p in RWRun.Players.Values) { p.Leaks = p.Missed = p.Drops = 0; p.SentMap = 0f; }    // the last map's votes after its board stay there
        InMap = RWRun.InCampaignMap;
        if (!InMap)
            return;
        foreach (long s in RWRun.Sessions())
            baseline[RWRun.KeyOf(s)] = Read(RWRun.Info(s) != null ? RWRun.Info(s).Activeplayerdata : null);
    }

    // The game's own numbers for a player, as stat ids (Spent and Gained summed over the aura colours).
    public static int[] Read(Tds_Playerdatatojson d)
    {
        int[] v = new int[Stats];
        if (d == null)
            return v;
        v[Kills] = d.Kills;
        v[Built] = d.Placed + d.Upgraded;
        v[Drowned] = d.Vesseldeaths;
        v[Lost] = d.Deaths;
        v[Sold] = d.Sold;
        v[Spent] = Sum(d.Spent);
        v[Gained] = Sum(d.Gained);
        return v;
    }

    static int Sum(List<Link_Resourceset> list)
    {
        float s = 0f;
        if (list != null) foreach (Link_Resourceset r in list) s += r.Amount;
        return Mathf.RoundToInt(s);
    }

    // Host, every frame of a campaign map: listens on the Basin for foes that reach it (Leaky Hull). The game rebuilds a
    // thing's hit chain when an upgrade lands, so the handler goes back in front a few times a second (ModKitUtil.Front).
    public static void Update()
    {
        if (!RWRun.InCampaignMap || !ModKitUtil.IsServer || Time.time < nextHook)
            return;
        nextHook = Time.time + 0.25f;
        Entity_Master c = RWUtil.PlayerCore();
        if (c == null)
            return;
        if (c != core && core != null) ModKitUtil.Unfront(core, onCore);
        core = c;
        ModKitUtil.Front(core, onCore);
    }

    // A foe's hit on the Basin: one leak a foe, charged to its last hitter's player; nobody's when no player's thing hit it.
    static void OnCore(Link_Interact hit)
    {
        try
        {
            if (hit == null || hit.Type != "Hurt" || hit.From == null || leaked.Contains(hit.From))
                return;
            List<long> players = RWRun.Sessions();
            if (players.Contains(ModKitUtil.Owner(hit.From)))
                return;    // a player's own thing (a stray shot) is no leak
            leaked.Add(hit.From);
            TeamLeaks++;
            Entity_Master by = hit.From.Lasthit;
            long owner = by != null ? ModKitUtil.Owner(by) : -1;
            if (players.Contains(owner)) RWRun.For(owner).Leaks++;
        }
        catch (Exception e) { RWLog.Once("leaks", "leak hook: " + e.Message); }
    }

    // Tallies the votes a question closed with: who didn't answer, and who voted with the winner (null: no winner).
    public static void Votes(ICollection<long> asked, Dictionary<long, string> votes, string winner)
    {
        foreach (long s in asked)
        {
            string v;
            RWPlayer p = RWRun.For(s);
            if (!votes.TryGetValue(s, out v) || v == null) { p.Missed++; continue; }
            if (winner != null && v == winner) p.Run[WinVotes]++;
        }
    }

    // A rejoin brought someone back (RWRejoin): Gone Fishing.
    public static void Back(long session)
    {
        if (RWRun.InCampaignMap) RWRun.For(session).Drops++;
    }

    // ---------------------------------------------------------------- the boards (host)

    // This map's cards: everyone in the map, a player still away too (the rejoin keeps their numbers).
    public static List<Row> MapRows()
    {
        List<Row> rows = new List<Row>();
        List<long> here = RWRun.Sessions();
        foreach (long s in here)
        {
            Tds_Clientinfo info = RWRun.Info(s);
            rows.Add(MapRow(s, RWRun.For(s), info != null ? info.Activeplayerdata : null, info, false));
        }
        foreach (RWRejoin.Seat seat in RWRejoin.Seats.Values)
        {
            if (!seat.Away || here.Contains(seat.Session)) continue;
            RWPlayer p;
            RWRun.Players.TryGetValue(RWRun.KeyOf(seat.Session), out p);
            Row r = MapRow(seat.Session, p, seat.Stats, null, true);
            if (r.Name == "") r.Name = seat.Name;
            if (r.Hero == "") r.Hero = seat.Hero;
            rows.Add(r);
        }
        return rows;
    }

    static Row MapRow(long session, RWPlayer p, Tds_Playerdatatojson data, Tds_Clientinfo info, bool away)
    {
        Row r = new Row { Session = session, Away = away };
        r.Key = p != null ? p.Key : RWRun.KeyOf(session);
        r.Name = p != null ? p.Name : "";
        r.Hero = p != null ? p.Hero : "";
        int[] now = Read(data), was;
        if (!baseline.TryGetValue(r.Key, out was)) was = new int[Stats];
        // A count lower than at the start was reset since (Activeplayer passed through 0): the count is all this map's.
        for (int i = 0; i < Stats; i++) r.V[i] = now[i] >= was[i] ? now[i] - was[i] : now[i];
        if (p != null)
        {
            r.V[Leaks] = p.Leaks;
            r.V[Missed] = p.Missed;
            r.V[Drops] = p.Drops;
            r.V[Sent] = Mathf.RoundToInt(p.SentMap);
        }
        if (info != null && info.Resources != null)
        {
            float wallet = 0f;
            foreach (Link_Resource res in info.Resources) wallet += res.Amount;
            r.V[Hoard] = Mathf.RoundToInt(wallet);
        }
        return r;
    }

    // After a won map, once: adds the map to the run's totals and hands out its titles (two or more players). Null when
    // this map was taken already; the rows otherwise, four at most (the lobby's limit; an away seat gives way first).
    public static List<Row> TakeMap()
    {
        if (ShownThisMap || !RWCampaign.Live || !InMap)
            return null;
        ShownThisMap = true;
        List<Row> rows = MapRows();
        foreach (Row r in rows)
        {
            RWPlayer p;
            if (!RWRun.Players.TryGetValue(r.Key, out p)) continue;
            for (int i = 0; i < Stats; i++) if (i != Hoard && i != WinVotes && i != PureSpent && i != Mvps && i != Maps && i != Watched) p.Run[i] += r.V[i];
            p.Run[Maps]++;
            p.Run[Watched] += TeamLeaks;
            p.Leaks = p.Missed = p.Drops = 0;
            p.SentMap = 0f;
        }
        if (rows.Count > Cards)
        {
            rows.Sort((a, b) => a.Away != b.Away ? (a.Away ? 1 : -1) : string.CompareOrdinal(a.Key, b.Key));
            rows.RemoveRange(Cards, rows.Count - Cards);
        }
        if (rows.Count < 2)
            return rows;
        Give(rows, false, Parity(rows.Count));
        foreach (Row r in rows)
        {
            RWPlayer p;
            if (r.Mvp && RWRun.Players.TryGetValue(r.Key, out p)) p.Run[Mvps]++;
        }
        return rows;
    }

    // After a won map (the vote to leave, or the map's end in a bonus Tide): takes the map and writes its board. False
    // with fewer than two players (no board alone, §1) or once this map has had its board.
    public static bool ShowMap(string next)
    {
        List<Row> rows = TakeMap();
        if (rows == null || rows.Count < 2)
            return false;
        RWChart.Node won = RWCampaign.Here;
        Write(MapBoard, rows, RWCampaign.Route.Count, Game_Mapvars.Instance != null ? Game_Mapvars.Instance.Wave.Value : 0, RWCampaign.Retries, TeamLeaks, next,
            won != null ? RWVote.MapName(won) : "");
        return true;
    }

    // The campaign's end: every player who sailed a map, on the run's totals.
    public static bool ShowVoyage(string next)
    {
        List<Row> rows = new List<Row>();
        List<long> here = RWRun.Sessions();
        foreach (long s in here) RWRun.For(s);    // after a lost map's Load the players' sessions are unknown again
        foreach (RWPlayer p in RWRun.Players.Values)
        {
            if (p.Run[Maps] <= 0) continue;
            Row r = new Row { Key = p.Key, Name = p.Name, Hero = p.Hero, Session = p.Session, Away = !here.Contains(p.Session) };
            for (int i = 0; i < Stats; i++) r.V[i] = p.Run[i];
            r.V[Hoard] = 0;
            rows.Add(r);
        }
        if (rows.Count < 2)
            return false;
        // Four cards at most: those who sailed the most maps, then by key.
        rows.Sort((a, b) => { int c = b.V[Maps].CompareTo(a.V[Maps]); return c != 0 ? c : string.CompareOrdinal(a.Key, b.Key); });
        if (rows.Count > Cards) rows.RemoveRange(Cards, rows.Count - Cards);
        Give(rows, true, Parity(rows.Count));
        int leaks = 0;
        foreach (RWPlayer p in RWRun.Players.Values) leaks = Math.Max(leaks, p.Run[Watched]);    // the one who sailed the most maps saw the most
        Write(VoyageBoard, rows, RWCampaign.Route.Count, Game_Mapvars.Instance != null ? Game_Mapvars.Instance.Wave.Value : 0, RWCampaign.Retries, leaks, next, "");
        return true;
    }

    static void Write(string kind, List<Row> rows, int map, int tide, int retries, int leaks, string next, string name)
    {
        rows.Sort((a, b) => string.CompareOrdinal(a.Key, b.Key));
        Round++;
        LastRows = rows;
        for (int i = 0; i < Cards; i++) Text[i] = i < rows.Count ? EncodeRow(kind, rows[i]) : "";
        Text[Cards] = EncodeHead(kind, map, tide, retries, leaks, next, name);
        string said = "";
        foreach (Row r in rows) said += (said == "" ? "" : "; ") + r.Name + " " + r.Title + (r.Mvp ? " MVP" : "") + " [" + string.Join(",", Array.ConvertAll(r.V, x => x.ToString())) + "]";
        RWLog.Info((kind == MapBoard ? "map board " : "voyage board ") + Round + ": " + said);
        RWRun.WriteLog();
    }

    // The board is over: every vessel's lines go blank, and every machine closes it.
    public static void Clear()
    {
        bool any = false;
        foreach (string t in Text) if (t != "") any = true;
        if (!any)
            return;
        for (int i = 0; i < Lines; i++) Text[i] = "";
        RWRun.WriteLog();
    }

    // ---------------------------------------------------------------- the title icons (12 x 12, temporary art, docs/TEMP_ART.md)
    // The rows are concept-tools/mvp_board_look.py's, coloured like the chart's icons (no Storm Coral: the MVP banner is the
    // view's one coral piece).

    static readonly Dictionary<char, Color32> palette = new Dictionary<char, Color32>
    {
        { 'w', new Color32(0xF2, 0xF4, 0xF3, 255) }, { 'g', new Color32(0xA9, 0xB1, 0xB8, 255) }, { 'G', new Color32(0x7D, 0x86, 0x8F, 255) },
        { 'o', new Color32(0xF0, 0x9A, 0x3E, 255) }, { 'y', new Color32(0xF6, 0xD8, 0x6B, 255) }, { 'b', new Color32(0x7A, 0x4E, 0x33, 255) },
        { 'B', new Color32(0xA8, 0x72, 0x48, 255) }, { 's', new Color32(0xD8, 0xC4, 0x9A, 255) }, { 'l', new Color32(0x9C, 0xD3, 0xDB, 255) },
        { 'p', new Color32(0xF4, 0xEE, 0xF8, 255) }, { 'n', new Color32(0x93, 0xCC, 0xA1, 255) },
    };

    static readonly Dictionary<string, string[]> art = new Dictionary<string, string[]>
    {
        { "TB", new[] { "............", ".....llll...", "...llwwwwl..", "..lwwl..lwl.", ".lwl.....wl.", ".lw...l..l..", "lwl..lwl....", "lw..lwwwl..l", "lwllwwwwwllw", "lwwwwwwwwwww", "llllllllllll", "............" } },
        { "SW", new[] { "............", "..gggggg....", ".gwwwwwwgG..", ".gggggggGG..", "..GGGGGGG...", ".....bB.....", ".....bB.....", ".....bB.....", ".....bB.....", ".....bB.....", ".....bb.....", "............" } },
        { "TR", new[] { "............", "....bbbb....", ".....bb.....", "...bBBBBb...", "..bBBBBBBb..", ".bBBByyBBBb.", ".bBByBBBBBb.", ".bBBByyBBBb.", ".bBBBBByBBb.", ".bBBByyBBBb.", "..bbbbbbbb..", "............" } },
        { "QM", new[] { "............", "..bbbbbbbb..", ".bBBBBBBBBb.", ".bBbBBBBbBb.", ".bBBbBBbBBb.", ".bBBBbbBBBb.", ".bBBBbbBBBb.", ".bBBbBBbBBb.", ".bBbBBBBbBb.", ".bBBBBBBBBb.", "..bbbbbbbb..", "............" } },
        { "US", new[] { "............", "....owwo....", "..ooowwooo..", ".ooo....ooo.", ".oo......oo.", "ww........ww", "ww........ww", ".oo......oo.", ".ooo....ooo.", "..ooowwooo..", "....owwo....", "............" } },
        { "OH", new[] { "............", ".....yy.....", "....yyyy....", ".....yy.....", "............", "s..........s", "ss........ss", "sss......sss", ".ssss..ssss.", "..ssssssss..", "...ssssss...", "............" } },
        { "OS", new[] { "............", "...wwwwww...", "..wGGGGGGw..", ".wGGGGGGGGw.", "wwwwwwwwwwww", "...ssssss...", "..ssGssGss..", "..ssssssss..", "..sssGGsss..", "...ssssss...", "....ssss....", "............" } },
        { "FM", new[] { "............", "....gggg....", "..gg.ww.gg..", ".g...ww...g.", ".g...ww...g.", "g....oo....g", "g....oo....g", ".g...oo...g.", ".g........g.", "..gg....gg..", "....gggg....", "............" } },
        { "PD", new[] { "............", "............", "....pppp....", "...pppppp...", "..pppwwppp..", "..ppwwpppp..", "..pppppppp..", "..pppppppp..", "...pppppp...", "....pppp....", "............", "............" } },
        { "DH", new[] { "......b.....", "......B.....", ".....b......", ".....B......", "....b.......", "....B.......", "...b........", "..wwww......", ".wwwwww.....", "wwwwwwww....", ".w.w.w.w....", "............" } },
        { "AN", new[] { "....gggg....", "....g..G....", "....ggGG....", ".....gG.....", "...ggggGG...", ".....gG.....", ".....gG.....", "g....gG....G", "gg...gG...GG", ".gg..gG..GG.", "..ggg..GGG..", "....gGGG...." } },
        { "BA", new[] { "............", "....gggg....", "...gGGGGg...", "..gGwwwwGg..", "..gGw..wGg..", "..gGwwwwGg..", ".ggggggggg..", ".gGGgGGgGGg.", "gGGgGGgGGgGg", "gggggggggggg", "............", "............" } },
        { "DW", new[] { "............", "............", "............", ".bbbbbbbbbb.", "bBBBBBBBBBBb", "bBBbBBBBbBBb", "bBBBBBbBBBBb", ".bbbbbbbbbb.", "............", "..l..ll..l..", ".llllllllll.", "............" } },
        { "HO", new[] { "............", "...y.yy.y...", "..yyyyyyyy..", ".bbbbbbbbbb.", ".bBBBBBBBBb.", ".bbbbyybbbb.", ".bBBByyBBBb.", ".bBBBBBBBBb.", ".bBBBBBBBBb.", ".bbbbbbbbbb.", "............", "............" } },
        { "ST", new[] { "............", "....wwww....", "...ww..ww...", "........ww..", ".......ww...", "......ww....", ".....ww.....", ".....ww.....", "............", ".....ww.....", ".....ww.....", "............" } },
        { "LH", new[] { "............", ".gggggggggg.", ".gGGGGGGGGg.", "..gGGGGGGg..", "..gGGGGGGg..", "..gGGGGGGg..", "...gggggg...", "......l.....", "......l.....", ".....lll....", "......l.....", "............" } },
        { "FL", new[] { "............", "..bbbbbbbb..", "...g....g...", "...gssssg...", "....gssg....", ".....gg.....", ".....gg.....", "....gssg....", "...gssssg...", "..bbbbbbbb..", "............", "............" } },
        { "GF", new[] { "..........b.", ".........bB.", "........bB.w", ".......bB..w", "......bB...w", ".....bB....w", "....bB.....w", "...bB......w", "..bB.......o", ".bB.........", "bb..........", "............" } },
        { "SP", new[] { "............", ".....w......", ".....w......", "...wwwww....", ".....w....w.", ".....w...www", "..........w.", "...w........", "..www.......", "...w........", "............", "............" } },
    };

    static readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();

    public static Sprite Icon(string code)
    {
        Sprite s;
        if (sprites.TryGetValue(code, out s) && s != null)
            return s;
        string[] rows;
        if (!art.TryGetValue(code, out rows))
            return null;
        s = ModKitAssets.Pixel(rows, "RW_Board_" + code, palette);
        sprites[code] = s;
        return s;
    }
}
