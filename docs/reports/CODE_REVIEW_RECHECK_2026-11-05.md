# The code review's bugs, checked again (2026-11-05)

Dimitri: "Wait for other threads to finish for the most updated version, then check to see if the bugs still remain."
This checks the 54 bugs of `CODE_REVIEW_2026-11-04.md` again on the newest base, `ecd1991` (PR #89's fixes, kit 1.9). It
uses PC testing round 3's readings in game (PR #91, `TESTING_ROUND_3_2026-11-05.md` §3). PR #91 holds test code only, and
one counter that the review builds alone compile in. It changes none of the code under these bugs, so this check holds
with or without it. The check reads the code and round 3's logs; nothing new was run in game.

## 1. In short

- **Gone in game:** 18 of the 32 fixed bugs. Round 3 read each one on the fixed build: none showed, and all six parts'
  self-tests pass with 0 failures.
- **Fixed in the code, not yet seen in game:** the other 14. Ten need a second player (MODE-D4, D7, B1-B4) or a check
  round 3 didn't reach (MODE-D8, A2, D10, ARG-3). The last four are guards against leaks and throws (MODE-C7, C8, ARG-5,
  ARG-6), which a log shows only when they misbehave.
- **Still there:** 20 of the 22 left for a reason (`CODE_REVIEW_FIXES_2026-11-05.md` §4). Their code hasn't changed
  since the review (`63af4b0`), so each stands as written.
- **The other two are not bugs after all:** MODE-C12, read again in game (`Totalwaves` is 30), and ARG-1 for the two
  foes it was read on (Dagger and Spear: attack speed floors at 0).
- **Round 3's three open readings are explained** (§2): none is a bug in the fixes.

## 2. Round 3's open readings

- **Pure Aura +2 at Tide 29's break, "where the constant says 3":** this is correct. Tide 29 is a bonus Tide: the map is
  won at Tide 15 (`RWCampaign.WinTide`). A bonus Tide pays `PerBonusTide`, 2, not `PerTide`, 3 (`14_Run.cs` `PayTide`;
  scope 06 [Pure Aura]: "Per bonus tide: 2"). Round 3's check compared against `PerTide`. MODE-D1's fix holds: one
  "Tide 29 paid" line and one VENDCINT.
- **Speed 3 to 3 after buying Caffeine (MODE-C1):** expected. Caffeine grows every Tide, on `Wave`'s change (kit
  `15_ROGUE_MODE_ENGINE.md` §18.1), and the speed was read within 1.5 s of buying, before any Tide. What C1 is about did
  read yes: the bought Cardinals' effects reached the vessel and the tower built before. A reading after the next Tide
  would show the speed.
- **Spring Tide by a Fie wall didn't halve hits (R3, MODE-C3):** consistent with the bug. C3 needs Spring Tide's Beacon
  on the wall first. Round 3 put the wall's effect on first, the order in which the stock Beacon doesn't change hits.
  C3 stays open and waits for Dimitri's word (C2 and C3 share one fix).

## 3. Every bug

"In game" is round 3's reading on the fixed build; "-" means not read.

