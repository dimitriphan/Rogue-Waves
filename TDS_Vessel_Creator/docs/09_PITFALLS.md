# 09 · Pitfalls: every error met so far, and its fix

Each entry: **symptom** → cause → fix (where the fix lives in the template). All of these happened while building Smocky
(the first test clone), Sprigg 1.0-1.4.1, Argo 1.0, Mummer 1.0, the kit 1.4 boat work, Argo 2.0 and RogueLab. Add new
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
    nothing (not observed, avoided) → before removing a vessel mod, select another vessel in game.

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
90. **`Units +1` spawns nothing** (`17` §12.4) → the Hut, Keep and Garage read `Units` only in their count checks →
    after the mod, `SendMessage` the building's check (`Addressminercount`, `Addressmobcount`, `Addressdogcount`); it
    spawns or removes units to match `Units`. Seen in game (RogueLab round 7).
91. **A faster respawn set at the vessel's death does nothing** (`17` §12.6) → the core reads its `Respawn` stat in
    its own handler on the vessel's `Despawn`, which runs before a mod's handler and before `Game_Entity.Ondespawn` →
    the mod must be on the core before the death (the core is the team's, so it speeds every player). Seen in game:
    10 s plain, 5 s with the cut made before, 10 s with it made in either handler.
