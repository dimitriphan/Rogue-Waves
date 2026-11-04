# Missing upgrades, 2026-11-04: Yossele and Spool against the base vessels

> Frozen as written. Asked by Dimitri, 2026-11-04: "For now, limit the scope to just Yossele and Spool. Balance-wise, what
> upgrades are missing from each character that needs to be added?" Cloud only: no PC, no Remote Control. **Nothing in a
> mod or a scope file changed.** Every name, cost and number below is a **proposal** until Dimitri picks it; a picked one
> goes into scope `07` or `08` in his format, then into the vessel's `DESIGN.md` and code.
>
> **Since (2026-11-04):** Dimitri accepted every proposal ("Accept all changes"); all are in scopes `07` and `08` as
> written here, none is built yet.

**In short.**

1. **Yossele is complete.** Every building has 7 to 9 upgrades and a capstone, inside the base game's bands (§1). One
   balance hole is left: it has no way to turn the colour it has too much of into the one it lacks, which every base
   vessel with a gatherer has in some form (§2). One upgrade fills it: **Wood Wide Web** on the Pachira Sapling (Y1).
   The "upgrade that can't be clicked" from the play test isn't a bug: Picky Placement sat on no route the bot played,
   and every other upgrade was bought by clicking (§2).
2. **Spool is half built.** It has 15 upgrades where a base vessel has 38 to 41. No building has a capstone, the Yarn
   Stash has no upgrades, and the Thread Head and Loom stop at level 1 (§1). Its balance holes are the ones the
   2026-11-03 studies found, and each has an upgrade that fills it (§3): **Tacking Stitch** (Stitched that hold the
   line, for foes that walk round the wall), **Dyed in the Wool** (Blue for the wall, and The Hunt, which pays none),
   **Cross Stitch** and the Thread Head's **Needle Point** line (damage that keeps up with Tides 10-14), **Iron Thimble**
   and **Voodoo Doll** (a wall that outlasts the Maul's slam). The rest fill the trees to the base vessels' shape (§4).
3. **The simulator agrees, within limits** (§5, 5,040 runs). Wood Wide Web turns Yossele's The Hunt from a Tide 12
   loss into a win and changes nothing else. For Spool, all the §3 upgrades together take Purlest from a Tide 14 loss
   to a win in 92% of runs, Woozy from Tide 9 to 11 and The Hunt from Tide 5 to 11 with the gap shut, and Purlest from
   Tide 5 to 10-11 on a line foes walk round. That puts Spool about level with Smacky, not above. What no upgrade
   fixes is a Spool on Woozy or The Hunt whose wall is walked round: it is out of Red before it can buy any of them,
   so it still falls by Tide 5. That part stays with his wall test and the first-Tide numbers (PLAN Open questions 3, 6).

Ranked by what they fix, the ones to decide first are Y1, S1, S2, S3 and S5 (§6).

## 1. The trees side by side

The base game's trees, counted from scope `16` (levels as the sheets write them, `% 1` to `% 4`; level 4 is the
game's tier-5 capstone):

| Vessel | Building | Upgrades | By level | Capstone |
| --- | --- | --- | --- | --- |
| Smacky | Archer, Bomb, Wall, Mining Hut | 10, 9, 10, 9 | 3/3/3/1 to 2/2/3/2 | each has 1-2 |
| Sno Sno | Cannon, Heater, Ice Brick, Sno Box | 11, 8, 10, 9 | 2/4/3/2 to 2/3/2/1 | each has 1-2 |
| Boutman | Bouthouse, Fish Barrel, Boutboat, Gubby | 10, 10, 9, 11 | | each has 1-2 |
| Fie | The Wall, Silo Pipes, Knight's Keep, Rest Stop | 9, 10, 10, 12 | | each has 2 |
| Buster | Pedestal, Garage, Speed Pad, Checkpoint | 10, 9, 10, 9 | | each has 1-2 |
| **Yossele** | Mushroom Cannon, Dandelion Tree, Stonehenge, Sapling | **7, 8, 8, 9** | 2/2/2/1, 2/3/2/1, 2/3/2/1, 2/3/2/2 | **each has 1-2** |
| **Spool** | Cookie Tin, Thread Head, Pin Cushion, Loom, Yarn Stash | **5, 2, 6, 2, 0** | 2/2/1/0, 2/0/0/0, 2/2/2/0, 2/0/0/0, none | **none** |

The kit's rule (`TDS_Vessel_Creator/docs/02_DESIGN_RULES.md` §4): 7-11 upgrades a building, 1-3 roots, branches that
cross over, one that ends early, and one tier-5 capstone that changes how the building plays. Costs by level (sum of
aura): level 1 15-75 (median 40), level 2 25-500 (100), level 3 75-750 (300), level 4 1,500-5,025 (3,000).

- **Yossele** meets the rule on every building; the Mushroom Cannon's 7 is the floor the mods use. Its costs sit in
  the bands (level 4: 2,150 to 4,250). Nothing structural is missing.
- **Spool** misses it on every building. The sheet already says so three times (`! NEEDS YARN STASH UPGRADES !`,
  `! NEEDS THREAD HEAD LEVEL 2 AND 3 UPGRADES !`, `! NEEDS LOOM LEVEL 2 AND 3 UPGRADES !`), and the Cookie Tin and Pin
  Cushion end at level 3 with no capstone. Bringing each building to 7-8 means about 22 new upgrades; §3 has the ones
  a balance hole asks for, §4 the rest.

## 2. Yossele

**What the studies found** (`BALANCE_2026-11-03.md` §4, `BALANCE_RERUN_2026-11-03.md` §4 and §6, the play test
`PLAYTEST_2026-10-26.md` §2): Yossele is at or above Smacky on every map. Red is short for every vessel (kills pay only
Red). Yossele runs out of **Red on Woozy** and **Blue on The Hunt** (no node there pays Blue; a grown Sapling's fruit is
its only Blue), and ends Purlest holding 15,000 Blue and 15,000 Yellow it can't use. The play test's Purlest wallet
showed the same: Red 39-101 at each Tide's start, Blue up to 803.

**The hole.** Every base vessel with a gatherer can turn spare aura into what it lacks: Smacky's **Processing Plant**
(Mining Hut level 3, Yellow 250: on each gain, at most once a second, 0.5 of each other colour becomes 1 of the picked
one), Fie's **Circular Routing** (5% of each held colour every Tide), Sno Sno's **Rich Sno**, Boutman's **Farmingbed**
(kit `docs/17` §1, scope `16`). Yossele's only one is **Grafted** (fruit in one colour at 70%), which needs a fully
grown Sapling and a player who walks over the fruit. Moving Yossele's costs from Red to Blue was tried in the first
study and loses Woozy (§4 there), so the fix is a trade, not new prices.

**The "upgrade that can't be clicked".** The play test bought 31 of Yossele's 32 upgrades by clicking their entries;
the 32nd, **Picky Placement**, was on no route the bot played (`PLAYTEST_2026-10-26.md` §2 "Upgrades";
`TDS_Mod/ModSource/Yossele/IMPLEMENTATION_NOTES.md` says the same of Fortifying Roots in another run). It leads from
both of the Tree's level-1 upgrades (`DESIGN.md` "Upgrade trees"), so nothing is missing there. If Dimitri saw a button
that didn't respond by hand, that is a new bug and needs his description; the HUD's left edge cut off at 2560x1440
(`UX_FEATURES_2026-11-03.md` O7) is the nearest known one.

### Y1. Wood Wide Web (Pachira Sapling, a third level-3 upgrade)

| | |
| --- | --- |
| Name | **Wood Wide Web** (the real nickname for the fungal network trees trade nutrients through) |
| In-game line | Trades spare aura for what you lack |
| Numbers | once a second, while you hold at least 100 more of your most-held colour than of your least, 2 of the most become 1 of the least. Once a second for the player however many Saplings carry it |
| Cost | Yellow 250 (the Processing Plant's price and level) |
| Sits | Sapling level 3 beside Connected Roots and Harvesting Season; leads to both level-4 upgrades |
| Fills | Red on Woozy, Blue on The Hunt, the unused Blue and Yellow on Purlest |
| Engine | a timer in the Sapling brain spending and giving through `Helper_Resources` (kit `docs/16` §5); the stock `Tower_Converter` does the same every 3 s but converts for every player (`engine_study_2026-10-28/C_units_towers_projectiles.md`), so the pack writes its own. Needs the trade's look (a root pulse) and a warning-free HUD tick |

**In the simulator** (§5): The Hunt with the gap shut goes from lost at Tide 12 (93%) to a win in every run, about the
same as picking up half the fruit did in the re-run (§6 there); on an open line Tide 12 → 12-13. Purlest and Woozy
don't move (Yossele already wins them shut, and on Woozy's open line it has no surplus left to trade). At one trade
every 2 s The Hunt shut is lost at Tide 13 instead: the rate is the knob. Yossele is already at or above Smacky
everywhere, so Y1 makes The Hunt Yossele's best map rather than rescuing it; if Dimitri also takes P6 (fruit × 5), Web
covers what the fruit gave up, and if he doesn't, one trade every 2 s is the gentler price.


## 3. Spool: the holes and the upgrades that fill them

**What the studies found**, hole by hole, with the upgrade proposed for each. The numbers are the 2026-11-03 re-run's
(`BALANCE_RERUN_2026-11-03.md`), the PC round's (`BALANCE_ROUND_2026-11-03.md`) and the play test's.

| # | Hole | Evidence | Fills it |
| --- | --- | --- | --- |
| H1 | **Foes walk round the wall.** Three Pin Cushions across the last stretch were walked round on Purlest and Woozy (M5), and walkers don't stop for Crafted | open line: every map lost by Tide 5 whatever the numbers; Stitched that block like a wall: Tide 10-11 everywhere (re-run §5) | S1 Tacking Stitch |
| H2 | **Blue runs out on every map**, Yellow piles up. The Pin Cushion and all its upgrades are Blue; The Hunt pays no Blue at all | ends Woozy with 22 Blue and 3,840 Yellow, The Hunt with 10 Blue and 1,256 Yellow; Purlest spends 12,000 Blue on Cushions (re-run §4) | S2 Dyed in the Wool; Yellow as the second colour of the wall's new upgrades |
| H3 | **The Crafted's damage never grows.** No Cookie Tin upgrade adds Damage, while foes' health goes from 40 to 320 | +1 Damage per Tin upgrade wins Purlest 32% (first study §5); P3 (+1 flat) is half of the P2+P3 package | S3 Cross Stitch, S4 Needle Point line |
| H4 | **The Maul's slam** at Tide 10 kills every Crafted near the line and a Pin Cushion in two (M7); Spool rebuilds Cushions until Blue runs out | Pin Cushion Health 150 alone: Purlest still Tide 11 (first study §5); the Cushion has no health upgrade at all | S5 Iron Thimble, S6 Voodoo Doll |
| H5 | **Thread is short on Woozy**; on Purlest 300 Thread a map expires on the ground | Thread drop +1, or Looms twice as fast: Woozy Tide 8 → 10 (first study §5) | S7 Bobbin Winder, S8 High Thread Count, S11 Remnant Bin |
| H6 | **Too few hands on a poor map** | 6 Crafted a Tin (P2): Woozy alive at Tide 10 87% (first study §5) | S9 Patchwork Army |
| H7 | **The first Tide is thin** | no start-kit number saves a player who doesn't wall (first study §5); a person's wall decides it | no upgrade: first Tide aura buys none. P4's words and his wall test (PLAN Open questions 3, 6) |
| H8 | **A walking Stitched is hard to sell** (the play test's click missed the 16 px unit twice) | `PLAYTEST_2026-10-26.md` §3 | not an upgrade: an **UNTHREAD** button on the Cookie Tin that sells its own Crafted (half their Thread back), as the stock Sell does for each one. Listed in §6 as a fix to pick |

### S1. Tacking Stitch (Cookie Tin, a third level-2 upgrade)

| | |
| --- | --- |
| Name | **Tacking Stitch** (a tacking stitch holds fabric in place until the real seam goes in) |
| In-game line | Stitched hold the line |
| Numbers | the Tin's Stitched block foes like a Snoble (Sno Sno's mob-blocker block, the one Overstuffed Mangled already stand on in Spool's code) and have double health; the Tin stitches no Darts |
| Cost | Red 110 |
| Sits | level 2 beside Discounted Material and New Design (three ways to take the Tin: cheap runners, Darts, a living wall); leads to the level-3 upgrades |
| Fills | H1. A Spool whose Cushions are walked round still has a line foes must stop at (§5: Purlest's open line Tide 5 → 10; on Woozy and The Hunt Spool is out of Red before its Tin reaches level 2) |
| Engine | `Unit.Blocker` and the Snoble block are built and self-tested for the Overstuffed Mangled (`ModSource/Spool/Code/02_Data.cs`, `04_Content.cs`); a blocking Stitched is a second unit id with that flag. Not seen in a real Tide yet: the PC check is whether foes stop at a block that moves into place |

### S2. Dyed in the Wool (Loom, level 2)

| | |
| --- | --- |
| Name | **Dyed in the Wool** |
| In-game line | Pick the colour of its aura |
| Numbers | a COLOUR button (Grafted's, three colours); every payout of the Loom's own strikes and its Seam's comes in the picked colour, 70% of what the node paid in all |
| Cost | Yellow 150 |
| Sits | Loom level 2 beside S7 Bobbin Winder; both level-1 upgrades lead to it |
| Fills | H2. On The Hunt it is the only Blue Spool can get; everywhere it turns the Loom's spare Yellow into the wall's Blue |
| Engine | the Sapling already tells which strike made a payout (Yossele's fruit, `DESIGN.md` "Aura fruit"); the Loom's brain takes that payout back and gives 70% of it in the picked colour through `Helper_Resources` |

### S3. Cross Stitch (Cookie Tin, a second level-3 upgrade)

| | |
| --- | --- |
| In-game line | Crafted hit harder |
| Numbers | the Tin's Crafted deal double damage (Stitched 2 → 4, Darts 2.5 → 5) |
| Cost | Red 350 |
| Sits | level 3 beside One Last Surprise; every level 2 leads to both |
| Fills | H3 |
| Engine | a `Damage` Pmulti on the unit (the stock mod the Woolen Lining health already uses) |

### S4. The Thread Head's buffer line: Needle Point, Stitch in Time

The sheet: "Can be specialized as either a buffer or a thread porter." Patchwork Job starts the buffer, Threaded
Delivery the porter; levels 2 and 3 are the sheet's open line.

| Level | Name | In-game line | Numbers | Cost |
| --- | --- | --- | --- | --- |
| 2 | **Needle Point** | Points the Crafted at the weak spot | your Crafted within 3 tiles of it deal 25% more damage | Red 80 |
| 3 | **Stitch in Time** | Nearby Crafted shrug off hits | your Crafted within 3 tiles take 30% less damage | Red 300 + Yellow 100 |

Fills H3 and part of H4. Engine: Yossele's Fortifying Roots is the same aura (damage reduction to its team within its
range; built and proven in PC round 9), with a damage Pmulti beside it.

### S5. Iron Thimble (Pin Cushion, a third level-3 upgrade)

| | |
| --- | --- |
| In-game line | Sturdier pins, harder to break |
| Numbers | health +200% of base; takes 25% less damage |
| Cost | Blue 300 + Yellow 300 (Yellow is the colour Spool has spare) |
| Sits | level 3 beside Overstuffed and Burst Seams; **both** level-2 upgrades lead to it, so Pin Barrage no longer ends its branch |
| Fills | H4, and the Cushion's missing health line (every base wall has one: Reinforced, Stone Wall, Steel Wall; A Cold One; Flux) |
| Engine | stock Health Pmulti and the damage-reduction stat Yossele's Monument and Fortifying Roots use |

### S6. Voodoo Doll (Pin Cushion, level 4 capstone)

| | |
| --- | --- |
| In-game line | Prick it and everyone feels it |
| Numbers | once a Tide, the first time it falls below half health, every foe within 3 tiles is pinned (stunned) for 3 s and the Cushion mends half its health |
| Cost | Blue 2000 + Yellow 1500 |
| Fills | H4: three seconds with the Maul held is a slam skipped and the line rebuilt |
| Engine | the stun is Hallucinogen's and Entangling Roots' (`Debuff_Yossele_Daze`, its own look since stock looks load per map, pitfall #114) |

### S7-S9. Thread and hands

| # | Building, level | Name | In-game line | Numbers | Cost | Fills |
| --- | --- | --- | --- | --- | --- | --- |
| S7 | Loom, 2 | **Bobbin Winder** | Spins Thread twice as fast | 1 Thread every 3 s (6), holds 20 (10) | Yellow 100 | H5 |
| S8 | Thread Head, 3 (porter) | **High Thread Count** | Foes near it drop more Thread | foes felled within its 3 tiles drop 1 more Thread | Yellow 250 | H5 |
| S9 | Cookie Tin, 4 (capstone) | **Patchwork Army** | A bigger, faster sewing circle | 3 more Crafted alive (4 → 7), one stitched every 2 s (4) | Red 2500 + Yellow 1000 | H6 |

## 4. Spool: filling the trees to the base vessels' shape

These close the sheet's `! NEEDS !` lines and give every building a capstone. They answer no hole of §3 on their own;
their numbers are placed against the closest base upgrade, and Dimitri can swap any for his own idea.

| # | Building, level | Name | In-game line | Numbers | Cost | Closest base upgrade |
| --- | --- | --- | --- | --- | --- | --- |
| S10 | Thread Head, 2 (porter) | **Big Bobbin** | Carries more, reaches farther | carries 40 (20), collects within 5 tiles (3) | Yellow 60 | Pool (Fish Barrel) |
| S11 | Yarn Stash, 3 | **Remnant Bin** | Nothing goes to waste | Thread on the ground lasts 30 s (15); what still expires goes to the Stash at half | Yellow 300 | Farmingbed |
| S12 | Thread Head, 4 (capstone) | **Head of the Class** | The best stitch in the house | its buffs reach 4 tiles and grow to +50% damage and 45% less damage; Thread it picks up reaches the Stash at once | Red 2000 + Yellow 1500 | Engineering Firm |
| S13 | Loom, 3 | **Seam Squad** | More Seams to mine and guard | 3 Seams a Loom (1) | Yellow 350 | Mining Union |
| S14 | Loom, 3 | **Snare Line** | Seams hunt the wild aura | its Seams chase aura beasts within 6 tiles of the Loom | Yellow 300 | Fishbender |
| S15 | Loom, 4 (capstone) | **Golden Thread** (Mummer's capstone name; the sheet keeps Mummer's Loom and Thread) | Thread spun from gold | the Loom becomes a drop-off point (Spool and Thread Heads deliver there, straight into the Stash), and spins twice as fast | Yellow 3000 | Excavation Facility |
| S16 | Yarn Stash, 1 | **Roomy Pockets** | Spool carries more | Spool carries 40 Thread (20), picks it up within 2 tiles (1) | Yellow 30 | Fish of Fury (Bouthouse: upgrades on the vessel) |
| S17 | Yarn Stash, 1 | **Quick Patch** | Faster, cheaper PATCH | PATCH costs 15 Thread (20), ready again in 2.5 s (5) | Yellow 25 + Blue 10 | Repair Team |
| S18 | Yarn Stash, 2 | **Darning** | PATCH mends a whole patch | PATCH also mends everything of yours within 1.5 tiles of the target | Yellow 100 | Rejuvenate |
| S19 | Yarn Stash, 2 | **Elbow Patches** | Spool gets stuck in | Spool's health +60%, damage +100% | Red 60 + Yellow 60 | Enter the Boutman |
| S20 | Yarn Stash, 3 | **Mending Circle** | Everything mends between Tides | at each Tide's start every building of yours mends 25% of its health | Blue 150 + Yellow 200 | Rejuvenate, Cold Feet |
| S21 | Yarn Stash, 4 (capstone) | **Quilting Bee** | Everyone sews at once | a BEE button, once a Tide: every Tin stitches its full count at once with no Thread, and Crafted deal +50% damage for 15 s | Yellow 2500 + Red 1000 | Kung Fu Tackle |

- **The Yarn Stash's +25 cap.** The sheet says each Stash upgrade raises the cap by 25 (50 → 225 with all seven);
  every S-number above keeps that.
- **S14** needs a PC check first: M11's beasts died to a despawn, so whether a beast's 15 (or 75) of each colour
  goes to whoever kills it is unproven (`BALANCE_RERUN_2026-11-03.md` §10). If it does, two small beasts a Tide take
  Spool on The Hunt from Tide 5 to Tide 10 (re-run §4, P).
- **The Sewing Machine Direction** (scope `11`) replaces the Loom; whether its upgrades carry over is a scope `11`
  question for later.

## 5. The simulator check

The re-run's simulator (`balance_rerun_2026-11-03/`) copied to `balance_upgrades_2026-11-04/` with the proposals
added as upgrades that cost their aura and take their tier (`policies.UP`), bought by two new policies: Yossele's
smart player takes Focused Roots → Forest Project → Wood Wide Web on its first Sapling; Spool's takes the listed
package first on each building's next tier, then plays as before (Tins Tacking → Cross → Army, a Head Needle →
Stitch in Time → Head of the Class, or Big Bobbin → High Thread Count, the wall Sharper → Loose Stuffing → Iron Thimble
→ Voodoo Doll, the first Loom Dyed in the Wool and the others Bobbin Winder, Quick Patch on the Stash). Dyed in the
Wool picks whichever of Red and Blue is lower. 84 setups, 60 runs each, no Cardinals, 60 s breaks, each player's best
Woozy node order. "T9" is the Tide a run was lost on (the share of runs in brackets), "win" a run that cleared Tide 15.

**Spool, the gap shut** (the world where a person's wall holds):

| Package | Purlest | Woozy | The Hunt |
| --- | --- | --- | --- |
| as written | T14 (95%) | T9 (53%) | T5 (67%) |
| S1 Tacking Stitch | T14 (87%) | T9 (never afforded) | T5 (never afforded) |
| S3 + S4 + S12 damage (Cross Stitch, Needle Point, Stitch in Time, Head of the Class) | **win 97%** | T9 (never afforded) | T5 |
| S5 + S6 wall (Iron Thimble, Voodoo Doll) | T14 (95%) | T10 (83%) | T5 |
| S2 + S17 Blue (Dyed in the Wool, Quick Patch) | T14 (100%) | **T11 (100%)** | **T11 (85%)** |
| the same, Dyed at 50% | T14 (55%) | T11 (100%) | T10 (67%) |
| S7 + S10 + S8 Thread (Bobbin Winder, Big Bobbin, High Thread Count) | **win 83%** | T10 (92%) | T5 (92%) |
| S9 Patchwork Army | T14 (78%) | T9 | T5 |
| **all of §3** | **win 92%** | **T11 (100%)** | **T11 (97%)** |
| all, Dyed at 50% | win 95% | T11 (100%) | T10 (52%) |
| all but Tacking Stitch | win 83% | T11 (100%) | T11 (97%) |

**Spool, the line walked round** (the round's M5):

| Package | Purlest | Woozy | The Hunt |
| --- | --- | --- | --- |
| as written | T5 (100%) | T4 (100%) | T5 (67%) |
| S1 Tacking Stitch | **T10 (67%)** | T4 (never afforded) | T5 (never afforded) |
| damage | T7 (42%) | T4 | T5 |
| Blue | T5 | T5 (73%) | T4-5 |
| **all of §3** | **T11 (60%)** | T5 (88%) | T5 (82%) |
| all but Tacking Stitch | T8 (53%) | T5 | T5 |

**Yossele:**

| Wood Wide Web | Purlest | Woozy | The Hunt |
| --- | --- | --- | --- |
| shut, as written | win 100% | win 100% | T12 (93%) |
| shut, Web (a trade a second) | win 100% | win 100% | **win 100%** |
| shut, Web (a trade every 2 s) | win 100% | win 100% | T13 (70%) |
| open, as written | T14 (100%) | T12 (100%) | T12 (100%) |
| open, Web | T14 (98%) | T12 (no surplus to trade) | T13 (62%) |

The yardstick from the re-run (§4 there): Smacky wins Purlest, loses Woozy at Tide 12 and The Hunt at Tide 10-12 with
the gap shut; on an open line it wins Purlest and loses Woozy at Tide 6 and The Hunt at Tide 10.

**What it says.**

- **Each package answers its own map, as §3 expects.** Damage and Thread win Purlest, where Spool has the aura and
  runs out of damage. Blue carries Woozy and The Hunt, where it has neither Red nor Blue: Dyed in the Wool's trade went
  mostly into **Red** there (2,480 Red against 185 Blue a Woozy run), which is what let the rest be bought at all. The
  wall upgrades alone barely move anything; they matter against the Maul only once the damage is there.
- **"Never afforded" is the finding on Woozy and The Hunt.** Spool reaches Tide 9 on Woozy holding 16 Red; the Tin's
  level-2 and level-3 upgrades never get bought. So on poor maps the order matters: Dyed in the Wool first (Yellow,
  which Spool has), then the Red upgrades it pays for.
- **Tacking Stitch is the open line's upgrade.** It is the only single upgrade that moves Purlest's open line (Tide 5
  → 10), and the gap between "all" and "all but Tacking" (Tide 11 against 8) is its worth. On Woozy and The Hunt's open
  line Spool dies at Tide 4-5, before its first Tin reaches level 2: no upgrade bought after Tide 3 can help there.
- **Nothing overshoots.** With everything, Spool sits about where Smacky is on the shut maps and below Yossele;
  Dyed in the Wool at 50% changes little on Purlest and Woozy and costs a Tide on The Hunt, so 70% is the number if
  The Hunt should be playable for Spool.

**What the model can't tell.** It is the re-run's model, with its limits (that report §8, the round's "Limits"): one
lane, a bot that buys in a fixed order, kills credited the way the model guesses. New here: the Thread Head's auras
reach every Crafted at its core (the model has no positions inside the line), Tacking Stitch blocks as soon as four
Stitched stand (the PC check of S1), Voodoo Doll's stun holds every foe within 3 tiles of the line, and Wood Wide Web
trades every second, breaks included. The capstones beyond S6, S9 and S12 and the §4 fill (S10 aside) aren't modelled.

## 6. What to decide first

| Order | What | Why | If he says no |
| --- | --- | --- | --- |
| 1 | **S2 Dyed in the Wool** (Loom, Yellow 150) | the one upgrade that moves Woozy and The Hunt for Spool; it pays for the rest | Spool stays a Purlest-only vessel; The Hunt stays at Tide 5 |
| 2 | **S3 Cross Stitch + S4 Needle Point / Stitch in Time** | Purlest's last Tides; the sheet's own "main source of damage" | P2 and P3 (PLAN Open questions 3) do the same job as base numbers |
| 3 | **S1 Tacking Stitch** (Tin, Red 110) | the open line; the round's M5 says that's the world his wall may be in | the open line stays lost by Tide 5 on every map |
| 4 | **S5 Iron Thimble, S6 Voodoo Doll** | the wall's missing health line and capstone; worth more once 2 is in | the Cushion stays the only base-game-shaped wall with no health upgrade |
| 5 | **Y1 Wood Wide Web** (Sapling, Yellow 250; a trade a second or every 2 s) | Yossele's one hole; overlaps the fruit (P6) on The Hunt | Yossele stays as is: the studies already call it balanced |
| 6 | **S7-S9, the §4 fill (S10-S21)** | close scope `08`'s three `! NEEDS !` lines and give each building a capstone | the names are placeholders for his own; the trees stay short of the base shape |
| - | **The Tin's UNTHREAD button** (H8) | the walking-Stitched sell problem, not an upgrade | the click stays hard |

Each picked line goes into scope `08` (or `07`) in his format, marked `~` where the number is still Claude's, then into
`DESIGN.md` and code, then a PC round: S1's block on a real Tide, S2's payout swap, S6's stun on the Maul, S14's beast
kill (M11), and Y1's trade on the HUD.

## 7. Files

- `balance_upgrades_2026-11-04/`: a copy of the re-run's simulator (`../balance_rerun_2026-11-03/`, left frozen) with
  the proposals added as priced upgrades. `sim.py` and `policies.py` mark each change "2026-11-04"; `make_experiments.py`
  writes `experiments/exp_U.json`; `python3 mc.py experiments/exp_U.json 60 results/res_U.json` runs it (about 30
  minutes on 4 cores); `tables.py` is unchanged.
- Sources: scope `07`, `08`, `11`, `16`; `TDS_Mod/ModSource/Yossele/DESIGN.md`, `TDS_Mod/ModSource/Spool/DESIGN.md`;
  kit `docs/02` §4, `docs/16` §5, `docs/17`; the 2026-11-03 balance reports and the 2026-10-26 play test.
