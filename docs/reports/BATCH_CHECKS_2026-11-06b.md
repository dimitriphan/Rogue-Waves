# Batch checks (2026-11-06)

Machine-read by `claude-haiku-5-5` (effort low) through the Message Batches API, batch `msgbatch_01K9BgzwscBGLn6Zq1cQekKt`, on base `86988e1`, checks scope, design, by `TDS_Mod/Tools/batch/run_checks.py`. Cost: $0.63 at batch prices. Every row is a lead to confirm by hand before it changes code, docs, art or the scope: the model reads, it does not run the game. Nothing ran on the PC.

## 1. Scope against code

Recipe: `docs/reports/SCOPE_CODE_AUDIT_2026-11-05.md` (marks as there). Code paths are relative to `TDS_Mod/ModSource`.

| File | MATCH | DIFFERENT | MISSING | UNBUILT-QUEUED | UNBUILT-BY-DESIGN |
| --- | --- | --- | --- | --- | --- |
| 07_Vessel_Yossele.txt | 133 | 2 | 0 | 0 | 1 |
| 08_Vessel_Spool.txt | 75 | 4 | 2 | 19 | 4 |
| 11_Directions.txt | 50 | 5 | 0 | 0 | 0 |

### 1.1 DIFFERENT and MISSING (to confirm by hand)

| File | Lines | Says | Code | Mark | Note |
| --- | --- | --- | --- | --- | --- |
| 07_Vessel_Yossele.txt | 17 | Status: built as 0.2.0-dev | `02_Data.cs:8` | DIFFERENT | Version is 0.1.0-dev in code; the scope and PLAN say 0.2.0-dev |
| 07_Vessel_Yossele.txt | 149 | Garden Weeds numbers: stacks up to Growth, +10% each; debuff lasts 3 seconds; chance 40% | `02_Data.cs:314; 09_Dandelion.cs:35,58,335-339` | DIFFERENT | Weeds_Last=15 makes a stack last 15 s; 3 s is only Weeds_Window, the time after a seed hit that counts. Stacks up to Growth, 10% each and 40% chance do match |
| 08_Vessel_Spool.txt | 10 | Archetype: Commands a disposable minion army | `-` | MISSING | Looked for an archetype text in the hero entry and journal; none. Nothing queued. |
| 08_Vessel_Spool.txt | 14 | Accent sage 93CCA1 and 60A871 | `04_Content.cs:384` | DIFFERENT | 93CCA1 matches Sage at 04_Content.cs:384. 60A871 is not a constant; the thread colour is 6FAE7E (11_Client.cs:14). |
| 08_Vessel_Spool.txt | 22 | Health regen: 0 per second | `-` | MISSING | No regen stat set in HeroStats. Nothing queued; the engine default applies. |
| 08_Vessel_Spool.txt | 64 | Limit: 1 | `07_Building.cs:156-157` | DIFFERENT | Nothing enforces one Stash. A second one is allowed and only warned; the newest holds the Thread. |
| 08_Vessel_Spool.txt | 276-280 | Pin Barrage: 20% ranged reflect; leads to Iron Thimble; Blue 100 | `02_Data.cs:317-318` | DIFFERENT | Ranged 20% is built ('Rangedreflect_Base=0.2', 'Blueaura=100'), but Pin Barrage's Next list is empty, so it does not lead to Iron Thimble (which is unbuilt). |
| 08_Vessel_Spool.txt | 349 | Directions: Efficient Recycler, Sewing Machine; and Made of Tougher Stuff for any vessel with minions | `04_Content.cs:658-686` | DIFFERENT | Recycler and Sewing are built. 'Made of Tougher Stuff' is not in the code (searched BuildDirections and SpoolData) and no plan item queues it. |
| 11_Directions.txt | 11:108-118 | Spare Hands: unit buildings keep +1 unit (rank I), units respawn sooner (rank II, -25%), +2 units and -40% respawn (rank III); clashes with Sno Sno; Needs a building that makes units | `16_Directions.cs:57-58 (Needs Units); 167 (Meets Units excludes Hero_Snosno and Hero_Yossele); 22_AnyDirections.cs:43-44 (HandsUnits 0/1/1/2, HandsRespawn 0/0/0.25/0.4), 288-306 Hands` | DIFFERENT | Code also excludes Yossele (Meets line 167) and the scope says only Sno Sno. Yossele is not in the scope's clash. Numbers match. |
| 11_Directions.txt | 11:297-307 | Compost Bin (Yossele): Shift, unlocks FERTILIZE; rank I 1 fertilizer per 10 foes within 4 tiles; rank II per 6 within 4; rank III per 6 within 7; desc text | `Yossele/Code/02_Data.cs:102-110 (Compostbins Kills 10/6/6, Radius 4/4/7); Yossele/Code/05_Server.cs:101-107 CompostRank, 120-140 Compost` | DIFFERENT | Numbers match. The description text differs: scope says 'Unlocks FERTILIZE for Yossele if not already unlocked, and gives a stack of fertilizer per x enemies killed in a radius around Yossele'; code desc is shorter (02_Data.cs:105-108). Look is art. |
| 11_Directions.txt | 11:308-318 | Dandelion Season (Yossele): Tree becomes Sapling, no limit, 70% damage/range/stages; rank II 25% more per stage; rank III +5% per other Sapling at max Growth up to +50%; clash with other Tree Directions | `Yossele/Code/02_Data.cs:125-137 (Seasons, SaplingShare 0.7, SaplingStage 0.25, SaplingPack 0.05, SaplingPackMax 0.5); Yossele/Code/09_Dandelion.cs:353-393 Shift and OnGrowth; Yossele/Code/05_Server.cs:335-343 TreeSlot; Yossele/Code/16_Directions.cs n/a; Clash 1 in 04_Content.cs:645-651` | DIFFERENT | Numbers match. Rank I desc differs: scope 'slightly weaker statwise and don't scale as well as the Tree'; code 'slightly weaker and slower to grow'. Rank II and III texts match. |
| 11_Directions.txt | 11:363-373 | Deep Breath (Yossele): after 2 s without firing, next beam +40/+70/+100%; rank III +1 tile range for that beam | `Yossele/Code/02_Data.cs:181-188 (BreathBonus 0.4/0.7/1, BreathAfter 2, BreathRange 1); Yossele/Code/17_Directions.cs:27-35 Charged (reload + BreathAfter), 52-54 reach, 64-92 OnDealt` | DIFFERENT | The 2 s counts from when Yossele could fire again (reload plus 2 s), not from the last shot. The code comment says this is deliberate; the scope line says 'after 2 seconds without firing'. Bonus and range numbers match. |
| 11_Directions.txt | 11:386-396 | Efficient Recycler (Spool): rank I, II and III increase Thread gained from Unthreaded minions; rank III heals units and towers round it; Thread back 65/80/100%, heals 10 within 2.5 tiles | `Spool/Code/02_Data.cs:102-111 (Recyclers Percent 65/80/100, Heal 10, Radius 2.5 at rank III); Spool/Code/08_Tin.cs:570-586 Gone Sell refund and MendAround; Spool/Code/05_Server.cs:319-334 MendAround` | DIFFERENT | Numbers and the rank III heal match. The rule differs: scope ties the Thread to 'Unthread' (UNTHREAD, queued as B19), the code gives back Thread on selling a Crafted. The Unthread mechanic is UNBUILT-QUEUED under B19. |

### 1.2 07_Vessel_Yossele.txt

