# Spool sprite breakdown (for ChatGPT), 2026-11-08

Dimitri, 2026-11-08 08:21Z: "Give a detailed breakdown of Spool's sprites so that it understands what the vision is for
Spool exactly." Then, 08:35Z: **"Use the current Spool sprites as the key art that is currently in the Art Bench."**

**The key art.** The Art Bench (https://claude.ai/artifact/PgF7QP6n2HN56syTDUaUi5, built 2026-11-06) shows the code's
own Spool: Dimitri's reshade of his own sprite in his sage colours (`TDS_Mod/Assets/SpoolMod/Editor/SpoolCharacter.cs`,
`Front` and `Back`), animated by the code. Its 13 character files are copied here unchanged, in `spool/artbench_key/`:
`Spool-Sheet`, `SpoolAttack-Sheet`, `SpoolPatch-Sheet`, `SpoolIntroR-Sheet`, `SpoolFall-Sheet`,
`SpoolIdlejustidle-Sheet`, `SSpoolSheet`, `Spool_Icon`, `Spoolendstat1`, `Spoolendstat2`, `Readyup6spool`,
`Spooltabicon`, `Spoolsplash`, plus the key frame alone (`Spool_key_idle_v0_f0.png`, its x8) and the main sheet at x4.
The key frame is **approved and frozen pixel for pixel** (hard rule 6): 18 x 19 px, one connected piece, 12 colours,
24% lone specks (his pixels; the speck limit applies to new pixels only, hard rule 18). It replaces the target key of
PR #184 (ChatGPT's version 01 fixed), which is removed. The game itself still runs ChatGPT's round 1 Spool
(`TDS_Mod/ModSource/Spool/Art/`) until new sheets come in.

**How to use it.** Upload the block below to the ChatGPT Project as `SPOOL_SPRITE_BREAKDOWN.txt` with the files of
`spool/artbench_key/` (copies in project files, `chatgpt/prompts/spool_sprite_breakdown_artbench_2026-11-08.txt` and
`chatgpt/prompts/spool_artbench_key/`). It replaces the Spool parts of `FEEDBACK_ROUND1_2026-11-07.md` batch 2B and
`FEEDBACK_SPOOL_KEY_2026-11-08.md`; sizes and row order come from `ASSET_MANIFEST.md`.

