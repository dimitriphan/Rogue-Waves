// RULES FILE: no engine types. Compiled whole and run here by Tools/cloud-check/rules_check.py (RogueWavesMode/README.md).
// The questions' text and the votes' counting (20_Vote.cs has the header and runs the break): the one writer and the
// one reader of what the host puts on a vessel's RW_Ask, and who wins a vote.
public static partial class RWVote
{
    public const string LeaveKind = "L", DirectionKind = "D", MapKind = "M", TreasureKind = "T";
    public const string RedrawMark = "r";    // Spare Compass's redraw, while the holder has one this map
    public const int RedrawPick = -2;        // the pick that asks for it
    public const string StayMark = "s";      // the map question was asked when the party stayed (B1)
    public const string RowMark = ":";       // ends the row a map question names: the leg after a Cove
}

// A question as the host writes it on a vessel and that player's machine reads it:
//   D:<card>.<card>[.r]         Direction cards by their index in RWDirections.List; "r" offers Spare Compass's redraw
//   M:[<row>:]<col>.<col>[.s]   the next map's columns; a row when it isn't the next one (the leg after a Cove); "s" when
//                               the party stayed and votes ahead
//   T:<card>.<card>             the Wreck's Trinkets by their index in RWCardinals.Sold
//   L                           leave or stay
// A part a build doesn't know is skipped, not refused: that is how a mark was added without breaking an older client,
// so a new mark is a new part, never a change to these.
public class RWQuestion
{
    public string Kind = "";
    public readonly List<int> Numbers = new List<int>();   // the cards or columns, in the order asked
    public int Row = -1;                                    // the row named, or -1
    public bool Redraw, Stay;

    public static string Directions(List<int> cards, bool redraw) { return RWVote.DirectionKind + ":" + Join(cards, redraw ? RWVote.RedrawMark : null); }

    // row: the chart row the columns are on when it isn't the next one, else -1.
    public static string Map(List<int> cols, int row, bool stay)
    {
        return RWVote.MapKind + ":" + (row >= 0 ? row + RWVote.RowMark : "") + Join(cols, stay ? RWVote.StayMark : null);
    }

    public static string Treasure(List<int> cards) { return RWVote.TreasureKind + ":" + Join(cards, null); }

    public static string Leave() { return RWVote.LeaveKind; }

    static string Join(List<int> numbers, string mark)
    {
        List<string> parts = new List<string>();
        foreach (int n in numbers) parts.Add(n.ToString());
        if (mark != null) parts.Add(mark);
        return string.Join(".", parts.ToArray());
    }

    // A question's cards as the log lists them: its parts, marks and all, with commas.
    public static string Listed(string what) { return what.Substring(what.LastIndexOf(':') + 1).Replace(".", ","); }

    public static RWQuestion Read(string what)
    {
        RWQuestion q = new RWQuestion();
        if (string.IsNullOrEmpty(what))
            return q;
        q.Kind = what.Substring(0, 1);
        string body = what.Length > 2 ? what.Substring(2) : "";
        // A map question on the leg after a Cove names its row first ("M:<row>:<cols>").
        int mark = body.IndexOf(RWVote.RowMark, StringComparison.Ordinal);
        if (mark > 0 && int.TryParse(body.Substring(0, mark), out q.Row)) body = body.Substring(mark + 1);
        else q.Row = -1;
        string[] parts = body.Length > 0 ? body.Split('.') : new string[0];
        foreach (string p in parts)
        {
            int n;
            if (p == RWVote.RedrawMark && q.Kind == RWVote.DirectionKind) q.Redraw = true;
            else if (p == RWVote.StayMark && q.Kind == RWVote.MapKind) q.Stay = true;
            else if (int.TryParse(p, out n)) q.Numbers.Add(n);
        }
        return q;
    }
}

// Who wins a vote, and which card a pick is (docs/scope/04 [Tides and Leaving], [Sea Chart]).
public static class RWTally
{
    // The map vote: the column with the most votes, the first of the choices on a tie or when nobody answered; a vote
    // that is no number or no choice counts for nothing. Returns the winner's place among the choices.
    public static int MapWinner(IEnumerable<string> votes, List<int> cols, out int top)
    {
        Dictionary<int, int> count = new Dictionary<int, int>();
        foreach (string v in votes)
        {
            int c;
            if (int.TryParse(v, out c))
            {
                int have;
                count.TryGetValue(c, out have);
                count[c] = have + 1;
            }
        }
        int best = 0;
        top = 0;
        for (int i = 0; i < cols.Count; i++)
        {
            int have;
            count.TryGetValue(cols[i], out have);
            if (have > top) { top = have; best = i; }
        }
        return best;
    }

    // The vote to leave: how many of the players in the map said "Leave"; all is how many players there are.
    public static int ToLeave(Dictionary<long, string> votes, List<long> players, out int all)
    {
        int yes = 0;
        all = 0;
        foreach (long s in players)
        {
            all++;
            string v;
            if (votes.TryGetValue(s, out v) && v == "Leave") yes++;
        }
        return yes;
    }

    // A majority leaves; a half, or nobody answering, stays.
    public static bool Leaves(int yes, int all) { return yes * 2 > all; }

    // The card a Direction pick is, of 'count' offered: the first when time ran out (pick null) or the pick is no card.
    public static int Card(string pick, int count)
    {
        int k = -1;
        if (pick != null) int.TryParse(pick, out k);
        if (k < 0 || k >= count) k = 0;
        return k;
    }

    // The Wreck's pick: a vote is the Trinket's index in 'sold'; the card it is among the 'offer', the first when time
    // ran out or the pick is no Trinket of the offer.
    public static int TreasureCard(string pick, List<string> offer, List<string> sold)
    {
        int k;
        return pick != null && int.TryParse(pick, out k) && k >= 0 && k < sold.Count ? Mathf.Max(0, offer.IndexOf(sold[k])) : 0;
    }
}
