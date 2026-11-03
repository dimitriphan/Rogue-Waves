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
| Spool | the Thread Head and the four Crafted (Stitched, Dart, Seam, Mangled), redrawn from Dimitri's Spool sprite (2026-10-28) and animated | `ModSource/Spool/Art/Temporary/` | wired on PR #42 (`ad85b00`, a `FileArt` like Yossele's), compiled; not yet seen in game |
| Mode UI | three weather icons, redrawn | `ModSource/RogueWavesMode/Art/Temporary/`, rows in `Code/08_Icons.cs` | wired, compiled |
| Mode UI | the crest (12 and 32 px), Pure Aura, Depth, retry, vote icons | same folder | files only, waiting for their features |
| Directions, Cardinals | icons for the six Directions and the one Cardinal the scope names (`docs/scope/11`, `12`) | same folder | files only |
| Argo | built art (Argo 2.0) | `ModSource/Argo/DESIGN.md` | not temporary: shipped look |

## Versions to pick from (2026-10-28)

Dimitri, 2026-10-28: "For all temporary assets, create alternative versions for me to choose for each asset. There should
be different versions for every single animation included for Yossele and Spool. Create 3 versions of each asset for
now." Every asset above has three new versions in `ModSource/<Part>/Art/Temporary/variants/v1|v2|v3/` (`game_size/`,
`aseprite/`, `preview_8x/`, and `models/` for the two 3D models), each with the same name, size and frame layout as the
file in `game_size/`, which stays the one in game until he picks. "Current" is always a fourth choice. The folder
READMEs name each version.

- **Picking.** One pick per animation (35 in all), so a sheet can take its walk from one version and its attack from
  another: page https://claude.ai/artifact/HsfbYEykGD92BAFWhJPnZs (private to Dimitri; picks are saved on the page,
  collection `picks`, one document per item, `{choice: "cur" | "v1" | "v2" | "v3"}`; a Claude thread reads them with
  the ArtifactData tool, or he pastes the text the page gives).
- **Applying.** `python3 concept-tools/temp_art/apply_picks.py picks.json` copies the picked frames over
  `game_size/` (the wired files), copies picked icons whole and prints which snippet to paste for a model pick. The three
  weather icons are also rows in `RogueWavesMode/Code/08_Icons.cs`: `variants_icons.py` prints each version's rows. Then
  re-export the `.aseprite` copies (`tart.write_sheet`) and delete the versions not picked. Spool's sheets are wired on
  PR #42's branch, which has its own copy of `Spool/Art/Temporary/game_size/`: copy the applied files there too.
- **Rules kept.** Yossele's body in every version is the code's frame drawn from his reshade, cut into parts that move;
  the Thread Head wears his zipper mouth pixel for pixel; the Crafted keep their bodies from his sprite (only how they
  move changes); no soft rim; opaque, lit from the upper left. None of it has been seen in game.
- **Generators.** `concept-tools/temp_art/variants_yossele.py`, `variants_spool.py`, `variants_icons.py`,
  `variants_models.py` (renders the C# snippets in `model_variants/` with the cloud preview), `variants_page.py` (the page).

## Dimitri's picks (2026-11-03), applied

Applied with `apply_picks.py` over `game_size/` (and the `.aseprite` copies and previews rebuilt from the result); the
Rising Water rows in `RogueWavesMode/Code/08_Icons.cs`, the tuft rows in `Spool/Code/11_Client.cs`, `YosseleModels.Fruit`
and `SpoolModels.Needle` changed to match. All of it compiled (cloud check); none of it seen in game. The `variants/`
folders stay until he has seen the picks in game, in case he wants to swap one. **Re-running the 2026-10-27 generators
(`cordyfriend.py`, `yossele_anims.py`, `spool_threadhead.py`, `spool_units.py`, `ui_icons.py`) would undo these picks;**
re-run `apply_picks.py` with the list below after any of them.

| Part | Item | Pick |
| --- | --- | --- |
| Yossele | idle, walk | current |
| Yossele | mouth laser attack | v2 Recoil blast |
| Yossele | Cordyfriend idle, walk, attack | v1 Tall clubs, hop (all three) |
| Yossele | aura fruit model | v2 Berry bunch |
| Spool | Thread Head walk, attack | v1 Criss-cross, hop; Jab-jab |
| Spool | Stitched walk / attack | v3 Scamper / current |
| Spool | Dart walk / attack | v2 Waddle / current |
| Spool | Seam walk, attack | v2 Waddle, Slam |
| Spool | Mangled walk, attack | v2 Waddle, Slam |
| Spool | ground Thread tuft | v1 Coiled loop |
| Spool | Dart needle model | v1 Pearl-headed pin |
| UI | Rising Water | v2 Water up the steps |
| UI | Quickening Current, Nesting Ground | current |
| UI | crest (12 px and 32 px) | v1 Curl in a badge (both) |
| UI | Pure Aura | v3 Glowing orb |
| UI | Depth | v1 Anchor |
| UI | Retry, Vote | current |
| UI | Made of Tougher Stuff | v1 Cross-stitched patch |
| UI | Compost Bin, Sewing Machine | current |
| UI | Dandelion Season | v2 Seeds on the wind |
| UI | Mother of Mushrooms | v3 Spore shower |
| UI | Efficient Recycler | v2 Spool and arrow |
| UI | Vampire Dentures | v1 Wide open |

As `apply_picks.py` input: `concept-tools/temp_art/picks_2026-11-03.json`.

## Previews and generators

A page of every asset, animated: https://claude.ai/artifact/S1ropAhQwBHhpLDDxWi7Xo (private to Dimitri; rebuild from
`/mnt/project-files/concepts/temp_art_2026-10-27/temp_art_catalog.html`).

Every new file was drawn by `concept-tools/temp_art/` (`tart.py` the toolkit, `cordyfriend.py`, `ui_icons.py`, `yossele_anims.py`, `spool_threadhead.py`,
`spool_units.py`); re-run
a generator to redraw. A contact sheet of the 2026-10-27 set is in the project files at
`/mnt/project-files/concepts/temp_art_2026-10-27/`.
