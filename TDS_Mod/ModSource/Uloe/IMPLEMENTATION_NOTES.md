# Uloe implementation notes (0.1.0-dev, 2026-11-03)

**2026-11-04, code simplification (compiled, not yet run in game):** besides the stock route below, the game's own `Entity_Ai_General_Upgrade` runs the buildings' upgrade panels again (the brain's mirror of it is gone; it still hears each upgrade through `Master.Upgrade`), and the shared helpers and asset reader moved to kit 1.8's `ModKitUtil` and `ModKitAssets` with their bodies unchanged. The self-test now has 145 checks (148 less the deleted route's three). The PC checks: `docs/reports/SIMPLIFICATION_BUILD_2026-11-04.md` §4 (T1, G1, G3, U1, L1).

**Built from the scope in one stretch: design (`DESIGN.md`), code (`Code/`), self-test (`Code/99_SelfTest.cs`), temporary art
(`Assets/UloeMod/Editor`, `Previews/0.1/ASSETS.md`).** The scaffold is Spool with Uloe's content; every Spool mechanic, sheet,
model, effect and sound is gone. The sheet is `docs/scope/10_Vessel_Uloe.txt`; the routes are `docs/reports/SCOPE_FIT_PLAN_2026-10-28.md`
§8.3, §10, §11.

## What has been checked, and how

| Check | State |
|---|---|
| Release build, `Build-UloeMod.ps1` | compiled against the game's reference list |
| `-Define ULOE_SELFTEST,ULOE_SHOTS` | compiled |
| `Assets/UloeMod/Editor` | built and exported by Unity (round 1) |
| The generators' output | run outside Unity on the kit's `Tools/cloud-check/lite/UnityLite.cs` (renders in `Previews/0.1/`): every sheet, model and sound was looked at or measured. The rest frame equals Dimitri's concept frame pixel for pixel |
| **Self-test in game** | round 1 ran (below). The fixes after it are compiled, not yet run. Nothing has been played by hand |

## Round 1 (2026-11-03, first in-game self-test): 138 pass, 8 fail, 1 setup

All eight fails and the setup were the test's. No mechanic was at fault, though the Flood Light's section never got past its first line, so **its
mechanics have still never run**.

| What the log said | Cause | Fix |
|---|---|---|
| Light still 1 after Uloe left; no skin at no Light; the skin goes; the cord snapping (setup) | The vessel never walked: the kit's `FoeTile` stays within a level of its point and this map climbs, so it found no far tile | `WalkTile`: all the ground, on any level, by the game's own path. The vessel also really walks off for the Bank's and Mirror's checks now, and back after the snap |
| A Photon was drawn to the foe | `Fights` counts only walks; the foe stood within a Photon's Range. The hit itself passed | Checks the brain took the foe as its `Target` |
| The burst hurt nothing, marked nothing | Killed a frame after the foe spawned, before its hit box reached the physics scene `Entityarea` searches (Spool round 1b learned the same) | Waits 0.3 s |
| Bounce Card's relay on A | The relay is one source: its new share replaces the old instead of adding to it. The check added them | Checks A holds at least the relay |
| Flood Light not built | The test's own node stood on a one-tile ridge and cut off the tile `Spot` chose; Build's walk gives up on a tile it can't reach | `ReachSpot` asks the path |

One decision followed from it, below. And the own route's hit hook now works on a copy of `Data` and takes a missing `Damagemulti` as 1, since it is the
default now and a caller may hand one `Data` to several foes.

## Code map

| File | What |
|---|---|
| `00_UloeMod.cs` | Boot (pack registry part "Uloe", asset lookup, content, journal), per-frame calls into the kit and the server |
| `01_Util.cs` | Uloe's own helpers: `Picked`, `IsNode`, `NearestNode`, `NodeAura`, `SegmentDistance`; stats, tiles, walking, JSON, Vis, debuff looks, `Keep`, `Front` and `Listen` are the kit's `ModKitUtil` since kit 1.8 |
| `02_Data.cs` | Every name, text and number |
| `03_Assets.cs` | The vessel's animations (twice, plain and lit), 12 px icons, tower material, sound levels; slicing, the Photon's unit sheet, the boat sprite, sounds and effects are the kit's `ModKitAssets` since kit 1.8 |
| `04_Content.cs` | Clones the hero (Smacky), the Glass (Archer), Bank and Flood Light (Mining Hut), Mirror (Wall), Photon (miner), the spark (Sno Sno's snowball), the beam (Buster's Totem beam), the commands, the four Directions, the journal |
| `05_Server.cs` | Host rules: `UloeLight` (one source's share of Light), `UloeServer` (spawn and despawn hooks, `Shine`, Flood Light paths), `UloeMarks` (Blind and Illuminate) |
| `06_HeroBrain.cs` | The vessel: arms the spark, its own Light, PLUG IN, the Directions |
| `07_Building.cs` | Base for the buildings (the game's own `Entity_Ai_General_Upgrade` runs the upgrade panel since 2026-11-04, the brain hears `Master.Upgrade`; the Light stat, Light's skin), the Flood Light |
| `08_Bank.cs` | The Power Bank and the Photon's brain (wander, visit, follow a line, fight, burst) |
| `09_Glass.cs` | The Magnifying Glass: target, beam, Light and Burning Point on Damage |
| `10_Mirror.cs` | The Mirror's front-of-chain hit hook: Blind or Illuminate the attacker, Glare; Bounce Card |
| `11_Client.cs` | Every look and sound (Vis RPCs): cords, rays, rings, the beam and the lens turning, flashes, bursts, warnings |
| `12_Commands.cs` | The pickers: PLUG IN (a node or your Bank; UNPLUG while plugged) and AIM |
| `13_Journal.cs` | Journal pages, diagnostics dump |
| `97_Shots.cs`, `99_SelfTest.cs` | Debug builds only |

## Read from the game's IL for this build (none of it seen in game yet)

- `Helper_Entitytools.Interacthurt` calls `From.Interact`, then `To.Oninteract` (so a Photon's hit hook is `Master.Interact`, as
  `Entity_Ai_Mob_Merchant.Interact` does it; `Entity_Ai_Mob_Melee` hits through `Interacthurt`).
- `Helper_Entitytools.Updateinteract` **nulls `Interact` and `Oninteract`** and rebuilds both from the entity's AIs, on every
  `Upgrade` and `Removeupgrade`. So every handler of Uloe's is said again a few times a second: `ModKitUtil.Listen` (the attacker's
  side), `ModKitUtil.Front` (the Mirror's, as Spool's Cushion; both in the kit since 1.8).
- `Entity_Ai_Effect_Barrel_Boost.Downgrade` only acts on its own `Upgradeobj`, so one instance per level would clean up after
  itself; Uloe still keeps one level on a foe at a time.
- `Entity_Ai_Effect_Tower_Merchut` adds `Effect_Multi` to `Damagemulti`, has no `Downgrade`, and puts `Debuff_Targeted` on for 9999 s
  a second after it starts: the look must be taken off by hand (`UloeMarks.TakeOffLit`).
- `Projectile_Ai_Beam` moves its transform to the hit point; each pulse costs `1 / Tickrate` of `Lifetime`, so Lifetime 0.2 at Tickrate 5
  is exactly one pulse. It stops at the first terrain unless `Collide` is 0 (Uloe sets 0: the Range decides, not a hill).
  `Projectile_Master.Interact` calls `Interacthurt(Master, To, copy of Data)`: empty Data takes the attacker's Damage stat and records
  the kill credit. The beam's line is drawn by every client (`UloeClient.BeamRun`); the projectile's own look is a small glow.
- `Bl_Address.Getload` on a missing item only logs a warning (pitfall #114's line), so a map without `Debuff_Woozy` or `Debuff_Targeted`
  loses the stock look and keeps Uloe's own.

## Decided after round 1: Blind and Illuminate take the own route (the stock route deleted 2026-11-04)

**2026-11-04:** the stock route below (`UloeData.StockMarks`, `UloeMarks.Stock`, the six `Uloe_Blind<n>`/`Uloe_Lit<n>` upgrades and
the self-test's route loop) was deleted, as `docs/reports/CODE_SIMPLIFICATION_2026-11-04.md` finding 7 proposed: it never ran in a
release build. The self-test lost three checks that read those upgrades (two content checks and "no Blind upgrade is left on the
foe"), and its Blind and Illuminate checks dropped the "own: " prefix. Git keeps the stock route if it is ever wanted again.

Round 1 logged `stock: a marked foe falling logged 2 OperationCanceledException`, `own: ... 0`, and every Blind and Illuminate check passed on both
routes. The self-test now runs only the route in use. The reasoning, from before the run:

The brief and the fit plan put Blind on the stock `Entity_Ai_Effect_Barrel_Boost` and Illuminate on `Entity_Ai_Effect_Tower_Merchut`, and
`UloeData.StockMarks = true` does that (RogueLab R1, R2 proved Barrel_Boost on a tower). Reading their IL raised a problem R1 couldn't show:

- Both wait on `UniTask.Delay(..., destroyCancellationToken)` inside an `async void`. A foe that falls destroys the AI, the delay is
  cancelled, and the cancel surfaces as a logged `OperationCanceledException` through `AsyncMethodBuilderCore.ThrowAsync` (the same line
  Player.log already shows for `Projectile_Master.Kill` when a map ends). Barrel_Boost's delay runs its whole 3-5 s, so **every foe that
  falls while Blinded logs one exception on the host**; Merchut's only for its first second. So does a stronger Blind replacing a weaker.
  It does no harm to the game (the base game's barrels do it too), but Photons Blind nearly everything they touch, so expect a lot of them.
- Each application also writes the foe's `Upgradelist` (a `NetworkList`), adds a component and rebuilds its hit chain.

So `UloeMarks.Stock` is a run-time switch with a second route, the same numbers through `Addmod(foe, "Damage", -x, Multi, seconds)`
(pitfall #120's advice for a debuff that must wear off) and a front-of-chain hook for Illuminate, with Uloe's own looks only.

## Assumptions the code makes (written before round 1; the self-test's checks passed on those it covers, the looks are still unseen)

1. **A `Light` stat by `Addstat`** on a pack building shows on the panel, with the `Light` icon. R3 proved the summing with
   `Display_Exposed` false; Uloe sets it true so the owner reads the number.
2. **The Glass's beam**: `Tower_Bustertotem_Beam` cloned with a new `Vis` fires and hits. If its `Hit` or ray settings surprise, the
   fallback is `ModKitCombat.HurtWithStat` (it already runs when the template is missing).
3. **Skins with an extra prefab** (`Lit1..3`, Yossele's Growth pattern) on the Archer-cloned Glass and the Hut-cloned buildings.
4. **The hero's Plugged skin** swapping to a second `Spriteobject`: Argo's Naut does this on a unit; on a vessel it is new. Check the
   animation doesn't hitch.
5. **`Entity_Vis_Animator_Base.Animplay("Plug", Force)`** plays the 6-frame sheet.
6. **The moving parts**: the `Lens` and `Lamp` under `Vis`, turned by the client. Check the lens faces its target (local -z) and the lamp its
   aim; a skin change remakes the visual, the lamp is found again, the lens waits for the next beam.
7. **Photons**: `Checkifstandable` and `Getpath` from a unit that just spawned on the Bank's tile; a Move with a building as its goal and a
   stop distance.
8. **The Flood Light's node reach**: 2.5 tiles from a tile centre means a 2-tile gap at most. If players find it hard to place, raise `Range`.
9. **`Upgrade` with `Priority` -3 on a foe** doesn't touch its Level or its visual (read: `Mainupgrade` takes the highest priority at or above 0).

## What the self-test can't show (left for Dimitri's play test)

It sends every Build, Sell, PLUG IN, AIM and Upgrade command itself, so the clicking is unseen: the pickers opening and their
range spheres, UNPLUG on the button, the hotkey. Also the looks: the cord sagging and following Uloe, the plug at the node, rays and rings
in URP, the beam's line, the lit skins' size, the debuff looks' height over a foe, the whole temporary art; and the balance: whether an unlit Glass
is too weak on the first Tides, whether Uloe tied to a node is a real cost.

## Directions (kit 1.7)

`UloeContent.BuildDirections` registers four Directions, one upgrade per rank (`Direction_Uloe_<Name><rank>_Upgradeobject`, `Extension`,
`Brightbulb`, `Batteries`, `Flashbulb`, ranks 1-3); the vessel's brain reads the ranks with `ModKitDirections.Rank` twice a second, and the numbers are arrays in
`UloeData` indexed by rank. With none held the numbers are the plain ones. The mode finds them by name, and its cards find their pictures by name too
(`RWAssets.Like("Direction_" + name)`): `Tools/Make-DirectionIcons.ps1` drew the four into `ModSource/RogueWavesMode/Art/Temporary/game_size`, so **the mode part has
to be rebuilt to show them**; until then the cards show the crest.

## Open in the sheet (Dimitri's)

More upgrade levels and capstones, the lore, his own sprites, models and sounds, every default in `DESIGN.md`. The mode's
"Dazzling" modifier (fit plan §5) can now read what Blind and Illuminate are: it isn't built here.
