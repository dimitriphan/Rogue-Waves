# Spool in pastel green: the main sprite, as an example (2026-11-05)

**A proposal, not wired.** Dimitri, 2026-11-05: "Spool's current color scheme is too similar to enemy units. Swap
Spool's color scheme to pastel yellow, but only recolor the main sprite for now so I can see an example." After seeing the yellow
he asked, 03:00Z: "Change the color to pastel green instead." The game still draws Spool in purple from `SpoolCharacter.P` (`Assets/SpoolMod/Editor/SpoolCharacter.cs`); nothing here is read
by a build.

![before and after](preview_8x/Spool_Pastel_compare.png)

| File | What |
| --- | --- |
| `game_size/Spool_Pastel.png` | 32 x 32, the main sprite (his reshade, the rest frame and the icon) in the new colours |
| `aseprite/Spool_Pastel.aseprite` | the same, one frame, layer "TEMP art" |
| `preview_8x/Spool_Pastel_8x.png` | 8x on the house checker |
| `preview_8x/Spool_Pastel_compare.png` | now and proposed side by side at 8x, at game size and 3x on grass beside the foe palette, the palette map and the distances below |

Generator: `concept-tools/temp_art/spool_pastel.py`. It reads `Front` and `P` from `SpoolCharacter.cs`, so the "now"
picture is exactly what the game builds (it equals the project files' `concepts/vessels_2026-10-09/v3_flat/game_size/spool_reshade.png`
pixel for pixel, checked 2026-11-05).

## Palette map

Only the purple family changes: 147 of the sprite's 195 pixels, none moved. The shade keeps the hue within 2 degrees
and gains saturation, the mouth's dark is a deep green near-black (kit `docs/18` §3). The hue, mint h149, sits in the
gap between the greens the game and the pack already use (below).

| Key | Role | Old | New | Old h s v | New h s v |
| --- | --- | --- | --- | --- | --- |
| `a` | plush | `9363AE` | `A4EBC7` | 278 43 68 | 150 30 92 |
| `b` | plush shade | `784B94` | `6DC99A` | 277 49 58 | 149 46 79 |
| `m` | inside the mouth | `41174F` | `1F3830` | 285 71 31 | 161 45 22 |
| `n` | mouth edge | `4A1D5A` | `25473C` | 284 68 35 | 161 48 28 |

Unchanged: teeth and cuffs `E4DEDA` / `9A938E`, gloves `F6F3F8` / `D8D1E2`, cotton `FFFFFF`, tongue `E58CC0` /
`C96CA6`, eyes `E6D85A`. The severed arm on the floor keeps its white cotton end against the new shade.

## What it was compared against

- **Every Tide foe** (kit `docs/18` §5.1, `TDS_Vessel_Creator/reference/palettes/tds_foes.gpl`): the magenta crystal
  `F6BAFF` `DA89E6` `D168E0` `A54FB1` `8D4497` (h292) and the stone greys `8C8C8C` `6D6C6C` `646464`. Spool's plush was
  13 (shade 11) CIE76 units from the crystal's `8D4497` and 14-15 degrees of hue from it; the green is 43 (shade 44)
  from the nearest foe colour, a grey, and 142-143 degrees of hue from the crystal. The mouth's dark sits about as far
  from the stone grey (24, edge 22) as the old one sat from the crystal (29, edge 24); it is small and inside the hood.
- **The elite skins** (material tints over foes, `reference/art_survey_2026-10-15.txt` part 3): Hardened green,
  Superheated white, Tempered red, Fractured near-black, Annealed a faint bronze (h22, 6% alpha), Molten orange-red.
  **Hardened is green** (`_Tintcolor` 1.75, 11.98, 1.75: h120), so a Hardened foe glows green. Spool's mint is 30
  degrees away, much paler, and Hardened stays on a grey and magenta crystal body; still, this is the one foe look to
  check by eye next to Spool in game.
- **Greens already taken** (`tds_base_vessels.gpl`, `docs/STYLE.md`): the shared leaf green `7BCF80` / `65AD69` (h124,
  Smacky's sprout, Sno Sno), Yossele's leaf `72CC35` / `6ABE30` and eyes `99E550` (h90-96), Buster's collar `9FF3E9` /
  `19DFCE` (h173). Mint h149 sits in the middle of that gap (25 degrees from the leaf green and from Buster's collar,
  53 from Yossele). Green on the map's grass is the other thing to look at in game: on the house checker the paler
  plush and the darker shade both stand off it.
  The kit's rule (`docs/18` §4.2, no saturated hue in 270-320 degrees) now holds for Spool, so its §7 exception would go
  once the follow-up lands.

## Follow-up, if Dimitri says yes

Swapping the four keys in `SpoolCharacter.P` recolours every 32 px vessel frame at once (idle, walk, attack, Patch,
fall, landing, the boat mini, the icon, the end-screen poses), since they are all drawn from it. Beyond that, each its
own yes or no:

1. Spool's vessel sheets through `SpoolCharacter.P` (the four keys above): one code change, then a PC round to look.
2. The UI colour: `Purple` in `ModSource/Spool/Code/04_Content.cs` and `Code/13_Journal.cs` (`Displaycolor`, journal), the death
   puff's `9363AE` in `Assets/SpoolMod/Editor/SpoolBuilder.cs`, the splash and tab icon (drawn in the eyes' yellow: may want a darker line).
3. The Thread Head and the four Crafted (`Art/Temporary/game_size/`, his 2026-11-05 picks), purple and lilac now.
4. Spool's thread, which is lilac everywhere: the ground tuft, `SpoolClient.ThreadColor`, the needle's tail, the
   Patch thread (`l`, `L` in `P`), and the models' felt (the palette's `9363AE` in `Assets/SpoolMod/Editor/SpoolLowpoly.cs`, built into `Assets/SpoolMod/Art/Models/Spool_Palette.png`).
5. The docs that name his purple: `docs/STYLE.md` roster row, kit `docs/18` §7.
