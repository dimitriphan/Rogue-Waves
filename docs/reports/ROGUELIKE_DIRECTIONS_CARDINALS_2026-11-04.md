# Cardinal overlap check, and Directions and Cardinals from other roguelikes (2026-11-04)

Frozen report. Dimitri asked (2026-11-04 04:33Z): "Check to make sure that the new cardinals you created do not overlap
with already existing cardinals. If there is an overlap, remove your own cardinal. Afterwards, develop some more
directions and cardinals using other roguelikes as inspiration, such as Risk of Rain or Hades and then update the
artifact."

**Result:**

- 5 of the 13 Cardinals from `DIRECTIONS_CARDINALS_2026-11-04.md` did what a base game Cardinal already does. They are
  removed from scope `12`. At Dimitri's next word, Claude's 7 older Cardinals that copied stock ones went too, from the scope and
  from the mode's code.
- 11 more Directions and 7 more Cardinals were added, all Claude's `~defaults~` with `By: Claude`.
- Every one of them was checked against the 40 base game Cardinals and every pack Direction and Cardinal before it went in.
- Nothing is built, and nothing here has run in game.
- The Scope Editor is rebuilt from the new scope (https://claude.ai/artifact/5KAGb5wdqiNV1bEoxNMgEo).

## 1. The overlap check

**What I compared against:**

- The base game Cardinals' own in-game lines, as the game itself reports them (kit
  `reference/roguelab_round1.txt`, `INFO cardinal` lines).
- What each one's script does (`docs/reports/engine_study_2026-10-28/A_cardinals.md`).
- The pack's eight Cardinals that came before (Vampire Dentures and Claude's seven).

**The test:** a new Cardinal overlaps when the player would get the same effect from a stock Cardinal. That holds even
when it lands on other things (vessel or towers) or is gated a little differently.

| New Cardinal | Its effect | Closest existing Cardinal (in-game line) | Verdict |
| --- | --- | --- | --- |
| Brass Spyglass | vessel range +0.5 tile | none gives range | kept |
| Gutting Knife | vessel +20% damage on foes above half health | none | kept |
| Oil Can | towers +5% attack speed a stack | Cubed Drink "increasing flat damage of towers and units" (damage, not speed); Rusty Pipe (health and damage) | kept; its pair is gone, so Clashes is now none |
| **Iron Rivets** | towers +8% health a stack | **Rusty Pipe** "Towers become stronger after some Tides" (a Health and Damage Multi on every tower) | **removed** |
| Tar Bucket | towers mend when left alone | none mends | kept |
| Rubber Duck | vessel respawns sooner | none | kept |
| **Siren Conch** | a foe brought low turns friendly | **Blue Paint** "Enemies kept at very low health will eventually turn friendly" (the same charm script) | **removed** |
| Spare Plank | one free wall that comes back | Processor Jr, Cubed Drink, Tacklebox give a free building of their own kind, never a wall | kept |
| **Kelp Net** | foes near the Basin move slower | **Frozen Computer** "Time slows with enemies near the Basin" | **removed** |
| **Lucky Geode** | every ore node mines faster | **Starter Seeds** "Slightly increase aura gain from all mines" | **removed** |
| Cork Lifebuoy | the Basin lives through one more killing blow a map | Looped Mirror replays the Tide; Opal Core adds health. The game's own last stand is not a Cardinal | kept |
| Signal Flare | a bolt every 20 s at the toughest foe near the Basin | Aura Gavel's bolts fire when your units drown, not on a timer | kept |
| **Storm Lantern** | towers +25% attack speed for 10 s at each Tide's start | **Sport Water** "Vessels are boosted at the start of Tides" (its own Tide burst, put on towers) | **removed** |

The removals are done in scope `12`. Two more changes came with them:

- Oil Can's Clashes line is now `~None~`. Its note says why its pair is gone.
- A new `~No copies~` line in `12` [Rules] says a new Cardinal never does what a base game Cardinal does.

