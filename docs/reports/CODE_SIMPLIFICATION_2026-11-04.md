# The mod's code against the base game's: what could be simpler (2026-11-04)

> **Since written (2026-11-04, later the same day):** Dimitri said go. Findings 7, 8 (without SONAR), 2 and 3 are built
> and compiled, not yet run in game; finding 1 is not built, because it would change numbers in four places. What was
> built, what was not and the PC list: `SIMPLIFICATION_BUILD_2026-11-04.md`.

Dimitri asked (2026-11-04 17:04Z): look through the code of the modded variables, compare it to the base game's code,
find ways to simplify mechanics or bring them closer to the base game, and where something is overcomplicated, find
the method that removes it. **No change was made**: this page is findings and proposed methods only.

**What was read.** The shipping code of every part at `07bde83` (PR #73 merged on top of #72): Yossele, Spool, Argo,
Uloe, RogueWavesFoes, RogueWavesMode and `Shared`, about 30,600 lines once the debug-only files (`9x_*.cs`,
`ModKitTest`, `ModKitPlay`) are left out. The game's `Assembly-CSharp.dll` was decompiled again in this thread with
the kit's recipe (`TDS_Vessel_Creator/docs/13` §11, 703 types). The decompile stays in the thread's scratchpad and is
not committed. Prior studies used: `ENGINE_STUDY_2026-10-28.md` and its behaviour catalogue, the Argo code review
(`research/ARGO_CODE_REVIEW_2026-10-12.md` in project files), kit chapters 01, 05, 09, 15 and 17. "Modded variables"
is read as the mod's stats, mechanics and the systems that drive them: stat pipelines, timers, debuffs, auras, armour,
upgrade flow and the state each part keeps.

**Tags.** **read** = from the decompiled code only; **proven** = seen in game by an earlier round (cited). Every
proposal below is read only. None has been tried in game, and each says which PC check it needs before it could
replace proven behaviour (the 2026-11-04 round: Spool 133/0, Argo 142/0, Yossele 173/0, Uloe 148/0, mode 148/0).
File paths are under `TDS_Mod/ModSource/` unless they say otherwise; game classes are cited as `Class.cs:line` in this
decompile.

## Verdict

Most of the mod already rides the base game's own systems. Stats are the game's stats with the game's modifiers, and
timed debuffs use the engine's own timed `Addmod`. Counters are `Style = Counter` stats, and the mode is a copy of
`Mode_Normal` with its Tide table. Sea weather spawns the stock map objects, and the choice dialog is the stock one.
Several Directions run on stock effect scripts, Vampire Dentures reads the game's own `Damagedone`, and Aging copies
Melting. There is no framework or layer with one user.

The overcomplication sits in three places, and each has a base-game method that removes it:

1. **Damage taken is scaled by five hand-kept hit hooks** (Yossele, Argo, Spool, Uloe, the mode). Each one keeps its
   own handler at the front of the entity's `Oninteract` chain, ten times a second, because the game rebuilds that chain
   on every upgrade. The base game does armour and vulnerability with **one stat, `Resistadd`, and one stock effect
   script, `Entity_Ai_Effect_Fiewall_Regen`** (Fie's walls and Smacky's Beacon use it). That stat-and-script route
   survives the rebuild by itself.
2. **Every building brain re-implements `Entity_Ai_General_Upgrade` line for line**, in four copies, after stripping the
   stock script. The stock script is generic, and keeping it would also make Voice in a Bottle work on pack buildings.
3. **Four copies of the same scaffold**: each vessel's `01_Util`, `03_Assets` and `04_Content` share 75-85% of their
   lines. That is about 2,500 lines in the three extra copies, while the pack's build already compiles `Shared` into
   every part.

The rest is either a real new mechanic with no stock script (spores, vines, water networks, Light, Thread, Growth) or
a documented workaround for a quirk a PC round found. "What looks reimplemented but should stay" below lists those, so
nobody re-derives them.

## Ranked findings

The ranking is by how much complexity each removes, weighed against the risk to the proven behaviour. "Behaviour"
says whether a player would notice. Line counts are estimates from the source.

| # | Finding | Removes (about) | Behaviour | Risk to the 148/0 | Needs |
| --- | --- | --- | --- | --- | --- |
| 1 | Armour and vulnerability as the stock `Resistadd` stat + `Fiewall_Regen` script | 300 lines of hooks; 3 of 5 hook copies shrink to their leftovers | none if one held modifier carries today's total (route A); slightly different stacking if each source is its own modifier (route B) | medium | PC check R1-R5; Spring Tide moved off the Beacon first |
| 2 | Keep the stock `Entity_Ai_General_Upgrade` on pack buildings instead of the mirror | 200 lines (4 × 50) | Voice in a Bottle starts working on pack buildings | low-medium | PC check G1-G3 |
| 3 | One shared scaffold in `Shared` (Util, Assets, Content helpers, one hit-hook helper) | 2,500 lines | none | low for play, medium for the build | kit-policy change (kit `05` §0), one self-test round of all four vessels |
| 4 | Vine Blockade's barrier as the stock `Healthover` layer | 25 lines | barrier shows on the HUD; a hit bigger than what is left goes through whole | low | design call |
| 5 | Pin Cushion: Spin and Loose Stuffing from the game's `Damagetaken`; Reflect as stock Bramble | 30-60 lines | Bramble's prick is flat and hits ranged attackers too | low | design call for Reflect |
| 6 | Argo's "Slowed" list read from the foe's own Speed stat | 40 lines | a teammate's slow also counts for the Scatter's bonus | medium | design call |
| 7 | Uloe's dead stock-mark branch and its switch | 30 lines | none | none | compile and Uloe's self-test |
| 8 | The 2026-10-12 Argo review, still open | 430 lines (its own count) | none | none to low | Dimitri's word on SONAR |
| 9 | Brain timers on the game's tick delegates | small | none | medium | not worth doing alone |
| 10 | Yossele's regeneration as the stock Aegis | 15 lines | a heal look every second, even at full health | low | not recommended now |

### 1. Armour and vulnerability: one stat, `Resistadd`, and the stock `Fiewall_Regen` script

**What the mod does.** Five parts each keep a static table per entity and a delegate they push to the front of that
entity's `Oninteract` chain, then re-push it ten times a second because `Helper_Entitytools.Updateinteract` rebuilds the
chain from the entity's AIs on every upgrade or effect (Helper_Entitytools.cs:138-151; kit `17` §2.3). The handler copies
the hit's data and scales `Damagemulti`:

| Part | Where | What it scales |
| --- | --- | --- |
| Yossele | `Yossele/Code/12_Hooks.cs` (323 lines; `Armor` :42, `Mark` :84, `First` :244, `OnHurt` :257) | Stonehenge armour per Growth stage, Monument's 20%, Fortifying Roots' aura (30%, strongest counts, capped 90%), Easy Target (+15% taken); also Connected Roots, Vine Blockade, Monument's cleanse |
| Argo | `Argo/Code/09_Hooks.cs` (189 lines; `First` :140) | Bulkhead (a tuned tower takes 25% less), Soak (a foe in Crushing Depth takes 15% more), Pinged (SONAR, switched off) |
| Uloe | `Uloe/Code/05_Server.cs:267-288` (`UloeMark.OnHit`), `Uloe/Code/01_Util.cs:494-520` (`Front`, `Unfront`), the re-front in `UloeMarks.Tick` | Illuminate (+x% taken, three levels) |
| Mode | `RogueWavesMode/Code/16_Directions.cs:394-455` (`RWHits`) | Made of Tougher Stuff (minions take 15/25/35% less, rank III caps a hit at 25% of max health), Spring Tide rank III's 10% shrug |
| Spool | `Spool/Code/10_Cushion.cs:50-60` (`First`) | Slow Unravel (half now, half later), and the Cushion's counting (finding 5) |

**What the base game does** (read). Fie's wall (`Entity_Ai_Tower_Fiewall.cs:216-227`, `:310-333`) and Smacky's Beacon
(`Entity_Ai_Effect_Smackywall_Beacon.cs:37-53`) give a building a stat named `Resistadd` (`Style Number`,
`Display_Type Percent`, `Value_Type Variable`) with `Helper_Entitytools.Addstat`, then raise it with ordinary `Addmod`
calls. Fie's walls also put the effect upgrade `Effect_Fiewall_Regen_Upgradeobject` on the building. Its script is
eleven lines:

```csharp
// Entity_Ai_Effect_Fiewall_Regen (decompiled, whole body)
public override void Setpriority(int New) { Priority = 9; }          // before the damage AIs at 10
public override void Oninteract(Link_Interact Data) {
    if (Data.Type == "Hurt" && Data.Data.ContainsKey("Damagemulti") && Master.Statsserver.ContainsKey("Resistadd"))
        Data.Data["Damagemulti"] /= 1f + Master.Statsserver["Resistadd"].Value;
}
```

It is a real AI in the entity's `Ais`, so `Updateinteract` puts it back in the chain at priority 9 every time, with
nothing to re-push. It reads the stat live, so any `Addmod` on `Resistadd` takes effect on the next hit. A timed `Addmod`
(`duration` > 0) is removed by the engine's own coroutine (Helper_Entitytools.cs:611-654, started on `Game_Entity`, not
on the target), so a timed mark needs no table and no sweep. The engine study's catalogue already rates this script
**data-only** (`ENGINE_STUDY_2026-10-28_BEHAVIOURS.md` line 139).

