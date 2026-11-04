# Argo: minion versions to pick from (2026-11-04)

Dimitri, 2026-11-04: "Create new sprites for all modded minions. They should be closer in size to snobles. Create at least 10 versions of each." **Everything here is temporary and nothing is wired**: the art in game stays as it is until he picks (`docs/TEMP_ART.md`, "Minion versions"). Picks page: https://claude.ai/artifact/Rj9xQXsYUnauTGQyiFfAJX.

Size: the Snoble is 6-8 px wide and 5-8 px tall in a 12 px cell (measured on the game's sheet, kit `docs/18` section 2). Every version keeps the wired sheet's name, size and frame layout; only the body in each cell is smaller. Drawn by `concept-tools/minions/` (`argo.py`; `build.py` writes this folder).

Not wired: the six Naut sheets are drawn by code (`ArgoCharacter.NautSheet`, `NautTuneSheet`) and ArgoBuilder has no file override yet. A pick needs Yossele's `FileArt` in `ArgoBuilder.SaveSheet`, then the six picked sheets copied to `Art/Temporary/game_size/`. Same names, sizes and layout as the code's sheets (96 x 64 each). Argo himself keeps his shipped look (Dimitri, 2026-11-04 05:31Z); only his crew is redrawn here.

Each `<minion>/vNN/` has `game_size/` (the sheet), `aseprite/` (one frame per cell, a tag per row and animation, layer "TEMP art") and `preview_8x/` (on a checker). GIFs of every version are in the project files, `/mnt/project-files/concepts/minions_2026-11-04/`.

## Naut (`naut/`)

Today: 9 x 11 px at rest. Sheets: `Naut_Port-Sheet`, `NautTune_Port-Sheet`, `Naut_Starboard-Sheet`, `NautTune_Starboard-Sheet`, `Naut_Bosun-Sheet`, `NautTune_Bosun-Sheet`.

| Version | Name | Size at rest | Look |
| --- | --- | --- | --- |
| v01 | Little Diver | 8 x 9 | Today's Naut at Snoble size: a brass dome with a porthole, a crew-coloured suit, dark boots. |
| v02 | Big Dome | 10 x 11 | A helmet far too big for the little body under it; the porthole takes up its whole front. |
| v03 | Running Light | 8 x 11 | A crew-coloured lamp sits on top of the dome, so Port glows red, Starboard green, Bosun gold. |
| v04 | Diving Bell | 9 x 10 | The whole Naut is a little brass bell with a porthole, walking on two short legs; a crew band rings it. |
| v05 | Snorkel | 7 x 12 | No helmet, a crew-coloured hood with big round goggles and a snorkel that bobs as it walks. |
| v06 | Hard Suit | 8 x 10 | A white steel suit with ball joints and a small dome; a crew-coloured light glows on its chest. |
| v07 | Air Hose | 10 x 9 | An air hose runs from the back of its dome down to the ground and drags behind it. |
| v08 | Fish Tank | 8 x 10 | The dome is clear glass, and the Naut inside is a little crew-coloured fish. |
| v09 | Flippers | 8 x 9 | Big crew-coloured flippers that slap down as it waddles. |
| v10 | Buoy | 8 x 12 | A round buoy for a body, striped crew colour and white, with a little brass dome on top. |
