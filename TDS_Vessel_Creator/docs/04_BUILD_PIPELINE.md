# 04 · Build pipeline: from concept to shipped zip

## 1. Environment (once per machine)

| Need | Where (this PC) | Notes |
|---|---|---|
| TDS (Steam) | `D:\Steam\steamapps\common\TDS` | `TDS\DATA\Managed` holds the DLLs the build script compiles against |
| Unity 2022.3.41f1 | `C:\Program Files\Unity\Hub\Editor\2022.3.41f1` | the game's version; its bundled Roslyn (`Editor\Data\DotNetSdkRoslyn\csc.dll`) is the compile check |
| Mod project | `C:\Users\Dimitri\TDS_Mod` | URP 14; **TDS Mod Tools** imported (use the unitypackage in the game folder, `TDS\TDS Mod Tools.unitypackage`, which is newer than the one beside the Exporter README) |
| UnityMCP | MCP for Unity 9.3.2, stdio | lets an agent run editor menu items (`execute_menu_item`), read the console, refresh. Use stdio (port 8080 is taken by NVIDIA Broadcast); don't let it reinstall its server (pinned venv, see `docs/11`) |
| PowerShell 5.1 | built in | all scripts are Windows PowerShell 5.1 |

There is no dotnet SDK, ILSpy, git, Node or real Python on the PC; game code is read with Cecil (`docs/11`).

Project layout per vessel:

```
TDS_Mod/
  Assets/<Name>Mod/Editor/*.cs              generator + menu (namespace <Name>Build)
  Assets/<Name>Mod/{Art,Prefabs,Data}/      generated assets (rebuilt by Build All Assets)
  Assets/<Name>Mod/<Name>Mod_Default.prefab the mod prefab (code + library)
  ModSource/<Name>/Code/*.cs                runtime code, joined in name order
  ModSource/<Name>/Build-<Name>Mod.ps1      join + compile check -> Build/<Name>Mod_Code.cs
  ModSource/<Name>/Deploy-Wait.ps1          enable after export, optionally launch
  ModSource/<Name>/Tools/Decode-Dump.ps1    in-game captures -> PNG
  ModSource/<Name>/{DESIGN.md, IMPLEMENTATION_NOTES.md, Concept/, Previews/}
  ModSource/Shared/*.cs                     the shared kit, compiled into every mod
```

## 2. Scaffold

```powershell
powershell -ExecutionPolicy Bypass -File <resource>\tools\New-Vessel.ps1 -Name Bramble
```

Copies the template (Mummer 1.0.0) into `ModSource/Bramble` and `Assets/BrambleMod/Editor`, renaming
`Mummer/mummer/MUMMER` → `Bramble/bramble/BRAMBLE` and the template unit `Understudy` → `Brambleunit`, adds the shared
kit if the project has none, and writes `DESIGN.md` from the form. Right after scaffolding the vessel **is Mummer under
a new name**: it compiles, builds and passes Mummer's self-test. Then:

1. Make Unity import and compile the new folder: UnityMCP `execute_menu_item("Assets/Refresh")` (a `refresh_unity`
   call alone did not import a brand-new folder), then wait until `Library/ScriptAssemblies/Assembly-CSharp-Editor.dll`
   is newer than the scaffold and `Assets/<Name>Mod/Editor/*.cs.meta` exist. Check the console for errors.
2. Run the build script once: it must pass before you change anything.
3. Put the user's concept (if any) in `ModSource/<Name>/Concept/`.

## 3. Replace the template, in this order

| Step | Files | What changes |
|---|---|---|
| Design | `DESIGN.md` | everything in `docs/02`, before code |
| Data | `Code/02_Data.cs` | ids, texts, stats, units, the four buildables and their trees, sound volumes, warnings, journal labels |
| Art | `Editor/<Name>Character.cs`, `<Name>Models.cs`, `<Name>Audio.cs`, builder `VfxSpec` list and palette | drawings, models, sounds, effects (keep the formats in `docs/03`) |
| Content | `Code/04_Content.cs` | templates per buildable, which units exist, commands (ability, building buttons), hero AI (melee/ranged) |
| Logic | `Code/05_Server.cs`, `06_HeroBrain.cs`, `07_Building.cs`, `08_*.cs`, `09_*.cs` | the vessel's rules (keep the generic parts listed in `docs/05`) |
| Client | `Code/10_Client.cs`, `11_Commands.cs` | effect/sound handlers for your Vis events, button and picker classes |
| Journal | `Code/12_Journal.cs` | the spec (labels for your params, units) |
| Tests | `Code/99_SelfTest.cs` | keep the generic sections, replace the mechanic sections (`docs/08`) |

Delete what the new vessel doesn't use (no units? remove the puppet brain, its spawns and its test section).
Rename Mummer-specific class and file names once the mechanics are yours (`<Name>StageBrain` → whatever it now is).

## 4. Build, export, deploy

From a cloud thread, or for several vessels in one round: `TDS_Mod/Tools/pc/README.md`. A handoff carries the code
(`make_handoff.py`, fetched with `Get-Handoff.ps1`, checked and applied with `Apply-Handoff.ps1`), Tools > ModKit > Build
Queue builds and exports every listed vessel in one call, and `Run-SelfTests.ps1` runs and summarises the self-tests.
The steps below are what those tools do for one vessel.

```powershell
# compile check (add -Define <NAME>_SELFTEST,<NAME>_SHOTS for a test build)
powershell -ExecutionPolicy Bypass -Command "& 'C:\Users\Dimitri\TDS_Mod\ModSource\<Name>\Build-<Name>Mod.ps1'"
```

