# Text audit (2026-11-05)

Dimitri's queued order of 04:51Z ("build the unbuilt candidates, do a text audit, then do a balance simulator sweep"),
run after the build of the unbuilt candidates (PRs #103-#114, base `a3cf96a`, kit 1.12). Every player-facing string
of the pack and the living docs, read for spelling, grammar, the naming rule, stale facts, text that doesn't match what
the code does, and the repository clean rule. No balance number changed; the scope keeps Dimitri's wording except
for spelling, grammar and stale facts, and every doubtful line is in §4 for him. Docs and text only: no behaviour
changed. Compiled here (§5); nothing ran on the PC.

## 1. What was read, and how

| Set | Size | How |
| --- | --- | --- |
| Player-facing strings | 21,459 string literals in the six parts, the kit and the editors; 1,949 with two or more words outside the self-tests | pulled out of the C# with a small tokenizer (comments skipped), spell-checked (`aspell`, en_GB), the upgrade, Direction, Trinket, Cardinal, objective, weather, shop, vote, card, menu, rejoin and mod-list lines read by hand |
| Upgrade lines against the scope | every `% N:{Name}` of scopes `07`-`10` | `TDS_Mod/Tools/cloud-check/scope_upgrades.py`: every built upgrade's in-game line matches its scope line (Spool's 22 are unbuilt, as queued) |
| Names against the scope | the 71 Directions, Trinkets and Cardinals of scopes `11` and `12` | each name is found in the code, spelled the same |
| Docs | `README.md`, `CLAUDE.md`, `docs/PLAN.md` (Part A and Part C "Now" read; Log and Decisions are dated and only searched), `docs/scope/00`-`16`, `docs/mode/`, `docs/STYLE.md`, `PACK.md`, `TEMP_ART.md`, `EXPANSION_SCOPE.md`, `RESEARCH_GAPS.md`, the kit's `README.md` and `docs/01`-`24`, the parts' READMEs, `DESIGN.md`s and notes | `codespell`, `aspell` (unknown words that occur once or twice in the repository), doubled words across line breaks, and searches for each stale fact and old name; scope `00`-`06`, `13`, `14`, `STYLE.md`, `README.md` and the mode's README read in full |

Clean, nothing to change:

- **Naming rule.** "Vessel Cardinal" and "Team Cardinal" are left only where they belong: dated entries, the rename's
  own notes, the save-compatibility comments and the self-test that checks the shop no longer says them.
- **Stale campaign facts.** "15 maps" and "Swell" (the old name of Depth) are left only in the Log, Decisions, dated
  kit rounds and Argo's Deep Water model part (its water is a part named `Swell`). Every living page says 10 maps, won
  by leaving map 10 after Tide 15.
- **Repository clean rule.** No living page presents a pre-scope mod. The mentions left are the ones PR #78 kept on
  purpose: evidence tags ("proven by RogueLab/Riptide"), the `Riptide` and `RogueLab` folders, the kit, Argo's history
  and Dimitri's own scope notes (`02_Progress.txt`'s Riptide line).
- **Spelling of player text.** `aspell` found no misspelt word in a player-facing string; the words it flagged are the
  pack's names (Yossele, Mushboom, VENDCINT...), the game's (Snoble, Smacky) or deliberate ("Tanky", "offcuts").
- **Doubled words.** None in the docs or the strings.
- **Base game text.** Scope `16`'s `%` lines quote the game's own upgrade text, typos included ("infront", "firey"):
  kept, since they are the game's.

## 2. Fixed: what players read

The in-game line and its scope line changed together, so `scope_upgrades.py` still matches them.

