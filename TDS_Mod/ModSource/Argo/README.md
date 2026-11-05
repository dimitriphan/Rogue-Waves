# Argo — Fathoms Below ▪ Intermediate (v1.4.0)

> History: this is the player README of the standalone 1.4.0 release from `ModProjects`. Argo 2.0 (`DESIGN.md`) has
> no SONAR and ships only inside the Rogue Waves pack (`docs/PACK.md`), never as its own `ArgoMod`.

A new vessel for **TDS - Tower Defense Strategy**: a bulky deep-sea diver in a brass helmet, with a crew of Nauts,
a scatter cannon, a harpoon tower and a drill that sinks deeper every Tide.

## Install

Copy the `ArgoMod` folder into `<Steam>\steamapps\common\TDS\Mods\` and enable it in the game's Mods menu.
Every player in a lobby needs the mod. Argo appears on the boat's vessel select. It works alongside Sprigg and Mummer.

## The vessel

- **Argo** is sturdy and slow: 65 health, speed 2.25, melee. Every wrench swing also hits a second enemy standing
  next to the first, for 60% damage.
- **SONAR** (vessel button, the same keys as Buster's Dash) pings every enemy within 6 tiles: for 5 seconds they
  take 20% more damage from everyone, including teammates' towers. Deep Drills inside the ping quake, holding
  enemies in their Tremors for a second. The button greys out during its 15 second recharge, and only you fire it.
- **Fathoms** (the special rule): a Deep Drill sinks a Fathom at the start of every Tide it stands through, down
  to Fathom 5. Each Fathom makes it mine harder and faster (about 1.9x, 3x, 4.2x, 5.6x its first rate) and widens
  and strengthens its Tremors (+0.4 tiles and +4% slow a Fathom). Its owner gets a message saying what each Fathom
  brought. Argo's Scatter Cannons and Harpoon Towers inside a drill's Tremors are loaded by its depth: +1 pellet or
  +1 pierce for each Fathom below the first. Nauts can tune any tower, teammates' included.

## Buildings

| Building | Cost | What it does |
|---|---|---|
| **Scatter Cannon** | 30 red | Short range, sprays a cone of 6 pellets. Shreds crowds up close. |
| **Harpoon Tower** | 35 red | Long range, slow and heavy. Each harpoon pierces 3 enemies down the line that holds the most, and reels the first one 1.5 tiles closer (bosses aren't moved). |
| **Diving Bell** | 35 blue | Home of the Nauts, **Port** (red light) and **Starboard** (green light). |
| **Deep Drill** | 35 yellow | Mines ore up to 2 tiles away for aura (a brass crack runs to the ore it mines), sinks a Fathom each Tide, and its Tremors slow enemies inside its range. |

**Sending Nauts:** select a Diving Bell, press **PORT** or **STARBOARD**, then click any tower on your team. The
Naut walks over and tunes it: +20% attack speed and a slow repair. One Naut per tower. Click the Diving Bell itself
to call a Naut home. A knocked-out Naut swims home and is back in 8 seconds.

Select a building, or start placing one, to see its range: the Scatter Cannon's and Harpoon's reach, the Deep
Drill's Tremors. Turrets aim up and down slopes, and hit enemies below them.

Each building has a branching upgrade tree, tiers 2 to 5, with a tier-5 capstone:

- **Scatter:** Broadside fires a full ring of 16 pellets every 4th volley.
- **Harpoon:** Leviathan Lance, where every 5th shot pierces everything.
- **Diving Bell:** Full Crew doubles tuning, and Nauts dive straight to their tower.
- **Deep Drill:** Mantle Tap mines half again as fast, and every 20 seconds a quake pins every enemy in its Tremor.

The journal has pages for Argo, every building, every upgrade and the Nauts, plus Argo's tab among the vessels.

## Changes in 1.4.0

Each building now has a job tied to the others:

- **Harpoons reel.** The first enemy each harpoon hits is pulled 1.5 tiles toward the tower, into the Scatter
  Cannon's cone. Bosses aren't moved. **Anchor Line** now pins the reeled enemy where it lands, for a second.
- **Drills load the guns.** A Scatter Cannon or Harpoon Tower inside a Deep Drill's Tremors gets +1 pellet or +1
  pierce for each Fathom the drill has sunk below the first. Lose the drill and the guns lose it too.
- **Double Barrel** replaces Choke: the Scatter Cannon loads a second volley while nothing is in range and fires both
  at the next enemy.
- **Old Salts** replaces Hard Hats: a Naut tunes each kind of Argo tower its own way (Scatter +2 pellets, Harpoon +2
  pierce and a longer reel, Deep Drill a stronger slow). Other towers still get the faster attacks.
- **Pay Dirt** replaces Rotary Bit: an enemy that falls inside the drill's Tremors is dug up for two strikes of ore.

## Changes in 1.3.0

- **Harpoon Tower brought into the base game's tower range:** damage 5 (+2 a level; was 9, +3) and range 6.5 (was 8),
  the top of what the shipped towers have. It still fires slow, heavy harpoons that pierce three enemies.
- **Argo's wrench hits for 4** (was 5), Smacky's damage: every swing also hits a second enemy, so Argo stays strong
  in a crowd. Health stays 65.
- **The Diving Bell's upgrades are paid in Blue**, its own colour (support), with some Yellow from tier 3 up; each
  upgrade costs the same total as before.
- **Slows on their own now leave an enemy at least a tenth of its speed** (two strong Tremor slows used to bring it
  almost to a stop), and holds (Anchor Line, Undertow, quakes) stop it fully; they used to let it creep at a tenth.
- A tower or Naut whose script hits an error keeps working (before, one error switched it off for the match).

## Changes in 1.2.2 (quality pass)

- Descriptions are written in words, like the game's own (none of the base game's descriptions carries a
  number): the Deep Drill "mines ore nearby", Rotary Bit "mines much faster", Broadside fires "every few
  volleys", the Fathoms rule and SONAR's text likewise. The exact numbers stay in the journal's EFFECTS lines;
  Gusher's now also says "From Fathom 3 down".
- No gameplay change.

## Changes in 1.2.1

- **Argo is drawn at the base vessels' size.** The sprite filled its whole frame (28x31 px, bigger than any base
  vessel); it is now 24x28 like the concept and Boutman, still bulky, with the gauntlets on the sleeves. The boat
  sprite shrank from 15x14 to 11x12 so Argo no longer towers over the others on the boat.
- **Shorter harpoon barrels.** The Harpoon Tower's gun reached 0.7-0.84 tiles from its centre and swept over the
  next tiles as it turned; the tip now sits 0.55-0.67 out, like the Scatter Cannon's, where the shot leaves.

## Changes in 1.2

- **Fathoms grow much more each Tide.** A drill now sinks to Fathom 5 (7 with Deep Core; was 3 and 5), and every
  Fathom adds +75% ore per strike, +10% strikes a second, +0.4 tiles of Tremor and +4% Tremor slow (was +50% ore and
  +0.5 tiles). At Fathom 5 a drill mines about 5.6x as fast as when built (1.1 topped out at 2x). A message tells the
  drill's owner what each Fathom brought.
- Rotary Bit (+40% mining speed) and Mantle Tap (twice as hard) keep their full value at every Fathom; before, they
  were added to the Fathom bonus and counted for less the deeper the drill went.
- Argo's second swing hit, Barbed bleed, Aftershock pulses and the Leviathan Lance count as Argo's hits: they get the
  kill, and an Aegis no longer heals itself while taking them.
- Every upgrade description was rewritten to say what the upgrade does (the drill mines ore, it doesn't "bite" it),
  and the journal lists each upgrade's exact effects in plain words.

## Changes in 1.1

- Ranges are drawn centred on the towers. The Scatter Cannon's range used to sit two tiles underground (it was
  cloned from Smacky's Bomb Tower, which lobs), the others above the tower; the Deep Drill's range is its Tremors.
- The Scatter Cannon and Harpoon Tower aim along the real line to their target: shots at enemies below no longer fly
  over their heads, and the turrets tilt toward the target.
- SONAR no longer fires by itself whenever its recharge ends (its button refresh used to re-cast it), and no longer
  says "recharging" when you use it. That repeat also shook the camera through nearby Deep Drills' quakes.
- Slowed and held enemies show the game's slow marker (it never appeared before).
- The Deep Drill shows which ore it bites; clearer texts for SONAR, the Diving Bell and the Deep Drill.
- The journal, vessel tabs and boat spot now come from the shared kit used by every vessel mod.

## Notes

- The towers are built low-poly, in the style of Sno Sno's and Buster's, and sized to match them.
- The journal code also keeps the vessel tabs in sync for every vessel mod installed, in whatever order the mods load.
- Source, the asset generator and the automated in-game test are in `TDS_Mod\ModSource\Argo` (see
  `IMPLEMENTATION_NOTES.md`).
