# Uloe: design (v0.1.0-dev, 2026-11-03; the 24 upgrades and Light sharing built 2026-11-05)

Rogue Waves' Uloe is **built from the scope text**: `docs/scope/10_Vessel_Uloe.txt`, the pack's last vessel. Every rule the
sheet gives is kept as written; every blank slot is a **default picked here**, marked *(default)* and listed in
"Defaults picked here" at the end. Numbers are held to `TDS_Vessel_Creator/docs/02` and `docs/12` and to the five base
vessels (`docs/scope/16_Base_Vessels.txt`). Routes follow `docs/reports/SCOPE_FIT_PLAN_2026-10-28.md` §8.3, §10 and §11.
Status: **its self-test passes in game (148/0, 2026-11-04), never played by hand**; temporary art drawn by code (`IMPLEMENTATION_NOTES.md`).
**2026-11-05:** the 24 upgrades scope `10` added on 2026-11-04, Light sharing between teammates (decision 18) and Supernova's
SURGE are built and compiled in cloud-check, **not yet run on the PC** (self-test section 8b waits for the next PC round).

## Identity

- **Name ▪ title ▪ difficulty:** Uloe, "Light Personified" ▪ Advanced (sheet).
- **Archetype** *(default)*: bring light to a cluster of towers: plug in beside them, keep Photons drifting among them,
  aim Flood Lights at what matters.
- **In-game line** *(default)*: "Frail, single-target ranged" (the pattern of Sno Sno's line).
- **Look (sheet):** a glass light bulb head with a face for a filament, a ribbed screw base on a tall lamp post, yellow
  floating arms made of electricity at the sides, a cord to a plug. The only tall, thin, legless outline on the boat.
- **Sprite baseline:** Dimitri's concept frame (`docs/concepts/art/uloe.ase`, embedded pixel for pixel in
  `UloeCharacter.cs`), moved and added to, never recoloured. Reference base vessel: Sno Sno (an object come alive; legless,
  it hops; `docs/STYLE.md`). Departure kept from the style sheet: the head is under half its height.
