# 15 · The Rogue Waves mode engine: what a mod can build a roguelike run on

How the game starts, plays and ends a match, keeps players' choices, pays bounties, applies Cardinals and saves, read
for the Rogue Waves mode (`docs/scope/` in the Rogue Waves repository). Everything here was **read from the game's code**
(`Assembly-CSharp.dll` of 2026-10-08, decompiled as in `13` §11) unless it says **verified in a mod** or **untested**,
or was **proven in game** by RogueLab rounds 1-6 (§11, §15, §16), RogueWavesMode rounds 1-6 (§14.5, §17) or Riptide
(§13). `File.cs:line` points into that decompiled folder (never committed; rebuild it in a minute).

Read `01` §1, §3, §6, §8 and §11, `07` and `13` §3 first.

## 1. Verdict per scope feature

| Scope feature | Verdict | Route (section) |
|---|---|---|
| A new mode entry | **go, proven** | a mod `Modeobject` whose script names are the stock `Mode_Normal` / `Clientmode_Normal`, with Gameplan twins; Riptide runs on it (§2, §13) |
| A mode script written in the mod | **no** | `Type.GetType` can't see mod types; run the mode's logic as a host brain beside `Mode_Normal` (§2) |
| Host's campaign, vessel locked before the run | **go** | the boat's ready-up is the lock (§6) |
| Maps in a row (the campaign's 10) | **go, proven** | the party returns to the boat between maps and the boat sails the sea chart to the next one (§17; RogueWavesMode round 1). Chaining straight to the next map with `Changescene_ServerRpc("Gameloading")` is proven too (§3, §11) |
| State across the campaign's maps (Pure Aura, Directions, Depth) | **go, proven** on the host | the mod's `DontDestroyOnLoad` object, keyed by player; it survived the chain (§3, §11) |
| Showing that state on every client | **fallback** | no new `NetworkVariable`s: counters on each vessel (`01` §6), Vis RPCs, upgrades (§3) |
| Direction pick (one of three per player) | **go, proven** | a command on the player's own vessel opening a `Runprompt` dialog, the pick sent with `Command_ServerRpc`: Riptide's Salvage (§6, §13). Bring its menu up first (`Bl_Menus.Changemenu`), then it takes eight cards and returns the pick (§15) |
| Map vote (one of the 1-3 chart nodes linked to the last map) | **go** (route V1) | no boat-side vote exists: vote in game before leaving the map, with the same commands, then the boat sails to the winner (§6, §17); the in-map vote is not built yet |
| Leave or stay between Tides from Tide 15 | **go, proven** | the same vote; "leave" = the stock win path through public members, proven between Tides (§4, §11) |
| Bonus Tides | **go, proven** | a longer Tide table in the mod's mode (live writes proven by Riptide, §13), or rewind `Game_Mapvars.Entry`, which also spends a core Tide (§4, §11) |
| Pure Aura earned per Tide | **go** | host counter per player, paid on `Wave` (§3) |
| Objectives like bounties | **fallback** | Merchut's contracts are hardcoded; copy its pattern in a mod brain (§5) |
| Shopkeeper between Tides | **go, proven solo** | the game has no shop; VENDCINT is a neutral unit with buttons on Merchut's command-button pattern (§5; RogueLab rounds 4-5, §16); another player's click needs the online evening |
| Fund Transfer (aura between players) | **go** | a shop command; the host takes with `Useresources` and gives with `Giveresources(list, owner)`, both per player (§5, §14.2) |
| Directions held per player | **fallback** | the core is the team's: grant on the player's vessel and re-grant when each map starts (§14.1) |
| New Cardinals | **go, proven** | data: entry + `Artifactobject` + upgrades, registered in `Tds_Masterdatabase`; equipped from the lobby and carried to map 2 (§7, §11) |
| Buying a Cardinal mid-run | **go, proven** | core upgrades applied on the host plus the synced lobby list and `SendMessage("Updateartifactlist")` for the HUD row: Riptide's Salvage grants (§7, §13) |
| Depth (ascension, on hold), the campaign and unlocks kept in the save | **go** | `Bla_Data.Playerdata` in a folder of the mod's own; write, read back and a restart proven (§8, §11, §15) |
| Enemy modifiers Cursed, Berserk, Aging, Dazzling | **go** | `13` §6 (the recipe); Aging and Dazzling checked in §16 |
| The F-key respawn bug | **go, not reproduced** | client-side: put the main vessel first in the `Hero` tag list; a by-hand repro with a real Boutbro is still needed (§9, §11) |
| Maps from the base game | **go, proven** | Gameplans resolve by name (§3); all 45 base maps played clean in the mod's mode, never Special_Custom (§15); a mod can also ship maps as `Map` prefabs (§10; untested, and they load through the custom-map slot whose stock map hung, §15) |

## 2. Modes and how a mod adds one

The chain from `13` §3 holds. The mode's scripts are attached **by type name**:

```csharp
// Game_Rounds.Startgame (Game_Rounds.cs:257), server; Startgameclient (:264) on every machine
Mode_Base obj = (Mode_Base)gameObject.AddComponent(Type.GetType(Modegen.Modescript.name));
```

- `Type.GetType` searches the game's assembly; mod code is compiled into a separate RoslynCSharp domain
  (`Bla_Mods.cs:46,62`), so a mod's `Mode_Rogue` class is never found (read; the same wall as `09` pitfall 8 for AIs).
  `Modescript` is any asset **named** `Mode_Normal`: a name carrier, as the kit does for AIs.
- `Mode_Base` has one virtual, `Run(Modeobject)`. `Mode_Normal.Finished` is public; `Win()` and `Coredeath()` are
  private (read).
- The mod's `Modeobject` must carry the params the stock scripts read without a fallback: `Startwait`,
  `Basinthreshold` (Mode_Normal), `Tipthreshold` (Clientmode_Normal.cs:106) (read).
- `Game_Init` resolves the match's four names through `Tds_Masterdatabase.Getitem("Map" | "Mode" | "Soundtrack" |
  "Gameplan", name)` (Game_Init.cs:104-107) and waits for `Modeentry.Modeloads` and the map's groups. A mod adds its
  `Modeentryobject` and `Gameplandatabaseobject` to `Tds_Masterdatabase.Instance.Items["Mode"]` / `["Gameplan"]` on
  every machine, exactly as vessels add `Items["Hero"]` (verified in a mod for heroes; proven for modes by Riptide and RogueLab), and marks
  its load group loaded (`01` §3).
- A match always needs a Gameplan: the win path reads `Gameplan.Par` (Game_Rounds.cs:289) (read).
- The base game has a seventh mode asset, `Mode_Downpour`, besides the six in `13` §3 (asset list, §12); it is not in
  the database's Mode folder, and "Downpour" is `Mode_Normal`'s display name (RogueLab round 1).

**So the Rogue Waves mode is:** the stock `Mode_Normal` loop playing the mod's own `Wavesobject`, plus a host brain
(the mod's `Default` prefab code, as the template's server module `05_Server`) that listens to `Game_Mapvars.Wave` / `Entry` and adds
everything the run needs. The brain knows it is in a Rogue match by the mode's name in `Tds_Multiplayerinfo.Mode`.

## 3. A run across several maps, and what survives

**How a match starts** (read): only the leader picks a map (`Tds_Multiplayer_Remote.Selectmap_ServerRpc`, :648 checks
`Blm_Server.Currentleader`), which writes the networked strings `Map`, `Mode`, `Soundtrack`, `Gameplan` on
`Tds_Multiplayerinfo` (:672-675); any change un-readies everyone (:683). When all are ready, the server loads
`Gameloading` (:594), then `Game`, whose `Game_Init` reads the strings back (Game_Init.cs:96-103).

**How it ends** (read): a win sets `Gamecomplete`/`Gamestate` to "Win" and calls `Game_Rounds.Instance.Gamecompleted()`
(Mode_Normal.cs:300-309). The end screen's buttons call `Returnmap_ServerRpc` (Game_Endscreen.cs:541,549), a vote with a
30 s timer that despawns everything and loads `Setup` (the boat) through Netcode's scene manager
(Tds_Multiplayer_Remote.cs:38-54, 84-240). The network session stays up.

**Chaining maps.** Game code already changes scene from inside a mode: `Mode_Final` sends everyone to `Credits` with
`Blm_Multiplayer_Remote.Instance.Changescene_ServerRpc(scene)` (Mode_Final.cs:237), and the developer tool
`Game_Admin.Play` starts a Gameplan directly: it writes the four strings, sets every player's `Activeplayer` to 1 and
changes scene to `Gameloading` with `Lobbystate` "Closed" (Game_Admin.cs:163-185). The game's own exit despawns
everything first (`Game_Entity.Instance.Despawnall(true)`, Tds_Multiplayer_Remote.cs:30-54). The RPC runs only when its sender
is the current leader (Blm_Multiplayer_Remote.cs:154). So the host's mod, after a won map, writes the next map's four
strings on `Tds_Multiplayerinfo` and calls `Changescene_ServerRpc` with `Gameloading` (**proven**, §11). The pack does
not chain: with the sea chart (§17) the game returns to the boat after each map and the boat sails to the next one
(RogueWavesMode round 1, proven).

**What survives a scene change** (read): `Blm_Server` with `Tds_Multiplayerinfo` (spawned `Spawn(false)`,
Blm_Multiplayer.cs:279-282), `Bla_Mods` (`DontDestroyOnLoad`, :33), and the mod's `Default` prefab instance, which the
kit already marks `DontDestroyOnLoad` (`05` §1, Boot; verified in a mod). `Game_Init`, `Game_Rounds` and `Game_Mapvars` are
rebuilt per map. Statics in prefab code stay (one compile per activation, §10).

**Where run state lives:** a host dictionary on the `Default` object keyed by `Sessionid` (client ids change on a
rejoin, §14.5; a client restart changes the `Sessionid` too unless the pack keeps the GUID, RogueWavesMode 0.3.0, so
what outlives one host session keys on `Displayname` or the kept GUID, `19` §5): Pure Aura, Directions owned, map
number, Depth level (on hold). Clients can't get a new `NetworkVariable` (a mod can't add networked behaviours).
Show it through what already syncs: a counter stat on each
player's vessel (`Addstat` + `Style = Counter`, `01` §6, verified for Mummer's Thread), restored by the host at each
vessel spawn; Directions as visible upgrades on the vessel (`Upgradelist` shows on the panel); one-off messages as Vis
RPCs.

## 4. Tides: bonus Tides and leaving

- `Mode_Normal.Run` counts the table's entries once (Mode_Normal.cs:315) but reads the networked `Entry` on every pass
  (:363,366). A mod's mode simply has a longer `Wavesobject` (Tides 16+ as the bonus Tides); or the host rewinds
  `Entry` to replay entries while `Wave` keeps rising (**proven**, §11).
- The enemy core's `Healthresource` counts Tides left (`13` §3), and a rewound Tide still spends one (round 1: 2 -> 1
  on a replay). Bonus Tides by rewind must top the core up, or the run ends before the table does.
- **Leaving as a win** uses public members only: `Mode_Normal.Finished = true`, `Gamecomplete` and `Gamestate` =
  "Win", `Game_Rounds.Instance.Gamecompleted()` (read). The host's brain does it between Tides only (a `Wave` entry
  with `Wave = false` is playing), which is the scope's rule.

## 5. Merchut, bounties and the shop

There is **no shop** in the base game (no Buy/Shop/Purchase code besides Fie's shop tower buffs) (read).

**Merchut** (`Tower_Merchut`, a map building; `Entity_Ai_Tower_Merchut`): a fixed run of 20 contracts in order, each
an enemy type, a count, an elite upgrade and a reward, from private hardcoded lists (Entity_Ai_Tower_Merchut.cs:10-32).
Its button `Command_Tower_Merchut` ("Contracted") can be pressed by any player (`Subowner` 0..9, :60) and shows the
enemy's sprite, count, elite and reward (Entity_Command_Tower_Merchut.cs:53-87). On the next Tide it spawns the
foes through the enemy core's `Spawn` command, one per second, each with the elite and
`Tower_Merchut_Boost_Upgradeobject` (takes more damage, "Targeted" mark) (Entity_Ai_Tower_Merchut.cs:73-110;
Entity_Ai_Effect_Tower_Merchut.cs:6-21). The reward (red, blue, yellow aura) is split across players in the next
break; **it never checks that the foes died** (Entity_Ai_Tower_Merchut.cs:216-243). All host-side (read).

`Entity_Ai_Mob_Merchant` is a different unit (shoots unboosted foes, gives Buster an artifact badge) (read).

**For the mode:** the contract lists can't be extended, but every piece is public: the core's `Spawn` command and
`Spawndata` (Entity_Ai_Main_Enemycore.cs:10-15,68), `Helper_Resources.Giveresources` (Helper_Resources.cs:75), command buttons any
player can press. A mod brain runs its own objectives (with a kill check) and its own shop (commands on a shop
building or on each vessel, priced in Pure Aura). Fund Transfer is a shop command that moves resources from one
player to another on the host (proven solo: each colour moved by exactly 40, RogueLab round 5 probe F1; between two
players it waits for the online evening).

## 6. Choices on every client

**The only client→host channel with a payload and an owner check is an entity command** (read; the payload-free ones
are `19` §6 rules 5-6):
`Entity_Master_Remote.Command_ServerRpc(string Link, string Json)` (Entity_Master_Remote.cs:43) is accepted when the
sender is the entity's owner or a subowner, the link is in `Commandlistserver` and the command is `Controllable`, and
then calls the entity's `Command(Link, Json, Server)` (:84; Helper_Entitytools.cs:342). The handler isn't told who
sent it: identify the voter by the entity's owner (a command on the player's own vessel) or the Json. Buttons show
only to an owner or subowner on the same team (Game_Ui.cs:298); the host can set `Subowner` (Game_Players.cs:77-84).

Not usable as a menu: `Entity_Command_Itemselect_Base` is a "click a spot in the world" helper; `Selectother` and
`Blurryphoto_Select` only change the local selection (read). Chat's RPC is private, but its `Chat` list is public: the
host can write a line for everyone (`Chat.Add`) and read every player's lines (`20` §2).

| Choice | Route | Verdict |
|---|---|---|
| Direction (each player, one of three) | three commands on the player's own vessel; the host applies the pick. Nicer: a client `Bla_Ui_Confirmation.Instance.Runprompt(entries, title, sub)` dialog (Bla_Ui_Confirmation.cs:342, used in game by the pause menu) opened by a host Vis RPC, answered through the vessel command (proven by Riptide from a vessel command, §13, and by RogueLab with eight cards once its menu is up, §15) | go |
| Map vote | no boat-side vote (only the leader picks, `Changehero` takes only hero names); vote in game with the same commands over the 1-3 chart nodes linked to this map, then the party returns to the boat and it sails to the winner (route V1, §17). Every map is voted (user, 2026-10-08), so map 1 needs a staging step: the leader's pick loads and the opening break is held while players pick Directions and vote | go (in-map vote not built yet) |
| Leave or stay | the same vote with a host timer and count; the built-in `Returnmap` vote only exists after a map ends | go |
| Vessel lock | ready-up: the select screen opens only while not ready (Macro_Worldvisual.cs:617); `Startgame_ServerRpc` waits for all (Tds_Multiplayer_Remote.cs:542-569). `Changehero_ServerRpc` itself checks nothing | go |

A mod builds itself, every time: one choice per player, the count, a timeout or host override, and the shared display.

## 7. Cardinals (artifacts in the code)

- **Chosen in the lobby** from the player's unlocked ones (`Unlock_Artifact_<name>` in the save,
  Macro_Setupui.cs:264-273): one per colour plus one "Mixed" slot, sent by `Changeartifact_ServerRpc`
  (Tds_Multiplayer_Remote.cs:296,343-372), kept in the networked `Tds_Multiplayerinfo.Artifacts` + `Mixedart`
  (Tds_Multiplayerinfo.cs:21-23) (read).
- **Loaded** on every machine at match start: each `Artifactentryobject.Artifactloads` group, then the
  `Artifactobject` into `Game_Mapvars.Artifactobjects`, a plain local list (Game_Init.cs:72-92;
  Game_Client_Init.cs:81-89) (read).
- **Applied once**, when the player core gets its "Spawn" command (sent by the mode after `Startwait`): `Applyartifacts`
  force-applies each artifact's `Coreupgrades` to the core (Entity_Ai_Main_Core.cs:181; Helper_Entityai.cs:242-250).
  Those upgrades' AIs do the work: `Entity_Ai_Artifact_Teamapply` / `Towerapply` add upgrades to new towers and units at
  `Game_Entity.Onspawn`, `Heroapply` to heroes, `Coreapply` to cores, each naming its upgrades in `Defaultparams`
  (read).
- **HUD:** `Game_Mapvars.Artifacttrack` is a networked list of exactly 8 floats indexed by artifact position
  (Game_Mapvars.cs:125-127); `Game_Artifacts` builds the icons once at init (:120-134) (read).
- `Map_Cardinalconditions` is only a list on the Discoveries screen (Macro_Discoveriesui.cs:43,168).
  `Artifactentryobject.Leaderboard_Multiplier` is the score multiplier (out of scope) (read).

**A new Cardinal** is data: an `Artifactentryobject` in `Tds_Masterdatabase.Instance.Items["Artifact"]` (public,
filled only from the game's database, Tds_Masterdatabase.cs:9,42-63), its `Artifactobject` in a loaded group, its
upgrades, and the unlock key `Unlock/Unlock_Artifact_<name>` in the save so the lobby offers it (**proven** with the colour slot's RPC, which checks the name, and it carried to map 2;
the picker's look by hand is still unseen, §11).
**Mid-run:** the host applies the core upgrades itself with `Helper_Entitytools.Upgrade(core, up, Forced, artifact)`
(Helper_Entitytools.cs:484), the call the start of a match uses; to show it, every machine also appends to
`Artifactobjects`, within the 8 counter slots, and the HUD (private builder) must be redrawn (**proven** by Riptide
with `SendMessage("Updateartifactlist")`, §13).

## 8. The save

`Blockage.Saveold.Bl_Save` is dead code. The live save is `Blockage.Tags.Bla_Data` (read):

- `Bla_Data.Playerdata` (public static `Datacontainer`, Bla_Data.cs:59), written with Easy Save 3 to `SaveFile.es3`
  and slot files `SaveFileSlot1..10.es3`, `SaveFileSlotAuto.es3` (:36-42,115,133,142). Each slot is a profile.
- Folder + key → float or string: `Adddata(folder, name, float, Addtype)`, `Adddata(folder, name, string)`,
  `Readdata(folder, name)` → `Entry {Value, Valuestring, Loaded}` (Datacontainer.cs:33,77,121).
- Unknown folders and keys survive a load and save; no checksum or version check (Datacontainer.cs:15-31;
  Bla_Data.cs:324-327). The `Metadata` folder is wiped on every load (:57,199-208): never keep pack data there (the
  one write that makes sense is putting the player's own GUID back, `19` §5).
- A slot switch replaces `Playerdata`: never cache it; re-read after `Bla_Data.Instance.Onslotchanged` and
  `Onslotremoved`, which a clear fires instead (:271-274, 317, 339-342).
- The game saves at match end, on the boat and on quit (Game_Endscreen.cs:514, Macro_Main.cs:516, Bla_Data.cs:150),
  and on a slot switch or clear; the match-end save comes before the match's stats are written (`23` §1.2).
- Unlocks: `Tds_Data.Checkunlocks` runs every `Items["Unlock"]` entry's trackers and writes `Unlock/<Dataadd>` = 1 and
  `Unlock_Notif/<Dataadd>` (Tds_Data.cs:23-63). `Unlock_Tracker_Conditions` can test any folder and key;
  `Unlock_Tracker_Matchreader` reads only the game's own last-match JSON; `Unlock_Tracker_Monobehaviour` finds its type
  with `Type.GetType` (no mod types). Steam achievements check inspector-set folders and keys
  (Bla_Data_Steamachievements.cs:83-105): a key in the mod's own folder can't trip one.

```csharp
// Depth, the ascension level (on hold; write, read back and a restart proven; folder and key names are the pack's, globally unique: Readdata(name) searches every folder)
Bl_Tags.Entry e = Bla_Data.Playerdata.Readdata("RogueWaves", "RW_Depth");
int level = e.Loaded ? (int)e.Value : 0;
Bla_Data.Playerdata.Adddata("RogueWaves", "RW_Depth", (float)(level + 1), Bl_Tags.Addtype.Replace);
```

Always pass the typed third argument (two `Adddata` overloads with optional parameters).

## 9. The F-key respawn bug

- The "Focus" action calls `Game_Selection.Onfocus` (Game_Selection.cs:719). "My vessels" are the entities in
  `Game_Entity.Instance.Entityclienttags["Hero"]` owned by the local player, **in the order they were added**
  (`Getplayercharacter`, :575-597). Any minion whose data tags it `Hero` (Boutbro) is in that list.
- `Onfocus` caches the list; when one entry is destroyed it rebuilds it and sets `Cycle = 0` (:638-657). Then, if the
  current selection is a Hero it selects `Heroselect[Cycle]`; if nothing (or a non-Hero) is selected, which is the
  usual case right after a death, it selects the Hero **nearest the camera's `Movepos`** (:659-678).
- A respawn spawns a **new** vessel entity (Entity_Ai_Main_Core.cs:126-153); the dead one leaves the tag list
  (Entity_Master_Client.cs:120-125) and the new one is appended (:176-178). The list is now [Boutbro, vessel] and
  `Cycle = 0` lands on the minion; with nothing selected, the minion wins whenever it is nearer the camera than the
  fresh vessel at the Basin. Order or distance, not a stale reference (read).

**Fix (compiled and run, but the bug did not reproduce, §11):** a client-side handler added to
`Bl_Inputs.Actions["Focus"].Function` after the game's own: on the first press after the local vessel respawned, it
selects the main vessel (its `Being` equals `Getload("Hero", Tds_Clientinfo.Hero)`; `Game_Selection.Selected` and
`Select()` are public) and moves it to index 0 of `Entityclienttags["Hero"]` so later presses cycle from it. It works
for Boutman too, while the pack is loaded (RogueLab's `RogueLabFocusFix`, now kit 1.6's `ModKitFocus`). It cannot be
done in `Game_Entity.Onspawn` (server-side, Game_Entity.cs:294-296); the tag list is built on each client. For the
pack's own minion vessels the same fix applies; dropping `Hero` from a minion's tags also removes it from F but costs
hero artifacts (`Artifact_Heroapply`), Halberd's dive targeting and Drinkstand support (read), so keep the tag and
reorder.

## 10. Mod loading facts that shape the pack

- All enabled mods load at boot, 5 s after `Bla_Mods.Start`, in ModTool's discovery order; the game never sorts them
  (Bla_Mods.cs:60-70,112,135-144) (read). A modded launch on 2026-10-08 loaded Crackpot, Rustle, Sprigg, Mummer,
  Riptide, Argo: not alphabetical (verified, Player.log). No mod can count on running after another.
- Prefab keywords activate by `IndexOf` on the name: `Default` when the mod loads, `Mainmenu` at each main menu (most
  likely not the first one after launch, `23` §4.1),
  `World` on the boat, `Loading`, `Gameserver` (server, after `Startgame`), `Gameclient` (Bla_Mods.cs:40,113 and the
  callers) (read). Every match of every mod activates, and **each instance's code is compiled again into its own
  assembly**: statics are per prefab per activation, and no mod sees another's types (read; reflection is blocked).
  Mods meet only through game state: GameObjects, `Tds_Masterdatabase.Items`, `Bl_Address` tables.
- Nothing in a bundle is registered automatically: code adds it (`Items[...]`, `Bl_Address`) (read).
- `Autoloadscenes` is never read; the game never loads a mod's scenes (read). **Maps ship as prefabs:**
  `Custom_Map_Overworld` collects every loaded mod prefab whose name contains `Map`, sorted by name, and fills a map
  slot from its `Mods_Mapsettings` (`Gamemode` by name, lighting, atmosphere, `Mapdata`, `Mapvisuals`, difficulty,
  name, colour, image) (Custom_Map_Overworld.cs:19-90; the component ships in the Mod Tools package's
  `Assets/Exporteradds`). That slot is per machine, unrecorded and edits stock data, and its load hung (`09` #78):
  **pack maps register like the mode instead** (`22` §3, decompile 2026-10-14). The package's
  `ModToolSettings.supportedContent` is 7 (assets, scenes and code) (read).
- Being modded switches off leaderboard records (Game_Endscreen.cs:57), for the whole lobby when one player is modded;
  stats, unlocks and medals still count (`23` §5).
- A compile error in one `Default` prefab skips that mod's later `Default` prefabs, and **`Toggle` undoes nothing**
  (code, hooks and statics stay): players restart the game after enabling or disabling the pack (`23` §4.3).

## 11. PC research list (one build, one launch, then one online check)

Built as the throwaway mod `TDS_Mod/ModSource/RogueLab`, which plays steps 1-9 by itself on a solo host (its
`README.md`). **Round 1 ran on 2026-10-08: 13 pass, 0 fail, 0 setup**, Boutman on 3B_Thehunt chained into 0B_Hydragen.
Every log line is in `reference/roguelab_round1.txt`. Results:

| Step | Result |
| --- | --- |
| 1 Database | 6 modes (Mode_Normal shows as "Downpour", 30 Tides; Storm "High Tide" 15; Drought "Flood" 20; Tsunami 4), 51 Gameplans, 54 maps, 40 artifacts, 105 unlock keys; Mode_Normal's 60-entry table and params logged |
| 2 Mode entry | **proven**: the match played `Mode_RogueLab` (stock scripts, 3-Tide table) on a `RogueLab_` twin Gameplan swapped in on the boat |
| 3 Chain | **proven**: after a win, map 2 loaded straight from map 1 (`Despawnall`, four strings, `Gameloading`); the mod object and its dictionary survived, and the mode and Cardinal carried over |
| 4 Leave, rewind | **proven**: the win path between Tides ends the map as "Win"; rewinding `Entry` replays a Tide, and the core still loses one (§4) |
| 5 Save | write and read back **proven**; surviving a restart **proven** in round 2 (§15) |
| 6 Cardinal | **proven**: a mod `Artifactentryobject` (a copy of Torrid Badge) accepted by the colour slot's RPC, its group loaded, its core upgrades applied; a stock artifact granted mid-match too |
| 7 Prompt | the dialog ended **Faulted**: round 2's log shows `Runprompt` threw a `NullReferenceException` after building one card, not a timeout (§15). The host still needs its own timeout to pick and close an unanswered one (Riptide's `Abandon`) |
| 8 F key | **not reproduced**: the mod-spawned Boutbro was not counted as the player's (not in the owned `Hero` list), so F found only the vessel. Needs a hand check: play Boutman, let Boutbro spawn, die, press F |
| 9 Enemies | 8 stock foes with stats and drops (Claymore is loaded but in no Tide); Hardened, Tempered, Superheated, Fractured, Annealed with params; Explode and Weakened are **not** in Mode_Normal's group |

Exceptions: one `OperationCanceledException` in `Macro_Setupui.Mapselected` right after the boat-side swap (harmless,
the match loaded) and five `NullReferenceException`s from the game's own teardown at quit. Steps 10 and 11 stay manual.

1. **Database:** keys and counts of `Tds_Masterdatabase.Items` for `Mode`, `Gameplan`, `Map`, `Artifact`, `Unlock`;
   every field of `Mode_Normal`'s `Modeobject` (`Defaultparams`, each `Waves` and `Wavecontent`) and of one Gameplan.
2. **Mode entry:** register `Mode_RogueLab` (scripts named `Mode_Normal`/`Clientmode_Normal`, a 3-Tide table) with a
   Modeentry and Gameplan; after the leader picks a base map, the host overwrites `Tds_Multiplayerinfo.Mode` and
   `Gameplan`; the match must start and play the test table.
3. **Chain:** on that match's win, the host writes the next map's strings and calls `Changescene_ServerRpc`
   (`Gameloading`); the second map must load and a host dictionary on the `Default` object must survive.
4. **Leave:** force the win path between Tides 1 and 2; then rewind `Entry` once and log `Wave` and the core's
   `Healthresource`.
5. **Save:** write `RogueWaves/RW_Test`, switch slots, restart, read it back.
6. **Cardinal:** register a test `Artifactentryobject` + unlock key; it must appear in the lobby picker; grant a stock
   artifact's `Coreupgrades` mid-match and log the effect and the HUD.
7. **Prompt:** open `Runprompt` on the host and log the returned entry.
8. **F key:** with Boutman, die, respawn, press F; then apply the reorder fix and repeat.
9. **Enemies** (`13` §10 Q1, Q2, Q4): the Sprigg `Enemies()` dump; `Display_Name`, `Display_Desc`, `Defaultparams` of
   the seven elite upgrades; `Database.Init` group names and `Bl_Address.Groups["Mob"]` (each list's group and item
   names) at mod load and again in a match.
10. **Packaging** (`14` §6 Q1, Q3): a `Map_Test` prefab with `Mods_Mapsettings` shows in the custom-map slot; log the
    deny lists and P/Invoke switch of the game's `RoslynCSharpSettings` (its reference list is already in
    `Build-<Name>Mod.ps1`).
11. **Online, with a friend:** steps 2, 3, 7 and 8 on the client side.

## 12. Sources

- The decompiled `Assembly-CSharp.dll` (703 types; `13` §11). Paths above are its files.
- `TDS Mod Tools.unitypackage` (2026-10-08 copy): `Assets/Exporteradds/*.cs`, `ModTool/Resources/*.asset`, and the
  base game's Addressables entry list in `AddressableAssetsData/AssetGroups/Default Local Group.asset`, saved as
  `reference/addressables_default_group.txt` (1436 asset paths: the mode, map, artifact, being and upgrade names of
  that copy; `Mode_Tsunami` and `Map_Manafoldold` are not in it).
- `Player.log` of a modded launch, 2026-10-08.

## 13. Already proven in game: Riptide

Riptide (`TDS_Mod/ModSource/Riptide`, PR #1; Dimitri's 2026-10-04 prototype: one Basin, 30 random Tides, a Salvage of
artifacts between them) passed 70 of 70 PC probes in round 3. Its `IMPLEMENTATION_NOTES.md` is the evidence; what it
settles here:

- A mode entry cloned from `Mode_Normal`'s, its `Modeobject` in a same-named `Bl_Address` group, and Gameplan twins
  load and play like stock (probe 1). The end screen saves records under the twin's name.
- `Mode_Normal` reads each Tide entry live on the host: a Tide written during the break before it is what spawns, and
  elites through `Wavecontent.Upgrades` spawn with their upgrade and skin (probe 2).
- An artifact granted mid-match (`Upgrade(core, up, Forced, art)` plus catch-up for apply-kinds), added to the synced
  lobby list with `SendMessage("Updateartifactlist")`, acts like a lobby pick and shows on the HUD (probe 3).
- `Runprompt` works from mod code; a per-vessel `Commandobject` button reopens it; the pick reaches the host through
  `Command_ServerRpc`; holding `Time_Wave` keeps a break open (probe 4, round 2). The dialog needs re-centring for
  three or more cards (`RiptideSalvage.Centre`).
- `PlayerPrefs`, named messages and `OnClientConnectedCallback` pass the game's code security (probe 5).
- A Setup-screen switch (a copy of the alternate-map toggle) is a working start route, a fallback if the main-menu
  button (§14.8, `docs/mode/MAIN_MENU_START.md` in the project) ever fails.

RogueLab round 1 (§11) then proved the rest of the run's skeleton: the mod Cardinal from the lobby, the chain to a
second map with host state, leaving between Tides as a win, rewinding `Entry`, the save keys.

## 14. Second reading (2026-10-09)

The research list in the Rogue Waves repository (`docs/RESEARCH_GAPS.md`) asked the decompiled game a second set of
questions. All **read**; RogueLab round 2's results are §15.

### 14.1 Cores and per-player upgrades

- The map places the cores; `Mode_Normal` finds them by the `Core` tag (Mode_Normal.cs:350-358) and any core's death
  loses the match (:236-260). One core spawns a vessel for every active player of its team, with `Owner` = that player's
  `Sessionid` and the core as parent (Entity_Ai_Main_Core.cs:95-97, 184-195). The core has no owner.
- `Artifact_Heroapply` upgrades every hero child of the core, so every player's vessel (Entity_Master.cs:333-335,
  Entity_Ai_Artifact_Heroapply.cs:7-17). `Artifact_Towerapply` hooks `Game_Entity.Onspawn` and its team check compares
  the artifact's entity with itself (Entity_Ai_Artifact_Towerapply.cs:14), so it upgrades **every** new `Tower`.
- So a per-player Direction can't sit on the core. Put it on the player's vessel (owner = `Sessionid`), re-grant it from
  host state at each map's first spawn (a respawn copies the dead vessel's upgrades, Entity_Ai_Main_Core.cs:137-144; a
  repeat grant is ignored), and give tower Directions through the mod's own `Onspawn` hook filtered by owner.
  `ModKitDirections` (kit 1.6) grants on the core and needs this change before the mode uses it.

### 14.2 Resources and moving aura

- Aura is per player: `Helper_Resources.Give` adds to `Game_Players.Prevsessionlist[Owner].Resources`
  (Helper_Resources.cs:16-29); `Owner = -1` pays every active player, `Split` divides first (:50-70). `Give` scales by
  `Game_Mapvars.Resourcegainmultiplier` (:21), which Cardinals change (`Artifact_Bottomrock`); `Mode_Normal` sets it to
  1 only around the start aura (Mode_Normal.cs:359-362), so Tide pay (:66) is scaled. Set it to 1 around a transfer.
- The stock **Money Bag** (`Artifact_Pig`, "enables donation") already moves aura: its button needs 25 of each aura
  (Entity_Command_Artifact_Pig_Launch.cs:30-47), the host spends it from the thrower and throws a bag; a same-team
  entity with an owner that it hits pays that owner `Amount - 1` of each (Entity_Ai_Effect_Artifact_Pig.cs:72-165). Its
  logic is private; a mod transfers directly with the public `Useresources` and `Giveresources(list, owner)`.
- Enemy `Drops` are paid on a death despawn to every player, split (Game_Entity.cs:413-415); no object lands on the
  ground.

### 14.3 Difficulty levers

- Nothing scales foes with the player count or the map: `Mapentryobject.Difficulty` only draws icons and the "TYPE"
  label (Macro_Worldui.cs:101-115, Macro_Setupui.cs:187); `Gameplan.Par` feeds the par time, leaderboard points and the
  par medal. A foe's level is the highest `Priority` among its upgrades (Helper_Entitytools.cs:528-532), and foes get
  upgrades only from the Tide entry's `Content.Upgrades` (Mode_Normal.cs:118-142). `Levelmod` = `Leveladd × (Level-1)`.
- The game's own **easier** setting ("Treated", "The water is calmer than normal"; round 2): with `Difficultysetting` =
  1 (Macro_Gamecore.cs:72) the core gets `Special_Treated_Upgradeproperties` with its "Spawn" command
  (Entity_Ai_Main_Core.cs:174-178); its AI scales the start aura and adds drops, and marks the match `Mode_Treated`
  (Entity_Ai_Main_Core_Treated.cs:19-30, 136). Its params (verified): `Aura_Base` 2, `Aura_Drop` 4, `Aura_Basemax` 500,
  `Extra_Time` 3, `Extra_Wavetime` 30, `Health_Base` 200: an extra twice the mode's start aura split among the players
  (at most 500), each kill paying its drops ×4 plus 4× in each aura it doesn't drop (split), 3 s more in every break and
  30 s more per Tide, +200 base health on every core (:29-30, 63, 70, 145-154). The setting is only ever 0 or 1
  (Macro_Gamecore.cs:56, 72): there is no harder one.
- So a run gets harder only through what the mod writes: the Tide table (counts, intervals, more entries, elites, all
  proven live), the Treated upgrade forced on the core (to ease), `Resourcegainmultiplier`, and run modifiers of its
  own. A 4-player run faces the same Tides as a solo one unless the mode scales them.

### 14.4 Maps and modes

- Map, mode and soundtrack are three strings loaded separately (Game_Init.cs:96-119): any map takes any mode. The
  coupling is data: the mode's `Specialspots` go to named children of the map's `Mode` transform, and a missing one only
  logs "Mode might not work properly" (Game_Rounds.cs:72-98); map buildings (Merchut, gates) come from the map's
  `Object` child whatever the mode (Game_Grid.cs:656-735); beings under `Entities` are swept by `Despawnanonblock`
  unless their block adds a node (corrected by the decompile, `22` §2). No code names a map except `Special_Custom` (no
  records). Alt maps are a Gameplan swap behind `Unlock_Altmaps` (Tds_Multiplayer_Remote.cs:662-664). RogueLab round 2
  played 28 maps in the mod's mode and round 3 the other 15, all clean (§15).

### 14.5 Players who drop

- The player key within one host session is the `Sessionid` (long): the host maps each client's saved GUID to a
  number and reuses it on a rejoin (Blm_Player.cs:79-92, Blm_Multiplayer.cs:457-466). The GUID sits in the save's
  `Metadata`, which every load wipes, so **a friend who restarts the game comes back with a new `Sessionid`**; only a
  drop with the game still open keeps it (decompile 2026-10-14, `19` §5), unless the pack keeps the GUID and puts it
  back (RogueWavesMode 0.3.0, `19` §5). Netcode client ids and `Tds_Clientinfo` are
  not stable; the Steam id is stable but out of reach for mod code; `Displayname` survives a restart. Run state is
  keyed by `Sessionid` (RogueLab and Riptide already do).
- A drop despawns nothing: teammates become subowners of the dropped player's entities, and lose that on a rejoin
  (Game_Players.cs:24-88). A rejoin gets a fresh `Tds_Clientinfo` with `Activeplayer` 0 and no aura
  (Tds_Clientinfo.cs:25,37).
- A match closes the lobby (`Lobbystate` "Closed", which the chain also sends), and a closed Steam lobby refuses joins
  (Blm_Multiplayer.cs:67-77, Blm_Multiplayer_Remote.cs:278-297): a dropped friend can't rejoin a running map.
- **A mod can reopen it** (read, 2026-10-11; built in `RogueWavesMode` 0.2.0-0.2.1, `docs/mode/REJOIN.md` in the project;
  0.2.1 passed solo PC round 6, 47/0, and the real drop and rejoin waits for the online evening, `docs/mode/REJOIN.md` §6):
  `Blm_Multiplayer_Remote.Instance.Changelobbystatus_ServerOnly("Open")` is public and takes a string, so it needs no
  Steamworks type (mods can't reference Steamworks); `Blm_Multiplayer.Instance.Server_Serveropen` is the public flag the
  connection check reads. `Game_Load` shuts the lobby again when its loading screen ends (Game_Load.cs:261, "Loading
  90%"), about 10 s after the fly-in and well after `Gameloaded` is true: verified on the PC (round 5, 2026-10-11),
  where it shut a lobby the mod had opened a moment before. A mod that keeps it open re-checks every frame.
- **A late joiner** gets the running `Game` scene and every networked thing from Netcode, and the map's client start
  runs by itself (`Game_Client_Init.Initalize`, Game_Client_Init.cs:39-177: groups, local `Loaded`, the mode's client
  script). Each thing's client side waits for that `Loaded` (Entity_Master_Client.cs:138) and then 10 s for its being's
  group; the HUD reads the current wallet and Tide (Game_Ui.cs:1745). Of the start, only `Game_Client_Init.Startvisuals`
  (camera fly-in, HUD start, game-speed hook, the mods' `Gameclient` prefabs) is missed (with every state sent by RPC,
  `19` §5): just the loading scene calls it (Game_Load.cs:245, :293), so a mod calls it (public) once the local `Loaded`
  is true. Nothing on the host waits for clients after the first Tide's start check (Game_Init.cs:123). A late joiner is
  a client whose `Game_Init` spawns with `Gameloaded` already true.
- The host can restore a rejoin's `Tds_Clientinfo` (all server-written): `Hero`, `Team`, `Activeplayer` (set 1 before
  `Activeplayerdata`: a change to 0 resets it), `Activeplayerdata`, `Playerstatsjson`, `Resources`. A host chat line:
  `Blm_Ui_Chat.Instance.Chat.Add(new Link_Chatcontainer { Sender, Text })` (a server-written `NetworkList`). The main
  menu's error box shows the last line of `Blm_Multiplayer.Instance.Multiplayererror`.

### 14.6 The choice dialog

`Runprompt(entries, title, sub, Forcedisplaymenu)` (Bla_Ui_Confirmation.cs:342). An entry has `Name`, `Display_Name`,
`Display_Color`, `Display_Desc` (shown on hover), `Display_Image`, `Display_Imagecolor`, `Tomenu` (:16-31): no price, no
disabled state, so a shop writes the price in the name and refuses on the host. One card per entry with no limit and no
scrolling in code (:149); the list's `GridLayoutGroup` is a mod-adjustable default (Riptide and RogueLab use up to
4 per row). The first click wins; no timeout and no cancel (:196-231). It hands the gamepad focus to its parent
`Blu2_Menu_Handler` per card (the **last** card becomes the pad's start button), so it must open while that menu is up
(§15); its own `Changemenu` isn't forced and is dropped during a transition, so open the menu with `Force`, wait,
then call with `Forcedisplaymenu: false` (`20` §2, `examples/recipes/RecipeUi.cs`).

### 14.7 Damage, pickups, light, changing towers (vessel mechanics)

- **Hurt path:** `Interacthurt` puts the attacker's `Damage` and `Damagemulti` = 1 in the data and sets `Lasthit`
  (Helper_Entitytools.cs:166-170), then the attacker's AIs' `Interact`, then the target's AIs' `Oninteract` in
  `Priority` order (:138-151). The target's own general AI applies it at priority 10 (Entity_Ai_Tower_General.cs:15,28;
  Entity_Ai_Mob_General.cs:28), so an AI below 10 can scale or zero `Damagemulti` first (Fiewall protection at 9,
  Entity_Ai_Effect_Fiewall_Protection.cs:7,14; Subcore zeroes it). `Dodamage` drains `Healtharmor` (1 per hit),
  `Healthover`, `Healthresource`, then `Health` (Helper_Damagetools.cs:17-64). There is no damage-taken stat;
  `Entity_Master.Damagetaken` is a running total. **Reflect** = hurt `Interact.From` back (Smackywall's Spike does,
  Entity_Ai_Tower_Smackywall.cs:173-177); **delayed damage** = cut `Damagemulti` and deal the rest later;
  **Illuminated** = a debuff AI below 10 that raises `Damagemulti`; **Blinded** = a modifier on the foe's `Damage`.
- **Pickups:** the stock aura pickup (`Special_Aurapickup`) checks each slow tick for a same-team `Mob`-tagged entity
  within 1 unit, pays its owner with `Giveresources`, and expires after 8 s
  (Entity_Ai_Artifact_Checklist_Special_Pickup.cs:20-51). Boutman's fish carry no resources.
- **Light:** no gameplay light exists (lighting is rendering only). A tower's light is a number the mod sums from
  sources in range with `Helper_Entityquerytools.Entityarea(pos, radius, query, sort)` (Helper_Entityquerytools.cs:217).
- **Changing a stock tower:** an upgrade adds AIs and fires `Master.Upgrade` (Helper_Entitytools.cs:504-550); data can't
  remove a base AI, code in an added AI can change what the tower does.
- **Journal:** pages are `Logobject` assets (Logobject.cs:8-55); no code lists enemies, so the pack's foe pages are its
  own (`06`).

### 14.8 The main menu and the boat lobby (2026-10-09, read)

- **The boat is the lobby.** Main menu Play loads `Setup` (Mainmenu_Main.cs:49-80; a save without
  `Tutorial_New_Game` gets the tutorial prompt first, :51). On the boat, `Macro_Init` starts the host when not already
  connected: `Starthost(4)`, then `Macro_Main.Startup` (Macro_Init.cs:17-22,39-46). `Starthost` creates the Steam lobby,
  public and joinable (Blm_Multiplayer.cs:370-417, 218-236). The boat's Invite button opens Steam's overlay
  (Macro_Main.cs:558; Blm_Multiplayer.cs:468). The game also hosts from the main menu once: the tutorial, straight into
  `Gameloading` (Mainmenu_Main.cs:133-163).
- **Joining:** a Steam join request is taken when `Client_Canjoin` is 1, or 0 and the joiner's scene is in
  `Joinablescenes` (data); the joiner loads `Clientjoinscene` and starts the client (Blm_Multiplayer.cs:149-196,321-353).
  A late joiner whose host has a node picked opens the Setup panel on it (Macro_Main.cs:445-453).
- **Start:** each player's ready sets `Activeplayer`; when all are ready the server closes the lobby and loads
  `Gameloading` from `Setup` with whatever four strings are set (Tds_Multiplayer_Remote.cs:509-598). Picking a node
  writes them from the node's Gameplan and unreadies everyone (:600-696); a mod rewrites them after (§3, Riptide).
- **Prefab keywords:** `Mainmenu` activates at each main menu (Mainmenu_Main.cs:362); `World` only on the host's boat
  (Macro_Main.cs:395, inside the host branch), so client-side boat UI runs from the `Default` prefab's `Update`.
- A client without the match's mode or Gameplan entry stops loading on a null entry, and the host waits for every
  client's `Loaded` (Game_Client_Init.cs:101-104; Game_Init.cs:123-134); after 120 s every machine, the host too,
  leaves for the main menu (Game_Load.cs:36-70; `19` §3). The start-from-main-menu route built on these
  facts: `docs/mode/MAIN_MENU_START.md` in the Rogue Waves repository.

## 15. RogueLab rounds 2 and 3 (2026-10-09)

RogueLab on the PC, solo host, Boutman on 3B_Thehunt and 0B_Hydragen, then a tour of the base maps. **Round 2** (0.2.0):
15 pass, 1 fail, 0 setup; the tour hung loading Special_Custom and the 75-minute timeout ended the run. **Round 3**
(0.3.0, the dialog with its menu up and the 15 maps round 2 left): **16 pass, 0 fail, 0 setup**, `TEST DONE`. Every
line is in `reference/roguelab_round2.txt` and `roguelab_round3.txt`; all **verified** unless marked read.

| Question | Result |
| --- | --- |
| Save across a restart | **proven**: the counter round 1 wrote read 2 on the next launch, 3 on the one after |
| Several `Default` prefabs in one bundle | **proven**: `RogueLabMod_Second_Default` (its own code) started at boot and its marker was found in the match. A pack can split its code over prefabs, each compiled alone (`14`) |
| Loading another mode's group | `Bl_Address.Load` on each mode's folder at boot loaded all five in seconds; their elites and tables are in `13` §10 Q2-Q3 |
| Other modes | Storm, Drought and Tsunami run the `Mode_Normal` script with their own params and tables; Final and Tutorial have their own scripts. Start aura: Normal, Storm, Drought 50 of each; Tsunami 500; Final 100; Tutorial none |
| Treated | the easier setting, params in §14.3 (off on this PC: `Difficultysetting` 0) |
| Map tour | **all 45 base maps clean** in the mod's mode: 28 in round 2 (every `Mode_Normal` Gameplan but the two played, clean in round 1), the 15 whose Gameplan names Storm, Drought, Tsunami or Final in round 3 (now twinned too). Each loaded in 14-20 s with the vessel, one core (two on Splithead and Splithead δ), 1-4 enemy cores, subcores on Artrio (2), Hold (2), Elgu Bounde, Crimson Lythe δ and Crescent Valeur δ (1), and no "might not work properly" warning. The probe found no Merchut on any map (it looks for a `Tower`-tagged `Merchut` being; untested whether that's how the map places it) |
| Special_Custom | **never offer it**: loading Map_Custom (six sub-loads, the player's custom-map slot) threw `InvalidKeyException` for two keys and a `NullReferenceException` in `Bl_Address.Load` and never finished |
| Map labels | `Mapentryobject.Difficulty` runs 1-9 (Ome Drydock δ 9, Thanh Bitterfield δ 8, most 4-6), Final and Custom 0: the MAP lines are a ready table for a vote's danger label (it still scales nothing, §14.3) |
| Swarm cost (host) | Boutbros added on the host, frame time over 5 s. Round 2: 0 → 8.8 ms mean (114 fps), 50 → 10.7, 100 → 10.9, 200 → 12.1 ms (82 fps), worst 35 ms. Round 3: 0 → 7.0 ms (143 fps), 50 → 8.3, 100 → 9.5, 200 → 10.4 ms (97 fps), worst 21 ms. 200 extra units cost about 3.5 ms here; the client half needs the online evening |
| Choice dialog | Round 2 **failed**: the 3-card and the 8-card `Runprompt` both threw a `NullReferenceException` inside `Runprompt` after building **one** card. For each card it sets `GetComponentInParent<Blu2_Menu_Handler>().Gamepadstartbutton` (Bla_Ui_Confirmation.cs:190), and `GetComponentInParent` skips inactive objects: opened by a coroutine 3 s into map 2, the dialog (its own menu, `Confirmation`) was inactive. Round 3 **proved the fix**: `Bl_Menus.Changemenu("Confirmation")`, wait until that lookup is not null (1.3 s), then `Runprompt` built all 8 cards and a click on card 1 returned it (`Succeeded`, no exception in the log) |
| Eight cards on screen | the cards are 180×180 px on a 2560×1440 screen. The stock grid (2 per row) stacks 8 cards in 4 rows and the last 2 rows fall off the screen; set the grid's `constraintCount` to 4 and all 8 fit (two rows). Put the price and the gist in the name; the description shows on hover |
| F key | not reproduced again: the mod-spawned Boutbro has owner -1 (the player is 1), so it is never in F's list. Still a hand check |
| Exceptions | per map change an `OperationCanceledException` in `Blu2_Menu_Handler.Animin` (the game's own); round 2 also the two `Runprompt` errors and Special_Custom's load errors; round 3 only one more, `Game_Ui.OnDestroy` at quit |

0.2.0 ran its boot dumps twice (a doubled call); the reference file keeps one copy.

## 16. Third reading: the 2026-10-09 scope's mechanics (read)

The mechanics Dimitri's 2026-10-09 scope adds (shopkeeper VENDCINT, Fund Transfer limits, Fertilize, aura fruit,
Aging, Dazzling, Vampire Dentures), checked one by one in the Rogue Waves repository's `docs/reports/MECHANICS_CHECK.md`.
The engine facts it found that weren't in the kit, **read** unless marked; RogueLab rounds 4 and 5 (0.4.0-0.4.1,
2026-10-09) proved the ones marked so, and round 6 passed them all again:

- **The vessel's fall is client code.** `Entity_Vis_Hero.Spawn` calls `Landing()` unless the entity has the
  `Prespawn` tag: it sets the sprite shader's `_Offset` to (-10, 200, -10, 0), tweens it to 0 over 3 s and plays
  `Falling`, then `Land` (Entity_Vis_Hero.cs:33-82, 171-178). **Proven** that a non-vessel unit's sprite materials carry `_Offset` and
  follow the tween (Boutbro: 2 of 2); Boutbro has only `Idle` and `Move` animations, so a unit without `Falling` and
  `Land` just drops. Another unit falls the same way if the mod's client code
  copies it; listing `Entity_Vis_Hero` on a non-vessel also brings the player arrow and the "VESSEL DROWNED" alert
  (Entity_Vis_Hero.cs:84-120, 166-169). The game has no leave-upwards code.
- **`Spawn(..., Sameasparent: true)` overwrites the spawn's Owner, Team and Subowners with the parent's**
  (Helper_Entitytools.cs:77-93): spawn a neutral unit with `false`, a player's minion with the player's building or
  vessel as the parent. A shop every player can use: Owner -1, every `Blm_Server.Sessionids` key as a subowner
  (Merchut uses 0..9, Entity_Ai_Tower_Merchut.cs:59-61). **Proven** both ways: a parented minion took the local
  player as owner; a neutral unit kept owner -1 with the player as subowner.
- **Buttons show only on the selected entity** and only to its owner or a subowner on its team (Game_Ui.cs:298); the
  bar rebuilds when the selected entity's `Commandlist` changes (Game_Ui.cs:1334-1342, 1387), and `Createcommand` /
  `Removecommand` change it on the host at any time (Helper_Entitytools.cs:264-293, 409). A `Button`-style command's
  UI object is named its link plus its `Priority`; other styles use the link alone.
- **`Lasthit` is set only when the hit's data has no `Damage`** (Helper_Entitytools.cs:166-171): a vessel's or unit's
  own attack sets it (**proven**: a kill by `Interacthurt` with no data left the vessel as `Lasthit`), a hit with its
  own data (projectiles, Projectile_Master.cs:394, and any mod `Hurt` that passes
  `Damage`) doesn't. Count kills by place through `Ondespawn`, not by killer.
- **A heal's amount is its data's `Damage`** (times `Damagemulti`), not a `Heal` key: `Interactheal` with no
  `Damage` throws `KeyNotFoundException` in `Helper_Damagetools.Doheal` (**proven**, round 4).
- **`Damagedone`** on the attacker rises after each hit (Entity_Master.cs:208, Helper_Damagetools.cs:21-70): the only
  measure of damage actually dealt (lifesteal). **Proven** (round 5: 0 -> 4 on a hit of 4).
- **`Game_Mapvars.Wave` is 0 in the opening break** (**proven**) and rises as each Tide starts (Mode_Normal.cs:318, 371-376).
- **`Resourcegainmultiplier` scales Tide pay and drops, not the start aura**: `Mode_Normal` sets it to 1 only around
  the start aura (Mode_Normal.cs:359-362); Tide pay goes through `Give` (:66, Helper_Resources.cs:21).
- **Health** is the `Health` stat: `Value` now, `Value_Calc` the max; whether a max change keeps the fraction depends
  on the stat's type in data (`Variable` keeps it, `Fixed` refills, `Variabletrue` keeps the amount; Link_Stats.cs:46-103). **Proven** for Boutbro (`Variable`): a +100% max took
  5/10 to 10/20, and `Speed`/`Damage` modifiers added mid-match apply at once (round 5).
- **`Radio.Vfxdebuffcheck(name)` puts a stock debuff's look on any unit** (**proven**, round 5: 2 children under a
  foe's look), the route for showing modifiers as effects.
- **A `Button`-style command added mid-match shows on the selected unit's bar and goes when removed** (**proven**, round
  5, on the vessel and on a neutral unit for its subowner).
- **UniTask: read a task's `Status` once.** After `GetResult()`, reading `Status` throws "Token version is not matched,
  can not await twice" (round 5); keep the status in a local.
- **The game's own aura pickup** pays the owner of the first same-team `Mob` within 1 unit
  (Entity_Ai_Artifact_Checklist_Special_Pickup.cs:20-51): vessels and kit minions qualify.

## 17. The world map and the boat (2026-10-09, read)

Read for a generated sea chart (the Rogue Waves repository's `docs/mode/SEA_CHART.md`, which has the route and the probes):

- **A world is data plus a prefab.** `Mapworlddatabaseobject` (folder `"World"`): `Worldobj`, `Defaultlighting`,
  `Defaultnode`, `Display_Name`/`Desc`/`Sound`/`Soundtrack` (Mapworlddatabaseobject.cs). Its nodes are
  `Macro_Link_Add_Base` children: `Links`, `Unlockcondition`, `Cameraposition`, `Boat_Radius`, `Boat_Warp`,
  `Node_Hidden`, `Waypointspot`; kinds `Macro_Maplink_Add` (`Gameplan`, `Altgameplan`), `Macro_Nodelink_Add`,
  `Macro_Warplink_Add` (`Toworld`, `Landingspot`). Nodes are found by GameObject name with `GetComponentsInChildren`
  at use time, so nodes added in code count.
- **Routing is `Links` only.** The hovered node shows one "go to" arrow per unlocked link (Macro_Worldui.cs:428-470);
  the boat sails straight at the target's nearest `Dock` child, 2 to 10 units/s (Macro_Worldvisual.cs:71-77,
  1409-1760). The leader's code reads `Prevhovering.Cameraposition` with no null check (:1694): every node needs one.
- **Only the leader steers.** Clicks (Macro_Main.cs:599), `Selectmap_ServerRpc`, `Waypointmap`, `Pingnode` and
  `Selectworld` (Tds_Multiplayer_Remote.cs:648, 741, 809, 867) check `Currentleader`. The boat's pose reaches clients
  through `Moveboat_ServerRpc` (no ownership or leader check) into networked `Boatpos`/`Boatdir`/`Campos`/`Camdir`.
- **The world name is networked, the world is not.** Each machine resolves `Tds_Multiplayerinfo.World` through
  `Getitem("World", name)` and instantiates `Worldobj` itself. `Startup` waits for it with no timeout
  (Macro_Main.cs:404), and the host takes it from its save's `Player_Worldselect`, which the leader's map click
  rewrites (:633): a mod world in that key hangs the boat once the mod is gone. A warp (`Selectworld_ServerRpc`,
  Tds_Multiplayer_Remote.cs:827-885) sets `World`, `Selected` and `Worlded`; every machine's `Moveworld` swaps worlds
  with the title card, and `Macro_Worldvisual.Update` re-swaps any machine whose world differs from `Worlded`.
- **The only client-written networked value on the boat** is the player's own `Blm_Player.Displayname` (owner write,
  Blm_Player.cs:30; set from the Steam name at spawn, :98). `Changeartifact_ServerRpc` has no leader check
  (Tds_Multiplayer_Remote.cs:296) but doesn't say who sent it.

- **Building a world in code** (read while writing the mode part's chart, 2026-10-10; proven by RogueWavesMode PC round
  1, 34/0, Rogue Waves `docs/mode/SEA_CHART.md` §9): `Changeworld` instantiates `Worldobj` and names the copy after it, and
  `Macro_Worldvisual.Update` swaps any machine whose world's GameObject name differs from `Worlded`, so **the prefab's
  name must be the world's database name** or every machine swaps forever. `Selectworld` writes the landing node into
  `Selected`, and a non-leader with a non-empty `Selected` opens the Setup panel on that node (Macro_Main.cs, end of
  `Update`): land a party through the world's `Defaultnode` with `Selected` empty. `Showhover` walks the hovered node's
  `Links` with no null check: give every node a list. The node components have no `Awake`, and `Selectmap_ServerRpc`
  finds nodes with `FindObjectsByType` (active objects only), so a template kept under an inactive `DontDestroyOnLoad`
  holder is never picked by mistake. A client's `Startup` waits for the world item before the boat is `Ready`: a mod
  that registers worlds must not wait for `Macro_Main.Ready`. `Macro_Main.Blacklist` stops a node being picked but not
  hovered; the arrows only follow `Links`. A Gameplan the player hasn't played shows "UNCLASSIFIED" in the Setup panel
  unless `Prediscover` is set (Macro_Setupui.cs `Mapselected`).

## 18. Fourth reading: Cardinal patterns, messages, the final map, chart decorations, unlocks (2026-10-09, read)

Read for "what else can the run use" (Dimitri, 2026-10-09). Map objects, nodes, grid and wallet went to `16`; ideas
built on both are in the Rogue Waves repository's `docs/mode/MECHANIC_IDEAS.md`. All **read**, nothing tried in game.

### 18.1 How the stock Cardinals work, and patterns to copy

- **The HUD counter** is written by `Helper_Entitytools.Editartifact(name, value)` into `Artifacttrack[index]`
  (Helper_Entitytools.cs:450-459). Almost every Cardinal passes `Time.fixedTime` ("last fired", which flashes the
  icon); Coin passes the Tide number (Entity_Ai_Artifact_Coin.cs:63).
- **Checklist is not a quest system:** every killed `Enemy` drops a `Special_Aurapickup` that pays the first vessel
  or unit to touch it 0.5 of each aura within 8 s (Entity_Ai_Artifact_Checklist.cs:9-27; `_Special_Pickup`.cs:13-63).
- **Fie's Rest Stop is not an item system** (`docs/EXPANSION_SCOPE.md` row 7): it throws timed buffs, a modifier with a
  duration plus a debuff look (`Addmod(.., duration)`, `Vfxdebuffcheck`) (Entity_Ai_Tower_Fieshop.cs:76-160). A
  consumable a mod sells is the same two calls.

| Pattern | Stock Cardinals that show it (read) |
| --- | --- |
| Upgrade every unit that spawns, filtered by tag, name or parent (+ a sweep at `Starting` for those already there) | Mobapply, Voiceapply (units whose parent is a vessel), Paint, Greatspear (`Enemy_Spear` only), Chimera and Seeds (every `Resource` node) (`Game_Entity.Onspawn`) |
| React to a kill or a death | Checklist (drop a pickup), Gavel (a friendly unit's death fires a bolt, stronger by its level), Titan (bursts on its own death), Pig Named Bank (`Artifact_Moneybag`) on `Reason == "Sell"` (`Game_Entity.Ondespawn`) |
| Count down Tides, then fire and remove itself | Rusty Pipe ("Age" counter on the unit) |
| Grow every Tide | Caffeine, Opal Core, Coin, Pig Named Bank (each building's sell value) (`Wave.OnValueChanged`) |
| Bend the Tide clock | Vintage (+`Time_Base` to each break), Same Day Shipping (shorter breaks, pays aura), Mooncore (skips Tides 3, 7, 14, 21, 28: `Entry` +2, `Wave` +1, "THE MOON ALIGNS WITH YOUR CHOICE"; hooks `Entry`), Loop (first hit on the core clears the field and replays the Tide, once) |
| A free build that comes back | Onetimebuild (button removed while its unit lives, back when it dies); Build (a permanent fifth build slot, range 1000, sells for 0) |
| One special unit at a time | Paper (the first Towerfighter built becomes a buffed paper one), Computer (a static lock) |
| Delayed delivery | Heroapply_Delay (upgrade name → seconds), Paint (charm after a timer), Snare, Greatspear's look |
| A trade-off | Glass Core (a sub-core that takes no damage; when it breaks, everyone loses `Aura_Base` of their wallet), Wavebreaker (foes in range get a health multiplier), Computer (enemies near a core slow time and stun), Bottom Rock (`16` §5) |
| Swap a unit type | Textbook (every Dagger becomes a training dagger) |
| Change a unit's team | Blue Paint's charm: team set to the core's, `Enemy` tag removed (Entity_Ai_Effect_Artifact_Bluepaintcharm.cs:19-35) |

Bugs not to copy: Mobapply, Voiceapply and Pig Named Bank compare a team with itself, so their team filter never
filters (Entity_Ai_Artifact_Mobapply.cs:14); Paper's tower and Rusty Pipe's buff remove the same modifier four times
(Entity_Ai_Effect_Artifact_Paper_Tower.cs:34-42); most never unsubscribe from `Onspawn`/`Ondespawn` (Gavel does it
right, Entity_Ai_Artifact_Gavel.cs:71-76), harmless only because the map ends.

### 18.2 Messages on every client

- **Warning popup:** any entity's `Radio.Uiwarning_ClientRpc(text, icon, seconds)` shows an icon, the upper-cased
  text and the "Warning" sound on every machine (Entity_Master_Radio.cs:864-907; Game_Ui.cs:1582-1617). The stock map
  events use it ("THE CLOCK QUICKENS", "THE LAND SHIFTS"). The icon is a name from the game's icon table (add one the
  way Argo does, `16` §5).
- **Banner and note:** `Game_Mapvars.Instance.Display` (networked: `Display_Wave`, `Display_Note`, `Display_State`,
  `Display_Endgame`, Game_Mapvars.cs:11-19, 70). A new non-empty `Display_Note` slides in under the banner for 5 s
  when it differs from the last note shown (Game_Ui.cs:1541-1555), and `Mode_Normal` rewrites the whole struct from
  its own copy (`20` §2); `Mode_Normal` writes the Tide entry's `Note` there. Writing "TIDE CLEARED" / "APPROACHES" /
  "APPROACHING" also muffles the music (Clientmode_Normal.cs:84-95).
- **Music stages** advance at the mode's `Musicwave1..4` params (Clientmode_Normal.cs:22-83): a mod's `Modeobject`
  sets where the music builds. A stage plays only at exactly its Tide with the one before reached, so a skipped Tide or
  a rejoin freezes the music (`21` §1.2). **Tips** show only on the end screen, from the `Earlygame_Funfacts` list
  (Clientmode_Normal.cs:106-131).
- **Tutorial prompts** (`Tds_Tutorialmenu.Instance.Descprompt`, `Highlightprompt`, `Imageprompt`,
  Tds_Tutorialmenu.cs:67-175) are client-local; the tutorial menu exists in ordinary matches too (the rotate and
  Cardinal hints use it, `20` §2). Untested from a mod.
- **`Radio.Dataadd_ClientRpc(folder, name, value)`** writes into every client's own save
  (Entity_Master_Radio.cs:815-826): the host can mark a campaign milestone or an unlock in each player's save.

### 18.3 The final map has no boss; every Basin has a last stand

`Mode_Final` shows "SURVIVE", counts down 6:00, plays only `Wave[0]`'s content once, pays `Resourcesinit` every real
second (so fast-forward lowers income per game second, Mode_Final.cs:290-296) and wins when the timer ends **or a core
dies** (Mode_Final.cs:230-308), then goes to `Credits` after 15 s. `Clientmode_Final` swaps the music for a waterfall
loop (15 s delayed fade-in) and fades it out at `Gamecomplete` (Clientmode_Final.cs:17-31); the final Gameplan's
`Soundtrack_Cyan` loads but never plays. In every mode, not only the final map, each Basin (`Main_Core`) and sub-core
survives its **first lethal hit of the match at 1 HP** with the `World_Core_Oneshot` effect (`Finalhit`,
Entity_Ai_Main_Core.cs:36-45; Entity_Ai_Main_Subcore.cs:18-27), so a map is lost on the second killing blow. There is no
boss being anywhere: a Breaker map's boss would be the pack's own enemy, spawned through an enemy core's `Spawn` command
(`13` §3), with the map ended by the host on its `Despawn` (the win path, §4).

### 18.4 Chart decorations, unlocks, Discoveries, the journal

- **Node addons are hover effects.** When the leader hovers a node, every client calls `Show` on each active
  `Macro_Addon_Base` whose GameObject name equals the node's (Macro_Main.cs:609-616; Tds_Multiplayer_Radio.cs:138-160),
  and `Hide` on the next hover. `Macro_Addon_Nodeshowbillboardlink` needs only a sprite: a billboard image above the
  node for `Expiry` s (Nodeshowbillboardlink.cs:27-41): a sea-chart node's modifier or reward icon on hover.
  `Nodeshowblipprogress` (pips from an `Unlock_Tracker_Additive`) needs an unlock entry; `Nodeshowcine` needs a
  `PlayableDirector`; `Nodeshowdebuff` plays a debuff look on the boat when it sails to listed nodes. A permanent
  "cleared" mark is a child object of the mod's own (§17's nodes are built in code anyway).
- **Unlocks** are `Unlockentryobject`s in `Items["Unlock"]` (`Dataadd`, `Livetracker` list), checked by
  `Tds_Data.Checkunlocks`, which writes `Unlock/<key>` and `Unlock_Notif/<key>` (Tds_Data.cs:26-61). Trackers:
  `Additive` (a `<key>_Counter` toward `Unlockam`), `Conditions` (compare save keys), `Matchreader` (the last match's
  JSON). The boat's unlock screen shows an artifact panel for `Unlock_Artifact_<name>` and a vessel panel for
  `Unlock_Hero_<name>` (Macro_Unlock.cs:641-672, 795-826). The match is a substring, and **any other key is marked
  seen with no announcement**, so a Depth unlock through the stock system is silent: announce it with a Milestone or
  the pack's own message (decompile 2026-10-14, `23` §3.2).
- **Discoveries** (`Macro_Discoveriesui.Items`, a list of `Discoverycont`: Map or Milestone, name, text, sprite,
  condition) light the boat's "New" badge when more are unlocked than the saved count (Macro_Discoveriesui.cs:25-157;
  Macro_Extraui.cs:85-105): a mod can append Milestones ("Campaign cleared at Depth 1").
- **Journal:** the only live item is `TextTotalComplete` (unlocked vs total linked pages,
  Logitems.TextTotalComplete.cs:41-104); `TextMapStats` is the map's static text. Pages lock on `Unlock/` keys
  (Logobject.cs:77-88).

### 18.5 Dev tools and presence

- `Game_Admin` (public: `Setartifact`, `Getmoney`, `Setlighting`, `Spawnenemy`, `Spawnhero`, `Lose`, `Settimescale`;
  Game_Admin.cs:39-131) is reached in release only through the Sandbox Cardinal's buttons, which null-check it
  (Entity_Ai_Tower_Sandbox.cs:107-150): useful in self-tests, not a console.
- **Discord presence can't be set by a mod:** the game sets one random "Details" line at start and keeps the client
  private (Bla_Discordpresence.cs:27, 47-71); reaching it needs reflection, which mod code can't use (§10).
