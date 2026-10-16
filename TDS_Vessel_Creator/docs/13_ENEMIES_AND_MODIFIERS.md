# 13 · Enemies and enemy modifiers (new foes, new elites)

How the game defines its enemies, fills its Tides and makes "elite" enemies, and how a mod would add a new enemy type
and a new enemy modifier. Everything here was **read from the game's code** (`Assembly-CSharp.dll`, decompiled in a
cloud thread on 2026-10-04, see §11) unless it says **verified in a mod**. Riptide built the Tide-table
route (`15` §13) and §10 Q1-Q6 are answered (Q5 by `18`); test every other recipe in game before relying on it.

Read `01_GAME_MODEL.md` (§4-9: entities, AIs and brains, stats, upgrades, damage) and `12_MINION_VESSELS.md` (units
are the closest thing a mod has built to an enemy) before this one.

## 1. The shipped enemies

Enemy ids live in the `Mob` label like towers and units. The Sandbox artifact's enemy picker
(`Entity_Ai_Tower_Sandbox.Enemies`) lists the nine regular ones; the rest are map specials.

| Being id | Bespoke AI | What its code does | Stats its code reads (besides Health, Speed, Damage, Range, Attackspeed) |
|---|---|---|---|
| `Enemy_Dagger` | none | plain melee: walks to the Basin, hits what is in range | - |
| `Enemy_Spear` | none | same code as Dagger; the difference is data: 'Javelin', Speed 2.5, Damage 5, Attackspeed 1.4 (Dagger 1, 3, 0.5; RogueLab round 1) | - |
| `Enemy_Aegis` | `Entity_Ai_Enemy_Aegis` | heals itself (`Heal` once a second on the really-slow tick, debuff look `Debuff_Aegisheal`) once nothing has hit it for `Healtime` s (`Master.Lasthittime`) | `Healtime`, `Heal` |
| `Enemy_Ballista` | `Entity_Ai_Enemy_Ballista` | ranged: fires `Enemy_Ballista_Bolt` at the nearest target it can see, steps 0.3 forward, then slows itself 50% until its next shot | `Projspeed` |
| `Enemy_Maul` | `Entity_Ai_Enemy_Maul` | melee slam: Vis "Attack", 1.3 s wind-up, then hurts up to `Projcount` targets within `Projradius` (shake, `Maul_Explosion`) | `Projcount`, `Projradius` |
| `Enemy_Onager` | `Entity_Ai_Enemy_Onager` | lobs `Enemy_Onager_Bomb` at the **farthest** target it can see in range | `Projduration`, `Projcount` |
| `Enemy_Claymore` | `Entity_Ai_Enemy_Claymore` | when a tower is within `Rangedetect`, launches itself as `Enemy_Claymore_Bomb` at the farthest tower in `Range`. When "killed" it lingers untargetable for `Deathlife` s, then launches anyway if anything is near (the launch is a despawn, not a death: a Claymore killed near towers pays no drops and gives no kill, `24` §3) | `Rangedetect`, `Deathlife` |
| `Enemy_Excalibur` | `Entity_Ai_Enemy_Excalibur` | 1.3 s wind-up, then a straight bolt `Enemy_Excalibur_Bolt` | `Projspeed` |
| `Enemy_Bulwark` | `Entity_Ai_Enemy_Bulwark` | its hits stun (Damage, Speed, Attackspeed overridden to 0.01 for `Stunduration`, look `Debuff_Disabled`); attack = 1.6 s wind-up, close slam (`Closedamage` to `Closecount` targets in `Closerange`), then 5 s later drops `Enemy_Bulwark_Bomb` on up to `Projcount` spots out to `Maxrange` (target spots taken **before** the wind-up, each at least `Projradius` farther than the last; the slam never sets `Lasthit`) | `Stunduration`, `Projcount`, `Projradius`, `Maxrange`, `Closecount`, `Closerange`, `Closedamage` |
| (Halberd) | `Entity_Ai_Enemy_Halberd` | flying ore guard: flies to a resource, dives on Gatherers and vessels near it, retreats to its `Parent` (the `Halberdnest` tower that spawned it) below `Scarepercent` health and heals there; at full health it picks the nearest resource (`24` §2) | `Protectrange`, `Scarepercent`, `Speedfly`, `Heal` |
| `Enemy_Boutman` | `Entity_Ai_Enemy_Boutman` | the sleeping Boutman on Artrio: wakes at `Awaketide` or when no `Artriobarrier` is left; awake, spawns an `Evil`-skinned `Mob_Boutmanfishmini` every `Fishspawninterval` s and his Damage grows by `Damageincrease` a second; knocked out, a `Hero_Boutman` joins the players 5 s later; if a player already plays Boutman, an `Enemy_Reallystrongfish` spawns in his place | `Awaketide`, `Fishspawninterval`, `Fishhealthmulti`, `Damageincrease` |

