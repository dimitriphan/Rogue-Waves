# Research gaps: what the expansion still needs to learn about the game

Engine facts the scope (`docs/SCOPE_v0.txt`) depends on that Phase 1 left open. Listed 2026-10-08, against
`TDS_Vessel_Creator/docs/13`, `14`, `15`, RogueLab round 1, Riptide's notes and Argo's six PC rounds; **answered from
the decompiled game 2026-10-09** (the facts, with `file:line`, are in kit `docs/15` §14). This is the research half of
`docs/SCOPE_GAPS.md`, which lists the **design** gaps; nothing here repeats it. Sonar is out of scope (user).
**RogueLab rounds 2 and 3 ran on the PC 2026-10-09** (15 pass, 1 fail; then 16 pass, 0 fail; results in kit `docs/15`
§15, raw lines in `TDS_Vessel_Creator/reference/roguelab_round2.txt` and `roguelab_round3.txt`).

**Status:** **answered** = read from the game's code or seen on the PC, enough to design on; **online** = needs a second
player; **by hand** = Dimitri plays it. Nothing waits on the PC any more.

## Summary

Of the 18 items, 16 are answered (A5 and B5 keep a half for the online evening), 1 needs an online evening with a
friend and 1 waits on a scope decision; the F-key check by hand is still open too. Three answers change plans:
**Directions can't be per player on the core** (A1), **nothing in the game scales foes for more players or harder
maps** (A3), and **a friend who drops can't rejoin a running map** (A5). The PC rounds added one rule, **bring the
choice dialog's menu up before opening it** (A6), and one map to never offer, Special_Custom (A4).

## A. Before Phase 5 designs the mode

| # | Gap | Answer | Status |
| --- | --- | --- | --- |
| A1 | Per-player Directions | The core is the team's: it spawns every player's vessel and has no owner; `Heroapply` reaches every vessel and `Towerapply` every new tower. A Direction must go on the player's vessel, re-granted from host state at each spawn, and tower Directions through the mod's own spawn hook filtered by owner. **`ModKitDirections` (kit 1.6) needs that change** before Phase 5 uses it (§14.1) | answered |
| A2 | Fund Transfer's route | Aura is per player. The host moves it with the public `Useresources` and `Giveresources(list, owner)`, with the gain multiplier held at 1 (Cardinals change it). The stock Money Bag Cardinal already donates aura, lossily and by a thrown bag; its code is private, so it's prior art, not a reusable piece. Which aura moves stays Dimitri's call (scope `! NEEDS !`) (§14.2) | answered |
| A3 | Difficulty levers | The game has none to borrow: no scaling by player count or map; a map's `Difficulty` is only a label. Levers a mod has: the Tide table (counts, intervals, elites, extra entries; proven live), the game's **easier** setting's upgrade forced on the core (`Special_Treated`: an extra twice the mode's start aura, enemy drops ×4, +30 s per Tide, +200 core health; there is no harder setting), the aura gain multiplier, and modifiers of its own. The other modes' tables are logged: Tsunami starts at 500 aura with four long Tides, Drought has 20 short ones (§14.3, §15) | answered |
| A4 | The map pool | Any map takes any mode; the coupling is data (§14.4). **All 45 base maps played clean** in the mod's mode (vessel, cores, no warnings, 14-20 s loads), the Final map and the δ maps included. **Special_Custom hangs the load: never offer it.** Each map's difficulty label (1-9) is logged for a vote (§15) | answered |
| A5 | Drop and rejoin | Key run state by `Sessionid` (stable on a rejoin; RogueLab and Riptide already do). A drop despawns nothing; teammates can command the dropped player's things. **A running map refuses joins** (closed lobby), and a rejoin comes back with no aura. So a dropped friend rejoins only between runs, unless the host reopens the lobby between maps (§14.5) | answered; reopening between maps: online |
| A6 | A shop and a vote showing more than three things | The dialog takes any number of entries, two per row, with title, colour, image and a hover description; no price or disabled field, so prices go in the name and the host refuses a short purse. The shop is the vessel-command route Riptide proved; no building needed (§14.6). Round 3: with its menu brought up first (`Bl_Menus.Changemenu`, 1.3 s) it took eight cards and returned the clicked one; the stock two-per-row grid pushes rows 3-4 off the screen, four per row fits all eight (§15) | answered |
| A7 | A save key surviving a restart | Proven: round 1's launch counter read 2 on the next launch (§15) | answered |
| A8 | Online, for any mode piece | Unchanged: nothing in the mode has run on a client yet (`docs/15` §11 step 11) | online |
| A9 | One bundle, several `Default` prefabs | Proven: a second prefab with its own code started from the same bundle (§15) | answered |

