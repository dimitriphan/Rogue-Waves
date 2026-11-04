# 23 · The save, unlocks and the mod loader (what a pack may write, gate and expect at boot)

Expansion rows 9 (unlocks), 10 (save compatibility) and 15 (packaging, the loader side). Everything here was **read
from the game's code** (decompiled as in `13` §11) on 2026-10-14; the `Adddata` overload question was settled from
the IL (marked *IL*). Easy Save 3, ModTool, RoslynCSharp and Steamworks are not in that assembly, so claims about them
say *untested*. A mistake here costs the player's save, so §8 is worth a PC round before the pack writes anything new.
Files of the `Blockage.Tags` namespace are cited without the prefix (`Bla_Data.cs`).

Read `15` §3, §8, §10, §18.4 and `14` first.

## 1. The save

### 1.1 Files and model

- One live system: `Bla_Data`, through Easy Save 3. `Blockage.Saveold.Bl_Save` / `Bl_Savedata` have no caller: dead
  code (the Rogue Waves `EXPANSION_SCOPE.md` row 10 names them).
- Files in `persistentDataPath` (`%USERPROFILE%\AppData\LocalLow\Blockage\TDS`): `SaveFile.es3` (the active profile,
  key `Data_Player_1`, plus a `Defaultsave` key rewritten at every boot), an ES3 backup of it after every save,
  `SaveFileSlot1..10.es3` (one per profile), `SaveFileSlotAuto.es3` (one auto copy for all slots), and
  `Oldversionbackup.es3` (Bla_Data.cs:36-142, 225).
- The data is `folder + key → Entry { Item, Valuestring, Value (float), Loaded }`; one entry holds both a float and a
  string, and each `Adddata` overload changes only its own half (Datacontainer.cs:33-119; Bl_Tags.cs:24-40).
- *IL:* the game's own two-argument `Adddata(folder, key)` calls (as the decompile prints them) are the **float**
  overload with 0 and `Replace`. In mod code a two-argument call does not compile (both overloads match: CS0121 with
  Roslyn 3.8), so the kit's rule "always pass the typed third argument" stands.
- `Readdata(folder, key)` returns `Loaded = false` when missing; `Readdata(key)` searches **every** folder.

Stock folders: `Player` (selections, the last match), `Map` (per Gameplan: `Clear_`, `Time_`, `Wave_`, medals,
`Leaderboarddata_`…), `Maphistory`, `Unlock`, `Unlock_Notif`, `Unlock_Other` (event flags), `Tutorial`, `Settings`,
`Settings_Sounds`, `Metadata_Persistent`, and `Metadata` (**wiped on every load**, `19` §5).

### 1.2 When it loads and saves

- Boot: `Bla_Data.Start` loads the profile, else the backup, else a fresh one; drops `Metadata`; fires `Onload`
  (Bla_Data.cs:81-236). This is long before any mod loads, so **a mod never sees `Onload`**.

| When | Call | Where |
| --- | --- | --- |
| Match end, every machine | `Savedata(true, map)` (auto slot too) | Game_Endscreen.cs:510-514 |
| Boat ready, every machine, after the unlock sequence | `Savedata(true, world)` | Macro_Main.cs:513-521 |
| Quit, slot switch, slot clear | `Savedata(false)` | Bla_Data.cs:150-153, 267-338 |

- **The match-end save runs before the match's stats are written** (`End` saves, then `Runendsequence` writes `Map/*`,
  `Maphistory`, medals): those reach disk at the boat save or at quit; a crash between the end screen and the boat
  loses them.
- `Onsave` fires **after** the files are written: a mod writing inside it misses that save.

### 1.3 Every machine has its own save

No save sync exists. After an online match **every player** writes their own `Map/*`, medals and unlocks; back on the
boat each machine runs the unlock sequence against its own save. Only the **host** restores the world, Gameplan and
equipped Cardinals from its save into the network state; clients' `Player_Artifactselect` is rewritten from what the
host equips (Macro_Main.cs:157-191, 337-394, 509-511).

