# Mummer implementation notes (v1.0.0, 2026-10-02)

Mummer, **Strings Attached ▪ Advanced**: a hand-sewn costume vessel that sews the Tide into a cast of puppets. The
design, the balance numbers and every change from the concept (`Concept/`, the user's "Mummer Vessel Concept.zip")
are in `Mummer_DESIGN.md`. Built on the scaffolding of Sprigg and Argo; `../reference/VESSEL_SURVEY.md` covers how the
shipped vessels work.

## Layout

`Build-MummerMod.ps1` joins `Code/*.cs` (name order) and `ModSource/Shared/*.cs` into one Roslyn string, renaming the shared
kit's `ModKit` prefix to `MummerKit` (each mod gets its own copy, so mods never collide), rejects the namespaces the
game's compiler refuses, and compile-checks the result with Unity's csc against the game's own references.

| File | Role |
|---|---|
| 00_MummerMod | Boot: icons first (the game freezes its icon table on first use), wait for the template vessel, load Smacky's group plus Buster's and Sno Sno's (for the Curtain's pad block and the Aegis Understudy's mob-blocker), build, diagnostics; Update runs server, client and journal tabs; LateUpdate the boat |
| 01_Util | Helpers: positions, teams, foe queries, damage/heal, counters, skins, `Hold`/`Slow`, client effects, game debuffs by name, JSON |
| 02_Data | Every name, text, stat, costume, Thread rule, upgrade tree (explicit costs and next lists), warning text |
| 03_Assets | Asset library access: sprite sheets into Spriteobjects (vessel, three Understudy costumes), tower material, pixel icons, sounds and particles |
| 04_Content | Registers everything (Hero, Mob, Upgrade, Projectile, Command and **Vfx** lists), builds the vessel, the four buildings, Understudies, the felt bolt, HOOK and COSTUME commands, the tooltip upgrade, the hero entry |
| 05_Server | Host rules: brains on spawn, Thread per player, Scraps when foes fall near a Loom, costume unlocks, Marks, Unravel on each Tide |
| 06_HeroBrain | Vessel: build options, Thread counter, HOOK (pick, recoil shown on the button, the reel, the root and the Mark) |
| 07_Building | Shared building brain (upgrades mirror `General_Upgrade`) and the Loom |
| 08_Stage | Puppet Stage (stitching, costumes, cast), Marionette Cross (grab, hold, flail), Curtain (tangle) |
| 09_Puppet | Understudy: engages foes within its leash with the stock combat AIs, Marked foes first; Aegis self-repair, Grand Finale |
| 10_Client | All players: animations, thread lines, flying Scraps, stitch/finale/costume effects, sounds, owner-only warnings |
| 11_Commands | HOOK picker (an `Itemselect` like Sell; the icon greys while recoiling) and the COSTUME button (tinted, shows the costume and its Thread cost) |
| 12_Journal | Fills the shared journal kit's spec for Mummer; diagnostics dump |
| 97_Shots | `-Define MUMMER_SHOTS`: full-frame and close-up captures as `[MummerDump]` log lines |
| 99_SelfTest | `-Define MUMMER_SELFTEST`: starts a solo match and checks every mechanic (72 checks) |

Shared kit (`ModSource/Shared`, used by Mummer and Sprigg 1.4.1, the same design as Argo's): `ModKitJournal` (vessel-agnostic
journal pages and the vessel-tab sync) and `ModKitBoat` (a boat spot per vessel, laid out on the deck, button
unlocked).

Editor side (`Assets/MummerMod/Editor`, namespace `MummerBuild`): `MummerCharacter` + `MummerPx` (pixel art: vessel,
Understudies, UI), `MummerModels` + `MummerLowpoly` (low-poly towers in Sno Sno's and Buster's style, upgrade parts,
debuff meshes), `MummerAudio` (synthesised SFX), `MummerBuilder` (Tools > Mummer > Build All Assets), `MummerModPrefab`
(Build Mod Prefab, Park Other Mods, Export Mod, Restore Parked Mods).

## How Mummer is built

- **Vessel.** Smacky's AIs (stripped of Smacky's own) + `Entity_Ai_Mob_Combat` and `Entity_Vis_Mob_Attack`, so each
  swing plays the Peekaboo animation. Range 2: melee with reach.
