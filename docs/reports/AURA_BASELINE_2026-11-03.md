# Aura baseline 2026-11-03: what a map pays, what each vessel mines, and where Yossele and Spool sit

> Frozen as written. Asked by Dimitri (2026-11-03 06:04Z): "Calculate the average aura per map, as well as each base
> vessels aura mining capabilities per map and use those calculations as a baseline to balance yossele and spool."
> Cloud only. No PC round and no Remote Control. Nothing in the mods changed. Every number below is a **proposal** until
> Dimitri picks it. This report follows `BALANCE_2026-11-03.md` and uses the same simulator. The base vessels' sprites
> stay private: only numbers from the dumps and the decompiled code are used here.

**In one paragraph.** A node pays every 5 hits from anything that hits for Damage 1 or more. Stock gatherers hit for
less, so a node takes 10 to 18 of their hits to pay. Read from the game's code, one stock gatherer beside a Red node
earns **17-36 aura a Tide**, or **30-106** with its first mining upgrades. Here a Tide means the 180 s Tide plus its
60 s break. Yossele's Pachira Sapling earns **60-90**, or 138 with Focused Roots. Spool's Loom earns **96**, or 288 with
its Seam. Over a 15-Tide map, a base vessel ends with about **4,800 aura**: 150 from the start, 885 Red from kills and
about 3,800 from its gatherers. Map by map that is about 8,100 on Purlest, 3,700 on Woozy and 2,600 on The Hunt. Mining
by hand in every break would add 400-4,200 more.

Yossele's gatherers out-earn the base vessels' average **2.0-2.5×**, and Spool's **4.1-5.3×**. The simulator says the
two answers differ:

- **Yossele's Sapling can come down.** At Attackspeed 0.15 (now 0.25), its mining is 1.2-1.5× the base average, and
  Yossele still does at least as well as Smacky on all three maps.
- **Spool's Loom cannot.** It is Spool's only Blue for its Pin Cushion wall. At Hut speed Spool loses Woozy at Tide 3-5,
  and the P1-P3 package stops winning Purlest.
- **P1-P3 stand.** The gap they close is Purlest's Tide 10-11 wall, not income.

Reading the Mining Hut from the code also showed that the earlier study's Smacky was too strong. Its Hut mined about
1.7× what the game's miner does. With the code's Hut, Smacky loses Woozy at Tide 10, not 12.

## The proposals at a glance

| # | Vessel | Change | From → to | Why | Confidence |
|---|---|---|---|---|---|
| P5 | Yossele | Pachira Sapling, Attackspeed | `~0.25~` → `~0.15~` | brings its mining to 1.2-1.5× the base average, the top of the base range; Yossele still at or above Smacky on every map (§5) | medium: needs M8-M9 (§7) |
| P6 | Yossele | Aura fruit's value | Attackspeed × Damage × **15** → × **5** of each colour | a grown, Focused Sapling's fruit is worth 27, over 5× the node's own payout of 5; with every fruit picked up it earns 8× the best stock path (883 a Tide against Ore Scraps' 106). At × 5, a fruit is worth about one more payout (§3) | medium: his own number, needs M10 |
| P7 | Spool | Scope `08` Loom line "Mining: ~like Smacky's Mining Hut~" | → "~like a Sapling: a Damage-1 strike every 2.5 s~" (the build) | the build mines 5× a Hut, and Spool needs it (§5); the line should say what is built | high |
| - | Spool | P1-P3 from `BALANCE_2026-11-03.md` | unchanged | the baseline does not touch Purlest's Tide 10-11 gap (§6) | as before |

P5 and P7 change Claude's `~defaults~` in scopes `07` and `08`. The fruit's × 15 in P6 is Dimitri's own number, so
it is only offered. All three wait for his word (PLAN Open questions 4).

## 1. How a node pays (read from the code)

- **A node** (`Entity_Ai_Resource_Mine`) has `Healthresource` 4 and `Health` 1 (`Resource_Red`: RogueLab round 7;
  `Health` read on `Resource_Orange`). A hit takes `min(Damage, 1) + Damageresource` off `Healthresource`
  (`Helper_Damagetools.Dodamage`). Once that is 0, hits take the raw Damage off `Health`. The hit that empties
  `Health` pays every player (split) and refills the node. So a node pays every **ceil(4 / ore hit) + ceil(1 / Damage)**
  hits: 5 for anything with Damage 1 or more, 10 at Damage 0.5, 13 at 0.4, 18 at 0.3.
