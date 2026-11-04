# Research gaps: what the expansion still needs to learn about the game

Engine facts the scope (`docs/scope/`) depends on that Phase 1 left open. Listed 2026-10-08, against
`TDS_Vessel_Creator/docs/13`, `14`, `15`, RogueLab round 1, Riptide's notes and Argo's six PC rounds; **answered from
the decompiled game 2026-10-09** (the facts, with `file:line`, are in kit `docs/15` §14). This is the research half of
`docs/reports/SCOPE_GAPS.md`, which lists the **design** gaps; nothing here repeats it. Sonar is out of scope (user).
**RogueLab rounds 2 and 3 ran on the PC 2026-10-09** (15 pass, 1 fail; then 16 pass, 0 fail; results in kit `docs/15`
§15, raw lines in `TDS_Vessel_Creator/reference/roguelab_round2.txt` and `roguelab_round3.txt`).

**Status:** **answered** = read from the game's code or seen on the PC, enough to design on; **online** = needs a second
player; **by hand** = Dimitri plays it. Nothing waits on the PC any more.

## Summary

Of the 18 items, 17 are answered (A5 and B5 keep a half for the online evening; C3's answer waits on the scope's new
enemy types) and 1 needs an online evening with a friend (A8); the F-key check by hand is still open too. Three
answers change plans: **Directions can't be per player on the core** (A1), **nothing in the game scales foes for more
players or harder maps** (A3), and **a friend who drops can't rejoin a running map** in the base game (A5; the pack
now reopens it, `docs/mode/REJOIN.md`). The PC rounds added one rule, **bring the choice dialog's menu up before
opening it** (A6), and one map to never offer, Special_Custom (A4).

## A. Before Phase 5 designs the mode

| # | Gap | Answer | Status |
| --- | --- | --- | --- |
| A1 | Per-player Directions | The core is the team's: it spawns every player's vessel and has no owner; `Heroapply` reaches every vessel and `Towerapply` every new tower. A Direction must go on the player's vessel, re-granted from host state at each map's first spawn (a respawn copies the vessel's upgrades), and tower Directions through the mod's own spawn hook filtered by owner. **`ModKitDirections` (kit 1.6) needs that change** before Phase 5 uses it (§14.1) | answered |
| A2 | Fund Transfer's route | Aura is per player. The host moves it with the public `Useresources` and `Giveresources(list, owner)`, with the gain multiplier held at 1 (Cardinals change it). The stock Money Bag Cardinal already donates aura, lossily and by a thrown bag; its code is private, so it's prior art, not a reusable piece. Dimitri settled it: base aura only (`docs/scope/06`) (§14.2) | answered |
| A3 | Difficulty levers | The game has none to borrow: no scaling by player count or map; a map's `Difficulty` is only a label. Levers a mod has: the Tide table (counts, intervals, elites, extra entries; proven live), the game's **easier** setting's upgrade forced on the core (`Special_Treated`: an extra twice the mode's start aura, enemy drops ×4, +30 s per Tide, +200 core health; there is no harder setting), the aura gain multiplier, and modifiers of its own. The other modes' tables are logged: Tsunami starts at 500 aura with four large Tides and long breaks (240 s, then 180 s), Drought has 20 short ones (120 s) (§14.3, §15) | answered |
| A4 | The map pool | Any map takes any mode; the coupling is data (§14.4). **All 45 base maps played clean** in the mod's mode (vessel, cores, no warnings, 14-20 s loads), the Final map and the δ maps included. **Special_Custom hangs the load: never offer it.** Each map's difficulty label (1-9) is logged for a vote (§15) | answered |
| A5 | Drop and rejoin | Key run state by `Sessionid` (stable on a rejoin; RogueLab and Riptide already do). A drop despawns nothing; teammates can command the dropped player's things. **A running map refuses joins** (closed lobby), and a rejoin comes back with no aura. So a dropped friend rejoins only between runs, unless the host reopens the lobby between maps (§14.5). **2026-10-11:** a mod can reopen it mid-map and a late joiner catches up; built, solo PC round 6 passed 47/0, `docs/mode/REJOIN.md` | answered; rejoin built, needs online (`REJOIN.md` §6) |
| A6 | A shop and a vote showing more than three things | The dialog takes any number of entries, two per row, with title, colour, image and a hover description; no price or disabled field, so prices go in the name and the host refuses a short purse. The shop is the vessel-command route Riptide proved; no building needed (§14.6). Round 3: with its menu brought up first (`Bl_Menus.Changemenu`, 1.3 s) it took eight cards and returned the clicked one; the stock two-per-row grid pushes rows 3-4 off the screen, four per row fits all eight (§15) | answered |
| A7 | A save key surviving a restart | Proven: round 1's launch counter read 2 on the next launch (§15) | answered |
| A8 | Online, for any mode piece | Unchanged: nothing in the mode has run on a client yet (`docs/15` §11 step 11) | online |
| A9 | One bundle, several `Default` prefabs | Proven: a second prefab with its own code started from the same bundle (§15) | answered |

