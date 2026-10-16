# 05 · Code cookbook

The runtime code is one C# 9 string compiled by the game. Snippets below use the template's names (`Mummer...`); after
scaffolding they carry your vessel's name. Constraints: no `System.IO`, `System.Reflection`, `HashSet`, LINQ,
`JsonUtility`; no mod class in an AI list; everything that changes the game runs on the server
(`MummerUtil.IsServer`).

## 0. Modules: what to keep, what to replace

| File | Generic (keep, rename) | Vessel-specific (replace) |
|---|---|---|
| `00_<Name>Mod.cs` | boot order, Update/LateUpdate wiring | `ExtraGroups` you borrow from |
| `01_Util.cs` | all helpers | add your own |
| `02_Data.cs` | the `Build`/`Upg`/`U(...)` structure, `WarnText` scheme | every name, number, text |
| `03_Assets.cs` | library access, `Frames`, `Anim`, `Worldsprite`, `Pixel`, `ApplyTowerMaterial`, `Play`, `Effect` | sheet names in `Hero()`/`Puppet()`, icon drawings |
| `04_Content.cs` | `Build` skeleton, `Register`, `RegisterVfx`, `Strip`, `Stats`, `TowerVisual`, `BuildTower`, `Tooltip`, `BuildHero`, `BuildEntry`, icons | `BuildPuppet`, `BuildProjectile`, `BuildCommands` details, templates per building |
| `05_Server.cs` | Onspawn/Ondespawn/Wave hooks, per-player state, brain attachment | Thread, Marks, FoeFell, families |
| `06_HeroBrain.cs` | build options, ability command + cooldown button | Hook, Reel |
| `07_Building.cs` | `MummerBuilding` (mirrors the game's upgrade flow) | `MummerLoomBrain` |
| `08_Stage.cs`, `09_Puppet.cs` | patterns: spawning units, holding foes, traps | everything |
| `10_Client.cs` | Vis subscription, owner warnings, `Animate`, `Run`, thread lines | the handlers |
| `11_Commands.cs` | picker (`Itemselect`) and stateful button classes | texts, validity rules |
| `12_Journal.cs` | the spec fill-in | labels, units |
| `97_Shots.cs`, `99_SelfTest.cs` | captures, match driver, helpers, generic checks | mechanic sections |

## 1. Boot (00)

```csharp
IEnumerator Boot()
{
    // 1. wait for the template vessel's entry in the master database
    while ((template = MummerUtil.Heroentry(MummerData.TemplateHero)) == null) yield return new WaitForSecondsRealtime(0.5f);
    // 2. the asset library rides in the mod prefab
    if (!MummerAssets.Find()) yield break;
    // 3. icons NOW: the game's icon table freezes on the first Geticon call
    MummerContent.AddIcons();
    // 4. load the template group (+ groups borrowed from: Buster's pad, Sno Sno's blocker...) and wait for .Loaded
    // 5. build and register everything, then the journal
    MummerContent.Build(template);
    // 6. 8 s later, after every mod had its turn: fall back to stock icons if ours missed the table
    yield return new WaitForSecondsRealtime(8f); MummerContent.CheckIcons();
}
void Update()     { MummerServer.Update(); MummerClient.Update(); MummerJournal.Tick(); }
void LateUpdate() { ModKitBoat.LateUpdate(MummerData.Hero); }
```

`Start()` detaches the mod object (`SetParent(null)`, `DontDestroyOnLoad`): it lives for the whole session.

## 2. Registering data (04)

The mod makes its own `Bl_Address.Loadgroup` with one `Loadlist` per label, adds each list to
`Bl_Address.Groups[label]`, and registers every object:

```csharp
static void Register(string label, Bl_ScriptableObject obj)
{
    Bl_Address.Loadlist list = lists[label];
    list.Items[obj.Objectname] = obj;
    if (!list.Index.Contains(obj)) list.Index.Add(obj);
    FixedString64Bytes id = new FixedString64Bytes(obj.Id);
    list.Uids[id] = obj;
    obj.Loadsource = MummerData.Hero;
    Bl_Address.Groupsname[new FixedString64Bytes(obj.Objectname)] = obj;   // global: names must be unique
    Bl_Address.Groupsids[id] = obj;
}
// at the end of Build:
Bl_Address.Loadedfolders[MummerData.Hero] = group;           // the game won't try to load our "folder"
MummerUtil.Folder("Hero")[MummerData.Hero] = Entry;          // the vessel appears in menus and on the boat
```

Register the being, its entity, its `Statproperties`, every `Visualproperties`, every upgrade and upgrade visual,
commands, projectiles. Effects go in the `Vfx` list by GameObject name (`RegisterVfx`).

**Cloning** (`MummerUtil.Clone(source, name)`) = `Object.Instantiate` + `Retag`: `name = Objectname = id-string`,
`Id = lower(name) + "-<vessel>"`. Never edit a template object in place: clone it. `Visualproperties.Entry` is a
**struct**: copy, edit, assign back. Strip vessel-specific scripts from cloned AI/vis lists (`Strip`: names containing
Smacky/Snosno/Boutman/Fie/Buster, and `Entity_Ai_General_Upgrade`).

## 3. Stats (04)

`Stats(template, name, new[] { "Health=25+25", "Damage=1", "Range=2.5" })` builds a `Statproperties` with exactly
these stats (`+x` = Leveladd), copying display style from the template's same-named stat (`Display_Exposed` off: shown to the owner's team only). List only
the stats the entity uses: leftovers like Projcount mean nothing on your building.

