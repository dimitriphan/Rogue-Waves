# Spool implementation notes (0.1.0-dev, 2026-10-26)

## 2026-11-06: PC round 5's test leftovers (compiled here, not yet run in game)

Test only (`docs/reports/PC_ROUND5_2026-11-06.md` §2.1): "Pin Barrage leads to Iron Thimble alone" replaces the stale
"ends its branch"; `Drive()` logs the map's end once (`Gamecomplete` past "Running", the end screen's words), and from
then on a fail is a SETUP that says so; `NextTide` logs a Tide that didn't come (`TideRose`) and Voodoo Doll's next-Tide
line is then a SETUP; Snare Line logs what the hunt sees as it starts and when it ends (`SpoolUnitBrain.SnareWhy`, self-test
only). Next run: once without `SPOOL_SHOTS` first (each picture adds 1-2 s; the round 5 map drowned mid-run).

## 2026-11-06: Diorama's tiny look, kit 1.15 (compiled here, not yet run in game)

The vessel's visual now carries the skins Diorama switches to, named after the being and its
copy's (`ModKitAssets.TinyLooks` in `04_Content.cs`: the same
sprites at half size; kit `docs/17` §11 rule 19), so holding Diorama shows it tiny; before, the stats came and the look
never did (`docs/reports/PC_FULL_2026-11-06.md` §5 item 1). For the PC: the mode's gaps check "Diorama: Spool,
Argo and Uloe carry the tiny look's skins too", and the look by eye.

## 2026-11-06: the 22 upgrades and UNTHREAD (compiled, not yet run in game)

