# The game's network surface: every RPC and every synced field

Reference for `docs/19_NETWORKING.md`. Read from the decompiled `Assembly-CSharp.dll` (2026-10-14, `docs/13` §11);
`File.cs:line` points into that folder (never committed). Tried only with a second game copy on one PC (`docs/19` §8, 2026-11-04), never on real Steam online. All 47 RPCs are reliable and
every ServerRpc has `RequireOwnership = false`, so **any client can call any ServerRpc**; "Checks" is what the host
verifies before acting. "Leader" means the host in practice (`19` §1).

## 1. RPCs

### 1.1 Lobby and session (`Blm_Server`, `Blm_Multiplayer_Remote`)

| RPC (args) | Does | Checks | For a mod |
| --- | --- | --- | --- |
| `Blm_Server.Lobbyupdateup_ServerRpc()` :84 | host answers with `Lobbyupdatedown_ClientRpc` | none; each client sends it on connect | — |
| `Blm_Server.Lobbyupdatedown_ClientRpc(ulong[] Ids, NetworkObjectReference[] Objects)` :164 | every machine rebuilds its player lists and `Isleader` | host sends | — |
| `Blm_Server.Getsessionid_ServerRpc(string Guid)` :246 | host maps the GUID to a Sessionid, writes the sender's `Blm_Player.Sessionid` | none | never call: the GUID is the identity (`19` §5) |
| `Blm_Server.Sendsteamidup_ServerRpc(string Name, ulong Value)` :301 | host relays to everyone | none | — |
| `Blm_Server.Sendsteamiddown_ClientRpc(ulong Player, string Name, ulong Value)` :345 | sets that player's `Steamid`, `Steamname`, avatar; the owner then writes `Displayname` | host sends | one-shot: a late joiner never gets earlier players' avatars |
| `Blm_Multiplayer_Remote.Setleader_ServerRpc(ulong Newuser)` :38 | moves `Currentleader` | sender is leader, or leader gone and sender is host | no caller in the game |
| `…Kickplayer_ServerRpc(ulong Clientid)` :76 | `Blm_Multiplayer.Kickclient` → NGO `DisconnectClient` | leader | no caller; a host mod calls `Blm_Multiplayer.Instance.Kickclient(id)` |
| `…Changescene_ServerRpc(Scenechangevar)` :117 | lobby state, `Lastscene`, NGO `LoadScene` Single | leader | how a host mod changes scene |
| `…Addscene_ServerRpc` :168 / `…Removescene_ServerRpc` :217 | NGO additive load / unload | leader | — |

### 1.2 Boat and setup (`Tds_Multiplayer_Remote`, `Tds_Multiplayer_Radio`, `Macro_Main`)

| RPC (args) | Does | Checks | For a mod |
| --- | --- | --- | --- |
| `Quitmap_ServerRpc()` :60 | ends the map: `Gamecompleted`, lobby Closed, `Gamecomplete` "Return" | leader | a mode can end a map this way |
| `Returnmap_ServerRpc(bool State)` :177 | end-screen vote: sender's `Activeplayer` 2 (go) or 1 | sender's player object, found by client id; **no state guard** | — |
| `Changehero_ServerRpc(string Hero)` :245 | writes the sender's `Tds_Clientinfo.Hero`; log `HERO SELECTED <name>` | the hero exists in the **host's** database (:280); nothing else | the host's mod list decides which vessels exist |
| `Changeartifact_ServerRpc(string Art, bool Mixed)` :296 | sets the run's Cardinal slot | **none** | — |
| `Removeartifact_ServerRpc(string Type)` :378 | clears it | **none** | — |
| `Unready_ServerRpc()` :467 | every player `Activeplayer` 0 | **none** | — |
| `Startgame_ServerRpc(bool State)` :509 | sender ready; all ready → lobby Closed, `Enteringmap`, 4 s, `Changescene("Gameloading")` | sender's own flag | — |
| `Selectmap_ServerRpc(string Nodename, bool Alt)` :600 | writes `Map`, `Mode`, `Soundtrack`, `Gameplan`, `Selected`; unreadies | leader; node unlock from the **host's** save (:670); node found by GameObject name (:654) | node names must be unique and equal on every machine |
| `Waypointmap_ServerRpc(string Nodename)` :699 | clears the pick, sets `Waypointed` | leader | — |
| `Pingnode_ServerRpc(string Nodename, string Checknodename)` :765 | → `Pongnode_ClientRpc` with the host's unlock state | leader | — |
| `Selectworld_ServerRpc(string Worldname)` :827 | `World` | leader | — |
| `Tds_Multiplayer_Radio.Sfxui_ClientRpc(string Item)` :18 | UI sound on every machine | host sends | stock key only (`21` §2) |
| `Tds_Multiplayer_Radio.Sfx_ClientRpc(Vector3 Position, string Item)` :56 | positioned sound everywhere | host sends | stock key only |
| `Tds_Multiplayer_Radio.Pongnode_ClientRpc(string Nodename, bool Unlock)` :96 | boat node answer | host sends | — |
| `Macro_Main.Moveboat_ServerRpc(Vector3 ×4)` :68 | boat and camera position | **none** | — |

