# Argo review 2026-11-04: are the numbers balanced, and does the kit reward skill?

> Frozen as written. Asked by Dimitri (2026-11-04 04:42Z): "Do a review on Argo's current kit. Is it mathematically
> balanced? Do the mechanics work well together in a way that has skill expression? Make any changes deemed
> necessary, then update the scope editor artifact." Cloud only: no PC round, no mod built. The changes are in scope
> `09` as Claude `~defaults~`, each line ending with a "Review 2026-11-04: was ..." note; nothing in the mod changed.
> The project-files copy of the scope is not overwritten (that needs Dimitri's word).

**In one paragraph.** Argo is close to balanced, but not for the reason its sheet gives. Two things in the code make
it stronger than its numbers say: a slow stacks with its own re-ticks, so a "30%" pool slows 60% and an upgraded one
nearly stops foes, and the Drill Rig mines like Yossele's Sapling, about four times a Mining Hut. With the slows
fixed to what the sheet says, the simulator puts Argo between Smacky and Yossele: it wins Purlest, falls at Tide 12
on Woozy and The Hunt (Smacky Tide 12 and 11, Yossele a win and Tide 12). As built, it wins Woozy every time. The kit
has real skill in it: **where the Tremor goes is worth seven Tides on Woozy**, and how many Drills to sink is worth a
win. Its weak spots are the "pin" theme, which four upgrades read and almost nothing makes, a handful of upgrades that
do less than their price (Abyss, Gusher, Mantle Tap's ore, Trench, Double Barrel, Wide Bore's cone), and Blue: every
control piece costs Blue, so on maps without a Blue node the crew and the water upgrades never arrive. Fourteen
changes in scope `09` fix those; two of them are code fixes the sheet already asked for.

## 1. How it was checked

- **Read** the built Argo 2.0 (`TDS_Mod/ModSource/Argo/Code`, `02_Data.cs` and the brains), its `DESIGN.md`, scope
  `09` and the kit's engine notes (`docs/01` §9, `ENGINE_STUDY_2026-10-28.md`). The 2026-10-12 code review
  (project files `research/ARGO_CODE_REVIEW_2026-10-12.md`) is about code size, not balance; nothing in it changes a
  number, and its cleanup is still Dimitri's call.
- **Hand numbers** (`balance_argo_2026-11-04/argo_math.py`): mining by Fathom, damage per aura against the stock
  towers, pellets in a lane, the real slows, pins, and what each weak upgrade pays.
- **The balance simulator** with Argo added (`balance_argo_2026-11-04/`, a copy of the 2026-11-03 re-run's model,
  same maps, nodes, Tide table and 60 s breaks). 84 setups, 2,940 runs. Smacky and Yossele come from the re-run's
  `res_L.json` (same model). What the model can and cannot tell is in §3.

## 2. The numbers, building by building

### The vessel

Health 65, Damage 4, Attackspeed 0.4, a 60% cleave on a second foe within 1.25 tiles: 1.6 damage a second on one
foe, 2.6 on two (Smacky 2.0). Inside the band (`docs/02` §2) and slower than Smacky (2.25 against 3). **No change.**

### Scatter Cannon (Red 30) and Harpoon Gun (Red 35)

| Building | one target | a packed crowd | crowd per 10 aura |
| --- | --- | --- | --- |
| Archer Tower (R25) | 2.80 | 2.80 | 1.12 |
| Bomb Tower (R25 B25) | 1.05 | 4.20 | 0.84 |
| Scatter Cannon (R30), 6 pellets | 0.98 | 4.50 | 1.50 |
| Scatter on slowed foes (+50%) | 1.46 | 6.75 | 2.25 |
| Harpoon Gun (R35), pierce 3 | 1.75 | 5.25 | 1.50 |

- The Scatter is a crowd weapon that is weak on a lone foe, as the sheet says. Its +50% bonus is the problem: in
  Argo's water and Tremor almost every foe is slowed, so the bonus is always on and asks nothing of the player.
- **Wide Bore's wider cone costs hits.** In a lane 1.5 tiles wide, a 75° cone throws more pellets off the lane than
  its two new pellets add: at 3 tiles, 2.6 pellets land against the base gun's 3.2. Only on an open merge does it gain.

  | distance | 50°, 6 pellets | 75° (Wide Bore), 8 | 60°, 8 (the change) |
  | --- | --- | --- | --- |
  | 2 tiles | 4.8 | 3.9 | 5.2 |
  | 3 tiles | 3.2 | 2.6 | 3.5 |
  | 4 tiles | 2.4 | 2.0 | 2.6 |

- **Double Barrel** stores one volley after a quiet spell: about one extra volley a Tide, for the same Red 30 as Wide
  Bore.
- The Harpoon is slow on one foe (1.75 against the Archer's 2.8) and pays it back in pierce and range. Both paths are
  sound: the damage path ends in Whaler (×2 on big pools) and the Lance; the control path in Twin Line and Undertow.
  **No change** besides the Reel hint (§4).

### Deep Water (Blue 20, FLOOD Blue 15 a pool)

- **The real slow is twice the written one.** Every 0.25 s the water slows each foe in it again for 0.5 s, and
  `ArgoUtil.Slow` adds a new Speed Multi mod each time; Multi mods sum (kit `docs/17`, verified in PC round 7), so a
  foe always carries two. The Tremor re-ticks every 0.5 s for 0.75 s: one or two at once.

  | Source | written | real |
  | --- | --- | --- |
  | Deep Water | 30% | 60% |
  | Deep Water, Undercurrent and two levels | 45% | 90% (the 10% floor) |
  | Tremor, Fathom 1 | 20% | 20-40% |
  | Tremor, Fathom 5 | 36% | 36-72% |
  | Tremor, Fathom 5, Seismic, Old Salts | 56% | 56-90% |

  A Dagger crossing six pools spends 8.6 s in them as written, 15 s as coded, 60 s with Undercurrent. The 1.3 notes
  saw the Tremor's overlap and floored it at 10%; nobody measured the pool.
- **Abyss** (Blue 260) adds a free pool to each FLOOD, worth Blue 15: it pays back after 17 FLOODs.
- **Trench** (Blue 3000) adds one pool a Tide and holds one foe for 1 s every 6 s: under 1% of the Tide's foes.
- **Kelp Bed's catch and Trench's drag hold foes but are not pins** (`ArgoUtil.Root`, not `ArgoStatus.Pinned`), so
  they feed nothing that reads pins (below).

### Pins: read four times, made almost nowhere

The Scatter's bonus, Salt Sting, Bedrock and "held twice as long in Deep Water" all read **Pinned**. Only two things
pin: Anchor Line (the first foe a harpoon hits, every 2.86 s, for 1 s, 2 s in water) and Mantle Tap's quake (once a
Tide, Yellow 3000 at tier 5). So Bedrock (Blue 80 + Red 30) does 0.5-1 damage a second, and Salt Sting (Blue 70 +
Red 30) 0.35. The theme the sheet builds the kit around ("slow foes, pin them, reel them in, and blast what's stuck")
has almost nothing at its middle step.

### Drill Rig (Yellow 35)

| Fathom | Damage | strikes/s | strikes a payout | aura a Tide on a Red node |
| --- | --- | --- | --- | --- |
| 1 | 0.50 | 0.25 | 10 | 30 |
| 2 | 0.88 | 0.28 | 7 | 47 |
| 3 | 1.25 | 0.30 | 5 | 72 |
| 5 | 2.00 | 0.35 | 5 | 84 |
| 7 (Deep Core) | 2.75 | 0.40 | 5 | 96 |

- A node takes at most one share of ore a strike (`docs/01` §9), and the Drill passes no `Damageresource` (Yossele's
  Sapling, Spool's Loom and Uloe do). So **the Fathoms' "+75% ore" stops at Fathom 3**, and **Mantle Tap's "×2 ore"
  mines nothing**. The mining that is left is still large: 30 to 84 aura a Tide, against a Mining Hut's 18 and a
  Sapling's 60-90. Built in the opening break, one Drill earns about 1,235 aura a map on a Red node (a Hut 288).
- **Gusher** (Yellow 275): 12 strikes a Tide, 12 aura on a Red node, 23 Tides to pay back on a 15-Tide map.
- **Deep Core** (Yellow 120): +12 aura a Tide, plus 0.8 tiles and 8% of Tremor. Fair as a stepping stone.
- The Tremor itself is the best control in the kit: 2 tiles at Fathom 1, 3.6 at Fathom 5, aimed anywhere within 5
  tiles, and it grows for free.

### Miner's Base (Blue 35, limit 1)

- A Naut adds 20% attack speed to one tower: two Nauts are worth about 0.4 of a Scatter for Blue 35, fair.
- **Old Salts** (Blue 30) is the strongest tier-2 in the kit (+2 pellets on each tuned Scatter, Wide Bore's whole
  effect on two guns at once). It is the crew's identity; **no change**.
- **Full Crew** (Blue 2500 + Yellow 500) is a modest capstone: +20% more attack speed and diving Nauts. Left as is.
- **Blue 35 is the problem.** With Deep Water's Blue 20, it is more than the start's 50 Blue, and on Woozy (Yellow or
  Red node first) and The Hunt (no Blue node) Argo never earns another Blue. In the simulator the crew never came on
  either map, and Argo spent 40-50 Blue in a whole map while 1,000-2,000 Yellow sat unspent.

## 3. The simulator

Smart play: a Scatter by every core first, Deep Water flooded to six pools, a Drill by the richest node aimed at the
merge (when the node is within 7 tiles of it), the Miner's Base tuning the busiest guns, Harpoons on the control path,
then the cheapest upgrade it can afford. 30 runs a setup; "T12" is the Tide a run was lost on, "win" survived Tide 15.

| | Purlest | Woozy, Yellow node first | Woozy, Red first | The Hunt |
| --- | --- | --- | --- | --- |
| Smacky, walls shut (re-run) | win | T12 (best order) | | T10.8 |
| Smacky, walls walked round | win | T6 | | T10.1 |
| Yossele (re-run) | win | **win** (best order) | | T11.9 |
| **Argo as built** (slows stack) | win | **win 100%** | T13.6 (27% win) | T13.2 (13% win) |
| **Argo as written** (one slow a source) | win | T12.2 | T12.0 | T12.3 |
| as written, Tremor never on the lane | win | **T4.9** | T9.1 | T11.9 |
| as written, a Drill by all three nodes | win | **win 100%** | T12.0 | **win 93%** |
| as written, damage Harpoons | win | T13.2 (30% win) | T12.0 | T12.3 |
| as written, the Drill's control path (Seismic, Bedrock, Fault Line) | win | T8.7 | T12.0 | T10.4 |
| **with the review's changes** | win | T12.1 | T11.7 | **T13.9 (43% win)** |

Mining (aura, whole map): Argo as written 5,500 on Woozy and 6,500 on The Hunt with two Drills (Yossele 4,700 and
7,300, Smacky 770 and 1,300); 42,600 on Purlest (Yossele 48,000, Smacky 10,000).

What it says:

- **The stacking slows are worth one to four Tides.** Fixed, Argo sits between Smacky and Yossele on every map, which
  is where a no-wall control vessel should be. No number needs raising to make up for the fix.
- **The Drill is Argo's economy and its best control at once.** A third Drill wins Woozy and The Hunt; giving a Drill
  the control path instead of Pay Dirt, Deep Core and Gusher costs three Tides on Woozy. One Drill cannot do both, so
  the choice is real, and two Drills on two paths is the skilled answer.
- **The changes help where Blue is short.** On The Hunt the Base's new price brings the crew (T12.3 to T13.9); on
  Woozy, with the Yellow node first, Argo still has no Blue for Kelp Bed or Anchor Line, so it does not move.
- **What the model can't tell:** Nauts, reel direction, Anchor Line and the pin changes left the results unchanged,
  because the economy and the Tremor decide the runs before those upgrades come in, and the policy rarely reaches
  them on Blue-poor maps. Their value here is the hand numbers in §2. (The first pass, `res_A.json`, tried reels
  pulled toward the core and Nauts never sent: no change either, and two pools a quarter of a Tide, for the same reason.)
- The runs used Mantle Tap at ×2 speed; the sheet now says ×1.5. A four-seed check on Purlest: ×2 mines 69,000, ×1.5
  58,000, as built 42,600. The Opal makes every gatherer rich; on a Red node Mantle Tap's Drill earns 126 a Tide.

## 4. Skill expression

Where a player's choices show, best first:

1. **AIM** (where the Tremor goes): the biggest single skill in the kit. Off the lane, Argo loses Woozy at Tide 5
   instead of 12. The Tremor touching the water spreads through every pool, so AIM and FLOOD are one decision.
2. **Which Drills, and on which path**: one by every node wins maps; an economy Drill and a control Drill side by
   side is the skilled build.
3. **FLOOD's shape**: where the pools run decides what the Tremor can shake and where the Scatters sit.
4. **Reel direction**: a reel pulls foes toward the gun. A Reel gun beside the core pulls foes toward the core; one
   up the lane or beside it drags them back or off the path. Placing the gun is the skill, and nothing tells the player
   that, so the journal gets a hint.
5. **The crew**: sending Port and Starboard to the gun under pressure, a teammate's included.
6. **Anchor beside a building**: a pin beside any tower becomes a stun.

Where the kit asks nothing: the Scatter's always-on bonus (now split), Double Barrel (now a real opener), and the
pin readers that had no pins to read (now fed by Kelp Bed and Trench).

## 5. Changes made in scope `09`

Every changed line keeps a `~default~` and ends with a "Review 2026-11-04: was ..." note.

| # | Where | Was | Now | Why |
| --- | --- | --- | --- | --- |
| 1 | Vessel stats, new line "Slows" | (re-ticks stack in code) | one slow per source on a foe, renewed, never stacked with itself; sources add up, floor 10% | the sheet's numbers become the real ones; **code fix** |
| 2 | Vessel stats, new line "Pins" | Anchor Line and Mantle Tap pin | Anchor Line, Kelp Bed, Trench and Mantle Tap pin | gives Salt Sting, Bedrock and the Scatter's pin bonus something to read; **code fix** (`ArgoUtil.Pin`) |
| 3 | Scatter, debuff bonus | +50% slowed or pinned | +50% slowed, **+100% pinned** | the slow bonus is always on; pins pay for the play that makes them |
| 4 | Wide Bore | +2 pellets, cone +25° | +2 pellets, **cone +10°** | the wide cone lost 20% of hits on a lane |
| 5 | Double Barrel | stores 1 volley when idle | stores a volley **every 2 s, up to 3** | an opener for a Scatter behind the water |
| 6 | Kelp Bed | holds 0.75 s on entering | **pins 0.5 s** (1 s in the water) | a pin source on every foe that wades in |
| 7 | Abyss | a free pool a FLOOD | a free pool a FLOOD, **slow +10%** | paid back after 17 FLOODs |
| 8 | Trench | 1 pool a Tide, a 1 s hold every 6 s | **2 pools a Tide, a pin every 3 s** (2 s in the water) | the tier-5 did less than a tier-3 |
| 9 | Seismic | Blue 25 + Yellow 10 | **Yellow 35** | a Blue-free control path, and a Yellow sink |
| 10 | Bedrock | Blue 80 + Red 30 | **Yellow 80 + Red 30** | same |
| 11 | Gusher | 12 strikes a Tide | **4 strikes a Fathom** (20 at 5, 28 at 7) | 12 aura a Tide on Yellow 275; now pays back in under 14 Tides, and rewards Deep Core |
| 12 | Fault Line | Blue 300 | **Yellow 300** | as 9 |
| 13 | Mantle Tap | ore ×2, a quake once a Tide | **strikes ×1.5, a quake every 20 s** | the ×2 mined nothing; one quake a Tide pinned under 1% of the Tide |
| 14 | Miner's Base | Blue 35 | **Blue 20 + Yellow 15** | with Deep Water it fits the start's 50 Blue, so the crew comes on Blue-poor maps |

Two notes with no number changed: the Fathom line now says that a strike breaks at most one share of ore (Fathom 3
on), and Reel's line asks the journal for "Place it beside or up the lane".

Questions added to scope `09` (also PLAN Open questions 8): take, change or drop each change; and **the Drill's
mining against Yossele's P5**. The Drill mines what a Sapling does. If Dimitri takes P5 (Sapling Attackspeed 0.15,
`AURA_BASELINE_2026-11-03.md`), the Drill should come down with it (Attackspeed 0.25 to 0.2). Otherwise it stays.

## 6. Not changed, and why

Vessel stats; the Scatter's and the Harpoon's base stats; Grapeshot, Pressure Rounds, Broadside, Ironclad, Barnacle
Shot; the whole Harpoon tree (Tether, Anchor Line, Reel, Twin Line, Undertow, Long Line, Skewer, Whaler, Leviathan
Lance); Deep Water's base slow, Undercurrent, Brine, Salt Sting (it works once pins come), Crushing Depth; the Drill's
base, its Fathom curve, Pay Dirt and Deep Core; the Nauts, Flippers, Old Salts, Overhaul, Calibrate, Third Mate,
Bulkhead and Full Crew; the four Argo Directions in scope `11`.

## 7. For the next Argo build

Code (2.0.x, xhigh, a PC round): renew one slow per source and foe in `ArgoUtil.Slow` (Deep Water, Tremor, Tether,
Barnacle); `ArgoWater` pins with `ArgoUtil.Pin` for Kelp Bed and Trench; split `Debuffbonus` into slowed and pinned;
Mantle Tap's `Damage_Pmulti` becomes an Attackspeed ×1.5 and its quake a 20 s timer; Gusher reads the Fathom; the
prices. The self-test checks that read the old numbers move with them. `TDS_Mod/ModSource/Argo/IMPLEMENTATION_NOTES.md`
has the list. PC checks worth adding: a foe's real speed in a pool (60% today, 30% after), and a Kelp-pinned foe
taking Salt Sting's double brine.

## 8. Files

- `docs/scope/09_Vessel_Argo.txt`: the changes (§5).
- `docs/reports/balance_argo_2026-11-04/`: the simulator with Argo, `argo_math.py`, experiments and results (its
  `README.md`).
- `TDS_Mod/ModSource/Argo/DESIGN.md` (a pointer), `IMPLEMENTATION_NOTES.md` (the code list).
