# Setting up the Rogue Waves Claude Project

What to hand the coordinator so its first session starts at Phase 1 with nothing missing. Five things: the
repository, the project instructions, the project files, the first message, and two PC-side chores.

## 1. The repository

Connect `dimitriphan/Rogue-Waves`. All work so far is on branch `claude/rogue-waves-mod-setup-ngfuv3`; the remote has
no `main` yet. Before connecting, make that branch the default (GitHub: Settings > General > Default branch) or
merge it into a new `main`, so the project's sessions start from it. Do not connect `ModProjects`: the coordinator
needs it only once, when Argo's phase copies the shipped Argo in, and the plan says so at that step.

## 2. Project instructions (paste as the project's instructions)

> You coordinate the Rogue Waves expansion mod for *TDS - Tower Defense Strategy*. Open `CLAUDE.md` first; it is the
> map. Then read `docs/PLAN.md` Part A and follow it as your operating rules, and Part C "Now" for where the work
> stands. Read nothing else until the current phase's reads in Part B send you there. Rules about the game live in
> `TDS_Vessel_Creator/`; the user's scope is `docs/SCOPE_v0.txt`. Write findings to files, keep chat replies under
> 150 words, ask questions only at gates, and update Part C before you end a session.

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

> Start Phase 1 from `docs/PLAN.md` Part B. The game DLL and the TDS Mod Tools package are in the project files.

If the DLL is not uploaded yet, send instead: "Run `docs/14` §6 question 1 from the TDS Mod Tools package in the
project files, then stop." That one needs only the unitypackage.

## 5. PC-side chores (before the first PC round, after Phase 2)

- Regenerate the read-only GitHub token for `dimitriphan/Rogue-Waves` and save it as `C:\Users\Dimitri\.modprojects_token`
  (`TDS_Mod/Tools/pc/README.md`, one-time setup).
- Nothing else changes on the PC: `C:\Users\Dimitri\TDS_Mod` stays the Unity project, and handoffs arrive the same way.

## What the coordinator will do on its own

Run `TDS_Mod/Tools/cloud-check/setup.sh` once per container when it first needs a compile check, write kit chapter
`docs/15` from Phase 1, collect questions for the Phase 1 gate, and keep Part C current.
