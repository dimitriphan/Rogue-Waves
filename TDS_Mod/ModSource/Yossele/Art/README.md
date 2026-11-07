# Yossele: final art (ChatGPT, 2026-11-07)

Made in Dimitri's ChatGPT Project from the brief in `docs/art/` and brought in on 2026-11-07 ("bring in my ChatGPT
art", `docs/art/README.md`): 13 game-size PNGs, each the size and layout its row in `docs/art/ASSET_MANIFEST.md` gives.

These replace the final sheets Claude drew from his reshade on 2026-11-05 (and their `.aseprite` copies); that version is in git history before this change.

Held back after the art review: `YosseleFall-Sheet.png` here is a stand-in (the new idle_v0 frames twice) because
ChatGPT's fall showed a collapse and Falling loops in mid-air; the journal tab icon stays the code's (it must be a
white line drawing). Both go back to ChatGPT.

Files here, one folder above `Temporary/`, win over the temporary files and over the code's own sheets
(`YosseleBuilder.FileArt`; `build_log.txt` says "from Art/ (final)" for each). A file of the wrong size is skipped and logged.

- `aseprite/`: the same frames as `.aseprite` files (`TDS_Mod/Tools/aseprite/ase_sheets.py pack`), one tag per row.
  Edit, then `ase_sheets.py unpack` (or `export_sheets.lua` in Aseprite) and save over the `.png` here.
- `preview_8x/`: ChatGPT's 8x previews, for looking at; never built.

Checks run on the way in: `concept-tools/chatgpt/pixelize.py --check` and `concept-tools/temp_art/art_check.py`
(0 problems except a few colours beside the old palette), then the art review in `docs/STYLE.md`.
