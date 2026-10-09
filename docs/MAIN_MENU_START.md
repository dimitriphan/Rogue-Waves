# Starting a campaign from the main menu (2026-10-09)

Dimitri asked (2026-10-09 16:29Z) whether, instead of equipping a Cardinal to enter the mode, the host could press a
button on the main menu, pick the mode, invite friends and start from there. This page answers it from the game's code
and the proven mods (Riptide, RogueLab), and proposes the route. It **replaces the Phase 1 gate rule** "a run starts
when the host equips the mode's Cardinal in the lobby" only if Dimitri adopts it (§6); until then `docs/SCOPE_v0.txt`
and `docs/PLAN.md` Decisions stand as they are.

Tags: **proven** = shown in game by Riptide (kit `docs/15` §13) or RogueLab (§11, §15); **read** = from the decompiled
`Assembly-CSharp.dll` (kit `docs/13` §11), file:line into that folder; **untested** = needs the PC (§5). Kit paths:
`docs/NN` = `TDS_Vessel_Creator/docs/`. The engine facts are filed once in kit `docs/15` §14.8.

## 1. Answer

**Yes.** A mod can put a button on the main menu, and that button can take the host to the game's own lobby with the
mode already set. The game has no lobby screen on the main menu: the **boat is the lobby**. Pressing Play loads the
`Setup` scene, and the boat creates the host and the Steam lobby when it opens (read, `docs/15` §14.8). So the button
does what Play does, plus one flag; the host's mod then sets the mode on the boat, where friends join by Steam invite,
pick vessels and ready up as they do today. No Cardinal is needed.

## 2. The route

