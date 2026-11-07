# Batch checks

`run_checks.py` runs the project's mechanical checks through the Claude API's Message Batches endpoint, which costs
half the normal price and returns within 24 hours (most batches within one). It pays from the API credit (the Max
plan's $100 a month, `docs/PROJECT_SETUP.md` "API credit"), not from the plan's Claude Code usage.

| Check | What it reads | Recipe it repeats |
| --- | --- | --- |
| `text` | every string of two or more words in the six parts' game code (`Code/00`-`89`, self-tests left out), each text once with every place it appears, in requests of 250 | `docs/reports/TEXT_AUDIT_2026-11-05.md` §1: spelling (British), grammar, the naming rule, stale facts |
| `scope` | one request per scope file (`01`, `03`-`14`, `16`; `00`, `02` and `15` hold no game behaviour), with the game code of the parts it covers, line-numbered (`11` gets the mode and all four vessels, since each vessel's Directions span its files), PLAN Part C "Now" for what is queued, and for `07`-`10` the output of `TDS_Mod/Tools/cloud-check/scope_upgrades.py` | `docs/reports/SCOPE_CODE_AUDIT_2026-11-05.md`: each line to code `file:line`, marked MATCH, DIFFERENT, MISSING, UNBUILT-QUEUED or UNBUILT-BY-DESIGN |
| `design` | one request per vessel: its `DESIGN.md`, line-numbered, with its game code (the same cached block as its scope request) | each number and rule to code, marked MATCH, DIFFERENT or NOT-IN-CODE; the scope wins over a design page |
| `docs` | `CLAUDE.md`, PLAN Part C "Now", `docs/reports/README.md`, this README and `docs/skills/*.md`, after a free local pass that lists every cited path naming nothing in the repository | contradictions, stale facts, missing paths, unclear rules |
| `art` | PNGs, each scaled up (nearest neighbour) on Deep Swell and on Foam, with `concept-tools/temp_art/art_check.py`'s findings beside it, in requests of 8, against `docs/STYLE.md` | `docs/STYLE.md` "Art review" steps 1-2 by machine; the contact sheets for his eye stay a hand step |

`all` (the default) runs `text`, `scope`, `design` and `docs`. `art` runs when named (`--check art`: the art in use,
each part's `Art/*.png` and `Art/Temporary/game_size/`, about a hundred images), with `--art PATH...` (files or
folders inside the repository), or with `--since` when a part's art changed (`preview_8x` folders are skipped).

The result is one dated report, `docs/reports/BATCH_CHECKS_<date>.md`: a section per check, the rows to confirm first
(DIFFERENT and MISSING, DIFFERENT and NOT-IN-CODE), every row by file, each request's tokens and cost, and the month's
batch spend so far. Every row is a lead for a thread to confirm by hand before it changes code, docs, art or the scope.
Only the mod's own code and art, the scope and the project's docs are sent; never the game's binaries, the base
vessels' sprites or anything from `/mnt/project-files`.

## Run it

Python 3.10 or later, the SDK (`python3 -m pip install anthropic`) and Pillow for `art`. From the repository root:

```
python3 TDS_Mod/Tools/batch/run_checks.py plan                         # requests and cost estimate; sends nothing
python3 TDS_Mod/Tools/batch/run_checks.py plan --exact                 # exact input tokens (free endpoint; needs the key)
python3 TDS_Mod/Tools/batch/run_checks.py submit --model haiku         # the four checks on Haiku 5.5, waits, writes the report
python3 TDS_Mod/Tools/batch/run_checks.py submit                       # the same on Sonnet 5.5
python3 TDS_Mod/Tools/batch/run_checks.py submit --since last --model haiku --max-cost 3   # only what changed since each check's last full run
python3 TDS_Mod/Tools/batch/run_checks.py submit --check art --art TDS_Mod/ModSource/Spool/Art/Temporary/game_size
python3 TDS_Mod/Tools/batch/run_checks.py submit --check scope,design --scope-files 07,11 --no-wait
python3 TDS_Mod/Tools/batch/run_checks.py collect msgbatch_...         # finish a --no-wait or interrupted run
python3 TDS_Mod/Tools/batch/run_checks.py spend                        # this month's batch reports and their cost
```

Options: `--check` (`all`, or a comma list of `text,scope,design,docs,art`), `--model sonnet|haiku` (default `sonnet`
= `claude-sonnet-5-5`; `haiku` = `claude-haiku-5-5`), `--effort low|medium|high` (default `medium`; it sets how much
the model thinks, and thinking is billed as output), `--advisor none|opus` (default `none`; below), `--scope-files 07,11`,
`--art PATH...`.

- **`--advisor opus`** gives the `scope` and `design` requests the Claude API's advisor tool (beta
  `advisor-tool-2026-03-01`, `platform.claude.com/docs/en/agents-and-tools/tool-use/advisor-tool`): after a first pass
  over every line the model consults Opus 5.5 (`claude-opus-5-5`) once, before its answer, on the rows it is least
  sure of. The advisor reads the whole request (system, code, scope, what the model has written so far) under its own
  prompt of about 1,700 tokens, so each consult costs Opus 5.5's batch price ($2 in, $10 out per million) on the whole
  request, and the model then reads the request again. That is why it is off by default: the requests are 90K-540K
  tokens and the model consults once, the shape where the advisor costs most. On the four vessels' scope and design
  requests (`plan --exact`, base `d8cddbd`) Haiku 5.5 with the advisor is about $2.74, at most $4.44; Haiku alone
  $0.38; Sonnet 5.5 alone $1.52; Sonnet with the advisor $4.25. A full Haiku run with it is about $12 counted, so the
  default `--max-cost 15` trims it. The advisor's answer is capped at 2,048 tokens and is encrypted: only the model
  reads it. Tested 2026-11-07 on one made-up design request in a real batch: it consulted once, the answer kept its
  schema, $0.015 in all, $0.014 of it the advisor.

- **`--since REF`** keeps only what changed between `REF` and `HEAD` (committed changes): `text` for parts whose game
  code changed, `scope` for scope files whose parts or own text changed, `design` for vessels whose code or page
  changed, `docs` when one of its files did, `art` for changed PNGs. `--since last` diffs each check from its own
  base, kept in a hidden `batch-bases` line at the end of each report: the commit up to which that check last ran in
  full with every request answered. A narrowed run (`--scope-files`, `--art`), a failed or cut request, or an explicit
  `--since REF` leaves that check's base where it was, so nothing it left out is skipped later; a check with no base
  yet, or with a base outside the clone's history (a shallow clone is deepened once first), is checked whole. Nothing changed: it says so and sends nothing. A change to the mode's code wakes ten scope
  files, so such a run costs nearly a full one.
- **`--max-cost USD`** (default 15): `submit` refuses to send, exit code 4, when the most the run can cost is over
  it: every request writing its whole `max_tokens` (8,000 text, docs and art; 64,000 scope; 48,000 design; thinking
  included) and every cache block written, never read; with the advisor, each consult reading all of that and writing
  its 2,048, and the model reading it all again uncached. Over the cap, `submit` first takes the advisor off the
  largest requests, one at a time, and says which; only if the run is still over does it refuse. `submit` counts the input first (the free endpoint), since the
  3.5-characters guess runs low (2.78M guessed, 4.19M counted on base `1cc21d1`). Counted, a full run is about $1.33 and
  at most $2.56 on Haiku 5.5, about $5.48 and at most $10.52 on Sonnet 5.5; the first real full Haiku run cost $1.19,
  and $1.82 with the seven answers cut by the lower limits of that day rerun. `plan --exact` prints both figures.
- **The advisor's cost** is in every figure: `plan`'s estimate and most, the cap, and the report, whose cost line
  names the advisor's share and whose Requests table has an Advisor column (consults and their $, `not consulted`,
  or why it failed). The report prices each step (`usage.iterations`) at its own model's batch price, since the API's
  top-level token figures leave the advisor out.
