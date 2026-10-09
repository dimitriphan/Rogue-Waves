# Rogue Waves: the prompt plan

The plan for building the expansion in `docs/SCOPE_v0.txt` (the user's scope, 2026-10-08, the highest truth after the
user's newest words). It has three parts: how the **coordinator** (the Claude session that runs this project) works so
it stays cheap and good, the **phases** with the prompt that opens each one, and the **state** every session reads first
and updates last. Rules about the game live in the kit; this page only says what to do next and how to do it cheaply.

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
| Threads on open-ended engine work: Phase 1 decompiler research (mode chain, save writes, client UI), Uloe's light-level feasibility, Phase 5's three-map loop and its online sync, a PC round whose self-test fails for an unnamed reason after one Opus pass | Fable 5.1 | New territory, no prior art in the kit |

Escalation rule: an Opus thread that fails twice at the same problem hands it to a Fable thread with a short summary
of what was tried. Prompting: Fable does better with the goal and the gate than with steps, so give a Fable thread the
outcome and the constraints; the Part D templates stay as written for Opus threads.

---

## Part B. Phases and their opening prompts

Each phase ends at a gate. A gate is a short written result the user can approve in a minute. Phases 1 and 2 are
cheap and decide the shape of everything after them, so nothing else starts before they close.

### Phase 0. Definitions (half a session)

Close the scope's blanks with the user before anything is researched. Reads: `docs/SCOPE_v0.txt` once, this section.

Opening prompt:
> Read `docs/SCOPE_v0.txt`. List every term it uses that the kit does not define and every item with no body
> (done 2026-10-08; see Part C Decisions). Write them to
> `docs/PLAN.md` Part C "Open questions" with the default you would take for each. Ask the user once, in one message.

Gate: every open question has an answer or an accepted default.

### Phase 1. Feasibility of the Rogue Wave Mode (2-3 sessions, research only)

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

### Phase 2. Pack foundation (1-2 sessions)

Reads: `docs/14` § 5 (the two pack layouts), `docs/15`, `docs/04` § 7, `kit/README.md`, `docs/03` § 1 and § 4,
`docs/EXPANSION_SCOPE.md` rows 15 and 18.

Opening prompt:
> Decide and write down (a) packaging: one `RogueWavesMod` or one mod per vessel plus one for the mode, load order,
> what the shared kit 1.6 must add for Rogue hooks (Growth, Light, Thread, Pure Aura, Direction application), and the
> version scheme; (b) a one-page style sheet `docs/STYLE.md`: the pack's palette, motifs, naming voice, which base
> vessel each new vessel's sprites follow, and the tower silhouette rule; (c) the roster's shape language, adding
> Yossele, Spool, Uloe to the taken list in `.claude/rules/design.md`. Bump the kit only if (a) needs it.

Gate: packaging and style approved; kit 1.6 scope listed.

### Phase 3. Vessels, one at a time (3-5 sessions each, plus PC rounds)

Order and reason: **Argo** first (designed from the scope, with shipped code and tests to reuse, so it is the cheapest
way to shake out kit 1.6), then **Yossele** (Growth is a new generic mechanic other pieces may reuse), then **Spool** (minion-heavy:
`docs/12`, and Mummer's Thread code is the nearest prior art), then **Uloe** (light levels are new engine territory:
check feasibility of "light received" as a stat before designing the rest).

Each vessel runs the kit's loop; the reading list is `CLAUDE.md`'s reading-order table, not more:

1. `DESIGN.md` from `template/DESIGN_TEMPLATE.md`, holding the scope's text as the concept and listing every change
   under "Changes from the concept, and why" (`docs/02`, `docs/12` for units; `examples/Mummer_DESIGN.md` for limits).
2. Concept art at the game's formats (`docs/03`), style sheet from Phase 2.
3. Scaffold (`new_vessel.py`), code module by module (`docs/05`), `cloud-check` clean in release and test defines.
4. Handoff and PC round (`TDS_Mod/Tools/pc/README.md`); self-test 0 failures; captures reviewed.
5. `IMPLEMENTATION_NOTES.md`, lessons to `docs/09`, release zip.

Opening prompt (per vessel):
> Build `<Vessel>` from its section of `docs/SCOPE_v0.txt` through the five steps in `docs/PLAN.md` Phase 3, one
> step per session unless a step is small. Start with step 1 and stop at the design gate.
> For Argo: design from the scope text. Then copy `ModProjects/TDS_Mod/ModSource/Argo` and `Assets/ArgoMod` in,
> read only its `IMPLEMENTATION_NOTES.md` and `02_Data.cs`, keep what matches the design and list the rest as changes.

Gate per vessel: the kit's definition of done (`TDS_Vessel_Creator/README.md`).

### Phase 4. Enemies and modifiers (1-2 sessions plus one PC round)

Reads: `docs/13`, `docs/15` § enemies. Cursed (an aura that lowers nearby towers' and units' attack) and Berserk
(damage and speed up as health drops) are the two modifiers; new enemy types come from the Phase 1 PC list.

Opening prompt:
> Implement `Enemy_RogueCursed` and `Enemy_RogueBerserk` as modifiers per `docs/13` § 6, with honest visuals and
> journal pages, and the elite skins the PC research returned. Then the new enemy types the mode needs, one each.

Gate: both modifiers seen in a self-test, 0 exceptions beside every installed mod.

### Phase 5. The Rogue Wave Mode (4-8 sessions, several PC rounds)

Build in the order a run plays, each piece testable alone: Pure Aura (per-player counter, earned per tide) →
objectives/bounties (reuse Merchut) → shopkeeper between tides → Directions (data, pick UI, application through
upgrades) → Fund Transfer → map vote → leave-or-stay at Tide 15 and bonus tides → the three-map loop with rising
difficulty → ascension unlock. Reads: `docs/15`, `docs/07` for every synced piece.

Opening prompt:
> Implement the next unchecked piece of the Phase 5 list in `docs/PLAN.md` Part C, host-authoritative per `docs/07`,
> with a self-test section and a journal page. Stop when it compiles and the handoff is packed.

Gate: a full three-map run completed on the PC, online once with a friend.

### Phase 6. Content passes (2-3 sessions)

New Cardinals (run modifiers, built like the base game's), the Direction catalogue (per vessel: stat bonuses and one "fundamental change" each), the streamlined
the Boutman F-key fix. Each is data plus small code on finished systems.

### Phase 7. Hardening and release (2 sessions plus PC rounds)

Roster balance (`docs/02` § 7 across all vessels and ascension levels), online checklist for every new sync, the pack
README, versioned zips, and `docs/10` hand-off. Gate: release.

---

## Part C. State (read first, update last)

### Now

- **Done:** Phases 1 and 2 (2026-10-08). **Phase 3 Argo** (2026-10-08): steps 1-5; PC round 6 **139 pass, 0 fail**,
  Deep Water bobs; the playable Argo 2.0 (v0.1.0-dev) was installed on the PC; lessons in kit `docs/09` #72-76. Work is
  merged into #3's branch (PR #7, PR #10). No release: Argo ships in the pack.
- **Also done:** `docs/SCOPE_v0.txt` (the user's roguewave.txt) rewritten in its own format with the current state;
  every item only the user can supply is marked `! NEEDS ... !` there (2026-10-08, branch
  `claude/roguewave-txt-update-aiqiql`). Copy at `/mnt/project-files/roguewave.txt`. The game's `TDS\Mods` folder
  was emptied at the user's ask (the pack will carry everything); the 7 mods sit in the PC's
  `ClaudeProjects\handoff\mods_removed_20261008`, so the playable Argo is no longer installed.
- **Also done (2026-10-09):** the research list `docs/RESEARCH_GAPS.md` answered from the decompiled game (kit
  `docs/15` §14) and RogueLab round 2 on the PC (15 pass, 1 fail; kit `docs/15` §15): 14 of 18 items answered.
  RogueLab 0.3.0 built for the rest (handoff `RogueLab_round3`, compile-checked).
- **Next:** RogueLab round 3 on the PC (15 untoured maps, the choice dialog with its menu up), then the F-key check by
  hand and one online evening; then the coordinator's next phase in Part B. Left on Argo: his balance pass (his call).
- **Blocked:** round 3 waits on Dimitri's OK for the PC session.
- **Also (2026-10-09):** a main-menu start button is feasible (`docs/MAIN_MENU_START.md`, kit `docs/15` §14.8); Dimitri
  adopted it; its probes M1-M7 join the next RogueLab round.
- **Also (2026-10-09 evening):** Dimitri's roguewaveWIP.txt answers folded into `docs/SCOPE_v0.txt` (PR #15: VENDCINT,
  Fund Transfer limits, vote rules, vessel abilities, Crafted types, Aging and Dazzling, an optional-objective
  template). Every new mechanic checked (`docs/MECHANICS_CHECK.md`, kit `docs/15` §16): all go; eight need the game.
  RogueLab 0.4.0 (round 4, compiled here) runs them with M1-M5 and M7 in one solo launch; handoff `RogueLab_round4`.
  **Next:** round 4 on the PC once Dimitri sends the OK sentence.

### Decisions

- 2026-10-08: vessels arrive one at a time through the kit loop; Argo first. Mode feasibility (Phase 1) closes before
  any vessel code.
- 2026-10-08: Cardinals, bounties and the shopkeeper are built on the base game's own systems where Phase 1 finds
  them usable, never reinvented.
- 2026-10-08 (user): **Cardinals are the base game's run-modifier effects.** A new Cardinal is a new run modifier.
  **Score does not count** in the Rogue Wave mode, so leaderboards and the Cardinal score multiplier are out of scope.
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
- 2026-10-08 (defaults taken): three Directions offered and one picked per map, extras bought at the shop; ascension
  persists across sessions if Phase 1 finds save writes safe, else per session; online is 2-4 players,
  host-authoritative, every player with the whole pack; the F-key respawn fix covers the new vessels, and Boutman if
  it is the same code path.
- 2026-10-08 (Phase 1, default): the mode is the stock `Mode_Normal` loop (named by the mod's `Modeobject`) plus a
  host brain in the pack's code; a mode class of our own is impossible (`docs/15` §2).
- 2026-10-08 (Phase 1, default): run state lives on the host's `DontDestroyOnLoad` object; Pure Aura shows as a counter
  on each player's vessel; ascension is saved in `Bla_Data` folder `RogueWaves` (save writes are safe, `docs/15` §8).
- 2026-10-08 (Phase 1, default): the F-key fix is a client-side reorder of the `Hero` tag list, for Boutman and every
  pack vessel; minions keep the `Hero` tag (`docs/15` §9).
- 2026-10-08 (user, Phase 1 gate): **every map is voted on, map 1 included**, so map 1 opens with a staging step (the
  leader's pick loads, the opening break is held for Direction picks and the vote, then the host chains to the voted
  map, `docs/15` §6). The start rule (the host equips the mode's Cardinal) was **replaced 2026-10-09** (below); it was
  never named, since "Rogue Wave" is the expansion's name (user). RogueLab tests the colour slot, which checks the name; the Mixed
  slot takes any string. **The §11 PC round is approved.**
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

- 2026-10-09 (user): **a run is a campaign of 15 maps, saved between maps** (was 3 maps in one sitting); the host's
  save holds it, keyed per player by Steam id (`docs/SCOPE_VS_LOOP.md` §E). **Swell (ascension) is on hold** until the
  core loop is settled. Per-player Cardinals researched (§F): most stock ones can be made personal. **Directions rank
  up** (II, III) when offered again to a player who holds them, so the catalogue stays about 10 Any and 6 per vessel. **A lost map spends one of a few retries** (3 by
  default) and replays from the last save; quitting mid-map counts as a loss; with none left the campaign ends.
- 2026-10-09 (user): **a campaign starts from a Rogue Wave button on the main menu** (New / Continue), which takes the
  host to the boat with the mode set; friends join there by invite. No mode Cardinal (`docs/MAIN_MENU_START.md`).
- 2026-10-09 (user, roguewaveWIP.txt): shopkeeper **VENDCINT** drops in near the vessels' spawn each break and leaves at
  its end; sells Cardinals and **only the next rank of Directions a vessel holds**. **Fund Transfer: base aura only**,
  50 x tide per break (default: the coming tide, so 50 before Tide 1). Pure Aura 3 a tide (default: 4 a bonus tide).
  Leave by **majority**; **bonus tides never end** until that vote; a failed campaign keeps nothing; 3 retries; every
  base map in the vote; base vessels get Directions; modifiers show as an effect. 5 Swell levels (still on hold).
  Model Lighthouse kept as a name; the main-menu button stays the start rule unless Dimitri says otherwise.

### Open questions

Phase 0's eight questions are closed (see Decisions). New ones go here, with a default.

| # | Question | Default if unanswered |
| --- | --- | --- |
| - | none open | |

### Phase 5 pieces

- [ ] Pure Aura  - [ ] objectives/bounties  - [ ] shopkeeper  - [ ] Directions  - [ ] Fund Transfer  - [ ] map vote
- [ ] leave-or-stay and bonus tides  - [ ] three-map loop  - [ ] ascension

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
