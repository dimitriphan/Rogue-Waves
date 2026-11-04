# Argo Hollow, round 3: ten new bodies (concept sprites, 2026-11-04)

Round 2 (`docs/concepts/argo_hollow_2026-11-04/`) did not land. Dimitri, 05:21Z: "None of the round 2 versions appeal
to me. Try 10 new designs." His round 1 note still stands: "Hollow, but play around with the body shape and size more
with more concepts".

So round 3 goes back to round 1's Hollow, which he picked. Every design keeps that Hollow's parts:

- Argo's round, riveted brass dome, with the valve on the crown, a lit streak and the side bolt.
- The breastplate rivets.
- The porthole full of sea: a pocket of air, the surface line, two pale lights adrift and a small fish.
- The faded suit and the empty gloves.

What changes is how big the helmet is and how the body is shaped, sized and weighted:

| Design | Helmet | Body | Size | Watch |
| --- | --- | --- | --- | --- |
| **Big Head** | huge, the biggest thing on him | tiny empty suit, short legs | 23 x 27 | reads as cute; the port and water read best |
| **Long Legs** | small | short body on long thin legs | 18 x 26 | thin legs need a careful walk cycle |
| **Lead Boots** | normal | the suit tapers down to huge lead boots with brass straps | 19 x 26 | a heavy, stomping walk |
| **Waterlogged** | normal | swollen round with seawater, stretched seams, leaking cuffs | 22 x 27 | drips need their own frames |
| **Broad** | normal | wide shoulders under strapped lead weights, thin waist | 25 x 26 | wide on the boat |
| **Air Tank** | normal | a brass tank on his back shows past the shoulder, with a hose to the helmet; the bubbles come from the tank | 21 x 27 | the tank sits on one side, so v1 must be drawn rather than mirrored |
| **Stout** | broad, sitting low | short and squat on stubby boots | 26 x 23 | lowest of the ten |
| **Lopsided** | sagged to one side, valve bent | one long sleeve and one empty cuff pouring sea | 23 x 26 | lopsided, so v1 must be drawn rather than mirrored |
| **Bell Skirt** | normal | flares like a diving bell over a riveted brass hem; only the toes show | 24 x 27 | Move needs a glide or shuffle |
| **Long Arms** | normal | long empty sleeves reach the floor and the gloves drag | 23 x 26 | the arms drag through the walk |

The picks page has round 3 on top, with rounds 2 and 1 folded below it:
https://claude.ai/artifact/FnchhGQBHvi9JyhC47TCe2. The pick is stored in db collection `picks`, doc `hollow3`
(choice = `BigHead|LongLegs|LeadBoots|Waterlogged|Broad|AirTank|Stout|Lopsided|BellSkirt|LongArms`, plus a note).

## Rules checked (kit `docs/18` §4.2)

Every frame meets these rules:

- 32 px cell, feet on the bottom row, centred on column 16.
- 23-27 px tall and 18-26 px wide.
- Fully opaque.
- Flat shading: lit edge on the left, shade on the right.
- No pure black, and nothing in the foes' magenta band.

`hollow_ten.py` prints the check for every frame.

## Files

`art/<Design>/` holds three versions of each design:

- `game_size/Argo_Hollow_<Design>.png`: 224 x 32, with Idle v0 in frames 0-5 and the back view (v2) last.
- `aseprite/`: tags `Idle_v0` and `Back_v2`, on the layer "CONCEPT art".
- `preview_8x/`: an 8x sheet and an idle GIF.

`art/argo_hollow_ten_lineup_6x.png`, `art/argo_hollow_ten_lineup_1x_x3.png` and `art/argo_hollow_ten_idle.gif` put
round 1's Hollow first, for comparison.

To rebuild, run `python3 concept-tools/argo_divers/hollow_ten.py`, then `build_page.py`, and republish to the same
link. Copies are in project files under `concepts/argo_divers_2026-11-04/hollow_ten/`.
