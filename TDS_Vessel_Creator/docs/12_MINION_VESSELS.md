# 12 · Minion vessels (units, like Boutman and Fie)

A minion vessel plays through units it makes, keeps and orders: Fie's knights, Boutman's fish, Buster's dog,
Smacky's miners, and in mods Mummer's Understudies, Argo's Nauts and Sprigg's Sproutlings. This chapter is what the
game's own unit code does (read from its IL, 2026-10-02) and how a mod builds the same thing. Each recipe says whether
it was **verified in a mod** or is **read from the game's code** (then test it first).

## 1. How the shipped vessels do it

| Vessel | Unit source | Units | Count / respawn | Orders | Upgrades reach units by |
|---|---|---|---|---|---|
| Smacky | Mining Hut (Yellow 35) | miner: 5 HP (+5/lvl), speed 1, dmg 0.5, mines ore | `Units` 1, `Respawn` 10 s | none (they mine near the hut); Convert button changes what ore pays | hut upgrades ("Mining Operation: hire another miner"); miners' level = hut level |
| Buster | Garage (Red 60) | dog: 50 HP (+10/lvl), dmg 3, speed 2, damage from speed | 1, 5 s | none | garage upgrades mirrored as `Mob_Busterdog_<Key><Tier>` upgrades (Jack Up: `Health_Pmulti` 2, so health ×3, respawn -4.5 s) |
| Fie | Knight's Keep (Red 60) | knight: 25 HP, dmg 1 (+0.5/lvl), speed 1, engage 1; weapons from Fie's Silo | 1, 10 s; Partner/Squad add knights | **Rally**: click a tile, knights defend that spot | keep upgrades mirrored as `Mob_Fieknight_<Key><Tier>` (Partner, Squad, Commander, Convoy...) |
| Boutman | fish (`Mob_Boutmanfish`, Yellow 25 each) recruited through Boutman himself (his fishing and Fish/Fishfollow commands; read `Entity_Ai_Hero_Boutman` before copying the exact flow); Barrel (Blue 25) houses and heals them; Bouthouse carries Boutman's own upgrades | fish: 50 HP (+75/lvl), dmg 10 (+10), engage 3; get knocked out (Deadarmor) instead of dying and recover in a barrel | per fish | each fish guards its spot (`Engagerange`); **Relocate** (upgrade) moves a fish's spot; the vessel can make a fish **follow** him; **Rally** | each fish has its own upgrade tree (Barp, Batfish, Bwordfish, Bass...) |
| Mummer (mod) | Puppet Stage (Red 50), paid in Thread | Understudy 24/16/70 HP by costume | 2 (+1/+2), 6 s | leashed to the stage, step out to meet foes (Marked first); COSTUME picks the next one | stage params applied by the stage brain (Puppethealth_Multi...) |
| Argo (mod) | Diving Bell (Blue 35) | Nauts (Port, Starboard, Bosun) | 2, 8 s | **PORT/STARBOARD**: pick a tower, the Naut walks there and tunes it | bell upgrades read by the Naut brain |
| Sprigg (mod) | Root (Yellow 21) | Sproutling: a miner clone using the game's own miner AI | 1, 10 s | none | — |

Boutman lists only three buildables (Fish Barrel, Boutboat, Bouthouse): his fish take the place of a fourth. A minion
vessel can trade a building slot for its units that way. His vessel upgrades come from the Bouthouse
(`Askforupgrades`): a building can be the home of the vessel's own upgrade tree.

## 2. The unit building (spawner): what the game does

Every shipped unit building (`Entity_Ai_Tower_Smackyhut`, `_Bustergarage`, `_Fiekeep`) runs the same loop:

1. **Spawn template**: one `Link_Spawn New` with `Genadd` = the building's own tile (`Beingserver.Currentblock`),
   `Spawnstyle = Forced`, `Being = Getload("Mob", unitId)`, `Preupgrades` = the unit-side upgrades bought so far.