- **HOOK** (keys 6/7). The foe walks itself up to 2 tiles toward Mummer on its own `Move` command (speed ×6 for 0.55 s),
  so its path stays valid and its AI walks on afterwards. Two things learned in testing:
  `Helper_Gridtools.Getblock` looks tiles up by exact position (hundredths), so the reel snaps to the tile centre and
  takes the ground's `Surface` entity; and a foe's combat AI re-targets every slow tick, so the brain re-asserts the
  reel's `Move` until it lands. Then a 0.75 s root and a Mark.
- **Debuff looks** (Marked, Held, Tangled, stun) are the game's own debuff system: prefabs registered by name under the
  `Vfx` label, sent with `Radio.Vfxdebuffcheck(name, target.Beingref.Value, seconds)`. Use the synced
  `Entity_Master.Beingref`, never `Link_Being.Beingrefparallel`: that copy is taken before the spawn fills in its
  `Worldobject`, so clients can't resolve the target and nothing appears (this also hid Sprigg's and Argo's slows).
- **Thread** lives on the server per player and shows as a counter on the vessel. Scraps: the best Loom of each owner
  within its Scrap range pays when a foe falls (`Game_Entity.Ondespawn`).
- **Understudies** are clones of Smacky's miner with `Mob_General` + `Mob_Movement` + the stock melee or ranged combat
  AI. The brain only moves them (`Move` with `Goalobject`/`Goalposition`, `Goalstring` "Idle"); the stock AIs fight.
  The Ballista costume fires `Mob_Understudy_Bolt` (a clone of the archer's arrow, `Tower_Smackyarcher_Arrow`); the
  Aegis costume stands on Sno Sno's mob-blocker block.
- **Marionette Cross** holds with modifiers (speed -100%, attack speed -95%), released on time, death or the Cross
  being destroyed; the held foe's flail is a real hit from it to its nearest neighbour.
- **Curtain** is a clone of Buster's pad (Perma-Wall's stump, `Trap_Smackywall_Perma`, if Buster's group is missing):
  walkable, so foes pass through and get tangled.
- **Buttons.** Updating a command with `Updatecommand(m, link, "", false, false)` syncs the button's
  `Goalstring`/`Goalvalue` to every player without running the command again (with `run = true` it re-runs the
  handler).

## Status

- Load: `[Mummer] ready: 7 beings, 28 upgrades, 15 effects, icons ok`, alongside Sprigg and Argo; boat spot on deck;
  journal 8/8 vessel pages with complete tabs.
- Self-test, 6 runs on 2026-10-02: run 6 passed 72/72. Failures in runs 1-3 were in the harness; run 4 caught the
  debuff reference, run 5 the reel's tile lookup.
- Release build (no defines) booted alongside Sprigg 1.4.1 and Argo 1.0.0: all three ready, own icons, 0 exceptions.
- Known cosmetic: with Double Bill the Stage's side turrets reach a little past its tile.

## Build, deploy and test

```
powershell -ExecutionPolicy Bypass -Command "& .\Build-MummerMod.ps1 [-Define MUMMER_SELFTEST,MUMMER_SHOTS]"
```
Then in Unity (or via UnityMCP): Tools > Mummer > Build Mod Prefab, Park Other Mods, Export Mod, and once the export
lands, Restore Parked Mods. (Called through UnityMCP, Export Mod times out and is retried, so it exports twice: wait for
the second pass before restoring.) After changing `Editor/*.cs`, let Unity finish recompiling before Build All Assets,
or the old builder runs.
```
powershell -ExecutionPolicy Bypass -File Deploy-Wait.ps1 [-Launch]
```
Results: `%USERPROFILE%\AppData\LocalLow\Blockage\TDS\Player.log` (`[Mummer] TEST PASS|FAIL`, `TEST DONE`);
`Tools\Decode-Dump.ps1 -Log <copy of Player.log> -Out <dir>` turns `[MummerDump]` captures into PNGs. Always rebuild
without defines before shipping.
