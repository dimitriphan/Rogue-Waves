# A. Cardinals (artifacts): `Entity_Ai_Artifact_*` and `Entity_Ai_Effect_Artifact_*`

53 scripts, read from the game's code as decompiled in this thread (kit `docs/13` §11 recipe; never committed). Only the
code was read: which stock upgrade asset carries a given effect script lives in asset data that is not in the code, so
"carrier" is given only when the code Getloads it by name.

## How a Cardinal reaches entities (applies to every entry)
- A Cardinal is an `Artifactobject` (`Coreupgrades` list, `Stattrack`, `Basintutorialprompt`), loaded from the lobby's
  `Tds_Multiplayerinfo.Instance.Artifacts` into `Game_Mapvars.Artifactobjects` (Artifactobject.cs:10-14, Game_Init.cs:72-92).
- `Entity_Ai_Main_Core`, on its "Spawn" command, runs `Helper_Entityai.Applyartifacts(Master)` unless the core is tagged
  `Noart`. That applies every Coreupgrade to the main core with `Artifactobj` set (Entity_Ai_Main_Core.cs:166-168,
  Helper_Entityai.cs:218-226, Helper_Entitytools.cs:494). So every root `Entity_Ai_Artifact_*` script runs **on the core**.
  The vessels spawn with Parent = core (Entity_Ai_Main_Core.cs:84), so the core's `Child()` fires once for each vessel.
- `Artifactobj` is null on any AI added another way. Nine scripts dereference it (Checklist, Coin, Computer, Greatspear,
  Moneybag, Moneybagold, Mooncore, Onetimebuild, Samedayshipping). On a mod's plain upgrade they throw NullReference.
- `Helper_Entitytools.Editartifact(name, v)` writes the Cardinal's HUD counter `Game_Mapvars.Artifacttrack[i]`. It does
  nothing when no loaded Cardinal has that name (Helper_Entitytools.cs:430-447). Many scripts hard-code the name.
- Order inside `Entity_Master.Executestart` (Entity_Master.cs:286-330): the entity's own AIs run `Spawn`, then the
  `Initupgradelistserver` upgrades are applied (their `Spawn` + `Starting` run right then), then the tags are set, then
  `Loaded` fires, then the entity's own AIs run `Starting`, then `Parent.Child(this)`. Consequences:
  - An upgrade pushed into `Initupgradelistserver` from `Game_Entity.Onspawn` works.
  - One pushed from `Child()` arrives too late and is never applied.
  - Effects delivered through the init list run before the holder's `Taglistserver` is filled.
- `Game_Entity.Onspawn` fires with `Parent` and `Entitydata` (team, owner) already set and `Beingserver.Entity.Tags`
  readable, but before `Taglistserver` exists (Game_Entity.cs:206-273). A handler that throws aborts the rest of that spawn.
- `Modtypes.Multi` is added into the stat's multiplier: Multi -0.3 gives ×0.7 (Helper_Entitytools.cs:641).
  `Override` 0.01 pins the stat at 0.01, which freezes Speed or Attackspeed.
- `Game_Entity` and `Game_Mapvars` are Game-scene objects (Game_Entity.cs:43), so a global handler that is never removed
  lasts until the map unloads. If its upgrade is removed mid-map, the handler keeps firing on a destroyed component.
- Many scripts scale by `30 / Mode.Enemies.Totalwaves`, which is **integer** division: 1 for 16-30 tides, 2 for 11-15,
  3 for 8-10, and **0 for more than 30 tides** (the script then gives nothing or divides by zero). Every such script
  skips its work when `Game_Mapvars.Instance.Mode` is null or `Totalwaves` is 0.

---

### `Entity_Ai_Artifact_Bottomrock` (46 lines)
- **Does:** Starting sets the map-wide `Resourcegainmultiplier` = `Gainmultiplier` and `Resourcelossmultiplier` = 0, so every spend in `Helper_Resources.Useresources` costs nothing; it shows warning "THE ONLY WAY IS UP". Every entity spawned afterwards gets `Sellvalue` and `Upgradesellvalue` 0. Each vessel child gets the tower-limit effect.
- **Attached by:** Cardinal Coreupgrade, on the core. Uses `Child` for children tagged `Hero`.
- **Reads:** `Gainmultiplier` (required). It loops over every Defaultparams entry, but each pass applies the same upgrade.
- **Hard-coded:** Getload Upgrade `Effect_Artifact_Rock_Upgradeproperties`; warning icon `Artifact_Rock`; tag `Hero`. Clears the static `Towers` dictionary that `Bottomrock_Effect` uses.
- **Hooks:** Spawn, Starting, Child; `Game_Entity.Onspawn` (never removed). Writes `Game_Mapvars.Resourcegainmultiplier` and `Resourcelossmultiplier` (never restored).
- **Reuse:** `data + stock asset`. The free-spend and zero-sell changes hit every player and every team.
- **Quirks:** It never unsubscribes. `Glasscore`'s resource tax also uses `Useresources`, so under this Cardinal it costs 0 (Helper_Resources.cs:132).
- **Cite:** Entity_Ai_Artifact_Bottomrock.cs:13-16, 19-25, 27-39, 41-45.

### `Entity_Ai_Artifact_Bottomrock_Effect` (131 lines)
- **Does:** Counts every same-team `Mob`/`Tower`/`Trap` entity whose `Costs.Count > 0` into `Bottomrock.Towers[owner]`. When the count reaches `Maxtowers`, it sets `Controllable = false` on the holder's `Buildoption1`-`4` commands, and turns them back on when the count drops. A `Towerlimit` counter stat shows `Maxtowers - count`.
- **Attached by:** an upgrade on a vessel (Bottomrock applies `Effect_Artifact_Rock_Upgradeproperties`). Expects `Buildoption1`-`4` commands.
- **Reads:** `Maxtowers` (required). Creates stat `Towerlimit` (Counter).
- **Hard-coded:** tags `Mob`, `Tower`, `Trap`; command keys `Buildoption1`-`4`; the static `Entity_Ai_Artifact_Bottomrock.Towers`.
- **Hooks:** Starting, Reallyslowtick; adds `Childadded` straight onto the holder's `Entity_Master.Child` (never removed).
- **Reuse:** `data-only` on any vessel with `Buildoption1`-`4`. Use it with Bottomrock, or a stale static list survives from the previous map.
- **Quirks:** The count is keyed by the vessel's Owner, but the query is team-wide (it never filters by owner), so co-op allies' towers count against each vessel's limit. The `Original1`-`4` values are saved but never restored: the commands always come back as `Controllable = true`.
- **Cite:** Entity_Ai_Artifact_Bottomrock_Effect.cs:24-38, 48-63, 64-94, 95-119.

### `Entity_Ai_Artifact_Build` (44 lines)
- **Does:** Gives the holder a `Buildoption4` command (from `Command_Buildoption4`) to build the being named by the first Defaultparams key. The key's value becomes the build's `Goalvalue`, which the build AI uses as the cost multiplier and the sell value (Entity_Ai_Hero_Buildsell.cs:137,155). Adds stats `Buildrange` 1000 and `Sellrate` 0, and sets `Subowner` to 0-9 so any player slot can command the holder.
- **Attached by:** Cardinal Coreupgrade (core). Needs a build AI on the holder to execute the command (unclear which one the core has).
- **Reads:** the first Defaultparams entry (key = being name, value = cost scale). Creates `Buildrange` and `Sellrate`.
- **Hard-coded:** command asset `Command_Buildoption4`, slot `Buildoption4`.
- **Hooks:** Spawn.
- **Reuse:** `data + stock asset`: a free or discounted shared build button.
- **Quirks:** It sets `Display_Exposed = false` twice on `Buildrange` and never on `Sellrate` (copy-paste slip).
- **Cite:** Entity_Ai_Artifact_Build.cs:13-25, 26-39, 40-42.

