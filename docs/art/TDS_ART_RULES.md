# TDS art rules: the facts only, 2026-11-08

> **Replaced 2026-11-08 by `reset_2026-11-08/`** (Dimitri: "create a prompt that will have it reset all of its rulings"). Kept as history; ChatGPT no longer follows it.

Dimitri, 2026-11-08: "provide a new TDS art theme ruling that only has the factual rules, with nothing about what the
characters look like currently." These are the rules the base game's own art follows, measured on its sheets (kit
`docs/18` §1-3 and §6, kit `docs/03` §1-2), plus the pack's file and delivery specs (`ASSET_MANIFEST.md`,
`FEEDBACK_ROUND1_2026-11-07.md` rules 1-8). No character's look is described here: a character's design comes only from
its own spec (for Spool, `SPOOL_SPRITE_BREAKDOWN.md`) and Dimitri's word. `CHATGPT_HARD_RULES.md` keeps the workflow.

**How to use it.** Upload the block below to the ChatGPT Project as `TDS_ART_RULES.txt` (a copy is in project files,
`chatgpt/prompts/tds_art_rules_2026-11-08.txt`). Where it and an older note disagree on a style fact, this file wins.

```text
TDS ART RULES (FACTS ONLY, 2026-11-08)

These are the measured rules of the base game's art. They say how to draw, never what a character looks like: each character's look comes from its own spec and Dimitri's word.

A. CANVAS AND FILES
1. Pixel art at 20 pixels per game unit, nearest-neighbour (point) filtering, no compression. Pivot at the bottom centre of each cell.
2. Cells are square. Sheets are read left to right, one facing per row. File name, sheet size, cell size and row order exactly as ASSET_MANIFEST (guide Part 6).
3. Kinds and their cells:
   - Vessel: 32 px cell; drawing 15-27 px wide, 19-28 px tall, never taller than 28; never a bigger cell.
   - Small foe: 32 px cell, 8-19 x 9-17. Standard foe: 32 px, 18-25 x 20-28. Heavy foe: 48 px, 30-35 x 33-40. Boss foe: 64 px.
   - Minion of a vessel: 32 px cell, 13-15 x 16-19.
   - Unit: 16 px cell (12 or 24 where the manifest says), 6-12 x 8-12.
   - Mini vessel (boat select): 16 px cell, about 7 x 10.
   - Icon: 12 x 12 (a crest also 32 x 32). Ready-up face: 12 x 12. End-screen pose: 32 x 32. Journal tab: 32 x 32. Splash: 128 x 128.
4. Placement: feet on the bottom row of the cell in every ground frame (units may bob 1-3 px); the drawing's box centred on the cell's middle column (column 16 of 32), within 1 px; never touching the cell's sides.
5. Vessel sheets: main sheet 192 x 256 (6 frames x 8 rows: idle v0-v3, move v0-v3); attack 128 x 128 (4 frames x 4 rows); ability 192 x 128 (6 frames x 4 rows); land 384 x 32 (12 frames, 1 row); fall 384 x 32 (12 frames, 1 row); menu idle 192 x 32 (6 frames); mini 96 x 128 (16 px cells, 6 x 8).
6. Facings: v0 toward the camera heading screen-right; v1 toward the camera heading left; v2 away heading right; v3 away heading left. v2 and v3 are true back views. A row marked "mirror of" is the exact silhouette mirror of its source, re-shaded so the shade stays on the right and underside; anything that must not swap sides is redrawn.

B. SHAPE AND PROPORTION
7. Vessels are chibi: the head or head-mass is about half the height or more; the body is small; hands small or stubby; legs short (about 2-3 px) or none.
8. The silhouette must read at 1x zoom. Simple shapes beat texture; one signature prop or feature reads better than many details.
9. Opaque only: every pixel alpha 0 or 255. The one exception is a splash's inside fill at alpha 70.
10. No outline: no dark line around the body. The edge of a form is its own fill colour, darkened only where that side is in shade.

C. COLOUR
11. Never pure black 000000. The darkest pixels are near-blacks or dark tones of a hue (for example 2B2922, 333232, 4A4949, 161616).
12. Every ramp keeps its hue within 2 degrees. A shade is the same hue with up to 25 points less value and up to 23 points more saturation. The one exception: white shades to a pale cold blue.
13. Two or three tones per material, rarely four. A vessel uses 10-21 colours in its whole sheet; a foe 8.
14. A vessel's main colour sits at 90-100% value and under 60% saturation; only shades, accents and darks go lower. One or two saturated accents.
15. Neutrals, wood, skin and common accents come from the shared world palette (03_palettes.txt, section shared) so a new character lives in the same world.
16. Reserved colours: hue 270-320 (magenta and purple) only on foes; the 8 foe colours belong to the foe family; Pearl F4EEF8 only for Pure Aura and the PearlOyster pearl.
17. Icons sit on the panel colour 2E4A62: never paint 2E4A62 or 22384B into an object, and keep an object's darkest tone at least 20 luminance points away from 2E4A62.

D. LIGHT AND SHADING
18. Light comes from the upper left. The left and top edges of each form keep the fill colour; the right side and the underside of each form take the shade; whatever overlaps casts a row of shade under it (a brim on a face, a body over legs).
19. On base vessels 32-45% of right-edge pixels and 20-57% of bottom-edge pixels are darker than the pixel inside them, against 5-13% on the left edge. Keep within those ranges.
20. Almost no highlights: shading is a band of shade, not a gradient. No dithering, no noise, no anti-aliasing, no painted texture. At most 13% of opaque pixels may be lone specks (a pixel with no same-colour neighbour).

E. ANIMATION
21. Vessel folders and frames: Idle 6, Move 6, Fall 6 or 12, Land 12-15 (one facing), Attack 2-4, an ability 6. Vessels play at 8-10 frames a second.
22. Idle is small: 6 distinct frames; 2-220 pixels change between frames; the head top moves 0-3 px (a breath: down on frames 2-3, up on 4-5); one secondary motion.
23. Move is big: 45-350 pixels change between frames; the head bobs 2-7 px. The landing frames are 2 and 5 (the game plays the footstep there): legs apart, body down; the passing frames between them: legs together, body up.
24. Attack: anticipation (lean back 1-2 px), strike (the biggest pose, a pale flash allowed), recovery. 200-780 pixels change between frames.
25. Land (12 frames): frames 0-3 airborne above the cell bottom; 4-6 squash to about 60% height on impact; 7-10 unfold and overshoot 1 px; frame 11 equals idle_v0 frame 0. All distinct.
26. Fall (12 frames): a mid-air loop, never a collapse: on at least 10 of 12 frames the lowest pixel is 3+ px above the bottom row; the body turns 30-45 degrees per frame; frame 11 flows into frame 0.
27. Airborne frames are the only frames off the bottom row; list each one's lift in px.
28. Every frame of a sheet is an edit of the approved key frame: same size, same palette, same parts.

F. UI ART
29. Ready-up (12 x 12): the character's face inside the white corner brackets the base ready-up icons use.
30. Splash (128 x 128): line art, not a filled figure: a 3 px accent outline, an inset line in the body colour, a translucent fill of the accent at alpha 70, one shape per body part.
31. Journal tab (32 x 32): a white FFFFFF 1 px line drawing of the head, open interior, clear ground. No fill, no colour.
32. End-screen poses (32 x 32): 1 happy, 2 defeated (a slumped pose, not only new eyes).
33. Icons (12 x 12): one found object filling most of the square, readable at 1x; no UI symbols (bar charts, crosses, arrows), no numbers, no text.

G. FOES (the family rule)
34. Foes share one palette of 8 colours and the reserved magenta band; they have no face (a 2 x 2 socket) and no expression; their light is faceted (light top and left faces, a pale ridge, a dark far side); 3-12 frames a second by weight. Friends never borrow any of it.

H. NEVER
35. Never copy a base-game pixel into a pack asset. Never scale a sprite up or down to fit: redraw at the size. Never use a gradient, glow, blur or soft brush. Never gore.
```