| Lines | Says | Code | Mark | Note |
| --- | --- | --- | --- | --- |
| 2 | Yossele, The Patient Golem (name) | `02_Data.cs:67-68` | MATCH | Displayname and Displaysub |
| 5 | Note: defaults Claude picked | `02_Data.cs:1-4` | MATCH | Meta note on defaults; the file header says the same |
| 8 | Title: The Patient Golem | `02_Data.cs:68` | MATCH | Displaysub |
| 9 | Difficulty: Intermediate | `02_Data.cs:69` | MATCH | Displayskill |
| 10 | Archetype: towers grow from bonuses kept alive, scaling growing-defence | `02_Data.cs:72` | MATCH | TooltipDesc and the Growth rule carry it |
| 11 | In-game line: Sturdy, single-target ranged that mends itself | `02_Data.cs:70` | MATCH | HeroDesc |
| 12 | Look: clay head, mouth hole, two-leaf sprout, green eyes, four root limbs | `03_Assets.cs:6-26` | MATCH | Hero sprite loads Yossele-Sheet; the look itself is art (temporary sheet), not checkable from code |
| 13 | Towers: round holes, root feet; each Growth stage adds leaves or sprouts | `04_Content.cs:179-197` | MATCH | Yossele_Growth skins per stage; the model art itself is not checkable from code |
| 14 | Accent colour: leaf green | `04_Content.cs:24` | MATCH | Accent 72CC35 |
| 15 | Sprites: NEEDS YOUR FULL SPRITE SHEET (scope's own NEEDS slot) | `03_Assets.cs:6-41` | UNBUILT-BY-DESIGN | Scope says the first release waits for it; temporary sheets are used |
| 15 | Sprite sizes: 32 px cells for idle, move, attack, fall, land; attack 4 frames | `03_Assets.cs:9-22` | MATCH | Frames(...,32) for each sheet; Attack anim uses 4 frames |
| 16 | Lore comes once every character is fully realized | `02_Data.cs:73-74` | MATCH | Placeholder Lore text as DESIGN.md says |
| 17 | Status: built as 0.2.0-dev | `02_Data.cs:8` | DIFFERENT | Version is 0.1.0-dev in code; the scope and PLAN say 0.2.0-dev |
| 20 | Attack: ranged, green laser from mouth | `04_Content.cs:309-327; 06_HeroBrain.cs:85-95` | MATCH | BuildLaser clones the snowball; Arm names it on the Attack command |
| 21 | Health 70 | `02_Data.cs:85` | MATCH | Health=70 |
| 22 | Health regen 1 per second, only while hurt | `02_Data.cs:87; 06_HeroBrain.cs:98-109` | MATCH | Regen=1; returns when nothing is missing |
| 23 | Speed 1.75 | `02_Data.cs:85` | MATCH | Speed=1.75 |
| 24 | Damage 5.5 | `02_Data.cs:85` | MATCH | Damage=5.5 |
| 25 | Range 4 tiles | `02_Data.cs:85` | MATCH | Range=4 |
| 26 | Projectile speed 15 | `02_Data.cs:85` | MATCH | Projspeed=15 |
| 27 | Attack speed 0.5 | `02_Data.cs:85` | MATCH | Attackspeed=0.5 |
| 28 | Build range 2.5 tiles | `02_Data.cs:85` | MATCH | Buildrange=2.5 |
| 29 | Sell rate 50% | `02_Data.cs:85` | MATCH | Sellrate=0.5 |
| 30 | Special rule: Patient Growth, tooltip text | `02_Data.cs:71-72; 04_Content.cs:681-704` | MATCH | Tooltip upgrade at tier -3 with the scope's text |
| 33-34 | Fertilize: +1 Growth stage for 1 fertilizer | `06_HeroBrain.cs:205-258` | MATCH | Grow(1,true) after AddFertilizer(-1) |
| 35 | Target: one of your buildings; hotkey takes nearest that can still grow | `06_HeroBrain.cs:209-230` | MATCH | Owner check and nearest-growable search |
| 36 | Range 6 tiles for the hotkey; a clicked building at any distance | `02_Data.cs:97; 06_HeroBrain.cs:210-230` | MATCH | FertilizeReach=6 only in the nearest search; a picked building skips it |
| 37 | Cooldown 0.5 seconds | `02_Data.cs:98; 06_HeroBrain.cs:207,247` | MATCH | FertilizeGuard=0.5 |
| 38 | Keys 6 and 7 | `04_Content.cs:367` | MATCH | Keybind 6 and 7 |
| 39 | Unlocked by a Direction; button hidden until held | `06_HeroBrain.cs:156-173` | MATCH | CheckFertilize adds the command only when Rank>0 |
| 40 | Cost: 1 Fertilizer | `06_HeroBrain.cs:242-250` | MATCH | Spends one |
| 43 | Fertilizer: speeds Growth, unlocked only by a Direction | `06_HeroBrain.cs:156-179` | MATCH | ShowFertilizer only when Unlocked or Rank>0 |
| 44 | Start: 0 each map | `05_Server.cs:145-153` | MATCH | OnMapStart clears Fertilizer |
| 45 | Cap 10 | `02_Data.cs:96; 05_Server.cs:93` | MATCH | FertilizerMax=10 and clamp |
| 46 | Earned by the Directions; first is Compost Bin | `02_Data.cs:102-110; 05_Server.cs:121-140` | MATCH | Compost counts foes felled in radius |
| 47 | Spent on Fertilize | `06_HeroBrain.cs:242-250` | MATCH | Fertilize spends one |
| 48 | Between maps: resets | `05_Server.cs:145-153` | MATCH | OnMapStart |
| 49 | On the vessel's death: kept | `05_Server.cs:78-87; 05_Server.cs:257-262` | MATCH | Player record kept; hero death does not clear it |
| 52 | Growth rises each tide a tower survives; resets if destroyed | `07_Building.cs:95-105; 05_Server.cs:236-247` | MATCH | TideBegan grows; a destroyed building is new at 0 |
| 53-54 | Per tide 1 stack; 5 on a map with 4 tides or fewer | `02_Data.cs:93-94; 05_Server.cs:169-174` | MATCH | GrowthPerTide=1, GrowthPerLongTide=5, LongTides=4 |
| 59 | Lobs long-range spore bombs in an arc, AOE, stacks spores | `08_Cannon.cs:35-64` | MATCH | Fire and Land |
| 60 | DOT increases as spores stack | `08_Cannon.cs:217` | MATCH | Stacks times SporeDps times SporeTick |
| 62 | In-game line: Lobs spore bombs that stack spores, damaging foes over time | `02_Data.cs:289` | MATCH | Desc text |
| 63-68 | Stats: Health 20+25; Damage 1+0.25 on impact; Attack speed 0.4; Range 9; AOE radius 1; Spores 2 per hit | `02_Data.cs:291; 08_Cannon.cs:75` | MATCH | Stats line; Blast deals Damage |
| 69 | Spore damage 0.3 per second per stack | `02_Data.cs:200; 08_Cannon.cs:217` | MATCH | SporeDps=0.3 |
| 70 | Spore tick every 0.5 seconds | `02_Data.cs:203; 08_Cannon.cs:202` | MATCH | SporeTick=0.5 |
| 71 | Each stack falls off 6 seconds after the last new one | `02_Data.cs:201; 08_Cannon.cs:154,210-211` | MATCH | SporeFade=6 |
| 72 | Max stacks 10; fully spored foes show a fungal effect | `02_Data.cs:202,48; 08_Cannon.cs:163` | MATCH | SporeMax=10; VfxSporefull |
| 73 | Limit: none | `02_Data.cs:288-302` | MATCH | No limit set for the cannon |
| 74 | Cost: Red 15 | `02_Data.cs:289` | MATCH | Redaura=15 |
| 76 | Growth max 5 | `02_Data.cs:289` | MATCH | Max=5 |
| 77 | Each stage: attack speed and AOE increase | `02_Data.cs:204-206; 08_Cannon.cs:82-86` | MATCH | CannonGrowSpeed and CannonGrowRadius via OnGrowth |
| 78-79 | +10% attack speed and +0.1 radius per stage; at max a bounce 1 s later at half radius and damage with 1 stack | `02_Data.cs:205-210; 08_Cannon.cs:53-64` | MATCH | BounceDelay=1, BounceShare=0.5, BounceSpores=1 |
| 81-84 | Sticky Spores: full spores slow 25%; Red 50 | `02_Data.cs:294; 08_Cannon.cs:164-168` | MATCH | Sticky_Slow=0.25 via SlowOn at full stacks |
| 85-88 | Farout Fungus: range +2; AOE +50% per second in air up to +75%; Red 45 Blue 10 | `02_Data.cs:204,209,295; 08_Cannon.cs:45-47` | MATCH | Range_Add=2, Farout_Per=0.5, FaroutMax=0.75 |
| 90-93 | Viral Outbreak: 30% chance, 15 damage, 2 tiles, 1 stack; Red 90 | `02_Data.cs:296; 08_Cannon.cs:291-295` | MATCH | Viral_Chance, Viral_Damage, Viral_Radius, Viral_Spores |
| 94-97 | Deathcap Training: DOT +25%, max stacks 8 but keep higher stacks; Red 100 | `02_Data.cs:297; 08_Cannon.cs:89,93,150-153` | MATCH | Deathcap_Multi=0.25, Sporemax_Base=8, stacks kept above cap |
| 99-102 | Explosive Finish: 1 damage per stack to foes within 1.25, others get 2 stacks, 1 s guard; Red 420 | `02_Data.cs:298,210; 08_Cannon.cs:246-270 (YosseleSpores.Burst)` | MATCH | Finish_Per=1, Finish_Radius=1.25, Finish_Spores=2 |
| 103-106 | Hallucinogen: every 5 s, 20% chance to stun 2 s; Red 360 Blue 280 | `02_Data.cs:299; 08_Cannon.cs:218-223,274-278` | MATCH | Stun uses Yossele's own daze look (VfxDaze), not the stock stun |
| 108-111 | Cordyceps: 20% chance plus 5% per stack to rise as a Cordyfriend; Red 900 Blue 1250 | `02_Data.cs:300; 08_Cannon.cs:92,296; 16_Friend.cs:30-38` | MATCH | Cordy_Chance=0.2, Cordy_Per=0.05 |
| 116 | Dandelion seeds float round the tree, stack, strike all at once | `13_Client.cs:393-426; 09_Dandelion.cs:133-166` | MATCH | Orbit draws seeds; Strike sends them together |
| 118 | In-game line: Releases seeds that strike targets who wander too close | `02_Data.cs:306` | MATCH | Desc text |
| 119 | Health 50, +25 per level | `02_Data.cs:308` | MATCH | Health=50+25 |
| 120 | Damage 2 per seed, +0.25 per level | `02_Data.cs:308; 09_Dandelion.cs:125-131` | MATCH | Damage=2+0.25 |
| 121 | Range 7 tiles | `02_Data.cs:308` | MATCH | Range=7 |
| 122 | Seed limit 10 | `02_Data.cs:308` | MATCH | Seedlimit=10 |
| 123 | Seed spawn 1 every 2 s; full at tide start | `02_Data.cs:308; 09_Dandelion.cs:107-114` | MATCH | Seedrate=0.5; OnTide sets Ready to Limit |
| 124 | Seed flight 0.3 s; shared out, nearest first | `02_Data.cs:225; 09_Dandelion.cs:133-159` | MATCH | SeedFlight=0.3; foes are nearest first |
| 125 | Limit: 1 | `05_Server.cs:309-350` | MATCH | TreeOf, TreeSlot and Limit |
| 126 | Cost: Red 50 | `02_Data.cs:306` | MATCH | Redaura=50 |
| 128-129 | Growth max 15; each stage +3 seed limit and +20% range | `02_Data.cs:306,223-224; 09_Dandelion.cs:351-358` | MATCH | SeedsPerStage=3, RangePerStage=0.2 |
| 130 | At max: nothing extra | `09_Dandelion.cs:351-358` | MATCH | No max-only rule |
| 132-136 | Seed Packets: felled foe refunds 2 seeds; +3 damage; leads to Picky and Weeds; Red 40 | `02_Data.cs:311; 09_Dandelion.cs:318-334` | MATCH | Packets_Refund=2, Damage_Add=3, Next Picky3,Weeds3 |
| 137-141 | Demand and Supply: twice as fast below a quarter of the limit; Red 20 Yellow 40 | `02_Data.cs:312; 09_Dandelion.cs:49-61` | MATCH | Demand_Max=1, Demand_Below=0.25 |
| 143-146 | Picky Placement: toughest first; 0.25% of max health; Red 100 | `02_Data.cs:313; 09_Dandelion.cs:125-131,142-143` | MATCH | Picky_Percent=0.0025 |
| 147-148 | Garden Weeds: a debuff that may speed seed regrowth when the debuffed foe dies | `02_Data.cs:314; 09_Dandelion.cs:317-347` | MATCH | Weeds stacks added on a fall within the window |
| 149 | Garden Weeds numbers: stacks up to Growth, +10% each; debuff lasts 3 seconds; chance 40% | `02_Data.cs:314; 09_Dandelion.cs:35,58,335-339` | DIFFERENT | Weeds_Last=15 makes a stack last 15 s; 3 s is only Weeds_Window, the time after a seed hit that counts. Stacks up to Growth, 10% each and 40% chance do match |
| 150 | Garden Weeds cost: Red 90 Yellow 30 | `02_Data.cs:314` | MATCH | Redaura=90;Yellowaura=30 |
| 151-154 | D-MDS: no damage, intercepts projectiles in range; spawn 1 every 4 s; 1 seed per projectile, 5 for a boss; Blue 150 Yellow 40 | `02_Data.cs:315,229; 09_Dandelion.cs:234-313` | MATCH | Seedrate_Multi=-0.5 gives 1 per 4 s; Dmds_Boss=5; BossHealth 600; Threat counts hurting shots |
| 156-159 | Bonsai: 40% of the Tree with its upgrades; sells for 0; limit 1; planted at the Tree's cost; Red 300 Blue 300 Yellow 300 | `02_Data.cs:316,363-368; 09_Dandelion.cs:435-486; 05_Server.cs:335-343` | MATCH | Bonsai_Share=0.4; Sellvalue 0; TreeSlot limit 1 |
| 160-163 | Rubbery Shell: seeds bounce 2 times within 2 tiles; D-MDS seed jumps; Red 450 Blue 600 | `02_Data.cs:317; 09_Dandelion.cs:200-226,287-300` | MATCH | Rubbery_Bounces=2, Rubbery_Range=2 |
| 165-168 | Dandelion Storm: range +100%, seed limit +20, seed rate +100%; Red 1050 Blue 800 Yellow 2400 | `02_Data.cs:318; 07_Building.cs:173-178` | MATCH | Range_Multi=1, Seedlimit_Add=20, Seedrate_Multi=1 as own-stat modifiers |
| 172 | Directional wall; vines extend forwards if possible | `10_Henge.cs:136,150-172` | MATCH | Line and Step, level or one up or down |
| 173 | Vines expand onto existing buildings' tiles | `10_Henge.cs:131-147,155` | MATCH | Surface finds any tile, building tiles included |
| 174 | Vines slow by default, with more debuffs per upgrade path | `10_Henge.cs:217-271,386` | MATCH | Slow in YosseleVines.Update; upgrade effects in Work and Remedy |
| 176 | In-game line: Sturdy stones, vines creep forward and slow foes | `02_Data.cs:324` | MATCH | Desc text |
| 177 | Health 80, +40 per level | `02_Data.cs:326` | MATCH | Health=80+40 |
| 178 | Vine slow 15% | `02_Data.cs:326; 10_Henge.cs:386` | MATCH | Slow=0.15 |
| 179 | Vine length 1 tile at the start | `10_Henge.cs:29` | MATCH | Length = 1 + Growth/2 |
| 180 | Facing turned while placing, with Sno Sno's arrows | `04_Content.cs:125-145,428-432; 10_Henge.cs:58-73` | MATCH | Orientable and ShowFacing copies Ice Wall's Forward |
| 181 | Vines are flat tiles on the ground | `04_Content.cs:509-558; 03_Assets.cs:105-107` | MATCH | BuildGround; the ground material is kept |
| 182 | On a vine: within 0.55 tiles of a tile's centre | `02_Data.cs:241; 10_Henge.cs:201-213` | MATCH | VineReach=0.55, Covers |
| 183 | Limit: none | `04_Content.cs:324-502` | MATCH | No limit set |
| 184 | Cost: Blue 25 | `02_Data.cs:324` | MATCH | Blueaura=25 |
| 186 | Growth max 8 | `02_Data.cs:324` | MATCH | Max=8 |
| 187 | Each stage: damage reduction +10%; vine length +1 every 2 Growth | `02_Data.cs:238; 10_Henge.cs:29,116-121` | MATCH | HengeArmorPerStage=0.1; Length uses Growth/2 |
| 188 | At max: nothing extra | `10_Henge.cs:116-121` | MATCH | No max-only rule |
| 190-191 | Verdant Spread: vines extend all around; in-game line | `02_Data.cs:329; 10_Henge.cs:131-147` | MATCH | Desc and Around |
| 192 | Verdant: every tile within 2, 3 at full Growth | `02_Data.cs:329; 10_Henge.cs:31-39` | MATCH | Verdant_Reach=2, Verdant_Full=3 |
| 194-197 | Vine Blockade: +50% max health; barrier 30% of max health each tide; Blue 35 | `02_Data.cs:330; 10_Henge.cs:105-114; 12_Hooks.cs:404-420` | MATCH | Health_Multi=0.5, Blockade_Share=0.3; barrier soaks, no heal past it |
| 199-202 | Natural Remedy: 0.5 health per second per Growth stage; stage 1 before growing; Blue 90 | `02_Data.cs:331; 10_Henge.cs:274-276` | MATCH | Remedy_Heal=0.5 with Max(1,Growth) |
| 203-206 | Opportunistic Carnivore: 5 per second per foe, limit 5 foes; Blue 80 Red 30 | `02_Data.cs:332; 10_Henge.cs:229-236` | MATCH | Carnivore_Drain=5, Carnivore_Max=5 |
| 207-210 | Easy Target: 15% more damage after 5 s on vines; Blue 40 Red 75 | `02_Data.cs:333; 10_Henge.cs:237-241; 12_Hooks.cs:397-399` | MATCH | Easy_After=5, Easy_Bonus=0.15 |
| 212-215 | Entangling Roots: 7 s on vines, stun 1.5 s, 5% max health, once per foe; Blue 420 Red 300 | `02_Data.cs:334; 10_Henge.cs:242-252` | MATCH | Roots_After=7, Roots_Stun=1.5, Roots_Percent=0.05; snared list |
| 216-219 | Obsidian: +200% max health; Blue 600 | `02_Data.cs:335` | MATCH | Health_Multi=2 |
| 221-224 | Monument: 20% damage reduction, no foe debuffs; 90% cap; Blue 2000 Yellow 1500 | `02_Data.cs:336,239; 10_Henge.cs:116-121; 12_Hooks.cs:86,214-240` | MATCH | Monument_Armor=0.2, ArmorCap=0.9, Cleanse |
| 228 | Collects every aura node in range through its roots | `11_Pachira.cs:96-105` | MATCH | Ores within Range |
| 230 | In-game line: Its roots mine every ore node in reach, fully grown it drops aura fruit | `02_Data.cs:342` | MATCH | Desc text |
| 231-234 | Health 30, +20; Damage 1 per strike; Attack speed 0.25; Range 2.5 | `02_Data.cs:344; 11_Pachira.cs:107-123` | MATCH | Health=30+20, Damage=1, Attackspeed=0.25, Range=2.5 |
| 235 | Aura fruit: Attack speed x Damage x 15 of all auras; any vessel or minion picks it up | `11_Pachira.cs:172-186,428-436` | MATCH | FruitEach with FruitScale=15; Picker takes Hero or Mob |
| 236 | Fruit pickup within 0.75 tiles; stays until picked up or map ends | `02_Data.cs:249; 11_Pachira.cs:405-424` | MATCH | FruitReach=0.75; no expiry |
| 237-238 | Limit: none; Cost: Yellow 35 | `02_Data.cs:342` | MATCH | Yellowaura=35; no limit set |
| 240 | Growth max 5 | `02_Data.cs:342` | MATCH | Max=5 |
| 241-242 | Each stage: mining speed +10%, range +0.4 | `02_Data.cs:247; 11_Pachira.cs:62,140` | MATCH | PachiraGrowSpeed=0.1, PachiraGrowRange=0.4 |
| 243 | At max: drops aura fruit after fully mining a node | `11_Pachira.cs:191-199` | MATCH | NodeEmptied drops when FullGrown |
| 245-248 | Focused Roots: +80% mining speed, only the nearest node; Yellow 30 | `02_Data.cs:347; 11_Pachira.cs:52-53,62-64` | MATCH | Focused_Multi=0.8; GetRange(0,1) |
| 249-252 | Sustaining Practices: 0.5 health per second per node per stage, stage 1 before growing; Yellow 25 Blue 10 | `02_Data.cs:348; 11_Pachira.cs:34-47` | MATCH | Sustain_Heal=0.5 with Max(1,Growth) |
| 254-257 | Grafted: fruit colour chosen, 70% efficiency; Red 60 Blue 60 Yellow 60 | `02_Data.cs:349; 11_Pachira.cs:177-186` | MATCH | Grafted_Efficiency=0.7; one colour at 70% of the total |
| 258-261 | Forest Project: +10% mining speed per other Sapling within 4 tiles, anyone's; Yellow 140 | `02_Data.cs:350; 11_Pachira.cs:76-84` | MATCH | Forest_Per=0.1, Forest_Range=4; any owner counts |
| 262-265 | Spontaneous Mulch: +20% for 5 s on a foe death in range, no stacking; Red 40 Yellow 30 | `02_Data.cs:351; 11_Pachira.cs:87-93` | MATCH | Mulch_Bonus=0.2, Mulch_Last=5; refreshes |
| 267-270 | Connected Roots: while above 50% health, takes 65% of hits on nearby buildings; other Saplings excluded; Yellow 120 Blue 300 | `02_Data.cs:352; 12_Hooks.cs:270-283,382-390` | MATCH | Connected_Share=0.65, Connected_Above=0.5; Sapling excluded |
| 271-274 | Harvesting Season: fruit worth double; Yellow 400 | `02_Data.cs:353; 11_Pachira.cs:174` | MATCH | Harvest_Multi=1 doubles FruitEach |
| 275-278 | Wood Wide Web: 2 most-held become 1 least-held while gap at least 100, once a second per player; Yellow 250 | `02_Data.cs:354; 11_Pachira.cs:303-361` | MATCH | Web_Gap=100, Web_Take=2, Web_Give=1; one trade per player each second |
| 280-283 | Fortifying Roots: +100% health; nearby buildings 30% less damage, itself included; Blue 2300 Yellow 1000 | `02_Data.cs:355; 12_Hooks.cs:292-295,86` | MATCH | Health_Multi=1, Fortify_Armor=0.3; strongest aura counts |
| 284-287 | Softening Roots: nearby nodes lose 1 health point, never below 1; Yellow 2300 Red 1000 | `02_Data.cs:356; 11_Pachira.cs:242-296` | MATCH | Soften_Points=1; cut limited so value stays at least 1 |
| 291 | Cordyfriend made by Cordyceps, the Mushroom Cannon's level 4 upgrade | `02_Data.cs:300; 16_Friend.cs:30-38` | MATCH | Cordyceps is tier 5, which the file header says is the sheet's level 4 |
| 292 | Attack: melee | `04_Content.cs:584` | MATCH | Mob_Combat melee AI |
| 293-297 | Health 75; Damage 2; Range 1.5; Attack speed 0.5; Speed 2.25 | `02_Data.cs:215` | MATCH | FriendStats |
| 298 | Up to 30 at once per player | `02_Data.cs:216; 16_Friend.cs:20-38` | MATCH | FriendCap=30 checked in Raise and Spawn |
| 299 | Respawn: none | `16_Friend.cs:93-101` | MATCH | Nothing respawns; Gone only counts |
| 300 | Orders: walk to nearest foe within 4 tiles, else back to where it rose | `02_Data.cs:217; 16_Friend.cs:151-175` | MATCH | FriendEngage=4; Think returns Home |
| 301 | Leash: 5 tiles from where it rose | `02_Data.cs:218; 16_Friend.cs:157-174` | MATCH | FriendLeash=5 |
| 302 | Cost: free | `04_Content.cs:587-588` | MATCH | Empty Costs list |
| 303 | Look resembles a Snoble; temporary sprite 24 px, idle 6, move 6, attack 4 frames | `04_Content.cs:575-619; 03_Assets.cs:28-41; 13_Client.cs:61-78` | MATCH | Friend sheet 24 px; Snoble look and tint fallback when the sheet is missing |
| 306 | Directions: Compost Bin, Dandelion Season, Mother of Mushrooms | `04_Content.cs:628-664; 02_Data.cs:102-148` | MATCH | BuildDirections registers each |
| 307 | Directions: Cutting Jar, Sap Bucket, Creeping Ivy, Deep Breath, Fairy Ring (made by Claude) | `04_Content.cs:661; 02_Data.cs:156-195; 17_Directions.cs:1-14` | MATCH | VesselDirections list; each rank's numbers in 02_Data.cs |

### 1.3 08_Vessel_Spool.txt

| Lines | Says | Code | Mark | Note |
| --- | --- | --- | --- | --- |
| 5-6 | Spool built with these defaults; tilde values stay open until taken | `02_Data.cs:1-374` | MATCH | Tilde defaults are the numbers in SpoolData, as the scope says they are. |
| 2 | Spool, Crafty Crafted Crafter | `02_Data.cs:54-55` | MATCH | Displayname 'Spool', Displaysub 'Crafty Crafted Crafter'. |
| 8-9 | Title Crafty Crafted Crafter; Difficulty Advanced | `02_Data.cs:55-56` | MATCH | Displaysub and Displayskill 'Advanced'. |
| 10 | Archetype: Commands a disposable minion army | `-` | MISSING | Looked for an archetype text in the hero entry and journal; none. Nothing queued. |
| 11 | In-game line: Crafty melee, commands a stitched army | `02_Data.cs:57` | MATCH | HeroDesc has the same text. |
| 12 | Look: hooded sage plush, stitched face, severed arm (look description) | `03_Assets.cs:6-27` | UNBUILT-BY-DESIGN | Look is art; the code loads the Spool-Sheet frames. Cannot be checked from text. Scope sprites await design. |
| 13 | Towers look: stitches, pins and tins (default) | `-` | UNBUILT-BY-DESIGN | A sheet note the scope marks as a default; no code text. Towers use the Tower_Spool* models. |
| 14 | Accent sage 93CCA1 and 60A871 | `04_Content.cs:384` | DIFFERENT | 93CCA1 matches Sage at 04_Content.cs:384. 60A871 is not a constant; the thread colour is 6FAE7E (11_Client.cs:14). |
| 15 | Sprites: full sheet comes once past drafting | `03_Assets.cs:6-27` | UNBUILT-BY-DESIGN | Temporary art in use; the full sprite sheet is held for design (scope 07 NEEDS line). |
| 16 | Lore: comes once the vessel is finalized | `02_Data.cs:61` | UNBUILT-BY-DESIGN | Placeholder Lore text 'Spool's story is still being stitched.' |
| 17 | Status: Built 0.1.0-dev; the Stash, Thread Head and Loom upgrades, Tacking Stitch, Cross Stitch, Patchwork Army, Iron Thimble, Voodoo Doll and UNTHREAD not built | `02_Data.cs:261-341; 04_Content.cs:658-691` | UNBUILT-QUEUED | The listed unbuilt items are queued as Spool B18/B19 (PLAN Next: Spool's 22 upgrades and UNTHREAD). |
| 20 | Attack: melee | `04_Content.cs:745-747` | MATCH | Hero gets Entity_Ai_Mob_Combat, the melee AI. |
| 21 | Health: 45 | `02_Data.cs:76` | MATCH | HeroStats 'Health=45'. |
| 22 | Health regen: 0 per second | `-` | MISSING | No regen stat set in HeroStats. Nothing queued; the engine default applies. |
| 23 | Speed: 2.75 | `02_Data.cs:76` | MATCH | 'Speed=2.75'. |
| 24 | Damage: 2.5 | `02_Data.cs:76` | MATCH | 'Damage=2.5'. |
| 25 | Range: 1.5 tiles | `02_Data.cs:76` | MATCH | 'Range=1.5'. |
| 26 | Attack speed: 0.6 | `02_Data.cs:76` | MATCH | 'Attackspeed=0.6'. |
| 27 | Build range: 2.5 tiles | `02_Data.cs:76` | MATCH | 'Buildrange=2.5'. |
| 28 | Sell rate: 50% | `02_Data.cs:76` | MATCH | 'Sellrate=0.5'. |
| 29 | Special rule: Hand Stitched, foes drop Thread, Crafted cost Thread, PATCH mends | `02_Data.cs:58-59; 04_Content.cs:709-731` | MATCH | TooltipName 'Hand Stitched' and TooltipDesc match; the tier -3 tooltip is built. |
| 31-33 | PATCH: uses 20 Thread to heal 20% of max health, up to 500 | `02_Data.cs:95-96; 06_HeroBrain.cs:319-325` | MATCH | PatchCost 20, PatchHeal 0.2, PatchMax 500; Mathf.Min(max*PatchHeal, PatchMax). |
| 34 | Target: one of your units or buildings | `06_HeroBrain.cs:249-255` | MATCH | Patchable() allows only the player's own units and buildings. |
| 35 | Range: 4 tiles | `02_Data.cs:96; 06_HeroBrain.cs:269,300` | MATCH | PatchRange 4, checked with +0.6 slack. |
| 36 | Cooldown: 5 seconds | `02_Data.cs:96; 06_HeroBrain.cs:327` | MATCH | PatchCooldown 5; PatchReady set to now + 5. |
| 37 | Keys: 6 / 7 | `04_Content.cs:393` | MATCH | Ability keybinds {'6','7'}. |
| 38 | Unlocked: from the start | `06_HeroBrain.cs:63-67` | MATCH | Patch command created in Begin(). |
| 39 | Cost: Thread 20 taken from the Yarn Stash | `06_HeroBrain.cs:319; 05_Server.cs:132-140` | MATCH | SpendStash(PatchCost) from the Stash. |
| 42 | Thread: units crafted from Thread dropped by enemies or mined from nodes | `05_Server.cs:280-288; 07_Building.cs:237-251` | MATCH | FoeFell drops; Loom strikes ore. |
| 43 | Start: 10 in the Yarn Stash each map | `02_Data.cs:83; 07_Building.cs:159-162` | MATCH | StashStart 10, given once per placed Stash. |
| 44 | Cap: 50 in the Yarn Stash, +25 per upgrade | `02_Data.cs:84,268; 07_Building.cs:151` | MATCH | StashCap 50; 'Threadcap=50+25'. |
| 45 | Carried: Spool 20, a Thread Head 20 | `02_Data.cs:85; 02_Data.cs:226` | MATCH | CarryHero 20, CarryHead 20, 'Carry=20'. |
| 46 | Earned by: small enemies drop 1, large elite up to 5; pack modifiers count as elite; Loom and Pin Cushion also make Thread | `02_Data.cs:86; 05_Server.cs:280-304; 07_Building.cs:253-268; 10_Cushion.cs:115-126` | MATCH | DropSmall 1, DropLarge 3, DropElite 2, DropMax 5; Elite() reads RW_Modifier_ keys. |
| 47 | Thread lasts 15 seconds on the ground | `02_Data.cs:88; 05_Server.cs:97` | MATCH | DropLife 15; drops swept after Until. |
| 48 | Pickup: by walking within 1 tile | `02_Data.cs:89; 06_HeroBrain.cs:138` | MATCH | PickHero 1f used as Collect reach. |
| 49 | Collected by Spool or a Thread Head; no lag; collection flies like Boutman's fish | `05_Server.cs:225-248; 11_Client.cs:326-348` | MATCH | Drops are a list, not entities; Fly() tuft animation. |
| 50 | Delivered to the main base by Spool or a Thread Head | `05_Server.cs:155-168; 09_Head.cs:170-185` | MATCH | Deliver() into the Stash; the Head delivers on its own. |
| 51 | Drop-off: within 1.5 tiles of the Yarn Stash | `02_Data.cs:89; 06_HeroBrain.cs:152` | MATCH | DropOff 1.5, AtStash check. |
| 52 | Spent on Crafted and Patch | `08_Tin.cs:78; 06_HeroBrain.cs:319` | MATCH | SpendStash for Tin stitches and PATCH. |
| 53 | Sold: a Crafted refunds half its Thread, rounded down | `08_Tin.cs:578` | MATCH | Cost / 2 (integer division) when not Recycler. |
| 54 | Between maps: resets | `05_Server.cs:66-75` | MATCH | Players, drops and Heroes cleared when Game_Entity changes. |
| 55 | On the vessel's death: kept | `05_Server.cs:424-431` | MATCH | Hero despawn does not clear SpoolPlayer.Stash. |
| 58-59 | Yarn Stash holds Thread; placed first like Boutman's Bouthouse | `06_HeroBrain.cs:79-85; 07_Building.cs:149-166` | MATCH | Menu holds only the Stash until one stands. |
| 61 | In-game line: Holds Spool's Thread, the Crafted draw from it | `02_Data.cs:266` | MATCH | Desc text matches. |
| 62 | Health: 100, +50 per level | `02_Data.cs:268` | MATCH | 'Health=100+50'. |
| 63 | Thread cap 50, +25 per upgrade | `02_Data.cs:268; 07_Building.cs:151` | MATCH | 'Threadcap=50+25'; Cap uses the stat. |
| 64 | Limit: 1 | `07_Building.cs:156-157` | DIFFERENT | Nothing enforces one Stash. A second one is allowed and only warned; the newest holds the Thread. |
| 65 | Cost: free, placed at the start | `02_Data.cs:266` | MATCH | Costs empty string. |
| 66-97 | Yarn Stash upgrades: Roomy Pockets, Quick Patch, Darning, Elbow Patches, Remnant Bin, Mending Circle, Quilting Bee (and the +25 cap per upgrade) | `02_Data.cs:263-270 (Upgrades = new Upg[0])` | UNBUILT-QUEUED | Seven Yarn Stash upgrades not built (script leads: all MISSING); queued as Spool B18/B19. |
| 101-111 | Stitched: melee, 20 HP, damage 5, range 1.5, attack speed 0.8, speed 1.8, leash 2, cost 10, made by the Tin | `02_Data.cs:196-199; 04_Content.cs:519-556` | MATCH | Stats 'Health=20', 'Damage=5', 'Range=1.5', 'Attackspeed=0.8', 'Speed=1.8'; Leash 2; Thread 10. |
| 109 | Stitched runs to the targeted spot and idles until it sees an enemy | `08_Tin.cs:326-327; 08_Tin.cs:395-430` | MATCH | Walk to Spot, then Guard(). |
| 112-122 | Dart: made by Tin with New Design, ranged, 12 HP, damage 5.5, range 3.5, attack speed 0.6, speed 1.6, leash 1, cost 15 | `02_Data.cs:200-203; 08_Tin.cs:36; 08_Tin.cs:419-420` | MATCH | Ranged=true, 'Health=12', 'Damage=5.5', 'Range=3.5', 'Projspeed=14'; Thread 15. |
| 123-135 | Seam: made by Loom once upgraded; melee, 10 HP, damage 1, range 1.5, attack speed 0.8, speed 1.5; 1 per Loom; respawn 10 s; leash 2.5; free | `02_Data.cs:204-207,337; 07_Building.cs:282-294; 02_Data.cs:204` | MATCH | Stats match; 'Seamrespawn_Base=10'; KeepSeam keeps one. |
| 136-147 | Mangled: made by Pin Cushion when hit; melee, 15 HP (45 tanky), damage 1.5, range 1.5, attack speed 0.8, speed 2; 1 per 40 damage, up to 3; leash 2; free | `02_Data.cs:208-215,319; 10_Cushion.cs:128-149` | MATCH | 'Mangled_Per=40;Mangled_Max=3'; Overstuffed stats 'Health=45'; Leash 2. |
| 149-156 | Repurposed Cookie Tin: uses the Stash's Thread, sends Stitched down a path to the target, Darts via New Design, Crafted cost Thread, not smart, can be sold by Spool | `08_Tin.cs:13-155; 04_Content.cs:539; 02_Data.cs:273-276` | MATCH | Tin does not collect; Crafted cost SpendStash; Sellable true for Crafted. |
| 156 | UNTHREAD button sells every Crafted the Tin has out, refunding half their Thread | `-` | UNBUILT-QUEUED | No UNTHREAD command; only the normal Sell. Queued as Spool B19 (UNTHREAD). |
| 157-164 | Tin stats: Health 60 +25; Crafted alive 6; crafting rate 1 every 4 s; target within 8 tiles; cost Red 50 | `02_Data.cs:274-276; 08_Tin.cs:31,174-210` | MATCH | 'Health=60+25', 'Units=6', 'Respawn=4', 'Reach=8', Costs 'Redaura=50'. |
| 166-169 | Reuse Reduce Recycle: killed Crafted drop half cost rounded up; Red 30 | `02_Data.cs:279-280; 08_Tin.cs:593-594` | MATCH | 'Recycle_Base=0.5'; CeilToInt(Cost*Recycle); 'Redaura=30'. |
| 170-173 | Woolen Lining: health +50%; Red 30 | `02_Data.cs:281-282; 08_Tin.cs:309-310` | MATCH | 'Craftedhealth_Multi=0.5' adds a Health Multi 0.5; 'Redaura=30'. |
| 175-178 | Discounted Material: Stitched cost 8 Thread, 30% faster; Red 90 | `02_Data.cs:283-284; 08_Tin.cs:41-42,126` | MATCH | 'Stitchedcost_Base=8;Stitchedspeed_Multi=0.3'; 'Redaura=90'. |
| 179-182 | New Design: spawns Darts; Red 100 | `02_Data.cs:285-286; 08_Tin.cs:36` | MATCH | 'Dart_Base=1'; Kind returns Dart; 'Redaura=100'. |
| 183-186 | Tacking Stitch: Stitched block enemies like a Snoble, +100% health, no Darts; Red 110 | `-` | UNBUILT-QUEUED | Not in 02_Data.cs Tin upgrades. Queued as Spool B18/B19. |
| 188-191 | One Last Surprise: Crafted burst on death, radius = attack range, damage 3x; Red 400 | `02_Data.cs:287-288; 08_Tin.cs:596-601` | MATCH | 'Burst_Times=3'; radius Range; 'Redaura=400'. |
| 192-195 | Cross Stitch: Crafted +100% damage; Red 350 | `-` | UNBUILT-QUEUED | Not built. Queued as Spool B18/B19. |
| 197-200 | Patchwork Army: Crafted alive +3, rate 1 every 2 s; Red 2500 + Yellow 1000 | `-` | UNBUILT-QUEUED | Not built. Queued as Spool B18/B19. |
| 203-206 | Thread Head: a controller minion; SPOT moves it; collects Thread; RETURN sends it home; no Threaded Delivery means you send it back yourself | `04_Content.cs:560-582; 09_Head.cs:52-141; 09_Head.cs:171-199` | MATCH | SPOT and RETURN buttons; Collect within reach. |
| 207 | Specialised as a buffer or a thread porter | `02_Data.cs:232-253` | UNBUILT-QUEUED | Buffer and porter upgrades are not built (Big Bobbin, Needle Point and the rest). Queued as Spool B18/B19. |
| 208 | Made by: bought from the build menu | `02_Data.cs:31; 04_Content.cs:586-607` | MATCH | Token in Buildables; SwapTokens places the Head. |
| 209-214 | Thread Head: melee, health 60, damage 2, range 1.5, attack speed 0.6, speed 2.25 | `02_Data.cs:226` | MATCH | HeadStats 'Health=60', 'Damage=2', 'Range=1.5', 'Attackspeed=0.6', 'Speed=2.25'. |
| 215-216 | Carries 20 Thread; collects from 3 tiles around it | `02_Data.cs:226; 09_Head.cs:18-19` | MATCH | 'Carry=20', 'Collect=3'. |
| 217 | How many: no limit | `-` | MATCH | No cap on Heads placed. |
| 218 | Respawn 15 seconds at the Yarn Stash | `02_Data.cs:226; 09_Head.cs:281; 05_Server.cs:506` | MATCH | 'Respawn=15'; Comeback At +15; spawns at Home block. |
| 219-220 | Guards the spot chosen; leash 3 | `09_Head.cs:205-222; 02_Data.cs:226` | MATCH | 'Leash=3'; Guard() within leash. |
| 221 | Cost: Red 40 | `02_Data.cs:294` | MATCH | 'Redaura=40'. |
| 223-226 | Threaded Delivery: runs back to the Yarn Stash when full, then returns to its position; Red 30 | `02_Data.cs:299-300; 09_Head.cs:187-188,181-183` | MATCH | 'Delivery_Base=1'; 'Redaura=30'; StartReturn when Carried reaches Cap. |
| 227-230 | Patchwork Job: toggle MEND, 2 health a second within 2 tiles, 1 Thread per 5 health; Red 30 | `02_Data.cs:301-302; 09_Head.cs:66-81,226-266` | MATCH | 'Mend_Base=2;Mendrange_Base=2;Mendper_Base=5'; toggle MEND button; 'Redaura=30'. |
| 232-235 | Big Bobbin: carries 40, collects from 5 tiles; Yellow 60 | `-` | UNBUILT-QUEUED | Not built. Queued as Spool B18/B19. |
| 236-239 | Needle Point: +25% damage for Crafted within 3 tiles; Red 80 | `-` | UNBUILT-QUEUED | Not built. Queued as Spool B18/B19. |
| 241-244 | High Thread Count: +1 Thread from foes felled within 3 tiles; Yellow 250 | `-` | UNBUILT-QUEUED | Not built. Queued as Spool B18/B19. |
| 245-248 | Stitch in Time: 30% damage reduction for Crafted within 3 tiles; Red 300 + Yellow 100 | `-` | UNBUILT-QUEUED | Not built. Queued as Spool B18/B19. |
| 250-253 | Head of the Class: buffs reach 4 tiles, +50% damage, 45% reduction; Thread goes straight home; Red 2000 + Yellow 1500 | `-` | UNBUILT-QUEUED | Not built. Queued as Spool B18/B19. |
| 256-258 | Pin Cushion: reflects melee damage, spins Thread, spills Mangled by choice; Mangled spec into tanky or suicidal | `10_Cushion.cs:53-126,128-149; 02_Data.cs:313-323` | MATCH | Reflect on melee; Spin; Loose Stuffing, Overstuffed and Burst. |
| 260 | In-game line: Build it first: pricks melee attackers, spins Thread when hit; journal adds Set one where the paths meet | `02_Data.cs:308-309` | MATCH | Desc and Lore contain both texts. |
| 261-265 | Pin Cushion stats: health 100 +40; melee reflect 20%; 1 Thread per 25 damage; cost Blue 20 | `02_Data.cs:308,310` | MATCH | 'Health=100+40', 'Reflect=0.2', 'Spin=25', 'Blueaura=20'. |
| 267-270 | Sharper Pins: 40% of melee damage reflected; Blue 30 | `02_Data.cs:313-314; 07_Building.cs:96-101` | MATCH | 'Reflect_Add=0.2' adds to Reflect 0.2 to give 0.4; 'Blueaura=30'. |
| 271-274 | Slow Unravel: half damage now, half over 4 s, Thread spun x2; Blue 30 | `02_Data.cs:315-316; 10_Cushion.cs:80-89,151-166` | MATCH | 'Unravel_Base=4;Spin_Multi=-0.5' halves Spin (x2 Thread); 'Blueaura=30'. |
| 276-280 | Pin Barrage: 20% ranged reflect; leads to Iron Thimble; Blue 100 | `02_Data.cs:317-318` | DIFFERENT | Ranged 20% is built ('Rangedreflect_Base=0.2', 'Blueaura=100'), but Pin Barrage's Next list is empty, so it does not lead to Iron Thimble (which is unbuilt). |
| 281-284 | Loose Stuffing: spins Mangled when damaged; Blue 90 | `02_Data.cs:319-320; 10_Cushion.cs:128-149` | MATCH | 'Mangled_Per=40;Mangled_Max=3'; 'Blueaura=90'. |
| 286-288 | Overstuffed: Mangled health x3; jumps 2 tiles and blocks like a Snoble; Blue 400 | `02_Data.cs:321-322,215; 10_Cushion.cs:145-147; 04_Content.cs:528-536` | MATCH | Stuffed health 45; 'Leap_Base=2'; Snoble block; 'Blueaura=400'. |
| 289-292 | Burst Seams: 5 damage to enemies within 1.25 tiles when a Mangled reaches one; Blue 400 | `02_Data.cs:323-324; 08_Tin.cs:488-497` | MATCH | 'Seams_Damage=5;Seams_Radius=1.25'; BurstOn on reaching a foe; 'Blueaura=400'. |
| 294-297 | Iron Thimble: +200% health, 25% damage reduction; Blue 300 + Yellow 300 | `-` | UNBUILT-QUEUED | Not built. Queued as Spool B18/B19. |
| 299-302 | Voodoo Doll: once a tide, below 50% health stuns foes within 3 tiles for 3 s and heals 50%; Blue 2000 + Yellow 1500 | `-` | UNBUILT-QUEUED | Not built. Queued as Spool B18/B19. |
| 305 | Loom connects to nodes to mine aura | `07_Building.cs:237-251` | MATCH | Strike() on the nearest Resource. |
| 306 | Loom spins Thread for Thread Heads to port, or for support | `07_Building.cs:253-268; 05_Server.cs:225-268` | MATCH | Spin() held up to Hold; CollectLooms takes it. |
| 309-314 | Loom stats: health 40 +20; range 2.5; mines 1 for 0.3 strikes a second; spins 1 Thread every 6 s, holds 10; cost Yellow 40 | `02_Data.cs:332,330` | MATCH | 'Health=40+20', 'Damage=1', 'Attackspeed=0.3', 'Range=2.5', 'Spinrate=6', 'Hold=10'; 'Yellowaura=40'. |
| 316-319 | Direct Line: its Thread goes straight to the Yarn Stash; Yellow 30 | `02_Data.cs:335-336; 07_Building.cs:253-260` | MATCH | 'Direct_Base=1'; AddStash on spin when Direct; 'Yellowaura=30'. |
| 320-323 | Seam Allowance: creates Seams that mine and guard; Yellow 30 | `02_Data.cs:337-338; 07_Building.cs:282-294` | MATCH | 'Seam_Base=1;Seamrespawn_Base=10'; one Seam per Loom; 'Yellowaura=30'. |
| 325-328 | Dyed in the Wool: aura comes in the colour picked; Yellow 150 | `-` | UNBUILT-QUEUED | Not built. Queued as Spool B18/B19. |
| 329-332 | Bobbin Winder: 1 Thread every 3 s, holds 20; Yellow 100 | `-` | UNBUILT-QUEUED | Not built. Queued as Spool B18/B19. |
| 334-337 | Seam Squad: 3 Seams per Loom; Yellow 350 | `-` | UNBUILT-QUEUED | Not built. Queued as Spool B18/B19. |
| 338-341 | Snare Line: Seams chase aura beasts within 6 tiles; Yellow 300 | `-` | UNBUILT-QUEUED | Not built. Queued as Spool B18/B19. |
| 343-346 | Golden Thread: drop-off point, spins twice as fast; Yellow 3000 | `-` | UNBUILT-QUEUED | Not built. Queued as Spool B18/B19. |
| 349 | Directions: Efficient Recycler, Sewing Machine; and Made of Tougher Stuff for any vessel with minions | `04_Content.cs:658-686` | DIFFERENT | Recycler and Sewing are built. 'Made of Tougher Stuff' is not in the code (searched BuildDirections and SpoolData) and no plan item queues it. |
| 350 | Directions: Double Needle, Button Jar, Pattern Book, Merit Badge, Odd Socks | `04_Content.cs:688-690; 02_Data.cs:131-173` | MATCH | All five ranks are built as Directions. |

### 1.4 11_Directions.txt

| Lines | Says | Code | Mark | Note |
| --- | --- | --- | --- | --- |
| 11:9-19 | Made of Tougher Stuff: Kind Boost, Vessel Any with Minions, Touches Minions, rank texts, numbers 15/25/35% less damage and rank III max 25% of max health per hit, Look, Clashes Any Vessel without Minion, By Dimitri | `16_Directions.cs:31-32 (Mine entry, Reach Mob, Needs Minions); 112-113 (TougherResist 0/0.15/0.25/0.35, TougherMaxShare 0.25); 163-175 (Meets, HasMinions list of nine); 555-556 (Reach Tougher); 22_AnyDirections.cs n/a; 12_Hooks n/a; RWHits 16_Directions.cs:707-762 (OnHurt)` | MATCH | Numbers and rank texts match. Clash is expressed as Needs Minions (nine vessels), which matches the 'without Minion' sense. Look is art fetched by name (RWDirections.Picture, Direction_Tougher); the picture is not in the code. |
| 11:20-30 | Barnacle Coat: Vessel Any, Touches your towers, three ranks, prick 2/4/7, Look, no clash, By Claude | `16_Directions.cs:33-35 (Reach Tower, Script Entity_Ai_Effect_Smackywall_Bramble, Numbers 2/4/7); 15_Content.cs upgrade build via RWContent.Upgrade` | MATCH | Numbers and desc text match. Look is art; code fetches the library sprite by name only. |
| 11:31-41 | Tidal Mend: Touches your towers, ranks text, mends 5/8/12 within 2/2.5/3 tiles, Look, no clash | `16_Directions.cs:40-41 (Mend entry); 115-117 (MendHeal 0/5/8/12, MendRange 0/2/2.5/3); 589-612 (TideEffects MendAround); 634-660 area: MendAround (16_Directions.cs ~644-671)` | MATCH | Flat 5/8/12 heal and ranges per 14_Run note (Dimitri 2026-11-06). Look is art. |
| 11:42-52 | Spring Tide: towers gain +4/+7/+10 health a tide, rank III 10% less damage, clashes with Wound Spring | `16_Directions.cs:42-43 (Spring, Clash Spring); 114-115 (SpringShrug 0.1, SpringHealth 0/4/7/10); 553-554 (rank III RWHits.Resist SpringShrug); 589-600 TideEffects Grow; 22_AnyDirections.cs:91 Wound entry Clash Spring` | MATCH | Spring Tide's test is still open on the PC (IMPLEMENTATION_NOTES first entry: Multi near 0.01 in INFO lines). |
| 11:53-63 | Cut Costs: your buildings cost -10/-20/-30%, look, no clash | `16_Directions.cs:44-45 (Cost); 108 (CostScale 1/0.9/0.8/0.7); 417-428 Builds (BuildOption command Goalvalue cut); 417 Builds via Grant` | MATCH | Look is art. |
| 11:64-74 | Cheap Refits: your upgrades cost -10/-20/-30%, look, no clash | `16_Directions.cs:46-47 (Refit); 109 (RefitScale); 563-568 Refit (Upgrade command on Tower and Bouthouse)` | MATCH | Look is art. |
| 11:75-85 | Compound Interest: each tide 3/5/8% of each colour of unspent base aura, at most 40 of each | `16_Directions.cs:48-49 (Interest); 110-111 (InterestShare 0/0.03/0.05/0.08, InterestCap 40); 688-702 Interest (Mathf.Min(InterestCap, Floor(amount*share)))` | MATCH | Look is art. |
| 11:86-96 | Wound Spring: Touches all its buildings; towers attack +6/+12/+20%; clashes with Spring Tide; By Claude with StarCraft II reference | `16_Directions.cs:53-54 (Wound, Clash Spring); 22_AnyDirections.cs:187-217 Hooks, 216-248 Sweep; 23_BaseDirections.cs n/a; 22_AnyDirections.cs:52 WoundSpeed 0/0.06/0.12/0.2; 22_AnyDirections.cs:242 Wound Attackspeed Multi on SweptTags` | MATCH | Sweeps Tower and Bouthouse tags, which is the building set in practice; numbers match. Look is art. |
| 11:97-107 | Fine Sieve: each Tide pays the least-held colour 3/5/8 times the Tide number, paid to holder, look | `16_Directions.cs:55-56 (Sieve); 22_AnyDirections.cs:40-41 (SieveTimes 0/3/5/8); 22_AnyDirections.cs:608-624 Tide (amount SieveTimes[rank]*wave, least colour)` | MATCH | Paid on the break after each Tide, which is the scope's 'each tide'. Look is art. |
| 11:108-118 | Spare Hands: unit buildings keep +1 unit (rank I), units respawn sooner (rank II, -25%), +2 units and -40% respawn (rank III); clashes with Sno Sno; Needs a building that makes units | `16_Directions.cs:57-58 (Needs Units); 167 (Meets Units excludes Hero_Snosno and Hero_Yossele); 22_AnyDirections.cs:43-44 (HandsUnits 0/1/1/2, HandsRespawn 0/0/0.25/0.4), 288-306 Hands` | DIFFERENT | Code also excludes Yossele (Meets line 167) and the scope says only Sno Sno. Yossele is not in the scope's clash. Numbers match. |
| 11:119-128 | Galley Kettle: +4% attack speed a hit up to +20/+35/+50%, gone 3 s after last hit; rank III +10% damage at the top; text | `16_Directions.cs:59-60 (Kettle); 22_AnyDirections.cs:45-46 (KettleStep 0.04, KettleCap 0/0.2/0.35/0.5, KettleLasts 3, KettleTopDamage 0.1), 187-212 Hooks, 310-355 OnDealt` | MATCH | Look is art. |
| 11:129-139 | Chain Shot: every 4th hit jumps 1/2/3 times within 2 tiles for half damage; rank III every 3rd; text | `16_Directions.cs:61-62 (Chain); 22_AnyDirections.cs:46-48 (ChainEvery 0/4/4/3, ChainJumps 0/1/2/3, ChainReach 2, ChainShare 0.5), 448-475 Jumps, 345-349 OnDealt` | MATCH | Look is art. |
| 11:141-151 | Salt Barrel: health +15/+30/+45%; rank II and III mend 0.5 and 1 a second while hurt | `16_Directions.cs:63-64 (Salt); 22_AnyDirections.cs:49-50 (SaltHealth 0/0.15/0.3/0.45, SaltMend 0/0/0.5/1); 22_AnyDirections.cs:205-211 Hooks mend once a second` | MATCH | Look is art. |
| 11:152-162 | Fog Horn: foes within 2/2/3 tiles slowed 10/15/25% | `16_Directions.cs:65-66 (Fog); 22_AnyDirections.cs:51-52 (FogSlow 0/0.1/0.15/0.25, FogReach 0/2/2/3), 571-602 Fog` | MATCH | Look is art. |
| 11:163-173 | Clam Shell: rank I once a map survives a killing blow at 1 HP; rank II untouchable 3 s after a save; rank III twice a map | `16_Directions.cs:67-68 (Clam); 22_AnyDirections.cs:53-54 (ClamSaves 0/1/1/2, ClamSafe 3), 409-443 OnHurt (SafeUntil when Clam>=2)` | MATCH | Look is art. |
| 11:174-184 | Notched Oar: every 10th/7th/5th hit double damage | `16_Directions.cs:69-70 (Oar); 22_AnyDirections.cs:55 (OarEvery 0/10/7/5), 336-341 OnDealt doubles multi` | MATCH | Look is art. |
| 11:185-195 | Lamp Oil: drowned foe bursts 15/25/35% of max health within 1/1.25/1.5 tiles | `16_Directions.cs:71-72 (Lamp); 22_AnyDirections.cs:56-57 (LampBurst 0/0.15/0.25/0.35, LampReach 0/1/1.25/1.5), 506-517 OnDespawn` | MATCH | Look is art. |
| 11:196-206 | Fish Hook: 15/25/35% of hits snag, bleed half the hit over 3 s, up to 3/3/5 bleeds | `16_Directions.cs:73-74 (Hook); 22_AnyDirections.cs:58-62 (HookChance, HookBleeds 0/3/3/5, HookShare 0.5, HookLasts 3, HookTicks 6), 529-560 Snag, Bleeds` | MATCH | Look is art. |
| 11:207-217 | Spare Compass: 4 offered at rank I, rank II one redraw a map, 5 offered at rank III | `16_Directions.cs:75-76 (Compass); 22_AnyDirections.cs:63 (CompassMore 0/1/1/2), 136-138 CanRedraw (rank 2+ once a map); 16_Directions.cs:238 Offers 3 + MoreOffers` | MATCH | Look is art. |
| 11:218-228 | Magpie Nest: vessel and towers +1/+1.5/+2% damage per Cardinal and Trinket held, stacks counted | `16_Directions.cs:77-78 (Magpie); 22_AnyDirections.cs:65 (MagpieEach 0/0.01/0.015/0.02), 221-245 Sweep, 284 Hoard (Trinkets + RWCardinals.HeldCount)` | MATCH | Look is art. |
| 11:229-239 | Fire Ship: Duo for Lamp Oil and Chain Shot; each Chain Shot jump bursts 10% of struck foe's max health within 0.75 tiles; one rank | `16_Directions.cs:79-80 (Fireship Needs Duo:Lamp+Chain, Ranks 1); 22_AnyDirections.cs:66 (FireShipBurst 0.1, FireShipReach 0.75), 470 Chain Fire bursts` | MATCH | Look is art. |
| 11:240-250 | Tide Pool: Duo for Tidal Mend and Spring Tide; mending past full kept as extra max health up to +20%, until tide end; one rank | `16_Directions.cs:81-82 (Pool Needs Duo:Mend+Spring, Ranks 1); 16_Directions.cs:675-685 (PoolCap 0.2, Pool) and 602 Pool holder; 16_Directions.cs:594 drop each Tide` | MATCH | Look is art. |
| 11:252-262 | Deep Current (Argo): Deep Water reach +15/+30/+50% | `Argo/Code/02_Data.cs:180-188 (Deepcurrents Bonus 0.15/0.3/0.5); Argo/Code/11_Water.cs:126-128 (Reach = PoolReach*(1+bonus))` | MATCH | Vessel Argo at Argo/Code/04_Content.cs BuildDirections. Look is art. |
| 11:263-273 | Heavy Salvo (Argo): Harpoon Gun damage +10/+20/+35% | `Argo/Code/02_Data.cs:190-198 (Heavysalvos 0.1/0.2/0.35); Argo/Code/07_TowerBrain.cs:279-287 CheckSalvo (Damage Multi)` | MATCH | Look is art. |
| 11:274-284 | Chum Bucket (Argo): Slowed or pinned foes take +8/+15/+25% from the holder's buildings, on top of the Scatter bonus | `Argo/Code/02_Data.cs:201-209 (Chumbuckets 0.08/0.15/0.25); Argo/Code/09_Hooks.cs:146-176 ChumOf, 157-176 OnHurt` | MATCH | Look is art. |
| 11:285-295 | Pressure Gauge (Argo): Drill Rigs start 1/2/3 Fathoms down when built | `Argo/Code/02_Data.cs:212-220 (Pressuregauges Bonus 1/2/3); Argo/Code/07_TowerBrain.cs:886-892 StartFathom (1 + bonus)` | MATCH | Fathom 1 is the default start; the scope's 'Fathoms down' is read as Fathoms below the first. Ambiguous wording, read as built. |
| 11:297-307 | Compost Bin (Yossele): Shift, unlocks FERTILIZE; rank I 1 fertilizer per 10 foes within 4 tiles; rank II per 6 within 4; rank III per 6 within 7; desc text | `Yossele/Code/02_Data.cs:102-110 (Compostbins Kills 10/6/6, Radius 4/4/7); Yossele/Code/05_Server.cs:101-107 CompostRank, 120-140 Compost` | DIFFERENT | Numbers match. The description text differs: scope says 'Unlocks FERTILIZE for Yossele if not already unlocked, and gives a stack of fertilizer per x enemies killed in a radius around Yossele'; code desc is shorter (02_Data.cs:105-108). Look is art. |
| 11:308-318 | Dandelion Season (Yossele): Tree becomes Sapling, no limit, 70% damage/range/stages; rank II 25% more per stage; rank III +5% per other Sapling at max Growth up to +50%; clash with other Tree Directions | `Yossele/Code/02_Data.cs:125-137 (Seasons, SaplingShare 0.7, SaplingStage 0.25, SaplingPack 0.05, SaplingPackMax 0.5); Yossele/Code/09_Dandelion.cs:353-393 Shift and OnGrowth; Yossele/Code/05_Server.cs:335-343 TreeSlot; Yossele/Code/16_Directions.cs n/a; Clash 1 in 04_Content.cs:645-651` | DIFFERENT | Numbers match. Rank I desc differs: scope 'slightly weaker statwise and don't scale as well as the Tree'; code 'slightly weaker and slower to grow'. Rank II and III texts match. |
| 11:319-329 | Mother of Mushrooms (Yossele): Tree replaced by Mushboom, explodes in radius on spore stacks consuming them; Growth raises range; rank II range up; rank III consumes half and deals full damage; clash | `Yossele/Code/02_Data.cs:140-152 (Mushboomkits; MushboomEvery 8, PerStack 2, PerStage 0.5, Range 0.25); Yossele/Code/09_Dandelion.cs:405-428 Mushboom (take half at rank III, full damage); Yossele/Code/09_Dandelion.cs:363-393 Shift` | MATCH | Look is art. |
| 11:330-340 | Cutting Jar (Yossele): building that falls leaves a cutting; rebuild on same tile within 20/20/40 s keeps 25/50/75% of Growth, rounded down | `Yossele/Code/02_Data.cs:156-158 (CuttingKeep 0.25/0.5/0.75, CuttingWait 20/20/40); Yossele/Code/17_Directions.cs:192-231 YosseleCuttings (Fell, Take, FloorToInt, 0.5 tile)` | MATCH | Look is art. |
| 11:341-351 | Sap Bucket (Yossele): laser sap slows 15/25/35% for 2 s; rank III damage from your buildings +10% on sapped foes | `Yossele/Code/02_Data.cs:164-172 (SapSlow 0.15/0.25/0.35, SapLasts 2, SapBonus 0.1); Yossele/Code/17_Directions.cs:132-171 YosseleSap.Stick; Yossele/Code/12_Hooks.cs:109-125 Sap, SapOf` | MATCH | Look is art. |
| 11:352-362 | Creeping Ivy (Yossele): vines +1 tile; rank II foes on vines -15%; rank III +0.5 tile a Tide, at most +3 | `Yossele/Code/02_Data.cs:173-179 (IvyReach 1, IvyMax 3, IvySlow 0.15, IvyPerTide 0.5); Yossele/Code/10_Henge.cs:45-56 IvyBonus, 108 IvyTides, 386 vine slow` | MATCH | Look is art. |
| 11:363-373 | Deep Breath (Yossele): after 2 s without firing, next beam +40/+70/+100%; rank III +1 tile range for that beam | `Yossele/Code/02_Data.cs:181-188 (BreathBonus 0.4/0.7/1, BreathAfter 2, BreathRange 1); Yossele/Code/17_Directions.cs:27-35 Charged (reload + BreathAfter), 52-54 reach, 64-92 OnDealt` | DIFFERENT | The 2 s counts from when Yossele could fire again (reload plus 2 s), not from the last shot. The code comment says this is deliberate; the scope line says 'after 2 seconds without firing'. Bonus and range numbers match. |
| 11:374-384 | Fairy Ring (Yossele): after 1.5 s still, Yossele and buildings within 1.5/2/2.5 tiles mend 1/1.5/2% max health a second | `Yossele/Code/02_Data.cs:189-196 (FairyRadius 1.5/2/2.5, FairyHeal 0.01/0.015/0.02, FairyStill 1.5); Yossele/Code/17_Directions.cs:94-127 Fairy, Mend` | MATCH | Look is art. |
| 11:386-396 | Efficient Recycler (Spool): rank I, II and III increase Thread gained from Unthreaded minions; rank III heals units and towers round it; Thread back 65/80/100%, heals 10 within 2.5 tiles | `Spool/Code/02_Data.cs:102-111 (Recyclers Percent 65/80/100, Heal 10, Radius 2.5 at rank III); Spool/Code/08_Tin.cs:570-586 Gone Sell refund and MendAround; Spool/Code/05_Server.cs:319-334 MendAround` | DIFFERENT | Numbers and the rank III heal match. The rule differs: scope ties the Thread to 'Unthread' (UNTHREAD, queued as B19), the code gives back Thread on selling a Crafted. The Unthread mechanic is UNBUILT-QUEUED under B19. |
| 11:397-407 | Sewing Machine (Spool): Loom replaced by Sewing Machine, mines quicker, heals nearby, no Thread; rank II more healing; rank III larger radius; mines +50%, heals 10/20/20 each tide within 3/3/5 tiles; clash with other Loom Directions | `Spool/Code/02_Data.cs:113-127 (SewingMine 0.5, Sewingmachines Radius 3/3/5, Heal 10/20/20); Spool/Code/07_Building.cs:213-235 Tick (sewing stops Spin, Attackspeed Multi), Spool/Code/05_Server.cs:336-347 OnTide` | MATCH | Clash Loom Directions via Clash 1 in 04_Content.cs:678-686. Look is art. |
| 11:408-418 | Double Needle (Spool): Tins stitch faster -15/-30/-30%; rank III every third stitch makes a second Crafted for no Thread | `Spool/Code/02_Data.cs:132-138 (Doubleneedles, NeedleCut 0.15/0.3/0.3, NeedleEvery 3); Spool/Code/08_Tin.cs:26-33 StitchTime, 110-112 extra Crafted` | MATCH | Look is art. |
| 11:419-429 | Button Jar (Spool): Shift; Tins stitch two Crafted for one's Thread, each 55/65/75% health and damage; Tins hold twice as many | `Spool/Code/02_Data.cs:142-149 (ButtonShare 0.55/0.65/0.75, ButtonPair 2); Spool/Code/08_Tin.cs:25 Slots doubled, 89-98 pair, 127-128 share; Spool/Code/08_Tin.cs:Button Jar Small Pmulti in SpoolUnitBrain` | MATCH | Look is art. Kind Shift in scope; built as a Direction like the rest. |
| 11:430-440 | Pattern Book (Spool): once a Tide the first 1/2/3 Crafted of each Tin to fall are stitched again for free; ranks I and II usual time; rank III instant | `Spool/Code/02_Data.cs:151-157 (Patternbooks, PatternFree 1/2/3); Spool/Code/08_Tin.cs:57-73 Tick (instant at rank III), 133-149 Fell (once per wave)` | MATCH | Look is art. |
| 11:441-451 | Merit Badge (Spool): +3% damage and health per foe drowned, up to +15/+30/+45%; lost when it falls | `Spool/Code/02_Data.cs:159-166 (Meritbadges, MeritEach 0.03, MeritCap 0.15/0.3/0.45); Spool/Code/08_Tin.cs:501-515 Drowned (Damage and Health Multi)` | MATCH | Look is art. |
| 11:452-462 | Odd Socks (Spool): +8/+15/+25% damage beside another kind within 2 tiles; rank III also 10% less damage taken | `Spool/Code/02_Data.cs:168-175 (Oddsocks, SocksBonus 0.08/0.15/0.25, SocksRange 2, SocksGuard 0.1); Spool/Code/08_Tin.cs:517-555 Socks, OnGuarded` | MATCH | Look is art. |
| 11:464-474 | Extension Cord (Uloe): PLUG IN reach +2/+4/+6 tiles | `Uloe/Code/02_Data.cs:110 (ExtensionReach 0/2/4/6); Uloe/Code/06_HeroBrain.cs:25 Reach, 117-123 CheckDirections` | MATCH | Look is art. |
| 11:475-485 | Brighter Bulb (Uloe): Light +1/+2/+3, plugged in or not | `Uloe/Code/02_Data.cs:111 (BrightLight 0/1/2/3); Uloe/Code/06_HeroBrain.cs:165-172 Feed (adds BrightLight whether plugged or not)` | MATCH | Look is art. |
| 11:486-496 | Spare Batteries (Uloe): each Power Bank keeps +1 Photon; rank II respawn -25%; rank III +2 Photons, respawn -25% | `Uloe/Code/02_Data.cs:112-113 (BatteryUnits 0/1/1/2, BatteryRespawn 1/1/0.75/0.75); Uloe/Code/08_Bank.cs:17 Slots, 21-27 RespawnNow` | MATCH | Look is art. |
| 11:497-507 | Flash Bulb (Uloe): Shift; sparks Illuminate foes (rank I), also Blind (rank II), both harder (rank III) | `Uloe/Code/02_Data.cs:114-115 (FlashLit 0/1/1/2, FlashBlind 0/0/1/2); Uloe/Code/06_HeroBrain.cs:126-140 OnDealt` | MATCH | Look is art. Kind Shift in scope; built as a Direction. |
| 11:510-520 | Full Gallop (Buster): speed +10/+20/+30%; Buster's hits grow with his speed as in the base game | `16_Directions.cs:85-86 (Gallop, Stat Speed 0.1/0.2/0.3 Multi)` | MATCH | The hit growth is base game behaviour, not pack code. Look is art. |
| 11:521-531 | House Regulars (Fie): attack speed +10/+20/+30% | `16_Directions.cs:87-88 (Regulars, Stat Attackspeed 0.1/0.2/0.3 Multi)` | MATCH | Look is art. |
| 11:532-542 | Spare Reel (Boutman): fishing range +1/+2/+3 | `16_Directions.cs:89-90 (Longline, Stat Fishingrange Plain Add 1/2/3)` | MATCH | Look is art. |
| 11:543-553 | Packed Snow (Sno Sno): speed +8/+16/+25% | `16_Directions.cs:91-92 (Packedsnow, Stat Speed 0.08/0.16/0.25 Multi)` | MATCH | Look is art. |
| 11:554-564 | Skipping Stones (Smacky): damage +15/+30/+50% | `16_Directions.cs:93-94 (Skipping, Stat Damage 0.15/0.3/0.5 Multi)` | MATCH | Look is art. |
| 11:565-575 | Mason's Trowel (Smacky): walls health +20/+40/+60%; rank III mends 1 a second after 3 s with no hit; walls | `16_Directions.cs:96-97 (Trowel); 23_BaseDirections.cs:27-28 (TrowelHealth 0/0.2/0.4/0.6, TrowelMend 1, TrowelRest 3); 23_BaseDirections.cs:204-232 Trowel, Walls` | MATCH | Look is art. |
| 11:576-586 | Snow Shovel (Sno Sno): his snow starts at level 30/40/55 where the game's is 20 | `16_Directions.cs:98-99 (Shovel); 23_BaseDirections.cs:20 (ShovelLevel 20/30/40/55), 117-158 Trail, Topping` | MATCH | Look is art. |
| 11:587-597 | Worm Tin (Boutman): Boutbro builds for -10/-20/-30% on Gubbies, Fish Barrels and the Boutboat | `16_Directions.cs:100-101 (Wormtin); 23_BaseDirections.cs:22-26 WormCut 0/0.1/0.2/0.3 on Fishcost, Barrelcost, Boatcost; 235-245 Worm` | MATCH | Cuts Boutbro's build price stats, which price those three builds. Look is art. |
| 11:598-608 | Open Sign (Fie): Rest Stop keeps 5/6/7 regulars where the game keeps 4 | `16_Directions.cs:102-103 (Opensign); 23_BaseDirections.cs:22 (SignRegulars 4/5/6/7), 249-286 Open` | MATCH | Look is art. |
| 11:609-619 | Long Leash (Buster): rank I keeps full reach while dashing; rank II dash hits +15%; rank III +30% | `16_Directions.cs:104-105 (Leash); 23_BaseDirections.cs:23 (LeashHit 0/0/0.15/0.3), 165-180 DashCut (rank I cancels -1 Range), 290-307 OnDash` | MATCH | Look is art. |

## 2. Design pages against code

Each vessel's `DESIGN.md` (lines as D:N), every number and rule mapped to its code. The scope wins over a design page: a DIFFERENT row is fixed in whichever of the two the scope agrees with least.

| File | MATCH | DIFFERENT | NOT-IN-CODE |
| --- | --- | --- | --- |
| Argo/DESIGN.md | 67 | 3 | 0 |
| Spool/DESIGN.md | 71 | 1 | 4 |
| Uloe/DESIGN.md | 106 | 1 | 1 |
| Yossele/DESIGN.md | 74 | 1 | 1 |

### 2.1 DIFFERENT and NOT-IN-CODE (to confirm by hand)

| File | Lines | Says | Code | Mark | Note |
| --- | --- | --- | --- | --- | --- |
| Argo/DESIGN.md | 50 | FLOOD keys 1/Quickaction, adds an adjacent pool for Blue 15 | `02_Data.cs:44 (FloodLink Buildoption1), 416-417 (Pool Blueaura=15, Hidden); 07_TowerBrain.cs:138-145; 11_Water.cs:262-290` | DIFFERENT | Blue 15 and adjacency match. No 'Quickaction' key is set anywhere in the code; the FLOOD key is whatever Command_Buildoption1 carries. |
| Argo/DESIGN.md | 85 | Salt Sting3: without Brine its sting is on a default 0.5 a second; Brinepinned_Multi 1, Blue 70 and Red 30 | `02_Data.cs:172 (SaltBrine 0.5), 348 (Brinepinned_Multi=1, Blueaura=70;Redaura=30); 11_Water.cs:203-205` | DIFFERENT | The code doubles SaltBrine (0.5) on a pinned foe, so the sting is 1 a second, not 0.5. Code fx text (02_Data.cs:349) also says 1 a second. |
| Argo/DESIGN.md | 156 | Kit 1.6 comes in: ModKitPack, ModKitTides, ModKitCounter, ModKitFocus | `00_ArgoMod.cs:20-21 (header says Kit 1.7); 00_ArgoMod.cs:40, 129; 05_Server.cs:45-48; 07_TowerBrain.cs:877` | DIFFERENT | The design says Kit 1.6; the code header in 00_ArgoMod.cs says Kit 1.7. The four kit calls are present. |
| Spool/DESIGN.md | 37 | Health regen 0 | `no regen stat is set anywhere in 02_Data.cs or 04_Content.cs` | NOT-IN-CODE | Searched for Healthregen / regen stats in the hero entry; the stock value is left as is. |
| Spool/DESIGN.md | 132 | No tier-5 capstones built yet | `02_Data.cs:261-341 (Upg Tier values only 2, 3, 4)` | NOT-IN-CODE | Searched every upgrade Tier; none is 5. Matches the page's own status. |
| Spool/DESIGN.md | 153-156 | Seven Yarn Stash upgrades, Thread Head levels 2-4, Loom levels 2-4, Tin's Tacking Stitch, Cross Stitch, Patchwork Army, Pin Cushion's Iron Thimble and Voodoo Doll, the Tin's UNTHREAD, not built | `02_Data.cs:268-269 (Stash Upgrades empty); 02_Data.cs:297-303 (Head has only levels 2); 02_Data.cs:333-339 (Loom only levels 2)` | NOT-IN-CODE | Searched the build tables for these names; none is present, consistent with the page. |
| Spool/DESIGN.md | 160-161 | Directions: Efficient Recycler, Sewing Machine built by Spool; Made of Tougher Stuff built by the mode | `04_Content.cs:658-691 (BuildDirections for Recyclers and Sewingmachines); Made of Tougher Stuff: not in these files` | NOT-IN-CODE | The mode file RogueWavesMode/Code/16_Directions.cs is not in the given code; Spool's files hold only the first two. |
| Spool/DESIGN.md | 198 | Test plan: Crafted cost 10, at most 4, one every 4 s; sold Crafted refunds 5 | `02_Data.cs:276 (Units=6); 02_Data.cs:200 and 196 (Stitched Thread 10); 08_Tin.cs:25 (Slots)` | DIFFERENT | Page says at most 4 Crafted; code gives Units=6 (page says 6 in the table at line 84). |
| Uloe/DESIGN.md | D:34 | Health regen 0 | `Uloe/Code/02_Data.cs:72-75` | NOT-IN-CODE | searched HeroStats and BuildHero for a regen stat; none set, so the template value stands |
| Uloe/DESIGN.md | D:97 | Mirror level 1 at Light 1+, level 2 at 3+, level 3 at 6+ | `Uloe/Code/10_Mirror.cs:25-32; Uloe/Code/02_Data.cs:92` | DIFFERENT | code: any Light above 0 gives level 1 (l <= 0 gives 0); page says Light 1 and up |
| Yossele/DESIGN.md | D:112 | Any level 2 leads to Bonsai4 and Rubbery4 | `02_Data.cs:311-315` | DIFFERENT | Only the tier-3 nodes Picky, Weeds and D-MDS list Bonsai4 and Rubbery4; the level-2 nodes Packets and Demand do not. |
| Yossele/DESIGN.md | D:236-243 | Test plan items: stats, growth forcing, Cordyfriend, Tree rules, Stonehenge and Sapling checks | `self-test code is excluded from this supply` | NOT-IN-CODE | Searched the supplied game code; the self-test (99_SelfTest.cs and YOSSELE_SELFTEST) was left out, so these checks cannot be matched. |

### 2.2 Argo/DESIGN.md

| Lines | Says | Code | Mark | Note |
| --- | --- | --- | --- | --- |
| 11-12 | Name Argo, title Deep Sea Diver, difficulty Intermediate | `02_Data.cs:103-105` | MATCH | Displayname, Displaysub and Displayskill constants hold these values. |
| 26-32 | Health 65, Speed 2.25, Damage 4, Range 1.75, Attackspeed 0.4, Buildrange 2, Sellrate 0.5 | `02_Data.cs:127-128 (HeroStats); 04_Content.cs:696-706 (BuildHero applies them)` | MATCH | All seven stat values match the table. |
| 25 | Attack is melee (Mob_Combat) | `04_Content.cs:661-665` | MATCH | Entity_Ai_Mob_Combat is inserted into the hero's AI scripts. |
| 34 | Display_Desc Sturdy, slow melee, hits two at once | `02_Data.cs:106` | MATCH | HeroDesc text is identical. |
| 35 | Second hit: a foe within 1.25 tiles of the first takes 60% | `02_Data.cs:131-132 (CleaveReach 1.25, CleaveMulti 0.6); 06_HeroBrain.cs:196-234` | MATCH | OnHit finds the nearest foe within CleaveReach and deals Damage times Damagemulti times CleaveMulti. |
| 36 | No SONAR, no ability button | `02_Data.cs:18 (SonarEnabled false); 04_Content.cs:309; 06_HeroBrain.cs:82` | MATCH | SONAR command is only built and bound when SonarEnabled is true. |
| 37-39 | Special rule text: Swings hit two enemies, no walls of its own, Scatter Cannons punish slowed and pinned foes, Drill Rigs sink a Fathom each Tide and their aimed Tremors widen, the crew tunes towers, never traps | `02_Data.cs:108 (TooltipDesc); 04_Content.cs:627-649 (Tooltip)` | MATCH | The tooltip text is the same string and is shown as a tier -3 upgrade. |
| 40-42 | Scatter bonus reads slowed +50% or pinned +100%; Slowed and Pinned sources listed | `02_Data.cs:281 (Debuffbonus=0.5, Pinbonus=1); 07_TowerBrain.cs:387-400; 01_Util.cs:166-197 (Root, Pin); 11_Water.cs:184-223; 07_TowerBrain.cs:399,406,701,709` | MATCH | Slow sources are Deep Water, Tremor, Barnacle Shot and Tether; Pin sources are Anchor Line, Kelp Bed, Trench and Mantle quake. |
| 48 | Scatter Cannon: Tower_Argoscatter, Bomb Tower template, Red 30, Health 40+25, Damage 1.25+0.25, Attackspeed 0.6, Range 4.5, Projcount 6, Spread 50, Pierce 1, Debuffbonus 0.5, Pinbonus 1 | `02_Data.cs:29, 277-281 (Template Tower_Smackybomber, Costs Redaura=30, Stats)` | MATCH | All stats and the cost match; 6 pellets are fired per volley in 07_TowerBrain.cs:245-273. |
| 49 | Harpoon Gun: Tower_Argoharpoon, Archer Tower, Red 35, Health 40+25, Damage 5+2, Attackspeed 0.35, Range 6.5, Projspeed 18, Pierce 3 | `02_Data.cs:30, 306-308 (Template Tower_Smackyarcher, Costs Redaura=35, Stats)` | MATCH | Base gun reels only when an upgrade adds Reel_Tiles or Root_Base (07_TowerBrain.cs:357). |
| 50 | Deep Water: Trap_Argowater, Trap tag, Buster's Speed Pad template, Blue 20, Health 30+15, Slow 0.3+0.05, ceiling 0.65 | `02_Data.cs:33, 336-338 (Template Trap_Busterpad, Costs Blueaura=20, Stats); 02_Data.cs:170 (WaterSlowCap 0.65); 02_Data.cs:154 (SlowCap 0.6)` | MATCH | Tremor slow cap stays 0.6 and Deep Water's cap is 0.65, as stated. |
| 50 | FLOOD keys 1/Quickaction, adds an adjacent pool for Blue 15 | `02_Data.cs:44 (FloodLink Buildoption1), 416-417 (Pool Blueaura=15, Hidden); 07_TowerBrain.cs:138-145; 11_Water.cs:262-290` | DIFFERENT | Blue 15 and adjacency match. No 'Quickaction' key is set anywhere in the code; the FLOOD key is whatever Command_Buildoption1 carries. |
| 50 | A pinned foe in Deep Water is held twice as long; a Tremor touching a pool spreads through connected pools | `02_Data.cs:164 (WaterPinMulti 2); 01_Util.cs:184-185; 11_Water.cs:131-153; 07_TowerBrain.cs:728-750` | MATCH | Pin doubles seconds for wet foes; Shaken and TremorFoes spread the Tremor through connected water. |
| 51 | Drill Rig: Tower_Argodrill, Gatherer tag, Archer Tower, Yellow 35, Health 40+25, Damage 0.5, Attackspeed 0.25, Range 5, Tremor 2, Slow 0.2 | `02_Data.cs:32, 360-366 (Template Tower_Smackyarcher, Costs Yellowaura=35, Stats)` | MATCH | All values match. |
| 51 | Mines ore up to 2 tiles away | `02_Data.cs:156 (OreReach 2.1); 07_TowerBrain.cs:822-838` | MATCH | Flat distance up to 2.1, which is 2 tiles straight out and 1 diagonally. |
| 51 | Sinks a Fathom each Tide, to 5; +0.4 tiles Tremor and +4% slow per Fathom; faster mining | `02_Data.cs:148-153 (FathomMax 5, FathomTremor 0.4, FathomSlow 0.04, FathomYield, FathomSpeed); 07_TowerBrain.cs:864-920` | MATCH | OnTide sinks one Fathom per Tide up to FathomMax and ApplyFathom applies the per-Fathom modifiers. |
| 51 | AIM picks a Tremor spot within 5 tiles (Range) | `07_TowerBrain.cs:752-772 (Aim, reach Range + AimSlack); 02_Data.cs:173 (AimSlack 0.5); 16_AimSelect.cs:64-97` | MATCH | The server refuses a spot past Range plus 0.5, as the design's Step 3 note says. |
| 52 | Miner's Base: Tower_Argobell, no tag, Mining Hut template, Blue 20 and Yellow 15 | `02_Data.cs:31, 388-389 (Template Tower_Smackyhut, Tags none, Costs Blueaura=20;Yellowaura=15)` | MATCH | Tags is an empty array and the cost matches the review price. |
| 52 | Health 50+25, Units 2, Respawn 8 | `02_Data.cs:390 (Stats); 07_TowerBrain.cs:517 (CrewSize); 07_TowerBrain.cs:594 (Respawn)` | MATCH | Stats match; Respawn is read from the stat with NautRespawn as fallback. |
| 52 | Limit 1: its build button goes while one stands | `06_HeroBrain.cs:118-136 (CheckBase); 05_Server.cs:180-186 (HasBase)` | MATCH | Buildoption5 is removed while a Base stands and restored after it is gone. |
| 52 | PORT or STARBOARD, then click a tower (a teammate's too); the Naut tunes it: +20% attack speed and a slow repair | `02_Data.cs:78-79 (TuneReach 1.75, TuneAttackspeed 0.2), 78-79 TuneHeal 0.5; 08_NautBrain.cs:151-222; 07_TowerBrain.cs:819-841 (CrewOrder)` | MATCH | Tuning adds 20% attack speed and heals 0.5 a second; CrewOrder accepts same-team towers. |
| 52 | Towers only, never traps | `07_TowerBrain.cs:636-640 (CrewOrder Trap check); 14_CrewSelect.cs:65-66,122-126` | MATCH | Picks on Trap-tagged entities are refused on the server and the client prompt says so. |
| 54 | Units Mob_Argonaut_Port and Mob_Argonaut_Starboard, and Mob_Argonaut_Bosun from Third Mate | `02_Data.cs:63-65 (Nauts); 04_Content.cs:550-592; 02_Data.cs:404 (Third Mate Units_Add=1)` | MATCH | Bosun is a third crew entry, unlocked by Units_Add. |
| 56-57 | Scatter and Harpoon aim at each target via the Turret part; the Drill's drill spins; Deep Water bobs; the Base stays still | `10_Client.cs:349-367 (Aim, Turret); 10_Client.cs:116-123 and 169-173; 10_Client.cs:478-503 (Spin, Spinner); 10_Client.cs:505-544 (Bob, Swell); no motion code for the Base` | MATCH | Each motion is present. The Base has no motion code, which matches its being still. |
| 65 | Scatter tree: Wide Bore2 and Double Barrel2 lead to Grapeshot3 and Barnacle3; Pressure4 and Ironclad4 (ends) lead to Broadside5 | `02_Data.cs:284-301 (Scatter Upgrades and Next lists)` | MATCH | Next links match, with Ironclad4 as the end. |
| 66 | Harpoon tree: Long Line2 and Tether2 lead to Skewer3, Anchor Line3 (ends), Reel3; Whaler4, Twin Line4, Leviathan5, Undertow5 | `02_Data.cs:311-330 (Next lists)` | MATCH | Code links are Skewer to Whaler, Reel to Twin Line, Whaler to Leviathan and Twin Line to Undertow. The design line is terse but agrees with these. |
| 67 | Deep Water tree: Undercurrent2 and Brine2 lead to Kelp Bed3 and Salt Sting3; Abyss4 (ends) and Crushing Depth4 lead to Trench5 | `02_Data.cs:341-356 (Next lists)` | MATCH | Next links match, with Abyss4 as the end. |
| 68 | Drill tree: Pay Dirt2 and Seismic2 lead to Deep Core3 and Bedrock3; Gusher4 and Fault Line4 (ends) lead to Mantle Tap5 | `02_Data.cs:369-383 (Next lists)` | MATCH | Next links match, with Fault Line4 as the end. |
| 69 | Base tree: Flippers2 and Old Salts2 lead to Overhaul3 and Calibrate3; Third Mate4 and Bulkhead4 (ends) lead to Full Crew5 | `02_Data.cs:395-409 (Next lists)` | MATCH | Next links match, with Bulkhead4 as the end. |
| 74 | Wide Bore2: cone +10, +2 pellets, Red 30 | `02_Data.cs:285 (Projcount_Add=2;Spread_Add=10, Redaura=30)` | MATCH | Values match. |
| 75 | Double Barrel2: up to 3 volleys, a volley every 2 s, Red 30 | `02_Data.cs:287 (Double_Base=3;Double_Every=2, Redaura=30); 07_TowerBrain.cs:210-242` | MATCH | Stored volleys are capped at Double_Base and spaced by Double_Every. |
| 76 | Ironclad4: Health_Multi 1.5, Healtharmor_Base 2, Red 250 and Blue 150, ends | `02_Data.cs:298 (Health_Multi=1.5;Healtharmor_Base=2, Redaura=250;Blueaura=150); 07_TowerBrain.cs:987-997 (armour)` | MATCH | Armour is added as Healtharmor. |
| 77 | Tether2: slow 0.3 for 2 s, Red 30, next Anchor3 and Reel3 | `02_Data.cs:314 (Tetherslow_Base=0.3;Tetherslow_Duration=2, Redaura=30); 07_TowerBrain.cs:403-408` | MATCH | Each harpoon hit slows the foe by 0.3 for 2 s. |
| 78 | Anchor Line3: pins the foe hit 1 s, stunned beside a building, Red 60 and Blue 50, ends | `02_Data.cs:318 (Root_Base=1;Pinstun_Base=1, Redaura=60;Blueaura=50); 07_TowerBrain.cs:457-467; 01_Util.cs:180-197 (Pin, stun via Beside)` | MATCH | Stun applies only when a building is within StunReach. |
| 79 | Reel3: Reel_Tiles 1.5, Red 60 and Blue 50, next Twin Line4 | `02_Data.cs:320 (Reel_Tiles=1.5, Redaura=60;Blueaura=50); 07_TowerBrain.cs:471-498` | MATCH | The foe reels up to ReelTiles plus any TunedReel. |
| 80 | Twin Line4: Twin_Base 0.3, Red 350, next Undertow5 | `02_Data.cs:324 (Twin_Base=0.3, Redaura=350; Next Undertow5); 07_TowerBrain.cs:335-342` | MATCH | A second harpoon follows 0.3 s later. |
| 81 | Undertow5: radius 1.5, tiles 1.5, Red 1750 and Blue 1750 | `02_Data.cs:328 (Undertow_Radius=1.5;Undertow_Tiles=1.5, Redaura=1750;Blueaura=1750); 07_TowerBrain.cs:483-497` | MATCH | Foes within the radius are reeled as well. |
| 82 | Undercurrent2: Slow_Add 0.1, Blue 20, next Kelp Bed3 and Salt Sting3 | `02_Data.cs:342 (Slow_Add=0.1, Blueaura=20; Next Kelp3,Salt3)` | MATCH | Values match. |
| 83 | Brine2: Brine_Damage 0.5, Blue 25, next Salt Sting3 | `02_Data.cs:344 (Brine_Damage=0.5, Blueaura=25; Next Salt3); 11_Water.cs:202-211` | MATCH | Brine damage per second is applied each water tick. |
| 84 | Kelp Bed3: Catch_Duration 0.5 as a pin, Blue 80, next Abyss4 and Crushing Depth4 | `02_Data.cs:346 (Catch_Duration=0.5, Blueaura=80; Next Abyss4,Crushing4); 11_Water.cs:189-199` | MATCH | Kelp Bed uses Pin, so it is a pin held 1 s in water. |
| 85 | Salt Sting3: without Brine its sting is on a default 0.5 a second; Brinepinned_Multi 1, Blue 70 and Red 30 | `02_Data.cs:172 (SaltBrine 0.5), 348 (Brinepinned_Multi=1, Blueaura=70;Redaura=30); 11_Water.cs:203-205` | DIFFERENT | The code doubles SaltBrine (0.5) on a pinned foe, so the sting is 1 a second, not 0.5. Code fx text (02_Data.cs:349) also says 1 a second. |
| 86 | Abyss4: FLOOD fills two tiles at once, Flood_Add 1, Slow_Add 0.1, Blue 260, ends; slow whole at 0.65 | `02_Data.cs:350 (Flood_Add=1;Slow_Add=0.1, Blueaura=260); 02_Data.cs:170 (WaterSlowCap 0.65); 11_Water.cs:287-293` | MATCH | Extra free pool is added and Slow is capped at 0.65. |
| 87 | Crushing Depth4: Damagetaken_Multi 0.15, Blue 300, next Trench5 | `02_Data.cs:352 (Damagetaken_Multi=0.15, Blueaura=300; Next Trench5); 09_Hooks.cs:156-192; 11_Water.cs:212-215` | MATCH | Foes in the water take 15% more through ArgoHooks.Soak. |
| 88 | Trench5: Spread_Per_Tide 2, Dragunder_Every 3, Dragunder_Duration 1 (2 s in water), Blue 3000, ends | `02_Data.cs:354 (Spread_Per_Tide=2;Dragunder_Every=3;Dragunder_Duration=1, Blueaura=3000); 11_Water.cs:217-229, 232-243` | MATCH | Two free pools per Tide and a drag under every 3 s, each pin doubled in water. |
| 89 | Bedrock3: Bedrock_Damage 1.5 a second on pinned foes in the Tremor, Yellow 80 and Red 30, next Fault Line4 | `02_Data.cs:376 (Bedrock_Damage=1.5, Yellowaura=80;Redaura=30; Next Faultline4); 07_TowerBrain.cs:712-724` | MATCH | Damage is applied each second to pinned foes inside the Tremor. |
| 90 | Fault Line4: Fault_Damage 12, Yellow 300, ends | `02_Data.cs:380 (Fault_Damage=12, Yellowaura=300); 07_TowerBrain.cs:801-812` | MATCH | A foe reeled into or across the Tremor takes 12 damage. |
| 91 | Seismic2: Slow_Add 0.1, Tremor_Add 0.5, Yellow 35, next Bedrock3 | `02_Data.cs:372 (Slow_Add=0.1;Tremor_Add=0.5, Yellowaura=35; Next Bedrock3)` | MATCH | Values match. |
| 92 | Gusher4: Gusher_Base 4 strikes per Fathom from Fathom 3, Yellow 275, next Mantle Tap5 | `02_Data.cs:378 (Gusher_Base=4, Yellowaura=275; Next Mantle5); 07_TowerBrain.cs:906-920` | MATCH | Burst is Gusher_Base times Fathom and only from Fathom 3. |
| 93 | Mantle Tap5: Attackspeed_Pmulti 0.5, Mantle_Base 2, Mantle_Every 20, Yellow 3000 | `02_Data.cs:382 (Attackspeed_Pmulti=0.5;Mantle_Base=2;Mantle_Every=20, Yellowaura=3000); 07_TowerBrain.cs:706-710, 975-979` | MATCH | Pmulti multiplies attack speed and a quake pins the Tremor's foes every 20 s. |
| 94 | Old Salts2: Drill Rig's Tremor slow +10% (not Deep Water), Salts_Base 1, Blue 30, next Overhaul3 | `02_Data.cs:398-399 (Salts_Base=1, Blueaura=30; Next Overhaul3); 08_NautBrain.cs:166-180 (SaltsSlow 0.1 on Drill)` | MATCH | Only the Drill branch changes the Tremor slow; Deep Water is not touched. |
| 98-99 | Five buildables: a fifth build option cloned from Command_Buildoption4 with its own key | `04_Content.cs:402-429 (BuildOption5); 06_HeroBrain.cs:96-113` | MATCH | Command_Buildoption5 is cloned from Buildoption4 with keys changed from 4 to 5. |
| 102-103 | 1.4.0 loaded guns (Drill Tremor adds pellet or pierce per Fathom) is dropped | `07_TowerBrain.cs:864-884 (ApplyFathom touches only Damage, Attackspeed, Tremor, Slow)` | MATCH | No rule adds pellets or pierce from a Tremor; the absence matches. |
| 148-150 | FLOOD is a clone of Command_Buildoption1 aimed at Trap_Argowater_Pool (Blue 15, hidden), sent as Build, adjacency checked, spawned; Abyss adds Flood_Add free pools | `04_Content.cs:378-397 (FloodCommand); 07_TowerBrain.cs:138-145; 11_Water.cs:249-294 (Flood, adjacency Touching), 299-335 (FreePool), 339-349 (Spawn)` | MATCH | Each step is present. |
| 152 | Water networks rebuilt every 0.25 s; the most-upgraded pump leads | `02_Data.cs:166 (WaterTick 0.25); 11_Water.cs:71-76 (Update), 94-131 (Rebuild, Lead by UpgradeCount)` | MATCH | Both values match. |
| 154 | AIM refuses a tile past Range + 0.5; clients read centre and radius from the AIM command | `02_Data.cs:173 (AimSlack 0.5); 07_TowerBrain.cs:752-799 (Aim, SyncAim); 10_Client.cs:642-666 (Aimed, AimEntry)` | MATCH | Server check and client read-back both present. |
| 155 | Tremor is a stat: 2 at Fathom 1, +0.4 per Fathom, Seismic +0.5 | `07_TowerBrain.cs:68 (TremorRadius = Stat Tremor default 2); 02_Data.cs:152, 372; 07_TowerBrain.cs:874` | MATCH | The Tremor stat carries the base, the Fathom increase and Seismic's Tremor_Add. |
| 156 | Kit 1.6 comes in: ModKitPack, ModKitTides, ModKitCounter, ModKitFocus | `00_ArgoMod.cs:20-21 (header says Kit 1.7); 00_ArgoMod.cs:40, 129; 05_Server.cs:45-48; 07_TowerBrain.cs:877` | DIFFERENT | The design says Kit 1.6; the code header in 00_ArgoMod.cs says Kit 1.7. The four kit calls are present. |
| 157 | A standalone Argo already loaded makes the pack's Argo stand down | `00_ArgoMod.cs:56-60` | MATCH | ModKitPack.Taken causes Notice and yield break. |
| 158 | Deep Water bobs: Swell part raised and lowered 0.012 over 3.2 s, frame still | `10_Client.cs:508 (SwellAmp 0.012, SwellPeriod 3.2); 10_Client.cs:510-544` | MATCH | Only the Swell part moves. |
| 164-165 | Five build options appear and each builds; Miner's Base button goes once one stands and returns after a sell | `06_HeroBrain.cs:74-136; 05_Server.cs:173-186` | MATCH | Build options are created and the Base button is removed and restored. |
| 166 | Scatter: +50% to slowed and +100% to pinned foes, none to clean foes; Ironclad health | `07_TowerBrain.cs:387-396; 02_Data.cs:281, 298` | MATCH | Bonus is applied only to debuffed foes. |
| 167-168 | Harpoon: base gun pierces and moves nobody; Anchor Line pins 1 s and stuns beside a building; Reel moves toward the gun; Undertow reels around a hit; boss never moved | `07_TowerBrain.cs:351-370, 458-498; 01_Util.cs:199 (Boss), 253-283 (ReelTile skips Boss)` | MATCH | Bosses are never moved (MaxHealth at or above BossHealth 600). |
| 169-170 | Deep Water slows inside; FLOOD adds adjacent pool for Blue 15; pinned foe in a pool held 2 s; Tremor spreads through connected pools | `11_Water.cs:157-230, 262-290; 01_Util.cs:184-185; 07_TowerBrain.cs:728-750` | MATCH | Each rule is implemented. |
| 171-172 | Drill Rig: AIM moves the Tremor; client ring at server radius; Fathoms widen it; Bedrock hurts pinned foes; Fault Line bursts reeled foes | `07_TowerBrain.cs:752-812; 10_Client.cs:615-640 (Tremor ring); 07_TowerBrain.cs:864-884; 07_TowerBrain.cs:712-724` | MATCH | Each effect is implemented. |
| 173 | Miner's Base: a Naut picked onto Deep Water is refused (towers only) | `07_TowerBrain.cs:636-640` | MATCH | Trap tag is refused server side. |
| 174 | F selects Argo after a respawn with the crew out (F-key fix) | `00_ArgoMod.cs:129 (ModKitFocus.Update)` | MATCH | The kit's ModKitFocus fix runs every frame; the respawn case is handled in the kit, not in Argo's code. |
| 190-195 | Deep Current +15, +30, +50 percent Deep Water reach | `02_Data.cs:182-187 (Bonus 0.15, 0.3, 0.5); 11_Water.cs:126-128` | MATCH | Reach multiplier applied by the lead pump's owner's rank. |
| 192-193 | Heavy Salvo: Harpoon Guns +10, +20, +35 percent Damage | `02_Data.cs:190-197 (Bonus 0.1, 0.2, 0.35); 07_TowerBrain.cs:277-287` | MATCH | Held Multi on each gun's Damage. |
| 194 | Chum Bucket: slowed or pinned foe takes +8, +15, +25 percent from the holder's buildings | `02_Data.cs:201-209 (Bonus 0.08, 0.15, 0.25); 09_Hooks.cs:146-154, 174-176` | MATCH | Applied on top of the Scatter's own bonus. |
| 195 | Pressure Gauge: a new Drill Rig starts 1, 2 or 3 Fathoms down, never past its deepest | `02_Data.cs:212-220 (Bonus 1, 2, 3); 07_TowerBrain.cs:886-892 (StartFathom, Clamp to deepest)` | MATCH | Counted below the first Fathom, as stated. |

### 2.3 Spool/DESIGN.md

| Lines | Says | Code | Mark | Note |
| --- | --- | --- | --- | --- |
| 13 | Name Spool, title Crafty Crafted Crafter, difficulty Advanced | `02_Data.cs:54-56 (Displayname, Displaysub, Displayskill)` | MATCH | Strings match. |
| 29 | Lore is a placeholder line in the journal | `02_Data.cs:61 (Lore); 13_Journal.cs:26` | MATCH | Lore text is the placeholder and is passed to the journal. |
| 36 | Health 45 | `02_Data.cs:78 (HeroStats Health=45)` | MATCH |  |
| 37 | Health regen 0 | `no regen stat is set anywhere in 02_Data.cs or 04_Content.cs` | NOT-IN-CODE | Searched for Healthregen / regen stats in the hero entry; the stock value is left as is. |
| 35 | Attack is melee, Mob_Combat like Smacky | `04_Content.cs:746-747 (BuildHero inserts Entity_Ai_Mob_Combat)` | MATCH |  |
| 38 | Speed 2.75 | `02_Data.cs:78 (Speed=2.75)` | MATCH |  |
| 39 | Damage 2.5 | `02_Data.cs:78 (Damage=2.5)` | MATCH |  |
| 40 | Range 1.5 | `02_Data.cs:78 (Range=1.5)` | MATCH |  |
| 41 | Attackspeed 0.6 | `02_Data.cs:78 (Attackspeed=0.6)` | MATCH |  |
| 42 | Buildrange 2.5 | `02_Data.cs:78 (Buildrange=2.5)` | MATCH |  |
| 43 | Sellrate 0.5 | `02_Data.cs:78 (Sellrate=0.5)` | MATCH |  |
| 45 | Display_Desc Crafty melee, commands a stitched army | `02_Data.cs:57 (HeroDesc); 04_Content.cs:742` | MATCH |  |
| 46-47 | Special rule Hand Stitched: melee, foes drop Thread, Crafted cost Thread, PATCH mends | `02_Data.cs:58-59 (TooltipName, TooltipDesc); 04_Content.cs:720-721 (Tooltip)` | MATCH | Tooltip is the tier -3 Upgradesinit entry. |
| 48 | PATCH is a picker on keys 6 / 7, Onpressed, priority 6, available from the start | `04_Content.cs:393-395 (keys 6 and 7, Onpressed, priority 6 for ability commands); 412-411 Command(...) for Patch` | MATCH |  |
| 49 | 20 Thread from the Yarn Stash heals by 20% of max health, at most 500 | `02_Data.cs:95-96 (PatchCost 20, PatchHeal 0.2, PatchMax 500); 06_HeroBrain.cs:325 (heal = min(max*0.2, 500)) and 319 (SpendStash 20)` | MATCH |  |
| 50 | Range 4 tiles from Spool, cooldown 5 s | `02_Data.cs:96 (PatchRange 4f, PatchCooldown 5f); 06_HeroBrain.cs:300 (range check with PatchRange + 0.6)` | MATCH | The code adds a 0.6 tolerance on the range check. |
| 51 | Hotkey falls back to the thing under the cursor, else the most hurt in range | `06_HeroBrain.cs:282-293 (Patch: target null then MostHurt); 12_Commands.cs:77-84 (keyed with no pick sends empty json)` | MATCH |  |
| 51-52 | Refused with an owner-only line when nothing hurt, out of reach, cooling, Stash short; Spool itself is not a target | `06_HeroBrain.cs:286 (WarnCooldown), 297 (WarnNoTarget), 303 (WarnFar), 310 (WarnFull), 322 (WarnShort); 157-158 Patchable excludes Master` | MATCH |  |
| 56-57 | Thread kept per player on the host: Yarn Stash and carried; carried up to 20 | `05_Server.cs:7-20 (SpoolPlayer Stash and Carried); 02_Data.cs:85 (CarryHero=20)` | MATCH |  |
| 58-59 | Start 10 in the Stash when placed; cap 50, +25 per Stash level; reset between maps; carried kept on Spool's death | `02_Data.cs:83-84 (StashStart 10, StashCap 50); 02_Data.cs:268 (Threadcap=50+25); 07_Building.cs:159-162 (Placed, Stash 10); 05_Server.cs:72-73 (players cleared on new game entity)` | MATCH | Carried is kept in SpoolPlayer while the hero is down; no code zeroes it on death. |
| 60-61 | Drops: 1 per small foe, 3 for base health 40 or more, +2 elite, at most 5 | `02_Data.cs:86-87 (DropSmall 1, DropLarge 3, DropElite 2, DropMax 5, LargeHealth 40); 05_Server.cs:280-287 (FoeFell)` | MATCH |  |
| 61 | Elite is any Enemy_* modifier | `05_Server.cs:294-304 (Elite: ModKitUtil.Elite plus RW_Modifier_ keys)` | MATCH | The code also counts the pack's own RW_Modifier_ keys, which the page does not mention. |
| 62 | A drop lasts 15 s; drops land only while some player is Spool; one drop per foe, first carrier takes it | `02_Data.cs:88 (DropLife 15); 05_Server.cs:282 (players.Count == 0 returns); 05_Server.cs:216 (Until); 05_Server.cs:225-247 (Collect)` | MATCH | Merging (see 71-72) can combine drops from several foes. |
| 63 | Spool picks up within 1 tile; a Thread Head pulls in within 3 tiles | `02_Data.cs:89 (PickHero 1f, PickHead 3f); 06_HeroBrain.cs:138 (PickHero); 09_Head.cs:19 and 160 (Collect stat 3)` | MATCH |  |
| 64 | Each pickup flies to the carrier; a carrier takes only what it has room for | `11_Client.cs:118 (Spool_Take runs Fly); 05_Server.cs:233-239 (room limit in Collect)` | MATCH |  |
| 65 | Drop-off within 1.5 tiles of the Yarn Stash; carried Thread goes in up to the cap; rest stays carried | `02_Data.cs:89 (DropOff 1.5f); 05_Server.cs:160-161 (Deliver put = min(carried, Room)); 06_HeroBrain.cs:152-155` | MATCH |  |
| 66 | Thread is spent on Crafted (Tin draws from the Stash wherever it stands) and PATCH | `08_Tin.cs:78 (SpendStash cost); 06_HeroBrain.cs:319 (SpendStash for PATCH)` | MATCH |  |
| 67-68 | Selling a Crafted refunds half its Thread rounded down into the Stash; overflow drops beside it | `07_Building.cs (not here): 07_Building.cs is outside; see 04-level: 06? Tin sell: 07_Building.cs n/a; 08 uses Cost/2 in SpoolUnitBrain.Gone at 09? see 06/08 note` | MATCH | Implemented in 08_Tin.cs:Gone's counterpart SpoolUnitBrain.Gone at 08_Tin.cs:574-586 (back = Cost/2 integer division, AddStash, Drop of overflow at 579-581). |
| 69-70 | Panel shows Carried and Stash on Spool; Thread on the Yarn Stash; Loom shows Thread held; Thread Head shows Carried | `05_Server.cs:143-152 (Show); 07_Building.cs (Loom ShowHeld 09/07 file: ShowHeld in 07_Building.cs:279); 09_Head.cs:83 (Show)` | MATCH |  |
| 71-72 | Ground Thread is not networked; clients draw tufts from Vis; a new drop within half a tile merges | `05_Server.cs:194-222 (Drop, Vis Spool_Tuft); 05_Server.cs:203 (Merge 0.5 test); 11_Client.cs:254-277 (SetTuft)` | MATCH |  |
| 83 | Yarn Stash: template Tower_Smackyhut, free, limit 1, Health 100 (+50/level), Thread cap 50 (+25/level), drop-off point, Thread Heads respawn there | `02_Data.cs:265-268 (Stash template, Costs empty, Health=100+50, Threadcap=50+25); 06_HeroBrain.cs:85 (only the Stash until a home exists); 07_Building.cs:153-158 (second stash only warns)` | MATCH | Limit 1 is enforced by the build menu, not by a count; a second Stash can still be built and only warns. |
| 84 | Repurposed Cookie Tin: Red 50, Health 60 (+25/level), Crafted 6, one every 4 s | `02_Data.cs:273-276 (Costs Redaura=50, Health=60+25, Units=6, Respawn=4)` | MATCH |  |
| 85 | Thread Head: Red 40, no limit, Health 60, Damage 2, Range 1.5, Attackspeed 0.6, Speed 2.25, carries 20, collects 3, leash 3, respawn 15 | `02_Data.cs:224-227 (HeadStats) and 293-294 (Costs Redaura=40); 02_Data.cs:85 (CarryHead 20)` | MATCH | No cap on the number of Heads is written anywhere. |
| 86 | Pin Cushion: Blue 20, Health 100 (+40/level), reflect 20% melee, 1 Thread per 25 damage taken, in-game line says to build first | `02_Data.cs:307-310 (Blueaura=20, Health=100+40, Reflect=0.2, Spin=25) and 308 (Desc Build it first)` | MATCH |  |
| 87 | Loom: Yellow 40, Health 40 (+20/level), Range 2.5, Damage 1, Attackspeed 0.3, spins 1 every 6 s up to 10 held | `02_Data.cs:330-332 (Yellowaura=40, Health=40+20, Damage=1, Attackspeed=0.3, Range=2.5, Spinrate=6, Hold=10)` | MATCH |  |
| 89-90 | Yarn Stash sold for nothing; destroyed, its Thread spills as drops and the menu goes back to the Stash (rebuilt free) | `07_Building.cs:170-191 (SpoolStashBrain.Gone spills on killed); 06_HeroBrain.cs:86 (menu returns to Stash when no home)` | MATCH | No refund code exists for a sold Stash; the stock refund of an empty cost gives nothing. |
| 92 | Cookie Tin: 6 Crafted alive at once; one stitched every 4 s while the Stash has the Thread | `02_Data.cs:276 (Units=6); 08_Tin.cs:25 (Slots from Units); 08_Tin.cs:112 (StitchTime from Respawn 4); 08_Tin.cs:78 (SpendStash)` | MATCH |  |
| 93-94 | TARGET on keys Quickaction / 5 picks a walkable tile within 8 tiles a Crafted can reach; a pin marker stands there | `04_Content.cs:393 and 423-425 (TARGET as Command, keys Quickaction and 5 via Command()); 02_Data.cs:276 (Reach=8); 08_Tin.cs:174-210 (Reachable); 08_Tin.cs:212-227 (MovePin)` | MATCH |  |
| 95-97 | Until a spot is picked Crafted guard the Tin; new Crafted run to the current spot; Crafted already out keep their spot; no limit on Tins | `08_Tin.cs:121 (u.Spot = HasSpot ? Spot : Pos); 08_Tin.cs:208-217 is Reachable (no change to existing Crafted); 08_Tin.cs:121-122 and 06 Anchor logic in 08_Tin.cs:355-357` | MATCH | Existing Crafted keep their Spot because MovePin does not touch them. |
| 98-104 | Thread Head bought from the build menu, placed through a Buster pad token, guards its tile; SPOT moves it; RETURN sends it to the Stash; killed, drops carried and returns after 15 s with upgrades; sold, gone | `02_Data.cs:585-86 and 590 (token block on Trap_Busterpad via 04_Content.cs:609-618 PadBeing); 05_Server.cs:450-469 (SwapTokens); 09_Head.cs:24 and 122-133 (Spot, Standable); 09_Head.cs:135-141 (StartReturn); 09_Head.cs:272-292 (Gone: comeback at Respawn 15 with upgrades); 09_Head.cs:274-275 (drops carried)` | MATCH | Refund on sale (Red 40 times Sellrate) is the stock refund; no code sets it. |
| 105-107 | Pin Cushion is a wall like Smacky's; a melee hit (Range 2.25 or less) pricks the attacker for 20%, with kill credit; every 25 damage spins 1 Thread beside it | `02_Data.cs:307 (Tower_Smackywall template); 10_Cushion.cs:13 (MeleeRange 2.25) and 76-78 (Reflect 0.2 prick); 10_Cushion.cs:100-112 (Prick via ModKitUtil.Hurt); 10_Cushion.cs:115-126 (Spin per 25)` | MATCH | Kill credit is handled inside ModKitUtil.Hurt, which is not in the code given, so the credit itself is unverified. |
| 108-110 | Loom strikes ore within 2.5 tiles 0.3 times a second; spins 1 every 6 s while holding fewer than 10; Spool picks held Thread within 1.5, Head within 3 | `07_Building.cs:237-251 (Strike, Range and Attackspeed from stats); 07_Building.cs:253-268 (Spin); 02_Data.cs:330-332 (Spinrate 6, Hold 10); 05_Server.cs:251-268 (CollectLooms); 06_HeroBrain.cs:139 (LoomPick 1.5); 09_Head.cs:161 (Head collects Looms within Reach 3)` | MATCH |  |
| 116 | Stitched: Health 20, Damage 5, Range 1.5, Attackspeed 0.8, Speed 1.8, leash 2, Thread 10, melee | `02_Data.cs:196-199 (Stats Health=20, Speed=1.8, Damage=5, Range=1.5, Attackspeed=0.8; Leash 2f; Thread 10)` | MATCH |  |
| 117 | Dart: Health 12, Damage 5.5, Range 3.5, Attackspeed 0.6, Speed 1.6, leash 1, Thread 15, ranged | `02_Data.cs:200-203 (Health=12, Speed=1.6, Damage=5.5, Range=3.5, Attackspeed=0.6, Projspeed=14; Leash 1f; Thread 15; Ranged)` | MATCH |  |
| 118 | Seam: Health 10, Damage 1, Range 1.5, Attackspeed 0.8, Speed 1.5, leash 2.5, free, respawn 10 s, mines | `02_Data.cs:204-207 (Health=10, Speed=1.5, Damage=1, Range=1.5, Attackspeed=0.8; Leash 2.5f); 02_Data.cs:337-338 (Seamrespawn_Base=10); 07_Building.cs:282-293 (KeepSeam)` | MATCH |  |
| 119 | Mangled: Health 15 (45 Overstuffed), Damage 1.5, Range 1.5, Attackspeed 0.8, Speed 2, leash 2, melee, 1 per 40 damage, max 3 alive | `02_Data.cs:208-215 (Mangled Health=15, Damage=1.5, Speed=2, Leash 2f; Stuffed Health=45, Leash 0.5f); 10_Cushion.cs:128-149 (Stuff: Mangled_Per 40, Mangled_Max 3)` | MATCH | Stuffed Leash is 0.5 in code, which the page does not state. |
| 121-123 | Stock fighter AIs; Dart uses Entity_Ai_Mob_Ranged with a needle cloned from the archer arrow; brain only walks; Seam mines by striking ore | `04_Content.cs:497 (Entity_Ai_Mob_Combat / Entity_Ai_Mob_Ranged); 04_Content.cs:356-368 (BuildProjectile clones Tower_Smackyarcher_Arrow); 07_Building.cs (Seam mining in 07:443-458 Mine)` | MATCH | Mine is in 08_Tin.cs:443-458 (SpoolUnitBrain.Mine). |
| 124-126 | Stitched and Darts run to the Tin's spot ignoring foes, then guard within leash; Seam fights within leash or mines; Mangled guards its Cushion | `08_Tin.cs:355-357 (Anchor); 08_Tin.cs:395-430 (Guard); 08_Tin.cs:443-458 (Mine); 08_Tin.cs:386-392 (Think: Seam mines)` | MATCH |  |
| 127 | Only Tin Crafted are sellable (half Thread back); Seams and Mangled cost nothing and refund nothing | `02_Data.cs:200 and 196 (Sellable=true on Stitched and Dart only); 08_Tin.cs:574-586 (Gone sell refund)` | MATCH |  |
| 137 | Reuse Reduce Recycle: a killed Crafted drops half its cost, rounded up; Red 30; next Discounted, New Design | `02_Data.cs:279-280 (Recycle_Base=0.5, Redaura=30, Next Discounted3,Design3); 08_Tin.cs:593-594 (CeilToInt(Cost*Recycle))` | MATCH |  |
| 138 | Woolen Lining: Crafted health +50%; Red 30; next Discounted, New Design | `02_Data.cs:281-282 (Craftedhealth_Multi=0.5, Redaura=30, Next Discounted3,Design3); 08_Tin.cs:125 and 07_Building? SpoolUnitBrain.Begin 308-311 (Health Multi)` | MATCH |  |
| 139 | Discounted Material: Stitched cost 8 and walk 30% faster; Red 90; next One Last Surprise | `02_Data.cs:283-284 (Stitchedcost_Base=8, Stitchedspeed_Multi=0.3, Redaura=90, Next Surprise4); 08_Tin.cs:41-42 (cost 8); 08_Tin.cs:126 (speed 0.3)` | MATCH |  |
| 140 | New Design: the Tin stitches Darts; Red 100; next One Last Surprise | `02_Data.cs:285-286 (Dart_Base=1, Redaura=100, Next Surprise4); 08_Tin.cs:36 (Kind = Dart when Design)` | MATCH |  |
| 141 | One Last Surprise: a falling Crafted hurts every foe within its attack range for 3 times its damage; Red 400 | `02_Data.cs:287-288 (Burst_Times=3, Redaura=400); 08_Tin.cs:597-599 (damage = Damage*Burst within Range)` | MATCH |  |
| 142 | Threaded Delivery: full, it walks to the Stash, drops off and returns to its spot; Red 30 | `02_Data.cs:299-300 (Delivery_Base=1, Redaura=30); 09_Head.cs:187-188 (Has Delivery and Carried>=Cap starts return); 09_Head.cs:179-184 (returns to Spot)` | MATCH |  |
| 143 | Patchwork Job: MEND toggle, 2 health a second to each unit within 2 tiles, 1 Thread per 5 health; Red 30 | `02_Data.cs:301-302 (Mend_Base=2, Mendrange_Base=2, Mendper_Base=5, Redaura=30); 09_Head.cs:226-266 (Mend); 09_Head.cs:114-119 (MEND toggle)` | MATCH |  |
| 144 | Sharper Pins: reflects 40% of melee damage; Blue 30; next Pin Barrage, Loose Stuffing | `02_Data.cs:313-314 (Reflect_Add=0.2 on top of Reflect=0.2, Blueaura=30, Next Barrage3,Stuffing3); 10_Cushion.cs:76 (Stat Reflect)` | MATCH | Reflect_Add stacks onto the base 0.2 through the stat modifier, giving 0.4. |
| 145 | Slow Unravel: half of each hit now, half over the next 4 s, Thread spun twice; Blue 30; next Pin Barrage, Loose Stuffing | `02_Data.cs:315-316 (Unravel_Base=4, Spin_Multi=-0.5, Blueaura=30, Next Barrage3,Stuffing3); 10_Cushion.cs:80-89 (half now, owed over secs); 10_Cushion.cs:115-117 (Spin Stat halved gives twice the Thread)` | MATCH |  |
| 146 | Pin Barrage: also reflects 20% of ranged damage; Blue 100; ends the branch | `02_Data.cs:317-318 (Rangedreflect_Base=0.2, Blueaura=100, Next none); 10_Cushion.cs:76 (ranged reflect)` | MATCH |  |
| 147 | Loose Stuffing: 1 Mangled per 40 damage taken, at most 3 alive; Blue 90; next Overstuffed, Burst Seams | `02_Data.cs:319-320 (Mangled_Per=40, Mangled_Max=3, Blueaura=90, Next Overstuffed4,Burst4); 10_Cushion.cs:128-149 (Stuff)` | MATCH |  |
| 148 | Overstuffed: Mangled health x3, each leaps up to 2 tiles toward the foe that hit the Cushion and holds, blocking like a Snoble; Blue 400 | `02_Data.cs:321-322 (Leap_Base=2, Blueaura=400); 10_Cushion.cs:145-146 (Leap, LeapAt); 07_Building.cs (08_Tin.cs:467-485 LeapIn, 377-385 Holding); 04_Content.cs:528-535 (Snoble block for Stuffed); 02_Data.cs:215 (Stuffed Health=45)` | MATCH | Health x3 is a fixed 45 on the Overstuffed unit, not a modifier from the upgrade. |
| 149 | Burst Seams: a Mangled that reaches a foe bursts for 5 damage within 1.25 tiles; Blue 400 | `02_Data.cs:323-324 (Seams_Damage=5, Seams_Radius=1.25, Blueaura=400); 08_Tin.cs:487-497 (BurstOn)` | MATCH |  |
| 150 | Direct Line: spun Thread goes into the Stash while it has room; Yellow 30 | `02_Data.cs:335-336 (Direct_Base=1, Yellowaura=30); 07_Building.cs:253-258 (Spin: AddStash when Has Direct and room)` | MATCH |  |
| 151 | Seam Allowance: one Seam per Loom; Yellow 30 | `02_Data.cs:337-338 (Seam_Base=1, Yellowaura=30); 07_Building.cs:282-294 (KeepSeam, one seam at a time)` | MATCH |  |
| 132 | No tier-5 capstones built yet | `02_Data.cs:261-341 (Upg Tier values only 2, 3, 4)` | NOT-IN-CODE | Searched every upgrade Tier; none is 5. Matches the page's own status. |
| 153-156 | Seven Yarn Stash upgrades, Thread Head levels 2-4, Loom levels 2-4, Tin's Tacking Stitch, Cross Stitch, Patchwork Army, Pin Cushion's Iron Thimble and Voodoo Doll, the Tin's UNTHREAD, not built | `02_Data.cs:268-269 (Stash Upgrades empty); 02_Data.cs:297-303 (Head has only levels 2); 02_Data.cs:333-339 (Loom only levels 2)` | NOT-IN-CODE | Searched the build tables for these names; none is present, consistent with the page. |
| 160-161 | Directions: Efficient Recycler, Sewing Machine built by Spool; Made of Tougher Stuff built by the mode | `04_Content.cs:658-691 (BuildDirections for Recyclers and Sewingmachines); Made of Tougher Stuff: not in these files` | NOT-IN-CODE | The mode file RogueWavesMode/Code/16_Directions.cs is not in the given code; Spool's files hold only the first two. |
| 168-174 | Claude's five: Double Needle -15/30/30%, rank III extra Crafted; Button Jar 55/65/75% and doubled Units; Pattern Book 1/2/3 free; Merit Badge +3% up to 15/30/45%; Odd Socks 8/15/25% within 2 tiles, rank III 10% less damage | `02_Data.cs:138-139 (NeedleCut 0.15, 0.3, 0.3; NeedleEvery 3); 02_Data.cs:148-149 (ButtonShare 0.55, 0.65, 0.75; ButtonPair 2); 02_Data.cs:157 (PatternFree 1, 2, 3); 02_Data.cs:165-166 (MeritEach 0.03, MeritCap 0.15, 0.3, 0.45); 02_Data.cs:174-175 (SocksBonus 0.08, 0.15, 0.25; SocksRange 2; SocksGuard 0.1); 08_Tin.cs:109-112 and 89-92 (uses)` | MATCH | Pattern Book rank III at once is in 08_Tin.cs:61. |
| 178-179 | Ground Thread is a sage tuft bobbing and fading in its last 2 s; pickup and drop-off fly; Stash, Loom and Heads show counters | `11_Client.cs:300-323 (AnimateTufts, left < 2 fade); 11_Client.cs:326-348 (Fly); 05_Server.cs:143-152 (Show)` | MATCH |  |
| 180-181 | Crafted pop with a stitch puff; pin marker per Tin spot; burst ring; prick spark; PATCH thread from Spool to target | `11_Client.cs:135-138 (Spool_Stitch with World_Spool_Stitch); 11_Client.cs:168-172 (Spool_Target pin line); 11_Client.cs:156-159 (Spool_Burst ring); 11_Client.cs:164-167 (Spool_Prick); 11_Client.cs:175-179 (Spool_Patch thread)` | MATCH | The pin marker is the Pin entity, 08_Tin.cs:212-227. |
| 182 | Sounds stay at or under the game's levels (docs/03) | `02_Data.cs:44-50 (SoundVolume table); 03_Assets.cs:127-133 (Play scales by these)` | MATCH | Volumes are set in the table; the comparison with docs/03 is outside the given code and is unverified. |
| 198 | Test plan: Crafted cost 10, at most 4, one every 4 s; sold Crafted refunds 5 | `02_Data.cs:276 (Units=6); 02_Data.cs:200 and 196 (Stitched Thread 10); 08_Tin.cs:25 (Slots)` | DIFFERENT | Page says at most 4 Crafted; code gives Units=6 (page says 6 in the table at line 84). |
| 201-202 | Test plan: respawn at the Stash with upgrades; Thread Head token swap; collects within 3; RETURN delivers | `05_Server.cs:450-469 (SwapTokens); 09_Head.cs:272-292 (Gone and comeback); 09_Head.cs:24-35` | MATCH |  |
| 203 | Test plan: Mangled per 40 (max 3), Overstuffed block, Burst Seams, reflect 20% / 40%, Thread per 25 damage | `10_Cushion.cs:128-149; 02_Data.cs:313-322; 10_Cushion.cs:115-117` | MATCH |  |
| 204-205 | Test plan: Loom spins every 6 s up to 10; PATCH cost 20, heal 20% capped at 500, range, cooldown | `02_Data.cs:330-332 (Spinrate 6, Hold 10); 02_Data.cs:95-96 (PatchCost 20, PatchHeal 0.2, PatchMax 500, PatchRange 4, PatchCooldown 5)` | MATCH |  |
| 212-230 | Defaults table: thread per foe, one drop per foe, merge half tile and 15 per tuft, drops only while Spool plays, Loom pickup 1.5, PATCH hotkey, Stash sold refunds nothing, Stash destroyed spills, Tin with no spot guards the Tin, Crafted keep spot, token on pad block, Head sold drops carried, Loom destroyed drops held, melee via Range 2.25, Barrage ends branch | `02_Data.cs:86-96 and 89-91; 05_Server.cs:194-222; 05_Server.cs:282; 06_HeroBrain.cs:319-322; 07_Building.cs:170-191; 08_Tin.cs:121 and 355-357; 04_Content.cs:609-618; 09_Head.cs:274-275; 07_Building.cs:299-301; 10_Cushion.cs:13 and 75; 02_Data.cs:145-146` | MATCH | Each default matches its stated value. The 1 Thread drops for an elite (+2) and merge reach are in 02_Data.cs:86-91. |

### 2.4 Uloe/DESIGN.md

| Lines | Says | Code | Mark | Note |
| --- | --- | --- | --- | --- |
| D:33 | Health 30 | `Uloe/Code/02_Data.cs:74` | MATCH | HeroStats Health=30 |
| D:35 | Speed 2.25 | `Uloe/Code/02_Data.cs:74` | MATCH | Speed=2.25 |
| D:36 | Damage 2 | `Uloe/Code/02_Data.cs:74` | MATCH | Damage=2 |
| D:37 | Range 3 | `Uloe/Code/02_Data.cs:74` | MATCH | Range=3 |
| D:38 | Projectile speed 15 | `Uloe/Code/02_Data.cs:74` | MATCH | Projspeed=15 |
| D:39 | Attack speed 0.5 | `Uloe/Code/02_Data.cs:74` | MATCH | Attackspeed=0.5 |
| D:40 | Build range 2.5 | `Uloe/Code/02_Data.cs:74` | MATCH | Buildrange=2.5 |
| D:41 | Sell rate 0.5 | `Uloe/Code/02_Data.cs:74` | MATCH | Sellrate=0.5 |
| D:34 | Health regen 0 | `Uloe/Code/02_Data.cs:72-75` | NOT-IN-CODE | searched HeroStats and BuildHero for a regen stat; none set, so the template value stands |
| D:43-44 | Special rule Bright Idea and tooltip text | `Uloe/Code/02_Data.cs:52-53` | MATCH | TooltipName and TooltipDesc are the same text |
| D:52 | Keys 6 and 7, Onpressed, priority 6 | `Uloe/Code/04_Content.cs:394-396` | MATCH | ability keybind 6,7, Onpressed, priority 6 |
| D:55 | Plug range 5 tiles | `Uloe/Code/02_Data.cs:96` | MATCH | PlugRange=5 |
| D:57 | Plugged radius 4 | `Uloe/Code/02_Data.cs:96` | MATCH | PlugRadius=4 |
| D:58 | Node light 2 + 0.4 x aura, max 10 | `Uloe/Code/06_HeroBrain.cs:157-163` | MATCH | NodeBase 2, NodePerAura 0.4, NodeMax 10 in 02_Data.cs:97 |
| D:59 | Power Bank light 4 + 2 per upgrade | `Uloe/Code/06_HeroBrain.cs:157-163` | MATCH | BankBase 4, BankPerUpgrade 2 in 02_Data.cs:98 |
| D:60 | Cooldown 0.5 s from an unplug, button greys | `Uloe/Code/02_Data.cs:96; 06_HeroBrain.cs:330-383` | MATCH | PlugCooldown 0.5 set in Unplug, SetButton greys Off |
| D:61-63 | Bulb skin, cord sags and follows every frame, ring at real radius | `Uloe/Code/06_HeroBrain.cs:317; Uloe/Code/11_Client.cs:316-336` | MATCH | Plugged skin, DrawLinks redraws cord and ring each frame |
| D:55-56 | Walking past the range snaps the cord with a spark and owner-only line | `Uloe/Code/06_HeroBrain.cs:352-362` | MATCH | CheckCord calls Unplug(true) with WarnSnapped |
| D:69 | Light is a stat, 1.5 s timed Add re-added each second | `Uloe/Code/05_Server.cs:30-56; Uloe/Code/02_Data.cs:80` | MATCH | Feed removes old Add and adds new with LightHold 1.5 each LightTick 1 |
| D:72 | Teammate Light cap 4 | `Uloe/Code/02_Data.cs:82; Uloe/Code/05_Server.cs:43-45` | MATCH | TeamLightCap 4 applied only when by differs from owner |
| D:78 | Uloe Light 1, radius 3 | `Uloe/Code/02_Data.cs:83` | MATCH | HeroLight 1, HeroRadius 3 |
| D:80 | Photon Light 1, radius 2 | `Uloe/Code/02_Data.cs:84` | MATCH | PhotonLight 1, PhotonRadius 2 |
| D:81 | Aimed Flood Light 2 + 0.2 x aura, bonus at most 3, aim reach 6 | `Uloe/Code/07_Building.cs:197-205; Uloe/Code/02_Data.cs:285,313` | MATCH | Output = Lightout 2 + min(3, 0.2 x aura); Aimrange 6 |
| D:82 | Wide Flood 60% of that, radius 3.5 | `Uloe/Code/02_Data.cs:288; Uloe/Code/07_Building.cs:381-385` | MATCH | Wide_Base 0.6, Wideradius_Base 3.5 |
| D:83 | Long Throw 80%, 8 tiles, 0.6 each side | `Uloe/Code/02_Data.cs:290; Uloe/Code/07_Building.cs:401-412` | MATCH | Line_Base 0.8, Linereach 8, Linewidth 0.6 |
| D:84 | Bounce Card 50% radius 3, never to another Bounce Card | `Uloe/Code/02_Data.cs:267; Uloe/Code/10_Mirror.cs:68; Uloe/Code/05_Server.cs:213-215` | MATCH | Relay_Base 0.5, radius 3, skipRelays excludes Bounce mirrors |
| D:85 | Bright Spark Photon light 2, radius 2.5 | `Uloe/Code/02_Data.cs:240; Uloe/Code/08_Bank.cs:160-164` | MATCH | Photonlight_Base 2, Photonradius_Base 2.5 |
| D:86 | SURGE Photon light 3 for 15 s | `Uloe/Code/02_Data.cs:248; Uloe/Code/08_Bank.cs:70-74,160` | MATCH | Novalight_Base 3, Novatime_Base 15 |
| D:87 | Stadium Lights 100% radius 5 | `Uloe/Code/02_Data.cs:302; Uloe/Code/07_Building.cs:381-385` | MATCH | Stadium_Base 1, Stadiumradius_Base 5 |
| D:88 | Lighthouse 60%, held 6 s, 9 tiles, turn 6 s | `Uloe/Code/02_Data.cs:305; Uloe/Code/07_Building.cs:480-491` | MATCH | Lighthouse_Base 0.6, household 6, reach 9, turn 6 |
| D:90-91 | Light counted up to 12 | `Uloe/Code/02_Data.cs:81; Uloe/Code/07_Building.cs:33` | MATCH | LightCap 12 clamps Light |
| D:95 | Magnifying Glass +20% per Light, +40% with Polished Lens | `Uloe/Code/02_Data.cs:89,211; Uloe/Code/09_Glass.cs:18-19` | MATCH | GlassPerLight 0.2; Perlight_Base 0.4 |
| D:96 | Power Bank Photons back 10% faster per Light | `Uloe/Code/02_Data.cs:90; Uloe/Code/08_Bank.cs:23-26` | MATCH | BankPerLight 0.1 divides Respawn |
| D:97 | Mirror level 1 at Light 1+, level 2 at 3+, level 3 at 6+ | `Uloe/Code/10_Mirror.cs:25-32; Uloe/Code/02_Data.cs:92` | DIFFERENT | code: any Light above 0 gives level 1 (l <= 0 gives 0); page says Light 1 and up |
| D:98 | Flood Light mines 10% faster per Light | `Uloe/Code/02_Data.cs:91; Uloe/Code/07_Building.cs:317` | MATCH | FloodPerLight 0.1 multiplies attack speed |
| D:100 | Skin at Light above 0, 4, 8 | `Uloe/Code/02_Data.cs:86; Uloe/Code/07_Building.cs:135-147` | MATCH | LookBands 0,4,8 select Lit1..3 |
| D:110-112 | Blind and Illuminate per level: 15/30/45 percent, 3/4/5 s | `Uloe/Code/02_Data.cs:103-105` | MATCH | BlindMulti, LitMulti, MarkSeconds |
| D:116 | Photon hit Blind 1, Illuminate 1 | `Uloe/Code/02_Data.cs:83,149; Uloe/Code/08_Bank.cs:227-238` | MATCH | PhotonMark 1 used in OnDealt |
| D:117 | Short Circuit burst Blind 2, Illuminate 2 | `Uloe/Code/02_Data.cs:149; Uloe/Code/08_Bank.cs:394` | MATCH | BurstMark 2 |
| D:122 | Static Charge Photons mark at 2 | `Uloe/Code/02_Data.cs:238; Uloe/Code/08_Bank.cs:165` | MATCH | Staticmark_Base 2 read by MarkLevel |
| D:123 | SURGE marks 3 and 3 | `Uloe/Code/02_Data.cs:164; Uloe/Code/08_Bank.cs:78` | MATCH | NovaMark 3 |
| D:124 | Sunspot Illuminate 1 from Light 4, 2 from 8, 3 at 12 | `Uloe/Code/02_Data.cs:156; Uloe/Code/09_Glass.cs:24-35` | MATCH | SunspotLight 4, 8, 12 |
| D:125 | Death Ray full heat Illuminate 3 | `Uloe/Code/02_Data.cs:164; Uloe/Code/09_Glass.cs:106-107` | MATCH | DeathMark 3 |
| D:126 | Hall of Mirrors mark on every foe within 2 tiles of attacker | `Uloe/Code/02_Data.cs:149-150 region; Uloe/Code/10_Mirror.cs:148-155` | MATCH | Hall_Base 2 read in Hall branch |
| D:127 | Heliostat beams nearest foe within 4, 1.5 x Light, its mark | `Uloe/Code/10_Mirror.cs:78-92` | MATCH | Heliorange 4, Heliostat_Base 1.5, Mark at its level |
| D:128 | Searchlight Illuminate 1, 2 at Light out 5+ | `Uloe/Code/02_Data.cs:164; Uloe/Code/07_Building.cs:442` | MATCH | SearchLit 1, SearchLitBright 2 at Searchbright 5 |
| D:129 | Lighthouse marks foes it crosses Blind 2 and Illuminate 2 | `Uloe/Code/02_Data.cs:164; Uloe/Code/07_Building.cs:508-509` | MATCH | LighthouseMark 2 |
| D:131-132 | One level of each held; stronger replaces weaker; refresh in last second | `Uloe/Code/05_Server.cs:386-394` | MATCH | Takes() implements replace, refuse and refresh rules |
| D:134-135 | Blind as timed Multi Addmod on Damage; Illuminate a front hit hook | `Uloe/Code/05_Server.cs:397-433` | MATCH | Addmod Multi on Damage; Front(hook) for Illuminate |
| D:145 | Magnifying Glass Health 30 +20, Damage 2 +0.5, Attackspeed 0.75, Range 4.5, cost Red 30 | `Uloe/Code/02_Data.cs:201-204,202` | MATCH | Stats and Costs strings match |
| D:146 | Power Bank Health 60 +25, Units 2, Respawn 8, cost Red 40 + Yellow 20 | `Uloe/Code/02_Data.cs:228-231` | MATCH | Stats and Costs match |
| D:147 | Mirror Health 100 +40, cost Blue 20 | `Uloe/Code/02_Data.cs:255-258` | MATCH | Stats and Costs match |
| D:148 | Flood Light Health 40 +20, Damage 1 at 0.3, Range 2.5, aim 6, Light out 2, cost Yellow 40 | `Uloe/Code/02_Data.cs:282-285` | MATCH | Stats and Costs match |
| D:150-152 | In-game lines for the four buildings | `Uloe/Code/02_Data.cs:201,228,255,282` | MATCH | Desc strings match |
| D:161 | Mirror blinds any foe whose hit lands, melee or ranged | `Uloe/Code/10_Mirror.cs:94-108` | MATCH | OnHit has no range check |
| D:163-166 | Flood Light 2.5 node reach, pays like pick, no node says once, AIM 6 tiles, nearest default | `Uloe/Code/07_Building.cs:283-311,364-371` | MATCH | FindNode, Tell WarnNoNode once, CurrentAim nearest owner building |
| D:172 | Photon melee via stock Mob_Combat | `Uloe/Code/04_Content.cs:484` | MATCH | Entity_Ai_Mob_Combat in Aiscripts |
| D:173-177 | Photon Health 8, Damage 0.5, Range 1.5, Attack speed 0.6, Speed 1.75 | `Uloe/Code/02_Data.cs:141` | MATCH | PhotonStats |
| D:178 | 2 Photons per Power Bank | `Uloe/Code/02_Data.cs:231` | MATCH | Units=2 |
| D:179 | Respawn 8 s | `Uloe/Code/02_Data.cs:231` | MATCH | Respawn=8 |
| D:181 | Leash 3.5 tiles | `Uloe/Code/02_Data.cs:147` | MATCH | PhotonLeash 3.5 |
| D:186-189 | Wander every 1.5 to 4 s, half visits, 1.25 path grab, 2.5 fight | `Uloe/Code/02_Data.cs:147-148; Uloe/Code/08_Bank.cs:299-365` | MATCH | WanderMin 1.5, WanderMax 4, PhotonVisit 0.5, PathGrab 1.25, PhotonEngage 2.5 |
| D:190 | Photons not selectable or sellable; go with Power Bank | `Uloe/Code/04_Content.cs:495-498` | MATCH | Sellable false, Destroyonparent true |
| D:202 | Burning Point +12%, max +120%, Red 35 | `Uloe/Code/02_Data.cs:207` | MATCH | Ramp_Base 0.12, Rampmax_Base 1.2, Redaura=35 |
| D:203 | Wide Lens Range +1.5, Red 30 | `Uloe/Code/02_Data.cs:209` | MATCH | Range_Add=1.5, Redaura=30 |
| D:204 | Polished Lens +40% per Light, Red 90 | `Uloe/Code/02_Data.cs:211` | MATCH | Perlight_Base 0.4, Redaura=90 |
| D:205 | Quick Focus attack speed +50%, Red 80 | `Uloe/Code/02_Data.cs:213` | MATCH | Attackspeed_Pmulti=0.5, Redaura=80 |
| D:206 | Prism 2 beams within 1.5 at 50%, Red 100 | `Uloe/Code/02_Data.cs:215; Uloe/Code/09_Glass.cs:161-184` | MATCH | Prism_Base 2, Prismradius 1.5, Prismshare 0.5 |
| D:207 | Sunspot Red 300 and Blue 50 | `Uloe/Code/02_Data.cs:217` | MATCH | Costs Redaura=300;Blueaura=50 |
| D:208 | Focal Point within 0.5 tiles, full damage, Red 350 | `Uloe/Code/02_Data.cs:219; Uloe/Code/09_Glass.cs:136-149` | MATCH | FocalWidth 0.5; Redaura=350 |
| D:209 | Death Ray +12%, max +250%, idle 3 s, Red 3000 | `Uloe/Code/02_Data.cs:221; Uloe/Code/09_Glass.cs:58,77` | MATCH | Deathramp 0.12, Deathmax 2.5, Deathidle 3 |
| D:210 | Short Circuit 3 damage within 1.5, Red 40 | `Uloe/Code/02_Data.cs:234; Uloe/Code/08_Bank.cs:394` | MATCH | Burstdamage 3, Burstradius 1.5 |
| D:211 | Extra Cell Units +1, Yellow 35 | `Uloe/Code/02_Data.cs:236` | MATCH | Units_Add=1, Yellowaura=35 |
| D:212 | Static Charge Photon Damage +100%, Red 90 | `Uloe/Code/02_Data.cs:238; Uloe/Code/08_Bank.cs:205` | MATCH | Static_Base 1 held as Damage Multi |
| D:213 | Bright Spark Yellow 90 | `Uloe/Code/02_Data.cs:240` | MATCH | Yellowaura=90 |
| D:214 | Surge Protector 40% less damage, heal 2 a second, Yellow 60 and Blue 40 | `Uloe/Code/02_Data.cs:242; Uloe/Code/06_HeroBrain.cs:190-234` | MATCH | Surge_Base 0.4, Surgeheal_Base 2, costs match |
| D:215 | Chain Lightning 2 foes within 2, Red 350 | `Uloe/Code/02_Data.cs:244; Uloe/Code/08_Bank.cs:251-278` | MATCH | Chain_Base 2, Chainradius_Base 2 |
| D:216 | Photon Swarm Units +2, leash 5, Red 150 and Yellow 200 | `Uloe/Code/02_Data.cs:246` | MATCH | Units_Add=2, Leash_Base=5 |
| D:217 | Supernova 20 damage within 2, Blind 3, Illuminate 3, fills slots, Red 2000 and Yellow 1000, SURGE once a Tide | `Uloe/Code/02_Data.cs:248; Uloe/Code/08_Bank.cs:45-84` | MATCH | Supernova_Base 20, Novaradius 2; SurgeReady uses NovaWave against Wave; slots filled |
| D:218 | Silver Backing Blind one level stronger, at most 3, Blue 30 | `Uloe/Code/02_Data.cs:261; Uloe/Code/10_Mirror.cs:37-40` | MATCH | Blindup_Base 1, clamped to 3 levels |
| D:219 | Vanity Lights Illuminate in place of Blind, Blue 30 | `Uloe/Code/02_Data.cs:263; Uloe/Code/10_Mirror.cs:42-48` | MATCH | Illuminate_Base key; Mark chooses Illuminate under Vanity |
| D:220 | Glare chance N x 20%, Blue 100 | `Uloe/Code/02_Data.cs:265; Uloe/Code/10_Mirror.cs:112-125` | MATCH | Nullify_Base 0.2 times blind level |
| D:221 | Bounce Card 50% within 3, Blue 100 | `Uloe/Code/02_Data.cs:267` | MATCH | Relay_Base 0.5, Relayradius 3, Blue 100 |
| D:222 | Tempered Glass Health +200%, 20% less damage, Blue 90 | `Uloe/Code/02_Data.cs:269; Uloe/Code/10_Mirror.cs:128-134` | MATCH | Health_Pmulti=2, Tempered_Base 0.2 |
| D:223 | Hall of Mirrors within 2, Blue 350 | `Uloe/Code/02_Data.cs:271` | MATCH | Hall_Base=2, Blue 350 |
| D:224 | Reflection 30%, 60% from Light 6, Blue 300 and Red 100 | `Uloe/Code/02_Data.cs:273; Uloe/Code/10_Mirror.cs:50,135-142` | MATCH | Reflect 0.3, Reflectlit 0.6 at MirrorLevel3 6 |
| D:225 | Heliostat 1.5 x Light, range 4, Blue 2500 and Red 1000 | `Uloe/Code/02_Data.cs:275` | MATCH | Heliostat_Base 1.5, Heliorange_Base 4 |
| D:226 | Wide Flood 60%, radius 3.5, cannot be aimed, Yellow 30 | `Uloe/Code/02_Data.cs:288; Uloe/Code/07_Building.cs:191` | MATCH | CanAim false for Wide without Stadium |
| D:227 | Long Throw 80%, 8 tiles, Yellow 30 | `Uloe/Code/02_Data.cs:290` | MATCH | Line_Base 0.8, Linereach 8 |
| D:228 | Halogen Bulb Light out +2, Yellow 90 | `Uloe/Code/02_Data.cs:292` | MATCH | Lightout_Add=2 |
| D:229 | High Voltage attack speed +60%, Yellow 100 | `Uloe/Code/02_Data.cs:294` | MATCH | Attackspeed_Pmulti=0.6 |
| D:230 | Colour Filter 70% of share, COLOUR on key 4, Yellow 250 | `Uloe/Code/02_Data.cs:296; Uloe/Code/04_Content.cs:420; Uloe/Code/07_Building.cs:349-361` | MATCH | Filter_Base 0.7, Colour.Keybind 4 |
| D:231 | Searchlight Illuminate 1, 2 at Light out 5, slow 20%, Yellow 250 and Red 100 | `Uloe/Code/02_Data.cs:298-299; Uloe/Code/07_Building.cs:417-475` | MATCH | Searchslow 0.2, Searchradius 1.5, Searchbright 5 |
| D:232 | Stadium Lights 100% within 5, attack speed x2, Yellow 3000 | `Uloe/Code/02_Data.cs:302` | MATCH | Attackspeed_Multi=1 applied as a Multi modifier on Attackspeed (doubles it) |
| D:233 | Lighthouse 9 tiles, full turn 6 s, Yellow 2500 and Red 500, AIM goes away | `Uloe/Code/02_Data.cs:305-306; Uloe/Code/07_Building.cs:191` | MATCH | CanAim false for Lighthouse |
| D:237-238 | Prism and Focal extra hits via HurtWithStat, stock beam is look only | `Uloe/Code/09_Glass.cs:92,96,115-132` | MATCH | Beam(foe, !focal) uses Interact 0 with Focal |
| D:239-240 | Static Charge held Multi; Chain jumps come from the Bank | `Uloe/Code/08_Bank.cs:205,251-278` | MATCH | Keep Damage Multi; Chain uses Home.Master |
| D:241 | Surge Protector is hit hook on the vessel while cord in that Bank | `Uloe/Code/06_HeroBrain.cs:190-234` | MATCH | CheckSurge and OnSurgeHit |
| D:242 | SURGE holds Off until Wave rises; Photons stay standing | `Uloe/Code/08_Bank.cs:45-54,70-84` | MATCH | SyncSurge Off state; Burst does not despawn photons |
| D:244-246 | Colour Filter traded for owner from strike payout, split and gain handled | `Uloe/Code/07_Building.cs:315-361` | MATCH | Purse read around Interact, Give negative and back at 70% |
| D:247-248 | Searchlight slow one untimed Speed Multi per foe per Flood Light | `Uloe/Code/07_Building.cs:444-474` | MATCH | SlowOn once, Unmod when foe leaves |
| D:249-250 | Lighthouse counts swept wedge five times a second, lit at most once a second | `Uloe/Code/07_Building.cs:480-523` | MATCH | LighthouseStep 0.2, crossed dictionary with 1 s rule |
| D:262-264 | Extension Cord reach +2, +4, +6 tiles | `Uloe/Code/02_Data.cs:110` | MATCH | ExtensionReach 0, 2, 4, 6 |
| D:272-274 | Brighter Bulb Light +1, +2, +3, plugged or not | `Uloe/Code/02_Data.cs:111; Uloe/Code/06_HeroBrain.cs:169` | MATCH | BrightLight 0,1,2,3 added in every Feed |
| D:282-284 | Spare Batteries: +1 Photon, then respawn 0.75, then +2 Photons | `Uloe/Code/02_Data.cs:112-113` | MATCH | BatteryUnits 0,1,1,2; BatteryRespawn 1,1,0.75,0.75 |
| D:292-294 | Flash Bulb Illuminate and Blind ranks | `Uloe/Code/02_Data.cs:114-115; Uloe/Code/06_HeroBrain.cs:126-140` | MATCH | FlashLit 0,1,1,2; FlashBlind 0,0,1,2 |
| D:76 | Hero Lore placeholder line | `Uloe/Code/02_Data.cs:55` | MATCH | Lore text matches |
| D:22-23 | Accent glint yellow F1EF72 | `Uloe/Code/04_Content.cs:18` | MATCH | Accent Color(0.945, 0.937, 0.447) equals F1EF72 |

### 2.5 Yossele/DESIGN.md

| Lines | Says | Code | Mark | Note |
| --- | --- | --- | --- | --- |
| D:13 | Name Yossele, title The Patient Golem, difficulty Intermediate | `02_Data.cs:67-69` | MATCH | Displayname, Displaysub and Displayskill constants hold these values. |
| D:22 | Accent is leaf green 72CC35 | `04_Content.cs:24` | MATCH | Accent is 0x72,0xCC,0x35. |
| D:24 | Each Growth stage is a skin that adds sprouts round the tower foot | `04_Content.cs:179-187` | MATCH | ModKitUtil.AddSkin per stage with Yossele_Growth skins; no motion is added. |
| D:25 | Lore is a placeholder in the journal | `02_Data.cs:74` | MATCH | Lore constant is a placeholder line passed to the journal. |
| D:31 | Attack is a ranged green laser bolt via Entity_Ai_Mob_Ranged | `06_HeroBrain.cs:85-95; 04_Content.cs:309-337` | MATCH | Arm sets the Attack command Goalstring to the laser; BuildLaser clones the Snoball projectile. |
| D:32 | Health 70 | `02_Data.cs:85` | MATCH | HeroStats Health=70. |
| D:33-39 | Speed 1.75, Damage 5.5, Range 4, Projspeed 15, Attackspeed 0.5, Buildrange 2.5, Sellrate 0.5 | `02_Data.cs:85` | MATCH | HeroStats holds every value in this row. |
| D:40 | Regen 1 health a second, only while hurt | `02_Data.cs:87; 06_HeroBrain.cs:98-109` | MATCH | Regen constant is 1; the brain heals only when missing health. |
| D:42 | Display_Desc Sturdy, single-target ranged that mends itself | `02_Data.cs:70` | MATCH | HeroDesc holds this text. |
| D:43-44 | Special rule Patient Growth tooltip text | `02_Data.cs:71-72; 04_Content.cs:682-704` | MATCH | TooltipName and TooltipDesc hold this text; Tooltip() builds the tier -3 upgrade. |
| D:45-52 | Laser: Snoball clone, green bolt look, Archer end look, hit tagged Yosselelaser, Shoot Vis plays zap | `04_Content.cs:309-337; 06_HeroBrain.cs:88-93; 13_Client.cs:88-93` | MATCH | Clone, model, end look, Hurtdict tag and Shoot-to-zap are all in code. The fill-in of Damage is in the game (Helper_Entitytools.cs:166), not supplied. |
| D:53-57 | Growth per Tide: stands a whole Tide before, first grows at Tide N+2, 1 stage a Tide, 5 on maps of 4 Tides or fewer, maxima 5/15/8/5, destroyed means gone | `07_Building.cs:95-105; 05_Server.cs:170-174; 02_Data.cs:93-94; 02_Data.cs:289,306,324,342; 05_Server.cs:245-247` | MATCH | TideBegan checks BuiltWave <= wave-2; GrowthPerTide returns 5 when tides are 4 or fewer; a fresh building starts at 0. |
| D:56 | Map Tide count is the mode's Totalwaves | `05_Server.cs:156-164` | MATCH | TidesOnMap reads Game_Mapvars.Mode.Enemies.Totalwaves. The cited Entity_Ai_Artifact_Coin.cs is not supplied. |
| D:58-61 | FERTILIZE on keys 6/7, picks own building, 1 fertilizer for +1 Growth, refused at max, hotkey fallback within 6 tiles, 0.5 s guard, no button until a Direction grants it | `04_Content.cs:367; 02_Data.cs:97-98; 06_HeroBrain.cs:159-173,205-258` | MATCH | Keybind is 6 and 7; reach is 6; guard is 0.5; the command exists only when Rank is above 0. |
| D:62-63 | Fertilizer per player, cap 10, kept through deaths, reset each map | `02_Data.cs:96; 05_Server.cs:89-98,145-153` | MATCH | FertilizerMax is 10; OnMapStart zeroes it; Player records persist across deaths. |
| D:64 | Compost Bin: three rank Directions, granted only by the mode or self-test | `02_Data.cs:101-110; 04_Content.cs:637-643` | MATCH | Three Compostbins are built as Directions; the grant comes from the mode (ModKitDirections), not in code here. |
| D:70 | Mushroom Cannon: Red 15, Health 20+25, Damage 1+0.25, Attackspeed 0.4, Range 9, Radius 1, Spores 2; spores 0.3 a second, tick every 0.5 s, fall off after 6 s, max 10 | `02_Data.cs:200-202,289,291; 08_Cannon.cs:97-100,203-204` | MATCH | Costs, stats, SporeDps 0.3, SporeTick 0.5, SporeFade 6 and SporeMax 10 all match. |
| D:71 | Dandelion Tree: Red 50, Health 50+25, Damage 2+0.25, Range 7, Seedlimit 10, Seedrate 0.5; limit 1; a seed every 2 s; full at each Tide start; all ready seeds strike nearest first | `02_Data.cs:306,308; 09_Dandelion.cs:73-80,107-114,143-166; 05_Server.cs:335-342` | MATCH | Seedrate 0.5 gives one seed every 2 s; OnTide sets Ready to Limit; Strike shares seeds nearest first. |
| D:72 | Stonehenge: Blue 25, Health 80+40, Slow 0.15, orientable, vines 1 tile and +1 every 2 Growth stages, forward over building tiles, foes on vines slowed 15% | `02_Data.cs:324,326; 10_Henge.cs:29,58-73,131-160; 10_Henge.cs:386; 04_Content.cs:426-432` | MATCH | Length is 1 + Growth/2; Covers includes building tiles; slow is Stat Slow 0.15; Orientable is set. |
| D:73 | Pachira Sapling: Yellow 35, Health 30+20, Damage 1, Attackspeed 0.25, Range 2.5; strikes every ore node in range; drops aura fruit when fully grown | `02_Data.cs:342,344; 11_Pachira.cs:48-55,190-199` | MATCH | Costs and stats match; every node in Ores() is struck; NodeEmptied drops fruit when FullGrown. |
| D:74 | Bonsai: Red 50, 40% of Tree, Health 20+10, Damage 0.8+0.1, Range 2.8, Seedlimit 4, Seedrate 0.2, limit 1, sells for nothing | `02_Data.cs:230,364-367; 09_Dandelion.cs:435-449; 05_Server.cs:325-331` | MATCH | Costs and stats match; BonsaiShare 0.4; sell values zeroed on start; HasBonsai enforces the limit. |
| D:80 | Cannon Growth: attack speed +10% and radius +0.1 a stage; full Growth bounces once at half radius and damage with 1 stack | `02_Data.cs:205-210; 08_Cannon.cs:56-61,82-86` | MATCH | CannonGrowSpeed 0.1, CannonGrowRadius 0.1, bounce delay 1, share 0.5, 1 spore. |
| D:81 | Tree Growth: seed limit +3 and range +20% of base a stage | `02_Data.cs:223-224; 09_Dandelion.cs:349-358` | MATCH | SeedsPerStage 3 and RangePerStage 0.2 are applied as limit and range modifiers. |
| D:82 | Stonehenge Growth: damage taken -10% a stage; vines +1 tile every 2 stages | `02_Data.cs:238; 10_Henge.cs:29,116-121` | MATCH | HengeArmorPerStage 0.1 feeds Armor; Length adds 1 per 2 Growth. |
| D:83 | Pachira Growth: mining speed +10% and range +0.4 a stage; full Growth gives aura fruit | `02_Data.cs:247; 11_Pachira.cs:62-73,138-142` | MATCH | PachiraGrowSpeed 0.1 and PachiraGrowRange 0.4 are applied per stage. |
| D:84 | Bonsai Growth: seed limit +1.2 (40% of 3), range +20% of base | `02_Data.cs:223,230; 09_Dandelion.cs:441,349-358` | MATCH | BonsaiBrain StageSeeds is 3 times 0.4, which gives 1.2. |
| D:86-87 | Aura fruit worth Attackspeed x Damage x 15 of each colour; 0.375 x 1 x 15 paid as 6; pickup within 0.75 tiles | `11_Pachira.cs:172-186; 02_Data.cs:248-249; 11_Pachira.cs:110-111; 12_Hooks.cs:? ; 11_Pachira.cs:186-188; 11_Pachira.cs:? ; 02_Data.cs:248-249; 11_Pachira.cs:188` | MATCH | FruitEach uses FruitScale 15; Num rounds 5.625 to 6; FruitReach is 0.75 (YosseleFruit.Picker in 11_Pachira.cs:428-436). |
| D:88-93 | Fixed 2026-10-23: a node refills and pays on the strike that empties it; fruit drops on the free tile beside the node nearest the Sapling | `11_Pachira.cs:107-136,191-224` | MATCH | Paid() detects a payout by refill or emptying; DropTile picks the nearest free neighbour tile. |
| D:94-95 | Vines are a flat texture on networked ground beings cloned from Sno Sno's snow; forwards if possible | `04_Content.cs:509-558; 10_Henge.cs:149-160` | MATCH | BuildGround clones Effect_Snosnosnow with no tag or AI; Step stops at gaps and cliffs. |
| D:97-98 | Motion: cannon cap aims at each lob; seeds float round the crown; Stonehenge faces its placement; Sapling still | `13_Client.cs:289-299,393-426; 10_Henge.cs:58-68` | MATCH | Aim turns the Turret part; Orbit floats seeds; the Henge reads its orientation; the Sapling has no motion code. |
| D:105-106 | Four sheet levels map to tiers 2 to 5 | `02_Data.cs:282-283; 04_Content.cs:479-480` | MATCH | Tier 2 roots are registered; tiers run 2 to 5. |
| D:109 | Cannon tree: Sticky2/Farout2 to Viral3/Deathcap3 to Finish4/Hallucinogen4 to Cordyceps5 | `02_Data.cs:294-300` | MATCH | Next lists match exactly. |
| D:110-111 | Tree: Packets2 to Picky3/Weeds3; Demand2 to D-MDS3/Picky3/Weeds3 | `02_Data.cs:311-312` | MATCH | Next lists match. |
| D:112 | Any level 2 leads to Bonsai4 and Rubbery4 | `02_Data.cs:311-315` | DIFFERENT | Only the tier-3 nodes Picky, Weeds and D-MDS list Bonsai4 and Rubbery4; the level-2 nodes Packets and Demand do not. |
| D:113 | Stonehenge tree: Verdant2/Blockade2 to Remedy3/Carnivore3/Easy3 to Roots4/Obsidian4 to Monument5 | `02_Data.cs:329-336` | MATCH | Next lists match. |
| D:114 | Pachira tree: Focused2/Sustaining2 to Grafted3/Forest3/Mulch3 to Connected4/Harvest4/Web4 to Fortifying5/Softening5 | `02_Data.cs:347-356` | MATCH | Next lists match. |
| D:119-120 | Cannon Sticky Slow 0.25 (Red 50); Farout Range_Add 2, Farout_Per 0.5, Red 45 Blue 10 | `02_Data.cs:294-295` | MATCH | Params and costs match; FaroutMax 0.75 caps the bonus in 08_Cannon.cs:209. |
| D:121-124 | Viral 30% 15 damage 2 tiles 1 stack Red 90; Deathcap +25% and max 8 Red 100; Finish 1 per stack within 1.25, 2 to others, 1 s guard Red 420; Hallucinogen every 5 s 20% stun 2 s Red 360 Blue 280 | `02_Data.cs:296-299; 08_Cannon.cs:287-298,246-270,215-222` | MATCH | Params and costs match; Viral, Finish and stun logic match. |
| D:125 | Cordyceps 20% plus 5% a stack; Red 900 Blue 1250 | `02_Data.cs:300; 08_Cannon.cs:91-92` | MATCH | Cordy_Chance 0.2 plus Cordy_Per 0.05 per stack; costs match. |
| D:126-127 | Seed Packets 2 back, Damage +3, Red 40; Demand twice as fast under a quarter, Red 20 Yellow 40 | `02_Data.cs:311-312; 09_Dandelion.cs:55-58,328-335` | MATCH | Packets_Refund 2 and Damage_Add 3; Demand_Below 0.25 doubles the rate. |
| D:128-129 | Picky: toughest first and 0.25% of max health; Garden Weeds 40%, 3 s window, 15 s, one per stage, Red 90 Yellow 30 | `02_Data.cs:313-314; 09_Dandelion.cs:125-131,142-143,335` | MATCH | Picky_Percent 0.0025 and max-health sort; Weeds window, chance, duration and the Growth cap match. |
| D:130 | D-MDS intercepts projectiles, 1 seed each, 5 for a boss, seed growth -50%, Blue 150 Yellow 40 | `02_Data.cs:315; 09_Dandelion.cs:263-304` | MATCH | Seedrate_Multi -0.5, Dmds_Boss 5, and boss cost are as written. |
| D:131-133 | Bonsai4 costs Red 300 Blue 300 Yellow 300; Rubbery Red 450 Blue 600 with 2 bounces at 2 tiles; Storm Range +100%, seeds +20, growth +100% | `02_Data.cs:316-318; 09_Dandelion.cs:199-226` | MATCH | Costs and params match; bounce logic matches. |
| D:134-136 | Verdant 2 steps, 3 at full; Blockade +50% health and 30% barrier; Remedy 0.5 a stage to buildings on vines | `02_Data.cs:329-331; 10_Henge.cs:35-37,105-114,269-292` | MATCH | Verdant_Reach 2 and Verdant_Full 3; Blockade_Share 0.3; Remedy heal scales with Growth. |
| D:137-139 | Carnivore 5 a second from up to 5 foes; Easy Target +15% after 5 s; Roots 1.5 s stun and 5% health after 7 s, once per foe | `02_Data.cs:332-334; 10_Henge.cs:217-252` | MATCH | Carnivore_Drain 5 and Carnivore_Max 5; Easy_After 5 and Easy_Bonus 0.15; Roots_After 7, stun 1.5, percent 0.05, once per foe. |
| D:140-141 | Obsidian max health +200%; Monument 20% less damage and debuffs stripped | `02_Data.cs:335-336; 10_Henge.cs:116-121; 12_Hooks.cs:214-240` | MATCH | Health_Multi 2 and Monument_Armor 0.2; Cleanse strips Override and negative multipliers. |
| D:142-143 | Focused x1.8 on nearest node only, Yellow 30; Sustaining 0.5 a second per node per stage, Yellow 25 Blue 10 | `02_Data.cs:347-348; 11_Pachira.cs:52-53,34-47` | MATCH | Focused_Multi 0.8 gives 1.8; GetRange limits to one node; heal per second matches. |
| D:144 | Grafted: COLOUR button, one colour pays 70% of the fruit total, Red 60 Blue 60 Yellow 60 | `02_Data.cs:349; 11_Pachira.cs:177-186; 04_Content.cs:374-386` | MATCH | Grafted_Efficiency 0.7 applies to one colour; the COLOUR command is built. |
| D:145-146 | Forest +10% per Sapling within 4 tiles with no cap, Yellow 140; Mulch +20% for 5 s, no stacking, Red 40 Yellow 30 | `02_Data.cs:350-351; 11_Pachira.cs:62-93` | MATCH | Forest_Per 0.1 and Forest_Range 4 with no cap; Mulch_Bonus 0.2 and Mulch_Last 5 with refresh. |
| D:147 | Connected 65% of hits on owner's buildings in range above 50% health, other Saplings out, Yellow 120 Blue 300 | `02_Data.cs:352; 12_Hooks.cs:270-283,380-390` | MATCH | Connected_Share 0.65 and Connected_Above 0.5; other Saplings are excluded. |
| D:148 | Harvesting Season fruit +100%, Yellow 400 | `02_Data.cs:353; 11_Pachira.cs:174` | MATCH | Harvest_Multi 1 doubles FruitEach. |
| D:149 | Wood Wide Web once a second, 2 most for 1 least while gap is 100 or more, Yellow 250 | `02_Data.cs:354; 11_Pachira.cs:303-361` | MATCH | Web_Gap 100, Web_Take 2, Web_Give 1; trade is once a second per player. |
| D:150 | Fortifying max health +100%, 30% less damage for team buildings in range, Blue 2300 Yellow 1000 | `02_Data.cs:355; 12_Hooks.cs:289-295,391-399` | MATCH | Health_Multi 1 and Fortify_Armor 0.3; aura applied to team buildings in range. |
| D:151 | Softening one hit fewer, never fewer than 1, Yellow 2300 Red 1000 | `02_Data.cs:356; 11_Pachira.cs:242-296` | MATCH | Soften_Points 1 with the minimum of 1 applied. |
| D:155-158 | D-MDS: projectile pools, shooter tag Enemy, Hurt type, Kill(false) | `09_Dandelion.cs:234-261,306-313` | MATCH | Threats reads Game_Entity.Projectiles, checks Enemy tag and Hurt, and calls Kill(false). Helper_Damagetools.cs is not supplied. |
| D:163-165 | Bosses cost 5 seeds a projectile; boss means base health 600 or more | `02_Data.cs:229; 09_Dandelion.cs:263-269` | MATCH | BossHealth 600 and CostFor returns Dmds_Boss (5). |
| D:166-173 | Cordyfriend: 20% plus 5% a stack, Health 75, Damage 2, Range 1.5, Attackspeed 0.5, Speed 2.25, mob-blocker block, own sheet, tags, walks to foes within 4 tiles and 5 of rise point | `08_Cannon.cs:296; 02_Data.cs:215,217-218; 04_Content.cs:575-579,592; 16_Friend.cs:157-175` | MATCH | Stats, chance, block, tags, engage range and leash all match. |
| D:174-179 | Monument strips Override and negative Multi/Pmulti modifiers on hit and by sweep ten times a second | `12_Hooks.cs:361-368,214-240,265-310` | MATCH | OnHurt calls Cleanse on foe hits; Update sweeps monuments every 0.1 s. The Bulwark file is not supplied. |
| D:180-186 | Bonsai copies Tree upgrades live at 40%, counts rounded at least 1, distances kept, keeps last copy, slot rule, sells for 0 | `09_Dandelion.cs:435-486; 07_Building.cs:158-186; 05_Server.cs:335-343; 02_Data.cs:233-234` | MATCH | Copy runs every second; BonsaiKept, BonsaiCounts and share scaling match; TreeSlot follows the rule. |
| D:187-188 | Softening: Add -1 modifier on each node in range with maximum 2 or more, removed when none reaches it | `11_Pachira.cs:242-296` | MATCH | Healthresource Add -cut modifier when Value_Calc 2 or more; removed when out of range. |
| D:189-190 | Damage reduction adds Stonehenge Growth, Monument and aura, capped at 90% | `12_Hooks.cs:85-86; 02_Data.cs:239` | MATCH | Reduction adds armour and aura and clamps to ArmorCap 0.9. |
| D:191-193 | Easy Target scales hits by 1.15 while marked, lasting 1 s past the last vine tick, with Debuff_Yossele_Marked look | `12_Hooks.cs:89-105,397-399; 10_Henge.cs:239; 02_Data.cs:49,242` | MATCH | Mark adds Easy_Bonus; EasyLinger is 1; VfxMarked is the look. |
| D:194-195 | Hallucinogen is a plain stun with Yossele's own daze look | `08_Cannon.cs:274-278; 02_Data.cs:53` | MATCH | Stun roots with stun true and VfxDaze. |
| D:211-212 | Changes table: four upgrade levels become tiers 2-5 | `02_Data.cs:282-283; 04_Content.cs:479-480` | MATCH | Tier mapping as in the upgrade tree. |
| D:218 | Deep Breath charges 2 s past the laser's reload | `11_Pachira.cs:? ; 17_Directions.cs:28-32` | MATCH | Charged uses reload (1 / Attackspeed) plus BreathAfter 2. |
| D:219 | Growth on Tide start for buildings standing through the whole Tide | `07_Building.cs:95-105` | MATCH | TideBegan grows only buildings with BuiltWave <= wave - 2. |
| D:220 | Aura fruit on each payout of a fully grown Sapling strike, on the free tile beside the node | `11_Pachira.cs:191-224` | MATCH | NodeEmptied drops fruit on DropTile when FullGrown. |
| D:221 | D-MDS threats: every foe projectile that hurts; boss costs 5 seeds | `09_Dandelion.cs:234-269` | MATCH | Threat test and CostFor match. |
| D:222 | Cordyfriend cap of 30 at once per player | `02_Data.cs:216; 16_Friend.cs:20-27,34-37` | MATCH | FriendCap 30 is enforced per owner. |
| D:223 | Cordyfriend walks to foes within 4 tiles that are within 5 of rise point | `02_Data.cs:217-218; 16_Friend.cs:151-175` | MATCH | FriendEngage 4 and FriendLeash 5 are used in Think. |
| D:225 | Bonsai copies Tree upgrades live at 40% and keeps the last copy | `09_Dandelion.cs:465-485` | MATCH | Copy applies BonsaiShare to each upgrade and keeps the set when no Tree stands. |
| D:226 | Natural Remedy counts stage 1 before it has grown | `10_Henge.cs:274-276` | MATCH | Remedy uses Mathf.Max(1, Growth). |
| D:227 | Easy Target debuff lasts while on the vines and 1 s after | `10_Henge.cs:239; 12_Hooks.cs:89-96` | MATCH | Mark is given dt plus EasyLinger 1 each tick on the vines. |
| D:228 | Softening Roots Yellow 2300 Red 1000 | `02_Data.cs:356` | MATCH | Costs match the table. |
| D:229 | Stonehenge is a one-tile orientable building | `04_Content.cs:428-432` | MATCH | Orientable is set and ShowFacing adds the arrows. |
| D:236-243 | Test plan items: stats, growth forcing, Cordyfriend, Tree rules, Stonehenge and Sapling checks | `self-test code is excluded from this supply` | NOT-IN-CODE | Searched the supplied game code; the self-test (99_SelfTest.cs and YOSSELE_SELFTEST) was left out, so these checks cannot be matched. |

## 3. Requests

| Request | Result | Input tokens | Output tokens | $ |
| --- | --- | --- | --- | --- |
| design-Argo | ok | 129,857 | 20,314 | 0.065 |
| design-Spool | ok | 101,978 | 21,416 | 0.058 |
| design-Uloe | ok | 104,158 | 17,387 | 0.053 |
| design-Yossele | ok | 134,857 | 19,286 | 0.065 |
| scope-07 | ok | 137,104 | 33,618 | 0.084 |
| scope-08 | ok | 106,634 | 23,558 | 0.062 |
| scope-11 | ok | 686,803 | 25,556 | 0.246 |

A result other than `ok`: `max_tokens` (answer cut, rerun that request with a lower effort), `refusal`, `unreadable answer` or `errored` (rerun it alone with `--check` and `--scope-files`).

Spent on batch checks in 2026-11, this run included: $1.82 of the $100 monthly API credit.

<!-- batch-bases: {"design": "86988e1fcd8ab90e56fe3e07e8139f8b79f5b71e", "docs": "86988e1fcd8ab90e56fe3e07e8139f8b79f5b71e", "text": "86988e1fcd8ab90e56fe3e07e8139f8b79f5b71e"} -->