**Claude's older Cardinals, removed too (Dimitri, 04:49Z: "Remove the older cardinals that are also still copies"):**

Claude's seven *earlier* Cardinals (2026-10-09, built 2026-11-03) had been made on purpose as the shop's own versions
of stock Cardinals. Under the same test, every one was a copy:

| Older Cardinal | Copied | How |
| --- | --- | --- |
| Spiny Hull | Opal Core | its "deals damage when touched", put on the vessel |
| Sea Legs | Fluffy Slippers | the same script, `Entity_Ai_Effect_Artifact_Slippers` |
| Rising Tide | Sport Water | the same script, `Entity_Ai_Effect_Artifact_Caffeine` |
| Puffer Float | Frail Titan, Aura Gavel | Frail Titan's death blast, on your units |
| Tide Clock | Vintage Watch | the same script, `Entity_Ai_Artifact_Vintage` |
| Rusted Bell | Wave Breaker | the same script, `Entity_Ai_Artifact_Wavebreaker` |
| Second Wind | Looped Mirror | the same script, `Entity_Ai_Effect_Artifact_Loop` |

All seven are removed from scope `12`, and from the mode's code (`RWCardinals.Mine`, their icons in
`RogueWavesModeArt.cs`, and the load of the two stock groups only Tide Clock and Second Wind used; mode
`IMPLEMENTATION_NOTES.md`). The code compiles here and has not been run on the PC.

After this, the pack's own Cardinals are Vampire Dentures (built) and Claude's 15 not yet built. Scope `12`'s Team
Cardinals are now only Claude's four new ones. The No copies line is now Dimitri's rule, written without `~`.

**Directions were checked too:**

- No new Direction copies a Cardinal.
- One Cardinal sits close to a Direction: Oil Can is Wound Spring's attack speed for towers. One is a Direction you pick
  and rank, the other a Cardinal you buy and stack, so both stay. §5 question 2.

## 2. What the roguelikes gave

| Game | The idea there | What the pack took |
| --- | --- | --- |
| *Risk of Rain 2* | Common items stack without a cap and each does one plain thing on a hit or a kill: Tri-Tip Dagger bleeds, Gasoline and Will-o'-the-wisp burst foes you kill, Tougher Times blocks some hits, Bustling Fungus heals you while you stand still, Wake of Vultures lends you an elite's power | Fish Hook, Lamp Oil, Oilskin Coat, Fairy Ring, Gull Feather |
| *Hades* | Duo boons come only to a player holding boons of two gods. Chaos boons are a curse for a few rooms, then a blessing. The Mirror's Fated Persuasion shows one more boon, and Fated Authority rerolls. Demeter's Killing Freeze chills the last foe | **Duo**, a new Direction kind (Fire Ship, Tide Pool); Bottled Storm; Spare Compass; Tin Whistle |
| *Slay the Spire* | Pen Nib doubles every 10th attack. The Membership Card makes the shop cheaper, the Maw Bank pays while you don't spend, and the Courier restocks the shop | Notched Oar, Punch Card, Pearl Oyster, Cargo Crate |
| *Balatro* | Abstract Joker grows with every Joker held. Interest pays on saved money, Clearance Sale makes the shop cheaper, and the Overstock voucher adds a shop slot | Magpie Nest, Pearl Oyster, Punch Card, Cargo Crate |
| *The Binding of Isaac* | Brimstone is a charged beam, Steam Sale halves shop prices, and transformations reward holding several of one kind | Deep Breath, Punch Card, Odd Socks |
| *Monster Train* | Slay triggers make a unit grow with each kill, and two clans' units work together | Merit Badge, Odd Socks |

**Left out:**

- Crit chance: the game has no crit stat.
- Shaped Glass's double damage for half health: Diorama already alters the vessel's stats both ways.
- Hades' Heat, and Risk of Rain's Shrine of the Mountain (harder foes for more reward): sea weather and bonus Tides
  already pay more Pure Aura for a harder map.