## B. Before each vessel's design gate

| # | Gap | Answer | Status |
| --- | --- | --- | --- |
| B1 | Light received (Uloe) | The game has no gameplay light; lighting is rendering only. Light is a number the mod sums from sources in range with the game's own area query, on a slow tick. Feasible; its cost goes with B5's numbers (§14.7) | answered |
| B2 | Changing damage as it lands (Pin Cushion, Illuminated, Blinded, Mirror) | Every hit passes through the target's AIs in priority order before the target's own AI applies it, carrying the attacker: an AI can scale or cancel it (Fie's wall does), hurt the attacker back (Smacky's spiked wall does) or hold half for later. All four mechanics are go (§14.7) | answered |
| B3 | Ground pickups (aura fruit, Thread) | The game's own aura pickup is the pattern: it checks for a nearby same-team unit, pays its owner and expires. Enemy drops never touch the ground (paid to everyone at death), so Thread drops are the pack's own pickups; Boutman's fish carry nothing (§14.7) | answered |
| B4 | Directions that change a stock vessel | An upgrade adds AIs (it can't remove one by data), and an added AI's code can change what a tower does. Possible if the catalogue wants it (a SCOPE_GAPS decision) (§14.7) | answered |
| B5 | A swarm's cost | On the host, two runs: 7.0-8.8 ms a frame with none, 10.4-12.1 ms with 200 extra units (82-97 fps); 200 units cost about 3.5 ms (§15) | answered (host); online (client) |

## C. Enemies (Phase 4), from `docs/13` §10

| # | Gap | Answer | Status |
| --- | --- | --- | --- |
| C1 | Explode and Weakened elite values | Molten: health ×3, speed ×0.15, on drowning 10 damage to 5 targets in 2.2; Melting: -0.001 drains for 60 s. They ship in the Tsunami and Final groups (`docs/13` §10) | answered |
| C2 | The other modes' Tide tables | Logged: Storm 15 Tides, Drought 20, Tsunami 4, Final 1, Tutorial 3 (`docs/13` §10, TIDE2 lines) | answered |
| C3 | Enemy sprite format and elite skins | Needed only for new enemy types or skins (both `! NEEDS !` in the scope) | waits on that decision |
| C4 | Enemy pages in the journal | Journal pages are data assets; no code lists enemies, so the pack's foe pages are its own, as vessels' are (`docs/06`) | answered |

Flying waits for a flying enemy.

## D. Pieces the scope doesn't ask for

New maps, a boss or final fight, music, localization, tutorial tips and vessel skins (`docs/EXPANSION_SCOPE.md` rows 4,
5, 8, 12, 13, 14) need research only if Dimitri wants them.

## E. What is left, in order

1. **By hand:** the F-key bug with Boutman (`docs/15` §11 step 8; the mod could not reproduce it in three rounds,
   because the mod-spawned Boutbro is not the player's): let Boutbro spawn, die, press F after respawning.
2. **One online evening** with a friend: A5 (reopening the lobby between maps), A8, B5's client half.
3. **Kit 1.7** at Phase 5's start: `ModKitDirections` grants per player on the vessel (A1).
4. **C3** when the scope decides on new enemy types or elite skins.
