# The scope on the game's own mechanics: a fit plan (2026-10-28)

> **Since (2026-10-29):** the eight probes of §11 ran on Dimitri's PC in RogueLab round 8 and all passed (21/0 on the
> rerun; `TDS_Vessel_Creator/reference/roguelab_round8.txt`). Every route they settle is now **proven**: rung B as a
> whole (a stock script on pack data, with every param key it reads), numbered ranks, Uloe's light stat, a death node
> with its own hit count, a pack Cardinal on a stock script, the held aura loop, the regen building and the
> owner-filtered spawn hook. Two details changed, both in §11: a stock script needs **all** its param keys (Blind on
> Barrel_Boost lists five, the unused at 0), and each light source must **replace** its own timed mod, not add
> another. The tables below still say **read** where they were written; §11 says which rows that now means proven.

Dimitri asked (2026-10-28): plan how the ideas in the scope could fit in seamlessly without creating new mechanics.
This page walks `docs/scope/` (00-16, as on 2026-10-28) idea by idea and names, for each, the stock mechanic, class or
data path it rides on. The engine tour behind it is `ENGINE_STUDY_2026-10-28.md`; the stock scripts it names are
catalogued in `ENGINE_STUDY_2026-10-28_BEHAVIOURS.md`. **Nothing here changes the scope**: where a line has no stock
route, the page proposes the closest stock-based alternative for Dimitri to pick or leave.

**Rungs** (`ENGINE_STUDY_2026-10-28.md` §4): **A** stock data only; **B** a stock script listed in pack data with
pack numbers; **C** a stock pattern copied in a pack brain; **D** a new mechanic. A and B need no new rules; C walks a
road the game already walks; D is where surprises live. **State:** **built** (in the pack, with its PC round),
**proven** (the route seen in game by a probe), **read** (from the code only), **gate** (waits on the design gate or a
`! NEEDS !` line). Kit paths: `docs/NN` = `TDS_Vessel_Creator/docs/`.

## 1. Answer

**Everything in the scope fits on the game's own mechanics.** The tables below walk 61 ideas. 19 need only stock
data or a stock script listed in the pack's data with the pack's numbers (rungs A and B), 10 mix that with a small
pack brain, and 32 copy a stock pattern in a pack brain, most of them already proven on the PC or built. **No idea
needs a new mechanic** once two roads are taken (§10): Uloe's light becomes a stat the game sums by itself, and
Photons' wandering becomes Kippy's runner.

What this reading changes in the plan:

1. **Uloe's light is a stat, not a new system** (§8.3, §10): each light source re-adds a short timed modifier to a
   `Light` stat on the towers in its radius, the way Fie's wall protects; the stat system does the sum and the fade.
2. **Blind and Illuminate are stock effects**: Blind is the barrel's timed debuff with only damage set
   (`Entity_Ai_Effect_Barrel_Boost`), Illuminate the bounty's "Targeted" mark (`Entity_Ai_Effect_Tower_Merchut`) (§8.3).
3. **Depth can be the game's easier setting turned around** (`Entity_Ai_Main_Core_Treated` with harder numbers),
   for when Depth comes off hold (§2.1).
4. **Two engine rules shape Directions and Cardinals** (§7): the same upgrade applied twice does nothing, so ranks
   and stacked Cardinals are numbered upgrades of their own; and the stock "everything a vessel builds" Cardinal
   script reaches every player's buildings, so per-player effects use the pack's own owner-filtered spawn hook.
5. **Options for six `! NEEDS !` lines** as stock scripts with pack numbers: Cardinals (§4.1), the campaign's
   modifier pool (§5.1), objectives (§6), Directions for any vessel (§7.1), and Spool's Yarn Stash, Thread Head and Loom
   upgrades (§8.2). They are suggestions; the scope is untouched.
