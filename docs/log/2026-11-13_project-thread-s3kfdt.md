- **2026-11-13: four lessons from shanraisshan/claude-code-best-practice applied** (Dimitri: "2, 3, 4, 5, 6"): docs,
  settings and one Python script; nothing in the mod changed, nothing compiled or run on the PC.
  - **Rules script (2):** `TDS_Mod/Tools/cloud-check/check_rules.py` runs the mechanical half of `AGENTS.md` "Code
    Review Rules" for free (committed binaries, `.claude` JSON that does not parse, `Shared` against the kit's copy,
    "Now" over 4 KB, a dated entry in PLAN's Log section; on what the branch adds: `System.IO`/`System.Reflection` in
    mod code, the old Cardinal names, cited paths that do not exist, a kit change without its README, and the art
    pixel rules). `.github/workflows/rules.yml` runs it on every PR; ship-a-pr runs it before the PR opens.
  - **Log split (3):** the Log left `docs/PLAN.md` (230 KB to 48 KB): history to `docs/log/2026-10-08_archive.md`,
    new entries one file per PR (`docs/log/README.md`). PRs no longer conflict on the Log.
  - **Deny rules (4):** `.claude/settings.json` denies reading `api_assembly_csharp.txt` and `vessel_data_survey.txt`
    whole (Read and `cat`); grep still works.
  - **Skills (5):** the four project skills moved from `docs/skills/` to `.claude/skills/<name>/SKILL.md`, where Claude
    Code loads them from the repository (seen loading in this thread). scope-audit runs as its own sub-agent
    (`context: fork`); ship-a-pr and scope-audit fill in live state with `!` commands; each has a Gotchas section.
    The claude.ai copies are older and now duplicates: his to delete.
  - **His own sessions (6):** `docs/PROJECT_SETUP.md` "Your own Claude Code sessions".
  Built on PR #202's branch (its settings fix and wording).
