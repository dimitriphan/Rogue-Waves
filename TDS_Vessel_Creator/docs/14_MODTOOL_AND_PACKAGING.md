# 14 · ModTool and packaging: what a mod can carry, and how an expansion ships

What the mod container itself allows: content types, scenes, mod folders, load order, the code sandbox, and how a pack
of several parts should be packaged. Written 2026-10-08 from a sourced research round (upstream ModTool code,
RoslynCSharp's shipped settings, Blockage's Patch 0.4 note) plus the game's API dump; nothing here was tried in the game
yet except the load order (§6) and Q4 (several `Default` prefabs in one bundle all activate: RogueLab round 2, `15` §15). Tags: **verified** = seen in a shipping mod, **read** = read from code, **untested** =
inferred or from a secondary source. The rest of the pipeline (export, park, deploy, release) is `docs/04`; the mod
prefab and the compiler's reference list, `docs/01` §1.

## 1. The container: ModTool, as TDS uses it

TDS Mod Tools is Blockage's build of **ModTool** (Hello Meow, open source, v2.0 of 2022-06, last commit 2025-02).
Everything below about ModTool was read from upstream master; Blockage's fork could differ, and §6 lists what to check
on the PC.

| Fact | Tag | Source |
|---|---|---|
| A mod is a folder with `<name>.info` (JSON: `_name`, `_author`, `_description`, `_version`, `_unityVersion`, `_platforms`, `_content`, `_isEnabled`, nothing else) and a per-platform folder (`Windows`) holding `<name>.assets` and, when scenes were exported, `<name>.scenes` | read | ModTool `ModInfo.cs`, `Mod.cs`; verified layout in every TDS export (`docs/01` §1) |
| Content flags: `ModTool.Shared.ModContent { Scenes = 1, Assets = 2, Code = 4 }`. The exporter starts from all flags and drops one when that category is empty; `ExportSettings.content` can also restrict it | read | `ModContent.cs`, `ExportStep.cs` |
| **Our template exports `content = ModContent.Assets` only** (`<Name>ModPrefab.cs`), so no vessel mod has ever asked for a scene: the missing `.scenes` file says nothing about what TDS allows | verified | `template/Mummer/Editor/MummerModPrefab.cs` |
| Audio, textures, materials, shaders, lightmaps have no flag of their own: they enter the bundle only as dependencies of a prefab or ScriptableObject (`Assets`) or of a scene (`Scenes`) | read | `ExportStep.cs` collects `t:prefab t:scriptableobject` and `t:scene` |
| Mods must be exported with the game's exact Unity version (2022.3.41f1); the exporter throws on a mismatch | read, matches `docs/04` §1 | `ExportStep.VerifyProject` |
| Unity can't deserialize fields of `[Serializable]` types that were loaded at run time, so a mod's own ScriptableObject or serializable classes can't be inspector fields; only the game's own types deserialize. This is why the kit builds its data in code (`04_Content.cs`) and stores extra data as JSON strings | read; the kit's practice is verified | ModTool README "Limitations"; `docs/05` §17 |
| Mods inherit the game's project settings: no new tags, layers or input axes | read | ModTool README |
| Mods are discovered by `*.info` under `<working dir>/Mods`, loaded asynchronously; a mod whose `_platforms` lacks the running platform fails with "Platform not supported" | read | `ModManager.cs` |
| **No dependencies, no load order, no priority** anywhere in ModTool. The only inter-mod relation is a *conflict*: two mods sharing an assembly name or a scene name refuse to load together | read | `Mod.UpdateConflicts`, `ModManager.AddMod` |
| TDS: `Bla_Mods` holds `List<ModTool.Mod> Mods` / `Loadedmods`, a `Toggle(mod)` and an `Autoloadscenes` list of strings; `Activateprefabs(prefabCheck)` instantiates every prefab whose name contains the check (the game passes `"Mainmenu"`, `"Loading"`, `"World"`, `"Gameserver"`, `"Gameclient"`; a mod's `"Default"` prefabs activate when the mod loads) | read | `reference/api_assembly_csharp.txt` (`Bla_Mods`) |
| TDS: the Mods menu (`Bla_Ui_Mods`) shows title, author, description, version, image, state and a toggle per mod, plus Refresh; ModTool's example `ModMenu`/`ModItem` is unused (`23` §4.3); nothing in the API dump names an order | read | API dump, decompile |

## 2. Scenes, so maps

ModTool in general treats scenes as first-class content: `Mod.scenes` is a list of `ModScene`, each loaded
**additively** with `SceneManager.LoadSceneAsync(name, Additive)` and only after its mod has finished loading, and
**never automatically**: host or mod code must call `ModScene.Load()`. Lightmaps travel inside the `.scenes` bundle.
(read: `ModScene.cs`, `Mod.cs`.)

What this meant for a new map in TDS (superseded: §6 and `15` §10 answer it from the code):

- A mod can probably ship a scene if the exporter is asked to (`ExportSettings.content` including `Scenes`, and the
  fork's `ModToolSettings.supportedContent` still allowing it).
- Loading it is the hard part: TDS builds its maps with `Game_Grid.Generatemap` from a map prefab and
  `Mapobject`/`Mapentryobject` data (`16` §1), not by loading a scene per map. `Bla_Mods.Autoloadscenes` is
  never read, so the game never loads a mod's scenes (§6). The likely route to a
  new map is therefore a **map prefab plus map data registered the way the game registers its own**, not a scene.
- Whichever route, a mod scene layered on the running game's scenes would have to be unloaded and reloaded by the
  host's own flow, which no mod code reaches today.

Scope row 4 (maps) stays **high risk**, but the route changed (decompile, 2026-10-14): the custom-map slot
(`Map` prefabs with `Mods_Mapsettings`, picked up by `Custom_Map_Overworld`, §6) is per machine, unrecorded, edits
stock data and hung in RogueLab (`09` #78). Pack maps register like the mode: their own `Mapentryobject`, group,
`Mapobject` and `Gameplan` on every machine (`22` §3). Not yet tried in game.

## 3. The code sandbox: RoslynCSharp

Mod C# is one string compiled at load by RoslynCSharp (Trivial Interactive; the 1.x line with Roslyn 3.8 / C# 9 is
now legacy, superseded by their 2.x). Its security layer denies at four granularities (assembly, namespace, type,
member) plus a global P/Invoke switch, and a failed check reports every illegal usage. The asset's **shipped default**
deny list is: namespaces `System.IO.*`, `System.Reflection.*`; type `System.AppDomain`; member
`UnityEngine.Application.Quit`; assemblies `UnityEditor.dll`, `Mono.Cecil.dll`; P/Invoke off. TDS's observed blocks
(`docs/09` §A: System.IO, System.Reflection) match these defaults exactly, so expect the rest of the list to be
blocked too (untested: the deny lists of TDS's own `RoslynCSharpSettings` in `resources.assets` were not dumped; only
its reference list was read, the one in `Build-<Name>Mod.ps1`). The check is a pre-load filter, not a sandbox:
anything it lets through runs with the game's full rights. (read, from the asset's settings file as shipped in public
projects; the store page could not be fetched.)

Known from the kit and unchanged: the fixed reference list in `Build-<Name>Mod.ps1` (no `System.Core`, no
`JsonUtility`), compile errors surface only in `Player.log` (`docs/09` §A), one string per mod.

## 4. Distribution and the game's side

- Mod support arrived in **Patch 0.4 (2024-11-23)**; Blockage switched the build from IL2CPP to **Mono** to make it
  possible (slower boot, "easier to snoop through the game files"). Mods are enabled under Main Menu > Extras > Mods.
  (read: Steam news via mirrors; exact wording unverified.)
- **No Steam Workshop** was found for app 2392280, in the store, in the API dump (Steamworks is used for lobbies
  only: `Steamworks.Data.Lobby`) or in community posts, where a player asked for one. Distribution is a zip the player
  unpacks into `TDS\Mods` (`docs/04` §5). (read + absence of evidence.)
- No community mod of any kind (map, enemy, mode, vessel) was found outside this project's six vessels, so there is
  no prior art for anything beyond vessels. (untested: Discord and the Steam hub were unreachable.)

## 5. Packaging an expansion (synthesis, not a source)

Given §1-§4, the choices for a pack of several vessels plus a mode:

| Choice | For | Against |
|---|---|---|
| **One mod folder** (`RogueWavesMod`): one bundle, one `Default` prefab per part or one that boots all parts | One toggle, one version, no cross-part order problem (parts still don't share types or statics: each prefab's code compiles into its own assembly, `15` §10) | All or nothing for the player; one bundle to rebuild for any change; the park/export dance (`docs/04` §7) becomes moot but the bundle grows |
| **Several mod folders** (one per vessel + one for the mode) | Players enable parts independently; each part keeps its own release cycle, as today | ModTool can't express "needs the mode part"; order is undefined; each part compiles its own copy of shared code (today's `<Name>Kit` rename), so parts can't share types or static state and must find each other through the game's own objects at run time; same-named assemblies or scenes would block loading |

