# Scope vs the proven loop: where the run outgrows `SCOPE_v0.txt` (2026-10-09)

> **Renamed since (2026-11-04, at Dimitri's word):** a Vessel Cardinal is now a **Trinket** and a Team Cardinal just a
> **Cardinal** (`docs/scope/12_Cardinals.txt`). The text below keeps the names it was written with.

> **Superseded in part (note added 2026-10-11).** Since this page: 10 maps (not 15) and Depth (not Swell); bonus tides
> have no limit; Fund Transfer moves base aura only; Cardinals stack with no limit; breaks are 60 s; the start is a
> main-menu button, not a start Cardinal; a dropped player can rejoin the running map (`docs/mode/REJOIN.md`); only the
> host's quit spends a retry, and a campaign has 3. Personal Cardinals are now **Vessel Cardinals**, and there is no cap of
> 8 Team Cardinals: Cardinals scale without limit (2026-10-11). Maps are now sailed on the sea chart (`docs/mode/SEA_CHART.md`), so #17's chaining and staging map and the
> "start Cardinal" resume are gone, and #3's "Tide 30 of map 10" is moot (bonus tides never end). §E's engine facts were never moved into kit `docs/15`;
> they stay here. `docs/scope/` and `docs/PLAN.md` Part C "Decisions" win. **2026-10-15:** the full decompile
> (2026-10-14) found `Blm_Player.Steamid` out of a mod's reach (Steamworks is not in the game's compiler references,
> kit `docs/19` §5), so #16, #17, #19 and §F can't key players by Steam id: per-player rows key on the Steam name or
> the GUID the pack keeps (kit `docs/23` §2.3, `docs/19` §5).

Dimitri asked (2026-10-09) whether the scope needs expanding, given the gameplay loop as it stands. The loop as proven
so far: Riptide (`TDS_Mod/ModSource/Riptide`, 70 of 70 on the PC), RogueLab rounds 1-3 (kit `docs/15` §11, §15) and Argo
2.0 (`TDS_Mod/ModSource/Argo/DESIGN.md`). This page lists what that loop does or needs that the scope says nothing
about, and what to add, each with its evidence.

**Updated 2026-10-09 13:45Z with Dimitri's answers.** A run is now a **campaign of 15 maps, saved between maps** (he
changed `docs/SCOPE_v0.txt` that way; this PR carries the edit), so findings 2, 4, 11 and 14 changed and §E is new. He
asked whether Cardinals can belong to one player: §F answers it and finding 6 changed. **Swell is on hold** until the
core loop is settled, so findings 9 and 10 are parked. Everything else still recommends and stays his to veto.

**Updated 2026-10-09 20:48Z (roguewave 2.txt):** a campaign is now **10 maps**, and ascension levels are now named
**Depth** (was Swell). Findings 3, 14 and 20 and §E changed with it; Tide numbers (leave after Tide 15, the 30-Tide
table) are per map and did not.

