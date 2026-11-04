# 01 · How TDS works inside (what a vessel mod plugs into)

Everything here was read from the game's `Assembly-CSharp.dll` with Cecil (no decompiler on the PC) and confirmed in
game. Type and member names are the game's own; look any of them up in `reference/api_assembly_csharp.txt`, and read a
method's body with `tools/cecil/il.ps1` (`docs/11_TOOLING.md`). If the game updates, re-check what you rely on.

## 1. Mods

- A mod is a folder in `TDS\Mods\<Name>Mod` (`<Name>Mod.info` JSON + `Windows\<name>mod.assets` bundle), exported by
  the **TDS Mod Tools** (ModTool) Unity package from a Unity 2022.3.41f1 project, and enabled in the game's Mods menu
  (`"_isEnabled": true` in the .info).
- A prefab whose name contains **`Default`** (`<Name>Mod_Default.prefab`) is instantiated when the mod loads
  (`Bla_Mods.OnModLoaded`; `Activateprefabs` later does the same for `Mainmenu`, `Loading`, `World`, `Gameserver`,
  `Gameclient` prefabs). Ours carries:
  - a `Mods_Gamescript` component whose `Code` string is the whole mod's C#, compiled by the game at load with
    **RoslynCSharp (Roslyn 3.8, C# 9)**; the first type in the string is added to the prefab instance and is the entry
    point;
  - the `<Name>Assets_Library` child holding every sprite, model, effect and sound (`docs/03`).
- The game's compiler references a **fixed list** of assemblies (in `Build-<Name>Mod.ps1`). **Not available:**
  `System.Core` (no `HashSet`, no LINQ), `UnityEngine.JSONSerializeModule` (no `JsonUtility`), `System.IO`,
  `System.Reflection` (blocked by the code security settings), `UnityEngine.TextRenderingModule` (no `TextAnchor`).
- Being modded switches off leaderboard records (`Game_Endscreen.Validate`), for the whole lobby when one player is
  modded; stats, unlocks, medals and achievements still count (`23` §5). Lobbies are not blocked (no version or mod
  check, `19` §1), but every player must have the same mods enabled.
- ModTool's export puts **every prefab and ScriptableObject in the project** outside paths containing `/Editor/`,
  `/ModTool/` or `Packages/` into the bundle. Several mods in one project must be parked during an export (`docs/04`).

## 2. A vessel is a Hero

Internally vessels are **Heroes**: `Hero_Smacky`, `Hero_Snosno`, `Hero_Boutman`, `Hero_Fie`, `Hero_Buster`. The chain:

```
Tds_Masterdatabase.Instance.Items["Hero"][id]  ->  Heroentryobject      (menus: names, art, journal, which group to load)
   .Heroloads (Bl_Address_Group, Objectname == hero id)                  (the Addressables group with the vessel's data)
Bl_Address.Groups["Hero"] -> Beingobject  Hero_<Name>                    (a placeable "being": Block + Entity)
   .Entity -> Entityobject Hero_<Name>_Entity                            (AIs, visuals, stats, upgrades, costs, tags)
       .Defaultstat   Statproperties                                     (stat list)
       .Defaultvisual Visualproperties                                   (sprite or model, skins)
       .Upgrades / .Upgradesinit   Upgradeproperties                     (upgrade trees / upgrades applied at spawn)
```

**Heroentryobject** fields a vessel sets: `Heroloads`, `Towerdisplay` (the 4 buildable ids), `Displayname`,
`Displaysub` (title), `Displayskill` (difficulty), `Color`, `Icon` (12×12), `Worldsplash` (128×128), `Endscreenwinicon`
/ `Endscreenloseicon` (32×32), `Worldsprite` (Texture2DArray 16×16×48 for the boat), `Loadingsprite` /
`Landingsprite` / `Idlesprite` (sheets for loading and landing), `Log` (the journal page).

**Entityobject** fields: `Name`, `Display_Name`, `Display_Desc`, `Aiscripts`, `Visualscripts`, `Defaultstat`,
`Defaultvisual`, `Upgrades` (tier-2 roots), `Upgradesinit` (applied at spawn; holds the special-rule tooltip),
`Costs` (`List<Link_Resourceset>{Name, Amount}`), `Drops`, `Tags`, `Sellable`, `Controllable`.

Almost everything is a `Bl_ScriptableObject` (not `Logobject` or `Spriteobject`) with `name`, `Objectname` (the id the
network sends) and `Id` (unique string).

## 3. Loading and the global name table

- `Bl_Address.Groups[label]` → `List<Bl_Address.Loadlist>`; each list has `Items` (name → object), `Index`, `Uids`.
  Labels: `Hero`, `Mob` (towers, traps, units, enemies), `Upgrade`, `Projectile`, `Command`, `Vfx`, `Sound`, `Ui`,
  `Other`.
- `Bl_Address.Groupsname` (FixedString64Bytes name → object) and `Groupsids` are **global across all groups and
  mods**. This is how clients resolve a networked name. Two mods registering the same name = one silently wins.
- `Bl_Address.Load(folder)` returns early when `Bl_Address.Loadedfolders` already has the folder; a mod builds its own
  `Loadgroup` (`Loaded = true`), registers its objects in its own `Loadlist`s, and sets `Loadedfolders[heroId]`.
- **Asset groups are never unloaded.** A mod loads the template vessel's group (and any other vessel's group it borrows
  from, e.g. `Hero_Buster` for the walk-through trap block) at boot and can reference those objects all session, on
  every machine.
