# Rogue Waves

Dimitri's expansion-sized fan mod for *TDS - Tower Defense Strategy* (Blockage): a roguelike campaign mode, four new
vessels (Yossele, Spool, Argo, Uloe), new foes, Directions, Cardinals and Trinkets, shipped as one pack. This file
holds only what the repository can't tell you, and points to the files that hold the rules.

**Truth, in order:** Dimitri's newest words > `docs/scope/` (what to build) and `TDS_Vessel_Creator/` (how the game
works) > this file. **Every session** reads `docs/PLAN.md` Part C "Now" (state), then the task's reads below.

## Check your work

- Once per container: `cd TDS_Mod/Tools/cloud-check && ./setup.sh ../../../TDS_Vessel_Creator`.
- Per changed part, from `TDS_Mod/ModSource`: `python3 ../Tools/cloud-check/check_mod.py <Part>/Code Shared <Part> ""`,
  again with its self-test defines, and `../Tools/cloud-check/check_editor.sh ../Assets/<Part>Mod/Editor`
  (`.claude/rules/mod-code.md`; limits and play builds: `TDS_Mod/Tools/cloud-check/README.md`).
- A clean compile is not a test. Report which it was, with the command and its output: "compiled here", or "self-test
  N/0 on the PC". Self-tests and PC rounds: `TDS_Mod/Tools/pc/README.md`, kit `docs/08`. Never launch TDS here. A
  docs-only change needs no compile, but every path, section and number it cites must exist.

## Skills and plugins

| When | Use |
| --- | --- |
| Before opening a code PR | `/code-review high` on the diff, fix what it confirms; `max` only when Dimitri asks |
| After a build or a big code change | `/simplify`, keeping every game number (`docs/reports/CODE_SIMPLIFICATION_2026-11-04.md`) |
| A change that reads what another player sends, or a save file | `/security-review`; otherwise skip it (no secrets, no server) |
| A failing self-test or PC-round bug | `docs/09_PITFALLS.md` by symptom first, then a debugging skill |
| A chart for a report | `dataviz` |

Skills that assume a test runner, worktrees, subagents or their own handoff file (superpowers' process skills,
`handoff`) don't fit: design goes through `docs/scope/` and `DESIGN.md`, state through Part C, subagents only when
Dimitri or `docs/PLAN.md` asks. `/run` can't start the game here; the PC does. A procedure repeated three times (a PC
round, a scope fold-in) is worth proposing as a project skill.

## Rules the code doesn't show

- Names: **Trinkets** (were "Vessel Cardinals") and **Cardinals** (were "Team Cardinals"). Save keys keep the old names.
- The repository presents only Rogue Waves: no mention of the mods from before the scope, except where the kit's
  template, dated reports, the PLAN Log, version histories and his scope notes keep them on purpose.
- Never commit the game's binaries, base-vessel game sprites or anything from `/mnt/project-files`; overwrite a file
  there only at Dimitri's word. Nothing touches his PC without his OK in the thread.
- Pull requests base on `claude/phase-1-6lfnar` and start with the session's attribution block. Claude merges them (his
  word, 2026-11-05; PLAN Part A "Merging"), merge commits, after merging the base. Each PR edits Part C "Now": keep the
  base's entries first and add yours last, three lines at most, the detail in the Log.

## Map

| Path | What | Read first |
| --- | --- | --- |
| `TDS_Vessel_Creator/` | The kit: every TDS rule learned so far | `README.md`, then the chapter the task needs |
| `TDS_Mod/ModSource/<Part>` + `Assets/<Part>Mod` | The pack's parts: `Argo`, `Yossele`, `Spool`, `Uloe`, `RogueWavesMode`, `RogueWavesFoes`; `Pack` builds them into one bundle; `Shared` is the live kit (keep equal to `TDS_Vessel_Creator/kit/Shared`) | its `README.md` or `DESIGN.md`, then `IMPLEMENTATION_NOTES.md` |
| `TDS_Mod/Tools/` | `cloud-check` (compile, previews), `pc` (PC rounds), `aseprite`, `batch` (text and scope checks on API credit) | its `README.md` |
| `docs/scope/` | Dimitri's scope, one `.txt` per category; `! NEEDS ... !` marks what only he can supply, `~value~` a default Claude picked | `00_Read_Me.txt`, then the file the task touches |
| `docs/PLAN.md` | Part A coordinator rules, B phases, C state, D prompt templates | Part C "Now" |
| `docs/PACK.md`, `docs/STYLE.md`, `docs/TEMP_ART.md` | Packaging and versions; look and naming; temporary art | before packaging; before drawing or naming |
| `docs/EXPANSION_SCOPE.md`, `docs/RESEARCH_GAPS.md` | Non-vessel systems and what is known about them; engine facts left to research | before non-vessel work |
| `docs/mode/`, `docs/concepts/`, `docs/reports/` | Mode design pages; concept pages and art; dated, frozen reports | the page the task names; reports' `README.md` |
| `concept-tools/` | Python for concept pages, sprites, low-poly models, the sea chart | the docstring of each file |

## Reading order by task

`.claude/rules/` adds short notes on its own when you touch the matching files.

| Task | Read before starting |
| --- | --- |
| Design a vessel, building, upgrade tree, unit | kit `docs/02`; units `docs/12`; the engine's limits `docs/01`, `docs/05`, `examples/Mummer_DESIGN.md` |
| Sprites, UI art, towers, effects, sounds | kit `docs/03`; a vessel's or a foe's look `docs/18`; `docs/STYLE.md` |
| Write or change mod code | kit `docs/09`, then `docs/05`; online `docs/07` |
| Enemies, elites, modifiers | kit `docs/13` |
| Directions or Trinkets for every vessel; base vessels' assets | kit `docs/17` §8, §10, §11 |
| Non-vessel systems; mode, UI, music, text, maps, save, bosses | `docs/EXPANSION_SCOPE.md`, kit `docs/14`-`16`, `docs/19`-`24`, `examples/recipes/` |
| Send code to the PC, build, self-test, release | `TDS_Mod/Tools/pc/README.md`, kit `docs/08` |
| Review or hand off | kit `docs/10` |

## Keeping the library current

New knowledge goes where the next reader looks, once: an engine fact or error to kit `docs/09` (next free number in
Part C) or its chapter; a vessel's numbers to its `DESIGN.md`, its build state to `IMPLEMENTATION_NOTES.md`; a kit
change bumps the version in `TDS_Vessel_Creator/kit/README.md` and the file headers; a new folder or part adds a row
above. Keep this file under 80 lines.
