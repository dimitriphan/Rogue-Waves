# Yossele, The Patient Golem ▪ Intermediate (0.1.0-dev)

A vessel for the Rogue Waves pack (*TDS - Tower Defense Strategy*): a round clay golem on root limbs whose buildings
grow stronger for every Tide they survive. Ships only inside the pack (`docs/PACK.md`). **Temporary art; passes its
in-game self-test (91/0), not yet played online** (`IMPLEMENTATION_NOTES.md`).

## The vessel

- Sturdy melee: 60 health, 4.5 damage, and it mends itself 1 health a second while hurt.
- **Growth** (the special rule): each building gains a stage at the start of every Tide it stood through, up to its
  own maximum. Destroyed or sold, it is gone; a new one starts over.
- **FERTILIZE**: once a Compost Bin Direction is held, foes felled near Yossele make fertilizer; spend one to grow a
  building of yours a stage at once.

## Buildings

| Building | Cost | What it does | Max Growth |
|---|---|---|---|
| Mushroom Cannon | Red 35 | Long-range lobbed spore bombs; spores stack and rot foes. Fully grown, the bombs bounce | 5 |
| Primordial Dandelion Tree | Red 50 | One per player. Grows seeds that all strike at once; each stage adds seeds and range | 10 |
| Overgrown Stonehenge | Blue 25 | Turn it while placing: its vines creep that way each stage and slow foes; it hardens as it grows | 8 |
| Pachira Sapling | Yellow 35 | Its roots mine every ore node in reach; fully grown, a mined-out node drops an aura fruit | 5 |

Numbers and upgrade trees: `DESIGN.md`. Every value the scope left open: `docs/YOSSELE_BUILD_NOTES.md`.
