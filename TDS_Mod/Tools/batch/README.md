# Batch checks

`run_checks.py` runs the project's two mechanical checks through the Claude API's Message Batches endpoint, which
costs half the normal price and returns within 24 hours (most batches within one). It pays from the API credit (the
Max plan's $100 a month, `docs/PROJECT_SETUP.md` "API credit"), not from the plan's Claude Code usage.

| Check | What it reads | Recipe it repeats |
| --- | --- | --- |
| `text` | every string of two or more words in the six parts' game code (`Code/00`-`89`, self-tests left out), each text once with every place it appears, in requests of 250 | `docs/reports/TEXT_AUDIT_2026-11-05.md` §1: spelling (British), grammar, the naming rule, stale facts |
| `scope` | one request per scope file (`01`, `03`-`14`, `16`; `00`, `02` and `15` hold no game behaviour), with the game code of the parts it covers, line-numbered (`11` gets the mode and all four vessels, since each vessel's Directions span its files), PLAN Part C "Now" for what is queued, and for `07`-`10` the output of `TDS_Mod/Tools/cloud-check/scope_upgrades.py` | `docs/reports/SCOPE_CODE_AUDIT_2026-11-05.md`: each line to code `file:line`, marked MATCH, DIFFERENT, MISSING, UNBUILT-QUEUED or UNBUILT-BY-DESIGN |

The result is one dated report, `docs/reports/BATCH_CHECKS_<date>.md`: the text findings, a mark count per scope
file, the DIFFERENT and MISSING rows first, every row by file, then each request's tokens and cost. Every row is a lead
for a thread to confirm by hand before it changes code or the scope. Only the mod's own code, the scope files and
PLAN Part C are sent; never the game's binaries or the base vessels' sprites.

## Run it

Python 3.10 or later, and the SDK: `python3 -m pip install anthropic`. From the repository root:

```
python3 TDS_Mod/Tools/batch/run_checks.py plan                    # requests and cost estimate; sends nothing
python3 TDS_Mod/Tools/batch/run_checks.py plan --exact            # exact input tokens (free endpoint; needs the key)
python3 TDS_Mod/Tools/batch/run_checks.py submit                  # both checks on Sonnet 5.5, waits, writes the report
python3 TDS_Mod/Tools/batch/run_checks.py submit --model haiku    # the same on Haiku 5.5
python3 TDS_Mod/Tools/batch/run_checks.py submit --check scope --scope-files 07,11 --no-wait
python3 TDS_Mod/Tools/batch/run_checks.py collect msgbatch_...    # finish a --no-wait or interrupted run
```

Options: `--check text|scope|all` (default `all`), `--model sonnet|haiku` (default `sonnet` = `claude-sonnet-5-5`;
`haiku` = `claude-haiku-5-5`), `--effort low|medium|high` (default `medium`; it sets how much the model thinks, and
thinking is billed as output). `submit` polls once a minute; Ctrl+C stops waiting, not the batch. The batch id and
what each request holds are kept in `out/` beside the script (not committed), so `collect` runs on the machine that
submitted.

**The key.** Read from `ANTHROPIC_API_KEY`. When the variable is unset, the script asks for the key once, hidden, and
stores it nowhere. On a Windows PC, set it for one PowerShell window only (`$env:ANTHROPIC_API_KEY = "..."`), never as
a permanent user variable: Claude Code on that PC would then offer to bill every session to the key
(`docs/PROJECT_SETUP.md` "API credit"). In this project's cloud threads, the key lives in the cloud environment's
settings, which Dimitri enters; a cloud session's own Claude Code keeps using the plan whatever the key.

## Cost per run

Measured by `plan` on base `8b7daed` (2026-11-05); input tokens estimated at 3.5 characters a token, output guessed
at 3,000 tokens per text request and 14,000 per scope file (thinking included). Batch prices are half the standard
ones: Sonnet 5.5 $1 in / $5 out per million tokens; Haiku 5.5 $0.05 / $0.25, or $0.25 / $1.25 for a prompt over 100K
tokens (the mode's scope files are about 165K each).

| Run | Requests | Input tokens | Sonnet 5.5 | Haiku 5.5 |
| --- | --- | --- | --- | --- |
| `text` | 6 (1,310 strings) | about 30,000 | about $0.12 | about $0.01 |
| `scope`, all 14 files | 14 | about 2,355,000 | about $3.34 | about $0.71 |
| `all` (the default) | 20 | about 2,385,000 | about $3.46 | about $0.72 |
| `scope` for one vessel (`07`-`10`) | 1 | 68,000-91,000 | about $0.14-0.16 | about $0.01 |
| `scope` for `11` (Directions) | 1 | about 457,000 | about $0.53 | about $0.13 |

Each part's code is its own block, marked for prompt caching, the mode's first, so ten scope requests share the
mode's block: a cache hit reads it at a tenth of the price, a write costs 1.25 times. Inside a batch, hits are best
effort, so a run can cost up to about $0.57 more on Sonnet or $0.13 on Haiku, or noticeably less. The report gives the
real cost from the API's usage figures. At these prices the $100 a month covers about 25 full Sonnet runs or 120 Haiku
runs. Rerun `plan` after a big code
change to refresh the numbers.

**Which model.** Sonnet 5.5 for a scope audit whose rows will drive work; Haiku 5.5 for a cheap first sweep or the
text check. Neither replaces the hand-read audit's judgment: check every DIFFERENT and MISSING row against the code.

## What ran where

Built 2026-11-05. Here (cloud): `plan` for every check on both models, the report writer on made-up results, and the
missing-key exit. No real batch has run: no key was available in the container.
