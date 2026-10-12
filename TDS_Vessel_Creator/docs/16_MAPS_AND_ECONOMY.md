# 16 · Maps, map objects and the economy underneath (what a run can change inside a map)

How a map is built into a grid, how placement and enemy paths work, what the base game's map-special objects do, how
aura nodes pay and how the wallet works, read on 2026-10-09 for the Rogue Waves mode's in-map events, objectives and
Directions. Everything here was **read from the game's code** (`Assembly-CSharp.dll` of 2026-10-08, decompiled as in
`13` §11) unless it says otherwise; `File.cs:line` points into that decompiled folder (never committed). Most of
this chapter was not tried in game yet (§9 is the PC list); §3's flood and nest and §6's host `Time_Scale` were
verified by RogueWavesMode PC rounds 1-4 (2026-10-11). Ideas built on these facts are in the Rogue Waves repository's
`docs/MECHANIC_IDEAS.md`.

Read `01` §4-9, `13` §3 and `15` §14.2-14.4 first.

## 1. The grid

- **A map is a hand-made prefab, never generated.** `Mapobject` holds `Mapdata`, `Mapvis`, `Lighting`, `Atmosphere`
  (Mapobject.cs:8-14); `Game_Grid.Generatemap` loads it by `Mapentryobject.Maploads` (Game_Grid.cs:174-175). Its
  children have roles: `Hit` = the collision mesh (layer 6, :179-193), `Maxbound` = the grid's size from its scale
  (:194-201), `Entities` = beings spawned by their Transform's name (:275-311), `Object` = towers, cores and the like
  (:656-713).
- **No randomness anywhere in the layout:** no `Random.` call in `Game_Grid`, `Helper_Gridtools`, `Helper_Navtools`,
  the `Block_Funct_*` files or the enemy core; enemy spawn spots cycle through the core's neighbours in order
  (Entity_Ai_Main_Enemycore.cs:56, 107-118). A map plays the same layout every time; variety is the mod's to add (§2).
- **Tiles.** One world unit per tile; positions are stored ×100 from `Originadjust` (Helper_Gridtools.cs:89); off-grid
  positions go to an `Offgrid` dictionary (Game_Grid.cs:954-965). A node is a `Genitem` with a direction (tiles can
  tilt), a `Connections` list, 11 occupant slots and a `Property` set: Move / Stand / Place / Occupy / Nav block, each
  with a team variant, plus `Influence` (Genitem.cs:14-36, 59).
- **A "block" is an entity** whose `Blockobject` has `Functs`, `Properties` (`Genproperties`), `Placementconditions`
  and `Additionalspace` (Blockobject.cs:27-33). The node's flags are the fold of its occupants' `Genproperties`
  (Game_Grid.cs:864-899). Path weights (`Navinfluence`): Attractor -100, Tower 20, Wall 40, Repellor 100, Inf 999
  (Genproperties.cs:32-42).
- **The graph is generated in 10 passes** of each block's `Genfunct` (Game_Grid.cs:368, 737-743): ground nodes, a
  `Block_Surface` one tile up, `Block_Stair` to a free neighbour one higher, then links to 26 neighbours; one-way and
  two-way blocks trim their links (Block_Funct_Block_Oneway.cs:36-55, Block_Funct_Block_Twoway.cs:48-92). Nodes closer
  than 0.5 merge (Game_Grid.cs:459-556), then `Maploaded` is set (:775).

## 2. Placement and enemy paths

- **Placement** is `Helper_Gridtools.Checkifplaceable`: each cell of the being's `Placementconditions` (Navagatable,
  Occupyable, Placeable, Notnavagatable, Includeteam), rotated by the build's orientation (Helper_Gridtools.cs:426-456);
  the footprint is `Additionalspace` (Game_Grid.cs:1060-1069). The server runs it for every build
  (Entity_Ai_Hero_Buildsell.cs:61) and for Fie's wall extension (Entity_Ai_Tower_Fiewall_Build.cs:38).