- **`--max-wait MIN`** stops waiting after `MIN` minutes; the batch keeps running and `collect` finishes it.
- **Exit codes:** 0 done or nothing to check, 2 bad arguments, 3 no key (nothing sent), 4 over `--max-cost`, 1 any
  other failure.

`submit` polls once a minute; Ctrl+C stops waiting, not the batch. The batch id and what each request holds are kept
in `out/` beside the script (not committed), so `collect` needs that record: set `RW_BATCH_OUT` to keep it elsewhere.
A cloud container is gone after its thread, so a cloud thread waits for its batch (Haiku batches of this size
usually end within minutes) or sets `RW_BATCH_OUT=/mnt/project-files/batch` so a later thread can collect.

**The key.** Read from `RW_BATCH_API_KEY`, a name Claude Code never reads, so no session is ever offered the key;
`ANTHROPIC_API_KEY` works too, as a fallback. When neither is set, the script asks for the key once, hidden, and
stores it nowhere (with no terminal it stops with exit code 3). In this project's cloud threads the key is an
**environment variable** of the cloud environment (`RW_BATCH_API_KEY=...`, which Dimitri enters), not a network secret:
the agent proxy never attaches a network secret to `api.anthropic.com`. Every command in that environment's sessions
can read the variable; it is his private environment. On a Windows PC, set it for one PowerShell window
(`$env:RW_BATCH_API_KEY = "..."`); never set `ANTHROPIC_API_KEY` as a permanent user variable, since Claude Code on
that PC would then offer to bill every session to the key (`docs/PROJECT_SETUP.md` "API credit").

