# Dimitri's answers to the open decisions, and what each needs (2026-11-05)

Dimitri, 04:59Z, on `OPEN_DECISIONS_2026-11-05.md`: "10. Cursed should also weaken vessels. 15. Create art for new looks
and rename as well. 18. Uloe can share light up to a limit. 19. Build all for now. 25. Balance as if you only build one
wall, and use recommended for rest. 26. Reduce the amount mined if it is unbalanced. 34. Create art for every missing
asset. Use defaults for the rest."

This page records every answer, what this PR changed (scope and docs only), and a numbered hand-off for the threads
he already queued at 04:51Z: **the build thread** (build the unbuilt candidates), **the text audit** and **the balance
sweep**. Nothing here is built, and nothing touched the PC.

## 1. Every answer

"Scope" means this PR wrote it into `docs/scope/`; an id (B1, A1, S1) is a hand-off item in §3.

| # | Answer | Where it goes |
| --- | --- | --- |
| 1 | The boat opens the map vote when none was set | B1 |
| 2 | Lobby Cardinals lock once a campaign starts, offering only what the shop may sell (closes MODE-D9) | scope 04 [Sea Chart] "Lobby Cardinals"; B2 |
| 3 | Caps kept: a new one stacks to 6 copies, a base game Cardinal is bought once; both then leave the shop | scope 12 "Stacking"; B3 |
| 4 | A held stock Cardinal leaves the shelf | scope 12; B3 |
| 5 | Trinkets on the vessel's panel; Cardinals past 8 as a "+N" count | scope 12 "No limit"; B4 |
| 6 | MODE-D5 stays as built: extra health, no Hardened look | nothing to do |
| 7 | The mode polish's nine readings confirmed | PLAN Decisions |
| 8 | The weather's long line stays the boat's; the map start announces its name | scope 04 [Sea Weather] "Shown" |
| 9 | Weather numbers as written | scope 04 (defaults taken, question removed) |
| 10 | **Cursed also weakens vessels** | scope 13 "Does"; B5 |
| 11 | Dazzling Blinds and Illuminates at Light level 1 (15%), lingering 3 s | scope 13 "Numbers"; B5 |
| 12 | The pack's own foe modifiers count as elite for Spool's Thread | scope 08 "Earned by"; B6 |
| 13 | Objective kinds are the template's: Keep the Basin Clean and Mind the Vessels Guard, Beat the Clock Rush, Raze the Nest Hunt | scope 14 |
| 14 | Spring Tide and Tidal Mend rebuilt on the mode's own effects (MODE-C2, C3, C5) | B7 |
| 15 | **The Shift Directions rename and get new looks** (Dandelion Sapling, Primordial Mushboom, Sewing Machine) | B8; A1 |
| 16 | Cut Costs on Boutman stays as built | nothing to do |
| 17 | Monument's Cleanse strips foes' debuffs only (YOS-2) | B9 |
| 18 | **Uloe's Light is shared with teammates, up to a limit**: up to `~4~` Light per building from teammates (Claude's number) | scope 10 [Light] "Sharing"; B10 |
| 19 | **Build all**: the 15 new Directions, 11 Trinkets and 4 Cardinals, research-choice pairs and Duos | B11 |
| 20-23 | Yossele's text: FERTILIZE's 6 tiles is the hotkey's pick; the 90% cap written; Focused Roots "+80%"; Natural Remedy counts stage 1 | scope 07 |
| 24 | Yossele's mining (P5, P6): no change until his play test | nothing to do |
| 25 | **Spool balanced as if one Pin Cushion wall is built**, the report's recommendations for the rest | S1 |
| 26 | **The Loom's mining lowered if it is unbalanced** | S2 |
| 27 | Salt Sting 1 a second without Brine (the code's); the text says so | B12 |
| 28 | Argo: one slow per source, never stacked with its own re-ticks | B13 |
| 29 | Argo: bosses are never pinned or held either | B13 |
| 30 | Argo's 14 review changes stand; the next Argo build builds them | B14 |
| 31-32 | Uloe's 24 upgrades and his names as written; Supernova a SURGE button once a Tide | scope 10 (defaults taken, questions removed); B14 |
| 33 | Every other default in 04 and 09-14 taken | scope: 1,192 values lose their tildes (below) |
| 34 | **Art for every missing asset** | A1-A4 |
| 35 | The kit keeps its older-mod examples | nothing to do |
| 36 | `D:\TDS_second` kept | nothing to do |
| 37 | His PC's scope copy synced at the next PC round | the next PC round |
| 38 | Scope wording fixed: 12's Trinket note, 06's base game Cardinal cost (Pure Aura 15), 15's Colour slot; Spool's Cookie Tin role is Tower | scope 06, 12, 15; Spool `DESIGN.md` |
| 39 | The audit's appendix leans taken | scope 04, 09, 10, 11, 13; code-side ones B15 |

**The defaults taken (33):** per `00_Read_Me.txt` [Symbols] ("Delete both ~ to make it yours"), every `~value~` in 04
and 09-14 lost its tildes, 1,192 in all; a script compared each file with the tildes taken out and the only other
changes are the edits listed above. Kept as Claude's on purpose: the lines beside a `! NEEDS !` (04's modifier
schedule, 12's shop list), the new light-sharing limit in 10, and the two mining lines the balance sweep may lower
(08's Loom, 10's Flood Light). Scopes 07, 08, 15 and 16 keep their defaults: the questions didn't take them. 04's
`! NEEDS !` lines stay open. The project-files copy of the scope and the Scope Editor are now behind the repository in
12 files; overwriting them needs his word.

## 2. What this PR changed

`docs/scope/02` (a Log line), `04`, `06`, `07`, `08`, `09`, `10`, `11`, `12`, `13`, `14`, `15`;
`TDS_Mod/ModSource/Spool/DESIGN.md` (the Tin's role); `docs/PLAN.md` (Part C, Decisions, Log); this page and the reports
index. Docs only: nothing compiled, nothing run.

## 3. Hand-off

### For the build thread (code; cloud-check each part, then one PC round with his OK)

- **B1** (1, audit 04 row 1): when a won map ends without the vote to leave (a bonus-Tide loss, the host's quit after
  Tide 15), open the same map vote on the boat when no choice is set (`20_Vote.cs:356-381`, `05_Boat.cs:13-15`, `:167-168`).
- **B2** (2, MODE-D9): lock each player's lobby Cardinal picks once a campaign starts and filter the picker as the shop
  filters (`17_Cardinals.cs:38-42`, `NotSold`). Then MODE-D9's Tide 34 win can't happen; mark it closed.
- **B3** (3, 4, audit 12 row 4): the shop stops offering a stacking pack Cardinal or Trinket at 6 copies and a stock
  Cardinal once held (`RWCardinals.Stacks`, `Buy`, the shelf roll in `17_Cardinals.cs:167-180`).
- **B4** (5, audit 12 rows 1-3, MODE-C11): a held Trinket shows on its vessel's panel (put its upgrade on the vessel
  with no script, as Directions are; `17_Cardinals.cs:96`, `:307`), and Cardinals past the row's 8 show as "+N" (the
  count the file's header promises, `17_Cardinals.cs:5-6`, `:258-262`). Trinkets get their own shelf slot (audit 12 row 1).
- **B5** (10, 11, audit 13 rows 1-3): Cursed's aura also cuts vessels' attack 25%; Dazzling Blinds and Illuminates at
  level 1 (15%), and Illuminate lingers 3 s after leaving the aura as Blind does (`RogueWavesFoes/Code/00_RogueWavesFoesMod.cs:84`, `:142-144`).
  The four modifiers' in-game lines in scope 13 are the code's own; no change there.
- **B6** (12, audit 08 row 1): count `RW_Modifier_` foes as elite for Spool's Thread drop. A Spool change at his word
  (it lifts "leave Spool as is" for this line only).
- **B7** (14, MODE-C2, C3, C5, audit 11 row 1, simplification finding 1): Spring Tide and Tidal Mend on the mode's own
  effects instead of `Entity_Ai_Effect_Smackywall_Beacon` and `_Regen` (`16_Directions.cs:35`, `:42`, `:317-348`):
  no Beacon cut cancelled, no `Resistadd` on Fie's walls, Mend's heal read each Tide at the tower's level now. Scope 11's
  numbers stand.
- **B8** (15, audit 11 rows 6, 7, 9): the Shift Directions rename the building's panel: Dandelion Season's Tree becomes
  Dandelion Sapling, Mother of Mushrooms' Tree Primordial Mushboom, Sewing Machine's Loom Sewing Machine
  (`Spool/IMPLEMENTATION_NOTES.md:159` calls the Loom's name a technical limit: find the way). The new looks come from A1.
- **B9** (17, YOS-2): Monument's `Cleanse` strips only foes' debuffs and leaves stock trade-off modifiers alone
  (`Yossele/Code/12_Hooks.cs:119-145`).
- **B10** (18, ULO-3): a building takes Light from teammates' Uloes too, at most 4 a building from teammates (scope 10
  [Light] "Sharing"), and the host accepts AIM only from the building's owner. Every by-team path the finding lists
  (`Uloe/Code/05_Server.cs:138-175`, `07_Building.cs:324-377`, `10_Mirror.cs:48`, `08_Bank.cs:239`) needs the cap.
- **B11** (19): build every Claude-made Direction, Trinket and Cardinal in scopes 11 and 12, the research-choice pairs
  (12 [Rules] "Pairs") and Duos (06), with the routes in `DIRECTIONS_CARDINALS_2026-11-04.md` §6 and
  `ROGUELIKE_DIRECTIONS_CARDINALS_2026-11-04.md` §5. Their icons come from A2.
- **B12** (27): Salt Sting's in-game text and scope 09's line say "1 a second without Brine", the code's number.
- **B13** (28, 29, audit 09 rows 1-4): Argo's slows renew instead of stacking with their own re-ticks (Deep Water, the
  Tremor, Barnacle Shot, Tether); `ArgoUtil.Pin` and `Root` skip bosses as the reel does (600 max health or more).
