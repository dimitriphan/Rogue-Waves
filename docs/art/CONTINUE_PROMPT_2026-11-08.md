# Continue prompt for ChatGPT, 2026-11-08

> **Replaced 2026-11-08 by `reset_2026-11-08/`** (Dimitri: "create a prompt that will have it reset all of its rulings"). Kept as history; ChatGPT no longer follows it.

Dimitri, 2026-11-08: "create a prompt to continue the asset process", after ChatGPT delivered Argo
(`Argo_Complete_Attack_Refined.zip`, brought in by its own thread) and stopped Spool for a restart from an earlier
version without a chosen source. The Argo review (`docs/reports/ART_REVIEW_ARGO_2026-11-08.md`, PR #179) took all 19
files and asked for five small edits (A1, A2, D1, D2, N1), which open the prompt. This prompt opens a fresh chat in the ChatGPT Project "Rogue Waves Art" on
GPT-6 Astra Medium only (Dimitri, 2026-11-08). Paste `CHATGPT_HARD_RULES.md`'s block first, then this one, and attach Spool's Art Bench key files
and the breakdown (below). A copy is in project files, `chatgpt/prompts/continue_artbench_2026-11-08.txt` (the older
`continue_2026-11-08.txt` there is the version 01 restart).

## The Spool key

**Dimitri, 2026-11-08 08:35Z:** "Use the current Spool sprites as the key art that is currently in the Art Bench." The
key is therefore the Art Bench's Spool (the code's `SpoolCharacter`, his own reshade in sage), approved and frozen:
`spool/artbench_key/Spool_key_idle_v0_f0.png`, 18 x 19 px, with the 13 Art Bench files as the starting frames of each
sheet (`SPOOL_SPRITE_BREAKDOWN.md`). It replaces the restart from ChatGPT's version 01 (his word of 03:20Z) and the
target key of PR #184. Attach the files of `spool/artbench_key/` (project files `chatgpt/prompts/spool_artbench_key/`)
and the breakdown with this prompt; `Spool_Restart_Handoff.zip` is no longer needed for Spool.

```text
CONTINUE ROGUE WAVES ART (2026-11-08)

Read the hard rules I pasted first; they win over everything below except my newest words. Use GPT-6 Astra Medium only. Attached: Spool_key_idle_v0_f0.png and Spool's 13 Art Bench files, SPOOL_SPRITE_BREAKDOWN.txt, Round1_feedback.txt.

WHERE WE ARE
- Argo: all 19 files of Argo_Complete_Attack_Refined.zip are taken into the game. Locked, do not change: the argo_naut_key_v1 design, the approved fall, the walk, the idles, the Sonar sheet, the white-line tab icon, the splash style, the Nauts. Your relit mirrored facings are fine. Only the five small edits of Step 1 remain.
- Spool: the key is the attached Art Bench key, approved (Step 2). Nothing of Spool's earlier history is used.
- After Spool: the rest of batch 2A, then 2D, 2E, 2F of Round1_feedback.txt.

STEP 1. ARGO: FIVE SMALL EDITS ON THE EXISTING SHEETS (no redraw; change only these pixels; one batch, then stop)
A1. ArgoAttack-Sheet: the lean reads as a crouch (the head drops 5 px but moves under 1 px toward the strike, and the coat shifts 1 px back on the swing). On frames 2-3, tip the hood and head 1-2 px toward the strike, coat leading. Keep the drop.
A2. ArgoAttack-Sheet: frame 4 holds the wrench forward at chest height, so the jump back to frame 1's overhead pose is 291 px. Bring the wrench halfway back up on frame 4.
D1. The open wrench (a U jaw on a straight stick) reads as a fork at 1x, most of all on Argosplash and Argoendstat1. Give it a solid head block with one longer jaw (an adjustable wrench), the same on every sheet that shows it. I approve this one exception to the locked key: the wrench head only, nothing else on the key or the fall changes.
D2. Readyup6argo: the face is two pale vertical bars and reads as a lamp at 12 px. Use two dot lenses, as on Argo_Icon.
N1 (optional). The six Naut sheets bob 1 px on the walk; make it 2 px to match Argo. Skip it if it breaks their 16 px cells.
Deliver the changed files only, each with its delivery line and which frames changed.

STEP 2. SPOOL KEY: ALREADY APPROVED (no drawing in this step)
My word, 2026-11-08: "Use the current Spool sprites as the key art that is currently in the Art Bench." The key is the attached Spool_key_idle_v0_f0.png (Spool-Sheet idle_v0 frame 0), 18 x 19 px, 12 colours, approved and frozen pixel for pixel. Nothing of spool_history, version 01 or your round 1 Spool is used. Read SPOOL_SPRITE_BREAKDOWN.txt, then reply in at most 5 lines: the key's measured size, colour count and the parts you see, and that you will not redraw it. Then stop.

STEP 3. SPOOL SHEETS (after my "next")
Start each sheet from its attached Art Bench file and keep its design, poses and timing; the key frame stays byte for byte. Fix only what the breakdown's section 7 names: re-shade the mirrored rows v1 and v3, check the move landing frames and bob, IntroR frame 11 equal to the key, and REDRAW SpoolFall-Sheet as a mid-air loop (the Art Bench row barely leaves the ground). Sheets: Spool-Sheet, SpoolAttack-Sheet, SpoolPatch-Sheet, SpoolIntroR-Sheet, SpoolFall-Sheet, SpoolIdlejustidle-Sheet, SSpoolSheet, Spool_Icon, Spoolendstat1, Spoolendstat2, Readyup6spool, Spooltabicon, Spoolsplash. Batches of 6-10; stop after each, listing per sheet which frames changed.

STEP 4. THE REST OF BATCH 2A
YosseleFall-Sheet, UloeFall-Sheet (mid-air loops), Yosseletabicon, Uloetabicon (white line art), Direction_Extension (orange cord E2944A / B8702F on a light reel, never the panel colour). Argo's three 2A items are already delivered.

STEP 5. THEN 2D, 2E, 2F of Round1_feedback.txt, in that order, one batch at a time.

Before you start, reply with at most 5 lines: the Argo files Step 1 changes, the Spool key file you opened and its measured size, and any question with your default. Then do Step 1, stop, and do Step 2 when I say next.
```
