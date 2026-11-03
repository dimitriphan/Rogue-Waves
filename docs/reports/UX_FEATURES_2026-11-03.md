# UX features that fit the game: a feasibility study (2026-11-03)

> **Since (2026-11-03 06:21Z):** Dimitri picked option 1 of §8, the five recommended features. They are on the
> Phase 5 list in `docs/PLAN.md` Part C; the other rows stay as reference.

Dimitri asked (2026-11-03): "Look into additional features that would fit into the game while enhancing user
experience, such as buttons, transition screens or multi-player lobbies. These are just examples, but see if these
options are feasible as well." This page answers with the standard of `SCOPE_FIT_PLAN_2026-10-28.md`: every feature
rides on a stock screen, menu item, text slot or flow the game already has, and none invents a new UI system. **Nothing
here changes the scope or the plan**; §8 lists what Dimitri can pick.

**How it was read.** Kit chapters `20` (menus, dialogs, HUD), `19` (networking), `15` §3, §14.5-14.8, §17-18, `21`,
`23`, the engine study, `docs/mode/MAIN_MENU_START.md`, `REJOIN.md`, `SEA_CHART.md` and the mode part's code. The
engine study only skimmed the client UI classes (its §15), so this thread decompiled the game's `Assembly-CSharp.dll`
again with the kit's recipe (`docs/13` §11, 703 types; kept in the thread's scratchpad, never committed) and read the
screens a player passes through: `Game_Load` (loading screen), `Game_Client_Init.Startvisuals` (map intro card),
`Game_Endscreen`, `Clientmode_Normal` (its end), `Tds_Pausemenu`, `Macro_Worldui.Changeworld` (the boat's world card),
`Mainmenu_Main`, `Bla_Ui_Mods`, `Blm_Ui_Chat`, `Tds_Clientinfo`, `Game_Client_Players`, `Macro_Extraui`,
`Tds_Multiplayer_Remote` (return and exit), `Blm_Multiplayer(_Remote)` (kick, errors), `Entity_Master_Remote`,
`Credits_Main`. Line numbers are from this decompile.

**Tags.** **built** = in the pack; **proven** = seen in game by a mod (round cited); **read** = from the code only;
**new** = not in the kit before this page. **Cost:** XS = a few lines on code that exists; S = a small module (under
about 200 lines); M = a module with its own UI or sync; L = several modules plus art. **Risk:** what can go wrong in
game. **PC:** *probe* = one RogueLab-style solo check (§7); *eye* = Dimitri looks; *online* = needs a friend
(the planned online evening, kit `19` §8). Kit paths: `docs/NN` = `TDS_Vessel_Creator/docs/`.

## 1. Answer

**All three examples are feasible, in the game's own shapes, and none needs a new mechanic.**

- **Buttons: yes.** A mod clones a stock `Blu3_Button` beside the one it copies and gives it its own click; the
  main-menu Rogue Waves button proved the pattern (RogueLab round 6, M1-M5). The same works on the boat's bottom row,
  the pause menu and the end screen, and each new button opens the stock choice dialog for its contents. What a mod
  **cannot** do is add a menu of its own that the game's menu system reaches (kit `20` §1): a new button opens the
  choice dialog, a warning popup or a line of text, never a new screen.
- **Transition screens: yes, as text on the game's own five.** The game already passes through a loading screen, a map
  intro card, the boat's world card, the end screen and Tide banners, and each has a text slot a mod can write (§4).
  The best value is one **campaign card**: the same short line ("Map 4 of 10 · Rising Water · 2 retries left") on all
  of them, so a player always knows where the run stands. A new full-screen interlude is possible but not worth it
  (§4, T8).
- **Multiplayer lobbies: the boat already is the lobby**, and it stays that way (decided 2026-10-09,
  `docs/mode/MAIN_MENU_START.md` §1, §4). What fits on top: a warning when a friend has no mods at all (a networked flag
  the game already syncs, **new**, §5 L1), a pack-version check once the map loads, a host kick button, a clear way back
  in after a drop, and a chat fallback for votes. A lobby browser, passwords, more than 4 players and host migration
  are out of reach (§5).

Of the 42 rows below, **33 are feasible features**, 2 are feasible but not recommended (a lobby screen of its own, a
full-screen interlude), 5 are not possible, and 2 are already the game's (pings and photo mode; the build bar's edge,
which is stock layout). The five
worth adding first (§6) are the campaign card, the retry warnings, the friend-has-no-mods warning with the rejoin
instructions, weather icons on the chart, and the pack's tips in the game's tip list. All five are S or XS and ride on
text slots and flows that already show on every machine.

