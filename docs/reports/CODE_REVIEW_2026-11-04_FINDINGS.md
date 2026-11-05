# Code review against the engine: every finding (2026-11-04)

The detail behind `CODE_REVIEW_2026-11-04.md`: every finding of the first pass, by part, as each reviewer wrote it
(cloud only, read from the mod's code and the decompiled game; nothing here was seen in game). The summary, the
ten to fix first, the base's compile repair and the short PC list are in that report.

**How to read an entry.** Each has Where, Severity, Confidence, Tags, What happens, Why (the game's code as
`Class.cs:line` in the decompiled `Assembly-CSharp.dll`, kit `docs/13` §11), Fix (low-risk or risky) and the PC check
that would prove it. **Confirmed** means read from the mod's code and the game's; **plausible** names what decides
it. A **lead note** marks the entries the review's lead read again at `63af4b0` and what it found. The issue
definition: a defect that changes what a player sees or can crash; style, naming and "could be simpler" are out.

**Line numbers** are as each reviewer read them: sections 2 and 8 at `63af4b0` (PR #80 and #82 merged), the others at
`5d9b09e` (before them). PR #80 renamed helper calls (no line moves), but in each part's `01_Util.cs` and
`03_Assets.cs` and in the vessels' building brains it removed whole blocks, so a line there can be off. Search by the
function the entry names.

**Tags.** `in-PR80` / `in-simplification-code`: in code PR #80 moved or replaced, with whether the bug survived.
`known`: already listed elsewhere (the lead note says where). `test-only`: a self-test gap.

| Section | Part | Ids |
| --- | --- | --- |
| 1 | RogueWavesMode: shop, objectives, votes, difficulty | MODE-D1 to D10 |
| 2 | RogueWavesMode: boot, mode, world, boat, campaign | MODE-A1 to A3 |
| 3 | RogueWavesMode: run state, Directions, Cardinals | MODE-C1 to C12 |
| 4 | RogueWavesMode: weather, rejoin, online, dialogs, menu | MODE-B1 to B6 |
| 5 | The shared kit and the foes part | SHARED-1, SHARED-2, FOES-1 to 3 |
| 6 | Yossele | YOS-1 to 4 |
| 7 | Spool | SPL-1 to 6 |
| 8 | Argo | ARG-1 to 6 |
| 9 | Uloe | ULO-1 to 3 |

## 1. RogueWavesMode: shop, objectives, votes, difficulty (`18_Shop`, `19_Objectives`, `20_Vote`, `21_Difficulty`)

Read line by line at `5d9b09e`, with what they call in `14_Run`, `16_Directions`, `17_Cardinals`, `11_Dialog`, `06_Campaign`, `02_Mode`, `01_Util`, `00_RogueWavesModeMod` and `Shared/ModKitTides`. Stopped before the debug-only files.

### MODE-D1. From Tide 29's break on, every bonus break fires twice: double Pure Aura, double Compound Interest, VENDCINT lands twice
- Where: `TDS_Mod/ModSource/RogueWavesMode/Code/21_Difficulty.cs:163-190` (the rewrite at :168), `20_Vote.cs:57-80`
  (pay :65-66, shop :73-74), `Shared/ModKitTides.cs:57-63`, `16_Directions.cs:375-389`, `14_Run.cs:212-219`
- Severity: wrong-behaviour
- Confidence: confirmed (code + engine read)
- Tags: none
- What happens: Tide 29 is cleared, so Entry is 59 (row 58, the break after Tide 29). `ModKitTides` fires Break(29) and
  `RWVote.Break` pays the Tide and the interest, lands VENDCINT and posts the objective. `RWDifficulty.Bonus` then
  rewinds Entry to 57. On the next frame `ModKitTides` sees Entry change to 57, row 56, another break row, and fires
  Break(29) **again**. Each player's Pure Aura is paid twice (2+2, or 3+3 on a weather map). Compound Interest pays
  real base aura twice. VENDCINT, which landed a frame earlier, is despawned and dropped again ("VENDCINT HAS LANDED"
  shows twice). The objective is posted again. The second `Bonus` call returns at its Entry check, so the core top-up
  is not doubled. The same happens at every later bonus break (30, 31, ...), because each one ends at Entry 59.
- Why (engine evidence): `Mode_Normal.Run` reads row `Entry - 1` on each pass and adds 1 when a row's clock runs out
  (Mode_Normal.cs:357-363, 481-483; pitfall #121). The table has 60 rows, Tide N at row 2N-1 (kit 15 §11, #121).
  `ModKitTides.Update` fires Break on **any** change of Entry onto a break row (`EntryIsBreak`, :57-63; :55-56 handles
  only a falling Wave). The mod's own write at 21_Difficulty.cs:168 is such a change.
- Fix: in `RWVote.Break`, handle each wave once per map. Keep `lastBreak` (reset to -1 in `RWVote.MapStart`) and
  return when `wave > 0 && wave == lastBreak`. Low-risk: local, and no legitimate second break exists with the same
  Wave (a real break always follows a Tide, which raises Wave).
- PC check: on a campaign map, force Tides on to Tide 31 (Time_Wave ≤ 0, pitfall #60). In the log, count the "Tide N
  paid ... Pure Aura each" and "VENDCINT landed at" lines per break. Today Tides 29, 30 and 31 each log 2 of both and
  `RWVote.Breaks` rises by 2 per break. After the fix it should be 1 each.
- Lead note: re-read at `63af4b0` and confirmed: `ModKitTides.cs:57-63` fires `Break` on any change of Entry onto a break row, `RWDifficulty.Bonus` (`21_Difficulty.cs:162-168`) writes `Entry - 2` from inside the `Break` listener, and `RWVote.Break` (`20_Vote.cs:57-80`) has no repeat guard. The shared-kit reviewer found the same bug from the kit side (SHARED-1, section 5).

### MODE-D2. An objective running when a map ends is judged at the next map's opening break, and can pay there (also on a replay)
- Where: `19_Objectives.cs:24-31` (statics), `:119-145` (`Begin` is the only reset), `:166-194` (`Resolve`);
  `20_Vote.cs:31-53` (`MapStart` doesn't clear them), `:57-62` (`Break` calls `Resolve` first), `:93-99` (`MapEnd`);
  `14_Run.cs:187-203, 221-227`
- Severity: wrong-behaviour
- Confidence: confirmed (code + engine read). Which objective pays depends on the stale state, as listed below.
- Tags: none
- What happens: `RWObjectives.Active`, `failed`, `targetFell`, `clockLeft` and `tideLength` are statics that nothing
  clears at a map's end or start. Two cases leave `Active` set when a map ends: (a) the Basin falls in a Tide whose
  objective was taken; (b) a player takes the posted objective in the break where the party votes to leave, or the host
  quits. On the next map, the replay or the next one, `ModKitTides` fires Break(0) as `Mode_Normal.Run` sets Entry to 1,
  and `RWVote.Break` calls `RWObjectives.Resolve()` before anything else. The old objective is judged against stale
  state:
  - "Mind the Vessels" pays 2 Pure Aura to everyone if no vessel drowned in the last Tide played.
  - "Beat the Clock" pays 3 if the clock's last reading in the old map's Tide row was above 25%.
  - Meteor and Nest pay if their target fell.
  - "Keep the Basin Clean" always fails at that moment, since the Basin doesn't exist yet (#127), and puts
    "Keep the Basin Clean failed." in chat as the new map starts.

  The payment lands after `RWRun.MapStart`'s checkpoint. So a team replaying a lost map gets Pure Aura for an
  objective of the lost one, which they keep if they win the replay. An objective taken just before leaving pays on
  the next map although it never ran.
- Why (engine evidence): MapStart fires as soon as `Maploaded` is set in `Game_Grid.Generatemap` (Game_Grid.cs:715).
  That is before `Startgame` (Game_Init.cs:122-140), and `Run` then writes Entry 1 (Mode_Normal.cs:310), which
  `ModKitTides` reports as Break(0). `Despawnall` passes `Death: false` (Game_Entity.cs:46-80), so the teardown itself
  doesn't change `failed` or `targetFell`. The stale values are the ones the old map left.
- Fix: clear the objective at each map's start: `RWObjectives.Reset()` (Active = Posted = null, target = null,
  failed = targetFell = false, clockLeft = tideLength = 0), called first in `RWVote.MapStart`, before the
  `InCampaignMap` return. Low-risk.
- PC check: take Mind the Vessels in the break where the party votes to leave, then start the next map. At its opening
  break the log shows "paid 2 Pure Aura each: Mind the Vessels" today. After the fix, no objective line before the
  first "posted:".
- Lead note: re-read at `63af4b0`: `RWVote.MapStart` (`20_Vote.cs:31-53`) resets nothing in `RWObjectives`, and `Break` calls `RWObjectives.Resolve()` first (`:61`). Confirmed on the mod's side.

### MODE-D3. "Mind the Vessels" fails whenever a Boutbro falls
- Where: `19_Objectives.cs:197-205` (:203)
- Severity: wrong-behaviour
- Confidence: confirmed (code + kit data read)
- Tags: none
- What happens: with Boutman in the party, the objective "No vessel drowns this Tide" fails as soon as any Boutbro is
  killed. Boutbros are fighting helpers that die often, so the objective is close to unwinnable for a team with
  Boutman. A lobby Blurry Photo's clones do the same.
- Why (engine evidence): the check is "killed, tagged `Hero`, owner ≥ 0". Boutbro is `Mob_Boutmanbro (Mob, Hero,
  Boutfamily, Boutbro)`, and units take their building's owner (kit 17 §2.5, §5; Entity_Ai_Hero_Buildsell.cs:179-202).
  `Ondespawn` gets `Death` as `killed` (Game_Entity.cs:323-338). Pitfall #145 is the same trap.
- Fix: count only the player's own vessel. Compare the despawned being's `Objectname` with
  `Getload("Hero", Tds_Clientinfo.Hero)`, as `RWRun.Vessel` does, or require a parent tagged `Core` (kit 17 §2.5).
  Low-risk.
- PC check: play Boutman, take Mind the Vessels, and let a Boutbro die while Boutman lives through the Tide. The log
  shows "Hands failed" today. Expect "Hands done" and "paid 2 Pure Aura each".
- Lead note: re-read at `63af4b0` (`19_Objectives.cs:197-205`: killed, tagged `Hero`, owner >= 0). The 2026-11-04 test round met the same trap in `RWRun.Vessel` (pitfall #145, fixed there); this is the second place.

### MODE-D4. A question asked while a player's vessel is down never reaches them (picks, map vote, vote to leave)
- Where: `14_Run.cs:278-289` (`Ask` writes on the current vessel entity), `:244-262` (`Carry` makes a fresh `RW_Ask` and
  `RW_Run` on a respawned vessel and never writes them again), `20_Vote.cs:215-227, 282-283, 351-352` (each question is
  written once), `14_Run.cs:383-389` (`RWMine.Send` needs a vessel)
- Severity: online-only (it happens solo too, with any death)
- Confidence: confirmed (code + engine read)
- Tags: none
- What happens: the vote to leave opens once the field is clear in a break, which is right after the Tide in which
  vessels drown. A player whose vessel is down at that moment has no `RW_Ask` to read. Their respawned vessel gets a
  blank one, so their dialog never opens. The vote waits its full 20 s, and their vote counts as none. With two
  players, one drowned player means the other's "Leave" (1 of 2) can never pass, and the party can't leave that break.
  Direction picks asked after a vote to leave time out to the player's first card. The run line, and with it the
  board's "posted" text, is lost the same way until the next `Line()`. So the BOARD button says "NO OBJECTIVE THIS
  BREAK". A pick made in a dialog that was open when the vessel died is dropped silently: `Send` finds no vessel.
- Why (engine evidence): `Game_Entity.Despawn` destroys the entity's NetworkObject in the same call
  (Game_Entity.cs:323-405), and a respawn spawns a **new** vessel entity (kit 15 §9, Entity_Ai_Main_Core.cs:126-153), so
  the old entity's commands are gone. `RWRun.Vessel` returns null while the vessel is down. `Ask` then returns without
  writing anything, and nothing writes it later.
- Fix: keep the open question per session on the host (what, round) and the current run line. In `Carry`, write them
  onto a vessel whose `RW_Ask`/`RW_Run` differs. Low-to-medium risk: it is local, but it is the online path.
- PC check: with two players, have one vessel drown in the last seconds of Tide 15 (or kill it with the self-test right
  as the field clears). After its respawn, its owner's `RWAsk.Open` stays "" and the host logs "the vote to leave:
  1 of 2 to leave". Expect the dialog to open on the respawned vessel.

### MODE-D5. From map 2 on, every foe wears the Hardened ("Health") elite look, and real elites can lose theirs
- Where: `21_Difficulty.cs:56, 80-82, 105-116`
- Severity: wrong-behaviour
- Confidence: confirmed that every foe without another look-changing upgrade shows `Health`. Plausible that a real
  elite loses its own look: that depends on the order its upgrades' `Starting` runs, and the health upgrade is listed
  after the row's own upgrades (:80-81).
- Tags: none
- What happens: the map's health bonus is an upgrade on the stock `Entity_Ai_Enemy_Health_Upgrade`, which is the
  Hardened elite's script. Its `Starting` also sets the foe's `Skin` to `Health`. So from map 2 every foe is tinted as a
  Hardened elite. A Tide row's own Armor, Tempered or Superheated foe can come out looking Hardened, and players can no
  longer read which foes are elites. That is the one thing the elite tint is for.
- Why (engine evidence): Entity_Ai_Enemy_Health_Upgrade.cs:7-15 writes `Master.Skin.Value = "Health"`. `Skin` is one
  synced value and the last writer wins (Armor writes "Armor", Entity_Ai_Enemy_Armor_Upgrade.cs:10). Every client draws
  `Vis.Skinoverrides[Skin]` (Entity_Master_Client.cs:204-205, 521-530). Stock foes carry a `Health` skin override
  (kit 13 §4 and its elite table, Hardened = `Health`).
- Fix: give map health without the elite script. On the host, hook each campaign foe's `master.Loaded` and `Addmod`
  `Health` Multi `HealthPerMap × (map - 1)`, checking `Statsserver.ContainsKey` (pitfall #82). Risky: it touches every
  foe's spawn on the host. Until then, a stopgap is to leave the health bonus out (`Health()` returns null).
- PC check: on map 2 of a campaign (or after `RWDifficulty.Apply(2, 1)`), spawn a plain Tide-1 foe. On the host,
  `foe.Skin.Value` reads "Health" and the foe shows the Hardened tint. Expect "" and its plain look.
- Lead note: the engine half re-read: `Entity_Ai_Enemy_Health_Upgrade.Starting` adds the Health mod and then writes `Master.Skin.Value = "Health"`.

### MODE-D6. The vote to leave doesn't hold the break: a late vote is cut off by the next Tide and never asked again for that Tide
- Where: `20_Vote.cs:119-126` (it opens when the field clears), `:342-354` (`AskLeave`: no `Hold`), `:82-88` (`Tide` →
  `CloseQuestion`), `:344-346` (`leaveWave` blocks a re-ask), `:356-371`
- Severity: wrong-behaviour
- Confidence: confirmed (code + engine read)
- Tags: none
- What happens: a Tide that runs out its clock with foes still on the field starts its break with them alive. The vote
  to leave waits until they are dead. If that takes more than 40 of the break's 60 s, the 20 s vote is still open when
  the next Tide starts. `RWVote.Tide` closes it uncounted, and a Leave majority is thrown away. The bonus Tide starts
  although the party voted to go, and the same wave can't ask again. That contradicts the scope's "leave between
  tides, when a majority votes to" (scope 04 [Tides and Leaving]).
- Why (engine evidence): a break's clock runs down by itself (Mode_Normal.cs:409-412). Only the mod's `HoldBreak`
  keeps a break open, and `AskPicks`/`AskMap` take it while `AskLeave` doesn't.
- Fix: `Hold(LeaveSeconds)` in `AskLeave`, and `HoldBreak = false` in `CloseLeave` when the party stays. Low-risk.
- PC check: let a bonus Tide time out with a foe left, kill it 45 s into the break, and vote Leave. Today the log has no
  "the vote to leave:" line and Wave rises. Expect "1 of 1 to leave", then the picks.

### MODE-D7. Closing a question also closes the shop or Fund Transfer dialog the player opened after answering
- Where: `20_Vote.cs:461-470` (`RWAsk.Update`: Goalstring "" plus `Open != ""` → `RWDialog.Cancel()`), `:546-549` (`Open`
  stays set after answering); `11_Dialog.cs:142-148`; `18_Shop.cs:181-187`
- Severity: wrong-behaviour (minor)
- Confidence: confirmed (code read)
- Tags: none
- What happens: a player answers the opening Direction pick or the vote to leave, then opens VENDCINT's SHOP or FUND
  while others are still choosing. When the last player answers or the vote times out, the host clears `RW_Ask`, and
  every machine whose `Open` is set calls `RWDialog.Cancel()`. That closes whatever dialog is up: the player's half-made
  purchase or transfer vanishes, and they have to start again.
- Why (engine evidence): there is one confirmation menu (`Bla_Ui_Confirmation.Instance`), and `Cancel` closes it
  whatever it shows (11_Dialog.cs:142-148).
- Fix: in `RWAsk`, set `Open = ""` once the answer is sent, and call `Cancel()` only while its own question is still
  `running`. Low-risk.
- PC check: answer a pick, open SHOP, and have the other player answer. The shop dialog closes today. Expect it to stay.

### MODE-D8. "Keep the Basin Clean" watches only the first Basin
- Where: `19_Objectives.cs:127-128, 173-177, 207`; `01_Util.cs:95-103`
- Severity: wrong-behaviour (minor)
- Confidence: confirmed (code read)
- Tags: none
- What happens: on two-Basin maps (Splithead, Splithead δ, kit 15 §15), damage to the second Basin doesn't fail the
  objective.
- Why (engine evidence): `RWUtil.PlayerCore()` returns the first living core in `Entityclient`.
- Fix: sum `Damagetaken` over `RWUtil.PlayerCores()` at `Begin` and at `Resolve`. Low-risk.
- PC check: on Splithead, take it and let a foe hit only the second Basin. Today the objective is reported done.

### MODE-D9. With a lobby Mooncore, the bonus rewind ends the map as a "Win" at Tide 34 with no vote
- Where: `21_Difficulty.cs:166-168`; `17_Cardinals.cs:39-43` (`NotSold` filters only the shop)
- Severity: wrong-behaviour
- Confidence: plausible. What decides it: whether a Mooncore picked in the boat's lobby is applied on a campaign map.
  I found no filter in the mode, but `05_Boat.cs` wasn't read.
- Tags: none
- What happens: bonus Tides end at Entry 59. When the Tide played at Wave 34 clears, Entry goes from 58 to 59, and
  Mooncore's Entry hook adds 2 (35 is a multiple of 7), taking Entry to 61, past the 60 rows. `Mode_Normal.Run` leaves
  its loop and calls `Win()`. The map ends with no Direction picks and no map vote, so `Choice` stays -1 and the boat
  doesn't sail by itself.
- Why (engine evidence): Entity_Ai_Artifact_Mooncore.cs:14-31 (Entry +2 and Wave +1 when (Wave + 1) % 7 == 0);
  Mode_Normal.cs:357 (`Entry <= Totalentries`), :484-487 (`Win`).
- Fix: in `Bonus`, rewind to a row that leaves room for Mooncore's jump, or remove lobby Cardinals the mode can't
  host. Risky: it touches the rewind.
- PC check: pick Mooncore in the lobby, then stay to Tide 34. The map ends "Win" at Entry 61 with no vote.

### MODE-D10. A VENDCINT button can go dead for the rest of the launch if its dialog coroutine throws
- Where: `18_Shop.cs:181-187` (`asking` blocks every later `Open`), `:250-255, 266-306`
- Severity: wrong-behaviour
- Confidence: plausible. What decides it: whether `Blm_Server.Sessionidsplayer` can still hold a destroyed `Blm_Player`
  while a friend drops. `FriendPicture` calls `GetComponent` on it, which would throw a `MissingReferenceException`
  before `asking = ""`. `Blm_Server` wasn't read.
- Tags: none
- What happens: if anything in `Shelf`, `Fund` or `Board` throws before `asking` is reset, the coroutine dies (#133), and
  every later press of SHOP, FUND or BOARD returns at once, for the whole launch.
- Fix: reset `asking` in a `finally`-style wrapper. Run the dialog body through a try in steps, or put a time limit on
  `asking` (clear it after 60 s or when the scene changes). Low-risk.
- PC check: open FUND as a friend drops; then press SHOP. Nothing opens if it threw.

### Checked and fine (section 1)
- `Modefile`, `vars.Mode` and `RWMode.ModeObject` are one object (`Game_Rounds.Generatemode` uses `Getload` with no copy;
  Game_Init.cs:117), so `RWDifficulty`'s live edits reach `Mode_Normal.Run`.
- MapStart (`Maploaded`, Game_Grid.cs:715) comes before `Startgame`, so the table is restored and scaled before `Run`
  reads it. `Restore`/`Apply` keep the row count. `Run` copies each row's struct as the row starts, so a mid-row edit
  can't disturb it, and it walks the live table only in its first synchronous pass.
- `Totalwaves` stays 30: only `OnValidate` writes it (Wavesobject.cs:56-72), and the rewind never grows the table. The
  N4 divide-by-zero needs Totalwaves above 30, so Coin, Caffeine, Moneybag, Opal Core, Rusty Pipe and Voice are safe.
- The rewind maths (#121): Entry 59 → 57, and the clock's end replays row 57 (Tide 29). The core top-up happens once
  per bonus Tide.
- Beat the Clock (#142): `clockLeft` is read only in the Tide's own row. `Run` writes Entry and the next row's
  `Time_Wave` in one synchronous step (Mode_Normal.cs:481 → 357-363), so no mixed reading is possible.
- The opening break before the Basin (#127): the shop and objective of Break(0) wait for every vessel to stand and the
  fly-in to end, or 90 s.
- The host-only `RW_Foe_Health<N>` on clients: `Getload` misses are null-checked (Entity_Master_Client.cs:232-243,
  Game_Ui.cs:117, 1127-1131), and its empty `Display_Name` hides it. It never throws.
- Map teardown: `Despawnall` passes `Death: false`, so no false "target fell" or "vessel drowned".
- Fund Transfer: an amount ≤ 0, a transfer to oneself and an unknown colour are refused. The host clamps the amount to
  the limit. The aura multipliers are set to 1 and restored in a `finally`. `Checkresources` runs before
  `Useresources`. The limit is 50 × (Wave + 1) per break, as scope 06 says.
- Buying: the host checks the price before charging. A stock Cardinal already held is refused before any charge, so
  two players buying the same one costs the second nothing. A Direction rank must be the next one (`Top`). The host
  rolls the shelf again from its own state, and its map number (Route + 1 - WonThisMap) equals the client's (the
  world's route + 1).
- Vote counting: votes are keyed by the vessel's owner (the owner check of `Command_ServerRpc`), and only present
  sessions count. Ties go to the first choice on a map vote and to staying on a leave vote. `until` is set at every
  ask, so every vote closes. The host's own vote counts like anyone's.
- The map ending during a vote: `MapEnd` clears `State` and `HoldBreak`, and `Leave()` saves the run before it ends
  the map.
- VENDCINT at a map's end: `Leave()` guards a destroyed `Stand` with an explicit `!= null`.

### Not yet reviewed (section 1)
- Debug-only files, false-pass hunt: `91_Feel.cs`, `94_DialogLook.cs`, `95_Look.cs`, `96_Flow.cs`, `97_Shots.cs`. In
  particular, whether `96_Flow`'s bonus-Tide checks would notice MODE-D1's double pay.
- The client's map-vote row, `RWCampaign.Live ? Route.Count + 1 : route.Count + 2` (`20_Vote.cs:516`), in the staging
  map. It needs `04_World.cs` (`Parse` and the staging world), which wasn't read.
- Fund Transfer to a friend who dropped: `Prevsessionlist` accepts them, and `RWRejoin`'s seat restore of their aura may
  overwrite it (`09_Rejoin.cs` not read).
- `RWShop.Tile` on an occupied tile (what `Helper_Gridtools.Checkifstandable` returns was not read). The VENDCINT jump
  on a client whose `e.Client` was null when the shop was first seen (its Vis handler is never attached).
- `RWDifficulty.Apply` on Wait and Gate content rows: an extra copy of a Wait row would add a wait if such a row has an
  Enemy set. It needs Mode_Normal's table data (`reference/roguelab_round1.txt`).
- Whether the RogueWavesFoes modifiers also write `Skin` (the overwrite order in MODE-D5).
- Docs not read: `19_NETWORKING.md` §6, `20_UI_AND_HUD.md`, `16_MAPS_AND_ECONOMY.md` §5, most of `13`,
  `docs/reports/MODE_POLISH_2026-11-03.md` and `TEST_ROUND_2026-11-04.md`. A finding above may already be known there.
  None is in the part's `IMPLEMENTATION_NOTES.md`.

## 2. RogueWavesMode: boot, mode, world, boat, campaign (`00`-`06`)

Read line by line; every cited line re-checked at `63af4b0` (PR #80 and #82 merged). Stopped before `99_SelfTest.cs`.

### MODE-A1. When the staging map ends, nothing saves it: a quit before map 1 leaves `Staging` stuck on for the whole campaign, and the boat then reopens the staging map by itself
- Where:
  - `TDS_Mod/ModSource/RogueWavesMode/Code/20_Vote.cs:384-396`: `RWVote.Leave`, the staging branch. The flag is cleared
    in memory only, at :392.
  - `06_Campaign.cs:95`: `Continue` reads `RW_Campaign_Staging`.
  - `06_Campaign.cs:147-166`: `Save`/`SaveChoice`, the key's only writer. Map 1's `MapStart` (:188) is the next call.
  - The consequences: `05_Boat.cs:86`, `05_Boat.cs:153-159`, `20_Vote.cs:47-49`.
- Severity: save
- Confidence: confirmed (code read).
  - Every write of the field: `New` (06_Campaign.cs:66), `Continue` (:95) and `Leave` (20_Vote.cs:392); the self-tests
    write it too.
  - No campaign save runs between the staging vote and map 1. The two `SaveChoice` calls (20_Vote.cs:270, :309) come
    before `Leave`. `OnBoat` (06_Campaign.cs:246-254) reaches `Lost` only when `Playing >= 0`, and `Playing` is -1
    through the staging map.
- Tags: none
- What happens:
  1. A new campaign plays its staging map. The map vote closes (`CloseMap`/`AskMap`). `RWCampaign.SaveChoice()` writes
     `RW_Campaign_Staging = "1"` and the chosen column.
  2. `RWVote.Leave` sets `Staging = false` and calls `Quitmap_ServerRpc`. Its only save is `RWRun.Save()` (:388), which
     writes the players' keys and not the campaign's.
  3. The party is back on the boat. Before map 1 reaches `MapStart`, the host quits to the main menu or the game closes:
     on the boat, during the sail, in the Setup panel, or during map 1's load. Stopping right after the Directions and
     the vote is a natural place to pause.
  4. On Continue, `Staging` is read back as true. Map 1 starts with `Next` set, so `RWVote.MapStart` doesn't take it for
     the staging map (`staging = Staging && Next == null`, 20_Vote.cs:47). Nothing clears the flag again, and every
     `RWCampaign.Save` from here on writes "1" back.
  5. On every later boat visit, the host's boat can select `RW_Harbour` by itself (05_Boat.cs:153-159). That happens
     12 s after the boat came up, if nothing is selected and no `Selectmap` was sent on this visit (`Picked == ""`;
     the auto-sail sets `Picked` only when it arrives, :276). Two common ways in:
     - the auto-sail to the vote's column, or to a replay, arrives more than 12 s after the boat came up;
     - the leader steers by hand, for example after a map lost in a bonus Tide, where no where-next vote set `Choice`.
  6. The Setup panel opens on the staging Gameplan. While it is open, `Sail` stops driving the boat (05_Boat.cs:251),
     and the leader has to close the panel.
  7. If the party readies up instead, the staging map plays again:
     - `Next` is still null, because the harbour is not in `Open()` (06_Campaign.cs:43-51) and so is never taken as
       `Next` (05_Boat.cs:160-165);
     - `RWCampaign.MapStart` ignores the map (:176), so no retry is at stake;
     - `RWVote.MapStart` treats it as the staging map: every player is owed a free Direction pick (20_Vote.cs:49), and
       the column vote runs again.
  8. The harbour also stays off the boat's blacklist on every chart visit (05_Boat.cs:86). The leader can pick it by
     hand too, if the boat can reach it.
- Why (engine evidence):
  - `Quitmap_ServerRpc` (Tds_Multiplayer_Remote.cs:59) sets `Gamecomplete` to "Return" (:99), so `ModKitTides` fires
    `MapEnd("Return")`. `RWCampaign.MapEnd` then returns at 06_Campaign.cs:225-226 (`Playing < 0` and not
    `WonThisMap`) without saving.
  - `Bla_Data.OnApplicationQuit` (Blockage.Tags.Bla_Data.cs:150) only writes out the "1" that is still in `Playerdata`.
  - The self-tests can't see it. 96_Flow.cs:344 checks the in-memory flag. 93_TwoPlayers.cs:363-371 clears the flag and
    then calls `SaveChoice` by hand, which is the fix's order.
- Fix: in `RWVote.Leave`'s staging branch, call `RWCampaign.SaveChoice()` right after `RWCampaign.Staging = false;`.
  `Save` is already guarded by `Live`. Low-risk: one more flush, and the timing doesn't change. An optional extra
  guard is to auto-open the harbour only while `RWCampaign.Route.Count == 0` (05_Boat.cs:153).
- PC check:
  1. Play a new campaign's staging map through its vote.
  2. Back on the boat, read `RWUtil.ReadString(RWData.SaveFolder, RWKeys.Staged)`. Today it reads "1"; once fixed, "".
  3. Quit to the main menu, Continue, and win map 1.
  4. On the next boat, don't click, and let more than 12 s pass before anything is picked (steer by hand, or lose in a
     bonus Tide first). Today the log says `staging: the harbour opens` and the Setup panel shows the staging map. Once
     fixed, nothing happens.
- Lead note: re-read at `63af4b0`: the staging branch of `RWVote.Leave` (`20_Vote.cs:384-396`) clears `RWCampaign.Staging` and quits the map with no campaign save.

### MODE-A2. An exception in one of Boot's unguarded steps turns the whole part off for the launch, silently
- Where: `TDS_Mod/ModSource/RogueWavesMode/Code/00_RogueWavesModeMod.cs:47-48` (`RWAssets.Find()` and `RWIcons.Add()`,
  with no try) and `:70` (`yield return RWContent.Boot();`). Their neighbours `RWMode.Register` (:65) and `RWTips.Boot`
  (:69) are wrapped.
- Severity: crash (the part dies silently)
- Confidence: plausible. What decides it is whether `RWAssets.Find` (12_Menu.cs), `RWIcons.Add` (08_Icons.cs) or
  `RWContent.Boot` (15_Content.cs) can throw; I did not read those files. In the tested setup the boot logs "ready", so
  none of them threw there.
- Tags: none
- What happens: one throw ends the `Boot` coroutine, and from a nested `yield return` it never reaches the caller (pitfall
  #133). `Ready` (:87) is then never set, and `Update` returns at :102 for the rest of the launch. That turns off:
  - the main-menu button;
  - the boat (warp, guard, Continue);
  - the `ModKitTides` hooks, which are never subscribed (:71-83);
  - rejoin and the load watchdog.
  The only trace is Unity's own exception line. If the throw comes before `RWMode.Register`, the mode and its twins are
  not registered either. A client that joins a host on a chart then waits on its boat's loading screen forever:
  `Startup` waits for the world item with no timeout (Macro_Main.cs:400).
- Why (engine evidence): Unity's coroutine semantics (#133). No game code is involved.
- Fix: wrap each of the three in try/catch, like their neighbours. Step `RWContent.Boot` by hand: `MoveNext()` inside a
  try, `yield return e.Current` outside it. Low-risk.
- PC check: make one of the three throw on purpose in a dev build (for example, rename the asset library). Today the log
  never says `[RogueWavesMode] ready` and the menu button is missing. Once fixed, it logs the warning and then `ready`.

### MODE-A3. `RWUtil.PlayerCore()` / `PlayerCores()` also match sub-cores, so "the Basin" can be a sub-core on five maps
- Where:
  - `TDS_Mod/ModSource/RogueWavesMode/Code/01_Util.cs:65-85`: `PlayerCore` :65-73, `PlayerCores` :75-85.
  - The callers:
    - `17_Cardinals.cs:227` and `:234` (`GrantCardinal`);
    - `17_Cardinals.cs:314-316` (counts a Cardinal missing on each core);
    - `17_Cardinals.cs:389` (PR #82's re-grant waits for `PlayerCores().Count > 0`);
    - `18_Shop.cs:28`, through :69 (VENDCINT's tile);
    - `19_Objectives.cs:127-128`, `:173` and `:212`, through :207.
- Severity: wrong-behaviour
- Confidence: plausible.
  - Confirmed: sub-cores carry the `Core` tag. `Mode_Normal.Run` queries the tag `Core` (Mode_Normal.cs:345) and later
    tests `Subcore` on those same entities (:454). So `Core && !Enemycore` also matches the sub-cores of Artrio (2),
    Hold (2), Elgu Bounde, Crimson Lythe δ and Crescent Valeur δ (kit `15` §15).
  - Not confirmed: whether a sub-core comes before the Basin in `Game_Entity.Entityclient`, which follows the order the
    map's objects spawn in.
- Tags: none
- What happens: on those maps, `PlayerCore()` ("the player team's first living Basin") may return a sub-core. If it
  does:
  - An objective that watches the Basin's damage records a core's `Damagetaken` when it starts (19_Objectives.cs:127-128)
    and calls `Core()` again when it ends (:173). If the first living core changes in between, for example when a
    sub-core that came first dies, the end reads a different entity than the start.
  - The meteor is placed relative to that core (:212).
  - VENDCINT lands next to it (18_Shop.cs:28-31).
  `PlayerCores()` also puts every Cardinal on each sub-core. "A Cardinal goes on every core" (17_Cardinals.cs:231) is
  deliberate. Whether that differs from the stock game depends on whether a sub-core's own AI applies lobby Cardinals
  when `Mode_Normal` sends it "Spawn" (Mode_Normal.cs:350). If it doesn't, a Cardinal that acts once per core would act
  once per core here.
- Fix: in `PlayerCore()`, also skip `Taglistserver.Contains("Subcore")`. Low-risk. For `PlayerCores()`, first decide
  whether sub-cores should hold Cardinals at all. That one is risky: it changes what Cardinals touch, and the
  missing-count at 17_Cardinals.cs:314-316 would have to agree.
- PC check: on Artrio, at the opening break and again at Tide 1, log `PlayerCore().Being.Value` and the being of every
  `PlayerCores()` entry. A sub-core's name confirms the finding.

### Checked and fine (section 2)

- **The mode loop against Mode_Normal:**
  - `Register` changes only the `Wavetime` of the break rows after Tide 1; the opening break keeps 45 s.
  - `Run` counts its rows once (Mode_Normal.cs:309). RWDifficulty restores the same count at `MapStart`, which fires at
    `Maploaded` (Game_Grid.cs:715), before `Generatemode`/`Startgame` (Game_Init.cs:136-139).
  - `Modegen` is the registered object itself (`Getload`, Game_Rounds.cs:58), so live writes reach the running mode.
  - `Defaultparams` is rebuilt in the clone's `OnEnable` (Modeobject.cs:41-46).
- **The shared `Content` lists:** `new List<Waves>(stock.Enemies.Wave)` (02_Mode.cs:27) shares every `Content` list with
  Mode_Normal. Nothing writes into those lists in place: RWDifficulty assigns fresh lists (21_Difficulty.cs:35-39, :68-99,
  :179-186). Fragile for future code, but not a bug today.
- **Tide 0 at `Run`'s start:** `Wave` and `Entry` default to -1 (Game_Mapvars.cs:62-64), so `ModKitTides` fires
  `Tide(0)` when `Run` writes 0. `RWCampaign.Tide`, `RWWeather.Tide` and `RWObjectives.Begin` all ignore wave 0.
  `Gamecomplete` defaults to "Running" (:66), so there is no false `MapEnd`.
- **Won, lost and left maps:**
  - `Tide(16)` or the break's clear check calls `Won` exactly once.
  - "Win", "Lose", "Return" (EXIT MAP, Tds_Multiplayer_Remote.cs:99) and "Leave" each count at most once; `ended` stops
    a second `MapEnd`.
  - `Lost` saves `Playing = -1` at once, so neither `OnBoat` nor `Continue` counts the map twice.
  - A quit to the main menu during a map spends exactly one retry, whichever of `AtMainMenu` and `ModKitTides` runs
    first.
- **Continue after a crash during a map:** `Playing` is saved at `MapStart`. `Continue` loses that map, reloads the run
  checkpoint and ends the campaign when no retries are left.
- **Map 10:** won in a bonus Tide, it completes at `MapEnd`; after a crash, at `Continue`.
- **Save API:**
  - `Readdata` returns `Loaded = false` with no side effects.
  - Every read handles "" and a missing key.
  - The six old keys were all strings (git history), so `Migrate` copies them as strings, once per `Playerdata` object,
    and never overwrites a new key.
  - Seeds stay decimal strings, parsed as `uint`.
- **Slot switch and clear:** both replace `Playerdata` (Blockage.Tags.Bla_Data.cs:257, :317), including the early return
  that skips `Onslotchanged` (:339). `Live`'s reference check covers every campaign write, and `SlotChanged` can't throw.
- **The boat guard:**
  - Only the leader's click writes `Player_Worldselect`/`Player_Gameplanselect` (Macro_Main.cs:629-639). The gameplan
    key gets a node name, which the `RW_` check catches.
  - Clients never write either key. The host's `Startup` reads both again on every boat (:352-371).
  - The auto-sail's direct `Selectmap_ServerRpc` writes neither key.
- **Clients:**
  - `Keep` uses the route held in the world's name.
  - Charts come only from the seed and the pool.
  - `Selectmap_ServerRpc` finds only active nodes, never the inactive templates.
  - `Changeworld` reuses its copy of a world by name (Macro_Worldvisual.cs:95-117), and its `Update` swaps again when the
    name differs (:1516-1525).
  - Pruning old charts keeps both `World` and `Worlded`.
- **Statics across the main menu and New:** `New`/`Continue` reset every campaign field. `LeaveBehind` resets them
  without touching the save. `RWBoat` resets its per-scene state when `Macro_Main` changes (05_Boat.cs:47-53).
  `lockSaid` is never cleared, so a second campaign in one launch skips one chat line, which is cosmetic.
- **Edges left out:**
  - `MapStart` fires at `Maploaded`, before the host waits for the clients (Game_Init.cs:123-135). A load that later
    sends everyone to the main menu would cost a retry; RWLoad's 75 s watchdog normally comes first.
  - A hand-edited `RW_Campaign_Route` that runs off the chart makes `Continue` return true and never warp.

### Not yet reviewed (section 2)

- `99_SelfTest.cs`: not read, so not checked for false passes or for code that would leak into a release build. (I saw
  in passing that 96_Flow.cs:344 and 93_TwoPlayers.cs:363-371 don't cover MODE-A1.)
- `RWUtil.Ground()`, `EnemyCores()` and `Count()` (01_Util.cs:53-120) read host-only lists. I didn't check that every
  caller runs only on the host.
- `05_Boat.cs:106`: `RWWeather.Preload` runs on slow frames inside `Keep`, with no try. If it threw on every call, `Host`
  would never run on a slow frame, so `Guard`, `Lock` and `RestoreLobby` would stop. Undecided until 07_Weather.cs is
  read.
- `RWWorld.Ensure`/`Build` and `Sea`, `Rocks`, `Buoys`, `Strip` in `04_World.cs`: not traced for a throw that repeats
  every frame.
- MODE-A2's deciders: whether 12_Menu.cs `RWAssets.Find`, 08_Icons.cs `RWIcons.Add` and 15_Content.cs `RWContent.Boot`
  can throw.
- MODE-A3's open questions:
  - the spawn order of the Basin and the sub-cores;
  - whether the stock game puts lobby Cardinals on sub-cores;
  - for the Cardinals reviewer, whether 17_Cardinals.cs:314-316 counts a sub-core that can't hold a Cardinal as
    missing forever.
- PR #80's call-site edits in 00, 02, 04 and 05 were checked only where cited above. PR #82 was not reviewed; it is
  outside 00-06.
- Not traced:
  - a client reaching the boat before its mod is `Ready`;
  - a client whose pool differs, i.e. a different mod set (the same-pack rule).
- No compile was run.

## 3. RogueWavesMode: run state, Directions, Cardinals (`13_Card`, `14_Run`, `15_Content`, `16_Directions`, `17_Cardinals`)

Read in full at `5d9b09e`. Stopped before `92_Gaps.cs`. `~` marks an approximate line in the decompiled game.

### MODE-C1. A Cardinal bought mid-map reaches only what spawns after it; Heroapply ones do nothing for the rest of the map
- **Where:** `17_Cardinals.cs:232-263` (`GrantCardinal`, called from `Buy` while a map runs)
- **Severity:** wrong-behaviour
- **Confidence:** confirmed
- **Tags:** order-of-init, VENDCINT, stock Cardinals
- **What happens:** `GrantCardinal` upgrades every core with the Cardinal's core upgrade and then stops. The stock apply-kind
  Cardinals on sale only act on spawns, or on child events, that come after that. Rusty Pipe and Frail Titan (Towerapply)
  and Voice in a Bottle (Voiceapply) reach only towers and units built afterwards. Beast Tassle, Sport Water, Paper Tower and
  Fluffy Slippers (Heroapply) reach the vessel only when it is parented to the core, which happened at map start, so they do
  nothing for the rest of the map they were paid for in. The re-grant at the next map is the known Basin-order bug, which I
  don't count here.
- **Why:** `Entity_Ai_Artifact_Towerapply.cs:26-37` subscribes `Game_Entity.Onspawn` in `Spawn()`, and its handler only adds to
  the new entity's `Initupgradelistserver` (`:19-21`). `Entity_Ai_Artifact_Voiceapply.cs:12-29` does the same.
  `Entity_Ai_Artifact_Heroapply.cs:6-17` acts only in `Child(Master)`. A catch-up pass after a grant was proven in game before (see the lead note).
- **Fix:** low-risk (proven in game before). After the core loop in `GrantCardinal`, when a map is running, port `CatchUp`: upgrade
  each existing matching same-team entity with the effect upgrade (the stock handler's filter) for Towerapply and Voiceapply,
  and each existing vessel with the listed upgrades for Heroapply, as `Child` would.
- **PC check:** Mid-map, buy Sport Water and Rusty Pipe at VENDCINT. Read the vessel's `Upgradelistserver` and the
  `Upgradelistserver` of a tower built before the purchase: the `Effect_Artifact_Caffeine_*` and `Effect_Artifact_Rustypipe_*`
  entries are missing today. The vessel's speed is also unchanged.
- Lead note: the mod's half re-read at `63af4b0`: `GrantCardinal` upgrades the cores only (`17_Cardinals.cs:252-255`). The catch-up pass the fix names was proven in game before (`TDS_Mod/ModSource/Riptide/Code/05_Artifacts.cs`, `CatchUp`).

### MODE-C2. Spring Tide's stock Beacon cancels Smacky's Beacon damage cut on every tower built after Spring Tide
- **Where:** `16_Directions.cs:42` (Spring: `Entity_Ai_Effect_Smackywall_Beacon` with Effect_Multi 1), `:317-348` (`Reach`, the
  upgrade is at `:339`)
- **Severity:** wrong-behaviour
- **Confidence:** confirmed
- **Tags:** Smacky, stock-script reuse, order-of-init
- **What happens:** On a tower spawned after its owner took Spring Tide, Spring Tide's Beacon starts first. It starts inside
  `Onspawn`, before `Movebeing` gives the tower the tile aura. When Smacky's own Beacon effect then arrives through its aura,
  it finds `Resistadd` already there and marks itself as already active, so it never divides the hit's damage. Spring Tide's
  instance does the dividing instead, by its Effect_Multi of 1. The tower loses Smacky's Beacon damage cut (÷1.5) for its
  whole life. Towers built before Spring Tide was taken keep the cut, because Smacky's instance was first on them. The same
  Smacky build therefore behaves differently depending on build order.
- **Why:** In `Entity_Ai_Effect_Smackywall_Beacon.cs:34-52`, `Starting` creates `Resistadd` when it is absent and otherwise
  sets `Alreadyactive`. At `:23-25` it divides `Damagemulti` by Effect_Multi only when `!Alreadyactive`. `Game_Entity.cs:271-273`
  fires `Onspawn` before `Movebeing` at `:278`, and `Helper_Entitytools.Upgrade` runs the new AI's `Spawn()` and `Starting()`
  at once (~`:532-539`).
- **Fix:** risky, because it changes a Direction that has been played. Stop reusing the stock Beacon for Spring Tide, and give
  the per-Tide health with the mode's own Health Add mod (keep the handle and edit it per Tide and per rank). The fix for C3 is
  the same.
- **PC check:** As Smacky, take Spring Tide. Build one tower before and one after, both receiving Smacky's Beacon effect. Hit
  both with the same damage, or read each Beacon instance's `Alreadyactive` by reflection: the tower built after takes 1.5×
  the damage.

### MODE-C3. Spring Tide's `Resistadd` = 1 halves the damage Fie's Regen walls take
- **Where:** `16_Directions.cs:42` and `:339` (the same upgrade as C2)
- **Severity:** wrong-behaviour
- **Confidence:** confirmed
- **Tags:** Fie, stock-script reuse
- **What happens:** When Spring Tide's Beacon is the first Beacon on a tower, it creates `Resistadd` with an Add mod of 1 (and a
  Multi mod of Effect_Multi − 1 = 0). Fie's wall Regen effect divides every hit by 1 + `Resistadd` whenever that stat exists.
  For a Fie player holding Spring Tide, segments grown by Spread5 (which copy the parent's `Resistadd` and get Regen) take
  half damage, and Shield3 walls divide by 2 + R instead of 1 + R. This is a large defence buff that nothing intends.
- **Why:** `Entity_Ai_Effect_Smackywall_Beacon.cs:37-47` (Add 1). `Entity_Ai_Effect_Fiewall_Regen.cs:10-12` (÷ 1 + Resistadd).
  `Entity_Ai_Tower_Fiewall.cs` ~420-425 (Spread5 copies `Resistadd` and adds Regen) and ~279-302 (Shield3 adds to `Resistadd`).
- **Fix:** the same as C2 (the mode's own Health mod instead of the stock Beacon); risky.
- **PC check:** As Fie with Spread5 and Spring Tide, read a grown segment's `Statsserver["Resistadd"]`. It is 1 or more; without
  Spring Tide or Shield3 it is absent. Compare the damage the segment takes.
- Lead note: **known**. The same meeting of Spring Tide's `Resistadd` and Fie's Regen is in the simplification build report (`SIMPLIFICATION_BUILD_2026-11-04.md` §2 item 4), and its PC check R3 asks for it on today's build. This finding confirms it from code. R3 says to write it down as pitfall #150; that number has since gone to PR #82's entry, so it would be #151.

### MODE-C4. Cheap Refits rewrites the upgrade price every 0.5 s and wipes Voice in a Bottle's discount
- **Where:** `16_Directions.cs:351-359` (`Refit` writes `c.Goalvalue = scale`). `Grant` calls it at `:239-243` on every owned
  tower on each 0.5 s Tick.
- **Severity:** wrong-behaviour
- **Confidence:** confirmed
- **Tags:** Voice in a Bottle (sold), absolute write over a relative stock effect
- **What happens:** Voice in a Bottle lowers each tower's `Upgrade` command `Goalvalue` by a step per Tide and adds its counter
  back after an upgrade. `Refit` writes the absolute value `scale` whenever the price differs from it, so within 0.5 s it
  undoes every Voice step on the towers of a Refits holder. Voice's counter (the discount it thinks it gave) keeps growing.
  After an upgrade, Voice's `Resetmod` adds that counter back, and the price sits above `scale`, possibly above 1, until the
  next Tick. A player holding both pays the Refits price only.
- **Why:** `Entity_Ai_Effect_Artifact_Voice.cs:11-27` (a relative step per Tide on `Upgrader.Goalvalue`) and `:33-39`
  (`Resetmod` adds the counter back). The price paid is the `Upgrade` command's Goalvalue (`Entity_Ai_General_Upgrade.cs`
  ~73-75).
- **Fix:** low-to-medium risk. Make Refit relative, and apply it once per tower and per rank: keep the factor already applied
  per tower, and on a change multiply `Goalvalue` by `scale / applied` instead of writing `scale`.
- **PC check:** Hold Voice in a Bottle and Cheap Refits. After two Tides, read an existing tower's `Upgrade` Goalvalue: today
  it is exactly the Refits scale, when it should be lower by Voice's steps.
- Lead note: re-read at `63af4b0`: `Refit` (`16_Directions.cs:351-359`) writes `c.Goalvalue = scale` whenever the price differs.

### MODE-C5. Tidal Mend's heal is frozen at the tower's level when the effect started
- **Where:** `16_Directions.cs:35` (Mend: `Entity_Ai_Effect_Smackywall_Regen`, Effect_Add 5/8/12), `:317-348` (`Reach`)
- **Severity:** wrong-behaviour
- **Confidence:** confirmed
- **Tags:** stock-script reuse, order-of-init
- **What happens:** The stock Regen works out its heal once, in `Starting`, as Effect_Add × the entity's Level. A tower built
  after Mend gets the effect inside `Onspawn`, while its Level is still the default 1, so it heals 5/8/12 per Tide. A tower
  that already stood when Mend was taken or ranked up gets the effect at its current level: at level 3 it heals 15/24/36. No
  tower's heal follows its later upgrades. The same rank therefore heals different amounts depending on when each tower was
  built or last swept.
- **Why:** `Entity_Ai_Effect_Smackywall_Regen.cs:22-38` (heal = `Effect_Add * Level` in `Starting`). With only the Priority −3
  effect present (the `Onspawn` case), `Helper_Entitytools.Upgrade` clamps Level to `Option_Defaultlevel` (~`:543`).
  `Entity_Master.cs:293-324` skips AIs that have already started (the `Started` guard), so Start does not re-run the effect.
- **Fix:** low-to-medium risk. Decide whether the heal is flat or scales with level. For flat, build per-level copies with
  Effect_Add / Level (the way Cardinal copies are built) and re-apply them on the tower's `Upgrade` delegate. For per-level,
  re-apply Mend on every upgrade of a tower.
- **PC check:** Take Mend rank 1 while a level-3 tower stands, then build a new tower. Damage both and end a Tide: they heal
  15 and 5.

### MODE-C6. Cheap Refits never reaches the Bouthouse, and Cut Costs misses the build command on Fie's walls
- **Where:** `16_Directions.cs:242` (the Refit loop keeps only beings with `Tags.Contains("Tower")`) and `:283-291` (`Scale`
  edits only the vessel's `Buildoption*` commands)
- **Severity:** wrong-behaviour
- **Confidence:** confirmed
- **Tags:** Boutman (#85), Fie
- **What happens:** (a) Boutman buys the whole tree on the Bouthouse. Its being is tagged `Bouthouse` and `Herotower`, not
  `Tower`, so a Boutman with Cheap Refits pays full price for every upgrade. (b) Fie extends a wall through the wall's own
  `Buildoption1` command. `Scale` edits only the vessel's commands, so Cut Costs gives no discount on segments built from a
  wall.
- **Why:** `TDS_Vessel_Creator/reference/vessel_data_survey.txt:534-536` (the Bouthouse's tags; it carries
  `Entity_Ai_General_Upgrade`). `Entity_Ai_Tower_Fiewall.cs` ~347-352 (Connect2 puts `Buildoption1` on the wall).
- **Fix:** low-risk. In the Refit loop, also accept the `Bouthouse` tag. Refit only edits a command, so #85's ban on upgrading
  the Bouthouse doesn't apply. In `Scale`, also scale the `Buildoption*` commands on the holder's Fie walls.
- **PC check:** As Boutman with Cheap Refits, read the Bouthouse's `Upgrade` command Goalvalue: today it stays 1. As Fie with
  Cut Costs, check the price of a segment built from a wall: today it is unchanged.

### MODE-C7. Two static dictionaries grow every map and keep dead vessels
- **Where:** `16_Directions.cs:249-259` (`statMods`, keyed by `vessel.GetInstanceID() + d.Name`) and `17_Cardinals.cs:347-378`
  (`dealt`, keyed by `Entity_Master`)
- **Severity:** leak
- **Confidence:** confirmed
- **Tags:** static state
- **What happens:** Each map spawns new vessels. Both dictionaries add entries for them and never remove or clear any, so
  destroyed vessels' masters and modifiers stay referenced for the whole session. The amount is small, but it grows with each
  map and each respawn.
- **Why:** Neither dictionary has a `Remove` or `Clear` anywhere in the mode. `RWHits.Reset` at MapStart (`20_Vote.cs`) is the
  existing per-map reset point.
- **Fix:** low-risk. Clear both dictionaries next to `RWHits.Reset`.
- **PC check:** none needed (code reading).

### MODE-C8. `RWDirections.OnSpawn` upgrades inside `Game_Entity.Spawn` with no try/catch
- **Where:** `16_Directions.cs:305-315` (`OnSpawn`) and `:317-348` (`Reach`)
- **Severity:** crash
- **Confidence:** plausible
- **Tags:** order-of-init (the same class as the known Basin bug), in-PR80 call site (`RWHits.Resist`), survives the move
- **What happens:** `Reach` runs `Helper_Entitytools.Upgrade` and `RWHits.Resist` from the `Onspawn` delegate, before the entity
  has its stats, AIs, tile and lists. I found nothing in today's three scripts that throws there. If anything ever does (a
  null lookup in `Reach`, or a later Script Direction or Trinket that reads a stat in `Starting`), the exception escapes
  `Game_Entity.Spawn` at the `Onspawn` call. The entity then skips `Movebeing` and `Addlists`, and the delegate chain stops, so
  the subscribers after it (the stock Towerapply and Voiceapply handlers) miss that spawn. The kit's rule is to hand
  spawn-time effects to `Initupgradelistserver`, which is what the stock apply-kinds do.
- **Why:** `Game_Entity.cs:271-273` (`Onspawn`), `:278` (`Movebeing`), `:283` (`Loaded += Addlists`).
  `Entity_Ai_Artifact_Towerapply.cs:19-21` (the stock pattern).
- **What decides it:** whether any path in `Reach` can throw on a real spawn, for example a spawn whose owner has no RWPlayer,
  or whose `Beingserver.Entity` is null.
- **Fix:** low-risk: wrap the body of `OnSpawn` in try/catch with `RWLog.Once`. Moving the effect upgrades to
  `m.Initupgradelistserver` (the kit's way) would start them after `Movebeing`. That may also put Smacky's Beacon first (C2),
  so verify it on the PC together with C2's fix.
- **PC check:** In a self-test, hold a Script Direction and spawn a tower for an owner with no RWPlayer.
- Lead note: the same multicast `Onspawn` is the path ULO-2 names (section 9): a throw in any part's spawn hook reaches the caller of the spawn.

### MODE-C9. `RWRun.KeyOf` can give one player two keys
- **Where:** `14_Run.cs`, `KeyOf` (a GUID from `Blm_Multiplayer.Sessionids`, else `"name:"+Displayname`, else `"session:"+id`)
- **Severity:** save
- **Confidence:** plausible
- **Tags:** per-player keys, save round-trip
- **What happens:** Suppose the GUID lookup fails at one call (before `Sessionids` holds the host's own id, or after a rejoin
  changes the session id) and succeeds at another. The same player is then saved under one key and looked up under the
  other, and `Load` starts them fresh, losing their Directions, Trinkets and Pure Aura.
- **What decides it:** whether `Blm_Multiplayer.Sessionids` holds every player's id, including the host's, at every point
  where `KeyOf` runs: lobby, MapStart, Save and after a rejoin.
- **Fix:** low-risk. Prefer the GUID, and when a later call finds a GUID for a player saved under a name key, move that entry
  to the GUID key.
- **PC check:** Log `KeyOf` for the host and a client at the lobby, at MapStart and after a rejoin.

### MODE-C10. A rejoiner's Cardinal row may not line up with the host's counters
- **Where:** `17_Cardinals.cs`, `Row()` (appends `info.Artifacts` entries to `Game_Mapvars.Artifactobjects` on every machine)
- **Severity:** online-only
- **Confidence:** plausible
- **Tags:** HUD row, rejoin
- **What happens:** The HUD row's counters (`Artifacttrack`, 8 floats) are indexed by position in `Artifactobjects`. A client
  that rejoins builds that list from the lobby data at join time and then appends to it. If its order differs from the order
  the host appended in, or it ends up with a duplicate, counters such as Voice's and the coins' show under the wrong
  Cardinal.
- **What decides it:** how a joining client's `Game_Mapvars` builds `Artifactobjects` from `Tds_Multiplayerinfo.Artifacts` and
  `Mixedart`, and whether `Row()` skips entries that are already present.
- **Fix:** low-risk if confirmed: rebuild the list in one fixed order on every machine (`Mixedart` first, then the order of
  `info.Artifacts`).
- **PC check:** Buy two Cardinals, rejoin a client, and compare its row and counters with the host's.

### MODE-C11. Cardinals past the HUD's 8 slots appear nowhere
- **Where:** `17_Cardinals.cs:258` (adds to `info.Artifacts` only while `Count + Mixed < 8`) and `14_Run.cs`, `RWRunLabel`
- **Severity:** wrong-behaviour
- **Confidence:** plausible
- **Tags:** HUD row 8
- **What happens:** From the 9th Cardinal on (the 8th with a Mixed pick), each new Cardinal works but appears nowhere. The
  header of `17_Cardinals.cs` says the run line shows a count, but `RWRunLabel` shows only "Pure Aura N" and the Goalstring of
  `RW_Run`.
- **What decides it:** whether `RW_Run`'s line was meant to carry the overflow count, as the header says.
- **Fix:** low-risk: add "+N Cardinals" to the run line once `shown` is full.

### MODE-C12. Voice in a Bottle's step uses integer `30 / Totalwaves`
- **Where:** the Cardinal is sold through `17_Cardinals.cs:39-43` (it is not in `NotSold`)
- **Severity:** wrong-behaviour
- **Confidence:** plausible
- **Tags:** stock Cardinal on sale, engine assumption
- **What happens:** Voice's step per Tide is `Rate_Base/100 * (float)(30 / Totalwaves)`, and the division is integer. With more
  than 30 waves the step is 0 and the Cardinal does nothing. From 16 to 30 waves the step is ×1, and at 15 or fewer it is ×2
  or more.
- **Why:** `Entity_Ai_Effect_Artifact_Voice.cs:15-17`.
- **What decides it:** the value of `Mode.Enemies.Totalwaves` on a Rogue Waves map.
- **Fix:** low-risk: if Totalwaves is above 30, add `Artifact_Voice` to `NotSold`.

### Checked and fine (section 3)
- Executestart does not re-start the AIs that `Reach` started inside `Onspawn` (the `Started` guard at `Entity_Master.cs:293-298`
  and `:318-324`, and `Helper_Entitytools.cs` ~532-539). Regen is not subscribed twice and Beacon's Health mod is not added twice.
- The Level set during the `Onspawn` upgrade clamps to `Option_Defaultlevel`, and Executestart's init upgrades then set the real
  Level.
- The Pure Aura counter stat made before Start survives, because `Addstat` skips names that already exist and clears nothing.
- Commands made with `Createcommand` before Start survive.
- Tougher excludes Hero beings (Boutbro, Buster), and every `Reach` skips VENDCINT (owner −1).
- Tower Directions are never added to the Bouthouse as upgrades (#85). C6 is only about the command-only Refit.
- Stat Directions keep their `Link_Modifier` handles and change rank through the `Value_Multi` and `Value_Add` setters, so
  `Changed` fires (#119).
- A rank-up removes the lower-rank effect upgrade before it adds the new one (#116).
- Bottom Rock works through `Controllable`, not the upgrade Goalvalue, so it does not clash with Refits (and it is not sold).
- Trinket and Cardinal grants pass their `Artifactobject` (#117). The Trinket grant skips definitions without a script, and
  Dentures works in `Update` instead.
- PayTide (`wave <= WinTide ? 3 : 2` plus weather) and Interest pay out only when wave > 0 (`20_Vote.cs` Break).
- `RWHits` copies `hit.Data` before editing it and keeps itself at the front of the chain (`in-PR80`: the same body moves to
  `ModKitUtil.Front`, with nothing to carry over).
- `Count()`'s prefix matching keeps Moon apart from Mooncore and Loop apart from Loopedmirror, and `#n` stacks parse.
- `RWRun.Save` runs only while Live, and `Decode` rejects a line with fewer than 7 fields.
- `RWRun.Vessel` returns null when the being is known but not matched (Boutbro, #145), and the `Bl_Address.Getload("Hero", ...)`
  label is valid.
- Ask, RunLine, Scale and Refit call `Updatecommand(..., false, false)`, so no command runs.
- `13_Card.cs` (read in full): card, lobby and tip text only; no defect found.
- `15_Content.cs` (read in full): its own `RW_Content` group; script carriers named after their classes; an upgrade template
  with Priority −3 and empty Costs; VENDCINT cloned from `Mob_Smackyminer`. No defect found.

### Not yet reviewed (section 3)
- `92_Gaps.cs` (debug-only, 1505 lines) is not read, and nothing has been checked for false passes. Look first at whether its
  checks for Spring Tide, Refits, mid-map Cardinals and Dentures only test that an upgrade is present. Such checks would pass
  despite C1-C6.
- Vampire Dentures on each vessel: whether every vessel's being handles the `Interactheal` "Heal" interaction (Buster has no
  `Mob_General`), and whether `Entity_Master.Damagedone` counts the vessel's own hits.
- Mid-map timing of Diorama (Heroapply_Delay) and of the Coreapply Cardinals (Fragment, Glass Core, Opal Core). C1 covers
  only Towerapply, Voiceapply and Heroapply.
- Blurry Photo's clone, if a lobby pick allows it (it isn't sold): whether `RWRun.Vessel` can pick the clone over the vessel.
- The deciding facts of C9-C12: `Blm_Multiplayer.Sessionids` contents, how a joiner builds `Artifactobjects`, `RW_Run`'s
  Goalstring, and the mode's `Totalwaves`.
- `05_Boat.cs` `RestoreLobby` and the vessel lock, read only where `17_Cardinals.cs` calls into them. The Interest maths
  (`16_Directions.cs:375-389`) was read but not checked against `Giveresources` rounding.

## 4. RogueWavesMode: weather, icons, rejoin, online, dialogs, menu (`07`-`12`)

Read line by line at `5d9b09e`, with `00_RogueWavesModeMod.cs` and `01_Util.cs`. Stopped before `93_TwoPlayers.cs`.

### MODE-B1. Any drop, rejoin or turned-away join drops Quickening Current's speed to x1 for everyone, and a rejoiner never gets the current speed
- Where: `TDS_Mod/ModSource/RogueWavesMode/Code/07_Weather.cs:243-259` (the only `Time_Scale` writes, at Tides 5 and 10);
  `09_Rejoin.cs:409-413` (the rejoiner relies on `Startvisuals` for the speed), `:284-285` (the only re-apply, and only after
  `Unstick`)
- Severity: online-only
- Confidence: confirmed (code + engine read)
- Tags: (none)
- What happens: three friends play a Quickening Current map, and after Tide 10 the game runs at x1.5. One friend's Wi-Fi drops.
  From then on the host and the remaining client run at x1 for the rest of the map, while `Time_Scale` still reads 1.5. The
  weather quietly does nothing, though it still pays its extra Pure Aura. When the friend rejoins, every machine resets to x1
  again, and the rejoiner runs at x1 too. A drop between Tides 5 and 10 loses x1.25 until Tide 10, where x1.5 is written.
  A friend who is turned away mid-map (join, then kick) triggers the reset twice. The players' own fast-forward toggle
  (`Time_Speedup`) is lost the same way. That loss is stock behaviour, but the pack is what lets players join mid-map.
- Why (engine evidence): every machine's map pause menu adds `Scalechange` to the static `Blm_Server.Lobbyupdate`
  (`Tds_Pausemenu.cs:248`). `Scalechange` sets `Time.timeScale = 1f` (`:262-265`), and `OnDestroy` never removes it
  (`:267-276`), so the copy from every past scene fires too. `Lobbyupdate` runs on every machine from
  `Blm_Server.Updateplayercount` (`Blm_Server.cs:143-161`). That is called by `Lobbyupdatedown_ClientRpc` (`:164-227`), which
  the host sends on every connect and disconnect (`Blm_Multiplayer.cs:288-300`, subscribed at `:380-381`), and by every player
  object's spawn and despawn (`Blm_Player.cs:62`, `:124`). The speed is applied only by the hook
  `Game_Client_Init.Timescalealter`, and only when `Time_Scale` or `Time_Speedup` *changes* (`Game_Client_Init.cs:213-226`,
  hooked at `:315-318`). NGO doesn't fire on an equal value, and the mode writes `Time_Scale` only twice per map. `Main_Chrono`
  survives this because it writes the speed again at every Tide (kit `16` §3). For the rejoiner: `Startvisuals` calls
  `Timescalealter` at `:319`, one line *before* `Gametimescale = true` (`:320`), so that first call is a no-op. The call at
  `:323` sits inside the speed-up hook's own local function. The client therefore stays at x1 until the next change.
- Fix: on every machine, every frame of a campaign map, set
  `Time.timeScale = Time_Scale.Value * (Time_Speedup.Value ? 1.5f : 1f)` when it differs. Do it only when
  `Game_Client_Init.Instance != null && Instance.Gametimescale && Time.timeScale != 0f` and no photo mode is up
  (`Tds_Photomode.cs:291` sets its own scale). This is the re-apply that `09_Rejoin.cs:284-285` already runs after `Unstick`,
  extended to every machine. Risky: it touches timing everywhere, and it must stay out of the solo pause (0) and photo mode.
  A host-only variant: one frame after each `Blm_Server.Lobbyupdate`, nudge `Time_Scale` (the target + 0.0001, then the target
  on the next frame) so that every machine's own hook applies it again.
- PC check: `Run-TwoPlayers.ps1` on a Quickening Current map, with `RWWeather.QuickTide` lowered to 1 as `99_SelfTest.cs:36`
  does. After the speed-up, log `Time.timeScale` and `Game_Mapvars.Instance.Time_Scale.Value` on both machines. Then drop the
  client (`NetworkManager.DisconnectClient` on the host, pitfall #148), rejoin, and log both values again on both machines.
  Expected: 1.25 and 1.25. The code predicts `Time.timeScale` 1 against `Time_Scale` 1.25 on the host, on the client, and on
  the rejoiner after its catch-up. A check that reads only `Time_Scale` passes either way.
- Lead note: the engine half re-read: `Tds_Pausemenu` adds `Scalechange` to `Blm_Server.Lobbyupdate` (`:248`) and `Scalechange` sets `Time.timeScale = 1f` (`:262-265`). The 2026-11-04 test round's "Quickening on a client: pass" had no drop in it.

### MODE-B2. `RWRejoin.Repair` restarts client halves that are still alive, not only the dead ones: visual scripts added twice, every handler hooked twice, a stale entry left in `Entityclient`
- Where: `09_Rejoin.cs:348-353` (restarts every `Entity_Master_Client` with `Master == null`). This runs 1 s after `Localplayer`
  arrives (`:400-402`), and in the `initDied` case right after `Repair` sets `Loaded` itself (`:332-334`). Also `Looks`
  `:374-388`.
- Severity: online-only
- Confidence: the code path is confirmed (code + engine read). How many things it hits and how visible the doubles are is
  plausible. What decides it: the "rejoin: started again what the late player object stopped: N entities" log line of a rejoin
  in which no `Entity_Master_Client.Start` NullReferenceException came first, i.e. #139's order didn't bite, as in the #141
  rejoin after a game restart. A large N there means live halves were restarted.
- Tags: (none)
- What happens: a client half (`Entity_Master_Client`) that is alive and still waiting for the local player's `Loaded` also
  has `Master == null`, so `Repair` runs its `Start` a second time, and both runs go through. Each such thing then:
  - carries two copies of every visual script;
  - plays its spawn effect and sound twice;
  - reacts twice to every move, turn, state, skin and upgrade change;
  - is listed twice in `Game_Entity.Entityclient`, and when it dies, one destroyed entry stays in that list for the rest of
    the map.

  Which things are hit depends on the join:
  - If the player object arrives late (#139): the things spawned in the second between it and `Repair`, such as Tide foes and
    a teammate's new tower.
  - If it doesn't (the #141 rejoin after a game restart): every client half on the map waits on `Loaded` with
    `Master == null`, and if the map's load (`Initalize`) isn't done within that second, `Repair` restarts all of them.

  `Looks` does the same to a single thing, rarely (see below).
- Why (engine evidence): `Master` is assigned only inside `Start`, after a once-a-frame wait for the local player's `Loaded`
  (`Entity_Master_Client.cs:136-140`); nothing else in the game writes it. After a game restart, `Loaded` stays false until
  `Game_Client_Init.Initalize` has loaded every group (`Game_Client_Init.cs:39-175`). In the cable-pull case, `Repair` sets
  `Loaded` in the same call as the restart loop, so no waiting half has seen it yet. The second run:
  - adds the thing to `Entityclient` again (`Entity_Master_Client.cs:169`);
  - runs `Generatesetup()` (`:202`), which `AddComponent`s every visual script with no guard (`:83-99`);
  - hooks every NetworkVariable again (`:191-225`);
  - plays `Vfxstart`/`Sfxstart` (`:216`, `:220`) and sends `Pingload_ServerRpc` (`:230`) again.

  `OnDestroy` removes only one list entry (`:101-125`). In `Looks`: a half whose group is late waits at `:183`, but it read
  `Beingobject` before the wait (`:166`), so it dies as soon as the group arrives (#141). If a `Looks` pass lands in the frame
  between the group arriving and that wait returning, it restarts the half. The restart fills the shared `Beingobject`, so
  the first run no longer dies, and both runs complete.
- Fix: in `CatchUp`, after the `initDied` part of `Repair`, wait until the local `Loaded` is true, then two more frames. By
  then every live half has passed its wait and set `Master`, so restart only the halves whose `Master` is still null: those
  are the ones #139 killed. In `Looks`, require the being to have been loaded on the previous pass too. Low risk: both changes
  only narrow what is restarted.
- PC check:
  1. Read the Player.log of the 2026-11-04 rejoin after a game restart (#141). Look for the "started again ... N entities"
     line, and for any `Entity_Master_Client.Start` NullReferenceException before it.
  2. In a two-player round, rejoin mid-Tide twice: once with a pulled cable, once with a game restart.
  3. On the rejoiner, 3 s after that line, log the count of each `Entity_Vis_Base` type on each thing and the duplicates in
     `Game_Entity.Instance.Entityclient`.
  4. After the Tide, count the destroyed (`== null`) entries left in `Entityclient`.

  The code predicts doubles for the restarted things.

### MODE-B3. A late client's catch-up outlives its map: after a turn-away, the next map's fly-in plays under the loading screen
- Where: `09_Rejoin.cs:397-419` (`CatchUp`: waits up to 90 s for `Localplayer`, then up to 180 s for `LocalLoaded()`, and
  never checks that the map is still the same), started at `:279`. `Looks` (`:368`) checks only that
  `Game_Mapvars.Instance != null`.
- Severity: online-only
- Confidence: confirmed (code + engine read)
- Tags: (none)
- What happens: a friend tries Join Game mid-map and is turned away, as designed (`TurnAway`). Their machine had already
  judged itself a rejoiner (`late`) and started `CatchUp`, which goes on waiting on the main menu. The host's chat said "join
  on the boat", so they do, and the party starts the next map within about 3 minutes of the turn-away (4.5 at most). That
  friend's own load of the new map finishes while the loading screen is still up and before the host has started the map. At
  that point `CatchUp` calls `Setintroposition` and `Startvisuals`, and the real call at "Loading 80%" then does nothing.
  The player loses the map's fly-in and title, which play hidden under the loading screen, and other mods' `Gameclient`
  prefabs are switched on before the map has started. Nothing gets stuck: at 80% `Game_Load` hides the loading cameras on its
  own (`Game_Load.cs:250-257`), and the camera is already where the fly-in would have left it. Each turn-away starts another
  `CatchUp` (a new map resets `late`, `09_Rejoin.cs:256-260`), and MODE-B4 invites repeats. A rejoiner who drops again during
  its own catch-up leaves one behind in the same way.
- Why (engine evidence): `LocalLoaded()` (`09_Rejoin.cs:457-461`) turns true as soon as any map's `Game_Client_Init` exists
  and the local `Loaded` is true. That flag is set at the end of this client's own `Initalize` (`Game_Client_Init.cs:175`).
  This happens before the host can set `Gameloaded`, which waits for every client's load (kit `19` §3; the mod's own comment at
  `09_Rejoin.cs:268-269`). So it also comes before `Game_Load` passes its waits for `Gameloaded` (`Game_Load.cs:199`, `:240`)
  and calls `Startvisuals` at 80% (`:248-249`). `Startvisuals` runs once per map (`Visualsstarted`,
  `Game_Client_Init.cs:230-235`) and starts with `Activateprefabs("Gameclient")` (`:234`).
- Fix: capture `Game_Mapvars.Instance` when `CatchUp` and `Looks` start, and stop as soon as `Game_Mapvars.Instance` is no
  longer that object (check after every wait). Low risk.
- PC check: in a two-player round, have a joiner turned away mid-map (a third copy, or the client after a pretend turn-away).
  They then join on the boat after the map ends, and the next map is started within 3 minutes. Proof: in that client's log, a
  "rejoin: map loaded here after ... s; camera, HUD and game speed started" line before its "Loading 80%" line. By eye: no
  fly-in on that client.

### MODE-B4. The main menu tells a turned-away player to "Join Game" again, which only turns them away again
- Where: `09_Rejoin.cs:482-491` (arms `inMap` for any connected client whose map reads "Running" or ""), `:469-481` (adds the
  line on the drop), `:500-503`
- Severity: online-only
- Confidence: confirmed (code + engine read). The timing (the map's objects are there before the kick) comes from the join
  order.
- Tags: (none)
- What happens: a friend who is not in the map tries Join Game, and the host turns them away. The chat line saying "New
  players join on the boat" is seen only by the host's party. The friend lands on their main menu, whose error box reads:
  "Lost the map. If Host is still playing: Steam friends, right-click Host, Join Game." They follow it and are turned away
  again (O5 shows that happens).
- Why (engine evidence): the host kicks only after the joiner's `Sessionid` round trip (`Blm_Player.cs:66-110`). By then the
  joiner has the scene's networked objects, so `Game_Mapvars` exists on the joiner and `Gamecomplete` reads "Running" (its
  default, `Game_Mapvars.cs:66`). That is enough for `Watch` to set `inMap`. The kick ends the connection, `Watch` adds the
  line, and the main menu shows the newest entry (`Bla_Ui_Multiplayererrornotif.cs:21`).
- Fix: arm `inMap` only once this machine has really played the map, i.e. when the local `Tds_Clientinfo.Activeplayer` is
  above 0. The host gives a rejoiner 1 back in `Restore`, while a turned-away player keeps 0. Low risk.
- PC check: repeat O5, then read the turned-away client's main-menu error box. Expected: the stock text or a "join on the
  boat" line. The code predicts the "Lost the map ... Join Game" line.

### MODE-B5. Weather's `Begin` isn't tied to its map: a weather map left during its waits can put its popup and its object on the next map
- Where: `07_Weather.cs:221-241`. `Begin` waits 2 s, then up to 60 s for the beings, then without limit for the deter, then
  runs `Announce` and the spawn, and never checks that the map is still the same. `DeterUpdate` (`:151-165`) returns early
  while no map is up, and starts its own 30 s clock again on the next map.
- Severity: wrong-behaviour
- Confidence: plausible. What decides it: whether a weather map ever starts with its beings or its deter missing, so that
  `Begin` is still waiting when the host leaves. Normally the boat has preloaded them, and the waits end at once.
- Tags: (none)
- What happens: take a Rising Water or Nesting Ground map whose objects were not ready at the start. The host leaves within
  the minute (pause menu, Exit), and the party starts the next map from the boat. `Begin` is still running on the mod's
  persistent object, so when its waits end, on the new map it:
  - shows "RISING WATER ROLLS IN" (or the nest's line);
  - spawns the flood or the nest, even if that map is calm;
  - overwrites `RWWeather.Flood` or `Nest`.
- Why (engine evidence): the coroutine runs on `RogueWavesModeMod`, which survives scene loads (`DontDestroyOnLoad`,
  `00_RogueWavesModeMod.cs:31`). `Announce`, `SpawnFlood` and `SpawnNest` (`07_Weather.cs:269-275`, `:278-301`, `:358`) act on
  whatever map is current: the first enemy core (`RWUtil.EnemyCores()`, `01_Util.cs:53`) and the ground
  (`RWUtil.Ground()`, `01_Util.cs:87`).
- Fix: capture `Game_Mapvars.Instance` in `MapStart` and pass it to `Begin`. After each wait, stop if
  `Game_Mapvars.Instance` is no longer that object. Low risk.
- PC check: in a test build, skip `RWBoat`'s preload for a Nesting Ground node. Leave the map within 20 s and start a calm
  map. Look for a "weather Nesting Ground on <old node>" line or the popup on the new map.

### MODE-B6. The rejoin's "what died" flags outlive their map
- Where: `09_Rejoin.cs:315-325` (`OnLog` sets `initDied`, `artifactsDied` or `endDied` on any client exception with the
  matching stack, in any map); `:328-357` (only `Repair` clears them)
- Severity: online-only
- Confidence: plausible. What decides it: whether `Game_Client_Init.Initalize`, `Game_Artifacts.Init` or
  `Game_Endscreen.Initalize` ever throw outside a rejoin (for example the end screen of a map that was rejoined earlier), and
  what a second `Game_Rounds.Startgameclient` does (`Game_Rounds.cs` not read).
- Tags: (none)
- What happens: a client hits one of those exceptions on some map, and no `Repair` follows on that map, so the flag stays
  set. On a later rejoin in the same game session (a restart clears the flags), the player object may not arrive late, so
  that map's own `Initalize` is still loading. `Repair` then sees the stale `initDied`, sets `Loaded` early and calls
  `Startgameclient`. When `Initalize` finishes, it calls `Startgameclient` a second time (`Game_Client_Init.cs:175-176`),
  which would start the client mode script twice (its music, among other things). Setting `Loaded` early also lets every
  client half go ahead before its groups are there. A stale `artifactsDied` or `endDied` sends `Init` or `Initalize` a second
  time to the new map's Cardinal row or end screen (`09_Rejoin.cs:338-347`).
- Why (engine evidence): see Where. The second call is the game's own, at `Game_Client_Init.cs:176`.
- Fix: clear the three flags when `Client()` sees a new map (`mv != clientMap`, `09_Rejoin.cs:256-260`), and act on them only
  in a late map. Low risk.
- PC check: in a client test build, throw once from a `Game_Endscreen`-stack frame on map 1, or search past rounds' logs for
  such exceptions. On map 2, drop and rejoin without restarting the game. The "started again ..." line should not list
  ", the end screen".

### Checked and fine (section 4)

- Quickening never writes `Time_Scale` 0, only 1.25, 1.5 or 1 (kit `19` §6). A mid-map quit puts `Time.timeScale` back to 1
  (`Tds_Pausemenu.cs:42`, `:274`), and `MapEnd` puts `Time_Scale` back at the end screen. Every `MapStart` resets
  `speedStep`, `Current`, `Flood` and `Nest` (`07_Weather.cs:205-209`).
- `Sessionid` reuse: the host gives `Count + 1` to each new GUID and clears the table only in `Leaveserver`
  (`Blm_Multiplayer.cs:406-453`). Returning from a map doesn't restart the host, so a seat's key is never handed to another
  player within a host session.
- `Restore` order: it sets `Activeplayer` to 1 before the stats. The game resets a player's stats and wallet only when
  `Activeplayer` changes to 0 (`Tds_Clientinfo.cs:66-78`), so a restored wallet and stats stick. The seat keeps its `Stats`
  object by reference, which survives the old clientinfo's destruction.
- The same player dropping twice: each return compares `seat.Info` with the new clientinfo (`09_Rejoin.cs:113`; Unity's `!=`
  works on a destroyed object). While the player is present, the seat copies again every 0.25 s (`:101`).
- A vessel that dies while its player is away respawns from the core's stored `Link_Spawn` with its owner kept
  (`Entity_Ai_Main_Core.cs` `Respawn`). It doesn't need the absent clientinfo.
- `TurnAway` skips the host, and remembers a turned-away session only while it is connected (`09_Rejoin.cs:129`, `:190`), so
  O5's second try is turned away again. `Door` sets `Server_Serveropen` itself, because `Changelobbystatus_ServerOnly` does
  nothing without a Steam lobby (`Blm_Multiplayer_Remote.cs:265-296`).
- `RWIdentity`:
  - At launch `Metadata` is empty, so the kept GUID goes back.
  - While joined, the live GUID is kept.
  - The device hash stops a copied save from cloning a GUID.
  - It writes the save only when something changed.
- `RWLoad` runs on the host only (`10_Online.cs:97`). It does nothing on the boat or in a running map (`:108-111`), skips the
  host's own entry (`:135`), and `sent` stops double kicks (`:142`). It waits for the host's own `Loaded` (`:128-130`).
- `RWDialog.Ask` when the dialog's scene unloads:
  - `Runprompt`'s loop yields on the dialog's `destroyCancellationToken` (`Bla_Ui_Confirmation.cs:207`).
  - `Up()` reads false on a destroyed instance, and `done(null)` is called.
  - `Unplace` skips destroyed rectangles.

  Timed or cancelled prompts stay pending until the scene ends: a small per-frame poll, not worth a finding.
- `RWMenu`: the clone lives in the main-menu scene, which is reloaded on every visit, and `seen`/`Button` reset with each new
  `Mainmenu_Main` (`12_Menu.cs:34-37`), so there is no duplicate and no stale button. A click before `seen` is set is ignored
  (`:133`).
- `RWIcons.Add` runs at boot, before any `Geticon` of the part (pitfall #20). `For()` falls back to a stock icon
  (`08_Icons.cs:94-99`).
- `StandIn` registers the deter once per launch, checking `HasDeter` first. Which `Block_No...` being it copies can differ
  between machines; that changes only the look, because the host decides the flooding.
- `DropDeadNests` removes exactly the dead nest's delegate (`Delegate.Remove`, `07_Weather.cs:317`), without reflection. It
  runs every host frame of a campaign map (`20_Vote.cs:108`).
- `Setintroposition` (`Game_Client_Init.cs:183-193`) only moves the intro camera's anchor. `Game_Load`'s own call at 70%,
  after an early `Startvisuals` (MODE-B3), therefore changes nothing on screen.

### Not yet reviewed (section 4)

I stopped at the coordinator's request after `Bla_Ui_Confirmation.cs`. Not done:

- `93_TwoPlayers.cs` (debug-only, to be checked for false passes): not read at all.
- Decompiled files I didn't read:
  - `Entity_Ai_Main_Flood.cs`, `Entity_Ai_Main_Chrono.cs`, `Entity_Ai_Tower_Halberdnest.cs`, `Game_Init.cs`.
  - `Mainmenu_Main.cs`: the idle menu, `Play`, the tutorial check.
  - `Blockage.Menu.Bl_Menus.cs`, `Blu1_Ui_Handler.cs`, `Blu2_Menu_Handler.cs`, `Blu3_Button.cs`: when `Active` and
    `Intransition` flip on close, whether the card's non-forced `Closemenu` can be dropped, and `Items` registration and
    `Addmenu` for the cloned button.
  - `Game_Rounds.cs`: what `Startgameclient` does when called twice (MODE-B6).
  - `Game_Artifacts.cs`, `Game_Endscreen.cs`: whether `Repair`'s re-run is safe when the first run didn't die.
  - `Helper_Entitytools.Spawn` with `Forced` on an occupied tile, and `Helper_Gridtools.Checkifplaceable`.
- Open questions in my files that those reads would settle:
  - `RWWeather.CanNest` (`07_Weather.cs:350-355`) and `SpawnNest` (`:358-361`) check only that `Tower_Halberdnest` is loaded,
    not `Enemy_Halberd`, which `Needs` lists (`:52`). `Begin` spawns after 60 s even if `Loaded(w)` is still false (`:226`).
    `RWObjectives.Post` gates the Raze the Nest objective on `CanNest` alone (`19_Objectives.cs:88`). If the nest grows a
    Halberd that isn't loaded, does that throw inside the Tide counter's write (#123)? Needs `Entity_Ai_Tower_Halberdnest.cs`.
  - `RWDialog.Number` (`11_Dialog.cs:48-80`) indexes `Digits[text[d] - '0']`, so a negative number throws
    `IndexOutOfRangeException` on its `-`. Its one caller, `18_Shop.cs:298` (the Fund Transfer amounts), was not checked for a
    negative amount.
  - Not traced:
    - two dialogs asked at once (`asks`/`owner` in `11_Dialog.cs`), or a click landing during a timed close;
    - a rejoiner arriving during a vote, a shop or a map change (the `RWVote`/`RWAsk` side);
    - whether the local `Loaded` can still read true from the boat when the next map's `Game_Client_Init` appears. If it
      can, MODE-B3's early `Startvisuals` would land before the map is even built.
  - The flood's stand-in on occupied tiles, and stand-in leftovers across maps: not traced (needs `Entity_Ai_Main_Flood.cs`).
  - `DropDeadNests`: could a Tide written in the frame or two after a nest dies still meet the dead hook (#123)? Not traced.
  - Loopback only (the two-player test rig): after a map whose door opened, `Door(false, "the map ended")` leaves
    `Server_Serveropen` false, and the stock reopen on the boat needs a Steam lobby, so later boat joins in the rig may be
    refused. Check whether `93_TwoPlayers.cs` makes up for it.

## 5. The shared kit and the foes part (`Shared/ModKit*`, `RogueWavesFoes`)

Read line by line at `5d9b09e`: `ModKitBoat`, `Combat`, `Counter`, `Directions`, `Focus`, `Journal`, `Pack`, `Tides`, `Towers` (shipping), `ModKitTest` and `ModKitPlay` (debug-only), the foes part's `00_RogueWavesFoesMod.cs`, `99_SelfTest.cs` and README. Kit 1.8's new `ModKitUtil` and `ModKitAssets` (PR #80) were reviewed where the vessels' helpers were, in each vessel's section.

### SHARED-1. A rewind of `Entry` inside a break fires `Break` a second time: every bonus break from Tide 29 on pays its Pure Aura twice
- Where: `TDS_Mod/ModSource/Shared/ModKitTides.cs:57-63`; the consumer: `TDS_Mod/ModSource/RogueWavesMode/Code/20_Vote.cs:57-80`
  (`RWVote.Break`: `PayTide` :65, `Interest` :66, `OpenShop` :74, `RWDifficulty.Bonus` :77), `21_Difficulty.cs:162-168` (`Entry -= 2`),
  `14_Run.cs:212-219` (`PayTide`)
- Severity: wrong-behaviour
- Confidence: confirmed (code + engine read); not seen in game
- Tags: (none)
- What happens: the party stays past Tide 15, and the map plays on past Tide 29, the table's last Tide. Tide 29 ends, and Mode_Normal
  moves `Entry` onto the last break. `ModKitTides` fires `Break(29)`, and `RWVote.Break` pays `PayTide(29)`, pays `Interest(29)`,
  opens the shop and calls `RWDifficulty.Bonus(29)`, which writes `Entry = Entry - 2` (the break before Tide 29). At the next
  frame's poll, `ModKitTides` sees `Entry` change again, onto a row that is also a break (`EntryIsBreak` true). It fires `Break(29)`
  a second time, with the same wave, because `Wave` doesn't move on a rewind. So every player gets `PayTide(29)` and
  `Interest(29)` twice, and `OpenShop(29)` runs again. `Bonus` itself does not rewind a second time, because its guard
  (`Entry < table.Count - 2`) then holds; that is what 96_Flow.cs:678 sees. The same happens at every later bonus break (Tide 30,
  31, ...). The pay is `PerBonusTide`, plus the weather's `PureAuraPerTide`, twice per bonus Tide. If the table's last row is not the
  break straight after Tide 29 (when `Bonus` is entered at `Entry == table.Count`), the guard still fails after the first rewind: it
  rewinds twice, `Break` fires three times, and `Entry` lands one Tide early (Tide 28's row).
- Why (engine evidence): Mode_Normal writes `Entry` only at the end of a row (`Mode_Normal.cs:481-482`, `entry.Value += 1`) and
  reads the row once, at the top of the loop (`Mode_Normal.cs:359-360`). A break's countdown loop (`Mode_Normal.cs:409-467`) never
  looks at `Entry`, so the mode's write stays put until the next poll. `ModKitTides.cs:57-63` fires `Break` on any change of
  `Entry` onto a break row, including a change made by its own listener. The flow test checks only `Entry` and the row size at that
  break (`96_Flow.cs:672-678`), and its comment at :674 notes that "the jump itself reads as a break". No check reads Pure Aura there
  (the only bonus pay check is Tide 16's, `96_Flow.cs:661`).
- Fix: in `RWVote.Break`, ignore a second break of the same `wave`: keep `lastBreakWave` and return early, before `Resolve`/`PayTide`
  (reset it in `MapStart`). This is low-risk: it is local to the mode and needs no kit change. The alternative in the kit is to fire
  `Break` only on a change from a non-break row (or none) onto a break. That is risky: it changes the event for every part, and the
  flow test's own `Entry` jump at 96_Flow.cs:673 relies on a jump reading as a break.
- PC check: in the flow test, or by hand past Tide 29, log `RWVote.Breaks` and one player's `RWRun.For(s).Pure` just before Tide 29
  ends and 2 s into its break. Healthy: `Breaks` +1 and Pure +`PerBonusTide` (+ weather). Bug: `Breaks` +2, and `Player.log` shows
  "Tide 29 paid N Pure Aura each" twice.
- Lead note: the same bug as MODE-D1 (section 1), found separately; one fix covers both. MODE-D1's fix (a repeat guard in `RWVote.Break`) leaves the kit unchanged.

### FOES-1. A vessel that dies inside a Dazzling aura comes back Illuminated for the rest of the map
- Where: `TDS_Mod/ModSource/RogueWavesFoes/Code/00_RogueWavesFoesMod.cs:336-361` (`DazzlingTick`), :298-309 (`Targets` takes `Hero`)
- Severity: wrong-behaviour
- Confidence: plausible. The mod's side is confirmed. The engine side rests on the brief's key fact "a vessel's death: modifiers
  lost, upgrades kept (copied into the new vessel's Preupgrades)", which I did not trace in the decompiled respawn code. I also did
  not read `Entity_Ai_Effect_Tower_Merchut` to confirm that its `Effect_Multi` acts on a vessel. Those two reads decide it.
- Tags: (none)
- What happens: a Dazzling foe walks within 3 tiles of a vessel. The host lays `RW_Effect_Illuminate_Upgradeobject` (Merchut, +20%
  taken) on it, adds it to `illuminated` (:348-352), and lays the timed Blind on it. The vessel dies there. The new vessel carries the
  dead one's upgrades, Illuminate among them. The new entity was never added to `illuminated`, so the `else if
  (illuminated.Contains(t))` branch (:354-358) never removes it. Walking into an aura again doesn't help either, since `!ContainsKey`
  (:348) is then false and the vessel is still not listed. The vessel takes +20% damage until the map ends. The dead entity's own
  entry is only swept (:360). A Blind copied the same way depends on whether Barrel_Boost's own timer runs on a respawned vessel
  (not read).
- Why (engine evidence): `Entity_Master.cs:300-303` (`Executestart`) applies every `Initupgradelistserver` entry with
  `Helper_Entitytools.Upgrade(..., Forced)`. A respawn's copied upgrades therefore arrive as real upgrades on the new entity. The
  mod tracks "Illuminated by me" in a list of entities instead of reading the upgrade.
- Fix: decide removal by the upgrade, not the list. Replace `else if (illuminated.Contains(t))` with `else if (t.Upgradelistserver !=
  null && t.Upgradelistserver.ContainsKey(Illuminate.Objectname))`. Only this part ever lays that upgrade, so this is low-risk.
- PC check: put a Dazzling Dagger beside your own vessel. Once `Upgradelistserver` holds `RW_Effect_Illuminate_Upgradeobject`, kill the
  vessel with `Interacthurt` for its Health. After the respawn, despawn the foe and wait 5 s. Read the new vessel's
  `Upgradelistserver` and land one `Damage 10` hit. Healthy: no Illuminate, 10 taken. Bug: Illuminate present, 12 taken.

### FOES-2. The Bouthouse is not skipped: Illuminate laid on it is forwarded to Boutman and never taken off him
- Where: `TDS_Mod/ModSource/RogueWavesFoes/Code/00_RogueWavesFoesMod.cs:298-309` (`Targets`: `Tower`, `Mob`, `Hero`, team != 0, no
  `Bouthouse` skip), :348-352 (Illuminate has no `Damage > 0` test, unlike Blind at :346)
- Severity: wrong-behaviour
- Confidence: plausible. It is decided by the Bouthouse carrying `Tower` or `Mob` (data, not in the decompile) and by pitfall #85's
  forwarding (seen in game for run upgrades).
- Tags: (none)
- What happens: a Dazzling foe passes within 3 tiles of a Bouthouse, and the Bouthouse gets Illuminate. Per #85, the Bouthouse
  forwards every upgrade it holds to Boutman (`Askforupgrades`). Boutman then carries Illuminate wherever Boutman stands, and is never in
  `illuminated`. When the foe leaves, only the Bouthouse's copy is removed (:354-358). Boutman takes +20% for the rest of the map
  (and keeps it through a death, FOES-1).
- Why (engine evidence): pitfall #85 (`09_PITFALLS.md`): "never give the Bouthouse a run upgrade; skip the tag `Bouthouse` in 'all
  buildings' effects". `Targets` reads `Entitiesserver` by tag with no such skip.
- Fix: skip entities whose `Taglistserver` contains `Bouthouse` in `Targets`, which is low-risk. FOES-1's fix then also cleans
  Boutman once Boutman is out of range.
- PC check: as Boutman, stand the Bouthouse 2 tiles from a Dazzling Dagger, away from Boutman. After 3 s, read Boutman's
  `Upgradelistserver`; then despawn the foe and read it again 5 s later. Bug: Illuminate on Boutman both times.

### SHARED-2. `ModKitTides` fires a spurious `Tide(0)` at every map's start
- Where: `TDS_Mod/ModSource/Shared/ModKitTides.cs:41-54`
- Severity: wrong-behaviour (low: the effects seen today are small)
- Confidence: confirmed (code + engine read) for the event. Its visible effect through a carried-over objective is plausible.
- Tags: (none)
- What happens: on the host, MapStart fires as soon as `Maploaded` is true and records `Wave = mv.Wave.Value`, which is still the
  NetworkVariable's initial -1. At least 100 ms later, `Mode_Normal.Run` writes `Wave = 0` and `Entry = 1`. The poll sees `0 > -1`
  and fires `Tide(0)`, then `Break(0)` in the same `Update`. The kit's contract (file header) is "a Tide starts; 1 at the game's TIDE 1".
  Today's listeners: Yossele logs "Tide 0: ..." and counts a Tide; Argo sets its `Tide` to 0; vessel buildings don't exist yet, so
  nothing grows, mends or gushes. `RWVote.Tide(0)` (`20_Vote.cs:82-91`) runs `RWShop.Leave`, `CloseQuestion`, `ClearBoard` and
  `RWObjectives.Begin(0)` during the fly-in, and `Break(0)` then calls `RWObjectives.Resolve()` at once. That is visible only if
  `RWObjectives.Active` survived the previous map: it is cleared by `Post`/`Resolve`/`Begin` only (`19_Objectives.cs:83,111,143,171`),
  not at MapStart or MapEnd. Then a Meteor or Nest target spawns during the fly-in, and "Hands" (`failed` reset by `Begin`) pays at
  once. That carry-over is the mode's to confirm.
- Why (engine evidence): `Game_Mapvars.cs:62` (`Wave` starts at -1) and `Game_Grid.cs:715` (`Maploaded = true` at the end of
  `Generatemap`). `Game_Init.cs:120-139` awaits `Generatemap`, then waits for every client in steps of at least 100 ms, then
  `Generatemode`, then `Startgame`. `Game_Rounds.cs:245-251` and `Mode_Normal.cs:310-312` show `Run` writing `Entry = 1, Wave = 0`.
- Fix: at MapStart, record `Wave = Mathf.Max(mv.Wave.Value, 0)` (`ModKitTides.cs:46`). This is low-risk: Tide 1 then still reads
  `1 > 0`, and a rewind's `wave < Wave` branch is unchanged. Kit change: bump the version and copy to `kit/Shared`.
- PC check: a solo map with Yossele. `Player.log` holds "Tide 0: map Tides ..." right after "GAME START". With the fix, the first
  such line is "Tide 1".

### FOES-3. A foe with both Aging and Berserk loses Berserk's damage growth
- Where: `TDS_Mod/ModSource/RogueWavesFoes/Code/00_RogueWavesFoesMod.cs:267-277` (both branches assign `f.Speed`/`f.Damage`),
  :283-296 (both ticks write `f.Damage`)
- Severity: wrong-behaviour
- Confidence: plausible. It is decided by whether one foe can hold two pack modifiers: `RWDifficulty.Pool` (`21_Difficulty.cs:140-159`)
  turns on up to `1 + (map - 3) / 2`. I did not read whether they go into one `Wavecontent.Upgrades` list or one per row.
- Tags: (none)
- What happens: on a foe with both modifiers, the Berserk branch overwrites `f.Speed` and `f.Damage`, which held Aging's mods. Each
  tick, `BerserkTick` writes `missing` into the shared Damage mod, then `AgingTick` overwrites it with `share`. Damage follows Aging
  alone, and Berserk's +100% at no health never shows. Aging's 0-valued Damage mod is orphaned, and its −30% Speed mod stays on,
  which is intended. `RWFLooks.Holds` (:411-421) also shows only the first modifier's ring.
- Why (engine evidence): each `Addmod` returns its own `Link_Modifier`, wired to `Editstat` by its own `Changed` delegate
  (`Helper_Entitytools.cs:624-647`). A second assignment to the field drops the first mod's handle, not its effect.
- Fix: give `Foe` separate handles (`AgingDamage`, `BerserkDamage`, `BerserkSpeed`). This is low-risk: the stats' Multi buckets sum.
- PC check: spawn a Dagger with both upgrades, hurt it to 50%, and after 1 s read `Damage` `Value_Calc` against a plain Dagger's.
  Expected base × (1 + share + 0.5); bug: base × (1 + share).

### Checked and fine (section 5)
- ModKitTides: "Leave" on a destroyed map (`ReferenceEquals` versus Unity's null) fires once and not after a `Return`/`Win`/`Lose`
  (`Gamecomplete` starts at "Running", `Game_Mapvars.cs:66`; its writers are `Mode_Normal.cs:259,302` and
  `Tds_Multiplayer_Remote.cs:99,167`). Clients only reset. The row is `Entry - 1`, matching `Mode_Normal.cs:359-360` (#121). It is
  polled, not hooked on `Wave.OnValueChanged`, so a throwing listener can't stop the Tide loop (#123), and each delegate has its own
  try (:79-97).
- Six kit copies: each `ModKitTides` copy polls and fires to its own listeners only, and no copy reads another's statics. The boat
  layout is computed identically from `Spots_Home` by every copy (no flicker), and `EnsureSpots`/`Arrange` are idempotent. The
  journal tab sync is the same sort in every copy.
- ModKitFocus: installed once through the registry mark. `Bl_Inputs.Actions` entries are created once and persist across scenes
  (`Blockage.Bla_Input.cs` `Bind`). Its handler in fact runs before the game's `Onfocus`, which `Game_Selection.Start` adds per map
  (`Game_Selection.cs:697-698`), not "after" as its comment says. The press still ends on the vessel: `Onfocus` rebuilds
  `Heroselect` when the old vessel is destroyed and takes index 0, where the kit moved the vessel (`Game_Selection.cs:610-660`).
- Direct `Link_Modifier` edits (`ModKitCounter.Set`, Berserk and Aging ticks): the setters fire `Changed`, which runs `Addmod`'s
  `Editstat` (`Link_Modifier.cs:73-105`, `Helper_Entitytools.cs:624-647`). The `Value_Calc` setter re-derives `Value` by the stat's
  type, keeping the fraction for Variable stats, and `Updateref` syncs it (`Link_Stats.cs:70-117`). This holds when the stat existed
  at `Addmod`.
- `ModKitCounter.Get` reads `Statsserver`, which is empty on clients, although its comment says "every client". It is a latent trap
  only: the mode's call sits in `RWRun.Tick`, which returns on clients (`14_Run.cs:232`).
- Foes `Targets` excludes team 0, which is Mode_Normal's enemy team (`Mode_Normal.cs:319-321`; players are set to team 1 at :331).
  So Cursed and Dazzling never touch foes.
- Cursed's held Damage mod is removed on leaving range and dropped for dead targets. A vessel's death drops modifiers, so nothing
  leaks.
- ModKitDirections.Grant checks `IsServer` and the same-name early return (`Upgradelistserver.ContainsKey`) before `Upgrade`.
- Debug-only code is fenced (`#if MODKIT_TEST`, `#if MODKIT_PLAY`, `#if ROGUEWAVESFOES_SELFTEST`), and nothing leaks into release.
  The foes self-test's `Spot` failure check compares a NaN result with `Vector3.zero` (`99_SelfTest.cs:118-119`): that is a false
  fail or throw, not a false pass.

### Not yet reviewed (section 5)
- ModKitDirections: every part's Direction id length against the 61-byte `FixedString64Bytes` of the synced `Upgradelist` (where
  `Has`/`Rank` read on clients; pitfall #13). Also the template fields `Make` does not reset, and `Helper_Entitytools.Upgrade`/
  `Removeupgrade` (`Helper_Entitytools.cs:464-590`) line by line against #115-#118 and #82-#83, including pruning of vessel brains.
- `Entity_Ai_Main_Enemycore`'s Spawn path for `spawndata.Upgrades` (`Mode_Normal.cs:120-142`): whether modifiers reach the foe through
  `Initupgradelistserver` (after stats, `Entity_Master.cs:300-303`) or earlier. That decides whether `RWFModifiers.Find` can `Addmod`
  before the foe's stats exist (#82: silently no Aging or Berserk).
- `Entity_Ai_Effect_Barrel_Boost`: does it remove its own upgrade after `Duration_Base`, so Blind re-applies, and how does it behave
  beside a stock Barrel_Boost (#116)? `Entity_Ai_Effect_Tower_Merchut` on towers and vessels, and its `Downgrade`. The respawn's
  upgrade copy (FOES-1).
- Whether the foes self-test's `Health` read (`Value_Calc`, the maximum) is a false pass for Aging depends on Health's `Value_Type`
  (data).
- ModKitCombat: `credit ?? from` (pitfall #6) with a destroyed credit leaves `Lasthit` at the previous hitter; its callers are not
  checked. ModKitPack `Notice`/`Show`: does `Helper_Uitools.Warning` show on the main menu? ModKitBoat `Unlock`'s `SendMessage("Start")`
  against `Macro_Characterselect_Click.Start` (proven in game since kit 1.0).
- ModKitJournal and ModKitTowers were read but not checked against the engine (proven in game since kit 1.2-1.3).
- Kit docs not read: `05_CODE_COOKBOOK.md`, `17_BASE_VESSELS.md` §2/§8/§11, `15_ROGUE_MODE_ENGINE.md`, `06_JOURNAL.md`,
  `13_ENEMIES_AND_MODIFIERS.md`, `24_ENEMY_BODIES_AND_BOSSES.md`, the repository's `docs/PACK.md` beyond §1-5.
- For the mode reviewer: `RWObjectives.Active` survives a map end (SHARED-2), and the `RWVote.Break` repeat guard (SHARED-1).

## 6. Yossele

Files `00`-`16` read in full at `5d9b09e`; of the debug files only parts of `99_SelfTest.cs`.

### YOS-1. A Grafted Sapling's fruit always looks like the ALL fruit: the spawn replaces its colour skin with the Sapling's
- Where: `01_Util.cs:48-60` at `63af4b0` (`SpawnOnTile`; was `:156-167`), reached from `11_Pachira.cs:193-195` (`NodeEmptied` picks the skin)
  and `11_Pachira.cs:324` (`YosseleFruit.Drop`).
- Severity: wrong-behaviour
- Confidence: confirmed (code + engine read)
- Tags: in-simplification-code (the reviewer expected PR #80 to move the helper; it stayed in Yossele, see the lead note).
- What happens:
  - A Grafted Sapling set to RED, BLUE or YELLOW drops a fruit that pays one colour at 70% of three. On every machine it
    draws as the three-colour ALL fruit.
  - `SpawnOnTile` writes the colour into `spawn.Skin`, then calls `Helper_Entitytools.Spawn(spawn, ..., parent, true)`.
    That call overwrites Skin with the parent's.
  - The parent is the Sapling. It is fully grown (`NodeEmptied` returns unless `FullGrown`), so its skin is
    `"Growth" + Max` (`07_Building.cs:131`).
  - The fruit's visuals have no such skin override, so `Editvis` falls back to Default.
  - The same overwrite gives the vine (`10_Henge.cs:297`) the Henge's Growth skin, and the Cordyfriend
    (`16_Friend.cs:57-61`, the same Spawn call) the cannon's. Neither matters: neither has Growth overrides, and `""`
    draws Default too.
  - The self-test's Grafted block (`99_SelfTest.cs:1240-1252`) never reads the dropped fruit's Skin. It reads only
    `sapling.Colour`, `FruitPay()` and the button's Goalstring. So PC round 9 (160/0) passed with the bug in place
    (test-only gap).
- Why (engine evidence):
  - `Helper_Entitytools.cs:66-75`: inside `if (Sameasparent && Parentbeing != null)`, the code sets
    `Newspawn.Skin = Parentbeing.Worldobject.Skin.Value` (the Team, Owner and Subowners overwrites sit beside it).
  - `Entity_Master_Client.cs:520-530`: `Skinoverrides[skin]` is used only when the key exists; otherwise the Default
    entry is used.
- Fix (low-risk):
  - In `SpawnOnTile`, after the spawn, add `if (made != null && made.Worldobject != null) YosseleUtil.Skin(made.Worldobject, skin ?? "");`.
    Skin is networked, and the buildings' Growth looks already change it at runtime.
  - Add a self-test line: a Grafted BLUE drop has `fruit.Skin.Value == "Blueaura"`.
  - Check whether the kit's docs/05 `SpawnOnTile` recipe has the same pattern.
- PC check: take a Grafted Sapling set to BLUE and let it mine a node out at full growth.
  - Expected: the fruit's `Skin.Value` reads `"Blueaura"` and the blue model is drawn.
  - With the bug: it reads `"Growth<max>"` and the three-colour model is drawn.
- Lead note: re-read at `63af4b0`. PR #80 did **not** move this helper: it is still Yossele's own `SpawnOnTile` (`Yossele/Code/01_Util.cs:48-60`), so only Yossele is hit. `Helper_Entitytools.Spawn` with `Sameasparent` copies the parent's Skin over the one the mod set (`Helper_Entitytools.cs:66-75`). Confirmed. The cookbook recipe the helper's comment names (`docs/05` `SpawnOnTile`) should be checked for the same pattern.

### YOS-2. Monument's Cleanse also strips stock trade-off modifiers, not only foes' debuffs
- Where: `12_Hooks.cs:119-145` (`Cleanse`). It is called on every foe hit on a Monument (`12_Hooks.cs:263-264`).
- Severity: wrong-behaviour
- Confidence: plausible. Stock data decides it: does any stock artifact or vessel effect put a negative Multi/Pmulti,
  or an Override, on a player's towers in a normal co-op game?
- Tags: #107. Not in PR80: Cleanse stays in `12_Hooks.cs`, and only `First` becomes `ModKitUtil.Front`.
- What happens:
  - Cleanse removes every modifier with `Value_Override_On > 0 || Value_Override != 0 || Value_Multi < 0 || Value_Pmulti < 0`,
    whatever its source.
  - Its comment rests on "Yossele's own buildings carry no negative modifiers of their own". In the first release,
    though, Yossele plays the normal game with stock artifacts.
  - `Entity_Ai_Effect_Artifact_Paper_Tower` adds four Multi mods (Health, Speed, Damage, Attackspeed) from its asset
    `Defaultparams`. On the first foe hit, any negative one (the trade-off half) is removed from a Monument building,
    which keeps only the upside.
  - The same happens to any other tower-wide trade-off, or to a positive Override from another player's vessel.
  - The artifact's own `Downgrade` later calls `Remove()` on a mod that is already gone.
- Why (engine evidence):
  - `Entity_Ai_Effect_Artifact_Paper_Tower.cs:15-18`: Multi mods built from Defaultparams, with the sign set by the
    asset.
  - `12_Hooks.cs:132`: the filter takes any negative Multi.
  - `01_Util.cs:170-174`: `Unmod` invokes the mod's `Remove`.
- Fix (risky, because it changes when the strip runs):
  - Strip only the modifiers the foe's hit added: snapshot `e.Modifiers` in the front-of-chain hook, and strip only the
    difference.
  - Or exempt modifiers that belong to an upgrade on the building's own upgrade list.
- PC check: in a normal co-op game, take Paper Tower (or another artifact with a tower trade-off) and build a
  Monument. Let one foe hit it. Compare its Health, Damage, Speed and Attackspeed, and its Modifiers list, before and
  after the hit.

### YOS-3. The Cordyfriend cap of 30 per owner is checked 0.1 s before the spawn, so a burst of deaths overshoots it
- Where: `16_Friend.cs:31-47` (`Raise`: the cap check, then `Later(0.1f)`) and `16_Friend.cs:49-71` (`Spawn`, no
  re-check).
- Severity: wrong-behaviour
- Confidence: confirmed (code read; `Later` is a delayed coroutine in `01_Util.cs`)
- Tags: none (not in PR80)
- What happens:
  - `CountOf(owner)` counts only friends that have already spawned.
  - A Viral Blast, or a crowd of spored foes falling within 0.1 s near the cap, passes the check once per death. The
    owner can end with up to 29 + N friends. Each extra one is a walking networked mob, and the cap exists to bound
    them.
- Why (engine evidence): `16_Friend.cs:35` (the check) against `16_Friend.cs:46` (the deferred spawn); `Spawn`
  re-checks only that the cannon is alive (`16_Friend.cs:51-52`).
- Fix (low-risk): repeat `if (CountOf(cannon.Owner) >= YosseleData.FriendCap) { Capped++; return; }` at the top of
  `Spawn`.
- PC check (self-test): hold an owner at 29 friends, call `Raise` 5 times in one frame and wait 0.2 s. `CountOf` should
  be 30; with the bug it is 34.

### YOS-4. The vines' slow look is the stock `Debuff_Slow`, which #114 says may not be loaded on every map
- Where: `02_Data.cs:50` (`VfxSlow = "Debuff_Slow"`), used on every vine tick at `10_Henge.cs:367`.
- Severity: wrong-behaviour (the look only; the slow itself still applies)
- Confidence: plausible. It is decided by whether the stock `Debuff_Slow` sits in a group that every Yossele match
  loads. #114 found `Debuff_Stun` loaded on 3B_Thehunt but not on 3B_Woozy.
- Tags: #114. Test-only part: `DebuffReport(foe, YosseleData.VfxSlow)` at `99_SelfTest.cs:995` proves the look on
  the self-test map only, as #114 itself says.
- What happens:
  - After #114, Yossele swapped the stock stun look for its own (`02_Data.cs:51-52`), but it kept the stock slow look.
  - If `Debuff_Slow` is not loaded, each vine slow logs "Could not retrieve Vfx/Debuff_Slow" and draws nothing.
  - It is the stock Smacky bomber's look (`Entity_Ai_Tower_Smackybomber.cs:221`). Yossele clones `Tower_Smackybomber`,
    so the look is likely loaded, but that is not proven. `ExtraGroups` holds only `Hero_Snosno` (`02_Data.cs:16`).
- Why (engine evidence): the kit's `09_PITFALLS.md` #114, and `Entity_Ai_Tower_Smackybomber.cs:221`.
- Fix (low-risk): give the vine slow its own look, as `Debuff_Yossele_Daze` was given to the stun, or add the group
  that holds `Debuff_Slow` to `ExtraGroups`.
- PC check: play a Yossele-only match on 3B_Woozy, vine a foe, and grep the log for
  "Could not retrieve Vfx/Debuff_Slow". The 2026-10-26 Woozy log may already settle this, if vines were used there.

### Checked and fine (section 6)
- Per-player state with two Yosseles:
  - `YosselePlayer` is keyed by owner (`05_Server.cs`).
  - Connected Roots shares damage only to the Sapling owner's buildings and never to another Sapling
    (`12_Hooks.cs:173-185`), so a hit can't bounce between two Saplings.
  - The friend cap counts per owner.
- Map-change statics: `05_Server.cs` resets every static, and re-hooks Onspawn and Ondespawn, whenever
  `Game_Entity.Instance` changes.
- Spawn order: brains attach from Onspawn, which runs inside `Game_Entity.Spawn` before `Entity_Master.Start` builds the
  stats. No brain reads stats at attach time.
- The static `pending` cannon in `16_Friend.cs:59-63,78` is set and cleared around the synchronous
  `Helper_Entitytools.Spawn`. Onspawn fires inside that call, so `Spawned` reads the right cannon.
- D-MDS (#105):
  - The host kills a shot with `Kill(false)`, which plays no explosion: explosions live in Despawn, which only
    `Kill(true)` reaches (`Projectile_Master.cs:271-334`).
  - A Vis makes each client put out its own unnetworked copy (`13_Client.cs` PutOut, clients only).
- Laser: the Attack's Goalstring names the laser clone of `Hero_Snosno_Snoball`. Client copies come through
  `Projectileclient` with the resolved Master (`Entity_Master_Radio.cs:161-197`).
- Ore payout (#106): Strike reads `Healthresource` before and after the hit, and `Paid()` follows the mine's order:
  Dodamage, Addressdamage, Reheal, Giveresources (`Entity_Ai_Resource_Mine.cs:93-128`).
- Monument strip timing (#107): it strips on the hit itself, without a timer, and the Bulwark's stun order allows this
  (`Entity_Ai_Enemy_Bulwark.cs:32-41`).
- `hit.From ?? guard.Master` (`12_Hooks.cs:283`) is a `??` on a Unity object. It goes wrong only for a destroyed
  attacker, which can't land a hit: Projectile_Ai_Base silently kills the shots of a shooter that has died.
- There is no `?.` in files 00-16. The other `??` uses (`04_Content.cs:457,808-811`, `14_Commands.cs:24-25`,
  `15_Journal.cs:39`) act on boot-time loads, which come back C#-null, not destroyed.
- Keybinds (#113): FERTILIZE `"6"`/`"7"` and COLOUR `"Quickaction"`/`"5"` are all in `Bl_Inputs.Actions` (Quickaction
  and 1-8); `Game_Selection.cs:293-304`.
- Commands from clients: `Command_ServerRpc` requires owner or subowner, Controllable and a Ref
  (`Entity_Master_Remote.cs:85`). The FERTILIZE picker sends `{"Ref":id}`.
- Death and respawn: the core copies `Upgradelistserver` into Preupgrades (`Entity_Ai_Main_Core.cs`, Respawn). `Begin`
  re-subscribes Command on the new hero, and the per-owner `YosselePlayer` survives.
- Client Vis: the `Client.Vis` delegate is never reset by the game, so the hook stays. Owner-only warnings use
  `OwnedLocally` (compared against `Localplayer.Sessionid`).
- Counters: `ModKitCounter` uses a Counter stat with a Base mod, and the Link_Stats setter clamps the value and syncs it
  with `Updateref`.
- Brain-disabling exceptions (#80): `YosseleFaults` stops a brain only after 600 failing frames in a row.
- Journal and icons at boot (#20): `AddIcons` runs before the group loads, and `CheckIcons` checks again after 8 s.
- Sound keys (#96): the hero's footstep key is copied from the template's Move.
- Growth and Tides: a building grows on Tides that begin after it is built (`BuiltWave`, host-only `ModKitTides`
  polling).
- `Visualproperties.Entry` is a struct, and `04_Content.cs` stores the edited copies back.
- The upgrade mirror in `07_Building.cs` (OnCommand "Upgrade", in-PR80) is replaced by stock
  `Entity_Ai_General_Upgrade` in PR80. Nothing found there survives the merge, so it was not hunted further.

### Not yet reviewed (section 6)
- `97_Measure.cs`, `97_Shots.cs` and `98_Play.cs` were not read. Nobody has checked them for false passes or for
  release leaks.
- `99_SelfTest.cs` was only grepped: the Grafted block at 1240-1252, the Growth-skin checks at 599-602 and the
  DebuffReport at 995. The rest is unread.
- Not checked: whether the debug files (97-99) are left out of the release build.
- Keybind collisions: `Game_Selection.Addcommand` binds a key only once per selection (`Game_Selection.cs:298`). Not
  checked: whether a stock command on the Sapling's or the hero's panel already holds `Quickaction`, `5`, `6` or `7`.
  That is asset data.
- The kit's docs/05 `SpawnOnTile` recipe (YOS-1). PR #80 left the helper in Yossele, and no other part has one; whether another part spawns a skinned child with `Sameasparent` was not checked.
- The callers of `YosseleServer.HeroOf`, and the boat case (`ModKitBoat` in `LateUpdate`).

## 7. Spool

Files `00`-`13` read line by line at `5d9b09e`, with `DESIGN.md`, `IMPLEMENTATION_NOTES.md` and the kit classes Spool calls. The debug files were not read.

### SPL-1. With Threaded Delivery and a full Yarn Stash, a Thread Head stays by the Stash instead of guarding its spot
- Where: `TDS_Mod/ModSource/Spool/Code/09_Head.cs:153-182`. Also `05_Server.cs:154-167` (`Deliver` returns 0 when
  `Room` is 0) and `05_Server.cs:17` (`Room`).
- Severity: wrong-behaviour
- Confidence: confirmed (the mod's own logic; the only engine fact used is that `State` reads "Move" while walking,
  `Helper_Entitytools.Updatecommand`, which sets `State = Link` for a Statechange command)
- Tags: survives PR #80 (the Head's `Tick` is not part of it)
- What happens: a Thread Head with Threaded Delivery fills to 20 and walks to the Stash, but the Stash is at its cap
  (50, easily reached once every Tin has its 4 Crafted alive). Next, every 0.25 s:
  1. `Deliver` puts 0.
  2. `Returning` is cleared, and the Head is sent to its spot (`:162-167`).
  3. On the next tick it is still within 1.5 tiles of the Stash, so `Carried >= Cap` starts the return again
     (`:169-170`).
  4. On the tick after that, step 2 sends it back toward its spot.

  The Head turns round every quarter second and never leaves the Stash. It doesn't guard its spot, doesn't collect out
  in the field, and keeps re-pathing. This lasts until a Tin or PATCH spends Thread. With several Heads, all of them
  bunch up at the base for as long as the Stash stays full.
- Why: `Deliver` (`05_Server.cs:159-161`): `put = Min(carried, p.Room); if (put <= 0) return 0;`. The auto-return test
  (`09_Head.cs:169`) only checks `Carried >= Cap && Player.Home != null` and never checks that the Stash has room.
- Fix: gate the auto-return on room: `... && Player.Home != null && Player.Room > 0`. Optionally warn "Yarn Stash is
  full" once. Low-risk (local, changes nothing when the Stash has room).
- PC check: self-test or by hand: Head with Delivery, Stash at 50 with no Tin able to spend, put 20 Thread on the
  ground at the Head's spot. After 10 s, the Head's distance to its `Spot` should be ≤ 0.9; today it stays about 1.5
  from the Stash. Logging the Head's `Returning` every tick shows it flipping true/false every 0.25 s.
- Lead note: re-read at `63af4b0` (`09_Head.cs` `Tick`): confirmed. PR #80 only renamed a helper call in this file.

### SPL-2. RETURN on a Thread Head that carries nothing parks it at the Stash indefinitely
- Where: `TDS_Mod/ModSource/Spool/Code/09_Head.cs:90-98` (RETURN accepted whatever it carries), `:120-126`
  (`StartReturn`), `:153` (the turn-round only runs when `Carried > 0`), `:171-182` (it keeps walking back to the Stash)
- Severity: wrong-behaviour
- Confidence: confirmed (the mod's own logic)
- Tags: survives PR #80
- What happens: the player presses RETURN on a Head with 0 Thread, e.g. to bring it home. The design says RETURN
  "sends it to the Stash to drop off and come back".
  1. The Head walks to the Stash.
  2. The drop-off and turn-round block is skipped, because `Carried > 0` is false.
  3. The `Returning` branch sends a fresh Move to the Stash whenever the Head is idle, and `return`s before `Guard()`.

  The Head stands at the Stash, not guarding, until Thread happens to come within its 3-tile reach (a drop near the
  base, or a Loom next to the Stash) or the player presses SPOT. Nothing tells the player why it doesn't come back.
- Why: `09_Head.cs:153` `if (Carried > 0 && SpoolServer.AtStash(...)) { ... if (Returning) { Returning = false; Walk(Spot) } }`
  is the only place `Returning` is cleared while the Stash stands.
- Fix: turn round on arrival whatever the Head carries. Make `:153` `if (SpoolServer.AtStash(Owner, pos, DropOff))`,
  with the `Deliver` call inside `if (Carried > 0)`. Or refuse RETURN with 0 carried (a warning). Low-risk.
- PC check: place a Thread Head far from any drops (Carried 0) and press RETURN. Ten seconds after it reaches the Stash
  it should be walking back to its spot; today `Returning` is still true and it stands within 1 tile of the Stash.
- Lead note: re-read at `63af4b0`: the turn-round sits inside `if (Carried > 0 && ...AtStash(...))`. Confirmed.

### SPL-3. TARGET is accepted unchecked while the Tin has no live Crafted, so its Crafted can be sent to a tile they never reach
- Where: `TDS_Mod/ModSource/Spool/Code/08_Tin.cs:107-129` (the standable/path check only runs with a live Crafted of
  this Tin; otherwise `"ok, unchecked"` returns the raw tile), `:87-91` (the spot and pin are set, "Spot set"),
  `:142` (the pin is spawned `Forced`), and `:261-270` (a Crafted that never arrives only re-issues its Move)
- Severity: wrong-behaviour
- Confidence: confirmed from code that no check runs. Plausible that the Crafted can't reach such a tile. That is
  decided by `Helper_Navtools.Getpath` to an occupied or cut-off tile, which I didn't open.
- Tags: survives PR #80
- What happens: TARGET is used while the Tin has no Crafted alive. That happens in the first second after placing it
  (the first stitch comes 1 s after `Begin`), or whenever its Crafted are dead and the Stash is short, which is common
  early (10 starting Thread is one Stitched).
  1. The player clicks a tile within 8 tiles that a Crafted can't stand on or reach: under one of their own towers, or
     ground cut off by walls or cliffs.
  2. "Spot set" shows, and the pin is placed there by a `Forced` spawn, which skips the placement check
     (`Game_Entity.cs:118-121`).
  3. Each new Crafted runs toward the spot. `Entity_Ai_Mob_Movement` paths to the nearest Surface within 3 of the goal
     (`Entity_Ai_Mob_Movement.cs:132-140`). On an invalid path it goes `Idleblocked`/`Idle` (`:249-260`).
  4. `Arrived` never becomes true, so `Guard()` never runs. The brain re-sends a Move every 0.4 s. The Crafted stand
     where their path failed and only fight what walks into their own range.
- Why: `Reachable` validates with "a live Crafted of this Tin, when there is one". With none, nothing is validated, and
  the spot is never re-checked when the first Crafted appears.
- Fix: when `why == "ok, unchecked"`, re-run `Reachable` with the first Crafted spawned after it, in `Tick` after
  `SpawnUnit`. If that fails, fall back to the Tin's own tile and warn `WarnUnreachable`. Or refuse a tile whose
  `Beings` hold a `Tower`. Low-risk.
- PC check: place a Tin with the Stash empty (no Crafted alive) and TARGET the tile of one of your own towers within 8
  tiles. Expect "Spot set" and the pin on that tower's tile. Refill the Stash: the new Stitched's `Arrived` stays false
  and its `Orders` count climbs every 0.4 s. It doesn't move to a foe 2 tiles from its spot.

### SPL-4. Any one exception stops a Spool brain for the rest of that entity's life (pitfall #80)
- Where: `TDS_Mod/ModSource/Spool/Code/06_HeroBrain.cs:48-52`, `07_Building.cs:48-52` (Stash, Tin, Head, Cushion,
  Loom), `08_Tin.cs:203-207` (every Crafted, Seam, Mangled)
- Severity: wrong-behaviour (silent)
- Confidence: plausible. The pattern is confirmed: `catch { Warn; enabled = false; }` on the first throw. I found no
  concrete throw in the shipping ticks; one candidate (not proven) is below.
- Tags: a known pitfall pattern (#80; Argo 2.0's `ArgoFaults` is the fix). Survives PR #80 as described, because the
  catch is in the brains' `Update`, not in the upgrade mirror. Check whether the PR's rewrite of `SpoolBuilding`
  touches `Update`.
- What happens: one throw and the brain is off for good:
  - Hero brain off:
    - Spool stops picking up and delivering Thread.
    - The Directions ranks stop refreshing.
    - The build menu stops switching, so a Stash destroyed later can't be rebuilt that life.
    - The PATCH icon stays grey after the next use, because only `Update` calls `SetPatchButton(false)` (`:34-35`).
  - Tin brain off: the Tin never stitches again.
  - Cushion brain off: its hit hook is no longer moved back to the front, so after the next upgrade or effect there
    are no reflects, no Thread spun and no Mangled.
  - Unit brain off: the unit stands still.

  The only sign is one `[Spool] ... brain:` line in Player.log.
- Why: the kit's own pitfall #80 records this as a bug met in game. A candidate trigger, not proven: `MoveTo` →
  `Updatecommand` → `Entity_Ai_Mob_Movement.Action` → `Checkifstandablenearest(Goalobject.Beingserver.Currentblock,
  ...)` (`Entity_Ai_Mob_Movement.cs:144`), if the targeted foe's `Currentblock` is null that frame.
- Fix: skip the failing tick, log once per kind, and disable only after ~600 consecutive throws (`ArgoFaults`).
  Low-risk.
- PC check: grep every run's Player.log for `[Spool] ` lines ending in `brain:`. Each one is a brain that stopped. To
  prove the effect, a debug build can throw once in `SpoolTinBrain.Tick`: the Tin never stitches again.
- Lead note: Yossele (`YosseleFaults`) and Argo (`ArgoFaults`) already skip a failing tick and stop only after about 600 in a row. One kit class for all four vessels would fix SPL-4 and ULO-2 together.

### SPL-5. A friend who joins or rejoins mid-map never sees Thread already lying on the ground
- Where: `TDS_Mod/ModSource/Spool/Code/11_Client.cs:86-93` (tufts exist only from `Spool_Tuft` Vis events),
  `05_Server.cs:190-219` (each drop is sent once)
- Severity: online-only
- Confidence: confirmed (code). Engine: `Vis_ClientRpc` is RPC-only state that late joiners miss (kit `19` §5).
- Tags: survives PR #80
- What happens: drops last 15 s and merges refresh them. A client that joins or rejoins in that window (or whose
  hook on the carrying entity isn't made yet; the scan runs every 0.25 s, `:45-59`) has no tuft for it. Spool or a
  Head still picks it up on the host, so Thread appears from nothing on that screen. A `Spool_Take` for an unknown id
  only flies a tuft from the spot. Small, but player-visible.
- Fix: catch-up per `19` §6 rule 5. Answer `Pingload` on a Spool entity by re-sending each live drop's `Spool_Tuft` to
  that client, with the drop's remaining life rather than a fresh 15 s. Risky (online path).
- PC check: two-player loopback (`Run-TwoPlayers.ps1`). Kill foes so tufts lie on the ground, then have the friend
  rejoin within 10 s. The friend sees no tufts where the host does; Spool walking there raises the `Carried` counter
  on both machines.

### SPL-6. A second Yarn Stash can be placed in the half second before the menu changes, leaving an orphan Stash
- Where: `TDS_Mod/ModSource/Spool/Code/06_HeroBrain.cs:36-41, 79-102` (the menu is rebuilt every 0.5 s),
  `07_Building.cs:199-212` (`Home` is set only when the new Stash's brain starts) and `:216-237`
- Severity: wrong-behaviour (edge)
- Confidence: plausible. Decided by whether the build picker or a client can send a second placement within ~0.5 s
  (`Entity_Command_Build` not read). The server side accepts it: `Entity_Ai_Hero_Buildsell.cs:64-80` builds any being
  that matches a `Buildoption` `Goalstring`, and the Stash costs nothing.
- Tags: survives PR #80
- What happens: two Stashes stand, and the newest is `Home`. The older one is an orphan: no drop-off, no comebacks,
  no counter. If the newest falls while the older stands:
  1. `Home` goes null and the Thread spills.
  2. The menu goes back to Stash-only.
  3. Tins warn "Place your Yarn Stash first" while a Stash visibly stands.
- Fix: count a Stash as home from `OnSpawn`: set `Player.Home` there, or have `CheckMenu` treat any Stash brain in
  `Buildings` as home. Or despawn a second Stash in `OnSpawn`. Low-risk.
- PC check: in the self-test, send two Stash build JSONs 0.1 s apart. Expect the log line "has a second Yarn Stash";
  destroy the newer one and read `Menu()` (Stash only) while the older Stash still stands.

### Checked and fine (section 7)
- Client commands: PATCH, TARGET, SPOT, RETURN and MEND reach the brains only from the owner or a sub-owner, for a
  `Controllable` command with a command object (`Entity_Master_Remote.cs:85`). Spool's commands meet both conditions.
- PATCH:
  - The heal can't exceed max: the `Link_Stats.Value` setter clamps to `Value_Calc` (`Link_Stats.cs:52-60`).
  - The heal data carries the `Damage` and `Damagemulti` that `Doheal` reads (`Helper_Damagetools.cs:82-83`).
  - Owner, team, range, cooldown and cost are all checked server-side, and the cost is spent only when the heal runs.
- Keybinds are all in `Bl_Inputs.Actions` (`Quickaction`, `3`-`7`). A key shared with another command is skipped, not
  thrown on (`Game_Selection.cs:298-301`).
- Selling walking units:
  - The stock AIs subscribe to `Command` in `Awake` (`Entity_Ai_Base.cs` Awake), so the stock Sell clears and matches
    first (`Entity_Ai_Hero_Buildsell.cs:109-121`) and the fallback fills `Goalobject` before `Action`.
  - Crafted refund only through `Gone` (empty `Costs`).
  - A Head refunds Costs × the hero's Sellrate × Sellvalue, plus its upgrades × `Upgradesellvalue` (default 1,
    `Link_Spawn.cs:32-34`; `Buildsell.cs:197-209`).
- Despawn order: `Despawn` is idempotent (`Game_Entity.cs:325`). Children with `Destroyonparent` (Seams, Mangled,
  pins) go before `Ondespawn` (`Entity_Master` Executestart `Removeself`; `Game_Entity.cs:332-338`), so `Tin.Gone`'s
  second despawn of its pin is a no-op.
- Spawn-time hand-off (#40): `PendingHome` and `PendingHead` are set and cleared around the synchronous `Onspawn`
  (`Game_Entity.cs:271-274`). `Onspawn` runs before `Movebeing` (`:278`), and `SwapTokens` waits for `Currentblock`.
- Unit counts: units leave `SpoolServer.Units` in `Ondespawn`, and `UnitsOf` counts by home. A sold or destroyed Tin's
  Crafted live on without being counted anywhere.
- Statics: players, drops, tokens, comebacks and debuff looks reset when `Game_Entity.Instance` changes. The
  `ModKitCounter` dictionary is swept.
- Thread:
  - The Stash and carried Thread are per owner.
  - A failed stitch refunds.
  - Spool keeps its carried Thread at death; a Head drops what it carried.
  - A sold Crafted's overflow drops on the ground.
  - With two Spool players, drops are shared by design, and MostHurt, MEND and the sell fallback filter by owner.
- Pin Cushion:
  - `Interacthurt` fills `Damage` before the `Oninteract` chain, so the front hook sees it.
  - Slow Unravel copies the data before editing it (#30).
  - The `own` guard stops the Cushion counting its own pricks and delayed damage.
  - `Unravel` clears `owed` only after it has finished iterating it.
- Leash: `Entity_Ai_Mob_Movement` paths to the target's tile at query time and re-queries on a new `Commandtime`
  (`:129-150, 215-228`). It doesn't chase a moving goal, so the leash checks hold.
- Dart: `Entity_Ai_Mob_Ranged` fires only when the Attack `Goalstring` names a projectile (`:30`), which `Arm` sets.
- Upgrades: Spool's own copy of the upgrade flow matches `Entity_Ai_General_Upgrade.cs:39-87` check for check, so
  there's no defect there (and PR #80 replaces it anyway). An upgrade raises `Level` (`Helper_Entitytools.cs:541-543`),
  so the `+N/level` Health stats grow as designed.
- Directions are Priority −3 with empty `Costs` (`ModKitDirections.Make`), and their ranks are read from the
  vessel's `Upgradelistserver` on the host.

### Not yet reviewed (section 7)
- Debug-only files, not read (so no false-pass check was done): `97_Measure.cs`, `97_Shots.cs`, `98_Play.cs`,
  `99_SelfTest.cs`. Worth checking there: whether the RETURN and Threaded Delivery checks only use a Head that carries
  Thread with room in the Stash, which would hide SPL-1 and SPL-2.
- Engine reads I didn't get to:
  - `Helper_Navtools.Getpath` and `Helper_Gridtools.Checkifstandable` on an occupied or cut-off tile (decides SPL-3).
  - `Entity_Command_Build` repeat placement (SPL-6).
  - `Helper_Entityquerytools.Entityarea` and `Helper_Entityai.Checkenemymelee`/`Checkenemyranged` (possible SPL-4
    triggers).
  - `Entity_Ai_Tower_Fiekeep` and `Entity_Ai_Tower_Smackyhut` (stock unit lifecycle, for comparison).
  - `Helper_Resources`.
  - How foes reaching the Basin are removed (whether Thread drops there).
- Areas I didn't examine closely:
  - What happens when Spool dies mid-PATCH, or with the mode's rejoin.
  - Hotkey precedence between RETURN/MEND (`4`/`3`) and the Head's `Upgradeoption3`/`4`.
  - Whether a heal both cancelling Slow Unravel's owed damage and still healing is what the design wants.
  - The Shared kit beyond the classes named at the top (`ModKitPack`, `ModKitFocus`, `ModKitBoat`, `ModKitJournal`).

## 8. Argo

Read at `5d9b09e`; every cited line looked up again at `63af4b0`. Where PR #80 moved the code, the old place is given as "was". The debug files were not read.

### ARG-1. Two Anchor Line stuns on one foe can make its attack speed negative, so it attacks every tick
- Where: `TDS_Mod/ModSource/Argo/Code/01_Util.cs:178-190` (Pin; the stun is line 188, was 405-406). Callers:
  `07_TowerBrain.cs:455` (Anchor Line hit, `Root_Base=1;Pinstun_Base=1`, `02_Data.cs:293`), `02_Data.cs:164`
  (`WaterPinMulti = 2`). The same recipe is in the kit: `TDS_Vessel_Creator/docs/05_CODE_COOKBOOK.md:201`.
- Severity: wrong-behaviour
- Confidence: plausible. It depends on whether enemy `Attackspeed` has Limits (see PC check). The stacking and the
  engine's maths are confirmed from code.
- Tags: in-PR80 (the bug survived: Pin stayed in Argo's `01_Util.cs`), kit-recipe
- What happens: a foe stands beside a building, which is where the stun applies. One Anchor Line harpoon pins and
  stuns it for 1 s, or 2 s if it is wet. A second Anchor harpoon hits it inside that time. This is easy to get: two
  Harpoon Guns with Pierce 3, or one gun sped up by a tune and Full Crew against a wet foe. Each Pin adds its own
  `Attackspeed` Multi of -0.95, so the multiplier becomes 1 - 1.9 = -0.9. If the stat has no Limits, the foe's
  attack speed turns negative, and its "stun" then lets it hit the building on every check of its attack loop.
  Separately, a foe whose attack speed is already buffed (+Multi from a modifier) is only slowed, not stunned:
  1 + 0.5 - 0.95 = 0.55.
- Why (engine evidence): Multi mods add their deltas (`Helper_Entitytools.cs:641`), and the value is
  `(Base*Pmulti+Add)*Multi` (`Helper_Entitytools.cs:646`). That value is clamped only to `Value_Bounds`
  (`Link_Stats.cs:78`), and a stat without Limits gets bounds of -infinity to +infinity (`Entity_Master.cs:359-362`).
  The mob AIs time attacks as `Time.fixedTime - Lastattack >= 1f / Attackspeed` (`Entity_Ai_Mob_Melee.cs:63`, wait
  loop `:76`; `Entity_Ai_Mob_Ranged.cs:61`). When the value is negative the test is always true and the wait loop
  exits at once. Pitfall #79 found exactly this for `Speed`, which is why `ModKitUtil.Speed` raises a bound below 0.
  `Attackspeed` has no such guard. The game's own stun (Bulwark) uses Override 0.01 instead (`TDS_Vessel_Creator/docs/13_ENEMIES_AND_MODIFIERS.md:26`).
- Fix: low-risk: before the Addmod, raise `Attackspeed.Value_Bounds.x` to 0 if it is below 0, the same way
  `ModKitUtil.Speed` does for Speed. A value of 0 means 1/0 = infinity, so the foe never attacks. Better, but
  risky because it changes the proven stun: stun with an Override 0.01 mod, as the Bulwark does. Add and remove
  each one and never edit it (#81). This also stuns a buffed foe. Correct the cookbook line either way.
- PC check: with a foe beside a tower, log `Statsserver["Attackspeed"].Value_Bounds` for a few enemy kinds. If x is
  -infinity, the bug is real. Then stun one foe twice within 1 s (call `ArgoUtil.Pin(.., true)` twice), log
  `Value_Multi`/`Value` (expect -0.9 × base), and count the building's Health loss per second against an
  unstunned foe. The self-test's stun check (`99_SelfTest.cs:1699`) uses a single Pin and cannot catch this.
- Lead note: the recipe is the cookbook's (`docs/05` line 201), so a fix there reaches every future stun.

### ARG-2. The game's slow look blinks on foes in Deep Water and in the Tremor
- Where: `TDS_Mod/ModSource/Argo/Code/11_Water.cs:178-184`, `07_TowerBrain.cs:686-691`, and `01_Util.cs:316-328`
  (Slow → `ModKitUtil.Debuff(.., "Debuff_Slow", seconds)`)
- Severity: wrong-behaviour (cosmetic)
- Confidence: plausible. The send and stop timings are confirmed from code. Whether the gap shows depends on how long
  the `Debuff_Slow` particles last after `OnStop`.
- Tags: none
- What happens: Deep Water ticks every 0.25 s. It sends the slow look only when 1.5 s have passed since the last send,
  and each send lasts the slow's own 0.5 s (`WaterTick + 0.25`). So the look shows for 0.5 s and is then off for about
  1.25 s, all the time the foe stands in water. The Tremor does the same: a 0.75 s look about every 2 s. Argo's own
  `Argo_Wet` look on the same throttle is sent for 1.6 s (`11_Water.cs:181`), so it stays on.
- Why (engine evidence): `Vfx_Debuff.FixedUpdate` counts `Duration` down and calls `Removeonmod` at 0
  (`Vfx_Debuff.cs:63-67`). That sends `OnStop` to the look's effects and destroys it after `Afterduration`
  (`Vfx_Debuff.cs:30-42`). A later send makes a new look (`Helper_Vfxtools.cs:112-125`).
- Fix: low-risk: on the throttled sends, send the look for the throttle window plus one tick (water 2.0 s, Tremor
  2.25 s). Keep the Speed mod's own seconds as they are. The look then outlasts the slow by at most that window after
  the foe leaves, as `Argo_Wet` already does.
- PC check: watch a foe walk through Deep Water and through a Tremor. The blue slow look should stay on the whole
  time, not flash once every 1.5-2 s.

### ARG-3. Another source's shorter slow look cuts Argo's look short, and Argo doesn't send it again
- Where: `TDS_Mod/ModSource/Shared/ModKitUtil.cs:234-256` (the `shown` record and its `until >= end - 0.1f` skip;
  was `Argo/Code/01_Util.cs:556-576`)
- Severity: wrong-behaviour (cosmetic)
- Confidence: confirmed (code and engine read). The effect is small: the pin keeps its own `Argo_Pin` look.
- Tags: in-PR80 (the bug survived the move and now applies to every part that uses `ModKitUtil.Debuff`)
- What happens: Argo pins a wet foe and sends `Debuff_Slow` for 2 s. Within that time a stock tower (for example the
  Smacky bomber, `Entity_Ai_Tower_Smackybomber.cs:221`) or another part sends `Debuff_Slow` with a shorter time. The
  look's `Duration` is replaced by the shorter one. Argo's record still says its look runs to the old end, so later
  Slow and Root sends are skipped. The look disappears while the foe is still held or slowed.
- Why (engine evidence): `Helper_Vfxtools.Debuff` sets `Duration` on an existing look and does not take the larger
  value (`Helper_Vfxtools.cs:129`).
- Fix: low-risk: let the skip cover only the last ~0.5 s (send again once the recorded end is less than 0.5 s away).
  That costs one send per look every ~0.5 s at most. Or accept it as cosmetic.
- PC check: pin a foe in Deep Water next to a stock slowing tower and watch whether the slow look ends before the
  rope/star look does.

### ARG-4. The AIM picker's "Too far" reach and Tremor ring go stale while the Drill Rig stays selected, and after a Calibrate tune
- Where: `TDS_Mod/ModSource/Argo/Code/16_AimSelect.cs:50-56` (Reach from `Link.Goalstring`), `:76` (ring from
  `Link.Goalvalue`), `:86-87` (the label). Also `07_TowerBrain.cs:770-776` (SyncAim, called only at `:122`, `:683`
  radius change, `:760` Aim, `:872` Fathom) and `08_NautBrain.cs:160-162` / `:192` (a Calibrate tune adds and removes
  Range on the drill without calling SyncAim).
- Severity: wrong-behaviour (UI only; the server's check in `07_TowerBrain.cs:744-749` is correct)
- Confidence: confirmed (code and engine read)
- Tags: none
- What happens: (a) The player selects the Drill Rig, keeps it selected while it sinks a Fathom or is upgraded, then
  opens AIM. The ring and reach shown are the ones from when it was selected. (b) A Naut tunes the drill with
  Calibrate (+Range). AIM still says "Too far" on tiles the server would accept. When the tune ends, the ghost shows
  on tiles the server then refuses with warning 10. Clicking still sends the command (`Send` is not gated), so only
  the prompt misleads.
- Why (engine evidence): the selection copies the command's `Link` struct once, when the panel is built
  (`Game_Selection.cs:288`), so later `Updatecommand` changes don't reach this picker. The reach in `Goalstring` is
  rewritten only by SyncAim, and no Range change calls it.
- Fix: low-risk: in `Running`/`Reach`, read the drill's current synced AIM entry (the last `Commandlist` entry named
  `AimLink`, as `ArgoClient.Aimed` does) instead of `Link`. On the server, call SyncAim when a tune starts or stops
  on a drill. One Updatecommand per tune.
- PC check: tune a Drill Rig with Calibrate, open AIM, point between the old and new reach. Today it says "Too far".
  After the fix it says "Shake the ground here" and the server accepts.

### ARG-5. The sound mixer is searched once, so if it isn't found the first time, Argo's sounds miss it for the rest of the session
- Where: `TDS_Mod/ModSource/Shared/ModKitAssets.cs:256-272` (`mixerSearched = true` is set before the
  `Bla_Audio.Instance == null` and `audio.Mixer == null` returns; was `Argo/Code/03_Assets.cs:359-377`)
- Severity: wrong-behaviour (sound)
- Confidence: plausible. It happens only if the first mod sound plays before `Bla_Audio` and its Mixer exist, which
  is unlikely because sounds play in a map, long after the menu.
- Tags: in-PR80 (the bug survived the move and now applies to every part's sounds)
- What happens: if the first lookup finds no audio manager, `null` is cached for good. Every later mod sound plays
  with no mixer group, so it ignores the game's volume settings until the game restarts.
- Why (engine evidence): code read of the mod only. The engine side is just "Instance may be null before the audio
  manager starts".
- Fix: low-risk: set `mixerSearched = true` only once a group was found, or when `Bla_Audio.Instance` was non-null
  and its Database was read.
- PC check: none needed unless sounds ignore the volume slider. Then log `Mixer()` at the first play.

### ARG-6. `ArgoClient.rest` keeps every recoiling turret Transform for the whole session
- Where: `TDS_Mod/ModSource/Argo/Code/10_Client.cs:364-371`
- Severity: leak (negligible)
- Confidence: confirmed (code read: nothing removes or clears entries)
- Tags: none
- What happens: each turret that ever recoils on this machine leaves one entry, including destroyed ones, which stay
  as fake-null keys. Over a long campaign that is a few hundred small entries.
- Why (engine evidence): none needed. It is a plain dictionary keyed by Unity objects.
- Fix: low-risk: clear it in the per-map reset, or drop keys whose Transform is null when the dictionary passes 64
  entries (the pattern `slowVfx` already uses).
- PC check: none.

### Checked and fine (section 8)

- `Clientsendstack++` in Debuff and MustSee does not build up: the engine drains it every FixedUpdate
  (`Game_Mapvars.cs:166-182`, line 180).
- AIM, crew orders and FLOOD from clients: `Command_ServerRpc` checks Owner/Subowner, that the command exists, Ref and
  Controllable before the brain runs (`Entity_Master_Remote.cs:85`).
- FLOOD: the json field order matches Builddata; the water's owner pays; pool sell refund via Sellvalue.
- Pools outlive a sold pump as designed: they have no Destroyonparent (`Game_Entity.cs:209-222`).
- Hits: a fresh ShotData dict per shot, Projectile_Master copies Data per hit (`Projectile_Master.cs:256-266`),
  OnHit copies `hit.Data` before editing (#29/#30), and the target reads the same `Link_Interact.Data`.
- `??` is used only on non-Unity types or on brains just checked.
- Command and Upgrade subscriptions survive upgrades. The Interact hook is set again after Updateinteract, which
  resets only Interact and Oninteract.
- Addmod's timed removal and an early Remove can't both remove the mod (Removal plus the Dead guard).
- AIM sync: SyncAim keeps one synced entry (Updatecommand updates the first, so a duplicate would go stale), and
  clients read the last one (#72).
- The Pending fields are set around the synchronous Onspawn (#40). The Dive and crew-gone paths are fine.
  Buildoption5 is created again and removed at Begin.
- Statics are reset per map on a new `Game_Entity.Instance`; ArgoFaults; cleave's recursion guard; Lasthit via
  ModKitCombat.
- The reel's speed boost is removed or expires on time; the reel picks the nearest hit (#40b).
- Root at -1 Multi and Slow cut at `Value_Multi - 0.1` keep stacked holds and slows at the Speed floor
  (#79; enemy Speed Limits 0.05..99999).
- CrewOrder treats a target that vanished in flight as a recall (warning 600+i). That is a mild UX choice, not a bug.
- `ArgoWater.OnTide` reuses `Net` objects from before the Rebuild. This is harmless at `Spread_Per_Tide=1`. Check it
  again if the 2026-11-04 review's "2 pools per Tide" is built, because the inner loop would then use a stale Net.

### Not yet reviewed (section 8)

- `Argo/Code/97_Shot.cs` and `99_SelfTest.cs` (the hunt for false passes): not started.
- The PR #80 versions of Argo's files as merged in `63af4b0`. I reviewed `5d9b09e` and only spot-checked the lines
  above in the new tree. Not checked: the switch to stock `Entity_Ai_General_Upgrade` for the building brains, the
  `ModKitUtil.Front` hit chain, Bulkhead's one-tuner-per-tower change, and whether removing `Stagger_Base`,
  `LastGusher`, `Argo_Bleed` and `AftershockEvery` left anything reading them.
- Kit internals that Argo calls (ModKitTides, Counter, Directions, Focus, Pack, Towers, Boat): only their call
  sites were checked.
- Engine paths not read: `Entity_Ai_Mob_Movement` and `Helper_Navtools.Getpath` (does a reeled foe re-path),
  `Helper_Gridtools.Checkifplaceable` (FLOOD placement), `Entity_Master_Client.Vis` on the host.
- Data not checked: enemy `Attackspeed` Limits (this decides ARG-1).
- Late join and rejoin of looks sent only by Vis (Argo_Pin, Argo_Wet, the Tremor ring), and two Argos on one team
  sharing a water network (ownership, who pays).
- No compile run. Kit docs 05, 17 §2 and 12 were read only where a finding needed them.

## 9. Uloe

Files `00`-`13` read line by line at `5d9b09e`, with `DESIGN.md`, `IMPLEMENTATION_NOTES.md`, the README and `Shared/ModKitDirections.cs`. Of the debug files only parts of `99_SelfTest.cs`.

### ULO-1. PLUG IN sticks on UNPLUG, and the bulb stays lit, once the Power Bank Uloe is plugged into is sold or destroyed
- Where: `TDS_Mod/ModSource/Uloe/Code/06_HeroBrain.cs:277-283` (`CheckCord`: `if (Plug == null) return;`),
  `06_HeroBrain.cs:255-259` (`Unplug`: `if (Plug == null) return;`), `06_HeroBrain.cs:166-176` (the UNPLUG command lands
  in `Unplug`), `12_Commands.cs:173-181` (the client sends `{"Unplug":1}` on every press while Goalstring is "On").
- Severity: wrong-behaviour
- Confidence: confirmed (code + engine read)
- Tags: none (not in PR #80's code; survives it)
- What happens: Uloe plugs into one of its Power Banks. The Bank is then sold, or foes destroy it. From the next frame
  the hero brain's `Plug` field holds a destroyed object. `CheckCord` (every 0.2 s) and `Unplug` both start with
  `Plug == null`, which is true for a destroyed object (Unity's overloaded `==`), so they return before
  `UloeUtil.Alive(Plug)` is ever tested. As a result:
  1. the vessel keeps the "Plugged" skin, so its bulb burns on every screen;
  2. the PLUG IN command keeps Goalstring "On", so the button reads UNPLUG;
  3. every press of the button or of hotkey 6/7 sends `{"Unplug":1}` (the client's `Activate` reads "On"), and the
     server's `Unplug` returns on the same null test: nothing happens, and no "Unplugged" line shows;
  4. so PLUG IN, the vessel's one ability, can't be used again until the vessel dies (a new entity, a new brain) or the
     map ends.
  Light quietly drops back to the plain 1 within 3 tiles, because `Feed`'s `Plug != null` treats the dead object as null.
  The cord and ring stay drawn on clients for up to `LinkLife` (12 s). The cord is cut correctly only when the 0.2 s check
  happens to land in the one frame between the despawn and the destroy, about 1 time in 12 at 60 fps. A one-shot node
  (`Resource_Death`, which dies when mined out) would trigger the same thing if it carries `Healthresource`, which is
  what `UloeUtil.IsNode` asks for. That is not checked (see "Not yet reviewed").
- Why (engine evidence): `Game_Entity.Despawn` sets `Item.Dead = true` (Game_Entity.cs:329), runs the handlers
  (:334, :338) and ends with `NetworkObject.Despawn(true)` (Game_Entity.cs:407), which destroys the GameObject at the end
  of that frame. After that, the C# reference compares equal to null (pitfall #6, #63). The self-test never sells or
  loses a Bank that is plugged in: it unplugs (`99_SelfTest.cs:862`) before the sell section (`:1179-1191`), so 148/0
  can't see this.
- Fix: test the reference itself in both places: `if ((object)Plug == null) return;` in `CheckCord` and `Unplug`. Let
  `Alive(Plug)` (already Unity-null aware) decide the snap. For the effects' position in `Unplug`, cache the plug's
  position at `PlugIn` and on each live `CheckCord`. (Reading `Entityposition.Value` on the destroyed object is only a field
  read and also works.) Low-risk: local, and nothing changes while the target lives.
- PC check: plug into your own Power Bank, then sell it. Within 1 s the button should read PLUG IN, the vessel's
  `Skin.Value` should be "", and the PLUG IN command's Goalstring should not be "On". Today UNPLUG stays, pressing it does
  nothing, and the bulb stays lit. Repeat with a Bank that foes destroy. As a self-test step: plug into the Bank, send
  Sell, wait 1 s, then check `ReferenceEquals(hero.Plug, null)` and `hm.Skin.Value == ""`.
- Lead note: re-read at `63af4b0` (`06_HeroBrain.cs:255-283`) against `Game_Entity.Despawn` (`Dead = true`, the handlers, then `NetworkObject.Despawn(true)`): confirmed.

### ULO-2. One exception switches a Uloe brain off for the rest of its life (pitfall #80's pattern, still here)
- Where: `TDS_Mod/ModSource/Uloe/Code/06_HeroBrain.cs:62-66`, `07_Building.cs:59-63`, `08_Bank.cs:128-132` (each
  `catch { Warn; enabled = false; }`).
- Severity: wrong-behaviour (latent)
- Confidence: plausible. Deciding it needs a throw that can reach one of these catches in normal play.
- Tags: none (the catches are in the brains' `Update`, not in PR #80's moved or replaced code; survives it)
- What happens: a single exception in a tick turns the brain off until that entity is gone:
  - hero brain: no more Light from Uloe, no cord checks, the Directions are never read again, and Flash Bulb's hit
    hook is lost at the next `Updateinteract`;
  - a building: no Light skin, and nothing it does (the Glass stops beaming, the Mirror's hook is lost at its next
    upgrade, the Flood Light stops mining, the Bank stops making Photons);
  - a Photon: it stands still, gives no Light, and stops marking foes.
  One concrete path into a catch: the Bank's `Spawn()` (`08_Bank.cs:59`) calls `Helper_Entitytools.Spawn`, which calls
  `Game_Entity.Spawn`, which calls `Onspawn(component2)` (Game_Entity.cs:273) synchronously. That one multicast delegate
  holds every pack part's spawn hook (Argo, Spool, Yossele `05_Server.cs`, RogueWavesMode `16_Directions.cs:301`, Uloe)
  and the stock Cardinal hooks (`Entity_Ai_Artifact_Teamapply`/`Towerapply`/`Mobapply`/...). If any one of them throws
  for the new Photon, the throw unwinds into the Bank's `Tick`, and that Bank never makes another Photon. Uloe's own hook
  is wrapped. I stopped before checking the others.
- Why (engine evidence): pitfall #80 ("seen in game in Argo 1.x"). Multicast delegates stop at the first throwing
  handler and rethrow to the caller. Argo 2.0's `ArgoFaults` is the kit's fix.
- Fix: port `ArgoFaults`. Skip the failing tick and log the first stack; stop only after about 600 failing frames in a
  row. Low-risk.
- PC check: in a debug build, make one tick of each brain throw once (a static flag the self-test sets), then check that
  the brain still acts a second later (the Bank still refills, the Glass still beams). In long multi-part runs, grep
  Player.log for `[Uloe] ... brain:`, `photon brain:` and `hero brain:` warnings.
- Lead note: see SPL-4 (section 7): the same pattern, and the same kit-wide fix.

### ULO-3. With two Uloe players, a Flood Light's default aim, Long Throw, Wide Flood, Bounce Card and Photon visits feed the teammate's buildings; the design says "your buildings"
- Where: `TDS_Mod/ModSource/Uloe/Code/05_Server.cs:138-156` (`Shine` filters by team, :145),
  `05_Server.cs:159-175` (`NearestBuilding` by team), `07_Building.cs:324-331` (`CurrentAim` falls back to
  `NearestBuilding(Team, ...)`), `07_Building.cs:364-366` (the Long Throw line by team), `07_Building.cs:377` (the host
  accepts an AIM at any same-team Uloe building, while the client's picker allows only the owner's,
  `12_Commands.cs:142-148`), `10_Mirror.cs:48` (Bounce Card through `Shine`), `08_Bank.cs:239` (Photon visits by team).
- Severity: wrong-behaviour (only with two Uloe players on a team)
- Confidence: confirmed (code against `DESIGN.md:146` "picks one of your buildings ... the nearest one", `:185`
  Bounce Card "to your buildings", `:186` Wide Flood "to your buildings", `:167` visits to "your buildings"). Whether team-wide
  sharing is meant is Dimitri's call. The comment at `05_Server.cs:136-137` says Uloe's own light reaches any Uloe's
  buildings on the team, which may have been meant for the vessel's light only.
- Tags: none (survives PR #80)
- What happens: two players on Uloe build near each other. Player A's new Flood Light, with nothing picked, aims at
  the nearest Uloe building on the team, which may be B's. A's own Glass then stays unlit until A opens AIM and picks
  it, and the aim falls back to B's building whenever A's pick dies. A's Wide Flood, Long Throw and Bounce Card shares
  land on B's buildings too, and A's Photons wander off to visit B's towers (but follow only A's Flood Light lines,
  `PathNear(Owner, ...)`). Each panel shows Light its owner didn't place. With one Uloe player nothing changes.
- Why (engine evidence): mod-only logic. `UloeServer.Buildings` holds every player's Uloe buildings
  (`05_Server.cs:209-219`), and every filter above compares `Team`, never `Owner`.
- Fix: pass the owner into `NearestBuilding`, the Long Throw loop, the Photon's visit list, the host's AIM check and
  the buildings' calls to `Shine` (an owner argument, with −1 meaning team for the vessel's own light), and compare
  `b.Owner`. Low-risk in code, but it changes how two Uloe players share Light, so confirm the intent first.
- PC check: `Run-TwoPlayers.ps1` with both players on Uloe. The host builds a Flood Light on a node beside the client's
  Glass, with none of its own buildings in reach. Read the host Flood Light's `Aimed` (today: the client's Glass) and the
  client Glass's `Light` stat (today: 2 or more, from the host's Flood Light).

### Checked and fine (section 9)

- Light sum: each source keeps one `Link_Modifier` per tower and removes it before re-adding (`05_Server.cs:20-29`).
  The engine's `Removestat` and `Removal` are guarded by `Dead`, so the timer's later call does nothing
  (Helper_Entitytools.cs:591-687). No stacking (#119). A tower out of reach fades in 1.5 s. A sold or destroyed tower's
  entry is swept by Unity-null (`05_Server.cs:32-40`; a Dictionary keyed by a destroyed Object still finds it by
  instance id).
- #82: the `Light` stat is added in the building's `Begin`, once `Statsserver` is filled (Entity_Master.cs `Start`),
  and in the same call that sets `Started`, which every `Shine` and `Feed` checks. `Addstat` needs `IsSpawned`, which is
  true by then.
- `UloeUtil.Keep` (the Glass's Damage Multi, edited in place): `Value_Multi`'s setter fires `Changed`, then `Editstat`,
  and the stat is recomputed (Link_Modifier.cs:73-88). Leveladd's own mod coexists (Entity_Master.cs:390-399).
- Map change: `UloeServer`, `UloeMarks`, the look throttle and `UloeClient` all reset when `Game_Entity.Instance`
  changes. Every per-entity dictionary is pruned (marks when expired or dead, `shown` capped at 256 and pruned) or dies
  with its brain.
- Vessel death: it carries no modifiers. Directions come back as Preupgrades and are re-read every 0.5 s through the
  synced `Upgradelist`. The new entity gets a new brain from `OnSpawn`. Client cords keyed by the old id are dropped
  when their `From` dies.
- Commands: `Command_ServerRpc` lets only the owner or a subowner run a Controllable command with a resolved `Ref`
  (Entity_Master_Remote.cs:85). PLUG IN checks on the host: the Bank's owner, `IsNode`, reach plus slack, and the
  cooldown. A stale id falls back to the nearest valid target in reach. AIM checks team and reach. The upgrade flow
  matches the stock `Entity_Ai_General_Upgrade` line for line (`in-PR80`, which replaces it anyway).
- Blind and Illuminate (own route): one held Blind mod per foe, replaced, never stacked. The Illuminate hook works on a
  copy of `Data`, adds a missing `Damagemulti` (Dodamage reads it unguarded, Helper_Damagetools.cs:16), and is re-fronted
  10 times a second after `Updateinteract`. Looks go through `Vfxdebuff_ClientRpc` with the stack counted.
  `UloeUtil.Front` and `Debuff` keep their behaviour when they move (`in-PR80`, no bug found).
- Mirror: Glare nullifies on a copy. Bounce Card never feeds another Bounce Card or itself, so it can't loop (only
  Bounce Mirrors re-emit received Light). A dead shooter (Unity-null `From`) is skipped.
- Glass: the target is re-checked each shot. The beam's `Projdata.Data` is null, so the engine gives each shot a fresh
  dictionary (Helper_Damagetools.cs:296-303). Burning Point resets on a new target.
- Flood Light pay: the node pays through its own stock `Giveresources(Resources, -1, Split)`
  (Entity_Ai_Resource_Mine.cs:125), the same as every stock miner, with `Resourcegainmultiplier` applied there. The Power
  Bank has no income of its own. One text slip, out of scope as text: the lore at `02_Data.cs:229` and the comment at
  `07_Building.cs:207-208` say the owner keeps the pay, but it is split among all players.
- Photons: `PendingHome` is set and cleared around the synchronous `Onspawn` (#40). They go with their Bank
  (`Destroyonparent`; on a Bank's death its Photons die first, Game_Entity.cs:334, so Short Circuit bursts them, which reads
  as intended). They inherit the Bank's `Lit` skin through `Spawn` (Helper_Entitytools.cs:75), which is harmless: the
  client's `Editvis` checks `ContainsKey` (Entity_Master_Client.cs:524-528).
- `?.` and `??`: none on a possibly destroyed Unity object (each `??` operand is freshly made or C# null).
- Self-test: `99_SelfTest.cs:555` (node Light from aura) compares the formula with itself, and `:558` (reach refusal)
  passes when the node is already in reach. Both are weak checks, but the code they cover reads correct, so neither is a
  false pass in the brief's sense.

### Not yet reviewed (section 9)

- `Code/97_Shots.cs`: not read.
- `Code/99_SelfTest.cs`: only lines 546-560 and 1170-1254, plus greps for plug, sell and despawn. The rest is unread for
  false passes and release leaks.
- To decide ULO-2: whether any `Game_Entity.Onspawn` subscriber can throw on a Photon spawn. Candidates: Argo, Spool and
  Yossele `05_Server.cs`, RogueWavesMode `16_Directions.cs:301`, and the stock `Entity_Ai_Artifact_*` spawn hooks.
- For ULO-1's second trigger: whether a one-shot `Resource_Death` node (crops, `Resource_Crop_*`) carries `Healthresource`
  and the `Resource` tag.
- Kit `05_CODE_COOKBOOK.md`, `17_BASE_VESSELS.md` §2/§2.6/§8/§11, `19_NETWORKING.md` and `15_ROGUE_MODE_ENGINE.md` §14.7
  were not read in full. Only the engine paths above were checked against code.
- Not checked: whether the client's `Vfxdebuff_ClientRpc` replaces or stacks a running look of the same name when
  a Blind changes level (cosmetic either way). `Helper_Resources.Giveresources`'s multiplier path was not reread (a stock
  path, untouched by Uloe).
- Nothing was compiled; no hypothesis needed it.
