# 19 · Networking under the hood (what a pack's mode, maps and UI must respect online)

`07` gives the rules a vessel follows online; this chapter is the engine underneath, for the parts of an expansion
that are not a vessel: the mode's host brain, the in-map vote, the shop, the HUD, maps, rejoin. Everything here was
**read from the game's code** (`Assembly-CSharp.dll` of 2026-10-08, decompiled as in `13` §11) on 2026-10-14 unless it
says otherwise; `File.cs:line` points into that decompiled folder (never committed). **Nothing here was tried
online**: §8 is the list for the first online evening. Every RPC and every synced field, with its checks, is in
`reference/network_surface.md`.

Read `07`, `01` §3-4 and `15` §6, §14.5 first.

## 1. Transport, lobby, approval

- **Stack:** Unity Netcode for GameObjects over Steam (`Blm_Multiplayer` requires `FacepunchTransport` and
  `NetworkManager`, Blm_Multiplayer.cs:13-14). Every RPC is reliable and every ServerRpc has
  `RequireOwnership = false`: **any client can call any ServerRpc**, so the checks inside are all there is.
- **A mod cannot add network code.** Mods compile at run time without NGO's code generation: no new RPC, no
  `NetworkVariable`, no network prefab. A mod calls the game's RPCs and writes the game's variables (`15` §13).
- **Hosting:** the boat scene starts the host (`Macro_Init` → `Starthost(4)` → `StartHost` with an approval
  callback, then a public, joinable Steam lobby of 4) (Macro_Init.cs:17-22; Blm_Multiplayer.cs:370-417).
