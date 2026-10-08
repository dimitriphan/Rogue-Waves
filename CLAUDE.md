# Rogue Waves

An expansion-sized fan mod for *TDS - Tower Defense Strategy* (Blockage): new **vessels** (playable characters) and, once
researched, the other pieces an expansion has (`docs/EXPANSION_SCOPE.md`). This repository carries the kit and every
lesson learned building six vessel mods in `ModProjects`; the vessels themselves arrive after their overhaul.

**Where the truth lives, in order:** the user's newest words > `TDS_Vessel_Creator/` (the kit) > this file. This file
is only a map and a reading order. Rules live in the kit; don't copy them here or into `.claude/rules/`, link them.

## Map

| Path | What | Read first |
| --- | --- | --- |
| `TDS_Vessel_Creator/` | The vessel-making kit: every TDS rule, distilled from shipping Sprigg, Argo, Mummer and building Crackpot, Rustle, Broodie | `README.md`, then the doc the task needs (below) |
| `TDS_Mod/` | The vessel-free part of the Unity mod project. `ModSource/Shared` is the **live** shared kit (1.5), copied to `TDS_Vessel_Creator/kit/Shared`. `Assets/ModKit/Editor`: Unity build queue. `Tools/cloud-check`: compile and preview here. `Tools/pc`: PC rounds. `Tools/aseprite`: hand-edited frames. `ModSource/Riptide`: a tested roguelike-mode prototype, prior art for the mode | `TDS_Mod/README.md` |
| `TDS_Mod/ModSource/RogueLab` | Phase 1 research mod (never released): the PC round of `docs/15` §11 | its `README.md` |
| `concept-tools/` | Generic Python for concept pages: pixel rasteriser, low-poly builder and renderer, mod-page builder | the docstring at the top of each file |
| `docs/SCOPE_v0.txt` | The user's scope for the expansion (2026-10-08): mode, mechanics, four vessels, modifiers, a bug fix. The highest truth after the user's newest words | when a task touches its subject |
| `docs/PACK.md` | How the pack is built and shipped: one `RogueWavesMod` bundle, a prefab per part, no load order, kit 1.6 scope, versions | before packaging or kit work |
| `docs/STYLE.md` | The pack's style sheet: theme, palette, motifs, naming voice, each new vessel's shape and sprite reference | before drawing or naming anything for the pack |
| `docs/PROJECT_SETUP.md` | What to hand the coordinator: repository, project instructions, project files, first message, PC chores | when setting up or re-creating the Claude Project |
| `docs/PLAN.md` | The prompt plan: the coordinator's operating rules (Part A), phases and their opening prompts (B), state and open questions (C), prompt templates (D) | **every session**: Part C "Now" |
| `docs/EXPANSION_SCOPE.md` | Beyond vessels: enemies, modes, maps, bosses, music, journal, unlocks, packaging. What is known, what is not, what to research | before any non-vessel work |
| `docs/concepts/` | Vessel concept pages drafted from the user's sprite art and the scope (Yossele, Spool, Uloe), with the art in `art/` | before designing one of those vessels |

No vessel lives here yet. When one starts: `TDS_Mod/ModSource/<Name>` + `Assets/<Name>Mod` (scaffold with
`TDS_Vessel_Creator/tools/New-Vessel.ps1` or `TDS_Mod/Tools/cloud-check/new_vessel.py`), add a row here and in
`TDS_Mod/README.md`.

## Reading order by task

| Task | Read before starting |
| --- | --- |
| Any session on this project | `docs/PLAN.md` Part C "Now", then the phase's reads in Part B; nothing else until the task sends you there |
| Design a vessel, building, upgrade tree, unit | `docs/02_DESIGN_RULES.md`; units: `docs/12`; the engine's limits: `docs/01`, `docs/05` and `examples/Mummer_DESIGN.md` ("Changes from the concept") |
| Draw sprites, UI art, towers, effects, sounds | `docs/03_ART_AND_AUDIO.md` |
| Write or change mod code | `docs/09_PITFALLS.md`, then `docs/05_CODE_COOKBOOK.md`; online: `docs/07` |
| Enemies, elites, modifiers | `docs/13_ENEMIES_AND_MODIFIERS.md` |
| Anything an expansion has that a vessel mod doesn't | `docs/EXPANSION_SCOPE.md`, then `docs/14_MODTOOL_AND_PACKAGING.md` (what a mod can carry, how a pack ships), `docs/15` (the Rogue mode engine), `docs/13` §11 (how the game's code was read here) and `docs/11_TOOLING.md` |
| Check a mod without Unity | `TDS_Mod/Tools/cloud-check/README.md` |
| Send code to the PC, build, self-test, release | `TDS_Mod/Tools/pc/README.md`; self-tests: `docs/08` |
| Review or hand off | `docs/10_CHECKLISTS.md` |

(`docs/NN_*` = `TDS_Vessel_Creator/docs/`; `docs/EXPANSION_SCOPE.md` is this repository's own.) `.claude/rules/` adds
short, path-scoped notes that load only when you touch the matching files.

## This container vs the user's PC

The kit's pipeline (Unity 2022.3.41f1, TDS Mod Tools, UnityMCP, the game) runs only on the user's Windows PC. Here you
can design, draw, write code and run `TDS_Mod/Tools/cloud-check` (Roslyn compile against the game's API, sprite/model
previews). Never launch TDS or take desktop screenshots. Report what compiled vs what was verified in game.

## Keeping the library current

New knowledge goes where the next reader will look for it, once:

- A new engine fact or error → `docs/09_PITFALLS.md` (numbered) or the matching `docs/` chapter; a fact about a
  non-vessel system → the chapter `docs/EXPANSION_SCOPE.md` says it belongs to (new chapters take the next number, 14+).
- A vessel's numbers, rules and every change from its concept → that vessel's `DESIGN.md`; build state and what to
  verify in game → its `IMPLEMENTATION_NOTES.md`.
- A kit change → bump the version in `TDS_Vessel_Creator/kit/README.md` and the file headers, and keep
  `TDS_Mod/ModSource/Shared` and `TDS_Vessel_Creator/kit/Shared` equal.
- A new folder or a new vessel → one row in the tables above and in the folder's `README.md`.

Keep this file under 80 lines. Prefer a pointer to a paragraph; prefer a table row to a paragraph.
