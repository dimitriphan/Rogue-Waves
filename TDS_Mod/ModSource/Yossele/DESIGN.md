# Yossele: design (v0.1.0-dev, 2026-10-12)

Rogue Waves' Yossele is **designed from the scope text** (`docs/scope/07_Vessel_Yossele.txt`, Dimitri's 2026-10-12
edit), checked against `TDS_Vessel_Creator/docs/02`. Where the scope gives a number it is used as written; every number
it leaves open is a **default** picked here and listed in `docs/YOSSELE_BUILD_NOTES.md` (Dimitri asked for that list).
Status: **built (0.1.0-dev, temporary art) and passed its in-game self-test, PC round 8, 98/0**
(`IMPLEMENTATION_NOTES.md`). This page describes that build. The scope's 2026-10-13 edit is not in it yet
(`docs/PLAN.md` Part C): a ranged green laser from the mouth (Range ~3, projectile speed ~15) and Speed ~2.25 in place
of the melee attack, Range 1.5 and Speed 2.5 below; Selfish Sapling renamed Forest Project, Grafted Colors renamed
Grafted, Entangling Spaghetti Roots renamed Entangling Roots (same rules and numbers).

## Identity

- **Name ▪ title ▪ difficulty:** Yossele, "The Patient Golem" ▪ Intermediate (scope).
- **One-line fantasy:** keep your buildings alive and they grow into something formidable; Yossele holds the line
  meanwhile.
- **Silhouette:** a round clay head with a see-through hole for a mouth, a two-leaf sprout, glowing green eyes, four
  long root-like limbs (taken shape, `docs/STYLE.md`). The hole is its mouth (scope).
- **Sprite baseline:** Dimitri's reshade, `concepts/vessels_2026-10-09/v3_flat/yossele_reshade` (flat finish: opaque,
  one shade band down the right edge, cast shadow, no half-alpha rim). Every 32 px frame is his pixels moved, squashed
  or added to, never redrawn (the 16 px boat mini is drawn new at 10 x 13). Back views start from
  `concepts/vessels_2026-10-09/back/yossele_back`.
