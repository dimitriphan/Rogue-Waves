# Rogue Waves mode: temporary UI art (2026-10-27, table brought up to date 2026-11-04)

**Everything in this folder is temporary.** It gives the mode's UI a look in the pack's palette and motifs
(`docs/STYLE.md`) until Dimitri's own art replaces it; the pack's whole list is `docs/TEMP_ART.md`. Icons are 12 x 12
like the game's `Icon_Types` icons and the ones Argo and the mode already add (kit `docs/03` §2, `docs/09` #20):
opaque, no outline, lit from the upper left, one object each.

| File (`game_size/`) | What it shows | For | In game |
| --- | --- | --- | --- |
| `RogueWaves_RisingWater.png` | a teal swell with a foam crest, an arrow rising beside it | weather Rising Water (scope 04) | **wired**: `Code/08_Icons.cs` rows; compiled, not yet seen |
| `RogueWaves_Quickening.png` | three foam chevrons over running water | weather Quickening Current | **wired** (same) |
| `RogueWaves_Nesting.png` | a coral egg in a sand nest | weather Nesting Ground | **wired** (same) |
| `RogueWaves_Crest.png` | the crest: one breaking curl with a Foam rim | the mode's icon (`STYLE.md` "The crest") | **wired** (2026-11-03): `Code/08_Icons.cs` |
| `RogueWaves_Crest32.png` | the crest at 32 x 32, drawn, not scaled | the main menu's Rogue Waves button (`docs/mode/MAIN_MENU_START.md`) and the New campaign card | **seen in game** (`Code/12_Menu.cs`) |
| `RogueWaves_PureAura.png` | a Pearl drop with its blue and pink sheen | the Pure Aura counter and rewards | **seen in game** (2026-11-03) |
| `RogueWaves_Depth.png` | a coral sounding lead on its line | Depth (ascension) marks | wired; waits for Depth, which is on hold |
| `RogueWaves_Retry.png` | a coral and foam life ring | the campaign's retries, the Continue card | **seen in game** (2026-11-03) |
| `RogueWaves_Vote.png` | a teal pennant on a pole | the chart vote | **seen in game** (2026-11-03) |
| `RogueWaves_Stay.png`, `_Leave`, `_Shop`, `_Fund`, `_Board` | the buttons of the leave vote, VENDCINT, Fund Transfer and the Board | those buttons | **seen in game**; drawn by `Assets/RogueWavesModeMod/Editor/RogueWavesModeArt.cs` when missing |
| `Vendcint-Sheet.png` | VENDCINT, a vending machine | the shopkeeper | **seen in game** (landing, panel, its jump away); drawn by the same editor file when missing |
| `Direction_Tougher.png` | a Stitched with a bandaid on its head, holding back tears | Made of Tougher Stuff (scope 11) | **seen in game** on the Direction cards (`Code/16_Directions.cs` reads `Direction_<Look>`) |
| `Direction_CompostBin.png` | a small grey bin of dirt with green stink lines | Compost Bin | **seen in game** (same) |
| `Direction_DandelionSeason.png` | a yellow dandelion with a jagged leaf | Dandelion Season | **seen in game** (same) |
| `Direction_MotherOfMushrooms.png` | a red cap over a growing-kit box | Mother of Mushrooms | **seen in game** (same) |
| `Direction_EfficientRecycler.png` | the recycling loop drawn in yarn | Efficient Recycler | **seen in game** (same) |
| `Direction_SewingMachine.png` | the classic light blue sewing machine | Sewing Machine | **seen in game** (same) |
| `Direction_Barnacle`, `_Mend`, `_Spring`, `_Cost`, `_Refit`, `_Interest`, `_Gallop`, `_Regulars`, `_Longline`, `_Packedsnow`, `_Skipping` (`.png`) | the mode's own Directions: Barnacle Coat, Tidal Mend, Spring Tide, Cut Costs, Cheap Refits, Compound Interest, and the base vessels' Full Gallop, House Regulars, Long Line, Packed Snow, Skipping Stones | the Direction cards | **seen in game**; drawn by `RogueWavesModeArt.cs` when missing |
| `Direction_Extension`, `_Brightbulb`, `_Batteries`, `_Flashbulb` (`.png`) | Uloe's Extension Cord, Brighter Bulb, Spare Batteries, Flash Bulb | the Direction cards | **seen in game**; drawn by `ModSource/Uloe/Tools/Make-DirectionIcons.ps1` |
| `Direction_Deepcurrent`, `_Heavysalvo` (`.png`) | Argo's Deep Current and Heavy Salvo (both showed the crest before) | the Direction cards | wired, compiled (2026-11-04); drawn by `ModSource/Argo/Tools/Make-DirectionIcons.ps1` |
| `Cardinal_VampireDentures.png` | cheap plastic fangs on red gums | Vampire Dentures, a Trinket since 2026-11-04 (scope 12; the file keeps its name) | **seen in game** |

`aseprite/RogueWaves_Icons.aseprite` holds 15 of the 12 x 12 icons (the three weathers, the crest, Depth, Pure Aura,
Retry, Vote, six Directions and Vampire Dentures), one frame and one tag each; `RogueWaves_Crest32.aseprite`
the big crest. Their layer is named "TEMP art". `preview_8x/` holds enlarged copies for review.

Not drawn yet, and why: the vote, shop, Direction-card and leave-prompt frames (the game's own choice dialog and panels
are reused, kit `docs/15` §15, so there is no frame slot to fill), the sea chart's islands (each map's own island
from the base game's world map, copied at run time in `Code/04_World.cs`; a shape only when none is found), and Cardinal or Direction art bigger than
12 x 12 (the size those slots show at hasn't been seen in game).

## Replacing it

Draw the icon at the same size and save it as `ModSource/RogueWavesMode/Art/<same name>.png`, one folder up. Tools > RogueWavesMode >
Build All Assets packs the PNGs into the sprite library; only the three weather icons are string rows in code. For them, run
`python3 concept-tools/temp_art/ui_icons.py` after changing its rows (it prints them in the form `08_Icons.cs` uses), or
ask a Claude thread to turn your PNG into rows. Delete the temporary file once yours is in.

**Versions to pick from (2026-10-28):** three new versions of every file above are in `variants/v1|v2|v3/` (its README names them); nothing there is wired until Dimitri picks (`docs/TEMP_ART.md`).

**Picks applied (2026-11-03):** the files in `game_size/` now carry Dimitri's picks among the versions (the list is in `docs/TEMP_ART.md`, "Dimitri's picks"), so where a row above describes art he replaced, the picked version's line in `variants/README.md` describes it now. Compiled, not yet seen in game.
