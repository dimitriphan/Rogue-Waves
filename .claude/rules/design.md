---
paths:
  - "**/DESIGN.md"
  - "**/DESIGN_TEMPLATE.md"
  - "docs/concepts/**"
  - "TDS_Mod/ModSource/*/Code/02_Data.cs"
---
# Designing a vessel

Follow `TDS_Vessel_Creator/docs/02_DESIGN_RULES.md` section by section (identity, stat bands, 4 buildables with cost
colour = job, 7-upgrade trees with 2 roots, crossover, an early end and a tier-5 capstone, no numbers in upgrade text).
`docs/12` for units. What the engine can't do and the substitutes that worked: `examples/Mummer_DESIGN.md`.

Shape language already taken (keep the new one distinct, carry it into the towers): Smacky square, Sno Sno round,
Buster dog, Boutman cat in overalls, Fie knight, Sprigg bulb in a pot, Argo bulky dome diver, Mummer triangle costume,
Crackpot cracked orb + spiral staff, Rustle horned mask + leaf cloak to the floor, Broodie forked crescent horn,
Yossele holed round clay head on root limbs, Spool hooded plush with a stitched face opening, Uloe glass bulb
on a corded lamp post (Dimitri's concept art, `docs/concepts/YOSSELE_SPOOL_ULOE.md`)
(`docs/STYLE.md` has the pack roster's references, colours and tower motifs).

Shipped mod values to calibrate against: Mummer 45 HP / 2.75 speed / 3.5 dmg / range 2 / 0.6 atk; Argo 65 / 2.25 / 5 /
1.75 / 0.4. A vessel's `DESIGN.md` must match its `Code/02_Data.cs`.

Respect the user's concept: keep its fantasy and names, change only what is infeasible, unreadable or unbalanced, and
list each change under "Changes from the concept, and why".