- `Bl_Address.Getload(label, name)` searches `Groups[label]`; it logs "Could not retrieve ... The GROUP exists, but item
  does not" when the item is missing.
- Names travel as `FixedString64Bytes`: keep ids ≤ 60 characters.
- English text is the localization key, and unknown keys display as-is, so English strings just work. An English
  string equal to a stock key gets the stock translation; a mod translates by writing `Bla_Localization.translations`
  on each machine after every language load (`21` §3).

## 4. Entities at run time

All entities (vessels, towers, units, enemies, projectiles' targets, ore, ground tiles) spawn from **one generic
network prefab**; `Entity_Master.Being` (synced) carries the being's Objectname and each client looks it up locally.

| `Entity_Master` member | What |
|---|---|
| `Beingserver` (`Link_Being`) | server-side: `Being`, `Entity`, `Currentblock` (`Genitem` tile), `Dead`, `Worldobject` |
| `Statsserver` | `Dictionary<string, Link_Stats>`; `Link_Stats.Value`, `Value_Calc`, `Value_Base` |
| `Commandlistserver` | `Dictionary<string, Link_Command>` (the entity's commands by link name) |
| `Commandlist` | synced `NetworkList<Link_Commandref>`: what clients see of each command (Goalstring, Goalvalue...) |
| `Upgradelistserver` / `Upgradelist` | applied upgrades (server dict / synced names) |
| `Taglistserver` / `Taglist` | tags (server list / synced list; clients only have `Taglist`) |
| `Entityposition`, `Entitydirection` | synced position (y = ground + 0.5 for towers, units, vessels) and facing |
| `Entitydata` | synced `{Owner (session id, long), Subowner (long[], compared by reference: assign a new array), Level, Team, Sellvalue, Upgradesellvalue}` (`19` §2) |
| `State`, `Skin`, `Beingref` | synced state name ("Idle", "Move", "Attack"...), skin name, a `Link_Beingref` to itself |
| `Radio` (`Entity_Master_Radio`) | RPCs: `Vis_ClientRpc(type, pos, number)`, `Vfxdebuffcheck`, `Vfxoneshotcheck`, `Vfxremovedebuff_ClientRpc`, `Uiwarning_ClientRpc` |
| `Remote` (`Entity_Master_Remote`) | `Command_ServerRpc(link, json)`: a client asks the server to run a command |
| `Client` (`Entity_Master_Client`) | per-machine visuals; event `Vis(type, pos, number)`; `Visobj` = the entity's "Vis" child |
| events | `Command(command, json, server)`, `Upgrade(Upgradeproperties)`, `Interact` (outgoing hits), `Oninteract` (incoming hits) |

- `Game_Entity.Instance`: `IsServer`, `Entityclient` (every entity on this machine), events `Onspawn(Entity_Master)`
  (fires synchronously inside `Spawn`, **host only**: a client has no spawn event and polls `Entityclient`, `19` §2) and
  `Ondespawn(Entity_Master, bool killed, string reason)`, `Despawn(being, killed, reason)`.
- `Game_Mapvars.Instance`: `Wave` (the Tide number, `NetworkVariable<int>`, subscribe to `OnValueChanged`), `Time_Wave`
  (seconds to the next Tide; the game advances only when it is **≤ 0**).
- Ground tiles are entities tagged `Surface`; a tile is 1 unit; `Helper_Gridtools.Getblock(pos)` needs the **exact**
  tile position (it rounds `(pos - Originadjust) * 100`).

## 5. AIs, brains and visual scripts

- The engine attaches AIs and visual scripts with `AddComponent(Type.GetType(listEntry.name))`: only the list entry's
  **name** matters. Any of the game's own classes can be attached to new data through a name-carrier object
  (`MummerUtil.Script("Entity_Ai_Mob_Combat")`).
- **A mod's own classes cannot be listed there** (they are not in the game's assembly; upgrade pruning also compares
  `GetType().ToString()`). Mod logic lives in **brains**: plain `MonoBehaviour`s the mod adds in `Game_Entity.Onspawn`
  on the server.
- Generic AIs every vessel can reuse: `Entity_Ai_Mob_General`, `Entity_Ai_Mob_Movement`, `Entity_Ai_Hero_Buildsell`,
  `Entity_Ai_Mob_Combat` (melee: targets, mines ore, sends Vis "Attack"), `Entity_Ai_Mob_Ranged` (fires the projectile
  named in `Commandlistserver["Attack"].Goalstring`, sends "Shoot"), `Entity_Ai_Tower_General`. Visual scripts:
  `Entity_Vis_Animator_Mob`, `Entity_Vis_Hero`, `Entity_Vis_Mob_Attack` (plays the Attack animation on "Attack"/"Shoot").
- Scripts named after a shipped vessel (`*_Smacky*`, `*_Snosno*`, `*_Fie*`, `*_Buster*`, `*_Boutman*`) hard-code that
  vessel's objects: strip them from cloned data. Strip the copied `Entity_Ai_General_Upgrade` too, and add one fresh
  carrier of it to each building that upgrades: it runs the whole upgrade panel, and the brain hears each upgrade
  through `Master.Upgrade` (`docs/05` §14).
- Enemies: `Entity_Ai_Enemy_Basic` walks to its parent core's Goal whenever it is Idle (`Approachgoal`), and
  `Helper_Entityai.Checkenemymelee` re-targets "Attack" every slow tick when something is in range. An enemy spawned
  without a parent throws a NullReferenceException every tick.

## 6. Stats, modifiers and counters

- `Statproperties.Defaultstatgroup {Name, Value, Leveladd, Style, Value_Type, Display_Type, Display_Exposed}`;
  `Leveladd` is added per level (Level = highest applied upgrade tier).
- Change stats only through modifiers: `Helper_Entitytools.Addmod(master, stat, value, Modtypes.Add | Multi | Pmulti |
  Base, duration)` (duration 0 = permanent). Slows and stuns are `Speed` Multi modifiers (-1 = held in place).
- A counter shown on the entity (like the archer's kills, Mummer's Thread) is a stat with `Style = Counter`
  added by `Helper_Entitytools.Addstat`, driven by a Base modifier (`MummerUtil.Counter/SetCounter`).
- Common stats: Health, Healtharmor, Speed, Damage, Range, Attackspeed, Projspeed, Units, Respawn, Buildrange,
  Sellrate, Healthresource (ore).

## 7. Upgrades

- `Upgradeproperties {Priority (tier), Costs, Defaultparam(s), Ent_Visual, Upgrades (next options), Aiscripts,
  Display_Name, Display_Desc}`; ids `<Being>_<Key><Tier>_Upgradeobject`.
- What the player may buy: `Entity.Upgrades` plus the `.Upgrades` of every applied upgrade whose Priority equals the
  current Level. `Entity_Ai_General_Upgrade` handles the "Upgrade" command: throttle 0.05 s, `Helper_Resources
  .Checkresources/Useresources(costs, owner, scale)` (scale = the "Upgrade" command's Goalvalue),
  `Helper_Entitytools.Upgrade(master, up, Forced|Normal, null)`, Vfx "Upgradeborder", `Activeplayerdata.Upgraded++`.
- **The engine never applies upgrade params itself**: each vessel's own AI reads them (`Stat_Add`, `Stat_Multi`,
  `Stat_Pmulti`, `X_Base`, `X_Max`, `X_Percent`, `X_Duration`). A mod's building brain does the same (`docs/05`).
- An upgrade's `Ent_Visual` replaces the building's model; skins (`Visualproperties.Skinoverrides`) switch variants.
- Buffs/debuffs in the base game are `Effect_*` upgrades applied to the target, each with an effect AI.

## 8. Commands and buttons

- `Commandobject` (label `Command`): `Style` (Button), `Display_Name`, `Display_Desc`, `Color`, `Keybind` (e.g.
  `["6","7"]`), `Keybindstate` (`Onpressed` / `Onrelease`), `Priority`, `Icon` (icon name), `Commandfuncts` (a list of
  `Entity_Command_Base` **instances**: mod subclasses work here, unlike AIs).
- On an entity: `Helper_Entitytools.Createcommand(master, commandObjectName, link, ...)` → `Link_Command {Goalobject,
  Goalposition, Goalvalue, Goalstring, Controllable, Statechange}`; `Updatecommand(master, link, json, server, run)`
  syncs it to `Commandlist` and, **if `run`**, invokes `Master.Command` handlers (and `Action` on a state change).
- The vessel's build buttons are commands `Buildoption1..4` with `Goalstring` = buildable id, `Goalvalue` 1.
- Built-in UI classes: `Entity_Command_Simple` (one-click button), `Entity_Command_Itemselect_Base` (point-and-click
  picker, as Sell and Fie's Reroute use), `Entity_Command_Move`.
- Building: the client sends `Command_ServerRpc("Build", {"Tower","Position":{x,y,z},"Orientation","Owner"})`.

## 9. Damage, healing, ore, projectiles

- `Helper_Entitytools.Interact(from, to, "Hurt", data)`; `Helper_Damagetools.Dodamage` reads `Data["Damage"] ×
  Data["Damagemulti"]`. `Healtharmor` makes each hit count as its
  `Damagemulti` (1 by default), whatever its Damage (`09` #39). Healing: type `"Heal"`.
- Ore (`Healthresource`): each hit removes `clamp(Damage, 0, 1) + Data["Damageresource"]`; it pays out (aura) when
  emptied. Heroes mine with their normal attack.
- Projectiles: `Projectileobject` (clone `Tower_Smackyarcher_Arrow` or the Heavy Bow's arrow for pierce);
  `Helper_Damagetools.Projectile` copies the projectile's `Defaultparams` per shot and lays the shot's params over the
  copy; the shot's `Data` dictionary is kept by reference, so give each shot its own (copy it before editing).

## 10. Visuals

- `Visualproperties.Default` is an `Entry` **struct** (copy, modify, assign back): `Style` (Sprite / Model / Both),
  `Spriteobject`, `Visprefab`, `Displayname`, `Displaycolor`, `Additionalprefabs`, `Additionalvisualscripts`, `Vfxend`
  (death effect), `Sfxstart`/`Sfxend` (stock sound keys played on every machine at spawn and at a real death, `21`
  §2.2), `Rootvisualproperty`. Skins: `Skinoverride` list + `Skinoverrides` dict, selected by `Master.Skin`.
- `Spriteobject`: `Items` (name → `Spritesinlist {Name, Sprites (variants), Loop, Reset, Priority, Speed, Spriteto,
  Dumpsplit}`), `Itemslist`, `Defaultspeedmulti`, `Defaultfolder`, `Preview`. Variant 0-1 face front, 2-3 face back.
  Custom animations play with `Entity_Vis_Animator_Base.Animplay(name, Game_Sprite.Animationtype.Force)`.
- Effects by name: `Radio.Vfxoneshotcheck(name, pos, dir)`; debuffs `Radio.Vfxdebuffcheck(name, target.Beingref.Value,
  seconds)` → `Helper_Vfxtools.Debuff` instantiates `Getload("Vfx", name)` under the target's `Vis` child (local
  position 0), adds `Vfx_Debuff` (destroys it after the duration); sending it again refreshes the duration. All `*check`
  RPCs share one budget (`Game_Mapvars.Clientsendstack`): a burst of 50, refilled by 4 per FixedUpdate online (10 solo);
  RPCs over it are dropped silently.
- Icons: `Tds_Masterdatabase` folder `Icons` → `Iconobject` `Icon_Types` / `Icon_Stats`; `Helper_Uitools` copies them
  into a private table **on the first `Geticon` call and never again**.
- Sounds: the game's player is internal and its table is built once, before mods load, so mod clips play on their
  own `AudioSource`s routed to the game's mixer group (the one the "Upgrade" sound uses, `General_Main`). Stock keys
  play from mod code (`Helper_Vfxtools.Playsound`, host `Tds_Multiplayer_Radio.Instance.Sfx_ClientRpc`), and an
  unknown key throws (`21` §2).

## 11. Menus, boat, save

- Vessel select is on the boat: `Macro_Worldvisual.Populatecharselect` lays heroes onto `Boat/Charselect/Spots` children
  (6 shipped spots, all named "Spot"), one per hero, and marks heroes without `Unlock/Unlock_Hero_<Objectname>` as
  locked (`Macro_Characterselect_Click {Character, Locked}`); the lock only blocks the mouse (`23` §3.3).
- A click on the boat is one `Physics.Raycast` from `Camera.main` on the UI layer (`Boatclick`), and the nearest
  vessel box wins, so a vessel whose box its neighbours cover can't be picked with the mouse (the Left/Right vessel
  arrows still cycle every vessel). The select camera looks straight across the deck, level with the vessels, so
  vessels at the same place along the deck hide each other whatever their depth; the game's box (0.4 cube, centre 0.2
  below the spot) runs from the eyes to below the feet. Kit 1.4 `ModKitBoat` lays crowded boats out so every box
  shows.
- `Macro_Main.Selectcharacter(id)` saves `Player_Heroselect`; at startup the game calls `Selectcharacter` with the
  saved value. The host's `Changehero_ServerRpc` only checks the hero exists (no unlock check).
- Journal: `Logobject` pages (`Elements` = `[SerializeReference]` Logitems), `Heroentryobject.Log`, opened with
  `Bl_Menus.Changemenu("Journal")` + `Loghandler.Log_Current = page` (`docs/06`).

## 12. Shipped vessels at a glance

| Vessel | Title ▪ difficulty | Buildables | Attack | Special rule |
|---|---|---|---|---|
| Smacky | Good Ol' Fella ▪ Basic | Archer Tower, Bomb Tower, Mining Hut, Wall | melee (Mob_Combat) | none |
| Sno Sno | Destroyer of Worlds ▪ Intermediate | Sno Cannon, Sno Box, Ice Brick, Heater | ranged (Mob_Ranged) | Snoborne |
| Boutman | Fisher Extraordinaire ▪ Advanced | Fish Barrel, Boutboat, Bouthouse (+ fish) | melee (Mob_Melee) | The Bout Way |
| Fie | From the Walls ▪ Intermediate | Silo Pipes, Rest Stop, Knight's Keep, The Wall | melee | none |
| Buster | A Very Fast Dog ▪ Advanced | Checkpoint (trap), Pedestal, Speed Pad (trap), Garage | melee + Dash | Good Dog |

Full conventions: `reference/VESSEL_SURVEY.md`; raw data (stats, trees, costs, params of every shipped buildable):
`reference/vessel_data_survey.txt`. Mod vessels so far: Sprigg (plant, ranged, Intermediate), Argo (diver, melee,
Intermediate), Mummer (costume, melee with reach, Advanced): see `examples/`.