### 1.3 Entities (`Entity_Master_Remote`, `Entity_Master_Radio`, one of each per entity)

| RPC (args) | Does | Checks / throttle | For a mod |
| --- | --- | --- | --- |
| `Remote.Pingload_ServerRpc()` :10 | host invokes `Master.Pingpongserver(rpcParams)` (sender id inside) | none; each client sends it **once per entity** when that entity's client side is built (Entity_Master_Client.cs:230-233) | the host learns "this client now sees this entity" (`19` §6) |
| `Remote.Command_ServerRpc(string Link, string Json)` :43 | runs the command on the host | sender's Sessionid is `Owner` or in `Subowner`; link in `Commandlistserver`, `Controllable` (:85) | the client → host channel with data |
| `Radio.Pongload_ClientRpc(string Type, string Msg, ClientRpcParams)` :14 | `Client.Pingpongclient(Type, Msg)` | targetable | the only per-client message (`19` §6) |
| `Radio.Kill_ClientRpc(bool Death)` :62 | death look and sound, `Client.Despawn` | unthrottled | sent by `Game_Entity.Despawn` |
| `Radio.Interaction_ClientRpc(Link_Beingref From, To, string Type)` :118 | `Client.Interact` | unthrottled | no stock visual listens |
| `Radio.Projectile_ClientRpc(...)` :226 (11 args), :347 (7 args) | client-local look-alike projectile; the host skips it (:169) | `Projectilecheck` (throttle) | — |
| `Radio.Vis_ClientRpc(string Vistype, Vector3 Position, float Number)` :430 | `Client.Vis` | **unthrottled** | the mod channel for effects (`05` §8) |
| `Radio.Vfxoneshot_ClientRpc(string Item, Vector3 Position, Vector3 Direction)` :487 | stock effect | `Vfxoneshotcheck` (throttle) | can be dropped |
| `Radio.Vfxshake_ClientRpc(Vector3, Vector3, float Duration, float Range)` :529 | camera shake | unthrottled | — |
| `Radio.Vfxdebuff_ClientRpc(...)` :579 (with position), :645 | debuff look on a target | `Vfxdebuffcheck` (throttle) | **can be dropped** (`19` §4) |
| `Radio.Vfxremovedebuff_ClientRpc(string Item, Link_Beingref On)` :700 | removes it | unthrottled | — |
| `Radio.Vfxemitter_ClientRpc(string Item, Vector3, Vector3, Link_Beingref On)` :761 | emitter on a target | `Vfxemittercheck` (throttle) | can be dropped |
| `Radio.Dataadd_ClientRpc(string Folder, string Name, float Value)` :815 | writes into **every client's own save** | unthrottled | campaign marks (`15` §18.2) |
| `Radio.Uiwarning_ClientRpc(string Desc, string Icon, float Duration)` :864 | warning popup everywhere | unthrottled | `20` §2 |

Throttle: the four `*check` helpers send only while `Game_Mapvars.Clientsendstack < 50` and **drop** the send
otherwise; the stack drains by `Reductionrate` per physics step, 10 solo and 4 with more players, chosen once at map
start (Game_Mapvars.cs:114-182; Entity_Master_Radio.cs:208, 218, 479, 571, 637, 753).

### 1.4 Map, chat, credits

| RPC (args) | Does | Checks |
| --- | --- | --- |
| `Game_Players.Ping_ServerRpc(Vector3 Pos, Vector3 Dir)` :123 | → `Ping_ClientRpc` to all (ping marker, 15 s) | none |
| `Game_Players.Togglespeed_ServerRpc()` :244 | flips `Time_Speedup` | **none**: any player toggles speed for all |
| `Blm_Ui_Chat.Sendchat_ServerRpc(string Text)` :119 (private) | adds a line with `Sender = Displayname` | none |
| `Credits_Main.Goingback_ServerRpc()` :41, `Win_Demomain.Goingback_ServerRpc()` :30 (private) | sets `Goingback` | none |

