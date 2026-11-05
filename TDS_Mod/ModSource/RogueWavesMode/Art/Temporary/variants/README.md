# Rogue Waves mode UI: three versions of each temporary asset (2026-10-28)

**Everything here is temporary and nothing here is wired.** Each `v1`, `v2`, `v3` folder holds a complete replacement for
the files in `../game_size/` (same names, sizes and frame layouts), for Dimitri to pick from one animation at a time;
`../game_size/` stays the one in game until he does. How picks are made and applied: `docs/TEMP_ART.md`, "Versions to
pick from". Each folder has `game_size/` (the drop-in PNGs), `aseprite/` (layer "TEMP art", a tag per animation and
view) and `preview_8x/` (enlarged sheets).

| File | v1 | v2 | v3 |
| --- | --- | --- | --- |
| `RogueWaves_RisingWater.png` | Tide gauge | Water up the steps | Rising column |
| `RogueWaves_Quickening.png` | Racing streaks | Whirlpool | Foam arrow |
| `RogueWaves_Nesting.png` | Two eggs in the sand | Speckled egg in twigs | Hatching |
| `RogueWaves_Crest.png` | Curl in a badge | Twin curls | Curl before the sun |
| `RogueWaves_PureAura.png` | Pearl in a clam | Pearl motes | Glowing orb |
| `RogueWaves_Depth.png` | Anchor | Darkening water | Diving bell |
| `RogueWaves_Retry.png` | Round arrow | Coral heart | Buoy |
| `RogueWaves_Vote.png` | Chart pin | Two pennants | Shell in the box |
| `Direction_Tougher.png` | Cross-stitched patch | Felt shield | Thimble armour |
| `Direction_CompostBin.png` | Leafy bin | Heap with a worm | Open lid |
| `Direction_DandelionSeason.png` | Seed clock | Seeds on the wind | Three flowers |
| `Direction_MotherOfMushrooms.png` | Mother and two | Cluster on a log | Spore shower |
| `Direction_EfficientRecycler.png` | Three arrows | Spool and arrow | Yarn loop |
| `Direction_SewingMachine.png` | Side view | Hand wheel | Stitching a seam |
| `Cardinal_VampireDentures.png` | Wide open | Blood drop | On a cushion |
| `RogueWaves_Crest32.png` | Curl in a badge | Curl before the sun | Twin crests |

Each version is a different idea for the same thing, in the pack palette (`docs/STYLE.md`), not a recolour. The three
weather icons are wired as string rows in `Code/08_Icons.cs`: `python3 concept-tools/temp_art/variants_icons.py` prints
each version's rows. `aseprite/RogueWaves_Icons.aseprite` holds every 12 x 12 icon of that version. Generator:
`concept-tools/temp_art/variants_icons.py`.
