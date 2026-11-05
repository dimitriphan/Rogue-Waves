# Code review against the engine (2026-11-04)

Quality check 1 of the list Dimitri started on 2026-11-04 ("Start with 1, 2, and 3"): every part's code read against
the kit's engine rules and the decompiled game, to find real bugs. Cloud only: nothing here was run in game.
**Paused early** at Dimitri's word ("Once every thread is at a good stopping point, pause activity", 23:22Z). The
first pass reached every part's shipping code; the planned second pass over the mode and the shared kit was not
started (§7). Every finding is written out in full, by part, in `CODE_REVIEW_2026-11-04_FINDINGS.md`.

Frozen as written. The one change made with it is the build repair in §2; no finding below is fixed yet.

## 1. In short

- **The base didn't compile.** Since PR #82 merged, the mode's release build has failed with 6 errors, and the
  self-test builds of the mode, Spool, Argo and Uloe fail too. Built that way, the mode would be missing in game. This
  PR repairs it, and every part compiles at 0 errors again (§2).
- **55 findings, 54 different bugs.** Two reviewers found the bonus-break double pay separately (MODE-D1 and
  SHARED-1). Of the 54, 32 are **confirmed** from the mod's code and the game's, and 22 are **plausible**: each of those
  says what would decide it. None has been seen in game yet.
- **By kind:** 40 wrong behaviour, 8 online only, 2 save, 2 crash (both plausible), 2 leak. In the debug files that
  were read, no self-test check passes while its feature is broken, but most debug files weren't read (§7). Five
  findings point to a self-test that doesn't look where the bug is: MODE-A1, MODE-D1, YOS-1, ULO-1 and ARG-1.
- **The worst for a player:** from Tide 29 on, every bonus break pays twice (MODE-D1). An objective left running at a
  map's end pays at the next map's start, also on a replay (MODE-D2). A question asked while a vessel is down never
  reaches its player, so two players can't vote to leave that break (MODE-D4). A quit right after the staging map
  brings the staging map back later (MODE-A1). §3 has the ten to fix first.
- **Re-read by the review's lead:** 11 of the top findings were checked again at `63af4b0`, and the engine half of 2
  more. All held up (the "Lead" column in §4). One reviewer's tag was wrong: PR #80 didn't move Yossele's spawn helper,
  so YOS-1 hits Yossele only.
- **Fixed here:** only the build (§2), because the pause came first. §5 sorts the fixes into what a code thread can do
  at once, what needs Dimitri's word, and what needs a fact from the PC first.

## 2. The base didn't compile (fixed in this PR)

