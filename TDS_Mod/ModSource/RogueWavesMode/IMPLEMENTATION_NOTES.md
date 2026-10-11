# RogueWavesMode: implementation notes

## 0.1.1 (2026-10-11): Rising Water's flooded tiles

**PC round 1** (0.1.0 self-test, 2026-10-11): **34 pass, 0 fail, 0 setup.** Proven in game: the chart world built in
code loads and is warped onto; the arrows match the links; the boat sails to a chosen island by itself (2.5-3.3 s) and
stops 2.7 from it; the pick opens the Setup panel in `Mode_RogueWave` and the map loads in the mode; a won map is
sailed and the party lands one row on, the boat 0.05 from the dock; the save never keeps a chart name; the campaign's
seed and route survive Continue and Stop removes them. Quickening ran 1 -> 1.25 -> 1.5; the nest sent a Halberd.
The chart's `CHART` lines equal `concept-tools/sea_chart.py --check 20261009`.

**The bug it found:** Rising Water's water rose (y 2 -> 3.6) but flooded no tile. At its Tide the flood fetches
`Block_Flood_Deter` by name (Entity_Ai_Main_Flood.cs `Rise`) and threw a NullReferenceException in
`Helper_Gridtools.Checkifplaceable`, because `Load` stopped at the first group that held `Main_Flood` and the deter
was never loaded. W1 passed anyway, since it only checked the height. Fixed in 0.1.1:

- Each weather lists every being it needs (`RWWeather.Needs`: the flood and its deter; the nest and its Halberd), and
  the loader goes through the home Gameplan's groups in the game's order (the map's sub-loads, the map, the plan's
  mode) and then every stock mode's, until all of them are in. The log names the group each one came from.
- W1 now also needs flooded tiles to appear, and each weather map passes only if no error is logged while it runs.
  W1 logs the flood's `Maxheight`, `Heightspeed` and `Projslow` after the Tide: right after the spawn they read empty.

The run's other errors are the game's own (kit `docs/08` §6): one `OperationCanceledException` in
`Macro_Setupui.Mapselected` per map load, and a `NullReferenceException` in `Blu2_Menu_Handler.Animin` on the way back
to the boat after a won map.


## 0.1.0 (2026-10-10): sea chart and sea weather

**State:** compiled here (release and `ROGUEWAVESMODE_SELFTEST`: 0 errors; editor builder: clean). The chart generator
matches `concept-tools/sea_chart.py` on 400 seeds. PC round 1 (above) ran it in game. The PC round is
`docs/SEA_CHART.md` §9.

What only the game can tell, and what the self-test logs for it:

- Whether a world built in code loads, looks right and is lit: C1 logs the boat's position and the stock world's bounds
  before the warp, and the boot log names the material the chart copies (the boat's). The islands sit at y 0 with the
  sea's top at -0.4 and the docks at y 0; if the boat floats in the wrong place or the camera is off, change
  `RWWorld` (Scale 0.6, `Cameraposition` 5 north of each island) from the logged numbers.
- Whether the stock arrows and boat behave on code-built nodes (C2, C3).
- Whether `Main_Flood` and `Tower_Halberdnest` work off their home maps (W1, W3; kit `docs/16` §9 item 1). The flood
  spawns a tile under the lowest ground and its height stats are Manafold's: W1 logs them with the map's extent, for
  tuning. The flood's water mesh is sized for Manafold and may not cover a larger map.
- Quickening's `Time_Scale` reaching clients is an online check (kit `docs/16` §9 item 4); solo, W2 reads it on the host.

Not built in this slice (later Phase 5 pieces): the in-map vote that sets `RWCampaign.Choice` (until then the leader
steers with the arrows), Pure Aura (the weather's +1 a Tide is `RWWeather.PureAuraPerTide`), retries, the main-menu
button that calls `RWCampaign.New`/`Continue`, the chart's own art and a hover billboard, Directions, the shop.

Known limits:

- Auto-sailing runs on the leader's machine and is driven by host state, so it works when the host is the leader (the
  base game's default). If leadership passes to a client, the leader steers by hand.
- A chart node never offers its `Altgameplan`: δ maps are their own nodes.
- Twins of every base Gameplan exist in `Mode_RogueWave`; their medals and records write save keys named `RW_...`.
