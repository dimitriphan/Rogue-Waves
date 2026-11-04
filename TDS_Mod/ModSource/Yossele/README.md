# Yossele, The Patient Golem ▪ Intermediate (0.2.0-dev)

A vessel for the Rogue Waves pack (*TDS - Tower Defense Strategy*): a round clay golem on root limbs whose buildings
grow stronger for every Tide they survive. Release 1 of the pack holds Yossele alone (`docs/PACK.md`). **0.2.0-dev is
built from Dimitri's 2026-10-20 sheet with temporary art, passed its in-game self-test (PC round 9e, 160/0; 173/0 on
2026-11-04).** Since 2026-11-03 the PC's Mods folder holds the release build of every pack part for his play; the
earlier play build is parked (`IMPLEMENTATION_NOTES.md`). The melee 0.1 build (PC round 8, 98/0) is history
(`docs/YOSSELE_BUILD_NOTES.md`).

## The vessel

- Sturdy ranged: 70 health, a green mouth laser for 5.5 at 4 tiles, slow on its root legs (1.75), and it mends itself
  1 health a second while hurt.
- **Growth** (the special rule): each building gains a stage at the start of every Tide it stood through (5 stages on a
  map of 4 Tides or fewer), up to its own maximum. Destroyed or sold, it is gone; a new one starts over.
- **FERTILIZE** (with the mode only): once a Compost Bin Direction is held, foes felled near Yossele make fertilizer;
  spend one to grow a building of yours a stage at once. Release 1 has no Directions, so no FERTILIZE.

## Buildings

| Building | Cost | What it does | Max Growth |
|---|---|---|---|
| Mushroom Cannon | Red 15 | Long-range lobbed spore bombs; spores stack and rot foes. Fully grown, the bombs bounce. Its last upgrade, Cordyceps, raises fallen foes as Cordyfriends | 5 |
| Primordial Dandelion Tree | Red 50 | One per player. Seeds grow and all strike when foes come near; each stage adds seeds and range. Upgrades can turn it on foes' projectiles (D-MDS) or plant a Bonsai copy | 15 |
| Overgrown Stonehenge | Blue 25 | Turn it while placing: its vines creep that way every 2 stages and slow foes; it hardens as it grows | 8 |
| Pachira Sapling | Yellow 35 | Its roots mine every ore node in reach; fully grown, each payout drops an aura fruit beside the node | 5 |

Numbers, upgrade trees and every default: `DESIGN.md`. What is still to check: `docs/reports/YOSSELE_TEST_PLAN_2026-10-23.md`.
