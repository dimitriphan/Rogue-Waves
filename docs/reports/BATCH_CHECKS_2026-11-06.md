# Batch checks (2026-11-06)

Machine-read by `claude-haiku-5-5` (effort medium) through the Message Batches API, batch `msgbatch_01XM6GKkEVGqJgqX7PVfJCeP`, on base `86988e1`, checks text, scope, design, docs, by `TDS_Mod/Tools/batch/run_checks.py`. Cost: $1.19 at batch prices. Every row is a lead to confirm by hand before it changes code, docs, art or the scope: the model reads, it does not run the game. Nothing ran on the PC.

## 0. Summary: the leads to confirm first

The first real batch run (Haiku 5.5, base `86988e1`). It came in two parts: this report (25 requests, $1.19) and
`BATCH_CHECKS_2026-11-06b.md` (the 7 requests whose answers were cut at their length limit here, design for the four
vessels and scope `07`, `08`, `11`, rerun with `--effort low`, $0.63). Together $1.82 of the $100 monthly credit, every
scope file but `00`, `02`, `15`, every design page, the text and the docs read. Nothing below is fixed: each row is a
lead for a thread to check against the code by hand, and the model was sometimes given too little to judge (see
"Likely noise"). The cost estimate in the batch README undercounts: `plan --exact` counted 4.13 million input tokens
against about 2.78 million estimated, and the four design and three scope answers need more than their output limit at
medium effort.

**Worth a look (one line each; the full row is in the section named):**

- Meteor objective off by one map (scope `14`:14; §2.1): code gives 15 hits and 9.5 tiles on map 1 (`12 + 3*map`,
  `8 + 1.5*map` with map 1), the scope says 12 and 8 at the first map.
- Pearl Oyster (scope `12`:126-137; §2.1): buying the Oyster itself does not stop its pay, on purpose by the comment
  above `OnBuy` (`24_Trinkets.cs:476-479`), while the scope reads as any buy. Sharper edge: `p.Bought = id !=
  "Cardinal_Oyster"` sets `Bought` back to false when the Oyster is bought after another buy, so its pay starts again.
- Shop's excluded Cardinals (scope `12`:5; §2.1): `NotSold` also drops `Artifact_Rock`, which the scope does not list.
- Uloe (scope `10`; §2.1): Plug In on a safe socket can reach Light 10, the scope's note says at most 8; Supernova
  bursts Photons in place rather than bringing them back; Bounce Card, Wide Flood and Lighthouse feed teammates'
  buildings too (scope: "your buildings"; may be the shared-light decision, cap 4).
- Mode (scope `04`; §2.1): a missing stock elite's replacement can be one of the campaign's four; only the meteor
  objective grows with the map; very big islands are shrunk below the base game's size.
- Yossele: Garden Weeds stacks last 15 s, the scope's 3 s is only the hit window (06b §1.1); Spare Hands also excludes
  Yossele, the scope names only Sno Sno (06b §1.1); Deep Breath's 2 s counts from reload, by the code's own comment.
- Spool: nothing holds the Yarn Stash to one (scope `08`:64); Pin Barrage leads nowhere while Iron Thimble is unbuilt
  (queued, B19); the thread colour is `6FAE7E`, the scope's second sage is `60A871` (06b §1.1).
- Design pages (06b §2.1): Argo's Salt Sting stings 1 a second unpinned by Brine's doubling, page says 0.5; Argo's
  page cites kit 1.6, code header 1.7; Spool's test plan says at most 4 Crafted, code 6; Yossele's page says any level
  2 leads to Bonsai and Rubbery, only the tier-3 nodes do; Uloe's Mirror reaches level 1 at any Light above 0.
- Text (§1): Uloe's Death Ray text says both "never cools down" and "cools after
  3 s"; Argo's Mantle Tap says "every few seconds", its other line "every 20s"; two log lines in
  `RogueWavesMode/Code/09_Rejoin.cs` (374, 416) and Uloe's "one slow a Flood Light" do not parse.
- Docs (§3): `docs/reports/README.md`'s STOCK_TRINKETS row still says 13 Trinkets and 16 Cardinals (his answer made it
  17 and 12); PLAN's "API credit set up" entry keeps the first cost guess ($3.50 / $0.70); the three cited
  paths that name nothing are on purpose (`TDS_Mod/Handoff/Argo_next/` as a former path, `pc/QUEUE.md` as the stale
  project-files lock, `Art/Temporary/game_size/` as a pattern under each part): a lead for the free pass, not the docs.

