# Yossele: final sprite sheets (2026-11-05)

Dimitri took the default of `docs/reports/OPEN_DECISIONS_2026-11-05b.md` item 11 ("Default for all", 16:04Z): Claude
draws Yossele's final sheets from his reshade and the temporary sheets, as `.aseprite` for him to edit. They are his to
change; scope `07`'s sprite `! NEEDS !` line stays until he takes them.

Files here, one folder above `Temporary/`, win over the temporary files and over the code's own sheets
(`YosseleBuilder.FileArt`; `build_log.txt` says "from Art/ (final)" for each).

| File | Size, layout | What |
| --- | --- | --- |
| `Yossele-Sheet.png` | 192 x 256, 32 px cells; rows idle v0-v3, then move v0-v3; 6 frames | his reshade breathing, and the knuckle walk he asked for on 2026-10-12 |
| `YosseleAttack-Sheet.png` | 128 x 128, 32 px, rows v0-v3, 4 frames | the mouth laser, his pick "Recoil blast": squat, charge, shot with a flash, recover |
| `YosseleFertilize-Sheet.png` | 192 x 128, 32 px, rows v0-v3, 6 frames | Fertilize: a root reaches down, the patch sprouts |
| `YosseleFall-Sheet.png` | 384 x 32, one row of 12 | falling in at a map's start, arms up, sparks round |
| `YosseleIntroR-Sheet.png` | 384 x 32, one row of 12 | landing: the drop, the dust, the eyes lighting |

Rows v0-v3 are front heading right, front heading left, back heading right, back heading left (kit `docs/18` §1).

**What changed from the temporary sheets.** Every frame is still his reshade's pixels moved as parts. The left-heading
facings (v1, v3) were exact mirrors of v0 and v2, so the reshade's shade band, on the right edge of each form, landed
on the left whenever Yossele walked left, and the light seemed to come from the right. Each mirrored run of clay and leaf
has its edge band moved back to the right end, so the shade sits on the right in all four facings (shade on the right edge: v0 100%, the mirrored rows
11% before and 94-100% after; kit `docs/18` §3 rules 2 and 7). Cast shadows (under the head, the collar) keep their order
inside the run, and every outline stays where it was. No colour was added: the sheets use only his reshade's colours and the laser's and soil's.

**Editing.** `aseprite/<sheet>.aseprite` holds the same frames, one per cell, a tag per row (`idle_v0` ... `move_v3`,
`attack_v0` ..., `fertilize_v0` ..., `fall`, `land`) on a layer named "FINAL art"; each tag's user data names its place
in the sheet. Export each tag back to its row (`TDS_Mod/Tools/aseprite/ase_sheets.py unpack`, or `export_sheets.lua`
inside Aseprite) and save over the `.png` here, then Tools > Yossele > Build All Assets. `preview_8x/` holds enlarged
sheets and GIFs for review; they are never built.

Generator: `concept-tools/temp_art/yossele_final.py` (its docstring; it reads `Temporary/game_size/` and the code's
sheets from the cloud preview). Re-running it overwrites this folder, so once he edits a file here, edit by hand.
These files freeze the code's Fertilize, fall and landing as they were on 2026-11-05: a later change to
`YosseleCharacter` reaches the game only after this folder's file is redrawn or deleted (the build log names the source).
The same mirroring is still in `YosseleCharacter` itself, so a sheet the code draws without a file here keeps it.