## B. Before each vessel's design gate

| # | Gap | Answer | Status |
| --- | --- | --- | --- |
| B1 | Light received (Uloe) | The game has no gameplay light; lighting is rendering only. Light is a number the mod sums from sources in range with the game's own area query, on a slow tick. Feasible; its cost goes with B5's numbers (§14.7) | answered |
| B2 | Changing damage as it lands (Pin Cushion, Illuminated, Blinded, Mirror) | Every hit passes through the target's AIs in priority order before the target's own AI applies it, carrying the attacker: an AI can scale or cancel it (Fie's wall does), hurt the attacker back (Smacky's spiked wall does) or hold half for later. A shot's hit names its shooter as the attacker, not the shot (Projectile_Master.cs:316, 394; read 2026-10-09), so a reflect reaches ranged foes too (Pin Cushion's later upgrade). All four mechanics are go (§14.7) | answered |
| B3 | Ground pickups (aura fruit, Thread) | The game's own aura pickup is the pattern: it checks for a nearby same-team unit, pays its owner and expires. Enemy drops never touch the ground (paid to everyone at death), so Thread drops are the pack's own pickups; Boutman's fish carry nothing (§14.7) | answered |
| B4 | Directions that change a stock vessel | An upgrade adds AIs (it can't remove one by data), and an added AI's code can change what a tower does. Possible if the catalogue wants it (a SCOPE_GAPS decision) (§14.7) | answered |
| B5 | A swarm's cost | On the host, two runs: 7.0-8.8 ms a frame with none, 10.4-12.1 ms with 200 extra units (82-97 fps); 200 units cost about 3.5 ms (§15) | answered (host); online (client) |

## C. Enemies (Phase 4), from `docs/13` §10

| # | Gap | Answer | Status |
| --- | --- | --- | --- |
| C1 | Explode and Weakened elite values | Molten: health ×4, speed ×1.15 (`Multi` params add to 1: 3 and 0.15), on drowning 10 damage to 5 targets in 2.2; Melting: -0.001 drains for 60 s. They ship in the Tsunami and Final groups (`docs/13` §10) | answered |
| C2 | The other modes' Tide tables | Logged: Storm 15 Tides, Drought 20, Tsunami 4, Final 1, Tutorial 3 (`docs/13` §10, TIDE2 lines) | answered |
| C3 | Enemy sprite format and elite skins | Needed only for new enemy types or skins (both `! NEEDS !` in the scope) | **answered** 2026-10-15 from the game's own sheets (kit `docs/18` §2, §5); new types still wait on the scope |
| C4 | Enemy pages in the journal | Journal pages are data assets; no code lists enemies, so the pack's foe pages are its own, as vessels' are (`docs/06`) | answered |

Flying (`docs/13` §10 Q7) is read from the game's code since 2026-10-14 (kit `docs/24` §2); it matters only if the
pack adds a flying enemy.

## D. Pieces the scope doesn't ask for

New maps, a boss or final fight, music, localization, tutorial tips and vessel skins (`docs/EXPANSION_SCOPE.md` rows 4,
5, 8, 12, 13, 14) need research only if Dimitri wants them.

## E. What is left, in order

1. **By hand:** the F-key bug with Boutman (`docs/15` §11 step 8; the mod could not reproduce it in three rounds,
   because the mod-spawned Boutbro is not the player's): let Boutbro spawn, die, press F after respawning.
   **2026-11-04:** the real death was run through the game's own respawn: F selects `Hero_Boutman`, no fault found (`docs/reports/TEST_ROUND_2026-11-04.md`). A person pressing it stays open only as an eye check.
2. **One online evening** with a friend: A5 (reopening the lobby between maps), A8, B5's client half, the client
   checks `docs/reports/MECHANICS_CHECK.md` §3 lists (a Fund Transfer between two players among them; its moves are proven
   solo), the real drop and rejoin (`docs/mode/REJOIN.md` §6, O1-O8), the sea chart on a client (`docs/mode/SEA_CHART.md` C7),
   a friend joining the host's boat after the main-menu button, and Quickening's `Time_Scale` and Rising Water's
   flood on a client (kit `docs/16` §9 item 4). Kit `docs/19` §8 (N1-N14) is the evening's full list and extends
   this one.
   **2026-11-04:** all of this that isn't Steam or a real network ran with a real second player on the PC, and passed after one rejoin fix (kit `docs/19` §8, `docs/reports/TEST_ROUND_2026-11-04.md`). What is left of the evening: Steam's Join Game and lobby (A5, A8), a real network's delay and drop, a person's eye.
3. **Kit 1.7** at Phase 5's start: `ModKitDirections` grants per player on the vessel (A1). A respawn keeping a
   granted −3 upgrade was seen in RogueLab round 7 (2026-10-12, `docs/reports/BASE_VESSEL_CHECKS_2026-10-12.md`).
4. **C3** when the scope decides on new enemy types (modifiers show as an effect, not a skin: `docs/scope/13`).