## 4. Buildables and upgrade trees (04: `BuildTower`)

1. Load the template being (`Tower_Smackyhut`, `Tower_Smackyarcher`, `Trap_Busterpad`...), clone being + entity.
2. Entity: name, display texts, `Aiscripts = Strip(...)` (keeps `Entity_Ai_Tower_General`), `Costs`, empty
   `Upgradesinit`, `Sellable`, `Controllable`, tags (+ family tag), stats, visual (`TowerVisual`: model from the
   library, optional skins).
3. Upgrades: clone a template upgrade of the same tier, set name/desc/tier/costs, **empty `Aiscripts`**, own
   `Ent_Visual`, params from the data string (`"Range_Add=1.5;Scraprange_Add=2"`); link `Upgrades` (next options) from
   the curated `Next` keys; `entity.Upgrades` = the tier-2 roots.
4. Register everything; remember `UpgradeDefs[upgradeId]` so the brain knows each upgrade's key.

## 5. The vessel (04: `BuildHero`, `BuildEntry`, `Tooltip`)

- Clone the template hero (Smacky). AIs: `Strip(template)` + `Entity_Ai_Mob_Combat` for melee; for ranged use
  `Entity_Ai_Mob_Ranged` **instead** and arm it in the hero brain:

  ```csharp
  // examples/code/Sprigg_06_HeroBrain.cs: name the projectile the stock ranged AI fires, and let its damage tag hurt
  Master.Commandlistserver["Attack"].Goalstring = SpriggData.HeroSeed;        // a registered Projectileobject
  Entity_Ai_Mob_Ranged ranged = Master.GetComponent<Entity_Ai_Mob_Ranged>();
  if (ranged != null && ranged.Hurtdict != null) ranged.Hurtdict["Seed"] = 1f;
  ```
- Visual scripts: keep the template's + `Entity_Vis_Mob_Attack`. Visual: the Spriteobject from `MummerAssets.Hero`.
- `Upgradesinit` = `[Tooltip()]`: a cloned upgrade, tier -3, no costs/params/AIs/visual, name = rule name, desc =
  rule text.
- `BuildEntry`: `Instantiate(template entry)`, retag, its own `Heroloads` group copy, `Towerdisplay`, names, `Color`,
  `Icon`/`Worldsplash`/end icons/`Worldsprite`/`Loadingsprite`/`Landingsprite`/`Idlesprite` from the library, and
  `Log` (set by the journal). Add it to the master database's `Hero` folder.

## 6. Brains: attaching logic (05)

```csharp
// once per Game_Entity instance (a new one per match)
ge.Onspawn += OnSpawn; ge.Ondespawn += OnDespawn;
mapvars.Wave.OnValueChanged += OnWave;                       // (int old, int now) - a new Tide

static void OnSpawn(Entity_Master master)
{
    if (!MummerUtil.IsServer || master == null || master.Beingserver == null || master.Beingserver.Being == null) return;
    string being = master.Beingserver.Being.Objectname;
    if (being == MummerData.Hero) master.gameObject.AddComponent<MummerHeroBrain>().Master = master;
    else if (MummerData.BuildFor(being) != null) { /* AddComponent the building's brain, set Def */ }
}
```