- **Joining is only through Steam** (a friend's "Join Game" or an invite → `Startclientsteam`). There is no lobby
  data, server list or `+connect_lobby` handling (Blm_Multiplayer.cs:149-196, 321-352).
- **Approval checks two things:** fewer than 4 connected and `Server_Serveropen`; else "Server is closed" or "Server is
  full" (Blm_Multiplayer.cs:63-90). **No version, mod-list or password check**; the payload is the fixed text "None".
  NGO's own config hash refuses a different game build, but every mod entity uses the same generic prefab, so it
  cannot see mods.
- **The leader is always the host.** `Setleader_ServerRpc` and `Kickplayer_ServerRpc` have no caller in the game and a
  missing leader is reassigned to the host (Blm_Multiplayer.cs:301-319). Every "leader only" RPC is host-only in
  practice.
- **Scene flow of a match:** `Mainmenu` → `Setup` (boat, host started) → `Startgame` → `Gameloading` (Single) → `Game`
  added → `Gameloading` removed at "Loading 90%" → end screen → `Setup` or `Mainmenu`
  (Tds_Multiplayer_Remote.cs:509-597; Game_Load.cs:256-314). A host mod changes scene with `Changescene_ServerRpc`
  (leader only).
- **No host migration.** The host leaving or crashing disconnects everyone to `Serverclosescene` with an error
  (Blm_Multiplayer.cs:238-246). Run state that lives only on the host is gone unless the host's mod saved it.

## 2. What syncs, and what never reaches a client

| Data | How a client gets it | Where |
| --- | --- | --- |
| An entity | NGO spawns the generic prefab; the client's `Entity_Master_Client.Start` waits `Clientready` and `Being`, loads the being **by name** from its own address book, builds the look from `Mainupgrade`, `Upgradelist`, `Skin`, `State` | Entity_Master_Client.cs:129-233 |
| Stats | `Modifiers` stay on the host; the computed value goes into the `Stats` list | Link_Stats.cs:105-117 |
| Commands | `Commandlist`: `Goalstring` (61 bytes), `Goalvalue`, `Goalposition`, `Controllable`; **`Goalobject` only through `Updatecommandclientobject`** (Fie's silos, Buster's pads; `Updatecommand` resends the last one); a command created without a command object (`Createcommand(m, "", link)`) is never listed | Helper_Entitytools.cs:264-431 |
| Tags, upgrades | `Taglist`, `Upgradelist` (written with the host lists) | Helper_Entitytools.cs:433-496 |
| Tide | `Wave`, `Entry`, `Time_Wave`, `Display`, `Gamecomplete` on `Game_Mapvars` | Game_Mapvars.cs:60-70 |
| Wallet | each player's `Tds_Clientinfo.Resources` list | Helper_Resources.cs:12-73 |
| A clock | `Game_Mapvars.Time_Synced`, written by the host every 0.05 s | Game_Mapvars.cs:54, 131-162 |
| Speed | `Time_Scale` and `Time_Speedup` hooks set `Time.timeScale` on each machine | Game_Client_Init.cs:236-249 |

**Host only, empty or silent on a client** (Entity_Master.cs:132-214; Game_Entity.cs:21-29, 294, 361;
Game_Mapvars.cs:131-162):

- per entity: `Statsserver`, `Modifiers`, `Taglistserver`, `Commandlistserver`, `Upgradelistserver`, `Ais`;
- the lists `Entitiesserver`, `Entitiesserverteam`, `Beingsserver`, so **every `Helper_Entityquerytools` query
  returns nothing on a client**;
- `Game_Entity.Onspawn` / `Ondespawn` (a client has **no spawn event**: poll `Entityclient` or hook each entity's
  `Client.Despawn`);
- `Game_Mapvars.Tick`, `Slowtick`, `Reallyslowtick`;
- the mode script (`Modescript`); only `Clientmodescript` runs on every machine (Game_Rounds.cs:253-267).

The self-test runs as host, where all of these are filled, so **it cannot catch a client reading host-only data**.

**Struct traps** (reference §2): `Entitydata.Subowner` is compared by reference, so editing the array in place never
syncs (build a new array); `Stats` and `Resources` entries compare by name only, `Commandlist` entries by every field
but `Goalobject`; all three are updated by remove + add, so an updated entry moves to the end of the list on clients.

## 3. Loading a map, and the 120 s kick

1. All ready → lobby closed → `Changescene("Gameloading")` after 4 s (Tds_Multiplayer_Remote.cs:509-597).
2. `Game_Init` (host): loads the groups, `Generatemap`, then **waits for every player's `Loaded` with no timeout**,
   then `Generatemode`, `Spawnsafe`, `Gameloaded`, `Startgame`, the `Gameserver` prefabs (Game_Init.cs:47-170).
3. `Game_Client_Init` (every machine, host too): loads the same groups from the synced names, waits `Maploaded`,
   builds the map's look, sets its own `Loaded`, starts the client mode script (Game_Client_Init.cs:39-178).
4. `Game_Load` on each machine runs a 120 s `Loadtimeout`; past it, `Kickout` = leave the server and load the main
   menu (Game_Load.cs:36-70).

So **one client that cannot load the map** (missing pack, missing map entry, a wrong group name that hangs
`Waitload`) stalls the host at "Loading 50%", and after 120 s **every machine, the host too, drops to the main
menu**. A pack defends itself in §6 rule 10. Timing for a watchdog: after `Gameloaded` the loading screen still takes
about 15 s (`Minloadtime` 5 s, a 3 s fade, a 10 s fly-in, Game_Load.cs:173-263) before its own timer stops, so a
client must be sent back well before 105 s; RogueWavesMode 0.3.0 does it at 75 s (`RWLoad`). A kicked player object
leaves `Blm_Server.Players` on despawn (Blm_Player.cs:113-127), which is what `Game_Init`'s wait loop reads.

## 4. The effect throttle

`Projectilecheck`, `Vfxoneshotcheck`, `Vfxdebuffcheck` and `Vfxemittercheck` share `Game_Mapvars.Clientsendstack`:
they send only while it is under 50 and **drop** the send otherwise. It drains by `Reductionrate` per physics step:
**10 solo, 4 with more players**, chosen at map start (Game_Mapvars.cs:114-182; Entity_Master_Radio.cs:208-753).

- In a busy online fight, debuff looks, stock effects, emitters and look-alike projectiles silently fail to show. A
  solo test drains 2.5 times faster and hides it.
- Not throttled: `Vis_ClientRpc`, `Uiwarning_ClientRpc`, `Kill_ClientRpc`, `Vfxremovedebuff_ClientRpc`,
  `Vfxshake_ClientRpc`. **What a player must see goes through `Vis`, a skin, a stat or a networked being**; throttled
  channels are for flavour. (This narrows `07` rule 9 and `05` §8.)
- Or past the throttle: the `*check` methods are only the gate, and the ClientRpcs behind them are public. A must-see
  look calls `Vfxdebuff_ClientRpc(item, beingref, seconds)` or `Vfxoneshot_ClientRpc(item, position, direction)`
  itself and adds one to `Clientsendstack`, so stock effects make room. Send a debuff look again only when the new
  one would end later than the one showing (`Helper_Vfxtools.Debuff` resets `Duration` on a look already attached).
  Argo 2.0.1 does this for its slow look and its refused-flood mark (`ArgoUtil.Debuff`, `MustSee`). Compiled, not
  run.

## 5. Late join, drops and identity

**A late joiner gets** every entity with its stats, commands, tags, upgrades, skin and state; the Tide, banner, speed,
wallets, chat history and `Displayname`s (NGO sends spawned objects with their current values).

**A late joiner misses** everything sent by RPC before it arrived: debuff looks and emitters, any look a mod built
from a `Vis` event, earlier players' Steam avatars, `Dataadd_ClientRpc` save marks, `Startvisuals` and the music
stage (`15` §14.5, `21` §1.2). §6 rule 5 is the hook to re-send them.

**A drop** (Game_Players.cs:53-120; Blm_Player.cs:113-127): the player object goes; teammates become sub-owners of the
dropped player's things (only if their `Activeplayer > 0`); their `Tds_Clientinfo` (wallet, hero, stats) is destroyed
unless a mod copied it (RogueWavesMode does, `docs/mode/REJOIN.md`).

**Identity across a drop: the `Sessionid` survives only while the friend's game stays open.**

- The host hands out `Sessionid`s per `Player_Guid` (Blm_Multiplayer.cs:457-466). Each client keeps its GUID in its
  save's `Metadata` folder (Blm_Player.cs:79-91), and `Bla_Data` **removes `Metadata` on every load**
  (`Temporaryfields`, Blockage.Tags.Bla_Data.cs:57, 199-208), once per launch (the code's default; the inspector
  value is data, PC list `23` §8).
- So a pulled cable or a Wi-Fi drop (game still open) comes back with the same `Sessionid`; **a friend who restarts
  the game gets a new GUID and a new `Sessionid`**, and the stock hand-back (`Playerconnected`) misses too.
- The host's table is cleared when the host leaves networking (Blm_Multiplayer.cs:425): `Sessionid`s last one host
  session (the boat and every map from it), never across sessions.
- Durable per player: the synced, owner-written `Displayname` (the Steam name; not unique, and it changes if the friend
  renames). `Blm_Player.Steamid` (a `SteamId`, set on every machine by a ClientRpc a moment after spawn, 0 without
  Steam; Blm_Player.cs:34, Blm_Server.cs:399) is **out of reach**: Steamworks is not in the game's compiler reference
  list (`Build-<Name>Mod.ps1`), so code that touches the field doesn't compile in game (cloud-check stubs it as
  `object` and can't tell). `Blm_Player.Steamname` is a plain string copy, but only on machines that saw the ClientRpc.

**The fix: keep the GUID.** The pack is on every machine, so each keeps its GUID in the pack's own save folder and
writes it back into `Metadata/Player_Guid` before the game reads it on joining (`Blm_Player.Start`); the host then maps
it to the same `Sessionid`, and the stock hand-back and RogueWavesMode's seats work as for a pulled cable.
RogueWavesMode 0.3.0 (`RWIdentity`, `Code/10_Online.cs`) does this every half second, on the main menu too:

- `Metadata` already holds a GUID while connected, or nothing is kept yet: that one is kept (the host may know it).
- Otherwise the kept GUID goes back (a new launch, another save slot); with neither, a new one is made.
- The kept GUID is tied to the computer (a hash of `SystemInfo.deviceUniqueIdentifier`). Without that, a save copied
  to a friend's PC would give two players one GUID, and the host's `Populatesessionid` would `Add` the same
  `Sessionid` twice and throw.
- It lasts per save slot and per PC. A rejoin while the host still holds the old player object would collide the same
  way; a restart takes longer than Steam's disconnect, so this is not expected (`docs/mode/REJOIN.md` §4, O3b).

The host can also read a player's kept GUID back from `Blm_Multiplayer.Instance.Sessionids` (GUID → `Sessionid`): a
key that survives sessions and Steam renames, but not a new PC or another save slot. Read from code; compiled, not
run.

## 6. Rules for mode code (what `07` doesn't cover)

1. **Run state lives on the host, keyed by `Sessionid` within one host session.** With the GUID kept (§5) that also
   covers a client restart. Anything that must survive the host's session (the campaign's per-player things) keys on
   `Displayname` (`RecipeNet.DurableName`) or the kept GUID (§5), each with its limits.
