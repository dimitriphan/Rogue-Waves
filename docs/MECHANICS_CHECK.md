# The 2026-10-09 scope's new mechanics, checked against the game (2026-10-09)

Dimitri's roguewaveWIP.txt (folded into `docs/SCOPE_v0.txt` on PR #15) adds a shopkeeper, Fund Transfer limits, vote
rules, vessel abilities, minion types and two enemy modifiers. This page says, for each, whether the game's code and the
mods already built settle it, or whether it needs the game, and which RogueLab round 4 probe answers it.

Tags: **proven** = shown in game by a mod (Riptide, Argo, Mummer, RogueLab rounds 1-3); **read** = from the decompiled
`Assembly-CSharp.dll` (kit `docs/13` §11), `File.cs:line` into that folder; **untested** = needs the PC. Kit paths:
`docs/NN` = `TDS_Vessel_Creator/docs/`. New engine facts found here are filed once in kit `docs/15` §16.

## 1. Answer

Everything in the file can be built. Nothing new needs a part of the game a mod can't reach. Most of it reuses routes
already proven by Argo, Riptide or RogueLab. Eight things can only be confirmed in the game, and RogueLab round 4 checks
all eight in one short solo launch, together with the main-menu button's probes. Seven more need a second player, so
they join the online evening that was already planned.

Four points need Dimitri's eye (§4): the Fund Transfer cap is 0 in the opening break as written; what a bonus tide pays;
the shop selling only ranks; and that "reduced aura gain" can't use the game's own multiplier for Tide pay.

## 2. Mechanic by mechanic

