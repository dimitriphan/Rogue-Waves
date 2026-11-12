# Chat 3: Spool's game files, one step at a time

Dimitri, 2026-11-08 21:15Z: "have it walk step by step on creating the new assets needed. Dissect the base game
Vessel's animations and sprites so that it understands a bit more about TDS, but make sure it understands completely
what it is creating before continuing in steps." His answers at 21:20Z: only Spool's 13 character files (its units and
Uloe later); the base game's facts go in as text, its pictures stay look-only; before each step ChatGPT answers a
short quiz and waits for "go", after it waits for "approved"; the order is facings, idle, move, attack, Patch, land,
fall, menu idle and boat mini, the UI set. Dimitri, 21:54Z: "Pivot the Chat 3 to also decompile
Yossele and Argo first based on the keyframes": Step 0 takes his finals apart from their key frames before any Spool
step (both finals' v1 and v3 rows are exact pixel mirrors, checked on every sheet).

**Where the numbers come from.** Part 2: kit `TDS_Vessel_Creator/docs/03` §1 and `docs/18` §1-§4, and the game's own
animation tables (`research/art_dump_2026-10-15/animation_tables.txt` in project files). Part 3: measured on 2026-11-08
on the base sheets in `research/art_dump_2026-10-15/sheets/` (private, never in this repository): per frame, the top of
the drawing, its lift off the bottom row, its width and height, and the pixels that change to the next frame. Part 4:
measured the same way on Dimitri's approved finals (`Yossele_Final_Approved.zip`, `Argo_Final_Complete.zip`, his
uploads of 2026-11-08). Part 5: `docs/art/ASSET_MANIFEST.md`. Note: the base game's Land sheets mostly stay on the
ground and squash; the old rule "frames 0-3 airborne" (`TDS_ART_RULES.md` rule 25) did not match them.

**How to use it.** Open a new chat in the Project after chat 2's handoff. Paste the block below and attach: the key
frame (both files), chat 2's approved concept sheet and its handoff text, and the two finals' `assets/` folders and
key frames (project files `chatgpt/prompts/reset_2026-11-08/chat3_references_Yossele_Argo.zip`). Copy of this prompt:
`chatgpt/prompts/reset_2026-11-08/3_assets.txt`.