## 2. Rules every feature here obeys

From the kit, each one already met in game or read in the code:

1. **No new menu.** A `Blu2_Menu_Handler` made by a mod never joins `Bl_Menus.Menus`, so `Changemenu` can't open it
   (kit `20` §1). New UI is a clone inside a stock menu, the choice dialog, or an own Canvas (§4 there).
2. **No new network code.** No RPC, no `NetworkVariable` (kit `19` §1). Whatever clients must see rides on what already
   syncs: commands on entities (61-byte `Goalstring`), `Game_Mapvars.Display`, warning popups, chat lines, stats,
   the boat's world name.
3. **UI is per scene and per machine.** Menus are rebuilt in each scene; mod UI is rebuilt each time (kit `20` §7.9),
   and every machine runs it from synced data.
4. **Gamepad focus belongs to the active menu** (kit `20` §1.3): a mod panel outside a menu loses pad focus every
   frame, so anything a pad player must click goes through the choice dialog.
5. **The choice dialog has no timeout, no cancel and no disabled card** (kit `20` §2): a timed vote closes it itself.
6. **Base-vessel game sprites stay private** and the pack's own art is temporary until Dimitri's (`docs/TEMP_ART.md`);
   nothing here needs new art except icons that already exist as temporary files.

## 3. Buttons

