# Concept tools

The generic Python behind the concept pages in `ModProjects/tds-vessel-concepts`, without the per-vessel scripts. Each
script's docstring says how to run it; `lowpoly.py` and `pngout.py` are libraries the scripts import. Only
`sea_chart.py`'s PNG needs Pillow (`pip install pillow`, may be missing in a fresh container); the rest use the
standard library.

| File | What |
| --- | --- |
| `sprites.py` | Tiny pixel-sprite rasteriser: shapes → parts with z-order, auto shading, outlines, detail pixels. Note: the kit's house style (`TDS_Vessel_Creator/docs/03` §1) has since replaced the outline look this started with |
| `lowpoly.py` | Python port of the kit's low-poly mesh builder (`template/Mummer/Editor/MummerLowpoly.cs`) with a flat-shaded renderer at the kit's preview angle; a model written here ports line by line into `<Name>Models.cs` |
| `pngout.py` | Writes RGBA PNGs without Pillow |
| `sea_chart.py` | Seeded Slay the Spire style sea chart for the campaign (`docs/mode/SEA_CHART.md`): the reference the mod's C# port (`TDS_Mod/ModSource/RogueWavesMode/Code/03_Chart.cs`) matches line for line (`--check`), and a PNG preview |
| `temp_art/` | The pack's temporary art drawn as files (`docs/TEMP_ART.md`): `tart.py` (canvas, upper-left shading, PNG + `.aseprite` + 8x preview), `cordyfriend.py` (Yossele's Cordyfriend sheet), `ui_icons.py` (the mode's 12 x 12 icons, the 32 px crest, Spool's tuft), `yossele_anims.py` (Yossele's idle, walk and laser sheets), `spool_threadhead.py` and `spool_units.py` (the Thread Head and the four Crafted) |
| `build_mod_pages.py` | Builds an illustrated sheet for a vessel from the mod itself (`02_Data.cs` + `run_preview.sh` output), so the page can't drift from the code. Pages go to `docs/concepts/<slug>/`; add a row to its `VESSELS` table per vessel |

A new concept page follows the Broodie pattern described in `.claude/rules/concepts.md`: an art script, a models script
on `lowpoly.py`, and a design script that checks the design against `docs/02` and writes `DESIGN.md`.
