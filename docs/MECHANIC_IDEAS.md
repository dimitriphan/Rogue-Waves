# Mechanic ideas from the game's own systems (2026-10-09)

> Status 2026-10-11: Dimitri picked **sea weather** (§2) on 2026-10-10. It is now in `docs/scope/05`, built in
> `RogueWavesMode` and passed PC round 4 (38/0, `docs/SEA_CHART.md` §8-9). The other ideas are still unpicked and
> untested; the text below is as written on 2026-10-09. "Personal" Cardinals are now called Vessel Cardinals.

Dimitri asked (2026-10-09 23:04Z) for a deeper look at how TDS works, the important topics written into the research
lessons, and mechanics that would fit his current plan. The lessons are in the kit: **`docs/16`** (new: inside a map:
grid, paths, map objects, aura nodes, the wallet, game speed, lighting) and **`docs/15` §18** (the stock Cardinals'
patterns, messages to every client, the final map, sea-chart decorations, unlocks). This page is the second half:
ideas for him to pick from. **None of it is a scope change**; the files in `docs/scope/` are untouched.

Every idea rides on something the base game already does, so the code to copy exists. Tags as elsewhere: **read** =
from the decompiled game (`file:line` into that folder, kit `docs/13` §11), **untested** = not tried in game; nothing
here has been. Kit paths: `docs/NN` = `TDS_Vessel_Creator/docs/`. Rules kept: no sonar, Argo's numbers untouched,
Depth stays on hold (ideas that could later serve Depth say so and stop there).

## 1. Shortlist

If only five go further, these fill the scope's open `! NEEDS !` lines with the least new code:

| # | Idea | Fills | Rides on |
| --- | --- | --- | --- |
| 1 | **Sea weather on chart nodes**: some maps on the chart carry a condition (rising water, a quickening clock, a nest), shown when the leader hovers the node; harder weather pays more Pure Aura | `04` "which modifiers change from campaign to campaign"; makes the chart's vote a real choice | the stock map objects spawned mid-map (`docs/16` §3), the node billboard (`docs/15` §18.4) |
| 2 | **Five objectives from stock pieces**: Raze the Nest, Bag the Beast, Clean Hull, Beat the Clock, plus the existing Mind the Meteor on a one-shot node | `14` "list of optional objectives" | Halberd nest, aura beast, core damage, the Tide timer, `Resource_Death` (`docs/16` §3-4) |
| 3 | **Cost Directions** (Any): build and upgrade discounts for one player, already shown on the buttons | `11` "Directions for every vessel" | a build button's `Goalvalue` is that building's price scale (`docs/16` §2) |
| 4 | **Base-vessel Directions** from each stock vessel's own numbers (Buster's dash range, Fie's regulars, Boutman's rally, Sno Sno's trail, Smacky's bombs) | `11` "(Base Game Vessels)" | the hero and tower AIs' params (§4) |
| 5 | **Cardinals from the stock patterns**: a countdown Cardinal, a free-build Cardinal, an interest Cardinal, a last-stand Cardinal, a break-length Cardinal | `12` "list of new Cardinals" | the 12 patterns in `docs/15` §18.1 |

## 2. Sea weather on the chart (per-map conditions)

The scope says each map is harder than the last, with "more enemy modifiers" and changes "from campaign to campaign"
(`docs/scope/04`), and the chart offers 1-3 linked maps per vote (`05`). Today the maps on offer differ only by the
base map. **Idea:** when the chart is generated, some nodes get a weather from a small list; the leader's hover shows its
icon over the node (`Macro_Addon_Nodeshowbillboardlink` needs only a sprite, `docs/15` §18.4), the in-map vote card
names it, and the host spawns its object when the map starts. Weather pays extra Pure Aura per Tide, so the team trades
danger for pay, Slay the Spire's elite rooms. Seeded with the chart, so every client agrees.

| Weather (sea-weather names) | What happens | Rides on (read) | Notes |
| --- | --- | --- | --- |
| **Rising Water** | each Tide the water climbs; flooded tiles can't be built on, and shots through water slow | `Main_Flood` (Entity_Ai_Main_Flood.cs:27-80) spawned at the map's lowest surface | the pack's theme exactly; maps with low ground lose space over 15 Tides. Argo's Deep Water and the flood are different systems, no clash in code |
| **Quickening Current** | from a set Tide the whole game runs faster (×1.25, then ×1.5) with "THE CLOCK QUICKENS" | `Main_Chrono`, or the host writing `Game_Mapvars.Time_Scale` (Entity_Ai_Main_Chrono.cs:14-53) | stacks with players' fast-forward; bonus Tides get shorter |
| **Nesting Ground** | a Halberd nest sits near the enemy cores, sends a Halberd every few Tides and grows | `Tower_Halberdnest` (Entity_Ai_Tower_Halberdnest.cs:27-69) | doubles as objective §3.1 |
| **Split Current** | from Tide 1 every enemy core's spawns fan out to the others, so foes come from every side | `Main_Enemycoresplit` (Entity_Ai_Main_Enemycoresplit.cs:11-29) | only bites on maps with 2+ enemy cores (most have 1-4, `docs/15` §15) |
| **Beast Shoals** | a den of aura beasts wanders the map; they pay aura when killed but fight back | `Tower_Den` + `Mob_Aurabeast` (Entity_Ai_Tower_Den.cs:40-144) | a "good" weather: risk for base aura |
| **Fog Bank** (look only) | later maps darker and foggier | `Bl_Lighting.Editlighting` with a stock lighting group, run on every client (`docs/16` §7) | no gameplay effect; a mood ramp across the 10 maps, and Uloe's light reads better in it |

