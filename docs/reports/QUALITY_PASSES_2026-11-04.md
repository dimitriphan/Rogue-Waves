# Quality passes over the whole repository (2026-11-04)

Dimitri, in the project (20:18Z), picking from the coordinator's list of token-heavy checks: "Start with 1, 2, and 3".
Item 3 is this one: repeated passes over the whole repository until a pass finds nothing, at most six. At 23:22Z he asked
every thread to reach a good stopping point and pause, so **one pass ran**; it was not yet a clean pass.

**Scope.** Docs, the kit (`TDS_Vessel_Creator/`), the scope files, reports' index, READMEs, code comments, tool scripts,
`docs/TEMP_ART.md` and every file reference. Started from the base after PR #79 (kit check), then the base after PRs #80
(simplification build, kit 1.8) and #82 (Cardinals on later maps) was merged in.

**An issue** is a statement that contradicts the code or `docs/PLAN.md` Part C, a broken link or file reference, a stale
name (the pre-2026-11-04 Cardinal names), a stale status ("not built", "not run", "waits for online" for what has since
been built or run; "15 maps", "Swell"), a wrong version, or a dead file. Not issues: style, wording, and what PR #78 kept
on purpose (the kit's Mummer template, Riptide and RogueLab as proof, dated reports, the PLAN Log, Dimitri's own words).

## Pass log

| Pass | How | Found | Fixed | Left for Dimitri |
| --- | --- | --- | --- | --- |
| 1 | Scripts: every markdown link and backtick path against `git ls-files` (0 broken links; 559 path hits triaged by hand, 4 real), the old Cardinal names, stale-status phrases, version strings, files nothing names. Then six readers, one per area (root and `docs/` pages; mode and foes; Yossele and Spool; Argo and Uloe; tools and kit; scope), each claim checked against the code; every finding checked again before a fix | 127 | 120 | 7 (below) |

Pass 2 did not run (Dimitri's pause). The next pass should start with the phrases pass 1 kept meeting (below), across
every page, since a fact written in several pages goes stale in the ones a thread doesn't open.

## What pass 1 fixed, by area

| Area | Fixed | The kind of thing |
| --- | --- | --- |
| Scope (`02`, `03`, `04`, `07`, `08`, `12`, `16`) | 9 | the F-key fix runs with all four vessels, not two; Spool "nothing built yet"; Yossele and Spool self-test counts (173, 133); Cardinals past 8 "settled when the shop is built" (they are counted in the run line); scope `16`'s side-by-side table lacked Uloe; the modifiers' default against `21_Difficulty.cs`; "the rest of the mode" after Release 1 (only Depth is left, on hold) |
| `docs/PLAN.md` Part B and C (base entries) | 10 | the button, the vote, kit 1.7 and the mode's new checks still listed as to do; the F-key check and the chance-based upgrades (both passed 2026-11-04); "Phase 5 pieces" saying online, rejoin and the 10-map campaign wait (they ran with a second player and on ten real maps); Open question 2 citing a `! NEEDS !` line scope `08` no longer has; Open question 7's number reused; Phases 4 and 5 with no built mark; the `.info` "1.0" |
| `docs/mode/`, `docs/RESEARCH_GAPS.md`, `docs/PACK.md`, `docs/EXPANSION_SCOPE.md`, `README.md`, `CLAUDE.md` | 27 | the mode index ("in-map vote still to build", "main menu not built", "compiled only"); the sea chart's islands (the base game's own now), the leader steering "until the vote exists", weather pay "once Pure Aura exists", the billboard "not built"; rejoin "compiled, not run"; kit 1.7 "at Phase 5"; `ModKitTides` "no break event"; `Has(vessel, name)` (it takes the owner too); A8 "nothing ran on a client"; tips "only on the end screen"; the README's harder difficulty (Depth, on hold) |
| `docs/TEMP_ART.md` | 8 | Yossele's and Spool's sheets "next PC round" / "not yet seen" (in every build since); "PR #42's branch" (merged); the Photon "needs `FileArt`" (`UloeBuilder` has it) |
| Mode and foes (code comments, README, art READMEs) | 17 | comments saying the button, the vote, Pure Aura and bonus Tides past 30 are later pieces; a comment citing a report that never existed (`UNTESTED_FEATURES_2026-11-04.md`) and a `-Solo` switch `Run-TwoPlayers.ps1` never had; the world name's `r<retries>`; Cardinal stacking "with no limit" (the code makes 6 copies); the foes build script calling itself the mode's; the icon file's 15 frames; the islands; the dialog's rows |
| Uloe | 9 | "self-tested once" (148/0 since); Long Throw 0.6 tile each side, not half; the self-test runs one mark route, not both; what counts as a node; Direction icons "until the mode is rebuilt"; the Photon's file override (it exists; also in `concept-tools/minions/readmes.py`, which writes that README) |
| Argo | 10 | status counts (142/0, a second player); kit 1.6 headers; "scope has no Argo Directions" (it has four of Claude's defaults); the 1.4.0 code map's preface; the translators' sheet `Argo_Localization.csv` and the effect and sound `Use` texts still naming Fathoms Below, Harpoon Tower, Diving Bell and Deep Drill (names and descriptions only, nothing the game reads) |
| Yossele | 10 | status counts, the play build parked since 2026-11-03; Dandelion Season and Mother of Mushrooms "not built"; the code map missing `97_Measure.cs` and `98_Play.cs`; the footstep frames; "Spaghetti Roots" (Entangling Roots) in two effect comments |
| Spool | 12 | the code map (133 checks, `97_Measure.cs`, `98_Play.cs`); the Thread Head token's name; "Yossele is the play build there"; pitfall #113's check and the sheets "not yet run / built"; the Yarn Stash's `! NEEDS !` line and "no capstones" (on the sheet since 2026-11-04, not built); the Directions "not built" (all three are); the art README's PR #42 branch; `ASSETS.md` "nothing seen in game" |
| Tools and kit | 8 | `Tools/pc/README.md`: two paths broken by a tab byte and a lost backslash, the Mods folder's play build (now every part's release build), "no Pack step yet"; `Run-TwoPlayers.ps1`'s START line; `ModKitDirections.cs`'s signatures (both kit copies, still equal); kit `20` §5's vote recipe against the built vote |

