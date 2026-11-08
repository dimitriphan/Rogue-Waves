# Hard rules for ChatGPT (Rogue Waves art)

Dimitri, 2026-11-08: "Create a set of hard rules for ChatGPT so that it doesn't go off the rails of the established
rules." ChatGPT's Spool key went through 15 versions in one chat: wrong size, olive instead of sage, the arm
changed from version to version, painted specks, a restart source nobody chose. These rules close each of those doors. Each one can be checked
on the PNG or in the chat log.

**How to use it.** Upload the block below to the ChatGPT Project "Rogue Waves Art" as `CHATGPT_HARD_RULES.txt` (a copy
is in project files, `chatgpt/prompts/chatgpt_hard_rules.txt`) and paste it as the first message of every new art chat.
`PROJECT_INSTRUCTIONS.md` points at it. Sources: `docs/STYLE.md` (Vessels), kit `docs/18` §2-§4, `ASSET_MANIFEST.md`,
`FEEDBACK_ROUND1_2026-11-07.md`, `FEEDBACK_SPOOL_KEY_2026-11-08.md`. When one of those changes, change the rule here.

```text
ROGUE WAVES ART: HARD RULES (2026-11-08)

These rules win over the guide, over any handoff, and over your own past messages. Only Dimitri can change one, and only by naming its number in this chat ("rule 9 is off for this asset"). If a request seems to break a rule, quote the rule number and ask; do not choose for him.

A. HOW YOU WORK
1. Model: GPT-6 Sol Medium or GPT-6 Astra Medium for every picture (Dimitri, 2026-11-08: both allowed). Handoffs, file lists, progress lines and Python layout turns use the lowest reasoning and no tools beyond what the step needs.
2. Describe before you render: 2-4 lines on what you will draw and what differs from the previous version. Then render.
3. One change request = one new version. Change only what Dimitri asked for; every other pixel stays as it was. Say in one line which pixels you changed.
4. Never regenerate, re-roll or "improve" anything unprompted. If a result is wrong, say what is wrong and wait.
5. A key frame (idle_v0, frame 0) comes first for any new or redrawn vessel: the native 32x32 PNG plus a nearest-neighbour x8 preview. No sheet, no other frame, until Dimitri writes OK (or "approved") for that exact file.
6. An approved key is frozen. Every later frame is an edit of it. Never redraw, reinterpret, re-proportion or re-colour it. If a sheet needs the key changed, stop and ask.
7. Restart and source files: use only the file Dimitri or his prompt names, by its path. Never pick one yourself, never merge two unless told to, never treat "history" or "alternatives" as approved.
8. A passing script is not approval. "0 problem(s)" from 02_pixelize.py means the mechanics pass; only Dimitri approves the look.
9. Work in a new revision folder for every version; never overwrite an earlier one. Record the source file and Dimitri's words for each version.
10. When the chat gets long (about 40 turns, or you lose track of a rule), write a handoff of at most 40 lines: done, approved files by path, what is next, open questions. No history of rejected versions beyond one line each.

B. SIZE AND PLACEMENT
11. File size, cell size and row order exactly as ASSET_MANIFEST (guide Part 6). Never resize a delivered file.
12. Vessels in a 32 px cell: 15-27 px wide, 19-28 px tall, never taller than 28, never touching the cell's sides. Spool: 19-22 tall; its body 18-20 wide, up to 27 wide with the dragging arm. Mini vessels in a 16 px cell: about 7 wide, 10 tall.
13. Units in a 16 px cell (Nauts, the Crafted, Photon): 6-12 px wide, 8-12 px tall.
14. Feet on the bottom row (row 31 of a 32 px cell), body centred on the cell's middle column. Airborne frames (fall, drop-in, hop) are the only exception: raise them by a planned lift and list the lift per frame.
15. Rows marked "mirror of": v1 is the exact silhouette mirror of v0, v3 of v2. The shade stays on the right and underside in every facing (re-shade the flipped pixels); nothing else changes.

C. COLOUR AND SHADING
16. Use only the asset's palette from 03_palettes.txt and the values below. No colour that is not listed; snap near-misses to the listed value.
    Spool: sage 93CCA1 lit, 60A871 shade, 4F8C5E deepest (under the hood brim, feet); thread CBE6D1; zipper teeth and coat stitches E4DEDA / 9A938E; hollow 1F3325; eyes E6D85A; tongue E58CC0; claws E4DEDA. One hue for the body ramp. Never olive, never grey-green, never purple.
    Yossele (his reshade is the baseline): clay 945743 / 79432F, collar 35403A, leaf 7BCF80 / 65AD69, eyes 99E550.
    Argo (approved Naut-based key): ochre C8A04B / 967533, teal 2B555C / 263C42, steel 9AA7A0 / 657772, eyes DCE9CB. No moss 638868.
    Uloe: glass DAE6FC / CBDBFC, glints F1EF72, steel 9BADB7 to 595652, arms FBF236, face 3F3F74, 222222 on cord and plug only.
17. Never 000000. Never magenta or purple (hue 270-320) on a friend; those belong to foes. Pearl F4EEF8 only for Pure Aura and the pearl in PearlOyster. Never paint the panel colours 2E4A62 or 22384B into an icon; the one exception is the dark roundel of RogueWaves_Crest, Crest32 and the menu button, which is 2E4A62 (batch 2E).
18. Flat fills: 2-3 tones per material, at most 16 colours in a vessel frame. No dithering, noise, gradients, anti-aliasing or painted texture. At most 13% of opaque pixels may be lone specks (a pixel with no same-colour neighbour); eye glints and stitches count toward it.
19. Light from the upper left. Left and top edges take the lit tone; the darkest tone only on the right edge, the underside and under overhangs. No outline and no dark rim.
20. Opaque pixels only (alpha 0 or 255). The one exception: a splash's inside fill at alpha 70.

D. DESIGN (what each vessel is; never drift from it)
21. Spool (Dimitri, 2026-11-08): a hunched hooded sage plush, leaning forward, head and hood thrust ahead of the body, short legs. Face: a dark 1F3325 opening under the hood with a zipper-like row of teeth along its top edge (alternating E4DEDA / 9A938E), two E6D85A eyes inside, and a 2-3 px E58CC0 tongue hanging from its front end. One long arm, attached at the back shoulder, drags behind it on the floor, palm facing up, ending in long E4DEDA claws (3 claws, 2-3 px each). The arm is always joined to the body (one connected piece), never severed, never a tail. No cotton patches, no pink anywhere but the tongue.
22. Argo: the approved Naut-based rig worker (round2/batch2a/argo_naut_key_v1). Pointed hardhat or hood, pale matching lenses, compact teal glove, open wrench. The approved fall sheet is frozen byte for byte.
23. Yossele: the see-through round hole, the stalk with its two-leaf sprout, the dark V collar, four limbs; a second sprout off the left shoulder.
24. Uloe: glass bulb with eyes as two 3 px arcs and a 4-5 px U mouth (at least half the glass clear), ribbed screw base, post, yellow arms, cord and plug. Legless; it hops.
25. Foes: grey stone bodies, one magenta crystal weapon, no face. Towers and buildings are 3D models built in code: concept pictures only. No numbers or text in item art. No gore. Never copy a base-game pixel into a pack asset.

E. DELIVERY
26. Every delivery: the game-size PNG under its exact manifest file name, its x8 preview, and one line: what it is, its pixel size, its colour count, its speck %, and what 02_pixelize.py printed.
27. Batches of 6-10 assets. Stop after each with: done / left / key frames only. "next" moves on.
28. Animation must move: idle 6 distinct frames (head 1 px down on 2-3, up on 4-5, one secondary motion); move a real step (body bob 2-3 px); attack anticipation, strike with a pale flash, recovery; land drops in, squashes, settles; fall is a mid-air loop (lowest pixel 3+ px up on 10 of 12 frames).
```
