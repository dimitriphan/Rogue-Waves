# Engine study 2026-10-28: the stock behaviour catalogue

The appendix to [`ENGINE_STUDY_2026-10-28.md`](ENGINE_STUDY_2026-10-28.md) §13: every behaviour script of *TDS - Tower
Defense Strategy* that a pack thing could carry, what it does and whether the pack can reuse it. Read from the game's
code as decompiled in this thread (kit `docs/13` §11 recipe); the decompiled files are never committed, and nothing
here quotes them. Nothing here was run in game: every line is **read**, in the kit's sense.

The full entries (what each script does, how it is attached, the params and stats it reads, what it hard-codes, its
hooks, its quirks and its source lines) are in four files, one per group:

| File | Group | Scripts |
| --- | --- | --- |
| [`engine_study_2026-10-28/A_cardinals.md`](engine_study_2026-10-28/A_cardinals.md) | Cardinals: `Entity_Ai_Artifact_*`, `Entity_Ai_Effect_Artifact_*` | 53 |
| [`engine_study_2026-10-28/B_effects_enemies_cores.md`](engine_study_2026-10-28/B_effects_enemies_cores.md) | Effects, enemies and elites, cores and map brains, general AIs, ore | 83 |
| [`engine_study_2026-10-28/C_units_towers_projectiles.md`](engine_study_2026-10-28/C_units_towers_projectiles.md) | Units, towers, traps, projectiles, three code helpers | 52 |
| [`engine_study_2026-10-28/D_commands_visuals.md`](engine_study_2026-10-28/D_commands_visuals.md) | Buttons (`Entity_Command_*`), visual scripts (`Entity_Vis_*`), effect helpers (`Vfx_*`) | 96 |

Each file opens with the facts that hold across its group (how a Cardinal reaches the core, how upgrades start, how
commands reach the host) and ends with its "Most reusable for a mod" list, copied in §4 below.

## 1. How it was checked

Four readers took one group each and read every file of it. The entries were then checked against the code in
samples, and every fact the engine study (§14) or the fit plan leans on was read again line by line:

- the upgrade path: the same upgrade twice, removal by script name, the Cardinal argument (`Helper_Entitytools`);
- `30 / Totalwaves` in Coin, Caffeine, Moneybag, Opalcore, Rustypipe and Voice;
- Chrono's starting speed of 1.25, Melting's missing `Downgrade`, the shooter's despawn killing its shots;
- the easier setting's guard and its end-screen flag, `Give` on a colour not held yet;
- Vis "Fall" against `Effect_Chute`'s "Herofall", Boutman's foe look writing its unlock, Teamapply's spawn filter;
- no `Random` in any `Entity_Ai_*` file.

Three corrections came out of the checks; the catalogue files carry them:

| What | Was | Now |
| --- | --- | --- |
| `Entity_Ai_Artifact_Teamapply` / `_Voiceapply` | read by the fit plan's first draft as "a Direction on one player's builds" | they reach **every** vessel's buildings (the parent need only be tagged `Hero`; the team check compares the holder with itself). Fit for a team Cardinal; per-player Directions use the pack's owner-filtered hook (fit plan §3) |
| `Entity_Ai_Effect_Artifact_Opalcore` as plain thorns | "thorns plus growing HP" | it also adds a `Damage` stat, warns "BASIN GROWS STRONGER" and sets skin `Sub`; plain thorns come from `Entity_Ai_Effect_Smackywall_Bramble` |
| Chrono's ×1.25 and the pack's Quickening | could have sped up a pack map | the mode writes `Time_Scale` itself (RogueWavesMode `07_Weather.cs`), so only the stock Chrono map runs at 1.25 |

## 2. How to read the reuse classes