Rules that hold either way:

1. Shared code is copied into each compiled string (the kit's `Shared` folder renamed per mod, `kit/README.md`);
   keep the copies byte-identical and the kit version in every part equal.
2. One version string for the whole pack, in every `.info` and every `<Name>Data.Version` (`docs/04` §6); every
   player in a lobby needs the same set and version (`docs/07`).
3. A part that needs another detects it at run time (a known prefab, component or registered id the other part
   creates) and degrades gracefully with one log line; never assume order.
4. Never give two parts the same assembly or scene name.
5. Player install stays the zip into `TDS\Mods` plus enable in the Mods menu; one README for the pack.

Rogue Waves' choice (2026-10-08, the repository's `docs/PACK.md`): one bundle with one `Default` prefab per part,
since each prefab is its own assembly anyway (`15` §10). Parts find each other through the game's own objects (the pack
registry, `kit/README.md`).

## 6. Open questions for a PC session

1. In the TDS Mod Tools package: `ModToolSettings.supportedContent` (is `Scenes` still allowed?), and whether an export
   with `ExportSettings.content = Assets | Scenes` writes a `Windows\<name>.scenes` file and the game loads the mod.
2. `Bla_Mods`: what `Autoloadscenes` does, the order mods load in (list order, alphabetical, `.info`-driven), whether
   all enabled mods load at boot, and what `Toggle` writes. Read with the Cecil scripts (`docs/11`) or the decompiler
   (`docs/13` §11).
