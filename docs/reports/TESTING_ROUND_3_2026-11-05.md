# PC testing round 3 (2026-11-05)

Dimitri: "Run PC testing round as well." **Stopped early: the usage limit was reached before the two-player part.** Everything
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
