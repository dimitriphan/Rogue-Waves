# RogueWavesMode: implementation notes

## 0.1.0-dev, later (2026-11-04): the test round's fixes

`docs/reports/TEST_ROUND_2026-11-04.md` has the round; `docs/mode/REJOIN.md` §6 has the rejoin's table. What changed in the mode:

- **Rejoin** (`09_Rejoin.cs`): `OnLog`, `Repair` and `Looks` run again what a late player object or a late load group stopped (pitfalls #139, #141).
- **Spring Tide** (`16_Directions.cs`): numbers `{1,4},{1,7},{1,10}`, and rank III's shrug is `SpringShrug` in the part's hit hook (pitfall #140). `HasMinions` adds Fie and Buster. `Picture` aliases Recycler and Mushrooms.
- **Fund Transfer** (`18_Shop.cs`, `11_Dialog.cs`): every card has a picture; `RWDialog.Number` draws the amounts.
- **Objectives** (`19_Objectives.cs`, `07_Weather.cs`): Beat the Clock judges the clock's last reading in the Tide's own row (`clockLeft`, pitfall #142); Raze the Nest isn't offered where no nest fits (`CanNest`), and says so if one finds no room.
- **`RWRun.Vessel`** (`14_Run.cs`): Boutbro isn't the vessel. **`RWDifficulty.Pool`** (`21_Difficulty.cs`): a reserve list for an elite the map hasn't loaded.

Test code: `92_Gaps.cs` (`ROGUEWAVESMODE_GAPS`) and `93_TwoPlayers.cs` (`ROGUEWAVESMODE_TWOPC`), both out of a release build, and `Tools/pc/Run-TwoPlayers.ps1`.

## For the next mode build (2026-11-04, repository check)

`RWCardinals.NotSold` (`17_Cardinals.cs`) names four ids that are not among the game's 40 Cardinals (`reference/roguelab_round1.txt`):
`Artifact_Moon`, `Artifact_Loopedmirror`, `Artifact_Sand` and `Artifact_Bottomrock` (Bottom Rock is `Artifact_Rock`, also listed).
Harmless, since the shelf only ever sees the folder's 40; drop them or confirm what they meant at the next build
(`docs/reports/REPO_QUALITY_CHECK_2026-11-04.md` O3). The seven removed Cardinals' icons are gone from `Art/Temporary/game_size/`.

## 0.1.0-dev, later (2026-11-04): seven Cardinals removed

At Dimitri's word, Claude's seven starter Cardinals are gone from `RWCardinals.Mine` and their icons from
`RogueWavesModeArt.cs`: Spiny Hull, Sea Legs, Rising Tide, Puffer Float, Tide Clock, Rusted Bell and Second Wind. Each one
copied a stock Cardinal (`docs/reports/ROGUELIKE_DIRECTIONS_CARDINALS_2026-11-04.md` §1). With them goes the `Borrowed` load of
`Artifact_Vintage` and `Artifact_Loop`, which only Tide Clock and Second Wind needed. The pack now sells only
Vampire Dentures of its own, beside the stock Cardinals on sale. Claude's newer Cardinals in scope `12` are not built yet.
A saved campaign that still holds one of the seven keeps its id, but `Def()` finds no sheet for it, so nothing is
granted. **State:** compiled here: release and `ROGUEWAVESMODE_SELFTEST` gave the same 9 errors as the base, all TMPro members the cloud stubs lacked (`14_Run.cs`; added to the stubs 2026-11-04, so both now compile at 0), none from this change; editor builder clean. Not run on the PC. The next PC round's shop check (`Cardinals: every one on
sale goes on with no error`) covers it.

## 0.1.0-dev (2026-11-03): the mode polish

The coordinator's mode-polish pass (Dimitri's "Run 3, 4, and 5", 2026-11-03): the part checked line by line against
`docs/scope/04` and `docs/mode/`; the findings, the judgment calls kept for Dimitri and what is not built are in
`docs/reports/MODE_POLISH_2026-11-03.md`. **State:** compiled here (release and `ROGUEWAVESMODE_SELFTEST`: 0 errors;
editor builder clean); chart parity with `sea_chart.py` on seeds 1-2000 and 20261009. **Not run in game.**

- **Version** `0.1.0-dev`: the pack version plus `-dev` (`docs/PACK.md` §4), in `RWData.Version` and the prefab
  builder; the part counted its own versions before (0.1-0.3.0).
- **Save keys** (`01_Util.cs`, `RWKeys`): every key in `RogueWaves` carries `RW_` (kit `docs/23` §2 rule 3).
  `RWKeys.Migrate`, every machine from launch, renames the six old keys once per save object (the value is kept
  unless the new key already exists) and logs "save: N keys of RogueWaves renamed".
- **Retries** (`06_Campaign.cs`): 3 per campaign. A lost map spends one; the boat then offers only that map
  (`RWCampaign.Open`, the host's blacklist) until it is won. With none left the campaign ends and leaves the save.
- **The map in play** is in the save while it runs (`RW_Campaign_Playing`, set at `ModKitTides.MapStart` when the
  map is the chart's). If the host leaves it, it counts as lost: on the boat in the same launch (`RWCampaign.OnBoat`)
  or by `Continue` in a later one. So a late "Leave" (kit bug B1) changes nothing: the map was already counted and
  `MapEnd` ignores a map no longer in play. That also ends B2-1 (the next map losing its weather and route).
- **Won at Tide 15** (`RWCampaign.Tide`): when Tide 16 starts the map is won and every later Tide is a bonus Tide, so
  dying in one loses nothing; a "Win" end before it also wins. Ten maps won complete the campaign (it leaves the save;
  Depth, its reward, is on hold).
- **Breaks** (`02_Mode.cs`): every break between Tides of `Mode_RogueWave` is 60 s (Mode_Normal's 30 s, and its one
  10 s break before Tide 19); the opening break keeps Mode_Normal's 45 s. The `Waves` entries are structs, so
  Mode_Normal's table is untouched (K0 checks it).
- **Home** (`05_Boat.cs`): with no campaign on (ended, or left behind by a slot switch) a party still on a chart is
  warped back to the stock world it sailed from (`RW_Stock_World`), so no chart map is played outside a campaign.
- **Chart links** (`03_Chart.cs`, `concept-tools/sea_chart.py`): a row's last map takes at most 3 links (scope `04`
  [Sea Chart]); before, 1,399 of 20,000 charts had a map with 4. Seed 20261009 is unchanged.
- **Weather lines** (`07_Weather.cs`): Quickening's and Nesting's in-game lines are scope `04`'s.
- **Rejoin** (`09_Rejoin.cs`): a turned-away player is forgotten once they leave, so a second Join Game turns them
  away again (B4-14, `docs/mode/REJOIN.md` O5); the "Lost the map" line names this map's host (B4-12).
- **Prefab description** names the retries and the rejoin (B4-13).

New self-test checks for the next PC round: **K0** and **CH** at boot (breaks; 500 charts with 1-3 links), **K1** (no
unprefixed key), **P1** per map (the map in play is saved), **C5** (a won map is no longer in play), **T0-T6** on the
boat after S2 with pretend map ends (retries, replay-only blacklist, host quit on the boat and by Continue, Tide 16
wins and a later loss costs nothing, no retry left ends it, the tenth map completes it), **H1** (taken home after
`Stop`). What only the game can tell: whether the blacklist alone keeps the other choices from being picked during a
replay (the arrows still point at them, as for every closed node), and the chat lines on the boat.

## 0.3.0 (2026-10-15): fixes from the decompile

Dimitri asked to fix the four problems the decompile found in the pack (kit `docs/19` §3-5, `23` §6; pitfalls
#93-95, #99). **State:** compiled here (release and `ROGUEWAVESMODE_SELFTEST`: 0 errors; editor builder unchanged and
clean). **Not run in game, not tried online.**

- **Restart rejoin** (`10_Online.cs`, `RWIdentity`, every machine, every 0.5 s from launch): the player's GUID is
  kept in `RogueWaves/Player_Guid` (with `Player_Device`, a hash of this PC's id) and written back into
  `Metadata/Player_Guid`, which the game wipes on launch and reads on joining. The host then gives the same
  `Sessionid`, so `RWRejoin` and the game's own hand-back work as for a pulled cable. While connected, the GUID in use
  is the one kept. Online check: `docs/mode/REJOIN.md` O3b.
- **One failed load** (`RWLoad`, host, once a second while a `Game_Load` exists): 75 s into the loading screen, once
  the host has loaded, every player whose `Loaded` is still false is kicked with a chat line. Online check: O3c.
- **Hidden effects:** the mode sends none; Argo's slow look and refused-flood mark go past the throttle (Argo 2.0.1,
  its notes). Yossele 0.2.0-dev sends its looks with the ClientRpc the same way (`Yossele/Code/01_Util.cs`).
- **Slot switch** (`06_Campaign.cs`): the campaign remembers the `Bla_Data.Playerdata` object it was started or
  continued in; `RWCampaign.Live` (boat, weather, map end, every save) and the `Onslotchanged` / `Onslotremoved`
  hooks forget it from memory when that object changed. After a switch its own slot keeps it for Continue; clearing
  the slot it lives in erases it (the game reloads that slot from its default save).

New self-test checks for the next PC round: **I1** (on the boat: the kept GUID equals `Metadata`'s and this PC's; says
whether this launch put one back, so a second launch should say "put back"), **L1** (first map: the load watch saw
the loading screen and kicked nobody), **S1/S2** (end: the same save keeps the campaign; a pretended other save
leaves it behind while its slot still holds it).

## 0.2.1 (2026-10-11): the lobby is reopened every frame

**PC round 6** (0.2.1 self-test, 2026-10-11): **47 pass, 0 fail, 0 setup.** R2 "Server_Serveropen False -> True";
R2b open again two frames after the game's close; R1, R3-R6 and every round-4 check pass. The same six game-only
exception lines as round 5. Still only online: O1-O8 (`docs/mode/REJOIN.md` §6).

Since (2026-10-15, compiled here, not run): `08_Icons.cs` logs `icons: three weathers` on success, so
`Run-SelfTests.ps1 -BootCheck` sees the mode part ready (it only logged `icons:` on a failure before).

**PC round 5** (0.2.0 self-test, 2026-10-11): **45 pass, 1 fail, 0 setup.** Every round-4 check passed again, and R1,
R3, R4, R5, R6 and the second half of R2 passed. R2 failed: the mod opened the lobby ("rejoin: lobby opened"), then
the game's loading screen ended ("Loading 90%", `Game_Load` removing the Gameloading scene with `Lobbystate` "Closed",
about 10 s after the fly-in) and shut it, and 0.2.0 only looked again a second later. So a drop early in a map left
the lobby shut for up to a second. 0.2.1 checks the door every frame; the probe waits until `Game_Load` is gone, and a
new R2b shuts the lobby the way the game does and checks it is open two frames later. Six exception lines, all with
only game frames in their stacks: 3 `Macro_Setupui.Mapselected` `OperationCanceledException` (just after a map is
picked) and 3 `Blu2_Menu_Handler.Animin` `NullReferenceException` (just after a won map is sailed). Game `Assembly-CSharp.dll` on the PC matches the cloud copy
(SHA256 fb80f939...). Compiled here (release and self-test: 0 errors).

## 0.2.0 (2026-10-11): rejoining a running map

Dimitri asked for a way back into a running map for a player whose connection drops. Design, what comes back and the
online checks: `docs/mode/REJOIN.md`. Engine facts: kit `docs/15` §14.5. **State:** compiled here (release and
`ROGUEWAVESMODE_SELFTEST`: 0 errors; editor builder unchanged and clean). Then run in game: solo PC round
`RogueWavesMode_round5` (the round-4 run plus R1-R6 in the first map) gave 45/1, round 6 47/0 on 0.2.1 (above); the
drop and rejoin themselves need a friend online (`docs/mode/REJOIN.md` §6, O1-O8).

`Code/09_Rejoin.cs` (`RWRejoin`), every frame:

- **Host:** a `Seat` per `Sessionid` in the map once `Gameloaded` is true (vessel name, team, aura, stats, name;
  copied every 0.25 s). A seat missing from `Blm_Server.Sessionids` is away: a chat line, and the lobby opens
  (`Changelobbystatus_ServerOnly("Open")` plus `Server_Serveropen`, re-checked every second because `Game_Load` shuts
  it at the end of its loading screen). A seat's `Sessionid` back with a new `Tds_Clientinfo` gets `Restore`. The lobby
  shuts when every seat is filled or `Gamecomplete` leaves "Running"; when the map is simply gone the boat's own scene
  change has opened it, so nothing is shut then. A `Sessionid` with no seat is kicked (`Kickclient`) with a chat line.
- **Rejoiner:** late if `Game_Init` spawns with `Gameloaded` already true. Preloads the map's weather
  (`MapWeather`: chart name + Gameplan), loads each player's vessel group once its `Hero` is known (its own arrives with the seat), and once the local
  `Loaded` is true calls `Setintroposition` and `Startvisuals(false)`. If `Gametimescale` is still false 15 s later it
  frees the camera and HUD by hand and follows the game speed itself (`Bl_Camera` is internal, so the camera can't be
  switched from a mod).
- **Dropped client:** a connection that ends while `Gamecomplete` is "Running" adds a "Lost the map ... Join Game"
  line to `Multiplayererror` a frame after the game's own "Disconnected", so the main menu shows it.

What only the game can tell: whether Steam's Join Game reaches a lobby reopened mid-map (O2), the rejoiner's vessel and
weather visuals inside the 10 s group window (O3, O7), and any hitch for the others while Netcode sends the map (O6).


## 0.1.3 (2026-10-11): the stand-in is made in the map, and no flood without it

**PC round 4** (0.1.3 self-test, 2026-10-11): **38 pass, 0 fail, 0 setup.** The broader "Block_No..." match found
`Block_Noplacementfish` (Boutman's, loaded with `Hero_Boutman`, which Manafold's sub-loads bring) already on the boat,
so the stand-in was made there: "Block_Flood_Deter stood in by a copy of Block_Noplacementfish". The water climbed
(y 2 -> 3.6) and flooded 67 tiles at the first Tide with no error, and the flood's throw is gone. Not yet seen: what
the stand-in looks like on a flooded tile (it borrows the fish block's look), and Rising Water online.

**PC round 3** (0.1.2 self-test): **35 pass, 3 fail**, the same three Rising Water failures. The load now runs once
(1 log line, was 192), but neither `Block_Noplacement` nor `Block_Nobuild_Brittle` is loaded on the boat, so no
stand-in was made and the flood threw again. That throw unwinds through the `Wave` setter into `Mode_Normal.Run`,
which drives the Tides, so a flood without its deter is not safe to leave on a map.

0.1.3:

- Every machine retries the stand-in once a second for each map's first 30 s, while the map's groups finish loading
  (`RWWeather.DeterUpdate`), and takes any loaded `Block_No...` being if neither preferred one is there. It logs once:
  "on the map after N s: Block_Flood_Deter stood in by a copy of ...", or the `Block_` beings it saw.
- The host waits for that before spawning the flood, and **does not spawn it** if there is still no deter.
- W0 for Rising Water now checks only `Main_Flood` (the deter can only exist in the map); W1 checks the flooding.

If round 4 also finds no `Block_No...` being, the log lists the loaded `Block_` beings to build one from.


## 0.1.2 (2026-10-11): a stand-in for the flood's deter block

**PC round 2** (0.1.1 self-test): **35 pass, 3 fail.** Every chart, save, Quickening and Nesting check passed (Nesting
now logs `[found Tower_Halberdnest Enemy_Halberd]` from `Map_Vultures`). The three failures are all Rising Water:
`Block_Flood_Deter` is in **none** of the groups loaded (`Hero_Boutman`, `Map_Manafold`, which holds `Main_Flood`,
and all six stock modes' groups), so the flood still threw at its Tide and flooded nothing. The game itself asks for it
by name and finds nothing, so it may not ship at all. The loader also retried every half second (192 log lines).

0.1.2: when the deter is still missing after the groups, the boat registers a copy of a stock no-build block
(`Block_Noplacement`, else `Block_Nobuild_Brittle`) named `Block_Flood_Deter`, where `Bl_Address.Getload` and a
spawn's id lookup find it (`Groups["Mob"]`, `Groupsids`, `Groupsname`), on every machine. The log line ends
"Block_Flood_Deter stood in by a copy of ...". Each weather is now loaded once per launch. Round 3 checks that the
flooded tiles appear; what the stand-in looks like on a flooded tile is unseen.


## 0.1.1 (2026-10-11): Rising Water's flooded tiles (incomplete, see 0.1.2)

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
`docs/mode/SEA_CHART.md` §9.

What only the game can tell, and what the self-test logs for it:

- Whether a world built in code loads, looks right and is lit: C1 logs the boat's position and the stock world's bounds
  before the warp, and the boot log names the material the chart copies (the boat's). The islands sit at y 0 with the
  sea's top at -0.4 and the docks at y 0; if the boat floats in the wrong place or the camera is off, change
  `RWWorld` (Scale 0.6, `Cameraposition` 5 north of each island) from the logged numbers.
- Whether the stock arrows and boat behave on code-built nodes (C2, C3).
- Whether `Main_Flood` and `Tower_Halberdnest` work off their home maps (W1, W3; kit `docs/16` §3). The flood
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
