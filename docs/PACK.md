# Rogue Waves: how the pack is built and shipped

Phase 2 decision (2026-10-08, approved by the user). It is built on `TDS_Vessel_Creator/docs/14` §5 (the two
layouts) and `docs/15` §10 (how the game loads mods). The style is in `docs/STYLE.md`.

## 1. Layout: one bundle, one part per prefab

The release is **one mod folder, `RogueWavesMod`**: one zip, one toggle in the Mods menu and one version. It carries
**one `Default` prefab per part**:

| Part | Prefab | Dev mod (own build, own self-test) |
| --- | --- | --- |
| The mode (Rogue Waves run, Pure Aura, shop, objectives, Directions catalogue, Cardinals and Trinkets, map vote, ascension) | `RogueWaves_Mode_Default` | `RogueWavesModeMod` (its engine work proven first in RogueLab) |
| Enemies and modifiers (Cursed, Berserk, Aging, Dazzling, new foes) | `RogueWaves_Foes_Default` | `RogueWavesFoesMod` |
| Argo, Yossele, Spool, Uloe (one part each) | `RogueWaves_<Vessel>_Default` | `<Vessel>Mod`, as in the kit today |

Why this layout:

- The game compiles **each prefab into its own assembly** (`docs/15` §10). So even a single bundle can't give parts a
  shared domain unless every line goes into one code string. That would mean four vessels and the mode compiled on
  every launch, where one compile error takes the whole pack down, and the kit, build scripts and self-tests are all
  per vessel. So parts stay separate in code and meet through game state, which they can do in one bundle as well as
  in several. One limit read from the loader since (kit `docs/23` §4.3, 2026-10-14): a compile error in one
  `Default` prefab skips the bundle's `Default` prefabs after it, so every part is compiled alone (cloud-check)
  before a pack build.
- One bundle beats several mod folders for players. There is one thing to install and enable, and an online lobby
  can't end up with mismatched part versions (every player needs the whole pack, a Phase 1 decision).
- Development doesn't change. Each part is built, exported and self-tested as its own dev mod with the kit pipeline.
  Only a release assembles the pack, in three steps (built 2026-10-28; first run in PC round Yossele_round10, PACK DONE ok): `TDS_Mod/Tools/pc/Build-Pack.ps1`
  builds each part's release code with the pack version, Tools > ModKit > Build Pack (`ModKitPipeline.cs`) builds
  each part's prefab under its pack name, parks every non-pack `Assets/*Mod` folder and exports everything once as
  `RogueWavesMod`, and `Build-Pack.ps1 -Zip` makes the zip of §5. The version and the parts are in
  `TDS_Mod/ModSource/Pack/pack.txt`, the player README in `TDS_Mod/ModSource/Pack/README.txt`.

**Prefab names.** Each name contains `Default` and none of the other keywords the game matches with `IndexOf`: `Map`
(it would join the custom-map list), `World`, `Mainmenu`, `Loading`, `Gameserver`, `Gameclient` (`docs/15` §10). No
two parts share an assembly name, a scene name or an id.

## 2. Load order: none, by design

Mods load in an undefined order, and so do prefabs (`docs/15` §10). Rules:

1. Every part works alone: a vessel without the mode is a normal vessel, and the mode runs with stock vessels.
2. A part that wants another part finds it through game state at run time (the registry below, an id in
   `Tds_Masterdatabase.Items`), waits for it up to 30 s after the main menu and degrades with one log line. It never
   assumes it loaded first or last.
3. **Standalone vessel mods clash with the pack.** ArgoMod 1.4.0 and the pack's Argo would both register `Hero_Argo`.
   The pack's Argo part checks whether `Items["Hero"]["Hero_Argo"]` already exists before it registers. If it does,
   the part stands down and shows one main-menu warning: "disable ArgoMod while Rogue Waves is on". The pack README
   says the same. ArgoMod 1.4.0 has no such check, so the README is the real guard.

## 3. Kit 1.6 and 1.7: what the parts share