6. **Four tempting borrows to avoid**: the stock Melting script for Aging (it shows as Melting), a stock Merchut for
   objectives (it writes Buster's unlock keys), the Opal Core for thorns (it announces "BASIN GROWS STRONGER"), and
   the vessel's own visual script on VENDCINT (a player arrow and "VESSEL DROWNED") (§3, §5, §6, §4.1).

Eight probes (§11) would prove the read routes in one RogueLab-style round after Release 1. (They ran on 2026-10-29,
ahead of Release 1, and all passed: §11.)

## 2. The mode (`04`, `05`)

| Idea (scope line) | Rung | Rides on | State |
| --- | --- | --- | --- |
| Campaign of 10 maps, 2 stretches, sea chart, sea weather | A + C | the boat's own world map and nodes; weather = stock map objects (`Main_Flood`, `Main_Chrono`, `Tower_Halberdnest`) spawned by the host (`docs/mode/SEA_CHART.md`) | built (RogueWavesMode 0.3.0, PC rounds 1-4) |
| Start from the main menu, New or Continue | C | a clone of the menu's Play button (`docs/mode/MAIN_MENU_START.md`) | proven (RogueLab M1-M7); not yet built into the mode |
| Vessels locked for the campaign | A | the boat's ready-up is the lock (`docs/15` §1); on Continue the save gives each player their vessel back | read |
| Saving between maps, Continue | C | `Bla_Data.Playerdata` in the pack's own folder (`docs/15` §8, `docs/23`) | built (mode save), proven |
| Every map voted, among the 1-3 linked ones | C | the choice dialog on each machine (`Runprompt`, proven A6), votes as commands on each vessel (Riptide), then the boat sails (`docs/15` §6, §17) | proven pieces; in-map vote not built |
| Each map harder: modifiers, spawn rates, health | A + B | the Tide table's own fields, written by the host before each Tide (`Wavecontent.Count`, `Interval`, `Upgrades`; live writes proven, `docs/15` §13); health and speed through the stock elites listed with pack params (`Enemy_Health_Upgradeobject`'s script `Entity_Ai_Enemy_Health_Upgrade` reads `Health_Multi`; `docs/13` §4) | proven pieces |
| "Changes per campaign: which modifiers and enemies" (`! NEEDS !`) | A | see §5.1: a modifier pool rolled with the chart's seed, drawn from the six stock elites and the pack's four | gate |
| 3 retries; losing a map replays it from the last save | C | the save written at each map's start; a retry sails back into the same chart node (the chart's own load). The game has no retry of its own (no code names one) | read |
| The host quitting mid-map spends a retry | C | an "in a map" flag in the save, set at the map's start and cleared at its end; Continue with the flag set spends one | read |
| Score doesn't count | A | free: a modded game already fails the record check (`Game_Endscreen.Validate`, Game_Endscreen.cs:56) | read |
| 60 s breaks | A | the Tide row's `Wavetime` | proven (table writes) |
| Won at Tide 15; leave by majority vote between Tides; bonus Tides, each harder | C | the vote as above; "leave" is the stock win path; bonus Tides by rewriting the next row each break (`docs/15` §1, §4) | proven pieces; not built |
| Dying in a bonus Tide doesn't lose the map | C | the mode marks the map won when Tide 15 clears; a Basin death after that ends the map as a win (`Mode_Normal.Coredeath` sets Lose, Mode_Normal.cs:236-282, so the mode writes Win first) | read |
| Depth: 5 levels, each adds a change (on hold) | A + B | proposal in §2.1 | gate (on hold) |

### 2.1 Depth from the game's own easier setting (a proposal for when Depth is designed)

