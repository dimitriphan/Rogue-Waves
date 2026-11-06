#if ROGUEWAVESMODE_SELFTEST
// The MVP board's boot checks (docs/mode/MVP_BOARD.md §4, 27_Board.cs), pure data, no map:
//   MB1  two players: one title each, one accolade and one failure, every order of rows the same board
//   MB2  three and four players: the failures are half (an odd crew alternates), titles never repeat on a board
//   MB3  a thousand made-up boards of two to four players: failures exactly as common as accolades, one MVP each
//   MB4  nothing stands out (all even, or all 0): Deckhand and Spotless
//   MB5  the MVP: the largest summed share; a tie goes to finishing blows, then aura gained, then the key
//   MB6  a card and the header fit 61 bytes with a long name in any script, and read back
//   MB7  a save line keeps the run's totals; an older line reads as none
//   MB8  every title has a 12 x 12 icon and a line
using System;
using System.Collections.Generic;
using UnityEngine;

public static partial class RWTest
{
    static RWBoard.Row BoardRow(string key, params int[] pairs)
    {
        RWBoard.Row r = new RWBoard.Row { Key = key, Name = key, Hero = "Hero_Yossele", Session = key.GetHashCode() & 0xff };
        for (int i = 0; i + 1 < pairs.Length; i += 2) r.V[pairs[i]] = pairs[i + 1];
        return r;
    }

    static string BoardSays(List<RWBoard.Row> rows)
    {
        List<RWBoard.Row> sorted = new List<RWBoard.Row>(rows);
        sorted.Sort((a, b) => string.CompareOrdinal(a.Key, b.Key));
        List<string> s = new List<string>();
        foreach (RWBoard.Row r in sorted) s.Add(r.Key + ":" + r.Title + (r.Mvp ? "*" : ""));
        return string.Join(" ", s.ToArray());
    }

    static int BoardFailures(List<RWBoard.Row> rows)
    {
        int n = 0;
        foreach (RWBoard.Row r in rows) { RWBoard.Title t = RWBoard.Find(r.Title); if (t != null && !t.Good) n++; }
        return n;
    }