- `Onspawn` fires **synchronously inside** `Helper_Entitytools.Spawn`: to tell a unit which building made it, set a
  static "pending" field just before `Spawn` and read it in `OnSpawn` (`MummerServer.PendingStage`).
- A brain starts in its `Update` once `Master.Statsserver.Count > 0 && Master.Commandlistserver != null &&
  Master.Radio != null` (stats are empty on the spawn frame), then runs `Begin()` once and `Tick()` each frame,
  wrapped in try/catch that logs and disables the brain instead of spamming.
- Per-player state (custom resources, unlocks) lives in a dictionary keyed by `MummerUtil.Owner(master)` (the session
  id), so several players can play the same vessel.
- `OnDespawn(master, killed, reason)`: `killed` is false for sells and test cleanup. Read a foe's base health from its
  data (`MummerUtil.BaseHealth`), not from its stats (0 by then).

## 7. Build options and ability buttons (06, 11)

```csharp
// the vessel's four build buttons
for (int i = 0; i < 4; i++)
{
    Link_Command c = Helper_Entitytools.Createcommand(Master, "Command_Buildoption" + (i + 1), "Buildoption" + (i + 1), false, true);
    c.Goalstring = MummerData.Towers[i]; c.Goalvalue = 1f;
    Helper_Entitytools.Updatecommand(Master, "Buildoption" + (i + 1), "", true, true);
}
// the ability: a registered Commandobject (cloned from Command_Tower_Smackyhut_Convert, Style Button, keys 6/7,
// Onpressed, priority 6, Commandfuncts = { new MummerHookSelect() }, Icon = "Mummer_Hook")
hookCommand = Helper_Entitytools.Createcommand(Master, MummerData.HookCommand, MummerData.HookLink, false, true);
Helper_Entitytools.Updatecommand(Master, MummerData.HookLink, "", true, true);
Master.Command += OnCommand;     // (string command, string json, bool server)
```

**Cooldown on the button**: the server sets `hookCommand.Goalstring = "Off"` and calls
`Updatecommand(Master, link, "", false, false)`: that syncs the command to every client **without running it**
(`run = true` would invoke `OnCommand` again). The button class reads `Link.Goalstring` in `Render` and greys its
`Icon` child. A stateful building button (COSTUME) does the same with `Goalstring`/`Goalvalue` for its label.

**Picker** (`Entity_Command_Itemselect_Base`, like Sell): override `Visobjs` (borders `Sellborder`/`Sellfromborder`
and the prompt `Ui_Command_Sell`, never null), `Running` (hover feedback from `Game_Client_Mouseobject.Master`),
`Send` (`Remote.Command_ServerRpc(Link.Name.ToString(), "{\"Ref\":" + target.NetworkObjectId + "}")`; on the hotkey,
send "" to mean "nearest"), `Finish`. The server resolves the id (`MummerUtil.ById`) and validates owner, team, range,
cooldown, cost. Clients only have `Taglist` (not `Taglistserver`).

## 8. Effects everyone sees

| Need | Server call | Client side |
|---|---|---|
| a one-off effect/sound/animation | `master.Radio.Vis_ClientRpc("Mummer_Hook", pos, number)` | `MummerClient` subscribes to `master.Client.Vis` of your entities and switches on the type |
| a status look on a target | `radio.Vfxdebuffcheck("Debuff_Mummermarked", target.Beingref.Value, seconds)` | the game attaches the registered prefab under the target's `Vis` child and removes it after `seconds` |
| remove it early | `radio.Vfxremovedebuff_ClientRpc(name, target.Beingref.Value)` | |
| a stock effect | `radio.Vfxoneshotcheck("Upgradeborder", pos, Vector3.forward)` | |
| owner-only message | `Vis(master, "Mummer_Warn", pos, code + value * 100)` | client: if `OwnedLocally(master)` → `Helper_Uitools.Warning(text, "", 2f)` |
| a persistent look | `MummerUtil.Skin(master, "Ballista")` | skins in `Visualproperties.Skinoverrides` |

