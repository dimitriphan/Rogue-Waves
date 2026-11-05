# Project skills (drafts)

Four procedures every Rogue Waves thread used to have re-explained, written as skills. Each file is a complete
`SKILL.md`: a `name`, a one-line `description` (what makes Claude reach for it) and the steps.

| File | Use it when |
| --- | --- |
| `ship-a-pr.md` | a thread opens, updates or merges its own pull request |
| `pc-round.md` | code must be built, self-tested or played on Dimitri's PC |
| `scope-audit.md` | the code is checked against one `docs/scope` file |
| `scope-sync.md` | Dimitri says "sync", or sends back an edited scope file |

## Installing

- **From the thread:** Claude posts each skill on a save card; press Save on each.
- **By hand:** in claude.ai's settings, open Skills, create a skill, and paste the whole file (front matter
  included); the name and description come from its top lines.

## Keeping them current

These files are the source. When a rule they cite changes (`CLAUDE.md`, `docs/PLAN.md` Part A, `TDS_Mod/Tools/pc/README.md`,
`docs/scope/00_Read_Me.txt`), the PR that changes it edits the skill here too and posts it again for saving. A
skill names no number that changes (next free kit or pitfall numbers, sha, versions): it points to where that lives.
