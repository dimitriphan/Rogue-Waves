# Shared vessel-mod kit

Compiled into every vessel mod of the project and every part of the Rogue Waves pack (`TDS_Mod/ModSource/Shared/`); each build script renames the `ModKit`
prefix to `<Name>Kit`, so every mod gets its own copy of the types and none collide.

| File | Provides | Used as |
|---|---|---|
| `ModKitJournal.cs` | `ModKitJournalSpec`, `ModKitJournalBuilding`, `ModKitJournalUpgrade`, `ModKitJournalUnit`, `ModKitVesselJournal` (builds all of a vessel's journal pages), `ModKitVesselTabs` (one tab per vessel on every vessel page) | `docs/06_JOURNAL.md` |
| `ModKitBoat.cs` | `ModKitBoat.LateUpdate(heroId)`: one boat spot per vessel, every vessel clickable on a crowded deck (staggered rows), the vessel's button unlocked; `ClickReport` for self-tests | call it from the mod's `LateUpdate` |
| `ModKitTowers.cs` | `ModKitTowers.Fit`: a tower's hit prefab and range anchor at the brain's eye height, so the game draws one centred range; `RangePreview` | once per buildable, where its template is set up |
| `ModKitCombat.cs` | `ModKitCombat.Hurt`/`Mark`: hits from the mod's own code that deal damage and count as hits (kills, Aegis regen) | for every hit the mod deals itself |
| `ModKitPack.cs` | Kit 1.6, the pack: `Join(part, version)` registers a part in one `RogueWaves_Registry` object and warns when parts disagree on pack version or kit; `Has(part)` (another part joined); `Taken(heroId)` (a standalone copy already loaded), `Mark(job)` (one part does a pack-wide job), `Notice` (an error the player sees) | the repository's `docs/PACK.md` |
| `ModKitTides.cs` | Kit 1.6: `MapStart`, `Tide(n)` and `MapEnd(result)` events, host only, polled from `Game_Mapvars` (a rewind resets without firing) | call `Update()` from the part's `Update` |
| `ModKitCounter.cs` | Kit 1.6: a number shown on an entity as a stat (`Set`/`Get`), e.g. Argo's Fathom | |
| `ModKitDirections.cs` | Kit 1.6: Directions, the Rogue mode's per-vessel upgrade picks: ids, `Make`, `Has`, `Grant` (host; 1.6 grants on the team's Core, which makes a Direction team-wide; kit 1.7 moves it per player onto the vessel, `docs/15` §14.1) | `docs/15` |
| `ModKitFocus.cs` | Kit 1.6: the F-key fix (the first F after a respawn selects the vessel again), installed once per pack | call `Update()` from the part's `Update`; `docs/15` §9 |
| `ModKitTest.cs` | Self-test builds only (`MODKIT_TEST`, set by the build scripts with any `<NAME>_SELFTEST`): the pinned test map (`PickNode`), the map snapshot (`MapDump`), placement searches that log why and a picture of the ground (`Spot`, `FoeTile`, `SpawnFoe`, `Picture`), and the `TEST SETUP` outcome | `docs/08` |
| `ModKitPlay.cs` | Play builds only (`MODKIT_PLAY`, never set by a build script: `-Define <NAME>_PLAY,<NAME>_SHOTS,MODKIT_PLAY,MODKIT_TEST`): a virtual player for a vessel's play test. Input System events on the game's own mouse and keyboard reach the HUD, the map and the keys as a hand does; aimed clicks (`ClickTile`, `ClickThing`); a player's moves (`SelectVessel`, `Build`, `Upgrade`, `Sell`, `Command`, `WalkTo`, `FastForward`); `Build` and `Upgrade` fall back to the command and log `FALLBACK` when the click didn't take, the others log that the move by hand failed; the foes' lanes (`PlanLanes`), the map loop (`Plans`, `Scenario`) | each vessel's `Code/98_Play.cs`; the repository's `docs/reports/PLAYTEST_2026-10-26.md` |

## Versions

