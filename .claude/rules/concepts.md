---
paths:
  - "docs/concepts/**"
  - "concept-tools/**"
---
# Concept pages

A concept is the design the kit redraws at the game's formats: `index.html` + sprite sheets + `manifest.json` (or, for a
first draft, a Markdown page with its art in `docs/concepts/art/`), filling the sections of
`TDS_Vessel_Creator/template/DESIGN_TEMPLATE.md`. Use the game's real formats (`docs/03`) so the redraw is a straight
port, and design only what the engine can do or say what needs a substitute. Put new pages in `docs/concepts/<vessel>/`.

- A concept page has three scripts: an art script (sprites + manifest), a models script on `concept-tools/lowpoly.py`
  (a port of the kit's `Lp`), and a design script that checks the design against `docs/02` and writes `DESIGN.md`.
- Once a vessel is implemented, its page is **generated from the mod** (`concept-tools/build_mod_pages.py` after
  `TDS_Mod/Tools/cloud-check/lite/run_preview.sh`). Edit the mod, not the page.
- Sprite scripts need Pillow (`pip install pillow` if a fresh container lacks it).