- **B14** (30-32): Argo's 14 review changes and Uloe's 24 upgrades are the queued candidates already; Supernova is a
  SURGE button once a Tide.
- **B15** (39, audit code-side rows): Made of Tougher Stuff skips only the player's own vessel, not every `Hero`
  (audit 11 row 4); Compost Bin's rank III text says "within a much larger radius" (11 row 8); the objective node takes
  12 + 3 x map hits (`Healthresource` 11 + 3 x map, 14 row 2).

### For art (34, 15; Claude draws, temporary-art rules in `docs/TEMP_ART.md`)

- **A1:** the three Shift looks: Dandelion Sapling, Primordial Mushboom (a large mushroom tower), Sewing Machine (a
  classic light blue sewing machine).
- **A2:** an icon for every Direction, Trinket and Cardinal B11 builds.
- **A3:** the objectives' looks in scope 14 (a mop and bucket, a cracked stopwatch, a life ring, a burning nest) and
  Mind the Meteor's white crystal node.
- **A4:** every other asset with no art yet: a pass over the scope's Look lines, `docs/reports/SCOPE_GAPS.md` and
  `docs/TEMP_ART.md` first, listing each before drawing. Reading "missing" as no art at all; final versions of the
  temporary art (Yossele's sheet for Release 1 among them) only if he says so.

### For the balance sweep

- **S1** (25): balance Spool assuming the player builds one Pin Cushion wall, not a full wall: sweep P1-P3
  (`BALANCE_2026-11-03.md` §8: Dart Range, Crafted a Tin, Stitched and Dart Damage) until one wall carries Purlest and
  Woozy as Smacky does, then write the picks into scope 08. Take the report's recommendations for the rest: P4, the
  "wall the path with Pin Cushions first" line in the journal and tooltip.
- **S2** (26): measure the Loom's mining against the base vessels (`AURA_BASELINE_2026-11-03.md` §5: about 5 times a
  Mining Hut today); if it is above the base range, lower it to the top of that range and write the build's number
  into scope 08's Mining line (P7). Claude adds: check Uloe's Flood Light the same way, since it mines like the Loom.

## 4. Spool's defaults (05:10Z: "Use all defaults")

On `SPOOL_DEFAULTS_2026-11-05.md` (PLAN Open question 2, S1-S17). S1-S9, S11, S12 and S14 keep what is built: nothing to
do. Scope 08's defaults are his now (143 values lose their tildes), except the four numbers the balance sweep owns
(Stitched Damage, Dart Damage, Dart Range, Crafted alive a Tin) and the Loom's mining line (S1-S2 above). His answer
lifts "leave Spool as is" for these items.

### For the build thread

- **B16** (S10): the Thread Head's stock MOVE goes; SPOT stays.
- **B17** (S13): once he has tried selling a walking Stitched by hand, and if it is hard, give the Crafted a bigger
  click box for the sell picker. Ask him first; he owes the try (`TESTING_ROUND_3_2026-11-05.md` §4).
- **B18** (S15): Pin Barrage leads to Iron Thimble at level 4, as scope 08 says.
- **B19** (S16): build scope 08's 22 unbuilt upgrades and the Tin's UNTHREAD button with the queued candidates, after
  the balance sweep has written its one-wall numbers into 08 (`UPGRADE_GAPS_2026-11-04.md`).

