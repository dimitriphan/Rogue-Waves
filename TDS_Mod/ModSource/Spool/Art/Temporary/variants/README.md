# Spool: three versions of each temporary asset (2026-10-28)

**Everything here is temporary and nothing here is wired.** Each `v1`, `v2`, `v3` folder holds a complete replacement for
the files in `../game_size/` (same names, sizes and frame layouts), for Dimitri to pick from one animation at a time;
`../game_size/` stays the one in game until he does. How picks are made and applied: `docs/TEMP_ART.md`, "Versions to
pick from". Each folder has `game_size/` (the drop-in PNGs), `aseprite/` (layer "TEMP art", a tag per animation and
view) and `preview_8x/` (enlarged sheets and GIFs), and `models/` for the needle.

| File | v1 | v2 | v3 |
| --- | --- | --- | --- |
| `Spoolhead-Sheet.png` (Thread Head) | Criss-cross, hop and jab-jab | Unravelling, waddle and bite | Knitting needle, march and slam |
| `Spoolstitched/dart/seam/mangled-Sheet.png` (Crafted) | Hop and lunge | Waddle and slam | Scamper and flurry |
| Dart attack | High lob | Sidearm fling | Stab then throw |
| `Spool_Tuft.png` | Coiled loop | Snarl with two ends | Cut snippets |
| `models/Needle.cs.txt` (Dart needle, render `preview_8x/models_Needle.png`) | Pearl-headed pin | Darning needle | Curved needle |

Built from Dimitri's Spool sprite as the current set is: the Thread Head wears his zipper mouth pixel for pixel in all
three (v1 adds criss-cross wraps and a curl, v2 a long strand dragging on the ground, v3 a knitting needle through the
ball); the Crafted keep their bodies (`spool_units.DOLLS`) and only move differently. A needle version is a whole
`Needle` method to paste over `SpoolModels.Needle` (PR #42). Generators: `concept-tools/temp_art/variants_spool.py`,
`variants_models.py`.
