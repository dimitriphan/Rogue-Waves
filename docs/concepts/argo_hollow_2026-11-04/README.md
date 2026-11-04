# Argo Hollow, round 2: new head and body shapes (concept sprites, 2026-11-04)

Round 1 (`docs/concepts/argo_divers_2026-11-04/`): Dimitri picked **Hollow** on the picks page ("Hollow, but play around
with the body shape and size more with more concepts") and in the thread, 05:10Z: "Create more concepts based on the
hollow choice, but play around more with the body and head's shapes."

Every take keeps Hollow's idea: nobody in the suit, the helmet full of seawater (a pocket of air, a bright surface line,
two pale lights adrift, a small fish), the faded suit, empty gloves, the last air bubbling out. The silhouette changes:

| Take | Head | Body | Size | Watch |
| --- | --- | --- | --- | --- |
| **Bell Jar** | tall bell jar, tall oval port, sea standing high | thin sack slumping wider at the hem, long sleeves to the ground | 15 x 27 | narrowest on the boat, near Uloe's tall-thin outline |
| **Fish Tank** | wide flat-topped tank, big square window, kelp inside, lid bolts | stubby, mostly head | 21 x 26 | water reads best at 1x; the dome becomes a box; bolts read like ears |
| **Slump** | sunk low into the suit and tipped back, port looking down | a deflated pear settled on the floor, no legs, sleeves spilt to the sides | 24 x 25 | lowest and widest; Move needs a shuffle or slide |
| **Fishbowl** | a glass globe of sea in the brass collar, brass cap, glints | a coat hung as on a hanger, square shoulders | 16 x 27 | drops the brass dome (Argo's motif), collar and cap carry it |
| **Hunch** | slid forward off the collar, nodding down | bent under the helmet, one arm dragging, a cut air hose to the floor | 22 x 26 | lopsided: v1 must be drawn, not mirrored |

Picks page (round 2 on top, round 1 below): https://claude.ai/artifact/FnchhGQBHvi9JyhC47TCe2, db collection `picks`,
doc `hollow` (choice = `BellJar|FishTank|Slump|Fishbowl|Hunch`, note); round 1 stays in doc `argo`.

## Rules checked (kit `docs/18` §4.2)

Every frame: 32 px cell, feet (or hem) on the bottom row, centred on column 16 (±0.5), 25-27 px tall, 15-24 px wide,
fully opaque, flat shading as on the shipped vessels (lit left edge, two-pixel shade band right, shade row below), 7-18
colours, no pure black, nothing in the foes' magenta band. Kept from 1.4.0: pale lights in a dark port.
`hollow_shapes.py` prints the check per frame.

## Files

`art/<Take>/`: `game_size/Argo_Hollow_<Take>.png` (224 x 32: Idle v0 0-5, back v2), `aseprite/` (tags `Idle_v0`,
`Back_v2`, layer "CONCEPT art"), `preview_8x/` (8x sheet, idle GIF). `art/argo_hollow_lineup_8x.png`,
`art/argo_hollow_lineup_1x_x3.png` and `art/argo_hollow_idle.gif` put round 1's Hollow first for comparison.
Rebuild: `python3 concept-tools/argo_divers/hollow_shapes.py`, then `build_page.py` (republish to the same link).
Copies in project files `concepts/argo_divers_2026-11-04/hollow/`.
