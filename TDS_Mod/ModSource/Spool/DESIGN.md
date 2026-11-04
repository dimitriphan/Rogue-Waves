# Spool: design (v0.1.0-dev, 2026-10-26)

Rogue Waves' Spool is **built from the scope text**: Dimitri's 2026-10-23 sheet, `docs/scope/08_Vessel_Spool.txt` as
PR #40 left it (Yarn Stash the one supply base, Thread Head bought from the build menu with no limit, Patch up to 500).
Every number the sheet gives is used as written. A number written `~like this~` in the sheet is a default Claude picked
there and is used too; anything the sheet leaves open is a **default picked here**, marked *(default)* below and
listed in "Defaults picked here" at the end. Checked against `TDS_Vessel_Creator/docs/02` and `docs/12` (minion vessels).
Status: **self-test passes in game** (PC round 1c 120/0, 2026-10-26; 133/0 on 2026-11-04); balance P1-P7 wait on a
hand try of a Pin Cushion wall; temporary art (`IMPLEMENTATION_NOTES.md`, `Previews/0.1/ASSETS.md`).

## Identity

- **Name ▪ title ▪ difficulty:** Spool, "Crafty Crafted Crafter" ▪ Advanced (sheet).
- **One-line fantasy:** a disposable stitched army. Foes drop Thread, Spool and its Thread Heads haul it home, and the
  Cookie Tins turn it into Crafted that run where you point.
- **Silhouette:** a hooded purple plush whose face is a stitched opening, yellow eyes looking out, a pink tongue,
  white gloves, and a severed arm on the floor with cotton stuffing coming out of the wound (not a tail)
  (`docs/STYLE.md`).
- **Sprite baseline:** Dimitri's reshade (`concepts/vessels_2026-10-09/v3_flat/game_size/spool_reshade.png`, equal to
  the `spool_reshade.aseprite` he uploaded) and the back view in `concepts/vessels_2026-10-09/back/`. Every 32 px frame is
  his pixels moved or added to, never redrawn or recoloured; the walk is the hinged-hood cycle he asked for
  (`concepts/walk_2026-10-12/`). Reference base vessel: Fie (`docs/STYLE.md`).
- **Palette:** the sprite's own: plush `9363AE` / `784B94`, hollow `41174F`, stitches `E4DEDA` / `9A938E`, eyes `E6D85A`,
  tongue `E58CC0` / `C96CA6`, gloves `F6F3F8` / `D8D1E2`. Accent (hero `Color`, buttons, journal): plush purple `9363AE`.
- **Towers:** low-poly sleek, never voxel. Motif: stitches, pins and tins (sheet default): a wicker basket of yarn,
  a round blue cookie tin, a tomato pin cushion bristling with pins, a wooden frame loom.
- **Lore:** none yet (sheet: "Lore comes once the vessel is finalized"). The journal shows a placeholder line.

## The vessel

| Stat | Value | Band (docs/02) |
|---|---|---|
| Attack | melee (Mob_Combat, like Smacky) | |
| Health | 45 | 25-65 |
| Health regen | 0 | |
| Speed | 2.75 | 2-3.5 |
| Damage | 2.5 (low: the Crafted do the damage) | 1.5-5 |
| Range | 1.5 | 1.5-2 |
| Attackspeed | 0.6 | 0.4-0.75 |
| Buildrange | 2.5 | 2-3 |
| Sellrate | 0.5 | 0.5-0.75 |

- **Display_Desc:** "Crafty melee, commands a stitched army" (sheet).
- **Special rule** ("Hand Stitched", tooltip): "Melee, foes drop Thread that Spool or a Thread Head carries to the Yarn
  Stash, Crafted cost Thread, PATCH mends a unit or building" (sheet).
- **PATCH** (ability, keys 6 / 7, Onpressed, priority 6, from the start): a picker. Press it, point at one of your own
  units or buildings, click: 20 Thread taken from the Yarn Stash heals it by 20% of its maximum health, at most 500.
  Range 4 tiles from Spool, cooldown 5 s (the button greys out while it cools). The hotkey falls back to the thing under
  the cursor, else the most hurt of yours in range *(default)*. Refused, with an owner-only line, when nothing is hurt,
  out of reach, cooling down, or the Stash is short. Spool itself is not a target (the sheet says units or buildings).

