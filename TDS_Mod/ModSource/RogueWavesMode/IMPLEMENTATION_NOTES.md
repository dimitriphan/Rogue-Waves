# RogueWavesMode: implementation notes

## 0.1.0-dev, later (2026-11-06): the MVP board (compiled here, not yet run in game)

Dimitri: one title per player, "the most notable thing", failures as common as accolades; defaults for the rest
(`docs/mode/MVP_BOARD.md`, §4 the rule, §6 the pieces).
- `27_Board.cs` (host): per-map snapshot of `Activeplayerdata`; tallies on `RWPlayer` (Leaks, Missed, Drops, SentMap, and
  `Run[]` saved as the run line's ninth field); the leak hook on the Basin; `Give` (pure); `ShowMap`, `ShowVoyage`; the
  board as `RW_Log0-4` None commands on every vessel (`14_Run.cs` `CarryLog`, `15_Content.cs` `LogCommands`).
- `20_Vote.cs`: state "Board" between the vote to leave and the picks (12 s or everyone's Continue, order "B"); after
  map 10 the Voyage board, then `Leave()`. `06_Campaign.cs`: the late board after a bonus Tide, the Voyage board after
  the last lost retry. `09_Rejoin.cs`: drops. `17_Cardinals.cs`, `18_Shop.cs`: Pure Aura spent, aura sent.
- `28_BoardView.cs` (every machine): reads its own vessel's lines, draws the cards with `26_ChartView.cs`'s pieces
  (made public), Continue by click, Enter, Space or the pad's south button; Tab replays the last board.
- Self-tests: MB1-MB8 at boot (`90_Board.cs`), MB9-MB11 in `93_TwoPlayers.cs` after both leave.
- For the PC: MB1-MB11 pass; the look at 1080p with four cards; do foes hit the Basin as "Hurt" with themselves as
  `From` (leaks show in the caption); does `Activeplayerdata` reset on the boat (a card's numbers look like one map's);
  which things `Deaths` counts (Driftwood); the late board over the end screen; Tab replay; the pad's south button;
  Continue after a rejoin keeps the run's totals.

## 0.1.0-dev, later (2026-11-06): the menu buttons' wobble (self-test 168/0 on the PC, `Wobble_round1`)

- Dimitri: "The button itself wobbles and jitters, including the base game buttons in the main menu." The game's layout puts
  its button pictures on half pixels at UI scale 3, and the main menu's camera sways with the UI camera as its child
  (pitfall #174). `RWPixels` (`26_Pixels.cs`, every machine, every scene with menus) moves each still picture by under half a
  pixel onto whole screen pixels, measured from its canvas's corner; it leaves pictures the game slides, animates, scrolls
  or lays out, the choice dialog's, and any someone keeps writing. A pass at most every half second, 64 pictures a frame,
  no garbage.
- Self-test P1-P2 (`94_Pixels.cs`, in the flow on the main menu) reads the drawn frame over 144 frames with and without it.
  PC checks: `docs/reports/BUTTON_WOBBLE_2026-11-06.md`.

## 0.1.0-dev, later (2026-11-06): sea chart navigation (compiled here, not yet run in game)

Dimitri: "Go" on `docs/mode/CHART_NAVIGATION.md`, built with its §7 defaults (`docs/reports/CHART_BUILD_2026-11-06.md`).
- Stops: `RWChart` places one Cove (rows 4, 9) and one Wreck (rows 2-3, 6-8) per stretch from a second stream; `S` in the
  world's name and `RW_Campaign_Stops`; older campaigns keep their charts without stops. Route counts rows, a Cove's too.
- The Cove is always blacklisted on the boat; `RWCampaign.Rest` is the one save write (route, gift, `Choice = After`), then
  the party warps onto the new name and sails on (`05_Boat.cs` `Rested`). Its second vote: `M:<row>:<cols>`, `RW_Campaign_After`.
- The Wreck plays row + 1 (`21_Difficulty.cs`); after its Tide 15 the "T" question gives each player a Trinket of three, free.
- `26_ChartView.cs`: the chart beside the vote (`RWDialog.Ask(..., at)` moves the cards right), key M, banner and mini chart.
  Pointer and key through `UnityEngine.InputSystem`; the shade blocks clicks; the view hides under any other dialog.
- Self-tests: CV1, CV3, CV9 at boot; CV10-CV12 (`90_Stops.cs`); CV2, CV4, CV5 in the flow. Older tests run with
  `StopsOnNew` off. Save fuzz F5 covers the `S` mark. PC list: the report's §4.

## 0.1.0-dev, later (2026-11-06): round 4 and batch fixes (compiled, not yet run in game)

- A friend's end title holds on every machine: `RWCard.HoldEnd` runs everywhere, and a friend's `ClientEnd` writes the last campaign title the sync brought (`IsEndTitle`) into the shown title, never the synced value (P1-4, pitfall #129; `13_Card.cs`).
- `RWBody` (`10_Online.cs`): boot loads `General_Default`, and a late joiner's things get the Vfx/Mobbase body the first frame they are in, with a count line after (pitfall #173).
- The meteor is 12 hits and about 8 tiles from the Basin on map 1, +3 hits and +1.5 tiles a map after (`19_Objectives.cs`, scope `14`:14; it was one map ahead).
- Test fixes (round 4 §8): U3 and T4 compute their titles from the map's row; Spring Tide's expected max comes from the tower's own Health buckets; the flow's leave vote keeps its own line (`LeaveVote`); the review buys at the shelf price and checks a later map against what map 1 held (`99_SelfTest.cs`, `93_TwoPlayers.cs`, `96_Flow.cs`, `92_Review.cs`).
- Three rejoin log lines are reworded (`09_Rejoin.cs`).
- PC checks for the next round: `docs/reports/BUG_FIXES_2026-11-06.md` §5.

## 0.1.0-dev, later (2026-11-06): Tidal Mend's heal is flat (compiled, not yet run in game)

Dimitri picked "Code follows the scope" for the third scope audit's N1 (`docs/reports/SCOPE_CODE_AUDIT_2026-11-06.md` §2):
`TideEffects` heals `MendHeal` 5 / 8 / 12 a rank, whatever the tower's level (scope `11`). The gaps test adds "each mend is
the rank's flat number" (`RWDirections.Mends`), with the towers' levels in its line. PC checks:
`docs/reports/STOCK_TRINKETS_2026-11-05.md` §8.

## 0.1.0-dev, later (2026-11-05): his answers on the split (compiled, not yet run in game)

Dimitri: "Keep that, Make Trinket, Pay only share" (`docs/reports/STOCK_TRINKETS_2026-11-05.md` §7). Kind Unit (Torrid
Badge, Bendigo, Kippy, Fish Bait): `GrantStock` puts the stock spawner upgrade on the buyer's vessel (Artifactobject
passed); `RWTrinkets.Companions` takes away a buyer's unit whose parent vessel is gone once the new vessel's
stands (one at a time; a live spawner's unit is never taken), outside the spawn. `RWTrinkets.Pay` divides a Coin's sums
by `RWRun.Sessions().Count`; the start sum waits for every vessel.

## 0.1.0-dev, later (2026-11-05): base game Cardinals split into Trinkets (compiled, not yet run in game)

Dimitri: "separate them" (`docs/reports/STOCK_TRINKETS_2026-11-05.md`). `RWCardinals.StockTrinketIds` lists the 13 stock
Cardinals that are Trinkets now, each with its kind: Hero (Sport Water, Fluffy Slippers, Beast Tassle, Diorama, Paper
Tower: `GrantStock` puts the Cardinal's own Heroapply effects on the buyer's vessel, `Reach` on a Hero-tagged unit of
theirs), Tower (Rusty Pipe, Frail Titan) and Built (Voice in a Bottle): `Reach` gives the Towerapply or Voiceapply
effect to the buyer's new things through their init list (pitfall #118), at once to what stands; Coin (the four):
`RWTrinkets.CoinStart` and `Tide` pay the buyer the Coin's own sums; Gavel: `RWTrinkets.Gavel` fires the Gavel's bolt
(`Bolt`, shared with Signal Flare) when one of the buyer's units drowns. A held one is `"<Artifact id>1"` in
`RWPlayer.Trinkets`, with a scriptless panel line `RW_StockTrinket<i>_Upgradeobject` (the game's own words); never on the
core or the row. `StockTaken`: not sold while a lobby pick or a pre-split save's run Cardinal already gives it to
everyone. Gaps rules "Stock split" x2. The PC list is the report's §6.

## 0.1.0-dev, later (2026-11-05): a look per weather (compiled, not yet run in game)

Dimitri's pick on the third decisions page (`docs/reports/OPEN_DECISIONS_2026-11-05c.md`): `RWWeather` ("the look",
`07_Weather.cs`) holds a three-row table, Rising Water light mist, Quickening Current storm grey, Nesting Ground dusk,
the PC's own L1 numbers (`docs/reports/LIGHTING_2026-11-05.md` §6.3). `LookUpdate`, every frame on every machine, sets
it once a map with `ModKitLight.Set` and a 4 s fade when this machine's intro is over (`Game_Client_Init.Gametimescale`),
from the synced names (`RWRejoin.MapWeather`); a calm map sets nothing. Fog distances are relative to the camera's
distance to the map's lowest tile (the `Surface` entities every machine has). Self-test W4 on each probe map and on
the flow's calm map 1; the gaps build's L1 takes its three looks from the same table. No kit change. The look's own PC
round: `docs/reports/WEATHER_LOOK_2026-11-05.md` §4.

## 0.1.0-dev, later (2026-11-05): the balance sweep's numbers (compiled, not yet run in game)

Set S2 of `docs/reports/MODE_BALANCE_REVIEW_2026-11-05.md`, tuned by `docs/reports/BALANCE_SWEEP_2026-11-05.md`:

- `RWDifficulty`: map k's Tide 15 plays the base game's row `FinishRows[k]` (15, 18, 20, 21, 21, 24 x5) and its Tides
  1-14 the rows between (F1); a Tide entry and the break before it carry the notes of the row they play; the bonus Tides
  go on from the row after and rewind on row 29. Foe health is one Pmulti (F2), x(2.6 + 0.08(k - 1)) (F3) x(1 + 2%
  for each Cardinal stack, Trinket copy and Direction rank held as the map starts) (F5, `Hoard`). Modifiers from map 2,
  one more every second map, each Tide taking them in turn (F4). +50% foes a player past the first (X1).
- `RWCardinals`: every price is 3 more for each Trinket copy the buyer holds and each Cardinal the run has bought (C1,
  `Ramp`). The host writes that ramp on each vessel as a fourth None command, `RW_Price`, which `PriceMine` reads: a
  bought stock Cardinal and a lobby pick look the same on a client.
- `RWObjectives`: the board opens from the break before Tide 3 (P1, `ObjectivesFrom`). Self-tests that read map 1's
  opening board go through `RWTest.BoardFromTide1`, which sets it back however they end.

## 0.1.0-dev, later (2026-11-05): Spring Tide measured on a plain tower (compiled, not yet run in game)

Light_round1's two Spring Tide fails (`0 -> 0.0375` where 10 is asked) point at the test's tower, not B7's effect. The
archer it measured is built right after the flow buys every stock Cardinal, so it holds Frail Titan (a Health Pmulti on
every tower spawned after it) and is Paper Tower's paper tower (a Health Multi on the first Towerfighter child after it;
the game calls `Parent.Child` at the end of every spawn, Entity_Master.cs:333-336). Both readings, and the stock Beacon's
`0.0375 -> 0.1375` of 4 Nov, fit `(base x Pmulti + Add) x Multi` with a base part of -6.25, Multi 0.01 and a floor of 0
(pitfall #171): `Grow`'s +10 Add would then be on it. Fitted, not yet confirmed: the data's numbers are only on the PC.

The flow (`96_Flow.cs`) now places a plain archer (`PlaceArcher`) before the map's first Cardinal is bought and checks on
it that the max rises by 10 at Tide 1's break and the tower's own modifier (`RWDirections.SpringOn`, a read-only
accessor for the self-test) by 10, with no `Resistadd`, and a third line at Tide 2's break: both by 10 again (the
modifier's `Value_Add` setter path, never run before). The archer built after the Cardinals keeps its other checks and
has its Health parts and Cardinal effects logged before and after Tide 1, so the next round confirms the cause. No game
behaviour changed.

## 0.1.0-dev, later (2026-11-05): the decided fixes, kit 1.14 (compiled, not yet run in game)

Dimitri's "default for all" on the second decisions page (`docs/reports/DECIDED_CODE_FIXES_2026-11-05.md`):

- Argo's Cleave is an extra hit for Notched Oar, Fish Hook, Gutting Knife and the attack counts (`RWAny.ExtraHit`);
- Tar Bucket marks hits with Mason's Trowel's hook (`RWUtil.Hit`);
- one copy test, `RWRun.IsCopy`, on the run-time tags (pitfall #169); a Hero-tagged spawn is reached once it has stats;
- `ClashHere`, `PriceMine` and `MaxedHere` read a held stack's panel id as `Grant` puts it;
- Boutman's Direction shows as Spare Reel (id `Longline` kept); the leave vote reads "Most votes win. Nobody leaves right
  before their demise".

## 0.1.0-dev, later (2026-11-05): a model library and the meteor's white crystal (compiled, not yet run in game)

Dimitri's "default for all" on `docs/reports/OPEN_DECISIONS_2026-11-05b.md` item 10: the part gets a model library and
a temporary white crystal for Mind the Meteor's node; the "white crystal" line stays.

- **The library.** Tools > RogueWavesMode > Build All Assets now also builds each model of
  `Assets/RogueWavesModeMod/Editor/RogueWavesModeModels.cs` into `RWAssets_Library/Models/<Name>` (`<Name>/Vis/<Name>_Export`,
  mesh and the 8x8 palette under `Assets/RogueWavesModeMod/Art/Models/`); `RogueWavesModeLowpoly.cs` is the kit's `Lp`
  builder with the mode's palette. Its `build_log.txt` counts the models.
- **Lit like the game's.** `RWModels.Get` (`Code/25_Models.cs`) moves the model's renderers once onto a copy of a game
  material on the "Shader Graphs/General Tower" shader (the stock ore's own, else Smacky's archer tower's) with the
  mode's palette as `_MainTex`, the way the vessel parts light their towers. Read from the game's code
  (`Entity_Vis_Resource`): a resource node's look flashes `_Overlaycolor`/`_Overlayamount` on every renderer of its
  model whose shader is that one, on "Hurt", so the crystal flashes when mined like a stock ore.
- **The meteor.** `RWObjectives.MakeMeteor` sets the copied ore entry's `Visprefab` to `RW_Meteor` (a `Sprite` style
  becomes `Model`; the ore's own look stays if the library has none). The boot log's `meteor:` line says which look it
  got and what the ore's look was. The gaps self-test (`ROGUEWAVESMODE_GAPS`) checks the spawned node wears it.
- Compiled here (release, self-test and gaps builds, 0 errors; editor clean) and rendered with the cloud preview; not
  built in Unity, not seen in game. For the next PC round: the boot line, the node on a map (size against a stock ore,
  white against the grass and the darkest map), the hit flash, and the gaps check.

## 0.1.0-dev, later (2026-11-05): a map's lighting, kit 1.13 (run in game: 23/0, Light_round1)

- `Update` calls `ModKitLight.Update()` every frame on every machine: the pack's one holder of a lighting look (kit
  `ModKitLight`: fog, ambient, sun, a borrowed stock grade). With no look set it does nothing; no weather or map sets one
  yet (a design call, `docs/reports/LIGHTING_2026-11-05.md` §5).
- The gaps build's `light` part (`92_GapsLight.cs`, `-rwonly light`, its own launch): L0-L6 on one calm map, a picture of
  each look with `ROGUEWAVESMODE_SHOTS`, and the 55 stock lighting groups' numbers in the log.
- A weather look would run on every machine from `RWRejoin.MapWeather()` (the synced names), never from the host alone.
- PC, 2026-11-05 (`docs/reports/LIGHTING_2026-11-05.md` §6): the `light` part passed 23/0 on Map_Skyhook with 16
  pictures; the self-test with every part on passed every weather check (W0-W3) and failed Spring Tide's Tide gain twice
  (`96_Flow.cs`, `0 -> 0.0375`, B7's `RWDirections.TideEffects`/`Grow`): the test's setup, not the effect (this file's first entry).

## 0.1.0-dev, later (2026-11-05): the clean-up, kit 1.12 (compiled, not yet run in game)

No number or behaviour changed (`docs/reports/BUILD_UNBUILT_2026-11-05.md` §2):

- one tower list a tag a frame for the sweeps (`RWAny.Owned`);
- Mason's Trowel and Tar Bucket mend through `RWUtil.Mend`;
- Lamp Oil and Gull Feather share `RWAny.DrownedBy`, Clam Shell and Cork share `RWAny.SetChain`;
- `RWDirections.Top` keeps each id's parse, `RWHold` keys by the thing, `RWCardinals.Def` keeps its answers;
- Fine Sieve reads the purse through `ModKitUtil.Purse`, Fog Horn and Tin Whistle slow through `ModKitUtil.SlowOn`;
- Spare Plank skips its scan while the plank stands, and Signal Flare looks four times a second while there is no Basin;
- `KitOf` and seven counters nothing read are gone.

## 0.1.0-dev, later (2026-11-05): the objectives' icons

Compiled, not yet run in game (A3, scope `14` Look lines). Five 12 x 12 icons, `RogueWaves_Objective_Meteor`, `_Hull`,
`_Clock`, `_Hands` and `_Nest` (`Art/Temporary/`, `concept-tools/temp_art/objective_icons.py`), go into the icon table with the
Board's (`RWContent.IconNames`). `RWObjectives.Icon` names the posted one's, or the Board's when the icon table lacks it; the
Board's Take card (`RWObjectives.PostedOn`, `CardText`) and the DONE popup show it. Mind the Meteor's node itself is still a stock ore
node's copy: a white crystal would be a model, and this part's library holds sprites only. `MakeMeteor` logs once what
the ore's look is made of (`meteor: the ore's look is ...`), for the round that decides how to give it one.
For the PC: the Board's card and the DONE popup for each objective; that log line.

## 0.1.0-dev, later (2026-11-05): Claude's Trinkets and Cardinals

Compiled, not yet run in game (scope `12`, B11). Sheets in `17_Cardinals.cs` `Mine` (each with its scope line as the
in-game line and the look `Cardinal_<Name>`, drawn by the art PR; the crest stands in until then); what they do is
`24_Trinkets.cs` (`RWTrinkets`), host only, the stacks read four times a second (`RWCardinals.Held`, `Stacked`):
- **Brass Spyglass**: the vessel's Range Add +0.5, or +0.25 for a melee one; "ranged" is a vessel with a `Projspeed`
  stat (Sno Sno, Yossele, Uloe).
- **Gutting Knife**: the vessel's own hit (`RWAny.Extra` hits left out) on a foe above half health, `Damagemulti` x1.2 a
  stack, on its `Interact`.
- **Oil Can**: Attackspeed Multi +5% a stack on the holder's towers (not the vessel, its clones or a ghost).
- **Tar Bucket**: a tower with no hit (no drop in health, no `Lasthittime`) for 4 s mends 1% of its health a second a
  stack.
- **Rubber Duck**: the Basin's own respawn handler reads the core's `Respawn` stat as the vessel dies, so a first and a
  last handler on the vessel's `Despawn` lower it by 20% a stack (at most 60%) for that moment and put it back (a Fixed
  stat's Value follows its Calc on every recount).
- **Spare Plank**: the vessel's own (Defense) button costs nothing while no plank stands (Hero_Buildsell prices a build
  by the button's `Goalvalue`, which is what a host's own click reads too); the wall built at no price is the plank, and
  the button's price comes back until it falls. Argo (his Drill Rig is no wall) and Buster are never offered it. The
  core's Processor Jr slot is the team's and is left alone. The Any Cut Costs Direction skips the held button.
- **Punch Card** and **Pearl Oyster**: a pair (`RWCardinalDef.Clash`): one held, the other is never on the holder's
  shelf and the shop refuses it ("Clashes with ..."). Punch Card: every price 20% off, rounded down (`PriceFor`, and
  `PriceMine` on a machine from the vessel's mark). Pearl Oyster: +1 Pure Aura each Tide's break while nothing was bought
  on the map; the Oyster's own buy doesn't count, a Direction rank does (`RWPlayer.Bought`, not saved).
- **Bottled Storm**: on buying, 5 Tides of 25% more damage on the vessel; when they end, a random Direction of theirs
  below rank III goes one up (`RWDirections.NextRanks`), or Pure Aura 20. A second stack's storm follows the first.
  `RWPlayer.StormLeft` and `StormsDone` are the save line's eighth field; a line from before reads 0/0.
- **Oilskin Coat**: `1 - 0.9^stacks` of the hits on the vessel do nothing, judged after the other mods' fronts and
  before Clam Shell (`RWAny.AfterFronts(..., RWAny.ClamHook)`).
- **Gull Feather**: an elite (a stock modifier's upgrade or the pack's `RW_Modifier_`) whose last hit was the vessel's
  adds 8 s of Damage Multi +0.2 and Speed Multi +0.15; more elites lengthen it.
- **Cork Lifebuoy**: a hook right after the core's own (`Entity_Ai_Main_Core`, which spends its once-a-map stand
  first) turns the next killing blow into 1 health, once a map, with the game's flash and a warning.
- **Signal Flare**: every 20 s, the Aura Gavel's bolt from the Basin for 15 a stack at the foe with the most health
  within 8 tiles; ready and waiting when no foe is near.
- **Cargo Crate**: one more card a stack on the shelf (at most 6); the first three are the shelf without it.
- **Tin Whistle**: once every foe of the Tide's row has come (its contents' Counts, the row's beings only), the last 3
  standing move 30% slower (`ModKitUtil.SlowOn`) and take 25% more damage.
- Self-test: the gaps build's rules part checks the pair, the prices, Spare Plank's vessels, the Crate's shelf, the
  Oilskin chances and the storm's save line; the new part "trinkets" (`92_GapsTrinkets.cs`) buys each on a calm map
  with Smacky and measures it on the vessel, an archer, a wall, a foe or the Basin. The flow's buy-everything loop and
  its shop click leave these out (their effects would change the flow's later checks).

## 0.1.0-dev, later (2026-11-05): the base vessels' Directions no held stat carries

Compiled, not yet run in game (scope `11`). Sheets in `16_Directions.cs` `Mine` (no `Stat`); what they do is
`23_BaseDirections.cs` (`RWBase`), host only, on each holder's own things, the holders read from `RWAny`'s crews four
times a second:
- **Mason's Trowel** (Smacky): his walls' Health Pmulti +20 / 40 / 60% (`RWHold`); rank III mends 1 a second once 3 s
  pass with no hit, seen by a hook first in each wall's chain (a hit with its own Damage leaves no `Lasthittime`) and by
  a drop in health for the moments the hook is off.
- **Snow Shovel** (Sno Sno): the snow on each tile he leaves comes up to 30 / 40 / 55, read every frame as his tile
  changes; a new snow waits until its AI lifts the Level cap to 100 (`Link_Stats.Value` clamps to `Value_Calc`). His own
  +75% on snow above 20 then works on his trail.
- **Worm Tin** (Boutman): Boutbro's `Fishcost`, `Barrelcost`, `Boatcost` Multi -10 / 20 / 30%, then his own `Countfish`,
  `Countbarrel`, `Countboat` by `SendMessage` (private methods, one float), so the price follows at once.
- **Open Sign** (Fie): the Rest Stop's regulars count is a private field read only when Regulars lands. With no
  `System.Reflection` (kit `docs/01`), `RWBase.Open` lands a copy of the Regulars sheet with `Perm_Count` raised (the
  sheet every Rest Stop shares is never changed), puts back the `Duration` hook chain and the look it rewrites, and lands
  Buffet again if held (its throw outranks Regulars'). It lands again whenever the AI or its `Duration` chain is not the
  one it left. Without Regulars it does nothing.
- **Long Leash** (Buster): the game's -1 (`Rangemod`, set at each slow tick while he dashes and still on at the first
  tick after, when he looks for a foe to bite) is found among his Range modifiers the first time he dashes and met by a
  +1 every frame; rank II and III multiply his dash hits' `Damagemulti` on his `Interact`.
- Self-test: the gaps build's new part "base" (`92_GapsBase.cs`) takes each vessel to a calm map: a wall hit and left,
  a walk on fresh ground, Boutbro's boat price, a Rest Stop's regulars counted through its own `Duration` hook, a dash
  and the moment after it.

## 0.1.0-dev, later (2026-11-05): the Directions any vessel can take

Compiled, not yet run in game (scope `11` (Any), B11; `docs/reports/DIRECTIONS_CARDINALS_2026-11-04.md` §4.1 and
`ROGUELIKE_DIRECTIONS_CARDINALS_2026-11-04.md` §3.1 say what each rides on). The sheets are in `16_Directions.cs` `Mine`;
what they do is `22_AnyDirections.cs` (`RWAny`), host only, reading the ranks a player holds four times a second:
- **On the player's things, twice a second** (`RWHold`, one kept modifier per thing and key, kit `docs/17` §11.1 rules
  4-7): Wound Spring (towers' Attackspeed), Magpie Nest (vessel's and towers' Damage, per Trinket stack and Cardinal,
  the lobby's picks counted), Salt Barrel (the vessel's Health Pmulti, and its mend while hurt), Spare Hands (Units on
  every unit building, then the base vessels' own count message, verified in RogueLab round 7; Boutman's barrels'
  `Vacantcurrent` too; Respawn on them and the Bouthouse). Spare Hands is not offered to Sno Sno, and not to Yossele
  either: his only units, the Cordyfriends, rise from foes, not from a building with a count.
- **On the vessel's hits** (its `Interact`, re-said ten times a second): Galley Kettle, Notched Oar (Damagemulti x2 before
  the foe takes it), Chain Shot (the jumps a frame later, to the nearest foe within 2 tiles of the last one struck), Fish
  Hook (six ticks over 3 s). Every hit these deal carries `RW_Extra`, so none of them counts it as the vessel's. Kettle,
  Oar and Chain count attacks: the hits an attack lands in one frame count once (Oar doubles each of them, Chain jumps
  from the first), so a vessel that strikes five foes at once doesn't fill Galley Kettle five times faster. Fish Hook
  rolls for each foe struck.
- **On a hit the vessel takes**: Clam Shell, saves counted per map. Its hook sits after every other mod's front hook
  (Uloe's SURGE cut, Made of Tougher Stuff) and before the game's first handler, which takes the health
  (`Helper_Damagetools.Dodamage`), so it judges the blow as cut; it reads armour, overshield and ore health as `Dodamage`
  does. An upgrade AI on the vessel that changes the hit still acts after it.
- **On a foe the vessel drowns** (`Game_Entity.Ondespawn`, its `Lasthit`, set within 0.25 s: a hit with its own Damage
  never moves `Lasthit`, so an older vessel hit doesn't count): Lamp Oil's burst, credited to the vessel, so a foe the
  burst drowns bursts too. Fire Ship's burst, on each Chain Shot jump, hits the foes around the struck one,
  not the struck one itself, as Lamp Oil's does.
- **Around the vessel**: Fog Horn, the strongest holder's slow on each foe, four times a second, taken off as it leaves.
- **At a break**: Fine Sieve (with the map's gain multiplier, as Compound Interest); Tide Pool in `TideEffects`, an Add
  to a tower's max health taken off at the next break.
- **The offers**: Spare Compass in `RWDirections.Roll` (4 / 4 / 5); its redraw is a "Draw again" card (`RWVote.RedrawMark`,
  order `RedrawPick` -2), once a map, with a full pick's time again (the old cards come back if nothing else is left). A Duo (`Needs = "Duo:A+B"`, one rank) is offered only
  to a player holding both; Wound Spring and Spring Tide share the Clash name "Spring".
- No look of their own yet (pitfall #114); their card pictures wait for the art (A2), so the catalogue's "a picture of its
  own" check fails for them until then.
- Self-test: the gaps build's "rules" part checks the Duos, the pair, Spare Hands' vessels and Spare Compass's counts;
  its new "any" part (`92_GapsAny.cs`) measures each one on a real archer, Mining Hut, foe or vessel. The flow test no
  longer grants these (Tide Pool would grow the tower its Spring Tide check measures); its line meant to drop the Any
  Directions after the reach check never matched an id and is gone, so the flow runs as in round 3.

## 0.1.0-dev, later (2026-11-05): the decisions' build items

Compiled, not yet run in game (`docs/reports/DECISIONS_APPLIED_2026-11-05.md` §3; PLAN Part C has the PC list):
- **B1** the first "Stay" on a won map votes on the next map there and then (`20_Vote.cs` `AskMap(true)`, the cards say
  "When the party leaves"); leaving later sails to it with no second vote. A won map left with no vote at all (the host
  quit right after Tide 15) asks the host once on the boat (`05_Boat.cs` `AskHere`): the boat has no channel for a
  friend's pick, so the host picks, said in chat, and the stock arrows still steer.
- **B2** the lobby's Cardinals are the campaign's from its first map (save key `RW_Campaign_Lobby`); on the boat they are
  put back, and before that a pick the shop doesn't sell comes off (`17_Cardinals.cs` `RecordLobby`, `LockLobby`;
  MODE-D9 closed, MAX MODEB-4 with it: a lobby pick never reaches the shelf).
- **B3, B4** the shelf: 2 Cardinal slots and 1 Trinket slot, from a seeded shuffle that skips what a player can't buy
  more of (6 copies; a stock one held, lobby picks too), read from what syncs on a client (`MaxedHere`) and from the run
  on the host (`MaxedFor`), rolled once a break per reader (`ShelfFor`): a card bought stays on it as "Held" and
  pulls no new one in until the next break. Every held Trinket stack is on the vessel's panel ("X", "X x2"; a unit Trinket by a mark
  with no script). Held Cardinals past the row's 8 show on the run line as "+N Cardinals" (RW_Run's Goalvalue).
- **B7** Spring Tide and Tidal Mend run on the mode's own effects at each break (`16_Directions.cs` `TideEffects`): no
  stock Beacon or Regen, no `Resistadd` on Fie's walls (pitfall #159), Mend's heal at the tower's level each Tide (flat since 2026-11-06, above).
- **B15, MODEB-8** Made of Tougher Stuff skips only the player's own vessel (Boutbro gets it); the meteor node takes
  12 + 3 x map hits (`Healthresource` 11 + 3 x map).
- **MODEA-3** in a campaign the host keeps the boat's lead (`KeepLead`, said once). **MODE-A3** the Basin is the main
  core on a map with sub-cores (`RWUtil.PlayerCore`). **MODEB-7** VENDCINT never lands mid-Tide from the 90 s fallback.
- Self-test: the flow, the gaps and both two-player builds follow the stay's map vote; new checks B1, B3, B4.

## 0.1.0-dev, later (2026-11-05): the code review at max

Kit 1.11, compiled, not yet run in game (`docs/reports/CODE_REVIEW_MAX_2026-11-05.md`): a counted chart map ends staging,
and picking the harbour clears the next map (MODEA-1, -2); an ended campaign's Cardinals leave the lobby list (MODEB-1);
the staging map's vote shows row 1 on a friend's screen (MODEB-3); Voice in a Bottle's catch-up uses the stock filter
(MODEB-5); the map's health upgrades are registered on every machine (P1-7); a rejoin restarts only halves stopped at
the load (P1-3); a purchase shows at once (P1-8). The gaps build compiles without a play build (P1-6); its probes and
checks for MODE-D1, the F key, the Cardinal row, SONAR, N4 and the save fuzz's F4 can now fail.

## 0.1.0-dev, later (2026-11-05): crisp icons

Compiled, not yet run in game. Dimitri saw the Rogue Waves button's icon, and others like it, jittering: every pack
picture the game drew stretched to an uneven number of screen pixels per art pixel (pitfall #156). Now:

- **The menu's crest** (`12_Menu.cs`, `RWMenu.Image`): `RWAssets.Crisp` sizes the 32 px crest to 32 units in Play's
  48-unit box, its corner on a whole screen pixel, instead of stretching it to 48.
- **Cards** (`11_Dialog.cs`, `RWDialog.Fit`): every card's `Icon` is crisped at the list's own scale once the dialog is
  placed. Card art that stood at 32 px now uses the 12 px crest (New campaign, a Direction's or Cardinal's fallback,
  the dialog-look test) and `RWDialog.Number` draws 16 px (wider only for five digits).
- **Icon_Types** (`08_Icons.cs`): the weathers and the library icons go in as 16 px boxes (`ModKitAssets.Pixel`'s box,
  `ModKitAssets.Boxed`, kit 1.10), so the HUD's SHOP, FUND TRANSFER and OBJECTIVE draw one unit per pixel; the
  warning popup draws them three to one. Pure Aura in Icon_Stats stays 12 px. The part's own copy of `Pixel` is gone.

## 0.1.0-dev, later (2026-11-05): the code review's fixes

Compiled, not yet run in game. The confirmed findings of `docs/reports/CODE_REVIEW_2026-11-04.md` that need
no word from Dimitri, fixed in place and named in the code by their ids: the bonus break once (D1), objectives reset
each map (D2), Mind the Vessels counting only vessels (D3), questions kept for a vessel that is down (D4), the map's
health without the Hardened look (D5), the leave vote holding the break (D6), a question closing only its own dialog
(D7), every Basin watched (D8), VENDCINT's buttons freed after a throw (D10), the staging flag saved (A1), the boot's
steps guarded (A2), a mid-map Cardinal reaching what stands (C1), Cheap Refits and Cut Costs as kept cuts, also on the
Bouthouse and Fie's walls (C4, C6, C7, C8), Quickening's speed after a drop (B1) and the rejoin's three (B2-B4). What
was left and why, and the PC test list: `docs/reports/CODE_REVIEW_FIXES_2026-11-05.md`.

## 0.1.0-dev, later (2026-11-05): save fuzz and three damaged-save guards

Quality check 4 (`docs/reports/QUALITY_CHECKS_4-7_2026-11-05.md` §1). `98_SaveFuzz.cs` (`ROGUEWAVESMODE_SELFTEST` only)
runs seven checks, F1-F7, over the campaign save's readers and writers; `99_SelfTest.cs` calls it on the boat after C0,
before its campaign starts, and puts back every key it touches, so the self-test should read 155 (148 + 7).
`Tools/cloud-check/save_fuzz.py` runs the same file here against the methods copied from this code. What it found, now
guarded (a normal save never takes these paths):

- `RWCampaign.Saved` cuts a route that leaves the chart back to its last charted map (one warning) and drops the
  replay and playing columns saved for it: before, Continue waited on the boat for a world `RWWorld.Ensure` never builds.
- `RWRun.Load`, `Forget` and `SavedCrew` read at most `RWRun.MaxSaved` (64) players, and `Save` writes no more: a
  damaged count of `2147483647` read the save two billion times.
- Self-test builds only: `RWUtil.HoldFlush` keeps the fuzz's writes off the disk until it has put every key back.
- `RWRun.Load` and `Decode` drop empty ids (`"a,,b"`), and a negative Pure Aura loads as 0.

**State:** compiled here (release, `_SELFTEST`, `_SELFTEST,_GAPS` at 0 errors); the fuzz passed 7/7 here on a stand-in
save, and failed F2, F4 (hung), F6 on the code before the guards. Not run on the PC.

## 0.1.0-dev, later (2026-11-04): Trinkets and Cardinals

Dimitri: "Rename Vessel Cardinals as Trinkets and Team Cardinals as just Cardinals." The shop card's kind line reads
**Trinket** or **Cardinal** (`18_Shop.cs`), the SHOP button and VENDCINT's line say "Cardinals, Trinkets" (`15_Content.cs`).
In the code: `RWCardinalDef.Trinket` (was `Vessel`), `RWCardinals.IsTrinket`, `GrantCardinal`, `GrantAllCardinals`,
`RWPlayer.Trinkets` (was `Cardinals`), `RWRun.Cardinals` (was `Team`), `RWRun.CardinalsKey`; the self-tests' check
lines name the new kinds (same checks, same count). **Kept on purpose, so a saved campaign still loads:** the save key
`RW_Run_Team`, the player line's field order, and the held ids `Cardinal_<Name><n>` (Vampire Dentures is
`Cardinal_Dentures`); also the icon's asset name `Cardinal_VampireDentures`, which a rename would make a PC build
change. The class stays `RWCardinals`: it sells both kinds and the stock Cardinals. **State:** compiled here (release,
`ROGUEWAVESMODE_SELFTEST`, `_GAPS`, `_FEEL` at 0 errors as before; `_TWOPC`'s Netcode-only errors the same before and
after, checked with a transport stub); not run on the PC.

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
