# Scope review (2026-10-10)

Dimitri asked (2026-10-10 22:36Z) for a look through the current text files and scopes, pointing out anything that
might need improvement or reconsideration. This page reads the 15 files in `docs/scope/` against each other, against
the settled decisions (`docs/PLAN.md` Part C, `docs/SCOPE_VS_LOOP.md`, `docs/MAIN_MENU_START.md`, `docs/SEA_CHART.md`,
`docs/MECHANICS_CHECK.md`) and against the engine facts in the kit (`docs/NN` = `TDS_Vessel_Creator/docs/`). **No scope
file was changed**: Dimitri decides and sends back the files he edits.

Not reopened: the 10-map campaign saved between maps and keyed by Steam id, 3 retries, the main-menu start, the sea
chart with the vote in the map (V1), VENDCINT selling Cardinals and ranks only, 2 Pure Aura per bonus tide, the first
Fund Transfer cap of 50, Depth on hold, no sonar, Argo's numbers. The "Sea chart and sea weather build" thread added
Sea Weather to `05` and `02` (PR #21, merged); nothing here duplicates that edit, and its numbers are its own NEEDS line.

The repository copy, the project files copy and the PC copy (`Desktop\RogueWave\scope`) are all equal today (same
sizes, checked 2026-10-10).

**Call:** **Dimitri** = only he should decide (a default is given); **default** = a reasonable default, his to veto;
**fix** = stale or unclear wording, no decision needed (Claude folds it in when he says so, or when he next sends the
file back).

## The short list: decisions that shape the loop

| # | Question | Default offered |
| --- | --- | --- |
| 12 | Losing during a **bonus tide**: is that a lost map (a retry spent, the map's Pure Aura gone)? | No: a map is won once Tide 15 is cleared; dying later ends it as a win and only the unfinished tide pays nothing |
| 14 | Between maps the party is back on the **boat, the game's lobby**: vessel and lobby Cardinal pickers are live again | The mod restores each saved vessel and locks lobby Cardinals after map 1 |
| 23 | **Yossele's Fertilize** exists only through a Direction, but the pack's vessels must also work outside the mode | Fertilize works from the start; Compost Bin makes fertilizer come faster |
| 10 | **Whose quit** burns a retry? | Only the host leaving (or the whole team); a friend who drops rejoins on the boat |
| 16 | A stock break is **30 s**, and players must build, shop, transfer and take objectives in it | Mode breaks of 45 s, or hold the clock until everyone presses Ready |
| 49-50 | Objective **pay for "All players"** and **how many** are offered per break | Each player gets the full pay; VENDCINT offers 2, one active at a time |
| 40-41 | **Team Cardinals** paid by one player's Pure Aura; **Vampire Dentures** stacking without limit | Any one player pays; stacks capped at 3 |
| 20 | **Rank prices** for Directions (the open `! NEEDS !`) | Rank II 15, rank III 25 Pure Aura, one rule for all |

## By file

### 00_Read_Me.txt

| # | Finding | Recommend | Call |
| --- | --- | --- | --- |
| 1 | The Formatting legend lacks `#` (a point) and `\| Heading \|` (the file's title), which every file uses | Add both lines | fix |
| 2 | An older `roguewave.txt` (18 KB, 2026-10-09) still sits in PC `Desktop\RogueWave` beside the `scope` folder; editing it by mistake would be lost work | Rename it `roguewave_OLD.txt` or delete it, and name the PC copy in line 5 | Dimitri |

### 01_Expansion.txt

| # | Finding | Recommend | Call |
| --- | --- | --- | --- |
| 3 | "2 to 4 players": can one player run a campaign? Every PC test so far was solo, and a majority vote works with one | "1 to 4 players" | Dimitri |
| 4 | The list misses the features players will notice first: the sea chart, VENDCINT, optional objectives, the campaign save | One line each | fix |

### 02_Progress.txt (Claude keeps this one)

| # | Finding | Recommend | Call |
| --- | --- | --- | --- |
| 5 | Line 9 says mechanics the game must answer "go to a PC test round"; they went: RogueLab rounds 4-6 proved every mechanic and the main-menu button (round 6, 22 of 22) | Update the line | fix |
| 6 | Missing: the deep code dive and `docs/MECHANIC_IDEAS.md` (waiting on his picks), and this review | One line each (the Sea Weather line of 2026-10-10 is already in) | fix |

### 03_Bug_Fix.txt

| # | Finding | Recommend | Call |
| --- | --- | --- | --- |
| 7 | "Could apply to new vessels": Spool's Thread Head is a controllable minion that "works like Boutman's fish", the exact case of the bug. And the game's Mods folder is empty, so the hand check needs the next dev build | Name Thread Head in the line; the check rides with the next PC build that carries the shared code | default |

### 04_Rogue_Wave_Mode.txt

| # | Finding | Recommend | Call |
| --- | --- | --- | --- |
| 8 | Line 2 says upgrades are "purchasable with a new currency found in game from doing objectives". Pure Aura mostly comes from tides (3 each), Directions are free picks, the shop sells ranks | "earned each tide and from optional objectives"; "Directions picked before each map, ranks bought at VENDCINT" | fix |
| 9 | "Each map is harder" has no mechanism in the scope. The defaults in `SCOPE_VS_LOOP.md` never landed: #4 Riptide's seeded Tide director, each map deeper on its curve; #5 Tides scaled ×(1 + 0.35 per extra player); #20 the stretch shape | Add them as three lines | default |
| 10 | "Quitting during a map counts as losing it." A dropped friend doesn't end a map (the match goes on, `docs/15` §14.5), so only the host's quit truly ends it. If a friend's crash also spends a team retry, one bad connection costs everyone | Only the host leaving, or the whole team, spends a retry; a friend who drops sits out the map and rejoins on the boat | Dimitri |
| 11 | A retry replays "from the last save", which is before the map's opening break, where Directions are picked. If the three offers are drawn fresh, losing a map rerolls them | Seed each player's offers per map, so a retry shows the same three (and the same objectives) | default |
| 12 | After Tide 15 the team may stay for bonus tides at 2 Pure Aura each. If dying in one is a lost map, the retry and the whole map's Pure Aura (3 × 15 and more) are at stake for 2 a tide, so nobody would stay | A map counts as won once Tide 15 is cleared; dying in a bonus tide ends it as a win, and only the tide in progress pays nothing | Dimitri |
| 13 | `! NEEDS WHICH MODIFIERS AND ENEMIES CAN CHANGE !` | Sea Weather (now in `05`, merged in PR #21) answers part of it for maps; enemy pools per campaign are still open | - |

### 05_Gameplay_Rundown.txt

| # | Finding | Recommend | Call |
| --- | --- | --- | --- |
| 14 | Between maps the party returns to the boat, which is the game's lobby (`docs/MAIN_MENU_START.md` §1). Its vessel picker and Cardinal slots work there, and lobby Cardinals load at every match start (`docs/15` §7). Without a rule, a player can change vessel every map, and swap stock Cardinals for free instead of buying them | The mod puts each player's saved vessel back, and lobby Cardinals lock after map 1. The alternative: no lobby Cardinals in the mode at all, every Cardinal comes from VENDCINT | Dimitri |
| 15 | "The 5th and 10th rows close a stretch": the scope (with PR #21's line) says the hardest map closes each stretch, but not what else a closing map does. The base game has no boss (`docs/15` §18.3), so a boss is new enemy work | `SCOPE_VS_LOOP.md` #20's default: a Breaker Tide closes the map and VENDCINT carries more stock | Dimitri |
| 16 | A stock break is 30 s (the opening break 45 s). In it each player builds, clicks VENDCINT, shops, sends aura and takes an objective. The scope sets no break length; `SCOPE_VS_LOOP.md` #8 (hold the clock at shop breaks) never landed | Mode breaks of 45 s, or the clock waits until every player presses Ready (with a cap) | Dimitri |
| 17 | Three wordings for the same moment: "reaching Tide 15" (line 2), "clear Tide 15" (line 2, end), "after Tide 15" (line 10). Also "ascension level" for Depth level, "waves" for tides | "after clearing Tide 15" everywhere; "Depth level"; "tides" | fix |
| 18 | `! NEEDS LIST OF OPTIONAL OBJECTIVES !` | `docs/MECHANIC_IDEAS.md` §3 has six that fit the template (Raze the Nest, Bag the Beast, Clean Hull, Beat the Clock, Light Purse, Steady Hands) | Dimitri (pick) |

### 06_New_Mechanics.txt

| # | Finding | Recommend | Call |
| --- | --- | --- | --- |
| 19 | What fills the three offered Directions isn't said: which pools, and whether rank-ups can fill all three | Three different Directions from Any plus the vessel's own list; a held one shows only as its next rank; a rank III never comes back | default |
| 20 | `! NEEDS PRICES FOR DIRECTION RANKS !` needs an anchor. A player earns about 45 Pure Aura a map (15 tides × 3), plus 1 a tide on a weather map, 2 per bonus tide and objectives, so roughly 450-600 a campaign; Vampire Dentures costs 15 | One rule for every Direction: rank II 15, rank III 25 | Dimitri |
| 21 | VENDCINT comes "at the start of every break between tides", but Fund Transfer allows 50 "before Tide 1", so it must also come in the opening break | "every break, the opening one included" | fix |
| 22 | Must a vessel walk to VENDCINT? The game doesn't check distance (`docs/MECHANICS_CHECK.md` §2): a player clicks it and shops from anywhere on the map | Within a few tiles, since its drop-in is the point; or anywhere, if the walk only costs break time | Dimitri |

### 07_Vessel_Yossele.txt

| # | Finding | Recommend | Call |
| --- | --- | --- | --- |
| 23 | Fertilize "is only unlocked if a Direction is picked that includes it". Directions exist only in the mode, and the pack's vessels must play as normal vessels outside it (`docs/PACK.md` §2 rule 1), so in a normal game Yossele's ability never shows. In the mode it shows only if Compost Bin turns up in a random three | Fertilize works from the start with a slow base fertilizer rule; Compost Bin makes fertilizer come faster | Dimitri |
| 24 | Growth "resets if the tower is destroyed": it also resets at every new map, since towers don't carry over (`SCOPE_VS_LOOP.md` #1, #13) | Add "and at the start of each map" | fix |
| 25 | Aura fruit: "any vessel or minion can pick up". The game pays the picker's owner (`docs/15` §14.7), so a teammate takes Yossele's aura | Decide: the picker keeps it (a way to share), or it always pays Yossele's player | Dimitri |
| 26 | Overgrown Stonehenge spawns "a weak vine wall in front ... if possible" each tide. The game never checks that a path stays open (`docs/16` §2), so an automatic wall could seal a lane | The vine wall spawns only off the enemy path, or foes walk through it slowly | default |
| 27 | Deep Roots (example) rank III "start fully grown" removes the slow-growth play. The number of Growth stages isn't stated anywhere | Stage count at the design gate; if Deep Roots is adopted, rank III stops a stage short of full | default |

### 08_Vessel_Spool.txt

| # | Finding | Recommend | Call |
| --- | --- | --- | --- |
| 28 | No unit cap. Four Spools (or Uloes) multiply units past the 200 the host was measured at (`docs/15` §15); `SCOPE_VS_LOOP.md` #15 proposed 50 per player, not folded in | Add a cap line for Crafted (and Photons in `10`) | default |
| 29 | Unthread "removes one of Spool's minions": Thread Head included? It holds the fourth building place | Unthread works on Crafted only | default |
| 30 | Line 28 writes `[Spool]`: brackets mean a mechanic in the legend | `Spool` | fix |

### 09_Vessel_Argo.txt

| # | Finding | Recommend | Call |
| --- | --- | --- | --- |
| 31 | Matches Argo 2.0's `DESIGN.md`. Its only open question elsewhere: do the Nauts (Port and Starboard) count as minions for Made of Tougher Stuff (#36) | Answered with #36 | - |

### 10_Vessel_Uloe.txt

| # | Finding | Recommend | Call |
| --- | --- | --- | --- |
| 32 | Status says "a check that the game can measure light comes before its design". Answered: the game has no gameplay light, and the mod sums each tower's light from sources in range (`docs/15` §14.7) | Drop the check from the line | fix |
| 33 | Flood Light is the Economy building, but no line says it earns aura | Say whether it mines the node it plugs into (as Loom and Pachira do) | Dimitri |
| 34 | Dazzling foes "Illuminate" player towers (they take more damage), while Uloe's towers want "Light". Two different things, close words | Keep Illuminate as the debuff only; Dazzling never gives Light | default |

### 11_Directions.txt

| # | Finding | Recommend | Call |
| --- | --- | --- | --- |
| 35 | Count against the target (about 7 Any, 4 per new vessel): Any 1, Argo 0, Yossele 3 (plus the Deep Roots example), Spool 2, Uloe 0, base vessels none. The one Any needs minions, so a vessel without minions has no Any Direction yet | `docs/MECHANIC_IDEAS.md` §4 has six Any and five base-vessel Directions to pick from | Dimitri (pick) |
| 36 | "Vessel: Any Vessel with (Minions)" isn't a template value, and Clashes is used to say who can't take it. "Minions" isn't defined: Crafted, Thread Head, Photons, Nauts, Boutman's fish, Fie's regulars? | Add a `- Needs:` line to the template (e.g. "a vessel with minions"), and list which units count | Dimitri |
| 37 | `docs/STYLE.md` says a Direction is "named like a heading or a course". Only Deep Roots fits; Compost Bin, Sewing Machine, Mother of Mushrooms, Efficient Recycler, Dandelion Season, Made of Tougher Stuff don't, and they read well | Relax the style rule to his naming (plain names that match the Look line) | Dimitri |
| 38 | Three Directions replace a building (Dandelion Season, Mother of Mushrooms, Sewing Machine), and each lists the others by hand in Clashes | A `- Replaces:` line in the template; two Directions that replace the same building never go together | default |
| 39 | Base vessels "get their own Directions", with no count. A Shift on a stock vessel is new code per vessel; a Boost is cheap (`docs/MECHANICS_CHECK.md` §2) | Two Boosts per base vessel to start | Dimitri |

### 12_Cardinals.txt

| # | Finding | Recommend | Call |
| --- | --- | --- | --- |
| 40 | A Team Cardinal helps everyone but is paid from one player's Pure Aura, which can't be sent | Any one player pays, or the price is split among those who chip in | Dimitri |
| 41 | Vampire Dentures stacks without limit at 15; with about 500 Pure Aura a campaign, a player could hold 20 or more | Cap at 3 stacks, or the price rises per copy | Dimitri |
| 42 | `! NEEDS WHICH BASE GAME CARDINALS THE SHOP ALSO SELLS !` | `SCOPE_VS_LOOP.md` #7: all but Moon Core, Looped Mirror, Vintage Watch, Same Day Shipping, Money Bag (it duplicates Fund Transfer), Sand Filled Box, Blurry Photo, Checklist, Computer. Bottom Rock only with the transfer guard (it zeroes the spend multiplier, `docs/MECHANIC_IDEAS.md` §7) | default |
| 43 | Every Basin already survives its first killing blow each map (`docs/15` §18.3) | A "survive a killing blow" Cardinal adds one more save, it doesn't create the first | default |
| 44 | Line 6 counts "lobby picks" in the 8 Team slots; whether lobby Cardinals exist in the mode at all is #14 | Follows #14 | - |
| 45 | Wording: "Your vessel attacks heal themselves"; Cracked Buoy's "towers start every map with more health" (towers are built during the map) | "Your vessel heals for part of the damage it deals"; "Your towers have more health" | fix |

### 13_Enemy_Modifiers.txt

| # | Finding | Recommend | Call |
| --- | --- | --- | --- |
| 46 | "More enemy modifiers" each map: can one foe carry two new modifiers, and one on top of a stock elite (Hardened, Molten)? | Up to one new modifier per foe on maps 1-5, two on maps 6-10, stock elites on top | Dimitri |
| 47 | Do the new modifiers appear outside the mode? | Mode only, as the pack's vessels are normal vessels outside it | default |
| 48 | Aging grows the longer a foe lives, and Argo's slows and pins keep foes alive longer: a crowd-control vessel meets the strongest Aging | A note for the Phase 4 balance pass | - |

### 14_Optional_Objectives.txt

| # | Finding | Recommend | Call |
| --- | --- | --- | --- |
| 49 | Mind the Meteor is "Taken by: All players" for 3 Pure Aura, but the template says a team splits the pay: 3 split four ways is under 1 each | Each player on the team gets the full pay; or price team objectives per player | Dimitri |
| 50 | Every objective is "Offered: every break", with no limit on how many run at once. A meteor every break could add up to 45 a map, as much as the tides | VENDCINT offers 2 per break; one active objective per player (and one team one) at a time | Dimitri |
| 51 | Who accepts a team objective? | Any player takes it for the team | default |
| 52 | "Kind: Mining" is not in the template's kinds. And a stock mine never runs out, so "fully mine" needs the one-shot node that dies when emptied (`docs/MECHANIC_IDEAS.md` §3) | Add Mining to the kinds; the one-shot node is for its design gate | fix |

## Other text files that are now stale

| # | File | Stale text | Recommend | Call |
| --- | --- | --- | --- | --- |
| 53 | `docs/SCOPE_GAPS.md` (2026-10-08) | still lists the mode's start Cardinal, a vote of 3 random maps, the shopkeeper's "who and where", the Fund cap, Thread Head "building or minion", "no upgrade ideas" for Mushroom Cannon and Mirror; `CLAUDE.md` sends readers to it "before picking the next piece of work" | Refresh it, or a banner pointing to the scope and this page | fix |
| 54 | `docs/SCOPE_VS_LOOP.md` | #3 makes Tide 30 of map 10 the campaign's last (the scope: bonus tides never end); #6 pools Pure Aura through Fund Transfer (decided: base aura only); #9, #17 and §B6 use a start Cardinal (replaced by the main-menu button); #17 resumes by chaining maps with no staging map (the sea chart returns to the boat) | A dated note at the top naming the superseded findings | fix |
| 55 | `docs/STYLE.md` | lines 34 and 45 say no Cardinal is named yet (Vampire Dentures is); the Direction naming rule (#37) | Update with #37's answer | fix |
| 56 | The project's instructions (Project settings) | "the user's scope is `docs/SCOPE_v0.txt`", which is now a pointer to `docs/scope/` | Point them at `docs/scope/00_Read_Me.txt` | Dimitri |
| 57 | `docs/EXPANSION_SCOPE.md` row 3, `docs/PLAN.md` Decisions (the 2026-10-09 "15 maps, 10 Any and 6" line), kit `docs/15` §1 and §3 ("three maps") | answered or superseded: the menu button exists; 10 maps, 7 and 4; the three-map chain was a test | Mark superseded where a reader could take it as current | fix |

## What happens next

Dimitri answers what he wants in the scope files and sends back the ones he changes; each answer is folded in by the
rule in `docs/PLAN.md` Part A. The **fix** rows can be folded in on his word without further questions. The
**default** rows stand unless he vetoes them, and join the matching scope file at its next edit or design gate.
