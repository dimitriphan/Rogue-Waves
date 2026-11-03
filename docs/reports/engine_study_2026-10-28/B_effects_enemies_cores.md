# B: effects, enemies, cores, resources, general AIs (83 scripts)

Source: the game's code as decompiled in this thread (kit `docs/13` §11 recipe); the decompiled files are never
committed. Line numbers are the file's own (with `//IL_` lines counted). "(N lines)" = `wc -l`.

## Read first: facts that hold across this group

- **Upgrade AIs get `Spawn()` then `Starting()` back to back**, after the holder's `Upgrade(Property)` event has fired
  (new AIs are already subscribed, so their own `Upgrade` override sees it first) (`Helper_Entitytools.cs:530-539`).
  Initial upgrades (`Initupgradelistserver`) are applied **before** the entity's own tags (`Entity_Master.cs:299-311`),
  so a `Taglistserver.Contains` in `Starting` of an init upgrade does not see the entity's data tags yet.
- **Same upgrade twice does nothing**: `Upgrade` returns if `Objectname` is already listed (`Helper_Entitytools.cs:470`).
  Timed effects therefore do not refresh or stack by re-applying; use a second upgrade asset to stack.
- **Removal is by script type name**: `Removeupgrade` destroys an AI only if NO remaining upgrade (or the entity) lists
  that type (`Helper_Entitytools.cs:571-586`). Two upgrades naming the same script: removing one fires its `Downgrade`
  (mods removed) but its AI instance survives and keeps its hooks (e.g. an invulnerability `Oninteract`).
- **`Addmod` on a stat the holder lacks still files a modifier** under that name in `Master.Modifiers`
  (`Helper_Entitytools.cs:612-619`). The game uses such names as flags: `Shopbuff`, `Shopdebuff`, `Protectbuff`,
  `Trueeffect`, `Redblocked`, `Scorch`, `Death`. Upgrades carry no stat mods by data (`Upgradeproperties.cs` has only
  `Defaultparams`, `Aiscripts`, `Costs`, `Upgrades`, `Priority`), so a flag needs a script that calls `Addmod`.
- **Multi/Pmulti mods add into the stat's multiplier** (`Value_Multi += mod`), so `Speed_Multi = -0.5` is -50 %,
  `0.5` is +50 %; Override mods replace the stat with their sum (`Helper_Entitytools.cs:641-646`, `Link_Modifier.cs:107-125`).
- **`Helper_Entitytools.Interact(From, To, ...)` runs the SOURCE's `Interact` hooks too** (`Helper_Entitytools.cs:191-211`):
  self-damage (Puffer, Blaze, Scorch) and thorns pass through the holder's own outgoing hooks.
- **Damage layers** (`Helper_Damagetools.cs:11-76`): `Healtharmor` > 0 absorbs exactly `1 x Damagemulti` per hit
  regardless of Damage; `Healthover` next; `Healthresource` (ore) takes at most 1 per hit plus Data `Damageresource`;
  then `Health`. `Lasthittime` is only set by `Interacthurt` when the hit had no explicit `Damage` key
  (`Helper_Entitytools.cs:150-155`).
- **Currency**: `Redaura`, `Blueaura`, `Yellowaura`. `Giveresources(list, -1, Split)` divides each amount among
  active players; a real owner id gets the full amount (`Helper_Resources.cs:48-73`).
- **Reuse classes used below**: hard-coded vfx/debuff/skin names are looks resolved by name and do not change the
  class; `data + stock asset` means the script `Getload`s a stock Mob/Upgrade/Projectile/Command by name.

---

## Effects (43)

### `Entity_Ai_Effect_Barrel_Boost` (58 lines)
- **Does:** Timed four-stat change: adds Health Pmulti and Speed/Attackspeed/Damage Multi, shows `Debuff_Woozy`
  for `Duration_Base` s, then removes its own upgrade; Downgrade removes all four mods.
- **Attached by:** upgrade, any entity.
- **Reads:** required `Health_Pmulti`, `Speed_Multi`, `Attackspeed_Multi`, `Damage_Multi`, `Duration_Base`.
- **Hard-coded:** vfx `Debuff_Woozy`.
- **Hooks:** Starting, Downgrade; UniTask delay on `destroyCancellationToken`.
- **Reuse:** `data-only` - a generic timed buff/debuff (set unused stats to 0).
- **Cite:** `Entity_Ai_Effect_Barrel_Boost.cs:16-23`, `:25-34`, `:36-56`.

### `Entity_Ai_Effect_Boutmanbucket_Boost` (42 lines)
- **Does:** Timed invulnerability: at priority 8 (before damage at 10) sets `Damage` and `Damagemulti` of every
  incoming Hurt to 0; removes itself after `Duration_Base` s.
- **Attached by:** upgrade, any entity.
- **Reads:** required `Duration_Base`; Data `Damage` (only acts if present).
- **Hard-coded:** vfx `Debuff_Boutman_White`.
- **Hooks:** Setpriority (forced 8), Oninteract, Starting, Downgrade (no-op).
- **Reuse:** `data-only` - shield pickup / spawn protection.
- **Quirks:** `Setpriority` ignores its argument.
- **Cite:** `Entity_Ai_Effect_Boutmanbucket_Boost.cs:8-20`, `:22-36`.

### `Entity_Ai_Effect_Boutmanfamily_Boost` (151 lines)
- **Does:** Branches on holder tags. Untagged: every outgoing Hurt with `Damagemulti` gets `+Damage_Multi`.
  `Boutdad`/`Boutmom`: every outgoing Hurt without `Aoe` fires projectile `Mob_Boutmandad_Aoe`/`Mob_Boutmanmom_Aoe`
  at the target carrying that hit's Damage (`Aoe`=1). `Boutbro`: Damage and Range Multi, and on its Move action it
  teleports when the goal tile is > 3 away or has no path.
- **Attached by:** upgrade; `Boutbro` holder needs a `Move` command.
- **Reads:** `Damage_Multi` (required, read per hit, untagged branch); `Boutbro_Damage_Multi`, `Boutbro_Range_Multi`
  (required only for `Boutbro`); Data `Damage` (required for dad/mom), `Aoe`, `Damagemulti`.
- **Hard-coded:** tags `Boutbro`, `Boutdad`, `Boutmom`, `Surface`; projectiles `Mob_Boutmandad_Aoe`,
  `Mob_Boutmanmom_Aoe`; vfx `Debuff_Boutman_Red` (1e8 s), `World_Boutmanteleportstart`, `World_Boutmanteleportcat`.
- **Hooks:** Starting, Interact, Action, Downgrade.
- **Reuse:** `data-only` for the untagged branch (permanent outgoing damage amp); `data + stock asset` for the splash
  branches (a mod unit can carry the `Boutdad`/`Boutmom` tag by data).
- **Quirks:** red vfx is never removed on Downgrade; the `Boutbro` teleport dereferences `Toposition` before its null
  check (NRE when no tile is found).
- **Cite:** `Entity_Ai_Effect_Boutmanfamily_Boost.cs:14-25`, `:27-75`, `:77-112`, `:137-150`.

### `Entity_Ai_Effect_Boutmanfish_Bass` (22 lines)
- **Does:** Execute mark: when the holder is hurt while `Health.Value / Value_Calc <= Percent_Base`, adds 999999 to
  the hit's Damage (Damagemulti 1).
- **Attached by:** upgrade, any entity with `Health`.
- **Reads:** required `Percent_Base` (a fraction, default 0.15); holder `Health` (optional); Data `Damage` required.
- **Hard-coded:** vfx `Debuff_Deathmarkedfish` (999999 s), `World_Mob_Boutmanfish_Execute`.
- **Hooks:** Starting, Oninteract (priority 0, before damage).
- **Reuse:** `data-only` - execute/death-mark debuff.
- **Quirks:** while `Healtharmor` > 0 the hit still only removes 1 armor (layer rule); vfx never removed.
- **Cite:** `Entity_Ai_Effect_Boutmanfish_Bass.cs:7-20`.

### `Entity_Ai_Effect_Boutmanfish_Bassboost` (76 lines)
- **Does:** Blink movement: on the holder's `Move` action, picks the target tile (Surface nearest the goal position
  within 3, or the standable tile nearest the goal object); if it is > 2 away or unreachable, moves the holder there
  instantly (`Moveblock` + `Entityposition`) and sets the command to the stored `Goalstring` or `Idle`.
- **Attached by:** upgrade on a mover (needs `Commandlistserver["Move"]`, from `Entity_Ai_Mob_Movement`).
- **Reads:** no params; `Move` command required at Starting.
- **Hard-coded:** tag `Surface`; vfx `World_Boutmanteleportstart`, `World_Boutmanteleport`.
- **Hooks:** Starting, Action.
- **Reuse:** `data-only` - teleporting walker for any unit.
- **Quirks:** NRE when no tile is found (`Toposition.Position` read before the null check); the normal movement AI
  also gets the same Move action.
- **Cite:** `Entity_Ai_Effect_Boutmanfish_Bassboost.cs:10-15`, `:17-52`, `:53-74`.

### `Entity_Ai_Effect_Boutmanfish_Caviarbuff` (52 lines)
- **Does:** One-shot payout: on command `Mine` (fired by `Resource_Mine`/`Resource_Opal` when their ore is depleted)
  gives `Amount_Base` of each aura to the holder's Owner (Split) and removes itself; also pays on death.
- **Attached by:** upgrade, meant for ore nodes.
- **Reads:** required `Amount_Base`.
- **Hard-coded:** `Redaura`, `Blueaura`, `Yellowaura`; vfx `Debuff_Caviar` refreshed every 1 s.
- **Hooks:** Command, Reallyslowtick, Despawn.
- **Reuse:** `data-only` - treasure-ore marker.
- **Quirks:** `Give` appends to a list that is never cleared (a second payout would pay double).
- **Cite:** `Entity_Ai_Effect_Boutmanfish_Caviarbuff.cs:8-50`; `Entity_Ai_Resource_Mine.cs:127`.

### `Entity_Ai_Effect_Boutmanfish_Puffer` (39 lines)
- **Does:** Poison: every 0.2 s the holder hurts itself for `Percent_Base` % of its CURRENT Health (Data `Poison`=1);
  removes itself after `Duration_Base` s.
