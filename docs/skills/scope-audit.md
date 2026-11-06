---
name: scope-audit
description: Use in Rogue Waves when asked to check the code against Dimitri's scope (a scope audit or scope-to-code check), one docs/scope file at a time.
---

# Audit Rogue Waves code against the scope

Method from `docs/reports/SCOPE_CODE_AUDIT_2026-11-05.md` (read its opening table of marks and one "File by file"
section as the model, not the whole page). Effort high. Dimitri's newest words beat the scope, and the scope beats the
code.

## Setup

1. Read `docs/scope/00_Read_Me.txt` "Rules" (symbols and values) once.
2. One scope file per pass. A vessel file pairs with its part: `07` Yossele, `08` Spool, `09` Argo, `10` Uloe.
3. Know the queues before marking: `docs/reports/DECISIONS_APPLIED_2026-11-05.md` §3-4 and the newest
   `BUILD_*`/`DECISIONS_*` reports name what is already queued to build.

## Each line

Every `- Field: value` slot, every `% N:{Name}` upgrade and every `{Name}` sheet gets one row. `#` lines are design
notes and never hold a value: skip them.

| Mark | Means |
| --- | --- |
| MATCH | the code does what the line says, at the `file:line` given |
| RESOLVED | a row of an earlier audit that no longer disagrees, and why |
| UNBUILT-QUEUED | not built, already on a queued build list (name the item, e.g. B6) |
| UNBUILT-BY-DESIGN | not built, and the scope says so (Depth on hold, a `! NEEDS !` slot) |
| DIFFERENT | built, and the code does something else: new work |
| MISSING | asked for, nothing builds it, nothing queues it: new work |

Row: scope `file:line`, the item, the mark, the code `file:line` (relative to `TDS_Mod/ModSource`), one short note.
A `~value~` is Claude's default and is audited like any value; say it is a default.

## Helper for upgrades

From the repository root:
`python3 TDS_Mod/Tools/cloud-check/scope_upgrades.py docs/scope/<NN>_Vessel_<Part>.txt TDS_Mod/ModSource/<Part>/Code/02_Data.cs`
matches every `% N:{Name}` to its `U(...)` entry (scope level N = tier N+1, cost, in-game line, Leads to). `V=1`
before the command adds the Numbers line and the code parameters side by side, to read by hand.

## Output

1. `docs/reports/SCOPE_CODE_AUDIT_<yyyy-mm-dd>.md` (or a new section of today's): the base commit, a summary table per
   file (rows, MATCH, UNBUILT-QUEUED, UNBUILT-BY-DESIGN, DIFFERENT/MISSING), then the file's rows. Number the new
   DIFFERENT and MISSING rows (N1, N2, ...) so build threads can take them.
2. Change no game code in the audit. Fix scope text only where it contradicts a decision Dimitri already made, and
   list each fix.
3. Docs-only PR with `ship-a-pr`: grep that every cited `file:line` exists; say "docs only, read from the code,
   nothing compiled or run on the PC".
4. A scope file the audit edited makes his copies out of date: say so, and sync only at his word (`scope-sync`).
