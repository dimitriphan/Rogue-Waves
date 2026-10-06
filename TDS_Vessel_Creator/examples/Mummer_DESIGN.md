# Mummer: design (v1.0.0, 2026-10-02)

**MUMMER, Strings Attached ▪ Advanced.** A hand-sewn costume with a happy face drawn on the front, and something small
in the dark underneath. Built from the user's "Mummer Vessel Concept.zip" (concept page and sprite pack kept in
`Concept/`), on the scaffolding proven by Sprigg and Argo. Shape language: Smacky is the square, Sno Sno the circle,
Mummer the triangle.

## The kit

| | What it does | Cost |
|---|---|---|
| **Mummer** (vessel) | Melee with reach: a shadow arm swipes out from under the hem (Peekaboo). 45 HP, speed 2.75, damage 3.5, range 2, attack speed 0.6 | |
| **HOOK** (keys 6/7) | Pick a foe within 5 tiles: it is reeled up to 2 tiles toward Mummer (walking on its own movement, so its path stays intact), held 0.75 s and **Marked** for 8 s. Recoil 9 s; the button greys out meanwhile. Boss-weight foes (600+ HP) are Marked but not moved | |
| **Loom** | Foes felled within 3.5 tiles leave **Scraps**, spun into **Thread** (1, 3 for big foes, 5 for huge ones). Picks at ore in reach for aura | 40 Yellow |
| **Puppet Stage** | Stitches **Understudies** with Thread, 2 at a time, one every 6 s, leashed 4 tiles from the stage. **COSTUME** button cycles the costume it stitches | 50 Red |
| **Marionette Cross** | Every ~7 s grabs a foe in range 4 for 4 s: it can't move or attack its attackers, flails at its neighbour with 60% of its own damage, and the strings cut it for 2/s. Boss-weight foes are immune | 30 Blue, 15 Red |
| **Curtain** | Walk-through trap (Buster's pad block). Foes passing through are **Tangled**: 35% slow for 2 s and too stage-struck to attack for 1 s | 20 Blue |

**Rule (tooltip upgrade "The Show Must Go On"):** melee with reach; foes felled near a Loom leave Scraps of Thread;
Stages stitch Understudies with Thread in the costumes of foes you've scrapped; half your Thread unravels each Tide
(not on the first); can HOOK foes.

**Thread.** Start 4, cap 99. A Marked foe pays +2 Thread wherever it falls. Unravelling is the reason to spend it.

**Costumes** (unit stats; Thread cost):
- Understudy (plain felt): 24 HP, damage 3, range 1.5, attack speed 0.8, speed 1.6. 2 Thread. Always available.
- Ballista Understudy: 16 HP, damage 3.5, **range 4** (felt bolts, the stock ranged AI). 4 Thread. Unlocked by
  scrapping a ranged foe (Ballista, Onager, Excalibur, Bulwark).
- Aegis Understudy: 70 HP, damage 5, attack speed 0.4, speed 1, on Sno Sno's mob-blocker block, patches itself up
  (3 HP/s after 3 s unhurt, like the Aegis it copies). 5 Thread. Unlocked by scrapping a heavy foe (Aegis, Maul,
  Halberd).

## Upgrade trees (tiers 2 to 5, costs on the base game's curve)

- **Loom** (yellow): Fast Shuttle / Long Weft → Silk (no unravelling) / Ore Thread (a Thread every 3 ore strikes) →
  Patchwork (a free Understudy each Tide, branch ends) / Tapestry (double Scraps) → **Golden Thread** (big foes and
  bosses leave 10 Thread).
- **Puppet Stage** (red): Long Strings (+3 leash, +25% speed) / Quick Change (stitch twice as fast, a new costume
  re-dresses the cast) → Double Bill (+1 Understudy) / Second Cast (half the Thread) → Method Acting (+60% health and
  damage) / Grand Finale (Understudies burst for 8 when they fall, branch ends) → **Standing Ovation** (+2
  Understudies, healing 2/s on stage).
- **Marionette Cross** (blue): Quick Grab / Long Arm → Double Cross (two foes) / Cut Strings (2 s stun on release) →
  Ventriloquist (neighbours turn on the held foe) / Encore (a foe that falls while held becomes a free Understudy,
  branch ends) → **Puppet Master** (holds until the foe falls, bosses 4 s).
- **Curtain** (blue): Velvet (+20% slow) / Spotlight (Marks) → Heavy Drape (+1.5 s) / Trapdoor (Understudies heal 3/s
  nearby) → Smoke and Mirrors (tangled foes attack each other) / Iron Curtain (held 1 s as they pass, branch ends) →
  **Final Act** (once per Tide the curtain drops: every foe within 3 tiles stunned 3 s).

## Changes from the concept, and why

| Concept | Mummer 1.0 | Why |
|---|---|---|
| Puppets copy *any* enemy type seen, at 60% of its HP | Three costumes (plain, Ballista, Aegis) unlocked by scrapping a ranged or heavy foe | Enemy AIs are built to walk to the Basin; dressing a unit in an exact enemy copy would hand it AI that fights the player. Three readable roles (brawler, archer, tank) carry the idea and stay balanceable |
| Strings that enemies can cut, leaving puppets limp | A leash: Understudies stay within 4 tiles of their Stage (7 with Long Strings) | No per-puppet string entity exists to attack; a leash keeps the positional play without an invisible fail state |
| Marionette Cross: full control for 6 s, 1-3 s to grab | Holds for 4 s every ~7 s; the held foe flails at its neighbour with its own damage | Switching a foe's team breaks its pathing and the Tide's enemy count. The flail keeps "turns it on the Tide" honest: big foes hit their friends hard |
| Curtain: blocks enemy targeting and Ballista sightlines | Walk-through trap that tangles: slow + no attacks for a moment | Sightlines aren't simulated, so blocking them couldn't be shown or tested. The tangle does the same job (protect what's behind it) and reads on screen |
| Iron Curtain: blocks projectiles | Iron Curtain: holds foes 1 s as they pass | Same reason |
| Hook reels 2 tiles, cooldown 8 s | Reel up to 2 tiles via the foe's own movement, 9 s recoil, heavy foes Marked only | Argo 1.0 avoided pushing enemies to protect pathing; walking the foe with its own movement keeps the path valid (tested: it walks on afterwards). Bosses can't be dragged |
| Half the Thread unravels every Tide | Same, except when the first Tide arrives | Otherwise the starting Thread halves before the first Stage is built |
| Puppet costs 3 / 9 / 15 Thread | 2 / 4 / 5 | Understudies die often and Thread unravels; the concept's prices left a Stage idle most of the run |
| Golden Thread: Golden Scraps from Aegis and bosses | Big foes (40+ HP) and bosses leave 10 Thread | Same intent, works for every heavy foe family |
| Scarecrow costume (free decoy each Tide) | Patchwork sews a free Understudy at a nearby Stage | A static decoy only works if foes target it, which the game decides; a free Understudy is the same gift and always works |
