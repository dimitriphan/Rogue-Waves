# TDS Vessel Creator

Everything needed to add new **vessels** (playable characters) to *TDS - Tower Defense Strategy* as mods that look,
play and read like the five vessels Blockage shipped, show up in the journal, and work online. It is distilled from
building three vessels end to end (Sprigg, Argo, Mummer; 2026-10-01/02), including every mistake made on the way.

Written for an AI agent (Claude or another) working on the user's Windows PC with the TDS mod project, but a human can
follow it too. **Read this file fully, then the docs in order.** Don't skip `docs/09_PITFALLS.md`.

## The non-negotiables

1. **Study before designing.** Every decision copies how a shipped vessel does the same thing
   (`reference/VESSEL_SURVEY.md`, `docs/01_GAME_MODEL.md`). If no shipped vessel does it, find the engine feature that
   does (`docs/11_TOOLING.md`: Cecil inspection) before inventing one.
2. **Start from the template** (`tools/New-Vessel.ps1`), never from a blank file. It is a working, tested vessel.
3. **Fill in DESIGN.md first** and hold it to `docs/02_DESIGN_RULES.md`: name ▪ title ▪ difficulty, a special rule shown
   as a tooltip, exactly 4 buildables, curated upgrade trees (tiers 2-5, one capstone), stats inside the shipped bands.
4. **Art matches the game, not a generic style.** Sprites follow the conventions all five base vessels share, using
   whichever base vessel is closest to the concept as the reference (Smacky, Sno Sno, Boutman, Fie or Buster). Towers
   are **always low-poly and sleek** like Sno Sno's and Buster's, **never voxel**: voxels only look good handmade, and
   generated voxel towers looked messy. Sounds are as quiet as the game's (`docs/03_ART_AND_AUDIO.md`).
5. **Every id carries the vessel name** (`Hero_<Name>`, `Tower_<Name>x`, `Mob_<Name>x`, `Debuff_<Name>x`, `<Name>_Sound`).
   The game keeps one global name table; a collision silently breaks another mod.
6. **Server decides, engine syncs.** Game state changes only on the host, through the engine's own networked channels
   (spawns, stats, skins, upgrades, commands, debuffs). Clients only draw (`docs/07_ONLINE.md`).
7. **Every mechanic is visible on every screen**, and its visual is honest (an area effect looks exactly as big as it is).
8. **The journal is built by the shared kit** (`kit/Shared/ModKitJournal.cs`) from the vessel's data, with a page per
   vessel, building, upgrade and unit, and one tab per vessel on every vessel page (`docs/06_JOURNAL.md`).
9. **Test in the game with the in-mod self-test** (`docs/08_TESTING.md`); look at the in-game captures. The game
   ignores injected input, and desktop screenshots are forbidden (they once captured another game the user was playing).
10. **Ask the user before launching TDS** unless they have already said yes for the session. Close it with
    `CloseMainWindow()` on process `TDSgame`.
11. **Ship the release build** (no `-Define`), packaged as `<Name>_TDS_Mod_v<x.y.z>.zip` with a player README (a
    Rogue Waves vessel ships instead as a prefab inside the one pack bundle, `docs/04` §5 and the repository's
    `docs/PACK.md`), and
    record what you learned (`docs/10_CHECKLISTS.md`).

## Quick start

```powershell
# 0. once per machine: docs/04_BUILD_PIPELINE.md "Environment" (Unity 2022.3.41f1 project with the TDS mod tools, UnityMCP)
# 1. scaffold (renames the reference vessel Mummer into <Name>, with no id collisions)
powershell -ExecutionPolicy Bypass -File tools\New-Vessel.ps1 -Name Bramble
# 2. design: fill in ModSource\Bramble\DESIGN.md
# 3. code + art: replace Mummer's mechanics and drawings module by module (docs/05, docs/03)
powershell -ExecutionPolicy Bypass -Command "& 'C:\Users\Dimitri\TDS_Mod\ModSource\Bramble\Build-BrambleMod.ps1' -Define BRAMBLE_SELFTEST,BRAMBLE_SHOTS"
# 4. Unity: Tools > Bramble > Build All Assets, Build Mod Prefab, Park Other Mods, Export Mod, (wait) Restore Parked Mods
#    (or Tools > ModKit > Build Queue for several vessels in one call: TDS_Mod/Tools/pc/README.md)
# 5. Deploy-Wait.ps1 -Launch (or Run-SelfTests.ps1), read Player.log, decode captures, fix, repeat
# 6. release: rebuild without -Define, export, Deploy-Wait.ps1 (no -Launch), zip, notes
```

