# How TDS works: an engine study (2026-10-28)

> **Since (2026-10-29):** RogueLab round 8 ran the facts a solo host can see on Dimitri's PC and passed 21/0
> (`TDS_Vessel_Creator/reference/roguelab_round8.txt`, `TDS_Mod/ModSource/RogueLab/README.md`). N1-N5, N7 and N13 are
> now **verified** and in the kit (pitfalls #115-#118 and #120, kit `16` §3 for N7, `17` §9 for N13); §14 marks each.
> N6 and N8-N12 stay read only (N12 is never to be tried: it would write a stock unlock into the save). The rest of the
> page is as written on 2026-10-28.

Dimitri asked (2026-10-28): go through the game's code, understand as much as possible of how it works, and plan how
the scope's ideas can fit in without new mechanics. This page is the first half: one tour of the game, system by
system, with the classes and methods that run each part and what a mod can reach. The second half, the scope idea by
idea, is `SCOPE_FIT_PLAN_2026-10-28.md`. The catalogue of every stock behaviour script, the piece the kit did not have
yet, is `ENGINE_STUDY_2026-10-28_BEHAVIOURS.md`.

**How it was read.** The game's `Assembly-CSharp.dll` (unchanged since 2026-10-08) was decompiled in this thread with
the kit's recipe (`docs/13` §11): 703 types. The decompiled code stays in the thread's scratchpad and is never committed.
Line numbers below are from this decompile; the same recipe run with other settings can put them a few lines off the
kit's older citations (for example `Mode_Normal`'s start aura is at :353-356 here and :359-362 in the kit).

**Tags.** **proven** = seen in game by a mod (cited to the round); **read** = from the code only; **new** = not in the
kit before this reading. Nothing new was run in game for this page. Kit paths: `docs/NN` = `TDS_Vessel_Creator/docs/`.

**What the kit already holds.** Before this reading the kit had most of the engine: the data model (`docs/01`), the
vessels' shared rules (`docs/17` §2), the mode loop (`docs/15`), maps and the wallet (`docs/16`), enemies (`docs/13`,
`docs/24`), networking (`docs/19`), UI (`docs/20`), sound and text (`docs/21`), maps (`docs/22`) and the save
(`docs/23`). This page doesn't repeat them: each section gives the shape of the system in a few lines and points to
the chapter with the detail. What this reading adds is the behaviour catalogue (§13) and the facts in §14.

## 0. The game in one paragraph

The host runs everything. A match is a map loaded with a mode; every thing in it (vessel, tower, unit, foe, ore,
ground tile) is one generic networked entity whose behaviour comes from small scripts named in its data. The mode
runs a table of Tides: each Tide asks the enemy cores to spawn foes, waits until they are gone or the clock runs out,
then gives a break. Players earn aura from Tide pay, drops and mining, spend it on buildings and upgrades through
their vessel's buttons, and lose when the Basin (player core) dies. Cardinals are upgrades put on the cores at the
start. Clients mostly watch: they see synced positions, stats, buttons and a few messages, and send button presses to
the host.

## 1. Loading a match

| Step | Code (read) | Kit |
| --- | --- | --- |
| Main menu, then the boat (lobby, vessel select, Cardinal picks, map chart) | `Macro_Main`, `Macro_Worldvisual`, `Macro_Setupui`, `Tds_Multiplayerinfo` (`Mode`, `Artifacts`) | `docs/01` §11, `docs/15` §14.8, §17 |
| `Game_Init.Loadgame` (Game_Init.cs:47-137): load `General_Default`, every player's vessel group, each picked Cardinal's group into `Game_Mapvars.Artifactobjects`, the map group and its sub-loads, the mode group (sets `Game_Mapvars.Mode`), the music; `Game_Grid.Generatemap`; wait until every client reports `Loaded`; `Game_Rounds.Generatemode`; then `Spawnsafe = true` | `Game_Init`, `Game_Grid`, `Game_Rounds` | `docs/19` §3 (120 s kick), `docs/22` §1 |
| Entities spawned before `Spawnsafe` wait for it to start (Entity_Master.cs:451-457) | `Entity_Master.Start` | §2 below |
| The mode's `Run` starts the Tides (§9) | `Mode_Normal.Run` | `docs/13` §3 |

