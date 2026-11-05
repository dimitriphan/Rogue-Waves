# The unbuilt candidates, built: what landed, what waits, what the PC must check (2026-11-05)

Dimitri (2026-11-05 04:51Z and 04:59Z): build every queued candidate ("19 build all", "34 Claude makes every missing
asset"), as several PRs, without changing a balance number. This is the thread's closing report. Every part below
**compiled here** (cloud-check, every part with every test switch, the editor builders; previews rendered for the art);
**nothing has run on the PC**. The PRs from #95 on have had no self-test in game, and kit 1.12 has only compiled.

## 1. What was built

| PR | Part | What |
| --- | --- | --- |
| #103 | 1 | The decisions' code items (`DECISIONS_APPLIED_2026-11-05.md` §3-§4): B1 map vote, B2 lobby Cardinals locked, B3-B4 shelf caps, Trinket slot, Trinkets on the vessel's panel, B5 Dazzling, B6 pack modifiers elite for Spool, B7 Spring Tide and Tidal Mend on the mode's own effects, B9 Monument keeps a building's own trade-offs, B12 Salt Sting's line, B13 Argo's slows renew, B15 Tougher for Boutbro, B16 no stock MOVE on the Thread Head; the max review's three calls at yes (MODEB-8, MODEA-3, YOS-3); round 3's Stitched click (pitfall #162) and Artrio Basin; pitfall #163 |
| #104 | 2 | Argo's fourteen review changes (scope `09`, `ARGO_REVIEW_2026-11-04.md` §5) |
| #105 | 3 | Uloe's 24 upgrades (scope `10`), at most 4 Light a building from teammates (B10), AIM and Photon visits for the owner's buildings, Supernova's SURGE once a Tide, Colour Filter's COLOUR; self-test 8b |
| #106 | 4 | Yossele's Wood Wide Web; the Shift Directions rename the building's panel (B8, pitfall #164) |
| #107 | 5 | Argo's Chum Bucket and Pressure Gauge; Yossele's Cutting Jar, Sap Bucket, Creeping Ivy, Deep Breath, Fairy Ring (self-test 12c) |
| #108 | 6 | Spool's Double Needle, Button Jar, Pattern Book, Merit Badge, Odd Socks (self-test 10c) |
| #109 | 7 | The 13 Directions any vessel takes, the Duos Fire Ship and Tide Pool, the Wound Spring / Spring Tide pair (`22_AnyDirections.cs`, gaps part "any") |
| #110 | 8 | The base vessels' Mason's Trowel, Snow Shovel, Worm Tin, Open Sign, Long Leash (`23_BaseDirections.cs`, gaps part "base") |
| #111 | 9 | Claude's 11 Trinkets, 4 Cardinals and the Punch Card / Pearl Oyster pair (`24_Trinkets.cs`, gaps part "trinkets"); pitfalls #165-#166 |
| #112 | 10 | The 47 icons of parts 5-9, temporary (`concept-tools/temp_art/b11_icons.py`); the art review in `docs/STYLE.md` and `concept-tools/temp_art/art_check.py` |
| #113 | 11 | The models and the rest of the missing art, temporary: three Shift looks, Uloe's 24 upgrade looks, Wood Wide Web, Button Jar's small Stitched and Dart (`Small` skin), the five objectives' icons on the Board card and the DONE popup |
| this PR | 12 | `/simplify` on parts 1-11 (section 2); kit 1.12 |

## 2. The clean-up (part 12)

Four reviewers read the release code of parts 1-11 (reuse, simplification, efficiency, depth); `/code-review high`
then read the clean-up itself. The rule was the same as on 2026-11-04: **no number and no behaviour changes.**

**Kit 1.12** (one copy instead of several; `TDS_Vessel_Creator/kit/README.md`):

- `ModKitUtil.SlowOn`: the held slow with the 10% floor. It was copied word for word in Yossele and the mode.
- `ModKitUtil.Auras`, `Purse`, `Gain`, `Give`: a player's aura, the map's gain and the game's own Give.
  - Uloe's Colour Filter, Yossele's Wood Wide Web and the mode's Fine Sieve each read the purse by hand.
  - Colour Filter now reads it once before a strike and once after, instead of six times.
- `ModKitCombat.SetMulti`: a hit hook's new Damagemulti, set on a copy of the hit's data (pitfall #30). It replaces 20
  copies across the five parts.
- `ModKitUtil.Keep` on Health takes the mod off and makes it again, and does nothing when the value hasn't changed.
  Spool's Merit Badge and the mode's held mods did this by hand. No part kept Health with `Keep` before, so nothing else
  changes.

**The mode:**

- Dead code and counters that nothing read are gone:
  - `RWTrinkets.KitOf`;
  - Gutted, Stormed, Oystered, Whistled, Sieved, LobbyFixes and Redraws;
  - one counter each in Yossele, Spool and Argo.
- One filtered tower list a tag a frame, `RWAny.Owned`. Wound Spring, Magpie Nest, Spare Hands, Oil Can and Tar Bucket
  each copied and filtered the list on their own.
- Mason's Trowel and Tar Bucket share one rest-then-mend routine, `RWUtil.Rest` and `RWUtil.Mend`. Each keeps its own
  test for a hit.
- Lamp Oil and Gull Feather share `RWAny.DrownedBy`.
- Clam Shell's and Cork's chain placement share `RWAny.SetChain`.
- `RWRun.Info` replaces a second copy of the client-info lookup.
- `RWDirections.Top` keeps each Direction id's parse. It runs about 75 times a second per player and re-parsed every
  held id each time.
- `RWHold` keys by the thing itself, not a new string on every look.
- `RWCardinals.Def` keeps its answer for each held id.
- Id tests compare ordinally.
- Spare Plank skips its button scan while the plank stands.
- Signal Flare looks four times a second while there is no Basin, not every frame.

**The vessel parts:**

- Yossele's Monument watch (four dictionaries) and Sap Bucket (three) are one record each.
- Uloe's Light ledger is one table instead of two.
- Argo's Chum Bucket tests the foe before looking up the attacker's component.

**Left as it is, with the reason:**

- Not built because the risk is too large for a build that has never run:
  - one record per player for the Any, base and Trinket Directions;
  - one hit handler per side instead of the Any/Trinket pairs;
  - the Directions' held stats read from the def table;
  - a ranks cache per player;
  - one table of the mode's per-map resets;
  - one vessel table.

  Each would rewire every Direction or the order of the vessel's hit hooks. They are worth doing after a PC round
  proves this build.
- Undone after the code review, as not worth the risk:
  - **Trinkets reading the player and vessel from RWAny's crews.** That depended on two timers staying in step.
  - **Dropping Aegis from Gull Feather's elite list.** It matches no asset in the default group, but a map could load
  one.
- Not one helper, because each site is different:
  - **Healing to the top.** The sites differ in thresholds, and Spool pays Thread for each point.
  - **RWBase's wall, Boutbro and shop sweep.** Boutbro is tagged Hero, so `Owned` would skip him.
- Would change what happens:
  - **Tin Whistle through `RWHits.Resist`.** It would end at map end, not at the next map's start.
  - **Magnifying Glass's one area query a shot.** A split foe born mid-shot would be missed.
  - **A cache of the Basin cores.** Artrio's sub-core can come mid-map.
- **A shared colour-button base.** The game finds scripts by class name.

**Found, not changed: behaviour questions for the next code review or Dimitri:**

1. **`ModKitUtil.Front` flips the order of two parts' hit handlers on the same thing.** Whoever re-fronts last goes
   first, so the order swaps every 0.05-0.1 s.
   - Real cases:
     - Made of Tougher Stuff III's cap and Odd Socks III's x0.9 on a Crafted give either `cap` or `0.9 x cap`;
     - Spring Tide III's shrug against Yossele's Blockade soak and Connected Roots share on Yossele's towers.
   - Fix: `Front` leaves a handler alone when it is already among the leading mod handlers. A fully fixed order needs
     priorities across parts, which is a large kit change.
2. **Uloe's Searchlight slow ignores the 10% floor.** It uses `Addmod` directly, not `SlowOn`. On a foe already under
   Fog Horn, Tin Whistle or Sap it can go below 10%.
3. **Argo's Cleave counts as the vessel's own hit.** It is dealt from the vessel, so Fish Hook, Gutting Knife and Notched
   Oar count it. The parts have no shared "extra hit" mark.
4. **`RWDifficulty.StockElites` lists Aegis.** No asset by that name is in the default group, so a seed that draws it
   takes the next stock elite in reserve. Taking it out would change which modifiers each seed draws.
5. **Four tests for "is this foe elite".** They are `ModKitUtil.Elite`, Spool's, Gull Feather's and Yossele's
   `FoeUpgrade`. Making them one changes which foes count.
6. **Tar Bucket's hit test is weaker than Trowel's.** It uses a drop in health and `Lasthittime`; Trowel uses a hook. A
   hit carrying its own Damage never sets `Lasthittime` (kit `docs/17` §2.6).
7. **Latent problems:**
   - `HasMinions` lists every vessel, so `Needs="Minions"` never filters.
   - Spring Tide and Tidal Mend reach Blurry Photo copies, but Wound Spring and Oil Can don't.
   - `RWCardinals`' paired lookups (`ClashHere`, `PriceMine`) assume the plain stack id.

## 3. What waits, and why

- **Spool's 22 upgrades and the Tin's UNTHREAD (B19), and Pin Barrage leading to Iron Thimble (B18):** after the
  balance sweep, as he ordered (S16). B17, a bigger Crafted click, waits on his try at selling a walking Stitched.
- **Spool's P1-P4:** still held (PLAN Open questions 3).
- **The max review's §5.2:** twelve findings that only a fact from the PC can settle
  (`CODE_REVIEW_MAX_2026-11-05.md`). They were not in this thread's scope and go with the next round.
- **The meteor's white crystal in the map:** the node keeps a stock ore's look.
  - A crystal needs a model, and the mode part carries no model library.
  - The boot log names what the ore's look is made of (`meteor: the ore's look is ...`). He can accept the ore look,
    or the next art pass gives the mode a library.
- **Balance numbers:** none changed. The tilde lines in scopes `08` and `10`, set S2 with every proposal, the five Spool
  numbers and Spring Tide beside a Fie wall belong to the balance sweep.
- All the art is temporary. His look by eye comes first.

## 4. Readings for Dimitri to confirm

Each is how the code reads a scope line. A "no" is a small change.

| Item | How the code reads it |
| --- | --- |
| Deep Breath | The 2 s rest counts from the laser's reload, since Yossele fires every 2 s. |
| Galley Kettle, Notched Oar, Chain Shot | They count attacks. Hits in one frame are one attack; foes struck are not counted. |
| Clam Shell | It judges a blow after the other mods' front hooks and before the game's own AIs. |
| Lamp Oil | It needs the killing blow's Lasthit, within 0.25 s. |
| Spare Hands | Not offered to Yossele or Sno Sno. |
| Spare Plank | The vessel's own Defense build button is free. Argo and Buster are never offered it. |
| Pearl Oyster | Buying the Oyster itself isn't a buy; Direction ranks are. |
| Bottled Storm | Its reward is a random Direction's rank. |
| Brass Spyglass | "Ranged" means the vessel has a Projspeed. |
| Gutting Knife | It skips the pack's extra hits. |
| Gull Feather | Each later elite adds 8 s. |
| Cargo Crate | One more card each stack, up to 6. |
| Tin Whistle | It counts the Tide row's own beings, not a split's or a summon's. |
| Cork Lifebuoy | It comes after the game's own last stand. |
| Open Sign | It needs the Regulars upgrade. |

## 5. The next PC round

Run every self-test on kit 1.12 first. None has run since kit 1.9, and kit 1.11 was only boot-checked. Then:

1. **The mode's gaps build** (`ROGUEWAVESMODE_GAPS`): parts "rules", "any", "base" and "trinkets".
2. **Argo:**
   - the Chum Bucket and Pressure Gauge lines;
   - the fourteen review changes of part 2;
   - Chum Bucket's bonus (part 12 changed the order of its tests).
3. **Yossele:**
   - section 12c;
   - Wood Wide Web's trade (now through the kit's purse);
   - the Shift names on the building's panel;
   - the Monument watch and Sap Bucket (both one record now).
4. **Spool:**
   - section 10c, with Button Jar's small look;
   - Merit Badge's health (now through `Keep`);
   - no MOVE on the Thread Head;
   - the Stitched click.
5. **Uloe:**
   - section 8b and the 24 upgrades;
   - the teammate cap of 4 Light (one ledger now);
   - Colour Filter's trade.
6. **By eye:**
   - the 47 icons and the five objective icons;
   - the three Shift models, Uloe's 24 upgrade looks, Wood Wide Web;
   - the small Stitched and Dart.
7. **The meteor's boot log line.**
8. **The earlier lists:**
   - `CODE_REVIEW_MAX_2026-11-05.md` §6;
   - round 3's open items (Spring Tide beside a Fie wall: 10 became 4.9, where 9.5 is expected);
   - the icons after #99.

The next free pitfall is still **#167** (#162-#166 came from this build).
