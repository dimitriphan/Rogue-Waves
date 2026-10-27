# Yossele implementation notes (0.2.0-dev, 2026-10-23)

**0.2.0-dev is the 2026-10-12 build (0.1.0-dev, PC rounds 1-8) brought up to Dimitri's sheet of 2026-10-20**
(`docs/scope/07_Vessel_Yossele.txt`, with his 2026-10-21 answers): design in `DESIGN.md` (every default is under
"Changes from the concept"), temporary art in `Assets/YosseleMod/Editor` (`Previews/0.2/ASSETS.md` lists what changed),
code in `Code/`, self-test in `Code/99_SelfTest.cs`. The 0.1 asset list and its defaults are in
`docs/YOSSELE_BUILD_NOTES.md` and `Previews/0.1/ASSETS.md`.

**State: PC round 9 passed on its fifth try (9e, 2026-10-23): self-test 160 pass, 0 fail, 0 setup; release boot
check clean; 0.2.0-dev is Dimitri's play build** (`Mods\YosseleMod`, the only mod; 0.1.0-dev parked on the PC). Here:
`check_mod.py` compiles the mod with kit 1.6 at 0 errors, 0 warnings, in release, with `YOSSELE_SELFTEST` and with
`YOSSELE_SELFTEST,YOSSELE_SHOTS`; `check_editor.sh` passes on `Assets/YosseleMod/Editor`; the cloud preview renders
every sheet and model. Not seen yet: the look by eye, a played map, online. What is left to check, and how:
`docs/reports/YOSSELE_TEST_PLAN_2026-10-23.md`.

## Code map

