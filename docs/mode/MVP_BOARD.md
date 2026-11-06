# The MVP board: who carried the crew, and who sank it (2026-11-06, built, not yet run on the PC)

Dimitri asked (2026-11-06 06:35Z): "Draft an MVP board for multiplayer that shows accolades and failures of the
players." Then (06:55Z): "It doesn't need to account for one accolade and one failure, just the most notable thing of
the player (balanced so that getting a failure is as common as getting an accolade). Defaults for rest, and then
build." This page holds **when it shows** (§1), **what it reads** (§2), **the titles** (§3), **how they are handed
out** (§4), **the look** (§5) and **the code** (§6). It is built as written here; the PC has not run it yet.

Tags as in `SEA_CHART.md`: **proven** = shown in game, **read** = from the game's code, **untested** = needs the PC.
Code paths are `TDS_Mod/ModSource/RogueWavesMode/Code/`. `~value~` = a default Claude picked, his to change.
Mockup: `docs/concepts/art/mvp_board_map.png`, drawn by `concept-tools/mvp_board_look.py` with the pack's own vessel
icons and no game pictures; Inter stands in for the game's font, as in the chart mockups.

## 1. When it shows

| Board | When | Who sees it |
| --- | --- | --- |
| **Map board** ("SHIP'S LOG") | A won map's leave vote passes (`20_Vote.cs` "leaving"): the board first, then the Direction picks and the next-map vote | every machine, each player's own card marked |
| **Map board, late** | A won map that ends in a bonus Tide instead (it still counts as won and saves what the bonus Tides earned, `06_Campaign.cs` `MapEnd`): the same board over the stock end screen (`~untested: drawing over the end screen~`) | every machine |
| **Voyage board** ("THE VOYAGE") | The campaign's end. Map 10 won: its map board, then the Voyage board, then the stock win path (`20_Vote.cs` `CloseBoard`, then `Leave()`, no picks). The last retry lost: the Voyage board alone, over the end screen | every machine; the run's totals |
| **On a key** | Tab opens the last board again and Tab closes it (`~Tab~`; not while typing, a dialog or the chart view is up), so a friend who blinked can read it | the one who pressed |

- Multiplayer only: with one player in the map the board is skipped (`~solo: no board~`). Alone, the stock end screen
  already says it all.
- It holds the flow for `~12 s~` or until every player presses Continue (a click, Enter, Space or the pad's south
  button), whichever is first; it never waits on a
  player who doesn't answer (the vote's rule, `20_Vote.cs` header). The Tide clock stays held while it shows, as it
  does for the picks (`RWVote.HoldBreak`).
- The caption's "Next:" names what really follows: "Direction picks" after maps 1-9, "The Voyage" after map 10,
  "Back to the boat" for a late board; the Voyage board names nothing after it.
- A Cove has no map, so no board; a Wreck is a map like any other and gets one (`CHART_NAVIGATION.md` §4).
- A lost map shows no board: it replays from the last save, and what it earned goes with it (`14_Run.cs` header), so
  its numbers would vanish anyway. The Voyage board counts the retries used instead.

## 2. What it reads: most of it is already kept

**The game's own match statistics** (read, kit `docs/16` §5 "Match statistics"; `reference/network_surface.md:83`):
`Tds_Clientinfo.Activeplayerdata` on the host holds per player `Kills`, `Deaths`, `Vesseldeaths`, `Placed`,
`Upgraded`, `Sold`, and `Spent` / `Gained` per aura colour (fields in `reference/api_assembly_csharp.txt`,
`Tds_Playerdatatojson`). The stock end screen has a player-stats step (`Game_Endscreen.Populateplayerstats`; only the
name is in the repository, what it shows is for the PC). The mode's rejoin already keeps and restores these numbers
across a drop (`09_Rejoin.cs` `Seat.Stats`), so a friend who reconnects keeps their card.

Two catches, both read:

