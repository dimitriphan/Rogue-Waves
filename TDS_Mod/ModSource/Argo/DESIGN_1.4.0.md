# Argo: design (v1.4.0, 2026-10-04; first built as 1.0.0, 2026-10-02)

**ARGO, Fathoms Below ▪ Intermediate.** A bulky deep-sea diver in a domed brass helmet (built in the spirit of
League's Nautilus), with a crew of Nauts, a scatter cannon, a harpoon tower and a drill that sinks deeper every Tide.
Built from the user's "Argo Vessel Concept.zip" (kept in `Concept/`). Up to 1.3 the design lived in `README.md` and
`IMPLEMENTATION_NOTES.md`; this file is its record from 1.4 on.

## The kit

| | What it does | Cost |
|---|---|---|
| **Argo** (vessel) | Sturdy, slow melee ("Sturdy, slow melee, hits two at once"): 65 HP, speed 2.25, damage 4, range 1.75, attack speed 0.4, buildrange 2, sellrate 0.5. Every swing also hits a second foe within 1.25 tiles of the first for 60% | |
| **SONAR** (keys 6/7) | Pings every foe within 6 tiles: Pinged for 5 s, they take 20% more damage from everyone. Deep Drills inside the ping quake, holding foes in their Tremors for a second. Cooldown 15 s, the button greys out | |
| **Scatter Cannon** | Short range (4.5), sprays a cone of 6 pellets | 30 Red |
| **Harpoon Tower** | Long range (6.5), slow (0.35), damage 5 (+2 a level); each harpoon pierces 3 foes down the line that holds the most, and **reels the first foe it hits 1.5 tiles toward the tower** (Boss-weight foes, 600+ health, aren't moved) | 35 Red |
| **Diving Bell** | Houses Nauts **Port** and **Starboard** (Bosun from Third Mate): pick one, click any tower, a teammate's too, and the Naut walks over and tunes it (+20% attack speed, a slow repair) | 35 Blue |
| **Deep Drill** | Mines ore up to 2 tiles away; its Tremors (range 2) slow foes 20%; sinks a Fathom each Tide. Argo's guns inside its Tremors are **loaded**: +1 pellet (Scatter) or +1 pierce (Harpoon) per Fathom below the first | 35 Yellow |

**Rule (tooltip upgrade "Fathoms"):** "Swings hit two enemies, Deep Drills sink a Fathom each Tide: every Fathom mines
faster, widens their Tremors and loads the guns inside them, Nauts tune any tower, teammates' too".

**Fathoms.** Fathom 1 when built, down to 5 (7 with Deep Core). Each Fathom: +75% ore per strike, +10% strikes a
second, +0.4 tiles and +4% slow on the Tremors, and one more step of load for the guns inside them.

## Upgrade trees (tiers 2 to 5)

- **Scatter Cannon** (red): Wide Bore (+2 pellets, wider cone) / **Double Barrel** (stores a volley while no foe is in
  range; the next target gets both, the second 0.15 s after the first) → Grapeshot (+3 pellets) / Barnacle Shot
  (pellets slow) → Pressure Rounds (+1 pierce, +0.5 damage) / Undertow (faster, each hit holds 0.35 s, branch ends)
  → **Broadside** (a ring of 16 every 4th volley).
- **Harpoon Tower** (red): Long Line (+2 range, faster harpoons) / Barbed (bleed) → Skewer (+3 pierce) / **Anchor
  Line** (the reeled foe is pinned 1 s where it lands) → Whaler (+4 damage, up to x2 on big health pools) / Twin Line
  (a second harpoon per shot, it reels its own first foe; branch ends) → **Leviathan Lance** (every 5th shot pierces
  everything at twice the range; the lance reels nothing).
- **Diving Bell** (blue): Flippers (Nauts +50% speed) / **Old Salts** (a Naut tunes each kind of Argo tower its own
  way: Scatter +2 pellets; Harpoon +2 pierce and reels 1 tile farther; Drill Tremor slow +10%; other towers get the
  attack speed only) → Overhaul (repair) / Calibrate (tuned towers +1 range) → Third Mate (Bosun) /
  Bulkhead (-25% damage taken, branch ends) → **Full Crew** (tuning +40%, Nauts dive to their tower).
- **Deep Drill** (yellow): **Pay Dirt** (a foe felled inside its Tremors is dug up: 2 mining strikes on its ore, at the
  Fathom's yield) / Seismic (+10% slow, +0.5 tiles) → Deep Core (Fathom 7) / Aftershock (Tremors hurt) → Gusher (12
  strikes each Tide from Fathom 3) / Fault Line (+1.5 tiles, branch ends) → **Mantle Tap** (ore x2, a quake once a
  Tide).

Next lists: Widebore2 → Grapeshot3, Barnacle3; Double2 → Barnacle3, Grapeshot3. Longline2 → Skewer3, Anchor3; Barbed2
→ Anchor3; Anchor3 → Twinline4. Flippers2 → Overhaul3, Calibrate3; Salts2 → Overhaul3. Paydirt2 → Deepcore3,
Aftershock3; Seismic2 → Aftershock3. Costs: Double2 30 Red, Salts2 30 Blue, Paydirt2 30 Yellow; every other upgrade
as in 1.3. Upgrade descriptions carry no numbers; the journal's EFFECTS lines carry them.

## Changes in 1.4.0 (identity pass, 2026-10-04)

From the user's gameplay design review: Argo's "first change to try" (section 8), a tier-2 root per building that
changes behaviour (2.2), and bridges between buildings (2.3). The vessel's stats, cleave, SONAR, every other upgrade and
the Diving Bell's base tuning stay as they were.

| 1.3 | 1.4 | Why |
|---|---|---|
| Harpoon: pierces a line | The first foe each harpoon hits is reeled 1.5 tiles toward the tower over 0.4 s (on its own Move command, never physics; stops 1.2 tiles short of the tower, measured from the tile the foe is stepping onto, so a foe about 2 tiles out along a row has nowhere to go; when one projectile step hits several foes, the one nearest the tower is "first"; Boss-weight foes and foes already reeled aren't moved) | The long gun now feeds the short one: a harpoon yanks a foe into the Scatter's cone |
| Anchor3 Anchor Line: pins the first foe hit | Pins the reeled foe where it lands, for 1 s | Same key and price; it finishes the reel instead of replacing it |
| Fathoms touch only the Drill | Argo's guns inside a Drill's Tremors are loaded by its depth: Scatter +1 pellet, Harpoon +1 pierce per Fathom below the first (the deepest Drill of the team that reaches the gun counts; checked every second and at each Tide) | The special rule reaches the whole nest, and losing the Drill costs the guns their depth |
| Choke2 Choke (tighter, farther) | Double2 **Double Barrel** | Tier-2 behaviour root; pairs with harpoons dropping foes into the cone |
| Hardhats2 Hard Hats (Naut health) | Salts2 **Old Salts** | Tier-2 behaviour root; where the crew goes becomes a choice |
| Rotary2 Rotary Bit (mining speed) | Paydirt2 **Pay Dirt** | Tier-2 behaviour root and a guns-to-Drill bridge: what the guns fell in the Tremors feeds the Drill |

In code the load is called "Pressure"; the game's texts say the Drill "loads the guns", so it never reads like the
Scatter's Pressure Rounds upgrade. Boss weight is the foe's full health (`Health.Value_Calc`, what `ArgoUtil.MaxHealth`
reads), the same 600 line Mummer's HOOK uses.

**Looks and sounds.** A reel draws a rope from the harpoon to the foe for the reel time on every screen, with a quiet
winch (`Argo_Winch`: five clicks and a low creak, at the game's levels). A gun that gains load gives off a few
bubbles (silent). Double Barrel gives a quiet winch click when it stores a volley. Pay Dirt throws dust where the foe
fell and runs the brass tether from that spot to the Drill. Models: Double Barrel adds a shorter second barrel under the
first, clamped by a brass plate; Old Salts sets a captain's cap (white crown, navy band, black visor, gold badge) on
the bell's top hatch; Pay Dirt hangs a rust-red ore skip full of ore from the drill frame's front crossbar.

## Test plan (self-test, 1.4 additions)

They replace the Choke, Hard Hats and Rotary Bit checks.

1. A harpoon reels the first foe of a lane at least a tile closer within 1 s (two foes 5 tiles out where the hill has
   room, else one foe 4 tiles out); the foe behind it, in its own two-foe lane then, is hit but doesn't move.
2. A Boss-weight foe (700 health added) is harpooned but not moved.
3. A second Drill near the guns (on any free tile within 10), its Tremors widened to reach both and sunk to Fathom 3:
   the Scatter has 6+2 pellets and the Harpoon 3+2 pierce. Once it is sold, they are back to 6 and 3 within 1.5 s.
4. Double Barrel (forced: the other root) stores one volley while nothing is in range; the next target gets two
   volleys, twice the pellets, 0.15 s apart.
5. Anchor Line (forced: the other tier 3): the reeled foe is at its Speed floor right after landing, and moving again
   about a second later.
6. Pay Dirt: a foe felled inside the Tremors gives 2 mining strikes; one felled outside gives none. (Ore pays its
   Yellow only when it cracks, so the strikes are counted, not the Yellow.)
7. Old Salts (bought as the Bell's root; Flippers is forced afterwards for its own check): Port tunes the Scatter (+2
   pellets), then the Harpoon (+2 pierce, reel +1 tile; the Scatter is back to normal), then the Drill (+10% slow; the
   Harpoon is back), and everything is back once Port is called home.

Foes in the reel tests are held at their Speed floor until the hit (a reel's +5 Multi on top still walks them at five
times their speed), so only the reel moves them, and the tower is kept from firing until they are ready.
