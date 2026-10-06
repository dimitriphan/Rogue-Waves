# Shared vessel-mod kit

Compiled into every vessel mod of the project (`TDS_Mod/ModSource/Shared/`); each build script renames the `ModKit`
prefix to `<Name>Kit`, so every mod gets its own copy of the types and none collide.

| File | Provides | Used as |
|---|---|---|
| `ModKitJournal.cs` | `ModKitJournalSpec`, `ModKitJournalBuilding`, `ModKitJournalUpgrade`, `ModKitJournalUnit`, `ModKitVesselJournal` (builds all of a vessel's journal pages), `ModKitVesselTabs` (one tab per vessel on every vessel page) | `docs/06_JOURNAL.md` |
| `ModKitBoat.cs` | `ModKitBoat.LateUpdate(heroId)`: one boat spot per vessel, every vessel clickable on a crowded deck (staggered rows), the vessel's button unlocked; `ClickReport` for self-tests | call it from the mod's `LateUpdate` |
| `ModKitTowers.cs` | `ModKitTowers.Fit`: a tower's hit prefab and range anchor at the brain's eye height, so the game draws one centred range; `RangePreview` | once per buildable, where its template is set up |
| `ModKitCombat.cs` | `ModKitCombat.Hurt`/`Mark`: hits from the mod's own code that deal damage and count as hits (kills, Aegis regen) | for every hit the mod deals itself |
| `ModKitTest.cs` | Self-test builds only (`MODKIT_TEST`, set by the build scripts with any `<NAME>_SELFTEST`): the pinned test map (`PickNode`), the map snapshot (`MapDump`), placement searches that log why and a picture of the ground (`Spot`, `FoeTile`, `SpawnFoe`, `Picture`), and the `TEST SETUP` outcome | `docs/08` |

## Versions

| Kit | Date | Change | Shipped in |
|---|---|---|---|
| 1.0 | 2026-10-02 | Argo 1.0's VesselJournal/VesselTabs moved here and generalised; boat spots laid on the deck (10 positions) | Mummer 1.0.0, Sprigg 1.4.1 |
| 1.1 | 2026-10-02 | Boat: extra spots beyond 10 take lanes at 1/4, 3/4, 1/8, 7/8 of each gap (first 10 positions identical to 1.0) | |
| 1.2 | 2026-10-03 | `ModKitTowers` (range anchors and range previews, after playtesters saw ranges below the towers); Argo now uses the kit too (its own VesselJournal/VesselTabs and boat code retired) | Mummer 1.1.0, Argo 1.1.0, Sprigg 1.5.0 |
| 1.3 | 2026-10-03 | `ModKitCombat` (hits that deal damage and count as hits, after Sprigg's towers were seen to "heal" foes); journal upgrades take hand-written EFFECTS lines (`ModKitJournalUpgrade.Effects`) | Mummer 1.2.0, Argo 1.2.0, Sprigg 1.6.0 |
| 1.4 | 2026-10-04 | Boat: with more vessels than shipped spots, every vessel stands in evenly spaced, staggered rows (2 rows to 11 vessels, 3 to 16, then smaller), back rows a little higher and click boxes moved onto the sprites, after 10 vessels left some unclickable; shipped spots' first positions kept on `Charselect/Spots_Home`; extras named like the shipped spots so 1.0-1.3 copies stand down; `ClickReport` self-test helper | |
| 1.5 | 2026-10-05 | `ModKitTest` (self-test builds only): every self-test plays one pinned map (Thehunt) and logs its ground once, placement searches every level and logs a picture of the ground when it fails, and a check whose scene can't be set up is `TEST SETUP`, not FAIL; after Argo 1.4.0 took nine PC rounds, seven for the test's own placement. Build scripts add `MODKIT_TEST` to any `_SELFTEST` build | compile-checked; first PC run pending |

## Compatibility rules (several mods run the kit at once)

- **Tab order** must never change: shipped vessels first (`Hero_Smacky`, `Hero_Snosno`, `Hero_Buster`, `Hero_Boutman`,
  `Hero_Fie`), then mods by id (ordinal). Argo 1.0 has its own copy with the same rule.
- **Boat layout** must be the same in every mod built with kit 1.4 or later (every mod sets positions every frame; two
  algorithms that disagree make spots flicker): change `ModKitBoat`'s layout constants only in a kit release and rebuild
  every vessel. Kit 1.4 names every spot like the shipped ones, which makes 1.0-1.3 copies in older builds leave the
  spots alone, so a 1.4 build can be installed beside older ones.
- When you change the kit: bump the version here and in the file header, keep the two rules, compile every mod in the
  project (`Build-<Name>Mod.ps1` for each), and rebuild + re-export the mods you ship.
