# Riptide implementation notes (v0.2.3, 2026-10-05)

**RIPTIDE**, a roguelike game mode: one Basin, thirty random Tides and a Salvage of artifacts between them. The design
is `DESIGN.md`; the research behind it is `reports/2026-10-04_roguelike_mode_research.md` in the ModProjects Claude Project's files, not this one's.

## Status

**Probes run three times on the PC; round 3 (v0.2.1) passed all 70 checks.** 0.2.2 only changes a screenshot's timing; 0.2.3 centres the Salvage dialog (below). The build registers the mode and its
Gameplan twins, writes each Tide into the table during the break before it, grants artifacts mid-match and runs the
research report's PC probes 1 to 5. v0.2.0 wires up what the probes cleared:

- **The Salvage** (`06_Salvage`): when a Salvage break starts, the host rolls each player's offers, warns "SALVAGE
  WASHED ASHORE", writes the offers into the SALVAGE command on that player's vessel and holds the break's clock
  (`Time_Wave` may not drop) for up to `SalvageSeconds`; then it picks at random for whoever hasn't. Each machine opens
  the game's choice dialog when its own vessel's command gets offers, and sends the pick with `Command_ServerRpc`.
  Note: the game raises the vessel's `Command` event for a player's command with `server = false` (`Updatecommand(...,
  Server: false)`); that flag means "issued by the host", so the host-side handler doesn't filter on it.
