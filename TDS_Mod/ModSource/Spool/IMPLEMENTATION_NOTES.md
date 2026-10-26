# Spool implementation notes (0.1.0-dev, 2026-10-26)

**Built from the scope in one stretch: design (`DESIGN.md`), temporary art (`Assets/SpoolMod/Editor`,
`Previews/0.1/ASSETS.md`), code (`Code/`), self-test (`Code/99_SelfTest.cs`).** Started at Dimitri's "Begin
implementing Spools mechanics" (2026-10-26) from the kit's template (Mummer's scaffold, with the 1.6 pack calls,
`ModKitCombat` kill credit, `ModKitTowers.Fit` and unthrottled looks added to the template first). Every Mummer
mechanic, sheet, model, effect and sound is gone. The sheet is `docs/scope/08_Vessel_Spool.txt` as PR #40 leaves it.

**Checked here only:** `check_mod.py` compiles the mod with kit 1.6 at 0 errors, 0 warnings, in release, with
`SPOOL_SELFTEST` and with `SPOOL_SELFTEST,SPOOL_SHOTS`; `check_editor.sh` passes on `Assets/SpoolMod/Editor`; the cloud
preview (`lite/run_preview.sh ... sma`) renders every sheet and model and synthesises every sound. The walk (Move rows)
equals the concept's `SpoolWalk-Sheet.png` pixel for pixel, and the rest frame equals Dimitri's reshade. **Not built in
Unity, not run in game: no PC round yet.** Every line below that says how the game behaves is read from its code, not
seen.

## Code map

| File | What |
|---|---|
| `00_SpoolMod.cs` | Boot (pack registry part "Spool", asset lookup, content, journal), per-frame calls into the kit and the server |
| `01_Util.cs` | Helpers: stats, health (`Value` left, `Value_Calc` max), elites, tiles, walking (`MoveTo`), JSON, hurt and heal, Vis, owner-only warnings |
| `02_Data.cs` | Every name, text and number: the hero, Thread, the five buildables and their upgrade trees, the Crafted, warnings, sound levels |
| `03_Assets.cs` | Sprite sheets to animations (hero, Crafted, Thread Head), the boat sprite, 12 px icons, sounds and effects |
| `04_Content.cs` | Clones the hero (Smacky), the Stash, Tin, Cushion and Loom (Smacky's hut and wall), the Thread Head and the Crafted (Smacky's miner), the Head's placement token and the Tin's pin (Buster's pad), the needle (the archer's arrow), five commands, the journal |
| `05_Server.cs` | Host rules: Thread per player (Stash and carried), ground drops (a list, no network entities), pickup, drop-off, the token swap, the Thread Head's comeback, spawn and despawn hooks |
| `06_HeroBrain.cs` | Spool: the Stash-first build menu, pickup and drop-off, PATCH, the fallback that lets the Sell picker find walking units |
| `07_Building.cs` | Base for the buildings (upgrade flow mirroring `Entity_Ai_General_Upgrade`, unit spawns), the Yarn Stash, the Loom |
| `08_Tin.cs` | The Cookie Tin (stitching, TARGET, the pin) and every Crafted's brain (run to the spot, guard, leap, burst, mine) |
| `09_Head.cs` | The Thread Head: guard, collect, SPOT, RETURN, Threaded Delivery, MEND, death and comeback |
| `10_Cushion.cs` | The Pin Cushion's front-of-chain hit hook: reflect, spin, Slow Unravel, Loose Stuffing |
| `11_Client.cs` | Every look and sound (Vis RPCs): ground tufts, flights to the carrier and the Stash, pops, pricks, patch, warnings |
| `12_Commands.cs` | The PATCH picker (Mummer's HOOK picker on your own units and buildings), the RETURN and MEND buttons |
| `13_Journal.cs` | Journal pages, diagnostics dump |
| `97_Shots.cs`, `99_SelfTest.cs` | Debug builds only; the self-test makes about 120 checks in the sections of `DESIGN.md`'s test plan |

## What the first PC round must show (each is an assumption the code makes)

1. **The Stash-first menu.** Until a Yarn Stash stands, the hero's `Buildoption1-4` are removed and only the Stash is
   offered; then the four buildables come back (checked every 0.5 s, remove and recreate, as Argo and Boutman do).
   Check the bar's order and that a destroyed Stash brings the Stash-only menu back.
2. **The Thread Head token.** The build menu places `Mob_Spoolheadtoken` on Buster's walk-through pad block and the
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

## Read from the game's code for this build (not yet seen in game)

These become pitfalls (`docs/09`) or kit text once a PC round proves them:

- `Entity_Ai_Hero_Buildsell`: a build command is `{Tower, Position, Orientation, Owner}`; the server checks the hero's
  `Build` command for the being (`Goalstring`) and the price scale (`Goalvalue`); the built thing's parent is the hero
  and its `Sellvalue` the price scale. A sell is `{Tower, Position, Owner}`, refunded as Costs × health share (when
  `Sellhealthfactor`) × Sellrate × Sellvalue, plus upgrades × `Upgradesellvalue`; it despawns with reason "Sell".
- A command's `Action` runs after `Command`, and the stock AIs run before a mod's handler.
- A heal's amount is its data's `Damage` (`Helper_Damagetools.Doheal`).
- `Helper_Entitytools.Spawn(Link_Spawn, position, parent, sameAsParent)`; `Link_Spawn` carries `Genadd`, `Spawnstyle`,
  `Sellvalue`, `Upgradesellvalue`, `Preupgrades`, `Destroyonparent`, `Parent`, `Owner`.

## PC rounds

None yet. Round 1: build in Unity (Tools > Spool > Build All Assets, then the Build Queue), the self-test with
`SPOOL_SELFTEST`, captures with `SPOOL_SHOTS`; Spool must be the only dev mod in the Mods folder for the round
(Yossele 0.2.0-dev is Dimitri's play build there now: park it and put it back after).

## Open in the sheet (Dimitri's)

`! NEEDS YARN STASH UPGRADES !`, the Thread Head's and the Loom's levels 2 and 3, level-4 capstones; every default in
`DESIGN.md` "Defaults picked here"; his own sprites, models and sounds (all of this build's art is temporary).
