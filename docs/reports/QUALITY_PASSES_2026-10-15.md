# Quality passes until three clean (2026-10-15)

Dimitri asked (2026-10-15 05:18Z): "Do another check on every asset in the repository now. Continue until there are no
noticeable issues 3 times in a row." Base: `claude/phase-1-6lfnar` at f455c37 (PRs #2-#36 merged). Everything here
was read, compiled or run as a script in the cloud; nothing was run in game.

## How each pass works

- **Readers.** Ten readers per pass, one area each, every file in the area read again from the start: A1 kit
  `README`s and chapters 01-12; A2 chapters 13-18; A3 chapters 19-24, `reference/`, `examples/recipes/`; B1 the
  vessel template, `examples/`, kit `tools/`; B2 Argo and the shared kit (both copies); B3 Yossele; B4 RogueWavesMode,
  RogueLab, Riptide, `Handoff/`; C `TDS_Mod/Tools`, `TDS_Mod/README.md`, `concept-tools/`, `.claude/`, `.gitignore`;
  D `CLAUDE.md`, `README.md`, `docs/` pages, `docs/mode/`, `docs/reports/`, `docs/concepts/`; E `docs/scope/`.
- **Checks.** Each claim against the code, the game's API dump and data survey, the decompiled game, the round logs and
  the other living pages. Then every mod, define set, recipe folder and editor folder compiled with cloud-check (21
  builds), and a script checks every repository path and pitfall number named anywhere.
- **What counts.** A noticeable issue is one a reader or a tool would trip on: a wrong fact, a pointer that leads
  nowhere, stale state on a living page, two copies that differ, a code or script bug, a missing piece a rule requires.
  Wording taste, history in frozen reports and dated log lines, and items already decided or scheduled
  (`docs/PLAN.md` Part C Decisions 2026-10-15) do not count.
- **Fixes.** Applied when they change no game behaviour or balance, no kit rule and none of Dimitri's words or choices:
  docs, comments, tools, editor builders, the compile-only recipes. Anything else is listed below for Dimitri, with the
  default it gets. A pass is **clean** when it finds nothing beyond what it chose to leave with a stated reason.

## Pass 1: 179 issues

| Area | Found | Fixed by its reader | Fixed by the lead or another reader | Listed |
| --- | --- | --- | --- | --- |
| A1 kit 01-12 | 14 | 10 | 4 | 0 |
| A2 kit 13-18 | 23 | 21 | 2 | 0 |
| A3 kit 19-24, reference, recipes | 11 | 6 | 4 | 1 |
| B1 template, examples, tools | 13 | 6 | 5 | 2 |
| B2 Argo, shared kit | 9 | 8 | 0 | 1 |
| B3 Yossele | 33 | 23 | 2 | 8 |
| B4 mode, RogueLab, Riptide, handoffs | 13 | 10 | 0 | 3 |
| C tools, settings | 11 | 8 | 2 | 1 |
| D project docs | 36 | 30 | 6 | 0 |
| E scope | 16 | 7 | 9 | 0 |

**Heaviest finds** (all fixed):

- `TDS_Mod/Tools/pc/Apply-Handoff.ps1` could not be parsed: the 2026-10-15 edit put a block's closing `}` inside a
  comment, so the next PC round could not apply any handoff.
- `Run-SelfTests.ps1`'s `Launch` returned its warning strings with `$done`, so a timed-out self-test still counted as
  done (and `-BootCheck` passed on a timeout); the warnings now go to the host.
- The vessel template's builder sliced sheets with the texture's width after the first import (pitfall #102) and its
  Export did not park the other mods (pitfall #103); both now take Yossele's fixes (`PngSize`, `ParkOthers()`), and
  Argo's builder takes `PngSize` too. Spool and Uloe would have met both on their first build.
- Stale state across the Yossele pages (round 5, 91/0 → round 8, 98/0), the root `README.md` (18 kit chapters → 24),
  PLAN Part B's "draft PR #30", and every pointer still sending sea weather to scope `05`.
- Wrong facts in the kit: flyers do not path with `Getpathlinear` (`16` §2), a Speed between 1 and 2 plays as 1
  (`18` §1, §5.3; the art survey's header), the Ballista steps toward its target (`24` §3), the recipe's vote counted
  players who left and its load watchdog kept entries across maps.
- Scope: the Direction-name rule now follows Dimitri's "use defaults for all" (a name matches its Look line; a heading
  or a course is optional) in `00`, `06` and `15`; `02`'s state and log are current; repo references sit in `/~ \`
  notes; `16`'s Black Sno, Snoble Mother and Engineer numbers corrected from the game's code.
- `setup.sh` now unzips into a temporary folder (Decisions 2026-10-15 C23, due once PR #30 merged), and
  `new_vessel.py` adds missing kit files and warns on differing ones like `New-Vessel.ps1`.

**Listed for Dimitri** (code that changes behaviour in game or a choice; defaults in `docs/PLAN.md` Part C "Next"):

| # | What | Default |
| --- | --- | --- |
| B3-24 | Yossele's debuff looks still go through the throttled `Vfxdebuffcheck`, and the vines renew the slow look every 0.5 s per foe | Copy Argo 2.0.1's `ArgoUtil.Debuff`, renew the slow look only near its expiry; next Yossele build |
| B3-25, B3-26 | Yossele's display names are the pre-2026-10-13 ones (Selfish Sapling, Grafted Colors, Entangling Spaghetti Roots), and Selfish's in-game line has numbers | Rename strings, comments and tables to the scope's names and lines in the next build |
| B3-27, B3-30 | Yossele's footstep plays on the peak-lift frame; the Mend effect and three sounds are built but never played | Footstep on frames 0 and 3; play or drop the unused assets, next build |
| B3-28, B3-29 | Two small leaks in Yossele: `incoming` keeps a foe that died before its seeds landed; `LastHit` statics are never cleared per map | Fix in the next build |
| B3-31 | Connected Roots guards the whole team's buildings; the scope says "yours" | Ask at the next Yossele build: own or team |
| B4-11, B4-13 | Two weather lines in code differ from scope `04`'s in-game lines; the mode's dev-export description omits the rejoin | Take scope `04`'s lines at the next mode change; the description with the one pack version |
| B4-12 | `09_Rejoin.cs` keeps the first host's name for the "Lost the map" line all launch | Clear it on leaving a map, next mode change |
| A3-9 | The mode's save keys (`Campaign_Seed`, `Stock_World`, ...) lack the `RW_` prefix kit `23` §2 rule 3 asks for | Rename at the next mode change; no campaign has been saved outside the self-test, so no migration |
| B1-3 | The template never wires kit 1.3 `ModKitCombat` (kill credit) or kit 1.2 `ModKitTowers.Fit` | Add them with the 1.6 pack calls (B10), before Spool is scaffolded |
| B1-4 | `<Name>Aseprite.cs` names unit sheets so the unit branch skips them (logged, no `.aseprite` for units) in the template, Argo and Yossele | Fix with B10 |
| B2-X1, B2-X6 | Argo's DESIGN gives FLOOD "1/Quickaction", the code key 1 only; `GiveUpAfter` (12 s) is unused, the Naut gives up after 6 s | At Argo's balance pass |
| C10 | `.claude/settings.json` lacks `apt-get update` and `python3 -m pip install UnityPy`, which the docs tell sessions to run | Add them (permissions are yours) |

Left on purpose: `docs/YOSSELE_BUILD_NOTES.md` stays in `docs/` (a living page for the current Yossele build, kept
current in this pass); old Argo notes and the Handoff READMEs stay as history.
