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

---

## Part B. Phases and their opening prompts

Each phase ends at a gate. A gate is a short written result the user can approve in a minute. Phases 1 and 2 are
cheap and decide the shape of everything after them, so nothing else starts before they close.

### Phase 0. Definitions (half a session)

Close the scope's blanks with the user before anything is researched. Reads: `docs/SCOPE_v0.txt` once, this section.

Opening prompt:
> Read `docs/SCOPE_v0.txt`. List every term it uses that the kit does not define and every item with no body
> (candidates: Cardinals, Streamlined Economy Changes, Uloe's unnamed Defense building, which Argo is the baseline for
> the rework, how many Directions a run offers, whether ascension persists across sessions). Write them to
> `docs/PLAN.md` Part C "Open questions" with the default you would take for each. Ask the user once, in one message.

Gate: every open question has an answer or an accepted default.

### Phase 1. Feasibility of the Rogue Wave Mode (2-3 sessions, research only)

The mode is the riskiest piece and decides whether the expansion is one mod, several, or a mode-less vessel pack. The
kit has already read the mode chain (`docs/13` § 3) and the API dump names the pieces the scope needs: `Mode_Base`,
`Modeobject`, `Gameplandatabaseobject`, `Artifactobject` and `Game_Artifacts`, `Map_Cardinalconditions`
(Cardinals are a base-game system), `Entity_Ai_Tower_Merchut.Activatebounty` (a base-game shopkeeper with bounties),
`Bl_Save`, `Unlock_*`, `Entity_Ai_Main_Core.Addplayer.Respawn` (the F-key bug). Reads: `docs/13` § 3 and § 11,
`docs/11`, `docs/01` § 1 and § 11.

Opening prompt:
> Answer, with the decompiler setup in `docs/13` § 11 and subagents per Part D, these questions and write
> `TDS_Vessel_Creator/docs/14_ROGUE_MODE_ENGINE.md` in the kit's style (what is read vs untested):
> 1. Can a mod add a mode entry and a server mode script (`Mode_Base` subclass or a `Mode_Normal` wrapper), and have the
>    host carry state (Pure Aura per player, chosen Directions, ascension) across three consecutive maps? What is the
>    cheapest fallback if not (three matches with state kept by the host mod, or in the save)?
> 2. How does the base game's shopkeeper (`Merchut`, bounties) and its Cardinal/Artifact system work, and can a mod add
>    shop items, bounties, Cardinals and Artifacts the same way the base game does?
> 3. How are choices shown on every client (Direction pick, map vote, "leave or stay" between tides): is there a
>    menu or command UI a mod can reuse (`Entity_Command_Itemselect_Base`, `Bl_Menus`), with the host as the authority?
> 4. What may a mod write to the save without breaking it (`Bl_Save`, `Unlock_*`), for ascension and unlocks?
> 5. Enemy modifiers: close `docs/13` § 10 questions 1, 2 and 4 from the code; list what needs the PC.
> 6. The F-key respawn bug: where selection goes after a vessel respawn, and whether a mod can fix it for Boutman and
>    for its own minion vessels.
> End with a go / fallback / no table per scope feature and a PC research list (one build, one launch).

Gate: the user picks go / fallback per feature. This fixes the mode's design and the packaging.

### Phase 2. Pack foundation (1-2 sessions)

Reads: `docs/14`, `docs/04` § 7, `kit/README.md`, `docs/03` § 1 and § 4, `docs/EXPANSION_SCOPE.md` rows 15 and 18.

Opening prompt:
> Decide and write down (a) packaging: one `RogueWavesMod` or one mod per vessel plus one for the mode, load order,
> what the shared kit 1.6 must add for Rogue hooks (Growth, Light, Thread, Pure Aura, Direction application), and the
> version scheme; (b) a one-page style sheet `docs/STYLE.md`: the pack's palette, motifs, naming voice, which base
> vessel each new vessel's sprites follow, and the tower silhouette rule; (c) the roster's shape language, adding
> Yossele, Spool, Uloe to the taken list in `.claude/rules/design.md`. Bump the kit only if (a) needs it.

Gate: packaging and style approved; kit 1.6 scope listed.

### Phase 3. Vessels, one at a time (3-5 sessions each, plus PC rounds)