### 1.4 Slots, backups, corruption

- A slot switch saves, loads the other file, **replaces `Bla_Data.Playerdata`**, copies the settings, saves, then fires
  `Onslotchanged`; a clear replaces it and fires `Onslotremoved` (Bla_Data.cs:249-342). Each slot carries its own copy
  of every folder, the pack's included. "Restore auto save" rolls back to the last `Savedata(true)`, so pack data
  flushed since with `Savedata(false)` is lost.
- Load tries the backup only when the key is **missing**. **An unreadable file is not caught**: `Loaded` stays false
  and the game runs on an empty profile that **never saves** (predicted); the file on disk is left as it was.
- No checksum, schema version or migration code. Unknown folders and keys load and save untouched.
- No cloud API is called; whether Steam Auto-Cloud syncs the folder is a Steamworks setting (§8).

## 2. What a mod may write

1. **Only through the API:** `Readdata`, `Adddata(folder, key, string)`, `Adddata(folder, key, float, Addtype)`,
   `Removedata`, `Removefolder`, then `Bla_Data.Instance.Savedata(false, "")` (RogueWavesMode's `RWUtil.Flush`).
   **Never touch `Folders`, `Folderslist`, `Data` or `Datalist`:** `Initialize` rebuilds dictionaries with `Add`, so
   one duplicate key makes the **next launch load an empty profile that never saves**.
2. **Its own folder** (`RogueWaves`): survives every load, save and slot copy; removed only by Clear Save. Never write
   `Metadata` (wiped), `Settings_Sounds` (every key is fed to the mixer as a volume), `Settings`,
   `Metadata_Persistent`, or stock folders for stock content.
3. **Prefix every key** (`RW_`): stock trackers with folder `"Any"` search every folder, so a pack key named like a
   stock key could advance a stock unlock. Never put `Unlock_Hero` or `Unlock_Artifact` in a pack key that is not a
   vessel or Cardinal unlock (the boat opens a panel on the substring, §3.2).
4. **Floats hold whole numbers exactly only up to 16 777 216:** seeds and big counters go in strings. Guard
   against NaN (a `Divide` by 0 makes one).
5. **Never keep a reference** to `Bla_Data.Playerdata`: re-read state after `Onslotchanged` **and** `Onslotremoved`, and
   check `Playerdata.Loaded` before trusting a read.
6. **Handlers on `Onsave`, `Onslotchanged`, `Onslotremoved` must not throw:** they are invoked without `try`, and a
   throwing `Onsave` at match end would abort the end screen (Game_Endscreen.cs:510-518).
7. Keep the pack's data small and save at state changes, never per frame (each save serializes the whole profile on the
   main thread).

### 2.1 When the pack is gone

Nothing reads or prunes unknown folders; the pack's folder waits, invisible, until Clear Save or the pack's return.
Stock values that point at pack content:

