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

Set the project's model to Fable 5.1; Part A's "Model per role" table says when a thread should drop to Opus 5.5.

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
