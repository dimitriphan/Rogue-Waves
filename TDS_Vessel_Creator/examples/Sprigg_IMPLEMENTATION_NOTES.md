# Sprigg implementation notes (v1.4.1, 2026-10-02)

Read `../reference/VESSEL_SURVEY.md` first. It records how the five shipped vessels are built and the decisions taken
for Sprigg from that study.

## Layout

All code lives in `Code/*.cs`. `Build-SpriggMod.ps1` joins the files in name order into one Roslyn string; the first
type is the entry point.

| File | Role |
|---|---|
| 00_SpriggMod | Boot: wait for the template hero, load Smacky's asset group, build content, run diagnostics |
| 01_Util | Helpers: stats and counters, skins, spawning (`SpawnPlant`, `SpawnOnTile`), `Script()` name carriers, cost and JSON parsing |
| 02_Data | Every name, text, stat, upgrade tree (explicit `next` lists and costs) and tuning number |
| 03_Assets | Asset-pack access: sprite slicing, Spriteobjects, the tower material, icons, client sound and particle playback |
| 04_Content | Builds and registers the vessel, buildables, Sproutling, Mulch, Stump, projectiles, WATER and the tooltip upgrade |
| 05_Server | Host rules: brain attachment, Tides, Mulch and Evergreen leftovers, refunds |
| 06_HeroBrain | Vessel brain: build options, arms the stock ranged attack, Sap, blooming, WATER |
| 07_PlantBrain | Plant brain: Rings, stage skins, attacks, upgrade gating (mirrors General_Upgrade), every upgrade effect |
| 10_Client | All players: Sprigg particles and sounds from Vis RPCs, owner-only warnings |
| 11_World | Boat spot and unlocking the vessel button |
| 12_Diagnostics | One-time Player.log dump of the built data |
| 13_Journal | Sprigg's journal pages (vessel, buildings, upgrades, Sproutling) cloned from Smacky's, plus Sprigg's vessel tab on every vessel page |
| 14_WaterSelect | The WATER button's plant picker (subclass of the game's `Entity_Command_Itemselect_Base`, like Sell/Reroute) |
| 96_JournalDump | `-Define SPRIGG_JOURNALDUMP`: logs the journal tree and entity heights |
| 97_ArtDump | `-Define SPRIGG_ARTDUMP`: logs every base vessel's sheets, the sound mix and boat-screen captures; `Shot(name)` captures TDS's own frame (also used by `SPRIGG_SHOTS`) |
| 98_Survey | `-Define SPRIGG_SURVEY`: dumps every base vessel's data at the main menu |
| 99_SelfTest | `-Define SPRIGG_SELFTEST`: starts a solo match by itself and checks every mechanic; add `SPRIGG_SHOTS` for boat, planting, watering and bloom captures |

## How Sprigg is built

- **Vessel.** AIs Mob_General + **Mob_Ranged** + Mob_Movement + Hero_Buildsell, built like Sno Sno; visscript Entity_Vis_Mob_Attack.
  - The hero brain sets `Attack.Goalstring = Hero_Sprigg_Seed`, and the stock AI does the targeting, ore mining and the Shoot animation.
  - Stats are in the ranged band (35 HP, Range 3, Projspeed 15).
  - The special rule is the tier −3 `Hero_Sprigg_Tooltip_Upgradeproperties` 'Green Thumb' in Upgradesinit.