Left out on purpose: **Oroboro** (moves the cores and kills every vessel building mid-map, Entity_Ai_Oroboro.cs:30-172)
is too destructive for a run with retries; keep it for a Breaker map if ever. Weather is one per map at first; later
maps could carry two, which is the scope's "each map harder" without touching Depth.

**Cost:** a weather table, a spawn at map start, one billboard sprite each, the extra pay. **Untested:** whether each
object works on a map not built for it (`docs/16` §9 items 1-2).

## 3. Optional objectives from stock pieces

The scope's template (`docs/scope/14`) has two entries (Net the Shoal, Mind the Meteor). These fit it field for field;
names follow its "short order on a notice board" voice. All are offered at VENDCINT in a break and last the next Tide.

| Objective | Kind | Task | Rides on (read) | Taken by |
| --- | --- | --- | --- | --- |
| **Raze the Nest** | Hunt (structure) | a Halberd nest lands near the enemy cores; destroy it before the Tide ends | `Tower_Halberdnest` spawned by the host; done on its `Despawn` with death (`docs/16` §3) | team |
| **Bag the Beast** | Hunt | one large aura beast wanders in; kill it before it leaves | `Mob_Aurabeast` (pays base aura split on death, Entity_Ai_Mob_Aurabeast.cs:152-190) + a `General_Expire` timer to make it leave | team, or the player who lands the kill (`Lasthit` caveat, `docs/15` §16) |
| **Clean Hull** | Guard | no foe hurts the Basin this Tide | the core's `Entity_Master.Damagetaken` running total, read before and after (`docs/15` §14.7) | team |
| **Beat the Clock** | Rush | clear the Tide before its timer runs out | a Tide ends early when its foes are gone; the host compares `Time_Wave` at the Tide's end (`docs/13` §3) | team |
| **Light Purse** | something new: thrift | finish the Tide having spent under a set amount | `Activeplayerdata.Spent` per player (`docs/16` §5) | one player |
| **Steady Hands** | something new: survival | your vessel doesn't drown this Tide | `Activeplayerdata.Vesseldeaths` (`docs/16` §5) | one player |

**Mind the Meteor** (already in the scope): the stock mine node never runs out (it refills and pays again,
Entity_Ai_Resource_Mine.cs:106-138), so "fully mine" needs a one-shot node: `Resource_Death` dies and pays when emptied
(Entity_Ai_Resource_Death.cs:25-73), and a node pays all players split, which matches its "All players". For its
design gate.

## 4. Directions the engine already supports

### Any vessel

| Direction | Kind | Rank I → III | Rides on (read) |
| --- | --- | --- | --- |
| **Cut Costs** | Boost, all its buildings | buildings cost less (e.g. 10/20/30%) | each build button's `Goalvalue` is the price and sell scale for that player; the button shows the new price (Entity_Ai_Hero_Buildsell.cs:76-87; Entity_Command_Build.cs:70-93) |
| **Cheap Refits** | Boost, all its buildings | upgrades cost less | the `Upgrade` command's `Goalvalue` (`docs/01` §7) |
| **Spare Hull** | Shift, one building | one free copy of a chosen building each map; the button comes back when it falls | the Onetimebuild Cardinal's free-build button (`docs/15` §18.1) |
| **Last Stand** | Shift, the vessel | the vessel survives its first lethal hit each map at 1 health | the Basin's own `Finalhit` (Entity_Ai_Main_Core.cs:36-45) on the vessel |
| **Compound Interest** | Boost, aura income | each Tide, a small share of your base aura is paid on top, capped | Fie's silo tide payout (Entity_Ai_Tower_Fiesilo.cs:341-362), per player |
| **Parting Shot** | Shift, its units (minion vessels) | when one of your units dies it fires a bolt at the nearest foe | the Gavel Cardinal (Entity_Ai_Artifact_Gavel.cs:14-76) |

Cost Directions are the cheapest content in the pack: no new art beyond the icon, per player by construction, and
honest on screen. They also answer "what does a base vessel get" for every vessel at once.

### Base game vessels (`11` "Base Game Vessels")

