# Rogue Waves: the prompt plan

The plan for building the expansion in `docs/scope/` (the user's scope, 2026-10-08, one file per category since
2026-10-09, the highest truth after the user's newest words). It has three parts: how the **coordinator** (the Claude
session that runs this project) works so it stays cheap and good, the **phases** with the prompt that opens each
one, and the **state** every session reads first and updates last. Rules about the game live in the kit; this page
only says what to do next and how to do it cheaply.

---

## Part A. Coordinator operating prompt

Paste this once as the project's instructions, or keep it here and let `CLAUDE.md` point at it.

> You coordinate the Rogue Waves expansion mod for *TDS - Tower Defense Strategy*. Your job is to move
> `docs/PLAN.md` forward one gate at a time, spending as few tokens as a careful lead would.
>
> **Start of every session.** Read `CLAUDE.md` (automatic), then only `docs/PLAN.md` § Part C "Now". Open the one kit
> chapter the current task names in Part B. Do not read the rest of the plan, the scope file, or other chapters unless
> the task sends you there.
>
> **Reading budget.** At most three kit chapters per session unless you are writing code (then `docs/09` § A and the
> `docs/05` section you need). Never read `reference/api_assembly_csharp.txt`, `vessel_data_survey.txt` or a whole
> `Player.log`: grep them with 3 lines of context. Read `docs/09_PITFALLS.md` by section, matched to the symptom. A
> fact already written in the kit or in Part C is never re-derived; cite it.
>
> **Delegate reading, keep judgement.** Any question that means sweeping files or decompiled code goes to a subagent
> with the template in Part D. Ask for an answer under 250 words, each claim tagged verified / read / untested with a
> `file:line`. You decide; the subagent only reports. Run independent questions in one batch.
>
> **Write to files, not to chat.** Findings go to the kit chapter they belong to (new engine fact → that chapter or
> `docs/09`; a mode or system fact → `docs/14+`), decisions go to Part C "Decisions" as one line each. Chat replies
> say what changed, what is blocked, what you need (Dimitri dropped the 150-word cap, 2026-11-04). Never paste
> file contents or restate a file.
>
> **One thing at a time.** One vessel, one system, one gate. Design before code (`DESIGN.md`, checked against
> `docs/02` by its design script), `cloud-check` before any handoff, a handoff only when it is worth the user's PC
> time, and batched: several fixes per round, never one. Compile is not a test; say which it was.
>
> **The user's scope.** `docs/scope/` is the scope as one `.txt` per category (`00_Read_Me.txt` lists them), and
> `/mnt/project-files/scope/` is his copy; keep the two equal, writing to his copy only at his word ("sync", `CLAUDE.md`). He sends back only the files he edited. An upload
> arrives without its name: its first line (`| Directions |`, `| New Vessel: Yossele |`) says which file it is. Fold in
> just that file: diff it against the repo copy, keep his wording, put each `< answer >` into the line above it in his
> format, and update both copies. Touch another scope file only where his edit contradicts it, and name that file in
> the reply.
>
> **Ask only at gates.** Questions for the user are collected in Part C "Open questions" and asked together when a
> gate is reached or the work cannot continue without them. Defaults you can take yourself are taken and logged.
>
> **Merging (Dimitri, 2026-11-05: "Automatically merge pull requests").** Claude merges PRs into
> `claude/phase-1-6lfnar` itself, with a merge commit, through the GitHub MCP tool `merge_pull_request` (pass
> `expectedHeadSha`). Before merging: merge the base into the PR's branch (Part C entries base first, the PR's own
> last), run the cloud-check of every changed part (docs-only: check the cited paths), and leave a PR whose own thread
> is still working on it. After merging, message the threads of the PRs still open to re-merge the base. Pushing
> straight to the base stays denied, and his OK is still needed for anything that touches his PC.
>
> **End of every session.** Update Part C "Now" (done / next / blocked, three lines; fold finished items into
> "State" and put their detail in "Log" as one dated entry, so "Now" stays under about 4 KB), add decisions and open
> questions, commit with a message under five lines, push to the working branch.

### Model per role

Which model each role uses, and the escalation rule: `docs/PROJECT_SETUP.md` "Model per role".

Effort per thread (Dimitri, 2026-11-04): `xhigh` for code builds, PC rounds, debugging and engine work; `high` for
design, reviews and text PRs; `medium` for lookups, syncs and small fixes; `max` only at his word. A quality check
states its pass cap and what counts as an issue in its opening prompt. Which skill to use when is in `CLAUDE.md`.

---

## Part B. Phases and their opening prompts

Each phase ends at a gate. A gate is a short written result the user can approve in a minute. Phases 1 and 2 are
cheap and decide the shape of everything after them, so nothing else starts before they close.

**Release order (user, 2026-10-20).** The first release is **Yossele alone** ("Release 1" below, after Phase 3). Everything
else comes afterwards: Spool and Uloe (the rest of Phase 3), enemies and modifiers (Phase 4), the Rogue Waves mode with
its sea chart (Phase 5), content (Phase 6), and Argo, built and waiting, inside the pack. Phases keep their numbers; Release
1 takes Yossele through Phase 3 and a one-part Phase 7, and nothing in Phases 4-6 starts before Release 1 ships unless
Release 1 is blocked on the PC or on Dimitri.

### Phase 0. Definitions (half a session; done 2026-10-08)

Close the scope's blanks with the user before anything is researched. Reads: `docs/scope/` once, this section.

Opening prompt:
> Read `docs/scope/`. List every term it uses that the kit does not define and every item with no body
> (done 2026-10-08; see Part C Decisions). Write them to
> `docs/PLAN.md` Part C "Open questions" with the default you would take for each. Ask the user once, in one message.

Gate: every open question has an answer or an accepted default.

### Phase 1. Feasibility of the Rogue Waves Mode (2-3 sessions, research only; done 2026-10-08)

The mode is the riskiest piece and decides whether the expansion is one mod, several, or a mode-less vessel pack. The
kit has already read the mode chain (`docs/13` § 3) and the API dump names the pieces the scope needs: `Mode_Base`,
`Modeobject`, `Gameplandatabaseobject`, `Artifactobject` and `Game_Artifacts`, `Map_Cardinalconditions`
(Cardinals are a base-game system), `Entity_Ai_Tower_Merchut.Activatebounty` (a base-game shopkeeper with bounties),
`Bl_Save`, `Unlock_*`, `Entity_Ai_Main_Core.Addplayer.Respawn` (the F-key bug). Reads: `docs/13` § 3 and § 11,
`docs/11`, `docs/01` § 1 and § 11. **Prerequisite:** the game's `Assembly-CSharp.dll` among this Claude Project's
files (`docs/13` § 11); without it the session can only grep the API dump, and the questions below stay "read".

Opening prompt:
> Answer, with the decompiler setup in `docs/13` § 11 and subagents per Part D, these questions and write
> `TDS_Vessel_Creator/docs/15_ROGUE_MODE_ENGINE.md` in the kit's style (what is read vs untested):
> 1. Can a mod add a mode entry and a server mode script (`Mode_Base` subclass or a `Mode_Normal` wrapper), and have the
>    host carry state (Pure Aura per player, chosen Directions, ascension) across three consecutive maps? What is the
>    cheapest fallback if not (three matches with state kept by the host mod, or in the save)?
> 2. How does the base game's shopkeeper (`Merchut`, bounties) work, and how are Cardinals (the run-modifier effects:
>    `Map_Cardinalconditions`, `Artifactentryobject`, `Game_Artifacts`) chosen and applied? Can a mod add shop items,
>    bounties and Cardinals the same way the base game does? Scoring is out of scope: the mode does not count score.
> 3. How are choices shown on every client (Direction pick, map vote, "leave or stay" between tides): is there a
>    menu or command UI a mod can reuse (`Entity_Command_Itemselect_Base`, `Bl_Menus`), with the host as the authority?
> 4. What may a mod write to the save without breaking it (`Bl_Save`, `Unlock_*`), for ascension and unlocks?
> 5. Enemy modifiers: close `docs/13` § 10 questions 1, 2 and 4 from the code; list what needs the PC.
> 6. The F-key respawn bug: where selection goes after a vessel respawn, and whether a mod can fix it for Boutman and
>    for its own minion vessels.
> 7. Add `docs/14` § 6 (ModTool scenes, `Bla_Mods` order, the RoslynCSharp settings, several `Default` prefabs) to
>    the PC list; they decide maps and the pack's layout.
> End with a go / fallback / no table per scope feature and a PC research list (one build, one launch).

Gate: the user picks go / fallback per feature. This fixes the mode's design and the packaging.

### Phase 2. Pack foundation (1-2 sessions; done 2026-10-08)

Reads: `docs/14` § 5 (the two pack layouts), `docs/15`, `docs/04` § 7, `kit/README.md`, `docs/03` § 1 and § 4,
`docs/EXPANSION_SCOPE.md` rows 15 and 18.

Opening prompt:
> Decide and write down (a) packaging: one `RogueWavesMod` or one mod per vessel plus one for the mode, load order,
> what the shared kit 1.6 must add for Rogue hooks (Growth, Light, Thread, Pure Aura, Direction application), and the
> version scheme; (b) a one-page style sheet `docs/STYLE.md`: the pack's palette, motifs, naming voice, which base
> vessel each new vessel's sprites follow, and the tower silhouette rule; (c) the roster's shape language, adding
> Yossele, Spool, Uloe to the taken list in `.claude/rules/design.md`. Bump the kit only if (a) needs it.

Gate: packaging and style approved; kit 1.6 scope listed.

### Phase 3. Vessels, one at a time (3-5 sessions each, plus PC rounds; Argo done 2026-10-08, Yossele built 2026-10-12 and merged, PR #30; Yossele is finished first, for Release 1)

Order and reason: **Argo** first (designed from the scope, with shipped code and tests to reuse, so it is the cheapest
way to shake out kit 1.6), then **Yossele** (Growth is a new generic mechanic other pieces may reuse; finished to
Dimitri's 2026-10-20 sheet and released alone, Release 1 below), then **Spool** (minion-heavy:
`docs/12`, and the template's Thread code is the nearest prior art), then **Uloe** (light levels are new engine territory:
"light received" is feasible: the mod sums light sources itself, `docs/RESEARCH_GAPS.md` B1).

Each vessel runs the kit's loop; the reading list is `CLAUDE.md`'s reading-order table, not more:

1. `DESIGN.md` from `template/DESIGN_TEMPLATE.md`, holding the scope's text as the concept and listing every change
   under "Changes from the concept, and why" (`docs/02`, `docs/12` for units; `examples/Mummer_DESIGN.md` for limits).
2. Concept art at the game's formats (`docs/03`), style sheet from Phase 2.
3. Scaffold (`new_vessel.py`), code module by module (`docs/05`), `cloud-check` clean in release and test defines.
4. Handoff and PC round (`TDS_Mod/Tools/pc/README.md`); self-test 0 failures; captures reviewed.
5. `IMPLEMENTATION_NOTES.md`, lessons to `docs/09`; no zip of its own: the vessel ships as its prefab in the pack
   (`docs/PACK.md`).

Opening prompt (per vessel):
> Build `<Vessel>` from its file in `docs/scope/` (its Directions are in `11_Directions.txt`) through the five steps
> in `docs/PLAN.md` Phase 3, one step per session unless a step is small. Start with step 1 and stop at the design gate.

Gate per vessel: the kit's definition of done (`TDS_Vessel_Creator/README.md`).

### Release 1. Yossele alone (user, 2026-10-20; several sessions plus PC rounds)

What ships: the `RogueWavesMod` bundle (`docs/PACK.md`) holding **only Yossele's part**, pack version 0.1.0, as a zip
with its README. Yossele works in a normal game: a vessel part works without the mode (`docs/PACK.md` §2 rule 1). It
carries the F-key fix (`ModKitFocus`, run by every vessel part). It does not carry the mode, Argo, Spool, Uloe or the
foes. Kit 1.7 is not needed: its "Leave" fix is the mode's, and its focus-mark fix only matters once two parts install
the handler (`docs/reports/QUALITY_CHECK_2026-10-15.md` B1, B2).

Steps, in order:

1. Dimitri's answers to the questions in scope `07` and `01`: done 2026-10-21 (Decisions).
2. `DESIGN.md` updated to the 2026-10-20 sheet; the gap is listed in Yossele's `IMPLEMENTATION_NOTES.md` ("Gap to the
   2026-10-20 sheet"). Research first what the engine may refuse: D-MDS's projectile interception, Monument's "no
   debuffs", Cordyfriend raised from a dying foe. Design gate. Done 2026-10-23 at Dimitri's ask to update the mod to
   the sheet: all three read as possible in the decompiled game; every default is in Yossele's `DESIGN.md`.
3. Code it (Phase 3 step 3), with the fixes carried over in Part C "Next", and Dimitri's own sprites when he sends
   them (`docs/03`, `docs/18`). PC round: self-test 0 failures, then his play test and his balance pass. Coded
   2026-10-23 as 0.2.0-dev with the carried-over fixes; PC round 9 passed the self-test (160/0) and made it his play
   build the same day. His play test next, with what it must show in `docs/reports/YOSSELE_TEST_PLAN_2026-10-23.md`.
4. The pack build with one part: Tools > ModKit > Build Pack with `Tools/pc/Build-Pack.ps1` (`docs/PACK.md` §1; built 2026-10-28, first run in round 10: the 0.1.0 zip exists), the one pack version
   in every place `docs/PACK.md` §4 names, the README of §5, the Mods-folder install checked on the PC with no dev mods
   in it.
5. `docs/10` hand-off; `RogueWaves-0.1.0.zip` to Dimitri. Gate: Dimitri says ship.

### Phase 4. Enemies and modifiers (1-2 sessions plus one PC round; built 2026-11-03, the foes part)

Reads: `docs/13`, `docs/15` §1, `docs/18` (foe art). The modifiers are the scope's four (`docs/scope/13`): Cursed (an
aura that lowers nearby towers' and units' attack), Berserk (damage and speed up as health drops), Aging and Dazzling;
they show as an effect on the foe, not a new skin. New enemy types: crystal foes per `docs/18` §5.6, when the scope
names them.

Opening prompt:
> Implement the scope's four modifiers (`docs/scope/13`) per `docs/13` § 6, each shown as an effect on the foe (no skins;
> colours per `docs/18` §5.5 and §7), with journal pages. Then the new enemy types the mode needs, one each.

Gate: the four modifiers seen in a self-test, 0 exceptions with the whole pack loaded.

### Phase 5. The Rogue Waves Mode (4-8 sessions, several PC rounds; built and run in game 2026-11-03, "Phase 5 pieces" in Part C)

