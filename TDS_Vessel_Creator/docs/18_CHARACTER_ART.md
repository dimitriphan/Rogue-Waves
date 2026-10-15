# 18 · Vessel and enemy art: the rules the game's own sprites follow

What a vessel's and an enemy's sprites must look like and how they must be built, **measured on the game's own
sheets** (2026-10-15): every texture and animation table in the game's Addressables bundle was read with UnityPy, and
the drawing code (`Entity_Vis_Animator_Base`, `Helper_Entityvis`, `Entity_Master_Client`) was decompiled. Everything
here is **read from the game's assets or code**; nothing new was tried in game. The per-sheet numbers are in
`reference/art_survey_2026-10-15.txt`; the palettes are `reference/palettes/*.gpl` (GIMP format, Aseprite opens it).

This chapter adds to `03` and does not repeat it: `03` §1-3 keep the sheet names, the generator and the UI art, and
`03` §1 "House style" plus `.claude/rules/art.md` keep the shading rules the user confirmed (opaque, no outline,
Smacky's half-alpha rim is his alone). Where a measurement corrects an older line, §8 says so and the line was fixed.

Game art is never committed. The extracted sheets (113 PNGs: every vessel, unit, minion and enemy sheet, the enemy
projectiles and impacts, the six elite overlay textures) and line-ups sit in the project files at
`/mnt/project-files/research/art_dump_2026-10-15/` (`sheets/`, `previews/`, `animation_tables.txt`).

## 1. How the game draws a character

| Piece | What it does | Source |
|---|---|---|
| `<Being>_Visualproperties` | `Default` (and one `Skinoverride` per skin): `Style` (0 Model, 1 Sprite, 2 Both, 3 Overlay), `Spriteobject`, `Additionalmaterials`, `Displaycolor`, `Sfxend` (death sound) | asset |
| `<Being>_Sprite` (`Spriteobject`) | named folders (`Idle`, `Move`, `Attack`...), each a list of **variants** (facings), each a list of frames; per folder `Loop`, `Reset`, `Priority`, `Speed`, `Spriteto`; per frame an optional `Sound` | asset |
| Frame rate | the animator ticks every `1 / (2 × Defaultspeedmulti)` s and a frame lasts `ceil(2 / Speed)` ticks, so a folder plays at `Defaultspeedmulti` frames a second at `Speed` 1, twice that at `Speed` 2 or more (the cap), and a `Speed` between 1 and 2 plays as 1 (`24` §1); frames advance only while the sprite is visible | `Entity_Vis_Animator_Base` |
| Facing | `Helper_Entityvis.Rotatesprite` picks the variant from the move direction against the camera: **v0 toward the camera heading screen-right, v1 toward the camera heading screen-left, v2 away heading right, v3 away heading left**. Toward/away switches at a dot of 0.4 with the camera's forward, left/right at ±0.2 with its right, so small turns don't flicker. Sprites are never flipped in code: every facing is drawn in the sheet | code |
| Sprite import | pixels per unit **20**, pivot **bottom centre** (0.5, 0) on every vessel, unit and enemy sheet. One exception: Bulwark's 64 px cells pivot at 0.35 or 0.65 so the body, not the crystal, stands on the spot | asset |
| Elite looks | a skin keeps the foe's own sprite object and adds **one material** (`Character <Key> Effect`, shader `Character Billboard Gradient Batchless`) as a second pass over the sprite: a tint gradient plus a scrolling 32×32 pattern (§5.5). Nothing is redrawn | asset, `Entity_Master_Client` |
| `Displaycolor` | not the sprite: the tint of the selection panel's backing and buttons (at 50-75% alpha). Each vessel's is its main colour (Smacky `80B3FF`, Boutman `FFC34C`, Fie `FFA76D`, Buster `FF8080`, Sno Sno `DEDEDE`); every Tide foe's is the crystal `D168E0` | asset, `Game_Ui` |
| Footsteps | a vessel's Move folder plays `General_Footstep` on frames 2 and 5 of each variant (Smacky, Fie, Boutman): one per landing of a two-step cycle | asset |

## 2. Sheet formats, measured

Cells are square; a sheet is 6 frames wide; rows run v0, v1, v2, v3 per folder. Sizes are the opaque box of the
front idle frames; "fps" is `Speed × Defaultspeedmulti` for Idle and Move.

| Kind | Cell | Body (w × h) | Folders (frames per variant) | fps | Examples |
|---|---|---|---|---|---|
| Vessel | 32 | 15-27 × 19-28 | Idle 6, Move 6, Falling 6 or 12 (1-2 variants), Land 12-15 (1 variant), Landquick 1, Attack 2-4 (or Attack1-3 of 3) | 8-10 | Smacky 15-18 × 23-26, Sno Sno 19-25 × 20, Boutman 24-27 × 24-26 (rod included), Fie 21 × 27-28, Buster 16-27 × 19-20 |
| Small foe | 32 | 8-19 × 9-17 | Idle 6, Move 6 | 7-12 | Claymore 9 × 10 (a bomb), Spear 19 × 17 |
| Standard foe | 32 | 18-25 × 20-28 | Idle 6, Move 6, Attack 6 when it winds up | 3-6 | Dagger 18 × 22, Aegis 21 × 28, Ballista 25 × 23, Onager 24 × 24 |
| Heavy foe | 48 | 30-35 × 33-40 | Idle 6, Move 6, Attack 6 | 4 | Maul 30 × 36, Excalibur 35 × 40 |
| Boss foe | 64 | 42 × 64 | Idle 6, Move 6, Attack 7 (last frame on its own sheet) | 3 | Bulwark |
| Flying foe | 32 | 24 × 12 | Idle 6, Move 6, Flying (model) | 7 | Halberd (`Vulture-Sheet`, Style Both, no elite skins) |
| Minion of a vessel | 32 | 13-15 × 16-19 | Idle 6, Move 6, Dead 1 | 6-7 | Boutman's fish, Kippy |
| Unit | 16 (Snobles 12) | 6-12 × 8-12 | Move 6 (Idle often 1 frame), `Movelinear` for dogs | 4-8 | miner, knight, dog, Boutbro |

So: **a 32 px cell holds anything up to about 28 px tall; bigger foes move to 48 or 64 px cells, never to a
stretched sprite**. Feet always touch the bottom row of the cell (no gap on any vessel or foe sheet; the 16 px
units bob 1-3 px) and the body's box is centred on column 16 (±1). Attack sheets are separate textures when their
frame count differs (Boutman 3, Buster 2, Sno Sno 4); foes keep Idle, Move and Attack on one sheet (Ballista
192 × 384 = 12 rows).

## 3. The house rules both share

Measured on all five base vessels and all ten foes (the nine Tide foes and Halberd); `03` §1 has the vessel-side wording.

1. **Opaque, no outline.** No sheet has a dark line round the body. The silhouette edge is the fill colour itself,
   darkened only where that side is in shade (§4.2). Half-transparent pixels: Smacky 33% (alpha 63 and 127, his
   rim), Sno Sno 9% (alpha 126, the snow patch), Buster 1%, every other vessel and foe 0% (units and minions:
   Smacky's miner 5%, alpha 126; the Aurabeast 16%).
2. **Light from the upper left.** Vessels: 32-45% of right-edge pixels and 20-57% of bottom-edge pixels are darker
   than the pixel inside them, against 5-13% on the left. Foes: the light crystal tones sit left and top, the two dark
   ones right and bottom (mean x 0.6-0.9 of the box).
3. **Shade keeps the hue.** Every ramp in the game holds its hue within 2°; a shade is the same hue, usually with more
   saturation (up to 23 points) and less value (up to 25 points) over §4.1's ramps (Boutman fur `ADCDE5` h206 >
   `91B7D4` h206 > `859EB0` h205; Fie's hair `FFA76D` > `FF8F44` > `FF7D25`, all h24, only gains saturation; Buster
   `F0E5C1` > `E8D392`, both h45). The one exception is white, which shades to a pale cold blue (Sno Sno `FFFFFF` >
   `C7E0F2` > `ADCDE5`).
4. **Two or three tones per material**, rarely a fourth; a whole vessel uses 10-21 colours, a foe 8.
5. **Darks are never pure black.** Eyes and the darkest details use warm or neutral near-blacks: `2B2922`, `333232`,
   `4A4949`, `3F3E3E`, `161616` (Boutman's pupils); Fie is the exception, with mid-grey eyes (`828282` over `B1B1B1`,
   §4.1). A few `000000` pixels sit on Buster's and Boutman's parents' sheets (`Buster-Sheet`, `Busterrun`,
   `Boutdad`, `Boutmom`, `Dadair`), which is why `tds_shared.gpl` lists it; don't copy them.
