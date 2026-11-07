# Prompt: ChatGPT recreates the pack's art

Dimitri, 2026-11-07: a prompt for the ChatGPT Project "Rogue Waves Art" (set up by `README.md`) that remakes every art
asset in the pack, in three stages with a stop after each: (1) sketch the vessels and the mode's other characters in
higher detail, so he can see whether ChatGPT reads his vision; (2) read the style resources and ask him questions;
(3) make the assets in batches, per `ASSET_MANIFEST.md`, delivered as `README.md` "Bringing art in" expects. It also
redraws the main menu's Rogue Waves button after his reference picture (a sleek, minimal tile).

**How to use it.** Open a new chat inside the Project, attach his button reference picture (the white tile with a grey
ship and » arrows), paste the block below and send. Answer each stop in plain words; "go" or "next" moves it on. Save
the PNGs it delivers to `Desktop\RogueWave\sprites\chatgpt\`, then tell a Claude thread "bring in my ChatGPT art".

**After the button comes in.** Today `TDS_Mod/ModSource/RogueWavesMode/Code/12_Menu.cs` (`RWMenu.Image`) fills the tile Rogue Teal and
shows `RogueWaves_Crest32`; the new look needs that thread to keep the tile's own white face and show
`RogueWaves_MenuButton.png` in Play's 48-unit picture box instead.

```text
You are making the art for Rogue Waves, my fan expansion for TDS - Tower Defense Strategy. Your instructions and every file in this Project still apply (01_RogueWaves_Art_Guide.md first). This chat remakes every art asset in the pack, in three stages. Stop at the end of each stage and wait for my answer; never run two stages in one reply.

STAGE 1: SKETCH MY VESSELS (before reading the style rules in depth)
Show me that you see my characters before you make anything for the game.
1. Read Part 5 of the guide (my concepts), the vessel rows of Part 2's table, his_sprites.png, and the pack_*.png contact sheets. Where they disagree, the newer word wins: Spool is sage green now (plush 93CCA1 / 60A871), not the purple Part 5 still describes.
2. Make one higher-detail character sheet per vessel: Yossele, Spool, Uloe, Argo. Each is one picture with a front three-quarter view facing right and a back view, about 128 pixels tall, still pixel art with flat colours and light from the upper left, but with more detail than the 32 px game sprite allows. Keep each silhouette and colour from my sprites; add detail, don't redesign.
   - Yossele: the patient clay golem. Round head with a see-through hole (it is the mouth), green eyes beside it at different heights, a two-leaf sprout on a short stalk and a second sprout off the left shoulder, a dark slate V at the neck, four long root-like limbs that reach the ground.
   - Spool: a hooded sage plush whose whole face is a stitched opening (light and dark stitches around a dark green hollow), two small yellow eyes looking out, a pink tongue hanging from the right, white glove hands held out, short legs, and its own severed arm on the floor with white cotton stuffing spilling out (not a tail).
   - Uloe: light personified. A pale blue light-bulb head with yellow glints, a filament face (two arched dark blue eyes over a wide U), a ribbed screw base on a long steel lamp post that widens into a foot, a yellow light stroke slanting down on each side, a power cord running to a two-prong plug on the left.
   - Argo: the deep sea diver who went down to find the bottom of the Basin and never quite came back up. Bulky round brass dome with a porthole and pale eyes in the dark, riveted breastplate, blue suit, brass mitts on the hips, dark boots.
3. Then one line-up of the mode's other characters at the same detail: Yossele's Cordyfriend; Spool's Crafted (Dart, small Dart, Mangled, Seam, Stitched, small Stitched) and the Thread Head; Uloe's Photon; VENDCINT the shopkeeper. Take each one's look from its current sheet (pack_*.png) and its scope Look line (docs/scope/ on GitHub).
4. Under each picture, list in a few short lines what you were sure of and what you guessed. Then stop and ask me what you got wrong.

STAGE 2: READ THE STYLE RESOURCES, THEN ASK ME
When I say go, read the whole guide (Parts 1-6), 03_palettes.txt, the game_*.png contact sheets, and open 04_pack_sheets.zip and 05_game_sheets.zip in Python to measure real sizes, heights and colours. Fold my Stage 1 corrections in.
Then ask me your clarifying questions in one numbered list, at most 15, each with the answer you would pick if I said "defaults". Cover at least:
- which assets to remake and in what order (my suggestion below), and whether Yossele's sheets marked "final" (my own reshade) are remade or skipped;
- Argo: keep his shipped look, or one of the diver takes (Barnacled, Angler, Hollow, Passenger, Pressure Hull);
- how far a remake may move from the current version (polish only, or a new drawing in the same silhouette);
- full animation sheets, or key frames only for Claude and me to fill in;
- the main menu button (below);
- anything in the guide you find unclear or contradictory.
Stop and wait for my answers.

STAGE 3: MAKE THE ASSETS
Work through Part 6 of the guide (the asset manifest) in batches, following Part 1's workflow for every asset: exact file name, size and cell, one frame or icon per generated picture, 02_pixelize.py until it prints "0 problem(s)", the x8 preview checked by eye against the current version and the nearest base-game sprite. Suggested order unless I change it in Stage 2:
1. The main menu button picture.
2. The icons (90): Directions, then Trinkets, then Cardinals, then the RogueWaves_ UI icons.
3. The minions and units, then VENDCINT.
4. Per vessel (Spool, Uloe, Argo, then Yossele if I said yes): the key frame first (idle_v0, frame 0) for my OK, then idle and move, attack, the vessel's own action sheet, fall and land, then its UI pieces (icon, splash, end stats, journal tab, ready-up, mini sheet).
5. Towers and buildings are 3D models built in code: only if I ask, concept pictures, front and three-quarter view.
Each batch is about 6 to 10 assets. For each asset deliver the game-size PNG under its exact file name, its _x8 preview, and one line: what it is and what 02_pixelize.py printed. End every batch with a progress line (done / left / skipped, and which are key frames only) and stop until I say "next". Never invent a file name: ask if an asset is not in Part 6.

THE MAIN MENU BUTTON
I attached a picture of the look I want for the Rogue Waves button on the main menu: sleek and minimal. Match its style, not its picture. What I see in it: a square tile with a plain white face (FFFFFF); a thin light grey (DEDFDE) double-line frame set just inside the edge, with tiny notches at the corners; lots of empty white space; one picture in the middle drawn in a single flat light grey (C8C8C8): a small ship shape on the left and a » double chevron on the right, strokes about 2 pixels thick, no shading, no outline, no second colour.
The tile and its frame already exist in the game, so you draw only the picture: RogueWaves_MenuButton.png, 48 x 48, the drawing on a clear ground. Make it the pack's crest (one breaking wave curl, one clean shape, not a pattern) followed by the same kind of » double chevron, in C8C8C8 only, with the same stroke weight and empty space as the reference, so it reads as "set out to sea" and sits beside the game's own Play tile as a sibling. Check it with: python3 02_pixelize.py in.png RogueWaves_MenuButton.png --cell 48 --anchor center --palette C8C8C8 --preview 8. Also show me a mock-up of the whole tile (white face, the double frame, your picture) at x4, so I can judge the look. Ask me in Stage 2 whether I want one touch of Rogue Teal (3F8F96) on it; until I say so, grey only.

DELIVERY
I save your PNGs to Desktop\RogueWave\sprites\chatgpt\ and a Claude thread brings them into the pack, so the file names must be exact and the PNGs game size, fully opaque pixels on a clear ground. Keep replies short: the files, the check result, the progress line, and questions only at the stops.
```