- **Smacky's miner** (`Entity_Ai_Mob_Smackyminer`): Damage 0.5, Attackspeed 0.5, Speed 1. One hit per round trip: walk
  to the node, 0.3 s wind-up, hit, wait 2 s, walk back to the Hut, wait 2 s. That is about 6.8 s a hit with the Hut
  beside the node, so a payout takes about 68 s. The Hut starts one miner. Mining Operation and Mining Union add one
  each. Overtime makes them 1.75× faster. Ore Scraps pays the Hut's owner 1 of each colour the node has, every hit, on
  top of the payout. On the Opal that is 3 a hit. Marketplace's whole-map hits are tiny: 1 damage split over every
  node, every 4 visits.
- **Sno Box** (`Entity_Ai_Tower_Snosnobox.Tick`): every 5 s it hits every node in Range (3, +1 a level), the k-th
  nearest at 0.4 / k. Slush: 0.4 a second at Damage 0.5. Soft Sno: 0.6-1.65 a second, faster the less snow is on its
  own tile, and the Box piles snow there itself.
- **Silo Pipes** (`Entity_Ai_Tower_Fiesilo`): builds its pipe to the nearest node at any range (0.25 tiles a second),
  then hits it every 4 s at Damage 0.3. High Flow: Damage 0.55. Quick Construct: 1.5× faster. Old Pipes: a second node.
  Circular Routing pays 5% of each wallet entry every Tide, at most 50 each.
- **Checkpoint** (`Entity_Ai_Trap_Bustercheckpoint`): every 3.3 s it charges 0.5 × efficiency (100% within 2 tiles of
  its node, 10% at 10). A same-team unit touching it spends the charge as Damage-1 hits. So it mines only while
  Buster's dogs or Buster run through it. The tables assume a dog lap every 10 s. Lap 1 and Lap 2 raise the rate to
  0.675 a second. Tune Up pays 5 of each colour on the collector's next attack (not counted).
- **Boutman** has no gatherer. His attack ignores nodes until Fishbender (Yellow 1,750, tier 4 on the Bouthouse).
  Boutbro hits a node he stands by. Farmingbed pays 5% of a fish's worth when it is fished out.
- **Every other vessel mines by hand.** The stock melee and ranged AIs (`Entity_Ai_Mob_Combat`, `Mob_Ranged`) list
  `Resource` among their targets. A vessel standing idle by a node hits it at its Attackspeed: 0.5, or 0.6 for Spool.
  That is **120 a Tide on a Red node and 1,140 on the Opal**. Any idle melee unit beside a node does the same: Fie's
  knights at 1 a second, Spool's Stitched at 0.8. Yossele's laser mining ore (the 2026-10-28 default) is this same
  stock behaviour.
- **Yossele's Sapling and Spool's Loom** hit with Damage 1, so a node pays every 5 of their hits. The Sapling hits
  every node in its Range once every 4 s (Attackspeed 0.25), 10% faster a Growth stage (up to 5) and 80% faster with
  Focused Roots (one node only). Fully grown, each payout drops a fruit worth Attackspeed × Damage × 15 of each colour
  (scope `07`), if someone picks it up. The Loom hits its nearest node every 2.5 s. Its Seam is a stock melee unit beside the node and
  hits 0.8 a second. That is what the code does, but it has not been seen in game (Spool `IMPLEMENTATION_NOTES.md` 6).

## 2. What a map hands out