2. **Add()**: `Helper_Entitytools.Spawn(New, Master.Position.Value, Master.Beingserver, true, null)`: the unit appears
   on the building's tile with the building as its `Parent` (the building's `Children` list holds its units), same
   owner and team. The building keeps the `Link_Being` in a list, subscribes the unit's **`Loaded`** event
   (`Entity_Master m`) to apply per-unit extras when it is ready (skins, modifiers: `Specialspawn`), and its
   **`Despawn`** event (`bool Death`) to respawn.
3. **Respawn**: when a unit despawns, wait the building's `Respawn` stat (seconds), and if the building still stands,
   spawn again from the same template.
4. **Count**: `Units` stat = how many. Extra units are removed with `Game_Entity.Instance.Despawn(being, false, "Pop")`;
   missing ones are added.
5. **Upgrades reach units** two ways: the building's upgrade `Tower_X_Key2` adds the matching unit upgrade
   `Mob_Y_Key2_Upgradeobject` to `New.Preupgrades` (future units) and applies it to every live unit with
   `Helper_Entitytools.Upgrade(unit, up, ..., null)` (`Removeupgrade` undoes it); and the unit's **level** follows the
   building's: the Hut sets `unit.Option_Defaultlevel = building.Entitydata.Value.Level`; the Garage's and Keep's units
   level through the mirrored upgrades (an upgrade sets Level to its highest Priority). Either way the unit's
   `Leveladd` stats grow with the building.
6. **Unit AI**: a bespoke `Entity_Ai_Mob_<Unit>` on top of `Mob_General` + `Mob_Movement` decides what the unit does
   (mine, defend a spot, follow, use a silo weapon). Some are generic enough to reuse: Sprigg's Sproutlings run the
   game's own `Entity_Ai_Mob_Smackyminer` (it never names Smacky's objects). The knight and fish AIs are tied to Fie's
   and Boutman's objects; don't reuse those.

## 3. A unit building in a mod (verified: Mummer's Stage, Argo's Bell, Sprigg's Root)

Mod classes can't be AIs, so the building's **brain** does the spawner loop and each unit gets a **unit brain**
(`docs/05` §6, §10). Template: `template/Mummer/Code/08_Stage.cs` (`MummerStageBrain`) and `09_Puppet.cs`.

```csharp
// in the building brain (a MummerBuilding subclass), server only
readonly List<MummerPuppetBrain> cast = new List<MummerPuppetBrain>();
float nextSpawn;

protected override void Tick()
{
    cast.RemoveAll(u => u == null || u.Master == null || !MummerUtil.Alive(u.Master));
    int want = Mathf.RoundToInt(Stat("Units", 1f));
    if (cast.Count >= want || Time.time < nextSpawn) return;
    nextSpawn = Time.time + Stat("Respawn", 6f);         // one unit per Respawn interval, like the game
    Spawn(MummerContent.Beings[unitId]);
}

void Spawn(Beingobject being)
{
    Link_Spawn spawn = Helper_Entitytools.Createlinkspawnparent(Master.Beingserver);   // owner, team, parent
    spawn.Genadd = Master.Beingserver.Currentblock;
    spawn.Spawnstyle = Link_Spawn.Spawnstyles.Forced;
    spawn.Being = being;
    MummerServer.PendingStage = this;                    // OnSpawn (synchronous) tells the unit brain its building
    Link_Being b = Helper_Entitytools.Spawn(spawn, Master.Position.Value, Master.Beingserver, true, null);
    MummerServer.PendingStage = null;
    if (b != null && b.Worldobject != null) { var u = b.Worldobject.GetComponent<MummerPuppetBrain>(); if (u != null) cast.Add(u); }
}
```

- **Unit data**: clone `Mob_Smackyminer` (being + entity), AIs `Entity_Ai_Mob_General` + `Entity_Ai_Mob_Movement` +
  `Entity_Ai_Mob_Combat` (melee) or `Entity_Ai_Mob_Ranged` (+ projectile in its Attack command and `Hurtdict`), vis
  `Entity_Vis_Animator_Mob` + `Entity_Vis_Mob_Attack`, tags `Mob`, `Mobfighter`, `<Name>unit`, keep the template's
  `Distancewait` stat (spacing), `Sellable = false`, `Controllable = false`, `Costs` empty, `Drops` empty.
