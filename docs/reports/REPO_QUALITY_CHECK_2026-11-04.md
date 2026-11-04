# Repository quality check, 2026-11-04

> **Stage 2 done, 2026-11-04 17:30Z, on the base with PRs #71 and #72 merged:** fixed in place S1-S12, S14, O1, M1, Q1
> and Q2 (Argo's lines now also cite the test round's 142/0). S13 was already fixed by PR #71. Q1 went further than listed:
> the stubs gained every TMPro member the mode uses, so its release, `ROGUEWAVESMODE_SELFTEST` and `_GAPS` builds
> compile at 0 errors; `_TWOPC` (Netcode's transport, the chat box) still compiles only on the PC, now said in the
> cloud-check README. O3 is a note in the mode's `IMPLEMENTATION_NOTES.md`; M2 still waits for Dimitri's word. S12
> moved Part C "Now"'s Done list word for word into "Log" and left a ten-line state in its place. Below is the report as
> written in stage 1.

Dimitri, 2026-11-04 05:41Z: "If all threads are idle, begin tidying up the repository while doing a quality check of
the resources in the repository." Two threads were still running (Uloe's upgrades, the untested-features PC round), so
this runs in two stages: **stage 1** (this report) reads only; **stage 2** does the moves, deletions and rewrites once
those threads' PRs have merged, so nothing collides with them.

**Checked:** every tracked file (1,155) on `claude/phase-1-6lfnar` at `c987d4f` (PRs #69 and #70 merged).
**Issue** = a wrong or stale fact in a living page, a broken path, a duplicate or orphaned file, or a name or number
that differs between the scope, a report and the code. Style is not an issue. Dated reports in `docs/reports/` are
frozen: a stale line inside one is not counted, only a living page that points at the wrong thing.

**How:** scripted scans (stale terms, every Markdown link and every backticked repository path resolved, MD5 of every
file, files nothing names, temporary art that no code loads), every mod and editor folder compiled with
`TDS_Mod/Tools/cloud-check`, then the living pages read against the code and the newest reports.

**Result:** 21 issues, none that breaks a build or a self-test. 14 are stale lines in living pages, 3 are files
nothing uses, 2 are tooling gaps, 2 are small mismatches between the scope and the code. Section 7 lists what was
checked and found clean.

## 1. Compiles

| Part | Release | Self-test build | Editor |
| --- | --- | --- | --- |
| Argo, Yossele, Spool, Uloe, RogueWavesFoes, Riptide | 0 errors | 0 errors | clean |
| RogueLab | stops on its deliberate `#error` (research only, as documented) | 0 errors | clean |
| RogueWavesMode | 2 errors, both false (Q1) | 2 errors, both false | clean |
| `Assets/ModKit/Editor` | | | clean |

`TDS_Mod/ModSource/Shared` and `TDS_Vessel_Creator/kit/Shared` are byte for byte equal.

| # | Issue | Where | Stage 2 |
| --- | --- | --- | --- |
| Q1 | The cloud stubs lack `TMPro.TextAlignmentOptions` and `GridLayoutGroup.Axis`, so the mode shows 2 errors here that the game never sees (`11_Dialog.cs:154`, `:163`); its notes still say "9 errors as the base" | `TDS_Mod/Tools/cloud-check/stubs/ThirdParty.cs`; `RogueWavesMode/IMPLEMENTATION_NOTES.md` top entry | add the two members, rerun `build_stubs.sh`, so the mode compiles at 0 |
| Q2 | `TDS_Mod/Tools/cloud-check/README.md` says "`apt-get update` first", but `setup.sh` only checks for mono; in a fresh container `apt-get install mono-devel` fails without it | `setup.sh` line 8 | say so in the script's error message |

## 2. Stale lines in living pages

| # | Says | True now | Where |
| --- | --- | --- | --- |
| S1 | Argo 2.0.1 "compiled only" / "not yet run" | Built and self-tested on the PC, 136/0, in the 2026-11-03 round (`BUILD_2026-11-03.md` row "Argo, with its Directions"); only its online look is unseen | `CLAUDE.md` Argo row; `TDS_Mod/README.md` Argo row; scope `09` Status |
| S2 | The live kit is 1.6 | Kit 1.7 since 2026-11-03 (`kit/README.md`) | `README.md` (TDS_Mod row); `TDS_Mod/README.md` Shared row; `TDS_Vessel_Creator/README.md` kit/Shared row; `.claude/rules/mod-code.md`; `CLAUDE.md` PACK row ("kit 1.6 scope"; PACK §3 is now "Kit 1.6 and 1.7") |
| S3 | The repository's vessels are Argo and Yossele | Spool 0.1.0-dev, Uloe 0.1.0-dev and the foes part are built too | `README.md` first paragraph and TDS_Mod row; `TDS_Mod/README.md` first paragraph ("Argo is the first vessel here") |
| S4 | Mods folder: "normally none … Yossele's play build stays installed" | `TDS\Mods` holds every part's release build, all enabled (2026-11-04 05:35Z; `BUILD_2026-11-03.md` §9 and PLAN Part C) | `TDS_Vessel_Creator/README.md` "Facts about this PC", Installed mods |
| S5 | The project instructions to paste still carry "Write findings to files, keep chat replies under 150 words, ask questions only at gates" | Dimitri removed that sentence 2026-11-04 03:42Z | `docs/PROJECT_SETUP.md` §2 |
| S6 | Status "2026-10-08, rows updated to 2026-10-15"; enemies, the mode's shop and Cardinals are "read" | The foes part, the shop, Directions and Cardinals are built and seen in game (2026-11-03) | `docs/EXPANSION_SCOPE.md` status line and rows 2, 3, 6, 7 |
| S7 | Uloe's temporary art lives in `ModSource/Uloe/Previews/` | No such folder: Uloe's art is drawn by `Assets/UloeMod/Editor`, nothing is on file | `docs/TEMP_ART.md` Uloe row |
| S8 | The reports index | Misses `ARGO_REVIEW_2026-11-04.md`, `BUILD_2026-11-03.md`, `DIALOG_PLACEMENT_2026-11-03.md` and the `balance_argo_2026-11-04/` folder | `docs/reports/README.md` |
| S9 | Kit 1.7's row: "Rogue Waves parts on `claude/rogue-waves-mode`" | The parts live on the default branch `claude/phase-1-6lfnar` | `TDS_Vessel_Creator/kit/README.md` versions table |
| S10 | "kit 1.6 has no ranks" (why Yossele's Compost Bin is one Direction per rank) | Kit 1.7 has `Rank`; the code still works, the comment is behind | `Yossele/Code/02_Data.cs:101`, `04_Content.cs:662` (comment only; a rewrite to ranks is a build, not tidying) |
| S11 | Mode notes: release and self-test give "the same 9 errors as the base" | 2 now (Q1) | `RogueWavesMode/IMPLEMENTATION_NOTES.md` top entry |
| S12 | `docs/PLAN.md` Part C "Now" is one 100-line paragraph whose "Done" runs from 2026-10-08 to today | Part A wants "Now" short and the history in "Log" | `docs/PLAN.md` lines 229-306 |
| S13 | Scope `10`'s stat slots (Health, Speed, Damage, Range, Attack speed) are empty | Uloe is built at 30 / 2.25 / 2 / 3 / 0.5 (`Uloe/DESIGN.md`, `02_Data.cs`) | scope `10`; the Uloe upgrades thread owns that file now, so this goes to it, not into stage 2 |
| S14 | The Argo diver concept pages are listed in `CLAUDE.md` as "takes to pick from" | Dimitri 05:31Z: "Keep the original for now"; they are an archive | `CLAUDE.md` concepts row |

## 3. Broken paths

Every Markdown link resolves. Of the backticked repository paths, the misses are PC-only folders (`Assets/_Parked`,
`ModProjects` vessels, `research/` in the project files) or dated reports' old paths, which are frozen. One living
page points at nothing: S7.

## 4. Files nothing uses, and duplicates

| # | Issue | Stage 2 |
| --- | --- | --- |
| O1 | Seven icons of the Cardinals removed by PR #66 are still in `RogueWavesMode/Art/Temporary/game_size/` (`Cardinal_Bell`, `_Clock`, `_Hull`, `_Legs`, `_Puffer`, `_Rising`, `_Wind`). No code names them, but the prefab builder bundles every file in that folder, so they ship as dead weight | delete the seven |
| O2 | Argo 1.4.0's concept page and assets (`Argo/Concept/`, 28 files) and the round 5 previews (`Argo/Previews/round5/`, 20 files), kept beside 2.0's | keep: `DESIGN_1.4.0.md` and the frozen `ARGO_NEXT` report cite them; listed so nobody mistakes them for 2.0's look |
| O3 | `TDS_Mod/ModSource/RogueWavesMode/Code/17_Cardinals.cs` `NotSold` names four ids that are not among the game's 40 Cardinals (`Artifact_Moon`, `_Loopedmirror`, `_Sand`, `_Bottomrock`; Bottom Rock is `Artifact_Rock`, listed too) | harmless, the shelf never sees them; leave the code, note it in the mode's notes for the next mode build |

Duplicates (62 groups by MD5) are all deliberate: the kit's two `Shared` copies; temporary art where Dimitri's pick is
both the wired file and its `variants/v<n>/` original (the picks page reads the variants); the model snippets that
`concept-tools/temp_art/variants_models.py` copies from `model_variants/`; and the balance simulator, copied into each
of the four `docs/reports/balance_*` folders so each report reruns on its own numbers (`sim.py` differs between them).
None is removed.

## 5. Scope, reports and code side by side

| Checked | Result |
| --- | --- |
| Each vessel's stats: `DESIGN.md` vs `02_Data.cs` (Argo, Yossele, Spool, Uloe) | match |
| Scope `07`, `08`, `09` stats vs code | match (`08`, `09` as `~defaults~`); `10` empty (S13) |
| Directions in the mode's code vs scope `11` | all 12 built ones are on the sheet; the other 43 are unbuilt Claude `~defaults~` or vessel Directions built in the vessels' own code |
| Cardinals in code vs scope `12` | Vampire Dentures only, as `12` says; the 15 new Claude Cardinals are unbuilt |
| Names in the two Directions/Cardinals reports vs scope `11`/`12` | every name found, except stock Cardinals and the five removed on purpose |
| The shop's left-out stock Cardinals, code vs scope `12` | same set, but see O3 |
| Pack and dev versions | every part's code says `0.1.0-dev`, `pack.txt` says `0.1.0`; but Spool's and Uloe's dev prefabs still export `.info` version `"1.0"` (`SpoolModPrefab.cs:110`, `UloeModPrefab.cs:110`), unlike Yossele, Argo, the mode and the foes (M1) |
| Mode constants (breaks 60 s, 3 retries, win at Tide 15, 3 on the shelf) vs scope `04`/`06`/`12` | match |

| # | Issue | Stage 2 |
| --- | --- | --- |
| M1 | Spool's and Uloe's dev `.info` versions say `1.0` | set both to `0.1.0-dev` with the PACK.md §4 comment, as Yossele's (an editor-only change; the release pack sets its own) |
| M2 | The project files' scope copy (`/mnt/project-files/scope/`) is behind the repository in 12 of 17 files (`00`, `02`, `04`, `06`-`15`); `11` and `12` by hundreds of lines | outside the repository: waits for Dimitri's word to overwrite (already on his list) |

## 6. The PC lock

`/mnt/project-files/pc/QUEUE.md` now says HELD by "Test all untested mod features" since 05:45Z. Its old FREE line,
kept as history, still describes the Mods folder of 2026-10-29 (Yossele alone); the next thread that frees the lock
writes the current state.

## 7. Checked and clean

- **"15 maps" and "Swell":** only in dated Decisions and Log entries marked superseded, in frozen reports, as the
  palette colour Deep Swell, as Argo's `Swell` model part and in the Riptide prototype. Nothing living says either.
- **The removed Cardinals** (Spiny Hull, Sea Legs, Rising Tide, Puffer Float, Tide Clock, Rusted Bell, Second Wind;
  and Iron Rivets, Siren Conch, Kelp Net, Lucky Geode, Storm Lantern): named only as history (scope `02`, `12`'s
  note, PLAN, the mode's notes and code comment, the reports). Only their icons are left (O1).
- **Old file names** (`roguewave.txt`, `15_Vessel_Template.txt`): history only.
- **Pitfalls** run 1-138 with only #92 unused (as PLAN says); next free #139 and kit chapter 25 match PLAN.
- **Privacy:** no base-vessel game sprite is tracked; the art dump stays in the project files.
- **`CLAUDE.md`** is 78 lines (limit 80) and every path in its map exists.
- **Orphans:** apart from O1, every document is named by another, and the rest are generated logs inside report folders.

## 8. Stage 2 plan

After the Uloe upgrades and untested-features PRs merge, on a fresh base:

1. Fix S1-S9, S11, S14 in place (one line or one row each), S6 as a new status line and four row updates.
2. S12: move "Now"'s "Done" entries older than 2026-11-03 into "Log" (word for word, newest last), leaving "Now" a short
   paragraph that points to Log; done last, after the other threads' Part C entries are in.
3. Delete O1's seven icons; M1's two version strings; Q1's two stub members and Q2's message; then compile every
   mod again.
4. Hand S13 to the Uloe thread if it is still open, or fill it as `~defaults~` from `Uloe/DESIGN.md` if not.
5. PLAN Part C: existing entries first, this one last; one PR, Dimitri merges.

Left alone on purpose: frozen reports, the `variants/` folders, the balance folders' copies, `TDS_Mod/Handoff/` (the
record of every PC round), Argo 1.4.0's concept files, anything in `/mnt/project-files` (M2).
