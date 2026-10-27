# Yossele: design (0.2.0-dev, the 2026-10-20 sheet, 2026-10-23)

Rogue Waves' Yossele is **designed from the scope text** (`docs/scope/07_Vessel_Yossele.txt`, Dimitri's 2026-10-20
sheet with his 2026-10-21 answers), checked against `TDS_Vessel_Creator/docs/02`. Where the sheet gives a number it is
used as written; a value written `~like this~` on the sheet is a default Claude picked, and every default this page adds
is listed under "Changes from the concept". Status: **the 2026-10-12 build (PC round 8, 98/0) brought up to this sheet,
compiled only** (`IMPLEMENTATION_NOTES.md`, "Build of 2026-10-23"); nothing below that is new has run in game yet.
Yossele is Release 1, alone (`docs/PLAN.md` Part B): it works in a normal game, without the mode.

## Identity

- **Name ▪ title ▪ difficulty:** Yossele, "The Patient Golem" ▪ Intermediate (scope).
- **One-line fantasy:** keep your buildings alive and they grow into something formidable; Yossele holds the line
  meanwhile with a green laser from its mouth.
- **Silhouette:** a round clay head with a see-through hole for a mouth, a two-leaf sprout, glowing green eyes, four
  long root-like limbs (`docs/STYLE.md`). The hole is its mouth (scope), and the laser comes out of it.
- **Sprite baseline:** Dimitri's reshade, `concepts/vessels_2026-10-09/v3_flat/yossele_reshade` (flat finish: opaque,
  one shade band down the right edge, cast shadow, no half-alpha rim). Every 32 px frame is his pixels moved, squashed
  or added to, never redrawn. **Temporary:** the release waits for his own sprite sheet (Decisions 2026-10-21).