- **No "path still open" check exists.** Neither the host nor the client tests whether a build seals a path
  (Entity_Command_Build.cs:113-116). What stops a maze is data: which blocks carry Navblock, and the stock
  `Entity_Ai_General_Adjacentblocker`, which marks the tiles beside a structure `Block_Noplacement`
  (Entity_Ai_General_Adjacentblocker.cs:6-20).
- **The build check accepts any command whose key contains "Build"** (not `Build` itself) and takes that command's
  `Goalvalue` as the **cost scale** of the building: it checks and spends `Costs × Goalvalue` and stores it as the
  building's sell value (Entity_Ai_Hero_Buildsell.cs:76-87, 146-170). The button shows the scaled price
  (Entity_Command_Build.cs:70-93). With `01` §7 (an upgrade's price scale is the `Upgrade` command's `Goalvalue`),
  **build and upgrade discounts are per vessel, so per player, and the UI already shows them.**
- **Paths** are a greedy A*-like search over `Connections` (Helper_Navtools.cs:18-172): distance so far + straight
  line to the goal, horizontal steps ×5 and vertical ×20 (:76), +0.75 per diagonal (:80), + the node's `Influence`
  (:82); a node is passable when its Navblock for the mover's team is ≤ 0 (:67). Results are cached per goal and start
  for 10 s in the public `Game_Grid.Instance.Savedpaths` (Helper_Navtools.cs:152-191); the cache is wiped only when a
  node is added or removed (Game_Grid.cs:949, 993), **not** on build or sell. A mob plans once per Move command and
  re-checks only Moveblock per step; blocked, it goes `Idleblocked` and `Enemy_Basic` re-issues Move every slow tick
  (Entity_Ai_Mob_Movement.cs:210-245; Entity_Ai_Enemy_Basic.cs:32-53).
- **Goal core:** the enemy core lists the `Core` entities it has a path to, hands them out round-robin per spawn and
  rebuilds the list only when its current goal is gone (Entity_Ai_Main_Enemycore.cs:39-94). Flying uses the same graph
  in a straight line (`Getpathlinear`, Helper_Navtools.cs:221-305); there is no separate air pathing.
- **Changing the map mid-match** (read; a spawned wall verified, below):
  - mark a tile unbuildable or unwalkable by spawning a block entity on its node (`Link_Spawn.Genadd`): the spawn runs
    `Movebeing` and `Updatefunct`, which re-fold the node's flags (Game_Entity.cs:126-128, 299-301). This is how the
    stock flood works (§3);
  - add or remove nodes and links with the public `Helper_Gridtools.Addgenitem`, `Removegenitem`, `Addconnection`,
    `Removeconnection` (Helper_Gridtools.cs:140-212). A new node is not linked by itself; Boutboat links its own
    (Entity_Ai_Tower_Boutmanboat.cs:252);
  - paths follow after the 10 s cache expiry, or at once if the host clears `Game_Grid.Instance.Savedpaths`.
    **Verified** (RogueLab round 7, 2026-10-12): a wall spawned on tile 21 of the foes' 61-tile path was still crossed
    by the cached path; after the clear a 60-tile path went round it.

## 3. The base game's map-special objects

Each is a being placed by a map; its team and skin come from data (`Modeobject.Specialspots` or the map's
`Map_Generationedit`, Game_Grid.cs:291). "Wave" = subscribes to `Game_Mapvars.Wave`.

