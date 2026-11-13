# Project skills

Four procedures every Rogue Waves thread used to have re-explained. Claude Code loads them from this folder in any
session opened on the repository (cloud threads and the PC alike), front matter included.

| Skill | Use it when | Runs |
| --- | --- | --- |
| `ship-a-pr` | a thread opens, updates or merges its own pull request | in the thread; fills in the base head, branch, next free numbers |
| `pc-round` | code must be built, self-tested or played on Dimitri's PC | in the thread, effort xhigh |
| `scope-audit` | the code is checked against one `docs/scope` file | as its own sub-agent (`context: fork`, Opus, high): only the result returns |
| `scope-sync` | Dimitri says "sync", or sends back an edited scope file | in the thread, effort medium |

## Keeping them current

These files are the source; there is no second copy to save (the claude.ai copies from before 2026-11-13 are
duplicates for Dimitri to delete: claude.ai keeps only a skill's name, description and body, so `context: fork` and
the other settings work only here). When a rule a skill cites changes (`CLAUDE.md`, `docs/PLAN.md` Part A,
`TDS_Mod/Tools/pc/README.md`, `docs/scope/00_Read_Me.txt`), the PR that changes it edits the skill too. A skill names
no number that changes (next free kit or pitfall numbers, sha, versions): it points to where that lives, or fills it
in with a `!` command when it loads. Each skill ends with **Gotchas**: add a line whenever a thread trips on something
the steps did not warn about.
