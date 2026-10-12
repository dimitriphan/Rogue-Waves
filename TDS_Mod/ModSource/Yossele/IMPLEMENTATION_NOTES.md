# Yossele implementation notes (0.1.0-dev, 2026-10-12)

**Built from the scope in one stretch: design (`DESIGN.md`), temporary art (`Assets/YosseleMod/Editor`,
`Previews/0.1/ASSETS.md`), code (`Code/`), self-test (`Code/99_SelfTest.cs`).** The folder started as a copy of
Mummer's scaffold; every Mummer mechanic, sheet, model, effect and sound is gone. New assets and every default value:
`docs/YOSSELE_BUILD_NOTES.md`.

**Checked here only:** `check_mod.py` compiles the mod with kit 1.6 at 0 errors, 0 warnings, in release and with
`YOSSELE_SELFTEST,YOSSELE_SHOTS,YOSSELE_PROBE`; `check_editor.sh` passes on `Assets/YosseleMod/Editor`; the cloud
preview renders every sheet and model. **Not built in Unity, not run, not seen in game.**

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
   "Stonehenge at ... faces ..."); the client turns the model's `Facing` part from the synced orientation. If the game
   already turns the whole tower, the model turns twice: drop the client turn.
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

None yet.