- **The SALVAGE button** (`07_Commands`): a `Commandobject` registered in Riptide's "Command" load list, key 8, lit while
  its owner has offers; pressing it reopens the dialog. Its 12×12 icon goes into `Icon_Types` at boot, with a stock
  fallback if the icon table froze first (the kit's icon pitfall).
- **Driftwood** fills a short offer; picking it gives the picker aura of every colour. A failed grant becomes
  Driftwood.
- **The RIPTIDE switch** (`08_Switch`): the host gets a copy of the Setup screen's alternate-map toggle, with
  Riptide's icon, placed beside it (or in its layout group); it sets `RiptideDirector.Switch`. Every machine rewrites
  the Setup screen's "TYPE n NORMAL" line to "TYPE n RIPTIDE" while the synced mode is Riptide's, and back when it
  isn't. Where the copy lands is logged (`RiptideSwitch.Layout`) and shot, because its placement is a guess until
  round 2.
- Each new per-frame piece runs under its own fault guard (`RiptideMod.Guard`).
- The cloud-check stubs gained `Button.onClick`, which the self-test uses to click the switch.

- **Round 1** (2026-10-04, solo as Smacky on The Hunt, the sandbox build): **45 of 48 passed**, in about 2.5 minutes.
  The game's code security accepted `PlayerPrefs`, named messages and `OnClientConnectedCallback`. The log is on the
  PC in `Desktop\ClaudeProjects\handoff\riptide_round1` (with the save from before the run). Findings, all handled in
  0.1.1:
  - After the Basin drowned, the director started a new run and ended it again on every frame until the game closed
    (`Match()` restarted whenever no run was going). A run now only starts once per match.
  - Probe 4's two fails were the test's: the dialog's list keeps a layout child (`Bl_Obj.Destroychildrenignorelayout`),
    so the test counted 4 cards and clicked the first offer. The test now finds cards by name.
  - Probe 5's fail was the test's too: the 16-byte buffer was 2 bytes short for "riptide". Sending is still untested.
  - The cards showed only icons, in the dialog's default colours, and the 2-column panel ran off the bottom of the
    screen. Cards now take the artifact's slot colour and name it on hover, and `RiptideSalvage.Fit` moves the panel
    on screen (new check in probe 4; the layout is logged).
  - The stock table and foe stats were logged; the budget curve, foe arrivals and elites are refitted to them
    (DESIGN "Tide budget curve"). Probe 2 now logs a Hardened Dagger's live Health to confirm the elite factors.
  - The end screen saved the run under the twin (`Lose_Riptide_3B_Thehunt`, `Wave_Riptide_3B_Thehunt`), as designed.
    The self-test also sets the save's last vessel pick to Smacky.
- The code compiles with Roslyn 3.8 (C# 9) against stubs of the game's API and the game's reference list: 0 errors and 0
  warnings in release, `RIPTIDE_SELFTEST`, `RIPTIDE_SELFTEST,RIPTIDE_SHOTS` and
  `RIPTIDE_SELFTEST,RIPTIDE_SANDBOX,RIPTIDE_SHOTS`. The editor script is clean too. How: `TDS_Mod/Tools/cloud-check`,
  with an empty folder as the shared-kit argument, since Riptide doesn't use the vessel kit (from `TDS_Mod/ModSource`:
  `python3 ../Tools/cloud-check/check_mod.py Riptide/Code /tmp/empty Riptide "RIPTIDE_SELFTEST,RIPTIDE_SANDBOX"`).
- The cloud-check stubs gained what probes 4 and 5 call: `UniTask<T>` with its awaiter, and Netcode's
  `CustomMessagingManager`, `FastBufferWriter`/`Reader`, `NetworkDelivery` and the connect callbacks.
- **Round 2** (2026-10-04, v0.2.0, same setup): **64 of 67 passed**, in about 3 minutes. The switch, the TYPE line,
  the Salvage end to end (offers on the command, the dialog opening by itself on screen, a click reaching the host,
  the clock held at 20 of 20, the button greying, Driftwood paying +121 aura after a timed-out pick) all passed.
  Findings, handled in 0.2.1:
  - Two "no errors" fails were the game's own: `Entity_Ai_Effect_Tower_Merchut` waits a second with its object's
    cancellation token and logs an `OperationCanceledException` whenever the effect goes first (10 times in the run,
    none from Riptide's code). The test counts these apart now.
  - "The dialog closes when the host picks" checked whether the Driftwood card was still in the list, but a closed
    menu only turns its canvas off and keeps its cards. The check now reads the menu's `Active`, as probe 4 does.
    `Abandon` also logs, and closes the menu outright if the card is gone.
  - The `setup_switch` shot was taken in the frame of the click, before the Setup screen had finished sliding in and
    before the TYPE line changed. It is now taken a second after the TYPE line check, just before the match starts,
    and the log gives the switch's and the alternate-map toggle's rectangles on screen.
  - The switch showed the stock icon when on: each toggle state has its own "Altmap" icon. All of them get Riptide's
    icon now.
  - No shot showed the SALVAGE button (the vessel was never selected). Reading `Game_Selection.Addcommand` showed the
    bar looks up `Bl_Inputs.Actions[key]` for each key without checking, and shipped commands only use 1, 4-7 and
    Quickaction. If the input map has no "8", selecting the vessel would throw and cost it its bar. `CheckKey` drops
    the key at boot when it's missing (SALVAGE is then click-only), and new checks select the vessel, find SALVAGE on
    the bar and its key, shoot the HUD (`salvage_button`), and press the button to reopen a dialog another menu closed.
- **Round 3** (2026-10-04, v0.2.1, same setup): **70 of 70 passed**, in about 3.5 minutes, with 0 errors (4 of the
  game's own cancelled tasks, counted apart). The host's auto-pick closed the dialog; the vessel's bar holds SALVAGE,
  the game has keys 1 to 8 and key 8 is bound; the button reopens a dialog another menu closed. What the shots showed:
  - `setup_switch`: "TYPE 7 RIPTIDE". The switch sits just left of the stock alternate-map toggle, and both hang from
    the bottom edge with the same rectangle (y -75 to 45 px on a 1440 px screen), so only their top third shows and
    Riptide's icon (swapped on all 4 icon parts) isn't in view. That is where the game puts its own toggle.
  - `salvage_button`: the HUD is there, but the shot came in the same frame as the selection, before the bar slid in.
    0.2.2 waits a second first.
  - `salvage_dialog`, `salvage_open`: all three cards on screen.
- **0.2.3** (2026-10-05): Dimitri, playing the 0.2.1 release, found the Salvage screen "far off centered". The game's
  dialog is laid out for the pause menu's two-card prompts: rounds 1 to 3 logged its list at x 2083-2428, y 123-468 on a
  2560x1440 screen (bottom right), a 2-column grid, so three cards showed as two plus one in the corner; 0.2.1's `Fit`
  only nudged it on screen. `RiptideSalvage.Centre` now sets the list's grid to one row (`FixedColumnCount`, up to 4),
  widens the list to hold it, forces the layout (`Canvas.ForceUpdateCanvases`, `LayoutRebuilder.ForceRebuildLayoutImmediate`)
  and moves the dialog's parts that hold the title, subtitle, hover line and cards by one screen offset, so the block of
  cards and the title's and subtitle's drawn words sits mid-screen. It runs when the cards appear, two frames later and
  at 0.5 s; `Unfit` restores positions, grid and list size after the pick. The game's compiler has no `TextAnchor`, so
  the grid's alignment stays as it is. The first Salvage of a session logs the dialog's whole layout (each part's
  place, image, text, layout group and fitter) as "Salvage dialog: ...", so a hand-played Player.log shows how it sits.
- **Not wired yet:** the whitelist (empty, so every loaded artifact but the never-offer list can be offered, until a
  hand-played run sorts them) and the journal page.
- **Round 2 checks** (the same debug build): "switch: ..." on the world map, and "salvage: ..." after probe 4, which
  runs a Salvage end to end and a timed-out Driftwood Salvage. The run's own Salvages stay off in the self-test until
  then, so probes 1 to 4 see the same match as in round 1.

## Assumptions the probes check

| Assumption | Where | Probe |
|---|---|---|
| A `Modeentryobject` cloned from Mode_Normal's, in the master database's "Mode" folder, with its `Modeobject` in a `Bl_Address` group of the same name, loads like a stock mode | `03_Content.Build` | 1 |
| Gameplan twins in the "Gameplan" folder load like their stock node, though no world-map node carries their name | `03_Content.BuildTwins`, `04_Director.WorldMap` | 1 (the match runs on the twin). The end screen's records (`Lose_Riptide_<map>`) are checked by hand. |
| `Mode_Normal` reads each table entry live on the host, so a Tide written into entry 2t-1 at the start of the break before it is what spawns | `04_Director.Write` | 2 |
| Elites through `Wavecontent.Upgrades` spawn with the upgrade and its skin | `04_Director.Plan` | 2 |
| An artifact's `Coreupgrades`, applied to the core mid-match with `Upgrade(core, up, Forced, art)`, act like a lobby pick; the apply-kinds need catch-up | `05_Artifacts.Grant`, `CatchUp` | 3 |
| Adding to the synced lobby list mid-match is safe, and `SendMessage("Updateartifactlist")` redraws the HUD row | `05_Artifacts.Tick` | 3 |
| `Bla_Ui_Confirmation.Runprompt` works from mod code; a click through `Blu3_Button.Buttonclick` returns the card | `06_Salvage.Card`, `99_SelfTest.Probe4` | 4 |
| `PlayerPrefs`, named messages and `OnClientConnectedCallback` pass the game's code security | `98_Sandbox` | 5 |
| A drowned Basin sets `Gamecomplete` to "Lose" and ends the run; the director replays the core-destroyed shot | `04_Director.Match`, `EndRun`, `CoreShot` | 1 (ending); the shot is checked by eye |

The self-test also logs what the tuning needs: the stock Tide table entry by entry, every foe's stats, drops and
threat, and the six elite upgrades' params.

## Layout

All code lives in `Code/*.cs`. `Build-RiptideMod.ps1` joins the files in name order into one Roslyn string; the first
type is the entry point.

| File | Role |
|---|---|
| 00_RiptideMod | Boot: wait for Mode_Normal's entry, load its group, build the content, load every artifact group; ticks the director and the artifact row |
| 01_Util | Log, fault guard, master-database and `Bl_Address` lookups, ids, host/match tests, entity helpers |
| 02_Data | Every name, text and tuning number: the run, the budget curve, foes, elites, banner lines, Salvage text, the never-offer list |
| 03_Content | Registers `Mode_Riptide` (entry, mode, 60-entry table) and the Gameplan twins; loads all artifacts |
| 04_Director | Host only: the switch's effect on the world map, the run (seed, writing each Tide, banner lines, opening each Salvage, the ending) |
| 05_Artifacts | Granting an artifact mid-match (core upgrades, catch-up, the lobby list, the warning); the HUD row on every machine |
| 06_Salvage | The Salvage: offers, the clock hold and the auto-pick on the host; the dialog and its cards on every machine; Driftwood |
| 07_Commands | The SALVAGE button (a `Commandobject` and its button class) and the icon it shares with the switch |
| 08_Switch | The RIPTIDE switch on the Setup screen (host) and the "TYPE n RIPTIDE" line (everyone) |
| 97_Shots, 98_Sandbox | Debug builds only: screenshots into Player.log; probe 5 |
| 99_SelfTest | Debug build only: probes 1 to 5 |

`Assets/RiptideMod/Editor/RiptideModPrefab.cs`: Tools > Riptide > Build Mod Prefab (code only, no art), Park Other Mods,
Export Mod (version 0.2.3), Restore Parked Mods.

## Build and test (Windows)

```powershell
powershell -ExecutionPolicy Bypass -Command "& 'C:\Users\Dimitri\TDS_Mod\ModSource\Riptide\Build-RiptideMod.ps1' -Define RIPTIDE_SELFTEST,RIPTIDE_SANDBOX,RIPTIDE_SHOTS"
# Unity: Tools > Riptide > Build Mod Prefab, Park Other Mods, Export Mod (wait for both passes), Restore Parked Mods
powershell -ExecutionPolicy Bypass -File C:\Users\Dimitri\TDS_Mod\ModSource\Riptide\Deploy-Wait.ps1 -Launch
# read Player.log for "[Riptide] TEST" lines; Tools\Decode-Dump.ps1 turns the screenshots into PNGs
```

- The self-test plays solo as host. It embarks from the main menu, picks Smacky and the first Mode_Normal node with no
  unlock condition, turns RIPTIDE on and readies up. It ends by drowning the Basin.
- **If the game refuses the mod**, Player.log names the refused API: that is probe 5's answer. Rebuild with
  `-Define RIPTIDE_SELFTEST,RIPTIDE_SHOTS` and run again for probes 1 to 4.
- **After the probes**, take the debug build out of `TDS\Mods` (or set `_isEnabled` to false in `RiptideMod.info`).
  It starts a match by itself at every launch.
- The other vessel mods can stay installed: Riptide touches only the "Mode" and "Gameplan" folders and loads the
  artifact groups.

## Known gaps

- The Setup screen's records line (the one that alternates with the map's description) still reads the stock map's
  best Tide, not the Riptide twin's.
- A release build can still offer any loaded artifact (no whitelist yet), including ones that only make sense when
  brought from Setup.
- A client who joins while the switch is on starts with the world map's camera at the default node, because the synced
  Gameplan name is the twin's and no node carries it. This only affects the camera.
- Unlocks tied to one stock map don't count Riptide runs; unlocks for "Any" map still do.
