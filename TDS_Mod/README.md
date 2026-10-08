# TDS_Mod

The vessel-free part of the Unity mod project (`C:\Users\Dimitri\TDS_Mod`) for *TDS: Tower Defense Strategy*, made
with the TDS Vessel Creator (Unity 2022.3.41f1, TDS Mod Tools). Rogue Waves has no vessel folder yet; the ones built in
`ModProjects` (Sprigg, Argo, Mummer, Crackpot, Rustle, Broodie) join after their overhaul.

| Path | What |
| --- | --- |
| `ModSource/Shared/` | The shared vessel-mod kit (1.5), compiled into every vessel and renamed `<Name>Kit` per mod: journal, boat, tower ranges, combat hits, self-test helpers. Keep it equal to `TDS_Vessel_Creator/kit/Shared`; rules in `TDS_Vessel_Creator/kit/README.md` |
| `Assets/ModKit/Editor/ModKitPipeline.cs` | Unity: Tools > ModKit > Build Queue, one Build All Assets, Build Mod Prefab, Export and Restore for every vessel in `handoff\queue.txt` |
| `Tools/cloud-check/` | Compile a vessel against the game's API here (Roslyn 3.8, no `System.Core`), compile its editor code, preview its sprites, models and sounds, read self-test map snapshots. One-time `setup.sh` per container (`README.md`) |
| `Tools/pc/` | A PC round: pack a handoff from git, fetch and apply it on the PC, build, self-test, release (`README.md`). `Get-Handoff.ps1` fetches from this repository |
| `Tools/aseprite/` | Hand-edit any generated frame in Aseprite and keep the edit across builds (`README.md`) |
| `ModSource/RogueLab/`, `Assets/RogueLabMod/Editor` | Phase 1 research mod, never released: tests the Rogue mode engine on the PC (`ModSource/RogueLab/README.md`) |
| `ModSource/Riptide/`, `Assets/RiptideMod/Editor` | Riptide 0.2.3 (2026-10-05), a roguelike mode prototype from the PC, never in `ModProjects`: random Tides, a host director, a Salvage artifact pick on every client, a Setup-screen switch; its PC probes passed 70 of 70. Prior art for the Rogue Wave mode (`TDS_Vessel_Creator/docs/15`); read its `IMPLEMENTATION_NOTES.md` first. Its research report (`2026-10-04_roguelike_mode_research.md`) lived in the ModProjects Claude Project's files |

Per vessel, once one exists: `ModSource/<Name>/` (`DESIGN.md`, `IMPLEMENTATION_NOTES.md`, `Concept/`, `Code/`,
`Build-<Name>Mod.ps1`) and `Assets/<Name>Mod/Editor` (generates every sprite, model, sound and effect in Unity).
Scaffold with `TDS_Vessel_Creator/tools/New-Vessel.ps1` (PC) or `Tools/cloud-check/new_vessel.py` (here). Check before
pushing:

```bash
cd TDS_Mod/ModSource
python3 ../Tools/cloud-check/check_mod.py <Name>/Code Shared <Name> ""
python3 ../Tools/cloud-check/check_mod.py <Name>/Code Shared <Name> "<NAME>_SELFTEST,<NAME>_SHOTS,<NAME>_PROBE"
../Tools/cloud-check/check_editor.sh ../Assets/<Name>Mod/Editor
```

A clean compile is not a test: self-test and play happen on the PC (`TDS_Vessel_Creator/docs/08_TESTING.md`).
