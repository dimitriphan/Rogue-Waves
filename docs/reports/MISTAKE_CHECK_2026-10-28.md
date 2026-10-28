# Mistake check (2026-10-28)

Dimitri asked (2026-10-28 04:01Z): "go through the repository and check for mistakes." Base: `claude/phase-1-6lfnar`
at 500381f (PRs #2-#39, #41, #44 merged; #40, #42 and #43 open and not in the tree). Two passes, as set up front;
everything here was read, compiled or run as a script in the cloud, nothing in game.

## What counted

An issue is something **wrong**, not a style preference: wrong code (a compile error, a logic bug, a mismatch with the
kit's rules or with `docs/scope/`), a doc that contradicts the scope or another living doc, a stale pointer to a
renamed or moved file, a broken path or link, a pitfall number that does not exist, two copies that should be equal
but differ, or `docs/PLAN.md` Part C "Now" saying something untrue. History in frozen reports, dated Log lines,
`Handoff/` and `Previews/` folders, items already scheduled in Part C Decisions, and lines that are true but leave
something out did not count. Pointers to the open PRs' files (`docs/reports/PLAYTEST_2026-10-26.md` on #43, Spool's
code on #42) are expected to land with them and were not counted either.

## How each pass worked

- **Tools first.** Every mod compiled with `cloud-check` in release and with every debug define set (Argo, Yossele,
  RogueWavesMode, RogueLab with `ROGUELAB_SELFTEST`, Riptide, the Mummer template, the recipes: 13 builds), every
  editor folder with `check_editor.sh` (7), every `.py` and `.sh` byte-compiled or parsed. A script resolved every
  backticked repository path in every text file (783 tokens, 77 left to read by hand) and compared every pitfall
  number cited anywhere with the numbers `docs/09` defines. `TDS_Mod/ModSource/Shared` was diffed against
  `TDS_Vessel_Creator/kit/Shared`.
- **Pass 1: six readers, one area each,** every file in the area read from the start and each claim checked against
  the thing it describes: A kit `README`s, chapters 01-12, template, examples, tools; B chapters 13-24, `reference/`,
  recipes; C Argo, the shared kit, RogueLab, Riptide, `TDS_Mod/Tools`, `.claude/`; D Yossele against scope `07`,
  `11`, `06`; E RogueWavesMode, `docs/mode/`, the `docs/` pages, `concept-tools/`; F `docs/scope/`, PLAN Parts B-D,
  `CLAUDE.md`, the reports index. Readers reported; the lead verified both sides of every finding and made the fix.
- **Pass 2: three readers on a different cut:** one re-read pass 1's diff and the touched scope files and READMEs; one
  read the three built parts code-first and checked their pages against the code; one checked every claim the kit
  and the `docs/` pages make about this repository's own classes, tools and flags against the live tree.

## Results

| | Found | Fixed | Listed for Dimitri |
| --- | --- | --- | --- |
| Tools (compile, paths, pitfall numbers, syntax, shared-kit diff) | 0 | 0 | 0 |
| Pass 1 | 20 | 18 | 2 |
| Pass 2 | 11 | 10 | 1 |

Every mod compiles at 0 errors, 0 warnings in every define set (RogueLab's release build stops on its deliberate
`#error`, as its README says); every editor folder compiles; every path and pitfall number named in a living page
resolves; the two shared-kit copies are equal.

### Pass 1: 20 issues

**Stale state after Yossele's PC round 9 (2026-10-23)**, the same fact in seven places: `docs/scope/07`'s Status line
and `02`'s [Now] and Next lines still said 0.2.0-dev was compiled only and 0.1.0-dev (98/98) was the installed play
build; `01`'s Status said "Yossele's next build comes next"; `ModSource/Yossele/README.md` and `DESIGN.md` said the
same; `docs/PLAN.md` Part C "Now" opened with "RogueWavesMode 0.2.1" and "Yossele 0.1.0-dev is built" (and corrected
itself four lines down); the root `README.md` named Yossele 0.1.0-dev twice; `RogueWavesMode/IMPLEMENTATION_NOTES.md`
said Yossele's looks still went through `Vfxdebuffcheck` "until its next build". All now say 0.2.0-dev, PC round 9e
160/0, Dimitri's play build; `02` gets its 2026-10-23 Log line. (Fixed.)

**Scope `16` against the sheets and the game:** the Side by Side table gave Yossele Claude defaults (`~60~ ~2.25~
~4.5~ ~3~`) where `07` carries Dimitri's own 70 / 1.75 / 5.5 / 4 tiles (and the code builds them); Boutman's and
Fie's accent colours (`#BEA167`, `#CD465F`) were not the game's `Displaycolor` (`FFC34C`, `FFA76D`: kit `docs/18` §1,
`reference/art_survey_2026-10-15.txt`, the palettes), while the other three vessels' were. (Fixed; the project-files
copy of `01` and `07` is synced, `02`'s [Now] line too; `02`'s Next line, its Log line and `16` still need the copy
refreshed from this branch, as does the PC's.)

**Export settings that differ from the code:** `Assets/ArgoMod/Editor/ArgoModPrefab.cs` exported Argo 2.0 as
version "1.4.0" with 1.4.0's "Fathoms Below" text (the code is "Deep Sea Diver" at "0.1.0-dev", `docs/PACK.md` §4);
`Assets/RogueLabMod/Editor/RogueLabModPrefab.cs` wrote "0.3" where the code is 0.5.2. (Fixed; Argo's takes effect at
its next PC build. The one pack version for every part stays scheduled for the first pack build, Decisions
2026-10-15.)

**Pointers and comments:** `ModKitPlay.cs` (both copies) named a `ModKitPlay.Next()` that does not exist
(`RunScenario` moves to the next map); the kit `README` and the recipes `README` said the recipes cover chapters
19-23 (no recipe for 22); Yossele's `02_Data.cs` and `DESIGN.md` cited the boss-health rule to `docs/13` (it is
`docs/05` §16); six notes in kit `20`, `22`, `23` and `24` said they "correct" a chapter that already carries the
fix; Argo's notes gave the 1.4.0 vessel "damage 5" (4 since 1.3.0) and pointed at an `Archive/` folder that is not
in this repository; `concept-tools/README.md` listed three of the six temporary-art generators. (Fixed.)

**Listed, not changed (judgment calls):**

- **Salt Sting without Brine.** `Argo/Code/11_Water.cs:191-192` stings a pinned foe for `SaltBrine` (0.5) doubled
  = 1.0 a second when Brine was not taken, the same as with Brine; the upgrade text, `DESIGN.md` and scope `09`
  say "0.5 a second without Brine". Round 2 measured only the with-Brine case. Either the text is right and
  `SaltBrine` should be 0.25, or the code is right and the three texts should say 1 a second. The scope line is
  Dimitri's: default taken if he says nothing is to **keep the code and fix the texts to "1 a second"** at Argo's
  balance pass (Part C "Now", "Left on Argo").
- **The mode's save keys without `RW_`** (`Campaign_Seed`, `Campaign_Route`, `Stock_Gameplan`, `Stock_World`,
  `Player_Device`, `Player_Guid` in folder `RogueWaves`) against kit `docs/23` §2 rule 3. Already scheduled for the
  mode's next change (Part C "Now", `QUALITY_PASSES_2026-10-15.md` A3-9); noted here because `Player_Guid` is also
  the name of a stock key, so the rename should keep a one-time read of the old names.