- **Palette:** clay `864B41` to `733E35` (the reshade's `945743`), leaf `72CC35` / `6ABE30`, eyes `99E550`, collar
  `323C39`. Accent (hero `Color`, buttons, journal): leaf green `72CC35`.
- **Towers:** low-poly sleek, never voxel. Motif on the top and front faces: round holes and root feet in clay, with
  moss and leaves. Each **Growth** stage is a **skin** that adds sprouts round the tower's foot (`docs/STYLE.md`), never
  a motion.
- **Lore:** none yet (scope: "Lore comes once every character is fully realized"). The journal shows a placeholder
  line, listed in the build notes.

## The vessel

| Stat | Value | Band (docs/02) | Note |
|---|---|---|---|
| Attack | melee (Mob_Combat) | | the scope's 2026-10-12 edit (now a ranged laser, see Status) |
| Health | 60 | 25-65 | "more health": Smacky 50, Argo 65 |
| Speed | 2.5 | 2-3.5 | default |
| Damage | 4.5 | 1.5-5 | "more damage": Smacky 4 |
| Range | 1.5 | 1.5-2 | default |
| Attackspeed | 0.5 | 0.4-0.75 | default |
| Buildrange | 2.5 | 2-3 | default |
| Sellrate | 0.5 | 0.5-0.75 | default |
| Regen | 1 health a second | (no base vessel regenerates) | "more health regen"; the brain heals itself |

- **Display_Desc:** "Sturdy, single-target melee that mends itself".
- **Special rule** ("Patient Growth", tooltip): "Sturdy melee that mends itself, buildings grow a stage for every Tide
  they survive and lose it all when destroyed, a Direction can unlock FERTILIZE".
- **Growth** (scope): every Yossele building carries a **Growth** counter (`ModKitCounter`, shown in its panel) and a
  matching skin. A building gains one stage when a Tide begins if it stood through the whole Tide before it (built
  before that Tide started); a building built during Tide N first grows when Tide N+2 begins. Each building has its
  own maximum (scope: 5, 10, 8, 5). Destroyed means gone: a rebuilt building starts at 0. A sold building is gone too.
- **FERTILIZE** (scope ability): a picker on Buster's Dash keys (6/7, Onpressed, priority 6): press, click one of your
  own Yossele buildings, and it spends 1 fertilizer for +1 Growth at once (refused at the building's maximum). The
  button doesn't exist until a Direction unlocks it (scope); the hotkey falls back to the building under the cursor,
  else the nearest of yours that can still grow within 6 tiles. No cooldown beyond a 0.5 s guard: fertilizer is the
  limit.
- **Fertilizer** (scope resource): a counter on the vessel, per player on the host, shown only once unlocked. Cap 10.
  Kept through Yossele's deaths; starts at 0 each map (the campaign save does not carry it yet).
- **Compost Bin** (`docs/scope/11`, the first fertilizer Direction): registered as three Directions, one per rank
  (`Direction_Yossele_Compostbin`, `_Compostbin2`, `_Compostbin3`), since kit 1.6 has no ranks. Held on the core (kit
  1.6) or on the vessel (kit 1.7, ready): rank I unlocks FERTILIZE and gives 1 fertilizer per 10 foes felled within 4
  tiles of Yossele; rank II per 6 within 4; rank III per 6 within 7. Felled by anyone counts.

## Buildables (four)

| # | Name | Id | Role tag | Template | Cost | Stats (+per level) | What it does |
|---|---|---|---|---|---|---|---|
| 1 | Mushroom Cannon | `Tower_Yosselecannon` | Towerfighter | Bomb Tower | Red 35 | Health 35+25, Damage 1.5+0.5, Attackspeed 0.3, Range 6, Radius 1, Spores 2 | Lobs a spore bomb in an arc (Smacky's Bomb Tower's lob): impact damage in Radius, and 2 spore stacks on every foe hit. Each stack deals 0.3 a second; stacks fall off 4 s after the last new one; max 10 (scope) |
| 2 | Primordial Dandelion Tree | `Tower_Yosseledandelion` | Towerfighter | Archer Tower | Red 50 | Health 50+25, Damage 2+0.5, Range **6** (Dimitri 2026-10-12: twice the first build's 3), Seedlimit 6, Seedrate 0.5 | **Limit 1** (scope; Argo's Miner's Base pattern: its button goes while one stands). Grows a seed every 2 s up to its limit, the seeds floating round it; when a foe comes in range every ready seed strikes at once, shared out so each foe gets enough to fell it (nearest first) |
| 3 | Overgrown Stonehenge | `Tower_Yosselehenge` | none | Mining Hut (orientable) | Blue 25 | Health 80+40, Slow 0.25 | Directional stones (the player turns it while placing; the Hut's one-tile block, since Smacky's wall needs two tiles and posts). It shows its facing like Sno Sno's Ice Wall: an arrow on the placement border and one while selected (Dimitri 2026-10-12). As it grows its vines creep forward over the ground (+1 tile every 2 Growth stages, below), over building tiles too (scope); foes on vines are slowed by 25% |
| 4 | Pachira Sapling | `Tower_Yosselepachira` | Gatherer | Mining Hut | Yellow 35 | Health 30+20, Damage 1, Attackspeed 0.25, Range 2 | Its roots mine **every** ore node in Range at once (scope: "like Sno Sno's" Sno Box). At full Growth a node it mines out drops an **aura fruit** |

Growth per building (scope numbers bold, defaults plain):

| Building | Max | Per stage | Full Growth |
|---|---|---|---|
| Mushroom Cannon | **5** | attack speed +8%, blast radius +0.1 | spores **bounce once**: a second blast at the same spot **1 s** later, **half radius, half damage**, 1 stack |
| Dandelion Tree | **10** | seed limit **+3**, range **+40% of its base** (+2.4) | (none in the scope) |
| Overgrown Stonehenge | **8** | damage taken **-10%**; vines **+1 tile every 2 stages** (1 at stage 0-1, 5 at stage 8) | (none in the scope) |
| Pachira Sapling | **5** | mining speed +10%, range +0.3 | **aura fruit** from every node it mines out |

- **Aura fruit** (scope: "a small bonus to all auras", Grafted Colors' example uses 30 of each): **30 Red, 30 Blue, 30
  Yellow**, paid to the owner of the vessel or minion that picks it up (any player's, scope). It sits on the node's
  tile until picked up or the map ends; a picker must come within 0.75 tiles.
- **Vines** are a flat pixel texture on each tile, like Sno Sno's snow (Dimitri 2026-10-12; was a low-poly mat), drawn by networked ground beings (`Effect_Yosselevine`, cloned from Sno Sno's snow without its tag or AI, so
  the cannon never eats them) on each tile they cover; they grow when the Stonehenge does, at the start of a Tide, and
  die with it. "Forward" is the direction the player turned it to; "forwards if possible": the line stops at a cliff
  or a gap (one level up or down is fine).

Tower motion (`docs/03` §4a): the Mushroom Cannon's cap (`Turret`) turns toward each lob (Argo's custom aim); the
Dandelion Tree's ready seeds float round its crown (client, from its synced `Seeds` counter; the limit is the `Seedlimit` stat); the Stonehenge faces its
placement direction; the Sapling stays still. Growth is a skin on all four.

## Upgrade trees

The scope gives each building three levels (`1:`, `2:`, `3:`), two choices at most per level. They map to the game's
tiers 2, 3 and 4, and every choice of one level leads to every choice of the next (default). The trees are shorter than
the kit's 7-11 (5-6 upgrades) and end at tier 4: that is the scope's shape, kept. Costs sit in the shipped bands for
those tiers (`docs/02` §4); level 3 is priced at the top of the tier-4 band since it is the last.

```
Mushroom Cannon:  Sticky Spores2 / Farout Fungus2 -> Viral Outbreak3 / Deathcap Training3 -> Explosive Finish4 / Hallucinogen4
Dandelion Tree:   Seed Packets2 / Demand and Supply2 -> Picky Placement3 / Garden Weeds3 -> Dandelion Storm4
Stonehenge:       Verdant Spread2 / Vine Blockade2 -> Natural Remedy3 / Opportunistic Carnivore3 -> Entangling Spaghetti Roots4
Pachira Sapling:  Focused Roots2 / Sustaining Practices2 -> Grafted Colors3 / Selfish Sapling3 -> Connected Roots4 / Harvesting Season4
```

| Building | Upgrade (key) | Desc | Params | Cost |
|---|---|---|---|---|
| Cannon | Sticky Spores (Sticky2) | Fully spored foes are slowed | Sticky_Slow **0.25** | Red 30 |
| Cannon | Farout Fungus (Farout2) | Longer range, bigger blasts from longer flights | Range_Add 2; Farout_Per 0.5 (radius +50% a second of flight, up to +75%; a lob flies 0.6 s + 0.08 s a tile) | Red 25, Blue 10 |
| Cannon | Viral Outbreak (Viral3) | Fully spored foes may burst when felled | Viral_Chance **0.3** | Red 90 |
| Cannon | Deathcap Training (Deathcap3) | Deadlier spores, fewer of them | Deathcap_Multi **0.25**; Sporemax_Base **8** | Red 100 |
| Cannon | Explosive Finish (Finish4) | Full spores burst, spreading the rot | Finish_Per 1 (damage per stack consumed); Finish_Radius 1.25; Finish_Spores 2 | Red 420 |
| Cannon | Hallucinogen (Hallucinogen4) | Fully spored foes may turn on their friends | Hallu_Every **5**; Hallu_Chance **0.2**; Hallu_Duration **2** | Red 360, Blue 60 |
| Dandelion | Seed Packets (Packets2) | Seeds that fell a foe come back | Packets_Refund **2** | Red 30 |
| Dandelion | Demand and Supply (Demand2) | Grows seeds faster when few are ready | Demand_Max 1 (up to x2 when empty) | Red 30 |
| Dandelion | Picky Placement (Picky3) | Seeds go for the toughest foes and bite deeper | Picky_Percent **0.01** | Red 100 |
| Dandelion | Garden Weeds (Weeds3) | Foes felled after a seed hit speed up the seeds | Weeds_Per **0.1**; Weeds_Window 3; Weeds_Last 15 | Red 90, Yellow 30 |
| Dandelion | Dandelion Storm (Storm4) | Huge range, many more seeds, grown faster | Range_Multi **0.5**; Seedlimit_Add **10**; Seedrate_Multi **0.5** | Red 450 |
| Stonehenge | Verdant Spread (Verdant2) | Vines spread all around | Verdant_Base 1 (vines cover every ground tile within the vine length, counted in steps along the grid) | Blue 30 |
| Stonehenge | Vine Blockade (Blockade2) | A vine barrier each Tide, sturdier stones | Health_Multi 0.5; Blockade_Share 0.3 (barrier = 30% of max health) | Blue 35 |
| Stonehenge | Natural Remedy (Remedy3) | Vines mend buildings | Remedy_Heal **10** | Blue 90 |
| Stonehenge | Opportunistic Carnivore (Carnivore3) | Vines drain foes to mend the stones | Carnivore_Drain **10**; Carnivore_Max **5** | Blue 80, Red 30 |
| Stonehenge | Entangling Spaghetti Roots (Spaghetti4) | Lingering foes are snared and sapped | Spaghetti_After **7**; Spaghetti_Stun 1.5; Spaghetti_Percent 0.1 | Blue 420 |
| Sapling | Focused Roots (Focused2) | Mines one node much faster | Focused_Multi 1.5 (x2.5 speed) | Yellow 30 |
| Sapling | Sustaining Practices (Sustaining2) | Mends itself from the ore around it | Sustain_Heal 0.25 (a second, per node in range, per Growth stage, at least stage 1) | Yellow 25, Blue 10 |
| Sapling | Grafted Colors (Grafted3) | Pick the colour of its fruit | Grafted_Efficiency **0.7**; a COLOUR button | Yellow 100 |
| Sapling | Selfish Sapling (Selfish3) | Mines faster for each other Sapling nearby | Selfish_Per **0.2** (a Sapling within Selfish_Range 4); Selfish_Max **1** (Dimitri 2026-10-12, reversed from "faster with none nearby") | Yellow 90 |
| Sapling | Connected Roots (Connected4) | Takes hits for buildings nearby while healthy | Connected_Share **0.65**; Connected_Above **0.5** | Yellow 350, Blue 80 |
| Sapling | Harvesting Season (Harvest4) | Fruit is worth double | Harvest_Multi **1** | Yellow 400 |

Bold params are the scope's numbers; the rest are defaults (all listed in `docs/YOSSELE_BUILD_NOTES.md`).

- **Hallucinogen** (the engine can't switch a foe's team, `docs/02` §3): a foe "attacking its allies" is held in place
  for 2 s and hits the nearest other foe within 1.7 tiles with its own Damage once a second, as Mummer's Smoke and
  Mirrors does (verified pattern in the template). A purple swirl marks it.
- **Explosive Finish**: the burst consumes the foe's stacks, deals 1 per stack consumed to every foe within 1.25 tiles
  (the bursting foe included), and gives the others 2 stacks; a foe that bursts can't burst again for 1 s (no chains
  in one frame).
- **Viral Outbreak**: a full-stack foe that dies has a 30% chance to burst as one spore bomb (the cannon's impact damage
  and spores) where it fell.
- **Connected Roots**: 65% of each hit a building of yours within the Sapling's Range takes goes to the Sapling instead,
  while the Sapling is above half health.
- **Grafted Colors**: the COLOUR button cycles Red, Blue, Yellow and All; one colour pays 3 x 30 x 0.7 = **63** of it
  (scope's example).

## Visual effects and sounds (temporary art, all generated)

- Debuff looks (the game attaches them): `Debuff_Yossele_Spores` (green-brown puffs, the stack count isn't drawn),
  `Debuff_Yossele_Sporefull` (a toadstool cap over the head at full stacks), `Debuff_Yossele_Daze` (Hallucinogen's
  swirl), `Debuff_Yossele_Tangle` (vines round the feet, Spaghetti Roots' snare), `Debuff_Yossele_Blockade` (a vine
  barrier ring on the Stonehenge); stock `Debuff_Slow` for the vines' slow.
- World effects: `World_Yossele_Sporeblast` (scaled to the real radius), `World_Yossele_Seedhit`, `World_Yossele_Grow`
  (a burst of leaves when a building gains a stage), `World_Yossele_Fertilize`, `World_Yossele_Compost`,
  `World_Yossele_Fruitpick`, `World_Yossele_Mend` (generated, not played yet), `World_Yossele_Drain`,
  `World_Yossele_Dig`, `World_Hero_Yossele_Death`, `World_Yossele_Towerdown`.
- Client-drawn moving pieces: the spore bomb's arc (model `Yossele_Sporebomb`), seeds flying (`Yossele_Seed`), the
  orbiting ready seeds.
- Sounds: `Yossele_Thud` (attack), `Yossele_Lob`, `Yossele_Pop` (blast), `Yossele_Seeds`, `Yossele_Creak` (vines
  grow), `Yossele_Rustle` (a building grows), `Yossele_Fertilize`, `Yossele_Pick` (fruit), `Yossele_Dig` (mining),
  `Yossele_Mend`, `Yossele_Landing`, `Yossele_Death`, `Yossele_Footstep` (`Yossele_Mend`, `_Landing` and `_Footstep`
  are generated but no code plays them yet; the walk uses the template's footstep).

## Changes from the concept, and why

| Concept (scope) | Yossele 0.1 | Why |
|---|---|---|
| Three upgrade levels of one or two choices | Tiers 2-4, every choice leads to every next choice; no tier 5 | The scope's shape; the kit's 7-11 upgrades and tier-5 capstone are guidance, the concept wins |
| Hallucinogen: foes "attack their allies" | Held 2 s, hits the nearest foe with its own Damage each second | The engine can't change a foe's team or target (`docs/02` §3, Mummer's Smoke and Mirrors) |
| Fertilizer unlocked by a Direction | Compost Bin built as three rank Directions; the mode's pick UI isn't built yet, so only the self-test (and later the mode) grants it | Ranks are Phase 5 (kit 1.7); the vessel reads the highest rank it finds |
| Dandelion Season, Mother of Mushrooms (`11_Directions.txt`) | Not built | They replace the Dandelion Tree with new buildings; they come with the Direction catalogue (Phase 5/6) |
| "Growth rises with each tide a tower survives" | A stage when a Tide begins, for buildings that stood through the whole Tide before | Makes "survived" exact; the game has no break event yet (`ModKitTides`) |
| Aura fruit "a small bonus to all auras" | 30 of each colour | The scope's own Grafted Colors example |
| Stonehenge "directional wall like Sno Sno" | A one-tile orientable building (Mining Hut template), turned while placing, with the Ice Wall's two facing arrows; vines go the way it faces | The game's placement turn is the native way (the Ice Wall's too); Smacky's wall is a two-tile post wall |

## Test plan (self-test, `docs/08`, `99_SelfTest.cs`)

1. Generic: vessel select, boat spot, the four build buttons, journal pages and tabs, the Dandelion Tree's limit.
2. Vessel: stats as designed; regen heals a hurt Yossele; FERTILIZE absent without Compost Bin, present with it;
   Compost Bin pays 1 fertilizer per N kills in its radius; FERTILIZE adds a stage and spends one; refused at the max.
3. Growth: a building gains a stage after standing through a Tide (forced Tides), skin `Growth<n>` set, caps at its
   maximum; a rebuilt building is back to 0.
4. Mushroom Cannon: a lob lands, hurts and spores foes in its radius; stacks cap at 10 (8 with Deathcap); DOT ticks;
   full Growth bounces at half radius 1 s later; Sticky slows a full-stack foe; Explosive Finish consumes stacks;
   Hallucinogen is bought (its daze is chance-based: the test logs it, doesn't check it).
5. Dandelion Tree: seeds grow to the limit; all strike when a foe enters range; limit and range grow with Growth;
   Seed Packets refunds; the build button goes while one stands.
6. Stonehenge: vines along its facing, longer with Growth, all around with Verdant; foes on vines slowed; damage taken
   cut by Growth; Blockade absorbs; Natural Remedy and Carnivore heal; Spaghetti Roots stuns after 7 s.
7. Pachira Sapling: mines every node in range at once; at full Growth a mined-out node drops a fruit; the vessel picks
   it up and gets 30 of each (63 of one with Grafted Colors); Connected Roots takes a share of a nearby building's hit.
