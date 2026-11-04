# Rogue Waves style sheet

One page every vessel, enemy, Cardinal, Trinket, UI piece and text of the pack follows. The game's own rules come first:
sprites `TDS_Vessel_Creator/docs/03` §1 (house style), towers §4 (low-poly sleek, never voxel). This page only adds
what makes the pack read as one. Phase 2 draft, 2026-10-08, approved by the user.

## The theme

The base game is a world going under water. Its modes are named by water level: Still Waters, Downpour, High Tide,
Flood, Tsunami. **A rogue wave is the one that comes out of nowhere.** The pack is about the unexpected: each run is
different, things wash ashore, and the sea takes back what you don't defend. Vessels keep their own colours and
fantasy. The pack's look lives in what belongs to the run: Pure Aura, Cardinals, Trinkets, Directions, the shop, the vote, the
modifiers and the mode's UI.

## Palette (pack pieces only)

| Swatch | Hex | Used for |
| --- | --- | --- |
| Deep Swell | `2E4A62` | panels and shadows of the mode's UI, the dark side of pack icons |
| Rogue Teal | `3F8F96` | the mode's main colour: the main menu's Rogue Waves button, vote and shop frames |
| Foam | `E9F1EE` | rims, highlights, the crest motif |
| Pearl (Pure Aura) | `F4EEF8`, sheen `C9E4F2` / `F2D9E6` | Pure Aura only: a white that is none of the base auras (Red, Blue, Yellow, Green = all), with a faint two-tone sheen |
| Storm Coral | `E2725B` | danger and ascension marks (not a modifier's colour: see Enemies below) |
| Wet Sand | `D8C49A` | the shopkeeper's stall and washed-up things |

Rules: pastel or muted mains with one saturated accent (the house style). Never put pure black or a black outline on
anything. Pure Aura is always Pearl, so it never looks like a base aura.

## Motifs

- **The crest**: one breaking curl with a Foam rim. It is the pack's mark: the mode icon, the corner of pack UI
  frames, the main menu's Rogue Waves button. Draw it as one clean shape, not a pattern.
- **Washed ashore**: Cardinals, Trinkets and Directions are found objects, as in the base game (Red Coin, Rusty Pipe, Fish
  Bait). The pack's Cardinals and Trinkets are flotsam, things the wave brought. The first is named in
  `docs/scope/12_Cardinals.txt` (Vampire Dentures, a Trinket); Cracked Buoy and Ship's Bell are examples only (`15_Templates.txt` uses Cracked Buoy). One object per
  icon, readable at 12×12.
- **Pearl drops** for Pure Aura: a small teardrop, Pearl with its sheen, used in the counter, the shop and rewards.
- No skulls, gore or horror for the harder modifiers. Danger reads through the shape (and Storm Coral on danger marks; a modifier takes its own colour, see Enemies).

## Naming voice

| Thing | Voice | Examples |
| --- | --- | --- |
| Vessel | one soft name + an epithet, as the scope writes them | Yossele, The Patient Golem; Spool, Crafty Crafted Crafter |
| Buildings, units | the everyday object named plainly, with a twist; the scope's names stay | Repurposed Cookie Tin, Pin Cushion, Flood Light |
| Cardinals, Trinkets | two plain words, a found object; one-line effect, no numbers (base style) | Vampire Dentures, Cracked Buoy, Ship's Bell (base: Red Coin, Rusty Pipe) |
| Directions | a plain name that matches its Look line; a heading or a course, short and active, is optional (`docs/PLAN.md` Decisions 2026-10-15) | Compost Bin, Sewing Machine; Deep Roots, Loose Threads, Hard to Port |
| Optional objectives | a short order, like a bounty on a notice board (`docs/scope/14_Optional_Objectives.txt`) | Mind the Meteor |
| Mode, ascension, events | sea weather | Rogue Waves; ascension levels "Depth 1, 2, ..." (renamed from Swell by the user, 2026-10-09); banners "A rogue wave rises" |
| Enemy modifiers | the scope's names (Cursed, Berserk, Aging, Dazzling; `docs/scope/13`). Base elites are named like metalwork (Hardened, Tempered, Superheated, Fractured, Annealed, Molten), for reference |

Text is plain and short, sentence case, and has no numbers in upgrade text (`docs/02`).

## The roster: shape language and sprite reference

**Dimitri's sprite concepts decide each vessel's look.** They are described pixel by pixel in
`docs/concepts/YOSSELE_SPOOL_ULOE.md` (PR #2), with the art in `docs/concepts/art/`. This table carries them into the
pack and doesn't redraw them. Each vessel owns its silhouette and carries it into its towers. Taken shapes are in
`.claude/rules/design.md`.

| Vessel | Shape (from the art) | Sprite reference for the full sheets (`docs/03` §1) | Colours (from the art) | Towers carry |
| --- | --- | --- | --- | --- |
| **Argo** | bulky dome diver, hands touching the body | as shipped in ArgoMod 1.4.0 (look unchanged) | as shipped | brass domes and portholes, as shipped |
| **Yossele** | a round clay head with a see-through hole, a two-leaf sprout on a stalk, glowing green eyes, four long root-like limbs | Buster for the poses (big pose changes on long limbs), Smacky for the head-to-body weight | clay `864B41` to `733E35`, leaf `72CC35` / `6ABE30`, eyes `99E550`, collar `323C39` | round holes and root feet; each Growth stage is a **skin** that adds leaves or sprouts, never a motion |
| **Spool** | a hooded purple plush whose face is a stitched opening, yellow eyes looking out, a pink tongue, white gloves, a severed arm on the floor with cotton stuffing spilling from the wound | Fie (a small humanoid read by its head and hood) | purple `7E4D90` to `633773`, stitches `B5B1B0` / `605F5B`, hollow `490F5F`, eyes `BFBA3F`, tongue `D77BBA` | stitches, pins and tins (the Cookie Tin, the Pin Cushion) |
| **Uloe** | a glass light-bulb head with a face for a filament, a ribbed screw base on a tall lamp post, yellow floating arms of electricity at the sides, a cord to a plug | Sno Sno (an object come alive; legless, it hops) | glass `DAE6FC` / `CBDBFC`, glints `F1EF72`, steel `9BADB7` to `595652`, arms `FBF236`, eyes `3F3F74` | glass, screw rings and cords: the Flood Light's lamp, the Magnifying Glass's lens, the Mirror's panes |

Each must read at 1× zoom against the boat and the darkest map. The concept page (§4) checks them against the taken
shapes. Uloe is the only tall, thin, legless outline on the boat. Its bulb is glass on a post, not Sprigg's plant bulb.

**Where the art departs from the house style**, kept as drawn until Dimitri says otherwise (each is decided at that
vessel's design gate): Yossele's and Spool's eyes glow instead of being dark dots; Yossele's limbs are long (the style
wants 2 px or none); Uloe's head is under half its height. Spool keeps Loom and Thread although Mummer used them: Mummer was
Spool's prototype (`docs/scope/08`). The names stay the user's.

## Towers

Low-poly sleek per `docs/03` §4, from each vessel's 8×8 palette. The silhouette rule: **every tower shows its
vessel's motif on the top or front face** (Yossele's round hole and roots, Spool's stitches and pins, Uloe's glass
and screw rings, Argo's dome), so a player can tell whose tower it is from the game camera. Upgrades keep the silhouette and add one feature
each. States (Growth, Light, a held seed count) show as skins or a counter, not as motion.

## Enemies

New foes are crystal foes like every base foe: a grey stone body carrying one magenta crystal weapon, in the one
8-colour palette all base foes share, named from the armoury (kit `docs/18` §5-6, measured 2026-10-15; this replaces
"one colour family per foe"). The pack's own palette stays off foes. New modifiers ship no skins: they show as an
effect on the foe (`docs/scope/13_Enemy_Modifiers.txt`, decided 2026-10-09; `docs/13` §6). The base elites' looks are
overlay materials in green, white, red, near-black, bronze and orange (`docs/18` §5.5), so a modifier's effect takes a
colour none of them uses; Storm Coral sits between Tempered's red and Molten's orange, so it is not that colour.