| # | Feature | Hooks into (stock) | Engine | Cost | Risk | PC |
| --- | --- | --- | --- | --- | --- | --- |
| B1 | **Main-menu Rogue Waves button** with New / Continue / Back | clone of `Main/Play`, `Runprompt` on the main menu (`docs/mode/MAIN_MENU_START.md` §2) | proven (RogueLab M1-M5); not built (Phase 5 piece 2) | S | low | eye (look beside Play) |
| B1a | Its hover tip | `Main/Items["Buttontip"]`, which each stock button rewrites on hover ("Embark" for Play, Mainmenu_Main.cs:98) | read | XS | low | eye |
| B1b | **Continue shows the campaign** before you commit: the card's hover text says "Map 4 of 10, 2 retries left, crew: Yossele, Spool" | the choice card's `Display_Desc` (hover box) from the host's save | read | XS | low | probe |
| B1c | **Abandon campaign** as a fourth card (with a second "Are you sure?" prompt) | `Runprompt` twice; `RWCampaign.Stop` exists | read | XS | low (two prompts in a row: open the second only after the first closes, kit `20` §2) | probe |
| B2 | **Boat "Campaign" button**: a status prompt (map, stretch, retries, each player's vessel) and, for the host, Abandon | a clone of one of the boat's bottom buttons (`World/Items["Equip"]`, `["Discover"]`, `["Leaderboards"]`, `["Warp"]`, Macro_Extraui.cs) opening `Runprompt` | read | S | medium: the row's layout and the pad's start button are data, unseen | probe, eye |
| B3 | **Pause menu "Campaign" button** with the same status | a clone of `Pause/Items["Journal"]`; the title line changes on hover through `Items["Title"]` (Tds_Pausemenu.cs:197-225) | read | S | medium: the pause menu toggles its items per scene each time it opens (:146-170), so the clone must be shown again after that | probe |
| B4 | **Exit warning that names the cost**: the host's "EXIT MAP / Return to the world?" becomes "Leaving now loses this map: 1 retry left" before Tide 15, and "The map is won: leave now?" after | the prompt is built inside the private `Exitgamehost` (Tds_Pausemenu.cs:23-47); the mod rewrites the open dialog's subtitle when `Confirmation` opens with title "EXIT MAP", and the `Exit` item's hover title | read | S | medium: a text swap on a dialog the game owns, timed by watching the menu | probe |
| B5 | **Leave vote between Tides after Tide 15** (scope `04` [Tides and Leaving]) | a `Button` command "LEAVE" on each vessel, drawn only for its owner (kit `20` §3), or the choice dialog (recipe kit `20` §5) | proven pieces (commands V2, dialog A6); not built (Phase 5) | M | low | probe, online |
| B6 | **Pure Aura / run line on the HUD** (scope `06` [Pure Aura] "Shown as") | a counter stat on the vessel (proven for Mummer's Thread) or a `None` command read into a HUD label (kit `20` §5 "Run line") | proven pieces; not built (Phase 5) | S-M | low | probe |
| B7 | **Version tag on the main menu**: "1.x.x · Rogue Waves 0.1.0" | `Main/Items["Updatetext"]`, set to `Application.version` (Mainmenu_Main.cs:412) | read | XS | low | eye |
| B8 | **The pack's entry in the Mods menu**: description, author, version and a picture | `Bla_Ui_Mods` shows the `.info`'s name, author, description, version and a `modImage` (Bla_Ui_Mods.cs:27-70) | read (where ModTool loads `modImage` from is in `ModTool.dll`, not read here) | XS | low | eye |
| B9 | **Hide the score on the pack's end screens** (scope `04` [Campaign] "Score: does not count") | `Win`/`Lose` `Items["Recordscore"]` (Game_Endscreen.cs:446-456) set inactive on `RW_` plans | read | XS | low | probe |
| B10 | **Host kick button** on the boat (a friend who is idle, or without the pack) | `Blm_Multiplayer.Instance.Kickclient(clientId)` is public and host-only (Blm_Multiplayer.cs:445-455); the game's own `Kickplayer_ServerRpc` has no caller (Blm_Multiplayer_Remote.cs:76-95, kit `19` §1) | read | S | low; a kicked friend can join again (no ban list) | probe (solo: the call itself), online |
| B11 | A pack settings page (toggles such as "show the run line") | none: a mod can't add a menu, and the settings page saves its own known keys | not possible as a page; a card in B2/B3 can hold a toggle | - | - | - |

## 4. Transition screens

The game's own transitions, in the order a campaign meets them, and what each can say.

| # | Moment | What it shows today | Text slot a mod can write | Engine | Cost | Risk | PC |
| --- | --- | --- | --- | --- | --- | --- | --- |
| T1 | **Loading screen** (scene `Gameloading`) | each player's vessel falling (`Loadingsprite`), a bar with the map's name and "<map text> - Entering" (Game_Load.cs:108, 171-172) | `Game_Load.Ui/Bar/Desc` (plain `.text`); the pack's `Loading` keyword prefab starts on every machine as the screen opens (Game_Load.cs:75) and writes it once the bar appears, about 3 s in | read (**new**: the bar's text and its timing) | S | low | probe, eye |
| T2 | **Map intro card** (menu `Cine`) | the mode's name, then the map's name and text (Game_Client_Init.cs:294, 317) | `Cine/Items["Maptitlealt"]` and its `Desc`; the `Gameclient` keyword prefab starts in the same call (:257), so it rewrites the line a frame later | read (**new**) | XS | low (keep the card's length: its layer swallowed the play test's first clicks, `PLAYTEST_2026-10-26.md` P1) | probe, eye |
| T3 | **Boat world card** (menu `Betweenworld`, then the world title) | the world's name, text and sound (Macro_Worldui.cs:610-647) | the chart world's `Display_Name`/`Display_Desc`, already "Sea Chart" / "Map n of 10" (`04_World.cs`) | **built** (the line); the warp is proven (mode PC rounds 1-4), the card's look not yet seen | XS to extend | low | eye |
| T4 | **End screen** (`Win` / `Lose`) | title = `Display_Endgame` ("BASIN REMAINS" / "BASIN DROWNED", Mode_Normal.cs:305, 321), the mode's name, map, time, Tide, score, medals, a tip, each player's stats, the return button with its 30 s timer (Game_Endscreen.cs:431-456, 541-566) | **`Game_Mapvars.Display.Display_Endgame` is synced**, so the host writes "MAP 4 OF 10 WON" / "MAP LOST · 2 RETRIES LEFT" / "CAMPAIGN WON" and every machine shows it | read (**new**: the title is the one synced free text at map end) | S | medium: the host must write it after `Mode_Normal`'s own write and before the screen reads it (when `Gamedatajson` arrives, :462-470) | probe |
| T5 | **Tide banner and popups** | Tide number, a 5 s note, warning popups (kit `20` §2) | `Uiwarning_ClientRpc` (already used for the weather, `07_Weather.cs`); `Display_Note` is overwritten by the mode | built (weather); read | XS each | low; keep the stock banner words, the music listens to them (kit `21` §1.2) | eye |
| T6 | **Tips**: the pack's own lines ("Only the host leaving a map spends a retry") | the end screen's `Tip` line from the database list `Other/Earlygame_Funfacts` (Clientmode_Normal.cs:106-131), the pause menu's blurb from its own fact list (Tds_Pausemenu.cs:255-263) and the main menu's Close Game dialog (Mainmenu_Main.cs:203-211) | add entries to `Factobject.Data` and `Datas` on every machine at boot; each entry's `Unlockconditions` gates it on a save key, so pack tips can show only to players who have played a campaign | read (**new**: tips show in three places, not only the end screen as kit `15` §18.2 and `docs/EXPANSION_SCOPE.md` row 13 say) | S | low; the stock pick `list[minute % (count - 1)]` never shows the last tip and throws with one, which more entries only make rarer | probe |
| T7 | **Campaign won**: a closing moment | `Mode_Final` sends everyone to the `Credits` scene after its win (Mode_Final.cs:233-237), and `Credits` returns a party that came from a map to the boat with the lobby open (Credits_Main.cs:83-91) | after map 10's win the host calls the same `Changescene_ServerRpc("Credits")` | read | XS | medium: it rolls the base game's credits, a taste call | eye |
| T8 | A full-screen campaign interlude of its own (a painted card between maps) | none | an own Canvas over the boat (kit `20` §4) | read | M-L, with art | medium: it hides the boat, which already is the between-maps screen with the chart; pad focus (§2 rule 4) | not recommended |

**The campaign card (T1-T4 together).** The same facts on every screen: map number, stretch, the map's weather and
retries left. Clients already know the first three without any network: every machine builds the chart from its world
name (`RW_Chart_<seed>_<route>`, `04_World.cs`), which holds the seed and the route. **Retries live only on the host.**
They reach clients on the end screen (T4, synced) and in chat today; for the loading screen and the intro card a carrier
is needed: the cheapest is a suffix on the world name (a lost map would then rebuild the chart once and play the world
card, which is also the right moment to say "2 retries left"), or the host's `RW_Run` command on each vessel once the
map has started (kit `20` §5). That choice is the build's, not a blocker.

## 5. The lobby: host, friends, rejoin, voting

What exists (kit `19` §1, `15` §14.8): the boat starts the host and a public Steam lobby of 4; friends join only
through Steam (invite or Join Game); the leader is always the host; approval checks only "full" and "open"; the boat
has the player list with vessels and ready marks, the Invite button and the ready-up; the end screen has the return
vote. The pack adds the main-menu start (B1), rejoin after a drop or a restart (`docs/mode/REJOIN.md`, built) and
sending back a friend who can't load (`RWLoad`, built).

| # | Feature | Hooks into (stock) | Engine | Cost | Risk | PC |
| --- | --- | --- | --- | --- | --- | --- |
| L1 | **"<name> has no mods on" warning on the boat**, before anyone readies | **`Tds_Clientinfo.Mods`** is a networked bool each player writes from `Bla_Mods.Instance.Modactive` at every scene change (Tds_Clientinfo.cs:23, 57-63); the host reads it and shows a warning popup and a chat line | read (**new**) | S | low; it can't tell *which* mods, only none at all (the friend who would hang at loading, `MAIN_MENU_START.md` §3) | probe (own flag), online |
| L2 | **Pack-version check** once the map loads (scope `01` [Online] "same pack version") | each client's mod sends its version as the `Json` of a controllable `None` command on its own vessel (`Command_ServerRpc(Link, Json)` checks only ownership, Entity_Master_Remote.cs:43-70); the host compares and warns in chat | read (commands proven) | S | low; it comes after the load, since nothing a client writes on the boat can carry a version (the only client-written values there are the Steam name and L1's flag) | probe, online |
| L3 | **Crew reminder on Continue**: "Waiting for Dimitri's friend (Spool) from last session" | the save's per-player record (kit `23` §2.3) and the boat's player list; a chat line | read | S | low | probe |
| L4 | **Vessel lock shown on the boat** (scope `04` "locked into their choice") | the Setup panel's empty `State` line (already used for the weather, `05_Boat.cs` `SetupNote`) or a chat line; reverting a wrong pick with `Macro_Main.Instance.Selectcharacter` (kit `23` §3.3) | read | S | medium: the revert fights the player's clicks | probe |
| L5 | **Rejoin instructions where the dropped player lands**: "Rejoin: Steam friends, right-click the host, Join Game" | the main menu's error box shows the last line of `Blm_Multiplayer.Instance.Multiplayererror` (Bla_Ui_Multiplayererrornotif.cs:17-21; the drop adds the disconnect reason, Blm_Multiplayer.cs:238-246); the dropped player's own mod adds a line after a drop from a pack map | read | XS | low | probe (solo: a forced leave), online |
| L6 | **"<name> is back" popup** for the team (beside the stock "WARNING ▪ PLAYER DISCONNECT", Game_Client_Players.cs:31) | `Uiwarning_ClientRpc` on rejoin | read | XS | low | online |
| L7 | **Vote with a tally and a timer** (every map voted, leave vote; scope `04`) | the choice dialog on each machine, picks as commands, tally in the subtitle, closed by the host on the break timer (kit `20` §5) | proven pieces; not built (Phase 5) | M | low | probe, online |
| L8 | **Chat as a vote fallback**: "1", "2", "3" or "leave" typed in the stock chat count as votes (for a player who closed the dialog, or a pad player) | the chat is a host-written `NetworkList` whose entries carry the sender's `Displayname` (Blm_Ui_Chat.cs:119-170); the host reads new lines | read (**new**: the host sees every typed line with its sender) | S | medium: `Displayname` is not unique; ties to a player by name only | online |
| L9 | A lobby screen of its own on the main menu | none; joining the main menu has no scene to sync to | rejected 2026-10-09 (`MAIN_MENU_START.md` §4) | - | - | not recommended |
| L10 | Lobby browser, public list, password | none: no Steamworks reference in mod code, no lobby data (kit `19` §1, §5) | not possible | - | - | - |
| L11 | More than 4 players, spectators | `Starthost(4)`, approval refuses at 4 (kit `19` §1) | not possible without changing the game's data and its HUD for 4 | - | - | - |
| L12 | Host migration (a run that survives the host leaving) | none: the host leaving disconnects everyone (kit `19` §1) | not possible; the host's save and Continue are the answer (built) | - | - | - |

