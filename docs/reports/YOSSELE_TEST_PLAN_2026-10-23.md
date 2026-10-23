# Yossele 0.2.0-dev: test plan for what has not run yet (2026-10-23)

> **Since written (2026-10-23, evening):** §2 ran. PC round 9 passed on its fifth try, 9e: 160 pass, 0 fail, 0 setup,
> and 0.2.0-dev is Dimitri's play build. Every failure on the way was the test itself, a stock look the game doesn't
> load (kit `docs/09` #110) or a handoff made from the wrong base; none was a rule of the sheet. Two engine facts came
> out of it: a node pays on the hit after its `Healthresource` reaches 0 (#106, corrected) and a foe's health reads 0
> right after its spawn (#111). Rounds in Yossele's `IMPLEMENTATION_NOTES.md`. Sections 3-7 are still open, plus one
> new item for a played map: the laser most likely mines any ore node in range (the stock ranged AI targets
> `Resource`).

Dimitri asked (2026-10-23): update Yossele's mod to the current vessel documentation, then begin testing the cases that
need testing but haven't had a chance yet. The update is 0.2.0-dev (`TDS_Mod/ModSource/Yossele/IMPLEMENTATION_NOTES.md`,
"Build of 2026-10-23"). This page lists every case that has not been seen running, says which check covers it, and
records the checks that could run here, in the cloud. Nothing on this page has run in game.

**Definitions used for the checks below.** A *case* is a rule, look or code path of 0.2.0-dev, or one left unchecked by
0.1.0-dev, that nobody has seen working in game. An *issue* is something that would throw, behave against the sheet
(`docs/scope/07_Vessel_Yossele.txt`) or against the decompiled game, or a mismatch between the code, the art
generators and the sheet. Wording and style are not issues. **Pass cap: one pass of each cloud check**, no repeats.

## 1. Run here (cloud), 2026-10-23

| Check | Result |
|---|---|
| Compile, kit 1.6, release / `YOSSELE_SELFTEST` / `YOSSELE_SELFTEST,YOSSELE_SHOTS` (`check_mod.py`) | 0 errors, 0 warnings, all three |
| Editor code (`check_editor.sh Assets/YosseleMod/Editor`) | passes |
| Art preview (`lite/run_preview.sh ... sma`) | every sheet, model and sound renders; changed files in `TDS_Mod/ModSource/Yossele/Previews/0.2/` |
| Consistency script, upgrades: each of the 32 has a model key, each "leads to" exists | 0 issues |
| Consistency, looks: each of the 27 Vis types the server sends has a client case | 0 issues |
| Consistency, assets: each sound played is generated and has a volume, each effect named exists | 0 issues; `Debuff_Yossele_Daze` is generated but unused since Hallucinogen became a plain stun (stock look) |
| Consistency, numbers: every upgrade cost, and each building's cost, Growth max, health, damage, attack speed and range, against the sheet; the vessel's and the Cordyfriend's stats | 0 mismatches |
| One read of the riskiest new code (D-MDS, Bonsai copy and slot, Cordyfriend spawn and despawn hooks) | 0 issues; Garden Weeds had no self-test check, now added (§12 below); a per-Tide log line added for sections 3 and 5 |

The consistency script is in the thread's scratch space, not the repository; the checks it ran are the rows above.

## 2. PC round 9: the self-test (`Code/99_SelfTest.cs`, run by `Run-SelfTests.ps1`)

148 checks in the code (round 8's build had 89). New or changed since round 8, by section:

| § | Cases | Kind |
|---|---|---|
| 1 | The stock ranged AI, armed with `Hero_Yossele_Laser`; stats 70 / 1.75 / 5.5 / range 4 / projectile 15; a laser hurts a foe in range and the client counts the Zap | engine assumption (laser clone, Attack command) |
| 4 | 5 Growth stages a Tide when the map has 4 Tides or fewer (forced count); the Tree full at a Tide's start | rule |
| 6 | Viral Outbreak's numbers; Explosive Finish; Cordyceps' chance (20% + 5% a stack); a Cordyfriend rises, stands owned by the cannon's player, has 75 / 2 / 1.5 / 0.5 / 2.25, fights with the stock melee AI on the Snoble's block, never tagged Sno, walks to a foe near where it rose | engine assumption (minion spawn) |
| 7 | Deathcap's cap of 8 and its keep-above-8 rule; Hallucinogen's stun holds a foe | rule |
| 8 | Rubbery Shell bounces to a foe within 2 tiles; Dandelion Storm's +100% range, +20 seeds, +100% seed growth | rule |
| 9 | Easy Target's mark after 5 s and its +15% on a hit; Entangling Roots; Monument's armour, and its strip of foes' debuffs | rule; the strip uses a faked stun |
| 10 | Obsidian's health | rule |
| 11 | Fruit from a real payout of a stock node; Grafted at 70%; Connected Roots and Fortifying Roots together; Forest Project with no cap; Harvesting Season; Softening Roots takes one hit off a node's payout; Spontaneous Mulch | engine assumption (payout, `Healthresource`) |
| 12 | A rebuilt Tree; Demand and Supply; D-MDS' seed growth -50%, its boss cost, a ballista bolt shot down (seeds spent, bolt gone), its seeds never strike; Bonsai: the slot, cost Red 50, sells for 0, 40% stats, copied upgrades at 40%, the empty slot while it stands, no second one, its Growth seeds, the slot back when it goes; Garden Weeds: no stack outside the 3 s window, stacks capped at Growth, +10% seed growth each | engine assumption (pooled projectiles) |

A round passes at 0 FAIL; a SETUP line (a test that couldn't set itself up) is read case by case (`TDS_Vessel_Creator/docs/08`
§5). Counters logged, not checked: Viral bursts, natural Cordyceps rises, Hallucinogen's natural stuns, the client tint.

## 3. In game, not in the self-test

To check on a played map, by Dimitri or in a later scripted round:

1. **Cordyfriends over a whole map:** they walk back home when no foe is near, foes stop at them, they stay when their
   cannon is sold or felled, and the 31st doesn't rise while 30 stand.
2. **D-MDS against real shooters:** an archer's arrow, a ballista bolt and a bomb (the bomb must not explode); several
   at once; a boss's shot costing 5 seeds.
3. **Monument against a real Bulwark stun** (the self-test fakes the stun's modifiers).
4. **Growth on a real short map:** a map whose Tide count is 4 or fewer gives 5 stages a Tide; every other map, 1.
   Which stock maps qualify is not known yet: the host's log now prints each Tide's line with the map's Tide count
   and the stages it gave (`05_Server.cs` `OnTide`).
5. **Fruit on each node kind** the maps have, and Softening Roots' modifier going away when the Sapling is sold.
6. **The Bonsai** keeping its copy when the Tree is felled, and the slot when the Tree is rebuilt.
7. **Connected Roots** shares only the Sapling owner's buildings' hits (needs a second player's building in range, so
   online, item 6.4).
8. **Garden Weeds in play:** stacks from real seed kills and their 15 s.

## 4. By eye (temporary art)

1. The attack: the mouth's dim, glow and flare frames line up with the shot; the laser bolt's size and colour.
2. The new models (`Previews/0.2/`), the Bonsai dish, and the vine texture's join between tiles (the preview doesn't draw
   textures).
3. The Cordyfriend: the Snoble's sprite tinted fungus green, and how it looks gliding (the Snoble has only Idle).
4. The Mend sparkles on Natural Remedy and Sustaining Practices, and the Easy Target mark.
5. Still open from 0.1: debuff marker heights, vine and fruit placement, the Stonehenge's facing arrows
   (`IMPLEMENTATION_NOTES.md` rounds 2-8).

## 5. Chance-based

Viral Outbreak (30%), Hallucinogen's natural stun (20% every 5 s at full spores), Cordyceps' natural rise (20% + 5% a
stack) and Garden Weeds (40%) are logged as counters in the self-test. In a play session the host's `Player.log` now
gets one line a Tide with the same counters (totals since the game started) and the Cordyfriends raised, fallen and
capped, so a played map shows whether each one fires at about its rate.

## 6. Online (needs a friend)

1. The laser, its Zap and the attack frames on a client.
2. The Cordyfriend's tint on a client.
3. D-MDS: the client's copy of a shot-down projectile goes out too.
4. Connected Roots with two Yossele players: only the owner's buildings.
5. Debuff looks sent once per target (Argo 2.0.1's way) and cleared at a map's end.
6. The Tree and Bonsai slots per player.

## 7. Balance (Dimitri)

Every `~value~` on the sheet is a Claude default and waits for his pass: notably Projectile speed 15, the Cordyfriend's
cap of 30, its orders and leash, D-MDS' boss rule (a shooter of 600 base health or more costs 5 seeds), Softening
Roots' line and cost, and the 0.1 defaults still marked on the sheet.

## 8. Order

PC round 9 (self-test of 0.2.0-dev, then swap Dimitri's play build to it) → his play test (sections 3-5) → his sprite
sheet → his balance pass → one online evening (section 6) → the one-part pack build (`docs/PLAN.md` Release 1, step 4).
