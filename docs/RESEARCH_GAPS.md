# Research gaps: what the expansion still needs to learn about the game

Engine facts the scope (`docs/SCOPE_v0.txt`) depends on that Phase 1 left open. Listed 2026-10-08, against
`TDS_Vessel_Creator/docs/13`, `14`, `15`, RogueLab round 1, Riptide's notes and Argo's six PC rounds; **answered from
the decompiled game 2026-10-09** (the facts, with `file:line`, are in kit `docs/15` §14). This is the research half of
`docs/SCOPE_GAPS.md` (PR #6), which lists the **design** gaps; nothing here repeats it. Sonar is out of scope (user).

**Status:** **answered** = read from the game's code, enough to design on; **PC** = waits on RogueLab round 2
(handoff `RogueLab_round2`, built and compile-checked 2026-10-09); **online** = needs a second player; **by hand** =
Dimitri plays it.

## Summary

Of the 18 items, 10 are answered from the code (three with a detail left for the PC), 6 wait on one PC round
(RogueLab round 2, no clicks needed), 1 needs an online evening with a friend and 1 waits on a scope decision; the F-key
check by hand is still open too. Three answers change plans:
**Directions can't be per player on the core** (A1), **nothing in the game scales foes for more players or harder
maps** (A3), and **a friend who drops can't rejoin a running map** (A5).

## A. Before Phase 5 designs the mode

| # | Gap | Answer | Status |
| --- | --- | --- | --- |
| A1 | Per-player Directions | The core is the team's: it spawns every player's vessel and has no owner; `Heroapply` reaches every vessel and `Towerapply` every new tower. A Direction must go on the player's vessel, re-granted from host state at each spawn, and tower Directions through the mod's own spawn hook filtered by owner. **`ModKitDirections` (kit 1.6) needs that change** before Phase 5 uses it (§14.1) | answered |
| A2 | Fund Transfer's route | Aura is per player. The host moves it with the public `Useresources` and `Giveresources(list, owner)`, with the gain multiplier held at 1 (Cardinals change it). The stock Money Bag Cardinal already donates aura, lossily and by a thrown bag; its code is private, so it's prior art, not a reusable piece. Which aura moves stays Dimitri's call (scope `! NEEDS !`) (§14.2) | answered |
| A3 | Difficulty levers | The game has none to borrow: no scaling by player count or map; a map's `Difficulty` is only a label. Levers a mod has: the Tide table (counts, intervals, elites, extra entries; proven live), the game's hard setting's upgrade forced on the core (`Special_Treated`, more start aura and drops), the aura gain multiplier, and modifiers of its own. **A 4-player run faces solo Tides unless the mode scales them** (§14.3) | answered; the hard setting's numbers and the other modes' tables: PC |
| A4 | The map pool | Any map takes any mode; the coupling is data (the mode's spots on the map, which only warn when missing; map buildings like Merchut and gates come with the map). No code singles out a map. Whether each map starts cleanly as the mod's mode: the PC round plays every candidate once (§14.4) | PC (the tour) |
| A5 | Drop and rejoin | Key run state by `Sessionid` (stable on a rejoin; RogueLab and Riptide already do). A drop despawns nothing; teammates can command the dropped player's things. **A running map refuses joins** (closed lobby), and a rejoin comes back with no aura. So a dropped friend rejoins only between runs, unless the host reopens the lobby between maps (§14.5) | answered; reopening between maps: online |
| A6 | A shop and a vote showing more than three things | The dialog takes any number of entries, two per row, with title, colour, image and a hover description; no price or disabled field, so prices go in the name and the host refuses a short purse. The shop is the vessel-command route Riptide proved; no building needed (§14.6) | answered; how eight cards fit on screen: PC |
| A7 | A save key surviving a restart | Round 1 left a launch counter in the save; round 2's boot reads it | PC |
| A8 | Online, for any mode piece | Unchanged: nothing in the mode has run on a client yet (`docs/15` §11 step 11) | online |
| A9 | One bundle, several `Default` prefabs | RogueLab 0.2 ships a second prefab with its own code in the same bundle and checks it starts | PC |

## B. Before each vessel's design gate

| # | Gap | Answer | Status |
| --- | --- | --- | --- |
| B1 | Light received (Uloe) | The game has no gameplay light; lighting is rendering only. Light is a number the mod sums from sources in range with the game's own area query, on a slow tick. Feasible; its cost goes with B5's numbers (§14.7) | answered |
| B2 | Changing damage as it lands (Pin Cushion, Illuminated, Blinded, Mirror) | Every hit passes through the target's AIs in priority order before the target's own AI applies it, carrying the attacker: an AI can scale or cancel it (Fie's wall does), hurt the attacker back (Smacky's spiked wall does) or hold half for later. All four mechanics are go (§14.7) | answered |
| B3 | Ground pickups (aura fruit, Thread) | The game's own aura pickup is the pattern: it checks for a nearby same-team unit, pays its owner and expires. Enemy drops never touch the ground (paid to everyone at death), so Thread drops are the pack's own pickups; Boutman's fish carry nothing (§14.7) | answered |
| B4 | Directions that change a stock vessel | An upgrade adds AIs (it can't remove one by data), and an added AI's code can change what a tower does. Possible if the catalogue wants it (a SCOPE_GAPS decision) (§14.7) | answered |
| B5 | A swarm's cost | Frame time with 50, 100 and 200 extra units on the host | PC (host); online (client) |

## C. Enemies (Phase 4), from `docs/13` §10

| # | Gap | Answer | Status |
| --- | --- | --- | --- |
| C1 | Explode and Weakened elite values | Round 2 loads every mode's group and dumps the elites again | PC |
| C2 | The other modes' Tide tables | Round 2 logs Storm, Drought, Tsunami, Final | PC |
| C3 | Enemy sprite format and elite skins | Needed only for new enemy types or skins (both `! NEEDS !` in the scope) | waits on that decision |
| C4 | Enemy pages in the journal | Journal pages are data assets; no code lists enemies, so the pack's foe pages are its own, as vessels' are (`docs/06`) | answered |

Flying waits for a flying enemy.

## D. Pieces the scope doesn't ask for

New maps, a boss or final fight, music, localization, tutorial tips and vessel skins (`docs/EXPANSION_SCOPE.md` rows 4,
5, 8, 12, 13, 14) need research only if Dimitri wants them.

## E. What is left, in order

1. **RogueLab round 2 on the PC** (`TDS_Mod/ModSource/RogueLab/README.md`, "Round 2"): A3's and C's data, A4's map
   tour, A6, A7, A9, B5's host half. About an hour unattended; afterwards `RogueLabMod` comes out of the Mods folder.
2. **By hand:** the F-key bug with Boutman (`docs/15` §11 step 8): let Boutbro spawn, die, press F after respawning.
3. **One online evening** with a friend: A5 (reopening the lobby between maps), A8, B5's client half.
4. **Kit 1.7** at Phase 5's start: `ModKitDirections` grants per player on the vessel (A1).
