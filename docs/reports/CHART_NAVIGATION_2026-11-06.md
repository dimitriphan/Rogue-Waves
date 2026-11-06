# Sea chart navigation and stops, a proposal (2026-11-06)

Asked by Dimitri (2026-11-06 02:18Z): make navigating the roguelike map feel like the spotlight's sea chart and
Slay the Spire 2's map, because picking among nodes alone is confusing. Asked whether to add new kinds of stop too, he
picked "Add rest and treasure stops" (02:28Z). Docs only: nothing built, nothing run on the PC. The design is
`docs/mode/CHART_NAVIGATION.md`; mockups `docs/concepts/art/chart_view_vote.png` and `chart_view_boat.png` (from
`concept-tools/sea_chart_view.py`, seed 20261009, the spotlight's chart, with the proposed stops).

## Findings (read from `TDS_Mod/ModSource/RogueWavesMode/Code/`)

1. The vote is a dialog of cards in the map (`20_Vote.cs`), apart from the chart: no position, nothing about what
   each choice leads to.
2. The boat's camera frames only the next row (`04_World.cs` `CameraSpot`); the whole chart is never on screen.
3. The boat sails by itself after a vote held in another scene, and nothing on the boat says where to (`05_Boat.cs`).
4. The chart has no legend; progress and retries are on the campaign card only (`13_Card.cs`), not at the vote;
   Danger shows only on the cards; every island is a map.

## Proposal

- **A chart view**: the whole chart on one flat panel (harbour at the bottom, row 10 at the top, row rail with the two
  stretches, Danger numbers, Breaker rings, weather icons, sailed route in teal, "you are here", out-of-reach islands
  faded, a legend). It opens by itself beside the stock cards at every map vote (cards lettered A-C to match; click a
  lit island or a card; hovering shows the island's card and lights where it leads) and with a key (~M~) at any
  time. On the boat a banner names where the boat sails (read by every machine from the boat's heading) and a mini
  chart inks the lane.
- **The Cove** (rest): one per stretch, rows ~4 and 9~, no map, ~a retry back~; when a Cove wins, the same map asks
  where to sail after it. **The Wreck** (treasure): one per stretch, rows ~2-3 and 6-8~, calm, ~one row harder~, pays
  each player ~one Trinket of 3~. The campaign stays 10 rows; difficulty follows the row.
- The stops come from a second random stream, so a seed's maps, links and weather stay the same, and campaigns saved
  before the change keep charts without stops.

## Review (sub-agent, PR #137)

Nine findings, all taken: the vote's winner must not ride on the chart's name (the host warps whenever the name
changes and clears the choice on arrival), so clients read it from the boat; the view sorts above the dialog's dim and
the cards move right; `RWDialog.Choose` instead of `Cancel`; the campaign card already shows progress; a client's name
is one map behind in a map; `InputSystem` for the key; self-tests run on the PC; the mockup's hover card, tie
footnote and tally line; "2 retries left".

## For Dimitri

- Defaults to confirm or change: `CHART_NAVIGATION.md` §7.
- Building it needs his "go": one part (`RogueWavesMode`), a new `26_ChartView.cs` plus the vote, dialog, boat, world,
  chart, campaign and difficulty changes in §6 there. Its PC round: checks CV1-CV12 (§8 there); the main unknowns are
  whether the stock choice dialog lets clicks through to a panel beside it (if not, the chart is a picture and the
  cards pick) and a code-built stop with no map.