- **Palette:** clay `864B41` to `733E35` (the reshade's `945743`), leaf `72CC35` / `6ABE30`, eyes `99E550`, collar
  `323C39`. Accent (hero `Color`, buttons, journal): leaf green `72CC35`.
- **Towers:** low-poly sleek, never voxel. Motif on the top and front faces: round holes and root feet in clay, with
  moss and leaves. Each **Growth** stage is a **skin** that adds sprouts round the tower's foot, never a motion.
- **Lore:** none yet (scope: "Lore comes once every character is fully realized"). The journal shows a placeholder.

## The vessel

| Stat | Value | Band (docs/02) | Note |
|---|---|---|---|
| Attack | ranged: a green laser bolt from its mouth (`Entity_Ai_Mob_Ranged`) | | sheet |
| Health | 70 | 25-65 | sheet: "more than Smacky's 50"; above the band on purpose (the sturdy vessel) |
| Speed | 1.75 | 2-3.5 | sheet: slower than the usual 2.5; below the band on purpose |
| Damage | 5.5 | 1.5-5 | sheet: "more than Smacky's 4"; above the band on purpose |
| Range | 4 | ranged ~3 | sheet |
| Projspeed | 15 | 15 | sheet's default |
| Attackspeed | 0.5 | 0.4-0.75 | sheet |
| Buildrange | 2.5 | 2-3 | sheet |
| Sellrate | 0.5 | 0.5-0.75 | sheet |
| Regen | 1 health a second, only while hurt | (no base vessel regenerates) | sheet; the brain heals itself |

- **Display_Desc:** "Sturdy, single-target ranged that mends itself" (sheet's default line).
- **Special rule** ("Patient Growth", tooltip): "Sturdy fighter that mends itself, buildings grow a stage for every
  Tide they survive and lose it all when destroyed, a Direction can unlock FERTILIZE" (sheet).
- **The laser** (read from the game's code, 2026-10-23): the stock ranged AI fires the projectile named in its `Attack`
  command's `Goalstring` with the shot's `Speed`, `Accel` and `Zvelocity` from `Projspeed` and the target as `Goal`
  (Entity_Ai_Mob_Ranged.cs). The projectile is a clone of Sno Sno's homing `Hero_Snosno_Snoball` (the kit's "homing
  shot for any ranged hero", `docs/17` §10), its look swapped for a green bolt (`Yossele_Laser`) and its end look for the
  Archer arrow's. Its hit carries no `Damage`, so the game fills in Yossele's live `Damage` (Helper_Entitytools.cs:166);
  the hit is tagged `Yosselelaser` (the AI's `Hurtdict`). The stock AI also mines ore the way Sno Sno does. The AI sends
  Vis "Shoot", which plays the Attack animation (Entity_Vis_Mob_Attack.cs) and Yossele's zap sound. Without Sno Sno's
  snowball the Archer's arrow is the template.
- **Growth** (scope): every Yossele building carries a **Growth** counter (`ModKitCounter`, shown in its panel) and a
  matching skin. A building gains Growth when a Tide begins if it stood through the whole Tide before it (built before
  that Tide started); a building built during Tide N first grows when Tide N+2 begins. **1 stage a Tide, 5 on a map with
  4 Tides or fewer** (sheet; the map's count is its mode's `Totalwaves`, Entity_Ai_Artifact_Coin.cs:47). Each building
  has its own maximum (5, 15, 8, 5). Destroyed or sold means gone: a rebuilt building starts at 0.
- **FERTILIZE** (scope ability, unchanged): a picker on Buster's Dash keys (6/7): click one of your own Yossele buildings
  and it spends 1 fertilizer for +1 Growth at once (refused at the maximum); the hotkey with nothing picked takes the
  nearest of yours that can still grow within 6 tiles; 0.5 s guard. The button doesn't exist until a Direction unlocks
  it, so **Release 1 has no FERTILIZE** (no Directions outside the mode; Dimitri 2026-10-21).
- **Fertilizer** (unchanged): per player on the host, cap 10, kept through Yossele's deaths, **reset each map** (Dimitri
  2026-10-21).
- **Compost Bin** (`docs/scope/11`): three rank Directions as before; only the mode or the self-test grants them.

## Buildables (four, and the Bonsai)

| # | Name | Id | Template | Cost | Stats (+per level) | What it does |
|---|---|---|---|---|---|---|
| 1 | Mushroom Cannon | `Tower_Yosselecannon` | Bomb Tower | Red 15 | Health 20+25, Damage 1+0.25, Attackspeed 0.4, Range 9, Radius 1, Spores 2 | Lobs a spore bomb in an arc: impact damage in Radius and 2 spore stacks on every foe hit. Each stack deals 0.3 a second (a tick every 0.5 s); every stack falls off 6 s after the last new one; max 10. A fully spored foe wears the toadstool look |
| 2 | Primordial Dandelion Tree | `Tower_Yosseledandelion` | Archer Tower | Red 50 | Health 50+25, Damage 2+0.25, Range 7, Seedlimit 10, Seedrate 0.5 | **Limit 1.** Grows a seed every 2 s up to its limit, **full again at the start of each Tide**; when foes come in range every ready seed strikes at once, shared out so each foe gets enough to fell it (nearest first) |
| 3 | Overgrown Stonehenge | `Tower_Yosselehenge` | Mining Hut (orientable) | Blue 25 | Health 80+40, Slow 0.15 | Directional stones turned while placing (Sno Sno's facing arrows); vines creep forward over the ground (1 tile, +1 every 2 Growth stages), over building tiles too; foes on vines are slowed 15% |
| 4 | Pachira Sapling | `Tower_Yosselepachira` | Mining Hut | Yellow 35 | Health 30+20, Damage 1, Attackspeed 0.25, Range 2.5 | Its roots strike every ore node in Range at once; fully grown, each payout it brings about drops an **aura fruit** |
| - | Bonsai | `Tower_Yosselebonsai` | Archer Tower | Red 50 (the Tree's) | 40% of the Tree's: Health 20+10, Damage 0.8+0.1, Range 2.8, Seedlimit 4, Seedrate 0.2 | Only with the Tree's **Bonsai** upgrade; limit 1; a copy of your Tree at 40% (below); sells for nothing |

Growth per building (sheet):

| Building | Max | Per stage | Full Growth |
|---|---|---|---|
| Mushroom Cannon | 5 | attack speed +10%, blast radius +0.1 | spores **bounce once**: a second blast at the same spot 1 s later, half radius, half damage, 1 stack |
| Dandelion Tree | 15 | seed limit +3, range +20% of its base | nothing extra |
| Overgrown Stonehenge | 8 | damage taken -10%; vines +1 tile every 2 stages | nothing extra |
| Pachira Sapling | 5 | mining speed +10%, range +0.4 | aura fruit |
| Bonsai | 15 | seed limit +1.2 (40% of 3), range +20% of its base | nothing extra |

- **Aura fruit:** worth `Attackspeed x Damage x 15` of every aura colour, from the Sapling's live stats when it drops
  (sheet; 0.375 x 1 x 15 = 5.6, paid as 6, for a fully grown Sapling with no upgrades). Any player's vessel or minion
  within 0.75 tiles picks it up. **Fixed 2026-10-23:** the game's ore nodes never run out; at 0 a node refills itself and
  pays out on the spot (Entity_Ai_Resource_Mine.cs). The 2026-10-12 build dropped fruit when a node despawned, which a
  normal node never does, so in play it never dropped one (its self-test called the drop by hand). The Sapling now
  checks each strike: when the node's `Healthresource` was at or below what the strike takes, that strike made the
  payout, and the fruit drops on the free ground tile beside the node nearest the Sapling (the node's own tile can't be
  walked on). A one-shot node that despawns counts the same way.
- **Vines** are a flat pixel texture on each tile, drawn by networked ground beings (`Effect_Yosselevine`, cloned from
  Sno Sno's snow without its tag or AI); "forwards if possible": the line stops at a cliff or a gap.

Tower motion (`docs/03` §4a): the Mushroom Cannon's cap aims at each lob; the Dandelion Tree's and the Bonsai's ready
seeds float round the crown; the Stonehenge faces its placement direction; the Sapling stays still.

## Upgrade trees

The sheet gives four levels (`1:` to `4:`). They map to the game's tiers 2 to 5; level 4 is the tier-5 capstone. Every
choice of a level leads to every choice of the next, except where the sheet writes **Leads to** (the Tree's level 1).

```
Mushroom Cannon:  Sticky Spores2 / Farout Fungus2 -> Viral Outbreak3 / Deathcap Training3 -> Explosive Finish4 / Hallucinogen4 -> Cordyceps5
Dandelion Tree:   Seed Packets2 -> Picky Placement3 / Garden Weeds3
                  Demand and Supply2 -> D-MDS3 / Picky Placement3 / Garden Weeds3
                  (any level 2) -> Bonsai4 / Rubbery Shell4 -> Dandelion Storm5
Stonehenge:       Verdant Spread2 / Vine Blockade2 -> Natural Remedy3 / Opportunistic Carnivore3 / Easy Target3 -> Entangling Roots4 / Obsidian4 -> Monument5
Pachira Sapling:  Focused Roots2 / Sustaining Practices2 -> Grafted3 / Forest Project3 / Spontaneous Mulch3 -> Connected Roots4 / Harvesting Season4 -> Fortifying Roots5 / Softening Roots5
```

| Building | Upgrade (key) | In-game line | Params | Cost |
|---|---|---|---|---|
| Cannon | Sticky Spores (Sticky2) | Fully spored foes are slowed | Sticky_Slow 0.25 | Red 50 |
| Cannon | Farout Fungus (Farout2) | Spore bombs increase in potency while in the air | Range_Add 2; Farout_Per 0.5 (radius +50% a second of flight, up to +75%) | Red 45, Blue 10 |
| Cannon | Viral Outbreak (Viral3) | Fully spored foes may burst when felled | 30%: 15 damage within 2 tiles, 1 stack | Red 90 |
| Cannon | Deathcap Training (Deathcap3) | Deadlier spores, fewer of them | spore damage +25%; it stacks a foe to 8 at most, and a foe already above 8 keeps its stacks | Red 100 |
| Cannon | Explosive Finish (Finish4) | Full spores burst, spreading the rot | 1 a stack consumed to every foe within 1.25 tiles, 2 stacks to the others, no second burst for 1 s | Red 420 |
| Cannon | Hallucinogen (Hallucinogen4) | Fully spored foes are stunned momentarily | every 5 s at full stacks, 20%: stunned 2 s | Red 360, Blue 280 |
| Cannon | Cordyceps (Cordyceps5) | Once a foe, now a friend! | a foe that falls with spores rises as a Cordyfriend: 20% + 5% a stack | Red 900, Blue 1250 |
| Tree | Seed Packets (Packets2) | Hardened seeds that come back after felling a foe. | 2 seeds back after a seed's kill; Damage +3 | Red 40 |
| Tree | Demand and Supply (Demand2) | Grows seeds faster when few are ready | twice as fast while fewer than a quarter of the limit are ready | Red 20, Yellow 40 |
| Tree | Picky Placement (Picky3) | Seeds go for the toughest foes and bite deeper | toughest first; each seed also deals 0.25% of the foe's max health | Red 100 |
| Tree | Garden Weeds (Weeds3) | Chance to recharge seeds quicker when enemies recently damaged die. | a foe that falls within 3 s of a seed hit: 40% for a stack (+10% seed growth, 15 s), at most one a Growth stage | Red 90, Yellow 30 |
| Tree | D-MDS (Dmds3) | Can no longer damage foes, but now intercepts enemy projectiles. | seeds stop striking foes and shoot down foes' projectiles in Range instead, 1 seed each (5 for a boss's); seed growth -50% (1 every 4 s) | Blue 150, Yellow 40 |
| Tree | Bonsai (Bonsai4) | Plant a copy of the Primordial Dandelion Tree with reduced effects. | unlocks the Bonsai: limit 1, the Tree's cost, 40%, sells for 0% | Red 300, Blue 300, Yellow 300 |
| Tree | Rubbery Shell (Rubbery4) | Seeds bounce between targets. | a strike bounces on twice, each time to the nearest foe within 2 tiles it hasn't hit; with D-MDS a seed goes on to 2 more projectiles within 2 tiles | Red 450, Blue 600 |
| Tree | Dandelion Storm (Storm5) | Huge range, many more seeds, grown faster | Range +100%; seed limit +20; seed growth +100% | Red 1050, Blue 800, Yellow 2400 |
| Stonehenge | Verdant Spread (Verdant2) | Vines spread all around | every ground tile within 2 steps, 3 at full Growth | Blue 30 |
| Stonehenge | Vine Blockade (Blockade2) | A vine barrier each Tide, sturdier stones | max health +50%; a barrier of 30% of it each Tide | Blue 35 |
| Stonehenge | Natural Remedy (Remedy3) | Vines mend buildings | 0.5 health a second per Growth stage to every building on its vines | Blue 90 |
| Stonehenge | Opportunistic Carnivore (Carnivore3) | Vines drain foes to mend the stones | 5 a second from each of up to 5 foes on its vines | Blue 80, Red 30 |
| Stonehenge | Easy Target (Easy3) | Vines eventually cause foes to take more damage | a foe 5 s on its vines takes +15% damage from everything | Blue 40, Red 75 |
| Stonehenge | Entangling Roots (Roots4) | Lingering foes are snared and sapped | a foe 7 s on its vines is stunned 1.5 s and loses 5% of its max health, which mends the stones; once per foe | Blue 420, Red 300 |
| Stonehenge | Obsidian (Obsidian4) | Massively increases health | max health +200% | Blue 600 |
| Stonehenge | Monument (Monument5) | Withstand the test of time | 20% less damage taken; foes' debuffs are stripped at once | Blue 2000, Yellow 1500 |
| Sapling | Focused Roots (Focused2) | Mines one node much faster | x1.8 mining speed on its nearest node only | Yellow 30 |
| Sapling | Sustaining Practices (Sustaining2) | Mends itself from the ore around it | 0.5 a second per node in range per Growth stage (stage 1 before it has grown) | Yellow 25, Blue 10 |
| Sapling | Grafted (Grafted3) | Pick the colour of its fruit | a COLOUR button; one colour pays 70% of the fruit's total in it | Red 60, Blue 60, Yellow 60 |
| Sapling | Forest Project (Forest3) | Mines faster for each other Sapling nearby | +10% a Sapling within 4 tiles, anyone's, no cap | Yellow 140 |
| Sapling | Spontaneous Mulch (Mulch3) | Foes dying within range increase mining speed | +20% for 5 s, a new death refreshes it, no stacking | Red 40, Yellow 30 |
| Sapling | Connected Roots (Connected4) | Takes hits for buildings nearby while healthy | above 50% health it takes 65% of each hit on its owner's buildings in its Range (other Saplings left out) | Yellow 120, Blue 300 |
| Sapling | Harvesting Season (Harvest4) | Fruit is worth double | fruit +100% | Yellow 400 |
| Sapling | Fortifying Roots (Fortifying5) | Increases health and grants a damage reduction aura to nearby buildings. | max health +100%; its team's buildings in its Range and itself take 30% less damage | Blue 2300, Yellow 1000 |
| Sapling | Softening Roots (Softening5) | Softens the ore around it | every node in its Range takes one hit fewer to pay out, never fewer than 1 | Yellow 2300, Red 1000 |

How the new rules work (read from the game's code, 2026-10-23; untested):

- **D-MDS.** Projectiles are not networked entities: they are pooled GameObjects in `Game_Entity.Instance.Projectiles`
  (one list per projectile name), each a `Projectile_Master` with its shooter in `Master`; only the host's copy deals
  damage (`Ishost`), clients get a look-only copy through `Projectile_ClientRpc` (Helper_Damagetools.cs:204-380,
  Projectile_Master.cs:388). The Tree's brain scans the host's pools: an active projectile whose shooter is a foe
  (tagged `Enemy`, not a core), whose interaction is `Hurt` and which is within the Tree's Range is shot down with
  `Kill(false)`: silently, because `Kill(true)` runs its death handlers and the bombs' explosion lives there
  (Projectile_Ai_Explosion.cs: `Despawn`). A Vis tells every client to put out its own copy (the nearest active one of
  the same shooter) and draws the seed. **What counts:** every stock foe shot: the Ballista's and the Excalibur's
  bolts, the Onager's lobs, the Bulwark's bomb rain and the Claymore launching itself. **Bosses** (a shooter of base
  health 600 or more, the kit's rule; the stock Bulwark has 30000) cost 5 seeds a projectile (Dimitri's open question
  on the sheet, answered with a default).
- **Cordyfriend.** When a spored foe falls (the host's despawn hook) and its last cannon has Cordyceps, a roll of 20%
  + 5% a stack raises a Cordyfriend on the foe's tile, a networked unit owned by the cannon's player (spawned as the
  cannon's child, `docs/05` §10). Stats from the sheet: Health 75, Damage 2, Range 1.5, Attackspeed 0.5, Speed 2.25,
  melee (the stock `Entity_Ai_Mob_Combat`). It stands on Sno Sno's mob-blocker block (the Snoble's, as Mummer's Aegis
  Understudy does) so foes stop at it ("stalling foes"), and looks like a Snoble: it borrows the Snoble's own sprite,
  tinted fungus green on every client (temporary art; the Snoble's sheet has only an Idle animation, so it glides when
  it walks). Never tagged `Snoble` or `Sno` (`docs/17`). Its brain walks it to the nearest foe within 4 tiles that is
  within 5 tiles of where it rose, else back there.
- **Monument's "no debuffs from foes".** The only stock foe debuff on a building is the Bulwark's stun: on each of its
  hits it adds `Override` modifiers to the target's `Damage`, `Speed` and `Attackspeed` and the `Debuff_Disabled` look
  (Entity_Ai_Enemy_Bulwark.cs:32-40). The attacker's `Interact` runs before the target's `Oninteract`
  (Helper_Entitytools.cs:154-181), so Yossele's front hit hook on a Monument strips any `Override` or negative
  `Multi`/`Pmulti` modifier the moment a foe's hit lands, and a sweep does the same ten times a second for debuffs that
  don't come with a hit.
- **Bonsai.** It is the Tree's brain at 40%: its own stats are the Tree's at 40%, and it copies the upgrades of your
  standing Tree every second, each upgrade's positive numbers at 40% (counts rounded, at least 1; distances, durations
  and the D-MDS seed penalty unchanged). With no Tree standing it keeps the last copy. It has its own Growth (a copy grows
  like the Tree). It has no upgrades of its own; the panel shows none. Its button takes the Tree's build slot: the slot
  shows the Tree while you have none, the Bonsai while your Tree has the upgrade and no Bonsai stands, and nothing
  otherwise (no fifth slot needed). Sells for 0%: its sell values are set to 0 when it spawns (the Bottom Rock
  artifact's way, Entity_Ai_Artifact_Bottomrock.cs).
- **Softening Roots.** A node's `Healthresource` counts its hits before the paying one (kit `docs/16` §4). The Sapling
  puts one `Add -1` modifier on each node in its Range whose maximum is 2 or more, and takes them off when it goes.
- **Damage reduction stacks by adding** (the engine's percentages add): Stonehenge Growth, Monument and Fortifying Roots'
  aura add up, capped at 90%.
- **Easy Target** puts Yossele's front hit hook on the foe: every hit on it is scaled by 1.15 while it is marked (the
  mark lasts 1 s past its last vine tick), with its own `Debuff_Yossele_Marked` look (red and gold sparks over the
  head; the stock `Debuff_Weakened` isn't loaded in a normal game, PC round 9).
- **Hallucinogen** is now a plain stun (the sheet's 2026-10-20 rule; the old "attacks its friends" is gone), with
  Yossele's own `Debuff_Yossele_Daze` look (the stock `Debuff_Stun` isn't loaded on every map; play test 2026-10-26).

## Visual effects and sounds (temporary art, all generated)

As in the 2026-10-12 build, plus: the laser bolt `Yossele_Laser` (a short lime bolt) and its sound `Yossele_Zap`; an
Attack animation where the mouth hole glows dim, then bright, and flares as the shot leaves (the slam is gone, Yossele
no longer fights in melee); models for every new upgrade (Cordyceps5, Dmds3, Bonsai4, Rubbery4, Storm5, Easy3, Roots4,
Obsidian4, Monument5, Forest3, Mulch3, Connected4, Fortifying5, Softening5) and the Bonsai's own
(`Tower_Yosselebonsai`, the Tree at 60% in a bonsai dish); Growth skins 11-15 reuse stage 10's sprouts. The
Cordyfriend borrows the Snoble's sprite, tinted. D-MDS's intercept draws a seed flying to the projectile and the
seed-hit puff where it goes out. Natural Remedy and Sustaining Practices now show `World_Yossele_Mend` and play
`Yossele_Mend` when they heal (at most every 1.5 or 2 s per building). `Yossele_Landing` and `Yossele_Footstep` are
gone (the game's own sounds play). Renders and notes: `Previews/0.2/`.

## Changes from the concept, and why

| Concept (scope) | Yossele (this build) | Why |
|---|---|---|
| Four upgrade levels | Tiers 2-5; level 4 is tier 5 | The game's tiers; the panel shows up to four options |
| Hallucinogen, Explosive Finish, Viral and the rest | As written | |
| Fertilizer unlocked by a Direction | Compost Bin as three rank Directions, granted by the mode or the self-test only | Ranks are Phase 5; Release 1 has no Directions (Dimitri 2026-10-21) |
| Dandelion Season, Mother of Mushrooms (`11_Directions.txt`) | Not built | They come with the Direction catalogue (Phase 5/6) |
| "Growth rises with each tide a tower survives" | A stage when a Tide begins, for buildings that stood through the whole Tide before | Makes "survived" exact; the game has no break event yet (`ModKitTides`) |
| Aura fruit "after fully mining a node" | Each payout a fully grown Sapling's strike brings about, on the free tile beside the node | The game's nodes refill instead of running out (above) |
| D-MDS: "what counts as projectiles, bosses?" (Dimitri's note) | Every foe projectile that hurts; a boss's costs 5 seeds | Default; the stock list is short (above) |
| Cordyfriend "one for each foe that rises, no cap" (Claude's default) | Up to 30 at once per player | A safety limit on networked units in long games; the sheet's default changed to match |
| Cordyfriend's orders and leash (blank) | Walks to the nearest foe within 4 tiles that is within 5 of where it rose, else back there | Default (`docs/12` §4 "guard a spot") |
| Cordyfriend "resembles a Snoble" | The Snoble's own sprite, tinted fungus green | Temporary art until Dimitri's; Idle only, so it glides |
| Bonsai "with the same upgrades as the Tree, reduced effects" | It copies your Tree's upgrades live at 40% (penalties, distances, durations kept) and keeps the last copy if the Tree goes | Default reading of "a copy" |
| Natural Remedy "0.5 per [Growth]" | Stage 1 before it has grown, like Sustaining Practices | Default: otherwise it does nothing until the first stage |
| Easy Target's debuff length (unsaid) | While on the vines, and 1 s after | Default |
| Softening Roots' line and cost (blank) | "Softens the ore around it"; Yellow 2300, Red 1000 | Default, priced beside Fortifying Roots |
| Stonehenge "directional wall like Sno Sno" | A one-tile orientable building with the Ice Wall's two facing arrows | The game's placement turn is the native way |

## Test plan (self-test, `docs/08`, `99_SelfTest.cs`)

The 2026-10-12 sections stay, retuned to the sheet's numbers, and these are added (the full list of what is still
unchecked, in game and by eye, is `docs/reports/YOSSELE_TEST_PLAN_2026-10-23.md`):

1. Vessel: ranged AI armed with the laser, stats 70 / 1.75 / 5.5 / 4 / Projspeed 15; the laser hurts a foe for 5.5.
2. Growth: 5 stages a Tide on a map with 4 Tides or fewer (the test forces the count); the Tree is full at a Tide's start.
3. Cannon: Cordyceps raises a Cordyfriend from a spored foe's fall; Hallucinogen stuns; Viral's burst numbers.
4. Tree: Demand's quarter rule; D-MDS shoots down a foe's projectile and spends a seed; Rubbery Shell bounces; the
   Bonsai's slot, limit, 40% stats, copied upgrades and 0 sell value; Garden Weeds' window, stack cap and seed growth.
5. Stonehenge: Easy Target's +15%; Entangling Roots once per foe; Obsidian's health; Monument strips a stun.
6. Sapling: fruit from a real payout of a stock node; the fruit's value formula; Forest Project with no cap; Spontaneous
   Mulch; Fortifying Roots' aura; Softening Roots' minus one hit.