Scope `08` B19 and B18 (Dimitri's "Yes", 2026-11-06): the Yarn Stash's 7 upgrades (Pockets, Quick Patch, Darning, Elbow
Patches, Remnant Bin, Mending Circle, Quilting Bee), the Cookie Tin's 3 (Tacking Stitch, Cross Stitch, Patchwork Army) and
UNTHREAD, the Thread Head's 5 (Bobbin, Needle Point, High Thread Count, Stitch in Time, Head of the Class), the Pin
Cushion's 2 (Iron Thimble, Voodoo Doll; Pin Barrage leads to Iron Thimble) and the Loom's 5 (Dyed in the Wool, Bobbin,
Seam Squad, Snare Line, Golden Thread). All 37 rows match the sheet (`Tools/cloud-check/scope_upgrades.py`). New: the
Tacked Stitched (`Mob_Spoolstitched_Tacked`, Sno Sno's blocker block), BEE on the Stash, UNTHREAD on a Tin and COLOUR on
a Loom (key 4). Pitfall #80, Spool's share: a brain that throws skips the tick (`SpoolFaults`, `01_Util.cs`) and stops
only after 600 frames running; #158: the build puff goes 0.5 s late and the Crafted's pop and leap through their home.
After `/code-review high`: a Tacked crowd holds where it stopped, a Seam's colour trade is never made twice, Woolen
Lining and Cross Stitch reach the Crafted already out. After the PR review: a Thread Head's RETURN and Threaded
Delivery walk to the nearer drop-off point (a Golden Thread Loom too); Head of the Class's Thread flies to the Stash; a
rebuilt Stash keeps Thread over its cap until spent. Self-test sections 10d-10f (`98_UpStash.cs`, `98_UpTinHead.cs`,
`98_UpLoom.cs`, about 75 checks). The readings picked where the sheet is silent: `DESIGN.md` "Defaults picked here".
For the PC: the self-test with the new sections; by hand BEE, UNTHREAD and COLOUR's buttons and icons, a Tacked line
on a path, a Voodoo stun, a Golden Thread drop-off, a Remnant flying home on a client.

## 2026-11-06: one Yarn Stash a player (compiled, not yet run in game)

Scope `08`:64 says Limit: 1, so a player holds one Yarn Stash. `SpoolServer.FirstStash`, `HasStash` and `StashLimit` (`05_Server.cs`) decide it. The Stash button goes from the spawn of a Stash, and the four buildables come once one has started (`06_HeroBrain.cs`, `CheckMenu`). A second Stash that gets past the menu is taken back with reason "Limit", and the first keeps the Thread (`07_Building.cs`). The PATCH hotkey test sets the Thread at the press. Three self-test checks are new: "a second Stash can't be built", "a second Stash past the menu is taken back" and "two Stash orders at once build one Stash, the first" (`docs/reports/BUG_FIXES_2026-11-06.md` §5 item 5).

## 2026-11-05: the balance sweep's numbers (compiled, not yet run in game)

For one Pin Cushion (Dimitri's answer 25) and the Loom's mining (answer 26), `docs/reports/BALANCE_SWEEP_2026-11-05.md`
section 3: Stitched Damage 2 -> 5, Dart Damage 2.5 -> 5.5, Crafted a Tin 4 -> 6, Dart Range kept at 3.5, the Loom's
Attackspeed 0.4 -> 0.3 (`02_Data.cs`; the stat fallbacks in `07_Building.cs` and `08_Tin.cs` follow). The Pin Cushion's
in-game line and journal text say to build it first (P4). The self-test's Tin check counts to 6 with Thread for 6. The
play build's `SPOOL_TUNE` package (Stitched 3, Dart 3.5, Range 4.5, 6 a Tin) is gone: its numbers no longer match.

## 2026-11-05: the clean-up, kit 1.12 (compiled, not yet run in game)

No number or behaviour changed. Merit Badge's health goes through `ModKitUtil.Keep`, which now makes a Health mod again as this did by hand; Odd Socks and the Pin Cushion set Damagemulti with `ModKitCombat.SetMulti`; `SpoolClient.PinsQuieted` (read by nothing) is gone. `docs/reports/BUILD_UNBUILT_2026-11-05.md` §2.

## 2026-11-05: the Sewing Machine's model and Button Jar's small Crafted

Compiled and rendered here, not yet run in game: `Spool_Sewingmachine` (the classic light blue machine on the Loom's
plinth, a sage felt scrap under the needle), the model `SewingModel` names; Button Jar's small Stitched and Dart, code
dolls `Stitchedsmall` and `Dartsmall` (the picked Crafted a row and a column smaller) saved as `Spoolstitchedsmall-Sheet`
and `Spooldartsmall-Sheet`. Their visual gets a `Small` skin when the library has the sheet (`SpoolContent.SmallLooks`), and
a Crafted with a Button Jar share puts it on in `Begin`. Self-test section 10c checks the skin. Temporary art
(`docs/TEMP_ART.md`); renders in `Previews/0.1/`.

## 2026-11-05: the decisions' build items

Compiled, not yet run in game: the pack's own foe modifiers (`RW_Modifier_`) count as elite for the Thread drop
(`SpoolServer.Elite`, B6); the Thread Head's stock MOVE has no button, its command stays for the walk (the link points at
`Command_Mob_Spoolhead_Move`, a None-styled copy of `Command_Move`, B16); the Cookie Tin's TARGET pin no longer takes a
Stitched's clicks: its hit colliders go off on every machine (`SpoolClient.QuietPins`, pitfall #162; round 3's selection
miss, B17's code half). For the PC: the Head's panel without MOVE and still walking to SPOT; a Stitched on its pin
selected and sold by a click.

**2026-11-05, the code review at max (kit 1.11, compiled, not yet run in game):** a Thread Head back with Patchwork Job shows MEND as it is (SPL-8: `Started` after the comeback upgrades); a last Stash, Loom or Head falling while Spool is down spills its Thread (SPL-10); "Loom strikes ore" is a SETUP with no ore in reach (one pass fewer on the test map). Correctness only; nothing about how Spool plays. `docs/reports/CODE_REVIEW_MAX_2026-11-05.md`.

**2026-11-05, crisp command icons (kit 1.10, compiled, not yet run in game):** the PATCH, TARGET, SPOT, RETURN and MEND buttons' 12 px icons go into Icon_Types as 16 px textures with the drawing centred (`AddIcon`'s box, `ModKitAssets.CommandIcon`), so the HUD draws them one unit per pixel instead of stretching them by 4/3, which jittered as the buttons moved (pitfall #156). They look a little smaller, the size of the game's own icons. Stat icons are unchanged.

**2026-11-05, the code review's fixes (compiled, not yet run in game; `docs/reports/CODE_REVIEW_FIXES_2026-11-05.md`):** two plain bugs in the Thread Head's walk, nothing about how Spool plays: with Delivery it heads home only when the Stash has room (SPL-1), and it turns round at the Stash whatever it carries (SPL-2). The PC checks: that report's §5.

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
| `97_Shots.cs`, `99_SelfTest.cs`, `98_Up*.cs` | Debug builds only; the self-test makes 133 checks (2026-11-04) in the sections of `DESIGN.md`'s test plan, and the upgrades' sections 10d-10f (2026-11-06) |
| `97_Measure.cs`, `98_Play.cs` | Play builds only (`SPOOL_PLAY`, with kit `ModKitPlay`): the virtual player and the balance round's measures |

## What the first PC round must show (each is an assumption the code makes)

1. **The Stash-first menu.** Until a Yarn Stash stands, the hero's `Buildoption1-4` are removed and only the Stash is
   offered; then the four buildables come back (checked every 0.5 s and also the moment a Stash spawns or goes (2026-11-06), remove and recreate, as Argo and Boutman do).
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

## Sage recolour (2026-11-05)

At Dimitri's word Spool is sage green, not purple: `SpoolCharacter.P` (plush, mouth, thread), `SpoolLowpoly.Pal` (felt
ramp, `Plush`, `Pale0-2`, renamed from `Purple` / `Lilac0-2`), the effects in `SpoolBuilder`, `Sage` (was `Purple`) in
`04_Content.cs` and `13_Journal.cs`, the units' `Tint`, `SpoolClient.ThreadColor`, `IconPalette`; the five minion sheets
and the tuft recoloured by `concept-tools/temp_art/spool_sage.py`. No number or behaviour changed. Compiled here
(release and `SPOOL_SELFTEST,SPOOL_SHOTS` 0 errors, editor clean, `lite/run_preview.sh sm` renders in `Previews/0.1/`).
Next PC round: the build log still says "from Art/Temporary/" for the five sheets, and Spool reads apart from a Hardened
foe and from the grass.

## Directions (kit 1.7)

`SpoolContent.BuildDirections` registers two Directions, one upgrade per rank (`Direction_Spool_<Name><rank>_Upgradeobject`);
the vessel's brain copies the ranks held to `SpoolPlayer` twice a second, and they stay there while the vessel is down.

- **Efficient Recycler** (`Recycler`): selling a Crafted refunds 65 / 80 / 100% of its cost, rounded down (`08_Tin.cs`);
  rank III also heals hurt same-team Mob and Tower things within 2.5 tiles once, by 10 (default, docs/scope/11).
- **Sewing Machine** (`Sewingmachine`, Clash 1): every Loom of that player keeps its model, gets a held +50% Attackspeed
  (default, docs/scope/11), spins no Thread (Thread it already holds can still be collected, and Direct Line does nothing),
  and each Tide heals its team's Mob and Tower around itself, Cores left out: radius 3 heal 10, radius 3 heal 20, radius 5 heal 20.
  Since 2026-11-05 (B8, decision 15) its panel says **Sewing Machine**: the Loom's visuals (base and every upgrade's) carry a
  `Sewing` skin whose entry's `Displayname` is the panel's title (`Game_Ui`), and the Loom shows it while its player holds
  the Direction. The model stays the Loom's until A1 draws `Spool_Sewingmachine`, which the skin then takes on its own.