| AI | What it does (read) | Trigger |
| --- | --- | --- |
| `Main_Flood` | rises a step per Tide toward `Maxheight` (`Heightspeed`, slower past `Halfheight`, `Quarterheight`), spawns `Block_Flood_Deter` on every surface tile it covers, multiplies the velocity of projectiles inside it by `Projslow` (Entity_Ai_Main_Flood.cs:15-80) | Wave |
| `Main_Chrono` | from Tide `Quickspeed` / `Doublespeed` / `Superspeed` sets the networked game speed `Game_Mapvars.Time_Scale` to 1.5 / 2 / 2.25, back to 1 for `Lingertime` s after each Tide change, with a warning popup ("THE CLOCK QUICKENS") (Entity_Ai_Main_Chrono.cs:14-53) | Wave |
| `Main_Tpgate` | at `Starting` links its node to every other `Tp`-tagged entity sharing an upgrade key, then teleports any being arriving from more than 2 units away (Entity_Ai_Main_Tpgate.cs:11-81) | node |
| `Main_Subcore` | survives the first lethal hit at 1 HP; on death, after 4 s, kills every non-team mob and tower within 10, then the rest (Entity_Ai_Main_Subcore.cs:11-108) | death |
| `Main_Enemycoresplit` (+ `_Effect`) | at Tide 1 upgrades every enemy core so its spawns fan out to the other enemy cores ("Spawnsplit") (Entity_Ai_Main_Enemycoresplit.cs:11-29; `_Effect`.cs:61-123) | Wave = 1 |
| `Oroboro` | at Tide `Path2`, then `Path3`, moves the cores and enemy cores to `Tp` / `Tpe` markers, kills every vessel, vessel building and enemy, warns "THE LAND SHIFTS" (Entity_Ai_Oroboro.cs:30-172) | Wave |
| `Tower_Halberdnest` | gains `Healthperwave` max health per Tide; every `Waveperunit` Tides spawns an `Enemy_Halberd`, respawned after `Respawn` s while the nest stands (Entity_Ai_Tower_Halberdnest.cs:27-69) | Wave |
| `Tower_Den` | keeps up to `Maxunits` `Mob_Aurabeast`, adding one each time its Tide counter passes `Addperwave` (5 on Thehunt, so not before Tide 6), `Respawn` s; hit, it sends its beasts at the attacker (Entity_Ai_Tower_Den.cs:40-144). Tagged `Resource`, team 3 on Thehunt | Wave |
| `Mob_Aurabeast` | wanders surface tiles; a vessel or gatherer that hurts it makes it aggressive; on death splits its `Redauramine`/`Blueauramine`/`Yellowauramine` among all players (Entity_Ai_Mob_Aurabeast.cs:81-190) | death |
| `Blastheadore` | each Tide grows its own payout and spawns `Resource_Branchrock_<colour>` nodes on free neighbours (Entity_Ai_Blastheadore.cs:68-137) | Wave |
| `Tower_Artriobarrier` | barriers sharing a tag share damage and healing; one dying removes them all and their placements (Entity_Ai_Tower_Artriobarrier.cs:17-71) | death |
| `Tower_Tilebuff`, `Tilebuffany`, `Tilebuffanyonly` | force-applies its `Upgrades[0]` to beings on its tile (own-team towers / anything alive / and removes it on leaving) (Entity_Ai_Tower_Tilebuff*.cs:17-39) | tile |
| `Tower_Barrel` | on death spawns `Trap_Barrel_Dead` and `Effect_Barrel_Boost` (timed stat multipliers) on the 13 tiles around (Entity_Ai_Tower_Barrel.cs:24-50) | death |
| `Tower_Converter` | every 3 s pays every player `Convert` of their lowest aura and takes `Convert`/2 of the other two (Entity_Ai_Tower_Converter.cs:43-147) | timer |
| `General_Spawnmobs` | spawns each `Defaultparams` key as a mob nearby, respawned after the value in s (0 = once) (Entity_Ai_General_Spawnmobs.cs:14-77) | spawn |
| `General_Expire`, `Effect_Killonspawn` | despawn after `Duration` / `Delay_Base` (Entity_Ai_General_Expire.cs:24-27; Entity_Ai_Effect_Killonspawn.cs:11-19) | timer |
| `Modresource` (an upgrade's AI) | adds `Red_Base`/`Red_Add` (and Blue, Yellow) to the entity's aura-mine stats: anything it is put on pays aura (Entity_Ai_Modresource.cs:15-95) | upgrade |
| `Tower_Sandbox` | the Sandbox Cardinal's debug panel: spawn foes and elites, money, god mode, next / pause / fast-forward Tide, by writing `Wave` and `Time_Wave` (Entity_Ai_Tower_Sandbox.cs:66-296) | buttons |

`Effect_Chute` (speed override + "Herofall" look), `Block_Surface` (colliders only), `Mob_Invincible` (idles),
`Tower_Snare` and `Tower_Drinkstand` are small helpers. Merchut is `15` §5; Boutman on Artrio is `13` §1.

**How they reach a map:** the map prefab's `Entities` / `Object` children, or the mode's `Specialspots` placed at named
children of the map's `Mode` transform (Game_Rounds.cs:72-157). No game code spawns a map special mid-match, but each
one subscribes to `Wave` in `Spawn` (Game_Entity.cs:303) and reads its stats from data, so **a host spawning one with
`Helper_Entitytools.Spawn` mid-match works from the next Tide on** (verified for `Main_Flood` and `Tower_Halberdnest`,
below; read for the rest). Caveats: `Main_Tpgate` pairs only at `Starting`, so spawn both gates together; the flood
starts at whatever height it spawns; `Main_Flood`, `Tower_Halberdnest` and `Main_Enemycoresplit` never unsubscribe from
`Wave`, so despawning one leaks its handler until the map ends. **Verified** (RogueWavesMode round 1, 2026-10-11):
`Main_Flood` and `Tower_Halberdnest` spawned on Skyhook and Blasthead rise and send a Halberd at the next Tide; a
second `Tower_Den` spawned on Thehunt (with `Addperwave` cut to 0) added 2 beasts at Tide 1 (RogueLab round 7). A map
object's being and the beings it spawns can sit in different groups: the flood fetches `Block_Flood_Deter` by name at
each Tide and throws in `Helper_Gridtools.Checkifplaceable` if that is not loaded, so load every being the object
spawns, not just its own. `Block_Flood_Deter` itself is in none of the groups `Map_Manafold`, `Hero_Boutman` or the six
stock modes load (round 2): the game ships no flood block. The throw unwinds through the `Wave` setter into
`Mode_Normal.Run`, which drives the Tides, so never leave a flood on a map without it. `Block_Noplacement` is not loaded
on the boat (round 3); RogueWavesMode 0.1.3 registers a copy of Boutman's `Block_Noplacementfish` (loaded with
`Hero_Boutman`, one of Manafold's sub-loads) under the name `Block_Flood_Deter`, once the map has loaded; **verified**
in round 4: 67 tiles flooded at the first Tide, no error.

## 4. Aura nodes and mining

- **A node** is an entity tagged `Resource` with a `Healthresource` stat and one or more of `Redauramine`,
  `Blueauramine`, `Yellowauramine` (Entity_Ai_Resource_Mine.cs:41-64). Damage to `Healthresource` is clamped to 0-1 per
  hit plus any `Damageresource` bonus (Helper_Damagetools.cs:47-60), so `Healthresource` is "hits per payout".
- **Mine and Opal never run out:** at 0 the node refills and pays every colour it has to **all players, split**
  (Owner -1) (Entity_Ai_Resource_Mine.cs:106-138). Opal cycles Red, Blue, All, Yellow, one per Tide
  (Entity_Ai_Resource_Opal.cs:75-124). `Resource_Death` is one-shot: it dies and pays on despawn
  (Entity_Ai_Resource_Death.cs:25-73). `Resource_Farmland` fills `Percent` and spawns a `Resource_Crop_<colour>`
  when full, then waits for that crop to die (Entity_Ai_Resource_Farmland.cs:32-76); `Resource_Farmstead` offers three
  farmland builds to all 10 players (Entity_Ai_Resource_Farmstead.cs:11-27).
- **Who mines does not matter for pay:** Smacky's miner walks to the nearest reachable node
  (Entity_Ai_Mob_Smackyminer.cs:73-167), Fie's silo links to the nearest at any range and keeps it until it despawns or
  is rerouted (Entity_Ai_Tower_Fiesilo.cs:143-196), Sno Box hits every node in range with falling shares
  (Entity_Ai_Tower_Snosnobox.cs:271-283), Smacky's hut hits every node on the map after enough visits
  (Entity_Ai_Tower_Smackyhut.cs:213-255). Enemies list `Resource` among their targets, and a node an enemy breaks
  still pays the players (Entity_Ai_Enemy_Maul.cs:23).
- **A mod can make nodes mid-match:** Farmland and Blastheadore already spawn nodes at run time
  (Entity_Ai_Resource_Farmland.cs:51-56; Entity_Ai_Blastheadore.cs:97). A spawned stock node (or a being with the tag,
  the stat and the Mine AI) is found by every miner by tag (Entity_Ai_Tower_Fiesilo.cs:124). A silo already linked
  elsewhere won't switch to it on its own. **Verified** (RogueLab round 7): a `Resource_Red` spawned beside a Smacky
  Hut paid 5 Red when hit, but the Hut's miner kept mining its old ore for 40 s, so only miners picking a new target
  find it.

## 5. The wallet, aura colours and conversion

- **Three auras are hard-coded** in game logic: `Redaura`, `Blueaura`, `Yellowaura`. "Green", "Orange", "Purple" are
  only Fie's gun colours, from the mix of a node's colours (Entity_Ai_Tower_Fiesilo.cs:202-248); Fie's Convert button
  changes the gun, not aura.
- **The wallet takes any name.** `Tds_Clientinfo.Resources` is a `NetworkList<Link_Resource>` of name + amount
  (Tds_Clientinfo.cs:37); `Give` adds an entry for a new name (Helper_Resources.cs:22-29); the HUD makes a slot for
  each new name and looks up an icon of that name (Game_Ui.cs:1436-1452), which must exist (null icon → exception at
  :1452; icons go in early, the way Argo adds its own, `ModSource/Argo/Code/04_Content.cs` `AddIcons`).
- **But stock code walks every wallet entry**, so a fifth currency there leaks: Glass Core's break takes `Aura_Base` of
  every entry (Entity_Ai_Effect_Artifact_Glasscore.cs:62-69); the Converter picks the lowest entry and, if that is an
  unknown name, pays the previous player's conversion (Entity_Ai_Tower_Converter.cs:116-134); a Fie silo upgrade pays a
  share of every entry each Tide (Entity_Ai_Tower_Fiesilo.cs:356-362). **Pure Aura stays off the wallet** (the counter
  on the vessel, `15` §3), or every one of these needs handling.
- **Conversion:** Smacky's Mining Hut button cycles a target colour and, on each gain, trades 0.5 of each other colour
  for 1 of it (Entity_Ai_Tower_Smackyhut.cs:196-311); the Converter is the timed version above.
- **Spending scales by `Game_Mapvars.Resourcelossmultiplier`** as well as the call's `Scale`
  (Helper_Resources.cs:121-132), just as `Give` scales by `Resourcegainmultiplier`. The stock Cardinal Bottom Rock sets
  the loss multiplier to 0 (everything free, gains cut) (Entity_Ai_Artifact_Bottomrock.cs:14-17). So a Fund Transfer
  that takes with `Useresources` under Bottom Rock costs the sender nothing: set it to 1 around the transfer, as
  `Mode_Normal` does for gains (`15` §14.2).
- **Match statistics** per player (kills, deaths, vessel deaths, placed, upgraded, sold, spent and gained per colour)
  are in `Tds_Clientinfo.Activeplayerdata` on the host, reset only when `Activeplayer` drops to 0, not per scene
  (Tds_Clientinfo.cs:31, 67-75; Game_Entity.cs:389-395; Helper_Resources.cs:33-40, 142-145). Kills are credited to the
  last hitter's owner, so the `15` §16 `Lasthit` caveat applies.

## 6. Time

- `Game_Mapvars.Time_Scale` (networked float) is the game speed every machine runs at; `Main_Chrono` and
  `Game_Admin.Settimescale` write it (Game_Mapvars.cs:56). `Time_Speedup` (networked bool) is the players'
  fast-forward toggle (Game_Players.cs:270). A mod writing `Time_Scale` on the host changes the pace for everyone.
- Tick rates: `Time_Gametick` 0.05 s, `Time_Slowtick` 0.2, `Time_Projtick` 0.02, `Time_Reallyslowtick` 1
  (Game_Mapvars.cs:54-120).

## 7. Lighting and atmosphere

- `Bl_Lighting.Editlighting(Bl_Lighting_Group)` sets fog, skybox, ambient, the volume profile, the sun and the
  reflection probe at any time (Blockage.Lighting.Bl_Lighting.cs:23-97); `Game_Admin.Setlighting` uses it
  (Game_Admin.cs:75).
  `Atmosobject` is clouds, FOV and camera angle only (Atmosobject.cs:7-13).
- **Nothing syncs it**: each machine applies the map's lighting itself (Game_Client_Init.cs:168-171; Game_Load.cs:137,
  217). A mid-map change (dusk on later maps, a storm look) must run on every client, from a Vis RPC or each client's
  own code reading a synced value. No gameplay code reads light (`15` §14.7), and there is no weather system.