It builds on two lists and repeats neither: `docs/reports/SCOPE_GAPS.md` (what the scope names but hasn't expanded) and
`docs/RESEARCH_GAPS.md` (engine facts). Existing `! NEEDS ... !` markers are only mentioned where a finding gives them
an evidence-based default. Kit paths: `docs/NN` = `TDS_Vessel_Creator/docs/`. RogueLab round 3 and the §15 facts it
added are in PR #11 until it merges.

**Call:** **Dimitri** = changes the run in a way only he should decide; **default** = taken here, his to veto.

## Summary

| # | Where the loop outgrows the scope | Recommend adding to the scope | Call |
| --- | --- | --- | --- |
| 1 | Every map is a fresh match; the scope never says what carries over | A "carries over / resets" line | default |
| 2 | A run can last close to three hours, in one sitting | **Decided:** a 10-map campaign (was 15), saved between maps (§E) | done |
| 3 | Stock Cardinals scale by the Tide table's length | A 30-Tide table: leave from 15, bonus 16-30 | default |
| 4 | Nothing in the game makes Tides vary or harden | Riptide's director as the Tide source, each map deeper on its curve | default |
| 5 | Foes don't scale with the number of players | Tides scale with player count | default |
| 6 | Cardinals are team-wide, Pure Aura is per player, the HUD holds 8 | Personal Cardinals for one player, team ones capped at 8 (§F) | default |
| 7 | Some stock Cardinals fight the run's own rules | A shop list that leaves them out | default |
| 8 | A stock break is 30 s, too short to shop | Shop breaks that hold the clock | default |
| 9 | Depth (was Swell) has no way to be picked, and saves are per machine | parked: Depth is on hold (Dimitri) | parked |
| 10 | The game's easier setting ("Treated") still works in a run | parked with Depth | parked |
| 11 | A dropped friend can't rejoin a running map; the campaign lives on the host | Drop and host-quit rules | default |
| 12 | A won map would count toward stock records and medals | Runs keep their own records | default |
| 13 | Yossele's Growth resets with every map | Note for Yossele's design gate | default |
| 14 | 10 Direction picks per player per campaign, with stock vessels too | **Decided:** Directions rank up when offered again | done |
| 15 | Four minion vessels multiply unit counts | A unit cap per player | default |
| 16 | The game forgets who a player is between launches | Key the saved campaign by Steam id | default |
| 17 | A saved campaign needs a home and a way back in | The host's save holds it; resume from the lobby | default |
| 18 | Quitting mid-map could undo a loss | **Decided:** limited retries; quitting mid-map spends one | done |
| 19 | Who may join a campaign later | Anyone; saved players get their things back | default |
| 20 | 10 maps need a shape | Two stretches of five, a Breaker map closing each | default |

## A. The run's shape

**1. What carries between maps.** The chain to the next map despawns everything, and `Game_Mapvars`, `Game_Rounds` and
the cores are rebuilt; only the host's `DontDestroyOnLoad` object survives (`docs/15` §3, RogueLab round 1 step 3). So
towers, base aura (back to the mode's 50 of each, `reference/roguelab_round1.txt` line 65) and every in-map resource
(Growth, Thread, Fathoms) reset; only what the host keeps comes back. The scope only says the loop continues map after
map. **Add:** carries over: the vessel, Pure Aura, Directions, Cardinals bought; resets: towers, base aura, vessel
resources. An option for Dimitri: leftover base aura turns into a little Pure Aura when the team leaves, which gives
staying for bonus Tides a second reason.

**2. Run length.** *Decided by Dimitri: a 10-map campaign (15 until 2026-10-09 20:48Z), saved between maps (§E).* What led there: the stock table
gives each Tide up to 180 s and each break 30 s, with a 45 s opening (`roguelab_round1.txt` lines 67-91); a Tide ends
early only when its foes are gone (`docs/13` §3). Fifteen Tides at full timers is about 53 minutes, so three maps can
approach 2 h 40 min before bonus Tides, the map-1 staging load and three 14-20 s loads (`docs/15` §15). The run can't be
saved mid-way (state is in the host's memory, §3) and a dropped player can't come back into a running map (§14.5). At
the same full timers, 10 maps would be up to about 9 hours, so saving between maps is what makes the campaign playable.
Real Tide lengths are unmeasured; the first Phase 5 PC round logs them. If a map feels long, the levers are the leave
Tide, the Tide timers and the break length.

**3. A 30-Tide table.** Five stock Cardinals read `Totalwaves` and scale by 30 ÷ `Totalwaves` in whole numbers (Coin,
Sport Water, Opal Core, Pig Named Bank, Voice in a Bottle; Riptide `DESIGN.md` "The reward pool"). A 15-Tide table would
double them; 20 would round to 1. Bonus Tides by rewinding `Entry` also spend one of the enemy core's Tides left
(`docs/15` §4). **Add** (gives two `! NEEDS !` a default): every map's table has 30 Tides like Mode_Normal; leaving
opens after Tide 15; Tides 16-30 are the bonus Tides; Tide 30 of map 10 is the campaign's final Tide. The enemy core's
"Tides left" counter then reads true.

**4. Where the Tides come from.** The scope wants "modifiers / enemies that change with each run" and "increasing
difficulty", but the game scales nothing by map and has no harder setting (`docs/15` §14.3). Riptide's seeded director
already writes random Tides live, from a budget curve fitted to the stock table, with packs, elites, Breakers and
banners, and passed 70 of 70 (Riptide `DESIGN.md` "Tide budget curve", `docs/15` §13). **Add:** Rogue Waves' Tides come
from Riptide's director, seeded per campaign (the seed goes in the save); each map starts deeper on its curve than the
one before (finding 20 gives the shape); Cursed and Berserk join its elite pool. This gives "what makes each map harder
than the one before" a mechanism; the numbers stay Dimitri's.

**5. Player count.** Nothing scales foes with players (`docs/15` §14.3), while each player brings a vessel, aura income
and Directions. Riptide scales its budget by 1 + 0.35 per extra player (Riptide `DESIGN.md`). **Add:** the mode scales
Tides by player count; 0.35 per extra player is the starting value.

## B. The economy

**6. Cardinals are team-wide.** *Changed after Dimitri asked for Cardinals specific to players: §F shows most can be.* A
Cardinal's upgrades go on the core, which is the team's (`docs/15` §7, §14.1), but the scope buys them with Pure Aura,
which is per player. The HUD tracks exactly 8 Cardinals (`Artifacttrack`, §7), and the lobby already offers five slots
(one per colour plus Mixed), one of them taken by the start Cardinal. Four players buying freely would hit 8 within a
map, and a 10-map campaign far sooner. **Add:** two kinds. **Personal Cardinals** belong to the player who bought them
with their own Pure Aura, act only on that player's things and show on their vessel's panel, not the HUD row (§F).
**Team Cardinals** (the ones that can only act on the core, the foes or the clock) go on the core and the HUD row, at
most 8 a campaign counting lobby picks; any player can pay, and Fund Transfer of Pure Aura lets the team pool (which
answers "which aura can be sent": Pure Aura too). Lobby Cardinals stay allowed, as in Riptide.

**7. Stock Cardinals that clash with the run.** From the 40 logged (`roguelab_round1.txt` lines 19-58): Moon Core skips
the 3rd and every 7th Tide (Pure Aura per Tide, the Tide-15 leave), Looped Mirror repeats a Tide, Vintage Watch and Same
Day Shipping change break length (the shop's time), Money Bag duplicates Fund Transfer, Sand Filled Box is sandbox mode,
Blurry Photo splits vessels (Directions sit on the vessel, §14.1), and Checklist and Computer have their own UI
(Riptide). **Add** (default for "which base game Cardinals the shop sells"): Riptide's mid-run whitelist minus these,
each worth one look in play.

**8. Shop time.** A stock break is 30 s (`roguelab_round1.txt`), in which players also build. Riptide held the break's
clock while players chose, up to 30 s, so nobody could stall the team (Riptide `DESIGN.md` "Salvage"). **Add:** the shop
is open every break, and shop breaks after Tides 5, 10 and 15 hold the clock up to 30 s (Riptide's Salvage rhythm).

## C. Progression and online

*Findings 9 and 10 are parked: Dimitri put Depth (then named Swell) on hold until the core loop is settled (2026-10-09). Kept for when he
picks it up.*

**9. Picking a Depth level.** Depth levels must "carry over between sessions", but the scope has no way to pick one, and
the save is local to each machine and slot (`docs/15` §8) while the run lives on the host (§3). The lobby already offers
only the Cardinals a player's save unlocks, and a mod Cardinal there is proven (§7, §11). **Add:** one start Cardinal
per Depth level, each unlocked in the save by clearing the level below; the host equips the highest they hold or a lower
one, which both starts the run and sets its Depth. Each finisher's own save records the next level (a client write,
untested: the online evening checks it). **Dimitri:** this turns the one unnamed start Cardinal into a family of them (a
level-0 one plus one per Depth); the alternative is one start Cardinal and a Depth pick in the staging step.

**10. The easier setting.** The game's only difficulty option, Treated, still applies in a mod mode: more start aura,
drops ×4, +30 s a Tide, +200 core health, and it marks the match `Mode_Treated` (`docs/15` §14.3). **Add:** Treated is
allowed, but a Treated run unlocks no Depth level (the mark makes the check cheap).

**11. Drops and the host.** A dropped friend can't rejoin a running map and a rejoin comes back with no aura; within a
session, run state is keyed by `Sessionid`, which a rejoin keeps (`docs/15` §14.5). The live campaign is on the host
only (§3). **Add:** a dropped player's Pure Aura, Directions and Cardinals stay in the campaign; they come back between
maps if the host's lobby can reopen (the online evening checks it), or at the next session (finding 19). If the host
quits mid-map, that map is lost to the last save (finding 18); the campaign itself stays in the host's save.

**12. Records and stock unlocks.** "Score does not count", but the end screen saves records and medals under the
Gameplan's name; Riptide gives each map a twin Gameplan so a run touches no stock record or medal, while stock unlocks
for "any map" still count (Riptide `DESIGN.md` "How it plugs into the game"; RogueLab round 3 twinned all 45 maps).
Being modded already turns leaderboards off (`docs/15` §10). **Add:** runs save no stock records or medals; "any map"
unlocks still count.

## D. Vessels inside a run

**13. Growth resets every map** (finding 1). Each map still runs 15-30 Tides, close to a stock map, so Growth pays
within a map, but the "scaling archetype" can't carry across a run unless a Direction carries it. **For Yossele's design
gate:** a Direction such as "towers start one Growth stage up" rather than Growth that survives maps.

**14. Direction catalogue size.** One Direction is picked per map and more are bought, so a campaign gives each player
10 or more. With no repeats, the last offer needs three a player doesn't hold yet: about 12 Directions visible to every
vessel, stock ones included (`docs/PACK.md` §2: the mode runs with stock vessels, which have only the Any pool unless
base vessels get their own, a `! NEEDS !`). Riptide cut its picks to six a run for the same reason: four players picking
every Tide would empty its 40 Cardinals by Tide 10 (Riptide `DESIGN.md` "Changes"). A Direction is an upgrade on the
vessel (`docs/15` §14.1), and a stronger rank is just another upgrade. *Decided by Dimitri (2026-10-09): ranks.* A held
Direction can be offered again as its next rank (II, III), so about 7 Any and 4 per pack vessel cover a 10-map
campaign (10 and 6 were sized for 15 maps; each Direction takes up to three picks, so 10 picks need at least four a
player can draw from, with three left to choose from at the last pick).

**15. Minions times four.** On the host, 200 extra units cost about 3.5 ms a frame (`docs/15` §15); the client's cost is
unmeasured. Four Spools or Uloes in one run multiply Crafted and Photons. **Add:** a unit cap per player for Spool's
Crafted and Uloe's Photons, 50 to start, so a full team stays within the measured 200.

## E. The 10-map campaign (Dimitri, 2026-10-09; 15 maps until 20:48Z)

Engine facts below were read from the decompiled game on 2026-10-09 (`docs/13` §11 setup; files are the game's, cited as
`File.cs:line`). They belong in kit `docs/15` as a new section once PRs #11 and #12 merge, so this PR doesn't collide
with their edits to that chapter.

**16. Who a player is between launches.** The in-session player key, `Sessionid`, is handed out in join order
(`Blm_Multiplayer.cs:457-466`) from a `Player_Guid` each client keeps in the save's `Metadata` folder
(`Blm_Player.cs:82-89`), and the game wipes that folder on every load (`Blockage.Tags.Bla_Data.cs:57, 199-208`). So
after a restart the same friend gets a new GUID and maybe a different `Sessionid`. The Steam id is stable (Steam builds,
`Blm_Multiplayer.Steam`): each client sends it on joining (`Blm_Player.cs:77`) and every machine stores it on that
player's `Blm_Player.Steamid` (`Blm_Server.cs:399`; `Blm_Player.cs:34`). **Add:** the saved campaign keys each player by
Steam id; the live state stays keyed by `Sessionid` and is matched to the saved one when a player joins.

**17. Where the campaign is saved, and how it comes back.** The host writes the campaign to its own save in the pack's
folder (`RogueWaves`) the moment a map is won, before chaining to the next one: the map number, the seed, the maps
played, the next map if it was already voted, and per Steam id the vessel, Pure Aura, Directions and Cardinals (strings
through `Adddata`, proven with a restart, `docs/15` §8, §15). `Savedata()` is public (`Bla_Data.cs:94`), so the mod
saves at once instead of waiting for the game. To resume, the players gather on the boat and the host equips the start
Cardinal as usual; the mod finds the saved campaign and asks "Continue or start over" (the choice dialog, proven). The
host can put each returning player back on their saved vessel: `Tds_Clientinfo.Hero` is a networked string only the
server writes (`Tds_Clientinfo.cs:17`), the same one the game's own `Changehero_ServerRpc` sets
(`Tds_Multiplayer_Remote.cs:245-280`) (read; untested from a mod). The next map loads by swapping the match's strings on
the boat, as RogueLab and Riptide already do for the mode and Gameplan (`docs/15` §11, §13; swapping the map itself
there is untested), so a resumed session needs no staging map. **Add:** the campaign lives in the host's save slot (one
campaign in progress per slot), and only the host who started it can continue it. Giving every player a copy, so anyone
can host, would need each client to write its own save (untested); not recommended for the first version.

**18. Losing a map, and quitting mid-map.** Saves happen only between maps, so a team that quits during a map it is
losing would resume before that map: a free retry. *Decided by Dimitri (2026-10-09): limited retries.* A lost map uses
one retry and the team replays it from the last save; with none left, a lost map ends the campaign. The mod marks the
save "in a map" at each map's start, so quitting mid-map counts as a loss and spends a retry. The retries left are saved
with the campaign. **Default:** 3 per campaign (the count is Dimitri's, still a `! NEEDS !` in the scope).

**19. Who can join a campaign later.** Saved state is per Steam id (finding 16), so a friend who missed a session loses
nothing, and someone new can join. **Add:** anyone can join a resumed campaign; a saved player gets their vessel, Pure
Aura, Directions and Cardinals back, and a new player starts with none and picks a Direction at the next map's start.
The 4-player cap still holds per session.

**20. The shape of 10 maps.** Riptide split 30 Tides into three stretches with a Breaker closing each (Riptide
`DESIGN.md` "The run"), and the 45 offerable maps carry difficulty labels 1-9 (`docs/15` §15; labels scale nothing,
§14.3). **Add:** two stretches of five maps; the vote offers maps whose label fits the stretch (low labels first, the
δ maps last); the fifth map of each stretch ends with a Breaker Tide and a richer shop. 45 maps leave three fresh
choices for every one of the 10 votes without replaying a map, with 15 to spare. Stretch names need to avoid "Depth",
which the scope now uses for ascension (it was "Swell").

## F. Cardinals for one player (Dimitri's question, 2026-10-09)

**Yes, most can be.** Read from the decompiled game (every claim **read**, `File.cs:line`); nothing here has run yet.

**Why they're shared today.** A lobby Cardinal's upgrades go on the core, which has no owner
(`Entity_Ai_Main_Core.cs:181`), and its effect script then spreads them:

| Stock script | What it reaches | One player only? |
| --- | --- | --- |
| `Artifact_Heroapply` | every hero child of the core as it spawns, applying each upgrade named in its params (`Entity_Ai_Artifact_Heroapply.cs:9-16`) | yes: apply those upgrades to one vessel; every vessel carries its player's `Owner` (`Entity_Ai_Main_Core.cs:97`) |
| `Artifact_Towerapply`, `Mobapply` | every new tower or mob on the map: their team check compares the artifact with itself (`Entity_Ai_Artifact_Towerapply.cs:14`, `Mobapply.cs:14`) | yes: the mod's own spawn hook, filtered by `Owner` (the tower-Direction route, `docs/15` §14.1) |
| `Artifact_Teamapply` | towers and mobs whose parent is a hero (`Entity_Ai_Artifact_Teamapply.cs:12`) | yes: filter by the parent's `Owner` |
| `Artifact_Coin` | aura to every player, split (`Entity_Ai_Artifact_Coin.cs:33, 62`) | yes: give to one owner; with an owner the amount isn't divided (`Helper_Resources.cs:50-72`) |
| `Artifact_Coreapply` | the core | no |

**Per stock Cardinal** (by script name; which script each Cardinal uses is data on the PC, so a few are inferred):

- **Can be personal (about 24):** the four Coins, Sport Water, Rusty Pipe, Beast Tassle, Diorama, Fluffy Slippers,
  Frail Titan, Cubed Drink, Aura Gavel, Voice in a Bottle, Pig Named Bank, Blurry Photo (its pick already checks the
  owner), Money Bag (already per player), Checklist (its orb already pays one owner), Torrid Badge, Bendigo, Kippy and
  Fish Bait (spawned mobs take their host's owner, `Helper_Entitytools.cs:88`, so the mod hosts the spawner on the
  owner's vessel), Tacklebox's aura; Paper Tower only with a mod version (one static paper tower per game,
  `Entity_Ai_Effect_Artifact_Paper.cs:13`); Bottom Rock's tower limit is keyed by owner but counts the whole team's
  towers (`17` §9), and its gain multiplier is not per owner.
- **Team only by nature (about 14):** Opal Core, Looped Mirror, Glass Core (the core), Frozen Computer (game speed),
  Moon Core, Vintage Watch, Same Day Shipping's clock (the Tide clock), Chimera Crystals, Starter Seeds (the map's ore),
  Spear Point, 1st Grade Textbook, Blue Paint, Wave Breaker (the foes), Fragment (unclear, barriers by the enemy core).
  Sand Filled Box stays out of the mode.

**How a personal Cardinal works.** The mod never puts it on the lobby list or the core; the host records who owns it
(the lobby list holds names only, `Tds_Multiplayerinfo.cs:21`) and applies its effect upgrades to that player's vessel,
towers and units, or pays that player's aura, re-applied at each map's start like a Direction. It's saved per Steam id
with the campaign (finding 17). New Cardinals can be designed personal from the start.

**Showing it.** The HUD's Cardinal row is built on each machine from its own list (`Game_Mapvars.cs:88`,
`Game_Artifacts.cs:28`), but its networked counters (`Artifacttrack`, 8 slots) are indexed by the host's list order
(`Helper_Entitytools.cs:453-459`), so lists that differ per machine put counters on the wrong icons, and a ninth is
dropped silently. Personal Cardinals therefore show as upgrades on the owner's vessel panel, as Directions do (`docs/15`
§3), and stay out of the 8.

## What happens next

Findings 14 (ranks) and 18 (limited retries, 3 by default) are decided; finding 6 now has a default from §F. Every
default stands until he vetoes it. Accepted rows go into the matching `docs/scope/` file in his format, and they join PLAN Phase 5's
piece list where they add work (campaign save and resume, player scaling, the shop break, the drop rule). The Phase 5
gate in `docs/PLAN.md` now reads "a full 10-map campaign completed on the PC (saved and continued at least once)".