## 2. Data: what a thing is made of

Everything is a `Bl_ScriptableObject` with an `Objectname` that the network sends; `Bl_Address` keeps them in groups
by label (`Hero`, `Mob`, `Upgrade`, `Projectile`, `Command`, `Vfx`, `Sound`, `Ui`, `Mode`, `Artifact`, `Other`) and
in one global name table, so two mods using one name collide silently (`docs/01` §3).

| Asset | Holds | Read by |
| --- | --- | --- |
| `Beingobject` | a placeable thing: `Block` (footprint) + `Entity` | `Game_Entity.Spawn` |
| `Entityobject` | `Aiscripts`, `Visualscripts`, `Defaultstat`, `Defaultvisual`, `Upgrades`, `Upgradesinit`, `Costs`, `Drops`, `Tags`, `Sellable`, `Controllable`, `Defaulthit` | `Entity_Master.Start` |
| `Statproperties` | stats: `Name`, `Value`, `Leveladd`, `Style` (Number, Counter...), `Value_Type` (`Fixed`, `Variable`, `Variableadd`, `Variabletrue`), limits | `Entity_Master.Start` (Entity_Master.cs:353-400) |
| `Upgradeproperties` | `Priority`, `Costs`, `Defaultparams`, `Aiscripts`, `Ent_Visual`, next `Upgrades`, `Display_Name/Desc` | `Helper_Entitytools.Upgrade` |
| `Commandobject` | a button: `Style`, `Keybind`, `Icon`, `Commandfuncts` (instances: mod subclasses work) | `Helper_Entitytools.Createcommand` |
| `Projectileobject` | a shot and its `Defaultparams` | `Helper_Damagetools.Projectile` |
| `Modeobject` + `Wavesobject` | the mode's script and its Tide table (§9) | `Game_Rounds`, `Mode_Normal` |
| `Artifactentryobject` + `Artifactobject` | a Cardinal: lobby entry, `Coreupgrades` | `Game_Init`, `Entity_Ai_Main_Core` |
| `Heroentryobject`, `Mapentryobject`, `Modeentryobject` | menu entries pointing at the groups to load | the boat, `Game_Init` |

Mods can't add classes the engine attaches by name, but **they can name the game's own classes in their own data**
(`docs/01` §5). That one fact is what the fit plan leans on: most stock behaviour is a script that reads its numbers
from the data that lists it, so a pack asset that lists a stock script with its own numbers gets the stock behaviour
with no new code.

## 3. Entities and their life

One network prefab serves every entity; `Entity_Master` holds its synced state (`Being`, `Entitydata` {Owner,
Subowner, Level, Team, Sellvalue}, `Entityposition`, `State`, `Skin`, `Commandlist`, `Upgradelist`, `Taglist`, the
`Stats` list) and its server-only state (`Beingserver`, `Statsserver`, `Commandlistserver`, `Upgradelistserver`,
`Taglistserver`, `Lasthit`, `Damagetaken`, `Damagedone`) (`docs/01` §4, `docs/19` §2).

**Spawning** (read): `Helper_Entitytools.Spawn(being, position, parent, Sameasparent, style, ...)` builds a
`Link_Spawn` {Being, Position, Team, Owner, Subowners, Skin, Spawnstyle, `Preupgrades`, Parent, Destroyonparent,
`Initstats`} and calls `Game_Entity.Spawn` (Game_Entity.cs:90). Spawn styles (Link_Spawn.cs:6-12; Game_Entity.cs:118-156):

| Style | Places the thing |
| --- | --- |
| `Spawn` | only if its tile is placeable for it |
| `Forced` | always, where asked |
| `Loose` | on the nearest placeable tile within 5, or not at all |
| `Looseforced` | the nearest placeable tile within 5, else where asked |

`Sameasparent: true` (the default) copies the parent's owner and team over the ones given (`docs/reports/MECHANICS_CHECK.md`
row "Every player can use VENDCINT"). `Game_Entity.Onspawn` fires inside `Spawn`, before the entity has stats
(Game_Entity.cs:273).

