# Argo implementation notes (2.0.0, PC round 6 139/0, 2026-10-08; v1.4.0, 2026-10-04; 1.0.0 on 2026-10-02)

**2.0.1 (2026-10-15, compiled, not run): looks that must show online.** The decompile showed the game drops
`Vfxdebuffcheck` and `Vfxoneshotcheck` sends while 50 effects wait (kit `docs/19` §4, pitfall #95), which happens in
busy online fights. `ArgoUtil.Debuff` (the slow look on every slow, root and pin) now calls `Vfxdebuff_ClientRpc`
itself, counts it in `Clientsendstack`, and skips a look already showing until the new one's end (cleared per map,
pruned at 256 foes); the refused-flood mark goes through `ArgoUtil.MustSee`. The upgrade and placement borders stay
on the throttled path (flavour; Argo's own `Vis` looks carry the news). Compiled with kit 1.6 at 0 errors in release
and `ARGO_SELFTEST,ARGO_SHOTS`. Check online: a slowed foe shows its slow look on the friend's screen in a
big fight (kit `docs/19` §8 N4).

**2.0 for Rogue Waves, before its first PC round (step 3 code done, 2026-10-08; the rounds below then ran).** Design approved (`DESIGN.md`, "Step 3, code reality" lists
how the code does it). Art from step 2 (`Previews/2.0/`). **Checked here only:** `check_mod.py` compiles the mod with
kit 1.6 at 0 errors in release, `ARGO_SELFTEST` and `ARGO_SELFTEST,ARGO_SHOTS`; `check_editor.sh` passes.
**Not built in Unity, not run, not seen in game.** The self-test (`99_SelfTest.cs`) follows DESIGN's 2.0 test plan.

What the first PC round must show, in order (each is an assumption the code makes):

1. **The fifth build option.** `Command_Buildoption5` is a clone of `Command_Buildoption4` with key 5. If the hero's
   build bar or select screen shows only four, or the button does nothing, take DESIGN's fallback (Deep Water as a
   FLOOD-like command). `Diagnostics` logs `Option5Note` and every command's keys.
2. **Deep Water on Buster's Speed Pad** (`Trap_Busterpad`; else `Trap_Smackywall_Perma`). Its Health and upgrade tiers
   are assumed; Smacky's hut tiers are lent when the template has none.
3. **FLOOD** goes through `Command_ServerRpc`, which needs a `Build` command (`Command_Build`) on the trap; the server
   checks adjacency and pays Blue 15 scaled like a build. Refusals log `Refused` and warn 11.
4. **AIM**: the picker sends the surface position; the drill converts it with its own offset
   (`Pos(Master) - Currentblock.Position`). If the Tremor ring sits a level off, that offset is the cause.
5. **The Miner's Base limit** removes `Buildoption5` while one stands (Boutman's pattern) and re-creates it after a
   sell or a death; `BaseHidden`/`BaseShown` count it.
6. **Kit 1.6**: the registry line `[RogueWaves:Argo] v0.1.0-dev kit 1.6` (the pack version, `docs/PACK.md` §4) in Player.log; the F-key check.

**PC round 1 (2026-10-08): 93 pass, 9 fail, 0 setup** (log summary in the thread; Thehunt). Boot is clean: the
registry line `[RogueWaves:Argo] v0.1.0-dev kit 1.6`, journal tabs, no mod exceptions. The failures:

