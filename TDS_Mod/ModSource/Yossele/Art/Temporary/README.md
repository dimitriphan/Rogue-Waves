# Yossele: temporary art drawn as files (2026-10-27)

**Everything in this folder is temporary.** It fills a slot until Dimitri's own art replaces it. The rest of Yossele's
temporary art is drawn by code in `Assets/YosseleMod/Editor` (list: `../../Previews/0.1/ASSETS.md` and `0.2/ASSETS.md`);
the pack's whole list is `docs/TEMP_ART.md`.

| File | Size, layout | Fills | Replaced | In game |
| --- | --- | --- | --- | --- |
| `game_size/Yosselefriend-Sheet.png` | 384 x 96, 24 px cells, PPU 20, bottom-centre pivot. Rows v0 v1 v2 v3 (front heading right, front heading left, back heading right, back heading left); columns 0-5 Idle, 6-11 Move, 12-15 Attack | the Cordyfriend (Cordyceps' minion, scope 07): a pale puffball like a Snoble, dot eyes, olive spore specks, three orange cordyceps clubs on its crown; idle breath and club sway, a waddle, a lunge with a spore puff | the Snoble's sprite tinted green it wore until now (still the fallback if the sheet is missing) | wired: `YosseleBuilder.CopySheet`, `YosseleAssets.Friend`, `YosseleContent.BuildFriend`; compiled, not yet built in Unity or seen in game |
| `game_size/Yossele-Sheet.png` | 192 x 256, 32 px cells: rows 0-3 Idle v0-v3, rows 4-7 Move v0-v3, 6 frames each | the vessel's idle and walk. Idle is the code's (his reshade breathing); Move is the knuckle walk he asked for on 2026-10-12 (`concepts/walk_2026-10-12`): the body drops on each plant and the arms reach | the code's walk, a bob and leg shuffle | wired: `YosseleBuilder.FileArt`; compiled, not yet built in Unity or seen in game |
| `game_size/YosseleAttack-Sheet.png` | 128 x 128, 32 px cells, rows v0-v3, 4 frames | the mouth laser: a squat as the hole glows, a lean back as it charges lime with sparks, the shot with a white core and a flash, a recover with a fading glow and a smoke wisp | the code's attack, a lean with the hole lit | wired, same; not yet seen in game |
| `preview_8x/models_Fruit.png` | render only | the aura fruit (`Yossele_Fruit*`, drawn by `YosseleModels.Fruit`): now 0.42 wide on a pale disc, two leaves | the 0.3 pod the play test couldn't pick out (`docs/reports/PLAYTEST_2026-10-26.md` §2) | code change, compiled |

Each `aseprite/<sheet>.aseprite` is the same sheet as one frame per cell with a tag per row and animation
(`idle_v0`, `move_v0`, `attack_v0`, ...); its layer is named "TEMP art". `preview_8x/` holds enlarged copies on a
checker and animated GIFs (`Yossele_idle.gif`, `Yossele_move.gif`, `Yossele_attack.gif`, the four views side by side)
for review; they are never built.

**How the vessel sheets load.** `YosseleBuilder.FileArt` (2026-10-27) lets a file replace any sheet the code draws:
`Art/<name>.png` (final) wins over `Art/Temporary/game_size/<name>.png`, which wins over the code; a file of the wrong
size is skipped. Hand-edited frames in `ModSource/Yossele/Aseprite/` still merge on top. `build_log.txt` names the source
of each sheet ("sprites: Yossele-Sheet.png from Art/Temporary/ (temporary)").

## Replacing it

1. Draw the new sheet in the same layout (or edit the `.aseprite` and export each tag back to its row and column).
2. Save it as `ModSource/Yossele/Art/<same name>.png` (for example `Art/Yosselefriend-Sheet.png`), one folder up. A file there wins over the one
   in `Art/Temporary/game_size/`; `build_log.txt` says which one each build used.
3. Tools > Yossele > Build All Assets, then build the mod as usual. Delete the temporary file once yours is in.

Generators: `concept-tools/temp_art/cordyfriend.py` and `yossele_anims.py` (re-run to redraw; they overwrite this
folder only; `yossele_anims.py` takes the code's sheets from the lite renderer and the walk concept, see its docstring).
To go back to the code's idle, walk or attack, delete the matching file in `game_size/`.

**Versions to pick from (2026-10-28):** three new versions of every file above are in `variants/v1|v2|v3/` (its README names them); nothing there is wired until Dimitri picks (`docs/TEMP_ART.md`).
