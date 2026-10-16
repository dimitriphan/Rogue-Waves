# Base vessel survey (2026-10-01)

How the five shipped vessels are built, read from two sources:
- **Data:** an in-game dump, `vessel_data_survey.txt` (survey build: `Build-SpriggMod.ps1 -Define SPRIGG_SURVEY`).
- **Code:** the game's AI and command classes, read with Cecil.

This is the reference for making a vessel that feels like the developers made it.

## Roster

| Vessel | Title ▪ difficulty | Buildables | Hero attack | Special rule tooltip |
|---|---|---|---|---|
| Smacky | Good Ol' Fella ▪ Basic | Archer Tower, Bomb Tower, Mining Hut, Wall | melee (`Entity_Ai_Mob_Combat`) | none |
| Sno Sno | Destroyer of Worlds ▪ Intermediate | Sno Cannon, Sno Box, Ice Brick, Heater | **ranged** (`Entity_Ai_Mob_Ranged`) | 'Snoborne' |
| Boutman | Fisher Extraordinaire ▪ Advanced | Fish Barrel, Boutboat, Bouthouse (+ fish as buildable units) | melee (`Entity_Ai_Mob_Melee`) + Boutman AI | 'The Bout Way' |
| Fie | From the Walls ▪ Intermediate | Silo Pipes, Rest Stop, Knight's Keep, The Wall | melee (`Entity_Ai_Mob_Combat`) | none |
| Buster | A Very Fast Dog ▪ Advanced | Checkpoint (trap), Pedestal, Speed Pad (trap), Garage | melee + Dash command | 'Good Dog' |

## Conventions shared by every vessel

**Hero (vessel) entity**
- AIs:
  - Always `Entity_Ai_Hero_Buildsell` and `Entity_Ai_Mob_Movement`; `Entity_Ai_Mob_General` on every vessel but Buster,
    who has `Entity_Ai_Mob_Dog` in its place and adds `Entity_Ai_Mob_Movementlinear` (`vessel_data_survey.txt`, Buster
    block; `docs/17` §7).
  - Plus one generic attack AI.
  - Plus a `Entity_Ai_Hero_<Name>` AI, which creates `Buildoption1..4` (Goalstring = buildable, Goalvalue 1) in Spawn (Boutman's creates only
    `Buildoption1`, the Bouthouse; Boutbro's AI creates 1-3) and owns the vessel's special behaviour.
- Ranged heroes use the stock `Entity_Ai_Mob_Ranged`:
  - In its Starting, the hero AI sets `Commandlistserver["Attack"].Goalstring` to the projectile (Sno Sno: `Hero_Snosno_Snoball`) and adds the projectile's Data tag to `Hurtdict`.
  - The stock AI then targets Mob, Tower and Resource. Shooting ore is how heroes mine.
  - It fires with Speed and Zvelocity set to Projspeed (Accel is half of it) and plays the "Shoot" Vis.
  - The visscript `Entity_Vis_Mob_Attack` turns "Shoot" into the Attack animation.
- Hero stats:
  - Health 25–50, Speed 2–3.5, Damage 1.5–4, Range 1.5–3, Attackspeed 0.5–0.75, Buildrange 2–3, Sellrate 0.5–0.75.
  - Ranged heroes add Projspeed (15).
  - Every stat but Buster's Speeddamage has `Display_Exposed` off (the data survey's `/hidden`), which hides it only
    from a player of another team. The owner's panel shows each stat whose `Display_Type` is not None and that has
    an icon; health stats go to the bar (Game_Ui.cs:146-174, 965-982).
- Special rule: an `Upgradeproperties` named `Hero_<Name>_Tooltip_Upgradeproperties`, tier (Priority) −3, no costs, params or AIs, placed in the entity's `Upgradesinit`. Display_Name is the rule's name, Display_Desc the rule.
- Entity Display_Desc follows the pattern "Regular, single-target ranged" / "Fast, single-target melee".
- Log: Title is the name. Maindesc is one or two lines of **lore**, not mechanics.
- A hero ability is a hero command: Buster's `Command_Movelinear` is Style Button, keys [6, 7], keystate Onpressed, priority 6.

**Buildables**
- AIs:
  - `Entity_Ai_Tower_General` (towers only; traps skip it), `Entity_Ai_General_Upgrade` and one bespoke `Entity_Ai_<Type>_<Name>`.
  - The bespoke AI applies every upgrade by name in Upgrade/Downgrade via `Addmod`, reading the upgrade's params.
- Tags: Tower plus a role. Towerfighter = attacks, Gatherer = economy, and a family tag (Smackywall, Fiewall, Boutbarrel...) that the bespoke AI uses to find siblings.
- Base costs:
  - Single aura colour, 10–60 (bomb towers and the Sno Box mix two; the Boutboat costs Red 100).
  - Red = offence, Blue = defence, Yellow = economy.
- Stats use **Leveladd**, e.g. Health 25 (+25/lvl). Level = the highest applied upgrade tier, set by `Helper_Entitytools.Upgrade`.
- Upgrade trees:
  - 1–3 tier-2 roots, then curated `next` lists.
  - Branches often end early: Cannons (tier 3) and Perma-Wall (tier 4) have no next.
  - 8–12 upgrades per buildable.
  - Exactly one tier-5 capstone per line, sometimes two.