## Map

| Path | What |
|---|---|
| `docs/01_GAME_MODEL.md` | How TDS represents vessels, entities, stats, upgrades, commands, assets and networking |
| `docs/02_DESIGN_RULES.md` | The quality bar: identity, kit structure, numbers, upgrade trees, text, balance process |
| `docs/03_ART_AND_AUDIO.md` | Sprite, UI, tower, effect and sound specs, and the house style |
| `docs/04_BUILD_PIPELINE.md` | Environment, scaffold, build, export, deploy, release, versioning, many vessels |
| `docs/05_CODE_COOKBOOK.md` | Module by module guide to the template, with recipes for every engine hook used |
| `docs/06_JOURNAL.md` | The shared journal kit: what it builds and how to feed it |
| `docs/07_ONLINE.md` | Multiplayer rules and checklist |
| `docs/08_TESTING.md` | The self-test harness, in-game captures, reading the log |
| `docs/09_PITFALLS.md` | Every error met so far: symptom, cause, fix |
| `docs/10_CHECKLISTS.md` | Design, art, code, release and hand-off checklists |
| `docs/11_TOOLING.md` | Reading the game's code (Cecil scripts, API dump), UnityMCP, PowerShell gotchas |
| `docs/12_MINION_VESSELS.md` | Vessels built around units (like Fie, Boutman, Buster): spawners, respawn, upgrades reaching units, unit behaviour, player orders, rules, tests |
| `docs/13_ENEMIES_AND_MODIFIERS.md` | Enemies and elites: the shipped roster, Tide tables and enemy cores, the six enemy modifiers, how a mod adds a new enemy or modifier, open questions for the PC |
| `docs/14_MODTOOL_AND_PACKAGING.md` | The mod container: ModTool content types and scenes, the `.info`, no load order or dependencies, the RoslynCSharp sandbox, no Workshop, how a pack of several parts should ship, PC checks that decide maps |
| `docs/15_ROGUE_MODE_ENGINE.md` | The Rogue Waves mode's engine: modes and chaining maps (proven by RogueLab's chained tour of every base map), what survives a scene change, Merchut and bounties, Cardinals (artifacts), client choices, the save, the F-key bug, mod loading; go / fallback / no per scope feature, PC list |
| `docs/16_MAPS_AND_ECONOMY.md` | Inside a map: the grid, placement and enemy paths, the base game's map-special objects (flood, clock, nests, dens, gates), aura nodes and mining, the wallet and colour conversion, game speed, lighting; what a run can change mid-map |
| `docs/17_BASE_VESSELS.md` | The five base vessels in code (Smacky, Sno Sno, Boutman, Fie, Buster): the shared stat, upgrade, respawn and ownership rules, each vessel's buildings and units (what reads stats live), quirks and bugs, what each run upgrade needs per vessel, the stock Cardinals on any vessel, assets to borrow, integration rules for Directions, Cardinals and new vessels |
| `docs/18_CHARACTER_ART.md` | Vessel and enemy art measured on the game's own sheets: how sprites are drawn (facings, frame rate, pivots, elite overlays), sheet formats by kind and tier, the shared palette, the crystal foe family, what separates a vessel from a foe, must/must-not rules for new designs |
| `docs/19_NETWORKING.md` | The engine under online play, read from the decompiled game: transport and lobby, every RPC and synced field (`reference/network_surface.md`), the map load and its 120 s kick, the effect throttle, late join, drops and identity (a restart changes the `Sessionid`), rules for a mode's host brain, the online-evening list |
| `docs/20_UI_AND_HUD.md` | Menus, the choice dialog and other prompts, the HUD and what a mod can feed it without building UI, building new UI, input and pad focus, recipes for the in-map vote, shop and run line |
| `docs/21_MUSIC_SOUND_TEXT.md` | Soundtracks and music stages, the sound table (never takes a mod key; an unknown key throws), playing stock sounds, the pack's own music, the localization table and how a pack translates |
| `docs/22_NEW_MAPS.md` | A new map: the chain from a chart node to the `Mapdata` prefab, its children (grid, `Entities`, `Object`, colliders, visuals), why not the custom-map slot, the pack's own registration, online |
| `docs/23_SAVE_UNLOCKS_LOADER.md` | The save (files, slots, what a mod may write, removal, a schema version), unlocks and what the boat announces, the mod loader's boot order, keywords, compile errors and Toggle, records and achievements |
| `docs/24_ENEMY_BODIES_AND_BOSSES.md` | Enemy sprites and animation from the code, flying (Halberd), movement per enemy kind, the Tide around the contents, hooks for a mode, a pack boss plan |
| `kit/Shared/` | The shared kit every vessel compiles (kit 1.6): journal, boat, tower ranges, combat hits, self-test helpers, and the pack's registry, Tides, counter, Directions and F-key fix (`kit/README.md`) |
| `template/Mummer/` | The reference vessel (Mummer 1.0.0): runtime code, editor generator, build scripts |
| `template/DESIGN_TEMPLATE.md` | The design form (copied into each new vessel) |
| `tools/New-Vessel.ps1` | Scaffolds a new vessel from the template |
| `tools/cecil/` | Scripts that read the game's compiled code (IL, callers, string literals, API dump) |
| `reference/` | Base-vessel survey (conventions + full data dump), the game API dump, the base game's Addressables asset list (`addressables_default_group.txt`), RogueLab's round logs of the stock modes, Tide tables, artifacts, elites and enemies (`roguelab_round1.txt`-`roguelab_round3.txt`) and its base-vessel checks (`roguelab_round7.txt`, `docs/17`), every RPC and synced field with its checks (`network_surface.md`), `maps/`: where the self-test map's ground snapshots go (none committed yet), and the art survey of the base game's sprites (`art_survey_2026-10-15.txt`) with its palettes (`palettes/*.gpl`, `docs/18` §9) |
| `examples/` | The design and implementation notes of the three vessels built first (Sprigg, Argo 1.x, Mummer); `examples/code/` holds frozen Argo 1.x and Sprigg 1.x modules the docs cite (damage hooks, tower firing, turret aim/recoil/spin, a generator with moving parts, unit orders and a crew picker, ranged vessel, leftovers, data survey, base-vessel art dump); `examples/recipes/` holds compiled, untested sketches for chapters 19-23 (messages to one machine, late join, the dialog and a timed vote, music, translations, the save) |

