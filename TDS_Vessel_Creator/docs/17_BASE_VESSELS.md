# 17 · The five base vessels inside, and how run upgrades reach every vessel

What each of the game's five vessels (Smacky, Sno Sno, Boutman, Fie, Buster) really does in code, and what that means
for the Rogue Waves pack: a **Direction** or a **Trinket** (one player's Cardinal; a Vessel Cardinal until 2026-11-04) must work on all of them and on the pack's own
vessels, and a new vessel may borrow their assets. Read on 2026-10-12 (Dimitri: "get a clear understanding of each
character's mechanics so modded assets and characters integrate smoothly"). Everything is **read from the game's code**
(`Assembly-CSharp.dll` of 2026-10-08, decompiled as in `13` §11; `File.cs:line` points into that folder, never
committed) or **data** (`reference/vessel_data_survey.txt`), unless it says **guess**. RogueLab round 7 (2026-10-12)
tried §12's list in game; lines marked **verified** were seen there (`docs/reports/BASE_VESSEL_CHECKS_2026-10-12.md` in the
Rogue Waves repository, every line in `reference/roguelab_round7.txt`).

Read `01` §4-9, `reference/VESSEL_SURVEY.md` (each vessel's data and the conventions they share) and `12` §1-2 first.
This chapter adds behaviour, not data: the survey keeps the stats, costs and upgrade params.

## 1. At a glance

| | Smacky | Sno Sno | Boutman | Fie | Buster |
| --- | --- | --- | --- | --- | --- |
| Attack | melee (`Mob_Combat`) | ranged (`Mob_Ranged`, homing `Hero_Snosno_Snoball`) | melee (`Mob_Melee` + his own targeting); ranged after Shao-min (uses `Fishingrange`) | melee (`Mob_Combat`) | melee (`Mob_Combat`); damage = 1.5 + `Speed`×`Speeddamage` |
| Ability | none | none | recruit fish (Fishfollow), fish KO'd fish out of barrels, Rally (capstone) | none | **Dash** (`Command_Movelinear`, keys 6/7, 0.5 s anti-spam, no cost, no range limit) |
| Special rule in code | none | Snoborne: `Truemove` drops Level-20 snow on the tile he leaves; ×1.75 speed on thick snow (Entity_Ai_Hero_Snosno.cs:56-82) | The Bout Way: builds only the Bouthouse; Boutbro builds the rest at a rising price; can't mine until Fishbender | none | Good Dog: no code of its own; `Entity_Ai_Mob_Movementlinear` + `Entity_Ai_Mob_Dog` |
| Buildables | Archer, Bomber, Mining Hut, Wall | Sno Cannon, Sno Box, Heater, Ice Brick | Bouthouse (him); Fish, Barrel, Boutboat (Boutbro) | Knight's Keep, Rest Stop, Wall, Silo Pipes | Pedestal, Garage, Speed Pad (trap), Checkpoint (trap) |
| Units | miners → engineers (Hut) | Snobles (decay), Snoble Mother | Boutbro, fish, Buppy, Boutdad, Boutmom, ghosts | knights (+ drones) | dogs (one lap each) |
| His own upgrade tree | none | none | on the Bouthouse, forwarded to him | none | none |
| Income that pays **only his player** | Ore Scraps, Convert | Rich Sno | Farmingbed, Baviar (effect `Caviarbuff`) | Circular Routing, Hyper Pipes | Checkpoint attack aura |
| Death | normal respawn | normal | normal; after Enter the Boutman: KO, never dies | normal | normal |

Build-button order in code differs from the data's tower list for Smacky (wall 3, hut 4) and Sno Sno (Heater 3, Ice
Brick 4) (Entity_Ai_Hero_Smacky.cs:14-17; Entity_Ai_Hero_Snosno.cs:21-24). **Fish are built by Boutbro**, not by
Boutman, and the first fish and the first barrel are free (§5.1): this corrects `12` §1.

## 2. The shared layer: rules every vessel obeys

### 2.1 How stats combine

`Helper_Entitytools.Addmod` (Helper_Entitytools.cs:611-707) adds one `Link_Modifier` and re-computes
(:655-666):

```
Value_Calc = (ΣBase × ΣPmulti + ΣAdd) × ΣMulti × (1 − clamp01(OverrideOn)) + ΣOverride
  ΣBase = data value (or Initstats) + Leveladd × (Level − 1) + Base mods
  ΣPmulti = 1 + Pmulti mods     ΣMulti = 1 + Multi mods     (each sum adds; mods do not compound)
```

- **Multi and Pmulti are summed percentages.** Two `Multi +0.5` give ×2, not ×2.25. A Direction's `Multi +0.25` is ×1.25
  on a fresh archer but about ×1.1 on one already at Multi 2.5 (Fire Arrows + Auto Bow + Bow Squad). Pmulti scales only
  the base (and `Leveladd`), Multi scales everything; a Pmulti and a Multi do compound with each other. **Verified**:
  archer Damage 4 went to 8 with two Multi +0.5, to 8 with two Pmulti +0.5, to 9 with one of each.
- **An upgrade's params apply nothing by themselves.** Each stock AI reads the `Defaultparams` of its own upgrades by
  name (`Entity_Ai_Tower_Smackyarcher` reads `Range_Add` off Long Bow); no engine code turns params into mods.
  **Verified**: a −3 upgrade carrying `Range_Add 0.5` left an archer, a Sno Cannon and Boutman at the same Range. A
  Direction's code calls `Addmod` itself.
- **`Addmod` on a stat the entity does not have yet is listed but never applied**, and a later `Addstat` ignores it
  (:632-651, 709-778). Stats that appear mid-match: the Wall turret's Damage/Attackspeed/Range (Fie, Living Wall), the
  Silo's Range (Front-Line Pipes), the Ice Brick's Damage (Tip of the Iceberg), the Speed Pad's Damage (Speed Bump),
  Smacky's wall Damage (Spikes), every counter. `Removestat` on their downgrade drops outside mods with them.
- **No base vessel resets a stat.** Every vessel AI removes only its own `Link_Modifier` by handle; an outside mod
  survives their upgrades, downgrades and level changes (all five readers agree). **Verified** on an archer: Range 7
  with an outside +1, 9.5 with Long Bow, 7 again after `Removeupgrade`, the mod listed throughout.
- **Health is `Variable`** on vessels and the core (every other hero stat, and the core's `Respawn`, is `Fixed`): a
  change of max Health keeps the health fraction. **Verified**: 50/50 +20 gave 70/70, 30/50 +20 gave 42/70. A Health
  Cardinal or Direction refills nothing on a hurt vessel; heal it separately if the design wants that.
- An Override mod replaces the stat; live overrides sum; `Value_Override = 0` switches it off, so stuns use 0.01.
  `Editstat` adds `Value_Override_On` again on **every** change of an Override mod and subtracts it once on removal
  (:665, 703): never edit an Override mod, remove and re-add it (`09` #81).
- Modifiers carry no source (`Link_Modifier.From` is never set). Keep the reference `Addmod` returns and call
  `Remove()`. A mod on a missing stat is a free **marker** (`Shopbuff`, `Protectbuff`): test with
  `Modifiers.ContainsKey`.

### 2.2 Level and upgrade priority

`Helper_Entitytools.Upgrade` sets `Level` = the highest `Priority` ≥ 0 among applied upgrades (floored by
`Option_Defaultlevel`) and makes that upgrade the `Mainupgrade`, whose next options fill the building's upgrade panel
and whose `Ent_Visual` replaces its model (Helper_Entitytools.cs:494, 526-540, 562-564). **An effect upgrade with
Priority ≥ 0 hijacks the panel, the model and every `Leveladd` stat.** Stock effects use −1/−2, the tooltip −3, and
`ModKitDirections.Make` −3: keep it that way. `Removeupgrade` recomputes neither (:567-609). **Verified**: a −3
upgrade left Level and main upgrade alone on an archer at Level 2, a Sno Cannon and the Bouthouse; a Priority 0 one
became a fresh archer's main upgrade and stayed it after `Removeupgrade`. The Bouthouse sets its `Skin` to its Level
on any upgrade (`'1'` has no override, so its model stays).

Sell refunds the costs of **every** applied upgrade (Entity_Ai_Hero_Buildsell.cs:242-253), so a Direction or Cardinal
upgrade must have empty `Costs`.

### 2.3 Timing: when a hook can touch stats

| Moment | Stats exist? | Fit for |
| --- | --- | --- |
| `Game_Entity.Onspawn` (inside `Spawn`, Game_Entity.cs:294) | **no**: `Entity_Master.Start` builds them later (Entity_Master.cs:347-420) | adding upgrades to `Initupgradelistserver` (the stock Cardinals' way), attaching a brain, subscribing `Loaded`/`Despawn` |
| `master.Loaded` (in `Executestart`, after Preupgrades) | yes | `Addmod`, `Addstat`, commands |
| `parent.Child(master)` (end of `Executestart`) | yes | "a building or unit of mine appeared" |
| `Game_Entity.Ondespawn(m, killed, reason)` | yes, `Lasthit` still set | kills, sells (`"Sell"`), deaths |
| an upgrade AI's `Interact` / `Oninteract` / `Kill` | — | per hit dealt or taken, per kill; survives `Updateinteract` because it is in `Ais` |

The kit's brains already wait for `Statsserver.Count > 0` (`05` §6). Two engine sweeps remove outside hooks:
`Helper_Entitytools.Upgrade`/`Removeupgrade` **destroy every `Entity_Ai_Base` whose type is not listed** by the entity
or its upgrades (:540-547, 600-607), and `Updateinteract` rebuilds the `Interact`/`Oninteract` chains from `Ais`
(:138-151). Both run each time anything gives the entity an upgrade, and the base vessels do that constantly: Buster's
pads and checkpoints, Sno Sno's Shock/Scorch on foes and Sweat on allied towers, Fie's Rest Stop side dishes and gun
pickups. A delegate a mod adds to `Master.Interact` directly is gone after the next one (`05` §9).

### 2.4 What a death keeps

| | Modifiers | Upgrades | Source |
| --- | --- | --- | --- |
| The vessel dies and respawns | **lost** (new entity) | **kept**: the core copies `Upgradelistserver` into the new vessel's `Preupgrades`; their AIs run `Spawn` + `Starting` again | Entity_Ai_Main_Core.cs:138-150 |
| A unit respawns from its building | lost | the building's `Preupgrades` template only | `12` §2 |
| A leftover rebuilds a tower (Perma-Wall, Thin Ice) | lost | only upgrades with Priority **> 0**: a −3 Direction is dropped | Entity_Ai_Tower_Smackywall.cs:128-152; Entity_Ai_Tower_Snosnowall.cs:355-361 |
| Detonate's fresh bomber, Gutter's fresh fish | lost | none | Entity_Ai_Tower_Smackybomber.cs:279; Entity_Ai_Mob_Boutmanfish.cs:870-878 |

So an effect that must outlive a death is **an upgrade on the entity**, and anything per-entity is re-applied at
`Onspawn` for every respawn, rebuild and replacement. **Verified**: a respawned Boutman kept a −3 upgrade.

**Respawn time** is the core's `Respawn` stat (10, Fixed), read by the core's own handler on the vessel's `Despawn`
the moment it dies. **Verified**: 10.0 s plain, 5.0 s with `Respawn −50 %` put on the core before the death, 10.0 s
when the same cut was made in the vessel's `Despawn` handler or in `Game_Entity.Ondespawn` (both run after the core's).
The core is the team's, so a faster respawn for one player has to be on the core before that death and speeds everyone
dying meanwhile (**guess**: a handler subscribed in the vessel's `Onspawn`, before the core's, would run first).

### 2.5 Owner, parent, tags: finding a player's things

- Everything built goes through `Hero_Buildsell`: `Parent` = the builder, `Owner`/`Team` = the builder's
  (Entity_Ai_Hero_Buildsell.cs:179-202; Helper_Entitytools.cs:78-94). Units take their building's owner. **`Owner`
  (`Entitydata.Value.Owner`, the session id) is the only reliable key**: `Parent` dies with the vessel (built towers
  keep a dead parent), Boutman's fish have Boutbro as parent, Boutdad has the first boat, `Subowner` is often everyone.
- The main vessel of a player: in `Entitiesserver["Hero"]` with that owner and a parent tagged `Core`. Boutbro and
  Blurry Photo clones are also tagged `Hero` (clones carry `Clone` + `Prespawn`); the enemy Boutman's allied copy on
  Artrio/Manafold is a `Hero_Boutman` on team 1 tagged `Prespawn` with no player (Entity_Ai_Enemy_Boutman.cs:99-108).
- **Tag queries only see data tags.** `Entitiesserver[tag]` is filled once from `Entity.Tags` (Game_Entity.cs:319-343);
  a tag added at run time never makes an entity findable, removing one does hide it.
- Base-vessel buff buildings filter by **team**, never owner: Rest Stop (same-team `Mob`), Engineers and Heater's Sweat
  (same-team `Towerfighter`), Lifeline (same-team `Mob`), Speed Pad (same-team `Mob`), Checkpoint collect (same-team
  `Mob`). In co-op they already help other players' and modded things that carry those data tags.

| Kind | Data tags on the base vessels |
| --- | --- |
| Vessel | `Mob, Hero` (Buster adds `Dog, Buster`) |
| Attack building | `Tower, Towerfighter` (+ family: `Boutboat`, `Fiewall`, `Totem`) |
| Economy | `Tower, Gatherer` (Hut, Silo `+Silo`, Sno Box); Checkpoint `Gatherer, Trap, Checkpoint` |
| Other building | `Tower` (+ `Smackywall`, `Boutbarrel`): Ice Brick, Keep, Rest Stop |
| Trap | `Trap` (+ `Pad`, `Checkpoint`); no Health stat, so nothing can hurt it |
| Units | `Mob, Mobfighter` (+ `Fish`, `Knight`, `Dog`, `Snoble`); miner `Mob, Gatherer`; engineer `Mob` only |
| Odd ones | Bouthouse `Bouthouse, Herotower` (**no `Tower`**); Boutdad, Boutmom `Mobfighter, Boutfamily` (**no `Mob`**, no stats); Fie's Wall drops `Towerfighter` at spawn until Living Wall; Fie's drones have no tags |

### 2.6 Kill credit and income

- `Interacthurt` sets `To.Lasthit = From` **only when the hit data has no `Damage`** (Helper_Entitytools.cs:166-171).
  At a death `Lasthit.Kill(dead)` fires and `Lasthit`'s owner gets the kill (Game_Entity.cs:376-391). Every hit that
  carries an explicit `Damage` is uncredited (table in §8).
- **Enemy drops and ore pay every player** (`Giveresources(..., −1, Split)`, Game_Entity.cs:413-416;
  Entity_Ai_Resource_Mine.cs:136), and ore never records who broke it. A hit removes at most `1 + Damageresource` ore
  health (Helper_Damagetools.cs:47-58), so mining speed is attack rate, not Damage. The only payments to one player are
  the vessel-specific ones in §1's table, sell refunds and a few Cardinals.
- `Helper_Resources.Giveresources(list, ownerId)` pays one player × `Resourcegainmultiplier`; `−1` with `Default` pays
  everyone in full, with `Split` divides (Helper_Resources.cs:48-71). `Resourcechanged("Add")` carries no amount.

### 2.7 Costs

- Build price scale = the vessel's `BuildoptionN.Goalvalue` × `Resourcelossmultiplier` (Buildsell.cs:76-89; `16` §5). It
  is also the building's `Sellvalue`, so a discount also cuts its refund. **Verified**: a `Buildoption` at 0.8 built a
  Red 25 archer for 20 with `Sellvalue` 0.8.
- Upgrade price scale = the building's own `Upgrade` command `Goalvalue` (Entity_Ai_General_Upgrade.cs:31, 73-75);
  Voice in a Bottle lowers it. **Verified**: at 0.8 Long Bow (Blue 25) cost 20. Voice removes itself from a building
  without `Entity_Ai_General_Upgrade` (Entity_Ai_Effect_Artifact_Voice.cs:49, 68): the Mummer template's buildings
  (which mirror the flow) lack it; the pack's buildings carry it since 2026-11-04 (`05` §14), so Voice works on them
  (not yet seen in game).
- Boutbro rewrites his own `Goalvalue`s on every spawn, child and despawn (§5.1): write his cost stats instead.

## 3. Smacky (Good Ol' Fella, Basic)

**Hero.** Stats Health 50, Speed 3, Damage 4, Range 1.5, Attackspeed 0.5. `Entity_Ai_Hero_Smacky` only makes the four
build buttons and re-runs melee targeting (Entity_Ai_Hero_Smacky.cs:8-44). No tooltip, no ability, no Attack
animation. Sprite `Hero_Smacky_Sprite`: Idle 4×6, Move 4×6 (footstep on frames 2 and 5), Falling 1×12, Land 1×12,
Landquick 4×1. The tutorial forces Smacky.

| Building | What it does | Read live (an outside `Addmod` works) | Fixed (stat mods do nothing) |
| --- | --- | --- | --- |
| Archer (Red 25, `Tower, Towerfighter`) | tracking arrows; Fire splits a hit into 5 burn ticks; Assassin adds HP/35 (≤100); Death Mark executes ≤15 % and +0.25 Damage per kill | Damage (at impact), Attackspeed, Range, Projspeed | burn split, Boss and Death Mark numbers, projectile name |
| Bomber (Red 25 Blue 25, `Tower, Towerfighter`) | lobbed blasts hitting at most `Projcount` foes; split/bounce children; Cannons add a second gun; Detonate (death) hurts **every** foe on the map for 1000 and spawns a fresh, un-upgraded bomber | Damage, Attackspeed, Range, Projduration, **Projcount** | Detonate 1000, slow, split/bounce |
| Mining Hut (Yellow 35, `Tower, Gatherer`) | spawns miners (one ore hit per round trip) or engineers (Support Team) | Respawn, Health | **`Units`**: only `Addressminercount` (on a downgrade) reads it; Starting spawns exactly one (Entity_Ai_Tower_Smackyhut.cs:70-132) |
| Wall (Blue 20, `Tower, Smackywall`) | posts link to `Smackywall` neighbours; Spikes reflect; Bramble and Beacon tile auras; Perma-Wall rebuilds | Health, Damage once Spikes made it | regen 7.5/s, Rejuvenate `100 × Level`, Bramble 15, Beacon 750/÷1.5 |

- **Engineers** walk to the same-team `Towerfighter` of **any** player nearest their **Hut** (sorted from
  `Master.Parent`, no range limit) that lacks their buff and give it `Effect_Smackyhut_Buff` (Range +25 %,
  Attackspeed +50 %, Damage +25 % Pmulti, 8 s) or, when their `Effect` stat is not exactly 1, `…Buff2` (×3 attack
  speed, 14 s) (Entity_Ai_Mob_Smackyengineer.cs:42, 69, 95-162). **Any outside change to an engineer's `Effect`
  switches it to the Firm buff.**
- Ore Scraps (`Mob_Smackyminer_Chips`) pays the **unit's owner** 1 of each colour the hit ore yields, per ore hit;
  Convert trades −0.5/−0.5 for +1 on a gain of the owner's, at most once a second (`Convertcooldown`)
  (Entity_Ai_Effect_Smackyhut_Scrap.cs:8-40; Entity_Ai_Tower_Smackyhut.cs:196-208, 258-364).
- Kill credit: hero, miners, all projectiles incl. explosions and split children, Spikes, the Spikes trap. **Not**:
  Detonate, Bramble, Marketplace, burn ticks (the first arrow already credited).
- Bugs: Bomb Launcher's downgrade removes Quick Bombs' mod; Convert and Marketplace keep running after their
  downgrade (it clears the flag before testing it to unsubscribe, Entity_Ai_Tower_Smackyhut.cs:526-544; the hut's
  despawn does unsubscribe them, :168-195); units removed by lowering `Units` come back; live miners lag one level;
  Rejuvenate heals for the previous tier; a drowned wall leaves its Bramble and Beacon tiles buffing; engineers whose
  hut died throw every tick (no null check on `Parent`).
- Borrow: `Entity_Ai_Mob_Smackyminer` (generic miner, already Sprigg's), `Entity_Ai_Tower_Tilebuff` (a tile aura that
  force-applies the being's first upgrade to same-team towers on its tile), `Projectile_Ai_Splitbounce`, the archer
  arrows (the kit's default projectile clones, `01` §9), `Effect_Smackyhut_Buff/Buff2` (timed tower buff, removes
  itself), `Effect_Smackywall_Beacon` (÷1.5 damage at priority 9 + Health per Tide).

## 4. Sno Sno (Destroyer of Worlds, Intermediate)

**Hero.** Health 25, Speed 2, Damage 3, Range 3, Attackspeed 0.5, Projspeed 15. Ranged through the stock
`Entity_Ai_Mob_Ranged`: `Starting` sets `Attack.Goalstring = Hero_Snosno_Snoball` and adds `Hurtdict["Snoball"]`
(Entity_Ai_Hero_Snosno.cs:47-54). `Hurtdict` is public and copied into each shot, so any key a mod adds reaches every
target (`Damageresource` makes him mine faster). Snoborne: on each tile change (`Truemove`, Game_Grid.cs:1077) he
leaves Level-20 snow on an empty tile, refills thin snow to 20, and gets a Speed Pmulti +0.75 on snow above 20.
Animations: Idle 4×6, Move 4×6, Falling 2×6, Land 1×12, **Attack 4×4** (played on "Shoot"), Landquick 4×1.

| Building | What it does | Read live | Fixed |
| --- | --- | --- | --- |
| Sno Cannon (Red 30, `Tower, Towerfighter`) | eats the nearest **`Snoble`** within `Rangesno` whole, or else `Ammo` Level from the nearest **`Sno`-tagged entity** (both **of any team**), then fires; Snoblaster laser drains snow to 5000 and fires a ×6 beam | Damage, Attackspeed, Range, Rangesno, Ammo, Projspeed | splash, slow, Hypercool, Permasno, laser and Snoble numbers |
| Sno Box (Blue 10 Yellow 25, `Tower, Gatherer`) | hurts every ore in Range (no `Lasthit`) and makes snow (`Heal` Level), only when an ore is in Range, on its tile or, with Sno Fall, where it throws it (no range limit); all its hurts on `Mob`s ×20 (×50 Hard Sno) | Damage, Attackspeed, Range, Heal | ×20/×50, Snoble every 5th, Snoaddrate |
| Heater (Red 60, `Tower, Towerfighter`) | burns up to 5 foes in Range, melts snow; Cookin' Eggs pulse, Heat Treat heals allies, Cold Sweats gives every same-team `Towerfighter` in its Range +100 % attack speed | Damage, Attackspeed, Range, Snomeltspeed, Rangesno | pulse 20, heals 10/3.3, Shock +15 %, Scorch ramp |
| Ice Brick (Blue 15, `Tower`) | heals from the snow on its tile, then an overshield (`Healthover`) up to `Health × Overmax`; leftovers Thin Ice (rebuilds) and Rock | Health, Heal, Healtime, Overmax | ×0.85 resist, bursts 200/35, snow lob 20 |

- **Snow** (`Effect_Snosnosnow`, Entity name `Sno`, tag `Sno`, Level 0-100, skins 2-10) is a networked being any mod
  can make: spawn it on a `Surface` with Initstats `{Level}`, or send `Interact(from, snow, "Grow", {Amount, Spread})`
  (Projectile_Ai_Sno.cs:35-57). Snow does nothing to foes; Sno Sno's things read it. His snow is team −2: own-team
  filters and `Mob`/`Tower` tag queries never see it, but a team-only foe query (`Styles.Team`, `Teamfor = false`)
  does, since it leaves out only its own team and −1 (Helper_Entityquerytools.cs:18-29, 56-61); the cannon, laser,
  Mother and Heater take snow and Snobles of **any** player.
- **Never tag a mod entity `Sno` or `Snoble`.** The cannon despawns a `Sno`-tagged `Mob` as ammo, throws on a non-Mob
  without `Level`, and picks its shot by GameObject name (`Projectiles[name]`, Entity_Ai_Tower_Snosnocannon.cs:241-259):
  any other name is a `KeyNotFoundException`. **Verified**: a Dagger copy whose data tags add `Sno`, beside a cannon,
  was despawned in 0.17 s (`killed False`, reason `''`: `Shootsnow` eats a `Mob`); a stock Dagger on the same tile
  stayed. No exception showed.
- **Snobles** (`Mob, Sno, Snoble, Mobfighter`) are stationary melee blockers thrown by the Box (Snobles upgrade) and the
  cannon; no movement, no respawn, made all match long. Their health **drains faster and faster by direct stat edit**
  (Entity_Ai_Mob_Snoble.cs:57-69), bypassing damage reduction: "minions take less damage" must also cut `Drain` or raise
  Health. The Snoble Mother walks to the enemy core, eats snow to rank up and bursts for `Deathdamage`.
- Kill credit: hero, basic/Blower/Boulder shots, Heater beam, Snobles, Box blasts, icicles. **Not**: black and Snoble
  shots, laser, Eggs pulse, Scorch, Ice Brick bursts, the Mother's death blast.
- Income: only **Rich Sno** pays his player (1 per colour tag when a tile is used up,
  Entity_Ai_Effect_Snosnosnow_Rich.cs:8-41).
- Bugs: Take the Heat removes Scorch/Shock from the brick instead of the attacker (a scorched foe triggers the 200 burst
  on every hit); Sno Boulder sits in the tier-1 root list; the laser's Attackspeed zeroing (`Base = −Attackspeed`)
  fails once any Multi or Add mod is on the cannon, so a Direction on its attack speed lets it fire both ways;
  `Units_Base` (Snobles) and the Heater's `Projcount` are never read.
- Borrow: the snow being and its Grow interaction, `Hero_Snosno_Snoball` (homing shot for any ranged hero),
  `Tower_Snosnoheater_Pulse`/`_Healpulse` (expanding rings), `Tower_Snosnobox_Snolaunch` (lobbed shot that lands as
  snow), `Effect_Snosnowall_Breaker` (permanent −25 % damage, −15 % health and speed), `Effect_Snosnoheater_Sweat`
  (+100 % attack speed, 10 s), the Box's `Command_Tower_Snosnobox_Launch` (click-a-tile command).

## 5. Boutman (Fisher Extraordinaire, Advanced)

```
Hero_Boutman (Mob, Hero) ── builds only ──> Tower_Boutmanhouse (Bouthouse, Herotower; no Tower, no Health)
  ├ Tower_Boutmanhousepersistent (keeps the house's upgrades if it falls)
  └ Mob_Boutmanbro "Boutbro" (Mob, Hero, Boutfamily, Boutbro): builds and mines
      ├ Mob_Boutmanfish (Mob, Mobfighter, Fish, Fishmain) + its no-build tile
      ├ Tower_Boutmanbarrel (Tower, Boutbarrel) ── Buppy (Mob, Mobfighter, Fish)
      └ Tower_Boutmanboat (Tower, Towerfighter, Boutboat)
          └ Boutdad / Boutmom / ghosts (Mobfighter, Boutfamily; no Mob, no stats)
```

### 5.1 The vessel and the Bouthouse

- `Entity_Ai_Hero_Boutman` (priority 9) builds a state machine on `Master.State`: Idle → fish a `Pond` fish within
  `Fishingrange` that has `Live > 0` (waits `1/Fishingspeed`), else melee; `Ko` (Entity_Ai_Hero_Boutman.cs:500-567). His
  attack query has no `Resource` (can't mine) until Fishbender.
- **The Bouthouse is his upgrade panel.** Every upgrade on the house is copied onto the hero by `Askforupgrades`
  (house upgrades, hero respawn) and parked on the persistent object when the house dies
  (Entity_Ai_Tower_Boutmanhouse.cs:56-116). **Any upgrade put on the Bouthouse leaks onto Boutman**: never give it a
  building Direction as an upgrade. **Verified**: on Boutman 1.5 s after the house got it.
- After Enter the Boutman he **never dies**: at 0 HP he goes `Ko` (Health 1, untargetable) until healed to full; his AI
  then applies damage itself and zeroes `Damagemulti` for `Mob_General` (Entity_Ai_Hero_Boutman.cs:127-203). A
  damage-reduction AI must run at priority ≤ 8 to act on him. "On vessel death" effects must also watch the `Ko`
  command.
- **The Bout Way's price**: Boutbro sets each `BuildoptionN.Goalvalue` = fish `#Fishmain × Fishcost`, barrel
  `#Boutbarrel × Barrelcost`, boat `(#Boutboat + 1) × Boatcost`, recounted team-wide on spawn, child and despawn
  (Entity_Ai_Mob_Boutbro.cs:130, 167, 204). So fish 1 and barrel 1 cost 0, fish n costs (n−1) × Yellow 25. **Verified**:
  fish 1 and barrel 1 free, fish 2 cost 25. A discount Direction mods Boutbro's `Fishcost`/`Barrelcost`/`Boatcost`
  (re-applied on each Boutbro spawn), and the price follows at the **next recount**: **verified**, a `Fishcost −20 %`
  made after fish 2 left fish 3 at 50, then fish 4 cost 60 (3 × 0.8 × 25).
- Boutbro calls every core's `Child` at `Starting` (:74-83), so `Heroapply` Cardinals reach him; his `Hero` tag also
  puts him in the F-key list (`15` §9).
- The Shao-min ranged attack AI only fires on a GameObject named "Hero Boutman" (Entity_Ai_Hero_Boutman_Shoot.cs:75):
  **giving it to another vessel does nothing.** It aims within `Fishingrange`, not `Range`; the wave itself flies 2.4
  tiles (`Hero_Boutman_Range`: Zvelocity 12, Lifetime 0.2).

### 5.2 Fish, barrels, the boat

- A fish guards the tile it was built on (its `Block_Noplacementfish` keeps others off), fights within `Engagerange`,
  goes **KO** at 0 HP (`Dead` state, `Healtharmor` = `Deadarmor`) instead of dying, flies to a same-team barrel with a
  free slot, is healed by the barrel's live `Heal`, and is fished out by Boutman. The hit that empties the armour
  drowns it, unless a barrel has begun healing it. Fish handle their own hurt at priority 10: reduce at ≤ 9.
- Barrel capacity is `Vacantcurrent`; **`Units` is only displayed** (Entity_Ai_Tower_Boutmanbarrel.cs:361-368).
  Farmingbed pays the **fish's owner** 5 % of the fish's worth each time it leaves (:208-264).
- **The boat does not shoot: Boutdad does, with the boat's live `Damage`, `Attackspeed`, `Range`, `Projspeed`**
  (Entity_Ai_Mob_Boutmandad.cs:79-141). Buff the boat to buff Dad, Mom and the Battleship cannon. Dad and Mom are
  **one on the map at a time, whatever the team** (tag-only query, Entity_Ai_Tower_Boutmanboat.cs:80-101). Mom appears
  at 4 Raft boats but leaves below 5.
- Read live: hero Damage/Attackspeed/Range/Health/Fishingspeed/Fishingrange; fish Damage/Attackspeed/Range/Speed/
  Engagerange/Health; barrel Heal/Range/Speed/Vacantcurrent; boat everything. **Not**: the hero's `Heal` stat
  (overwritten by params before use), Iron Fish's own params (never read), Boutbro's cost stats outside a recount.
- Kill credit: hero melee and Shoot, fish and their shots, Buppy, Boutbro, the boat cannon. **Not**: Dad, Mom, ghost
  projectiles, the Family splash, Balibut, Shrimb, Pubberfish poison.
- Income: no aura building; Boutbro's mining pays everyone. His own: Farmingbed, Baviar (2 of each colour when a marked
  foe dies, to the **foe's** owner field, Effect_Boutmanfish_Caviarbuff.cs:28; who receives it in play is **guess**).
- Bugs: the ghost reads `Attackspeed` as seconds between shots; Watertower divides by zero with one free tile beside the
  barrel; Fishfollow and Boutbro's cost counts check team, not owner; Mine buffs use `Data.Add("Damageresource")`, so a
  mod that adds the key first makes them throw (write `Data[key] = …`); Tank's overshield stacks forever.
- Borrow: `Block_Noplacementfish` (no-build tile whose click selects its parent; already Rising Water's flood block,
  `16` §3), `Tower_Boutmanhousepersistent` (an invisible upgrade container), `Tower_Boutmanbarrel_Pulse`, the generic
  effect AIs `Entity_Ai_Effect_Boutmanspawnbed_Boost` (timed Damage Multi), `_Bucket_Boost` (timed immunity at
  priority 8), `_Boutmanfish_Puffer` (poison), `_Bass` (execute below a %), `_Caviarbuff` (pays on death), the unit
  picker UI `Ui_Command_Boutman_Fishfollow`.

## 6. Fie (From the Walls, Intermediate)

**Hero.** Health 50, Speed 3, Damage 3, Range 2, Attackspeed 0.5, Buildrange 3, Sellrate 0.6. No ability, no tooltip,
no Attack animation; `Entity_Ai_Hero_Fie` only makes the build buttons and re-runs melee targeting
(Entity_Ai_Hero_Fie.cs:8-44).

| Building | What it does | Read live | Fixed |
| --- | --- | --- | --- |
| Silo Pipes (Yellow 35, `Tower, Gatherer, Silo`) | pipes to the nearest ore (any range), mines it every `1/Attackspeed`, fills `Gun` by `Gunspeed`; hands a gun to a knight; Circular Routing pays the owner 5 % of each wallet entry per Tide (≤ 50), Hyper Pipes 1 of each per second for 600 s | Damage, Attackspeed, Gunspeed, Buildspeed, Range (after Front-Line Pipes) | Energized, Circular, Hyper amounts |
| Rest Stop (Yellow 50, `Tower`) | throws a buff at a same-team `Mob` in sight (towers too after Second Servings): Damage Multi + `Damagemulti × Effect` for `Duration`, plus the food's stat | Attackspeed, Range, Duration, **Effect**, Damagemulti | the food %, Perm_Count, tower ×5 |
| Knight's Keep (Red 60, `Tower`) | spawns unarmed knights that fetch a gun from the nearest same-team `Silo`-tagged entity and guard the rally point | Respawn | **`Units`** (only explicit `Add()` calls spawn, Entity_Ai_Tower_Fiekeep.cs:132-158) |
| The Wall (Blue 20, `Tower, Fiewall`) | grows by `Command_Build_Fiewall`; effects spread over connected `Fiewall`s of **any** owner (and adjacent `Tower`s of any team after Shared Strength); Living Wall turns it into a turret | Health; turret Damage/Attackspeed/Range after Living Wall | Explode 45, True Wall 1000, Health3/Shield3 amounts (applied once) |

- **Knights** (`Mob, Mobfighter, Knight`) gain their weapon as a forced upgrade `Mob_Fieknight_<Colour>_Upgradeobject`
  built from the silo's `Gun` link string (Entity_Ai_Mob_Fieknight.cs:103-155); the gun's stats are **Base** mods.
  A knight needs a parent with a `Rally` link, `Units` and `Children` (:91, 383, 508): only a Keep-like parent can
  spawn one. Retire and every respawn make a new entity (gun and outside mods lost).
- **A modded building can arm Fie's knights**: tag `Silo`, a `Gun` stat ≥ 1, a `Gun` link whose Goalstring is
  Red/Blue/Yellow/Green/Orange/Purple/All, and an `Oninteract "Gun"` that answers `Interact(self, knight, "Gun")`
  (Entity_Ai_Tower_Fiesilo.cs:101-116). An unknown colour makes `Upgrade(null)` throw.
- Rest Stop buffs reach **any** same-team `Mob` that reads the stat live, modded ones included; buffs from two shops
  don't stack (the `Shopbuff` marker). Bug: `Applyeffects` tests `Shopbuff` on the **shop**, so a Rest Stop fed by
  another's Second Servings stops buffing (Entity_Ai_Tower_Fieshop.cs:92). **Never put a `Shopbuff` marker on a shop.**
- Kill credit: Fie, knight bullets (a drone's kills go to the drone), the wall turret. **Not**: Sword slash, wall
  explosion, True Wall, Calamari spikes.
- Other bugs: Side of Sushi's crit never fires; the Yellow commander amp falls as Commander rises; Hyper Pipes puts its
  `Attackspeed_Multi` param on Damage; popped knights respawn; Front-Line Pipes counts allied deaths; the host never
  checks wall-growth adjacency or range.
- Borrow: the Rest Stop buff projectiles `Tower_Fieshop_Buff…Buff5_Buffet` (tracking ally buff, interaction `"Buff"`),
  the knight guns (shotgun, pistol, rifle, grenade, SMG, sniper), `Tower_Fiewall_Shot` (beam),
  `Effect_Fiewall_Protection` (−75 % damage at priority 9), `Effect_Fiewall_Weak` (permanent −25 % speed and damage),
  `Effect_Tower_Fieshop_Ramen3` (3/s regen) and `_Calamari3` (thorns, both end with the `Shopbuff` marker),
  `Command_Tower_Fiekeep_Rally` (click a tile, units guard it), `Command_Tower_Fiesilo_Reroute` (pick an ore),
  `Command_Mob_Fieknight_Retire`.

## 7. Buster (A Very Fast Dog, Advanced)

**Hero.** Health 50, Speed 3.5, Damage 1.5, Range 1.5, Attackspeed 0.5, Speeddamage 1. AIs `Mob_Movementlinear`,
`Mob_Movement`, `Mob_Combat`, `Hero_Buildsell`, `Mob_Dog`, `Hero_Buster`, **no `Mob_General`**: `Entity_Ai_Mob_Dog` is
his damage handler (priority 10) and keeps a Damage **Base** mod = `Speed × Speeddamage` (Entity_Ai_Mob_Dog.cs:63, 183,
324), so **a Speed buff is also a damage buff**. Animations: Idle, Move, **Movelinear** 4×6 (loop), Falling, Land,
Landquick, Attack 4×2.

**The Dash**, a reusable hero ability: `Entity_Ai_Mob_Movementlinear.Spawn` creates `Command_Movelinear` (Button, keys
6/7, Onpressed, priority 6, Statechange). The client sends a unit direction snapped to 90°; the host runs tile by tile
along `Helper_Navtools.Getpathlinear` until nothing lies ahead (no range limit), one tile per `1/Speed` s
(Entity_Ai_Mob_Movementlinear.cs:9-152). `Mob_Dog` hits each foe it runs into with `Interacthurt` (credited); a
collision adds the negative `Collisionspeed` (Buster −0.1, a dog −1) to the speed pads gave it, floored at 0, so it
never cuts the runner's own Speed (Entity_Ai_Mob_Dog.cs:97-149; Entity_Ai_Effect_Busterpad_Speed.cs:34). A
server-side dash must set `Commandlistserver["Movelinear"].Goalposition` and `Updatecommand`, as the pads do; the AI
ignores server-issued commands.

| Building | What it does | Read live | Fixed |
| --- | --- | --- | --- |
| Pedestal (Red 25, `Tower, Towerfighter, Totem`) | tracking beam, Damage per beam tick; Blink chains through same-team `Totem`s; Pure Ex: beam targets deal no damage | Damage, Attackspeed, Range, Rangebreak | projectile name |
| Garage (Red 60, `Tower, Towerfighter`) | spawns dogs; mirrors each of its upgrades onto its dogs as the same-named `Mob_Busterdog_*` upgrade (tiers 2-3 also as stat mods and a skin) | Respawn, Health | **`Units`** (only `Addressdogcount` on a downgrade reads it; the `Units_Add` params are never read) |
| Speed Pad (Blue 25, trap, `Trap, Pad`) | same-team `Mob`s running with it get a decaying Speed Add; dash runners are redirected; Refuel lets a tower stand on it (+75 % attack speed) | Speed, Effect, Maxspeed, Dogspeed, Damage after Speed Bump | — |
| Checkpoint (Yellow 50, trap, `Gatherer, Trap, Checkpoint`) | charges by distance to its linked ore; any same-team `Mob` touching it collects and the checkpoint hurts the ore (pays everyone); Tune Up's aura: next hit ×3 and **5 of each colour to the collector's owner** | Attackspeed, Damageresource, Healthresource, Collectrange, Range | aura payout |

- **Dogs** (`Mob, Mobfighter, Dog`) run one straight lap and then **despawn themselves with `killed = true`, reason
  `"Dog"`** (Entity_Ai_Mob_Dog.cs:312-321); the garage spawns a fresh one after `Respawn`. Every dog buff must be
  re-applied on every spawn, and "when a minion dies" effects must skip reason `"Dog"`.
- Traps have no Health and no `Tower_General`: health Cardinals do nothing on them, and "all buildings" Directions must
  decide whether a Pad's Speed and a Checkpoint's Range/Attackspeed (ally speed, income) are meant.
- **Torrid Badge names Buster**: its merchant gives `Artifact_Badge_Buster_Upgradeproperties` to every same-team
  entity tagged `Buster` (Entity_Ai_Mob_Merchant.cs:41-43, 230-240). No other stock Cardinal names a vessel.
- Bugs: the Garage's Mechanic's Friend downgrade removes the wrong upgrade; the Checkpoint's `Collect` delegate returns
  a value, so only the last subscriber counts (Quick Pass is lost once a later collect upgrade is bought); Rev Dog makes
  dogs permanently untargetable; Boost Pad's Pmulti lands on `Dogspeed`; every pad, checkpoint and dog buff given to an
  ally rebuilds that ally's hook chains.
- Borrow: the Dash (`Entity_Ai_Mob_Movementlinear` + `Entity_Vis_Movementlinear`, a `Movelinear` animation, a `Speed`
  stat; add `Entity_Ai_Mob_Dog` only for ram damage and then drop `Mob_General`), `Mob_Busterdog` (a self-running ram
  minion), `Effect_Busterpad_Speed` (decaying speed), `Effect_Busterdog_Towerboost` (+50 % damage, +45 % range, 5 s),
  `Effect_Tower_Bustertotem_Blazedebuff` (ramping delayed damage), the beam projectiles.

## 8. What each common run upgrade needs, vessel by vessel

The matrix the pack's Directions and Trinkets are designed against. "Re-apply" = at every `Onspawn` of that
kind of entity with the player's owner, after its stats exist (§2.3).

| Effect | Smacky | Sno Sno | Boutman | Fie | Buster |
| --- | --- | --- | --- | --- | --- |
| Vessel Damage / Attackspeed / Range | live | live (Range and Projspeed for his shot) | live; his ranged reach is `Fishingrange` | live | live; **Speed also adds damage**; Range is −1 while dashing |
| Vessel heals on hit | upgrade AI `Interact` on the hero | same; hits carry `Snoball` | same; his splash hits come through `Interact` too, tag yours `Aoe` | same | same; melee and dash hits both count |
| Vessel takes less damage | AI priority < 10 | < 10 | **≤ 8** (his AI is 9) | < 10 | < 10 (`Mob_Dog` handles hurt at 10) |
| Towers' damage % | Archer, Bomber; Hut and Wall have no Damage (Wall after Spikes) | Cannon, Heater, Box (×20 on foes); Ice Brick only after Tip of the Iceberg | **Boat** (= Dad, Mom, cannon); Barrel and Bouthouse have none | Wall turret after Living Wall; Silo Damage = mining | Pedestal; Garage has none (buff dogs) |
| Towers' health | all four; re-apply on Perma-Wall and Detonate rebuilds | all four (also raises the Ice Brick's overshield cap); Thin Ice trap is `Trap` | Barrel, Boat; **Bouthouse has no Health and must never get an upgrade** | all four; walls grown mid-match have a wall as parent | Pedestal, Garage; traps have no Health |
| Minion stats | miners, engineers (never touch `Effect`) | Snobles, Mother (made all match) | Boutbro, fish, Buppy; Dad and Mom: buff the boat | knights (+ drones, no tags) | dogs (every lap) |
| Minions take less damage | AI < 10 | **Drain bypasses it**: also cut `Drain` | fish ≤ 9; Dad, Mom have no health | AI < 10, as an upgrade | AI < 10 |
| More units | `Units` alone does nothing: `SendMessage("Addressminercount")` on the hut after the mod (**verified**) | no unit count to raise | barrel `Vacantcurrent`, not `Units` | `Units` + `SendMessage("Addressmobcount")` on the keep (**verified**) | `Units` + `SendMessage("Addressdogcount")` on the garage (**verified**) |
| Units respawn faster | Hut `Respawn` | — | Bouthouse `Respawn` (Boutbro) | Keep `Respawn` | Garage `Respawn` |
| Build discount | `Buildoption` Goalvalue | same | Boutbro's `Fishcost`/`Barrelcost`/`Boatcost` (Goalvalues are rewritten) | same | same |
| Per-kill effect (credit by `Lasthit` owner) | misses Detonate, Bramble | misses black/Snoble shots, laser, pulses, Scorch, bursts | misses Dad, Mom, ghosts, poison | misses Sword, wall blasts | misses Speed Bump, Collision, Diesel flames |
| His player's aura income | Scraps, Convert; else pay directly | Rich Sno; else pay directly | Farmingbed; else pay directly | Circular, Hyper (cached: pay directly) | Checkpoint aura; else pay directly |

Mining always pays the team, so an **income Direction pays the player directly** (`Giveresources(list, owner)`) from
an event it can see: a Tide, a kill credited to the player, or the player's things hitting ore (an AI on each
`Resource` reading `Interact.From`, as Starter Seeds reaches every ore). Raising a building's mining stats raises
everyone's income, not the player's.

## 9. The stock Cardinals on any vessel

Every stock Cardinal reaches a vessel through its data tags, its parent or its build commands, never its name, with
one exception. Each row says what a **pack vessel** needs for the Cardinal to work on it.

| Cardinal (code) | Reaches by | A pack vessel needs |
| --- | --- | --- |
| Torrid Badge | tag **`Buster`** (Merchant.cs:41) | never the tag `Buster`; the generic part works anyway |
| Paper Tower | `Buildoption1..4` whose being is `Towerfighter`, read at the effect's `Starting` (Effect_Artifact_Paper.cs:15-63) | build commands named `Buildoption1..4`, made before then; attack buildings tagged `Towerfighter`; built through `Hero_Buildsell` |
| Bottom Rock | disables `Buildoption1..4` past its limit; the count is **team-wide** (Artifact_Bottomrock_Effect.cs:32, 56-64) | the same command names (any other `Build*` command escapes the limit) |
| Voice in a Bottle | `Entity_Ai_General_Upgrade` on buildings whose parent is tagged `Hero` | carry the stock upgrade AI (`05` §14; the pack's buildings since 2026-11-04); a building that mirrors the flow loses it (§2.7) |
| Diorama, Blurry Photo | set `Skin` = the being's name (`<name>2` for the clone) | a `Skinoverride` of that name for the look; never drive the vessel's own stages by `Skin`; the clone (tags `Clone`, `Prespawn`, parent a `Hero`) also fires the pack's `Onspawn` |
| Pig Named Bank, Voiceapply, Teamapply | the thing's **parent tagged `Hero`**, whoever owns it: Teamapply reached another player's building and kept reaching new ones after its upgrade was removed (verified, RogueLab round 8; pitfall #118) | build through `Hero_Buildsell`; a building's units (parent = building) are left out, as for the base vessels |
| Heroapply family (Sport Water, Fluffy Slippers, Diorama, Money Bag...) | data tag `Hero` and the core's `Child` | tag `Hero`; a minion that builds and calls the core's `Child` (Boutbro) gets them too |
| Towerapply (Rusty Pipe, Frail Titan) | data tag `Tower` | `Tower` on every building (traps and the Bouthouse are skipped) |
| Mobapply, Aura Gavel, Kippy, Checklist | data tag `Mob` | `Mob` on the vessel and every unit (Boutdad has none, so it is skipped) |
| Cubed Drink | `Mobfighter`/`Towerfighter`/`Hero` **with a Damage stat** | the tags and a `Damage` stat |
| Onetime Build, Build (Processor Jr, Starter Seeds, Cubed Drink, Sand Filled Box, Tacklebox) | put `Entity_Ai_Hero_Buildsell` and a build AI on the **core**, with a `Buildslot` param (Tacklebox's `Entity_Ai_Artifact_Build` has none: always `Buildoption4`) (verified, the PC dump) | — |
| Coins, clock, core, ore and foe Cardinals | no vessel | — |

`15` §18.1 has each Cardinal's pattern and the stock bugs. Each Cardinal's core upgrades, AIs and params were dumped
in game (`CARD` lines of `reference/roguelab_round7.txt`): every row above matches its AI (`Heroapply`, `Towerapply`,
`Coreapply`, `Voiceapply`, `General_Spawnmobs` for Torrid Badge, Bendigo, Fish Bait, Kippy). All use Priority −1
except Frozen Computer and Wave Breaker (1), which therefore set the core's Level and main upgrade.

## 10. Assets a new vessel can borrow

Load the owning vessel's group first (`09` #15). By need:

| Need | Base asset |
| --- | --- |
| A dash or charge | `Command_Movelinear` + `Entity_Ai_Mob_Movementlinear` + `Entity_Vis_Movementlinear` (Buster) |
| Ram damage while moving | `Entity_Ai_Mob_Dog` (needs `Speed, Speeddamage, Speeddamagestartup, Collisionspeed, Damage`; replaces `Mob_General`) |
| Click a tile for units to guard | `Command_Tower_Fiekeep_Rally` (a `Move` subclass; answer `Relocatedata`) |
| Click a tile for a building to target | `Command_Tower_Snosnobox_Launch` |
| Pick a unit / pick an ore | `Ui_Command_Boutman_Fishfollow`; `Command_Tower_Fiesilo_Reroute` |
| A no-build tile that selects its owner | `Block_Noplacementfish` |
| An invisible holder of upgrades | `Tower_Boutmanhousepersistent` (pattern) |
| A tile aura | `Entity_Ai_Tower_Tilebuff` + an effect being (`Effect_Smackywall_Bramble`) |
| Ground that spreads | `Effect_Snosnosnow` with `"Grow"` |
| Timed buffs (remove themselves) | `Effect_Smackyhut_Buff/Buff2` (towers), `Effect_Snosnoheater_Sweat`, `Effect_Busterdog_Towerboost`, `Effect_Tower_Boutmanbarrel_Spawnbed3_Boost` |
| Damage reduction, immunity, regen, thorns | `Effect_Smackywall_Beacon`, `Effect_Fiewall_Protection`, `Effect_Tower_Boutmanbarrel_Bucket4_Boost`, `Effect_Tower_Fieshop_Ramen3`, `_Calamari3` |
| Debuffs | `Effect_Snosnowall_Breaker`, `Effect_Fiewall_Weak`, `Effect_Mob_Boutmanfish_Pufferdebuff`, `_Bassdebuff`, `Effect_Tower_Bustertotem_Blazedebuff` |
| Ally buff projectile | `Tower_Fieshop_Buff`…`Buff5_Buffet` |
| Rings, beams, lobs | `Tower_Snosnoheater_Pulse`/`_Healpulse`, `Tower_Boutmanbarrel_Pulse`, `Tower_Fiewall_Shot`, `Tower_Bustertotem_Beam`, `Tower_Snosnobox_Snolaunch`, `Mob_Boutmandadbait` |
| A miner unit | `Entity_Ai_Mob_Smackyminer` (no Smacky names inside) |
| Debuff looks for `Vfxdebuffcheck` | `Debuff_Buffed`, `Debuff_Stun`, `Debuff_Slow`, `Debuff_Slowcold`, `Debuff_Fire`, `Debuff_Poison`, `Debuff_Recruit`, `Debuff_Dogfast1/2` (not `Debuff_Weakened`: `09` #110) |

A stock effect AI reads only its own upgrade's `Defaultparams`, so a clone of the upgrade with new params and a new
name works (as Boutmom reuses `Entity_Ai_Enemy_Red_Upgrade`). AIs named after a vessel's buildings (`Entity_Ai_Tower_*`,
the engineer, the Boutman family boosts, `Hero_Boutman_Shoot`) are name-locked: strip them from clones (`09` #9).

## 11. Integration rules

### 11.1 Writing a Direction or Trinket that works on every vessel

1. **Carry it as an upgrade** on the entity: Priority −3, empty `Costs`, no `Ent_Visual` (`ModKitDirections.Make`). On
   the vessel it survives death; a Perma-Wall or Thin Ice rebuild drops it.
2. **Find the player's things by `Entitydata.Value.Owner`** from `Entitiesserver["Hero"|"Tower"|"Trap"|"Mob"]`; the
   vessel is the `Hero` with that owner whose parent is tagged `Core`. Skip `Clone`/`Prespawn`.
3. **Never `Addmod` in `Onspawn`**: add the upgrade to `Initupgradelistserver` there, or `Addmod` at `Loaded`.
4. **Stick to the generic stats** every stock AI reads live: `Damage, Attackspeed, Range, Speed, Health, Projspeed,
   Buildrange, Sellrate, Respawn` (on unit buildings). Check `Statsserver.ContainsKey` first, and watch `Master.Upgrade`
   for stats that appear later (§2.1). A vessel's own counters (Snow, Thread, Units, Gun) work only where its code
   reads them. The upgrade's `Defaultparams` apply nothing by themselves: the Direction's code adds the mods (verified).
5. **Size percentages for the summed buckets**: a "+x %" is `Pmulti` for the base, `Multi` for everything; neither
   compounds with its own kind. Use `Add` for flat bonuses.
6. **Keep each `Link_Modifier`** and remove it yourself; use marker mods for "already applied". Never edit an Override.
7. **Re-apply on every spawn** of units and rebuilt towers (dogs every lap, Snobles all match, knights and miners on
   respawn, Gutter fish, Detonate's bomber).
8. **Per-hit effects** live in a stock-AI upgrade's `Interact` or a handler the brain re-inserts after each upgrade
   (§2.3), or, for damage taken scaled by a share, the stock `Resistadd` stat and `Fiewall_Regen` script (`05` §9,
   read only); **per-kill** effects use `Ondespawn` + `Lasthit.Entitydata.Value.Owner` and accept §8's gaps.
9. **Income pays the player directly** with `Giveresources(list, owner)`; never `−1`.
10. **Unit counts need the building's own count check**, never `Units` alone: after the mod, `SendMessage` the Hut's
    `Addressminercount`, the Keep's `Addressmobcount` or the Garage's `Addressdogcount` (verified); Boutman's barrels
    use `Vacantcurrent`.
11. **Never upgrade the Bouthouse** (leaks onto Boutman); act on Boutman at priority ≤ 8; buff the boat for Dad and Mom.
12. **Never put the tags `Sno`, `Snoble`, `Buster`, `Fish`, `Boutfamily`, `Knight`, `Silo`, `Fiewall` on pack things**
    unless the pack wants that base vessel's code to grab them.

### 11.2 Building a new vessel that generic effects reach

13. **Data tags**: vessel `Mob, Hero`; attack buildings `Tower, Towerfighter`; economy `Tower, Gatherer`; other
    buildings `Tower`; traps `Trap`; fighting units `Mob, Mobfighter`; worker units `Mob, Gatherer`; a unit that builds
    also `Hero`.
14. **The stock hero stack and stat names**: `Mob_General`, `Mob_Movement`, `Hero_Buildsell`, one stock attack AI, data
    stats `Health, Speed, Damage, Range, Attackspeed, Buildrange, Sellrate` (+ `Projspeed` ranged). Every unit gets a
    stat block (Boutdad's missing one makes him unreachable).
15. **Build through `Hero_Buildsell` with commands named `Buildoption1..4`**, created in the vessel's `Spawn`.
16. **Read stats live in brains**, never cache them in fields (the base vessels' param-only numbers are what run
    upgrades can't reach). Put a unit's numbers on the unit or its home building, read at use (as Boutdad reads the
    boat).
17. **Let hits fill `Damage` themselves** (`Interacthurt` with no `Damage` key, or `ModKitCombat`) so kills are
    credited.
18. **Spawn units with a fresh `Link_Spawn` and the building as parent** (owner follows); make the count follow a stat
    the brain polls, so "+1 unit" works by `Addmod` (the base vessels' `Units` does not).
19. **Keep vessel state in upgrades** so it comes back after death; give the vessel a `Skinoverride` named after its
    being (Diorama) and `<being>2` (Blurry Photo), and don't use `Skin` for gameplay stages on the vessel.

## 12. PC list

RogueLab round 7 (2026-10-12, three launches) ran items 1-6; the results are folded into the sections named.

1. Each stock Cardinal's core upgrades: dumped (§9). Done.
2. `Value_Type` of Health and the hero stats: Health is `Variable` and keeps its fraction; a Health raise refills
   nothing on a hurt vessel (§2.1). Done.
3. Boutman's first fish and barrel are free, and a cost mod waits for a recount (§5.1). Done. **Open**: does a
   Blurry Photo clone take the house's upgrades (needs that Cardinal equipped)?
4. More units: the count check after a `Units` mod spawns one on the Hut, Keep and Garage; `Units` alone spawns
   nothing (§8, rule 10). Done.
5. A −3 upgrade leaves Level, panel and model alone; on the house it reaches Boutman (§2.2, §5.1). Done.
6. Respawn faster per player: a cut made at the death comes too late (§2.4). Done; the `Onspawn` route is untested.

## 13. Sources

The decompiled `Assembly-CSharp.dll` (703 types), read by six parallel readers (one per vessel, one for the shared
layer and the stock Cardinals) and spot-checked by hand on 2026-10-12: the stat formula (Helper_Entitytools.cs:655-666),
upgrade pruning and Level (:484-565), stat creation after `Onspawn` (Entity_Master.cs:347-420; Game_Entity.cs:294),
the respawn's upgrade copy (Entity_Ai_Main_Core.cs:138-150), data-only tag lists (Game_Entity.cs:319-343), Torrid
Badge's `Buster` tag (Entity_Ai_Mob_Merchant.cs:41-43). Data from `reference/vessel_data_survey.txt`.
