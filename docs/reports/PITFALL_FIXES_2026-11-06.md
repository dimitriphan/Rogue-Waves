# Fixes the pitfalls point to (2026-11-06)

Dimitri asked (07:15Z): "Based on the pitfalls, would there be any bug fixes or improvements that might fixable?"
This is an assessment on base `3680331`, read from the code only: nothing was changed, built or run on the PC. Every
pitfall in kit `docs/09` (#1-#173) was checked against the six parts and `Shared`, with the open leads from
`BATCH_CHECKS_2026-11-06.md` §0, `CODE_REVIEW_FIXES_2026-11-05.md` §4 and `CHART_BUILD_2026-11-06.md` §5. Each hit
below was read in place; "plausible" means the code reads that way but nothing has shown it in game.

## 1. Fixable now (no decision needed), best first

Rows 4, 5, 6 and 10 were left for the PC by the 11-05 fix report; each can be written now, then proven in its
next PC round, so their risk is medium.

| # | Pitfall / id | What to fix | Where | Effort | Risk |
| --- | --- | --- | --- | --- | --- |
| 1 | #80, ULO-2 | One exception switches a brain off for the whole map (`enabled = false`): Uloe's vessel, buildings and Photons go dead. Argo and Yossele skip the bad tick and stop only after many failures in a row. Give Uloe a local copy of that guard (`ArgoFaults`, `Argo/Code/01_Util.cs:13`); Spool has the same fault, and moving the guard into the kit for all four is in §2 | `Uloe/Code/06_HeroBrain.cs:75`, `07_Building.cs:64`, `08_Bank.cs:212` | small | low |
| 2 | #146, elite reserve | When a stock elite isn't loaded on a map, its stand-in is taken from the campaign's own four first (one that was due later), so the map gets the wrong one; scope `04`:19 says the next one outside the four. Keep the first picks, fill a gap from outside the four | `RogueWavesMode/Code/21_Difficulty.cs:277-286` | small | low |
| 3 | Pearl Oyster | An Oyster given by a Wreck after a buy goes through `OnGain` only, so `Bought` stays true and it pays nothing until the next map. (Buying the Oyster itself restarting its pay matches the code's "since holding it" rule.) Set `Bought = false` when the Oyster is gained | `RogueWavesMode/Code/24_Trinkets.cs:482-491` | small | low |
| 4 | #158, ARG-2/ULO-6 (plausible) | Build looks and Photon pops are sent in the first frame, before a client's 0.25 s hook scan, so a client can miss them. Send them about 0.5 s later; left for a PC look on 11-05, so watch a building go up on a client after the fix | `Argo/Code/07_TowerBrain.cs:135`, `Uloe/Code/07_Building.cs:84`, `Uloe/Code/08_Bank.cs:222` | small | medium |
| 5 | FOES-1 (plausible) | A respawned vessel that carries Illuminate is never cleared, because clearing asks the brain's own list, not the thing's upgrades; left on 11-05 for a PC fact (whether a respawn keeps the upgrade), so prove it on the PC after | `RogueWavesFoes/Code/00_RogueWavesFoesMod.cs:358` | small | medium |
| 6 | MODE-B5 (plausible) | The weather's start waits up to 60 s and then announces, even if that map has already ended. Stop if the map changed (the `RWRejoin.InMap` guard); left on 11-05 until seen, so check a short map on the PC after | `RogueWavesMode/Code/07_Weather.cs:253-262` | small | medium |
| 7 | Chart notes 1-3 | The end title and Continue line say "Map N" where the card says "Row N"; "THE COVE" banner shows again on Continue and for a joiner; no self-test for the Cove's rank-up gift | `06_Campaign.cs:308`, `12_Menu.cs:157`, `13_Card.cs:275`; `26_ChartView.cs:337-339`; `90_Stops.cs` | small each | low |
| 8 | #143 | Two cards can still get no picture and draw as a white square: a stock Cardinal with no icon, and the Back card if the game's icon lookup throws. Fall back to the crest | `RogueWavesMode/Code/17_Cardinals.cs:332`, `18_Shop.cs:211-215` | small | low |
| 9 | #117 (plausible) | The catch-up pass gives stock Cardinal upgrades with no `Artifactobj`; the other two give paths pass it. Pass `art` here too | `RogueWavesMode/Code/17_Cardinals.cs:715` | small | low |
| 10 | ARG-2 (11-04) (plausible) | Argo's slow look is sent every 1.5 s but lasts 0.5 s (water) or 0.75 s (Tremor), so it blinks. Send the look for the full 1.5 s window; left on 11-05 until seen; only the look's length changes, not the slow | `Argo/Code/11_Water.cs:178-184`, `07_TowerBrain.cs:699-701` | small | medium |
| 11 | #95 (plausible) | Two effects use the game's own throttled one-shot, so a busy online fight drops them; the pack's own send path doesn't | `Argo/Code/11_Water.cs:282`, `RogueWavesMode/Code/24_Trinkets.cs:588` | small | low |
| 12 | #157 | A shot whose shooter died while it flew loses its Knife, Chain or wall bonus (`hit.From == null` returns early). Rare and small | `24_Trinkets.cs:257`, `22_AnyDirections.cs:314`, `23_BaseDirections.cs:294` | small | low |
| 13 | MODE-B6 | The rejoin's three "what died" flags are only cleared by a rejoin, so a flag from an earlier map can act on a later one. Tag each with its map | `RogueWavesMode/Code/09_Rejoin.cs:315-325` (set), `353` (Repair) | small | medium |
| 14 | Text | The self-test label "one slow a Flood Light" doesn't parse | `Uloe/Code/98_Upgrades.cs:413` | small | low |

Bigger, and best proven on the PC or with two players: SPL-3 (a Thread Head's pin taken "ok, unchecked" may be
unreachable; `Spool/Code/08_Tin.cs:207-209`, medium, medium), chart note 4 (map votes carry no round, so a late vote
could count in the Cove's second question; `20_Vote.cs:373`, `718`, medium, medium, online), SPL-5 (thread already on
the ground is never shown to a late joiner; `Spool/Code/05_Server.cs:220`, large, high).

## 2. Needs Dimitri's word

- **Supernova:** scope `10`:159 says the Photons "all come back at once"; the code fills every empty slot and bursts the
  Photons where they stand (`Uloe/Code/08_Bank.cs:76-80`). Should a bursting Photon fly back to its Power Bank?
- **Plug In on a safe socket:** the bank's part is capped at 8, but Brighter Bulb adds up to 3 on top, so Uloe can give
  11 (`Uloe/Code/06_HeroBrain.cs:164`, `173`). Is "at most 8" Uloe's total Light or the bank's part?
- **Spool and the kit-wide guard:** Spool's brains switch off on their first exception too (#80, SPL-4:
  `Spool/Code/06_HeroBrain.cs:48-52`, `07_Building.cs:50`, `08_Tin.cs:299`), and SPL-3 is open. An earlier fix report
  left Spool as is and called one kit-wide guard a design change (`CODE_REVIEW_FIXES_2026-11-05.md` §4). Moving the
  guard into the kit (kit 1.15) for all four vessels is medium effort and medium risk; fix Spool now, or with its 22
  upgrades thread?

## 3. Needs the PC (code is in place, only a look is missing)

#151 Grafted fruit keeps its skin; #152 foes from map 2 don't look Hardened; #153 Quickening Current's speed survives
a join; #156 the menu crest and cards don't jitter; #158 a building going up on a client (after item 4); #173 a late
joiner's Sprite foes have bodies; YOS-4 and #114: whether the stock `Debuff_Slow` look is loaded on every map
(Yossele and Argo use it; Argo only preloads `Hero_Buster`, `Argo/Code/02_Data.cs:24`), else preload its group;
ARG-1 attack speed floors on foes other than Dagger and Spear.

## 4. Checked and not a bug

`Uloe/Code/09_Glass.cs:128`'s `"Collide"` is the game's beam param (next to `Beam`, `Track` in the game's strings), not
a misspelt `"Collision"`, which is the projectile's. The vessels' `Updatecommand(..., true)` at spawn run before the
handler subscribes, on purpose (#36). The `"Hero"`-parent checks in `17_Cardinals.cs:699`, `993` copy the stock
Voiceapply filter (#145 applies to finding the vessel, which `:709` guards). `NotSold`'s `Artifact_Rock` is Bottom Rock
(#132), dropped on purpose. Already fixed: MODE-C2/C3/C5/D9/A3/C11, YOS-2, ULO-3 and teammate feeding (decided), SPL-6,
the rejoin log lines, the Death Ray text, the reports README's 17/12 row, #154, #155, #157's F-key case. Clean on the
rest of #1-#173 that code can show.
