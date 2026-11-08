# Feedback for ChatGPT: Spool's key frame (batch 2B), 2026-11-08

Dimitri, 2026-11-08, with ChatGPT's picture "Selected game study" beside "Refined 32x32 key - 8x": "I want ChatGPT to
make this keyframe more similar to the art style of TDS." The picture is **Spool** (hooded sage plush, stitched face
opening, yellow eyes, pink tongue, severed arm with cotton stuffing; `docs/STYLE.md` Vessels, `docs/concepts/YOSSELE_SPOOL_ULOE.md` §2),
the key frame batch 2B of `FEEDBACK_ROUND1_2026-11-07.md` asks for before the sheets.

**What was measured.** The 32x32 key was read back from the 8x picture (every 8x8 block one flat colour, so the read
is exact; where it sat in its 32 px canvas is not shown, so it was placed feet-down, centred). It is 27 x 30 px, 523
pixels, 14 colours. `concept-tools/temp_art/art_check.py --cell 32 --size 32x32 --baseline` passes it (opaque, no
`000000`, no foe magenta). Against the house rules (kit `docs/18` §2-§4) and his concept (`docs/concepts/art/spool_8x.png`,
about 18 x 19 px):

| Check | Key frame | House or target |
| --- | --- | --- |
| Size | 27 x 30 | vessels 15-27 x 19-28 (never over 28 tall); his Spool about 18 x 19; batch 2B 18-20 x 19-22 |
| Body ramp | `8E9F80` > `718667` > `586E52` > `3F5442`: hue 92° to 128°, value 32-62%, grey-olive | sage `93CCA1` / `60A871` (h134, one hue); shade keeps the hue within 2° |
| Texture | 113 of 523 pixels (22%) are single specks of their colour | Fie 13%, Smacky 12%, round 1 Spool 14%: flat fills, 2-3 tones per material |
| Face | a 6 px-tall dark mask, 12 scattered `E4DEDA` specks, eyes 3 px of `E6D85A` with `AC9D43` / `F6E791` edges | a 2-3 px stitched slit, a ring of alternating `E4DEDA` / `9A938E` stitches round `1F3325`, two 1 x 2 eyes |
| Arm | a tentacle curling up from the right side, joined to the body | his concept: the severed arm lies on the floor to the left, separate, cotton at the wound (it is not a tail) |
| Stuffing | cream blossoms over the shoulder and belly | stuffing only at the severed arm's wound |
| Hands | none read | two small glove hands, 3 x 3, `DDDDDD` |
| Light | 10 of 42 left-edge pixels in the darkest tones | left and top edges lit; dark only on the right and underside |

**Keep:** the pointed hood and the head-heavy chibi mass, the dark opening with yellow eyes peering out, the tongue
on the right, the exact `E4DEDA`, `E6D85A`, `E58CC0` it already uses, and the clean mechanics.

**How to use it.** In the ChatGPT Project "Rogue Waves Art" chat where the key frame was made (or a new one, on GPT-6
Sol Medium), paste the block below. A copy is in project files, `chatgpt/prompts/feedback_spool_key_2026-11-08.txt`.

```text
SPOOL KEY FRAME: MAKE IT READ AS TDS ART

Your refined 32x32 key (idle_v0, frame 0) is Spool. It passes the mechanical checks (opaque, no 000000, no foe magenta). Keep that, the pointed hood, the big head-heavy mass, the dark face opening with yellow eyes looking out, the tongue on the right, and the exact E4DEDA, E6D85A and E58CC0. It still reads as a painted study shrunk down, not as a TDS vessel. Redraw the key with these changes, describe it in 2-4 lines first, then render the 32x32 PNG itself and an x8 preview, and wait for my OK before any sheet.

1. Size. Now 27 px wide and 30 tall: too big (TDS vessels are 15-27 x 19-28, never over 28 tall). Make Spool 18-20 px wide and 19-22 px tall, like my concept in his_sprites.png. Feet on the bottom row (row 31), body centred on column 16, a clear margin on both sides.
2. Colours. The body is now a grey-olive ramp (8E9F80, 718667, 586E52, 3F5442) that drifts from yellow-green to blue-green and is too dark. Use only the agreed sage: 93CCA1 for the lit body, 60A871 for its shade, thread 4F8C5E as the deepest shade under the hood brim and the feet. One hue for the whole ramp.
3. Flat fills, no paint texture. 22% of the pixels are lone specks of their colour (TDS vessels 12-13%). Fill each form with its lit tone, add one band of shade on its right side and underside, and stop. 2-3 tones per material, 10-16 colours in the whole frame. No dithering, no noise, no lone pixels except an eye glint.
4. Light from the upper left. The left and top edges take the lit tone 93CCA1; 60A871 and 4F8C5E go only on the right edge, the underside and under the hood's overhang. No dark rim anywhere.
5. The face. The opening is now a big dark mask. Make it a narrow stitched slit 2-3 px tall across the hood's face: the hollow 1F3325, ringed by alternating E4DEDA / 9A938E stitch pixels (stitches, never teeth). Two eyes inside, each 1 px wide and 2 px tall in E6D85A, one pixel apart in height if you like. Drop the AC9D43 and F6E791 edge pixels round the eyes.
6. The tongue: 2-3 px of E58CC0 hanging from the right end of the slit, one shade pixel below it at most. One grey pin pixel (9A938E) may poke out at the hood's top right.
7. The hands: two small glove hands, 3x3 each, DDDDDD lit with CFCEDF shade, held out at the sides.
8. The arm. Remove the tentacle joined to the body. Spool's severed arm lies on the floor as a separate object about 2 px to the LEFT of the body, on the bottom row, sage like the body, with a small E4DEDA cotton tuft spilling from the wound. Never joined to the body and never a tail.
9. Stuffing. Remove the cream blossoms from the shoulder and belly. The only cotton is the tuft at the arm's wound.
10. Legs. Two short stubby legs, 2-3 px tall, so the head-and-hood mass is more than half the height.

When I OK the key, the sheets follow batch 2B of my round 1 feedback.
```
