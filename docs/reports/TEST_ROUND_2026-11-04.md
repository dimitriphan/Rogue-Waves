# Test round, 2026-11-04: every feature that had not been tested

Dimitri, in the project: "Develop upgrades for Uloe until they're fully fleshed out. Afterwards, make sure to test all untested
features of the mod." Uloe's upgrades are a design thread of their own. This report is the second half.

Everything here ran on his PC in TDS itself. Logs are in `handoff\twoplayer_logs` and `handoff\selftest_logs`, and captures in the
scratchpad of the run. None of it is committed, because they hold base-game art.

**In short:** the online half of the mod had never run, and it had a bug that would have broken every rejoin. It now passes with a real
second player. Ten bugs were found and fixed, and five of them would have been seen by any player. What is left needs a person: Steam's Join
Game, a real network, his eye and his Pin Cushion wall (section 5).

## 1. Results in game

| What ran | Result | Log |
| --- | --- | --- |
| Two players, the whole round: host and a real second player | Host **61 pass, 0 fail**. Second player, across five launches: **83 pass, 0 fail**. No setup failures | `host_20261104_061012`, `second_20261104_061012_*` |
| Four vessel pairs on a client: Spool and Argo, Uloe and Spool, Yossele and Yossele, Argo and Uloe | Each pair: host **36 pass, 0 fail**, second **64 pass, 0 fail** | `host_20261104_063429`, `_064025`, `_064616`, `_065203` |
| Spool's self-test, with the Direction checks new here | **133 pass, 0 fail** | `Spool_20261104_071853` |
| Argo's self-test, new checks | **142 pass, 0 fail**, 1 setup | `Argo_20261104_072224` |
| Yossele's self-test, new checks | **173 pass, 0 fail** | `Yossele_20261104_085645` |
| Uloe's self-test | **148 pass, 0 fail**, 1 setup | `Uloe_20261104_081239` |
| The whole mod as one pack, all six parts in one bundle, enabled alone | Boots: six `[RogueWaves:<Part>] v0.1.0 kit 1.7` lines, each part ready, no error but the game's own at quitting | `boot_20261104_085943` |
| Solo gaps, Direction effects and the HUD after each way into a map (`-rwonly effects,hud`) | **60 pass, 1 fail**, the one being a check of mine since fixed; the HUD alone again: **31 pass, 0 fail** | `host_20261104_065946`, `_070736` |
| Solo gaps: roll rules for all 9 vessels and clashes, the catalogue, the foes, 36 UI-scale checks (`-rwonly rules,foes,scales`) | **60 pass, 0 fail** | `host_20261104_090353` |
| The 45 maps, each with a vessel, VENDCINT, weather and an objective (`-rwonly tour`) | 33 maps: **208 pass, 1 fail**, then the other 12: **82 pass, 0 fail**. The one fail is the game's own, section 6 | `host_20261104_090903`, `_095141` |
| Ten real maps to the win (`-rwonly ten`) | **44 pass, 0 fail**: Tide 15 clears each, the end screen reads CAMPAIGN WON, and the party goes home to the stock world. Route 0123223223, 28 minutes | `host_20261104_100816` |
| The mode's whole flow with every part enabled: release builds of the vessels and foes beside the mode's self-test | **148 pass, 0 fail** | `RogueWavesMode_20261104_111205` |
| Direction effects and objective failures again, on the last code (`-rwonly effects`) | **33 pass, 0 fail** | `host_20261104_111335` |

The last code change was Beat the Clock (section 2, bug 6). The flow and the effects part ran after it. The two-player round, the pairs, the tour and the ten maps ran just before it, on code that differs only in that objective.

The Spool, Argo and first Uloe and Yossele launches ran with the mode's test build wrongly left on beside them (section 6). Spool and
Argo passed anyway and were not run again. Yossele and Uloe were, and the logs above are the clean ones.

## 2. Bugs found and fixed

