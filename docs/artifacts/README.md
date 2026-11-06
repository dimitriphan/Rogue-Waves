# The project's artifacts

Every claude.ai artifact made for Rogue Waves, which ones are live, where their source lives, and how what Dimitri
types or drops on a page comes back into the repository. All are private to him (each page's Share menu).

## Live: pages that take his input

Each one keeps his input in the page's own database. Nothing reaches the repository until he presses **Send** on the
page and then says the phrase below in the project; a thread then reads every `waiting` row, folds it in through a
reviewed PR, and writes the row's `status` back to `pr` (with `pr: <number>`) and then `merged`, which the page shows.

| Page | Link | Pinned | Source | He says | Rows |
| --- | --- | --- | --- | --- | --- |
| Art Bench | https://claude.ai/artifact/PgF7QP6n2HN56syTDUaUi5 | yes | `TDS_Mod/Tools/artbench/` | "bring in my art edits" | `edits` (+ assets) |
| Scope Editor | https://claude.ai/artifact/5KAGb5wdqiNV1bEoxNMgEo | yes | `scope_editor/` | "bring in my scope edits" | `scopes`, one per file |
| Vessel Editor | https://claude.ai/artifact/JiVvnHii85Fg9c1DBMC6wj | no | `vessel_editor/` | "bring in my vessel edits" | `sheets`, one per vessel |

Status values on a row: `draft` (saved on the page, not sent), `waiting` (sent), `pr`, `merged`. Any edit after a send
turns the row back to `draft`, so a thread only ever takes what he sent. A thread clears a `merged` row only after it
has rebuilt and republished the page, so the page's own copy already carries the change.

## Live: pages to read

| Page | Link | Pinned | Source |
| --- | --- | --- | --- |
| Spotlight (every new feature, with PC/built chips) | https://claude.ai/artifact/MCPDgVngHmnZcVHeZwxNSr | yes | `docs/spotlight/` |
| Vessel Spotlight (journal pages, read-only twin of the Vessel Editor) | https://claude.ai/artifact/NZ3wMMaujKDSNSaYCw7NZw | no | `vessel_editor/` (`build.py` writes both) |

The Vessel Spotlight still shows the scope of 2026-10-23; whether it merges into the Spotlight is his open question in
the spotlight thread, so it is neither rebuilt nor archived here.

## Archived 2026-11-06

Older pages whose decisions are made, deleted from his gallery at his pick (2026-11-06), so the links below no longer
open. Each page's HTML is saved in the project files, `/mnt/project-files/artifacts_archive/2026-11-06/<slug>/page.html`,
with its picks rows under `db/picks/` for the three picks pages (35, 1 and 8 rows), all listed in that folder's
`MANIFEST.md`. The first Scope Editor could no longer be read by then, so its copy is its build file from
`/mnt/project-files/concepts/scope_editor_2026-11-04/scope_editor.html`, and none of its rows could be saved. They stay out of the repository because several embed base-vessel game sprites or his uploads.

| Page | Link | Why archived |
| --- | --- | --- |
| Rogue Waves Scope Editor (first copy) | https://claude.ai/artifact/GQR8Lnvie9DQCDiFTCwkwp | superseded by the Scope Editor above |
| Rogue Waves Temporary Art | https://claude.ai/artifact/S1ropAhQwBHhpLDDxWi7Xo | catalog of 2026-10-28; the Art Bench shows the art as built now |
| Temp Art Picks | https://claude.ai/artifact/HsfbYEykGD92BAFWhJPnZs | his 35 picks applied 2026-11-03 (`docs/TEMP_ART.md`) |
| Argo Diver Takes | https://claude.ai/artifact/FnchhGQBHvi9JyhC47TCe2 | he kept Argo's original look (PLAN Log 2026-11-04) |
| Rogue Waves Minion Picks | https://claude.ai/artifact/Rj9xQXsYUnauTGQyiFfAJX | all eight minions picked and wired 2026-11-05 |
| Yossele, Spool, Uloe Concepts | https://claude.ai/artifact/FvbsCn1CZ2deoJUFUr4faM | first concept round of 2026-10-09, long since replaced |

## Rules for every page

- Publish a new version to the same link (pass `url`); never a second copy. Read the live page first.
- Keep capabilities as the table's pages declare them; a republish without `capabilities` keeps them.
- A page that takes input gets a Send step and the status rows above, so a thread never guesses which saves are final.
- Built pages that hold game sprites (Vessel Editor, Vessel Spotlight) are written outside the repository.
