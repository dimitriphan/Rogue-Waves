# Sea chart navigation, a proposal (2026-11-06)

Asked by Dimitri (2026-11-06 02:18Z): make navigating the roguelike map feel like the spotlight's sea chart and
Slay the Spire 2's map, because picking among nodes alone is confusing. Docs only: nothing built, nothing run on the PC.
The design is `docs/mode/CHART_NAVIGATION.md`; mockups `docs/concepts/art/chart_view_vote.png` and `chart_view_boat.png`
(from `concept-tools/sea_chart_view.py`, seed 20261009, the spotlight's chart).

## Findings (read from `TDS_Mod/ModSource/RogueWavesMode/Code/`)

1. The vote is a dialog of cards in the map (`20_Vote.cs`), apart from the chart: no position, nothing about what
   each choice leads to.
2. The boat's camera frames only the next row (`04_World.cs` `CameraSpot`); the whole chart is never on screen.
3. The boat sails by itself after a vote held in another scene, and nothing on the boat says where to (`05_Boat.cs`).
4. No legend, no "map N of 10", no stretch or retries on the chart; Danger shows only on the cards.

## Proposal in one paragraph

A **chart view**: the whole chart on one flat panel (harbour at the bottom, row 10 at the top, row rail with the two
stretches, Danger numbers, Breaker rings, weather icons, sailed route in teal, "you are here", out-of-reach islands
faded, a legend). It opens by itself beside the stock cards at every map vote (cards lettered A-C to match; click a
lit island or a card; hovering shows the island's card and lights where it leads), and with a key (~M~) at any
time. On the boat a banner names the voted map and a mini chart inks the lane as the boat sails; the 3D chart gains
a ring on the voted island. The chart's generator, seeds, saves, the in-map vote's channel and the 3D boat stay as
they are, so a running campaign continues.

## For Dimitri

- **Asked in the thread:** whether to add new kinds of island too (unknown "?" islands; rest and treasure stops).
  Recommended: navigation only for now; the rest after he has seen the view.
- Defaults taken (`CHART_NAVIGATION.md` §6): the key ~M~, re-voting until the timer ends ~on~, others' votes on the
  chart ~later~, the mini chart ~top right~.
- Building it needs his "go"; it is one part (`RogueWavesMode`), a new `26_ChartView.cs` plus small changes to the
  vote, the dialog, the boat and the world (§5 there). Its PC round: checks CV1-CV8 (§7 there), the main unknown
  being whether the stock choice dialog lets clicks through to a panel beside it (if not, the chart is a picture and
  the cards pick).
