# D. Command buttons, visual scripts, VFX helpers

Source: the game's code as decompiled in this thread (kit `docs/13` §11 recipe); the decompiled files are never
committed. Line numbers are file lines; `//IL_` lines ignored. 96 entries: 24 `Entity_Command_*`, 60 `Entity_Vis_*`,
`Helper_Entityvis`, `Helper_Vfxtools`, 10 `Vfx_*`.

## How these are wired (applies to every entry below)

- **Commands.** `Game_Selection.Addcommand` loads the `Commandobject` by the `Link_Commandref.Commandid` string
  (`Bl_Address.Getload(Commandid)`), skips it unless `Controllable`, and `DeepCopy()`s (a `MemberwiseClone`: shallow,
  inspector fields kept) every `Commandfuncts` entry, then sets `Master` (selected entity), `Select`, `Origin`
  (the `Commandobject`), `Link` (Game_Selection.cs:266-289). A button press or keybind sets `Activecommand`,
  `Commandtime`, fires `Commandchange`, then calls `Activate(Keybind)` on each copy. `Render(button)` refreshes labels.
- **Host path.** `Entity_Master_Remote.Command_ServerRpc(link, json)` runs on the host only if the sender's session is
  the entity's `Owner` or in `Subowner`, `Commandlistserver` holds `link` with a `Ref` and `Controllable`
  (Entity_Master_Remote.cs:69-72); then `Helper_Entitytools.Updatecommand(master, link, json, Server:false)` sets
  `State` to `link` if the command has `Statechange`, raises `Master.Command(link, json, false)` for every AI, and copies
  the host `Link_Command` `Goalposition/Goalvalue/Goalstring/Controllable` back into the synced `Commandlist`
  (Helper_Entitytools.cs:280-347). So the json is parsed by whichever AI's `Command()` matches the link name.
- **A mod's `Commandobject` can list any stock `Entity_Command_*`** (instances by `[SerializeReference]`,
  Commandobject.cs:37-38); the client side always works, the effect depends on a host AI answering that link.
- **Vis scripts.** `Entity_Master_Client.Generatesetup` does `AddComponent(Type.GetType(entry.name))` for each
  `Beingobject.Entity.Visualscripts` entry, sets `Master/Client/Remote/Radio`, and calls `Spawn()` at once, in list
  order (Entity_Master_Client.cs:89-97). A visual entry's `Additionalvisualscripts` are added the same way when that
  skin/upgrade visual is applied, skipped if a component of that type already exists, keyed by the main upgrade name
  (`Visscriptspersistent`) or time, and destroyed when that upgrade leaves `Upgradelist` (Entity_Master_Client.cs:574-612).
- **Callbacks.** `Entity_Vis_Base.Start` subscribes `State(old,new)`, `Interact`, `Vis(type,pos,number)`, `Despawn`;
  `OnDestroy` unsubscribes them and calls `Destroyed()` (Entity_Vis_Base.cs:58-88). `Vischanged(Currentvis)` and
  `Hitchanged(Currentvishit)` fire when the visual/hit prefab is rebuilt (Entity_Master_Client.cs:370-377, 679-686);
  `Selected/Deselected` from Game_Selection.cs:454-460, 520-526. Vis events come from the host's
  `Radio.Vis_ClientRpc(type, position, number)` (Entity_Master_Radio.cs:420-459).
- **Stock Vis senders.** `Hurt`/`Heal` (Number = Health `Value/Value_Calc`, Helper_Damagetools.cs:61) from
  `Entity_Ai_Mob_General`, `Entity_Ai_Tower_General`, `Entity_Ai_Trap_General` and others; `Attack` from
  `Entity_Ai_Mob_Melee`, Smackybomber, Fiesilo, several enemies; `Shoot` from `Entity_Ai_Mob_Ranged` and ~25 shooters
  (Position = aim vector or own position); `Position` from Tpgate/connector/boost AIs.
- **Order rule.** Every script that does `Base = GetComponent<Entity_Vis_Animator_Base>()` in `Spawn` needs an
  animator (any `Entity_Vis_Animator_*` or `Entity_Vis_Mob_Bird`/`Mob_Boutmandad`) listed **before** it, or `Base`
  stays null and its first `Vis`/`FixedUpdate` throws.
- **Missing data.** `Helper_Entityvis.Getstatlocal` returns `Value 0` (name "None") for a missing stat and
  `Getcommandlocal` a default struct for a missing command, so most scripts degrade to "off" rather than throw.
- Stats/commands subscriptions (`Stats.OnListChanged += ...`) made in `Spawn` and removed only in `Despawn` leak if the
  script is destroyed by an upgrade change (Additionalvis) instead of a despawn.

---

## Commands (`Entity_Command_*`)

### `Entity_Command_Base` (51 lines)
- **Does:** Base class. `Activate` only clears `Select.Activecommand`; `Button`/`Render` empty; `Getnewlink` refreshes
  `Link` from `Master.Commandlist` by `Name`; `DeepCopy` clones and nulls the runtime refs.
- **Sends:** nothing.
- **Reuse:** `no` as a behaviour (a button that does nothing); it is the parent mod subclasses extend.
- **Cite:** Entity_Command_Base.cs:17-20, 30-40, 42-50.

### `Entity_Command_Simple` (13 lines)
- **Does:** One-click button: on `Activate` sends the command and closes it.
- **Sends:** `Command_ServerRpc(Link.Name, "")`.
- **Reads:** `Link.Name` only. **Draws:** nothing.
- **Hooks:** `Activate`.
- **Reuse:** `data-only`: any mod command whose host AI acts on its own link name with empty json (mode cycle, trigger).
- **Cite:** Entity_Command_Simple.cs:6-12.

### `Entity_Command_Itemselect_Base` (108 lines)
- **Does:** "Click an entity" picker. Plays UI sound `Commandstart`, waits for the button release, then each frame puts
  `Clone2` on the own visual and `Clone1` on the hovered entity's visual (`Game_Client_Mouseobject.Master`), calls
  `Running()`, and on a click calls `Send()`; `Finish()` after. With `Keybind && Keybindable` it calls `Send()` at once.
- **Sends:** nothing itself (`Send` empty).
- **Draws:** Vfx `Moveborder` (hovered) and `Movefromborder` (self), via overridable `Visobjs()`.
- **Hooks:** `Activate`, virtual `Send/Visobjs/Running/Finish`.
- **Reuse:** `no` alone (Send is empty); the base of Sell/Reroute/Fishfollow and of mod pickers (kit `docs/05` §7).
- **Cite:** Entity_Command_Itemselect_Base.cs:23-27, 50-59, 61-106.

### `Entity_Command_Move` (165 lines)
- **Does:** "Click a ground tile" order. After the button release, shows `Movefromborder` on the own visual and
  `Moveborder` on the hovered surface tile; on click sends. Keybind sends at once to the tile under the mouse.
  Click feedback: Vfx `Minebordercommand` if a `Resource`-tagged entity is within 0.45 of the tile, else
  `Movebordercommand`. `Activatecheck()` (virtual, true) gates it; a refusal plays `Notenough` and shakes the button's
  `Notenough` child image.
- **Sends:** `Command_ServerRpc(Link.Name, {"To":{x,y,z}})` (`Entity_Ai_Mob_Movement.Movementdata`, tile position).
  Stock handler: `Entity_Ai_Mob_Movement.Command` for link `Move` (snaps to the nearest `Moverquery` entity within 0.5).
- **Reads:** `Link.Name`; `Game_Client_Mouseobject.Surfacemaster`. **Draws:** the two borders above.
- **Hard-coded:** Vfx `Moveborder`, `Movefromborder`, `Minebordercommand`, `Movebordercommand`; tag `Resource`;
  sounds `Commandstart`, `Notenough`.
- **Reuse:** `data + stock asset`: any mod unit with `Entity_Ai_Mob_Movement` and a `Move` command gets a move order;
  with another link name a mod AI can parse the same `{"To"}` json (ground-target ability).
- **Cite:** Entity_Command_Move.cs:32-70, 72-76, 83-123, 124-163; Entity_Ai_Mob_Movement.cs:45-49.

### `Entity_Command_Movelinear` (141 lines)
- **Does:** "Pick a direction" order: arrow `Movefromborderdirectional` on the own visual, rotated toward the mouse
  and snapped to 90°; on click sends.
- **Sends:** `Command_ServerRpc(Link.Name, {"To":{x,y,z}})` where `To` is a **unit direction** (not a position);
  plays `Movebordercommandlinear` at the start point. Stock handler: `Entity_Ai_Mob_Movementlinear` link
  `Movelinear` (only `!Server`, with a cooldown).
- **Reads:** `Link.Name`; `Surfacemaster.Extrapositiondata.Trueupdirection`.
- **Hard-coded:** Vfx `Movefromborderdirectional`, `Movebordercommandlinear`.
- **Reuse:** `data + stock asset` (with `Entity_Ai_Mob_Movementlinear`).
- **Quirks:** keybind path sends the previous `Rundir`/`Runstart` (zero on first use); `Send` does not null-check the
  own `Entity_Master_Client`.
