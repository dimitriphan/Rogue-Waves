# Rogue Waves

An expansion-sized fan mod for *TDS - Tower Defense Strategy* (Blockage). This repository is the home of the Claude
Project for Rogue Waves and holds only what that work needs: the vessel-making kit, the shared mod code, the tools, and
every lesson learned building six vessel mods in `ModProjects` (2026-10-01 to 2026-10-05). Its first vessel is Argo
2.0 (`TDS_Mod/ModSource/Argo`); the other vessels made so far (Sprigg, Mummer, Crackpot, Rustle, Broodie) are due for an
overhaul and stay in `ModProjects` until it happens.

Start with `CLAUDE.md` (the map and reading order), then `TDS_Vessel_Creator/README.md`.

## What is here

| Path | What | Taken from `ModProjects` |
| --- | --- | --- |
| `TDS_Vessel_Creator/` | The kit: 15 docs (game model, design rules, art, pipeline, code cookbook, journal, online, testing, pitfalls, checklists, tooling, minion vessels, enemies and modifiers, ModTool and packaging, the Rogue mode engine), the shared kit 1.6, the Mummer template, the scaffold script, the Cecil scripts, the API dump and base-vessel survey, and `examples/` (design and implementation notes, cited code) | as is |
| `TDS_Mod/` | The parts of the Unity mod project that belong to every vessel: `ModSource/Shared` (kit 1.6, live copy), `Assets/ModKit/Editor/ModKitPipeline.cs` (Unity build queue), `Tools/cloud-check` (compile and preview without Unity), `Tools/pc` (PC rounds), `Tools/aseprite` (hand-edit frames) | without the six vessels |
| `concept-tools/` | The generic Python used by concept pages: `sprites.py` (pixel rasteriser), `lowpoly.py` (port of the kit's mesh builder with a renderer), `pngout.py`, `build_mod_pages.py` (sheets drawn from a mod) | without the per-vessel scripts and pages |
| `docs/EXPANSION_SCOPE.md` | What an expansion needs beyond vessels, what the kit already knows about each piece, and what still has to be researched (Phase 1 answered rows 3, 6, 7, 9 and 10 in `TDS_Vessel_Creator/docs/15`) | new |
| `docs/` (the rest) | The user's scope (`SCOPE_v0.txt`), the prompt plan (`PLAN.md`), the Claude Project setup (`PROJECT_SETUP.md`) and vessel concept pages (`concepts/`); `CLAUDE.md` maps each | new |
| `.claude/rules/` | Short path-scoped notes that load when the matching files are touched | paths updated |

## What was left behind, and why

- `TDS_Mod/ModSource/<Vessel>`, `TDS_Mod/Assets/<Vessel>Mod`, `TDS_Mod/Handoff/`: the six vessels. They will be
  overhauled before they join Rogue Waves.
- `tds-vessel-concepts/`: the concept pages and their per-vessel sprite scripts. Same reason.
- `aseprite/roguewaves/`: the hand-drawn Sprigg and its idle loop. It is Sprigg art, so it waits with Sprigg; copy
  `aseprite/roguewaves/sprigg_idle.py` over when the first hand-drawn Rogue Waves sprite needs an idle.
- The kit's `template/Mummer/` and `examples/` **are** kept: the template is the scaffold every vessel starts from
  (`tools/New-Vessel.ps1`), and the examples are the lessons, not the characters.

## Loose ends to settle when the first vessel starts here

- `TDS_Mod/Tools/pc/Get-Handoff.ps1` defaults to this repository. The PC has no read-only token
  (`C:\Users\Dimitri\.modprojects_token`) yet; PC sessions fetch with the signed-in `gh` instead, so pack handoffs
  with `make_handoff.py --manifest`. Make the token only if a PC round has to run without `gh`.
- Done 2026-10-08: the PC lock `pc/QUEUE.md` and the game's `Assembly-CSharp.dll` are in the Rogue Waves Claude
  Project's files (the DLL is never committed).
- `TDS_Mod/Tools/cloud-check` needs its one-time `setup.sh` in each new container (`TDS_Mod/Tools/cloud-check/README.md`).
- `TDS_Vessel_Creator/README.md` "Facts about this PC" says what is installed in the game's Mods folder (nothing between
  PC rounds since 2026-10-08); update it if that changes.