| Saved value | Effect without the pack | Mitigation |
| --- | --- | --- |
| `Player/Player_Worldselect` = a pack world (the chart) | **the boat never loads** (waits for the world with no timeout, Macro_Main.cs:356-417) | RogueWavesMode's guard (`05_Boat.cs`) |
| `Player/Player_Heroselect` = a pack vessel | the host ignores the unknown hero; no vessel selected | the player picks by hand (`09` #22); a pack guard could keep a stock vessel saved |
| a pack Cardinal or Trinket in `Player_Artifactselect` | a ghost entry, skipped everywhere | never leave pack Cardinals or Trinkets equipped on the boat |
| `Unlock/Unlock_Hero_<pack>`, `Map/*_<pack plan>` | inert | none |
| `Unlock_Notif/<pack key>` still 0 | one blank unlock panel to click away | none |

The leader is always the host (`19` §1), so only a host's save gets the pack world; the guard on the host is enough
today, and it should run on every machine if the pack ever passes leadership.

### 2.2 Versioning (recipe, untested)

Keep `RogueWaves/RW_Schema`. Missing: write it. Older: migrate key by key, then write it and flush. **Newer than this
build: go read-only** and warn once, so a downgrade cannot clobber newer data. Offer "forget Rogue Waves"
(`Removefolder("RogueWaves")` plus the clean-ups above). Recipe: `examples/recipes/RecipeSave.cs`.

### 2.3 Per-player data across sessions

`Sessionid`s last one host session and change when a client restarts (`19` §5). Per-player campaign data keys on the
synced Steam name (`RW_P_<Displayname>_…`, `RecipeSave.PlayerKey`): the Steam id is out of reach for mod code (`19`
§5). Two friends with one name share a row; a rename starts a new one.

## 3. Unlocks

### 3.1 The stock system

- `Unlockentryobject { Dataadd (the save key), Priority, Livetracker (list of Unlock_Tracker_Base) }` in
  `Items["Unlock"]` (105 entries). Trackers: `Additive` (sums keys, writes `_Counter`), `Conditions` (`Comparedata`
  AND list), `Matchreader` (reads the last match; `Gameplan = "Any"` by default), `Monobehaviour` (game types only:
  Pig, Kippy, Bird).
- `Tds_Data.Checkunlocks` runs **only at the start of the boat's unlock sequence**, on every machine, against the local
  save: sorts entries by `Priority`, skips those already unlocked, runs all trackers, on a pass writes
  `Unlock/<Dataadd>` = 1 and `Unlock_Notif/<Dataadd>` = 0 (Tds_Data.cs:23-64). **An unlock is earned at the next boat
  visit.** A non-`Unlockentryobject` in `Items["Unlock"]` makes the sort throw.
- `Unlock_Tracker_Base.Run` is virtual: a mod subclass works in principle, but the stock `Conditions` needs no new type.

### 3.2 What the boat announces

`Macro_Unlock.Unlockseq` (once per boat visit): `Checkunlocks`; then each new `Unlock_Notif` key either flies to a world
`Macro_Unlocklink_Add` with that key, or opens the **Cardinal panel** (key contains `Unlock_Artifact`) or the **vessel
panel** (key contains `Unlock_Hero`); **any other key is marked seen with no announcement** (Macro_Unlock.cs:303-994).
So a Depth unlock through the stock system is silent (`15` §18.4 says the same): use a Milestone or the pack's own message.

### 3.3 Vessel locks are cosmetic

The boat locks a hero without `Unlock/Unlock_Hero_<Objectname>`, but the lock only blocks the mouse: the Left/Right
arrows cycle every hero, and the host accepts any hero that exists (Macro_Worldvisual.cs:392-480, 1194;
Tds_Multiplayer_Remote.cs:280-283). A pack gate enforces itself (revert a locked pick with
`Macro_Main.Instance.Selectcharacter(<stock>)`). The kit's `ModKitBoat.Unlock` clears `Locked` at run time without
writing the save.

### 3.4 Recipe: unlock a vessel after a won campaign (untested)

1. At boot, on every machine: an `Unlockentryobject` `RW_Unlock_<X>` with `Dataadd = "Unlock_Hero_" + <X's
   Objectname>`, high `Priority`, and one `Unlock_Tracker_Conditions` on `RogueWaves/RW_CampaignsWon >= 1`; set with the
   indexer `Items["Unlock"][name] = u`.
2. On the campaign's final win, **each player's own machine** adds 1 to `RogueWaves/RW_CampaignsWon` and flushes (the
   host tells clients with a message, `15` §18.2; or the final map uses a plan name only finals use, and the condition
   reads the `Map/Clear_RW_Final_…` key every end screen writes).
3. The next boat visit writes the unlock and shows the vessel panel; the vessel part calls `ModKitBoat.Unlock` only
   when the key exists (a kit change) and reverts arrow picks while locked.

### 3.5 Milestones (Discoveries)

`Macro_Discoveriesui.Items` is a boat scene object; append the pack's Milestones on every boat visit, on every machine.
**A Milestone with a null condition, or a blip condition with an empty tracker list, breaks the boat's startup**
(Macro_Extraui.cs:89; Macro_Discoveriesui.cs:120, 199).

### 3.6 What modded play still counts

Match stats, medals, `Matchreader` trackers with `"Any"`, special-bird keys from pack twins (each twin counts as a new
map for the Bird unlock) and achievements all count in modded play. A final built on `Mode_Final`'s script ends with
`Gamecomplete = "Other"`: no end-screen stats, then the Credits, which reset the saved world to the home world.

## 4. The mod loader

### 4.1 Boot order

1. First scene: `Bla_Data` loads the save; `Tds_Masterdatabase` builds `Items`; `Bla_Addressables` loads the boot
   groups; `Bla_Mods` creates the RoslynCSharp domain and **waits 5 s** (Bla_Mods.cs:60-67); `Init_Main` waits 3 s +
   1 s and loads `Mainmenu` (Init_Main.cs:28-60).
2. About 5 s after boot: `ModManager.Refresh()`; each enabled mod loads; `OnModLoaded` activates its `Default`
   prefabs, sets `Modactive` (Bla_Mods.cs:67-119).

So **the first main menu after launch most likely appears before any mod has loaded** and gets no `Mainmenu` prefab
(predicted). The pack's `Default` prefab that watches for `Mainmenu_Main` (`docs/mode/MAIN_MENU_START.md`) is right.

### 4.2 Keywords

| Keyword | When | Machines |
| --- | --- | --- |
| `Default` | when the mod loads | every |
| `Mainmenu` | each main menu start (likely not the first) | every |
| `World` | boat startup, before the unlock sequence | host |
| `Loading` | the loading screen | every |
| `Gameserver` | after `Generatemode` and `Startgame` | host |
| `Gameclient` | `Startvisuals` (skipped by a mid-map rejoin) | every |

Each activation compiles the prefab's code again into a new assembly. Keyword instances die with their scene. **No
game callback exists for match start, scene change, save or unload**: a mod polls, or hooks
`SceneManager.sceneLoaded`, `Bla_Data.Onsave/Onslotchanged/Onslotremoved`, `Bla_Mods.Instance.Modchanged`, or a
network variable's `OnValueChanged`.

### 4.3 Errors and Toggle

- **A compile or security failure throws inside the activation loop:** for `Default` prefabs, the rest of **that
  mod's** `Default` prefabs are skipped, so in the one-bundle pack **one part's compile error stops the parts after it**
  (narrows `docs/PACK.md` §1). For a keyword prefab the exception leaves the **game's** caller: a broken `Mainmenu` or
  `World` prefab stops the main menu or the boat for every mod. Ship `Default` prefabs only.
- **Toggle undoes nothing:** `OnModUnloaded` only removes the mod from the list; its compiled code, statics,
  registrations and delegate hooks stay; toggling on again compiles a second copy. Its guard `loadState != 1 ||
  loadState != 3` is always true (Bla_Mods.cs:92-133). **Players restart the game after enabling or disabling the
  pack** (README line).
- The Mods menu (`Bla_Ui_Mods`): title, author, description, version, image, state (`CONFLICT`, `INVALID`, `ENABLED`,
  load %, `DISABLED`), a toggle and Refresh. ModTool's sample `ModMenu` is unused.

## 5. Records, achievements, the end screen

- `Game_Endscreen.Validate`: -2 with the Sandbox Cardinal (no stats at all); -1 when **any** player's
  `Tds_Clientinfo.Mods` is set, this machine has a mod active, the plan has `Record` false, it is `Special_Custom`, or
  the map was Treated (stats and medals written, no leaderboard record) (Game_Endscreen.cs:23-62). **One modded player
  switches records off for the whole lobby.**
- Leaderboard submission is not gated on mods: the boat re-submits the player's unmodded total.
- **Achievements are not gated on mods** (Bla_Data_Steamachievements.cs:76-110): a key in the pack's own folder never
  trips one; writing stock keys for stock content could.
- The end screen reads every player's `Heroentryobject` with no null check: a vessel unknown on one machine breaks that
  machine's stats panel.
- A self-test that equips the Sandbox Cardinal earns nothing (stats and unlocks skipped).

## 6. Findings for the pack's existing code

1. **Rejoin after a restart** gets a new `Sessionid` and RogueWavesMode turns it away (`19` §5); `docs/mode/REJOIN.md`
   O1-O3 should test the two cases apart. **Fixed in RogueWavesMode 0.3.0** (2026-10-15; passed a real restart and rejoin on 2026-11-04, `19` §8 N9): the pack
   keeps the GUID in its `RogueWaves` folder and puts it back into `Metadata` (`RWIdentity`, `19` §5).
2. **Campaign statics survive a slot switch:** after a switch the next `Save()` would write slot A's campaign into
   slot B. **Fixed in 0.3.0:** `Bla_Data.Playerdata` is replaced only at launch, on `Switchslot` and on `Cleardata`
   of the current slot (Blockage.Tags.Bla_Data.cs:173-343), so the campaign remembers the `Playerdata` object it came
   from; `RWCampaign.Live` leaves it behind (in its own slot) when that object changed, checked on `Onslotchanged`,
   `Onslotremoved`, before each save and at each map start. New/Continue re-read.
3. **"Restore auto save" rolls pack data back** to the last stock autosave (pack flushes use `Savedata(false)`).
   Acceptable; worth a README line.

## 7. Pitfalls (predicted from the code; move to `09` once met)

1. Editing the save's lists directly, or a null folder or key name → the next launch loads an empty profile that never
   saves.
2. A throwing handler on a save or slot event stops the game's code after the call.
3. The match-end save comes before the stats; flush pack state at its own changes.
4. A Milestone or blip condition without a tracker breaks the boat.
5. A pack key containing `Unlock_Hero` or `Unlock_Artifact` opens a blank panel.
6. The vessel lock is cosmetic.
7. One part's compile error stops the later parts of the bundle; a non-`Default` keyword prefab can take a game scene
   down.

## 8. PC list

1. **Format:** open `SaveFile.es3` in a text editor (JSON or encrypted; the backup's file name; the `RogueWaves`
   folder).
2. **Uninstall:** with a campaign on the chart, a pack vessel selected and a pack key in `Unlock`, quit, disable the
   pack, launch, Play: the boat loads, a stock vessel can be picked, no exception. Re-enable: Continue finds the
   campaign.
3. **Slots:** with a campaign active, switch slot on the main menu and embark: the log says "campaign left behind",
   the boat shows the stock world, and the new slot's save has no `RogueWaves/RW_Campaign_Seed` (§6.2; the self-test's
   S2 pretends the switch).
