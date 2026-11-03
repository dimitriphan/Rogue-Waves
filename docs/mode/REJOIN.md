# Rejoining a running map

Dimitri's ask (2026-10-11): a player who drops from a map because of connection trouble has no way back in; make a way
to rejoin mid-game without interrupting the others.

**Answer: a true mid-map rejoin works in this engine.** The game already keeps a dropped player's things and hands them
back on return; the one hard block is that it shuts the lobby for the whole map. `RogueWavesMode` 0.2.0
(`TDS_Mod/ModSource/RogueWavesMode/Code/09_Rejoin.cs`) opens it while a seat is empty and catches the rejoiner up.
**State:** 0.2.1 passed the solo PC round 6, 47 of 47 (2026-10-11; round 5's one failure is in §6). The drop and
rejoin themselves need a friend online (§6, O1-O8). 0.3.0 (2026-10-15, compiled, not run) adds what the decompile
showed was missing: a friend who restarts the game keeps their `Sessionid` (`RWIdentity`, O3b), and one player who
can't load a map no longer sends everyone to the main menu (`RWLoad`, O3c); both in `Code/10_Online.cs`. 0.1.0-dev
(2026-11-03, compiled, not run; the part now carries the pack version): a turned-away player stays turned away on a
second try (O5), and the "Lost the map" line names the host of the map just lost, not the first host of the launch.

## 1. What the game does today

Read from the decompiled game (kit `docs/13` §11; file names are its types, lines as decompiled).

| Fact | Where |
| --- | --- |
| A player's stable key is the `Sessionid`: the host maps each player's saved GUID to a number and gives the same number back on a rejoin | `Blm_Player.cs` Start, `Blm_Multiplayer.Getsessionid` |
| A vessel, its towers and buildings are owned by that `Sessionid` (`Entitydata.Owner`), and the core respawns a dead vessel with the same owner, also while its player is away | `Entity_Ai_Main_Core.cs:97`, `:126` |
| A drop despawns nothing. Teammates become sub-owners of the dropped player's things, and a rejoin with the same `Sessionid` takes that away again: the game was built for reconnects | `Game_Players.cs:24` `Playerconnected`, `:53` `Playerdisconnected` |
| A client's controls, build menu and HUD find their things by the local `Sessionid` | `Game_Ui.cs:298`, `Game_Selection.cs:479`, `Entity_Command_Build.cs:134` |
| **The block:** the lobby is shut when the map loads and again when its loading screen ends (`Changelobbystatus_ServerOnly("Closed")`: Steam lobby not joinable, `Server_Serveropen` false), and the connection check then refuses everyone ("Server is closed") | `Tds_Multiplayer_Remote.cs:593`, `Game_Load.cs:261`, `Blm_Multiplayer_Remote.cs:278-297`, `Blm_Multiplayer.cs:64-77` |
| A late joiner gets the running scene and every networked thing from Netcode. The map's own client start runs by itself (`Game_Client_Init.Initalize`: every vessel's, Cardinal's, the map's, mode's and music's groups, then the local `Loaded`, then the mode's client script); each thing's client side waits for that `Loaded`; the HUD reads the current wallet and Tide | `Game_Client_Init.cs:39-177`, `Entity_Master_Client.cs:138`, `Game_Ui.cs:1745-1800` |
| What a late joiner misses: the camera fly-in, HUD start, game-speed hook and the mods' `Gameclient` prefabs all live in `Game_Client_Init.Startvisuals`, which only the loading scene (`Game_Load`) calls, and a late joiner never loads that scene | `Game_Load.cs:245`, `:293` |
| A rejoin's `Tds_Clientinfo` is new and empty: no vessel name, `Activeplayer` 0 (shared pay and the end screen skip them), no aura, new stats | `Tds_Clientinfo.cs`, `Helper_Resources.Giveresource`, `Game_Endscreen.cs:92` |
| Nothing on the host waits for clients once the map runs (only `Game_Init.Loadgame` waits, before the first Tide), so a joining client holds nobody up | `Game_Init.cs:123` |
| The joining side accepts a Steam join request only where `Client_Canjoin` and the joinable scenes allow (the main menu is where invites are accepted) | `Blm_Multiplayer.cs:153` |

## 2. How the rejoin works

**The host** (every map, every mode, while the pack is on):

- When the map starts it keeps a **seat** for every player in it: vessel name, team, aura, end-screen stats, name,
  copied four times a second.
- When a seat empties it says in chat: "*Name* lost the connection. Their vessel and towers wait for them: in Steam,
  right-click *Host* and pick Join Game." and **opens the lobby** (the game's own switch). The game's loading screen
  can shut it again; the host checks every frame and opens it again (the loading screen ends about 10 s after the fly-in).
- When the player comes back with the same `Sessionid`, it gives the seat back (vessel name, team, `Activeplayer` 1,
  stats, aura) and says "*Name* is back." The game itself hands their things back (`Playerconnected`).
- Once every seat is filled, or the map ends, it **shuts the lobby** again. Someone who wasn't in the map is turned
  away with a chat line ("joins on the boat").

**The rejoiner:** knows it joined late (the map was already loaded when its copy of the map arrived). Once the game's
own client start is done it runs the camera, HUD and game speed start (`Startvisuals`, the same fly-in the others saw
at the start, on their screen only), loads the vessels' groups (its own vessel's name only arrives with the seat), and loads
the map's sea weather objects, which the others loaded on the boat (the weather comes from the chart's name and the
Gameplan, which every machine has).

**The dropped player:** back on the main menu, the game's error box says "Lost the map. If *Host* is still playing:
Steam friends, right-click *Host*, Join Game."

### What comes back, what doesn't

