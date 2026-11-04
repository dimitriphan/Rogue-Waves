# Argo, a diver who might not be human (concept sprites, 2026-11-04)

Dimitri, 2026-11-04 04:50Z: "Create 5 new sprite arts of Argo based on his idea of being a deep sea diver that might
not entirely be human." Five takes, each drawn over Argo's shipped standing frame (`TDS_Mod/Assets/ArgoMod/Editor/ArgoCharacter.cs`,
the 1.4.0 look Argo 2.0 keeps: round brass dome, porthole on the near half, riveted breastplate, blue suit, brass
mitts on the hips, dark boots), so the bulky dome diver stays his silhouette (`docs/STYLE.md`). Each take changes one
thing: how much of him is still human, what era the suit is, or what the sea has done to him. Lore line they build on
(scope `09`): "Argo went down to find the bottom of the Basin and never quite came back up."

**Picks page:** https://claude.ai/artifact/FnchhGQBHvi9JyhC47TCe2 (animated, every take front, back and at 2x; his pick
and note are saved in its db collection `picks`, doc `argo`, fields `choice` = `Barnacled|Angler|Hollow|Passenger|Hull|Shipped`
and `note`). Nothing is wired: these are concepts, not temporary art, and Argo's mod is unchanged.

| Take | How human | What changed | Idle | Watch |
| --- | --- | --- | --- | --- |
| **Barnacled** | mostly | verdigris on the brass, barnacles breaking the dome's edge and on the shoulder, kelp snagged on the valve, a starfish on the chest | kelp sways, a bubble | closest to 1.4.0 (an overlay on every existing animation); barnacles speckle at 1x |
| **Angler** | half | the valve grown into an anglerfish lure arching over the porthole, wide fish eyes and a toothy mouth line, a dorsal fin on the dome, webbed fins for mitts | lure glows and dips, near fin flicks | 28 px tall, the ceiling; the wrench swing needs a gripping fin |
| **Hollow** | none | helmet full of seawater (air pocket, surface line), two pale lights adrift, a small fish, faded suit, no shoulders, empty gloves hanging | fish crosses, lights drift, last air bubbles out, a drip | quietest; from far it passes for 1.4.0 |
| **Passenger** | none, someone else is home | one big gold eye with an octopus slit pupil, coral tentacles out of the cuffs curled to the hips, a third from under the collar | tips curl, pupil slides toward you, blink | coral near Storm Coral (pack danger mark); 24 px wide |
| **Pressure Hull** | hard to say | a later, rounder hard suit: steel barrel body, ball joints, pincers on the hips, smaller brass dome with a big port and a side port, a light for a face | the light swells and fades, the chest seam glows in time | biggest change (blue suit and mitts gone); white steel near Sno Sno's white; cyan `19DFCE` is Buster's collar teal |

## Rules checked (kit `docs/18` §4.2)

Every frame: 32 px cell, feet on the bottom row, centred on column 16 (±0.5), 19-28 px tall (27-28), 19-24 px wide,
fully opaque (the 1.4.0 half-alpha rim became the fill colour; no ground patch), light from the upper left with shade
on the right and under overlaps, 7-21 colours, no pure black, nothing in the foes' magenta band (h270-320). Kept from
1.4.0 by `docs/STYLE.md` ("look unchanged"): the pale eyes in a dark porthole instead of near-black dots.
`argo_divers.py` prints the check for every frame.

## Files

`art/<Take>/`: `game_size/Argo_<Take>.png` (224 x 32: Idle v0 frames 0-5, then the back v2), `aseprite/Argo_<Take>.aseprite`
(tags `Idle_v0`, `Back_v2`, layer "CONCEPT art"), `preview_8x/Argo_<Take>_8x.png` and `Argo_<Take>_idle.gif`.
`art/argo_divers_lineup_8x.png` (shipped + five, front and back), `art/argo_divers_lineup_1x_x3.png` (game size on the
boat's grey), `art/argo_divers_idle.gif` (all six idling). Copies in project files `concepts/argo_divers_2026-11-04/`.

Rebuild: `python3 concept-tools/argo_divers/argo_divers.py` (art), then `python3 concept-tools/argo_divers/build_page.py`
(the picks page, written to project files; republish it to the same link).

## When he picks

The pick is a direction, not a finished sheet. Next: draw the full set at that look (Idle, Move, Attack, Land, Fall,
World, v0-v3 with v1/v3 redrawn where a prop or the shaded side swaps, kit `docs/18` §4.2) in `ArgoCharacter.cs` or as
Aseprite frames over it (`TDS_Mod/Tools/aseprite/README.md`), carry the motif into the towers (`docs/STYLE.md` "Towers
carry"), then a PC round to see it in game.
