# Quality check of the lessons and resources (2026-10-11)

> **Note (added 2026-10-15):** the kit has 24 chapters now, and the scope was re-laid on 2026-10-14
> (`docs/scope/00_Read_Me.txt`), so §3's scope line numbers no longer match. The next check is
> `QUALITY_CHECK_2026-10-15.md`.

Dimitri asked (2026-10-11 21:22Z) for a quality check of every lesson and resource made so far. This pass builds on
the kit accuracy review of 2026-10-09 (PR #12, commit 0ad9c04) and checks what was written or left stale since,
against the settled decisions (`docs/PLAN.md` Part C "Decisions", `docs/scope/`) and the PC round results. Kit paths:
`docs/NN` = `TDS_Vessel_Creator/docs/`.

**What was read:** all 16 kit chapters, the kit and template READMEs, `DESIGN_TEMPLATE.md`, `examples/`,
`reference/VESSEL_SURVEY.md`; every file in `docs/` and `docs/scope/`; the READMEs and notes under `TDS_Mod/`
(Argo, RogueWavesMode, RogueLab, Riptide, cloud-check, pc, aseprite); `.claude/rules/`; the `concept-tools/`
docstrings. Mechanical checks: every relative link and backticked path, every `docs/NN §X` reference, every pitfall
number cited, about 400 game API names grepped in the API dump, `ModSource/Shared` vs `kit/Shared`, and the scope and
research copies in the project files.

**Result:** the kit's engine facts hold. No API name is missing from the dump, every `§` and pitfall reference
resolves, the Shared kit copies are byte-identical, and the project-files copies of the scope and the research pages
match the repository. What had drifted was **status**: pages written before the RogueWavesMode rounds (2026-10-11)
still said "untested", "PC round pending", "three maps", "fallback" or "ascension". All of that is fixed in place
below. Three scope questions need Dimitri (§3); a few calls are left for later (§4).

## 1. Fixed in place

| File | Was | Now |
| --- | --- | --- |
| `docs/15` §1, §3 | "Three maps in a row" chained without the boat; map vote "fallback, one of three"; shop "fallback"; ascension | 10 maps, back to the boat and sailed on the chart (proven, §17); vote go (V1, 1-3 linked nodes, not built yet); VENDCINT proven solo; Depth (on hold) |
| `docs/15` §5, §6, §8, §9, §11, §13, §14.5, §16, §17, header | Fund Transfer untested; chain to the voted map; `RW_Ascension` example; `Mode_RogueTest`; Setup switch as the fallback for a mod Cardinal; rejoin 0.2.0; round 4 only; world in code untested (cited `SEA_CHART` §7) | proven solo (F1); sea chart route; `RW_Depth`; `Mode_RogueLab`; fallback for the main-menu button; 0.2.1, 47/0, online left; rounds 4-5; proven by round 1 (`SEA_CHART` §9); header names RogueLab 1-6 and RogueWavesMode 1-6; `ModKitFocus` named |
| `docs/16` intro, §3, §9 | "Nothing tried in game"; spawning map objects untested; flood block "may not ship" | the flood, nest and host `Time_Scale` verified (rounds 1-4); "the game ships no flood block", the stand-in named `Block_Flood_Deter` once the map has loaded; PC list items 1 and 4 marked done or half done |
| `docs/13` §10 | Superheated without attack speed; Tsunami "4 long Tides, 240 s first break" | attack speed 1 (×2); 4 Tides of 180 s (last 9999 s), breaks 240 s then 180 s (round 2 TIDE2 lines) |
| `docs/09` | Argo's notes cited pitfalls "40j" and "40k", which were never carried over | new **#79** (overlapping slows and a -0.9 root) and **#80** (one exception disables a brain); header rule now "next number in §J", as `.claude/rules/kit-docs.md` says |
| `template/DESIGN_TEMPLATE.md` | cost bands 45-450, 100-1750, 1500-5000 | `docs/02` §4's shipped ranges: 25-500, 75-750, 1500-5025 |
| `docs/12`, `docs/10`, `docs/11`, `docs/14`, `docs/04`, `docs/08` | Argo's "Diving Bell"; "none of the three mod vessels"; "soft rim only after Smacky"; no Build Queue menu item; "PACK.md in that repository"; "nothing tried" | Miner's Base (1.4.0's Diving Bell, limit 1); "no mod"; "only when Smacky is the reference"; Build Queue listed; "the repository's `docs/PACK.md`"; the load order was verified; template helper order flagged |
| `kit/README.md`, `ModKitDirections.cs` (both copies, comment only) | "the F-key fix RogueLab proved"; 1.6 shipped only in Argo; header said a respawn can drop a Direction and cited `docs/15` §9 | the fix ran, the bug never reproduced; RogueWavesMode 0.1.0-0.2.1 added; a respawn keeps upgrades, per-player Directions go on the vessel in kit 1.7 (`docs/15` §14.1) |
| Kit `README.md`, `reference/maps/README.md`, `examples/` | docs/15 row "three-map chain"; `reference/` row missing rounds 2-3; maps "no snapshot yet: kit 1.5 writes one"; Sprigg's rim not marked as a departure; Argo 1.0 notes not marked as history | fixed; Argo 1.0's notes now open with "History: no SONAR in 2.0, journal is the kit's" |
| `CLAUDE.md`, `README.md`, `TDS_Mod/README.md` | RogueWavesMode 0.1.0 "PC round pending"; no rows for SEA_CHART, REJOIN, MAIN_MENU_START, MECHANICS_CHECK, the reviews; "15 docs"; Riptide 0.2.3 "70 of 70"; no `Handoff/` row | 0.2.1 with its rounds; rows added (72 of 80 lines); 16 docs; 70/70 was 0.2.1; `Handoff/` row |
| `docs/PLAN.md` | Part C "Now" was 75 lines (Part A says three); Phase 5 checklist said "PC round pending"; superseded decisions unmarked (15 maps, 10 Any + 6, Swell, any quit, extra Directions at the shop, chaining map 1); PR #24 "ready" | "Now" is three lines, the old entries moved to "Log"; checklist marks sea weather and rejoin done, adds the main-menu button piece; superseded decisions marked; PR #24 merged; Q1-Q3 opened |
| `docs/mode/SEA_CHART.md`, `MAIN_MENU_START.md`, `REJOIN.md`, `RESEARCH_GAPS.md`, `MECHANIC_IDEAS.md`, `EXPANSION_SCOPE.md`, `STYLE.md` | "none of it tried in game"; "compiled, not yet run"; flood "`Block_Flood_Deter`" as if stock; lobby "stays closed through the chain"; mod World "untested"; `Game_Load.cs:311`; A2 aura still "his call"; online list without the rejoin and chart checks; ideas "none adopted"; menu button "left to learn"; modifier skins "undecided"; Cardinals "none named yet" | proven solo with round numbers; the stand-in explained; the pack reopens the lobby for a dropped player; built (`SEA_CHART.md`); `:261`, as kit §14.5 was corrected; base aura only; online list adds REJOIN O1-O8, chart C7, the friend join and client `Time_Scale`; sea weather adopted; button proven; effect, not skin (scope 13); Vampire Dentures |
| `docs/reports/SCOPE_GAPS.md`, `SCOPE_VS_LOOP.md`, `REPO_SCAN_2026-10-08.md`, `concepts/YOSSELE_SPOOL_ULOE.md`, `SCOPE_REVIEW_2026-10-10.md` | dated pages with no word on what was decided since | a dated note at the top of each listing what changed; the review's ten answered rows say so |
| `docs/scope/02_Progress.txt` (Claude keeps it), `05` | "a PC round will prove it" (x3); no rejoin line; links to `SEA_CHART` §6 and §7 | proven, with the rounds; a rejoin line; §7-9 and §8. Project-files copy updated; the PC copy too (§5) |
| `TDS_Mod/Tools/` and `concept-tools/` | cloud-check README in the ModProjects voice (Rustle example, "your unzipped copy"); `Get-PcManifest.ps1` named a missing `Send-ToCloud.ps1`; `sea_chart.py` named `04_Chart.cs`; "all need Pillow"; Riptide cited a `reports/` folder this project doesn't have; RogueLab README's rounds out of order and stopping at round 3; Argo 2.0 notes headed "in progress, not run" | Argo example and `../../../TDS_Vessel_Creator`; clause dropped; `03_Chart.cs`; only `sea_chart.py`'s PNG needs it; "in the ModProjects project's files"; rounds 1-6 in order; heading says round 6 139/0 |

## 2. Checked and clean

- Elite numbers (Hardened ×4, Tempered ×2, Fractured ×15/×10, Annealed, Molten ×4 health and ×1.15 speed, Melting),
  Treated as the easier setting, "never offer Special_Custom", `Changemenu("Confirmation")` before `Runprompt`, "the
  base game scales nothing by players", "a respawn keeps upgrades": all match the round logs and the code notes.
- PC round counts agree everywhere (RogueLab 13/0 to 22/0, RogueWavesMode 34/0 to 47/0, Argo 139/0).
- No live text still says sonar, Swell, 15 maps, rank prices 15/25 or 30 s mode breaks; Argo's docs keep SONAR only
  as unused code or 1.4.0 history.
- Kit 1.6 everywhere; every `kit/Shared` header matches the version table.

## 3. Questions for Dimitri (answered 2026-10-11 21:54Z, folded in)

His answers: (1) Cardinals scale forever without limit, split into Team Cardinals and Vessel Cardinals (was
"Personal"); (2) yes; (3) yes. `docs/scope/04`, `05`, `06`, `08`, `10`, `12` and `14` now carry them, and the smaller
wording below too.

| # | Question | Default |
| --- | --- | --- |
| Q1 | `12_Cardinals`: the Cardinal row holds 8, so "at most 8 Team Cardinals", but Cardinals "stack with no limit". Does each copy of a Team Cardinal take a slot? | Copies of one Team Cardinal share its slot (the mod counts them), so at most 8 different Team Cardinals; how the row shows a stack is checked in game |
| Q2 | `04` line 9 says a dropped friend "sits out the rest of that map and comes back on the boat", but the rejoin he asked for (`03`, `docs/mode/REJOIN.md`) brings them back into the running map | Reword `04`: they can rejoin the running map; a drop while the map loads comes back on the boat |
| Q3 | Fold in the scope review's six wording fixes (`SCOPE_REVIEW_2026-10-10.md` #8 Pure Aura's sources, #17 one wording for "after clearing Tide 15" and Depth, #21 VENDCINT in the opening break, #30 `[Spool]` brackets, #32 Uloe's light check is answered, #52 Mining as an objective kind)? | Yes |

Smaller scope wording, folded in with Q3 if he likes: `06` line 5 reads "about 7 for every vessel" where `11` says
"7 Any"; `04` line 8 ("quitting counts as losing") reads before line 9's "only the host".

## 4. Left as they are (a call for later, not an error)

| Item | Why it waits |
| --- | --- |
| "Exactly 4 buildables" (kit `README.md`, `docs/02` §3, `docs/10`, `DESIGN_TEMPLATE.md`, `.claude/rules/design.md`) while Argo 2.0 has five | The rule stays 4; Argo's fifth (a `Command_Buildoption4` clone, verified in round 2) was Dimitri's exception. Record it in `docs/02` §3 when the next vessel's design gate asks |
| Pack version: `docs/PACK.md` §4 says dev parts carry the pack version + `-dev`; Argo is `0.1.0-dev`, RogueWavesMode `0.2.1` (its code cites `PACK.md` §5 for versions, which are §4) | Settle when the pack is first assembled (Phase 7) |
| `docs/05` and the template don't mention kit 1.6's `ModKitPack`, `ModKitTides`, `ModKitCounter`, `ModKitFocus` | Write it with the first new vessel (Yossele), when a pack vessel first uses them |
| `TDS_Mod/Tools/pc/Run-SelfTests.ps1` leaves a self-test build installed but disabled, while the Mods folder is kept empty between rounds | The PC sessions remove it by hand (pc README); change the script on the next PC round |
| `concept-tools/build_mod_pages.py` writes to `concepts/<slug>/`, the rules say `docs/concepts/` | A code change; make it when a mod page is next built |
| `.claude/settings.json` allows the other concept-tools scripts but not `sea_chart.py` | A permission change; Dimitri's call |
| `docs/PROJECT_SETUP.md` names the default branch, which is far behind the integration branch `claude/phase-1-6lfnar` | Merge the integration branch into the default, or name it in the page: Dimitri's call |
| `docs/02` lists a "Simple" difficulty no shipped vessel uses; the Mummer template prices a tier-2 upgrade in two colours, which `docs/02` §4 allows only from tier 3; kit `README.md` says "three vessels" where `CLAUDE.md` says six; the kit `README.md`/`docs/04` give the `ModKit` → `<Name>Kit` rename a reason `docs/15` §10 makes moot | Small rule questions for the next design gate |
| `docs/mode/MAIN_MENU_START.md` §2 cites pitfall #25 for cloning menu items; #23 is the closer match | Both are about cloned journal tabs; left |

## 5. Copies

`docs/scope/02` and `05` were copied to `/mnt/project-files/scope/` and the PC's `Desktop\RogueWave\scope`; the two
`research/` pages (`SCOPE_REVIEW_2026-10-10.md`, `MECHANIC_IDEAS.md`) to `/mnt/project-files/research/`.
