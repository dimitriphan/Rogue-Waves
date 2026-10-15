# cloud-check: compile and preview vessel mods without Unity or the game

Crackpot and Rustle (in ModProjects) were written on a Linux machine with no Windows, Unity or copy of TDS; Argo 2.0,
RogueLab and RogueWavesMode were checked the same way here. These scripts are how such code is checked and its art
reviewed. They don't replace a build in Unity and a test in the game: they catch
compile errors early and let you look at the generated sprites, models and sounds.

## What it checks

| Script | Does | Stands in for |
|---|---|---|
| `check_mod.py <CodeDir> <SharedDir> <Prefix> "<DEFINES>"` | Joins `Code/*.cs` and the shared kit the way `Build-<Name>Mod.ps1` does (`ModKit` renamed to `<Prefix>Kit`, `using` lines hoisted, `#define`s first), rejects `System.IO`/`System.Reflection`, and compiles with Roslyn 3.8 (the compiler the game ships) against the game's reference list: no `System.dll`, no `System.Core`. Errors are mapped back to `file:line`; `-w` anywhere also prints warnings. | The game compiling the mod at load |
| `check_editor.sh <EditorDir>` | Compiles a vessel's `Assets/<Name>Mod/Editor` code against `UnityEditor`/`UnityEngine` and stubs of the two ModTool classes the builders use. | Unity compiling the editor scripts |
| `lite/run_preview.sh <EditorDir> <Prefix> <OutDir> [s][m][a]` | Runs the vessel's own sprite, model and audio generators on `UnityLite` (a small Mathf/Vector/Quaternion/Mesh/Texture2D stand-in). `s` writes every sheet `WriteSprites` writes (`OutDir/sheets`, plus enlarged copies in `OutDir/preview`); `m` renders every model with a software rasteriser from the builder's preview camera angle (`preview/models_<Group>.png`, one PNG per model in `models/`; moving parts are merged in at rest, and the first one is also drawn turned 60° to check its pivot); `a` prints each sound's length, RMS and peak. | Tools > <Name> > Preview Sprites / Preview Models |
| `new_vessel.py <TDS_Vessel_Creator> <TDS_Mod> <Name>` | Port of `tools/New-Vessel.ps1`. | New-Vessel.ps1 |
| `map_view.py save\|near\|spots ...` | Reads the self-test's map snapshot (`TEST MAP` lines, kit 1.5): saves it with a top-down PNG, prints the ground around a tile, or lists the tiles `ModKitTest.Spot` would try. | Seeing the test map before a PC round |

Debug builds: pass the defines like the PowerShell script, e.g. `"ARGO_SELFTEST,ARGO_SHOTS"`.

## Setup (once)

Needs `mono-devel` (`apt-get update` first in a fresh container), `python3`, `curl` and `unzip`, plus the TDS Vessel
Creator (`TDS_Vessel_Creator/` in this repository) (its `reference/api_assembly_csharp.txt` dump is turned into stubs of
the game's API; the stubs are not committed).

```bash
cd TDS_Mod/Tools/cloud-check
./setup.sh ../../../TDS_Vessel_Creator       # NuGet: Microsoft.Net.Compilers 3.8.0, UnityEngine.Modules 2021.3.33, Unity3D.SDK 2021.1.14.1
```

## Use

```bash
cd TDS_Mod/ModSource
python3 ../Tools/cloud-check/check_mod.py Argo/Code Shared Argo ""
python3 ../Tools/cloud-check/check_mod.py Argo/Code Shared Argo "ARGO_SELFTEST,ARGO_SHOTS"
../Tools/cloud-check/check_editor.sh ../Assets/ArgoMod/Editor
python3 ../Tools/cloud-check/check_mod.py ../../TDS_Vessel_Creator/examples/recipes none Recipe ""   # the recipes
cd ../Assets/ArgoMod/Editor && ../../../Tools/cloud-check/lite/run_preview.sh . Argo /tmp/argo sma
```

## Limits

- The stubs have the game's signatures, not its behaviour. A clean compile says the code uses the API correctly; it
  says nothing about what the game does with it. Every mechanic still needs the in-game self-test
  (`<NAME>_SELFTEST`) or a playtest.
- Reference versions differ slightly from the project's (Unity 2022.3.41f1): engine modules 2021.3.33, UnityEditor
  2021.1. APIs the vessels use exist in both; an API added after 2021 would show up as a false error.
- The preview renderer is flat-shaded and orthographic, close to the builder's preview camera but not the game's
  lighting or materials.
- Types from assemblies outside the game's reference list are stubbed loosely: `Blm_Player.Steamid` (Steamworks) is an
  `object` here, so code touching it compiles here and fails in game. Keep to the reference list in
  `Build-<Name>Mod.ps1`.
- `stubs/ThirdParty.cs` (Netcode, TMP, UniTask stand-ins) is hand-written: add a member when real code needs it
  (`ClientRpcParams.Send.TargetClientIds` and `NetworkManager.ServerClientId`, 2026-10-14), then rerun
  `build_stubs.sh <TDS_Vessel_Creator>`.
- Validated against the unmodified Mummer template: 0 errors in release and with every debug define, and a seeded
  `HashSet` (missing `System.Core`) is reported as CS1069, as the game would.
