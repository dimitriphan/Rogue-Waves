# Code review at max (2026-11-05)

Dimitri asked for "code-review at max" (2026-11-05 05:08Z). Every part of the pack (Argo, Yossele, Spool, Uloe, the
mode, the foes, `Shared`, the Editor scripts) was read again against the engine's rules, adversarially, after the
2026-11-04 review (`CODE_REVIEW_2026-11-04.md`, 54 bugs) and its fixes (`CODE_REVIEW_FIXES_2026-11-05.md`). Nothing
listed there as left on purpose is repeated here. **Compiled here, not run on the PC**: §6 is the next round's list.

## 1. In short

- **58 findings**, none of them in the earlier lists: 32 in shipping code (a few of them looks only), 24 in self-test
  code, 2 in a build guard. 35 fixed in this PR (17 shipping, 2 build guards, 16 test), 23 left with a reason (§5).
- **The worst ones fixed:** the F-key fix never armed in play, because the vessel it last saw is destroyed before the new
  one comes (P1-1, kit 1.11, pitfall #157); a campaign that skipped its staging map kept replaying it with free picks
  (MODEA-1, MODEA-2); Cardinals bought in a campaign stayed in the lobby for the next stock game, free (MODEB-1); Voice in
  a Bottle's mid-map catch-up upgraded the Bouthouse, which passes it to Boutman (MODEB-5); a Pachira's share of a hit
  vanished when the shooter died in flight (P1-2); a last Stash falling while Spool was down spilled nothing (SPL-10).
- **Self-tests that could not fail** are the largest group: 24, of which 16 are now real checks or an honest SETUP line.
  The 2026-11-04 test round's "F key on a real Boutman death: Pass" came from one of them (KIT-2) with the fix unarmed
  (P1-1): F chose the vessel because the bug didn't happen that time, not because the fix ran.
- **Needs Dimitri (§5.1):** three calls.
- Kit 1.11 (`ModKitFocus`, `ModKitPack.Kit`), copied to `TDS_Vessel_Creator/kit/Shared`; pitfalls #157 and #158.

## 2. How it was done

- **Pass cap:** one first pass over everything (the `/code-review max` skill), then one pass by seven reviewers, one per
  area (mode 00-10; mode 11-21; kit, foes and the mode's test files; Argo; Yossele; Spool; Uloe), at most 15 findings
  each; then each finding read again here before it was fixed or left; then `/code-review high` on the fix diff. No
  further passes.
- **An issue** is a concrete state that gives wrong behaviour, an exception, a leak that grows without end, a host and
  client that disagree, or a self-test check that can't detect what it says it checks. Each has a scenario. Style,
  balance numbers and matching the scope are out (a parallel thread audits the scope).
- **Engine facts** were read from the game's own code, disassembled into the thread's scratch folder (never committed),
  and the kit's pitfalls (`TDS_Vessel_Creator/docs/09_PITFALLS.md`), `docs/17` §8/§11, `docs/13`, `docs/15`, `docs/19`.
- **Confidence:** CONFIRMED is traced end to end with the engine fact checked; PLAUSIBLE has one fact unchecked, named.
- Ids: `P1-n` the first pass; `MODEA`, `MODEB`, `KIT`, `ARG`, `YOS`, `SPL`, `ULO` the second. They are this report's own
  (the 2026-11-04 review's ARG-1 or YOS-1 are other bugs); the code names each fix `MAX <id>`.

## 3. Fixed

Shipping code:

| Id | Where | Rule broken | Conf. | What a player met | The fix |
| --- | --- | --- | --- | --- | --- |
| P1-1 | `Shared/ModKitFocus.cs:50` | #155, #6: a destroyed object reads `== null` | CONFIRMED | The F-key fix never noticed a respawn, so F could still pick a Boutbro or Argo's crew | the respawn is noticed by reference; a new map's first vessel is no respawn; `Installed` for the self-test (kit 1.11, pitfall #157) |
| P1-2 | `Yossele/Code/12_Hooks.cs:272` | #6: `??` keeps a destroyed reference | CONFIRMED | A Pachira-shared building took 35% of a hit and the Pachira 0% when the shooter died while its shot flew | the fallback is picked with Unity's `!=` |
| P1-3 | `RogueWavesMode/Code/09_Rejoin.cs:364` | logic (MODE-B2's own rule) | CONFIRMED | A thing spawned in the two frames after a rejoiner loaded was started twice (two looks, two handlers) | only halves already without a Master at the load are restarted |
| P1-7 | `RogueWavesMode/Code/21_Difficulty.cs:115` | kit `docs/13` §7: register every upgrade on every machine | CONFIRMED | From map 2 a friend's log filled with "Could not retrieve Upgrade/RW_Foe_Health…" on every foe selected | the health upgrades for maps 2-10 are built with the content, on every machine |
| P1-8 | `RogueWavesMode/Code/18_Shop.cs:352` | logic (throttle) | CONFIRMED | A purchase's Pure Aura showed on the counter up to 0.5 s late | `RWRun.TickNow` |
| P1-9 | `RogueWavesMode/Code/18_Shop.cs:111` | efficiency | CONFIRMED | Thousands of strings a second all map long to find one stand | the being is compared as the synced struct |
| MODEA-1 | `RogueWavesMode/Code/06_Campaign.cs:204` | logic (campaign state) | CONFIRMED | Sailing straight to a row-1 map kept the staging flag: every later boat opened the harbour by itself and replayed the staging map with free Direction picks | a counted chart map ends staging |
| MODEA-2 | `RogueWavesMode/Code/05_Boat.cs:165` | logic (stale state) | CONFIRMED | A row-1 node picked before the harbour stayed "next": the staging map played as an uncounted map with no picks | picking the harbour clears "next" |
| MODEB-1 | `RogueWavesMode/Code/05_Boat.cs:141`, `17_Cardinals.cs:388` | engine: `Game_Init.Loadgame` loads the lobby's Cardinal list on every map | CONFIRMED | Cardinals bought in an ended campaign came free in the next stock game; after a quit, a later lobby lost its own picks of the same Cardinal | the lobby list is restored when the campaign is over, and the mode forgets its ids at the main menu |
| MODEB-3 | `RogueWavesMode/Code/20_Vote.cs:544` | logic (host vs client) | CONFIRMED | On the staging map a friend's map vote showed row 2's maps | the staging map's vote is on row 1 on every machine |
| MODEB-5 | `RogueWavesMode/Code/17_Cardinals.cs:313` | kit `docs/17` §11, pitfall #85; the stock `Voiceapply` filter | CONFIRMED (filter), PLAUSIBLE (harm) | Voice in a Bottle bought mid-map reached the Bouthouse, and so Boutman | the catch-up uses the stock filter: `Tower`, `Trap` or `Mob` |
| ULO-4 | `Uloe/Code/07_Building.cs:43` | `Game_Entity.Despawn` destroys last; #6, #63 | PLAUSIBLE (frame order) | A Bank sold or lost in the frame it was due a Photon made one that never went | a building's brain does nothing once its entity is Dead |
| ULO-7 | `Uloe/Code/06_HeroBrain.cs:289` | logic (ULO-1's fix) | CONFIRMED | A sold Bank, or Uloe dying, said "The cord snapped" | only a cord pulled past its length snaps |
| ULO-10 | `Uloe/Code/07_Building.cs:251` | #155 family | PLAUSIBLE (a node in reach going) | A Flood Light's cord (and Wide Flood's ring) stayed drawn 12 s after its node went | the cord and ring are taken off when the node goes |
| SPL-8 | `Spool/Code/07_Building.cs:62` | logic (start order) | CONFIRMED | A Thread Head back with Patchwork Job showed MEND OFF while mending; each upgrade it came back with played a sound | `Started` is set after the upgrades it comes back with |
| SPL-10 | `Spool/Code/05_Server.cs:193` | logic | CONFIRMED | A last Stash (or Loom, or Head) falling while Spool was down spilled nothing | the dying thing's own entity sends the look; with no radio at all the Thread still lies there |
| YOS-5 | `Yossele/Code/06_HeroBrain.cs:224` | logic | PLAUSIBLE (reaching it) | FERTILIZE on a building not begun or being felled spent a fertilizer for nothing | refused as no target |
| P1-6 | `RogueWavesMode/Code/92_Gaps.cs:415` | build guard | CONFIRMED (compiled) | The gaps debug build didn't compile without a play build (3 errors) | the two helpers moved out of `#if MODKIT_PLAY` |
| ARG-7 | `Argo/Code/10_Client.cs:202` | build guard | CONFIRMED (compiled) | `ARGO_REVIEW` alone didn't compile | `#if ARGO_SELFTEST && ARGO_REVIEW` |

Self-test and debug code (a check that couldn't fail or skipped without a word):

| Id | Where | What it did | Now |
| --- | --- | --- | --- |
| P1-5 | `Argo/Code/99_SelfTest.cs:1988` | looked for "ModKitFocus", which the build renames: always skipped | runs when Argo's copy installed the fix, else a SETUP line |
| KIT-1 | `RogueWavesMode/Code/92_Review.cs:232` | the MODE-D1 probe read SEEN on the fixed build | reads the payout lines |
| KIT-2 | `RogueWavesMode/Code/92_Gaps.cs:1584` | the F-key check ran in the killing hit's frame | waits for the death |
| KIT-3 | `RogueWavesMode/Code/92_Gaps.cs:994` | "row keeps to 8" with one Cardinal held | a SETUP line until a test holds 9 (§5) |
| KIT-4 | `RogueWavesFoes/Code/99_SelfTest.cs:158` | Berserk's look passed on the Aging foe's ring | counts the Berserk foe's own |
| KIT-6 | `RogueWavesMode/Code/92_Gaps.cs:766` | `Check(true)` for SONAR | counts errors around the press |
| KIT-7 | `RogueWavesMode/Code/93_TwoPlayers.cs:1113` | N4 passed with under 20 foes | SETUP when the fight is small |
| KIT-8 | `RogueWavesMode/Code/98_SaveFuzz.cs:140` | F4 never asked for an empty player id | asks |
| ARG-4 | `Argo/Code/99_SelfTest.cs:1190` | `Check(true)` for Deep Water's being | an INFO line |
| ARG-5 | `Argo/Code/99_SelfTest.cs:1715` | the stun passed on a foe with no attack speed | SETUP for such a foe |
| ARG-6 | `Argo/Code/99_SelfTest.cs:1469`, `:1578` | Brine, Salt Sting, Crushing Depth, Bedrock dropped out | SETUP lines |
| SPL-9 | `Spool/Code/99_SelfTest.cs:971` | "Loom strikes ore" passed with no ore (none on the test map) | SETUP |
| YOS-1 | `Yossele/Code/99_SelfTest.cs:1069` | a broken Entangling Roots read as SETUP | FAIL when the foe stayed on the vines past the time |
| YOS-6 (a) | `Yossele/Code/99_SelfTest.cs:618` | "Growth stops at the maximum" read a clamped value | no growth step may run at the maximum |
| ULO-5 | `Uloe/Code/99_SelfTest.cs:661` | "no mark left" read 0 on any destroyed foe | asks the mark table (`UloeMarks.Holds`) |
| ULO-9 | `Uloe/Code/99_SelfTest.cs:1165`, `:651` | Flash Bulb and Illuminate checks dropped out when the foe died | SETUP lines |

**Self-test counts change:** Argo one pass fewer (ARG-4 is a reading now), and one more when Argo's copy runs the F-key
fix (else a SETUP); Spool one pass fewer and one SETUP on the test map (SPL-9); the mode's gaps run one pass fewer and one
SETUP (KIT-3). Every other count is unchanged unless a check now fails, which is the point. Round 3's counts were Yossele
177, Spool 135, Argo 144, Uloe 152, Foes 12, mode 156.

## 4. Choices made

- **P1-1, the F key:** a map change resets what was last seen, so a new map's first vessel isn't taken for a respawn
  (the `/code-review high` pass caught that first version).
- **KIT-3:** made honest (SETUP) rather than buying nine Cardinals mid-test, which would change what every later check in
  that run plays with.
- **YOS-3 left:** a same-team filter on fruit would also shut out a friend on another team, and scope 07 says any vessel
  or minion picks fruit; it is a call (§5.1).

## 5. Left, and why

### 5.1 Needs Dimitri

| Id | The question | Claude's default |
| --- | --- | --- |
| MODEB-8 | Made of Tougher Stuff skips Boutbro (tagged `Hero`); kit `docs/17` §8 counts him among Boutman's minions. Should he get it? | yes, as the kit's table says |
| MODEA-3 | If the host hands the boat's lead to a friend, the replay rule, the staging harbour and the auto-sail stop working. Should a campaign keep the lead with the host? | yes, give it back, said once in chat |
| YOS-3 | A mad beast on a hunt map (a neutral `Mob`) can pick up aura fruit and its pay is lost. Only players' vessels and minions? | yes, leave out neutral beings |

### 5.2 A fact from the PC decides it

| Id | Where | Conf. | What | The check |
| --- | --- | --- | --- | --- |
| P1-4 | `RogueWavesMode/Code/13_Card.cs:170` | PLAUSIBLE | A friend's end screen may keep the stock title (pitfall #129's fix is host-only) | two players: read the friend's Win/Lose title |
| MODEB-2 | `16_Directions.cs:236` | CONFIRMED | Cut Costs never reaches Boutbro's fish, barrel and boat prices (MODE-C6 was incomplete) | which way stays stable through Boutbro's recounts |
| MODEB-4 | `17_Cardinals.cs:199` | PLAUSIBLE | A stock Cardinal already picked in the lobby can be bought again for nothing | whether a lobby pick puts the same core upgrades |
| MODEB-6 | `16_Directions.cs:246` | PLAUSIBLE | Cheap Refits skips upgradeable traps (Speed Pad, Checkpoint) | a trap's commands |
| MODEB-7 | `20_Vote.cs:122` | PLAUSIBLE | The opening picks' 90 s fallback can land VENDCINT mid-Tide | whether a session can lack a vessel that long |
| ARG-1 | `Argo/Code/08_NautBrain.cs:63` | PLAUSIBLE | With Full Crew, a Naut that surfaces over 2.5 tiles from its tower may dive forever | whether tile neighbours include diagonals |
| ARG-2, ULO-6 | `Argo/Code/07_TowerBrain.cs:132`, `Uloe/Code/07_Building.cs:82` | CONFIRMED (code) | Build looks, sounds and Photon pops sent in the first frame are mostly lost (pitfall #158) | watch a building go up |
| SPL-11 | `Spool/Code/09_Head.cs:120` | PLAUSIBLE | A Head sent home to a Stash it can't reach stands still for good | whether own walls block its path |
| SPL-7 | `Spool/Code/06_HeroBrain.cs:55` | PLAUSIBLE | A Blurry Photo copy of Spool would take the player's Thread counters | whether the base game lets it be equipped with a pack vessel |
| YOS-4 | `Yossele/Code/11_Pachira.cs:315` | PLAUSIBLE | Fruit never merges, so a long map can pile up hundreds of synced fruit | real payout rates on a long map |
| KIT-9 | `RogueWavesMode/Code/96_Flow.cs:587` | CONFIRMED (no-op) | The flow's clean-up of test Directions removes nothing | its later expected values need one run |
| KIT-5 | `RogueWavesMode/Code/99_SelfTest.cs:565` | CONFIRMED | R4 can't fail on vessel or Activeplayer | needs a separate pretend seat, proven on the PC |

### 5.3 Test code worth a later pass

KIT-10 (W2 passes at time scale 0), ARG-3 (the drill's checks count bites, not ore mined), ARG-8 (melee hits count as
projectile hits), SPL-12 (three Spool checks on a destroyed Loom or a missing tile), YOS-2 (the Seed Packets refund can
only pass or SETUP), YOS-6 (b) (the Mushboom's range mod stays after its ranks go; only after a test removes them),
ULO-8 (four Uloe checks compare the formula with itself). Each needs a new reading or a set-up foe, best written with a
PC round to see it pass. ARG-9 (a moved tuning Naut's sparks stay on the old tower) is cosmetic and needs a synced tower
id.

## 6. PC test list

First bring the PC up to this PR; every part logs "kit 1.11". Then each part's self-test (counts in §3).

| Id | Do | Expect |
| --- | --- | --- |
| P1-1 | Let a vessel with minions tagged Hero (Boutman, or Argo with crew out) die and respawn; press F once | the vessel is selected; the log's F-key fix count goes up by one |
| P1-2 | A Pachira-connected building; a ranged foe shoots it and dies before the shot lands | the Pachira takes its 65% share |
| P1-3 | Rejoin mid-Tide while foes spawn | no thing with two looks; the "started again" count is only stopped ones |
| P1-7 | Two players, map 2; the friend selects a foe | no "Could not retrieve Upgrade/RW_Foe_Health" in the friend's log |
| MODEA-1 | New campaign: sail straight to a row-1 node and play it | on the next boat the harbour stays shut |
| MODEA-2 | New campaign: click a row-1 node, back out, let the harbour open, ready | the staging map holds its picks and vote |
| MODEB-1 | Win or lose a campaign holding a bought stock Cardinal; start a stock map | the lobby's Cardinal row has only the lobby's picks |
| MODEB-3 | Two players on the staging map: the map vote | the friend's cards are row 1's maps |
| MODEB-5 | As Boutman with a Bouthouse up, buy Voice in a Bottle mid-map | the Bouthouse's upgrade list has no Voice entry |
| ULO-7 | Plug into a Bank, sell it | no "The cord snapped" |
| ULO-10 | A Flood Light on a node that runs out | its cord goes at once |
| SPL-8 | A Head with Patchwork Job and MEND on dies and comes back | MEND reads On |
| SPL-10 | Spool down, its Stash its last building and holding Thread, foes kill the Stash | Thread tufts on the ground |

## 7. How it was checked

From `TDS_Mod/ModSource`, after `TDS_Mod/Tools/cloud-check/setup.sh` once, on the base with PRs #98 and #99 merged in:

- `python3 ../Tools/cloud-check/check_mod.py <Part>/Code Shared <Part> ""`: Argo, Yossele, Spool, Uloe, RogueWavesMode,
  RogueWavesFoes all 0 errors, 0 warnings.
- With `<PART>_SELFTEST,<PART>_SHOTS,<PART>_REVIEW` for the four vessels, `ARGO_REVIEW` alone, the foes with
  `ROGUEWAVESFOES_SELFTEST,ROGUEWAVESFOES_SHOTS`, the mode with `_SELFTEST,_SHOTS`, `_SELFTEST,_GAPS` and `_SELFTEST,_FEEL`:
  0 errors. Warnings: the base's `96_Flow.cs:311` and `92_Gaps.cs:791` (unused locals). The mode's `TWOPC` and
  `DIALOGLOOK` builds don't compile here (the stubs' known limits), so KIT-7's edit is compiled on the PC only.
- `../Tools/cloud-check/check_editor.sh ../Assets/<Part>Mod/Editor` for all six: "editor check done".
- `Shared` equals `TDS_Vessel_Creator/kit/Shared` (`diff -r`, no output).

Nothing was run in game. `/code-review high` on the fix diff found nine things; fixed: the F-key fix took a new map's
first vessel for a respawn; a FAIL that could fire on a foe that stepped off the vines and back; Argo's F-key skip as a
plain log line; the kit README and pitfall number missing; a doubled guard in Uloe and Spool; a comment above the wrong
method. Left as a choice: the fruit filter (now §5.1) and KIT-3's SETUP (§4).
