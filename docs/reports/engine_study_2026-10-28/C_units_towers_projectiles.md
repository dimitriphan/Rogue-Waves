# C. Generic units, towers, traps, projectiles and their helpers

Source: the game's code as decompiled in this thread (kit `docs/13` §11 recipe); the decompiled files are never
committed. Line numbers are the file's own (`grep -n`). 52 entries: 30 unit/tower/trap AIs, `Projectile_Master` + 18
`Projectile_Ai_*` (incl. `_Base`), 3 helpers.

Shared facts used below (checked in code outside this group):
- `Helper_Entitytools.Updatecommand` sets `State` and `Commandtime`, then fires `Command`, then fires `Action` **only if the
  state changed**, and silently does nothing if the command was never created (`Helper_Entitytools.cs:293-346`). So a unit
  whose `Idle` command was never created (no `_General`/`_Invincible`/`_Merchant` on it) stays stuck in `Attack` after
  losing a target.
- AIs added by `Helper_Entitytools.Upgrade` get `Spawn()` then `Starting()` immediately (`Helper_Entitytools.cs:532-540`).
- `Interacthurt` adds `Damage` (from the attacker's `Damage` stat) and `Damagemulti`=1 only when the Data has no `Damage`,
  and only then sets `To.Lasthit` (`Helper_Entitytools.cs:150-156`). `Lasthit` is what gets `Kill(...)` and the kill count
  on death (`Game_Entity.cs:355-361`). Any hit whose Data already carries `Damage` gives no kill credit.
- The tag index `Game_Entity.Entitiesserver` is built from the **Entityobject's** `Tags` at spawn and cleaned from them at
  despawn (`Game_Entity.cs:300-309`, `400-406`). `Helper_Entitytools.Tag` only edits `Taglistserver`
  (`Helper_Entitytools.cs:413-421`), so runtime tags are invisible to `Entitynearest`/`Entityall`/`Entityfarthest`.
- A tile's `Genitem.Updatefunct` fires whenever a being is added to or removed from it (`Game_Grid.cs:997-1030`).
- Layer 6 is the map's collision mesh (`Game_Client_Init.cs:147`); every projectile AI treats layer 6 as "terrain".

---

## 1. Units (Entity_Ai_Mob_*)

### `Entity_Ai_Mob_General` (32 lines)
- **Does:** The standard damage handler for units. On `Hurt` it applies `Helper_Damagetools.Dodamage`, sends `Vis "Hurt"`,
  and despawns the unit (Death) when every health layer is at 0. On `Heal` it applies `Doheal`. It creates the `Idle`
  command at `Spawn` and enters it at `Starting`.
- **Attached by:** entity (every killable unit). It forces `Priority` 10, so it runs after the AIs that edit hit Data.
- **Reads:** interact Data `Damage` and `Damagemulti` (both required on Hurt and Heal), stats `Healtharmor`/`Healthover`/
  `Healthresource`/`Health` (each optional).
- **Hard-coded:** client vis keys `Hurt`, `Heal`.
- **Hooks:** `Spawn`, `Starting`, `Oninteract`, `Setpriority`.
- **Reuse:** data-only.
- **Cite:** `Entity_Ai_Mob_General.cs:3-31`.

### `Entity_Ai_Mob_Invincible` (12 lines)
- **Does:** Only creates and enters the `Idle` command. It has no `Oninteract`, so the unit ignores all Hurt and Heal
  (nothing lowers its health).
- **Attached by:** entity, as a replacement for `_General`.
- **Reads:** nothing.
- **Hard-coded:** none.
- **Hooks:** `Spawn`, `Starting`.
- **Reuse:** data-only. Gives an immune unit that still has a working `Idle` state for the combat loops.
- **Cite:** `Entity_Ai_Mob_Invincible.cs:3-11`.

### `Entity_Ai_Mob_Movement` (292 lines)
- **Does:** Grid walking along a nav path for the `Move` command.
  - **Goal.** The `Move` command's goal is one of two things. (a) `Goalposition` ≠ (-1,-1,-1): it goes to the nearest
    `Surface` tile within 3 of that point. (b) Otherwise `Goalobject`: it goes to the nearest standable tile next to that
    entity's tile (`Helper_Gridtools.Checkifstandablenearest`), and `Compdist` = the distance from that tile to the goal's
    tile.
  - **Player orders.** A `Move` with Json `{To}` picks the `Surface` within 0.5 of `To` and sets `Goalvalue` 0.
  - **Stepping.** The path comes from `Helper_Navtools.Getpath`. It steps one tile every 1/(`Speed`×boost) s, with `Speed`
    clamped to 0.05-12 tiles/s. It stops early once the current tile is within `Goalvalue - Compdist` of the target tile,
    so `Goalvalue` works as an attack range.
  - **Re-pathing.** If the `Goalobject` changes or the command is re-issued during a walk, it re-queries and re-paths.
  - **Panic.** If more than two re-paths keep the same next tile, it plays `Debuff_Panic` and walks 3× faster. The boost
    loses 1 per completed step.
  - **Blocked or done.** If a tile can't be entered (`Moveblock` null), or the path is invalid but non-empty, it switches
    to `Idleblocked`. When the walk ends, it switches to `Goalstring` if one is set (then clears it), else to `Idle`.
  - **Leash.** None.
  - **Slowtick (Idle/Idleblocked).** Slides the visual position (`Entityposition`) onto the unit's slot offset on its tile.
- **Attached by:** entity (units). Needs `Being.Currentblock`. Other AIs (`_Aurabeast`, `_Kippy`, `_Merchant`) drive it
  through `Commandlistserver["Move"]`.
- **Reads:** stat `Speed`. `Action` checks it with `ContainsKey`, but `Slowtick` reads it with [], so it is effectively
  required. Command fields `Goalobject`, `Goalposition`, `Goalvalue`, `Goalstring`.
- **Hard-coded:** command asset `Command_Move`; commands `Move`, `Idleblocked`; vfx `Debuff_Panic`; tag `Surface`.
- **Hooks:** `Spawn`, `Command`, `Action`, `Slowtick`, `Master.Position.OnValueChanged`.
- **Reuse:** data + stock asset (`Command_Move`).
- **Quirks:**
  - `Goalposition` is never reset to the sentinel after a walk. Only a player `Move` or another AI resets it.
  - The stop distance uses tile positions, not entity positions.
- **Cite:** `Entity_Ai_Mob_Movement.cs:32-43`, `45-58`, `65-68`, `129-150`, `176-197`, `204-213`, `233-261`, `269-291`.

### `Entity_Ai_Mob_Movementlinear` (178 lines)
- **Does:** Straight-line movement for `Movelinear`.
  - `Goalposition` holds a **direction**, not a place. `Helper_Navtools.Getpathlinear` walks tile by tile while the next
    connection's direction · Direction > 0.5 (or the step is straight up or down), and respects team nav blocks.
  - It steps every 1/`Speed` s. A new direction mid-walk re-paths.
  - At the end it switches to `Goalstring` or `Idle`. With no `Speed` stat it goes `Idleblocked`.
  - `Tick` lerps the visual onto the tile and sets the facing to the direction.
- **Attached by:** entity. Player `Movelinear` commands are accepted only from a client (`!Server`), with a 0.5 s
  cooldown. Server code (e.g. `Entity_Ai_Mob_Dog`) writes `Goalposition` directly and calls `Updatecommand`.
- **Reads:** stat `Speed`; command `Goalposition` (direction), `Goalstring`.
- **Hard-coded:** command asset `Command_Movelinear`, command `Movelinear`.
- **Hooks:** `Spawn`, `Command`, `Tick`, `Action`.
- **Reuse:** data + stock asset.
- **Cite:** `Entity_Ai_Mob_Movementlinear.cs:17-30`, `37-59`, `62-77`, `84-157`; `Helper_Navtools.cs:198-238`.

### `Entity_Ai_Mob_Melee` (139 lines)
- **Does:** Runs the `Attack` state against `Commandlistserver["Attack"].Goalobject`. It does **not** pick targets
  (`_Combat` and `_Aurabeast` do).
  - **Timing.** Lunge time = clamp(1/`Attackspeed`, 0, 0.175) s. If the attack is ready when the state is entered, the
    first strike is timed to land after one lunge. After that, one strike every 1/`Attackspeed` s.
  - **Each wait tick** (`Time_Gametick`): it aborts if the command is re-issued. It drops to `Idle` if the target is gone
    or farther than `Range`+0.1 (visual positions). During the last lunge window it plays a DOTween arc
    (self → up-mid → target → self) on the visual position.
  - **Strike.** If the target is still within `Range`+0.1: `Interacthurt(Master, target)` with no Data, so damage is the
    unit's `Damage` stat. Then `Vis "Attack"`. It loops while the state stays `Attack`.
- **Attached by:** entity, or as the base class of `_Combat`/`_Aurabeast`. Needs an `Idle` command from another AI.
- **Reads:** stats `Attackspeed`, `Range` (required), `Damage` (used by `Interacthurt` if present).
- **Hard-coded:** command `Attack`, vis key `Attack`.
- **Hooks:** `Spawn`, `Action`.
- **Reuse:** data-only, but on its own it never attacks. Pair it with a targeting AI.
- **Quirks:** An `Action("Attack")` that arrives while the old loop still runs forces `Idle`.
- **Cite:** `Entity_Ai_Mob_Melee.cs:22-27`, `44-55`, `62-66`, `76-130`.

### `Entity_Ai_Mob_Combat` (39 lines)
- **Does:** `_Melee` plus target picking. Every `Slowtick` while `Idle` or `Idleblocked`, `Helper_Entityai.Checkenemymelee`
  takes the nearest enemy (team ≠ own and ≠ -1, targetable) tagged `Mob`, `Tower` or `Resource` within `Range`+0.1 of the
  visual position, with no line-of-sight check. It sets that as the `Attack` goal and enters `Attack`.
  - **Mining.** Ore is attacked like any target. `Dodamage` treats `Healthresource` as a hit counter: each hit removes
    min(`Damage`,1) + Data `Damageresource`. `Entity_Ai_Resource_Mine` pays all players when it empties.
  - **Chasing and leash.** No chase, no leash, no retarget while moving: during `Move` it ignores enemies.
- **Attached by:** entity (fighters, gatherers). Needs `Idle` from `_General` and, to move, `_Movement`.
- **Reads:** stats `Range` (checked), `Attackspeed`, `Damage`.
- **Hard-coded:** tags `Mob`, `Tower`, `Resource`; states `Idle`, `Idleblocked`.
- **Hooks:** `Starting`, `Slowtick` (plus the inherited `_Melee` hooks).
- **Reuse:** data-only.
- **Quirks:** Ore nodes are skipped if their team is -1 (`Checkteam`). Which team map ore carries is unclear.
- **Cite:** `Entity_Ai_Mob_Combat.cs:8-38`; `Helper_Entityai.cs:91-102`; `Helper_Damagetools.cs:47-61`;
  `Entity_Ai_Resource_Mine.cs:93-128`.

### `Entity_Ai_Mob_Ranged` (137 lines)
- **Does:** A ranged auto-attack.
  - **Targeting.** On `Slowtick` while `Idle`/`Idleblocked`, `Checkenemyranged` takes the nearest **visible** enemy
    (raycast on the `Block` layer) tagged `Mob`/`Tower`/`Resource` within `Range`, and enters `Attack`.
  - **Firing.** The loop fires at once if ready, then every 1/`Attackspeed` s. It drops to `Idle` if the target is gone or
    beyond `Range` (grid positions). Each shot nudges the visual 0.25 forward, sends `Vis "Shoot"`, and fires the
    projectile **named by `Commandlistserver["Attack"].Goalstring`**. Params: `Speed`=`Projspeed`,
    `Accel`=`Projspeed`/2, `Zvelocity`=`Projspeed`, `Goal`=target. Data = a copy of the public `Hurtdict` (empty by
    default, so damage comes from the shooter's `Damage` stat).
- **Attached by:** entity. In stock it is used only on Snosno, whose hero AI sets that Goalstring to
  `Hero_Snosno_Snoball` and adds `Snoball` to `Hurtdict`.
- **Reads:** stats `Range` (checked), `Attackspeed`, `Projspeed` (required).
- **Hard-coded:** tags `Mob`, `Tower`, `Resource`; vis key `Shoot`.
- **Hooks:** `Spawn`, `Action`, `Slowtick`.
- **Reuse:** no (data-only it is inert). Nothing in data can set the Attack Goalstring; it needs mod code to name the
  projectile.
- **Quirks:** With an empty Goalstring, `Action` returns without leaving `Attack`, and `Slowtick` only retargets in
  `Idle`, so the unit freezes in `Attack`.
- **Cite:** `Entity_Ai_Mob_Ranged.cs:19-26`, `30-33`, `61-103`, `114-136`; `Entity_Ai_Hero_Snosno.cs:49-53`.

### `Entity_Ai_Mob_Aurabeast` (166 lines)
- **Does:** A neutral beast built on `_Melee`.
  - **Wandering.** While `Idle`/`Idleblocked` with no attack goal, on every 17th such slowtick (at least 3.4 s apart) it
    walks to the `Surface` nearest to one tile ahead, with the heading turned 45° further each time.
  - **Aggro.** The first Hurt from a `Gatherer` or `Hero` makes it Mad: it gets the upgrade
    `Effect_Mob_Aurabeast_Aggro_Upgradeobject`, the runtime tag `Mob`, a manual add to the `Mob` tag index, and the
    `Debuff_Alerted` vfx for 99999 s. From then on it auto-attacks like `_Combat`.
  - **Stun.** Each damaging hit from a `Gatherer` stuns it for `Stunduration`: `Speed` and `Damage` get Override mods
    (0.1 and 0.01) plus the `Debuff_Stun` vfx.
  - **Death.** Pays `Redauramine`/`Blueauramine`/`Yellowauramine` stat values as Red/Blue/Yellow aura, split among all
    players, with the `World_*aura` vfx.
- **Attached by:** entity (`Mob_Aurabeast`, spawned by `Entity_Ai_Tower_Den`). Needs `_Movement` (`Move` command) and
  `_General` (damage, `Idle`).
- **Reads:** stats `Stunduration` (required when hit by a Gatherer), `*auramine` (optional), `Range`, `Attackspeed`.
- **Hard-coded:** upgrade `Effect_Mob_Aurabeast_Aggro_Upgradeobject`; tags `Gatherer`, `Hero`, `Mob`, `Surface`; vfx
  `Debuff_Alerted`, `Debuff_Stun`, `World_Redaura`, `World_Blueaura`, `World_Yellowaura`; resources `Redaura`,
  `Blueaura`, `Yellowaura`.
- **Hooks:** `Starting`, `Slowtick`, `Oninteract`, `Despawn`.
- **Reuse:** data + stock asset.
- **Quirks:**
  - The manual add to `Entitiesserver["Mob"]` is not undone at despawn unless its Entityobject lists `Mob`, which leaves
    a destroyed reference in the index.
  - Its `Oninteract` deals no damage itself.
- **Cite:** `Entity_Ai_Mob_Aurabeast.cs:24-31`, `41-61`, `64-87`, `110-114`, `125-163`.

### `Entity_Ai_Mob_Bird` (93 lines)
- **Does:** A pop-able bird that sits 1.65 above its `Parent`.
  - **Poke.** The player command `Poke` (asset `Command_Artifact_Bird`, no state change; `Subowner` = players 0-9, so
    anyone can use it) hurts the bird itself for 10.
  - **Hits.** It applies damage itself at priority 9, then zeroes `Damage`/`Damagemulti` in the Data so later AIs deal
    none.
  - **Burst.** When its health is gone it sets `Health` to 0.1, plays `Macro_Birdburst` and `Vis "Dead"`, and waits
    0.5 s. Then it `Interacthurt`s **every enemy `Mob`/`Tower` on the map** (no radius, untargetable ones too) with its
    `Damage` stat, plays `World_Bird_Explode` and a shake, and despawns.
  - **Kills.** Each kill it scores gives every active player the full `Resource` amount of Redaura (owner -1, not
    split).
- **Attached by:** entity spawned as a child (Parent required for the hover).
- **Reads:** stats `Resource` (required at `Spawn`), `Health`, `Damage`.
- **Hard-coded:** command asset `Command_Artifact_Bird`; vfx `Macro_Birdburst`, `World_Bird_Explode`; tags `Mob`, `Tower`.
- **Hooks:** `Spawn`, `Command`, `Slowtick`, `Oninteract`, `Kill`.
- **Reuse:** data + stock asset. Gives a map-wide "smart bomb" pickup.
- **Quirks:**
  - The `Death` guard field is never set to true, so every hit during the 0.5 s wait starts another map-wide burst.
  - The bird zeroes Data on Heal too.
- **Cite:** `Entity_Ai_Mob_Bird.cs:9`, `11-21`, `24-37`, `47-66`, `68-87`, `89-92`; `Helper_Resources.cs:48-72`.

### `Entity_Ai_Mob_Kippy` (89 lines)
- **Does:** A support runner. While `Idle`/`Idleblocked` (on `Reallyslowtick`) it walks to the **farthest allied `Mob`**
  within 100, with `Goalstring` `Idlerest`. On arrival (`Action "Idlerest"`) it gives that unit a permanent `Attackspeed`
  Add of `Boost` (no duration) and the `Debuff_Kippy` vfx for 5 s. It waits 1/`Attackspeed` s, goes `Idle`, and picks the
  next target.
- **Attached by:** entity. Needs `_Movement` (`Move`) and an `Idle` command.
- **Reads:** stats `Boost`, `Attackspeed` (required).
- **Hard-coded:** tag `Mob`; command `Idlerest`; vfx `Debuff_Kippy`.
- **Hooks:** `Spawn`, `Starting`, `Action`, `Reallyslowtick`.
- **Reuse:** data + stock asset (vfx only). A buff-ferry unit.
- **Quirks:**
  - The buff stacks forever on every visit (Add mod with Duration 0).
  - If Kippy is itself an allied `Mob` and no other is in range, it can pick itself (distance 0 passes the `>=` test).
- **Cite:** `Entity_Ai_Mob_Kippy.cs:13-24`, `32-44`, `49-63`, `76-87`; `Helper_Entityquerytools.cs:291-313`.

### `Entity_Ai_Mob_Merchant` (190 lines)
- **Does:** A self-contained guard unit that handles its own damage.
  - **Targeting.** Every slowtick in `Idle`/`Move`/`Idleblocked` it sorts **all** enemy `Mob`/`Tower` on the map by
    distance and picks the nearest one **without** the `Tower_Merchut_Boost_Upgradeobject` upgrade.
  - **Chase.** It walks to that target with `Goalvalue`=`Range`, and re-issues the walk if the target moved ≥1.
  - **Leash.** With no target, or while `Idleblocked`, it walks back to its spawn tile if more than 0.5 away.
  - **Shooting.** Every 1/`Attackspeed` s, in any state, it fires `Mob_Merchant_Dart` (Goal = target, damage = its
    `Damage` stat) if the target is within `Range`.
  - **Marking.** Every Hurt it deals tags the victim with `Tower_Merchut_Boost_Upgradeobject`.
  - **Badge.** Every second it gives allied `Buster`-tagged entities `Artifact_Badge_Buster_Upgradeproperties`.
- **Attached by:** entity. Needs `_Movement`. Do not add `_General` too: its own `Oninteract` already applies damage.
- **Reads:** stats `Range`, `Attackspeed` (required), `Damage`.
- **Hard-coded:** projectile `Mob_Merchant_Dart`; upgrades `Tower_Merchut_Boost_Upgradeobject`,
  `Artifact_Badge_Buster_Upgradeproperties`; tags `Mob`, `Tower`, `Buster`, `Surface`.
- **Hooks:** `Spawn`, `Oninteract`, `Interact`, `Slowtick`, `Reallyslowtick`.
- **Reuse:** vessel-bound (Buster tag and badge, Merchut mark).
- **Quirks:** Targeting is map-wide, with no `Range` limit on the chase.
- **Cite:** `Entity_Ai_Mob_Merchant.cs:27-45`, `47-68`, `70-82`, `84-135`, `170-189`.

---

## 2. Towers (Entity_Ai_Tower_*)

### `Entity_Ai_Tower_General` (40 lines)
- **Does:** The same as `_Mob_General` (damage and heal at priority 10, `Idle` command). Also, if a tower of level ≥5
  dies, it shows the UI warning "ALERT ▪ HIGH VALUE TOWER DROWNED" with sound `Hightowerdestroyed` for 5 s.
- **Attached by:** entity (towers).
- **Reads:** Data `Damage`, `Damagemulti`; the health stats; `Entitydata.Level`.
- **Hard-coded:** the warning text and sound key `Hightowerdestroyed`.
- **Hooks:** `Spawn`, `Starting`, `Oninteract`, `Despawn`, `Setpriority`.
- **Reuse:** data-only.
- **Cite:** `Entity_Ai_Tower_General.cs:13-39`.

### `Entity_Ai_Tower_Artriobarrier` (64 lines)
- **Does:** Shared health between linked barriers.
  - **Hurt.** When this barrier is hurt, every **other** `Artriobarrier`-tagged entity on the map loses
    `Damage`×`Damagemulti` straight from `Health`, bypassing armor and overshield with no death check. Then the
    `Impact_Glass` vfx.
  - **Heal.** Adds that amount to the others' `Health`.
  - **Death.** Despawns (not death) every `Artriobarrier` and `Artrioplacement` entity on the map.
  - **Itself.** It takes no damage from this script; `_Tower_General` does that.
- **Attached by:** entity tagged `Artriobarrier`.
- **Reads:** Data `Damage`, `Damagemulti` (required on both Hurt and Heal); others' `Health`.
- **Hard-coded:** tags `Artriobarrier`, `Artrioplacement`; vfx `Impact_Glass`.
- **Hooks:** `Oninteract`, `Despawn`.
- **Reuse:** data-only (the tags are data).
- **Quirks:**
  - The query is Tag-style `Entityall`: map-wide, **any team and owner**. One player's barrier links to every barrier.
  - Linked `Health` can go below 0 without dying.
- **Cite:** `Entity_Ai_Tower_Artriobarrier.cs:8-26`, `34-45`, `49-62`; `Helper_Entityquerytools.cs:268-289`.

### `Entity_Ai_Tower_Artriominibarrier` (64 lines)
- **Does:** The same as `_Artriobarrier` with tag `Artriominibarrier`. On death it despawns only `Artriominibarrier`
  entities.
- **Attached by:** entity tagged `Artriominibarrier`.
- **Reads:** Data `Damage`, `Damagemulti`; `Health`.
- **Hard-coded:** tag `Artriominibarrier`; vfx `Impact_Glass`.
- **Hooks:** `Oninteract`, `Despawn`.
- **Reuse:** data-only.
- **Quirks:** The same map-wide, any-team link.
- **Cite:** `Entity_Ai_Tower_Artriominibarrier.cs:8-62`.

### `Entity_Ai_Tower_Barrel` (50 lines)
- **Does:** On death, spawns `Trap_Barrel_Dead` (forced, its own tile and orientation, as its child). It then spawns
  `Effect_Barrel_Boost` on every placeable `Surface` tile in a 13-tile diamond of radius 2, rotated with the barrel.
- **Attached by:** entity on a tile.
- **Reads:** nothing.
- **Hard-coded:** mobs `Trap_Barrel_Dead`, `Effect_Barrel_Boost`; tag `Surface`.
- **Hooks:** `Despawn`.
- **Reuse:** data + stock asset. Any entity can burst into barrel boosts on death.
- **Cite:** `Entity_Ai_Tower_Barrel.cs:7-22`, `24-49`.

### `Entity_Ai_Tower_Buff` (48 lines)
- **Does:** At spawn it gives its **Parent** the holder Entityobject's `Upgrades[0]` (forced), unless the Parent already
  has it. It removes that upgrade when it despawns. It creates a hidden `Effected` stat (1 while buffing; read by
  `Entity_Vis_Tower_Buff`).
- **Attached by:** entity spawned as a child; Parent required (`Spawn` null-refs otherwise).
- **Reads:** `Master.Beingserver.Entity.Upgrades[0]` (required); creates stat `Effected`.
- **Hard-coded:** none.
- **Hooks:** `Spawn`, `Despawn`.
- **Reuse:** data-only. A child that buffs its parent for as long as it lives.
- **Quirks:** It checks the Parent only once, at spawn.
- **Cite:** `Entity_Ai_Tower_Buff.cs:8-23`, `25-35`, `37-47`.

### `Entity_Ai_Tower_Converter` (146 lines)
- **Does:** Every 3 s, for each player: find their lowest resource. If the **tower owner** holds ≥`Convert` of each of
  Red/Blue/Yellow aura, that player gets +`Convert` of their lowest aura and −`Convert`/2 of each of the other two. Then
  the `World_Tower_Converter_Convert` vfx.
- **Attached by:** entity.
- **Reads:** stat `Convert` (required, read once at `Starting`).
- **Hard-coded:** resources `Redaura`, `Blueaura`, `Yellowaura`; vfx `World_Tower_Converter_Convert`.
- **Hooks:** `Starting`, `Reallyslowtick`.
- **Reuse:** data-only (stock vfx).
- **Quirks:**
  - The cost is checked against the owner but never paid.
  - It converts for **every** player.
  - If a player's lowest resource is not an aura, the previous player's conversion set is reused.
  - Later changes to `Convert` are ignored.
- **Cite:** `Entity_Ai_Tower_Converter.cs:43-91`, `94-144`.

### `Entity_Ai_Tower_Den` (151 lines)
- **Does:** A beast spawner.
  - **Spawning.** One second after `Starting` it spawns `Mob_Aurabeast` (`Mob_Largeaurabeast` if the entity is
    `Tower_Largeden_Entity`) as a forced child. Every `Addperwave` Tides it adds one more, up to `Maxunits`. Each miner
    that despawns (death or not) respawns after `Respawn` s while the den lives.
  - **When hurt.** It clears the attacker's `Attack` (or `Ore`) goal, sends a 0-damage `Hurt` from the attacker to every
    miner (which angers them through `_Aurabeast`), and sets itself `Option_Targetable` = false.
- **Attached by:** entity.
- **Reads:** stats `Respawn`, `Addperwave`, `Maxunits` (required).
- **Hard-coded:** mobs `Mob_Aurabeast`, `Mob_Largeaurabeast`; entity name `Tower_Largeden_Entity`; commands `Attack`,
  `Ore`.
- **Hooks:** `Starting`, `Oninteract`, `Despawn`, `Game_Mapvars.Wave.OnValueChanged` (removed at `Despawn`).
- **Reuse:** data + stock asset (aurabeasts only).
- **Quirks:**
  - `Retarget()` (makes it targetable again after 1 s) is never called, so after the first hit the den stays
    untargetable forever.
  - The attacker's own `Interact` hooks fire once per miner.
  - `Minersmod` and `Resources` are unused.
- **Cite:** `Entity_Ai_Tower_Den.cs:11`, `40-68`, `70-82`, `84-88`, `90-114`, `116-144`.

### `Entity_Ai_Tower_Drinkstand` (88 lines)
- **Does:** Every 1/`Attackspeed` s it fires `Tower_Drinkstand_Buff` with interaction type `Buff` at up to `Count` allies
  within `Range`, nearest first. Allies are tagged `Mobfighter`/`Towerfighter`/`Hero`, untargetable ones included; it
  skips itself, units without a `Damage` stat, and units with a `Drinkbuff` modifier. When a `Buff` hit lands on an
  ally, the ally gets the marker `Drinkbuff` (Add 1 for `Duration` s), the `Debuff_Drinkstand` vfx, and the upgrade
  `Effect_Artifact_Cubeddrink_Upgradeproperties`.
- **Attached by:** entity.
- **Reads:** stats `Attackspeed`, `Range`, `Count`, `Duration` (required).
- **Hard-coded:** projectile `Tower_Drinkstand_Buff`; upgrade `Effect_Artifact_Cubeddrink_Upgradeproperties`; vfx
  `Debuff_Drinkstand`; modifier `Drinkbuff`; tags as above.
- **Hooks:** `Spawn`, `Interact`, `Slowtick`.
- **Reuse:** data + stock asset (only the Cubeddrink buff).
- **Quirks:**
  - The "already buffed" guard in `Applyeffects` checks the **stand's** `Modifiers`, not the target's.
  - It never removes the Cubeddrink upgrade (unclear whether that upgrade expires itself).
- **Cite:** `Entity_Ai_Tower_Drinkstand.cs:11`, `15-25`, `27-44`, `46-54`, `56-87`.

### `Entity_Ai_Tower_Halberdnest` (66 lines)
- **Does:** On each Tide ≥1:
  - adds `Healthperwave` to its `Currentmax` stat, which `Entity_Ai_Enemy_Halberd` reads as bonus health;
  - on the first Tide, and on every Tide divisible by `Waveperunit`, spawns `Enemy_Halberd` as a forced child (its team)
    with the `World_Enemyspawnborder` vfx.
  Each halberd respawns `Respawn` s after it despawns, while the nest lives.
- **Attached by:** entity.
- **Reads:** stats `Healthperwave`, `Waveperunit`, `Respawn` (required); creates a mod on `Currentmax`.
- **Hard-coded:** mob `Enemy_Halberd`; command `Goal`; vfx `World_Enemyspawnborder`.
- **Hooks:** `Spawn`, `Game_Mapvars.Wave.OnValueChanged`.
- **Reuse:** data + stock asset (halberds only).
- **Quirks:** It never unsubscribes from `Wave` (there is no `Despawn`), so `Changed` still runs after the nest is gone.
  Unclear whether that throws or spawns.
- **Cite:** `Entity_Ai_Tower_Halberdnest.cs:19-38`, `40-65`; `Entity_Ai_Enemy_Halberd.cs:258-260`.

### `Entity_Ai_Tower_Merchut` (245 lines)
- **Does:** A bounty board with 20 contracts in hard-coded lists (count, enemy type, elite upgrade, reward ×
  `Resource`).
  - **Accepting.** The player command `Contracted` (when Goalvalue is 0) plays `Bounty_Accept` and arms the contract.
  - **Spawning.** On the next Tide it spawns `Count` enemies, one per second, through the first `Enemycore`'s `Spawn`
    command, with upgrades [elite, `Tower_Merchut_Boost_Upgradeobject`].
  - **Paying.** At the next "Between" display state it pays the reward in all three auras, split among players, then
    loads the next contract.
  - **Unlocks.** From contract `Busterunlock` on, it writes the unlock `Map_Mercs_Trust`, and after the last contract
    `Map_Mercs_Finish`. With no `Hero_Buster` player it also shows UI warnings (at `Busterunlock`/2 and at
    `Busterunlock`) and sets the skin `Nobuster` at `Busterunlock`. With a Buster player the skin is `Nobuster` from the
    first contract, with no warnings.
- **Attached by:** entity or upgrade (`Downgrade` unsubscribes only for its own `Upgradeobj`).
- **Reads:** stats `Resource`, `Busterunlock` (required); `Display.Display_State`.
- **Hard-coded:**
  - command assets `Command_Proxy`, `Command_Tower_Merchut`;
  - enemies `Enemy_Dagger`…`Enemy_Onager`; elites `Enemy_Armor/Hyper/Health/Chaos_Upgradeobject`;
  - tag `Enemycore`; hero `Hero_Buster`; sound `Bounty_Accept`;
  - warning sounds `Map_Bounty1-3`; unlock keys `Map_Mercs_Trust`, `Map_Mercs_Finish`; skin `Nobuster`.
- **Hooks:** `Spawn`, `Starting`, `Command`, `Downgrade`, `Despawn`, `Reallyslowtick`, `Wave.OnValueChanged`.
- **Reuse:** no. It is map-bound (needs an Enemycore, Buster checks, and writes the game's own unlock flags).
- **Quirks:**
  - The reward does not depend on killing the spawned enemies.
  - `Enemycorelist[0]` throws if there is no Enemycore.
- **Cite:** `Entity_Ai_Tower_Merchut.cs:10-32`, `52-71`, `73-97`, `127-191`, `193-244`.

### `Entity_Ai_Tower_Sandbox` (298 lines)
- **Does:** Sandbox admin panel. Player commands (none change state):
  - pick the enemy to spawn, cycling 9 `Enemy_*`; pick the elite, cycling 7;
  - `Spawn`; `Money`; `Clearmoney`;
  - `Clearenemy` (despawns every enemy `Mob`/`Tower` on the map);
  - `Nexttide` (bumps `Entry`, restores `Wave`, sets `Time_Wave` 0.1; 5 s cooldown);
  - `Pause` (holds `Time_Wave`); `Forward` (`Time_Scale` 3);
  - `Godmode` (`Health` Override 1e8 on every `Core`/`Hero`, any team).
  It also marks every spawned entity's stats as exposed in the UI (`Display_Exposed` for stats with an icon).
- **Attached by:** entity on a map with `Game_Admin.Instance` (used without a null check at `Spawn`).
- **Reads:** nothing from data.
- **Hard-coded:** the `Command_Tower_Sandbox_*` command assets; the `Enemies`/`Elites` lists; tags `Core`, `Hero`, `Mob`,
  `Tower`.
- **Hooks:** `Spawn`, `Command`, `Tick`, `Game_Entity.Onspawn` (never removed).
- **Reuse:** no. It is a debug tool that needs `Game_Admin`.
- **Cite:** `Entity_Ai_Tower_Sandbox.cs:33-35`, `51-96`, `98-267`, `269-297`.

### `Entity_Ai_Tower_Snare` (40 lines)
- **Does:** Each slowtick it looks for enemy `Mob`s (untargetable included) within 0.25 of its position. If any are
  found, it gives each one `Effect_Artifact_Tackle_Upgradeproperties` (unless they have it) and despawns itself (Death).
- **Attached by:** entity on a tile.
- **Reads:** nothing.
- **Hard-coded:** upgrade `Effect_Artifact_Tackle_Upgradeproperties`; tag `Mob`.
- **Hooks:** `Spawn`, `Slowtick`.
- **Reuse:** data + stock asset (only the Tackle effect).
- **Cite:** `Entity_Ai_Tower_Snare.cs:11-39`.

### `Entity_Ai_Tower_Tilebuff` (66 lines)
- **Does:** Whenever its tile's occupants change (and at spawn), it gives every **same-team** being on the tile whose
  Entityobject `Tags` contain `Tower` the holder's `Upgrades[0]`. It removes the upgrade from everything it ever buffed
  when it despawns. `Effected` counts the beings it covers.
- **Attached by:** entity on a tile.
- **Reads:** `Entity.Upgrades[0]` (required); creates stat `Effected`.
- **Hard-coded:** tag `Tower` (data tags).
- **Hooks:** `Spawn`, `Despawn`, `Genitem.Updatefunct`.
- **Reuse:** data-only. A plinth or foundation that buffs the tower built on it.
- **Quirks:**
  - A tower that leaves the tile keeps the buff.
  - Two buffers on one tile share one upgrade, so the first to despawn strips it.
- **Cite:** `Entity_Ai_Tower_Tilebuff.cs:9-31`, `33-48`, `50-65`.

### `Entity_Ai_Tower_Tilebuffany` (66 lines)
- **Does:** The same as `_Tilebuff`, but it buffs **any** non-dead being on the tile, of any team (enemies walking over
  it included).
- **Attached by:** entity on a tile.
- **Reads:** `Entity.Upgrades[0]`; `Effected`.
- **Hard-coded:** none.
- **Hooks:** as `_Tilebuff`.
- **Reuse:** data-only.
- **Quirks:** The buff persists after a unit walks off, until the buffer despawns.
- **Cite:** `Entity_Ai_Tower_Tilebuffany.cs:17-29`.

### `Entity_Ai_Tower_Tilebuffanyonly` (75 lines)
- **Does:** The same as `_Tilebuffany`, without the `Dead` filter. On every occupant change it also **removes** the
  upgrade from anyone who has left the tile. That makes it a true "while standing here" zone.
- **Attached by:** entity on a tile.
- **Reads:** `Entity.Upgrades[0]`; `Effected`.
- **Hard-coded:** none.
- **Hooks:** as `_Tilebuff`.
- **Reuse:** data-only. A buff or debuff tile (hazard, spring, pad).
- **Cite:** `Entity_Ai_Tower_Tilebuffanyonly.cs:15-39`.

---

## 3. Traps (Entity_Ai_Trap_*)

### `Entity_Ai_Trap_General` (22 lines)
- **Does:** Damage and heal like `_Mob_General` (priority 10), but `Addressdamage(Kill:false)`: a trap at 0 health is
  never despawned by damage. It creates no `Idle` command.
- **Attached by:** entity (traps).
- **Reads:** Data `Damage`, `Damagemulti`; the health stats.
- **Hard-coded:** vis keys `Hurt`, `Heal`.
- **Hooks:** `Oninteract`, `Setpriority`.
- **Reuse:** data-only. Gives damage numbers on something that cannot die from damage.
- **Cite:** `Entity_Ai_Trap_General.cs:3-21`.

### `Entity_Ai_Trap_Iceunit` (68 lines)
- **Does:** A frozen enemy.
  - **Setup.** For each `Defaultparams` entry (enemy name → number) it adds that number to `Healthresource`; the
    **last** key becomes `Tospawn`, and the skin is set to that name.
  - **Each Tide.** It sets `Healthresource` = its `Value_Calc` − Tide number. When that reaches ≤0 it hatches: it spawns
    `Tospawn` (forced, as a child of the nearest `Enemycore` to the origin, so on the enemy team), with preupgrade
    `Enemy_Health_Upgradeobject` unless the enemy is `Enemy_Excalibur`. It points that Enemycore's `Goal` command at the
    nearest `Core` within 999, plays `World_Enemyspawnborder`, and despawns itself (Death).
- **Attached by:** **upgrade only**: `Spawn` reads `Upgradeobj.Defaultparams`, which is null on entity AIs.
- **Reads:** Defaultparams (any keys; key = enemy mob name, value = Tide count, at least one required); stat
  `Healthresource` (required).
- **Hard-coded:** tags `Enemycore`, `Core`; upgrade `Enemy_Health_Upgradeobject`; vfx `World_Enemyspawnborder`; command
  `Goal`.
- **Hooks:** `Spawn`, `Despawn`, `Wave.OnValueChanged` (removed at `Despawn`).
- **Reuse:** data + stock asset. A mod's upgrade can carry its own `{Enemy_X: N}` to hatch any stock enemy at Tide N.
- **Quirks:**
  - It overwrites the ice's health from the Tide each wave, so hits on it don't carry over.
  - The countdown uses the absolute Tide, not the number of Tides since it was placed.
  - If no Core or Enemycore exists, it just despawns.
- **Cite:** `Entity_Ai_Trap_Iceunit.cs:16-47`, `49-61`.

### `Entity_Ai_Trap_Fierally` (8 lines)
- **Does:** Creates a `Parent` proxy command whose `Goalobject` is `Master.Parent`, and pushes it to clients so client
  visuals can find the parent.
- **Attached by:** entity spawned as a child.
- **Reads:** `Parent`.
- **Hard-coded:** command asset `Command_Proxy`.
- **Hooks:** `Spawn`.
- **Reuse:** data + stock asset. No gameplay effect.
- **Cite:** `Entity_Ai_Trap_Fierally.cs:3-7`.

### `Entity_Ai_Trap_Fishnoplacement` (8 lines)
- **Does:** Identical to `_Fierally` (a `Parent` proxy command synced to clients).
- **Attached by:** entity spawned as a child.
- **Reads:** `Parent`.
- **Hard-coded:** `Command_Proxy`.
- **Hooks:** `Spawn`.
- **Reuse:** data + stock asset. No gameplay effect.
- **Cite:** `Entity_Ai_Trap_Fishnoplacement.cs:3-7`.

---

## 4. Projectiles

How a projectile works end to end:

1. **Firing.** Code calls `Helper_Damagetools.Projectile(shooter, name, pos, Projdata, Interactiontype="Hurt")`. No
   script in this group fires a data-chosen projectile: `_Mob_Ranged` needs a code-set Goalstring, and the towers name
   their own.
2. **Setup.** The `Projectileobject` (`Getload("Projectile", name)`) gives the physics `Raytype`, `Radius`, the
   `Controllers` (the `Projectile_Ai_*` scripts, added by type name), `Defaultparam`, the vfx and sfx.
3. **Params.** The per-shot `Params` override the asset's `Defaultparams` key by key. `Paramsobject` (usually `Goal`),
   `Paramsposition` and `Data` (the hit's Data dict) are passed through.
4. **Hits.** Only the host deals hits, through `Projectile_Master.Interact(To)`, which sends a copy of `Data` as `Hurt`
   (`Interacthurt`) or as another interaction type.
5. **Clients.** They get a visual copy over RPC when `Clientreplicate` is set. `Data` is not sent.

### `Projectile_Master` (351 lines)
- **Does:** A pooled projectile body. Each `OnEnable` it resets velocity, force, scale and Hitposition, instantiates `Vis`
  and `Hit`, binds a physics mode, adds one `Projectile_Ai_*` per `Controllers` entry and calls their `Spawn`, plays
  `Vfxstart`/`Sfxstart`, and runs one `Tick`.
  - **Physics modes.** `Regularphysics`: a Rigidbody; the `Hit` prefab's colliders become layer-8 triggers whose
    Enter/Stay/Exit call `Collide`/`Collidestay`/`Collideexit`. `Spherecast` (`Reference.Radius`) and `Raycast` cast
    along each fixed step and call `Collide` for every hit. `Nocollision` just moves.
  - **Velocity.** Velocity += `Force`×dt each fixed step.
  - **Tick.** AI `Tick` runs every `Time_Projtick`.
  - **Kill(Death).** Moves to `Hitposition`, then calls `Remove` hooks, then (only if Death) the `Despawn` hooks. It
    destroys the AIs and, on death, plays `Vfxend`/`Sfxend`. It deactivates after `Vfxdead`×2 s, or at once.
- **Attached by:** the projectile prefab (`Game_Entity.Projectile_Instance`).
- **Reads:** `Additionalparams`, `Additionalvfxparams` (pushed into the VFX graph floats), `Additionalobjectparams`,
  `Additionalpositionparams`, `Data`, `Interactiontype`.
- **Hard-coded:** child transforms `Entity`, `Vis`; layer 8; masks `Block`, `Entity`.
- **Hooks:** `FixedUpdate`, `OnEnable`.
- **Reuse:** data-only (a mod's own `Projectileobject` picks any controllers and params).
- **Quirks:**
  - `Vfxdead` waits twice the stated time (`×2000` ms).
  - Cast hits in one step reach `Collide` in engine order, not nearest-first.
  - Interact only happens on the host (`Ishost`).
  - Hits that carry `Damage` in Data give no kill credit (see the shared facts above).
- **Cite:** `Projectile_Master.cs:89-145`, `147-254`, `256-269`, `271-334`, `336-350`; `Projectileobject.cs:17-72`.

### `Projectile_Ai_Base` (152 lines)
- **Does:** The base class. It subscribes the AI's `Collide`/`Collidestay`/`Collideexit`/`Tick`/`Despawn`/`Remove` to the
  projectile; a non-overridden virtual unsubscribes itself on first call. It also subscribes to the **shooter's**
  `Despawn`, which kills the projectile with `Death:false`.
- **Attached by:** base class only.
- **Reads:** nothing.
- **Hard-coded:** none.
- **Hooks:** `Awake`, `OnDestroy`, `Entity_Master.Despawn` of the shooter.
- **Reuse:** n/a (base class).
- **Quirks:** When the shooter dies or despawns, all its live projectiles vanish without their death effects (no
  explosion, no split).
- **Cite:** `Projectile_Ai_Base.cs:74-77`, `79-103`.

### `Projectile_Ai_Simple` (151 lines)
- **Does:** A straight or ballistic shot.
  - **Launch.** If a `Goal` is set it faces the goal, then sets velocity from `Xvelocity`/`Yvelocity`/`Zvelocity` along
    its right/up/forward axes, and `Force` = up×`Gravity`.
  - **Terrain.** It dies on terrain unless `Collision`≠1.
  - **Entities.** It hits any entity collider **not on the shooter's team**, or any team with `Any`=1. Neutral (-1) and
    untargetable entities count too.
  - **Pierce.** With `Pierce`≤1 it hits once and dies. Otherwise it hits each new target once until `Pierce` targets
    have been hit.
  - **`Interact`.** 0 = pass through entities; 1 = hit; ≥2 = die or pierce without dealing the hit.
  - **Arming.** Collisions are ignored for the first `Activetime` s.
  - **Lifetime.** It dies after `Lifetime` s (default 10).
- **Attached by:** a `Projectileobject`'s Controllers.
- **Reads:** params `Gravity`, `Lifetime`, `X/Y/Zvelocity`, `Interact`, `Collision`, `Pierce`, `Activetime`, `Any` (all
  optional); object `Goal` (optional).
- **Hard-coded:** layer 6.
- **Hooks:** `Spawn`, `Collide`, `Tick`.
- **Reuse:** data-only.
- **Quirks:** Here `Activetime` is an arming delay. In `_Delay` the same key is a fuse.
- **Cite:** `Projectile_Ai_Simple.cs:24-80`, `82-140`, `142-150`.

### `Projectile_Ai_Lob` (177 lines)
- **Does:** An arc onto a target.
  - **Aim.** It aims at the ground under `Goal`: a raycast from `Goal`+up down 10 on `Block`. It solves the launch
    velocity so the shot lands there in `Flytime` s (default 2) under `Gravity`.
  - **Hit.** The first enemy entity it touches (any team with `Any`=1) gets one hit (none if `Interact`≥2). After that,
    `Interact` becomes 2.
  - **Sticking.** It sticks to what it hit (`Sticky`, default 1), or to terrain, and dies `Delay` s after impact.
  - **Lifetime.** `Lifetime`+`Delay`.
  - **VFX.** It sets the VFX float `Speed` to 0 on impact.
- **Attached by:** Controllers.
- **Reads:** object `Goal` (**required**: dereferenced unguarded at `:135`); params `Gravity`, `Interact`, `Activetime`,
  `Any`, `Sticky`, `Flytime`, `Delay`, `Lifetime` (optional).
- **Hard-coded:** layer 6; VFX property `Speed`.
- **Hooks:** `Spawn`, `Collide`, `Tick`.
- **Reuse:** data-only. Pair it with `_Explosion` for a mortar.
- **Quirks:**
  - The terrain and client stick offset is stored in world space but re-applied as a local point.
  - A null `Goal` throws.
- **Cite:** `Projectile_Ai_Lob.cs:36-83`, `99-140`, `143-176`.

### `Projectile_Ai_Home` (141 lines)
- **Does:** A homing missile. Each tick it adds (direction to target)×`Accel` to its velocity, capped at `Speed`. It hits
  only the `Goal` entity (on contact, or when within one step) and then dies. If the target is gone it flies to the
  last known point and dies without a hit. It always dies on terrain. Optional kick from `X/Y/Zvelocity`.
- **Attached by:** Controllers.
- **Reads:** object `Goal` (**required**); params `Speed`, `Accel` (**required**); `X/Y/Zvelocity`, `Interact`,
  `Lifetime`, `Activetime` (optional).
- **Hard-coded:** layer 6.
- **Hooks:** `Spawn`, `Collide`, `Tick`.
- **Reuse:** data-only.
- **Quirks:**
  - A null `Goal` throws: the else-branch dereferences it.
  - The `Collision` param is read but never used.
- **Cite:** `Projectile_Ai_Home.cs:24-58`, `71-82`, `103-106`, `114-139`.

### `Projectile_Ai_Track` (121 lines)
- **Does:** A perfect tracker. Each tick its velocity points straight at the target, and its speed grows by `Accel`×dt up
  to `Speed`. It hits only `Goal`. It dies on terrain only when `Collision`=1 (the default).
- **Attached by:** Controllers.
- **Reads:** params `Speed`, `Accel` (**required**); object `Goal`; `X/Y/Zvelocity`, `Interact`, `Collision`, `Lifetime`
  (optional).
- **Hard-coded:** layer 6.
- **Hooks:** `Spawn`, `Collide`, `Tick`.
- **Reuse:** data-only. These params match what `_Mob_Ranged` sends.
- **Quirks:** Without a `Goal` it flies to the world origin.
- **Cite:** `Projectile_Ai_Track.cs:20-50`, `61-91`, `94-120`.

### `Projectile_Ai_Beam` (182 lines)
- **Does:** A beam from a fixed origin.
  - **Aim.** The aim point moves toward the target at `Trackspeed` units/s (`Track`=1). With no target, or with
    `Track`=0, the aim extends to `Maxrange`−0.1 along its line.
  - **Hits.** Every 1/`Tickrate` s it raycasts from the origin to the aim point and hits **only the nearest** enemy entity
    in front of the first terrain block (terrain is ignored if `Collide`=0). It moves its visual end to the hit point.
  - **Death.** It dies when its lifetime is spent (each pulse costs 1/`Tickrate`), when the beam gets longer than
    `Maxrange`, or when its target dies (`Killontarget`).
- **Attached by:** Controllers.
- **Reads:** object `Goal`; params `Trackspeed` (default 10), `Tickrate` (10), `Maxrange` (5), `Interact`,
  `Killontarget`, `Track`, `Lifetime`, `Collide` (optional).
- **Hard-coded:** layer 6; masks `Block`, `Entity`.
- **Hooks:** `Spawn`, `Tick`.
- **Reuse:** data-only.
- **Quirks:**
  - Pulses are limited by `Time_Projtick`, so a high `Tickrate` lengthens the real lifetime.
  - With `Interact`=0 the visual end never moves.
- **Cite:** `Projectile_Ai_Beam.cs:34-88`, `90-181`.

### `Projectile_Ai_Expand` (74 lines)
- **Does:** A growing shockwave. It tweens its scale from `Initradius`×2 to `Radius`×2 over `Lifetime` s, then dies. Each
  new enemy collider it touches (any team with `Any`=1) is hit once, up to `Pierce` targets (default 10).
- **Attached by:** Controllers. The hit area grows only with `Raytype` `Regularphysics` (scaled trigger colliders); a
  spherecast radius stays fixed.
- **Reads:** params `Radius`, `Initradius`, `Lifetime`, `Any`, `Pierce` (optional).
- **Hard-coded:** layer 6.
- **Hooks:** `Spawn`, `Collide`.
- **Reuse:** data-only.
- **Quirks:** The DOTween is not killed if the projectile dies early. Its `onComplete` still calls `Kill` on the pooled
  object.
- **Cite:** `Projectile_Ai_Expand.cs:22-33`, `35-73`.

### `Projectile_Ai_Delay` (22 lines)
- **Does:** A fuse: it kills the projectile (Death) `Activetime` s after spawn (default 1).
- **Attached by:** Controllers.
- **Reads:** param `Activetime` (optional).
- **Hard-coded:** none.
- **Hooks:** `Spawn`, `Tick`.
- **Reuse:** data-only. Pair it with `_Explosion` for a timed bomb.
- **Quirks:** `Activetime` is also `_Simple`'s and `_Lob`'s arming delay, so the same key arms and detonates at once.
- **Cite:** `Projectile_Ai_Delay.cs:5-21`.

### `Projectile_Ai_Explosion` (54 lines)
- **Does:** On death (`Despawn`, Death only), at the hit point, it hits up to `Count` (default 999) **enemy-team**
  entities within `Radius` (default 1), nearest first, untargetable included, with the projectile's Data and interaction
  type. Camera shake unless `Vis`=0.
- **Attached by:** Controllers.
- **Reads:** params `Radius`, `Count`, `Vis` (optional).
- **Hard-coded:** none.
- **Hooks:** `Spawn`, `Despawn`.
- **Reuse:** data-only.
- **Quirks:**
  - It does not fire when the projectile is removed without death (shooter despawned).
  - The physics query is not de-duplicated per entity.
- **Cite:** `Projectile_Ai_Explosion.cs:15-53`; `Helper_Entityquerytools.cs:159-182`.

### `Projectile_Ai_Explosion_Any` (55 lines)
- **Does:** The same as `_Explosion`, but it selects by tag `Mob`/`Tower`/`Trap` of **any team**. That is friendly fire
  and can include the shooter.
- **Attached by:** Controllers.
- **Reads:** `Radius`, `Count`, `Vis`.
- **Hard-coded:** tags `Mob`, `Tower`, `Trap`.
- **Hooks:** `Spawn`, `Despawn`.
- **Reuse:** data-only.
- **Cite:** `Projectile_Ai_Explosion_Any.cs:32-33`.

### `Projectile_Ai_Explosioncollide` (89 lines)
- **Does:** An impact burst. Each time it touches an enemy entity (at most once per fixed frame), it hits enemy-team
  entities within `Radius`, nearest first, with the `General_Explosionsmall` vfx and a shake. It does **not** kill the
  projectile. `Count` is shared across all bursts.
- **Attached by:** Controllers.
- **Reads:** `Radius`, `Count`, `Vis` (optional).
- **Hard-coded:** vfx `General_Explosionsmall`; layer 6.
- **Hooks:** `Spawn`, `Collide`.
- **Reuse:** data + stock asset.
- **Cite:** `Projectile_Ai_Explosioncollide.cs:17-36`, `38-88`.

### `Projectile_Ai_Splitbounce` (106 lines)
- **Does:** On death (host only) it does one of two things, set by the `Splitbounce` param.
  - **`Splitbounce`=1 (split).** Fires 3 fixed-velocity child projectiles named by **its own projectile name**:
    `Tower_Smackybomber_Dynamite` → `_Dynamitestick`, `_Bigbomb` → `_Smallbomb`, anything else → `_Minibomb`.
  - **`Splitbounce`=2 (bounce).** Re-fires **a copy of itself** at the farthest enemy `Mob`/`Tower` within
    `Bouncerange`, with an arc kick (Y = 5 + dy/2, X = dx, Z = dz) and `Activetime` 0.3.
  - **Data.** Both use Data `{Bomb:0}`, so damage comes from the shooter's `Damage` stat, and the Smackybomber AI reads
    the `Bomb` key to slow targets.
- **Attached by:** Controllers.
- **Reads:** params `Splitbounce`, `Bouncerange` (optional).
- **Hard-coded:** projectile names `Tower_Smackybomber_*`; tags `Mob`, `Tower`; Data key `Bomb`.
- **Hooks:** `Spawn`, `Despawn`.
- **Reuse:** Split is vessel-bound (Smacky bombs). Bounce is data-only.
- **Quirks:**
  - If `Splitbounce`=2 sits in the asset's own `Defaultparam`, each copy bounces again, so the chain is unbounded while
    targets exist.
  - The bounce arc assumes about a 1 s flight under the asset's gravity (unclear for other gravities).
- **Cite:** `Projectile_Ai_Splitbounce.cs:21-42`, `44-104`; `Entity_Ai_Tower_Smackybomber.cs:219`.

### `Projectile_Ai_Corebomb` (193 lines)
- **Does:** The core-loss bomb fired by `Mode_Normal`.
  - **Path.** The host builds a nav path from `Start` to `Goal`, tweens along it over `Time` s, and replicates it to
    clients as `Main_Core_Corebomb` (`Style` 0) or `Main_Core_Ind`.
  - **Hurt=1, in flight.** Every enemy `Mob`/`Tower` on `Goal`'s team that comes within 5 is hurt once (From = itself)
    for 20 × (its stacking `Coredamaged` modifier count).
  - **Hurt=1, on arrival.** Every remaining one **on the map** is hurt the same way.
- **Attached by:** Controllers (the `Main_Core_*` projectiles).
- **Reads:** objects `Goal`, `Start` (required for a path); params `Time`, `Style`, `Hurt` (optional).
- **Hard-coded:** projectile names `Main_Core_Corebomb`, `Main_Core_Ind`; modifier `Coredamaged`; damage 20 and range 5
  are constants.
- **Hooks:** `Spawn`, `Tick`, `Despawn`.
- **Reuse:** no. It is mode-bound and replicates itself under fixed names.
- **Quirks:**
  - The self-inflicted hurt gives no kill credit.
  - Damage escalates with every core bomb ever taken.
- **Cite:** `Projectile_Ai_Corebomb.cs:46-127`, `128-137`, `144-192`; `Mode_Normal.cs:431-460`.

### `Projectile_Ai_Excaliburbolt` (31 lines)
- **Does:** It pierces everything. On each new enemy it touches, it drops a bomb with `Goal` = that enemy and
  `Activetime` = 2 + (number of enemies hit so far): `Enemy_Excalibur_Bomblarge` for the first, `Enemy_Excalibur_Bomb`
  for the rest.
- **Attached by:** Controllers (`Enemy_Excalibur_Bolt`, fired by `Entity_Ai_Enemy_Excalibur`).
- **Reads:** nothing.
- **Hard-coded:** projectiles `Enemy_Excalibur_Bomblarge`, `Enemy_Excalibur_Bomb`.
- **Hooks:** `Collide`.
- **Reuse:** data + stock asset (stock bombs only).
- **Cite:** `Projectile_Ai_Excaliburbolt.cs:8-30`; `Entity_Ai_Enemy_Excalibur.cs:47`.

### `Projectile_Ai_Sno` (57 lines)
- **Does:** On death (host), on the nearest `Surface` within 1:
  - with param `Box`: calls the shooter's `Entity_Ai_Tower_Snosnobox.Addsno(tile, Paramsobject["Ore"])`;
  - otherwise, if no `Sno`-named entity is on the tile: spawns `Effect_Snosnosnow` there (team -2, child of the shooter,
    `Level` = `Am`);
  - otherwise: sends a `Grow` interact `{Amount: Am, Spread: 1}` to that Sno.
- **Attached by:** Controllers.
- **Reads:** param `Am` (required), `Box` (optional); object `Ore`.
- **Hard-coded:** mob `Effect_Snosnosnow`; entity name `Sno`; script `Entity_Ai_Tower_Snosnobox`; interact type `Grow`.
- **Hooks:** `Despawn`.
- **Reuse:** vessel-bound (Snosno).
- **Cite:** `Projectile_Ai_Sno.cs:7-56`.

### `Projectile_Ai_Snoball` (14 lines)
- **Does:** At spawn it writes `Damage` = the shooter's current `Damage` stat and `Damagemulti` = `Multi` (default 1) into
  the hit Data. The damage is snapshotted when the shot is fired.
- **Attached by:** Controllers.
- **Reads:** param `Multi` (optional); shooter stat `Damage` (required).
- **Hard-coded:** none.
- **Hooks:** `Spawn`.
- **Reuse:** data-only. Gives any projectile a damage multiplier.
- **Quirks:**
  - It throws if the shooter already put `Damage` in Data.
  - Because Data carries `Damage`, its hits give **no kill credit** (`Lasthit` is not set).
- **Cite:** `Projectile_Ai_Snoball.cs:5-13`; `Helper_Entitytools.cs:150-156`.

### `Projectile_Ai_Snoble` (57 lines)
- **Does:** On death (host), on the nearest `Surface` within 1, it spawns (loose, child of the shooter) a `Mob_Snoble`
  (`Type` 0/1) or a `Mob_Snoblemini` (`Type` 2/3). Types 1 and 3, or landing on a `Snoblack` Sno, add the Black upgrade,
  and the black types add Data `Black`=1.
- **Attached by:** Controllers.
- **Reads:** params `Type` (0-3), `Skin` (read but unused).
- **Hard-coded:** mobs `Mob_Snoble`, `Mob_Snoblemini`; upgrades `Mob_Snoble_Black_Upgradeobject`,
  `Mob_Snoblemini_Black_Upgradeobject`; entity name `Sno`; tag `Snoblack`.
- **Hooks:** `Spawn`, `Despawn`.
- **Reuse:** vessel-bound (Snosno). The spawn-on-landing pattern is general, but the names are fixed.
- **Cite:** `Projectile_Ai_Snoble.cs:13-56`.

### `Projectile_Ai_Snosnocannon_Laser` (32 lines)
- **Does:** A damage field. When an enemy entity **enters** the trigger it is hit. While an enemy `Mob` **stays** inside,
  it is re-hit every 0.3 s, using a 0.3 s `Laserhit` Multi modifier as the cooldown marker.
- **Attached by:** Controllers. Needs `Regularphysics` (only triggers send `Collidestay`); its lifetime must come from
  another AI (e.g. `_Delay`).
- **Reads:** nothing.
- **Hard-coded:** modifier `Laserhit`; tag `Mob`.
- **Hooks:** `Collide`, `Collidestay`.
- **Reuse:** data-only (the name says Snosno, but the code is generic).
- **Cite:** `Projectile_Ai_Snosnocannon_Laser.cs:8-31`; `Entity_Projectileobject_Connect.cs:11-33`.

---

## 5. Helpers

### `Helper_Damagetools` (389 lines)
- `Dodamage(Master, Link)`: applies one Hurt and returns a fraction for the vis. It assumes `Damagemulti` is present
  whenever `Damage` is. Layers are tried in order:
  - `Healtharmor` > 0: loses exactly 1×`Damagemulti` per hit (damage ignored);
  - `Healthover`: absorbs `Damage`×multi;
  - `Healthresource` > 0: loses min(`Damage`,1) + Data `Damageresource`, ignoring multi;
  - `Health`: loses `Damage`×multi.
- `Doheal(Master, Link)`: heals `Damage`×`Damagemulti` through `Healthresource` → `Health` → `Healthover` →
  `Healtharmor`. Both keys are required. `Armorhealmulti` is optional.
- `Addressdamage(Master, Kill=true)`: true once every existing health layer is ≤0. With `Kill` it despawns the entity
  (Death, reason `Kill`).
- `Getpercenthealth(Master)`: sum of current over sum of max across the layers (`Healthover` only while > 0).
- `Projectile(Master, name, Position, Projdata, Interactiontype="Hurt")`: takes a pooled object per name, loads and
  caches the `Projectileobject`, merges params, activates it (which runs the controllers' `Spawn`), and on the host sends
  the replicate RPC if `Clientreplicate` is set. It assumes the shooter has a `Radio`. `Paramsobject`, `Paramsposition`
  and `Data` are passed by reference.
- **Quirks:**
  - When `Healthover` breaks, the leftover is computed after the layer was set to 0. The full hit goes through to the
    next layer.
  - In `Doheal`, "left over" = heal − (missing after heal), so a heal that fills one layer carries its full amount to the
    next. `Armorhealmulti` scales only that bookkeeping, not the armor heal.
  - Armor makes every hit worth 1.
  - The projectile cache is static (never cleared).
- **Cite:** `Helper_Damagetools.cs:11-75`, `41-46`, `77-146`, `89`, `127-131`, `148-171`, `173-202`, `204-388`.

### `Helper_Entityai` (228 lines)
- `Checkenemymelee(Master, Query)`: needs a `Range` stat (`ContainsKey` check). Nearest query match within `Range`+0.1
  of the visual position becomes the `Attack` goal, and the unit enters `Attack`. Assumes an `Attack` command exists.
- `Checkenemyranged(Master, Query, Position, Visposition, Far=false)`: the same within `Range`, with line-of-sight from
  `Visposition`. `Far` picks the farthest match instead of the nearest.
- `Wait(Duration, token)`: a scaled-time `UniTask.Delay` in FixedUpdate timing.
- `Checkdiradjacent(dir, Query, Placeabletest, Testfor)`: rotates a local offset by `Testfor`'s facing and returns the
  nearest match within 0.5 of that spot. If `Placeabletest` is given, it must be placeable there for `Testfor`'s
  orientation and team. `Testfor` is required despite its default of null.
- `Adjacenthorizontal(Query, Placeabletest, Testfor)`: tiles of `Checkdiradjacent` over
  `Game_Grid_Rules.Adjacentshorizontal`.
- `Adjacentany(Query, Spots, Placeabletest, Testfor)`: the same over the given offsets.
- `Getfirstonblock(Block, name)`: first being on the tile whose **Entityobject `Name`** equals `name`; returns its
  `Worldobject`.
- `Getfirstonblockbeing(Block, name)`: first being whose **Beingobject `Objectname`** equals `name`; returns its
  `Link_Being`.
- `Applyartifacts(Master)`: force-applies every active Cardinal's (`Game_Mapvars.Artifactobjects`) `Coreupgrades` to
  `Master`, with the Cardinal as `Artifactobj`.
- **Cite:** `Helper_Entityai.cs:91-102`, `104-118`, `121-133`, `135-151`, `153-166`, `168-192`, `194-216`, `218-227`.

### `Helper_Entityquerytools` (338 lines)
- **Teams and styles.**
  - Teams: "enemy" (`Teamfor`=false) means team ≠ query team **and ≠ -1**. "Ally" (`Teamfor`=true) means the same team.
    `Being` sets the query team.
  - Styles: `Tag`=0, `Team`=1, `Both`=2, `Either`=3.
- **Methods:**
  - `Visibility(Item, Position)`: no `Block`-layer collider between the two points.
  - `Rayblock(Position, Ray)`: first `Block` hit point along the ray, or the ray's end.
  - `Projcheckteam(Check, Proj)`: true if same team as the projectile's shooter. Team -1 is not special here.
  - `Entityray(Position, Direction, Query)`: nearest query-matching entity collider along the ray (`Block` and `Entity`
    layers).
  - `Entityrayall(Position, Direction, Query)`: all matching colliders along the ray.
  - `Entityarea(Position, Radius, Query, Sort)` and `Entitybox(Position, Size, Rotation, Query, Sort)`: physics overlap;
    keep colliders whose entity passes the team and tag check (and targetable / LOS options); optionally sort by
    distance.
  - `Entitynearest(Position, Maxdist, Query[, Ignore])` and `Entityfarthest(...)`: scan the server tag or team index (not
    physics), apply targetable, team and LOS. Farthest uses `>=` from 0 and `<` Maxdist.
  - `Entityall()`: every entity in the team index.
  - `Entityall(Position, Query, Sort)`: the index list with **no radius**. Only `Both` filters team and targetable; `Tag`,
    `Team` and `Either` return the raw lists.
- **Quirks:**
  - Index-based queries never see runtime tags added with `Helper_Entitytools.Tag`.
  - Index lists are not de-duplicated, so an entity with two matching tags appears twice (and is hit twice by
    `Entityall` loops).
  - With `Either`, the physics queries match **every** entity, because the tag test is skipped when the team test
    passes, and the team test always passes for `Either`.
  - Tag-style `Entityall` is map-wide and any team.
  - `Query.Check` overrides the source list, but the team check still applies in nearest/farthest.
- **Cite:** `Helper_Entityquerytools.cs:7-46`, `48-67`, `69-94`, `96-113`, `115-157`, `159-207`, `209-253`, `255-289`,
  `291-337`.

---

## Most reusable for a mod

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
