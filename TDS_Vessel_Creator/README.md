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
11. **Ship the release build** (no `-Define`), packaged as `<Name>_TDS_Mod_v<x.y.z>.zip` with a player README, and
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
| `docs/15_ROGUE_MODE_ENGINE.md` | The Rogue Waves mode's engine: modes and chaining maps (proven on a 3-map test chain), what survives a scene change, Merchut and bounties, Cardinals (artifacts), client choices, the save, the F-key bug, mod loading; go / fallback / no per scope feature, PC list |
| `docs/16_MAPS_AND_ECONOMY.md` | Inside a map: the grid, placement and enemy paths, the base game's map-special objects (flood, clock, nests, dens, gates), aura nodes and mining, the wallet and colour conversion, game speed, lighting; what a run can change mid-map |
| `kit/Shared/` | The shared kit every vessel compiles (kit 1.6): journal, boat, tower ranges, combat hits, self-test helpers, and the pack's registry, Tides, counter, Directions and F-key fix (`kit/README.md`) |
| `template/Mummer/` | The reference vessel (Mummer 1.0.0): runtime code, editor generator, build scripts |
| `template/DESIGN_TEMPLATE.md` | The design form (copied into each new vessel) |
| `tools/New-Vessel.ps1` | Scaffolds a new vessel from the template |
| `tools/cecil/` | Scripts that read the game's compiled code (IL, callers, string literals, API dump) |
| `reference/` | Base-vessel survey (conventions + full data dump), the game API dump, the base game's Addressables asset list (`addressables_default_group.txt`), RogueLab's round logs of the stock modes, Tide tables, artifacts, elites and enemies (`roguelab_round1.txt`-`roguelab_round3.txt`), and `maps/`: where the self-test map's ground snapshots go (none committed yet) |
| `examples/` | The design and implementation notes of the three vessels built so far; `examples/code/` holds Argo's and Sprigg's modules the docs cite (damage hooks, tower firing, turret aim/recoil/spin, a generator with moving parts, unit orders and a crew picker, ranged vessel, leftovers, data survey, base-vessel art dump) |

## Definition of done (every vessel)

- `DESIGN.md` complete, every change from the user's concept listed with its reason.
- Self-test: 0 failures, including the generic checks (vessel select, boat spot, build, upgrades, Tides, journal pages,
  tabs on every vessel page). Captures reviewed: boat, buildings, every effect, journal pages.
- Release build boots alongside every other installed vessel mod: `[<Name>] ready`, own icons, **0 exceptions**.
- Online checklist (`docs/07_ONLINE.md`) passed; ask the user to try it with a friend if it adds new kinds of sync.
- Zip, player README, `IMPLEMENTATION_NOTES.md`, and any new lesson added to `docs/09_PITFALLS.md`.

## Facts about this PC (adjust for another one)

| | |
|---|---|
| Game | `D:\Steam\steamapps\common\TDS` (exe `TDS\TDSgame.exe`, process `TDSgame`, Steam app 2392280) |
| Mods folder | `D:\Steam\steamapps\common\TDS\Mods\<Name>Mod` |
| Unity project | `C:\Users\Dimitri\TDS_Mod` (Unity 2022.3.41f1, URP 14, TDS Mod Tools imported) |
| Game log | `%USERPROFILE%\AppData\LocalLow\Blockage\TDS\Player.log` (overwritten each launch; `Player-prev.log` keeps the last) |
| Installed mods | none: the Mods folder is kept empty between PC rounds (2026-10-08); a round installs its dev build and removes it after. The older vessel mods' sources sit in `C:\Users\Dimitri\TDS_Mod\Prototype Mods\<Name>`, outside Unity's `Assets` |