- **Palette (the art's):** glass `DAE6FC` / `CBDBFC`, glints `F1EF72`, steel `9BADB7` to `595652`, arms `FBF236` /
  `D4CD3C`, eyes `3F3F74`, cord `222222`. **Accent** *(default)*: glint yellow `F1EF72` (hero `Color`, buttons, journal).
- **Towers (sheet):** glass, screw rings and cords, low-poly sleek, never voxel: the Magnifying Glass's lens, the Power
  Bank's terminals, the Mirror's panes, the Flood Light's lamp.
- **Lore:** none yet (sheet: "Lore comes once the character is finalized"). The journal shows a placeholder line.

## The vessel

| Stat | Value | Band (docs/02) | Why |
|---|---|---|---|
| Attack | ranged, a spark (stock `Entity_Ai_Mob_Ranged`, armed by the brain like Sno Sno and Yossele) | | sheet |
| Health | 30 *(default)* | 25-65 | ranged low; a little over Sno Sno's 25, Uloe must stand near the fight to plug in |
| Health regen | 0 *(default)* | | no base vessel regenerates |
| Speed | 2.25 *(default)* | 2-3.5 | a lamp post hopping; Sno Sno 2 |
| Damage | 2 *(default)* | 1.5-5 | low: towers and Photons do the damage |
| Range | 3 *(default)* | ranged ~3 | |
| Projectile speed | 15 *(default)* | 15 | |
| Attack speed | 0.5 *(default)* | 0.4-0.75 | |
| Build range | 2.5 *(default)* | 2-3 | |
| Sell rate | 0.5 *(default)* | 0.5-0.75 | |

- **Special rule** *(default)* "Bright Idea", tooltip: "Ranged, towers grow stronger with the Light they receive, Uloe and
  Photons give Light, PLUG IN to a node or Power Bank for far more, Photons Illuminate and Blind foes".

### Plug In (ability)

Sheet: "plugs into a node or a Power Bank and gives off high levels of Light in a radius around them, as long as they stay
in range of what they plugged into. The level of Light depends on the node's total aura or the Power Bank's upgrades."
"The plug and cord are the ability's: they move with Uloe, like a cord in real life."

- **Keys:** 6 / 7, Onpressed, priority 6 (Buster's Dash setup). **Unlocked:** from the start. **Cost:** none *(default)*.
- **Target:** a node (an entity tagged `Resource` with `Healthresource`; dens are tagged `Resource` too and are not nodes) or one of the player's own Power Banks: a picker, like Sell.
  The hotkey takes what is under the cursor, else the nearest valid one in reach *(default)*. The server checks every pick.
- **Range:** 5 tiles *(default)*, the cord's length. Walking past it snaps the cord: Uloe unplugs, with a spark and an
  owner-only line.
- **Light while plugged:** radius 4 *(default)*.
  Node: `2 + 0.4 x total aura`, at most 10 *(default)*: a 5 aura node gives 4, a 15 aura node 8.
  Power Bank: `4 + 2 per upgrade` *(default)*: 4, or 6 with Short Circuit; a bank with Surge Protector gives at most Light 8 (scope `10`:145, `UloeData.SurgeBankMax`), and Uloe plugged into it gives at most Light 8 in all, Brighter Bulb included (Dimitri 2026-11-06, "keep cap at 8").
- **Press again:** unplugs. **Cooldown:** 0.5 s from an unplug (the template's default); the button greys out.
- **Look:** the bulb burns bright while plugged (a skin, so every player sees it); a cord sags from Uloe's foot to a plug
  at the target and moves with Uloe every frame on every machine (Argo's harpoon rope); a ring on the ground at the real
  radius (`docs/02` §6: honest visuals).

## Light

The game has no light for gameplay, so **Light is a stat**. Each of Uloe's buildings has a `Light` stat (0 until lit, shown
on its panel). Each source removes its own last modifier and re-adds a 1.5 s timed `Add` every second (pitfall #119,
RogueLab R3): the stat system does the summing, and Light fades by itself about 1.5 s after a source leaves.

**Sharing** (scope `10` [Light] "Sharing", decision 18, 2026-11-05): a source lights every Uloe building of its team in
reach, and a building takes at most **4** *(default)* Light from teammates' sources all told (`UloeData.TeamLightCap`,
`UloeLight`); its own player's Light is never capped. What a player points stays their own: a Flood Light's AIM and its
default aim take only the owner's buildings, and Photons visit only their owner's (ULO-3).

| Source | Light | Reach | Slot |
|---|---|---|---|
| Uloe | 1 *(default)* | radius 3 *(default)* | sheet |
| Uloe plugged in | see Plug In | radius 4 | sheet |
| Photon | 1 *(default)* | radius 2 *(default)* | sheet |
| Flood Light, aimed | `2 + 0.2 x node's total aura`, bonus at most 3 *(default)*: 3 on a 5 aura node | the one tower it is aimed at, within 6 tiles | sheet |
| Flood Light with Wide Flood | 60% of that *(default)* | radius 3.5 *(default)* | sheet |
| Flood Light with Long Throw | 80% of that *(default)* | every building on a line 8 tiles long, 0.6 tile each side *(default)* | sheet |
| Mirror with Bounce Card | 50% of the Light it receives *(default)* | radius 3 *(default)*; never to another Bounce Card (a loop) | sheet |
| Photon with Bright Spark | 2 | radius 2.5 | scope 10 |
| Photon after SURGE (Supernova) | 3 for 15 s | its radius | scope 10 |
| Flood Light with Stadium Lights | 100%, on top of its aim | radius 5 | scope 10 |
| Flood Light with Lighthouse | 60%, held 6 s | a beam 9 tiles long turning once every 6 s | scope 10 |

What Light does ("Towers have increased effects based on how much light they're receiving"), each counted up to Light 12
*(default)*:

| Building | Effect per Light *(defaults)* |
|---|---|
| Magnifying Glass | +20% Damage (+40% with Polished Lens) |
| Power Bank | Photons come back 10% faster |
| Mirror | sets its strength: Light 1+ level 1, Light 3+ level 2, Light 6+ level 3; at no Light it does nothing |
| Flood Light | mines 10% faster |

**The look:** a skin per Light band (Light above 0, 4 and 8 *(defaults)*): a ring of light, then rays, round the tower's foot,
on the base model and every upgrade's (`docs/STYLE.md`: states show as skins or a counter).

### Illuminate and Blind

Sheet: "Photons can Illuminate enemies, increasing their damage taken. Can also Blind enemies, reducing their damage.
Illuminate and Blind have different levels of strength depending on its source."

| Level *(defaults)* | Blind: foe's Damage | Illuminate: damage the foe takes | Lasts |
|---|---|---|---|
| 1 | -15% | +15% | 3 s |
| 2 | -30% | +30% | 4 s |
| 3 | -45% | +45% | 5 s |

| Source *(defaults)* | Blind | Illuminate |
|---|---|---|
| Photon's hit | 1 | 1 |
| Photon's burst (Short Circuit) | 2 | 2 |
| Lit Mirror, hit by a foe | by its Light (1-3) | |
| Mirror with Silver Backing | by its Light, +1 (at most 3) | |
| Mirror with Vanity Lights | | by its Light (1-3), in place of Blind |
| Uloe with Flash Bulb | rank II: 1; rank III: 2 | rank I, II: 1; rank III: 2 |
| Photon with Static Charge (and its Chain Lightning jumps) | 2 | 2 |
| SURGE (Supernova) | 3 | 3 |
| Glass with Sunspot, its target | | 1 from Light 4, 2 from 8, 3 at 12 |
| Glass with Death Ray at full heat, its target | | 3 |
| Mirror with Hall of Mirrors | the attacker's mark, on every foe within 2 tiles of it | |
| Mirror with Heliostat, the foe it beams | its own mark at its level | |
| Flood Light with Searchlight, foes in its Light | | 1; 2 at Light out 5+ |
| Flood Light with Lighthouse, foes the beam crosses | 2 | 2 |

A foe holds one level of each at a time *(default)*: a stronger one replaces a weaker, a weaker does nothing while a
stronger is on, the same level refreshes in its last second.

**Routes (changed from fit plan §8.3 after the first in-game run).** Blind is `Addmod(foe, "Damage", -x, Multi, seconds)` and
Illuminate a front-of-chain hit hook, the same numbers with no stock scripts, each showing Uloe's own look (`Debuff_Uloe_Blind`,
`Debuff_Uloe_Lit`). The plan's route, the stock `Entity_Ai_Effect_Barrel_Boost` and `Entity_Ai_Effect_Tower_Merchut` on pack
upgrades, worked but logged 2 `OperationCanceledException` for every marked foe that fell; it was deleted on 2026-11-04 (git keeps
it). The cost: a marked foe no longer lists "Blinded" or
"Illuminated" among its upgrades when selected; the looks are what show it.

## Buildables

| Name | Id | Slot | Template | Cost *(default)* | Stats *(defaults)* |
|---|---|---|---|---|---|
| Magnifying Glass | `Tower_Uloeglass` | Offense | Tower_Smackyarcher | Red 30 | Health 30 (+20/level), Damage 2 (+0.5/level), Attack speed 0.75, Range 4.5, limit none |
| Power Bank | `Tower_Uloebank` | Support | Tower_Smackyhut | Red 40 + Yellow 20 | Health 60 (+25/level), Units 2, Respawn 8 s, limit none |
| Mirror | `Tower_Uloemirror` | Defense | Tower_Smackywall | Blue 20 | Health 100 (+40/level), limit none |
| Flood Light | `Tower_Uloeflood` | Economy | Tower_Smackyhut | Yellow 40 | Health 40 (+20/level), mines Damage 1 at 0.3 *(balance sweep 2026-11-05; was 0.4)*, node reach Range 2.5, aim reach 6, Light out 2, limit none |

In-game lines *(defaults)*: Magnifying Glass "Focused, single-target beam, burns brighter in Light"; Power Bank "Makes
Photons that wander and give Light"; Mirror "Sturdy, Blinds what hits it when lit"; Flood Light "Mines a node, aims its
Light at a tower".

- **Magnifying Glass** (sheet: "Focuses beams of light to deal damage. Damage dependent on how much Light it is
  receiving"). Beams the nearest foe in Range with the stock `Projectile_Ai_Beam`; it takes the Damage stat, on which Light
  is a held `Multi`. The lens turns to its target on every screen. Unlit it is weak on purpose (1.5 damage a second against
  the Archer Tower's 2.8; the Archer costs 25); at Light 4 it matches the Archer, at Light 10 it does 4.5.
- **Power Bank** (sheet: "Spawns Photons"). Keeps its Units of Photons, one per Respawn seconds. Its upgrades decide the
  Light Plug In gets from it.
- **Mirror** (sheet: "Wall that can be empowered by Light to blind enemies"). Blocks like Smacky's Wall. A foe whose hit
  lands on a lit Mirror is Blinded, at the level its Light gives, whether the foe is melee or ranged *(default)*.
- **Flood Light** (sheet: "Plugs into nodes and provides light. Can be directed towards towers to focus their light
  benefits"). It must stand within 2.5 tiles of a node: it strikes that node like the Mining Hut's pick (the stock payout)
  and gives Light; with no node in reach it gives none, and says so once *(default)*. **AIM** (a picker on the building,
  keys Quickaction / 5) picks one of your buildings within 6 tiles; until a pick, or when the pick falls, it aims at the
  nearest one *(default)*. A cord runs to the node and a soft ray to what it lights, on every screen.

### Photon (minion, sheet: "similar to Sno Sno", "Can attack enemies for very little damage, but apply illumination and blinding")

| Slot | Value *(defaults)* | Why |
|---|---|---|
| Attack | melee, a small zap (stock `Entity_Ai_Mob_Combat`) | the sheet's "very little damage" |
| Health | 8 | between the miner's 5 and the knight's 25 |
| Damage | 0.5 | Smacky's miner's |
| Range | 1.5 | |
| Attack speed | 0.6 | |
| Speed | 1.75 | under the dog's 2 |
| How many | 2 per Power Bank | docs/12 §7: weak alone, more with upgrades |
| Respawn | 8 s | docs/12: 5-10 |
| Orders | wander, follow light paths | sheet, below |
| Leash | 3.5 tiles from its Power Bank | |
| Cost | free | |

- **Orders (sheet: "wander around aimlessly where they spawn, but will follow light paths from Flood Lights if they're being
  directed").** No game AI uses randomness, so the brain walks them (fit plan §10). Every 1.5 to 4 s a Photon picks a
  walkable tile within its leash, or half the time *(default)* one of your buildings within it, which it goes and stands by
  (Kippy's visit-and-go-on): that is how Photons carry their Light from tower to tower. A Photon that comes within 1.25
  tiles *(default)* of the line from a directed Flood Light to its target walks to the target and wanders there until the
  line goes away. A foe within 2.5 tiles and its leash draws it to fight.
- Photons aren't selectable or sellable, and go with their Power Bank.

## Upgrade trees

Scope `10`'s levels `% 1` to `% 4` are the game's tiers 2 to 5; a building takes one upgrade a level. Eight a building,
32 in all. Dimitri's eight lines (bold) keep his words; Claude wrote the other 24 on 2026-11-04
(`docs/reports/ULOE_UPGRADES_2026-11-04.md` §3) and Dimitri took them with the open decisions on 2026-11-05
(`docs/scope/02_Progress.txt`); built 2026-11-05 by the routes in the report's §6. "All" leads to every choice of
the next level; "ends" leads nowhere. Costs sum per upgrade; params are in `Code/02_Data.cs`.

| Building | Tier | Upgrade | In-game line | What it does | Leads to | Cost |
|---|---|---|---|---|---|---|
| Magnifying Glass | 2 | **Burning Point** | Burns hotter on the same target | +12% Damage per beam on one target, at most +120%; a new target starts over | Polished Lens, Quick Focus | Red 35 |
| | 2 | Wide Lens | Longer reach | Range +1.5 | Quick Focus, Prism | Red 30 |
| | 3 | Polished Lens | Makes more of every Light | +40% Damage per Light in place of +20% | Sunspot | Red 90 |
| | 3 | Quick Focus | Beams faster | attack speed +50% | Sunspot, Focal Point | Red 80 |
| | 3 | Prism | Splits the beam onto nearby foes | 2 more beams at the foes nearest its target, within 1.5 tiles, 50% damage each | Focal Point | Red 100 |
| | 4 | Sunspot | Shows its target up for everyone | its target Illuminated: 1 from Light 4, 2 from 8, 3 at 12 | Death Ray | Red 300 + Blue 50 |
| | 4 | Focal Point | Burns through a whole line | full damage to every foe within 0.5 tiles of the line from the lens out to its Range; with Prism the side beams burn their lines too | ends | Red 350 |
| | 5 | Death Ray | Never cools down | +12% a beam, at most +250%, kept from target to target, lost after 3 s with nothing to beam; at full heat its target is Illuminated 3; replaces Burning Point's ramp | | Red 3000 |
| Power Bank | 2 | **Short Circuit** | Photons burst when they fall | 3 damage within 1.5 tiles, Blind 2 and Illuminate 2 | Static Charge, Surge Protector | Red 40 |
| | 2 | Extra Cell | Holds another Photon | Units +1 | all | Yellow 35 |
| | 3 | Static Charge | Photons zap harder | Photon Damage +100%; its hits mark at level 2 | all | Red 90 |
| | 3 | Bright Spark | Brighter Photons | a Photon gives Light 2 within 2.5 tiles | all | Yellow 90 |
| | 3 | Surge Protector | Shields Uloe while plugged in | plugged into this Bank, Uloe takes 40% less damage and heals 2 a second | ends | Yellow 60 + Blue 40 |
| | 4 | Chain Lightning | Zaps jump between foes | each Photon hit jumps to the 2 foes nearest it within 2 tiles, the Photon's damage and marks (the jumps are the Bank's) | Supernova | Red 350 |
| | 4 | Photon Swarm | A swarm of Photons | Units +2; leash 5 tiles | Supernova | Red 150 + Yellow 200 |
| | 5 | Supernova | Every Photon goes off at once | a SURGE button, once a Tide (greyed until the Wave rises): every Photon of this Bank bursts for 20 within 2 tiles, Blind 3 and Illuminate 3, then flies straight back to the Bank at double speed (6 s at most, Dimitri 2026-11-06); every empty slot fills at once; its Photons give Light 3 for 15 s | | Red 2000 + Yellow 1000 |
| Mirror | 2 | **Silver Backing** | Blinds harder | Blind one level stronger, at most 3 | all | Blue 30 |
| | 2 | **Vanity Lights** | Shows foes up instead | Illuminates what hits it, in place of Blind, at the level its Light gives | all | Blue 30 |
| | 3 | **Glare** | Can shrug off a Blinded foe's hit | a hit from a foe with Blind level N has an N x 20% chance to do nothing | all | Blue 100 |
| | 3 | **Bounce Card** | Passes its Light on to nearby towers | 50% of the Light it receives, to your buildings within 3 tiles; never to another Bounce Card | all | Blue 100 |
| | 3 | Tempered Glass | Toughened panes | Health +200% of its base (a `Pmulti` of 2); 20% less damage from each hit | all | Blue 90 |
| | 4 | Hall of Mirrors | Its flash spreads through the crowd | every foe within 2 tiles of the attacker gets the same mark at the same level | Heliostat | Blue 350 |
| | 4 | Reflection | Hits come back as light | 30% of what each hit does back at the attacker (after Glare and Tempered Glass), 60% from Light 6 | ends | Blue 300 + Red 100 |
| | 5 | Heliostat | Aims its Light at foes | once a second, the nearest foe within 4 tiles takes 1.5 x its Light and its mark at its level; unlit it does nothing | | Blue 2500 + Red 1000 |
| Flood Light | 2 | **Wide Flood** | Lights all around it, can't be aimed | 60% Light, radius 3.5; AIM goes away | all | Yellow 30 |
| | 2 | **Long Throw** | Throws a line of Light at a tower | AIM's line lights every building on it, 80% Light, 8 tiles | all | Yellow 30 |
| | 3 | Halogen Bulb | A brighter lamp | Light out +2 | all | Yellow 90 |
| | 3 | High Voltage | Strikes its node faster | attack speed +60% | all | Yellow 100 |
| | 4 | Colour Filter | Pick the colour of its aura | a COLOUR button (key 4: a Long Throw keeps AIM on Quickaction / 5) cycles All, Red, Blue, Yellow; each payout its strikes cause is traded, for its owner, into the picked colour at 70% of the share's total | ends | Yellow 250 |
| | 4 | Searchlight | Foes in its light are caught out | foes in its Light (its ring, Long Throw's line within 1 tile, 1.5 tiles round its aim, what Lighthouse crossed in the last second) are Illuminated 1, 2 once Light out is 5+, and slowed 20%, one untimed slow a Flood Light that comes off as they leave | all | Yellow 250 + Red 100 |
| | 5 | Stadium Lights | Lights up the whole ground | 100% of its Light within 5 tiles; AIM comes back on top (what it aims at takes its Light twice); attack speed x2; a second node in reach struck too | | Yellow 3000 |
| | 5 | Lighthouse | Sweeps the sea with light | its beam turns a full circle every 6 s, 9 tiles long: your buildings it crosses keep 60% of its Light for 6 s, foes it crosses are Blinded and Illuminated 2; AIM goes away | | Yellow 2500 + Red 500 |

**Routes** (report §6; what the code does that the line leaves open):

- Prism's and Focal Point's extra hits are the Glass's own (`ModKitCombat.HurtWithStat`, so Light and the ramp count);
  with Focal Point the stock beam drawn is a look only (`Interact` 0), so the target is hit once.
- Static Charge is a held `Multi` on each Photon's Damage. Chain Lightning's jumps come from the Bank: a Photon's own
  `Interact` would hear its jumps and jump again.
- Surge Protector is the vessel's hit hook at the front of its chain, on while the cord is in that Bank (`UloeHeroBrain`).
- SURGE keeps "Off" in the command's `Goalstring` until `Game_Mapvars.Wave` rises; Photons that burst stay standing.
- Colour Filter: a node pays inside the strike, split among the active players (`Entity_Ai_Resource_Mine.Oninteract`,
  `Styles.Split`), so the owner's purse read around the strike is their share, after the map's gain. That gain is given
  back as negative amounts through the game's own `Giveresource` (its Gained stat nets out) and 70% of its total paid in
  the picked colour. Teammates keep their shares as paid. **Not yet seen on the PC.**
- Searchlight's slow is one untimed `Speed` `Multi` per foe per Flood Light, never re-added, taken off as the foe leaves
  its Light (`ARGO_REVIEW_2026-11-04.md` §7).
- Lighthouse counts the wedge the beam swept since its last step (five a second), so nothing slips between steps; a
  building is lit again at most once a second. Clients turn the beam themselves from where it was and how long a turn takes.

## Directions

Kit 1.7 form: one upgrade per rank, `Direction_Uloe_<Name><rank>_Upgradeobject`, read with `ModKitDirections.Rank`. All four are
defaults for `docs/scope/11`'s "! NEEDS ULOE'S DIRECTIONS !".

```
{Extension Cord}
	- Kind: Boost
	- Vessel: Uloe
	- Touches: The vessel itself
	- Rank I: Plug In reaches farther.
	- Rank II: Plug In reaches farther still.
	- Rank III: Plug In reaches much farther.
	- Numbers: ~reach +2 / +4 / +6 tiles~
	- Look: An orange extension cord on a reel
	- Clashes: None
{Brighter Bulb}
	- Kind: Boost
	- Vessel: Uloe
	- Touches: The vessel itself
	- Rank I: Uloe gives off more Light.
	- Rank II: Uloe gives off more Light still.
	- Rank III: Uloe gives off much more Light.
	- Numbers: ~Light +1 / +2 / +3, plugged in or not~
	- Look: A frosted light bulb stamped 100
	- Clashes: None
{Spare Batteries}
	- Kind: Boost
	- Vessel: Uloe
	- Touches: {Power Bank}
	- Rank I: Each Power Bank keeps another Photon.
	- Rank II: Photons come back quicker.
	- Rank III: Each Power Bank keeps yet another Photon.
	- Numbers: ~+1 Photon; +1 Photon, respawn -25%; +2 Photons, respawn -25%~
	- Look: Two batteries taped together
	- Clashes: None
{Flash Bulb}
	- Kind: Shift
	- Vessel: Uloe
	- Touches: The vessel itself
	- Rank I: Uloe's sparks Illuminate foes.
	- Rank II: Uloe's sparks also Blind foes.
	- Rank III: Uloe's sparks Illuminate and Blind harder.
	- Numbers: ~rank I Illuminate 1; rank II Illuminate 1, Blind 1; rank III Illuminate 2, Blind 2~
	- Look: An old camera's flash bulb, still smoking
	- Clashes: None
```

## Visual effects and sounds

- Sparks are a yellow bolt with a small flash where they land. The Glass's beam is a bright line from the lens that turns to
  its target; a Photon pops in a burst of motes; a burst Photon throws a ring at its real radius; a lit Mirror flashes at what
  hit it; a nullified hit rings; a snapped cord throws sparks.
- Sounds stay at or under the game's levels (`docs/03`): zap, beam, plug in, unplug, flash, pop, burst, nullify, build.

## Changes from the concept, and why

| Sheet | Uloe 0.1.0-dev | Why |
|---|---|---|
| Photons "wander aimlessly" | half their walks are visits to your buildings | no game AI uses randomness (fit plan §10): the visit gives them a job a player can read, and their Light has to reach towers to matter |
| "Blind or Illuminate based on upgrade path" | Vanity Lights replaces the Mirror's Blind with Illuminate; Silver Backing strengthens Blind | the sheet gives one line for two paths; the Mirror blinds before either |
| "A portion of Light directed to Mirror is added to nearby towers" | never to another Bounce Card | two Bounce Cards would feed each other without end |
| Flood Light in the Economy slot, "Plugs into nodes" | it mines the node it plugs into, like Smacky's Mining Hut | an Economy building must earn; the sheet says what it plugs into and not what it pays |
| "Levels" of Blind and Illuminate | three levels, one held at a time per effect | "different levels of strength depending on its source" and "levels of Blind on opponent" both need a number to read |

## Test plan (self-test sections, docs/08)

1. The vessel: stock ranged AI, armed; tooltip, stats, PLUG IN, keys, the four on the build menu.
2. Light: Uloe lights a Glass; two sources sum; Light fades about 1.5 s after Uloe leaves.
3. Plug In: refusals, a node's Light from its aura, a Power Bank's from its upgrades, the skin, unplug, the snap.
4. Blind and Illuminate, on the route in use (the own route since round 1): each level's numbers, a stronger replacing a weaker, a weaker refused, wearing off.
5. Magnifying Glass: damage by Light, beams, Burning Point's ramp and its reset.
6. Power Bank and Photons: count, respawn, faster with Light, wandering, Light from a Photon, a hit marking a foe, Short Circuit.
7. Mirror: unlit does nothing; level by Light; Silver Backing; Vanity Lights; Glare's chance; Bounce Card.
8. Flood Light: mines; aims; AIM's refusals; no node, no Light; Wide Flood; Long Throw; a Photon following the line.
8b. The 24 upgrades (`Code/98_Upgrades.cs`): the teammate cap; Polished Lens, Sunspot, Death Ray on the first Glass, Wide
   Lens, Prism, Focal Point and Quick Focus on two more; Static Charge, Chain Lightning, Supernova's SURGE on the first
   Bank, Extra Cell, Bright Spark, Photon Swarm, Surge Protector on two more; Hall of Mirrors, Heliostat, Reflection,
   Tempered Glass; Halogen Bulb, Searchlight, Stadium Lights, High Voltage, Colour Filter, and Lighthouse on a third Flood
   Light if a node has room. The extra buildings are sold at the end.
9. Directions: each rank read and acting.
10. Journal: Uloe's pages, the tabs.

## Defaults picked here

Each is Claude's pick where the sheet is silent; Dimitri can change any of them in `docs/scope/10` and the next build takes
it. The number lives in `Code/02_Data.cs` unless the row says otherwise.

| What | Default | Why |
|---|---|---|
| Archetype, in-game line, accent | as above | sheet blank |
| Vessel stats | table above | inside docs/02's bands, next to Sno Sno |
| Special rule | "Bright Idea" | template slot |
| Plug In's cost, range, radius, cooldown | none, 5, 4, 0.5 s | a free, always-on ability that ties Uloe to one spot is its own cost |
| Plug In from a node | `2 + 0.4 x total aura`, at most 10 | "depends on the node's total aura" |
| Plug In from a Bank | `4 + 2 per upgrade` | "depends on the Power Bank's upgrades" |
| The hotkey with nothing under the cursor | the nearest valid in reach | docs/02: a picker's hotkey falls back |
| Uloe's own Light | 1 within 3 tiles | "Uloe provides light around them" |
| A Photon's Light | 1 within 2 tiles | "Photons provide light around them" |
| Light's effects | table above | "increased effects" |
| Light's cap | 12 counted | a line of Flood Lights can't make a tower absurd |
| Light's look | a skin at Light above 0, 4, 8 | docs/STYLE.md |
| Blind and Illuminate levels and times | table above | "different levels of strength" |
| One level held at a time | stronger replaces weaker | the `Multi` bucket would sum them otherwise (pitfall #115) |
| Mirror blinds any foe that hits it | melee or ranged | light reflects at range too |
| All tower stats, costs, names, upgrade numbers | tables above | sheet blank; costs in docs/02's bands by job colour |
| Power Bank's colour | Red + Yellow | a Support building; a hybrid may take two colours; unit producers sit at the top of the band (60) |
| Flood Light with no node in reach | no Light, an owner-only line once | "plugs into nodes" |
| Flood Light with no pick | the nearest of your buildings in reach | it should light something from the moment it stands |
| Light from teammates | at most 4 a building, all teammates together | scope `10` "Sharing" `~4~`, decision 18 |
| Photon's stats, walks | tables above | docs/12 |
| Directions | the four above | `! NEEDS ULOE'S DIRECTIONS !` |

## Questions (for Dimitri, each with the default in use)

1. Upgrade names and levels: as placed, named here. Rename at will.
2. The trees: answered 2026-11-05, 8 upgrades a building as scope `10` has them; Supernova is a button.
3. Is a foe that hits the Mirror from range meant to be Blinded? Default: yes.
4. Should Photons be selectable, with an order of their own? Default: no, the sheet gives them only wander and follow.