```text
SPOOL: THE SPRITE, PART BY PART (2026-11-08, Art Bench key)

Dimitri, 2026-11-08: "Use the current Spool sprites as the key art that is currently in the Art Bench." Attached: Spool_key_idle_v0_f0.png (32x32) and its x8 preview, and Spool's 13 Art Bench files (Spool-Sheet.png and the rest). The key frame is approved and frozen: never redraw, re-proportion, re-colour or "clean" it. Every frame you deliver is an edit of it. Where any earlier note, handoff, version or picture disagrees (version 01, the target key with claws, ChatGPT's round 1 Spool), this wins.

1. WHO SPOOL IS, IN ONE LINE
A small hunched sage-green plush in a pointed hood, its face a wide dark mouth-opening rimmed with a zipper of small teeth top and bottom, two yellow eyes inside, a pink tongue hanging from the front corner, two white gloved hands at its sides, short legs, and one arm lying behind it along the floor, joined to it at the hip and dragged wherever it goes, ending in pale tufts.

2. THE KEY FRAME (Spool-Sheet idle_v0, frame 0): THE MAP
Facing v0 = toward the camera, heading screen-right. The hood and mouth face RIGHT; the floor arm trails LEFT.
Columns 0-31 left to right, rows 0-31 top to bottom. "." = transparent. Rows 0-12 are empty.
Legend: L 93CCA1 sage / s 60A871 sage shade / H 1F3325 mouth inside / n 26422F mouth edge / W E4DEDA teeth and cuffs / g 9A938E teeth shade / C F6F3F8 glove / c D8D1E2 glove shade / F FFFFFF cotton / P E58CC0 tongue / p C96CA6 tongue shade / Y E6D85A eyes

   01234567890123456789012345678901
13 ..................LLs...........
14 ...............LLLLLss..........
15 .............LLLLLLLsss.........
16 ............LLLLsWgWgWs.........
17 ...........LLLsWgHHHHHgg........
18 ...........LsWgHHHHHHHW.........
19 ...........sgHHHYHHHYHn.........
20 ...........WHHHHHHHHHH..........
21 ...........sgWgWgWgWPpW.........
22 ...........LssssssssPps.........
23 ............LLLLLLLssp..........
24 .........Cc..LLLLsss............
25 ..........Cc.LLLsssW.W..........
26 .........CCcLLLLssWCc...........
27 ..........ssLLLLLssWCc..........
28 ..........LLLLLLLLss............
29 ........c.Ls.LLLLLss............
30 ......c.sLLs.Ls...ss............
31 ......FLLLs..Ls...sss...........

3. THE PARTS, WITH THEIR PIXELS
- Box: 18 px wide (columns 6-23), 19 px tall (rows 13-31). Feet on row 31.
- Hood: rows 13-18, its point at columns 18-19 on row 13, sloping down to the left to column 11 at row 17. The hood is the top third and leans forward over the mouth: the hunch.
- Mouth-opening: rows 17-20, columns 12-22, 1F3325 inside, one 26422F pixel at its front corner (column 22, row 19). It is big: it takes most of the face.
- Teeth, a zipper top and bottom: the top row alternates E4DEDA / 9A938E from column 17 to 21 (row 16) and runs down the back edge (columns 12-16, rows 17-19); the bottom row alternates E4DEDA / 9A938E on row 21 (columns 12-19). Small single-pixel teeth, never fangs.
- Eyes: two single E6D85A pixels on row 19, columns 16 and 20, inside the dark.
- Tongue: E58CC0 at column 20 on rows 21-22, its C96CA6 shade beside it (column 21, rows 21-23), hanging from the front corner.
- Body: rows 22-28, columns 10-19, under the hood; 60A871 on its right side and on row 22 under the mouth.
- Gloves: two white gloved hands. The back one at the left, columns 9-11, rows 24-26 (F6F3F8 with D8D1E2 shade); the front one at the right, columns 18-21, rows 25-27, with E4DEDA cuff pixels on columns 18-19.
- Legs: two stubby legs, columns 13-14 and 18-20, rows 29-31.
- The floor arm (the signature): from the hip (columns 10-11, rows 27-29) it lies down and back along the floor to column 6, rows 29-31, ending in pale tufts: D8D1E2 at column 8 row 29 and column 6 row 30, FFFFFF at column 6 row 31. It is joined to Spool and goes where it goes: on every move frame it is dragged behind him along the floor, in the air it rides with the body.
- One piece: every pixel touches another (8-way).

4. COLOURS
The key's 12: 93CCA1, 60A871, 1F3325, 26422F, E4DEDA, 9A938E, F6F3F8, D8D1E2, FFFFFF, E58CC0, C96CA6, E6D85A. For effects only, as in the Art Bench sheets: CBE6D1 / A2CFAD thread, C9D2DD / 8C96A3 needle, BDB3A8 dust, F8E39A sparkle. Spoolendstat2 alone keeps the greyed tones of its Art Bench file (709A76, 8FB093, 938D87, AF7696, B8B3BA, C089A5, C0BAB5, CAC7C7, D0CECB). No other colour; no olive, no grey-green, no purple.
Lone specks: the key's own count as approved; new pixels you add must not raise a frame above 25%.

5. SILHOUETTE: WHAT MUST READ AT 1x
A pointed hood leaning forward, a wide dark mouth with a light zipper rim, two yellow dots, a pink drip, two white blobs (the gloves) at the sides, short legs, and a thin arm trailing back along the floor to a pale tip.

6. THE FOUR FACINGS (the Art Bench rows)
- v0 (row 0): the map above.
- v1 (row 1): the mirror of v0. The Art Bench row is a plain flip (shade on the left); re-shade it so 60A871 sits on the right side and underside of each form again. Nothing else changes.
- v2 (row 2): the back, from SpoolCharacter's Back view: the back of the hood (no mouth, no eyes, no tongue; one 9A938E pixel at its left edge), a 60A871 band at row 22 where the hood meets the body, the two gloves, the legs, and the floor arm trailing to the RIGHT with its FFFFFF and D8D1E2 tufts. Keep it as the Art Bench draws it.
- v3 (row 3): the mirror of v2, re-shaded like v1.

7. EVERY SHEET (start from its Art Bench file: same poses, same timing, the key frozen)
Each Art Bench sheet is the starting frame set. Keep its design and poses; change only what the manifest rules below ask for, and say per sheet which frames changed.
- Spool-Sheet (192x256): idle rows breathe (the hood swings, the gloves move). Keep. Move rows: the hood leads; the floor arm drags along the floor behind. Check the landing frames 2 and 5 (legs apart, body down) and a 2-3 px bob.
- SpoolAttack-Sheet (128x128): the hood swings wide open on the strike, the front glove throws, a pale flash. Keep; anticipation on frame 0, strike on 1-2, recovery on 3.
- SpoolPatch-Sheet (192x128): the front glove holds the needle up, stitches out with a thread, a sparkle. Keep; every facing moves.
- SpoolIntroR-Sheet (384x32): drops in with the mouth wide, squashes flat with dust, unfolds; frame 11 must equal the key exactly.
- SpoolFall-Sheet (384x32): the Art Bench row barely leaves the ground; this one must be REDRAWN as a mid-air loop (lowest pixel 3+ px above row 31 on at least 10 of 12 frames, the body turning, the floor arm and gloves flailing, the mouth open). Same parts and colours.
- SpoolIdlejustidle-Sheet (192x32): the menu idle; keep.
- SSpoolSheet (96x128, 16 px cells): the boat mini; keep, re-shade v1 and v3.
- Spool_Icon, Spoolendstat1 (happy, mouth open, sparkles), Spoolendstat2 (defeated, greyed and slumped), Readyup6spool (the face in white corner brackets), Spooltabicon (white 1 px line drawing), Spoolsplash (line art): keep as the Art Bench draws them unless Dimitri asks.

8. CHECK BEFORE YOU DELIVER A SPOOL FRAME
- The key frame byte for byte equal to Spool_key_idle_v0_f0.png wherever it appears (idle_v0 frame 0, IntroR frame 11).
- Same parts as the key: hood, mouth with the zipper rim, two eye pixels, tongue, two gloves, legs, the floor arm joined and trailing.
- Feet on row 31 on ground frames; never taller than 22 px on a ground frame.
- Only the colours of section 4; shade on the right and underside in every facing.
```
