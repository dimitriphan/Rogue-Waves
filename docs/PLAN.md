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
> last), run the cloud-check of every changed part and `check_rules.py` (docs-only: check the cited paths), and leave a PR whose own thread
> is still working on it. **Review (Dimitri, 2026-11-06: "use Chat GPT to review pull requests so that you aren't
> using Claude to review Claude"):** Codex, from his ChatGPT plan, reviews every PR against `AGENTS.md` "Code Review
> Rules"; no Claude reviewer sub-agent while Codex answers. The thread answers each Codex comment (fix, or say why not) and merges once
> Codex has reviewed the head with no P0/P1 left open; Codex comments but never approves, so a clean review (its
> 👍 on the PR) is the approval. **Fallback (Dimitri, 2026-11-07, for when his ChatGPT usage runs out):** no
> review within an hour: comment `@codex review` once; still none an hour later, or Codex answers that its usage
> limit is reached: tell Dimitri in the thread, and a Claude reviewer sub-agent (model: `docs/PROJECT_SETUP.md`
> "Model per role", stand-in PR reviewer) runs `mattpocock-skills:code-review` on the head, labelling findings P0-P3 under the same rules; the
> thread fixes or answers each P0/P1, the same reviewer re-reviews the new head (no new Codex wait), the PR body notes
> that Claude reviewed in Codex's place, and the thread merges once no P0/P1 is open and cloud-check is clean. After merging, message the threads of the PRs still open to re-merge the base. Pushing
> straight to the base stays denied, and his OK is still needed for anything that touches his PC.
>
> **End of every session.** Edit Part C "Now"'s State / Next / open lines in place (no dated entries there, so "Now"
> stays under about 4 KB) and put what was done in the Log as one new file (`docs/log/README.md`), add decisions and open
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

- **State (2026-11-12, base `0ef182b`):** Phases 1-3 done; Phase 4's foes and Phase 5's mode built and run in game.
  Parts: Argo 2.0.1, Yossele 0.2.0-dev, Spool 0.1.0-dev, Uloe 0.1.0-dev (ChatGPT's art since 2026-11-07, Argo
  redesigned; `docs/TEMP_ART.md`), `RogueWavesMode` 0.1.0-dev (main-menu button to end screen, sea chart, MVP board), the foes part;
  kit 1.15. Last PC run: Flow_round1 (`PC_FLOW_ROUND1_2026-11-12.md`). Before it, Art_round1 on `d8cddbd` (`PC_ART_ROUND1_2026-11-07.md`: the new art in every build, vessel
  self-tests pass, the Thread Heads' fix #165 proven with the whole pack; the mode's flow failed 121/2: the game's gamepad
  mode, pitfall #177; fixed by #196, proven 2026-11-12 in `PC_FLOW_ROUND1_2026-11-12.md`, mouse 190/0, gamepad 190/0). Before it: rounds 5 and 6 and the full test
  (`PC_ROUND5_2026-11-06.md`, `PC_ROUND6_2026-11-06.md`, `PC_FULL_2026-11-06.md`). Built since, compiled here, not run
  on the PC: ChatGPT's full Argo set (#179), the chart verdict (Tab, overlaps). ChatGPT reset 2026-11-08 (`docs/art/reset_2026-11-08/`), Spool's new
  sheets not in yet; Spool keeps its claws, Argo's Look is the rig worker and its five edits wait on ChatGPT
  (`docs/art/ARGO_EDITS_2026-11-12.md`). The Run Batch button for Claude Code (#188, #189). The sweep's tuned numbers in scope `04`, `08`,
  `10`, `12` keep their tildes until he takes them; his project-files scope copy and the Scope Editor (version 14) equal `1c236a7`
  (08's and 09's Look lines changed after, #193); his PC's copy waits for a PC round. Dated history:
  `docs/log/`; reports: `docs/reports/README.md`.
- **Next, in order:** a PC round with #179's Argo art, and what rounds 5 and 6 left (`PC_ROUND5_2026-11-06.md`
  §7, `PC_ROUND6_2026-11-06.md` §6: COLOUR's panel, the elite stand-in, Spool's by-hand checks) and the full test's
  leftovers (`PC_FULL_2026-11-06.md` §6); the weather look's own round (`WEATHER_LOOK_2026-11-05.md` §4 items 2 and
  4-7); the map cards' freeze, which waits in Unity's render thread (round 6 §2).
- **Still open for his eye or word:** the code review's bugs left for his word, a PC fact or a sighting
  (`CODE_REVIEW_RECHECK_2026-11-05.md`, `CODE_REVIEW_MAX_2026-11-05.md` §5.1); by hand: the F key on a real Boutman death
  (P1-1), the icons, Spool's sage green beside a Hardened foe and on grass, the picked temporary art and minions, the
  button's and chart's look, a flooded map, the chart, VENDCINT, Uloe and the campaign (`BUILD_2026-11-03.md` §6), the
  polish's readings (`MODE_POLISH_2026-11-03.md` §2).
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
- **Next free numbers:** kit chapter 25, kit 1.16, pitfall #178 (which PR used which pitfall numbers: `grep -rn "pitfall #" docs/log`).

### Log

One file per PR in `docs/log/` (`docs/log/README.md`); history before 2026-11-13 in `docs/log/2026-10-08_archive.md`. Search it; never read the folder whole.

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
