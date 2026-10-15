# 24 · Enemy bodies, flyers and bosses (art format, movement, a pack boss)

The second reading of the enemy side, for Phase 4 and a possible pack boss. `13` stays the enemies chapter (data,
Tide tables, elites, how to add a foe); this one answers its §10 questions 5 (art format) and 7 (flying) from code,
details movement per enemy kind, and plans a boss. Everything here was **read from the game's code** (decompiled as in
`13` §11) on 2026-10-14 unless it says otherwise; nothing was tried in game. Stock numbers are data: sheet sizes, PPU,
frame counts and elite skins were read from the asset bundle on 2026-10-15 (`18` §2, §5.5); the rest is §8's PC list.

Read `13` §1-6 first.

## 1. The art format (answers `13` §10 Q5)

| Piece | Fields the code uses | Where |
| --- | --- | --- |
| `Visualproperties` | `Default` (an `Entry`) and `Skinoverride` (one `Entry` per skin) | Visualproperties.cs:9-70 |
| `Entry` | `Style` {Model, Sprite, Both, Overlay}, `Visprefab`, `Spriteobject`, `Additionalvisualscripts`, `Additionalmaterials`, `Additionalprefabs`, `Vfxstart/Vfxend`, `Sfxstart/Sfxend`, `Drawtransparent`, `Spritealwayson` | |
| `Spriteobject` | `Defaultspeedmulti` (1), `Defaultfolder`, `Itemslist` → `Items` by name | Spriteobject.cs:7-54 |
| `Spritesinlist` (one animation) | `Name`, `Sprites` (one list per direction), `Loop`, `Reset`, `Priority`, `Speed` (1), `Spriteto` | Spritesinlist.cs:9-32 |
| `Spritecont` (one frame) | `Item` (the Sprite), `Sound` (played on that frame) | Spritecont.cs:15-17 |

- **Sprite enemies all share one body:** the client loads `Getload("Vfx", "Mobbase")` and adds the `Spriteobject`;
  `Visprefab` is used only when `Style` is not `Sprite` (Entity_Master_Client.cs:136, 578).
- `Dump`/`Dumpsplit` are editor conveniences cut in `OnValidate`; at run time only `Itemslist[].Sprites` counts, so a
  `Spriteobject` built in code fills `Sprites` directly.

