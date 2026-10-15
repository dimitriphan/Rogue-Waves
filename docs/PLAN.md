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
> stay under 150 words: what changed, what is blocked, what you need. Never paste file contents or restate a file.
>
> **One thing at a time.** One vessel, one system, one gate. Design before code (`DESIGN.md`, checked against
> `docs/02` by its design script), `cloud-check` before any handoff, a handoff only when it is worth the user's PC
> time, and batched: several fixes per round, never one. Compile is not a test; say which it was.
>
> **The user's scope.** `docs/scope/` is the scope as one `.txt` per category (`00_Read_Me.txt` lists them), and
> `/mnt/project-files/scope/` is his copy; keep the two equal. He sends back only the files he edited. An upload
> arrives without its name: its first line (`| Directions |`, `| New Vessel: Yossele |`) says which file it is. Fold in
> just that file: diff it against the repo copy, keep his wording, put each `< answer >` into the line above it in his
> format, and update both copies. Touch another scope file only where his edit contradicts it, and name that file in
> the reply.
>
> **Ask only at gates.** Questions for the user are collected in Part C "Open questions" and asked together when a
> gate is reached or the work cannot continue without them. Defaults you can take yourself are taken and logged.
>
> **End of every session.** Update Part C "Now" (done / next / blocked, three lines), add decisions and open
> questions, commit with a message under five lines, push to the working branch.

### Model per role

Fable 5.1 costs 2.5x Opus 5.5 per token (same 1M context); its edge is judgment on open-ended problems. The plan
puts the judgment in the coordinator and the volume in the threads, so the models follow that split.

| Role | Model | Why |
| --- | --- | --- |
| Coordinator at a gate (Phase 1 go/fallback, packaging, design gates, definition of done) | Fable 5.1 | Few tokens, costly if wrong |
| Coordinator on routine sessions (session close, packing a handoff, Phase 6 data passes) | Opus 5.5 | Routine |
| Threads by default (Part D research, code modules, sprite generators, cloud-check loops) | Opus 5.5 | The kit makes this work routine |
| Threads on open-ended engine work: Phase 1 decompiler research (mode chain, save writes, client UI), Phase 5's 10-map campaign loop and its online sync, a PC round whose self-test fails for an unnamed reason after one Opus pass | Fable 5.1 | New territory, no prior art in the kit |

Escalation rule: an Opus thread that fails twice at the same problem hands it to a Fable thread with a short summary
of what was tried. Prompting: Fable does better with the goal and the gate than with steps, so give a Fable thread the
outcome and the constraints; the Part D templates stay as written for Opus threads.

---

## Part B. Phases and their opening prompts

Each phase ends at a gate. A gate is a short written result the user can approve in a minute. Phases 1 and 2 are
cheap and decide the shape of everything after them, so nothing else starts before they close.

### Phase 0. Definitions (half a session)

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

### Phase 3. Vessels, one at a time (3-5 sessions each, plus PC rounds; Argo done 2026-10-08, Yossele in draft PR #30)

Order and reason: **Argo** first (designed from the scope, with shipped code and tests to reuse, so it is the cheapest
way to shake out kit 1.6), then **Yossele** (Growth is a new generic mechanic other pieces may reuse), then **Spool** (minion-heavy:
`docs/12`, and Mummer's Thread code is the nearest prior art), then **Uloe** (light levels are new engine territory:
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
> For Argo: design from the scope text. Then copy `ModProjects/TDS_Mod/ModSource/Argo` and `Assets/ArgoMod` in,
> read only its `IMPLEMENTATION_NOTES.md` and `02_Data.cs`, keep what matches the design and list the rest as changes.

Gate per vessel: the kit's definition of done (`TDS_Vessel_Creator/README.md`).

### Phase 4. Enemies and modifiers (1-2 sessions plus one PC round)

Reads: `docs/13`, `docs/15` §1, `docs/18` (foe art). The modifiers are the scope's four (`docs/scope/13`): Cursed (an
aura that lowers nearby towers' and units' attack), Berserk (damage and speed up as health drops), Aging and Dazzling;
they show as an effect on the foe, not a new skin. New enemy types: crystal foes per `docs/18` §5.6, when the scope
names them.

Opening prompt:
> Implement the scope's four modifiers (`docs/scope/13`) per `docs/13` § 6, each shown as an effect on the foe (no skins;
> colours per `docs/18` §5.5 and §7), with journal pages. Then the new enemy types the mode needs, one each.

Gate: the four modifiers seen in a self-test, 0 exceptions with the whole pack loaded.

### Phase 5. The Rogue Waves Mode (4-8 sessions, several PC rounds)

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

