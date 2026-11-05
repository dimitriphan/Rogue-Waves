# Dimitri's open decisions (2026-11-05)

Dimitri: "Check the idle threads to see if there is anything that can be continued or combined into a single thread
instead." Nearly every idle thread waits on a call from him, so this page gathers all of them in one place, read from
the reports on the base after PR #91 (`dc297e6`). Nothing here is new work and nothing was changed to write it.

**How to answer:** reply in the thread by number, for example "3 no cap, 13 keep stock, rest defaults". Each line's
option in `~tildes~` is Claude's recommended default; "defaults" takes every one of them. A question you skip stays open
and nothing changes for it. Small answers go straight into a PR; bigger ones (a build, a PC round) get their own thread.

## A. The campaign

1. **No map vote after a bonus-Tide loss or a host quit** (the leader steers alone): `~open the same vote on the boat~` / keep it and write "leader steers" into scope 04. Source: `SCOPE_CODE_AUDIT_2026-11-04.md` 04 row 1.
2. **The boat's lobby Cardinal picker between maps** is still free and offers Moon Core and the 14 others the shop leaves out (with a lobby Mooncore a bonus rewind ends the map as a Win at Tide 34, MODE-D9): `~lock lobby picks once a campaign starts~` / filter them as the shop does / leave free and write it into 04 and 12. Source: audit 04 row 2; `CODE_REVIEW_2026-11-04.md` §5 B.
3. **Cardinal stacking:** a pack Cardinal or Trinket works for 6 copies, a stock one is held once, but scope 12 says no limit: `~keep the caps and write them into 12~` / no limit. Source: `QUALITY_PASSES_2026-11-04.md` "Left for Dimitri" 1.
4. **A stock Cardinal you already hold is still offered** on the shelf: `~take it off the shelf~` / leave it. Source: audit 12 row 4.
5. **A held Trinket shows nowhere, nor a Cardinal past the 8th:** `~Trinkets on the vessel's panel, Cardinals past 8 as a "+N" count~` / leave hidden. Source: audit 12 rows 1-3; MODE-C11.
6. **Extra foe health per map (MODE-D5):** already built as `~kept, with the Hardened look removed~` / drop the extra health until a look of its own exists. Source: `CODE_REVIEW_FIXES_2026-11-05.md` §3.
7. **The mode polish's nine readings** (Tide 15 "cleared" = Tide 16 starts; host quit after Tide 15 = won; 45 s opening break; replay = lost map from Tide 1; 3 retries = fourth loss ends it; the new chat lines; warped home at the end; a Win before Tide 15 wins; old keys migrated): `~confirm all nine~` / name the ones to change. Source: `docs/reports/MODE_POLISH_2026-11-03.md` §2.
8. **Weather's long line** shows on the boat when the map is picked, the map start shows only "RISING WATER ROLLS IN": `~keep it, scope 04 and the template say so~` / show the whole line at map start too. Source: audit 04 row 5.
9. **Weather numbers** (odds, extra Pure Aura, Quickening's speeds): `~as written~` / your numbers. Source: scope 04 [Questions] 1.

## B. Foes and objectives

10. **Cursed also weakens vessels** (scope: "towers and units"): `~towers and units only, as written~` / vessels too. Source: audit 13 row 1.
11. **Dazzling's Blind and Illuminate level:** `~level 1 (15%)~` / "10 Light" as one line of 13 says. Source: audit 13 row 2.
12. **The pack's own foe modifiers count as elites** for Spool's Thread drop (up to 5): `~yes~` / no, stock elites only. Source: audit 08 row 1 (a Spool change, so it waits while Spool stays as is).
13. **Objective kinds in scope 14** (Defence, Speed, Hunting) are not the template's (Hunt, Guard, Hold, Rush, Mining): `~use the template's kinds~` / keep and fix the template. Source: quality passes 2.

## C. Directions and Trinkets

14. **Spring Tide and Tidal Mend on stock scripts** halve Fie's wall hits, cancel Smacky's Beacon cut and freeze Mend's heal at a tower's level (MODE-C2, C3, C5): `~rebuild both on the mode's own effects~` (numbers change on some towers) / keep stock scripts and write the quirks into 11. Source: `CODE_REVIEW_2026-11-04.md` §5 B; audit 11 row 1; simplification finding 1.
15. **The three Shift Directions don't change the building** (Dandelion Season, Mother of Mushrooms, Sewing Machine): `~rename now, new look when its art exists~` / leave as is. Source: audit 11 rows 6, 7, 9.
16. **Cut Costs on Boutman:** today the same amount comes off his larger fish price: `~keep that~` / don't offer it to Boutman / build Boutbro's own cost stats (Worm Tin). Source: audit 11 row 2; `CODE_REVIEW_FIXES_2026-11-05.md` §3.
17. **Monument's Cleanse also strips stock trade-offs** from a player's towers (YOS-2): `~leave stock trade-offs alone, strip foes' debuffs only~` / strip all. Source: `CODE_REVIEW_2026-11-04.md` §5 B.
18. **Two Uloe players feed each other's buildings with Light** (ULO-3): `~your own buildings only, as designed~` / share. Source: same.
19. **The 15 new Directions, Claude's 11 Trinkets and 4 Cardinals, the research-choice pairs and Duos:** `~each stands; nothing is built until you pick~` / take, change or drop each. Source: `DIRECTIONS_CARDINALS_2026-11-04.md` §6; `ROGUELIKE_DIRECTIONS_CARDINALS_2026-11-04.md` §5.

## D. Vessels

20. **Yossele's FERTILIZE on a click** has no range (the hotkey picks within 6 tiles): `~write "6 tiles is the hotkey's pick"~` / add a range check on clicks. Source: audit 07 row 1.
21. **Building damage reduction is capped at 90%** (Stonehenge 80% + Monument 20% would be unkillable): `~keep the cap, write it into 07~` / no cap. Source: audit 07 row 3.
22. **Focused Roots** adds +80% to the same sum as Growth (x1.53 when grown): `~reword to "+80% mining speed"~` / a separate x1.8. Source: audit 07 row 4.
23. **Natural Remedy at Growth 0** heals as stage 1: `~keep, add "counts as stage 1 before it has grown"~` / 0 heal at Growth 0. Source: audit 07 row 2.
24. **Mining (P5, P6):** Sapling Attackspeed 0.25 to 0.15 (with Argo's Drill to 0.2 and Uloe's Flood Light with it), fruit x15 to x5: `~no change until your Yossele play test~` / take P5 / take P6 / both. Source: `AURA_BASELINE_2026-11-03.md`; PLAN Open questions 4.
25. **Spool's balance (P1-P4):** Dart Range 4.5, 6 Crafted a Tin, +1 damage, and "wall with Pin Cushions first" in the tooltip: `~held until you try your own wall on Purlest and Woozy~` / take P4 only / take all four. Source: `BALANCE_2026-11-03.md` §8; PLAN Open questions 3 and 6.
26. **Spool's Loom line (P7)** says it mines "like a Mining Hut"; it mines 5x that: `~write what is built~` / change the code. Source: `AURA_BASELINE_2026-11-03.md`.
27. **Argo's Salt Sting without Brine:** `~1.0 a second, as built (fix the text)~` / 0.5 a second. Source: audit 09 row 5; `MISTAKE_CHECK_2026-10-28.md`.
28. **Argo's slows stack with their own re-ticks** (a 30% pool slows 60%): `~one slow per source~` / keep. Source: audit 09 rows 2-4.
29. **Argo's pins and reels on bosses:** `~bosses never pinned or reeled~` / only never reeled. Source: audit 09 row 1.
30. **Argo's 14 review changes in scope 09:** `~each stands; the next Argo build picks them up~` / take, change or drop each. Source: `ARGO_REVIEW_2026-11-04.md` §5.
31. **Uloe's 24 new upgrades and the names of yours:** `~as written~` / take, rename, change or drop each. Source: `ULOE_UPGRADES_2026-11-04.md` §3; scope 10 [Questions] 1-2.
32. **Uloe's Supernova:** `~a SURGE button once a Tide~` / goes off by itself the first time the Power Bank is hit each Tide. Source: scope 10 [Questions] 3.
33. **Every other `~default~` in scopes 04 and 09-14:** `~take them all as written~` / name the ones to change. Source: PLAN Open questions 1, 5.

## E. Art, release, the PC and housekeeping

34. **Final sprites:** Release 1 (Yossele alone) waits for art: `~you draw Yossele's sheet (scope 07 has no request for it yet: Claude adds one)~` / Claude draws finals from the temporary art for you to edit / ship with the temporary art. Source: PLAN Part C "Release 1 state".
35. **The kit's mentions of mods from before the scope** (the Mummer template, Riptide and RogueLab as proof): `~keep, as the PR #78 rule already does~` / clean them out. Source: `KIT_CHECK_2026-11-04.md` "Left on purpose".
36. **`D:\TDS_second`** (1.2 GB, the second player's copy): `~keep it, round 3's two-player list hasn't run~` / delete (the runner makes it again). Source: `TEST_ROUND_2026-11-04.md`.
37. **Your PC's scope copy** (`Desktop\RogueWave\scope`) is behind the repository: `~sync it at the next PC round~` / leave. Source: `TESTING_ROUND_3_2026-11-05.md` §5.
38. **Scope wording leftovers** (12's "could be made Trinkets" note; 06 has no cost for stock Cardinals, they cost 15; 00's layout against 12; Spool's Cookie Tin role says Towerfighter, code says Tower): `~fix each as the report leans~` / leave. Source: quality passes 3-6.
39. **The scope audit's remaining rows** (appendix): `~take Claude's lean on each~` / name the ones to change.

**Things only you can do** (no answer needed, just a time): try clicking and selling a Stitched standing at its Cookie
Tin by hand, which the virtual click couldn't (`TESTING_ROUND_3_2026-11-05.md` §4); the card-hover stutter by hand
(`SMOOTHNESS_2026-11-04.md`); one online evening with a friend; turn off the connectors that fail to load (Figma, Notion,
Slack, Datadog and the rest).

## Appendix: the scope audit's other rows (question 39)

Already settled by PR #89: 11 row 3 (Cheap Refits now keeps Voice in a Bottle's discount) and 14 row 3 (Mind the Vessels
counts only each player's own vessel). Each row below names Claude's lean in `SCOPE_CODE_AUDIT_2026-11-04.md` §3.

| Row | What | Lean |
| --- | --- | --- |
| 04 row 3 | Bonus Tides' +10% starts only from Tide 30 | scope: say so |
| 04 row 4 | The modifier schedule and the stand-in elite | scope: add both |
| 08 Q1 | Efficient Recycler now raises the Thread refunded on a sell | as written |
| 09 row 6 | Seven in-game lines the review rewrote | scope: mark them unbuilt |
| 10 row 1 | A Mirror's level from Light | scope: "level 1 from any Light" |
| 11 row 4 | Made of Tougher Stuff never reaches Boutbro | code: skip only the player's own vessel |
| 11 row 5 | Full Gallop also grows Buster's hits | scope: say so |
| 11 row 8 | Compost Bin's "much larger radius" | code: reword the in-game text |
| 11 row 10 | Flash Bulb's look | scope: "flash bulb" |
| 13 row 3 | Illuminate lingers 3 s after leaving the aura | code: as Blind does |
| 13 row 4 | The four modifiers' empty in-game lines | scope: fill them |
| 14 row 1 | Mind the Meteor's white crystal | code: list as temporary art until drawn |
| 14 row 2 | Objective health "12 hits + 3 per map" | code |
| 14 row 4 | The objectives' looks (mop, stopwatch, ring, nest) | mark as not drawn yet |
| 15 row 1 | A Cardinal's Colour is its lobby slot | scope |
| 16 row 1 | Uloe missing from the side-by-side table | scope: add Uloe |
| Spool's play test | A way to sell a walking Stitched; the Thread Head's stock MOVE | waits while Spool stays as is |