| Mechanic | Verdict | Route and evidence | Round 4 probe |
| --- | --- | --- | --- |
| **VENDCINT spawns between tides near the vessels' spawn point** | go | Host spawn with `Helper_Entitytools.Spawn` (proven, `docs/05` §10). Break start and end: `Game_Mapvars.Entry` and `Time_Wave`, which counts down to 0 (proven, RogueLab; Mode_Normal.cs:369, 415-419). Spawn near the player core (the core spawns vessels on its own adjacent tiles, Entity_Ai_Main_Core.cs:61-67, 87-100, read) | V1 |
| **Every player can use VENDCINT** | go, client half online | Owner -1 and every session id as a subowner, as Merchut does with 0..9 (Entity_Ai_Tower_Merchut.cs:59-61, read). Pass `Sameasparent: false` to `Spawn`, or the parent's owner overwrites it (Helper_Entitytools.cs:77-93, read). Its buttons show **only while it is selected** and only to an owner or subowner (Game_Ui.cs:298, read), so players click VENDCINT to shop. Distance is not checked by the game; the host can check it | V1 (host), online |
| **VENDCINT falls from the sky and jumps away** | go if V1 passes | The vessel fall is client-side code, not data: `Entity_Vis_Hero.Landing` sets the sprite shader's `_Offset` to (-10, 200, -10) and tweens it to 0 over 3 s, playing `Falling` then `Land` (Entity_Vis_Hero.cs:33-82, 171-178, read). The mod copies it in its client code from a host Vis RPC (proven route, `docs/05` §8). Listing `Entity_Vis_Hero` on VENDCINT would also bring a player arrow and a "VESSEL DROWNED" alert, so don't. The game has no jump-away code; the mod runs the tween upwards, then removes it without a death | V1 |
| VENDCINT's menu | go, proven | A button opens a `Runprompt` dialog on the presser's machine with its menu up; the pick goes to the host as a command on the player's own vessel (Riptide's Salvage, `docs/15` §6, §13). The dialog has no price field: the price goes in the card's name, and the host refuses what a player can't afford | - |
| Shop sells Cardinals and the next rank of held Directions | go | Host-side check of the buyer's Directions (`docs/15` §14.1). Cardinals bought mid-run: proven (§7) | - |
| **Fund Transfer, base aura only, 50 x tide per break** | go | Per colour, `Useresources` from the sender then `Giveresources` to the receiver, with `Resourcegainmultiplier` and `Resourcelossmultiplier` set to 1 around it (Helper_Resources.cs:21, 121-131, read). The cap is a host counter per player reset at each break. Amounts come as preset cards (25, 50, 100, all), since the dialog has no number entry. The game counts a transfer in the end screen's Gained and Spent (read; harmless) | F1 |
| Pure Aura, 3 a tide and more on bonus tides | go | A host counter per player, paid when `Wave` rises (`docs/15` §3) | - |
| **Leave by majority vote; endless bonus tides, each harder** | go, proven pieces | Votes are commands on each player's vessel (proven, Riptide). Count the voters as the players in `Game_Players.Prevsessionlist` with `Activeplayer > 0` (Helper_Resources.cs:53-60, read); recount when someone drops (online). Bonus tides: the mod writes the next Tide entry during each break, with bigger counts and elites (live writes proven, `docs/15` §13), and tops up the enemy core's Tides-left so the map doesn't end (§4, proven) | online |
| Every base map in the vote | go, proven | RogueLab rounds 2 and 3 played the base maps in the mod's mode; never the custom map slot (`docs/15` §15, PR #11) | - |
| Each map harder: modifiers, spawn rate, health, objectives | go, proven | Tide entries' counts, intervals and elite upgrades (proven); a health upgrade added at spawn (`docs/13` §4, §6.5, read) | A1 |
| 3 retries, a failed campaign keeps nothing | go | The campaign's save in `Bla_Data` (write, read back and restart proven, `docs/15` §8, §15); a failed campaign deletes its keys | - |
| 5 Swell levels: a new modifier and lower aura gain each | go, but see §4 | Modifiers as above. `Resourcegainmultiplier` scales aura given, but `Mode_Normal` sets it to 1 around its own Tide pay (Mode_Normal.cs:359-362, read), so lower pay means a smaller start aura, smaller drops or a core upgrade, as the easier setting does the other way (`docs/15` §14.3). Swell stays on hold | - |
| Base game vessels get Directions | go, content cost | Directions sit on each player's vessel (`docs/15` §14.1). A Boost on a stock vessel is a stat modifier or an upgrade on its towers through the mod's own spawn hook (proven pattern, `docs/05` §6, §9). A Shift that changes how a stock vessel plays is new code per vessel, so base vessels are cheapest with Boosts | - |
| **Yossele's Fertilize appears only once unlocked** | go if V2 passes | A vessel's buttons are its `Commandlist`, a networked list the host can add to and take from mid-match (`Createcommand`, `Removecommand`, Helper_Entitytools.cs:264-293, 409, read). Each machine's bar rebuilds when the list changes (Game_Ui.cs:1334-1342, 1387, read). The game does the same for the Money Bag's button (Entity_Ai_Effect_Artifact_Pig.cs:15, read) | V2 |
| Growth per tide survived, reset when destroyed | go, proven | Argo's Drill Rig sinks one Fathom per tide through the kit's tide hook (`ModKitTides`), and a building's brain dies with it (`docs/05` §6) | - |
| **Fertilizer per enemies killed near Yossele** (Compost Bin) | go if K1 passes | The game reports every despawn with the entity and a killed flag (`Game_Entity.Ondespawn`, `docs/05` §6); the mod checks the distance to Yossele. Counting kills near Yossele doesn't need to know who killed the enemy, which matters: `Lasthit` is set only when a hit's data carries no `Damage` (Helper_Entitytools.cs:166-171, read), and projectiles pass their own data (Projectile_Master.cs:394, read), so tower kills may not name a killer | K1 |
| **Aura fruit picked up by any vessel or minion** | go if O1 passes | The game's own aura pickup looks for a same-team `Mob`-tagged entity within 1 unit and pays its owner (Entity_Ai_Artifact_Checklist_Special_Pickup.cs:20-51, read); vessels and the kit's minions carry `Mob` (verified). The fruit copies that. A minion pays its player only if it was spawned with the player as its owner; RogueLab's Boutbro spawned from the core had owner -1 (round 2) | O1 |
| Vines siphon health and heal | go, proven pattern | An area tick that hurts foes standing on the vines and heals the Stonehenge or buildings (`Interacthurt`, `Interactheal`), like Argo's Deep Water (proven) | - |
| Spores: stacking damage, slow at max, a spore bomb on death | go | Copy the stock Blaze effect (a counter stat, ticking damage, a look, removed at the end; Entity_Ai_Effect_Bustertotem_Blaze.cs:28-65, read) and the stock explode-on-death enemy upgrade (Entity_Ai_Enemy_Explode_Upgrade.cs:50-61, read) | - |
| Spool's Unthread | go, proven | A click-a-unit picker (Mummer's HOOK, Argo's crew, `docs/05` §7), `Despawn` on the host, the Thread counter refunded (`docs/05` §9) | - |
| Crafted don't follow Spool; Thread Head like Boutman's fish | go | Fewer orders to build. Fish guard a spot and can follow the vessel (`docs/12` §1, §4) | - |
| Stitched, Dart, Seam, Mangled | go | Melee and ranged units (proven, `docs/12` §3). Seam mines with the game's `Entity_Ai_Mob_Smackyminer`, which walks back to its parent, here the Loom (Entity_Ai_Mob_Smackyminer.cs:49-90, read; Sprigg's minions use it). Mangled: a hook on the Pin Cushion's damage spawns them (proven hook pattern, `docs/05` §9) | - |
| Uloe's Plug In and its cord | go, proven pieces | Range to the plug point is a host check. The cord is a `LineRenderer` reset every frame on each machine, like Argo's harpoon rope (proven). Light is a number the mod sums; the game has no gameplay light (`docs/15` §14.7). A friend who joins mid-map needs the plug state from the host (online) | online |
| Illuminate and Blind, with levels | go | Illuminated: a handler first in the target's damage chain raising `Damagemulti`; Blinded: a timed `Damage` modifier (`docs/15` §14.7, Argo's Pinged proven). Mirror's chance to nullify: the same handler setting `Damagemulti` to 0 | A1 |
| **Aging: -30% speed, health and damage up to +100% over 50 s** | go if A1 passes | Health is the `Health` stat: `Value` is current, `Value_Calc` the max (Link_Stats.cs:46-103, read). One `Multi` modifier per stat, its value raised each tick, as the stock Melting effect does (Entity_Ai_Effect_Enemy_Weaken.cs:55, 79, read). Whether the health fraction is kept depends on the stat's type, set in data (Link_Stats.cs:79-98): A1 reads it and the mod sets the fraction itself if needed | A1 |
| Dazzling | go | The enemy finds nearby vessels, towers and units (`Helper_Entityquerytools.Entityarea`) and Blinds or Illuminates them as above. Boutman applies damage at priority 9, not 10 (Entity_Ai_Hero_Boutman.cs:124, 146, read), so the handler goes first in the chain. A building with no `Damage` stat (a wall) can't be Blinded | - |
| **Modifiers as an effect on the enemy, not a skin** | go if E1 passes | Stock elites swap the skin (`Master.Skin`); a look instead goes through `Radio.Vfxdebuffcheck`, re-sent every few seconds (`docs/13` §6.4, `docs/05` §8; Argo does it) | E1 |
| **Vampire Dentures** | go if D1 passes | The amount dealt is known only after the hit, in the vessel's `Damagedone` (Entity_Master.cs:208, read); heal by a share of its rise with `Interactheal`. Projectile hits count for the shooter (Projectile_Master.cs:394, read) | D1 |
| Optional objectives | go | Merchut's contracts are hardcoded, but each piece is public: the enemy core's `Spawn` for marked foes, `Ondespawn` for the kill check, pay through the Pure Aura counter (`docs/15` §5) | - |