**What.** On `63af4b0` (PR #82 merged after PR #80), the cloud check gives these results:

| Build | Errors on `63af4b0` | After this PR |
| --- | --- | --- |
| RogueWavesMode, release | 6 (`17_Cardinals.cs`: `RWUtil.Folder`, `Load`, `IsServer`) | 0 |
| RogueWavesMode, `_SELFTEST` | 10 | 0 (1 warning, as before) |
| Spool, `_SELFTEST` + `_SHOTS` | 151 | 0 |
| Argo, `_SELFTEST` + `_SHOTS` | 294 | 0 |
| Uloe, `_SELFTEST` + `_SHOTS` | 183 | 0 |
| Spool, Argo, Uloe, Yossele, Foes, release | 0 | 0 |
| Yossele and Foes, `_SELFTEST` + `_SHOTS` | 0 | 0 |

The game compiles each part when it loads it, so a mode part with 6 errors would fail to load. That means no
main-menu button, no boat and no campaign.

**Why.** PR #82's one commit (`f59ef26`) was made on the PC, whose copy of the code is older than PR #80. It replaced
seven files whole: `17_Cardinals.cs`, `92_Gaps.cs`, `93_TwoPlayers.cs` and `96_Flow.cs` in the mode, and the
`99_SelfTest.cs` of Spool, Argo and Uloe. With the new Cardinal fix and the new checks, those copies brought back
three older things:

- the helper names kit 1.8 had moved into `ModKitUtil` (`RWUtil.Folder`, `ArgoUtil.Pos`, `SpoolUtil.Stat` and so on);
- Uloe's checks of the stock-mark route PR #80 deleted;
- one comment the clean-up of PR #78 had changed.

Git saw no conflict, because the branch sat on top of PR #80.

**The repair.** The seven files were three-way merged: base `5d9b09e`, PR #80 (`06365ae`) and PR #82 (`63af4b0`).
PR #82's Cardinal fix and new checks are kept, and PR #80's renames and deletions are back. Compared with `63af4b0`
once the helper names are set aside, only two things differ: Uloe's stock-mark checks are gone again, as PR #80 left
them, and Argo's comment reads as PR #78 left it. The other debug builds compile as they did before PR #82
(`_GAPS`, `_CHARTLOOK` and `_SHOTS` with the mode's self-test: 0 errors). Three builds still report errors here only because
the cloud check's stubs don't carry Netcode's transport, `UnityEngine.EventSystems` or `InputSystem`: `_TWOPC`,
`_DIALOGLOOK` and `MODKIT_PLAY`. The cloud-check README names the first and the last, and all three show the same
errors at `5d9b09e`.

**So it doesn't happen again.** A change made on the PC goes back as a diff applied to the newest base, never as
whole files. That rule is now in `TDS_Mod/Tools/pc/README.md`. The PC's own `TDS_Mod` is still older than PR #80, so
the next PC round begins by bringing it up to the base (the handoff's manifest step does that).

## 3. The ten to fix first

Ranked by what a player meets, how sure the finding is, and how cheap the fix is.

| # | Id | What a player meets | Fix | Risk |
| --- | --- | --- | --- | --- |
| 1 | MODE-D1 | From Tide 29, every bonus break pays Pure Aura and Compound Interest twice and lands VENDCINT twice | `RWVote.Break` handles each Tide's break once per map | low |
| 2 | MODE-D2 | An objective left running when a map ends is judged at the next map's opening break, and can pay there, also on the replay of a lost map | clear `RWObjectives` in `RWVote.MapStart` | low |
| 3 | MODE-D4 | A pick, map vote or vote to leave asked while a player's vessel is down never reaches that player; with two players the party can't leave that break | keep each player's open question on the host and write it onto the respawned vessel | low to medium (online path) |
| 4 | MODE-A1 | A quit between the staging map and map 1 leaves the staging flag saved, so the boat later opens the staging map again (a free Direction pick, the column vote again) | one `RWCampaign.SaveChoice()` after the flag is cleared | low |
| 5 | MODE-C1 | A Cardinal bought mid-map reaches only what spawns later: Sport Water, Paper Tower, Beast Tassle and Fluffy Slippers do nothing on the map they were bought on | a catch-up pass after a mid-map grant (proven in game before) | low |
| 6 | MODE-D3 | "Mind the Vessels" fails whenever a Boutbro falls, so it is near unwinnable with Boutman | count only the player's own vessel | low |
| 7 | MODE-D5 | From map 2, every foe wears the Hardened elite tint, because the map's health bonus uses that elite's script | the map's health as the mode's own mod on each foe; until then, leave it out | risky |
| 8 | ULO-1 | Once the Power Bank Uloe is plugged into is sold or destroyed, PLUG IN stays stuck on UNPLUG and the bulb stays lit until the vessel dies | test the reference itself (`(object)Plug == null`) in two places | low |
| 9 | SPL-1, SPL-2 | A Thread Head with a full Yarn Stash turns round at the Stash every quarter second; RETURN with nothing carried parks it there | gate the auto-return on room; turn round on arrival whatever it carries | low |
| 10 | MODE-C4 | Cheap Refits rewrites the upgrade price every 0.5 s, so Voice in a Bottle's discount is lost | apply Refits as a factor, once per tower and rank | low to medium |

Next after those: MODE-D6 (a late Leave majority is thrown away when the next Tide starts), MODE-B1 (any drop or rejoin
puts Quickening Current back to x1 on every machine), YOS-1 (a Grafted Sapling's fruit always looks like the ALL
fruit), MODE-C6 (Cheap Refits never reaches the Bouthouse) and MODE-B3/B4 (a turned-away friend is told to join
again, and their next map's fly-in plays under the loading screen).

## 4. Every finding

C = confirmed, P = plausible. Lead: re-read by the review's lead at `63af4b0` (✓), or only its engine half (engine).
The full entry (where, why, fix, PC check) is under the same id in `CODE_REVIEW_2026-11-04_FINDINGS.md`.

**RogueWavesMode: shop, objectives, votes, difficulty** (its section 1)

| Id | What goes wrong | Kind | Conf. | Fix risk | Lead |
| --- | --- | --- | --- | --- | --- |
| MODE-D1 | From Tide 29, every bonus break fires twice: double Pure Aura and interest, VENDCINT lands twice | wrong | C | low | ✓ |
| MODE-D2 | An objective running at a map's end pays or fails at the next map's start, also on a replay | wrong | C | low | ✓ |
| MODE-D3 | "Mind the Vessels" fails whenever a Boutbro falls | wrong | C | low | ✓ |
| MODE-D4 | A question asked while a vessel is down never reaches its player | online | C | low to medium | |
| MODE-D5 | From map 2, every foe wears the Hardened look; real elites may lose theirs | wrong | C (look), P (elites) | risky | engine |
| MODE-D6 | The vote to leave doesn't hold the break: a late vote is cut off by the next Tide | wrong | C | low | |
| MODE-D7 | Closing a question also closes a SHOP or FUND dialog opened after answering | wrong, minor | C | low | |
| MODE-D8 | "Keep the Basin Clean" watches only the first Basin on two-Basin maps | wrong, minor | C | low | |
| MODE-D9 | A lobby Mooncore plus the bonus rewind can end a map as a Win at Tide 34, with no picks or vote | wrong | P | risky | |
| MODE-D10 | A VENDCINT dialog that throws leaves SHOP, FUND and BOARD dead for the launch | wrong | P | low | |

**RogueWavesMode: boot, mode, world, boat, campaign** (section 2)

| Id | What goes wrong | Kind | Conf. | Fix risk | Lead |
| --- | --- | --- | --- | --- | --- |
| MODE-A1 | The staging map's end isn't saved: after a quit before map 1, the staging map comes back | save | C | low | ✓ |
| MODE-A2 | One throw in three unguarded boot steps turns the whole mode off for the launch | crash | P | low | |
| MODE-A3 | "The Basin" can be a sub-core on five maps; Cardinals also go on sub-cores | wrong | P | low (one core), risky (all cores) | |

**RogueWavesMode: run state, Directions, Cardinals** (section 3)

| Id | What goes wrong | Kind | Conf. | Fix risk | Lead |
| --- | --- | --- | --- | --- | --- |
| MODE-C1 | A Cardinal bought mid-map reaches only later spawns | wrong | C | low | ✓ (mod) |
| MODE-C2 | Spring Tide's stock Beacon cancels Smacky's Beacon damage cut on towers built after it | wrong | C | risky | |
| MODE-C3 | Spring Tide's `Resistadd` 1 halves the damage Fie's grown and Shield walls take (known: PR #80's check R3) | wrong | C | risky | |
| MODE-C4 | Cheap Refits wipes Voice in a Bottle's discount | wrong | C | low to medium | ✓ |
| MODE-C5 | Tidal Mend's heal is frozen at the tower's level when the effect started | wrong | C | low to medium | |
| MODE-C6 | Cheap Refits skips the Bouthouse; Cut Costs misses Fie's wall-built segments | wrong | C | low | |
| MODE-C7 | Two static dictionaries grow every map and keep dead vessels | leak | C | low | |
| MODE-C8 | The Directions' spawn hook upgrades inside the game's spawn with no try | crash | P | low | |
| MODE-C9 | One player can get two save keys, which would lose their run on load | save | P | low | |
| MODE-C10 | A rejoiner's Cardinal row may not line up with the host's counters | online | P | low | |
| MODE-C11 | Cardinals past the HUD's 8 slots appear nowhere | wrong | P | low | |
| MODE-C12 | Voice in a Bottle gives nothing on a map of more than 30 waves | wrong | P | low | |

**RogueWavesMode: weather, rejoin, online, dialogs, menu** (section 4)

| Id | What goes wrong | Kind | Conf. | Fix risk | Lead |
| --- | --- | --- | --- | --- | --- |
| MODE-B1 | Any drop, rejoin or turned-away join puts Quickening Current back to x1 everywhere | online | C | risky | engine |
| MODE-B2 | A rejoin's repair restarts client halves still alive: doubled looks and handlers | online | C (path), P (effect) | low | |
| MODE-B3 | A turned-away client's catch-up outlives its map: the next fly-in plays under the loading screen | online | C | low | |
| MODE-B4 | A turned-away player's main menu says to Join Game again, which turns them away again | online | C | low | |
| MODE-B5 | A weather map left while its start waits can put its popup and flood or nest on the next map | wrong | P | low | |
| MODE-B6 | The rejoin's "what died" flags outlive their map; the client mode could start twice | online | P | low | |

**The shared kit and the foes part** (section 5)

| Id | What goes wrong | Kind | Conf. | Fix risk | Lead |
| --- | --- | --- | --- | --- | --- |
| SHARED-1 | The kit side of MODE-D1: a rewind of Entry inside a break fires Break again | (same bug) | C | low | ✓ |
| SHARED-2 | The Tide poller fires a stray Tide 0 at each map's start | wrong, low | C | low | |
| FOES-1 | A vessel that dies in a Dazzling aura comes back Illuminated (+20% taken) for the map | wrong | P | low | |
| FOES-2 | Illuminate on a Bouthouse goes on to Boutman and is never taken off | wrong | P | low | |
| FOES-3 | A foe with both Aging and Berserk loses Berserk's damage growth | wrong | P | low | |

**Yossele** (section 6)

| Id | What goes wrong | Kind | Conf. | Fix risk | Lead |
| --- | --- | --- | --- | --- | --- |
| YOS-1 | A Grafted Sapling's fruit always looks like the ALL fruit | wrong | C | low | ✓ |
| YOS-2 | Monument's Cleanse also strips the downside of stock artifacts such as Paper Tower | wrong | P | risky | |
| YOS-3 | A burst of deaths can push one owner past the Cordyfriend cap of 30 | wrong | C | low | |
| YOS-4 | The vines' slow look may not load on some maps (pitfall #114) | wrong, look | P | low | |

**Spool** (section 7)

| Id | What goes wrong | Kind | Conf. | Fix risk | Lead |
| --- | --- | --- | --- | --- | --- |
| SPL-1 | With Threaded Delivery and a full Stash, a Thread Head never leaves the Stash | wrong | C | low | ✓ |
| SPL-2 | RETURN on a Thread Head carrying nothing parks it at the Stash | wrong | C | low | ✓ |
| SPL-3 | TARGET is taken unchecked while the Tin has no live Crafted | wrong | C (no check), P (unreachable) | low | |
| SPL-4 | One exception stops a Spool brain for good (pitfall #80's pattern) | wrong, silent | P | low | |
| SPL-5 | A friend who joins or rejoins mid-map never sees Thread already on the ground | online | C | risky | |
| SPL-6 | A second Yarn Stash placed within half a second leaves an orphan | wrong, edge | P | low | |

**Argo** (section 8)

| Id | What goes wrong | Kind | Conf. | Fix risk | Lead |
| --- | --- | --- | --- | --- | --- |
| ARG-1 | Two overlapping Anchor Line stuns can make a foe's attack speed negative, so it attacks every tick | wrong | P | low (a bound), risky (the stock stun) | |
| ARG-2 | The slow look blinks on foes in Deep Water and the Tremor | wrong, look | P | low | |
| ARG-3 | Another source's shorter slow look cuts Argo's short, and Argo doesn't send it again | wrong, look | C | low | |
| ARG-4 | The AIM picker's reach and ring go stale while the Drill Rig stays selected | wrong, UI | C | low | |
| ARG-5 | The sound mixer is searched once; a miss would stick for the session | wrong, sound | P | low | |
| ARG-6 | One dictionary keeps every recoiled turret for the session | leak, tiny | C | low | |

**Uloe** (section 9)

| Id | What goes wrong | Kind | Conf. | Fix risk | Lead |
| --- | --- | --- | --- | --- | --- |
| ULO-1 | PLUG IN sticks on UNPLUG once the plugged Power Bank is sold or destroyed | wrong | C | low | ✓ |
| ULO-2 | One exception switches a Uloe brain off for good (pitfall #80's pattern) | wrong, latent | P | low | |
| ULO-3 | With two Uloe players, Light reaches the teammate's buildings where the design says "your buildings" | wrong | C (against the design) | low, once intended | |

## 5. What to fix, in batches

**A. Low-risk and local: a code thread can do these in one PR, with the cloud check, then one PC round.**
MODE-D1, D2, D3, D6, D7, D8, D10; MODE-A1, A2, A3 (`PlayerCore` only); MODE-B2, B3, B4, B5, B6; MODE-C1, C6, C7, C8,
C11; SHARED-2; FOES-1, FOES-2, FOES-3; YOS-1, YOS-3, YOS-4; SPL-1, SPL-2, SPL-3, SPL-4, SPL-6; ARG-2, ARG-3, ARG-4,
ARG-5, ARG-6; ULO-1, ULO-2. Each entry's Fix line says what to change. Three of them are best done once in the kit
(with a kit version bump and the copy in `TDS_Vessel_Creator/kit/Shared`):

- one brain-fault guard for all four vessels, from Argo's and Yossele's (SPL-4, ULO-2);
- the stun recipe in `docs/05` (ARG-1);
- the `SpawnOnTile` recipe in `docs/05` (YOS-1).

**B. Needs Dimitri's word first** (each changes how something plays):

- ULO-3: should two Uloe players share Light, or does it go only to your own buildings?
- MODE-C2, C3 and C5: Spring Tide and Tidal Mend built on the mode's own mods instead of stock scripts, which changes
  their numbers on some towers. C3 is the same question as the simplification build's finding 1.
- MODE-D5: the map's extra foe health without the elite look, or no extra health until that is built.
- YOS-2: should Monument's Cleanse leave a stock artifact's trade-off alone?
- MODE-D9: are lobby Cardinals such as Mooncore allowed in the campaign?

**C. Needs a fact from the PC first** (its PC check decides it): ARG-1 (enemy `Attackspeed` limits), MODE-C9 (session
ids at every point a key is made), MODE-C10 (how a joiner builds its Cardinal list), MODE-C12 (`Totalwaves` on a
campaign map), MODE-A3 (the Basin and sub-cores' order on Artrio), FOES-1 (the respawn's copied upgrades).

**D. Online, medium or risky:** MODE-D4 (the questions), MODE-B1 (the game speed on every machine), SPL-5 (Thread a
late joiner can't see). Each needs a two-player round to prove.

## 6. PC checks

**First:** bring the PC's `TDS_Mod` up to this PR's base. It is older than PR #80 (§2), so a round on it would test old
code. Then build each part with `-Define <NAME>_SELFTEST` as usual. Each line below says what to do and what to read;
the full check is in the finding's entry. Today's result is what the code predicts.

**Solo, the mode** (host; most are quickest from the self-test's hooks):

| Id | Do | Read: today / when fixed |
| --- | --- | --- |
| MODE-D1 | Stay past Tide 29 (force the Tides on, pitfall #60) | "Tide N paid ... Pure Aura" and "VENDCINT landed" twice per break from Tide 29 / once |
| MODE-D2 | Take Mind the Vessels in the break where the party votes to leave; start the next map | "paid 2 Pure Aura each: Mind the Vessels" at its opening break / no objective line before the first "posted:" |
| MODE-A1 | Play a new campaign's staging map through its vote; on the boat read `RW_Campaign_Staging` | "1" / "" |
| MODE-D3 | As Boutman, take Mind the Vessels and let a Boutbro die | "Hands failed" / "Hands done" |
| MODE-D5 | On map 2, spawn a plain Tide-1 foe; read its `Skin.Value` | "Health" with the Hardened tint / "" |
| MODE-D6 | Let a bonus Tide time out with a foe left, kill it 45 s into the break, vote Leave | no "the vote to leave:" line and the next Tide starts / "1 of 1 to leave" |
| MODE-D8 | On Splithead, take Keep the Basin Clean and let a foe hit only the second Basin | done / failed |
| MODE-D9 | Pick Mooncore in the lobby and stay to Tide 34 | the map ends "Win" with no vote / plays on |
| MODE-C1 | Mid-map, buy Sport Water and Rusty Pipe; read the vessel's and an older tower's `Upgradelistserver` | their effect entries missing / present |
| MODE-C2 | As Smacky, take Spring Tide; build one tower before and one after; hit both the same | the later tower takes 1.5x / the same |
| MODE-C3 | As Fie with Spread5 and Spring Tide, read a grown segment's `Resistadd` (and PR #80's R3) | 1 or more / absent |
| MODE-C4 | Hold Voice in a Bottle and Cheap Refits; after two Tides read a tower's `Upgrade` Goalvalue | exactly the Refits price / lower by Voice's steps |
| MODE-C5 | Take Tidal Mend rank 1 beside a level-3 tower, then build a new tower; damage both, end a Tide | heals 15 and 5 / as designed |
| MODE-C6 | As Boutman with Cheap Refits, read the Bouthouse's `Upgrade` Goalvalue | 1 / the Refits price |
| MODE-C12 | Log `Mode.Enemies.Totalwaves` on a campaign map | above 30 means Voice gives nothing |
| MODE-A3 | On Artrio, log `PlayerCore()` and every `PlayerCores()` being at the opening break and Tide 1 | a sub-core's name confirms it |
| SHARED-2 | A solo map with Yossele: the first "Tide" line after "GAME START" | "Tide 0" / "Tide 1" |
| MODE-A2 | In a dev build, make one boot step throw on purpose | no "ready" line and no menu button / a warning, then "ready" |

**Two players** (`Run-TwoPlayers.ps1`):

| Id | Do | Read: today / when fixed |
| --- | --- | --- |
| MODE-D4 | Let the friend's vessel drown in the last seconds of Tide 15 | its owner's `RWAsk.Open` stays "", the host logs "1 of 2 to leave" / the dialog opens on the respawned vessel |
| MODE-D7 | Answer a pick, open SHOP, have the other player answer | the shop closes / stays |
| MODE-D10 | Open FUND as the friend drops, then press SHOP | nothing opens if it threw |
| MODE-B1 | On a Quickening map (QuickTide lowered), drop the friend and rejoin; log `Time.timeScale` and `Time_Scale` on both | 1 against 1.25 everywhere / 1.25 |
| MODE-B2 | Rejoin mid-Tide (cable pull, then restart); count each visual script per client half and duplicates in `Entityclient` | doubles for what spawned in that window / none |
| MODE-B3 | Join mid-map, be turned away, join on the boat, start the next map within 4 minutes | "rejoin: map loaded here" before "Loading 80%", no fly-in / no such line |
| MODE-B4 | Repeat the test round's O5 (a mid-map join turned away); read that client's main-menu error box | "Lost the map ... Join Game" / the stock text or "join on the boat" |
| MODE-B6 | Throw once from an end-screen frame on map 1 (test build); on map 2 rejoin after a restart | ", the end screen" in the "started again ..." line / not there |
| MODE-C9 | Log `RWRun.KeyOf` for host and client at the lobby, at MapStart and after a rejoin | two different keys for one player confirms it |
| MODE-C10 | Buy two Cardinals, rejoin the client, compare its Cardinal row and counters with the host's | rows differ confirms it |
| SPL-5 | Kill foes so Thread lies on the ground, then have the friend rejoin within 10 s | no tufts on the friend's screen / tufts |
| ULO-3 | Both on Uloe; the host's Flood Light beside the client's Glass, none of its own in reach | it aims at the client's Glass / nothing (if "your buildings" is meant) |

**The vessels and the foes:**

| Id | Do | Read: today / when fixed |
| --- | --- | --- |
| YOS-1 | A Grafted Sapling set to BLUE mines a node out at full growth | the fruit's `Skin.Value` "Growth<max>", three-colour model / "Blueaura", blue |
| YOS-2 | Paper Tower plus a Monument; let one foe hit it | its trade-off stats change after the hit / they stay |
| YOS-3 | Self-test: hold an owner at 29 friends, call `Raise` 5 times in one frame, wait 0.2 s | `CountOf` 34 / 30 |
| YOS-4 | A Yossele-only match on 3B_Woozy; vine a foe; grep the log | "Could not retrieve Vfx/Debuff_Slow" confirms it |
| SPL-1 | A Head with Delivery, the Stash at 50 with no Tin able to spend, 20 Thread at the Head's spot | after 10 s it stands about 1.5 from the Stash, `Returning` flipping / back at its spot |
| SPL-2 | A Head carrying nothing, far from drops: press RETURN | 10 s after it reaches the Stash `Returning` is still true / walking back |
| SPL-3 | A Tin with no Crafted alive: TARGET your own tower's tile within 8 tiles, then refill the Stash | the Stitched's `Arrived` stays false / refused or moved to a reachable tile |
| SPL-6 | Self-test: two Stash build JSONs 0.1 s apart, then destroy the newer Stash | "has a second Yarn Stash"; the menu goes back to Stash only while one stands |
| SPL-4, ULO-2 | Grep every round's Player.log | each `[Spool] ... brain:` or `[Uloe] ... brain:` line is a brain that stopped |
| ARG-1 | Log `Statsserver["Attackspeed"].Value_Bounds` for a few foes; stun one twice within 1 s | x at -infinity and a negative `Value` confirm it |
| ARG-2, ARG-3 | Watch a foe in Deep Water, in a Tremor, and next to a stock slowing tower | the slow look flashes or ends early / stays on |
| ARG-4 | Tune a Drill Rig with Calibrate, open AIM between the old and the new reach | "Too far" / "Shake the ground here" |
| ULO-1 | Plug into your own Power Bank, then sell it (and once more with foes destroying it) | UNPLUG stays and does nothing, bulb lit / PLUG IN within 1 s, `Skin.Value` "" |
| FOES-1 | A Dazzling Dagger beside your vessel; kill the vessel; after the respawn read its `Upgradelistserver`, land a 10 hit | Illuminate there, 12 taken / none, 10 |
| FOES-2 | The Bouthouse 2 tiles from a Dazzling Dagger, Boutman away; read Boutman's upgrades, despawn the foe, read again | Illuminate both times / gone |
| FOES-3 | A Dagger with Aging and Berserk at 50% health; read its Damage against a plain one's | base x (1 + share) / base x (1 + share + 0.5) |
| MODE-B5 | Skip the boat's preload for a Nesting Ground node, leave within 20 s, start a calm map | the nest's popup or nest on the calm map confirms it |

Still open from earlier lists: PR #80's T0-L1 (`SIMPLIFICATION_BUILD_2026-11-04.md` §4) and the second test round's
leftovers (Spool host with an Argo friend showing no debuff looks, Argo's Deep Current check, the Cardinal-return check).

## 7. Not yet reviewed

- **The second pass** over the mode and the shared kit was not started, and neither was the code-review skill's pass.
- **Debug-only files**, the hunt for false passes: the mode's `91_Feel`, `92_Gaps`, `93_TwoPlayers`, `94_DialogLook`,
  `95_Look`, `96_Flow`, `97_Shots` and `99_SelfTest`; Yossele's `97_Measure`, `97_Shots` and `98_Play`, and most of
  `99_SelfTest`; all of Spool's and Argo's; most of Uloe's `99_SelfTest` and its `97_Shots`. The foes part's self-test
  and the kit's `ModKitTest` and `ModKitPlay` were read: they are fenced, and nothing leaks into a release build.
- **Code merged during the review:** PR #80's new kit files (`ModKitUtil`, `ModKitAssets`, `ModKitUtil.Front`) were
  read only where a vessel's helper had been, and the switch of the pack buildings to the stock upgrade AI wasn't
  reviewed. Of PR #82, only the re-grant's wait for `PlayerCores()` was read (MODE-A3).
- **Facts that decide the plausible findings**, which the engine files read here couldn't settle: each entry's
  "plausible" line names its own, and each section's "Not yet reviewed" list in the findings file has the rest
  (for example `CanNest` and `Enemy_Halberd`, `RWDialog.Number` with a negative amount, keybind clashes on a panel, a
  one-shot node under PLUG IN).
- **Earlier reports** were searched only by keyword for findings already known: only MODE-C3 was (PR #80's R3).

## 8. How it ran

- The game's `Assembly-CSharp.dll` was decompiled again with the kit's recipe (`docs/13` §11, 703 types). The
  decompile stays in the thread's scratchpad: it is never committed.
- Nine reviewers, one area each, all with one brief. The brief held the engine rules (kit `docs/09` in full, `05`,
  `17` §2, §8 and §11, `19`, `23`; and `15` and `20`-`24` for the mode) and the issue definition: a defect that changes
  what a player sees, or that can crash. Style and "could be simpler" were out, and so was anything already known (the
  hand-kept hit hooks, the Cardinal re-grant PR #82 fixed, Deep Water's slow stacking, Salt Sting's text). Each
  reviewer had to confirm a finding from the code or name what would decide it. The planned limit was every file
  once, then one second pass over the mode and the shared kit.
- Every part compiled at 0 errors at `5d9b09e` before the review. The 5-hour usage limit stopped every reviewer from
  20:41Z to 23:40Z. They then wrapped up at the pause, and the base had moved to `63af4b0` by then (§2).