Build in the order a run plays, each piece testable alone: the main-menu button → Pure Aura (per-player counter, earned
per tide) → objectives/bounties (Merchut's pattern in a mod brain; its contracts are hardcoded, `docs/15` §5) → VENDCINT
between tides → Directions (data, pick UI, application through upgrades) → Fund Transfer → the in-map vote on the sea
chart (chart, weather and rejoin are built) → leave-or-stay at Tide 15 and bonus tides → the 10-map campaign loop with
rising difficulty, saved between maps → Depth (on hold). Part C "Phase 5 pieces" is the live list. Reads: `docs/15`,
`docs/07` for every synced piece.

Opening prompt:
> Implement the next unchecked piece of the Phase 5 list in `docs/PLAN.md` Part C, host-authoritative per `docs/07`,
> with a self-test section and a journal page. Stop when it compiles and the handoff is packed.

Gate: a full 10-map campaign completed on the PC (saved and continued at least once), online once with a friend.

### Phase 6. Content passes (2-3 sessions)

New Cardinals and Trinkets (run modifiers, built like the base game's Cardinals), the Direction catalogue (per vessel: stat bonuses and one "fundamental change" each), and the
Boutman F-key fix's by-hand check (the fix is built: `ModKitFocus`, kit 1.6; a real Boutman death passed on 2026-11-04 with the fix unarmed, so kit 1.11's fix and a person's own press are left). Each is data plus small code on finished systems.

### Phase 7. Hardening and release (2 sessions plus PC rounds; Release 1 runs its pack steps for Yossele alone)

Roster balance (`docs/02` § 7 across all vessels and ascension levels), online checklist for every new sync, the pack
README, versioned zips, and `docs/10` hand-off. Gate: release.

---

## Part C. State (read first, update last)

### Now

- **State (2026-11-05, base `7de9305`):** Phases 1-3 done; Phase 4's foes and Phase 5's mode built and run in game.
  Parts: Argo 2.0.1, Yossele 0.2.0-dev, Spool 0.1.0-dev, Uloe 0.1.0-dev (temporary art, `docs/TEMP_ART.md`; Argo keeps his
  shipped look), `RogueWavesMode` 0.1.0-dev (main-menu button to end screen), the foes part; kit 1.14. Last run on the PC:
  testing round 3 (kit 1.9, `docs/reports/TESTING_ROUND_3_2026-11-05.md`) and `Light_round1` (`fb208b8`, kit 1.13,
  `docs/reports/LIGHTING_2026-11-05.md` §6). Built since, compiled here, not run on the PC: the unbuilt candidates
  (`BUILD_UNBUILT_2026-11-05.md`), the max review's fixes, the decided code fixes, the balance sweep, Spring Tide's test,
  the meteor's crystal and Yossele's final sheets. Every scope default is his except the sweep's tuned numbers in `04`,
  `08`, `10`, `12` (tildes until after PC round 4); his project-files scope copy and the Scope Editor equal `7de9305`.
  Dated history: "Log" (newest last); reports: `docs/reports/README.md`.
- **Next, in order:** PC round 4 (his OK 16:04Z; lists: `BUILD_UNBUILT_2026-11-05.md` §5, `CODE_REVIEW_MAX_2026-11-05.md`
  §6, `BALANCE_SWEEP_2026-11-05.md` §8, `DECIDED_CODE_FIXES_2026-11-05.md` §4; Spring Tide's three lines pass and its two
  `INFO` lines show a Multi near 0.01, `RogueWavesMode/IMPLEMENTATION_NOTES.md` first entry; the meteor on a map and
  Yossele walking left; it also syncs the PC's scope copy), then scope `07`'s sprite NEEDS line cleared in a docs PR;
  the per-weather lighting look's PC round with foes on screen (`WEATHER_LOOK_2026-11-05.md` §4; built, entry below);
  then Spool's 22 upgrades and UNTHREAD (scope `08`, B19).
- **Still open for his eye or word:** the code review's bugs left for his word, a PC fact or a sighting
  (`CODE_REVIEW_RECHECK_2026-11-05.md`, `CODE_REVIEW_MAX_2026-11-05.md` §5.1); by hand: the F key on a real Boutman death
  (P1-1), the icons, Spool's sage green beside a Hardened foe and on grass, the picked temporary art and minions, the
  button's and chart's look, a flooded map, the chart, VENDCINT, Uloe and the campaign (`BUILD_2026-11-03.md` §6), the
  polish's readings (`MODE_POLISH_2026-11-03.md` §2); his `.claude/settings.json` edit (item 14,
  `OPEN_DECISIONS_2026-11-05b.md`).
- **Online evening:** only what loopback can't be: Steam's Join Game and lobby, a real network's delay and drop, a
  person's eye (`docs/reports/TEST_ROUND_2026-11-04.md` §5, `docs/RESEARCH_GAPS.md` §E, kit `docs/19` §8). Wherever
  "one online evening" appears, read that.
- **Release 1, Yossele alone** (Part B; `docs/reports/PRIORITIES_2026-10-28.md`): the pack builds and boots; left: his
  play test of 0.2.0-dev (`YOSSELE_TEST_PLAN_2026-10-23.md` §3-5), his look by eye and sprites (final sheets drawn,
  `ModSource/Yossele/Art/README.md`), his balance pass, the Compost Bin numbers in scope `11`, one online evening, then
  the one-part pack build (version `0.1.0`) and his "ship". Left on Argo: his balance pass (FLOOD's key, `GiveUpAfter`,
  Salt Sting's no-Brine number, `MISTAKE_CHECK_2026-10-28.md` B2-X1/X6) and whether it plays easier than its base
  (sweep). Spool: his play test and look by eye (`Spool/IMPLEMENTATION_NOTES.md` "PC rounds"), the sheet's open items,
  his sprites. Engine study facts N6, N8-N11 stay read only until a round needs them (never N12).
- **Blocked:** nothing; what's left needs Dimitri at the keyboard or a friend online.
- **Next free numbers:** kit chapter 25, kit 1.15, pitfall #175 (which PR used which pitfall numbers: the last Log entry).
- **2026-11-05: the always-read files trimmed** (Dimitri: token efficiency): "Now" from 32 KB to about 4 KB, its dated
  entries moved verbatim to the Log's last entry; Part A's model table to `docs/PROJECT_SETUP.md`; no rule dropped.
- **2026-11-05: a look per weather built** (`docs/reports/WEATHER_LOOK_2026-11-05.md`): mist, storm grey, dusk from the
  map's weather on every machine, a 4 s fade after the intro, calm maps unchanged; self-test W4. Compiled here (the mode,
  every test build), not run on the PC; its PC round is §4. No kit change; no pitfall used.
- **2026-11-05: API credit set up** (Dimitri: "Setup both"): `docs/PROJECT_SETUP.md` "API credit" (the $100 can't
  overflow the plan; usage credits can; key steps for Windows) and `TDS_Mod/Tools/batch/` (text and scope checks on
  the Batches API, about $3.50 a run on Sonnet 5.5, $0.70 on Haiku 5.5). No real batch run yet: no key here.
- **2026-11-05: four project skills drafted** (Dimitri: "Draft the 4 skills"): `docs/skills/` holds ship-a-pr,
  pc-round, scope-audit and scope-sync, each a whole SKILL.md for his claude.ai settings, plus how to install them.
  Docs only, no compile; nothing ran on the PC. No kit change, no pitfall.
- **2026-11-05: base game Cardinals split** (Dimitri: "separate them"): 13 of the 29 sold are Trinkets, held once per buyer
  (`docs/reports/STOCK_TRINKETS_2026-11-05.md`; §5 three questions, §6 the PC list); scope `12` lists them as defaults.
  Compiled here, not run on the PC. No kit change, no pitfall.
- **2026-11-05: feature spotlight page** (Dimitri: "Design a spotlight of all of the new features"): `docs/spotlight/`
  (artifact https://claude.ai/artifact/MCPDgVngHmnZcVHeZwxNSr), every feature chipped played, built-not-run or not built,
  with the PR #130 Trinket split. Docs only, no compile; nothing ran on the PC. No kit change, no pitfall.
- **2026-11-05: his answers on the split** (Dimitri: "Keep that", "Make Trinket", "Pay only share"): 17 of the
  29 sold are Trinkets, held once per buyer, companions one at a time, Coins a share (`docs/reports/STOCK_TRINKETS_2026-11-05.md`
  §6-§7 the PC list); scope `12` lists them. Compiled here, not run on the PC. No kit change, no pitfall.
- **2026-11-06: third scope-to-code audit** (Dimitri: `/scope-audit`, "All 17 files, changes only"): 512 rows, 355 MATCH,
  117 resolved, 24 queued (Spool B18/B19), 11 by design; N1 Tidal Mend x level and N2 Abyss's capped slow wait for his
  word; scope 02, 04, 05, 15 texts fixed (`SCOPE_CODE_AUDIT_2026-11-06.md`). Docs only, read from the code, nothing run.
- **2026-11-06: Tidal Mend and Abyss follow the scope** (Dimitri: "Code follows the scope"): Mend heals a flat 5/8/12,
  Deep Water slows up to its own 0.65 (Abyss's +10% whole; the Tremor keeps 0.6); self-tests read both. Compiled here,
  not run on the PC (`STOCK_TRINKETS_2026-11-05.md` §8). No kit change, no pitfall.
- **2026-11-06: batch checks extended** (Dimitri: "set up the cheap batch jobs further"): `design`, `docs` and `art`
  checks, `--since last`, `--max-cost`, `spend`, a morning-routine line (`TDS_Mod/Tools/batch/README.md`); the key is
  the cloud environment variable `RW_BATCH_API_KEY`. Tested on made-up answers here; no real batch yet: no key.
- **2026-11-06: first batch checks run** (Dimitri: key saved): Haiku 5.5, $1.82 of the $100; leads only, none fixed
  (`docs/reports/BATCH_CHECKS_2026-11-06.md` §0, rerun of seven cut answers in `06b`); the batch README's estimate
  undercounts (4.13M tokens counted, not 2.78M). Docs only, nothing run on the PC. No kit change, no pitfall.
- **2026-11-06: sea chart navigation proposed** (Dimitri: "closer to ... Slay the Spire 2"; "Add rest and treasure
  stops"): a whole-chart view at votes and on a key, a boat banner, a Cove and a Wreck per stretch
  (`docs/mode/CHART_NAVIGATION.md`). Built on his go (entry below).
- **2026-11-06: PC round 4 merged** (Dimitri: "Go"): `PC_ROUND4_2026-11-05.md`, no game fault in a self-test; open: P1-4
  (rejoined friend's "BASIN BREACHED"), U3's "MAP 1 OF 10 WON", 548 Vfx/Mobbase lines, §8 test faults. Scope `07` sprite
  NEEDS cleared. Test lines compiled here and on the PC. Pitfall #172.
- **2026-11-06: Art Bench page** (Dimitri: an artifact to edit every sprite frame and model, "Give the whole row"):
  every sheet by row as `.aseprite`, every model as `.glb` for Blender, edits sent back on the page and brought in by
  `TDS_Mod/Tools/artbench/ingest.py`. Tool only, tested here; models need builder support next. No kit change.
- **2026-11-06: round 4 and batch bugs fixed** (Dimitri: "Fix bugs"): a friend's end title, the Vfx/Mobbase lines on a join
  (#173), the meteor's first map, one Yarn Stash, Plug In's Light 8 cap, every round 4 test fault
  (`BUG_FIXES_2026-11-06.md`, PC lines §5). Compiled here, not run on the PC. No kit change.
- **2026-11-06: input artifacts send edits back** (Dimitri: "Update older Artifacts that take input"): the Scope and
  Vessel Editors gain a Send step ("bring in my scope / vessel edits"); six old pages archived to project files and
  deleted. Index and steps: `docs/artifacts/README.md` (Log).
- **2026-11-06: sea chart redrawn in the game's look** (Dimitri: "more inline with the current TDS art style and
  theme"): no panel, the stock title and rule, framed square tiles, pixel icons, corner brackets (`CHART_NAVIGATION.md` §3.6,
  `chart_look_*.png`). Built on his go (entry below).

- **2026-11-06: batch checks on the PC without Claude** (Dimitri: run it when the 5-hour limit is up):
  `TDS_Mod/Tools/batch/Run-Batch.ps1` (`-SaveKey` once, then one command; report moved to `Desktop\RogueWave\batch`).
  Written here, not run on the PC (no PowerShell here). No game code; no kit change, no pitfall.
- **2026-11-06: Rogue Tide theme for pages and decks** (Dimitri: "Choose rogue tide"): `docs/themes/rogue-tide.md`, the
  pack palette with the HUD's bracket frames and tiles, linked from `STYLE.md`. Docs only; nothing ran on the PC.
- **2026-11-06: Scope Editor refreshed in Rogue Tide** (Dimitri: "Update the rogue waves scope editor with updated
  information"): scope `02` logs 11-05/06 work, `04` gains the chart view, Cove and Wreck as ~defaults~; the page wears
  Rogue Tide and is republished. Docs and page only; nothing ran on the PC.
- **2026-11-06: spotlight restyled in Rogue Tide** (Dimitri: "as if it were a finished product ready to ship"): no
  status chips or "not built" list; chart view with Cove and Wreck added (`docs/spotlight/`, artifact v3). Docs only,
  nothing ran on the PC. No kit change, no pitfall.
- **2026-11-06: sea chart navigation built** (Dimitri: "Go"): the chart view beside the vote and on M, banner and mini
  chart, Coves and Wrecks, CV1-CV12 (`CHART_BUILD_2026-11-06.md`, PC list §4). Compiled here and save fuzz passed, not
  run on the PC. No kit change, no pitfall.
- **2026-11-06: multiplayer MVP board proposed** (Dimitri: "Draft an MVP board for multiplayer that shows accolades and
  failures of the players"): one accolade and one failure per player after each won map and at the campaign's end, mostly
  from the game's own match statistics (`docs/mode/MVP_BOARD.md`). Docs only; not built until his go.
- **2026-11-06: the menu buttons' wobble fixed** (Dimitri: "The button itself wobbles and jitters"): the game's half-pixel
  layout under the menu camera's sway; `RWPixels` puts still pictures on whole pixels (`BUTTON_WOBBLE_2026-11-06.md`, #174).
  PC self-test 168/0: the game's places flicker 144 of 144 frames, ours 0; the fixed build is in his Mods. No kit change.
- **2026-11-06: chart round 1 on the PC** (Dimitri: "Go chart round"): flow 178/1 (CV10's blacklist read too early, test
  fixed), two players 167/0; chart pictures on his PC; after-win vote's view and a mini chart overlap for his eye
  (`CHART_ROUND1_2026-11-06.md`). His builds and save back. No kit change, no pitfall.
- **2026-11-06: what the pitfalls still point to** (Dimitri: "would there be any bug fixes or improvements that might
  fixable?"): 14 small fixes need no decision (5 best proven on the PC after), three need his word
  (`docs/reports/PITFALL_FIXES_2026-11-06.md`). Read from code only; nothing changed or run on the PC.
- **2026-11-06: MVP board built** (Dimitri: "just the most notable thing of the player ... Defaults for rest, and then
  build"): one title per player, failures as common as accolades, Tab replays it; `27_Board.cs`, `28_BoardView.cs`,
  MB1-MB11 (`MVP_BOARD.md` §4, §6). Compiled here, not run on the PC. No kit change, no pitfall.
- **2026-11-06: the whole-pixel fix's review follow-up** (Dimitri: "Follow up pr"): buttons left alone while a hover grows
  them, the game's stacked lists always; a still P2 passes only if its control saw the flicker (`BUTTON_WOBBLE_2026-11-06.md`
  §3-4). Compiled here, not run on the PC. No kit change, no pitfall.

### Log (newest last; was "Now" until 2026-10-11)

- **2026-10-08:** Phases 1 and 2 done. **Phase 3 Argo**: steps 1-5; PC round 6 **139 pass, 0 fail**,
  Deep Water bobs; the playable Argo 2.0 (v0.1.0-dev) was installed on the PC; lessons in kit `docs/09` #72-76. Work is
  merged into #3's branch (PR #7, PR #10). No release: Argo ships in the pack.
- **2026-10-08:** `docs/SCOPE_v0.txt` (the user's roguewave.txt) rewritten in its own format with the current state;
  every item only the user can supply is marked `! NEEDS ... !` there (2026-10-08, branch
  `claude/roguewave-txt-update-aiqiql`). Copy at `/mnt/project-files/roguewave.txt`. The game's `TDS\Mods` folder
  was emptied at the user's ask (the pack will carry everything); the 7 mods sit in the PC's
  `ClaudeProjects\handoff\mods_removed_20261008`, so the playable Argo is no longer installed.
- **Also (2026-10-09):** the research list `docs/RESEARCH_GAPS.md` (PR #8, merged): answered from the decompiled
  game (kit `docs/15` §14) and RogueLab rounds 2 and 3 on the PC (round 3: 16 pass, 0 fail; kit `docs/15` §15, `09`
  #77-78): 16 of 18 items answered, all 45 base maps run in a mod's mode, the choice dialog takes eight cards.
- **Also (2026-10-09):** an accuracy review of the kit and the project docs against the decompiled game and the
  round logs (branch `claude/kit-accuracy-review-k0ej7d`, its PR lists every fix). Worth knowing before Phase 4 and 5:
  elite `*_Multi` params add to 1 (Hardened `Health_Multi` 3 = health ×4, `docs/13` §10); a respawn keeps the vessel's
  upgrades, so Directions on the vessel need a re-grant only when a map starts (`docs/15` §14.1).
- **Also (2026-10-09):** a main-menu start button is feasible (`docs/mode/MAIN_MENU_START.md`, kit `docs/15` §14.8); Dimitri
  adopted it; its probes M1-M7 join the next RogueLab round.
- **Also (2026-10-09 evening):** Dimitri's roguewaveWIP.txt answers folded into `docs/SCOPE_v0.txt` (PR #15: VENDCINT,
  Fund Transfer limits, vote rules, vessel abilities, Crafted types, Aging and Dazzling, an optional-objective
  template). Every new mechanic checked (`docs/reports/MECHANICS_CHECK.md`, kit `docs/15` §16): all go; eight need the game.
  RogueLab 0.4.0 (round 4, compiled here) runs them with M1-M5 and M7 in one solo launch; handoff `RogueLab_round4`.
  Round 4 ran: 11 pass, 5 fail, 1 setup; VENDCINT's ownership and fall-in, Fund Transfer's moves, a minion's owner
  and kill tracking are proven; the failures were probe bugs. Round 5 (0.4.1): 20 pass, 2 fail; every
  mechanic is now proven in game (`docs/reports/MECHANICS_CHECK.md` §3). M1 and M4 of the main-menu button failed on probe
  bugs. Round 6 (0.4.2): **22 pass, 0 fail**; the main-menu button's probes passed too. Left by hand: how the button
  looks beside Play (the automated run never shows the Main menu). RogueLab is out of the Mods folder.
- **Also (2026-10-09 20:48Z):** Dimitri's roguewave 2.txt folded into `docs/SCOPE_v0.txt` (branch
  `claude/scope-10-maps-lecoiw`): a campaign is **10 maps** (was 15); ascension levels are named **Depth** (was
  Swell), each adding a change on top of the ones before; new Directions Made of Tougher Stuff (Any vessel with
  minions), Dandelion Season and Mother of Mushrooms (Yossele), Sewing Machine (Spool); objective Mind the Meteor
  (3 Pure Aura); Thread Head sends Stitched, its paths turn them into Darts. Compost Bin's counts are now x, for
  Yossele's design gate. Map-count knock-ons in `docs/reports/SCOPE_VS_LOOP.md` (#3, #14, #20, §E).
- **Also (2026-10-09 21:50Z):** the scope is split into `docs/scope/`, one file per category (15 files: read-me,
  expansion, progress, bug fix, mode, rundown, mechanics, one per vessel, Directions, Cardinals, modifiers,
  objectives); copy at `/mnt/project-files/scope/` (replaces `roguewave.txt`) and on the PC in
  `Desktop\RogueWave\scope`. `docs/SCOPE_v0.txt` is a pointer. Dimitri now sends back only the files he edits; the
  fold-in rule is in Part A.
- **Also (2026-10-09 22:00Z):** Dimitri asked whether the boat could sail a generated Slay the Spire style map to
  choose maps. Yes, read from code (`docs/mode/SEA_CHART.md`, kit `docs/15` §17): the base campaign map is a world of nodes
  with `Links`, which a mod can build from a seed and register as its own world; the stock boat, arrows, Setup panel
  and map loading work on it. Prototype generator `concept-tools/sea_chart.py`. **Dimitri adopted it with V1 (22:23Z)**:
  the vote is held in the map and the boat sails the chart to the winner; `docs/scope/05` updated. Probes C1-C7 join
  the next RogueLab round (C8 dropped with V2).
- **Also (2026-10-09 23:30Z):** a deeper read of the game (Dimitri's ask): new kit chapter `docs/16` (grid, paths,
  the base game's map objects, aura nodes, the wallet, game speed, lighting) and `docs/15` §18 (the stock Cardinals'
  patterns, messages to every client, the final map, chart decorations, unlocks); ideas for him to pick from in
  `docs/mode/MECHANIC_IDEAS.md` (sea weather on chart nodes, objectives, Directions, Cardinals), scope untouched. Read
  only; its PC checks (`docs/16` §9) can ride with the sea chart's C1-C7.
- **Also (2026-10-10 23:00Z):** Dimitri asked to **begin the sea chart and sea weather**. Built as the first slice of
  the pack's mode part, `TDS_Mod/ModSource/RogueWavesMode` 0.1.0 (dev mod `RogueWavesModeMod`, prefab
  `RogueWaves_Mode_Default`): `Mode_RogueWave` with `RW_` twins of the 45 base Gameplans; the seeded chart (C# port of
  `concept-tools/sea_chart.py`, identical on 400 seeds; maps by row difficulty, weather per node); the chart as a
  world built in code on every machine; the host's warp, auto-sail and pick; the campaign's seed and route in the save;
  three weathers (Rising Water, Quickening Current, Nesting Ground) on stock map objects. Sea Weather is in
  `docs/scope/05` with its numbers as a NEEDS line. Compiled here, **not run in game**: its PC round (C1-C6, W0-W3,
  `docs/mode/SEA_CHART.md` §9) is handoff `RogueWavesMode_round1`, waiting on Dimitri's OK sentence. Until the in-map vote
  exists, the leader steers with the boat's arrows (only the linked maps can be picked).
- **Also (2026-10-11 04:15Z):** PC round 1 ran on Dimitri's "Go": **34 pass, 0 fail**. The chart, the boat's sailing,
  the pick, the map loading in the mode, the return one row on and the save all work in game. One bug: Rising Water's
  deter blocks were never loaded, so the water rose but flooded nothing. Fixed in 0.1.1 (every needed being is
  loaded; W1 checks flooded tiles); handoff `RogueWavesMode_round2` waits on his OK. PC scope copy synced.
  Round 2 (05:17Z): 35 pass, 3 fail, all Rising Water: `Block_Flood_Deter` is in no group the game loads for
  Manafold or any mode. 0.1.2 stands in a copy of a stock no-build block; handoff `RogueWavesMode_round3`.
  Round 3 (05:42Z): 35/3 again, no no-build block is loaded on the boat. 0.1.3 makes the stand-in in the map and
  spawns no flood without it (the flood's throw would break the Tide loop); handoff `RogueWavesMode_round4`.
  **Round 4 (06:01Z): 38 pass, 0 fail**: the sea chart and all three sea weathers work in game (solo). PR #22.
  Not seen yet: the chart's and the flooded tiles' look by eye, and any of it online.
- **Also (2026-10-11 06:12Z):** Dimitri asked for a way back into a running map after a dropped connection. Built as
  `RogueWavesMode` 0.2.0 (`docs/mode/REJOIN.md`, kit `docs/15` §14.5): the game already keeps a dropped player's things and
  gives them back on return; the mod reopens the lobby while a seat is empty, gives the seat back (aura, vessel,
  stats) and starts the rejoiner's camera and HUD. Every map, any mode. 0.2.1 passed solo PC round 6, 47/0 (round 5's
  one failure: the game's loading screen shut the lobby; now reopened every frame). PR #24 merged; the
  rejoin itself needs a friend online (`docs/mode/REJOIN.md` §6, O1-O8, part of the online evening).
- **Also (2026-10-11 22:25Z):** Dimitri asked to test every implemented mechanic. All three probe sets ran again on the
  PC on one build of `27591bc` (the PC already matched it; no handoff): **208 pass, 0 fail** (Argo 139, RogueLab 22,
  RogueWavesMode 47), every exception the game's own; Argo's steep-shot check can skip when its test foe fails to
  spawn (passed on a second launch). Mods folder empty again. `docs/reports/FULL_TEST_2026-10-11.md`.
- **Also (2026-10-12):** Dimitri asked for a deep dive into every base vessel so modded assets, Directions and
  Cardinals work for every vessel. The game's code was decompiled (scratchpad only) and read per vessel; the result is
  kit `docs/17_BASE_VESSELS.md`, pitfalls #81-86, and fixes to `docs/12` §1 (Boutbro builds the fish), `docs/13` §6
  (Multi and Pmulti add up) and `docs/reports/SCOPE_VS_LOOP.md` (Bottom Rock counts the team). Nothing tried in game; `17` §12 is
  the PC list, worth a probe round before the first Direction is built.
- **Also (2026-10-12 06:10Z):** Dimitri asked to test every PC check possible. RogueLab 0.5.x (round 7, built and run
  on the PC in three launches: 24/2/1, 7/2, 7/0; every failure the probe's own) checked `17` §12 and `16` §9 items 1-3.
  Verified: percentages add, outside mods outlive upgrades, a −3 upgrade leaves Level and panel alone and the
  Bouthouse leaks it onto Boutman, the Bout Way's prices, price scales, unit counts, the `Sno` grab, a re-routed
  path, a den and an ore node spawned mid-match, a respawn keeping a −3 upgrade. New: an upgrade's params apply nothing
  by themselves; Health raises keep the health fraction; a respawn cut made at the death is too late (`09` #89-91).
  Left: a Blurry Photo clone and the house's upgrades. Mods folder empty again. `docs/reports/BASE_VESSEL_CHECKS_2026-10-12.md`.
- **Also (2026-10-13):** Dimitri asked for a vessel template with a slot for every value. New scope file
  `docs/scope/15_Vessel_Template.txt`; his edited `07` (Yossele) and `08` (Spool) are folded in and re-laid in it. A
  value written `~like this` is a default Claude picked (Yossele's come from the 2026-10-12 build, Spool's are new);
  each sheet ends with its open questions. Yossele's sheet now asks for a ranged laser and a slower speed, which the
  build (PR #30) doesn't have yet. Then, at his ask, `docs/scope/16_Base_Vessels.txt`: the five base vessels in the same
  template from the game's data (kit `reference/vessel_data_survey.txt`, `docs/17`), with a side-by-side stat table.
- **Also (2026-10-14):** Dimitri asked for one rule set for every scope file, approved the proposal
  (`/mnt/project-files/research/SCOPE_RULES_PROPOSAL_2026-10-14.txt`) as written, and asked for every file reformatted
  in his own words. Rules now in `docs/scope/00_Read_Me.txt`; every template (Vessel, Direction, Cardinal, Optional
  Objective, Enemy Modifier, Sea Weather, Bug) in `15_Templates.txt` (was `15_Vessel_Template.txt`); `04` renamed
  `04_Rogue_Waves_Mode.txt` and given the sea chart and sea weather from `05`, which is now a numbered walkthrough;
  Argo (built numbers, `~`) and Uloe (blank slots) laid into the vessel template; dates only in `02`.
- **Also (2026-10-14):** Dimitri asked to decompile the game and turn it into kits and lessons. The whole
  `Assembly-CSharp.dll` was decompiled into the thread's scratchpad (never committed) and six areas the kit was thin
  on were read in parallel and spot-checked by hand. Result: kit chapters `docs/19_NETWORKING.md` (with
  `reference/network_surface.md`), `20_UI_AND_HUD`, `21_MUSIC_SOUND_TEXT`, `22_NEW_MAPS`, `23_SAVE_UNLOCKS_LOADER`,
  `24_ENEMY_BODIES_AND_BOSSES`; five recipes in `examples/recipes/` that compile with cloud-check; pitfalls #93-101;
  corrections in `docs/01`, `03`, `05`, `07`, `13`, `14`, `15`, `docs/mode/REJOIN.md`, `docs/PACK.md`,
  `docs/mode/SEA_CHART.md` and `docs/EXPANSION_SCOPE.md`. Findings for the pack's own code: a friend who **restarts**
  the game gets a new `Sessionid` and RogueWavesMode turns them away (`docs/mode/REJOIN.md` O3b); one client that
  can't load a map sends **every** machine to the main menu after 120 s; debuff looks and stock effects drop silently
  in a busy online fight; campaign statics survive a save-slot switch. Each chapter ends with a PC list; the online
  evening list is `docs/19` §8.
- **Also (2026-10-15):** Dimitri said "fix those". **RogueWavesMode 0.3.0** (`Code/10_Online.cs`, `06_Campaign.cs`):
  every machine keeps its player GUID in the pack's save folder and puts it back after the game wipes it, so a
  restarted friend keeps their `Sessionid` and seat (`RWIdentity`); the host sends back a player still loading 75 s
  into the loading screen (`RWLoad`); the campaign leaves itself behind when the save slot changes
  (`RWCampaign.Live`). **Argo 2.0.1**: its slow look and refused-flood mark go past the effect throttle. Compiled (0
  errors, release and self-test); **not run**. Next PC round of the mode adds I1, L1, S1/S2; online:
  `docs/mode/REJOIN.md` O3b, O3c, `docs/19` §8 N4, N9, N14. Yossele (PR #30) still sends its looks through the
  throttled call; its thread can copy Argo's `ArgoUtil.Debuff`.
- **Also (2026-10-15):** Dimitri asked for a deep dive into the art of the game's vessels and enemies, with rules for
  new designs. The game's asset bundle was copied read-only from the PC (`StreamingAssets` connected in the thread)
  and read with UnityPy; the drawing code was decompiled. Result: kit `docs/18_CHARACTER_ART.md`, survey
  `reference/art_survey_2026-10-15.txt`, palettes `reference/palettes/*.gpl`, pitfalls #87-88, fixes to `docs/03` §1
  (facings, size, edges), `docs/13` §5-6 and §10 Q5 (elite looks are overlay materials), `docs/STYLE.md` "Enemies"
  (all foes share one palette). Extracted sheets (not committed): `/mnt/project-files/research/art_dump_2026-10-15/`.
  Spool's purple sits next to the foes' magenta; Dimitri: leave it as is for now (Decisions).
- **Also (2026-10-15):** Vessel Spotlight artifact (https://claude.ai/artifact/NZ3wMMaujKDSNSaYCw7NZw), journal-style
  pages for the base five and for Spool and Yossele from Dimitri's own art and sheets only; WIP covers show what they
  still lack (every sprite but one front frame, all UI art, tower models, numbers, costs, lore). Source and rebuild:
  project files `concepts/vessel_spotlight_2026-10-15/`.

- **Also (2026-10-15):** Dimitri asked for two quality checks of every asset and a cleaner layout. Five readers per
  pass (kit, code, tools, docs and scope, open PRs); 128 findings, then 49 more. Fixed in place where no game behaviour
  changes; dated reports moved to `docs/reports/`, the mode's design pages to `docs/mode/`, `README.md` rewritten,
  Riptide compiles here again. What is left for Dimitri, for each open PR, and how to merge them:
  `docs/reports/QUALITY_CHECK_2026-10-15.md`.
- **2026-10-15/16, quality passes:** 23 passes over every file, ten readers each through pass 8 and thirteen from
  pass 9 (179, 61, 27, 37, 16, 2, 0, 7, 39, 37, 18 and 8 issues; then, counting only wrong facts, 8, 10, 16, 4, 1, 4,
  3, 0, 2, 5 and 2). Fixes changed no game behaviour; scope `16`'s base-vessel lines now say what the game's code
  does. Dimitri stopped the run after pass 22 (pass 23 had already finished, its two fixes kept); why the counts
  stayed up, and what is listed for him: `docs/reports/QUALITY_PASSES_2026-10-15.md`.

- **2026-10-16, scope default marker:** at Dimitri's ask a Claude default in the scope is written `~value~` (a `~`
  on each side) instead of `~value`; every marker in `docs/scope/` was closed (rule in `00_Read_Me.txt` [Symbols]).
  The spotlight's `parse.py` (project files) reads both forms; its rebuilt output is unchanged.

- **2026-10-20, Yossele sheet:** Dimitri's new `07_Vessel_Yossele.txt` folded in (layout, markers, his values made his
  where he changed them inside `~ ~`, Cordyfriend in the minion template, the template's `- Numbers:` slot where only
  prose held a number); five questions at its end. The gap between the sheet and the 0.1.0-dev build is listed in
  `TDS_Mod/ModSource/Yossele/IMPLEMENTATION_NOTES.md` ("Gap to the 2026-10-20 sheet").
- **2026-10-20, release order:** at Dimitri's word the first release is Yossele alone and everything else follows.
  Part B gained "Release order" and "Release 1"; `docs/PACK.md` §5 and scope `01`, `02` and `04` say the same; four
  release questions with defaults in scope `01`.
- **2026-10-21, answers:** Dimitri answered scope `01`'s and `07`'s questions: every default kept except Bonsai, limit
  1 (planted at the Tree's cost). The Tree's level 4 upgrade is Dandelion Storm again; Dandelion Season is only the
  Direction. Both `[Questions]` blocks are gone.
- **2026-10-23, Yossele 0.2.0-dev:** at Dimitri's ask the mod was brought up to the 2026-10-20 sheet: the ranged mouth
  laser (Sno Sno's ranged AI, a clone of its snowball), the new stats, 5 Growth stages a Tide on maps of 4 Tides or
  fewer, Cordyceps and the Cordyfriend (the Snoble's look, tinted, cap 30), D-MDS, Bonsai, Rubbery Shell, Easy Target,
  Obsidian, Monument, Spontaneous Mulch, Fortifying and Softening Roots, the changed rules and names, art for each,
  and the carried-over fixes B3-24 to B3-31, B3-36 and B3-39 (Connected Roots: the owner's buildings, as the sheet
  says). Compiled 0 errors in all three builds. Pitfalls #105-110. What is left to check and where:
  `docs/reports/YOSSELE_TEST_PLAN_2026-10-23.md`.
- **2026-10-23, PC round 9 (Dimitri's OK, 21:23Z):** scope folder on the PC refreshed (17 files; the stale `04` file
  deleted). Self-test of 0.2.0-dev: 9 154/6, 9b 157/2, 9c stopped at the apply check (made from the wrong base,
  nothing changed), 9d 157/2, **9e 160 pass, 0 fail**; every failure was the test itself or a stock look that isn't
  loaded (#110), none a rule of the sheet (Yossele's `IMPLEMENTATION_NOTES.md`). Release boot check clean; 0.2.0-dev is
  his play build, 0.1.0-dev parked on the PC. Pitfalls: #106 corrected (a node pays on the hit after `Healthresource`
  reaches 0: `Resource_Orange` has `Health` 1), #111 (a foe's health reads 0 right after its spawn).
- **2026-10-23, Spool answers and the Vessel Editor:** Dimitri's `08_Vessel_Spool1.txt` folded into scope `08` (the
  Yarn Stash is the one supply base, the Cookie Tin uses its Thread, the Thread Head returns there and is bought from
  the build menu with no limit, Patch heals up to 500; Questions 1-3 gone). His `07_Vessel_Yossele2.txt` equals scope
  `07` already. The Vessel Spotlight (version 3, same link) shows his reshade frames and reads the sheets with a new
  JS parser; the new **Vessel Editor** (https://claude.ai/artifact/JiVvnHii85Fg9c1DBMC6wj) edits Spool and Yossele or
  a new vessel from the template, saves in the page, and exports the `.txt` byte for byte with `~defaults~` (tested:
  07, 08, 09, 10, 16 and both uploads round-trip unchanged). Its source and README are in project files
  `concepts/vessel_spotlight_2026-10-15/`.
- **2026-10-26, template before Spool:** the kit template (Mummer's scaffold) took the 1.6 pack calls, `ModKitCombat`
  kill credit, `ModKitTowers.Fit` tower ranges and unthrottled debuff looks; the Aseprite writers keep unit sheets named
  after the vessel (B10, B1-3, B1-22, B1-4, C2-6). Compiled, not run.
- **2026-10-26, Spool 0.1.0-dev:** Dimitri: "Begin implementing Spools mechanics." Built from scope `08` as PR #40 leaves
  it: the Yarn Stash first (the menu holds only it until it stands), Thread dropped by foes and carried home by Spool
  and Thread Heads (ground Thread is a host list drawn by each client, not network entities), the Cookie Tin and its
  Crafted (TARGET with the fish's picker), the Thread Head (placed through a walk-through token; SPOT, RETURN, Threaded
  Delivery, MEND, comeback with its upgrades), the Pin Cushion's hit hook (reflect, spin, Slow Unravel, Mangled), the
  Loom (spins Thread, Direct Line, Seams), PATCH; every upgrade the sheet names. Temporary art from Dimitri's reshade
  (the walk is the 2026-10-12 concept, pixel for pixel), 19 models, 6 effects, 14 sounds. `check_mod.py` 0 errors in
  release and both test builds, `check_editor.sh` clean. Defaults: `TDS_Mod/ModSource/Spool/DESIGN.md` "Defaults
  picked here".
- **2026-10-26, Spool PC round 1** (Dimitri: "Go ahead"; Yossele parked and put back each time): 1 = 95/22, 1b =
  117/3, 1c = **120/0**. All but one fail were the self-test tripping over itself (foes killed in their spawn frame,
  pitfall #112; Spool's own army felling the test's foes; Tins and Thread Heads moving Thread mid-check); the one real
  slip, Slow Unravel's delayed half starting a frame early, is fixed. No exception from Spool. The self-test sends the
  build, sell, TARGET and SPOT commands itself, so the clicking (pickers, build menu) waits for Dimitri's play test.
- **2026-10-26, play test:** at Dimitri's "Do a test run of Yossele and Spool as if you were a player testing these
  characters", a debug-only virtual player (kit `ModKitPlay`, play builds only; Input System events on the game's own
  mouse and keyboard, so the HUD, the map and the keys get a hand's input) played 12 runs on Purlest, Woozy and The Hunt.
  Found and fixed: the Thread Head's panel never drew (RETURN's and MEND's keybinds named input actions the game doesn't
  have; PR #42, pitfall #113) and Hallucinogen's stun look is a stock effect Woozy doesn't load (PR #41, pitfall #114).
  Balance, what was clicked and what needs Dimitri: `docs/reports/PLAYTEST_2026-10-26.md`.
- **2026-10-28, priorities:** at Dimitri's ask ("figure out what is most needed right now") the scope was read
  against the state; the ranked list for Release 1 and what waits after it is `docs/reports/PRIORITIES_2026-10-28.md`.
  Nothing built.
- **2026-10-28, defaults and the pack (Dimitri: "Use defaults for now, then proceed"; round OK 20:07Z):** defaults
  logged (Decisions), scope `07` asks for his sprite sheet, the pack step built. PC round Yossele_round10: self-test of
  0.1.0-dev **160/0**, the three temporary sheets used, release boot check clean; the first pack build `PACK DONE ok`,
  its boot check clean at v0.1.0; `handoff\RogueWaves-0.1.0.zip` (3.1 MB) made and held for his sprites. His play build
  is now 0.1.0-dev with the Daze fix and the temporary art (`TDS_Mod/Handoff/Yossele_round10/README.md`).
- **2026-10-28, engine study and scope fit:** at Dimitri's ask ("understand as much as you can about how it functions,
  and plan how ideas in the scope could seamlessly fit in without needing to create new mechanics") the game was
  decompiled again in the cloud (never committed) and read against the kit. `docs/reports/ENGINE_STUDY_2026-10-28.md`
  walks it system by system, sorts every road into four rungs (stock data, a stock script on pack data, a stock pattern
  in a pack brain, a new mechanic) and lists 13 facts new to the kit, all read only (the same upgrade twice does
  nothing; nine stock Cardinal scripts need their Cardinal passed; Chrono starts at ×1.25; Teamapply reaches every
  vessel's builds). Its appendix catalogues 284 stock behaviour scripts: 245 can be put on pack things by data.
  `docs/reports/SCOPE_FIT_PLAN_2026-10-28.md` walks 61 scope ideas: 19 on stock data or a stock script, 10 mixed, 32 on
  a stock pattern, none on a new mechanic once Uloe's light is a stat and Photons walk like Kippy; it offers
  stock-script options for six `! NEEDS !` lines (scope untouched) and eight PC probes. Nothing compiled or run; the PC
  was not touched. PR #48.
- **2026-10-29, RogueLab round 8:** at Dimitri's ask ("Do a PC round to check") RogueLab 0.6.0 (`10_Round8.cs`) ran the
  fit plan's probes R1-R8 and the engine study's N1-N5, N7 and N13 on a solo host: 20 pass, 1 fail (R6's own
  bookkeeping), then **21 pass, 0 fail** after a one-file fix (handoff `RogueLab_round8b`). Seen in game: a stock script
  runs on pack data once it has every param key it reads; the same upgrade twice does nothing and differently named
  copies stack; removing one of two upgrades that share a script keeps both running; Teamapply reaches every player's
  buildings and keeps reaching new ones after removal, while an owner-filtered spawn hook reaches only that player's;
  a light stat sums sources that each replace their own 1.5 s mod; a death node pays once; Vintage and Coin work as pack
  Cardinals with their `Artifactobject`; Melting's drain stays; Smackywall's regen heals at the Tide change; Chrono
  starts at ×1.25. New kit pitfalls #115-#120, kit `16` §3 (Chrono), `17` §9 (Teamapply), `13` §3 (`30 / Totalwaves`),
  `08` §6 (shutdown noise); every line in `TDS_Vessel_Creator/reference/roguelab_round8.txt`. Dimitri's Yossele
  0.1.0-dev play build was parked for each launch and put back. PR #48.

- **2026-10-28, temporary art versions (Dimitri: "For all temporary assets, create alternative versions for me to choose
  for each asset. There should be different versions for every single animation included for Yossele and Spool. Create
  3 versions of each asset for now."):** three new versions of each of the 2026-10-27 assets, in
  `ModSource/<Part>/Art/Temporary/variants/v1|v2|v3/` (same names, sizes and layouts, so a pick drops in): Yossele's
  idle, walk and attack (moved from the code's frame of his reshade), the Cordyfriend (three builds), the fruit model;
  the Thread Head (his zipper mouth in all three), the four Crafted (their bodies kept, new walks and attacks), the
  tuft, the needle model; the fifteen 12 x 12 icons and the 32 px crest. 35 picks, one per animation, so versions
  can mix. Generators `concept-tools/temp_art/variants_*.py`, `apply_picks.py`; page `variants_page.py`. Nothing
  wired changed and nothing was seen in game.
- **2026-11-03, mode polish (Dimitri's "Run 3, 4, and 5", item 4):** RogueWavesMode checked line by line against scope
  `04` and `docs/mode/`, two passes: 15 issues fixed as **0.1.0-dev** (the pack version plus `-dev`): 3 retries (only
  the lost map can be picked until won), the host leaving a running map loses it (saved while it runs, counted on the
  boat or by Continue), won once Tide 15 is cleared, ten maps complete the campaign and the party goes home, 60 s
  breaks, `RW_` save keys with a one-time rename, at most 3 links per chart map (`sea_chart.py` too), scope `04`'s
  weather lines, rejoin B4-12/B4-14, the dev description (B4-13), and B2-1 on the mode's side. Compiled 0 errors in all
  three builds; chart parity on 2,001 seeds. New self-test checks K0, CH, K1, P1, T0-T6, H1; not run.
  `docs/reports/MODE_POLISH_2026-11-03.md` (§2 readings and §3 judgment calls for Dimitri).
- **2026-11-03, temporary art picks applied (Dimitri's list, one pick per animation):** 25 of the 35 changed (the
  rest kept current): Yossele's attack v2, the Cordyfriend v1, the fruit v2; Thread Head v1, Stitched walk v3, Dart
  walk v2, Seam and Mangled v2, tuft v1, needle v1; Rising Water v2, crest v1 (both sizes), Pure Aura v3, Depth v1,
  four Direction icons and Vampire Dentures. `apply_picks.py` pasted them into `game_size/` and rebuilt the
  `.aseprite` copies, previews and GIFs; Rising Water's rows in `08_Icons.cs`, the tuft rows in `11_Client.cs`,
  `YosseleModels.Fruit` and `SpoolModels.Needle` changed by hand. All compiled in the cloud check; none seen in game.
  The `variants/` folders stay until he has seen the picks in game. The list: `docs/TEMP_ART.md`.
- **2026-11-03, UX features (Dimitri: "additional features that would fit into the game while enhancing user
  experience, such as buttons, transition screens or multi-player lobbies"):** a feasibility study, cloud only, no code
  (`docs/reports/UX_FEATURES_2026-11-03.md`). The game's screens were decompiled again and read (loading screen, map
  intro card, end screen, pause menu, boat world card, main menu, Mods menu, chat, kick). All three examples fit on
  stock UI: buttons as clones that open the choice dialog (no new menus), transitions as text on the game's own five
  screens (one "campaign card": map, stretch, weather, retries), and the boat stays the lobby. 33 features feasible,
  5 not possible (own menus, lobby browser, more than 4 players, host migration, Discord presence). Recommended first:
  the campaign card, retry warnings, a "friend has no mods" warning with rejoin instructions, weather icons on the
  chart, the pack's tips. Ten facts new to the kit (X1-X10, read) and ten solo probes (U1-U10) wait for a round.
  Dimitri picked the five recommended (decision card, 06:21Z); they are on the Phase 5 list.
- **2026-11-03, balance study (cloud only):** at Dimitri's ask (a long virtual play test of Yossele and Spool), a
  simulator of a map Tide by Tide (`docs/reports/balance_2026-11-03/`, Python, no game code) was calibrated on the
  2026-10-26 play test and run about 13,000 times on Purlest, Woozy and The Hunt, Smacky as the yardstick. Found: the
  2026-10-26 play test had three Coin Cardinals equipped; Red is every vessel's scarce colour and Purlest's Opal is
  what piles up Blue; Yossele is at or above Smacky everywhere (no change; moving its costs to Blue loses Woozy);
  Spool's thin first Tide is the missing Pin Cushion wall, and it loses Purlest at Tide 11 to the Maul and Tempered
  Aegis. Proposed (his pick, Open questions 3): Dart Range 4.5, 6 Crafted a Tin, Stitched 3 / Dart 3.5 Damage (all
  Claude `~defaults~` in scope `08`), a "wall first" line; a PC round to confirm (report §9).
- **2026-11-03, aura baseline (Dimitri: "Calculate the average aura per map, as well as each base vessels aura mining
  capabilities per map and use those calculations as a baseline to balance yossele and spool"; cloud only):** every base
  vessel's gatherer read in the decompiled game (Mining Hut miner, Sno Box, Silo Pipes, Checkpoint; Boutman has none
  before Fishbender; every other vessel mines by hand when idle) and the nodes of Purlest, Woozy and The Hunt matched
  from the play test's ground dumps. A node pays every 5 hits of Damage 1 or more, 10-18 of the stock gatherers'
  smaller hits. One stock gatherer by a Red node earns 17-36 aura a Tide (30-106 with its first upgrades); a base
  vessel ends a map with about 4,800 (8,100 Purlest, 3,700 Woozy, 2,600 The Hunt; kills 885 Red on every map).
  Yossele's Sapling earns 60-90 (2.0-2.5× the base average a map), Spool's Loom 96 (4.1-5.3×), its Seam path 288, and a
  Sapling whose fruit is all picked up 883. Found: the first study's Smacky Hut mined 1.7× the game's (Smacky loses
  Woozy at Tide 10, not 12). Checked in the simulator (1,500 runs): a Sapling at 0.15 keeps Yossele at or above Smacky;
  a slower Loom leaves Spool no Blue for its wall (loses Woozy at Tide 3-5), so it stays. P1-P4 stand. Options for
  his pick (Open questions 4): P5 Sapling Attackspeed 0.15, P6 fruit × 5, P7 scope `08`'s Loom line to what is built;
  PC checks M8-M11 (node census, gatherers timed, the Seam, The Hunt's beasts). `aura.py` and experiment J in
  `docs/reports/balance_2026-11-03/`.
- **2026-11-03: every planned feature, built and tested in game** (Dimitri: "Create all planned features and assets, then test them in game
  to make sure they function and fit seamlessly into the game. Any feature added should be to the quality of the base game."). Kit 1.7; the
  mode's main-menu button, staging map, Pure Aura, VENDCINT, Directions, Cardinals, objectives, Fund Transfer, votes, bonus Tides, difficulty,
  card and warnings; the foes part; vessel Directions for Yossele, Spool, Argo; Uloe from `docs/scope/10`. Self-tests in game: mode 139/0 (and with
  every part on), foes 12/0, Yossele 160/0, Spool 121/0, Argo 136/0, Uloe 147/0/3. Playing it caught what compiling never would: the chart's
  camera looked at the sky, the Tide table was read one row off, a dead nest stopped the Tides for good (pitfalls #121-#136). `docs/reports/BUILD_2026-11-03.md`.
- **2026-11-03: the balance round** (`BALANCE_2026-11-03.md` §9, `AURA_BASELINE_2026-11-03.md` M8-M11), no shipped number changed; `docs/reports/BALANCE_ROUND_2026-11-03.md`. The model's nodes, gatherers and Crafted hold; its wall doesn't: foes walk round a three-tile line, and walling first lost Tide 1-2, not Tide 11. Tin first, by a bot: Spool as written Purlest Tide 4, Woozy Tide 2; with P1-P3 Purlest Tide 5, Woozy Tide 2. Yossele on Woozy, Tide 14. Harness: `Code/97_Measure.cs`, `ModKitMeasure`. The simulator's re-run with these numbers is left for a cloud thread.
- **2026-11-03: the simulator re-run on the round's numbers** (cloud only, analysis only: no mod or scope number changed; `docs/reports/BALANCE_RERUN_2026-11-03.md`, simulator in `docs/reports/balance_rerun_2026-11-03/`, 11,880 runs). The Opal pays every colour on every payout (about 21 aura a strike, not 9.5); Woozy's first node matters more than any number. Calibrated within a Tide on Spool Tin first; four Tides harsh on Yossele's bot on Woozy. Spool: with a wall that holds, P2 and P3 alone win Purlest in 72% of runs and give Woozy two Tides, P1 helps only if Darts stand back; on a line foes walk round, no number buys more than a Tide, and Stitched that block foes would (Tide 10-11 everywhere, a design question). Yossele: P5 changes no map's result (some Woozy and Hunt runs end two Tides sooner); fruit is The Hunt's only Blue and decides it, P6 keeps most of it. The Hunt has no Blue node: Spool loses there at Tide 5 whatever its numbers, Tide 10 if it hunts two small aura beasts a Tide (whether a vessel can kill them is the next measure).
- **2026-11-04: new Directions and Cardinals** (Dimitri: "Create new directions and cardinals based on the ones I had already
  conceptualized. Look into Starcraft 2's campaign's upgrade paths..."; his Yossele-and-Spool limit lifted for this ask). 21 Directions
  (8 Any, 3 Yossele, 3 Spool, 2 Argo, 1 more per base vessel) and 13 Cardinals (8 Vessel, 5 Team), Claude `~defaults~` at the stock
  Cardinals' scale, each with the StarCraft II upgrade behind it and the game piece it rides on (read, not built). New scope rule: every
  Direction and Cardinal ends with `By: Dimitri` or `By: Claude`; `06` and `12` gain Power and Pairs lines. `docs/reports/DIRECTIONS_CARDINALS_2026-11-04.md`.
  Scope Editor artifact https://claude.ai/artifact/5KAGb5wdqiNV1bEoxNMgEo (every file 00-16, line edit, filters by maker, Export .txt; edits in its
  db collection `scopes`, doc `f<NN>` per edited file; rebuild from project files `concepts/scope_editor_2026-11-04/`).
- **2026-11-04: Argo diver concept sprites** (Dimitri: "Create 5 new sprite arts of Argo based on his idea of being a deep sea diver
  that might not entirely be human"; the Yossele-and-Spool limit lifted for this ask). Five takes over Argo's shipped standing
  frame, each a 6-frame idle and a back view at 32 px with an `.aseprite` and 8x previews, checked against kit `docs/18` §4.2:
  Barnacled (the sea grew on him), Angler (a lure and fins), Hollow (an empty suit full of sea), Passenger (an octopus
  drives it), Pressure Hull (a later hard suit with a light for a face). `docs/concepts/argo_divers_2026-11-04/README.md`;
  picks page https://claude.ai/artifact/FnchhGQBHvi9JyhC47TCe2 (db collection `picks`, doc `argo`); generator
  `concept-tools/argo_divers/`. Concepts only: Argo's mod and `docs/STYLE.md` are unchanged until he picks.
- **2026-11-04: Cardinal overlap check and roguelike Directions and Cardinals** (Dimitri: "Check to make sure that the new cardinals you
  created do not overlap with already existing cardinals... develop some more directions and cardinals using other roguelikes as inspiration").
  Checked against the 40 stock Cardinals' in-game lines (kit `reference/roguelab_round1.txt`): Iron Rivets (Rusty Pipe), Siren Conch (Blue
  Paint), Kelp Net (Frozen Computer), Lucky Geode (Starter Seeds) and Storm Lantern (Sport Water) removed. Added 11 Directions (Notched Oar,
  Lamp Oil, Fish Hook, Spare Compass, Magpie Nest; Duos Fire Ship and Tide Pool; Yossele Deep Breath, Fairy Ring; Spool Merit Badge, Odd Socks)
  and 7 Cardinals (Punch Card, Pearl Oyster, Bottled Storm, Oilskin Coat, Gull Feather; Team Cargo Crate, Tin Whistle). New: the Duo kind
  (`06`, `15`) and a No copies line (`12`). Then, at his word (04:49Z), Claude's seven earlier Cardinals that copied stock ones are removed
  from scope `12` and from `RWCardinals.Mine` and their icons (RogueWavesMode `IMPLEMENTATION_NOTES.md`; compiled here, not run on the PC).
  `docs/reports/ROGUELIKE_DIRECTIONS_CARDINALS_2026-11-04.md`.
- **2026-11-03: the choice dialogs' placement.** Dimitri: "The UI for VENDCINT and selecting the Rogue Waves button is too far to the side, cutting off many options." Every dialog the mode asks now stands in the middle of the screen with every card and word on it, at any size and card count; the stock dialog slides in late, so the old centring measured it mid-slide (pitfall #137). 2026-11-04, Dimitri: "All of the buttons have very tiny stutters": my placement froze one frame of 83 to 146 ms each time a dialog opened (pitfall #138); now no opening of 10 has a frame over 25 ms, as stock. In game on that code: 70 pass, at four screen sizes, and 148 pass in the whole campaign with every part on and #66's Cardinal removals, whose first PC run this was: 30 on sale, all go on with no error. Release builds with all of it are in `TDS\Mods`. His eye on the stutter is still owed: his message doesn't say which kind he saw. `docs/reports/DIALOG_PLACEMENT_2026-11-03.md`.
- **2026-11-04: Argo Hollow, round 2** (Dimitri picked Hollow on the page, "play around with the body shape and size more", and in the
  thread: "Create more concepts based on the hollow choice, but play around more with the body and head's shapes"). Five shapes keeping
  the empty, sea-filled suit: Bell Jar (tall, thin), Fish Tank (wide tank head, stubby body), Slump (sunk into a pear on the floor),
  Fishbowl (a glass globe head, a coat on a hanger), Hunch (helmet slid forward, cut air hose). Same checks and outputs;
  `docs/concepts/argo_hollow_2026-11-04/README.md`; round 2 sits on top of the same picks page (doc `hollow`); generator
  `concept-tools/argo_divers/hollow_shapes.py`. Still concepts only.
- **2026-11-04: Argo Hollow, round 3** (Dimitri: "None of the round 2 versions appeal to me. Try 10 new designs"). Back to round 1's
  Hollow helmet (riveted brass dome, valve, sea in the porthole) with ten bodies varying helmet size and body shape, size and weight:
  Big Head, Long Legs, Lead Boots, Waterlogged, Broad, Air Tank, Stout, Lopsided, Bell Skirt, Long Arms. Same checks and outputs;
  `docs/concepts/argo_hollow_ten_2026-11-04/README.md`; round 3 on top of the same picks page (doc `hollow3`), rounds 1-2 folded
  below; generator `concept-tools/argo_divers/hollow_ten.py`. Still concepts only.
  Then Dimitri, 05:31Z: "Keep the original for now." Argo keeps the shipped look; no diver art is drawn into the mod.
- **2026-11-04: Argo's kit reviewed** (Dimitri: "Do a review on Argo's current kit. Is it mathematically balanced? Do the
  mechanics work well together in a way that has skill expression? Make any changes deemed necessary, then update the scope editor
  artifact."). Read from the built code, hand numbers, and the balance simulator with Argo added (84 setups, 2,940 runs). Found: a slow
  stacks with its own re-ticks (a 30% pool slows 60%), worth one to four Tides; with that fixed Argo sits between Smacky and Yossele.
  The Drill mines like a Sapling, and Fathom ore past Fathom 3 and Mantle Tap's ×2 mine nothing. Pins are read four times and made
  almost nowhere. Blue starves the crew and the water on Woozy and The Hunt. AIM is worth seven Tides on Woozy. Fourteen changes in scope
  `09` (Claude `~defaults~`, each with a "Review 2026-11-04: was ..." note), the code list in Argo's `IMPLEMENTATION_NOTES.md`; Scope Editor
  republished on the same link. `docs/reports/ARGO_REVIEW_2026-11-04.md`, simulator in `docs/reports/balance_argo_2026-11-04/`.
- **2026-11-04: Uloe's upgrades fleshed out** (Dimitri: "Develop upgrades for Uloe until they're fully fleshed out."). Uloe had 8
  upgrades (Glass 1, Bank 1, Mirror 4, Flood Light 2), no capstones. Now 32: each building 8, two roots, crossing branches, one early end,
  a capstone (Death Ray, Supernova, Heliostat, Stadium Lights or Lighthouse). They fill five holes: crowds (Prism, Focal Point, Chain
  Lightning), a colour trade (Colour Filter), a parked frail vessel (Surge Protector), no slow (Searchlight), no capstones. Hand numbers:
  unlit about half of Smacky's towers, at Light 8 level with them, at Light 12 a quarter or more above. Blank stat slots in scope `10` now
  hold the built numbers. `docs/reports/ULOE_UPGRADES_2026-11-04.md`, numbers in `docs/reports/uloe_upgrades_2026-11-04/`; Scope
  Editor republished on the same link.
- **2026-11-04: every untested feature tested** (Dimitri: "Develop upgrades for Uloe until they're fully fleshed out. Afterwards, make sure to test all untested features of the mod."; Uloe's upgrades are their own thread). On the PC: a real second player through a second copy of the game on the loopback address (`TDS_Mod/Tools/pc/Run-TwoPlayers.ps1`, `RogueWavesMode/Code/93_TwoPlayers.cs`), the solo gaps (`92_Gaps.cs`), new Direction and rate checks in the vessels' self-tests. Ten bugs fixed; pitfalls #139-#149; `docs/mode/REJOIN.md` §6, kit `docs/19` §8 and `docs/RESEARCH_GAPS.md` §E say what is left. Report: `docs/reports/TEST_ROUND_2026-11-04.md`.
- **2026-11-04: minion versions** (Dimitri: "Create new sprites for all modded minions. They should be closer in size to
  snobles. Create at least 10 versions of each."). Eight minions counted from the scope and the builders: Cordyfriend
  (Yossele), Thread Head and the Crafted Stitched, Dart, Seam, Mangled (Spool), Photon (Uloe), Naut (Argo; Port,
  Starboard, Bosun). The Snoble measures 6-8 x 5-8 px in 12 px cells; today's minions are 9-15 x 10-21, the 80 new
  versions mostly 6-12 across and 8-12 tall, the Thread Head the biggest. Every version keeps its wired sheet's name,
  size and layout (`ModSource/<Part>/Art/Temporary/minions_2026-11-04/`); Cordyfriend and Spool picks are a file copy,
  Photon and Naut picks first need a `FileArt` hook in their builders. Picks page https://claude.ai/artifact/Rj9xQXsYUnauTGQyiFfAJX (collection `picks`, one doc per
  minion); generator `concept-tools/minions/`; GIFs in `/mnt/project-files/concepts/minions_2026-11-04/`. Nothing seen in game.
- **2026-11-04: smoothness in real play** (Dimitri: "I am unable to test that at the moment, continue with other test features for now."). The one gap the test round left: no frame had been timed in real play. Nothing here is the mod's to fix. Two campaigns with every part on, 148 pass each, hold 144 Hz nearly all the time. In a map, a dialog's opening and a real card's hover cost the same in the mode's layout as in the game's own, and VENDCINT's landing is clean. A changed line of text costs one or two dropped frames in a map, in the game's own layout too, which may be the stutter he saw; that stays his to say. Test-only timer behind `ROGUEWAVESMODE_FEEL`. `docs/reports/SMOOTHNESS_2026-11-04.md`.
- **2026-11-04: "Now"'s Done list, moved here word for word** by the repository tidy
  (`docs/reports/REPO_QUALITY_CHECK_2026-11-04.md` S12), so "Now" stays the short state Part A asks for. It read:
  > Phases 1-2, Phase 3 Argo, and the mode part `RogueWavesMode` (0.2.1: sea chart, sea weather, rejoin; solo PC
  > rounds passed; full re-test 2026-10-11, 208/0; 0.3.0 below). Yossele 0.2.0-dev is built and is Dimitri's play build
  > (2026-10-23 below; 0.1.0-dev, PC round 8, 98/0, is parked). Kit chapters 17 (base vessels; RogueLab
  > round 7 checked it in game, `docs/reports/BASE_VESSEL_CHECKS_2026-10-12.md`) and 18 (character art); the Vessel
  > Spotlight pages (Log). 2026-10-14: every scope file follows one rule set (`docs/scope/00_Read_Me.txt`), with every
  > template in `15_Templates.txt`; a full decompile gave kit chapters 19-24 and pitfalls #93-101. 2026-10-15:
  > RogueWavesMode 0.3.0 and Argo 2.0.1 fix the four problems it found (compiled, not run; Log); two quality passes and
  > a reorganisation (`docs/reports/`, `docs/mode/`; `docs/reports/QUALITY_CHECK_2026-10-15.md`); then passes over every
  > file: twelve passes, then eleven more counting only wrong facts, stopped at Dimitri's word once pass 23 was in (one
  > clean pass, pass 20, never two in a row); what was found and why is in `docs/reports/QUALITY_PASSES_2026-10-15.md`.
  > 2026-10-16: scope defaults are written `~value~` (Log). 2026-10-23: Yossele 0.2.0-dev, built from the 2026-10-20
  > sheet; PC round 9 passed its self-test 160/0 and it is Dimitri's play build (Log). 2026-10-23: Spool's answers folded
  > into scope `08`; the Spotlight rebuilt on his newest sheets and reshade sprites, plus a Vessel Editor page that exports
  > the sheet (Log). 2026-10-26: at Dimitri's ask, Spool 0.1.0-dev is built from scope `08` with temporary art; its first
  > PC round passed its self-test 120/0 on round 1c (Log). 2026-10-26: Yossele 0.2.0-dev and Spool 0.1.0-dev played
  > through the HUD by a virtual player on three maps; two bugs fixed on PRs #41 and #42 and seen fixed in game; Spool's
  > first Tide is too thin (`docs/reports/PLAYTEST_2026-10-26.md`). 2026-10-27: a temporary art pass
  > (`docs/TEMP_ART.md`): the Cordyfriend's own sheet and a bigger fruit (Yossele, wired, compiled), the mode's weather
  > icons redrawn (wired) and twelve more icons for the crest, Pure Aura, Depth, retries, the vote, the scope's Directions
  > and Vampire Dentures (files), Spool's tuft and needle (wired on PR #42); then temporary animations: Yossele's
  > knuckle walk and laser attack (files over the code's sheets, wired, compiled) and Spool's Thread Head and four Crafted
  > redrawn (wired on PR #42 at `ad85b00`). 2026-10-28: a mistake check over every file, two passes
  > (`docs/reports/MISTAKE_CHECK_2026-10-28.md`): 31 found, 28 fixed (stale Yossele state in eleven places, scope `16`'s
  > Yossele row and two accent colours, three dev-export versions, pointers), every mod compiles clean. 2026-10-28: the
  > game's code read system by system and the scope fitted to it
  > (`docs/reports/ENGINE_STUDY_2026-10-28.md`, its catalogue of 284 stock behaviour scripts, and
  > `docs/reports/SCOPE_FIT_PLAN_2026-10-28.md`): every scope idea rides on a stock mechanic, none needs a new one (Log).
  > 2026-10-29: RogueLab round 8 checked it on the PC, **21 pass, 0 fail** on the rerun: all eight probes and seven of
  > the 13 facts are now seen in game, in the kit as pitfalls #115-#120 (Log).
  > 2026-10-28: three versions of every temporary asset and every animation to pick from (`Art/Temporary/variants/v1-v3/`,
  > nothing wired changes; Log). 2026-11-03: the mode polish, RogueWavesMode 0.1.0-dev (retries, Tide 15 wins,
  > 60 s breaks, `RW_` keys; compiled, not run; Log). 2026-11-03: Dimitri's 35 picks applied to the wired temporary art
  > (`docs/TEMP_ART.md` "Dimitri's picks"; compiled, not seen in game; Log). 2026-11-03: a UX features study; Dimitri
  > picked its five recommended features for Phase 5 (`docs/reports/UX_FEATURES_2026-11-03.md`; Decisions; Log). 2026-11-03: a balance study of Yossele and Spool in a simulator
  > calibrated on the 2026-10-26 play test (`docs/reports/BALANCE_2026-11-03.md`, cloud only): Yossele unchanged; Spool's
  > proposals P1-P4 wait for his pick (Open questions 3; Log). 2026-11-03: an aura baseline read from the game's code
  > (`docs/reports/AURA_BASELINE_2026-11-03.md`, cloud only): a base vessel earns about 4,800 aura a map; Yossele's
  > Sapling mines 2-2.5× and Spool's Loom 4-5× the base average; P1-P4 stand; options P5-P7 (Open questions 3-4; Log).
  > 2026-11-03: **every planned feature built and tested in game** (`docs/reports/BUILD_2026-11-03.md`): the whole campaign, from the main-menu
  > button to the end screen, self-test 139/0 in game, with every part of the pack on at once as well; the foes part; Uloe; kit 1.7; the sea
  > chart rebuilt on the base game's own art. 2026-11-03: the balance simulator re-run on the round's numbers
  > (`docs/reports/BALANCE_RERUN_2026-11-03.md`, cloud only, no number changed): his own wall decides Spool's picks (Log).
  > 2026-11-04: the missing upgrades of Yossele and Spool, against the base vessels' trees (`docs/reports/UPGRADE_GAPS_2026-11-04.md`,
  > cloud only, no mod or scope changed): Yossele needs one (Wood Wide Web, a trade); Spool needs 22, five of them fill the
  > studies' holes; Dimitri accepted them all and they are in scopes `07` and `08`, not built (Decisions).
  > 2026-11-04: at Dimitri's ask (his Yossele-and-Spool limit lifted for it), 21 new Directions and 13 new Cardinals drawn
  > from StarCraft II's campaign upgrades at the stock Cardinals' power, all Claude `~defaults~` in scopes `11` and `12`;
  > every Direction and Cardinal now carries `By: Dimitri` or `By: Claude` (`docs/reports/DIRECTIONS_CARDINALS_2026-11-04.md`,
  > not built); the Scope Editor artifact (https://claude.ai/artifact/5KAGb5wdqiNV1bEoxNMgEo) shows and edits every scope file, makers
  > tagged, and exports the `.txt` (source in project files `concepts/scope_editor_2026-11-04/`; Log).
  > 2026-11-04: five concept sprites of Argo as a diver who might not be entirely human (Barnacled, Angler, Hollow,
  > Passenger, Pressure Hull), drawn over his shipped frame, idle and back each; Dimitri picks on
  > https://claude.ai/artifact/FnchhGQBHvi9JyhC47TCe2 (`docs/concepts/argo_divers_2026-11-04/`; not wired; Log).
  > 2026-11-04: the overlap check Dimitri asked for: 5 of those 13 Cardinals copied a stock one and are removed (Iron Rivets, Siren
  > Conch, Kelp Net, Lucky Geode, Storm Lantern); then 11 more Directions (Any, 2 Duos, Yossele, Spool) and 7 Cardinals after Risk of
  > Rain 2, Hades and other roguelikes, all Claude `~defaults~`, not built; the Scope Editor rebuilt on the same link
  > (`docs/reports/ROGUELIKE_DIRECTIONS_CARDINALS_2026-11-04.md`; Log). At his next word Claude's seven older Cardinals that copied
  > stock ones (Spiny Hull, Sea Legs, Rising Tide, Puffer Float, Tide Clock, Rusted Bell, Second Wind) are removed from scope `12`
  > and from RogueWavesMode's code (compiled here, not run on the PC).
  > 2026-11-04: he picked Hollow; round 2 draws it in five head and body shapes (Bell Jar, Fish Tank, Slump, Fishbowl,
  > Hunch) on the same page (`docs/concepts/argo_hollow_2026-11-04/`; not wired; Log).
  > 2026-11-04: round 2 did not appeal; round 3 draws ten new Hollow bodies on round 1's helmet (Big Head, Long Legs, Lead
  > Boots, Waterlogged, Broad, Air Tank, Stout, Lopsided, Bell Skirt, Long Arms), top of the same page
  > (`docs/concepts/argo_hollow_ten_2026-11-04/`; not wired; Log).
  > 2026-11-04 05:31Z, Dimitri: "Keep the original for now." Argo keeps his shipped look; the three concept rounds stay on file.
  > 2026-11-04: a balance and skill review of Argo (his Yossele-and-Spool limit lifted for it; cloud only, nothing built):
  > close to balanced once its slows match its sheet; fourteen changes in scope `09`, each marked with what it was, and two
  > code fixes for the next Argo build (`docs/reports/ARGO_REVIEW_2026-11-04.md`; Open questions 8; Log).
  > 2026-11-04: Uloe's upgrades fleshed out (his Yossele-and-Spool limit lifted for it; cloud only, nothing built): 8 to 32,
  > every building 8 with a capstone, all Claude `~defaults~` in scope `10` beside his eight lines; hand numbers put a lit
  > Uloe level with Smacky (`docs/reports/ULOE_UPGRADES_2026-11-04.md`; Open questions 9; Log).
  > 2026-11-04: **every feature that had not been tested was run, the mod's online half with a real second player** (two copies of the game on the PC over the loopback address; `docs/reports/TEST_ROUND_2026-11-04.md`): votes, shop, Fund Transfer, weather, the drop and rejoin, Continue with a friend, a friend without the pack, four vessel pairs on a client, the F key on a real death, Direction effects on real hits, UI scales, the 45 maps, ten maps to the win. Ten bugs fixed, among them a rejoin that left a friend on a map with no looks for good and a Spring Tide that made hits on a tower infinite. Vessel self-tests: Spool 133, Argo 142, Yossele 173, Uloe 148, no failures. The whole mod boots as one pack (`pack.txt` itself stays Yossele alone). Compiled and run on the PC.
  > 2026-11-04: **new sprites for every modded minion**, ten Snoble-sized versions each (Dimitri: "Create new sprites for all
  > modded minions. They should be closer in size to snobles. Create at least 10 versions of each."): Cordyfriend, Thread Head,
  > Stitched, Dart, Seam, Mangled, Photon, Naut (in all three crew colours); the foes part adds no bodies. Files only, nothing
  > wired until he picks on the Minion Picks page https://claude.ai/artifact/Rj9xQXsYUnauTGQyiFfAJX (`docs/TEMP_ART.md` "Minion versions"; Log).
- **2026-11-04: code simplification report** (Dimitri: "Don't make any changes"; cloud only, nothing changed;
  `docs/reports/CODE_SIMPLIFICATION_2026-11-04.md`): the mod's code read against the base game's for what could be simpler,
  ten findings ranked. The three big ones: armour and "takes more" as the game's own `Resistadd` stat with the stock
  `Fiewall_Regen` script, instead of five hand-kept hit hooks; the stock `Entity_Ai_General_Upgrade` instead of four mirrors
  of it, which also makes Voice in a Bottle work on pack buildings; and the four vessels' shared scaffold (about 2,500 lines)
  moved into `Shared`. Read only, so it needs a PC check: a tower holding Spring Tide next to a Fie wall that spreads its
  resistance may take half damage today (R3). Each step waits for his word; the suggested order is the report's last section.
- **2026-11-04: Trinkets and Cardinals** (Dimitri: "Rename Vessel Cardinals as Trinkets and Team Cardinals as just
  Cardinals, then fix merge conflicts."). Scope `00`, `01`, `04`-`06`, `11`, `12` (now "New Cardinals and Trinkets",
  its list split into (Trinkets) and (Cardinals)) and `15` (the template is "Cardinal or Trinket"); `02` logs it. The
  shop card's kind line reads Trinket or Cardinal, the SHOP button and VENDCINT's line name both; the code's names follow
  (`RWPlayer.Trinkets`, `RWRun.Cardinals`, `IsTrinket`) with the save key and ids kept. Kit `17` and pitfall #118, the
  style sheet, `TEMP_ART.md`, `PACK.md`, `EXPANSION_SCOPE.md`, `docs/mode/`, the mode's README and notes; the nine
  reports that used the old names carry a note on top. Compiled here, not run on the PC. The same PR folds in #74 and
  #75, which both conflicted with the base once #76 merged.
- **2026-11-04: kit and resources check** (Dimitri: "Read through TDS_Vessel_Creator to make sure everything is updated,
  then review the resources to make sure everything checks out."; cloud only, `docs/reports/KIT_CHECK_2026-11-04.md`).
  Two passes over every kit page and the parts' pages: 29 stale lines in the kit (the pack's vessels, minions and foes
  part now cited; the two-player round, the stock `Resistadd` route, re-ticked slows in #79, the 2026-11-04 results in
  pitfalls #93-#108 and chapters 15, 16, 19, 23; #122 amended by #142) and 6 in the resources (Spool's and Uloe's
  status, the mode's README and temporary-art table, a stale `TEMP_ART.md` row, scope `02` [Now]). Pre-scope mentions
  kept by the #78 rule. The project-files copy of scope `02` and the Scope Editor wait for his word to sync. Every part
  compiles clean; nothing run on the PC.
- **2026-11-04: code simplification built** (Dimitri: "Begin development of Code Simplification, then add the tests to
  the testing thread."; cloud, compiled, not run on the PC; `docs/reports/SIMPLIFICATION_BUILD_2026-11-04.md`). Uloe's
  dead stock-mark route and Argo's 1.4 leftovers deleted (SONAR kept); pack buildings carry the game's own
  `Entity_Ai_General_Upgrade` again instead of four mirrors, so Voice in a Bottle should now work on them; kit 1.8 puts
  the helpers and the asset reader every vessel copied into `Shared/ModKitUtil.cs` and `Shared/ModKitAssets.cs`, and the
  five hit-chain copies into `ModKitUtil.Front`. Every part compiles at 0 errors. Not built: finding 1 (four places where
  it changes numbers, report §2), the per-vessel content builders (they need a shared data type), findings 4-6, 9, 10.
  Kit docs `01`, `05`, `09` #9 and `17` §2.7 describe the stock upgrade AI. The PC list (T0-L1, with R3 on today's
  build) went to the testing thread.
- **2026-11-04: code review against the engine** (Dimitri: "Start with 1, 2, and 3" on the coordinator's list of
  quality checks; paused early at "Once every thread is at a good stopping point, pause activity"; cloud only, nothing
  run on the PC; `docs/reports/CODE_REVIEW_2026-11-04.md`, every finding in `_FINDINGS.md`). Nine reviewers read every
  part's shipping code against the kit's rules and the decompiled game: 55 findings, 54 bugs, 32 confirmed and 22
  plausible, none seen in game yet. The worst: every bonus break from Tide 29 pays twice, an objective left running
  pays at the next map's start, a question asked while a vessel is down never reaches its player, and a quit after
  the staging map brings it back. None is fixed: the report sorts the fixes into a low-risk batch, the ones that need
  Dimitri's word (Uloe's shared Light, Spring Tide and Tidal Mend, the map's foe health look, Monument's Cleanse, lobby
  Cardinals) and the ones a PC fact decides. Not done: the second pass over the mode and the shared kit, and most
  debug files. The same PR repairs the base: PR #82's commit came from the PC's copy, older than PR #80, and put back
  old helper names, so the mode's release build failed with 6 errors and three vessels' self-tests failed too. Seven
  files three-way merged; every part compiles at 0 errors again. A change made on the PC goes back as a diff on the
  newest base (`TDS_Mod/Tools/pc/README.md`).
- **2026-11-04: scope against code** (Dimitri: "Are there any token intense quality checks that you would recommend on
  this project?", then "Start with 1, 2, and 3"; this is item 2, cloud only, `docs/reports/SCOPE_CODE_AUDIT_2026-11-04.md`).
  2,367 scope lines read against the code at `5d9b09e` (each row re-read after #80 and #82), a second pass on every file with more than three issues: 41
  mismatches (22 on `~defaults~`), none in 00-03, 05, 06. First to decide: no map vote after a bonus-Tide loss (04:58),
  the lobby Cardinal picker still free between maps (04:61), Trinkets and Cardinals past the eighth shown nowhere, stock
  Cardinals bought once but still offered, the Shift Directions not renaming their building. Scope 16 matches the game's
  data; 44 of its lines need the decompiled game. No scope file or code changed; nothing run on the PC.
- **2026-11-04: the repository presents only Rogue Waves** (PR #78; Dimitri: remove mentions of the mods before the
  scope, and make the README summarise Rogue Waves; Log entry added 2026-11-05, it was in "Now" only). The README,
  `CLAUDE.md`, the maps, rules, tool notes and the pack parts' comments drop `ModProjects` and the earlier vessels; the
  kit, `Shared`, dated reports, this Log, the scope, the Riptide and RogueLab folders, the proof citations and every
  path a script uses keep them (the PR body lists what stayed and why). Compiled here; nothing run on the PC.
- **2026-11-04: held Cardinals on later maps** (PR #82, from the testing thread's PC round; Log entry added 2026-11-05).
  A map counts as loaded before its Basin stands, so the team's Cardinals found no core on every map after the one they
  were bought on and did nothing; they now wait for the core (`17_Cardinals.cs`, pitfall #150), seen working in game. The
  same commit added an old-save check for the Trinket rename, a later-map check in the flow, a Nesting Ground drop,
  end-screen rows, a big fight on a client, and fixes to Spool's, Argo's and Uloe's checks. The round stopped at the
  usage limit before PR #80's test list (`docs/reports/SIMPLIFICATION_BUILD_2026-11-04.md` §4) and wrote no report. The
  commit was made from the PC's copy, older than PR #80, and broke the cloud build until PR #84.
- **2026-11-04: one quality pass over the whole repository** (PR #83, item 3 of "Start with 1, 2, and 3", paused at
  his word after one pass; Log entry added 2026-11-05; `docs/reports/QUALITY_PASSES_2026-11-04.md`). 127 issues, 120
  fixed in docs and comments, mostly "not built / not run / online left" lines; 7 left for him, the first that the
  build caps a pack Cardinal or Trinket at 6 working copies and a stock Cardinal at 1 where scope `12` says no limit.
- **2026-11-05: the project set up to Claude Code's best practices** (PR #85; Log entry added 2026-11-05). `CLAUDE.md`
  rewritten as the checks to run, which skill to use when and the rules the code doesn't show, build state left to this
  Part; `.claude/settings.json` denies merging and pushing to the base branch; effort per thread in Part A; new project
  instructions in `docs/PROJECT_SETUP.md` §2.
- **2026-11-05: quality checks 4-7** (Dimitri: "81 is merged, resume with new project instructions and plugins in
  mind"; the coordinator's list's last four; cloud only, `docs/reports/QUALITY_CHECKS_4-7_2026-11-05.md`). One pass each,
  then a re-check of every fix. **4:** `RogueWavesMode/Code/98_SaveFuzz.cs` puts seeded random and damaged values
  through the campaign save (F1-F7, in the mode's self-test, every key put back) and `Tools/cloud-check/save_fuzz.py`
  runs it here on the mode's own methods and a stand-in save: on the base it failed three checks and hung on a fourth
  case; three guards fix them (`RWCampaign.Saved` cuts a route that leaves the chart back onto it, `RWRun` reads at most
  64 saved players and drops empty ids and negative Pure Aura), and it passes 7/7. **5:** the Scope Editor holds no
  edits and equals his project-files copy; both are behind the repository in 7 files, Claude's lines only; one note
  (scope `12`, `17_Cardinals.cs`) claimed a Cardinal count in the run line that the code doesn't show. **6:** every
  sprite listed, present, the right size, and none private or from before the scope. **7:** this Log lacked #78, #82,
  #83 and #85, and "Now" the code review's open bugs and #82's unfinished round. Compiled here (release, self-test,
  gaps: 0 errors); nothing run on the PC.
- **2026-11-05: Dimitri's open decisions on one page** (Dimitri: "Check the idle threads to see if there is anything
  that can be continued or combined into a single thread instead"). Nearly every idle thread waits on his word, so
  `docs/reports/OPEN_DECISIONS_2026-11-05.md` gathers the calls from the code review (§5 B), the scope audit, the quality
  pass, the kit check, the mode polish, the balance reports, testing round 3 and this Part's Open questions: 39
  questions, each with Claude's default, and an appendix of the audit's smaller rows. Docs only.
- **2026-11-05: his answers applied** (same thread). Scope 04 and 09-14's defaults taken, the text answers written into
  02, 04, 06-15 and Spool's `DESIGN.md`, the answered [Questions] removed; the hand-off for the build thread, art and the
  balance sweep in `docs/reports/DECISIONS_APPLIED_2026-11-05.md` §3. Docs only.

- **2026-11-04 to 2026-11-05: the "Now" entries of those days** (moved here verbatim from "Now" on 2026-11-05 to keep
  "Now" short; some repeat entries above). The old "Now" read:
  - **Done (state on 2026-11-04):** Phases 1-2 and Phase 3 Argo. **Built and run on the PC:** Argo 2.0.1, Yossele 0.2.0-dev
  (Dimitri's play build), Spool 0.1.0-dev and Uloe 0.1.0-dev, all with temporary art (`docs/TEMP_ART.md`, his 35 picks);
  the mode `RogueWavesMode` 0.1.0-dev (the whole campaign, from the main-menu button to the end screen); the foes part;
  kit 1.7. The 2026-11-03 build round passed 139/0 with every part on (`docs/reports/BUILD_2026-11-03.md`); the
  2026-11-04 test round ran the online half with a real second player, ten maps to the win and every vessel's self-test
  with no failure, and the six parts boot as one pack (`docs/reports/TEST_ROUND_2026-11-04.md`). **Designed, not built**
  (all Claude `~defaults~` until Dimitri takes them): Yossele's and Spool's missing upgrades (scopes `07`, `08`), Argo's
  review changes (`09`), Uloe's upgrades 8 to 32 (`10`), the new Directions and Claude's 11 Trinkets and 4 Cardinals (`11`, `12`);
  since 2026-11-05 he has taken the defaults and all of these are built but Spool's (parts 1-12 below).
  Argo keeps his shipped look (2026-11-04 05:31Z). Every dated entry, newest last, is in "Log"; the reports are indexed in
  `docs/reports/README.md`. 2026-11-04: ten Snoble-sized versions of every modded minion; his picks wired 2026-11-05 (below). 2026-11-04: every frame timed in real play, nothing of the mod's to fix (`docs/reports/SMOOTHNESS_2026-11-04.md`).
  2026-11-04: the repository checked and tidied (`docs/reports/REPO_QUALITY_CHECK_2026-11-04.md`). 2026-11-04: the mod's
  code read against the base game's for what could be simpler: ten findings, nothing changed, each waits for his word
  (`docs/reports/CODE_SIMPLIFICATION_2026-11-04.md`). 2026-11-04: Vessel Cardinals are now **Trinkets** and Team Cardinals
  just **Cardinals**, in the scope, the docs and the mode's shop (Decisions; compiled, not run on the PC). 2026-11-04: the
  repository no longer presents the mods from before the scope: the README now summarises Rogue Waves, and the maps, rules,
  tool notes and the pack parts' comments drop `ModProjects` and the earlier vessels; the kit, dated reports, this Log, the
  scope and every path a script uses keep them (PR body lists what stayed and why). 2026-11-04: the kit and the
  resources read against the repository: 35 stale lines fixed, no code changed (`docs/reports/KIT_CHECK_2026-11-04.md`). 2026-11-04: the code simplification
  built at his word (findings 7, 8 without SONAR, 2 and 3; kit 1.8 with `ModKitUtil` and `ModKitAssets`; about 2,200 lines
  gone, no number changed); finding 1 not built, it would change numbers (Decisions). Compiled, not run on the PC: the
  testing thread runs `docs/reports/SIMPLIFICATION_BUILD_2026-11-04.md` §4. 2026-11-05: `CLAUDE.md` rewritten to the
  Claude Code best-practices page: the checks to run, which skill to use when, the rules the code doesn't show; build
  state left out of it, since this Part holds it. `.claude/settings.json` now denies merging and pushing to the base
  branch; effort per thread is in Part A; new project instructions in `docs/PROJECT_SETUP.md` §2.
  2026-11-04: one quality pass over the whole repository, paused at his word before a second: 127 issues, 120 fixed
  in docs and comments (mostly "not built / not run / online left" lines), 7 for him
  (`docs/reports/QUALITY_PASSES_2026-11-04.md`); the cloud compile break it found after PR #82 is repaired by PR #84.
  2026-11-04: every line of the scope read against the code: 41 mismatches, 22 of them on Claude's defaults, each with
  who should yield; nothing changed, each waits for his word (`docs/reports/SCOPE_CODE_AUDIT_2026-11-04.md` §2).
  2026-11-05: quality checks 4-7 (`docs/reports/QUALITY_CHECKS_4-7_2026-11-05.md`): save and load fuzz tests, seven
  checks as self-test code (`RogueWavesMode/Code/98_SaveFuzz.cs`, the mode's self-test should read 155) and run here on a
  stand-in save (`Tools/cloud-check/save_fuzz.py`, 7/7); three damaged-save bugs guarded (a route off the chart stranded
  the boat after Continue, a huge player count froze it, empty ids loaded); compiled, not run on the PC. The Scope
  Editor holds no edits and, like his project-files copy, is behind the repository in 7 files (his word to sync); art
  needed nothing; four merged PRs got their missing Log entries. Still open from 2026-11-04, found missing here: the
  code review's 54 bugs are unfixed (`docs/reports/CODE_REVIEW_2026-11-04.md` §5 batches, §6 PC list); PR #82's test
  round stopped before #80's test list (`docs/reports/SIMPLIFICATION_BUILD_2026-11-04.md` §4), and the PC's `TDS_Mod` is
  older than PR #80, so the next PC round brings it up to the base first. Then, at his word: his project-files scope
  copy (all 17 files) and the Scope Editor (version 7) synced to the repository; his PC's copy waits for a PC round. The
  chart's maps keep following the base game's map list (Decisions 2026-11-05).
  2026-11-05: the code review's bugs fixed, 32 of 54 (every confirmed one that needs neither his word nor a PC fact,
  and six plain guards), with kit 1.9 and pitfalls #151-#155; the map's extra foe health kept without the Hardened
  look. Compiled, not run on the PC: the next PC round runs `docs/reports/CODE_REVIEW_FIXES_2026-11-05.md` §5.
  2026-11-05: PC testing round 3, stopped early at the usage limit: on PR #89's base every self-test passes and the fixes read hold in game; the two-player list did not run (`docs/reports/TESTING_ROUND_3_2026-11-05.md`).
  2026-11-05: the 54 bugs checked again on that base with round 3's readings (`docs/reports/CODE_REVIEW_RECHECK_2026-11-05.md`):
  18 fixed and gone in game, 14 fixed but not yet seen (the two-player ones, D8, A2, D10, ARG-3 and four guards), 20 still
  there for his word, a PC fact or a sighting, MODE-C12 and ARG-1 (Dagger, Spear) not bugs; round 3's open readings explained.
  2026-11-05: Spool is sage green, not purple, at his word (the purple read too close to the foes' magenta; a yellow
  and a mint tried first, `ModSource/Spool/Art/Temporary/pastel_green_2026-11-05/`): its frames (`SpoolCharacter.P`), the
  UI colour, the Thread Head and Crafted sheets, the thread and the models' felt (`concept-tools/temp_art/spool_sage.py`).
  Compiled here (release and self-test 0 errors, editor clean), not run on the PC: the next Spool round looks at it
  beside a Hardened foe and on grass.
  2026-11-05: Claude now merges PRs itself, at his word (Part A "Merging"; `CLAUDE.md` and `docs/PROJECT_SETUP.md` §2
  follow). #91, #92 and #90 merged through the GitHub MCP `merge_pull_request` tool, each after the base was merged in
  and its cloud-check passed. `.claude/settings.json` still lists the merge tools under "deny" (Claude may not edit
  its own permissions; the deny did not stop the MCP tool), and his project instructions still say "only Dimitri
  merges" until the new §2 text is pasted there.
  2026-11-05: every call waiting on Dimitri gathered on one page, 39 questions by theme, each with Claude's default
  (`docs/reports/OPEN_DECISIONS_2026-11-05.md`); answered in its thread, small answers go straight into a PR.
  2026-11-05: his answers applied (`docs/reports/DECISIONS_APPLIED_2026-11-05.md`): the scope's defaults in 04 and 09-14
  taken (1,192 tildes removed) and the text answers written; what needs code or art is handed to the queued build
  thread (B1-B15), art (A1-A4) and the balance sweep (S1-S2). Docs only. His project-files scope copy and the Scope
  Editor are behind again in 12 files (his word to sync).
  2026-11-05: synced at his word (project-files scope copy, Scope Editor version 8); Open question 2, Spool's
  defaults, spelled out as S1-S17 for his answer (`docs/reports/SPOOL_DEFAULTS_2026-11-05.md`).
  2026-11-05: Spool's defaults answered ("Use all defaults"): scope 08's defaults taken except the four numbers the
  balance sweep owns; hand-off B16-B19 in `docs/reports/DECISIONS_APPLIED_2026-11-05.md` §4.
  2026-11-05: the scope read against the code a second time (`docs/reports/SCOPE_CODE_AUDIT_2026-11-05.md`): 369
  checkable rows, 269 MATCH, 93 on the queued lists, 7 unbuilt by design, no new behaviour mismatch; of the first audit's
  41 rows 18 are resolved and 23 queued. Ten new items: N1-N4 fixed in the scope text (Spool sage green, the picked
  Cordyfriend, self-test counts, 16's tildes and 15's weather line), N5-N10 for the build and balance threads (B5
  half built, 47 Direction/Trinket/Cardinal sheets, pairs, B12 is code, the sweep's tilde lines, Leads to). New tool
  `TDS_Mod/Tools/cloud-check/scope_upgrades.py`. Docs only. His project-files scope copy and the Scope Editor are
  behind in six files (02, 07, 08, 09, 15, 16; his word to sync).
  2026-11-05: the jittering button icons fixed (pitfall #156, kit 1.10): every pack icon the game drew stretched (the
  menu's 32 px crest in Play's 48-unit box, the 12 px command icons and card pictures in 16-unit boxes) now draws a whole
  number of screen pixels per art pixel: command icons are 16 px boxes (all four vessels and the mode's SHOP, FUND
  TRANSFER, OBJECTIVE, weathers), the crest is sized to 32 units, and every card's picture is crisped where the dialog is
  placed; card art uses the 12 px crest and 24 px numbers; the
  sea chart's weather marks keep their size. Compiled here, not yet seen in game: his eye on the menu, a
  dialog sliding in, and the HUD's buttons.
  2026-11-05: the code review at max, at his word (`docs/reports/CODE_REVIEW_MAX_2026-11-05.md`): every part read
  against the engine again by a first pass and seven reviewers, 58 findings none of which were known; 35 fixed (the
  F-key fix that never armed, kit 1.11 and pitfall #157; a skipped staging map replayed forever; campaign Cardinals left
  free in the lobby; Voice in a Bottle reaching the Bouthouse; 16 self-test checks that couldn't fail), 23 left: three
  for his word (§5.1), the rest for a PC fact. Pitfall #158 (first-frame looks lost). Compiled here (every part, release
  and test builds, 0 errors; editors clean), not run on the PC: the next round runs its §6.
  2026-11-05: the mode's balance reviewed against the base game at his word ("with stacking cardinals and directions,
  the difficulty should be higher than base game"; `docs/reports/MODE_BALANCE_REVIEW_2026-11-05.md`): as built every
  map asks less than a Downpour run and the gap widens (carried power about x3.5 by map 10, foe pressure x1.5); the
  proposals F1-F5, X1, C1 and P1 wait for his word (scope `04` and `06` numbers are his); the balance simulator sweep
  thread starts from set S2 with `docs/reports/mode_balance_2026-11-05/`. Simulated here, no code changed.
  2026-11-05: PC testing round 3 finished at his "Continue": every fix read in game holds, online too (D4, D7, B1-B4, D8, A2, D10,
  ARG-3); kit 1.9's self-tests pass. Live and unfixed: Spring Tide by a Fie wall halves hits (R3, C3), the part takes a sub-core for
  the Basin on Artrio (A3). New: a Crafted with no TARGET stands inside its Tin (`docs/reports/TESTING_ROUND_3_2026-11-05.md` §7).
  The PC holds this base, with release builds on.
  2026-11-05: the build of the unbuilt candidates, part 1 (the decisions' code items, `DECISIONS_APPLIED_2026-11-05.md`
  §3-§4, and the max review's three calls at their defaults): B1 the map vote held when the party first stays, and the
  host's ask on the boat when none was held; B2 the lobby's Cardinals locked to the campaign's (MODE-D9, MODEB-4
  closed); B3-B4 the shelf's caps and Trinket slot, one roll a break, Trinkets on the vessel's panel, "+N Cardinals" on the run line; B5
  Dazzling at 15% with Illuminate's 3 s linger; B6 the pack's foe modifiers are elite for Spool's Thread; B7 Spring Tide
  and Tidal Mend on the mode's own effects (pitfall #159's fix); B9 Monument's Cleanse leaves a building's own
  trade-offs; B12 Salt Sting's line; B13 Argo's slows renew, bosses never held; B15 Tougher for Boutbro (MODEB-8), the
  Compost Bin III line, the meteor's 12 + 3 x map hits; B16 no stock MOVE on the Thread Head; the Stitched's click no
  longer goes to its TARGET pin (pitfall #162); MODEA-3 the host keeps the boat's lead; YOS-3 fruit for players only;
  MODE-A3 the Basin is the main core on Artrio; MODEB-7 no VENDCINT mid-Tide. Pitfalls #162-#163. Compiled here (every
  part, release and test builds, 0 errors; editors clean), not run on the PC; the PC list is in the PR.
  2026-11-05, part 2: Argo's review of 2026-11-04 built (scope `09`, `ARGO_REVIEW_2026-11-04.md` §5, all fourteen):
  Kelp Bed and Trench pin, the Scatter's +100% on a pinned foe, Wide Bore, Double Barrel's three, Abyss, Trench, Gusher a
  Fathom, Mantle Tap's speed and 20 s quake, Yellow prices for the Drill's control path, the Base Blue 20 + Yellow 15.
  Compiled here (release, self-test and review builds, 0 errors), not run on the PC.
  2026-11-05, part 3: Uloe's 24 upgrades built (scope `10`, `ULOE_UPGRADES_2026-11-04.md` §3 and §6), eight a building
  with every Leads to (N10); a building takes at most 4 Light from teammates (B10, decision 18), and AIM, the default aim
  and Photon visits keep to the owner's buildings (ULO-3); Supernova's SURGE once a Tide and Colour Filter's COLOUR are
  one-click buttons. Self-test section 8b. Compiled here (release, self-test and review builds, 0 errors), not run on
  the PC.
  2026-11-05, part 4: Yossele's Wood Wide Web built (scope `07`, `UPGRADE_GAPS_2026-11-04.md` Y1), and the Shift
  Directions rename the building's panel (B8, decision 15): Dandelion Sapling, Primordial Mushboom, Sewing Machine, by a
  skin whose entry's Displayname is the panel's title (pitfall #164); the models wait for A1. Compiled here (release, self-test and
  review builds, 0 errors), not run on the PC.
  2026-11-05, part 5: Argo's and Yossele's new Directions built (scope `11`, B11): Chum Bucket and Pressure Gauge for Argo;
  Cutting Jar, Sap Bucket, Creeping Ivy, Deep Breath and Fairy Ring for Yossele, each with the sheet's numbers and its
  self-test lines (Argo after Deep Current, Yossele section 12c). Deep Breath's 2 s rest counts from the laser's reload,
  since Yossele fires every 2 s. Compiled here (release, self-test and review builds, 0 errors), not run on the PC.
  2026-11-05, part 6: Spool's new Directions built (scope `11`, B11): Double Needle, Button Jar, Pattern Book, Merit
  Badge and Odd Socks, with the sheet's numbers and self-test section 10c; Button Jar's smaller look waits for the art.
  Compiled here (release, self-test and review builds, 0 errors), not run on the PC.
  2026-11-05, part 7: the Directions any vessel can take built (scope `11` (Any), B11, audit N7): Wound Spring, Fine
  Sieve, Spare Hands, Galley Kettle, Chain Shot, Salt Barrel, Fog Horn, Clam Shell, Notched Oar, Lamp Oil, Fish Hook,
  Spare Compass (with its once-a-map redraw), Magpie Nest, and the Duos Fire Ship and Tide Pool, offered only to a player
  holding both; Wound Spring and Spring Tide are a pair. In `RogueWavesMode/Code/22_AnyDirections.cs`, self-test part
  "any" of the gaps build. Compiled here (release and test builds, 0 errors), not run on the PC.
  2026-11-05, part 8: the base vessels' Directions no held stat carries built (scope `11`): Mason's Trowel, Snow Shovel,
  Worm Tin, Open Sign, Long Leash, in `RogueWavesMode/Code/23_BaseDirections.cs`, self-test part "base" of the gaps
  build. Compiled here, not run on the PC.
  2026-11-05, part 9: Claude's Trinkets and Cardinals built (scope `12`, B11): Brass Spyglass, Gutting Knife, Oil Can,
  Tar Bucket, Rubber Duck, Spare Plank, Punch Card and Pearl Oyster (a pair), Bottled Storm, Oilskin Coat, Gull
  Feather; the Cardinals Cork Lifebuoy, Signal Flare, Cargo Crate and Tin Whistle. In
  `RogueWavesMode/Code/24_Trinkets.cs`, self-test part "trinkets" of the gaps build; their 15 icons wait for the art.
  Compiled here, not run on the PC.
  2026-11-05, part 10: the 47 icons of the Directions, Trinkets and Cardinals built in parts 5-9 (A2), temporary, 12 x 12
  from their Look lines (`concept-tools/temp_art/b11_icons.py`), and the art review in `docs/STYLE.md` with its
  check script (`concept-tools/temp_art/art_check.py`: 0 problems on all 47). Not yet seen in game.
  2026-11-05, part 11: the models and the rest of the missing art (A1, A3, A4), temporary: the three Shift looks
  (`Yossele_Dandelionsapling`, `Yossele_Mushboom`, `Spool_Sewingmachine`), a look for each of Uloe's 24 new upgrades,
  Wood Wide Web's, Button Jar's small Stitched and Dart (a `Small` skin), and the five objectives' icons on the Board's
  card and the DONE popup (`concept-tools/temp_art/objective_icons.py`). The meteor node keeps a stock ore's look: a
  white crystal needs a model the mode part doesn't carry, and its boot log now names what that look is made of.
  Compiled and rendered here, not run on the PC.
  2026-11-05, part 12: `/simplify` on parts 1-11, no number or behaviour changed: kit 1.12 (`ModKitUtil.SlowOn`,
  `Auras`/`Purse`/`Gain`/`Give`, `ModKitCombat.SetMulti`, `Keep` on Health), the mode's one tower list a frame, shared
  rest-then-mend, kept Direction parses, and the vessel parts' single records. Also seven behaviour questions for the next
  code review, the leftovers, his readings and the next PC round's list: `docs/reports/BUILD_UNBUILT_2026-11-05.md`
  (the thread's closing report). Compiled here (every part, every test switch, 0 errors; the two-PC build fails here as
  always), not run on the PC.
  2026-11-05: the text audit, Dimitri's queued order (`docs/reports/TEXT_AUDIT_2026-11-05.md`): every player-facing
  string and the living docs read for spelling, grammar, the names, stale facts and the clean rule. Seven game lines
  fixed, five with their scope lines (Rising Water's Tide, Dazzling's Illuminates, Vampire Dentures' grammar, "per hit",
  "farther", Spool's mend line, Argo's Mods-menu text); about 40 scope and doc lines (the F-key "pass" of 2026-11-04 ran
  with the fix unarmed: scope `03`, here, `RESEARCH_GAPS.md`, kit `docs/15`; stale "not built" and "passes in game"
  lines; typos). Seven calls for him in its §4 (the meteor's line, Long Line named twice, the leave vote's line, Depth's
  unlock, numbers in Notched Oar, British or American spelling, the weather's pay wording for the sweep). No number
  changed. Compiled here (Argo, Spool, the mode and the foes, every test switch, 0 errors; editors clean), not run on
  the PC. His project-files scope copy and the Scope Editor are behind in 00, 02-09 and 11-13 too (his word to sync).
  2026-11-05: every call waiting on Dimitri after PR #115 on one page, at his word (`docs/reports/OPEN_DECISIONS_2026-11-05b.md`):
  14 numbered items with Claude's recommendation and what each blocks (the build's 15 readings and 7 behaviour
  questions, Yossele's ~90 scope `07` defaults, the last defaults and built `! NEEDS !` lines, five wording calls, the
  meteor's crystal, Yossele's final sheet, when to run the next PC round, the sync, `.claude/settings.json`). Spring
  Tide by a Fie wall is not a call: B7 in PR #103 rebuilt it, and the PC still has to see it. Docs only, no compile.
  2026-11-05 16:04Z, his answer: "Default for all, and I give permission for 12". Applied here: Yossele's 95 scope `07`
  defaults, 03's names, 06's rules, 10's sharing limit and 12's shop list are his; the NEEDS lines in 11, 12 and 14
  cleared; Depth's notes in 04 and 05. Handed on (the report's end): items 2, 5, 6 and 9 to a code thread, 10 and 11 to
  art, the next PC round (his OK) right after the balance sweep, then the sync and 04's NEEDS line from this thread;
  `.claude/settings.json` is his edit. Docs only, no compile.
  2026-11-05: the weather test and a map's lighting, at his ask ("Test the weather effect. Are we able to drastically
  alter the lighting of levels to include features like fog?"; `docs/reports/LIGHTING_2026-11-05.md`): the weather's
  code is as it passed on the PC (156/0 on `ecd1991`), so its test is the mode's self-test again on the current base,
  plus his eye on a flooded map. Lighting: yes; fog, the ambient colours, the sun and any of the 55 stock grades can
  change a running map on every machine (kit 1.13 `ModKitLight`, kit `docs/16` §7.1, pitfall #167); a grade of our own
  needs a Unity-built profile, and drifting fog banks would be new art. The gaps build's `light` part shows each look in
  pictures. Compiled here (every part, every test switch, 0 errors); run on the PC since (the entry below); whether a
  look ships is §5.
  2026-11-05: the art of decisions 10 and 11 (`OPEN_DECISIONS_2026-11-05b.md`, his "default for all"). The mode part has
  a model library (`Assets/RogueWavesModeMod/Editor/RogueWavesModeModels.cs` into `RWAssets_Library/Models`, lit with a
  copy of the game's "General Tower" material by `RWModels`, `Code/25_Models.cs`), and Mind the Meteor's node wears its
  first model, a temporary white crystal on a scorched fallen stone (`RW_Meteor`; the line stays; the gaps self-test
  checks it). Yossele's final sheets drawn by Claude from his reshade and the temporary sheets, for him to edit
  (`ModSource/Yossele/Art/README.md`, `.aseprite` in `Art/aseprite/`): idle and walk, attack, Fertilize, fall, landing;
  the left-heading facings were plain mirrors with the shade on the left, now on the right like his reshade. Scope
  `07`'s sprite NEEDS line stays until he takes them. Compiled here (the mode's release, self-test and gaps builds, 0
  errors; editors clean), `art_check.py` 0 problems, model rendered; not built in Unity, not run on the PC: the next
  round looks at the meteor on a map (size, the hit flash, the boot line) and Yossele walking left.
  2026-11-05: the code items of his "default for all" on the second decisions page
  (`docs/reports/DECIDED_CODE_FIXES_2026-11-05.md`): kit 1.14 (`Front` keeps two parts' hit handlers in a steady order,
  pitfall #168), Uloe's Searchlight slow keeps the 10% floor, Argo's Cleave is no longer his own hit for Notched Oar, Fish
  Hook and Gutting Knife, Tar Bucket uses Mason's Trowel's hook, Blurry Photo copies are found by their run-time tags
  (pitfall #169), the Cardinals' paired lookups read the panel's ids, Boutman's Direction is Spare Reel, the leave vote's
  new line, "towards" in Argo's Reel. `HasMinions` was right (all nine vessels have minions). Compiled here (every part,
  release and self-test, 0 errors), not run on the PC: its §4 joins the next round's list.
  2026-11-05: the weather and lighting PC round (`Light_round1`, his "Run it now", code `fb208b8`, kit 1.13;
  `docs/reports/LIGHTING_2026-11-05.md` §6). The mode's self-test with every part on: 157 pass, 2 fail; every weather
  check passes (W0-W3, 12 of 12); the two fails are Spring Tide's Tide gain as B7 rebuilt it, run for the first time
  (`0 -> 0.0375` where 10 is asked), handed on for a thread of its own. The gaps build's `light` part: 23/0 and 16
  pictures: real depth fog on units and buildings alike (the UI stays sharp over it), a look darkens the ground but not
  the sky (night reads as dusk; only a borrowed grade changes the sky), lamps are drawn, nothing measurable per frame;
  the 55 stock groups' numbers in `docs/reports/lighting_2026-11-05/light_groups.txt`. Pitfall #170 (the HUD hides
  after 10 s without input). His release builds, save and settings are back as they were; Unity's project holds
  `fb208b8`. Whether a look ships (§5) goes to the open-decisions page with the pictures.
  2026-11-05: Spring Tide's Tide gain, the round's two fails, traced to the test's tower, not B7's effect
  (`TDS_Mod/ModSource/RogueWavesMode/IMPLEMENTATION_NOTES.md`, first entry; pitfall #171): the flow built its archer
  right after buying every stock Cardinal, so it holds Frail Titan and is Paper Tower's paper tower, and both readings fit
  a max that sums below 0 (read as 0) and a +10 Add scaled by 0.01; fitted, not yet confirmed. The flow now measures on
  a plain archer placed before the map's first Cardinal (the max and Spring Tide's own modifier +10 at Tide 1, and again
  at Tide 2, a new line) and logs the other archer's Health parts. No game behaviour changed. Compiled here (the mode's
  release, self-test, with gaps and with shots: 0 errors), not run on the PC. **PC round 4:** the mode's self-test with
  every part on shows the three `Spring Tide` lines passing, and the two `INFO Spring Tide: the archer built after the
  Cardinals` lines list Paper Tower's and Frail Titan's effects with a Multi near 0.01 (the cause confirmed).
  2026-11-05: the balance simulator sweep, Dimitri's queued order on his "Starting set S2, all proposals"
  (`docs/reports/BALANCE_SWEEP_2026-11-05.md`, runs in `balance_sweep_2026-11-05/`). The mode: map k's Tide 15 plays
  the base game's row 15, 18, 20, 21, 21, then 24 to map 10 (F1); foe health one Pmulti (F2) of x2.6 on map 1 (F3),
  +0.08 a map, +2% for each Cardinal, Trinket copy and Direction rank held (F5); modifiers from map 2 (F4); +50% foes
  a player (X1); prices +3 a held item (C1, shown right on every machine by a fourth None command, `RW_Price`);
  objectives from the break before Tide 3 (P1). Measured (16 seeds): D 0.8 on map 1, 1.0 on map 3, about 1.5 on maps
  8-10 (mid case), the review's target; Yossele follows Smacky, Spool on one Cushion can't hold the mode's maps until
  a Stitched blocks (its 22 upgrades), and Argo looks easier than its base game (a PC check). Spool on one Pin Cushion
  (answers 25, 26): Stitched 5, Dart 5.5, 6 Crafted a Tin, Range 3.5 kept, the Loom 0.3 a second, the Cushion's "Build
  it first" line (P4); Uloe's Flood Light 0.3 with it. Spring Tide by a Fie wall is code, not a number: B7's rebuild,
  whose weather-round fails were the test's tower (the entry above). The weather's pay line is fixed. Scope `04`,
  `06`, `08`, `10`, `12`, `14` carry the numbers with tildes. Compiled here (the mode, Spool, Uloe, every test switch
  but the two-PC and dialog-look builds, which fail here as always; editors clean), not run on the PC: its §8 joins PC
  round 4's list. Next: the scope sync and scope `04`'s tidy, then PC round 4, then Spool's 22 upgrades.
  2026-11-05: whether a lighting look ships (`docs/reports/OPEN_DECISIONS_2026-11-05c.md`, the lighting round's §5 call).
  Dimitri picked **a look per weather** (17:45Z): Rising Water under light mist, Quickening Current under storm grey,
  Nesting Ground at dusk, calm maps on their own light. Not built: a code thread of the mode builds it from the page's
  hand-off (a table and one `ModKitLight` call from the map's weather, a self-test check), then a PC round shows it
  with foes on screen. Fog Bank (heavy sea fog) and a mood ramp stay ideas. Docs only.
  2026-11-05: the scope sync after the sweep, his "sync" (item 13 of `docs/reports/OPEN_DECISIONS_2026-11-05b.md`)
  and item 4's default for scope `04`: the per-campaign NEEDS line is gone, and the modifier schedule that answers it
  (2 of the 4 new modifiers and 2 stock elites a campaign, from map 2, one more every second map) is his, without
  tildes. The sweep's tuned numbers in `04`, `08`, `10` and `12` keep their tildes, as the sweep's report §7-8
  leaves them, until he takes them (`06`'s and `14`'s lines were written as built). Scope `02` logs it. The Scope Editor and his project-files copy are refreshed
  from this base (his word asked in the thread before the copy is overwritten). Docs only, no compile; nothing ran on the PC.
  - **Next (2026-11-03):** Dimitri's eye and his answers: the chart, VENDCINT, Uloe and the campaign by hand (`docs/reports/BUILD_2026-11-03.md` §6), the `~defaults~`
  in scope `04`, `10`-`14` (taken 2026-11-05, like every Open question below); one online evening, which now covers the campaign's votes and shop too; the balance round's picks (Open questions 3-4 and 6, now the balance sweep's), after he tries a wall of his own on Purlest and Woozy (`docs/reports/BALANCE_RERUN_2026-11-03.md` §9). Release 1 below is unchanged.
  - **Online evening, since 2026-11-04:** only what loopback can't be is left: Steam's Join Game and lobby, a real network's delay and drop, a person's eye (`docs/reports/TEST_ROUND_2026-11-04.md` §5). Wherever "one online evening" appears below, read that.
  - **Before that:** the next PC round shows the picked temporary art in game (Yossele's attack and Cordyfriend, Spool's sheets,
  tuft and needle, Rising Water, the picked minions of 2026-11-05 with Uloe's Photon). **Release 1, Yossele alone** (Part B, Decisions 2026-10-20, answers 2026-10-21): ranked 2026-10-28 in `docs/reports/PRIORITIES_2026-10-28.md` (a PC round to install the default branch's Yossele, then his play test; his sprite sheet, not yet asked for in scope `07`; three balance calls from the play test; the `Pack` step; one online evening). Defaults taken 2026-10-28 and the pack step built: Decisions. Then his play test of
  0.2.0-dev (`docs/reports/YOSSELE_TEST_PLAN_2026-10-23.md` §3-5), his sprites and balance, the one-part pack build
  (its one version, `0.1.0`, replaces the dev builds' `0.1.0-dev`). Phase 5's pieces are built and passed in game
  ahead of Release 1 (kit 1.7, the main-menu button, the in-map vote and the mode's checks K0, CH, K1, P1, T0-T6, H1:
  `docs/reports/BUILD_2026-11-03.md`, "Phase 5 pieces" below); Dimitri confirms the polish's readings
  (`docs/reports/MODE_POLISH_2026-11-03.md` §2). By hand:
  the F key on a real Boutman death again (its 2026-11-04 pass ran with the fix unarmed; kit 1.11 arms it, `docs/reports/CODE_REVIEW_MAX_2026-11-05.md` P1-1) and a person's own press, the button's and the chart's look, a flooded map; one online evening (`docs/RESEARCH_GAPS.md` §E,
  kit `docs/19` §8); in `.claude/settings.json`, the two setup commands the docs ask for (C10). Left on Argo: his
  balance pass (with FLOOD's key, `GiveUpAfter` and Salt Sting's no-Brine number, B2-X1/X6, `MISTAKE_CHECK_2026-10-28.md`). Also left on Yossele: his look by eye (temporary
  art; the next PC round checks the Cordyfriend's own sheet, `IMPLEMENTATION_NOTES.md` 2026-10-27), his balance pass
  (the 2026-11-03 balance study proposes no change), and the Compost Bin numbers in scope `11` (the chance-based upgrades passed by their rates, `docs/reports/TEST_ROUND_2026-11-04.md`). Spool (beside Release 1, at Dimitri's ask): his play test and look by eye (the pickers, the temporary art;
  `TDS_Mod/ModSource/Spool/IMPLEMENTATION_NOTES.md` "PC rounds" lists what the self-test can't show), his answers to the
  sheet's open items and to the defaults in its `DESIGN.md`, then his sprites. The fit plan's eight
  probes passed in RogueLab round 8 (`SCOPE_FIT_PLAN_2026-10-28.md` §11); of the engine study's 13 facts (§14), N6 and
  N8-N11 stay read only until a round needs them (never N12).
  - **Release 1 state (2026-10-28):** the pack builds and boots (round 10); what's left is his play test of the new play
  build, his sprite sheet, his balance pass and one online evening, then a rebuild of the pack and his "ship".
  - **Blocked:** nothing; the research left needs Dimitri at the keyboard or a friend online. His calls from the play test
  (report section 5): Spool's first Tide (more starting Thread, a free first Stitched or a cheaper first Tin), Yossele's
  Red squeeze and the laser mining ore, a way to sell a walking Stitched, the Thread Head's stock MOVE. The 2026-11-03
  balance study answers the first two: no start-kit number fixes Spool's first Tide, the Pin Cushion wall does; the Red
  squeeze is every vessel's and the laser keeps mining (`BALANCE_2026-11-03.md`; its P1-P4 are Open questions 3).
  - **Next free numbers:** kit chapter 25, kit 1.15, pitfall #172 (#171 a tower's max Health under Frail Titan and Paper Tower, Spring Tide's test; #170 the HUD's idle hide, `LIGHTING_2026-11-05.md` §6; #168-#169 the decided code fixes, `DECIDED_CODE_FIXES_2026-11-05.md`; #167 a map's lighting, `LIGHTING_2026-11-05.md`; #162-#166 the build of the unbuilt candidates; #92 is unused; #105-#111 are Yossele's, #112-#113 Spool's, #114 Yossele's per-map look, #115-#120 the engine study's round 8, #121-#136 the build round of 2026-11-03, #137-#138 the dialog's placement, #139-#149 the test round of 2026-11-04, #150 the Basin's late start, PR #82, #151-#155 the code review's fixes, #156 the icons' pixel size, #157-#158 the max review).

- **2026-11-05: a look per weather, built** (Dimitri's pick (a), 17:45Z, `OPEN_DECISIONS_2026-11-05c.md`;
  `docs/reports/WEATHER_LOOK_2026-11-05.md`). `RWWeather` "the look" (`RogueWavesMode/Code/07_Weather.cs`): Rising
  Water light mist (`C9D6D8`, 0.8-2.2 x the camera's distance, the map's own ambient and sun), Quickening Current storm
  grey, Nesting Ground dusk, the PC's L1 numbers unchanged; a calm map sets nothing. `LookUpdate` sets it once a map on
  every machine from `RWRejoin.MapWeather()` once that machine's intro is over (`Game_Client_Init.Gametimescale`), with
  `ModKitLight.Set` and a 4 s fade; the ground is the lowest `Surface` tile (`RWUtil.GroundLow`, every machine). Self-test
  W4 on the three probe maps and the flow's calm map 1; the gaps build's L1 pictures the shipped three. `/simplify` and
  `/code-review high` run, their fixes in (report §2b). Compiled here: the mode release, self-test, gaps, gaps with shots,
  shots, chart look and feel builds 0 errors (the base's warnings), editor clean; the two-PC and dialog-look builds fail
  here as always. Not run on the PC. No kit change (kit 1.14 stays), no pitfall used: next free kit 1.15, pitfall #172
  (which PR used which pitfall numbers: the entry before this one).

- 2026-11-05, API credit and batch checks (Dimitri 20:13Z "Setup both of those options", on the coordinator's 20:12Z
  proposal). Checked against the Claude Code docs ("Authentication") and four help-centre articles: the Max plan's $100
  monthly API credit pays only for API calls with a key from the linked Console organization (and `claude -p` or Agent
  SDK runs with it); it can't pay for interactive Claude Code, cloud sessions or usage credits, and Claude Code has no
  automatic overflow to a key: a set and approved `ANTHROPIC_API_KEY` replaces the plan for every terminal session,
  while cloud sessions always keep the plan. The overflow is usage credits (`claude.ai/settings/usage`), paid
  separately. `docs/PROJECT_SETUP.md` "API credit" has the Windows steps (key in the cloud environment, or one
  PowerShell window, never a permanent variable) and how to confirm it. `TDS_Mod/Tools/batch/run_checks.py`: `plan`,
  `submit`, `collect`; the text audit's strings (1,310 from the six parts' game code) and one scope-to-code request per
  scope file (`01`, `03`-`14`, `16`), structured answers, report `docs/reports/BATCH_CHECKS_<date>.md`; cost table in
  its README. Ran here: `plan` for both checks on both models, the report writer on made-up results, the missing-key
  exit. Not run: a real batch (no key in the container). No mod code changed, so nothing compiled; no kit change, no
  pitfall: next free kit 1.15, pitfall #172.
- **2026-11-05: four project skills drafted** (Dimitri: "Draft the 4 skills", after the coordinator named four
  recipes re-explained to nearly every thread). `docs/skills/ship-a-pr.md` (branch, Part C entry, cloud-check, attribution,
  re-merge the base, second merger renumbers, merge commit via `merge_pull_request`), `pc-round.md` (his OK and the PC lock
  first; handoff, park and back up, apply, self-test builds, `Run-SelfTests.ps1 -TimeoutMinutes 30` for the mode, gaps
  launches, two players, scope copy, restore, dated report), `scope-audit.md` (one file a pass, the six marks of
  `SCOPE_CODE_AUDIT_2026-11-05.md`, `scope_upgrades.py`), `scope-sync.md` (his edits first from the Scope Editor's
  `scopes` rows and the project-files diff, keep his tildes, NEEDS lines, `build.py`, publish to the same URL), and a
  README on installing them. Each was also posted on a save card. Steps taken from `CLAUDE.md`, Part A,
  `TDS_Mod/Tools/pc/README.md` and the 2026-11-05 reports; project memory supplied two facts the repository doesn't hold
  (the PC lock lives in memory; RC sessions can't reach the project files). Docs only, no compile. Next free numbers
  unchanged: kit 1.15, pitfall #172.
- **2026-11-05: base game Cardinals split into Trinkets and Cardinals** (Dimitri: "Check which base game Cardinals can
  be turned into trinkets and which can only be teamwide, then separate them"). `docs/reports/STOCK_TRINKETS_2026-11-05.md`:
  of the 29 the shop sells, 13 are Trinkets (Sport Water, Fluffy Slippers, Beast Tassle, Diorama, Paper Tower: their
  Heroapply effects on the buyer's vessel; Rusty Pipe, Frail Titan, Voice in a Bottle: their effects on the buyer's
  towers or builds through the init list; the four Coins: the mode pays the buyer the whole sum; Aura Gavel: its bolt
  for the buyer's own units), held once by each buyer, on their vessel panel; 16 stay Cardinals (Basin, foes, ore, Basin
  builds, companion units). `RWCardinals.StockTrinketIds`, `GrantStock`, `Reach`, `RWTrinkets.CoinStart`/`Gavel`;
  gaps rules "Stock split" x2; a lobby pick or a pre-split save's Cardinal keeps the game's team-wide one. Scope `12`:
  the list as `~defaults~`. Compiled here (release, self-test, gaps, shots: 0 errors), not run on the PC (report §6 =
  next round's list; §5 three questions: Gavel, companions, Coin's sum). No kit change, no pitfall: kit 1.15, #172.
- **2026-11-05: Rogue Waves Spotlight** (Dimitri: "Design a spotlight of all of the new features added in the Rogue
  Waves mod"). A new artifact, https://claude.ai/artifact/MCPDgVngHmnZcVHeZwxNSr; the older Vessel Spotlight
  (NZ3wMMaujKDSNSaYCw7NZw, the per-vessel journal with the base vessels' private sheets) is kept as it was, since this page
  covers the whole pack instead. Source `docs/spotlight/` (`spotlight.src.html`, `build.py`, the built page). Read from
  scope `01`, `03`-`14`, Part C and the 2026-11-05 reports, `STOCK_TRINKETS_2026-11-05.md` included (13 base Trinkets,
  16 base Cardinals). Flagged as called a feature but not built: Depth, new enemy types, Spool's 22 upgrades and
  UNTHREAD, Spool's and Uloe's final sprites, three vessels' lore, the modifiers' journal pages, Fog Bank and the mood
  ramp (ideas); most of 5 November's work is marked built, not yet run, until PC round 4 reports. Docs only, no compile.
  Next free numbers unchanged: kit 1.15, pitfall #172.
- **2026-11-05: his answers on the split** (Dimitri 23:51Z: "1. Keep that 2. Make Trinket 3. Pay only share"). Aura
  Gavel stays a Trinket; Torrid Badge, Bendigo, Kippy and Fish Bait become Trinkets of kind Unit (their stock spawner on
  the buyer's vessel; `RWTrinkets.Companions` removes the drowned vessel's unit once the new vessel's stands); a Coin pays its buyer the sum divided by the players. 17 Trinkets, 12
  Cardinals. Report §7, scope `12` (list still `~defaults~` but his three answers). Compiled here (0 errors), not run on the PC. No kit, no pitfall.
- **2026-11-06: third scope-to-code audit** (Dimitri 00:44Z `/scope-audit`; card 00:48Z "All 17 files, changes only").
  `docs/reports/SCOPE_CODE_AUDIT_2026-11-06.md`, base `c197626`: every row of the 11-05 audit re-read against the ~35
  PRs since `99b9f3d`, plus the new scope lines; seven file passes. 512 rows: 355 MATCH, 117 RESOLVED (all 93 queued
  rows outside Spool now built), 24 UNBUILT-QUEUED (Spool's 22 upgrades and UNTHREAD, B19; Pin Barrage's Leads to,
  B18), 11 by design, 5 DIFFERENT. For his word: N1 Tidal Mend heals x the tower's level (11:38 says flat 5/8/12), N2
  Abyss's +10% slow held to +5% by `SlowCap` 0.6. Fixed in the scope here: N3 Depth's note (04:16, 05:13) no longer
  claims an end-screen line, N4 02:9's installed build, N5 15:176's Stacks "no limit" (his answer 3). His scope copies
  are behind in 02, 04, 05, 12, 15 until he says "sync". Docs only, no compile, nothing run on the PC. Next free
  numbers unchanged: kit 1.15, pitfall #172.
- **2026-11-06: Tidal Mend and Abyss follow the scope** (Dimitri 01:03Z, card in the scope audit's thread: "Code follows
  the scope" for N1 and N2 of `docs/reports/SCOPE_CODE_AUDIT_2026-11-06.md` §2). N1: `RogueWavesMode/Code/16_Directions.cs`
  `TideEffects` heals `MendHeal` 5 / 8 / 12 a rank with no tower-level factor; the gaps test checks each mend is that flat
  number (`RWDirections.Mends`). N2: `ArgoData.WaterSlowCap` 0.65 caps Deep Water (`Argo/Code/11_Water.cs`), the Tremor
  keeps `SlowCap` 0.6; the self-test reads the slow on a foe in the water (`ArgoUtil.WaterSlowOn`): the pump's whole
  slow, then the 0.65 ceiling. The two comments citing scope 11's dropped NEEDS line now point at Claude's sheets.
  `/code-review high`: seven of its eight findings fixed; the eighth (the ceiling is a typed number) is noted in Argo's
  `DESIGN.md` and notes. Compiled here: Argo release, SELFTEST, SELFTEST+SHOTS and REVIEW 0 errors 0 warnings; the mode
  release 0/0, SELFTEST 0/1, SELFTEST+GAPS and with SHOTS 0/2 (the base's warnings). Not run on the PC:
  `docs/reports/STOCK_TRINKETS_2026-11-05.md` §8. No kit change, no pitfall; next free numbers unchanged: kit 1.15,
  pitfall #172.
- **2026-11-06: batch checks extended** (Dimitri: "Help me set up the cheap batch jobs further"):
  `TDS_Mod/Tools/batch/run_checks.py` gains `design` (each vessel's DESIGN.md against its code: MATCH, DIFFERENT,
  NOT-IN-CODE), `docs` (CLAUDE.md, Part C "Now", the reports' README, the batch README and `docs/skills/` read against
  each other, after a free local pass listing cited paths that name nothing) and `art` (PNGs on Deep Swell and Foam
  against `docs/STYLE.md`, with `art_check.py` beside each); `--since REF|last` (only what changed since the newest
  report's base, per check), `--max-cost` (a true ceiling, every request at its max_tokens; default $10, exit 4),
  `--max-wait`, `spend` (the month's batch reports against the $100), exit code 3 without a key, `RW_BATCH_OUT` for the
  record. The key is read from `RW_BATCH_API_KEY` (Claude Code never reads that name), an environment variable of the
  cloud environment: a network secret is never attached to api.anthropic.com. Review fixes: scope 11 sent 5 cache
  marks (the API takes 4; now first and last part only), `--since last` keeps a base per check (a narrowed or failed
  run doesn't move it), `--art` sends only tracked files. `all` = text, scope, design, docs: about $4.07 (at most
  $6.94) on Sonnet 5.5, $0.76 (at most $1.26) on Haiku 5.5; art in use $0.29 / $0.01. Run here: `plan` for every check
  on both models, the report writer on made-up answers, per-check bases through failed, narrowed and explicit runs, the
  missing-key and over-cap exits. No real batch: the key is not in the cloud environment yet. No kit change, no pitfall; nothing ran on the PC.
- **2026-11-06: first batch checks run** (Dimitri: "It's saved", the key in the cloud environment): `plan --exact`
  (free) proved the key and counted 4.13M input tokens against the README's 2.78M estimate (about $1.32, at most $2.00
  on Haiku 5.5). Batch `msgbatch_01XM6GKkEVGqJgqX7PVfJCeP`: 25 requests, $1.19, `docs/reports/BATCH_CHECKS_2026-11-06.md`;
  seven answers (the four design pages, scope `07`, `08`, `11`) hit their max_tokens at medium effort, so they were rerun
  at `--effort low` for $0.63 (`BATCH_CHECKS_2026-11-06b.md`, every answer whole). $1.82 of the month's $100. A
  hand-written §0 sorts the rows into leads (meteor objective one map ahead, Uloe's Plug In cap and teammate light,
  Spool's Stash limit, design-page numbers, text slips, two stale docs lines) and noise ("Made of Tougher Stuff" is
  built in the mode; versions, labels and tolerances). Nothing fixed; docs only; nothing ran on the PC.
- **2026-11-06: sea chart navigation proposed** (Dimitri, 02:18Z: navigating "through just the nodes feels a little bit
  confusing"): read from the mode's code why (the vote's cards sit apart from the chart, the boat's camera frames one
  row, the boat sails by itself, no legend or progress) and proposed a chart view in the manner of Slay the Spire's map
  screen: the whole chart on one panel beside the lettered cards at every vote, clickable lit islands, a hover card
  with where each island leads, a key (~M~) to open it any time, a banner and a mini chart on the boat, a ring on the
  island it sails to (clients read the winner from the boat's heading: in the chart's name it would re-warp the party).
  Asked about new kinds of stop, he picked "Add rest and treasure stops" (02:28Z): a Cove (no map, ~a retry back~,
  rows ~4 and 9~; a Cove's win asks where to sail after it in the same map) and a Wreck (~one row harder~, ~a Trinket
  of 3~ each, rows ~2-3 and 6-8~), from a second random stream so a seed's maps stay the same; old campaigns keep
  charts without stops; difficulty follows the row. Mockups from the new `concept-tools/sea_chart_view.py`. Code and
  checks CV1-CV12 in `CHART_NAVIGATION.md` §6 and §8. Reviewer's nine findings fixed. Docs only; nothing ran on the
  PC. No kit change, no pitfall.
- **2026-11-06: PC round 4's results merged.** The round ran on his PC on `8b7daed` (kit 1.14; Dimitri "Go" 20:07Z
  11-05) and wrote `docs/reports/PC_ROUND4_2026-11-05.md` with its test-only lines (`92_Round4.cs` part `r4`, `92_Gaps.cs`
  F key, `92_Review.cs` R3's wall picture, Uloe `98_Upgrades.cs`, Spool `99_SelfTest.cs`). The cloud thread merged base
  `b1fffbd`, compiled the mode (`_GAPS`, `_GAPS,MODKIT_PLAY`), Uloe and Spool with their self-test defines, added pitfall
  #172 (the mode's Cardinal catch-up reaches towers already standing; Spring Tide's plain archer) and cleared scope `07`'s
  sprite NEEDS line (Yossele walks rows 4/6 and 5/7 and attacks rows 2 and 1 from `Art/`, report §6.6). Not fixed here:
  P1-4, U3's expected title, the 548 "Could not retrieve Vfx/Mobbase" lines, §8's test faults. Next free: kit 1.15,
  pitfall #173.
- **2026-11-06: Art Bench** (thread "Build sprite and model editing artifact"; Dimitri 02:10Z asked for an artifact
  laying out every sprite frame to download for Aseprite and send back, the same for models in Blender; draft approved
  02:19Z with "Give the whole row", "Blender is ok", the return "whichever is more token efficient": dropping on the
  page). Page https://claude.ai/artifact/PgF7QP6n2HN56syTDUaUi5, source `TDS_Mod/Tools/artbench/` (README): 64 sheets
  (1145 frames) of the four vessels and the mode, 90 mode icons, 175 models. A row downloads as a zip of one `.aseprite`
  (tags and `gen=` in the builder's merge format, the part's palette); a returned `.aseprite` or PNG is checked (size,
  frame counts, changed frames, new colours, magenta) and kept in the page's `edits` collection with the row as a PNG
  asset. `ingest.py` writes merge-route sheets into `ModSource/<Part>/Aseprite/<sheet>.aseprite` (only changed frames,
  mirrored facings follow) and file-route sheets into their PNG; models into `ModSource/<Part>/Art/Models/` (not read by
  the builders yet: the first model edit adds that and a PC round). The cloud preview's `m` run now also dumps each
  model's mesh (`meshes/`) and runs for RogueWavesMode (no Px, no builder). Tested here: `web/test_ase.mjs` 45/0 against
  `ase_sheets.py`; all 175 models through `.glb` and back, and through headless Blender 4.0.2, with 0 Khronos
  validator errors; three made-up sprite edits and one model through `ingest.py`, the builder's merge re-run in Python
  showing exactly the edited frames and a mirror following; the reviewer ran the real `YosseleAseprite.Apply` and its two
  findings are fixed (a second edit to a row kept the first; frames the file holds unedited still follow the code). Found: 629 triangles in 16 Spool and Uloe models have zero
  normals (thin triangles pass the builder's 1e-12 cross check but `Normalize` zeroes under 1e-5); not fixed, for a
  later kit fix. No mod code, no kit change, no pitfall; nothing ran on the PC.
- **2026-11-06: round 4 and batch bugs fixed** (thread "Fix round 4 and batch check bugs"; Dimitri 02:53Z "Fix bugs" on
  the round 4 findings and `BATCH_CHECKS_2026-11-06.md` §0's meteor, Plug In, Yarn Stash and text leads). Report
  `docs/reports/BUG_FIXES_2026-11-06.md`. Mode: `RWCard.HoldEnd` on every machine, a friend's `ClientEnd` writes the synced
  campaign title into the shown Win/Lose title (P1-4; the failing U3 was map 1's, so round 4 §5's "map 2" was a misread);
  `RWBody` loads `General_Default` at boot and gives a late joiner's things `Vfx/Mobbase` (pitfall #173, read from the IL);
  the meteor 12 hits and 8 tiles on map 1 (scope `14`:14); U3/T4, Spring Tide, Leave and the review's later map tests.
  Spool: one Yarn Stash a player (scope `08`:64). Uloe: `SurgeBankMax` 8 (scope `10`:145), Searchlight, Reflection, the
  stall. Argo: Chum Bucket's test foe. Left for his word: Mantle Tap's scope `09`:248 against `09`:249; Yossele's two lines
  are his. Compiled here (every part and test build at its baseline), `/code-review high` 7 of 10 fixed; nothing ran on
  the PC. No kit change; pitfall #173. Next free: kit 1.15, pitfall #174.
- **2026-11-06: input artifacts brought up to date, old ones archived** (Dimitri 02:57Z: "Update older Artifacts that take
  input ... now that I updated Claude with artifact capabilities, and store older and less relevant artifacts in a folder
  elsewhere. Pin the artifacts that are expected to be revisited the most."). The Scope Editor (version 11) and the Vessel
  Editor (version 2, contract 0.2.96) gain the Art Bench's return path: a Send step marks a saved row `waiting` with his
  note and the commit the page was built from; "bring in my scope edits" / "bring in my vessel edits" has a thread fold
  the waiting rows in through a reviewed PR and write `pr` then `merged` back, which the page shows. The Scope Editor
  also gains `user` (view-only viewers lose the Send button). Both rebuilt from `docs/scope` at `a9d84a3` (the Vessel
  Editor had read the stale project-files copy of 2026-10-23); their sources move into `docs/artifacts/` (index and
  steps: `docs/artifacts/README.md`; `scope-sync` skill updated). The Art Bench already had the path: unchanged. Six
  pages whose decisions are made (the first Scope Editor, Temporary Art, Temp Art Picks, Argo Diver Takes, Minion
  Picks, the 10-09 concepts) are copied with their picks to `/mnt/project-files/artifacts_archive/2026-11-06/`
  (`MANIFEST.md`; the first Scope Editor from its build file, as it could no longer be read) and deleted from his
  gallery at his pick. Reviewer's fixes: a save queued behind a failed send still runs; `invalid_argument` means view
  only; a thread's status write no longer redraws the Vessel Editor (keeps the Send bar's note and undo). Pinned: Art Bench, Scope Editor, Spotlight. Tested here: both pages headless
  against a mock store (edit, send with note, a thread's `pr`, a cleared row, phone width), no errors. No mod code.

- **2026-11-06: the sea chart view redrawn in the game's look** (Dimitri, 05:23Z: "Draft a design that is more inline
  with the current TDS art style and theme"): the first draft's board (rounded panel, discs, heavy type) replaced by the
  stock UI's own pieces as the PC round 4 pictures show them: no panel, the chart on the dialog's dim; the stock title
  with its glyph and the rule that turns down the right; one caption line with "•" instead of a hover box; square tiles
  framed like the stock cards (teal, coral Breakers, slate once sailed); coloured 12 px pixel icons (Cove, Wreck,
  harbour, boat; a white tick); white corner brackets on the choices; a banner in the HUD's "TIDE APPROACHING" shape.
  New `concept-tools/sea_chart_look.py` (mockups `docs/concepts/art/chart_look_vote.png`, `chart_look_boat.png`; a
  preview over a round 4 screenshot kept in the project files only). `CHART_NAVIGATION.md` §2, §3.1, §3.6, §6 and §8
  updated; two review rounds fixed. Docs only; nothing ran on the PC.

- **2026-11-06: batch checks on the PC without Claude** (Dimitri 05:47Z: "a method of doing the batch job that does not
  rely on messaging you so I can still call it when my 5 hour limit is up"): `TDS_Mod/Tools/batch/Run-Batch.ps1` keeps
  the key as the Windows user variable `RW_BATCH_API_KEY` (`-SaveKey`, asked hidden), pulls the base, installs
  `anthropic` and `pillow` once, runs `run_checks.py submit --since last --model haiku --max-cost 3 --max-wait 120`
  (`-Plan`, `-Full`, `-Model`, `-Check`, `-MaxCost`, `-Collect`), and moves the report to `Desktop\RogueWave\batch` for a
  thread to read and commit (moved, so an untracked report never stops the next pull). `run_checks.py`'s `git()` returns a failure instead of crashing when git is missing, and `submit` checks `--max-cost` on counted input (the guess ran low: the first full
  Haiku run cost $1.19 against $0.76 estimated); max_tokens back to 64k scope / 48k design (the first run cut seven
  answers at 40k / 24k); default `--max-cost` 15. README "On your
  PC, without Claude"; `docs/PROJECT_SETUP.md` points there. The key bills the API credit, never the plan. Written here,
  not run: the container has no PowerShell; Python side run here (`plan`, git missing). Nothing ran on the PC.
- **2026-11-06: the Rogue Tide theme** (Dimitri asked for a theme from the TDS UI, drafts first, then picked Rogue Tide
  of three: Fogbound Slate, the stock grey; Rogue Tide; Harbour Fog, a light one). `docs/themes/rogue-tide.md`: STYLE.md's
  palette (Deep Swell ground, Rogue Teal tiles, Foam text, one Storm Coral accent, Wet Sand, Pearl for Pure Aura only),
  Exo 2 Light caps / Barlow / Pixelify Sans, the HUD's bracket frame and bevelled square tiles, CSS tokens and contrast
  rules; colours measured from the round 4 screenshots. STYLE.md and CLAUDE.md's map point to it. Docs only.
- **2026-11-06: Scope Editor refreshed** (Dimitri 06:19Z: "Update the rogue waves scope editor with updated information"):
  the scope files had not changed since the page's v12, so the new information went into them first: `02_Progress.txt`
  [Now] gains the chart navigation and the unproven round 4 fixes, its [Log] the stock Trinket split, Tidal Mend and
  Abyss, the round 4 bug fixes, the chart navigation with its stops, and the Rogue Tide pick; `04_Rogue_Waves_Mode.txt`
  [Sea Chart] gains the chart view and look lines and {Cove} and {Wreck} sheets from `docs/mode/CHART_NAVIGATION.md`
  §4.1-4.2, every value Claude's ~default~. `docs/artifacts/scope_editor/template.html` wears Rogue Tide (Exo 2 caps,
  Barlow, Pixelify Sans; square tiles, bracket frames; Deep Swell in dark, Foam paper in light); the Send flow is
  unchanged. Rebuilt and republished to the same link after the merge. Docs and page only; nothing ran on the PC.
- **2026-11-06: the feature spotlight as the finished pack, in Rogue Tide** (Dimitri: "update the spotlight using the
  rogue wave theme. Have the spotlight not mention if an asset is completed or not, as if it were a finished product
  ready to ship"). `docs/spotlight/spotlight.src.html` restyled with `docs/themes/rogue-tide.md` (Deep Swell ground,
  bracket frames, bevelled teal tiles, one coral banner, Exo 2 / Barlow / Pixelify Sans, one dark look). Removed: every
  status chip and the legend, the "not built" section, the release plan, temporary-art notes, the dated "list changed
  today" box, the Steam line. Added: the chart view (`chart_look_vote.png`), the M key, the boat banner, the Cove and
  the Wreck; weather never on a Cove or Wreck; a Cove gives a retry back. Upgrade tooltips still carry no numbers. Kept
  separate from the Vessel Spotlight (his merge answer still open). Republished to the same artifact URL.
- **2026-11-06: sea chart navigation built** (Dimitri 05:59Z: "Go"; the §7 defaults as written, his to change):
  `03_Chart.cs` and `sea_chart.py` place a Cove (rows 4, 9) and a Wreck (rows 2-3, 6-8) per stretch from a second
  stream, equal in C# and Python; `S` in the chart's name; the Cove rests on the boat (a retry back, or each player's
  lowest Direction a rank up) and asks where after; the Wreck plays a row harder and gives a free Trinket of three;
  `26_ChartView.cs` shows the chart beside the vote (islands vote), on M, and a banner and mini chart on the boat;
  self-tests CV1-CV5, CV9-CV12 (`90_Stops.cs`, `96_Flow.cs`); save fuzz F5 covers `S` and `save_fuzz.py` compiles again.
  `/code-review high` fixed 8 of 10; `/simplify` run. Compiled here (release 0/0, self-test 0 errors), not run on the
  PC; PC list `docs/reports/CHART_BUILD_2026-11-06.md` §4. No kit change, no pitfall.
- **2026-11-06: multiplayer MVP board proposed** (Dimitri 06:35Z: "Draft an MVP board for multiplayer that shows
  accolades and failures of the players"): `docs/mode/MVP_BOARD.md`. A "SHIP'S LOG" board after each won map (after
  the leave vote, before the picks) and a Voyage board at the campaign's end, two or more players only; six accolades
  and seven failures plus a fallback for each, everyone gets exactly one of each, an MVP in corner brackets under the
  one coral banner. Most of it reads `Activeplayerdata` (kit `docs/16` §5), snapshotted per map; new: aura sent per
  map, unanswered votes, drops, and leaks charged to the last hitter (an engine hook). Mockup
  `docs/concepts/art/mvp_board_map.png` from `concept-tools/mvp_board_look.py`. Docs only, nothing compiled or run on
  the PC; no kit change, no pitfall.
- **2026-11-06: the menu buttons' wobble fixed** (thread "Fix stuttering buttons in main menu"; Dimitri 05:48Z "The
  buttons in game still stutter, but mainly in the main menu", 05:57Z "The button itself wobbles and jitters, including the
  base game buttons in the main menu"). Report `docs/reports/BUTTON_WOBBLE_2026-11-06.md`. Read from his game's `level1`-`3`
  and `boot.config` (UnityPy, read only): at UI scale 3 the stock layout centres Play's 48-unit picture in a 75-unit tile and
  the 16-unit icons in 45-unit tiles, half a canvas unit off; the `Ui` canvas is Screen Space - Camera on a child of
  `Main Camera`, and the main menu's and the boat's virtual cameras carry Cinemachine noise, so the pixel columns on the
  half lines flip from frame to frame. Not a hitch: the GC is incremental and the mod's main-menu garbage is about 9 bytes
  a frame; hover only swaps sprites and tweens colour. New `RogueWavesMode/Code/26_Pixels.cs` (`RWPixels`, every machine,
  every frame after `RWMenu`): a pass every half second, 64 pictures a frame, no garbage, moves each still `Image` and
  `RawImage` of a screen-space menu canvas by under half a pixel onto the screen grid measured from the canvas's corner;
  skips scalers, transitions, animators, layout cells, scroll lists, `Bla_Ui_Confirmation` and a picture someone else keeps
  writing. Self-test P1-P2 (`94_Pixels.cs`, in `96_Flow.cs` after B1): whole pixels, and 144 frames read back with the fix,
  then at the game's own places (`RWPixels.Restore`), then on again. Kit `docs/09` #174 and `docs/20` §6. Compiled here
  (every mode build; `TWOPC` and `DIALOGLOOK` keep the base's stub errors), `/code-review high` ten fixed. PC round
  `Wobble_round1` (his OK 05:57Z, 06:15Z; code `2a3d907`): mode self-test 168/0/0 with every part on, P1 and P2 pass, FINDING
  P2 SEEN (the game's own places change in 144 of 144 frames at 2560x1440, ours in 0); release build and boot check of six
  parts, 0 exceptions; the fixed mode build stays in his Mods, the old one parked; `Run-SelfTests.ps1` doesn't restore the
  save, so it was copied back by hand (the README said otherwise).
  No kit change; pitfall #174. Next free: kit 1.15, pitfall #175.

- **2026-11-06: chart round 1 on the PC** (Dimitri 06:59Z: "Go chart round"): handoff `Chart_round1` (`0be84ef`, code
  `5e03648` = the chart build on `beab230` plus the wobble fix, since merged as #150). Mode flow 178 pass, 1 fail: CV10's
  "never picked" line ran before the boat wrote its blacklist; the rest, Continue and sail-on lines pass, CV1-CV5, CV9,
  CV11, CV12 pass. Two players 167/0 over six launches. Captures in `Desktop\RogueWave\chart_round1` (PC only): the
  staging vote with the chart beside the cards, the boat's banner and mini chart, "SAILING TO THE COVE"; open for his
  eye: the after-win vote captured with no view and its title over the cards, and the game's top-right title over the
  mini chart. CV10 now waits for the blacklist (compiled here). Not run: mouse clicks on islands, key M, the hover hitch.
  PC: Mods hash-identical, save copied back by hand, Unity project at the handoff with a TWOPC build.
- **2026-11-06: pitfall fixes assessed** (thread "Find fixes suggested by the pitfalls"): every pitfall #1-#173 checked
  against the six parts and `Shared` by three read-only sub-agents (two Haiku scans by number range, one Opus pass over
  the batch §0 leads, the code review's 22 left items and the chart reviewer's notes), each hit read in place. Top items:
  Spool's and Uloe's brains switch off on their first exception (#80), a missing stock elite's stand-in comes from the
  campaign's own four (#146), a Wreck-given Pearl Oyster pays nothing that map. Not bugs: Uloe's `"Collide"` (the game's
  beam param), the vessels' spawn-time `Updatecommand(..., true)`. Docs only.
- **2026-11-06: MVP board built** (Dimitri 06:55Z: "It doesn't need to account for one accolade and one failure, just
  the most notable thing of the player (balanced so that getting a failure is as common as getting an accolade).
  Defaults for rest, and then build."): each player gets the one title whose share stands out most above an even split;
  half the crew takes a failure, an odd crew one more on every other odd-sized board, so failures and accolades come out equal over a run
  (MB3: 1503 and 1503 over 1002 made-up boards). Quartermaster (most aura spent) joins the accolades; the MVP is apart
  from the title. `27_Board.cs` snapshots `Activeplayerdata` per map, keeps leaks (the Basin's `Oninteract`, charged to
  `Lasthit`), unanswered votes, drops, aura sent and the run's totals (saved as the run line's ninth field); writes four
  cards and a header as `RW_Log0-4` on every vessel. `20_Vote.cs` holds the break in state "Board" for 12 s or until
  all press Continue (order "B"); after map 10 the Voyage board follows. `28_BoardView.cs` draws it with the chart
  view's pieces; Tab replays it. Self-tests MB1-MB8 at boot (`90_Board.cs`), MB9-MB11 in the two-player round. The
  cloud stubs gain Tab, Enter, Space, NumpadEnter and `Gamepad.buttonSouth`. Mockup redrawn with one title a card.
  Compiled here (release, self-test and gaps 0 errors), MB1-MB6's pure code run under mono; not run on the PC. PC list
  in `RogueWavesMode/IMPLEMENTATION_NOTES.md`. `/code-review high` fixed 10 items, the PR #151 reviewer 4 more (a stale
  board on the next map, MB10 times its Continue, odd boards alternate on a count, two doc slips). No kit change, no
  pitfall (HashSet is already #1's).
- **2026-11-06: the whole-pixel fix's review follow-up** (Dimitri 07:24Z: "Follow up pr", after PR #150 merged with the
  review's two points open). `26_Pixels.cs`: the children of a `Blu4_List_Stacker`'s `Ref` are left alone (the stacker
  sits on the Ref or above it, writes them in its `Update` and is no `LayoutGroup`; read once per picture, when its seat is
  made); a picture under a `Blu4_Button_Animator` whose Enter or Click list holds a `Blu5_Buttonanim_Size` (a DOTween
  `DOScale` of the button to `Tosize` and back to exactly 1) keeps that animator in its seat and is skipped while the
  button is `Active` (hovered) or its scale isn't 1, so it is placed at rest and never moved while grown. Stackers in his
  game: one in `level1` (the patch banners), five in `level3`, none on the boat. `94_Pixels.cs`: a still P2 passes only
  if the same reading saw the flicker at the game's places, else SETUP on half pixels, INFO when they are whole already
  (an even UI scale); a flicker with the fix still fails. `/code-review high` on the first draft: a scale gate dropped (it
  unplaced static scaled pictures and failed P1 under a resting pointer), the per-pass stacker scan dropped, the control no
  longer hides a failure; the reviewer's pass: the grow skip only while grown, the even-scale case, the notes' wording. Compiled here (the mode with no define, `SELFTEST`, `+SHOTS`,
  `+GAPS`: 0 errors); not run on the PC; his Mods hold the round's `2a3d907` build. No kit change, no pitfall. Next free:
  kit 1.15, pitfall #175.

### Decisions

- 2026-11-05: Claude merges PRs into the base itself, merge commits, after the base is merged in and the cloud-check
  passes (Dimitri: "Automatically merge pull requests"); replaces "only Dimitri merges".
- 2026-10-08: vessels arrive one at a time through the kit loop; Argo first. Mode feasibility (Phase 1) closes before
  any vessel code.
- 2026-10-08: Cardinals, bounties and the shopkeeper are built on the base game's own systems where Phase 1 finds
  them usable, never reinvented.
- 2026-10-08 (user): **Cardinals are the base game's run-modifier effects.** A new Cardinal is a new run modifier.
  **Score does not count** in the Rogue Waves mode, so leaderboards and the Cardinal score multiplier are out of scope.
- 2026-10-08 (user): "Streamlined Economy Changes" means **aura sharing between players between tides**, which is the
  scope's Fund Transfer. No separate design; it is one Phase 5 piece.
- 2026-10-08 (user): Uloe's Defense building is **Mirror**.
- 2026-10-08 (user): the scope text is **the planned version of every vessel, Argo included**. Argo is designed from
  the scope text; its shipped 1.4.0 code in `ModProjects` is reused only where it already matches, and every
  difference is listed in `DESIGN.md` as a change from 1.4.0, not from the scope.
- 2026-10-08 (user): **Argo 2.0 design approved** (`TDS_Mod/ModSource/Argo/DESIGN.md`): five buildables with Deep
  Water, the Harpoon's damage or crowd-control paths, an aimed Tremor, a limit-1 Miner's Base, no "loaded guns".
- 2026-10-08 (user): **Argo is designed without SONAR for now.** No ability button and no Pinged debuff; 1.4.0's
  SONAR code stays in the source, unused, until the user decides.
- 2026-10-08: the kit chapter for the mode engine is `docs/15` (`docs/14` went to ModTool and packaging).
- 2026-10-08 (defaults taken): three Directions offered and one picked per map, extras bought at the shop (superseded
  2026-10-09: the shop sells only the next rank of a Direction already held); ascension persists across sessions if
  Phase 1 finds save writes safe, else per session; online is 2-4 players, host-authoritative, every player with the
  whole pack; the F-key respawn fix covers the new vessels, and Boutman if it is the same code path.
- 2026-10-08 (Phase 1, default): the mode is the stock `Mode_Normal` loop (named by the mod's `Modeobject`) plus a
  host brain in the pack's code; a mode class of our own is impossible (`docs/15` §2).
- 2026-10-08 (Phase 1, default): run state lives on the host's `DontDestroyOnLoad` object; Pure Aura shows as a counter
  on each player's vessel; ascension is saved in `Bla_Data` folder `RogueWaves` (save writes are safe, `docs/15` §8).
- 2026-10-08 (Phase 1, default): the F-key fix is a client-side reorder of the `Hero` tag list, for Boutman and every
  pack vessel; minions keep the `Hero` tag (`docs/15` §9).
- 2026-10-08 (user, Phase 1 gate): **every map is voted on, map 1 included**, so map 1 opens with a staging step (the
  leader's pick loads, the opening break is held for Direction picks and the vote, then the host chains to the voted
  map, `docs/15` §6; the chaining was superseded 2026-10-09 22:23Z by the sea chart, below). The start rule (the host
  equips the mode's Cardinal) was **replaced 2026-10-09** (below); it was never named, since "Rogue Waves" is the
  expansion's name (user). RogueLab tests the colour slot, which checks the name; the Mixed slot takes any string. **The
  §11 PC round is approved.**
- 2026-10-08 (Phase 1, input to Phase 2): each prefab's code is its own assembly and mods can't see each other's
  types (`docs/15` §10), which favours one pack mod carrying the mode.
- 2026-10-08 (user, Phase 2 gate): **one `RogueWavesMod` bundle** with a `Default` prefab per part (mode, foes, each
  vessel), parts developed and self-tested as their own dev mods, no load order, one pack version (`docs/PACK.md`).
  **Style approved** (`docs/STYLE.md`): the pack look lives in run pieces; vessels look as the user's concept art draws
  them, house-style departures kept as drawn until each vessel's design gate. Kit 1.6 = `docs/PACK.md` §3.
- 2026-10-08 (user): **Argo ships only inside the full `RogueWavesMod` pack**, no standalone ArgoMod 2.0. Argo's
  balance pass waits for a later session of his; step 5 is lessons and notes, no release zip. (Release order
  superseded 2026-10-20: Yossele is released first, alone; Argo still ships only inside the pack, later.)

- 2026-10-09 (research): **Directions are held per player on the vessel, not on the core** (the core is the team's,
  `docs/15` §14.1); `ModKitDirections` changes to that as kit 1.7 when Phase 5 starts. Per-player run state is keyed
  by `Sessionid`. The base game scales nothing by player count or map, so the mode's difficulty is its own (§14.3).

- 2026-10-09 (user; superseded below: 10 maps, Depth, about 7 Any and 4 per new vessel, only the host's quit spends
  a retry): **a run is a campaign of 15 maps, saved between maps** (was 3 maps in one sitting); the host's
  save holds it, keyed per player by Steam id (`docs/reports/SCOPE_VS_LOOP.md` §E; since 2026-10-14 the Steam id is
  known to be out of a mod's reach, so per-player rows key on the Steam name or the GUID the pack keeps, kit `docs/23`
  §2.3, `docs/19` §5). **Swell (ascension) is on hold** until the
  core loop is settled. Per-player Cardinals researched (§F): most stock ones can be made personal. **Directions rank
  up** (II, III) when offered again to a player who holds them, so the catalogue stays about 10 Any and 6 per vessel. **A lost map spends one of a few retries** (3 by
  default) and replays from the last save; quitting mid-map counts as a loss; with none left the campaign ends.
- 2026-10-09 (user): **a campaign starts from a Rogue Waves button on the main menu** (New / Continue), which takes the
  host to the boat with the mode set; friends join there by invite. No mode Cardinal (`docs/mode/MAIN_MENU_START.md`).
- 2026-10-09 20:48Z (user, roguewave 2.txt): **a campaign is 10 maps** (replaces 15), won by leaving map 10 after
  Tide 15 or later; the Direction catalogue target drops to about 7 Any and 4 per new vessel; the map shape default is
  two stretches of five. **Ascension levels are Depth 1-5** (was Swell), still on hold. Bonus tide pay stays 2 and the
  Fund line keeps the 19:47Z wording, though the uploaded file still had the older text.
- 2026-10-09 (user, roguewaveWIP.txt): shopkeeper **VENDCINT** drops in near the vessels' spawn each break and leaves at
  its end; sells Cardinals and **only the next rank of Directions a vessel holds**. **Fund Transfer: base aura only**,
  50 x the coming tide per break (50 before Tide 1). Pure Aura 3 a tide, **2 a bonus tide** (both confirmed 19:47Z).
  Leave by **majority**; **bonus tides never end** until that vote; a failed campaign keeps nothing; 3 retries; every
  base map in the vote; base vessels get Directions; modifiers show as an effect. 5 Depth levels (then called Swell;
  still on hold). Model Lighthouse kept as a name; the main-menu button stays the start rule (confirmed on a card
  18:20Z).

- 2026-10-09 (user): **the scope is one file per category** (`docs/scope/`); he sends back only what he edits.
- 2026-10-09 22:23Z (user): **maps are chosen on a sea chart** (`docs/mode/SEA_CHART.md`): a seeded Slay the Spire style
  map of 10 rows the boat sails across; the vote offers the maps linked to the last one (1-3), is held in the map
  (route **V1**) and the boat then sails to the winner; the party returns to the boat between maps. Default taken:
  map 1 is voted in the staging map's opening break, then the party sails from the harbour to it.

- 2026-10-10 (user): **sea weather is in the scope** (`docs/scope/05` [Sea Weather], in `04` since 2026-10-14;
  `docs/mode/SEA_CHART.md` §8). Defaults taken: three weathers (Rising Water, Quickening Current, Nesting Ground);
  rows 1, 5, 10 calm, 1 in 3 then 1 in 2 elsewhere; +1 Pure Aura a Tide; Quickening x1.25 from Tide 5, x1.5 from Tide
  10. Maps spread by row difficulty (aim 2 + (row-1)/2, Breaker rows +2), none twice. The mode part is
  `RogueWavesMode`; nothing starts a campaign until the main-menu button piece calls `RWCampaign.New`/`Continue`.

- 2026-10-11 (user, scope review): only the **host's** mid-map quit spends a retry; **no Cardinal swaps** on the boat
  between maps; **breaks last 60 s**; Direction **rank II 20, rank III 30** Pure Aura; Team Cardinals work for everyone,
  **Cardinals stack with no limit**; a **new objective every break**, team objectives pay every player in full;
  Fertilize stays Direction-only; a bonus tide still pays 2 (`docs/reports/SCOPE_REVIEW_2026-10-10.md`).
- 2026-10-11 (user, card): **dying in a bonus tide counts as a won map**: no retry spent, the tide in progress pays nothing.
- 2026-10-11 (user, quality check Q1-Q3): **Cardinals scale forever**: no limit on Team or Vessel Cardinals (no cap of 8;
  past the HUD row's 8 counters the mode shows Team Cardinals another way, settled when the shop is built); Cardinals
  split into **Team** and **Vessel** (was Personal). A dropped friend can rejoin the running map (`04`). The scope
  review's six wording fixes are folded in (#8, #17, #21, #30, #32, #52).
- 2026-10-12 (user): the name is spelled **Rogue Waves** everywhere, the mode and its main-menu button included
  (was "Rogue Wave"). Identifiers (`Mode_RogueWave`, `RW_` save keys), file names (`04_Rogue_Wave_Mode.txt`) and the
  PC folder `Desktop\RogueWave` keep their old spelling, so saves and the scope's file names stay valid (the scope file
  was renamed after all on 2026-10-14: `04_Rogue_Waves_Mode.txt`, below).
- 2026-10-13 (user): every vessel sheet follows `docs/scope/15_Vessel_Template.txt` (`15_Templates.txt` since
  2026-10-14); `~` marks a Claude default. Yossele attacks at range (a green laser from the mouth) and moves slower;
  Selfish Sapling is now Forest Project and Grafted Colors is Grafted, Entangling Spaghetti Roots is Entangling Roots.
  Spool's ability is **Patch** (20 Thread heals 20%), Unthread is just selling, and a **Yarn Stash** (placed at the
  start, like the Bouthouse) holds the Thread.
- 2026-10-14 (user): the scope follows `docs/scope/00_Read_Me.txt`'s rules: one meaning per symbol, every number
  on a `- Field: value` slot, `/~ \` for Claude's notes, `->` to point at another file, each fact in one file, dates
  only in `02`, and every template in `15_Templates.txt`. `04_Rogue_Wave_Mode.txt` is renamed `04_Rogue_Waves_Mode.txt`.
- 2026-10-15 (user): Spool's purple stays as it is for now, though it sits near the crystal foes' magenta (kit
  `docs/18` §7); every other new design keeps magenta for foes only.

- 2026-10-15 (default): `docs/` holds living pages only; dated reports go to `docs/reports/` (frozen, a note on top
  when something changed since), the mode's design pages to `docs/mode/`. Handoff folders hold only round packs.

- 2026-10-15 (user: "Use defaults for all", on `docs/reports/QUALITY_CHECK_2026-10-15.md` §4 and the open questions):
  - **Kit 1.7 before the main-menu button ships**: "Leave" fires when the map object is gone (B1), the focus mark
    survives the build's rename (B2), `ModKitTides`' rewind comment and `ModKitCombat`'s header (B17, B18).
  - **One pack version** for every part, read by its data and its prefab, set when the pack is first built (B3, B5);
    the mode warns on the main menu on a mismatch, built with the pack (B6).
  - The vessel template gets kit 1.6's pack calls before Spool is scaffolded (B10). Argo's four plain skips become
    `TEST SETUP` at its next round (B12); Salt Sting's text and the unused Stagger wait for its balance pass (B14, B15).
  - Tools: `api-dump.ps1` is brought up to the dump's format at the next PC session (C2); `setup.sh` unzips into a
    temporary folder after PR #30 merges (C23, done); the API dump gets a header line at the next dump (A19); old Argo notes
    stay as history (A17). Done in PR #36: `gh` before the token (C6), BootCheck's `icons:` line (C5), PS 5.1 patch
    lists (C10), the log read race (C2-3), the allowlist (C19).
  - Scope: PR #32's thread fixes scope 16 (Knight Commander, Diesel Dog, Rev Dog, Battalion, Black Sno) and the
    wording items D20-D22, D26, D27; after #32 merges, the repo's `docs/scope/` is copied over the project files (D1).
  - Directions: names match the Look line; the heading or course style is optional (`SCOPE_REVIEW_2026-10-10` #37).
  - The GitHub default branch becomes `claude/phase-1-6lfnar`; only Dimitri can switch it (GitHub settings). Done:
    it is the default now (`docs/PROJECT_SETUP.md` §1).
- 2026-10-16 (user): a Claude default in the scope is surrounded by `~`, `~value~`, not only prefixed. Claude's
  default: the closing `~` goes right after the value, before its unit or punctuation (`~3~ tiles`); a phrase or a cost is
  closed at its end (`~Red 25 + Blue 10~`); an empty `~~` is a slot Claude will fill.
- 2026-10-20 (user): **the first release is Yossele alone; everything else comes afterwards** (the mode and its sea
  chart, Argo, Spool, Uloe, the foes). Part B "Release order" and "Release 1". Defaults taken, his to overrule (scope
  `01`, confirmed 2026-10-21): the release is the `RogueWavesMod` bundle holding only Yossele's part, version 0.1.0, a zip he
  shares (the game has no Workshop, `docs/14` §4), so later releases update the same mod; outside the mode there are
  no Directions, so Release 1 has no Fertilize; it waits for his own sprites.
- 2026-10-21 (user): Release 1's four defaults confirmed; Yossele's sheet answers: fertilizer resets each map,
  Cordyfriend's range 1.5 tiles, the Tree's capstone is Dandelion Storm, "long tide" maps have 4 Tides or fewer, one
  Bonsai per player.
- 2026-10-28 (user, "Use defaults for now, then proceed", on `docs/reports/PRIORITIES_2026-10-28.md`): Yossele's laser
  keeps mining ore; `07`'s costs, healths and in-game lines stay as written (the Red/Blue balance waits for his pass);
  the D-MDS line stays his; the temporary Cordyfriend ships unless he sends his own; the dev version becomes 0.1.0-dev
  (pack version plus -dev, `docs/PACK.md` §4) and the mod's `.info` carries it; the HUD cut-off is checked by his eye
  against Smacky's build bar (no desktop screenshots here). Scope `07`'s Sprites slot now asks for his sheet. The pack step is built:
  `TDS_Mod/ModSource/Pack/pack.txt`, `Tools/pc/Build-Pack.ps1`, Tools > ModKit > Build Pack, `Run-SelfTests.ps1 -Pack`.
- 2026-11-03 (user, decision card on `docs/reports/UX_FEATURES_2026-11-03.md`): **the five recommended UX features join
  the Phase 5 list**, each built beside the piece it touches: the campaign card (T1-T4), the retry warnings (B4, T4),
  the no-mods warning with the rejoin line (L1, L5), weather icons on the chart (O1) and the pack's tips (T6). The
  other feasible rows stay in the report as reference.
- 2026-11-03 (Claude, defaults while building; each stands until Dimitri says otherwise): the sea chart shows the base game's own islands, the home
  world's water and rocks at runtime, no new art; the main-menu button is a tile beside Play, so the stock column keeps its places; after a loss the boat
  sails back by itself; Bottom Rock (`Artifact_Rock`) stays off the shelf. The lists in scope `04`, `10`-`14` are `~defaults~` beside their `! NEEDS !` lines.
- 2026-11-04 (Dimitri, "Accept all changes, then update the scopes in the documents"): every upgrade of
  `docs/reports/UPGRADE_GAPS_2026-11-04.md` is in: Yossele's Wood Wide Web (scope `07`), Spool's 22 and the Cookie Tin's
  UNTHREAD button (scope `08`, which no longer has a `! NEEDS !` line). Open question 7 (then the upgrade gaps; the number now holds the new Directions) is closed. Not built yet.
- 2026-11-04 (Dimitri, new Directions and Cardinals): every Direction and Cardinal in the scope names its maker in a `By:` slot
  (rule in scope `00`). Two that clash form a research-choice pair (`~default~`, Open question 7).
- 2026-11-04 (Dimitri, "Rename Vessel Cardinals as Trinkets and Team Cardinals as just Cardinals"): a run modifier
  that reaches one player's things is a **Trinket**; one that reaches everyone, and every stock Cardinal on sale, is a
  **Cardinal**. Renamed in the scope, the living docs and the mode's code and shop text; dated entries and reports keep
  the old names (a note on each report). A saved campaign keeps loading: the save key `RW_Run_Team` and the held ids
  `Cardinal_<Name><n>` stay (`TDS_Mod/ModSource/RogueWavesMode/IMPLEMENTATION_NOTES.md`).
- 2026-11-04 (Claude, building the code simplification at Dimitri's word): armour and "takes more" stay in the pack's
  own hit hooks; the stock `Resistadd` route (finding 1) waits until its four number changes are dealt with
  (`docs/reports/SIMPLIFICATION_BUILD_2026-11-04.md` §2). Pack buildings use the game's own upgrade AI.
- 2026-11-05 (Dimitri, "Leave as is" on the chart maps card): a chart's islands keep taking their maps from the game's
  map list at each load, so a TDS update that adds or drops a map mid-campaign redraws the islands' maps; the rows,
  links, route and retries stay (`docs/reports/QUALITY_CHECKS_4-7_2026-11-05.md` §1.4 item 5). Same day, his "sync":
  his project-files scope copy and the Scope Editor brought up to the repository.
- **2026-11-05: the open decisions answered** (Dimitri, 04:59Z, on `docs/reports/OPEN_DECISIONS_2026-11-05.md`: "10. Cursed
  should also weaken vessels. 15. Create art for new looks and rename as well. 18. Uloe can share light up to a limit.
  19. Build all for now. 25. Balance as if you only build one wall, and use recommended for rest. 26. Reduce the amount
  mined if it is unbalanced. 34. Create art for every missing asset. Use defaults for the rest."). Every answer, and
  what it needs, in `docs/reports/DECISIONS_APPLIED_2026-11-05.md` §1. Among them: the mode polish's nine readings
  confirmed; MODE-D5 stays; lobby Cardinals lock once a campaign starts; caps of 6 copies and one stock Cardinal kept;
  Spring Tide and Tidal Mend move to the mode's own effects; a teammate's Uloe Light counts up to 4 a building; Yossele's
  P5 and P6 wait for his play test; `D:\TDS_second` and the kit's older-mod examples stay. Open questions 1 and 3-13 closed by it; 2 (Spool's `DESIGN.md` defaults) was not on the page and stays open. Same day, his "sync": the project-files scope copy and the Scope Editor (version 8) brought up to the repository; and
  question 2 spelled out as S1-S17 in `docs/reports/SPOOL_DEFAULTS_2026-11-05.md`.
- 2026-11-05 (Dimitri, "Check which base game Cardinals can be turned into trinkets and which can only be teamwide, then
  separate them"; 23:51Z "Keep that, Make Trinket, Pay only share"): 17 stock Cardinals on sale are Trinkets, held once
  by each buyer (Aura Gavel and the four companions included; a Coin pays one share); 12 stay Cardinals. Scope `12`,
  `docs/reports/STOCK_TRINKETS_2026-11-05.md` §7.

### Open questions

Phase 0's eight questions are closed (see Decisions). New ones go here, with a default.

| # | Question | Default if unanswered |
| --- | --- | --- |
| 1 | Scope sheets: the `[Questions]` blocks in `docs/scope/04`, `08`, `09` and `10` | the `~` default each line names |
| 2 | Spool: the defaults in its `DESIGN.md`, spelled out as S1-S17 in `docs/reports/SPOOL_DEFAULTS_2026-11-05.md`, answered 2026-11-05 ("Use all defaults"); (the Yarn Stash's upgrades, the Thread Head's and the Loom's levels 2 and 3 and the level-4 capstones are in scope `08` since 2026-11-04: designed, not built) | built without the new upgrades; every default stands |
| 3 | Spool's balance (`docs/reports/BALANCE_2026-11-03.md` §8): Dart Range 4.5, 6 Crafted a Tin, Stitched 3 and Dart 3.5 Damage, and a "wall with Pin Cushions first" line; then its §9 PC round. The aura baseline leaves them unchanged | numbers as written in scope `08`; nothing changes until he picks |
| 4 | Mining against the base vessels (`docs/reports/AURA_BASELINE_2026-11-03.md`): P5 Yossele's Sapling Attackspeed 0.25 → 0.15 (scope `07`), P6 its fruit Attackspeed × Damage × 15 → × 5 (his own number), P7 scope `08`'s Loom "Mining: like Smacky's Mining Hut" → what is built; then PC checks M8-M11 | numbers as written in scopes `07` and `08`; nothing changes until he picks |
| 5 | The defaults in scope `04`, `10`, `11`, `12`, `13`, `14` (`docs/reports/BUILD_2026-11-03.md` §5) | each `~value~` stands |
| 6 | Questions 3 and 4 in the light of the PC round (`docs/reports/BALANCE_ROUND_2026-11-03.md`) and its re-run (`docs/reports/BALANCE_RERUN_2026-11-03.md` §9): his own wall on Purlest and Woozy decides them. If it holds, P2 and P3 carry Spool (P1 only with Darts held back); if foes walk round it, no number buys more than a Tide | nothing changes |
| 7 | New Directions, Cardinals and Trinkets (`docs/reports/DIRECTIONS_CARDINALS_2026-11-04.md` §6, `docs/reports/ROGUELIKE_DIRECTIONS_CARDINALS_2026-11-04.md` §5): take, change or drop each; keep the research-choice pairs and Duos or not | each `~value~` stands; nothing is built until he picks |
| 8 | Argo's review (`docs/reports/ARGO_REVIEW_2026-11-04.md` §5): take, change or drop each of the fourteen changes in scope `09`; and if he takes P5 (question 4), the Drill Rig's Attackspeed 0.25 → 0.2 with it | each `~value~` stands; the next Argo build picks them up; the Drill stays at 0.25 unless P5 is taken |
| 9 | Uloe's 24 new upgrades (`docs/reports/ULOE_UPGRADES_2026-11-04.md` §3, scope `10` [Questions] 2-3): take, rename, change or drop each; Supernova a button or automatic; and if he takes P5 (question 4), the Flood Light's strike rate drops with the Sapling's | each `~value~` stands; nothing is built until he picks; the next Uloe build is an xhigh PC round |
| 10 | The scope-against-code audit (`docs/reports/SCOPE_CODE_AUDIT_2026-11-04.md` §2-3): for each of the 41 rows, scope or code yields | nothing changes until he picks; each row names my lean |
| 11 | The quality pass's seven (`docs/reports/QUALITY_PASSES_2026-11-04.md` "Left for Dimitri"): Cardinal stacking (6 working copies of a pack one, 1 of a stock one, against scope `12`'s no limit), scope `14`'s objective kinds, three scope wording points, Spool's Cookie Tin role, his scope copy | the build's caps stand; nothing changes until he picks |
| 12 | The code review's calls (`docs/reports/CODE_REVIEW_2026-11-04.md`): §5 B: ULO-3 (do two Uloes share Light?), MODE-C2, C3 and C5 (Spring Tide and Tidal Mend on the mode's own mods, not stock scripts), MODE-D5 (the map's extra foe health without the elite look), YOS-2 (Monument's Cleanse and a stock artifact's trade-off), MODE-D9 (lobby Cardinals in a campaign) | the low-risk batch can be built without them; these wait |
| 13 | All of the above, and every other call waiting on him, on one page (`docs/reports/OPEN_DECISIONS_2026-11-05.md`): 39 questions by theme | each line's `~default~`; nothing changes for a question he skips |

**2026-11-05: questions 1-13 above are answered** (2 at 05:10Z, `DECISIONS_APPLIED_2026-11-05.md` §4) (Decisions 2026-11-05, `docs/reports/DECISIONS_APPLIED_2026-11-05.md`); the rows stay for their history.

### Phase 5 pieces

All of these passed in game on 2026-11-03 (`docs/reports/BUILD_2026-11-03.md`) and with a real second player on one PC on 2026-11-04 (`docs/reports/TEST_ROUND_2026-11-04.md`); left on them: Dimitri's eye, Steam online.

- [x] kit 1.7 (Decisions 2026-10-15), before the button
- [x] main-menu Rogue Waves button
- [x] Pure Aura  - [x] objectives/bounties  - [x] shopkeeper  - [x] Directions  - [x] Fund Transfer  - [x] map vote
  (sea chart and boat trip passed PC rounds 1-4, 2026-10-11)
- [x] sea weather (PC round 4, 38/0; on a client 2026-11-04; the look by eye left)  - [x] rejoin after a drop (PC round 6, 47/0;
  O1-O8 with a second player 2026-11-04, `docs/mode/REJOIN.md` §6; Steam's Join Game left)
- [x] leave-or-stay and bonus tides
- [x] 10-map campaign loop (two maps played for real, a loss, a retry, Continue; ten real maps won on 2026-11-04, 44/0; Phase 5's gate, a full campaign by hand and on Steam, is left)
- [ ] ascension (Depth, on hold)
- [x] UX (Decisions 2026-11-03; `docs/reports/UX_FEATURES_2026-11-03.md`, probes U1-U10 in its §7; results in `docs/reports/BUILD_2026-11-03.md` §3):
  - [x] campaign card on the loading screen, the map intro card, the chart's world card and the end screen title
  - [x] retry warnings: the host's exit dialog names the cost; the end screen says how many retries are left
  - [x] "<name> has no mods on" warning on the boat; the rejoin line in the dropped player's main-menu error box
  - [x] weather icons above chart nodes on hover, and always over a weathered map
  - [x] the pack's tips in the game's tip lists (end screen, pause menu, Close Game)

---

## Part D. Prompt templates (copy, fill, send)

**Research question to a subagent (Explore or general-purpose):**
> Question: <one sentence>. Look in: <2-3 paths or a grep pattern>. Answer in under 250 words: the fact, then each
> claim tagged verified (seen in a mod) / read (from the game's code, with `file:line`) / untested. If the dump does
> not show it, say so; do not guess. No file dumps.

**Design review (before code):**
> Check `ModSource/<Name>/DESIGN.md` against `docs/02` section by section and `docs/12` if it has units. Report only
> violations and missing fields, one line each, with the section number. Under 150 words.

**Code module:**
> Write `<Name>/Code/<NN>_<Module>.cs` for <mechanic> per `DESIGN.md` § <n>, following `docs/05` § <k> and
> `examples/code/<nearest>.cs`. Host decides, engine syncs (`docs/07`). Then run the three `cloud-check` commands in
> `TDS_Mod/README.md` and report pass/fail only.

**Handoff for the PC:**
> Pack `make_handoff.py <Name>_round<N> --manifest <pc_manifest.txt> --only <Name>,Shared` (or `--since` the commit
> the PC last applied) with every fix since the last round (`TDS_Mod/Tools/pc/README.md` step 3), write the one-sentence
> request for the user (`TDS_Mod/Tools/pc/README.md` step 5), and list what the round must verify in
> `IMPLEMENTATION_NOTES.md`.

**Session close:**
> Update `docs/PLAN.md` Part C (Now: done / next / blocked; new decisions; new open questions). Commit, push.
