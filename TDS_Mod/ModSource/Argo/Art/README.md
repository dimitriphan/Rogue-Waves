# Argo: final art (ChatGPT)

Made in Dimitri's ChatGPT Project from the brief in `docs/art/`, and brought in with "bring in my ChatGPT art"
(`docs/art/README.md`). Each of the 19 game-size PNGs has the size and layout of its row in
`docs/art/ASSET_MANIFEST.md`.

- 2026-11-07: round 1 came in. It had a stand-in fall and no Sonar sheet.
- 2026-11-08: the complete set replaced it. It is drawn from the key he picked in ChatGPT (`argo_naut_key_v1`) and has
  his approved fall. `ArgoSonar-Sheet` and `Argotabicon` (a white line drawing) now replace the code's own.
  Review: `docs/reports/ART_REVIEW_ARGO_2026-11-08.md`.

Files here, one folder above `Temporary/`, win over the temporary files and over the code's own sheets
(`ArgoBuilder.FileArt`; `build_log.txt` says "from Art/ (final)" for each). A file of the wrong size is skipped and logged.

- `aseprite/`: the same frames as `.aseprite` files (`TDS_Mod/Tools/aseprite/ase_sheets.py pack`), one tag per row.
  Edit, then `ase_sheets.py unpack` (or `export_sheets.lua` in Aseprite) and save over the `.png` here.
- `preview_8x/`: 8x copies, for looking at; never built.

Checks run on the way in: `concept-tools/chatgpt/pixelize.py --check` and `concept-tools/temp_art/art_check.py`
(0 problems; the splash's alpha-70 fill is expected), then the art review in `docs/STYLE.md`.
