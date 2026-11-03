# 09 · Pitfalls: every error met so far, and its fix

Each entry: **symptom** → cause → fix (where the fix lives in the template). Most of these happened while building Smocky
(the first test clone), Sprigg 1.0-1.4.1, Argo 1.0, Mummer 1.0, the kit 1.4 boat work, Argo 2.0 and RogueLab; the later ones ending "Read from code" or "Read from the game's assets" were read, not hit. Add new
ones as the next number in §J (older entries kept suffixes such as 18a and 40a).

## A. Compiling

1. **Compiles in Unity / csc but fails in game** (`HashSet`, `System.Linq`, `JsonUtility`, `TextAnchor` not found) →
   the game's compiler references a fixed list without `System.Core`, `JSONSerializeModule`, `TextRenderingModule` →
   use `List` + `Contains`, hand-written JSON readers (`JsonString`, `JsonNumber`), no LINQ; the build script compiles
   against exactly the game's list, so trust its result, not Unity's.
2. **Mod refused/blocked** → `System.IO` or `System.Reflection` used (blocked by the code security settings) → the build
   script rejects them; read files in the editor generator instead; call private game methods with
   `GameObject.SendMessage(name)` when unavoidable.
3. **C# features missing** → Roslyn 3.8 = C# 9: no file-scoped namespaces, no `record struct`, no global usings.
4. **Duplicate type when two mods (or a mod and the kit) define the same class** → the build script renames the kit's
   `ModKit` prefix to `<Name>Kit`; prefix your own classes with the vessel name. Renaming the kit once collided with the
   vessel's own `MummerJournal` → keep kit and vessel names distinct (`MummerKitVesselJournal` vs `MummerJournal`).
5. **"member names cannot be the same as their enclosing type" / ambiguous `Mark`** → a nested class and a method shared
   a name (`MummerServer.Mark` class + `Mark()` method) → rename the class (`MarkInfo`).
6. **`?.` on Unity objects lies** → a destroyed `UnityEngine.Object` is "== null" but `?.` doesn't see it → write explicit
   `x != null` checks.
7. **PowerShell `-File` breaks `-Define A,B`** → pass it with `-Command "& .\Build-X.ps1 -Define A,B"`.

## B. Loading and data

8. **Mod AI classes in `Aiscripts` never run** → the engine attaches scripts with `Type.GetType(name)` from the game's
   assembly, and upgrade pruning compares type names → brains as MonoBehaviours added in `Game_Entity.Onspawn`; stock AIs
   via name carriers (`Util.Script`).
9. **A cloned tower behaves like Smacky's or throws** → copied vessel-specific scripts (`Entity_Ai_Tower_Smackyarcher`
   hard-codes Smacky's objects; the generic `Entity_Ai_General_Upgrade` runs the stock upgrade flow) → `Strip()` removes
   any script named after a shipped vessel, and the building brain mirrors the upgrade AI.
10. **Edits to `Visualproperties.Default` have no effect** → `Entry` is a struct → copy, change, assign back.
11. **NRE in `Bl_Address_Group.OnEnable`** → `ScriptableObject.CreateInstance` leaves its item list null → `Instantiate`
    an existing group (`template.Heroloads`) and retag it.
12. **Another mod's object disappears / the wrong object loads** → two mods registered the same `Objectname` in the
    global `Bl_Address.Groupsname` → every id contains the vessel name (the scaffold also renames the template's unit,
    `Understudy`, whose ids carried no vessel name).
13. **Names truncated on the network** → `FixedString64Bytes` holds 61 UTF-8 bytes → ids ≤ 60 chars (`Util.Clip`).
14. **Leftover stats do nothing / confuse the panel** → cloning kept the template's stats (Projduration, Projcount) →
    `Stats()` builds exactly the listed stats.
