---
name: ship-a-pr
description: Use in the Rogue Waves repository when a thread opens, updates or merges its own pull request on claude/phase-1-6lfnar.
---

# Ship a Rogue Waves pull request

Rules from `CLAUDE.md`, `docs/PLAN.md` Part A "Merging" and Part C "Now". Dimitri's newest words win over this page.

## Live state (filled in when the skill loads; don't re-read it)

Where a line still shows its `!` command (the claude.ai copy may not run them), run the commands once yourself.


- Base head: !`cd "$(git rev-parse --show-toplevel 2>/dev/null || echo /home/user/Rogue-Waves)" && git fetch -q origin claude/phase-1-6lfnar 2>/dev/null; git log -1 --format='%h %s' origin/claude/phase-1-6lfnar`
- This branch: !`cd "$(git rev-parse --show-toplevel 2>/dev/null || echo /home/user/Rogue-Waves)" && git branch --show-current; git status --short | wc -l | sed 's/$/ changed files/'`
- !`cd "$(git rev-parse --show-toplevel 2>/dev/null || echo /home/user/Rogue-Waves)" && grep -m1 'Next free numbers' docs/PLAN.md`
- Newest Log files: !`cd "$(git rev-parse --show-toplevel 2>/dev/null || echo /home/user/Rogue-Waves)" && ls docs/log | grep -v -e README -e archive | tail -3`

## Before the PR

1. Work on the thread's own `claude/...` branch, started from `origin/claude/phase-1-6lfnar`. Never push to the base.
2. "Next free numbers" (Live state above) gives the next kit version, kit chapter and pitfall number; take yours from
   there, never from memory. Read Part C "Now" (only that section) when you edit its lines.
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
   Every PR, code or docs, from the root: `python3 TDS_Mod/Tools/cloud-check/check_rules.py` (AGENTS.md's mechanical
   rules, free; GitHub runs it on each PR too). Fix what it flags before Codex sees the PR.
3. Docs only: no compile, but every path, section and number the change cites must exist (grep each one).
4. Code: `mattpocock-skills:code-review` against `origin/claude/phase-1-6lfnar` and fix what it confirms.
   `/security-review` only for code that reads another player's messages or a save.
5. Say which it was: "compiled here" (with the command and its output) or "self-test N/0 on the PC". A clean compile
   is not a test.

## Part C and the Log

- Do not add dated entries to Part C "Now" (Dimitri 2026-11-12, token use). Edit its State, Next, open and "Next free
  numbers" lines in place so they stay true, and keep it under about 4 KB.
- The Log is one new file per PR, `docs/log/<YYYY-MM-DD>_<branch's last part>.md` (Dimitri 2026-11-13; `docs/log/README.md`):
  `- **YYYY-MM-DD: what changed** (Dimitri: "his words"): result; compiled here / not run on the PC; kit or pitfall used.`
  The detail goes under it in the same file. Never append to `docs/PLAN.md` or the frozen archive.

## Open

1. Commit (message under five lines, ending with the session's attribution lines) and `git push -u origin <branch>`.
2. GitHub MCP `create_pull_request`, base `claude/phase-1-6lfnar`. The body starts with the two attribution lines from
   the session context, verbatim (the `claude-projects-attribution` marker for `dimitriphan`, then the
   "Requested by" line with the thread link). Write the rest with `mattpocock-skills:pr`, keeping: what changed,
   what was checked and how, what ran on the PC.
3. Assign and request review from `dimitriphan` (`issue_write` update, `update_pull_request` reviewers), then
   `subscribe_pr_activity`.

## Review (Codex, Dimitri 2026-11-06)

Codex, from Dimitri's ChatGPT plan, reviews every PR on its own against `AGENTS.md` "Code Review Rules"; no Claude
reviewer sub-agent while Codex answers (fallback in step 3). `mattpocock-skills:code-review` before a code PR stays, as the
author's own check.

1. Codex's comments arrive as PR events. Answer each one in its thread: fix it and say which commit, or say why not.
   It marks findings P0 to P3; P0 and P1 must be fixed or answered before the merge.
2. Codex never approves: its review of the current head with no P0/P1 left open is the approval. A clean review is
   a 👍 reaction on the PR; its "Codex Review Summary" comment tracks the run per commit. A push after its review
   gets a new review; wait for it (or comment `@codex review`).
3. No review an hour after opening or pushing: comment `@codex review` once. Still none an hour later, or Codex
   answers that its usage limit is reached (Dimitri 2026-11-07): tell Dimitri in the thread, then run a Claude
   reviewer sub-agent (model: `docs/PROJECT_SETUP.md` "Model per role") with `mattpocock-skills:code-review` on the head,
   labelling findings P0-P3 under the same rules. Fix or answer each P0/P1; after a fix push the same reviewer
   re-reviews the new head (no new Codex wait). Note in the PR body that Claude reviewed in Codex's place; merge once
   no P0/P1 is open.

## Merge (Claude merges its own PR, Dimitri 2026-11-05)

0. Codex's review, or the stand-in Claude review (above), of the head has no P0/P1 open, and cloud-check is clean.
1. `git fetch origin claude/phase-1-6lfnar` and merge it into your branch. In Part C "Now", keep the base's lines and
   re-apply your edits (your Log file never conflicts). If a PR merged first used the same kit version or pitfall numbers, you are the second
   merger: renumber yours to the next free ones everywhere they appear, and fix "Next free numbers".
2. Re-run the checks above on the merged tree and push.
3. GitHub MCP `merge_pull_request` with `merge_method: "merge"` and `expectedHeadSha` set to your pushed head.
   `gh pr merge` is refused here. Do not merge a PR whose own thread is still working on it.
4. After the merge: tell the coordinator (`message_coordinator`), so the threads with PRs still open re-merge the base
   and re-run cloud-check. Update the PR STATE line in project memory (merged range, base sha, next free numbers).
   Tell the phone widget: `StatusWidget/status/set_status.py --id <thread id> --state shipped --step "PR #<n>: <title>"`
   (`StatusWidget/README.md` "When a thread reports").
5. A Remote Control (PC) thread has no GitHub tool: a cloud thread opens and merges its PR for it.

## Gotchas

- Codex replying "create an environment" means its usage limit is reached: go to the fallback (Review step 3).
- Docs and prompt PRs (no code changed) never get the Claude stand-in reviewer; they wait for Codex alone.
- A merged PR's branch is restarted from the newest base under the same name; the new work is a new PR.
- A base merge that brings a Log entry into `docs/PLAN.md` (from a PR opened before 2026-11-13): move it into its own
  `docs/log/` file in the merge commit.
- `/mnt/project-files` is a network mount and is never committed; a file from it reaches the repo only by Dimitri's word.