## On your PC, without Claude

`Run-Batch.ps1` beside this file runs everything above from a PowerShell window, with no Claude session, so it works
when the plan's five-hour limit is used up: the key bills the API credit, never the plan.

1. **Once:** Python 3.10 or later (`winget install Python.Python.3.12`), Git (`winget install Git.Git`), and a clone of
   the repository on the base branch: `git clone -b claude/phase-1-6lfnar https://github.com/dimitriphan/Rogue-Waves.git
   "$HOME\Rogue-Waves"` (Git asks you to sign in to GitHub the first time). Then save the key:
   `powershell -ExecutionPolicy Bypass -File "$HOME\Rogue-Waves\TDS_Mod\Tools\batch\Run-Batch.ps1" -SaveKey`. It asks
   for the key, hidden, and keeps it as your Windows user variable `RW_BATCH_API_KEY`, which Claude Code never reads.
2. **Any time:** `powershell -ExecutionPolicy Bypass -File "$HOME\Rogue-Waves\TDS_Mod\Tools\batch\Run-Batch.ps1"`.
   It pulls the newest base, installs `anthropic` and `pillow` the first time, sends Haiku 5.5 only what changed since
   each check's last full run (at most $3), waits for the batch and moves the report to `Desktop\RogueWave\batch`.
   `-Plan` gives the cost and sends nothing; `-Full` checks everything; `-Model sonnet -MaxCost 12` uses Sonnet 5.5 (at most about $10.52);
   `-Check scope,design` picks checks; `-Advisor opus` adds the advisor; `-Collect msgbatch_...` finishes a batch still running after the two-hour wait.
3. **Hand it back** when you are next in the project: "read the batch report on my Desktop". That folder is approved for
   the project's threads; a thread reads the report, confirms its leads and commits it in a docs PR.

The report leaves the clone's `docs\reports` (an untracked report there could stop the next `git pull`), so neither the
PC nor the cloud sees it until a thread commits it: until then a run checks the same changes again (it never skips any).

## From a thread or the morning routine

A thread that wants a check runs `submit` itself and reads the report; it commits the report in its own docs PR (Part
C "Now" names it). The cheap daily form, for the morning routine once the key is set:

```
[ -n "$RW_BATCH_API_KEY" ] && python3 -m pip install -q anthropic \
  && python3 TDS_Mod/Tools/batch/run_checks.py submit --since last --model haiku --max-cost 3 --max-wait 90
```

