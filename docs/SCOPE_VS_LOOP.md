# Scope vs the proven loop: where the run outgrows `SCOPE_v0.txt` (2026-10-09)

Dimitri asked (2026-10-09) whether the scope needs expanding, given the gameplay loop as it stands. The loop as proven
so far: Riptide (`TDS_Mod/ModSource/Riptide`, 70 of 70 on the PC), RogueLab rounds 1-3 (kit `docs/15` §11, §15) and
Argo 2.0 (`TDS_Mod/ModSource/Argo/DESIGN.md`). This page lists what that loop does or needs that the scope says
nothing about, and what to add, each with its evidence. It recommends; it does not rewrite `docs/SCOPE_v0.txt`.

It builds on two lists and repeats neither: `docs/SCOPE_GAPS.md` (what the scope names but hasn't expanded) and
`docs/RESEARCH_GAPS.md` (engine facts). Existing `! NEEDS ... !` markers are only mentioned where a finding gives
them an evidence-based default. Kit paths: `docs/NN` = `TDS_Vessel_Creator/docs/`. RogueLab round 3 and the §15
facts it added are in PR #11 until it merges.

**Call:** **Dimitri** = changes the run in a way only he should decide; **default** = taken here, his to veto.

## Summary

| # | Where the loop outgrows the scope | Recommend adding to the scope | Call |
| --- | --- | --- | --- |
| 1 | Every map is a fresh match; the scope never says what carries over | A "carries over / resets" line | default |
| 2 | A run can last close to three hours, in one sitting | A target run length | **Dimitri** |
| 3 | Stock Cardinals scale by the Tide table's length | A 30-Tide table: leave from 15, bonus 16-30 | default |
| 4 | Nothing in the game makes Tides vary or harden | Riptide's director as the Tide source, maps 2-3 deeper on its curve | default |
| 5 | Foes don't scale with the number of players | Tides scale with player count | default |
| 6 | Cardinals are team-wide, Pure Aura is per player, the HUD holds 8 | Cardinals are team buys, at most 8 a run | **Dimitri** |
| 7 | Some stock Cardinals fight the run's own rules | A shop list that leaves them out | default |
| 8 | A stock break is 30 s, too short to shop | Shop breaks that hold the clock | default |
| 9 | Swell has no way to be picked, and saves are per machine | One start Cardinal per Swell level | **Dimitri** |
| 10 | The game's easier setting ("Treated") still works in a run | Treated runs unlock no Swell | default |
| 11 | A dropped friend can't rejoin a running map; the run lives on the host | Drop and host-quit rules | default |
| 12 | A won map would count toward stock records and medals | Runs keep their own records | default |
| 13 | Yossele's Growth resets with every map | Note for Yossele's design gate | default |
| 14 | Directions need a pool big enough for 3 maps, with stock vessels too | A minimum catalogue size | default |
| 15 | Four minion vessels multiply unit counts | A unit cap per player | default |

## A. The run's shape

**1. What carries between maps.** The chain to the next map despawns everything, and `Game_Mapvars`, `Game_Rounds`
and the cores are rebuilt; only the host's `DontDestroyOnLoad` object survives (`docs/15` §3, RogueLab round 1 step
3). So towers, base aura (back to the mode's 50 of each, `reference/roguelab_round1.txt` line 65) and every in-map
resource (Growth, Thread, Fathoms) reset; only what the host keeps comes back. The scope only says the loop "continues
2 more times". **Add:** carries over: the vessel, Pure Aura, Directions, Cardinals bought; resets: towers, base aura,
vessel resources. An option for Dimitri: leftover base aura turns into a little Pure Aura when the team leaves, which
gives staying for bonus Tides a second reason.

**2. Run length.** The stock table gives each Tide up to 180 s and each break 30 s, with a 45 s opening
(`roguelab_round1.txt` lines 67-91); a Tide ends early only when its foes are gone (`docs/13` §3). Fifteen Tides at
full timers is about 53 minutes, so three maps can approach 2 h 40 min before bonus Tides, the map-1 staging load and
three 14-20 s loads (`docs/15` §15). The run can't be saved mid-way (state is in the host's memory, §3) and a dropped
player can't come back into a running map (§14.5). **Add:** a target length, and "a run is one sitting". Real Tide
lengths are unmeasured; the first Phase 5 PC round logs them. If the target is shorter, the levers are the leave Tide,
the Tide timers and the break length.

**3. A 30-Tide table.** Five stock Cardinals read `Totalwaves` and scale by 30 ÷ `Totalwaves` in whole numbers (Coin,
Sport Water, Opal Core, Pig Named Bank, Voice in a Bottle; Riptide `DESIGN.md` "The reward pool"). A 15-Tide table
would double them; 20 would round to 1. Bonus Tides by rewinding `Entry` also spend one of the enemy core's Tides left
(`docs/15` §4). **Add** (gives two `! NEEDS !` a default): every map's table has 30 Tides like Mode_Normal; leaving
opens after Tide 15; Tides 16-30 are the bonus Tides; Tide 30 of map 3 is the run's final Tide. The enemy core's
"Tides left" counter then reads true.