6. **A shared world palette.** 53 colours recur in three or more character families (`reference/palettes/
   tds_shared.gpl`): the whites and greys (`FFFFFF`, `DEDEDE`, `939292`), wood `826D60` / `64544A` / `5A4C42`, skin
   `DA867A`, sand `DFC9A8` / `C5B294`, Smacky's blues `4764A4` / `364E82` / `97AFE5`, the red `D63A44` / `BE2D36` /
   `A12D37`, the leaf green `7BCF80` (its shade `65AD69` is only Smacky's and Sno Sno's), the yellows `F6E43B` /
   `FFF48B`. A new character that takes its neutrals, wood, skin and accents from this list looks like it lives in the
   same world.
7. **Every facing is drawn.** The code never flips a sprite. v1 is v0 heading the other way: the stock foes' v1 is an
   exact mirror (Dagger, Maul: 0 pixels differ from the mirror), but vessels redraw what must not swap (Boutman keeps
   his rod and his shaded side, so his v1 differs from v0's mirror in 112 of 280 pixels; Buster's is a near mirror,
   40 of 275). v2 and v3 are drawn backs.
8. **Idle is small, Move is big.** Idle frames change 2-220 pixels and move the top of the head 0-3 px; Move changes
   45-350 pixels and bobs the head 2-7 px (Sno Sno hops 7). Foes hold some idle frames still (Aegis repeats frame 0
   for frames 4 and 5).