15. **Borrowed piece missing (Curtain without Buster's pad)** → that vessel's group wasn't loaded → load it at boot
    (`Data.ExtraGroups`), fall back to a Smacky piece if absent; groups are never unloaded, so it's safe all session.
16. **An upgrade param buffs the wrong thing** (Method Acting's `Health_Multi` would have buffed the Stage itself) →
    params named after a stat the building owns become modifiers on the building → name unit-stat params differently
    (`Puppethealth_Multi`) and apply them to units yourself.

## C. Menus, boat, icons

17. **The vessel shows only as a shadow on the boat** → `Populatecharselect` locks vessels missing from the save and the
    button's `Start` already sent "Locked" to its VFX → set `button.Locked = false`, then `SendMessage("Start")`
    (`ModKitBoat.Unlock`).
18. **No boat spot, or extra spots in a line off the stern** → one spot per vessel; the old code repeated the last gap →
    `ModKitBoat` adds spots and lays extras on the deck between the shipped ones.
18a. **Some vessels can't be clicked on a crowded boat** (10 vessels with kit 1.0-1.3) → a click picks the nearest
    vessel box, and extras in the gaps 0.14 off the line covered their neighbours' boxes → kit 1.4 stands every vessel
    in evenly spaced, staggered rows, back rows higher, with the boxes moved onto the sprites; the self-test's
    `ModKitBoat.ClickReport` fires the game's own raycast over each box.
19. **Sprite sheets drawn on the startup screen** → the game instantiates the mod prefab at startup and the library's
    Sprites holder was active → save every library holder inactive.
20. **Ability/resource icons blank or stock** → `Helper_Uitools` copies the icon table on the **first** `Geticon` and
    never again; Sprigg 1.3 asked for an icon right after its build, freezing the table before other mods added
    theirs → add icons at boot before any asset loading; never call `Geticon` while building; check 8 s later and fall
    back to stock icon names (`CheckIcons`).
21. **Picker shows no border/prompt or throws** → Fie's Reroute assets are only loaded when Fie is in the match → use
    Sell's (`Sellborder`, `Sellfromborder`, `Ui_Command_Sell`); never hand the selection loop a null border (a hidden
    placeholder object).
22. **Risk when uninstalling a vessel** → at startup the game calls `Selectcharacter` with the save's
    `Player_Heroselect`, which the self-test sets to the tested vessel; if that vessel is gone the selection points at
    nothing (not observed, avoided) → before removing a vessel mod, select another vessel in game. Read from code
    since (`23` §2.1): the host ignores the unknown hero and the player picks one by hand; a saved pack *world* is
    worse (the boat never loads), which RogueWavesMode guards.

## D. Journal

23. **Smacky's tab missing on a vessel page** → `Instantiate` remaps a page's self-references, turning the template's own
    tab into the clone's → `ModKitVesselTabs.Sync()` after building, and `Tick()` every frame.
24. **New journal links/3D views don't draw** → brand-new `Link` + `Image3D` items lack fields the renderer expects →
    reuse/deep-copy template items (`Fresh<T>`).
25. **A stray page with its own tab row appears** → a scratch clone used for copying items was left alive → destroy
    scratch pages.
26. **The journal's 3D view frames only part of a tower** → it fits the model by its first renderer → a disabled
    whole-model renderer on `Vis`.

## E. Gameplay and engine

27. **The vessel hits twice** (Sprigg: an invisible melee hit from 3.5 tiles plus its seed) → the cloned melee AI stayed
    next to the ranged one → exactly one stock attack AI.
28. **Custom animation cut off instantly** → `Animplaysoft` keeps the current priority, so Idle/Move replace it → 
    `Animplay(name, Game_Sprite.Animationtype.Force)`.
29. **Projectiles behave like the previous shot** → each shot starts from a copy of the projectile's `Defaultparams` and
    only the params passed override it, but `Projdata.Data` is kept by reference (Helper_Damagetools.cs:262, 305) →
    pass every param the shot needs and a fresh `Data` dictionary every shot.
30. **Pierce damage scales wrong / changes other hits** → each hit gets a copy of the projectile's `Data`, but handlers
    later in that hit's `Oninteract` chain see an edit, and some AIs reuse one dictionary across hits → copy before
    edit.
31. **Status looks (Marked, held, tangled, slow) never appear** → sent with `Beingserver.Beingrefparallel`, a copy taken
    before `Spawn` set its `Worldobject`, so clients can't resolve the target → `Vfxdebuffcheck(name,
    target.Beingref.Value, s)` (`Util.Debuff`). Sprigg ≤ 1.4.0 and Argo 1.0 have this bug in their slows.
32. **A tile lookup finds nothing** → `Getblock(pos)` matches the exact tile position (hundredths) → snap to the tile
    centre and use the ground's `Surface` entity (`HeroBrain.Tile`), trying the same level, one down, one up.
33. **A pulled enemy walks back to its fight** → its combat AI re-targets every slow tick → re-assert your Move each
    frame for the pull window, then root it (`KeepReeling`).
34. **A tower stopped aiming** → its bespoke AI (which turned the turret) was stripped → design attacks that don't
    need aiming (Thornroot) or rotate the `Vis/<Part>` on the client from Vis events (Argo).
35. **The first Tide halves a starting resource** → a "per Tide" rule fired when Tide 1 began → skip it when the previous
    Tide is below 1.
36. **A button re-runs its ability when its look changes** → `Updatecommand(m, link, json, server, run: true)` invokes
    the command handlers → use `run: false` to only sync Goalstring/Goalvalue. (Argo 1.0's SONAR uses `true`,
    probably warning "recharging" on every use.)
37. **Enemy throws every tick** → spawned without a parent; `Approachgoal` reads its parent's Goal → spawn with
    `Createlinkspawnparent(enemyCore.Beingserver)`.
38. **A despawned foe reads 0 health** → stats are cleared → read base values from `Entity.Defaultstat`.
39. **Armoured foes ignore damage numbers** → `Healtharmor` makes each hit count as its `Damagemulti` (1 by default),
    whatever its Damage → expected; design around it.
40. **A unit spawned for building A gets building B's settings** → several spawns in a row and `Onspawn` reading the wrong
    context → `Onspawn` fires synchronously inside `Spawn`: set a static "pending" field right before and clear it
    right after.
40a. **A pull or test measured from a walking foe's tile acts a tile early** → `Beingserver.Currentblock` is the tile
    the foe is stepping onto: `Entity_Ai_Mob_Movement` claims it as the step starts, then waits 1/Speed (a foe held at
    the 0.05 floor sits on it for 20 s while it looks a tile back) → measure from it knowingly, and give test foes room
    from the tile in front of them (Argo's `Lane`).
40b. **"The first foe a piercing shot hits" is sometimes the one behind** → `Projectile_Master` sweeps each step with
    `Physics.SphereCastAll` and calls `Collide` in the array's order, which isn't by distance → collect the hits of
    that step (they all land in one frame) and pick the nearest yourself (Argo's `ReelNearest`).

## F. Visuals and audio

41. **Area effect smaller/larger than its real reach** → drawn at a fixed size → scale it to the radius
    (`ParticleSystemScalingMode.Shape`, transform scale).
42. **Dome bursts spray sideways** → Unity's Hemisphere shape faces +Z → rotate it (`HemisphereUp()`).
43. **Effects float or sink** → `Entityposition.y` is ground + 0.5; debuffs attach at the target's feet (`Vis` origin) →
    ground effects at y - 0.5; overhead markers ~1.4-1.7 up.
44. **Marker hidden behind tall enemies** → placed at 1.15 → raise and enlarge (Mummer 1.0: 1.6, r 0.21).
45. **Tower details invisible in game** → modelled on the far side (Mummer's quilt) → put them on the front (-z) and top.
46. **Spindly towers** → parts thinner than ~0.05 → thicken.
47. **Towers too small** (Sprigg 0.6) or overlapping neighbours → height 0.8-1.45, footprint inside the tile.
48. **Sprite reads muddy** → noise texture (felt "nap" dots) → one sheen line; a limb behind the body → draw order; thin
    thread lines invisible → 2 px or lighter colour.
49. **String-drawn icon malformed** → rows of unequal length → check every row's length.
50. **Splash parts look like pills** → drawn as separate shapes → one chained shape per limb.
51. **Sounds ~20× too loud** → normalised clips at volume 1 → per-clip volumes from the measured game levels, mixer group
    of the "Upgrade" sound.

## G. Build, export, tools

52. **Another mod's assets inside your bundle** → ModTool exports every prefab/ScriptableObject outside `/Editor/` →
    Park Other Mods (to `Assets/_Parked/Editor`: the path contains `/Editor/`, so the exporter skips it, and every
    vessel mod parks to the same place), Export, Restore. Parked editor scripts still compile; expect a recompile after
    each park and restore.
53. **Export Mod "times out" in UnityMCP and runs twice** → the bridge retries a slow menu call → wait for the .info
    timestamp to settle before Restore.
54. **The installed mod is disabled after an export** → the exporter writes `"_isEnabled": false` → `Deploy-Wait.ps1`.
55. **Build All Assets produced the old art** → it ran before Unity recompiled the edited generator → check
    `Library/ScriptAssemblies/Assembly-CSharp-Editor.dll` is newer than your edit.
56. **A restore looked failed and a parallel cleanup deleted a folder** → never batch deletes with unverified steps;
    verify file counts after every park/restore.
57. **UnityMCP won't start** → HTTP mode collides with NVIDIA Broadcast on 8080, and letting it reinstall its server
    pulls broken versions → stdio mode, pinned venv (`docs/11`).
58. **PowerShell array garbles a scripted edit** → in `@('a', 'b' + $x)` the comma binds before `+` → parenthesise, or
    write new text to a file and splice it. `Set-Content` defaults to ANSI: pass `-Encoding utf8`; reading a UTF-8 file
    without BOM via `Get-Content -Raw` can mangle `▪`.
59. **No decompiler** → read IL with the Cecil scripts (`tools/cecil`).
59a. **A scripted rename changed nothing** → a PowerShell hashtable (`[ordered]@{}` too) treats `Mummer`, `MUMMER`
    and `mummer` as one key, so only the last mapping survived → a list of pairs (`New-Vessel.ps1`).
59b. **A new folder never imports** → UnityMCP `refresh_unity` didn't pick up a folder created outside Unity →
    `execute_menu_item("Assets/Refresh")`, then wait for the `.meta` files and the editor assembly.

## H. Testing

60. **Tides never advance in the test** → `Time_Wave` set to a small positive number → set it ≤ 0.
61. **Stat checks fail right after a spawn** → stats are filled a frame later → wait.
62. **Spawn helpers find no tile** → only one height level searched → search `Surface` entities at y, y-1, y+1.
63. **Checks on a destroyed foe** → Unity null vs C# reference → explicit spawned flags.
64. **A totals check fails because the vessel killed the target first** → count events instead.
65. **Desktop screenshot captured another game** → never screenshot the desktop; use in-mod captures only.
66. **`Stop-Process TDS` does nothing** → the process is `TDSgame`; close with `CloseMainWindow()`.
67. **Log evidence gone** → `Player.log` is overwritten each launch → copy it after every run.

## I. Process

68. Ask before launching TDS each session; the user may be playing something else.
69. Don't change the user's concept silently: list every change and its reason in DESIGN.md and the final report.
70. Report honestly: what was verified in game, what only compiled, and what is unverified.

## J. Added later

71. **No `Command_Buildoption1`-`4` to clone at boot** (Argo 2.0, PC round 1, 2026-10-08) → the vessels' build
    commands aren't in any group loaded when a mod boots (inferred: the match loads them, since every vessel creates
    them when it lands) → clone them when they appear (Argo's `ArgoContent.LateClones`, once a second from `Update`),
    and look them up quietly: `Bl_Address.Getload` logs "Could not retrieve ... The GROUP exists, but item does not"
    on every miss.
72. **Clients keep an old value a synced command carries** (Argo 2.0, PC rounds 1-3: the Tremor radius read 2.4 on
    clients while the host sent 4.4) → the synced `Commandlist` held two entries of the same name, and
    `Updatecommand` updates only the first it finds and moves it to the end → keep one entry on the host (remove the
    rest before updating), and on clients read the **last** entry of that name (`ArgoClient.Aimed`). Verified in game
    (round 3 removed one stale entry; the check passed).
73. **A tower's effect check reads too high** (Argo 2.0, round 2: Brine measured 12 damage a second) → the vessel's
    other guns were shooting the same foe → while measuring one building, stop every other gun and the vessel, and
    clear the vessel's own debuffs on the dummy before each hit. Verified in game (rounds 2-5).
74. **The self-test finds no tile for a building's neighbours** (Argo 2.0, round 3, Thehunt's hill: Deep Water on a
    one-tile ridge left no room for its pools) → search for a spot by what the building needs around it (five tiles on
    one level for Deep Water and its pools) and build it last, after the guns took their spots; log what blocks each
    neighbour when the search fails. Verified in game (rounds 4-5).
75. **The self-test summary is last launch's** (Argo 2.0, round 2) → a Steam Cloud "syncfailed" dialog held the launch,
    TDS never ran, and `Run-SelfTests.ps1` summarized the old `Player.log` → it now refuses a Player.log the game didn't
    write that launch (`Tools/pc`, 46e53bd); Steam logs the hold in `Steam\logs\console_log.txt`. Verified on the PC.
76. **The Build Queue call over UnityMCP times out, then its result reads "still compiling"** (Argo 2.0, rounds 1-6) →
    the call runs anyway; MCP replays it 2-5 times, each export restores the parked mods and forces a recompile, and the
    replays overwrite `queue_status.txt` after a good run → make one call and trust the `[ModKit]` lines in
    `%LOCALAPPDATA%\Unity\Editor\Editor.log` (and the `.info` file's time). Verified on the PC.
77. **The game's choice dialog shows one card, then the mod's prompt ends "Faulted"** (RogueLab, rounds 1-2) →
    `Runprompt` gives each card to `GetComponentInParent<Blu2_Menu_Handler>()`, which skips inactive objects; opened
    from code while its menu (`Confirmation`) was down, the lookup was null and it threw after the first card → call
    `Bl_Menus.Changemenu("Confirmation")`, wait until that lookup is not null (1.3 s), then `Runprompt`; for more than
    four cards set its grid's `constraintCount` to 4 or the bottom rows fall off the screen. Verified in game (round 3,
    `15` §15).
78. **A chained map never finishes loading and the mod's timeout never fires** (RogueLab round 2, Special_Custom) →
    Map_Custom (the player's custom-map slot) fails in `Bl_Address.Load` with `InvalidKeyException`s; the timeout sat
    behind a "no `Game_Mapvars`, not in a match" return → never chain into `Special_` Gameplans, and check a load
    timeout before any in-match test. Verified in game (round 3 toured the last 15 maps with the fix).
79. **Overlapping slows crawl a foe at the game's own Speed floor, and a "root" of -0.9 never holds it** (Argo 1.3.0,
    carried from ModProjects' 40j) → raw `Speed` Multi mods stack, and enemy `Speed` has Limits 0.05..99999 (read in
    game), so two full Tremor ticks or a slow on a rooted foe left it crawling, never stopped → make a root -1 (Speed at
    its floor), cut slows so slows alone stop at 10% of a foe's speed and skip them on a held foe, and raise a
    `Speed.Value_Bounds.x` under 0 to 0 (`ArgoUtil.Speed`, `TDS_Mod/ModSource/Argo`). Seen in game in Argo 1.x; the fix
    is in Argo 2.0.
80. **One exception silences a tower or a unit for the whole match** (Argo 1.3.0, carried from ModProjects' 40k) → the
    brain's catch logged and set `enabled = false` on its first exception → skip the failing tick and stop the brain
    only after it throws on every frame for 600 frames (~10 s) (`ArgoFaults`, `TDS_Mod/ModSource/Argo`). Seen in game in
    Argo 1.x; the fix is in Argo 2.0.
81. **A stat stays at 0 after an Override mod was changed and then removed** (base-vessel dive, `17` §2.1) →
    `Editstat` adds `Value_Override_On` again on every change of an Override mod and takes it off once on removal →
    never edit an Override mod: remove it and add a new one; use 0.01, not 0, for a stun. Read from code.
82. **A Direction's `Addmod` does nothing on a fresh building or a stat that comes later** (`17` §2.1, §2.3) → stats
    are built after `Onspawn` (`Entity_Master.Start`), and a mod on a missing stat is listed but never applied, even
    when `Addstat` makes the stat later → add the upgrade to `Initupgradelistserver` in `Onspawn`, `Addmod` at
    `Loaded` or later, check `Statsserver.ContainsKey`, and re-apply when an upgrade adds the stat. Read from code.
83. **A run upgrade changes a building's panel, model or `Leveladd` stats, or pays money back on sell** (`17` §2.2) →
    the highest `Priority` ≥ 0 sets `Level` and `Mainupgrade`, and sell refunds every applied upgrade's `Costs` → give
    effect upgrades Priority −3 and empty `Costs` (`ModKitDirections.Make` does). Seen in game (RogueLab round 7,
    2026-10-12): a −3 upgrade left Level and main upgrade alone on an archer, a Sno Cannon and the Bouthouse.
84. **An entity tagged at run time is never found by `Entitiesserver[tag]`** (`17` §2.5) → the tag lists are filled
    once from the data's `Entity.Tags` → put the tag in the being's data, or keep the mod's own list. Read from code.
85. **A building upgrade lands on Boutman himself** (`17` §5.1) → the Bouthouse forwards every upgrade it holds to the
    hero (`Askforupgrades`) → never give the Bouthouse a run upgrade; skip the tag `Bouthouse` in "all buildings"
    effects. Seen in game (RogueLab round 7): on Boutman 1.5 s after the house got it.
86. **A Sno Sno cannon eats a modded entity** (`17` §4) → it takes ammo from the nearest entity tagged `Sno` or
    `Snoble` of any team and despawns a `Mob` it takes → never tag pack things `Sno` or `Snoble` (nor other
    base-vessel family tags, `17` §11.1 rule 12). Seen in game (RogueLab round 7): a foe tagged `Sno` beside a cannon
    was gone in 0.17 s. The throw on an unknown shot name read from code was not seen.
87. **A sprite faces the wrong way when it walks left** (expected from the code, `18` §1) → the game never flips a
    sprite; it switches to v1 (toward the camera, heading screen-left) or v3 (away, heading left) → draw v1 and v3 as
    the left-heading views, not as copies of v0 and v2. Read from code.
88. **A drawn elite sheet, or a foe that vanishes under Fractured** (`18` §5.5) → elite looks are one overlay material
    added over the foe's own sprite (Fractured tints near-black) → draw no elite sheets, keep the six cloned
    `Skinoverride`s, and keep the foe mid-to-light. Read from the game's assets.
89. **A Direction's params change nothing** (RogueLab round 7, `17` §2.1) → no engine code turns an upgrade's
    `Defaultparams` into stat mods; each stock AI reads the params of its own upgrades by name → the Direction's code
    calls `Addmod` itself. Seen in game: `Range_Add 0.5` on a −3 upgrade left an archer, a cannon and Boutman as
    they were.
90. **`Units +1` spawns nothing** (`17` §8) → the Hut, Keep and Garage read `Units` only in their count checks →
    after the mod, `SendMessage` the building's check (`Addressminercount`, `Addressmobcount`, `Addressdogcount`); it
    spawns or removes units to match `Units`. Seen in game (RogueLab round 7).
91. **A faster respawn set at the vessel's death does nothing** (`17` §2.4) → the core reads its `Respawn` stat in
    its own handler on the vessel's `Despawn`, which runs before a mod's handler and before `Game_Entity.Ondespawn` →
    the mod must be on the core before the death (the core is the team's, so it speeds every player). Seen in game:
    10 s plain, 5 s with the cut made before, 10 s with it made in either handler.
93. **Every machine drops to the main menu 120 s into a map load** (decompile, `19` §3) → one client that can't load
    the map (pack missing, a `Subloads` group name `Waitload` never finds) never sets `Loaded`; the host waits for it
    with no timeout, and each machine's `Game_Load` kicks itself out at 120 s → every player runs the same pack; the
    host sends back a client still not loaded 75 s into the loading screen, once the host itself has loaded
    (RogueWavesMode 0.3.0 `RWLoad`; `RecipeNet.LoadWatchdog` is the bare recipe). Read from code; compiled, not run.
    (92 is not used.)
94. **A friend who restarted the game is turned away on rejoin** (decompile, `19` §5) → the client's GUID lives in the
    save's `Metadata`, which every launch wipes, so a restart brings a new `Sessionid` and RogueWavesMode's seat check
    misses → keep the GUID in the pack's own save folder and put it back into `Metadata` before the game reads it on
    joining; the host maps a GUID it has seen to the same `Sessionid` (`Blm_Multiplayer.Getsessionid`) and the stock
    reconnect does the rest (RogueWavesMode 0.3.0 `RWIdentity`, tied to the computer so a copied save can't give two
    players one id). Test a pulled cable and a restart apart (`docs/mode/REJOIN.md` O1-O3b). Read from code; compiled, not
    run.
95. **Effects show solo but not in a busy online fight** (decompile, `19` §4) → `Projectilecheck`, `Vfxoneshotcheck`,
    `Vfxdebuffcheck` and `Vfxemittercheck` drop the send while 50 are in flight; the queue drains 10 per physics step
    solo and 4 online → what a player must see goes through `Vis_ClientRpc`, a skin, a stat or a networked being, or
    calls the public `Vfxdebuff_ClientRpc` / `Vfxoneshot_ClientRpc` itself and adds one to `Clientsendstack`, sending a
    look again only when the one showing would end sooner than the new one (Argo 2.0.1 `ArgoUtil.Debuff`, `MustSee`).
    Read from code; compiled, not run.
96. **`KeyNotFoundException` from the sound player, or a sound variant that never plays** (decompile, `21` §2.1) → the
    table is built once, before mods load, and never takes a new key; an unknown, empty or null key throws wherever the
    game plays it; `Random.Range(0, Count - 1)` never picks the last variant → check every key the pack writes at boot
    (`RecipeSound.Exists`), play own clips on own sources, replace a `Soundpair.val` list instead of appending. Read
    from code.
97. **The vessel's `<Name>_Localization.csv` changes nothing in game** (decompile, `21` §3.1) → the game reads only its
    own `Resources/Localization` and its dev file → keep the CSV as a translator's sheet and write the rows into
    `Bla_Localization.translations` after each language load (`RecipeText`). Read from code.
98. **The next launch loads an empty profile that never saves** (decompile, `23` §2) → `Initialize` rebuilds the
    save's dictionaries with `Add`, so a duplicate or null folder or key name (from editing `Folders`, `Data` or
    `Datalist` directly) throws, and the load isn't caught → only `Readdata`, `Adddata` with its typed third argument,
    `Removedata` and `Removefolder`. Read from code.
99. **A slot switch writes one slot's campaign into the other** (decompile, `23` §6) → the pack's statics outlive
    `Bla_Data.Playerdata`, which a switch or a clear of the current slot replaces (nothing else does after launch) →
    remember the `Playerdata` object the state came from, check it before every write, and drop the state on
    `Onslotchanged` and `Onslotremoved`, in a handler that never throws (RogueWavesMode 0.3.0 `RWCampaign.Live`;
    `RecipeSave.Install`). Read from code; compiled, not run.
100. **One part's compile error silently drops the parts after it** (decompile, `23` §4.3) → the loader activates a
     bundle's `Default` prefabs in one loop, and a compile failure leaves the loop → compile each part alone with
     cloud-check before a build, and ship `Default` prefabs only (a broken keyword prefab stops a game scene for every
     mod). Read from code.
101. **A map's cores or buildings vanish at load** (decompile, `22` §2) → `Despawnanonblock` removes every being
     without a grid node, and beings under the map's `Entities` child get none unless their block adds one → put them
     under `Object`, one tile above the ground, on a tile centre (`Maxbound` is the grid's minimum corner). Read from
     code.
102. **"Slices 6, 7, 14, 15 ... lie outside the texture" on a sheet 192 wide** (Yossele round 1, 2026-10-12) → the
     builder sliced against `tex.width` read right after the first import, which scales a sheet that isn't a power of
     two up to 256 → read the PNG's own size from its header (`YosseleBuilder.PngSize`). Argo's and the template's
     builders, whose main sheets are 192 wide too (6 frames of 32), took the same `PngSize` on 2026-10-15.
103. **"FAILED Export Mod finished but ... .info wasn't written"** (Yossele round 1) → the vessel's `Export()` refused
     because other `<Name>Mod` folders were still in Assets; the Build Queue doesn't park them → `Export()` calls its own
     park step first (`ParkOthers()`/`Park()`), as Argo, Yossele, RogueLab, RogueWavesMode and (since 2026-10-15) the template do.
104. **A handoff changed an Editor script, but the Build Queue built with the old one** (Yossele round 8, 2026-10-12)
     → `Assets/Refresh` reloaded the domain without compiling ("compile time=0 ms"), since `Apply-Handoff` keeps the
     zip's file times, older than the last compile; the editor assembly only recompiled later, during the export's
     park and restore → when a round changes `Assets/<Name>Mod/Editor`, check that `Library/ScriptAssemblies/
     Assembly-CSharp-Editor.dll` is newer than the apply (or reimport the changed scripts) before the Build Queue, and
     check a generated asset the change should alter.
105. **A foe's shot that a building "shot down" still explodes on the host, or keeps flying on a client** (Yossele
     D-MDS, decompile 2026-10-23) → projectiles aren't networked entities: the host keeps pooled ones per name in
     `Game_Entity.Projectiles` and only its copy deals damage; `Kill(true)` runs the death handlers, where a bomb's
     explosion lives (`Projectile_Ai_Explosion`) → put the host's copy out with `Kill(false)` and send a Vis so each
     client puts out its look-only copy (`YosseleDandelionBrain.Threats`, `YosseleClient`). Read from code; compiled,
     not run.
106. **"When a node is mined out" never fires, and a node at 0 `Healthresource` doesn't pay yet** (Yossele's aura
     fruit, 2026-10-23) → stock ore nodes never run out and pay only once every health stat is at 0
     (`Helper_Damagetools.Addressdamage`): `Resource_Orange` also has `Health` 1/1, so the hit that takes
     `Healthresource` to 0 pays nothing and the next hit, which takes `Health`, pays and refills both
     (`Entity_Ai_Resource_Mine`; kit `16` §4) → to act on a payout your own strike caused, read the node's
     `Healthresource` before and after the strike (a refill means it paid); to set a node up to pay on the next hit,
     put `Healthresource` at 0, not 1. **Verified** on `Resource_Orange` (Yossele round 9e: `Health 1/1`, the third
     strike paid); other node kinds not checked.
111. **A foe's `Health` and its maximum both read 0 right after it spawns** (Yossele rounds 9 and 9d) → its stats fill
     in a little later, so a loss measured from a reading taken at the spawn is wrong (round 9 got -58) → read health
     live once its maximum is above 0, and for a foe one hit can fell, take who felled it from your despawn hook
     (`master.Lasthit` while `killed`): its object compares equal to null afterwards. **Verified** in game.
107. **Stripping a debuff a foe's hit puts on your building seems to need a timer after the hit** → it doesn't:
     `Helper_Entitytools.Interacthurt` runs the attacker's `Interact` before the target's `Oninteract` chain, so the
     debuff (the Bulwark's stun: Override modifiers and the Disabled look) is already on when a handler at the front of
     the chain sees the hit → strip it there, and sweep ten times a second for debuffs that come without a hit
     (Yossele's Monument, `YosseleHooks`). Read from code; compiled, not run.
108. **Code that needs the map's Tide count finds no field for it** → the map's mode holds it:
     `Game_Mapvars.Instance.Mode.Enemies.Totalwaves` (as `Entity_Ai_Artifact_Coin` reads it); guard each step against
     null (`YosseleServer.TidesOnMap`). Read from code; compiled, not run.
109. **CS7036 calling `Helper_Damagetools.Projectile` in cloud-check, though the game's code compiles without the last
     argument** → the stubs drop default parameter values, so `Interactiontype` (default `"Hurt"`) must be passed
     explicitly; the game's Roslyn accepts it either way.
110. **"Could not retrieve Vfx/Debuff_Weakened The GROUP exists, but item does not"** (Yossele round 9, 2026-10-23,
     solo on 3B_Thehunt) → `Debuff_Weakened` is only named by the Fiewall's code and isn't in the Vfx group in a normal
     game, though kit `17` §10 listed it → give the look your own effect (`Debuff_Yossele_Marked`), or check a stock
     debuff name with the self-test's `DebuffReport` before relying on it.
112. **A foe killed in the frame it spawned leaves nothing your despawn hook keys on, and an area search two frames
     later misses it** (Spool round 1, 2026-10-26; #105-#111 are Yossele's, PR #41) → `Entity_Master.Start` adds a
     being's tags (`Taglistserver`) and stats a frame after `Game_Entity` spawns it, so a hook that checks the "Enemy"
     tag skips a foe despawned in its spawn frame (no Thread dropped), and `Helper_Entityquerytools.Entityarea` (a
     `Physics.OverlapSphere`) didn't find a foe that had just got its tags → in a self-test, wait until a spawned foe is
     tagged and has `Health` before killing or reading it (#111 for its health), and about 0.3 s more before an area
     search around it. Real Tide foes live long enough for neither to matter. **Verified** in game (the area search: missed
     at two frames, found after 0.3 s; not timed more finely).
113. **Selecting your unit throws `KeyNotFoundException: The given key 'Quickaction2' was not present in the
     dictionary` in `Game_Selection.Addcommand`, and its panel never draws** (Spool play test, 2026-10-26: no buttons, no
     upgrades, the previous selection's buttons stay) → `Addcommand` looks each name in a command's `Keybind` up in
     `Blockage.Bl_Inputs.Actions`, which holds only the game's input actions (`Quickaction` and `1` to `8`) → name only
     those (Spool's RETURN takes `4`, MEND `3`), and have the self-test check every key against `Bl_Inputs.Actions`
     (Spool's does). **Seen** in game; the fix is checked by Spool's play test.
114. **"Could not retrieve Vfx/Debuff_Stun The GROUP exists, but item does not" on one map, though the self-test saw
     the look attach on another** (Yossele play test, 2026-10-26) → stock effects load with groups, and which groups a
     match loads depends on its map and vessels: `Debuff_Stun` is in the Vfx group on 3B_Thehunt (the self-test map, so
     round 9e's `DebuffReport` passed) but not on 3B_Woozy, where each Hallucinogen stun logged the line and drew nothing
     → a `DebuffReport` proves a stock look on its own map only (this amends #110): give the look the mod's own effect
     (Yossele's `Debuff_Yossele_Daze`), or name only a stock look from a group the mod loads itself (its template
     vessel's or its `ExtraGroups`). **Verified** in game (play test 2026-10-26).
115. **A second rank or stack of a Direction or Cardinal does nothing** (RogueLab round 8, 2026-10-29) →
     `Helper_Entitytools.Upgrade` returns at once when an upgrade of that name is already on the entity: the same asset
     applied twice left an archer at ×0.7 with one script instance → give each rank or stack its own upgrade asset with
     its own name; two assets with different names on one stock script stack (two Barrel_Boost −0.3 read ×0.4: the
     `Multi` bucket sums, kit `17` §2.1). **Verified** in game.
116. **Removing one of two upgrades that list the same stock script leaves its effect running** (round 8) →
     `Removeupgrade` runs the script's `Downgrade`, then destroys only the AIs whose type no upgrade left on the entity
     names, so both instances live on: a wall still lost 1 + 2 a prick after thorn upgrade A was removed, and 0 only
     once B went too → remove upgrades that share a stock script together, or give the one that must come off alone a
     script nothing else on the entity names. **Verified** in game.
117. **`KeyNotFoundException: The given key 'Health_Pmulti' was not present`, or a `NullReferenceException`, when a pack
     upgrade lists a stock script** (round 8) → a stock script reads every param it knows by key, not just the ones the
     pack sets (Barrel_Boost reads five), and nine Cardinal scripts (Checklist, Coin, Computer, Greatspear, Moneybag,
     Moneybagold, Mooncore, Onetimebuild, Samedayshipping) read `Artifactobj`, which stays null unless `Upgrade`'s fourth
     argument passes the Cardinal → copy the stock upgrade's `Defaultparams` keys whole (0 for the unused ones), and pass
     the `Artifactobject` (a mid-map grant on Vintage then ran clean and lengthened the break by its 40 s). **Verified**
     in game for Barrel_Boost, Coin and Vintage.
118. **The stock "everything a vessel builds" Cardinal reaches every player's buildings, and keeps reaching new ones
     after it is removed** (round 8) → `Entity_Ai_Artifact_Teamapply` checks only that a new thing's parent is tagged
     `Hero`: a building owned by another player got its gift, and after the upgrade was removed a new archer still got
     it (its spawn handler is never taken off) → for one player's things use an owner-filtered `Onspawn` hook that adds
     the upgrade to `Initupgradelistserver` (round 8: only that player's towers got it, a rebuilt one again); keep
     Teamapply for Team Cardinals that last the whole map. **Verified** in game (owner −1 stood in for a teammate).
119. **A timed mod re-added every second reads double half the time** (round 8) → `Addmod(..., Duration)` takes each
     mod off after its own timer, so a 1.5 s `Add 1` added every second read `[1 1 2 1 2 1 2 1]` → keep each source's
     `Link_Modifier` and remove it before adding the next: two sources then read 2, one 1, and 0 about 1.5 s after the
     last (Uloe's light, fit plan §10). **Verified** in game.
120. **Melting's drained stats never come back** (round 8) → `Entity_Ai_Effect_Enemy_Weaken` has no `Downgrade`: a 3 s
     −0.1 Damage drain left an archer at 3.18 of 4 after the effect was gone → never borrow it for a debuff that must
     wear off (Aging); write that on `Addmod` with a `Duration`. **Verified** in game.

121. **The Tide table's row is `Entry - 1`** (Rogue Waves mode, 2026-11-03) → `Mode_Normal.Run` starts `Entry` at 1, reads
     `Wave[Entry - 1]` on every pass and adds 1 when a row's clock runs out: the opening break is row 0, Tide N is row
     2N - 1, the break after it row 2N. Reading `table[Entry]` looks right in a self-test, because every event moves by one
     row together: the shop then opens as a Tide starts (a run of 54 passes had it so, until Beat the Clock couldn't be
     won) → `ModKitTides.EntryIsBreak`. To replay a row from inside the break before it, set `Entry` to that row's index:
     the clock's end adds the 1. Writing `Entry` mid-Tide ends the row's spawning. **Verified** in game.
122. **A Tide's clock starts when its foes have all been sent, and runs out whatever happens** (2026-11-03) → `Time_Wave` is
     written at the row's start, not counted during the spawning, and after the field is clear a second loop counts the
     rest: a break comes at the clock's end, not at the last kill, so "the clock left at the break" is always about 0 →
     to read "cleared early", watch for no foes with a clock that has started (`Time_Wave` below the row's `Wavetime`).
     **Verified** in game (Beat the Clock).
123. **A dead `Tower_Halberdnest` stops the Tides for good** (2026-11-03) → `Entity_Ai_Tower_Halberdnest.Spawn` hooks
     `Game_Mapvars.Wave.OnValueChanged` and never unhooks; with the nest despawned, its next growth throws in
     `Vfxoneshot_ClientRpc` inside the counter's write, inside `Mode_Normal.Run`, an `async void` nothing restarts: no more
     Tides, no error on screen → take the delegate off the list once the nest is dead: walk `GetInvocationList()`, test
     `d.Target as Entity_Ai_Tower_Halberdnest`, `Delegate.Remove` (`RWWeather.DropDeadNests`). It needs no reflection, which
     the game blocks. Any handler that throws in `Wave`'s write does the same. **Verified** in game.
124. **A world's camera looks along its node's `Cameraposition` forward** (2026-11-03) → `Macro_Main.Camdir` holds `-forward`,
     which is the offset from the target, not the view: a transform turned to look back at the node shows the sky, and every
     probe still passes. The stock pose is about 20 units from the boat, 28-40 degrees down. The boat floats on its world's
     water top (the home world's water top is y -0.98 in its own coordinates). A stock node's own art is at the node's local
     origin, but about a fifth of stock nodes are only a marker post (1.3 wide), their map drawn into the world's landscape,
     and `Altgameplan` nodes often hold the real island. `Homevolume` is a depth of field for the menu's view, not a grade:
     it blurs a chart. **Verified** in game.
125. **Choice dialog cards draw `Display_Image` at its own pixels-per-unit** (2026-11-03) → the pack's icons at 20 come out the
     right size; a map's `Mapentryobject.Display_Image` comes out a speck: make a `Sprite.Create` of the same texture and rect
     at 20. **Verified** in game.
126. **The main menu's column already reaches the screen's foot** (2026-11-03) → a Play-sized clone under Play pushes More and
     Exit off the screen at 720p: stand it one step right of Play, move what stood beside Play (`Hero`) and `Buttontip` one
     step on, and hide the clone's `Linev`. In the clone only the child `Image` is the picture; `Border`, `Lineh`, `Linev` and the
     root's `Wave Box` are the tile. An unattended menu goes idle and hides its buttons (`Mainmenu_Main.Idlethreshold`).
     **Verified** in game.
127. **The map's fly-in hides the HUD for about 10 s, and the opening break's row is up as the map loads** (2026-11-03) → the
     opening break fires before the Basin and vessels exist; the vessels' own start runs on the Tide clock, so holding
     `Time_Wave` before they stand stops them spawning; a dialog opened in the fly-in is invisible → wait for vessels and for
     `Time_Wave <= table[0].Wavetime - 14`. **Verified** in game.
128. **`Tds_Clientinfo.Mods` is only written at a scene change after the player object spawned** (2026-11-03) → it reads false on
     a player's first boat, so warning about it warns about the host himself → every machine with the pack writes its own
     (owner-writable), and the host waits 8 s before warning about a friend. **Verified** in game.
129. **`Mode_Normal` rewrites `Display` from its own copy, and the end screen reads the title once** (2026-11-03) → hold
     `Display_Endgame` every tick, and write the shown `Win`/`Lose` `Title` text too: one run in two showed the forced win's
     "BASIN BREACHED" otherwise. **Verified** in game.
130. **A tower has no `Upgrade` command as it spawns** (2026-11-03) → an owner-hook on `Onspawn` that scales the command's
     `Goalvalue` finds nothing; keep it up on a slow tick. **Verified** in game (Cheap Refits).
131. **The pause menu's `Exit` is the map's, `Leavegame` the boat's** (2026-11-03) → the menu shows them by scene: on the boat
     the host's way home is `Leavegame` ("RETURN HOME", card `Exit`, `Leaveserver`, main menu). `Exit` there opens "EXIT MAP"
     and quits nothing. **Verified** in game.
132. **`Artifact_Rock` is Bottom Rock: all aura pay drops to 1%** (2026-11-03) → `Resourcegainmultiplier` 1 to 0.01 on buying
     it; any `Give` is scaled by it. A shop's list of stock Cardinals has to leave it out by that id. **Verified** in game.
133. **A self-test step that does what a player can't leaves a state a player never meets** (2026-11-03) → unrelated probes
     after a whole campaign in one boat scene failed (a warp home and a warp out in one scene left the boat standing, its
     `Loaded` state unknown) and passed from a fresh boat: go back to the main menu between test blocks. Unity also runs a
     yielded coroutine by itself, so an exception in one never reaches its caller's `try`: step inner coroutines by hand.
134. **The queue can stall on a Unity modal** (2026-11-03) → "Moving file failed" during an export waits for a click with no
     log line, and the queue's next launch tests the old export: run a watcher during exports that presses Try Again
     (`EnumWindows`, `BM_CLICK`), and read the export's time from `queue_status.txt` before launching.
135. **`Macro_Main.Playgame` with nothing selected crashes the game** (2026-11-03) → a native crash in `Boatlaunch`, from a play harness whose map wasn't on the world map → check the node first. **Verified** in game.
136. **Three walls across a lane don't stop foes** (2026-11-03) → three Pin Cushions on the lane's own tile and either side of it, on Purlest and on Woozy, or three Stonehenges on Purlest: the foes planned round them and the walls took no hits, while a smart player's model assumed they'd stand at the line (the repository's `docs/reports/BALANCE_ROUND_2026-11-03.md` M5). A build sent in the moment after Spool's Stash stands is refused too: wait for `Home`. **Verified** in game.
