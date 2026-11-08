# Spool sprite breakdown (for ChatGPT), 2026-11-08

Dimitri, 2026-11-08: "ChatGPT is not understanding the key frame sprites given too well. Give a detailed breakdown of
Spool's sprites so that it understands what the vision is for Spool exactly." The vision is his word of 2026-11-08
03:20Z: "the arm is attached, but dragging behind him palm facing up with long claws. Keep the zipper like teeth and
hunched posture", on top of the original hunched key (ChatGPT's version 01, `round2/batch2a/spool_key_lazy_v4`).
`docs/STYLE.md` (Spool row) and scope `08` Look say the same.

**Target key.** `spool/Spool_target_key_2026-11-08.png` (32x32) and its x8 preview are version 01 with only its faults
fixed (arm joined, cotton patches and pink specks removed, shading moved to the mid tone). It is a **layout guide**:
the size, pose, parts and pixel positions to build from. `art_check.py --cell 32 --size 32x32 --baseline` passes it:
27 x 22 px, one connected piece, 8 colours, 10% lone specks. ChatGPT may refine the shading and the claws inside
these rules, never move a part or change a size.

**How to use it.** Upload the block below to the ChatGPT Project as `SPOOL_SPRITE_BREAKDOWN.txt` with the two PNGs
(copies in project files, `chatgpt/prompts/spool_sprite_breakdown_2026-11-08.txt` and `chatgpt/prompts/spool/`), and
attach them to the chat that draws Spool. It replaces the Spool parts of `FEEDBACK_ROUND1_2026-11-07.md` batch 2B
and `FEEDBACK_SPOOL_KEY_2026-11-08.md`; the sheet list and sizes come from `ASSET_MANIFEST.md`.

