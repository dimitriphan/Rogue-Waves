# New Directions and Cardinals, from StarCraft II's campaign upgrades (2026-11-04)

Frozen report. Dimitri asked (2026-11-04 03:35Z): "Create new directions and cardinals based on the ones I had already
conceptualized. Look into Starcraft 2's campaign's upgrade paths for ideas that would fit into the power scale of the
vanilla TDS cardinals. Make sure to note which ones were created by Claude and which were created by me. Afterwards,
update all of the scopes." His standing "Yossele and Spool only" limit is lifted for this ask.

**Result:** 21 new Directions and 13 new Cardinals, all Claude-made and written as `~defaults~` in scopes `11` and
`12`. Every Direction and Cardinal in the scope, old and new, now ends with a `- By:` slot naming its maker (rule in
`00` [Values], slot in `15`). Nothing is built; nothing here has run in game. Engine claims are **read** (from the
game's code, through the kit or the engine study) unless marked otherwise.

## 1. Who made what

| | Dimitri's | Claude's, earlier (built 2026-11-03) | Claude's, new (this report) | Total |
| --- | --- | --- | --- | --- |
| Directions | 6 | 17 | 21 | 44 |
| Cardinals | 1 | 7 | 13 | 21 |

Dimitri's originals: **Made of Tougher Stuff**, **Compost Bin**, **Dandelion Season**, **Mother of Mushrooms**,
**Efficient Recycler**, **Sewing Machine** (Directions) and **Vampire Dentures** (Cardinal). Their numbers are mostly
Claude's `~defaults~`, but the idea, the ranks and the look are his, so `By: Dimitri`. The earlier Claude entries came
from `docs/mode/MECHANIC_IDEAS.md` and the 2026-11-03 build round, and are built in `RogueWavesMode`, `Argo` and `Uloe`.

What the new ones take from his:

- **His two shapes.** A *Boost* is three ranks of one stat with a twist at rank III (Made of Tougher Stuff's per-hit
  cap, Efficient Recycler's heal). A *Shift* swaps how one building works (Sewing Machine replaces the Loom, Mother of
  Mushrooms the Tree). The new ones keep both: 19 Boosts, most with a twist at rank II or III, and 2 Shifts (Cutting Jar, Button Jar).
- **His vessel rules.** Cutting Jar softens Yossele's [Growth] reset; Pattern Book sits beside Efficient Recycler as a
  second way to waste less Thread; Spare Hands and Made of Tougher Stuff both need a vessel with units.
- **Vampire Dentures' shape.** A Vessel Cardinal is one plain effect on one player's vessel or towers, priced in Pure
  Aura, often stacking: the new Vessel Cardinals follow it.

## 2. The power scale

The vanilla Cardinals' own numbers, where the PC dump has them (kit `reference/roguelab_round7.txt`, `CARD` lines;
what each script does: `docs/reports/engine_study_2026-10-28/A_cardinals.md`):

| Stock Cardinal | Its numbers | Scale it sets |
| --- | --- | --- |
| Red / Blue / Yellow Coin | 25 of one colour at the start, then 5 × the Tide's number (× 30 / the map's Tides) each Tide, shared | a steady fraction of a map's income |
| Green Coin | 6 of each colour, then 2 × the Tide's number of each | the same, spread |
| Aura Gavel | a friendly unit's death fires a 5 to 25 damage bolt within 12 tiles | small, frequent damage |
| Wave Breaker | foes within 6 tiles of the Basin have 50% less max health | strong, local to the Basin |
| Frozen Computer | foes within 7.5 / 4 tiles of the Basin slow the whole game to 0.75 / 0.5, and stun | strong, with a cost |
| Vintage Watch | +20 s on the first break, +5 s on each later one | small, utility |
| Same Day Shipping | breaks cut to 3 s, 15 of each colour per break | a trade |
| Bottom Rock | aura income × 0.01, spending free, a tower limit | a trade that rewrites the run |

The vessel and tower Cardinals (Sport Water, Fluffy Slippers, Diorama, Rusty Pipe, Frail Titan, Beast Tassle) keep
their numbers in data the dump didn't reach; their scripts set one or two stats of the vessel or of every tower
(`A_cardinals.md`), so they sit near one level 1 or 2 building upgrade, spread wide. For comparison, the base game's
level 1 upgrades are big on one building (Archer Tower Rapid Fire: attack speed +0.5 on a base 0.7, Red 35; scope `16`).

The pack's own economy: Pure Aura 3 a Tide (+1 on a weather map, +2 a bonus Tide, more from objectives), so about 45 to
60 a map; a Direction's rank II costs 20 and rank III 30; the built Cardinals cost 10 to 25.