## 4. Vessels

### 4.1 What a vessel is made of

| Vessel | Main ramp (light > shade > dark) | Accent | Eyes | Signature | Colours (opaque, whole main sheet; the `.gpl` lists fewer) |
|---|---|---|---|---|---|
| Smacky | head `FDE791` > `F2D76B` | overalls `97AFE5` > `4764A4` > `364E82`, sprout `7BCF80` / `65AD69` | 1 × 2, `4A4949` / `333232` | sprout, square head, half-alpha rim | 20 |
| Sno Sno | `FFFFFF` > `C7E0F2` > `ADCDE5` | stick arms `826D60` / `64544A` / `5A4C42` | 1 × 2 dots | two snowballs, snow patch | 10 |
| Boutman | fur `ADCDE5` > `91B7D4` > `859EB0` | overalls `D63A44` > `BE2D36`, hat `DFC9A8` > `C5B294` | 2 × 2, `161616` over `333232`, a 1 × 2 white glint on the inner side | hat brim, rod | 18 |
| Fie | hair `FFA76D` > `FF8F44` > `FF7D25` | shirt `D63A44` / `A12D37`, skin `FFDFC1` > `FFCA99` | 1 × 2, `828282` over `B1B1B1` | hair over one eye | 21 |
| Buster | `F4ECD3` > `F0E5C1` > `E8D392` | collar `9FF3E9` / `19DFCE`, tag `F6E43B` | 1 × 2, `2B2922` over `4E4B3E` | collar, ear, tail | 17 |

Value: every main colour sits at 90-100% brightness (HSV value); only shades, accents and darks go lower. Every main
colour's saturation stays under 60% (Fie's hair is the most saturated at 57%).

### 4.2 Rules for a vessel (new or redrawn)

