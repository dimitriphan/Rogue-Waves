# Chart round 1 on the PC (2026-11-06)

Dimitri: "Go chart round" (06:59Z). The checks of `docs/reports/CHART_BUILD_2026-11-06.md` §4 on his PC, handoff
`Chart_round1` (`0be84ef`): code `5e03648`, the chart build (PR #148, base `beab230`) plus the main-menu wobble fix,
which was merged afterwards as PR #150. The base this report merges into holds the same mode code, so the round ran on
what the base ships, plus one test fix (§4). Kit 1.14.

## 1. In short

- **Self-test, the mode's whole flow: 178 pass, 1 fail, 0 setup** (`20261106_022104`). The one fail is CV10's first line,
  read before the boat had written its blacklist (§4); the boat then sailed to the Cove and rested as designed.
- **Two players: 167 pass, 0 fail** across the six launches.
- The chart view shows at the staging vote, and the mini chart and banner on the boat; pictures for his eye in
  `Desktop\RogueWave\chart_round1` (§3).
- **For his eye:** the in-map "WHERE NEXT?" after a won map was captured with no chart view beside it and the dialog's
  title over its cards (§3, pictures 021312 and 021315); on the boat the game's own "Sea Chart" title and player name
  overlap the mini chart's header (020654).

## 2. Self-tests

| Run | Result |
| --- | --- |
| Mode flow, `Run-SelfTests.ps1 -Vessels RogueWavesMode -Also Argo,Yossele,Spool,Uloe,RogueWavesFoes -TimeoutMinutes 30` | pass 178, fail 1, setup 0; errors logged 310 (4 `OperationCanceledException` lines, seen as INFO) |
| Two players, `ROGUEWAVESMODE_SELFTEST,ROGUEWAVESMODE_TWOPC`, `Run-TwoPlayers.ps1` (1463 s) | host 63/0, second 69/0, stranger 7/0, rejoin 20/0, continue 8/0, no pack 0/0 |

The chart lines, in the order the flow logs them:

| Check | Result | Line |
| --- | --- | --- |
| CV1 | PASS | 37 tiles for 37 islands, 59 lanes for 59 links |
| CV3 | PASS | 11 faded, 0 wrong, choices A0 B1 |
| CV9 | PASS | all 500 charts |
| CV2 | PASS | view 'Vote', letters A0,B1,C2,C3, lit 4 |
| CV4 | PASS | the island's click reached the host as the card's would (votes 0, RW_Node_1_0 with 1) |
| CV5 | PASS | the view closes, no canvas left |
| CV10 (blacklist) | **FAIL** | `RW_Node_4_0`: read right after the warp; the next line logs "36 nodes blacklisted" |
| CV10 (rest) | PASS | rested at RW_Node_4_0, route 0000, retries 3, choice 1, gift retry |
| CV11 | PASS | Continue at the Cove: retries 3, route 0000, choice 1 |
| CV10 (sail on) | PASS | warped onto `RW_Chart_20261009S_0000r3`, picked RW_Node_5_1 |
| CV12 | PASS | offered Cardinal_Punch, Artifact_Slippers, Cardinal_Spyglass; given Cardinal_Punch |
| CV12 (row) | INFO | RW_Node_2_0 plays row 3's Tide 15 row 20 (its own row's 18) |

The wobble fix's P1 and P2 passed in the same run. On the second player, CV1, CV3 and CV9 pass in every copy, and
"C7 the chart one row on, on the second player's machine" passes. The second player's logs were not searched for
"chart view", "banner", "SAILING" or "Cove" (the PC session ran out of usage); they are in
`handoff\twoplayer_logs\*_20261106_022416*`.

## 3. Pictures (on the PC only: `Desktop\RogueWave\chart_round1`, 2560x1440)

| File | Shows |
| --- | --- |
| `chart_020735` | The staging vote: the whole SEA CHART on the left half (rows 1-10, Breakers coral, Cove and Wreck icons, A/B/C letters, YOU ARE HERE, legend, "24 s") beside WHERE NEXT? with 4 cards |
| `chart_020650`, `020654`, `020751`, `021448`, `021649`, `021812`, `021902`, `021953`, `022043`, `022056` | The boat: the mini chart top right ("M ▪ the whole chart") and a SAILING TO banner. In `020654` the game's "Sea Chart" title and the player name overlap the mini chart's header |
| `chart_022049` | "SAILING TO THE COVE", "Row 4 of 10 ▪ No map: the party rests", the mini chart with the path sailed |
| `chart_021312`, `021315` | The in-map WHERE NEXT? after a won map, with no chart view; in `021315` the dialog sits right, its title over the cards. The captures are 3 s apart and may catch the dialog's slide-in: his look decides whether this is a fault |

## 4. Fixed after the round

- CV10's blacklist line now waits up to 10 s for the boat to list the Cove before it checks (`90_Stops.cs`). Compiled
  here (release 0 errors, self-test 0 errors); not run again on the PC.

## 5. For Dimitri

1. Look at `chart_021312` and `chart_021315`: is the chart view missing beside the after-win vote, and are the title
   and cards overlapping once the dialog has settled? If so, that is the next fix.
2. `chart_020654`: the mini chart under the game's own top-right title. Moving the mini chart down a line is a small change.
3. By hand (not in this round): click a lit island with the mouse beside the dialog; press M on the boat, in a break,
   and while typing in chat (`CHART_BUILD_2026-11-06.md` §4 items 2 and 7).

## 6. Not run

`CHART_BUILD_2026-11-06.md` §4 items 2 and 7 (by hand), item 9 (a hitch while the pointer crosses islands: nobody
moved a mouse), and the second player's banner lines (logs kept, not read).

## 7. The PC as left

TDS closed. `TDS\Mods` hash-identical to before the round, all six parts enabled; `RogueWavesModeMod` back from
`handoff\parked\release_before_Chart_round1_20261106` (.info `5E0C6D53163190AD`, assets `58CA2202E5DCDFA8`). His save
copied back by hand from `save_backup_20261106_020532` (6 of 6 files match). The save after the round is in
`handoff\Chart_round1\save_after_round`, the TWOPC test build in `handoff\Chart_round1\test_build_left`; the apply's
backup in `handoff\backup_Chart_round1_20261106_020410`. The Unity project holds the handoff's code (`5e03648`, the
mode) with the TWOPC build in its Assets; `pc_manifest.txt` rewritten (1130 files).