4. **First main menu:** a dev `Mainmenu` prefab that logs on `Awake`: does it log at the first main menu?
5. **Compile failure:** a dev bundle with two `Default` prefabs, the first with a compile error: does the second start?
6. **Toggle mid-session:** exceptions, two instances after re-enabling?
7. **Arrows:** can Left/Right select a locked stock vessel?
8. **`Player_Guid`:** log it at boot on two launches: new each launch (the `19` §5 consequence)?
9. **Pack unlock:** §3.4 in a dev build with the key set by hand.
10. **Steam Cloud:** does Steam list `SaveFile.es3` in the game's remote cache?

## 9. Sources

The decompiled `Assembly-CSharp.dll` (Bla_Data, Datacontainer, Bl_Tags, Tds_Data, Unlock*, Macro_Unlock,
Macro_Worldvisual, Macro_Main, Macro_Discoveriesui, Bla_Mods, Bla_Ui_Mods, Game_Endscreen, Bla_Data_Steamachievements,
Init_Main, Mainmenu_Main), read on 2026-10-14 by a parallel reader who spot-checked its citations; the `Metadata` wipe
and the GUID were re-checked by hand (`19` §9). The pack's save code: `TDS_Mod/ModSource/RogueWavesMode` (`01_Util.cs`,
`05_Boat.cs`, `06_Campaign.cs`).