- **Attached by:** upgrade, any entity.
- **Reads:** required `Percent_Base`, `Duration_Base`; holder `Health` (optional).
- **Hard-coded:** vfx `Debuff_Poison`; Data key `Poison`.
- **Hooks:** Starting, Slowtick.
- **Reuse:** `data-only` - percent-health DoT.
- **Quirks:** source is the holder (`Interact(Master, Master)`), so the holder's outgoing hooks run and kill credit is
  its own; percent of current health means it cannot kill alone; one extra tick after removal.
- **Cite:** `Entity_Ai_Effect_Boutmanfish_Puffer.cs:14-37`.

### `Entity_Ai_Effect_Boutmanmine_Boost` (31 lines)
- **Does:** While the holder's `Following` stat > 0, adds `Damageresource = Resourcedamage_Base` to each outgoing Hurt
  (extra damage to ore's `Healthresource`); removes itself when `Following` <= 0.
- **Attached by:** upgrade on Boutman followers.
- **Reads:** required `Resourcedamage_Base`; holder stat `Following` required (KeyNotFound each second otherwise).
- **Hard-coded:** vfx `Debuff_Boutman_Yellow`.
- **Hooks:** Interact, Starting, Reallyslowtick.
- **Reuse:** `vessel-bound` - `Following` is raised by `Entity_Ai_Hero_Boutman` rallies (`Entity_Ai_Hero_Boutman.cs:291`);
  a mod entity with its own `Following` stat could hold it.
- **Quirks:** `Data.Add` throws if the hit already has `Damageresource` (two of these, or with `Smackyhut_Buff`).
- **Cite:** `Entity_Ai_Effect_Boutmanmine_Boost.cs:7-29`; `Helper_Damagetools.cs:47-52`.

### `Entity_Ai_Effect_Boutmanrecruit_Boost` (48 lines)
- **Does:** While `Following` > 0: Range, Attackspeed, Damage Pmulti; removes itself when `Following` <= 0.
- **Attached by:** upgrade on Boutman followers.
- **Reads:** required `Range_Multi`, `Attackspeed_Multi`, `Damage_Multi` (applied as Pmulti); stat `Following` required.
- **Hard-coded:** vfx `Debuff_Boutman_Red`.
- **Hooks:** Starting, Downgrade, Reallyslowtick.
- **Reuse:** `vessel-bound` - same `Following` gate as above.
- **Cite:** `Entity_Ai_Effect_Boutmanrecruit_Boost.cs:11-47`.

### `Entity_Ai_Effect_Boutmanspawnbed_Boost` (34 lines)
- **Does:** Timed Damage Multi `Damage_Multi` for `Duration_Base` s, then removes itself.
- **Attached by:** upgrade, any entity.
- **Reads:** required `Damage_Multi`, `Duration_Base`.
- **Hard-coded:** vfx `Debuff_Boutman_Blue`.
- **Hooks:** Starting, Downgrade.
- **Reuse:** `data-only`.
- **Cite:** `Entity_Ai_Effect_Boutmanspawnbed_Boost.cs:10-33`.

### `Entity_Ai_Effect_Brittle` (54 lines)
- **Does:** Tile trigger: subscribes to its tile's `Updatefunct`; the first time a living being whose entity data
  tags contain `Enemy` is on the tile, spawns Mob `Block_Nobuild_Brittle` there (child of the holder) and unsubscribes.
- **Attached by:** entity (no `Upgradeobj` use); holder must stand on a tile.
- **Reads:** nothing.
- **Hard-coded:** Mob `Block_Nobuild_Brittle`; tag `Enemy` (from `Being.Entity.Tags`, data tags).
- **Hooks:** Spawn, Despawn, `Genitem.Updatefunct`.
- **Reuse:** `data + stock asset` - "enemy steps here -> spawn X" trap pattern, X fixed.
- **Cite:** `Entity_Ai_Effect_Brittle.cs:10-35`, `:37-53`.

### `Entity_Ai_Effect_Bustercheckpoint_Attackaura` (85 lines)
- **Does:** Damage Multi from Spawn; on the holder's next outgoing Hurt pays `Amount_Base` of each aura to its Owner
  and removes itself (with `Duration_Base` present: removes after that delay instead).
- **Attached by:** upgrade, any attacker.
- **Reads:** required `Damage_Multi`, `Amount_Base`; optional `Duration_Base` (switches mode).
- **Hard-coded:** aura names; vfx `Debuff_Auragaincheckpoint`, `Debuff_Auragaincheckpointbigger`.
- **Hooks:** Spawn, Interact, Reallyslowtick, Downgrade.
- **Reuse:** `data-only` - "first hit pays out" bounty/checkpoint pickup.
- **Quirks:** in `Duration_Base` mode EVERY hit during the delay pays again and starts another timer.
- **Cite:** `Entity_Ai_Effect_Bustercheckpoint_Attackaura.cs:11-33`, `:35-64`.

### `Entity_Ai_Effect_Busterdog_Towerboost` (39 lines)
- **Does:** Timed Damage and Range Multi for `Duration_Base` s, then removes itself.
- **Attached by:** upgrade, any entity.
- **Reads:** required `Damage_Multi`, `Range_Multi`, `Duration_Base`.
- **Hard-coded:** vfx `World_Effect_Busterdogborder`.
- **Hooks:** Spawn, Downgrade.
- **Reuse:** `data-only` - timed tower buff.
- **Cite:** `Entity_Ai_Effect_Busterdog_Towerboost.cs:9-37`.

### `Entity_Ai_Effect_Busterpad_Collision` (80 lines)
- **Does:** Ram: on the holder's first outgoing Hurt, hits every other-team Mob/Tower within `Range_Base` for
  `Damage_Base + target Health x (Damageperspeed_Base x holder Speed / 100)`, stuns them and itself (Speed Override
  0.01 for 1 s) and removes itself; also removed as soon as the holder's state is not `Move`/`Movelinear`.
- **Attached by:** upgrade on a moving unit with a `Speed` stat.
- **Reads:** required `Range_Base`, `Damage_Base`, `Damageperspeed_Base`; stat `Speed` (holder), `Health` (targets).
- **Hard-coded:** tags `Mob`, `Tower`; vfx `Debuff_Dogcollision`, `Debuff_Stun`, `World_Dogbigcollision`.
- **Hooks:** Spawn, Interact, Reallyslowtick, Slowtick.
- **Reuse:** `data-only` - charge/impact attack.
- **Cite:** `Entity_Ai_Effect_Busterpad_Collision.cs:11-50`, `:57-79`.

### `Entity_Ai_Effect_Busterpad_Speed` (69 lines)
- **Does:** Speed boost fed by interacts: incoming interact Type `Speed` adds Data `Speed` (clamped to `Maxspeed`) to an
  Add mod on Speed; decays by `Decay_Base` per s; removed at 0 or when state is not `Move`/`Movelinear`.
- **Attached by:** upgrade; something must send Type `Speed` interacts (`Entity_Ai_Trap_Busterpad`).
- **Reads:** required `Decay_Base`; Data `Speed`, `Maxspeed` required on a `Speed` interact.
- **Hard-coded:** state names `Move`, `Movelinear`.
- **Hooks:** Spawn, Oninteract, Slowtick.
- **Reuse:** `data-only` - but needs a sender of `Speed` interacts (the Buster pad trap, or mod code).
- **Quirks:** starts at Speed 1; `Kill` can run twice in one tick (Removeupgrade is then a no-op).
- **Cite:** `Entity_Ai_Effect_Busterpad_Speed.cs:14-36`, `:38-67`.

### `Entity_Ai_Effect_Busterpad_Towerbuff` (17 lines)
- **Does:** Permanent Attackspeed Pmulti until the upgrade is removed.
- **Attached by:** upgrade.
- **Reads:** required `Attackspeed_Pmulti`.
- **Hard-coded:** none.
- **Hooks:** Starting, Downgrade.
- **Reuse:** `data-only`.
- **Cite:** `Entity_Ai_Effect_Busterpad_Towerbuff.cs:5-16`.

### `Entity_Ai_Effect_Bustertotem_Blaze` (78 lines)
- **Does:** Ramping fuse: creates exposed stat `Blaze` (counter, max `Damage_Max`) starting at `Damage_Base`; each
  0.2 s the tick rate grows by `Damagetick_Ramp` and Blaze grows; every incoming Hurt resets the fuse to `Duration_Base`.
  When the fuse runs out or Blaze reaches `Damage_Max`, the holder hurts itself for the stored Blaze and the effect ends.
- **Attached by:** upgrade, any entity.
- **Reads:** required `Damagetick_Base`, `Damagetick_Ramp`, `Damage_Max`, `Duration_Base`, `Damage_Base`; creates stat `Blaze`.
- **Hard-coded:** vfx `Debuff_Blaze`, `World_Blazeexpiry`.
- **Hooks:** Oninteract, Starting, Slowtick.
- **Reuse:** `data-only` - "keep hitting it to build a bomb" mark.
- **Quirks:** burst is self-inflicted (holder's outgoing hooks run, holder gets the kill).
- **Cite:** `Entity_Ai_Effect_Bustertotem_Blaze.cs:20-47`, `:49-77`.

### `Entity_Ai_Effect_Calamari3` (33 lines)
- **Does:** Thorns: each incoming Hurt without `Spike` hits the attacker for `Damage_Base` (tagged `Spike`); removes
  itself once the holder has no `Shopbuff` modifier.
- **Attached by:** upgrade from the Fie shop.
- **Reads:** required `Damage_Base`; modifier flag `Shopbuff`.
- **Hard-coded:** `Shopbuff`; Data `Spike`; vfx `Fieshop_Spike`.
- **Hooks:** Oninteract, Slowtick.
- **Reuse:** `vessel-bound` - `Shopbuff` is set only by `Entity_Ai_Tower_Fieshop` (`Entity_Ai_Tower_Fieshop.cs:90`);
  without it the effect deletes itself within 0.2 s. Use `Smackywall_Bramble` for plain thorns.
- **Cite:** `Entity_Ai_Effect_Calamari3.cs:8-32`.

### `Entity_Ai_Effect_Chute` (23 lines)
- **Does:** Speed Override `Speed_Override` until removed, and plays the holder's `Herofall` visual.
- **Attached by:** upgrade, meant for vessels (drop-in).
- **Reads:** required `Speed_Override`.
- **Hard-coded:** `Vis_ClientRpc("Herofall")`.
- **Hooks:** Starting, Downgrade.
- **Reuse:** `data-only` - root while landing (the visual only exists on models that define `Herofall`; unclear otherwise).
- **Cite:** `Entity_Ai_Effect_Chute.cs:7-22`.

### `Entity_Ai_Effect_Enemy_Weaken` (85 lines)
- **Does:** Stacking melt: creates stat `Enemyweaken` (stack count, starts 1); each incoming Hurt carrying Data
  `Enemyweaken` adds a stack and resets the timer to `Duration_Base`; every 1 s adds `drain x dt x stacks` to Multi
  mods on Health, Attackspeed, Damage, Speed. Ends when the timer runs out.
- **Attached by:** upgrade (stock asset `Effect_Enemy_Weakened_Upgradeobject`, applied by `Enemy_Explode_Upgrade`).
- **Reads:** required `Duration_Base`, `Health_Drain`, `Attackspeed_Drain`, `Damage_Drain`, `Speed_Drain`; Data `Enemyweaken`.
- **Hard-coded:** stat `Enemyweaken`; vfx `Debuff_Melting`.
- **Hooks:** Oninteract, Starting, Reallyslowtick.
- **Reuse:** `data-only` - stacking debuff that worsens over time.
- **Quirks:** the four Multi mods are NEVER removed (no Downgrade/Despawn): the accumulated stat loss is permanent
  after the effect ends.
- **Cite:** `Entity_Ai_Effect_Enemy_Weaken.cs:23-33`, `:35-59`, `:61-84`.

### `Entity_Ai_Effect_Fiewall_Protection` (26 lines)
- **Does:** At priority 9 multiplies incoming Hurt `Damagemulti` by `1 - Damage_Resist`; removes itself when the holder
  has no `Protectbuff` modifier.
- **Attached by:** upgrade from Fie's wall.
- **Reads:** required `Damage_Resist`; flag `Protectbuff`.
- **Hard-coded:** `Protectbuff`.
- **Hooks:** Setpriority (9), Oninteract, Slowtick.
- **Reuse:** `vessel-bound` - `Protectbuff` comes from `Entity_Ai_Tower_Fiewall.cs:253`.
- **Quirks:** `Dead` starts true, so the `Count <= 0` branch is dead code; only a missing key ends it.
- **Cite:** `Entity_Ai_Effect_Fiewall_Protection.cs:3-24`.

### `Entity_Ai_Effect_Fiewall_Regen` (15 lines)
- **Does:** At priority 9 divides incoming Hurt `Damagemulti` by `1 + Resistadd` (holder stat). Despite the name, a
  damage resist, not a regen.
- **Attached by:** upgrade; holder needs stat `Resistadd` (else no effect).
- **Reads:** no params; stat `Resistadd` (optional).
- **Hard-coded:** none.
- **Hooks:** Setpriority (9), Oninteract.
- **Reuse:** `data-only` - resist scaled by a stat; `Resistadd` can be a data stat or come from `Smackywall_Beacon`.
- **Cite:** `Entity_Ai_Effect_Fiewall_Regen.cs:3-14`.

### `Entity_Ai_Effect_Fiewall_True` (50 lines)
- **Does:** Repel: when an attacker without the `Trueeffect` flag hits the holder, flags it for 3 s, spends one
  `Trueadd`, and if the attacker has a Parent hits it for `Damage_Base` (Data `True`) and moves it to a Surface tile
  next to that Parent (sends it home). Removes itself when `Trueadd` <= 0.
- **Attached by:** upgrade; holder needs stat `Trueadd`.
- **Reads:** required `Damage_Base`; stat `Trueadd` (gate); flag `Trueeffect` on attacker.
- **Hard-coded:** Mob `General_Mob` (placement test), tag `Surface`; vfx `Fiewall_Trueblast`, `Fiewall_Trueedge`.
- **Hooks:** Setpriority (9), Oninteract.
- **Reuse:** `data + stock asset` - a "knock back to spawn" charge shield if the holder's data gives `Trueadd` charges.
- **Quirks:** a charge is spent even when the attacker has no Parent (no effect); NRE if `Data.From` is null.
- **Cite:** `Entity_Ai_Effect_Fiewall_True.cs:12-48`.

### `Entity_Ai_Effect_Fiewall_Weak` (28 lines)
- **Does:** Speed and Damage Multi until removed.
- **Attached by:** upgrade.
- **Reads:** required `Speed_Multi`, `Damage_Multi`.
- **Hard-coded:** removes vfx `Debuff_Broken` (never adds it).
- **Hooks:** Spawn, Downgrade.
- **Reuse:** `data-only`.
- **Quirks:** removes `Debuff_Broken` on ANY downgrade of the holder, not only its own.
- **Cite:** `Entity_Ai_Effect_Fiewall_Weak.cs:7-27`.

### `Entity_Ai_Effect_Killonspawn` (22 lines)
- **Does:** Despawns the holder (Death true, reason `Self`) `Delay_Base` s after Starting.
- **Attached by:** upgrade.
- **Reads:** required `Delay_Base`.
- **Hard-coded:** none.
- **Hooks:** Starting, Tick.
- **Reuse:** `data-only` - lifetime for summons/temporary objects (as an upgrade; `General_Expire` is the entity form).
- **Quirks:** keeps calling Despawn every tick until gone.
- **Cite:** `Entity_Ai_Effect_Killonspawn.cs:9-20`.

### `Entity_Ai_Effect_Ramen3` (37 lines)
- **Does:** Self-heal `Heal_Base` every 1 s (`Armorhealmulti` 0.05) while the holder has a `Shopbuff` modifier.
- **Attached by:** upgrade from the Fie shop.
- **Reads:** required `Heal_Base`; flag `Shopbuff`.
- **Hard-coded:** `Shopbuff`.
- **Hooks:** Starting, Reallyslowtick.
- **Reuse:** `vessel-bound` - `Shopbuff` gate (see Calamari3).
- **Cite:** `Entity_Ai_Effect_Ramen3.cs:9-35`.

### `Entity_Ai_Effect_Smackyhut_Buff` (61 lines)
- **Does:** Sets skin `Buff`; Health/Speed/Attackspeed Multi, Damage +1 (Add); outgoing Hurts on `Resource` targets
  get `Damageresource = Damage_Resource`. Downgrade restores mods and the previous skin.
- **Attached by:** upgrade on Smacky's units.
- **Reads:** required `Health_Multi`, `Speed_Multi`, `Attackspeed_Multi`, `Damage_Resource` (per ore hit).
- **Hard-coded:** skin `Buff`; tag `Resource`.
- **Hooks:** Spawn, Interact, Downgrade.
- **Reuse:** `data-only` - stat buff + mining bonus (the `Buff` skin only exists on Smacky models).
- **Quirks:** `Data.Add("Damageresource")` throws if another script already added it.
- **Cite:** `Entity_Ai_Effect_Smackyhut_Buff.cs:15-35`, `:37-60`.

### `Entity_Ai_Effect_Smackyhut_Scrap` (43 lines)
- **Does:** Each outgoing Hurt on a `Resource` gives the holder's Owner 1 aura of each colour the target has a
  `<Colour>auramine` stat for.
- **Attached by:** upgrade, any attacker.
- **Reads:** no params; target stats `Redauramine`, `Blueauramine`, `Yellowauramine` (optional).
- **Hard-coded:** tag `Resource`; aura names.
- **Hooks:** Interact.
- **Reuse:** `data-only` - "scavenger" income per ore hit.
- **Cite:** `Entity_Ai_Effect_Smackyhut_Scrap.cs:8-42`.

### `Entity_Ai_Effect_Smackyhut_Towerbuff` (59 lines)
- **Does:** Timed Range/Attackspeed/Damage Pmulti for `Duration_Base` s with a border vfx, then removes itself.
- **Attached by:** upgrade.
- **Reads:** required `Range_Pmulti`, `Attackspeed_Pmulti`, `Damage_Pmulti`, `Duration_Base`, `Effect_Skin` (>=1 picks the firm border).
- **Hard-coded:** vfx `World_Effect_Smackyhut_Engineerborder`, `World_Effect_Smackyhut_Engineerborderfirm`.
- **Hooks:** Spawn, Downgrade, Despawn.
- **Reuse:** `data-only`.
- **Quirks:** only the non-firm border vfx is removed on Downgrade/Despawn (firm one lingers its full duration).
- **Cite:** `Entity_Ai_Effect_Smackyhut_Towerbuff.cs:11-58`.

### `Entity_Ai_Effect_Smackywall_Beacon` (93 lines)
- **Does:** After 0.1 s adds Health `+Effect_Add` and another `+Effect_Add` every Tide. The first beacon on an entity
  creates stat `Resistadd` (Add 1, Multi `Effect_Multi-1`) and at priority 9 divides incoming `Damagemulti` by
  `Effect_Multi`; later beacons only add Multi to `Resistadd`.
- **Attached by:** upgrade, any entity.
- **Reads:** required `Effect_Add`, `Effect_Multi`; creates/reads stat `Resistadd`.
- **Hard-coded:** stat `Resistadd`.
- **Hooks:** Setpriority (9), Oninteract, Starting, Downgrade, Despawn, `Game_Mapvars.Instance.Wave.OnValueChanged`.
- **Reuse:** `data-only` - per-Tide growing wall + resist.
- **Quirks:** the Health mod (base + per-Tide growth) is never removed; `Resistadd` from later beacons only matters to
  readers such as `Fiewall_Regen`/`Tower_Fiewall`.
- **Cite:** `Entity_Ai_Effect_Smackywall_Beacon.cs:21-32`, `:34-61`, `:63-92`.

### `Entity_Ai_Effect_Smackywall_Bramble` (20 lines)
- **Does:** Thorns: each incoming Hurt without `Spike` hits the attacker for `Effect_Add` (tagged `Spike`).
- **Attached by:** upgrade, any entity.
- **Reads:** required `Effect_Add`.
- **Hard-coded:** Data `Spike`.
- **Hooks:** Oninteract.
- **Reuse:** `data-only` - plain thorns, no gate.
- **Quirks:** NRE if `Data.From` is null; the thorn hit runs the holder's own outgoing hooks.
- **Cite:** `Entity_Ai_Effect_Smackywall_Bramble.cs:5-18`.

### `Entity_Ai_Effect_Smackywall_Regen` (59 lines)
- **Does:** Each Tide change heals every same-team Mob/Tower within `Range_Base` for `Effect_Add x holder Level`
  (`Armorhealmulti` 0.05) with a pulse vfx.
- **Attached by:** upgrade, any entity.
- **Reads:** required `Effect_Add`, `Range_Base`; holder `Entitydata.Level`.
- **Hard-coded:** tags `Mob`, `Tower`; vfx `World_Tower_Smackywall_Healpulse`.
- **Hooks:** Starting, Downgrade, Despawn, `Wave.OnValueChanged`.
- **Reuse:** `data-only` - per-Tide heal aura.
- **Quirks:** heal amount uses the Level at Starting (later level-ups ignored).
- **Cite:** `Entity_Ai_Effect_Smackywall_Regen.cs:12-43`, `:45-58`.

### `Entity_Ai_Effect_Snosnoheater_Scorch` (69 lines)
- **Does:** Burn: each incoming Hurt carrying Data `Burn` resets the timer to `Duration_Base`; once `Threshold_Base` s
  have passed since Starting, a Burn hit activates scorch (flag `Scorch`): every 0.2 s the holder hurts itself for a
  value starting at `Damage_Base` and growing by `Damage_Add` up to about `Damage_Max`.
- **Attached by:** upgrade; needs hits that carry `Burn` (Sno heater, `Entity_Ai_Tower_Smackyarcher`).
- **Reads:** required `Damage_Base`, `Threshold_Base`, `Damage_Add`, `Duration_Base`, `Damage_Max`; Data `Burn`.
- **Hard-coded:** Data `Burn`; flag `Scorch`; vfx `Debuff_Scorch`.
- **Hooks:** Oninteract, Starting, Slowtick.
- **Reuse:** `data-only` if the mod's hits carry a `Burn` key.
- **Quirks:** self-inflicted damage (holder's hooks, own kill credit); a timed `Scorch` mod is added on every Burn hit.
- **Cite:** `Entity_Ai_Effect_Snosnoheater_Scorch.cs:24-47`, `:49-67`.

### `Entity_Ai_Effect_Snosnoheater_Shock` (35 lines)
- **Does:** The next incoming Hurt carrying Data `Snoball` gets `+Effect_Multi` Damagemulti, then the effect ends;
  also ends after `Duration_Base` s.
- **Attached by:** upgrade.
- **Reads:** required `Effect_Multi`, `Duration_Base`; Data `Snoball`, `Damagemulti`.
- **Hard-coded:** Data `Snoball`; vfx `Debuff_Shock`.
- **Hooks:** Oninteract, Starting, Downgrade.
- **Reuse:** `data-only` if the follow-up hits carry `Snoball` (Sno's snowballs do).
- **Cite:** `Entity_Ai_Effect_Snosnoheater_Shock.cs:5-34`.

### `Entity_Ai_Effect_Snosnoheater_Sweat` (38 lines)
- **Does:** Timed Attackspeed Pmulti `Effect_Multi` for `Duration_Base` s.
- **Attached by:** upgrade.
- **Reads:** required `Effect_Multi`, `Duration_Base`.
- **Hard-coded:** vfx `World_Effect_Snosnoheater_Sweat`.
- **Hooks:** Spawn, Downgrade, Despawn.
- **Reuse:** `data-only`.
- **Cite:** `Entity_Ai_Effect_Snosnoheater_Sweat.cs:7-37`.

### `Entity_Ai_Effect_Snosnosnow` (139 lines)
- **Does:** Sno's spreading snow tile. Skin = `Levelskin` string + `ceil(Level/10)`; despawns at Level <= 0. On an
  incoming `Grow` interact (Data `Amount`, `Spread`) with `Amount > 1`: if `Drain <= Level`, passes a split `Grow` to
  adjacent Sno snow farther from the source and spawns new `Effect_Snosnosnow` on empty adjacent Surfaces (copying
  tags and upgrades); own Level rises by the split amount.
- **Attached by:** entity `Effect_Snosnosnow`.
- **Reads:** stats `Level`, `Levelskin` (`Value_String`), `Drain` required; Data `Amount`, `Spread`.
- **Hard-coded:** Mob `General_Effect`, `Effect_Snosnosnow`; tags `Surface`, `Sno`.
- **Hooks:** Spawn, Oninteract, Upgrade, `Link_Stats.Changed`.
- **Reuse:** `vessel-bound` - Sno's snow system (`Grow` comes from `Projectile_Ai_Sno.cs:47`).
- **Cite:** `Entity_Ai_Effect_Snosnosnow.cs:22-55`, `:57-119`, `:129-138`.

### `Entity_Ai_Effect_Snosnosnow_Black` (7 lines)
- **Does:** Adds runtime tag `Snoblack` on Spawn.
- **Attached by:** upgrade/entity.
- **Reads:** nothing. **Hard-coded:** tag `Snoblack`. **Hooks:** Spawn.
- **Reuse:** `vessel-bound` - the tag means something only to Sno code.
- **Cite:** `Entity_Ai_Effect_Snosnosnow_Black.cs:3-6`.

### `Entity_Ai_Effect_Snosnosnow_Rich` (42 lines)
- **Does:** On death gives the holder's Owner `Amount_Base` aura per colour tag `Richred`/`Richblue`/`Richyellow`.
- **Attached by:** upgrade.
- **Reads:** required `Amount_Base` (only when dying with one of the tags).
- **Hard-coded:** tags `Richred`, `Richblue`, `Richyellow`; aura names.
- **Hooks:** Despawn.
- **Reuse:** `data-only` - death bounty; colours chosen by data tags.
- **Cite:** `Entity_Ai_Effect_Snosnosnow_Rich.cs:8-41`.

### `Entity_Ai_Effect_Snosnowall_Breaker` (38 lines)
- **Does:** Health/Speed/Damage Multi with `Debuff_Broken` shown (9999 s) until removed.
- **Attached by:** upgrade.
- **Reads:** required `Health_Multi`, `Speed_Multi`, `Damage_Multi`.
- **Hard-coded:** vfx `Debuff_Broken`.
- **Hooks:** Spawn, Downgrade.
- **Reuse:** `data-only` - visible permanent debuff.
- **Quirks:** removes `Debuff_Broken` on ANY downgrade of the holder.
- **Cite:** `Entity_Ai_Effect_Snosnowall_Breaker.cs:11-37`.

### `Entity_Ai_Effect_Speed` (30 lines)
- **Does:** Speed Multi `Speed_Multi`, plus Speed Override if `Speed_Override` is given; until removed.
- **Attached by:** upgrade.
- **Reads:** required `Speed_Multi`; optional `Speed_Override`.
- **Hard-coded:** none.
- **Hooks:** Starting, Downgrade.
- **Reuse:** `data-only` - slow/haste/root (Override ~0 roots).
- **Cite:** `Entity_Ai_Effect_Speed.cs:7-29`.

### `Entity_Ai_Effect_Sushi3` (30 lines)
- **Does:** Intended: outgoing Hurts get `+Damage_Multi` if `Cooldown_Base` s passed since the last hit. Removes itself
  without a `Shopbuff` modifier.
- **Attached by:** upgrade from the Fie shop.
- **Reads:** required `Cooldown_Base`, `Damage_Multi`; flag `Shopbuff`.
- **Hard-coded:** `Shopbuff`; vfx `Shot_Shootimpactspecial`.
- **Hooks:** Interact, Slowtick.
- **Reuse:** `vessel-bound` (Shopbuff gate) and broken.
- **Quirks:** the test is `Ticks - Time.time > Cooldown_Base` (last-hit time minus now, always <= 0): the bonus never
  fires for a positive cooldown.
- **Cite:** `Entity_Ai_Effect_Sushi3.cs:9-29`.

### `Entity_Ai_Effect_Tastes` (28 lines)
- **Does:** Health Multi `Health_Multi` while the holder has a `Shopdebuff` modifier.
- **Attached by:** upgrade from the Fie shop debuff tower.
- **Reads:** required `Health_Multi`; flag `Shopdebuff`.
- **Hard-coded:** `Shopdebuff`.
- **Hooks:** Starting, Slowtick, Downgrade.
- **Reuse:** `vessel-bound` - `Shopdebuff` comes from `Entity_Ai_Tower_Fieshop_Debuff.cs:50`.
- **Cite:** `Entity_Ai_Effect_Tastes.cs:7-27`.

### `Entity_Ai_Effect_Tower_Merchut` (23 lines)
- **Does:** Vulnerability mark: every incoming Hurt gets `+Effect_Multi` Damagemulti; shows `Debuff_Targeted` after 1 s
  (9999 s).
- **Attached by:** upgrade (applied by `Entity_Ai_Tower_Merchut`).
- **Reads:** required `Effect_Multi`; Data `Damagemulti` required (no ContainsKey guard: a Hurt without it throws).
- **Hard-coded:** vfx `Debuff_Targeted`.
- **Hooks:** Oninteract, Starting.
- **Reuse:** `data-only` - "marked: takes more damage".
- **Cite:** `Entity_Ai_Effect_Tower_Merchut.cs:6-22`.

---

## Enemies and elite upgrades (16)

Shared enemy loop: attack AIs create an `Attack` command (server-only, state-changing). `Helper_Entityai.Checkenemyranged`
sets `Attack.Goalobject` to the nearest (or `Far`: farthest) VISIBLE target within stat `Range` and calls
`Updatecommand("Attack")`; `Checkenemymelee` uses nearest within `Range + 0.1`, no visibility
(`Helper_Entityai.cs:91-118`). `Action("Attack")` fires only when the state actually changes (`Helper_Entitytools.cs:326-332`),
so each AI returns to `Idle` after its attack. Walking comes from `Entity_Ai_Mob_Movement` (`Move` command,
`Entity_Ai_Mob_Movement.cs:34`) and `Entity_Ai_Enemy_Basic` (go to Goal). While attack-ready and in state `Move`, most
attack AIs decrement `Commandlistserver["Move"].Goalvalue` (a stop distance read at `Entity_Ai_Mob_Movement.cs:194`;
purpose unclear). "Enemy targets" below = other-team, non-neutral entities (`Teamfor=false`, `Helper_Entityquerytools.cs:48-63`).

### `Entity_Ai_Enemy_Aegis` (34 lines)
- **Does:** Regen: every 1 s, if `Healtime` s have passed since `Master.Lasthittime`, heals itself `Heal`.
- **Attached by:** entity (enemy).
- **Reads:** stats `Healtime`, `Heal` required (read once at Starting).
- **Hard-coded:** vfx `Debuff_Aegisheal`.
- **Hooks:** Starting, Reallyslowtick.
- **Reuse:** `data-only` - out-of-combat regen for any enemy or unit.
- **Quirks:** `Lasthittime` is set only by `Interacthurt` hits without an explicit `Damage` key, so scripted hits with
  explicit Damage (thorns, explosions, slams) do not interrupt the regen; heal value fixed at Starting.
- **Cite:** `Entity_Ai_Enemy_Aegis.cs:10-33`; `Helper_Entitytools.cs:150-155`.

### `Entity_Ai_Enemy_Armor_Upgrade` (33 lines)
- **Does:** Armored elite: skin `Armor`; adds stat `Healtharmor` = `Healtharmor_Base + base Health / Healtharmor_Perhealth`.
  Each hit removes exactly 1 x Damagemulti armor before Health is touched (hit-count armor).
- **Attached by:** upgrade (elite); no-op at entity level.
- **Reads:** required `Healtharmor_Base`, `Healtharmor_Perhealth`; stat `Health` (optional).
- **Hard-coded:** skin `Armor`; stat `Healtharmor`.
- **Hooks:** Starting, Downgrade.
- **Reuse:** `data-only` - "N hits to break" shield on anything.
- **Quirks:** Downgrade sets skin to `""`, clobbering other elite skins.
- **Cite:** `Entity_Ai_Enemy_Armor_Upgrade.cs:5-32`; `Helper_Damagetools.cs:17-27`.

### `Entity_Ai_Enemy_Ballista` (84 lines)
- **Does:** Ranged sniper. Targets: nearest visible enemy `Mob`/`Tower`/`Resource` in `Range`, checked each 0.2 s once
  `1/Attackspeed` s passed. Attack: faces the target, lunges 0.3 forward, fires homing projectile
  `Enemy_Ballista_Bolt` (Speed `Projspeed`, Accel /3, Zvelocity /4, Goal = target), slows itself -50 % for one attack
  interval, then Idle (resumes walking).
- **Attached by:** entity (enemy) with `Range`, `Attackspeed`, `Projspeed`.
- **Reads:** stats `Projspeed`, `Attackspeed`, `Range`; `Move` command.
- **Hard-coded:** projectile `Enemy_Ballista_Bolt`; tags `Mob`, `Tower`, `Resource`; vis `Shoot`.
- **Hooks:** Starting, Action, Slowtick.
- **Reuse:** `data + stock asset` - any enemy body can shoot ballista bolts.
- **Cite:** `Entity_Ai_Enemy_Ballista.cs:15-61`, `:63-83`.

### `Entity_Ai_Enemy_Basic` (52 lines)
- **Does:** Walker: creates `Goal`; whenever state is `Idle`/`Idleblocked`, copies the Parent's `Goal.Goalobject` if its
  own is empty and issues `Move` to it (goal object, no position); Idle if none.
- **Attached by:** entity (enemy), spawned as a child of an `Enemycore` (whose `Goal` is the core it targets).
- **Reads:** commands `Goal` (own), `Move` (from `Entity_Ai_Mob_Movement`), Parent's `Goal`.
- **Hard-coded:** state names.
- **Hooks:** Spawn, Slowtick.
- **Reuse:** `data-only` - the standard "walk to the core" brain.
- **Quirks:** NRE if Parent is null and no goal is set (spawned by something else).
- **Cite:** `Entity_Ai_Enemy_Basic.cs:6-51`.

### `Entity_Ai_Enemy_Boutman` (280 lines)
- **Does:** Caged story boss. Starts `Sleeping` (skin too); wakes when Tide >= `Awaketide` or no `Artriobarrier`
  entity is left; 5 s later warns "ALERT ... SOMEONE IS VERY GRUMPY" and applies `Enemy_Boutman_Tooltipwake_Upgradeproperties`.
  Awake: melee-attacks nearest enemy `Mob`/`Tower` (`Checkenemymelee`), otherwise walks to the nearest `Core` (1000
  radius); spawns `Mob_Boutmanfishmini` (skin `Evil`, Health Multi `Fishhealthmulti`) every `Fishspawninterval` s; its
  Damage grows by `Damageincrease` per s. Cannot die once awake: at 0 HP it is knocked out, and 5 s later spawns
  playable `Hero_Boutman` (team 1, subowners 0-9, tag `Prespawn`) and despawns. If a player already plays
  `Hero_Boutman`, it replaces itself with `Enemy_Reallystrongfish` at spawn.
- **Attached by:** entity `Enemy_Boutman_Entity` (most branches check that `Objectname`).
- **Reads:** stats `Awaketide`, `Fishspawninterval`, `Fishhealthmulti`, `Damageincrease`, `Range`, `Health`.
- **Hard-coded:** Mobs `General_Mob`, `Mob_Boutmanfishmini`, `Enemy_Reallystrongfish`; Hero `Hero_Boutman`; Upgrade
  `Enemy_Boutman_Tooltipwake_Upgradeproperties`; tags `Artriobarrier`, `Core`, `Surface`; vfx `World_Boutmanko`,
  `World_Fishrecruited`, `Debuff_Mad`; sound `Boutman_Getup`; strips `Resource` from `Entity_Ai_Mob_Combat` targets.
- **Hooks:** Spawn, Oninteract (priority 9, deals its own damage), Slowtick.
- **Reuse:** `vessel-bound` - bound to Boutman's hero, fish and the cage map.
- **Cite:** `Entity_Ai_Enemy_Boutman.cs:35-91`, `:93-133`, `:135-176`, `:178-279`.

### `Entity_Ai_Enemy_Bulwark` (122 lines)
- **Does:** Siege brute. Targets: nearest visible enemy `Mob`/`Tower`/`Resource` in `Range` (vision from 10 above).
  Attack: records up to `Projcount` target spots (nearest first, each farther than the previous + `Projradius`, inside
  `Maxrange`); after 1.6 s slams up to `Closecount` enemies within `Closerange` for `Closedamage`; 5 s later drops
  `Enemy_Bulwark_Bomb` on each spot 1 s apart; then Idle. All its outgoing hits stun the target (Damage, Speed,
  Attackspeed Override 0.01 for `Stunduration`).
- **Attached by:** entity (enemy).
- **Reads:** stats `Attackspeed`, `Range`, `Projcount`, `Projradius`, `Maxrange`, `Closecount`, `Closerange`,
  `Closedamage`, `Stunduration`.
- **Hard-coded:** projectile `Enemy_Bulwark_Bomb`; tags; vfx `Debuff_Disabled`; vis `Attack`, `Shoot`.
- **Hooks:** Starting, Interact, Action, Slowtick.
- **Reuse:** `data + stock asset` - its outgoing-hit stun (`Interact`) alone is a reusable "disabling hits" brain.
- **Quirks:** `Inattack` is never set, so only the `Attack` state gates re-attacks (~7.6 s+ per cycle with bombs).
- **Cite:** `Entity_Ai_Enemy_Bulwark.cs:17-41`, `:43-103`, `:105-121`.

### `Entity_Ai_Enemy_Chaos_Upgrade` (50 lines)
- **Does:** Elite: Health/Speed/Damage/Attackspeed Multi and skin `Chaos`.
- **Attached by:** upgrade (elite).
- **Reads:** required `Health_Multi`, `Speed_Multi`, `Damage_Multi`, `Attackspeed_Multi`.
- **Hard-coded:** skin `Chaos`.
- **Hooks:** Starting, Downgrade.
- **Reuse:** `data-only` (fine as a permanent upgrade).
- **Quirks:** Downgrade removes `Mod1` four times; Speed/Damage/Attackspeed mods stay forever.
- **Cite:** `Entity_Ai_Enemy_Chaos_Upgrade.cs:13-48`.

### `Entity_Ai_Enemy_Claymore` (119 lines)
- **Does:** Kamikaze. Targets: when any enemy `Tower` is within `Rangedetect`, picks the FARTHEST enemy `Tower` within
  `Range` and launches itself as `Enemy_Claymore_Bomb` (lob, ~1 s), hides at y=1000 and despawns 2 s later (not a death).
  Handles its own damage at priority 9 and zeroes it for later AIs; at 0 HP it stays at 0.1, turns untargetable, gets
  a `Death` flag, and after `Deathlife` s launches at the farthest enemy `Mob`/`Tower`/`Resource` in `Range` (if one is in
  `Rangedetect`) or dies.
- **Attached by:** entity (enemy).
- **Reads:** stats `Rangedetect`, `Range`, `Deathlife`, `Health`.
- **Hard-coded:** projectile `Enemy_Claymore_Bomb`; tags; vis `Attack`; command `Launch`.
- **Hooks:** Spawn (Setpriority 9), Oninteract, Slowtick.
- **Reuse:** `data + stock asset`.
- **Quirks:** zeroes `Damage` on EVERY interact type, so heals do nothing to it.
- **Cite:** `Entity_Ai_Enemy_Claymore.cs:17-49`, `:51-100`, `:107-118`.

### `Entity_Ai_Enemy_Excalibur` (73 lines)
- **Does:** Ranged. Targets: nearest visible enemy `Mob`/`Tower`/`Resource` in `Range`. Attack: 1.3 s windup, straight
  projectile `Enemy_Excalibur_Bolt` (velocity = direction x `Projspeed`), 1 s recovery, Idle.
- **Attached by:** entity (enemy).
- **Reads:** stats `Attackspeed`, `Range`, `Projspeed`.
- **Hard-coded:** projectile `Enemy_Excalibur_Bolt`; tags; vis `Attack`, `Shoot`.
- **Hooks:** Starting, Action, Slowtick.
- **Reuse:** `data + stock asset`.
- **Cite:** `Entity_Ai_Enemy_Excalibur.cs:15-54`, `:56-72`.

### `Entity_Ai_Enemy_Explode_Upgrade` (82 lines)
- **Does:** Elite: Health/Speed Multi, skin `Explode`. On death hits up to `Damage_Count` nearest enemy
  `Mob`/`Tower`/`Resource` within `Radius_Base` (ignores targetable) for `Damage_Base` (Data `Enemyweaken`) and applies
  `Effect_Enemy_Weakened_Upgradeobject` (the melt debuff) to those without it.
- **Attached by:** upgrade (elite).
- **Reads:** required `Health_Multi`, `Speed_Multi`, `Damage_Count`, `Radius_Base`, `Damage_Base`.
- **Hard-coded:** Upgrade `Effect_Enemy_Weakened_Upgradeobject`; skin `Explode`; vfx `Effect_Enemy_Explode`.
- **Hooks:** Starting, Downgrade, Despawn.
- **Reuse:** `data + stock asset` - death blast + debuff; works on any entity given by upgrade.
- **Quirks:** NRE on death if attached at entity level (`Upgradeobj` null in Despawn); the first blast's
  `Enemyweaken` key arrives before the debuff exists, so stacks start at 1.
- **Cite:** `Entity_Ai_Enemy_Explode_Upgrade.cs:16-31`, `:50-80`.

### `Entity_Ai_Enemy_Halberd` (307 lines)
- **Does:** Flying guardian. Guards a `Defenseobject`: at full health the nearest `Resource` (any team, 999 radius),
  otherwise its Parent. Travel: walks if something is overhead, else rises to target height +2, flies over at `Speedfly`
  and lands on the nearest standable tile. At a guarded ore: dives one enemy `Gatherer`/`Hero` within `Protectrange`
  (marks it with stat `Halberdblocked`), melee-attacks enemy `Mob`/`Tower`, and returns if pulled beyond
  `Protectrange`; an attacker that hits it becomes the new `Defenseobject`. Below `Scarepercent` health it flies home to
  its Parent, where it heals a ramping `Heal x time`, sets a Health Add mod to the Parent's `Currentmax`, and fights
  nearby enemies; back at full health it picks an ore again.
- **Attached by:** entity (enemy) spawned by an enemy core (needs Parent).
- **Reads:** stats `Heal`, `Speedfly`, `Scarepercent`, `Protectrange`, `Range`; Parent stat `Currentmax` (optional).
- **Hard-coded:** tags `Resource`, `Gatherer`, `Hero`, `Mob`, `Tower`, `Surface`; stat `Halberdblocked`; vfx
  `Debuff_Redtarget`, `Debuff_Aegisheal`; state `Flying`.
- **Hooks:** Spawn, Oninteract, Slowtick.
- **Reuse:** `data-only` - an ore-raider/defender enemy.
- **Quirks:** `Halberdblocked` is a permanent stat on EVERY Gatherer/Hero in range (each unit can be dived once per
  match, by any Halberd); `Healtime` keeps ramping until full health.
- **Cite:** `Entity_Ai_Enemy_Halberd.cs:33-63`, `:65-130`, `:132-139`, `:141-301`.

### `Entity_Ai_Enemy_Health_Upgrade` (29 lines)
- **Does:** Elite: Health Multi `Health_Multi`, skin `Health`.
- **Attached by:** upgrade. **Reads:** required `Health_Multi`. **Hard-coded:** skin `Health`.
- **Hooks:** Starting, Downgrade.
- **Reuse:** `data-only`.
- **Quirks:** Downgrade resets skin to `""`.
- **Cite:** `Entity_Ai_Enemy_Health_Upgrade.cs:7-28`.

### `Entity_Ai_Enemy_Hyper_Upgrade` (50 lines)
- **Does:** Elite: Health/Speed/Damage/Attackspeed Multi, skin `Hyper`.
- **Attached by:** upgrade.
- **Reads:** required `Health_Multi`, `Speed_Multi`, `Damage_Multi`, `Attackspeed_Multi`.
- **Hard-coded:** skin `Hyper`.
- **Hooks:** Starting, Downgrade.
- **Reuse:** `data-only` (as a permanent upgrade).
- **Quirks:** same Downgrade bug as Chaos (`Mod1` removed four times).
- **Cite:** `Entity_Ai_Enemy_Hyper_Upgrade.cs:13-48`.

### `Entity_Ai_Enemy_Maul` (81 lines)
- **Does:** Melee AoE. Targets: nearest enemy `Mob`/`Tower`/`Resource` within `Range + 0.1`. Attack: 1.3 s windup, then
  `Interacthurt` (Damage stat) on up to `Projcount` nearest enemies within `Projradius`, Idle.
- **Attached by:** entity (enemy).
- **Reads:** stats `Attackspeed`, `Range`, `Projcount`, `Projradius`, `Damage`.
- **Hard-coded:** tags; vis `Attack`, `Shoot`.
- **Hooks:** Starting, Action, Slowtick.
- **Reuse:** `data-only` - cleave attacker, no projectile.
- **Cite:** `Entity_Ai_Enemy_Maul.cs:17-62`, `:64-80`.

### `Entity_Ai_Enemy_Onager` (76 lines)
- **Does:** Artillery. Targets: FARTHEST visible enemy `Mob`/`Tower`/`Resource` in `Range`. Attack: lobs
  `Enemy_Onager_Bomb` at the ground under the target (flight `Projduration`, `Count` = `Projcount`), Idle.
- **Attached by:** entity (enemy).
- **Reads:** stats `Attackspeed`, `Range`, `Projduration`, `Projcount`.
- **Hard-coded:** projectile `Enemy_Onager_Bomb`; tags; vis `Shoot`.
- **Hooks:** Starting, Action, Slowtick.
- **Reuse:** `data + stock asset`.
- **Cite:** `Entity_Ai_Enemy_Onager.cs:13-57`, `:59-75`.

### `Entity_Ai_Enemy_Red_Upgrade` (166 lines)
- **Does:** Aggro elite: Health/Damage Multi, skin `Red`. Picks an aggro target (enemy `Mob`/`Tower` within
  `Aggrorange_Base` without a `Redblocked` modifier, or a targetable Mob/Tower attacker) and overrides `Goal` and `Move`
  to chase it; after more than 5 checks stuck in `Idleblocked` it flags the target `Redblocked` for 5 s and drops it.
  Checks run every 1 s and on each hit.
- **Attached by:** upgrade (elite).
- **Reads:** required `Health_Multi`, `Damage_Multi`, `Aggrorange_Base`; commands `Move`, `Goal` (optional).
- **Hard-coded:** skin `Red`; flag `Redblocked`; vfx `Debuff_Redangry`, `Debuff_Redtarget`.
- **Hooks:** Spawn, Starting, Downgrade, Oninteract, Reallyslowtick.
- **Reuse:** `data-only` - "hunts towers" modifier for any walker.
- **Quirks:** the area loop keeps the LAST match, i.e. the FARTHEST unblocked target (list is sorted nearest first);
  attacker check reads `Redblocked` from Statsserver while the flag lives in Modifiers; Downgrade removes `Mod1` twice
  (Damage mod stays); NRE at entity level (`Upgradeobj` null).
- **Cite:** `Entity_Ai_Enemy_Red_Upgrade.cs:30-56`, `:58-148`, `:150-165`; `Helper_Entityquerytools.cs:177-180`.

---

## Cores, enemy cores, map brains (10)

### `Entity_Ai_Main_Core` (284 lines)
- **Does:** The basin core. **Commands answered:** `Spawn` (server): if custom setting `Difficultysetting` == 1 applies
  `Special_Treated_Upgradeproperties`; unless tagged `Noart` applies every chosen Cardinal's `Coreupgrades`
  (`Helper_Entityai.Applyartifacts`); spawns each active player's hero (`Tds_Clientinfo.Hero`) of the core's team on
  adjacent Surface tiles in turn, as children of the core. `Spawnmob` (server): spawns the hero named by
  `Spawnmob.Goalstring` for every player. `Hotspawn` is created but nothing handles it. Heroes respawn after stat
  `Respawn` s with their upgrades, owner and subowners (counts `Vesseldeaths`). Priority 9: the first hit that would kill
  it is nulled and Health set to 1. On death: vfx, then 4 s later kills every other-team `Mob`/`Tower` (10 radius, then
  all) with self-hits of 1e11.
- **Attached by:** entity (core).
- **Reads:** stats `Health`, `Respawn`; setting `Difficultysetting`; tag `Noart`; skin `Training`; `Display_Endgame`.
- **Hard-coded:** Mob `General_Mob`, `Mob_Localbuddy` (given to a hard-coded list of player names); Upgrade
  `Special_Treated_Upgradeproperties`; vfx `World_Core_Oneshot`, `World_Core_Death`, `World_Core_Breach`
  (`BASIN BREACHED`), `World_Coretraining_Death`.
- **Hooks:** Setpriority, Oninteract, Spawn, Command, Despawn, `Entityposition.OnValueChanged`, hero `Despawn`/`Loaded`.
- **Reuse:** `data-only` - a mod map's core or a second player core; the mode sends `Spawn` once (`Mode_Normal.cs:350`).
- **Quirks:** respawn fires on any hero despawn (not only death); no adjacent tile -> IndexOutOfRange in `Addplayer`;
  `Spawnmob` ignores team and active state.
- **Cite:** `Entity_Ai_Main_Core.cs:29-44`, `:46-63`, `:65-152`, `:154-200`, `:202-283`.

### `Entity_Ai_Main_Core_Treated` (156 lines)
- **Does:** Easy-mode package (once per match, static guard): sets `Display.Mode_Treated`; gives everyone the mode's
  `Enemies.Resourcesinit` x `Aura_Base` (capped `Aura_Basemax`); adds Health Base `Health_Base` to every `Core`; adds
  `Extra_Time` to `Time_Wave` when the phase turns `Between` and `Extra_Wavetime` when it turns `Wave` (3 s throttle);
  each killed `Enemy` with drops pays `(Aura_Drop-1) x` its drops plus the missing aura colours.
- **Attached by:** upgrade `Special_Treated_Upgradeproperties` on the core.
- **Reads:** required `Aura_Drop`, `Extra_Time`, `Extra_Wavetime`, `Aura_Base`, `Aura_Basemax`, `Health_Base`.
- **Hard-coded:** tags `Core`, `Enemy`; aura names; phase names `Between`, `Wave`.
- **Hooks:** Spawn, `Game_Entity.Ondespawn`, `Display.OnValueChanged`, `Time_Wave.OnValueChanged`.
- **Reuse:** `data-only` - a "blessing" that adds time, starting aura, bonus drops, core health.
- **Quirks:** never unsubscribes; missing-colour amount uses the LAST drop's amount.
- **Cite:** `Entity_Ai_Main_Core_Treated.cs:17-57`, `:59-109`, `:121-155`.

### `Entity_Ai_Main_Coresplit_Effect` (7 lines)
- **Does:** Tags the holder `Noart` on Spawn, so a core's `Spawn` command skips Cardinal core upgrades.
- **Attached by:** upgrade/entity on a secondary core. **Reads:** nothing. **Hard-coded:** tag `Noart`. **Hooks:** Spawn.
- **Reuse:** `data-only`.
- **Cite:** `Entity_Ai_Main_Coresplit_Effect.cs:3-6`; `Entity_Ai_Main_Core.cs:166-169`.

### `Entity_Ai_Main_Enemycore` (124 lines)
- **Does:** Enemy spawner. **Commands answered:** `Spawn` (server, Json `Spawndata {Spawn: Mob name, Upgrades: [upgrade
  names]}`): picks the next reachable `Core` in round-robin (list built by path test at Spawn), stores it in its `Goal`
  command, and spawns the Mob on the next adjacent Surface tile as its child with those upgrades as `Preupgrades`
  (elites), vfx `World_Enemyspawnborder`. `Goal` holds the target core (read by `Enemy_Basic` children). No stat drives
  spawning: what, when and where come from the mode (`Mode_Normal.cs:120-142`), which also sets its `Healthresource`
  (`Mode_Normal.cs:337-342`).
- **Attached by:** entity (enemy core).
- **Reads:** Json only; no params.
- **Hard-coded:** Mob `General_Mob`; tags `Core`, `Surface`; vfx `World_Enemyspawnborder`.
- **Hooks:** Spawn, Command, `Entityposition.OnValueChanged`.
- **Reuse:** `data-only` - mod nests/portals that the mode can command with any Mob + upgrades.
- **Quirks:** with an empty core list the call only rebuilds the list (that spawn is lost); the list is rebuilt only
  when the CURRENT entry is null; no adjacent tile -> IndexOutOfRange.
- **Cite:** `Entity_Ai_Main_Enemycore.cs:27-45`, `:56-123`.

### `Entity_Ai_Main_Enemycoresplit` (36 lines)
- **Does:** Map modifier: when Tide becomes 1, applies `Main_Enemycoresplit_Effect_Upgradeproperties` to every `Enemycore`.
- **Attached by:** entity (map object).
- **Reads:** nothing. **Hard-coded:** Upgrade `Main_Enemycoresplit_Effect_Upgradeproperties`; tag `Enemycore`.
- **Hooks:** Spawn, `Wave.OnValueChanged` (never unsubscribed).
- **Reuse:** `data + stock asset`.
- **Cite:** `Entity_Ai_Main_Enemycoresplit.cs:11-35`.

### `Entity_Ai_Main_Enemycoresplit_Effect` (118 lines)
- **Does:** Spawn multiplier. **Commands answered:** `Spawn` (server): relays the same Json as `Spawnsplit` to every
  OTHER enemy core (list taken when the effect started); `Spawnsplit` (server): spawns like `Enemycore` (next reachable
  core as Goal, next adjacent tile). With the effect on all N enemy cores, each mode spawn appears at all N.
- **Attached by:** upgrade on an `Enemycore` (needs its `Goal` command).
- **Reads:** Json; no params.
- **Hard-coded:** Mob `General_Mob`; tags `Core`, `Enemycore`, `Surface`; vfx `World_Enemyspawnborder`.
- **Hooks:** Spawn, Command.
- **Reuse:** `data-only` - "every portal spawns everything" modifier.
- **Quirks:** sets the holder `Option_Targetable = true` unconditionally after its query.
- **Cite:** `Entity_Ai_Main_Enemycoresplit_Effect.cs:21-49`, `:51-116`.

### `Entity_Ai_Main_Subcore` (97 lines)
- **Does:** Secondary core: priority-9 one-lethal-hit protection (Health set to 1, once); on death vfx and the same
  4 s "kill every other-team Mob/Tower" sweep as the core. No commands.
- **Attached by:** entity.
- **Reads:** stat `Health`; `Display_Endgame`.
- **Hard-coded:** vfx `World_Core_Oneshot`, `World_Subcore_Breach`, `World_Subcore_Death`; tags `Mob`, `Tower`.
- **Hooks:** Setpriority, Oninteract, Despawn.
- **Reuse:** `data-only` - a defend-this objective that survives one lethal hit.
- **Cite:** `Entity_Ai_Main_Subcore.cs:11-26`, `:28-96`.

### `Entity_Ai_Main_Chrono` (67 lines)
- **Does:** Clock map: on each Tide change sets `Time_Scale` 1 for `Lingertime` s, then to the current speed: 1.5 from
  Tide `Quickspeed`, 2 from `Doublespeed`, 2.25 from `Superspeed` (UI warnings "THE CLOCK QUICKENS" ...), back to 1 if
  the Tide drops below `Quickspeed`.
- **Attached by:** entity (map object).
- **Reads:** stats `Lingertime`, `Quickspeed`, `Doublespeed`, `Superspeed`.
- **Hard-coded:** warning key `Map_Chrono`; vfx `Map_Chronoeffect`.
- **Hooks:** Spawn, Despawn, `Wave.OnValueChanged`.
- **Reuse:** `data-only` - a "fast current" sea-weather event.
- **Quirks:** before `Quickspeed` no branch fires but the initial `Speedvalue` 1.25 is still applied: the game runs 1.25x.
- **Cite:** `Entity_Ai_Main_Chrono.cs:10-25`, `:27-54`, `:56-66`.

### `Entity_Ai_Main_Flood` (80 lines)
- **Does:** Rising water: each Tide (until `Maxheight`) rises by `Heightspeed` while below `Halfheight`, by
  `Halfheightspeed` while below `Quarterheight`, else by `Quarterheightspeed` (5 s tween); puts `Block_Flood_Deter` on
  every `Surface` tile below the water + 1. Projectiles touching its colliders have velocity multiplied by `Projslow`
  every physics stay.
- **Attached by:** entity (the water object; needs colliders under `Currenthit`).
- **Reads:** stats `Maxheight`, `Halfheight`, `Quarterheight`, `Heightspeed`, `Halfheightspeed`, `Quarterheightspeed`, `Projslow`.
- **Hard-coded:** Mob `Block_Flood_Deter`; tag `Surface`; physics layer 8.
- **Hooks:** Spawn, `Wave.OnValueChanged` (never unsubscribed), `Entity_Hitobject_Signals.Collidestay`.
- **Reuse:** `data + stock asset` - flood/tide event for a sea map.
- **Quirks:** `Projslow` compounds per physics step, not per second.
- **Cite:** `Entity_Ai_Main_Flood.cs:15-25`, `:27-65`, `:67-79`.

### `Entity_Ai_Main_Tpgate` (69 lines)
- **Does:** Teleport gate: 1 s after start, adds nav connections from its tile to every other `Tp`-tagged entity that
  shares one of its upgrade names (pairing by shared upgrade). When a being arrives on its tile from > 2 units away
  (i.e. through the link), snaps it to the gate with vfx.
- **Attached by:** entity tagged `Tp`; pairs need a common upgrade.
- **Reads:** nothing.
- **Hard-coded:** tag `Tp`; vfx `Debuff_Tp`, `World_Tp`; vis `Position`.
- **Hooks:** Starting, Spawn, Despawn, `Genitem.Updatefunct`.
- **Reuse:** `data-only` - whirlpool/portal pairs.
- **Quirks:** the connection task uses a default token (runs even if the gate is destroyed meanwhile).
- **Cite:** `Entity_Ai_Main_Tpgate.cs:11-30`, `:37-68`.

---

## General AIs (4)

### `Entity_Ai_General_Adjacentblocker` (21 lines)
- **Does:** On Spawn puts `Block_Noplacement` (child) on every adjacent placeable Surface tile.
- **Attached by:** entity/upgrade.
- **Reads:** nothing. **Hard-coded:** Mobs `General_Mob`, `Block_Noplacement`; tag `Surface`. **Hooks:** Spawn.
- **Reuse:** `data + stock asset` - no-build ring around an object.
- **Cite:** `Entity_Ai_General_Adjacentblocker.cs:6-19`.

### `Entity_Ai_General_Expire` (29 lines)
- **Does:** Despawns the holder (death) after stat `Duration` s, polling each game tick.
- **Attached by:** entity.
- **Reads:** stat `Duration` required. **Hard-coded:** none. **Hooks:** Spawn.
- **Reuse:** `data-only` - lifetime for summons, pickups, puddles.
- **Cite:** `Entity_Ai_General_Expire.cs:8-27`.

### `Entity_Ai_General_Spawnmobs` (72 lines)
- **Does:** Spawner: each `Defaultparams` entry is `Mob name -> respawn seconds`; one per second it spawns that Mob on
  the next adjacent Surface tile as a child; if the respawn value is not 0, each spawned mob is replaced that many
  seconds after it despawns (while the holder lives).
- **Attached by:** upgrade (reads `Upgradeobj`).
- **Reads:** all `Defaultparams` keys (Mob names; failures caught per entry).
- **Hard-coded:** Mob `General_Mob`; tag `Surface`.
- **Hooks:** Starting, child `Despawn`.
- **Reuse:** `data + stock asset` - barracks/nest; any Mob `Getload` can find.
- **Quirks:** respawns use the LAST assigned tile (shared `New`), not the original; respawn triggers on any despawn;
  NRE at entity level.
- **Cite:** `Entity_Ai_General_Spawnmobs.cs:14-47`, `:49-70`.

### `Entity_Ai_General_Upgrade` (89 lines)
- **Does:** Upgrade menu: creates commands `Upgrade` and `Upgradeoption1-4` (stock `Command_Upgrade*`, Goalvalue 0-3).
  On `Upgrade` with Json `{Upgrade, Upgrader}`: from a client the upgrade must be in the entity's `Upgrades` or in an
  applied upgrade's `Upgrades` whose `Priority` equals the current Level (0.05 s throttle); charges `Costs` x 1 from the
  Upgrader's wallet, applies it, vfx `Upgradeborder`, counts `Upgraded`.
- **Attached by:** entity (towers).
- **Reads:** Json; upgrade `Costs`, `Priority`, `Upgrades`.
- **Hard-coded:** Commands `Command_Upgrade`, `Command_Upgradeoption1`-`4`; vfx `Upgradeborder`.
- **Hooks:** Spawn, Command.
- **Reuse:** `data + stock asset` - upgrade trees for mod towers/objects.
- **Cite:** `Entity_Ai_General_Upgrade.cs:20-37`, `:39-87`.

---

## Resources / ore (5)

### `Entity_Ai_Resource_Death` (79 lines)
- **Does:** Breakable ore: applies damage itself at priority 10 and dies at 0; on death gives every active player
  (split) the current value of each `<Colour>auramine` stat.
- **Attached by:** entity (ore).
- **Reads:** stats `Redauramine`, `Blueauramine`, `Yellowauramine` (optional), health layers.
- **Hard-coded:** aura names; vfx `World_Redaura`, `World_Blueaura`, `World_Yellowaura`; vis `Hurt`.
- **Hooks:** Setpriority (10), Oninteract, Despawn.
- **Reuse:** `data-only` - one-shot crates/wrecks paying aura.
- **Quirks:** NRE if a Hurt has no `From`; any death-despawn pays (e.g. a sweep).
- **Cite:** `Entity_Ai_Resource_Death.cs:15-28`, `:30-77`.

### `Entity_Ai_Resource_Farmland` (151 lines)
- **Does:** Crop plot: stat `Percent` grows by `Heal` every 0.2 s; at 100 spawns one fruit (default `Resource_Crop_Blue`,
  team 0) whose three `*auramine` stats = `Yield`; regrows after the fruit dies. Named upgrades switch the crop colour and
  add `Yield_Add`/`Heal_Add`.
- **Attached by:** entity `Resource_Farmland_*`.
- **Reads:** stats `Heal`, `Percent`, `Yield` required; upgrade params `Yield_Add`, `Heal_Add` (required for the named upgrades).
- **Hard-coded:** Mobs `Resource_Crop_Blue/Red/Yellow`; upgrades `Resource_Farmland_Blue2/Red2/Yellow2/Bundle3_Upgradeproperties`.
- **Hooks:** Spawn, Slowtick, Upgrade, Downgrade.
- **Reuse:** `data + stock asset` - timed regrowing pickup (crop names fixed).
- **Quirks:** Bundle3 Downgrade removes `Yieldmod3` twice and never `Healmod3`.
- **Cite:** `Entity_Ai_Resource_Farmland.cs:32-75`, `:77-150`.

### `Entity_Ai_Resource_Farmstead` (59 lines)
- **Does:** Build hub for farmland: commands `Buildoption1-3` (stock `Command_Buildoption1-3`) with Goalstrings
  `Resource_Farmland_Red/Blue/Yellow`; Goalvalue 0, set to 1 on `Idle` when a child object named `Farmlandred`/
  `Farmlandblue`/`Farmlandyellow` exists; gives subowners 0-9 (all players).
- **Attached by:** entity.
- **Reads:** children's GameObject names.
- **Hard-coded:** the command, entity and child names above.
- **Hooks:** Spawn, Action.
- **Reuse:** `data + stock asset`; Goalvalue looks like a cost multiplier (as in `Entity_Ai_Hero_Buildsell.cs:73-79`),
  but how the farmstead's own options are consumed is unclear.
- **Cite:** `Entity_Ai_Resource_Farmstead.cs:11-58`.

### `Entity_Ai_Resource_Mine` (135 lines)
- **Does:** Endless ore: applies damage itself at priority 10 (`Healthresource` takes <= 1 + `Damageresource` per hit);
  when depleted, reheals to full, gives every active player (split) its `*auramine` values, plays emitters and
  `World_Rockbreak`, and fires command `Mine`. Appends Level to the skin at start; handles Heal.
- **Attached by:** entity (ore).
- **Reads:** stats `Redauramine`/`Blueauramine`/`Yellowauramine` (optional, watched for change), `Healthresource`.
- **Hard-coded:** aura names; vfx `World_Redaura`/`World_Blueaura`/`World_Yellowaura`, `World_Rockbreak`; command `Mine`.
- **Hooks:** Setpriority (10), Spawn, Starting, Oninteract, `Link_Stats.Changed`.
- **Reuse:** `data-only` - mod ore nodes with any aura mix.
- **Quirks:** NRE if a Hurt has no `From`.
- **Cite:** `Entity_Ai_Resource_Mine.cs:19-27`, `:29-91`, `:93-134`.

### `Entity_Ai_Resource_Opal` (196 lines)
- **Does:** Endless ore like `Resource_Mine` whose colour cycles each Tide (Base mods on the `*auramine` stats): red
  (`Regular`), blue (`Regular`), all three (`All`), yellow (`Regular`), repeating; first state after start is blue.
- **Attached by:** entity (ore).
- **Reads:** stats `Regular`, `All`, `Healthresource` required; `*auramine` stats (needed to pay); `Type` (marker).
- **Hard-coded:** vfx `World_Opalring_Red/Blue/All/Yellow`, aura vfx, `World_Rockbreak`; command `Mine`.
- **Hooks:** Setpriority (10), Spawn, Starting, Oninteract, Despawn, `Wave.OnValueChanged`, `Link_Stats.Changed`.
- **Reuse:** `data-only` - rotating-reward node.
- **Cite:** `Entity_Ai_Resource_Opal.cs:43-73`, `:75-116`, `:118-146`, `:148-189`.

---

## Other (5)

### `Entity_Ai_Modresource` (96 lines)
- **Does:** Ore modifier: for each colour, `<C>_Base` creates the `<C>auramine` stat (or adds that much if it exists);
  `<C>_Add` adds an Add mod to an existing stat. Colours: Blue, Red, Yellow.
- **Attached by:** upgrade (does nothing at entity level).
- **Reads:** all optional: `Blue_Base`, `Blue_Add`, `Red_Base`, `Red_Add`, `Yellow_Base`, `Yellow_Add`.
- **Hard-coded:** stat names `Blueauramine`, `Redauramine`, `Yellowauramine`.
- **Hooks:** Starting.
- **Reuse:** `data-only` - enrich any ore (or make anything pay aura with `Resource_Death`/`Mine`).
- **Quirks:** builds a `Resources` list that is never used; mods are never removed on Downgrade.
- **Cite:** `Entity_Ai_Modresource.cs:8-95`.

### `Entity_Ai_Blastheadore` (134 lines)
- **Does:** Growing ore: each Tide spawns `Resource_Branchrock_<Colour>` (by tag `Redmine`/`Bluemine`/`Yellowmine`) on
  every adjacent tower-placeable Surface tile, 0.5 s apart, then grows Add mods on `Healthresource` by stat
  `Healthgrowth` and on its aura stat by `Growth`. On hit-object change, links to neighbouring mine entities through
  `Post_<NW..W>` stats and `Walls` updates (visual walls).
- **Attached by:** entity (ore) tagged with one colour.
- **Reads:** stats `Healthgrowth` (optional gate), `Growth` (required with it), `Post_*` (optional).
- **Hard-coded:** Mobs `General_Tower`, `Resource_Branchrock_Red/Blue/Yellow`; tags `Redmine`, `Bluemine`, `Yellowmine`, `Surface`.
- **Hooks:** Hitchanged, Command (no-op), Spawn, Starting, Despawn, `Wave.OnValueChanged`.
- **Reuse:** `data + stock asset`.
- **Quirks:** with no colour tag `Auramod` is null (NRE when `Healthgrowth` exists).
- **Cite:** `Entity_Ai_Blastheadore.cs:20-51`, `:58-97`, `:99-127`.

### `Entity_Ai_Oroboro` (169 lines)
- **Does:** Shifting map: when Tide reaches stat `Path2`, moves every `Core` to the team -1 `Tp` marker tile (adding
  its neighbours' connections) and every `Enemycore` to the team -1 `Tpe` marker, kills (death) every `Hero`,
  `Herotower` and `Enemy`, warns "THE LAND SHIFTS BENEATH YOU"; at `Path3` does the same with the team 0 markers.
- **Attached by:** entity (map object); map needs `Tp`/`Tpe` markers of teams -1 and 0.
- **Reads:** stats `Path2`, `Path3`.
- **Hard-coded:** tags `Tp`, `Tpe`, `Core`, `Enemycore`, `Hero`, `Herotower`, `Enemy`; warning key `Map_Loop`; vfx `Map_Auraborotint`.
- **Hooks:** Spawn, Despawn, `Wave.OnValueChanged`.
- **Reuse:** `data-only` (map-bound) - relocate base and spawners mid-run.
- **Quirks:** heroes are killed, not moved (they come back through core respawn); NRE if a marker is missing.
- **Cite:** `Entity_Ai_Oroboro.cs:10-81`, `:82-155`.

### `Entity_Ai_Block_Surface` (59 lines)
- **Does:** Ground tile: clones its `Collider` child towards each blocked neighbour connection (`Navblock`/`Navblockteam`
  > 0, within 1.05, not above) and orients the main collider; answers client pings with that layout
  (`Pongload_ClientRpc("Block_Surface")`).
- **Attached by:** entity (Surface tiles).
- **Reads:** grid connections; prefab child `Collider` under `Currenthit`.
- **Hard-coded:** child name `Collider`; pong key `Block_Surface`.
- **Hooks:** Spawn, `Pingpongserver`.
- **Reuse:** `data-only` for mod ground tiles whose prefab has a `Collider` child.
- **Cite:** `Entity_Ai_Block_Surface.cs:20-57`.

### `Entity_Ai_Entposition_Position` (18 lines)
- **Does:** Keeps `Entityposition` equal to `Position` (copies on every change).
- **Attached by:** entity. **Reads:** nothing. **Hard-coded:** none.
- **Hooks:** Starting, `Position.OnValueChanged`.
- **Reuse:** `data-only` - for static objects moved by grid position.
- **Cite:** `Entity_Ai_Entposition_Position.cs:7-17`.

---

## Most reusable for a mod

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
