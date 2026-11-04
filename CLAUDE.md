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
| `TDS_Mod/` | The vessel-free part of the Unity mod project. `ModSource/Shared` is the **live** shared kit (1.7), copied to `TDS_Vessel_Creator/kit/Shared`. `Assets/ModKit/Editor`: Unity build queue. `Tools/cloud-check`: compile and preview here. `Tools/pc`: PC rounds. `Tools/aseprite`: hand-edited frames. `ModSource/Riptide`: a tested roguelike-mode prototype, prior art for the mode | `TDS_Mod/README.md` |
| `TDS_Mod/ModSource/Argo` + `Assets/ArgoMod` | Argo 2.0, designed from the scope and built; PC round 6 passed 139/0; 2.0.1 (online look fix) compiled only; ships only inside the pack | its `DESIGN.md` |
| `TDS_Mod/ModSource/RogueWavesMode` | The pack's mode part (0.1.0-dev, the pack version: the whole campaign from the main-menu button to the end screen, sea chart, weather, Pure Aura, VENDCINT, Directions, Cardinals, objectives, votes, retries, rejoin; its self-test passes in game, `docs/reports/BUILD_2026-11-03.md`), dev mod `RogueWavesModeMod` | its `README.md`, then `docs/mode/SEA_CHART.md` §7-9 or `docs/mode/REJOIN.md` |
| `TDS_Mod/ModSource/Yossele` | Yossele 0.2.0-dev, built from the 2026-10-20 sheet (`docs/scope/07`) with temporary art; PC round 9 passed 160/0, Dimitri's play build | `DESIGN.md`, `IMPLEMENTATION_NOTES.md`, `docs/YOSSELE_BUILD_NOTES.md` |
| `TDS_Mod/ModSource/Spool` + `Assets/SpoolMod` | Spool 0.1.0-dev, built from `docs/scope/08` (2026-10-26) with temporary art; PC round 1c passed 120/0 | `DESIGN.md`, `IMPLEMENTATION_NOTES.md` |
| `TDS_Mod/ModSource/RogueWavesFoes` + `Assets/RogueWavesFoesMod` | The pack's enemies part: Cursed, Berserk, Aging, Dazzling, dev mod `RogueWavesFoesMod` | its `README.md` |
| `TDS_Mod/ModSource/Uloe` + `Assets/UloeMod` | Uloe 0.1.0-dev, built from `docs/scope/10` with temporary art; its self-test passes in game | `DESIGN.md`, `IMPLEMENTATION_NOTES.md` |
| `TDS_Mod/ModSource/RogueLab` | Phase 1 research mod (never released): the PC round of `docs/15` §11 | its `README.md` |
| `concept-tools/` | Generic Python for concept pages: pixel rasteriser, low-poly builder and renderer, PNG writer, mod-page builder, the sea chart generator | the docstring at the top of each file |
| `docs/scope/` | The user's scope for the expansion (ex roguewave.txt, split 2026-10-09): one `.txt` per category (mode, mechanics, each vessel, Directions, Cardinals, modifiers, objectives); he sends back only the files he edits (`docs/SCOPE_v0.txt` only points here). `! NEEDS ... !` marks what only he can supply. The highest truth after the user's newest words | `00_Read_Me.txt`, then the file the task touches |
| `docs/PACK.md` | How the pack is built and shipped: one `RogueWavesMod` bundle, a prefab per part, no load order, kit 1.6 scope, versions | before packaging or kit work |
| `docs/STYLE.md` | The pack's style sheet: theme, palette, motifs, naming voice, each new vessel's shape and sprite reference | before drawing or naming anything for the pack |
| `docs/PROJECT_SETUP.md` | What to hand the coordinator: repository, project instructions, project files, first message, PC chores | when setting up or re-creating the Claude Project |
| `docs/PLAN.md` | The prompt plan: the coordinator's operating rules (Part A), phases and their opening prompts (B), state and open questions (C), prompt templates (D) | **every session**: Part C "Now" |
| `docs/EXPANSION_SCOPE.md` | Beyond vessels: enemies, modes, maps, bosses, music, journal, unlocks, packaging. What is known, what is not, what to research | before any non-vessel work |
| `docs/mode/` | Design pages of the mode's pieces: sea chart and sea weather (`SEA_CHART.md`), rejoining a running map (`REJOIN.md`), the main-menu start (`MAIN_MENU_START.md`), mechanic ideas Dimitri can pick from (`MECHANIC_IDEAS.md`) | before touching that piece |
| `docs/reports/` | Dated reports, frozen (a note on top says what changed since): quality checks, the full PC re-test, scope reviews, `SCOPE_GAPS.md` (every asset the scope names), `MECHANICS_CHECK.md` (the 2026-10-09 scope's mechanics: all go, the eight that needed the game proven) | its `README.md`; `SCOPE_GAPS.md` for what the first scope named (check `docs/scope/` first) |
| `docs/TEMP_ART.md` | Every temporary (placeholder) asset of the pack, where it lives (`ModSource/<Part>/Art/Temporary/`) and how final art replaces it | before drawing or replacing art |
| `docs/RESEARCH_GAPS.md` | Engine facts the scope needed researched: 17 of 18 answered (2026-10-09, C3 2026-10-15), what is left and the order to answer it | before Phase 5 or a vessel design gate |
| `docs/concepts/` | Vessel concept pages drafted from the user's sprite art and the scope (Yossele, Spool, Uloe), with the art in `art/`; five Argo diver takes to pick from (`argo_divers_2026-11-04/`) | before designing or redrawing one of those vessels |

When a vessel starts: `TDS_Mod/ModSource/<Name>` + `Assets/<Name>Mod` (scaffold with
`TDS_Vessel_Creator/tools/New-Vessel.ps1` or `TDS_Mod/Tools/cloud-check/new_vessel.py`), add a row here and in
`TDS_Mod/README.md`.

## Reading order by task

| Task | Read before starting |
| --- | --- |
| Any session on this project | `docs/PLAN.md` Part C "Now", then the phase's reads in Part B; nothing else until the task sends you there |
| Design a vessel, building, upgrade tree, unit | `docs/02_DESIGN_RULES.md`; units: `docs/12`; the engine's limits: `docs/01`, `docs/05` and `examples/Mummer_DESIGN.md` ("Changes from the concept") |
| Draw sprites, UI art, towers, effects, sounds | `docs/03_ART_AND_AUDIO.md`; a vessel's or an enemy's look: `docs/18_CHARACTER_ART.md` |
| Write or change mod code | `docs/09_PITFALLS.md`, then `docs/05_CODE_COOKBOOK.md`; online: `docs/07` |
| Enemies, elites, modifiers | `docs/13_ENEMIES_AND_MODIFIERS.md` |
| Directions or Cardinals that must work on every vessel; reusing a base vessel's assets | `docs/17_BASE_VESSELS.md` (§8, §11; assets §10) |
| Anything an expansion has that a vessel mod doesn't | `docs/EXPANSION_SCOPE.md`, then `docs/14_MODTOOL_AND_PACKAGING.md` (what a mod can carry, how a pack ships), `docs/15` (the Rogue mode engine), `docs/16` (inside a map: grid, map objects, nodes, wallet), `docs/13` §11 (how the game's code was read here) and `docs/11_TOOLING.md` |
| A mode's host brain, its UI, music, text, maps, the save, a boss; anything online beyond a vessel | `docs/19`-`24` (online, UI and HUD, music and text, new maps, save and loader, enemy bodies and bosses; read from the decompiled game, untested), sketches in `TDS_Vessel_Creator/examples/recipes/` |
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
  non-vessel system → the chapter `docs/EXPANSION_SCOPE.md` says it belongs to (new chapters take the next free number;
  open PRs may hold some, see `docs/PLAN.md` Part C "Now").
- A vessel's numbers, rules and every change from its concept → that vessel's `DESIGN.md`; build state and what to
  verify in game → its `IMPLEMENTATION_NOTES.md`.
- A kit change → bump the version in `TDS_Vessel_Creator/kit/README.md` and the file headers, and keep
  `TDS_Mod/ModSource/Shared` and `TDS_Vessel_Creator/kit/Shared` equal.
- A new folder or a new vessel → one row in the tables above and in the folder's `README.md`.

Keep this file under 80 lines. Prefer a pointer to a paragraph; prefer a table row to a paragraph.
