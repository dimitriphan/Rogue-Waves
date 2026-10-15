# 22 · Making a new map (prefab layout, registration, what a mod map keeps)

Expansion row 4, the highest-risk piece. `16` covers what happens **inside** a loaded map (grid, paths, map objects,
nodes); this chapter covers what a new map must **be** for the game to load it, and how a pack registers one.
Everything here was **read from the game's code** (decompiled as in `13` §11) and the TDS Mod Tools package on
2026-10-14 unless it says otherwise. **No mod map has been loaded yet**: §7 is the PC list, starting with a dump of a
stock map's hierarchy, which settles every "inferred" below.

Read `16` §1-3, `15` §2, §10, §14.4, §17 and the repository's `docs/mode/SEA_CHART.md` first.

## 1. The chain from a chart node to the prefab

| Step | Object | Found by | Where |
| --- | --- | --- | --- |
| Node | `Macro_Maplink_Add.Gameplan` | GameObject name (`Selectmap_ServerRpc`) | Tds_Multiplayer_Remote.cs:600-672 |
| Plan | `Gameplandatabaseobject` (Map, Mode, Soundtrack, Par, Record, Prediscover, medals) | `Items["Gameplan"]` by name | Gameplandatabaseobject.cs:10-40 |
| Entry | `Mapentryobject` (Display_*, Difficulty, `Maploads`, `Subloads`) | `Items["Map"]` by name | Mapentryobject.cs:6-20 |
| Groups | `Maploads` + every `Subloads` group | `Bl_Address.Load`/`Waitload` by `Objectname` | Game_Init.cs:109-113 |
| Map | `Mapobject` (`Lighting`, `Atmosphere`, `Mapdata`, `Mapvis`) | `Getload("Map", name)` | Mapobject.cs:8-14 |

- Only the plan's map name crosses the network (`Tds_Multiplayerinfo.Map`, 61 bytes); every machine resolves it itself,
  so **the entry, its groups and the same `Mapobject` must exist on every machine** (Game_Client_Init.cs:101-113).
- **One name, three places:** `Game_Init` loads the `Mapobject` by the entry's name (:114), `Game_Grid` by the
  `Maploads` group's name (Game_Grid.cs:174). Stock maps use one string for the entry, its group and its `Mapobject`
  (`Map_Thehunt`); a mod map does the same, and sets the asset `.name` and `Objectname` alike.