**Starting** (Entity_Master.cs:286-457, read): `Start` (host only) builds the stats from `Defaultstat`, with
`Initstats` overriding any value it names (:382-386), adds one modifier per stat with a `Leveladd`, then adds every
script in `Entityobject.Aiscripts` by type name (:402-410). When `Spawnsafe` is true, `Executestart` runs in this
order: each script's `Spawn()`; every upgrade in `Initupgradelistserver` (the `Preupgrades` and anything `Onspawn`
hooks added) applied forced; the data tags; the `Loaded` event; each original script's `Starting()`; `Clientready`;
then the parent's `Child(entity)` (:286-330). So `Initstats` is how a spawner sets a thing's numbers per spawn
(a node's health, a unit's damage) without new data, and `Preupgrades` is how it hands over upgrades.

**Despawning:** `Game_Entity.Despawn(being, killed, reason)` runs the entity's `Despawn` handlers, then
`Game_Entity.Ondespawn(entity, killed, reason)` for everyone listening (reason `"Sell"` for a sale), then clears the
delegates (`docs/05` §6). A child spawned with `Destroyonparent` dies with its parent (:303-307).

## 4. Behaviour scripts (the game's AIs)

Every behaviour is an `Entity_Ai_Base` component (Entity_Ai_Base.cs). Its hooks (read):

| Hook | When | Notes |
| --- | --- | --- |
| `Spawn()` / `Starting()` | at start (§3); `Starting` after stats and upgrades exist | scripts added later by an upgrade get both from `Helper_Entitytools.Upgrade` |
| `Tick`, `Slowtick`, `Reallyslowtick` | every 0.05 s, 0.2 s and 1 s, host only, fired from `Game_Mapvars.Update` on `Time.fixedTime` (Game_Mapvars.cs:96-160) | a script that doesn't override a tick unsubscribes from it on its first call (Entity_Ai_Base.cs:66-84, 158-166), so idle scripts cost nothing |
| `Interact(link)` / `Oninteract(link)` | a hit or heal dealt / taken | one chain per entity, rebuilt by `Helper_Entitytools.Updateinteract` sorted by each script's `Priority`, low first (Helper_Entitytools.cs:122-136); the dealer's chain runs, then the target's (:157-165) |
| `Command(name, json, server)`, `Action(state)`, `Goal(...)` | a button press or command update; a state change | |
| `Upgrade(up)` / `Downgrade(up)` | an upgrade added to or removed from the entity | every script hears every upgrade |
| `Kill(victim)`, `Child(child)`, `Truemove(tile)`, `Despawn(death)` | a kill credited, a child started, a tile move, the end | |

**Where scripts come from:** the entity's `Aiscripts`, the `Aiscripts` of each upgrade applied to it
(`Helper_Entitytools.Upgrade`, which also removes every script whose type no applied upgrade or the entity lists,
:540-547), and Cardinals, whose upgrades go on the cores and from there onto other things (§10). A script reads its
numbers from `Upgradeobj.Defaultparams` (the upgrade that brought it) or from its entity's stats.

**Four ways a pack adds behaviour**, cheapest first. The fit plan sorts every scope idea into these rungs:

| Rung | What the pack ships | Example (already in the pack) |
| --- | --- | --- |
| **A. Stock data** | numbers in stock tables or stock assets: a Tide row, a stock elite on a Tide, a stock object spawned | sea weather spawns `Main_Flood`, `Main_Chrono`, `Tower_Halberdnest` (`docs/mode/SEA_CHART.md`) |
| **B. Stock script, pack data** | its own being or upgrade listing a stock script, with its own params and stats | Argo's Nauts and Yossele's units list `Entity_Ai_Mob_Combat`, `Entity_Ai_Mob_Movement` (`docs/12` §3) |
| **C. Stock pattern, pack brain** | a brain that copies a stock flow step for step, with the pack's own numbers and look | Aging's stat growth copies Melting (proven A1); VENDCINT's fall copies the vessel landing (proven V1) |
| **D. New mechanic** | a rule no stock system has | the light sum for Uloe (`docs/15` §14.7) |