- **data-only**: list the script in pack data (an upgrade's `Aiscripts`, an entity's AIs, a command's functions) and
  give it pack numbers in `Defaultparams`. Nothing else is needed.
- **data + stock asset**: as data-only, but the script also loads a stock look, shot, being or upgrade by name. That
  asset must be on the map (stock looks load per map, pitfall #114) or the pack must carry one under the same name.
- **vessel-bound**: the script names a base vessel's things or tags (Sno Sno's snow, Boutman's rallies and fish, Fie's
  shop and wall buffs, Smacky's split bombs). It works only beside that vessel.
- **no**: a base class, a code helper (mod code may call it), a map-bound brain, or a script that writes the game's
  own unlock keys. The engine study's §13 count puts these together.

## 3. Index, one line per script

The "Does" column is the entry's first sentence, cut; read the full entry before using a script.

### 3.1 Cardinals (53; full entries in [`engine_study_2026-10-28/A_cardinals.md`](engine_study_2026-10-28/A_cardinals.md))

| Script | Reuse | Does |
| --- | --- | --- |
| `Entity_Ai_Artifact_Bottomrock` | data + stock asset | Starting sets the map-wide `Resourcegainmultiplier` = `Gainmultiplier` and `Resourcelossmultiplier` = 0, so every spend ... |
| `Entity_Ai_Artifact_Bottomrock_Effect` | data-only | Counts every same-team `Mob`/`Tower`/`Trap` entity whose `Costs.Count > 0` into `Bottomrock.Towers[owner]`. |
| `Entity_Ai_Artifact_Build` | data + stock asset | Gives the holder a `Buildoption4` command (from `Command_Buildoption4`) to build the being named by the first ... |
| `Entity_Ai_Artifact_Checklist` | data + stock asset | Whenever an entity tagged `Enemy` is killed, it spawns a `Special_Aurapickup` (forced spawn) on that tile, parented to ... |
| `Entity_Ai_Artifact_Checklist_Special_Pickup` | data-only | A pickup. |
| `Entity_Ai_Artifact_Chimera` | data + stock asset | At Starting it upgrades every `Resource` (ore) entity on the map with `Effect_Artifact_Chimera_Upgradeproperties` and ... |
| `Entity_Ai_Artifact_Coin` | data-only | At Starting it gives `Red_Base`/`Blue_Base`/`Yellow_Base` aura, split among active players. |
| `Entity_Ai_Artifact_Computer` | data-only | Every 1 s it checks for enemy `Mob`/`Tower` within `Range` (or `Range_Base`) and within `Range_Base2`. |
| `Entity_Ai_Artifact_Coreapply` | data-only | At Spawn it applies every Defaultparams key that names an Upgrade asset to every `Core`-tagged entity. |
| `Entity_Ai_Artifact_Fragment_Effect` | data + stock asset | At Spawn it spawns `Tower_Artriominibarrier` (Team 1, no parent) on every `Surface` tile of the radius-2 square ring ... |
| `Entity_Ai_Artifact_Gavel` | data + stock asset | Every same-team `Mob` spawn gets `Effect_Artifact_Gavel_Upgradeproperties`. |
| `Entity_Ai_Artifact_Greatspear` | data + stock asset | Every spawn of the being `Enemy_Spear` gets `Artifact_Greatspear_Effect_Upgradeproperties` through its init list, and ... |
| `Entity_Ai_Artifact_Greatspear_Effect` | data-only | A permanent Speed Multi mod of `Speed_Multi`, plus the debuff look `Debuff_Greatspear` for 99999 s, applied 1 s after ... |
| `Entity_Ai_Artifact_Heroapply` | data-only | When a child tagged `Hero` starts, it applies every Defaultparams key as an Upgrade asset to that vessel. |
| `Entity_Ai_Artifact_Heroapply_Delay` | data-only | Like Heroapply, but each key's upgrade lands after `value` seconds of real time (`UniTask.Delay(..., ignoreTimeScale: ... |
| `Entity_Ai_Artifact_Mobapply` | data-only | Every spawn whose entity tags include `Mob` gets every Defaultparams key that resolves to an Upgrade, through its init ... |
| `Entity_Ai_Artifact_Moneybag` | data + stock asset | Every `Tower` or `Mob` whose parent is tagged `Hero` (that is, built directly by a vessel) gets ... |
| `Entity_Ai_Artifact_Moneybagold` | no | Meant to give each vessel `Effect_Artifact_Moneybag_Upgradeproperties` from `Child`, and bumps the counter on sells. |
| `Entity_Ai_Artifact_Mooncore` | data-only | Whenever `Entry` changes and `Wave+1` is 3 or a multiple of 7, it adds 2 to `Entry` and 1 to `Wave` (at most once per ... |
| `Entity_Ai_Artifact_Onetimebuild` | data + stock asset | Creates `Buildoption<Buildslot>` for the being named by the non-`Buildslot` key (value = cost scale), plus `Buildrange` ... |
| `Entity_Ai_Artifact_Onetimebuild2` | data + stock asset | An empty subclass of `Onetimebuild`, so a second one-time build can be named under a distinct script name (same ... |
| `Entity_Ai_Artifact_Onetimebuild3` | data + stock asset | A third empty subclass of `Onetimebuild`. |
| `Entity_Ai_Artifact_Paint` | data + stock asset | Every spawn tagged `Enemy` gets `Effect_Artifact_Bluepaint_Upgradeproperties` through its init list. |
| `Entity_Ai_Artifact_Paint_Effect` | data + stock asset | Once the holder's health fraction is at or below `Health_Percent` (a 0-1 value), it starts a timer of `Duration_Base + ... |
| `Entity_Ai_Artifact_Samedayshipping` | data-only | Between tides, from tide 1 on, whenever `Time_Wave` is above `Time_Base` and below 60, it clamps `Time_Wave` to ... |
| `Entity_Ai_Artifact_Seeds` | data + stock asset | Same pattern as Chimera, with `Effect_Artifact_Seeds_Upgradeproperties`: all `Resource` entities at Starting plus later ... |
| `Entity_Ai_Artifact_Teamapply` | data-only | Everything a vessel builds directly (a `Tower` or `Mob` whose parent is tagged `Hero`) gets the **first** Defaultparams ... |
| `Entity_Ai_Artifact_Textbook` | data + stock asset | Every `Enemy_Dagger_Entity` spawn is turned into the being `Enemy_Trainingdagger_Artifact`. |
| `Entity_Ai_Artifact_Towerapply` | data-only | Every spawn whose entity tags include `Tower` gets every Defaultparams key that resolves to an Upgrade. |
| `Entity_Ai_Artifact_Vintage` | data-only | Each time the display state enters `Between` (with a 5 s cooldown), after 0.5 s it adds `Timeinit_Base` the first time, ... |
| `Entity_Ai_Artifact_Voiceapply` | data-only | Like Teamapply, and also covers `Trap`: everything a vessel builds directly gets the first Defaultparams key that ... |
| `Entity_Ai_Artifact_Wavebreaker` | data-only | Every 1 s, each enemy `Mob`/`Tower` within the holder's `Range` gets a Health Multi mod of `Debuff_Base`, which lowers ... |
| `Entity_Ai_Effect_Artifact_Beastpelt` | data-only | Every 1 s it sets `Option_Targetable = false` on the holder and shows `Debuff_Elusive` for 3 s. |
| `Entity_Ai_Effect_Artifact_Bluepaintcharm` | data-only | Charms the holder. |
| `Entity_Ai_Effect_Artifact_Blurryphoto` | vessel-bound | On a vessel without the `Clone` tag, if a Hero-group asset with the vessel's being name exists, it spawns a clone of ... |
| `Entity_Ai_Effect_Artifact_Buff_Rustypipe` | data-only | Multi mods on `Health` (`Health_Multi`) and `Damage` (`Damage_Multi`). |
| `Entity_Ai_Effect_Artifact_Caffeine` | data-only | Base mods on `Speed`, `Damage`, `Attackspeed` and `Sellrate` equal to `*_Base × Wave`. |
| `Entity_Ai_Effect_Artifact_Chimera` | data-only | On an ore entity, it merges the mine stats `Redauramine`, `Blueauramine` and `Yellowauramine`. |
| `Entity_Ai_Effect_Artifact_Diorama` | data-only | Permanent Pmulti mods on `Speed`, `Damage`, `Attackspeed` and `Health`, an explosion vfx, and a skin set to the being ... |
| `Entity_Ai_Effect_Artifact_Drinkstand` | data-only | A Base mod of `Damage_Base` on Damage that lasts while the holder carries a `Drinkbuff` modifier; |
| `Entity_Ai_Effect_Artifact_Glasscore` | data-only | Turns a sub-core into a glass core, with priority 9 so it runs before the general AI. |
| `Entity_Ai_Effect_Artifact_Loop` | data-only | The first Hurt on the holder despawns (without death) every enemy `Mob`/`Tower` on the map, sets `Entry -= 2`, `Wave -= ... |
| `Entity_Ai_Effect_Artifact_Moneybag` | data-only | Each Wave change adds `Rate_Base × (30/Totalwaves)` to a `Sellvalue` counter stat and `Rate_Base/100 × (30/Totalwaves)` ... |
| `Entity_Ai_Effect_Artifact_Moneybagold` | data-only | An Add mod on `Sellrate` set to `lerp(Startrate_Base, Endrate_Base, Wave/30) / 100`, updated each tide. |
| `Entity_Ai_Effect_Artifact_Opalcore` | data-only | Thorns. |
| `Entity_Ai_Effect_Artifact_Paper` | data + stock asset | At Starting it records which of the vessel's `Buildoption1`-`4` targets are tagged `Towerfighter`. |
| `Entity_Ai_Effect_Artifact_Paper_Tower` | data-only | Multi mods on `Health`, `Speed`, `Damage` and `Attackspeed` (`*_Multi`). |
| `Entity_Ai_Effect_Artifact_Pig` | data + stock asset | Adds a `Coinbag` command. |
| `Entity_Ai_Effect_Artifact_Rustypipe` | data + stock asset | An `Age` counter starts at `ceil(Wave_Base / (30/Totalwaves))` and drops by 1 on each Wave change. |
| `Entity_Ai_Effect_Artifact_Slippers` | data-only | While the holder's `State` contains "Move", a Base Speed mod grows by `Speed_Base` per second, showing ... |
| `Entity_Ai_Effect_Artifact_Snare` | data-only | On attach it freezes the holder (Speed Override 0.01) for `clamp(Duration_Base - currentHP × Durationperhealth_Base, ... |
| `Entity_Ai_Effect_Artifact_Titan` | data-only | A Pmulti Health mod of `Health_Pmulti`. |
| `Entity_Ai_Effect_Artifact_Voice` | data-only | Each Wave change lowers the holder's upgrade-cost multiplier (`Entity_Ai_General_Upgrade.Upgradecommand.Goalvalue`, ... |

### 3.2 Effects, enemies, cores, map objects, resources, general (83; full entries in [`engine_study_2026-10-28/B_effects_enemies_cores.md`](engine_study_2026-10-28/B_effects_enemies_cores.md))

| Script | Reuse | Does |
| --- | --- | --- |
| `Entity_Ai_Effect_Barrel_Boost` | data-only | Timed four-stat change: adds Health Pmulti and Speed/Attackspeed/Damage Multi, shows `Debuff_Woozy` |
| `Entity_Ai_Effect_Boutmanbucket_Boost` | data-only | Timed invulnerability: at priority 8 (before damage at 10) sets `Damage` and `Damagemulti` of every |
| `Entity_Ai_Effect_Boutmanfamily_Boost` | data-only | Branches on holder tags. |
| `Entity_Ai_Effect_Boutmanfish_Bass` | data-only | Execute mark: when the holder is hurt while `Health.Value / Value_Calc <= Percent_Base`, adds 999999 to |
| `Entity_Ai_Effect_Boutmanfish_Bassboost` | data-only | Blink movement: on the holder's `Move` action, picks the target tile (Surface nearest the goal position |
| `Entity_Ai_Effect_Boutmanfish_Caviarbuff` | data-only | One-shot payout: on command `Mine` (fired by `Resource_Mine`/`Resource_Opal` when their ore is depleted) |
| `Entity_Ai_Effect_Boutmanfish_Puffer` | data-only | Poison: every 0.2 s the holder hurts itself for `Percent_Base` % of its CURRENT Health (Data `Poison`=1); |
| `Entity_Ai_Effect_Boutmanmine_Boost` | vessel-bound | While the holder's `Following` stat > 0, adds `Damageresource = Resourcedamage_Base` to each outgoing Hurt |
| `Entity_Ai_Effect_Boutmanrecruit_Boost` | vessel-bound | While `Following` > 0: Range, Attackspeed, Damage Pmulti; |
| `Entity_Ai_Effect_Boutmanspawnbed_Boost` | data-only | Timed Damage Multi `Damage_Multi` for `Duration_Base` s, then removes itself. |
| `Entity_Ai_Effect_Brittle` | data + stock asset | Tile trigger: subscribes to its tile's `Updatefunct`; |
| `Entity_Ai_Effect_Bustercheckpoint_Attackaura` | data-only | Damage Multi from Spawn; |
| `Entity_Ai_Effect_Busterdog_Towerboost` | data-only | Timed Damage and Range Multi for `Duration_Base` s, then removes itself. |
| `Entity_Ai_Effect_Busterpad_Collision` | data-only | Ram: on the holder's first outgoing Hurt, hits every other-team Mob/Tower within `Range_Base` for |
| `Entity_Ai_Effect_Busterpad_Speed` | data-only | Speed boost fed by interacts: incoming interact Type `Speed` adds Data `Speed` (clamped to `Maxspeed`) to an |
| `Entity_Ai_Effect_Busterpad_Towerbuff` | data-only | Permanent Attackspeed Pmulti until the upgrade is removed. |
| `Entity_Ai_Effect_Bustertotem_Blaze` | data-only | Ramping fuse: creates exposed stat `Blaze` (counter, max `Damage_Max`) starting at `Damage_Base`; |
| `Entity_Ai_Effect_Calamari3` | vessel-bound | Thorns: each incoming Hurt without `Spike` hits the attacker for `Damage_Base` (tagged `Spike`); |
| `Entity_Ai_Effect_Chute` | data-only | Speed Override `Speed_Override` until removed, and plays the holder's `Herofall` visual. |
| `Entity_Ai_Effect_Enemy_Weaken` | data-only | Stacking melt: creates stat `Enemyweaken` (stack count, starts 1); |
| `Entity_Ai_Effect_Fiewall_Protection` | vessel-bound | At priority 9 multiplies incoming Hurt `Damagemulti` by `1 - Damage_Resist`; |
| `Entity_Ai_Effect_Fiewall_Regen` | data-only | At priority 9 divides incoming Hurt `Damagemulti` by `1 + Resistadd` (holder stat). |
| `Entity_Ai_Effect_Fiewall_True` | data + stock asset | Repel: when an attacker without the `Trueeffect` flag hits the holder, flags it for 3 s, spends one |
| `Entity_Ai_Effect_Fiewall_Weak` | data-only | Speed and Damage Multi until removed. |
| `Entity_Ai_Effect_Killonspawn` | data-only | Despawns the holder (Death true, reason `Self`) `Delay_Base` s after Starting. |
| `Entity_Ai_Effect_Ramen3` | vessel-bound | Self-heal `Heal_Base` every 1 s (`Armorhealmulti` 0.05) while the holder has a `Shopbuff` modifier. |
| `Entity_Ai_Effect_Smackyhut_Buff` | data-only | Sets skin `Buff`; |
| `Entity_Ai_Effect_Smackyhut_Scrap` | data-only | Each outgoing Hurt on a `Resource` gives the holder's Owner 1 aura of each colour the target has a |
| `Entity_Ai_Effect_Smackyhut_Towerbuff` | data-only | Timed Range/Attackspeed/Damage Pmulti for `Duration_Base` s with a border vfx, then removes itself. |
| `Entity_Ai_Effect_Smackywall_Beacon` | data-only | After 0.1 s adds Health `+Effect_Add` and another `+Effect_Add` every Tide. |
| `Entity_Ai_Effect_Smackywall_Bramble` | data-only | Thorns: each incoming Hurt without `Spike` hits the attacker for `Effect_Add` (tagged `Spike`). |
| `Entity_Ai_Effect_Smackywall_Regen` | data-only | Each Tide change heals every same-team Mob/Tower within `Range_Base` for `Effect_Add x holder Level` |
| `Entity_Ai_Effect_Snosnoheater_Scorch` | data-only | Burn: each incoming Hurt carrying Data `Burn` resets the timer to `Duration_Base`; |
| `Entity_Ai_Effect_Snosnoheater_Shock` | data-only | The next incoming Hurt carrying Data `Snoball` gets `+Effect_Multi` Damagemulti, then the effect ends; |
| `Entity_Ai_Effect_Snosnoheater_Sweat` | data-only | Timed Attackspeed Pmulti `Effect_Multi` for `Duration_Base` s. |
| `Entity_Ai_Effect_Snosnosnow` | vessel-bound | Sno's spreading snow tile. |
| `Entity_Ai_Effect_Snosnosnow_Black` | vessel-bound | Adds runtime tag `Snoblack` on Spawn. |
| `Entity_Ai_Effect_Snosnosnow_Rich` | data-only | On death gives the holder's Owner `Amount_Base` aura per colour tag `Richred`/`Richblue`/`Richyellow`. |
| `Entity_Ai_Effect_Snosnowall_Breaker` | data-only | Health/Speed/Damage Multi with `Debuff_Broken` shown (9999 s) until removed. |
| `Entity_Ai_Effect_Speed` | data-only | Speed Multi `Speed_Multi`, plus Speed Override if `Speed_Override` is given; |
| `Entity_Ai_Effect_Sushi3` | vessel-bound | Intended: outgoing Hurts get `+Damage_Multi` if `Cooldown_Base` s passed since the last hit. |
| `Entity_Ai_Effect_Tastes` | vessel-bound | Health Multi `Health_Multi` while the holder has a `Shopdebuff` modifier. |
| `Entity_Ai_Effect_Tower_Merchut` | data-only | Vulnerability mark: every incoming Hurt gets `+Effect_Multi` Damagemulti; |
| `Entity_Ai_Enemy_Aegis` | data-only | Regen: every 1 s, if `Healtime` s have passed since `Master.Lasthittime`, heals itself `Heal`. |
| `Entity_Ai_Enemy_Armor_Upgrade` | data-only | Armored elite: skin `Armor`; |
| `Entity_Ai_Enemy_Ballista` | data + stock asset | Ranged sniper. |
| `Entity_Ai_Enemy_Basic` | data-only | Walker: creates `Goal`; |
| `Entity_Ai_Enemy_Boutman` | vessel-bound | Caged story boss. |
| `Entity_Ai_Enemy_Bulwark` | data + stock asset | Siege brute. |
| `Entity_Ai_Enemy_Chaos_Upgrade` | data-only | Elite: Health/Speed/Damage/Attackspeed Multi and skin `Chaos`. |
| `Entity_Ai_Enemy_Claymore` | data + stock asset | Kamikaze. |
| `Entity_Ai_Enemy_Excalibur` | data + stock asset | Ranged. |
| `Entity_Ai_Enemy_Explode_Upgrade` | data + stock asset | Elite: Health/Speed Multi, skin `Explode`. |
| `Entity_Ai_Enemy_Halberd` | data-only | Flying guardian. |
| `Entity_Ai_Enemy_Health_Upgrade` | data-only | Elite: Health Multi `Health_Multi`, skin `Health`. |
| `Entity_Ai_Enemy_Hyper_Upgrade` | data-only | Elite: Health/Speed/Damage/Attackspeed Multi, skin `Hyper`. |
| `Entity_Ai_Enemy_Maul` | data-only | Melee AoE. |
| `Entity_Ai_Enemy_Onager` | data + stock asset | Artillery. |
| `Entity_Ai_Enemy_Red_Upgrade` | data-only | Aggro elite: Health/Damage Multi, skin `Red`. |
| `Entity_Ai_Main_Core` | data-only | The basin core. |
| `Entity_Ai_Main_Core_Treated` | data-only | Easy-mode package (once per match, static guard): sets `Display.Mode_Treated`; |
| `Entity_Ai_Main_Coresplit_Effect` | data-only | Tags the holder `Noart` on Spawn, so a core's `Spawn` command skips Cardinal core upgrades. |
| `Entity_Ai_Main_Enemycore` | data-only | Enemy spawner. |
| `Entity_Ai_Main_Enemycoresplit` | data + stock asset | Map modifier: when Tide becomes 1, applies `Main_Enemycoresplit_Effect_Upgradeproperties` to every `Enemycore`. |
| `Entity_Ai_Main_Enemycoresplit_Effect` | data-only | Spawn multiplier. |
| `Entity_Ai_Main_Subcore` | data-only | Secondary core: priority-9 one-lethal-hit protection (Health set to 1, once); |
| `Entity_Ai_Main_Chrono` | data-only | Clock map: on each Tide change sets `Time_Scale` 1 for `Lingertime` s, then to the current speed: 1.5 from |
| `Entity_Ai_Main_Flood` | data + stock asset | Rising water: each Tide (until `Maxheight`) rises by `Heightspeed` while below `Halfheight`, by |
| `Entity_Ai_Main_Tpgate` | data-only | Teleport gate: 1 s after start, adds nav connections from its tile to every other `Tp`-tagged entity that |
| `Entity_Ai_General_Adjacentblocker` | data + stock asset | On Spawn puts `Block_Noplacement` (child) on every adjacent placeable Surface tile. |
| `Entity_Ai_General_Expire` | data-only | Despawns the holder (death) after stat `Duration` s, polling each game tick. |
| `Entity_Ai_General_Spawnmobs` | data + stock asset | Spawner: each `Defaultparams` entry is `Mob name -> respawn seconds`; |
| `Entity_Ai_General_Upgrade` | data + stock asset | Upgrade menu: creates commands `Upgrade` and `Upgradeoption1-4` (stock `Command_Upgrade*`, Goalvalue 0-3). |
| `Entity_Ai_Resource_Death` | data-only | Breakable ore: applies damage itself at priority 10 and dies at 0; |
| `Entity_Ai_Resource_Farmland` | data + stock asset | Crop plot: stat `Percent` grows by `Heal` every 0.2 s; |
| `Entity_Ai_Resource_Farmstead` | data + stock asset | Build hub for farmland: commands `Buildoption1-3` (stock `Command_Buildoption1-3`) with Goalstrings |
| `Entity_Ai_Resource_Mine` | data-only | Endless ore: applies damage itself at priority 10 (`Healthresource` takes <= 1 + `Damageresource` per hit); |
| `Entity_Ai_Resource_Opal` | data-only | Endless ore like `Resource_Mine` whose colour cycles each Tide (Base mods on the `*auramine` stats): red |
| `Entity_Ai_Modresource` | data-only | Ore modifier: for each colour, `<C>_Base` creates the `<C>auramine` stat (or adds that much if it exists); |
| `Entity_Ai_Blastheadore` | data + stock asset | Growing ore: each Tide spawns `Resource_Branchrock_<Colour>` (by tag `Redmine`/`Bluemine`/`Yellowmine`) on |
| `Entity_Ai_Oroboro` | data-only | Shifting map: when Tide reaches stat `Path2`, moves every `Core` to the team -1 `Tp` marker tile (adding |
| `Entity_Ai_Block_Surface` | data-only | Ground tile: clones its `Collider` child towards each blocked neighbour connection (`Navblock`/`Navblockteam` |
| `Entity_Ai_Entposition_Position` | data-only | Keeps `Entityposition` equal to `Position` (copies on every change). |

### 3.3 Units, towers, traps, projectiles, helpers (52; full entries in [`engine_study_2026-10-28/C_units_towers_projectiles.md`](engine_study_2026-10-28/C_units_towers_projectiles.md))

| Script | Reuse | Does |
| --- | --- | --- |
| `Entity_Ai_Mob_General` | data-only | The standard damage handler for units. |
| `Entity_Ai_Mob_Invincible` | data-only | Only creates and enters the `Idle` command. |
| `Entity_Ai_Mob_Movement` | data + stock asset | Grid walking along a nav path for the `Move` command. |
| `Entity_Ai_Mob_Movementlinear` | data + stock asset | Straight-line movement for `Movelinear`. |
| `Entity_Ai_Mob_Melee` | data-only | Runs the `Attack` state against `Commandlistserver["Attack"].Goalobject`. |
| `Entity_Ai_Mob_Combat` | data-only | `_Melee` plus target picking. |
| `Entity_Ai_Mob_Ranged` | no | A ranged auto-attack. |
| `Entity_Ai_Mob_Aurabeast` | data + stock asset | A neutral beast built on `_Melee`. |
| `Entity_Ai_Mob_Bird` | data + stock asset | A pop-able bird that sits 1.65 above its `Parent`. |
| `Entity_Ai_Mob_Kippy` | data + stock asset | A support runner. |
| `Entity_Ai_Mob_Merchant` | vessel-bound | A self-contained guard unit that handles its own damage. |
| `Entity_Ai_Tower_General` | data-only | The same as `_Mob_General` (damage and heal at priority 10, `Idle` command). |
| `Entity_Ai_Tower_Artriobarrier` | data-only | Shared health between linked barriers. |
| `Entity_Ai_Tower_Artriominibarrier` | data-only | The same as `_Artriobarrier` with tag `Artriominibarrier`. |
| `Entity_Ai_Tower_Barrel` | data + stock asset | On death, spawns `Trap_Barrel_Dead` (forced, its own tile and orientation, as its child). |
| `Entity_Ai_Tower_Buff` | data-only | At spawn it gives its **Parent** the holder Entityobject's `Upgrades[0]` (forced), unless the Parent already |
| `Entity_Ai_Tower_Converter` | data-only | Every 3 s, for each player: find their lowest resource. |
| `Entity_Ai_Tower_Den` | data + stock asset | A beast spawner. |
| `Entity_Ai_Tower_Drinkstand` | data + stock asset | Every 1/`Attackspeed` s it fires `Tower_Drinkstand_Buff` with interaction type `Buff` at up to `Count` allies |
| `Entity_Ai_Tower_Halberdnest` | data + stock asset | On each Tide ≥1: |
| `Entity_Ai_Tower_Merchut` | no | A bounty board with 20 contracts in hard-coded lists (count, enemy type, elite upgrade, reward × |
| `Entity_Ai_Tower_Sandbox` | no | Sandbox admin panel. |
| `Entity_Ai_Tower_Snare` | data + stock asset | Each slowtick it looks for enemy `Mob`s (untargetable included) within 0.25 of its position. |
| `Entity_Ai_Tower_Tilebuff` | data-only | Whenever its tile's occupants change (and at spawn), it gives every **same-team** being on the tile whose |
| `Entity_Ai_Tower_Tilebuffany` | data-only | The same as `_Tilebuff`, but it buffs **any** non-dead being on the tile, of any team (enemies walking over |
| `Entity_Ai_Tower_Tilebuffanyonly` | data-only | The same as `_Tilebuffany`, without the `Dead` filter. |
| `Entity_Ai_Trap_General` | data-only | Damage and heal like `_Mob_General` (priority 10), but `Addressdamage(Kill:false)`: a trap at 0 health is |
| `Entity_Ai_Trap_Iceunit` | data + stock asset | A frozen enemy. |
| `Entity_Ai_Trap_Fierally` | data + stock asset | Creates a `Parent` proxy command whose `Goalobject` is `Master.Parent`, and pushes it to clients so client |
| `Entity_Ai_Trap_Fishnoplacement` | data + stock asset | Identical to `_Fierally` (a `Parent` proxy command synced to clients). |
| `Projectile_Master` | data-only | A pooled projectile body. |
| `Projectile_Ai_Base` | no (base class) | The base class. |
| `Projectile_Ai_Simple` | data-only | A straight or ballistic shot. |
| `Projectile_Ai_Lob` | data-only | An arc onto a target. |
| `Projectile_Ai_Home` | data-only | A homing missile. |
| `Projectile_Ai_Track` | data-only | A perfect tracker. |
| `Projectile_Ai_Beam` | data-only | A beam from a fixed origin. |
| `Projectile_Ai_Expand` | data-only | A growing shockwave. |
| `Projectile_Ai_Delay` | data-only | A fuse: it kills the projectile (Death) `Activetime` s after spawn (default 1). |
| `Projectile_Ai_Explosion` | data-only | On death (`Despawn`, Death only), at the hit point, it hits up to `Count` (default 999) **enemy-team** |
| `Projectile_Ai_Explosion_Any` | data-only | The same as `_Explosion`, but it selects by tag `Mob`/`Tower`/`Trap` of **any team**. |
| `Projectile_Ai_Explosioncollide` | data + stock asset | An impact burst. |
| `Projectile_Ai_Splitbounce` | split: vessel-bound (Smacky); bounce: data-only | On death (host only) it does one of two things, set by the `Splitbounce` param. |
| `Projectile_Ai_Corebomb` | no | The core-loss bomb fired by `Mode_Normal`. |
| `Projectile_Ai_Excaliburbolt` | data + stock asset | It pierces everything. |
| `Projectile_Ai_Sno` | vessel-bound | On death (host), on the nearest `Surface` within 1: |
| `Projectile_Ai_Snoball` | data-only | At spawn it writes `Damage` = the shooter's current `Damage` stat and `Damagemulti` = `Multi` (default 1) into |
| `Projectile_Ai_Snoble` | vessel-bound | On death (host), on the nearest `Surface` within 1, it spawns (loose, child of the shooter) a `Mob_Snoble` |
| `Projectile_Ai_Snosnocannon_Laser` | data-only | A damage field. |
| `Helper_Damagetools` | no (code helper) | The hit and heal helpers every attack goes through: armour, overheal, resource and health layers, in that order. |
| `Helper_Entityai` | no (code helper) | The shared melee and ranged target checks, scaled waits and the Cardinal apply the AIs call. |
| `Helper_Entityquerytools` | no (code helper) | The nearest, all and farthest queries by tag, team and range. |

### 3.4 Buttons, visual scripts, effect helpers (96; full entries in [`engine_study_2026-10-28/D_commands_visuals.md`](engine_study_2026-10-28/D_commands_visuals.md))

| Script | Reuse | Does |
| --- | --- | --- |
| `Entity_Command_Base` | no | Base class. |
| `Entity_Command_Simple` | data-only | One-click button: on `Activate` sends the command and closes it. |
| `Entity_Command_Itemselect_Base` | no | "Click an entity" picker. |
| `Entity_Command_Move` | data + stock asset | "Click a ground tile" order. |
| `Entity_Command_Movelinear` | data + stock asset | "Pick a direction" order: arrow `Movefromborderdirectional` on the own visual, rotated toward the mouse |
| `Entity_Command_Build` | data + stock asset | Build-placement mode for the tower named by `Link.Goalstring`. |
| `Entity_Command_Tower_Fiewall_Build` | vessel-bound | `Entity_Command_Build` whose `Placementhover` is true only when an entity tagged `Fiewall` (client |
| `Entity_Command_Sell` | data + stock asset | Picker (Itemselect) that previews the refund of the hovered own `Sellable` entity: base `Costs` x health |
| `Entity_Command_Upgrade` | data + stock asset | Buys upgrade number `Link.Goalvalue` from the client's current `Mainupgrade.Upgrades` (or |
| `Entity_Command_Selectother` | data-only | One-click "select my X": loads Beingobject `Othername` (inspector field) from group `Mob`, selects the |
| `Entity_Command_Artifact_Blurryphoto_Select` | data-only | One-click "next copy": selects another entity with the same GameObject name as the selected one, same team |
| `Entity_Command_Artifact_Pig_Launch` | vessel-bound | Move-style ground picker gated client-side by 25 `Redaura`, 25 `Blueaura`, 25 `Yellowaura`; |
| `Entity_Command_Mob_Boutmanfish_Relocate` | data-only | Move-style ground picker (`Moveborder`/`Movefromborder`), click plays `Movebordercommand`. |
| `Entity_Command_Tower_Fiekeep_Command` | data + stock asset | Move-style picker for a rally point. |
| `Entity_Command_Tower_Snosnobox_Launch` | data + stock asset | Move-style ground picker with preview Vfx `World_Snosnobox_Fallpos_Command`; |
| `Entity_Command_Trap_Busterpad_Air` | data + stock asset | Launch-pad target picker. |
| `Entity_Command_Hero_Boutman_Fishfollow` | vessel-bound | Picker "RECRUIT FISH" (Ui `Ui_Command_Boutman_Fishfollow`, Vfx `Fishborder`) that accepts an own entity |
| `Entity_Command_Tower_Fiesilo_Reroute` | data-only | Picker "CONNECT TO RESOURCE" (Ui `Ui_Command_Tower_Fiesilo_Reroute`, Vfx `Rerouteborder`) accepting a |
| `Entity_Command_Tower_Fiesilo_Convert` | data-only | `Entity_Command_Simple` whose `Render` colours the button and icon by `Link.Goalstring`: `Red`, |
| `Entity_Command_Tower_Smackyhut_Convert` | data-only | As Fiesilo_Convert for `Red`/`Yellow`/`Blue` with icons `Redaura`/`Yellowaura`/`Blueaura`. |
| `Entity_Command_Tower_Simple_Toggle` | data-only | One-click toggle; |
| `Entity_Command_Tower_Sandbox_Enemytype` | data-only | `Entity_Command_Simple` whose label names the stock enemy in `Goalstring` (`Enemy_Dagger`, `Enemy_Spear`, |
| `Entity_Command_Tower_Sandbox_Elitetype` | data-only | Label for elite upgrade names: "" None, `Enemy_Health_Upgradeobject` Hardened, `Enemy_Red_` Tempered, |
| `Entity_Command_Tower_Merchut` | data + stock asset | One-click contract button. |
| `Entity_Vis_Base` | no | Abstract MonoBehaviour base: fields `Master/Being/Client/Radio/Remote/Dead`, virtual `Spawn, Despawn, |
| `Entity_Vis_Animator_Base` | data-only | Sprite animator. |
| `Entity_Vis_Animator_Basic` | data-only | Animator + hit feedback, no movement: `Hurt` kicks the sprite material `_Offset` away from the hit |
| `Entity_Vis_Animator_Mob` | data-only | Walking-unit animator: faces sprite variant by `Entitydirection` (`Rotatesprite`), and moves `Visobj` toward |
| `Entity_Vis_Animator_Mob_Simple` | data-only | Animator that faces by `Entitydirection` and slerps `Visobj` 50% per FixedUpdate to `Entityposition`, any |
| `Entity_Vis_Animator_Tower_Smackyarcher` | data-only | Sprite-tower animator: faces the last aim; |
| `Entity_Vis_Animator_Tower_Smackybomber` | data-only | As Smackyarcher but on Vis `Attack`: forces `Attack`, tweens each child of `Currentvis/Minions` toward the |
| `Entity_Vis_Animator_Tower_Snosnoheater` | data + stock asset | Animator plus an always-on Vfx `World_Tower_Snosnoheater_Heat` under the entity, scaled to `Range*2` and |
| `Entity_Vis_Artifact_Computer` | data + stock asset | Two always-on rings (Vfx `World_Artifact_Computerrangeeffect`) under `Vis`, scaled to `Range*2` and |
| `Entity_Vis_Artifact_Wavebreaker` | data + stock asset | One always-on ring (Vfx `World_Artifact_Wavebreakereffect`) under `Vis`, scaled to `Range*2`. |
| `Entity_Vis_Block_Noplacement` | data-only | Hides child `Select` of `Currentvis`, shows it while the active command's `Objectname` contains `Build` |
| `Entity_Vis_Block_Surface` | data + stock asset | Waits for the host Pingpong message `Block_Surface` (`Entity_Ai_Block_Surface.Block_Surface_Pinginfo`), |
| `Entity_Vis_Branching` | data-only | Connecting-wall mesh picker: reads 8 stats `Post_NW, Post_N, Post_NE, Post_E, Post_SE, Post_S, Post_SW, |
| `Entity_Vis_Claymore` | data + stock asset | When stat `Death >= 1`: plays sound `Enemy_Claymore_Activate` once, then flashes sprite `_Tintcolor` |
| `Entity_Vis_Effect_Snosnosnow_Richblack` | vessel-bound | Tints child `Snow`'s material and flattens the visual by tags: `Richred`, `Richblue`, `Richyellow` (-0.2 on |
| `Entity_Vis_Enemy_Ballista` | data-only | Vis `Shoot` -> forced anim `Attack`. |
| `Entity_Vis_Enemy_Excalibur` | data-only | Vis `Attack` -> forced anim `Attack`. |
| `Entity_Vis_Enemy_Bulwark` | data + stock asset | Vis `Attack` -> forced `Attack`; |
| `Entity_Vis_Enemy_Maul` | data + stock asset | Vis `Attack` -> forced `Attack`; |
| `Entity_Vis_Enemy_Halberd` | data-only | State `Flying` -> soft anim `Flying`, slow slerp (0.05) of `Visobj`, turns on child `Flying` (plays sound |
| `Entity_Vis_Enemy_Boutman` | no | States `Sleeping`/`Ko` -> soft anims of the same name, visual held at the entity; |
| `Entity_Vis_Enemy_Reallystrongfish` | no | As Enemy_Boutman, but Vis `Ko` plays no animation and writes `Unlock_Other/Enemy_Boutman_Defeat` and |
| `Entity_Vis_Fierally_Clientvis` | data-only | Shows child `Client` of the visual only to the owner and subowners (local session check). |
| `Entity_Vis_Hero` | data-only | Vessel shell. |
| `Entity_Vis_Hero_Boutman` | data-only | States `Fishing` -> soft `Fish`, `Ko` -> soft `Ko` (visual held); |
| `Entity_Vis_Hero_Buster` | data-only | Vis `Attack` -> `Animplay("Attack", Defaultrestart)`. |
| `Entity_Vis_Main_Core` | data-only | Mesh core: sets material[1] `_Overlaycolor` white; |
| `Entity_Vis_Main_Enemycore` | data + stock asset | Mesh enemy core: purple overlay colour; |
| `Entity_Vis_Main_Flood` | data + stock asset | Adds `Entity_Hitobject_Signals` to every collider; |
| `Entity_Vis_Mob_Attack` | data-only | Vis `Attack` or `Shoot` -> forced anim `Attack`. |
| `Entity_Vis_Mob_Bird` | data-only | Animator subclass: always variant 1, slerps 50% to `Entityposition`, faces direction; |
| `Entity_Vis_Mob_Boutmandad` | data + stock asset | Animator subclass for a leaping parent: state `Idle` -> soft "Idie", `Fly` -> hovers 3.5 up (slerp 0.05), |
| `Entity_Vis_Mob_Boutmanfish` | data + stock asset | Needs an animator. |
| `Entity_Vis_Mob_Boutmanfish_Engagerange` | data + stock asset | On select shows Ui `Rangecontainer` (radius `Range`, under `Vis`, follows the unit) and Ui |
| `Entity_Vis_Mob_Enttrack` | data-only | Lerps `Visobj` 5% per FixedUpdate toward `Entityposition`. |
| `Entity_Vis_Mob_Range` | data + stock asset | `Entity_Vis_Tower_Range` whose ring is parented under `Vis` on select, so it moves with a unit. |
| `Entity_Vis_Movementlinear` | data-only | Needs an animator. |
| `Entity_Vis_Resource` | data-only | Mesh ore node: white overlay colour; |
| `Entity_Vis_Resource_Opal` | data-only | Stat `Type` picks one of the visual's children `Red` (0), `Blue` (1), `All` (2), `Yellow` (3), and also |
| `Entity_Vis_Select_Parent` | data-only | When selected, selects the `Goalobject` of the entity's `Parent` command instead. |
| `Entity_Vis_Tower` | data-only | Mesh tower: `Hurt` flashes overlay (shader `Shader Graphs/General Tower` or material[1]) with low-health |
| `Entity_Vis_Tower_Range` | data + stock asset | Ui `Rangecontainer` ring at `Vis` (or `Detect` child) scaled to `Range*2`, shown while selected; |
| `Entity_Vis_Tower_Orientation` | data + stock asset | While selected, shows Ui `Directioncontainer` under `Vis` (facing arrow). |
| `Entity_Vis_Tower_Artriobarrier` | no | On `Hurt` flashes the map's `Wall` meshes, warns "ALERT ▪ BARRIER DESTRUCTION IMMINENT" at <=10% health; |
| `Entity_Vis_Tower_Boutmancage` | no | Warning "ALERT ▪ CAGE IS SOON TO BREAK!" at <=10% health; |
| `Entity_Vis_Tower_Boutmanbarrel` | data + stock asset | Always-on Vfx `World_Tower_Boutmanbarrel_Range` scaled to `Range*2`; |
| `Entity_Vis_Tower_Boutmanboat_Cannon` | data-only | Vis `Shoot`: turns child `Pivot` (flat) and `Pivot/Gun` (full) toward the aim vector. |
| `Entity_Vis_Tower_Buff` | data-only | When stat `Effected > 0` activates every direct child of the entity's root GameObject, else deactivates |
| `Entity_Vis_Tower_Bustercheckpoint` | data + stock asset | Ore-link display: Vfx `World_Tower_Bustercheckpoint_Fallpos` (children `Efficiency` text, `Ball`) sits on |
| `Entity_Vis_Tower_Fiesilo` | vessel-bound | Silo display: Vfx `World_Tower_Fiesilo_Fallpos` (children `Gradient` shown while selected, `Trail` |
| `Entity_Vis_Tower_Fiesilo_Secondore` | vessel-bound | Copy of Fiesilo for the second ore: command `Ore2`/`Gun2`, stats `Gun2`/`Connect2`, child |
| `Entity_Vis_Tower_Fiesilo_Frontrange` | data + stock asset | Always-on Ui `Rangecontainersubengage` ring, `Range*2`, moved every 1 s onto the `Ore` command's |
| `Entity_Vis_Tower_Smackyhut_Convert` | data-only | Colours child `Head`'s material[0] by the `Conversion` command's Goalstring (`Red`/`Blue`/`Yellow`). |
| `Entity_Vis_Tower_Smackywall` | data-only | Turns child `Posts` to `Truedirection` and enables `Posts/Front\|Back\|Left\|Right` by stats `Post_Front`, |
| `Entity_Vis_Tower_Snosnobox` | data + stock asset | Vis `Shoot` -> shows child `Sno` and plays emitter Vfx `World_Tower_Snosnobox_Sno` on the entity; |
| `Entity_Vis_Tower_Snosnobox_Fall` | data + stock asset | While selected, puts Vfx `World_Snosnobox_Fallpos` at the `Launch` command's `Goalposition` (updates on |
| `Entity_Vis_Tower_Snosnocannon` | data-only | Vis `Shoot` aims `Pivot`/`Pivot/Gun` (like Boutmanboat_Cannon); |
| `Entity_Vis_Tower_Snosnocannon_Snorange` | data + stock asset | Second selection ring (Ui `Rangecontainersub`) scaled by stat `Rangesno`. |
| `Entity_Vis_Trap_Busterpad` | data-only | On `Extrapositiondata` change, resets `Visobj` rotation and applies `Orientation` |
| `Entity_Vis_Trap_Busterpad_Air` | data + stock asset | While the `Air` command's `Goalvalue == 1`, shows Vfx `Airtimedistanceactive` with child `Gradient` |
| `Helper_Entityvis` | data-only (code helper mod code can call) | Static look helpers: sprite facing from the camera, local stat and command reads, owner highlight borders. |
| `Helper_Vfxtools` | data-only (code helper mod code can call) | Static effect helpers: pooled one-shots, debuff looks, camera shake. |
| `Vfx_Base` | data-only | Pooled one-shot. |
| `Vfx_Complex` | no | `Vfx_Base` meant to push `Data` floats into the root VisualEffect before playing. |
| `Vfx_Debuff` | data-only | Attached status effect: counts `Duration` down in FixedUpdate; |
| `Vfx_Emitter` | data-only | Persistent attached effect: `Start` caches VisualEffects (one = `Effect`, several = `Effects`) and applies |
| `Vfx_Add_Base` | no | Add-on base for Vfx prefabs: `OnEnable` -> `Run()`, `OnDisable` -> `Rundisable()`. |
| `Vfx_Sound` | data-only | Add-on: after `Delay` s plays sound `Sound` at the effect's position. |
| `Vfx_Soundui` | data-only | Add-on: after `Delay` s plays UI sound `Sound`. |
| `Vfx_Shake` | data-only | Add-on: after `Delay` s calls `Helper_Vfxtools.Shake(pos, forward*Offset, Duration, Am)` (defaults Am 25, |
| `Vfx_Volumeanimator` | data-only | Add-on: fades the object's post-process `Volume` weight 1 -> 0 over `Time` after `Delay`, with `Easetype` |
| `Vfx_Deatheffect` | data-only | Add-on: on disable (end of a pooled one-shot) fires `Oneshot(Vfx)` at its position/forward (chains effects). |

## 4. Most reusable for a mod

Each group's own list, as its reader wrote it (with the two corrections of §1).

### 4.1 Cardinals

- `Entity_Ai_Artifact_Coreapply`: pushes any named upgrades onto every `Core` (or every `Enemycore`). Use it for a map rule, a core blessing or a core curse.
- `Entity_Ai_Artifact_Heroapply` / `_Heroapply_Delay`: give every vessel named upgrades, optionally after N real seconds. This is how a Direction or Cardinal works on every vessel, mod vessels included.
- `Entity_Ai_Artifact_Mobapply` / `_Towerapply`: give named upgrades to every `Mob`/`Tower` spawn of **all** teams. Use them for enemy modifiers or elite traits, paired with effects that check team themselves.
- `Entity_Ai_Artifact_Teamapply` / `_Voiceapply`: give one named upgrade to everything a vessel builds. Use them for a friendly build buff. They reach every vessel's buildings, not only the holder's player (see Teamapply's Quirks), so they fit a team Cardinal, not a per-player Direction.
- `Entity_Ai_Effect_Artifact_Snare`: a stun that self-expires, scaled by health, and pays on hit. Use it for a "rooted" debuff or a trap payload.
- `Entity_Ai_Effect_Artifact_Titan`: a death explosion scaled by level, plus Health. Use it for a "volatile" elite or a kamikaze unit.
- `Entity_Ai_Effect_Artifact_Opalcore`: thorns (reflects the holder's `Damage`) plus Health that grows each tide. Use it for a spiked elite or a hardened core. It also adds a `Damage` stat, warns "BASIN GROWS STRONGER" and gives the holder the skin `Sub`; for plain thorns the fit plan uses `Entity_Ai_Effect_Smackywall_Bramble`.
- `Entity_Ai_Artifact_Wavebreaker`: an aura that cuts enemy max HP while they stay in range. Use it for a curse aura on any tower or core.
- `Entity_Ai_Effect_Artifact_Paint_Effect` + `_Bluepaintcharm`: an enemy at low HP converts to your side after a timer. Use them for a charm mechanic.
- `Entity_Ai_Artifact_Checklist_Special_Pickup` (stock entity `Special_Aurapickup`): an 8 s pickup that friendly mobs collect for aura. Use it for kill loot or a sea-weather drop.
- `Entity_Ai_Artifact_Vintage` / `_Samedayshipping`: lengthen or shorten the break between tides. Use them for sea weather (calm or rush).
- `Entity_Ai_Effect_Artifact_Glasscore`: makes an entity break without counting as a death, with a resource tax and a delayed map-wide blast. Use it for a defend-or-lose side objective.
- `Entity_Ai_Effect_Artifact_Caffeine` / `_Slippers` / `_Diorama` / `_Greatspear_Effect`: ready stat packages that ramp with the tide, ramp while moving, stay flat, or slow permanently. Avoid `Paper_Tower` and `Buff_Rustypipe`: their Downgrade leaves mods behind.
- `Entity_Ai_Artifact_Fragment_Effect`: rings any entity with stock `Tower_Artriominibarrier` walls. Use it for a fortification pickup (the team is fixed at 1).

### 4.2 Effects, enemies, cores, map objects, resources, general

- `Entity_Ai_Effect_Speed`: slow, haste or root (`Speed_Multi`, optional `Speed_Override`): sea-weather drag, nets, currents.
- `Entity_Ai_Effect_Barrel_Boost`: any timed 4-stat buff/debuff (Health/Speed/Attackspeed/Damage) that ends itself.
- `Entity_Ai_Effect_Busterdog_Towerboost`, `_Smackyhut_Towerbuff`, `_Snosnoheater_Sweat`: timed tower buffs (pickups, shrines, Cardinal procs).
- `Entity_Ai_Effect_Boutmanbucket_Boost`: timed invulnerability (shield pickup, spawn protection).
- `Entity_Ai_Effect_Boutmanfish_Puffer` / `_Bustertotem_Blaze` / `_Boutmanfish_Bass`: poison DoT, "build-a-bomb" mark, execute mark.
- `Entity_Ai_Effect_Smackywall_Bramble` and `Entity_Ai_Effect_Tower_Merchut`: plain thorns; "marked, takes +X damage".
- `Entity_Ai_Effect_Smackywall_Regen` / `_Smackywall_Beacon`: per-Tide heal-pulse aura; per-Tide growing, damage-resisting wall.
- `Entity_Ai_Effect_Bustercheckpoint_Attackaura`, `_Boutmanfish_Caviarbuff`, `_Snosnosnow_Rich`, `_Smackyhut_Scrap`: aura payouts (first-hit bounty, treasure ore, death bounty, per-hit scavenging).
- `Entity_Ai_General_Spawnmobs`: spawner with respawn timers from params (nests, barracks, reef that breeds units).
- `Entity_Ai_General_Expire` / `Entity_Ai_Effect_Killonspawn`: lifetime for summons, puddles, temporary pickups.
- Elite set `Enemy_Armor_Upgrade` (hit-count shield), `Enemy_Red_Upgrade` (hunts towers), `Enemy_Explode_Upgrade` (death blast + melt), `Enemy_Aegis` (regen), `Enemy_Health_Upgrade`: modifiers for the mode's elites (Chaos/Hyper only as permanent upgrades).
- `Entity_Ai_Main_Enemycore` + `Entity_Ai_Enemy_Basic` (+ `Main_Enemycoresplit_Effect`): mod spawn points the mode can command with any Mob and elite upgrades.
- `Entity_Ai_Main_Flood` / `Entity_Ai_Main_Chrono` / `Entity_Ai_Oroboro`: per-Tide map events (rising sea, fast current, shifting map).
- `Entity_Ai_Main_Tpgate`: whirlpool pairs, paired by a shared upgrade name on two `Tp`-tagged objects.
- `Entity_Ai_Main_Core_Treated`: a run blessing (extra time each phase, starting aura, bonus drops, core health).
- `Entity_Ai_Resource_Mine` / `_Opal` / `_Death` + `Entity_Ai_Modresource`: ore and wreck nodes with any aura mix.

### 4.3 Units, towers, traps, projectiles, helpers

- `Entity_Ai_Mob_General` / `_Tower_General` / `_Trap_General` / `_Mob_Invincible`: choose a body that dies, cannot die
  from damage, or is fully immune.
- `Entity_Ai_Mob_Movement` + `Entity_Ai_Mob_Combat`: summoned melee fighters or gatherers that auto-hit enemy
  `Mob`/`Tower`/ore in `Range`, from stats alone.
- `Entity_Ai_Tower_Buff`: a child entity (charm, banner, attachment) that grants its `Upgrades[0]` to its parent while it
  lives.
- `Entity_Ai_Tower_Tilebuffanyonly`: a tile zone (hazard pool, healing spring, slow tar) that applies an upgrade only
  while a unit stands on it.
- `Entity_Ai_Tower_Tilebuff`: a plinth or foundation that permanently buffs the allied tower built on its tile.
- `Entity_Ai_Trap_Iceunit` (on a mod upgrade with `{Enemy_X: N}`): a frozen-enemy objective that hatches a stock enemy at
  Tide N unless dealt with.
- `Entity_Ai_Tower_Artriobarrier`: linked walls or totems sharing one health pool through a mod-chosen tag. The link is
  map-wide and covers every team.
- `Entity_Ai_Tower_Barrel` / `_Snare`: drop stock barrel boosts on death; a one-shot step trap (Tackle effect only).
- `Entity_Ai_Mob_Bird`: a pop-to-detonate pickup that hurts every enemy on the map (watch the repeat-burst bug).
- `Entity_Ai_Mob_Kippy`: a runner that ferries a permanent, stacking `Attackspeed` buff to the farthest ally.
- Projectile kit, data-only through a mod `Projectileobject`:
  - `_Simple` (pierce, friendly fire, no-hit carrier);
  - `_Lob`/`_Delay` + `_Explosion` (mortar, timed bomb);
  - `_Track`/`_Home` (seekers); `_Beam` (channelled beam); `_Expand` (shockwave);
  - `_Explosioncollide` (impact burst); `_Snosnocannon_Laser` (lingering damage field);
  - `_Snoball` (damage snapshot with multiplier); `_Splitbounce`=2 (ricochet).
  All of them still need a shooter that names the projectile, which takes mod code.
- `Helper_Entityquerytools.Entityarea` + `Helper_Entitytools.Interact*` + `Helper_Damagetools.Projectile`: the three
  calls any small mod script needs for an aura, a pulse or a shot.

### 4.4 Buttons, visual scripts, effect helpers

- `Entity_Command_Move` + stock `Entity_Ai_Mob_Movement`: a move order for any mod minion (link `Move`).
- `Tower_Snosnobox_Launch` / `Mob_Boutmanfish_Relocate` / `Tower_Fiekeep_Command`: ground-target buttons (strike, drop, rally) sending `{"To"}`/`{"Position"}` to a mod AI.
- `Entity_Command_Tower_Fiesilo_Reroute`: "pick an ore node" picker sending `{"Ref"}` to a mod AI.
- `Entity_Command_Simple`, `Tower_Simple_Toggle`, `Tower_Smackyhut_Convert`, `Tower_Fiesilo_Convert`: trigger, on/off and 3-/6-colour mode buttons.
- `Entity_Command_Selectother` / `Entity_Command_Artifact_Blurryphoto_Select`: "jump to my X" and "next copy" buttons.
- `Entity_Command_Build` / `Entity_Command_Upgrade` with the stock build/upgrade AIs: build and buy buttons, price scaled by `Goalvalue`.
- `Entity_Vis_Animator_Mob` + `Entity_Vis_Mob_Attack`: a new sprite enemy or minion body from the mod's own `Spriteobject`.
- `Entity_Vis_Animator_Tower_Smackyarcher` / `Entity_Vis_Tower_Boutmanboat_Cannon`: sprite or model turrets that turn to the target on `Shoot`.
- `Entity_Vis_Tower_Range` / `Mob_Range` / `Tower_Snosnocannon_Snorange`: selection rings for `Range` and a second radius.
- `Entity_Vis_Artifact_Wavebreaker` / `Artifact_Computer` / `Animator_Tower_Snosnoheater`: always-on aura rings by `Range` (and `Range2`).
- `Entity_Vis_Mob_Boutmanfish_Engagerange`: leash ring around a `Defend` guard point.
- `Entity_Vis_Tower_Buff` + `Entity_Ai_Tower_Buff`: shown-while-buffed indicator (`Effected`; mind its quirk).
- `Entity_Vis_Fierally_Clientvis` / `Entity_Vis_Select_Parent`: owner-only markers; click a satellite to select its parent.
- `Entity_Vis_Main_Enemycore`: a destructible objective that pulses faster as `Healthresource` falls.
- `Helper_Vfxtools.Debuff`/`Oneshot` + `Vfx_Sound`/`Vfx_Shake`/`Vfx_Deatheffect` on a mod Vfx with a `Lifetime` float: debuffs, hit sparks.