### Pass 2

Pass 1's 23 changed files were re-read against their sources: every hunk is true and keeps `00_Read_Me.txt`'s rules
(one fragment pass 1 left in `02`'s Next line is mended). Eleven more issues:

- **The same stale fact, four more places:** scope `07`'s Softening Roots note ("built, not run in game yet"),
  `Yossele/DESIGN.md`'s "How the new rules work" heading ("untested") and its Cordyfriend paragraph (still "borrows
  the Snoble's sprite"; it has its own temporary sheet since 2026-10-27), `docs/concepts/YOSSELE_SPOOL_ULOE.md`'s
  note implying the current build is melee, and the root `README.md`'s "recipes of chapters 19-23". (Fixed.)
- **PLAN "Now"** said the template gets its 1.6 calls "before Spool is scaffolded"; Spool was scaffolded on PR #42
  without them. Now "before Uloe", and Spool takes them at its next round. (Fixed.)
- **The mode's dev export** (`RogueWavesModeModPrefab.cs`) wrote "0.1.3" where the code is 0.3.0, the same class of
  mismatch as Argo's and RogueLab's in pass 1. (Fixed.)
- **Kit `README`** said every move of the play harness logs `FALLBACK` when its click didn't take; only `Build` and
  `Upgrade` fall back to the command (`ModKitPlay.cs`), the others log that the move by hand failed. (Fixed.)
- **`Yossele/Previews/0.2/ASSETS.md`** opened with "none of this has been seen in game yet" and called
  `Debuff_Yossele_Daze` unused; it is pointed at as the asset list, so a "since" note now sits on top. (Fixed.)
- **Listed:** `RogueWavesMode`'s version is "0.3.0" with no `-dev`, against `docs/PACK.md` §4's rule for dev mods
  (Argo and Yossele carry `-dev`), so the registry logs "parts disagree" whenever the three dev parts load together.
  The one pack version (Decisions 2026-10-15 B3/B5) ends it; until then the bare number stays, since changing it
  changes what the mode logs and the PC rounds compared.

Everything else the three readers checked held: every logic-bug pattern looked for in the three parts' code (lists
changed while iterated, `?.` on Unity objects, server-only reads on clients, forbidden tags, hard-coded counts against
the scope), every kit class, method, flag, probe name and log format the docs name against the live tree,
`docs/RESEARCH_GAPS.md`'s "17 of 18", and every row of `docs/EXPANSION_SCOPE.md`.

**Not synced here:** the project-files copy of the scope (`/mnt/project-files/scope/`) took `01`, `07` and `02`'s
[Now] line; `02`'s Next and Log lines and `16` still need copying from this branch (the tool refused the overwrite),
and the PC copy (`Desktop\RogueWave\scope`) at the next PC round. That copy of `02` and `08` also carries PR #40's
Spool answers, which are not on the default branch yet.

## Why a pass still finds issues

The same reason as on 2026-10-15: a fact that lives in several living pages (a version, a round result, which build is
installed) goes stale in the pages the thread that changed it did not open. Round 9 updated `IMPLEMENTATION_NOTES.md`,
`TDS_Mod/README.md` and `CLAUDE.md` and missed the scope's Status lines, the vessel's own `README.md` and `DESIGN.md`
and the opening lines of PLAN "Now". The fix that lasts is the one `docs/PLAN.md` Part A already asks for: a fact is
written once and cited everywhere else.
