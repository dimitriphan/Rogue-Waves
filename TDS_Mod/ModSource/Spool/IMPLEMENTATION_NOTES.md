# Spool implementation notes (0.1.0-dev, 2026-10-26)

**2026-11-04, code simplification (compiled, not yet run in game):** the game's own `Entity_Ai_General_Upgrade` runs the buildings' and the Thread Head's upgrade panels again (the brain's mirror of it is gone; it still hears each upgrade through `Master.Upgrade`), and the shared helpers and asset reader moved to kit 1.8's `ModKitUtil` and `ModKitAssets` with their bodies unchanged; the Pin Cushion keeps its handler first through `ModKitUtil.Front`. The PC checks: `docs/reports/SIMPLIFICATION_BUILD_2026-11-04.md` §4 (self-test 133/0, G1, G3, H1, L1).

**Built from the scope in one stretch: design (`DESIGN.md`), temporary art (`Assets/SpoolMod/Editor`,
`Previews/0.1/ASSETS.md`), code (`Code/`), self-test (`Code/99_SelfTest.cs`).** Started at Dimitri's "Begin
implementing Spools mechanics" (2026-10-26) from the kit's template (with the 1.6 pack calls,
`ModKitCombat` kill credit, `ModKitTowers.Fit` and unthrottled looks added to the template first). Every template
mechanic, sheet, model, effect and sound is gone. The sheet is `docs/scope/08_Vessel_Spool.txt` as PR #40 leaves it.