2. **What every client must see goes into a synced carrier:** `Display` (banner, note), a Counter stat, a command's
   `Goalstring`/`Goalvalue`, `Skin`, the wallet, a chat line written by the host. RPC-only state must be re-sent to late
   joiners (rule 5).
3. **Effects that must show** use `Vis` (unthrottled), a skin, a networked being, or the ClientRpc itself (§4).
4. **One client, not all:** `master.Radio.Pongload_ClientRpc(type, msg, rpcParams)` with
   `rpcParams.Send.TargetClientIds = new ulong[]{ id }` reaches `Entity_Master_Client.Pingpongclient(type, msg)` on
   that machine only (Entity_Master_Radio.cs:14-59; the stock `Block_Surface` does exactly this,
   Entity_Ai_Block_Surface.cs:70-81). `Pingpongclient` also receives the game's own load ping once per entity: filter
   on your own `type` prefix. A player's client id: the `Blm_Server.Instance.Players` objects are named by client id
   and carry `Blm_Player.Sessionid`. Recipe: `examples/recipes/RecipeNet.cs`.
5. **Late-join catch-up:** each client sends `Pingload_ServerRpc` once when an entity's client side is built
   (Entity_Master_Client.cs:230-233); on the host, `master.Pingpongserver` receives it with
   `serverRpcParams.Receive.SenderClientId`. Hook it on an entity every map has (the Basin, tag `Core`) and answer that
   client alone with rule 4. Any client may also call `Pingload_ServerRpc()` itself as an argument-less "ask the host".
