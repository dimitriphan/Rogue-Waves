# Continue prompt for ChatGPT, 2026-11-08

Dimitri, 2026-11-08: "create a prompt to continue the asset process", after ChatGPT delivered Argo
(`Argo_Complete_Attack_Refined.zip`, brought in by its own thread) and stopped Spool for a restart from an earlier
version without a chosen source. The Argo review (`docs/reports/ART_REVIEW_ARGO_2026-11-08.md`, PR #179) took all 19
files and asked for five small edits (A1, A2, D1, D2, N1), which open the prompt. This prompt opens a fresh chat in the ChatGPT Project "Rogue Waves Art" on
GPT-6 Sol Medium. Paste `CHATGPT_HARD_RULES.md`'s block first, then this one, and attach `Spool_Restart_Handoff.zip`
(ChatGPT's own package; it holds `spool_history/` and the references). A copy is in project files,
`chatgpt/prompts/continue_2026-11-08.txt`.

## The Spool restart source, and why

Claude picked **version 12, "Compact TDS key"** (`spool_history/12_key.png`, ChatGPT's
`round2/spool_tds_key_v1/Spool_idle_v0_frame0.png`) as the base, with **the hood and face opening of version 13**
grafted on. Measured from the package (opaque pixels, colours, lone-speck share, pieces):

| Version | Size | Sage share | Specks | Arm | Gloves | Fits STYLE.md and the 2026-11-08 note |
| --- | --- | --- | --- | --- | --- | --- |
| 01 original hunched | 27x22 | 67% | 11% | joined, palm up | no | arm joined, too wide, cotton on the body |
| 02-08 options and C line | 19-27 x 18-26 | 82-90% | 3-7% | joined or tucked | no | no gloves, arm on the body; all rejected |
| 09-11 Folded Cowl | 23-27 x 26-30 | 0% (olive) | 14-22% | tentacle | no | wrong colour, too big, painterly |
| **12 compact TDS** | **20x22** | **69%** | **11%** | **separate, left, cotton** | **3x3 DDDDDD** | **all size, colour and anatomy rules; face slit reads as teeth, hood plain** |
| 13-15 detailed TDS | 27x28 | 86-87% | 9-12% | joined, curled up the side | partly | too big (28 tall is the cap, 19-22 asked), arm joined |

Version 12 is the only one that already meets every hard rule (`docs/STYLE.md` Spool row, `FEEDBACK_SPOOL_KEY_2026-11-08.md`).
Dimitri turned it down for "loss of detail" and liked version 13's hood and eyes, so the prompt keeps 12's body and
adds back 13's hood and a few readable folds, inside the 13% speck cap.

**The arm.** ChatGPT's handoff says Dimitri told it to keep an ATTACHED curled arm after version 12. `docs/STYLE.md`
and his concept (`his_sprites.png`) have the severed arm on the floor, and the merged note of 2026-11-08 repeats it.
This prompt follows STYLE.md; if he wants the attached arm, the source becomes version 15 shrunk to 22 px tall and
STYLE.md's Spool row changes with it.

```text
CONTINUE ROGUE WAVES ART (2026-11-08)

Read the hard rules I pasted first; they win over everything below except my newest words. Use GPT-6 Sol Medium. Attached: Spool_Restart_Handoff.zip (your own package: spool_history/, references/his_sprites.png, Round1_feedback.txt).

WHERE WE ARE
- Argo: all 19 files of Argo_Complete_Attack_Refined.zip are taken into the game. Locked, do not change: the argo_naut_key_v1 design, the approved fall, the walk, the idles, the Sonar sheet, the white-line tab icon, the splash style, the Nauts. Your relit mirrored facings are fine. Only the five small edits of Step 1 remain.
- Spool: restarting from the source in Step 2. Nothing else of Spool's history is approved.
- After Spool: the rest of batch 2A, then 2D, 2E, 2F of Round1_feedback.txt.

STEP 1. ARGO: FIVE SMALL EDITS ON THE EXISTING SHEETS (no redraw; change only these pixels; one batch, then stop)
A1. ArgoAttack-Sheet: the lean reads as a crouch (the head drops 5 px but moves under 1 px toward the strike, and the coat shifts 1 px back on the swing). On frames 2-3, tip the hood and head 1-2 px toward the strike, coat leading. Keep the drop.
A2. ArgoAttack-Sheet: frame 4 holds the wrench forward at chest height, so the jump back to frame 1's overhead pose is 291 px. Bring the wrench halfway back up on frame 4.
D1. The open wrench (a U jaw on a straight stick) reads as a fork at 1x, most of all on Argosplash and Argoendstat1. Give it a solid head block with one longer jaw (an adjustable wrench), the same on every sheet that shows it. I approve this one exception to the locked key: the wrench head only, nothing else on the key or the fall changes.
D2. Readyup6argo: the face is two pale vertical bars and reads as a lamp at 12 px. Use two dot lenses, as on Argo_Icon.
N1 (optional). The six Naut sheets bob 1 px on the walk; make it 2 px to match Argo. Skip it if it breaks their 16 px cells.
Deliver the changed files only, each with its delivery line and which frames changed.

STEP 2. SPOOL KEY FRAME (idle_v0, frame 0), FROM THIS SOURCE ONLY
Source: spool_history/12_key.png (= round2/spool_tds_key_v1/Spool_idle_v0_frame0.png), 20 px wide, 22 tall.
Keep from 12, unchanged: its size (18-20 wide, 19-22 tall; never bigger), its exact palette (93CCA1 / 60A871 / 4F8C5E, E4DEDA / 9A938E, 1F3325, E6D85A, E58CC0, DDDDDD / CFCEDF), the two 3x3 glove hands, the short legs, feet on row 31, and the severed arm lying on the floor about 2 px left of the body with its E4DEDA cotton tuft at the wound, separate from the body.
Change, using spool_history/13_key.png as the reference for the hood and eyes only (do not take 13's size, body or arm):
1. Hood: replace 12's plain cone with 13's folded hood: the soft point, the brim that droops over the left side, and 4F8C5E only in the shadow under the brim and on the right. Keep the hood inside 12's width.
2. Face: 12's opening is a 4-row band with a full ring of stitches; it reads as a mouth with teeth. Make it 13's opening scaled down: hollow 1F3325 2 px tall, about 8-10 px wide, set under the brim, with stitches only along its top and bottom edges, alternating E4DEDA / 9A938E with a gap between each stitch. Eyes: two 1x2 E6D85A, like 13's.
3. Tongue: 2-3 px of E58CC0 from the slit's right end.
4. Detail he missed in 12: one fold line in 60A871 down the body's right side and two spaced E4DEDA / 9A938E stitches on the coat (as in 15). Nothing more: the frame must stay at or under 13% lone specks and 10-16 colours.
5. Light from the upper left; no dark pixels on the left or top edge.
Deliver: 2-4 lines of description first, then the 32x32 PNG, the x8 preview, and the delivery line (size, colours, speck %, 02_pixelize.py result). Then stop and wait for my OK. No sheets before it.

STEP 3. SPOOL SHEETS (only after my OK on the key)
Batch 2B of Round1_feedback.txt, as edits of the approved key: Spool-Sheet, SpoolAttack-Sheet, SpoolPatch-Sheet (a needle-and-thread pass in three readable poses, every facing moving), SpoolIntroR-Sheet, SpoolIdlejustidle-Sheet, SSpoolSheet (about 7x10), Spool_Icon, Spoolendstat1, Spoolendstat2 (slumped, defeated), Readyup6spool, Spoolsplash (rule 6 of the round 1 note). Add Spool's two items from batch 2A here: SpoolFall-Sheet (mid-air loop) and Spooltabicon (white 1 px line art of the head). Batches of 6-10; stop after each.

STEP 4. THE REST OF BATCH 2A
YosseleFall-Sheet, UloeFall-Sheet (mid-air loops), Yosseletabicon, Uloetabicon (white line art), Direction_Extension (orange cord E2944A / B8702F on a light reel, never the panel colour). Argo's three 2A items are already delivered.

STEP 5. THEN 2D, 2E, 2F of Round1_feedback.txt, in that order, one batch at a time.

Before you start, reply with at most 5 lines: the Argo files Step 1 changes, the Spool source file you opened and its measured size, and any question with your default. Then do Step 1, stop, and do Step 2 when I say next.
```