| # | What a player would have seen | Why | Fix |
| --- | --- | --- | --- |
| 1 | **A friend who rejoins a running map stands on a map with no looks and no HUD, for good** | A rejoin's map arrives in one go and the player's own object comes a frame or more later. Everything the scene starts on that client reads it at once and dies: 506 things' client halves, the map's load, the Cardinal row, the end screen. Pitfall #139 | `RWRejoin.OnLog` notes what died, `Repair` runs it again once the object is there |
| 2 | **After a game restart, a rejoiner's flood has no look** | The same arrival can outrun the load groups: 116 of the flood's deter blocks died on a being not loaded yet. Pitfall #141 | `RWRejoin.Looks` runs those again once their group holds them |
| 3 | **Spring Tide makes towers take infinite damage at rank I, ten times the damage at rank III** | The stock Beacon script divides every hit by the number the Direction gave it as a reduction (0 and 0.1). Its check read only "applied without error". Pitfall #140 | The number stays at 1, a no-op, and rank III's shrug is the part's own hit hook. Measured on a real hit: 10 lost at rank I, 9 at rank III |
| 4 | **Fund Transfer's cards are blank white squares** | A card with no sprite keeps its white default, with no error. Pitfall #143 | The friend's vessel, the aura's colour, and numbers drawn in pixel digits (`RWDialog.Number`) |
| 5 | **Four Direction cards show the wrong picture**: Efficient Recycler and Mother of Mushrooms found none, Deep Current and Heavy Salvo had none, so all four showed the crest | Look names that match no file, and two icons never drawn | Aliases in `RWDirections.Picture`, and two new icons (`docs/TEMP_ART.md`) |
| 6 | Beat the Clock pays for any cleared Tide, however late | A Tide ends the moment its last foe falls, and the break's 60 s are on the clock a frame before `ModKitTides.InBreak` says break, so the check always saw plenty of time. Pitfall #142 | The clock's last reading in the Tide's own row, judged at the break. My first fix made it impossible to complete, and the final flow run caught that |
| 7 | Raze the Nest is offered on a map with no room for a nest, and can't be done | The roll never asked | `RWWeather.CanNest` filters it, and if a nest still finds no room, the chat says so |
| 8 | While Boutman is down, his Boutbro takes his Directions | The vessel lookup took whatever carried the `Hero` tag. Pitfall #145 | `RWRun.Vessel` checks the being against the player's hero |
| 9 | On `Alt_3B_Woozy` a rolled modifier may never show | That elite isn't loaded there. Pitfall #146 | `RWDifficulty.Pool` walks a reserve list and takes the first that loads |
| 10 | Fie and Buster are never offered Made of Tougher Stuff | `HasMinions` left them out | Added |

## 3. The list of gaps, and what each one came to