- They **reset only when `Activeplayer` drops to 0, not per scene**. Whether a player's `Activeplayer` passes through
  0 on the boat between maps is untested, so the host snapshots each player's numbers at a map's start and again when
  the board is drawn (the leave vote, or the map's end after a win), and boards the difference. Bonus Tides before
  the leave vote are counted. That is the snapshot `MECHANIC_IDEAS.md` §6 "A campaign summary" already asks for.
- **`Kills` go to the last hitter's owner, and only for hits that carry no `Damage`** (kit `docs/15` §16): a vessel's
  or unit's own attack and the stock shooters' shots set `Lasthit`, but burn ticks, pulses and a mod's scripted hits
  that pass `Damage` never do. So the stock count misses kills by such hits, and a vessel that kills mostly that way
  (a burn, a pulse) looks weaker than it is. Tidebreaker reads the stock count `~as a first cut~`; the fairer count
  is §6's damage tally. Leaky Hull's last hitter has the same gap (§3.2).

**The mode's own per-player state** (read, `14_Run.cs` `RWPlayer`, `20_Vote.cs`, `09_Rejoin.cs`): Pure Aura held
(`Pure`), base aura sent to friends in a break (`Sent`, Fund Transfer), Directions and Trinkets held, a Trinket bought
at VENDCINT this map (`Bought`), each player's vote (`RWVote.Votes`), and a dropped seat (`RWRejoin.Seat.Away`).
The aura left unspent in each wallet is `Tds_Clientinfo.Resources`, which the host already reads.

**New tracking** (§6), small tallies the mode doesn't keep yet: aura sent summed over a map (`Sent` resets each
break), votes left unanswered, drops, and for the Voyage board votes cast with the winning side, Pure Aura spent and
map MVPs won. One needs an engine hook: **leaks**, foes that reach the core, charged to a player (§3.2).

## 3. The titles

Each player gets **one** title: the accolade or the failure that stands out most for them (§4). Each title is one
line of text and one 12 x 12 pixel icon. Numbers on the board are fine: it is a tally, not upgrade,
Cardinal or Trinket text, which is where `docs/STYLE.md` keeps numbers out.

### 3.1 Accolades

| Title | Stands out for | Line on the board | Reads | Icon |
| --- | --- | --- | --- | --- |
| **Tidebreaker** | most finishing blows | "Sent 214 foes to the bottom." | `Kills` (§2 catch) | a breaking wave |
| **Shipwright** | most built and upgraded (`Placed` + `Upgraded`) | "Built half the harbour. Upgraded the other half." | `Placed`, `Upgraded` | a hammer |
| **Treasurer** | most aura gained | "Brought in 1,240 aura. Kept the lights on." | `Gained` | a coin purse |
| **Quartermaster** | most aura spent | "Every coin went to work." | `Spent` | a crate |
| **Unsinkable** | no vessel deaths, counted as finishing blows + 1 | "Never touched the seabed." | `Vesseldeaths`, `Kills` | a life ring |
| **Open Hand** | most base aura sent to friends | "Gave away 300 aura. A saint, or a smuggler." | `Sent` summed (new) | two hands |
| **Deckhand** | fallback: on the accolade side with nothing left that stands out (§4) | "Kept the ship afloat. Someone had to." | none | a mop |

### 3.2 Failures (light, never mean)

| Title | Stands out for | Line on the board | Reads | Icon |
| --- | --- | --- | --- | --- |
| **Anchor** | most vessel deaths | "Spent more time on the seabed than on deck." | `Vesseldeaths` | an anchor |
| **Driftwood** | most of their own towers and minions lost | "Their towers keep washing up on the beach." | `Deaths` (which deaths count: untested, §6) | a plank |
| **Dragon's Hoard** | most aura unspent when the board is drawn | "Died rich. Well, sailed rich." | `Resources` then | a chest |
| **Second Thoughts** | most sold | "Built it. Regretted it. Sold it. Built it again." | `Sold` | a "?" sign |
| **Leaky Hull** | most leaks on their watch | "Had it on the hook and let it slip." | leaks (new, engine hook) | a dripping bucket |
| **Fashionably Late** | most votes left unanswered | "Still reading the first card." | `RWVote` timeouts (new) | an hourglass |
| **Gone Fishing** | dropped and rejoined this map | "Went for bait mid-Tide. Came back for the glory." | `RWRejoin` drops (new) | a fishing rod |
| **Spotless** | fallback: on the failure side with nothing left that stands out (§4) | "Nothing went wrong. Suspicious." | none | a sparkle |

