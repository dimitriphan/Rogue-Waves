# Base-vessel and map checks on the PC (RogueLab round 7, 2026-10-12)

Dimitri asked to test every PC check left that a solo, unattended run can reach. That was kit `docs/17` §12 (the
five base vessels), kit `docs/16` §9 items 1-3 (maps), and the stat rules `17` §2.1-2.4 read from code. RogueLab
0.5.x (`TDS_Mod/ModSource/RogueLab/Code/09_Round7.cs`) ran three launches on one solo host, as Boutman on
3B_Thehunt. It loaded the other four vessels' groups so their buildings could be placed for the player. Every line
is in `TDS_Vessel_Creator/reference/roguelab_round7.txt`. The game's Mods folder was empty before and after.

| Launch | Build | Result |
| --- | --- | --- |
| 1 | 0.5.0, the whole round | 24 pass, 2 fail, 1 setup |
| 2 | 0.5.1, P1, M1 and T1 again after probe fixes | 7 pass, 2 fail (both M1, probe-side) |
| 3 | 0.5.2, M1 with the den's `Addperwave` cut to 0 | 7 pass, 0 fail |

All three failures and the setup were in the probes, not the game. Launch 1's Bouthouse check counted a skin change
the house always makes. P1's first path ended on the core's own tile. M1's den has a beast only every sixth Tide at
its stock numbers. Every exception logged was the game's own (`OperationCanceledException` from cancelled waits,
`Game_Init.OnDestroy` on quit).

## Verified in game

| # | Check | What happened |
| --- | --- | --- |
| S2 | Percentages add (`17` §2.1) | Archer Damage 4: two `Multi +0.5` give 8 (×2, not ×2.25). Two `Pmulti +0.5` also give ×2. A `Pmulti` with a `Multi` gives 9 (×2.25) |
| S3 | An outside mod outlives a stock upgrade (`17` §2.1) | Range 7 with an outside +1. Long Bow made it 9.5, removing Long Bow brought back 7, and the mod stayed listed throughout |
| S1 | Stat types (`17` §12.2) | Health is `Variable` on the vessel and the core; every other hero stat and the core's `Respawn` are `Fixed`. Raising max Health **keeps the health fraction**: full 50/50 +20 gave 70/70, hurt 30/50 +20 gave 42/70, then `Multi +0.5` gave 63/105. A Health Cardinal therefore heals nothing on a hurt vessel |
| B1 | Price scales (`16` §9.3) | A `Buildoption` Goalvalue of 0.8 made a Red 25 archer cost 20 (25 at 1), and its `Sellvalue` was 0.8. An `Upgrade` command Goalvalue of 0.8 made Long Bow (Blue 25) cost 20 |
| U1 | A Priority −3 upgrade (`17` §12.5, §2.2) | Level and main upgrade stayed the same on an archer at Level 2 (Long Bow), on a Sno Cannon and on the Bouthouse. **The Bouthouse sets `Skin` to its Level on any upgrade** (`'1'`, which has no override, so the model stays as it was; `Entity_Ai_Tower_Boutmanhouse.Upgrade`). Control: a Priority 0 upgrade on a fresh archer became its main upgrade and stayed so after `Removeupgrade` |
| U1 | The house leaks (`17` §5.1) | The probe upgrade put on the Bouthouse was on Boutman 1.5 s later |
| U1 | An upgrade's params do nothing by themselves (new) | The probe carried `Range_Add 0.5`; Range did not move on the archer, the cannon or Boutman. Each stock AI applies the params of **its own upgrades, by name** (`Entity_Ai_Tower_Smackyarcher` reads `Range_Add` from its Long Bow field). A Direction must `Addmod` itself |
| W1 | The Bout Way (`17` §12.3) | Boutbro's options at the start: fish ×0, barrel ×0, boat ×1. Fish 1 and barrel 1 were free; fish 2 cost Yellow 25. A `Fishcost −20 %` mod made after fish 2 left fish 3 at 50 (the option stays ×2 until a recount); fish 4 cost 60 (×2.4 = 3 × 0.8) |
| N1 | Unit counts (`17` §12.4) | On the Hut, the Keep and the Garage, `Units +1` alone spawned nothing. `SendMessage` of the building's own count check spawned exactly one, in the same frame: `Addressminercount` (Hut), `Addressmobcount` (Keep), `Addressdogcount` (Garage). These reconcile the units to `Units` both ways; `Add` would add one every call (IL of all three) |
| R1 | A respawn keeps a Priority −3 upgrade on the vessel (`17` §2.4, `RESEARCH_GAPS` E3) | Kept |
| R2 | Faster respawn per player (`17` §12.6) | The core's `Respawn` is 10 (Fixed). A respawn took 10.0 s; with `Respawn −50 %` on the core **before** the death it took 5.0 s. The same cut made in the vessel's own `Despawn` handler or in `Game_Entity.Ondespawn` changed nothing (10.0 s): the core's respawn handler subscribed first and reads the stat as the vessel despawns |
| T1 | The `Sno` tag grab (`17` §4, §11.12) | A Dagger copy whose data tags add `Sno`, beside a Sno Cannon, was despawned in 0.17 s (`killed False`, reason `''`: `Shootsnow` eats a `Mob` as ammo). A stock Dagger on the same tile stayed 12 s. No exception was logged |
| P1 | A blocked tile re-routes (`16` §9.2) | A wall spawned on tile 21 of the foes' 61-tile path. The cached path still crossed it until `Game_Grid.Savedpaths` was cleared; then a 60-tile path went round it |
| M1 | A mine node spawned mid-match (`16` §9.1) | `Resource_Red` (team 3, Healthresource 4, Redauramine 5) paid 5 Red when hit. The Hut's miner kept mining the ore it already had (`Resource_Orange`) for 40 s, so a node added mid-match is used only by miners that pick a new target |
| M1 | A den spawned mid-match (`16` §9.1) | A second `Tower_Den` (team 3 like the map's, tagged `Resource`) added 2 beasts at Tide 1 once its `Addperwave` was 0. At the stock 5, a den adds a beast only when its Tide counter exceeds `Addperwave` (`Entity_Ai_Tower_Den.Newwave`), so no den on any map grows before Tide 6 |
| C1 | Each stock Cardinal's core upgrades (`17` §12.1) | All 40 dumped (`CARD` lines). Frozen Computer and Wave Breaker use Priority 1; every other one uses −1. Processor Jr, Starter Seeds, Cubed Drink, Sand Filled Box and Tacklebox put `Entity_Ai_Hero_Buildsell` plus a build AI with a `Buildslot` on the core |

## Not reached by this run

- `17` §12.3's second half: whether a Blurry Photo clone takes the Bouthouse's upgrades (needs that Cardinal
  equipped; next RogueLab round).
- `16` §9 items 4-5 and everything in `RESEARCH_GAPS.md` §E: the online evening, the F-key on a real Boutman death,
  and the looks by eye.