```text
SPOOL: THE SPRITE, PART BY PART (2026-11-08)

This is the exact vision for Spool. Where any earlier note, handoff or picture disagrees, this wins. Attached: Spool_target_key_2026-11-08.png (32x32, the key frame layout) and Spool_target_key_2026-11-08_8x.png.

1. WHO SPOOL IS, IN ONE LINE
A small hunched plush creature in a sage-green hood, leaning forward, its face a dark opening with a zipper of teeth along the top, two yellow eyes glowing inside, a pink tongue hanging out, and one long arm, still attached at the back shoulder, dragging behind it on the floor with the palm facing up and three long ivory claws standing up from it.

2. THE KEY FRAME (idle_v0, frame 0): THE MAP
Facing v0 = toward the camera, heading screen-right. Spool leans to the RIGHT; the arm trails to the LEFT.
Columns 0-31 left to right, rows 0-31 top to bottom. "." = transparent.
Legend: L 93CCA1 sage lit / s 60A871 sage shade / d 4F8C5E sage deepest / H 1F3325 hollow / W E4DEDA ivory (teeth, claws) / g 9A938E grey (teeth) / P E58CC0 tongue / Y E6D85A eyes

   01234567890123456789012345678901
10 .....................LL.........
11 ...................LLLLss.......
12 ..................LLLLLLss......
13 ................LLLLLLLLLss.....
14 ..............LLLLLLLLLLLLss....
15 .............LLLLLLLLLgWgWgWs...
16 ............LLLLLLLgWgHHHHHLg...
17 ............LLLLgHHHHHHHHHHHW...
18 ...........LLLLWHHHHYYHHYYHWg...
19 ...........LLLLgHHHHHHHHHHgd....
20 ..........LLLLLLLLLLLLdddPP.....
21 .........LsLLLLLLLLsddd..PP.....
22 ........LsLLLLLLLLss.....P......
23 .......Ls.LLLLLLLsLL............
24 .......Ls.LLLLLLLsLs............
25 .......Ls.LLLLLLsLLLLs..........
26 .......Ls.LsLLLLsLsssd..........
27 ..W.W.WLs.LLLLLLsLs.............
28 ..W.W.WLs.ssLLsdLLs.............
29 ..W.W.WLs...LLs.LLs.............
30 ..LLLLLLs...LLs.LLs.............
31 ...dddddd...ddd.ddd.............
(Rows 0-9 are empty.)

3. THE PARTS, WITH THEIR PIXELS
- Box: 27 px wide (columns 2-28), 22 px tall (rows 10-31). Never taller than 22 here, never wider than 27. Two empty columns on each side.
- Hood: rows 10-19, the top 10 rows (45% of the height). Its point is at columns 21-22, row 10, leaning forward-right; the back of the hood slopes down to column 11-12 at rows 16-19. The hood is the biggest single mass and the face sits in it, pushed forward: this is the hunch.
- Face opening: rows 16-19, columns 16-27. Hollow 1F3325, 3-4 rows tall, wider at the front.
- Zipper teeth: along the TOP edge of the opening only: alternating 9A938E / E4DEDA, one pixel each, from column 19 (row 16) to column 27 (row 15), following the opening's slope. Single E4DEDA / 9A938E pixels mark the opening's corners (rows 17-19). They read as a zipper, a row of small teeth; never a full ring, never a grin of big teeth.
- Eyes: two E6D85A dashes, each 2 px wide and 1 px tall, on row 18 at columns 20-21 and 24-25, inside the hollow, one pixel apart from each other by the hollow. They glow; no pupils, no outline.
- Tongue: E58CC0, 2 px wide on rows 20-21 (columns 25-26), 1 px on row 22 (column 25), hanging from the front end of the opening, below the hood.
- Body: rows 20-28, columns 10-21. Short and round, smaller than the hood, tucked under it. One fold: a 60A871 line down column 16-17 (rows 23-27). Right side and underside in 60A871, deepest 4F8C5E only under the hood (row 20-21, columns 20-24) and where the legs meet the body (row 28, column 15).
- Near arm (default): short, hangs in front of the belly on the right: rows 23-26, columns 18-21, ending in a small 3 x 2 sage hand (rows 25-26). No claws on it.
- Legs: two stubby legs, 3 px wide, rows 28-31: columns 12-14 and 16-18, one empty column between. Feet = row 31 in 4F8C5E. Feet on row 31 in every ground frame.
- THE DRAGGING ARM (the signature): attached at the back shoulder (row 20-21, columns 9-11) and joined to the body there; it runs down and back as a 2 px band (lit 93CCA1 on its left pixel, 60A871 on its right) from columns 9-10 (row 21) and 8-9 (row 22) to columns 7-8 (rows 23-29), with ONE empty column (column 9) between it and the body from row 23 down, so it reads as an arm and not as body. It ends on the floor in an open hand lying PALM UP: the palm is row 30, columns 2-8 (lit, it faces up), its underside row 31 in 4F8C5E.
- Claws: three long E4DEDA claws standing straight up from the palm at columns 2, 4 and 6, each 3 px tall (rows 27-29), one empty column between claws. They are the only ivory below the face. A claw may curl 1 px at its tip toward the left (the trailing side) if it stays joined to the claw.
- One piece: every pixel touches another (4-way). Nothing floats, nothing lies apart.

4. COLOURS (only these)
93CCA1 lit sage (left and top edges, every form's main fill) / 60A871 sage shade (right side and underside of every form, the fold, the arm's right pixel) / 4F8C5E deepest (under the hood's overhang, under the body, the feet, the palm's underside; never on a left or top edge) / 1F3325 hollow (face opening only) / E4DEDA ivory (teeth, claws) / 9A938E grey (alternate teeth, opening corners) / E58CC0 tongue (tongue only) / E6D85A eyes (eyes only).
Optional, only when a frame needs it: CBE6D1 pale thread (the Patch thread, the attack flash), 9A938E needle (Patch). No other colour. No white FFFFFF, no gloves, no cotton patches, no pink outside the tongue, no olive or grey-green, no purple.
8-12 colours per frame. Lone specks at most 13% (the teeth and claw tips count).

5. SILHOUETTE: WHAT MUST READ AT 1x
From far away Spool is: a pointed hood leaning forward (top right), a dark band with a light zipper edge under it, a small body, two stubs of legs, and a thin line running back along the floor to a little fan of three pale spikes (bottom left). If the dragging arm or the claw fan disappears at 1x, the frame is wrong.

6. THE FOUR FACINGS
- v0 (row 0 of each sheet): the map above. Leans right, arm trails left.
- v1: the exact mirror of v0 (leans left, arm trails right). Then re-shade: 60A871 / 4F8C5E stay on the RIGHT side and underside of each form (the light stays upper left). Nothing else changes.
- v2 (seen from behind, heading right): the back of the hood (no opening, no teeth, no eyes, no tongue), the hood point still leaning right, the back of the body, the two legs, and the dragging arm trailing to the LEFT along the floor with the palm up and the three claws visible. The near arm is hidden behind the body.
- v3: the exact mirror of v2, re-shaded the same way as v1.

7. EVERY SHEET, FRAME BY FRAME (sizes and row order from ASSET_MANIFEST)
Every frame is an edit of the key; the hood, face, colours and size never change. The arm stays attached in every frame of every sheet.

Spool-Sheet.png (192x256, 32 px cells; rows idle_v0, v1, v2, v3, move_v0, v1, v2, v3; 6 frames each)
- Idle, 6 distinct frames: breathing. Frames 0-1 as the key; frames 2-3 the hood and face 1 px lower (the body squashes 1 px); frames 4-5 back up. Secondary motion: the tongue swings 1 px (out on 1 and 4); the claws twitch: one claw tip bends 1 px on frames 2 and 5. The hand stays on the floor. 2-60 pixels change per frame.
- Move, 6 frames: a hunched shuffle. Landing (contact) poses on frames 2 and 5 (the game plays the footstep there): legs apart, body 1 px down. Passing poses on 0-1 and 3-4: legs together, body 1-2 px up. The whole body bobs 2-3 px over the cycle; the hood leads, tilting 1 px further forward on the contact frames. The dragging arm lags: the hand slides along the floor 1-2 px behind the body's motion and the claws lean back (toward the trailing side) on the contact frames, upright again on the passing frames. 45-350 pixels change per frame.

SpoolAttack-Sheet.png (128x128; rows attack_v0, v1, v2, v3; 4 frames each)
The melee attack uses the claw arm (default). 0: anticipation: the body leans back 1-2 px and the dragging arm lifts off the floor behind it, coiling up. 1: strike, the biggest pose: the arm whips over and forward past the face, the palm and claws spread in front; one pale CBE6D1 arc 1 px wide trails the claws. 2: follow-through: the claws come down in front at floor level, the body leans 1-2 px forward. 3: recovery: the arm slides back toward the trailing pose. 200-780 pixels change per frame.

SpoolPatch-Sheet.png (192x128; rows patch_v0, v1, v2, v3; 6 frames each)
PATCH mends a unit or building: a needle-and-thread pass with the near hand; the claw arm stays dragging. 0-1: the near hand rises to face height holding a 9A938E needle (1 x 3 px) with a CBE6D1 thread. 2-3: it stabs down and forward (the stitch), the body dipping 1 px. 4-5: it pulls the thread up and back in a 1 px CBE6D1 loop. Three poses that read; every facing moves (v2 and v3 too).

SpoolIntroR-Sheet.png (384x32; land, 12 frames, one row, facing v0)
0-3: dropping in, airborne: the lowest pixel 8, 6, 4, 2 px above row 31; the arm and claws trail ABOVE the body. 4-6: impact squash to about 13 px tall (60%), the hand slapping flat on the floor, claws splayed. 7-10: unfold, overshoot 1 px taller than the key on frame 9. 11: exactly the key (idle_v0 frame 0). All 12 distinct.

SpoolFall-Sheet.png (384x32; fall, 12 frames, one row)
A mid-air tumble that loops, never a collapse: on at least 10 of 12 frames the lowest pixel is 3+ px above row 31. The body turns 30-45 degrees per frame; the dragging arm whips round it, claws trailing; the tongue flaps. Frame 11 flows into frame 0. List each frame's lift in the delivery line.

SpoolIdlejustidle-Sheet.png (192x32; menu_idle, 6 frames): the same 6 frames as idle_v0.

SSpoolSheet.png (96x128, 16 px cells; mini_idle v0-v3, mini_move v0-v3; 6 frames each)
The mini vessel, about 7 wide by 10 tall plus the arm: the hood point and slope (rows 0-4 of the drawing), a 1-row 1F3325 band with 2 pixels of E6D85A and 1 pixel of E4DEDA above it, a 1 px E58CC0 tongue pixel, a small body, and a 1 px arm line on the bottom row trailing back to one E4DEDA claw pixel. Idle breathes 1 px; move bobs 1-2 px. v1 and v3 mirror v0 and v2.

Spool_Icon.png (32x32): the head read big: the hood, the opening with its zipper, the two eyes, the tongue, and the claw hand raised palm up beside it. Same palette, same 1 px pixels (never a scaled-up sprite).

Spoolendstat1.png (32x32, happy): the key pose straightened a little, eyes as two upturned 3 px arcs, tongue out, the claw hand lifted palm up as a wave.
Spoolendstat2.png (32x32, defeated): slumped: the hood point flopped down over the face, the body flattened 3-4 px, eyes as two 1 px-tall dashes near the bottom of the opening, the arm limp along the floor with the claws curled flat, tongue hanging lower.

Readyup6spool.png (12x12): the face in the white corner brackets of the base ready-up icons: the hood point, a 1-row dark band with two E6D85A pixels and an E4DEDA zipper pixel above it, one E58CC0 pixel below.

Spooltabicon.png (32x32): a white FFFFFF 1 px line drawing of the head on a clear ground: the hood outline with its point, the opening's outline with 3-4 zipper ticks along its top. No fill, no colour.

Spoolsplash.png (128x128): line art in the splash style: a 3 px accent outline in 4F8C5E, an inset 93CCA1 line, a translucent fill of 60A871 at alpha 70, one shape per part: hood, opening (1F3325 fill opaque), each eye, tongue, body, each leg, the dragging arm, the palm, each of the three claws. Same pose as the key.

8. CHECK BEFORE YOU DELIVER A SPOOL FRAME
- Box: 27 x 22 or less on a ground frame; feet on row 31; two empty columns each side.
- One connected piece; the arm joined at the shoulder; one empty column between the arm and the body below the shoulder.
- Hood point leaning the way Spool heads; zipper only on the top edge of the opening; two eye dashes; tongue at the front.
- Palm up on the floor, three claws standing up, ivory only on teeth and claws.
- Only the colours of section 4; light upper left; 60A871 / 4F8C5E only on right edges, undersides and under overhangs.
- 8-12 colours, at most 13% lone specks.
```
