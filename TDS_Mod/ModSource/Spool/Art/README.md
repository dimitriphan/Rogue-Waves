# Spool: final art (ChatGPT, 2026-11-07)

Made in Dimitri's ChatGPT Project from the brief in `docs/art/` and brought in on 2026-11-07 ("bring in my ChatGPT
art", `docs/art/README.md`): 21 game-size PNGs, each the size and layout its row in `docs/art/ASSET_MANIFEST.md` gives.


Files here, one folder above `Temporary/`, win over the temporary files and over the code's own sheets
(`SpoolBuilder.FileArt`; `build_log.txt` says "from Art/ (final)" for each). A file of the wrong size is skipped and logged.

- `aseprite/`: the same frames as `.aseprite` files (`TDS_Mod/Tools/aseprite/ase_sheets.py pack`), one tag per row.
  Edit, then `ase_sheets.py unpack` (or `export_sheets.lua` in Aseprite) and save over the `.png` here.
- `preview_8x/`: ChatGPT's 8x previews, for looking at; never built.

Checks run on the way in: `concept-tools/chatgpt/pixelize.py --check` and `concept-tools/temp_art/art_check.py`
(0 problems except a few colours beside the old palette), then the art review in `docs/STYLE.md`.