**Leaky Hull's rule** (default taken) `~a leak counts against the foe's last hitter; a foe with no last hitter counts against nobody
and only shows in the team line~`. It is the cheapest rule (the same `Lasthit` owner as a kill) and reads as the joke
("had it on the hook"). It inherits the §2 gap: a foe last touched by a burn or a pulse has no last hitter. The
alternative was a team-only leak count with no name on it (§7).

### 3.3 The Voyage board's own titles

Summed over every won map's snapshot: **Old Salt** (most map MVPs), **First Mate** (most votes cast with the winning
side over the run; the map vote and the leave vote both count), **Pearl Diver** (most Pure Aura spent over the run:
every player is paid the same Pure Aura each Tide, `14_Run.cs` `PayTide`, so spending is what differs; Pearl is Pure
Aura's colour), **Barnacle** (most vessel deaths over the run; Anchor's voyage form), plus the map titles on run totals except
Anchor and Dragon's Hoard. The same hand-out (§4) runs over the Voyage list, for everyone who sailed a map (four at
most: those who sailed the most). Its team line: maps sailed, retries, leaks.

## 4. How titles are handed out

The host runs this once per board (`27_Board.cs` `Give`, a pure function the self-test calls); every machine draws
what the host sends (§6), so all see the same board.

1. **Counts.** For each title `t` and player `p`, a count `v(t, p)` from §3, and `total(t)` over the crew of `n`.
   Unsinkable's count is `Kills + 1` for a player with no vessel deaths, else 0.
2. **How much it stands out.** `note(t, p) = v(t, p) / total(t) - 1 / n`, a share above an even split. A title stands
   out for a player only when its note is above 0, so in an even crew nothing does.
3. **How many failures.** Half the crew, `n / 2`; an odd crew takes one more failure on every other odd-sized board
   the host gives (map and Voyage boards counted together, so a crew whose size changes between maps still alternates),
   so a failure is as common as an accolade (Dimitri's rule; each Continue restarts the count, which can tip the balance by one board). Two players: one each.
   Three: one failure, then two, in turn. Four: two.
4. **Who takes them.** Each player's lean is their best failure note minus their best accolade note. The players who
   lean furthest to a failure take the failure side; ties go to the smaller `RWPlayer.Key` (the GUID the pack keeps
   per player, the same after a rejoin or a Continue).
5. **Which title.** On each side, every (title, player) pair that stands out, the largest note first; ties go to the
   larger count, then the title's order in §3, then the key. A title goes to one player per board, a player gets one
   title. A player left with nothing gets **Deckhand** (accolade side) or **Spotless** (failure side), which can go
   to several.
6. **MVP.** For each player, the sum of `v(t, p) / total(t)` over the accolades with `total(t) > 0`, Unsinkable left
   out (it is finishing blows again). The largest sum is the MVP; ties go to more `Kills`, then more `Gained`, then
   the key; no MVP when every sum is 0. The MVP is apart from the title, so the MVP can wear a failure (carried the
   crew and drowned the most). One MVP per board, shown with the white corner brackets and the coral "MVP" banner.

The result depends only on the counts, the keys and that count, never on the order players joined.

## 5. The look (`docs/themes/rogue-tide.md`, the game's UI pieces as in `CHART_NAVIGATION.md` §3.6)

| Piece | The board |
| --- | --- |
| Background | No panel: the map stays under the dialog's dim (`Fade`, kit `20` §2), as the vote cards do |
| Title | "SHIP'S LOG" in capitals over the cards, the bright rule turning down the right side with its knot, the pack's 12 px crest on the rule's left end; "MAP 4 WON • THE NARROWS" (the map and its name) small under it |
| Player cards | One tall square card per player in the vote cards' build: light frame, a gap, a Deep Swell tile with a darker rim; the vessel's icon at the top, the player's name, then two tiles |
| Accolade row | a raised Deep Swell row (`#35546D`) with a Rogue Teal edge; its pixel icon on a small teal stock-built tile, the title in capitals and the line in Foam beside it (body text never sits on teal: the theme's contrast rule) |
| Failure row | the same with Button Slate `5B5D69` for the edge and the icon's tile, the line in Mist |
| Numbers | four small figures under the tiles (finishing blows, built, aura gained, vessel deaths) in the label font, Foam on Deep Swell |
| MVP | white corner brackets round that card and the view's one Storm Coral piece, a small "MVP" banner over it in white capitals |
| Your card | a thin Wet Sand line under your name, so you find yourself at a glance |
| Away player | a player still away when the board is drawn keeps their card (their numbers are held by the rejoin seat), dimmed, with "Away" under the name |
| Caption | one line under the cards, parts between "•": "Tide 15 • 2 retries left • 38 foes slipped through • Next: Direction picks" |
| Continue | a teal tile button, with the seconds left beside it ("Waiting for the crew • 7 s" once you pressed it) |