Rungs A and B need no new code paths in the game's sense: the stock script does the work and is already tested by
the base game. Rung C is new code that walks a road the game already walks, so its traps are known. Rung D is where
surprises live.

## 5. Stats and modifiers

`Helper_Entitytools.Addmod(entity, stat, value, type, duration)` is the only stat change; types `Base`, `Add`,
`Pmulti`, `Multi`, `Override`; `Value_Calc = (ΣBase × ΣPmulti + ΣAdd) × ΣMulti × (1 − OverrideOn) + ΣOverride`, each
kind summed, not compounded; a `duration` makes a timed modifier that removes itself (`docs/17` §2.1, `docs/01` §6).
Setting a held modifier's `Value_Multi` (or any part) re-computes the stat at once (Link_Modifier.cs, setters call
`Changed`): this is how Melting grows its drain every second.

Stat types decide what a max change does to the current value: `Variable` keeps the fraction (proven, `docs/17` §2.1;
proven on Boutbro in RogueLab round 5 A1). A counter on the panel is a stat with `Style = Counter` added with
`Addstat`. A modifier on a stat the entity doesn't have is kept as a free marker and never applies (`docs/17` §2.1).

## 6. Upgrades, levels and effects

An upgrade is applied with `Helper_Entitytools.Upgrade(entity, up, Normal | Forced, artifact)`: it joins
`Upgradelistserver`, its scripts are added and started, every script hears `Upgrade(up)`, and the entity's `Level`
becomes the highest `Priority` ≥ 0 among its upgrades, whose next options fill the upgrade panel (`docs/17` §2.2).
Buffs and debuffs are upgrades too (`Effect_*`, Priority −1/−2) carrying an effect script; `Removeupgrade` takes one
off. The game's own effects either remove themselves after a time (Melting, Speed) or stay for the match (Fie's
Weakened from an exploding wall, Entity_Ai_Tower_Fiewall.cs:219-235). An upgrade's params apply nothing by themselves:
each script reads its own (`docs/17` §2.1).

## 7. Combat, healing and finding things

