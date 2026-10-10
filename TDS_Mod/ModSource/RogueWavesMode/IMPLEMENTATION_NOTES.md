# RogueWavesMode: implementation notes

## 0.1.0 (2026-10-10): sea chart and sea weather

**State:** compiled here (release and `ROGUEWAVESMODE_SELFTEST`: 0 errors; editor builder: clean). The chart generator
matches `concept-tools/sea_chart.py` on 400 seeds. **Nothing has run in game yet.** The PC round is
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
