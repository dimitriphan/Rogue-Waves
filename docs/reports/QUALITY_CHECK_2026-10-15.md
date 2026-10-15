# Quality check and reorganisation (2026-10-15)

Dimitri asked (2026-10-15 03:24Z): "Quality check every asset made so far in the repository, then do it again.
Organize the repository in a way that makes it cleaner and more organized." This page is both passes, what was fixed,
what is left for Dimitri, what each open PR's thread should fix, and how to merge the open PRs into the new layout.
Base: `claude/phase-1-6lfnar` at PR #33's merge. Everything here was read or compiled in the cloud; nothing was run
in game.

## 1. How it was checked

Five readers per pass, one area each: A the kit (`TDS_Vessel_Creator/`), B the mod code (`TDS_Mod/ModSource`,
`Assets/*/Editor`), C the tools (`TDS_Mod/Tools`, kit `tools/`, `concept-tools/`, `.claude/`), D the project docs and
the scope (`docs/`, `CLAUDE.md`, `README.md`), E the four open PRs (#29, #30, #32, #34) and how they merge with this
branch. Each claim was checked against the code, the game's API dump, the decompiled game where the kit cites it, and
the round logs.

- **Pass 1** found 128 issues (A 20, B 23, C 23, D 30, E 32). The lead fixed the ones that change no game behaviour and
  reorganised the folders (§2).
- **Pass 2** re-read every area with pass 1's findings and the fix list in hand: it verified each fix, checked every
  moved file's pointers, read the files pass 1 skimmed, and challenged pass 1. It found 49 new issues (A 12, B 7, C 6,
  D 13, E 11), most of them small; the lead fixed those in scope too.
- **Pass 2 changed pass 1** in five places: B1 confirmed and widened (it also costs the next map its weather, B2-1);
  B2 lowered from H to M (only Argo installs the handler today); A7's fix was wrong (kit `13` never mentions Aging or
  Dazzling; the row now cites `15` §16); the pitfall-number plan A1/E1 collided with PR #34's #100-101 and was redone
  (§6); E18 (a dated report) lowered to L.
- **Compiled after the fixes** (cloud-check, stubs rebuilt): Argo (release; `ARGO_SELFTEST,ARGO_SHOTS`),
  RogueWavesMode (release; self-test), RogueLab (self-test), Riptide (three define sets; it failed before, C1),
  template Mummer, every editor folder: 0 errors, 0 warnings. Pass 2 also compiled each open PR merged with this
  branch: 0 errors once `build_stubs.sh` is rerun after #34.

## 2. The reorganisation

