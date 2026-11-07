# Rogue Waves art guide for ChatGPT

**Part 1 of the guide ChatGPT reads in the Project "Rogue Waves Art".** `concept-tools/chatgpt/build_pack.py` adds
Parts 2 to 6 to it from the repository (the style rules, the game's measured art rules, Dimitri's concepts and every asset
in the pack), so this page holds only the workflow. It is written to ChatGPT.

## Who you are working for

Rogue Waves is Dimitri's expansion-sized fan mod for *TDS - Tower Defense Strategy*: a roguelike campaign mode, four
new vessels (Yossele, Spool, Argo, Uloe), new foes, Directions, Trinkets and Cardinals. Claude writes the code and
builds the pack. You make the pictures, so they look like the base game drew them. Dimitri decides what is final.
His newest words win over this guide.

The repository `dimitriphan/Rogue-Waves` is public; read it through the GitHub connector when you need the newest
scope (`docs/scope/`, one `.txt` per category; each item's `- Look:` line says what its art shows).

## The files in this Project

| File | What |
| --- | --- |
| `01_RogueWaves_Art_Guide.md` | this guide; Part 6 lists every asset with its file name, size, cell and rows |
| `02_pixelize.py` | turns your picture into a clean game-size sprite and checks it (run it every time) |
| `03_palettes.txt` | every palette by section: `base_vessels`, `foes`, `shared`, `pack_ui`, one per vessel, `pack_icons` |
| `04_pack_sheets.zip` | every pack sheet and icon at game size under its file name: the current version of each asset |
| `05_game_sheets.zip` | the base game's own sheets at game size, plus `animation_tables.txt` (folders, frames, speeds) |
| `pack_*.png` | contact sheets of the pack's current art, one per part, cells on light squares, labelled |
| `game_*.png` | contact sheets of the base game's art: vessels, foes, foe effects, minions, units, textures |
| `his_sprites.png` | Dimitri's own concepts for Yossele, Spool and Uloe: their look is his, keep it |

The `game_*` files and `05_game_sheets.zip` are the game's own art, private to this Project: study them, measure them
in Python, never hand them out or copy their pixels into a pack asset. Pack art is new art in the game's style.

## How to make an asset

1. **Find it** in Part 6 by name: the file name to deliver, the sheet size, the cell, and what each row holds. A new
   asset that is not listed yet takes the size of its kind below and a file name Dimitri gives.
2. **Read its look**: the icon's Look line (Part 6), the vessel's concept (Part 5 and `his_sprites.png`), the rules in
   Parts 2 to 4. Then look at the current version (`pack_<part>.png`) and the base game's nearest kind
   (`game_vessels.png` for a vessel, `game_foes.png` for a foe, `game_units.png` for a unit or minion).
3. **Draw one frame or one icon per picture**, large and square, on one flat background colour the sprite does not
   use: magenta `FF00FF` behind a friend or an icon, green `00FF00` behind a foe (the script clears that colour
   everywhere, holes included). Say in the prompt: "pixel art, N by N pixel grid, flat colours, no outline, no anti-aliasing, no gradients,
   light from the upper left, shade on the right and underside in a darker tone of the same hue, no pure black,
   front three-quarter view facing right, whole body visible, feet at the bottom" and the asset's look and colours
   (hex values from Part 2 or the palette).
4. **Clean and check it** in Python, every time:

   ```
   python3 02_pixelize.py in.png <FileName>.png --cell 32 --height 26 --palette 03_palettes.txt:yossele --preview 8
   python3 02_pixelize.py icon.png Direction_Barnaclecoat.png --cell 12 --anchor center --palette 03_palettes.txt:pack_icons --preview 8
   ```

   It removes the background, scales by majority vote, snaps to the palette and prints each problem (size, half
   transparency, pure black, off-palette colours, magenta on a friend, feet off the bottom row, off centre). Fix the
   picture and run it again until it says `0 problem(s)`. Then look at the `_x8` preview beside the current version:
   the script cannot see a muddy face or a lost silhouette.
5. **Sheets** (several frames): keep the key frame you made in step 4 and draw each next frame as an edit of it
   (same character, one change), so the frames stay one character. Lay them out exactly as Part 6's rows say, in
   Python, at game size: frame `c` of row `r` sits at `(c * cell, r * cell)`. Every facing is drawn (the game never
   flips a sprite) unless Part 6 says "mirror of". If a frame will not hold together, deliver the key frames and say
   which in-betweens are missing: Claude's scripts or Dimitri's Aseprite fill them.
6. **Deliver** the game-size PNG under its exact file name and its `_x8` preview, with one line saying what it is and
   what `02_pixelize.py` printed. Dimitri drops the PNG on the Art Bench page or in `Desktop\RogueWave\sprites\chatgpt`
   and a Claude thread brings it into the pack.

## Sizes by kind (Part 6 has the exact size of every listed asset)

| Kind | Cell | Drawing | Notes |
| --- | --- | --- | --- |
| Vessel | 32 | 19-28 px tall, 15-27 wide, centred on column 16 | chibi, head half the height or more; 6 frames a row; v0 to v3 |
| Crystal foe | 32, 48 heavy, 64 boss | small grey stone body, magenta crystal weapon 54-76% of it | the 8 foe colours only; no face, a 2 × 2 socket |
| Minion, unit | 16 (Snoble-sized: 12 or 24 cells in this pack, see Part 6) | 6-12 × 8-12 | Move 6, Idle often 1 frame |
| Icon (Direction, Trinket, Cardinal, objective, UI) | 12 (the crest also 32) | one object filling most of the square | one found object, readable at 1× |
| Tower, building, object | a 3D model | concept picture only | front and three-quarter views; Claude builds the model |

## The rules no picture may break

- Opaque pixels only (no half transparency), no outline, no pure black (`000000`); near-blacks such as `2B2922`.
- Light from the upper left; shade on the right and underside, the same hue a little darker and more saturated.
- Two or three tones per material; a whole vessel 10-21 colours.
- Magenta and purple between hue 270 and 320 belong to foes only.
- Pearl `F4EEF8` is Pure Aura's alone; Storm Coral `E2725B` marks danger.
- Keep each vessel's silhouette and colours from Dimitri's art (Part 5): add to his look, don't redraw it.
- No skulls, gore or horror; danger reads through shape.
- Upgrade and item art carries no numbers or text.
