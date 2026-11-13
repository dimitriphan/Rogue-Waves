---
name: scope-sync
description: Use in Rogue Waves when Dimitri says "sync" for his scope copies, or sends back an edited scope file to fold in.
effort: medium
---

# Sync Dimitri's scope copies

Rules from `docs/PLAN.md` Part A ("The user's scope") and `docs/scope/00_Read_Me.txt`. Effort medium. The repository's
`docs/scope/` is the source; his copies are `/mnt/project-files/scope/`, the Scope Editor artifact
(https://claude.ai/artifact/5KAGb5wdqiNV1bEoxNMgEo) and `Desktop\RogueWave\scope` on his PC.

## Gate

- Overwrite `/mnt/project-files/scope/` or the Scope Editor only at his word in the thread ("sync"). His PC copy only
  in a PC round or with his OK (`pc-round` step 8).

## First, look for his edits

1. Scope Editor and Vessel Editor: `ArtifactData` list, collections `scopes` and `sheets`. A row with `status: "waiting"`
   is an edit he sent; a `draft` row is still his work in progress, so leave it. Steps and statuses:
   `docs/artifacts/README.md`.
2. Project files: `diff -r docs/scope /mnt/project-files/scope` (ignore `pc_backup_*` and the old zip). A line he
   changed that the repository never had is his edit, not staleness.
3. Fold every edit of his in before copying anything over it (below).

## Fold in one file of his

1. An upload has no name (`/mnt/project-files/uploads/hearth/<id>`): its first line (`| Directions |`,
   `| New Vessel: Yossele |`) says which file it is. Diff it against the repository copy.
2. Keep his wording. Put each `< answer >` into the line above it in his format and delete both the answer and the
   `! NEEDS !` it answers. A `~value~` he left alone keeps its tildes; one he changed or untilded is his.
3. Touch another scope file only where his edit contradicts it, and name that file in the reply.
4. Log the change in `docs/scope/02_Progress.txt`.

## Tildes and NEEDS lines

- Never strip a tilde he did not strip. Defaults he has not taken stay `~value~`, including any a decision says
  stay until a later round (the decision names them; e.g. the sweep's numbers in `04`, `08`, `10`, `12` until he takes
  them, PLAN Part C).
- Clear a `! NEEDS !` line only when it is answered or the thing is supplied, and say which.

## Sync

1. Docs PR with `ship-a-pr`; its Part C entry says his copies now equal the new base sha. Merge it.
2. Copy the 17 `docs/scope/NN_*.txt` files over `/mnt/project-files/scope/` (leave its other files). It is a network
   mount: wait about ten seconds and `diff -r` again.
3. Scope Editor: `python3 docs/artifacts/scope_editor/build.py docs/scope <scratchpad>/scope_editor.html`, `Artifact`
   read the editor's URL, then publish the built file to that same `url`. Set the rows you folded in to `merged`;
   delete one only once the republished page carries it, and leave `draft` rows alone.
4. Update project memory's scope line: base sha and Scope Editor version.
5. Reply: which files changed, the sha both copies now equal, and that his PC copy waits for the next PC round.

## Gotchas

- `/mnt/project-files` is a network mount: after copying, wait about ten seconds before the `diff -r`, or it reads stale.
- A `draft` row in the Scope Editor is his work in progress: never fold it in or delete it.