- **Cite:** Entity_Command_Movelinear.cs:30-51, 53-56, 73-77, 99-139; Entity_Ai_Mob_Movementlinear.cs:22-29.

### `Entity_Command_Build` (428 lines)
- **Does:** Build-placement mode for the tower named by `Link.Goalstring`. Checks cost (`Item.Entity.Costs` x
  `Link.Goalvalue`) first; then shows a ghost (model `Visprefab` with material `Selection Overlay`, or sprite
  `Spriteobject.Preview` on Vfx `Mobbase` tinted green), `Buildborder`/`Nobuildborder` on the hovered tile,
  `Buildfromborder` on the builder, owner highlight borders, a range ring and a forward arrow; `Rotate` input turns by
  90° if `Orientable`. Virtual `Placementstart/Placementhover/Placementend` let subclasses gate placement.
- **Sends:** `Command_ServerRpc("Build", {"Tower":Goalstring,"Position":{x,y,z},"Orientation":int,"Owner":session})`
  (`Entity_Ai_Hero_Buildsell.Builddata`), **always to link `Build`**. Handlers: `Entity_Ai_Hero_Buildsell`,
  `Entity_Ai_Tower_Fiewall_Build`.
- **Reads:** `Link.Goalstring` (Mob name), `Link.Goalvalue` (cost multiplier: 0 shows `FREE`, negative shows `+N`);
  the tower's `Range`/`Engagerange` default stats, hit prefab children `Detect` and `Forward`, `Block.Placementconditions`.
- **Draws:** Vfx `Buildborder`, `Nobuildborder`, `Buildfromborder`, `Buildborderforward`, `Mobbase`; Ui
  `Selectionrangecontainer` scaled to `Range*2` (if hit prefab has `Detect`) else `Engagerange*2`; `Helper_Entityvis.Highlight`.
- **Hooks:** `Render` subscribes local player `Tds_Clientinfo.Resources.OnListChanged` to recolour the
  `Resources/<name>/Am/Title` cost labels.
- **Reuse:** `data + stock asset`: a mod build button for any Beingobject, if the holder has a `Build` command and a
  stock build AI.
- **Quirks:** base `Placementhover` is always true and the `Obstruct` overlap loop discards its result, so the client
  never blocks a spot (host decides); `Orientation` is `static`, shared by every build button; after an unplaceable
  hover the ring re-grows to `Range*2` even when it was an `Engagerange` ring; needs `Defaulthit.Default.Entityprefab`
  (NRE otherwise); calls `Tds_Tutorialprompts.Rotatetutorial()` for orientable towers.
- **Cite:** Entity_Command_Build.cs:21, 37-111, 117-120, 126-139, 147-148, 186-265, 280-294, 327-345, 368-371.

### `Entity_Command_Tower_Fiewall_Build` (32 lines)
- **Does:** `Entity_Command_Build` whose `Placementhover` is true only when an entity tagged `Fiewall` (client
  `Taglist`) is within 0.8 of the hovered tile on layer `Entity`; else the ghost turns red and clicks are refused.
- **Sends:** as Build (`"Build"`, Builddata); handler `Entity_Ai_Tower_Fiewall_Build`.
- **Hard-coded:** tag `Fiewall`.
- **Reuse:** `vessel-bound` (needs Fie's `Fiewall` tag; a mod entity tagged `Fiewall` would also trigger Fie code).
- **Cite:** Entity_Command_Tower_Fiewall_Build.cs:8-31.

### `Entity_Command_Sell` (238 lines)
- **Does:** Picker (Itemselect) that previews the refund of the hovered own `Sellable` entity: base `Costs` x health
  ratio (if `Sellhealthfactor`; Health+Healthover+Healthresource+Healtharmor `Value/Value_Calc`) x seller `Sellrate`
  stat x `Entitydata.Sellvalue`, plus every upgrade's costs x `Upgradesellvalue`; shows "SELL ▪ name" or "CANNOT SELL".
- **Sends:** `Command_ServerRpc("Sell", {"Tower":<GameObject name>,"Position":{x,y,z},"Owner":session})`
  (`Selldata`), always to link `Sell`; handler `Entity_Ai_Hero_Buildsell`.
- **Reads:** hovered entity stats/entitydata; own `Sellrate` stat. **Draws:** Vfx `Sellborder`/`Sellfromborder`, Ui
  `Ui_Command_Sell` in menu `Main/Command` with `Resourcecontainer` rows, owner highlights (multiplayer only).
- **Reuse:** `data + stock asset` (holder needs a `Sell` command and `Entity_Ai_Hero_Buildsell`).
- **Quirks:** the "Percent" label shows the upgrade rate (`Upgradesellvalue`), not the base `Sellvalue` rate.
- **Cite:** Entity_Command_Sell.cs:24-37, 39-67, 99-185.

### `Entity_Command_Upgrade` (183 lines)
- **Does:** Buys upgrade number `Link.Goalvalue` from the client's current `Mainupgrade.Upgrades` (or
  `Beingobject.Entity.Upgrades` if none), after a client cost check scaled by the `Upgrade` command's `Goalvalue`.
  `Render` colours the cost labels live.
- **Sends:** `Command_ServerRpc("Upgrade", {"Upgrade":<Objectname>,"Upgrader":int session})`, always link `Upgrade`;
  handler `Entity_Ai_General_Upgrade`.
- **Reads:** `Link.Goalvalue` (index); `Upgrade` command `Goalvalue` (price scale). **Draws:** `Notenough` shake.
- **Reuse:** `data + stock asset` (holder needs an `Upgrade` command and `Entity_Ai_General_Upgrade`).
- **Cite:** Entity_Command_Upgrade.cs:15-122, 134-166; Entity_Ai_General_Upgrade.cs:39-46.

### `Entity_Command_Selectother` (60 lines)
- **Does:** One-click "select my X": loads Beingobject `Othername` (inspector field) from group `Mob`, selects the
  first entity whose GameObject name equals its `Entity.Name` with the same team and owner; else `Notenough`.