## Definition of done (every vessel)

- `DESIGN.md` complete, every change from the user's concept listed with its reason.
- Self-test: 0 failures, including the generic checks (vessel select, boat spot, build, upgrades, Tides, journal pages,
  tabs on every vessel page). Captures reviewed: boat, buildings, every effect, journal pages.
- Release build boots alongside every other installed vessel mod: `[<Name>] ready`, own icons, **0 exceptions**.
- Online checklist (`docs/07_ONLINE.md`) passed; ask the user to try it with a friend if it adds new kinds of sync.
- Zip and player README (a pack vessel: its prefab in the pack, `docs/04` §5), `IMPLEMENTATION_NOTES.md`, and any new lesson added to `docs/09_PITFALLS.md`.

## Facts about this PC (adjust for another one)

| | |
|---|---|
| Game | `D:\Steam\steamapps\common\TDS` (exe `TDS\TDSgame.exe`, process `TDSgame`, Steam app 2392280) |
| Mods folder | `D:\Steam\steamapps\common\TDS\Mods\<Name>Mod` |
| Game assets | `D:\Steam\steamapps\common\TDS\TDS\DATA\StreamingAssets\aa\StandaloneWindows64\defaultlocalgroup_assets_all_*.bundle` (one ~300 MB Addressables bundle with every sprite, model and data asset; read it with UnityPy, `docs/18` §9) |
| Unity project | `C:\Users\Dimitri\TDS_Mod` (Unity 2022.3.41f1, URP 14, TDS Mod Tools imported) |
| Game log | `%USERPROFILE%\AppData\LocalLow\Blockage\TDS\Player.log` (overwritten each launch; `Player-prev.log` keeps the last) |
| Installed mods | normally none: the Mods folder is kept empty between PC rounds (2026-10-08); a round installs its dev build and removes it after. Exception since 2026-10-12: Yossele's play build (0.2.0-dev since PC round 9, 2026-10-23) stays installed for Dimitri's own play test, and a new round moves it out of `TDS\Mods` by hand and puts it back after (no script does this). The older vessel mods' sources sit in `C:\Users\Dimitri\TDS_Mod\Prototype Mods\<Name>`, outside Unity's `Assets` |
