# Yossele: three versions of each temporary asset (2026-10-28)

**Everything here is temporary and nothing here is wired.** Each `v1`, `v2`, `v3` folder holds a complete replacement for
the files in `../game_size/` (same names, sizes and frame layouts), for Dimitri to pick from one animation at a time;
`../game_size/` stays the one in game until he does. How picks are made and applied: `docs/TEMP_ART.md`, "Versions to
pick from". Each folder has `game_size/` (the drop-in PNGs), `aseprite/` (layer "TEMP art", a tag per animation and
view) and `preview_8x/` (enlarged sheets and GIFs), and `models/` for the fruit.

| File | v1 | v2 | v3 |
| --- | --- | --- | --- |
| `Yossele-Sheet.png` (idle, walk) | Breathe and stride | Sway and waddle | Look about and scurry |
| `YosseleAttack-Sheet.png` | Charged beam | Recoil blast | Spit glob |
| `Yosselefriend-Sheet.png` (Cordyfriend) | Tall clubs, hop | Squat puffball, slam | Spore sack, sneeze |
| `models/Fruit.cs.txt` (aura fruit, render `preview_8x/models_Fruit.png`) | Pear on leaves | Berry bunch | Seed pod |

Yossele's body is never redrawn: every frame starts from the code's idle frame (his reshade, read from
`../game_size/Yossele-Sheet.png`), cut into body, sprout, shoulder leaf, eyes and four limbs, and only those move. The
Cordyfriend is three builds: tall clubs that pulse and a two-footed hop with a headbutt; a squat puffball that rolls
from foot to foot and slams with a ring of spores; a spore sack under an olive cap that scuttles and sneezes. A fruit
version is a whole `Fruit` method to paste over `YosseleModels.Fruit`. Generators: `concept-tools/temp_art/variants_yossele.py`,
`variants_models.py`.
