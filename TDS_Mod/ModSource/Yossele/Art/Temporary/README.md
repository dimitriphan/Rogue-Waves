# Yossele: temporary art drawn as files (2026-10-27)

**Everything in this folder is temporary.** It fills a slot until Dimitri's own art replaces it. The rest of Yossele's
temporary art is drawn by code in `Assets/YosseleMod/Editor` (list: `../../Previews/0.1/ASSETS.md` and `0.2/ASSETS.md`);
the pack's whole list is `docs/TEMP_ART.md`.

| File | Size, layout | Fills | Replaced | In game |
| --- | --- | --- | --- | --- |
| `game_size/Yosselefriend-Sheet.png` | 384 x 96, 24 px cells, PPU 20, bottom-centre pivot. Rows v0 v1 v2 v3 (front heading right, front heading left, back heading right, back heading left); columns 0-5 Idle, 6-11 Move, 12-15 Attack | the Cordyfriend (Cordyceps' minion, scope 07): a pale puffball like a Snoble, dot eyes, olive spore specks, three orange cordyceps clubs on its crown; idle breath and club sway, a waddle, a lunge with a spore puff | the Snoble's sprite tinted green it wore until now (still the fallback if the sheet is missing) | wired: `YosseleBuilder.CopySheet`, `YosseleAssets.Friend`, `YosseleContent.BuildFriend`; compiled, not yet built in Unity or seen in game |
| `preview_8x/models_Fruit.png` | render only | the aura fruit (`Yossele_Fruit*`, drawn by `YosseleModels.Fruit`): now 0.42 wide on a pale disc, two leaves | the 0.3 pod the play test couldn't pick out (`docs/reports/PLAYTEST_2026-10-26.md` §2) | code change, compiled |

`aseprite/Yosselefriend-Sheet.aseprite` is the same sheet as one frame per cell with a tag per row and animation
(`idle_v0`, `move_v0`, `attack_v0`, ...); its layer is named "TEMP art". `preview_8x/` holds enlarged copies on a
checker for review; they are never built.

## Replacing it

1. Draw the new sheet in the same layout (or edit the `.aseprite` and export each tag back to its row and column).
2. Save it as `ModSource/Yossele/Art/Yosselefriend-Sheet.png`, one folder up from here. A file there wins over the one
   in `Art/Temporary/game_size/`; `build_log.txt` says which one each build used.
3. Tools > Yossele > Build All Assets, then build the mod as usual. Delete the temporary file once yours is in.

Generator: `concept-tools/temp_art/cordyfriend.py` (re-run it to redraw; it overwrites this folder only).
