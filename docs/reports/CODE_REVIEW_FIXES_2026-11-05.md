# Code review fixes (2026-11-05)

The confirmed findings of `CODE_REVIEW_2026-11-04.md` built, highest impact first, at Dimitri's "resume with new
project instructions and plugins in mind" (2026-11-05 00:30Z). Each fix is named in the code by its finding's id
(`MODE-D1`, `ULO-1`...), and each finding's full entry is in `CODE_REVIEW_2026-11-04_FINDINGS.md`. **Compiled here,
not run on the PC**: §5 is the test list for the next PC round.

## 1. In short

- **32 of the 54 bugs fixed** (33 ids: MODE-D1 and SHARED-1 are one bug): 26 of the 32 confirmed ones, and 6 plausible
  ones whose fix is a guard that can't change a working path (MODE-A2, MODE-C8, MODE-D10, ARG-5, FOES-2, FOES-3).
- **Kit 1.9** carries three of them (SHARED-2, ARG-3, ARG-5), copied to `TDS_Vessel_Creator/kit/Shared`, with the version
  row in `kit/README.md` and five new pitfalls (#151-#155) for the engine facts the fixes rest on.
- **One call made without asking:** the map's extra foe health stays, now without the Hardened look (MODE-D5, §3). The
  numbers are the design's, so nothing in play changes but the look. The review listed it as his call; say if you'd
  rather have no extra health until it's seen in game.
- **Left:** 22, each for a reason in §4: his word (6), a fact from the PC (5), refuted by the review (1), online and
  risky (1), or a plausible bug whose fix isn't plainly safe (9).

## 2. What was fixed

| Id | What a player met | The fix | Where |
| --- | --- | --- | --- |
| MODE-D1, SHARED-1 | From Tide 29 every bonus break paid Pure Aura and interest twice and landed VENDCINT twice | `RWVote.Break` handles each Tide's break once; a Tide clears the guard, so a replayed Tide still gets its break | `20_Vote.cs` |
| MODE-D2 | An objective running at a map's end paid or failed at the next map's opening break | `RWObjectives.Reset` at every MapStart | `19_Objectives.cs`, `20_Vote.cs` |
| MODE-D3 | "Mind the Vessels" failed whenever a Boutbro fell | only the player's own vessel counts (`RWRun.IsVessel`: tagged `Hero`, not a clone, the player's own being) | `14_Run.cs`, `19_Objectives.cs` |
| MODE-D4 | A pick or a vote asked while a vessel was down never reached its player | the host keeps each player's open question and run line and writes them on the respawned vessel; a client keeps an answer it couldn't send and sends it once the vessel is back with the same question on it | `14_Run.cs`, `20_Vote.cs` |
| MODE-D5 | From map 2 every foe wore the Hardened elite tint | the map's health upgrade has no script; the host adds its Health Multi to each foe that carries it | `21_Difficulty.cs`, `00_RogueWavesModeMod.cs` |
| MODE-D6 | A late vote to leave was cut off by the next Tide | asking to leave holds the break for the vote's 20 s; no majority lets it go | `20_Vote.cs` |
| MODE-D7 | Closing a question also closed a SHOP or FUND dialog opened after answering | the question closes only its own dialog | `20_Vote.cs` |
| MODE-D8 | "Keep the Basin Clean" watched only the first Basin | every Basin's damage is noted at the start and each one is checked | `19_Objectives.cs` |
| MODE-D10 | A VENDCINT dialog that threw left SHOP, FUND and BOARD dead for the launch | each dialog runs through `RWUtil.Guarded`, and the buttons are freed when it ends, however it ends | `18_Shop.cs`, `01_Util.cs` |
| MODE-A1 | A quit between the staging map and map 1 brought the staging map back later | the campaign is saved as the staging map is left; Continue past map 1 never reads the flag on | `20_Vote.cs`, `06_Campaign.cs` |
| MODE-A2 | One throw in three boot steps turned the whole mode off, silently | each is guarded and logs a warning; the content's boot is stepped by hand (pitfall #133) | `00_RogueWavesModeMod.cs` |
| MODE-C1 | A Cardinal bought mid-map reached only later spawns | a catch-up pass gives its effect to what already stands, filtered as the stock kind would (Riptide's proven `CatchUp`) | `17_Cardinals.cs` |
| MODE-C4 | Cheap Refits rewrote the upgrade price every 0.5 s, so Voice in a Bottle's discount was lost | the cut is an amount taken off once per command and kept, so Voice's steps come off on top and its reset adds back only its own | `16_Directions.cs` |
| MODE-C6 | Cheap Refits skipped the Bouthouse; Cut Costs missed Fie's wall-built segments | the Bouthouse gets Refits; a Fie wall's own `Buildoption` commands get Cut Costs | `16_Directions.cs` |
| MODE-C7 | Two dictionaries grew every map | cleared at MapStart (`RWDirections.Reset`, `RWCardinals.Reset`) | `16_Directions.cs`, `17_Cardinals.cs` |
| MODE-C8 | The Directions' spawn hook could throw inside the game's spawn | its body is in a try | `16_Directions.cs` |
| MODE-B1 | Any drop or join put Quickening Current back to x1 on every machine | each machine sets the speed again in the two frames after a lobby change, and once when its own speed comes on (pitfall #153) | `07_Weather.cs` |
| MODE-B2 | A rejoin's repair restarted client halves that were alive | it restarts only halves still without a Master two frames after the player has loaded; a lookless half is restarted only after its group was in on two looks | `09_Rejoin.cs` |
| MODE-B3 | A turned-away player's catch-up outlived its map | every wait ends with its map | `09_Rejoin.cs` |
| MODE-B4 | A turned-away player was told to Join Game again | the "Lost the map" line is armed only once this player was in the map's play (`Activeplayer`) | `09_Rejoin.cs` |
| SHARED-2 | A stray Tide 0 at every map's start | `ModKitTides` reads `Max(Wave, 0)` on every poll (kit 1.9, pitfall #154) | `Shared/ModKitTides.cs` |
| ULO-1 | PLUG IN stuck on UNPLUG once the Power Bank was sold or destroyed | the cord tests the reference itself (pitfall #155) and keeps the plug's last position for the unplug effects | `Uloe/06_HeroBrain.cs` |
| SPL-1 | A Thread Head with a full Stash turned round at it every quarter second | it heads home only when the Stash has room | `Spool/09_Head.cs` |
| SPL-2 | RETURN with nothing carried parked the Head at the Stash | it turns round on arrival whatever it carries | `Spool/09_Head.cs` |
| YOS-1 | A Grafted Sapling's fruit always looked like the ALL fruit | the skin is written again after the spawn, which copied the parent's (pitfall #151) | `Yossele/01_Util.cs` |
| YOS-3 | A burst of deaths pushed an owner past 30 Cordyfriends | the cap is checked again at the spawn | `Yossele/16_Friend.cs` |
| ARG-3 | Another source's short slow look cut Argo's long one | `ModKitUtil.Debuff` sends a look a caller still renews once more in its last half second (kit 1.9) | `Shared/ModKitUtil.cs` |
| ARG-4 | AIM's reach and ring went stale while the Drill Rig stayed selected, and after a Calibrate tune | the picker reads the drill's newest synced AIM entry; the drill syncs again when its Range changes | `Argo/16_AimSelect.cs`, `Argo/07_TowerBrain.cs` |
| ARG-5 | A first miss ended the sound mixer's search for the session | it waits for the game's audio (kit 1.9) | `Shared/ModKitAssets.cs` |
| ARG-6 | The recoil dictionary kept every turret | destroyed turrets are dropped past 64 entries | `Argo/10_Client.cs` |
| FOES-2 | Illuminate on a Bouthouse could pass to Boutman and stay | the foes' auras skip the Bouthouse (pitfall #85) | `RogueWavesFoes` |
| FOES-3 | A foe with Aging and Berserk lost Berserk's damage | each modifier has its own handles | `RogueWavesFoes` |

## 3. Choices made

- **MODE-D5, the map's health:** the upgrade stays on every foe of the map (the self-tests read it there), with no
  script, so nothing paints the foe. The host adds `Health` Multi `0.08 × (map − 1)` itself, a quarter second after the
  foe comes at the latest; a Health Multi added later keeps the foe's share of health, so it still comes whole
  (kit `17` §2.1, verified). The stock script did the same sum. Pitfall #152.
- **MODE-C4, the price cuts:** applied as an amount off the price, not a factor, because Voice in a Bottle's steps and
  its reset are amounts too: price = 1 − Voice's steps − the Refits cut. The cut remembers the price it wrote. A new
  command, or a price someone else moved (a build price, or an upgrade price back at 1 or more), counts as a fresh
  price the game wrote, and the cut goes on again; a price only the cut wrote is left alone. Cut Costs on the vessel's
  own build commands goes the same way; its price is unchanged from before (1 − 0.1 per rank). Boutman is the
  exception: Boutbro rewrites his build prices as his fish count grows (kit `17` §5.1), so his cut is the same amount
  off a larger price, a smaller share of it (before, the cut wrote the plain price over his fish price).
- **MODE-D1 in the mode, not the kit:** the rewind is the mode's own (`RWDifficulty.Bonus`), and the kit's `Break` is
  right to fire on a rewound Entry for anyone who rewinds on purpose. The guard lives where the rewind does.
- **MODE-B1 on every machine, after lobby changes only:** setting the speed every frame would fight photo mode's own
  slider, which the mod can't read. The stock reset is tied to `Blm_Server.Lobbyupdate`, so the fix is too. On the
  pack's maps only.
- **Spool:** only SPL-1 and SPL-2, plain correctness bugs in the Head's walk; nothing about how Spool plays ("leave Spool
  as is for now").

## 4. Left, and why

| Ids | Why not here |
| --- | --- |
| MODE-C2, MODE-C3, MODE-C5, MODE-D9, ULO-3, YOS-2 | Each changes how something plays, so each waits for Dimitri's word (the review's §5 B) |
| MODE-A3, MODE-C9, MODE-C10, FOES-1, ARG-1 | Plausible; a fact from the PC decides each (the review's §5 C, with their PC checks) |
| MODE-C12 | Refuted by the review itself: `Totalwaves` stays 30 on every map |
| MODE-B5, MODE-B6, MODE-C11, YOS-4, ARG-2, SPL-6 | Plausible, and the fix isn't plainly safe without seeing the bug first |
| SPL-3, SPL-4, ULO-2 | Plausible; SPL-4 and ULO-2 are best built once in the kit as one brain-fault guard for all four vessels, which is a design change (and Spool stays as is) |
| SPL-5 | Online and risky: Thread a late joiner can't see needs a two-player round to prove |

## 5. PC test list

**First:** bring the PC's `TDS_Mod` up to this PR (it is older than PR #80). Every part should boot with "kit 1.9" in
its first line. Then each part's self-test as usual: this PR adds and removes no check, so the expected counts are
PR #80's list (`SIMPLIFICATION_BUILD_2026-11-04.md` §4), except the mode's, which PR #87's save fuzz brings to 155. Each row: what to do, and what to read
once fixed (the review's §6 has what the old build shows).

**Solo, the mode:**

| Id | Do | Expect |
| --- | --- | --- |
| MODE-D1 | Stay past Tide 29 (force the Tides on, pitfall #60) | one "Tide N paid ... Pure Aura" and one "VENDCINT landed" per bonus break |
| MODE-D2 | Take Mind the Vessels in the break where the party votes to leave; start the next map | no objective line before that map's first "posted:" |
| MODE-A1 | Play a new campaign's staging map through its vote; on the boat read `RW_Campaign_Staging` | "" |
| MODE-D3 | As Boutman, take Mind the Vessels and let a Boutbro die | "Hands done" |
| MODE-D5 | On map 2, spawn a plain Tide-1 foe; read its `Skin.Value` and its max Health against map 1's | "" and the plain look; Health × 1.08 |
| MODE-D6 | Let a bonus Tide time out with a foe left, kill it 45 s into the break, vote Leave | "1 of 1 to leave" and the party leaves |
| MODE-D8 | On Splithead, take Keep the Basin Clean and let a foe hit only the second Basin | failed |
| MODE-C1 | Mid-map, buy Sport Water and Rusty Pipe; read the vessel's and an older tower's `Upgradelistserver` | their effect entries present; the log says "reached N already standing" |
| MODE-C4 | Hold Voice in a Bottle and Cheap Refits; after two Tides read a tower's `Upgrade` Goalvalue; upgrade it and read again | below the Refits price by Voice's steps; after the upgrade, the Refits price |
| MODE-C6 | As Boutman with Cheap Refits, read the Bouthouse's `Upgrade` Goalvalue; as Fie with Cut Costs, price a segment built from a wall | the Refits price; the segment costs less |
| MODE-A2 | In a dev build, make `RWIcons.Add` throw on purpose | a warning, then "ready" and the menu button |
| MODE-D10 | In a dev build, make `Shelf` throw once; press SHOP again | a warning, then the shop opens |
| SHARED-2 | A solo map with Yossele: the first "Tide" line after "GAME START" | "Tide 1" |

**Two players** (`Run-TwoPlayers.ps1`):

| Id | Do | Expect |
| --- | --- | --- |
| MODE-D4 | Let the friend's vessel drown in the last seconds of Tide 15 | the dialog opens on the respawned vessel; the host logs "2 of 2 to leave" once both answer |
| MODE-D7 | Answer a pick, open SHOP, have the other player answer | the shop stays open |
| MODE-B1 | On a Quickening map (QuickTide lowered), drop the friend and rejoin; log `Time.timeScale` and `Time_Scale` on both | 1.25 and 1.25 on host, friend and rejoiner |
| MODE-B2 | Rejoin mid-Tide (a cable pull, then a restart); count each visual script per client half and duplicates in `Entityclient` | no doubles; the "started again the client halves" line counts only stopped ones |
| MODE-B3 | Join mid-map, be turned away, join on the boat, start the next map within 4 minutes | no "rejoin: map loaded here" line before "Loading 80%"; the fly-in plays |
| MODE-B4 | Repeat the test round's O5 (a mid-map join turned away); read that client's main-menu error box | the stock text or "join on the boat", not "Lost the map" |

**The vessels and the foes:**

| Id | Do | Expect |
| --- | --- | --- |
| ULO-1 | Plug into your own Power Bank, then sell it (and once more with foes destroying it) | PLUG IN within 1 s, `Skin.Value` "", the unplug effect at the Bank's place |
| SPL-1 | A Head with Delivery, the Stash at 50 with no Tin able to spend, 20 Thread at the Head's spot | after 10 s it stands at its spot; it heads home once the Stash has room |
| SPL-2 | A Head carrying nothing, far from drops: press RETURN | it walks back to its spot as it reaches the Stash |
| YOS-1 | A Grafted Sapling set to BLUE mines a node out at full growth | the fruit's `Skin.Value` "Blueaura", the blue model |
| YOS-3 | Self-test: hold an owner at 29 friends, call `Raise` 5 times in one frame, wait 0.2 s | `CountOf` 30 |
| ARG-3 | Pin a foe in Deep Water next to a stock slowing tower | if the tower's short slow cuts the look, it is back for the hold's last half second at the latest |
| ARG-4 | Tune a Drill Rig with Calibrate, open AIM between the old and the new reach | "Shake the ground here", and the server takes it |
| FOES-2 | The Bouthouse 2 tiles from a Dazzling Dagger, Boutman away; read Boutman's upgrades | no Illuminate |
| FOES-3 | A Dagger with Aging and Berserk at 50% health; read its Damage against a plain one's | base × (1 + Aging's share + 0.5) |

Still open from earlier lists: PR #80's T0-L1 (`SIMPLIFICATION_BUILD_2026-11-04.md` §4) and the second test round's
leftovers (Spool host with an Argo friend showing no debuff looks, Argo's Deep Current check, the Cardinal-return check).

## 6. How it was checked

From `TDS_Mod/ModSource`, after `setup.sh` once: `python3 ../Tools/cloud-check/check_mod.py <Part>/Code Shared <Part> ""`,
again with `<PART>_SELFTEST,<PART>_SHOTS`, and `../Tools/cloud-check/check_editor.sh ../Assets/<Part>Mod/Editor`, for Argo,
Yossele, Spool, Uloe, RogueWavesMode and RogueWavesFoes: 0 errors in all 18. The one warning, in the mode's self-test
build (`96_Flow.cs:311`, an unused local), is the base's. Nothing was run in game.

`/simplify` on the diff (reuse, simplification, efficiency, altitude), then fixed what it found inside the diff: one
writer for the vessel's question and run line, the held pick as one loop, fewer copied lists, and the rejoin's map check
as one wrapper, which it caught still running on a destroyed map. Skipped, as larger than the fix or outside the diff: a
shared plan-prefix helper, `Guarded` in the kit, a generic prune helper, a tag index in place of the entity scan,
`ModKitTides.SetEntry` for the bonus break, and three micro-optimisations.

`/code-review high` on the diff after the base's #86 and #87 were merged in: nine findings. Fixed:

- SHARED-2 still fired its Tide 0: the rewind rule set the -1 back a line later. `Max(Wave, 0)` now holds on every poll.
- The slow look's resend went every half second for a look a caller kept renewing, a slot in the game's send queue each
  time; now once, in the look's last half second (kit 1.9, pitfall #95, kit `19` §4).
- A pick held while the vessel was down could go to a round the host had closed meanwhile; it now goes only once the
  respawned vessel carries the same question (MODE-D4).
- The price cut took any price of 1 or more for a fresh one, so a price above 1 that it wrote itself (Boutbro's) was cut
  again every half second; it now remembers what it wrote.
- The Henge's vines, spawned with no skin asked, lost the Henge's skin after YOS-1; an empty skin keeps the parent's again.
- The price cuts copied the entity list once per player each half second; now once a frame for all.

Left: the "Lost the map" line needs `Activeplayer` above 0, which a player in the map has from the game's start
(`Game_Admin.Play`) or the rejoin's `Restore`, so the reviewer's seat with `Active` 0 is not a player in the map's play;
two writers of `Time.timeScale` (both read `RWWeather.GameSpeed`, so they agree); `Guarded` ends only the coroutine that
threw, on purpose (MODE-A2: the part must still boot, and every dialog step after it checks its answer).
