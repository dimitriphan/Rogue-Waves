# 20 · Menus, dialogs and the HUD (how a pack puts a vote, a shop and a run line on screen)

What the Rogue Waves mode needs to show: the in-map vote, VENDCINT's shop, Direction picks, a run line (Pure Aura,
objective, retries), banners and prompts. Everything here was **read from the game's code** (decompiled as in `13`
§11) on 2026-10-14 unless it says otherwise; nothing was tried in game except what `15` §14.6 and §15 record for the
choice dialog (Riptide, RogueLab). Files in `Blockage.Menu.` are cited by their short name (`Blu1_Ui_Handler.cs`).
The networking side of every recipe is `19` §6; compiled sketches are in `examples/recipes/RecipeUi.cs`.

Read `01` §8, §11, `05` §7-8 and `15` §6, §14.6, §18.2 first.

## 1. The menu system

| Piece | Role | Where |
| --- | --- | --- |
| `Bl_Menus` (static) | `Menus` (name → menu), `Ui` (the scene's root), `Overui`, `Changemenu`, `Closemenu`, `Getactivemenu` | Bl_Menus.cs:11-62 |
| `Blu1_Ui_Handler` | one per scene: canvases, layers, EventSystem, throttle, gamepad reselect, UI scale | Blu1_Ui_Handler.cs:70-700 |
| `Blu2_Menu_Handler` | one menu: `Layer`, `Historical`, `Overlay`, `Standalone`, transitions, `Defaults`, `Gamepadstartbutton`, `In_Sinkinputs`, `Items` (child name → GameObject) | Blu2_Menu_Handler.cs:46-114 |
| `Blu3_Item` (+ Button, Toggle, List, Options, Slider) | registers in its parent menu's `Items` under its **GameObject name**; hover and click only on the active layer | Blu3_Item.cs:27-93 |
| `Blu4_*` | `Scaler` (anchor placement, tweens), `Tooltip`, `Group` (show/hide), `Menu_Change`, `Sound`, `Input_Buttonkeybind`, `Externalcanvaslinker`, `Button_Unregistered` | §4, §6 |

- **Menus are rebuilt per scene.** Each scene's `Blu1_Ui_Handler.Awake` replaces `Bl_Menus.Menus` with that scene's
  menus (Blu1_Ui_Handler.cs:84-127). A reference kept across a scene change points at a destroyed object.
- **A mod cannot add a menu.** A `Blu2_Menu_Handler` created later joins only `Ui.Menus` in `Start`, never
  `Bl_Menus.Menus` or the layers (Blu2_Menu_Handler.cs:301-312), so `Changemenu` cannot reach it.

### 1.1 `Changemenu` refuses a call three ways, two of them silently

`Bl_Menus.Changemenu(name, Force = false, Source = null, Refresh = false)` (Blu1_Ui_Handler.cs:360-587):

- **Throttle:** without `Force`, a call while a transition runs is **dropped** (:362-365).
- **Unknown name:** ignored without a log (:394-397). Same menu already active: nothing unless `Refresh`.
- **Standalone menus** are refused, with only the log line "`<name>` is a Standalone menu and cannot use this method"
  (:583-586); `Changemenustandalone(name, state)` toggles them (no stock caller).
- `"Back"` and `"Close"` are pseudo-names for the layer history (:379-393).
- Layers: a menu replaces the active menu of its layer; only the **active layer** is interactable; lower layers stay
  visible (:447-551). While a higher layer (the choice dialog) is up, HUD buttons cannot be clicked.
- `In_Sinkinputs` adds `"Menu"` to `Bl_Inputs.Disables` only while the transition runs (:477-480, 618-627).

### 1.2 Menu names (from every stock call)

| Scene | Names |
| --- | --- |
| Main menu | `Main`, `Play`, `Tutorial`, `Tutorialplay`, `Journal`, `More`, `Settings`, `Tocredits`, `Start`, `Mainidle`, `Frommap`, `Loading` |
| Boat | `World`, `Gamecore`, `Setup`, `Character`, `Discovery`, `Leaderboard`, `Waypoint`, `Tomap`, `Betweenworld`, `Unlock`, `Journal`, `Loading` |
| Map | `Main` (the HUD), `Pause`, `Settings`, `Cine`, `Tutorial`, `Empty`, `Toworld`, `Win`/`Lose`, `Confirmation` (the choice dialog), `Chat`, `Photo` |

`"Main"` is a different menu in each scene: check the scene before using `Bl_Menus.Menus["Main"]`.

### 1.3 Escape and gamepad

- Escape opens `Pause` only when the HUD's layer is the active one and `Spawnsafe` is set (Tds_Pausemenu.cs:152-155);
  it stops time only in a solo game (:187-194). While a dialog is up, Escape does nothing; there is no generic
  "Escape closes the top menu".
- Gamepad: each menu's `Gamepadstartbutton` is the only supported focus; `Blu1_Ui_Handler.Update` re-selects it
  whenever the selection is empty or outside the active menu, and in a match `Game_Ui.Update` forces the command bar
  (Blu1_Ui_Handler.cs:645-692; Game_Ui.cs:1884-1911). **A mod panel outside any menu loses pad focus every frame**;
  for pad players prefer the choice dialog.

## 2. The choice dialog (`Bla_Ui_Confirmation`) and other prompts

`Bla_Ui_Confirmation.Instance.Runprompt(entries, title, sub, Forcedisplaymenu = true)` returns `UniTask<Entry>`
(Bla_Ui_Confirmation.cs:342-357).

- **Entry:** `Name` (returned, never shown), `Display_Name`, `Display_Color`, `Display_Desc` (hover box),
  `Display_Image` (a Sprite), `Display_Imagecolor`, `Tomenu`. No price, no disabled state, no per-card callback.
- **Its own menu change is not forced** (:118-121), so a running transition drops it, and it sets each card as the
  parent menu's pad start through `GetComponentInParent`, which **throws while the menu is inactive** (:190; RogueLab
  §15 hit it). Open the menu yourself with `Force`, wait until it is up, then call with `Forcedisplaymenu: false`
  (`15` §14.6 rule; recipe `RecipeUi.Prompt`).
