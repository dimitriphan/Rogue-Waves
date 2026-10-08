# Rogue Waves: how the pack is built and shipped

Phase 2 decision (2026-10-08, approved by the user). It is built on `TDS_Vessel_Creator/docs/14` §5 (the two
layouts) and `docs/15` §10 (how the game loads mods). The style is in `docs/STYLE.md`.

## 1. Layout: one bundle, one part per prefab

The release is **one mod folder, `RogueWavesMod`**: one zip, one toggle in the Mods menu and one version. It carries
**one `Default` prefab per part**:

| Part | Prefab | Dev mod (own build, own self-test) |
| --- | --- | --- |
| The mode (Rogue Wave run, Pure Aura, shop, objectives, Directions catalogue, Cardinals, map vote, ascension) | `RogueWaves_Mode_Default` | `RogueWavesModeMod` (grows out of RogueLab and Riptide) |
| Enemies and modifiers (Cursed, Berserk, new foes) | `RogueWaves_Foes_Default` | `RogueWavesFoesMod` |
| Argo, Yossele, Spool, Uloe (one part each) | `RogueWaves_<Vessel>_Default` | `<Vessel>Mod`, as in the kit today |

Why this layout:

- The game compiles **each prefab into its own assembly** (`docs/15` §10). So even a single bundle can't give parts a
  shared domain unless every line goes into one code string. That would mean four vessels and the mode compiled on
  every launch, where one compile error takes the whole pack down, and the kit, build scripts and self-tests are all
  per vessel. So parts stay separate in code and meet through game state, which they can do in one bundle as well as
  in several.
- One bundle beats several mod folders for players. There is one thing to install and enable, and an online lobby
  can't end up with mismatched part versions (every player needs the whole pack, a Phase 1 decision).
- Development doesn't change. Each part is built, exported and self-tested as its own dev mod with the kit pipeline.
  Only a release assembles the pack: the Build Queue gets a `Pack` step that parks every non-pack `Assets/*Mod` folder,
  builds each part's prefab and exports everything once as `RogueWavesMod`.

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

## 3. Kit 1.6: what the parts share

The kit stays per-mod source, renamed `<Part>Kit` per part (`kit/README.md`). Growth (Yossele), Thread (Spool) and
Light (Uloe) are each **one vessel's mechanic and live in that vessel's part**. The kit adds only what several parts
must agree on. That is the 1.6 scope, built when Phase 3 starts with Argo:

| Kit file | Gives | Used by |
| --- | --- | --- |
| `ModKitPack.cs` | The **registry**: a `DontDestroyOnLoad` GameObject `RogueWaves_Registry` that every part finds or creates, with one child named `<Part> <version> kit <kit>`. It covers part detection, version and kit mismatch warnings, and the Argo stand-down above | every part |
| `ModKitTides.cs` | Host-side events from `Game_Mapvars`: a Tide starts, a break starts, the map ends (win, lose, leave). One poll, many listeners | Growth, Pure Aura per Tide, Argo's tremors per Tide, vines per Tide, objectives |
| `ModKitCounter.cs` | A number drawn over an entity on every client, using the game's counter route (`docs/01` §6): set on the host, synced by the engine | Pure Aura on each vessel, Thread, Growth stage, seed count |
| `ModKitDirections.cs` | **Directions as data.** Each Direction is an `Upgradeproperties` named `Direction_<Vessel>_<Name>` (or `Direction_Any_<Name>`). The part that owns the mechanic registers it, and the mode lists every registered Direction by prefix, offers them and grants the pick with `Helper_Entitytools.Upgrade` on the player's **core**, like a mid-match Cardinal. It goes on the core because a respawn makes a new vessel entity that would drop the upgrade (`docs/15` §9). A vessel asks `Has(owner, name)` and reacts. Each map has a new core, so the mode re-grants every held Direction when a map starts, from its host state. The upgrade is game state, so it syncs to every client and needs no shared types | the mode offers and grants; every vessel registers and reacts |
| `ModKitFocus.cs` | The scope's F-key fix (`docs/15` §9): idempotent, every vessel part runs it, so it covers Boutman and every pack vessel | every vessel part |

Rules kept from 1.5: tab order, boat layout, icons at boot. A part's Directions and its Cardinals' upgrades load with
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
installing (unzip into `TDS\Mods`, enable in the Mods menu, disable standalone vessel mods of the same name), how the
mode starts (the host equips the "Rogue Wave" Cardinal on the boat) and online play (everyone needs the same
version). There is no Workshop (`docs/14` §1).