| Piece | Code (read) | Kit |
| --- | --- | --- |
| A hit | `Helper_Entitytools.Interacthurt(from, to, data)`: adds the dealer's `Damage` and sets `Lasthit` only when the data has no `Damage` (Helper_Entitytools.cs:138-165) | `docs/15` §16 |
| Damage taken | `Helper_Damagetools.Dodamage`: `Healtharmor` first (each hit costs 1 × `Damagemulti`), then `Healthover` (a hit larger than what is left passes in full), then `Healthresource` (ore, at most 1 + `Damageresource` per hit), then `Health` (Helper_Damagetools.cs:11-76); adds to `Damagetaken` and the dealer's `Damagedone` | `docs/01` §9, `docs/24` §6 |
| Healing | `Interactheal`, `Helper_Damagetools.Doheal` (the amount is the data's `Damage`) | `docs/reports/MECHANICS_CHECK.md` §3 D1 |
| Shots | `Helper_Damagetools.Projectile(entity, name, pos, data)`; a shot's hit names its shooter as the dealer | `docs/01` §9, `docs/16` §8 |
| Finding things | `Helper_Entityquerytools`: `Entityarea` (radius), `Entitybox`, `Entityray`/`Entityrayall` (a line), `Entitynearest`, `Entityfarthest`, `Entityall`, filtered by a `Link_Entityquery` {Being (for team), `Teamfor` (same or other team), `Tags`, `Style` Tag/Being/Both, `Ignoretargetable`} | `docs/05` §16 |
| Health share | `Helper_Damagetools.Getpercenthealth` sums `Health`, `Healthover`, `Healtharmor` and `Healthresource` (Helper_Damagetools.cs:173-202); the Halberd uses it to flee home when hurt (Entity_Ai_Enemy_Halberd.cs:201-215) | **new** |

## 8. Players, buttons, building and selling

- A player is a `Tds_Clientinfo` on their network player: `Hero`, `Resources` (the wallet), `Activeplayerdata`
  (Gained, Spent, kills, upgrades, vessel deaths). `Game_Players.Prevsessionlist` maps session ids to them, and keeps
  players who dropped (`docs/16` §5, `docs/19` §5).
- Ownership is `Entitydata.Owner` (a session id; −1 for the team's things) plus `Subowner`, the other ids that may
  press its buttons (Merchut lists 0-9, Entity_Ai_Tower_Merchut.cs:60) (`docs/17` §2.5).
- Buttons are commands: `Createcommand`, `Updatecommand` (synced to `Commandlist`; `run` calls the scripts' `Command`),
  `Removecommand`; a client's press arrives as `Remote.Command_ServerRpc` (`docs/01` §8; proven V2).
- `Entity_Ai_Hero_Buildsell` builds from `Buildoption1..4` (each command's `Goalvalue` scales that building's price
  and its sell value) and sells for the costs of every applied upgrade times the sell rate (`docs/16` §2, `docs/17`
  §2.7).
- The HUD (`Game_Ui`) shows the selected thing's buttons only to an owner or subowner; the choice dialog
  (`Bla_Ui_Confirmation`, `Runprompt`) is the one stock picker a mod can fill (`docs/20` §2-3).

## 9. Economy, Tides and the mode

- **Wallet:** `Helper_Resources.Giveresources(list, owner, style)` pays a player (`Default`) or splits across every
  active player (`Split`, owner −1); `Useresources`/`Checkresources` spend (Helper_Resources.cs:12-131). Pay is scaled
  by `Game_Mapvars.Resourcegainmultiplier`, spending by `Resourcelossmultiplier` (`docs/16` §5). **new:** a colour a
  player doesn't hold yet is added at the unscaled amount; only an existing entry gets the multiplier
  (Helper_Resources.cs:20-30). Every player holds all three colours after the start aura, so it never shows in play.
- **Drops** are paid split at a foe's death, never as a pickup (`docs/15` §14.7, B3). Ore nodes pay when emptied
  (`Resource_Mine` refills, `Resource_Death` dies) (`docs/16` §4).
- **The Tide table** (`Wavesobject`, read): `Resourcesinit` (start aura), `Wave` (a list of `Waves` {Displayedwave,
  Note, Wavetime, Wave (counts as a Tide), Content}), `Totalwaves`; each `Wavecontent` is {Style Spread / Gate / Wait,
  Interval, Count, Team, Enemy, Gate (which core), Upgrades (elites), Resources (pay when it starts)}.
- **`Mode_Normal.Run`** (Mode_Normal.cs:307-480, read): pays the start aura with the gain multiplier held at 1, then
  for each entry sets `Time_Wave` to its `Wavetime`, writes the banner (`Display`), runs its contents (each spawn is the
  enemy core's `Spawn` command, `{Spawn, Upgrades[]}`), waits while foes live and the clock runs, then the break. The
  map is won after the last entry and lost when the Basin dies (`Coredeath`, :236-282). Detail: `docs/13` §3,
  `docs/24` §4; editing the table live: `docs/15` §13 (proven).
- **The easier setting** is one upgrade on the core, `Entity_Ai_Main_Core_Treated`, with six params (`docs/15` §14.3).
  **new:** only one runs per map (a static guard, Entity_Ai_Main_Core_Treated.cs:127-132), and the `Mode_Treated` flag it
  sets is read in one place, the end screen's record check (Game_Endscreen.cs:56), which a modded game already fails.

## 10. Cardinals (artifacts)

Loaded at match start, applied once to the cores as upgrades; their scripts (`Entity_Ai_Artifact_Teamapply`,
`Towerapply`, `Heroapply`, `Coreapply`, `Mobapply`...) add more upgrades to things as they spawn; the HUD row keeps a
counter for 8 (`Artifacttrack`, Game_Mapvars.cs:76, 125-127; `Editartifact`). Granting one mid-run is proven (Riptide,
`docs/15` §7, §13). The 11 stock patterns are `docs/15` §18.1; what each Cardinal needs from a vessel is `docs/17` §9.

## 11. Looks, sound and the network

Visuals run on each machine from synced state and host RPCs: `Radio.Vis_ClientRpc` (a named cue such as "Attack",
"Fall"), `Vfxoneshotcheck`, `Vfxdebuffcheck` (a look under the entity, refreshed by re-sending), all sharing a budget
that drops sends past 50 in flight (`docs/19` §4, pitfall #95). Skins are one per entity. Stock looks load per map, so
pack effects carry their own (pitfall #114). Clients have no spawn event and poll (`docs/19` §2). Sound: `docs/21`.
Save, unlocks and the mod loader: `docs/23`.

## 12. Maps and map objects

The grid, placement and paths (`docs/16` §1-2); the base game's map-special objects a mod can spawn on any map, each a
stock script on a stock being (`Main_Flood`, `Main_Chrono`, `Tower_Halberdnest`, `Tower_Den`, `Main_Enemycoresplit`,
`Oroboro`; `docs/16` §3); lighting (`docs/16` §7); new maps (`docs/22`).

## 13. The stock behaviour catalogue

`ENGINE_STUDY_2026-10-28_BEHAVIOURS.md` lists every `Entity_Ai_*`, effect, command and visual script with what it
does, how it is attached, the params and stats it reads, what it hard-codes, and whether a pack can reuse it:
**data-only** (list it in pack data with pack numbers), **data + stock asset** (it also loads a stock look, shot or
being, which must be loaded on the map), **vessel-bound** (it names a base vessel's things or tags) or **no**.
Four readers went through every script of these groups, one file each; each entry was then checked against the code
in samples (`ENGINE_STUDY_2026-10-28_BEHAVIOURS.md` §1 lists the checks and the corrections).

| Group | Scripts | data-only | data + stock asset | vessel-bound | no |
| --- | --- | --- | --- | --- | --- |
| Cardinals (`Entity_Ai_Artifact_*`, `Entity_Ai_Effect_Artifact_*`) | 53 | 33 | 18 | 1 | 1 |
| Effects, enemies, cores, map objects, resources, general | 83 | 57 | 16 | 10 | 0 |
| Units, towers, traps, projectiles, helpers | 52 | 25 | 15 | 4 | 8 |
| Buttons, visual scripts, effect helpers | 96 | 50 | 31 | 6 | 9 |
| **All** | **284** | **165** | **80** | **21** | **18** |

So about seven in eight of the game's behaviour scripts can be put on pack things by data, which is why the fit plan
finds a stock road for almost every idea. The ones that can't are tied to one base vessel (Sno Sno's snow, Boutman's
rallies, Fie's shop and wall buffs), need code to arm them (`Entity_Ai_Mob_Ranged` without a shot name freezes in
`Attack`), or write the game's own unlock keys (the mercenaries' Merchut tower, Boutman's foe look).

## 14. Facts new to the kit from this reading

Written as **read**. RogueLab round 8 (2026-10-29) saw the ones marked **verified** below; they are in the kit now.
The rest go to the kit's pitfalls only once seen (PLAN Part A).

| # | Fact | Where (read) | Matters for |
| --- | --- | --- | --- |
| N1 | **The same upgrade applied twice does nothing**: `Upgrade` returns when the name is already on the entity. **Verified** (round 8: ×0.7 stayed ×0.7, one instance; a second asset with another name stacked to ×0.4); pitfall #115 | Helper_Entitytools.cs:470-473 | Direction ranks and stacked Cardinals need upgrades of their own (fit plan §4, §7) |
| N2 | **Removing an upgrade keeps a script that another applied upgrade, or the entity, still names**; only its `Downgrade` runs. **Verified** (round 8: both thorn instances kept pricking 1 + 2 after one upgrade was removed); pitfall #116 | Helper_Entitytools.cs:565-590 | two pack upgrades sharing one stock script can't be removed one at a time (an invulnerability would stay) |
| N3 | **Nine stock Cardinal scripts read `Artifactobj`** (Checklist, Coin, Computer, Greatspear, Moneybag, Moneybagold, Mooncore, Onetimebuild, Samedayshipping) and throw when listed on a plain upgrade; `Upgrade`'s fourth argument passes the Cardinal. **Verified** for Coin (round 8: NullReferenceException without it, clean with it); pitfall #117 | Helper_Entitytools.cs:464, 494; Entity_Ai_Artifact_Coin.cs:35 | pack Cardinals built on those scripts (fit plan §4) |
| N4 | **Several Cardinals scale by `30 / Totalwaves` in whole-number maths**, so any map reaches the same end value by its last Tide: exact on the campaign's 15 Tides (×2 per Tide), short on 16-29 Tides (×1), nothing past 30 (Rusty Pipe then divides by 0). **Verified** for Coin on a 3-Tide map (round 8: Red_Add 100 paid +1000 at Tide 1); the rounding down itself is read only | Entity_Ai_Artifact_Coin.cs:51-59; Caffeine, Moneybag, Opalcore, Rustypipe, Voice the same | stock and pack Cardinals on these scripts work as designed on 15-Tide maps; a map of another length should divide 30 |
| N5 | **Melting never takes its drained stats back** when it ends, and hard-codes its look `Debuff_Melting` and its `Enemyweaken` counter. **Verified** (round 8: Damage 4 → 3.18 and still 3.18 after it ended); pitfall #120 | Entity_Ai_Effect_Enemy_Weaken.cs:35-59, 61-79 (no `Downgrade`) | Aging can't borrow it (fit plan §5) |
| N6 | **A shooter's despawn kills its live shots without their death effects** (no burst, no split) | Projectile_Ai_Base.cs:74-77, 97-101 | a unit or tower that dies with lobs in the air |
| N7 | **The Chrono map runs ×1.25 before its first step**: the speed starts at 1.25 and is applied on every Tide change; kit `docs/16` §3 lists only 1.5/2/2.25. The pack's Quickening writes the speed itself, so it is not affected. **Verified** (round 8: 1.25 three seconds into Tide 1); kit `16` §3 corrected | Entity_Ai_Main_Chrono.cs:10-25, 27-54 | correcting `docs/16` §3 once seen |
| N8 | **No game AI uses randomness**: no `Entity_Ai_*` calls `Random`; every unit walks to a chosen goal | the whole decompile | Photons' wandering (fit plan §10) |
| N9 | **The easier setting runs once per map** (a static guard), and its `Mode_Treated` flag is read only by the end screen's record check | Entity_Ai_Main_Core_Treated.cs:127-132; Game_Endscreen.cs:56 | Depth from it (fit plan §2.1) |
| N10 | **A colour a player doesn't hold yet is paid at the unscaled amount**; only an existing entry gets the gain multiplier | Helper_Resources.cs:20-30 | none in play (the start aura gives every colour) |
| N11 | **Vis "Fall" has no sender**: `Entity_Vis_Hero` plays `Falling` on it, but the only stock fall cue, `Effect_Chute`'s, is "Herofall", which no visual script handles | Entity_Vis_Hero.cs:185-194; Entity_Ai_Effect_Chute.cs:7-22 | a pack drop-in look sends its own cue |
| N12 | **Boutman's foe look writes an unlock on every client that sees its KO** (`Enemy_Boutman_Defeat`) | Entity_Vis_Enemy_Boutman.cs:63-71 | never reuse that visual on a pack foe |
| N13 | **The stock "everything a vessel builds" Cardinal script reaches every vessel's buildings**: it checks only that the parent is tagged `Hero`, and its team check compares a team with itself. **Verified** (round 8: another player's building got the gift, and new ones still did after the upgrade was removed); pitfall #118 | Entity_Ai_Artifact_Teamapply.cs:10-16 | per-player Directions use the pack's own owner-filtered hook (fit plan §3) |

The kit already had the rest of what this reading touched (the Healthover overflow, the elites' removal bug, runtime
tags missing from queries #84, the bounty's missing kill check, the Treated numbers).

## 15. Not covered

Data values (stats, Tide tables, params) live in the game's bundles on the PC and were not read here; where a route
needs one, the fit plan says so. The client-side UI classes were skimmed, not read line by line (`docs/20` holds what is
known). Online behaviour is read only, as everywhere in the kit (`docs/19` §8 lists the online evening).