## 6. Other UX ideas, ranked

Ranked by value to a player against cost and risk; the first five are the recommendation.

| Rank | # | Feature | Hooks into (stock) | Engine | Cost | Risk | PC |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | T1-T4 | **The campaign card** on the loading screen, the intro card, the world card and the end screen | §4 | read; T3 built | S | low-medium | probe, eye |
| 2 | B4, T4 | **Retry warnings**: the exit dialog names the cost, the end screen says how many are left | §3, §4 | read | S | medium | probe |
| 3 | L1, L5 | **No-mods warning and rejoin instructions** | §5 | read | S | low | probe, online |
| 4 | O1 | **Weather icons on the chart**: hovering a node shows its weather's icon above it | `Macro_Addon_Nodeshowbillboardlink` needs only a sprite and plays on every client when the leader hovers (kit `15` §18.4); the weather icons exist as temporary art (`docs/TEMP_ART.md`) | read | S | low | probe, eye |
| 5 | T6 | **The pack's tips** in the game's tip lists | §4 | read | S | low | probe |
| 6 | B1b, B1c, B2 | Continue that shows the campaign; Abandon; the boat's Campaign button | §3 | read | S | low-medium | probe, eye |
| 7 | O2 | **Cleared marks on the chart**: a ring or flag on each node already sailed | a child object on the mod's own nodes (kit `15` §18.4); the chart is built in code already | read | S | low | eye |
| 8 | O3 | **A "Rogue Waves" journal page**: the rules in the game's journal (retries, voting, Pure Aura) | `Logobject` pages, as each vessel already has (kit `06`) | read; vessel pages proven | S-M | low | eye |
| 9 | O4 | **Campaign milestones** in the boat's Discoveries ("Campaign cleared", "Map 10 reached"), with the "New" badge | `Macro_Discoveriesui.Items` (kit `15` §18.4, `23` §3.5) | read | S | medium: a null condition breaks the boat's startup | probe |
| 10 | B9, B7, B8 | Score hidden; version tag; Mods-menu entry | §3 | read | XS | low | eye |
| 11 | L2, L3 | Version check after load; crew reminder | §5 | read | S | low | online |
| 12 | B10 | Host kick button | §3 | read | S | low | online |
| 13 | T7 | Credits after a won campaign | §4 | read | XS | taste | eye |
| 14 | O5 | **Music per sea weather or stretch** | the host picks a stock soundtrack with the map (kit `21` §1.4) | read (soundtrack writes proven) | XS-S | taste | eye |
| 15 | L8 | Chat vote fallback | §5 | read | S | medium | online |
| 16 | L4 | Vessel lock shown and enforced on the boat | §5 | read | S | medium | probe |
| - | O6 | Pings and photo mode | the game has both already (the `Ping` action in a map; `Pause/Photo`) | stock | 0 | - | - |
| - | O7 | The build bar's left edge off screen at 2560x1440 (`PLAYTEST_2026-10-26.md` §5) | stock HUD layout at the player's UI scale | stock | - | - | eye first (it may be the game's, not the pack's) |
| - | T8, L9 | Full-screen interlude; own lobby screen | §4, §5 | - | - | - | not recommended |
| - | O8 | Discord presence | set by the game, out of a mod's reach (kit `15` §18.5) | not possible | - | - | - |

