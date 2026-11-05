# PC testing round 3 (2026-11-05)

Dimitri: "Run PC testing round as well." **Stopped early at the usage limit, then finished at his "Continue": section 7 holds the two-player list and the rest, and corrects sections 3 and 5.** Everything
below ran on his PC in TDS. The base moved during the round, so each result names its code: "before" is `1c1a121` (PRs #80-#87),
"after" is `ecd1991` (PR #89's fixes, kit 1.9), both with this PR's test files.

## 1. In short

- **Kit 1.9 boots and every self-test passes** on `ecd1991`: no FAIL in any part. The self-test counts are higher than the
  lists said, because PR #82 added checks.
- **The fixes hold in game** for every finding read: 18 readings, table below. One, MODE-D1, reads SEEN only by my check's own
  counter; what it pays is once.
- **New, for Dimitri's eye:** a virtual click on a Stitched standing at its Cookie Tin lands on the Tin, 0 of 3 tries, and SELL by
  hand failed too (§4). Not proven to be the smaller art: no run with the old art was made.
- **Not run:** the whole two-player list (§5).

## 2. Self-tests

| Part | Before (`1c1a121`) | After (`ecd1991`, kit 1.9) | Log |
| --- | --- | --- | --- |
| Yossele | 177/0 | **177/0** | `Yossele_20261104_214855` |
| Spool | 135/0, 1 setup | **135/0, 1 setup** | `Spool_20261104_215351` |
| Argo | 144/0 | **144/0** | `Argo_20261104_215742` |
| Uloe | 152/0 | **152/0** | `Uloe_20261104_220223` |
| Foes | 12/0 | **12/0** | `RogueWavesFoes_20261104_220353` |
| Mode's flow, every part on | not run | **156/0** (F1-F7 and the Cardinals' return pass) | `RogueWavesMode_20261104_222357` |

Boot (T0, release builds, `1c1a121`): six "kit 1.8" lines, no "parts disagree", 0 exceptions. Kit 1.9 was read from the first
lines of the self-test logs on `ecd1991`; no release boot check was run on it. Exceptions read per log: the game's own only.

**Test faults found and fixed (test code only):**

- **His save equips Voice in a Bottle** (`Player_Artifactselect`), which since PR #80 lowers prices on pack buildings 3% a Tide. Three
  price checks read the list price and failed (Sticky Spores 44 of 50, Wide Bore and Tether 29.1 of 30). They now read the scale.
- Uloe's Bounce Card check was a setup: the two Mirrors stood 5.8 apart. The second now stands by the first.
- Spool's Sewing Machine rank III failed once: a building just past 3 tiles was mended at rank II. I tightened the search, and it
  now finds no building (the setup above), so this run doesn't check rank III's reach. It passed in two earlier runs. Why that
  building was mended is my reading (the engine's area query), not measured.
- Spool's Loom check failed once (`Spool_20261104_201056`, held 0) and passed in every later run. Cause not found.

## 3. Readings

| Id | Before | After | Reading after |
| --- | --- | --- | --- |
| ULO-1 sold, destroyed | seen | not seen | button and skin cleared |
| SPL-1 | seen (38 flips) | not seen | stays at its spot with the Stash full |
| SPL-2 | seen | not seen | back at its spot 10 s on |
| YOS-1 | seen (`Growth5`) | not seen | `Blueaura` |
| YOS-3 | - | not seen | 30, capped 4 |
| ARG-1 | not seen | not seen | floors at 0, bounds 0..99999 (Dagger, Spear): **not a bug for these two** |
| ARG-4 | - | not seen | AIM 5.5 to 6.5 as Range goes 5 to 6 (the synced entry; the picker not opened) |
| SHARED-2 | - | not seen | first line "Tide 1" |
| MODE-A1 | - | not seen | key is "" |
| MODE-D1 | - | **paid once** | Break fired twice (my counter is before the guard), but one "Tide 29 paid", one VENDCINT. **Pure +2 where the constant says 3: unexplained** |
| MODE-D2 | - | not seen | nothing paid before "posted:" |
| MODE-D3 | - | not seen | "Hands done" |
| MODE-D5 | - | not seen | skin "", Health 10.8 of 10 |
| MODE-D6 | - | not seen | "1 of 1 to leave", departing |
| MODE-C1 | - | not seen | effects on the vessel and the older tower; **speed read 3 to 3**, read within 1.5 s of buying |
| MODE-C4 | - | not seen | 1, then 0.94, then 0.84 with Refits, 0.9 after an upgrade |
| MODE-C6 | - | not seen | Bouthouse 0.9, Fie wall's build 0.9 |
| FOES-2, FOES-3 | - | not seen | no Illuminate on Boutman; Damage 4.6 against 3 |
| MODE-C12 | - | not seen | `Totalwaves` 30, Voice's step works |
| R3, MODE-C3 | - | not seen, **one order only** | the wall's effect was on before Spring Tide; hits unchanged. Spring Tide first wasn't tried, so this doesn't clear it |

"-" means not read before the fix. **Other checks, all passing:** G1 (price, Upgraded +1, one flash, sell refund on one building of
each kind; all four vessels, before the fix), G3 (47 paid of 50 x 0.94), G4, A1, H1 and U1 (through the self-tests), Deep Current,
the Cardinals' return. Round 2's Spool host + Argo friend: from its logs, the map was **lost** in the big fight before the drop, so
its rejoin fails follow from that. The unread host error was the game's own cancelled delay in Blue Paint's effect.
L1: I looked at eight captures from the before run, including the new Stitched, Dart, Mangled, Photon and Cordyfriend, and saw
nothing missing; his eye is still owed. Sounds not judged. Each new sheet loads "from Art/Temporary/" in the build logs.

## 4. For Dimitri

**The Stitched by hand:** the virtual pointer never got on a Stitched: each aim landed on the Tin it stood at, so SELL failed too
(host log `host_20261104_222531`). A person may aim better; please try it. The Dart wasn't reached (none in 40 s).

## 5. Not run

- **Two players, all of it:** MODE-D4, D7, B1, B2, B3, B4, G2 and the Spool host + Argo friend re-run with its debuff look (0 of 68
  stays open). `93_Review.cs` and the two-player edits **compiled but have never run**.
- MODE-D8 (my check found no Splithead by that name), MODE-A2, D10, ARG-3, SPL-1's "heads home once there's room".
- The scope copy to `Desktop\RogueWave\scope`; a release boot check on kit 1.9.
- `/code-review high`: not run on this PR's test code (usage limit).

## 6. The PC as left

TDS closed; all six parts enabled as release builds from `ecd1991`; save and registry put back from
`handoff\save_backup_round3_start`. Old builds in `handoff\parked\release_before_round3_20261104`. The shared `pc/QUEUE.md` wasn't
reachable from this session, so no lock was taken there.

Run them: `<NAME>_SELFTEST,<NAME>_REVIEW` for a vessel; the gaps build with `-Extra "-rwonly review"`; the two-player build with
`-Extra "-rwshort -rwreview -rwhosthero Hero_Spool -rwsecondhero Hero_Argo"`.

## 7. Continued: the two-player list and the rest (Dimitri: "Continue", 2026-11-05)

Picked up where the limit stopped §5, on the fixes' base plus PRs #90-#94 (the sage Spool). Every result below ran in TDS on
his PC. **This section replaces §5's "not run" list, and it corrects §3 where they differ.** The code review of this test
code (`/code-review high`) came after the last run: its fixes are test-only and compiled, not run.

**In short:** every fix read in game holds, online too. Two things are still live, both already on the review's list and neither
fixed: Spring Tide halves hits by a Fie wall, and the part takes a sub-core for the Basin on Artrio. One thing is new: a Crafted
with no TARGET stands inside its Tin, where a click can't reach it.

### 7.1 Two players (`host_20261105_002948` and `host_20261104_234523`, with `second_` logs of the same stamps)

| Check | Result | Reading |
| --- | --- | --- |
| The drop and rejoin, both pairs (Spool host + Argo friend; Argo host + sage Spool friend) | **pass** | O1-O4 and O7 hold in both; O3b and O5's second half were not part of this short run |
| Round 2's "0 of 68 looks" and its failed rejoin | **not a bug** | The map was lost in the test's big fight, which is what failed the rejoin. With the Basin held, 29 foes wore Argo's own slow on the friend's machine, and all 40 looks the host sent arrived |
| MODE-D4 | **holds** | The vessel was down as the vote opened (strict: the Tide's last foe left only after it fell). The dialog opened on the friend's machine, who answered |
| MODE-D7 | **holds** | SHOP stays up as the other player answers |
| MODE-B1 | **holds** | 1.25 on the host after the drop and after the rejoin, and on the rejoiner |
| MODE-B2 | **holds** | 499 client halves, no double, no being listed twice |
| MODE-B3 | **holds** | No "map loaded here" line on the next map; "Loading 80%" as normal |
| MODE-B4 | **holds** | The turned-away player's box reads "Disconnected", both tries |
| MODE-C9 | **not a bug here** | One key for the friend after the rejoin |
| MODE-C10 | **not a bug here** | The same four Cardinals in the same order on both machines |
| G2 | **pass** | A client's upgrades land and show: Argo 5 of 5, Spool 3 of 4 (the Yarn Stash has none) |

The host's one FAIL in both runs (C5, route 0) was this test's: `Win()` expected a route step from a map Tide 15 had already won.
Fixed in the test, compiled, not re-run.

### 7.2 Solo

| Check | Result | Reading |
| --- | --- | --- |
| MODE-D8 | **holds** | `5B_Split`, two Basins, a foe hits only the second: "Hull failed" |
| MODE-A2 | **holds** | `RWIcons.Add` threw on purpose: one warning, then "ready", and the button still took the party to the boat |
| MODE-D10 | **holds** | SHOP's dialog threw once: one warning, nothing stuck, SHOP opens on the next press |
| ARG-3 | **holds** | After another source's 0.2 s look, on for 3.7 of the next 4 s, longest gap 0.31 s |
| **R3, MODE-C3** | **live** | **With Spring Tide taken first, a 10 hit takes 4.88 beside a Shield wall and 4.76 on the wall** (`Resistadd` 1.05 and 1.1), against 9.52 and 9.09 the other way round and 10 away from any wall. Pitfall #159. Waits for his word (the review's §5 B) |
| **MODE-A3** | **seen** | On `4_Artrio`, `PlayerCore()` is `Main_Subcore`; `PlayerCores()` lists two sub-cores and `Main_Core`. Unfixed, in the review's list for a PC reading |
| ARG-1 | **not a bug** for Dagger and Spear | Attack speed floors at 0; other foes didn't spawn on this map |

**Corrected from §3:** "R3, one order only" is settled above. MODE-D1's and MODE-C1's open notes were read by the recheck
(`CODE_REVIEW_RECHECK_2026-11-05.md`): a bonus Tide pays 2, and Caffeine grows only as a Tide passes. **FOES-1 and FOES-2 are not
proven:** no Illuminate reached anything in my set-up, so "not seen" says nothing about those fixes.

The four guards (MODE-C7, C8, ARG-5, ARG-6) show only if something goes wrong. No "brain:" fault line (SPL-4, ULO-2) in any log of
this round. Not read: SPL-5, MODE-C2, C5, D9, B5, B6, YOS-2, YOS-4, ULO-3, ARG-2, SPL-3, SPL-6.

### 7.3 The smaller Crafted, and the sage Spool

- **Selling works:** SELL and a click sold a Stitched and a Dart standing at the Tin's spot. A walking Stitched was never sold in the
  2026-10-26 play test, so this is better than it was.
- **Selecting mostly works:** a Dart 3 of 3. A Stitched 0 of 3 and 2 of 3 in two runs: the pointer was on it, but the selection
  went to the **TARGET pin** under it (`Trap_Spoolpin`).
- **New, a Crafted with no TARGET stands inside its Tin** (0.1 tiles from its centre): clicks hit the Tin, 0 of 3, and it can't be sold
  that way. The same capture before PR #86 shows none in sight either, so this isn't the smaller art; the click itself wasn't
  run on the old art. The pointer goes by each being's own block, not its sprite, as far as these clicks show.
- **The sage look (four captures, for his eye):** beside a Hardened foe's lilac tint the two read apart at once. On stone he stands out
  well. On the map's green floor his pale mint is told apart by lightness, not hue, so that's the weakest case.

### 7.4 Also done

- **Release boot check:** on kit 1.9, six lines, no "parts disagree", 0 exceptions (`boot_20261105_022551`). Again on the newest base
  `26fdae2`, kit 1.11: six lines, 0 exceptions (`boot_20261105_024012`). **The self-tests and readings ran on kit 1.9**, not on
  what PRs #95-#101 changed since; only this boot check ran on those.
- **Scope:** the 17 files in `Desktop\RogueWave\scope` now equal `docs/scope`; the old copies are in
  `handoff\backup_scope_before_round3_20261105`.
- **`/code-review high` on the test code:** nothing reaches a release build (a text scan of the guards, and the release builds
  compile). 10 findings. Fixed: A2's check, a stopped part left on a map, B3's watcher, unstepped coroutines, D4's reading,
  readings that a failed setup turned into SEEN, N4's sender, Argo's needless stop, mis-encoded `§` in comments. Another thread
  fixed the `ARGO_REVIEW` guard. Left: the helpers copied across the four review files, as debug code that stays as is.

**The PC as left:** as §6, with the release builds below, built from this branch after the newest base was merged.
