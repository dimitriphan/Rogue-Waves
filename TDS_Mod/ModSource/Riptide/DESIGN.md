# Riptide: design (v0.2.3, 2026-10-05)

**RIPTIDE**, a roguelike game mode for *TDS: Tower Defense Strategy*: one Basin, thirty random Tides that keep pulling
harder, and a Salvage between them where each player picks one of three washed-up artifacts. It ends when the Basin
drowns or after Tide 30.

This is the mode's design. It follows the research report (`/mnt/project-files/reports/2026-10-04_roguelike_mode_research.md`,
section 4) and the kit's rules where they apply to a mode. Probes 1 to 5 ran on the PC on 2026-10-04 (45 of 48 checks
passed; the 3 fails were test mistakes). The Tide curve, foe arrivals and elites are fitted to the stock table they
logged. v0.2.0 wires up what the probes cleared: the Salvage end to end, Driftwood and the RIPTIDE switch; round 2
passed 64 of 67 checks on it, 0.2.1 fixed what it found, and round 3 passed all 70. 0.2.3 centres the Salvage dialog. Every number marked *tune* still needs a hand-played run.

## Identity

- **Name:** Riptide. The mode list already reads Normal, Storm, Drought, Tsunami: water and weather. A riptide is the
  current that pulls you out further than you meant to go, which is what a run does.
- **Shown as:** "RIPTIDE" in the loading screen's mode title (the game's own `Modetitle`, from
  `Modeentryobject.Display_Name`), on the Setup screen's mode line ("TYPE 3 RIPTIDE") and on the end screen.
- **Pitch line** (Setup tooltip and journal): "Random Tides, salvage between them, one Basin."
- **Lore:** "The current changes every time. Grab what floats past and hold on."
- **Tone:** the game's own words do the heavy lifting: Tide, Basin, Drowned, Breaker. Banner lines are 2-6 words.

## The run

| | |
|---|---|
| Map | The node the leader picked, played once. Map, lighting and soundtrack come from that node. |
| Length | 30 Tides in three stretches: **the Shallows** (1-10), **the Swell** (11-20), **the Deep** (21-30). |
| Breakers | Tides 10, 20 and 30 are **Breakers**: a headline foe with escorts, a bigger budget, a richer Salvage after. |
| Defeat | The Basin drowns: the game's own "BASIN DROWNED" and end screen. The run is over. |
| Victory | Tide 30 is cleared: the game's own "BASIN REMAINS". |
| Salvage | After Tides 1, 5, 10, 15, 20 and 25 (six per run). |
| Randomness | Seeded once per run on the host; the seed is logged so a run can be replayed. |
| Records | Each map's Riptide twin keeps its own best Tide and clears; Riptide never touches the stock map's records or medals. |
| Saved between runs | Nothing else in v1. |

The Tide table is a fixed 60 entries: a break, then a Tide, thirty times. Breaks keep the stock break length (the
first one is the long opening build time); Tides keep the stock Tide timer for the same Tide number, repeating the
stock's last one past its end (*tune*).

## Tide budget curve

