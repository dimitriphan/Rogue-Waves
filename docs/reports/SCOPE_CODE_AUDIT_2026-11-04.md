# Scope against code, line by line (2026-11-04)

Dimitri, in the project (20:17Z): "I need to maximize my token usage before the Thursday reset. Are there any token
intense quality checks that you would recommend on this project?", then (20:18Z) "Start with 1, 2, and 3". This is
item 2: every line of `docs/scope/00`-`16` read against what the code in `TDS_Mod/ModSource` actually does.

**What was checked.** Every scope line that states something the code can confirm: a number, a timing, a cost, a
count, an in-game text, a rule, who gets what. The code is the base branch at `5d9b09e` (PR #79 merged). Code paths
below are relative to `TDS_Mod/ModSource`. The base vessels' numbers (scope `16`) were checked against the game's own
data in `TDS_Vessel_Creator/reference/vessel_data_survey.txt` and kit `docs/17_BASE_VESSELS.md`.

**What counts as an issue.** The scope and the code disagree on something a player can see, or the scope describes
something that is neither built nor marked as not built yet. A difference that only shows in a comment or a status line
is listed in §4, not counted. Work that is designed and marked unbuilt (scope `07`/`08` upgrades, the new Directions,
Cardinals and Trinkets, Argo's review, Uloe's 24 upgrades, the minion picks) is listed under "Designed, not built", not
as an issue.

**Who should yield.** *Code*: the scope line is right and the code should change to match it. *Scope*: the code is
right and the line should be rewritten. *Dimitri*: a real choice either way; my lean is given.

**Nothing was changed.** No scope file and no code was touched; each row waits for Dimitri. Everything here was read from
the code, not run: only rows that say "measured" were seen in game, on an earlier PC round. Nothing ran on the PC for
this report.

**Since the audit (PRs #80 and #82, merged the same evening).** Line numbers below point at `5d9b09e` (read them with
`git show 5d9b09e:<path>`); the code simplification (#80) moved each vessel's helpers into `Shared/ModKitUtil.cs` and
`Shared/ModKitAssets.cs`, so many vessel line numbers have moved since. Every issue row was re-read on the merged code
and still holds. What changed around them: Spool's elite check is now the shared `ModKitUtil.Elite`, still `Enemy_` only
(08 row 1); Voice in a Bottle now works on pack buildings, so Cheap Refits undoes it there too (11 row 3); Uloe's unused
stock Blind and Illuminate route is gone, which leaves the foes part as the only user of the stock Merchut mark (13
notes); held Cardinals now go back on at every map (#82), which changes no row. The hit handlers stay as they were (#80
did not build its finding 1), so 07 row 3 and 11 row 4 read the current code.

## 1. Summary

| File | Lines checked | Issues | On `~defaults~` | Designed, not built | Second pass |
| --- | ---: | ---: | ---: | --- | --- |
| `00_Read_Me` | 0 | 0 | 0 | | not needed: only the scope's own rules |
| `01_Expansion` | 14 | 0 | 0 | the first release's one-part pack | not needed |
| `02_Progress` | 11 | 0 | 0 | | not needed: the [Log] is history |
| `03_Bug_Fix` | 8 | 0 | 0 | | not needed (one stale line, §4) |
| `04_Rogue_Waves_Mode` | 66 | 5 | 3 | [Depth] | ran |
| `05_Gameplay_Rundown` | 9 | 0 | 0 | the Depth unlock | not needed (its one mismatch is 04's row 1) |
| `06_New_Mechanics` | 30 | 0 | 0 | Duos | not needed |
| `07_Vessel_Yossele` | 244 | 4 | 1 | Wood Wide Web | ran |
| `08_Vessel_Spool` | 191 | 1 | 0 | 22 upgrades, the UNTHREAD button | not needed |
| `09_Vessel_Argo` | 201 | 6 | 6 | the 14 review changes, the Reel's hint | ran |
| `10_Vessel_Uloe` | 108 | 1 | 1 | 24 upgrades | ran |
| `11_Directions` | 176 | 10 | 5 | 32 sheets | ran |
| `12_Cardinals` | 24 | 4 | 0 | 15 sheets | ran |
| `13_Enemy_Modifiers` | 22 | 4 | 3 | | ran |
| `14_Optional_Objectives` | 59 | 4 | 3 | | ran |
| `15_Templates` | 9 | 1 | 0 | | not needed |
| `16_Base_Vessels` | 1,195 | 1 | 0 | | not needed (44 lines need the decompiled game, §5) |
| **Total** | **2,367** | **41** | **22** | | |

So 19 of the 41 are on Dimitri's own lines (no `~`): for those the code is usually what should move, unless he
prefers what plays today. The other 22 are Claude's defaults, which he hasn't taken yet.

The second pass ran on every file with more than three issues. It re-read every in-game text and every place a number
is used a second time. It added 04 row 5, 07's Compost Bin text (11 row 8), Argo rows 4 and 6, 11 rows 3, 4 and 5, 12
row 4, and the Dazzling linger (13 row 3).

## 2. Decide these first

These change what a player sees in a normal campaign, and most are on Dimitri's own lines.

1. **No map vote after a bonus-Tide loss or a host quit** (04 row 1). The leader steers to the next map alone.
2. **The lobby Cardinal picker stays open between maps, unfiltered** (04 row 2). Players can still pick and swap lobby
   Cardinals before each map, including Moon Core and the others the shop leaves out.
3. **A held Trinket shows nowhere, and neither does a Cardinal past the eighth** (12 rows 2 and 3).
4. **A stock Cardinal can be bought only once, but the shelf keeps offering it** (12 row 4). The player gets a card
   they can't buy.
5. **The three Shift Directions don't change the building**: the Tree stays "Primordial Dandelion Tree" under Dandelion
   Season and Mother of Mushrooms, and the Loom stays a Loom under Sewing Machine (11 rows 6, 7 and 9).
6. **The pack's own foe modifiers aren't elites for Spool's Thread** (08 row 1), and **Cursed also weakens vessels**
   (13 row 1).
7. **Three (Any) Directions do more or less than their sheet**: Cheap Refits cancels Voice in a Bottle, Tidal Mend
   heals times the tower's level, Made of Tougher Stuff never reaches Boutbro (11 rows 1, 3 and 4).
8. **Argo's slows stack with their own re-ticks**: a 30% pool slows 60% (09 rows 2-4, known from his review), and
   Salt Sting's no-Brine number (09 row 5, known).
9. **Yossele**: FERTILIZE has no range on a click, and every damage reduction on a building is capped at 90% (07 rows 1
   and 3).

## 3. File by file

### 00_Read_Me.txt

Nothing for the code to match: the file holds the scope's own rules (symbols, values, names, layout).

### 01_Expansion.txt

Checked 14 lines; no issue.

**Designed, not built**

| Line(s) | What | Marked by | Plays today |
| --- | --- | --- | --- |
| 01:21-28 | The first release: the pack holding only Yossele, version 0.1.0, as a zip | 01:28 Status; `docs/PLAN.md` Part B "Release 1" | The pack step builds (`Pack/pack.txt`); the parts still carry the dev version `0.1.0-dev` (`Yossele/Code/02_Data.cs:8`), and Yossele waits for his sprites |

**Notes**
- 01:25 matches: FERTILIZE appears only once a Compost Bin Direction is held (`Yossele/Code/06_HeroBrain.cs:147-166`),
  and Directions exist only in the mode, so Yossele has no FERTILIZE in a normal game.
- 01:17 "Players: 2 to 4" and 01:19 "everyone needs the same pack version" are rules for players, not features. Nothing
  stops a solo campaign (the self-tests run one), and the boat can only tell that a friend has no mods on at all, not
  which pack version they have (`RogueWavesMode/Code/13_Card.cs:196-244`).

### 02_Progress.txt

Checked the 11 [Now] lines that say what is built and passed, against the code and the reports they cite; no issue.
The [Log] is history and was not audited.

### 03_Bug_Fix.txt

Checked 8 lines; no issue. 03:13 matches `RogueWavesMode/Code/09_Rejoin.cs:1-14`: the host keeps each seat's vessel,
team and aura, and opens the lobby while a seat is empty. 03:7 is stale (§4).

### 04_Rogue_Waves_Mode.txt

Checked 66 lines; 5 issues (3 on `~defaults~`).

| # | Line | Scope says | Code does | Who should yield |
| --- | --- | --- | --- | --- |
| 1 | 04:58 (and 05:7) | "Every map is voted on, the first map included." | The next map's vote is held only when the party votes to leave (`RogueWavesMode/Code/20_Vote.cs:356-381`). A won map that ends any other way (the party falls in a bonus Tide, or the host quits after Tide 15) has no vote. Back on the boat no choice is set, so the leader steers to one of the linked maps alone with the stock arrows (`05_Boat.cs:13-15`, `:167-168`; `06_Campaign.cs:214-234` keeps the win). Read from the code; no self-test walks this path. | Code (Dimitri's line). The boat could open the same vote when no choice is set. |
| 2 | 04:61 (and 12:19) | "On the boat: between maps, players can't swap their Cardinals or Trinkets." | The bought ones can't be swapped: they are granted again at every map (`17_Cardinals.cs:288-296`). But the mode leaves the boat's stock Cardinal picker alone, so each player can still pick lobby Cardinals and change them before every map (`docs/reports/SCOPE_VS_LOOP.md` #6 kept "Lobby Cardinals stay allowed"). That picker is not filtered either: it also offers the 15 stock Cardinals the shop leaves out because they break the run, Moon Core, Looped Mirror and Bottom Rock among them (`17_Cardinals.cs:38-42`). Read from the code, not tried in game. | Dimitri. Lock the lobby picks once a campaign starts (or filter them as the shop does), or write "lobby Cardinals stay free between maps" into 04 and 12. I lean Code. |
| 3 | 04:24 | (default) "each bonus tide +10% foes on the last" | The +10% applies only once the Tide table runs out, from Tide 30: each replay of Tide 29's row is 10% bigger than the last (`21_Difficulty.cs:163-189`). Bonus Tides 16-29 are the game's own Tides 16-29 with the map's scaling. | Scope: "from Tide 30, each bonus Tide +10% foes on the last; before that, the game's own Tides 16-29". |
| 4 | 04:20 | (default) "each campaign's seed picks 2 of the 4 new modifiers and 2 of the stock elites, and one more joins as the maps go on" | As written, with two things the line doesn't say. When a map can't load one of the campaign's stock elites, the next elite or pack modifier outside the four stands in, so a map never plays easier than its number (`21_Difficulty.cs:140-159`, a fix seen on the PC on 2026-11-04). The first modifier joins at map 3, then one every second map, on one foe in five of each row (`:84`, `:144`). | Scope: add the stand-in and the schedule. |
| 5 | 04:70, 04:75, 04:82, 04:89 (and 15:245) | (default) Each weather's in-game line, which the template calls "the line announced when the map starts", e.g. "Rising Water: the water climbs each tide. More Pure Aura" | That line shows in the boat's Setup panel when the map is picked (`05_Boat.cs:109-114`; the text in `07_Weather.cs:29-38` matches 04 word for word). When the map starts, the announcement is the short "RISING WATER ROLLS IN" (`07_Weather.cs:232`), and Quickening Current adds "THE CURRENT QUICKENS" at Tide 5 and "THE CURRENT RACES" at Tide 10 (`:251`, `:257`). | Dimitri (15:245 is his note). I lean Scope: the line is the boat's; say so in 04:70 and the template. The other way is to show the whole line when the map starts. |

**Designed, not built**

| Line(s) | What | Marked by | Plays today |
| --- | --- | --- | --- |
| 04:16 (and 05:13) | "a new Depth level is unlocked" when a campaign is won | [Depth] Status 04:42 "on hold" | A won campaign ends with "CAMPAIGN WON" and leaves the save (`06_Campaign.cs:293-304`); nothing unlocks |
| 04:35-42 | [Depth]: 5 levels | Status 04:42 | Not built |

**Notes**
- Matches worth stating: 10 rows of 3 or 4 maps, links 1 to 3, rows 5 and 10 harder and calm, no map twice, every base
  map but the custom slot and the test plans (`03_Chart.cs`, `02_Mode.cs:79`); weather odds 1 in 3 then 1 in 2, never on
  a map that has it, +1 Pure Aura every Tide (bonus Tides included), x1.25 from Tide 5 and x1.5 from Tide 10, the nest 3
  to 7 tiles from an enemy core (`03_Chart.cs:255-266`, `14_Run.cs:214`, `07_Weather.cs`); 3 retries, a lost map replayed
  from the last save and only it open, the host quitting a running map loses it, Tide 15 cleared wins it
  (`06_Campaign.cs`); 60 s breaks (`02_Mode.cs:10`); the leave vote by majority, only in a break with the field clear
  (`20_Vote.cs:119-126`, `:356-381`); vessels locked in (`05_Boat.cs:171-193`); score hidden (`13_Card.cs:72-85`);
  islands from the base game's world maps (`04_World.cs:261-268`, `:364-377`).
- Built, not in scope: the opening break keeps the game's own 45 s (`02_Mode.cs:5-6`); the break clock is held while the
  party picks Directions (30 s) or votes (25 s for the map, 20 s to leave) (`20_Vote.cs:14`, `:187-191`); a tied or
  unanswered map vote goes to the first card (`20_Vote.cs:300-307`); a friend new to the campaign picks a vessel freely,
  then it locks.

### 05_Gameplay_Rundown.txt

Checked 9 lines; no issue of its own (05:7 is 04 row 1, 05:13's Depth unlock is 04's). The Direction pick comes on
leaving a map, after the leave vote, and for map 1 in the staging stop, so it comes before each new map as 05:6 and
05:11 say (`20_Vote.cs:1-8`). A replayed map gets no new pick.

### 06_New_Mechanics.txt

Checked 30 lines; no issue.

**Designed, not built**

| Line(s) | What | Marked by | Plays today |
| --- | --- | --- | --- |
| 06:16 | (default) Duos | 02 Log 2026-11-04 ("Nothing new is built") | No Duo exists; the offer roll knows only ranks, clashes and the minions rule (`16_Directions.cs:117-165`) |

**Notes**
- Matches worth stating: 3 offered; ranks II and III offered again and sold for Pure Aura 20 and 30, only the next rank
  sold, a held clash never offered (`16_Directions.cs:21-22`, `:117-200`); Pure Aura 3 a Tide and 2 a bonus Tide, per
  player, shown on the vessel and on a pearl line under the Tide counter (`14_Run.cs:27`, `:212-219`, `:398-456`); Fund
  Transfer base aura only, 50 x the coming Tide per break, reset each break (`18_Shop.cs:386-427`, `20_Vote.cs:68-69`);
  VENDCINT lands 2 to 7 tiles from the Basin at every break, the opening one included, and leaves 1.2 s before the break
  ends (`18_Shop.cs:23-118`, `20_Vote.cs:71-74`, `:114-117`).
- Built, not in scope: the shelf shows 3 Cardinals and Trinkets a break, rolled per map and Tide (`17_Cardinals.cs:28`,
  `:174-187`); a Direction pick left unanswered for 30 s takes the first card (`20_Vote.cs:239`); Fund Transfer offers
  10, 25, half the limit and the limit (`18_Shop.cs:296`).

### 07_Vessel_Yossele.txt

Checked 244 lines; 4 issues (1 on `~defaults~`). Units: the code reads `Attackspeed` as attacks per second
(`Yossele/Code/08_Cannon.cs:209`, `11_Pachira.cs:49`), so the sheet's 0.4, 0.25 and 0.5 were compared as rates. Percent
bonuses on one stat are `Multi` mods that add, as the engine does.

| # | Line | Scope says | Code does | Who should yield |
| --- | --- | --- | --- | --- |
| 1 | 07:36 | (default) FERTILIZE's range is 6 tiles. | Only the hotkey's own pick is limited to 6 tiles (`Yossele/Code/06_HeroBrain.cs:209-223`, `02_Data.cs:98`). A building you click is fertilized at any distance: `Fertilize()` checks owner, Growth and fertilizer, not range (`06_HeroBrain.cs:198-208`, `14_Commands.cs:80-93`). | Dimitri. I lean Scope: say the 6 tiles is the hotkey's pick (the note at 07:35 points that way). The other way is a range check on clicks. |
| 2 | 07:201 | Natural Remedy heals 0.5 health a second per [Growth] stage, so 0 at Growth 0. | It heals 0.5 x max(1, Growth), so a Stonehenge that hasn't grown yet still heals 0.5 a second (`10_Henge.cs:241-243`). `Yossele/DESIGN.md:226` records this as Claude's default. | Dimitri. I lean keeping it and adding "counts as stage 1 before it has grown", the note Sustaining Practices already has (07:251). |
| 3 | 07:223 (and 07:187, 07:282) | Stonehenge -10% damage taken per Growth stage (8 stages = 80%); Monument +20% damage reduction; Fortifying Roots a 30% aura. No cap is written. | A building's own reduction and the strongest aura add up and are capped at 90% (`12_Hooks.cs:49`, `:81`; `02_Data.cs:189`; `10_Henge.cs:84-88`). A fully grown Stonehenge gets only +10% from Monument, and from Growth 6 it gets at most 10-30% from Fortifying Roots. `DESIGN.md:192-193` records the cap; the scope doesn't. | Dimitri. I lean keeping the cap (80% + 20% would make the stones unkillable) and writing it into 07. |
| 4 | 07:247 | Focused Roots: "1.8 times the mining speed". | It adds +0.8 to the same additive Multi as Growth (+10% a stage), Forest Project and Spontaneous Mulch (`11_Pachira.cs:58-70`, `02_Data.cs:297`). That is x1.8 at Growth 0, but a fully grown Sapling goes from 1.5 to 2.3, which is x1.53. | Dimitri. I lean Scope: "+80% mining speed", the game's own way of adding percentages. The other way is a separate x1.8. |

**Designed, not built**

| Line(s) | What | Marked by | Plays today |
| --- | --- | --- | --- |
| 07:275-278 | The Sapling's level 3 {Wood Wide Web}: trade 2 of your most-held colour for 1 of your least-held, Yellow 250 | 02:44 ("None is built yet"); `DESIGN.md:102-103`; `docs/reports/UPGRADE_GAPS_2026-11-04.md` | Level 3 offers only Grafted, Forest Project and Spontaneous Mulch (`02_Data.cs:299-301`) |
| 07:307 | Claude's five Directions for Yossele | 02:45-46 | See 11 |

**Notes**
- Built, not in scope:
  - Garden Weeds' seed-growth stacks each last 15 s (`02_Data.cs:264`, `09_Dandelion.cs:333`); the "debuff" on the foe
    is an invisible 3 s window with no look of its own (`09_Dandelion.cs:331`).
  - A 0.3-tile hit margin on the Mushroom Cannon's blasts and Viral bursts, the Pachira's reach and Spontaneous Mulch,
    and Connected and Fortifying Roots (`08_Cannon.cs:249-251`, `11_Pachira.cs:87`, `:98-99`, `12_Hooks.cs:182`, `:195`).
    Explosive Finish has none (`08_Cannon.cs:442`).
  - A seed counts as killing a foe that falls within 0.6 s of a seed hit (`02_Data.cs:177`, `09_Dandelion.cs:324`).
  - Rubbery Shell's bounce deals the full damage of every seed sent to the first target (`09_Dandelion.cs:184-185`, `:219`).
  - A newly built Tree has 0 seeds until the next Tide begins (`09_Dandelion.cs:23`, `:103-110`).
  - Verdant Spread's "within 2 tiles" counts walking steps with no diagonals: a diamond (`10_Henge.cs:143-166`).
  - The aura fruit's value reads the Sapling's live Attackspeed, so Growth and the mining upgrades raise what a fruit
    pays (`11_Pachira.cs:172`). A fully grown Sapling with no upgrades pays 6 of each colour (`DESIGN.md:86-87`).
  - A placeholder lore line, "Yossele's story comes later..." (`02_Data.cs:75`); 07:16 leaves lore for later.
- Matches that could have been doubted: Deathcap holds a foe above 8 stacks where it is and gives it +25% damage
  (`08_Cannon.cs:267`, `:271`, `:329-331`); Growth +1 a Tide, or +5 on a map of 4 Tides or fewer (`05_Server.cs:164-168`,
  `07_Building.cs:355-365`); the Tree's range 7 tiles (`02_Data.cs:258`); D-MDS one seed every 4 s and 5 seeds for a
  shooter of 600 base health or more (`02_Data.cs:179`, `:265`); the Bonsai exactly 40% of the Tree and sold for 0
  (`02_Data.cs:315`, `09_Dandelion.cs:439-444`); the fertilizer cap of 10 holds with Compost Bin, kept through death and
  reset each map (`05_Server.cs:129-147`); every vessel stat, in-game line and upgrade cost (`02_Data.cs:84-88`,
  `:239-305`).

### 08_Vessel_Spool.txt

Checked 191 lines; 1 issue (0 on `~defaults~`).

| # | Line | Scope says | Code does | Who should yield |
| --- | --- | --- | --- | --- |
| 1 | 08:46 | "Smaller enemies drop 1 {Thread}, while larger elite enemies drop up to 5" | A foe counts as elite only if it carries an upgrade whose name starts with `Enemy_`: the game's six stock elites (`Spool/Code/01_Util.cs:338-346`; the +2 at `05_Server.cs:282-285`). The pack's own modifiers are named `RW_Modifier_*` (`RogueWavesFoes/Code/00_RogueWavesFoesMod.cs:80`), so a Cursed, Berserk, Aging or Dazzling foe drops 1 or 3 Thread, never 5. | Dimitri; the sheet doesn't say whether the pack's modifiers are elites. I lean Code: count `RW_Modifier_` as elite too. |

**Designed, not built**

| Line(s) | What | Marked by | Plays today |
| --- | --- | --- | --- |
| 08:66-97 | 7 Yarn Stash upgrades (Roomy Pockets, Quick Patch, Darning, Elbow Patches, Remnant Bin, Mending Circle, Quilting Bee) | Status 08:17; 02:44; `UPGRADE_GAPS_2026-11-04.md` | The Stash has no upgrades (`Spool/Code/02_Data.cs:215`): Thread cap 50, Spool carries 20 and picks up within 1 tile, PATCH 20 Thread with a 5 s cooldown on one target, ground Thread lost after 15 s |
| 08:156 | UNTHREAD on the Cookie Tin | Status 08:17 | Crafted are sold one at a time with the stock Sell, for half their Thread (`08_Tin.cs:409-421`) |
| 08:183-200 | Tin levels 2-4: Tacking Stitch, Cross Stitch, Patchwork Army | Status 08:17 | Level 2 Discounted Material and New Design, level 3 One Last Surprise, no level 4 (`02_Data.cs:222-234`) |
| 08:232-253 | Thread Head levels 2-4 (Big Bobbin, Needle Point, High Thread Count, Stitch in Time, Head of the Class) | Status 08:17 | Only Threaded Delivery and Patchwork Job (`02_Data.cs:243-249`) |
| 08:279, 08:294-302 | Pin Barrage leading to Iron Thimble; Voodoo Doll | Status 08:17 | Pin Barrage ends its branch (`02_Data.cs:263`) |
| 08:325-346 | Loom levels 2-4 (Dyed in the Wool, Bobbin Winder, Seam Squad, Snare Line, Golden Thread) | Status 08:17 | Only Direct Line and Seam Allowance (`02_Data.cs:279-285`) |
| 08:350 | Claude's five Spool Directions | 02:45-46 | See 11 |

**Notes**
- Matches: every vessel stat with no regen; Hand Stitched word for word; PATCH 20 Thread, 20% capped at 500, range 4,
  cooldown 5 s, keys 6/7; Thread start 10, cap 50 +25 a level, life 15 s; sell refunds half rounded down; every Crafted's
  stats and leash; all 15 built upgrades on cost, numbers and in-game line (`Spool/Code/02_Data.cs`, `06_HeroBrain.cs:277-332`,
  `08_Tin.cs:413-433`).
- How the code reads the drop rule: 1 Thread below base health 40, 3 at 40 or more even when not elite; an elite adds 2,
  up to 5 (`02_Data.cs:86-87`, `05_Server.cs:282-285`). `DESIGN.md:200` lists this as a default.
- Built, not in scope: the Thread Head's line "Guards a spot, collects Thread, carries it home"; a placeholder lore line;
  Spool picks up a Loom's Thread within 1.5 tiles; a sold Stash keeps its Thread and a destroyed one spills it; a fallen
  Thread Head drops what it carried; SPOT and RETURN buttons.
- The balance proposals P1-P7 are not applied, which matches "Leave Spool as is".

### 09_Vessel_Argo.txt

Checked 201 lines; 6 issues covering 13 lines (all 6 on `~defaults~`). Attack speed is attacks per second
(`Argo/Code/07_TowerBrain.cs:171-172`); Multi mods add from 1, so Ironclad's `Health_Multi=1.5` is x2.5.

| # | Line | Scope says | Code does | Who should yield |
| --- | --- | --- | --- | --- |
| 1 | 09:101 | (default) Bosses are "never moved by a reel or pin". | The reel refuses a foe of 600 max health or more (`Argo/Code/01_Util.cs:468-471`, `02_Data.cs:205`), but `ArgoUtil.Pin` and `Root` have no such check (`01_Util.cs:383-414`). Anchor Line (`07_TowerBrain.cs:470-474`) and Mantle Tap's quake (`:863-864`) pin a boss, and Kelp Bed and Trench hold one (`11_Water.cs:189`, `:218`). | Dimitri. I lean Code: a boss check in `Pin` and `Root`, since the review's Kelp Bed and Trench pins would otherwise hold every boss in the water. Or reword to "never reeled". |
| 2 | 09:159 (and 09:167) | (default) Deep Water slows 30%, +5% a level, Undercurrent +10%. | The water re-slows every foe every 0.25 s for 0.5 s (`11_Water.cs:184`, `02_Data.cs:168`), and each slow adds its own Speed mod (`01_Util.cs:532-546`), so a foe carries two: a 30% pool slows 60%, and Undercurrent plus levels reaches the 10% floor. The known drift (`docs/reports/ARGO_REVIEW_2026-11-04.md` §2). | Dimitri. I lean Code: one slow per source, as 09:36 and the review ask. |
| 3 | 09:211 (and 09:212, 09:223) | (default) The Tremor slows 20%, +4% a Fathom, Seismic +10%. | It re-slows every 0.5 s for 0.75 s (`07_TowerBrain.cs:696-709`), so one or two mods: 20-40% at Fathom 1, 36-72% at Fathom 5. Each mod is capped at 60% (`02_Data.cs:155`), which the scope doesn't mention. | Dimitri. I lean Code (the same fix). |
| 4 | 09:69, 09:112 | (default) Barnacle Shot "slows by 25% for 1.5 seconds"; Tether "slows every foe hit by 30% for 2 seconds". | Every pellet that hits adds its own slow (`07_TowerBrain.cs:400-402`): a foe hit by 2 or 3 pellets of one volley is slowed 50-75%. With Twin Line the second harpoon adds a second 30% Tether slow (`:338-345`, `:409-413`): 60%. | Dimitri. I lean Code: the review names Tether and Barnacle in the same fix. |
| 5 | 09:181 | (default) Salt Sting: "pinned foes take double brine (0.5 a second without Brine)". | Without Brine a pinned foe takes 0.5 x (1 + 1) = 1.0 a second (`11_Water.cs:195-200`, `02_Data.cs:172`), the same as with Brine. The in-game EFFECTS line repeats "0.5 a second" (`02_Data.cs:326`). The known drift (`docs/reports/MISTAKE_CHECK_2026-10-28.md:81-85`). | Dimitri. I lean keeping the code and writing "1 a second" in the scope and the in-game text, as that report's default says. |
| 6 | 09:43, 59, 176, 186, 196, 238, 248 | (default) The review rewrote these in-game lines (Scatter, Double Barrel, Kelp Bed, Abyss, Trench, Gusher, Mantle Tap), but none carries the "Review 2026-11-04" note that 09:10 says every changed line has. | The code still shows the earlier texts, each matching today's mechanics (`02_Data.cs:255`, `:264`, `:323`, `:327`, `:331`, `:354`, `:358`). | Scope: add the review note to each so the sheet reads as unbuilt; the new texts ship with their review changes. |

**Designed, not built**

| Line(s) | What | Marked by | Plays today |
| --- | --- | --- | --- |
| 09:36 | One slow per source, renewed, never stacked with itself | Review note; 02 Log 2026-11-04 | Re-ticks stack (rows 2-4) |
| 09:37 | Kelp Bed and Trench pin | Review note | Both hold through `Root`, which doesn't count as pinned (`11_Water.cs:189`, `:218`) |
| 09:50 | Debuff bonus +50% slowed, +100% pinned | Review note | +50% for either (`02_Data.cs:258`) |
| 09:56 | Wide Bore cone +10° | Review note | +25° (`02_Data.cs:262-263`) |
| 09:60 | Double Barrel stores a volley every 2 s, up to 3 | Review note | One stored volley after a reload with no target (`02_Data.cs:264-265`) |
| 09:177 | Kelp Bed pins 0.5 s (1 s in water) | Review note | A 0.75 s hold, caught again only after 3 s out of the water (`02_Data.cs:323-324`) |
| 09:187 | Abyss: slow +10% | Review note | Flood +1 only (`02_Data.cs:327`) |
| 09:197 | Trench: two pools a Tide, a pin every 3 s | Review note | One pool a Tide, a random foe in the water held 1 s every 6 s (`02_Data.cs:331-332`) |
| 09:225, 235, 245, 261 | New costs: Seismic Yellow 35, Bedrock Yellow 80 + Red 30, Fault Line Yellow 300, Miner's Base Blue 20 + Yellow 15 | Review notes | Blue 25 + Yellow 10, Blue 80 + Red 30, Blue 300, Blue 35 (`02_Data.cs:348-365`) |
| 09:239 | Gusher: 4 strikes a Fathom from Fathom 3 | Review note | 12 strikes a Tide from Fathom 3 (`02_Data.cs:354`) |
| 09:249 | Mantle Tap: strikes x1.5, a quake every 20 s pins 2 s | Review note | Ore per strike x2, a 2 s quake once a Tide (`02_Data.cs:358`, `07_TowerBrain.cs:712-717`) |
| 09:127 | The Reel's journal adds "Place it beside or up the lane..." | Review note | No such hint (`02_Data.cs:286`, `:297-298`) |
| 09:312 | Chum Bucket and Pressure Gauge | 02 Log 2026-11-04 | See 11 |

**Notes**
- Doubted and matching: Ironclad x2.5; Overhaul 2.5 health a second; Full Crew +40%; Whaler x2 at 200 max health;
  Leviathan every 5th shot, x2.5, range x2, piercing; the cleave at 60% within 1.25 tiles; one Miner's Base; Nauts refused
  on traps; every cost, Leads to, vessel and Naut stat (`Argo/Code/02_Data.cs:76-77`, `:127-133`, `:251-398`).
- Built, not in scope: each Tremor and Deep Water slow capped at 60%; one Naut per tower; picking the Miner's Base calls
  a Naut home; a Naut gives up after 6 s with no progress; AIM accepts 0.5 tile past Range; Anchor Line's "stun" is attack
  speed -95% and "beside a building" means any tower within 1.25 tiles of any team (`01_Util.cs:402-423`); the "Drill Rig
  has no ore within 2 tiles" warning.
- The Fathom toast says "mines N x as fast as at first", 5.6x at Fathom 5 (`10_Client.cs:593-599`). Past Fathom 3 a strike
  breaks at most one share of ore (the review's note on 09:212), so the toast overstates mining there. Worth fixing with
  Mantle Tap.
- "Boss" in the code means 600 max health or more (`02_Data.cs:205`); the scope gives no definition.

### 10_Vessel_Uloe.txt

Checked 108 lines; 1 issue (1 on `~defaults~`). Dazzling's disagreements with [Light] (10:52) are counted under 13
(rows 2 and 3), since Uloe's own code matches 10:52.

| # | Line | Scope says | Code does | Who should yield |
| --- | --- | --- | --- | --- |
| 1 | 10:51 | (default) A Mirror is level 1 from Light 1, level 2 from Light 3, level 3 from Light 6. | Level 1 starts at any Light above 0 (`Uloe/Code/10_Mirror.cs:106`). Light below 1 happens today: Bounce Card passes on half (`:110`), so a plain Mirror next to one with Light 1 gets 0.5 and still Blinds at level 1. | Dimitri. I lean Scope: "level 1 from any Light". |

**Designed, not built**

| Line(s) | What | Marked by | Plays today |
| --- | --- | --- | --- |
| 10:71-109 | The Glass's 7 new upgrades (Wide Lens, Polished Lens, Quick Focus, Prism, Sunspot, Focal Point, Death Ray) and Burning Point leading on | Status 10:17; `docs/reports/ULOE_UPGRADES_2026-11-04.md` | Burning Point ends the tree; Light +20% a point; single target; ramp to +120% (`Uloe/Code/02_Data.cs:81`, `:191`) |
| 10:126-159 | The Power Bank's 7 (Extra Cell, Static Charge, Bright Spark, Surge Protector, Chain Lightning, Photon Swarm, Supernova) and Short Circuit leading on | same | Short Circuit ends the tree; Photon Light 1 within 2, leash 3.5 (`02_Data.cs:76`, `:142`, `:203`) |
| 10:35 | "12 with all four" Bank upgrades | follows from the above | A Bank holds one upgrade: Plug In gives Light 4 or 6, plus Brighter Bulb's 3 (`06_HeroBrain.cs:151`) |
| 10:189-208 | The Mirror's 4 (Tempered Glass, Hall of Mirrors, Reflection, Heliostat) | same | Not built; the Mirror never attacks |
| 10:232-258 | The Flood Light's 6 (Halogen Bulb, High Voltage, Colour Filter, Searchlight, Stadium Lights, Lighthouse) | same | Not built; nothing of Uloe's slows |

**Notes**
- Matches: vessel stats, title, difficulty, in-game line and the special rule word for word (`02_Data.cs:47-67`); Plug
  In (node 2 + 0.4 x aura up to 10, Bank 4 + 2 per upgrade, radius 4, cord 5, 0.5 s cooldown, keys 6/7); Light sources,
  the cap of 12 and its four effects; Blind and Illuminate levels and times; all four buildings and the eight built
  upgrades on cost, line and number (`02_Data.cs:74-98`, `:183-241`).
- Built, not in scope: a placeholder lore line; Plug In and AIM hold 0.5 tile past their reach; Long Throw raises AIM
  from 6 to 8 tiles (`07_Building.cs:223`); Photons engage within 2.5 tiles; Plug In takes only the player's own Power
  Banks (10:36 says "a Power Bank"); Light, AIM and Photon visits also reach a teammate's Uloe buildings; plugged-in Light
  replaces Uloe's own Light 1 rather than adding to it (`06_HeroBrain.cs:159-160`).

### 11_Directions.txt

Checked 176 lines of the built sheets; 10 issues (5 on `~defaults~`). Built: 7 (Any), 5 base vessels', 3 Yossele, 2
Spool, 2 Argo, 4 Uloe.

| # | Line | Scope says | Code does | Who should yield |
| --- | --- | --- | --- | --- |
| 1 | 11:39 | (default) Tidal Mend mends 5 / 8 / 12 within 2 / 2.5 / 3 tiles each Tide. | It sets `Effect_Add` 5 / 8 / 12 on the stock `Smackywall_Regen` (`RogueWavesMode/Code/16_Directions.cs:35-36`), which heals `Effect_Add` x the holder's level, read once when it lands (`docs/reports/engine_study_2026-10-28/B_effects_enemies_cores.md:375-384`). A level 3 tower mends 15-36. The ranges match. Not measured in game. | Dimitri. I lean Code: divide by the level, or own the heal. Otherwise write "x the tower's level" into Numbers. |
| 2 | 11:56-61 | (default) Cut Costs, any vessel: "Your buildings cost less", -10 / -20 / -30%. | It scales the vessel's own `Buildoption` prices (`16_Directions.cs:236-238`, `:283-291`). Boutman builds through Boutbro, who rewrites his own, so the code leaves him out on purpose (`:281-282`; kit `docs/17` §2.7), yet it is still offered to Boutman (`:143`), where it does nothing. | Dimitri. I lean Code: don't offer it to Boutman, or write Boutbro's own cost stats (Worm Tin, 11:592, is the unbuilt sheet for that). |
| 3 | 11:72-74 | (default) Cheap Refits takes -10 / -20 / -30% off upgrades. Clashes: None. | It sets each tower's `Upgrade` price scale to 0.9 / 0.8 / 0.7 outright, twice a second (`16_Directions.cs:240-243`, `:351-358`). The stock Cardinal Voice in a Bottle, on sale, lowers that same number each Tide (kit `docs/17_BASE_VESSELS.md:161-163`), so for a player holding Cheap Refits, Voice's discount is undone within half a second. | Dimitri. I lean Code: multiply by the scale instead of setting it. |
| 4 | 11:13-14 | Made of Tougher Stuff (Dimitri's): "Your (Minions) take reduced damage". | It reaches only things tagged `Mob` and not `Hero` (`16_Directions.cs:322`). Boutbro is Boutman's minion but carries `Hero` (pitfall #145), so he never gets it; neither do Boutdad and Boutmom (no `Mob` tag). | Code: skip only the player's own vessel (`RWRun.Vessel`, `14_Run.cs:77-98`), not every `Hero`. |
| 5 | 11:519-522 | (default) Full Gallop: "Buster runs faster", +10 / +20 / +30%. | It adds Speed (`16_Directions.cs:53`), and Buster's damage is Speed x Speeddamage (kit `docs/17_BASE_VESSELS.md:346-350`), so rank III also takes his damage from 5 to about 6 (+21%). The speed was measured, 3.5 to 4.55 (`TEST_ROUND_2026-11-04.md`). | Dimitri. I lean Scope: say his hits grow with his speed, as the base game does. |
| 6 | 11:315 | Dandelion Season rank I: the Tree "becomes {Dandelion Sapling}". | The Tree keeps its name and model; the Direction changes its numbers and lifts the limit only (`Yossele/Code/02_Data.cs:255`; `09_Dandelion.cs:347-388`; `Yossele/IMPLEMENTATION_NOTES.md:245`). The numbers match and were measured on 2026-11-04. | Code (Dimitri's line): rename the panel, and a Sapling look when the art exists. |
| 7 | 11:326 | Mother of Mushrooms rank I: the Tree is "replaced by {Primordial Mushboom}, a large mushroom tower". | It is still "Primordial Dandelion Tree" with the Tree's model, and its Seeds counter shows 0 (`09_Dandelion.cs:400-423`). The burst matches: every 8 s, 2 damage per spore stack on every foe in range, +0.5 tiles a stage, rank II +25%, rank III takes half (`02_Data.cs:143-146`). | Code (Dimitri's line): a name, and a mushroom model (art). |
| 8 | 11:306 | Compost Bin rank III: fertilizer "in a much larger radius" around Yossele. | The radius is 7 tiles instead of 4, as 11:307 says (`Yossele/Code/02_Data.cs:109`). But the in-game text says foes felled "anywhere" around Yossele make fertilizer "faster" (`:110`), which reads as no radius at all; rank III needs the same 6 kills as rank II. | Code: "within a much larger radius". |
| 9 | 11:405 | Sewing Machine rank I: "{Loom} is replaced by {Sewing Machine}"; Look "a classic light blue sewing machine". | The Loom keeps its name, panel and model; it mines 50% faster, spins no Thread and mends each Tide (`Spool/Code/07_Building.cs:243-276`). The card says "Looms become Sewing Machines" (`Spool/Code/02_Data.cs:119`), which the panel then contradicts. The numbers match. | Code (Dimitri's line). `Spool/IMPLEMENTATION_NOTES.md:159` records the name as a technical limit, not his choice. |
| 10 | 11:510 | (default) Flash Bulb's Look: "An old camera's flash cube". | The card is a round blue flash bulb with smoke (`Uloe/Tools/Make-DirectionIcons.ps1:17-19`; `Direction_Flashbulb.png`), as `Uloe/DESIGN.md:239` says ("flash bulb, still smoking"). "Cube" came in when the defaults were copied into the scope. | Scope ("flash bulb"). |

**Designed, not built**

| Line(s) | What | Marked by | Plays today |
| --- | --- | --- | --- |
| 11:87-229 | 13 (Any) Boosts: Wound Spring, Fine Sieve, Spare Hands, Galley Kettle, Chain Shot, Salt Barrel, Fog Horn, Clam Shell, Notched Oar, Lamp Oil, Fish Hook, Spare Compass, Magpie Nest | 02:13, :45-46 | Never offered (`16_Directions.cs:28-63`); offers stay at 3 (no Spare Compass); Spring Tide has no clash since Wound Spring doesn't exist |
| 11:230-251 | 2 Duos: Fire Ship, Tide Pool | same | Neither exists; the roll has no "holds both" rule |
| 11:276-297 | Argo: Chum Bucket, Pressure Gauge | same | Drills start at Fathom 1 |
| 11:333-387 | Yossele: Cutting Jar, Sap Bucket, Creeping Ivy, Deep Breath, Fairy Ring | same | Not registered (`Yossele/Code/04_Content.cs:673-695`). Creeping Ivy's rank II slow would need to say whether it adds to the vines' own 15% |
| 11:412-466 | Spool: Double Needle, Button Jar, Pattern Book, Merit Badge, Odd Socks | same | Not registered |
| 11:570-624 | 5 base vessel Boosts: Mason's Trowel, Snow Shovel, Worm Tin, Open Sign, Long Leash | same | Each base vessel gets its one built Direction plus the (Any) ones |

**Notes**
- Matched and measured on the PC (`TEST_ROUND_2026-11-04.md`): Cut Costs 0.9 / 0.8 / 0.7; Cheap Refits 0.7; Barnacle
  Coat 2 and 7; Made of Tougher Stuff 15 / 25 / 35% with rank III's 25% cap per hit; Compound Interest 3 / 5 / 8%, at most
  40 a colour; Skipping Stones +50%, Full Gallop +30%, House Regulars +30%, Long Line +3, Packed Snow +25%; Dandelion
  Season; Efficient Recycler; Deep Current and Heavy Salvo.
- Matched in code, not measured: Spring Tide +4 / +7 / +10 health a Tide and rank III's 10% shrug; Tidal Mend's ranges;
  Mother of Mushrooms' burst; Sewing Machine's numbers; Compost Bin 10, 6, 6 kills; Uloe's four (Extension Cord, Brighter
  Bulb, Spare Batteries, Flash Bulb).
- Every rank line and name of the built sheets matches the in-game text (`16_Directions.cs:30-62`, each vessel's
  `02_Data.cs`), but for "Tide" capitalised and "minions" in lower case.
- Side effects the cards don't mention: Cut Costs also cuts the sell refund (the price scale is also `Sellvalue`, kit
  `docs/17` §2.7); Tidal Mend also mends teammates' things and the vessels in range; Long Line also widens Shao-min's aim
  and Kung Fu Tackle's reach (16:635, :661); the tower Directions and Cheap Refits skip Buster's traps and the Bouthouse
  (no `Tower` tag).
- Made of Tougher Stuff's "Clashes: Any Vessel without (Minion)": every vessel has minions, so none is left out
  (`16_Directions.cs:104-109`).
- Under Mother of Mushrooms the Bonsai still fires seeds (`09_Dandelion.cs:437`); the scope doesn't say.
- Kind (Boost or Shift) is neither shown nor used anywhere; Flash Bulb is a Shift that gives nothing up, which 06:14's
  Power default asks a Shift to do (a scope point).

### 12_Cardinals.txt

Checked 24 lines; 4 issues (0 on `~defaults~`).

| # | Line | Scope says | Code does | Who should yield |
| --- | --- | --- | --- | --- |
| 1 | 12:13 | Cardinals and Trinkets are two kinds, "kept apart in the shop and in the list". | One shelf of 3 is drawn from one mixed pool of 29 stock Cardinals and the one Trinket (`17_Cardinals.cs:28`, `:102-103`, `:167-180`). Only the word "Trinket" or "Cardinal" on each card tells them apart (`18_Shop.cs:217`). A Trinket comes up in about 1 break in 10. | Dimitri. I lean Code: a slot of its own for Trinkets. The label may be all he meant. |
| 2 | 12:14 | A Trinket "shows on their vessel's panel". | Vampire Dentures' upgrade has no script, and `Grant` skips any upgrade without one (`17_Cardinals.cs:96`, `:307`); its effect runs from a count of the held ids (`:362`). Nothing else shows a player's Trinkets: not the panel, the row or the run line. | Code: put the upgrade on the vessel even with no script, as Directions are. |
| 3 | 12:15-16 | A Cardinal "shows in the Cardinal row at the top of the screen"; no limit on how many a campaign holds; the note says past 8 "the mode shows them another way". | A Cardinal joins the row only while it holds fewer than 8, lobby picks included (`17_Cardinals.cs:258-262`). Past that it works but shows nowhere. The file's own header promises a count in the run line (`:5-6`), but the run line shows only the objective (`20_Vote.cs:430-436`). | Code: the count the scope's note and the code's header both promise. |
| 4 | 12:17 (with 12:15) | Cardinals and Trinkets stack with no limit; every base game Cardinal the shop sells is a Cardinal. | A stock Cardinal can be bought only once: "Already held" (`17_Cardinals.cs:199-200`), since the same stock upgrade twice does nothing (`:11`). The shelf still draws a held one (`:167-180`), so the player is offered a card they can't buy. | Dimitri. I lean Scope ("a base game Cardinal is bought once") plus Code keeping held ones off the shelf. |

**Designed, not built**

| Line(s) | What | Marked by | Plays today |
| --- | --- | --- | --- |
| 12:40-171 | 11 Trinkets: Brass Spyglass, Gutting Knife, Oil Can, Tar Bucket, Rubber Duck, Spare Plank, Punch Card, Pearl Oyster, Bottled Storm, Oilskin Coat, Gull Feather | 02:13, :45-46; `RogueWavesMode/IMPLEMENTATION_NOTES.md:41` | Vampire Dentures is the only Trinket; the pair rule (12:21) has nothing to act on |
| 12:174-221 | 4 Cardinals: Cork Lifebuoy, Signal Flare, Cargo Crate, Tin Whistle | same | The shelf stays at 3 (no Cargo Crate) |

**Notes**
- 12:7 checked name by name against the shop's left-out list (`17_Cardinals.cs:39-43`): every Cardinal the line means is
  off the shelf. "Pig" and "Money Bag" leave out both the game's Money Bag and its Pig Named Bank, which the first ask
  (`SCOPE_VS_LOOP.md` #7) didn't name. Four ids on the list name nothing in the game (harmless). 12:7 is not written in
  the game's spelling (Moon Core, Looped Mirror, Vintage Watch, Sand Filled Box, Frozen Computer): a wording fix.
- Vampire Dentures matches: its line word for word, 20% a stack with no cap (measured: 1 for 5, 0.8 for 2 with two
  stacks), cost 15, stacks with no limit, Red, saved per player, re-granted every map (`17_Cardinals.cs:16`, `:35`, `:373`;
  `14_Run.cs:147`, `:161`).
- Built, not in scope: a stock Cardinal costs Pure Aura 15 (`17_Cardinals.cs:27`); locked stock Cardinals (Kippy, 1st
  Grade Textbook, Fish Bait, Bendigo) are on sale too, which fits "all but".
- Latent: a future scripted Trinket that stacks gets only 6 numbered copies, and buying past them takes Pure Aura for
  nothing (`17_Cardinals.cs:25`, `:93-99`). Vampire Dentures is safe because it counts the held ids.
- The seven removed Cardinals are gone from the code but for comments. Model Lighthouse (12:24) is used nowhere.

### 13_Enemy_Modifiers.txt

Checked 22 lines; 4 issues (3 on `~defaults~`).

| # | Line | Scope says | Code does | Who should yield |
| --- | --- | --- | --- | --- |
| 1 | 13:10 | Cursed "Reduces the attack of all nearby towers and units". | It also weakens vessels: its targets are everything tagged Tower, Mob or Hero on the players' teams (`RogueWavesFoes/Code/00_RogueWavesFoesMod.cs:298-309`, `:322-334`). Dazzling's line names vessels; Cursed's doesn't. | Code (Dimitri's line), unless he meant vessels too; then 13:10 adds them. |
| 2 | 13:40-41 (with 10:52) | (default) Dazzling Blinds and Illuminates "-> 10 [Light]", whose levels are 15 / 30 / 45%; its own Numbers line says 20%. | The code follows 13:41: Blind -20% damage, Illuminate +20% damage taken (`00_RogueWavesFoesMod.cs:84`, `:142-144`). 20% is none of [Light]'s levels, so 13 disagrees with the definition it points to. | Dimitri: pick one. I lean "Blind and Illuminate at level 1 (15%)". |
| 3 | 13:41 (with 10:52) | (default) "for 3 seconds" | Blind is the stock timed boost and lingers 3 s after a target leaves the aura. Illuminate is taken off at the next 1 s check after it leaves, so it lingers 0 s (`00_RogueWavesFoesMod.cs:336-361`). | Code: keep Illuminate 3 s after leaving, as Blind does. |
| 4 | 13:9, 13:19, 13:29, 13:39 | (default) In-game line: `~~`, an empty slot. | Each modifier already shows a line: "A curse on this foe weakens the attack of everything nearby", "Stronger and faster the more it is hurt", "Slow, but its health and damage grow the longer it lives", "Blinds and illuminates everything nearby" (`00_RogueWavesFoesMod.cs:137-140`). | Scope: fill the four slots with these as defaults. |

**Notes**
- Matches: Cursed -25% damage within 3 tiles, only while in range; Berserk up to +100% damage and +50% speed with health
  lost; Aging -30% speed, health and damage rising to +100% over 50 s; the rings in indigo, hot pink, barnacle blue and
  bright yellow (`00_RogueWavesFoesMod.cs:81-84`, `:283-296`, `:373-381`).
- Not seen in game: Dazzling's Illuminate is the stock Merchut mark, which also puts a "Targeted" look on its target.
  Uloe took that look off by hand while it carried this route (`Uloe/Code/05_Server.cs:395-396` at `5d9b09e`; the route
  was removed as unused in #80); the foes part doesn't, so a tower may keep it after leaving the aura. Uloe dropped this
  stock route after its first PC run because a marked target that dies logs two errors (`Uloe/Code/02_Data.cs:101` at
  `5d9b09e`).

### 14_Optional_Objectives.txt

Checked 59 lines; 4 issues covering 8 lines (3 on `~defaults~`).

| # | Line | Scope says | Code does | Who should yield |
| --- | --- | --- | --- | --- |
| 1 | 14:22 | Mind the Meteor's Look: "A node with a white crystal". | The node is a copy of a stock ore node (Red if loaded, else Blue, else Yellow) with that node's own look; only its name and hover text say "Meteor" and "a node with a white crystal" (`RogueWavesMode/Code/19_Objectives.cs:39-76`). It is not listed as temporary art in `docs/TEMP_ART.md`. | Code (Dimitri's line): a white crystal node, or list it as temporary art until there is one. |
| 2 | 14:15 | (default) "12 hits + 3 per map" | The node gets `Healthresource` 12 + 3 x map and, by the code's own note, takes one hit more than that (`19_Objectives.cs:242-243`, pitfall #106): 16 hits on map 1 where the scope gives 15. The distance, 8 + 1.5 x map tiles, matches (`:216`). | Dimitri. I lean Code (`Healthresource` 11 + 3 x map). Not measured. |
| 3 | 14:50-51 | (default) Mind the Vessels: "No vessel drowns this Tide", "no vessel falls". | It fails when anything a player owns that is tagged Hero dies (`19_Objectives.cs:203-204`). Boutbro is tagged Hero (kit `docs/17_BASE_VESSELS.md:125`), so a Boutbro falling fails it; so would a Blurry Photo clone from a lobby pick. | Code: count only each player's own vessel, as `RWRun.Vessel` does (`14_Run.cs:77-98`). |
| 4 | 14:34, 14:46, 14:58, 14:70 | (default) Each objective's Look: a mop and bucket, a cracked stopwatch, a life ring, a burning nest. | Not drawn. The board shows one icon for every objective (`18_Shop.cs:320`, `19_Objectives.cs:193`), and `docs/TEMP_ART.md` lists none of these. | Dimitri: draw them in the next art pass, or mark the Looks as not drawn yet. |

**Notes**
- Matches: one objective posted at VENDCINT every break for the next Tide, taken by any player for the team, every player
  paid in full; pays 3, 2, 3, 2 and 4; each in-game line word for word; Keep the Basin Clean fails on any damage to the
  Basin; Beat the Clock needs more than 25% of the clock left; Raze the Nest's nest 3 to 7 tiles from an enemy core and
  never posted on a Nesting Ground map or where no nest fits (`19_Objectives.cs:14-21`, `:80-92`, `:166-194`;
  `07_Weather.cs:329-355`).

### 15_Templates.txt

Checked the 9 template lines that state a rule the code must follow; 1 issue (15:245's weather line is 04 row 5).

| # | Line | Scope says | Code does | Who should yield |
| --- | --- | --- | --- | --- |
| 1 | 15:178 | A Cardinal's or Trinket's Colour is "its lobby slot and the colour of its card in the shop". | The lobby slot follows it (Vampire Dentures' entry is Red, `17_Cardinals.cs:105-119`), but a shop card is teal when the buyer can pay and dark blue when they can't, whatever its colour (`18_Shop.cs:217`). | Dimitri (his note). I lean Scope ("its lobby slot"), since the card's colour already says whether it is affordable. |

**Notes**
- Matches: every new vessel's ability is on Buster's Dash keys 6 and 7 (15:51; FERTILIZE `Yossele/Code/04_Content.cs:405`,
  PATCH `Spool/Code/04_Content.cs:418`, PLUG IN `Uloe/Code/04_Content.cs:439`); in-game lines carry no numbers and the
  journal shows them (15:13). The examples (Deep Roots, Cracked Buoy, Net the Shoal) are marked "Example only".

### 16_Base_Vessels.txt

Checked 1,195 lines; 1 issue. Every base vessel's stats, buildings, minions and guns match the game's data, and a script
matched all 195 upgrades on level, in-game text (Tune Up's double space included), cost and Leads to. Where an upgrade
also exists on a unit at another price, 16 uses the building's, which is what the player pays.

| # | Line | Scope says | Code does | Who should yield |
| --- | --- | --- | --- | --- |
| 1 | 16:8-18 | The side-by-side table lists the five base vessels plus Argo, Yossele and Spool; 00:21 says 16 holds "every vessel's stats side by side". Uloe has no row. | Uloe is built and offered: Advanced, ranged, Health 30, Speed 2.25, Damage 2, Range 3, attack speed 0.5, build range 2.5, sell rate 0.5 (`Uloe/Code/02_Data.cs:49`, `:65-68`), the same as 10:9 and 10:20-29. | Scope: add `Uloe ~Advanced~ ~ranged~ ~30~ ~2.25~ ~2~ ~3~ ~0.5~ ~2.5~ ~0.5~` and name Uloe in 16:9. |

**Notes**
- The pack rows (16:16-18) match each vessel's sheet and its built hero; Argo's `~melee~` shortens the sheet's
  `~melee, with reach~`.
- Two accent colours looked wrong and are right: Boutman's #FFC34C and Fie's #FFA76D are the entities' own colours, not
  the hero-select ones, as the scope's notes say.
- Doubted and matching: Rejuvenate's "(200)" and its previous-tier bug, Hidden Surprise doing nothing without Spikes, the
  Bouncing Bombs' ranges 5/6/9, Hyper Pipes' 1800, Boutmom's arrival and departure, Buppy's divide-by-zero, Side of
  Sushi's dead crit, Fishbender's 1.5 cap.
- The pack code that touches base vessels (the base vessels' Directions, `RWRun.Vessel`'s Boutbro guard, the F-key fix)
  assumes no number that 16 states differently.
- 44 lines rest only on the decompiled game (§5).

## 4. Stale lines, not player-visible

| Where | Says | Now |
| --- | --- | --- |
| 03:7 | The F-key fix "runs with Argo and Yossele" | Every vessel part carries it; the first to boot installs it (`Shared/ModKitFocus.cs:6`) |
| 07:17 Status | Self-test 160 of 160 | 173 of 173 on 2026-11-04 (02:9) |
| 08:17 Status, 08:5 note | 120 of 120; "nothing of Spool is built yet" | 133/0 on 2026-11-04 (`Spool/DESIGN.md:8`); built |
| 11:253, 11:389, 11:468 | `! NEEDS ARGO'S / SPOOL'S / ULOE'S DIRECTIONS !` | Their defaults are built; the NEEDS lines wait on his own picks only |
| 11:318, 11:329 | "02_Data.cs Mushboomkits" | The numbers are the constants after it (`Yossele/Code/02_Data.cs:143-146`), two of them not marked default |
| `Yossele/DESIGN.md:215`, `Spool/DESIGN.md:158-160` | Their Directions "Not built" | Built (`Yossele/IMPLEMENTATION_NOTES.md:231-246`, `Spool/IMPLEMENTATION_NOTES.md:149-161`) |
| `Argo/Code/02_Data.cs:177` (comment) | Scope 11 has no Argo Directions | It has Deep Current and Heavy Salvo |
| `RogueWavesMode/Code/17_Cardinals.cs:5-6` (comment) | "the run line's count past that" | No count is written (12 row 3) |

## 5. What this audit could not settle

- **Looks and sprites.** Every Look line, sprite and animation is temporary art or not drawn; only the wiring was
  checked. By eye on the PC.
- **44 lines of scope 16** rest only on the game's decompiled code, which this container doesn't have. The five most
  worth a re-read: Cookin' Eggs "up to 10 foes" (16:401; the pulse has Pierce 30 and the Heater's count of 10 is never
  read), Iron Fish "of base" (16:648; its keys are Multi), Excavation Facility "damage +1" (16:243), Long Ranged
  "collect range" (16:1386; the param is `Range_Add`), Rapid Response "+200%" (16:987; the data says `Amount_Base=2`).
  The rest: Cannons' rate (16:125), Overtime (16:216), Marketplace (16:234), Engineering Firm (16:247), Sno at start
  and after death (16:303-308), Sno Boulder from the root (16:340), Snopowder (16:347), Hypercool (16:356), Permasno
  (16:364), Snoble Alert's rate (16:369), Cold Sweats (16:423), Tip of the Iceberg (16:473), Slush, Sno Bank and Soft
  Sno (16:499-512), Black Sno (16:517), Fish Follow (16:603-604), the Boutman heals (16:626, 631), 1 Inch Bout (16:644),
  Ip Man-of-War (16:657), Kung Fu Tackle (16:661), Bout Barge's 0.1 (16:744), Bout Battleship (16:757), Buppy's target
  (16:868), Boutmom's bonus (16:884), Living Wall (16:951), Stored Charge (16:1223), Blast Extra Zed (16:1245), Big Dog
  (16:1272), Diesel Dog (16:1285), Speed Bump and Wrong Way (16:1327, 1331), Checkpoint (16:1364), Quick Pass (16:1377),
  Lap 3 (16:1395). Each one's numbers match the data; how the game applies them is in code.
- **Spring Tide ranks II and III** on a tower that already held rank I: a tower that has held one stock Beacon ignores
  the next one's number (pitfall #140), which may stop the bigger health a Tide. Only rank I's and rank III's were measured.
- **Sno Sno's Snoble drain** may bypass Made of Tougher Stuff (kit `docs/17` §8): whether it counts as "damage from
  attacks" (11:14) is in the game's code.
- **The Cardinal row's stack count** for copies of one pack Cardinal (no pack Cardinal is built yet).
- **The journal's formatting** of each upgrade's numbers (the shared journal renders them).
- **Argo's 60% pool slow** is read from the code and the kit rule; not yet measured on the PC (`ARGO_REVIEW_2026-11-04.md` §7).
- **04 rows 1 and 2** were read from the code; no self-test walks either path.

## 6. How it was done

Six readers each took a slice (Yossele; Spool; Argo; Uloe with Dazzling; the (Any) and base vessels' Directions with
scope 12; scope 16 against the game's data) and wrote a table per file with a `file:line` for every claim. The mode
files (00-06, 13-15) were read here. Every issue row was then checked against the code it cites before going in this
report; two readers' rows were merged where they found the same thing (Dazzling's [Light] numbers, Tidal Mend's level
scaling, Boutbro and the `Hero` tag). A file with more than three issues had a second pass that re-read every in-game
text and every number used a second time. Lines were counted as checked only when they state something the code can
confirm; looks, archetypes, notes and history were not counted.
