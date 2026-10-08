# Rogue Waves style sheet

One page every vessel, enemy, Cardinal, UI piece and text of the pack follows. The game's own rules come first:
sprites `TDS_Vessel_Creator/docs/03` §1 (house style), towers §4 (low-poly sleek, never voxel). This page only adds
what makes the pack read as one. Phase 2 draft, 2026-10-08, waiting on the user's gate.

## The theme

The base game is a world going under water. Its modes are named by water level: Still Waters, Downpour, High Tide,
Flood, Tsunami. **A rogue wave is the one that comes out of nowhere.** The pack is about the unexpected: each run is
different, things wash ashore, and the sea takes back what you don't defend. Vessels keep their own colours and
fantasy. The pack's look lives in what belongs to the run: Pure Aura, Cardinals, Directions, the shop, the vote, the
modifiers and the mode's UI.

## Palette (pack pieces only)

| Swatch | Hex | Used for |
| --- | --- | --- |
| Deep Swell | `2E4A62` | panels and shadows of the mode's UI, the dark side of pack icons |
| Rogue Teal | `3F8F96` | the mode's main colour: the "Rogue Wave" Cardinal, vote and shop frames |
| Foam | `E9F1EE` | rims, highlights, the crest motif |
| Pearl (Pure Aura) | `F4EEF8`, sheen `C9E4F2` / `F2D9E6` | Pure Aura only: a white that is none of the base auras (Red, Blue, Yellow, Green = all), with a faint two-tone sheen |
| Storm Coral | `E2725B` | danger and the pack's new modifiers' accent (Cursed, Berserk), ascension marks |
| Wet Sand | `D8C49A` | the shopkeeper's stall and washed-up things |

Rules: pastel or muted mains with one saturated accent (the house style). Never put pure black or a black outline on
anything. Pure Aura is always Pearl, so it never looks like a base aura.

## Motifs

- **The crest**: one breaking curl with a Foam rim. It is the pack's mark: the mode icon, the corner of pack UI
  frames, the "Rogue Wave" Cardinal. Draw it as one clean shape, not a pattern.
- **Washed ashore**: Cardinals and Directions are found objects, as in the base game (Red Coin, Rusty Pipe, Fish Bait).
  Pack Cardinals are flotsam, things the wave brought: a cracked buoy, a message bottle, a ship's bell. One object per
  icon, readable at 12×12.
- **Pearl drops** for Pure Aura: a small teardrop, Pearl with its sheen, used in the counter, the shop and rewards.
- No skulls, gore or horror for the harder modifiers. Danger reads through Storm Coral and the shape.

## Naming voice

| Thing | Voice | Examples |
| --- | --- | --- |
| Vessel | one soft name + an epithet, as the scope writes them | Yossele, The Patient Golem; Spool, Crafty Crafted Crafter |
| Buildings, units | the everyday object named plainly, with a twist; the scope's names stay | Repurposed Cookie Tin, Pin Cushion, Flood Light |
| Cardinals | two plain words, a found object; one-line effect, no numbers (base style) | Cracked Buoy, Ship's Bell |
| Directions | a heading or a course, short and active | Deep Roots, Loose Threads, Hard to Port |
| Mode, ascension, events | sea weather | Rogue Wave; ascension levels "Swell 1, 2, ..."; banners "A rogue wave rises" |
| Enemy modifiers | the scope's names (Cursed, Berserk). Base elites are named like metalwork (Hardened, Tempered, Superheated, Fractured, Annealed), for reference |

Text is plain and short, sentence case, and has no numbers in upgrade text (`docs/02`).

## The roster: shape language and sprite reference

Each vessel owns one silhouette and carries it into its towers. Taken shapes are in `.claude/rules/design.md`.

| Vessel | Shape | Sprite reference (`docs/03` §1) | Main / accent | Signature | Towers carry |
| --- | --- | --- | --- | --- | --- |
| **Argo** | bulky dome diver, hands touching the body | as shipped in ArgoMod 1.4.0 (look unchanged) | as shipped | the dome helmet | brass domes and portholes, as shipped |
| **Yossele** | **menhir**: a tall slab rounded at the top and wider at the base, clay with moss on the shoulders | Sno Sno (an object come alive), but heavier and still: small eyes low on the slab, short stubby arms | clay `B08A6A`, moss `7FA65A`; accent one small mushroom cap `D9654F` | a carved mark on the brow, and moss that grows a little across the idle loop | standing stones and roots. Each [Growth] stage is a **skin** that adds moss, mushrooms or leaves, never a motion |
| **Spool** | **hourglass**: a wooden spool body (two flared rims, a waist wound in thread), stubby legs | Sno Sno (an object come alive) | wood `D9B07A`, thread `D9546B` | a needle held like a lance, a loose thread end that sways in idle | sewing things built on the spool's flared-rim cylinder: tin lids, a loom frame, a cushion bristling with pins |
| **Uloe** | **prism**: a faceted diamond (rhombus) body with a thin halo ring above, floating, no legs | Fie (a small humanoid read by its head shape) | warm white `FFF1B8`, Foam rim; accent amber `F2A93B` | the halo, which brightens and dims across the idle loop | faceted prisms and lenses: an octahedron on a stem, a lens frame, the Flood Light's dish, the Mirror wall's panes |

Each must read at 1× zoom against the boat and the darkest map, and differ from every shape already taken: Yossele
is not Smacky's square (it is taller, tapered and earthy), Spool is not Sno Sno's stacked balls (it is flat-topped
with rims), and Uloe is not Crackpot's orb or Sprigg's bulb (it is angular and faceted).

## Towers

Low-poly sleek per `docs/03` §4, from each vessel's 8×8 palette. The silhouette rule: **every tower shows its
vessel's primitive on the top or front face** (Yossele's rounded slab, Spool's flared rim, Uloe's facets, Argo's
dome), so a player can tell whose tower it is from the game camera. Upgrades keep the silhouette and add one feature
each. States (Growth, Light, a held seed count) show as skins or a counter, not as motion.

## Enemies

New foes follow the base foes' sprite style (one colour family per foe, red aura drops). New modifiers ship a skin
for every stock foe, as the base elites do (Health, Hyper, Red, Chaos, Armor, Explode), tinted with Storm Coral.
