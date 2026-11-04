---
paths:
  - "TDS_Mod/Assets/**"
  - "TDS_Vessel_Creator/template/Mummer/Editor/**"
  - "concept-tools/**"
  - "docs/concepts/art/**"
  - "docs/concepts/**/art/**"
  - "TDS_Mod/ModSource/*/Aseprite/**"
  - "TDS_Mod/Tools/aseprite/**"
---
# Drawing for TDS

Formats, house style, reference vessels, UI art, units, towers, effects and sound: `TDS_Vessel_Creator/docs/03_ART_AND_AUDIO.md`
(§1 sprites, §2 UI, §3 units, §4 towers, §6 sound). Name the base vessel the sprite follows. What the game's own
vessel and enemy sprites measure (facings, sizes, palettes, the crystal foes, vessel vs foe, must/must-not):
`TDS_Vessel_Creator/docs/18_CHARACTER_ART.md`.

Corrections to older work in this repo (the kit is right, the old pages were wrong):

- Sprites are fully opaque, edges in darker tones of the fill. The half-alpha rim is Smacky's alone and the ground patch
  is Sno Sno's snow: use them only when that vessel is the reference (the user, 2026-10-09).
- Towers are **always low-poly sleek, never voxel**. Pages that say "towers are voxel in the game" are wrong.
- Unit sheets are 10×4 of 16 px (move 0-5, attack 6-9), not 6×4. UI art is the game's set (12×12 ready-up, 128×128
  splash, 32×32 end icons and journal tab), not a 16×16 icon + 96×96 portrait.
User feedback to keep applying: a dopey face reads well (eyes wide apart, one a pixel lower); Argo's hands touch the
body, bulky "like Nautilus"; a figure peeking out of a costume sits in front of it; one sheen line instead of noise
texture; borrow how another game plays, never its look or names; sounds quiet (the first generated ones were 20× too
loud).

Preview without Unity: `TDS_Mod/Tools/cloud-check/lite/run_preview.sh <EditorDir> <Name> <OutDir> sma` (setup in
`cloud-check/README.md`). Hand-edited Aseprite frames override the code's art: `TDS_Mod/Tools/aseprite/README.md`.