The base game's easier setting is one upgrade on the core whose script, `Entity_Ai_Main_Core_Treated`, reads six
numbers: `Aura_Base` (extra start aura, a multiple of the mode's), `Aura_Basemax`, `Aura_Drop` (drop multiplier),
`Extra_Time` (seconds added to every break), `Extra_Wavetime` (seconds added to every Tide) and `Health_Base` (Basin
health) (`docs/15` §14.3). A pack upgrade listing the same script with **harder** numbers is a Depth level with no new
code: `Aura_Base` 0 (no extra), `Extra_Time` −10 (shorter breaks), `Extra_Wavetime` −20 (Tides end sooner),
`Health_Base` −50 (a weaker Basin). Read limits: only one such script runs per map (a static guard,
Entity_Ai_Main_Core_Treated.cs:127-132), so the five levels are five upgrades with growing numbers, one applied;
`Aura_Drop` below 1 would **take** aura on every kill (it pays `(Aura_Drop − 1) ×` the drop, split, and the wallet has no
floor, Helper_Resources.cs:12-46), so fewer drops go through `Resourcegainmultiplier` instead (it lowers Tide pay and
drops, `docs/reports/MECHANICS_CHECK.md` note). Other Depth changes on stock levers: one more stock elite per map
(§5.1), the Tide table's counts, Bottom Rock's spend multiplier the other way round. All read, none tried.

## 3. The new mechanics (`06`)

| Idea | Rung | Rides on | State |
| --- | --- | --- | --- |
| Directions: 3 offered, 1 picked before each map | C | the choice dialog (proven A6, eight cards); a command on the player's own vessel (Riptide) | proven route |
| A Direction kept on its vessel, back at every map | B | an upgrade at Priority −3 on the vessel, re-granted at each map's first spawn; a respawn keeps it (`docs/17` §2.4, proven); kit 1.7 makes `ModKitDirections` per player (`docs/15` §14.1) | proven pieces; kit 1.7 not built |
| A Direction on everything a player builds | C | the pack's spawn hook, filtered by owner, adds the Direction's effect upgrade to each new building's `Initupgradelistserver`, the way the stock Cardinals do (`docs/17` §2.3). The stock `Entity_Ai_Artifact_Teamapply` can't be borrowed for this: it gives its upgrade to whatever **any** vessel builds, wherever it sits (Entity_Ai_Artifact_Teamapply.cs:10-16), so it suits a Team Cardinal, not one player | read |
| Ranks II and III bought at VENDCINT | C | the shop's button (below); the rank is a second upgrade beside the first | read |
| Pure Aura, 3 a Tide, 2 a bonus Tide, +1 on a weather map, pearl white counter | C | a `Counter` stat on each vessel (`docs/01` §6, Mummer's Thread), paid on `Wave` (`docs/15` §3); kept off the wallet so Glass Core and the Converter never see it (`docs/mode/MECHANIC_IDEAS.md` §7) | proven pieces |
| Fund Transfer, base aura only, 50 × the coming Tide | C | `Useresources` from one, `Giveresources(list, owner)` to the other, both multipliers held at 1 (proven F1); preset amounts as dialog cards | proven solo |
| VENDCINT falls in at each break, jumps away | C | the vessel's landing tween, copied in client code (`Entity_Vis_Hero` drops the sprite from (−10, 200, −10) over 3 s, proven V1). Listing `Entity_Vis_Hero` itself would add a player arrow and "VESSEL DROWNED", so don't (catalogue D) | proven |
| Every player can shop at it | A + C | owner −1 and every session id as `Subowner`, as Merchut does (proven V1); the host checks the price | proven solo |
| A new objective offered at it every break | C | §6 | read |

## 4. Cardinals (`12`)

| Idea | Rung | Rides on | State |
| --- | --- | --- | --- |
| A new Cardinal is a run modifier like the stock ones | B | an `Artifactobject` whose core upgrades list stock artifact scripts with pack params (`docs/15` §7) | proven (Riptide) |
| Team Cardinals act on the core, foes or clock, show in the top row | B | applied to the core with the Cardinal passed as the upgrade's artifact (`Helper_Entitytools.Upgrade(core, up, Forced, artifact)`): nine stock scripts throw without it (catalogue A) | proven (mid-run grant) |
| Vessel Cardinals act on one player's things, show on the vessel's panel | B | the same effect upgrade put on that player's vessel (a −3 upgrade, `docs/17` §11.1) instead of the core | read |
| No limit; stacking | C | the HUD row counts only 8 (`Artifacttrack`); past 8 the mode shows them another way (scope note); a stacking Cardinal needs a numbered copy of its upgrade per purchase, since the same upgrade applied twice does nothing (Helper_Entitytools.cs:470-473; §7); the pack makes the copies at boot | read |
| Vampire Dentures: attacks heal a share of damage dealt | C | the rise of the vessel's `Damagedone`, healed with `Interactheal` (proven D1) | proven |
| "List of new Cardinals" (`! NEEDS !`) | B | §4.1 | gate |
| "Which base game Cardinals the shop also sells" (`! NEEDS !`) | A | the stock 40, granted mid-run as Riptide does; the 24 that can be Vessel Cardinals are in `docs/reports/SCOPE_VS_LOOP.md` §F; leave out Bottom Rock while Fund Transfer exists (it makes spending free, `docs/mode/MECHANIC_IDEAS.md` §7) | gate |

### 4.1 Cardinals that are only stock scripts with pack numbers (options for the `! NEEDS !` line)

Each row is a stock script that already does the whole effect, so the Cardinal is data: its own name, look, icon and
numbers. Names follow the style sheet's two plain words (`docs/STYLE.md`); all are suggestions.

| Cardinal | Kind | Effect | Stock script (catalogue A) |
| --- | --- | --- | --- |
| **Tide Clock** | Team | each break lasts longer (more shop time) | `Entity_Ai_Artifact_Vintage` on the core |
| **Rusted Bell** | Team | foes near the Basin lose max health while they stay | `Entity_Ai_Artifact_Wavebreaker` on the core |
| **Heavy Water** | Team | every foe walks a little slower | `Entity_Ai_Effect_Speed` (`Speed_Multi` −0.1) added by the mode to each Tide row's `Upgrades` (rung A + B) |
| **Second Wind** | Team | once a map, the first hit on the Basin clears the field and replays the Tide | `Entity_Ai_Effect_Artifact_Loop` on the core |
| **Puffer Float** | Vessel | your units burst when they fall | `Entity_Ai_Effect_Artifact_Titan`, put on your units by the owner-filtered spawn hook (§3) |
| **Spiny Hull** | Vessel | your vessel pricks what hits it | `Entity_Ai_Effect_Smackywall_Bramble` on the vessel |
| **Sea Legs** | Vessel | your vessel speeds up while it keeps moving | `Entity_Ai_Effect_Artifact_Slippers` |
| **Rising Tide** | Vessel | your vessel's stats grow every Tide | `Entity_Ai_Effect_Artifact_Caffeine` |

Most of these borrow a stock Cardinal's own script (Vintage, Wave Breaker, Loop, Frail Titan, Fluffy Slippers,
Caffeine), so each plays like that Cardinal with new numbers and a new name; where that is too close, selling the stock
Cardinal itself (§4, last row) is the honest alternative. The Opal Core's thorns are left out on purpose: on anything
but the Basin it shows "BASIN GROWS STRONGER" and sets the skin `Sub` (Entity_Ai_Effect_Artifact_Opalcore.cs:37-56).
Rising Tide, like the stock Caffeine, Coin and Money Bag, scales by 30 ÷ the map's Tide count in whole numbers, so it
reaches its full numbers on the 15th Tide; a campaign map of another length should divide 30 (engine study §14 N4).

`docs/mode/MECHANIC_IDEAS.md` §5 has six more built on stock patterns (rung C).

## 5. Enemy modifiers (`13`)

Every modifier is an upgrade on the foe (`docs/13` §6); the scope wants each shown as an effect, not a skin, so each
needs its own look (pitfall #114, the Hallucinogen lesson). The question per modifier is whether a stock script does the
stat work.

| Modifier | Rung | Rides on | State |
| --- | --- | --- | --- |
| **Cursed**: lowers the attack of nearby towers and units | C | `Entity_Ai_Artifact_Wavebreaker`'s loop exactly (every 1 s, other-team `Mob`/`Tower` in range get a held `Multi` modifier, removed when they leave), with `Damage` where it uses `Health`; or Fie's Rest Stop way, a 1.5 s timed `Addmod` re-sent each second (Entity_Ai_Tower_Fiewall.cs:248-259). Units without a `Damage` stat (walls) are untouched | read |
| **Berserk**: damage and speed rise as health falls | C | no stock script scales by missing health; the parts are stock: `Helper_Damagetools.Getpercenthealth` (the Halberd's flee check, Entity_Ai_Enemy_Halberd.cs:201-215) on a slow tick, and two held `Multi` modifiers whose value is set each tick (Melting's way) | read |
| **Aging**: −30% speed; health and damage up to +100% over 50 s | C | proven A1 (Health is `Variable`, the fraction is kept). **Not** the stock Melting script with pack numbers, tempting as it is (it grows `Multi` mods by a drain per second for `Duration_Base`, then leaves them on): it hard-codes the Melting look and its `Enemyweaken` counter, so an aged foe would look melted and the panel would say so (Entity_Ai_Effect_Enemy_Weaken.cs:37-58, 72-79) | proven |
| **Dazzling**: Blinds and Illuminates nearby vessels, towers and units | C | Cursed's loop with Uloe's two effects (`docs/15` §14.7); waits on Uloe's design for what Blind and Illuminate are | gate |

### 5.1 "Which modifiers change from campaign to campaign" (an option for the `! NEEDS !` line)

The six stock elites are already modifiers the game shows and balances: Hardened (health), Tempered (health, damage,
chases what hurts it), Superheated and Fractured (four multipliers), Annealed (hit armour), Molten (bursts and melts on
death) (`docs/13` §4). With the pack's four that is ten. **Proposal:** when the chart is rolled, the seed picks a pool
of four for the campaign (two stock, two pack), each map adds one more of the pool to its Tides from a set Tide, and
later maps raise their share. Rung A for the stock six (`Wavecontent.Upgrades`), C for the pack's four. New enemy
**types** stay with the scope's "later" (`docs/13` §5, `docs/24`).

## 6. Optional objectives (`14`)

| Idea | Rung | Rides on | State |
| --- | --- | --- | --- |
| Offered at VENDCINT every break, lasting the next Tide; team pay in full to every player | C | Merchut's flow, copied: a contract button any player can press, the next Tide's start spawns the marked foes through the enemy core's `Spawn` command, pay in the next break (`docs/15` §5). Spawning a stock Merchut instead is a trap: its 20 contracts are fixed, it pays base aura, never checks the foes died, and writes Buster's unlock keys to every player's save (Entity_Ai_Tower_Merchut.cs:159-185) | read |
| **Mind the Meteor**: fully mine a node somewhere on the map; later maps: more health, farther away | B | a one-shot node: the stock `Resource_Death` script dies and pays when emptied (`docs/mode/MECHANIC_IDEAS.md` §3), spawned with `Initstats` setting its `Healthresource` per map (Entity_Master.cs:382-386), `Loose` style near a random far tile; done on its `Ondespawn` with death; pays Pure Aura from the mode | read |
| "List of optional objectives" (`! NEEDS !`) | A-C | `docs/mode/MECHANIC_IDEAS.md` §3 has six on stock pieces (Raze the Nest, Bag the Beast, Clean Hull, Beat the Clock, Light Purse, Steady Hands); one more from this reading: **Hold the Line**, guard a stock `Glasscore`-style post through the Tide (`Entity_Ai_Effect_Artifact_Glasscore`, catalogue A) | gate |

## 7. Directions (`11`)

Two engine rules shape every Direction (catalogue B): **the same upgrade applied twice does nothing**
(`Helper_Entitytools.Upgrade` returns when the name is already on the entity, Helper_Entitytools.cs:470-473), so
ranks II and III are upgrades of their own, not repeats; and **removing an upgrade keeps a script another upgrade on
the same entity also names**, so two Directions sharing one stock script must not be taken off one at a time.

| Direction | Rung | Rides on | State |
| --- | --- | --- | --- |
| **Made of Tougher Stuff** (any vessel with minions) I-II: minions take less damage | C | a front-of-chain hit hook on each of the player's units, re-applied at each unit's spawn (`docs/17` §8 row "Minions take less damage": priority below 10, at most 8 on Boutman's fish, Snobles' `Drain` passes it). The stock `Entity_Ai_Effect_Fiewall_Regen` divides a hit by 1 + `Resistadd` at priority 9, but needs a `Resistadd` stat no stock unit has | read |
| Made of Tougher Stuff III: a cap on damage per hit | C | the same hook clamps `Damage × Damagemulti` | read |
| **Compost Bin** (Yossele) | C | kills counted near Yossele through `Ondespawn` (proven K1); FERTILIZE added to the bar mid-match (proven V2) | built (Yossele 0.2.0-dev); waits on the Direction pick |
| **Dandelion Season** I: the Tree becomes Saplings with no limit | A | a weaker copy of the Tree's data with no limit, swapped into the Tree's build slot; the Bonsai is already exactly this (40% of the Tree, Yossele `DESIGN.md`) | gate (numbers) |
| Dandelion Season II-III: Growth scales more; damage and range by the number of maxed Saplings | A + C | II is a number; III a slow tick counting the player's maxed Saplings and setting one held modifier each | gate |
| **Mother of Mushrooms** I-III: the Mushboom bursts, dealing damage by spore stacks and consuming them | C | spores are already a counter on the foe (Yossele's Cannon); the burst copies Molten's death blast query (`Effect_Enemy_Explode`: `Entityarea`, `Interacthurt`) on a cooldown, its range from Growth | gate |
| **Efficient Recycler** I-II: more Thread back from selling Crafted | A | one number in Spool's sell refund (half the Thread, rounded down) | gate (numbers); Spool 0.1.0-dev built (PR #42) |
| Efficient Recycler III: a heal around the sold minion | C | an `Interactheal` on same-team `Mob`/`Tower` in a radius, the query of `Entity_Ai_Effect_Smackywall_Regen` once instead of every Tide | gate |
| **Sewing Machine** I-III: replaces the Loom, mines faster, heals nearby, no Thread | B | a Loom copy (the Loom is Smacky's hut data) with higher mining numbers, listing the stock `Entity_Ai_Effect_Smackywall_Regen` (heals same-team units and buildings in `Range_Base` by `Effect_Add` × level each Tide); II and III are its two params. A heal every second instead of every Tide is rung C | read |
| Base vessels' own Directions | A + C | each one changes a number the vessel's own code reads (`docs/mode/MECHANIC_IDEAS.md` §4, `docs/17` §8) | gate |

### 7.1 Directions for any vessel that are one stock script each (options for `! NEEDS DIRECTIONS FOR EVERY VESSEL !`)

Each is the player's own upgrade on every tower they build (through the pack's spawn hook filtered by owner, §3),
listing one stock effect script with pack numbers, so it works on the base vessels and the pack's alike (`docs/17`
§11.1). The same upgrades through the stock `Entity_Ai_Artifact_Teamapply` would make Team Cardinals instead (every
vessel's buildings). Names are suggestions.

| Direction | Kind | Rank I | Stock script (catalogue B) | Watch |
| --- | --- | --- | --- | --- |
| **Barnacle Coat** | Boost | your towers prick what hits them | `Entity_Ai_Effect_Smackywall_Bramble` (flat `Effect_Add` back at the attacker) | the thorn hit runs the tower's own outgoing hooks |
| **Tidal Mend** | Boost | your towers heal around them each Tide | `Entity_Ai_Effect_Smackywall_Regen` | heals by the level at the time it was applied |
| **Spring Tide** | Boost | your towers gain health every Tide and take less damage | `Entity_Ai_Effect_Smackywall_Beacon` | its health gain is never removed, so it suits a Direction that never leaves |
| **Sea Salt** | Shift | foes your towers hit take more damage from everyone | `Entity_Ai_Effect_Tower_Merchut` (the bounty's "Targeted" mark) on the foe | no stock script puts an upgrade on what a tower hits, so a small pack hook does it (rung C); the mark throws on a hit with no `Damagemulti` |
| **Cut Costs** | Boost | your buildings cost less | each build button's `Goalvalue` (`docs/mode/MECHANIC_IDEAS.md` §4) | the button shows the new price by itself |

Every stock effect brings a stock look by name; stock looks load per map (pitfall #114), so on some maps the look is
missing while the effect works. Give each a pack look before it ships.

## 8. Vessels

### 8.1 Yossele (`07`) and Argo (`09`): built

Every mechanic on both sheets is built and passed its PC self-test (Yossele 0.2.0-dev, round 9e, 160/0; Argo 2.0,
round 6, 139/0), each on routes the kit already names: the stock fighter and ranged AIs armed by the vessel's brain,
front-of-chain hit hooks, counters, per-Tide growth through the kit's Tide hook, client looks from host cues. Nothing
here changes them. The only open lines are their Directions (`! NEEDS YOSSELE'S DIRECTIONS !`, `! NEEDS ARGO'S
DIRECTIONS !`): §7.1's any-vessel Directions fit both, and Compost Bin, Dandelion Season and Mother of Mushrooms are in §7.

### 8.2 Spool (`08`): built on PR #42

Spool 0.1.0-dev passed PC round 1c (120/0); its rules ride on the stock fighter AIs, the fish's Relocate picker, Smacky's
hut and wall data and a front-of-chain hook (Spool's `IMPLEMENTATION_NOTES.md` on that branch). Thread lives in a
host list drawn by each client, not as networked pickups, because the stock pickup (`Special_Aurapickup`) pays fixed
aura only (catalogue A). Options for its three `! NEEDS !` lines, each a stock script with Spool's numbers:

| Line | Option | Rung | Stock script |
| --- | --- | --- | --- |
| Yarn Stash upgrades | a bigger cap per level (the sheet's +25) | A | a number |
| | the Stash mends what stands around it each Tide | B | `Entity_Ai_Effect_Smackywall_Regen` |
| | the Stash hardens a little every Tide | B | `Entity_Ai_Effect_Smackywall_Beacon` |
| Thread Head levels 2-3 | buffer path: nearby Crafted fight faster for a while | B + C | `Entity_Ai_Effect_Busterdog_Towerboost` (timed Damage and Range) or `_Snosnoheater_Sweat` (timed attack speed), put on units by the Head's brain |
| | porter path: carries more, walks faster | A | numbers |
| Loom levels 2-3 | the Loom's owner earns a little on every ore hit | B | `Entity_Ai_Effect_Smackyhut_Scrap` |
| | pick the colour it mines | B | Smacky's hut Convert button (`Entity_Command_Tower_Smackyhut_Convert`, catalogue D) |

### 8.3 Uloe (`10`): before its design gate

Uloe is the sheet with the most new rules, and every one has a stock road. The key is light (§10, item 1).

| Idea | Rung | Rides on | State |
| --- | --- | --- | --- |
| Ranged attack | B | the stock `Entity_Ai_Mob_Ranged`, armed by the vessel's brain with its shot (Yossele's laser does this; data alone leaves it frozen in `Attack`, catalogue C) | proven pattern |
| **Plug In**: plug into a node or Power Bank, light while in range; cord moves with Uloe | C | a host range check; the cord a `LineRenderer` reset every frame on each machine (Argo's harpoon rope, proven); the light level read from the node's aura stats or the Power Bank's level | proven pieces |
| **Light**: towers stronger by the light they receive; Photons and Uloe give light | C | a `Light` stat on every tower that can be lit; each source re-adds a 1.5 s timed `Add` modifier to the towers in its radius every second (Fie's wall protection loop, Entity_Ai_Tower_Fiewall.cs:248-259), so the stat system sums the light and it fades on its own when a source moves away; each tower's brain reads its `Light` (§10) | read |
| **Illuminate**: foes take more damage, strength by source | B + C | `Entity_Ai_Effect_Tower_Merchut` with pack `Effect_Multi` (one upgrade per strength); it lasts until removed, so the pack's brain takes it off after its time | read |
| **Blind**: foes deal less damage, strength by source | B | `Entity_Ai_Effect_Barrel_Boost` with only `Damage_Multi` set (a timed four-stat change that removes itself); one upgrade per strength | read |
| **Magnifying Glass**: beams light; damage by light; more damage on the same target | B + C | the beam is a stock projectile, `Projectile_Ai_Beam` (tracks its target, hits the nearest foe in line, catalogue C), fired by the tower's brain; damage reads `Light`; the ramp is a held modifier reset on a new target | read |
| **Power Bank**: makes Photons | B | the stock `Entity_Ai_General_Spawnmobs` (params: unit name → respawn seconds) or the kit's unit building (`docs/12` §3) | proven pattern (kit) |
| Power Bank upgrade: Photons burst on death, blinding and illuminating | B + C | `Entity_Ai_Effect_Artifact_Titan` (a death burst by level; its kills credit nobody) plus Blind and Illuminate on what the burst hits | read |
| **Mirror**: a wall, lit to blind foes; chance to nullify by Blind level; passes light on | C | front-of-chain hooks on the Mirror: Blind the attacker (Bramble's flow, an upgrade instead of damage), set `Damagemulti` to 0 by chance (the bucket's invulnerability at priority 8, `Entity_Ai_Effect_Boutmanbucket_Boost`); passing light on is the Mirror acting as a light source | read |
| **Flood Light**: plugs into nodes, gives light; aimed at towers; a radius or a line | C | node links as the mining hut's (`docs/16` §4); the line is `Helper_Entityquerytools.Entityrayall` | read |
| **Photons**: weak hits that Blind and Illuminate | B + C | the stock fighter AI with pack stats; the effects put on by a small hit hook | proven pattern |
| Photons wander where they spawn, follow Flood Light paths | C | no stock unit wanders (no game AI uses randomness); the closest is Kippy's runner (`Entity_Ai_Mob_Kippy`: walks to the farthest ally, gives a buff, picks the next), pointed at towers so a Photon carries light from tower to tower; following a path is a `Move` order along the Flood Light's line | read |

## 9. Bugs and online (`01`, `03`)

| Idea | Rung | Rides on | State |
| --- | --- | --- | --- |
| F picks a minion after a respawn | C | client-side: the main vessel first in the `Hero` tag list (`docs/15` §9) | built, not seen working (needs a real Boutbro) |
| No way back in after a drop | C | the host reopens the lobby while a seat is empty (`docs/mode/REJOIN.md`) | built, solo round passed; online left |
| 2-4 players, host runs the run, same pack version | A + C | host authority is the game's own; the game checks no version (`docs/19` §1), so the pack compares its version itself when a player joins | read |

## 10. What needs a new mechanic, and the closest stock road

Read against the whole game, the scope has **two** ideas with no stock system under them, and both have a road that
turns them into rung C. Three more were rung D when first written and are already built with their own code.

| # | Idea | Why there is no stock system | Closest stock road |
| --- | --- | --- | --- |
| 1 | Uloe's **light received** | the game's lighting is rendering only (`docs/15` §14.7) | **light as a stat**: a `Light` stat on each lightable tower, fed by timed `Add` modifiers each source re-sends every second. The stat system does the summing, light fades by itself when a source leaves, and the panel can show the number. Base vessels' towers have no `Light` stat; the pack adds one at their spawn only if a Direction ever lights them (a modifier on a missing stat never applies, `docs/17` §2.1) |
| 2 | Photons **wandering** | no game AI uses randomness; every unit walks to a goal | Kippy's runner (visit, buff, next) aimed at towers, which also gives Photons a job the player can read; or the fish's guard-a-spot with a leash |
| - | D-MDS (seeds shoot down shots) | shots are pooled objects, not entities | built in Yossele with its own code (`DESIGN.md`) |
| - | Slow Unravel (half a hit now, half later) | no stock delayed damage | built in Spool's Pin Cushion hook |
| - | Thread on the ground, carried home | the stock pickup pays fixed aura only | built in Spool as a host list drawn by each client |

**Retries** look new but aren't: the chart already reloads a node and the save already restores a campaign, so a retry
is those two in order. For a smaller safety net inside a map, the stock Loop Cardinal replays a Tide (§4.1).

## 11. What to prove on the PC

Every **read** route above could ride one RogueLab-style round; none needed a round of its own. They ran as RogueLab
round 8 on 2026-10-29 (Dimitri's "Do a PC round to check"): the first launch passed 20 and failed one on the probe's
own bookkeeping, the rerun passed all 21. Results are from the rerun; every line is in
`TDS_Vessel_Creator/reference/roguelab_round8.txt`.

| # | Probe | Pass when | Settles | Result (round 8) |
| --- | --- | --- | --- | --- |
| R1 | A pack upgrade listing `Entity_Ai_Effect_Barrel_Boost` with only `Damage_Multi` −0.3 and `Duration_Base` 5, put on a foe | its Damage is ×0.7 for 5 s, then back, and the upgrade is gone | rung B as a whole: a stock script runs on pack data (Blind, §7.1, §8) | **pass** on a tower (a player's, so the probe could build it), with all five keys the script reads (the other three at 0): 4 → 2.8 → 4 after 6.5 s, upgrade gone. With only the two keys it throws `KeyNotFoundException: 'Health_Pmulti'`: pack data copies a stock upgrade's keys whole (pitfall #117) |
| R2 | The same upgrade applied twice; then a second asset with another name | the second apply changes nothing; the second asset stacks | Direction ranks, Cardinal stacking (§4, §7) | **pass**: 2.8 stayed 2.8 with one instance; the second asset made 1.6 (×0.4, the `Multi` bucket sums) (pitfall #115) |
| R3 | A `Light` stat on a pack tower, two sources each re-adding a 1.5 s `Add 1` every second | `Light` reads 2 with both in range, 1 with one, 0 about 1.5 s after both leave | Uloe's light (§10) | **pass** when each source **removes its own last mod before adding the next**: each second `[2 2 2 2 1 1 1 1]`, then 0. Adding without removing reads double half the time (`[1 1 2 1 2 1 2 1]`; pitfall #119) |
| R4 | A `Resource_Death` node spawned with `Initstats` `Healthresource` 12 | it dies and pays once after 12 hits | Mind the Meteor (§6) | **pass**: a node with `Entity_Ai_Resource_Death` (a copy of `Resource_Red`, its mine script swapped) died on hit 13 and paid once; as kit pitfall #106 says, the hit that takes it from 0 is the one that pays, so N hits need `Healthresource` N − 1 |
| R5 | A pack Team Cardinal whose core upgrade lists `Entity_Ai_Artifact_Vintage`, granted mid-map with its `Artifactobject` | breaks lengthen by its param; no exception | Cardinals as data (§4.1) | **pass**: the break's timer jumped +40 (`Timeinit_Base` 40), no error |
| R6 | Cursed's loop on a foe: player towers within 3 get a held `Damage` Multi −0.3, removed when it leaves | a nearby tower's Damage drops and comes back | Cursed, Dazzling (§5) | **pass** with a wall as the source: 2 away 4 → 2.8 → 4 once the source was gone; 4.2 away untouched. The loop must skip its own source and forget towers that die (the first launch's one failure) |
| R7 | A pack building listing `Entity_Ai_Effect_Smackywall_Regen` | hurt neighbours heal at the next Tide | Sewing Machine, Yarn Stash option (§7, §8) | **pass**: a neighbour 1.4 away healed 15 → 20 at Tide 1 (`Effect_Add` 5) |
| R8 | The pack's spawn hook adds a −3 upgrade listing `Entity_Ai_Effect_Smackywall_Bramble` to one player's new towers | that player's towers prick attackers, a teammate's don't; a sold and rebuilt tower gets it again | any-vessel Directions (§7.1) | **pass**: the player's tower had it and pricked; another owner's (−1, standing in for a teammate) had not; a rebuilt one had it again |

Also seen in the same round: N1-N5, N7 and N13 of the engine study (its §14). Teamapply reaching every player's
buildings, and new ones even after its upgrade is removed, is now verified, so per-player Directions keep the pack's
owner-filtered hook (R8). Online behaviour (a real teammate, a client) is still read only.