| Step | Who | What the mod does | Evidence |
| --- | --- | --- | --- |
| 1. Button | host's main menu | The pack's `Default` prefab code, each time a `Mainmenu_Main` appears, clones the main menu's `Play` button (`Bl_Menus.Menus["Main"].Items["Play"]`) into a "Rogue Wave" button with its own click and tooltip | cloning a menu item and wiring `Blu3_Button`: **proven** (Riptide's Setup switch; the journal tabs, `docs/09` #25). The main menu itself: **untested** |
| 2. Choice | host | Click opens a `Runprompt`: **New campaign**, **Continue campaign** (only when the host's save holds one), **Back** | the game opens `Runprompt` on the main menu itself (its Exit dialog, Mainmenu_Main.cs:186-211): **read** |
| 3. Embark | host | Sets the "Rogue" flag on the `Default` object and calls `Mainmenu_Main.Play` (`SendMessage("Play")`), which loads the boat | `SendMessage("Play")` reaching the boat: **proven** (RogueLab, every round) |
| 4. Mode on the boat | host's mod | When the boat is up, picks the staging node (`Selectmap_ServerRpc`) and swaps `Mode` and `Gameplan` to the Rogue twin; re-swaps if the host clicks another node | **proven** (RogueLab's `Lobby()`, Riptide's switch) |
| 5. Show it | every machine | The Setup panel opens on the picked node for everyone, joiners included; a "TYPE 3 ROGUE WAVE" line on it, as Riptide's "TYPE 3 RIPTIDE" | panel for late joiners: **read** (Macro_Main.cs:445-453); the type line: **proven** (Riptide) |
| 6. Friends | host, then friends | Host presses the boat's Invite button; friends accept in Steam and land on the boat with the panel open | **read** (Macro_Main.cs:558; Blm_Multiplayer.cs:149-196, 321) |
| 7. Start | everyone | Each picks a vessel and readies; the game starts when all are ready; the mode's own staging step (map 1 vote) follows | ready-up and start: **read** (Tds_Multiplayer_Remote.cs:509-598); map 1 in the mod's mode: **proven** |

Two details that shape the build:

- **Statics don't cross prefabs.** Each prefab's code is compiled into its own assembly (`docs/15` §10), so the button
  and the boat logic live in the same `Default` prefab code (its `Update` watches for `Mainmenu_Main` and
  `Macro_Main`), not in a separate `Mainmenu` prefab. The flag clears whenever the main menu appears, so the stock Play
  button still starts a normal game.
- **A fresh save goes to the tutorial.** `Play` shows the tutorial prompt until the save has `Tutorial_New_Game`
  (Mainmenu_Main.cs:51); the button is shown only when that key is set (the same test that shows the boat on the
  menu, :85).

**Continue campaign** fits here naturally: the campaign lives in the host's save (core loop decision 1, 2026-10-09),
the host chooses it before anyone joins, and the host's mod restores each player's things by Steam id when the first
map loads. The button is also where a campaign that has ended (no retries left) says so.

## 3. Friends who join

- **They need the pack, same version** (`docs/07` rule 1). The match's mode and Gameplan are names each machine looks
  up in its own database (Game_Client_Init.cs:101-104). A friend without the pack gets nothing back, the client's
  loading stops on a null `Modeentry`, and the host's loading waits for every client (Game_Init.cs:123-134): the whole
  team hangs at the loading screen (read; not tried, and not worth trying).
- **With the pack they see the mode** before starting: the Setup panel and the Rogue Wave type line read the networked
  mode string on every machine. They don't need the button: only the host presses it. A friend who presses it too
  just hosts their own lobby, like pressing Play.
- **Joining works only on the boat.** The lobby closes when the match starts and stays closed through the map chain
  (`docs/15` §14.5). Between campaign sessions, the host presses the button again (Continue), friends join on the boat,
  and new friends can join then too.
- The host can't yet tell whether a friend has the pack: no networked value a mod can add says so. A handshake is a
  Phase 7 polish item, not needed for this.

## 4. What changes, what is rejected

**Changes if adopted:** the mode's lobby Cardinal goes away (and with it `! NEEDS NAME FOR THE CARDINAL THAT STARTS A
RUN !` in `docs/SCOPE_v0.txt`); its colour slot is free for a player's Cardinal; the unseen look of a mod Cardinal in
the lobby picker (`docs/15` §7) no longer matters for starting. The staging step and the map-1 vote stay as decided:
the boat's node is only the backdrop map 1 loads on before the vote.

**Rejected: a lobby screen on the main menu itself.** The game can host from the main menu (its tutorial does,
Mainmenu_Main.cs:133-163), but it goes straight into the match. A friend joining a host who is still on the main menu
loads the join scene (Blm_Multiplayer.cs:321-325) while the host has no boat scene to sync it to (inferred, untested), and it would
mean rebuilding the vessel picker, the ready-up and the player list the boat already has.

**Later, optional:** a mod "World" (`Tds_Masterdatabase` item of type `World`, Macro_Main.cs:209-245) could give the
campaign its own sea chart on the boat instead of the stock world map. Untested; only worth it for looks.

## 5. What only the PC can confirm: RogueLab round 4 probe spec

Solo, automated, one launch (RogueLab's driver already embarks and starts a match without input):

| # | Probe | Pass when |
| --- | --- | --- |
| M1 | Clone `Main/Play` into `RogueLab_Start` when `Mainmenu_Main` appears | the item is active, registered in `Bl_Menus.Menus["Main"].Items`, has a `Blu3_Button`; log its rect beside Play's |
| M2 | Invoke the clone's `Buttonclick` from code (the game ignores injected input) | the mod's handler runs and the stock `Play` handler does not (no second scene load) |
| M3 | `Runprompt` with three entries on the main menu, auto-answered | three cards built, no exception (the `docs/15` §14.6 rule) |
| M4 | Handler sets the flag and sends `Play` | `Setup` loads; the flag read on the boat is still set |
| M5 | On the boat, staging node plus Rogue twin, no Cardinal equipped | Setup panel open on the host, Mode reads the mod's mode, the type line reads ROGUE WAVE; Start loads map 1 in the mod's mode |
| M6 | Back to the main menu, then the stock Play | flag cleared, the boat keeps the stock mode on a picked node |
| M7 | Dump `Blm_Multiplayer.Instance`: `Joinablescenes`, `Client_Canjoin`, `Clientjoinscene`, `Serverclosescene`, `Maxplayers`, lobby id after the boat opens | logged (data, not in code) |

By hand, by Dimitri: look at the button on the main menu (placement, hover tip, gamepad focus). **Online evening**
(already planned, `docs/RESEARCH_GAPS.md`): a friend with the pack joins by invite after step 4, sees the panel and the
ROGUE WAVE line, readies, and both load map 1. No PC session is started for this until Dimitri asks.

## 6. Recommendation and the gate

**Adopt the main-menu button as the way a campaign starts**, with New and Continue, and drop the mode's lobby Cardinal.
It is built from pieces already proven (the embark, the mode swap, the Setup-screen line); only the main-menu clone
and the dialog there are new, and probes M1-M3 settle them in one solo round.

If adopted, the edits are: `docs/SCOPE_v0.txt` line 47 ("the host begins the Rogue Wave by equipping the mode's
Cardinal in the lobby" becomes "the host starts a Rogue Wave from the main menu's Rogue Wave button, New or Continue,
and invites friends to the boat"), line 48's NEEDS marker removed, the same in `/mnt/project-files/roguewave.txt`;
`docs/PLAN.md` Decisions gets the new rule; `docs/PACK.md` line 78 follows. Riptide's Setup-screen switch stays the
fallback if the main-menu clone fails on the PC.