- **Upgrades → units**: the verified mod way is the building's params (`Puppethealth_Multi`, `Puppetspeed_Multi`):
  the unit brain applies them as modifiers when it starts (`Addmod(unit, "Health", x, Multi, 0)` then refill health),
  and the building re-applies to live units when it's upgraded. Name unit params so they don't collide with the
  building's own stats (`docs/09` #16). The game's way (unit-side upgrade objects in `Preupgrades`) also works for
  data, but a unit-side upgrade only does what some code reads from it: your unit brain must read
  `Master.Upgradelistserver`.
- **Unit level**: set `unit.Option_Defaultlevel = Master.Entitydata.Value.Level` after spawning and when the building
  levels up (read from the game; not yet used by a mod) if your unit stats have `Leveladd`.
- **Count changes**: an upgrade that raises `Units` simply lets the loop add more. To reduce, despawn the extras with
  reason "Pop" (no death effects, no kill).
- **When the building dies or is sold**: despawn its units (`OnDespawn` of the building) or let them live on (decide;
  the game's garages stop respawning but don't kill the dog).

## 4. What the units do (unit brain)

The stock combat AIs attack whatever comes inside the unit's `Range`, but they never walk out to find a fight. The unit
brain moves the unit with its `Move` command (`docs/05` §10) a few times a second (Mummer: every 0.4 s):