- **Sends:** nothing (client selection only). **Reads:** `Othername`, team/owner.
- **Reuse:** `data-only` (set `Othername` in the mod's Commandobject).
- **Cite:** Entity_Command_Selectother.cs:12, 29-45, 47-57.

### `Entity_Command_Artifact_Blurryphoto_Select` (61 lines)
- **Does:** One-click "next copy": selects another entity with the same GameObject name as the selected one, same team
  and owner, that is not currently selected (cycles copies); else `Notenough`.
- **Sends:** nothing. **Reads:** `Master.name`.
- **Reuse:** `data-only` (works for any building type).
- **Quirks:** field `Othername` is unused; `Debug.Log` on every press.
- **Cite:** Entity_Command_Artifact_Blurryphoto_Select.cs:11, 31-47.

### `Entity_Command_Artifact_Pig_Launch` (87 lines)
- **Does:** Move-style ground picker gated client-side by 25 `Redaura`, 25 `Blueaura`, 25 `Yellowaura`; preview Vfx
  `World_Artifact_Pig_Command`; button `Desc` text "Throw 25 Aura of each type".
- **Sends:** `Command_ServerRpc(Link.Name, {"Tower":null,"Position":{x,y,z},"Orientation":0,"Owner":session})`;
  stock handler `Entity_Ai_Effect_Artifact_Pig` link `Coinbag` (spends `Upgradeobj.Defaultparams["Amount"]`).
- **Hooks:** `Render` subscribes `Game_Mapvars.Wave.OnValueChanged` (removed only when the button is gone).
- **Reuse:** `vessel-bound`-ish: tied to the three aura resources; the text and the 25 are hard-coded.
- **Quirks:** client gate is a fixed 25 while the host spends `Amount`; refuses while the camera rotates.
- **Cite:** Entity_Command_Artifact_Pig_Launch.cs:12-24, 26-50, 52-80, 82-86; Entity_Ai_Effect_Artifact_Pig.cs:31-40.

### `Entity_Command_Mob_Boutmanfish_Relocate` (28 lines)
- **Does:** Move-style ground picker (`Moveborder`/`Movefromborder`), click plays `Movebordercommand`.
- **Sends:** `Command_ServerRpc(Link.Name, {"Position":{x,y,z},"Owner":-1})`
  (`Entity_Ai_Mob_Boutmanfish.Relocatedata`); stock handler link `Relocate` in `Entity_Ai_Mob_Boutmanfish`.
- **Reuse:** `data-only` client side (generic "pick a tile" sending `{"Position"}`); the stock handler is Boutman's fish.
- **Quirks:** refuses while the camera rotates; `Owner` left at -1.
- **Cite:** Entity_Command_Mob_Boutmanfish_Relocate.cs:9-21; Entity_Ai_Mob_Boutmanfish.cs:225.

### `Entity_Command_Tower_Fiekeep_Command` (71 lines)
- **Does:** Move-style picker for a rally point. `Render` (while the building is selected) draws Ui `Knightconnection`
  (named `Rallyspot` under the entity's `Vis`) whose `Line` VisualEffect `Goal` is the `Rally` command's
  `Goalobject` position; removed on selection change.
- **Sends:** `Command_ServerRpc(Link.Name, {"Position":{x,y,z},"Owner":-1})` (`Entity_Ai_Tower_Fiekeep.Relocatedata`);
  handler `Entity_Ai_Tower_Fiekeep` link `Rally`.
- **Reads:** command `Rally` `Goalobject`. **Hard-coded:** Ui `Knightconnection`, Vfx `Moveborder`, `Movefromborder`,
  `Movebordercommand`.
- **Reuse:** `data + stock asset` (rally button for any building whose AI keeps a `Rally` command with a `Goalobject`).
- **Quirks:** the "already drawn" check looks for `Rallyspot` on the root, but it is created under `Vis`, so every
  `Render` adds another `Selectionchange` handler.
- **Cite:** Entity_Command_Tower_Fiekeep_Command.cs:12-24, 32-70; Entity_Ai_Tower_Fiekeep.cs:73-80.

### `Entity_Command_Tower_Snosnobox_Launch` (28 lines)
- **Does:** Move-style ground picker with preview Vfx `World_Snosnobox_Fallpos_Command`; click plays `Movebordercommand`.
- **Sends:** `Command_ServerRpc(Link.Name, {"To":{x,y,z}})` (`Entity_Ai_Tower_Snosnobox.Launchdata`); handler link
  `Launch` (nearest `Surface` within 0.5).
- **Reuse:** `data + stock asset` (artillery-style target picker; json same shape as Move).
- **Cite:** Entity_Command_Tower_Snosnobox_Launch.cs:9-27; Entity_Ai_Tower_Snosnobox.cs:292-300.

### `Entity_Command_Trap_Busterpad_Air` (153 lines)
- **Does:** Launch-pad target picker. Shows Vfx `Airtimedistanceind` (children `Start`, `Gradient`, `Box`, `Boxred`);
  a spot is good only if it lies straight ahead of the pad's visual forward and no `Block`-layer collider is between.
- **Sends:** `Command_ServerRpc(Link.Name, {"To":{x,y,z}})` (Movementdata) only if good, else `Notenough`; handler
  `Entity_Ai_Trap_Busterpad` link `Air`.
- **Reuse:** `data + stock asset` (needs that Vfx and a pad-like holder).
- **Quirks:** "straight ahead" is `Dot(...) >= 1f` (exact); `Debug.Log` every frame; no keybind shortcut.
- **Cite:** Entity_Command_Trap_Busterpad_Air.cs:30-52, 54-57, 106-138; Entity_Ai_Trap_Busterpad.cs:164, 192.

### `Entity_Command_Hero_Boutman_Fishfollow` (207 lines)
- **Does:** Picker "RECRUIT FISH" (Ui `Ui_Command_Boutman_Fishfollow`, Vfx `Fishborder`) that accepts an own entity
  tagged `Fish` whose `Following` stat is 0. The button is positioned by the digits in `Link.Name` (x27 px) and shows the
  followed fish's sprite (from `Link.Goalstring` = its NetworkObjectId) with a `Back/Cancel` button.
- **Sends:** `Command_ServerRpc(Link.Name, {"Ref":<NetworkObjectId>})`, cancel sends `{"Ref":0}`
  (`Entity_Ai_Hero_Boutman.Followdata`); handler: links containing `Fishfollow` in `Entity_Ai_Hero_Boutman`.
- **Hard-coded:** tag `Fish`, stat `Following`, icon `Boutman_Follow`, button children `Back/Fish`, `Back/Cancel`.
- **Reuse:** `vessel-bound` (Boutman's `Fish` tag and button prefab).
- **Quirks:** `int.Parse` of the digits throws if `Link.Name` has none; `Debug.Log("Here")` each frame.
- **Cite:** Entity_Command_Hero_Boutman_Fishfollow.cs:30-31, 35-64, 92-112, 114-133, 153-180.

### `Entity_Command_Tower_Fiesilo_Reroute` (121 lines)
- **Does:** Picker "CONNECT TO RESOURCE" (Ui `Ui_Command_Tower_Fiesilo_Reroute`, Vfx `Rerouteborder`) accepting a
  `Resource`-tagged entity whose `Following` stat is 0 ("Connected" otherwise). `Keybindable`: a keybind sends whatever
  is under the mouse.
- **Sends:** `Command_ServerRpc(Link.Name, {"Ref":<NetworkObjectId>})` (`Entity_Ai_Tower_Fiesilo.Followdata`); handler
  link `Reroute` (re-checks `Resource` tag).
- **Reuse:** `data-only` client side ("pick an ore node" picker sending `{"Ref"}`); stock handler is Fie's silo.
- **Cite:** Entity_Command_Tower_Fiesilo_Reroute.cs:18-29, 31-52, 54-112; Entity_Ai_Tower_Fiesilo.cs:116-122.

### `Entity_Command_Tower_Fiesilo_Convert` (56 lines)
- **Does:** `Entity_Command_Simple` whose `Render` colours the button and icon by `Link.Goalstring`: `Red`,
  `Yellow`, `Blue`, `Green`, `Purple`, `Orange` (icons `Redgun`...`Orangegun`, labels RED/YEL/BLUE/GREE/PURP/ORAN).
- **Sends:** `(Link.Name, "")`; stock handler link `Conversion` (`Entity_Ai_Tower_Fiesilo`).
- **Reads:** `Goalstring`; button children `Icon`, `Label/Title`.
- **Reuse:** `data-only` (a 6-colour mode button for any AI that keeps `Goalstring` in those words).
- **Cite:** Entity_Command_Tower_Fiesilo_Convert.cs:7-55.

### `Entity_Command_Tower_Smackyhut_Convert` (35 lines)
- **Does:** As Fiesilo_Convert for `Red`/`Yellow`/`Blue` with icons `Redaura`/`Yellowaura`/`Blueaura`.
- **Sends:** `(Link.Name, "")`; stock handler `Conversion` in `Entity_Ai_Tower_Smackyhut`.
- **Reuse:** `data-only` (3-colour mode button).
- **Cite:** Entity_Command_Tower_Smackyhut_Convert.cs:7-34; Entity_Ai_Tower_Smackyhut.cs:266.

### `Entity_Command_Tower_Simple_Toggle` (30 lines)
- **Does:** One-click toggle; `Render` dims the `Icon` (0.5 grey, alpha 0.3) when `Link.Goalstring == "Off"`.
- **Sends:** `(Link.Name, "")`.
- **Reuse:** `data-only` (on/off switch; the host AI flips `Goalstring` between "Off" and anything else).
- **Cite:** Entity_Command_Tower_Simple_Toggle.cs:7-29.

### `Entity_Command_Tower_Sandbox_Enemytype` (50 lines)
- **Does:** `Entity_Command_Simple` whose label names the stock enemy in `Goalstring` (`Enemy_Dagger`, `Enemy_Spear`,
  `Enemy_Aegis`, `Enemy_Ballista`, `Enemy_Maul`, `Enemy_Onager`, `Enemy_Claymore`, `Enemy_Excalibur`, `Enemy_Bulwark`).
- **Sends:** `(Link.Name, "")` (stock: `Entity_Ai_Tower_Sandbox`).
- **Reuse:** `data-only`, but other names leave the label unchanged.
- **Cite:** Entity_Command_Tower_Sandbox_Enemytype.cs:7-49.

### `Entity_Command_Tower_Sandbox_Elitetype` (42 lines)
- **Does:** Label for elite upgrade names: "" None, `Enemy_Health_Upgradeobject` Hardened, `Enemy_Red_` Tempered,
  `Enemy_Hyper_` Superheated, `Enemy_Chaos_` Fractured, `Enemy_Armor_` Annealed, `Enemy_Explode_` Molten.
- **Sends:** `(Link.Name, "")`.
- **Reuse:** `data-only` (stock elite names only).
- **Cite:** Entity_Command_Tower_Sandbox_Elitetype.cs:7-41.

### `Entity_Command_Tower_Merchut` (82 lines)
- **Does:** One-click contract button. `Render`: greys the button and shows `Checkmark` when `Link.Goalvalue >= 1`;
  child `Enemy` shows the sprite of the Mob in command `Spawntype`'s `Goalstring` with `Amount` = its `Goalvalue`,
  `Type` = display name of upgrade `Spawnelite.Goalstring`, `Gain` = `Spawnelite.Goalvalue`, `Bountycount` =
  "CONTRACT x of y" from `Link.Goalposition.x/.y`.
- **Sends:** `(Link.Name, "")` (stock `Entity_Ai_Tower_Merchut`, link `Contracted`).
- **Reuse:** `data + stock asset` (a bounty display for any holder whose AI keeps `Spawntype`/`Spawnelite`; the
  button prefab must have those children).
- **Cite:** Entity_Command_Tower_Merchut.cs:9-15, 26-45, 46-80; Entity_Ai_Tower_Merchut.cs:66.

---

## Visual scripts (`Entity_Vis_*`)

### `Entity_Vis_Base` (89 lines)
- **Does:** Abstract MonoBehaviour base: fields `Master/Being/Client/Radio/Remote/Dead`, virtual `Spawn, Despawn,
  Destroyed, Vischanged, Hitchanged, Selected, Deselected, State, Interact, Vis`; subscribes the client delegates.
- **Reuse:** `no` (abstract; `AddComponent` of it fails). Mod vis scripts subclass it.
- **Cite:** Entity_Vis_Base.cs:4-56, 58-88.

### `Entity_Vis_Animator_Base` (243 lines)
- **Does:** Sprite animator. Clones `Client.Spritevis` (a `Spriteobject`), drives every `SpriteRenderer` under
  `Currentvis` every `1/(Defaultspeedmulti*2)` s: folder = animation name with `Speed`, `Priority`, `Loop`, `Reset`,
  `Spriteto` (next folder when a non-loop ends); per-frame `Sound`. Variant = facing (`Changevariant`; falls back to 0
  when the folder has fewer variants). API: `Animplay(name, Default|Force|Forceloose|Defaultrestart)` (Default switches
  only if new priority >= current), `Animplaysoft(name, prio=0)` (only if current priority <= prio), `Adjustspeed`.
- **Attached by:** entity `Visualscripts`; needs a Sprite/Both visual with a `Spriteobject`; frames advance only while
  `Renderer[0].isVisible` or `Visentry.Spritealwayson`.
- **Reads:** no stats; animation names come from the subclasses/other scripts.
- **Hooks:** `Spawn` -> `Startup`, `Vischanged`, `Despawn`; async loop until destroyed.
- **Reuse:** `data-only` (alone it loops the `Defaultfolder`; the base every unit/tower sprite needs).
- **Quirks:** while offscreen it re-instantiates the Spriteobject clone every tick; `Adjustspeed(anim, s)` sets the
  speed of the **current** folder, not `anim`; an empty renderer array throws once and silently stops the loop.
- **Cite:** Entity_Vis_Animator_Base.cs:31-51, 53-80, 99-167, 169-179, 181-237.

### `Entity_Vis_Animator_Basic` (65 lines)
- **Does:** Animator + hit feedback, no movement: `Hurt` kicks the sprite material `_Offset` away from the hit
  (Position = hitter minus target) and flashes `_Overlayamount` 0.75; below 50% health (Number) leaves an overlay and
  `_Blinkamount`; `Heal` same without the kick; `Attack` plays sound `General_Hit` at Position; `Position` snaps
  `Visobj` to `Entityposition`.
- **Reads:** Vis `Hurt`, `Heal`, `Attack`, `Position`. **Hard-coded:** sound `General_Hit`; material props `_Offset`,
  `_Overlayamount`, `_Blinkamount` (stock sprite shader).
- **Reuse:** `data-only` (static sprite things: traps, props, stationary units).
- **Cite:** Entity_Vis_Animator_Basic.cs:8-64.

### `Entity_Vis_Animator_Mob` (173 lines)
- **Does:** Walking-unit animator: faces sprite variant by `Entitydirection` (`Rotatesprite`), and moves `Visobj` toward
  `Entityposition`: state `Move` -> soft `Move` anim with lead/slerp scaled by `Speed`; state containing `Idle` -> soft
  `Move` until within ~0.22 then `Idle`; state `Attack` -> soft `Idle`, faster settle; plus vertical easing. Offscreen
  it snaps. Same `Hurt/Heal/Attack/Position` feedback as Basic.
- **Reads:** `Master.State`; stat `Speed` (clamped 0.05-15, read on each `State` change); Vis types as Basic.
- **Hard-coded:** anim names `Move`, `Idle`; sound `General_Hit`.
- **Reuse:** `data-only` (the generic enemy/minion sprite body).
- **Quirks:** any other state name (e.g. a mod's `Stunned`) only turns the sprite: the visual stops following the
  entity until the state changes.
- **Cite:** Entity_Vis_Animator_Mob.cs:27-100, 102-159, 161-172.

### `Entity_Vis_Animator_Mob_Simple` (49 lines)
- **Does:** Animator that faces by `Entitydirection` and slerps `Visobj` 50% per FixedUpdate to `Entityposition`, any
  state; `Attack` -> `General_Hit` sound; `Hurt` -> kick + flash (no low-health blink, no Heal).
- **Reuse:** `data-only` (fast simple movers: projectiles-as-units, birds, summons).
- **Cite:** Entity_Vis_Animator_Mob_Simple.cs:16-21, 23-48.

### `Entity_Vis_Animator_Tower_Smackyarcher` (77 lines)
- **Does:** Sprite-tower animator: faces the last aim; `Shoot` (Position = aim vector) re-faces, forces anim `Attack`,
  nudges material `_Offset` toward the target; with several SpriteRenderers it animates one at a time and moves to the
  next each shot (alternating archers). State `Idle`/"" -> `Idle`, `Attack` -> `Active`.
- **Reads:** Vis `Shoot`; states `Idle`, `""`, `Attack`. **Hard-coded:** anims `Attack`, `Idle`, `Active`.
- **Reuse:** `data-only` (any sprite shooter tower; nothing Smacky-specific).
- **Cite:** Entity_Vis_Animator_Tower_Smackyarcher.cs:21-27, 29-62, 64-76.

### `Entity_Vis_Animator_Tower_Smackybomber` (80 lines)
- **Does:** As Smackyarcher but on Vis `Attack`: forces `Attack`, tweens each child of `Currentvis/Minions` toward the
  target; swaps `Client.Spritevis` to `Tower_Smackybomber_Dynamite_Spriteobject` if upgrade
  `Tower_Smackybomber_Range3_Upgradeobject` is owned, else to `..._Big_Spriteobject` with `..._Splash2_...`.
- **Reads:** Vis `Attack`; `Upgradelist`; child `Minions` (required: NRE without it).
- **Reuse:** `data-only` if the prefab has a `Minions` child; the sprite swaps only fire with Smacky's upgrade names.
- **Cite:** Entity_Vis_Animator_Tower_Smackybomber.cs:30-58, 74-79.

### `Entity_Vis_Animator_Tower_Snosnoheater` (42 lines)
- **Does:** Animator plus an always-on Vfx `World_Tower_Snosnoheater_Heat` under the entity, scaled to `Range*2` and
  re-tweened on every stat change.
- **Reads:** stat `Range`. **Hooks:** `Spawn`, `Stats.OnListChanged`, `Despawn`.
- **Reuse:** `data + stock asset` (visible heat aura ring).
- **Quirks:** `Animplay("Idle")` runs before `Startup`, so it is a no-op.
- **Cite:** Entity_Vis_Animator_Tower_Snosnoheater.cs:14-41.

### `Entity_Vis_Artifact_Computer` (54 lines)
- **Does:** Two always-on rings (Vfx `World_Artifact_Computerrangeeffect`) under `Vis`, scaled to `Range*2` and
  `Range2*2`, updated on stat change.
- **Reads:** stats `Range`, `Range2`.
- **Reuse:** `data + stock asset` (double aura display: inner/outer radius).
- **Cite:** Entity_Vis_Artifact_Computer.cs:14-43.

### `Entity_Vis_Artifact_Wavebreaker` (44 lines)
- **Does:** One always-on ring (Vfx `World_Artifact_Wavebreakereffect`) under `Vis`, scaled to `Range*2`.
- **Reads:** stat `Range`.
- **Reuse:** `data + stock asset` (aura display).
- **Cite:** Entity_Vis_Artifact_Wavebreaker.cs:14-33.

### `Entity_Vis_Block_Noplacement` (54 lines)
- **Does:** Hides child `Select` of `Currentvis`, shows it while the active command's `Objectname` contains `Build`
  (polls every 0.1 s); keeps `Visobj` on `Entityposition`/`Position` changes.
- **Reads:** `Game_Selection.Commandchange`; child `Select` (required).
- **Reuse:** `data-only` (a "no build here" overlay that appears only during placement).
- **Cite:** Entity_Vis_Block_Noplacement.cs:10-30, 37-47, 49-53.

### `Entity_Vis_Block_Surface` (34 lines)
- **Does:** Waits for the host Pingpong message `Block_Surface` (`Entity_Ai_Block_Surface.Block_Surface_Pinginfo`),
  then clones the hit prefab's `Collider` child once per `Blockdirections` entry, aims the main one, and destroys itself.
- **Reuse:** `data + stock asset` (ground tiles that also run `Entity_Ai_Block_Surface`).
- **Cite:** Entity_Vis_Block_Surface.cs:6-33.

### `Entity_Vis_Branching` (62 lines)
- **Does:** Connecting-wall mesh picker: reads 8 stats `Post_NW, Post_N, Post_NE, Post_E, Post_SE, Post_S, Post_SW,
  Post_W` as a bitmask, looks it up in the `Entity_Branchobject_Table` on child `Meshcontainer`, sets child `Mesh`'s
  mesh and Y rotation.
- **Reads:** those stats (set by `Entity_Ai_Tower_Smackywall`, `Entity_Ai_Tower_Fiewall`, `Entity_Ai_Blastheadore`).
- **Reuse:** `data-only` with a prefab carrying an `Entity_Branchobject_Table` and a stock wall AI.
- **Quirks:** never unsubscribes `Stats.OnListChanged`.
- **Cite:** Entity_Vis_Branching.cs:8, 10-45, 57-61.

### `Entity_Vis_Claymore` (77 lines)
- **Does:** When stat `Death >= 1`: plays sound `Enemy_Claymore_Activate` once, then flashes sprite `_Tintcolor`
  every FixedUpdate (fuse); state `Launch` -> soft anim `Attack`; Vis `Attack` -> forced `Attack`.
- **Reads:** stat `Death` (set by `Entity_Ai_Enemy_Claymore`), state `Launch`, Vis `Attack`; needs an animator first.
- **Reuse:** `data + stock asset` (a "about to explode" flasher for any sprite enemy that raises `Death`).
- **Cite:** Entity_Vis_Claymore.cs:22-62, 64-76.

### `Entity_Vis_Effect_Snosnosnow_Richblack` (60 lines)
- **Does:** Tints child `Snow`'s material and flattens the visual by tags: `Richred`, `Richblue`, `Richyellow` (-0.2 on
  two channels each), `Snoblack` (-0.5 all); re-runs on `Taglist` changes.
- **Reuse:** `vessel-bound` (Sno's tags and `Snow` child).
- **Quirks:** never unsubscribes `Taglist.OnListChanged`.
- **Cite:** Entity_Vis_Effect_Snosnosnow_Richblack.cs:8-48, 55-59.

### `Entity_Vis_Enemy_Ballista` (19 lines)
- **Does:** Vis `Shoot` -> forced anim `Attack`. Needs an animator first.
- **Reuse:** `data-only`.
- **Cite:** Entity_Vis_Enemy_Ballista.cs:7-18.

### `Entity_Vis_Enemy_Excalibur` (19 lines)
- **Does:** Vis `Attack` -> forced anim `Attack`.
- **Reuse:** `data-only`.
- **Cite:** Entity_Vis_Enemy_Excalibur.cs:7-18.

### `Entity_Vis_Enemy_Bulwark` (24 lines)
- **Does:** Vis `Attack` -> forced `Attack`; Vis `Shoot` -> Vfx `Bulwark_Init` at the entity + camera shake 0.5 s.
- **Reuse:** `data + stock asset`.
- **Cite:** Entity_Vis_Enemy_Bulwark.cs:12-23.

### `Entity_Vis_Enemy_Maul` (24 lines)
- **Does:** Vis `Attack` -> forced `Attack`; Vis `Shoot` -> Vfx `Maul_Explosion` + shake 0.5 s.
- **Reuse:** `data + stock asset` (slam/stomp look).
- **Cite:** Entity_Vis_Enemy_Maul.cs:12-23.

### `Entity_Vis_Enemy_Halberd` (39 lines)
- **Does:** State `Flying` -> soft anim `Flying`, slow slerp (0.05) of `Visobj`, turns on child `Flying` (plays sound
  `Enemy_Halberd_Sound` when it switches on) and off `Flyingend`; other states reverse the children.
- **Reads:** state `Flying`; children `Flying`, `Flyingend` (optional; `Flyingend` required once `Flying` exists).
- **Reuse:** `data-only` (leap/jump look for a sprite enemy whose AI uses state `Flying`).
- **Cite:** Entity_Vis_Enemy_Halberd.cs:8-32.

### `Entity_Vis_Enemy_Boutman` (76 lines)
- **Does:** States `Sleeping`/`Ko` -> soft anims of the same name, visual held at the entity; Vis `Wakeup` -> `Wakeup`,
  `Attack` -> cycles `Attack1..Attack3`, `Ko` -> forced `Ko`.
- **Hard-coded:** on Vis `Ko` while `Gamecomplete == "Running"` it writes player data `Unlock_Other/Enemy_Boutman_Defeat`.
- **Reuse:** `no` as is (any reuse that sends `Ko` grants a stock unlock on every client).
- **Cite:** Entity_Vis_Enemy_Boutman.cs:11-36, 44-75.

### `Entity_Vis_Enemy_Reallystrongfish` (76 lines)
- **Does:** As Enemy_Boutman, but Vis `Ko` plays no animation and writes `Unlock_Other/Enemy_Boutman_Defeat` and
  `Unlock_Other/Enemy_Fish_Defeat`.
- **Reuse:** `no` (unlock side effect).
- **Cite:** Entity_Vis_Enemy_Reallystrongfish.cs:44-75.

### `Entity_Vis_Fierally_Clientvis` (18 lines)
- **Does:** Shows child `Client` of the visual only to the owner and subowners (local session check).
- **Reuse:** `data-only` (owner-only markers: rally flags, private indicators).
- **Cite:** Entity_Vis_Fierally_Clientvis.cs:11-17.

### `Entity_Vis_Hero` (202 lines)
- **Does:** Vessel shell. On spawn (unless tag `Prespawn`): hides the hit object 3 s, drops the sprite from offset
  (-10,200,-10) to 0 over 3 s with forced `Falling`, then forced `Land`. Adds Ui `Playerind` (off-screen arrow in the
  owner's colour and icon), hidden while selected or in the `Photo` menu, destroyed on `Gamecomplete` Win/Lose.
  Despawn shows warning "ALERT ▪ VESSEL DROWNED" (`Herodeath`, 5 s). Vis `Fall` -> soft `Falling`, then `Landquick`
  after 0.5 s.
- **Reads:** tag `Prespawn`; needs an animator first; anims `Falling`, `Land`, `Landquick`.
- **Reuse:** `data-only` for a mod vessel (any other entity would show the drowned warning).
- **Quirks:** no stock AI sends Vis `Fall` (only `Herofall` exists, from `Entity_Ai_Effect_Chute`); never unsubscribes
  `Gamecomplete.OnValueChanged`.
- **Cite:** Entity_Vis_Hero.cs:33-82, 84-122, 124-158, 160-177, 185-201.

### `Entity_Vis_Hero_Boutman` (66 lines)
- **Does:** States `Fishing` -> soft `Fish`, `Ko` -> soft `Ko` (visual held); Vis `Fish` -> `Fishthrow`, `Attack` ->
  cycles `Attack1`/`Attack2`, `Shoot` -> `Attack3`, `Ko` -> forced `Ko`.
- **Reuse:** `data-only` technically (names are the mod's own sheet), but the states/Vis come from Boutman's AIs.
- **Cite:** Entity_Vis_Hero_Boutman.cs:10-35, 43-65.

### `Entity_Vis_Hero_Buster` (20 lines)
- **Does:** Vis `Attack` -> `Animplay("Attack", Defaultrestart)`.
- **Reuse:** `data-only`.
- **Cite:** Entity_Vis_Hero_Buster.cs:13-19.

### `Entity_Vis_Main_Core` (58 lines)
- **Does:** Mesh core: sets material[1] `_Overlaycolor` white; on Vis `Hurt` flashes `_Overlayamount` (shader
  `Shader Graphs/General Tower` or material[1]) and shows warning "ALERT ▪ BASIN UNDER ATTACK" (`Basindamage`, 3 s).
- **Reuse:** `data-only` for a mod core/objective (the warning text is fixed).
- **Quirks:** the warning is raised once per MeshRenderer per hit.
- **Cite:** Entity_Vis_Main_Core.cs:8-23, 30-57.

### `Entity_Vis_Main_Enemycore` (141 lines)
- **Does:** Mesh enemy core: purple overlay colour; pulses the overlay every `Interval` x 0.2 s, faster as stat
  `Healthresource` falls (<=50%: 10, <=25%: 5, <=10%: 2); at 0 plays Vfx `World_Enemysubcore_Death` (GameObject named
  `Enemysubcore`), `World_Enemycoretraining_Death` (skin `Training`) or `World_Enemycore_Death`, and hides renderers.
- **Reads:** stat `Healthresource`, `Skin`, GameObject name. **Hooks:** `Stats.OnListChanged`, `Entityposition`.
- **Reuse:** `data + stock asset` (a destructible enemy objective that pulses as it weakens).
- **Quirks:** `Debug.Log` on death; never unsubscribes.
- **Cite:** Entity_Vis_Main_Enemycore.cs:15-34, 41-96, 103-140.

### `Entity_Vis_Main_Flood` (32 lines)
- **Does:** Adds `Entity_Hitobject_Signals` to every collider; on non-host machines multiplies the velocity of any
  `Projectile_Master` (layer 8) staying inside by stat `Projslow` (read once at `Spawn`). Host side is
  `Entity_Ai_Main_Flood`.
- **Reuse:** `data + stock asset` (slowing water/zone for projectiles, paired with `Entity_Ai_Main_Flood`).
- **Quirks:** `Projslow` is sampled once at spawn (0 if stats not synced yet, which would stop projectiles on clients:
  unclear if it happens).
- **Cite:** Entity_Vis_Main_Flood.cs:9-31; Entity_Ai_Main_Flood.cs:22.

### `Entity_Vis_Mob_Attack` (19 lines)
- **Does:** Vis `Attack` or `Shoot` -> forced anim `Attack`.
- **Reuse:** `data-only` (the generic "play attack frames" hook for any sprite unit).
- **Cite:** Entity_Vis_Mob_Attack.cs:12-18.

### `Entity_Vis_Mob_Bird` (26 lines)
- **Does:** Animator subclass: always variant 1, slerps 50% to `Entityposition`, faces direction; Vis `Attack` ->
  forced `Hit`; Vis `Dead` -> forced `Attack` and hides `Visobj`.
- **Reuse:** `data-only` (one-facing flyer).
- **Cite:** Entity_Vis_Mob_Bird.cs:7-25.

### `Entity_Vis_Mob_Boutmandad` (98 lines)
- **Does:** Animator subclass for a leaping parent: state `Idle` -> soft "Idie", `Fly` -> hovers 3.5 up (slerp 0.05),
  soft `Fly`, shows Vfx `Boutdadbeam` (made in `OnEnable`, under `Vis`, -20 down); Vis `Shoot` -> `Attack`, `Fly` ->
  `Fly` + Vfx `World_Boutdadleave` + sound `Boutmom_Jump` (GameObject `Boutmom`) or `Boutdad_Jump`, `Land` -> `Idle` +
  Vfx `World_Boutdadenter` + `Boutdad_Land`.
- **Reuse:** `data + stock asset` (jump-away/land boss look), Boutman sounds fixed.
- **Quirks:** typo `Idie`: the Idle state only falls back to the `Defaultfolder`.
- **Cite:** Entity_Vis_Mob_Boutmandad.cs:18-58, 60-85, 87-97.

### `Entity_Vis_Mob_Boutmanfish` (154 lines)
- **Does:** Needs an animator. State `Dead` -> soft `Dead`, bobbing; `Pond` -> soft `Dead`, grey `_Tintcolor` (white
  if stat `Live > 0`); leaving those -> forced `Idle`, white. While stat `Following > 0` keeps debuff Vfx
  `Debuff_Recruit` on. Vis `Enterpond` -> Vfx `World_Barrelenter`; `Leavepond` -> hop + `World_Barrelexit`; `Dead` ->
  `Dead` + `World_Boutmanfishko`; `Attack` -> sound `Boutfish_Melee`.
- **Reuse:** `data + stock asset` (downed/revivable unit look), but states/stats come from Boutman's fish AIs.
- **Cite:** Entity_Vis_Mob_Boutmanfish.cs:21-27, 29-106, 116-153.

### `Entity_Vis_Mob_Boutmanfish_Engagerange` (115 lines)
- **Does:** On select shows Ui `Rangecontainer` (radius `Range`, under `Vis`, follows the unit) and Ui
  `Rangecontainersubengage` (radius `Engagerange`) centred on the `Defend` command's `Goalposition`; follows stat and
  command changes until deselected.
- **Reads:** stats `Range`, `Engagerange`; command `Defend` (kept by `Entity_Ai_Mob_Boutmanfish`, `_Boutmanfishmini`,
  `Entity_Ai_Mob_Fieknight`).
- **Reuse:** `data + stock asset` (leash ring around a guard point for any unit).
- **Quirks:** selecting twice without a deselect double-subscribes (harmless).
- **Cite:** Entity_Vis_Mob_Boutmanfish_Engagerange.cs:19-29, 31-48, 68-97, 105-114.

### `Entity_Vis_Mob_Enttrack` (12 lines)
- **Does:** Lerps `Visobj` 5% per FixedUpdate toward `Entityposition`.
- **Reuse:** `data-only` (smooth follower for a non-animated body; pair with a non-moving animator).
- **Cite:** Entity_Vis_Mob_Enttrack.cs:5-11.

### `Entity_Vis_Mob_Range` (27 lines)
- **Does:** `Entity_Vis_Tower_Range` whose ring is parented under `Vis` on select, so it moves with a unit.
- **Reuse:** `data + stock asset` (Ui `Rangecontainer`).
- **Cite:** Entity_Vis_Mob_Range.cs:5-26.

### `Entity_Vis_Movementlinear` (61 lines)
- **Does:** Needs an animator. Faces by direction; in state `Movelinear` soft anim `Movelinear` and lerps toward
  `Entityposition` at `Speed * dt`; vertical easing always.
- **Reads:** stat `Speed`, state `Movelinear` (from `Entity_Ai_Mob_Movementlinear`).
- **Reuse:** `data-only`.
- **Quirks:** in any other state the visual does not follow horizontally.
- **Cite:** Entity_Vis_Movementlinear.cs:22-30, 32-60.

### `Entity_Vis_Resource` (60 lines)
- **Does:** Mesh ore node: white overlay colour; Vis `Hurt` flashes `_Overlayamount`.
- **Reuse:** `data-only` (any mesh resource/prop that gets hit).
- **Cite:** Entity_Vis_Resource.cs:8-27, 34-59.

### `Entity_Vis_Resource_Opal` (97 lines)
- **Does:** Stat `Type` picks one of the visual's children `Red` (0), `Blue` (1), `All` (2), `Yellow` (3), and also
  toggles same-named children of the **map** (`Game_Client_Init.Instance.Mapvis`).
- **Reads:** stat `Type`; children `Red`, `Blue`, `Yellow`, `All` (required).
- **Reuse:** `data-only` with those four children, but it also flips map objects with those names.
- **Cite:** Entity_Vis_Resource_Opal.cs:18-86.

### `Entity_Vis_Select_Parent` (16 lines)
- **Does:** When selected, selects the `Goalobject` of the entity's `Parent` command instead.
- **Reads:** command `Parent` (created and synced by `Entity_Ai_Trap_Fierally` / `Entity_Ai_Trap_Fishnoplacement`).
- **Reuse:** `data-only` (click a satellite piece to select its owner building).
- **Cite:** Entity_Vis_Select_Parent.cs:6-15; Entity_Ai_Trap_Fierally.cs:5-6.

### `Entity_Vis_Tower` (95 lines)
- **Does:** Mesh tower: `Hurt` flashes overlay (shader `Shader Graphs/General Tower` or material[1]) with low-health
  `_Blinkamount`; `Heal` likewise; keeps `Visobj` on `Entityposition` changes.
- **Reuse:** `data-only` (the generic model tower body).
- **Cite:** Entity_Vis_Tower.cs:10-28, 30-94.

### `Entity_Vis_Tower_Range` (74 lines)
- **Does:** Ui `Rangecontainer` ring at `Vis` (or `Detect` child) scaled to `Range*2`, shown while selected; updates
  on stat changes while selected.
- **Reads:** stat `Range`; child `Detect` (optional).
- **Reuse:** `data + stock asset` (selection range ring for any tower).
- **Cite:** Entity_Vis_Tower_Range.cs:14-32, 39-59, 66-73.

### `Entity_Vis_Tower_Orientation` (43 lines)
- **Does:** While selected, shows Ui `Directioncontainer` under `Vis` (facing arrow).
- **Reuse:** `data + stock asset` (orientable buildings).
- **Cite:** Entity_Vis_Tower_Orientation.cs:10-42.

### `Entity_Vis_Tower_Artriobarrier` (58 lines)
- **Does:** On `Hurt` flashes the map's `Wall` meshes, warns "ALERT ▪ BARRIER DESTRUCTION IMMINENT" at <=10% health;
  on despawn hides map `Wall`, shows map `Middle`.
- **Hard-coded:** at `Wave >= 20` writes unlock `Unlock_Other/Map_Barrier_Blocked`.
- **Reuse:** `no` (map-bound, unlock side effect).
- **Cite:** Entity_Vis_Tower_Artriobarrier.cs:11-23, 25-57.

### `Entity_Vis_Tower_Boutmancage` (28 lines)
- **Does:** Warning "ALERT ▪ CAGE IS SOON TO BREAK!" at <=10% health; on despawn swaps map objects
  `Map_Manafold_Cage_Closed` -> `Map_Manafold_Cage_Open`.
- **Reuse:** `no` (map-bound).
- **Cite:** Entity_Vis_Tower_Boutmancage.cs:7-27.

### `Entity_Vis_Tower_Boutmanbarrel` (53 lines)
- **Does:** Always-on Vfx `World_Tower_Boutmanbarrel_Range` scaled to `Range*2`; shows `Vis/Open` when stat
  `Vacantcurrent > 0`, else `Vis/Closed`.
- **Reads:** stats `Range`, `Vacantcurrent`; children `Vis/Open`, `Vis/Closed` (required).
- **Reuse:** `data + stock asset` (a "has room / full" building with an aura ring).
- **Cite:** Entity_Vis_Tower_Boutmanbarrel.cs:14-42.

### `Entity_Vis_Tower_Boutmanboat_Cannon` (17 lines)
- **Does:** Vis `Shoot`: turns child `Pivot` (flat) and `Pivot/Gun` (full) toward the aim vector.
- **Reuse:** `data-only` (any model turret with `Pivot/Gun`).
- **Cite:** Entity_Vis_Tower_Boutmanboat_Cannon.cs:7-16.

### `Entity_Vis_Tower_Buff` (32 lines)
- **Does:** When stat `Effected > 0` activates every direct child of the entity's root GameObject, else deactivates
  them; re-checks on stat change. `Effected` is set by `Entity_Ai_Tower_Buff` / `Entity_Ai_Tower_Tilebuff*`.
- **Reuse:** `data-only` (buffed-indicator), but see quirk.
- **Quirks:** it toggles the root's children (where `Vis` lives), not children of the visual: unclear which objects the
  stock prefabs expect it to hide.
- **Cite:** Entity_Vis_Tower_Buff.cs:6-31.

### `Entity_Vis_Tower_Bustercheckpoint` (156 lines)
- **Does:** Ore-link display: Vfx `World_Tower_Bustercheckpoint_Fallpos` (children `Efficiency` text, `Ball`) sits on
  the `Ore` command's `Goalobject`; children `Crystal` take that ore's `Displaycolor`, `Crystalinner` its fill
  (`Healthresource` ratio); Vfx `Tower_Bustercheckpoint_Spawncrystal`/`_Upcrystal`/`World_Collectcheckpoint` on fill
  steps; while selected shows `Efficiency` % and a ball path ore->crystal.
- **Reads:** command `Ore` (Goalobject, Goalstring = ore Mob name), stats `Healthresource`, `Efficiency` (from
  `Entity_Ai_Trap_Bustercheckpoint`).
- **Reuse:** `data + stock asset` (needs that AI or one keeping `Ore`), in practice Buster-shaped.
- **Quirks:** without `Crystalinner` it throws every FixedUpdate.
- **Cite:** Entity_Vis_Tower_Bustercheckpoint.cs:31-39, 41-101, 103-131, 133-141.

### `Entity_Vis_Tower_Fiesilo` (230 lines)
- **Does:** Silo display: Vfx `World_Tower_Fiesilo_Fallpos` (children `Gradient` shown while selected, `Trail`
  VisualEffect pipe with `Goal`, `Color`, `Pixels`) runs to the `Ore` command's `Goalobject`; child `Status/Weapon`
  shows the gun colour/tile from command `Gun`'s Goalstring (Red, Blue, Orange, Yellow, Green, Purple, All) and stat
  `Gun` progress (Vfx `Fiesilo_Weapongenerate` at 1); `Status/Connecting` vs `Status/Mining` by stat `Connect`
  (Vfx `Fiesilo_Oreconnect`); Vis `Attack` pulses the pipe.
- **Reuse:** `vessel-bound` (Fie's silo commands/stats, `Entity_Ai_Tower_Fiesilo`).
- **Cite:** Entity_Vis_Tower_Fiesilo.cs:16-57, 77-87, 89-172, 182-201, 216-229.

### `Entity_Vis_Tower_Fiesilo_Secondore` (222 lines)
- **Does:** Copy of Fiesilo for the second ore: command `Ore2`/`Gun2`, stats `Gun2`/`Connect2`, child
  `Status/Weapon2` (shown only while `Gun2 > 0`), Vis `Attack2` (sent by `Entity_Ai_Tower_Fiesilo_Second`); no
  Connecting/Mining swap (uses the `Trail` layer as its flag).
- **Reuse:** `vessel-bound`.
- **Cite:** Entity_Vis_Tower_Fiesilo_Secondore.cs:75-95, 145-163, 177-186, 208.

### `Entity_Vis_Tower_Fiesilo_Frontrange` (74 lines)
- **Does:** Always-on Ui `Rangecontainersubengage` ring, `Range*2`, moved every 1 s onto the `Ore` command's
  `Goalobject` (hidden when none).
- **Reuse:** `data + stock asset` (ring around a linked target).
- **Quirks:** the loop `continue`s without awaiting when the ring is null (would spin the main thread); never
  unsubscribes stats.
- **Cite:** Entity_Vis_Tower_Fiesilo_Frontrange.cs:16-25, 27-56.

### `Entity_Vis_Tower_Smackyhut_Convert` (43 lines)
- **Does:** Colours child `Head`'s material[0] by the `Conversion` command's Goalstring (`Red`/`Blue`/`Yellow`).
- **Reads:** command `Conversion`; child `Head` (required).
- **Reuse:** `data-only` (mode colour on a model).
- **Cite:** Entity_Vis_Tower_Smackyhut_Convert.cs:7-26, 33-42.

### `Entity_Vis_Tower_Smackywall` (61 lines)
- **Does:** Turns child `Posts` to `Truedirection` and enables `Posts/Front|Back|Left|Right` by stats `Post_Front`,
  `Post_Back`, `Post_Left`, `Post_Right`, on both the visual and the hit prefab.
- **Reuse:** `data-only` (with a wall AI setting those stats, e.g. `Entity_Ai_Tower_Smackywall`).
- **Cite:** Entity_Vis_Tower_Smackywall.cs:9-27, 29-47, 55-60.

### `Entity_Vis_Tower_Snosnobox` (19 lines)
- **Does:** Vis `Shoot` -> shows child `Sno` and plays emitter Vfx `World_Tower_Snosnobox_Sno` on the entity; `Empty`
  -> hides `Sno`.
- **Reuse:** `data + stock asset` (loaded/empty indicator).
- **Cite:** Entity_Vis_Tower_Snosnobox.cs:7-18.

### `Entity_Vis_Tower_Snosnobox_Fall` (62 lines)
- **Does:** While selected, puts Vfx `World_Snosnobox_Fallpos` at the `Launch` command's `Goalposition` (updates on
  command changes).
- **Reads:** command `Launch` (kept by `Entity_Ai_Tower_Snosnobox`).
- **Reuse:** `data + stock asset` (shows a building's chosen target spot).
- **Cite:** Entity_Vis_Tower_Snosnobox_Fall.cs:13-25, 27-35, 37-61.

### `Entity_Vis_Tower_Snosnocannon` (65 lines)
- **Does:** Vis `Shoot` aims `Pivot`/`Pivot/Gun` (like Boutmanboat_Cannon); Vis `Snoble` (index in `Position.x`: 0
  `Snoble`, 1 `Snobleblack`, 2 `Snoblemini`, 3 `Snobleminiblack`) shows `Gun/Gunsnoble` and `Gun/Minion` with that
  sprite from `Tower_Snosnocannon_Snobles_Sprite` for 0.1 s.
- **Reuse:** `data-only` for `Shoot`; `vessel-bound` for `Snoble` (Sno sprites; out-of-range index throws).
- **Cite:** Entity_Vis_Tower_Snosnocannon.cs:10-16, 22-59, 61-64.

### `Entity_Vis_Tower_Snosnocannon_Snorange` (64 lines)
- **Does:** Second selection ring (Ui `Rangecontainersub`) scaled by stat `Rangesno`.
- **Reuse:** `data + stock asset` (a second radius, e.g. splash or support range).
- **Cite:** Entity_Vis_Tower_Snosnocannon_Snorange.cs:14-27, 34-63.

### `Entity_Vis_Trap_Busterpad` (18 lines)
- **Does:** On `Extrapositiondata` change, resets `Visobj` rotation and applies `Orientation`
  (`Helper_Gridtools.Rotateorientation`).
- **Reuse:** `data-only` (visual follows a host-side rotation).
- **Cite:** Entity_Vis_Trap_Busterpad.cs:7-17.

### `Entity_Vis_Trap_Busterpad_Air` (40 lines)
- **Does:** While the `Air` command's `Goalvalue == 1`, shows Vfx `Airtimedistanceactive` with child `Gradient`
  stretched from the visual to `Goalposition`.
- **Reads:** command `Air` (kept by `Entity_Ai_Trap_Busterpad`).
- **Reuse:** `data + stock asset`.
- **Cite:** Entity_Vis_Trap_Busterpad_Air.cs:11-39.

---

## Helpers and VFX components

### `Helper_Entityvis` (180 lines)
- **Public methods:**
  `Rotatesprite(master, facing, side, dir, animator)` picks the variant from `dir` vs the camera: v0 toward camera
  heading screen-right, v1 toward/left, v2 away/right, v3 away/left (toward/away at dot 0.4 with the camera forward,
  sides at ±0.2 with its right); returns the new facing/side/dir.
  `Getstatlocal(master, stat)` reads the synced `Stats` list (name "None", value 0 if missing).
  `Getcommandlocal(master, command)` reads the synced `Commandlist` (default struct if missing).
  `Highlight(owner)` spawns Vfx `Ownerborder` (named `Highlightborder`) under every `Tower`/`Trap` of that owner,
  tinted with the player's colour; returns them for the caller to destroy.
- **Reuse:** `data-only` (static helpers; mod code can call them).
- **Cite:** Helper_Entityvis.cs:11-99, 101-116, 118-135, 137-179.

### `Helper_Vfxtools` (214 lines)
- **Public methods:**
  `Oneshot(name|prefab, pos, dir)` pooled one-shot (Vfx group), `Oneshot(..., Dictionary<string,float> data)` complex
  pool; `Getshotpool` / `Getcomplexshotpool` (pools keyed by prefab **name**, instances under `Game_Vfx`);
  `Debuff(name, offset, dir, attachObj, duration)` attaches/refreshes a child named after the effect with
  `Vfx_Debuff` (refresh sets the duration, not max); `Removedebuff(name, obj)` sets duration 0;
  `Emitter(name, offset, dir, obj[, data])` attaches a persistent `Vfx_Emitter` child or re-fires it;
  `Shake(pos, dir, duration, range=25)` Cinemachine impulse via `Game_Vfx.Shakesource`;
  `Playsound(pos, name)` / `Playsoundui(name)` wrap `Bl_Sound`.
- **Reuse:** `data-only` (mod code can call them with stock or own Vfx).
- **Quirks:** `Getshotpool`/`Debuff`/`Emitter` call `SetActive(false)` on the source prefab itself; two prefabs with
  the same name share one pool.
- **Cite:** Helper_Vfxtools.cs:8-33, 35-60, 62-108, 110-140, 142-196, 198-213.

### `Vfx_Base` (84 lines)
- **Does:** Pooled one-shot. First `Fire` caches child `VisualEffect`s and `Vfx_Add_Base`s and sets `Duration` = the
  longest VisualEffect float `Lifetime` + 0.1; every `Fire` activates the object (VFX autoplay, add-ons run in
  `OnEnable`) and after `Duration` runs `Rundisable`, frees the slot, deactivates.
- **Reuse:** `data-only` (added by `Helper_Vfxtools`; a mod Vfx prefab needs a VisualEffect exposing `Lifetime`).
- **Quirks:** with no `Lifetime` the instance stays `Running` forever and is never reused: every shot instantiates a
  new copy; the non-soft branch that calls `Play()` is never used.
- **Cite:** Vfx_Base.cs:23-55, 57-83.

### `Vfx_Complex` (42 lines)
- **Does:** `Vfx_Base` meant to push `Data` floats into the root VisualEffect before playing.
- **Quirks:** its private `Run()` is never called (`Fire` runs the base's private `Run(Soft)`), so `Data` is ignored
  and the Data overload of `Oneshot` behaves like the plain one.
- **Reuse:** `no` beyond plain one-shot behaviour.
- **Cite:** Vfx_Complex.cs:10-41; Vfx_Base.cs:57-83.

### `Vfx_Debuff` (71 lines)
- **Does:** Attached status effect: counts `Duration` down in FixedUpdate; at <=0 renames itself `Effectdead`, sends
  `OnStop` to every child VisualEffect and destroys itself after the root effect's `Lifetime`.
- **Reuse:** `data-only` (via `Radio.Vfxdebuffcheck` / `Helper_Vfxtools.Debuff`; duration 0 = removed next tick).
- **Cite:** Vfx_Debuff.cs:23-28, 30-43, 45-70.

### `Vfx_Emitter` (64 lines)
- **Does:** Persistent attached effect: `Start` caches VisualEffects (one = `Effect`, several = `Effects`) and applies
  `Data` floats; `Run` sends `OnPlay` to them and runs add-ons.
- **Reuse:** `data-only` (via `Helper_Vfxtools.Emitter` / `Radio.Vfxemittercheck`).
- **Quirks:** with several VisualEffects and non-null `Data`, `Updatedata` throws (`Effect` is null); no VisualEffect at
  all throws in `Start`.
- **Cite:** Vfx_Emitter.cs:17-43, 45-63.

### `Vfx_Add_Base` (22 lines)
- **Does:** Add-on base for Vfx prefabs: `OnEnable` -> `Run()`, `OnDisable` -> `Rundisable()`.
- **Reuse:** `no` alone (base class).
- **Cite:** Vfx_Add_Base.cs:5-21.

### `Vfx_Sound` (18 lines)
- **Does:** Add-on: after `Delay` s plays sound `Sound` at the effect's position.
- **Reuse:** `data-only` (component with inspector fields on a Vfx prefab).
- **Cite:** Vfx_Sound.cs:9-17.

### `Vfx_Soundui` (18 lines)
- **Does:** Add-on: after `Delay` s plays UI sound `Sound`.
- **Reuse:** `data-only`.
- **Cite:** Vfx_Soundui.cs:9-17.

### `Vfx_Shake` (22 lines)
- **Does:** Add-on: after `Delay` s calls `Helper_Vfxtools.Shake(pos, forward*Offset, Duration, Am)` (defaults Am 25,
  Duration 0.5, Offset 1).
- **Reuse:** `data-only`.
- **Cite:** Vfx_Shake.cs:5-21.

### `Vfx_Volumeanimator` (36 lines)
- **Does:** Add-on: fades the object's post-process `Volume` weight 1 -> 0 over `Time` after `Delay`, with `Easetype`
  (enabled when the tween starts).
- **Reuse:** `data-only` (flash/tint screen effect on a Vfx prefab).
- **Cite:** Vfx_Volumeanimator.cs:21-35.

### `Vfx_Deatheffect` (9 lines)
- **Does:** Add-on: on disable (end of a pooled one-shot) fires `Oneshot(Vfx)` at its position/forward (chains effects).
- **Reuse:** `data-only`.
- **Cite:** Vfx_Deatheffect.cs:5-8.

---

## Most reusable for a mod

- `Entity_Command_Move` + stock `Entity_Ai_Mob_Movement`: a move order for any mod minion (link `Move`).
- `Tower_Snosnobox_Launch` / `Mob_Boutmanfish_Relocate` / `Tower_Fiekeep_Command`: ground-target buttons (strike, drop, rally) sending `{"To"}`/`{"Position"}` to a mod AI.
- `Entity_Command_Tower_Fiesilo_Reroute`: "pick an ore node" picker sending `{"Ref"}` to a mod AI.
- `Entity_Command_Simple`, `Tower_Simple_Toggle`, `Tower_Smackyhut_Convert`, `Tower_Fiesilo_Convert`: trigger, on/off and 3-/6-colour mode buttons.
- `Entity_Command_Selectother` / `Entity_Command_Artifact_Blurryphoto_Select`: "jump to my X" and "next copy" buttons.
- `Entity_Command_Build` / `Entity_Command_Upgrade` with the stock build/upgrade AIs: build and buy buttons, price scaled by `Goalvalue`.
- `Entity_Vis_Animator_Mob` + `Entity_Vis_Mob_Attack`: a new sprite enemy or minion body from the mod's own `Spriteobject`.
- `Entity_Vis_Animator_Tower_Smackyarcher` / `Entity_Vis_Tower_Boutmanboat_Cannon`: sprite or model turrets that turn to the target on `Shoot`.
- `Entity_Vis_Tower_Range` / `Mob_Range` / `Tower_Snosnocannon_Snorange`: selection rings for `Range` and a second radius.
- `Entity_Vis_Artifact_Wavebreaker` / `Artifact_Computer` / `Animator_Tower_Snosnoheater`: always-on aura rings by `Range` (and `Range2`).
- `Entity_Vis_Mob_Boutmanfish_Engagerange`: leash ring around a `Defend` guard point.
- `Entity_Vis_Tower_Buff` + `Entity_Ai_Tower_Buff`: shown-while-buffed indicator (`Effected`; mind its quirk).
- `Entity_Vis_Fierally_Clientvis` / `Entity_Vis_Select_Parent`: owner-only markers; click a satellite to select its parent.
- `Entity_Vis_Main_Enemycore`: a destructible objective that pulses faster as `Healthresource` falls.
- `Helper_Vfxtools.Debuff`/`Oneshot` + `Vfx_Sound`/`Vfx_Shake`/`Vfx_Deatheffect` on a mod Vfx with a `Lifetime` float: debuffs, hit sparks.
