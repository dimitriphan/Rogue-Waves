# Argo: design (v2.0.0, approved 2026-10-08)

Rogue Waves' Argo is **designed from the scope text** (`docs/scope/09_Vessel_Argo.txt`, "Argo, Deep Sea Diver"), which the user calls
the planned version of every vessel (`docs/PLAN.md` Part C). The earlier standalone ArgoMod 1.4.0 is reused only
where it matches. "Changes from the concept, and from 1.4.0" lists every difference; 1.4.0's own design is `DESIGN_1.4.0.md`. Checked against `TDS_Vessel_Creator/docs/02`.
Status: **approved 2026-10-08; built and self-tested on the PC (round 6: 139 pass, 0 fail, `IMPLEMENTATION_NOTES.md`)**. Unmarked numbers are 1.4.0's, verified in its
ninth PC self-test (103/103).

## Identity

- **Name ▪ title ▪ difficulty:** Argo, "Deep Sea Diver" ▪ Intermediate (five buildables and a crew to manage, no
  custom resource).
- **One-line fantasy:** no walls; control the field instead. Slow foes, pin them, reel them in, and blast what's stuck.
- **Silhouette:** bulky dome-helmeted diver with hands touching the body (taken shape, `docs/STYLE.md`; look unchanged
  from 1.4.0).
- **Sprite reference vessel:** as shipped in 1.4.0.
- **Palette:** as shipped; accent = the 1.4.0 hero `Color`.
- **Towers:** low-poly sleek. Brass domes, portholes and rivets on the top and front faces (as shipped).
- **Lore:** as shipped in 1.4.0.

## The vessel

| Stat | Value (1.4.0, kept) | Band (docs/02) |
|---|---|---|
| Attack | melee (Mob_Combat), reach | |
| Health | 65 | 25-65 (bulky melee, top of the band) |
| Speed | 2.25 | 2-3.5 |
| Damage | 4 | 1.5-5 |
| Range | 1.75 | 1.5-2 |
| Attackspeed | 0.4 | 0.4-0.75 |
| Buildrange | 2 | 2-3 |
| Sellrate | 0.5 | 0.5-0.75 |

- **Display_Desc:** "Sturdy, slow melee, hits two at once" (1.4.0).
- **Second hit:** every swing also hits a second foe within 1.25 tiles of the first for 60% (1.4.0).
- **No SONAR** (user, 2026-10-08: not designed with SONAR for now). Argo has no ability button; nothing below needs one.
- **Special rule** ("Fathoms"): "Swings hit two enemies, no walls of its own, Scatter Cannons punish slowed and pinned
  foes, Drill Rigs sink a Fathom each Tide and their aimed Tremors widen, the crew tunes towers, never
  traps."
