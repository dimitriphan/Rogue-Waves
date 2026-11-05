# Uloe: minion versions to pick from (2026-11-04)

Dimitri, 2026-11-04: "Create new sprites for all modded minions. They should be closer in size to snobles. Create at least 10 versions of each." **Everything here is temporary and nothing is wired**: the art in game stays as it is until he picks (`docs/TEMP_ART.md`, "Minion versions"). Picks page: https://claude.ai/artifact/Rj9xQXsYUnauTGQyiFfAJX.

Size: the Snoble is 6-8 px wide and 5-8 px tall in a 12 px cell (measured on the game's sheet, kit `docs/18` section 2). Every version keeps the wired sheet's name, size and frame layout; only the body in each cell is smaller. Drawn by `concept-tools/minions/` (`uloe.py`; `build.py` writes this folder).

Not wired: `Uloephoton-Sheet` is drawn by code (`UloeCharacter.PhotonSheet`). `UloeBuilder.SaveSheet` already takes a file over it (`FileArt`: `Art/<name>.png`, else `Art/Temporary/game_size/<name>.png`, when it has the code sheet's size), so a pick only needs the picked sheet copied to `Art/Temporary/game_size/`. Same name, size and layout as the code's sheet (160 x 64).

Each `<minion>/vNN/` has `game_size/` (the sheet), `aseprite/` (one frame per cell, a tag per row and animation, layer "TEMP art") and `preview_8x/` (on a checker). GIFs of every version are in the project files, `/mnt/project-files/concepts/minions_2026-11-04/`.

## Photon (`photon/`)

Today: 12 x 10 px at rest. Sheets: `Uloephoton-Sheet`.

| Version | Name | Size at rest | Look |
| --- | --- | --- | --- |
| v01 | Spark | 10 x 9 | Today's Photon at Snoble size, a glowing yellow orb with two electric wisps and a flickering tail. |
| v02 | Mini Bulb | 6 x 8 | Uloe in small, a glass bulb with a glowing heart on a ribbed screw base; it hops like Sno Sno. |
| v03 | Firefly | 7 x 5 | A little ink-blue beetle with a big glowing tail light and two glassy wings that flutter. |
| v04 | Star | 7 x 7 | A four-point star that turns as it drifts, flashing from a plus to a cross. |
| v05 | Flame | 6 x 8 | A candle flame with a white heart; its tip flickers left and right. |
| v06 | Filament | 6 x 9 | A drop of glass with a glowing zig-zag filament inside for a smile. |
| v07 | Halo | 8 x 5 | A small orb wearing a tilted ring of light that turns as it floats. |
| v08 | Comet | 8 x 5 | A bright head trailing a long tail of light that streams back as it moves. |
| v09 | Prism | 7 x 7 | A three-sided glass shard with a beam of light caught inside it. |
| v10 | Plug | 9 x 7 | A little plug on two prong legs, its face lit, a cord tail trailing behind. |