| Vessel | Direction | Rides on (read) |
| --- | --- | --- |
| Buster | **Full Gallop**: keeps his full reach while dashing (today -1 range in the dash) | `Entity_Ai_Hero_Buster` `Rangemod` = -1 while `Movelinear` (Entity_Ai_Hero_Buster.cs:44-55) |
| Fie | **House Regulars**: the Rest Stop keeps more permanent regulars (stock 4) | `Permcount`, 4 until an upgrade sets `Perm_Count` (Entity_Ai_Tower_Fieshop.cs:36, 174-190, 243) |
| Boutman | **Long Line**: rally reaches farther and lasts longer | `Fishingrange` stat and `Rallyduration` (Entity_Ai_Hero_Boutman.cs:269-310) |
| Sno Sno | **Packed Snow**: the snow he leaves starts deeper, so he runs fast on it sooner | each step spawns snow at `Level` 20; on snow above 20 he gets +75% speed (Entity_Ai_Hero_Snosno.cs:56-82); the Direction raises the level of his snow as it spawns |
| Smacky | **Skipping Stones**: Bomb Tower shells bounce once more | `Splitbounce` per shot (`docs/16` §8) |

These change a number the vessel's own code already reads, so the vessel plays as Blockage made it, a little further
along.

## 5. Cardinals from the stock patterns (`12` "list of new Cardinals")

Names follow the style sheet (two plain words, a found object the wave washed up). Each copies a stock Cardinal's
pattern (`docs/15` §18.1); Personal or Team as `docs/SCOPE_VS_LOOP.md` §F defines.

| Cardinal | Kind | Effect (base style, no numbers) | Pattern it copies |
| --- | --- | --- | --- |
| **Barnacled Anchor** | Personal | after a number of Tides on a map, your vessel hardens for the rest of it | Rusty Pipe's countdown (Entity_Ai_Effect_Artifact_Buff_Rustypipe.cs) |
| **Spare Plank** | Personal | one free wall each map, back when it breaks | Onetimebuild |
| **Piggy Float** | Personal | your unspent base aura earns a little each Tide | Fie's silo interest |
| **Ship's Hourglass** | Team | breaks last longer (more shop time) | Vintage (Entity_Ai_Artifact_Vintage.cs:26-58); answers `SCOPE_VS_LOOP` #8 without holding the clock by hand |
| **Cork Lifebuoy** | Team | the Basin survives one extra killing blow each map | the Basin's `Finalhit`, granted again |
| **Message Bottle** | Team | each map, the vote shows one more map | the chart (`docs/SEA_CHART.md`): offer a fourth link |

**Vampire Dentures** (in the scope) rides on `Damagedone` (proven, `docs/15` §16); nothing new needed.

## 6. Shop, messages and the campaign's feel

- **VENDCINT's arrival and objective offers** as the stock warning popup ("VENDCINT HAS LANDED") and the note under the
  banner, which reach every client with no new sync (`docs/15` §18.2). Cheap polish.
- **Consumables at VENDCINT** (a scope change, so only an idea): a "snack" for Pure Aura that buffs your towers for
  one Tide, built the way Fie's Rest Stop buffs (a timed modifier plus a debuff look, `docs/15` §18.1). A Pure Aura sink
  between Direction ranks.
- **Music that builds with the run:** the mode's `Musicwave1..4` params set the Tides where the music steps up
  (Clientmode_Normal.cs:22-83); put the last step at Tide 15 so the leave vote feels like a peak.
- **Campaign milestones** on the boat's Discoveries screen ("Campaign cleared", "Cleared with three retries left"),
  written into every player's own save with `Dataadd_ClientRpc`, so friends get them too, not just the host
  (`docs/15` §18.2, §18.4).
- **A campaign summary** after map 10 from the per-player match statistics (kills, aura gained, built, vessel deaths);
  the host snapshots them each map, since they reset when a player stops being active (`docs/16` §5).

## 7. Watch-outs found on the way

| Finding | Why it matters | Default |
| --- | --- | --- |
| **Bottom Rock makes Fund Transfer free.** It sets the spend multiplier to 0, and `Useresources` applies it (Helper_Resources.cs:121-132) | a transfer would give aura without taking any | the transfer sets the multiplier to 1 around itself; or leave Bottom Rock out of the shop (add to `SCOPE_VS_LOOP` #7's list) |
| **A fifth wallet currency leaks** into Glass Core, the Converter and Fie's silo interest, which walk every wallet entry (`docs/16` §5) | Pure Aura must stay a counter on the vessel, as decided | keep it; no change |
| **Every Basin survives one killing blow per map** (`docs/15` §18.3) | retries and "lost map" logic should expect the 1-health save | none needed; Cork Lifebuoy builds on it |
| **No path check on building** (`docs/16` §2) | four minion vessels and walls could seal a lane; whether stock walls can is data | a Phase 5 PC check on a crowded map |
| **Map objects never unsubscribe from Tides** (`docs/16` §3) | a weather object despawned mid-map keeps reacting until the map ends | spawn weather for the whole map only |
| **Discord presence can't be set** (`docs/15` §18.5) | "Map 3 of 10" in Discord isn't possible | none |

## 8. What needs the PC

Nothing here was tried in game. The checks that would settle the shortlist are kit `docs/16` §9 (map objects spawned
mid-map, re-pathing, price scales, game speed on a client, the warning popup with a mod icon); they can ride along with
the sea chart's probes C1-C7 in the next RogueLab round. No round was started.