| # | Where | Before | After |
| --- | --- | --- | --- |
| P1 | `RogueWavesMode/Code/07_Weather.cs:33`, scope `04:76`, `docs/mode/SEA_CHART.md:169` | Rising Water: the water climbs each tide | ... each Tide (as every other line writes Tide; also the boot log's "Pure Aura a Tide", `07_Weather.cs:269`) |
| P2 | `RogueWavesFoes/Code/00_RogueWavesFoesMod.cs:141`, scope `13:39` | Blinds and illuminates everything nearby | Blinds and Illuminates everything nearby (both are statuses, capitalised everywhere else) |
| P3 | `RogueWavesMode/Code/17_Cardinals.cs:44`, scope `12:30` | Your vessel attacks heal themselves for a portion of damage dealt | Your vessel's attacks heal it for a portion of the damage dealt |
| P4 | `RogueWavesMode/Code/16_Directions.cs:32`, scope `11:16` | ... a max amount of damage per each hit | ... a max amount of damage per hit |
| P5 | `Argo/Code/02_Data.cs:183`, scope `11:259` | Deep Water reaches further still around each pool | ... farther still ... (ranks I and III say "farther") |
| P6 | `Spool/Code/02_Data.cs:302` | MEND: 2 Health a second ...; 1 Thread per 5 Health | ... 2 health a second ...; ... per 5 health (an amount, lower case, as Argo's and Uloe's mending lines) |
| P7 | `Assets/ArgoMod/Editor/ArgoModPrefab.cs:65` (the Mods menu) | a scatter cannon, a harpoon tower and a drill rig | a Scatter Cannon, a Harpoon Gun and a Drill Rig (the buildings' names) |

## 3. Fixed: the scope and the docs

| # | Where | Before | After |
| --- | --- | --- | --- |
| S1 | scope `03:8` (F Picks a Minion) | Status: built, and passed on the PC on a real death and respawn | the pass ran with the fix not yet switched on (the max review's P1-1, pitfall #157); fixed since, compiled, checked again next round |
| S2 | `docs/PLAN.md:228`, `:407`, `docs/RESEARCH_GAPS.md:17`, kit `docs/15:34` | the real Boutman death passed | the same, plus "with the fix unarmed; kit 1.11 arms it" |
| S3 | scope `06:19` ([Directions] Status) | built, and passed on the PC | ...; the Directions added since are compiled, not yet run on the PC |
| S4 | scope `02:13` ([Now]) | not built yet: the new upgrades in 07-10, the new Directions, Cardinals and Trinkets in 11-12 | not built yet: Spool's new upgrades and UNTHREAD; the rest is built, compiled, not yet run |
| S5 | scope `02:16` | only the Yossele play build is in [the Mods folder] now | it held only that until 2026-11-03, and since holds every pack part's release build (as `Yossele/README.md` says) |
| S6 | scope `00:5`, `docs/PLAN.md:41` (Part A) | Both [scope copies] are kept the same | ...; his copy is written only at his word ("sync"), as `CLAUDE.md` rules |
| S7 | `docs/PLAN.md:248` (Part C, the 2026-11-04 state) | Designed, not built: ... Argo's review, Uloe's 8 to 32, the new Directions, Trinkets and Cardinals | adds: since 2026-11-05 he has taken the defaults and all are built but Spool's |
| S8 | `docs/PLAN.md:398` (Part C "Next") | the `~defaults~` in scope 04, 10-14; Open questions 3-4 and 6 | the same, marked taken 2026-11-05 and now the balance sweep's |
| S9 | `README.md:37` | Every part ... passes its self-test in the game | passed it; what was built on 2026-11-05 is compiled, not yet run in game; only Spool's new upgrades are left to build |
| S10 | `README.md:16`, `:19` | earned each tide; between tides | each Tide; between Tides |
| S11 | `docs/STYLE.md:34`, `:46` | examples Cracked Buoy, Ship's Bell; "The first is named in 12" | the real ones (Brass Spyglass, Cork Lifebuoy); all sixteen are named in scope `12` |
| S12 | `docs/STYLE.md:49` | ascension levels "Depth 1, 2, ..." | ... (Depth is on hold) |
| S13 | `Spool/Art/Temporary/README.md:10` | the tuft, Thread Head and Crafted described lilac and purple; the needle with a purple thread tail | a note: sage since 2026-11-05 (`spool_sage.py`); the needle in game is the pale sage pearl-headed pin |
| S14 | `RogueWavesMode/README.md:18`, `Yossele/README.md:8` | the status stops at the 2026-11-04 round | adds what 2026-11-05 built, compiled, not yet run in game |
| S15 | scope `04:7` | New vessels and cardinals | ... Cardinals |
| S16 | scope `05:6`, `:9`, `:11` | gameplay alternations; traded with to the shopkeeper; choosing a direction | alterations; traded to the shopkeeper; choosing a [Direction] |
| S17 | scope `06:6`, `:32` | gameplay altering; inbetween | gameplay-altering; in between |
| S18 | scope `11:18`, `:304`-`306` | a bandaid; gives stack of fertilizer | a band-aid; gives a stack of fertilizer |
| S19 | scope `12:37` | a halloween dollar store | a Halloween dollar store |
| S20 | scope `07:115`, `08:50`, `09:6` | pixelart sprite based and not 3d; Starcraft; Drill rig | pixel art sprites, not 3D; StarCraft; Drill Rig |

## 4. For Dimitri

Nothing here is changed; each line names Claude's lean.

| # | Where | What | Lean |
| --- | --- | --- | --- |
| A1 | `RogueWavesMode/Code/19_Objectives.cs:71`, scope `14:22` | Mind the Meteor's node says "A node with a white crystal", but it still looks like a stock ore node (the crystal needs a model; your open call on its look) | if you keep the stock look, the line becomes "A fallen node. Mine it all" |
| A2 | scope `09:105` and `11:537` | **Long Line** names two things: Argo's Harpoon Gun upgrade and Boutman's Direction (Claude's). Scope `00` [Names]: a thing has one name everywhere | rename the Direction to **Spare Reel** (a fishing reel, the same Look) |
| A3 | `RogueWavesMode/Code/20_Vote.cs:662` | the stay-or-leave vote's line: "A majority leaves. Nobody leaves right before their demise" reads as if a majority of players walks off | "Most votes win. Nobody leaves right before their demise" |
| A4 | scope `04:16`, `05:13` | "a new Depth level is unlocked" while Depth is on hold (the end screen says so: `06_Campaign.cs:314`) | keep as your intent; add a Claude note that it waits on Depth |
| A5 | scope `11` (Notched Oar, `16_Directions.cs:70`) | "Every 10th / 7th / 5th hit": numbers in a Direction's text, which `STYLE.md` and kit `docs/02` keep out of upgrade text | keep: the count is the whole rule; or "Every so often" with the count in the journal |
| A6 | the pack's text | British and American spelling mix: Colour Filter, Armour, harbour in game; Woolen Lining, fertilizer, gray, color in the scope | British in player text (the game's "Colour" buttons), names as you wrote them |
| A7 | scope `04` [Sea Weather] Pays | "1 extra Pure Aura each tide, on top of the usual 3": a bonus Tide pays 2 + 1 (`14_Run.cs:267`) | the balance sweep words it with its scope `04`/`06` numbers |

## 5. Checks

Compiled here with `TDS_Mod/Tools/cloud-check` (setup run once in this container), from `TDS_Mod/ModSource`, after the
edits: `check_mod.py` for Argo (release, `ARGO_SELFTEST,ARGO_SHOTS`, `ARGO_REVIEW`), Spool (release,
`SPOOL_SELFTEST,SPOOL_SHOTS`, `SPOOL_REVIEW`), RogueWavesMode (release, `ROGUEWAVESMODE_SELFTEST,ROGUEWAVESMODE_SHOTS`,
`ROGUEWAVESMODE_SELFTEST,ROGUEWAVESMODE_GAPS`) and RogueWavesFoes (release, `ROGUEWAVESFOES_SELFTEST,ROGUEWAVESFOES_SHOTS`): 0 errors each;
`check_editor.sh` on the four parts' `Assets/<Part>Mod/Editor`: clean. (`ROGUEWAVESMODE_GAPS` alone doesn't compile, on the base too: `92_Review.cs:697` needs the self-test's code; the gaps build always carries `ROGUEWAVESMODE_SELFTEST`.) No self-test checks a changed string. Nothing
ran on the PC; the next PC round needs nothing new for this audit beyond reading the seven lines of §2 in game.
