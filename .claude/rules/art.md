---
paths:
  - "TDS_Mod/Assets/**"
  - "TDS_Vessel_Creator/template/Mummer/Editor/**"
  - "concept-tools/**"
  - "docs/concepts/**/art/**"
  - "TDS_Mod/ModSource/*/Aseprite/**"
  - "TDS_Mod/Tools/aseprite/**"
---
# Drawing for TDS

Formats, house style, reference vessels, UI art, units, towers, effects and sound: `TDS_Vessel_Creator/docs/03_ART_AND_AUDIO.md`
(§1 sprites, §2 UI, §3 units, §4 towers, §6 sound). Name the base vessel the sprite follows.

Corrections to older work in this repo (the kit is right, the old pages were wrong):

- Sprites need the half-alpha rim on the lit upper-left edge and a soft ground patch; crisp edges with no patch were wrong.
- Towers are **always low-poly sleek, never voxel**. Pages that say "towers are voxel in the game" are wrong.
- Unit sheets are 10×4 of 16 px (move 0-5, attack 6-9), not 6×4. UI art is the game's set (12×12 ready-up, 128×128
  splash, 32×32 end icons and journal tab), not a 16×16 icon + 96×96 portrait.
- The old `argo/` and `mummer/` concept assets in `ModProjects` still have these gaps.

User feedback to keep applying: Sprigg's dopey face (eyes wide apart, one a pixel lower); Argo's hands touch the body,
bulky "like Nautilus"; Mummer's peeking arm in front of the costume; one sheen line instead of noise texture; borrow how
another game plays, never its look or names (Broodie 1.0 was too Zerg); sounds quiet (Sprigg's first were 20× too loud).

Preview without Unity: `TDS_Mod/Tools/cloud-check/lite/run_preview.sh <EditorDir> <Name> <OutDir> sma` (setup in
`cloud-check/README.md`). Hand-edited Aseprite frames override the code's art: `TDS_Mod/Tools/aseprite/README.md`.
