# Token audit, 2026-11-12

Dimitri: "I feel like using the project chat uses tokens extremely quickly. Is there anything that can be removed,
archived, or changed that will greatly reduce the amount of tokens used?" Sizes measured on base `0ef182b`; tokens
estimated at about 4 bytes each.

## What every session pays before it starts

| Item | Size | Tokens | Who pays |
| --- | --- | --- | --- |
| Part C "Now" (`docs/PLAN.md`) | 23.3 KB, now 3.8 KB | ~5,800, now ~950 | every thread, every PR edit |
| Project memory index `MEMORY.md` | 13.3 KB | ~3,300 | every session and the coordinator |
| Shared folder listing (`/mnt/project-files`, 2,389 entries, cut by the host) | ~16 KB shown | ~4,000 | every session |
| Account plugins (superpowers hook, engineering, desktop-commander, handoff, cowork tools and skills) | | ~4,000-6,000 | every session |
| `CLAUDE.md` | 6.7 KB | ~1,700 | every session |
| Part A (`docs/PLAN.md`) | 6.1 KB | ~1,500 | coordinator |
| Recalled memory files (91 files, 199 KB; 2-3 recalled per turn) | 2-6 KB | ~500-1,500 | per turn |

## Ranked findings

1. **The coordinator's own long context (biggest, not measurable here).** The coordinator is woken for every message in
   every thread and on 1M context rarely compacts, so each wake re-reads a huge history. Cheaper: start a fresh
   coordinator (or `/compact` it) every day or two; set its effort to low or medium.
2. **Open threads that are done.** 17 of the latest 50 threads are unresolved though idle (see the thread reply);
   resolving them stops their late events waking the coordinator. Free and reversible.
3. **Part C "Now" regrew from 4 KB to 23 KB** because each PR added its 3-line entry and nobody folded them. Fixed in
   this PR (dated entries moved verbatim to the Log). To stop it regrowing: the ship-a-pr skill should say "put your
   entry in the Log only; edit Now's State/Next lines in place".
4. **Account plugins.** superpowers' SessionStart hook injects ~1,500 tokens and tells every session to load skills
   before anything (each load is thousands more); CLAUDE.md already says its skills don't fit. engineering's seven
   connectors fail every session. Turning off superpowers, engineering, handoff and cowork-plugin-management saves
   ~5k tokens per session plus the skill loads.
5. **Project memory.** 13.3 KB index, mostly finished history already in the Log. Proposed slim version 2.6 KB (saves
   ~2,700 tokens per session and per coordinator handoff), files in `/mnt/project-files/token_audit/` (not in the
   repo), applied only at his word.
6. **Shared folder listing.** `concepts/` alone holds 1,958 of 2,389 entries; the host cuts the listing, so zipping
   `concepts/`, `artifacts_archive/` and `chatgpt/2026-11-06/` saves maybe 1-3k tokens a session. Needs his word.
7. **Process rules.** Codex review costs Claude only the wakes on PR events; the Claude failsafe reviewer is the dear
   part (one Opus sub-agent per review and re-review). Status-widget calls are small (~200 tokens each). The morning
   routine starts a full session daily; Haiku batch costs API credit, not plan tokens.
8. **Old branches (138 on origin): no token saving.** Sessions fetch only the branch they need and never list branches;
   deleting them is housekeeping only.
9. **Repository restructure: little saving.** Sessions read sections, not whole files; the 224 KB `PLAN.md` costs only
   when a session greps or reads beyond "Now". Splitting the Log into `docs/plan/LOG.md` would stop accidental full
   reads; optional.