Order and reason: **Argo** rework first (code and tests exist in `ModProjects`; the rework is the cheapest way to shake
out kit 1.6), then **Yossele** (Growth is a new generic mechanic other pieces may reuse), then **Spool** (minion-heavy:
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
> For Argo: copy `ModProjects/TDS_Mod/ModSource/Argo` and `Assets/ArgoMod` in first, read only its
> `IMPLEMENTATION_NOTES.md` and `02_Data.cs`, then diff the scope against them.

Gate per vessel: the kit's definition of done (`TDS_Vessel_Creator/README.md`).

### Phase 4. Enemies and modifiers (1-2 sessions plus one PC round)

Reads: `docs/13`, `docs/14` § enemies. Cursed (an aura that lowers nearby towers' and units' attack) and Berserk
(damage and speed up as health drops) are the two modifiers; new enemy types come from the Phase 1 PC list.

Opening prompt:
> Implement `Enemy_RogueCursed` and `Enemy_RogueBerserk` as modifiers per `docs/13` § 6, with honest visuals and
> journal pages, and the elite skins the PC research returned. Then the new enemy types the mode needs, one each.

Gate: both modifiers seen in a self-test, 0 exceptions beside every installed mod.

### Phase 5. The Rogue Wave Mode (4-8 sessions, several PC rounds)

Build in the order a run plays, each piece testable alone: Pure Aura (per-player counter, earned per tide) →
objectives/bounties (reuse Merchut) → shopkeeper between tides → Directions (data, pick UI, application through
upgrades) → Fund Transfer → map vote → leave-or-stay at Tide 15 and bonus tides → the three-map loop with rising
difficulty → ascension unlock. Reads: `docs/14`, `docs/07` for every synced piece.

Opening prompt:
> Implement the next unchecked piece of the Phase 5 list in `docs/PLAN.md` Part C, host-authoritative per `docs/07`,
> with a self-test section and a journal page. Stop when it compiles and the handoff is packed.

Gate: a full three-map run completed on the PC, online once with a friend.

### Phase 6. Content passes (2-3 sessions)

New Cardinals, the Direction catalogue (per vessel: stat bonuses and one "fundamental change" each), the streamlined
economy changes once defined, the Boutman F-key fix. Each is data plus small code on finished systems.

### Phase 7. Hardening and release (2 sessions plus PC rounds)

Roster balance (`docs/02` § 7 across all vessels and ascension levels), online checklist for every new sync, the pack
README, versioned zips, and `docs/10` hand-off. Gate: release.

---

## Part C. State (read first, update last)

### Now

- **Done:** repository set up from `ModProjects` (2026-10-06); scope v0 and this plan written (2026-10-08).
- **Next:** Phase 0, the open questions below, in one message to the user.
- **Blocked:** nothing.

### Decisions

- 2026-10-08: vessels arrive one at a time through the kit loop; Argo first. Mode feasibility (Phase 1) closes before
  any vessel code.
- 2026-10-08: Cardinals, bounties and the shopkeeper are built on the base game's own systems where Phase 1 finds
  them usable, never reinvented.

### Open questions (Phase 0)

| # | Question | Default if unanswered |
| --- | --- | --- |
| 1 | What is a **Cardinal** in the base game as you play it (the code has `Map_Cardinalconditions` and a leaderboard multiplier for them)? What should a *new* Cardinal be: a run condition, an artifact, or a reward? | A run condition with a score multiplier, like the base game's |
| 2 | **Streamlined Economy Changes** has no body. What changes? | Deferred to Phase 6, no design until written |
| 3 | Uloe's **Defense** building has no name. | Coordinator proposes three names at Uloe's design gate |
| 4 | **Argo** baseline: rework from the shipped Argo 1.4.0 in `ModProjects`, or redesign from the scope text alone? | Rework from 1.4.0; the scope's changes listed as "Changes from the concept" |
| 5 | How many **Directions** are offered per pick, and how many picks per run (one per map, plus shop)? | Three offered, one picked, per map; extra ones bought |
| 6 | Does **ascension** persist across game sessions (save writes) or only across runs in one session? | Persist, if Phase 1 finds save writes safe; else per session |
| 7 | **Online** target: 2 to 4 players, host-authoritative, every player with the whole pack? | Yes |
| 8 | Is the Boutman **F-key bug** in scope for the pack (a fix to a base vessel), or only for the new vessels? | Fix for new vessels; Boutman if the same code path |

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
