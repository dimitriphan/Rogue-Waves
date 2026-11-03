# Rogue Waves mode: temporary UI art (2026-10-27)

**Everything in this folder is temporary.** It gives the mode's UI a look in the pack's palette and motifs
(`docs/STYLE.md`) until Dimitri's own art replaces it; the pack's whole list is `docs/TEMP_ART.md`. Icons are 12 x 12
like the game's `Icon_Types` icons and the ones Argo and the mode already add (kit `docs/03` §2, `docs/09` #20):
opaque, no outline, lit from the upper left, one object each.

| File (`game_size/`) | What it shows | For | In game |
| --- | --- | --- | --- |
| `RogueWaves_RisingWater.png` | a teal swell with a foam crest, an arrow rising beside it | weather Rising Water (scope 04) | **wired**: `Code/08_Icons.cs` rows; compiled, not yet seen |
| `RogueWaves_Quickening.png` | three foam chevrons over running water | weather Quickening Current | **wired** (same) |
| `RogueWaves_Nesting.png` | a coral egg in a sand nest | weather Nesting Ground | **wired** (same) |
| `RogueWaves_Crest.png` | the crest: one breaking curl with a Foam rim | the mode's icon (`STYLE.md` "The crest") | not wired: no slot yet |
| `RogueWaves_Crest32.png` | the crest at 32 x 32, drawn, not scaled | the main menu's Rogue Waves button (`docs/mode/MAIN_MENU_START.md`) | not wired: the button is built after Release 1 |
| `RogueWaves_PureAura.png` | a Pearl drop with its blue and pink sheen | the Pure Aura counter and rewards | not wired: Pure Aura is Phase 5 |
| `RogueWaves_Depth.png` | a coral sounding lead on its line | Depth (ascension) marks | not wired |
| `RogueWaves_Retry.png` | a coral and foam life ring | the campaign's retries | not wired |
| `RogueWaves_Vote.png` | a teal pennant on a pole | the chart vote | not wired |
| `Direction_Tougher.png` | a Stitched with a bandaid on its head, holding back tears | Made of Tougher Stuff (scope 11) | not wired |
| `Direction_CompostBin.png` | a small grey bin of dirt with green stink lines | Compost Bin | not wired (Yossele's Compost Bin Directions have no icon yet) |
| `Direction_DandelionSeason.png` | a yellow dandelion with a jagged leaf | Dandelion Season | not wired |
| `Direction_MotherOfMushrooms.png` | a red cap over a growing-kit box | Mother of Mushrooms | not wired |
| `Direction_EfficientRecycler.png` | the recycling loop drawn in yarn | Efficient Recycler | not wired |
| `Direction_SewingMachine.png` | the classic light blue sewing machine | Sewing Machine | not wired |
| `Cardinal_VampireDentures.png` | cheap plastic fangs on red gums | Vampire Dentures (scope 12) | not wired |

`aseprite/RogueWaves_Icons.aseprite` holds every 12 x 12 icon, one frame and one tag each; `RogueWaves_Crest32.aseprite`
the big crest. Their layer is named "TEMP art". `preview_8x/` holds enlarged copies for review.

Not drawn yet, and why: the vote, shop, Direction-card and leave-prompt frames (the game's own choice dialog and panels
are reused, kit `docs/15` §15, so there is no frame slot to fill), the sea chart's islands (built from shapes at run time in
`Code/04_World.cs`; a better look is model work for the chart's next change), and Cardinal or Direction art bigger than
12 x 12 (the size those slots show at hasn't been seen in game).

## Replacing it

Draw the icon at the same size and save it as `ModSource/RogueWavesMode/Art/<same name>.png`, one folder up. The mode has
no asset builder: its icons are string rows in code. For the three weather icons, run
`python3 concept-tools/temp_art/ui_icons.py` after changing its rows (it prints them in the form `08_Icons.cs` uses), or
ask a Claude thread to turn your PNG into rows. Delete the temporary file once yours is in.

**Versions to pick from (2026-10-28):** three new versions of every file above are in `variants/v1|v2|v3/` (its README names them); nothing there is wired until Dimitri picks (`docs/TEMP_ART.md`).

**Picks applied (2026-11-03):** the files in `game_size/` now carry Dimitri's picks among the versions (the list is in `docs/TEMP_ART.md`, "Dimitri's picks"), so where a row above describes art he replaced, the picked version's line in `variants/README.md` describes it now. Compiled, not yet seen in game.