6. **Client → host with data stays a command** on the player's own vessel (`15` §6), validated on the host. Votes are
   counted per `Sessionid` with a timeout: several stock ServerRpcs check nothing (`Changeartifact`, `Removeartifact`,
   `Unready`, `Togglespeed`, `Moveboat`), so any client can unready the lobby, toggle speed or change the run's
   Cardinal.
7. **On a client never read host lists:** poll `Game_Entity.Instance.Entityclient`; find your own things by
   `Entitydata.Value.Owner == Blm_Server.Instance.Localplayer.Sessionid.Value`; read `Taglist`, `Stats`,
   `Commandlist` (the last entry of a name).
8. **Time:** host timers decide; a countdown shown on clients reads `Time_Synced`, not `Time.time` (narrows `07` rule
   12). There is **no pause online** (the pause menu stops time only when one player is in, Tds_Pausemenu.cs:187-189),
   so a dialog runs while the Tide runs, unless the mode holds the Tide itself. Never write `Time_Scale` 0: the client
   hook ignores changes while `Time.timeScale` is 0, so every machine would stay frozen (Game_Client_Init.cs:236-250).
9. **Struct writes:** a new `Subowner` array; commands through `Updatecommand`; a host edit of `Goalobject` reaches
   clients only through `Updatecommandclientobject` (§2).
10. **Pack handshake (none in the game):** the host posts the pack's name and version as a chat line at the boat
    (late joiners get the whole chat list) and flags a player whose `Tds_Clientinfo.Mods` is false. During a map
    load a host-side watchdog can `Blm_Multiplayer.Instance.Kickclient(id)` a player whose `Loaded` is still false,
    so one broken client no longer sends everyone to the main menu at 120 s (§3; RogueWavesMode 0.3.0 `RWLoad`, 75 s).
    Both untested.
11. **The host's save and mod list decide:** `Selectmap` reads node unlocks from the host's save and `Changehero`
    accepts only heroes in the host's database (Tds_Multiplayer_Remote.cs:280, 670).

## 7. Randomness and client authority

- Gameplay code (`Entity_*`, `Mode_*`, `Game_*`, `Helper_*` except UI) uses no `Random`; randomness is cosmetic
  (`FlipbookPlus_*`, `Bl_Sound`, `Bla_Ambient`, `Extramethods.Pickrandom`), so a client's own rolls never feed back.
  The pack's rule stays: random choices on the host from a seed, results synced.