**Likely noise, checked or explained:** "Woolen Lining" (§1) is his own spelling in scope `08`:170, already an open
question (`docs/reports/TEXT_AUDIT_2026-11-05.md` §4 A6, which leans to names as he wrote them). "Made of Tougher Stuff" not found (06b §1.1, Spool design) is a false lead:
it is in `RogueWavesMode/Code/16_Directions.cs:31`, which the Spool requests were not given. The part versions
(Argo, Yossele read 0.1.0-dev, the pack's version) are the pack's numbering, not a bug. The missing "Kind" labels on
objectives and "By" fields on Directions and Cardinals, the missing 2-4 player check, "Streamlined Economy Changes"
as a name, Uloe's look being the temporary sheet, Argo's 0.5 aim slack and 2.1 ore reach, the 0.01 damage tolerance
and the strict "more than 25%" on the clock are tolerances or labels to decide on, not behaviour bugs. Health regen
"0" for Spool and Uloe is left to the template's stock value: worth one look at what that value is.

## 1. Text

8 findings. Recipe: `docs/reports/TEXT_AUDIT_2026-11-05.md` §1.

| Where | Text | Kind | Problem | Suggestion |
| --- | --- | --- | --- | --- |
| `Argo/Code/02_Data.cs:382` | Mines much faster, and every few seconds a quake pins every foe in its Tremor | unclear | Mantle Tap says a quake pins every foe in its Tremor 'every few seconds', but S127 says the quake happens 'Every 20s'. The two descriptions contradict each other. | Make the interval match in both strings, e.g. 'every 20 seconds a quake pins every foe in its Tremor' with S127 unchanged. |
| `RogueWavesMode/Code/09_Rejoin.cs:374` | rejoin: started again the client halves the late player object stopped: | grammar | The log line is missing the word "what": "started again the client halves the late player object stopped" does not parse. S380 has the same structure correctly with "what". | rejoin: started again what the late player object stopped, the client halves: |
| `RogueWavesMode/Code/09_Rejoin.cs:416` | things their look once their groups had loaded: | unclear | "things their look once their groups had loaded" does not parse as English, so a player cannot tell what the list refers to. | Rewrite the fragment so it reads as a clear list, e.g. "things that looked right once their groups had loaded:" |
| `Spool/Code/02_Data.cs:281` | Woolen Lining | spelling | "Woolen" is the US spelling; the pack uses British English, which is "Woollen". | Woollen Lining |
| `Uloe/Code/02_Data.cs:222` | It cools only after 3 s with nothing to beam | unclear | Contradicts S971 "Never cools down": the Death Ray is said to cool after 3 s with nothing to beam. | Reword so it matches the no-cooldown design, e.g. "Only cools after 3 s with nothing to beam" alongside a removal of "Never cools down", or change S971 to say it cools slowly. |
| `Uloe/Code/02_Data.cs:300` | and slowed 20%, one slow a Flood Light | grammar | The sentence does not parse: "one slow a Flood Light" is missing a word, so the rule is unclear. | Reword to something like "and slowed 20%, one slow per Flood Light" (or the intended rule). |
| `Yossele/Code/02_Data.cs:314` | Chance to recharge seeds quicker when enemies recently damaged die. | grammar | The fragment "when enemies recently damaged die" reads as a broken sentence and is unclear about who damaged what. | Reword to "Chance to recharge seeds quicker when enemies that recently damaged your towers die." |
| `Yossele/Code/02_Data.cs:342` | Its roots mine every ore node in reach, fully grown it drops aura fruit | grammar | Comma splice: two independent clauses are joined by a comma only. | Use "Its roots mine every ore node in reach; when fully grown, it drops aura fruit." |

## 2. Scope against code

Recipe: `docs/reports/SCOPE_CODE_AUDIT_2026-11-05.md` (marks as there). Code paths are relative to `TDS_Mod/ModSource`.

| File | MATCH | DIFFERENT | MISSING | UNBUILT-QUEUED | UNBUILT-BY-DESIGN |
| --- | --- | --- | --- | --- | --- |
| 01_Expansion.txt | 12 | 0 | 1 | 7 | 1 |
| 03_Bug_Fix.txt | 5 | 0 | 0 | 0 | 0 |
| 04_Rogue_Waves_Mode.txt | 64 | 3 | 0 | 0 | 3 |
| 05_Gameplay_Rundown.txt | 8 | 0 | 0 | 0 | 4 |
| 06_New_Mechanics.txt | 29 | 1 | 2 | 0 | 0 |
| 09_Vessel_Argo.txt | 106 | 3 | 0 | 0 | 1 |
| 10_Vessel_Uloe.txt | 104 | 7 | 1 | 0 | 2 |
| 12_Cardinals.txt | 33 | 2 | 1 | 0 | 1 |
| 13_Enemy_Modifiers.txt | 20 | 0 | 0 | 0 | 1 |
| 14_Optional_Objectives.txt | 56 | 4 | 5 | 0 | 0 |
| 16_Base_Vessels.txt | 0 | 0 | 0 | 0 | 73 |

### 2.1 DIFFERENT and MISSING (to confirm by hand)

| File | Lines | Says | Code | Mark | Note |
| --- | --- | --- | --- | --- | --- |
| 01_Expansion.txt | 01:17 | Players: 2 to 4 | `No 2-to-4 range in the pack code. 21_Difficulty.cs:120-122 scales for players beyond the first (CountPerPlayer), and 21_Difficulty.cs:54 passes RWRun.Sessions().Count` | MISSING | Looked for a minimum or maximum player count and a 4 in the pack code: none. The stock lobby may enforce the range, and the pack has no check or queue item for it. |
| 04_Rogue_Waves_Mode.txt | 04:19 | A stock elite a map can't load is replaced by the next one outside the four, so no map plays easier than its number. | `RWDifficulty.Pool (21_Difficulty.cs:276-286): reserve list is the campaign's four first, then the other stock elites, then the other pack modifiers; a missing one is skipped for the next in that list.` | DIFFERENT | The code's reserve puts the campaign's own other picks before the outside ones, so a replacement can be one of the four (the code's own comment says the campaign's first). The scope says outside the four. |
| 04_Rogue_Waves_Mode.txt | 04:22 | Each map is harder than the one before: more enemy modifiers, higher spawn rates, harder optional objectives and more enemy health. | `Modifiers 21_Difficulty.cs:267-288; spawn rates 21_Difficulty.cs:122-123; health 21_Difficulty.cs:115-117; objectives scale only in RWObjectives.SpawnMeteor (19_Objectives.cs:278-279 distance and 306 hits by map). Pay and the other four objectives (Hull, Clock, Hands, Nest) are flat (19_Objectives.cs:14-21).` | DIFFERENT | Only Mind the Meteor gets harder with the map. The other objectives' pay and targets do not change by map. |
| 04_Rogue_Waves_Mode.txt | 04:62 | Islands: each map shows its own island from the base game's world maps, at the base game's size, in the home world's water; a map with no island of its own borrows one. | `RWWorld.StockIsland (04_World.cs:380-422) copies the stock node, with scale = min(1, 22/width, 26/height) (04_World.cs:405) and the comment 'A very big one is shrunk to stay within its row' (04_World.cs:307); Sea from World_Omeanchor (04_World.cs:474-509); IslandFor borrows from islandPool (04_World.cs:364-378).` | DIFFERENT | Islands wider than 22 or taller than 26 units are shrunk to fit the row, so they do not show at the base game's size. Islands borrowed and home-world water match. |
| 06_New_Mechanics.txt | 17 | By: each Direction says who made it, Dimitri or Claude (points to 00_Read_Me [Values]) | `16_Directions.cs:7-18 RWDirectionDef has no author field; attribution appears only in code comments (16_Directions.cs:28, 50-52)` | MISSING | Looked for an author or By field on RWDirectionDef and the templates; none. Nothing in the plan queues it. |
| 06_New_Mechanics.txt | 27 | Shown as a pearl white counter on each player's vessel, a colour no base aura uses | `14_Run.cs:324-325 ModKitCounter.Set(v, PureStat, p.Pure) with no colour argument; 14_Run.cs:531 Pearl colour is set only on the HUD run line label` | MISSING | Looked for a colour on the vessel counter; the pack sets none, so the pearl look is not in this code. Pearl appears only on the run line label. |
| 06_New_Mechanics.txt | 33 | This is the expansion's 'Streamlined Economy Changes' | `18_Shop.cs:402-405 RWFund class named Fund Transfer; no 'Streamlined Economy Changes' text anywhere in the pack code` | DIFFERENT | The code's name for the feature is Fund Transfer; the scope's alternate expansion label is not in the code. |
| 09_Vessel_Argo.txt | 9:22 | Status: Argo 2.0.1 designed, built, passes PC self-test | `02_Data.cs:16` | DIFFERENT | ArgoData.Version is 0.1.0-dev (the pack version); the scope and PLAN name the Argo part 2.0.1. The Version constant is not 2.0.1. |
| 09_Vessel_Argo.txt | 9:209 | Range 5 tiles: how far AIM can place the Tremor | `02_Data.cs:366; 02_Data.cs:173; 07_TowerBrain.cs:762-767` | DIFFERENT | Range=5 is stored, but Aim accepts a spot up to Range plus AimSlack 0.5 (5.5 tiles), not Range alone. |
| 09_Vessel_Argo.txt | 9:210 | Ore reach: 2 tiles | `02_Data.cs:156; 07_TowerBrain.cs:822-838` | DIFFERENT | OreReach is 2.1 (a 0.1 tolerance over the scope's 2 tiles). |
| 10_Vessel_Uloe.txt | 10:12 | Look: glass bulb head with face, ribbed screw base on a tall post, yellow electric arms, cord to a plug | `03_Assets.cs:7-28` | DIFFERENT | Code loads the Uloe-Sheet frames (temporary art, docs/TEMP_ART.md). Nothing in code draws the described head, base, arms or plug; the look is the sheet's. |
| 10_Vessel_Uloe.txt | 10:13 | Towers look: towers carry glass, screw rings and cords | `04_Content.cs:138-148` | DIFFERENT | TowerVisual adds a Lit1-3 skin that draws a ring and rays round the foot. Glass, screw rings and cords are not built; towers use the temporary model. |
| 10_Vessel_Uloe.txt | 10:22 | Health regen: 0 per second | `02_Data.cs:72-75; 04_Content.cs:631` | MISSING | Looked for a Healthregen entry in HeroStats and in BuildHero. None set; the value is inherited from Smacky's stats. |
| 10_Vessel_Uloe.txt | 10:145 | Note: a safe socket gives Plug In at most Light 8, not 12 | `06_HeroBrain.cs:157-161` | DIFFERENT | Code has no cap: 4 + 2 per upgrade. A Surge bank with two more upgrades (Short Circuit, Extra Cell) gives 10, not 8. |
| 10_Vessel_Uloe.txt | 10:159 | Supernova numbers: 20 damage within 2 tiles, Blind and Illuminate level 3, all Photons come back at once, Photons give Light 3 for 15 s | `02_Data.cs:248; 08_Bank.cs:70-84, 401-416; 08_Bank.cs:161-162` | DIFFERENT | Damage, radius, level 3 and 15 s Light 3 match. But nothing returns: Supernova bursts the Photons where they stand and only fills empty slots (08_Bank.cs:79-80), so no Photon comes back at once. |
| 10_Vessel_Uloe.txt | 10:186-189 | Bounce Card: 50% of the Light it receives goes to your buildings within 3 tiles, never to another Bounce Card; cost Blue 100 | `02_Data.cs:267; 10_Mirror.cs:66-68; 05_Server.cs:201-220, 43-45` | DIFFERENT | Relay values match (0.5, 3 tiles, skipRelays, Blueaura=100). But Shine feeds every building of the team in range, not only the owner's; teammates' buildings take it as a teammate share capped at 4. |
| 10_Vessel_Uloe.txt | 10:224-227 | Wide Flood: 60% of its Light to your buildings within 3.5 tiles; can no longer be directed; cost Yellow 30 | `02_Data.cs:288; 07_Building.cs:191, 238-243, 381-385; 05_Server.cs:201-220` | DIFFERENT | Numbers match (Wide_Base 0.6, radius 3.5, aim removed, Yellowaura=30). The ring feeds every team building in range, teammates included, not only your own. |
| 10_Vessel_Uloe.txt | 10:256-259 | Lighthouse: beam turns a full circle every 6 s, 9 tiles long; your buildings it crosses keep 60% of its Light for 6 s; foes it crosses Blinded and Illuminated at level 2; AIM goes away; cost Yellow 2500 + Red 500 | `02_Data.cs:305-308; 07_Building.cs:191, 480-523, 508-509` | DIFFERENT | Turn 6, reach 9, 60% for 6 s, level 2 marks, Yellowaura=2500;Redaura=500 and no AIM all match. The building test (07:495) takes any team building, not only your own. |
| 12_Cardinals.txt | 12:5 | Shop's base game Cardinals: all but 14 named ids (Moon, Moon Core, Loop, Looped Mirror, Vintage, Same Day Shipping, Pig, Money Bag, Sandbox, Sand, Blurry Photo, Checklist, Computer, Bottom Rock) | `17_Cardinals.cs:65-69 NotSold` | DIFFERENT | NotSold holds those 14 plus Artifact_Rock, which the scope does not exclude, so the shop drops one more stock Cardinal than the scope says. The Bottom Rock 1% pay cut is game fact, not built, and is only a note. |
| 12_Cardinals.txt | 12:25 | Each Cardinal and Trinket says who made it, Dimitri or Claude | `17_Cardinals.cs:19-28 RWCardinalDef has no By field; no display text carries it` | MISSING | Looked for a By field or a text on the item; none. The sheet's By lines exist only in the scope file. The plan queues nothing for it. |
| 12_Cardinals.txt | 12:126-137 | Pearl Oyster: Trinket, +1 Pure Aura each Tide until the holder next buys something at VENDCINT on that map, starts again each map, no stacks, Blue, cost 15, clashes with Punch Card | `17_Cardinals.cs:54 sheet; 24_Trinkets.cs:488-501 Tide pay (Bought check); 24_Trinkets.cs:478-485 OnBuy; 14_Run.cs:247 Bought reset per map` | DIFFERENT | OnBuy sets Bought = false when the buy is the Oyster itself (id != Cardinal_Oyster), so buying the Oyster does not stop its pay; the scope says until you next buy something at VENDCINT, which the Oyster purchase is. Everything else matches. |
| 14_Optional_Objectives.txt | 14:12 | Kind: Mining | `19_Objectives.cs:5-9 (RWObjectiveDef has Name, Display, Line, Pays; no Kind field). The mining behaviour is in 19_Objectives.cs:273-310 (SpawnMeteor)` | MISSING | Looked for a Kind field or a label on the card and the popup; none. Behaviour exists, the kind label does not. Not queued. |
| 14_Optional_Objectives.txt | 14:14 | Numbers: 12 hits + 3 per map | `19_Objectives.cs:306 (Healthresource 11 + map*3, hits = Healthresource + 1 = 12 + 3*map), 19_Objectives.cs:309 (log says 12 + map*3)` | DIFFERENT | The code gives 15 hits on map 1 (map = 1 there). The scope's 12 at the first map needs 12 + 3*(map-1). The +3 per map step matches. |
| 14_Optional_Objectives.txt | 14:14 | Numbers: about 8 tiles + 1.5 per map from the Basin | `19_Objectives.cs:279 (want = 8f + map*1.5f), 19_Objectives.cs:293-294 (nearest standable tile to want)` | DIFFERENT | Map 1 gives 9.5 tiles, not 8. The 1.5 per map step matches. Same off-by-one as the hits. |
| 14_Optional_Objectives.txt | 14:24 | Kind: Guard | `19_Objectives.cs:5-9 (no Kind field)` | MISSING | No kind label in code or on the card. Not queued. |
| 14_Optional_Objectives.txt | 14:26 | Numbers: no damage at all | `19_Objectives.cs:262-270 (BasinClean), 19_Objectives.cs:267 (fails only when Damagetaken > start + 0.01f)` | DIFFERENT | The code allows 0.01 of damage before failing; the scope says none at all. |
| 14_Optional_Objectives.txt | 14:36 | Kind: Rush | `19_Objectives.cs:5-9 (no Kind field)` | MISSING | No kind label in code or on the card. Not queued. |
| 14_Optional_Objectives.txt | 14:38 | Numbers: the last foe falls with at least 25% of the tide's clock left | `19_Objectives.cs:33 (ClockShare = 0.25f), 19_Objectives.cs:228 (ok = clockLeft > tideLength * ClockShare), 19_Objectives.cs:202-206 (clockLeft from the Time_Wave reading)` | DIFFERENT | The code uses strictly greater than, so exactly 25% fails; the scope says at least 25%. |
| 14_Optional_Objectives.txt | 14:48 | Kind: Guard | `19_Objectives.cs:5-9 (no Kind field)` | MISSING | No kind label in code or on the card. Not queued. |
| 14_Optional_Objectives.txt | 14:60 | Kind: Hunt | `19_Objectives.cs:5-9 (no Kind field)` | MISSING | No kind label in code or on the card. Not queued. |

### 2.2 01_Expansion.txt

| Lines | Says | Code | Mark | Note |
| --- | --- | --- | --- | --- |
| 01:5 | Ships as one mod pack, RogueWavesMod, with every part inside it and no load order | `No pack assembly in the given files. RogueWavesModeMod is a separate dev mod (00:22, RWData.Part Mode at 00:137)` | UNBUILT-QUEUED | Queued as the one-part pack build (PLAN Part B Release 1, then the later parts join the same pack). The mode part exists as its own mod, so the single-pack shape is not yet built. |
| 01:6 | Four new vessels: Yossele, Spool, Argo, Uloe (scope 07 to 10) | `Vessel code is not in the files given. Spool's pending work is in the plan, not in these files` | UNBUILT-QUEUED | Queued: Spool's 22 upgrades and UNTHREAD (scope 08, PLAN Next, B18/B19). Yossele, Argo and Uloe are reported built in PLAN Part C, but their code was not in this audit's files, so those three rest on the plan, not on a code location. |
| 01:7 | Modifiers Cursed, Berserk, Aging and Dazzling so far | `21_Difficulty.cs:28 PackModifiers (RW_Modifier_Cursed/Berserk/Aging/Dazzling_Upgradeobject); 21_Difficulty.cs:247-288 CampaignPool and Pool` | MATCH | The four names are listed and drawn into each map's pool from the seed. The modifiers' own upgrade bodies are in the foes part, which is not in the files given. |
| 01:7 | New enemy types come later | `No enemy types in the mode code` | UNBUILT-BY-DESIGN | The scope itself says the new enemy types come later. |
| 01:8 | New Mechanics, pointer to 06_New_Mechanics.txt | `16_Directions.cs:1-764; 22_AnyDirections.cs:1-625; 23_BaseDirections.cs:1-308; 24_Trinkets.cs:1-803` | MATCH | Pointer line. The mechanics it points to are audited row by row under scope 06, not here. |
| 01:9 | New Roguelike Gamemode: the Rogue Waves mode (scope 04, 05) | `02_Mode.cs:8-94 RWMode.Register; 00_RogueWavesModeMod.cs:140-141 Mode and ModeDisplay; 00_RogueWavesModeMod.cs:22-35 Start` | MATCH | The mode is registered as Mode_RogueWave under the display name Rogue Waves. Scopes 04 and 05 are audited under their own lines. |
| 01:10 | Online Compatibility, pointer to [Online] | `09_Rejoin.cs:1-544; 10_Online.cs:1-148` | MATCH | Pointer line. Rejoin and the restart identity fix are built; the [Online] lines are rows 01:17 to 01:19 below. |
| 01:11 | Upgrade System [Direction], pointer to 06 and 11 | `16_Directions.cs:20-137 Mine sheets and Build; 16_Directions.cs:189-247 Roll; 16_Directions.cs:304-343 Grant` | MATCH | Directions are offered, held, ranked and granted on the vessel. Scope 11 is audited under its own lines. |
| 01:12 | Streamlined Economy: aura sharing between players between tides, which is Fund Transfer (scope 06) | `18_Shop.cs:402-444 RWFund; 18_Shop.cs:404 PerTide 50; 18_Shop.cs:407 Limit(wave) = PerTide x (wave + 1); 20_Vote.cs:91 Sent reset each break` | MATCH | Fund Transfer sends aura to a friend, capped per break, and the cap resets at each break. Only the four colour auras are accepted (Array.IndexOf over ModKitUtil.Auras, 18_Shop.cs:413). |
| 01:13 | New Cardinals and Trinkets; a new Cardinal or Trinket is a new run modifier (scope 12) | `17_Cardinals.cs:44-62 RWCardinals.Mine; 17_Cardinals.cs:65-69 NotSold; 24_Trinkets.cs:1-803; 14_Run.cs:37 Cardinals held per run` | MATCH | The Cardinals and Trinkets are built and held for the run. The run-modifier wording is design; scope 12 is audited under its own lines. |
| 01:14 | Bug fixes, pointer to 03_Bug_Fix.txt | `03_Bug_Fix.txt not in the files given; fixes in the code are checked under their own scope lines` | MATCH | Pointer line only. Nothing in the code could be checked against 03 here, so this row says no more than that the pointer is intact. |
| 01:17 | Players: 2 to 4 | `No 2-to-4 range in the pack code. 21_Difficulty.cs:120-122 scales for players beyond the first (CountPerPlayer), and 21_Difficulty.cs:54 passes RWRun.Sessions().Count` | MISSING | Looked for a minimum or maximum player count and a 4 in the pack code: none. The stock lobby may enforce the range, and the pack has no check or queue item for it. |
| 01:18 | Host: the host runs the run | `06_Campaign.cs:54-74 RWCampaign.New (host only); 06_Campaign.cs:264-272 OnBoat host check; 14_Run.cs:296-298 Tick returns unless ModKitUtil.IsServer` | MATCH | Campaign start, the map's loss and win, and the run's per-player Tick are host-only. |
| 01:19 | Pack version: everyone needs the same pack version | `00_RogueWavesModeMod.cs:32 ModKitPack.Join(RWData.Part, RWData.Version); 00_RogueWavesModeMod.cs:138 Version 0.1.0-dev` | MATCH | Each part joins the pack with its version. The check that players match is inside the kit's ModKitPack.Join, which is not in the files given. |
| 01:22 | First release: Yossele alone | `No single-part pack build in the files given` | UNBUILT-QUEUED | Queued: PLAN Part B Release 1 (Yossele's play test, sprites, balance pass, then the one-part pack build and his ship). |
| 01:23 | After it: the Rogue Waves mode with its sea chart and sea weather, Argo, Spool, Uloe, new enemies and modifiers, Directions, Cardinals, Trinkets and the rest | `Mode, Directions, Cardinals and Trinkets are in the files given; Argo, Spool and Uloe code is not` | UNBUILT-QUEUED | Queued after Release 1: Spool's 22 upgrades and UNTHREAD (scope 08, B18/B19). The mode and the Directions, Cardinals and Trinkets are built per the plan. |
| 01:24 | Download: the RogueWavesMod pack holding only Yossele, version 0.1.0; later releases add parts to the same pack (docs/PACK.md) | `No pack build in the files given. RWData.Version 00_RogueWavesModeMod.cs:138 is 0.1.0-dev for the mode part` | UNBUILT-QUEUED | Queued: the one-part pack build, version 0.1.0 (PLAN Part B Release 1). |
| 01:25 | Where: a zip you share yourself; the game has no Steam Workshop (kit docs/14 section 4) | `No distribution code. Delivery is the pack build step` | UNBUILT-QUEUED | Queued with the one-part pack build (PLAN Part B Release 1). Nothing to check in code. |
| 01:26 | Yossele outside the mode: Yossele plays in a normal game; no Fertilize in the first release, since it is unlocked only by a Direction | `Yossele's code is not in the files given. The mode's Directions (16_Directions.cs) hold no Fertilize entry` | MATCH | Checked only the mode side: no Fertilize Direction or unlock in 16_Directions.cs. Yossele's own part, and the claim that he plays outside the mode, are not in this audit's files and rest on PLAN Part C (0.2.0-dev built, play test pending). |
| 01:27 | Ready when: Yossele matches 07, carries your own sprites, passes its PC self-test and your play test, and you say ship | `Not a code behaviour. Sprites, the play test and the ship word are open` | UNBUILT-QUEUED | Queued: PLAN Part B Release 1 (his play test of 0.2.0-dev, his look and sprites, then his ship). |
| 01:28 | Status: Yossele 0.2.0-dev built from 07 and installed for the play test; sprites, balance pass and one-part pack come next | `Yossele part code not in the files given. The status matches PLAN Part C State and Part B Release 1` | MATCH | Checked against the plan's State and Release 1 lines, not against code: the Yossele part's code was not in this audit's files. |

### 2.3 03_Bug_Fix.txt

| Lines | Says | Code | Mark | Note |
| --- | --- | --- | --- | --- |
| 03:4-7 | {F Picks a Minion}: after the main vessel dies and respawns, F selects a controllable minion instead of the vessel. Check new vessels too. Built into shared code; runs with Argo, Yossele, Spool and Uloe. | `Shared/ModKitFocus.cs:21-54 (install once via ModKitPack.Mark('Focusfix'); Respawned detection), 56-77 (After: on Focus press, if Respawned, selects MainHero and moves it to the front of Entityclienttags Hero), 80-95 (MainHero: the local player's vessel by owner and Being name)` | MATCH | The shared handler does what the line says. Which vessel parts call ModKitFocus.Update is in their own code, which is not in the files audited here, so 'runs with Argo, Yossele, Spool and Uloe' is not checked from these files. The 'check new vessels' step is a test instruction, not code. |
| 03:8 | Status: built. Its PC pass on a real death ran with the fix not yet switched on; the fix is fixed and compiled since, and the next PC round checks it again. | `Shared/ModKitFocus.cs:21-38 (installed only when ModKitPack.Mark('Focusfix') succeeds, so one copy runs per pack)` | MATCH | The fix is in the code. The PC check on a real Boutman death stays open (PLAN by-hand list; CODE_REVIEW_MAX P1-1), so this row is not a PC confirmation. |
| 03:10-12 | {No Way Back In}: a player who loses the connection mid-map has no way back in because the game shuts the lobby for the whole map. Rejoining must not interrupt the others. How to see: a friend drops out and tries to come back. | `09_Rejoin.cs:59-147 (Host: seats kept per Sessionid; door opened while a seat is empty, 143-146), 152-159 (Door: Changelobbystatus_ServerOnly 'Open'/'Closed'), 188-201 (TurnAway: a player with no seat is kicked)` | MATCH | Door is checked every frame (145-146) because the game's loading screen shuts it again, as the comment says. 'Must not interrupt' is covered by the host copying seats without restarting the map, and by Restore (176-185) touching only the returning player's clientinfo. |
| 03:13 | Fix: the host keeps each player's seat (vessel, towers, aura) and opens the lobby while a seat is empty; the dropped player comes back with Join Game on the host in Steam. See docs/mode/REJOIN.md. | `09_Rejoin.cs:17-30 (Seat: Hero, Team, Active, Aura, Stats), 161-172 (Copy), 176-185 (Restore: Team, Hero, Activeplayer, Resources), 137 (chat line telling the player to right-click the host and pick Join Game), 145 (Door opened while a seat is empty)` | MATCH | Seats keep vessel, team and aura. Towers are not stored by the pack: 09_Rejoin.cs:175 says the game hands them back (owned by Sessionid), so the pack restores only its own side. 10_Online.cs:19-61 (RWIdentity) keeps the player's GUID so a restarted friend keeps its Sessionid. docs/mode/REJOIN.md is not in the audited files. |
| 03:14 | Status: built; passed on the PC with a real second player, a game restart included. Steam's Join Game and a real network's drop wait for the online evening. | `09_Rejoin.cs:1-14 (header: the feature), 10_Online.cs:19-61 (RWIdentity: the kept GUID that lets a restarted player rejoin)` | MATCH | The code is built. The PC pass is a report's claim, not checked here. Join Game in Steam and a real-network drop are open for the online evening (PLAN, Online evening), as the line says. |

### 2.4 04_Rogue_Waves_Mode.txt

| Lines | Says | Code | Mark | Note |
| --- | --- | --- | --- | --- |
| 04:7 | Summary: campaign of 10 maps with increasing difficulty and new modifiers per campaign; progress saved between maps; Directions picked before each map and ranks bought at VENDCINT; Score does not count; completing a campaign unlocks a harder modifier. | `RWChart.Rows=10 (03_Chart.cs:7); RWCampaign.Complete (06_Campaign.cs:311); RWCampaign.Save (06_Campaign.cs:162); RWVote.AskPicks (20_Vote.cs:231); RWShop.Buy D: rank purchase (18_Shop.cs:376-391); RWCard.HideScore (13_Card.cs:73); CampaignPool (21_Difficulty.cs:247).` | MATCH | Everything except the unlock clause is built. The unlock clause is Depth, which is on hold (see 04:39). |
| 04:10 | Maps: 10 in a row, with increasing difficulty. | `RWChart.Rows=10 (03_Chart.cs:7); campaign complete at Route.Count>=RWChart.Rows (06_Campaign.cs:104, 250); FinishRows difficulty ladder (21_Difficulty.cs:26).` | MATCH |  |
| 04:11 | Stretches: the 5th and 10th rows of the sea chart close a stretch (Breaker maps). | `RWChart.Stretch=5 (03_Chart.cs:8); n.Breaker = r % Stretch == 0 (03_Chart.cs:132).` | MATCH | Rows 5 and 10 are the breaker rows, as the scope says. |
| 04:12 | Vessels: players must select their vessel first, and are locked into their choice. | `RWBoat.Lock (05_Boat.cs:270-288) puts each player's vessel back to the one the campaign recorded (RWRun Hero, set at MapStart in 14_Run.cs:257).` | MATCH | The pick itself is the stock game's; the lock is the pack's. |
| 04:13 | Start: the host starts a campaign from the Rogue Waves main-menu button (New or Continue), which takes them to the boat with the mode set, and invites friends there. | `RWMenu.Make (12_Menu.cs:56), RWMenu.Choose New/Continue (12_Menu.cs:163-202), RWMenu.Boat starts it (12_Menu.cs:213-223), RWCampaign.New (06_Campaign.cs:54).` | MATCH |  |
| 04:14 | Saving: progress is saved between maps, so a campaign can be played over several sessions. | `RWCampaign.Save (06_Campaign.cs:162-176) on MapStart and Won; RWRun.Save at MapStart (14_Run.cs:260) and Won (06_Campaign.cs:284).` | MATCH |  |
| 04:15 | Continue: each player gets back vessel, Pure Aura, Directions and Trinkets, and the team its Cardinals. | `RWCampaign.Continue (06_Campaign.cs:78); RWRun.Load (14_Run.cs:160) with Encode/Decode of Pure, Directions, Trinkets (14_Run.cs:188-217) and Cardinals (CardinalsKey, 14_Run.cs:169-170); vessel via RWBoat.Lock.` | MATCH |  |
| 04:16 | Won: clearing Tide 15 on the final map and leaving completes the campaign; map 10 ends like every map and is won by leaving after Tide 15 or later; Depth is on hold so nothing unlocks. | `RWCampaign.Tide/Cleared (06_Campaign.cs:211-227); Won (06_Campaign.cs:274); MapEnd Complete (06_Campaign.cs:250-251); RWVote.Leave on map 10 (20_Vote.cs:477-481); Complete (06_Campaign.cs:311-315).` | MATCH | The Depth clause is by design (on hold); no unlock code exists. |
| 04:17 | Lost: a failed campaign keeps nothing. | `RWCampaign.End then Stop removes Seed, Route, Retries, Replay, Playing, Choice, Staged, Lobby keys and forgets run data (06_Campaign.cs:149-160, 317-322).` | MATCH |  |
| 04:18 | Score: does not count in this mode. | `RWCard.HideScore hides Recordscore on Win and Lose menus (13_Card.cs:73-85).` | MATCH |  |
| 04:19 | Changes per campaign: the seed picks 2 of the 4 new modifiers and 2 of the stock elites (Armor, Health, Red, Explode, Aegis). | `RWDifficulty.CampaignPool (21_Difficulty.cs:247-257): two pack and two stock picks from the seed; StockElites list (21_Difficulty.cs:27).` | MATCH |  |
| 04:19 | None is on for map 1; one comes on from map 2 and one more every second map (all four by map 8). | `RWDifficulty.ModifiersOn (21_Difficulty.cs:267): 0 below map 2, then 1+(map-2)/2, which gives 1 on maps 2-3, 2 on 4-5, 3 on 6-7, 4 on map 8.` | MATCH |  |
| 04:19 | On one foe in five of each row, taken in turn from Tide to Tide. | `RWDifficulty.Apply (21_Difficulty.cs:174-188): marked = total / ModifierShare (5); nth = t-1 then incremented per row, so the modifier rotates.` | MATCH |  |
| 04:19 | A stock elite a map can't load is replaced by the next one outside the four, so no map plays easier than its number. | `RWDifficulty.Pool (21_Difficulty.cs:276-286): reserve list is the campaign's four first, then the other stock elites, then the other pack modifiers; a missing one is skipped for the next in that list.` | DIFFERENT | The code's reserve puts the campaign's own other picks before the outside ones, so a replacement can be one of the four (the code's own comment says the campaign's first). The scope says outside the four. |
| 04:22 | Each map is harder than the one before: more enemy modifiers, higher spawn rates, harder optional objectives and more enemy health. | `Modifiers 21_Difficulty.cs:267-288; spawn rates 21_Difficulty.cs:122-123; health 21_Difficulty.cs:115-117; objectives scale only in RWObjectives.SpawnMeteor (19_Objectives.cs:278-279 distance and 306 hits by map). Pay and the other four objectives (Hull, Clock, Hands, Nest) are flat (19_Objectives.cs:14-21).` | DIFFERENT | Only Mind the Meteor gets harder with the map. The other objectives' pay and targets do not change by map. |
| 04:23 | Map k's Tide 15 plays the base game's rows 15, 18, 20, 21, 21, 24, 24, 24, 24, 24, and its Tides 1-14 the rows spread before it. | `RWDifficulty.FinishRows (21_Difficulty.cs:26); RowOf (21_Difficulty.cs:74-80) gives round(t x F / 15) for t up to 15.` | MATCH | The numbers are the scope's tuned defaults; the code matches them. |
| 04:23 | Foes +6% a map, +50% per player past the first; spawn intervals -3% a map. | `CountPerMap 0.06, CountPerPlayer 0.5, IntervalPerMap 0.03 (21_Difficulty.cs:18); scale and interval in Apply (21_Difficulty.cs:122-123).` | MATCH |  |
| 04:23 | Foe health x2.6 on map 1 and 0.08 more a map (x3.32 on map 10), then about +2% for every Cardinal, Trinket copy and Direction rank held when the map starts (the lobby's picks not counted), all multiplying, and on top of an elite's own. | `HealthLift 2.6, HealthPerMap 0.08 (21_Difficulty.cs:18-19); HealthFor (21_Difficulty.cs:117); HoardShare 0.02 (21_Difficulty.cs:20); Hoard (21_Difficulty.cs:59-68) counts RWRun.Cardinals, which excludes lobby picks; health added as a Pmulti (21_Difficulty.cs:236).` | MATCH | Pmulti stacks on the elite's own Multi, as the scope asks. |
| 04:23 | Bonus Tides play the rows after Tide 15's with the map's scaling, and from row 29 on each bonus Tide adds 10% foes on the last. | `RWDifficulty.Bonus (21_Difficulty.cs:292-320) rewinds to LastRow 29 and multiplies Count by 1.1 each bonus Tide; RowOf for tides past 15 gives finish+t-15 (21_Difficulty.cs:79).` | MATCH | Bonus Tides are reached through the table's rows after the finish row, as the scope says. |
| 04:26 | Retries: per campaign, 3. | `RWCampaign.RetriesPerCampaign = 3 (06_Campaign.cs:21).` | MATCH |  |
| 04:27 | Losing a map uses one retry and the team replays that map from the last save; with no retries left a lost map ends the campaign. | `RWCampaign.Lost (06_Campaign.cs:292-309): Retries--, Replay=col, RWRun.Load() restores the checkpoint; End when Retries is 0 (06_Campaign.cs:307-308).` | MATCH |  |
| 04:28 | The host quitting during a map counts as losing it; only the host quitting during a map spends a retry. | `RWCampaign.OnBoat (06_Campaign.cs:264-272) and MapEnd non-Win (06_Campaign.cs:236-241) call Lost; a friend's drop never reaches these (RWRejoin only says, 09_Rejoin.cs:505-543).` | MATCH |  |
| 04:29 | A friend who drops or quits spends no retry and can rejoin the running map. | `RWRejoin Host seats and Restore (09_Rejoin.cs:59-158, 161-185); Watch and Say the way back in (09_Rejoin.cs:505-538).` | MATCH |  |
| 04:29 | A friend who drops while the map loads comes back on the boat. | `RWLoad.Update kicks a player still loading after Grace 75 s and says they can join again on the boat (10_Online.cs:81-147).` | MATCH | The kick sends them to the main menu; joining again on the boat is their own step, as the scope implies. |
| 04:30 | Dying in a bonus Tide does not lose the map: it counts as won once Tide 15 is cleared, no retry is spent, and only the Tide in progress pays nothing. | `RWCampaign.Tide/Cleared -> Won (06_Campaign.cs:211-227, 274-286); Playing=-1 after Won so MapEnd does not Lost (06_Campaign.cs:236-241); RWRun.PayTide and the Tide-in-progress rule (14_Run.cs:269-278).` | MATCH |  |
| 04:31 | After a loss the boat sails back to the lost map by itself, and only that map can be picked until it is won. | `RWCampaign.Lost sets Choice=col when AutoReplay (06_Campaign.cs:301); RWCampaign.Open returns only the Replay column (06_Campaign.cs:43-51); RWBoat.Sail (05_Boat.cs:338-374).` | MATCH |  |
| 04:32 | Status: built, and a lost map, its retry and Continue passed on the PC. | `Status line; the built parts are the retry code above (06_Campaign.cs:292-309). The PC pass is in docs/reports/BUILD_2026-11-03.md, not in code.` | MATCH | Status line only. |
| 04:35-04:38 | Depth: 5 levels; each level adds a change along with the previous ones; named Depth 1, Depth 2 and so on; meant to carry over between sessions. | `No Depth code. RWCampaign.Complete says Depth, its reward, is on hold (06_Campaign.cs:314).` | UNBUILT-BY-DESIGN | Scope line 41 puts Depth on hold until the core loop is settled. |
| 04:39 | Depth is unlocked by completing a campaign. | `RWCampaign.Complete (06_Campaign.cs:311-315) ends the campaign and writes nothing for Depth.` | UNBUILT-BY-DESIGN | On hold (04:41). |
| 04:40-04:41 | Changes: blank until Depth is designed. Status: on hold until the core gameplay loop is settled. | `No code.` | UNBUILT-BY-DESIGN | The scope itself says it is on hold. |
| 04:44 | Breaks: 60 seconds between tides. | `RWMode.BreakSeconds = 60 (02_Mode.cs:10), applied to every break after Tide 1 (02_Mode.cs:30-38).` | MATCH |  |
| 04:45 | Map won once Tide 15 is cleared. | `RWCampaign.WinTide = 15 (06_Campaign.cs:22); Tide(wave) with wave > WinTide calls Cleared (06_Campaign.cs:211-218).` | MATCH |  |
| 04:46 | Leaving: after clearing Tide 15 the players vote between tides on leaving, and the map is left when a majority votes to. | `RWVote.AskLeave (20_Vote.cs:439-452) after Cleared; CloseLeave leaves only when yes*2 > all (20_Vote.cs:454-485).` | MATCH |  |
| 04:47 | Only between tides: the players can leave only between tides, not right before their demise. | `RWVote.Update opens the leave vote only when ModKitTides.InBreak and Foes()==0 (20_Vote.cs:143-150); AskLeave holds the break (20_Vote.cs:448).` | MATCH |  |
| 04:48 | Bonus Tides have no limit; they go on, each harder, until a majority votes to leave; staying bonus Tides accumulates Pure Aura. | `RWDifficulty.Bonus (21_Difficulty.cs:292-320); the Stay/Leave card (22_AnyDirections? no: 20_Vote.cs AskLeave and RWAsk.Run Stay/Leave, 20_Vote.cs:655-664); bonus Tide pay PerBonusTide 2 (14_Run.cs:34, 271-273).` | MATCH |  |
| 04:49 | Status: built, and passed on the PC (60 second breaks, win at Tide 15, vote to leave, bonus Tides past 29). | `Status line; built code at 02_Mode.cs:10, 06_Campaign.cs:22, 20_Vote.cs:439-485, 21_Difficulty.cs:292.` | MATCH | Status line only. |
| 04:52 | Chart: a campaign's maps sit on a sea chart generated when the campaign starts: a harbour, then 10 rows of 3 or 4 base game maps. | `RWChart.Generate (03_Chart.cs:102-165): widths row 0 = 1, rows 1-10 = 3 + Below(2); chart built from the seed at RWCampaign.New (06_Campaign.cs:54-58) through RWWorld.Chart.` | MATCH |  |
| 04:53 | Links: each map links to 1 to 3 maps in the next row, and the 5th and 10th rows close a stretch. | `RWChart.MaxLinks = 3 (03_Chart.cs:10); LinksBetween caps the last node at MaxLinks (03_Chart.cs:93); breaker rows 132.` | MATCH | The harbour (row 0) links to every node of row 1, 3 or 4 links; the scope speaks of maps, and the harbour is not one. |
| 04:54 | Maps offered: every base game map can be offered in the vote; never the custom map slot, which hangs while loading. | `RWMode.Register pool (02_Mode.cs:72-87): Gameplans in the six stock modes only (02_Mode.cs:97, 79); Special_ and Test_ plans excluded (02_Mode.cs:78-79).` | MATCH | The custom slot is taken to be one of the excluded Special_ plans; the code's comment says Special_ plans hang the load. Not verified on the PC. |
| 04:55 | No repeats: no map is on a chart twice. | `RWChart.Generate removes each picked plan from the left list (03_Chart.cs:134-148).` | MATCH |  |
| 04:56 | Spread: easier maps in the first rows, harder maps later, the hardest closing each stretch. | `RWChart.Generate aim Aim2(row) rises with the row and gets +4 on breaker rows (03_Chart.cs:67-68), and picks among the nearest 5 maps to the aim (03_Chart.cs:139-147).` | MATCH | The scope's stray delta mark in 'harder and delta maps' is a text glitch in the scope, not a rule. |
| 04:57 | Every map is voted on, the first map included. | `RWVote.AskMap for the staging map's opening break (20_Vote.cs:61, 308-309); RWVote.OpeningPicks (20_Vote.cs:220-229).` | MATCH |  |
| 04:58 | Vote: offers the maps linked to the one just played (the first row for map 1); held in the map after the vote to leave; map 1's vote is held in the opening break of a staging map. | `RWCampaign.Open (06_Campaign.cs:43-51) uses RWWorld.Choices; RWVote.AskMap (20_Vote.cs:321-361) after the Direction picks following a leave (20_Vote.cs:476-485, 308-309).` | MATCH |  |
| 04:59 | Sailing: the party returns to the boat, which sails across the chart to the winning map; it loads once everyone readies up; friends can join on the boat between maps. | `RWVote.Leave (20_Vote.cs:488-508) returns to the boat via the stock win path; RWBoat.Sail (05_Boat.cs:338-374) sails to the winner; RWRejoin join handling (09_Rejoin.cs:212-223).` | MATCH | Loading on ready-up is the stock game's. |
| 04:60 | On the boat, between maps, players can't swap their Cardinals or Trinkets. | `RWCardinals.LockLobby (17_Cardinals.cs:814-861) locks the lobby's picks; RWCardinals.RestoreLobby (17_Cardinals.cs:779-786); no code offers a swap of held Cardinals or Trinkets.` | MATCH | Held ones have no swap path in the code, so nothing to block. |
| 04:61 | Lobby Cardinals: picked once before the first map, then locked for the campaign; the lobby offers only what the shop may sell. | `RWCardinals.RecordLobby (17_Cardinals.cs:792-808); LockLobby (17_Cardinals.cs:814-861) drops NotSold picks and restores the saved ones; NotSold list (17_Cardinals.cs:65-69).` | MATCH |  |
| 04:62 | Islands: each map shows its own island from the base game's world maps, at the base game's size, in the home world's water; a map with no island of its own borrows one. | `RWWorld.StockIsland (04_World.cs:380-422) copies the stock node, with scale = min(1, 22/width, 26/height) (04_World.cs:405) and the comment 'A very big one is shrunk to stay within its row' (04_World.cs:307); Sea from World_Omeanchor (04_World.cs:474-509); IslandFor borrows from islandPool (04_World.cs:364-378).` | DIFFERENT | Islands wider than 22 or taller than 26 units are shrunk to fit the row, so they do not show at the base game's size. Islands borrowed and home-world water match. |
| 04:63 | Status: built, and passed on the PC, the in-map vote and boat trip included; how the chart looks is for Dimitri to check by eye. | `Status line; code at 20_Vote.cs and 05_Boat.cs.` | MATCH | Status line only. |
| 04:66 | Sea Weather: some maps on the chart carry a weather, rolled with the chart. | `RWChart.Generate weather loop (03_Chart.cs:151-163), rolled from the same PRNG as the chart.` | MATCH |  |
| 04:67 | Calm rows: the first row and the rows closing a stretch are calm. | `RWChart.Generate: if (r == 1 \|\| n.Breaker) continue, leaving Weather = Calm (03_Chart.cs:155).` | MATCH |  |
| 04:68 | Odds: about 1 map in 3 has weather in the first stretch, 1 in 2 in the second. | `RWChart.Generate: rng.Below(r <= Stretch ? 3 : 2) != 0 skips, so 1 in 3 for rows 2-5 and 1 in 2 for rows 6-10 (03_Chart.cs:156).` | MATCH |  |
| 04:69 | Pays 1 extra Pure Aura each Tide on top of the Tide's pay: 3 for Tides 1-15, 2 for a bonus Tide. | `RWWeather.PureAuraPerTide = 1 (07_Weather.cs:16); RWRun.PayTide pay = (wave<=15 ? 3 : 2) + PureAuraPerTide when Current != Calm (14_Run.cs:273).` | MATCH |  |
| 04:70 | Shown: the weather on its island on the chart, its in-game line in the boat's map panel when picked; the map's start announces only its name (RISING WATER ROLLS IN). | `RWChart icon Mark and Billboard on the island (04_World.cs:276-297); RWBoat.Keep SetupNote with Display: Short (05_Boat.cs:109-114); RWWeather.Begin Announce(w, Display.ToUpperInvariant() + ' ROLLS IN') (07_Weather.cs:264).` | MATCH |  |
| 04:71 | Never twice: a map never gets the weather it already has (Manafold floods, Chrono speeds up, Vultures has a nest). | `RWChart.Native (03_Chart.cs:56-65) and its use at 03_Chart.cs:161.` | MATCH | The alternate Chrono and Vultures maps are also excluded, which the scope does not forbid. |
| 04:72 | Status: built, passed its PC round; how a flooded map looks is for Dimitri to check by eye. | `Status line; code at 07_Weather.cs and 03_Chart.cs:56.` | MATCH | Status line only. |
| 04:75 | Rising Water in-game line: Rising Water: the water climbs each Tide. More Pure Aura. | `RWWeather.Short (07_Weather.cs:34) gives 'the water climbs each Tide. More Pure Aura'; Display (07_Weather.cs:27) gives 'Rising Water'; joined in RWBoat.SetupNote (05_Boat.cs:113).` | MATCH |  |
| 04:76 | Rising Water does: the water climbs each tide; flooded tiles can't be built on; shots through the water slow down. | `RWWeather.SpawnFlood spawns stock Main_Flood (07_Weather.cs:310-330); no-build stand-in Block_Flood_Deter (07_Weather.cs:142-146, 202-233); Projslow is stock (logged at 07_Weather.cs:328).` | MATCH | The climb, the no-build rule and the slow come from the stock Main_Flood and the stand-in, as the scope says. |
| 04:77 | Rising Water numbers: the water rises each tide by its own stats, as on Manafold. | `No stat is set by the pack; SpawnFlood logs Maxheight, Halfheight, Heightspeed, Projslow (07_Weather.cs:328); the stock Main_Flood runs them.` | MATCH | The numbers are the stock Manafold ones; the code does not change them. |
| 04:78 | Rising Water is never on Manafold. | `RWChart.Native: case RisingWater returns map == Map_Manafold (03_Chart.cs:60); used at 03_Chart.cs:161.` | MATCH |  |
| 04:79 | Rising Water look: a teal swell with a foam crest. | `RWIcons Rising pixel art (08_Icons.cs:24-35): teal T and foam F rows over a deep D base.` | MATCH | The look is the icon; the in-map light mist (07_Weather.cs:442) is a separate look added in docs/reports/WEATHER_LOOK_2026-11-05.md. |
| 04:82 | Quickening Current in-game line: the game runs faster from Tide 5. More Pure Aura. | `RWWeather.Short (07_Weather.cs:35) 'the game runs faster from Tide 5. More Pure Aura'; Display (07_Weather.cs:27).` | MATCH |  |
| 04:83 | Quickening Current does: from Tide 5 the whole game runs faster, and faster again from Tide 10. | `RWWeather.Tide (07_Weather.cs:275-291) sets Game_Mapvars.Time_Scale at QuickTide 5 and QuickerTide 10; SpeedUpdate keeps the speed on (07_Weather.cs:163-179).` | MATCH |  |
| 04:84 | Quickening Current numbers: x1.25 from Tide 5, x1.5 from Tide 10. | `RWWeather Quick = 1.25f, Quicker = 1.5f (07_Weather.cs:18); QuickTide 5, QuickerTide 10 (07_Weather.cs:17).` | MATCH |  |
| 04:85 | Quickening Current is never on Chrono. | `RWChart.Native: case QuickeningCurrent returns Map_Chrono or Map_Chrono_Alt (03_Chart.cs:61).` | MATCH |  |
| 04:86 | Quickening Current look: three foam chevrons. | `RWIcons Quickening pixel art (08_Icons.cs:40-54): foam F and shade f chevrons over teal.` | MATCH |  |
| 04:89 | Nesting Ground in-game line: a halberd nest grows near the foes. More Pure Aura. | `RWWeather.Short (07_Weather.cs:36) 'a halberd nest grows near the foes. More Pure Aura'; Display (07_Weather.cs:27).` | MATCH |  |
| 04:90 | Nesting Ground does: a halberd nest sits near the enemies; it grows each tide and sends out a halberd every few tides. | `RWWeather.SpawnNest spawns stock Tower_Halberdnest near the enemy core (07_Weather.cs:390-415); growth and halberds are the stock being's (logged at 07_Weather.cs:410).` | MATCH | The growth and halberds are the stock nest's; the pack only places it. |
| 04:91 | Nesting Ground numbers: placed 3 to 7 tiles from an enemy core. | `RWWeather.NestTile: d < 3 or d > 7 is skipped (07_Weather.cs:370-371).` | MATCH |  |
| 04:92 | Nesting Ground is never on Vultures. | `RWChart.Native: case NestingGround returns Map_Vultures or Map_Vultures_Alt (03_Chart.cs:62).` | MATCH |  |
| 04:93 | Nesting Ground look: a coral egg in a sand nest. | `RWIcons Nesting pixel art (08_Icons.cs:56-70): coral C egg over sand S nest.` | MATCH |  |

### 2.5 05_Gameplay_Rundown.txt

| Lines | Says | Code | Mark | Note |
| --- | --- | --- | --- | --- |
| 05:1 | \| Gameplay Rundown \| (title) | `-` | UNBUILT-BY-DESIGN | Title line only, no behaviour to build; treated as a heading and skipped as such, though kept for completeness. |
| 05:2 | Campaign step by step, from main menu to last map; each step points to rules file | `-` | UNBUILT-BY-DESIGN | Descriptive comment-style line (no behaviour, number, name or text to build). Skipped per the stated rules; marked as a non-behaviour line. |
| 05:4 | [Steps] heading | `-` | UNBUILT-BY-DESIGN | Heading; no behaviour to build. |
| 05:5 | 1. Lobby: friends grouped in host's campaign; vessel selected first, locked in; Rogue Waves button on main menu (New or Continue) takes them to the boat with the mode set, and invites friends there | `12_Menu.cs:141-224 (Clicked, Choose: New/Continue cards, embarking); 12_Menu.cs:213-223 (Boat starts New/Continue); 05_Boat.cs:266-288 (Lock vessels once campaign has seen them); 02_Mode.cs:20-94 (mode registered)` | MATCH | Main-menu button offers New and Continue, starts campaign on the boat, and vessels are locked in (Lock). Friend invitation on the boat is the stock lobby; Rejoin/online handled elsewhere. |
| 05:6 | 2. Direction: each player offered 3 new upgrades called Directions and picks 1; range from tower stats to major gameplay changes | `16_Directions.cs:119-136 (Build), 16_Directions.cs:189-247 (Roll, Offers=3 at line 22), 11_Dialog/20_Vote.cs:204-260 (AskPicks, ClosePicks, take one)` | MATCH | Offers = 3 (16_Directions.cs:22), one picked per player (ClosePicks, 20_Vote.cs:291-315). Range of Direction types spans Mine list (16_Directions.cs:29-106). |
| 05:7 | 3. Vote: after each player selects Direction, players vote on next map, boat sails across sea chart; every map voted on, first included | `20_Vote.cs:321-361 (AskMap), 20_Vote.cs:375-411 (CloseMap, sail to winner), 20_Vote.cs:36-67 (staging map / map 1 vote via staging), 05_Boat.cs:338-374 (Sail), 03_Chart.cs:102-165 (Generate)` | MATCH | Map vote after Directions (AskPicks then AskMap when staging or departing); staging map gives the first vote; boat sails to winner (Sail) via the chart. |
| 05:8 | 4. In the map: gameplay similar, new objectives tied to Pure Aura and shop; Pure Aura earned each tide; optional objectives for bonus Pure Aura, like bounty mechanic | `14_Run.cs:271-286 (PayTide, Pay); 19_Objectives.cs:1-316 (objectives, Pays field, Take, Resolve); 20_Vote.cs:168-194 (Begin, objective per Tide)` | MATCH | Pure Aura paid per Tide (PayTide), optional objectives with Pays (19_Objectives.cs:14-20) taken for the team. |
| 05:9 | 5. Breaks: completing objectives earns Pure Aura, traded to VENDCINT in breaks for Cardinals, Trinkets, or Direction ranks already held | `18_Shop.cs:24-61 (Land, VENDCINT in break), 18_Shop.cs:210-255 (Shelf: Cardinals/Trinkets and NextRanks), 18_Shop.cs:363-391 (Buy: Direction ranks), 17_Cardinals.cs:542-571 (Buy)` | MATCH | VENDCINT sells Cardinals, Trinkets and next Direction ranks (RWDirections.NextRanks) for Pure Aura; objectives pay Pure Aura via RWRun.Pay (19_Objectives.cs:241). |
| 05:10 | 6. Leaving: after clearing Tide 15, players vote between tides on leaving; majority leaves; staying bonus tides accumulate Pure Aura, bonus tides go on each harder until majority votes leave; leave only between tides | `20_Vote.cs:126-161 (Update: Tide 15 cleared, AskLeave), 20_Vote.cs:439-485 (AskLeave, CloseLeave, yes*2 > all majority), 21_Difficulty.cs:290-320 (Bonus, bonus Tides), 20_Vote.cs:126-161 (votes only in break via ModKitTides.InBreak in Update)` | MATCH | Leave vote after Tide 15 (RWCampaign.Cleared then AskLeave); majority is yes*2 > all to stay (CloseLeave line 468); bonus Tides rewound with RWDifficulty.Bonus. Vote held only in breaks (clearingWatch requires ModKitTides.InBreak). |
| 05:11 | 7. Next map: loop continues choosing Direction, playing map, escaping; total 10 maps with increasing difficulty | `06_Campaign.cs:14-16 (WinTide=15, RetriesPerCampaign=3), 06_Campaign.cs:53-107 (campaign), 06_Campaign.cs:311-315 (Complete when Route.Count>=RWChart.Rows), 05_Gameplan/21_Difficulty.cs:26 (FinishRows per map), 21_Difficulty.cs:120-122 (Apply scales by map)` | MATCH | RWChart.Rows = 10 (03_Chart.cs:7); map difficulty increases per map (FinishRows, CountPerMap, HealthPerMap, ModifiersOn). |
| 05:12 | 8. Between maps: campaign saved; players can stop and continue later; Continue on main menu's Rogue Waves button gives each player back vessel, Pure Aura, Directions, Trinkets; team gets Cardinals | `06_Campaign.cs:162-176 (Save), 06_Campaign.cs:78-107 (Continue), 14_Run.cs:146-171 (Save/Load of RWPlayer: Hero, Pure, Directions, Trinkets), 14_Run.cs:146-156 (RWRun.Cardinals saved as CardinalsKey), 12_Menu.cs:213-223 (Boat: Continue)` | MATCH | Campaign saved at each map start and win (RWRun.Save); Continue restores players (RWRun.Load), vessel lock (05_Boat.cs:266-288 uses RWPlayer.Hero) and team Cardinals (RWRun.Cardinals via CardinalsKey). |
| 05:13 | 9. The end: clearing Tide 15 on final map and leaving completes campaign; new Depth level unlocked; failed campaign keeps nothing. Depth on hold: until built, nothing unlocks | `06_Campaign.cs:317-322 (Complete logs 'Depth, its reward, is on hold'), 06_Campaign.cs:311-315 (Complete), 06_Campaign.cs:149-160 (Stop: failed/won campaign leaves nothing saved), 06_Campaign.cs:292-309 (Lost: no retries ends campaign)` | UNBUILT-BY-DESIGN | Depth unlock is on hold per the line's own note ('until it is built, nothing unlocks'); the rest matches: completion (Complete) and failed/won campaign keeps nothing (Stop removes all campaign keys). |

### 2.6 06_New_Mechanics.txt

| Lines | Says | Code | Mark | Note |
| --- | --- | --- | --- | --- |
| 7 | Offered: 3 are offered and 1 is picked before each map | `16_Directions.cs:22 (Offers = 3); 16_Directions.cs:189-246 Roll; 20_Vote.cs:231-249 AskPicks and ClosePicks (one card taken per player)` | MATCH | The pick is one card of the offers, the first card on timeout. |
| 8 | Ranks: a held Direction is offered again as its next rank; about 7 for any vessel and 4 per new vessel over 10 maps | `16_Directions.cs:214-232 (pool takes r == have + 1 for Any or the vessel)` | MATCH | The next-rank rule is in code. The 7 and 4 counts are balance estimates that no line of code states. |
| 9 | Rank costs: rank II Pure Aura 20, rank III Pure Aura 30, bought at VENDCINT | `16_Directions.cs:23 (RankTwoCost = 20, RankThreeCost = 30), :283 RankCost; 18_Shop.cs:383-391 Buy sets cost and takes p.Pure` | MATCH |  |
| 10 | Shop sells only the next rank of a Direction the vessel already holds | `16_Directions.cs:261-281 NextRanks; 18_Shop.cs:241-248 Shelf adds NextRanks` | MATCH |  |
| 11 | Kept on the player's vessel, back at the start of every map, saved with the campaign | `14_Run.cs:146-150 Save and 14_Run.cs:188-192 Encode (Directions field); 14_Run.cs:333 Carry; 16_Directions.cs:304-316 Grant` | MATCH |  |
| 12 | Base game vessels get their own Directions besides the Any ones | `16_Directions.cs:85-106 (Vessel = Buster, Fie, Boutman, Snosno, Smacky entries); 23_BaseDirections.cs:16-27 RWBase` | MATCH |  |
| 13 | Names: a plain name that matches its Look line; a heading or course is optional | `16_Directions.cs:285-299 Picture (Look field or RWAssets.Like('Direction_' + name))` | MATCH | Look names are set on Tougher only (16_Directions.cs:31); the rest resolve by name. |
| 14 | Power: rank I about +6% to +15% on one stat or rule; rank III about +20% to +50%; a Shift gives something up | `16_Directions.cs:85-94 (Amounts 0.1/0.2/0.3, 0.08/0.16/0.25, 0.15/0.3/0.5); 22_AnyDirections.cs:45-50 (SaltHealth 0.15/0.3/0.45); 16_Directions.cs:113-118 (CostScale, RefitScale)` | MATCH | The sampled numbers sit in the band; the full set of 40-odd Directions was not checked line by line. |
| 15 | Pairs: two Directions that clash are a pair; a player who holds one is never offered the other | `16_Directions.cs:42-43 and 53-54 (Clash = Spring on Spring and Wound); 16_Directions.cs:205-228 Roll (heldClashes)` | MATCH |  |
| 16 | Duos: offered only to a player who holds both named Directions, and has one rank | `16_Directions.cs:79-82 (Needs Duo:Lamp+Chain, Duo:Mend+Spring, Ranks = 1); 16_Directions.cs:163-175 Meets` | MATCH |  |
| 17 | By: each Direction says who made it, Dimitri or Claude (points to 00_Read_Me [Values]) | `16_Directions.cs:7-18 RWDirectionDef has no author field; attribution appears only in code comments (16_Directions.cs:28, 50-52)` | MISSING | Looked for an author or By field on RWDirectionDef and the templates; none. Nothing in the plan queues it. |
| 18 | List: one Direction template per Direction, under its vessel (11_Directions.txt) | `16_Directions.cs:119-137 Build (RWContent.Upgrade per Direction, Id(vessel, name)); 16_Directions.cs:141-146 Refresh` | MATCH |  |
| 19 | Status: built and passed on the PC; later Directions compiled, not yet run on the PC | `22_AnyDirections.cs:1-2 and 23_BaseDirections.cs:1-2 headers ('compiled, not yet run in game')` | MATCH | Status line; the code's own headers agree with it. |
| 23 | Pure Aura per tide: 3 | `14_Run.cs:34 (PerTide = 3); 14_Run.cs:271-278 PayTide` | MATCH |  |
| 24 | Pure Aura per bonus tide: 2 | `14_Run.cs:34 (PerBonusTide = 2); 14_Run.cs:273 (wave above WinTide pays PerBonusTide)` | MATCH |  |
| 25 | Weather map pays extra each tide (04 Sea Weather) | `07_Weather.cs:16 (PureAuraPerTide = 1); 14_Run.cs:273 adds it when RWWeather.Current is not Calm` | MATCH |  |
| 26 | Objectives: each pays its own amount (14) | `19_Objectives.cs:5-17 (Pays per objective); 19_Objectives.cs:240-241 Resolve pays a.Pays through RWRun.Pay` | MATCH |  |
| 27 | Shown as a pearl white counter on each player's vessel, a colour no base aura uses | `14_Run.cs:324-325 ModKitCounter.Set(v, PureStat, p.Pure) with no colour argument; 14_Run.cs:531 Pearl colour is set only on the HUD run line label` | MISSING | Looked for a colour on the vessel counter; the pack sets none, so the pearl look is not in this code. Pearl appears only on the run line label. |
| 28 | Spent on Cardinals, Trinkets and ranks of Directions, at VENDCINT | `17_Cardinals.cs:542-571 Buy; 18_Shop.cs:363-391 Buy (C: and D: items)` | MATCH |  |
| 29 | Status: built and passed on the PC | `14_Run.cs:322-333 Carry (counter written every Tick)` | MATCH | Status line; the code is built. |
| 33 | This is the expansion's 'Streamlined Economy Changes' | `18_Shop.cs:402-405 RWFund class named Fund Transfer; no 'Streamlined Economy Changes' text anywhere in the pack code` | DIFFERENT | The code's name for the feature is Fund Transfer; the scope's alternate expansion label is not in the code. |
| 34 | What can be sent: only base aura, never Pure Aura | `18_Shop.cs:300-302 Colour buttons Redaura, Blueaura, Yellowaura; 18_Shop.cs:413 Array.IndexOf(ModKitUtil.Auras, p[1]) check` | MATCH |  |
| 35 | Limit: 50 times the coming tide's number per break, any mix of colours; 50 before Tide 1, 250 before Tide 5 | `18_Shop.cs:308-309 and 18_Shop.cs:407 Limit(wave) = PerTide(50) * (wave + 1), PerTide = 50 at 18_Shop.cs:404; 18_Shop.cs:419-422 and 439 per-player Sent room` | MATCH | Wave 0 gives 50 and wave 4 gives 250, matching the scope's examples. |
| 36 | Where: at VENDCINT | `18_Shop.cs:282-320 Fund(); 18_Shop.cs:368-369 FundButton opens Fund` | MATCH |  |
| 37 | Status: built and passed on the PC with a real second player | `18_Shop.cs:282-320 (Fund dialogs and send)` | MATCH | Status line; the code is built. |
| 40 | {VENDCINT}: a sentient vending machine | `15_Content.cs:8 (Vendor = Mob_Vendcint); 15_Content.cs:235-236 (Display_Name VENDCINT, Display_Desc 'A sentient vending machine...')` | MATCH |  |
| 41 | Arrives: falls near the vessels' spawn point at the start of every break, the opening one before Tide 1 included | `18_Shop.cs:24-61 Land and 18_Shop.cs:73-92 Tile (2-7 tiles from the Basin); 20_Vote.cs:93-96 Break calls OpenShop for wave 0 and later; 20_Vote.cs:138-139 opening OpenShop(0); 18_Shop.cs:133-153 Fall` | MATCH | The fall is placed beside the Basin (the vessels' landing point in play). |
| 42 | Leaves at the end of the break, jumping into the sky and disappearing; a break lasts 60 seconds | `18_Shop.cs:98-102 Jump at Time_Wave <= JumpBefore; 18_Shop.cs:156-165 Jump; 18_Shop.cs:63-68 Leave; 20_Vote.cs:109 Tide calls RWShop.Leave; 02_Mode.cs:10 BreakSeconds = 60; 02_Mode.cs:35 Wavetime = BreakSeconds` | MATCH |  |
| 43 | Sells Cardinals, Trinkets and next Direction ranks for Pure Aura; base Cardinal costs 15; each costs about 3 more per Cardinal the team holds and per Trinket copy the buyer holds | `17_Cardinals.cs:34 (StockCost = 15); 17_Cardinals.cs:3 Ramp = 3; 17_Cardinals.cs:453-455 PriceFor and RampFor (Trinkets.Count plus RWRun.Cardinals.Count)` | MATCH | The ramp 3 is a tilde default and is in code. |
| 44 | Fund Transfer is done at VENDCINT too | `18_Shop.cs:368-369 FundButton; 15_Content.cs:207 FundName button on the Stand` | MATCH |  |
| 45 | An objective is offered at VENDCINT in every break from the one before Tide 3 | `19_Objectives.cs:34 (ObjectivesFrom = 3); 19_Objectives.cs:116 (returns when wave + 1 < FirstTide); 19_Objectives.cs:111-124 Post; 20_Vote.cs:180 OpenShop calls RWObjectives.Post` | MATCH | Break before Tide 2 (wave 1) posts nothing; break before Tide 3 (wave 2) posts. |
| 46 | Status: built with temporary art, and passed on the PC | `08_Icons.cs:1-5 temporary art header; 18_Shop.cs:1-9 VENDCINT header` | MATCH | Status line; the code is built with temporary icons. |

### 2.7 09_Vessel_Argo.txt

| Lines | Says | Code | Mark | Note |
| --- | --- | --- | --- | --- |
| 9:2 | Argo, Deep Sea Diver (name) | `02_Data.cs:103-104` | MATCH | Displayname Argo and Displaysub Deep Sea Diver. |
| 9:8 | ! NEEDS BALANCE PASS ! | `none` | UNBUILT-BY-DESIGN | The scope marks the balance pass as its own NEEDS slot; the numbers are built as defaults and the sweep's tuned numbers wait for PC round 4. |
| 9:9-10 | Every value is what the built Argo 2.0 uses; the 2026-11-04 review's changes are built, compiled, not yet run in game | `02_Data.cs:1-20, 02_Data.cs:126-129, 02_Data.cs:161-227` | MATCH | The numbers in 02_Data.cs are the built 2.0 set. Spot-checked review lines (Debuffbonus 0.5/Pinbonus 1, Kelp 0.5, Gusher 4, Trench 2 pools) are in the code. |
| 9:13-14 | Title Deep Sea Diver; Difficulty Intermediate | `02_Data.cs:104-105` | MATCH | Displaysub and Displayskill. |
| 9:15 | Archetype: no walls; control the field; slow, pin, reel, blast | `02_Data.cs:108` | MATCH | The archetype is a design descriptor with no string of its own; the nearest built text is TooltipDesc, which says no walls of its own. |
| 9:16 | In-game line: Sturdy, slow melee, hits two at once | `02_Data.cs:106` | MATCH | HeroDesc, same words. |
| 9:17-18 | Look: bulky dome-helmeted diver; towers look low-poly with brass domes and portholes | `03_Assets.cs:68-88; 02_Data.cs:277-441 (model names)` | MATCH | Art judged by eye; the code names the shipped sheets and models only. |
| 9:19 | Accent colour: sea blue | `02_Data.cs:119` | MATCH | Color (0.27, 0.56, 0.76) on the hero entry. |
| 9:20 | Sprites: as shipped in the earlier ArgoMod | `03_Assets.cs:70-75` | MATCH | Argo-Sheet, ArgoFall, ArgoIntroR, ArgoAttack and ArgoSonar sheets are loaded. |
| 9:21 | Lore text (Argo went down to find the bottom of the Basin...) | `02_Data.cs:110` | MATCH | LogDesc, same text. |
| 9:22 | Status: Argo 2.0.1 designed, built, passes PC self-test | `02_Data.cs:16` | DIFFERENT | ArgoData.Version is 0.1.0-dev (the pack version); the scope and PLAN name the Argo part 2.0.1. The Version constant is not 2.0.1. |
| 9:25 | Attack: melee, with reach | `02_Data.cs:106; 04_Content.cs:661-668; 06_HeroBrain.cs:5-16` | MATCH | Stock Entity_Ai_Mob_Combat swings at Range 1.75 with ArgoHeroBrain adding the second hit. |
| 9:26 | Health 65 | `02_Data.cs:128` | MATCH | Health=65 in HeroStats. |
| 9:27 | Health regen 0 per second | `02_Data.cs:126-129` | MATCH | No regen stat is written, so it stays at the Smacky template's default; the code does not set it to 0 explicitly. |
| 9:28-29 | Speed 2.25; Damage 4 | `02_Data.cs:128` | MATCH | Speed=2.25 and Damage=4 in HeroStats. |
| 9:30-33 | Range 1.75; Attack speed 0.4; Build range 2; Sell rate 50% | `02_Data.cs:128` | MATCH | Range=1.75, Attackspeed=0.4, Buildrange=2, Sellrate=0.5. |
| 9:34 | Second hit: a second foe within 1.25 tiles, 60% damage | `02_Data.cs:131-132; 06_HeroBrain.cs:196-234` | MATCH | CleaveReach 1.25, CleaveMulti 0.6; OnHit applies damage times CleaveMulti to the nearest second foe. |
| 9:35 | Special rule: Fathoms, the tooltip text | `02_Data.cs:107-108; 04_Content.cs:627-649` | MATCH | TooltipName and TooltipDesc match the scope text; the tooltip upgrade is built in Tooltip(). |
| 9:36 | Slows: one per source, renewed, never stacked with itself; sources add; floor 10% of speed | `01_Util.cs:319-360; 01_Util.cs:351` | MATCH | Held keys per source renew; cut is capped at Value_Multi minus 0.1. Review note built. |
| 9:37 | Pins: Anchor Line, Kelp Bed, Trench, Mantle Tap pin; held in place; twice as long in Deep Water | `01_Util.cs:166-197; 01_Util.cs:184-185; 07_TowerBrain.cs:462-466; 07_TowerBrain.cs:709; 11_Water.cs:189-201; 11_Water.cs:217-229` | MATCH | Pin and Root hold the foe; WaterPinMulti 2 doubles a Wet foe's pin. |
| 9:39 | (Offense 1) Scatter Cannon name | `02_Data.cs:277` | MATCH | Display Scatter Cannon. |
| 9:40 | Close range blasts effective against hordes; cone of 6 pellets | `02_Data.cs:281` | MATCH | Projcount=6 and Spread=50; TowerBrain.Volley fans the pellets. |
| 9:41 | Bonus damage to debuffed foes: +50% slowed, +100% pinned | `02_Data.cs:281; 07_TowerBrain.cs:385-396` | MATCH | Debuffbonus=0.5 and Pinbonus=1; a pinned foe takes the larger. |
| 9:43 | In-game line: Short range, sprays a cone of pellets, hits slowed foes harder and pinned foes much harder | `02_Data.cs:277` | MATCH | Desc text matches. |
| 9:44-47 | Scatter Health 40 +25; Damage 1.25 +0.25; Attack speed 0.6; Range 4.5 | `02_Data.cs:281` | MATCH | Health=40+25, Damage=1.25+0.25, Attackspeed=0.6, Range=4.5. |
| 9:48-49 | Projectile speed 16; 6 pellets in 50 degree cone, pierce 1 | `02_Data.cs:281; 07_TowerBrain.cs:251-262` | MATCH | Projspeed=16, Projcount=6, Spread=50, Pierce=1. |
| 9:50 | Debuff bonus: +50% slowed, +100% pinned | `02_Data.cs:281; 07_TowerBrain.cs:385-396` | MATCH | Same as 9:41; the larger of Pinbonus and Debuffbonus applies to a pinned foe. |
| 9:51-52 | Scatter Limit none; Cost Red 30 | `02_Data.cs:278` | MATCH | Costs Redaura=30; no limit logic for towers (the limit applies only to the Miner's Base). |
| 9:54-56 | Wide Bore: a wider cone with two more pellets; +2 pellets, cone +10 degrees; Red 30 | `02_Data.cs:285-286` | MATCH | Projcount_Add=2, Spread_Add=10, Redaura=30. Scope tier 1 is code tier 2 (the scope level is code tier minus 1 across all trees). |
| 9:58-61 | Double Barrel: stores a volley every 2 s while no foe is in range, up to 3; Red 30 | `02_Data.cs:287-288; 07_TowerBrain.cs:210-243` | MATCH | Double_Base=3, Double_Every=2; stored volleys fire DoubleDelay apart. |
| 9:63-66 | Grapeshot: +3 pellets; Red 90 | `02_Data.cs:289-290` | MATCH | Projcount_Add=3, Redaura=90. |
| 9:67-71 | Barnacle Shot: slows 25% for 1.5 s; leads to Ironclad; Red 60 + Blue 40 | `02_Data.cs:291-292; 07_TowerBrain.cs:397-399` | MATCH | Slow_Base=0.25, Duration_Base=1.5, Redaura=60;Blueaura=40, Next Ironclad4. |
| 9:73-76 | Pressure Rounds: pierce +1, pellet damage +0.5; Red 350 | `02_Data.cs:293-294` | MATCH | Pierce_Add=1, Damage_Add=0.5, Redaura=350. |
| 9:77-81 | Ironclad: health x2.5; armour 2, a hit takes 1, healing refills it; ends branch; Red 250 + Blue 150 | `02_Data.cs:298-299; 07_TowerBrain.cs:986-997; 02_Data.cs:296-297` | MATCH | Health_Multi=1.5 (x2.5 as the Fx line says), Healtharmor_Base=2, Redaura=250;Blueaura=150. The per-hit rule is the game's Healtharmor; refill is the game's rule, not checked here. |
| 9:83-86 | Broadside: full ring of pellets every few volleys; ring of 16 every 4th volley; pellet damage +0.5; Red 3000 | `02_Data.cs:300-301; 07_TowerBrain.cs:253-259` | MATCH | Broadside_Every=4, Broadside_Count=16, Damage_Add=0.5, Redaura=3000; the ring spreads 360 degrees. |
| 9:88 | (Offense 2) Harpoon Gun name | `02_Data.cs:306` | MATCH | Display Harpoon Gun. |
| 9:89-90 | Base harpoon pierces a lane and does not pull; pin or reel by upgrade; pinning against a wall stuns | `07_TowerBrain.cs:289-343; 07_TowerBrain.cs:357; 01_Util.cs:186-194` | MATCH | Throw reels or pins only when Reel_Tiles or Root_Base is set; Beside() makes Anchor Line a stun. |
| 9:91-92 | Damage path (Long Line, Skewer, Whaler, Leviathan); crowd control path (Tether, Anchor Line, Reel, Undertow) | `02_Data.cs:312-328` | MATCH | Tree branches as listed. |
| 9:94 | In-game line: Long range, harpoons pierce a line; its upgrades pick damage or crowd control | `02_Data.cs:306` | MATCH | Desc matches. |
| 9:95-96 | Harpoon Health 40 +25; Damage 5 +2 | `02_Data.cs:308` | MATCH | Health=40+25, Damage=5+2. |
| 9:97-100 | Attack speed 0.35; Range 6.5; Projectile speed 18; Pierce 3 | `02_Data.cs:308` | MATCH | Attackspeed=0.35, Range=6.5, Projspeed=18, Pierce=3. |
| 9:101 | Bosses: never moved by a reel or pin | `01_Util.cs:166-169; 01_Util.cs:182; 01_Util.cs:199; 00_Reel: 01_Util.cs:253-258; 02_Data.cs:227` | MATCH | Boss() is max health 600 or more; Root, Pin and ReelTile refuse it. |
| 9:102-103 | Harpoon Limit none; Cost Red 35 | `02_Data.cs:307` | MATCH | Costs Redaura=35; no limit logic. |
| 9:105-107 | Long Line: range +2, harpoon speed +4; leads to Skewer and Anchor Line; Red 25 | `02_Data.cs:312-313` | MATCH | Range_Add=2, Projspeed_Add=4, Redaura=25, Next Skewer3,Anchor3. |
| 9:110-113 | Tether: slows every foe hit 30% for 2 s; leads to Anchor Line and Reel; Red 30 | `02_Data.cs:314-315; 07_TowerBrain.cs:403-408` | MATCH | Tetherslow_Base=0.3, Tetherslow_Duration=2. |
| 9:116-119 | Skewer: pierce +3; leads to Whaler; Red 100 | `02_Data.cs:316-317` | MATCH | Pierce_Add=3, Redaura=100, Next Whaler4. |
| 9:121-125 | Anchor Line: pins first foe 1 s; stunned beside a building for 1 s; twice as long in Deep Water; Red 60 + Blue 50 | `02_Data.cs:318-319; 07_TowerBrain.cs:462-466; 01_Util.cs:180-197` | MATCH | Root_Base=1, Pinstun_Base=1, Redaura=60;Blueaura=50; Pin doubles in Wet water. |
| 9:126-130 | Reel: reels first foe 1.5 tiles toward the gun; journal line on placement; leads to Twin Line; Red 60 + Blue 50 | `02_Data.cs:320-321; 07_TowerBrain.cs:469-482; 01_Util.cs:228-251` | MATCH | Reel_Tiles=1.5; the journal Fx line is at 02_Data.cs:321; Next Twinline4. |
| 9:132-136 | Whaler: damage +4; up to x2 on big health pools; leads to Leviathan; Red 450 | `02_Data.cs:322-323; 07_TowerBrain.cs:409-414` | MATCH | Whaler_Base=200, Damage_Add=4; multiplier 1 + clamp(maxHealth/200) reaches x2. |
| 9:137-141 | Twin Line: a second harpoon follows, reeling its own foe; leads to Undertow; Red 350 | `02_Data.cs:324-325; 07_TowerBrain.cs:335-342, 351-370` | MATCH | Twin_Base=0.3; the second Throw is also added to the reel list. |
| 9:143-146 | Leviathan Lance: every 5th shot x2.5; pierces all, twice the range; Red 3500 | `02_Data.cs:326-327; 07_TowerBrain.cs:331-333, 362-366` | MATCH | Lance_Every=5, Lance_Multi=2.5, Lance_Range=2 (range times 2), pierce 999. |
| 9:147-150 | Undertow: foes within 1.5 tiles of the reeled one are reeled 1.5 tiles; Red 1750 + Blue 1750 | `02_Data.cs:328-329; 07_TowerBrain.cs:483-498` | MATCH | Undertow_Radius=1.5, Undertow_Tiles=1.5, Redaura=1750;Blueaura=1750. |
| 9:152 | (Trap) Deep Water name | `02_Data.cs:336` | MATCH | Display Deep Water, Trap=true. |
| 9:153 | Slowing trap combining with harpoon or drill; pinned foe held twice as long in water; tremor touching a pool spreads through connected pools | `01_Util.cs:184-185; 11_Water.cs:142-153; 07_TowerBrain.cs:727-750` | MATCH | Shaken() takes every network a Tremor touches; TremorFoes includes their foes. |
| 9:154-155 | FLOOD: a button on Deep Water (keys 1 / Quickaction) adds a pool next to it or a pool; connected pools act as one | `04_Content.cs:378-397; 07_TowerBrain.cs:138-145; 11_Water.cs:249-294; 02_Data.cs:43-44` | MATCH | FLOOD clones Command_Buildoption1 and inherits its keybind (not set in code); networks are touching tiles. |
| 9:157 | In-game line: Walk-through pool that slows foes inside. FLOOD adds a pool next to it | `02_Data.cs:336` | MATCH | Desc matches. |
| 9:158-159 | Deep Water Health 30 +15; Slow 30% +5% per level | `02_Data.cs:338` | MATCH | Health=30+15, Slow=0.3+0.05. |
| 9:160 | Pinned foes held twice as long in the water | `01_Util.cs:184-185; 02_Data.cs:164` | MATCH | WaterPinMulti=2. |
| 9:161 | FLOOD pool: Blue 15 each | `02_Data.cs:416-417` | MATCH | Pool costs Blueaura=15. |
| 9:162-163 | Deep Water Limit none; Cost Blue 20 | `02_Data.cs:337` | MATCH | Costs Blueaura=20; no limit logic. |
| 9:165-168 | Undercurrent: stronger slow, slow +10%; Blue 20 | `02_Data.cs:342-343; 11_Water.cs:170` | MATCH | Slow_Add=0.1, Blueaura=20. |
| 9:169-173 | Brine: 0.5 damage a second; leads to Salt Sting; Blue 25 | `02_Data.cs:344-345; 11_Water.cs:171, 203-211` | MATCH | Brine_Damage=0.5, Blueaura=25, Next Salt3. |
| 9:175-178 | Kelp Bed: pins 0.5 s on entering, 1 s in the water; Blue 80 | `02_Data.cs:346-347; 11_Water.cs:189-201` | MATCH | Catch_Duration=0.5 pins through Pin, which doubles in water; Blueaura=80. |
| 9:179-183 | Salt Sting: pinned foes take double brine (1 a second without Brine); leads to Crushing Depth; Blue 70 + Red 30 | `02_Data.cs:348-349; 11_Water.cs:172, 203-205` | MATCH | Brinepinned_Multi=1 doubles; SaltBrine 0.5 doubled gives 1 a second; Next Crushing4. |
| 9:185-189 | Abyss: FLOOD fills two tiles, free second pool, slow +10%; Blue 260; ends branch | `02_Data.cs:350-351; 11_Water.cs:287-293; 02_Data.cs:170-171` | MATCH | Flood_Add=1, Slow_Add=0.1, Blueaura=260; no Next. |
| 9:190-193 | Crushing Depth: +15% damage taken from everything; Blue 300 | `02_Data.cs:352-353; 11_Water.cs:212-215; 09_Hooks.cs:168-171` | MATCH | Damagetaken_Multi=0.15 via ArgoHooks.Soak; Blueaura=300. |
| 9:195-198 | Trench: spreads each Tide, drags foes under; two free pools per Tide; every 3 s a foe pinned 1 s; Blue 3000 | `02_Data.cs:354-355; 11_Water.cs:217-243` | MATCH | Spread_Per_Tide=2, Dragunder_Every=3, Dragunder_Duration=1, Blueaura=3000. |
| 9:200-201 | Drill Rig: mines ore, slowing tremors at a targeted location; tremor grows per tide; each Tide sinks a Fathom up to 5 | `02_Data.cs:360-367; 07_TowerBrain.cs:670-725, 864-920` | MATCH | Mining, Tremor and Fathom rules in the brain. |
| 9:202 | AIM: a button that places the Tremor anywhere within its range | `04_Content.cs:343-355; 07_TowerBrain.cs:752-772; 16_AimSelect.cs:64-109` | MATCH | Aim command and picker; the reach rule is a separate row (9:209). |
| 9:203 | Fathoms: sinks a Fathom every Tide down to Fathom 5; each mines faster and harder and widens and strengthens the Tremor | `07_TowerBrain.cs:900-920; 07_TowerBrain.cs:864-884; 02_Data.cs:143-155` | MATCH | OnTide sinks one Fathom; ApplyFathom adds yield, speed, Tremor and slow. |
| 9:205 | In-game line: Mines ore nearby and sinks deeper each Tide; AIM places its Tremor, which slows foes | `02_Data.cs:360` | MATCH | Desc matches. |
| 9:206-207 | Drill Rig Health 40 +25; Damage 0.5 (mining) | `02_Data.cs:366` | MATCH | Health=40+25, Damage=0.5; mining in DrillTick and Bite. |
| 9:208 | Attack speed 0.25 | `02_Data.cs:366` | MATCH | Attackspeed=0.25. |
| 9:209 | Range 5 tiles: how far AIM can place the Tremor | `02_Data.cs:366; 02_Data.cs:173; 07_TowerBrain.cs:762-767` | DIFFERENT | Range=5 is stored, but Aim accepts a spot up to Range plus AimSlack 0.5 (5.5 tiles), not Range alone. |
| 9:210 | Ore reach: 2 tiles | `02_Data.cs:156; 07_TowerBrain.cs:822-838` | DIFFERENT | OreReach is 2.1 (a 0.1 tolerance over the scope's 2 tiles). |
| 9:211 | Tremor: 2 tiles radius, slows 20% | `02_Data.cs:366; 07_TowerBrain.cs:694, 727-740` | MATCH | Tremor=2 and Slow=0.2; slow capped by SlowCap. |
| 9:212 | Each Fathom: +75% ore per strike, +10% strikes per second, +0.4 tiles Tremor, +4% Tremor slow | `02_Data.cs:150-153; 07_TowerBrain.cs:864-884` | MATCH | FathomYield 0.75, FathomSpeed 0.1, FathomTremor 0.4, FathomSlow 0.04 applied per Fathom below the first. The ore-share and aura figures are the game's mining and a design estimate; no code prints them. |
| 9:213 | Deepest Fathom: 5 | `02_Data.cs:148` | MATCH | FathomMax=5. |
| 9:214-215 | Drill Rig Limit none; Cost Yellow 35 | `02_Data.cs:361` | MATCH | Costs Yellowaura=35; no limit logic. |
| 9:217-220 | Pay Dirt: foes felled in its Tremor are dug up for ore; 2 mining strikes per foe; Yellow 30 | `02_Data.cs:370-371; 05_Server.cs:130-137; 07_TowerBrain.cs:501-513` | MATCH | Paydirt_Base=2, Yellowaura=30. |
| 9:221-225 | Seismic: Tremor slow +10%, Tremor +0.5 tiles; leads to Bedrock; Yellow 35 | `02_Data.cs:372-373` | MATCH | Slow_Add=0.1, Tremor_Add=0.5, Yellowaura=35, Next Bedrock3. |
| 9:227-230 | Deep Core: sinks two Fathoms past the usual bottom, deepest 7; Yellow 120 | `02_Data.cs:374-375; 02_Data.cs:149; 07_TowerBrain.cs:66` | MATCH | Fathommax_Base=7, Yellowaura=120; FathomDeepest 7 caps the skins. |
| 9:231-235 | Bedrock: pinned foes in its Tremor take 1.5 damage a second; leads to Fault Line; Yellow 80 + Red 30 | `02_Data.cs:376-377; 07_TowerBrain.cs:712-724` | MATCH | Bedrock_Damage=1.5, Yellowaura=80;Redaura=30, Next Faultline4. |
| 9:237-240 | Gusher: 4 mining strikes per Fathom at once each Tide, from Fathom 3 down; Yellow 275 | `02_Data.cs:378-379; 07_TowerBrain.cs:906-917` | MATCH | Gusher_Base=4 times Fathom, only at Fathom 3 or more; Yellowaura=275. |
| 9:241-245 | Fault Line: 12 damage to a foe reeled into or across the Tremor; Yellow 300 | `02_Data.cs:380-381; 07_TowerBrain.cs:802-812` | MATCH | Fault_Damage=12, Yellowaura=300. |
| 9:247-250 | Mantle Tap: strikes per second x1.5; every 20 s a quake pins foes for 2 s; Yellow 3000 | `02_Data.cs:382-383; 07_TowerBrain.cs:705-710, 851-862` | MATCH | Attackspeed_Pmulti=0.5, Mantle_Every=20, Mantle_Base=2 pins through Quake; Yellowaura=3000. |
| 9:252 | Miner's Base works like Boutman's bouthouse, limit 1 | `05_Server.cs:171-186; 06_HeroBrain.cs:118-136` | MATCH | HasBase and CheckBase hide the button while one stands. |
| 9:253-254 | Summons 2 units that tune and repair towers; can only enhance towers, not traps | `02_Data.cs:390; 07_TowerBrain.cs:517; 07_TowerBrain.cs:636-640; 08_NautBrain.cs:206-222` | MATCH | Units=2; CrewOrder refuses traps. |
| 9:255 | PORT and STARBOARD buttons that send a Naut to a tower, a teammate's too | `04_Content.cs:326-341; 07_TowerBrain.cs:619-666; 14_CrewSelect.cs:52-84` | MATCH | Crew commands; the team check allows a teammate's tower. |
| 9:257 | In-game line: One per player. Houses Nauts: press PORT or STARBOARD, then click a tower to tune it | `02_Data.cs:388` | MATCH | Desc matches. |
| 9:258-260 | Miner's Base Health 50 +25; Units 2; Limit 1 | `02_Data.cs:390; 05_Server.cs:173-186` | MATCH | Health=50+25, Units=2; one per owner by BaseLimit. |
| 9:261 | Cost Blue 20 + Yellow 15 | `02_Data.cs:389` | MATCH | Costs Blueaura=20;Yellowaura=15. |
| 9:263-266 | Flippers: Naut speed +50%; Blue 30 | `02_Data.cs:396-397; 08_NautBrain.cs:225-239` | MATCH | Nautspeed_Multi=0.5, Blueaura=30. |
| 9:267-272 | Old Salts: Scatter +2 pellets; Harpoon +2 pierce, reels 1 tile farther; Drill Tremor slow +10%; leads to Overhaul; Blue 30 | `02_Data.cs:398-399; 08_NautBrain.cs:166-180; 08_NautBrain.cs:12; 02_Data.cs:76-83` | MATCH | SaltsPellets 2, SaltsPierce 2, SaltsReel 1, SaltsSlow 0.1; Blueaura=30; Next Overhaul3. |
| 9:273-276 | Overhaul: repairs 2.5 health a second; Blue 90 + Yellow 30 | `02_Data.cs:400-401; 02_Data.cs:78; 08_NautBrain.cs:215` | MATCH | Repair_Base=2 plus TuneHeal 0.5 gives 2.5 a second. |
| 9:277-281 | Calibrate: tuned towers range +1 tile; leads to Third Mate; Blue 80 + Yellow 30 | `02_Data.cs:402-403; 08_NautBrain.cs:160-162` | MATCH | Tunerange_Base=1, Blueaura=80;Yellowaura=30. |
| 9:283-286 | Third Mate: hires Bosun, a third Naut; +1 Naut; Blue 200 + Yellow 100 | `02_Data.cs:404-405; 02_Data.cs:61-65; 07_TowerBrain.cs:517` | MATCH | Units_Add=1 gives three crew, the third being Bosun. |
| 9:287-291 | Bulkhead: tuned towers take 25% less damage; ends branch; Blue 250 + Yellow 100 | `02_Data.cs:406-407; 08_NautBrain.cs:163-165; 09_Hooks.cs:177-181` | MATCH | Bulkhead_Base=0.25 cuts the hit scale by 25%. |
| 9:293-296 | Full Crew: tuning twice as strong (+40% attack speed); Nauts dive to their tower; Blue 2500 + Yellow 500 | `02_Data.cs:408-409; 08_NautBrain.cs:63-67, 157; 07_TowerBrain.cs:600-610` | MATCH | Tuneboost_Base=0.2 on top of 0.2, Divebell_Base=1 makes Nauts dive. |
| 9:298 | (Minion) Naut: Port, Starboard and Bosun, Bosun from Third Mate | `02_Data.cs:61-65; 07_TowerBrain.cs:517` | MATCH | Three crew entries; the third is gated by CrewSize. |
| 9:300 | Naut made by Miner's Base | `07_TowerBrain.cs:550-584` | MATCH | Spawn() from the Base. |
| 9:301 | Naut attack: none | `04_Content.cs:550-592; 02_Data.cs:76` | MATCH | No Damage stat is set and no attack command is added; the cloned Engineer's remaining scripts were not checked in code. |
| 9:302-304 | Naut Health 20 +5; Range 1.5; Speed 1.6 | `02_Data.cs:76` | MATCH | NautStats Health=20+5, Range=1.5, Speed=1.6. |
| 9:305 | Tuning: +20% attack speed and 0.5 health a second; tuned from 1.75 tiles | `02_Data.cs:78-79; 08_NautBrain.cs:151-222; 08_NautBrain.cs:54` | MATCH | TuneAttackspeed 0.2, TuneHeal 0.5, TuneReach 1.75. |
| 9:306 | How many: 2, or 3 with Third Mate | `02_Data.cs:65; 07_TowerBrain.cs:517` | MATCH | Units 2, plus Units_Add 1. |
| 9:307 | Respawn: 8 seconds | `02_Data.cs:77; 07_TowerBrain.cs:594` | MATCH | NautRespawn 8. |
| 9:308 | Orders: goes to the tower pointed at and tunes it | `07_TowerBrain.cs:619-666; 14_CrewSelect.cs:76-84` | MATCH | CrewOrder sends the chosen Naut. |
| 9:309 | Naut cost: free | `04_Content.cs:550-592` | MATCH | No Costs are set on the Naut entity. |
| 9:311 | Directions written in 11_Directions.txt under Argo | `04_Content.cs:598-622; 02_Data.cs:177-220` | MATCH | BuildDirections makes the four families; the scope file 11_Directions.txt is not in the code given. |
| 9:312 | Deep Current, Heavy Salvo, Chum Bucket, Pressure Gauge (made by Claude) | `02_Data.cs:179-220; 04_Content.cs:607-621` | MATCH | Four Direction families, three ranks each. |

### 2.8 10_Vessel_Uloe.txt

| Lines | Says | Code | Mark | Note |
| --- | --- | --- | --- | --- |
| 10:2, 10:7-11 | Uloe, Light Personified; Identity: title, difficulty Advanced, archetype, in-game line Frail single-target ranged | `02_Data.cs:48-51` | MATCH | Displayname Uloe, Displaysub Light Personified, Displayskill Advanced, HeroDesc Frail, single-target ranged. The archetype is carried by the Plug In, Photon and Flood Light rows below. |
| 10:5, 10:17 | Build status note: temporary art, 24 upgrades after the eight lines, Light sharing built, compiled not run on the PC | `02_Data.cs:82; 02_Data.cs:197-311` | MATCH | TeamLightCap 4 is in code; four buildings with two tiers each give 32 upgrade entries. Status note only; compiled here, not PC-run. |
| 10:12 | Look: glass bulb head with face, ribbed screw base on a tall post, yellow electric arms, cord to a plug | `03_Assets.cs:7-28` | DIFFERENT | Code loads the Uloe-Sheet frames (temporary art, docs/TEMP_ART.md). Nothing in code draws the described head, base, arms or plug; the look is the sheet's. |
| 10:13 | Towers look: towers carry glass, screw rings and cords | `04_Content.cs:138-148` | DIFFERENT | TowerVisual adds a Lit1-3 skin that draws a ring and rays round the foot. Glass, screw rings and cords are not built; towers use the temporary model. |
| 10:14 | Accent colour: glint yellow | `04_Content.cs:18` | MATCH | Accent = (0.945, 0.937, 0.447), F1EF72, applied to the entry and the vessel visual (04_Content.cs:620). |
| 10:15 | Full sprite sheet (idle, move, back view, fall, land, attack) waits for the design gate; one front concept frame so far | `03_Assets.cs:7-28` | UNBUILT-BY-DESIGN | Scope says the full sheet comes after drafting. Built animations: Idle, Move, Falling, Land, Landquick, Attack, Plug. No back-view animation. |
| 10:16 | Lore comes once the character is finalized | `02_Data.cs:55` | UNBUILT-BY-DESIGN | Placeholder text 'Uloe's story is still warming up.' The scope defers the lore. |
| 10:20 | Attack: ranged, a spark | `04_Content.cs:320-348; 04_Content.cs:588-599; 06_HeroBrain.cs:107-114` | MATCH | Hero AI is Entity_Ai_Mob_Ranged; Attack goal is Hero_Uloe_Spark (Snosno snowball clone, yellow model). |
| 10:21 | Health: 30 | `02_Data.cs:74` | MATCH | HeroStats Health=30. |
| 10:22 | Health regen: 0 per second | `02_Data.cs:72-75; 04_Content.cs:631` | MISSING | Looked for a Healthregen entry in HeroStats and in BuildHero. None set; the value is inherited from Smacky's stats. |
| 10:23 | Speed: 2.25 | `02_Data.cs:74` | MATCH | Speed=2.25. |
| 10:24 | Damage: 2 (low: towers and Photons do the damage) | `02_Data.cs:74` | MATCH | Damage=2. |
| 10:25 | Range: 3 tiles | `02_Data.cs:74` | MATCH | Range=3. |
| 10:26 | Projectile speed: 15 | `02_Data.cs:74` | MATCH | Projspeed=15. |
| 10:27 | Attack speed: 0.5 | `02_Data.cs:74` | MATCH | Attackspeed=0.5. |
| 10:28 | Build range: 2.5 tiles | `02_Data.cs:74` | MATCH | Buildrange=2.5. |
| 10:29 | Sell rate: 50% | `02_Data.cs:74` | MATCH | Sellrate=0.5. |
| 10:30 | Special rule: Bright Idea, with its text | `02_Data.cs:52-53; 04_Content.cs:558-576` | MATCH | TooltipName Bright Idea and TooltipDesc match the scope text word for word; the tier -3 Tooltip is in Upgradesinit. |
| 10:33 | Plug In: gives high Light in a radius around the node or Power Bank while in range | `06_HeroBrain.cs:157-172; 05_Server.cs:201-220; 02_Data.cs:96` | MATCH | Feed sets Light from PlugLight and radius PlugRadius 4 when plugged. Shine feeds the buildings in that radius. |
| 10:34 | The plug and cord move with Uloe | `06_HeroBrain.cs:365-372; 11_Client.cs:316-345` | MATCH | DrawLinks starts the Plugcord at the vessel's position each frame. |
| 10:35 | Numbers: node Light 2 + 0.4 per aura point, max 10; Power Bank 4 + 2 per upgrade, within 4 tiles | `02_Data.cs:97-98, 96; 06_HeroBrain.cs:157-163; 07_Building.cs:19` | MATCH | NodeBase 2, NodePerAura 0.4, NodeMax 10, BankBase 4, BankPerUpgrade 2, PlugRadius 4. UpgradeCount counts all bank upgrades, so four give 12. |
| 10:36 | Target: a node or a Power Bank | `06_HeroBrain.cs:256-264` | MATCH | Pluggable accepts a node (IsNode) or the player's own started Power Bank. |
| 10:37 | Range: 5 tiles; the cord's length, walking past it unplugs | `02_Data.cs:96; 06_HeroBrain.cs:352-362` | MATCH | PlugRange 5. CheckCord unplugs past Reach + PlugSlack, which adds 0.5 beyond the 5 tiles. |
| 10:38 | Cooldown: 0.5 seconds | `02_Data.cs:96; 06_HeroBrain.cs:287-293, 330-349` | MATCH | PlugCooldown 0.5 is set on unplug and checked at PlugIn. |
| 10:39 | Keys: 6 / 7 | `04_Content.cs:394` | MATCH | Plug command keybind {'6','7'} for an ability. |
| 10:40 | Unlocked: from the start | `06_HeroBrain.cs:94-98` | MATCH | Plug command created in Begin for every vessel; no unlock gate. |
| 10:41 | Cost: none | `04_Content.cs:631-637` | MATCH | No cost is set on the vessel entity. |
| 10:44 | Rule: towers get stronger effects from the Light they receive; Photons and Uloe give Light | `05_Server.cs:201-220; 07_Building.cs:33` | MATCH | Shine feeds Light stat to buildings in range; Light property reads it, capped at 12. |
| 10:45 | Illuminate: Photons can Illuminate enemies, increasing damage taken | `05_Server.cs:415-433, 344-352` | MATCH | UloeMarks.Illuminate; UloeMark.OnHit raises the Damagemulti by LitMulti. |
| 10:46 | Blind: can Blind enemies, reducing their damage | `05_Server.cs:397-412` | MATCH | Blind adds a timed Multi of -BlindMulti to the foe's Damage. |
| 10:47 | Strength depends on the source; a foe holds one level of each | `05_Server.cs:386-394; 10_Mirror.cs:37-40` | MATCH | Takes replaces a weaker level with a stronger one; the mark level is set per source. |
| 10:48 | Details: the mod adds up the Light from sources in range (B1) | `05_Server.cs:201-220; 05_Server.cs:30-56` | MATCH | Light is a stat each source re-adds every second (UloeLight.Feed). |
| 10:49 | Sources: Uloe Light 1 within 3 tiles; Photon Light 1 within 2 tiles; Plug In and Flood Light as written | `02_Data.cs:83-84; 06_HeroBrain.cs:165-172; 07_Building.cs:197-205` | MATCH | HeroLight 1, HeroRadius 3, PhotonLight 1, PhotonRadius 2. |
| 10:50 | Cap: 12 Light counted per building | `02_Data.cs:81; 07_Building.cs:33` | MATCH | LightCap 12 applied in Light. |
| 10:51 | Sharing: a teammate's Uloe Light reaches your buildings, up to 4 Light per building from teammates | `02_Data.cs:82; 05_Server.cs:43-45, 59-69` | MATCH | TeamLightCap 4; a teammate's source is capped by SharedOn. |
| 10:52 | Effects: Magnifying Glass +20% per Light; Power Bank Photons 10% faster per Light; Mirror level 1 from any Light, 2 from Light 3, 3 from Light 6; Flood Light mines 10% faster per Light | `02_Data.cs:89-92; 09_Glass.cs:18-19; 08_Bank.cs:25; 10_Mirror.cs:25-32; 07_Building.cs:317` | MATCH | GlassPerLight 0.2, BankPerLight 0.1, FloodPerLight 0.1, MirrorLevel2 3 and MirrorLevel3 6. Level is 1 for any Light above zero. |
| 10:53 | Levels: Blind -15/-30/-45%, Illuminate +15/+30/+45%, lasting 3/4/5 s; one of each per foe, stronger replaces weaker | `02_Data.cs:103-105; 05_Server.cs:386-412, 344-352` | MATCH | BlindMulti, LitMulti and MarkSeconds match; Takes implements the replace rule. |
| 10:58 | Magnifying Glass focuses beams of light to deal damage to enemies | `09_Glass.cs:74-110, 115-132` | MATCH | Fire and Beam; the beam is a stock Projectile_Ai_Beam hit. |
| 10:59 | Magnifying Glass damage depends on the Light it receives | `09_Glass.cs:19, 72` | MATCH | Multi = PerLight * Light + Ramp, held on Damage. |
| 10:61 | Glass in-game line: Focused, single-target beam, burns brighter in Light | `02_Data.cs:202` | MATCH | Desc text identical. |
| 10:62-63 | Glass Health 30 +20 per level; Damage 2 +0.5 per level | `02_Data.cs:204` | MATCH | Stats Health=30+20, Damage=2+0.5. |
| 10:64-65 | Glass attack speed 0.75; range 4.5 tiles | `02_Data.cs:204` | MATCH | Attackspeed=0.75, Range=4.5. |
| 10:66 | Glass limit: none | `02_Data.cs:197-311` | MATCH | No build limit is coded for any building. |
| 10:67 | Glass cost: Red 30 | `02_Data.cs:202` | MATCH | Costs Redaura=30. |
| 10:69-73 | Burning Point: +12% damage per beam on one target, max +120%, new target starts over; cost Red 35; leads to Polished Lens and Quick Focus | `02_Data.cs:207; 09_Glass.cs:78-84` | MATCH | Ramp_Base 0.12, Rampmax_Base 1.2, Redaura=35, Next Polished3,Quick3. The scope's % 1 is tier 2 by the file's own convention. |
| 10:74-78 | Wide Lens: range +1.5 tiles; leads to Quick Focus and Prism; cost Red 30 | `02_Data.cs:209` | MATCH | Range_Add=1.5 is an own-stat Add via OnUpgrade (07_Building.cs:103-132); Redaura=30; Next Quick3,Prism3. |
| 10:80-84 | Polished Lens: +40% damage per Light instead of +20%; leads to Sunspot; cost Red 90 | `02_Data.cs:211; 09_Glass.cs:18` | MATCH | Perlight_Base=0.4 is read by PerLight; Redaura=90; Next Sunspot4. |
| 10:85-89 | Quick Focus: attack speed +50%; leads to Sunspot and Focal Point; cost Red 80 | `02_Data.cs:213` | MATCH | Attackspeed_Pmulti=0.5; Redaura=80; Next Sunspot4,Focal4. |
| 10:90-94 | Prism: 2 more beams at foes within 1.5 tiles, 50% damage each; leads to Focal Point; cost Red 100 | `02_Data.cs:215; 09_Glass.cs:161-184` | MATCH | Prism_Base 2, Prismradius_Base 1.5, Prismshare_Base 0.5, Redaura=100, Next Focal4. |
| 10:96-100 | Sunspot: target Illuminated at level 1 from Light 4, 2 from 8, 3 from 12; leads to Death Ray; cost Red 300 + Blue 50 | `02_Data.cs:156, 217; 09_Glass.cs:25-35, 97-105` | MATCH | SunspotLight {4,8,12}; Redaura=300;Blueaura=50; Next Deathray5. |
| 10:101-105 | Focal Point: full damage to every foe on the line out to range; with Prism the side beams pierce; leads nowhere; cost Red 350 | `02_Data.cs:219; 09_Glass.cs:136-149, 161-184` | MATCH | FocalWidth 0.5 line burn; Focal_Base=1; Redaura=350; Next empty. |
| 10:107-110 | Death Ray: +12% per beam, max +250%, kept target to target, lost after 3 s idle; Illuminated level 3 at full heat; replaces Burning Point's ramp; cost Red 3000 | `02_Data.cs:164, 221; 09_Glass.cs:21-22, 58, 74-77, 106-107` | MATCH | Deathramp 0.12, Deathmax 2.5, Deathidle 3, DeathMark 3, Redaura=3000. |
| 10:115 | Power Bank spawns Photons, light minions that give benefits to towers | `08_Bank.cs:112-127; 08_Bank.cs:87-109` | MATCH | Spawn creates a Photon with the bank as home. |
| 10:117 | Power Bank in-game line: Makes Photons that wander and give Light | `02_Data.cs:229` | MATCH | Desc text identical. |
| 10:118 | Power Bank Health 60 +25 per level | `02_Data.cs:231` | MATCH | Health=60+25. |
| 10:119 | Units: 2 Photons | `02_Data.cs:231; 08_Bank.cs:17` | MATCH | Units=2; Slots = Units + Batteries. |
| 10:120 | Respawn: 8 seconds | `02_Data.cs:231; 08_Bank.cs:21-27` | MATCH | Respawn=8, divided by battery and Light factors. |
| 10:121 | Power Bank limit: none | `02_Data.cs:197-311` | MATCH | No limit coded. |
| 10:122 | Power Bank cost: Red 40 + Yellow 20 | `02_Data.cs:229` | MATCH | Costs Redaura=40;Yellowaura=20. |
| 10:124-128 | Short Circuit: a falling Photon bursts for 3 damage within 1.5 tiles, Blind and Illuminate level 2; leads to Static Charge and Surge Protector; cost Red 40 | `02_Data.cs:234, 149; 08_Bank.cs:391-416` | MATCH | Burstdamage 3, Burstradius 1.5, BurstMark 2, Redaura=40, Next Static3,Surge3. |
| 10:129-132 | Extra Cell: one more Photon; cost Yellow 35 | `02_Data.cs:236` | MATCH | Units_Add=1, Yellowaura=35. |
| 10:134-137 | Static Charge: Photon damage +100%, its hits Blind and Illuminate at level 2; cost Red 90 | `02_Data.cs:238; 08_Bank.cs:205, 165` | MATCH | Static_Base 1 gives a Damage Multi of +1 (x2); Staticmark_Base 2; Redaura=90. |
| 10:138-141 | Bright Spark: a Photon gives Light 2 within 2.5 tiles; cost Yellow 90 | `02_Data.cs:240; 08_Bank.cs:156-164` | MATCH | Photonlight_Base 2, Photonradius_Base 2.5, Yellowaura=90. |
| 10:142-146 | Surge Protector: while plugged in, Uloe takes 40% less damage and heals 2 per second; ends the branch; cost Yellow 60 + Blue 40 | `02_Data.cs:242; 06_HeroBrain.cs:190-234` | MATCH | Surge_Base 0.4, Surgeheal_Base 2, heal once a second, Yellowaura=60;Blueaura=40. |
| 10:145 | Note: a safe socket gives Plug In at most Light 8, not 12 | `06_HeroBrain.cs:157-161` | DIFFERENT | Code has no cap: 4 + 2 per upgrade. A Surge bank with two more upgrades (Short Circuit, Extra Cell) gives 10, not 8. |
| 10:148-151 | Chain Lightning: each Photon hit jumps to 2 more foes within 2 tiles, same damage and marks; cost Red 350 | `02_Data.cs:244; 08_Bank.cs:251-278` | MATCH | Chain_Base 2, Chainradius_Base 2, Redaura=350; the jump carries the same damage and level. |
| 10:152-155 | Photon Swarm: units +2; leash 5 tiles; cost Red 150 + Yellow 200 | `02_Data.cs:246; 08_Bank.cs:155` | MATCH | Units_Add=2, Leash_Base=5, Redaura=150;Yellowaura=200. |
| 10:157-158 | Supernova: once a tide, every Photon bursts at once | `08_Bank.cs:14-15, 70-84; 02_Data.cs:248` | MATCH | SurgeReady compares the Game_Mapvars wave; Supernova bursts every Photon of the bank. |
| 10:159 | Supernova numbers: 20 damage within 2 tiles, Blind and Illuminate level 3, all Photons come back at once, Photons give Light 3 for 15 s | `02_Data.cs:248; 08_Bank.cs:70-84, 401-416; 08_Bank.cs:161-162` | DIFFERENT | Damage, radius, level 3 and 15 s Light 3 match. But nothing returns: Supernova bursts the Photons where they stand and only fills empty slots (08_Bank.cs:79-80), so no Photon comes back at once. |
| 10:160 | Supernova cost: Red 2000 + Yellow 1000 | `02_Data.cs:248` | MATCH | Redaura=2000;Yellowaura=1000. |
| 10:165 | Mirror: a wall that Light empowers to Blind enemies | `10_Mirror.cs:14-32, 58-75, 94-166` | MATCH | Blind at Level from Light; unlit gives no mark. |
| 10:167 | Mirror in-game line: Sturdy, Blinds what hits it when lit | `02_Data.cs:256` | MATCH | Desc text identical. |
| 10:168 | Mirror Health 100 +40 per level | `02_Data.cs:258` | MATCH | Health=100+40. |
| 10:169-170 | Mirror Blind: a foe whose hit lands is Blinded at the Light's level, melee or ranged; unlit does nothing. Limit none | `10_Mirror.cs:94-125, 143-146` | MATCH | Any hit from a foe gives a mark when level above zero; no limit coded. |
| 10:171 | Mirror cost: Blue 20 | `02_Data.cs:256` | MATCH | Blueaura=20. |
| 10:173-176 | Silver Backing: Blind one level stronger, at most level 3; cost Blue 30 | `02_Data.cs:261; 10_Mirror.cs:37-40` | MATCH | Blindup_Base 1; the level is capped at BlindMulti length 3; Blueaura=30. |
| 10:177-180 | Vanity Lights: Illuminate at the Light's level in place of Blind; cost Blue 30 | `02_Data.cs:263; 10_Mirror.cs:42-48` | MATCH | Mark uses Illuminate when Has Vanity; the Illuminate_Base=1 entry is not read, the branch is by Has. |
| 10:182-185 | Glare: a hit from a foe Blinded at level N has an N x 20% chance to do nothing; cost Blue 100 | `02_Data.cs:265; 10_Mirror.cs:112-125` | MATCH | Nullify_Base 0.2; chance = blind level times it; Blueaura=100. |
| 10:186-189 | Bounce Card: 50% of the Light it receives goes to your buildings within 3 tiles, never to another Bounce Card; cost Blue 100 | `02_Data.cs:267; 10_Mirror.cs:66-68; 05_Server.cs:201-220, 43-45` | DIFFERENT | Relay values match (0.5, 3 tiles, skipRelays, Blueaura=100). But Shine feeds every building of the team in range, not only the owner's; teammates' buildings take it as a teammate share capped at 4. |
| 10:190-193 | Tempered Glass: health +200% of base, takes 20% less damage; cost Blue 90 | `02_Data.cs:269; 10_Mirror.cs:128-134` | MATCH | Health_Pmulti 2, Tempered_Base 0.2, Blueaura=90. |
| 10:195-199 | Hall of Mirrors: the same mark at the same level on every foe within 2 tiles of the attacker; leads to Heliostat; cost Blue 350 | `02_Data.cs:271; 10_Mirror.cs:148-155` | MATCH | Hall_Base 2, Blueaura=350, Next Heliostat5. |
| 10:200-204 | Reflection: 30% of each hit back at the attacker, 60% from Light 6; leads nowhere; cost Blue 300 + Red 100 | `02_Data.cs:273; 10_Mirror.cs:50, 135-142` | MATCH | Reflect_Base 0.3, Reflectlit_Base 0.6 at MirrorLevel3 (6); Blueaura=300;Redaura=100. |
| 10:206-209 | Heliostat: beams the nearest foe within 4 tiles once a second for 1.5 per Light, gives its mark; still a wall; cost Blue 2500 + Red 1000 | `02_Data.cs:160, 275; 10_Mirror.cs:78-92` | MATCH | Heliostat_Base 1.5, Heliorange_Base 4, HeliostatEvery 1; the mark follows Mark(level). |
| 10:214 | Flood Light plugs into nodes and provides light, can be directed toward towers | `07_Building.cs:283-311, 364-413` | MATCH | FindNode takes the nearest node in range; Feed aims the Light at the chosen tower. |
| 10:216 | Flood Light in-game line: Mines a node, aims its Light at a tower | `02_Data.cs:283` | MATCH | Desc text identical. |
| 10:217 | Flood Light Health 40 +20 per level | `02_Data.cs:285` | MATCH | Health=40+20. |
| 10:218 | Mining: strikes a node within 2.5 tiles for 1, 0.3 times a second, 10% faster per Light; no node in reach, no Light | `02_Data.cs:285; 07_Building.cs:283-285, 315-325, 373-380` | MATCH | Damage=1, Attackspeed=0.3, Range=2.5, FloodPerLight 0.1 in the strike rate; Feed returns with no node. |
| 10:219 | Light out: 2, plus 0.2 per aura point up to 3 more, to the one building it aims at | `02_Data.cs:285, 313; 07_Building.cs:197-205, 392-399` | MATCH | Lightout=2; FloodNodePerAura 0.2 and FloodNodeMax 3; the aimed building gets Output. |
| 10:220 | Aim reach: 6 tiles; with no pick it lights the nearest | `02_Data.cs:285; 07_Building.cs:193, 364-371` | MATCH | Aimrange=6; CurrentAim falls back to NearestBuilding. |
| 10:221 | Flood Light limit: none | `02_Data.cs:197-311` | MATCH | No limit coded. |
| 10:222 | Flood Light cost: Yellow 40 | `02_Data.cs:283` | MATCH | Yellowaura=40. |
| 10:224-227 | Wide Flood: 60% of its Light to your buildings within 3.5 tiles; can no longer be directed; cost Yellow 30 | `02_Data.cs:288; 07_Building.cs:191, 238-243, 381-385; 05_Server.cs:201-220` | DIFFERENT | Numbers match (Wide_Base 0.6, radius 3.5, aim removed, Yellowaura=30). The ring feeds every team building in range, teammates included, not only your own. |
| 10:225 | Wide Flood in-game line: Lights all around it, can't be aimed | `02_Data.cs:289` | MATCH | Text identical. |
| 10:228-231 | Long Throw: 80% of its Light to every building on a line 8 tiles long toward its aim; cost Yellow 30 | `02_Data.cs:290; 07_Building.cs:401-412, 143-150` | MATCH | Line_Base 0.8, Linereach_Base 8, Linewidth_Base 0.6; LineEnd runs 8 tiles toward the aim; Yellowaura=30. |
| 10:233-236 | Halogen Bulb: Light out +2; cost Yellow 90 | `02_Data.cs:292` | MATCH | Lightout_Add=2, Yellowaura=90. |
| 10:237-240 | High Voltage: strikes +60% faster; cost Yellow 100 | `02_Data.cs:294; 07_Building.cs:317` | MATCH | Attackspeed_Pmulti=0.6, Yellowaura=100. |
| 10:242-246 | Colour Filter: payouts come in the picked colour at 70% of the node's total; COLOUR button; cost Yellow 250 | `02_Data.cs:296; 07_Building.cs:335, 349-361, 576-582; 04_Content.cs:412-420; 12_Commands.cs:256-284` | MATCH | Filter_Base 0.7, Yellowaura=250; the COLOUR button is keybound to 4. |
| 10:247-250 | Searchlight: foes in its Light Illuminated at level 1 (level 2 from Light out 5), slowed 20%, one slow per Flood Light; cost Yellow 250 + Red 100 | `02_Data.cs:298-301; 07_Building.cs:417-475` | MATCH | Searchlight_Base 1, Searchslow 0.2, Searchradius 1.5, Searchbright 5, SearchLit 1 and SearchLitBright 2; the script's MISSING lead was wrong, the code is there. |
| 10:252-255 | Stadium Lights: 100% of its Light within 5 tiles; AIM back; aimed building takes Light twice; strikes twice as fast and a second node; cost Yellow 3000 | `02_Data.cs:302; 07_Building.cs:191, 244-245, 307-310, 381-385, 386-391` | MATCH | Stadium_Base 1, Stadiumradius 5, Attackspeed_Multi 1 (x2), Node2 in reach, aimed building in the ring and the aim share, Yellowaura=3000. |
| 10:256-259 | Lighthouse: beam turns a full circle every 6 s, 9 tiles long; your buildings it crosses keep 60% of its Light for 6 s; foes it crosses Blinded and Illuminated at level 2; AIM goes away; cost Yellow 2500 + Red 500 | `02_Data.cs:305-308; 07_Building.cs:191, 480-523, 508-509` | DIFFERENT | Turn 6, reach 9, 60% for 6 s, level 2 marks, Yellowaura=2500;Redaura=500 and no AIM all match. The building test (07:495) takes any team building, not only your own. |
| 10:263 | Photon is made by a Power Bank | `08_Bank.cs:112-127` | MATCH | Spawn creates Mob_Uloephoton on the bank's tile. |
| 10:264 | Photon attacks for very little damage and applies Illuminate and Blind | `04_Content.cs:482-518; 08_Bank.cs:227-245` | MATCH | Generic melee AIs; OnDealt marks each hit. |
| 10:265 | Photon Health 8 | `02_Data.cs:141` | MATCH | Health=8. |
| 10:266 | Photon Damage 0.5 | `02_Data.cs:141` | MATCH | Damage=0.5. |
| 10:267 | Photon Range 1.5 tiles | `02_Data.cs:141` | MATCH | Range=1.5. |
| 10:268 | Photon attack speed 0.6 | `02_Data.cs:141` | MATCH | Attackspeed=0.6. |
| 10:269 | Photon speed 1.75 | `02_Data.cs:141` | MATCH | Speed=1.75. |
| 10:270 | How many: 2 per Power Bank | `02_Data.cs:231; 08_Bank.cs:17` | MATCH | Units=2. |
| 10:271 | Photon respawn 8 seconds | `02_Data.cs:231; 08_Bank.cs:21-27, 87-109` | MATCH | The Bank's Respawn=8 applies to its Photons. |
| 10:272 | Orders: Photons wander where they spawn, follow Flood Light paths when directed | `08_Bank.cs:288-317, 349-382; 05_Server.cs:243-249` | MATCH | Wander, CheckPath and PathNear implement it. |
| 10:273 | Marks: a hit Blinds and Illuminates at level 1 | `02_Data.cs:149; 08_Bank.cs:227-245` | MATCH | PhotonMark 1 with no Static Charge. |
| 10:274 | Leash: 3.5 tiles from its Power Bank | `02_Data.cs:147; 08_Bank.cs:155, 390-393` | MATCH | PhotonLeash 3.5. |
| 10:275 | Photon cost: free | `04_Content.cs:488-489` | MATCH | Costs empty list. |
| 10:277-278 | Directions: Extension Cord, Brighter Bulb, Spare Batteries, Flash Bulb (written in 11_Directions.txt) | `02_Data.cs:117-137; 04_Content.cs:537-553` | MATCH | Four Directions with the same names, three ranks each. 11_Directions.txt was not in the files given. |

### 2.9 12_Cardinals.txt

| Lines | Says | Code | Mark | Note |
| --- | --- | --- | --- | --- |
| 12:4 | Pointer to two reports (ride-on-game-code and overlap check) | `none` | MATCH | States no behaviour; nothing to build. |
| 12:5 | Shop's base game Cardinals: all but 14 named ids (Moon, Moon Core, Loop, Looped Mirror, Vintage, Same Day Shipping, Pig, Money Bag, Sandbox, Sand, Blurry Photo, Checklist, Computer, Bottom Rock) | `17_Cardinals.cs:65-69 NotSold` | DIFFERENT | NotSold holds those 14 plus Artifact_Rock, which the scope does not exclude, so the shop drops one more stock Cardinal than the scope says. The Bottom Rock 1% pay cut is game fact, not built, and is only a note. |
| 12:8 | A new Cardinal or Trinket is a new run modifier, the same kind of effect as the base game's Cardinals | `17_Cardinals.cs:44-62 Mine; 14_Run.cs:310-333 Carry; 17_Cardinals.cs:875-893 Grant` | MATCH | Sheets are granted to the vessel or the core as the base game's Cardinals are. |
| 12:9 | Names: found objects the wave washed up, two plain words each | `17_Cardinals.cs:45-62 Display names` | MATCH | All 16 Claude names are two words (Oil Can, Spare Plank, Cork Lifebuoy and the rest). |
| 12:10 | Bought from the shopkeeper with Pure Aura | `17_Cardinals.cs:542-571 Buy; 18_Shop.cs:217-255 Shelf and Shop dialog` | MATCH | Cost is taken from Pure Aura in Buy. |
| 12:11 | Two kinds, Cardinals and Trinkets, kept apart in the shop and in the list | `17_Cardinals.cs:349-375 Shelf (one Trinket slot); 18_Shop.cs:234 kind label; 14_Run.cs:13-37 RWRun.Cardinals vs RWPlayer.Trinkets` | MATCH | Kept apart in storage and shelf slots; the shop shows both in one dialog, each card labelled Trinket or Cardinal. |
| 12:12 | Trinket belongs to its buyer, acts only on that player's vessel, towers, units or aura, and shows on their vessel's panel | `17_Cardinals.cs:875-893 Grant (vessel panel); 17_Cardinals.cs:968-991 Reach (buyer's units and towers)` | MATCH | Owner checks by session in Reach and Owned. |
| 12:13 | Cardinal acts on the core, enemies or Tide clock, is everyone's, shows in the Cardinal row; every base game Cardinal the shop sells that isn't a Trinket goes on the core | `17_Cardinals.cs:625-661 GrantCardinal (cores, lobby row list); 17_Cardinals.cs:1052-1091 Row; 17_Cardinals.cs:644 stock ups on core` | MATCH | The row is filled from the synced lobby list while it has room (8). |
| 12:14 | Base game split: 17 named base game Cardinals act on one player's things and are sold as Trinkets; the other 12 stay Cardinals | `17_Cardinals.cs:86-93 stockTable (17 rows, kinds Hero, Coin, Gavel, Tower, Built, Unit)` | MATCH | Seventeen rows match the scope's 17 names by game name (Caffeine as Sport Water, Bird as Bendigo, Badge as Torrid Badge). Sold count of 29 = 16 Claude + 13 stock ids plus the 17 Trinket rows as the split is written; defaults with tildes. |
| 12:15 | Base game Trinkets: a Coin pays its buyer a share split by the players; Aura Gavel fires for the buyer's own units; Torrid Badge, Bendigo, Kippy and Fish Bait make their unit beside the buyer's vessel, the buyer's own, one at a time | `24_Trinkets.cs:665-678 Pay (divides by players); 24_Trinkets.cs:714-736 Gavel; 24_Trinkets.cs:683-711 Companions; 17_Cardinals.cs:897-915 GrantStock Unit; 17_Cardinals.cs:918-933 CompanionOf` | MATCH | One companion at a time is kept by Companions, which despawns orphans once a fresh unit stands. |
| 12:16 | A lobby pick works for everyone, even one the shop sells as a Trinket; the shop then doesn't sell that one | `17_Cardinals.cs:625-661 GrantCardinal (stock on every core); 17_Cardinals.cs:399-405 MaxedFor (InLobby); 17_Cardinals.cs:561 Buy refuses lobby stock; 17_Cardinals.cs:814-861 LockLobby` | MATCH | Lobby picks are never offered on the shelf, since MaxedFor returns true for them. |
| 12:17 | No limit on how many different Cardinals or Trinkets a campaign holds; past the Cardinal row's 8 the rest show as a +N count beside it | `17_Cardinals.cs:656 row cap of 8; 17_Cardinals.cs:597-611 Overflow; 14_Run.cs:505-508 '+N Cardinals' run line` | MATCH | No cap on the held list itself. |
| 12:18 | A stacking Cardinal or Trinket works for up to 6 copies and leaves the shop at 6; a base game Cardinal is bought once and leaves the shop; a base game Trinket is bought once by each player | `17_Cardinals.cs:32 Stacks=6; 17_Cardinals.cs:336-340 Cap (6 or 1); 17_Cardinals.cs:399-405 MaxedFor; 17_Cardinals.cs:555 Trinket Cap per player` | MATCH | Stock ids have Cap 1, so each buyer holds one; the scope's bought once by each player is the default with tildes. |
| 12:19 | Price ramp: each Cardinal or Trinket costs 3 more for every Cardinal the team bought (lobby picks not counted) and every Trinket copy the buyer holds, so the fourth buy of a 15 costs 24; Punch Card's 20% comes off the ramped price | `17_Cardinals.cs:35 Ramp=3; 17_Cardinals.cs:453 PriceFor; 17_Cardinals.cs:455 RampFor (RWRun.Cardinals + buyer's Trinkets); 17_Cardinals.cs:471 Price (punch after ramp)` | MATCH | Checked by hand: 15 + 3 x 3 = 24 for a buyer after three Cardinal buys; the ramp has no cap. |
| 12:20 | Trinkets and Directions are saved with the campaign for each player and come back when it continues | `14_Run.cs:188-217 Encode and Decode (Trinkets, Directions fields); 14_Run.cs:146-171 Save and Load` | MATCH | Per-player save keys RW_Run_Player_n. |
| 12:21 | On the boat: no Cardinal or Trinket swaps between maps | `17_Cardinals.cs:779-786 RestoreLobby; 17_Cardinals.cs:814-861 LockLobby; 17_Cardinals.cs:810 comment on boat lobby` | MATCH | The lobby is locked on the boat; the sea-chart rule itself lives in 04. |
| 12:22 | Power: a Trinket is worth about one level 1 or 2 building upgrade; a Cardinal about one base game Cardinal or less | `no code location; balance guide in docs/reports/DIRECTIONS_CARDINALS_2026-11-04.md section 2` | MATCH | A design guide the numbers on each sheet follow; code cannot check it. |
| 12:23 | Pairs: two Cardinals or Trinkets that clash are a pair; a player who holds one is never offered the other | `17_Cardinals.cs:409-417 ClashFor; 17_Cardinals.cs:45-54 Clash fields (Punch and Oyster); 17_Cardinals.cs:190 MaxedFor via ClashFor` | MATCH | Shelf skips maxed (clashing) ids so the other is not offered. |
| 12:24 | No copies: a new Cardinal or Trinket never does what a base game Cardinal already does | `no runtime check in code` | MATCH | A design check against the base game's in-game lines, recorded in the reports; the code has no test of it. |
| 12:25 | Each Cardinal and Trinket says who made it, Dimitri or Claude | `17_Cardinals.cs:19-28 RWCardinalDef has no By field; no display text carries it` | MISSING | Looked for a By field or a text on the item; none. The sheet's By lines exist only in the scope file. The plan queues nothing for it. |
| 12:26 | Model Lighthouse is kept for a later Cardinal or item | `none (no Lighthouse in the code)` | UNBUILT-BY-DESIGN | The scope reserves the name for later. |
| 12:30-41 | Vampire Dentures: Trinket, line, heals 20% of damage dealt per stack, lasts the campaign, stacks, shop, Red, cost 15, look, no clash, By Dimitri | `17_Cardinals.cs:46 sheet (Trinket, Cost 15, Look, Desc); 17_Cardinals.cs:33 DenturesHeal 0.2; 17_Cardinals.cs:1038-1046 heal per stack; 17_Cardinals.cs:19-28 Team default Red` | MATCH | Colour, stacking and campaign lifetime match; the By line is doc-only (12:25). |
| 12:42-53 | Brass Spyglass: Trinket, range +0.5 for ranged, +0.25 melee, no stacks, shop, Red, cost 12, no clash | `17_Cardinals.cs:47 sheet (Stacks=false, Cost 12); 24_Trinkets.cs:45 SpyglassRanged 0.5 and SpyglassMelee 0.25; 24_Trinkets.cs:209 Range Add; 24_Trinkets.cs:223 Ranged uses Projspeed` | MATCH | Ranged is judged by a Projspeed stat on the vessel. |
| 12:54-65 | Gutting Knife: Trinket, +20% damage against foes above half health, per stack, Red, cost 12, stacks | `17_Cardinals.cs:48 sheet; 24_Trinkets.cs:46 KnifeEach 0.2 and KnifeAbove 0.5; 24_Trinkets.cs:253-268 OnDealt` | MATCH | Foes at or below half health are skipped, so the bonus applies above half. |
| 12:66-77 | Oil Can: Trinket, towers attack speed +5% per stack, Red, cost 12, stacks | `17_Cardinals.cs:49 sheet; 24_Trinkets.cs:45 OilcanEach 0.05; 24_Trinkets.cs:218 Attackspeed Multi on towers` | MATCH | Its pair Iron Rivets is dropped in the scope and not in the code. |
| 12:78-89 | Tar Bucket: Trinket, towers mend 1% of max health a second after 4 s without a hit, +1% per stack, Blue, cost 15, stacks | `17_Cardinals.cs:50 sheet (Team Blue, Cost 15); 24_Trinkets.cs:46 TarEach 0.01 and TarAfter 4; 24_Trinkets.cs:229-249 Tar; 24_Trinkets.cs:244 Mend per second` | MATCH | Mend is the shared RWUtil.Mend rule. |
| 12:90-101 | Rubber Duck: Trinket, respawn -20% per stack, at most -60%, Green, cost 10, stacks, no clash | `17_Cardinals.cs:51 sheet (Team Green, Cost 10); 24_Trinkets.cs:46 DuckEach 0.2 and DuckMost 0.6; 24_Trinkets.cs:322 Cut=min; 24_Trinkets.cs:330-349 DuckFirst and DuckLast` | MATCH | Cut applies to the Basin's Respawn for that death only. |
| 12:102-113 | Spare Plank: Trinket, one free (Defense) building at a time, at its first level, no stacks, Yellow, cost 15, clashes with a vessel without a (Defense) building | `17_Cardinals.cs:52 sheet (Wall, Team Yellow, Cost 15); 24_Trinkets.cs:353-423 Plank (free button while no plank stands); 24_Trinkets.cs:56-57 WallHeroes and WallBeings; 17_Cardinals.cs:416 ClashFor wall check` | MATCH | The code does not test the level; a free build is the game's first-level build. Buster and Argo have no wall, so they clash. |
| 12:114-125 | Punch Card: Trinket, Cardinals and Trinkets cost 20% less rounded down, no stacks, Yellow, cost 15, clashes with Pearl Oyster | `17_Cardinals.cs:53 sheet (Clash Oyster, Team Yellow, Cost 15); 17_Cardinals.cs:471 Price floor; 24_Trinkets.cs:46 PunchOff 0.2; 17_Cardinals.cs:417 ClashFor` | MATCH | The 20% comes off the ramped price (12:19). |
| 12:126-137 | Pearl Oyster: Trinket, +1 Pure Aura each Tide until the holder next buys something at VENDCINT on that map, starts again each map, no stacks, Blue, cost 15, clashes with Punch Card | `17_Cardinals.cs:54 sheet; 24_Trinkets.cs:488-501 Tide pay (Bought check); 24_Trinkets.cs:478-485 OnBuy; 14_Run.cs:247 Bought reset per map` | DIFFERENT | OnBuy sets Bought = false when the buy is the Oyster itself (id != Cardinal_Oyster), so buying the Oyster does not stop its pay; the scope says until you next buy something at VENDCINT, which the Oyster purchase is. Everything else matches. |
| 12:138-149 | Bottled Storm: Trinket, next 5 Tides take 25% more damage; when they end, one Direction below rank III goes up a rank free, or Pure Aura 20 if none can; stacks, one storm at a time, Green, cost 15 | `17_Cardinals.cs:55 sheet (Team Green, Cost 15); 24_Trinkets.cs:47-48 StormTides 5, StormTaken 0.25, StormPure 20; 24_Trinkets.cs:293-294 damage taken; 24_Trinkets.cs:502-506 countdown; 24_Trinkets.cs:511-534 StartStorm and Calm` | MATCH | Calm picks randomly among NextRanks (ranks below III); one storm at a time through StormLeft. |
| 12:150-161 | Oilskin Coat: Trinket, 10% of hits on the vessel do nothing; each stack takes a tenth of what still lands (2 stacks 19%, 3 stacks 27%), stacks, Green, cost 12 | `17_Cardinals.cs:56 sheet (Team Green, Cost 12); 24_Trinkets.cs:45-46 OilskinEach 0.1; 24_Trinkets.cs:271-273 SlideChance 1 - 0.9^n; 24_Trinkets.cs:275-301 OnTaken` | MATCH | SlideChance gives 0.19 at 2 stacks and 0.271 at 3; the slide is a per-hit chance. |
| 12:162-173 | Gull Feather: Trinket, each elite foe the vessel drowns gives +20% damage and +15% speed for 8 s; more elites make it last longer, not grow; no stacks, Red, cost 15 | `17_Cardinals.cs:57 sheet (Stacks=false, Cost 15); 24_Trinkets.cs:48 GullDamage 0.2, GullSpeed 0.15, GullLasts 8; 24_Trinkets.cs:462-465 OnDespawn extends GullUntil; 24_Trinkets.cs:467-474 Elite (RW_Modifier_ or stock elite ids); 24_Trinkets.cs:59 StockElites` | MATCH | Elites are read from the foe's upgrade list, so base game and pack modifiers both count. |
| 12:175-186 | Cork Lifebuoy: Cardinal, the Basin lives through one more killing blow each map at 1 health, after the game's own last stand; no stacks, Green, cost 25, no clash | `17_Cardinals.cs:58 sheet (Stacks=false, Team Green, Cost 25); 24_Trinkets.cs:540-547 Basins; 24_Trinkets.cs:549-563 AfterStand (after Entity_Ai_Main_Core); 24_Trinkets.cs:565-589 OnBasin sets Health 1; 24_Trinkets.cs:1012-1016 map reset` | MATCH | corked list is cleared per map in Reset. |
| 12:187-198 | Signal Flare: Cardinal, every 20 s, 15 damage to the foe with most health within 8 tiles of the Basin, +15 per stack; stacks, Red, cost 15 | `17_Cardinals.cs:59 sheet (Cost 15, Red by default); 24_Trinkets.cs:49-50 FlareEvery 20, FlareDamage 15, FlareReach 8; 24_Trinkets.cs:593-622 Flare (most health in reach, damage 15 x stacks)` | MATCH | Damage is 15 times the stack count, which reads as +15 per stack. |
| 12:199-210 | Cargo Crate: Cardinal, VENDCINT offers one more Cardinal or Trinket each break per stack, at most 6; stacks, Yellow, cost 20 | `17_Cardinals.cs:60 sheet (Team Yellow, Cost 20); 24_Trinkets.cs:50 CrateMost 6; 17_Cardinals.cs:393 ShelfFor extra = min(CrateMost, CrateStacks()); 17_Cardinals.cs:345-375 Shelf spare fill` | MATCH | Crate's own stacks are read from the host's list (Stacked). |
| 12:211-222 | Tin Whistle: Cardinal, the last 3 foes of each Tide move 30% slower and take 25% more damage; no stacks, Blue, cost 15 | `17_Cardinals.cs:61 sheet (Stacks=false, Team Blue, Cost 15); 24_Trinkets.cs:51 WhistleSlow 0.3, WhistleLast 3, WhistleTaken 0.25; 24_Trinkets.cs:743-766 Whistle (row count, 3 or fewer left); 24_Trinkets.cs:790-800 OnWhistled` | MATCH | Foes are counted against the Tide's row through ReadRow. |

### 2.10 13_Enemy_Modifiers.txt

| Lines | Says | Code | Mark | Note |
| --- | --- | --- | --- | --- |
| 4 | / New enemy types will be added later, when ready | `none` | UNBUILT-BY-DESIGN | The scope itself defers new enemy types. Only the four modifiers are built. |
| 7-8 | {Cursed}; Kind: Aura | `00_RogueWavesFoesMod.cs:104 RWFModifiers.Cursed; 00_RogueWavesFoesMod.cs:327-339 CursedTick, aura test by Near(...)` | MATCH | Aura behaviour is a range check on each foe, once a second. |
| 9 | In-game line: A curse on this foe weakens the attack of everything nearby | `00_RogueWavesFoesMod.cs:138 Make(...) Display_Desc` | MATCH | Text is identical. |
| 10 | Does: Reduces the attack of all nearby vessels, towers and units | `00_RogueWavesFoesMod.cs:302-314 Targets (Tower, Mob, Hero; not Bouthouse); 00_RogueWavesFoesMod.cs:327-339 CursedTick adds Damage Multi` | MATCH | The Bouthouse exclusion is a pitfall #85 fix, not in the scope. |
| 11 | Numbers: attack -25% within 3 tiles | `00_RogueWavesFoesMod.cs:81 CursedRange = 3f, CursedDamage = -0.25f` | MATCH | Distance is flat (x and z) in tiles. |
| 12 | Shows as: an indigo ring around the foe | `00_RogueWavesFoesMod.cs:390-399 Colour Cursed = (0.45,0.33,0.85) indigo; 00_RogueWavesFoesMod.cs:401-426 Ring, 00_RogueWavesFoesMod.cs:474-490 Wear` | MATCH | A pulsing sprite ring in that colour. |
| 17-18 | {Berserk}; Kind: Self | `00_RogueWavesFoesMod.cs:105 RWFModifiers.Berserk; 00_RogueWavesFoesMod.cs:247 BerserkTick run per marked foe` | MATCH | Acts on its own foe only. |
| 19 | In-game line: Stronger and faster the more it is hurt | `00_RogueWavesFoesMod.cs:139 Make(...) Display_Desc` | MATCH | Text is identical. |
| 20 | Does: Damage and speed increases as health lowers | `00_RogueWavesFoesMod.cs:287-293 BerserkTick sets Damage and Speed Multi from 1 - health fraction` | MATCH | Grows with missing health. |
| 21 | Numbers: at no health left: damage +100%, speed +50% | `00_RogueWavesFoesMod.cs:82 BerserkDamage = 1f, BerserkSpeed = 0.5f; 00_RogueWavesFoesMod.cs:289-293 missing = clamp01(1 - hp)` | MATCH | Values match at zero health. |
| 22 | Shows as: a hot pink ring around the foe | `00_RogueWavesFoesMod.cs:395 Colour Berserk = (1,0.32,0.62) hot pink` | MATCH | Ring drawn by RWFLooks.Wear. |
| 27-28 | {Aging}; Kind: Self | `00_RogueWavesFoesMod.cs:105-106 RWFModifiers.Aging; 00_RogueWavesFoesMod.cs:248 AgingTick run per marked foe (295-300)` | MATCH | Self only. |
| 29 | In-game line: Slow, but its health and damage grow the longer it lives | `00_RogueWavesFoesMod.cs:140 Make(...) Display_Desc` | MATCH | Text is identical. |
| 30 | Does: Speed is reduced by 30%, but health and damage increase by up to 100% as it stays alive; health is scaled, not added (50% stays 50% on a higher max) | `00_RogueWavesFoesMod.cs:273 Addmod Speed Multi AgingSpeed (-0.3); 00_RogueWavesFoesMod.cs:274-275 Damage and Health Multi mods; 00_RogueWavesFoesMod.cs:297-299 share written to both` | MATCH | Health is a Multi on max health, so the fraction is kept by the game's own Variable health (kit docs/17 §2.1), as the code comment states. Not confirmed in game here. |
| 31 | Numbers: speed -30%; health and damage up to +100%; reaches max value after surviving for 50 seconds | `00_RogueWavesFoesMod.cs:83 AgingSpeed = -0.3f, AgingMax = 1f, AgingSeconds = 50f; 00_RogueWavesFoesMod.cs:297 Clamp01((now - Born) / 50) * AgingMax` | MATCH | Reaches the maximum at 50 s. |
| 32 | Shows as: a barnacle blue ring around the foe | `00_RogueWavesFoesMod.cs:396 Colour Aging = (0.55,0.82,0.86) barnacle blue` | MATCH | Ring drawn by RWFLooks.Wear. |
| 37-38 | {Dazzling}; Kind: Aura | `00_RogueWavesFoesMod.cs:107 Blind and Illuminate upgrades; 00_RogueWavesFoesMod.cs:341-378 DazzlingTick, aura range via Near` | MATCH | Aura, once a second. |
| 39 | In-game line: Blinds and Illuminates everything nearby | `00_RogueWavesFoesMod.cs:141 Make(...) Display_Desc` | MATCH | Text is identical. |
| 40 | Does: Blinds and Illuminates nearby vessels, towers, and units (cross-reference 10_Vessel_Uloe.txt [Light]) | `00_RogueWavesFoesMod.cs:143-145 Blind (Entity_Ai_Effect_Barrel_Boost) and Illuminate (Entity_Ai_Effect_Tower_Merchut) upgrades; 00_RogueWavesFoesMod.cs:341-378 DazzlingTick applies them to Targets` | MATCH | The Uloe [Light] cross-reference is in another part's scope, not this code; only this file's wording is checked here. |
| 41 | Numbers: within 3 tiles, Blind and Illuminate at level 1 (-15% damage dealt, +15% damage taken), lasting 3 seconds after leaving | `00_RogueWavesFoesMod.cs:85 DazzlingRange = 3f, BlindDamage = -0.15f, IlluminateTaken = 0.15f, DazzleSeconds = 3f; 00_RogueWavesFoesMod.cs:144 Blind Damage_Multi and Duration_Base; 00_RogueWavesFoesMod.cs:362-370 Illuminate removed 3 s after leaving` | MATCH | Blind's 3 s comes from its timed Duration_Base; Illuminate's from the leftAt timer. |
| 42 | Shows as: a bright yellow ring around the foe | `00_RogueWavesFoesMod.cs:397 Colour default = (1,0.92,0.35) bright yellow; 00_RogueWavesFoesMod.cs:401-426 Ring` | MATCH | The default branch of Colour is the Dazzling colour. |

### 2.11 14_Optional_Objectives.txt

| Lines | Says | Code | Mark | Note |
| --- | --- | --- | --- | --- |
| 14:5 | Names: a short order, like a bounty on a notice board | `19_Objectives.cs:16-20 (Display names: Mind the Meteor, Keep the Basin Clean, Beat the Clock, Mind the Vessels, Raze the Nest)` | MATCH | Names are short orders; the bounty flavour is not a rule. |
| 14:6 | Offered at VENDCINT in every break between tides from the break before Tide 3 (balance sweep P1) | `19_Objectives.cs:34-35 (ObjectivesFrom=3, FirstTide=3), 19_Objectives.cs:111-124 (Post returns when wave+1 < FirstTide), 20_Vote.cs:93-96 and 177-182 (Break calls OpenShop, which calls Post)` | MATCH | The break after Tide 2 (wave=2) posts for Tide 3, which is the break before Tide 3. |
| 14:7 | Team pay: an objective the team takes pays every player the same, full amount | `19_Objectives.cs:240-241 (RWRun.Pay(a.Pays, ...)), 14_Run.cs:280-286 (Pay adds the amount to every session)` | MATCH |  |
| 14:8 | Like: optional objectives that players can take on for bonus Pure Aura, like the base game's bounty | `19_Objectives.cs:240-241 (paid as Pure Aura on success)` | MATCH | The bounty comparison is flavour. |
| 14:11 | {Mind the Meteor} (name) | `19_Objectives.cs:16` | MATCH |  |
| 14:12 | Kind: Mining | `19_Objectives.cs:5-9 (RWObjectiveDef has Name, Display, Line, Pays; no Kind field). The mining behaviour is in 19_Objectives.cs:273-310 (SpawnMeteor)` | MISSING | Looked for a Kind field or a label on the card and the popup; none. Behaviour exists, the kind label does not. Not queued. |
| 14:13 | In-game line: Fully mine a node that spawns in a random location on the map. | `19_Objectives.cs:16 (Line)` | MATCH | Text matches exactly. |
| 14:14 | Numbers: 12 hits + 3 per map | `19_Objectives.cs:306 (Healthresource 11 + map*3, hits = Healthresource + 1 = 12 + 3*map), 19_Objectives.cs:309 (log says 12 + map*3)` | DIFFERENT | The code gives 15 hits on map 1 (map = 1 there). The scope's 12 at the first map needs 12 + 3*(map-1). The +3 per map step matches. |
| 14:14 | Numbers: about 8 tiles + 1.5 per map from the Basin | `19_Objectives.cs:279 (want = 8f + map*1.5f), 19_Objectives.cs:293-294 (nearest standable tile to want)` | DIFFERENT | Map 1 gives 9.5 tiles, not 8. The 1.5 per map step matches. Same off-by-one as the hits. |
| 14:15 | Taken by: The team | `19_Objectives.cs:156-164 (Take: one Active objective for the team)` | MATCH |  |
| 14:16 | Offered: every break at VENDCINT from the one before Tide 3 | `19_Objectives.cs:111-124, 20_Vote.cs:93-96, 20_Vote.cs:177-182` | MATCH | Same mechanism as 14:6. |
| 14:17 | Lasts: the next tide | `19_Objectives.cs:168-195 (Begin at the Tide's start), 19_Objectives.cs:216-244 (Resolve), 20_Vote.cs:81 (Break calls Resolve)` | MATCH |  |
| 14:18 | If failed: Nothing | `19_Objectives.cs:234-239 (on failure: message only, no Pay)` | MATCH |  |
| 14:19 | Later maps: node has more health and spawns further from the player's spawn point | `19_Objectives.cs:279 (distance grows with map), 19_Objectives.cs:306 (Healthresource grows with map)` | MATCH | Direction and slope match; the map-1 offset is noted at 14:14. |
| 14:20 | Pays: Pure Aura 3 | `19_Objectives.cs:16 (Pays = 3)` | MATCH |  |
| 14:21 | Look: a node with a white crystal | `19_Objectives.cs:87-96 (RWModels.Get(RWModels.Meteor, ...) swaps the ore's look), 25_Models.cs:14 (Meteor = RW_Meteor), 25_Models.cs:22-36` | MATCH | Built as temporary art; falls back to the ore's look if the library model is missing. Not run on the PC yet. |
| 14:22 | Clashes: None | `19_Objectives.cs:156-164 (no clash rule in Take or RWObjectiveDef)` | MATCH |  |
| 14:23 | {Keep the Basin Clean} (name) | `19_Objectives.cs:17` | MATCH |  |
| 14:24 | Kind: Guard | `19_Objectives.cs:5-9 (no Kind field)` | MISSING | No kind label in code or on the card. Not queued. |
| 14:25 | In-game line: No foe hurts the Basin this Tide | `19_Objectives.cs:17 (Line)` | MATCH | Text matches exactly. |
| 14:26 | Numbers: no damage at all | `19_Objectives.cs:262-270 (BasinClean), 19_Objectives.cs:267 (fails only when Damagetaken > start + 0.01f)` | DIFFERENT | The code allows 0.01 of damage before failing; the scope says none at all. |
| 14:27 | Taken by: The team | `19_Objectives.cs:156-164` | MATCH |  |
| 14:28 | Offered: every break at VENDCINT from the one before Tide 3 | `19_Objectives.cs:111-124, 20_Vote.cs:93-96, 177-182` | MATCH |  |
| 14:29 | Lasts: the next tide | `19_Objectives.cs:168-195, 216-244, 20_Vote.cs:81` | MATCH |  |
| 14:30 | If failed: Nothing | `19_Objectives.cs:234-239` | MATCH |  |
| 14:31 | Later maps: The same | `19_Objectives.cs:262-270 (no map scaling)` | MATCH |  |
| 14:32 | Pays: Pure Aura 2 | `19_Objectives.cs:17 (Pays = 2)` | MATCH |  |
| 14:33 | Look: a mop and bucket | `15_Content.cs:14-18 (IconNames includes RogueWaves_Objective_Hull), 08_Icons.cs:139-143 (added to the icon table), 19_Objectives.cs:146-154 (Icon)` | MATCH | The icon hook is in place; whether the art shows a mop and bucket cannot be read from code. |
| 14:34 | Clashes: None | `19_Objectives.cs:156-164` | MATCH |  |
| 14:35 | {Beat the Clock} (name) | `19_Objectives.cs:18` | MATCH |  |
| 14:36 | Kind: Rush | `19_Objectives.cs:5-9 (no Kind field)` | MISSING | No kind label in code or on the card. Not queued. |
| 14:37 | In-game line: Clear the Tide with time to spare | `19_Objectives.cs:18 (Line)` | MATCH |  |
| 14:38 | Numbers: the last foe falls with at least 25% of the tide's clock left | `19_Objectives.cs:33 (ClockShare = 0.25f), 19_Objectives.cs:228 (ok = clockLeft > tideLength * ClockShare), 19_Objectives.cs:202-206 (clockLeft from the Time_Wave reading)` | DIFFERENT | The code uses strictly greater than, so exactly 25% fails; the scope says at least 25%. |
| 14:39 | Taken by: The team | `19_Objectives.cs:156-164` | MATCH |  |
| 14:40 | Offered: every break at VENDCINT from the one before Tide 3 | `19_Objectives.cs:111-124, 20_Vote.cs:93-96, 177-182` | MATCH |  |
| 14:41 | Lasts: the next tide | `19_Objectives.cs:168-195, 216-244, 20_Vote.cs:81` | MATCH |  |
| 14:42 | If failed: Nothing | `19_Objectives.cs:234-239` | MATCH |  |
| 14:43 | Later maps: The same | `19_Objectives.cs:33 and 228 (fixed 25%, no map scaling)` | MATCH |  |
| 14:44 | Pays: Pure Aura 3 | `19_Objectives.cs:18 (Pays = 3)` | MATCH |  |
| 14:45 | Look: a cracked stopwatch | `15_Content.cs:17 (RogueWaves_Objective_Clock), 08_Icons.cs:139-143, 19_Objectives.cs:146-154` | MATCH | Icon hook is in place; the art itself is not readable from code. |
| 14:46 | Clashes: None | `19_Objectives.cs:156-164` | MATCH |  |
| 14:47 | {Mind the Vessels} (name) | `19_Objectives.cs:19` | MATCH |  |
| 14:48 | Kind: Guard | `19_Objectives.cs:5-9 (no Kind field)` | MISSING | No kind label in code or on the card. Not queued. |
| 14:49 | In-game line: No vessel drowns this Tide | `19_Objectives.cs:19 (Line)` | MATCH |  |
| 14:50 | Numbers: no vessel falls | `19_Objectives.cs:247-256 (OnDespawn sets failed for a killed player vessel), 19_Objectives.cs:229 (Hands: ok = !failed)` | MATCH | Any killed player vessel fails it; this is the drowning case the scope names. |
| 14:51 | Taken by: The team | `19_Objectives.cs:156-164` | MATCH |  |
| 14:52 | Offered: every break at VENDCINT from the one before Tide 3 | `19_Objectives.cs:111-124, 20_Vote.cs:93-96, 177-182` | MATCH |  |
| 14:53 | Lasts: the next tide | `19_Objectives.cs:168-195, 216-244, 20_Vote.cs:81` | MATCH |  |
| 14:54 | If failed: Nothing | `19_Objectives.cs:234-239` | MATCH |  |
| 14:55 | Later maps: The same | `19_Objectives.cs:229 and 247-256 (no map scaling)` | MATCH |  |
| 14:56 | Pays: Pure Aura 2 | `19_Objectives.cs:19 (Pays = 2)` | MATCH |  |
| 14:57 | Look: a life ring | `15_Content.cs:17 (RogueWaves_Objective_Hands), 08_Icons.cs:139-143, 19_Objectives.cs:146-154` | MATCH | Icon hook is in place; the art itself is not readable from code. |
| 14:58 | Clashes: None | `19_Objectives.cs:156-164` | MATCH |  |
| 14:59 | {Raze the Nest} (name) | `19_Objectives.cs:20` | MATCH |  |
| 14:60 | Kind: Hunt | `19_Objectives.cs:5-9 (no Kind field)` | MISSING | No kind label in code or on the card. Not queued. |
| 14:61 | In-game line: A halberd nest lands near the foes: destroy it before the Tide ends | `19_Objectives.cs:20 (Line)` | MATCH | Text matches exactly. |
| 14:62 | Numbers: 3 to 7 tiles from an enemy core | `07_Weather.cs:362-379 (NestTile: d < 3f \|\| d > 7f skipped, measured from the core flat)` | MATCH | Also requires a height within 2.5 of the core, which the scope does not mention. |
| 14:63 | Taken by: The team | `19_Objectives.cs:156-164` | MATCH |  |
| 14:64 | Offered: every break at VENDCINT from the one before Tide 3, never on a Nesting Ground map | `19_Objectives.cs:116 (from the break before Tide 3), 19_Objectives.cs:120 (Nest removed when RWWeather.Current == NestingGround or no room)` | MATCH |  |
| 14:65 | Lasts: the next tide | `19_Objectives.cs:168-195, 216-244, 20_Vote.cs:81` | MATCH |  |
| 14:66 | If failed: Nothing | `19_Objectives.cs:226 (Nest: ok = targetFell), 19_Objectives.cs:234-239 (no pay on failure)` | MATCH |  |
| 14:67 | Later maps: The same | `19_Objectives.cs:314 (SpawnNest via RWWeather.SpawnNestFor), 07_Weather.cs:359 and 390-415 (no map scaling)` | MATCH |  |
| 14:68 | Pays: Pure Aura 4 | `19_Objectives.cs:20 (Pays = 4)` | MATCH |  |
| 14:69 | Look: a burning nest | `15_Content.cs:17 (RogueWaves_Objective_Nest), 08_Icons.cs:139-143, 19_Objectives.cs:146-154; the nest itself is the stock Tower_Halberdnest (07_Weather.cs:392)` | MATCH | Icon hook is in place; the burning look of the nest is stock art, not readable from code. |
| 14:70 | Clashes: None | `19_Objectives.cs:156-164` | MATCH |  |

### 2.12 16_Base_Vessels.txt

| Lines | Says | Code | Mark | Note |
| --- | --- | --- | --- | --- |
| 16:9-19 | Side-by-side table of the five base vessels' stats (difficulty, attack, health, speed, damage, range, attack speed, build range, sell rate). | `none in RogueWavesMode` | UNBUILT-BY-DESIGN | The pack builds no base vessel. Lines 2-4 say these numbers are the game's own data. The code given has no vessel definitions; RWBase and RWDirections touch vessels only through scope 11 Directions, which this file does not list. |
| 16:22-31 | Smacky's identity: title, Basic difficulty, archetype, in-game line, look, accent colour #80B3FF, lore. | `none in RogueWavesMode` | UNBUILT-BY-DESIGN | Game data and text; the pack does not set vessel identity. |
| 16:33-43 | Smacky's vessel stats: melee, health 50, regen 0, speed 3, damage 4, range 1.5, attack speed 0.5, build range 2, sell rate 50%, no special rule. | `none in RogueWavesMode` | UNBUILT-BY-DESIGN | Game data read by the game, not built by the pack. |
| 16:45-67 | Smacky's Archer Tower: stats (health 25 +25/level, damage 4 +1/level, attack speed 0.7, range 6, projectile speed 20, cost Red 25) and upgrades Long Bow, Rapid Fire, Fire Arrows (tier 1). | `none in RogueWavesMode` | UNBUILT-BY-DESIGN | Game building data. Fire Arrows' 60% and 5-part timing are read from the game's code per the scope's own note. |
| 16:69-98 | Smacky's Archer Tower tiers 2-4: Eagle Eyes, Auto Bow, Heavy Bow, Bow Squad, Explosive Arrows, Assassin Shots, Death Mark, with their numbers and costs. | `none in RogueWavesMode` | UNBUILT-BY-DESIGN | Game upgrade data; the pack does not build or change these. |
| 16:100-123 | Smacky's Bomb Tower: stats (health 50, damage 3, attack speed 0.35, range 5, projectile count 5) and tier 1 upgrades Quick Bombs, Bomb Launcher, Bigger Bombs. | `none in RogueWavesMode` | UNBUILT-BY-DESIGN | Game building data. |
| 16:125-150 | Smacky's Bomb Tower tiers 2-4: Cannons, Dynamite, Staggering Bombs, Bouncing Bombs, Splitting Bombs, Detonate, with numbers and costs. | `none in RogueWavesMode` | UNBUILT-BY-DESIGN | Game upgrade data. |
| 16:152-170 | Smacky's Wall: health 50 +150/level, cost Blue 20; tier 1 Reinforced (health +100%), Repair Team (7.5 heal a second), Spikes (damage 3). | `none in RogueWavesMode` | UNBUILT-BY-DESIGN | Game building data. Mason's Trowel in scope 11 is a Direction and is RWBase.Trowel (23_BaseDirections.cs:19-21, :204-212), not this wall line. |
| 16:172-201 | Smacky's Wall tiers 2-4: Stone Wall, Rejuvenate, Hidden Surprise, Steel Wall, Perma-Wall, Bramble, Beacon, with numbers and costs. | `none in RogueWavesMode` | UNBUILT-BY-DESIGN | Game upgrade data. |
| 16:203-219 | Smacky's Mining Hut: stats (health 25 +25/level, respawn 10 s, units 1, cost Yellow 35) and tier 1 Mining Operation (units +1), Overtime (miner speed and attack speed +75%). | `none in RogueWavesMode` | UNBUILT-BY-DESIGN | Game building data. |
| 16:221-250 | Smacky's Mining Hut tiers 2-4: Ore Scraps, Mining Union, Processing Plant, Marketplace, Support Team, Excavation Facility (Real Tough miners), Engineering Firm, with numbers and costs. | `none in RogueWavesMode` | UNBUILT-BY-DESIGN | Game upgrade data. |
| 16:252-275 | Smacky's Miner and Engineer minions: health, damage, range, attack speed, speed, respawn, orders, and the engineer's buff (range +25%, attack speed +50%, damage +25% for 8 s). | `none in RogueWavesMode` | UNBUILT-BY-DESIGN | Game minion data. The engineer buff is read from the game's code. |
| 16:278-287 | Sno Sno's identity: title, Intermediate difficulty, archetype, in-game line, look, accent colour #DEDEDE, lore. | `none in RogueWavesMode` | UNBUILT-BY-DESIGN | Game data and text. |
| 16:289-309 | Sno Sno's vessel stats (health 25, speed 2, damage 3, range 3, projectile speed 15, attack speed 0.5), the Snoborne rule, and the Sno resource (level 0-100 per tile, earned by leaving Level-20 Sno, reset between maps). | `none in RogueWavesMode` | UNBUILT-BY-DESIGN | Game vessel and resource data; the pack does not model Sno. |
| 16:311-313 | Snoborne mechanic: leaves Level-20 Sno when walking, 75% faster on thick Sno above Level 20. | `none in RogueWavesMode` | UNBUILT-BY-DESIGN | Game mechanic. RWBase's Snow Shovel only reads the Snosno Shovel Direction; it does not implement Snoborne. |
| 16:315-327 | Sno Sno's Sno Cannon: eats Sno for ammo and fires; stats (health 50, damage 5, attack speed 0.7, range 6.5, projectile speed 15, Sno range 2 +0.25/level, ammo 10, cost Red 30). | `none in RogueWavesMode` | UNBUILT-BY-DESIGN | Game building data. |
| 16:329-354 | Sno Cannon tier 1-2 upgrades: Sno Blower, Sno Chomper, Sno Boulder, Extra Frosty, Snopowder, Sno Sucker, with numbers and costs. | `none in RogueWavesMode` | UNBUILT-BY-DESIGN | Game upgrade data. |
| 16:356-376 | Sno Cannon tiers 3-4: Hypercool, Icicle Shot, Permasno, Snoble Alert, Snoblaster 7A+, with numbers and costs. | `none in RogueWavesMode` | UNBUILT-BY-DESIGN | Game upgrade data. |
| 16:378-390 | Sno Sno's Heater: stats (health 100 +100/level, damage 1, attack speed 1, range 3.5, sno melt speed 10, sno range 3, pulse attack speed 0.25, cost Red 60). | `none in RogueWavesMode` | UNBUILT-BY-DESIGN | Game building data. |
| 16:392-412 | Heater tiers 1-2: Double Burn, Burning Up, Cookin' Eggs, Temperature Shock, Heat Treat. | `none in RogueWavesMode` | UNBUILT-BY-DESIGN | Game upgrade data. |
| 16:414-426 | Heater tiers 3-4: 134 - 57, Scorcher, Cold Sweats, with numbers and costs. | `none in RogueWavesMode` | UNBUILT-BY-DESIGN | Game upgrade data. |
| 16:428-437 | Sno Sno's Ice Brick: stats (health 50 +50/level, overshield 1, heal 1, heal delay 1, cost Blue 15). | `none in RogueWavesMode` | UNBUILT-BY-DESIGN | Game building data. |
| 16:439-481 | Ice Brick upgrades, tiers 1-4: A Cold One, Snoball Effect, Thin Ice, Cold Shoulder, On The Rocks, Brain Freeze, Ice Breaker, Cold Feet, Tip of the Iceberg, Take the Heat, with numbers and costs. | `none in RogueWavesMode` | UNBUILT-BY-DESIGN | Game upgrade data. |
| 16:483-493 | Sno Sno's Sno Box: stats (health 25 +25/level, damage 0.4, attack speed 0.2, range 3 +1/level, heal 15, projectile flight 2, cost Blue 10 + Yellow 25). | `none in RogueWavesMode` | UNBUILT-BY-DESIGN | Game building data. |
| 16:494-515 | Sno Box tier 1-2 upgrades: Sno Fall, Slush, Sno Bank, Hard Sno, Soft Sno, with numbers and costs. | `none in RogueWavesMode` | UNBUILT-BY-DESIGN | Game upgrade data. |
| 16:516-533 | Sno Box tiers 3-4: Black Sno, Rich Sno, Snobles (every 5th siphon throws a Snoble), BFS, with numbers and costs. | `none in RogueWavesMode` | UNBUILT-BY-DESIGN | Game upgrade data. |
| 16:535-557 | Sno Sno's Snoble and Mini Snoble minions: health, damage, range, attack speed, drain, orders. | `none in RogueWavesMode` | UNBUILT-BY-DESIGN | Game minion data. |
| 16:559-572 | Sno Sno's Snoble Mother minion: health 250, damage 25, range 3, attack speed 0.1, speed 0.25, rank-up thresholds 2500 and 7500, death damage 150, orders. | `none in RogueWavesMode` | UNBUILT-BY-DESIGN | Game minion data. |
| 16:575-599 | Boutman's identity (Advanced, archetype, in-game line, look, accent #FFC34C, lore) and vessel stats (health 50 +50/level, speed 3, damage 4, range 2, attack speed 0.75, fishing speed 0.3, fishing range 3, heal 1, special rule The Bout Way). | `none in RogueWavesMode` | UNBUILT-BY-DESIGN | Game vessel data and text. |
| 16:601-612 | Boutman's Fish Follow ability (recruit a fish, no cooldown, from the start) and the The Bout Way mechanic (Boutbro builds, fish cost (n-1) x Yellow 25, no mining until Fishbender, never drowns after Enter the Boutman). | `none in RogueWavesMode` | UNBUILT-BY-DESIGN | Game mechanic. RWBase's Worm Tin only changes Boutbro's cost stats (WormStats, 23_BaseDirections.cs:25, :235-245); it does not implement The Bout Way. |
| 16:614-620 | Bouthouse: Boutman's upgrade panel, summons Boutbro; stats respawn 10 s, cost free. | `none in RogueWavesMode` | UNBUILT-BY-DESIGN | Game building data. |
| 16:621-643 | Bouthouse tiers 1-2: Fish of Fury, Enter the Boutman, The Way of the Bout, Shao-min, Fight Blub, with numbers and costs. | `none in RogueWavesMode` | UNBUILT-BY-DESIGN | Game upgrade data. |
| 16:644-664 | Bouthouse tiers 3-4: 1 Inch Bout, Iron Fish, Fishbender, Ip Man-of-War, Kung Fu Tackle, with numbers and costs. | `none in RogueWavesMode` | UNBUILT-BY-DESIGN | Game upgrade data. |
| 16:666-676 | Boutman's Fish Barrel: stats (health 50 +25/level, range 6, heal 2 +1/level, units 1, free slots 1, speed 1, cost Blue 25). | `none in RogueWavesMode` | UNBUILT-BY-DESIGN | Game building data. |
| 16:678-702 | Fish Barrel tiers 1-2: Pond, Pool, Tank, Drybed, Farmingbed, Spawnbed, with numbers and costs. | `none in RogueWavesMode` | UNBUILT-BY-DESIGN | Game upgrade data. |
| 16:704-720 | Fish Barrel tiers 3-4: Janitorial Bucket, Country Gutter, IT Watercooler, Bout Inc Watertower, with numbers and costs. | `none in RogueWavesMode` | UNBUILT-BY-DESIGN | Game upgrade data. |
| 16:722-733 | Boutman's Boutboat: stats (health 75 +75/level, damage 3 +2/level, attack speed 2, range 4.5, landing speed 5, projectile speed 10, callcooldown 5, cost Red 100). | `none in RogueWavesMode` | UNBUILT-BY-DESIGN | Game building data. |
| 16:734-756 | Boutboat tiers 1-2: Bigger Boat, Speed Boat, Bout Barge, Bout Brig, Bout Raft, with numbers and costs. | `none in RogueWavesMode` | UNBUILT-BY-DESIGN | Game upgrade data. |
| 16:757-773 | Boutboat tiers 3-4: Bout Battleship, Bout Cargo Inc, Bout Yacht, Boutanic, with numbers and costs. | `none in RogueWavesMode` | UNBUILT-BY-DESIGN | Game upgrade data. |
| 16:775-788 | Boutbro's Gubby fish minion: health 50 +75/level, damage 10 +10/level, range 1.75, attack speed 0.3, speed 1.25, engage range 3, deadarmor 10 +5/level, orders, cost Yellow 25 rising per fish. | `none in RogueWavesMode` | UNBUILT-BY-DESIGN | Game minion data. |
| 16:791-820 | Gubby upgrades tiers 1-2: Barp, Batfish, Boldfish, Balibut, Boi, Bwordfish, Bbetta, with numbers and costs. | `none in RogueWavesMode` | UNBUILT-BY-DESIGN | Game upgrade data. |
| 16:821-837 | Gubby upgrades tiers 3-4: Bar, Baviar, Pubberfish, Bass, with numbers and costs. | `none in RogueWavesMode` | UNBUILT-BY-DESIGN | Game upgrade data. |
| 16:839-854 | Boutbro's stats: health 10, damage 3, range 1.5, attack speed 0.5, speed 3, build range 2, sell rate 50%, cost multipliers 1, 1 map, respawn 10 s, orders builds and mines, cost free. | `none in RogueWavesMode` | UNBUILT-BY-DESIGN | Game minion data. The cost multipliers are the ones Worm Tin rewrites (23_BaseDirections.cs:235-245), which is a scope 11 Direction. |
| 16:856-870 | Boutbro's Buppy minion: health 200, damage 25, speed 1, engage range 1000, self-damage 25 per hit, one per fish leaving a barrel after Bout Inc Watertower, orders. | `none in RogueWavesMode` | UNBUILT-BY-DESIGN | Game minion data. |
| 16:872-886 | Boutdad and Boutmom minions: made by the Boutboat, use the building's stats, counts, respawn, orders (Boutmom's bonus past 5 boats with Bout Raft). | `none in RogueWavesMode` | UNBUILT-BY-DESIGN | Game minion data. The pack's own code does not read Bout Raft. |
| 16:889-914 | Fie's identity (Intermediate, archetype, in-game line, look, accent #FFA76D, lore) and vessel stats (health 50, speed 3, damage 3, range 2, attack speed 0.5, build range 3, sell rate 60%) and the The Wall mechanic (Grow Wall, Blue 20 each, three upgrades spread to connected pieces). | `none in RogueWavesMode` | UNBUILT-BY-DESIGN | Game vessel data and mechanic. |
| 16:916-928 | Fie's The Wall building: stats (health 40 +40/level, healthadd 0, cost Blue 20); tier 1 Flux, health +30. | `none in RogueWavesMode` | UNBUILT-BY-DESIGN | Game building data. |
| 16:930-941 | The Wall tier 2: Carbon Infused (+25% health), Lifeline, Invisible Forces (resistance +0.05), with costs. | `none in RogueWavesMode` | UNBUILT-BY-DESIGN | Game upgrade data. |
| 16:943-963 | The Wall tiers 3-4: Shared Strength, Unstable Wall (45 damage, range 2), Living Wall (damage 3, attack speed 3, range 7), Fake Wall, True Wall, with costs. | `none in RogueWavesMode` | UNBUILT-BY-DESIGN | Game upgrade data. |
| 16:965-976 | Fie's Silo Pipes: stats (health 20 +20/level, damage 0.3, attack speed 0.25, gun 1, gun speed 0.05, build speed 0.25, connect 1, cost Yellow 35). | `none in RogueWavesMode` | UNBUILT-BY-DESIGN | Game building data. |
| 16:977-1003 | Silo Pipes tiers 1-3: High Flow, Arsenal, Rapid Response, Reroute, Circular Routing, Front-Line Pipes, Old Pipes, Quick Construct, with numbers and costs. | `none in RogueWavesMode` | UNBUILT-BY-DESIGN | Game upgrade data. |
| 16:1004-1020 | Silo Pipes tier 4 and Hyper Pipes: Pipe Surge and Hyper Pipes (600 s, 1 Red, Blue and Yellow each second), with costs. | `none in RogueWavesMode` | UNBUILT-BY-DESIGN | Game upgrade data. |
| 16:1022-1034 | Fie's Knight's Keep: stats (health 20 +20/level, respawn 10 s, units 1, cost Red 60); tier 1 Lone Knight and Partner. | `none in RogueWavesMode` | UNBUILT-BY-DESIGN | Game building data. |
| 16:1035-1065 | Knight's Keep tiers 2-3: Security Group, Squad, Trailblazer, Battalion, Knight Commander, Sword Hand, with numbers and costs. | `none in RogueWavesMode` | UNBUILT-BY-DESIGN | Game upgrade data. |
| 16:1066-1073 | Knight's Keep tier 4: Convoy and True Knight, with numbers and costs. | `none in RogueWavesMode` | UNBUILT-BY-DESIGN | Game upgrade data. |
| 16:1075-1086 | Fie's Rest Stop: stats (health 25 +25/level, attack speed 0.25, range 4.5 +1/level, projectile speed 3, effect 1, duration 10 s, damagemulti 0.15, cost Yellow 50). | `none in RogueWavesMode` | UNBUILT-BY-DESIGN | Game building data. |
| 16:1087-1116 | Rest Stop tiers 1-2: Spicy Calamari, Hearty Ramen, Fresh Sushi, Side of Calamari, Bigger Portions, Side of Ramen, Side of Sushi, with numbers and costs. | `none in RogueWavesMode` | UNBUILT-BY-DESIGN | Game upgrade data. |
| 16:1118-1138 | Rest Stop tiers 3-4: Finish the Plate, Regular Customer, Second Servings, All You Can Eat, Specific Tastes, with numbers and costs. | `none in RogueWavesMode` | UNBUILT-BY-DESIGN | Game upgrade data. |
| 16:1140-1152 | Fie's Knight minion: health 25, damage 1 +0.5/level, range 1, attack speed 1, speed 1, engage range 1, commander 0.25, one per Keep, respawn 10 s, orders, cost free. | `none in RogueWavesMode` | UNBUILT-BY-DESIGN | Game minion data. |
| 16:1153-1160 | The Knight's seven weapons (Sub-Machinegun, Shotgun, Sniper, Pistol, Rifle, Grenade, Ex Rifle) with their attack speed, damage, speed, range and health additions. | `none in RogueWavesMode` | UNBUILT-BY-DESIGN | Game weapon data. |
| 16:1163-1196 | Buster's identity (Advanced, archetype, in-game line, look, accent #FF8080, lore), vessel stats (health 50, speed 3.5, damage 1.5, range 1.5, attack speed 0.5, build range 2.5, sell rate 75%, speed damage 1, ram delay 0), and the Dash ability (straight line, 0.5 s cooldown, keys 6/7). | `none in RogueWavesMode` | UNBUILT-BY-DESIGN | Game vessel and ability data. Buster's Leash Direction (RWBase.Leash, 23_BaseDirections.cs:104-105, :290-307) reads the dash but does not build it. |
| 16:1198-1207 | Buster's Pedestal: a tracking beam; stats (health 25 +75/level, damage 2 +1/level, attack speed 0.4, range 5, beam break range 2, cost Red 25). | `none in RogueWavesMode` | UNBUILT-BY-DESIGN | Game building data. |
| 16:1209-1243 | Pedestal tiers 1-3: Improved Effector, Burst Effector, Inert Capacity, Stored Charge, Combo'd, Blast Maker, Blaze Effector, Blink Chain, with numbers and costs. | `none in RogueWavesMode` | UNBUILT-BY-DESIGN | Game upgrade data. |
| 16:1244-1252 | Pedestal tier 4: Blast Extra Zed and Pure Ex, with numbers and costs. | `none in RogueWavesMode` | UNBUILT-BY-DESIGN | Game upgrade data. |
| 16:1254-1261 | Buster's Garage: stats (health 50 +50/level, respawn 5 s, units 1, cost Red 60), spawns dogs that run one lap. | `none in RogueWavesMode` | UNBUILT-BY-DESIGN | Game building data. |
| 16:1262-1301 | Garage upgrades tiers 1-4: Jack Up, Pit Crew, Big Dog, Up Dog, Lap Dog, Diesel Dog, Mechanic's Friend, Rev Dog, Nitro Collar, with numbers and costs. | `none in RogueWavesMode` | UNBUILT-BY-DESIGN | Game upgrade data. |
| 16:1303-1311 | Buster's Speed Pad: stats (speed 2, dog speed 0, maxdogspeed 2, effect 1, maxspeed 5, cost Blue 25). | `none in RogueWavesMode` | UNBUILT-BY-DESIGN | Game building data. |
| 16:1312-1347 | Speed Pad tiers 1-3: Charge Pad, Boost Pad, Strong Finish, Speed Bump, Wrong Way, Refuel, Airtime, Circuit, with numbers and costs. | `none in RogueWavesMode` | UNBUILT-BY-DESIGN | Game upgrade data. |
| 16:1348-1356 | Speed Pad tier 4: Collision Course and Breakneck, with numbers and costs. | `none in RogueWavesMode` | UNBUILT-BY-DESIGN | Game upgrade data. |
| 16:1358-1370 | Buster's Checkpoint: stats (attack speed 0.3, range 10, charge capacity 3, charge per tick 0.5, efficiency 0, least 10, most 100, effect range 2, collect range 0.6, cost Yellow 50). | `none in RogueWavesMode` | UNBUILT-BY-DESIGN | Game building data. |
| 16:1371-1393 | Checkpoint tiers 1-2: Lap 1, Quick Pass, Lap 2, Long Ranged, Tune Up, with numbers and costs. | `none in RogueWavesMode` | UNBUILT-BY-DESIGN | Game upgrade data. |
| 16:1394-1411 | Checkpoint tiers 3-4: Lap 3, Strong Start, Marathon, Final Lap, with numbers and costs. | `none in RogueWavesMode` | UNBUILT-BY-DESIGN | Game upgrade data. |
| 16:1413-1425 | Buster's Dog minion: health 50 +10/level, damage 3, range 0.1, speed 2, speed damage 1 +0.25/level, speed after collision -1, ram delay 0.2, one per Garage, respawn 5 s, orders, cost free. | `none in RogueWavesMode` | UNBUILT-BY-DESIGN | Game minion data. |

## 3. Docs

2 findings from reading `CLAUDE.md`, PLAN Part C "Now", the reports' README, the batch README and `docs/skills/` against each other.

| Where | Kind | Problem | Fix |
| --- | --- | --- | --- |
| `docs/PLAN.md:41` | stale | The 2026-11-05 'API credit set up' entry says the batch checks cost about $3.50 a run on Sonnet 5.5 and $0.70 on Haiku 5.5. TDS_Mod/Tools/batch/README.md (its 'Cost per run' table, measured on base efc1157 on 2026-11-06) gives about $4.07 and $0.76 for the four checks, and the 2026-11-06 entry in the same Part C extends those checks. | Update the figures to about $4.07 (Sonnet 5.5) and $0.76 (Haiku 5.5) for all four checks, or say the older figures were for text and scope only and point to the batch README's table. |
| `docs/reports/README.md:11` | stale | The STOCK_TRINKETS row says 13 Trinkets and 16 Cardinals. PLAN Part C's later entry ('his answers on the split') records his answer as 17 of the 29 sold being Trinkets, which is the newer statement and wins. | Change the row to 17 Trinkets and 12 Cardinals, noting that the split was revised by Dimitri's answers (see PLAN Part C and the report's §6-§7), or point the row at the report's own top note. |

Cited paths the free local pass could not find: 3.

- docs/reports/README.md:70: `TDS_Mod/Handoff/Argo_next/`
- TDS_Mod/Tools/batch/README.md:16: `Art/Temporary/game_size/`
- docs/skills/pc-round.md:16: `pc/QUEUE.md`

## 4. Requests

| Request | Result | Input tokens | Output tokens | $ |
| --- | --- | --- | --- | --- |
| design-Argo | max_tokens | 129,857 | 24,000 | 0.070 |
| design-Spool | max_tokens | 101,978 | 24,000 | 0.061 |
| design-Uloe | max_tokens | 104,158 | 24,000 | 0.062 |
| design-Yossele | max_tokens | 134,857 | 24,000 | 0.071 |
| docs-01 | ok | 26,419 | 4,222 | 0.003 |
| scope-01 | ok | 250,970 | 11,130 | 0.021 |
| scope-03 | ok | 348,669 | 5,141 | 0.044 |
| scope-04 | ok | 253,662 | 24,654 | 0.039 |
| scope-05 | ok | 251,127 | 2,807 | 0.011 |
| scope-06 | ok | 251,777 | 10,640 | 0.021 |
| scope-07 | max_tokens | 137,104 | 40,000 | 0.092 |
| scope-08 | max_tokens | 106,634 | 40,000 | 0.082 |
| scope-09 | ok | 133,933 | 33,522 | 0.083 |
| scope-10 | ok | 103,354 | 37,305 | 0.078 |
| scope-11 | max_tokens | 686,803 | 40,000 | 0.193 |
| scope-12 | ok | 255,733 | 20,674 | 0.105 |
| scope-13 | ok | 261,240 | 6,760 | 0.090 |
| scope-14 | ok | 251,223 | 15,137 | 0.026 |
| scope-16 | ok | 283,201 | 14,748 | 0.034 |
| text-01 | ok | 10,810 | 3,939 | 0.002 |
| text-02 | ok | 11,106 | 4,345 | 0.002 |
| text-03 | ok | 10,974 | 3,088 | 0.001 |
| text-04 | ok | 10,223 | 3,261 | 0.001 |
| text-05 | ok | 10,691 | 4,424 | 0.002 |
| text-06 | ok | 3,108 | 717 | 0.000 |

A result other than `ok`: `max_tokens` (answer cut, rerun that request with a lower effort), `refusal`, `unreadable answer` or `errored` (rerun it alone with `--check` and `--scope-files`).

Spent on batch checks in 2026-11, this run included: $1.19 of the $100 monthly API credit.

<!-- batch-bases: {"docs": "86988e1fcd8ab90e56fe3e07e8139f8b79f5b71e", "text": "86988e1fcd8ab90e56fe3e07e8139f8b79f5b71e"} -->
