# Base game Cardinals split into Trinkets and Cardinals (2026-11-05)

Dimitri, 2026-11-05 23:27Z: "Some base game cardinals are closer to independent trinkets rather than team Cardinals.
Check which base game Cardinals can be turned into trinkets and which can only be teamwide, then separate them."

**Result.** Of the 29 base game Cardinals the shop sells, **13 are Trinkets now** and 16 stay Cardinals. A stock
Trinket is held once by each player who buys it and works on that player's things alone. Built in `RogueWavesMode`
(`17_Cardinals.cs`, `24_Trinkets.cs`, self-test rules in `92_GapsTrinkets.cs`), scope `12` updated with the list as
`~defaults~`. Compiled here, not run on the PC (section 6).

Sources: every stock Cardinal's core upgrade, as dumped in game (`TDS_Vessel_Creator/reference/roguelab_round7.txt`,
`CARD` lines), what each script does (`docs/reports/engine_study_2026-10-28/A_cardinals.md`), kit `docs/17` §9 and
`docs/15` §14.1-§14.2, and the first look at the question (`docs/reports/SCOPE_VS_LOOP.md` §F, 2026-10-09).

## 1. The test

A Cardinal can be a Trinket when what it does lands on things one player owns (their vessel, towers, units, builds or
aura), so the mode can give it to that player's things alone. It stays a Cardinal when it acts on the Basin, the foes,
the ore or the Tide clock, or adds a unit or a Basin build that serves the whole team whoever owns it. The game's own
scripts reach every player (Heroapply every vessel, Towerapply every tower, Coin splits its aura), so a Trinket never
goes on the core: the mode puts the same effect upgrades on the buyer's things, or pays or fires for the buyer.

## 2. The 29 the shop sells

| Cardinal (id) | How the game reaches | Now | Why |
| --- | --- | --- | --- |
| Sport Water (`Artifact_Caffeine`) | Heroapply: every vessel | **Trinket**, Hero | a growing buff on a vessel; the buyer's vessel takes the same effect upgrade |
| Fluffy Slippers (`Artifact_Slippers`) | Heroapply | **Trinket**, Hero | speed while the vessel keeps moving |
| Beast Tassle (`Artifact_Beastpelt`) | Heroapply | **Trinket**, Hero | the vessel turns elusive |
| Diorama (`Artifact_Diorama`) | Heroapply_Delay (5 s) | **Trinket**, Hero | stats and the tiny look on the vessel; the 5 s delay is dropped |
| Paper Tower (`Artifact_Paper`) | Heroapply; the effect watches its vessel's builds | **Trinket**, Hero | the first attack tower the buyer builds turns paper |
| Rusty Pipe (`Artifact_Rustypipe`) | Towerapply: every new tower | **Trinket**, Tower | the buyer's towers ripen after some Tides |
| Frail Titan (`Artifact_Titan`) | Towerapply | **Trinket**, Tower | a trade-off on towers; as a Cardinal it also weakened towers a teammate never chose to weaken |
| Voice in a Bottle (`Artifact_Voice`) | Voiceapply: everything a vessel builds | **Trinket**, Built | the buyer's builds' upgrades get cheaper each Tide |
| Red, Yellow, Blue, Green Coin (`Artifact_*coin`) | Coin: aura to every player, split | **Trinket**, Coin | aura is per player (kit `docs/15` §14.2); the buyer gets the whole sum the game would split |
| Aura Gavel (`Artifact_Gavel`) | every friendly unit's death fires a bolt | **Trinket**, Gavel (judgment, §5) | the trigger is a unit drowning, and units are a player's own |
| Processor Jr (`Artifact_Converter`) | a Basin build whose converter pays every player | Cardinal | the button is the Basin's and the pay is everyone's |
| Starter Seeds (`Artifact_Seeds`) | the converter, and every ore on the map | Cardinal | ore pays the whole team (kit `docs/17` §8) |
| Cubed Drink (`Artifact_Cubeddrink`) | a Basin build (slot 2); the stand buffs towers and units around it | Cardinal | a Basin button any player uses; a vessel's four build slots are all taken |
| Tacklebox (`Artifact_Tackle`) | a Basin build (`Buildoption4`): snare traps | Cardinal | the Basin's button; snared foes pay whoever hits them |
| Torrid Badge (`Artifact_Badge`) | spawns merchants by the Basin | Cardinal (§5) | a unit that shoots any foe; its badge goes to Buster by tag |
| Bendigo (`Artifact_Bird`) | spawns a bird | Cardinal (§5) | its pop hurts every foe on the map |
| Kippy (`Artifact_Kippy`) | spawns a cat | Cardinal (§5) | carries its buff to the farthest ally, anyone's |
| Fish Bait (`Artifact_Fishbait`) | spawns Gabby | Cardinal (§5) | a team unit by the Basin |
| Opal Core (`Artifact_Opalcore`) | Coreapply | Cardinal | the Basin's health |
| Glass Core (`Artifact_Glasscore`) | Coreapply | Cardinal | the Basins, and everyone's aura |
| Fragment (`Artifact_Fragment`) | Coreapply on the enemy Basin | Cardinal | the foes' Basin |
| Wave Breaker (`Artifact_Wavebreaker`) | foes near a Basin | Cardinal | the foes |
| Blue Paint (`Artifact_Bluepaint`) | every foe | Cardinal | the foes |
| Spear Point (`Artifact_Greatspear`) | every Javelin | Cardinal | the foes |
| 1st Grade Textbook (`Artifact_Textbook`) | every Dagger | Cardinal | the foes |
| Chimera Crystals (`Artifact_Chimera`) | every ore | Cardinal | the map's ore |

