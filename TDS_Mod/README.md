# TDS_Mod

The repository copy of the Unity mod project (`C:\Users\Dimitri\TDS_Mod`) for *TDS: Tower Defense Strategy*, made
with the TDS Vessel Creator (Unity 2022.3.41f1, TDS Mod Tools). Argo is the first vessel here (2.0); the others built in
`ModProjects` (Sprigg, Mummer, Crackpot, Rustle, Broodie) join after their overhaul.

| Path | What |
| --- | --- |
| `ModSource/Shared/` | The shared vessel-mod kit (1.6), compiled into every vessel and renamed `<Name>Kit` per mod: journal, boat, tower ranges, combat hits, self-test helpers, the pack's registry, Tides, counter, Directions and F-key fix. Keep it equal to `TDS_Vessel_Creator/kit/Shared`; rules in `TDS_Vessel_Creator/kit/README.md` |
| `ModSource/Pack/` | The release pack: `pack.txt` (the one version, the parts, the Mods menu text) and the player's `README.txt`; built with `Tools/pc/Build-Pack.ps1` and Tools > ModKit > Build Pack (`docs/PACK.md` §1). A handoff carries it with `--only Pack` |
| `Assets/ModKit/Editor/ModKitPipeline.cs` | Unity: Tools > ModKit > Build Queue, one Build All Assets, Build Mod Prefab, Export and Restore for every vessel in `handoff\queue.txt` |
| `Tools/cloud-check/` | Compile a vessel against the game's API here (Roslyn 3.8, no `System.Core`), compile its editor code, preview its sprites, models and sounds, read self-test map snapshots. One-time `setup.sh` per container (`README.md`) |
| `Tools/pc/` | A PC round: pack a handoff from git, fetch and apply it on the PC, build, self-test, release (`README.md`). `Get-Handoff.ps1` fetches from this repository |
| `Tools/aseprite/` | Hand-edit any generated frame in Aseprite and keep the edit across builds (`README.md`) |
| `Handoff/` | One folder per PC round (`<Part>_round<N>`): what was sent, its hashes and the round's notes (`Tools/pc/README.md`) |
| `ModSource/Argo/`, `Assets/ArgoMod/Editor` | Argo, copied from `ModProjects` at 1.4.0 (2026-10-08) and redesigned from the scope as 2.0, built and self-tested (PC round 6: 139 pass, 0 fail); 2.0.1 (2026-10-15, the decompile's online look fix) is compiled only; ships only inside the pack (`ModSource/Argo/DESIGN.md`; 1.4.0's in `DESIGN_1.4.0.md`) |
| `ModSource/Yossele/`, `Assets/YosseleMod/Editor` | Yossele 0.2.0-dev (2026-10-23), Release 1's only vessel, built from Dimitri's 2026-10-20 sheet (`docs/scope/07`) with temporary art generated from his reshade; PC round 9 passed 160/0 and it is his play build. 0.1.0-dev (2026-10-12) passed PC round 8 98/0 (`ModSource/Yossele/IMPLEMENTATION_NOTES.md`; defaults in `DESIGN.md`, 0.1's in `docs/YOSSELE_BUILD_NOTES.md`) |
| `ModSource/Spool/`, `Assets/SpoolMod/Editor` | Spool 0.1.0-dev (2026-10-26), built from `docs/scope/08` with temporary art generated from Dimitri's reshade; PC round 1c passed its self-test 120/0, not yet played by eye (`ModSource/Spool/IMPLEMENTATION_NOTES.md`; defaults in its `DESIGN.md`) |
| `ModSource/RogueWavesMode/`, `Assets/RogueWavesModeMod/Editor` | The pack's mode part, prefab `RogueWaves_Mode_Default`, dev mod `RogueWavesModeMod`: 0.3.0 holds the campaign's sea chart and sea weather (PC round 4, 38/0), rejoining a running map (PC round 6, 47/0 on 0.2.1; the online test is still to do) and the decompile's online fixes (0.3.0, compiled only) (`ModSource/RogueWavesMode/README.md`, `docs/mode/SEA_CHART.md` §7-9, `docs/mode/REJOIN.md`) |
| `ModSource/RogueLab/`, `Assets/RogueLabMod/Editor` | Phase 1 research mod, never released: tests the Rogue mode engine on the PC (`ModSource/RogueLab/README.md`) |
| `ModSource/Riptide/`, `Assets/RiptideMod/Editor` | Riptide 0.2.3 (2026-10-05), a roguelike mode prototype from the PC, never in `ModProjects`: random Tides, a host director, a Salvage artifact pick on every client, a Setup-screen switch; its PC probes passed 70 of 70 (0.2.1; 0.2.2-0.2.3 are untested tweaks). Prior art for the Rogue Waves mode (`TDS_Vessel_Creator/docs/15`); read its `IMPLEMENTATION_NOTES.md` first. Its research report (`2026-10-04_roguelike_mode_research.md`) lived in the ModProjects Claude Project's files. On the PC it now sits in `Prototype Mods\Riptide`, outside `Assets` (2026-10-08) |

Per vessel: `ModSource/<Name>/` (`DESIGN.md`, `IMPLEMENTATION_NOTES.md`, `Concept/`, `Code/`,
`Build-<Name>Mod.ps1`) and `Assets/<Name>Mod/Editor` (generates every sprite, model, sound and effect in Unity).
Scaffold with `TDS_Vessel_Creator/tools/New-Vessel.ps1` (PC) or `Tools/cloud-check/new_vessel.py` (here). Check before
pushing:

```bash
cd TDS_Mod/ModSource
python3 ../Tools/cloud-check/check_mod.py <Name>/Code Shared <Name> ""
python3 ../Tools/cloud-check/check_mod.py <Name>/Code Shared <Name> "<NAME>_SELFTEST,<NAME>_SHOTS"
../Tools/cloud-check/check_editor.sh ../Assets/<Name>Mod/Editor
```

Riptide carries no kit: pass an empty folder in place of `Shared`. RogueLab's release build stops on a deliberate
`#error` (research only): check it with `ROGUELAB_SELFTEST`.

A clean compile is not a test: self-test and play happen on the PC (`TDS_Vessel_Creator/docs/08_TESTING.md`).