### Thread

- Kept **per player on the host**, in two places: the **Yarn Stash** (what buildings and PATCH spend) and **carried**
  (Spool and each Thread Head carry up to 20; the sheet's default for Spool).
- **Start:** 10 in the Stash each map, the moment it is placed. **Cap:** 50 in the Stash, +25 per Stash upgrade (none
  yet). **Between maps:** resets. **On Spool's death:** what it carries is kept.
- **Drops:** a foe that falls leaves Thread on the ground where it fell: 1 Thread, 3 for a large foe (base health 40 or
  more), 2 more for an elite (any `Enemy_*` modifier), at most 5 *(default; the sheet says "up to 5")*. A drop lasts
  15 s, then it is gone. Drops land only while some player is Spool; one drop per foe, whoever picks it up first.
- **Pickup:** Spool by walking within 1 tile; a Thread Head pulls in everything within 3 tiles. Each pickup flies to the
  carrier like Boutman's fish to the bucket (a tuft and a short hop). A carrier takes only what it has room for.
- **Drop-off:** within 1.5 tiles of the owner's Yarn Stash, carried Thread goes in up to the cap; the rest stays carried.
- **Spent on:** Crafted (the Cookie Tin draws from the Stash wherever it stands) and PATCH.
- **Sold back:** selling a Crafted from the Cookie Tin refunds half its Thread, rounded down, into the Stash; what doesn't
  fit drops on the ground beside it *(default for the overflow)*.
- **Shown:** Spool's panel shows `Carried` and `Stash`; the Yarn Stash shows `Thread`; a Loom shows the Thread it holds;
  a Thread Head shows `Carried`.
- **Performance** (the sheet: "make sure it doesn't lag the game"): ground Thread is not a networked entity. The host
  keeps a list; clients draw a small tuft from one Vis event, fly it away from another, and time it out themselves.
  A new drop within half a tile of another merges into it.

## Buildables

The **Yarn Stash** is placed first, like Boutman's Bouthouse: until it stands, Spool's build menu holds only the Stash.
Once it stands the menu holds the four buildables. `Towerdisplay` (the boat's character screen, models only) shows the
Stash, Cookie Tin, Pin Cushion and Loom; the Thread Head is a unit and has its journal page.

| Name | Id | Role | Template | Cost | Stats | What it does |
|---|---|---|---|---|---|---|
| Yarn Stash | `Tower_Spoolstash` | supply | Tower_Smackyhut | free, limit 1 | Health 100 (+50/level), Thread cap 50 (+25/level) | holds the Thread; drop-off point; Thread Heads respawn here |
| Repurposed Cookie Tin | `Tower_Spooltin` | Towerfighter | Tower_Smackyhut | Red 50 | Health 60 (+25/level), Crafted 4, one every 4 s | stitches Crafted from Stash Thread and sends them to its target spot |
| Thread Head | `Mob_Spoolhead` | unit | Mob_Smackyminer | Red 40, no limit | Health 60, Damage 2, Range 1.5, Attackspeed 0.6, Speed 2.25, carries 20, collects 3 tiles, leash 3, respawn 15 s | guards a spot, collects Thread, carries it home |
| Pin Cushion | `Tower_Spoolcushion` | defence | Tower_Smackywall | Blue 20 | Health 100 (+40/level), reflect 20% melee, 1 Thread per 25 damage taken | the wall: pricks melee attackers, spins Thread when hit |
| Loom | `Tower_Spoolloom` | Gatherer | Tower_Smackyhut | Yellow 40 | Health 40 (+20/level), Range 2.5, Damage 1, Attackspeed 0.4 | mines ore in reach, spins 1 Thread every 6 s up to 10 held for pickup |

- **Yarn Stash** *(defaults)*: sellable for nothing (to move it; its Thread is kept for the next one). Destroyed, its
  Thread spills on the ground around it as drops and the build menu goes back to the Stash (rebuilt free). No upgrades
  yet: the sheet asks Dimitri for them (`! NEEDS YARN STASH UPGRADES !`).
- **Cookie Tin:** "Crafted alive at once" 4 per Tin; one is stitched every 4 s while the Stash has the Thread. Its
  **TARGET** button (the fish Relocate picker, keys Quickaction / 5) picks a walkable tile within 8 tiles that a Crafted
  can reach *(the sheet: "picked like Fie's rally point, on a path within 8 tiles")*; a pin marker (a walk-through trap
  being every player sees) stands there. Until a spot is picked the Crafted guard the Tin itself. New Crafted run to the
  current spot; Crafted already out keep the spot they were sent to until they die or are sold (sheet: "once they're
  set to a location they stay there until death"), so one Tin can hold several spots. Limit none.
- **Thread Head:** bought from the build menu like Boutman's fish and placed on a walkable path tile (the build menu
  places a one-frame token on Buster's walk-through pad block, which the host swaps for the Thread Head: a fish's own
  block would make it block foes like Gubby, which the sheet doesn't ask for). It guards the tile it was placed on;
  **SPOT** (the fish Relocate picker) moves that spot. It pulls in Thread within 3 tiles, fights foes within its leash
  of 3, and **RETURN** sends it to the Stash to drop off and come back (sheet: "without Threaded Delivery you send it back
  yourself"). Killed, it drops what it carried and comes back at the Stash after 15 s with the upgrades it had, then
  walks to its spot. Sold, it is gone (refund like any unit: Red 40 × Sellrate).
- **Pin Cushion:** a wall (blocks like Smacky's Wall). A hit from a melee foe (its `Range` stat 2.25 or less
  *(default test)*) pricks the attacker for 20% of the damage, with kill credit. Every 25 damage it takes spins 1
  Thread, dropped beside it for pickup.
- **Loom:** strikes ore within 2.5 tiles like the Mining Hut's pick (stock payout to its owner), and spins 1 Thread every
  6 s while it holds fewer than 10. Spool picks the held Thread up within 1.5 tiles *(default: the drop-off reach)*, a
  Thread Head within its 3 tiles.

### Crafted (units)

| Crafted | Id | Made by | Attack | Health | Damage | Range | Attackspeed | Speed | Leash | Cost |
|---|---|---|---|---|---|---|---|---|---|---|
| Stitched | `Mob_Spoolstitched` | Cookie Tin | melee | 20 | 2 | 1.5 | 0.8 | 1.8 | 2 from its spot | Thread 10 |
| Dart | `Mob_Spooldart` | Cookie Tin with New Design | ranged, thrown needles | 12 | 2.5 | 3.5 | 0.6 | 1.6 | 1 from its spot | Thread 15 |
| Seam | `Mob_Spoolseam` | Loom with Seam Allowance, 1 per Loom, respawn 10 s | melee, mines | 10 | 1 | 1.5 | 0.8 | 1.5 | 2.5 from its Loom | free |
| Mangled | `Mob_Spoolmangled` | Pin Cushion with Loose Stuffing, 1 per 40 damage taken, at most 3 alive | melee | 15 (45 Overstuffed) | 1.5 | 1.5 | 0.8 | 2 | 2 from its Cushion | free |

- Stock fighter AIs (`Entity_Ai_Mob_Combat`, the Dart `Entity_Ai_Mob_Ranged` with a needle cloned from the archer's
  arrow); the brain only walks them. Idle next to ore, the stock melee AI strikes it, which is how the Seam mines (like
  Smacky's miners).
- Stitched and Darts run to the Tin's spot ignoring foes on the way, then idle there and fight what comes within their
  leash. A Seam fights foes within its leash and otherwise stands at the nearest ore in reach of its Loom. A Mangled
  guards its Cushion.
- Only the Tin's Crafted are sellable (half their Thread back). Seams and Mangled cost nothing and refund nothing.

## Upgrade trees

The sheet's levels 1-3 are the game's tiers 2-4; a building takes one upgrade per level. Every choice of a level leads
to every choice of the next, except where noted. No tier-5 capstones yet (the sheet has none). Costs are the sheet's
defaults.

| Building | Tier | Upgrade | Desc (in-game line) | What it does | Cost | Next |
|---|---|---|---|---|---|---|
| Cookie Tin | 2 | Reuse Reduce Recycle | Fallen Crafted leave Thread behind | a Crafted that is killed drops half its cost, rounded up | Red 30 | Discounted, New Design |
| | 2 | Woolen Lining | Sturdier Crafted | Crafted health +50% | Red 30 | Discounted, New Design |
| | 3 | Discounted Material | Cheaper, faster Stitched | Stitched cost 8 Thread and walk 30% faster | Red 90 | One Last Surprise |
| | 3 | New Design | Stitches Darts that throw needles | the Tin stitches Darts | Red 100 | One Last Surprise |
| | 4 | One Last Surprise | Crafted burst when they fall | a falling Crafted hurts every foe within its attack range for 3 × its damage | Red 400 | |
| Thread Head | 2 | Threaded Delivery | Carries Thread home on its own | full, it walks to the Stash, drops off and returns to its spot | Red 30 | |
| | 2 | Patchwork Job | Mends nearby units with Thread | a **MEND** toggle: 2 health a second to each of your units within 2 tiles, 1 Thread per 5 health | Red 30 | |
| Pin Cushion | 2 | Sharper Pins | Pricks melee attackers harder | reflects 40% of melee damage | Blue 30 | Pin Barrage, Loose Stuffing |
| | 2 | Slow Unravel | Takes half its damage later, spins more Thread | half of each hit now, half over the next 4 s (heals can cancel it); Thread spun ×2 | Blue 30 | Pin Barrage, Loose Stuffing |
| | 3 | Pin Barrage | Pricks ranged attackers too | also reflects 20% of ranged damage | Blue 100 | *(ends: the level-4 upgrades shape Mangled)* |
| | 3 | Loose Stuffing | Spills Mangled when hit | 1 Mangled per 40 damage taken, at most 3 alive | Blue 90 | Overstuffed, Burst Seams |
| | 4 | Overstuffed | Mangled leap in to block | Mangled health ×3; each leaps up to 2 tiles toward the foe that hit the Cushion and holds there, blocking like a Snoble (Sno Sno's mob-blocker block) | Blue 400 | |
| | 4 | Burst Seams | Mangled burst on foes | a Mangled that reaches a foe bursts: 5 damage to every foe within 1.25 tiles | Blue 400 | |
| Loom | 2 | Direct Line | Thread goes straight home | spun Thread goes straight into the Stash while it has room | Yellow 30 | |
| | 2 | Seam Allowance | Makes Seams that mine and guard | one Seam per Loom | Yellow 30 | |

Added to the sheet, not built yet (Dimitri accepted them 2026-11-04, `docs/reports/UPGRADE_GAPS_2026-11-04.md`): seven
Yarn Stash upgrades, the Thread Head's levels 2-4, the Loom's levels 2-4, the Cookie Tin's Tacking Stitch, Cross Stitch
and Patchwork Army, the Pin Cushion's Iron Thimble and Voodoo Doll (Pin Barrage then leads to Iron Thimble), and the
Tin's UNTHREAD button. Scope `08` holds their numbers.

## Directions

`docs/scope/11`: **Efficient Recycler** (selling a Crafted refunds more Thread, the sheet's question 1 default),
**Sewing Machine** (replaces the Loom), **Made of Tougher Stuff** (any vessel with minions). Not built: Directions come
with the Rogue Waves mode, after the Yossele-only first release (`docs/PLAN.md`).

## Visual effects and sounds

- Ground Thread: a small purple tuft that bobs and fades in its last 2 s; a pickup flies to the carrier in a hop; a
  drop-off flies from the carrier into the Stash. The Stash, Loom and Thread Heads show their counters.
- Crafted pop out of the Tin with a stitch puff; a pin marker shows each Tin's spot; burst Crafted and Mangled throw a
  ring of stuffing; a pricked melee foe gets a pin spark; PATCH draws a thread from Spool to the target and stitches it.
- Sounds stay at or under the game's levels (`docs/03`): stitch, pop, tuft pick, drop-off, prick, burst, patch.

## Changes from the concept, and why

| Sheet | Spool 0.1.0-dev | Why |
|---|---|---|
| Thread Head "Works like Boutman's fish" | placed through a walk-through token, then a plain unit that doesn't block foes | a fish's block blocks foes (Gubby); the sheet asks for a porter that guards, not a wall |
| Target spot "on a path" | any walkable tile within 8 tiles that a Crafted can reach | the game has no "path tile" flag a mod can read cheaply; reachability is what matters |
| Pin Barrage leads on to level 3 | it ends the branch | both level-3 upgrades shape Mangled, which only Loose Stuffing makes |
| "Melee damage" | a hit whose attacker's `Range` is 2.25 or less | the hit itself doesn't say whether it was melee |

## Test plan (self-test sections, docs/08)

1. Vessel, boat, journal (generic).
2. The Stash first: only the Stash on the build menu, then the four; Stash Thread 10.
3. Thread: a drop on a felled foe; Spool's pickup within 1 tile; drop-off within 1.5; cap 50; expiry at 15 s.
4. Cookie Tin: Crafted cost 10, at most 4, one every 4 s; TARGET moves the spot; sold Crafted refunds 5.
5. Tin upgrades: Recycle drop, Woolen health, Discounted cost 8, New Design Darts, One Last Surprise burst.
6. Thread Head: token swap, collects within 3, RETURN delivers, Threaded Delivery, MEND heals and spends, respawn at
   the Stash with its upgrades.
7. Pin Cushion: reflect 20% / 40%, Thread per 25 damage, Slow Unravel split, Pin Barrage, Mangled per 40 (max 3),
   Overstuffed health and block, Burst Seams.
8. Loom: spins every 6 s up to 10, Direct Line, Seam spawn and respawn.
9. PATCH: cost 20 from the Stash, 20% heal capped at 500, range, cooldown, refusals.

## Defaults picked here

Each is Claude's pick where the sheet is silent; Dimitri can change any of them in `docs/scope/08` and the next build
takes it. The number lives in `Code/02_Data.cs` unless the row says otherwise.

| What | Default | Why |
|---|---|---|
| Thread per felled foe | 1; 3 for a large foe (base health 40 or more); +2 for an elite; at most 5 | the sheet: "smaller enemies drop 1, larger elite enemies drop up to 5" |
| One drop per foe | the first carrier to reach it takes it | Thread is per player; a shared drop is the simplest fair rule |
| Drops close together | merge within half a tile, up to 15 Thread in one tuft | fewer tufts on screen (the sheet: "doesn't lag the game") |
| Thread drops only while a Spool plays | yes | no tufts on maps nobody can use them on |
| Spool picks up a Loom's Thread | within 1.5 tiles (the drop-off reach) | the sheet gives 1 tile for the ground and nothing for the Loom |
| PATCH's hotkey | the thing under the cursor, else the most hurt of yours in range | the button picks; a hotkey needs a target without a click |
| PATCH on Spool itself | no | the sheet: "a unit or building" |
| A sold Crafted's refund that doesn't fit the Stash | dropped beside it as Thread | nothing is lost |
| Yarn Stash sold | refunds nothing, keeps its Thread for the next Stash | it is free; selling it is how you move it |
| Yarn Stash destroyed | its Thread spills on the ground; the menu goes back to the Stash (rebuilt free) | like the Bouthouse, the base comes first |
| A Cookie Tin with no spot picked | its Crafted guard the Tin | they need somewhere to stand |
| Crafted already out when TARGET moves | keep the spot they were sent to | the sheet: "they stay there until death" |
| Thread Head placement | a one-frame token on Buster's walk-through pad block, swapped for the Head | Changes from the concept |
| Thread Head sold | drops what it carried; the stock refund (Red 40 × Sellrate) | like any unit |
| A Loom destroyed | drops the Thread it held | nothing is lost |
| "Melee" for the Pin Cushion | the attacker's `Range` stat is 2.25 or less | Changes from the concept |
| Pin Barrage | ends its branch | Changes from the concept |