| Kit | Date | Change | Shipped in |
|---|---|---|---|
| 1.0 | 2026-10-02 | Argo 1.0's VesselJournal/VesselTabs moved here and generalised; boat spots laid on the deck (10 positions) | Mummer 1.0.0, Sprigg 1.4.1 |
| 1.1 | 2026-10-02 | Boat: extra spots beyond 10 take lanes at 1/4, 3/4, 1/8, 7/8 of each gap (first 10 positions identical to 1.0) | |
| 1.2 | 2026-10-03 | `ModKitTowers` (range anchors and range previews, after playtesters saw ranges below the towers); Argo now uses the kit too (its own VesselJournal/VesselTabs and boat code retired) | Mummer 1.1.0, Argo 1.1.0, Sprigg 1.5.0 |
| 1.3 | 2026-10-03 | `ModKitCombat` (hits that deal damage and count as hits, after Sprigg's towers were seen to "heal" foes); journal upgrades take hand-written EFFECTS lines (`ModKitJournalUpgrade.Effects`) | Mummer 1.2.0, Argo 1.2.0, Sprigg 1.6.0 |
| 1.4 | 2026-10-04 | Boat: with more vessels than shipped spots, every vessel stands in evenly spaced, staggered rows (2 rows to 11 vessels, 3 to 17, then smaller), back rows a little higher and click boxes moved onto the sprites, after 10 vessels left some unclickable; shipped spots' first positions kept on `Charselect/Spots_Home`; extras named like the shipped spots so 1.0-1.3 copies stand down; `ClickReport` self-test helper | |
| 1.5 | 2026-10-05 | `ModKitTest` (self-test builds only): every self-test plays one pinned map (Thehunt) and logs its ground once, placement searches every level and logs a picture of the ground when it fails, and a check whose scene can't be set up is `TEST SETUP`, not FAIL; after Argo 1.4.0 took nine PC rounds, seven for the test's own placement. Build scripts add `MODKIT_TEST` to any `_SELFTEST` build | Argo's and RogueLab's self-tests (PC rounds, 2026-10-08/09) |
| 1.6 | 2026-10-08 | The pack (the repository's `docs/PACK.md` §3): `ModKitPack` (registry, version check, one-copy jobs, player notices), `ModKitTides`, `ModKitCounter` (Argo's own counter moved here), `ModKitDirections`, `ModKitFocus` (the F-key fix RogueLab compiled and ran; the bug itself never reproduced, so a by-hand check is left (`docs/15` §9)). Nothing in 1.0-1.5 changed, so tab order and boat layout are unchanged | Argo 2.0 (`v0.1.0-dev`; PC rounds 1-6, 2026-10-08), RogueWavesMode 0.1.0-0.2.1 (PC rounds 1-6, 2026-10-11), Yossele 0.1.0-dev (PC rounds 1-8, 2026-10-12) |
| 1.6 + play | 2026-10-26 | `ModKitPlay.cs` added for play builds only (no version bump: release and self-test builds compile without it and nothing in 1.6 changed; 1.7 stays the Leave fix's) | Yossele 0.2.0-dev and Spool 0.1.0-dev play builds (play test 2026-10-26) |

## Compatibility rules (several mods run the kit at once)

- **Tab order** must never change: shipped vessels first (`Hero_Smacky`, `Hero_Snosno`, `Hero_Buster`, `Hero_Boutman`,
  `Hero_Fie`), then mods by id (ordinal). Argo 1.0 has its own copy with the same rule.
- **Boat layout** must be the same in every mod built with kit 1.4 or later (every mod sets positions every frame; two
  algorithms that disagree make spots flicker): change `ModKitBoat`'s layout constants only in a kit release and rebuild
  every vessel. Kit 1.4 names every spot like the shipped ones, which makes 1.0-1.3 copies in older builds leave the
  spots alone, so a 1.4 build can be installed beside older ones.
- When you change the kit: bump the version here and in the file header, keep the two rules, compile every mod in the
  project (`Build-<Name>Mod.ps1` for each), and rebuild + re-export the mods you ship.
