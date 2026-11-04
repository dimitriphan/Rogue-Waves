# Yossele: minion versions to pick from (2026-11-04)

Dimitri, 2026-11-04: "Create new sprites for all modded minions. They should be closer in size to snobles. Create at least 10 versions of each." **Everything here is temporary and nothing is wired**: the art in game stays as it is until he picks (`docs/TEMP_ART.md`, "Minion versions"). Picks page: https://claude.ai/artifact/Rj9xQXsYUnauTGQyiFfAJX.

Size: the Snoble is 6-8 px wide and 5-8 px tall in a 12 px cell (measured on the game's sheet, kit `docs/18` section 2). Every version keeps the wired sheet's name, size and frame layout; only the body in each cell is smaller. Drawn by `concept-tools/minions/` (`yossele.py`; `build.py` writes this folder).

A pick is a copy: put the picked `game_size/Yosselefriend-Sheet.png` over `Art/Temporary/game_size/` (the wired file, `YosseleBuilder.CopySheet`) and its `.aseprite` over `aseprite/`, then Build All Assets.

Each `<minion>/vNN/` has `game_size/` (the sheet), `aseprite/` (one frame per cell, a tag per row and animation, layer "TEMP art") and `preview_8x/` (on a checker). GIFs of every version are in the project files, `/mnt/project-files/concepts/minions_2026-11-04/`.

## Cordyfriend (`cordyfriend/`)

Today: 14 x 21 px at rest. Sheets: `Yosselefriend-Sheet`.

| Version | Name | Size at rest | Look |
| --- | --- | --- | --- |
| v01 | Puffball | 8 x 11 | Today's Cordyfriend at Snoble size. A cream puff on two nubs, three orange clubs on its crown. |
| v02 | Feeler | 8 x 13 | A legless puff that hops like a Snoble, one long club bending over its head like an antenna. |
| v03 | Toadstool | 10 x 8 | An orange cap with cream spots worn like a hat over a squat cream body; eyes peek under the brim. |
| v04 | Antlers | 8 x 11 | Two forked orange growths like little antlers; it charges head first. |
| v05 | Spore Cloud | 12 x 10 | Short stubby clubs and three olive spore motes circling it as it breathes. |
| v06 | Snowball Host | 8 x 10 | A snow-white puff that hops like a Snoble, orange clubs bursting out of its back. |
| v07 | Morel | 8 x 10 | A tall honeycombed morel cap on a pale little body; the pits catch the shade. |
| v08 | Moss Lump | 8 x 9 | An olive mossy body with bright orange club tips poking out all over its back. |
| v09 | Cluster | 8 x 12 | Two little puffballs have budded on its shoulder and ride along, each with an orange club. |
| v10 | Glowcap | 8 x 11 | Lime clubs that glow like Yossele's eyes, with glowing eyes to match. |