## 7. What only the PC can confirm (one RogueLab-style round, solo)

Every row marked *probe* can ride one round with the mode's next PC round; none needs a round of its own, and none
writes stock save keys.

| # | Probe | Pass when |
| --- | --- | --- |
| U1 | `Loading` prefab rewrites `Game_Load.Ui/Bar/Desc` after the bar appears | the log reads the new text back while the bar is up; Dimitri sees it once by eye |
| U2 | `Gameclient` prefab rewrites `Cine/Maptitlealt/Desc` a frame after `Startvisuals` | the text reads back changed; the card's timing is unchanged |
| U3 | host writes `Display_Endgame` "MAP 1 OF 10 WON" at a win, after `Mode_Normal`'s write | `Win/Items["Title"]` reads it on the host; `Recordscore` hidden |
| U4 | pack tips added to `Earlygame_Funfacts` and the pause menu's list, gated on a pack save key | the pause blurb and the end-screen `Tip` read a pack line with the key set, never without it |
| U5 | `Pause` clone of `Journal`, shown again after each open; the `Exit` prompt's subtitle rewritten | the clone is active and clickable on the second open; the dialog's subtitle reads the pack's line |
| U6 | boat clone of a bottom button opening a three-card prompt | the clone shows in the row; its prompt opens and closes without an exception |
| U7 | `Tds_Clientinfo.Mods` read on the boat for every player | the host's own value is true; logged per player |
| U8 | after a forced `Leaveserver` from a pack map, the mod's line is last in `Multiplayererror` | the main menu's error box shows it |
| U9 | `Macro_Addon_Nodeshowbillboardlink` named after a chart node with a weather icon | the leader's hover shows the icon above the node |
| U10 | `Kickclient` with a fake id, and `Runprompt` twice in a row (B1c) | no exception; the second dialog opens only after the first |

