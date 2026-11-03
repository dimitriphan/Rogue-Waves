# Quality passes (2026-10-15)

> **Note (added 2026-11-03):** the mode rows A3-9, B4-11, B4-12, B4-13, B4-14 and C2 are fixed in RogueWavesMode
> 0.1.0-dev (`MODE_POLISH_2026-11-03.md` §1).

Dimitri asked (2026-10-15 05:18Z): "Do another check on every asset in the repository now. Continue until there are no
noticeable issues 3 times in a row." At 13:23Z he cut it to two clean passes in a row, and on 2026-10-16 at 00:17Z
he asked to stop after pass 12 and say why every pass finds issues ("Why every pass found issues" below). At 00:31Z
he chose to go on counting only wrong facts, and at 15:15Z he stopped the run "after pass 22"; pass 23 had already
finished, and its two fixes are kept. Pass 20 was the one clean pass after that; two in a row never came. Base:
`claude/phase-1-6lfnar` at f455c37 (PRs #2-#36 merged). Everything here was read, compiled or run as a script in the
cloud; nothing was run in game.

## How each pass works

- **Readers.** Ten readers per pass, one area each, every file in the area read again from the start: A1 kit
  `README`s and chapters 01-12; A2 chapters 13-18; A3 chapters 19-24, `reference/`, `examples/recipes/`; B1 the
  vessel template, `examples/`, kit `tools/`; B2 Argo and the shared kit (both copies); B3 Yossele; B4 RogueWavesMode,
  RogueLab, Riptide, `Handoff/`; C `TDS_Mod/Tools`, `TDS_Mod/README.md`, `concept-tools/`, `.claude/`, `.gitignore`;
  D `CLAUDE.md`, `README.md`, `docs/` pages, `docs/mode/`, `docs/reports/`, `docs/concepts/`; E `docs/scope/`. From
  pass 9, three more readers check scope `16` line by line against the game's code (Smacky and Sno Sno, Boutman, Fie
  and Buster), so a pass has thirteen.
- **Checks.** Each claim against the code, the game's API dump and data survey, the decompiled game, the round logs and
  the other living pages. Then every mod, define set, recipe folder and editor folder compiled with cloud-check (21
  builds), and a script checks every repository path and pitfall number named anywhere.
- **What counts.** A noticeable issue is one a reader or a tool would trip on: a wrong fact, a pointer that leads
  nowhere, stale state on a living page, two copies that differ, a code or script bug, a missing piece a rule requires.
  Wording taste, history in frozen reports and dated log lines, and items already decided or scheduled
  (`docs/PLAN.md` Part C Decisions 2026-10-15) do not count. From pass 11, a line that leaves something out counts
  only when what it leaves out would make a player choose or play differently; a missing size of a small effect it
  already names does not. From pass 13 (Dimitri, 2026-10-16 00:31Z, "Only wrong facts"), only a false statement, a
  pointer that leads nowhere, a code or tool bug, or two copies that differ count; a true line that leaves something
  out never does.
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

## Pass 2: 61 issues

| Area | Found | Fixed by its reader | Fixed by the lead | Listed |
| --- | --- | --- | --- | --- |
| A1 kit 01-12 | 2 | 2 | 0 | 0 |
| A2 kit 13-18 | 4 | 4 | 0 | 0 |
| A3 kit 19-24, reference, recipes | 2 | 2 | 0 | 0 |
| B1 template, examples, tools | 9 | 6 | 2 | 1 |
| B2 Argo, shared kit | 4 | 2 | 2 | 0 |
| B3 Yossele | 1 | 1 | 0 | 0 |
| B4 mode, RogueLab, Riptide, handoffs | 1 | 0 | 0 | 1 |
| C tools, settings | 6 | 5 | 0 | 1 |
| D project docs | 11 | 9 | 2 | 0 |
| E scope | 21 | 20 | 1 | 0 |

Every pass 1 fix was re-checked in place and held. **Heaviest finds** (fixed unless listed):

