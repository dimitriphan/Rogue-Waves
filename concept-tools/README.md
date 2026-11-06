# Concept tools

Generic Python for the pack's concept pages, art previews and sea chart. Each script's docstring says how to run it;
`lowpoly.py` and `pngout.py` are libraries the scripts import. Only
`sea_chart.py`'s PNG needs Pillow (`pip install pillow`, may be missing in a fresh container); the rest use the
standard library.

| File | What |
| --- | --- |
| `sprites.py` | Tiny pixel-sprite rasteriser: shapes → parts with z-order, auto shading, outlines, detail pixels. Note: the kit's house style (`TDS_Vessel_Creator/docs/03` §1) has since replaced the outline look this started with |
| `lowpoly.py` | Python port of the kit's low-poly mesh builder (`template/Mummer/Editor/MummerLowpoly.cs`) with a flat-shaded renderer at the kit's preview angle; a model written here ports line by line into `<Name>Models.cs` |
| `pngout.py` | Writes RGBA PNGs without Pillow |
| `sea_chart.py` | Seeded Slay the Spire style sea chart for the campaign (`docs/mode/SEA_CHART.md`): the reference the mod's C# port (`TDS_Mod/ModSource/RogueWavesMode/Code/03_Chart.cs`) matches line for line (`--check`), and a PNG preview |
| `sea_chart_view.py` | Mockups of the proposed chart view (`docs/mode/CHART_NAVIGATION.md`) drawn from `sea_chart.py`'s chart: `docs/concepts/art/chart_view_vote.png`, `chart_view_boat.png` (needs Pillow) |
| `temp_art/` | The pack's temporary art drawn as files (`docs/TEMP_ART.md`): `tart.py` (canvas, upper-left shading, PNG + `.aseprite` + 8x preview), `cordyfriend.py` (Yossele's Cordyfriend sheet), `ui_icons.py` (the mode's 12 x 12 icons, the 32 px crest, Spool's tuft), `yossele_anims.py` (Yossele's idle, walk and laser sheets), `spool_threadhead.py` and `spool_units.py` (the Thread Head and the four Crafted) |
| `argo_divers/` | Five concept takes on Argo as a diver who might not be human (`argo_divers.py`: art over the shipped frame plus the kit `docs/18` §4.2 check; `hollow_shapes.py`: round 2, Hollow in five head and body shapes; `hollow_ten.py`: round 3, ten Hollow bodies on round 1's helmet; `build_page.py` + `template.html`: the picks page); `docs/concepts/argo_divers_2026-11-04/`, `argo_hollow_2026-11-04/`, `argo_hollow_ten_2026-11-04/` |
| `minions/` | Ten Snoble-sized versions of every modded minion (2026-11-04, `docs/TEMP_ART.md` "Minion versions"): `mlib.py` (poses, gaits, small parts, sheet writer on `temp_art/tart.py`), one file per vessel with the ten designs (`yossele.py`, `spool.py`, `uloe.py`, `argo.py`), `build.py` (sheets into each part's `Art/Temporary/minions_2026-11-04/`, GIFs and line-ups into the project files), `page.py` + `page_template.html` (the picks page), `readmes.py`, `lineup.py` (a check sheet while drawing) |
| `build_mod_pages.py` | Builds an illustrated sheet for a vessel from the mod itself (`02_Data.cs` + `run_preview.sh` output), so the page can't drift from the code. Pages go to `docs/concepts/<slug>/`; its `VESSELS` table starts empty, add a row per vessel |

A new concept page follows the pattern in `.claude/rules/concepts.md`: an art script, a models script
on `lowpoly.py`, and a design script that checks the design against `docs/02` and writes `DESIGN.md`.