What the enemies share:

- **Names are weapons and siege engines** (Dagger, Spear, Aegis, Ballista, Maul, Onager, Claymore, Excalibur, Bulwark,
  Halberd). A new enemy that should feel shipped gets a name from the same armoury (Flail, Pike, Glaive, Trebuchet...).
- **Attacks are tells.** Every heavy hit has a wind-up (1.3-1.6 s) announced by Vis "Attack" (the animation) and landed
  by Vis "Shoot" (the effect), so players can read it. Ranged enemies use projectiles the player can see.
- **Targets**: other-team entities tagged `Mob`, `Tower` or `Resource` (they also chip ore). Melee picks the nearest in
  `Range + 0.1` (`Helper_Entityai.Checkenemymelee`); ranged picks the nearest (Onager: farthest) **it can see**
  (`Checkenemyranged` sets `Visibleonly`: a ray from the enemy to the target must not hit the `Block` layer, i.e. the
  terrain; towers don't block it. Bulwark aims from 10 units up to skip the check).
- **Walking**: `Entity_Ai_Enemy_Basic` sends the enemy to its `Goal` (copied from its parent core) whenever it is Idle
  or Idleblocked. It needs a parent (kit pitfall 37).
- Bespoke AIs create their own `Attack` command, keep `Lastattack` against `1 / Attackspeed`, and lower the Move
  command's `Goalvalue` each slow tick while moving.

## 2. How an enemy is put together (data)

The same chain as any mob (`01` §2): `Beingobject` (label `Mob`) → `Entityobject` (`Name`, `Display_Name`,
`Display_Desc`, `Aiscripts`, `Visualscripts`, `Defaultstat`, `Defaultvisual`, `Tags`, `Upgradesinit`, `Drops`...).
Fields that matter for an enemy:

| Field | Why it matters |
|---|---|
| `Tags` | must contain **`Enemy`**: a Tide only waits for targetable team-0 entities tagged `Enemy` (§3) |
| `Drops` (`List<Link_Resourceset>`) | aura paid on death (`Game_Entity.Despawn` with Death = true), split between all players |
| `Countspawndeaths` | counts the kill in the killer's end-screen stats |
| `Targetable` | untargetable entities are skipped by tower queries **and by the Tide's "enemies left" check** |
| `Defaultvisual.Skinoverrides` | holds one look per elite (§4): skin names `Health`, `Red`, `Hyper`, `Chaos`, `Armor`, `Explode` |
| `Upgradesinit` | upgrades every copy gets at spawn (a built-in modifier or a tooltip) |

Which generic AIs and visual scripts the stock enemies carry, their stats, drops and sprite format are **not in the
code**: they are data. See §10, question 1.

## 3. Where enemies come from: modes, Tides and enemy cores

**The chain.** A Gameplan (`Gameplandatabaseobject`: `Map`, `Mode`, `Soundtrack`, medals) names a mode entry
(`Modeentryobject`, folder `Mode`: `Mode_Normal`, `Mode_Storm`, `Mode_Drought`, `Mode_Tsunami`, `Mode_Final`,
`Mode_Tutorial`). `Game_Init` loads that entry's group and sets `Game_Mapvars.Instance.Mode` to the `Modeobject` of
the same name, before the map is generated:

```
Modeobject                     (label Mode)
  .Enemies        Wavesobject  the Tide table
  .Modescript     name of the server script: Mode_Normal (also Storm/Drought/Tsunami), Mode_Final, Mode_Tutorial
  .Clientmodescript            Clientmode_*: music, tips on every machine (the Tide stings come from the host's
                               Mode_Normal through Tds_Multiplayer_Radio, so a mode on Mode_Normal gets them, `21` §1.2)
  .Specialspots   List<Entry{Location, Team, Optional, Item}>  beings placed at named spots of the map's "Mode" object
  .Defaultparams  Startwait, Basinthreshold, Tipthreshold...
Wavesobject
  .Resourcesinit  List<Link_Resourceset>   starting aura
  .Wave           List<Waves>              one entry per Tide or break, played in order
  .Totalwaves     number of entries with Wave = true (artifacts scale on it: 30 / Totalwaves)
Waves     {Displayedwave, Note (shown under the Tide banner), Wavetime (s), Wave (true = a Tide, false = a break), Content}
Wavecontent {Style, Interval, Count, Team, Enemy (Beingobject), Gate, Upgrades (List<Upgradeproperties>), Resources}
```

So the Tide table belongs to the **mode**, not the map: maps played in the same mode share it (confirmed,
§10). Maps add their own enemy-side pieces through `Specialspots` and `Map_Generationedit` (team, skin, `Preupgrade`
list) on placed objects.

**The loop** (`Mode_Normal.Run`, server only):

1. Players are put on team 1; enemies are team 0. Every entity tagged `Enemycore` gets `Healthresource` = the number of
   Tides (it drops by 1 each Tide: the core's counter is "Tides left").
2. Wait `Startwait`, give `Resourcesinit`, then play `Wave[Entry - 1]` for `Entry` = 1, 2, ... (`Game_Mapvars.Entry`).
3. A Tide entry raises `Game_Mapvars.Wave`, shows "TIDE n", then runs each `Wavecontent` **in turn** (the next one
   starts when the previous one has finished spawning):
   - `Spread`: `Count` spawns, `Interval` s apart, rotating over the enemy cores;
   - `Gate`: the same, all from enemy core number `Gate`;
   - `Wait`: no spawn, just waits `Interval` once;
   - `Resources` are handed out when the content starts.
4. The Tide then lasts until `Time_Wave` (set to `Wavetime`) runs out **or** no targetable team-0 `Enemy` is left.
   `Time_Wave` only starts counting once every content has finished spawning (Mode_Normal.cs:384-403), so "ending on
   time" means the whole entry, spawning included, took longer than `Wavetime` (decompile 2026-10-14, `24` §4).
   Ending on time plays "Tide_Completetime"; whatever is still alive keeps walking into the next entry.
5. A break entry counts `Wavetime` down; when the next entry is a Tide, the cores fire hint projectiles at the enemy
   cores and the banner reads "TIDE APPROACHES" under 3 s.
6. After the last entry the Basin remains (win). A destroyed core ends the run (lose).

**The spawn.** Content becomes a command on an enemy core:

```csharp
Helper_Entitytools.Updatecommand(enemyCore, "Spawn", json);   // json = {"Spawn":"Enemy_Dagger","Upgrades":["Enemy_Health_Upgradeobject"]}
```

`Entity_Ai_Main_Enemycore.Command` (server) then picks the next player core reachable by path as the `Goal`
(round-robin, so foes split between Basins), looks up the being in `Mob` and each upgrade in `Upgrade`, and spawns it
`Loose` on the next standable tile around the core with the upgrades as `Preupgrades`, the core as parent (team 0,
the core's skin), plus the `World_Enemyspawnborder` effect. The Sandbox's "Spawn" button (`Game_Admin.Spawnenemy`)
uses the same command.

**Spawn order on the new enemy** (`Entity_Master.Executestart`): its own AIs' `Spawn()` → every `Preupgrade` and
`Upgradesinit` entry applied (their AIs run `Spawn()` + `Starting()` at once) → tags → `Loaded` event → its own AIs'
`Starting()`. `Game_Entity.Onspawn` fires before all of that, with `Initupgradelistserver` already filled.

## 4. Enemy modifiers ("elites")

A modifier is an **upgrade applied to the enemy at spawn**. The game has six, all ids `Enemy_<Key>_Upgradeobject`,
each with a stock AI that reads its params and sets the enemy's skin. The names are the Sandbox's labels
(`Entity_Command_Tower_Sandbox_Elitetype`); they are all **metalworking words**, and a new one should be too
(Quenched, Forged, Galvanized, Gilded...).

| Name | Upgrade id | AI | Effect (from its code) | Params it reads | Skin |
|---|---|---|---|---|---|
| Hardened | `Enemy_Health_Upgradeobject` | `Entity_Ai_Enemy_Health_Upgrade` | more health | `Health_Multi` | `Health` |
| Tempered | `Enemy_Red_Upgradeobject` | `Entity_Ai_Enemy_Red_Upgrade` | more health and damage; **aggressive**: chases whatever hurts it or the farthest unblocked Mob/Tower within `Aggrorange_Base` (the last of a nearest-first list) instead of the Basin, shown with `Debuff_Redangry` on itself and `Debuff_Redtarget` on its target; gives up on the 6th blocked check and marks that target `Redblocked` for 5 s | `Health_Multi`, `Damage_Multi`, `Aggrorange_Base` | `Red` |
| Superheated | `Enemy_Hyper_Upgradeobject` | `Entity_Ai_Enemy_Hyper_Upgrade` | health, speed, damage and attack speed changed | `Health_Multi`, `Speed_Multi`, `Damage_Multi`, `Attackspeed_Multi` | `Hyper` |
| Fractured | `Enemy_Chaos_Upgradeobject` | `Entity_Ai_Enemy_Chaos_Upgrade` | same four multipliers as Superheated (the values differ in data) | same | `Chaos` |
| Annealed | `Enemy_Armor_Upgradeobject` | `Entity_Ai_Enemy_Armor_Upgrade` | adds `Healtharmor` = `Healtharmor_Base` + base Health / `Healtharmor_Perhealth`: armour that loses 1 per hit whatever the damage, so it takes that many hits before health drops (fast towers beat it) | `Healtharmor_Base`, `Healtharmor_Perhealth` | `Armor` |
| Molten | `Enemy_Explode_Upgradeobject` | `Entity_Ai_Enemy_Explode_Upgrade` | more health, changed speed; **on death** (`Effect_Enemy_Explode`) hurts up to `Damage_Count` mobs, towers and ore within `Radius_Base` for `Damage_Base` and makes them **Melting** | `Health_Multi`, `Speed_Multi`, `Damage_Count`, `Radius_Base`, `Damage_Base` | `Explode` |

**Melting** (`Effect_Enemy_Weakened_Upgradeobject`, `Entity_Ai_Effect_Enemy_Weaken`): a timed effect on the victim
that drains Health, Attackspeed, Damage and Speed multipliers every tick (`Health_Drain`, `Attackspeed_Drain`,
`Damage_Drain`, `Speed_Drain`) for `Duration_Base` s, shows `Debuff_Melting` and an `Enemyweaken` counter; further
hits that carry the `Enemyweaken` key refresh it and raise the counter (faster drain), then it removes itself. It is
the template for any "leaves a lingering debuff" modifier.

**Who applies modifiers:**

- the Tide table: `Wavecontent.Upgrades` (every enemy of that content gets the list);
- the Sandbox's elite picker (one at a time);
- map objects: `Map_Generationedit.Preupgrade`;
- other code at `Game_Entity.Onspawn`, by adding to `Initupgradelistserver` (the artifact helper
  `Entity_Ai_Artifact_Teamapply` does this for towers and units); the game then applies it like a Tide elite;
- stock modifier AIs can be reused on other upgrades with other params: Boutman's mom gets "Offended"
  (`Effect_Mob_Boutmanmom_Aggro_Upgradeobject`) through `Entity_Ai_Enemy_Red_Upgrade` with `Damage_Multi` 0.

**What the player sees:** the elite skin, its effects, and, on selecting the enemy, one line per applied upgrade with
a non-empty `Display_Name` (name, `Display_Desc`, an icon picked by `Priority`, `Game_Ui.Updateupgrades`). The
modifier's name and description are its tooltip.

**Rules the code implies:**

- An entity has **one** skin. Two modifiers that both set one: the last applied wins, and removing either resets it
  to "". A skin the enemy's visual doesn't have falls back to the default look (`Entity_Master_Client`).
- Stat changes are `Multi` modifiers, so two elites' `Health_Multi` add, they don't multiply (a `Multi` and a `Pmulti`
  multiply: `(Base × Pmulti + Add) × Multi`, Helper_Entitytools.cs:661).
- The stock `Downgrade` of Superheated, Fractured and Tempered removes `Mod1` several times and never the others.
  Elites are never removed in play so it doesn't show, but don't copy that code.

## 5. Making a new enemy type in a mod

Same constraints as every TDS mod: mod classes can't be AIs (logic lives in brains added on `Onspawn`), every id is
global (carry the mod's name), host decides, clone templates instead of building from nothing.

**Where it lives.** Enemies change everyone's game, so they belong in their own mod rather than inside a vessel: an
"enemy pack" made from the Mummer template with the vessel, journal tab and boat parts removed and the boot, register,
brains and self-test kept. (Default chosen here; a vessel that summons its own foes is the exception.)

1. **Clone a template enemy** whose behaviour is closest (Dagger for plain melee, Ballista for ranged, Maul for a
   slam...). Clone the `Beingobject`, its `Entityobject`, `Statproperties` and `Visualproperties`
   (`MummerUtil.Clone`), name them `Enemy_<Mod><Name>` / `..._Entity`, and register them in the mod's own `Mob` list
   on **every machine** at boot (`05` §2). The template must be loaded when you clone it (§10, question 4).
2. **Keep** the `Enemy` tag, `Entity_Ai_Enemy_Basic` and the template's generic AIs. Keep the template's bespoke
   `Entity_Ai_Enemy_<X>` only if you want exactly that attack; otherwise strip it and write the attack in a brain.
3. **Stats**: Health, Speed, Damage, Range, Attackspeed (+ Projspeed for ranged), plus any stat your brain reads (stock
   enemies use `Projcount`, `Projradius`, `Stunduration`...: reuse those names). **Drops** decide the aura it is worth.
4. **Art**: sprite sheets in the stock enemies' format and style (`18` §2, §5: a grey stone body carrying a magenta
   crystal weapon, the 8 foe colours). Keep the six `Skinoverride`s `Health`, `Red`, `Hyper`, `Chaos`, `Armor` and
   `Explode` of the cloned `Visualproperties`: each adds a shared overlay material to the foe's own sprite, so no
   elite sheet is drawn (`18` §5.5). Wind-ups need an "Attack" animation
   played on Vis "Attack" (`Entity_Vis_Mob_Attack`, or a client hook like `Entity_Vis_Enemy_Maul`).
5. **Behaviour** in a server brain attached in `Game_Entity.Onspawn` when `Beingserver.Being.Objectname` is yours. Copy
   the stock patterns: an `Attack` command and `Lastattack` vs `1 / Attackspeed`; wind-up then Vis "Shoot"; hits through
   `Helper_Entitytools.Interact(..., "Hurt", {Damage, Damagemulti})` (kit 1.3 `ModKitCombat`); shots with a cloned
   enemy projectile (`Enemy_Ballista_Bolt`...) through `Helper_Damagetools.Projectile`; on-death effects in a
   `Despawn` handler (the game clears the entity's delegates right after calling them).
6. **Put it in play**, one of:
   - **extra spawns** (simplest, additive): on the server, at a Tide start (`Game_Mapvars.Instance.Wave.OnValueChanged`),
     ask the enemy cores to spawn it through the same command the Tides use:

     ```csharp
     // Server only. Mods have no JsonUtility: write the JSON by hand, always with an Upgrades array.
     static void SpawnFromCores(string being, string upgradesCsvQuoted /* e.g. "" or "\"Enemy_Health_Upgradeobject\"" */)
     {
         Link_Entityquery q = default(Link_Entityquery);
         q.Tags = new List<string> { "Enemycore" };
         q.Style = Link_Entityquery.Styles.Tag;
         string json = "{\"Spawn\":\"" + being + "\",\"Upgrades\":[" + upgradesCsvQuoted + "]}";
         foreach (Entity_Master core in Helper_Entityquerytools.Entityall(Vector3.zero, q))
             Helper_Entitytools.Updatecommand(core, "Spawn", json);
     }
     ```

   - **in the Tide table** (feels most shipped: same pacing, banner, spawn border): on the server, before the Tide is
     reached, add a `Wavecontent` to the `Content` list of the Tides you want in `Game_Mapvars.Instance.Mode.Enemies.Wave`.
     `Game_Init` sets `Mode` before it generates the map, so the first `Enemycore` `Onspawn` should be a safe moment
     (inferred from the call order; confirm with the first test).
     Insert it among the existing contents (they run one after another, so appending at the end delays it until the
     rest has spawned). Give it non-null `Upgrades` and `Resources` lists. Don't add, remove or retype `Waves`
     entries: `Run` counts them once at the start and artifacts scale on `Totalwaves`. The `Modeobject` is a loaded
     asset that stays in memory, so the edit survives into the next run of that mode: edit once per session per mode.
   - **the Sandbox** for hand tests: `Entity_Ai_Tower_Sandbox.Enemies` and `.Elites` are public lists; add your ids
     when the Sandbox tower spawns (its button label won't know your name, it keeps the previous one).
7. **The Tide must wait for it**: tag `Enemy`, team 0 (it inherits the core's team as a child), targetable. Never make
   it untargetable to "hide" (burrow, stealth): the Tide counts only targetable enemies and can end while it lives.
   Use a Speed/Damage hold or a skin instead.

Design notes from the stock roster: each enemy adds **one** new rule the player must answer (heals if left alone,
outranges towers, punishes clumps, stuns, flies to ore); big rules come with a visible wind-up; enemies stay readable
at 1x. Vessel mods tell foe families apart by AI component (`05` §16, Mummer's Understudy costumes), so a new enemy
without a stock bespoke AI reads as a plain melee foe to them.

## 6. Making a new enemy modifier in a mod

1. **An upgrade** `Enemy_<Mod><Name>_Upgradeobject` (`ScriptableObject.CreateInstance<Upgradeproperties>()`; fill
   both `Defaultparam` and `Defaultparams`), `Priority` -1, a one-word metalworking `Display_Name`, a short comma-phrase
   `Display_Desc` with no numbers, its numbers in params with the game's names (`Health_Multi`, `Radius_Base`,
   `Damage_Count`...). Register it in the mod's `Upgrade` list on **every machine**: the enemy panel looks it up
   locally by name.
2. **Its logic**: leave `Aiscripts` empty and attach a server brain in `Game_Entity.Onspawn` when
   `master.Initupgradelistserver.ContainsKey(id)` (or on the entity's `Upgrade` event for upgrades added later). A
   stock modifier AI can be listed instead (name carrier) when its effect is exactly right, but each one also sets its
   own skin. Read stats from the `Loaded` event or the next tick, not in `Onspawn` (stats are empty on the spawn
   frame).
3. **Its effect**, with what the engine already does: stat modifiers (`Addmod`; `Pmulti` scales only the base and
   compounds with `Multi`, but mods of one kind add up, `17` §2.1); reactions to hits taken (`Oninteract`, first in
   the chain, `05` §9) or dealt (`Interact`); a death effect
   (`Despawn` with Death = true, like Molten); a lingering debuff on others (an `Effect_*` upgrade like Melting); an
   overshield (`Healthover` stat, absorbs damage) or hit armour (`Healtharmor`, absorbs hits).
4. **Its look on every screen**: base enemies have no skin for a new modifier, and an entity holds one skin (a modifier
   skin would hide a stock elite's look; the stock skins are overlay materials, not redrawn sprites, `18` §5.5), so
   show it with an effect parented to the enemy and refreshed (`Radio.Vfxdebuffcheck(name, master.Beingref.Value,
   seconds)` every couple of seconds, as Tempered does). That call is dropped while 50 sends are in flight, which a busy
   online fight reaches: a look every player must see goes out the way Argo 2.0.1's `ArgoUtil.Debuff` sends it (`19`
   §4, `09` #95). Leave the skin to the stock elites. Give it a colour no elite
   uses (taken: green, white, red, near-black, bronze, orange; `18` §5.5, §7).
5. **Who gets it**: list it in a `Wavecontent.Upgrades` (Tide table route above), in the `Upgrades` array of a
   `Spawn` command, or pick enemies at `Onspawn` and add it to their `Initupgradelistserver` (for "every fifth foe
   from Tide 8"). Skip `Enemycore`/`Core` and anything not tagged `Enemy`.
6. **Balance** against the stock elites' param values (surveyed, §10 question 2): a modifier is one
   twist on an existing enemy, not a new enemy.

## 7. Online

- The Tide loop, enemy cores, enemy AIs and modifier logic run **only on the host**. Tide-table edits and `Spawn`
  commands are host-only too (clients read only `Mode.Defaultparams` thresholds for music and tips).
- Clients need the enemy's being, entity, visuals and every modifier upgrade **registered locally** (names resolve
  through the global table), exactly like units and towers.
- What clients see comes through the networked channels: the spawn, stats, skin, `Upgradelist` (the enemy panel),
  Vis RPCs ("Attack"/"Shoot") and debuff/one-shot effects.

## 8. Testing

- The **Sandbox** artifact is a ready test bench: enemy and elite pickers, spawn, clear, next Tide, pause, fast
  forward, god mode for cores and vessels. Its lists can take mod ids (§5.6).
- In a self-test, spawn foes as children of an enemy core (`08`'s `SpawnFoe`) and pass a modifier in
  `Link_Spawn.Preupgrades`; or send the core a `Spawn` command to test the real path.
- Check, by counting events: the Tide does not clear while the new enemy lives; the enemy walks to the Basin and
  attacks what the code says; its drops pay; a modifier's stat change, effect and panel line appear; each of the six
  stock elites shows its skin on the new enemy.

## 9. Pitfalls (expected from the code; move them to `09` once met)

1. **The Tide clears while your enemy still walks** → it lacks the `Enemy` tag, isn't team 0, or turned untargetable →
   keep all three.
2. **A `Spawn` command does nothing** → the enemy core hadn't found a reachable player core yet (`Corelist` empty: the
   request only refreshes the list and is dropped), or the being/upgrade name isn't registered on the host → spawn
   from a Tide start, check names.
3. **A typo in an Upgrades name crashes the spawn** → the core adds `null` to `Preupgrades` and `Game_Entity.Spawn`
   reads its name → validate names before sending.
4. **`JsonUtility` doesn't compile** → mods don't have it → build the JSON string by hand, always with an `Upgrades`
   array (empty `[]` if none).
5. **An added `Wavecontent` throws** → its `Upgrades` or `Resources` list is null → give both an empty list.
6. **Your enemies multiply every run** → the Tide table edit stayed in the loaded asset → edit once per session per
   mode.
7. **Elites look plain on your enemy** → no skins named `Health`, `Red`, `Hyper`, `Chaos`, `Armor`, `Explode`.
8. **Your modifier has no line on the enemy panel** → empty `Display_Name`, or the upgrade isn't registered on that
   client.
9. **A ranged enemy never fires over a hill** → `Checkenemyranged` needs a clear ray over the terrain → aim from higher
   (Bulwark) or lob (Onager).
10. **Copied stock `Downgrade` leaves stats changed** → Superheated/Fractured/Tempered remove `Mod1` repeatedly →
    remove each modifier you added.
11. **A `*Gameserver*` prefab's code can't see the main mod's statics** → the game compiles each activated mod prefab's
    code separately (`Bla_Mods.Activatemodprefab`; keywords `Default`, `Mainmenu`, `World`, `Loading`, `Gameserver`,
    `Gameclient`) → put code that shares statics in one `Default` prefab (a bundle may hold several, each compiled
    alone, `15` §15) and hook `Onspawn`/`Wave` as the kit does.

## 10. Open questions for a PC session

1. **Stock enemy data**: the AIs, visual scripts, stats (with `Leveladd`), drops, tags, skins and `Upgradesinit` of each
   `Enemy_*`. Sprigg's research build already logs this (`examples/code/Sprigg_95_Research.cs`, `Enemies()`); its output
   may still be in Sprigg's `Research\dump` (now under `C:\Users\Dimitri\TDS_Mod\Prototype Mods\Sprigg`). *From the code
   (2026-10-08):* enemy AIs hold behaviour and fixed constants only; every number is a stat and every tag comes from
   `Beingobject.Entity.Tags` (no enemy AI adds tags). There is no Dagger or Spear AI. *Answered on the PC (2026-10-08,
   RogueLab round 1):* the 8 stock foes' AIs, visuals, stats, drops, tags and skins are in
   `reference/roguelab_round1.txt` (ENEMY lines); none has `Upgradesinit`, every one carries the six elite skins.
2. **Stock elite values**: `Display_Name`, `Display_Desc` and `Defaultparams` of the six `Enemy_*_Upgradeobject`s and
   `Effect_Enemy_Weakened_Upgradeobject`. *From the code (2026-10-08):* none of it is in code. Each AI reads
   `Defaultparams["X"]` bare (a missing key throws, so a reused stock AI needs every param it reads); the only constants
   are Tempered's 5 checks and 5 s `Redblocked`, Melting's `Enemyweaken` base 100 and 2 s debuff. *Answered for five
   (RogueLab round 1, ELITE lines):* Hardened `Health_Multi` 3; Tempered `Aggrorange_Base` 5, `Health_Multi` and
   `Damage_Multi` 1; Superheated health and damage 2, speed 1.25, attack speed 1; Fractured health 14, damage, speed and
   attack speed 9 (each a `Multi` modifier, which adds to a multiplier that starts at 1: Hardened health ×4, Tempered
   ×2, Superheated ×3, speed ×2.25 and attack speed ×2, Fractured ×15 and ×10); Annealed `Healtharmor_Base` 4,
   `Healtharmor_Perhealth` 10. *The last two answered (round 2, 2026-10-09; they ship in the Tsunami and Final groups):*
   Molten `Health_Multi` 3, `Speed_Multi` 0.15 (health ×4, speed ×1.15: the same `Multi` mods as Superheated's), on
   drowning hurts up to `Damage_Count` 5 within `Radius_Base` 2.2 for `Damage_Base` 10; Melting drains health, damage,
   speed and attack speed at -0.001 each for `Duration_Base` 60 s.
3. **Tide tables**: for each mode, every `Waves` (Note, Wavetime, Wave) and `Wavecontent` (enemy, count, interval,
   style, gate, upgrades); and confirm that two maps in one mode play the same table. *Mode_Normal answered* (RogueLab
   round 1, TIDE lines: 60 entries, 30 Tides, 180 s each, 30 s breaks). *The others answered (round 2, TIDE2 lines in
   `reference/roguelab_round2.txt`):* Storm 15 Tides (180 s, 30 s breaks, start 50 of each aura), Drought 20 (120 s, 20 s breaks,
   Annealed and Claymore rushes), Tsunami 4 Tides (180 s, the last 9999 s; long breaks of 240 s then 180 s; start 500
   of each aura, Molten and Bulwark), Final 1 Tide (start 100 of each aura, three Bulwarks and Excaliburs), Tutorial 3 Training Dagger Tides.
   A Gameplan names its mode, so two maps on one mode play one table. In every `Mode_Normal`-script mode the last
   Tide's `Wavetime` is 9999 s (Normal's 30th, Storm's 15th, Drought's 20th, Tsunami's 4th): it ends only when cleared.
4. **When the enemies are loaded**: which Addressables group holds `Enemy_Dagger` (search `Bl_Address.Loadedfolders`
   for the `Loadlist` whose `Items` has it) and whether it is loaded at boot or only in a match. That decides when a
   mod can clone a template enemy on every machine. *From the code (2026-10-08):* boot loads the groups named in
   `Database.Init` (`Bla_Addressables.cs:25-31`, data); a match loads `General_Default`, each hero's, artifact's, map's
   and mode's groups; nothing is ever unloaded. `Getload("Mob", name)` is null until its group loads. PC: log the
   `Init` names and `Bl_Address.Groups["Mob"]` (group and item names) at mod load and in a match (`15` §11 step 9).
   *Answered (RogueLab round 1):* boot loads `General_Init` and `General_Ambientinit` (sounds and effects only); every
   stock enemy is in the **mode's** group (`Mode_Normal`: Dagger, Spear, Aegis, Ballista, Excalibur, Maul, Onager,
   Claymore), which a mod can load at boot with `Bl_Address.Load` (3.5 s).
5. **Enemy art format**: sheet and frame size, PPU, animation names, and the elite skins' recolours. *Answered
   (2026-10-15, read from the game's asset bundle):* `18` §1-2 and §5; elite looks are overlay materials, part 3 of
   `reference/art_survey_2026-10-15.txt`.
6. **Journal**: whether the base journal has enemy pages a new enemy should join (`Logitems.TextImageBeing`, `Stats`
   and `Drops` can show one). *Answered (`docs/RESEARCH_GAPS.md` C4):* pages are data assets and no code lists enemies,
   so the pack's foe pages are its own.
7. **Flying**: how Halberd's flight interacts with walls and towers, if a flying enemy is ever wanted. *Answered from
   code (2026-10-14):* `24` §2.

## 11. How this was read (cloud decompiling)

The game's code can be read as C# in a cloud thread in about a minute, with no Windows:

```bash
apt-get update && apt-get install -y mono-devel   # mono + mcs; without the update a fresh container
                                                  # says "Unable to locate package" (2026-10-14)
# NuGet packages (api.nuget.org is reachable; dot.net downloads are not):
#   ICSharpCode.Decompiler 8.2.0.7535 (netstandard2.0), System.Collections.Immutable 6.0.0,
#   System.Reflection.Metadata 6.0.0, System.Memory 4.5.4, System.Runtime.CompilerServices.Unsafe 6.0.0,
#   System.Buffers 4.5.1, System.Numerics.Vectors 4.5.0  -> copy each lib/netstandard2.0/*.dll into bin/
```

`dump.cs`, compiled with `mcs -r:bin/ICSharpCode.Decompiler.dll -r:bin/System.Reflection.Metadata.dll
-r:bin/System.Collections.Immutable.dll -r:/usr/lib/mono/4.5/Facades/System.Runtime.dll
-r:/usr/lib/mono/4.5/Facades/netstandard.dll -r:System.Core.dll`, writes one `.cs` per type (703 types, 5.3 MB, 15 s)
from the game's `TDS\TDS\DATA\Managed\Assembly-CSharp.dll` (`docs/11` §4), which reached the container as a Claude Project file
(`/mnt/project-files/...`; a new Claude Project needs it uploaded again, it is never committed); grep the folder instead
of re-deriving IL with `tools/cecil`:

```csharp
using System; using System.IO; using System.Linq;
using ICSharpCode.Decompiler; using ICSharpCode.Decompiler.CSharp; using ICSharpCode.Decompiler.Metadata;
class P { static void Main(string[] a) {           // dump.exe <Assembly-CSharp.dll> <outDir> [name filters...]
  var pe = new PEFile(a[0]); var res = new UniversalAssemblyResolver(a[0], false, pe.DetectTargetFrameworkId());
  res.AddSearchDirectory(Path.GetDirectoryName(a[0])); Directory.CreateDirectory(a[1]);
  var d = new CSharpDecompiler(a[0], res, new DecompilerSettings(LanguageVersion.CSharp7_3) { ThrowOnAssemblyResolveErrors = false });
  foreach (var t in d.TypeSystem.MainModule.TypeDefinitions.Where(t => t.DeclaringTypeDefinition == null)) {
    var n = t.FullName.Replace('<','_').Replace('>','_');
    if (a.Length > 2 && !a.Skip(2).Any(p => n.Contains(p))) continue;
    try { File.WriteAllText(Path.Combine(a[1], n + ".cs"), d.DecompileTypeAsString(t.FullTypeName)); }
    catch (Exception e) { File.WriteAllText(Path.Combine(a[1], n + ".ERR"), e.ToString()); } } } }
```

The decompiled code is the game's: keep it in the thread's scratchpad, never commit or ship it. Data (stats, Tide
tables, param values) lives in the Addressables bundles, which only exist on the PC: that is what §10 is for.