- Owner-written values a client controls: `Loaded`, `Mods`, `Displayname` (`Loaded` false forever stalls the map
  start, §3).
- The stock build and sell handlers check that the sender owns the vessel, then trust the `Owner` written in the JSON
  (Entity_Command_Build.cs:134-135; Entity_Ai_Hero_Buildsell.cs:45-127). Among friends this is fine; a pack command
  takes the owner from `master.Entitydata`, never from the JSON.

## 8. The online evening (host + one friend; both send `Player.log`)

Before starting, compare the `Loaded Mod: <name>` lines (Bla_Mods.cs:111) in both logs. Every machine logs
`MY CLIENT IS<clientid> AND MY SESSION IS<session>` per player (Blm_Player.cs:105). These extend `docs/mode/REJOIN.md`
§6 (O1-O8) and `RESEARCH_GAPS.md` §E.

| # | Do | Expect (from code) | Log signs |
| --- | --- | --- | --- |
| N1 | Friend joins from the Steam friends list at the boat | joins, boat shown | client `STEAM JOINED Lobby`, `Client started`; host `APPROVED CONNECTION!!!!` |
| N2 | Both pick a vessel (one a pack vessel), ready up | both shown on the boat | host `HERO SELECTED <name>` per player |
| N3 | Start a map | both load and fly in | `Loading 0%` … `100%` on both; a stop at `Loading 50%` = the host waits for a client |
| N4 | A big fight with debuffs and many projectiles | effects missing only on the client = the throttle (§4) | nothing logged; by eye |
| N5 | The client uses every command of a pack vessel and the mode's picks | host decides, client sees the result | the mod's lines on both; no exception |
| N6 | An owner-only message (Vis + `OwnedLocally`, or a targeted `Pongload`) | only the owner sees it | the mod's lines |
| N7 | The client toggles speed and opens the pause menu | speed changes for all; nothing pauses | — |
| N8 | Friend pulls the cable mid-Tide, rejoins **without closing the game** | seat restored | host `PLAYER DISCONNECTED …`, later `PLAYER CONNECTED …`; **same** `MY SESSION IS` number |
| N9 | Friend ends the game in Task Manager, restarts, rejoins (same save slot) | with RogueWavesMode 0.3.0 on both, the **same** session number and the seat back (§5); the stock game gives a new one | client `identity: this player's id from the last launch is back`; **same** `MY SESSION IS` |
| N10 | A late joiner while a debuff look is on a foe | the look is missing on the joiner (§5) | — |
| N11 | A third player tries to join a running map | refused (closed lobby) | joiner `STEAM LOBBY ERROR`; no `APPROVED CONNECTION` |
| N12 | Finish the map, vote, sail on | everyone back on the boat | no `Disconnected` |
| N13 | Last: the host quits to the main menu mid-map | the client lands on the error screen | client `Disconnected` |
| N14 | Optional: friend joins with the pack disabled and the host starts a chart map | approved, then the start stalls; with RogueWavesMode 0.3.0 the host sends the friend back at 75 s and starts alone, without it both leave after 120 s | `Loading 50%`; host `load: … sent back so the others can play` |

Copy any `[Netcode]` warning into `09_PITFALLS.md` with what was being done at that moment.

**Run with a real second player, 2026-11-04** (`TDS_Mod/Tools/pc/Run-TwoPlayers.ps1`, pitfall #144; two copies of the game on one PC, UnityTransport on the loopback address): N1-N5, N7, N12, N13 and N14 pass, as do N8 and N9 as `docs/mode/REJOIN.md` O1-O3b, and N11 as O5 (`docs/reports/TEST_ROUND_2026-11-04.md`). N4 is by errors only: none on either machine, its looks unread. N6 and N10 were not run. All that is left of the evening is what loopback can't be: Steam's friends list and lobby, a real network's delay and drop, and a person's eye.

## 9. Sources

The decompiled `Assembly-CSharp.dll` (703 types), read on 2026-10-14 by a parallel reader and spot-checked by hand
(`Blm_Player` GUID, `Bla_Data.Temporaryfields`, `Game_Mapvars` throttle, `Game_Load` timeout, approval). NGO
behaviour not visible in the game's code (late-join replay, change detection by `Equals`) is NGO's documented
behaviour, not verified here.
