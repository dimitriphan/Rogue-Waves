# Art Bench: edit any mod sprite or model and send it back

The Art Bench page (https://claude.ai/artifact/PgF7QP6n2HN56syTDUaUi5, private to Dimitri) lays out every mod sprite
sheet frame by frame and every model. He downloads a row (a `.zip` holding one `.aseprite` with the row's tags and the
part's palette) or a model (a `.zip` holding a `.glb` for Blender), edits it, and drops it back on the same row or model.
The page checks it, shows the changed frames beside the current ones, and on "Send to the mod" keeps it as an edit marked
`waiting` (the row as a PNG in the page's assets, the details in its `edits` collection). A thread then brings the
waiting edits into the repo through a PR. Base-vessel game sprites are never read.

| File | Does |
| --- | --- |
| `build.py` | Builds the page: runs each vessel's sprite generator (`cloud-check/lite/run_preview.sh s`), puts the files the builder would use over it (`Art/` final, else `Art/Temporary/game_size/`), merges hand edits from `ModSource/<Part>/Aseprite/` like `<Part>Aseprite.Apply`, cuts frames with `aseprite/ase_sheets.py`'s layouts, and adds the mode's icons and every model (`run_preview.sh m` mesh dumps, `mesh_glb.py`) |
| `ingest.py` | Brings waiting edits in (below) |
| `mesh_glb.py` | Model mesh JSON to `.glb` and back, colours snapped to the 64-swatch palette |
| `web/page.html` | The page; `build.py` inlines `web/ase.js` (`.aseprite` writer and reader, zip, PNG; tested against `ase_sheets.py` by `web/test_ase.mjs`) and `web/glb.js` |

## Build and publish

Once per container: `cd TDS_Mod/Tools/cloud-check && ./setup.sh ../../../TDS_Vessel_Creator`.

```bash
python3 TDS_Mod/Tools/artbench/build.py /tmp/artbench    # about a minute; prints sheets, frames, icons, models
```

Publish `/tmp/artbench/index.html` with the Artifact tool to the URL above (pass it as `url` from a new conversation),
with `files` mapping `models/<Part>.json` for each part, and capabilities `{db: {}, user: {}, assets: {}, downloads: true}`
on the first publish only (omit them afterwards to keep them).

## Bringing edits in ("bring in my art edits")

1. `ArtifactData` `list` collection `edits` on the page URL; save the documents as a JSON list (`edits.json`).
2. For each `waiting` edit, `Artifact` `read` with `path` = its `asset` id and `out_dir` = one folder; the file lands
   as `<asset id>.png` (a sprite row) or `.json` (a model).
3. `python3 TDS_Mod/Tools/artbench/ingest.py edits.json <that folder>` (add `--dry` to see first). Per edit:
   - **merge route** (most vessel sheets): the row's tags go into `ModSource/<Part>/Aseprite/<sheet>.aseprite`, `gen=`
     holding the CRCs of the sheet before hand edits, so the builder swaps in only the frames he changed; a facing the
     code draws as a mirror of an edited tag comes along unedited with `mirror=`, so it follows. Only the frames the
     page marked changed are taken; the row's other frames keep an earlier hand edit, else follow the code.
   - **file route** (the Cordyfriend, the Thread Head, `UloeLit-Sheet`, the tuft, Vendcint, the mode icons): the
     changed frames are pasted into the file the build reads; a code-drawn sheet becomes `Art/<sheet>.png` (final), so
     later code changes to it stop showing.
   - **models**: the `.glb` becomes `ModSource/<Part>/Art/Models/<model>.json`. The builders don't read these yet: the
     first model edit also adds that support to the builders and the cloud preview, and needs a PC round.
4. Rebuild and republish the page, run the cloud checks for each touched part (`CLAUDE.md` "Check your work"), open the
   PR (`docs/skills/ship-a-pr`), and set each applied edit's `status` to `pr` with `pr: <number>` (`ArtifactData`
   `update`); `merged` after the merge, `seen` after the PC round that shows it.

## Limits

- Argo's six Naut crew sheets aren't on the bench: the preview skips sheets saved in a loop (`sprite_calls.py`).
- The foes use the game's own art with overlays; effects and sounds are not on the bench.
- The merge route trusts that the cloud preview draws a sheet exactly as Unity does (checked for the first vessels, see
  `../aseprite/README.md`); a frame that differs would read as hand-edited.
- Downloads are `.zip` because the page may only offer zip, png, json and a few other types.
