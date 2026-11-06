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
python3 TDS_Mod/Tools/batch/run_checks.py submit --since last --model haiku --max-cost 2   # only what changed since each check's last full run
python3 TDS_Mod/Tools/batch/run_checks.py submit --check art --art TDS_Mod/ModSource/Spool/Art/Temporary/game_size
python3 TDS_Mod/Tools/batch/run_checks.py submit --check scope,design --scope-files 07,11 --no-wait
python3 TDS_Mod/Tools/batch/run_checks.py collect msgbatch_...         # finish a --no-wait or interrupted run
python3 TDS_Mod/Tools/batch/run_checks.py spend                        # this month's batch reports and their cost
```

Options: `--check` (`all`, or a comma list of `text,scope,design,docs,art`), `--model sonnet|haiku` (default `sonnet`
= `claude-sonnet-5-5`; `haiku` = `claude-haiku-5-5`), `--effort low|medium|high` (default `medium`; it sets how much
the model thinks, and thinking is billed as output), `--scope-files 07,11`, `--art PATH...`.

- **`--since REF`** keeps only what changed between `REF` and `HEAD` (committed changes): `text` for parts whose game
  code changed, `scope` for scope files whose parts or own text changed, `design` for vessels whose code or page
  changed, `docs` when one of its files did, `art` for changed PNGs. `--since last` diffs each check from its own
  base, kept in a hidden `batch-bases` line at the end of each report: the commit up to which that check last ran in
  full with every request answered. A narrowed run (`--scope-files`, `--art`), a failed or cut request, or an explicit
  `--since REF` leaves that check's base where it was, so nothing it left out is skipped later; a check with no base
  yet, or with a base outside the clone's history (a shallow clone is deepened once first), is checked whole. Nothing changed: it says so and sends nothing. A change to the mode's code wakes ten scope
  files, so such a run costs nearly a full one.
- **`--max-cost USD`** (default 10): `submit` refuses to send, exit code 4, when the most the run can cost is over
  it: every request writing its whole `max_tokens` (8,000 text, docs and art; 40,000 scope; 24,000 design; thinking
  included) and every cache block written, never read. That is a true ceiling: a full Sonnet run is at most $6.94
  (about $4.07 expected), a full Haiku run at most $1.26 (about $0.76). `plan` prints both figures.
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

## From a thread or the morning routine

A thread that wants a check runs `submit` itself and reads the report; it commits the report in its own docs PR (Part
C "Now" names it). The cheap daily form, for the morning routine once the key is set:

```
[ -n "$RW_BATCH_API_KEY" ] && python3 -m pip install -q anthropic \
  && python3 TDS_Mod/Tools/batch/run_checks.py submit --since last --model haiku --max-cost 2 --max-wait 90
```

It sends only what changed since each check's last full run (usually cents; a full Haiku run is about $0.76 and at
most $1.26), skips itself without the key, and writes the report the routine's PR carries. The month's figure: `spend`, or the last line of each report.

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
| `all` (the four above) | 25 | about 2,778,000 | about $4.07 | about $0.76 |
| `art`, the art in use | 14 (106 images) | about 78,000 | about $0.29 | about $0.01 |
| `scope` for one vessel (`07`-`10`) | 1 | 68,000-91,000 | about $0.14-0.16 | about $0.01 |
| `scope` for `11` (Directions) | 1 | about 457,000 | about $0.53 | about $0.13 |

Each part's code is its own block, marked for prompt caching, the mode's first, so ten scope requests share the
mode's block and a vessel's scope and design requests share its block: a cache hit reads it at a tenth of the price,
a write costs 1.25 times. Inside a batch, hits are best effort, so a run can cost more than the estimate (never more
than the ceiling `plan` prints), or noticeably less. The report gives the real cost from the API's usage figures. At these prices the
$100 a month covers about 20 full Sonnet runs, or a daily Haiku run with plenty to spare. Rerun `plan` after a big
change to refresh the numbers.

**Which model.** Sonnet 5.5 for a scope or design audit whose rows will drive work; Haiku 5.5 for the daily sweep,
the text, docs and art checks, and a cheap first pass. Neither replaces the hand-read audit's judgment: check every
DIFFERENT, MISSING and NOT-IN-CODE row against the code, and look at art yourself before changing it.

## What ran where

Built 2026-11-05 (`text`, `scope`), extended 2026-11-06 (`design`, `docs`, `art`, `--since`, `--max-cost`,
`--max-wait`, `spend`; review fixes: at most 2 cache marks a request, the API's limit being 4, which scope `11`'s five
parts broke; per-check bases; a true cost ceiling; `--art` sends only files git tracks). Here (cloud): `plan` for every
check on both models, the report writer on made-up results for every check, per-check bases through a failed, a
narrowed and an explicit-`--since` run, the art pictures looked at, and the missing-key and over-cap exits. No real batch has run: no key was
available in the container.