| Before | After | Why |
| --- | --- | --- |
| Eleven pages loose in `docs/` | `docs/reports/` (dated, frozen: both quality checks, the full test, the repo scan, the scope review, `SCOPE_VS_LOOP`, `SCOPE_GAPS`, `MECHANICS_CHECK`) and `docs/mode/` (the mode's design pages: `SEA_CHART`, `REJOIN`, `MAIN_MENU_START`, `MECHANIC_IDEAS`), each with a `README.md` | `docs/` now holds only living pages (PLAN, PACK, STYLE, PROJECT_SETUP, EXPANSION_SCOPE, RESEARCH_GAPS, scope, concepts) |
| `TDS_Mod/Handoff/Argo_next/Argo_next.md` (a plan, not a handoff) | `docs/reports/ARGO_NEXT_2026-10-08.md`, with a superseded note | Handoff folders hold only round packs |
| `README.md` described the kit at 16 chapters and the first week | Rewritten: what the repository is, the folder table, the 18 chapters, what stayed in `ModProjects` | It was the most stale page |
| `CLAUDE.md` map had four rows for loose pages | One row each for `docs/mode/` and `docs/reports/` | Shorter map, one place per kind of page |
| Root `.gitignore` had a duplicate block and a stray `!` line | One clean file; adds `*.meta`, `settings.local.json`, per-mod `Build/`, build logs, generated Aseprite | Nothing generated gets committed by accident |

Every pointer to a moved page was rewritten (121 edits plus four found in pass 2), **except** on purpose:
`TDS_Mod/Handoff/*/README.md` (six round records, history) and `docs/scope/` (ten pointers in `02`, `03`, `05`, `12`:
PR #32 rewrites every scope file and the project-files copy must stay equal to it; repoint after #32 merges, §6).
Identifiers (`Mode_RogueWave`, `RW_` keys), the PC folder `Desktop\RogueWave`, the kit's chapter numbers and the scope's
numbering 00-16 are unchanged.

## 3. Fixed in this PR (no game behaviour changed)

- **Kit docs**: chapter counts and map rows; `09`'s header on read-not-hit entries; `14` Q4 now verified; `13` Q1-Q6
  answered and the modes' starting aura stated as such; `15`'s modifier row; `03`'s art route now starts from the
  2026-10-15 extraction; `18`'s height range (19-28), the Fie and Claymore/Bulwark palette exceptions, a column header;
  `02`'s Leveladd range (+20 to +150); `VESSEL_SURVEY`'s AI list; the art survey's colour label; frozen-example
  headers on every `examples/code` file; the template's pointers.
- **Mod code (comments and notes only)**: wrong chapter pointers in `RogueWavesMode` (`08_Icons` → `09` #20, PACK §4,
  `docs/STYLE.md`), `09_Rejoin`'s "back on the boat" comment, Argo's SONAR comment and journal-history heading, round
  counts in the mode's notes, Argo's 2.0 note above the 1.4.0 layout table, `Deploy-Wait.ps1`'s menu path.
- **Tools**: Riptide compiles here again (C1: `ThirdParty.cs` gained the UI layout, `Button.onClick`, TMP and
  named-message stubs, shaped like the real types); `check_editor.sh` writes to the cloud-check folder and fails on
  error; `check_mod.py -w` works anywhere on the line; `make_handoff.py` never packs retired Riptide unless `--only`
  names it; the PC README stops a round on every bad apply result and trusts `Editor.log`'s `[ModKit]` lines over
  `queue_status.txt` (pitfall #76); `New-Vessel.ps1`'s `-join`; `build_mod_pages.py`'s root; `_PROBE` defines that no
  code uses dropped from the docs.
- **Project docs**: `STYLE.md` (Storm Coral is not the modifier colour; four modifiers; Uloe's electric arms; Spool
  keeps Loom/Thread), `PACK.md`, `PLAN.md` Part B (four modifiers, no release zip, Phase 5's order, done markers),
  `PROJECT_SETUP.md` (work lands on `claude/phase-1-6lfnar`), `RESEARCH_GAPS` C3, banners on `SCOPE_GAPS`,
  `SCOPE_VS_LOOP`, `MAIN_MENU_START` and the concepts page, `.claude/rules/` (art paths, concept pages, no `_PROBE`).

## 4. For Dimitri (changes to game behaviour, the kit's rules, or settings; not made)

**Answered 2026-10-15 04:18Z: "Use defaults for all."** The schedule is in `docs/PLAN.md` Part C Decisions
(2026-10-15); C5, C6, C10, C2-3 and C19 were done in this PR.

| # | What | Default if you say nothing |
| --- | --- | --- |
| B1 | **Kit bug, latent:** `ModKitTides` fires "Leave" only when the next map wakes, because `Game_Mapvars.Instance` still points at the destroyed map on the boat. After a mid-map leave the next map's end sees no next node, so the route doesn't grow and the map after gets no weather (B2-1). Nothing hits it yet: no campaign starts outside the self-test. | Fix it as kit 1.7 (`ReferenceEquals` check, fire Leave when the map object is gone) before the main-menu button ships |
| B2 | Build scripts rename "ModKit" inside strings too, so `ModKitPack.Mark("ModKitFocus")` differs per part and the "installed once per pack" guarantee fails. Harmless today (only Argo installs it). | Fix with the kit 1.7 bump |
| B3, B5, E2-4, E2-5 | Versions disagree: Argo `0.1.0-dev`, mode `0.2.1` (no `-dev`), #34's mode `0.3.0`, #30's Yossele `0.1.0-dev`; the editor prefabs export yet other numbers ("1.4.0", "0.1.3", "1.0"). PACK §4 wants one pack version. | One pack version read by both data and prefab, set when the pack is first built |
| B6 | PACK §4 says the mode shows a menu warning on a version mismatch; no code does. | Build it with the pack, or drop the sentence |
| B10 | The vessel template (Mummer) has no kit 1.6 pack calls; Spool and Uloe would start from it. | Add them before Spool is scaffolded |
| B12 | Four Argo self-test checks skip with a plain log instead of `TEST SETUP`, so a count can hide a missing check. | Switch them at Argo's next round |
| B14, B15 | Argo's Salt Sting journal line says 0.5/s where the code gives 1.0/s; `Stagger` is read but no 2.0 upgrade sets it. | Fix with the balance pass |
| B17, B18 | `ModKitTides` comment vs kit `15` §4 on a rewound entry; `ModKitCombat`'s header. | With kit 1.7 |
| C2 | The committed API dump was not written by the committed `api-dump.ps1` (it has properties, enum values, non-public types). | Update the script from the dump's format at the next PC session |
| C5 | `Run-SelfTests.ps1 -BootCheck` waits for an `icons:` line the mode only logs on failure. | Log `icons:` on success in the mode |
| C6 | The PC README says the token fetches handoffs; PROJECT_SETUP says the signed-in `gh` does. | `gh` first, token only without it |
| C10, C23, C2-3 | Small script bugs: PS 5.1 JSON arrays in `Apply-Handoff`, a half-unzipped folder counted as done in `setup.sh`, a log read race in `Run-SelfTests`. | Fix at the next tools pass |
| C19 | `.claude/settings.json` lacks the relative `check_editor.sh` command the docs tell sessions to type, so it prompts. | Add it (permissions are yours to change) |
| D1 | **The repo's `docs/scope/` and `/mnt/project-files/scope/` differ in every file** (and two names). | PR #32 makes the repo copy the newest; then copy it over |
| D13 | The default branch is still the stale `claude/rogue-waves-mod-setup-ngfuv3`. | Switch it to `claude/phase-1-6lfnar` in GitHub's settings |
| D2-3, D2-4 | `docs/scope/16`: Knight Commander's numbers are Pistol Whip's; Diesel Dog and Rev Dog show only the Garage's never-read "units +1"; Battalion and Black Sno miss effects. Same in PR #32. | #32's thread fixes them from `reference/VESSEL_SURVEY.md` |
| D20-D22, D26, D27 | Scope wording: `02`'s "Next", Yossele's speed note, "Beginner" (the game says Basic), `00`'s legend, two Cardinal wordings from the scope review. | Fold into #32 or your next scope edit |
| A17, A19 | Old Argo notes contradict pitfall #29; the API dump has no date or game-version line. | Leave; add a header line at the next dump |

## 5. For the open PRs' threads

| PR | Fix in the PR |
| --- | --- |
| #29 round 7 | E28 version comment; move `docs/BASE_VESSEL_CHECKS_2026-10-12.md` to `docs/reports/` with a row in its README and repoint its four pointers (E2-2); pitfalls → #89-91 (§6); its PLAN Log entry goes before 2026-10-13 (E29) |
| #30 Yossele | E7 (its new pitfall blames Argo's and the template's builders, but Argo's sheet is 192 wide: the template has the same bug, so fix it there too); E8-E11 stale round counts (round 8 is 98/0); E12 the 2026-10-13 renames (Forest Project, Grafted, Entangling Roots); E13 drop its `docs/scope/07` hunk (the base re-laid 07 in the template); E22 + E2-6 (Argo's Debuff throttle alone won't stop the vine-slow RPC flood: refresh near expiry); E2-10 dictionary sweeps; E23-E27 comments and a stale preview; E2-5 version; pitfalls → #102-104 |
| #32 scope | D2 mark the 2026-10-12 file-name decision superseded; D3/E14-E17 every pointer to `05` [Sea Weather] → `04` (PLAN, `docs/mode/SEA_CHART.md`, `docs/mode/MECHANIC_IDEAS.md:3`, `06_Campaign.cs`, `07_Weather.cs`); D2-3/D2-4 scope 16 numbers |
| #34 decompile | E2-3 (`PACK.md §4`, not §5); E2-4 version; E5/E6 rows answered by kit `18` since; E19 `Stop()` needs a `Live` check; E20 its own save keys break chapter 23's rule 3; E2-7 clear `waiting` per load; E2-8 vote ties and votes from players who left; E2-9 two text errors in 20 and 22; E31 `07` rule 9; E32 Argo notes title; E30 rerun `build_stubs.sh` after merging |

## 6. Merging the open PRs into this layout

**Order:** #32, then #29, then #34, then #30 (smallest conflicts first; #30 has the most rework and wants #34's kit).

**Pitfall numbers** (base holds #87-88): #29's three → **#89-91**; **#92 unused** (one line says so); #34 keeps
**#93-101** (its code cites them); #30's three → **#102-104**. Next free: **#105**. Kit chapters: #34 holds 19-24; next
free **25**.

**Conflicts** (from `git merge-tree` of this branch with each PR):

| PR | File | Resolution |
| --- | --- | --- |
| #29 | `TDS_Mod/ModSource/RogueLab/Code/01_Util.cs` | PR's version lines; keep `docs/reports/MECHANICS_CHECK.md` in the comment |
| #29 | `TDS_Mod/ModSource/RogueLab/README.md` | PR's round 7 row; paths `docs/mode/MAIN_MENU_START.md`, `docs/reports/MECHANICS_CHECK.md` |
| #29 | `TDS_Vessel_Creator/docs/09_PITFALLS.md` | PR's #85-86 rewrites; base #87-88; PR's 87-89 as #89-91 |
| #29 | `docs/PLAN.md` | Both "Now" lines and Log entries in date order; "#87-89" → "#89-91" |
| #30 | `CLAUDE.md`, `TDS_Mod/README.md` | Keep this branch's mode rows; add the Yossele row |
| #30 | `09_PITFALLS.md` | Base #87-88; PR's 87-89 as #102-104; update Yossele `IMPLEMENTATION_NOTES.md:61,129` |
| #30 | `docs/PLAN.md` | Keep base "Next" (laser and speed); add #30's items; round 8 98/0 |
| #30 | `docs/scope/07_Vessel_Yossele.txt` | Take this branch's file whole (E13) |
| #32 | `docs/PLAN.md` | Both entries, 2026-10-14 before 2026-10-15 |
| #32 | `docs/mode/SEA_CHART.md:155` (was `docs/SEA_CHART.md`) | "From `docs/mode/MECHANIC_IDEAS.md` §2; the scope entry is `docs/scope/04` [Sea Weather]." |
| #34 | `CLAUDE.md`, `TDS_Mod/README.md`, `RogueWavesMode/README.md` | PR's 0.3.0 text with `docs/mode/` paths |
| #34 | `RogueWavesMode/Code/00_RogueWavesModeMod.cs` | PR side with `docs/mode/REJOIN.md` and its `10_Online.cs` line; Version "0.3.0", comment cites PACK §4 |
| #34 | `TDS_Vessel_Creator/README.md` | Rows 18 then 19-24; merge the reference row (art survey + `network_surface.md`); keep "frozen Argo 1.x and Sprigg 1.x" and add the recipes sentence |
| #34 | `09_PITFALLS.md` | Base #87-88, then #93-101; "(87-92 are left to …)" → "(92 is not used.)" |
| #34 | `15_ROGUE_MODE_ENGINE.md` | PR text with `docs/mode/MAIN_MENU_START.md` |
| #34 | `docs/PLAN.md` | Both entries in date order; give bare `REJOIN.md`, `SEA_CHART.md`, `PACK.md` their paths |

Renames merge cleanly: #34's edits to `docs/REJOIN.md` and `docs/SEA_CHART.md` land at `docs/mode/`.

**Pointers that merge without a conflict but are stale** (about 20): after each merge run

```bash
git grep -nE 'docs/(REJOIN|SEA_CHART|MAIN_MENU_START|MECHANIC_IDEAS|SCOPE_VS_LOOP|SCOPE_GAPS|MECHANICS_CHECK|FULL_TEST_2026-10-11|QUALITY_CHECK_2026-10-11|REPO_SCAN_2026-10-08|SCOPE_REVIEW_2026-10-10|BASE_VESSEL_CHECKS_2026-10-12)\.md' -- ':!TDS_Mod/Handoff'
```

and repoint every hit (`docs/mode/` for the four mode pages, `docs/reports/` for the rest). Known ones: #34's kit
`19:113,142,201`, `22:9`, `23:177,223`, `24:164`, the mode's `IMPLEMENTATION_NOTES.md:13`; #29's `17:9`,
`RESEARCH_GAPS.md:72`, RogueLab README:42. After #32 merges, include `docs/scope/` in the grep (its ten pointers plus
#32's nine new ones) and copy the scope back to `/mnt/project-files/scope/`.