- A map group can carry its own beings (Map_Thehunt's group holds 5 Mobs, 3 Upgrades, 4 Vfx): a map reusing a
  map-special being lists that map's group in `Subloads`.

## 2. The `Mapdata` prefab

`Generatemap` instantiates `Mapdata` at the world origin with identity rotation (Game_Grid.cs:175-178): the root's
own transform is ignored and **child positions are world positions**. It runs on the host only; clients build no grid.

| Child | Required | Found with | What happens | Where |
| --- | --- | --- | --- | --- |
| `Hit` | yes | first `FindDeepChild("Hit")` | host: kinematic Rigidbody, layer 6 on **descendants**, renderers destroyed | Game_Grid.cs:179-193 |
| `Maxbound` | yes, a **direct** child | `FindDeepChild`, then destroyed with `Find` | grid origin and size | :194-201, 274 |
| `Entities` | yes, direct child | `Find` | each child spawned **by its name** as a Mob, then the generation passes | :275-321 |
| `Object` | no | `Find` | each child spawned by name on its node | :656-735 |
| `Mode` | no | `Find` | the mode's named spots | Game_Rounds.cs:69-192 |
| anything else | no | — | kept on the host only (clients delete it) | Game_Client_Init.cs:130-167 |

### 2.1 Grid geometry

- `Origin = Maxbound.position`; size in tiles = `(int)Maxbound.localScale` per axis (a scaled parent is ignored).
  **Tile (i, j, k)'s centre is `Maxbound.position + (i + 0.5, j + 0.5, k + 0.5)`**: `Maxbound` marks the box's
  minimum corner (Game_Grid.cs:195-201; Helper_Gridtools.cs:89-133). `16` §1 omits this; every hand-placed block
  depends on it.
- Anything off those centres or outside the box becomes an `Offgrid` node (slow lookups, Game_Grid.cs:946-977). A
  ground block's surface node is one tile above it, so leave a free layer above the highest ground.
- Size costs time: node merging compares every node with every other and A* keeps its sets in lists. Stay near a stock
  map's node count; a `Generatemap` slow enough to pass 120 s sends everyone to the main menu (`19` §3).

### 2.2 `Entities`: the terrain

- Each child spawns `Forced` as `Getload("Mob", <child name>)`, **without** `Autogenadd` (Game_Grid.cs:281-288). A
  child whose name is not a loaded Mob with a `Block` (a typo, Unity's "Block_Ground (1)") throws inside
  `Generatemap` and the map never loads.
- Then 10 passes run each block's `Functs`: `Block_Ground` adds its node and spawns a `Block_Surface` one tile up;
  `Block_Surface` adds `Block_Stair` where a one-tile rise is free; `Block_Add` gives any other being a node; each funct
  links its own neighbours in pass 5; one-way and two-way blocks trim links (Block_Funct_*.cs). Nodes closer than 0.5
  merge.
- **Then `Despawnanonblock` removes every being that has no node** (Game_Grid.cs:655; Game_Entity.cs:435-446).
  `Entities` children never get a node from their spawn, so **an `Entities` child survives only if its own funct adds
  a node** (Ground, Add…). Cores and buildings go in `Object`. (Corrects `15` §14.4, which put map buildings in
  `Entities`; a stock-map dump confirms where Blockage puts them.)
- Stock blocks: `Block_Ground`, `Block_Surface`, `Block_Stair`, `Block_Oneway`, `Block_Twoway`, `Block_Connector`,
  `Block_None`, `Block_Occupied`, `Block_Surface_Attractor`, `Block_Surface_Deter`, `Block_Noplacement`, plus
  map-specific ones (`Block_Chute`, `Block_Waterfall`, `Block_Flood_Deter`…) (`reference/addressables_default_group.txt`
  :141-151, 524-537). Which group holds each is on the PC list.

### 2.3 `Object`: cores, buildings, map specials

- Each child spawns `Forced` with `Autogenadd`, taking the node at exactly that position with the same up vector
  (Game_Entity.cs:130-140): **a building sits one tile above its ground block, at the surface node's centre.**
- Teams come from `Map_Generationedit` (Mod Tools package: `Team`, `Skin`, `Preupgrade`) on the child; **without it
  the team is -1**, with it the default is 0 (Link_Spawn.cs:28; Game_Grid.cs:289-310). With `Mode_Normal`, players are
  team 1 and the core gives vessels only to its own team, so **`Main_Core` needs Team 1 and `Main_Enemycore` Team 0**
  (Mode_Normal.cs:324-338; Entity_Ai_Main_Core.cs:184-195).
- **Each core needs a free, walkable, same-height `Surface` tile on one of its four sides**, or the first vessel or
  enemy spawn throws on an empty list (Entity_Ai_Main_Core.cs:48-67, 90; Entity_Ai_Main_Enemycore.cs:50-57, 107). The
  enemy core needs a path to a core, or it silently drops every spawn (:122-132). `Mode_Normal` throws without an enemy
  core (Mode_Normal.cs:117).
- `Mode` children: for each of the mode's `Specialspots` (`Location`, `Team`, `Optional`, `Item`), a child named
  `Location` spawns `Item` there; a missing one only logs "Map does not contain a Mode spot" (Game_Rounds.cs:72-157).
  `Mode_Normal` has 4 spots whose names are data (PC list).

### 2.4 Colliders, visuals, light

- **Colliders on `Hit`'s direct children, layer 6 set in the prefab.** The host sets layer 6 on `Hit`'s descendants
  but not `Hit` itself; a client sets it only two levels below the `Mapdata` root (Extramethods.cs:100-109;
  Game_Client_Init.cs:130-167). Layer 6 is what mouse picks, tower line of sight and projectiles hit, so `Hit` must
  match the visible ground.
- **All visible geometry goes in `Mapvis`** (every machine instantiates it at the origin); its optional children
  `Entrypoint`, `Entrypointalt` and `Camerapoint` steer the intro camera (Game_Client_Init.cs:127-281).
- `Lighting` (a `Bl_Lighting_Group`: sky, fog, ambient, a URP volume profile, sun) and `Atmosphere` (an
  `Atmosobject`: loading clouds, default FOV 45) are **both required**: a null one throws (Bl_Lighting.cs:24;
  Game_Load.cs:141). The database has 55 lightings and 28 atmospheres to borrow by name.
- **No baked lightmaps**: the game's lightmap applier `Map_PrefabLightmapData` lives in `Assembly-CSharp`, not in the
  Mod Tools package, so a mod project can't add it. Use the realtime sun and copied game materials (as towers do,
  `03` §4).
- The camera is unbounded in a match (Game_Cameracontroller.cs:44, 329-333).

**Smallest valid map:** `Mapdata` → `Hit` (collider children, layer 6) · `Maxbound` (direct child, scale = tiles) ·
`Entities` (`Block_Ground` × N at tile centres) · `Object` (`Main_Core` Team 1, `Main_Enemycore` Team 0, each on a
surface centre with a free side and a path between them); plus `Mapvis`, a stock `Lighting` and a stock `Atmosphere`.

## 3. Getting the game to load it

### 3.1 Not the custom-map slot

`Custom_Map_Overworld` on the stock `Special_Custom` node picks the `Mapchoice`-th of every loaded mod's prefabs whose
name contains "Map", sorted by name, and writes its `Mods_Mapsettings` into the **shared stock** Gameplan and the
`Map_Custom` `Mapobject` (Custom_Map_Overworld.cs:19-105). It is not the pack's route:

- loading `Map_Custom` hung in RogueLab round 2 (`09` #78);
- the order is per machine, so players with different mod sets get different maps;
- any mod prefab with "Map" in its name and no `Mods_Mapsettings` throws;
- it edits stock objects for the session and records nothing (`Special_Custom` is unranked, `Record` false);
- mod code cannot reference `Mods_Mapsettings` or `Mod.prefabs` at all (the kit's compile references leave out
  `ModTool.dll` and `Exporteritems.dll`).

This corrects `14` §2 and `15` §10 ("maps ship as `Map` prefabs with `Mods_Mapsettings`").

### 3.2 Own registration (the pack's route)

The same pattern RogueLab used for its mode, which played 43 stock maps cleanly (`15` §15;
`TDS_Mod/ModSource/RogueLab/Code/02_Boot.cs`), on **every machine**, at boot:

1. **`Mapobject`**: `ScriptableObject.CreateInstance<Mapobject>()`, `name` and `Objectname` `RW_Map_<Name>`;
   `Lighting` and `Atmosphere` = stock items by name; `Mapdata` and `Mapvis` = the mod's templates.
2. **Group**: a pre-loaded `Bl_Address` group holding it (`Groups["Map"]`, `Groupsname`, `Loadedfolders[name]`), so
   `Load` returns early and `Waitload` returns at once (Bl_Address.cs:58, 271-282). **No Addressables catalog entry is
   needed.**
3. **`Mapentryobject`**: clone a stock entry; `Display_*`, `Difficulty` (1-9), `Maploads` = a clone of a stock group
   renamed to the same name, `Subloads` = the stock groups whose beings the map uses; into `Items["Map"]`.
4. **Gameplan**: clone a stock plan; Map, Mode, Soundtrack, `Par`, `Record`, `Prediscover = true`; keep its
   `Defaultmedalstable`; into `Items["Gameplan"]`.
5. **Node**: a `Macro_Maplink_Add` with that plan, as the sea chart builds; its GameObject name unique and equal on
   every machine.

Template rules: the `Mapdata` and `Mapvis` templates sit under an inactive holder in the `Default` prefab, are
active-self, and avoid the loader's keywords (`Default`, `Mainmenu`, `World`, `Loading`, `Gameserver`, `Gameclient`)
and "Map" in their names. `Bl_Address.Load` swallows exceptions and `Waitload` has no timeout, so **a wrong group name
hangs** (the `09` #78 shape) instead of failing.

### 3.3 Built in Unity, or from code

| Route | How | Risk |
| --- | --- | --- |
| Unity-built prefab | the hierarchy of §2 in the mod project; `Map_Generationedit` on the two cores; registered by code | needs the stock-map dump to copy conventions |
| Code-built from a seed | create `Hit`, `Maxbound`, `Entities`, `Object`, `Mapvis` under an inactive holder, then register | mod code can't add `Map_Generationedit` (it lives in `Exporteritems`): copy two Unity-built core children into each generated `Object`, or spawn the cores from the host after `Maploaded` and before `Startgame` with the team set |
| A stock map cloned and edited before `Generatemap` | `Instantiate` its `Mapdata`, edit `Entities`/`Object` children, register the copy with the stock `Mapvis` | stock `Hit` and `Mapvis` no longer match removed or added ground; safe only for additive specials or removed `Object` children. Never edit the shared stock template in place |

Terrain cannot change after `Generatemap`: the passes run once. Only special beings can be added mid-map (`16` §3,
verified for `Main_Flood`).

## 4. Online

The host builds the grid and syncs `Origin`; blocks, surfaces and buildings are networked entities. Each client
instantiates its **own** `Mapvis` and `Mapdata` copy and keeps only `Hit`, so **`Hit`, `Mapvis`, `Lighting` and
`Atmosphere` must be identical on every machine**. A seeded map derives everything from the synced name
(`RW_Map_<seed>`). A client missing the entry throws, never sets `Loaded`, and the whole lobby leaves at 120 s
(`19` §3).

## 5. What a mod map keeps

| System | Mod map | Why |
| --- | --- | --- |
| Leaderboard | nothing | `Validate` is -1 with any modded player (`23` §5) |
| Records (`Clear_`, `Time_`, `Wave_`…) and `Maphistory` | yes, under the plan's name | written when `Validate > -2` (Game_Endscreen.cs:231-346) |
| Medals | yes, if the plan has them | `Defaultmedalstable` compares `Time_` with `Par`; `Par = -1` turns time medals off |
| Map panel, setup screen, map stats page | yes | read from the entry and plan |
| Unlock trackers with `Gameplan = "Any"` | count pack matches | `Unlock_Tracker_Matchreader.cs:15, 80`; decide if the pack wants that |
| Discoveries | only if the pack adds entries | `23` §3.5 |

## 6. Pitfalls (predicted from the code; move to `09` once met)

1. A misnamed `Entities` or `Object` child → exception inside `Generatemap`, the map never loads.
2. `Maxbound` nested, or no `Entities` child → exception in `Generatemap`.
3. Cores in `Entities` → swept by `Despawnanonblock`; `Mode_Normal` then throws.
4. No `Map_Generationedit` on a core → team -1: no vessel spawns, or the Tide never ends.
5. A core without a free same-height side tile → exception on the first spawn.
6. No path from the enemy core to a core → every enemy spawn dropped silently.
7. `Object` positions off the surface centre (forgot +1 above ground, or the 0.5 offset) → no node, no spawn tiles.
8. Colliders deeper than `Hit`'s children → clients pick and aim through ground the host blocks.
9. Visuals in `Mapdata` instead of `Mapvis` → visible on the host only.
10. A null `Lighting` or `Atmosphere` → exception at load.
11. A wrong `Subloads` group name → `Waitload` hangs, then the 120 s kick.
12. Baked lightmaps or a custom URP shader → no applier; use realtime light and copied game materials.

## 7. PC list (one RogueLab-style round)

1. **Dump a stock map's `Mapdata`** (`Map_Testflat`, `Map_Thehunt`): direct children and counts; every distinct child
   name under `Entities`, `Object`, `Mode`; each child's `Map_Generationedit`; `Maxbound` position and scale; `Hit`'s
   collider layers and depths; `Mapvis`'s direct children.
2. **Dump groups:** which group holds `Block_Ground`, `Block_Surface`, `Block_Stair`, `General_Mob`, `Main_Core`,
   `Main_Enemycore`; `Mode_Normal`'s 4 `Specialspots`.
3. **Time a stock load:** node count and seconds from `Generatemap` to "MAP HAS BEEN GENERATED!" on a small and a
   large map.
4. **`Map_Custom`'s 6 subload names** and which throws (closes `09` #78's cause).
5. **A tiny registered map** (code-built 6 × 6 ground, a core Team 1, an enemy core Team 0, through §3.2): vessels
   spawn, a Tide arrives and ends, the end screen writes `Clear_RW_…`; then online, the client sees and builds.
6. **Negative checks:** no `Map_Generationedit` on the core (expect no vessel); nested `Maxbound` (expect the
   exception).

## 8. Sources

The decompiled `Assembly-CSharp.dll` (Game_Grid, Game_Init, Game_Client_Init, Game_Load, Game_Entity, Block_Funct_*,
Helper_Gridtools, Entity_Ai_Main_Core, Entity_Ai_Main_Enemycore, Custom_Map_Overworld, Bl_Address, Bl_Lighting,
Game_Endscreen), the TDS Mod Tools package (`Assets/Exporteradds`, `ProjectSettings/TagManager`), read on 2026-10-14 by
a parallel reader and spot-checked by hand (`Despawnanonblock`, `ChangeLayerAll`, the `Entities` spawn).