- Scope `16_Base_Vessels.txt`: 22 upgrade lines (and the Heater's stat line) gave the data's parameter names, but the game's code uses those numbers
  differently. Hyper Pipes raises damage, not attack speed; Big Dog's collision speed goes up 0.8, not down; Hard Sno
  makes the Box's hurt x50 instead of x20; Iron Fish's real buff is the recruit buff; Snobles' unit count is never
  read. Each line now says what the code does, with a `/~ Read from the game's code \` note.
- Kit `03`: a native turret's `Pivot/Gun` child is required, not optional (the game's cannon looks call `Gun.LookAt`
  with no null check, so a model without it throws on every shot). Kit `13`'s Boutman row named the wrong spawn and
  wake rule.
- `docs/PLAN.md` Part D's handoff template gave a `make_handoff.py` command that exits at once; three living pages
  still keyed players by Steam id, which kit `19` §5 says a mod can't read.
- The cloud preview skipped every sheet named in a loop without a word (Argo's six Naut sheets); it now warns.
- Pitfall #95 read backwards on when a look is sent again; the builders' localization comment said the game reads the
  sheet; `_PROBE` defines that no code reads came back in two notes through a merge.
- Listed: B4-14 (a turned-away player gets in on a second try), B1-22 (the template's throttled looks), C2-6 (with
  B1-4).

## Pass 3: 27 issues

| Area | Found | Fixed by its reader | Fixed by the lead | Listed |
| --- | --- | --- | --- | --- |
| A1 kit 01-12 | 2 | 2 | 0 | 0 |
| A2 kit 13-18 | 4 | 4 | 0 | 0 |
| A3 kit 19-24, reference, recipes | 5 | 4 | 1 | 0 |
| B1 template, examples, tools | 0 | 0 | 0 | 0 |
| B2 Argo, shared kit | 0 | 0 | 0 | 0 |
| B3 Yossele | 1 | 1 | 0 | 0 |
| B4 mode, RogueLab, Riptide, handoffs | 1 | 1 | 0 | 0 |
| C tools, settings | 2 | 2 | 0 | 0 |
| D project docs | 3 | 1 | 2 | 0 |
| E scope | 9 | 8 | 1 | 0 |

Every pass 2 fix held. Nothing new for Dimitri. **Heaviest finds** (all fixed):

- Scope `16`: seven more upgrade lines of pass 2's kind (Assassin Shots, Fire Arrows, Bout Battleship, Boost Pad, Second
  Servings, Boutman's two heals); `02`'s log line now counts 29 and the Heater's note.
- Kit `19` said a command's `Goalobject` never reaches clients; it does, through `Updatecommandclientobject` (Fie's silos
  and Buster's pads use it). Kit `13` §6.4 told a modifier to send its look through the throttled `Vfxdebuffcheck`
  with no word of the throttle; it now points at `19` §4 and #95.
- `TDS_Mod/Tools/pc/README.md` never had the round move Dimitri's Yossele play build out of `TDS\Mods` and back;
  `.claude/rules/mod-code.md` still gave kit 1.1's "first 10 boat positions" rule (since 1.4 the whole layout).
- Code line cites past the end of their files in kit `15`, `16`, `17` and `24`; kit `23` and `RecipeSave.cs` said a
  two-argument `Adddata` picks the float version (in mod code it doesn't compile); RogueLab called the easier
  "Treated" setting the hard one.

## Pass 4: 37 issues

| Area | Found | Fixed by its reader | Fixed by the lead | Listed |
| --- | --- | --- | --- | --- |
| A1 kit 01-12 | 2 | 2 | 0 | 0 |
| A2 kit 13-18 | 4 | 4 | 0 | 0 |
| A3 kit 19-24, reference, recipes | 2 | 2 | 0 | 0 |
| B1 template, examples, tools | 0 | 0 | 0 | 0 |
| B2 Argo, shared kit | 0 | 0 | 0 | 0 |
| B3 Yossele | 2 | 1 | 0 | 1 |
| B4 mode, RogueLab, Riptide, handoffs | 0 | 0 | 0 | 0 |
| C tools, settings | 1 | 1 | 0 | 0 |
| D project docs | 4 | 3 | 1 | 0 |
| E scope | 22 | 22 | 0 | 0 |

This pass checked every decompile line cite by script (over 500, in kit `13`-`24`, `reference/` and the project docs)
and every Numbers line of scope `16` against the game's code, to close those two classes instead of finding a few more
each pass. Every pass 3 fix held. **Heaviest finds** (fixed unless listed):

- Scope `16`: the line-by-line check found 21 more upgrade lines and the Sno Box's Heal line of the same kind
  (Rejuvenate, Engineering Firm, Marketplace, Snoble Alert, Tank among them); all 195 upgrades are now checked.
- Kit `15` §14.2 and §16 had `Resourcegainmultiplier` backwards: `Mode_Normal` holds it at 1 only for the start aura,
  so it scales Tide pay and drops (`MECHANICS_CHECK.md`'s Depth rows had the same, now noted).
- `docs/PROJECT_SETUP.md` still named the old default branch; GitHub's default is `claude/phase-1-6lfnar` now.
- `TDS_Mod/Tools/pc/README.md`'s release step zipped "the pack", which the Build Queue can't make yet (no Pack step).
- Kit `19` said `Commandlist` entries compare by name (the struct compares every field but `Goalobject`); kit `15` cited
  the player arrow in the wrong file; kit `01` said armour counts each hit as 1 (it is `Damagemulti`).
- Listed: B3-36 (Yossele's vine stems step 1 px at tile joints).

## Pass 5: 16 issues

| Area | Found | Fixed by its reader | Fixed by the lead | Listed |
| --- | --- | --- | --- | --- |
| A1 kit 01-12 | 0 | 0 | 0 | 0 |
| A2 kit 13-18 | 2 | 2 | 0 | 0 |
| A3 kit 19-24, reference, recipes | 1 | 1 | 0 | 0 |
| B1 template, examples, tools | 1 | 1 | 0 | 0 |
| B2 Argo, shared kit | 4 | 4 | 0 | 0 |
| B3 Yossele | 1 | 1 | 0 | 0 |
| B4 mode, RogueLab, Riptide, handoffs | 1 | 1 | 0 | 0 |
| C tools, settings | 0 | 0 | 0 | 0 |
| D project docs | 3 | 3 | 0 | 0 |
| E scope | 3 | 3 | 0 | 0 |

All low or middling, all fixed, nothing new for Dimitri. Most came from following pass 4's corrections into the places
that repeated the old rule: Argo's Ironclad comment and DESIGN row and scope `09` still gave the old armour rule; scope
`16`'s Tank line (a pass 4 fix) had the wrong trigger (a fish leaving, not landing) and Lap 3's +2 is a base mod;
`PLAN.md` Decisions still read the default branch and C23 as pending; two older reports got top notes (default branch,
Steam id). Smaller: kit `17` Cold Sweats is within its Range, kit `18` Claymore's attack frame, the kit's Tide number
is 1-based, a Sprigg note named the wrong prefab call, a recipe comment on the language-change order, Yossele's client
header, and the campaign comments (clearing a slot erases the campaign; a switch keeps it).

## Pass 6: 2 issues

Nine areas came back clean (A1, A3, B1, B2, B3, B4, C, D, E: 0 each). A2 found two low ones in kit `18` and `17`, both
fixed: pass 5's Claymore line was itself wrong (its Attack folder is one blank 1x1 frame played on `Launch`, which hides
the body while the bomb flies, not a still of the body), and `17` named an upgrade "Tank Bed" (it is Tank). Every pass 5
fix held; scope `16` had 59 more lines checked against the game's code, all right.

## Pass 7: 0 issues (clean)

All ten areas came back clean, with nothing fixed and nothing new listed. A2 confirmed pass 6's two fixes against the
art survey, the data survey and the decompile. E checked about 250 more stat and unit lines of scope `16` against the
game's code; all were right. Scope `16` and kit `17` agree. All 21 builds compile with 0 errors. The two `Shared`
copies are equal, the path and pitfall check shows no new candidates, and `/mnt/project-files/scope/` equals
`docs/scope/`.

## Pass 8: 7 issues

Not clean, so pass 7 does not count toward the two in a row. Six areas came back clean (A1, A3, B1, B3, B4, C). The
other four found 7 issues, all fixed, none high:

- **Scope `16` (4).** Three Buster upgrade lines gave the data's parameter names where the game's code uses the
  number another way. Passes 4 and 5 had marked them right after comparing them with the data survey rather than the
  code:
  - Circuit is +20% per tile to the nearest team pad within 5 tiles, so +100% for a lone pad.
  - Speed Bump hurts 5% of the foe's current health, +4% per point of its speed.
  - Quick Pass collects 1 more per pickup, plus 0.15 per point of speed above 2.

  Diesel Dog's four burn hits land at 0, 0, 0.5 and 1 s, not 0.5 s apart. The `02` log now counts 51 upgrade lines.
- **Kit `13` §10 Q3 (1).** It now says every `Mode_Normal`-script mode's last Tide runs 9999 s, ending only when
  cleared, not only Tsunami's.
- **`docs/mode/MECHANIC_IDEAS.md` (1).** The page now says kit `15` §18.1 has 11 patterns, not 12.
- **`ArgoModels.cs` (1).** A comment now names "Swell" among the part names.

Because survey comparison missed three lines, pass 9 reads every Numbers line of scope `16` against its code branch
in the decompile.

## Pass 9: 39 issues

Not clean. Five areas came back clean (A3, B1, B3, B4, C). All 39 issues are fixed, none high, and 28 of them are in
scope `16`.

**Scope `16`.** Three extra readers read every one of `16`'s 195 Numbers lines, its unit lines and its stat blocks
against the code branch that uses them. They found 28 lines that gave the data's names or left out what the code does.
Some examples:
- Cannons adds a second gun.
- Excavation makes every miner Real Tough.
- Brain Freeze's armour soaks 3 whole hits.
- Take the Heat hurts the Scorched foes that hit it.
- Snobles counts siphons, not Sno.
- Boutmom stays only from the 5th Bout Raft.
- Fish Follow has no range.
- A Buppy hurts itself 25 per hit.
- Hyper Pipes pays a flat 1 of each aura a second.
- Knight Commander reaches every same-gun Knight on the map.
- Pure Ex's silence is shorter on tougher foes.
- A collision only takes speed the pads gave.
- "Speed damage at start" is a ram delay.

Two of the 28 had been left on purpose for a reason the code does not bear out: the knight gun note and Stored
Charge. The `02` log now counts 59 upgrade lines. Every value the scope gives as the game's own is now checked against
the game's code.

**Kit chapters and project docs (11).**
- **Kit `15` §16 (M).** Stock projectiles do credit kills.
- **Kit `17`.** The Garage passes on all nine upgrades. Collisions take only pad speed. The Sno Cannon takes a whole
  Snoble first.
- **Kit `02`.** It names the Boutboat's cost of Red 100.
- **Kit `05` §16.** It points to the enemy ids and stats that exist.
- **The crowded boat (kit `README`, `04`, `ModKitBoat.cs` in both copies).** It keeps full size through 17 vessels,
  not 16.
- **Smaller fixes.** An `ArgoCharacter.cs` pose comment. `SEA_CHART.md`'s longest world name is 30 bytes. `STYLE.md`
  gains the objectives' naming row that scope `00` points to.

## Pass 10: 37 issues

Not clean. Two areas came back clean (B1, C). All 37 issues are fixed, and none is high.

**Scope `16` (19).** The second full read against the code found these:
- **Lines that left out how an upgrade plays.**
  - Ice Brick heals by eating Sno after 1 s without a hit.
  - Tip of the Iceberg fires only at those heal checks.
  - Hidden Surprise reflects nothing without Spikes.
  - Bouncing Bombs throws a second bomb.
  - The Heater melts once per attack.
  - Shao-min's wave flies 2.4 tiles at foes within his fishing range.
  - Ip Man-of-War gives Boutbro +800% damage, more range and a teleport.
  - Kung Fu Tackle's Rally has a 20 s cooldown.
  - Bout Battleship's cannon fires only with Dad or Mom aboard.
  - Bout Barge's "stun" is a slow.
  - Gubby Deadarmor counts hits.
  - Boutmom gains +25% damage per Raft boat past 5.
  - Lap 3's tick and pickup cost were wrong.
- **Corrections to passes 8 and 9.**
  - A Commander Knight counts itself.
  - Stored Charge never falls below +25%.
  - Boutmom is one on the map at a time.
- **The `02` log.** It counts 59 upgrade lines, not 62, and names pass 9's unit-line changes.

Kit `17` was changed to match in four places: the Heater is a `Towerfighter`, plus the unit tags, the drowning hit and
Boutmom's limit.

**Elsewhere (18).**
- **Kit `24` §6 (M).** The boss plan now fits Rogue Waves' own win rule (map won at Tide 15).
- **Comments that claimed projectiles reuse one damage dictionary.** They were wrong (pitfall #30) and are fixed in
  Argo `04`/`07`/`09`, Yossele `12_Hooks.cs` and both Argo implementation notes.
- **Kit `02`.** The unit-producer cost line no longer contradicts the Boutboat.
- **Smaller fixes.**
  - `VESSEL_SURVEY.md`: only 13 of 36 effects time out.
  - Yossele's `DESIGN.md`: the vines creep 1 tile per 2 Growth stages.
  - RogueLab's `README` step 10 is packaging.
  - `RESEARCH_GAPS.md`: Tsunami's Tides are large, not long.

Every one of pass 9's other fixes held.

## Pass 11: 18 issues

Not clean. Four areas came back clean (A1, A2, A3, C). 17 issues are fixed and 1 is listed; none is high. Every
fix below was checked against the decompiled game.

**Scope `16` (11).** The third read against the code found these:
- **Lines that left out how an upgrade or unit plays.**
  - Smacky's engineers pick the tower nearest their Hut, so where the Hut stands decides which towers get tuned.
  - The Sno Box makes Sno only while an ore is in its range.
  - 1 Inch Bout's ramp grows only in melee; Shao-min's shots alone don't feed it.
  - Ip Man-of-War also doubles each of Boutbro's hits, so he deals 18 times his base damage, not 9.
  - Fie's Wall: each piece has its own upgrades. Only Carbon Infused, Invisible Forces and True Wall spread, when
    bought, to the pieces connected then (three lines).
  - True Wall's charge deals 1000 damage and sends the foe back to its spawn, once per piece.
  - Unstable Wall's weaken is -25% speed and damage, for good.
  - Side of Sushi's critical hit never fires (a game bug, already in kit `17` §6).
  - Quick Pass stops working once Tune Up or Final Lap is bought (a game bug).
  - Wrong Way stuns only foes going the other way, and lengthens Boost Pad's slow on them.
  - The Checkpoint's "damage to ore" is the charge it gains each tick.
- Kit `17` was changed to match for the engineers and the Sno Box. The `02` log now counts 66 upgrade lines and names
  pass 10's Heater, Ice Brick and Deadarmor lines.

**Elsewhere (7).**
- **The frozen Argo examples (2, M).** `examples/code/Argo_07_TowerBrain.cs` and `Argo_09_Hooks.cs` still said
  projectiles share one damage dictionary, and kit `05` sends readers to both. Each now has a header line giving the
  truth (pitfall #30); their bodies are untouched.
- **Argo's implementation notes.** They said a shot's parameters merge into the shared `Defaultparams`; each shot
  starts from a copy (pitfall #29). The old Argo 1.0 notes in `examples/` keep the old sentence as history (decision
  A17).
- **RogueLab's `README`.** Map 1's row cited kit `15` §5 (the shop), which it never touches; it now cites the sections
  its code uses.
- **`docs/mode/MECHANIC_IDEAS.md`.** The "Long Line" idea for Boutman changed `Rallyduration`, a private 15 s field
  nothing sets; "lasts longer" now says it needs the pack's own code.
- **The `02` log** did not yet cover pass 10's changes to `16`.
- **Yossele's Stonehenge text (listed, B3-39).** The in-game Lore still says the vines creep "a little further" each
  Tide; they grow one tile per two Growth stages. It is game text, so it waits for the next Yossele build.

Every one of pass 10's fixes held.

## Pass 12: 8 issues

Not clean. Nine of the thirteen readers came back clean (A1, A2, A3, B1, B2, B3, B4, C, and Boutman's part of scope
`16`). All 8 issues are fixed; none is high. Every scope `16` fix was checked against the decompiled game.

**Scope `16` (6).**
- Sno Boulder: a new Sno Cannon offers it first, and bought there it makes no Sno at all (a game bug, kit `17` §4).
- Permasno: a shot that finds Sno takes the whole pool off its tile, up to 3 times its ammo.
- Tip of the Iceberg: each heal check that fires eats all the Sno on its tile.
- The Heater melts and burns on its own timer, whether foes are near or not.
- Second Servings: a Rest Stop holding its buff stops buffing until the buff ends (a game bug, kit `17` §6).
- Living Wall: each shot's beam hits twice, so 6 damage, not 3, except on a foe more than 5 tiles away.

**Bookkeeping from pass 11 (2).** The `02` log's count of changed upgrade lines (now 66) and `docs/PLAN.md` Part C
"Next", which did not yet carry pass 11's listed B3-39.

Every one of pass 11's fixes held. At 00:17Z on 2026-10-16 Dimitri asked to stop after this pass and say why every pass
finds issues; that is the next section.

## Pass 13: 8 issues (only wrong facts count from here)

Not clean. Dimitri chose to go on with a narrower bar: only wrong facts count. Seven of the thirteen readers came back
clean, and none of the three readers of scope `16` found a wrong line in it (one found a wrong line in kit `17`). The 8 wrong statements
are all fixed; none is high.
- **Kit `16` and `17` (4).** Ore Scraps pays 1 of each colour the hit ore yields, not of every colour; the Mining
  Hut's Convert trades at most once a second, not on every gain (both chapters); Fie's hero script also re-runs her
  melee targeting.
- **`docs/mode/SEA_CHART.md`.** The flood's stand-in block is made on the boat once Manafold's groups load, then
  retried in the map's first 30 s, not "once the map has loaded".
- **Argo `04_Content.cs` (comment).** Fie's Reroute picks an ore, not a silo.
- **cloud-check's preview driver (comment).** It renders at the builders' preview angle; the game camera has no
  fixed angle.
- **This report.** Scope `16` was 28 of pass 9's 39 issues, not 31.

## Pass 14: 10 issues

Not clean. Six of the thirteen readers came back clean (B2, B3, E and all three readers of scope `16`). Nine wrong
statements are fixed and one code problem is listed; none is high.
- **"Hidden" stats (kit `02`, `05`, `VESSEL_SURVEY.md`, and comments in Argo, Yossele and the template).** These said
  every shipped stat is hidden. `Display_Exposed` off hides a stat only from other teams; the owner's panel shows it
  (`Game_Ui.cs` 146-174).
- **Kit `17` §3.** Convert and Marketplace do unsubscribe when the hut goes; the bug is their downgrade, which clears
  the flag before testing it.
- **RogueWavesMode `07_Weather.cs` (comment).** The flood's stand-in block is made on the boat, as pass 13 fixed in
  `SEA_CHART.md`.
- **Comments and docstrings.** The template's Marked look is velvet, not plum; `sea_chart.py`'s `links_between`
  docstring now says what the walk does.
- **This report.** Pass 13 had seven clean readers, not eight.
- **The sea chart's links (listed, C2).** Scope `04` says each map links to 1 to 3 maps. The walk that draws the chart
  (`sea_chart.py` and the mode's `03_Chart.cs`) gives a row's last map every child left, so in about 7 seeds in 100 one
  map links to 4 and the vote offers 4. This changes the game, so it is listed with a default below.

## Pass 15: 16 issues

Not clean. Seven of the thirteen readers came back clean (A1, A2, A3, B2, E, and Smacky's, Sno Sno's, Fie's and
Buster's part of scope `16`). All 16 wrong statements are fixed, and none is high.
- **Scope `16`, Boutman (7, Claude-made lines only).** Four "its effect is in the building's code" notes now point to
  Boutman's or the fish's code (Fish of Fury, Fight Blub, Baviar, Pubberfish). Three lines said more than the code
  does: the Bout Battleship cannon keeps firing at its last target after Dad or Mom flies off; Boutmom keeps her last
  bonus when the Raft count falls back to 5; Buppy comes only when 2 tiles beside the barrel are free (kit `17`'s
  bug list already had this). Passes 10 and 11 had left four of these as too small, which no longer counts.
- **Map 1's vote (`SEA_CHART.md`, `sea_chart.py` docstring).** They said a vote offers 1 to 3 maps. Map 1's vote is
  the whole first row, 3 or 4 maps, as scope `04` says. This is not C2.
- **Comments (Yossele, the template, the mode).** `Stats()` styles each stat like the template's stat of the same
  name, not its first stat (Yossele and the template); Hallucinogen4's little caps sit on the big cap; Yossele's
  low-poly header named Mummer's drapes; the template's HOOK test foe stands about 3 tiles away, not 4; the flood's
  stand-in retries once a second in each map's first 30 s, not once per map.
- **This report.** Pass 14 had six clean readers, not seven.

## Pass 16: 4 issues

Not clean. Ten of the thirteen readers came back clean (A1, A2, A3, B1, B2, B3, B4, C, E, and Fie's and Buster's part
of scope `16`). All 4 wrong statements are fixed, and none is high.
- **Scope `16`, Bout Battleship (a pass 15 fix).** Pass 15 said the cannon keeps firing after Boutdad or Boutmom fly
  off. A landed Dad or Mom re-orders the boat every 0.2 s and the orders alternate between starting the firing loop
  and stopping it, so it keeps firing only if the last one started it. The line now says it may.
- **Kit `17` §4 (snow).** It said team filters never see Sno Sno's snow. A team-only foe query does; only own-team
  filters and `Mob`/`Tower` tag queries skip it.
- **`CLAUDE.md` and `SCOPE_GAPS.md`'s top note.** `MECHANICS_CHECK.md` proved in game only the eight mechanics that
  needed the game, not all of them; map 1's vote is the whole first row, as pass 15 fixed in `SEA_CHART.md`.

## Pass 17: 1 issue

Not clean. Twelve of the thirteen readers came back clean. The one wrong statement is fixed and is not high:
`docs/reports/README.md`'s row for `MECHANICS_CHECK.md` said every mechanic was proven on the PC, the same claim pass
16 fixed in `CLAUDE.md`. Only the eight that needed the game were.

## Pass 18: 4 issues

Not clean. Eleven of the thirteen readers came back clean. All 4 wrong statements are fixed, and none is high.
- **Unlocks (`docs/EXPANSION_SCOPE.md` row 9, kit `15` §18.4).** They said only vessel and Cardinal unlocks are
  announced. A world-link unlock plays its link's cine on the world map (`Macro_Unlock.cs` 409-490), as kit `23` §3.2
  already said.
- **Yossele (`DESIGN.md`, `docs/YOSSELE_BUILD_NOTES.md`, a `YosseleCharacter.cs` comment).** Stock `Debuff_Slow` shows
  only for the vines' slow, not Sticky Spores'. Every 32 px frame is Dimitri's pixels, but the 16 px boat mini, the
  splash, the ready-up face and the tab icon are drawn new; the pages said none was.

## Pass 19: 3 issues

Not clean. Ten of the thirteen readers came back clean. All 3 wrong statements are fixed, and none is high.
- **Argo (`TDS_Mod/ModSource/Argo/Code/02_Data.cs`, a comment).** `OreReach = 2.1f` reaches 2 tiles straight out
  and 1 diagonally, as `DESIGN.md` and the journal say; the comment said the next tile, diagonals too.
- **Yossele (`DESIGN.md`, a `YosseleCharacter.cs` comment).** Pass 18's fix named only the boat mini as drawn new; the
  32 px tab icon is drawn new too.
- **`docs/EXPANSION_SCOPE.md` row 3.** RogueLab round 1 chained two maps, not three; rounds 2 and 3 chained through
  every base map.

## Pass 20: 0 issues (clean)

All thirteen readers came back clean, with nothing fixed and nothing new listed. Each checked pass 19's fixes in its
area. One judgement call is left as it is: scope `16`'s Hypercool line says the cannon is back to +50% when idle; the
streak does restart then, and the first shot after keeps the last speed for one shot, a detail rather than a wrong fact.

## Pass 21: 2 issues

Not clean, which restarts the count of clean passes in a row. Twelve of the thirteen readers came back clean; the
second item is one B1 had left as a judgement call, which the lead counted as wrong. Both are fixed, and neither is
high.
- **Kit `15` (`TDS_Vessel_Creator/docs/15_ROGUE_MODE_ENGINE.md`), the trade-off row.** It said the Glass Core takes no
  damage. Each sub-core takes its own hits through it (the main core drops it), and a sub-core that breaks hurts every
  foe instead of losing the map (Entity_Ai_Effect_Artifact_Glasscore.cs; this wording is pass 22's correction).
- **Kit `03` (`TDS_Vessel_Creator/docs/03_ART_AND_AUDIO.md`), the custom aim row.** It said Argo 1.0 passed 64/64 in
  game. The only recorded run is 63/64, and the miss was in the test harness (Argo `IMPLEMENTATION_NOTES.md`).

## Pass 22: 5 issues

Not clean. Nine of the thirteen readers came back clean. All 5 wrong statements are fixed, and none is high; three of
them were in pass 21's own fix and its write-up (the kit `15` row, and the Pass 21 section's wording and its count).
- **Kit `15` (`TDS_Vessel_Creator/docs/15_ROGUE_MODE_ENGINE.md`), the trade-off row.** Pass 21's text read as if the
  Glass Core shields the main core. The main core drops it (Entity_Ai_Effect_Artifact_Glasscore.cs); only sub-cores
  keep it. The row now says so.
- **This report, the Pass 21 section.** It repeated that wording and counted eleven clean readers; twelve were.
- **`.gitignore`, a comment.** It said the handoff tools skip everything the PC writes. `Get-PcManifest.ps1` lists
  `build_log.txt`, which `make_handoff.py` reports as "On the PC only (left alone)"; the comment now says so.
- **Argo (`TDS_Mod/Assets/ArgoMod/Editor/ArgoBuilder.cs`, an effect's description).** It said `World_Argo_Flood`
  plays when a Deep Water is placed. Placing one plays the bubbles; the flood splash comes from FLOOD and from Abyss or
  Trench adding a free pool.

## Pass 23: 2 issues

Not clean. Eleven of the thirteen readers came back clean. Both wrong statements are fixed, and neither is high.
- **This report, the Pass 22 section.** It said two of pass 22's five finds were in pass 21's own fix and write-up;
  three were.
- **Scope `07` (`docs/scope/07_Vessel_Yossele.txt`), Explosive Finish's Numbers line (a Claude default).** It read as
  if every foe the burst hits gets 2 stacks. The bursting foe takes the damage but gets no stacks
  (`TDS_Mod/ModSource/Yossele/Code/08_Cannon.cs`), as Yossele's `DESIGN.md` says; the line now says so.

## Why every pass found issues

The counts fell from 179 to 0 by pass 7, then rose again: 7, 39, 37, 18, 8. Outside the base vessels the repository
has been close to clean since pass 6. Four things kept the count up.

1. **One file, scope `16`, has no floor.** It is a 1,400-line summary of the five base vessels that Claude wrote
   from the game's data, and since pass 8 it has been most of every count: 4 of 7, 28 of 39, 19 of 37, 11 of 18 and
   6 of 8. A summary can always say more. Each pass read it one layer deeper (the data survey, then the code branch
   for each upgrade, then every place a value is used, then how upgrades interact and where the game's own code is
   buggy), and each layer found lines the one before could not see. Pass 7 was clean only because it still compared
   `16` with the data survey; pass 8 compared it with the code. The finds are shrinking (28, 19, 11, 6) but still
   come.
2. **The bar moved.** "Noticeable" first meant wrong; from pass 9 it also meant leaving out something the code does.
   That roughly doubled what counted in `16`. Pass 11 narrowed it to omissions that would change how a player plays,
   and the count halved.
3. **Fixes made follow-ups.** Each fix in `16` changes the count in the `02` log, sometimes a matching line in kit
   `17`, this report and the plan's list of what is left. One to three issues a pass (pass 10's three corrections to
   passes 8 and 9; the `02` count in passes 9 to 12) came from the passes themselves.
4. **Each pass uses fresh readers.** Thirteen new readers go over about 400 files with no memory of the last pass.
   They don't miss the same things twice, so a few small finds a pass is expected even in good files: pass 12's areas
   outside `16` found nothing new of their own.

What this means: the code, tools, kit chapters, mode and vessel pages are settled; nothing in passes 11 or 12 was a
code bug or a high item. Scope `16` will keep giving a few lines a pass for as long as it is read against the code
this closely. It is reference for the base vessels (what Directions and Cardinals must work with), not something the
pack builds from.

Passes 13 to 23 bear this out. Scope `16` gave its last find in pass 16. Of the nine finds in passes 21 to 23, four
were in the pass before's own fixes or in this report's own counts; the other five were old lines (two kit rows, a
`.gitignore` comment, an effect's description, a scope default) that a fresh reader saw for the first time. None was a
code bug or a high item.

## Listed for Dimitri

Code that changes behaviour in game, or a choice; each default is in `docs/PLAN.md` Part C "Next".

| # | What | Default |
| --- | --- | --- |
| B3-24 | Yossele's debuff looks still go through the throttled `Vfxdebuffcheck`, and the vines renew the slow look every 0.5 s per foe | Copy Argo 2.0.1's `ArgoUtil.Debuff`, renew the slow look only near its expiry; next Yossele build |
| B3-39 | Yossele's Stonehenge Lore (`TDS_Mod/ModSource/Yossele/Code/02_Data.cs:203`) says the vines creep further each Tide; they grow 1 tile per 2 Growth stages | Fix the string with B3-25/B3-26 in the next build |
| B3-25, B3-26 | Yossele's display names are the pre-2026-10-13 ones (Selfish Sapling, Grafted Colors, Entangling Spaghetti Roots), and Selfish's in-game line has numbers | Rename strings, comments and tables to the scope's names and lines in the next build |
| B3-27, B3-30 | Yossele's footstep plays on the peak-lift frame; the Mend effect and three sounds are built but never played | Footstep on frames 0 and 3; play or drop the unused assets, next build |
| B3-28, B3-29 | Two small leaks in Yossele: `incoming` keeps a foe that died before its seeds landed; `LastHit` statics are never cleared per map | Fix in the next build |
| B3-36 | Yossele's vine ground texture: the horizontal stem enters a tile on rows 6-7 and leaves on rows 7-8, so a vine line along x steps 1 px at every joint (the comment and `ASSETS.md` say the edges match) | Shift one end by a row in the next build |
| B3-31 | Connected Roots guards the whole team's buildings; the scope says "yours" | Ask at the next Yossele build: own or team |
| B4-14 | `09_Rejoin.cs` turns a player away once per session id and keeps the list all map; the game gives a returning player the same session id, so a second Join Game gets them in, seatless, while a seat is empty | Forget a session once it leaves; add a second try to `docs/mode/REJOIN.md` §6 O5; next mode change |
| B4-11, B4-13 | Two weather lines in code differ from scope `04`'s in-game lines; the mode's dev-export description omits the rejoin | Take scope `04`'s lines at the next mode change; the description with the one pack version |
| C2 (pass 14) | The chart's link walk (`concept-tools/sea_chart.py`, `RogueWavesMode/Code/03_Chart.cs`) can give a row's last map 4 links (about 7 seeds in 100); scope `04` says 1 to 3 | Cap the last map at 3 in both at the next mode change, keeping scope `04` |
| B4-12 | `09_Rejoin.cs` keeps the first host's name for the "Lost the map" line all launch | Clear it on leaving a map, next mode change |
| A3-9 | The mode's save keys (`Campaign_Seed`, `Stock_World`, ...) lack the `RW_` prefix kit `23` §2 rule 3 asks for | Rename at the next mode change; no campaign has been saved outside the self-test, so no migration |
| B1-3 | The template never wires kit 1.3 `ModKitCombat` (kill credit) or kit 1.2 `ModKitTowers.Fit` | Add them with the 1.6 pack calls (B10), before Spool is scaffolded |
| B1-22 | The template's `MummerUtil.Debuff` sends its looks through the throttled `Vfxdebuffcheck` (B3-24's problem), so Spool and Uloe would inherit it | Port Argo 2.0.1's `ArgoUtil.Debuff` into the template with B10, before Spool is scaffolded |
| B1-4, C2-6 | `<Name>Aseprite.cs` (template, Argo, Yossele) and `ase_sheets.py` name unit sheets so the unit branch skips them: no `.aseprite` for units | Fix both writers together with B10 |
| B2-X1, B2-X6 | Argo's DESIGN gives FLOOD "1/Quickaction", the code key 1 only; `GiveUpAfter` (12 s) is unused, the Naut gives up after 6 s | At Argo's balance pass; then also the Ironclad line "the next 2 hits take 1 each", which a Bulkhead Naut stretches to 3 hits |
| C10 | `.claude/settings.json` lacks `apt-get update` and `python3 -m pip install UnityPy`, which the docs tell sessions to run | Add them (permissions are yours) |

## Left on purpose

`docs/YOSSELE_BUILD_NOTES.md` stays in `docs/` (a living page for the current Yossele build, kept
current in this pass); old Argo notes and the Handoff READMEs stay as history.