The 11 the shop never sells (`RWCardinals.NotSold`, scope `12`) are left out as before: Moon Core, Vintage Watch, Same
Day Shipping and Looped Mirror bend the Tide clock, Frozen Computer the game's speed, Bottom Rock everyone's pay,
Checklist the foes' drops, Sand Filled Box is the sandbox; Money Bag, Pig Named Bank and Blurry Photo would be Trinkets
if the shop ever sold them.

## 3. How each kind works (host)

- **Hero** (`RWCardinals.GrantStock`, twice a second through `RWRun.Carry`): the Cardinal's own effect upgrades, read
  from its core upgrade's params, go on the buyer's vessel (a respawn copies them; a repeat is skipped), and on a
  Hero-tagged unit of theirs such as Boutbro, who gets the game's own through the core's `Child`.
- **Tower, Built** (`RWCardinals.Reach`, the owner-filtered spawn hook and the sweep after a buy): the Towerapply or
  Voiceapply effect goes on each new tower of the buyer's (Built: tower, trap or unit whose parent is a Hero) through its
  init list, as the game's own scripts do (pitfall #118); a thing already standing gets it at once.
- **Coin** (`RWTrinkets.CoinStart`, `Tide`): the start sum once a map, once the Basin stands or at once when bought mid-map;
  then each break its Tide's sum, Add x Tide x (30 / Tides, whole number) as `Entity_Ai_Artifact_Coin` counts it, all
  to the buyer through `Giveresources(list, owner)`.
- **Gavel** (`RWTrinkets.Gavel`): one of the buyer's units drowns (a Mob, the vessel too, not a copy), and the Gavel's own
  bolt (`Artifact_Auragavel`, the one Signal Flare already throws) flies from it at the nearest foe Mob or tower within the
  Gavel's 12 tiles, damage by the unit's level from the Gavel's params (5 to 25). The Gavel's per-unit marker upgrade is
  not given (its use is unread).
- **Shop:** a stock Trinket takes the shelf's Trinket slot, shows "Trinket" on its card, and its line (the game's own
  name and words) goes on the buyer's vessel panel (B4). It is never put on the core or in the Cardinal row. It costs
  15 as before, with the ramp. Not offered when a lobby pick already gives it to everyone, or when a campaign saved
  before this change holds it as a Cardinal (that save keeps it on the core).

## 4. Balance

The mode must end harder than the base game. In one-player play nothing changes: the buyer is the whole team. With
more players each purchase now does less for the team (Sport Water lifts one vessel, not all; a Coin's sum is the
same total, paid to one player), but each player may buy their own copy. Every copy raises the buyer's later prices
by 3 (C1) and every foe's health by 2% (F5, `21_Difficulty.cs` counts each player's Trinkets), so a team that buys
four Sport Waters pays for four stacks of foe health where it used to pay for one Cardinal. No number changed.

## 5. Open questions for Dimitri

1. **Aura Gavel as a Trinket** (the judgment call; Trinket picked as the default): it now fires only for its buyer's
   units. Keep, or back to a Cardinal for every unit on the team?
2. **The four companion Cardinals** (Torrid Badge, Bendigo, Kippy, Fish Bait) stay Cardinals: the unit they add helps
   everyone whoever owns it. They could be personal by putting the spawner on the buyer's vessel, so the unit is theirs
   and takes their unit Directions and Trinkets, but a spawner on a vessel that drowns may spawn a second unit at each
   respawn (unread, needs the PC). Keep as Cardinals, or try them as Trinkets?
3. **Coin's sum:** the buyer gets the whole sum the game splits (the same total for the team). Or a share, as if split?

## 6. What was checked, and the PC list

Compiled here (`TDS_Mod/ModSource`, `python3 ../Tools/cloud-check/check_mod.py RogueWavesMode/Code Shared RogueWavesMode
"<defines>"`): release, `ROGUEWAVESMODE_SELFTEST`, `ROGUEWAVESMODE_SELFTEST,ROGUEWAVESMODE_GAPS`, and with `_SHOTS`
added: 0 errors, the same 0-2 warnings as the base. `ROGUEWAVESMODE_TWOPC` reports the Netcode transport error the stubs
always give (cloud-check `README.md`). Nothing ran on the PC.

For the next PC round (the gaps build, solo then two players):

1. Gaps rules: "Stock split: ..." two lines pass (13 Trinkets of the right kinds; Sport Water held once by its buyer, a
   second player buys their own, never in the run's Cardinals).
2. Buy Sport Water and Beast Tassle on Smacky: the vessel panel shows both lines, the vessel's Speed grows with the
   Tides and it is untargetable; a teammate's vessel gets neither. Drown and respawn: both stay.
3. Diorama on a pack vessel (Yossele): the tiny look and its stats; Paper Tower: the first archer turns paper.
4. Rusty Pipe and Frail Titan: the buyer's towers built before and after the buy hold the effect (`Holding`), a
   teammate's don't; a Frail Titan tower bursts when it drowns.
5. Voice in a Bottle: the buyer's tower's upgrade price falls over two Tides (the review's MODE-C4 and G3 still pass).
6. Red Coin: the buyer's Red rises by 25 at the map's start (or at the buy) and by Add x Tide x factor at each break;
   the other player's doesn't. Read `RWTrinkets.CoinPaid`.
7. Aura Gavel: one of the buyer's units drowns near a foe and a bolt flies (`RWTrinkets.Gavels` +1); a teammate's
   unit drowning fires none. Note the bolt's damage against the unit's level.
8. A lobby pick of Sport Water: it works for everyone and the shop never offers it; an old save holding Beast Tassle as
   a Cardinal keeps it on the core and in the row (the gaps "Old save" check still passes).
