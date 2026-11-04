# Code simplification, built: what changed, what did not, what the PC must check (2026-11-04)

Dimitri (2026-11-04 19:40Z): "Begin development of Code Simplification, then add the tests to the testing thread." This
builds the findings of `CODE_SIMPLIFICATION_2026-11-04.md` in its suggested order, with one rule: **nothing a player
sees or any number changes.** Save keys, ids and upgrade names are untouched. Everything below **compiled**
(cloud-check, every part and build flavour, the four editor builders); **nothing has run in game yet**. Section 4 is the
list the PC round runs.

## 1. What was built

| Finding | Change | Lines |
| --- | --- | --- |
| 7 | Uloe: the stock Blind/Illuminate route (`StockMarks`, `UloeMarks.Stock`, six `Uloe_Blind<n>`/`Uloe_Lit<n>` upgrades, the self-test's route loop) deleted. It could not run: `StockMarks` was a `const false` | about -90 |
| 8 (without SONAR) | Argo: the 1.4 leftovers (`Stagger_Base`, `LastGusher`, `Argo_Bleed`, `AftershockEvery`, their journal labels and effect specs) and counters nothing read; Bulkhead keeps one tuner per tower instead of an unread list of Nauts. **SONAR stays** (it waits on Dimitri's word) | about -20 |
| 2 | Pack buildings (all four vessels, and Spool's Thread Head) carry the game's own `Entity_Ai_General_Upgrade` again. It runs the upgrade panel: commands, throttle, costs × `Goalvalue`, `Upgradeborder`, the owner's Upgraded count. The four hand-written mirrors of it are gone; each brain ignores the "Upgrade" command and still hears every upgrade through `Master.Upgrade`. Argo's upgrade sound now plays from that event | about -170 |
| 3 | Kit 1.8: `Shared/ModKitUtil.cs` and `Shared/ModKitAssets.cs`. The helpers each vessel copied (database, beings, stats, queries, hits, debuff looks, costs, json, script carriers, content helpers) and the asset reader (library, slicing, Spriteobjects, the boat's Worldsprite, icons, sounds, effects) live once, bodies unchanged. Five of the mode's own copies too. The hit-chain code every part's hooks used (Yossele, Argo, Spool's Pin Cushion, Uloe, the mode's Directions) is one `ModKitUtil.Front` | about -1,920 |

In all: `TDS_Mod/ModSource` went from 4,917 lines removed against 2,719 added (-2,198). The four `01_Util.cs` went
from 431-666 lines to 42-397, the four `03_Assets.cs` from 384-438 to 102-218.

What each part keeps of its own (by design, `kit/README.md`): ids with its name (`Retag`, `Clone`), owner warnings
(`Warn`), `Later`, its hero's and units' sheets, icons and palette, tower material and sound levels, and Argo's own
`ById`, `Costs` and `Params` (they treat a 0 id and a bad number differently from the others; merging them would change
behaviour).

## 2. What was not built, and why

**Finding 1 (armour as the stock `Resistadd` stat + `Fiewall_Regen`)**: not built. Reading it again for "no number
changes", route A still changes numbers in four places unless other work goes first:

1. **The stock script edits the hit in place.** `Fiewall_Regen` divides `Data["Damagemulti"]` inside the dictionary it
   is handed, and `Interacthurt` hands every target the caller's own dictionary (pitfall #30). When one dictionary hits
   several things (a splash, a volley), each target holding the script divides it again for the next. The mod's hooks
   copy the dictionary first for exactly this reason. A foe marked by Easy Target, Soak or Illuminate inside a splash
   would change the damage of the foes hit after it.
2. **Order inside one hit.** Yossele's single handler applies armour, then lets Vine Blockade soak what is left. With
   armour moved to the script at priority 9, the soak (still a hook, run first) would take its share of the unarmoured
   hit: more soak, less wall health lost.
3. **Two copies of the same script.** A Fie wall beside a pack building adds its own `Effect_Fiewall_Regen_Upgradeobject`
   (it checks its own name, not the script), so the building would divide every hit twice. And when that upgrade
   leaves, the engine prunes AIs by type name, which also takes the pack's copy away until something re-applies it.
4. **Spring Tide's Beacon** gives every tower holding it `Resistadd` = 1; with the script, every hit halves. Spring Tide
   would have to leave the Beacon first (the report's own precondition).

Each of these needs its own change and a PC check first. Instead, the five copies of the front-of-chain code are now one
(finding 3's `ModKitHits` idea, as `ModKitUtil.Front`), which was the part of finding 1 that could be done safely. R3
(below) is run anyway: it is a possible live bug in today's build.

**Also not built:** the near-copies in each `04_Content.cs` (`Register`, `BuildTower`, `BuildHero`, `Tooltip`,
`AddIcon`...). They take each vessel's own data types (`YosseleData.Build`, `SpoolData.Build`...), so sharing them
needs a shared data type, a redesign rather than a move. The foes' and mode's upgrade factories stay too. Findings 4, 5
and 6 and route B of 1 are design calls left to Dimitri; 9 and 10 the report did not recommend. The review's own
self-test clean-up (moving `SpeedFoe`/`Enemy`/`Core` into `ModKitTest`) was left: it touches the test harness, and the
harness is what checks everything else here. The Mummer template keeps its own helpers (scripts copy it by name).

## 3. Checked here

- `TDS_Mod/Tools/cloud-check/check_mod.py` on every part and flavour: Yossele (release, self-test, measure), Spool
  (release, self-test, every tuning define), Argo, Uloe, RogueWavesFoes, RogueWavesMode (release, self-test, the
  GAPS/FEEL/CHARTLOOK build), the Mummer template against `kit/Shared`: **0 errors**. The mode's 1-2
  warnings are the same as on the base branch.
- `check_editor.sh` on the four editor builders: clean.
- `ModSource/Shared` = `TDS_Vessel_Creator/kit/Shared` (kit 1.8). No new kit file holds the prefix inside a string
  (the kit 1.7 rule); `ModKitAssets`' default name "ModKit" becomes the part's prefix, which is harmless.
- Self-test check counts: unchanged for every part except Uloe, which lost the three checks of the deleted route.

## 4. What the PC round must check

Build and export **every part from this branch together** (kit 1.7 → 1.8: a part left on an older build logs "parts
disagree"). Nothing here needs Dimitri's eyes except where marked.

| # | Check | Expected |
| --- | --- | --- |
| T0 | Boot (pack or each part) | each part logs `[RogueWaves:<Part>] v0.1.0-dev kit 1.8`, no "parts disagree", no new `missing sheet` / `missing model` lines (they still carry `[<Vessel>]`) |
| T1 | Self-tests | Yossele 173/0, Spool 133/0, Argo 142/0, **Uloe 145/0** (148 less the three deleted checks), mode full flow 148/0 with every part on, foes as before. Any FAIL is this PR's |
| G1 | Upgrades by hand, one building of each vessel and Spool's Thread Head | the panel lists the same options; each buy charges today's price, flashes `Upgradeborder` once, applies its effect (range, rate...) and adds **1** to the end screen's Upgraded count (not 2); selling refunds as before |
| G2 | A client buys an upgrade (two-player round, second game copy) | it lands on the host and both screens show it |
| G3 | Voice in a Bottle held, a pack building (**new**: it used to remove itself from pack buildings) | the artifact stays on the building, its counter shows, and the next upgrade costs 80% (as Long Bow, Blue 25 → 20) |
| G4 | Argo's towers | one upgrade sound per buy (it now plays from the upgrade event); FLOOD pools still have no Upgrade command (a self-test check) |
| R3 | **Today's build** (any build, this PR does not touch it): a tower holding Spring Tide next to a Fie wall with its adjacency upgrade | does the tower take about half of each hit? If yes, it is a live bug (Beacon `Resistadd` = 1 meets Fie's Regen); write it down as pitfall #150 |
| A1 | Argo Bulkhead | a tuned tower takes 25% less; retune to another tower and the armour moves; untune and it ends |
| U1 | Uloe Blind and Illuminate | Blind lowers a foe's damage for its time, Illuminate raises the damage it takes; both end; a foe dying while marked logs no exception |
| H1 | Hit hooks (all now through `ModKitUtil.Front`) | Yossele's armour and Easy Target, Argo's Soak, Spool's Pin Cushion and Slow Unravel, the mode's Made of Tougher Stuff and Spring Tide III: the self-tests' checks for them pass unchanged |
| L1 | Looks and sounds (by eye or the self-test shots) | every vessel's hero animations, units, boat sprite on character select, icons, effects and sound loudness as before |