### `Entity_Ai_Artifact_Checklist` (27 lines)
- **Does:** Whenever an entity tagged `Enemy` is killed, it spawns a `Special_Aurapickup` (forced spawn) on that tile, parented to the holder (same team), and bumps the Cardinal's counter.
- **Attached by:** Cardinal Coreupgrade (core).
- **Reads:** no params. Reads `Artifactobj.Objectname`.
- **Hard-coded:** Getload Mob `Special_Aurapickup`; tag `Enemy`.
- **Hooks:** Spawn; `Game_Entity.Ondespawn` (never removed).
- **Reuse:** `data + stock asset`, but only as a Cardinal. With `Artifactobj` null, the handler throws on every enemy kill.
- **Quirks:** It reuses one `Link_Spawn`, so its `Subowners` list grows with every drop. Charmed ex-enemies (Bluepaintcharm removes the `Enemy` tag) drop nothing.
- **Cite:** Entity_Ai_Artifact_Checklist.cs:9-17, 20-25.

### `Entity_Ai_Artifact_Checklist_Special_Pickup` (62 lines)
- **Does:** A pickup. Every 0.2 s, the nearest same-team `Mob` within 1 unit collects it: that mob's owner gets 0.5 each of `Redaura`/`Blueaura`/`Yellowaura`, a vfx plays, and the pickup despawns. It expires after 8 s.
- **Attached by:** entity AI of `Special_Aurapickup` (it never touches `Upgradeobj`, so it works as an entity AI).
- **Reads:** nothing. Duration 8 and gain 0.5 are hard-coded fields.
- **Hard-coded:** vfx `World_Aurapickup`; tag `Mob`; resource names `Redaura`, `Blueaura`, `Yellowaura`.
- **Hooks:** Spawn, Slowtick.
- **Reuse:** `data-only` (an entity of a mod's own naming its script works). The amounts can't be tuned.
- **Cite:** Entity_Ai_Artifact_Checklist_Special_Pickup.cs:11-13, 15-24, 26-34, 44-60.

### `Entity_Ai_Artifact_Chimera` (37 lines)
- **Does:** At Starting it upgrades every `Resource` (ore) entity on the map with `Effect_Artifact_Chimera_Upgradeproperties` and shows warning "FUSION OF AURA". Later ore spawns get the same upgrade through their init list, plus a pulse vfx.
- **Attached by:** Cardinal Coreupgrade (core).
- **Reads:** no params.
- **Hard-coded:** Getload Upgrade `Effect_Artifact_Chimera_Upgradeproperties`; tag `Resource`; vfx `Artifact_Chimera_Pulse`; icon `Artifact_Chimera`.
- **Hooks:** Spawn, Starting; `Game_Entity.Onspawn` (never removed).
- **Reuse:** `data + stock asset`.
- **Quirks:** It never unsubscribes.
- **Cite:** Entity_Ai_Artifact_Chimera.cs:10-16, 19-24, 26-36.

### `Entity_Ai_Artifact_Coin` (80 lines)
- **Does:** At Starting it gives `Red_Base`/`Blue_Base`/`Yellow_Base` aura, split among active players. On every change of `Wave` it gives `*_Add × New × (30/Totalwaves)`, split, and sets the counter to the tide number.
- **Attached by:** Cardinal Coreupgrade (core).
- **Reads:** required `Red_Base`, `Blue_Base`, `Yellow_Base`, `Red_Add`, `Blue_Add`, `Yellow_Add`; also reads `Artifactobj.Objectname`.
- **Hard-coded:** resource names; warning "COIN ACTIVATED" with the Cardinal's own name as icon.
- **Hooks:** Starting, Downgrade, Despawn; `Game_Mapvars.Wave.OnValueChanged` (removed on its own Downgrade and on Despawn).
- **Reuse:** `data-only`, but only as a Cardinal (Starting dereferences `Artifactobj`).
- **Quirks:** It pays on any Wave change, including Loop's -1 and Mooncore's +1. It gives nothing above 30 tides (integer factor).
- **Cite:** Entity_Ai_Artifact_Coin.cs:20-35, 38-63, 66-79.

### `Entity_Ai_Artifact_Computer` (124 lines)
- **Does:** Every 1 s it checks for enemy `Mob`/`Tower` within `Range` (or `Range_Base`) and within `Range_Base2`. Enemies in the outer band set the global `Time_Scale` to `Timescale_Base`; enemies inside `Range_Base2` set `Timescale_Base2`. The first time each band fills, it freezes up to `Stun_Count` (then `Stun_Count2`) enemies for `Stun_Base` s with Speed and Attackspeed Override 0.01. When both bands are empty it restores the saved time scale.
- **Attached by:** Cardinal Coreupgrade (core). Reuses the core's `Range` stat if it has one.
- **Reads:** required `Range_Base`, `Range_Base2`, `Timescale_Base`, `Timescale_Base2`, `Stun_Count`, `Stun_Count2`, `Stun_Base`; also `Artifactobj`. Creates `Range` (if missing) and `Range2`; the code never reads `Range2`.
- **Hard-coded:** vfx `Artifact_Computereffect`; debuff `Debuff_Stuncomputer`; tags `Mob`, `Tower`.
- **Hooks:** Spawn, Reallyslowtick. Writes `Game_Mapvars.Time_Scale`.
- **Reuse:** `data-only`, but only as a Cardinal (`Artifactobj` at :48, :68).
- **Quirks:** A static `Otheractive` lets only one instance run. If enemies jump straight into the inner band, no stun fires (State stays 0). Stun targets are picked as indices `i*5 % n`.
- **Cite:** Entity_Ai_Artifact_Computer.cs:8, 16-27, 28-43, 44-85, 86-91, 99-123.

### `Entity_Ai_Artifact_Coreapply` (31 lines)
- **Does:** At Spawn it applies every Defaultparams key that names an Upgrade asset to every `Core`-tagged entity. If the key `Enemy` is present, it targets `Enemycore`-tagged entities instead. Values are ignored.
- **Attached by:** any upgrade (it has no holder assumptions).
- **Reads:** keys = Upgrade asset names (optional; unknown keys are skipped); `Enemy` (optional switch).
- **Hard-coded:** tags `Core`, `Enemycore`.
- **Hooks:** Spawn.
- **Reuse:** `data-only`: it pushes any upgrades onto every core.
- **Quirks:** Only the cores that exist at Spawn get the upgrades; later cores don't.
- **Cite:** Entity_Ai_Artifact_Coreapply.cs:9-18, 19-29.

### `Entity_Ai_Artifact_Fragment_Effect` (90 lines)
- **Does:** At Spawn it spawns `Tower_Artriominibarrier` (Team 1, no parent) on every `Surface` tile of the radius-2 square ring around the holder, on layers -1/0/+1 (48 offsets rotated to the holder's facing). Only tiles where `General_Mob` could be placed count.
- **Attached by:** any upgrade or entity (it never reads `Upgradeobj`). The carrier asset isn't named in code.
- **Reads:** nothing.
- **Hard-coded:** Getload Mob `General_Mob` (placement test) and `Tower_Artriominibarrier`; tag `Surface`; `Team = 1`.
- **Hooks:** Spawn.
- **Reuse:** `data + stock asset`: a ring of barriers around any entity.
- **Quirks:** The team is fixed at 1 and not taken from the holder. The barriers have no parent, so they outlive the holder.
- **Cite:** Entity_Ai_Artifact_Fragment_Effect.cs:7-57, 74-88.

### `Entity_Ai_Artifact_Gavel` (76 lines)
- **Does:** Every same-team `Mob` spawn gets `Effect_Artifact_Gavel_Upgradeproperties`. When a same-team `Mob` is killed, the projectile `Artifact_Auragavel` flies from the corpse to the nearest enemy `Mob`/`Tower` within `Range_Base`. Its damage comes from the dead mob's level: `Damage_Base` at level 1, then `Damage2_Base` to `Damage5_Base`.
- **Attached by:** Cardinal Coreupgrade (core).
- **Reads:** required `Range_Base`, `Damage_Base`, `Damage2_Base`, `Damage3_Base`, `Damage4_Base`, `Damage5_Base`.
- **Hard-coded:** Getload Upgrade `Effect_Artifact_Gavel_Upgradeproperties`; projectile `Artifact_Auragavel`; counter name `Artifact_Gavel`; tags `Mob`, `Tower`.
- **Hooks:** Spawn, Despawn; `Game_Entity.Onspawn` and `Ondespawn` (removed on the holder's Despawn only, not on Downgrade).
- **Reuse:** `data + stock asset`: an on-ally-death avenger.
- **Quirks:** Removing the upgrade leaves both handlers subscribed.
- **Cite:** Entity_Ai_Artifact_Gavel.cs:10-49, 52-58, 60-75.

### `Entity_Ai_Artifact_Greatspear` (24 lines)
- **Does:** Every spawn of the being `Enemy_Spear` gets `Artifact_Greatspear_Effect_Upgradeproperties` through its init list, and the Cardinal's counter is bumped.
- **Attached by:** Cardinal Coreupgrade (core).
- **Reads:** `Artifactobj.Objectname`.
- **Hard-coded:** being name `Enemy_Spear`; Getload Upgrade `Artifact_Greatspear_Effect_Upgradeproperties`.
- **Hooks:** Spawn; `Game_Entity.Onspawn` (never removed).
- **Reuse:** `data + stock asset`, but only as a Cardinal, and it targets that one enemy only.
- **Quirks:** It calls `Initupgradelistserver.Add` without `ContainsKey`. A second holder, or an asset that already pre-lists the upgrade, throws inside Onspawn and aborts that spawn. It never unsubscribes.
- **Cite:** Entity_Ai_Artifact_Greatspear.cs:9-16, 18-23.

### `Entity_Ai_Artifact_Greatspear_Effect` (32 lines)
- **Does:** A permanent Speed Multi mod of `Speed_Multi`, plus the debuff look `Debuff_Greatspear` for 99999 s, applied 1 s after start.
- **Attached by:** upgrade (`Artifact_Greatspear_Effect_Upgradeproperties`). It does nothing if `Upgradeobj` is null.
- **Reads:** `Speed_Multi` (required).
- **Hard-coded:** debuff `Debuff_Greatspear`.
- **Hooks:** Starting, Downgrade (removes the mod).
- **Reuse:** `data-only`: a lasting slow or haste with an icon.
- **Quirks:** The debuff look is not cleared on Downgrade.
- **Cite:** Entity_Ai_Artifact_Greatspear_Effect.cs:8-23, 25-31.

### `Entity_Ai_Artifact_Heroapply` (18 lines)
- **Does:** When a child tagged `Hero` starts, it applies every Defaultparams key as an Upgrade asset to that vessel. Values are ignored.
- **Attached by:** an upgrade on the core (or on any parent of vessels).
- **Reads:** keys = Upgrade asset names (all required: there is no null check).
- **Hard-coded:** tag `Hero`.
- **Hooks:** Child.
- **Reuse:** `data-only`: the cleanest way to give every vessel an upgrade.
- **Quirks:** A key that doesn't resolve passes null into `Upgrade` and throws. Clones from Blurryphoto also receive the upgrades, because Blurryphoto calls `Core.Child(clone)`.
- **Cite:** Entity_Ai_Artifact_Heroapply.cs:6-17.

### `Entity_Ai_Artifact_Heroapply_Delay` (29 lines)
- **Does:** Like Heroapply, but each key's upgrade lands after `value` seconds of real time (`UniTask.Delay(..., ignoreTimeScale: true)`), and only if the vessel is still alive.
- **Attached by:** an upgrade on the core.
- **Reads:** key = Upgrade asset name, value = delay in seconds (both required; there is no null check).
- **Hard-coded:** tag `Hero`.
- **Hooks:** Child.
- **Reuse:** `data-only`.
- **Quirks:** The delay ignores pause and game speed.
- **Cite:** Entity_Ai_Artifact_Heroapply_Delay.cs:8-28.

### `Entity_Ai_Artifact_Mobapply` (39 lines)
- **Does:** Every spawn whose entity tags include `Mob` gets every Defaultparams key that resolves to an Upgrade, through its init list.
- **Attached by:** upgrade on the core.
- **Reads:** keys = Upgrade asset names (optional; unknown keys are skipped).
- **Hard-coded:** tag `Mob`.
- **Hooks:** Spawn; `Game_Entity.Onspawn` (never removed).
- **Reuse:** `data-only`.
- **Quirks:** The team check compares the holder with itself (`Master...Team != Master...Team`), so it hits every team, enemies included if they carry `Mob`. It never unsubscribes.
- **Cite:** Entity_Ai_Artifact_Mobapply.cs:11-24, 26-38.

### `Entity_Ai_Artifact_Moneybag` (33 lines)
- **Does:** Every `Tower` or `Mob` whose parent is tagged `Hero` (that is, built directly by a vessel) gets `Effect_Artifact_Moneybag_Upgradeproperties`. Each despawn with Reason `Sell` bumps the counter.
- **Attached by:** Cardinal Coreupgrade (core).
- **Reads:** `Artifactobj.Objectname` (in the sell handler).
- **Hard-coded:** Getload Upgrade `Effect_Artifact_Moneybag_Upgradeproperties`; tags `Hero`, `Tower`, `Mob`; reason `Sell`.
- **Hooks:** Spawn; `Game_Entity.Onspawn` and `Ondespawn` (never removed).
- **Reuse:** `data + stock asset`, but only as a Cardinal.
- **Quirks:** Its team check compares the holder with itself; the `Hero`-parent check is the real filter. It never unsubscribes.
- **Cite:** Entity_Ai_Artifact_Moneybag.cs:9-15, 17-23, 25-32.

### `Entity_Ai_Artifact_Moneybagold` (31 lines)
- **Does:** Meant to give each vessel `Effect_Artifact_Moneybag_Upgradeproperties` from `Child`, and bumps the counter on sells.
- **Attached by:** Cardinal Coreupgrade (core).
- **Reads:** `Artifactobj.Objectname`.
- **Hard-coded:** the same Upgrade asset as Moneybag; tag `Hero`; reason `Sell`.
- **Hooks:** Spawn, Child; `Game_Entity.Ondespawn` (never removed).
- **Reuse:** `no`. `Child` runs after the vessel's `Initupgradelistserver` was consumed, so the upgrade is never applied; only the sell counter works.
- **Quirks:** `Add` without `ContainsKey`. It never unsubscribes.
- **Cite:** Entity_Ai_Artifact_Moneybagold.cs:9-15, 17-30; Entity_Master.cs:300, 329.

### `Entity_Ai_Artifact_Mooncore` (52 lines)
- **Does:** Whenever `Entry` changes and `Wave+1` is 3 or a multiple of 7, it adds 2 to `Entry` and 1 to `Wave` (at most once per second), shows warning "THE MOON ALIGNS WITH YOUR CHOICE" and plays a vfx.
- **Attached by:** Cardinal Coreupgrade (core).
- **Reads:** no params; `Artifactobj.Objectname`.
- **Hard-coded:** tides 3 and every 7th; vfx `Artifact_Mooncoreeffect`; icon `Artifact_Moon`.
- **Hooks:** Starting, Downgrade, Despawn; `Game_Mapvars.Entry.OnValueChanged` (removed on Downgrade and Despawn). Writes `Entry` and `Wave`.
- **Reuse:** `data-only`, but only as a Cardinal (:28). The tides are not tunable.
- **Quirks:** It edits `Entry` from inside its own `Entry` handler; only the 1 s debounce stops it re-entering.
- **Cite:** Entity_Ai_Artifact_Mooncore.cs:12-16, 18-31, 38-51.

### `Entity_Ai_Artifact_Onetimebuild` (89 lines)
- **Does:** Creates `Buildoption<Buildslot>` for the being named by the non-`Buildslot` key (value = cost scale), plus `Buildrange` 1000, `Sellrate` 0 and `Subowner` 0-9, and removes the holder's `Sell` command. When the built child appears, the option is removed. When that child despawns, the option comes back and the `SelA`/`SelB` commands are removed. So only one can exist at a time.
- **Attached by:** Cardinal Coreupgrade (core). Built things must be children of the holder.
- **Reads:** `Buildslot` (optional, default 1); one other key = being name, value = cost scale (the last such key wins).
- **Hard-coded:** command asset `Command_Buildoption<N>`; commands `Sell`, `SelA`, `SelB`.
- **Hooks:** Spawn, Starting, Child; adds Despawn delegates on the built child.
- **Reuse:** `data + stock asset`, but only as a Cardinal (`Artifactobj` at :63, which throws when the child appears).
- **Quirks:** `Respawn` removes `Scrubconnection` from the holder's Despawn instead of the child's, which does nothing. Same `Sellrate` exposure slip as Build.
- **Cite:** Entity_Ai_Artifact_Onetimebuild.cs:15-50, 52-55, 57-88.

### `Entity_Ai_Artifact_Onetimebuild2` (3 lines)
- **Does:** An empty subclass of `Onetimebuild`, so a second one-time build can be named under a distinct script name (same behaviour).
- **Attached by:** as Onetimebuild. **Reads:** as Onetimebuild. **Hard-coded:** as Onetimebuild. **Hooks:** inherited.
- **Reuse:** `data + stock asset`, but only as a Cardinal.
- **Cite:** Entity_Ai_Artifact_Onetimebuild2.cs:1-3.

### `Entity_Ai_Artifact_Onetimebuild3` (3 lines)
- **Does:** A third empty subclass of `Onetimebuild`.
- **Attached by:** as Onetimebuild. **Reads:** as Onetimebuild. **Hard-coded:** as Onetimebuild. **Hooks:** inherited.
- **Reuse:** `data + stock asset`, but only as a Cardinal.
- **Cite:** Entity_Ai_Artifact_Onetimebuild3.cs:1-3.

### `Entity_Ai_Artifact_Paint` (22 lines)
- **Does:** Every spawn tagged `Enemy` gets `Effect_Artifact_Bluepaint_Upgradeproperties` through its init list.
- **Attached by:** Cardinal Coreupgrade (core).
- **Reads:** no params.
- **Hard-coded:** Getload Upgrade `Effect_Artifact_Bluepaint_Upgradeproperties`; tag `Enemy`.
- **Hooks:** Spawn; `Game_Entity.Onspawn` (never removed).
- **Reuse:** `data + stock asset`.
- **Quirks:** It never unsubscribes.
- **Cite:** Entity_Ai_Artifact_Paint.cs:8-14, 16-21.

### `Entity_Ai_Artifact_Paint_Effect` (41 lines)
- **Does:** Once the holder's health fraction is at or below `Health_Percent` (a 0-1 value), it starts a timer of `Duration_Base + maxHealth × Durationperhealth_Base` s with debuff `Debuff_Bluepaintstart`. If the holder is still alive at the end, it applies `Effect_Artifact_Bluepaint_Charmed_Upgradeproperties` (the charm) and removes itself.
- **Attached by:** upgrade on enemies (`Effect_Artifact_Bluepaint_Upgradeproperties`, likely by name).
- **Reads:** required `Health_Percent`, `Duration_Base`, `Durationperhealth_Base`. Reads stat `Health` (`Value_Calc` = max).
- **Hard-coded:** Getload Upgrade `Effect_Artifact_Bluepaint_Charmed_Upgradeproperties`; debuff `Debuff_Bluepaintstart`; vfx `World_Bluepainted`; counter `Artifact_Bluepaint`.
- **Hooks:** Spawn, Reallyslowtick (unsubscribes itself after triggering).
- **Reuse:** `data + stock asset`: a "convert at low HP" curse.
- **Quirks:** Without a `Health` stat it unsubscribes and never fires again.
- **Cite:** Entity_Ai_Artifact_Paint_Effect.cs:10-25, 27-35, 37-40; Helper_Damagetools.cs:173-200.

### `Entity_Ai_Artifact_Samedayshipping` (84 lines)
- **Does:** Between tides, from tide 1 on, whenever `Time_Wave` is above `Time_Base` and below 60, it clamps `Time_Wave` to `Time_Base`, so the break is shorter. It then gives `Red_Add`/`Blue_Add`/`Yellow_Add`, split (at most once per 5 s), with warning "SHIPPED" and a vfx.
- **Attached by:** Cardinal Coreupgrade (core).
- **Reads:** required `Red_Add`, `Blue_Add`, `Yellow_Add`, `Time_Base`; also `Artifactobj`. Reads `Display.Display_State == "Between"`.
- **Hard-coded:** state `Between`; the 60 s cap; vfx `World_Samedayshippingtime`; icon `Artifact_Samedayshipping`.
- **Hooks:** Starting, Downgrade, Despawn; `Game_Mapvars.Display` and `Time_Wave` `OnValueChanged` (both removed on Downgrade and Despawn). Writes `Time_Wave`.
- **Reuse:** `data-only`, but only as a Cardinal (:52).
- **Quirks:** `Editartifact` runs on every clamp, not just on payouts.
- **Cite:** Entity_Ai_Artifact_Samedayshipping.cs:13-37, 39-54, 66-83.

### `Entity_Ai_Artifact_Seeds` (38 lines)
- **Does:** Same pattern as Chimera, with `Effect_Artifact_Seeds_Upgradeproperties`: all `Resource` entities at Starting plus later ore spawns, a pulse vfx, and warning "RESOURCES FEEL PLENTIFUL".
- **Attached by:** Cardinal Coreupgrade (core).
- **Reads:** no params.
- **Hard-coded:** Getload Upgrade `Effect_Artifact_Seeds_Upgradeproperties`; tag `Resource`; vfx `Artifact_Seeds_Pulse`; icon `Artifact_Seeds`.
- **Hooks:** Spawn, Starting; `Game_Entity.Onspawn` (never removed).
- **Reuse:** `data + stock asset`.
- **Quirks:** It never unsubscribes. No effect script named Seeds is in this group.
- **Cite:** Entity_Ai_Artifact_Seeds.cs:10-17, 19-24, 26-37.

### `Entity_Ai_Artifact_Teamapply` (31 lines)
- **Does:** Everything a vessel builds directly (a `Tower` or `Mob` whose parent is tagged `Hero`) gets the **first** Defaultparams key that resolves to an Upgrade.
- **Attached by:** upgrade on the core.
- **Reads:** keys = Upgrade asset names (only the first one that resolves is used).
- **Hard-coded:** tags `Hero`, `Tower`, `Mob`.
- **Hooks:** Spawn; `Game_Entity.Onspawn` (never removed).
- **Reuse:** `data-only`.
- **Quirks:** Its team check compares the holder with itself. If no key resolves, `Selleffect` is null and the handler throws on every spawn. Units spawned by towers don't count, because their parent is the tower.
- **Cite:** Entity_Ai_Artifact_Teamapply.cs:10-16, 18-30.

### `Entity_Ai_Artifact_Textbook` (40 lines)
- **Does:** Every `Enemy_Dagger_Entity` spawn is turned into the being `Enemy_Trainingdagger_Artifact`. It swaps the Being, Entity and Block before `Start` builds stats and AIs, so the replacement's data is used.
- **Attached by:** Cardinal Coreupgrade (core).
- **Reads:** no params.
- **Hard-coded:** `Enemy_Dagger_Entity`; Getload Mob `Enemy_Trainingdagger_Artifact`; counter `Artifact_Textbook`.
- **Hooks:** Spawn; `Game_Entity.Onspawn` (never removed).
- **Reuse:** `data + stock asset`. The pattern (swap one enemy for another at spawn) needs code to retarget.
- **Quirks:** It overwrites `Beingref.Value` with a ref whose `Worldobject` is empty (impact unclear). It also Getloads in a field initializer.
- **Cite:** Entity_Ai_Artifact_Textbook.cs:8, 12-23, 25-39; Entity_Master.cs:341-411.

### `Entity_Ai_Artifact_Towerapply` (39 lines)
- **Does:** Every spawn whose entity tags include `Tower` gets every Defaultparams key that resolves to an Upgrade.
- **Attached by:** upgrade on the core.
- **Reads:** keys = Upgrade asset names (optional).
- **Hard-coded:** tag `Tower`.
- **Hooks:** Spawn; `Game_Entity.Onspawn` (never removed).
- **Reuse:** `data-only`.
- **Quirks:** Same self-compare team check as Mobapply, so it hits every team.
- **Cite:** Entity_Ai_Artifact_Towerapply.cs:11-24, 26-38.

### `Entity_Ai_Artifact_Vintage` (94 lines)
- **Does:** Each time the display state enters `Between` (with a 5 s cooldown), after 0.5 s it adds `Timeinit_Base` the first time, then `Time_Base` each later time, to `Time_Wave`, so the break is longer. Warning "EXTRA TIME", a screen-tint vfx, and the counter.
- **Attached by:** Cardinal Coreupgrade (core) or any upgrade (it never touches `Artifactobj`).
- **Reads:** required `Time_Base`, `Timeinit_Base`.
- **Hard-coded:** state `Between`; vfx `Artifact_Vintagescreentint`; icon and counter `Artifact_Vintage`.
- **Hooks:** Starting, Downgrade, Despawn; `Game_Mapvars.Display` and `Time_Wave` `OnValueChanged` (removed on Downgrade and Despawn). Writes `Time_Wave`.
- **Reuse:** `data-only`.
- **Cite:** Entity_Ai_Artifact_Vintage.cs:15-21, 23-40, 42-64, 76-93.

### `Entity_Ai_Artifact_Voiceapply` (31 lines)
- **Does:** Like Teamapply, and also covers `Trap`: everything a vessel builds directly gets the first Defaultparams key that resolves to an Upgrade.
- **Attached by:** upgrade on the core.
- **Reads:** keys = Upgrade asset names (first match used).
- **Hard-coded:** tags `Hero`, `Tower`, `Trap`, `Mob`.
- **Hooks:** Spawn; `Game_Entity.Onspawn` (never removed).
- **Reuse:** `data-only`.
- **Quirks:** Same as Teamapply (self-compare team check; throws if no key resolves).
- **Cite:** Entity_Ai_Artifact_Voiceapply.cs:10-16, 18-30.

### `Entity_Ai_Artifact_Wavebreaker` (74 lines)
- **Does:** Every 1 s, each enemy `Mob`/`Tower` within the holder's `Range` gets a Health Multi mod of `Debuff_Base`, which lowers max HP while it stays in range, plus debuff `Debuff_Wavebroken` (3 s). It also gets a 1 s marker mod named `Wavebreaker`. The Health mod is removed when the enemy leaves range.
- **Attached by:** Cardinal Coreupgrade (core) or any upgrade. It uses the holder's own `Range` if one exists.
- **Reads:** required `Debuff_Base`, `Range_Base` (used only when there is no `Range`). Creates `Range` if it is missing.
- **Hard-coded:** debuff `Debuff_Wavebroken`; marker key `Wavebreaker`; tags `Mob`, `Tower`.
- **Hooks:** Spawn, Reallyslowtick.
- **Reuse:** `data-only`: a "weaken in range" aura for any tower or core.
- **Quirks:** Whether current HP follows the lower max is unclear. Removing the upgrade leaves the Health mods on whoever is in range at that moment.
- **Cite:** Entity_Ai_Artifact_Wavebreaker.cs:14-50, 57-73.

### `Entity_Ai_Effect_Artifact_Beastpelt` (8 lines)
- **Does:** Every 1 s it sets `Option_Targetable = false` on the holder and shows `Debuff_Elusive` for 3 s.
- **Attached by:** upgrade (the carrier isn't named in code).
- **Reads:** nothing.
- **Hard-coded:** debuff `Debuff_Elusive`.
- **Hooks:** Reallyslowtick.
- **Reuse:** `data-only`: an untargetable trait.
- **Quirks:** There is no Downgrade, so the holder stays untargetable after the upgrade is removed.
- **Cite:** Entity_Ai_Effect_Artifact_Beastpelt.cs:3-7.

### `Entity_Ai_Effect_Artifact_Bluepaintcharm` (42 lines)
- **Does:** Charms the holder. It takes the team of `Master.Lasthit` (or of the nearest `Core` if there is none), sets `Subowner` 0-9, and makes the holder untargetable for 1 s. If the holder has a `Goal` command, it is re-parented to that entity and its goal is cleared. It then switches to `Idle` and drops the `Enemy` tag (also removing itself from `Entitiesserver["Enemy"]`).
- **Attached by:** upgrade (likely `Effect_Artifact_Bluepaint_Charmed_Upgradeproperties`, applied by Paint_Effect).
- **Reads:** nothing.
- **Hard-coded:** tags `Core`, `Enemy`; commands `Goal`, `Idle`.
- **Hooks:** Spawn.
- **Reuse:** `data-only`: a "turn an enemy to your side" effect.
- **Quirks:** `Entitiesserverteam` is not updated, so team-style queries (`Styles.Team` or `Either`) still list it under its old team. It changes nothing if no `Lasthit` and no Core exist.
- **Cite:** Entity_Ai_Effect_Artifact_Bluepaintcharm.cs:7-41; Helper_Entityquerytools.cs:18-33.

### `Entity_Ai_Effect_Artifact_Blurryphoto` (112 lines)
- **Does:** On a vessel without the `Clone` tag, if a Hero-group asset with the vessel's being name exists, it spawns a clone of that vessel parented to the original (`Destroyonparent`). The clone gets skin `<being>2` and tags `Prespawn` and `Clone`. When the clone loads, the script calls `Core.Child(clone)` so other Cardinals apply to it, adds Pmulti mods `Speed_Pmulti`, `Damage_Pmulti`, `Attackspeed_Pmulti` and `Health_Pmulti`, and gives it the select command `SelA`. A dead clone respawns after `Respawn_Base` s while the original lives.
- **Attached by:** upgrade on a vessel (via Heroapply or similar).
- **Reads:** required `Respawn_Base`, `Speed_Pmulti`, `Damage_Pmulti`, `Attackspeed_Pmulti`, `Health_Pmulti`.
- **Hard-coded:** Getload Hero `<being name>`; command asset `Command_Artifact_Blurryphoto_Select` (`SelA`); skins `<being>` and `<being>2`; tags `Clone`, `Prespawn`; counter `Artifact_Blurryphoto`.
- **Hooks:** Starting, Downgrade; Despawn and Loaded delegates on each clone; it invokes every Core's `Child`.
- **Reuse:** `vessel-bound`. It needs the vessel loadable as a Hero asset under its being name, plus a `<being>2` skin; otherwise only the skin is set.
- **Quirks:** The clone's mods are stored on the original's AI, so Downgrade only strips the latest clone's mods. The reused `Link_Spawn` grows `Subowners`. The respawned clone's skin is set after spawn.
- **Cite:** Entity_Ai_Effect_Artifact_Blurryphoto.cs:24-45, 51-68, 69-87, 90-111.

### `Entity_Ai_Effect_Artifact_Buff_Rustypipe` (30 lines)
- **Does:** Multi mods on `Health` (`Health_Multi`) and `Damage` (`Damage_Multi`).
- **Attached by:** upgrade (`Effect_Artifact_Rustypipe_Buff_Upgradeproperties`, applied by Rustypipe).
- **Reads:** required `Health_Multi`, `Damage_Multi`.
- **Hard-coded:** none.
- **Hooks:** Starting, Downgrade.
- **Reuse:** `data-only`.
- **Quirks:** Downgrade removes `Mod1` twice and never `Mod2`, so the Damage bonus is permanent.
- **Cite:** Entity_Ai_Effect_Artifact_Buff_Rustypipe.cs:7-14, 16-29.

### `Entity_Ai_Effect_Artifact_Caffeine` (71 lines)
- **Does:** Base mods on `Speed`, `Damage`, `Attackspeed` and `Sellrate` equal to `*_Base × Wave`. Each Wave change re-sets them to `*_Base × Wave × (30/Totalwaves)` and adds a burst of `*_Pmulti` mods for `Boost_Duration` s, shown with debuff `Debuff_Energyboost`.
- **Attached by:** upgrade (likely on vessels, since it reads `Sellrate`).
- **Reads:** required `Speed_Base`, `Damage_Base`, `Attackspeed_Base`, `Sellrate_Base`, `Damage_Pmulti`, `Attackspeed_Pmulti`, `Speed_Pmulti`, `Boost_Duration`.
- **Hard-coded:** debuff `Debuff_Energyboost`; counter `Artifact_Caffeine`.
- **Hooks:** Starting, Downgrade, Despawn; `Game_Mapvars.Wave.OnValueChanged` (removed on Downgrade and Despawn).
- **Reuse:** `data-only`: a buff that grows with the tide.
- **Quirks:** Starting omits the `30/Totalwaves` factor that the update applies. The burst mods aren't removed on Downgrade (they expire on their own).
- **Cite:** Entity_Ai_Effect_Artifact_Caffeine.cs:15-29, 31-39, 41-70.

### `Entity_Ai_Effect_Artifact_Chimera` (94 lines)
- **Does:** On an ore entity, it merges the mine stats `Redauramine`, `Blueauramine` and `Yellowauramine`. If one is largest and another is above 0, the largest gets `Ore_Add` plus the others' amounts, plus a `Ore_Multi` Pmulti, and the others are pinned to 0.01. If only one exists and it exceeds `2 × Ore_Split`, it splits `Ore_Split` into a new stat of the next colour (blue→red→yellow→blue). If nothing applies, it removes itself.
- **Attached by:** upgrade on `Resource` entities (`Effect_Artifact_Chimera_Upgradeproperties`).
- **Reads:** required `Ore_Split`; `Ore_Add` and `Ore_Multi` (needed when a merge happens).
- **Hard-coded:** stat names above; vfx `Artifact_Chimera_Pulse`; counter `Artifact_Chimera`.
- **Hooks:** Starting.
- **Reuse:** `data-only`: an ore-mixing modifier.
- **Quirks:** The split adds a stat that may already exist at 0 (whether `Addstat` overwrites it is unclear).
- **Cite:** Entity_Ai_Effect_Artifact_Chimera.cs:13-24, 25-48, 49-84, 85-91.

### `Entity_Ai_Effect_Artifact_Diorama` (56 lines)
- **Does:** Permanent Pmulti mods on `Speed`, `Damage`, `Attackspeed` and `Health`, an explosion vfx, and a skin set to the being name (`<being>2` on clones).
- **Attached by:** upgrade (likely on vessels).
- **Reads:** required `Speed_Pmulti`, `Damage_Pmulti`, `Attackspeed_Pmulti`, `Health_Pmulti`.
- **Hard-coded:** vfx `Artifact_Diorama_Explosion`; counter `Artifact_Diorama`; tag `Clone`; skin = being name.
- **Hooks:** Starting, Downgrade (removes all 4 mods).
- **Reuse:** `data-only` for the stats. The skin write assumes a skin named after the being exists (unclear for mod vessels).
- **Quirks:** The `Clone` check reads `Taglistserver`, which is still empty if the effect arrives through an init list.
- **Cite:** Entity_Ai_Effect_Artifact_Diorama.cs:14-32, 34-55.

### `Entity_Ai_Effect_Artifact_Drinkstand` (28 lines)
- **Does:** A Base mod of `Damage_Base` on Damage that lasts while the holder carries a `Drinkbuff` modifier; it removes its own upgrade once the marker is gone.
- **Attached by:** `Entity_Ai_Tower_Drinkstand`, which adds the `Drinkbuff` marker for the tower's `Duration` and applies `Effect_Artifact_Cubeddrink_Upgradeproperties`.
- **Reads:** required `Damage_Base`.
- **Hard-coded:** marker key `Drinkbuff`.
- **Hooks:** Starting, Slowtick, Downgrade.
- **Reuse:** `data-only`, if the source also adds a `Drinkbuff` modifier; without one it removes itself at the first Slowtick.
- **Cite:** Entity_Ai_Effect_Artifact_Drinkstand.cs:7-27; Entity_Ai_Tower_Drinkstand.cs:24, 36-42.

### `Entity_Ai_Effect_Artifact_Glasscore` (103 lines)
- **Does:** Turns a sub-core into a glass core, with priority 9 so it runs before the general AI. It applies Hurt damage itself and zeroes `Damage`/`Damagemulti` so the general AI adds none. At 0 HP it sets Health 1 and despawns **without** a death. On despawn it takes `resources × Aura_Base` from every player, and 1 s later deals `Damage_Base` Hurt to every enemy `Mob`/`Tower` on the whole map. On `Main_Core_Entity` it removes itself; on any other holder it sets skin `Sub`.
- **Attached by:** upgrade on cores (for example via Coreapply).
- **Reads:** required `Damage_Base`, `Aura_Base`. Interact `Data["Damage"]`, `Data["Damagemulti"]`.
- **Hard-coded:** entity `Main_Core_Entity`; skin `Sub`; vfx `World_Subcoreglass_Death`; warning "TONE BASIN DESTROYED" with icon `Artifact_Glasscore`; tags `Mob`, `Tower`.
- **Hooks:** Starting, Oninteract, Despawn.
- **Reuse:** `data-only`: a breakable objective with a payoff blast and a tax.
- **Quirks:** The blast hurts targets "from themselves" (`Interact(target, target, ...)`). `Totaldam` never grows. The blast has no radius (`Entityall`).
- **Cite:** Entity_Ai_Effect_Artifact_Glasscore.cs:14-30, 32-87, 89-102.

### `Entity_Ai_Effect_Artifact_Loop` (48 lines)
- **Does:** The first Hurt on the holder despawns (without death) every enemy `Mob`/`Tower` on the map, sets `Entry -= 2`, `Wave -= 1` and `Time_Wave = 0` (replaying the tide), shows warning "LOOP" with a vfx, and removes this upgrade from every `Core`.
- **Attached by:** upgrade on cores.
- **Reads:** no params.
- **Hard-coded:** tags `Mob`, `Tower`, `Core`; vfx `Artifact_Reversescreentint`; icon `Artifact_Loop`.
- **Hooks:** Starting, Oninteract. Writes `Game_Mapvars.Entry`, `Wave` and `Time_Wave`.
- **Reuse:** `data-only`: a one-shot "rewind the tide" charm.
- **Quirks:** A static `Looped` is shared by every instance and reset by any later Starting.
- **Cite:** Entity_Ai_Effect_Artifact_Loop.cs:7, 11-42, 44-47.

### `Entity_Ai_Effect_Artifact_Moneybag` (53 lines)
- **Does:** Each Wave change adds `Rate_Base × (30/Totalwaves)` to a `Sellvalue` counter stat and `Rate_Base/100 × (30/Totalwaves)` to the entity's `Sellvalue` and `Upgradesellvalue`, so the build sells for more the longer it stands. Above a counter of 40 it shows `Debuff_Shiny`.
- **Attached by:** upgrade on vessel-built towers and mobs (`Effect_Artifact_Moneybag_Upgradeproperties`).
- **Reads:** required `Rate_Base`. Creates stat `Sellvalue` (Counter).
- **Hard-coded:** vfx `World_Shinyburst`; debuff `Debuff_Shiny`; threshold 40.
- **Hooks:** Starting, Downgrade, Despawn; `Game_Mapvars.Wave.OnValueChanged` (removed on Downgrade and Despawn).
- **Reuse:** `data-only`: an "appreciating asset" trait.
- **Quirks:** It grows on any Wave change, including decreases. The gains already added to `Entitydata` are not undone on Downgrade.
- **Cite:** Entity_Ai_Effect_Artifact_Moneybag.cs:9-23, 25-37, 39-52.

### `Entity_Ai_Effect_Artifact_Moneybagold` (40 lines)
- **Does:** An Add mod on `Sellrate` set to `lerp(Startrate_Base, Endrate_Base, Wave/30) / 100`, updated each tide.
- **Attached by:** upgrade (the carrier isn't named in code; Moneybagold applies the Moneybag asset, not this one).
- **Reads:** required `Startrate_Base`, `Endrate_Base`. Needs a `Sellrate` stat on the holder.
- **Hard-coded:** the 30-tide lerp span.
- **Hooks:** Spawn, Starting, Downgrade, Despawn; `Game_Mapvars.Wave.OnValueChanged` (removed on Downgrade and Despawn).
- **Reuse:** `data-only`: a sell refund that improves by tide.
- **Quirks:** The mod is created as raw `Startrate_Base` and then immediately set to the /100 value. The mod is never removed on Downgrade.
- **Cite:** Entity_Ai_Effect_Artifact_Moneybagold.cs:9-13, 15-18, 20-39.

### `Entity_Ai_Effect_Artifact_Opalcore` (76 lines)
- **Does:** Thorns. Every Hurt without a `Spike` key answers the attacker with Hurt equal to the holder's `Damage` (tagged `Spike`, so it can't loop). It adds a `Damage` stat of `Damage_Base`, and each Wave change raises max Health by `Health_Add × (30/Totalwaves)`. Warning "BASIN GROWS STRONGER"; skin `Sub` unless the entity is `Main_Core`.
- **Attached by:** upgrade on cores.
- **Reads:** required `Damage_Base`, `Health_Add`. Interact key `Spike`. Creates stat `Damage`.
- **Hard-coded:** entity `Main_Core`; skin `Sub`; vfx `Artifact_Opalcorespawn`; icon and counter `Artifact_Opalcore`.
- **Hooks:** Starting, Oninteract, Downgrade, Despawn; `Game_Mapvars.Wave.OnValueChanged` (removed on Downgrade and Despawn).
- **Reuse:** `data-only`: thorns plus growing HP for any entity.
- **Quirks:** It checks `Main_Core` where Glasscore checks `Main_Core_Entity`. Entity names end in `_Entity`, so the main core probably gets skin `Sub` too (unclear).
- **Cite:** Entity_Ai_Effect_Artifact_Opalcore.cs:11-26, 28-35, 37-56, 58-75.

### `Entity_Ai_Effect_Artifact_Paper` (73 lines)
- **Does:** At Starting it records which of the vessel's `Buildoption1`-`4` targets are tagged `Towerfighter`. The first such tower the vessel builds while no paper tower is alive gets `Effect_Artifact_Paper_Tower_Upgradeproperties`.
- **Attached by:** upgrade on a vessel.
- **Reads:** no params. Reads `Commandlistserver["BuildoptionN"].Goalstring`.
- **Hard-coded:** Getload Upgrade `Effect_Artifact_Paper_Tower_Upgradeproperties`; tag `Towerfighter`; counter `Artifact_Paper`.
- **Hooks:** Starting, Child; empty Downgrade and Despawn.
- **Reuse:** `data + stock asset`: a "one champion tower" buff.
- **Quirks:** A static `Papertower` means only one champion exists across all players. Build options gained after Starting are ignored.
- **Cite:** Entity_Ai_Effect_Artifact_Paper.cs:13, 15-23, 25-64.

### `Entity_Ai_Effect_Artifact_Paper_Tower` (44 lines)
- **Does:** Multi mods on `Health`, `Speed`, `Damage` and `Attackspeed` (`*_Multi`).
- **Attached by:** upgrade (`Effect_Artifact_Paper_Tower_Upgradeproperties`).
- **Reads:** required `Health_Multi`, `Speed_Multi`, `Damage_Multi`, `Attackspeed_Multi`.
- **Hard-coded:** none.
- **Hooks:** Starting, Downgrade.
- **Reuse:** `data-only`: a four-stat package.
- **Quirks:** Downgrade calls `Mod1.Remove()` four times, so the Speed, Damage and Attackspeed mods are never removed.
- **Cite:** Entity_Ai_Effect_Artifact_Paper_Tower.cs:11-20, 22-43.

### `Entity_Ai_Effect_Artifact_Pig` (173 lines)
- **Does:** Adds a `Coinbag` command. When used with build-style JSON (`Owner`, `Position`), it spends `Amount` of each aura and lobs the projectile `Artifact_Moneybag` at the surface tile. On the holder's outgoing hits that carry `Aura`, a same-team target's owner gets `Amount - 1` of each aura and the hit is cancelled. Enemies hit farther than 1.5 from the holder take `Damage` (ore takes 0.05); closer enemies take 0. When the Cardinal `Artifact_Rock` is active, it lobs `Artifact_Moneybagrock` instead, with Aura 0 and 0.1 damage.
- **Attached by:** upgrade on a vessel.
- **Reads:** required `Amount`, `Damage`. Interact keys `Aura`, `Damage`.
- **Hard-coded:** command asset `Command_Artifact_Pig_Launch`; projectiles `Artifact_Moneybag`, `Artifact_Moneybagrock`; Cardinal name `Artifact_Rock` (read from `Tds_Multiplayerinfo.Artifacts` and `Mixedart`); tags `Surface`, `Resource`; resource names.
- **Hooks:** Starting, Command, Interact.
- **Reuse:** `data + stock asset`: a thrown "share resources" bag. It needs the stock command and projectiles.
- **Quirks:** The `Command` override ignores the `Server` flag. The refund is `Amount - 1` per colour.
- **Cite:** Entity_Ai_Effect_Artifact_Pig.cs:9-29, 31-100, 102-172.

### `Entity_Ai_Effect_Artifact_Rustypipe` (57 lines)
- **Does:** An `Age` counter starts at `ceil(Wave_Base / (30/Totalwaves))` and drops by 1 on each Wave change. At 0 it applies `Effect_Artifact_Rustypipe_Buff_Upgradeproperties`, plays a vfx, and removes itself.
- **Attached by:** upgrade (the carrier isn't named; likely on towers).
- **Reads:** required `Wave_Base`. Creates stat `Age`.
- **Hard-coded:** Getload Upgrade `Effect_Artifact_Rustypipe_Buff_Upgradeproperties`; vfx `World_Rustburst`; counter `Artifact_Rustypipe`.
- **Hooks:** Starting, Downgrade, Despawn; `Game_Mapvars.Wave.OnValueChanged` (removed on Downgrade and Despawn).
- **Reuse:** `data + stock asset`: a "ripen after N tides" timer. The swapped-in upgrade is fixed.
- **Quirks:** Downgrade removes the `Age` stat on **any** upgrade's removal, not just its own. Above 30 tides the factor is 0, so Age is infinite and it never ripens.
- **Cite:** Entity_Ai_Effect_Artifact_Rustypipe.cs:12-22, 24-39, 41-49.

### `Entity_Ai_Effect_Artifact_Slippers` (61 lines)
- **Does:** While the holder's `State` contains "Move", a Base Speed mod grows by `Speed_Base` per second, showing `Debuff_Speedsmoke` once it exceeds `5 × Speed_Base`. Any state change resets it to 0.
- **Attached by:** upgrade on moving units or vessels.
- **Reads:** required `Speed_Base`. Reads `Master.State`.
- **Hard-coded:** substring "Move"; debuff `Debuff_Speedsmoke`.
- **Hooks:** Starting, Slowtick; `Master.State.OnValueChanged` (never removed).
- **Reuse:** `data-only`: a momentum trait.
- **Quirks:** There is no Downgrade: the Speed mod and the State handler stay after removal.
- **Cite:** Entity_Ai_Effect_Artifact_Slippers.cs:16-27, 29-34, 36-60.

### `Entity_Ai_Effect_Artifact_Snare` (53 lines)
- **Does:** On attach it freezes the holder (Speed Override 0.01) for `clamp(Duration_Base - currentHP × Durationperhealth_Base, Minduration, 999)` s with debuff `Debuff_Snare`, then removes itself. While it lasts, each Hurt on the holder gives `Aura_Base` Red and Blue aura to every active player (full amount each) and plays an emitter vfx.
- **Attached by:** upgrade on enemies (the carrier isn't named).
- **Reads:** required `Aura_Base`, `Duration_Base`, `Durationperhealth_Base`, `Minduration`. Reads stat `Health` (`Value`, current HP).
- **Hard-coded:** debuff `Debuff_Snare`; emitter `World_Snareaura`; resource names (no Yellow).
- **Hooks:** Spawn, Oninteract.
- **Reuse:** `data-only`: a root debuff that self-expires, with a pay-on-hit.
- **Quirks:** Without a `Health` stat it removes itself at once.
- **Cite:** Entity_Ai_Effect_Artifact_Snare.cs:10-17, 19-52.

### `Entity_Ai_Effect_Artifact_Titan` (70 lines)
- **Does:** A Pmulti Health mod of `Health_Pmulti`. On **death** (not other despawns), it deals Hurt to every enemy `Mob`/`Tower` within `Range_Base`, with damage picked by the holder's level: `Damage_Base`, then `Damage2_Base` to `Damage5_Base`. Vfx `Artifact_Titan`.
- **Attached by:** upgrade (the carrier isn't named).
- **Reads:** required `Health_Pmulti`, `Range_Base`, `Damage_Base`, `Damage2_Base`, `Damage3_Base`, `Damage4_Base`, `Damage5_Base`.
- **Hard-coded:** vfx `Artifact_Titan`; counter `Artifact_Titan`; tags `Mob`, `Tower`.
- **Hooks:** Starting, Downgrade, Despawn.
- **Reuse:** `data-only`: a death-burst trait.
- **Quirks:** Damage comes "from the target itself" (`Interact(target, target, ...)`), so kills are not credited to the holder.
- **Cite:** Entity_Ai_Effect_Artifact_Titan.cs:8-14, 24-69.

### `Entity_Ai_Effect_Artifact_Voice` (91 lines)
- **Does:** Each Wave change lowers the holder's upgrade-cost multiplier (`Entity_Ai_General_Upgrade.Upgradecommand.Goalvalue`, which starts at 1) by `Rate_Base/100 × (30/Totalwaves)`, never below 0, and tracks the % saved in a `Voicevalue` counter. Any upgrade with `Priority > 0` (a level up) restores the discount.
- **Attached by:** upgrade on vessel builds (likely via Voiceapply). It removes itself if the holder has no `Entity_Ai_General_Upgrade`.
- **Reads:** required `Rate_Base`. Creates stat `Voicevalue`.
- **Hard-coded:** command `Upgrade`; vfx `World_Voiceburst`.
- **Hooks:** Starting, Upgrade, Downgrade, Despawn; `Game_Mapvars.Wave.OnValueChanged` (removed on Downgrade and Despawn).
- **Reuse:** `data-only`: "upgrades get cheaper the longer you wait".
- **Quirks:** It throws in `Upgrade` if `Upgradecommand` was null at Starting. Its own upgrade's `Upgrade` event can trigger a reset if that upgrade has `Priority > 0`.
- **Cite:** Entity_Ai_Effect_Artifact_Voice.cs:11-31, 33-42, 44-67, 84-90; Entity_Ai_General_Upgrade.cs:22, 31, 71-73.

---

## Global hooks at a glance

| Script | Global event / value | Unsubscribes? |
| --- | --- | --- |
| Bottomrock | `Game_Entity.Onspawn`; writes `Game_Mapvars.Resourcegainmultiplier`, `Resourcelossmultiplier` | never; values never restored |
| Bottomrock_Effect | holder's `Entity_Master.Child` (direct) | never |
| Checklist | `Game_Entity.Ondespawn` | never |
| Chimera, Seeds, Paint, Greatspear, Textbook, Mobapply, Towerapply, Teamapply, Voiceapply | `Game_Entity.Onspawn` | never |
| Moneybag | `Game_Entity.Onspawn` + `Ondespawn` | never |
| Moneybagold | `Game_Entity.Ondespawn` | never |
| Gavel | `Game_Entity.Onspawn` + `Ondespawn` | on holder Despawn only (not Downgrade) |
| Coin, Effect Caffeine, Moneybag (effect), Moneybagold (effect), Opalcore, Rustypipe, Voice | `Game_Mapvars.Wave.OnValueChanged` | yes, on own Downgrade and on Despawn |
| Mooncore | `Game_Mapvars.Entry.OnValueChanged`; writes `Entry`, `Wave` | yes (Downgrade and Despawn) |
| Samedayshipping, Vintage | `Game_Mapvars.Display` + `Time_Wave` `OnValueChanged`; write `Time_Wave` | yes (Downgrade and Despawn) |
| Computer | writes `Game_Mapvars.Time_Scale` (Reallyslowtick) | restores only when its bands empty |
| Loop | writes `Entry`, `Wave`, `Time_Wave` | n/a (one-shot) |
| Slippers | holder's `State.OnValueChanged` | never |
| All others | entity lifecycle only | n/a |

## Most reusable for a mod
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