Each Tide gets a threat budget, counted in **Daggers** (a foe's Health ÷ 10), and the director buys foes with it.
Round 1 on the PC logged the stock Mode_Normal table and every foe's stats. Counted the same way, with elites at their
Health factor, the stock table goes 5 at Tide 1, ~45 at Tide 5, ~250 at Tide 10, ~1200 at Tide 20 and ~8000 at its
finale, with a few breather Tides between. Riptide's curve follows it:

```
budget(t) = (5 + 0.92 × t^2.4) × players(n) × (1.6 on a Breaker)
players(n) = 1 + 0.35 (n - 1)
```

| Tide | 1 | 2 | 5 | 8 | 10 (Breaker) | 15 | 20 (Breaker) | 25 | 30 (Breaker) |
|---|---|---|---|---|---|---|---|---|---|
| Budget, solo | 6 | 10 | 49 | 140 | 378 | 617 | 1960 | 2089 | 5172 |
| Stock table | 5 | 12 | 44 | 135 | 270 | 400 | 1197 | 2100 | 8260 |

- **Threat per foe** (*tune*): Dagger 1, Spear 1.2 (fast), Aegis 8, Ballista 6 (reach 5), Maul 70, Onager 12
  (reach 9), Claymore 2 (very fast), Excalibur 1000, Bulwark 3000. A foe missing from the table costs Health ÷ 10.
- The curve sits on the stock one rather than above it: Salvage makes the team stronger, but Riptide's Tides are
  random, so a bad draw already makes a run harder than the stock map.
- Player scaling is 0.35 per extra player because more players also means more vessels and more aura (*tune* in a
  co-op playtest).

## Foes, packs and elites

**New foes arrive at set Tides**, close to the stock table (Spears at 4, Aegis at 7, a first Maul at 10, Ballistae at
11, Onagers at 21), a little earlier since a run is random. Each is announced on the banner the first time it shows up:

| Tide | 1 | 3 | 6 | 8 | 10 | 14 | 18 | 30 (bosses) |
|---|---|---|---|---|---|---|---|---|
| Foe | Dagger | Spear | Aegis | Maul | Ballista | Claymore | Onager | Excalibur or Bulwark |
| Banner | "Daggers ahead" | "Spears ahead" | "Aegis ahead" | "Mauls ahead" | "Ballistae ahead" | "Claymores ahead" | "Onagers ahead" | "Last Tide, hold fast" |

**A Tide is 1-4 packs**: 1-2 in the Shallows, 2-3 in the Swell, 3-4 in the Deep, with a 4 s pause between packs. Each
pack is one foe type, sent Spread over the enemy cores, worth an even share of what is left of the budget. The foe is
one whose count for that share lands between **3 and 50**, the newest of those twice as likely as each other. So later
Tides bring heavier foes, not endless Daggers, and a Tide tops out near the stock table's 120 foes. A pack spreads
over about 6 s (0.1-1 s apart, like the stock table); Mauls and heavier walk in up to 4 s apart.

**Elites** are the game's own modifiers (`Wavecontent.Upgrades`), arriving when the stock table first uses them:

| Elite | Upgrade | From Tide | What it does (logged) | Cost |
|---|---|---|---|---|
| Hardened | Enemy_Health | 6 | Health ×4 | ×4 |
| Tempered | Enemy_Red | 9 | Health ×2, Damage ×2, longer aggro | ×2 |
| Annealed | Enemy_Armor | 11 | armour | ×2 |
| Superheated | Enemy_Hyper | 14 | Health ×3, Damage ×3, faster | ×3.5 |
| Molten | Enemy_Explode | 16 | Health ×4, bursts when drowned | ×4 |
| Fractured | Enemy_Chaos | 18 | Health ×15, Damage ×10, very fast | ×15 |

The factors assume the game adds an upgrade's `_Multi` to a base of 1 (Hardened's `Health_Multi` 3 makes ×4); round 2
logs a Hardened Dagger's live Health to confirm. A pack is elite with a 20% chance at Tide 6, +3% a Tide, up to 70%.
Its cost multiplies, so an elite pack is fewer, tougher foes rather than extra threat. The first elite pack of each kind
gets the banner: "Hardened foes ahead".

