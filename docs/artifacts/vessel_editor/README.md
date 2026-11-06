# Vessel Editor and Vessel Spotlight

- **Vessel Editor** https://claude.ai/artifact/JiVvnHii85Fg9c1DBMC6wj: journal pages for the five base vessels, Spool
  and Yossele, with the Rogue Waves sheets editable line by line beside the page they make; Export/Import .txt byte
  for byte, Check, and Send. Capabilities `db`, `downloads`, `user`.
- **Vessel Spotlight** https://claude.ai/artifact/NZ3wMMaujKDSNSaYCw7NZw: the same pages, read only (version 3,
  2026-10-23; not rebuilt while its merge with the Spotlight is his open question).

Dimitri's rule: Spool and Yossele show only his own work, his reshade frames and his sheets 07, 08, 11. Every Claude
default (`~value~`, `/~ note \`) shows as WIP; none of Claude's concept or build art is used.

| File | Does |
| --- | --- |
| `build.py` | `python3 build.py <spotlight.html> <editor.html>`: reads `docs/scope` (07, 08, 11, 15, 16), the game's sheets from `/mnt/project-files/research/art_dump_2026-10-15/sheets/` and his frames from `/mnt/project-files/concepts/vessel_spotlight_2026-10-15/assets/`. Write both pages outside the repository: they hold game sprites. Needs Pillow |
| `parse.py` | Reads the base vessels from `16_Base_Vessels.txt` |
| `sheet.js` | Scope-sheet parser and writer (lines kept as text, so export = import byte for byte; lint) |
| `page.js`, `body.js` | Render the pages |
| `editor.js` | The editing layer, storage and Send (Editor only) |
| `template.html` | The page shell; data lands at `/*__DATA__*/null` |

Moved here from `/mnt/project-files/concepts/vessel_spotlight_2026-10-15/build/` on 2026-11-06, when the Send step was
added and the build switched from the project-files scope copy to `docs/scope`.

## What the page stores

Collection `sheets`, one doc per vessel id (`spool`, `yossele`, or a vessel he added by Import): `id`, `name`,
`file`, `text`, `frameUrl` (a sprite he set, as a PNG data URL), `accent`, `accent2`, `added`, `updatedAt`,
`history` (last five texts), `status` (`draft` | `waiting` | `pr` | `merged`), `note`, and on a sent row `sentAt`
and `builtFrom`, and from a thread `pr`.

## "Bring in my vessel edits"

1. `ArtifactData` `list` collection `sheets` on the Editor's URL. Take only rows with `status: "waiting"`.
2. Fold each `text` into `docs/scope/<file>` with the `scope-sync` skill; a new vessel (`added: true`) is a new scope
   file in the Vessel template, numbered next free. A `frameUrl` is art: take it through the Art Bench route
   (`TDS_Mod/Tools/artbench/README.md`), not into the scope. His `note` says what he meant.
3. Reviewed PR; set each row `status: "pr"`, `pr: <number>`. After the merge, rebuild, `Artifact` read the URL,
   publish to the same `url`, set the rows `merged`, and delete a row only once the page carries it.
4. An `added` vessel is not on the rebuilt page until `build.py`'s `RW` list names its id and scope file, with his frame saved
   as `dimitri_<id>.png` in the assets folder (`build.py` needs one). Add both in the same PR. Until it is, keep its row (as
   `merged`): deleting it would drop the vessel from the editor.