```text
ROGUE WAVES ART: SPOOL'S GAME FILES, STEP BY STEP (Dimitri, 2026-11-08)

The reset holds: no earlier ruling, file or Spool version counts. Attached: Spool_keyframe.png (32x32, frozen) and its x8; the approved concept sheet and the handoff from the concept chat; Yossele's and Argo's final assets with their key frames (approved, frozen, the examples of good work). Read Parts 1-5 now. Then answer the Step 0 quiz and stop.

PART 1: HOW WE WORK, EVERY STEP
1. Before a step: answer its quiz in your own words, with numbers. Then stop and wait for "go". If Dimitri corrects an answer, answer the quiz again.
2. Then draw the step. Every frame is an edit of the key frame: same parts, same proportions, same colours. The key frame itself is never redrawn; it appears pixel for pixel as idle_v0 frame 0, as land frame 11 and as menu idle frame 0.
3. Deliver: the game-size PNG under its exact file name; an x8 preview; one GIF per row at the game's speed; and a table with one line per row: frame sizes (w x h), lift off the bottom row in px per frame, pixels changed per frame, colours used. Then stop and wait for "approved".
4. A change request = one new version that changes only what Dimitri named; say which pixels changed. Never redraw, re-roll or "improve" anything on your own. Keep every version in its own folder; never overwrite one.
5. An approved step is frozen. If a later step seems to need an earlier one changed, stop and ask.
6. When the chat gets long, write a handoff of at most 40 lines (approved files, the step you are on, open questions) and say so.

The drawing rules for every step:
- Pixel art at 20 pixels per game unit. Every pixel fully opaque or fully clear (the splash's inside fill is the one exception, Step 9).
- The claws stay (Dimitri, 2026-11-12): the pale E4DEDA spikes on both hands and along the arm on the floor are long claws, and the arm on the floor is attached at the back, dragging behind, palm facing up. Every frame and every facing keeps them.
- Only the key frame's 8 colours: 93CCA1, 60A871, 9A938E, E4DEDA, 1F3325, E6D85A, 4F8C5E, E58CC0. A needle, thread, dust or flash may add colours only when Dimitri OKs each one at its quiz.
- No outline, no dark rim, no pure black 000000, no gradient, dithering, blur, glow or anti-aliasing.
- Light from the upper left: left and top edges keep the light tone; the right side, the underside and whatever sits under an overhang take the shade.
- Every ground frame stands with its feet on the bottom row (row 31 of a 32 px cell) and its box centred on column 16 within 1 px. Only airborne frames lift, and you list their lift.
- A vessel drawing stays within 15-27 px wide and 19-28 px tall in its 32 px cell on a normal frame. Squash and stretch frames of land and attack may go wider or shorter.

PART 2: HOW TDS DRAWS A VESSEL (the engine's facts)
- A sheet is a grid of square cells read left to right; each row is one animation in one facing. The pivot is the bottom centre of each cell. Point filtering: every pixel shows as a hard square.
- Facings: v0 = toward the camera, heading screen-right; v1 = toward the camera, heading screen-left; v2 = away from the camera (the back), heading right; v3 = the back, heading left. The game never flips a sprite in code: every facing is a drawn row. In the approved Yossele and Argo sheets every v1 row is the exact pixel mirror of v0 and every v3 of v2 (main, attack and ability sheets alike). Spool does the same unless Dimitri says at Step 1 that a part must not swap sides.
- Speed: a vessel plays 8-10 frames a second (Smacky 8, Fie 8, Sno Sno 8, Boutman 9, Buster 10). Idle and Move loop. Fall loops. Land plays once, then Idle. Attack and the ability play once (some at double speed, the game's cap), then Idle.
- The base game plays the footstep sound on Move frames 2 and 5 (frames counted from 0): one per landing of a two-step cycle. Spool's code plays its footstep on Move frames 1 and 4, so Spool's feet land on frames 1 and 4 (Yossele's on 0 and 3, Argo's on 2 and 5).
- The journal portrait is the main sheet's frame 0; the boat select uses a separate mini sheet.

PART 3: THE FIVE BASE VESSELS, TAKEN APART (measured on their sheets; frames counted from 0)
"top" = the drawing's top row in the cell; "lift" = empty rows under the lowest pixel; "changed" = pixels that differ from the next frame.

SMACKY (square head with a sprout, 15-18 wide, 23-26 tall, 20 colours, 8 fps)
- Idle 6: top 6, 8, 8, 9, 9, 7: a 3 px breath down and back up; height 26 to 23 to 25. Changed 37-228 a frame.
- Move 6: top 8, 9, 8, 6, 6, 10; lift 0, 0, 1, 0, 0, 1 (a 1 px hop on frames 2 and 5); the head bobs 4 px; changed 99-352.
- Fall 12 (one row): lift 0, 2, 8, 8, 8, 8, 8, 8, 7, 3, 2, 1; width swings 17-25 and height 18-29 as he tumbles; changed 98-319.
- Land 12 (one row): never leaves the ground. Frames 2-4 stretch up (27 to 31 tall, 29-31 wide); frames 5-8 slam flat to 11-13 tall and 22-31 wide; frames 9-10 spring back (22, 18 tall); frame 11 is the idle pose. Changed 47-602.

BOUTMAN (cat in hat with a rod, 24-27 wide incl. rod, 24-26 tall, 18 colours, 9 fps)
- Idle 6: top 6, 6, 8, 8, 7, 6 (2 px); changed 74-385.
- Move 6: top 5, 8, 5, 5, 8, 5; lift 1, 0, 2, 0, 0, 2 (little hops); changed 404-672, the biggest of the five.
- Attack: three moves of 3 frames each (punch, punch, rod cast); lift 2, 1, 0 (he hops into the punch); the rod frames reach the full 32 px width; changed 500-800.
- Land 15: dives in 9 px up, hits the ground squashed to 13 tall on frame 2, bounces 5-11 px up on frames 3-7, lands and stretches to 31 tall on frames 8-11, settles to 25-27.
- Fall 6: hangs 9-10 px up, height stays 22, arms flap; changed 438-676.

FIE (girl with huge orange hair, 21-22 wide, 27-28 tall, 21 colours, 8 fps)
- Idle 6: top 4, 4, 4, 5, 5, 4 (a 1 px breath); the hair sways; changed 17-311.
- Move 6: top 4, 5, 3, 3, 5, 4; lift 0, 0, 2, 1, 0, 1; changed 313-679.
- Land 12: one 7 px hop on frame 1, otherwise on the ground; frames 7-11 open into a wide arms-out pose (24 to 31 wide).
- Fall 6: 0-2 px up, stretched 30-31 tall (arms up); changed 349-595.

BUSTER (sitting dog, 16-27 wide, 19-20 tall, 17 colours, 10 fps)
- Idle 6: width 20, 22, 27, 27, 21, 16: the tail wags out and back; the head moves 1 px; changed 127-380.
- Move 6: lift 6, 3, 0, 3, 2, 0: a real bound; frame 2 stretches to 25 tall; changed 249-615.
- Attack 2 frames: changed 565-676 (two big poses, nothing between).
- Land 12: frame 0 fills the full 32 px height dropping in, frame 1 squashed to 15 tall, frames 3-4 hop 2-5 px, frames 5-11 settle at 19-20 tall.
- Fall 6: 6-7 px up, 23-25 tall.

SNO SNO (two snowballs with stick arms, 19-25 wide, 20 tall, 10 colours, 8 fps)
- Idle 6: the top never moves (row 12 on every frame); only the arms move (width 19-25); changed 51-213.
- Move 6: no lift at all; the hop is squash and stretch: top 14, 13, 14, 10, 7, 9 (18 tall up to 25 tall); changed 224-563.
- Attack 4 (double speed): changed 141-386.
- Land 12: stays on the ground; spreads to 32 wide and 5-7 tall on frames 5-7, rebuilds 13 to 20 tall by frame 11.
- Fall 6 (two facings): 0-3 px up, 21-29 tall.

WHAT ALL FIVE SHARE
- Idle is small and alive: the top moves 0-3 px, one secondary part moves (sprout, hair, tail, arms), 17-385 pixels change.
- Move is big: 100-680 pixels change; the body goes down on the landings (frames 2 and 5) and up between, by a hop (Smacky, Boutman, Fie, Buster) or by squash and stretch (Sno Sno).
- Attack has few frames and big pose changes (2-4 frames, or 3 per move; 140-800 pixels).
- Land tells a story with squash and stretch: down to 35-60% of the body's height and wider than the body, then a stretch taller than it, then settle into idle.
- Fall is a loop of mid-air poses with limbs reaching out; how high it floats differs (0-10 px).

PART 4: WHAT GOOD LOOKS LIKE HERE (Dimitri's approved finals, attached)
YOSSELE (17 wide, 21-23 tall, 5-6 colours)
- Idle: top 10, 10, 11, 11, 9, 9 (1 px down, 2 px up); changed 9-62. Small, as the base game does it.
- Move: top 12, 10, 11, 12, 10, 11; no lift; changed 62-111.
- Attack 4: changed 48-217; frame 1 reaches 22 wide.
- Land 12: lift 8, 6, 3, 1 on frames 0-3, then on the ground; frame 11 = the key.
- Fall 12: lift 3, 4, 3, 4... on every frame (never touches the ground); width 19-25 as it turns; frames 6-11 repeat 0-5.
ARGO (20-25 wide, 25-27 tall, 8 colours)
- Idle: top 6, 6, 7, 7, 6, 6 (1 px); changed 20-113.
- Move: top 6, 7, 5, 6, 7, 5 (2 px bob); no lift; changed 183-310.
- Attack 4: top 5, 8, 10, 7: tall wind-up, a deep lean down into the strike, recovery; changed 225-369.
- Land 12: lift 5, 4, 2 on frames 0-2, squash to 16 tall on frame 5, overshoot to 27 tall on frame 9, frame 11 = the key; changed 8-434.
- Fall 12: lift 4-7 px on every frame; width 20-26 as it turns; frames 6-11 repeat 0-5.
- Mini (boat): 7-8 wide, 9-11 tall in a 16 px cell, feet on the bottom row (Yossele 8 x 10).
Each final came as assets/ (exact file names), previews/ (x8 sheets and GIFs per row), reference/ (the key), DELIVERY.txt (one line per file: size, colours, checks) and MANIFEST.json. Deliver Spool the same way at the end.

PART 5: SPOOL'S 13 FILES (exact names and sizes; frames per row)
- Spool-Sheet.png 192x256, 32 px cells: rows 0-3 idle v0, v1 (mirror of v0), v2, v3 (mirror of v2), 6 frames each; rows 4-7 move v0-v3 the same way.
- SpoolAttack-Sheet.png 128x128: 4 rows (v0, v1 mirror, v2, v3 mirror) x 4 frames.
- SpoolPatch-Sheet.png 192x128: 4 rows x 6 frames (the Patch ability).
- SpoolIntroR-Sheet.png 384x32: land, 12 frames, one row.
- SpoolFall-Sheet.png 384x32: fall, 12 frames, one row.
- SpoolIdlejustidle-Sheet.png 192x32: menu idle, 6 frames.
- SSpoolSheet.png 96x128, 16 px cells: rows 0-3 mini idle v0-v3, rows 4-7 mini move v0-v3, 6 frames each (v1 and v3 mirrors).
- Spool_Icon.png 32x32; Spoolendstat1.png 32x32 (won); Spoolendstat2.png 32x32 (defeated); Readyup6spool.png 12x12; Spooltabicon.png 32x32; Spoolsplash.png 128x128.

THE STEPS (answer the quiz, wait for "go", draw, wait for "approved")

STEP 0: TAKE YOSSELE AND ARGO APART, STARTING FROM THEIR KEY FRAMES (no drawing)
Dimitri's finals were each built out of one approved key frame: Yossele_idle_v0_frame0.png, Argo_front_approved.png (and Argo_back.png for the back). Work out, in Python on the attached files, exactly how every sheet grew out of its key, so you build Spool's the same way. Three parts, each delivered on its own and closed by Dimitri's "approved":
0a YOSSELE, 0b ARGO, each:
  1. The key: its box (columns, rows, w x h), its colours, and its parts with their pixel coordinates.
  2. Where the key appears pixel for pixel (check every sheet: idle v0 frame 0, land frame 11, menu idle frame 0, and for Argo the back key as idle v2 frame 0).
  3. For every sheet and row, frame by frame: which parts of the key stay pixel-identical, which move (by how many px, which way), which are redrawn, and the top, lift, w x h and pixels changed (check against Part 4).
  4. The facings: confirm v1 and v3 are exact mirrors; how the back (v2) relates to the front key: what is kept, what is new.
  5. The mini (16 px) and the UI files: which parts of the key survive at that size, and how each was simplified.
0c THE METHOD: 5-10 rules, in your words, for growing a full set of sheets out of one key frame, taken from 0a and 0b (what stays frozen, what moves first, how big each animation's motion is, where the key must reappear). These rules then govern Steps 1-9 for Spool.
Quiz before 0a: what you will measure, with which Python checks, and how you will show it (tables per sheet, and an x8 picture marking the key's parts on Yossele's key and on Argo's).


STEP 1: THE FOUR FACINGS (one frame each: idle frame 0 of v0, v1, v2, v3)
Quiz: (a) Which way does the key frame face, and which facing is it? (b) What does the back (v2) show that the key frame does not, taken from the concept sheet? What disappears? (c) For v1: confirm it is the exact pixel mirror of v0, and name anything you think must not swap sides. (d) Size and centre of each of the four. (e) Which colours you will use.

STEP 2: IDLE (rows 0-3 of Spool-Sheet, 6 frames each)
Quiz: (a) Frame by frame for v0: the top row of the drawing, what moves (the breath and one secondary part, named), and the pixels you expect to change. (b) Which frame is the key, pixel for pixel. (c) How the frames loop from 5 back to 0. (d) What changes for v2.

STEP 3: MOVE (rows 4-7 of Spool-Sheet, 6 frames each)
Quiz: (a) Which frames carry the footstep (Spool: 1 and 4), and what the body does on them. (b) Frame by frame for v0: the top row, the lift (if any), what each limb does, how the part that trails along the floor (from your concept) follows. (c) Hop or squash and stretch, and why for this body. (d) Pixels changed per frame, against Part 3. (e) What changes for v2.

STEP 4: ATTACK (SpoolAttack-Sheet, 4 frames per facing)
Quiz: (a) What hits (which limb or part), per the concept sheet. (b) Frame by frame: anticipation, strike, follow-through, recovery: the pose, the top row, the width. (c) Any flash or effect, and its colour (ask). (d) How frame 3 leads back into idle.

STEP 5: PATCH (SpoolPatch-Sheet, 6 frames per facing)
Quiz: (a) What Patch looks like: the needle, the thread, where the hands are. (b) Frame by frame: the pose and what moves. (c) Any colour beyond the 8 (ask for each). (d) How it reads from the back (v2).

STEP 6: LAND (SpoolIntroR-Sheet, 12 frames)
Quiz: (a) Frame by frame: lift, height and width, the squash and the stretch, against Part 3 and Argo's land. (b) Which frame hits the ground. (c) Frame 11 = the key, pixel for pixel. (d) Any dust (colour, ask).

STEP 7: FALL (SpoolFall-Sheet, 12 frames, a loop)
Quiz: (a) Frame by frame: lift (it never touches the ground), the turn, what each limb and the trailing part do. (b) How frame 11 flows into frame 0. (c) Whether frames 6-11 repeat 0-5 (as Yossele and Argo do) or are all new.

STEP 8: MENU IDLE AND BOAT MINI
Quiz: (a) SpoolIdlejustidle-Sheet = the approved idle v0 row, copied exactly: confirm. (b) For SSpoolSheet: which parts survive at about 7-8 wide x 10 tall in a 16 px cell, which go, and how idle and move read at that size; v1 and v3 are mirrors.

STEP 9: THE UI SET (one file at a time, a quiz for each)
- Spool_Icon (32x32): the character's head or bust filling most of the square, readable at 1x, like Yossele's and Argo's icons.
- Spoolendstat1 (32x32): a happy, winning pose. Spoolendstat2 (32x32): defeated, a slumped pose (not only new eyes). Feet on the bottom row.
- Readyup6spool (12x12): the face inside the white corner brackets the base ready-up icons use (see Readyup6yossele / Readyup6argo).
- Spooltabicon (32x32): a white FFFFFF 1 px line drawing of the head, open inside, clear around; no fill, no other colour.
- Spoolsplash (128x128): line art, not a filled figure: a 3 px outline in an accent colour, an inset line in the body colour, and an inside fill of the accent at alpha 70 (the one half-clear exception); one shape per body part (see Yosselesplash and Argosplash).
Quiz for each: what it shows, its size and position in the square, the colours, and how it matches Yossele's and Argo's version.

AT THE END: package Spool like the two finals (assets/, previews/, reference/, DELIVERY.txt, MANIFEST.json) and stop.
```