Use `-Command "& ..."` (with `-File`, a comma list passed to `-Define` arrives as one string). The script joins
`Code/*.cs` and `../Shared/*.cs` (renaming `ModKit` → `<Name>Kit`), hoists `using` lines, rejects System.IO/Reflection,
and compiles with C# 9 against **exactly** the game's mod references. Passing here means it compiles in game.

Then in Unity (UnityMCP `execute_menu_item`):

1. `Tools/<Name>/Build All Assets` (only after editor code changes or art changes).
2. `Tools/<Name>/Build Mod Prefab`: embeds `Build/<Name>Mod_Code.cs` + the library → `<Name>Mod_Default.prefab`
   (writes `Build/prefab_log.txt`: check it says OK with today's time).
3. `Tools/<Name>/Park Other Mods`: moves every other `Assets/*Mod` folder to `Assets/_Parked/Editor` (GUIDs kept).
   Verify with a directory listing that only your mod remains under Assets.
4. `Tools/<Name>/Export Mod`: writes `TDS\Mods\<Name>Mod`. **Over UnityMCP this call times out and is retried, so the
   export runs twice, ~40-60 s apart.** Wait until `<Name>Mod.info` has had the same timestamp for a minute.
5. `Tools/<Name>/Restore Parked Mods`. Verify every mod folder is back with its file count, and `Assets/_Parked` is gone.
6. `Deploy-Wait.ps1 [-Launch]`: flips `"_isEnabled": true` (every export resets it to false; fix the other mods' .info
   too if they were re-exported) and launches through `steam://rungameid/2392280`. It waits up to 240 s for a fresh
   export; with an older export it times out and then continues.

Never delete or clean up anything in the same step as a park/restore whose result you have not checked.

## 5. Release

A Rogue Waves part ships inside the pack instead: one version and one `RogueWaves-<version>.zip` (the repository's
`docs/PACK.md` §4-5) replace step 4 and §6.

1. Rebuild **without** `-Define`, check `Build/<Name>Mod_Code.cs` has no `#define` line.
2. Build Mod Prefab, Park, Export (wait for both passes), Restore, `Deploy-Wait.ps1` without `-Launch`.
3. Boot check: launch, wait for `[<Name>] ready` and `[<Name>] icons: <Name>'s own`, count `Exception` lines (0 from
   any mod), close the game.
4. Package `<Name>_TDS_Mod_v<x.y.z>.zip` in `Desktop\ClaudeProjects`: `README - <Name>.txt` (install, online note,
   what the vessel does, what's new, uninstall; `examples/Mummer_player_README.txt`) + `Mods/<Name>Mod/...` exactly as
   installed (the .info inside has `_isEnabled: true`).
5. Write `IMPLEMENTATION_NOTES.md` (layout, how it's built, status with test results) and add new lessons to this
   resource (`docs/09`).

## 6. Versioning

Three places, kept equal: `<Name>Data.Version` ("1.0.0"; logged as `[<Name>] loaded v...`), `ExportSettings.version`
in `<Name>ModPrefab.cs` ("1.0"; shown in the Mods menu), and the zip/README name. Every player in a lobby needs the
same version. Bump the patch for fixes, the minor for new content.

## 7. Many vessels in one project

Any number of vessel mods can coexist, in the project and in the game:

- **Names**: every id, class, icon, effect and sound carries the vessel name; the build script renames the kit's
  classes per mod (`<Name>Kit...`), so two mods never define the same type.
- **Export**: the Mummer template's Export refuses to run while another `Assets/*Mod` folder is live (park, export,
  restore); Argo's Export parks the other folders itself (`ArgoModPrefab.cs`); restore afterwards either way.
- **Boat**: `ModKitBoat` (kit 1.4) adds one spot per vessel. While the vessels fit on the 6 shipped spots they stand
  there; past that, every vessel stands in staggered rows across the deck, evenly spaced in spot order from just past
  one outer shipped spot to the other (2 rows to 11 vessels, 3 rows to 16, then they shrink a little), each row behind
  standing a little higher so its heads show over the row in front, and the click boxes are moved up onto the sprites
  and trimmed to their width, so each vessel can be clicked where it's drawn. Kits 1.0-1.3 put extras in the gaps and
  by 10 vessels some couldn't be clicked. Every mod runs the layout every frame from the shipped spots' first
  positions (kept on `Charselect/Spots_Home`), and names extra spots like the shipped ones so older kit copies leave
  them alone.
- **Journal**: `ModKitVesselTabs` gives every vessel page one tab per vessel in one order (shipped first, then mods by
  id); every mod runs it, and it's idempotent. Verified with 8 vessels; with many more, capture a journal page to check
  the tab row still fits.
- **Icons**: every mod adds its icons at boot (before any asset loading) and checks them 8 s later.
- **Shared kit**: `ModSource/Shared` is compiled into every mod in the project. When changing it, keep behaviour that
  other installed mods depend on (tab order, the boat layout), bump the kit version in the file header,
  and rebuild + re-export the mods you ship.

## 8. Hand-off notes for another agent

- Ask the user before launching TDS (they may be playing another game). Never take desktop screenshots.
- Close the game with `(Get-Process TDSgame).CloseMainWindow()`; process `TDS` doesn't exist.
- `Player.log` is overwritten on each launch: copy it to a scratch folder before the next run.
- The self-test changes the save's selected vessel to the tested one (`Player_Heroselect`). If a vessel mod is ever
  removed, select a different vessel first: the game re-selects the saved vessel at startup.
