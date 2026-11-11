# Batch button (a Claude Code mod)

`rw-batch-button` puts a **Batch checks** band above the prompt in any Claude Code session opened on a Rogue Waves
clone, with three buttons, and adds `/run-batch`:

| Button | Command | What runs | Cost |
| --- | --- | --- | --- |
| Plan (free) | `/run-batch plan` | `run_checks.py plan --since last --model haiku`: the requests and their estimate | nothing sent, free |
| Run | `/run-batch run` | `submit --since last --model haiku --max-cost 3 --max-wait 90`, the morning routine's settings | about $1-2 of API credit, at most $3 |
| Full | `/run-batch full` | the same with every check whole (no `--since`) | at most $3; over that, nothing is sent (exit 4) |

Run and Full ask once more on the band before anything is sent. While a run goes, the band shows its minutes, an
**Output** button opens a pane with the output, and **Stop** (or `/run-batch stop`) ends the local process; a batch
already sent keeps running at Anthropic and `run_checks.py collect <id>` finishes it. The band stays away in folders
that hold no `TDS_Mod/Tools/batch/run_checks.py`, unless a clone sits at `~/Rogue-Waves` (`C:\Users\<you>\Rogue-Waves`), which it then uses; `/config` sets another clone's folder (`repoPath`).

Where it runs: on Windows the band starts `Run-Batch.ps1 -NoPull` beside `run_checks.py` (it needs Python 3.10+, Git,
a clone, and the key saved once with `Run-Batch.ps1 -SaveKey`; it never pulls, and it moves the report to
`Desktop\RogueWave\batch`; Stop ends that PowerShell and only its own descendants). Elsewhere, a cloud session included, it starts
`python3 run_checks.py` from the clone's root, installs `anthropic` and `pillow` once if missing, and reads the key from
`RW_BATCH_API_KEY` (the report lands in `docs/reports/`, uncommitted; a wait over 120 minutes ends with the batch id
to collect).

## Install

In a Claude Code terminal session:

```
/plugin install rw-batch-button --marketplace dimitriphan/Rogue-Waves
```

Answer `y` to add the marketplace, pick the user scope, and restart the Claude desktop app if it is open.

## Check it

From this folder: `claude plugin validate .` and `claude plugin test .` (three tests: the band in a clone, with Run
asking before it spends; no band elsewhere; `/run-batch run` only asks).
