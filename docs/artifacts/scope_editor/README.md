# Scope Editor

https://claude.ai/artifact/5KAGb5wdqiNV1bEoxNMgEo, private to Dimitri. Every scope file 00-16 as sheets (cards with
slot rows) or as raw text; By Dimitri / By Claude chips, Claude's `~defaults~` as tape marks, filters, search, line
edit, undo, format check, Import .txt, Export .txt byte for byte. Capabilities `db`, `downloads`, `user`.

| File | Does |
| --- | --- |
| `build.py` | `python3 build.py docs/scope <out.html>`: embeds every `NN_*.txt` and the commit they came from into `template.html` |
| `template.html` | The page; data lands at `/*__DATA__*/null` |

Moved here from `/mnt/project-files/concepts/scope_editor_2026-11-04/` on 2026-11-06, when the Send step was added.

## What the page stores

Collection `scopes`, one doc per edited file, id `f<NN>`: `text`, `baseHash`, `at`, `file`, `status`
(`draft` | `waiting` | `pr` | `merged`), `note` (his note for Claude), and on a sent row `sentAt`, `builtFrom` (the
commit the page was built from), and from a thread `pr`. An unedited file has no doc. Signed out, edits stay in that
browser only and cannot be sent.

## "Bring in my scope edits"

1. `ArtifactData` `list` collection `scopes` on the URL. Take only rows with `status: "waiting"`.
2. For each, fold `text` into `docs/scope/<file>` with the `scope-sync` skill ("Fold in one file of his"). If
   `builtFrom` is older than the base, diff his text against the scope at `builtFrom` first, so a line changed in the
   repository since is not taken for his. His `note` says what he meant.
3. Reviewed PR (`.claude/skills/ship-a-pr/SKILL.md`); set each row `status: "pr"`, `pr: <number>` (`ArtifactData` `update`
   with the row's `if_version`).
4. After the merge: rebuild (`python3 docs/artifacts/scope_editor/build.py docs/scope <scratchpad>/scope_editor.html`),
   `Artifact` read the URL, publish to the same `url`, then set the rows `merged`. Delete a `merged` row only once the
   republished page carries it (the page then shows the file as unedited).