| Gap | Result |
| --- | --- |
| Fund Transfer's dialogs and limits | **Pass with a real second player**: through all three dialogs, 25 Red from him to the host, 10 Blue back, the amount past the limit sends what's left, a spent limit moves nothing and says so, refusals reach the sender, Back sends nothing |
| Votes on a client | **Pass**: a tie goes to the first tied map, a map both pick wins, the timer closes the question on the machine that didn't answer, one of two isn't a majority to leave |
| The shop on a client | **Pass**: paid on the host from the buyer's Pure Aura only, the rank bought shows on their vessel, refusals say why, a Team Cardinal joins the row |
| Weather on a client | **Pass**: flood, Quickening's speed at 1.25 and back to 1, the nest |
| The drop and the rejoin (`docs/mode/REJOIN.md` O1-O8) | **Pass after bugs 1 and 2**: O1-O5, O3b, O3c, O4, O7. O6 not measured, O8 in parts (that page has the table) |
| Continue with a friend, locked vessels | **Pass**: the friend's run comes back by their id, a try at another vessel goes back and is said, Directions back on the vessel |
| A friend without the pack | **Pass**: let onto the boat, the host warned, sent back with a chat line when the map can't load (about 90 s from when the test began watching, the code's limit being 75 s), and the host starts alone |
| A client's HUD, for each vessel | **Pass**: on the four pairs, the second player's vessel builds through the order the HUD sends, upgrades and sells. The one building not upgraded is Spool's Yarn Stash, which has no upgrades on his sheet yet |
| The F key on a real Boutman death | **Pass**: a real death and the game's own respawn, and F selects `Hero_Boutman`. A person's press stays for his eye |
| Direction effects beyond "applied without error" | **Measured on real hits and real prices**. Cut Costs 25, 22.5, 20, 17.5. Cheap Refits 25 to 17.5. Barnacle Coat pricks for 2 and 7. Spring Tide as bug 3. Made of Tougher Stuff took 1.25 of 5, then 3.75 to 3.1. Interest's cap +40. Vampire Dentures heals 1 for 5, 0.8 for 2 with two stacks. Vessels' own: Efficient Recycler 65, 80, 100%, Sewing Machine, Heavy Salvo +10, +20, +35%, Deep Current +15, +30, +50%, Mother of Mushrooms, Dandelion Season. Base vessels' stats: Skipping 4 to 6, Gallop 3.5 to 4.55, Regulars 0.5 to 0.65, Long Line 3 to 6, Packed Snow 2 to 2.5 |
| The chance-based upgrades (Yossele's test plan §5) | **Pass by their rates**: Viral Outbreak about 30% (7 of 27), Cordyceps about 70% (18 of 27), Hallucinogen 20% (2 to 9 stuns over the runs, from about 36 rolls: the low end is short of the 7 expected, and foes can be shot before their roll, which was not measured) |
| Roguelike Directions and Cardinals (scope, PRs #63 and #66) | **Nothing to test**: they are scope only, not built |
| VENDCINT, the votes, the Board at other UI scales | **Pass, 36 of 36**: 2560x1440 at scales 1-4, 1920x1080 at 1-3, 1280x720 at 1-2 |
| Objective failures | **Pass**: each fails as written, pays nothing, says so, and the map runs on |
| The HUD after every way into a map | **Pass, 6 of 6**: after staging, a Tide, a death, a lost map, the host's Exit, a won map |
| Uloe and Argo by hand | **Pass**: builds, upgrades, sell, PLUG IN, UNPLUG, Flood Light's AIM, the Drill's AIM, the crew's buttons, with no fallback to a direct call |
| A real Pin Cushion wall | **Not run**: it needs a person's judgement of what a wall is, and P1-P4 wait for it as Open questions 3 says |

## 4. How it was tested

**A real second player on one PC.** The game refuses a second launch (`boot.config`), so the second player runs from a copy of the
game's folder without that line, and both copies find the same Mods. The mode's two-player build puts the game's own `UnityTransport`
on the loopback address in place of Steam's. So the second player is a real Netcode client with its own process, its own scene and its own
machine state, and nothing is faked but the transport. Pitfall #144; `TDS_Mod/Tools/pc/Run-TwoPlayers.ps1`, which starts, kills and
restarts the second copy when the host's log says.

**The virtual player** (`ModKitPlay`) moves the real mouse over the real HUD for the clicks that need one. It is why the checks in section 3 say "by hand".

**New checks** stay in the code, behind defines, so a later round can run them:

```bash
powershell -Command "& '.\Build-RogueWavesModeMod.ps1' -Define ROGUEWAVESMODE_SELFTEST,ROGUEWAVESMODE_TWOPC"
```

```bash
powershell -Command "& '.\Tools\pc\Run-TwoPlayers.ps1'"
```

The solo gaps build is `ROGUEWAVESMODE_SELFTEST,ROGUEWAVESMODE_GAPS,ROGUEWAVESMODE_SHOTS,MODKIT_PLAY`, run with
`-Extra "-rwonly rules,effects,scales,foes,ten,tour,blocked,hud"`. Run its long parts in separate launches (section 6).

## 5. What is left for Dimitri

- **Steam's Join Game and a real network.** Loopback has no Steam friends list, no Steam lobby, no real delay and no pulled cable. All the
  mod's own logic on either side has now run, so an online evening is only these: the Steam path, and what lag does.
- **His eye**: Fund Transfer's new pictures (I checked captures of the three dialogs, and they read right), the two new Direction icons, how a rejoin's fly-in looks, and anything a check can't call good.
- **His wall**: a Pin Cushion wall of his own on Purlest and Woozy (`BALANCE_RERUN_2026-11-03.md` §9), for P1-P4.

## 6. Open items and notes

- **A `Lose` menu seen over a map, in tests only.** After the `effects` and `tour` test parts had run in one launch, the game's
  `Lose` menu was up over a later map. It did not appear after any of the six real ways into a map, nor in a launch of `effects` then
  `hud`. Cause not pinned down. Those parts force what no player can (pitfall #133), so I read it as the test's, but that is a reading, not a proof.
- **Two setups.** Uloe's Long Throw on a second Flood Light had no free tile near the node on this map (it last passed in
  `Uloe_20261103_045153`), and Argo's Deep Current reach had no foe past the plain reach (the reach numbers themselves passed).
- **A test build left on.** `Run-TwoPlayers.ps1` turned the mode on and left it on, and `Run-SelfTests.ps1` only switches the parts it
  is given, so the mode's test ran beside the vessels' and disturbed them. The script now puts the setting back. Pitfall #147.
- **Failures that were the test's own.** My first drop called the game's disconnect callback directly and left the error box hidden; a real
  disconnect shows the line (pitfall #148). The AIM check searched for a warning the game writes in capitals. My first Yossele rate check held
  six foes for 30 s: the Tide then stalled and every later check failed, and with the hold shortened to 5.6 s a round the Tide ran on, so I read that as
  foes reaching the Basin (pitfall #149), though the log has no line that says so. None of these touched the mod's code, and all are fixed in the tests.
- **Errors in the logs that are the game's own**, all at scene changes or quitting: `Bla_Ambient` and `Bl_Sound.Stopgroup`, `Entity_Vis_Hero.Despawn`,
  cancelled delays (`OperationCanceledException`). `Entity_Vis_Hero.Landing` fails twice on a rejoin, and only the landing's look is lost.
  A stranger's and a no-pack friend's client halves die as in bug 1, but they are sent home moments later.
- **Spool's Yarn Stash** has no upgrades on his sheet, as `! NEEDS YARN STASH UPGRADES !` says.
- `D:\TDS_second` (1.2 GB) is still on the PC, for Dimitri to delete when he likes; the runner makes it again when it is missing. The whole-pack test build is parked in `handoff\parked\pack_all_20261104`, and `pack.txt` is back to Yossele alone.
- **Map_Monogrove's load list names two assets the game doesn't ship** (`InvalidKeyException`, then a null in `Bl_Address.Load`). It is the tour's one fail. The same key shows in `RogueLab_20261009_002213`, with no pack part on, so it is the game's. The map loads and plays. The test no longer counts it.