- **Must** be chibi: head or head-mass half the height or more, 19-28 px tall (Buster's sit 19-20), 15-27 px wide inside a 32 px cell,
  feet on the bottom row, centred on column 16.
- **Must** use one light main colour (value 90-100%) with a 2-3 step same-hue ramp, one saturated accent, near-black
  eyes of 2-4 pixels, and take neutrals, wood and skin from the shared palette (§3.6).
- **Must** shade the right side and the underside of each form and under whatever overlaps (a hat brim's row of
  shade on the face, hair over the forehead); the left and top edges stay the fill colour.
- **Must** carry one signature prop or feature that reads at 1× (hat, hair, collar, sprout).
- **Must** draw all four facings (v1 redrawn where a prop or the shaded side would swap) and give Move a hop with
  `General_Footstep` on the landing frames.
- **Must not** use an outline, a half-alpha rim (Smacky only) or a ground patch (Sno Sno only), pure black, a hue
  shift in its shading, or any colour in the foes' magenta band (§6).
- **Must not** go over 28 px tall or use a bigger cell: the boat, the vessel panel and the landing strips are all
  built round 32 px vessels.

## 5. Enemies: the crystal foes

### 5.1 One family, one palette

Every Tide foe (Dagger, Spear, Aegis, Ballista, Onager, Claymore, Maul, Excalibur, Bulwark, and Halberd off the Tide
tables) is the same creature: a **small grey stone body carrying a magenta crystal that is its weapon**, and the
whole family shares **one 8-colour palette** (Claymore's bomb adds three greys and a darker rim; Bulwark a bronze band) (`reference/palettes/tds_foes.gpl`):

| Role | Colours |
|---|---|
| Crystal: highlight ridge, top face, front face, side face, far side | `F6BAFF`, `DA89E6`, `D168E0`, `A54FB1`, `8D4497` (all h292-293) |
| Stone body: lit, shade, deep | `8C8C8C`, `6D6C6C`, `646464` (neutral grey) |

Bulwark, the boss, alone adds a three-tone bronze band (`C3B0AD`, `AB9B98`, `938482`) round its crystal. No vessel,
unit or minion uses any of the five crystal colours, and none uses a saturated (over 20%) hue between 270° and 320°:
**magenta means enemy**. The friendly side's "purple" (Fie's purple knights, `Purple-Sheet`) is a blue-violet at
h267 (`9677BB`, `8764B0`). The tutorial's Training Dagger is the same shape in wood (`826D60`) with crystal veins.

### 5.2 Anatomy

- The **crystal dominates**: 54-76% of a foe's opaque pixels (Dagger 76%, Maul 63%, Bulwark 57%, Ballista 54%). The
  weapon is the silhouette, and it matches the name: Dagger two blades, Spear a lance and a vane, Aegis a tower shield,
  Ballista a crossbow beam, Onager a slung boulder, Claymore a lone bomb crystal, Maul two hammer slabs, Excalibur a
  ring halo and a sword, Bulwark a crystal pillar wearing the body.
- The **body is a blocky grey stick figure**: a torso of 2-3 stone tones and two legs 2 px wide, often drawn apart
  from the body. It has no face.
- The **only "eye" is a socket**: a 2 × 2 square of `8D4497` or `A54FB1` cut into the crystal's front face.
- The crystal is **faceted, not shaded**: the top face `DA89E6`, the front face `D168E0`, a 1 px `F6BAFF` ridge on the
  top and left edges, the side face `A54FB1`, and whatever is behind the body (the far blade, the far hammer) in
  `A54FB1` / `8D4497` as a whole. Depth reads by tone: near pieces light, far pieces dark.
- Edges are not darkened (0-8% of edge pixels on any side): the facets do the shading.

### 5.3 Motion

- Frame rate scales with weight: Bulwark and Aegis 3 fps, Maul and Excalibur 4, Ballista and Onager 5, Dagger 6,
  Spear 7, Claymore 12. A vessel runs at 8-10. Slow frames read as heavy.
- Idle is a 1-2 px bob of the crystal over a still body; Move swings the legs and tips the crystal.
- **An attack is a wind-up and a flash**: frames 0-3 lift or draw back the weapon, frame 4 is the hit, drawn into the
  sprite as a pale `F6BAFF` and white burst with shards (Maul's slam lines, Excalibur's ring beam, Ballista's bolt
  flash), frame 5 settles. Only foes with a bespoke wind-up AI have an Attack folder (Ballista, Onager, Maul,
  Excalibur, Bulwark); Dagger, Spear and Aegis fight with Idle and Move alone. Time the folder to the AI: a slam
  plays 1.5-2.3 s (Maul and Excalibur 6 frames at 4 fps, Bulwark 7 at 3), about the AI's wind-up (`13` §1: 1.3-1.6
  s), so the flash lands with the hit; a shooter's plays 0.6-1.2 s (Ballista `Speed` 2: 6 frames at 10 fps; Onager
  `Speed` 1.5 plays as 1: 6 frames at 5 fps, §1).