**Breakers** spend 40% of their budget on a headline pack: one to three of the strongest foe so far (two Mauls at Tide
10), elite from Tide 20. At Tide 30 the headline is a boss, an Excalibur (the stock table's finale) or a Bulwark, elite
only if it still leaves room for escorts. The rest are escorts. Banner: "A Breaker rolls in".

**Other banner lines:** "The Shallows" (Tide 1), "The Swell rises" (11), "The Deep calls" (21), "Last Tide, hold fast"
(30). One line per Tide; a new foe outranks a stretch line, and a Breaker outranks both.

A sample solo run from the planner (seed 1): Tide 1, 5 Daggers; Tide 5, 24 Daggers and 20 Spears; Tide 10, 2 Mauls and
39 Ballistae; Tide 16, 22 Annealed Aegis and 11 Molten Aegis; Tide 23, 3 Tempered Mauls, 26 Tempered Aegis, 36 Onagers
and 29 Fractured Daggers; Tide 30, a Bulwark, 33 Hardened Aegis and 7 Tempered Mauls.

## Salvage (the pick between Tides)

- **When:** in the break after Tides 1, 5, 10, 15, 20 and 25. Never before Tide 1: the first one comes after it.
  Everyone sees "SALVAGE WASHED ASHORE", and each player's dialog opens by itself.
- **Who picks:** every player, from their own three. No votes.
- **What is offered:** artifacts from the whitelist (below) that the team doesn't hold, none offered twice in the same
  Salvage. After a Breaker each player sees **four**.
- **Effect:** the artifact joins the run for the whole team, exactly as if it had been brought from Setup: its
  upgrades go on the Basin core, it joins the HUD's artifact row on every screen, and everyone sees a warning with its
  icon: "CAFFEINE SALVAGED".
- **Clock:** the break's countdown stops while anyone is still choosing, for at most 30 s. Then the host picks at random
  from each remaining player's offers. Nobody can stall the team.
- **Running dry:** if the whitelist can't fill an offer, one **Driftwood** card fills the gap: whoever picks it gets
  aura of every colour (20 + 4 per Tide of each, *tune*), and everyone sees "DRIFTWOOD SALVAGED". It is the only
  mod-made reward in v1 and only a filler. An artifact that fails to grant also turns into Driftwood.
- **Lobby artifacts** brought from Setup still work and are never offered again.

### The reward pool

The 40 shipped artifacts, filtered by probe 3 and a hand-played run. The probe logs every artifact's core upgrades and
the effect script each one uses, then grants a few of each kind mid-match. What the code says about each kind:

| Kind (script on the core upgrade) | Mid-match grant | Plan |
|---|---|---|
| `Entity_Ai_Artifact_Coin` and other "every Tide" effects (`Newwave`) | Works: it hooks the Tide counter when applied, pays its start bonus at once | whitelist |
| `Entity_Ai_Artifact_Coreapply` | Works: applies to the cores when granted | whitelist |
| `Entity_Ai_Artifact_Towerapply`, `Mobapply` | Only towers and mobs spawned afterwards get it | whitelist with **catch-up**: the director applies the same upgrades to what already stands |
| `Entity_Ai_Artifact_Teamapply` | Same, for the players' towers and units | catch-up |
| `Entity_Ai_Artifact_Heroapply` | Only applies when a vessel respawns at the core | catch-up on the living vessels |
| Effects that read `Totalwaves` (Coin, Caffeine, Opalcore, Moneybag, Voice) | Scale by `30 / Totalwaves` in whole numbers | Riptide's table has `Totalwaves` = 30, so the factor is 1, as on a stock map |
| Anything that only acts at match start, needs a picker, or throws | | left out |

Known to need a look: the Sandbox (a test tool, never offered), Blurryphoto (adds other artifacts), Pig (reads the lobby
list), Checklist and Computer (their own UI).

Round 1 logged every artifact's kind. None of the 40 uses the Teamapply or Mobapply kinds. Four are Coin, four
Coreapply, seven Heroapply, two Towerapply and four spawn mobs (`Entity_Ai_General_Spawnmobs`: Badge, Bird, Fish Bait,
Kippy). Five build something once (Converter, Cubed Drink, Sandbox, Seeds, Tacklebox), and fifteen have a script of
their own. A mid-match grant of Fragment, Rusty Pipe, Beast Tassle, Badge and Bird logged no errors. Each artifact also
has a lobby slot colour (Red, Blue, Yellow, Green); Salvage ignores slots, so a run can hold several of one colour.

**The cards:** the game's dialog shows each card as an icon on a coloured tile (the pause menu's Exit and Back), with no
name on it. A Salvage card takes the artifact's slot colour, and hovering it puts "AURA GAVEL ▪ " and the artifact's
description in the dialog's sub line, the way the unlock popup names an artifact. The dialog was made for the pause menu's
two-card prompts: it sits in the bottom-right corner with its cards two to a row, so a Salvage's three cards sat far off
centre, two plus one (Dimitri, 2026-10-05). While a Salvage is open, Riptide lays the cards out in one row (up to four)
and moves the dialog's title and cards to the middle of the screen; after the pick it puts the dialog back for the pause
menu.

## What the player sees

