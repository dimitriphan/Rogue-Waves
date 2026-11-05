# Spool's open defaults, explained (2026-11-05)

Dimitri, 05:06Z: "elaborate more on the Spool's DESIGN.md". PLAN Open question 2 asked him to take or change "the
defaults in its `DESIGN.md`" without saying what they were. Here they are, each in plain words: what the sheet left
open, what Spool 0.1.0-dev does today, the other ways it could go, and Claude's pick in `~tildes~`. Source:
`TDS_Mod/ModSource/Spool/DESIGN.md` ("Defaults picked here", "Changes from the concept") and scope `08`.

**Answered 05:10Z, "Use all defaults"**: what follows is in `DECISIONS_APPLIED_2026-11-05.md` §4 (B16-B19).

**How to answer:** by number in the thread, for example "S3 no merging, S10 remove MOVE, rest defaults". Nothing is
built from this page: what he changes goes to the build thread's Spool items, and "leave Spool as is" holds until then.

## A. Thread drops

- **S1. How much Thread a foe drops.** The sheet says "smaller enemies drop 1, larger elite enemies drop up to 5".
  Today: 1 Thread, 3 for a large foe (base health 40 or more), 2 more for an elite, never more than 5. Since his answer
  of 2026-11-05, the pack's own modifiers (Cursed, Berserk, Aging, Dazzling) count as elite too. `~as built~` / a flat 1
  and 5 with nothing between / your own numbers.
- **S2. Who gets a drop.** Thread is kept per player. Today the first Spool or Thread Head to reach a tuft takes all of
  it. `~first to reach it~` / split between every Spool player / only the player whose things killed the foe.
- **S3. Tufts close together merge.** To keep the screen and the game light (the sheet: "make sure it doesn't lag the
  game"), a drop within half a tile of another joins it, up to 15 Thread in one tuft. `~merge~` / never merge.
- **S4. Drops only while someone plays Spool.** On a map with no Spool, no tufts appear at all. `~yes~` / always drop.

## B. Picking up and giving back

- **S5. Picking up a Loom's Thread.** The sheet gives Spool a 1-tile pickup on the ground but nothing for the Loom.
  Today Spool takes a Loom's Thread from 1.5 tiles, the same reach as dropping off at the Stash. `~1.5 tiles~` / 1 tile.
- **S6. A sold Crafted's refund when the Stash is full.** Selling a Crafted gives half its Thread back; what doesn't fit
  in the Stash is dropped beside it as a tuft, so nothing is lost. `~dropped as a tuft~` / lost.

## C. PATCH

- **S7. PATCH's hotkey.** The button asks you to click a target; the hotkey has no click. Today it mends whatever is
  under the cursor, else the most hurt of your units and buildings in reach. `~as built~` / only what is under the cursor.
- **S8. PATCH on Spool itself.** The sheet says "a unit or building", so today Spool can't patch itself.
  `~no~` / yes.

## D. Buildings and units

- **S9. Selling and losing the Yarn Stash.** Selling it refunds nothing and keeps its Thread for the next Stash (it is
  free, so selling is how you move it). Destroyed, its Thread spills on the ground and the build menu goes back to the
  Stash only, rebuilt free, like Boutman's Bouthouse. `~as built~` / a destroyed Stash loses its Thread.
- **S10. The Thread Head's two move buttons.** It has its own SPOT and the game's stock MOVE, which do the same job
  (`PLAYTEST_2026-10-26.md`). `~remove MOVE, keep SPOT~` / keep both.
- **S11. Placing a Thread Head.** The build menu places a one-frame token on a walk-through pad and swaps it for the
  Head, so the Head doesn't block foes the way Boutman's fish do (a fish's own block would). `~as built~` / let it block
  foes like a fish.
- **S12. A Cookie Tin with no spot picked.** Its Crafted guard the Tin. When TARGET moves the spot, Crafted already out
  keep their old spot until they die (the sheet: "they stay there until death"), so one Tin can hold several spots.
  `~as built~` / everyone moves to the new spot.
- **S13. Selling a walking Stitched.** The game's sell picker has to land on a 16-pixel unit that's moving, which the
  virtual player never managed (`PLAYTEST_2026-10-26.md`); in round 3 a click on a Stitched standing at its Tin landed on
  the Tin (`TESTING_ROUND_3_2026-11-05.md` §4). `~try it by hand first; if
  it's hard, give Crafted a bigger click box~` / an UNTHREAD-all button on the Tin instead (scope 08 already has
  UNTHREAD) / leave it.
- **S14. What counts as a melee hit for the Pin Cushion.** A hit doesn't say whether it was melee, so today an attacker
  with Range 2.25 or less counts. `~2.25~` / another number.
- **S15. Pin Barrage ends its branch.** Both level-3 Cushion upgrades below it shape the Mangled, which only Loose
  Stuffing makes, so Pin Barrage leads nowhere today. Scope 08 now gives it Iron Thimble at level 4. `~build Iron Thimble
  after it, as scope 08 says~` / leave it ending.

## E. What scope 08 adds that isn't built

- **S16. The 22 upgrades and the UNTHREAD button** he accepted on 2026-11-04 (`UPGRADE_GAPS_2026-11-04.md`): seven Yarn
  Stash upgrades, the Thread Head's and the Loom's levels 2-4, the Cookie Tin's Tacking Stitch, Cross Stitch and
  Patchwork Army, the Pin Cushion's Iron Thimble and Voodoo Doll. Their numbers are Claude's defaults in scope 08.
  `~build them with the queued candidates, after the balance sweep's one-wall numbers~` / wait for your play test / change some.
- **S17. Spool's own numbers in scope 08** (Health 45, Speed 2.75, Damage 2.5 and the rest, the Crafted's stats, the
  costs): Claude's defaults, not covered by "take every default" (that was scopes 04 and 09-14). `~take them, apart from
  what the balance sweep changes~` / keep them as defaults until your play test.
