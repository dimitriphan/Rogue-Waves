# Research gaps: what the expansion still needs to learn about the game (2026-10-08)

Engine facts the scope (`docs/SCOPE_v0.txt`) depends on that no research has settled yet. Checked against
`TDS_Vessel_Creator/docs/13`, `14`, `15` (Phase 1), RogueLab round 1, Riptide's notes and Argo's six PC rounds, on
branch `claude/project-thread-f7fmvc` (PR #7). It is the research half of `docs/SCOPE_GAPS.md` (PR #6), which lists
the **design** gaps (catalogues, numbers, art); nothing here repeats it. Sonar is out of scope for now (user).

**How each is answered:** **cloud** = read from the decompiled game in a container (`docs/13` §11), no PC;
**PC** = one line in a RogueLab-style research build; **online** = needs a second player. "Lead" is a name from the
API dump (`reference/api_assembly_csharp.txt`) found while writing this page, not yet read.

## Summary

Nine gaps block the mode's design (A), five sit in front of vessel design gates (B), four are Phase 4 leftovers (C),
and six expansion extras the scope never asks for wait on a yes (D). Everything in A and B is a cloud read except four
PC lines and the online check, so it fits **one cloud research session and one PC round** (§E).

## A. Before Phase 5 designs the mode

| # | Gap | Why it blocks | Known / lead | How |
| --- | --- | --- | --- | --- |
| A1 | **Per-player Directions.** The kit grants a Direction on the core, and the core is the team's (`ModKitDirections.cs` header) | The scope's Directions are "specific to each player"; on the core every player gets every pick | the Basin and "Sub-Basins" (Glass Core's text) may mean one core per player or several per team; `Artifact_Heroapply` reaches heroes. Fallback: the upgrade on the vessel, re-granted by the host at each respawn, as Pure Aura's counter is | cloud: how many cores a 2-4 player match spawns and who owns them; then a kit 1.6 fix |
| A2 | **Fund Transfer's route.** `Helper_Resources` between players is untested (`docs/15` §5) | The scope's one cooperative economy piece | **lead:** the stock Cardinal *Money Bag* (`Artifact_Pig`, "Throwable Aura, enables donation"), with `Entity_Command_Artifact_Pig_Launch` and `Entity_Ai_Effect_Artifact_Pig`: the base game already moves aura between players | cloud: read the Pig's donation; reuse it if it fits. Also settle which aura moves (PLAN says "aura sharing"; Pure Aura is per player too) |
| A3 | **Difficulty knobs.** What can make map 2 harder than map 1, and an ascension level harder than the last | "Increasing difficulty" and ascension have no lever named yet | proven: live Tide table writes and elites (Riptide's budget curve). Unread: `Statproperties.Leveladd` and `Defaultlevel` (do foes level up, by what?), `Mapentryobject.Difficulty` (int per map), Storm's 15-Tide table vs Normal's 30 (`docs/13` §10 Q3) | cloud: what sets an entity's level and who reads `Difficulty`; PC: log every map's `Difficulty` and the other modes' tables |
| A4 | **The map pool.** Which base maps the vote may offer | Every map is voted (user); a bad pick (tutorial, boss, gate map) breaks a run | 51 Gameplans, 54 maps logged (RogueLab step 1); names hint at tiers (`0B_` .. `5B_`, `Alt_`, `3BH_Kong`, `5B_Final`, `Special_`, `Test_`); RogueLab made one Gameplan twin, for 3B_Thehunt | cloud: what a Gameplan twin needs per map, which maps carry their own mode or groups (Explode and Weakened elites live in some other group); PC: play-load each candidate once |
| A5 | **Disconnect and reconnect mid-run** | Run state is keyed by client id; a dropped friend could lose their Pure Aura and Directions, or stall a vote | the game has its own reconnect: `Game_Players.Disconnectedreconnect`, `Prevsessionlist`, `Playerdisconnected`, a `Reconnect(Death)` on two AIs | cloud: which id survives a reconnect and when the vessel comes back; online: drop and rejoin once |
| A6 | **A shop and a vote that show more than three things** | The shop sells Cardinals and Directions at prices; votes need a visible tally | `Runprompt` proven with 3 cards, re-centred by hand (Riptide); unanswered it ends Faulted (§11 step 7). Merchut's buttons are the other route; a shop building needs a free spot on any map (kit `ModKitTest.Spot`) | PC: `Runprompt` with 8 entries and long text; spawn a neutral building near the Basin on two maps |
| A7 | **A save key surviving a restart** | Ascension is lost if it doesn't | write and read back proven; restart never tried (`docs/15` §11 step 5) | PC: one relaunch with the RogueLab build |
| A8 | **Online, for any mode piece** | The scope's "online compatibility"; nothing in the mode (Runprompt by Vis RPC, the chain, a mid-run Cardinal's HUD, the F key) has run on a client | vessels are proven online (`docs/07`); Riptide's sending probe is still untested | online: `docs/15` §11 step 11 with a friend, **before** Phase 5, not at its end |
| A9 | **The pack layout in game.** One bundle with several `Default` prefabs, each compiled alone | `docs/PACK.md` rests on it; every part built since assumes it | read from code only (`docs/14` §6 Q4); Argo is still its own dev mod; the Build Queue has no `Pack` step | PC: export RogueLab and Argo as one test bundle; both must boot and self-test |

## B. Before each vessel's design gate

| # | Gap | Vessel | Known / lead | How |
| --- | --- | --- | --- | --- |
| B1 | **"Light received" as a stat** (PLAN's feasibility check, not started) | Uloe | the game's only light code is rendering (`Bl_Lighting`, `Map_PrefabLightmapData`, `LightProbeRuntime`), so light is likely a number the mod computes from sources in range, never Unity light | cloud: confirm no gameplay light exists; then a Fable design note on sources, falloff and its cost per tick |
| B2 | **Changing damage as it lands**: reflect melee then ranged hits (Pin Cushion), half now and half later (Pin Cushion), take more (Illuminated), deal less (Blinded, Mirror) | Spool, Uloe | damage reads `Damage × Damagemulti` (`docs/01` §9); `Healtharmor` exists; **lead:** a public `Entity_Master.Damagetaken`. Unknown: whether a target's AI hears the hit and its attacker | cloud: the hurt path in `Helper_Damagetools`, any per-target damage-taken stat, any on-hurt callback |
| B3 | **Things on the ground players or units pick up**: aura fruit, Thread drops flying to a bucket "without lag" | Yossele, Spool | **leads:** `Entity_Ai_Artifact_Checklist_Special_Pickup` (resources, a collect query, a duration), Boutman's fish, Buster's checkpoint `Collectore` | cloud: read the pickup AI and Boutman's fish |
| B4 | **Directions that change a stock vessel** | Directions catalogue | stat Directions on stock vessels are easy (`Towerapply`, `Heroapply`); a "fundamental change" to Boutman or Fie means editing stock towers' data at run time | only if the catalogue gives stock vessels their own (a SCOPE_GAPS decision); then cloud |
| B5 | **A swarm's cost online** | Spool, Uloe | brains tick every 0.4 s (`docs/12`); no unit count was ever measured | PC: spawn 50, 100, 200 units with pickups, log frame time on host and client |

## C. Enemies (Phase 4), still open from `docs/13` §10

| # | Gap | Needed when |
| --- | --- | --- |
| C1 | Explode and Weakened elite values (not in Mode_Normal's group; Q2) | if the run's director offers them |
| C2 | The other modes' Tide tables (Q3) | A3 |
| C3 | Enemy sprite format and the elite skins' recolours (Q5) | only for new enemy types, or if Cursed and Berserk get skins (`STYLE.md` and `docs/13` §6.4 disagree, SCOPE_GAPS §5) |
| C4 | Whether the journal has enemy pages to join (Q6) | Cursed and Berserk's journal pages |

Flying (Q7) waits for a flying enemy.

## D. Expansion pieces the scope does not ask for

From `docs/EXPANSION_SCOPE.md`. None blocks the scope; each needs research only if the user wants it.

| Row | Piece | Note |
| --- | --- | --- |
| 4 | New maps | the scope's maps are the base game's; the `Map` prefab route is known (`docs/15` §10) |
| 5 | A boss or a final fight for map 3 | nothing read; `Mode_Final` exists |
| 8 | Music and stings | nothing read; `Gameplan.Soundtrack` picks one of six tracks by name |
| 12 | Localization | nothing read; mod text is English |
| 13 | Tutorial tips | `Tipthreshold` is a required mode param (`docs/15` §2); nothing else read |
| 14 | Vessel skins | partly read |

## E. Proposed order

1. **One cloud session** (decompiled game, Opus by default; B1 by Fable per PLAN Part A): A1, A2, A3 and A4's code
   half, A5's code half, B1, B2, B3. Results go to `docs/15` (mode) or the chapter each belongs to.
2. **One PC round**, "RogueLab round 2": A3 and A4's logs, A6, A7, A9, B5's host half.
3. **One online evening** with a friend: A5, A8, B5's client half.
4. C at Phase 4; D only on a yes.