**4. Where the Tides come from.** The scope wants "modifiers / enemies that change with each run" and "increasing
difficulty", but the game scales nothing by map and has no harder setting (`docs/15` §14.3). Riptide's seeded
director already writes random Tides live, from a budget curve fitted to the stock table, with packs, elites,
Breakers and banners, and passed 70 of 70 (Riptide `DESIGN.md` "Tide budget curve", `docs/15` §13). **Add:** Rogue
Wave's Tides come from Riptide's director, seeded per run; maps 2 and 3 start deeper on its curve (for example map 2's
Tide 1 at the curve's Tide 6); Cursed and Berserk join its elite pool. This gives "what makes map 2 and map 3 harder"
a mechanism; the numbers stay Dimitri's.

**5. Player count.** Nothing scales foes with players (`docs/15` §14.3), while each player brings a vessel, aura income
and Directions. Riptide scales its budget by 1 + 0.35 per extra player (Riptide `DESIGN.md`). **Add:** the mode scales
Tides by player count; 0.35 per extra player is the starting value.

## B. The economy

**6. Cardinals are team-wide.** A Cardinal's upgrades go on the core, which is the team's (`docs/15` §7, §14.1), but
the scope buys them with Pure Aura, which is per player. The HUD tracks exactly 8 Cardinals (`Artifacttrack`, §7), and
the lobby already offers five slots (one per colour plus Mixed), one of them taken by the start Cardinal. Four players
buying freely would hit 8 within a map. **Add:** Cardinals are team buys (any player can pay; or players pool through
Fund Transfer, which answers "which aura can be sent": Pure Aura too); a run holds at most 8 counting lobby picks.
**Dimitri:** are lobby Cardinals allowed in a run (Riptide allowed them), and are Cardinals team buys or does each
player buy for the team?

**7. Stock Cardinals that clash with the run.** From the 40 logged (`roguelab_round1.txt` lines 19-58): Moon Core skips
the 3rd and every 7th Tide (Pure Aura per Tide, the Tide-15 leave), Looped Mirror repeats a Tide, Vintage Watch and
Same Day Shipping change break length (the shop's time), Money Bag duplicates Fund Transfer, Sand Filled Box is
sandbox mode, Blurry Photo splits vessels (Directions sit on the vessel, §14.1), and Checklist and Computer have their
own UI (Riptide). **Add** (default for "which base game Cardinals the shop sells"): Riptide's mid-run whitelist minus
these, each worth one look in play.

**8. Shop time.** A stock break is 30 s (`roguelab_round1.txt`), in which players also build. Riptide held the break's
clock while players chose, up to 30 s, so nobody could stall the team (Riptide `DESIGN.md` "Salvage"). **Add:** the
shop is open every break, and shop breaks after Tides 5, 10 and 15 hold the clock up to 30 s (Riptide's Salvage
rhythm).

## C. Progression and online

**9. Picking a Swell level.** Swell levels must "carry over between sessions", but the scope has no way to pick one,
and the save is local to each machine and slot (`docs/15` §8) while the run lives on the host (§3). The lobby already
offers only the Cardinals a player's save unlocks, and a mod Cardinal there is proven (§7, §11). **Add:** one start
Cardinal per Swell level, each unlocked in the save by clearing the level below; the host equips the highest they hold
or a lower one, which both starts the run and sets its Swell. Each finisher's own save records the next level (a client
write, untested: the online evening checks it). **Dimitri:** this turns the one unnamed start Cardinal into a family
of them (a level-0 one plus one per Swell); the alternative is one start Cardinal and a Swell pick in the staging step.

**10. The easier setting.** The game's only difficulty option, Treated, still applies in a mod mode: more start aura,
drops ×4, +30 s a Tide, +200 core health, and it marks the match `Mode_Treated` (`docs/15` §14.3). **Add:** Treated
is allowed, but a Treated run unlocks no Swell level (the mark makes the check cheap).

**11. Drops and the host.** A dropped friend can't rejoin a running map and a rejoin comes back with no aura; run state
is keyed by `Sessionid`, which a rejoin keeps (`docs/15` §14.5). Run state lives on the host only (§3). **Add:** a
dropped player's Pure Aura and Directions are kept until the run ends; they come back between maps if the host's lobby
can reopen (the online evening checks it); if the host quits, the run ends and only Swell unlocks already earned stay.

**12. Records and stock unlocks.** "Score does not count", but the end screen saves records and medals under the
Gameplan's name; Riptide gives each map a twin Gameplan so a run touches no stock record or medal, while stock
unlocks for "any map" still count (Riptide `DESIGN.md` "How it plugs into the game"; RogueLab round 3 twinned all 45
maps). Being modded already turns leaderboards off (`docs/15` §10). **Add:** runs save no stock records or medals;
"any map" unlocks still count.

## D. Vessels inside a run

**13. Growth resets every map** (finding 1). Each map still runs 15-30 Tides, close to a stock map, so Growth pays
within a map, but the "scaling archetype" can't carry across a run unless a Direction carries it. **For Yossele's
design gate:** a Direction such as "towers start one Growth stage up" rather than Growth that survives maps.

**14. Direction catalogue size.** Three are offered per map and more are bought, never one already held, so a player
holding five still needs three new ones at the last offer. Riptide cut its picks to six a run because four players
picking every Tide would empty its 40 Cardinals by Tide 10 (Riptide `DESIGN.md` "Changes"). The mode runs with stock
vessels too (`docs/PACK.md` §2), which have only the Any pool unless base vessels get their own (a `! NEEDS !`).
**Add:** at least 10 Any Directions, and at least 6 per pack vessel, Any ones filling the rest of an offer.

**15. Minions times four.** On the host, 200 extra units cost about 3.5 ms a frame (`docs/15` §15); the client's cost is
unmeasured. Four Spools or Uloes in one run multiply Crafted and Photons. **Add:** a unit cap per player for Spool's
Crafted and Uloe's Photons, 50 to start, so a full team stays within the measured 200.

## What happens next

The three **Dimitri** calls (2, 6, 9) go to him with this page; every default stands until he vetoes it. Accepted rows
go into `docs/SCOPE_v0.txt` in his format, and findings 1-12 join PLAN Phase 5's piece list where they add work
(player scaling, the shop break, Swell Cardinals, the drop rule).
