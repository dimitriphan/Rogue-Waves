# 02 · Design rules: making a vessel feel like Blockage made it

A modded vessel should be indistinguishable in quality from Smacky, Sno Sno, Boutman, Fie and Buster: same structure,
same numbers, same tone, same polish. These rules come from the base-vessel survey (`reference/`) and from the user's
feedback on Sprigg, Argo and Mummer. Fill in `ModSource/<Name>/DESIGN.md` (the scaffold's form) against them before
writing code.

## 1. Identity

- **Name**: one capitalised word, lower case after the first letter (`Smacky`, `Boutman`, `Sprigg`, `Mummer`), 3-15
  letters, not already a vessel. It becomes every id (`Hero_<Name>`, `Tower_<Name>loom`).
- **Title ▪ difficulty**, shown as `Displaysub` and `Displayskill`: a playful epithet and one of **Basic**, **Simple**,
  **Intermediate**, **Advanced** ("Good Ol' Fella ▪ Basic", "A Very Fast Dog ▪ Advanced", "Strings Attached ▪
  Advanced"). Difficulty follows complexity: a custom resource, a picker ability or units to manage = Intermediate or
  Advanced.
- **Silhouette and shape language** distinct from every vessel on the boat: Smacky square, Sno Sno round, Buster a
  dog, Boutman a cat in overalls, Fie a knight, Sprigg a bulb in a pot, Argo a bulky dome-helmeted diver, Mummer a
  triangle costume. Carry the shape into the towers (Mummer's A-frames and pennants, Argo's portholes).
- **Palette**: two or three main colours plus one accent. The accent is used for the hero entry `Color`, button
  colours, the journal accent and the thread/effect colour.
- **Lore**: one or two lines of story for the journal (`Log` desc), not mechanics. Smacky's: "Smacky, you are a great
  Vessel. Let your work bring purification to our home." Mechanics go in the tooltip rule.
- **Respect the concept.** If the user supplied a concept, keep its fantasy, names and the feel of every ability.
  Change only what is infeasible in the engine, unreadable on screen, or unbalanced, and list every change with its
  reason in DESIGN.md ("Changes from the concept, and why"; `examples/Mummer_DESIGN.md`). Tell the user about them.

## 2. The vessel (hero)

| | Shipped values | Rule |
|---|---|---|
| Health | Sno Sno 25, Smacky/Fie/Buster/Boutman 50 | 25-65; ranged low, bulky melee high |
| Speed | 2 (Sno Sno) - 3.5 (Buster) | 2-3.5 |
| Damage | 1.5 (Buster) - 4 (Smacky) | 1.5-5, lower when the vessel has another damage source |
| Range | melee 1.5-2, ranged 3 | melee 1.5-2 (2 = "with reach"), ranged ~3 + Projspeed 15 |
| Attackspeed | 0.5-0.75 | 0.4-0.75 |
| Buildrange | 2-3 | 2-3 |
| Sellrate | 0.5-0.75 | 0.5-0.75 |

- Attack AI: **melee** = `Entity_Ai_Mob_Combat` like Smacky; **ranged** = `Entity_Ai_Mob_Ranged` with the projectile
  named in the Attack command, like Sno Sno (never both: Sprigg once hit twice, melee from 3.5 tiles plus its seed).
- `Display_Desc` follows the pattern "Fast, single-target melee", "Regular, single-target ranged".
- All hero stats stay hidden (copy the template's stat styles).
- **Special rule** (optional but recommended; Sno Sno, Boutman, Buster have one): a tier -3 upgrade
  `Hero_<Name>_Tooltip_Upgradeproperties` in `Upgradesinit`, named like "Snoborne", "Good Dog", "The Show Must Go On",
  with one comma-phrase line summarising every rule of the vessel ("Melee with reach, foes felled near a Loom leave
  Scraps of Thread, ..., can HOOK foes").
- **Ability** (optional): a hero command on Buster's Dash setup: Style Button, keys `["6","7"]`, `Onpressed`, priority
  6, the vessel's colour, a 12×12 icon. If it targets something, make it a **picker** (press, point, click, like Sell)
  whose hotkey falls back to the thing under the cursor or the nearest valid one; the server validates everything. Show
  its cooldown by greying the button. Never make it an invisible auto-cast.
- **Custom resource** (optional, e.g. Sprigg's Sap, Mummer's Thread): a Counter stat on the vessel, kept per player on
  the server, with a cap, a clear way to earn and spend it, an owner-only warning when it can't be spent, and a reason
  to spend it (Mummer's unravels each Tide). Don't punish the first Tide (Mummer's unravel once halved the starting
  Thread before anything was built).

## 3. Four buildables

Four, shown in `Towerdisplay`, covering different jobs (Boutman shows three; his fish take the fourth place). A vessel
built around units follows `docs/12_MINION_VESSELS.md` as well. Shipped mixes:

| Vessel | Offence | Defence | Economy | Units / utility |
|---|---|---|---|---|
| Smacky | Archer Tower (Red 25), Bomb Tower (Red 25 + Blue 25) | Wall (Blue 20) | Mining Hut (Yellow 35, miners) | |
| Sno Sno | Sno Cannon (Red 30), Heater (Red 60) | Ice Brick (Blue 15) | Sno Box (Blue 10 + Yellow 25) | |
| Fie | | The Wall (Blue 20) | Silo Pipes (Yellow 35), Rest Stop (Yellow 50) | Knight's Keep (Red 60, knights) |
| Buster | Pedestal (Red 25) | Speed Pad (trap, Blue 25) | Checkpoint (trap, Yellow 50) | Garage (Red 60, dog) |
| Mummer (mod) | Marionette Cross (Blue 30 + Red 15) | Curtain (trap, Blue 20) | Loom (Yellow 40) | Puppet Stage (Red 50, units) |

- **Cost colour = job**: Red offence, Blue defence/control, Yellow economy. Base cost 10-60 aura, one colour (two for
  a hybrid). Unit producers cost the most (60).
- **Stats** list only what the building uses, with `Leveladd` on Health (+25 to +150 per level) and the main damage
  stat (+0.5 to +2). Shipped bands: Health 20-100, Damage 0.3-5, Range 3-6.5 (towers), Attackspeed 0.2-1.
- **Tags**: `Tower` (or `Trap`) + a role (`Towerfighter` attacks, `Gatherer` economy) + a family tag
  (`<Name><building>`) the brains use to find siblings.
- **Units** (from a unit building): stats like the miner (5 HP), knight (25 HP), dog (50 HP); stock fighter AIs; leashed
  or ordered by the brain; respawn 5-10 s; count via `Units`.
- **Traps** walk-through (Buster's pad block `Trap_Busterpad_Block`; `Trap_Smackywall_Perma` as a fallback). Ground
  effects and leftovers are networked beings (Sno, Bramble, Perma-Wall stump), never client-only decorations.
- Prefer mechanics the engine already has: stat modifiers, skins, unit spawns, effect upgrades, networked beings, the
  game's debuff looks, its own projectiles. Things the engine does **not** simulate (sightlines, team-switching enemies,
  physics knockback, strings as attackable objects) need a native substitute; see Mummer's table in `examples/`.

## 4. Upgrade trees

Per buildable, like the shipped ones (counts and costs measured from the game's data):

- **7-11 upgrades** (shipped 8-12; mods use 7), tiers 2, 3, 4, 5.
- **1-3 tier-2 roots** (use 2), curated `next` lists that **cross over** (a tier-2 can lead to both tier-3s),
  at least one branch that **ends early** (a tier-3 or tier-4 with no next), and **one tier-5 capstone** (sometimes two).
- **Costs by tier** (sum of aura), in the building's colour, a second colour allowed from tier 3:

  | Tier | Shipped range | Median | Mods used |
  |---|---|---|---|
  | 2 | 15-75 | 40 | 20-35 |
  | 3 | 25-500 | 100 | 60-150 |
  | 4 | 75-750 | 300 | 260-450 |
  | 5 | 1500-5025 | 3000 | 2800-3500 |

- **Ids**: `<Being>_<Key><Tier>_Upgradeobject` (`Tower_Mummerloom_Golden5_Upgradeobject`); each upgrade has its own
  model (`<Being>_<Key><Tier>`) that keeps the base silhouette and adds one clear feature.
- **Names** are puns or flavourful two-word names ("Long Bow", "Eagle Eyes", "Rapid Fire", "Mining Operation",
  "134° - 57°"); **descriptions** are one short phrase, often with a comma ("Increased damage, dealt over time",
  "Hire another miner", "Slows enemies down on contact"). No numbers in the text; the journal shows the values.
- **Params** follow the game's naming: `Stat_Add`, `Stat_Multi`, `Stat_Pmulti`, `X_Base`, `X_Max`, `X_Percent`,
  `X_Duration`. A param that changes a **unit's** stat on a unit building must not reuse the building's own stat name
  (Mummer's Method Acting first used `Health_Multi`, which would have buffed the Stage itself; it became
  `Puppethealth_Multi`).
- The capstone changes how the building plays (Puppet Master holds until death; Final Act stuns a crowd once a Tide),
  not just +numbers.

## 5. Text and tone

- Short, light, punny, never grim. Tooltips are comma phrases; warnings are 2-6 words ("Hook is recoiling", "Not
  enough Thread to stitch").
- English text is the key; just write English.
- Owner-only warnings for refusals (out of range, on cooldown, can't afford): `Helper_Uitools.Warning` on the owner's
  machine only.

## 6. Feel and readability

- **Every mechanic shows on every player's screen**: an animation, an effect, a sound, a debuff look, a skin.
  Example: Mummer's HOOK plays a throw animation, draws a thread to the target, reels it in, puts a plum marker over it
  and rings a bell when it pays out.
- **Honest visuals**: an area effect is drawn at its real radius (Sprigg's spore cloud once looked half its size);
  debuff markers sit above the tallest foe sprites (Mummer's marker was hidden behind a Dagger at 1.15).
- **Sound is a quiet accent**, at or below the game's own levels (`docs/03`).
- Buttons show state: cooldown grey, current mode in the label (COSTUME shows "BALLISTA 4").

## 7. Balance process

1. Place every number inside the shipped bands above, next to the closest shipped equivalent.
2. Price custom resources so the main loop runs often (Mummer's units cost 2/4/5 Thread, not the concept's 3/9/15).
3. Write the self-test so each mechanic's numbers are checked in game (`docs/08`).
4. Ask the user to play it; they played Sprigg 1.1 online and sent concrete feedback that drove 1.2 and 1.3.

## 8. What the user has asked for (keep doing it)

- **Sprites** may follow **any base vessel's style** (Smacky, Sno Sno, Boutman, Fie, Buster): pick the one closest to
  the concept and match its proportions, palette and finish, inside the conventions all of them share (`docs/03` §1).
  Feedback on past vessels, for the same kind of concept: Sprigg got a dopey face (eyes wide apart, one a pixel lower)
  and soft rounded masses like Sno Sno; Argo's hands had to be connected to the body and the silhouette bulky "like
  Nautilus".
- **Towers: always "low poly sleek" like Sno Sno's or Buster's, never voxel.** Voxels only look good when handmade;
  the generated voxel towers (Sprigg 1.1) were called messy. This rule has no exceptions, whatever vessel the sprites
  follow (Smacky's sprites are fine as a reference, his voxel towers are not).
- Quiet sounds (Sprigg's first sounds were about 20 times too loud).
- Abilities you aim (WATER, HOOK pickers), honest area visuals, ideas in the spirit of Thornroot (things bursting out of
  the ground under the enemy).
- Journal pages for everything; the vessel on the boat like the others; online play.
