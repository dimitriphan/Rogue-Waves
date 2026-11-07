# Setting up the Rogue Waves Claude Project

What to hand the coordinator so a new or re-created project starts with nothing missing (first used 2026-10-08, at
Phase 1). Five things: the repository, the project instructions, the project files, the first message, and two PC-side
chores.

## 1. The repository

Connect `dimitriphan/Rogue-Waves`. Its default branch is **`claude/phase-1-6lfnar`** (switched from the old
`claude/rogue-waves-mod-setup-ngfuv3`, which stopped at PR #3, per Decisions 2026-10-15; no `main` exists): all work
lands there, and every thread branches from and opens its PR against it. It is
the only repository the project needs.

## 2. Project instructions (paste as the project's instructions)

> You coordinate Rogue Waves, Dimitri's expansion mod for TDS - Tower Defense Strategy. `CLAUDE.md` is the map: the
> checks to run, which skill to use when, and the rules the code doesn't show. Follow `docs/PLAN.md` Part A as your
> operating rules and read Part C "Now" for where the work stands; read nothing else until the task sends you there.
> The scope is `docs/scope/` (`00_Read_Me.txt` first; `~value~` marks Claude's defaults); game rules live in
> `TDS_Vessel_Creator/`. Dimitri's newest words win over both. Threads open PRs on `claude/phase-1-6lfnar`, each
> editing Part C "Now" (the base's entries first, its own last); Claude merges them once checked (Part A "Merging"). Say what was compiled here
> (cloud-check) and what ran on the PC; nothing touches his PC without his OK. Save lasting facts to project memory in
> the turn you learn them, and update Part C before a session ends.

(Proposed 2026-11-05 after the Claude Code best-practices page; the coordinator sets it. It replaces the 2026-11-04
text, which still pointed at `docs/SCOPE_v0.txt`. On 2026-11-04 Dimitri removed "keep chat replies under 150 words".)

Set the project's model to Fable 5.1; the "Model per role" table below says when a thread should drop to Opus 5.5.

## 3. Project files (upload to the project, never commit)

| File | From | Why |
|---|---|---|
| `TDS\DATA\Managed\Assembly-CSharp.dll` | `D:\Steam\steamapps\common\TDS\` | The game's code; Phase 1's decompiler (`docs/13` §11) needs it. Keep this path under the upload |
| `TDS\TDS Mod Tools.unitypackage` | same | Blockage's ModTool build; unpacked here it answers `docs/14` §6 question 1 (can a mod carry a scene) |
| `pc/QUEUE.md` | new, one line: `free` | The PC lock the round tools expect (`TDS_Mod/Tools/pc/README.md`) |
| A recent `Player.log` from a modded launch | `%USERPROFILE%\AppData\LocalLow\Blockage\TDS\` | Sample of what the boot and self-test checks read |

Optional: the other DLLs in `TDS\DATA\Managed` that `Build-<Name>Mod.ps1` references, if the cloud compile check
should match the PC byte for byte. Leave out the Addressables bundle, `resources.assets`, the Unity player and
everything else in the Steam folder; nothing here can open them.

## 4. The first message

> Read `docs/PLAN.md` Part C "Now" and carry on with its "Next". The game DLL and the TDS Mod Tools package are in
> the project files.

The first project (2026-10-08) started with "Start Phase 1 from `docs/PLAN.md` Part B"; Phases 0-2 are done since.
Upload the DLL first: without it a session can only grep the API dump (`docs/13` §11).

## 5. PC-side chores (before the first PC round, after Phase 2)

- Optional: a read-only GitHub token for `dimitriphan/Rogue-Waves` saved as `C:\Users\Dimitri\.modprojects_token`
  (`TDS_Mod/Tools/pc/README.md`). PC sessions fetch with the signed-in `gh` instead; make the token only if a round must
  run without `gh`.
- Nothing else changes on the PC: `C:\Users\Dimitri\TDS_Mod` stays the Unity project, and handoffs arrive the same way.

## What the coordinator will do on its own

Run `TDS_Mod/Tools/cloud-check/setup.sh` once per container when it first needs a compile check, follow Part A (one
gate at a time, questions collected for the next gate), and keep Part C current.

## Model per role

(Moved from `docs/PLAN.md` Part A, 2026-11-05.)

Fable 5.1 costs 2.5x Opus 5.5 per token (same 1M context); its edge is judgment on open-ended problems. The plan
puts the judgment in the coordinator and the volume in the threads, so the models follow that split.

| Role | Model | Why |
| --- | --- | --- |
| Coordinator at a gate (Phase 1 go/fallback, packaging, design gates, definition of done) | Fable 5.1 | Few tokens, costly if wrong |
| Coordinator on routine sessions (session close, packing a handoff, Phase 6 data passes) | Opus 5.5 | Routine |
| Stand-in PR reviewer when Codex stalls or is out of usage (`/code-review high`, PLAN Part A "Merging") | Opus 5.5 | Rule-bound, `AGENTS.md` P0-P3 |
| Threads by default (Part D research, code modules, sprite generators, cloud-check loops) | Opus 5.5 | The kit makes this work routine |
| Batch checks (`TDS_Mod/Tools/batch/run_checks.py`, API credit) | Haiku 5.5 daily, Sonnet 5.5 for an audit that drives work; `--advisor opus` lets scope and design consult Opus 5.5 once a request (advisor tool, beta `advisor-tool-2026-03-01`), off by default | Opus reads the whole request each consult, so it costs more than Sonnet alone (batch README "`--advisor opus`") |
| Threads on open-ended engine work: Phase 1 decompiler research (mode chain, save writes, client UI), Phase 5's 10-map campaign loop and its online sync, a PC round whose self-test fails for an unnamed reason after one Opus pass | Fable 5.1 | New territory, no prior art in the kit |

Escalation rule: an Opus thread that fails twice at the same problem hands it to a Fable thread with a short summary
of what was tried. Prompting: Fable does better with the goal and the gate than with steps, so give a Fable thread the
outcome and the constraints; the Part D templates stay as written for Opus threads.

## API credit

(Checked 2026-11-05 against the Claude Code docs and the Claude help centre; sources at the end.) The Max plan
includes $100 of API credit a month for the Claude Platform. It sits in the Claude Console organization linked to the
plan, shows on the Console's Billing page (`platform.claude.com/settings/billing`) with its expiry, does not roll
over, and is spent before any credit you buy. When it runs out, API requests stop until next month's credit, unless
you buy credit or turn on auto-reload.

**What it can pay for.** API calls made with a key from that organization: the batch checks
(`TDS_Mod/Tools/batch/README.md`; with `--advisor opus` also Opus 5.5's consults, counted in the run's cap), and `claude -p` or Agent SDK scripts you run yourself with that key.

**What it can't.** Interactive Claude Code (terminal, IDE, desktop, web), this project's threads, and the plan's
usage credits. So the credit cannot be an overflow for the plan. There is also no automatic overflow from the plan
to a key: with `ANTHROPIC_API_KEY` set, Claude Code in a terminal asks once whether to use it, and if you say yes it
bills every session to the key from the first message, instead of the plan, not after the limit. Cloud sessions,
these threads included, always use the plan and ignore the key.

### When the plan's limit is hit

1. **Usage credits** (the real overflow, paid separately at API prices). At `claude.ai/settings/usage`, section
   "Usage credits": **Enable**, **Adjust limit** (a monthly cap, or unlimited), **Add funds**, **Purchase**. Auto-reload
   and usage alerts are on the same page. They cover Claude chats and Claude Code in the terminal; the help article
   does not say whether they cover cloud sessions. The $100 API credit can't pay for them.
2. **Continue on API credit.** At the limit, Claude Code can offer to carry on through Console API credit at API
   prices; saying no keeps you on the plan. Only bought credit pays for this; the monthly $100 does not.
3. **Wait** for the five-hour or weekly limit to reset (`/status` shows what is left).

### Set up the key for the batch checks (Windows)

1. **Check the credit.** Console, Billing page: a monthly credit line with an expiry date. If there is none, link a
   Console organization to the plan as the help article "Monthly API credits for Max, Ultra, and Team plans" says.
   Leave auto-reload off: the $100 then is a hard cap.
2. **Make a key.** Console, Settings, API keys, **Create key**, name it `rogue-waves-batch`. It is shown once; keep it in
   a password manager. Never paste it into a chat, a scope file or the repository.
3. **Give it to the batch, not to Claude Code.** Two ways:
   - **Cloud (recommended; nothing touches the PC).** In the project's settings on claude.ai, Cloud environment, the
     gear beside the selected environment: under **Environment variables** add one line, `RW_BATCH_API_KEY=<the key>`.
     Not under Network secrets: the agent proxy never attaches one to `api.anthropic.com` (Claude Code docs "Configure
     cloud environments", "Requests that never get the secret"). The batch reads `RW_BATCH_API_KEY`, a name Claude Code
     itself never reads. A new thread then runs `plan --exact` (free) to prove it, and `submit --model haiku` (about $1.20).
   - **On the PC, with no Claude session** (works when the plan's five-hour limit is used up, since the key bills the
     API credit, not the plan): `TDS_Mod/Tools/batch/README.md` "On your PC, without Claude". Once:
     `Run-Batch.ps1 -SaveKey` keeps the key, asked for hidden, as the Windows user variable `RW_BATCH_API_KEY`; then
     `Run-Batch.ps1` runs the cheap daily form and moves the report to `Desktop\RogueWave\batch`. Don't set
     `ANTHROPIC_API_KEY` as a permanent user variable (System Properties or `setx`): every Claude Code session on the
     PC, the Remote Control rounds included, would then be offered the key, and `claude -p` uses it without asking.
4. **Confirm it is active.** `plan --exact` prints counted (not estimated) tokens; `submit` prints a batch id
   (`msgbatch_...`); the report ends with each request's cost; the Console's Billing page shows the spend. In Claude
   Code, `/status` names the credential in use and marks the unused one when both a login and a key exist;
   `/config`, "Use custom API key", changes the choice while the variable is set. To undo a key in a window:
   `Remove-Item Env:RW_BATCH_API_KEY`.

Sources: Claude Code docs "Authentication" (precedence; cloud sessions keep the plan); help centre "Use Claude Code
with your Pro, Max, or Ultra plan", "Monthly API credits for Max, Ultra, and Team plans", "Manage usage credits for
paid Claude plans", "Manage API key environment variables in Claude Code".