- **Buildables.** Cloned from Smacky's four.
  - They keep Entity_Ai_Tower_General. The bespoke Smacky AI and General_Upgrade are replaced by `SpriggPlantBrain`, because mod classes can't be AIs.
  - Upgrade handling mirrors General_Upgrade (throttle, costs, Forced/Normal, Upgradeborder, the owner's Upgraded stat) and adds the Ring gate.
  - Each buildable has its own family tag (Spriggpod, Spriggcap, Sprigghedge, Spriggroot).
- **Upgrades.** Curated trees from SpriggData: explicit next lists, one terminal tier-4 branch each, and one capstone.
  - Params are applied by the brain.
  - Prickles carries the game's `Entity_Ai_Effect_Smackywall_Bramble` (param Effect_Add).
- **Growth.**
  - Rings and Sap are Counter stats.
  - Plant stages are skins (Seed, Sprout, Sapling, base, Elder).
  - The vessel's stages are skins (Leafy, Bud, Bloom) at Tides 10, 20 and 30.
- **Mulch** is the networked ground-effect being `Effect_Sprigg_Mulch` (cloned from Effect_Smackywall_Bramble). A rich patch uses skin "Rich". The next plant on that tile absorbs it (+1 or +2 Rings).
- **Evergreen** leaves the networked trap `Trap_Sprigghedge_Stump` (cloned from Trap_Smackywall_Perma). It regrows the hedge, with its upgrades and 1 Ring, at the next Tide.
- **Sprigg-spawned plants** (hedge segments, Fairy Ring caps, regrowth) are flagged Volunteer and never trigger Seed Bank refunds.

## Status

- Load: clean. `[Sprigg] ready ...` is followed by the diagnostics lines.
- Automated match (2026-10-01, two runs): **63 / 63 checks pass**, with no exceptions from Sprigg in Player.log.
  - Covered: native ranged setup, tooltip, stats, build options, WATER, planting through Build at 0.6× cost, Seed stage, client visibility, Tide growth (Rings, Sap, hedge segments, Volunteer flag), Ring gating, curated tree, upgrade costs, Prickles on the stock Bramble AI, Deep Roots, plants and vessel shooting, Sproutlings, Mulch as a networked entity, absorbing Mulch, the Evergreen stump and regrowth, bloom stages, Bloom WATER.
  - Expected noise: one `OperationCanceledException` in `Macro_Setupui.Mapselected`, because the harness starts the match while the map-select animation is still running.
  - The harness spawns test enemies with `Createlinkspawnparent(core)`. A parentless enemy makes `Entity_Ai_Enemy_Basic.Approachgoal` throw every tick.
- Online: v1.1 was played online by the user and worked. Every player needs the same version enabled.
- v1.2 (2026-10-02, **64 / 64**, including the boat check), after the user's feedback on v1.1:
  - **Boat select.** `Populatecharselect` locks vessels missing from the save, and the button's `Start` may already have sent `Locked` to its VFX, so only the shadow drew. `11_World.Unlock` clears `Locked` and re-runs `Start` through `SendMessage`.
  - **Sprites** follow Sno Sno, not Smacky: two rounded masses (bulb and pot), twig arms, the C7E0F2-style shade on the facing side, a half-alpha rim light (a departure: Sno Sno has no rim, only Smacky does, `docs/03` §1), eyes 4A4949/333232, a puddle, a hop Move. The splash is line-art (translucent fill, 3px accent outline and an inset body-colour line); Readyup has white corner brackets; minis are about 7×10.
  - **Towers** are low-poly meshes from `SpriggLowpoly.Lp` (bevelled blocks, tubes, domes, folded leaves, flat-shaded with the 8×8 palette), 40–600 triangles each, with no voxels.
  - **Watering.** A 6-frame Water animation (the can tilts, drops fall, happy eyes) played with `Animplay(anim, Force)`. `Animplaysoft` left the priority unchanged, so Idle and Move replaced it at once. Watered plants squash and stretch (`SpriggClient.Bounce`).
  - **Audio.** Base world SFX measure about 0.004 (shots) to 0.015 (landings) in rms × volume; Sprigg's were about 20× louder and on the Effect mixer. `SpriggData.SoundVolume` now matches each clip to that band, and `SpriggAssets.Mixer()` uses the mixer group of the game's `Upgrade` sound (General_Main).
- v1.2.1: `Bla_Mods.Activateprefabs` instantiates the mod prefab, library included, at startup. The library's Sprites holder was active, so its SpriteRenderers drew Sprigg's sheets at the world origin on the startup screen. Every holder (Sprites, Models, VFX, Audio) is now saved inactive; `Transform.Find` and `GetComponent` still reach them.
- v1.3.0 (2026-10-02, **72 / 72**), after the user's notes on v1.2:
  - **Thornroot** replaces the Seedpod (same id `Tower_Spriggpod` and upgrade keys; new names, models and attack). Its old seed turret never turned to aim because the stock archer AI that did that was replaced by the brain. Now `SpriggPlantBrain.Strike` sends `Sprigg_Rootdig`, then after `ThornDelay` deals `Hurt` directly and sends `Sprigg_Thorn`. Clients puff dirt along the path and pop a `Sprigg_Thornspike*` model (variant per upgrade) out of the ground.
  - **Spores.** The bomb projectile used to explode (radius 1) wherever it collided, often on the enemy's body. The cloud was drawn at radius 0.4 as a flat ring at mid-height. The projectile is now cosmetic (interaction `Sprigg_Spore`, Radius 0). `Spores()` deals the damage at the landing point over `SporeRadius()` and sends `Sprigg_Sporecloud`. Clients draw a dome plus a ring scaled to that radius (`ParticleSystemScalingMode.Shape`) at ground level, which is the entity position minus 0.5.
  - **Unity's Hemisphere particle shape faces +Z.** Every earlier "Hemisphere" burst sprayed sideways. `SpriggBuilder.HemisphereUp()` probes it at build time and rotates the shape up.
  - **WATER picker.** `SpriggWaterSelect` overrides Visobjs, Running, Send and Finish. It uses Sell's border and prompt (`Sellborder`, `Ui_Command_Sell`), because Fie's Reroute assets only load when Fie is in the match. It sends `{"Ref":<NetworkObjectId>}`. The hotkey (`Keybindable`) sends the plant under the cursor, or "" for the nearest thirsty one. The hero brain validates owner, range (3.2), thirst and Sap. Clients play the can, then a `World_Sprigg_Waterstream` arc; the plant splashes and bounces when it lands.
  - **Journal.** Hero pages are Logobjects whose `Elements` are `[SerializeReference]` Logitems. `Object.Instantiate` deep-copies them, so pages are cloned from Smacky's and edited: Image3D, TextImageBeing, Stats, Drops, LinkUpgrade and TextUpgradeParam. Extra TextUpgradeParam lines come from re-instantiating the template, because their layout fields use UI types the mod cannot reference. The vessel tabs are a Grid of `Link{Sidelink}`. Tab art is `Thumbnail`: a 32×32 white 1 px line icon, `Spriggtabicon`.
  - **Sprite.** A dopey face: eyes wider apart and pushed to the facing side, the far eye 1 px lower, a slack mouth. The splash has an open mouth with a tongue.
- v1.4.0 (2026-10-02): the journal and the boat moved to the shared kit in `../Shared` (`ModKitJournal`, `ModKitBoat`;
  `Build-SpriggMod.ps1` joins them in with the prefix renamed to `SpriggKit`), the same design as Argo's
  `VesselJournal`/`VesselTabs`. `13_Journal` only fills the spec (rule banner, idle portrait, Sproutling unit page).
  Extra boat spots are laid out on the deck. Icons are added at boot and checked 8 s later (the game freezes its icon
  table on first use). Tools > Sprigg > Park Other Mods / Restore Parked Mods park every other `<Name>Mod` folder
  under `Assets/_Parked/Editor` for the export.
- v1.4.1 (2026-10-02): the slow debuff look (`Debuff_Slow`) now appears. It was sent with
  `Link_Being.Beingrefparallel`, a copy taken before the spawn fills in its `Worldobject`, so clients never found the
  target; it now uses `Entity_Master.Beingref.Value`, as the game's own debuffs do. Release build booted with Argo 1.0
  and Mummer 1.0: ready, own icons, 0 exceptions.
- In-game visual checks without desktop screenshots: `SpriggArtDump.Shot` reads TDS's own frame at WaitForEndOfFrame and logs base64 chunks (`[SpriggDump] name WxH i/n`); `Tools\Decode-Dump.ps1 -Log <Player.log> -Out <dir>` turns them into PNGs.

## Build, deploy and test

```
powershell -ExecutionPolicy Bypass -Command "& .\Build-SpriggMod.ps1 [-Define SPRIGG_SELFTEST,SPRIGG_SHOTS]"
```
Then, in Unity (or via UnityMCP), run Tools › Sprigg › Build Mod Prefab, Park Other Mods, Export Mod, and once the
export has landed (through UnityMCP it times out and runs twice; wait for the second pass), Restore Parked Mods.
```
powershell -ExecutionPolicy Bypass -File Deploy-Wait.ps1 -Launch
```
Results go to `%USERPROFILE%\AppData\LocalLow\Blockage\TDS\Player.log`. Always rebuild without defines before shipping.