**Animation names the code plays on an enemy:** `Move` (walking), `Idle` (settled, and during State `Attack`),
`Attack` (Vis "Attack" or "Shoot": the wind-up of Excalibur, Maul, Bulwark; the Ballista's shot), `Flying` (Halberd).
**There is no death animation:** death is `Vfxend` + `Sfxend`, played on every client only on a real death
(Entity_Master_Radio.cs:95-106). An unknown name is ignored.

**Directions:** `Helper_Entityvis.Rotatesprite` picks the variant from the facing against the camera
(Helper_Entityvis.cs:11-100):

| Variant | Facing | Moving on screen |
| --- | --- | --- |
| v0 | toward the camera | right |
| v1 | toward the camera | left |
| v2 | away | right |
| v3 | away | left |

There is **no flip** anywhere: v1 is drawn facing left. A missing variant falls back to **v0**, so draw 1 or 4, never 2
(narrows `03` §1's "v0 and v1 face the camera").

**Timing:** the animator ticks every `1 / (2 × Defaultspeedmulti)` s and a frame lasts whole ticks:
`ceil(2 / Speed)` of them, so `Speed` ≥ 2 is the fastest (one tick per frame) and a `Speed` between 1 and 2 acts as 1
(Entity_Vis_Animator_Base.cs:60-124). A non-looping animation ends on `Spriteto`. `Animplaysoft` (Idle, Move, Flying)
switches only when the current `Priority` is ≤ 0. Frames advance only while visible or `Spritealwayson`.

**Hurt and skins:**

- Vis "Hurt" flashes the first material's `_Overlayamount` and raises `_Blinkamount` under half health; keep Mobbase's
  material to keep it (Entity_Vis_Animator_Mob.cs:178-203).
- `Master.Skin` swaps the **whole** entry; an unknown skin falls back to `Default`; **a skin entry with no
  `Spriteobject` reuses the default sprites** and adds its own materials and prefabs, so an elite can be "same frames
  plus a tint material" (Entity_Master_Client.cs:542-700). The six elite skins are `Health`, `Hyper`, `Red`, `Chaos`,
  `Armor`, `Explode`. A skin entry with no `Vfxend` loses the death effect on that elite.
- Size on screen = the sprite's pixels / PPU; the hit box is `Entityobject.Defaulthit` (Entity_Master.cs:499-538).

**A stock-format enemy** is one `Spriteobject` with `Idle`, `Move` and (if it winds up) `Attack`, each with 1 or 4
directions; Idle and Move looping at Priority 0; a non-looping Attack whose `Spriteto` is `Idle`; `Vfxend`/`Sfxend`
for death; six skin entries that may reuse the sprites.

## 2. Flying (answers `13` §10 Q7)

**How the Halberd flies** (Entity_Ai_Enemy_Halberd.cs:65-167): it sets State `Flying`; casts a ray straight up; under a
roof it walks; under open sky it rises to the target's height + 2, **teleports its grid tile** to the standable tile
nearest the target within 5 (a `Forced` move, no step check), then glides its body there and lands. It ignores paths
and walls only while its tile has open sky. It is not a Basin attacker: it guards ore, dives on gatherers and vessels,
retreats to and heals at its `Parent`, the **Halberdnest tower** that spawned it (correcting `13` §1's "its enemy
core"), and at full health heads for the nearest resource.

**What "flying" means to the rest of the engine:**

- **Nothing marks air versus ground**: queries filter only on tags, team, targetable and visibility.
- **Towers shoot anything they can see**; the line-of-sight ray goes to `Entityposition` against blocks only, so a body
  in the air is easier to see, not harder.
- **Melee range is 3D** (`Entityposition` distance vs `Range + 0.1`, Entity_Ai_Mob_Melee.cs:92, 143): a body held 2
  units up is out of reach of a melee unit under it.
- `Getpathlinear` is used only by `Entity_Ai_Mob_Movementlinear` (a client-driven straight mover); no flyer uses it
  (correcting `16` §2's "flying uses the same graph in a straight line").

| Pack flyer route | Code | Gives | Limits |
| --- | --- | --- | --- |
| Clone the Halberd | none beyond registering | a resource raider that flies in clear sky | not a Basin attacker; heals at its parent |
| Walker + hover | a brain setting `Entity_Ai_Mob_Movement.Addoffset` (a public `Vector3`, as Fie's knights do sideways) | paths normally but floats; melee can't reach it | still wall-bound; its own lunge snaps it to the ground |
| Own flight brain | a host brain on the Halberd pattern aimed at the Basin, plus `Entity_Vis_Enemy_Halberd` for the look | flies over walls to the Basin | the Halberd visual needs both `Flying` and `Flyingend` children |

There is no anti-air rule to design around: "flying" must be made readable by the pack (a look, and a rule written in
its brain).

## 3. Movement per enemy kind

**The walker loop** (every stock enemy): `Enemy_Basic` copies its parent core's `Goal` once and sends `Move`;
`Mob_Movement` plans with A* over the tile graph, steps a tile per `1/Speed` s (Speed clamped 0.05-12), turns
`Idleblocked` on a refused step, and panics (3× speed, `Debuff_Panic`) after re-planning back and forth three times;
`Mob_Combat` looks for a target **only while `Idle`/`Idleblocked`** (a walker does not fight on the move, except the
Maul, which checks in any state). `Mob_Ranged` fires only once its `Attack` command's `Goalstring` names a projectile,
which nothing in `Mob_Ranged` sets (the only setter is Sno Sno's AI).

**Paths and walls:** a tile is open when its folded `Navblock` is 0; `Influence` adds cost (Tower 20, Wall 40,
Repellor 100); paths are cached 10 s; a step is refused on `Moveblock` (Helper_Navtools.cs:67-191;
Helper_Gridtools.cs:250-327). Inferred: walls with `Navblock` 0 stay in the graph, the walker plans through, is
refused, turns `Idleblocked` and hits the wall. `Findpath` checks one tile per step, not `Additionalspace`, so **a
multi-tile foe can plan a route its body cannot walk**: keep big foes on a one-tile block with a big sprite.

| Enemy | Rule beyond the walker loop |
| --- | --- |
| Aegis | heals `Heal` per second once not hit for `Healtime` |
| Ballista | bolt at `Projspeed`; steps 0.3 toward its target as it fires and is slowed 50% until the next shot |
| Maul | fights on the move; 1.3 s wind-up; hurts up to `Projcount` in `Projradius` |
| Onager | lobs at the ground under the **farthest** visible target |
| Claymore | launches at the farthest tower in `Range` once one is within `Rangedetect`; at 0 health lingers for `Deathlife`. The launch is `Despawn(Death: false)`: no drops, no kill credit |
| Excalibur | 1.3 s wind-up, straight bolt, 1 s pause |
| Bulwark | stuns; picks bomb spots **before** its 1.6 s wind-up, each farther than the last by `Projradius`; bombs 5 s later; its slam never sets `Lasthit` |
| Halberd | §2 |
| Boutman (as a foe) | asleep and damage-proof until `Awaketide` or no `Artriobarrier` left |

**Knobs without code:** stats (`Health`, `Speed`, `Damage`, `Range`, `Attackspeed`, `Projspeed`, `Healtharmor`,
`Healthresource`, a bespoke AI's own stats), `Aiscripts` (stock AIs compose), `Visualscripts`, `Tags`, `Targetable`,
`Upgradesinit` (elites baked in), `Drops`, `Defaulthit`, `Block`, `Leveladd` + `Defaultlevel`. **Needs code:** a ranged
walker's projectile name, a hover, a new attack shape, phases.

## 4. The Tide around the contents (details `13` §3 leaves out)

- A `Wavecontent` waits `Interval` **after every spawn, the last included**, so it lasts `Count × Interval`; `Spread`
  restarts at core 0 for every content; `Gate` must be a valid core index on every map that plays the table, or the
  awaited content throws and the Tide loop stops; a `Wait` content with an enemy set spawns nothing; `Team` is read by
  no code and `Displayedwave` is editor-only (Mode_Normal.cs:36-186; Wavesobject.cs:9-72).
- **`Time_Wave` counts down only after every content has finished spawning** (Mode_Normal.cs:384-403; verified by hand):
  a Tide lasts all its spawning plus up to `Wavetime`, and `Tide_Completetime` plays when the whole entry took longer
  than `Wavetime` (corrects `13` §3 step 4).
- Each Tide lowers every enemy core's `Healthresource` by 1.

## 5. Hooks a mode can use on the enemy side

| Hook | Fires |
| --- | --- |
| `Game_Entity.Onspawn(master)` (host) | every spawn, before the entity's own start |
| `Game_Entity.Ondespawn(master, killed, reason)` (host) | every despawn, before drops; reasons `""`, `Pop`, `Kill`, `Sell`, `Self`, `Rafts`, `Dog` |
| `Entity_Master.Loaded` | after tags and upgrades are in: read stats here |
| `Entity_Master.Child` on an **enemy core** | each foe that core spawns: a per-core "foe spawned" hook |
| `Entity_Master.Command` on an enemy core | each `Spawn` request with its JSON |
| `Entity_Master.Kill(victim)` | on the **killer** (`Lasthit`); set only by `Interacthurt` without a `Damage` key |
| `Game_Mapvars.Wave`, `Entry`, `Time_Wave`, `Display`, `Gamecomplete` | synced: every client sees them |

## 6. A pack boss

**What the engine offers:** no boss class, bar or state. The only scripted big foe is the Boutman: asleep and
damage-proof, a waking `Uiwarning_ClientRpc("ALERT ▪ SOMEONE IS VERY GRUMPY", …)`, timed adds, damage that grows
(Entity_Ai_Enemy_Boutman.cs:44-287). The health bar shows only for the selected entity. **A boss is a `Beingobject` like
any foe: stock AIs for walking and hitting, plus a host brain for phases** (answers the expansion scope's row 5).

| Tool (host) | Use |
| --- | --- |
| `Helper_Entitytools.Addmod` / `Addstat` | phase buffs, temporary stats |
| `Option_Defaultlevel` | raise `Level`, so every stat's `Leveladd` applies: data-driven phases |
| `Master.Skin.Value` | a new look per phase |
| `Radio.Vis_ClientRpc`, `Vfxshake_ClientRpc`, `Uiwarning_ClientRpc` | wind-ups, shakes, the red warning strip |
| `Game_Mapvars.Display.Display_Wave` | a banner line; free mid-Tide (the mode writes it at entry starts and breaks) |
| the parent enemy core's `Spawn` command | adds that the Tide waits for |
| a priority-9 `Oninteract` that zeroes damage | invulnerable windows while staying targetable |

**Plan** (working id `Enemy_RogueBreaker`, the headline of the Breaker Tide that closes maps 5 and 10, the Rogue
Waves `docs/reports/SCOPE_VS_LOOP.md` #20):

1. Data: clone `Enemy_Bulwark` (Health 30000, Speed 0.3, stun slam and bomb rain); big sprite art per §1 with
   `Phase2`/`Phase3` skin entries reusing the sprites with a material; a big `Defaulthit` on a **one-tile** block;
   `Leveladd` on Damage, Speed and Attackspeed so `Level` 2 and 3 are the phases.
2. Brain (attached in `Onspawn` when the being is the boss): on each slow tick compare health with 66% and 33%; at a
   threshold set the level and skin, warn, shake, call adds from the parent core, and open a short invulnerable window.
   Optional bar: write `Display_Wave = "<NAME> <percent>"` at most once a second, restore `TIDE n` at its death.
3. Ending: make the Breaker Tide the map's last entry with a long `Wavetime` (Tsunami's last Tide uses 9999 s). The
   Tide lasts until no targetable team-0 enemy is left, then the map is won by the stock loop: **no win code**.
4. Never make it untargetable (the Tide would end, `13` §9.1) and never shield it with `Healthover` (a hit larger
   than the shield passes in full, Helper_Damagetools.cs:28-46).

**The final map's 6:00** (Mode_Final.cs:241-310) is a hard-coded game-time clock, not data: only its first Tide's
contents run once; `Resourcesinit` is paid once per **real** second (fast-forward lowers income per game second); **a
destroyed Basin wins the final map**. A pack mode built on `Mode_Final` wires its own `Despawn`.

## 7. Pitfalls (predicted from the code; move to `09` once met)

1. A non-looping animation whose `Spriteto` is missing, or has fewer variants, stops the enemy animating for good (the
   runner's empty `catch` ends the loop, Entity_Vis_Animator_Base.cs:66-145).
2. A looping Attack, or an Idle/Move above Priority 0, never lets Idle/Move come back.
3. A custom State (not `Move`, not `*Idle*`, not `Attack`) freezes the body while the AI moves it.
4. Two direction variants: the foe faces right while walking away to the left.
5. `Adjustspeed(anim, s)` writes the **current** animation's speed, not `anim`'s.
6. A ranged walker never shoots until its brain sets the projectile name.
7. An `Overlay`-style upgrade look leaves a tint on removal, and re-adding it throws (Entity_Master_Client.cs:450-490):
   apply such a look once.
8. Reusing the Boutman visual and sending Vis "Ko" writes a stock unlock.
9. One `Gate` typo stops the whole run's Tides.
10. A foe killed by a `Death: false` despawn pays nothing and shows no death effect.

## 8. PC list (one enemy probe round)

1. Answered 2026-10-15 (`18` §2, `reference/art_survey_2026-10-15.txt` part 1): for each `Enemy_*`,
   `Visualproperties.Default` (`Style`, `Spriteobject`, `Defaultspeedmulti`, `Vfxend`/`Sfxend`); per animation `Name`,
   `Loop`, `Priority`, `Speed`, `Spriteto`, variant and frame counts; frame 0's texture size, rect, PPU and pivot.
2. Answered 2026-10-15 (`18` §5.5, the survey's part 3): each elite skin entry's `Spriteobject`,
   `Additionalmaterials` and shader.
3. `Defaulthit` collider size for Dagger, Bulwark, Halberd.
4. `Enemy_Halberd`: group, AIs, visual children (`Flying`, `Flyingend`), which map places a nest.
5. Block flags (`Genproperties`) of `General_Mob_Block_Height`, a wall, a tower and the Basin.
6. Tests: a Dagger with a hover offset of 2 (tower and melee hits); a Dagger against a wall (plan through or around);
   `Display_Wave = "TEST 72"` mid-Tide on host and client; a `Gate` past the core count; a boss mock (Bulwark clone,
   Health 3000) with the threshold brain, skin swap, warning, adds and the Tide ending at its death, host and one
   client.

## 9. Sources

The decompiled `Assembly-CSharp.dll` (Entity_Ai_Enemy_*, Entity_Ai_Mob_*, Entity_Vis_*, Helper_Entityvis,
Helper_Navtools, Helper_Gridtools, Spriteobject, Visualproperties, Mode_Normal, Mode_Final, Wavesobject), read on
2026-10-14 by a parallel reader and spot-checked by hand (the Tide timer, `Getpathlinear`, the variant fallback);
`reference/roguelab_round1-2.txt` for stock numbers.