| Comes back | Doesn't |
| --- | --- |
| The vessel, never despawned, with its upgrades and where it stood (respawned meanwhile if it died) | Pay from the Tides played while away: Tide pay goes to the players present, kill drops are split among them |
| Every tower and building, with control | Aura earned in the last quarter second before the drop |
| The aura held at the drop, team, vessel name, end-screen stats | The music's stage: the rejoiner hears the intro music until the next stage change (`Clientmode_Normal` counts stages from changes) |

### What stays untouched for the others

No pause, no scene change, no prompt. They see two chat lines. The lobby is open only while a seat is empty. One
cosmetic change the base game already makes on any drop: player colours follow join order
(`Tds_Clientinfo.Changecolors`), so a returning player takes the last colour.

## 3. Defaults picked (Dimitri can change any)

- **Every map, any mode**, not only Rogue Waves maps: a drop is the same everywhere.
- **Only players who were in the map** come back in; anyone else is turned away and joins on the boat. Letting a new
  friend in mid-map would need a vessel spawned for them, which the game only does at map start.
- **The way back is Steam's own Join Game** on the host. A mod can't call Steam (Steamworks is not in the mod
  compiler's reference list), and the game has no invite button in a map.
- **No time limit:** a seat waits until the map ends.
- The rejoiner sees the map's fly-in again; it uses the game's own camera start.

## 4. Limits and risks

- A player who drops **during the loading screen**, before the map starts, has no seat (the game never spawned their
  vessel) and is turned away; they rejoin on the boat. So is one the host sends back for not loading (O3c).
- A restart keeps the seat only with the same save slot on the same computer (the kept id lives in that slot and is
  tied to the PC), and only once the host has noticed the old connection is gone: rejoining while the host still holds
  the old player object would give two objects one `Sessionid`. A restart takes longer than Steam's disconnect, so
  this is not expected (O3b).
- The Steam side (Join Game shows on a host whose lobby was reopened mid-map) is unverified until an online evening.
- The rejoiner's own vessel: its group is loaded after the seat comes back, and the game gives a thing's client side
  10 s after the map loads to find its group (`Bl_Address.Waitforload`). Likely enough; checked online (O3).
- Sea weather objects on the rejoiner: same 10 s window (O7).
- Netcode sends the whole running map to the rejoiner in one go from the host; whether the others notice a hitch is O6.
- Pieces built later that keep per-player state (Pure Aura, Directions, Cardinals, votes) must add it to the seat
  (`RWRejoin.Seat`, `Copy`, `Restore`) and count a vote's voters from the seats present.

## 5. Where the code is

`TDS_Mod/ModSource/RogueWavesMode/Code/09_Rejoin.cs` and `10_Online.cs`, driven every frame from
`00_RogueWavesModeMod.cs`; self-test probes R1-R6, R2b, I1 and L1 in `99_SelfTest.cs`. Engine facts in kit `docs/15`
§14.5 and `docs/19` §3, §5.

## 6. Tests

**Solo PC round** (`RogueWavesMode_round6`, the round-4 run plus these, in the first map once its loading screen has
ended): R1 the host keeps a seat for each player; R2 a pretend empty seat opens the lobby, and filling it shuts it; R2b
the lobby opens again on the next frame after the game shuts it (as its loading screen does); R3 the drop is said in
chat; R4 a seat given back restores aura, vessel name and `Activeplayer`; R5 a rejoiner finds the map's weather from
the synced names; R6 the lobby is shut once the map ends.

Round 5 (0.2.0, 2026-10-11): 45 pass, 1 fail. R2 failed because the probe ran inside the first map's loading screen,
whose end (`Game_Load`, "Loading 90%", about 10 s after the fly-in) shut the lobby less than a second after the mod opened
it, and 0.2.0 only looked again a second later. 0.2.1 looks every frame and the probe waits for the loading screen.
Round 6 (0.2.1, 2026-10-11): 47 pass, 0 fail.

**Online, with a friend** (joins the online evening, `docs/RESEARCH_GAPS.md` §E):

| # | Do | Expect |
| --- | --- | --- |
| O1 | The friend pulls their network cable (or turns Wi-Fi off) mid-Tide, **game left open** | The host's chat says they lost the connection; the Tide goes on, nothing pauses |
| O2 | The friend reconnects and opens the Steam friends list | Join Game on the host; their main menu showed the "Lost the map" line |
| O3 | Join Game | The running map loads, the fly-in plays, then their HUD; their vessel where it stood, their towers sellable and upgradable, their aura as before |
| O3b | Repeat O1-O3, but the friend ends the game in Task Manager and starts it again (same save slot) | As O3. Since 0.3.0 the friend's pack puts the id from their last launch back (kit `docs/19` §5, `RWIdentity`), so the host gives them the same `Sessionid`; their log says "identity: this player's id from the last launch is back". With 0.2.1 they were turned away like a stranger (O5) |
| O3c | A friend with the pack turned off joins on the boat, and the host starts a chart map (the friend can't load it) | Since 0.3.0: 75 s into the loading screen the host sends that friend back with a chat line and the others start the map (`RWLoad`); before, everyone went to the main menu at 120 s |
| O4 | Play a Tide | Tide pay and kill drops reach them again |
| O5 | A third friend not in the map tries Join Game, then **tries again** | Turned away with a chat line both times; back on their main menu. Before 0.1.0-dev (2026-11-03) the second try got in, seatless, while a seat was empty (`QUALITY_PASSES_2026-10-15.md` B4-14) |
| O6 | Watch the others' screens while the friend joins | No stutter worth noting (note any, and the host's log time of the join) |
| O7 | Repeat O1-O3 on a Rising Water and a Nesting Ground map | Flood, flooded tiles and nest look right on the rejoiner's screen |
| O8 | Finish the map | The end screen lists the rejoiner with their stats; the party sails on together; the lobby is open on the boat |