- The **last** card becomes the pad's start button. The first click wins.
- **No timeout and no cancel:** closing the menu another way leaves the task pending. A timed vote closes the dialog
  itself (`Bl_Menus.Closemenu(dialog.gameObject, true)`) and treats the pending task as "no vote".
- `List`'s `GridLayoutGroup` is a mod-adjustable default (Riptide and RogueLab use up to 4 per row).
- **What it is made of** (read in game, 2026-11-03): `Confirmation` (its own Canvas) > `Main` > `Fade` (the dim), `Back` (the dark
  edge at the screen's right), `List` (a grid with a `ContentSizeFitter`, cell 60, gap -5, anchored at (1, 0.2) with its pivot top
  right, cards filling from the right) and `Title` (with `Line`, the corner's rule, 300 by 1000, and a small `Image`). `Subbox` and
  `Descbox` are the same object, `Sub`, a child of `List` that ignores the layout: one line 1000 wide, right-aligned, autosized
  to 10.35, running left from the cards. A long hover line runs off the screen's left edge wherever the cards are.
- **It slides in by anchors, and late** (pitfall #137): `Main` and `List` each carry a `Move` on their `Blu4_Transition_Animator`.
  `Main`'s anchors go from x 0.3 to 0 for about a second after `Intransition` has gone false, some 0.55 s after `Changemenu`, and
  before that every rectangle under it reads nonsense. Nothing measured before about 1.6 s is true, and `Main` must never be
  moved, since the dim and the dark edge go with it. To place the cards somewhere else, anchor `List` and `Title` inside `Main` and
  write them again in `Canvas.willRenderCanvases`, because `List`'s own `Move` writes its anchors back. `RWDialog.Fit` in
  `ModSource/RogueWavesMode/Code/11_Dialog.cs` does it, and puts everything back once the dialog has gone.
- **Placed elsewhere, keep it smooth and on whole pixels** (pitfall #138): don't ask this dialog's own text for its size, and leave the
  title's autosize on. The stock corner stands on whole screen pixels, and the art is drawn without smoothing, so put `List` and
  `Title` on whole pixels too.
- **The UI scale is the player's:** `Blu1_Ui_Handler.Updatescale` sets the `CanvasScaler`'s `scaleFactor`, and a resolution change
  doesn't touch it. On Dimitri's PC it is 3, so the canvas is 853 by 480 at 2560x1440 and 426 by 240 at 1280x720. Fit to the root
  canvas's rectangle, not to a resolution.
- **Client-local:** a pick reaches the host only through a command (`19` §6 rule 6).

| Other prompt | Call | Reach | Where |
| --- | --- | --- | --- |
| Warning popup (icon, upper-cased title, `Warning` sound) | `Helper_Uitools.Warning(text, icon, s)`; host `entity.Radio.Uiwarning_ClientRpc(text, icon, s)` | local / all | Game_Ui.cs:1582-1620 |
| Note under the top banner (5 s) | host writes `Game_Mapvars.Instance.Display` → `Display_Note` | all | Game_Ui.cs:1541-1555 |
| Tutorial prompts (`Minidescprompt`, `Highlightprompt`, `Descprompt`, `Imageprompt`) | `Tds_Tutorialmenu.Instance…` | local | Tds_Tutorialmenu.cs:67-209 |
| Chat line | host `Blm_Ui_Chat.Instance.Chat.Add(new Link_Chatcontainer{ Text, Sender })` | all, kept | Blm_Ui_Chat.cs:14-116 |

- **Warnings share one title:** the newest text titles all stacked icons, and a warning with the same icon replaces
  the previous one (Game_Ui.cs:1594-1615). Two mod warnings in a row show only the second text.
- **`Display_Note` shows only when its value changes** (Game_Ui.cs:1543), and `Mode_Normal` writes the whole
  `Display` struct back from its own copy at its own moments (Mode_Normal.cs:217, 261-486): a mod's note is
  overwritten, and an older note can replay. `Display_State` and `Display_Wave` are game logic (Cardinals, Merchut,
  Treated cores and the music read them), so only `Display_Note` is free text, and only between the mode's writes.
- **Tutorial prompts exist in every match** (the build tool's rotate hint and the Cardinal hint use them,
  Tds_Tutorialprompts.cs:1072-1110), as `15` §18.2 says. Still untested from a
  mod.

## 3. The HUD (`Game_Ui`, menu `Main` of the map scene)

`Game_Ui.Instance` initialises **on clients only** (the host is a client too) and loads its row prefabs from the `Ui`
group: `Warningcontainer`, `Resourcecontainer`, `Upgradecontainer`, `Statscontainer`, `Commandcontainer`,
`Countercontainer`, `Costcontainer`, `Buildcontainer`, `Nextupgradecontainer`, `Upgradepreviewcontainer`,
`Barcontainer` (Game_Ui.cs:1745-1839).

| HUD item | Shows | Fed by |
| --- | --- | --- |
| `Resources` | the local player's wallet | `Tds_Clientinfo.Resources` (Game_Ui.cs:1436-1474) |
| `Top`, `Wave`, `Wavetimer`, `Note` | banner, Tide, timer, note | `Game_Mapvars.Display`, `Time_Wave` (:1493-1580) |
| `Selection`, `Portrait`, `Health`, `Stats`, `Counters` | the **selected** entity | its stats and counters (:144-234, 791-1006) |
| `Commandlist`, `Customlist`, `Buildlist`, `Upgradelist` | buttons, custom UI, build menu, upgrade tree | the selected entity's `Commandlist` (:244-789) |
| Cardinal row | owned Cardinals, flash on change | `Artifacttrack` (Game_Artifacts.cs:19-135); the count never shows |
| Player list | names, vessels, ready marks | Game_Client_Players.cs:25-71 |

No minimap exists. After 10 s idle the top bar hides, and anything placed inside `Items["Top"]` with it
(Game_Ui.cs:1856-1883). The health bar shows only for the selected entity.

**Commands are the HUD's per-owner channel.** They are drawn only for the selected entity, only for its owner or a
sub-owner (Game_Ui.cs:298-301). By `Commandobject.Style`: `Button` → a button with icon and upper-cased name;
`Bar` → title and description; `Upgradebuild` → the build list (costs × the `Upgrade` command's `Goalvalue`);
`Custom` with a `Customui` prefab → that prefab in `Customlist`; **`None` → nothing drawn, but it exists and syncs**.
Each drawn command's `Render(button)` runs on every refresh and may draw from its own `Goalstring`/`Goalvalue`.

### 3.1 What a mod can feed without building UI

| Need | Route | Seen by | Limits |
| --- | --- | --- | --- |
| A number on the vessel | a `Counter` stat with a registered icon (`05` §9) | anyone who selects it | hidden unless selected; a row without an icon is skipped (Game_Ui.cs:166-205) |
| A per-player value for client code | a `None` command on the player's vessel; host writes `Goalstring`/`Goalvalue`, syncs with `Updatecommand(m, link, "", false, false)` | every client; the mod picks the owner | 61 bytes + one float per command (Riptide's offers ride this way, on its SALVAGE `Button` command) |
| A line for everyone | `Display_Note` or `Uiwarning_ClientRpc` | all | §2 |
| A resource in the wallet | `Helper_Resources.Giveresource` with a registered resource icon | its owner | no icon → exception in `Updateresource` (Game_Ui.cs:1449-1452) |
| A text in chat | host `Chat.Add` | all, kept in the log | 509 bytes; rich text works; not localized |

## 4. Building new UI from mod code

- **Under the HUD** (`Bl_Menus.Menus["Main"].transform` in the map scene): inherits the HUD's canvas, scale and
  show/hide. Argo clones the stock `Ui_Command_Sell` prompt into `Items["Command"]`
  (`examples/code/Argo_14_CrewSelect.cs`).
- **An own Canvas** (Screen Space Overlay, `sortingOrder` above the menu layers, `CanvasScaler`,
  `GraphicRaycaster`): independent of menus; the scene's EventSystem makes plain `UnityEngine.UI.Button`s work
  (inferred). Copy the UI scale from the save's `Settings/Uiscale` or add `Blu4_Externalcanvaslinker`
  (Blu1_Ui_Handler.cs:318-339).
- **Reusable stock pieces:** a HUD row prefab (`Bl_Address.Getload("Ui", "Commandcontainer")` and the ten others), the
  picker prompt `Ui_Command_Sell`, a choice card (`Bla_Ui_Confirmation.Confirmbutton`), a TMP font (copy `font` and
  `fontSharedMaterial` from any HUD label), `Bl_Obj.Uifull`/`Uizero`/`Destroychildren`, `Blu4_Scaler.Setposition`,
  `Blu4_Group.Run` (where `State` true means **hidden**).
- **Blockage components that read their parent menu throw outside one:** `Blu3_Item` hover, `Blu4_Sound`,
  `Blu4_Button_Animator`, `Blu4_Tooltip` with `Default = true` (Blu3_Item.cs:59-93; Blu4_Sound.cs:19-25). On an own
  canvas strip them from clones, or use `Blu4_Button_Unregistered`.
- **Input:** game input is `Bl_Inputs.Actions[name]` (`Blockage.Input`; write the namespace, `Input` alone is
  ambiguous with Unity's). Any key in `Bl_Inputs.Disables` mutes `Function` callbacks (world clicks), not
  `Functionoverride` (pause, chat). A modal panel adds its **own** key and always removes it; `"Menu"` is cleared by
  the menu system every frame.
- **World clicks through mod UI:** a select click over UI is ignored (`Bl_Menus.Overui`), **but only when no command
  is active**: while build, move or a picker is active, its own release handler fires under a click on mod UI too
  (Entity_Command_Itemselect_Base.cs:67-83; Entity_Command_Build.cs:148-151). Cancel the active command when opening a
  panel (`Game_Selection.Instance.Select()` clears it), and set `raycastTarget = false` on decoration.
- **Scrolling** zooms the camera regardless of UI: set `Game_Cameracontroller.Instance.Zoomlock` while a list is
  hovered (Game_Cameracontroller.cs:334-340).

## 5. Recipes for the Rogue Waves pieces (compiled sketches: `examples/recipes/RecipeUi.cs`)

| Piece | Host | Every client | Stock pieces |
| --- | --- | --- | --- |
| **In-map vote** | writes the options into a `None` command `RW_Vote` on each vessel (`Goalstring` "A\|B\|C", `Goalvalue` = round), the tally into `RW_Tally`; counts picks per `Sessionid` from the vessel's command; closes on a timer | sees a new round number, opens the dialog (forced menu, wait, `Runprompt(..., false)`), sends the pick as a command; rewrites the subtitle from `RW_Tally` | choice dialog, commands |
| **Shop (VENDCINT)** | on the buy command: `Helper_Resources.Useresources(cost, owner)` (owner from `master.Entitydata`), then grant; on failure a targeted `Pongload` to the buyer (`19` §6 rule 4) | cards with the price in the name, greyed when the synced wallet can't pay | choice dialog, wallet |
| **Run line** (Pure Aura, objective, retries) | writes a `None` command `RW_Run` on each vessel | a TMP label under the HUD, font copied from `Items["Wave"]`, `raycastTarget` off, text from its own vessel's `RW_Run`; rebuilt at each map | HUD, commands |
| **Banner** | `Uiwarning_ClientRpc` (popup), `Display_Note` (slide-in), `Vis_ClientRpc` + a client panel, or a chat line | — | §2 |

Two traps in the stock payment helpers (Helper_Resources.cs:95-140): `Checkresources` compares `Amount × Scale` but
`Useresources` charges `Amount × Scale × Resourcelossmultiplier`, so with a cost modifier above 1 the buyer can go
below zero; and `Checkresources` returns **true** for an owner not in `Prevsessionlist`. A shop checks the multiplied
price itself.

## 6. Tooltips, rich text, icons, localization

- `Blu4_Tooltip` (pointer only): `Text`, `Localize`, `Followmouse`, `Tooltipprefab`. The HUD fills icon tooltips with
  the icon's `Displayname` and command tooltips with the key only.
- Rich text: TMP tags (`<b>`, `<color>`) work; **no `<sprite>` tag** is set up anywhere, so inline icons are separate
  `Image`s (`Helper_Uitools.Icon(name, image)`). `Warning` upper-cases its text. The game's separator is "▪"
  (3 bytes in a 61-byte string).
- Text through `Bla_Localization.SetText(label, key)` is rewritten from its key on every language change, so a label
  set once with `SetText` and later with `.text` reverts. How a pack translates: `21` §3.

## 7. Pitfalls (predicted from the code; move to `09` once met)

1. `Changemenu` drops a call during a transition, ignores an unknown name and refuses a standalone menu: use
   `Force: true` for a dialog the host asked for, then wait for the menu.
2. `Runprompt` has no timeout or cancel: a timed vote closes the dialog itself.
3. While a dialog is up, Pause does nothing and the command bar is not clickable: a shop left open blocks the HUD.
4. A leftover key in `Bl_Inputs.Disables` freezes world input for the session.
5. Mod UI does not shield the world while a command is active (§4).
6. Commands show only for the selected, owned entity; a player who selected a tower doesn't see the vessel's buttons
   (`Game_Selection.Instance.Selected = vessel; Select()` brings them back).
7. A resource or counter without a registered icon breaks the wallet or hides the row.
8. `Display_Note` is overwritten by the mode; `Display_State`/`Display_Wave` are game logic.
9. `Game_Ui` and menus are per scene and client-side: rebuild mod UI on each map (the `Gameclient` keyword or polling
   `Game_Ui.Instance`).

## 8. Sources

The decompiled `Assembly-CSharp.dll` (Blockage.Menu.*, Bla_Ui_*, Game_Ui, Helper_Uitools, Helper_Resources,
Tds_Pausemenu, Tds_Tutorialmenu, Blm_Ui_Chat, Entity_Command_*), read on 2026-10-14 by a parallel reader and
spot-checked by hand (`Pingload`/`Pongload`, `Checkresources`/`Useresources`); the mod sources of Riptide, RogueLab and
Argo for what already worked in game.