- **Debuffs the kit applies** (the Scatter's bonus reads either): Slowed (Deep Water, Tremor, Barnacle Shot, Tether),
  Pinned (Anchor Line, Mantle Tap's quake).

## Buildables (five, as the scope lists; see Changes)

| # | Name | Id | Role tag | Template | Cost | Stats (+per level) | What it does |
|---|---|---|---|---|---|---|---|
| 1 | Scatter Cannon | `Tower_Argoscatter` | Towerfighter | Bomb Tower | Red 30 | Health 40+25, Damage 1.25+0.25, Attackspeed 0.6, Range 4.5, Projcount 6, Spread 50, Pierce 1, **Debuffbonus 0.5** | Sprays a cone of 6 pellets, shreds a crowd up close; **+50% damage to a debuffed foe** (new) |
| 2 | Harpoon Gun | `Tower_Argoharpoon` | Towerfighter | Archer Tower | Red 35 | Health 40+25, Damage 5+2, Attackspeed 0.35, Range 6.5, Projspeed 18, Pierce 3 | Slow, long-range harpoons that pierce a lane. **Its tree picks the job:** pure damage (Long Line, Skewer, Whaler, Leviathan Lance) or crowd control (Tether, Anchor Line's pin, Reel, Undertow). The base gun no longer reels (1.4.0 did) |
| 3 | Deep Water | `Trap_Argowater` | Trap | Buster's Speed Pad block | Blue 20 | Health 30+15, Slow 0.3+0.05 | **New.** Walk-through pool; foes inside are Slowed. **FLOOD** (keys 1/Quickaction, like Fie's Grow Wall) adds an adjacent pool for Blue 15. Combines: a pinned foe in Deep Water is held twice as long; a Tremor touching a pool spreads through every pool connected to it |
| 4 | Drill Rig | `Tower_Argodrill` | Gatherer | Archer Tower | Yellow 35 | Health 40+25, Damage 0.5 (mining), Attackspeed 0.25, Range 5 (aim), Tremor 2, Slow 0.2 | Mines ore up to 2 tiles away and sinks a Fathom each Tide (to 5): faster mining, a wider (+0.4 tiles) and stronger (+4%) Tremor. **AIM** (picker, within 5 tiles) **places the Tremor** where the player wants it (new; 1.4.0's ringed the drill) |
| 5 | Miner's Base | `Tower_Argobell` (1.4.0's id, kept) | none | Mining Hut | Blue 35 | Health 50+25, Units 2, Respawn 8 | 1.4.0's Diving Bell, renamed. **Limit 1** (new: its build button goes while one stands, Boutman's Bouthouse pattern). Houses **Port** and **Starboard**: PORT or STARBOARD, then click a tower (a teammate's too) and that Naut walks over and **tunes** it, +20% attack speed and a slow repair. **Towers only, never traps** (new) |

Units: `Mob_Argonaut_Port`, `Mob_Argonaut_Starboard` (and `Mob_Argonaut_Bosun` from Third Mate), as in 1.4.0.

Tower motion (`docs/03` §4a): the Scatter and Harpoon aim at each target (1.4.0's `Turret` part); the Drill's drill
spins while it mines; Deep Water bobs gently; the Base stays still.

## Upgrade trees (tiers 2-5)

Unmarked upgrades are 1.4.0's, with its costs and params (`DESIGN_1.4.0.md`). **New** or
**changed** ones are marked.

```
Scatter Cannon: Wide Bore2 / Double Barrel2 -> Grapeshot3 / Barnacle Shot3 -> Pressure Rounds4 / Ironclad4 (ends) -> Broadside5
Harpoon Gun:    Long Line2 / Tether2 -> Skewer3 / Anchor Line3 (ends) / Reel3 -> Whaler4 / Twin Line4 -> Leviathan Lance5 | Undertow5
Deep Water:     Undercurrent2 / Brine2 -> Kelp Bed3 / Salt Sting3 -> Abyss4 (ends) / Crushing Depth4 -> Trench5
Drill Rig:      Pay Dirt2 / Seismic2 -> Deep Core3 / Bedrock3 -> Gusher4 / Fault Line4 (ends) -> Mantle Tap5
Miner's Base:   Flippers2 / Old Salts2 -> Overhaul3 / Calibrate3 -> Third Mate4 / Bulkhead4 (ends) -> Full Crew5
```

| Building | Upgrade | Status | Desc | Params | Cost | Next |
|---|---|---|---|---|---|---|
| Scatter | Ironclad4 | **new** (scope: "increases health greatly"), replaces Undertow4 | Health greatly increased | Health_Multi 1.5 (health ×2.5), Healtharmor_Base 2 (the game's armour: each hit, whatever its damage, takes its Damagemulti, 1 by default, from it first) | Red 250, Blue 150 | (ends) |
| Harpoon | Tether2 | **new**, replaces Barbed2 | Harpooned foes are slowed | Tetherslow_Base 0.3, Tetherslow_Duration 2 | Red 30 | Anchor Line3, Reel3 |
| Harpoon | Anchor Line3 | **changed**: pins the foe hit, stunned instead beside a building | Pins the foe it hits, stunned against a building | Root_Base 1, Pinstun_Base 1 | Red 60, Blue 50 | (ends) |
| Harpoon | Reel3 | **new** (1.4.0's base reel moved here) | Reels the first foe hit toward the gun | Reel_Tiles 1.5 | Red 60, Blue 50 | Twin Line4 |
| Harpoon | Twin Line4 | **changed**: next is Undertow5 | A second harpoon follows, reeling its own foe | Twin_Base 0.3 | Red 350 | Undertow5 |
| Harpoon | Undertow5 | **new** CC capstone | Every harpoon reels the foes around its target | Undertow_Radius 1.5, Undertow_Tiles 1.5 | Red 1750, Blue 1750 | |
| Deep Water | Undercurrent2 | **new** | Stronger slow | Slow_Add 0.1 | Blue 20 | Kelp Bed3, Salt Sting3 |
| Deep Water | Brine2 | **new** | Foes inside take damage over time | Brine_Damage 0.5 | Blue 25 | Salt Sting3 |
| Deep Water | Kelp Bed3 | **new** | Foes are caught for a moment on entering | Catch_Duration 0.75 | Blue 80 | Abyss4, Crushing Depth4 |
| Deep Water | Salt Sting3 | **new** | Brine stings pinned foes harder | Brinepinned_Multi 1 (without Brine, its sting is on a default 0.5 a second) | Blue 70, Red 30 | Crushing Depth4 |
| Deep Water | Abyss4 | **new** | FLOOD fills two tiles at once | Flood_Add 1 | Blue 260 | (ends) |
| Deep Water | Crushing Depth4 | **new** | Foes inside take more damage | Damagetaken_Multi 0.15 | Blue 300 | Trench5 |
| Deep Water | Trench5 | **new** | Spreads by itself each Tide, and drags foes under now and then | Spread_Per_Tide 1, Dragunder_Every 6, Dragunder_Duration 1 | Blue 3000 | |
| Drill Rig | Bedrock3 | **new** (scope), replaces Aftershock3 | Pinned foes in its Tremor take damage over time | Bedrock_Damage 1.5 | Blue 80, Red 30 | Fault Line4 |
| Drill Rig | Fault Line4 | **changed** (scope): was +1.5 tiles | Foes reeled through its Tremor take a burst of damage | Fault_Damage 12 | Blue 300 | (ends) |
| Drill Rig | Seismic2 | **changed**: reach in the Tremor stat | The Tremor slows harder and reaches farther | Slow_Add 0.1, Tremor_Add 0.5 | Blue 25, Yellow 10 | Bedrock3 |
| Base | Old Salts2 | **changed**: Drill Rig's Tremor, never Deep Water | Nauts tune each kind of Argo tower its own way | Salts_Base 1 | Blue 30 | Overhaul3 |

## Choices this design makes

- **Five buildables.** The game's vessels create `Buildoption1..4`; a fifth is a `Command_Buildoption4` clone with
  its own key (works in game, PC round 2). Artifacts that walk the build options (Bottom Rock) only know 1-4.
  Fallback if it fails: Deep Water becomes the Drill Rig's FLOOD command (four build buttons, nothing lost but free
  placement).
- **1.4.0's "loaded guns"** (a gun inside a Drill's Tremor gains a pellet or pierce per Fathom) is dropped. The Tremor
  is now aimed at the foes, not at Argo's guns, so the rule would ask the player to aim it at their own towers.

## Art (step 2, generated; previews in `Previews/2.0/`)

Everything is drawn by the editor generators (`Assets/ArgoMod/Editor`), as in 1.4.0. The vessel, units, Scatter base,
Miner's Base (1.4.0's Diving Bell model, unchanged) and Drill Rig keep 1.4.0's look; only what 2.0 adds is new.

- **Models** (`ArgoModels.cs`): `Trap_Argowater` and its seven upgrades (`_Undercurrent2`, `_Brine2`, `_Kelp3`,
  `_Salt3`, `_Abyss4`, `_Crushing4`, `_Trench5`): a pool in a low stone frame filling the tile (the frame 0.075 high,
  0.27 to the top of the pump), with a small brass pump dome and porthole in the back corner (Argo's motif on the top
  face); the water darkens from Crushing Depth. `Argo_Pool` is a FLOOD pool: frame and water, no pump. New upgrade
  models: Scatter `_Ironclad4` (armour skirt and gun shield), Harpoon `_Tether2` (a teal line from drum to head),
  `_Reel3` (red winch wheels; Twin Line and Undertow keep them), `_Undertow5` (dark body with teal bands and a grapnel
  head), Drill `_Bedrock3` (a stone collar with teeth round the bore).
- **Effects** (`ArgoBuilder.cs`): `World_Argo_Flood` (splash), `World_Argo_Wet` (drips on a foe slowed by Deep
  Water), `World_Argo_Pinned` (rope bits round the feet), `World_Argo_Stun` (stars, Anchor Line beside a building),
  `World_Argo_Whirl` (Undertow's ring, scaled to its radius), `World_Argo_Kelp`, `World_Argo_Brine`,
  `World_Argo_Dragunder`, `World_Argo_Bedrock`, `World_Argo_Faultburst`. Slowed keeps the game's `Debuff_Slow` too,
  as 1.4.0 does.
- **Sounds** (`ArgoAudio.cs`): `Argo_Splash` (pool placed, FLOOD), `Argo_Pin`, `Argo_Gurgle` (Trench). Reels keep
  `Argo_Winch`, quakes `Argo_Quake`. Loudness targets go in `02_Data.cs` `SoundTarget` with the code (start near
  `Argo_Harpoon`'s 0.004).
- **Icons** (`03_Assets.cs`, 12×12): `FloodIcon` (a drop and a plus) and `AimIcon` (crosshairs on a ring).
- Kept unused in the source (SONAR is switched off by `SonarEnabled = false`): SONAR's sheet, icon, ring, ping and sound; Barbed's bleed;
  the 1.4.0 upgrade keys that 2.0 replaced (Undertow4, Barbed2, Aftershock3 models are no longer generated).

## Changes from the concept, and from 1.4.0

| Concept (scope) | Argo 2.0 | 1.4.0 had | Why |
|---|---|---|---|
| "Argo, Deep Sea Diver" | Title "Deep Sea Diver" | "Fathoms Below" | The scope's words |
| Five buildables | All five (Deep Water is new) | Four | The concept wins; the fifth build option works in game (PC round 2) |
| Harpoon Gun: long-range damage **or** crowd control, "pin or reel depending on upgrade path" | Base gun pierces and doesn't reel; the tree splits into damage and CC paths, two capstones | Every harpoon reeled | As written; the reel (verified in 1.4.0) moves to Reel3 |
| "Pinning against wall stuns" | Anchor Line pins the foe hit; beside any building it is stunned instead | Pinned the reeled foe | Argo has no walls, and the game has no knockback (`docs/05` §11) to pin a foe *against* something; "beside a building" reads the same on screen |
| Scatter: "bonus damage to enemies affected by debuffs" | +50% to Slowed or Pinned foes | none | As written; every debuff source is a tower, trap or upgrade |
| Scatter: "% increases health greatly" | Ironclad4 (ends early) | Undertow4 | As written |
| Deep Water: slowing trap, extends like Fie's wall, combines with Harpoon and Drill Rig | FLOOD extends it; pinned foes held twice as long; Tremors spread through pools | none | Gives "combines" two testable rules |
| Drill Rig: "tremors in targeted location; tremor size increases per tide" | AIM picker places the Tremor; Fathoms widen it each Tide | Tremors ringed the drill | A target the player picks needs a picker (`docs/02` §2) |
| Drill Rig %: pinned foes on tremors take DOT; pulled foes through tremors take burst | Bedrock3, Fault Line4 | Aftershock (Tremors hurt all), Fault Line (+range) | As written |
| Miner's Base: like the Bouthouse, limit 1; Port & Starboard tune towers, not traps | As written, on 1.4.0's verified Nauts | Diving Bell, no limit | As written |
| (the scope gives Argo no attack, ability or rule) | 1.4.0's melee and second hit, kept | same, plus SONAR | Keeps what players know |
| (none) | No SONAR and no Pinged debuff | SONAR (keys 6/7) pinged foes for +20% damage and quaked Drills | User, 2026-10-08: don't design with SONAR for now. Its 1.4.0 code stays in the source, unused, until decided |

### Step 3, code reality (2026-10-08)

- **FLOOD** is the Deep Water's own build option: a clone of `Command_Buildoption1` aimed at `Trap_Argowater_Pool`
  (Blue 15, hidden from the journal and the hero's buttons), sent as the game's `Build` command to the trap, which the
  server checks for adjacency (no diagonals) before spawning it Fie's way. Abyss adds `Flood_Add` free pools beside it.
- **Water networks**: the pump and every pool touching it (or touching a pool that does) are one network, rebuilt every
  0.25 s; the most-upgraded pump in it leads, so its upgrades work in every pool.
- **AIM** is an item-select picker (the crew buttons' pattern) on the ground under the mouse; the server refuses a tile
  past Range + 0.5. Clients read the Tremor's centre and radius from the AIM command's Goal fields.
- **Tremor reach** is a stat (`Tremor`, 2 at Fathom 1, +0.4 a Fathom, Seismic +0.5), so the game shows it.
- **Kit 1.6** comes in with Argo: `ModKitPack.Join` at start, `ModKitTides` for the Tide, `ModKitCounter` for Fathom,
  `ModKitFocus` for the F-key fix. A standalone Argo already loaded makes the pack's Argo stand down.
- Deep Water bobs: its water is a `Swell` part the clients raise and lower ±0.012 over 3.2 s; the frame stays still.

## Test plan (self-test, `docs/08`)

1.4.0's 103 checks stay where their mechanic stays. New or changed:

1. **Five build options appear and each builds** (the engine check); the Miner's Base button goes once one stands and
   comes back after a sell.
2. Scatter: +50% damage to a Slowed and a Pinned foe, none to a clean one; Ironclad's health.
3. Harpoon: the base gun pierces and doesn't move foes; Anchor Line pins the foe hit 1 s, stuns it beside a building;
   Reel moves it toward the gun (1.4.0's lane checks); Undertow reels the foes around a hit; a boss is never moved.
4. Deep Water: slow inside; FLOOD adds an adjacent pool for Blue 15; a pinned foe in a pool is held 2 s; a Tremor
   touching a pool spreads through connected pools.
5. Drill Rig: AIM moves the Tremor; the client ring matches the server's radius; Fathoms widen it each Tide; Bedrock
   hurts a pinned foe in it; Fault Line bursts a foe reeled through it.
6. Miner's Base: a Naut picked onto Deep Water is refused (towers only); tuning as in 1.4.0.
7. The F-key fix (`docs/15` §9): F selects Argo after a respawn with the crew out.

## Review 2026-11-04 (proposed, not built)

A balance and skill review (`docs/reports/ARGO_REVIEW_2026-11-04.md`) made fourteen changes and two notes in scope `09`, each
line marked with a "Review 2026-11-04" note: slows one per source and Kelp Bed and Trench as pins (both code fixes),
the Scatter's pin bonus, Wide Bore, Double Barrel, Abyss, Trench, Gusher, Mantle Tap, and Yellow prices for the Drill's
control path and the Miner's Base. The tables above stay as built until Dimitri takes them and the next Argo build
lands; `IMPLEMENTATION_NOTES.md` lists the code each one touches.
