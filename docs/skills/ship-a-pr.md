---
name: ship-a-pr
description: Use in the Rogue Waves repository when a thread opens, updates or merges its own pull request on claude/phase-1-6lfnar.
---

# Ship a Rogue Waves pull request

Rules from `CLAUDE.md`, `docs/PLAN.md` Part A "Merging" and Part C "Now". Dimitri's newest words win over this page.

## Before the PR

1. Work on the thread's own `claude/...` branch, started from `origin/claude/phase-1-6lfnar`. Never push to the base.
2. Read Part C "Now" in `docs/PLAN.md` (only that section). Its "Next free numbers" line gives the next kit version,
   kit chapter and pitfall number; take yours from there, never from memory.
3. A kit change: bump the version in `TDS_Vessel_Creator/kit/README.md` (its version table) and the file headers,
   and keep `TDS_Mod/ModSource/Shared` equal to `TDS_Vessel_Creator/kit/Shared` (`diff -r`). A new engine fact or
   error: `TDS_Vessel_Creator/docs/09_PITFALLS.md` at the next free number. Update the "Next free numbers" line.
4. Never commit the game's binaries, base-vessel game sprites, or anything from `/mnt/project-files`.

## Check

1. Once per container: `cd TDS_Mod/Tools/cloud-check && ./setup.sh ../../../TDS_Vessel_Creator`
   (needs `apt-get update && apt-get install -y mono-devel` in a fresh container).
2. For each changed part, from `TDS_Mod/ModSource`:
   `python3 ../Tools/cloud-check/check_mod.py <Part>/Code Shared <Part> ""`, again with its self-test defines
   (`"<PART>_SELFTEST"`, plus `_SHOTS`, `_GAPS`, `_TWOPC` where the part has them), and
   `../Tools/cloud-check/check_editor.sh ../Assets/<Part>Mod/Editor`. A kit change compiles every part.
3. Docs only: no compile, but every path, section and number the change cites must exist (grep each one).
4. Code: `/code-review high` on the diff (never `max` unless Dimitri names it) and fix what it confirms.
   `/security-review` only for code that reads another player's messages or a save.
5. Say which it was: "compiled here" (with the command and its output) or "self-test N/0 on the PC". A clean compile
   is not a test.

## Part C and the Log

- Add one entry at the END of Part C "Now", three lines at most:
  `- **YYYY-MM-DD: what changed** (Dimitri: "his words"): result; compiled here / not run on the PC; kit or pitfall used.`
- Put the detail in the Log (`### Log`, newest last) as one dated entry. Keep "Now" near 4 KB: fold finished items
  into its "State" line.

## Open

1. Commit (message under five lines, ending with the session's attribution lines) and `git push -u origin <branch>`.
2. GitHub MCP `create_pull_request`, base `claude/phase-1-6lfnar`. The body starts with the two attribution lines from
   the session context, verbatim (the `claude-projects-attribution` marker for `dimitriphan`, then the
   "Requested by" line with the thread link). Then: what changed, what was checked and how, what ran on the PC.
3. Assign and request review from `dimitriphan` (`issue_write` update, `update_pull_request` reviewers), then
   `subscribe_pr_activity`.

## Review (Codex, Dimitri 2026-11-06)

Codex, from Dimitri's ChatGPT plan, reviews every PR on its own against `AGENTS.md` "Code Review Rules"; no Claude
reviewer sub-agent while Codex answers (fallback in step 3). `/code-review high` before a code PR stays, as the author's own check.

1. Codex's comments arrive as PR events. Answer each one in its thread: fix it and say which commit, or say why not.
   It marks findings P0 to P3; P0 and P1 must be fixed or answered before the merge.
2. Codex never approves: its review of the current head with no P0/P1 left open is the approval. A clean review is
   a 👍 reaction on the PR; its "Codex Review Summary" comment tracks the run per commit. A push after its review
   gets a new review; wait for it (or comment `@codex review`).
3. No review an hour after opening or pushing: comment `@codex review` once. Still none an hour later (his ChatGPT
   usage ran out, or its GitHub app; Dimitri 2026-11-07): run a Claude reviewer sub-agent (Opus 5.5,
   `/code-review high` on the head, same rules), fix what it raises, note in the PR body that Claude reviewed in
   Codex's place, and merge once that review is clean.

## Merge (Claude merges its own PR, Dimitri 2026-11-05)

0. Codex's review, or the stand-in Claude review (above), is in and clean, and cloud-check is clean.
1. `git fetch origin claude/phase-1-6lfnar` and merge it into your branch. In Part C "Now", keep the base's entries
   first and yours last. If a PR merged first used the same kit version or pitfall numbers, you are the second
   merger: renumber yours to the next free ones everywhere they appear, and fix "Next free numbers".
2. Re-run the checks above on the merged tree and push.
3. GitHub MCP `merge_pull_request` with `merge_method: "merge"` and `expectedHeadSha` set to your pushed head.
   `gh pr merge` is refused here. Do not merge a PR whose own thread is still working on it.
4. After the merge: tell the coordinator (`message_coordinator`), so the threads with PRs still open re-merge the base
   and re-run cloud-check. Update the PR STATE line in project memory (merged range, base sha, next free numbers).
   Tell the phone widget: `StatusWidget/status/set_status.py --id <thread id> --state shipped --step "PR #<n>: <title>"`
   (`StatusWidget/README.md` "When a thread reports").
5. A Remote Control (PC) thread has no GitHub tool: a cloud thread opens and merges its PR for it.