- **Command_Buildoption1 and 4 aren't loaded when Argo boots** (`docs/09` #71), so no FLOOD and no fifth button; that
  is 3 FLOOD checks plus the Miner's Base build. Fixed: `ArgoContent.LateClones` clones both once the match loads them,
  a water built earlier gets FLOOD on its next frame, and the hero looks the command up quietly (the 314 "Could not
  retrieve Command_Buildoption5" lines). So the fifth build option itself is still unproven.
- Scatter bonus: the dummy stood in a Tremor or Deep Water, so its "clean" hit had the bonus too (3, 3, 2, 3). The test
  now clears Argo's status before each hit.
- Fault Line: the burst fired, but the foe had 5.5 health left; the check now accepts a burst that fells it.
- Deep Core: the test forced Bedrock3 before buying Deep Core3 (one tier 3 per tree); it now buys Pay Dirt and Deep
  Core first. Fathom 7 failed with it, and the client read a stale Tremor radius (2.4 vs 3.6) 0.8 s after a sink:
  `ApplyFathom` now syncs the AIM link at once, and the check waits up to 2 s and logs what the server sent.
- The PC's `Build-ArgoMod.ps1` was an older copy without the `MODKIT_TEST` line; round 2 replaces it.

**PC round 2 (2026-10-08): 133 pass, 3 fail, 0 setup.** **The fifth build option works in game**: Argo clones
`Command_Buildoption5` and FLOOD once the match loads the build commands (Player.log: "cloned from Command_Buildoption4
(keys 4 -> 5, priority 6)", "FLOOD: keys 1"), the Miner's Base builds from it, its button goes while one stands and
comes back after a sell, and FLOOD adds a pool for Blue 15. No mod exceptions, no "Could not retrieve" lines. The fails:

- Brine read 12 a second: the guns were shooting the foe in the pool. The test now stops every gun and the vessel
  while it measures (pinned read 1.15 a second, Brine doubled with Crushing Depth's 15%, as designed).
- Trench (and Abyss, which only logged "no free tile") found no free tile beside the water. The test now puts Deep
  Water where three tiles beside it are free, and both checks log what blocks each neighbour.
- Fathom 7: clients still read the old Tremor radius (2.4) while the server sent 4.4. The synced command list most
  likely held a stale second "Aim" entry (Updatecommand updates the first it finds and moves it to the end). The
  server now keeps one entry and clients read the last one; the check logs how many it removed.

**PC round 3 (2026-10-08): 123 pass, 5 fail, 0 setup.** **The Tremor radius now reaches clients after a sink**
(Fathom 7 passes; the server removed one stale "Aim" entry, which confirms round 2's cause). All 5 fails were the test's
layout on Thehunt's hill: Deep Water went on a one-tile ridge (two free tiles beside it), so Abyss and Trench had no
tile, no foe would spawn on its pool (so Brine never ran), and the second Harpoon Gun got the ridge's end with no lane of
two for Undertow. The base gun check read a Scatter pellet's hit on the far foe as the harpoon's (the harpoon hadn't
fired). Round 4's test: Deep Water is built last, where five tiles on one level take a pool; the second gun is built
right after the first; foes for the water can stand on any tile of it; the other guns hold fire while the base harpoon
is checked, which waits for its shot; Undertow takes one foe with room to reel and a second beside it.

**PC round 4 (2026-10-08): 134 pass, 0 fail, 0 setup.** Every 2.0 mechanic in DESIGN's test plan passed in game on
Thehunt, with no mod exceptions. One gap: Bedrock felled the test foe, so the two Fault Line checks skipped without a
line (they passed in round 3). The test now gives Fault Line its own foe and fails if it has none. Still to do: the
captures (`ARGO_SHOTS`) for step 4's art review, Deep Water's bob, and hand play.

**PC round 5 (2026-10-08): 138 pass, 0 fail, 0 setup, with captures** (`ARGO_SHOTS`, 19 PNGs on the PC in
`handoff\selftest_logs\Argo_round5_shots`, attached in the thread; not yet reviewed here). Fault Line's checks ran and
passed. Then the playable build (no defines, v0.1.0-dev) was built, boot-checked with Crackpot, Mummer, Rustle, Sprigg
(every vessel logged its icons; 18 NullReferenceExceptions in the game's own ambient-sound teardown at close, no mod
frame) and left **installed and enabled** for hand play. No zip, no publish.

**Deep Water's bob (2026-10-08, after round 5; verified in round 6).** The water and what
floats on it are a `Swell` part of every `Trap_Argowater*` model and of `Argo_Pool` (the frame and pump stay still;
`ArgoModels.Swell`). `ArgoClient.Bob` raises and lowers it by ±0.012 over 3.2 s, phased by the tile so a row of pools
rolls like one swell; parts are found again after an upgrade rebuilds the visual. The self-test checks it ("Deep
Water's water bobs gently"). The model preview at rest matches the old one.

**Round 5's captures reviewed (2026-10-08, `Previews/round5/REVIEW.md`).** Everything reads as designed except Deep
Water's 3D views in the journal and on the boat, where the flat pool is a line. The journal now gets a tipped
two-sided copy (`ArgoAssets.JournalModel`): compiled, not yet seen. The boat's view is the game's and stays.

**PC round 6 (2026-10-08): 139 pass, 0 fail, 0 setup** (handoff `Argo_round6`, a7f2322). The generator rebuilt every
Deep Water model with its `Swell` part, and "Deep Water's water bobs gently" passed (rose and fell 0.0182, amplitude
0.012). No mod exceptions. Then the playable build (no defines) was rebuilt and boot-checked with Crackpot, Mummer,
Rustle, Sprigg (every vessel's icons line, 0 exception lines) and left **installed and enabled**. The journal's tipped
Deep Water now reads as a small tipped pool in the journal (`Previews/round6/`), no longer a line.

**Argo 2.0 stands here (Phase 3 done).** Dimitri, 2026-10-08: Argo ships only inside the full `RogueWavesMod` pack (no
ArgoMod 2.0 zip), and its balance waits for a later pass of his. Lessons went to kit `docs/09` #72-76 and `docs/03`
§4a. Left for the pack: Argo's prefab joins `RogueWavesMod` (`docs/PACK.md`), and hand-play balance.

SONAR's 1.4.0 code stays in the source, switched off by `SonarEnabled = false`. Installing the dev build replaces ArgoMod 1.4.0 on the PC; the 1.4.0 release can be
put back from its export if needed.

Everything below is 1.4.0.

**1.4.0 (identity pass): ninth PC self-test 103 pass, 0 fail (2026-10-05); released and installed on the PC the same
day with Crackpot 1.1.0, Rustle 1.1.0, Mummer 1.5.0, Sprigg 1.7.0 and Riptide 0.2.1 (boot clean, no mod exceptions).
Not yet hand-played.** The design and the why are in `DESIGN.md` ("Changes in 1.4.0").

First PC self-test of 1.4.0 (2026-10-04): 94 pass, 4 fail, all the test's own foe placement. On Thehunt's hill there is
no free lane 5 tiles out from the Harpoon Tower (4 was free), and Pay Dirt judged its foes by their position before
their first frame. `Lane` now steps in to 3 tiles; `FoeBetween` picks the tile by its own position.

Second PC self-test (2026-10-04): 95 pass, 6 fail, all the test's again. The reel lane, 3 tiles out, had no tile to reel
to, so the harpoon hit without reeling, and the foe behind it crawled 0.47 tiles at the 0.05 Speed floor during the
wait. The second Deep Drill found no free tile on the guns' small hill. Wide Bore read 27 Red spent of 30 (a foe felled
meanwhile paid 3). Pay Dirt's foes were placed and killed but read as null once destroyed ("no foe placed"). Now the
lane must leave the first foe a tile to reel to (the reel's own search, `ReelRoom`), the reel check takes 0.7 tiles or
more (a reel stops within a quarter tile of its tile), the foe behind is measured from the hit, the second drill may go
on the terraces 4.5 tiles round, enemies are cleared before Wide Bore is paid, and Pay Dirt notes placement before the
kill.

Third PC self-test (2026-10-05): 99 pass, 1 fail. The reel is verified in game: Anchor Line's foe, 3 tiles out, was
reeled and held where it landed. The one fail: on the Harpoon Tower's hill no level lane of two foes leaves the first a
tile to reel to, so the two-foe check had no foes. With no room for two, the reel is now checked on one foe and the
pierce check is skipped.

Fourth PC self-test (2026-10-05): 100 pass, 2 fail. Lane tiles had also been allowed to step a level: the lane found
then, a level down 4 tiles out, was never reeled (neither in the reel check nor Anchor Line), most likely never
harpooned. Lanes are at the tower's level again (round 3's 3-tile lane reeled), and a lane with no reel logs how many
harpoons were fired.

Fifth PC self-test (2026-10-05): 97 pass, 4 fail, all the test's. The harpoon hit the lane's first foe 3 tiles out, 3
times, without reeling it: a walking foe stands, for the game, on the tile it is stepping onto (claimed as the step
starts, even at the Speed floor), so the held foe was already on the tile 2 out, and from there the only tile toward
the tower is 1 tile from it, inside the 1.2-tile stop. That is the reel as designed (Anchor Line's foe, 4 tiles out,
was reeled and held in the same run), so the lane now needs reel room from the tile in front of the first foe too, and
with no two-foe lane that far out on the hill the reel is checked on one foe 4 tiles out and the pierce in a second,
closer two-foe lane. The other three: the second Deep Drill found no tile within 4.5 (now 7, its Tremors widened to
reach both guns); the harpoon reeled the Tremor test's foe and the reel's +5 Speed hid the slow (the guns are jammed
for that check); Wide Bore read 27 Red spent of 30 with the forced Tide's foes paying meanwhile (only the drops in Red
are counted now).

Sixth PC self-test (2026-10-05): 96 pass, 3 fail. Those three fixes passed (second drill placed, Red spent 30, Tremors
1 -> 0.76), but this run's Harpoon Tower stood on the top of the hill, (-1, 11, -27), with no level lane at all that
leaves room to reel: no foes for the reel, the pierce or Anchor Line. 1.4.0 wasn't released; Argo 1.3.0 stays
installed while the other four vessels were released. The Harpoon Tower is now built where both lanes have room
(`HarpoonSpot`: one foe with a tile to reel to, two for the pierce, within 5 tiles of the vessel, else 8), and those
lane tiles are kept free of the other buildings.

Seventh PC self-test (2026-10-05): 100 pass, 1 fail. The reel is verified: the lane's foe, 3.9 tiles out, was reeled
to 2 tiles in 0.24 s; the foe behind it was pierced and stayed put; the Boss-weight foe was hit and not moved; Anchor
Line held its reeled foe at the Speed floor. The one fail, again the test's: the second Deep Drill found no tile within
7 of the guns (as in the second and fifth runs), so the two Pressure checks didn't run. `AnySpot` now tries every level
of each column from 2 above to 6 below, out to 10 tiles, and logs why when it finds none; the drill's Tremors are
widened to reach both guns wherever it stands. `Spot` (every other building and showpiece the test places) falls back
to the same search, as far out as it was asked.

Eighth PC self-test (2026-10-05): 101 pass, 2 fail. Placement is fixed: the second Deep Drill stood at (0, 12, -25)
and both Pressure checks passed (Fathom 3, pellets 8, pierce 5). The two fails were a real bug in the mod: the harpoon
reeled the foe behind the first (the first moved 0.02, the one behind 1.9 tiles). The game's projectile sweeps each
step with `SphereCastAll` and hands the hits over in the array's order, not by distance, so "the first foe hit" was
whichever came first in the array. The reel now waits for the rest of that step's hits (one frame) and goes to the
one nearest the tower (`ReelNearest`; kit docs/09 40b).

Ninth PC self-test (2026-10-05): 103 pass, 0 fail, nothing skipped. The reel took the lane's first foe from 3.9 tiles
to 2 in 0.07 s and the foe behind stayed put (moved 0.003); Anchor Line held and let go; the Boss-weight foe wasn't
moved; Pressure, Old Salts and every other check passed. Released (`Argo_TDS_Mod_v1.4.0.zip`).

- **Reel** (`ArgoUtil.Reel`, a port of Mummer's HOOK reel): picks the farthest standable tile up to 1.5 tiles toward
  the tower (Surface lookup, `Checkifstandable`, a `Getpath` no longer than ceil(k)+2), boosts the foe's Speed +5
  Multi and re-steers its Move command every frame (`ReelRun`, one coroutine per foe, a `reeling` table so a foe is
  reeled by one harpoon at a time). The boost is removed when the foe arrives, before `onDone` runs: left on, Anchor
  Line's -1 hold would leave it at five times its speed. Every non-lance harpoon's shot id goes in `reelShots` (8
  kept; it replaces `anchorShots`), and `OnHit` reels the first foe that shot hits.
- **Load** ("Pressure" in code): `CheckPressure` on Scatter and Harpoon brains every second, and right after a Tide
  (`OnTide` zeroes `nextPressure`); one Add modifier on Projcount or Pierce, swapped when the depth changes; Vis
  `Argo_Pressure` when it rises.
- **Double Barrel**: a full reload with no target sets `Stored`; `Volley()` was split out of `Scatter()` so the stored
  volley can follow `DoubleDelay` later.
- **Old Salts**: `ArgoNautBrain.StartTune` adds one `saltMod` by the post's being and sets the Harpoon's `TunedReel`;
  `StopTune` removes both. Hard Hats' Naut health modifier and its journal label are gone.
- **Pay Dirt**: `ArgoServer.OnDespawn` calls `PayDirt` on every Pay Dirt drill when a killed "Enemy" despawns; the
  drill bites its own ore `Paydirt_Base` times if the foe fell inside its Tremors.
- New Vis types `Argo_Reel` (rope + `Argo_Winch`), `Argo_Pressure`, `Argo_Loaded`, `Argo_Paydirt`; new synth
  `Argo_Winch` in `ArgoAudio.cs` (quiet, `SoundTarget` 0.0012). Models `_Double2`, `_Salts2`, `_Paydirt2` replace
  `_Choke2`, `_Hardhats2`, `_Rotary2` (key order kept); the Rotary Bit's gold hoop on the chuck is gone.
- Self-test: new sections 4b (reel, pierce, boss) and 6b (load, with a second drill that is sold), plus Double Barrel,
  Anchor Line, Pay Dirt and Old Salts in section 8. The Wide Bore and Skewer checks now add any load from the first
  drill. Not run yet: the PC self-test is next.

**1.3.0 (quality pass leftovers), changes and why:**

| 1.2.2 | 1.3.0 | Why |
|---|---|---|
| Harpoon Tower Damage 9 (+3/level), Range 8 | Damage 5 (+2/level), Range 6.5; Attackspeed 0.35, Pierce 3, Projspeed 18 kept. Single-target DPS 3.15 → 1.75 at level 1 (4.2 → 2.45 at 2, 5.25 → 3.15 at 3); a line of 3 foes 9.45 → 5.25 | Both sat above the shipped tower bands (Damage 0.3-5, Range 3-6.5); now at their top, next to the Sno Cannon (5+1, 6.5, 3.5 DPS) and the Archer (4+1, 6, 2.8 DPS), with pierce as its edge. The shown range follows the Range stat (`ModKitTowers.Fit`, self-test range check) |
| Diving Bell 35 Blue with a tree priced mostly in Yellow (Flippers 30 Yellow; Overhaul 90 Yellow + 30 Blue; Calibrate 110 Yellow; Third Mate 300 Yellow; Full Crew 2500 Yellow + 500 Blue) | Tier 2 Blue only (Flippers 30 Blue, Hard Hats 30 Blue); above it Blue first, Yellow second: Overhaul 90 Blue + 30 Yellow, Calibrate 80 Blue + 30 Yellow, Third Mate 200 Blue + 100 Yellow, Bulkhead 250 Blue + 100 Yellow (unchanged), Full Crew 2500 Blue + 500 Yellow. Every total kept | Cost colour = job: the Bell's Nauts tune, repair and armour towers (support, Blue) |
| Vessel Damage 5 | 4 (Smacky's, the highest shipped); Health 65 kept | docs/02 asks for lower damage when the vessel has another damage source, and every Argo swing cleaves a second foe. The bulky Nautilus health the user asked for stays |
| `Root` was a -0.9 slow; slows were raw `Speed` Multi mods | `Root` is -1 (Speed at its floor); `Slow` is cut so slows alone stop at 10% of a foe's speed (skipped on a held foe, the slow look still plays); both raise a `Speed.Value_Bounds.x` under 0 to 0 (`ArgoUtil.Speed`) | Two overlapping full-strength Tremor ticks, or a slow on a rooted foe, crawled it at the game's own Speed floor (enemy Speed carries Limits 0.05..99999, read in game, so it never went negative); slows should never become a hold, and a root should hold (docs/09 #79) |
| A brain's catch logged and set `enabled = false` on its first exception | `ArgoFaults`: the tick is skipped; a brain stops only after throwing on every frame for 600 frames (~10 s) | One bad tick silenced a tower or a Naut for the match (docs/09 #80) |

Not changed, noted: Whaler still adds +4 damage (now on 5, so 9 at its tier); Tremor ticks (every 0.5 s, each lasting
0.75 s) overlap for a quarter second, so a foe in them alternates between one and two slows (now floored at 10%).
New self-test checks: vessel Damage 4; Harpoon stats 5 / 6.5 / pierce 3 / 0.35; Long Line → range 8.5; the Bell tree's
colours; the speed floor (two full-strength Tremor slows = 10%, hold + slow = the foe's floor, back to full speed). Self-test **84/84** (the Harpoon's shown range circle 13 across, centred on it); release boot
with all five installed vessel mods: ready, own icons, 0 exceptions from any mod. `Desktop\ClaudeProjects\Argo_TDS_Mod_v1.3.0.zip`.

**1.2.2 (quality pass):** text only. Deep Drill, Rotary Bit, Deep Core, Gusher, Broadside, Leviathan Lance, SONAR and
the Fathoms tooltip reworded without numbers (no base-game description carries one; the journal's EFFECTS lines keep
them, Gusher's gained "From Fathom 3 down"). Self-test 77/77; release boot with Sprigg 1.6.2 and Mummer 1.3.1: 0
exceptions. Fable reviewers' open notes, not changed: the Harpoon Tower's Damage 9+3 and Range 8 sit above the shipped
tower bands (a slow, piercing design choice from two playtests); the Diving Bell is Blue while its tree is priced
mostly Yellow; stacked slows and roots are Multi mods that can sum past -100% (never seen in play).

Argo, **Fathoms Below ▪ Intermediate**: a bulky deep-sea diver (built in the spirit of League's Nautilus) with a
crew of Nauts, a scatter cannon, a harpoon tower and a drill that sinks deeper every Tide. Built from the concept in
`Concept/` (the user's "Argo Vessel Concept.zip") on Sprigg's proven scaffolding. Read
`TDS_Vessel_Creator/reference/VESSEL_SURVEY.md` for how the shipped vessels are built.

## Layout

2.0 (current): the table below is 1.4.0's. 2.0 builds against kit 1.6, adds `11_Water` (Deep Water) and
`16_AimSelect` (AIM: the Drill Rig's aimed Tremor), keeps SONAR's files switched off, and `99_SelfTest` has 139 checks (PC round 6).

All code lives in `Code/*.cs` plus the shared kit in `../Shared` (kit 1.3 at 1.4.0). `Build-ArgoMod.ps1` joins the files in name
order, then the kit with its `ModKit` prefix renamed to `ArgoKit`, into one Roslyn string; the first type is the entry
point.

| File | Role |
|---|---|
| 00_ArgoMod | Boot: add icons early, wait for the template hero, load Smacky's asset group, build content, diagnostics; ticks the kit's vessel-tab sync and boat |
| 01_Util | Helpers: stats and counters, skins, queries, JSON parsing, `Root`/`Slow`, `Hit()` damage dictionaries |
| 02_Data | Every name, text, stat, upgrade tree (explicit `next` lists and costs), tuning number and sound target |
| 03_Assets | Asset-pack access: sprite slicing, Spriteobjects (vessel, Nauts and their Tune skin), tower material, icons, auto-levelled sounds, particles |
| 04_Content | Builds and registers the vessel, buildables (stats from data), Nauts, projectiles, SONAR and crew commands, the tooltip upgrade |
| 05_Server | Host rules: brain attachment, Tides, the Naut roster ("one Naut per tower") |
| 06_HeroBrain | Vessel: build options, SONAR (cooldown shown on the button), the second hit of each swing |
| 07_TowerBrain | Scatter volleys, harpoon lines, the Diving Bell's crew, the Deep Drill (mining, Fathoms, Tremors, quakes), upgrades (mirrors General_Upgrade) |
| 08_NautBrain | A Naut walks to its tower with the stock movement AI, tunes it, repairs it, comes home |
| 09_Hooks | Pinged (+20% damage taken) and Bulkhead (-25%) via the target's hit event |
| 10_Client | All players: turret aiming and recoil, drill spin, Tremor rings, tuning marks, SONAR ping marks, effects, sounds, owner warnings |
| 12_Diagnostics | One-time Player.log dump of the built data |
| 13_Journal | `ArgoJournal`: fills the kit's `ModKitJournalSpec` for Argo (1.0's own generic VesselJournal/VesselTabs became the kit, so Argo now uses the kit's) |
| 14_CrewSelect | PORT / STARBOARD / BOSUN: pick any tower on your team (Itemselect, like Sell/Reroute); pick the bell to recall |
| 15_SonarButton | SONAR's button: `Entity_Command_Simple` that greys out while recharging |
| 97_Shot | `-Define ARGO_SHOTS`: full-frame and close-up (own camera) captures as `[ArgoDump]` log lines |
| 99_SelfTest | `-Define ARGO_SELFTEST`: starts a solo match and checks every mechanic (77 checks in 1.2: Fathom growth, kill credit and the client's Tremor slow added) |

Editor side (`Assets/ArgoMod/Editor`, namespace `ArgoBuild`): `ArgoCharacter` (pixel art), `ArgoModels` +
`ArgoLowpoly` (towers), `ArgoAudio` (synthesised SFX), `ArgoBuilder` (Tools > Argo > Build All Assets, Preview
Models), `ArgoPreview` (Preview Sprites), `ArgoModPrefab` (Build Mod Prefab, Export Mod, Restore Parked Mods).

## How Argo is built

- **Vessel.** Smacky's AIs (Mob_General, Mob_Movement, Mob_Combat, Hero_Buildsell) + visscript Entity_Vis_Mob_Attack
  (turns each swing's "Attack" into the wrench animation). Stats: 65 HP, speed 2.25, damage 5, range 1.75, attack
  speed 0.4. **Second hit:** `Helper_Entitytools.Interacthurt` fires the attacker's `Entity_Master.Interact` before the
  target takes the hit, so the brain carries 60% of each swing to the nearest other enemy (flagged `Argocleave`).
- **SONAR** (keys 6/7 like Buster's Dash): ping radius 6, Pinged 5 s, 15 s cooldown. The button's Goalstring is "Off"
  while recharging (synced through `Entity_Master.Commandlist`, the way the game's on/off toggles work).
- **Pinged / Bulkhead.** `ArgoHooks` puts its handler first in the target's `Oninteract` chain and scales
  `Damagemulti` on a *copy* of the hit's Data (projectiles reuse one dictionary for every pierce).
  `Helper_Entitytools.Updateinteract` rebuilds the chains on every upgrade or effect, so handlers are re-checked
  every 0.1 s (targets) / 0.25 s (towers, vessel).
- **Scatter Cannon / Harpoon Tower** fire the game's own projectiles (clones of the Heavy Bow arrow,
  `Projectile_Ai_Simple`, native `Pierce`). Each shot passes *every* parameter: the game merges a shot's parameters into
  the Projectileobject's shared `Defaultparams`. Clients swing the model's `Turret` part toward each target
  (`Argo_Scatter`/`Argo_Harpoon` Vis) - the bespoke turret AIs are gone, so aiming is ours.
- **Deep Drill** mines ore with the game's own ore damage (`Interacthurt` on the nearest `Resource` within 2.1 tiles,
  like the Sno Box), so the aura it digs out goes to the team exactly as mining does. Fathom 1 -> 5 (7 with Deep
  Core), one per Tide; each Fathom adds +75% ore per strike and +10% strikes a second (Multi mods), +0.4 tiles of
  Tremor and +4% Tremor slow (capped at 60%): about 1.9x, 3x, 4.2x, 5.6x the first mining rate at Fathoms 2-5. Skins
  `Fathom2..7` carry the depth to clients (overlay `Argo_Fathom2..5`, the deepest look reused past 5), and the
  owner gets a message per Fathom saying what it brought (`ArgoClient.FathomNote`, from the effect's own fathom).
  Rotary Bit and Mantle Tap are `Pmulti` (they scale the base), so they keep their full value at any Fathom.
  Its `Range` stat *is* the Tremor radius, so selecting it shows the ring; clients draw a dust ring at the same radius
  (rebuilt from the skin and upgrade list, test: "client Tremor radius matches the server's").
- **Diving Bell / Nauts.** Nauts are clones of Smacky's Engineer with only Mob_General + Mob_Movement. The brain sends
  them like the Mining Hut sends miners: `Move` command with `Goalobject` = the tower, `Goalposition` = -1, then
  `Updatecommand(..., true, true)`. A Naut within 1.75 tiles tunes: +20% attack speed (Full Crew +40%), heal 0.5/s
  (+2 with Overhaul), +1 range (Calibrate), -25% damage taken (Bulkhead), skin `Tune` (wrench-tapping Idle). One Naut
  per tower across every bell; picking the bell recalls; knocked out = back in 8 s and resumes its post. Full Crew
  "dives": despawn, respawn on a standable tile beside the post.
- **Icons.** The game copies its icon sets into a private table on first use and never again. Argo adds its icons
  the moment the database exists (before any asset loading) and checks 8 s after boot; if the table was already
  frozen, the buttons fall back to stock icons (never blank).

## The journal, for every future vessel (1.0, history: since kit 1.1 this is `ModKitVesselJournal`, kit `README.md`)

`13_Journal.cs` is vessel-agnostic:

- `VesselJournal` takes a `VesselJournalSpec` (hero id, template vessel, rule upgrade, lore, portrait, tab icon,
  accent colour, buildings with their upgrade trees and units, extra EFFECTS labels) and builds the full page set by
  cloning the template vessel's pages: vessel page (idle portrait like `Smacky4-Sheet_0`, the rule banner where Sno
  Sno's sits, stats, lore, TOWERS & UNITS for any number of buildings), building pages (RELATED ITEMS only for unit
  towers), upgrade pages (EFFECTS, NEXT UPGRADES) and unit pages.
- `VesselTabs.Sync()` keeps one tab per vessel in the Hero folder on every page with a vessel-tab row: base vessels
  first in the game's order (Smacky, Sno Sno, Buster, Boutman, Fie), then modded vessels by id; it adds missing tabs,
  drops duplicates and sorts. It is idempotent and order-independent, so every vessel mod can run it (Argo runs it at
  build and every 3 s). Verified with Sprigg 1.3 installed: 7 pages, identical tabs, no duplicates.
- For a new vessel: copy `13_Journal.cs`, rename `ArgoJournal` and fill the spec from its own data. Rename the two
  generic classes per mod if you want (each mod compiles into its own assembly, so the names do not collide).

## Status

- 1.2.1 (2026-10-03, art audit against kit 1.3 and Sprigg): the vessel sprite was 28×31 px (bigger than every base
  vessel and the concept's 24×28) → `ArgoCharacter` now draws it 24×28 (`Drop`, `HelmRx/HelmRy`, narrower torso, pads,
  sleeves and gauntlets, hands still on the sleeves); the boat sprite went from 15×14 to 11×12. Harpoon barrels
  shortened (tips 0.55-0.67 from centre, was 0.70-0.84) so the turning gun stays near its tile and its tip meets the
  shot's spawn point (0.35-0.4 out). Code unchanged apart from `Version`; compile check passed, exported and enabled.
  Not yet booted in game.
- 1.2 (2026-10-03): self-test run 1 passed **77/77** (Fathom 2: 0.875 ore a strike, 0.275 strikes/s, 24% slow;
  Rotary at Fathom 2: 0.385 strikes/s as expected; Fathom 7 skin and client Tremors 4.4 = server; second hit
  recorded as Argo's). The journal's upgrade page shows the new EFFECTS line. Release build booted with Sprigg 1.6
  and Mummer 1.2: all three ready, 0 exceptions.
- Load: clean (`[Argo] ready ...`, icons Argo's own, journal 36 pages) alongside Sprigg 1.3.
- Self-test run 2 (2026-10-02): 63/64 (the miss was the harness reading an enemy's speed on its spawn frame; fixed).
- Tower sizes were matched to the shipped towers measured in game (Sno Sno 0.75-1.15, Buster 0.85/1.9, Smacky 1.0-2.05):
  scale baked into the meshes about the ground point (`ArgoModels.*Scale`).

## Build, deploy and test

```
powershell -ExecutionPolicy Bypass -Command "& .\Build-ArgoMod.ps1 [-Define ARGO_SELFTEST,ARGO_SHOTS]"
```
Then in Unity (or via UnityMCP): Tools > Argo > Build Mod Prefab, Tools > Argo > Export Mod, and after the export
lands, Tools > Argo > Restore Parked Mods (the export parks other `<Name>Mod` asset folders under
`Assets/_Parked/Editor/` because ModTool exports every prefab and ScriptableObject outside `/Editor/` paths).
```
powershell -ExecutionPolicy Bypass -File Deploy-Wait.ps1 [-Launch]
```
Results go to `%USERPROFILE%\AppData\LocalLow\Blockage\TDS\Player.log` (`[Argo] TEST PASS|FAIL`, `TEST DONE`);
`Tools\Decode-Dump.ps1 -Log <copy of Player.log> -Out <dir>` turns the `[ArgoDump]` captures into PNGs.
Always rebuild without defines before shipping. **Sprigg's own Export Mod does not park ArgoMod**: park it (move
`Assets/ArgoMod` under `Assets/_Parked/Editor/`) before exporting Sprigg again.

## Changes in 1.1 (playtest feedback, 2026-10-03)

- **Ranges centred on the towers.** The game draws a tower's range as a sphere centred on its hit prefab's `Detect`
  child (while placing and when selected). Argo 1.0's towers kept the templates' hit prefabs: the Scatter Cannon's
  (Smacky's Bomb Tower) puts `Detect` 2 tiles underground, the Harpoon's and Drill's (Archer Tower) 0.75 up. Now
  `ModKitTowers.Fit(ent, id, model, Def.Eye, ...)` gives each tower its own hit prefab: `Detect` at `Def.Eye` above
  the tower (Scatter 0.12 and Harpoon 0.3, their muzzles; Drill 0, its Tremors), collider fitted to the model. The
  brains measure range from the same point (`ArgoTowerBrain.Eye`).
- **Steep shots.** `Fire` used a level direction and spread the height difference over the whole range, so shots at
  foes below flew over their heads. Shots now fly along the 3D line from the muzzle to the target's body (the cone is
  fanned around the vertical; harpoon lines are counted in 3D). The client pitches the turret (30° down, 20° up).
  Verified: the Harpoon hits a foe 3 tiles below.
- **SONAR** synced its button with `Updatecommand(..., run: true)`, which re-runs the command: it warned "recharging"
  on every use and cast itself again whenever the recharge ended (quaking nearby drills, i.e. camera shakes). Now
  `run: false`; the self-test checks the recharge doesn't fire it.
- **Debuff looks** (slows, holds) used `Beingrefparallel`, which clients can't resolve: now `ArgoUtil.Debuff` with
  the synced `Beingref`.
- **Deep Drill**: a brass crack runs from the drill to the ore it bites (`Argo_Mine`); texts say "ore up to 2 tiles
  away" and "Tremors slow foes in range" (the shown range).
- **Kit**: journal (`ModKitVesselJournal`, `ModKitVesselTabs`) and boat (`ModKitBoat`) from the shared kit; Argo's own
  copies are retired (`../Archive/Argo_1.0/11_World.cs`).
- Self-test run 1 of 1.1: 72/73 (the miss was the harness failing to place a foe right after a Tide; it now retries).

## Changes in 1.2 (second playtest, 2026-10-03)

- **Fathoms grow much more per Tide** (playtest: "could improve per tide much more"). 1.1 gave +50% ore per strike and
  +0.5 tiles a Fathom, three Fathoms deep (2x mining at most). See the Deep Drill bullet above for the new numbers
  (`ArgoData.Fathom*`). `ApplyFathom` adds Damage and Attackspeed `Multi` mods plus Range and Slow `Add`; the client
  computes Tremor radius and slow from the drill's upgrades and Fathom (`DrillStat`), checked against the server.
- **Multi vs Pmulti.** `Value_Calc = (Base * Pmulti + Add) * Multi` and Multi mods add up, so 1.1's Rotary Bit (Multi
  +0.4) and Mantle Tap (Multi +1) were diluted by the Fathom's Multi. Both are `Pmulti` now.
- **Hits that counted for nothing.** The second hit, Barbed bleed, Aftershock pulses and the Leviathan Lance's hits
  (data that already carries `Damage`) set no `Lasthit`/`Lasthittime`: no kill credit, and an Aegis regenerated under
  them. They go through `ModKitCombat` (`Hurt`; the towers' `OnHit` calls `Mark`).
- **Upgrade texts.** Every Desc rewritten against the code ("mines" ore, not "bites"); `Upg.Fx(...)` gives the journal
  plain EFFECTS lines; journal labels "Gusher Strikes" and "Quake Hold".