**Checked here first:** `check_mod.py` compiles the mod with kit 1.6 at 0 errors, 0 warnings, in release, with
`SPOOL_SELFTEST` and with `SPOOL_SELFTEST,SPOOL_SHOTS` (the play build, `SPOOL_PLAY` with `MODKIT_PLAY`, needs
`UnityEngine.InputSystem`, which cloud-check doesn't reference: it compiles on the PC only); `check_editor.sh` passes on `Assets/SpoolMod/Editor`; the cloud
preview (`lite/run_preview.sh ... sma`) renders every sheet and model and synthesises every sound. The walk (Move rows)
equals the concept's `SpoolWalk-Sheet.png` pixel for pixel, and the rest frame equals Dimitri's reshade. PC round 1
(below) built it in Unity and ran the self-test in game; what it proved is listed there, and the rest of what this page
says about the game is read from its code, not seen.

## Code map

| File | What |
|---|---|
| `00_SpoolMod.cs` | Boot (pack registry part "Spool", asset lookup, content, journal), per-frame calls into the kit and the server |
| `01_Util.cs` | Spool's own helpers (`Retag`, owner-only `Warn`, `Later`); stats, health, elites, tiles, walking, JSON, hurt and heal and Vis are the kit's `ModKitUtil` since kit 1.8 |
| `02_Data.cs` | Every name, text and number: the hero, Thread, the five buildables and their upgrade trees, the Crafted, warnings, sound levels |
| `03_Assets.cs` | The hero's animations, 12 px icons, tower material, sound levels; slicing, units, the boat sprite, sounds and effects are the kit's `ModKitAssets` since kit 1.8 |
| `04_Content.cs` | Clones the hero (Smacky), the Stash, Tin, Cushion and Loom (Smacky's hut and wall), the Thread Head and the Crafted (Smacky's miner), the Head's placement token and the Tin's pin (Buster's pad), the needle (the archer's arrow), five commands, the journal |
| `05_Server.cs` | Host rules: Thread per player (Stash and carried), ground drops (a list, no network entities), pickup, drop-off, the token swap, the Thread Head's comeback, spawn and despawn hooks |
| `06_HeroBrain.cs` | Spool: the Stash-first build menu, pickup and drop-off, PATCH, the fallback that lets the Sell picker find walking units |
| `07_Building.cs` | Base for the buildings (the game's own `Entity_Ai_General_Upgrade` runs the upgrade panel since 2026-11-04, the brain hears `Master.Upgrade`; unit spawns), the Yarn Stash, the Loom |
| `08_Tin.cs` | The Cookie Tin (stitching, TARGET, the pin) and every Crafted's brain (run to the spot, guard, leap, burst, mine) |
| `09_Head.cs` | The Thread Head: guard, collect, SPOT, RETURN, Threaded Delivery, MEND, death and comeback |
| `10_Cushion.cs` | The Pin Cushion's front-of-chain hit hook: reflect, spin, Slow Unravel, Loose Stuffing |
| `11_Client.cs` | Every look and sound (Vis RPCs): ground tufts, flights to the carrier and the Stash, pops, pricks, patch, warnings |
| `12_Commands.cs` | The PATCH picker (the template's HOOK picker on your own units and buildings), the RETURN and MEND buttons |
| `13_Journal.cs` | Journal pages, diagnostics dump |
| `97_Shots.cs`, `99_SelfTest.cs` | Debug builds only; the self-test makes 133 checks (2026-11-04) in the sections of `DESIGN.md`'s test plan |
| `97_Measure.cs`, `98_Play.cs` | Play builds only (`SPOOL_PLAY`, with kit `ModKitPlay`): the virtual player and the balance round's measures |

## What the first PC round must show (each is an assumption the code makes)

1. **The Stash-first menu.** Until a Yarn Stash stands, the hero's `Buildoption1-4` are removed and only the Stash is
   offered; then the four buildables come back (checked every 0.5 s, remove and recreate, as Argo and Boutman do).
   Check the bar's order and that a destroyed Stash brings the Stash-only menu back.
2. **The Thread Head token.** The build menu places `Mob_Spoolhead_Place` (tag `Spoolheadtoken`) on Buster's walk-through pad block and the
   host swaps it for the Thread Head at once (a `Forced` spawn on the token's block, then the token is despawned with
   reason "Place"). Check that the token can be placed on a path tile, the Head appears there, and foes walk past it.
   If `Trap_Busterpad` isn't loaded, Perma-Wall's walk-through block is used (a warning says so).
3. **TARGET and SPOT** reuse the fish's Relocate picker (`Entity_Command_Mob_Boutmanfish_Relocate`) on a tower and on
   a unit. Read from the game's code: it does its own picking and sends `{"Position":{x,y,z}}` to the selected thing.
   Check it opens from a Cookie Tin and from a Thread Head, and that the pin moves.
4. **Selling walking units.** The stock Sell picker finds a thing by the surface under the cursor (within 0.25) and its
   `Worldobject.name`; walking units are rarely on a tile centre, so Spool's brain falls back to the nearest of yours
   with that name within 1.5 tiles. Check selling a Stitched (5 Thread back to the Stash) and a Thread Head (Red 40 ×
   Sellrate, what it carried drops).
5. **Ground Thread** is drawn by each client: a `SpriteRenderer` tuft with the Crafted's sprite material, billboarded
   to the camera, bobbing and fading in its last 2 s; pickups fly to the carrier. Check the tufts show in URP, at a
   sensible size, and vanish when taken or after 15 s.
6. **The Crafted** are the stock fighter AIs (`Mob_Combat`, the Dart `Mob_Ranged`) walked by their brain. Check
   Stitched and Darts run to the spot without stopping for foes, then fight within their leash; that a Seam, idle next
   to ore, mines it (the stock melee AI strikes ore while idle, as Smacky's miners do); that a Mangled guards its Cushion.
7. **The Dart's needle** is the archer's arrow with `Spool_Needle` as its model: check it flies point first.
8. **The Pin Cushion's hit hook** sits at the front of `Oninteract` (Yossele's and Argo's pattern), re-applied every
   0.1 s. Check reflect 20% / 40% (with kill credit), a Thread per 25 damage taken, Slow Unravel's half-now, half-later
   split (the rest through `ModKitCombat.Hurt` every 0.5 s) and that a heal cancels what is still owed.
9. **Overstuffed** gives a Mangled Mob_Snoble's blocking block: check foes stop at it.
10. **The Thread Head's comeback**: killed, it returns at the Stash after 15 s with its upgrades given back as
    `Preupgrades` and MEND's state kept. Check the upgrades and the MEND button come back.
11. **PATCH**: the picker on your own units and buildings, 20 Thread from the Stash, 20% heal capped at 500, range 4,
    cooldown 5 s; the `Patch` animation plays (`SpoolPatch-Sheet`). Check the refusals show as owner-only lines.
12. **The Loom** strikes ore in reach with its own pick (Vis `Spool_Strike`); check the ore pays its owner and that it
    spins a Thread every 6 s up to 10 held.
13. **The look** (temporary art): sprite pivots, the Crafted at 16 px and the Head at 24 px against the game's units,
    the towers' size on a tile, the tufts' colour.

## Read from the game's code for this build

Round 1 ran each of these on the server side (the self-test sends the JSON itself), and each worked as read; what the
pickers send when clicked is still unseen:

- `Entity_Ai_Hero_Buildsell`: a build command is `{Tower, Position, Orientation, Owner}`; the server checks the hero's
  `Build` command for the being (`Goalstring`) and the price scale (`Goalvalue`); the built thing's parent is the hero
  and its `Sellvalue` the price scale. A sell is `{Tower, Position, Owner}`, refunded as Costs × health share (when
  `Sellhealthfactor`) × Sellrate × Sellvalue, plus upgrades × `Upgradesellvalue`; it despawns with reason "Sell".
- A command's `Action` runs after `Command`, and the stock AIs run before a mod's handler.
- A heal's amount is its data's `Damage` (`Helper_Damagetools.Doheal`).
- `Helper_Entitytools.Spawn(Link_Spawn, position, parent, sameAsParent)`; `Link_Spawn` carries `Genadd`, `Spawnstyle`,
  `Sellvalue`, `Upgradesellvalue`, `Preupgrades`, `Destroyonparent`, `Parent`, `Owner`.

## PC rounds

Spool must be the only dev mod in the Mods folder for a round (since 2026-11-03 the release build of every pack part is
there for Dimitri's play: park them and put them back after, `TDS_Mod/Tools/pc/README.md`). Build: the Build Queue (Build All Assets, Build Mod Prefab, Export); the self-test with
`SPOOL_SELFTEST`; captures with `SPOOL_SHOTS` (none taken yet).

- **Round 1 (2026-10-26, handoff `Spool_round1`, map 3B_Thehunt): 95 pass, 22 fail, 1 setup.** First build ever: Unity
  compiled the editor code at 0 errors, the queue built and exported it, the game loaded it (`v0.1.0-dev kit 1.6`), every
  being, upgrade, effect and icon registered, and none of the 11 exceptions in Player.log came from Spool (the game's
  own: a UniTask cancel during map load, `Bla_Ambient` at shutdown). Proven in game: the Stash-first menu (11 menu
  changes), the Thread Head token swap on Buster's pad block, TARGET's pin, the stock Sell picker finding walking units
  (the fallback was never needed), the Dart's needle, the Seam's comeback, MEND, PATCH and its refusals, the Stash's
  spill, the journal pages. Of the 22 fails, 21 were the self-test tripping over itself, fixed in the test for round 1b:
  - a foe is not tagged "Enemy" and has no stats in the frame it spawns (`Entity_Master.Start` adds them), so a foe
    killed in that frame drops nothing; the test now waits for both;
  - the vessel and the Crafted felled the Daggers the Cushion checks hit with, and a hit from a fallen foe lands
    nowhere; those foes now stand by the enemy core and hit from afar, and the checks read the Cushion's own counts;
  - the Tins kept stitching from the Stash, and the Thread Heads pulled in Thread lying about (from earlier checks, the
    Cushion's spin and the Looms) mid-check; the checks now net out the Stash's other traffic (`Traffic`), clear old
    drops, and remove the Thread Heads before the Cushion and Loom checks;
  - the comeback check looked for the returned Thread Head before its brain had started.
  One real slip: Slow Unravel's delayed half began on the next frame instead of half a second after the hit (5.625 taken
  at once instead of 5); fixed in `10_Cushion.cs`. Also gone: the never-assigned `forcing` flag (warning CS0649). Editor
  side, harmless: ModTool's restore after export logged "Missing Nested Prefab Asset" for `Spool_Needle` in
  `SpoolAssets_Library.prefab` and for the library in `SpoolMod_Default.prefab`; the exported mod loaded fine.
- **Round 1b (handoff `Spool_round1b`): 117 pass, 3 fail, 0 setup.** Every Cushion, Loom, PATCH, Stash and comeback
  check passes, with the game's own numbers: a Dagger's base health 10 (1 Thread), an Aegis's 80 (3), a Ballista's
  range 5 (not pricked until Pin Barrage). The three left were the test again: the vessel picked the Dagger's Thread
  up within the Stash's reach and handed it over in the same tick; both Thread Heads were already full (20) from
  earlier checks' Thread; One Last Surprise's foe was killed next to before its hit box had joined the physics scene
  that `Entityarea` searches (a guess; round 1c logs the foes in reach). No exception from Spool.
- **Round 1c (handoff `Spool_round1c`): 120 pass, 0 fail, 0 setup.** The burst found its foe once the test waited
  0.3 s (10 -> 2.5, the 7.5 expected; pitfall #112). No warning and no exception from Spool; Yossele 0.2.0-dev is back as
  the only mod.
- **Play test (2026-10-26, no handoff; `docs/reports/PLAYTEST_2026-10-26.md`).** A debug-only virtual player
  (`Code/98_Play.cs` with kit `ModKitPlay`, play builds only) played Purlest on its own income and Woozy with sandbox
  money, clicking the HUD: the Stash-only bar and then the four, a Thread Head placed on a path tile by hand, TARGET,
  SPOT, RETURN, MEND and PATCH picked or pressed on the map, all 15 upgrades bought by clicking, Thread Heads sold through
  SELL (a walking Stitched never: the aim couldn't land on the moving 16 px unit). One bug, fixed: every click on
  a Thread Head threw `KeyNotFoundException` "Quickaction2" in `Game_Selection.Addcommand`, which looks each keybind up
  in `Bl_Inputs.Actions` (only `Quickaction` and `1`-`8` exist), so the Head's panel never drew; RETURN's keybinds are
  now `{"4"}` and MEND's `{"3"}`, and the Head's SPOT, RETURN, MEND and upgrades were then used by hand. Balance: Purlest was
  lost at Tides 4, 1 and 2 (one Stitched from the 10 starting Thread holds the first Tide), Woozy at Tide 4 twice.
  After it: the self-test checks every Spool button's keys against `Bl_Inputs.Actions` (pitfall #113; passes in game since
  2026-11-03). Still open:
  selling a walking Stitched by clicking it. The stock Sell picker sells what the mouse ray hits (`Game_Client_Mouseobject`
  raycasts the hit boxes), and the virtual player's click kept missing the moving unit. Dimitri to try by hand; if it is
  hard for a person too, the fix is a larger hit box on the Crafted or a pause while the Sell picker is open.
  Temporary art wired 2026-10-27 (compiled and rendered here, not yet seen in game): the 12 x 12 tuft and the thicker
  needle from `Art/Temporary/README.md` (PR #44), because the play test never caught either in a capture.
  Then the Crafted's four sheets and the Thread Head's sheet: `SpoolBuilder.FileArt` (Yossele's override) loads
  `Art/<name>.png` or else `Art/Temporary/game_size/<name>.png` in place of the code's sheet when the size matches;
  editor-checked, then in every build since (2026-11-03, 2026-11-04); the build log should name the five sheets
  "from Art/Temporary/" and that the Crafted and the Head walk and strike in game.
  2026-11-05: the five sheets replaced by Dimitri's Snoble-sized picks (Thread Head v04 Bobbin, Stitched v06 Patchwork,
  Dart v01 Long Hood drawn a little larger, Seam v06 Button, Mangled v02 Burst; same names, sizes and layout,
  `docs/TEMP_ART.md` "Minion versions"). Same check at the next PC round, plus whether the smaller Crafted are still
  easy to click to sell.

What the self-test can't show, left for Dimitri's play test (numbers from the list above): it sends the Build, Sell,
TARGET, SPOT, RETURN, MEND and PATCH commands itself, so the clicking is unseen: the build bar's order and the Stash-only
bar (1), placing a Thread Head on a path tile by hand and foes walking past it (2), the pickers opening from a Tin and a
Thread Head (3), selling by clicking (4); also the tufts' look in URP (5), the Crafted running past foes to their spot
(6), the needle's flight (7), foes stopping at an Overstuffed Mangled (9), the Loom striking ore (12; no ore stood in
its reach on 3B_Thehunt) and the whole look (13).

## Directions (kit 1.7)

`SpoolContent.BuildDirections` registers two Directions, one upgrade per rank (`Direction_Spool_<Name><rank>_Upgradeobject`);
the vessel's brain copies the ranks held to `SpoolPlayer` twice a second, and they stay there while the vessel is down.

- **Efficient Recycler** (`Recycler`): selling a Crafted refunds 65 / 80 / 100% of its cost, rounded down (`08_Tin.cs`);
  rank III also heals hurt same-team Mob and Tower things within 2.5 tiles once, by 10 (default, docs/scope/11).
- **Sewing Machine** (`Sewingmachine`, Clash 1): every Loom of that player keeps its model, gets a held +50% Attackspeed
  (default, docs/scope/11), spins no Thread (Thread it already holds can still be collected, and Direct Line does nothing),
  and each Tide heals its team's Mob and Tower around itself, Cores left out: radius 3 heal 10, radius 3 heal 20, radius 5 heal 20.
  The Loom's panel still says Loom: a new name there needs a skin entry with its own `Displayname`, more than the one line allowed.

With none held the numbers and behaviour are today's. Measured in game on 2026-11-04 (`docs/reports/TEST_ROUND_2026-11-04.md`): selling a Crafted refunds 65, 80 and 100%, rank III mends around it, a Sewing Machine's Looms mine half as fast again and spin no Thread, and it mends 10 and 20 each Tide.

## Open in the sheet (Dimitri's)

The Yarn Stash's upgrades, the Thread Head's and the Loom's levels 2 and 3 and the level-4 capstones are on the sheet
since 2026-11-04 (designed, not built: `DESIGN.md`); every default in
`DESIGN.md` "Defaults picked here"; his own sprites, models and sounds (all of this build's art is temporary).
