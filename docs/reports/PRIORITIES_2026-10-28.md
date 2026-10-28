# What the project needs most now (2026-10-28)

Dimitri asked (2026-10-28): "Go through the scope and figure out what is most needed right now for the project."
Read: `docs/scope/` 00-16 (the default branch, plus `07`, `01` and `02` as PR #45 corrects them), `CLAUDE.md`,
`docs/PLAN.md` Parts A, B "Release 1" and C, `docs/PACK.md`, `docs/reports/PLAYTEST_2026-10-26.md` (PR #43),
`docs/reports/MISTAKE_CHECK_2026-10-28.md` (PR #45), `docs/reports/YOSSELE_TEST_PLAN_2026-10-23.md`, Yossele's
`IMPLEMENTATION_NOTES.md`, `docs/TEMP_ART.md`, the handoff `Yossele_next.md`. Nothing was built or run; this report
recommends, it doesn't start work.

**The yardstick.** The first release is Yossele alone (`01` [Releases]): the `RogueWavesMod` pack holding only Yossele,
version 0.1.0, a zip Dimitri shares himself, Yossele in a normal game, no {Fertilize}. Its "Ready when" line has four
conditions: Yossele matches `07`, carries Dimitri's own sprites, passes its PC self-test and his play test, and he says
ship. The self-test is done (round 9e, 160/0). So every item below is ranked by how much it moves one of the other three,
and how much it blocks the next one.

## The ranked list

| # | What | Who | Why it matters for Release 1 | What blocks it (scope gaps, open `~defaults~`) |
| --- | --- | --- | --- | --- |
| 1 | **Put the current Yossele on the PC, then Dimitri's own play test** | one PC round (his OK), then Dimitri | His play test is a "Ready when" condition and everything after it (balance, sprites' fit, online) feeds off what he sees. The build he has is round 9e's: it lacks Hallucinogen's Daze fix (`e5e05c1`, pitfall #114: on Woozy the stun shows nothing) and the temporary art wired since (the Cordyfriend's own sheet, the knuckle walk, the laser attack, the bigger fruit; `docs/TEMP_ART.md` "next PC round"). None of the 26 temporary assets has been seen in game. | Nothing in the scope. One PC round: build a release from the default branch head, check the build log names `Art/Temporary/` for each wired sheet, install it as the play build. Then his test against `YOSSELE_TEST_PLAN_2026-10-23.md` §3-5. |
| 2 | **Dimitri's Yossele sprite sheet** | Dimitri | The release waits for it ("carries your own sprites"); it is the one condition no thread can move. The longer it waits, the later everything ships, so the ask should go out now, in parallel with item 1. | **Scope gap:** `07` [Identity] Sprites says the sheet "comes once the design is past drafting". The design is past drafting (built, 160/0), but the slot isn't marked `! NEEDS !`, so nothing in the scope asks for it. The sheet also lists "back view, fall, land"; fall and land are drawn by the code today (`YosseleFall-Sheet`, `YosseleIntroR-Sheet` in `03_Assets.cs`), with no temporary file. The sizes the builders take: `Yossele-Sheet` 192 x 256 (32 px, rows Idle v0-v3 and Move v0-v3, 6 frames), `YosseleAttack-Sheet` 128 x 128 (4 frames, v0-v3), the Cordyfriend 384 x 96 (24 px, Idle 6, Move 6, Attack 4); a file of the same name and size in `ModSource/Yossele/Art/` replaces the temporary one with no code change. The Cordyfriend's own `Look` ("resembles a {Snoble}") doesn't say whether he draws it or the temporary one ships. |
| 3 | **Three balance calls the play test raised** | Dimitri | His balance pass is Release 1 step 3. The play test turned three of its open questions into concrete ones; answering them first makes the rest of the pass smaller. | (a) **The laser mines ore** (stock ranged AI shoots `Resource`; Woozy 802 damage, 0 kills by Tide 7). `07` [Vessel Stats] only says "Attack: ranged": no slot covers it. Default if unanswered: keep it (ore pays every player). (b) **Red runs short, Blue piles up** (Purlest Tide 8: R101, B803; The Hunt lost at Tide 7 with Red the limit all map). 17 of `07`'s costs are still Claude's `~defaults~`, among them the Tree `~Red 50~`, Viral Outbreak `~Red 90~`, Deathcap `~Red 100~`, Picky Placement `~Red 100~`, Explosive Finish `~Red 420~`, the Stonehenge `~Blue 25~` and its four `~Blue~` upgrades, the Sapling `~Yellow 35~`. Moving some Red costs to Blue is a pure numbers edit. (c) **D-MDS stops the Tree on maps without shooters** (an easy trap at level 2): its in-game line is his own words, so any warning is his call. In all, `07` holds about 90 `~` markers (17 costs, 3 building healths, 19 in-game lines, the rest numbers); his pass decides which stay. |
| 4 | **The pack build with one part** | a cloud thread, then a PC round | Release 1 step 4, the only remaining step that needs no input from Dimitri, so it can run now beside items 1-3. Without it there is no zip to ship even once he says ship. | Nothing in the scope. Not started: `TDS_Mod/Assets/ModKit/Editor/ModKitPipeline.cs` has no `Pack` step (`docs/PACK.md` §1). Also: the one version 0.1.0 in `YosseleData.Version` (now `0.2.0-dev`) and the mod's `.info` (now `"1.0"`), the `README.txt` of `PACK.md` §5, and a Mods-folder install with no dev mods. One small call: `PACK.md` §4 says a dev mod carries the pack version plus `-dev`, so the dev build after the pack would be `0.1.0-dev`, a lower number than today's `0.2.0-dev`; default: accept it, since dev numbers never ship. |
| 5 | **One online evening** | Dimitri and a friend | `01` [Online] (2 to 4 players, everyone the same version) applies to Release 1: friends who get the zip will play together. Nothing in this project has run online yet. | Nothing in the scope; it needs a friend. Six checks in the test plan §6 (the laser and Cordyfriend tint on a client, D-MDS' shot-down copy, Connected Roots with two Yossele players, debuff looks, the Tree and Bonsai slots per player). Best after item 1, on the same build. |

**Housekeeping that makes the above cheaper (minutes, no new work):** merge PR #45 first (it corrects the scope's own
status lines: on the default branch `01` still says "Yossele's next build comes next" and `02` still names 0.1.0-dev as
the play build), then PR #43 (the play-test report this list rests on). #40 and #42 are Spool and can wait. After #45, the
project-files copy of `02` and `16` still needs refreshing (its own TODO).

## Two corrections to how the open issues are usually listed

- **"One broken Yossele upgrade" is untested, not broken.** The play test bought 31 of 32 upgrades by hand; the 32nd,
  Picky Placement, "was on no route" (`PLAYTEST_2026-10-26.md` §2). Its rule passed the self-test (round 9e, `99_SelfTest.cs` "Picky Placement: 0.25% of the foe's max health a seed"). It wants one
  click in his play test, nothing more.
- **The HUD cut off at 2560x1440 may not be ours.** "MUSHROOM CANNON" showed as "USHROOM CANNON", but the stock vessels'
  bar wasn't compared. Before any fix: pick Smacky on the same screen. If Smacky's bar is cut too, it's the game's UI
  scale and the pack README can mention it; if not, it's Yossele's build-option names (longest of any vessel) and goes to
  the Yossele thread.

## Not now (after Release 1), and what each waits on

Ranked by how soon each is wanted after the release; none blocks Yossele.

| Area | State | Scope gaps that will block it |
| --- | --- | --- |
| Spool (PR #40, #42) | Built, 120/0; first Tide too thin (lost Purlest at Tides 4, 1, 2), walking Stitched hard to sell | `08`: `! NEEDS YARN STASH UPGRADES !`, Thread Head and Loom level 2-3 upgrades; 4 [Questions]; a call on the first Tide (more starting Thread, a free Stitched, or a cheaper Tin) |
| The mode (Phase 5) | Chart, weather, rejoin built; kit 1.7 then the main-menu button come first | `04`: `! NEEDS WHICH MODIFIERS AND ENEMIES CAN CHANGE FROM CAMPAIGN TO CAMPAIGN !`, 1 [Question]; Pure Aura's shop needs `12` and `14` below |
| Directions | 3 Yossele, a few Spool and Any written; Fertilize and Compost Bin built | `11`: Directions for every vessel, Argo's, Yossele's, Spool's, Uloe's all `! NEEDS !`; Dandelion Season and Mother of Mushrooms have no Numbers |
| Cardinals, objectives | One of each (Vampire Dentures, Mind the Meteor), no numbers | `12`: the list of new Cardinals, which stock ones the shop sells, Team Cardinals; `14`: the objective list and pay |
| Enemy modifiers (Phase 4) | Four names, rules in words | `13`: every Numbers and Shows as slot blank; Kind is a default |
| Argo | 2.0 built, 139/0; ships inside the pack later | `09`: `! NEEDS BALANCE PASS !`; Salt Sting text vs code (PR #45's call) |
| Uloe | Not designed | `10`: most slots blank, 1 [Question]; a design gate first |

## Recommendation

Start items 1, 2 and 4 together: the PC round and the pack step are Claude's and only need his OK for the round; the
sprite ask is one message. Items 3 and 5 follow his play test. Ask nothing else of him until Release 1 ships: the
`! NEEDS !` lines above are for the parts after it.
