# Art with ChatGPT

Dimitri, 2026-11-06: ChatGPT makes the pack's sprites and art "so that Claude doesn't need to use tokens for that
process", from a document of the TDS art style and the base game's sprites. This folder is that document's public half
and the procedure around it. The base game's sprites are the private half: they never enter this repository
(`CLAUDE.md` "Rules").

| File | What |
| --- | --- |
| `CHATGPT_BRIEF.md` | Part 1 of the guide ChatGPT reads: its workflow, sizes by kind, the rules no picture may break |
| `PROJECT_INSTRUCTIONS.md` | the text pasted into the ChatGPT Project's instructions |
| `RECREATE_PROMPT.md` | the prompt that has ChatGPT sketch the vessels, ask Dimitri, then remake every asset, and the main menu button's look |
| `FEEDBACK_ROUND1_2026-11-07.md` | the note for ChatGPT after the review of its round 1 art (`docs/reports/ART_REVIEW_CHATGPT_2026-11-07.md`): rules for every asset, what to keep, batches 2A-2F |
| `FEEDBACK_SPOOL_KEY_2026-11-08.md` | the note for ChatGPT on its Spool key frame (batch 2B): size, sage ramp, flat fills, stitched slit, the arm on the floor, what to keep |
| `ASSET_MANIFEST.md` | every pack sheet, icon and model: file name, size, cell, rows, Look line; generated, never edited |
| `concept-tools/chatgpt/pixelize.py` | turns a generated picture into a clean game-size sprite and checks it; runs in ChatGPT's Python |
| `concept-tools/chatgpt/build_pack.py` | builds the Project's files and rewrites `ASSET_MANIFEST.md` |

## Setup (Dimitri, once)

1. ChatGPT, Settings, Data controls: turn off "Improve the model for everyone" (the game's sprites go into the Project).
2. Settings, Connectors (Apps), GitHub: connect, and allow `dimitriphan/Rogue-Waves`. ChatGPT can then read the scope.
3. Make a Project "Rogue Waves Art"; paste `PROJECT_INSTRUCTIONS.md`'s block into its instructions; upload every file
   of the pack (below) except `00_README.txt`.

## The pack

```bash
python3 concept-tools/chatgpt/build_pack.py <out_dir> --game-art /mnt/project-files/research/art_dump_2026-10-15 \
    --manifest docs/art/ASSET_MANIFEST.md
```

Needs cloud-check set up (the Art Bench builder supplies the pack's sheets). `<out_dir>` is outside the repository:
`/mnt/project-files/chatgpt/<date>/` (a new folder each build, never an overwrite), copied to the PC's
`Desktop\RogueWave\chatgpt\<date>\` for upload. It holds about 23 files, inside the Plus plan's 25 per Project: the
guide (this brief, `docs/STYLE.md`, kit `docs/18` §1-7, kit `docs/03` §1-2 and §5, `docs/concepts/YOSSELE_SPOOL_ULOE.md`
and the manifest in one file), the script, the palettes, the pack's and the game's sheets as two zips, and contact
sheets 768 px wide (ChatGPT shrinks a picture's short side to 768 px, so wider ones lose pixels). Sheets with no room
on a contact sheet are named in `00_README.txt` and stay in the zips. Rebuild and re-upload the guide when the style
rules, the scope's Look lines or the asset list change.

## Bringing art in ("bring in my ChatGPT art")

ChatGPT delivers each asset as a game-size PNG under its manifest file name. Dimitri drops it on the Art Bench page
(then the Art Bench's own procedure, `TDS_Mod/Tools/artbench/README.md`) or in `Desktop\RogueWave\sprites\chatgpt\`.
For the folder:

1. Stage the files from the approved folder `C:\Users\Dimitri\Desktop\RogueWave\sprites` (`device_stage_files`).
2. Check each against its manifest row: `python3 concept-tools/chatgpt/pixelize.py --check <png> --cell <cell>
   --palette <pack palette>` and `concept-tools/temp_art/art_check.py`, then the art review in `docs/STYLE.md`
   (contact sheets beside the current version and his own sprite). A file that fails goes back to him with the lines.
   Fall, Land and hop sheets lift frames off the bottom row on purpose: check those with `pixelize.py --check
   --anchor center` (it skips only the feet line), and read `art_check.py --baseline`'s lines on their rows against the
   lifts ChatGPT lists for each frame.
   A `<Name>splash.png` carries one translucent fill on purpose: the accent colour at alpha 70, as the builders' own
   splashes. Both checkers' half-transparent lines are expected on a splash, so long as every semi-transparent pixel
   has alpha 70. Any other line on it fails as usual.
3. Place it where the builder takes a file over its own drawing: `TDS_Mod/ModSource/<Part>/Art/<name>.png` (final;
   Yossele, Spool and Uloe read it through `FileArt`), the mode's files in
   `TDS_Mod/ModSource/RogueWavesMode/Art/Temporary/game_size/` (replace in place), Argo's only with a builder change.
   Hand-edited sheets on the merge route stay with the Art Bench.
4. Compile the part (`CLAUDE.md` "Check your work"), open the PR with the art review's lines, and list each piece for
   his eye in the next PC round.

## What ChatGPT doesn't make

Towers, buildings and objects are 3D models built in code: ChatGPT gives concept pictures, Claude builds the model.
Sounds and the effects drawn by code at run time stay with Claude. Image models paint off the grid, so expect
`pixelize.py` to do most of the cleaning and some pieces to need a hand fix in Aseprite; long four-facing animation
sheets are the weakest case.