| File | What |
|---|---|
| `00_YosseleMod.cs` | Boot (pack registry part "Yossele", asset lookup, content, journal), per-frame calls into the kit and the server |
| `01_Util.cs` | Helpers: stats, health (`Value` left, `Value_Calc` max), tiles, ground spawns, kept modifiers, slows and roots, debuff looks (Argo 2.0.1's send), hurt via `ModKitCombat.Hurt` |
| `02_Data.cs` | Every name, text and number |
| `03_Assets.cs` | Sprite sheets to animations, pixel icons |
| `04_Content.cs` | Clones the hero (Smacky, made ranged like Sno Sno), the laser (Sno Sno's snowball), the four towers and the Bonsai, the Cordyfriend (the Snoble), vine and fruit ground beings, commands, Directions, Growth skins |
| `05_Server.cs` | Host rules: players, fertilizer, Compost Bin, Tides (`ModKitTides`, the map's Tide count), spawn and despawn hooks, the Tree's and the Bonsai's limit |
| `06_HeroBrain.cs` | Build options (the Tree's slot offers the Tree, then the Bonsai), arming the stock ranged AI with the laser, regen, FERTILIZE |
| `07_Building.cs` | Base for the buildings: Growth, upgrade flow (mirrors `Entity_Ai_General_Upgrade`) |
| `08_Cannon.cs` .. `11_Pachira.cs` | One brain per building (the Bonsai is a Tree brain in `09`), plus spores, vines, fruit and Softening Roots |
| `12_Hooks.cs` | One `Oninteract` front hook: Stonehenge armour, Blockade barrier, Connected Roots, Fortifying aura, Easy Target, Monument |
| `13_Client.cs`, `14_Commands.cs` | Every look and sound (Vis RPCs), the Cordyfriend's tint, the FERTILIZE picker and COLOUR button |
| `15_Journal.cs` | Journal pages, diagnostics dump |
| `16_Friend.cs` | Cordyfriends: raising, the cap, where each walks |
| `97_Shots.cs`, `99_SelfTest.cs` | Debug builds only |

## Build of 2026-10-23 (0.2.0-dev): what PC round 9 must show

Each line is an assumption the new code makes from the decompiled game, not yet seen running.

1. **The laser.** The vessel runs the stock `Entity_Ai_Mob_Ranged` (Sno Sno's set-up) and its Attack command fires
   `Hero_Yossele_Laser`, a clone of `Hero_Snosno_Snoball` with the `Yossele_Laser` model (fallback: Smacky's archer
   arrow, logged at boot). Check it targets, fires at range 4, hurts for 5.5, mines ore as Sno Sno does, plays the
   Attack sheet on "Shoot" and `Yossele_Zap` on every client.
2. **Cordyfriends.** Spawned as the cannon's child on the fallen foe's tile (Mummer's Understudies), fighting with the
   stock melee AI, moved by `YosseleFriendBrain`. Check one rises, fights, walks back home, blocks foes, stays when its
   cannon goes, is tinted on a client, and that 30 is the cap.
3. **D-MDS.** It reads the host's pooled projectiles (`Game_Entity.Projectiles`) and puts one out with `Kill(false)`;
   clients put out their look-only copy from the Vis. Check a real foe's shot (an archer's, a ballista's, a bomb's) goes
   out with no hit and no explosion, on the host and on a client.
4. **Fruit from payouts.** Nodes refill instead of running out, so a fully grown Sapling drops a fruit on each payout
   its strike brings about (`Healthresource` before and after the strike). Check on two or three stock node kinds.
5. **Softening Roots.** An Add -1 modifier on a node's `Healthresource` (hits per payout); check the node pays out one
   hit sooner and still refills, and that the modifier goes when the Sapling does.
6. **The Bonsai.** The Tree's build slot turns into the Bonsai's once Bonsai is bought; check the button, the cost
   (Red 50), sell value 0, its copy of the Tree's upgrades at 40%, and that it keeps them when the Tree goes.
7. **Monument.** The attacker's `Interact` runs before the target's `Oninteract`, so a foe's debuff is already on when
   the hook strips it. The self-test fakes the stun; a real Bulwark's stun on a Monument is the proof.
8. **Growth on short maps.** `Totalwaves` of the map's mode gives the Tide count (as the coin artifact reads it);
   check it is 4 or fewer on a short map and that buildings grow 5 stages a Tide there, 1 elsewhere. The host logs one
   line a Tide with the map's Tide count, the stages given and the chance-based counters (`OnTide`).
9. **Easy Target** shows its own `Debuff_Yossele_Marked` look on a foe that lingers 5 s on the vines (round 9: the
   stock `Debuff_Weakened` isn't loaded).
10. **Carried-over fixes** (`docs/reports/QUALITY_PASSES_2026-10-15.md`, B3-24 to B3-39): debuff looks sent once per
    target and cleared at a map's end, the footstep on frames 0 and 3, the Mend look and sound, the vine texture's join,
    Connected Roots on the owner's buildings only, the fruit's last pick cleared per map.

## PC rounds

**Round 1 (2026-10-12):** applied, Unity compiled the editor code clean, the code build passed; stopped at the export
(it didn't park the other mods, pitfall #103) and the sheet slicing read a scaled width (pitfall #102). Both fixed.

**Round 2 (2026-10-12, 3B_Thehunt): 86 pass, 1 fail, 1 setup.** Boot clean (`ready: 6 beings, 22 upgrades, 3
Directions, 16 effects, icons ok, ground from Effect_Snosnosnow`), no Yossele exceptions (11 lines, all the game's own
sound and setup ones). Proven in game: stats and regen, building, costs, the Tree's limit and its return, Growth on
forced Tides (counter and skin, stops at the max, no growth in the Tide built), Compost Bin unlocks FERTILIZE and pays
1 per 10 kills, FERTILIZE grows a stage and refuses a full building, every Cannon mechanic but the chance-based Viral
and Hallucinogen, the Dandelion's seeds, limit, range and Seed Packets, vines along the facing (orientation 0 faces
+z), longer with Growth, Verdant 3 -> 14 tiles, the vine slow, Carnivore, Blockade's health and barrier, Natural
Remedy, the Sapling's mining, fruit drop and pickup (30 of each), Grafted Colors (63 Blue), the journal and tabs.
- FAIL: "the game attaches Debuff_Slow": a modifier draws nothing; the vines now put the stock slow look on.
- SETUP: Spaghetti Roots' foe walked off the vines before 7 s; the test now holds it there.
- Not reached: the hit hook (armour, barrier, Connected Roots) saw no hit (`softened 0, soaked 0, shared 0, skipped
  0`); round 3 adds a real hit on each. The "far foes" check spawned none (it aimed off the hill); fixed.
- The slice warnings came from round 1's stale .meta on the first import; the meta is right now (48 slices).

**Round 3 (2026-10-12): 86 pass, 1 fail, 2 setup.** No slice warnings. Now proven in game: the stock slow look on the
vines, Growth's armour (a hit of 10 costs 6), Vine Blockade's barrier soaking a hit (72 -> 62, health untouched),
Connected Roots (cannon lost 3.5, Sapling 6.5); the far-foes Compost check ran (0 after 3).
- FAIL: "seeds grow back over time" read 0 seeds 1.5 s after emptying (it passed in round 2 with 0.75). Cause not
  found from the log; round 4's check waits up to 4 s and logs the limit, the Seedlimit stat, brain and strikes.
- SETUP: Seed Packets (this time no seed felled the foe; a chance of the test, not a bug) and Spaghetti Roots (the
  rooted foe still never stayed 7 s; round 4 moves its start back once it counts as on the vines, and logs it).

**Round 4 (2026-10-12): 58 pass, 0 fail, 1 setup, then the test itself stopped** on a NullReferenceException reading
the Dandelion Tree's brain: the tree had been removed by then (its brain destroys itself when its entity goes). Most
likely Tide foes felled it (they keep marching while the test holds the Tide clock), which also explains round 3's
"0 seeds" read. Round 5: every building logs when it goes and why, each test section clears Tide foes first, and a
felled building turns its checks into setup lines instead of stopping the run.

**Round 5 (2026-10-12): 91 pass, 0 fail, 0 setup.** Export clean, 0 slice warnings, no Yossele exceptions (the game's
own `Game_Ui.OnDestroy` and `Game_Init.OnDestroy` NREs at shutdown, also in earlier Argo and RogueLab logs). The only
building lost was the Tree the test fells on purpose, so clearing Tide foes per section was the fix. Newly proven: seeds
grow back (0.5 a second, ready 0.501 at limit 12), Seed Packets (2 refunded), Entangling Spaghetti Roots (1 snare),
every hit-hook check again. Not checked: Viral Outbreak's 30% burst and Hallucinogen's 20% daze (0 of each this run).
Left for Dimitri: the look by eye (temporary art, debuff heights, the Stonehenge's turn, vine and fruit placement), the
balance pass, and online play.

**Dimitri's notes from playing (2026-10-12), changed for round 6:**
- The Stonehenge now shows its facing like Sno Sno's Ice Wall. Read from the game's code: the build command draws an
  arrow on the placement border when a tower's hit prefab has a `Forward` child, and `Entity_Vis_Tower_Orientation`
  draws one while it is selected. The Stonehenge gets both; the marker copies the Ice Wall's own `Forward` when it can
  be read (`YosseleContent.ShowFacing`, logged at boot).
- Vine tiles are a flat 20 x 20 pixel texture on the ground like Sno Sno's snow (`YosseleModels.VineGround`, a
  cut-out quad), not a low-poly mat.
- The Dandelion Tree's range is 6 at Growth 0 (was 3); each stage still adds 40% of it.
- Selfish Sapling is reversed: +20% mining speed for each other Sapling within 4 tiles, up to +100%.
- Not seen yet by eye: the two arrows, the vine texture's look and height.

**Round 6 (2026-10-12): 96 pass, 1 fail, 0 setup.** Proven in game: the Stonehenge's facing arrows (the Ice Wall has
the same two: `Entity_Vis_Tower_Orientation` and a `Forward` marker at the tile centre), a Stonehenge turned 90 degrees
faces and grows its vines that way with the model turned once, the Tree's range 6 (10.8 at Growth 2), Selfish's rule.
- FAIL: the vine tiles had no texture. Boot puts every library model onto one copy of the game's tower material with
  the palette texture (`YosseleAssets.ApplyTowerMaterial`), the vine quad included. Round 7 leaves `*_Ground`
  materials alone and logs what a live vine tile and Sno Sno's snow draw with.

**Round 7 (2026-10-12): 97 pass, 0 fail, 0 setup**, then the play build installed for Dimitri. The vine tiles draw
their own texture (URP Lit, cut out, 0.01 above the tile). Sno Sno's snow is a 0.8 x 0.1 x 0.8 mesh on the game's
`Shader Graphs/General Block` material with no texture: if the vines should be lit exactly like it, that shader is
the one to copy with the vine texture. Waiting on Dimitri's look by eye.

**Dimitri, playing round 7's build (2026-10-12): the vines float.** A ground being stands half a tile above the ground
like a tower (the vine being at y 11, Yossele at 11, the ground at 10.5); the quad sat at the being's own height.
Round 8 puts it at the tower convention's ground (y -0.49) and checks the drawn tile against the being and the ground
block's top. (Note 3 above guessed the ground at the vine model's origin; the fruit, in the tower convention, was right.)

**Round 8 (2026-10-12): 98 pass, 0 fail, 0 setup** on the second build, then the play build installed for Dimitri.
The vine tile now draws at y 9.51 for a being at 10 (on the ground). The first build used the old editor code
(pitfall #104) and failed that one check. The ground block's top wasn't found by the check's lookup, so it passes on
the half-tile rule; Dimitri's eye is the real check.

**Round 9 (2026-10-23, 0.2.0-dev, 3B_Thehunt): 154 pass, 6 fail, 0 setup.** Apply clean, editor assembly rebuilt after
the apply, Build Queue ok, no Yossele warnings, the three exceptions the game's own. Proven in game: the ranged AI armed
with the laser (77 client zaps), the stats, 5 Growth stages a Tide on a 4-Tide map, Cordyceps raising Cordyfriends (4
raised, stats, stock melee AI, Snoble block, walking to a foe; 8 sprite renderers tinted), Viral and Explosive Finish,
Deathcap's cap, Rubbery Shell, Dandelion Storm, Easy Target's +15%, Entangling Roots once per foe, Monument's armour and
its strip, Obsidian, fruit from real payouts (12 dropped), Grafted, Connected and Fortifying Roots, Forest Project,
Harvesting Season, Softening Roots (4 -> 3 hits), D-MDS shooting down a ballista bolt, every Bonsai check, Garden Weeds.
The 0.1.0-dev play build went back into the Mods folder. The six failures, fixed for round 9b:
- "the laser hurts a foe": the foe's health was read right after its spawn, before it was filled in (0, then 58); the
  check now reads against its maximum, on a Dagger.
- "Hallucinogen's stun holds the foe": speed 0.525 -> 0.05, the Speed stat's lower bound; the game's own stun leaves
  0.1 (the Boutman boat's Override), so the check takes 0.1 or less.
- "Growth lengthens the vines": section 4's long Tide had grown the Stonehenge already and vines never shrink (Growth
  never falls in play); the check lays them again from Growth 0.
- "Debuff_Weakened attached": that stock look isn't loaded (pitfall #110); Easy Target has its own
  `Debuff_Yossele_Marked`.
- "Debuff_Yossele_Tangle attached" (no foe) and "Spontaneous Mulch" (`foe != null` after its fall): the first foe was
  most likely felled by Yossele's laser, which shoots anything in range while the test waits; Yossele now holds its fire
  (a Range override) from section 2 to the journal. The Mulch check keeps whether its foe spawned before felling it.

**Round 9b (2026-10-23, same map): 157 pass, 2 fail, 0 setup.** The four fixed checks pass, `Debuff_Yossele_Marked`
is generated and attached, and the "Could not retrieve Vfx/Debuff_Weakened" line is gone. Client zaps fell from 77 to
2: in round 9 Yossele most likely spent the round shooting the ore node near it (the stock ranged AI also targets
Resource), which holding its fire now stops. Not checked: whether that mining by laser is wanted in play. The two failures, fixed for round 9c:
- "the laser hurts a foe: no foe": one shot felled the Dagger and its object was gone. The check keeps whether the foe
  spawned, and a felled foe passes when the server saw Yossele's hit last (`YosseleServer.LastFelledBy`).
- "a fully grown Sapling drops a fruit when its strike pays a node out: payouts 9 -> 9": no payout of the Sapling's in
  6 s after the node was set one hit from paying out. Round 9d (below) showed why.

**Round 9c (2026-10-23): stopped at the apply check**, nothing changed: the handoff was made from round 9's commit, so
two files the PC already had in their 9b versions read DIFFERS. Remade from 9b's commit as round 9d. A follow-up
handoff in the same round is made `--since` the previous handoff's commit.

**Round 9d (2026-10-23, same map): 157 pass, 2 fail, 0 setup**, the same two checks, now with their details:
- "the laser hurts a foe: felled (1 fell), last hit by Hero_Yossele, zaps 0 -> 2": the laser works; the check wanted
  a health loss read against the foe's maximum, which was still 0 right after the spawn. A felled foe now passes on
  who felled it, and a living one is read live.
- "drops a fruit: strikes 46 -> 47, payouts 9 -> 9, node now 0/4, refilled by another hit 0x": the Sapling's strike
  took the node from 1 to 0 and it did NOT pay out. A node pays only when every health stat is at 0
  (`Helper_Damagetools.Addressdamage`), so it most likely has another (a `Health` of its own) that the next hit
  empties, and pays then; round 9 passed because Yossele's laser had taken the node to 0 first. In play the Sapling
  counts that later payout too (the node refills, so after > before). The check now gives the Sapling up to three
  strikes and logs the node's other health stats.

**Round 9e (2026-10-23, same map): 160 pass, 0 fail, 0 setup.** The laser's first loss was 5.5 (Yossele's hit, read
live). `Resource_Orange` logged `Health 1/1` beside its `Healthresource`: the Sapling's first strike took the node to
0, its second paid out and refilled it (payouts 9 -> 10, fruits 4 -> 5), so a node pays on the hit after its
`Healthresource` reaches 0 (kit `docs/09` #106, corrected). Exceptions: the game's own three. Then the release build
(no defines), boot check ("loaded v0.2.0-dev", "ready: 8 beings, 32 upgrades, 3 Directions, 17 effects, icons ok",
the 18 shutdown sound NREs round 8 had too), installed as the play build. The mod's `.info` still says `"_version":
"1.0"`; the version is set once for the pack (`docs/PACK.md` §4). Client zaps in a self-test are now 1 or 2: with its
fire held from section 2, Yossele no longer shoots the ore node beside it (the stock ranged AI targets `Resource`, so
in play the laser most likely mines any node in range too; worth a look on a played map).

**Play test (2026-10-26, no handoff; `docs/reports/PLAYTEST_2026-10-26.md`).** A debug-only virtual player
(`Code/98_Play.cs` with kit `ModKitPlay`, play builds only) played Purlest on its own income to Tide 8, Woozy with
sandbox money and spawned shooters, and The Hunt, clicking the HUD for everything: 30 of the 32 upgrades bought by
clicking their entries (Picky Placement and Fortifying Roots weren't on a route the bot finished), the Bonsai built from
the Tree's slot, the Tree sold through SELL. One bug: Hallucinogen's stun look, the stock `Debuff_Stun`, isn't loaded on
Woozy (10 stuns, 10 "Could not retrieve" lines, no look); it now uses the generated, unused `Debuff_Yossele_Daze`
(`YosseleData.VfxDaze`; pitfall #114); the last Woozy run had 9 stuns and no such line. Seen in play for the first
time: D-MDS taking down real Ballista, Onager and Excalibur shots (118 by Tide 7), Cordyfriends rising (14), fruit from a
fully grown Sapling (3 on `Resource_Blue`), the Bonsai keeping its copy when the Tree was sold. The laser does mine: on
Woozy the vessel dealt 802 damage by Tide 7 with no foe felled.

## Gap to the 2026-10-20 sheet: closed in 0.2.0-dev

Everything the gap list named is built (`DESIGN.md` has each rule and default): the ranged mouth laser and the new
stats (70 / 1.75 / 5.5 / range 4, projectile speed 15), 5 Growth stages a Tide on a map of 4 Tides or fewer, the level
4 capstones (Cordyceps and the Cordyfriend, Dandelion Storm, Monument, Fortifying Roots, Softening Roots), D-MDS,
Bonsai (limit 1, the Tree's cost), Rubbery Shell, Easy Target, Obsidian, Spontaneous Mulch, the changed rules and
numbers, and the 2026-10-13 names. Fertilize stays Direction-only, so the first release has none.

Still open: Dimitri's own sprite sheet (the release waits for it) and the Cordyfriend's sprite; the look by eye of the
new models and the attack sheet; the balance pass; online play.
