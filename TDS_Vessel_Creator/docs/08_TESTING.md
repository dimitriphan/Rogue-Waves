# 08 · Testing in the game

The game ignores injected mouse/keyboard input, and taking desktop screenshots is forbidden (one captured another game
the user was playing). So every vessel carries an **in-mod self-test**, compiled only into test builds, that starts a
solo match by itself, plays every mechanic through the same commands a player sends, and writes PASS/FAIL lines and
in-game captures to `Player.log`. A run takes 3-4 minutes from launch.

## 1. Running it

```powershell
powershell -ExecutionPolicy Bypass -Command "& '...\ModSource\<Name>\Build-<Name>Mod.ps1' -Define <NAME>_SELFTEST,<NAME>_SHOTS"
# Unity: Build Mod Prefab, Park Other Mods, Export Mod (wait for both passes), Restore Parked Mods
powershell -ExecutionPolicy Bypass -File ...\ModSource\<Name>\Deploy-Wait.ps1 -Launch     # ask the user first
```

Several vessels in one round: `TDS_Mod/Tools/pc/README.md` (Tools > ModKit > Build Queue exports them in one call;
`Run-SelfTests.ps1 -Vessels A,B` backs up the save, runs each self-test in its own launch and writes a short summary;
it switches only the parts it is given, pitfall #147). Two more kinds of run live there: a **play build**
(`ModKitPlay`, `-Define <NAME>_PLAY,<NAME>_SHOTS,MODKIT_PLAY,MODKIT_TEST`, run by `Run-Play.ps1`), a virtual player that
clicks the HUD and the map as a hand does, and the mode's **two-player round** (`Run-TwoPlayers.ps1`), a real second
client from a copy of the game on the same PC (`07` §4, pitfall #144).

Watch for the result (the log is ~40 MB with captures, so filter out dump lines):

```powershell
$log = "$env:USERPROFILE\AppData\LocalLow\Blockage\TDS\Player.log"
Get-Content $log | Where-Object { $_ -notlike '`[<Name>Dump`]*' } | Select-String '\[<Name>\] TEST (FAIL|DONE)|Exception'
```

Then close the game (`(Get-Process TDSgame).CloseMainWindow()`), copy the log to a scratch folder (the next launch
overwrites it), and decode the captures:

```powershell
powershell -ExecutionPolicy Bypass -File ...\ModSource\<Name>\Tools\Decode-Dump.ps1 -Log <copy> -Out <dir>
```

Read the PNGs and look: zoom into regions by cropping with System.Drawing when details are small. Afterwards, rebuild
without defines, export and run `Deploy-Wait.ps1` **without** `-Launch` so the installed mod doesn't auto-play.

## 2. What the harness does (`99_SelfTest.cs`)

1. **Main menu** → `Mainmenu_Main.SendMessage("Play")` (Embark), retry with `Transitiontomain` after 25 s.
2. **World map** (`Macro_Main` spawned) → `Selectcharacter(Hero_<Name>)`, `PickNode()` → `ModKitTest.PickNode`: the
   pinned test map `ModKitTest.TestPlan` (`3B_Thehunt`), else the last played map, else the first without an unlock
   condition → `Tds_Multiplayer_Remote.Selectmap_ServerRpc(node, false, default)` → `Playgame(true)`. Maps are fixed
   prefabs (`Game_Grid.Generatemap` has no randomness), so a pinned map is the same ground in every run. With SHOTS,
   first the boat select is opened and captured (`BoatShot`).
3. **Match** (the hero brain exists) → the `Scenario` coroutine sets `ModKitTest.Log`, logs the map's ground once
   (`ModKitTest.MapDump`, below), then runs the sections. Tides are frozen by holding
   `Game_Mapvars.Time_Wave` at 900 and forced by setting it to -1 (a small positive value does **not** advance it).

Shared helpers (kit 1.5 `ModKitTest`, self-test builds only): `Spot(c, being, team, ring, maxDist, used, what)` (a
free building tile: the ground within a level nearest the ring, then every level from 2 above to 6 below, nearest
first), `FoeTile`/`SpawnFoe(being, near, minDist, maxDist, skip)` (a standable tile within a level; spawned as a child of
the enemy core), `Ground(c, maxDist, up, down)`, `Picture(c, r, used)` (the ground around c as text), `Setup(name, why)`.
When `Spot` or `SpawnFoe` finds nothing they log why and a picture of the ground, so one PC round shows the whole
problem. The template uses them; older vessels keep their own copies until their self-test next changes.

Template helpers worth reusing (note `SpawnFoe`'s argument order differs from the kit's): `Send(master, link, json)` (a real `Command_ServerRpc`), `Build(hero, tower, at, known,
result)` (a real "Build" command, then waits for the new building's brain), `Spot(hero, being, ring)` (a placeable
tile at a distance), `SpawnFoe(being, near, maxDist, minDist, onBuildings)` (spawns an enemy **as a child of the enemy
core**, on a standable `Surface` tile searched at 3 heights), `Kill(e)` (despawn as a kill: drops and stats),
`ClearFoes()` (despawn without a kill), `ForceTide`, `Upgrade(building, key, owner)`, `BaseStat`, `Res(owner, aura)`,
`DebuffReport(foe, vfx)` (waits for the RPC, logs where the game attached the look: path, offset, renderer bounds),
`Until(condition, timeout)`.

## 3. Generic sections every vessel keeps

| Section | Checks |
|---|---|
| boat (SHOTS) | the vessel is unlocked on the boat; one spot per vessel; spots on the deck; every vessel can be clicked (`ModKitBoat.ClickReport`, the game's own raycast); captures `boat`, `boat_selected` |
| start | vessel select and map select reached the host |
| 1. vessel | the right stock attack AI and `Entity_Vis_Mob_Attack`; the tooltip upgrade; stats equal the data; four build options with the right ids; the ability command; resource counter; the vessel is visible to clients (`Client.Visobj` + sprite) |
| 2. build | each of the four buildables through the real Build command; costs paid; brain started |
| N. upgrades | an upgrade is paid and changes what it says; an upgrade from a later tier is refused; a branch unlocks its next tier |
| N. Tides | a forced Tide advances; per-Tide rules fire |
| N. journal | own vessel page; it links each building page; building pages link their upgrades and units; **every** vessel page has exactly one tab per vessel |
| captures | `buildings` close-up, the vessel close-up, journal pages (vessel, a building, an upgrade, a unit, another vessel's page) |

Mechanic sections (template: Stage, Loom, costumes, units engaging, HOOK, Cross, Curtain) are replaced by your own:
one section per mechanic, each check a single measurable fact with the measured values in its detail text
(`"HOOK reels the foe toward Mummer: 2.934->0.031"`).

## 4. Captures

- `<Name>Shots.Shot(name)`: the game's own frame at `WaitForEndOfFrame`, downscaled to ≤ 960 px wide, logged as
  `[<Name>Dump] <name> <w>x<h> <part>/<parts> <base64 RGBA, bottom row first>`.
- `<Name>Shots.Close(name, target, distance)`: a temporary camera copying the game camera's settings and heading,
  tipped down steeply and placed `distance` back from `target`, rendered to a 960×540 texture (the game's camera is
  untouched). Use it for every effect on a real target, buildings together, the vessel.
- Guard close-ups of targets that may be gone (`foe != null && Alive(foe)`), or the camera films the void.

## 5. Writing good checks (lessons from the runs)

- **Stats are empty on the spawn frame**: wait (0.5 s or `Until(Statsserver.Count > 0)`) before reading.
- **Destroyed Unity objects compare equal to null** but C# references stay: keep your own "spawned" flags.
- **Client RPCs arrive next frame or later**, even on the host: poll for a debuff or effect for ~0.6 s.
- **Spawn foes as children of the enemy core** (`Helper_Entitytools.Createlinkspawnparent(core.Beingserver)`, Skin
  ""), on a standable tile; a parentless enemy throws every tick.
- **The vessel fights too**: a foe pulled next to the vessel may die to its swings before the test kills it; count the
  events you test (bonuses paid, grabs made) instead of comparing totals.
- **Full slots block spawns**: free a unit slot before testing "the next unit is X".
- Test ranges/distances with margins (tiles are integers, positions are not).
- Freeze Tides during setup; clear forced-Tide foes so they don't pile up.
- One failing check should not stop the run: every section is independent and guarded.
- **A check whose scene can't be set up is SETUP, not FAIL**: no free tile, no lane, the foe died before the check.
  Log it with `ModKitTest.Setup(name, why)`; `TEST DONE` counts it apart (`setup=N`). A FAIL is the mod's behaviour.
- **Look at the ground before placing anything**: `TDS_Mod/Tools/cloud-check/map_view.py` reads the map snapshot
  (`reference/maps/`, or the `*_map.txt` a run writes) and prints the ground around a tile or the tiles `Spot` would
  try. Search for tiles (`Spot`, `SpawnFoe`) instead of fixed offsets; a fixed offset that works on one hill fails on the
  next (Argo 1.4.0: seven PC rounds on its own test placement).
- Rerun after every fix until 0 failures; a harness failure is still a failure to fix, and so is a SETUP line.
- **Release rule** (Dimitri, 2026-10-05): a run with 0 FAIL may be released with SETUP lines when every SETUP check
  passed on the PC in an earlier run and the mod code it covers hasn't changed since. The setup fix rides along with the
  next round. A SETUP on a check that has never passed, or on changed code, still needs a run before release.

## 6. Reading Player.log

- Your lines: `[<Name>] loaded v...`, `ready: ...`, diagnostics (every being's AIs, stats, costs, tags), `icons: ...`,
  `journal: N pages, tabs added ...`, `TEST PASS|FAIL|SETUP ...`, `TEST MAP ...` (the map snapshot),
  `TEST DONE pass=N fail=M setup=K`.
- `Could not retrieve <label>/<name> The GROUP exists, but item does not`: a name you use isn't registered.
- Exceptions: group by stack trace; anything mentioning your classes is yours.
- Baseline noise (not from mods): UniTask `OperationCanceledException` in `Macro_Setupui.Mapselected` (the harness
  leaves map select early) or `Mode_Normal.Run`, NRE in `Blu2_Menu_Handler.Animin` on the way back to the boat
  after a won map (RogueWavesMode round 1), `OperationCanceledException` in `Projectile_Master.Kill` when a map ends
  with shots in the air, NRE in `Tds_Clientinfo.Newscene` on disconnect, `Bla_Ambient` NREs at
  quit, NREs in `Game_Init.OnDestroy` and `Bl_Sound.Stopgroup` after `TEST DONE` as the game shuts down (RogueLab round 8), "Empty Load List!!", a missing Music System script, DOTween safe mode, `Bla_Ui_Mods.Populate` cancel when the
  Mods menu opens, and on `Map_Monogrove` an `InvalidKeyException` and a null in `Bl_Address.Load` for two assets its
  load list names and the game doesn't ship (seen with no pack part on too; the map still loads and plays, 2026-11-04).

## 7. Release boot check

Release only after a self-test with 0 FAIL (SETUP lines: the release rule in section 5). After the release export:
launch, wait for `[<Name>] icons:` (~30-60 s after boot), then check every installed vessel
mod logged `ready` and `icons: <its> own`, and the log has **0** `Exception` lines; close the game.