So the rule used, written into scope `06` and `12` as `~Power~` lines:

- **Direction, Boost:** rank I about +6% to +15% on one broad stat, or one small rule; rank III about +20% to +50%,
  usually with a twist. The existing Claude Directions already sit there (Cut Costs 10/20/30%, Skipping Stones
  15/30/50%).
- **Direction, Shift:** changes one building or rule and gives something up (Button Jar: twice the Crafted at 55% each).
- **Vessel Cardinal:** about one level 1 or 2 upgrade, spread over one player's things (Oil Can: towers +5% attack speed
  a stack, Pure Aura 12). Stacking ones grow slowly per stack.
- **Team Cardinal:** about one stock map Cardinal or less (Kelp Net: -20% speed near the Basin, where Wave Breaker takes
  50% of the health and Frozen Computer slows the whole game).
- **Income** stays under a stock Coin per player at rank II: Fine Sieve pays 3 / 5 / 8 × the Tide's number in your
  scarcest colour, about 360 / 600 / 960 a map, against a base vessel's ~4,800 (`AURA_BASELINE_2026-11-03.md`).

## 3. What StarCraft II's campaigns gave

| Campaign system | How it works there | What the pack took |
| --- | --- | --- |
| *Wings of Liberty* research (Hyperion lab) | Five tiers, each a pick of two that lock each other out: Ultra-Capacitors or Vanadium Plating, Orbital Depots or Micro-Filtering, Command Center Reactor or Automated Refinery, Regenerative Bio-Steel or Cellular Reactor, Psi Disrupter or Hive Mind Emulator... | **Pairs**: two that clash are a research choice. Wound Spring ↔ Spring Tide, Oil Can ↔ Iron Rivets. A Pairs line in `06` and `12` |
| *Wings of Liberty* armory | Per-unit, per-building upgrades bought with credits (Fortified Bunker +150 life, Neosteel Frame) | Narrow Boosts on one building: Mason's Trowel, Worm Tin, Open Sign |
| *Heart of the Swarm* mutations | Three per unit, swappable: +10 HP, +50% attack speed, +1 range, heal from damage dealt | Boost ranks: Salt Barrel, Galley Kettle, Brass Spyglass, Gutting Knife |
| *Heart of the Swarm* evolutions (strains) | A permanent pick that changes how the unit plays: Swarm Strain (3 Zerglings an egg), Vile (slowing), Torrasque (comes back) | **Shifts**: Button Jar, Cutting Jar; and Sap Bucket |
| Kerrigan's abilities and passives | Fury, Chain Reaction, Heroic Fortitude, Zergling Reconstitution, Twin Drones, Wild Mutation | The vessel Directions and the free-unit ones: Galley Kettle, Chain Shot, Salt Barrel, Spare Hands, Storm Lantern |
| *Legacy of the Void* War Council | Three versions of each unit (Zealot, Centurion, Sentinel...) | Sentinel's and Avenger's return: Pattern Book, Rubber Duck; Havoc's damage taken: Chum Bucket; Dark Archon's control: Siren Conch |
| *Legacy of the Void* Spear of Adun | Ship-wide calldowns and passives: Guardian Shell, Reconstruction Beam, Solar Lance, Temporal Field, Orbital Assimilators | **Team** Cardinals and safety nets: Cork Lifebuoy, Clam Shell, Signal Flare, Tar Bucket, Pressure Gauge |

Left out on purpose: anything that adds a unit type to the base vessels (Predator, Hercules, Brood Lord), calldowns
needing a click (Purifier Beam, Time Stop: the game has no targeted Cardinal), and mind control beyond one foe a Tide.

## 4. The new entries, what they ride on, and their scale

"Rides on" names the game piece each one reuses (kit `docs/17` §8-§11, `docs/15` §18.1, the engine study). Every row
is **read**, none **verified**; a row marked *pack code* needs code the pack writes itself, on a known pattern.

### 4.1 Directions

