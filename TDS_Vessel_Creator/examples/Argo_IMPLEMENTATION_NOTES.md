# Argo implementation notes (v1.0.0, 2026-10-02)

Argo, **Fathoms Below ▪ Intermediate**: a bulky deep-sea diver (built in the spirit of League's Nautilus) with a
crew of Nauts, a scatter cannon, a harpoon tower and a drill that sinks deeper every Tide. Built from the concept in
`Concept/` (the user's "Argo Vessel Concept.zip") on Sprigg's proven scaffolding. Read
`../Sprigg/Research/VESSEL_SURVEY.md` for how the shipped vessels are built.

## Layout

All code lives in `Code/*.cs`. `Build-ArgoMod.ps1` joins the files in name order into one Roslyn string; the first
type is the entry point.

| File | Role |
|---|---|
| 00_ArgoMod | Boot: add icons early, wait for the template hero, load Smacky's asset group, build content, diagnostics; keeps the journal's vessel tabs in sync |
| 01_Util | Helpers: stats and counters, skins, queries, JSON parsing, `Root`/`Slow`, `Hit()` damage dictionaries |
| 02_Data | Every name, text, stat, upgrade tree (explicit `next` lists and costs), tuning number and sound target |
| 03_Assets | Asset-pack access: sprite slicing, Spriteobjects (vessel, Nauts and their Tune skin), tower material, icons, auto-levelled sounds, particles |
| 04_Content | Builds and registers the vessel, buildables (stats from data), Nauts, projectiles, SONAR and crew commands, the tooltip upgrade |
| 05_Server | Host rules: brain attachment, Tides, the Naut roster ("one Naut per tower") |
| 06_HeroBrain | Vessel: build options, SONAR (cooldown shown on the button), the second hit of each swing |
| 07_TowerBrain | Scatter volleys, harpoon lines, the Diving Bell's crew, the Deep Drill (mining, Fathoms, Tremors, quakes), upgrades (mirrors General_Upgrade) |
| 08_NautBrain | A Naut walks to its tower with the stock movement AI, tunes it, repairs it, comes home |
| 09_Hooks | Pinged (+20% damage taken) and Bulkhead (-25%) via the target's hit event |
| 10_Client | All players: turret aiming and recoil, drill spin, Tremor rings, tuning marks, SONAR ping marks, effects, sounds, owner warnings |
| 11_World | Boat spot (counts every vessel, so several vessel mods agree) and unlocking the button |
| 12_Diagnostics | One-time Player.log dump of the built data |
| 13_Journal | **Generic** `VesselJournal` builder + `VesselTabs` sync, and `ArgoJournal` which fills a spec for Argo |
| 14_CrewSelect | PORT / STARBOARD / BOSUN: pick any tower on your team (Itemselect, like Sell/Reroute); pick the bell to recall |
| 15_SonarButton | SONAR's button: `Entity_Command_Simple` that greys out while recharging |
| 97_Shot | `-Define ARGO_SHOTS`: full-frame and close-up (own camera) captures as `[ArgoDump]` log lines |
| 99_SelfTest | `-Define ARGO_SELFTEST`: starts a solo match and checks every mechanic (64 checks) |

Editor side (`Assets/ArgoMod/Editor`, namespace `ArgoBuild`): `ArgoCharacter` (pixel art), `ArgoModels` +
`ArgoLowpoly` (towers), `ArgoAudio` (synthesised SFX), `ArgoBuilder` (Tools > Argo > Build All Assets, Preview
Models), `ArgoPreview` (Preview Sprites), `ArgoModPrefab` (Build Mod Prefab, Export Mod, Restore Parked Mods).

## How Argo is built

- **Vessel.** Smacky's AIs (Mob_General, Mob_Movement, Mob_Combat, Hero_Buildsell) + visscript Entity_Vis_Mob_Attack
  (turns each swing's "Attack" into the wrench animation). Stats: 65 HP, speed 2.25, damage 5, range 1.75, attack
  speed 0.4. **Second hit:** `Helper_Entitytools.Interacthurt` fires the attacker's `Entity_Master.Interact` before the
  target takes the hit, so the brain carries 60% of each swing to the nearest other enemy (flagged `Argocleave`).
- **SONAR** (keys 6/7 like Buster's Dash): ping radius 6, Pinged 5 s, 15 s cooldown. The button's Goalstring is "Off"
  while recharging (synced through `Entity_Master.Commandlist`, the way the game's on/off toggles work).
- **Pinged / Bulkhead.** `ArgoHooks` puts its handler first in the target's `Oninteract` chain and scales
  `Damagemulti` on a *copy* of the hit's Data (projectiles reuse one dictionary for every pierce).
  `Helper_Entitytools.Updateinteract` rebuilds the chains on every upgrade or effect, so handlers are re-checked
  every 0.1 s (targets) / 0.25 s (towers, vessel).
- **Scatter Cannon / Harpoon Tower** fire the game's own projectiles (clones of the Heavy Bow arrow,
  `Projectile_Ai_Simple`, native `Pierce`). Each shot passes *every* parameter: the game merges a shot's parameters into
  the Projectileobject's shared `Defaultparams`. Clients swing the model's `Turret` part toward each target
  (`Argo_Scatter`/`Argo_Harpoon` Vis) - the bespoke turret AIs are gone, so aiming is ours.
- **Deep Drill** bites ore with the game's own ore damage (`Interacthurt` on the nearest `Resource` within 2.1 tiles,
  like the Sno Box), so the aura it digs out goes to the team exactly as mining does. Fathom 1 -> 3 (5 with Deep
  Core), one per Tide: +50% bite and +0.5 tiles of Tremor per Fathom; skins `Fathom2..5` add the pipe-stack overlay.
  Its `Range` stat *is* the Tremor radius, so selecting it shows the ring; clients draw a dust ring at the same radius
  (rebuilt from the skin and upgrade list, test: "client Tremor radius matches the server's").
- **Diving Bell / Nauts.** Nauts are clones of Smacky's Engineer with only Mob_General + Mob_Movement. The brain sends
  them like the Mining Hut sends miners: `Move` command with `Goalobject` = the tower, `Goalposition` = -1, then
  `Updatecommand(..., true, true)`. A Naut within 1.75 tiles tunes: +20% attack speed (Full Crew +40%), heal 0.5/s
  (+2 with Overhaul), +1 range (Calibrate), -25% damage taken (Bulkhead), skin `Tune` (wrench-tapping Idle). One Naut
  per tower across every bell; picking the bell recalls; knocked out = back in 8 s and resumes its post. Full Crew
  "dives": despawn, respawn on a standable tile beside the post.
- **Icons.** The game copies its icon sets into a private table on first use and never again. Argo adds its icons
  the moment the database exists (before any asset loading) and checks 8 s after boot; if the table was already
  frozen, the buttons fall back to stock icons (never blank).

## The journal, for every future vessel

`13_Journal.cs` is vessel-agnostic:

- `VesselJournal` takes a `VesselJournalSpec` (hero id, template vessel, rule upgrade, lore, portrait, tab icon,
  accent colour, buildings with their upgrade trees and units, extra EFFECTS labels) and builds the full page set by
  cloning the template vessel's pages: vessel page (idle portrait like `Smacky4-Sheet_0`, the rule banner where Sno
  Sno's sits, stats, lore, TOWERS & UNITS for any number of buildings), building pages (RELATED ITEMS only for unit
  towers), upgrade pages (EFFECTS, NEXT UPGRADES) and unit pages.
- `VesselTabs.Sync()` keeps one tab per vessel in the Hero folder on every page with a vessel-tab row: base vessels
  first in the game's order (Smacky, Sno Sno, Buster, Boutman, Fie), then modded vessels by id; it adds missing tabs,
  drops duplicates and sorts. It is idempotent and order-independent, so every vessel mod can run it (Argo runs it at
  build and every 3 s). Verified with Sprigg 1.3 installed: 7 pages, identical tabs, no duplicates.
- For a new vessel: copy `13_Journal.cs`, rename `ArgoJournal` and fill the spec from its own data. Rename the two
  generic classes per mod if you want (each mod compiles into its own assembly, so the names do not collide).

## Status

- Load: clean (`[Argo] ready ...`, icons Argo's own, journal 36 pages) alongside Sprigg 1.3.
- Self-test run 2 (2026-10-02): 63/64 (the miss was the harness reading an enemy's speed on its spawn frame; fixed).
- Tower sizes were matched to the shipped towers measured in game (Sno Sno 0.75-1.15, Buster 0.85/1.9, Smacky 1.0-2.05):
  scale baked into the meshes about the ground point (`ArgoModels.*Scale`).

## Build, deploy and test

```
powershell -ExecutionPolicy Bypass -Command "& .\Build-ArgoMod.ps1 [-Define ARGO_SELFTEST,ARGO_SHOTS]"
```
Then in Unity (or via UnityMCP): Tools > Argo > Build Mod Prefab, Tools > Argo > Export Mod, and after the export
lands, Tools > Argo > Restore Parked Mods (the export parks other `<Name>Mod` asset folders under
`Assets/_Parked/Editor/` because ModTool exports every prefab and ScriptableObject outside `/Editor/` paths).
```
powershell -ExecutionPolicy Bypass -File Deploy-Wait.ps1 [-Launch]
```
Results go to `%USERPROFILE%\AppData\LocalLow\Blockage\TDS\Player.log` (`[Argo] TEST PASS|FAIL`, `TEST DONE`);
`Tools\Decode-Dump.ps1 -Log <copy of Player.log> -Out <dir>` turns the `[ArgoDump]` captures into PNGs.
Always rebuild without defines before shipping. **Sprigg's own Export Mod does not park ArgoMod**: park it (move
`Assets/ArgoMod` under `Assets/_Parked/Editor/`) before exporting Sprigg again.