## 2. Synced fields

67 fields. All server-written except the three marked **owner**. Everyone reads all.

| Class (lifetime) | Field: type, holds |
| --- | --- |
| `Blm_Player` (player object) | `Sessionid` long :28; `Displayname` FS64 :30 **owner**-written, the Steam name |
| `Tds_Clientinfo` (player object) | `Hero` FS64 :17; `Team` int :19; `Loaded` bool :21 **owner**; `Mods` bool :23 **owner** (mods active; read only to skip the leaderboard); `Activeplayer` int :25 (0 lobby, 1 ready or playing, 2 voted go); `Playerstatsjson` FS512 :33; `Resources` NetworkList<Link_Resource> :37 (the wallet) |
| `Blm_Server` (session) | `Currentleader` ulong :47; `Maxplayers` int :49; `Lastscene` FS64 :51 |
| `Blm_Ui_Chat` (session) | `Chat` NetworkList<Link_Chatcontainer {Text FS512, Sender FS64}> :42, never cleared |
| `Tds_Multiplayerinfo` (session) | `Map`, `Mode`, `World`, `Soundtrack`, `Gameplan`, `Gamestate` FS64 :9-19; `Artifacts` NetworkList<FS64> :21; `Mixedart` FS64 :23 |
| `Tds_Multiplayer_Remote` (session) | `Readytimer` float :17 (end-screen countdown) |
| `Macro_Main` (boat) | `Selected`, `Worlded`, `Waypointed` FS64 :22-26; `Enteringmap` bool :28; `Boatpos`, `Boatdir`, `Campos`, `Camdir` :30-36; `Alt` bool :40 |
| `Game_Init` (map) | `Loaded` bool :14 (the Game scene is in); `Gameloaded` bool :16 (map, mode and every client ready) |
| `Game_Grid` (map) | `Origin`, `Originadjust` :836-838 |
| `Game_Mapvars` (map) | `Time_Synced` float :54 (host clock, written every 0.05 s); `Time_Scale` :56; `Time_Speedup` bool :58; `Time_Wave` :60; `Wave` int :62; `Entry` int :64; `Gamecomplete` FS64 :66; `Gamedatajson` FS512 :68; `Display` Link_Displaydataref :70; `Maploaded` :72; `Spawnsafe` :74; `Artifacttrack` NetworkList<float> :76 (8 entries) |
| `Entity_Master` (each entity) | `Clientready` :146; `Being` FS64 :148; `Beingref` :150; `Upgradelist` NetworkList<FS64> :152; `Taglist` NetworkList<FS64> :154; `Mainupgrade` FS64 :156; `Skin` FS64 :158; `State` FS64 :160; `Commandlist` NetworkList<Link_Commandref> :164; `Entitydata` {Owner, Subowner long[], Level, Team, Sellvalue, Upgradesellvalue} :166; `Stats` NetworkList<Link_Statsref> :168; `Position`, `Entityposition`, `Entitydirection` :172-176; `Extrapositiondata` :178 |

**Host-only on each entity** (plain fields, empty on a client; Entity_Master.cs): `Beingserver` :132, `Statsserver`
:170, `Modifiers` :180, `Upgradelistserver` :182, `Commandlistserver` :190, `Taglistserver` :192, `Ais` :196, `Lasthit`
and the counters :198-214. **Host-only lists** (Game_Entity.cs:25-29): `Entitiesserver`, `Entitiesserverteam`,
`Beingsserver`. A client has `Entityclient` and `Entityclienttags` (:21-23).

Struct equality (what NGO compares before it sends a change):

- `Link_Entitydataref.Equals` compares `Subowner` **by reference** (Entity_Master.cs:25): edit a copy of the array,
  never the array in place (`Game_Players.Playerdisconnected` builds a new one, Game_Players.cs:53-88).
- `Link_Statsref` and `Link_Resource` compare by name only (Link_Statsref.cs:21-24, Link_Resource.cs:12-15);
  `Link_Commandref` compares every field but `Goalobject`, which it still serializes (Link_Commandref.cs:22-53; `19` §2).
  Updates are a remove + `Add`, so an updated entry moves to the end of the list on clients.
- `FixedString64Bytes` holds 61 UTF-8 bytes, `FixedString512Bytes` 509 (Unity.Collections); "▪" costs three.