- Upgrade costs:
  - tier 2: 15–75
  - tier 3: 25–500
  - tier 4: 75–750
  - tier 5: 1500–5025, sometimes split across colours
  - (Boutman's trees go higher: 150, 700, 1950 and 7500)
- Upgrade names follow `<Being>_<Key><Tier>_Upgradeobject`. Each upgrade has its own `Ent_Visual` (`..._Visualproperties`). Descriptions are short comma phrases ("Increased damage, dealt over time").
- Param naming: `Stat_Add`, `Stat_Multi`, `Stat_Pmulti`, `X_Base`, `X_Max`, `X_Percent`, `X_Duration`. Bespoke AIs read them, and the engine never applies them on its own.
- An upgrade can carry its own AI (Smackywall Regen3 adds `Entity_Ai_Effect_Smackywall_Regen`; Fiewall Tower4 adds `Entity_Ai_Tower_Fiewall_Turret`).
- **Buffs and debuffs** are `Effect_*_Upgradeobject` upgrades applied to the target with `Helper_Entitytools.Upgrade`:
  - Each has its own effect AI. The timed ones (13 of the survey's 36 `Effect_*` upgrades) carry `Duration_Base`, which that AI reads to remove itself.
  - Visual state shows through `Radio.Vfxdebuffcheck("Debuff_*")`.
- **Ground effects** are networked beings on a tile: Sno (`Effect_Snosnosnow`, General_Effectground block, Level stat plus skins '2'..'10'), Bramble and Beacon zones (General_Effect block, `Entity_Ai_Tower_Tilebuff`).
- **Death leftovers** are trap beings: Perma-Wall leaves `Trap_Smackywall_Perma`, which heals over time and respawns the wall; Hidden Surprise leaves `Trap_Smackywall_Spikes`; Thin Ice leaves `Trap_Snosnowall_Thin`.
- Visual growth by stage is done with **skins**: Bouthouse '2'..'5' per tier, Sno '2'..'10' per Level.
- Unit towers (Mining Hut, Keep, Garage):
  - Stats Units and Respawn.
  - Units are spawned with `Link_Spawn` from the tower's block.
  - Unit upgrades mirror tower upgrades through `Link_Spawn.Preupgrades`.
- Per-Tide behaviour subscribes to `Game_Mapvars.Wave.OnValueChanged` (`Newwave(old, new)`).
- Counters such as Deathmarkkill are added with `Addstat` (Style Counter) and driven by a Base modifier.

**Engine facts that constrain mods**
- AI and vis scripts are attached by `Type.GetType(obj.name)`, so only the object's **name** matters.
  - Any game AI class can be attached to new data through a name-carrier object.
  - Mod classes can't: they resolve to nothing, and upgrade pruning compares `GetType().ToString()`.
- `Entity_Ai_General_Upgrade.Command("Upgrade")`:
  1. Available = Entity.Upgrades plus `.Upgrades` of every applied upgrade whose Priority equals Level.
  2. Throttle 0.05 s.
  3. Checkresources, then Useresources, scaled by the "Upgrade" command's Goalvalue.
  4. `Upgrade(Forced if server-issued, else Normal)`.
  5. Vfx "Upgradeborder".
  6. The owner's `Tds_Clientinfo.Activeplayerdata.Upgraded++` (end-screen stats).
- The client upgrade panel reads `Entity_Master_Client.Mainupgrade.Upgrades` (or `Entity.Upgrades`) and sends `Command_ServerRpc("Upgrade", JsonUtility.ToJson(Upgradedata{Upgrade, Upgrader}))`.
- The game's sound table (`Bl_Sound.Database`) is a private dictionary built once from `Bla_Audio.Database` on first play, so names added later are ignored. Mod sounds must be played through their own AudioSources routed to the game's mixer.

## Decisions for Sprigg

1. **Native ranged vessel, modeled on Sno Sno.**
   - AIs: Mob_General, Mob_Ranged, Mob_Movement, Hero_Buildsell, plus visscript Entity_Vis_Mob_Attack.
   - The brain sets Attack.Goalstring to `Hero_Sprigg_Seed`.
   - Remove the custom attack loop. It double-attacked: the copied melee Combat AI also hit at 3.5 range.
   - Stats are in the ranged hero band.
2. **Special rule as a tooltip upgrade:** `Hero_Sprigg_Tooltip_Upgradeproperties` 'Green Thumb' (tier −3, Upgradesinit). Log becomes lore.
3. **WATER** is a hero ability button, configured like Buster's Dash (keys 6/7, Onpressed, priority 6).
4. **Buildables:**
   - Keep the brains (the only option for mod logic). Their upgrade handling mirrors General_Upgrade exactly, including player stats, plus the Ring gate.
   - Use Sprigg family tags instead of Smacky's.
   - Upgrade trees are curated (explicit next lists, terminal branches, one capstone each), with costs in each building's colour on the base game's tier curve.
5. **Mulch is a networked ground-effect being** (`Effect_Sprigg_Mulch`, from Effect_Smackywall_Bramble), consumed by the next plant on that tile. This replaces the client-side models.
6. **Evergreen leaves a stump trap** (`Trap_Sprigghedge_Stump`, from Trap_Smackywall_Perma) that regrows the hedge next Tide, in the Perma-Wall style.
7. **Prickles uses the stock `Entity_Ai_Effect_Smackywall_Bramble`** through the upgrade's AI list (param Effect_Add).
8. Plant growth stages stay as skins, and Rings and Sap stay Counter stats. Both are native patterns.
9. Fix the bug where Sprigg-spawned plants (hedge segments, Fairy Ring caps, regrowth) count as player-planted and trigger Seed Bank refunds.