**Use `target.Beingref.Value`**, never `target.Beingserver.Beingrefparallel` (a copy made before the spawn set its
network reference: clients can't find the target and nothing shows).

`Vfxdebuffcheck` and `Vfxoneshotcheck` share a throttle: in a busy online fight they are dropped silently (`19` §4).
Keep them for flavour; a look the player must see goes through `Vis`, a skin or a networked being. A message to one
machine (not one owner's vessel) is `Radio.Pongload_ClientRpc` with a target client id (`19` §6 rule 4).

`number` in Vis is a float: entity network ids fit exactly below 2^24, so `(float)foe.NetworkObjectId` round-trips.
Animations: `Entity_Vis_Animator_Base.Animplay("Hook", Game_Sprite.Animationtype.Force)` on the entity's animators
(`Animplaysoft` gets overridden by Idle/Move immediately). The stock attack AIs send "Attack" (melee) and "Shoot"
(ranged): hook your attack sound there.

## 9. Modifiers, counters, damage

```csharp
Helper_Entitytools.Addmod(foe, "Speed", -1f, Helper_Entitytools.Modtypes.Multi, seconds);      // held in place
Helper_Entitytools.Addmod(foe, "Attackspeed", -0.95f, Helper_Entitytools.Modtypes.Multi, s);   // can't attack
Link_Modifier m = Helper_Entitytools.Addmod(tower, "Range", 1.5f, Helper_Entitytools.Modtypes.Add, 0f); // permanent
m.Remove.Invoke();                                                                             // undo early
MummerUtil.Counter(hero, "Thread", 4); MummerUtil.SetCounter(hero, mod, "Thread", 7);          // shown while selected
MummerUtil.Hurt(from, to, 5f);   // Interact "Hurt" with Damage 5, Damagemulti 1
MummerUtil.Heal(from, to, 3f);   // Interact "Heal"
// ore: strike for 1 + bonus; it pays aura to the team when emptied (like the Sno Box, Mummer's Loom)
Helper_Entitytools.Interact(tower, ore, "Hurt", new Dictionary<string, float> { { "Damage", 1f }, { "Damagemulti", 1f }, { "Damageresource", bonus } });
```

To scale damage an entity takes from every source (Argo's Pinged/Bulkhead), put your handler **first** in its
`Oninteract` chain and re-check it often (`Updateinteract` rebuilds the chain on every upgrade/effect); copy
`hit.Data` before changing `Damagemulti` (`examples/code/Argo_09_Hooks.cs`).

## 10. Units (08, 09)

The full guide to unit-based vessels (how the game's own unit buildings work, orders, rally points, design rules,
tests) is `docs/12_MINION_VESSELS.md`. The basics:

```csharp
Link_Spawn spawn = Helper_Entitytools.Createlinkspawnparent(Master.Beingserver);  // owner/team from the building
spawn.Genadd = Master.Beingserver.Currentblock;
spawn.Spawnstyle = Link_Spawn.Spawnstyles.Forced;
spawn.Being = unitBeing;
MummerServer.PendingStage = this;                         // read by OnSpawn
Link_Being b = Helper_Entitytools.Spawn(spawn, Master.Position.Value, Master.Beingserver, true, null);
MummerServer.PendingStage = null;
```

Unit data: clone `Mob_Smackyminer`; AIs `Entity_Ai_Mob_General` + `Entity_Ai_Mob_Movement` + `Entity_Ai_Mob_Combat`
(or `_Ranged` with a projectile in its Attack command and `Hurtdict`); vis `Entity_Vis_Animator_Mob` +
`Entity_Vis_Mob_Attack`; tags `Mob`, `Mobfighter`, `<Name>unit`; keep the template's `Distancewait`.

**Moving a unit** (how the Mining Hut sends miners and Boutman's fish engage):

```csharp
Link_Command move = unit.Commandlistserver["Move"];
move.Goalobject = target;                                  // or null and:
move.Goalposition = target != null ? Vector3.one * -1f : spot;
move.Goalvalue = 0f;
move.Goalstring = "Idle";                                  // what to do on arrival
Helper_Entitytools.Updatecommand(unit, "Move", "", true, true);
```

The stock combat AI attacks what comes in range by itself; the brain only decides where to walk (leash, engage,
return). Don't re-issue a Move every frame unless needed.

## 11. Moving an enemy (HOOK's reel)

There is no knockback API. A foe can be moved with **its own Move command** (path stays valid, its AI resumes):
pick a tile (below), give the foe a temporary `Speed` Multi boost, set `Move.Goalposition` to the tile and
`Updatecommand(foe, "Move", "", true, true)`. **Its combat AI re-targets every slow tick**, so re-assert the Move every
frame until it arrives or the window ends (`MummerHeroBrain.KeepReeling`), then root it. Never teleport or change a
foe's team.

## 12. Tiles and positions

```csharp
// the walkable tile at a point: snap to the tile centre, try the same level, one down, one up
Entity_Master s = Helper_Entityquerytools.Entitynearest(new Vector3(Mathf.Round(p.x), Mathf.Round(p.y) + dy, Mathf.Round(p.z)), 0.5f, MummerUtil.Tagged("Surface"));
Genitem tile = s != null && s.Beingserver != null ? s.Beingserver.Currentblock : null;   // no ?. on Unity objects
Genitem stand = Helper_Gridtools.Checkifstandable(tile, foe.Beingserver);         // may throw: try/catch
Link_Path path = Helper_Navtools.Getpath(foe.Beingserver.Currentblock, stand, foe.Beingserver, false); // path.Valid, path.Length
bool canBuild = Helper_Gridtools.Checkifplaceable(tile, being, 0, team);
```

`Helper_Gridtools.Getblock(pos)` only finds a tile at its **exact** position. `Entityposition.y` = ground + 0.5.
Foes near a point: `MummerUtil.FoesNear(from, pos, radius)` (tag `Enemy`, not `Core`/`Enemycore`, other team).

## 13. Projectiles

Clone `Tower_Smackyarcher_Arrow` (or the Heavy Bow arrow for native pierce), swap `Vis` for your model, register. Fire
from a brain with `Helper_Damagetools.Projectile(master, projectileId, from, projdata, "Hurt")`, passing the params each
shot needs (`Xvelocity`, `Yvelocity`, `Zvelocity`, `Gravity`, `Pierce`, `Lifetime`, `Interact`, `Collision`): the game
copies the projectile's `Defaultparams` per shot and overrides the keys you pass; give each shot a fresh `Data`
dictionary, which is kept by reference (`examples/code/Argo_07_TowerBrain.cs`, `Fire`).

## 14. Building brains and upgrades (07)

`MummerBuilding` mirrors `Entity_Ai_General_Upgrade` so the game's upgrade panel works unchanged: it creates the
`Upgrade` and `Upgradeoption1..4` commands, handles "Upgrade" JSON (`{"Upgrade":name,"Upgrader":session}`, parsed by
hand), checks availability (roots + next of upgrades at the current Level), throttles, checks and spends resources,
applies with `Helper_Entitytools.Upgrade(..., Forced if server-issued else Normal)`, plays `Upgradeborder` and counts
the owner's `Upgraded` stat.

`OnUpgrade`: params on stats the building **has** (`Range_Add`, `Attackspeed_Multi`, `Health_Pmulti`...) become
modifiers; everything else is stored and read by the subclass with `Param("Key_Base", fallback)` / `Has("Key")`.
Subclasses implement `Tick()`, and optionally `Started2()`, `OnTide(int)`, `Command(...)`, `Upgraded(def)`.
Upgrades are applied again on load (`Upgradelistserver`) so a rebuilt brain catches up.

## 15. Networked leftovers and ground effects

Like Perma-Wall's stump and Sno Sno's snow: a being (cloned from `Trap_Smackywall_Perma` or `Effect_Smackywall_Bramble`)
spawned on the tile, Forced, same owner/team (`examples/code/Sprigg_01_Util.cs`, `SpawnOnTile`). The engine syncs it to everyone; find it
later with `Helper_Entityai.Getfirstonblock(tile, entityName)`.

## 16. Reading enemies

Tell enemy families apart by their AI component (`foe.GetComponent<Entity_Ai_Enemy_Ballista>() != null`), with the
being name as a fallback. Bosses: base Health ≥ ~600. Enemy ids: `13` §1 and the game's `Entity_Ai_Enemy_*`
classes in the API dump; their stats: the `ENEMY` lines of `reference/roguelab_round1.txt`.

## 17. JSON, numbers, misc

- Command payloads are flat JSON: `MummerUtil.JsonString(json, "Upgrade")`, `JsonNumber(json, "Upgrader")`.
- Parse numbers with `CultureInfo.InvariantCulture` (`MummerUtil.F`).
- Delays: `MummerUtil.Later(host, seconds, action)` (a coroutine on an active MonoBehaviour).
- Logging: `MummerLog.Info/Warn/Error` → `[Mummer] ...` in Player.log. Log once per event kind, not per frame.
- Don't name a class and a method the same (`Mark` vs `Mark()` broke a compile); avoid generic class names (the kit
  gets a per-mod prefix; give your own classes one too).
