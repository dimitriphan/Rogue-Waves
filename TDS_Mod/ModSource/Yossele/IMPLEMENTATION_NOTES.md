# Yossele implementation notes (0.1.0-dev, 2026-10-12)

**Built from the scope in one stretch: design (`DESIGN.md`), temporary art (`Assets/YosseleMod/Editor`,
`Previews/0.1/ASSETS.md`), code (`Code/`), self-test (`Code/99_SelfTest.cs`).** The folder started as a copy of
Mummer's scaffold; every Mummer mechanic, sheet, model, effect and sound is gone. New assets and every default value:
`docs/YOSSELE_BUILD_NOTES.md`.

**Checked here only:** `check_mod.py` compiles the mod with kit 1.6 at 0 errors, 0 warnings, in release and with
`YOSSELE_SELFTEST,YOSSELE_SHOTS,YOSSELE_PROBE`; `check_editor.sh` passes on `Assets/YosseleMod/Editor`; the cloud
preview renders every sheet and model. **Built in Unity and run in game (PC round 5, 2026-10-12): 91 pass, 0 fail, 0
setup.** Not yet seen by eye or played online; Viral Outbreak and Hallucinogen (chance-based) are not checked.

## Code map

| File | What |
|---|---|
| `00_YosseleMod.cs` | Boot (pack registry part "Yossele", asset lookup, content, journal), per-frame calls into the kit and the server |
| `01_Util.cs` | Helpers: stats, health (`Value` left, `Value_Calc` max), tiles, ground spawns, kept modifiers, slows and roots, hurt via `ModKitCombat.Hurt` |
| `02_Data.cs` | Every name, text and number |
| `03_Assets.cs` | Sprite sheets to animations, pixel icons |
| `04_Content.cs` | Clones the hero (Smacky), the four towers, vine and fruit ground beings, commands, Directions, Growth skins |
| `05_Server.cs` | Host rules: players, fertilizer, Compost Bin, Tides (`ModKitTides`), spawn and despawn hooks, the Tree's limit |
| `06_HeroBrain.cs` | Build options, regen, FERTILIZE |
| `07_Building.cs` | Base for the four buildings: Growth, upgrade flow (mirrors `Entity_Ai_General_Upgrade`) |
| `08_Cannon.cs` .. `11_Pachira.cs` | One brain per building, plus spores, vines and fruit |
| `12_Hooks.cs` | One `Oninteract` front hook: Stonehenge armour, Blockade barrier, Connected Roots |
| `13_Client.cs`, `14_Commands.cs` | Every look and sound (Vis RPCs), the FERTILIZE picker and COLOUR button |
| `15_Journal.cs` | Journal pages, diagnostics dump |
| `97_Shots.cs`, `99_SelfTest.cs` | Debug builds only |

## What the first PC round must show (each is an assumption the code makes)

1. **Growth timing.** `ModKitTides.Tide` fires as a Tide begins; a building built during Tide N first grows at N+2.
   The self-test forces Tides and checks counter and skin `Growth<n>`.
2. **Growth skins.** Each tower gets skins `Growth1`..max, each adding prefab `Yossele_Growth<n>` to the tower's own
   model. If a skin swaps the whole look instead of adding to it, the tower loses its upgrade model.
3. **Vines and fruit on tiles.** They are Sno Sno's snow (`Effect_Snosnosnow`, ground-effect block, tag `Sno` and its
   AI stripped) spawned `Forced` on a tile, building tiles included. Unknown: whether a forced spawn takes a tile a
   building stands on, and whether the snow's model origin matches `Yossele_Vine` (ground at y 0) and the fruit
   (tower convention, ground at y -0.5); one of them may sit half a tile off. Fallback template: Smacky's Bramble.
