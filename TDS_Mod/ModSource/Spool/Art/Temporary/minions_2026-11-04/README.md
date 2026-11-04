# Spool: minion versions to pick from (2026-11-04)

Dimitri, 2026-11-04: "Create new sprites for all modded minions. They should be closer in size to snobles. Create at least 10 versions of each." **Everything here is temporary and nothing is wired**: the art in game stays as it is until he picks (`docs/TEMP_ART.md`, "Minion versions"). Picks page: https://claude.ai/artifact/Rj9xQXsYUnauTGQyiFfAJX.

Size: the Snoble is 6-8 px wide and 5-8 px tall in a 12 px cell (measured on the game's sheet, kit `docs/18` section 2). Every version keeps the wired sheet's name, size and frame layout; only the body in each cell is smaller. Drawn by `concept-tools/minions/` (`spool.py`; `build.py` writes this folder).

A pick is a copy: put the picked `game_size/<sheet>.png` over `Art/Temporary/game_size/` (the wired files, `SpoolBuilder` `FileArt`) and its `.aseprite` over `aseprite/`, then rebuild Spool's assets.

Each `<minion>/vNN/` has `game_size/` (the sheet), `aseprite/` (one frame per cell, a tag per row and animation, layer "TEMP art") and `preview_8x/` (on a checker). GIFs of every version are in the project files, `/mnt/project-files/concepts/minions_2026-11-04/`.

## Thread Head (`threadhead/`)

Today: 15 x 21 px at rest. Sheets: `Spoolhead-Sheet`.

| Version | Name | Size at rest | Look |
| --- | --- | --- | --- |
| v01 | Yarn Ball | 13 x 11 | Today's Thread Head at Snoble size: a lilac ball with wraps, the zipper mouth, gloves, a loose end. |
| v02 | Pom-pom | 12 x 11 | A fluffy pom-pom with a ragged edge that bounces instead of walking; gloves bob beside it. |
| v03 | Skein | 14 x 9 | A long twisted skein of yarn lying across two legs, the mouth on its twist. |
| v04 | Bobbin | 12 x 11 | A wooden bobbin wound thick with purple thread; the mouth opens in the thread between the flanges. |
| v05 | Beanie | 12 x 14 | The ball wears a purple knitted beanie with a white pom-pom that flops as it walks. |
| v06 | Tangle | 15 x 13 | A messy ball with loops of loose thread sticking out all over it. |
| v07 | Candy Stripe | 12 x 11 | Wound from purple and white yarn in turns, so it reads like a sweet. |
| v08 | Knitting Needles | 12 x 16 | Two needles stuck crossways through the top of the ball, pearl heads up. |
| v09 | Hooded | 12 x 13 | The yarn ball wears Spool's purple hood, its point rising at the upper right. |
| v10 | Big Mouth | 12 x 11 | The zipper runs the whole width of the ball and the tongue lolls out; tiny gloves. |

## Stitched (`stitched/`)

Today: 11 x 12 px at rest. Sheets: `Spoolstitched-Sheet`.

| Version | Name | Size at rest | Look |
| --- | --- | --- | --- |
| v01 | Mini Spool | 10 x 9 | Spool in small, his hood point at the upper right, the zipper mouth with the eyes, white gloves. |
| v02 | Bean | 10 x 7 | A legless purple bean that hops like a Snoble, the mouth on its front. |
| v03 | Mitten | 8 x 9 | A purple mitten walking on its cuff; the thumb is its arm and swings as it goes. |
| v04 | Sock Puppet | 10 x 9 | A striped sock standing on its heel, the mouth at the toe. |
| v05 | Teddy Ears | 10 x 8 | A round plush with two little ears sewn on top, ears flick as it walks. |
| v06 | Patchwork | 10 x 8 | Sewn from purple and lilac squares with the seams showing. |
| v07 | Floppy Hood | 10 x 10 | The hood's point is long and flops over to the back; it bounces on every step. |
| v08 | Boxer | 11 x 8 | A stubby square plush in oversized white gloves; it jabs. |
| v09 | Tongue Out | 10 x 8 | Round and cheerful with a long tongue that wags as it walks. |
| v10 | Button Front | 10 x 10 | A big white button sewn on its chest; the eyes look out of the zipper above it. |

## Dart (`dart/`)

Today: 8 x 15 px at rest. Sheets: `Spooldart-Sheet`.

| Version | Name | Size at rest | Look |
| --- | --- | --- | --- |
| v01 | Long Hood | 6 x 11 | Thin and lilac, the hood drawn out into a long point, a needle in its leading glove. |
| v02 | Pinhead | 6 x 11 | A lilac body with a round pearl head like a sewing pin's, the eyes looking out of a slit. |
| v03 | Quill | 6 x 12 | Thin, with a white feather plume rising from its hood that bends as it runs. |
| v04 | Lancer | 5 x 10 | Carries a needle longer than itself upright like a lance and hurls it. |
| v05 | Thimble | 6 x 10 | A thin plush wearing a steel thimble as a helmet. |
| v06 | Bobbin | 8 x 10 | A slim wooden bobbin wound with lilac thread, the mouth opening in the thread. |
| v07 | Scissor Legs | 6 x 11 | A little lilac body striding on two steel scissor blades. |
| v08 | Upright Zip | 6 x 10 | A tall thin hood whose zipper runs top to bottom; one yellow eye looks out of the gap. |
| v09 | Ribbon | 8 x 9 | Wrapped in a lilac ribbon whose two tails stream behind it. |
| v10 | Tassel | 6 x 10 | A thin plush under a pointed cap with a tassel that swings as it runs. |

## Seam (`seam/`)

Today: 10 x 11 px at rest. Sheets: `Spoolseam-Sheet`.

| Version | Name | Size at rest | Look |
| --- | --- | --- | --- |
| v01 | Felt Ball | 8 x 8 | Round white felt with a purple seam down the front and cross stitches. |
| v02 | Snowball | 8 x 7 | A legless felt ball that hops like a Snoble, one purple seam ringing it. |
| v03 | Baseball | 8 x 9 | Two curved purple seams with stitch ticks, like a baseball on legs. |
| v04 | Dumpling | 8 x 9 | A felt dumpling pinched into pleats on top. |
| v05 | Pillow | 8 x 9 | A little square felt pillow with a tuft at each corner and a purple seam round its edge. |
| v06 | Button | 8 x 9 | A big round felt button with four holes; two are its eyes. |
| v07 | Beanbag | 8 x 9 | A slouchy pear of felt that sags and wobbles as it waddles. |
| v08 | Eyepatch | 8 x 8 | Round felt with a purple patch stitched over one eye. |
| v09 | Little Miner | 10 x 8 | Carries a felt pickaxe over its shoulder and swings it to dig. |
| v10 | Hard Hat | 8 x 9 | A felt ball in a yellow miner's hat with a little lamp. |

## Mangled (`mangled/`)

Today: 11 x 12 px at rest. Sheets: `Spoolmangled-Sheet`.

| Version | Name | Size at rest | Look |
| --- | --- | --- | --- |
| v01 | Torn | 8 x 9 | Torn white cotton with a purple patch and mismatched eyes, one yellow, one a button. |
| v02 | Burst | 10 x 9 | A spiky ball of cotton tufts standing out in every direction. |
| v03 | Half Hood | 8 x 11 | A scrap of Spool's purple hood still sits on a lump of spilled cotton; half a mouth shows. |
| v04 | Frankenpatch | 8 x 9 | Three scraps (purple, lilac, white) stitched into one lump with the stitches showing. |
| v05 | Ragdoll | 10 x 8 | An X stitched over one eye and one arm hanging by a thread. |
| v06 | Unravelled | 9 x 8 | Threads hang off its underside and trail as it moves; it sags. |
| v07 | Pin-studded | 9 x 11 | Pins from the Pin Cushion stuck in its back, pearl heads up. |
| v08 | Split Seam | 8 x 9 | Its seam has burst across the middle, the cotton showing like teeth. |
| v09 | Fuse | 8 x 12 | A felt bomb with a thread fuse sparking on top, made to burst. |
| v10 | Sandbag | 8 x 9 | A squat lump stuffed hard, patched and buttoned, built to block. |