- Healing on kill: Vampire Dentures heals on damage.
- Anything that needs a click (Equipment, Casts): the game has no targeted Cardinal.

**Scope:** after the 2026-11-04 limit, the new Directions are for any vessel, Yossele and Spool only. None are for
Argo, Uloe or the base vessels.

## 3. The new entries, what they ride on, and their scale

The scale is the same as before (`DIRECTIONS_CARDINALS_2026-11-04.md` §2):

- **Direction:** rank I is about +6% to +15% on one stat; rank III is about +20% to +50%.
- **Vessel Cardinal:** about one level 1 or 2 upgrade.
- **Team Cardinal:** about one stock Cardinal or less.

Every "Rides on" is **read**, from the kit (`docs/17` §8-§11, `docs/15` §18.1, `TDS_Mod/ModSource/RogueWavesMode`), and
none is **verified**. *Pack code* means code the pack writes itself, on a known pattern.

### 3.1 Directions

| Direction | Vessel | Kind | Source | Rides on | Scale |
| --- | --- | --- | --- | --- | --- |
| Notched Oar | Any | Boost | Pen Nib | *pack code*: a hit counter in the vessel's per-hit handler (`17` §11.1 rule 8) | every 10th / 7th / 5th hit ×2 ≈ +10 / 14 / 20% |
| Lamp Oil | Any | Boost | Gasoline, Will-o'-the-wisp | *pack code*: on a foe's death with the vessel as `Lasthit`; the burst through `ModKitCombat` so kills are credited (rule 17) | 15 / 25 / 35% of the foe's max health, 1-1.5 tiles |
| Fish Hook | Any | Boost | Tri-Tip Dagger, Hangover | *pack code*: per-hit handler; a timed damage tick with its own debuff look (pitfall #114) | 15-35% of hits, half the hit again over 3 s; up to ≈ +17% at rank III |
| Spare Compass | Any | Boost | Fated Persuasion, Fated Authority | `RWDirections.Offers` (3 today, `Code/16_Directions.cs`) and a redraw button on the offer dialog | 4 / 4 + redraw / 5 offers |
| Magpie Nest | Any | Boost | Abstract Joker | `Damage` Multi on the vessel and the owner's `Tower`s, re-set when a Cardinal is bought (`RWCardinals`) | +1 / 1.5 / 2% a Cardinal: ≈ +10 / 15 / 20% at 10 Cardinals |
| Fire Ship | Any, holding Lamp Oil and Chain Shot | **Duo** | Duo boons | Chain Shot's jump hit plus Lamp Oil's burst | 10% of the struck foe's max health a jump |
| Tide Pool | Any, holding Tidal Mend and Spring Tide | **Duo** | Duo boons | Tidal Mend's mend; a `Health` Add mod cleared at Tide's end | up to +20% max health on towers for a Tide |
| Deep Breath | Yossele | Boost | Brimstone | Yossele's laser in `TDS_Mod/ModSource/Yossele`, a timer since the last shot | +40 / 70 / 100% on the first beam after 2 s; rank III +1 tile |
| Fairy Ring | Yossele | Boost | Bustling Fungus | *pack code*: a 1 s sweep while Yossele's state has no "Move" (Fluffy Slippers reads the same state, `A_cardinals.md`) | 1-2% max health a second within 1.5-2.5 tiles |
| Merit Badge | Spool | Boost | Slay triggers | Spool's `Crafted` in `TDS_Mod/ModSource/Spool`; a per-unit kill count and `Damage`/`Health` Multi | +3% a kill, up to +15 / 30 / 45% |
| Odd Socks | Spool | Boost | transformations, clan pairs | Spool's `Crafted`, a 1 s check for another kind within 2 tiles | +8 / 15 / 25% damage; rank III -10% damage taken |

A **Duo** is a new kind, written into `06` [Directions] (`~Duos~`) and the Direction template in `15`:

- It is offered only to a player who already holds both Directions it names.
- It has one rank, so its Rank II and Rank III slots say so.
- The offer code in `16_Directions.cs` already leaves out what a vessel lacks. A Duo adds two "holds" checks.

### 3.2 Cardinals

| Cardinal | Kind | Cost | Source | Rides on | Scale |
| --- | --- | --- | --- | --- | --- |
| Punch Card | Vessel | 15 | Membership Card, Steam Sale, Clearance Sale | VENDCINT's price in `RWShop` (`Code/18_Shop.cs`) | -20% on Cardinals; pairs with Pearl Oyster |
| Pearl Oyster | Vessel | 15 | Maw Bank, interest | the Pure Aura pay each Tide (`RWRun`), stopped by a buy on that map | up to +1 a Tide, ≈ +15 a map when you save (a map pays 45-60) |
| Bottled Storm | Vessel, stacks | 15 | Chaos boons | a `Hurt` handler at priority ≤ 8 for 5 Tides (`17` §11.1 rule 11), then a free rank through `RWDirections` | +25% damage taken, then a rank worth Pure Aura 20-30 |
| Oilskin Coat | Vessel, stacks | 12 | Tougher Times | a `Hurt` handler at priority ≤ 8 that cancels the hit | 10%, 19%, 27%... |
| Gull Feather | Vessel | 15 | Wake of Vultures | on a death with the vessel as `Lasthit`, a foe carrying a modifier (`RogueWavesFoes`, or the game's elites) | +20% damage, +15% speed for 8 s |
| Cargo Crate | Team, stacks | 20 | Overstock, Courier | VENDCINT's three Cardinals rolled a break (`Code/18_Shop.cs`) | +1 on offer a stack, at most 6 |
| Tin Whistle | Team | 15 | Killing Freeze | *pack code*: the count of `Enemy` left in a Tide, then Spear Point's `Speed` Multi script and a damage-taken mod | the last 3 foes: -30% speed, +25% damage taken |

Pairs:

- Punch Card ↔ Pearl Oyster is a new research-choice pair: one rewards spending, the other saving.
- Wound Spring ↔ Spring Tide is still a pair.
- Oil Can ↔ Iron Rivets is gone.

## 4. The count now

| | Dimitri's | Claude's, earlier | Claude's, StarCraft II | Claude's, roguelikes | Total |
| --- | --- | --- | --- | --- | --- |
| Directions | 6 | 17 | 21 | 11 | 55 |
| Cardinals | 1 | 0 (7, all removed) | 8 (13, less 5 removed) | 7 | 16 |

## 5. Questions for Dimitri

1. ~~Should Claude's seven earlier Cardinals that copy stock ones also go?~~ Answered: yes, all seven are removed (§1).
2. Should a Cardinal that matches a Direction be allowed? The only one now is Oil Can (towers attack faster), which
   matches Wound Spring.
3. Should Duos exist? Each needs two other Directions first, so a Duo is rare in a 10-map campaign.
4. As before (PLAN Open question 7): take, change or drop each new entry, and keep the pairs or not.

## 6. Sources

- Risk of Rain 2 items: https://riskofrain2.fandom.com/wiki/Items
- Hades boons and the Mirror of Night: https://hades.fandom.com/wiki/Boons, https://hades.fandom.com/wiki/Mirror_of_Night
- Slay the Spire relics: https://slay-the-spire.fandom.com/wiki/Relics
- Balatro Jokers and vouchers: https://balatrogame.fandom.com/wiki/Jokers, https://balatrogame.fandom.com/wiki/Vouchers
- The Binding of Isaac transformations: https://bindingofisaacrebirth.fandom.com/wiki/Transformations
- Monster Train: https://monster-train.fandom.com/wiki/Monster_Train_Wiki
- The base game Cardinals: kit `reference/roguelab_round1.txt` (in-game lines), `reference/roguelab_round7.txt` (`CARD`
  lines), `docs/reports/engine_study_2026-10-28/A_cardinals.md`