- The 55 lighting groups and 28 atmospheres the base game ships (`reference/vessel_data_survey.txt`, `FOLDER Lighting`)
  can be loaded by name and applied to any map.

## 8. Projectile behaviours a building can borrow

- `Projectile_Ai_Splitbounce` (Smacky's Bomb Tower): a shot fired with the per-shot param `Splitbounce` = 1 splits into
  three small bombs on landing, = 2 re-fires itself once at the farthest foe within `Bouncerange`
  (Projectile_Ai_Splitbounce.cs:20-100; Entity_Ai_Tower_Smackybomber.cs:199, 324-329). Per-shot params override the
  projectile's `Defaultparams` (Helper_Damagetools.cs:262-277), and the bounce shot doesn't pass `Splitbounce` on, so
  it bounces exactly once: Yossele's full-Growth spore bomb.
- Others with params a mod can set per shot: `Beam` (`Trackspeed`, `Tickrate`, `Maxrange`, `Track`, `Lifetime`),
  `Expand` (`Radius`, `Initradius`, `Any`), `Delay` (`Activetime`), `Corebomb` (`Time`, `Style`, `Hurt`)
  (the files' `Spawn`).

## 9. PC list (for the next RogueLab round)

1. Done (RogueLab round 7, 2026-10-12): a den and a mine node spawned mid-match work (§3, §4).
2. Done: a wall on a path tile re-routes foes once `Savedpaths` is cleared (§2).
3. Done: 0.8 on a `Buildoption` and on an `Upgrade` command cut the paid price to 0.8 (`17` §2.7). The shown price
   was not read.
4. Write `Time_Scale` = 1.25 on the host; log the client's Tide timer (online evening; the host half is verified,
   RogueWavesMode W2: 1, 1.25, 1.5).
5. Call `Uiwarning_ClientRpc` (`15` §18.2) with a mod icon; check it on a client.

## 10. Sources

The decompiled `Assembly-CSharp.dll` (703 types, `13` §11), read by six parallel readers and spot-checked by hand on
2026-10-09 (Flood, Chrono, Checklist, Mooncore, the build cost scale, the wallet loops).
