# Scope gaps: what `SCOPE_v0.txt` names that is not yet expanded (2026-10-08)

> **Partly superseded (note added 2026-10-11, `docs/reports/QUALITY_CHECK_2026-10-11.md`).** Settled since: the campaign starts
> from a main-menu button (no start Cardinal), maps are voted on the sea chart among the 1-3 linked nodes, VENDCINT is
> the shopkeeper and the Fund Transfer cap is 50 x the coming tide, Thread Head is Spool's minion, Vampire Dentures is
> the first named Cardinal, and modifiers show as an effect, not a skin (`docs/scope/13`). Since then: the
> mode part `RogueWavesMode` exists (2026-10-10: sea chart, sea weather, rejoin), Yossele and Spool have full sheets in the vessel
> template (`docs/scope/07`, `08`), Spool's ability is Patch and a Yarn Stash holds the Thread (Thread Heads come from it by
> default), and Yossele is built in draft PR #30. Check `docs/scope/` before acting on a row.

Every asset `docs/SCOPE_v0.txt` names or implies, checked against branch `claude/phase-1-6lfnar` (PR #3) on
2026-10-08: what exists and what is still missing. It is a to-do list, not a design; each gap goes to the phase in
`docs/PLAN.md` Part B that owns it. Sonar is out of every design for now (user, 2026-10-08). Items only Dimitri can
supply are also marked `! NEEDS ... !` in `docs/SCOPE_v0.txt` itself; Argo's rows were updated on 2026-10-09.

**Status:** **none** = only the scope's words; **concept** = described in a concept page or `docs/STYLE.md`, no
numbers; **routed** = the engine route is read or proven (`TDS_Vessel_Creator/docs/15`), nothing designed;
**designed** = a `DESIGN.md` exists; **built** = compiles here; **tested** = passed on the PC.

## Summary

| Area | Status | Biggest gap |
| --- | --- | --- |
| Argo | tested: PC round 6 139/0, Phase 3 closed (PLAN Part C) | Argo's Directions; Dimitri's balance pass |
| Yossele | concept | design gate; Growth and fertilizer rules; Mushroom Cannon has no upgrade ideas |
| Spool | concept | design gate; Thread Head building or minion; minion orders UI; Mummer name clash |
| Uloe | concept | design gate (light received is feasible, `RESEARCH_GAPS.md` B1); Mirror has no upgrades |
| Rogue Waves mode | routed (start, chain, leave, prompt, save proven) | nothing designed: no objective list, shop, prices, difficulty curve or ascension levels |
| Directions | routed, kit file built | no catalogue at all; per-player grant decided (on the vessel, kit 1.7) |
| Pure Aura, Fund Transfer | routed | amounts, limit, icon, counter art |
| New Cardinals | routed and proven | none named yet; no effects |
| New enemy types | none | the scope names none |
| Cursed, Berserk | routed | numbers, look, journal pages |
| F-key bug fix | built (`ModKitFocus`) | never reproduced; needs a hand check with Boutbro |
| Online | routed | never played online; the friend check is manual |

## 1. The Rogue Waves mode (scope "Rogue Waves Mode", "Gameplay Rundown")

Engine verdicts: `TDS_Vessel_Creator/docs/15` §1. Build order: PLAN Phase 5. Prior art: `ModSource/Riptide` (Tide
director, Salvage dialog), `ModSource/RogueLab` (chain, leave, save).

| Asset | Exists | Missing |
| --- | --- | --- |
| The mode's start Cardinal | route decided: the host equips it in the lobby (PLAN Decisions); a mod Cardinal proven in the colour slot (`docs/15` §11) | **its name** ("Rogue Waves" is the mod's, user), entry, icon (the crest, `STYLE.md`), lobby text; the picker's look by hand is unseen |
| Vessel lock before the run | ready-up is the lock (`docs/15` §6) | nothing to build; one line of player-facing text |
| Map 1 staging step | rule decided (every map voted, `docs/15` §6) | the hold, the banner, what players see while it waits |
| Map vote (3 base-game maps) | route: in-game vote by vessel commands (`docs/15` §6) | **which maps can be offered** (pool per map 1/2/3, by difficulty?), the vote dialog, tie-break, timeout |
| Maps with increasing difficulty (now a 10-map campaign) | chain proven (RogueLab) | **what "harder" means per map**: Tide table or budget curve per map (Riptide's curve is the model), modifiers per map |
| "Modifiers / enemies that change with each run" | Riptide's seeded director picks foes and elites | which pool is randomised (stock foes, Cursed, Berserk, new types), per-run seed rules |
| Tide 15 leave-or-stay | win path between Tides proven (`docs/15` §4) | the vote's UI, timer, majority rule; what leaving pays |
| Bonus Tides past 15 | longer table or rewind proven | how many, how they scale, Pure Aura per bonus Tide, the final Tide of the last map (the scope's win condition) |
| Run success / failure | stock win and lose screens | run summary (maps, Tides, Pure Aura), what a loss keeps (nothing?) |
| Ascension levels | save key route (`RW_Ascension`, `docs/15` §8); naming "Depth 1, 2, ..." (`STYLE.md`) | **what each level adds** ("a harder difficulty modifier"), how many, how it's picked at the start, restart persistence check |
| Mode journal page, tips, banners | banner voice in `STYLE.md` ("A rogue wave rises") | every string; a journal page per mechanic (Phase 5 asks for one per piece) |
| Mode UI art | palette and crest motif (`STYLE.md`) | frames for the vote, shop, Direction cards, leave prompt |
| Music, stings | none (`EXPANSION_SCOPE.md` row 8, not named in the scope) | decide whether the mode has any |

## 2. New mechanics (scope "New Mechanics", "Upgrade System", "Streamlined Economy")

| Asset | Exists | Missing |
| --- | --- | --- |
| **Directions** (mechanic) | `ModKitDirections.cs` (kit 1.6): Directions as upgrades, granted on the core in 1.6; pick dialog proven (Riptide Salvage); default: 3 offered, 1 picked per map, extras at the shop (PLAN) | **per-player grant**: decided (`RESEARCH_GAPS.md` A1): the core is the team's, so a Direction goes on the player's vessel, re-granted when each map starts; kit 1.7 makes the change at Phase 5 |
| **Direction catalogue** | names' voice only ("Deep Roots, Loose Threads, Hard to Port", `STYLE.md`) | every Direction: stat bonuses and "fundamental changes" per vessel (Argo, Yossele, Spool, Uloe), generic `Any` ones, and **whether stock vessels get their own** (the mode runs with stock vessels, `PACK.md` §2); icons, text, prices |
| **Pure Aura** | counter route (`ModKitCounter`), host state proven across maps; Pearl colour and drop motif (`STYLE.md`) | earn rate per Tide, bonus Tide rate, the counter's icon, the pickup/reward effect, sound |
| **Objectives** (bounty-like, optional, "accepting") | route: a mod brain on Merchut's pattern, with a kill check (`docs/15` §5) | **the objective list** (kill X with elite Y, protect, no leaks...?), how one is offered and accepted, rewards, failure, the button and its art |
| **Shopkeeper** | route: commands on a shop building or on each vessel (`docs/15` §5); stall colour Wet Sand | **who and where**: a map building, an NPC sprite, or a vessel menu; stock (which Cardinals and Directions), prices, restocks, opening hours (between Tides only) |
| **Fund Transfer** (= the scope's "streamlined economy", PLAN) | route: a shop command, `Helper_Resources` (untested) | the "limited amount": cap per break or per map; the button and confirmation |
| Fertilizer (Yossele) | named in the scope | see §3 |

## 3. Vessels (scope "New Vessels")

Every vessel needs the kit's full set (`TDS_Vessel_Creator/README.md`, definition of done): `DESIGN.md` with stats,
attack, ability or none, difficulty, lore and upgrade trees tiers 2-5; the full sprite sheet (`docs/03` §1); tower
and upgrade models; effects, sounds, icons; journal; self-test; a release. The concept page
`docs/concepts/YOSSELE_SPOOL_ULOE.md` §5 lists eleven art-versus-scope mismatches for its three vessels' design gates.

### Argo, Deep Sea Diver: tested, Phase 3 closed

| Asset | Exists | Missing |
| --- | --- | --- |
| All five buildables, trees, Nauts, Deep Water's bob | `Argo/DESIGN.md` 2.0; steps 1-5 done; PC round 6 139 pass, 0 fail (`Argo/IMPLEMENTATION_NOTES.md`) | nothing |
| Argo's Directions | none | Argo's entries in the catalogue (§2) |
| Balance and release | ships in the pack, no release of its own (PLAN Part C) | Dimitri's balance pass (his call); SONAR's unused 1.4.0 code waits on the user |

### Yossele, The Patient Golem: concept only

| Asset (scope) | Exists | Missing |
| --- | --- | --- |
| The vessel | one 32×32 frame (`docs/concepts/art`), concept §1, STYLE row | stats ("more health, damage, regen"), its attack, ability, difficulty (default Intermediate), lore; the hole and sprouts' meaning (§5 items 5-6) |
| **Growth** (every building) | Sprigg's Rings and stage skins as prior art | the rule: time alive, Tides survived, or both; stages, what resets it (damage? rebuild?), how it shows (skins, `STYLE.md`) |
| **Fertilizer** | named | **how it is earned**, how it is spent, its pickup and UI (Sprigg's Mulch is the nearest) |
| Mushroom Cannon | scope text: spore stacks, DOT, Growth, full-Growth bounce | **no upgrade ideas in the scope**: the whole tree; spore stack cap; the Spores debuff look |
| Primordial Dandelion Tree (limit 1) | four upgrade ideas (hold/strike, refund, low-stack rate, Dandelion Storm) | the hold/strike toggle's button, seed caps, the floating-seed effect, the limit-1 build rule |
| Overgrown Stonehenge (Defense) | four ideas (long vines, vine wall each Tide, heals, portal capstone) | which debuffs and buffs vines carry, vine reach per Growth, the vine visual, the portal |
| Pachira Sapling (Economy) | two ideas (one node faster, fruit colour) | root reach, fruit pickup (who can pick it up, does it sync), the colour picker |
| Tower look | motif "round holes and root feet", Growth skins (`STYLE.md`) | every model and skin |

### Spool, Crafty Crafted Crafter: concept only

| Asset (scope) | Exists | Missing |
| --- | --- | --- |
| The vessel | one frame, concept §2, STYLE row | stats, attack, ability, difficulty (default Advanced), lore; "nothing in the sprite is a spool" (§5 item 7) |
| **Thread** currency | Mummer's Thread is prior art (`docs/02` §7) | drop rate from foes, node mining rate, cap, the flying pickup that "doesn't lag" (Boutman's fish route), counter |
| **Crafted** (minions) | none | unit types (melee, ranged, explode-on-death, tanky, aggressive), Thread prices, sprites for each, `docs/12` unit rules, "unthread by Spool" command |
| Minion orders on spawn (gatherer vs attacker) | none | the order UI and its sync; how a gatherer differs from a Thread Head |
| Crafted follow Spool ("maybe an upgrade") | none | decide: in or out |
| Repurposed Cookie Tin | garage-like behaviour, two upgrade ideas | the targeting picker, path split "like Boutman's fish", the tree |
| Thread Head (Support) | described as "a single larger minion" | **building or minion?** It decides whether Spool has four buildables (§5 item 8); porter/buffer switch, heal |
| Pin Cushion (Defense) | reflect, spin Thread, two ideas | reflect rule, the delayed-damage ("half now, half later") math, the Crafted spawn trigger |
| Loom (Economy) | two ideas | how it links to the Cookie Tin; **name clash with Mummer's Loom and Thread** (§5 item 11) |

### Uloe, Light Personified: concept only, feasibility first

| Asset (scope) | Exists | Missing |
| --- | --- | --- |
| **Light** ("light received" as a stat) | feasible (`RESEARCH_GAPS.md` B1): no gameplay light exists, so the mod sums sources in range with the game's area query | the rule: sources, falloff, cap, how a tower shows its light |
| The vessel | one frame, concept §3, STYLE row; Mirror named by the user | stats, whether Uloe attacks (§5 item 9), ability, difficulty (default Advanced), lore, the plug and cord (§5 item 10) |
| **Photons** (minions) | none | wander rule, following a Flood Light beam, damage, lifetime, the sprite |
| **Illuminated** and **Blinded** debuffs | none | values, durations, their effect visuals |
| Magnifying Glass (Offense) | one idea (ramp on the same target) | the beam visual and light-to-damage curve; the rest of the tree |
| Power Bank (Support) | one idea (Photons explode) | spawn rate, cap, the rest of the tree |
| Mirror (Defense) | name only (user) | **everything**: the scope names it but gives no ideas or numbers beyond "empowered by light to blind" |
| Flood Light (Economy) | two exclusive paths (radius, or redirected beam) | the aiming UI (Argo's AIM picker is prior art), the beam visual, node mining |

## 4. Cardinals (scope "New Cardinals")

| Asset | Exists | Missing |
| --- | --- | --- |
| The system | data route proven, mid-run grant proven (`docs/15` §7, §13); Riptide's pick from the 40 stock artifacts (its whitelist is empty, not wired yet) | nothing |
| New Cardinals | the naming voice only (`STYLE.md`); none named yet (user) | **the list**: names, how many, each effect, icons (12×12 flotsam), unlock rule, which are shop-only |
| Shop stock of Cardinals | none | which stock and new Cardinals the shop sells, prices in Pure Aura |

## 5. Enemies and modifiers (scope "New Enemy Types + Modifiers", "New Enemy Modifiers")

| Asset | Exists | Missing |
| --- | --- | --- |
| **New enemy types** | the route (`docs/13` §5); stock foe data (RogueLab round 1) | **the scope names none.** How many, what each does, art; or confirm the header means only the two modifiers |
| Cursed | route (`docs/13` §6); Storm Coral accent | radius, attack reduction, its aura visual, journal text |
| Berserk | route | the health-to-damage/speed curve, visual, journal text |
| Modifier look | **two rules disagree**: `STYLE.md` says new modifiers ship a skin for every stock foe; `docs/13` §6.4 says show them with an effect, since skins mean redrawing every enemy | settle one at Phase 4 |
| Which Tides get them | Riptide director as prior art | per-map and per-ascension rules (§1) |

## 6. Bug fix and online (scope "Bug Fix", "Online Compatibility")

| Asset | Exists | Missing |
| --- | --- | --- |
| F-key respawn fix | `ModKitFocus.cs` in kit 1.6, run by Argo | **never reproduced**: play Boutman, let Boutbro spawn, die, press F (`docs/15` §11 step 8); then each minion vessel's own check (Spool, Uloe) |
| Online | host-authoritative default, 2-4 players (PLAN) | the friend check (`docs/15` §11 step 11), a lobby check that everyone has the same pack version (`PACK.md` §4) |

## 7. Implied by shipping one pack

| Asset | Exists | Missing |
| --- | --- | --- |
| `Pack` step in the Build Queue | described in `PACK.md` §1 | not built (`Assets/ModKit/Editor` has none) |
| `RogueWavesMode` and `RogueWavesFoes` dev mods | named in `PACK.md` §1 | not scaffolded; grow out of RogueLab and Riptide |
| Player `README.txt` | contents listed in `PACK.md` §5 | the file |
| Roster balance across ascension | `docs/02` §7 per vessel | Phase 7 |