## 3. RogueLab round 4

RogueLab 0.4.0 (`TDS_Mod/ModSource/RogueLab`, compiled here with 0 errors): solo host, Boutman, one map. It starts from
the main menu button (probes M1-M5 and M7 of `docs/MAIN_MENU_START.md` §5), then runs these in map 1. Boutbro stands
in for VENDCINT.

| # | When | Probe | Pass when |
| --- | --- | --- | --- |
| V2 | opening break | With the vessel selected, add a button to it with a borrowed stock command, then remove it | the button appears on the bar, then goes |
| V1 | opening break | Spawn the stand-in near the core: owner -1, every player a subowner; give it a button; select it; run the fall-in on its sprites; jump; remove it | owner -1 and the local player a subowner; the button shows; its sprite materials have `_Offset` and move; it is gone afterwards. Also logs the stand-in's and the vessel's animation names |
| F1 | opening break | Give 100 of each base aura, take 40, give 40 back | each colour moves by exactly 40; logs `Wave` in the opening break |
| O1 | opening break | Spawn a minion with the vessel as its parent | its owner is the local player and it is tagged `Mob` |
| D1 | Tide 1 | The vessel hits a foe the way its attack does; a foe hits the vessel for 5, then a heal of 3 | `Damagedone` rises; logs the heal |
| A1 | Tide 1 | Hurt a foe, double its max health, then -30% speed and x2 damage | max health doubles; logs the stat type and whether the fraction was kept; speed x0.7, damage x2 |
| E1 | Tide 1 | `Vfxdebuffcheck("Debuff_Boutman_Red")` on a foe | a child appears under the foe's look |
| K1 | Tide 1 | The vessel hits a foe until it dies | `Ondespawn` gets it killed, with its place and the vessel as its last hitter; logs other deaths' killers |

**Online evening** (already planned, `docs/RESEARCH_GAPS.md`): a client sees VENDCINT's button and its press reaches
the host; a client's bar gets the Fertilize button; the fall-in and Uloe's cord show on a client and for a late
joiner; Aging's health shows right on a client's panel; the modifier look shows on a client; a Fund Transfer between
two players; the leave vote counts right after a player drops. **By hand**, once VENDCINT has its sprite: how its fall
and jump look, and whether foes go for it (default: it stands off the lanes and leaves before the Tide).

## 4. For Dimitri

| Point | Default taken in the scope |
| --- | --- |
| The cap is 50 x the tide number, and the tide number is 0 in the opening break (Mode_Normal.cs:318, read), so nothing could be sent before Tide 1 | Count the tide that is coming: 50 before Tide 1, 250 in the break before Tide 5 |
| "Bonus tides paying an additional 1 Pure Aura per tide" | Each bonus tide pays 4 (3 + 1). If it should climb (4, 5, 6, ...), say so |
| The shop sells only ranks of Directions a vessel already holds | New Directions come only from the pick before each map; the Directions template lost its Shop line, as in your file |
| Fund Transfer is base aura only | Replaces `docs/SCOPE_VS_LOOP.md` #6's idea that the team pools Pure Aura through it |
| "Reduced aura gain" per Swell level | The game's gain multiplier doesn't touch Tide pay, so a Swell lowers start aura and drops instead. Decided at Swell's design, which stays on hold |
