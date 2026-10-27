# Temporary art: every placeholder in the pack and how to replace it

Dimitri, 2026-10-27: "Create temporary assets for spool, yossele, and the rogue waves UI. Make sure to categorize them as
temporary so that they can be replaced later, but make them to the quality of the base game."

**Rule.** Temporary art lives in a folder named `Temporary` (`ModSource/<Part>/Art/Temporary/`, with a README listing each file;
not `Temp`, which git and Unity treat as a scratch folder)
or, when code draws it, is listed in that part's `Previews/<version>/ASSETS.md`. Each `.aseprite` copy names its layer
"TEMP art". Final art goes beside it, one folder up (`ModSource/<Part>/Art/<same name>.png`), and wins over the
temporary file wherever a builder loads files (Yossele's `CopySheet` and `FileArt`: any sheet the code draws can be
replaced by a file of the same name and size). Nothing temporary is final until Dimitri says so.
The quality bar is the game's own sprites (kit `docs/03` §1, `docs/18` §3-4): opaque, no outline, lit from the upper
left, shade keeps the hue, no pure black, magenta only on foes.

## The list

| Part | Temporary art | Where | State |
| --- | --- | --- | --- |
| Yossele | the vessel's sheets, menu art, towers, upgrades, effects, sounds (drawn by code from his reshade) | `ModSource/Yossele/Previews/0.1/ASSETS.md`, `0.2/ASSETS.md` | in game since PC round 9 |
| Yossele | the Cordyfriend's own sheet (was the Snoble's sprite tinted green) | `ModSource/Yossele/Art/Temporary/` | wired, compiled; next PC round |
| Yossele | the vessel's idle and walk (the knuckle walk) and its mouth-laser attack, as files over the code's sheets | `ModSource/Yossele/Art/Temporary/` (`Yossele-Sheet`, `YosseleAttack-Sheet`) | wired (`YosseleBuilder.FileArt`), compiled; next PC round |
| Yossele | a bigger aura fruit on a pale disc | `Assets/YosseleMod/Editor/YosseleModels.cs` `Fruit`; render in `Art/Temporary/preview_8x/` | compiled; next PC round |
| Spool | the vessel's sheets, Crafted, Thread Head, towers, effects, sounds (drawn by code) | Spool's `Previews/0.1/ASSETS.md` (PR #42) | in game since Spool round 1 |
| Spool | a 12 x 12 ground Thread tuft; a needle that reads in flight | `ModSource/Spool/Art/Temporary/` | wired on PR #42 (`e0d2bf6`), compiled; not yet seen in game |
| Spool | the Thread Head and the four Crafted (Stitched, Dart, Seam, Mangled), redrawn and animated | `ModSource/Spool/Art/Temporary/` | files only; PR #42 wires them (a `FileArt` like Yossele's) |
| Mode UI | three weather icons, redrawn | `ModSource/RogueWavesMode/Art/Temporary/`, rows in `Code/08_Icons.cs` | wired, compiled |
| Mode UI | the crest (12 and 32 px), Pure Aura, Depth, retry, vote icons | same folder | files only, waiting for their features |
| Directions, Cardinals | icons for the six Directions and the one Cardinal the scope names (`docs/scope/11`, `12`) | same folder | files only |
| Argo | built art (Argo 2.0) | `ModSource/Argo/DESIGN.md` | not temporary: shipped look |

## Previews and generators

Every new file was drawn by `concept-tools/temp_art/` (`tart.py` the toolkit, `cordyfriend.py`, `ui_icons.py`, `yossele_anims.py`, `spool_threadhead.py`,
`spool_units.py`); re-run
a generator to redraw. A contact sheet of the 2026-10-27 set is in the project files at
`/mnt/project-files/concepts/temp_art_2026-10-27/`.