## Left for Dimitri (not fixed)

1. **Cardinal stacking.** Scope `12` says Cardinals and Trinkets "stack with no limit"; the build gives a pack Cardinal or
   Trinket 6 working copies (a 7th costs Pure Aura and adds nothing) and a stock Cardinal can be held once
   (`RWCardinals.Stacks`, `Buy`). A note on the scope line says so; which one changes is his call.
2. Scope `14`: the objectives' `~Kind~` defaults (Defence, Speed, Hunting) are not the template's kinds (Hunt, Guard,
   Hold, Rush, Mining). Claude's defaults, so his to take or rename.
3. Scope `12`'s Trinket note still says about 24 stock Cardinals "could be made Trinkets", while every stock Cardinal on
   sale is a Cardinal. True as a fact about the game, so left.
4. Scope `06`: a Cardinal's cost "is on its own sheet in 12", but stock Cardinals have no sheet (they cost 15 Pure Aura,
   `RWCardinals.StockCost`).
5. Scope `00`'s layout rules (Status last, no dates outside `02`) don't match where the sheets put Status and two dated
   notes in `12`. Format, not fact.
6. Spool `DESIGN.md`: the Cookie Tin's role column says Towerfighter; the code tags it Tower only. Possibly a description.
7. The project-files copies of the scope (and the Scope Editor) now differ from the repository in `02`, `03`, `04`,
   `07`, `08`, `12` and `16`. Overwrites there need his word.

## Found on the way, not a docs issue

**The base branch no longer compiles in the cloud.** After PR #82 merged on top of PR #80 (kit 1.8), the mode's release
build has 6 errors (`17_Cardinals.cs` calls `RWUtil.Folder`, `Load` and `IsServer`, which kit 1.8 moved), its self-test 10,
and the self-test builds of Argo (294), Spool (151) and Uloe (183) fail the same way (`ArgoUtil.Stat`, `Pos`, `Flat`...).
At `06365ae` (#80 alone) all of them were 0. This PR changes no code that the compiler sees, and gives the same counts as
the base: the fix is code, so it is left to a code thread.

## Cloud-check

`TDS_Mod/Tools/cloud-check/setup.sh` once. Before the base merge: release and self-test builds of all six parts 0 errors
(the mode's self-test 1 warning, already there), the nine editor folders clean, the recipes 0. After it: this branch's
counts equal the base's, part by part (above); Yossele and the foes 0 errors in both builds; every editor folder clean;
recipes 0. The edits are comments and descriptive text; the one generated file they change is Argo's translators'
sheet, which the game never reads (kit `docs/21` §3.1). Nothing was run on the PC.
