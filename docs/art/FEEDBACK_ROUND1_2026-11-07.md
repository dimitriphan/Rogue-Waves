# Feedback for ChatGPT: art round 1, and batch 2

Dimitri, 2026-11-07: "Do a review of the ChatGPT assets. Do they follow the art style and rules of TDS? Generate a
report for ChatGPT on what could be changed so that the assets look more official." The review is
`docs/reports/ART_REVIEW_CHATGPT_2026-11-07.md`; this page is its note for ChatGPT, with PR #173's batch-2 list folded in
(the held-back falls, tab icons and `Direction_Extension`, the missing `ArgoSonar-Sheet`, `SpoolPatch`'s frozen rows).

**How to use it.** Open a new chat in the ChatGPT Project "Rogue Waves Art" on GPT-6 Sol Medium, paste the block below
and send. It answers with a batch plan and its questions, then stops. Save what it delivers to
`Desktop\RogueWave\sprites\chatgpt\` and tell a Claude thread "bring in my ChatGPT art" (`README.md`). A copy of the
block is in project files, `chatgpt/prompts/feedback_round1_2026-11-07.txt`.

```text
ROGUE WAVES ART: FEEDBACK ON ROUND 1, AND BATCH 2

Your round 1 art is in the pack (160 files). It passes every mechanical check: exact sizes, opaque pixels, no 000000, no foe magenta, feet on the bottom row, centred. Keep all of that. A review against the base game's sprites found what still keeps it from looking official. This chat fixes it in batches. Your Project instructions and the guide still apply; where this note is more exact, this note wins.

HOW TO WORK IN THIS CHAT
- Model: GPT-6 Sol Medium for every picture. Tell me if details drift (faces, stitches, small parts change between frames); I may switch to Astra for those assets.
- Bookkeeping turns (file lists, the batch plan, progress lines, laying frames out in Python, running 02_pixelize.py) need no deep thinking: keep them short, and use tools only where a step needs Python.
- Describe before you render: before each asset, write 2-4 lines on what you will draw, which frames change and how, and what differs from the round 1 file. Then render.
- Never regenerate unprompted. If a picture comes out wrong, say what is wrong and wait for me. Re-roll only when I ask.
- Batches of 6 to 10 assets. Stop after each one, with a progress line: done / left / key frames only. "next" moves on.
- For a redesigned vessel (Spool, Argo), deliver the key frame (idle_v0, frame 0) at x8 first and wait for my OK before the sheets.
- When a chat gets long, write me a short handoff (what is done, what is next, the rules above) so I can paste it into a fresh chat.

RULES FOR EVERY ASSET (what round 1 missed)
1. Animation must move. This is the biggest gap. Base TDS vessels change 30-380 pixels from one frame to the next; round 1 changes 0-30, and most idles have only 2 distinct frames out of 6.
   - Idle (6 frames): 6 distinct frames. Breathe: the head top goes 1 px down on frames 2-3 and back up on 4-5. Add one secondary motion per vessel: Yossele's sprout sways 1 px, Spool's tongue and gloves bob 1 px, Uloe's arms flicker and its glints alternate, Argo's wrench head dips 1 px.
   - Move (6 frames): a real step or hop, with the body bobbing 2-3 px. Contact poses (legs apart, body 1 px down) on frames 0 and 3; passing poses (legs together, body 1-2 px up) on 1-2 and 4-5. Uloe has no legs and hops like Sno Sno: its lowest pixel rises 0/2/5/6/3/0 px over the 6 frames, with a 1 px squash on frame 0 and a 1 px stretch at the top. The cord trails behind.
   - Attack (4 frames): frame 0 anticipation (lean back 1-2 px), frame 1 strike (the biggest pose, with a pale flash), frames 2-3 recovery. Base attacks change 200-780 px per frame.
   - Land (IntroR, 12 frames): the vessel drops in. Frames 0-3 above the cell bottom or crouched low; frames 4-6 squash to about 60% height on impact; frames 7-11 unfold, overshoot 1 px, and settle into the idle_v0 pose. All 12 frames distinct.
   - Fall (12 frames): a mid-air tumble that loops, never a collapse onto the ground. The body is off the ground: on at least 10 of the 12 frames the lowest pixel sits 3 or more px above the bottom row. The body turns in 30-45 degree steps, the limbs flail, and loose bits (leaf, tuft, glint, hardhat) trail above the body. Frame 11 flows into frame 0. Study the base game's Smacky fall in 05_game_sheets.zip.
2. The four facings are one character. v1 is v0's exact mirror and v3 is v2's, unless Part 6 says otherwise. The shade stays on the right and bottom in every facing; never flip the light.
3. No dark rim. Edges read by colour, as in the base game. The left and top edges take the lit tone. The darkest shade goes only on the right edge and the underside.
4. Icons are shown on the Deep Swell panel (2E4A62). Never paint 2E4A62 or 22384B as part of an object: it vanishes on that panel. Shade each object in its own hue, with its darkest tone at least 20 luminance points away from 2E4A62. One found object per icon, no UI symbols (bar charts, crosses, arrows).
5. Sizes inside the cell: vessels 15-27 px wide and 19-28 px tall in the 32 px cell, never touching the cell's sides. The mini vessel (16 px cell) is about 7 wide by 10 tall. Units in 16 px cells (Nauts, Crafted, Photon) are 6-12 px wide and 8-12 px tall, like the base game's miners, knights and dogs.
6. Splashes (128x128) are line art, not a filled figure: a translucent fill, a 3 px accent outline, an inset line in the body colour, one shape per body part. This is the one asset where semi-transparent pixels are right: report 02_pixelize.py's opacity line for splashes and go on.
7. Journal tab icons (32x32) are a white (FFFFFF) 1 px line drawing of the vessel's head, with an open interior on a clear ground. No fill, no colour.
8. Airborne frames (the Fall, the drop-in frames of Land, Uloe's hop): 02_pixelize.py puts every drawing's feet on the bottom row, which erases the lift. Clean each airborne frame on its own with 02_pixelize.py as usual, then in Python paste it into the sheet raised by its planned lift (Uloe's hop: 0/2/5/6/3/0 px), still centred on column 16. Check the finished sheet with --anchor center instead of bottom: that skips only the feet line and keeps every other check. In the asset's delivery line, list which frames are lifted and by how many pixels.

KEEP (do not regress)
- Yossele's palette and look: clay 945743 / 79432F, collar 35403A, leaf 7BCF80 / 65AD69, eyes 99E550; the see-through hole, the stalk with its two-leaf sprout, the dark V collar, four limbs. It matches my reshade, which is the baseline.
- Spool's colours: sage 93CCA1 / 60A871, thread 4F8C5E, stitches E4DEDA / 9A938E, hollow 1F3325, eyes E6D85A, tongue E58CC0.
- Uloe's silhouette (glass bulb, ribbed screw base, post, yellow arms, cord and plug), the bulb flash on its attack, and UloeLit-Sheet's glow.
- The endstats of Yossele, Uloe and Argo, the ready-up brackets, the Naut crew colours (Port red, Starboard green, Bosun yellow), VENDCINT's vending-machine design in Wet Sand.
- These icons, which already read at 12x12: RubberDuck, Cost, Refit, SewingMachine, Kettle, Leash, Wound, Trowel, Chain, Dandelion, Fairyring, Oddsocks, Stay, Vote, Objective_Hull, Objective_Nest, Nesting, Buttonjar, BrassSpyglass.

BATCH 2A: WHAT ROUND 1 HELD BACK OR MISSED (10 assets)
1-4. YosseleFall-Sheet.png, SpoolFall-Sheet.png, UloeFall-Sheet.png, ArgoFall-Sheet.png: 384x32, 12 frames, the mid-air loop of rule 1.
5-8. Yosseletabicon.png, Spooltabicon.png, Uloetabicon.png, Argotabicon.png: 32x32, white line art per rule 7.
9. Direction_Extension.png: 12x12, an orange extension cord on a reel. The round 1 file is 62 of 84 pixels in the panel colour (rule 4). Use an orange cord (E2944A lit, B8702F shade) on a light wooden or grey reel.
10. ArgoSonar-Sheet.png: 192x128, 32 px cells, rows sonar_v0, v1 (mirror), v2, v3 (mirror), 6 frames each. Argo in the new rig-worker look, sounding the deep: he braces, and a pale ring pulses out from his feet or wrench across the 6 frames.

BATCH 2B: SPOOL BACK TO MY CONCEPT (key frame first)
Round 1 drew Spool as a lanky 32 px-wide creature with a gaping toothed jaw, big spiky white gloves and the arm as a tail. My concept (his_sprites.png, guide Part 5) is a compact hooded plush:
- about 18-20 px wide and 19-22 px tall, centred on column 16;
- the face a narrow stitched slit 2-3 px tall, with alternating E4DEDA / 9A938E stitches round the 1F3325 hollow and two small E6D85A eyes looking out (stitches, not teeth);
- a small pink tongue on the right;
- small glove hands, 3x3 each, in DDDDDD, not FFFFFF fans;
- its own severed arm lying on the floor as a separate object about 2 px left of the body, with an E4DEDA cotton tuft at the wound, never joined to the body and never a tail.
Then the sheets: Spool-Sheet, SSpoolSheet (about 7x10), SpoolAttack-Sheet, SpoolPatch-Sheet (a needle-and-thread pass in three readable poses; round 1's v2 and v3 were six identical frames and v0 barely moved), SpoolIntroR-Sheet, SpoolIdlejustidle-Sheet, Spool_Icon, Spoolendstat1 and 2 (2, defeated, needs a slumped pose, not only new eyes), Readyup6spool, Spoolsplash.

BATCH 2C: ARGO, BRIGHTER AND CHIBI (key frame first)
The rig-worker design stays. Round 1 is dark and not in the house proportions: the head is 30% of the height, the front view is 61% dark teal, and it disappears on the Deep Swell panel. Change:
- head and hardhat about half the height (the house's big-head chibi);
- the ochre coat is the main mass, 45% or more of the pixels: C8A04B lit, 967533 shade;
- 2B555C for the waders and gloves, with 263C42 only as their shade on the right and underside, never down the lit left edge;
- a steel face guard in 9AA7A0 or lighter, with 2-3 px eyes behind it;
- drop the moss green 638868; keep steel 657772.
Then: Argo-Sheet, ArgoAttack-Sheet (the wrench swing with anticipation and follow-through), ArgoIntroR-Sheet, ArgoIdlejustidle-Sheet, SArgoSheet (about 7x10), Argo_Icon, Argoendstat1 and 2, Readyup6argo (the face must read inside the brackets), Argosplash (round 1's is one flat 263C42 colour and draws a gas-mask diver: redraw it as the hardhat rig worker in the rule 6 style).
Nauts (6 sheets, 16 px cells): replace the plumed grey helmet (it reads as a knight) with an ochre C8A04B hardhat with a brim. Keep their size and the crew colours.

BATCH 2D: YOSSELE AND ULOE, ANIMATION AND FACE
- Yossele: Yossele-Sheet (idle and move per rule 1), YosseleAttack-Sheet (its attack is a mouth laser: the hole glows 99E550 and flashes on frame 1, not a limb poke), YosseleFertilize-Sheet, YosseleIntroR-Sheet, SYosseleSheet (idle 6 distinct frames). Redraw v1 and v3 as exact mirrors of v0 and v2. In round 1, v1's sprout became a 2x2 block, its round hole went square and it grew a third eye pixel. Restore the second sprout off the left shoulder (one leaf, not specks).
- Uloe: the face is lost in round 1. Its 3F3F74 blocks fill two thirds of the bulb and read as a grimace. Make the eyes two 1 px arcs of 3 px each and the mouth a 4-5 px U, leaving at least half the glass clear. The glass is DAE6FC on the upper-left two thirds and CBDBFC on the right and bottom. Snap AABBD0 to CBDBFC and FFFAAE to F1EF72. Keep 222222 on the cord and plug only. Sheets: Uloe-Sheet (hop per rule 1), UloeLit-Sheet (same motion), UloeAttack-Sheet (add a frame-0 dim and recoil), UloePlug-Sheet (it barely moves now), UloeIntroR-Sheet, SUloeSheet (about 7x10), Uloe_Icon (same new face).

BATCH 2E: ICONS
- Redraw to their Look lines: Direction_Interest (a jar of coins with a sprout in it, not a bar chart), Direction_Longline (a long spool of fishing line), Direction_Regulars (a tally of a regular's tab), Direction_Mend (a tide pool with a green glow, not a red cross), Direction_Gallop (a horseshoe with a wing on it).
- Weak at 1x: PearlOyster (add a 2x2 F4EEF8 pearl at the lip), Hands (two work gloves with DA867A cuffs, not a white cloud), GuttingKnife (its handle is in the panel colour), Pool (add 3F8F96 water spilling over), OilskinCoat (reads as a leaf), SparePlank (straight edges and 2 nail heads; reads as a fish), Compass (reads as a pocket watch).
- Tell alike icons apart: Brightbulb and Flashbulb (smoke on the Flashbulb); the three life rings (CorkLifebuoy, RogueWaves_Retry, RogueWaves_Objective_Hands); the three teal waves (RogueWaves_Crest, RogueWaves_RisingWater, Spring).
- RogueWaves_PureAura: Pearl F4EEF8 with C9E4F2 / F2D9E6 sheen pixels, not grey.
- RogueWaves_Crest and Crest32: Foam is E9F1EE, not DEDEDE; the dark roundel is Deep Swell 2E4A62, not 22384B. Use one crest drawing for both crests and the menu button.
- Rule 4 for every icon that paints the panel colour. Fix the worst first: TarBucket, VampireDentures, Patternbook, Objective_Nest, Deepcurrent, Pool, Wormtin.

BATCH 2F: UNITS AND UI
- Yosselefriend-Sheet (Cordyfriend): the idle is static; give it a 1 px bob and an antler sway.
- Spoolhead-Sheet (Thread Head): lighten the flange tops to 826D60 over 64544A.
- The Crafted: give each type its own read: Seam a visible stitch line, Mangled a missing chunk.
- Yosselesplash and Uloesplash in the rule 6 style (Yossele's lines are too thin to hold the panel).
- Vendcint-Sheet: v1 moved the shade to the left; keep it on the right, as the base Merchant's does.

Start with Batch 2A. Before anything else, reply with the batch plan in one short list and the questions you need answered, each with your default. Then stop.
```