**Kills** pay only Red, and the same on every map (Downpour; a map's difficulty scales nothing): 885 over Tides 1-15
(table in `BALANCE_2026-11-03.md` §3). The start is 50 of each colour.

**Nodes.** The play test's ground dumps (2026-10-26) list every tile no building and no foe can take. Matching those
tiles to the nodes the bot named gives:

| Map | Nodes (distance from the core) | Anything else |
|---|---|---|
| Purlest (`3_Opal`) | the **Opal**, 11 tiles out. About 47.5 a payout (calibrated, 9.5 a hit), one colour a Tide in turn: Red, Blue, all, Yellow | nothing: the 5 blocked tiles are the core, the Opal and three enemy cores |
| Woozy | **Red** 3.6, **Blue** 4.0, **Yellow** 8.9, **Purple** 13.0 tiles out | five Tower Barrels (map buff barrels) |
| The Hunt | **Orange**, 5.8 tiles out. It paid +3 Red and +5 Yellow, read twice off the wallet in RogueLab round 7 | an aura-beast Den: 3 beasts at the start, at most 4. Beasts are tagged `Resource` and pay on death, amount unknown. 3 blocked tiles not identified |

Blue and Yellow nodes are assumed to pay 5 like Red. Purple is assumed shaped like Orange (3 + 5). Only these three
maps have a dump. The world map has six maps and the mode draws from 45 (M8 in §7 lists the rest).

## 3. Each base vessel's gatherer

One building beside one node, aura a Tide (a 180 s Tide and its 60 s break), at Tide 10: path bought, Sapling grown.
"Hits a payout" is on a Red node. Payback is the building and its path's cost divided by its take on a Red node.

| Vessel | Gatherer and path | Cost R/B/Y | Hits a payout | Red node | Orange | Opal | Tides to pay back |
|---|---|---|---|---|---|---|---|
| Smacky | Mining Hut | 0/0/35 | 10 | 18 | 28 | 168 | 2.0 |
| Smacky | Hut: Mining Operation, Ore Scraps | 75/0/190 | 10 | 106 | 198 | 547 | 2.5 |
| Smacky | Hut: Mining Operation, Mining Union | 0/0/165 | 10 | 53 | 85 | 503 | 3.1 |
| Sno Sno | Sno Box | 0/10/25 | 13 | 18 | 30 | 175 | 1.9 |
| Sno Sno | Sno Box: Slush | 0/10/60 | 10 | 48 | 77 | 456 | 1.5 |
| Sno Sno | Box: Slush, Soft Sno (snow piled up / kept off) | 0/35/310 | 10 | 72 / 198 | 115 / 317 | 684 / 1881 | 4.8 / 1.7 |
| Fie | Silo Pipes | 0/0/35 | 18 | 17 | 27 | 158 | 2.1 |
| Fie | Silo: High Flow | 0/0/65 | 10 | 30 | 48 | 285 | 2.2 |
| Fie | Silo: High Flow, Reroute, Quick Construct | 0/0/190 | 10 | 45 | 72 | 428 | 4.2 |
| Buster | Checkpoint (a dog lap every 10 s) | 0/0/50 | 5 | 36 | 58 | 342 | 1.4 |
| Buster | Checkpoint: Lap 1, Lap 2 | 0/0/205 | 5 | 81 | 130 | 770 | 2.5 |
| Boutman | none until Fishbender | | | 0 | 0 | 0 | |
| **Yossele** | Pachira Sapling (new / fully grown) | 0/0/35 | 5 | **60 / 90** | 96 / 144 | 570 / 855 | 0.4 |
| **Yossele** | Sapling: Focused Roots | 0/0/65 | 5 | **138** | 221 | 1311 | 0.5 |
| Yossele | Sapling: Focused Roots, every fruit picked up | 0/0/65 | 5 | **883** | 966 | 2056 | 0.1 |
| **Spool** | Loom | 0/0/40 | 5 | **96** | 154 | 912 | 0.4 |
| **Spool** | Loom: Seam Allowance | 0/0/70 | 5 | **288** | 461 | 2736 | 0.2 |

**Building for building:**

- A Sapling is about **3-5× a Mining Hut**, a Sno Box or a Silo, at the same Yellow 35.
- A Loom is about **5×**.
- With their cheap tier-2 upgrades, Focused Roots and Seam Allowance, they are 1.3× and 2.7× the best stock path
  through tier 3 (Smacky's Ore Scraps, 106), at a third of its price.
- The fruit is the biggest outlier. Every fruit is 27 aura (9 of each colour) against the node's own 5.

## 4. A map, Tides 1-15

The gatherers' setup:

- one building beside every node within 9 tiles of the core, or beside the nearest node if none is that close;
- built in the opening break;
- the vessel's best mining path through tier 3 bought at Tide 3.

"By hand" is the vessel hitting the nearest node in every break. Aura is in all colours, with Red in brackets.

| | Purlest | Woozy | The Hunt |
|---|---|---|---|
| Nodes in reach | Opal (11 tiles out) | Red, Blue, Yellow | Orange |
| Start | 150 (50) | 150 (50) | 150 (50) |
| Kills | 885 (885) | 885 (885) | 885 (885) |
| Smacky's gatherers | 7437 (2479) | 4232 (1411) | 2624 (1213) |
| Sno Sno's | 6268 (2089) | 1979 (660) | 1056 (396) |
| Fie's | 4012 (1337) | 1267 (422) | 676 (253) |
| Buster's | 10666 (3555) | 3368 (1123) | 1796 (674) |
| Boutman's | 0 | 0 | 0 |
| **Base vessels' average** (Smacky, Sno Sno, Fie, Buster) | **7096** (2365) | **2712** (904) | **1538** (634) |
| **Average aura a map** (start + kills + that average) | **8131** | **3747** | **2573** |
| By hand in every break, on top | 4204 (1401) | 442 (442) | 708 (266) |
| Yossele's gatherers | 17862 (5954) | 5641 (1880) | 3008 (1128) |
| Spool's gatherers | 37335 (12445) | 11790 (3930) | 6288 (2358) |

**The average aura a map is about 4,800** across the three maps, for a base vessel without Boutman:

- 150 from the start;
- 885 from kills, all of it Red;
- about 3,800 from its gatherers, 1,300 of it Red.

Mining by hand in every break adds about 1,800 more. **Red stays the scarce colour.** Only kills and a third of the
ore pay it.

| Map | Base: lowest / average / highest | Yossele | × average | Spool | × average |
|---|---|---|---|---|---|
| Purlest | 4012 / 7096 / 10666 | 17862 | 2.5 | 37335 | 5.3 |
| Woozy | 1267 / 2712 / 4232 | 5641 | 2.1 | 11790 | 4.3 |
| The Hunt | 676 / 1538 / 2624 | 3008 | 2.0 | 6288 | 4.1 |

With P5 (Sapling 0.15), Yossele's gatherers come to 10717 / 3384 / 1805: **1.5 / 1.2 / 1.2×** the average, inside the
base range (at its top on Purlest). A Loom at 0.15 with a half-strength Seam would put Spool at 1.2-1.6× too, but §5
shows Spool cannot afford it.

## 5. The simulator check

These runs use the simulator in `balance_2026-11-03/` with the mode's 60 s breaks, the smart player, no Cardinals and
60 runs each (`experiments/exp_J.json`). Two new settings come from this report:

- `hut_code`: Smacky's miner at the code's 6.8 s round trip, instead of the 4 s the first study used.
- `sapling_aspd` / `loom_aspd`: the Sapling's and the Loom's speed.

| Setup | Purlest | Woozy | The Hunt |
|---|---|---|---|
| Smacky, Hut as in the first study | wins 100% | lost T12 (97%) | lost T6 (53%), T5 |
| **Smacky, Hut from the code** | wins 100% | **lost T10 (87%)** | **lost T5 (93%)** |
| Yossele as built | wins 100% | **wins 100%** | lost T6 (52%), T7-9 |
| **Yossele, Sapling 0.15 (P5)** | wins 100% | wins 22%, else lost T12-14 | lost T5 (78%), T6 |
| Yossele, Sapling 0.10 | wins 100% | lost T10 (47%), T11 (38%) | lost T5 (95%) |
| Spool as built | lost T11 (97%) | lost T10 (63%), T8-9 | lost T2 (82%) |
| Spool, Loom 0.15 | lost T11 (53%), T10 (47%) | **lost T3 or T5 (47% each)** | lost T2 (83%) |
| Spool with P1-P3 | **wins 100%** | lost T11 (100%) | |
| Spool with P1-P3, Loom 0.15 | lost T12 (98%) | lost T5-6 (83%) | |

What it says:

- **Smacky was too strong as a yardstick on ore-poor maps.** Its Hut mined 1.7× the game's. The first study's "Smacky
  loses Woozy at Tide 12" is Tide 10 with the code's Hut. Its Purlest win stands.
- **Yossele as built is well above Smacky** on Woozy (wins against Tide 10) and The Hunt (Tide 6 against 5). The
  Sapling explains most of the gap.
- **With P5 (0.15), Yossele still wins Purlest and beats Smacky on Woozy** (it lasts to Tide 12-14 and wins one run in
  five), and it matches Smacky on The Hunt.
- **At 0.10 Yossele is level with Smacky.** That is the floor. Below it, Yossele falls behind the stock vessels.
- **Spool's Loom is load-bearing.** Kills pay only Red, and the Pin Cushion costs Blue 20. On a map without Purlest's
  Opal, the Loom is the only Blue Spool gets. At 0.15 the wall can't be rebuilt and Spool loses Woozy at Tide 3-5,
  five to seven Tides earlier than as built. The Loom's 5× is the price of a Blue-only wall, so it should stay. The scope
  line that says it mines "like Smacky's Mining Hut" should change to what is built (P7).
- **The Seam is a different case.** At 288 a Tide it is the largest stock-or-built gatherer path, and no simulated
  player buys it. If the PC round shows it really mines at 0.8 a second (M10), it is a candidate for a cut. A cut would
  be Damage 0.5 against ore, to mine like Smacky's miner as the scope's own default says. There is no proposal until
  it has been seen.

## 6. P1-P3 revisited

They stand unchanged. The baseline changes Spool's income picture, but not the gap P1-P3 close. On Purlest, Spool
already mines 3× Smacky on the Opal (4,400 Red against 1,400 with the code's Hut), ends with 3,900 Yellow unspent, and still loses at Tide 11 to the Maul's slam and the
Tempered Aegis. More or less ore does not move that Tide.

With the corrected Smacky, Spool as built is level with Smacky on Woozy (Tide 10 each), and P1-P3 put it a Tide ahead
(Tide 11). On Purlest, P1-P3 are what turn a Tide-11 loss into a win, where Smacky and Yossele both win. "Wall first"
(P4) is unchanged. Spool's first Tides are still the Pin Cushion wall, and §5 shows the wall is also where its Blue
goes.

## 7. What this leans on, and the PC checks that settle it

Read from code but not seen in game:

- the miner's 6.8 s round trip;
- the Box's, Silo's and Checkpoint's rates;
- the Seam mining;
- every vessel mining by hand.

Read once:

- the Opal's 47.5 a payout;
- the Orange node's 3 + 5.

Assumed:

- the Blue, Yellow and Purple nodes' payouts;
- a dog lap every 10 s through a Checkpoint;
- the Hut built beside its node.

Left out:

- The Hunt's aura beasts;
- Rich Sno, Circular Routing, Hyper Pipes and Tune Up (personal income from tier 3 up);
- every Cardinal and Direction.

Four measurements belong in the next PC round. They join §9 of `BALANCE_2026-11-03.md` and use RogueLab or the vessels'
debug play harness:

| # | What | How | Expected here |
|---|---|---|---|
| M8 | A node census | On each of the six world-map maps (and a sample of the mode's pool), list every `Resource`-tagged entity: name, tile, `Healthresource`, `Health`, each `...auramine` stat, and the Opal's `Regular` and `All` | Purlest 1, Woozy 4, The Hunt 1 + beasts; Red/Blue/Yellow 4 and 5; Opal about 47 |
| M9 | Stock gatherers' speed | A Mining Hut, Sno Box, Silo Pipes and Checkpoint (dogs running), each alone beside a `Resource_Red` for 3 Tides; and Smacky standing idle by one. Count payouts | 18, 18, 17, 36 and 120 a Tide (§3) |
| M10 | Yossele's and Spool's | The same for a Sapling (new and grown), a Loom, and a Loom with its Seam; check that the Seam hits the node at all | 60 / 90, 96, 288 |
| M11 | The Hunt's beasts | Kill an `Mob_Aurabeast` and a `Mob_Largeaurabeast` with Yossele's laser; log the wallet before and after | unknown |

After M8-M11, re-run `aura.py` and `exp_J` with the measured numbers, and write the picks into scopes `07` and `08`,
Yossele's and Spool's `DESIGN.md` and, for P6, the Sapling's fruit line.

## 8. Files

- `balance_2026-11-03/aura.py`: the baseline in §3-4 (`python3 aura.py` prints the tables and writes
  `results/aura_baseline.json`). Its docstring lists the code it was read from.
- `balance_2026-11-03/sim.py`: four new settings (`sapling_aspd`, `loom_aspd`, `seam_aspd`/`seam_hit`, `hut_code`).
  Their defaults are the numbers the first study used, so its results A-H reproduce unchanged.
- `balance_2026-11-03/experiments/exp_J.json`, `results/res_J.json`: §5.
- Nothing else changed: no mod, no scope file, no `DESIGN.md`.
