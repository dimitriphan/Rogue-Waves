# cloud-check: compile and preview vessel mods without Unity or the game

Every part of the pack (Argo 2.0, Yossele, Spool, Uloe, RogueWavesMode, RogueWavesFoes) and RogueLab is written and
checked here, on a Linux machine with no Windows, Unity or copy of TDS. These scripts are how such code is checked and its art
reviewed. They don't replace a build in Unity and a test in the game: they catch
compile errors early and let you look at the generated sprites, models and sounds.

## What it checks

| Script | Does | Stands in for |
|---|---|---|
| `check_mod.py <CodeDir> <SharedDir> <Prefix> "<DEFINES>"` | Joins `Code/*.cs` and the shared kit the way `Build-<Name>Mod.ps1` does (`ModKit` renamed to `<Prefix>Kit`, `using` lines hoisted, `#define`s first), rejects `System.IO`/`System.Reflection`, and compiles with Roslyn 3.8 (the compiler the game ships) against the game's reference list: no `System.dll`, no `System.Core`. Errors are mapped back to `file:line`; `-w` anywhere also prints warnings. | The game compiling the mod at load |
| `check_editor.sh <EditorDir>` | Compiles a vessel's `Assets/<Name>Mod/Editor` code against `UnityEditor`/`UnityEngine` and stubs of the two ModTool classes the builders use. | Unity compiling the editor scripts |
| `lite/run_preview.sh <EditorDir> <Prefix> <OutDir> [s][m][a]` | Runs the vessel's own sprite, model and audio generators on `UnityLite` (a small Mathf/Vector/Quaternion/Mesh/Texture2D stand-in). `s` writes every sheet `WriteSprites` names (`OutDir/sheets`, plus enlarged copies in `OutDir/preview`; a `foreach` over a table of string arrays, like Argo's six Naut sheets, is unrolled first, and any other sheet whose name isn't a plain string is skipped with a warning); `m` renders every model with a software rasteriser from the builder's preview camera angle (`preview/models_<Group>.png`, one PNG per model in `models/`; moving parts are merged in at rest, and the first one is also drawn turned 60° to check its pivot), and also writes each model's raw mesh to `meshes/<Name>.json` (body, moving parts relative to their pivots, palette; Unity coordinates; for `../artbench/mesh_glb.py`, which turns them into .glb for Blender; a part with no `Px` class, the mode part, gets only `meshes/`); `a` prints each sound's length, RMS and peak. | Tools > <Name> > Preview Sprites / Preview Models |
| `new_vessel.py <TDS_Vessel_Creator> <TDS_Mod> <Name>` | Port of `tools/New-Vessel.ps1`. | New-Vessel.ps1 |
| `rules_check.py [RogueWavesMode/Code] [--seeds N]` | Compiles the mode's rules files (first line `// RULES FILE`) whole, with `rules/Standins.cs` in place of the game, and runs with mono: the save fuzz `98_SaveFuzz.cs` (F1-F7), the rule checks `rules/RulesTest.cs` (R1-R16, the campaign's C1-C12, the votes' V1-V9) and the chart's parity with `concept-tools/sea_chart.py`; exits 1 on a failure or when a rules file names an engine type. Run from `TDS_Mod/ModSource`. `save_fuzz.py` is the old name and runs it. | The mode's self-test F1-F7, on the real save; nothing in game for R1-R16, C1-C12 and V1-V9 |
| `map_view.py save\|near\|spots ...` | Reads the self-test's map snapshot (`TEST MAP` lines, kit 1.5): saves it with a top-down PNG, prints the ground around a tile, or lists the tiles `ModKitTest.Spot` would try. | Seeing the test map before a PC round |
| `scope_upgrades.py <scope.txt> <02_Data.cs>` | Matches every `% N:{Name}` upgrade in a vessel's scope to the `U(...)` entry of the same name: level (scope N = tier N+1), cost, in-game line and Leads to; `V=1` adds the Numbers line and the parameters side by side. Used by `docs/reports/SCOPE_CODE_AUDIT_2026-11-05.md`. | Reading a vessel's scope against its code by hand |

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
- Play builds (`MODKIT_PLAY`, kit `ModKitPlay.cs` and a vessel's `98_Play.cs`) use more of `UnityEngine.InputSystem`
  than the stubs carry (since 2026-11-06 only `Keyboard.current[Key]` for the keys the mode reads, `Mouse.current` and
  their button and position controls, and `Gamepad.current.buttonSouth`, for the mode's chart view and MVP board): they may report CS0234 or CS1061 here (7 errors for Yossele, 2026-10-27) and
  compile only on the PC. Release and self-test builds don't include that code.
- The mode's two-player test build (`ROGUEWAVESMODE_TWOPC`, `93_TwoPlayers.cs`) drives Netcode's transport and the game's
  chat box, which the stubs don't carry: it reports errors here and compiles only on the PC. The mode's
  release, `ROGUEWAVESMODE_SELFTEST` and `ROGUEWAVESMODE_GAPS` builds compile at 0 errors (the TMPro and layout
  members they use were added to `stubs/ThirdParty.cs` on 2026-11-04).
- Validated against the unmodified template (`TDS_Vessel_Creator/template/Mummer`): 0 errors in release and with every debug define, and a seeded
  `HashSet` (missing `System.Core`) is reported as CS1069, as the game would.