| Moment | What is on screen | Whose |
|---|---|---|
| Setup | A **RIPTIDE** switch beside the alternate-map toggle (a copy of it with Riptide's crate-on-a-wave icon), the host's to flip; the mode line reads "TYPE 3 RIPTIDE" for everyone | mod (switch and line), game (toggle) |
| Loading | The mode title "RIPTIDE" | game |
| Each Tide | "TIDE 12" and the director's banner line ("Onagers ahead") | game banner, mod text |
| Enemy cores | The Tides-left counter, from 30 | game |
| Salvage | The game's choice dialog, titled "SALVAGE", "Pick one, the Tide waits", one card per offer: the artifact's icon on its slot colour; hovering a card shows its name and description | game dialog, mod content |
| | A **SALVAGE** button on the vessel's bar (key 8) that reopens the dialog, greyed when there is nothing to pick | mod command |
| After a pick | "CAFFEINE SALVAGED" with the artifact's icon, to everyone; the HUD artifact row grows | game warning and row |
| Defeat | "BASIN DROWNED", the core-destroyed camera shot, the end screen with "RIPTIDE" | game (the shot is replayed by the mod: the game plays it only for stock mode names) |
| Victory | "BASIN REMAINS" | game |
| Records | The end screen saves the run under the map's Riptide twin (its own best Tide and clears, no medals) | game, mod twin |
| Journal | A Riptide page: how a run goes, the stretches, foe arrivals, Salvage | mod (kit journal) |

Everything a player relies on comes from something every machine already has: the synced mode name, the banner, the
artifact list, a command on their own vessel, the game's warnings.

## Co-op

- **The host decides everything.** The director, the seed, the Tide table and all picks live on the host. Clients never
  read the Tide table, so its randomness can't desync them.
- **Mode switch:** the synced `Tds_Multiplayerinfo.Mode` and `Gameplan` are themselves the state. The host writes them;
  every client's Setup screen and loader read them, and every machine built the same twins at boot.
- **Offers and picks:** a SALVAGE command on each player's vessel. The host writes that player's offers into the
  command's `Goalstring` as indices into the artifact list every machine builds the same way ("12.30.4", "d" for
  Driftwood, to fit its 61 bytes) and the open Salvage's Tide into `Goalvalue` (`Updatecommand(..., false, false)`).
  The client opens the dialog from those and sends `Command_ServerRpc("Riptide_Salvage", {"Pick":n})`. The game only
  passes it on from the vessel's owner; the host checks the phase and the index. `Commandlist` is synced, so a player
  who joins mid-Salvage still sees their offers. When the host picks for a player (time ran out), their command
  empties and their open dialog closes.
- **Salvaged artifacts:** the host appends each pick to the synced lobby list `Tds_Multiplayerinfo.Artifacts`. Every
  machine (the host included) adds what is new there to its own HUD row. A NetworkList replays to late joiners, so no
  new sync channel is needed. When the players are back on the world map the host puts the lobby list back as it was.
- **Per-player state** (offers, picks) is keyed by session id. Shared state (budget, Tide, held artifacts) is the
  host's.
- Riptide adds no networked prefab and no named messages in v1. Named messages are probed (probe 5) for later.

## How it plugs into the game

- **Registered like a vessel** (kit `04_Content` pattern): at boot every machine clones Mode_Normal's `Modeentryobject`,
  `Modeobject` and `Wavesobject` into `Mode_Riptide`, pre-sizes the table to 60 entries, registers the entry in
  `Tds_Masterdatabase` folder "Mode" and the mode in its own `Bl_Address` group (`Loadedfolders["Mode_Riptide"]`).
  The build is deterministic: the placeholder table repeats the stock Tides in order, the same on every machine.