Four cards side by side, the most a game holds: the stock Steam lobby is 4 (read, kit `docs/19` §1). Whether four
fit at 1080p is for the PC.

## 6. The code (built 2026-11-06; compiled here, not run on the PC)

| Piece | Where | Needs the PC |
| --- | --- | --- |
| Snapshot `Activeplayerdata` per player at a map's start; board the difference (a count lower than its snapshot reads as reset, so it is all this map's) | `27_Board.cs` `MapStart`, `MapRows` | whether `Activeplayer` passes through 0 on the boat |
| Tallies: aura sent per map, unanswered votes, drops, leaks; for the Voyage, winning-side votes, Pure Aura spent, map MVPs, maps sailed | `14_Run.cs` `RWPlayer`, `20_Vote.cs` closers, `09_Rejoin.cs`, `17_Cardinals.cs`, `18_Shop.cs` | no |
| Leaks: the Basin's hits (`Oninteract`, kept in front with `ModKitUtil.Front`), one leak a foe, charged to its `Lasthit` owner | `27_Board.cs` `Update`, `OnCore` | yes: do foes hit the Basin as "Hurt", with themselves as `From` |
| `Deaths`: which of a player's things it counts (Driftwood) | `27_Board.cs` `Read` | a look at the numbers after a round |
| Hand-out (§4) on the host; four cards and a header as `None` commands `RW_Log0`-`RW_Log4` on every vessel, the board's number as `Goalvalue` | `27_Board.cs` `Give`, `Write`; `14_Run.cs` `CarryLog`; `15_Content.cs` | no |
| The vote holds the break for it (state "Board"), Continue is an order (`RW_Order` kind "B"); after map 10 the Voyage board follows; a won map that ends in a bonus Tide and the last lost map show theirs over the end screen | `20_Vote.cs` `OpenBoard`, `CloseBoard`; `06_Campaign.cs` | the late board over the end screen |
| The board on every machine from the lines and a local text table; Tab replays it | `28_BoardView.cs` (the chart view's pieces, `26_ChartView.cs`) | the look, and four cards at 1080p |
| Voyage totals saved for Continue (the run line's ninth field; an older line reads as none) | `14_Run.cs` `Encode`, `Decode` | Continue keeps them |
| Self-tests MB1-MB8 at boot (hand-out, balance over 1002 boards, fallbacks, MVP, 61 bytes, save line, icons); MB9-MB11 in the two-player round (both machines see both cards, Continue closes it early) | `90_Board.cs`, `93_TwoPlayers.cs` | the boot and two-player rounds |

A `None` command carries 61 bytes and one float (kit `20` §3.1 table): a card is the title's two letters, four
figures, the vessel and the name cut to fit; the line text never goes over the wire.

## 7. Choices for Dimitri (he took the defaults, 2026-11-06 06:55Z)

1. The titles and their lines (§3): as written; his to rename or cut any time.
2. Leaky Hull: the last hitter (default).
3. When: after the leave vote (default).
4. Solo: no board (default).
5. The board's name: "SHIP'S LOG" (default).
6. One title per player instead of one of each, failures as common as accolades: his rule, §4.