The kit stays per-mod source, renamed `<Part>Kit` per part (`kit/README.md`). Growth (Yossele), Thread (Spool) and
Light (Uloe) are each **one vessel's mechanic and live in that vessel's part**. The kit adds only what several parts
must agree on. That is the 1.6 scope, built when Phase 3 starts with Argo:

| Kit file | Gives | Used by |
| --- | --- | --- |
| `ModKitPack.cs` | The **registry**: a `DontDestroyOnLoad` GameObject `RogueWaves_Registry` that every part finds or creates, with one child named `<Part> <version> kit <kit>`. It covers part detection, version and kit mismatch warnings, and the Argo stand-down above | every part |
| `ModKitTides.cs` | Host-side events from `Game_Mapvars`: the map starts, a Tide starts, the map ends (win, lose, leave); a break is not an event yet. One poll, many listeners | Growth, Pure Aura per Tide, Argo's tremors per Tide, vines per Tide, objectives |
| `ModKitCounter.cs` | A number drawn over an entity on every client, using the game's counter route (`docs/01` §6): set on the host, synced by the engine | Pure Aura on each vessel, Thread, Growth stage, seed count |
| `ModKitDirections.cs` | **Directions as data.** Each Direction is an `Upgradeproperties` named `Direction_<Vessel>_<Name>_Upgradeobject` (or `Direction_Any_<Name>_Upgradeobject`). The part that owns the mechanic registers it, and the mode lists every registered Direction by prefix, offers them and grants the pick with `Helper_Entitytools.Upgrade` on the **player's vessel** (the core is the team's, so a Direction there would be team-wide, `docs/15` §14.1). A respawn copies the dead vessel's upgrades into the new one (`docs/15` §14.1), and each map has new vessels, so the mode re-grants every held Direction from its host state at each map's first spawn (a repeat grant is ignored); tower Directions go through the part's own spawn hook, filtered by owner. A vessel asks `Has(vessel, name)` and reacts. Kit 1.6 still grants on the core; kit 1.7 makes this change at Phase 5 (`docs/RESEARCH_GAPS.md` A1). The upgrade is game state, so it syncs to every client and needs no shared types | the mode offers and grants; every vessel registers and reacts |
| `ModKitFocus.cs` | The scope's F-key fix (`docs/15` §9): idempotent, every vessel part runs it, so it covers Boutman and every pack vessel | every vessel part |

Rules kept from 1.5: tab order, boat layout, icons at boot. A part's Directions and its Cardinals' and Trinkets' upgrades load with
its own group, so they exist on every machine that has the pack.

## 4. Versions

- **One pack version**, `MAJOR.MINOR.PATCH`, in `RogueWavesMod.info`, in every part's `<Part>Data.Version` and in the
  pack README. It stays 0.x until Phase 7's release, which is 1.0.0. Every pack build bumps the version of every part,
  even the unchanged ones.
- The kit keeps its own version (1.6, 1.7, ...) in `kit/README.md`. Every part of one pack build carries the same kit.
- Each part logs `[RogueWaves:<Part>] v<pack> kit <kit>` at boot. The mode logs a warning, and shows one on the main
  menu, when the registry holds two different pack or kit versions.
- Dev mods use the pack version plus `-dev` and are never shipped.

## 5. Ship

One zip, `RogueWaves-<version>.zip`, holding the `RogueWavesMod` folder and `README.txt`. That README covers
installing (unzip into `TDS\Mods`, enable in the Mods menu, disable standalone vessel mods of the same name, and restart
the game after enabling or disabling the pack: the Mods menu's toggle undoes nothing, kit `docs/23` §4.3), how the
mode starts (the host presses the Rogue Waves button on the main menu, `docs/mode/MAIN_MENU_START.md`) and online play
(everyone needs the same version). There is no Workshop (`docs/14` §4).

**Release order (user, 2026-10-20).** The first release is this same bundle holding **only Yossele's part**
(`RogueWaves_Yossele_Default`), version 0.1.0, built with the pack steps above. Each later release adds parts to the
same bundle (the mode, Argo, Spool, Uloe, the foes) and bumps the one version, so a player only ever updates one mod and
no standalone vessel mod has to be swapped out; the full pack is 1.0.0. The plan is `docs/PLAN.md` Part B "Release 1".
