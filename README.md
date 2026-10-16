# Rogue Waves

An expansion-sized fan mod for *TDS - Tower Defense Strategy* (Blockage). This repository is the home of the Claude
Project for Rogue Waves and holds only what that work needs: the vessel-making kit, the mod code, the tools, the
user's scope, and every lesson learned building six vessel mods in `ModProjects` (2026-10-01 to 2026-10-05). Its first
vessel is Argo 2.0 (`TDS_Mod/ModSource/Argo`), its second Yossele 0.1.0-dev (`TDS_Mod/ModSource/Yossele`); the other
vessels made in `ModProjects` (Sprigg, Mummer, Crackpot, Rustle, Broodie) are due for an overhaul and stay there until
it happens.

Start with `CLAUDE.md` (the map and reading order), then `TDS_Vessel_Creator/README.md`. Work lands on the branch
`claude/phase-1-6lfnar` (`docs/PROJECT_SETUP.md` §1).

## What is here

| Path | What |
| --- | --- |
| `TDS_Vessel_Creator/` | The kit: 24 chapters in `docs/` (game model, design rules, art, pipeline, code cookbook, journal, online, testing, pitfalls, checklists, tooling, minion vessels, enemies and modifiers, ModTool and packaging, the Rogue mode engine, maps and economy, the base vessels, character art, networking, UI and HUD, music, sound and text, new maps, the save, unlocks and loader, enemy bodies and bosses), the shared kit 1.6 (`kit/`), the Mummer template, the scaffold script, the Cecil scripts, `reference/` (API dump, network surface, base-vessel and art surveys, round logs) and `examples/` (notes, frozen code the chapters cite, and the compiled recipes of chapters 19-23 in `examples/recipes/`) |
| `TDS_Mod/` | The Unity mod project's repository copy: `ModSource/Shared` (kit 1.6, live copy), Argo 2.0, Yossele 0.1.0-dev, the mode part `RogueWavesMode`, the research mod `RogueLab`, the prototype `Riptide`, the editor code in `Assets/`, the tools (`Tools/cloud-check`, `Tools/pc`, `Tools/aseprite`) and one `Handoff/` folder per PC round (`TDS_Mod/README.md`) |
| `concept-tools/` | Generic Python for concept pages: pixel rasteriser, low-poly builder and renderer, PNG writer, mod-page builder, the sea chart generator |
| `docs/` | The living project pages (`PLAN.md`, `PACK.md`, `STYLE.md`, `EXPANSION_SCOPE.md`, `RESEARCH_GAPS.md`, `PROJECT_SETUP.md`), Yossele's build notes (`YOSSELE_BUILD_NOTES.md`), the user's scope (`scope/`), the mode's design pages (`mode/`), dated reports (`reports/`) and vessel concept pages (`concepts/`); `CLAUDE.md` maps each |
| `.claude/` | Path-scoped rule notes that load when the matching files are touched, and the session permissions |

## What stayed in `ModProjects`, and why

- `TDS_Mod/ModSource/<Vessel>`, `TDS_Mod/Assets/<Vessel>Mod` and their handoffs: the six vessels, overhauled before
  they join Rogue Waves.
- `tds-vessel-concepts/` (their concept pages and per-vessel sprite scripts) and `aseprite/roguewaves/` (the hand-drawn
  Sprigg and its idle loop; copy `sprigg_idle.py` over when the first hand-drawn Rogue Waves sprite needs an idle).
- The kit's `template/Mummer/` and `examples/` **are** kept: the template is the scaffold every vessel starts from
  (`tools/New-Vessel.ps1`), and the examples are the lessons, not the characters.

## Loose ends

- `TDS_Mod/Tools/pc/Get-Handoff.ps1` fetches with the PC's signed-in `gh`; its fallback, a read-only token
  (`C:\Users\Dimitri\.modprojects_token`), doesn't exist on the PC. Pack handoffs against the PC's files with
  `make_handoff.py --manifest` (`TDS_Mod/Tools/pc/README.md`).
- The game's `Assembly-CSharp.dll` and the PC lock `pc/QUEUE.md` live in the Claude Project's files, never in git.
- `TDS_Mod/Tools/cloud-check` needs its one-time `setup.sh` in each new container (its `README.md`).
- `TDS_Vessel_Creator/README.md` "Facts about this PC" says what is installed in the game's Mods folder; keep it current.