    static void BoardChecks()
    {
        // MB1: A sank most and drowned twice; B built most and never drowned.
        List<RWBoard.Row> two = new List<RWBoard.Row>
        {
            BoardRow("a", RWBoard.Kills, 200, RWBoard.Built, 10, RWBoard.Gained, 900, RWBoard.Drowned, 2),
            BoardRow("b", RWBoard.Kills, 60, RWBoard.Built, 30, RWBoard.Gained, 700, RWBoard.Drowned, 0, RWBoard.Sold, 4),
        };
        RWBoard.Give(two, false, 1);
        string first = BoardSays(two);
        List<RWBoard.Row> flipped = new List<RWBoard.Row> { two[1], two[0] };
        RWBoard.Give(flipped, false, 1);
        Check("MB1 two players: one title each, one accolade and one failure, the same board in any order",
            first == BoardSays(flipped) && BoardFailures(two) == 1 && two[0].Title != "" && two[1].Title != "" && two[0].Title != two[1].Title, first);

        // MB2: three players alternate one and two failures; four take two; no title twice.
        List<RWBoard.Row> three = new List<RWBoard.Row>
        {
            BoardRow("a", RWBoard.Kills, 120, RWBoard.Drowned, 3, RWBoard.Gained, 500),
            BoardRow("b", RWBoard.Kills, 80, RWBoard.Built, 40, RWBoard.Hoard, 400),
            BoardRow("c", RWBoard.Kills, 90, RWBoard.Sent, 300, RWBoard.Missed, 2),
        };
        RWBoard.Give(three, false, 0);
        int evenFails = BoardFailures(three);
        string evenSays = BoardSays(three);
        RWBoard.Give(three, false, 1);
        int oddFails = BoardFailures(three);
        List<RWBoard.Row> four = new List<RWBoard.Row>
        {
            BoardRow("a", RWBoard.Kills, 120, RWBoard.Drowned, 3), BoardRow("b", RWBoard.Built, 40, RWBoard.Hoard, 400),
            BoardRow("c", RWBoard.Sent, 300, RWBoard.Missed, 2), BoardRow("d", RWBoard.Gained, 800, RWBoard.Sold, 6),
        };
        RWBoard.Give(four, false, 3);
        bool unique = true;
        List<string> seen = new List<string>();
        foreach (RWBoard.Row r in four)
        {
            if (r.Title != RWBoard.Deckhand && r.Title != RWBoard.Spotless && seen.Contains(r.Title)) unique = false;
            seen.Add(r.Title);
        }
        Check("MB2 three players take one or two failures in turn, four take two, no title twice",
            evenFails == 1 && oddFails == 2 && BoardFailures(four) == 2 && unique, "three " + evenSays + " / " + BoardSays(three) + "; four " + BoardSays(four));

        // MB3: made-up boards, as random as a crew's numbers.
        System.Random rnd = new System.Random(20261106);
        int accolades = 0, failures = 0, noMvp = 0, boards = 1002, untitled = 0;
        for (int b = 0; b < boards; b++)
        {
            int n = 2 + b * 3 / boards;    // a crew keeps its size through a run: 334 boards each of two, three and four
            List<RWBoard.Row> rows = new List<RWBoard.Row>();
            for (int i = 0; i < n; i++)
            {
                RWBoard.Row r = BoardRow("p" + i);
                foreach (int s in new[] { RWBoard.Kills, RWBoard.Built, RWBoard.Gained, RWBoard.Spent, RWBoard.Lost, RWBoard.Hoard })
                    r.V[s] = rnd.Next(400);
                foreach (int s in new[] { RWBoard.Drowned, RWBoard.Sold, RWBoard.Leaks, RWBoard.Missed, RWBoard.Drops })
                    r.V[s] = rnd.Next(4) == 0 ? rnd.Next(5) : 0;
                r.V[RWBoard.Sent] = rnd.Next(3) == 0 ? rnd.Next(500) : 0;
                rows.Add(r);
            }
            RWBoard.Give(rows, false, b);
            int mvps = 0;
            foreach (RWBoard.Row r in rows)
            {
                RWBoard.Title t = RWBoard.Find(r.Title);
                if (t == null) untitled++;
                else if (t.Good) accolades++;
                else failures++;
                if (r.Mvp) mvps++;
            }
            if (mvps != 1) noMvp++;
        }
        Check("MB3 over " + boards + " boards of two to four players a failure is as common as an accolade, one MVP each",
            untitled == 0 && noMvp == 0 && accolades == failures, accolades + " accolades, " + failures + " failures, " + noMvp + " boards without one MVP, " + untitled + " untitled");

        // MB4: an even crew stands out nowhere.
        List<RWBoard.Row> even = new List<RWBoard.Row> { BoardRow("a", RWBoard.Kills, 50), BoardRow("b", RWBoard.Kills, 50) };
        RWBoard.Give(even, false, 0);
        List<RWBoard.Row> none = new List<RWBoard.Row> { BoardRow("a"), BoardRow("b"), BoardRow("c") };
        RWBoard.Give(none, false, 0);
        bool fallbacks = true;
        foreach (RWBoard.Row r in even) if (r.Title != RWBoard.Deckhand && r.Title != RWBoard.Spotless) fallbacks = false;
        foreach (RWBoard.Row r in none) if (r.Title != RWBoard.Deckhand && r.Title != RWBoard.Spotless) fallbacks = false;
        bool noneMvp = !none[0].Mvp && !none[1].Mvp && !none[2].Mvp;
        Check("MB4 nothing stands out: Deckhand and Spotless, and no MVP when nobody did anything", fallbacks && BoardFailures(even) == 1 && noneMvp, BoardSays(even) + " / " + BoardSays(none));

        // MB5: the MVP, and its ties.
        List<RWBoard.Row> lead = new List<RWBoard.Row> { BoardRow("a", RWBoard.Kills, 10, RWBoard.Gained, 10), BoardRow("b", RWBoard.Kills, 90, RWBoard.Gained, 90) };
        RWBoard.Give(lead, false, 0);
        List<RWBoard.Row> tie = new List<RWBoard.Row> { BoardRow("b"), BoardRow("a") };
        RWBoard.Give(tie, false, 0);    // nobody did anything: no MVP
        List<RWBoard.Row> swap = new List<RWBoard.Row> { BoardRow("a", RWBoard.Kills, 60, RWBoard.Built, 40), BoardRow("b", RWBoard.Kills, 40, RWBoard.Built, 60) };
        RWBoard.Give(swap, false, 0);   // equal shares: more finishing blows wins
        Check("MB5 the MVP has the largest share; a tie goes to finishing blows", lead[1].Mvp && !lead[0].Mvp && swap[0].Mvp && !swap[1].Mvp && !tie[0].Mvp && !tie[1].Mvp,
            BoardSays(lead) + " / " + BoardSays(swap));

        // MB6: 61 bytes.
        RWBoard.Row longName = BoardRow("k", RWBoard.Kills, 123456, RWBoard.Built, 9999, RWBoard.Gained, 999999, RWBoard.Drowned, 99);
        longName.Name = "Капитан Немо の 長い名前 🐙🐙🐙 with a very long tail";
        longName.Title = "TB";
        longName.Value = 123456;
        longName.Mvp = true;
        string line = RWBoard.EncodeRow(RWBoard.MapBoard, longName);
        string kind;
        RWBoard.Row back = RWBoard.DecodeRow(line, out kind);
        string headLine = RWBoard.EncodeHead(RWBoard.MapBoard, 10, 31, 3, 1234, RWBoard.NextPicks, "A map whose name is far too long to fit in sixty-one bytes");
        int bytes = System.Text.Encoding.UTF8.GetByteCount(line), headBytes = System.Text.Encoding.UTF8.GetByteCount(headLine);
        Check("MB6 a card and the header fit " + RWBoard.MaxBytes + " bytes and read back", bytes <= RWBoard.MaxBytes && headBytes <= RWBoard.MaxBytes && back != null && kind == RWBoard.MapBoard
            && back.Title == "TB" && back.Mvp && back.V[RWBoard.Kills] == 123456 && back.V[RWBoard.Gained] == 999999 && back.Hero == "Hero_Yossele" && longName.Name.StartsWith(back.Name) && back.Name.Length > 0,
            bytes + " and " + headBytes + " bytes, name '" + (back != null ? back.Name : "-") + "'");

        // MB7: the save line's ninth field.
        RWPlayer p = new RWPlayer { Key = "k", Name = "N", Hero = "Hero_Spool", Pure = 3 };
        p.Run[RWBoard.Kills] = 77;
        p.Run[RWBoard.Mvps] = 2;
        p.Run[RWBoard.Maps] = 5;
        RWPlayer again = RWRun.Decode(RWRun.Encode(p));
        RWPlayer old = RWRun.Decode("k\tN\tHero_Spool\t3\t\t\t0\t0/0");
        Check("MB7 a save line keeps the run's totals; an older one reads as none", again != null && again.Run[RWBoard.Kills] == 77 && again.Run[RWBoard.Mvps] == 2 && again.Run[RWBoard.Maps] == 5
            && old != null && old.Run[RWBoard.Kills] == 0 && old.Pure == 3, again != null ? string.Join(",", Array.ConvertAll(again.Run, x => x.ToString())) : "no line");

        // MB8: icons and lines.
        List<string> missing = new List<string>();
        foreach (RWBoard.Title t in RWBoard.Titles)
        {
            Sprite s = RWBoard.Icon(t.Code);
            if (s == null || (int)s.rect.width != 12 || (int)s.rect.height != 12 || string.IsNullOrEmpty(t.Line)) missing.Add(t.Code);
        }
        Check("MB8 every title has a 12 x 12 icon and a line", missing.Count == 0, missing.Count == 0 ? RWBoard.Titles.Count + " titles" : "missing " + string.Join(",", missing.ToArray()));
    }
}
#endif