- Death: `Enemy_Death`, or `Enemy_Deathlarge` for the 48 px foes. Projectiles and impacts are their own small strips
  in the same palette (`Ballistabolt-Sheet` 96 × 16, `Maulimpact-Sheet` 160 × 32, `ClaymoreExplosion-Sheet` 224 × 32).

### 5.4 Foes made from friends

The game's foe versions of its friendly characters (sleeping Boutman on Artrio `enemyBoutman-Sheet`, his fish
`EvilGubbyzmainz-sheet`) are **the same sprite with red eyes**: the eye pixels become `D63A44` (Boutman's darker
pupils `BE2D36`) and nothing else changes; their `Displaycolor` is `FF8080`. That is the house way to turn anything
hostile.

### 5.5 Elites are overlays, not drawings

All six elite skins on every foe point at the foe's own sprite and add one shared material (values in the survey,
part 3): Hardened a green glow with a slow downward pattern, Superheated a faint white with fast diagonal streaks,
Tempered red with a rising cross pattern, Fractured near-black with a churning pattern, Annealed a faint bronze grid,
Molten orange-red with a slow downward pattern. Their colours are taken: green, white, red, black, bronze, orange.
For the art this means:

- a new foe gets the six elite looks **for free** by carrying the six `Skinoverride` entries (a clone of a stock
  foe's `Visualproperties` keeps them); no elite sheet is drawn;
- the overlay needs flat area to show: a foe smaller than about 10 × 10 px (Claymore) barely shows it;
- Fractured darkens to near black, so a foe must not itself be dark, or that elite becomes invisible.

### 5.6 Rules for a new enemy

- **Must** be a crystal foe: a grey stone body (`8C8C8C` / `6D6C6C` / `646464`) carrying one magenta crystal weapon in
  the five crystal colours, nothing else (a boss may add one 3-tone band, like Bulwark's).
- **Must** take its name from the armoury (`13` §1) and its silhouette from that weapon, distinct from the nine taken
  shapes in §5.2 and Halberd's.
- **Must** pick its cell by tier: 32 px up to about 28 px tall, 48 px for a heavy, 64 px for a boss; bottom-centre
  pivot unless the body stands off-centre (Bulwark).
- **Must** draw Idle and Move in all four facings (v1 may be v0's exact mirror), and, if it winds up, an Attack folder
  whose hit frame carries a pale flash; frame rate from its weight (§5.3).
- **Must** carry the six elite `Skinoverride`s and stay mid-to-light so every elite reads on it.
- **Must not** have a face, eyes, a mouth or any vessel-like expression (the socket only); must not use any colour
  outside the 8 (no new hue, no black, no white except inside an attack flash).
- **Must not** be smaller than about 10 px in its smallest dimension unless, like Claymore, it is a projectile that
  lives a second.

## 6. What separates a vessel from a foe

| | Vessel | Crystal foe |
|---|---|---|
| Palette | its own pastel family, 10-21 colours, from the shared world palette | the 8 foe colours, shared by all |
| Reserved hue | anything but 270-320° | magenta h292 |
| Face | eyes of 2-4 near-black pixels, expression by eye position | a 2 × 2 crystal socket, no expression |
| Proportions | chibi: head half the height or more | small stick body, the weapon is 54-76% of the sprite |
| Light | a shade band on the right and under overlaps; almost no highlights | faceted: light top and left faces, a pale ridge, dark far side |
| Size | always a 32 px cell, 19-28 px tall | 32, 48 or 64 px by tier |
| Speed | 8-10 fps | 3-12 fps by weight |
| Folders | Idle, Move, Falling, Land, Landquick, attacks, abilities | Idle, Move, Attack if it winds up |
| UI colour | its main colour | `D168E0` |
| Hostile version | red eyes `D63A44`, darker pupils `BE2D36` (§5.4) | - |

## 7. Pack notes (Rogue Waves)

- **Spool's purple sits in the foes' band.** The concept and walk sheets use `7E4D90` / `9363AE` / `784B94` (h277-284,
  measured on `SpoolWalk-Sheet`), next to the crystal's h292 and past the friendly purple's h267. It is darker and
  greyer than the crystal and Spool has a face, so it may still read. **Dimitri, 2026-10-15: leave Spool's colours as
  they are for now.** The exception is his; the magenta rule still binds every other new design.
- **New modifiers' look.** Scope 13 says modifiers show as an effect, not a new skin. Kept: an entity holds one skin,
  so a modifier skin would hide a stock elite's look on a foe that has both. A third way the game's code allows is to
  add a cloned elite material straight to the foe's SpriteRenderer from the client, as `Entity_Master_Client` does for
  skins, which stacks with them (untested). Whatever the route, pick a colour the elites don't use (§5.5): Storm
  Coral (`E2725B`, `docs/STYLE.md`) sits between Tempered's red and Molten's orange.
- **New enemy types** come later (scope 01, 13). When they do, they are crystal foes per §5.6; the pack's own palette
  (`docs/STYLE.md`) stays on the pack's UI and run objects, not on foes.

## 8. Corrections this survey made

| Where | Was | Now |
|---|---|---|
| `03` §1 | v0 and v1 "near-identical" front views | v1 is v0 heading screen-left (§1, §3.7) |
| `03` §1 | body about 16 px wide and 20-24 px tall | 15-27 × 19-28 (§2) |
| `03` §1 | "Edges are darker shades of the fill" | only the right and bottom edges, where the shade falls (§3.2) |
| `13` §5 step 4, §6 step 4 | a new foe's art needs the six elite skins drawn; a new skin means redrawing every enemy | elite skins are material overlays: no drawing (§5.5) |
| `13` §10 question 5 | enemy art format unknown | answered (§2, §5) |
| `docs/STYLE.md` "Enemies" | one colour family per foe; future skins tinted Storm Coral | one palette for every foe; Storm Coral is close to two elites (§7) |
| shading guide of 2026-10-09 (point 4 of the concept-art page made in the project's "Vessel concept art" thread; not in the repository) | "shift the hue, not just the value" | the game keeps the hue and adds saturation; only white shades toward blue (§3.3) |

## 9. How this was read (repeatable)

1. Connect the game's `TDS\TDS\DATA\StreamingAssets` folder in the thread (read-only) and stage
   `aa/StandaloneWindows64/defaultlocalgroup_assets_all_*.bundle` (about 300 MB, the only bundle with game content)
   into the container.
2. `python3 -m pip install UnityPy`, then in a script kept outside the staged folder and run with `python3 -I`:
   `env = UnityPy.load(bundle)`; every `Texture2D` saves with `obj.read().image.save(...)`; `Sprite` gives `m_Rect`,
   `m_PixelsToUnits`, `m_Pivot` and its texture; `MonoBehaviour`s read with `obj.read_typetree()` (the game ships type
   trees), which gives every `Visualproperties`, `Spriteobject`, `Statproperties` and `Entityobject` by `m_Name`, with
   references as path ids. Materials give `m_SavedProperties` (colours, floats, textures). About 20 s for the bundle.
3. Measure with Pillow (box, colours, alpha, edge-versus-inside luminance per side); the survey's parts 1-2 are the
   output of exactly that.

Keep the extracted art in the scratchpad or the project files, never in the repository.

## 10. Not yet checked in game

- How the elite overlays look on a sprite that is not a stock foe (expected identical: the material only reads the
  sprite's alpha and colour).
- Whether a material added to a foe's SpriteRenderer from a mod (§7) survives the game's own skin changes.
- How Spool's purple (kept, §7) reads next to the crystal foes at the game's camera distance.