| Id | The bug | Now | In game |
| --- | --- | --- | --- |
| MODE-D1 (SHARED-1) | Bonus breaks from Tide 29 paid twice | fixed | gone: paid once |
| MODE-D2 | A running objective paid at the next map | fixed | gone |
| MODE-D3 | Mind the Vessels failed when a Boutbro fell | fixed | gone |
| MODE-D4 | A question asked while a vessel was down never reached its player | fixed | - (two players) |
| MODE-D5 | Every foe wore the Hardened look from map 2 | fixed | gone: no skin, Health 10.8 of 10 |
| MODE-D6 | The vote to leave didn't hold the break | fixed | gone |
| MODE-D7 | Closing a question also closed the shop | fixed | - (two players) |
| MODE-D8 | Keep the Basin Clean watched one Basin | fixed | - (not reached) |
| MODE-D9 | A lobby Mooncore ends the map as a Win at Tide 34 | still there: his word | - |
| MODE-D10 | A VENDCINT button could go dead after a throw | fixed | - (not reached) |
| MODE-A1 | The staging flag wasn't saved | fixed | gone |
| MODE-A2 | One throw in the boot turned the part off | fixed | - (not reached) |
| MODE-A3 | "The Basin" can be a sub-core on five maps | still there: a PC fact decides | - |
| MODE-C1 | A Cardinal bought mid-map reached only later spawns | fixed | gone (§2) |
| MODE-C2 | Spring Tide cancels Smacky's Beacon cut on later towers | still there: his word | - |
| MODE-C3 | Spring Tide halves the damage Fie's Regen walls take | still there: his word | one order only (§2) |
| MODE-C4 | Cheap Refits wiped Voice in a Bottle's discount | fixed | gone: 0.94, then 0.84 |
| MODE-C5 | Tidal Mend's heal is frozen at the tower's level | still there: his word | - |
| MODE-C6 | Cheap Refits missed the Bouthouse, Cut Costs Fie's walls | fixed | gone: both 0.9 |
| MODE-C7 | Two dictionaries grew every map | fixed (guard) | - |
| MODE-C8 | A throw in OnSpawn reached the game's spawn | fixed (guard) | - |
| MODE-C9 | `KeyOf` can give one player two keys | still there: a PC fact decides | - |
| MODE-C10 | A rejoiner's Cardinal row may not line up | still there: a PC fact decides | - |
| MODE-C11 | Cardinals past the HUD's 8 slots show nowhere | still there: plausible | - |
| MODE-C12 | Voice's step uses integer `30 / Totalwaves` | not a bug | `Totalwaves` 30 |
| MODE-B1 | A drop set Quickening's speed back to x1 | fixed | - (two players) |
| MODE-B2 | The rejoin restarted live client halves | fixed | - (two players) |
| MODE-B3 | A late client's catch-up outlived its map | fixed | - (two players) |
| MODE-B4 | A turned-away player was told to Join Game again | fixed | - (two players) |
| MODE-B5 | Weather's popup can land on the next map | still there: plausible | - |
| MODE-B6 | The rejoin's "what died" flags outlive their map | still there: plausible | - |
| SHARED-2 | A stray Tide 0 at every map's start | fixed | gone: first line "Tide 1" |
| FOES-1 | A vessel that dies in a Dazzling aura comes back Illuminated | still there: a PC fact decides | - |
| FOES-2 | Illuminate on the Bouthouse went to Boutman | fixed | gone |
| FOES-3 | Aging took Berserk's damage growth | fixed | gone: Damage 4.6 against 3 |
| YOS-1 | A Grafted Sapling's fruit looked like the ALL fruit | fixed | gone: `Blueaura` |
| YOS-2 | Monument's Cleanse strips stock trade-offs too | still there: his word | - |
| YOS-3 | Deaths in a burst overshot the Cordyfriend cap | fixed | gone: 30, 4 capped |
| YOS-4 | The vines' slow look may not load on every map | still there: plausible | - |
| SPL-1 | A Thread Head stayed at a full Stash | fixed | gone |
| SPL-2 | RETURN with nothing carried parked the Head | fixed | gone |
| SPL-3 | TARGET is unchecked while the Tin has no Crafted | still there: plausible | - |
| SPL-4 | One throw stops a Spool brain for good | still there: plausible | - |
| SPL-5 | A friend joining mid-map never sees lying Thread | still there: online | - |
| SPL-6 | A second Yarn Stash in the menu's half second | still there: plausible | - |
| ARG-1 | Two Anchor Line stuns can make attack speed negative | not a bug for Dagger and Spear | floors at 0 |
| ARG-2 | The slow look blinks in Deep Water and the Tremor | still there: plausible | - |
| ARG-3 | Another source's short slow look cut Argo's | fixed | - (not reached) |
| ARG-4 | AIM's reach and ring went stale | fixed | gone: 5.5 to 6.5 with Range |
| ARG-5 | The mixer was searched once | fixed (guard) | - |
| ARG-6 | The recoil dictionary kept every turret | fixed (guard) | - |
| ULO-1 | PLUG IN stuck once the Power Bank was sold | fixed | gone: sold and destroyed |
| ULO-2 | One throw switches a Uloe brain off | still there: plausible | - |
| ULO-3 | Two Uloes feed each other's buildings | still there: his word | - |

## 4. What would close the rest

- **The fixes not yet seen:** a two-player round runs `CODE_REVIEW_FIXES_2026-11-05.md` §5's two-player rows (D4, D7,
  B1-B4). A solo round reads D8, A2, D10 and ARG-3 from the same list. Round 3's two-player build compiled on the PC but
  never ran (`TESTING_ROUND_3_2026-11-05.md` §5).
- **The 20 still there:** Dimitri's word on six (MODE-C2, C3, C5, D9, ULO-3, YOS-2). A PC reading for four (MODE-A3, C9,
  C10, FOES-1), with each one's check in the review's §6. The nine plausible ones are fixed once each is seen; SPL-5
  needs a two-player round.
- **ARG-1** can be closed for Dagger and Spear. Other foes with an attack speed weren't read.

## 5. How it was checked

`git diff 63af4b0 ecd1991` over every file and function the 22 open bugs name: none of their code changed (the files
that did change, for other fixes, were read at the named functions). The 32 fixes: each carries its finding's id in
the code on `ecd1991`, compiled with 0 errors in every part (`CODE_REVIEW_FIXES_2026-11-05.md` §6). PR #91's diff:
test files (`92_Review.cs`, `93_Review.cs`, each vessel's `96_Review.cs` and `99_SelfTest.cs`, `92_Gaps.cs`,
`93_TwoPlayers.cs`), plus an `#if ARGO_REVIEW` counter in `Argo/Code/10_Client.cs`. Round 3's three open readings were
traced in `14_Run.cs`, `06_Campaign.cs`, scope 06 and kit `15` §18.1. Docs only; nothing compiled or run for this report.