**The method.**

- One pack upgrade, for example `RW_Effect_Resist_Upgradeobject`: Priority −3, empty `Costs`, `Aiscripts` = the name
  carrier `Script("Entity_Ai_Effect_Fiewall_Regen")` (kit `17` §2.2's rules for effect upgrades). It is built once in
  `Shared` so every part names the same object.
- `Resist(entity)`: `Addstat` `Resistadd` (Value 0, as Fie's code builds it) when it is missing, and apply that upgrade
  Forced. Do nothing if the entity already holds `Effect_Fiewall_Regen_Upgradeobject` (Fie's), or two copies of the script
  would divide every hit twice. Pitfall #115 already makes a second application of the pack's own upgrade a no-op.
- Each armour source becomes an `Addmod` on `Resistadd`. For a reduction of *r* the value is *r* / (1 − *r*) (30% → 0.429,
  25% → 0.333, 90% → 9). For *v* more damage taken it is −*v* / (1 + *v*) (15% → −0.130).
- **Route A, no number changes.** Each part keeps its own sum or maximum exactly as today (Yossele: armour + strongest
  aura, capped at 90%, times Easy Target) and writes the whole result into **one held modifier per entity**. This is the
  same "keep one modifier and set its value" pattern the mod already uses for Growth (`YosseleUtil.Keep`) and that
  Melting uses (kit `01` §6). The hook, its table and its re-push loop go away, and the numbers stay the same.
- **Route B, the base game's stacking.** One modifier per source. Sources then stack in the denominator as the stock
  ones do: two 30% sources give 46%, not 60%, and nothing needs a cap. This is closer to the base game but it is a
  balance change, so it is Dimitri's call.
- Timed vulnerability on foes (Easy Target, Soak, Illuminate) becomes a timed `Addmod(foe, "Resistadd", −0.13, Add, secs)`
  on a foe that got the upgrade once. The engine takes it off by itself. Unlike the stock Merchut mark Uloe tried, there
  is no `UniTask` to cancel on death, so the `OperationCanceledException` that sent Uloe to its own route
  (`Uloe/IMPLEMENTATION_NOTES.md` "Decided after round 1") cannot happen.

**What stays a hook** (no stock script does these): Connected Roots' share to the Sapling, Monument's cleanse,
Tougher Stuff rank III's per-hit cap, Spool's Slow Unravel, and Uloe's Glare (a nullified hit) and Blind-on-hit.
Finding 3's shared helper keeps one handler for them instead of five copies. With priority 9 the stat applies after
the front hook and before the damage at 10, so Connected Roots still takes its share from the full hit, as today.

**Traps found while reading** (read only; R2 and R3 check them):

- **Spring Tide's Beacon conflicts with this route.** The stock Beacon builds `Resistadd` as a *multiplier*: it adds 1
  and a Multi of `Effect_Multi` − 1 (`Entity_Ai_Effect_Smackywall_Beacon.cs:37-47`), so at Spring Tide's `Effect_Multi`
  of 1 every tower holding it carries `Resistadd` = 1. On a building that also has the Regen script, every hit would be
  divided by 2. Before finding 1, Spring Tide's health per Tide should leave the Beacon (a held `Health` Add raised on
  `ModKitTides.Tide`, about 15 lines). That also removes the "Alreadyactive" limit the mode notes at
  `16_Directions.cs:38-41`.
- **The same conflict may already be live today, through Fie.** A Fie wall tagged `Propagateadjacent`
  (`Entity_Ai_Tower_Fiewall.cs:415`) gives its resistance and the Regen upgrade to adjacent things tagged `Tower` or
  `Fiewall` (`:96-97`, `:153-176`, `:310-333`), whoever owns them. A tower holding Spring Tide next to such a wall would
  take 1 / (2 + *r*) of each hit. The base game has the same quirk for Smacky's Beacon walls beside Fie's. This is worth
  one look in game (R3) whatever is decided here. It is a candidate pitfall once seen (next free #150).
- `Resistadd` shows on the panel as a percent, Fie's way: a 30% armour reads "43%". Leave `Display_Exposed` false if
  that confuses.

**PC checks.** R1: a pack building with the upgrade and `Resistadd` 0.429 takes 70% of a known hit, before and after
another upgrade lands on it (the rebuild). R2: a tower with Spring Tide and the Regen script, after the Beacon change.
R3: today's build, Spring Tide beside a Fie wall with its adjacency upgrade (the possible live bug). R4: a foe with a
timed −0.13 takes 115% for its time, then 100%, and falls during it without an exception in `Player.log`. R5: each
vessel's self-test armour checks (`Softened`, `Crushed` and the rest) pass unchanged on route A.

### 2. The stock upgrade AI instead of four mirrors of it

**What the mod does.** Each vessel's `Content.Strip` drops `Entity_Ai_General_Upgrade` from cloned data
(`Yossele/Code/04_Content.cs:121-128`; pitfall #9 says "the building brain mirrors the upgrade AI"). Each building brain
then rebuilds it: it creates the `Upgrade` and `Upgradeoption1..4` commands, parses the JSON by hand, checks availability
and the throttle, spends the costs and applies the upgrade. It plays `Upgradeborder` and counts `Upgraded`
(`Yossele/Code/07_Building.cs:73-84, 137-182`, `Spool/Code/07_Building.cs:55-127`, `Uloe/Code/07_Building.cs:66-146`,
`Argo/Code/07_TowerBrain.cs:108-125, 923-986`).

**What the base game does** (read). `Entity_Ai_General_Upgrade.cs` is the same flow and nothing more: `Spawn` makes the
five commands, and `Command("Upgrade", json)` does the availability check, the 0.05 s throttle, `Checkresources` /
`Useresources` scaled by the `Upgrade` command's `Goalvalue`, `Helper_Entitytools.Upgrade` (Forced from the host),
`Upgradeborder` and `Activeplayerdata.Upgraded++`. It names no vessel, reads no params and hard-codes nothing, and the
mirror's own header comment says so ("mirrors exactly").

**The method.** Stop stripping it (remove the one line in each `VesselSpecific`, or list it with `Script(...)` on each
buildable). Delete the command creation and the `"Upgrade"` branch of each brain's `OnCommand`. Keep
`Master.Upgrade += OnUpgrade` and the catch-up over `Upgradelistserver` in `Begin`: the brain still hears every upgrade
through the entity's own event, which `Helper_Entitytools.Upgrade` raises (:548-551). `JsonString` and `JsonNumber`
then have no user left in Yossele (`07_Building.cs:151-152` is the only one).

**What it gains.** About 50 lines per vessel. Kit `17` §2.7 says Voice in a Bottle "removes itself from kit
buildings" because it looks for this exact component (`Entity_Ai_Effect_Artifact_Voice.cs:48-63`). With the stock AI
back, the Cardinal lowers pack upgrade prices as it does for base buildings, which is base-game parity. Its counter
also shows.

**Risks.** Spool's Thread Head is a unit with an upgrade tree. Whether a unit's data tolerates the stock AI is read
only, though Boutman's fish carry upgrade trees the same way. A brain must not create the five commands as well, or
they exist twice. Pitfall #9's text changes.

**PC checks.** G1: buy every tier on one building of each vessel; the panel, prices, refunds and the end screen's
Upgraded count match today. G2: a client buys one (it goes through `Command_ServerRpc` as before). G3: Voice in a
Bottle held, a pack building's upgrade costs 80%.

### 3. One shared scaffold instead of four copies

**What the mod does.** Each vessel carries its own `01_Util`, `03_Assets` and `04_Content`, 1,650-1,870 lines per
vessel. After vessel names are blanked, 200-246 of Yossele's 296 code lines in `01_Util` are also in each of the
others; for `03_Assets` it is 205-231 of 262, and for `04_Content` 341-405 of 583. Forty helpers appear in all four
Utils under the same names (`Load`, `Clone`, `Retag`, `Clip`, `Stat`, `Speed`, `Debuff`/`RemoveDebuff`/`ForgetLooks`,
`Hurt`, `Heal`, `Vis`, `Costs`, `Params`, `JsonString`, `JsonNumber`, `Script`, `Skin`, `Later`, `Faults`...), and
forty more in all four `Content`/`Assets` (`Strip`, `Register`, `BuildTower`, `BuildHero`, `Sheet`, `Frames`, `Tooltip`,
`AddIcon`, `Model`...). The foes part has its own upgrade factory (`RogueWavesFoes/Code/00_RogueWavesFoesMod.cs:143-200`)
and the mode another (`RWContent.Upgrade`).

**Why it was so.** Kit `05` §0 says to keep and rename a per-vessel Util: each vessel used to ship as its own mod.
The pack now ships as one bundle (`docs/PACK.md`), and every part's build already compiles `Shared` in with `ModKit`
renamed to the part's prefix (`TDS_Mod/Tools/cloud-check/README.md`). A `ModKitUtil`, `ModKitContent` and
`ModKitAssets` in `Shared` would therefore still be one private copy per part at run time, with one source to fix.
Release 1 (Yossele alone) is unaffected, because Yossele still carries the kit in its own build.

**The method.** Move the identical helpers to `Shared` with the part's name passed where an id is built (pitfall #12:
every id keeps its part's name). Keep each part's own differences as overloads or parameters. Add one `ModKitHits` with a
single front handler per entity, a list of registered scalers and one re-push loop, which replaces the five `First`
copies for whatever finding 1 leaves.

**Risk.** For play, nil: the code is moved, not changed. For the build, medium: kit `README.md` and the file headers
need a version bump, `TDS_Mod/ModSource/Shared` and `TDS_Vessel_Creator/kit/Shared` stay equal, and the new kit files
must not hold the prefix inside strings (the kit 1.7 note in `ModKitFocus.cs`). Check: cloud compile of every part and
`check_editor.sh`, then one PC round of the four self-tests (expect 133, 142, 173, 148, no failures). Best done when the
next build round opens these files anyway (the unbuilt upgrades of scopes 07-10).

### 4. Vine Blockade's barrier as the stock `Healthover` layer

**Today.** `YosseleHooks.Shield` and the soak step of `OnHurt` (`Yossele/Code/12_Hooks.cs:54-60`, the `shield` block
in `OnHurt`) absorb hits up to 30% of max health each Tide. The mod draws its own look and its own "break" cue.

**Base game** (read). `Helper_Damagetools.Dodamage` already has a barrier layer, `Healthover`, taken before `Health`
(Helper_Damagetools.cs:28-45). The HUD adds it to the health bar (`Game_Ui.cs:88`, `:832-836`), and Sno Sno's Ice Brick
uses it (`Entity_Ai_Tower_Snosnowall.cs:74-76`). The method: `Addstat` `Healthover` on the stones and set a held Base
modifier to 30% of max health when each Tide begins.

**The difference** to accept or refuse: a hit bigger than what is left of `Healthover` empties it and passes **in
full** (Dodamage's overflow branch), where Vine Blockade today soaks what it can. That is a design call. It removes the
shield table and the soak arithmetic, and players see the barrier on the bar.

### 5. Pin Cushion: count the game's damage, prick the stock way

**Today.** `Spool/Code/10_Cushion.cs` keeps a hook in front for five things: Reflect (melee attackers take a share
back), Pin Barrage (ranged too), Spin (a Thread per `Spin` damage taken), Loose Stuffing (a Mangled per 40 taken) and
Slow Unravel (half now, half later).

- **Spin and Loose Stuffing** only count damage. `Dodamage` adds every applied hit to `Master.Damagetaken`
  (Helper_Damagetools.cs:21-70), so the brain's `Tick` can read that counter's rise instead of hooking each hit, the
  way Vampire Dentures reads `Damagedone` (`RogueWavesMode/Code/17_Cardinals.cs:343-371`). It counts what was actually
  taken (after armour), which is what "damage taken" means on the sheet. Slow Unravel's later halves land in the
  counter too (today the hook counts each hit once, in full, when it lands), so the same total arrives a few seconds
  later; the sheet's "doubles thread creation" stays its own number on `Spin`.
- **Reflect and Pin Barrage** could be the stock `Entity_Ai_Effect_Smackywall_Bramble` that Barnacle Coat already uses
  (`16_Directions.cs:32`). But Bramble pricks a **flat** `Effect_Add` at any attacker, melee or ranged
  (`Entity_Ai_Effect_Smackywall_Bramble.cs`), not a share of the hit. That is a design change for Spool's sheet, so it
  is Dimitri's call.
- **Slow Unravel** has no stock form and stays a hook.

### 6. Argo's "Slowed" read from the foe itself

**Today.** `ArgoStatus` (`Argo/Code/01_Util.cs:611-666`) keeps three tables of foe → until for Slowed, Pinned and Wet,
which every Argo slow writes. The Scatter Cannon's +50% reads Slowed or Pinned.

**Base game.** A slow *is* a negative `Speed` modifier on the foe (kit `17` §2.1). "Slowed" can be read from the foe
itself: any live `Modifiers["Speed"]` entry with `Value_Multi` < 0 or an Override. That drops the Slowed table and its
bookkeeping, and the bonus then also fires on Sno Sno's, Fie's and Yossele's slows. That is closer to how a player reads
"a slowed foe", but it is a balance change. Pinned and Wet stay, because they are Argo's own states.

### 7. Uloe's dead stock-mark branch

`UloeData.StockMarks` is a `const false` (`Uloe/Code/02_Data.cs:101`), chosen after Uloe's round 1. The stock branch of
`UloeMarks.Blind` and `Illuminate`, `TakeOffLit`'s Merchut clean-up and the `UloeMarks.Stock` switch
(`Uloe/Code/05_Server.cs:292-385`) therefore never run in a release build. Delete them (about 30 lines), or let finding 1
replace Illuminate's hook outright. No behaviour change; check by compiling and running Uloe's self-test.

### 8. The Argo review of 2026-10-12, still open

None of its cuts has been made: SONAR is still compiled behind `SonarEnabled = false` (`Argo/Code/02_Data.cs:18`, 19
references in `06_HeroBrain.cs`). The 1.4 leftovers are still there (`Stagger_Base` at `07_TowerBrain.cs:403`,
`Argo_Bleed` at `10_Client.cs:131`, `AftershockEvery` at `02_Data.cs:157`). The self-test still carries its own
`SpeedFoe`, `Enemy` and `Core` (`99_SelfTest.cs:344, 374, 528`), and Bulkhead still keeps a list of Nauts per tower
(`09_Hooks.cs:12`). Its plan (`research/ARGO_CODE_REVIEW_2026-10-12.md`, "If told to go ahead") stands as written,
about 430 lines. Its finding 5 is folded into finding 1 here. SONAR still waits on Dimitri's word (PLAN records keeping
it until he decides).

### 9. Brain timers on the game's tick (not worth doing alone)

Every brain is a `MonoBehaviour` whose `Update` returns on clients and throttles itself with `Time.time`. There are 300
such reads across the parts, for example `nextCheck = Time.time + 0.5f`. The game drives its own AIs from three public
delegates on the map object, `Game_Mapvars.Tick`, `Slowtick` and `Reallyslowtick` (0.05, 0.2 and 1 s, host only, on
`Time.fixedTime`; Game_Mapvars.cs:116-160). A brain could subscribe to them in `Begin` and lose its server check and
its throttles, and the per-map object would drop the subscriptions with the map. **But** one exception in a subscriber
stops the rest of that delegate call for the game's own AIs too. Every handler would need the try/catch the brains
already wrap their `Update` in, and the gain is a few lines per brain. The same goes for `ModKitTides` listening to
`Game_Mapvars.Wave.OnValueChanged` (the Beacon does) instead of polling: the poll is proven, so keep it.

### 10. Yossele's regeneration as the stock Aegis (not recommended now)

`YosseleHeroBrain.Regen` (`Yossele/Code/06_HeroBrain.cs:96-108`) heals 1 a second while hurt. The base game's only
generic regeneration is the elite script `Entity_Ai_Enemy_Aegis`, data-only. It heals the holder's `Heal` stat every
second once `Healtime` seconds have passed since its last hit. Its numbers would be stats that run upgrades can reach.
But it heals and plays `Debuff_Aegisheal` every second even at full health, and stock looks load per map (pitfall
#114), so Yossele would sparkle forever or show nothing. Listed so nobody re-derives it. The regen-after-no-hit rule is
the base game's, if the sheet ever wants that.

## What looks reimplemented but should stay

| Piece | Why it stays |
| --- | --- |
| Brains as `MonoBehaviour`s added in `Onspawn` | the engine attaches AIs with `Type.GetType` from its own assembly and prunes every AI no upgrade lists (pitfall #8; Helper_Entitytools.cs:540-547) |
| Sending debuff looks past the game's throttle (`Debuff` with its "already showing" table, in each vessel) | the game's `Vfxdebuffcheck` drops sends while 50 are in flight (pitfall #95); keep it, but once, in `Shared` (finding 3) |
| Blind as a timed `Addmod` on `Damage` (Uloe), not the stock Barrel_Boost | Barrel_Boost waits on a `UniTask` and logs an exception for every foe that falls while Blinded (`Uloe/IMPLEMENTATION_NOTES.md`) |
| Spores, vines, Deep Water networks, Light, Thread drops, Growth, Fertilize | new mechanics (the engine study's rung D); no stock script does them |
| Pack damage over time dealt by the pack's own `Hurt` | the stock poison and burn hurt the holder from itself (`Entity_Ai_Effect_Boutmanfish_Puffer.cs`, `Snosnoheater_Scorch.cs`), so the kill is not credited to the vessel |
| The Directions' own owner-filtered spawn hook | the stock `Teamapply` reaches every vessel's buildings (engine study N13, pitfall #118) |
| Numbered copies of a Cardinal's upgrades for stacks | the same upgrade twice does nothing (pitfall #115) |
| The rejoin's seat copy four times a second | a rejoin brings a new, empty `Tds_Clientinfo`; the game hands back things and subowners only (`Game_Players.cs:24-51`) |
| The foes part scanning for its modifier upgrades | the modifiers can't carry the part's code (pitfall #8); Dazzling already uses stock scripts |
| Build-slot limits (the Tree, the Miner's Base, the Yarn Stash) by removing and re-adding a build option | the base game's own pattern (`Entity_Ai_Hero_Boutman.Checkbouthouses`) |
| Re-pushing the hit hooks that are left after finding 1 | still needed: an outside delegate is dropped on every `Updateinteract` (kit `05` §9) |

## Suggested order, if Dimitri says go

1. **Deletions with no behaviour change:** findings 7 and 8 (8's SONAR only at his word). Check: cloud compile, then
   each vessel's self-test in the next PC round.
2. **The shared scaffold**, finding 3, with `ModKitHits`. Check: cloud compile of all parts and editors, then one PC round
   of the four self-tests.
3. **The stock upgrade AI**, finding 2. Check: G1-G3.
4. **`Resistadd`**, finding 1, route A. Move Spring Tide off the Beacon first, and run R3 on today's build anyway. Check:
   R1-R5.
5. **The design calls** (4, 5, 6, and route B of 1) only at his word, each with the balance simulator re-run if it
   touches numbers.

Each step is its own PR, so a failed PC check undoes one step, not the lot.

## Not covered

The Unity editor code (`TDS_Mod/Assets/*Mod/Editor`), the self-tests' insides beyond what finding 8 repeats, `Riptide`
and `RogueLab`, and data values that live in the game's bundles on the PC. The cloud compile of this commit: Yossele,
Spool, Argo, Uloe and RogueWavesFoes compile clean. RogueWavesMode shows the two stub gaps the tidy check already
listed (`TMPro.TextAlignmentOptions` and `GridLayoutGroup.Axis` missing from the cloud stubs, `11_Dialog.cs:191, 200`),
which the PC compiles. This page changes no code, so nothing it says has been run.