Online (with the evening already planned, kit `19` §8): L1 with a friend who has no mods, L2, L5-L8, B10.

## 8. For Dimitri: what to pick

Nothing here is in the scope yet. Three ways to take it:

1. **Add the five recommended features** (§6 ranks 1-5) to the Phase 5 list, built beside the pieces they touch
   (the main-menu button, the vote, the campaign loop). This is the recommendation: each is S or XS, and they make the
   run readable without new art.
2. **Add every feasible feature** (§3-§6, the 33 marked feasible), as a polish pass after Phase 5's pieces.
3. **Keep this as a reference** and pick rows later.

Whichever is picked, the features live in the mode part (`RogueWavesMode`), not in Release 1 (Yossele alone): the
version tag (B7) and the Mods-menu entry (B8) are the only ones that also fit Release 1, at no risk.

## 9. Facts new to the kit from this reading

Written as **read**. They go into kit `20` (UI) and `15` §18.2 (tips) once a round sees them (PLAN Part A).

| # | Fact | Where (read) |
| --- | --- | --- |
| X1 | The loading screen's bar shows the map's name and "<text> - Entering" from about 3 s in; the `Loading` keyword starts as the screen opens, on every machine | Game_Load.cs:75, 165-172 |
| X2 | The map intro card (`Cine`: `Modetitle`, `Maptitlealt` + `Desc`) is written right after the `Gameclient` keyword starts, in the same call | Game_Client_Init.cs:251-320 |
| X3 | The end screen's title is the synced `Display_Endgame` (host-written: "BASIN REMAINS", "BASIN DROWNED"); the screen also shows the mode's name, map, time, Tide, score, medals and a tip | Mode_Normal.cs:305, 321; Game_Endscreen.cs:431-456 |
| X4 | Tips show in three places: the end screen (database `Other/Earlygame_Funfacts`, only while `Wave <= Tipthreshold`), the pause menu's blurb and the main menu's Close Game dialog (each its own `Factobject`); the pick never shows the last entry and throws with one | Clientmode_Normal.cs:106-131; Tds_Pausemenu.cs:255-263; Mainmenu_Main.cs:203-211 |
| X5 | `Tds_Clientinfo.Mods` (bool, owner-written) is each player's "any mod active", refreshed at every scene change; the host can read it on the boat | Tds_Clientinfo.cs:23, 57-63 |
| X6 | The host sees every chat line with its sender's `Displayname`; a client's mod can't send one (the send RPC is private) | Blm_Ui_Chat.cs:119-170 |
| X7 | `Blm_Multiplayer.Instance.Kickclient(clientId)` is public and works on the host; the stock kick RPC has no caller | Blm_Multiplayer.cs:445-455; Blm_Multiplayer_Remote.cs:76-95 |
| X8 | A client's command press sends a free `Json` string, checked only for ownership and `Controllable` | Entity_Master_Remote.cs:43-70 |
| X9 | `Credits` sends a party that came from a map back to the boat with the lobby open; `Mode_Final` goes there 15 s after its win | Credits_Main.cs:83-91; Mode_Final.cs:233-237 |
| X10 | The pause menu re-hides `Exit`, `Leavegame` and `Photo` and shows them by scene and leader each time it opens; the host's exit prompt is built in a private method ("EXIT MAP", "Return to the world?") | Tds_Pausemenu.cs:23-47, 140-170 |
