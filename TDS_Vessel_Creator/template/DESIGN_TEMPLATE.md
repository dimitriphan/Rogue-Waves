# {{NAME}}: design (v1.0.0, {{DATE}})

Fill this in before writing code. Every line is checked against `docs/02_DESIGN_RULES.md`; the finished form becomes
the vessel's DESIGN.md (see `examples/Mummer_DESIGN.md` for a complete one).

## Identity

- **Name ▪ title ▪ difficulty:** {{NAME}}, "<Title>" ▪ <Basic | Simple | Intermediate | Advanced>
- **One-line fantasy** (what the player feels): ...
- **Silhouette / shape language** (must differ from every vessel on the boat: Smacky square, Sno Sno round, Buster dog,
  Boutman cat, Fie knight, Sprigg bulb-in-pot, Argo bulky dome helmet, Mummer triangle): ...
- **Sprite reference vessel** (the base vessel whose sprite style this one follows: Smacky, Sno Sno, Boutman, Fie or
  Buster, see docs/03 §1) and why: ...
- **Palette** (2-3 main colours + accent; the accent becomes the journal accent and button colour): ...
- **Towers**: low-poly sleek like Sno Sno's/Buster's, never voxel (fixed rule). Tower shape language: ...
- **Lore** (1-2 lines, story not mechanics, for the journal): ...

## The vessel

| Stat | Value | Band (docs/02) |
|---|---|---|
| Attack | melee (Mob_Combat) / ranged (Mob_Ranged + projectile) | |
| Health | | 25-65 |
| Speed | | 2-3.5 |
| Damage | | 1.5-5 |
| Range | | 1.5-3 |
| Attackspeed | | 0.4-0.75 |
| Buildrange | | 2-3 |
| Sellrate | | 0.5-0.75 |

- **Display_Desc** (pattern "Regular, single-target melee"): ...
- **Special rule** (tooltip upgrade name + one comma-phrase line): ...
- **Ability** (if any; keys 6/7, Onpressed, priority 6; picker or instant; cooldown shown on the button): ...
- **Custom resource** (if any; a counter stat on the vessel, server-side per player; how it is earned, spent, capped,
  lost): ...

## Buildables (exactly 4)

| # | Name | Id | Role tag | Template being | Cost (aura colour) | Stats (with +per level) | What it does |
|---|---|---|---|---|---|---|---|
| 1 | | Tower_{{NAME}}<x> | Towerfighter / Gatherer / none | | Red=offence, Blue=defence, Yellow=economy | | |
| 2 | | | | | | | |
| 3 | | | | | | | |
| 4 | | Trap_{{NAME}}<x>? | | | | | |

Units (if a buildable makes them; docs/12): id `Mob_{{NAME}}<x>`, stats, AI (stock Mob_Combat / Mob_Ranged), count and
respawn, behaviour (guard / follow / work / support), the player's order (rally / pick / mode / follow), how upgrades
reach them.

Tower motion (docs/03 §4a): which towers aim (native `Pivot` script or custom), spin while working, idle motion, or
show state with a skin.

## Upgrade trees (per buildable: 7 upgrades, tiers 2-5)

```
<Building>: Key2a / Key2b -> Key3a / Key3b -> Key4a (branch ends) / Key4b -> Key5 (capstone)
```

| Building | Key+Tier | Display | Desc (short comma phrase) | Params (Stat_Add / _Multi / X_Base ...) | Cost | Next |
|---|---|---|---|---|---|---|
| | 2 | | | | 15-75 | |
| | 3 | | | | 25-500 | |
| | 4 | | | | 75-750 | |
| | 5 | | | | 1500-5025 | |

## Visual effects and sounds

- Debuff looks (attached to foes by the game, every player sees them): `Debuff_{{NAME}}<x>` ...
- World effects (played by the client on Vis events): `World_{{NAME}}_<x>` ...
- Sounds (`{{NAME}}_<x>`, with their target level from docs/03): ...

## Changes from the concept, and why

| Concept | {{NAME}} 1.0 | Why |
|---|---|---|
| | | |

## Test plan (self-test sections to write, docs/08)

1. Vessel, boat, journal (generic, keep)
2. Build all four, upgrades, Tides (generic, keep)
3. ... one section per mechanic, each with a PASS condition a log line can prove