With none held the numbers and behaviour are today's. Measured in game on 2026-11-04 (`docs/reports/TEST_ROUND_2026-11-04.md`): selling a Crafted refunds 65, 80 and 100%, rank III mends around it, a Sewing Machine's Looms mine half as fast again and spin no Thread, and it mends 10 and 20 each Tide.

Five more since 2026-11-05, scope 11's numbers in `02_Data.cs`, no `Clash`; compiled with and without `SPOOL_SELFTEST`,
not yet run in game (self-test section 10c, `98_Directions.cs`):

- **Double Needle** (`Doubleneedle`): a Tin's stitch time (`SpoolTinBrain.StitchTime`) is its Respawn x 0.85 / 0.7 / 0.7.
  Rank III: every third paid stitch a Tin makes while it is held adds one more of its kind for no Thread (`Doubled`),
  which may stand one over the Tin's Units until one falls.
- **Button Jar** (`Buttonjar`, a Shift with no clash): a stitch makes two for one's Thread, the cost split between them
  (what selling refunds); each has a Pmulti of share - 1 on Health and Damage (0.55 / 0.65 / 0.75), so Woolen Lining's
  Multi applies to the smaller base; the Tin's Units doubles; with one slot free a stitch makes one, for half the
  Thread. Selling refunds each its own half, rounded down (a 10-Thread pair gives back 2 + 2 where a whole one gives 5).
  Their look is the smaller one since the art PR (above).
- **Pattern Book** (`Patternbook`): a Tin's Crafted killed (not sold) is queued to come back (`SpoolTinBrain.Fell`, from
  the unit's `Gone`) while fewer than 1 / 2 / 3 have been queued this Tide (`ModKitTides.Wave`); it comes back of its
  own kind with Cost 0, before any paid stitch and in its place even past the Tin's Units, on the usual stitch time
  (rank III: at once); one that can't be made waits a second in the queue.
- **Merit Badge** (`Meritbadge`): a foe killed whose `Lasthit` is a Crafted (`SpoolServer.OnDespawn`) counts for it while
  its owner holds a rank: +3% Damage (a kept Multi) and Health (a Multi made anew, so its health keeps its share) each,
  up to +15 / 30 / 45%; a `Spool_Merit` look each time.
- **Odd Socks** (`Oddsocks`): every 0.4 s a Crafted with a Crafted of another kind and its team within 2 tiles
  (`SpoolServer.OtherKindNear`) has a kept +8 / 15 / 25% Multi on Damage, 0 when alone; rank III also puts a handler
  at the front of its hit chain (re-fronted every 0.1 s) that takes 10% off each hit (`SpoolUnitBrain.Socked`).

## Open in the sheet (Dimitri's)

Every default in `DESIGN.md` "Defaults picked here" (the upgrades' readings since 2026-11-06); his own sprites, models and sounds (all of this build's art is temporary).