- **Runs on the stock `Mode_Normal`** (a mod can't supply a mode class). The director steers it from the outside.
- **The switch:** on the world map, while RIPTIDE is on, the host turns a chosen node's `Mode_Normal` into
  `Mode_Riptide` after every map pick. Storm, Drought, Tsunami, Final and the Tutorial are left alone in v1.
- **Gameplan twins:** at boot every machine also clones each `Mode_Normal` Gameplan into `Riptide_<id>` (same map,
  soundtrack and intro; mode `Mode_Riptide`; no medals; `Record` off) in the master database's Gameplan folder. The
  switch swaps the synced Gameplan name to its twin as well as the mode. The end screen saves a match's `Clear_`,
  `Wave_`, `Time_` and `Lose_` records and its medals under the Gameplan's name, so a run lands in the twin's records
  and can't unlock the stock map's medals. Unlocks tied to one stock map don't count Riptide runs; unlocks for "Any"
  map still do.
- **The director** (host only, run state in statics): writes each Tide into the table at the start of the break before
  it (`Mode_Normal` reads each entry live, and only on the host), holds the break's clock during a Salvage, grants
  picks, and records the end of the run.
- **Artifacts** are loaded at boot on every machine (all 40 groups; asset groups are never unloaded), so a pick shows at
  once everywhere.

## Changes from the research report, and why

| Report | Riptide | Why |
|---|---|---|
| A pick between Tides | Six Salvages a run | There are only 40 artifacts and each is run-shaping. Four players picking every Tide would empty the pool by Tide 10. |
| Show picks through "a synced signal" | The synced lobby artifact list | It already replays to late joiners and every machine already loads artifacts from it. |
| Grant = apply `Coreupgrades` | Grant = apply them, plus catch-up for the apply-kinds | The game's tower, team, mob and vessel kinds only reach things spawned after them. |
| Pre-size to "for example 40" | 60 entries, `Totalwaves` = 30 | Break + Tide per Tide; artifacts divide 30 by `Totalwaves` in whole numbers, so more than 30 Tides would switch some off. |
| Defeat for free | Defeat for free, plus the core-destroyed camera shot | `Mode_Normal` plays that shot only for the stock mode names. |
| Swap the mode (open question 7.2) | Swap the mode and the Gameplan, to a Riptide twin of the map | With the mode alone, the end screen would save the run as the stock map's best Tide and clear, and award its medals. |

## Not in v1

Meta-progression, multi-map runs, mod-made rewards beyond Driftwood, the richer overworld "Run" node, a run summary
screen (v1 logs the summary and shows it as the last banner line), Riptide for Storm, Drought and Tsunami nodes.

## Defaults picked where the ask forked (for Dimitri to veto)

1. A single long match on one map with a host-only switch (report 4.2, simple option).
2. Every player picks their own each Salvage, 30 s timer, random auto-pick.
3. First Salvage after Tide 1, then after 5, 10, 15, 20, 25.
4. Rewards are whitelisted artifacts only, Driftwood only as a filler.
5. 30 Tides, Breakers at 10, 20, 30.
6. Lobby artifacts stay allowed.
7. Player scaling 0.35 per extra player.
8. The name Riptide, the stretches Shallows, Swell and Deep, the word Salvage.

## Test plan

**Probes first** (report section 6, the debug build's self-test, each check one `TEST PASS|FAIL` line):

1. **Mode registration and a run.** The mode and the Gameplan twins are registered; the switch turns a picked node
   into its Riptide twin; the match loads `Mode_Riptide` on `Mode_Normal`; Tides advance with Riptide's banner lines.
   Logs the stock table and the foes' stats for tuning. The ending (a drowned Basin ends the run) is checked last because it ends the match.
2. **Live Tide rewrite.** The director writes the next Tide during a break; exactly the written foes and elites spawn.
3. **Artifacts mid-match.** Logs all 40 artifacts' kinds, grants one of each kind, checks the core upgrades, the
   catch-up on a standing tower and the vessel, the HUD row, and that the lobby list goes back afterwards.
4. **The choice dialog.** `Bla_Ui_Confirmation.Runprompt` from mod code with three real offers: it opens, shows three
   cards, and returns the clicked one.
5. **The sandbox** (separate define, since a refused API would refuse the whole mod): `PlayerPrefs`, named messages,
   `OnClientConnectedCallback`.

**Round 1** (2026-10-04, solo on The Hunt): 45 of 48 passed. The 3 fails were test mistakes (the dialog's list keeps a
layout child, so the test counted 4 cards and clicked the first offer; the named-message buffer was 2 bytes short).
Every API passed the game's code security. A drowned Basin then restarted the run every frame (fixed in 0.1.1).

**Round 2** (v0.2.0) adds two checks to the same self-test:

- **switch:** the RIPTIDE switch is on the Setup screen, a click turns it on, and the line reads "TYPE n RIPTIDE".
- **salvage:** a Salvage end to end: three offers on the vessel's command, the dialog opening by itself, a click
  reaching the host and granting the pick, the break's clock held meanwhile, the button greying; then a Salvage left
  to time out with only Driftwood on offer: the host picks it, the aura arrives and the dialog closes.

**Round 2** (v0.2.0, 2026-10-04): 64 of 67 passed. Two fails were the game's own cancelled tasks counted as errors,
and one was a check that looked for the dialog's cards instead of its menu; none was a bug in the Salvage. The SALVAGE
button never showed in a shot, so 0.2.1 adds checks that select the vessel, find the button and its key on the bar,
and press it to reopen the dialog.

**Round 3** (v0.2.1, 2026-10-04): 70 of 70 passed, with no errors.

**Round 4** (v0.2.3) changes two checks and adds one: probe 4's dialog must sit in the middle of the screen with its
cards in one row and go back to the game's own place after the pick, and the Salvage end to end must open it that way
by itself.

**Then:** a solo run to Tide 30 by hand, and probe 6, a two-player online run with a friend joining mid-run, with both
Player.log files.
