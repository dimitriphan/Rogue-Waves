# Mode polish: RogueWavesMode against the scope (2026-11-03)

Dimitri's "Run 3, 4, and 5" (2026-11-03), item 4: check the mode part (`TDS_Mod/ModSource/RogueWavesMode`) line by line
against `docs/scope/` 00-16 and `docs/mode/`, and fix what differs. **Issue** = behaviour or text that differs from the
scope, a bug, or a dead pointer; style is not an issue. **Cap:** two passes. Where a scope line is a judgment call, the
current behaviour is kept and listed here (§3).

**Result:** 15 issues found, all fixed on this PR (§1), as version **0.1.0-dev**. Pass 1 found the code issues (F1-F13);
pass 2 re-read every fix against scope `04` and the docs, and found no new code issue, only doc lines that still named
the old keys, version or state (F14, F15). **Compiled here, not run in game** (§5). No PC round was asked for.

## 1. Fixed

| # | Scope line | What differed | Fix (file) | Self-test |
| --- | --- | --- | --- | --- |
| F1 | `04` [Retries] Per campaign 3; Losing a map | No retries: a lost map left the route as it was, and the party could pick any linked map again | 3 retries; a lost map spends one and only that map can be picked until it is won; with none left the campaign ends (`06_Campaign.cs` `Lost`, `Open`; `05_Boat.cs` blacklist) | T1, T5 |
| F2 | `04` [Retries] The host quitting | The host leaving a running map cost nothing | The map in play is saved while it runs (`RW_Campaign_Playing`); a map still in play when the host is back on the boat (`OnBoat`), or when Continue reads the save, is lost | P1, T2, T3 |
| F3 | `04` [Tides and Leaving] Map won; [Retries] Dying in a bonus tide | A map counted only when it ended as a win (Mode_Normal's 30 Tides); dying at Tide 20 lost it | Won when Tide 16 starts (`RWCampaign.Tide`); a later loss costs nothing (`MapEnd` ignores a map no longer in play) | T4, C5 |
| F4 | `04` [Campaign] Won, Lost | After the tenth map the campaign stayed in the save and the boat stood on a finished chart | Ten maps won complete the campaign; won or lost, it leaves the save; with no campaign on, the party is warped back to the stock world (`05_Boat.cs` `Home`) | T6, H1 |
| F5 | `04` [Tides and Leaving] Breaks: 60 seconds | Mode_Normal's 30 s breaks (and one 10 s) | Every break after Tide 1 is 60 s; Mode_Normal's own table is untouched (`02_Mode.cs`) | K0 |
| F6 | kit `docs/23` §2 rule 3 (`QUALITY_PASSES_2026-10-15.md` A3-9; mistake check 2026-10-28) | Save keys `Campaign_Seed`, `Stock_World`, `Player_Guid`... had no `RW_` | All keys are `RWKeys` with `RW_`; `RWKeys.Migrate` renames old ones once per save | K1, I1 |
| F7 | `docs/PACK.md` §4 (mistake check 2026-10-28) | Version 0.3.0, its own count | `0.1.0-dev`, the pack version plus `-dev`, in code and prefab builder | compile |
| F8 | `04` [Sea Chart] Links: 1 to 3 (C2) | A row's last map could take 4 links: 1,399 of 20,000 charts | Capped at 3 in `03_Chart.cs` and `concept-tools/sea_chart.py`; 0 of 20,000 now | CH |
| F9 | `04` [Sea Weather] In-game lines (B4-11) | Quickening "the tides run faster later on", Nesting "a halberd nest grows by the enemy" | The scope's lines (`07_Weather.cs` `Short`) | by eye |
| F10 | `docs/mode/REJOIN.md` O5 (B4-14) | A turned-away player got in, seatless, on a second Join Game | Forgotten once they leave (`09_Rejoin.cs`); O5 now asks for the second try | online O5 |
| F11 | `03` No Way Back In (B4-12) | The "Lost the map" line named the launch's first host | The host of this map only | online O2 |
| F12 | B4-13 | The dev export's description left out the rejoin | Names the retries and the rejoin | build |
| F13 | kit bug B1 / B2-1 (`QUALITY_CHECK_2026-10-15.md`) | A late "Leave" (fired when the next map wakes) cleared the next map's node: no route step, no weather | `MapEnd` acts only on the map in play, which the boat or Continue already counted; kit 1.7 is no longer needed for this | T2, T4 |
| F14 | dead or stale pointers | `docs/mode/SEA_CHART.md` §7 rows 2, 8 and the guard's keys; kit `docs/23` §8 item 3's key; `docs/mode/MAIN_MENU_START.md` on how an ended campaign says so; mode README, notes, `CLAUDE.md` and `TDS_Mod/README.md` rows at 0.3.0 | Updated | - |
| F15 | `03` No Way Back In Status | Named the part's old version | Version left out; state unchanged | - |

## 2. Readings chosen in the fixes (for Dimitri to confirm)

1. **"Tide 15 is cleared"** = Tide 16 starts, after the 60 s break. Dying in that break with foes still up loses the map.
2. **The host quitting after Tide 15** counts as won, not lost: the map was already won, and until the vote to leave
   exists the game's own menu is the only way to leave a won map (and map 10 is "won by leaving it").
3. **The opening break** keeps Mode_Normal's 45 s ("Best of Luck"); the scope's 60 s is "between tides". Mode_Normal's
   10 s break before Tide 19 ("What is that????") becomes 60 s like the others.
4. **"Replays that map from the last save"** = the boat offers only the lost map, from Tide 1. During a replay the stock
   arrows still point at the other linked maps: they can be hovered and sailed to, not picked.
5. **3 retries** = three lost maps are replayed; the fourth loss ends the campaign.
6. **New chat lines** (English, in the pack's voice, yours to reword): "Map lost. The team replays it from the boat: 2
   retries left.", "Map lost with no retries left: the campaign is over.", "Tide 15 cleared: this map is won. The Tides
   from here are bonus Tides.", "All 10 maps sailed: the campaign is won."
7. **Taken home:** when a campaign ends, the party is warped back to the stock world it sailed from, on the boat.
8. **A "Win" end before Tide 15** also wins the map (today only the self-test makes one; later the vote).
9. **Old keys are migrated** though A3-9 said no campaign was saved outside the self-test: `Player_Guid` may be on a PC
   that ran 0.3.0, and the rename costs one read per key at launch.

## 3. Judgment calls: current behaviour kept

| Scope line | Kept | Why |
| --- | --- | --- |
| `04` [Tides and Leaving] Bonus tides: no limit, each one harder | Mode_Normal's Tides 16-30 are the bonus Tides; Tide 30 runs until the map ends | Harder bonus Tides past 30 come with the vote to leave and Phase 4's scaling |
| `04` [Tides and Leaving] Leaving by vote, only between Tides | The host can still leave through the game's menu at any time (before Tide 15 it costs a retry) | The vote is a later piece (`docs/mode/SEA_CHART.md` §4, V1) |
| `04` [Campaign] Score: does not count | Nothing hides or changes the score; the `RW_` twins write their medals and records under their own `RW_` names, so stock records are untouched | Hiding it is UI work (kit `docs/20`) |
| `04` [Retries] A friend who drops while the map loads comes back on the boat | `RWLoad` sends them to the main menu at 75 s with "They can join again on the boat" | The game ends a failed load on the main menu; joining the boat is one click from there |
| kit `docs/23` §2.2 `RW_Schema` | No schema key; the rename goes by which keys exist | §2.2 is an untested recipe; one key set needs no version yet |

## 4. Not built yet (later pieces, not issues)

| Scope | Piece | Where it is planned |
| --- | --- | --- |
| `04` [Campaign] Start, Continue | The main-menu Rogue Waves button that calls `RWCampaign.New` / `Continue` | `docs/mode/MAIN_MENU_START.md` |
| `04` [Campaign] Vessels locked in; Continue gives back vessel, Pure Aura, Directions, Cardinals | Per-player campaign state | kit `docs/23` §2.3, Phase 5 |
| `04` [Campaign] Changes per campaign; [Difficulty] | Modifiers and enemies per campaign, per-map scaling | Phase 4; `! NEEDS !` in `04` |
| `04` [Depth] | On hold | scope `04` |
| `04` [Sea Chart] Vote; every map voted on; map 1's staging vote | The in-map vote; the leader steers with the arrows meanwhile | `docs/mode/SEA_CHART.md` §4 |
| `04` [Sea Chart] On the boat: no Cardinal swaps | Cardinals | `12`, Phase 5 |
| `04` [Sea Weather] Pays 1 extra; `06` [Pure Aura] 3 per tide, 2 per bonus tide | The Pure Aura counter (the weather's 1 is `RWWeather.PureAuraPerTide`) | `06`, Phase 5 |
| `06` [Directions] Ranks II, III return; kept every map | Directions | `11`, Phase 5 |
| `06` [Fund Transfer], [Shopkeeper] | VENDCINT, base-aura transfers | `06`, Phase 5 |

Matched the scope already: the chart's rows, stretches, spread, no repeats and the custom slot left out; the weather's
calm rows, odds, never-twice rule and numbers; the host's save and Continue; rejoining after a drop or a restart;
friends joining on the boat. No mode doc names 15 maps or the Swell (the word is now only a palette colour and Argo's
water part; Riptide's stretches are prior art).

## 5. Checks run here

- `check_mod.py RogueWavesMode/Code Shared RogueWavesMode ""` and `"ROGUEWAVESMODE_SELFTEST"`: 0 errors;
  `check_editor.sh ../Assets/RogueWavesModeMod/Editor`: clean.
- Chart parity: `03_Chart.cs` (mono `mcs`) and `sea_chart.py --check` print the same lines for seeds 1-2000 and
  20261009. Seed 20261009, the self-test's, is unchanged by the cap.
- Links: 0 of 20,000 charts have a map with 4 links (1,399 before); every changed chart is one that had one.

**Next PC round** (`docs/mode/SEA_CHART.md` §9): the new checks K0, CH, K1, P1, T0-T6 and H1 with the old ones. What
only the game shows: the blacklist during a replay, the chat lines on the boat, the warp home.

## 6. Merge notes

- PR #45 changes the same `Description` line of `Assets/RogueWavesModeMod/Editor/RogueWavesModeModPrefab.cs` (0.1.3 to
  0.3.0): keep this PR's line.
- `docs/PLAN.md` Part C is edited by every open PR: the standing order applies (re-merge the base after each merge).
- Scope `02`, `03` and `04` were edited in both copies (`docs/scope/` and the project files' `scope/`).