3. TDS's `RoslynCSharpSettings` instance in `TDS\DATA\resources.assets`: the full deny lists and P/Invoke, against the
   defaults in §3 (its reference list is already in `Build-<Name>Mod.ps1`).
4. Whether two mods with a `Default` prefab of the same name, or one mod with several `Default` prefabs, all activate
   (decides the "one folder, many parts" layout).
5. Blockage's Discord `#mods` channel: any documented limits or a Workshop plan.

**Answered from the code and the Mod Tools package (2026-10-08, `15` §10):** Q1 partly: `supportedContent` is 7
(assets, scenes, code), but the game never loads a mod's scenes (`Autoloadscenes` is never read); maps ship as `Map`
prefabs with `Mods_Mapsettings`, picked up by `Custom_Map_Overworld` (not the pack's route, see above). Q2: every
enabled mod loads at boot in ModTool's discovery order, never sorted (not alphabetical in a 2026-10-08 `Player.log`);
`Toggle` loads or unloads at once, but unloading undoes nothing of the mod's code, registrations or hooks, so players
restart after enabling or disabling the pack (`23` §4.3).
Q4: every prefab matching a keyword activates, in every mod, each compiled into its own assembly (**verified** for
several `Default` prefabs in one bundle: RogueLab round 2, `15` §15). Still on the PC: Q1's
custom-map check, Q3, and Q5 (not a PC task).

## 7. Sources

ModTool: github.com/Hello-Meow/ModTool (README, `ModTool.Shared/ModContent.cs`, `ModInfo.cs`, `ModToolSettings.cs`,
`ModTool/Mod.cs`, `ModScene.cs`, `ModManager.cs`, `ModTool.Editor.Exporting/ExportStep.cs`), master as of 2026-10-08.
RoslynCSharp: Asset Store id 142753 (via snippets) and the asset's `RoslynCSharpSettings.asset` as shipped in public
GitHub projects. TDS: Steam news "Patch 0.4 Mod Support" (2024-11-23, via mirrors), the game's API dump
(`reference/api_assembly_csharp.txt`). Unity: issue tracker on deserializing runtime-loaded `[Serializable]` types.