| Behaviour | How | Seen in |
|---|---|---|
| **Guard a spot** | home = a slot around the building (or the rally tile); a foe within `Engage` of the unit **and** within `Leash` of home → walk to it (`Goalobject = foe`); none → walk home | Mummer `Think()`, Boutman's fish (`Engagerange`), Fie's knights (`Defendhome`) |
| **Follow the vessel** | `Move.Goalobject = hero`, `Goalposition = -1`, re-issue when the hero is > 2 tiles away; fight what comes near on the way | Boutman's Fishfollow |
| **Go to a target the player picked** | a picker command (`Entity_Command_Itemselect_Base`, `docs/05` §7) sends the target's network id; the unit walks to it and does its job there | Argo's Nauts tuning a tower (`examples/code/Argo_08_NautBrain.cs`, `Argo_14_CrewSelect.cs`) |
| **Work** | walk to ore/tower and `Interact` it on a timer (`Hurt` with `Damageresource` for ore) | Smacky's miners, Sprigg's Sproutlings (stock miner AI) |
| **Ranged unit** | stop at `Range - 0.3` from the target instead of closing in; the ranged AI shoots | Mummer's Ballista Understudy |
| **Block the path** | give the unit a mob-blocker block (Sno Sno's `Mob_Snoble` block) so enemies stop at it | Mummer's Aegis Understudy |
| **Support** | stand next to towers and buff/heal them | Argo's Nauts, Smacky's engineers |

Rules: re-issue a Move only when the goal changed or the unit stopped (spamming restarts its path); stop guarding when
it strays past the leash (go home first); prefer the vessel's marked/priority targets; give each unit its own slot
around home so they don't stack on one tile.

## 5. Letting the player give orders

| Order | UI (client) | Server | Status |
|---|---|---|---|
| **Rally point** (Fie's Keep, Boutman's Relocate) | a building command whose `Commandfuncts` is a subclass of the game's `Entity_Command_Move`: press, click a ground tile. Its `Send()` takes `Game_Client_Mouseobject.Surfacemaster.Position.Value`, plays `Helper_Vfxtools.Oneshot("Movebordercommand", pos, Vector3.forward)` and sends `Command_ServerRpc(Link.Name.ToString(), "{\"Position\":{\"x\":..,\"y\":..,\"z\":..}}")` (the game's `Relocatedata` JSON; build it by hand, no JsonUtility) | the building's `Command("Rally", json)`: parse x/y/z, nearest `Surface` tile within 0.25 with no rally point already there (no range check), `Checkifstandable(tile, rallyBeing)`, `Helper_Navtools.Getpath(building tile, tile, ...)`.Valid; then Fie moves its rally-marker being there (`Helper_Gridtools.Moveblock(tile, rallyBeing)`) and points the Keep's own "Rally" command's `Defendhome.Goalposition` at it shown only to its owner (`Entity_Vis_Fierally_Clientvis` compares owner and local session) | read from the game's code; not yet used by a mod |
| **Send one unit to a target** | picker (`Entity_Command_Itemselect_Base`): Sell's border and prompt, sends `{"Ref":id}` | validate owner, team, range; set that unit's goal | verified (Argo crew, Mummer HOOK) |
| **Mode** (what units do / what gets made) | one-click `Entity_Command_Simple` showing the mode from `Link.Goalstring` | cycle the mode, sync with `Updatecommand(m, link, "", false, false)` | verified (Mummer COSTUME; Smacky's Convert) |
| **All units follow / rally to the vessel** | a vessel ability (keys 6/7) | every unit of the owner: follow the hero for N seconds | pattern (Boutman's Rally) |

Show the order: a marker effect at the rally tile for the owner (Vis + `OwnedLocally`), a line from the building to its
rally point while selected (the Keep draws one "Knightconnection" line in `Render`), a sound.

## 6. Units as buildables (Boutman-style, advanced)

The unit itself is placed and paid for like a building (a `Buildoption` whose Goalstring is the unit id, the unit
entity has `Costs`), guards the tile it was placed on, has its own upgrade tree and can be sold. Boutman's fish add a
knocked-out state (health 0 → "Dead" with `Deadarmor`, revived in a barrel) instead of despawning. None of the three mod
vessels has done this yet: build it on the unit-building recipe, give the unit brain the building's upgrade flow
(`MummerBuilding` works on any entity it creates the Upgrade commands on), and test selection, the upgrade panel,
selling and the journal page in game before relying on it.

## 7. Design rules for minion vessels

- **Difficulty** Intermediate or Advanced (units to manage).
- **Numbers**: base units are weak alone and strong in number or with upgrades: miner 5 HP, knight 25, dog 50, fish 50
  (+75/lvl); respawn 5-10 s; one unit per building at first, +1 at tier 2 ("Hire another Knight", "Mining Operation"),
  +1 more at tier 3-5. Total per building ≤ ~6.
- **Unit building cost**: the highest base costs (Red 60 for Keep and Garage); custom-resource units (Mummer's Thread)
  must be cheap enough to replace often (2/4/5 Thread).
- **Trees**: tier-2 options add a unit or toughen them; tier 3-4 change how they fight (weapons, blocking, ranged,
  stun); the capstone changes behaviour (Fie's Convoy: knights pursue on their own; Boutman's Bass: teleports and marks).
- **Agency**: at least one order the player gives (rally, pick, mode, follow).
- **Readability**: units look like the vessel's family (same palette and style); death and respawn are visible (a
  `Vfxend` death effect, a spawn puff and sound); the building shows its unit count through its stats panel.
- **Performance**: unit brains think every ~0.4 s, not every frame; foe queries use radius queries
  (`Entityarea`), never scans over all entities each frame.

## 8. Journal, online, tests

- **Journal**: units get their own pages through the kit (`ModKitJournalUnit` on the building: `Id`, `Title`, `Desc`,
  `Entity`, `Look` = first sprite frame); the building page lists them under RELATED ITEMS.
- **Online**: units are networked beings; all orders are commands to the host; per-owner state; rally markers either
  networked beings or owner-only Vis effects (`docs/07`).
- **Self-test sections** (Mummer's sections 3, 5, 6 are the model):
  - the building spawns `Units` units on its tile; they're visible to clients and have the right AI and stats;
  - a killed unit comes back after `Respawn` (± 1 s), and not before;
  - a `Units` upgrade adds one; a stat upgrade reaches **live and new** units;
  - units engage a foe spawned near home and hurt it; they return home afterwards; they don't pass the leash;
  - an order (rally / pick / mode) changes what they do, and a refused order (too far, blocked tile) warns the owner;
  - the building's journal page links each unit page.