It sends only what changed since each check's last full run (usually cents; a full Haiku run is about $1.33, at most
$2.56), skips itself without the key, and writes the report the routine's PR carries. The month's figure: `spend`, or the last line of each report.

## Cost per run

Measured by `plan` on base `efc1157` (2026-11-06); input tokens estimated at 3.5 characters a token (an image at width
x height / 750), output guessed per request: 3,000 text, 14,000 scope, 10,000 design, 4,000 docs, 3,000 art (thinking
included). Batch prices are half the standard ones: Sonnet 5.5 $1 in / $5 out per million tokens; Haiku 5.5 $0.05 /
$0.25, or $0.25 / $1.25 for a prompt over 100K tokens (the mode's scope files are about 165K each).

| Run | Requests | Input tokens | Sonnet 5.5 | Haiku 5.5 |
| --- | --- | --- | --- | --- |
| `text` | 6 (1,310 strings) | about 30,000 | about $0.12 | about $0.01 |
| `scope`, all 14 files | 14 | about 2,420,000 | about $3.40 | about $0.73 |
| `design`, four vessels | 4 | about 313,000 | about $0.51 | about $0.03 |
| `docs` | 1 | about 15,000 | about $0.04 | under $0.01 |
| `all` (the four above) | 25 | about 2,778,000 (counted: 4,194,000) | about $4.07 (counted: $5.48) | about $0.76 (counted: $1.33; first run $1.19) |
| `art`, the art in use | 14 (106 images) | about 78,000 | about $0.29 | about $0.01 |
| `scope` for one vessel (`07`-`10`) | 1 | 68,000-91,000 | about $0.14-0.16 | about $0.01 |
| `scope` for `11` (Directions) | 1 | about 457,000 | about $0.53 | about $0.13 |

Each part's code is its own block, marked for prompt caching, the mode's first, so ten scope requests share the
mode's block and a vessel's scope and design requests share its block: a cache hit reads it at a tenth of the price,
a write costs 1.25 times. Inside a batch, hits are best effort, so a run can cost more than the estimate (never more
than the ceiling `plan` prints), or noticeably less. The report gives the real cost from the API's usage figures. Counted by `plan --exact` on base `d8cddbd`
(2026-11-07, 5.10M input tokens): a full Haiku run is about $1.56, at most $2.81; `--advisor opus` adds Opus 5.5
reading each of the 18 scope and design requests once. At these prices the
$100 a month covers about 20 full Sonnet runs, or a daily Haiku run with plenty to spare. Rerun `plan` after a big
change to refresh the numbers.

**Which model.** Sonnet 5.5 for a scope or design audit whose rows will drive work; Haiku 5.5 for the daily sweep,
the text, docs and art checks, and a cheap first pass. `--advisor opus` puts Opus 5.5's judgment on a scope or design
audit for about $0.30 a vessel request; Sonnet 5.5 alone costs about half that, so reach for the advisor when Opus's
read is worth it, and compare the two reports on one base before making it a habit. Neither replaces the hand-read audit's judgment: check every
DIFFERENT, MISSING and NOT-IN-CODE row against the code, and look at art yourself before changing it.

## What ran where

Built 2026-11-05 (`text`, `scope`), extended 2026-11-06 (`Run-Batch.ps1`, written here, not yet run on the PC: no
PowerShell in the container; `design`, `docs`, `art`, `--since`, `--max-cost`,
`--max-wait`, `spend`; review fixes: at most 2 cache marks a request, the API's limit being 4, which scope `11`'s five
parts broke; per-check bases; a true cost ceiling; `--art` sends only files git tracks). Here (cloud): `plan` for every
check on both models, the report writer on made-up results for every check, per-check bases through a failed, a
narrowed and an explicit-`--since` run, the art pictures looked at, and the missing-key and over-cap exits. Real runs since: the first on
2026-11-06 (Haiku, $1.82 with its rerun, `docs/reports/BATCH_CHECKS_2026-11-06.md` and `06b`) and the morning
routine's (Haiku, $1.32, `BATCH_CHECKS_2026-11-06c.md`); `spend` lists a month's runs.