4. **Stonehenge facing.** Read from `Beingserver.Orientation` through `Helper_Gridtools.Orientationdir` (logged as
   "Stonehenge at ... faces ..."). The game turns every placed tower's `Vis` by its orientation
   (`Entity_Master_Client`, read from the game's code 2026-10-12), so the client's own turn was dropped: it would have
   turned the model twice. Round 6 builds the second Stonehenge turned 90 degrees and checks model and vines agree.
5. **The hit hook.** `YosseleHooks` reads `Damage` and `Damagemulti` from each hit at the front of `Oninteract`; hits
   without them are counted as `Skipped` (logged in the self-test's counters). Armour, barrier and Connected Roots do
   nothing if every hit is skipped.
6. **Ore and fruit.** The Sapling mines with the miner's `Hurt` (`Damageresource`); a node emptied within 2 s of its
   strike drops a fruit. The pickup looks for Hero- or Mob-tagged beings within 0.75 tiles each frame.
7. **FERTILIZE.** The button only appears once a Compost Bin is held (the self-test grants one with
   `ModKitDirections.Grant`). The picker is Mummer's HOOK picker on your own buildings; check it shows and that its
   border lands on the building.
8. **The Tree's limit** removes `Buildoption2` while one stands and re-creates it after; check the bar's order.
9. **Debuff heights.** Overhead markers follow Mummer 1.0's verified heights; if they sit at the foe's feet, raise
   them 0.5 (`ASSETS.md`).
10. **Cannon turret.** `Vis/Turret` aims its mouth (local -z) at each lob's landing spot.

## PC rounds

**Round 1 (2026-10-12):** applied, Unity compiled the editor code clean, the code build passed; stopped at the export
(it didn't park the other mods, pitfall #88) and the sheet slicing read a scaled width (pitfall #87). Both fixed.

**Round 2 (2026-10-12, 3B_Thehunt): 86 pass, 1 fail, 1 setup.** Boot clean (`ready: 6 beings, 22 upgrades, 3
Directions, 16 effects, icons ok, ground from Effect_Snosnosnow`), no Yossele exceptions (11 lines, all the game's own
sound and setup ones). Proven in game: stats and regen, building, costs, the Tree's limit and its return, Growth on
forced Tides (counter and skin, stops at the max, no growth in the Tide built), Compost Bin unlocks FERTILIZE and pays
1 per 10 kills, FERTILIZE grows a stage and refuses a full building, every Cannon mechanic but the chance-based Viral
and Hallucinogen, the Dandelion's seeds, limit, range and Seed Packets, vines along the facing (orientation 0 faces
+z), longer with Growth, Verdant 3 -> 14 tiles, the vine slow, Carnivore, Blockade's health and barrier, Natural
Remedy, the Sapling's mining, fruit drop and pickup (30 of each), Grafted Colors (63 Blue), the journal and tabs.
- FAIL: "the game attaches Debuff_Slow": a modifier draws nothing; the vines now put the stock slow look on.
- SETUP: Spaghetti Roots' foe walked off the vines before 7 s; the test now holds it there.
- Not reached: the hit hook (armour, barrier, Connected Roots) saw no hit (`softened 0, soaked 0, shared 0, skipped
  0`); round 3 adds a real hit on each. The "far foes" check spawned none (it aimed off the hill); fixed.
- The slice warnings came from round 1's stale .meta on the first import; the meta is right now (48 slices).

**Round 3 (2026-10-12): 86 pass, 1 fail, 2 setup.** No slice warnings. Now proven in game: the stock slow look on the
vines, Growth's armour (a hit of 10 costs 6), Vine Blockade's barrier soaking a hit (72 -> 62, health untouched),
Connected Roots (cannon lost 3.5, Sapling 6.5); the far-foes Compost check ran (0 after 3).
- FAIL: "seeds grow back over time" read 0 seeds 1.5 s after emptying (it passed in round 2 with 0.75). Cause not
  found from the log; round 4's check waits up to 4 s and logs the limit, the Seedlimit stat, brain and strikes.
- SETUP: Seed Packets (this time no seed felled the foe; a chance of the test, not a bug) and Spaghetti Roots (the
  rooted foe still never stayed 7 s; round 4 moves its start back once it counts as on the vines, and logs it).

**Round 4 (2026-10-12): 58 pass, 0 fail, 1 setup, then the test itself stopped** on a NullReferenceException reading
the Dandelion Tree's brain: the tree had been removed by then (its brain destroys itself when its entity goes). Most
likely Tide foes felled it (they keep marching while the test holds the Tide clock), which also explains round 3's
"0 seeds" read. Round 5: every building logs when it goes and why, each test section clears Tide foes first, and a
felled building turns its checks into setup lines instead of stopping the run.

**Round 5 (2026-10-12): 91 pass, 0 fail, 0 setup.** Export clean, 0 slice warnings, no Yossele exceptions (the game's
own `Game_Ui.OnDestroy` and `Game_Init.OnDestroy` NREs at shutdown, also in earlier Argo and RogueLab logs). The only
building lost was the Tree the test fells on purpose, so clearing Tide foes per section was the fix. Newly proven: seeds
grow back (0.5 a second, ready 0.501 at limit 12), Seed Packets (2 refunded), Entangling Spaghetti Roots (1 snare),
every hit-hook check again. Not checked: Viral Outbreak's 30% burst and Hallucinogen's 20% daze (0 of each this run).
Left for Dimitri: the look by eye (temporary art, debuff heights, the Stonehenge's turn, vine and fruit placement), the
balance pass, and online play.

**Dimitri's notes from playing (2026-10-12), changed for round 6:**
- The Stonehenge now shows its facing like Sno Sno's Ice Wall. Read from the game's code: the build command draws an
  arrow on the placement border when a tower's hit prefab has a `Forward` child, and `Entity_Vis_Tower_Orientation`
  draws one while it is selected. The Stonehenge gets both; the marker copies the Ice Wall's own `Forward` when it can
  be read (`YosseleContent.ShowFacing`, logged at boot).
- Vine tiles are a flat 20 x 20 pixel texture on the ground like Sno Sno's snow (`YosseleModels.VineGround`, a
  cut-out quad), not a low-poly mat.
- The Dandelion Tree's range is 6 at Growth 0 (was 3); each stage still adds 40% of it.
- Selfish Sapling is reversed: +20% mining speed for each other Sapling within 4 tiles, up to +100%.
- Not seen yet by eye: the two arrows, the vine texture's look and height.

**Round 6 (2026-10-12): 96 pass, 1 fail, 0 setup.** Proven in game: the Stonehenge's facing arrows (the Ice Wall has
the same two: `Entity_Vis_Tower_Orientation` and a `Forward` marker at the tile centre), a Stonehenge turned 90 degrees
faces and grows its vines that way with the model turned once, the Tree's range 6 (10.8 at Growth 2), Selfish's rule.
- FAIL: the vine tiles had no texture. Boot puts every library model onto one copy of the game's tower material with
  the palette texture (`YosseleAssets.ApplyTowerMaterial`), the vine quad included. Round 7 leaves `*_Ground`
  materials alone and logs what a live vine tile and Sno Sno's snow draw with.

**Round 7 (2026-10-12): 97 pass, 0 fail, 0 setup**, then the play build installed for Dimitri. The vine tiles draw
their own texture (URP Lit, cut out, 0.01 above the tile). Sno Sno's snow is a 0.8 x 0.1 x 0.8 mesh on the game's
`Shader Graphs/General Block` material with no texture: if the vines should be lit exactly like it, that shader is
the one to copy with the vine texture. Waiting on Dimitri's look by eye.