| Direction | Vessel | Kind | StarCraft II source | Rides on | Scale |
| --- | --- | --- | --- | --- | --- |
| Wound Spring | Any | Boost | Ultra-Capacitors | `Attackspeed` Multi on the owner's `Tower`s, re-applied on spawn (`17` §11.1 rules 4-7) | +6/12/20%; pairs with Spring Tide |
| Fine Sieve | Any | Boost | Micro-Filtering, Vespene Efficiency | `Giveresources(list, owner)` at each Tide, as Compound Interest (`17` §8 income) | ≤ a Coin per player at rank II |
| Spare Hands | Any with unit buildings | Boost | CC Reactor, Twin Drones, Zergling Reconstitution | `Units` + the building's own count message (Hut, Keep, Garage, **verified**), Boutman's barrels `Vacantcurrent`, pack vessels' own counts; `Respawn` on unit buildings | +1/+1/+2 units; respawn -25/-40% |
| Galley Kettle | Any | Boost | Fury, Adrenal Overload | *pack code*: a per-hit handler on the vessel (`17` §11.1 rule 8) adding a timed `Attackspeed` mod | up to +20/35/50% while hitting |
| Chain Shot | Any | Boost | Chain Reaction, Vicious Glaive | *pack code*: per-hit handler; extra hits through `ModKitCombat` so kills are credited (rule 17) | 50% damage jumps |
| Salt Barrel | Any | Boost | Heroic Fortitude, Hardened Carapace | `Health` Pmulti on the vessel; regen from the Ramen 3 regen effect (`17` §10) | +15/30/45% health |
| Fog Horn | Any | Boost | Psi Disrupter, Temporal Field | *pack code*: a 1 s sweep around the vessel, Wave Breaker's pattern with a `Speed` Multi (Greatspear's effect) | -10/15/25% speed, 2-3 tiles |
| Clam Shell | Any | Boost | Guardian Shell | *pack code*: the Basin's `Finalhit` pattern on the vessel, its hurt handler at priority ≤ 8 for Boutman (`17` §11.1 rule 11) | once / once + 3 s / twice a map |
| Cutting Jar | Yossele | Shift | Torrasque, Sentinel | Yossele's own Growth code (`TDS_Mod/ModSource/Yossele`) | keeps 25/50/75% of Growth |
| Sap Bucket | Yossele | Boost | Vile Strain | Yossele's laser hit; `Debuff_Slow` look | -15/25/35% speed, 2 s |
| Creeping Ivy | Yossele | Boost | Malignant Creep, Creeper Strain | the Stonehenge's vine reach in Yossele's code | +1 tile, then growth |
| Double Needle | Spool | Boost | Tech Reactor, Twin Drones | the Tin's stitch timer in Spool's code | -15/30%, then a free extra |
| Button Jar | Spool | Shift | Swarm Strain | the Tin's count and the Crafted's stats in Spool's code | 2 × 55-75% for one's Thread |
| Pattern Book | Spool | Boost | Sentinel, Zergling Reconstitution | the Tin's restitch in Spool's code | 1/2/3 free a Tide a Tin |
| Chum Bucket | Argo | Boost | Havoc | Argo's tower hit, beside the Scatter Cannon's +50% vs slowed and pinned | +8/15/25% damage taken |
| Pressure Gauge | Argo | Boost | Automated Refinery, Orbital Assimilators | the Drill Rig's Fathom counter in Argo's code | starts 1/2/3 Fathoms down |
| Mason's Trowel | Smacky | Boost | Fortified Bunker, Neosteel Frame | `Health` Multi on Smacky's Walls; re-apply on Perma-Wall rebuilds (`17` §8); regen `Effect_Smackywall_Regen` | +20/40/60% |
| Snow Shovel | Sno Sno | Boost | Malignant Creep | the `Level` of the snow he drops (Entity_Ai_Hero_Snosno.cs:56-82; `MECHANIC_IDEAS.md` §4) | level 30/40/55 vs 20 |
| Worm Tin | Boutman | Boost | Orbital Depots | Boutbro's `Fishcost`, `Barrelcost`, `Boatcost` (`17` §8: his Goalvalues are rewritten) | -10/20/30% |
| Open Sign | Fie | Boost | CC Reactor | the Rest Stop's `Perm_Count` (stock 4; Entity_Ai_Tower_Fieshop.cs:36, 174-190) | 5/6/7 regulars |
| Long Leash | Buster | Boost | Raptor Strain, Centurion | *pack code*: +1 `Range` Add while he dashes, against the code's -1 (Entity_Ai_Hero_Buster.cs:44-55); dash damage % | full reach, +15/30% |

### 4.2 Cardinals