New Cardinals (run modifiers, built like the base game's), the Direction catalogue (per vessel: stat bonuses and one "fundamental change" each), and the
Boutman F-key fix's by-hand check (the fix is built: `ModKitFocus`, kit 1.6). Each is data plus small code on finished systems.

### Phase 7. Hardening and release (2 sessions plus PC rounds)

Roster balance (`docs/02` § 7 across all vessels and ascension levels), online checklist for every new sync, the pack
README, versioned zips, and `docs/10` hand-off. Gate: release.

---

## Part C. State (read first, update last)

### Now

- **Done:** Phases 1-2, Phase 3 Argo, and the mode part `RogueWavesMode` 0.2.1 (sea chart, sea weather, rejoin; solo PC
  rounds passed; full re-test 2026-10-11, 208/0). Yossele 0.1.0-dev is built (PC round 8, 98/0, Dimitri's play notes
  folded in); its sheet now asks for a ranged laser and a slower speed. Kit chapters 17 (base vessels; RogueLab
  round 7 checked it in game, `docs/reports/BASE_VESSEL_CHECKS_2026-10-12.md`) and 18 (character art); the Vessel
  Spotlight pages (Log). 2026-10-14: every scope file follows one rule set (`docs/scope/00_Read_Me.txt`), with every
  template in `15_Templates.txt`; a full decompile gave kit chapters 19-24 and pitfalls #93-101. 2026-10-15:
  RogueWavesMode 0.3.0 and Argo 2.0.1 fix the four problems it found (compiled, not run; Log); two quality passes and
  a reorganisation (`docs/reports/`, `docs/mode/`; `docs/reports/QUALITY_CHECK_2026-10-15.md`). Older entries are in
  "Log" below.
- **Next:** Phase 5's next pieces, the main-menu button and the in-map vote (kit 1.7 first, Decisions 2026-10-15).
  By hand: the F-key check, the button's and the chart's look, a flooded map; one online evening
  (`docs/RESEARCH_GAPS.md` §E). Left on Argo: his balance pass. Yossele's next build takes the ranged laser and speed;
  also left on Yossele: his look by eye (temporary art), his balance pass, the Compost Bin numbers in scope `11`, and
  the chance-based Viral Outbreak and Hallucinogen, still unchecked.
- **Blocked:** nothing; the research left needs Dimitri at the keyboard or a friend online.
- **Next free numbers:** kit chapter 25, pitfall #105 (#92 is unused).

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

### Decisions

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
  balance pass waits for a later session of his; step 5 is lessons and notes, no release zip.

- 2026-10-09 (research): **Directions are held per player on the vessel, not on the core** (the core is the team's,
  `docs/15` §14.1); `ModKitDirections` changes to that as kit 1.7 when Phase 5 starts. Per-player run state is keyed
  by `Sessionid`. The base game scales nothing by player count or map, so the mode's difficulty is its own (§14.3).

- 2026-10-09 (user; superseded below: 10 maps, Depth, about 7 Any and 4 per new vessel, only the host's quit spends
  a retry): **a run is a campaign of 15 maps, saved between maps** (was 3 maps in one sitting); the host's
  save holds it, keyed per player by Steam id (`docs/reports/SCOPE_VS_LOOP.md` §E). **Swell (ascension) is on hold** until the
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

- 2026-10-10 (user): **sea weather is in the scope** (`docs/scope/05` [Sea Weather], `docs/mode/SEA_CHART.md` §8).
  Defaults taken: three weathers (Rising Water, Quickening Current, Nesting Ground); rows 1, 5, 10 calm, 1 in 3 then 1
  in 2 elsewhere; +1 Pure Aura a Tide; Quickening x1.25 from Tide 5, x1.5 from Tide 10. Maps spread by row difficulty
  (aim 2 + (row-1)/2, Breaker rows +2), none twice. The mode part is `RogueWavesMode`; nothing starts a campaign
  until the main-menu button piece calls `RWCampaign.New`/`Continue`.

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
  PC folder `Desktop\RogueWave` keep their old spelling, so saves and the scope's file names stay valid.
- 2026-10-13 (user): every vessel sheet follows `docs/scope/15_Vessel_Template.txt`; `~` marks a Claude default.
  Yossele attacks at range (a green laser from the mouth) and moves slower; Selfish Sapling is now Forest Project and
  Grafted Colors is Grafted, Entangling Spaghetti Roots is Entangling Roots. Spool's ability is **Patch** (20 Thread
  heals 20%), Unthread is just selling, and a **Yarn Stash** (placed at the start, like the Bouthouse) holds the Thread.
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
    temporary folder after PR #30 merges (C23); the API dump gets a header line at the next dump (A19); old Argo notes
    stay as history (A17). Done in PR #36: `gh` before the token (C6), BootCheck's `icons:` line (C5), PS 5.1 patch
    lists (C10), the log read race (C2-3), the allowlist (C19).
  - Scope: PR #32's thread fixes scope 16 (Knight Commander, Diesel Dog, Rev Dog, Battalion, Black Sno) and the
    wording items D20-D22, D26, D27; after #32 merges, the repo's `docs/scope/` is copied over the project files (D1).
  - Directions: names match the Look line; the heading or course style is optional (`SCOPE_REVIEW_2026-10-10` #37).
  - The GitHub default branch becomes `claude/phase-1-6lfnar`; only Dimitri can switch it (GitHub settings).

### Open questions

Phase 0's eight questions are closed (see Decisions). New ones go here, with a default.

| # | Question | Default if unanswered |
| --- | --- | --- |
| 1 | Scope sheets: the `[Questions]` blocks in `docs/scope/04`, `07`, `08`, `09` and `10` | the `~` default each line names |

### Phase 5 pieces

- [ ] kit 1.7 (Decisions 2026-10-15), before the button
- [ ] main-menu Rogue Waves button (calls `RWCampaign.New`/`Continue`; nothing starts a campaign until it exists)
- [ ] Pure Aura  - [ ] objectives/bounties  - [ ] shopkeeper  - [ ] Directions  - [ ] Fund Transfer  - [ ] map vote
  (sea chart and boat trip built and passed PC rounds 1-4, 2026-10-11; the in-map vote still to build)
- [x] sea weather (PC round 4, 38/0; the look by eye and online left)  - [x] rejoin after a drop (PC round 6, 47/0;
  online O1-O8 left)
- [ ] leave-or-stay and bonus tides  - [ ] 10-map campaign loop  - [ ] ascension (Depth, on hold)

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
> Pack `make_handoff.py <Name> --only <Name>,Shared` with every fix since the last round, write the one-sentence
> request for the user (`TDS_Mod/Tools/pc/README.md` step 5), and list what the round must verify in
> `IMPLEMENTATION_NOTES.md`.

**Session close:**
> Update `docs/PLAN.md` Part C (Now: done / next / blocked; new decisions; new open questions). Commit, push.
