# Dimitri's open decisions, second page (2026-11-05)

Dimitri, 15:47Z: "Create a report of everything that needs my decision right now." Read from the base `46753c3` (after
PR #115) and checked item by item against the reports and the code. The first page (`OPEN_DECISIONS_2026-11-05.md`)
was answered at 04:59Z and is not reopened here. Nothing was changed to write this page.

**Answered 2026-11-05 16:04Z** (Dimitri: "Default for all, and I give permission for 12"). Every **Rec** below is
taken; what was applied and who does the rest is in "The answers" at the end.

**How to answer:** by number, for example "1 yes, 2 yes but c keep, 9 American, rest as recommended". "Rest as
recommended" takes every **Rec** below. A skipped item stays open and nothing changes for it. Scope and doc answers go
straight into a PR from this thread; anything that needs code goes to the thread that owns it.

## A. Game design and balance

1. **The build's readings of 15 scope lines** (`BUILD_UNBUILT_2026-11-05.md` §4). Among them: Galley Kettle, Notched Oar
   and Chain Shot count attacks, not foes struck; Clam Shell judges a blow after the other mods' hooks; Lamp Oil needs
   the killing blow within 0.25 s; Spare Plank makes the vessel's own Defense button free. **Rec:** yes to all 15 as
   built. Blocks: nothing. A "no" on any line is a small code change.
2. **Seven behaviour questions the clean-up found** (`BUILD_UNBUILT_2026-11-05.md` §2). Blocks: the next code review.
   **Rec:**
   - a. Two parts' hit handlers on one thing swap order every 0.05-0.1 s (`ModKitUtil.Front`): fix it, so a handler
     already in front stays there.
   - b. Uloe's Searchlight slow can go below the 10% floor: give it the floor (`SlowOn`).
   - c. Argo's Cleave counts as his own hit for Fish Hook, Gutting Knife and Notched Oar: stop counting it. Gutting Knife
     already skips the pack's extra hits.
   - d. Aegis is in the stock elite list but matches no asset: leave it. Taking it out changes every seed's modifiers.
   - e. Four different tests for "is this foe elite": leave them until a PC round shows they disagree.
   - f. Tar Bucket's hit test misses hits that carry their own Damage: use Mason's Trowel's hook.
   - g. Three latent slips (`HasMinions`, Blurry Photo copies, the Cardinals' paired lookups): fix all three.
3. **Yossele's defaults in scope `07`.** About 90 `~values~` are still Claude's: the 04:59Z answer covered 04 and
   09-14, not 07. They are what the build plays and what Release 1 ships. **Rec:** take them as yours now. Your play
   test can still change any of them. Blocks: nothing in code. The scope just stops marking them as Claude's.
4. **The last small defaults, and the `! NEEDS !` lines that are now built.** Defaults: 03's two fix names ("F Picks a
   Minion", "No Way Back In"); 06's Direction power, pairs and Duos rules; 10's 4 Light a building from teammates; 12's
   shop list. NEEDS lines: 04's modifiers per campaign, 11's Directions, 12's Cardinal/Trinket list and shop list, and
   14's objectives are all built from Claude's lists. **Rec:** take them all and clear those NEEDS lines. Do 04 after the
   balance sweep, which rewrites its modifier schedule. Blocks: nothing, docs only.

## B. Wording (`TEXT_AUDIT_2026-11-05.md` §4)

5. **"Long Line" names two things:** Argo's Harpoon Gun upgrade and Boutman's Direction. **Rec:** rename the Direction
   **Spare Reel**. Blocks: one code line and one scope line.
6. **The leave vote's line** "A majority leaves..." reads as if players walk off. **Rec:** "Most votes win. Nobody leaves
   right before their demise." Blocks: one code line.
7. **Depth "unlocks"** in scopes 04 and 05 while Depth is on hold. **Rec:** keep your line and add a Claude note that it
   waits on Depth. Blocks: nothing.
8. **Numbers in Notched Oar's text** ("Every 10th / 7th / 5th hit"), which the style keeps out of upgrade text. **Rec:**
   keep them, because the count is the whole rule. Blocks: nothing.
9. **British or American spelling.** The game's text mixes the two (Colour, Armour, harbour), and so does the scope
   (Woolen, fertilizer, gray, color). **Rec:** British in player text, to match the game's own "Colour" buttons, and
   names as you wrote them. Blocks: a spelling pass.

## C. Art

10. **The meteor node's look.** Scope 14 asks for a white crystal. The node still uses a stock ore look, because the mode
    part has no model library. **Rec:** b.
    - a. Keep the ore look, and the line becomes "A fallen node. Mine it all".
    - b. Claude gives the mode part a model library and draws a temporary crystal. The line stays. This follows your
      answer 34, "art for every missing asset".

    Blocks: `19_Objectives.cs:71` and the next art pass.
11. **Yossele's final sprite sheet for Release 1.** Scope 07 still says "NEEDS YOUR FULL SPRITE SHEET", and Release 1
    has waited on it since 2026-10-28. Your answer 34 was read as art for assets with none, and final versions of the
    temporary art were left for your word (`DECISIONS_APPLIED_2026-11-05.md` A4). **Rec:** b.
    - a. You draw it.
    - b. Claude draws a final sheet from your reshade and the temporary sheet, in .aseprite, for you to edit.
    - c. Release 1 ships with the temporary art.

    Blocks: Release 1.

## D. PC and testing

12. **When to run the next PC round.** Nothing has run on the PC since kit 1.9. Kit 1.11 was only boot-checked, and
    PRs #95-#115 (kit 1.12, every new Direction, Trinket and Cardinal, Uloe's 24 upgrades, Argo's 14 changes) have never
    run in game. The list is `BUILD_UNBUILT_2026-11-05.md` §5 plus `CODE_REVIEW_MAX_2026-11-05.md` §5.2 and §6. **Rec:**
    right after the balance sweep merges, and before Spool's 22 upgrades, so this build is proven before more code
    lands. It touches your PC, so it needs your OK. Blocks: the Spring Tide fix check, the 12 PC-fact findings, and the
    refactors the clean-up held back.

## E. Repository and housekeeping

13. **Sync your scope copy.** `/mnt/project-files/scope/` and the Scope Editor (v9) differ from the repository in 15
    of 17 files (all but 01 and 14). **Rec:** say "sync", and Claude syncs once after the balance sweep merges, because
    the sweep changes 04, 06, 08 and 10. Your PC's copy is synced at the next PC round. Blocks: your copy matching.
14. **`.claude/settings.json`, which only you can edit** (Claude may not change its own permissions).
    - a. It still denies merging (`merge_pull_request`, `enable_pr_auto_merge`, `gh pr merge`), although you asked
      Claude to merge.
    - b. It lacks the two setup commands the docs use (`apt-get update`, `python3 -m pip install UnityPy`; C10 of
      `QUALITY_PASSES_2026-10-15.md`).

    **Rec:** delete the three merge lines, keep the two denies on pushing to the base branch, and add the two setup
    commands. Blocks: nothing. It only removes a contradiction and some permission prompts.

## Settled already, so not asked again

- **Spring Tide halving hits beside a Fie wall** (round 3: 10 -> 4.9). Your answer 14 rebuilt it on the mode's own
  effects, with no `Resistadd` on the tower. It was built in PR #103 (B7, pitfall #159) and has a self-test check
  (`96_Flow.cs:624`). What is left is seeing it in game (item 12).
- **Spool's P1-P4**: answer 25. The sweep tunes P1-P3 for one Pin Cushion wall, and P4 (the "wall with Pin Cushions
  first" line) is taken. **Weather pay wording** (`TEXT_AUDIT` A7): the sweep words it with its numbers.
- **The kit's older-mod mentions**: kept (answer 35, the default). `D:\TDS_second` is kept too (answer 36).
- **The max review's three calls** (MODEB-8, MODEA-3, YOS-3): yes, built in PR #103.
- **The earlier code review's six calls** (MODE-C2, C3, C5, D9, ULO-3, YOS-2): answered at 04:59Z and built in #103 and
  #105. Its other 14 open bugs wait on a PC fact or a sighting, not on you (`CODE_REVIEW_RECHECK_2026-11-05.md` §4).

## For your eye or hands (no decision, just a time)

- The icons after PR #99: the menu tile, a dialog sliding in, the HUD buttons.
- Spool's sage green on the green floor.
- The card-hover stutter by hand.
- The F key on a real Boutman death on kit 1.11. The 2026-11-04 pass ran with the fix unarmed.
- Selling a walking Stitched by hand. PR #103 fixed the click, and the bigger click box (B17) waits on your try.
- One online evening with a friend.
- Turning off the connectors that fail to load: Figma, Notion, Slack, Datadog and the rest.
- What only you can supply: your Argo balance pass, and the Yossele play test that decides P5/P6 (Yossele's
  Sapling speed and fruit pay).

## The answers (16:04Z)

Dimitri: "Default for all, and I give permission for 12."

| # | Taken | Done by |
| --- | --- | --- |
| 1 | The 15 readings stand as built | nothing to do |
| 2 | Fix a, b, c, f, g; leave d and e | a code thread (next code review or build) |
| 3 | Scope 07's 95 defaults are his | done in this PR |
| 4 | 03's names, 06's rules, 10's sharing limit, 12's shop list are his; NEEDS lines in 11, 12, 14 cleared | done in this PR; 04's NEEDS line after the balance sweep, from this thread |
| 5 | Boutman's Direction becomes **Spare Reel** (`16_Directions.cs:89`, scope `11:537`) | a code thread, code and scope together |
| 6 | "Most votes win. Nobody leaves right before their demise." (`20_Vote.cs:662`) | a code thread |
| 7 | Depth's lines kept, with a Claude note | done in this PR (scope 04, 05) |
| 8 | Notched Oar keeps its numbers | nothing to do |
| 9 | British spelling in player text, names as he wrote them | a code thread, with the scope lines that are in-game lines |
| 10 | The mode part gets a model library and a temporary white crystal; the line stays | an art thread |
| 11 | Claude draws Yossele's final sheet from his reshade and the temporary sheet, in .aseprite, for him to edit; 07's sprite NEEDS stays until he takes it | an art thread |
| 12 | **His OK for the next PC round**, right after the balance sweep merges and before Spool's 22 upgrades | the PC thread |
| 13 | "sync": his project-files scope copy and the Scope Editor once the sweep merges | this thread |
| 14 | `.claude/settings.json`: delete the three merge deny lines, add `apt-get update` and `python3 -m pip install UnityPy` | Dimitri (Claude may not change its own permissions) |