| Cardinal | Kind | Cost | StarCraft II source | Rides on | Scale |
| --- | --- | --- | --- | --- | --- |
| Brass Spyglass | Vessel | 12 | Ocular Implants, Grooved Spines | `Range` Add on the owner's vessel | +0.5 / +0.25 tile |
| Gutting Knife | Vessel, stacks | 12 | Sundering Glaive, Hydriodic Bile | *pack code*: per-hit handler reading the target's `Health`, as Vampire Dentures reads `Damagedone` | +20% a stack above half health |
| Oil Can | Vessel, stacks | 12 | Ultra-Capacitors | `Attackspeed` Multi on the owner's towers | +5% a stack; pairs with Iron Rivets |
| Iron Rivets | Vessel, stacks | 10 | Vanadium Plating | `Health` Multi on the owner's towers (more max health doesn't heal: pitfall from PC round 7) | +8% a stack |
| Tar Bucket | Vessel, stacks | 15 | Regenerative Bio-Steel, Reconstruction Beam | regen effect (Ramen 3) gated by a 4 s no-hit check, *pack code* | 1% a second a stack |
| Rubber Duck | Vessel, stacks | 10 | Zergling Reconstitution, Avenger | the vessel's respawn time, set before death (PC round 7) | -20% a stack, floor -60% |
| Siren Conch | Vessel | 25 | Hive Mind Emulator, Dark Archon | Blue Paint's charm (`Entity_Ai_Effect_Artifact_Bluepaintcharm`), on one foe a Tide | one foe under 200 health |
| Spare Plank | Vessel | 15 | Orbital Depots | Processor Jr's `Onetimebuild` button, owner's Defense building (`docs/15` §18.1) | one free wall at a time |
| Kelp Net | Team | 20 | Psi Disrupter | Wave Breaker's 1 s sweep with a `Speed` Multi | -20% speed within 6 tiles |
| Lucky Geode | Team, stacks | 20 | Micro-Filtering | Starter Seeds' sweep over every `Resource` | +10% mining a stack, everyone |
| Cork Lifebuoy | Team | 25 | Guardian Shell | the Basin's `Finalhit`, granted once more (`docs/15` §18.3) | one more last stand |
| Signal Flare | Team, stacks | 15 | Solar Lance | Aura Gavel's bolt `Artifact_Auragavel` on a 20 s timer | 15 damage a stack, every 20 s |
| Storm Lantern | Team | 15 | Wild Mutation, Energizer | Sport Water's Tide burst (`Entity_Ai_Effect_Artifact_Caffeine`, Base 0, burst only) on every tower | +25% attack speed, 10 s a Tide |

Two possible overlaps to watch: Cork Lifebuoy with Second Wind (both on the Basin; Second Wind fires on the first hit,
the Lifebuoy on the second killing blow, so both can be held), and Kelp Net with Rusted Bell (speed vs health, both
near the Basin, as the base game's Frozen Computer and Wave Breaker).

## 5. What changed in the scope

| File | Change |
| --- | --- |
| `00_Read_Me.txt` | [Values] gains the `By:` rule; [Layout]'s shared slot order ends "Look, Clashes and By, Status at the very end" |
| `02_Progress.txt` | one log line |
| `06_New_Mechanics.txt` | [Directions] gains `~Power~`, `~Pairs~` and `By` lines |
| `07`, `08`, `09`, `10` | each vessel's [Directions] lists the new names; Argo's and Uloe's no longer say "none yet" |
| `11_Directions.txt` | `By:` on all 23 old sheets; 21 new sheets; Spring Tide now clashes with Wound Spring |
| `12_Cardinals.txt` | `Power`, `Pairs` and `By` rules; `By:` on all 8 old sheets; 13 new sheets |
| `15_Templates.txt` | the Direction and Cardinal templates (and their examples) gain the `By:` slot |

The `! NEEDS ... !` lines in `11` and `12` stay: they are Dimitri's to close, by taking or replacing Claude's lists.
The project files' scope copy is not touched (it waits on his word).

## 6. For Dimitri

- Take, change or drop each new sheet: delete its `~` to take a value. The `By:` stays `Claude` either way.
- The two pairs (Wound Spring ↔ Spring Tide, Oil Can ↔ Iron Rivets) are a new rule: say if pairs should exist at all.
- Building them is a later round (xhigh, PC): the Any Directions and Cardinals go in `RogueWavesMode`
  (`16_Directions.cs`, `17_Cardinals.cs`), the vessel ones in each vessel's code.

## Sources

- [Liquipedia: Campaign research (Wings of Liberty)](https://liquipedia.net/starcraft2/Campaign/Research)
- [GamesBeat: Heart of the Swarm abilities, mutations and evolutions](https://gamesbeat.com/?p=44263)
- [Gamepressure: Legacy of the Void War Council](https://www.gamepressure.com/starcraft2legacyofthevoid/war-council/zc80a3)
- Spear of Adun names (Guardian Shell, Reconstruction Beam, Solar Lance, Temporal Field, Orbital Assimilators): from
  memory of the game, not re-checked against a source.
