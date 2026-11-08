# Continue prompt for ChatGPT, 2026-11-08

Dimitri, 2026-11-08: "create a prompt to continue the asset process", after ChatGPT delivered Argo
(`Argo_Complete_Attack_Refined.zip`, brought in by its own thread) and stopped Spool for a restart from an earlier
version without a chosen source. The Argo review (`docs/reports/ART_REVIEW_ARGO_2026-11-08.md`, PR #179) took all 19
files and asked for five small edits (A1, A2, D1, D2, N1), which open the prompt. This prompt opens a fresh chat in the ChatGPT Project "Rogue Waves Art" on
GPT-6 Sol Medium. Paste `CHATGPT_HARD_RULES.md`'s block first, then this one, and attach `Spool_Restart_Handoff.zip`
(ChatGPT's own package; it holds `spool_history/` and the references). A copy is in project files,
`chatgpt/prompts/continue_2026-11-08.txt`.

## The Spool restart source

**Dimitri's pick, 2026-11-08 03:20Z:** "Use the original idea that the arm is attached, but dragging behind him palm
facing up with long claws. Keep the zipper like teeth and hunched posture." The source is therefore **version 01, the
original hunched key** (`spool_history/01_key.png`, ChatGPT's `round2/batch2a/spool_key_lazy_v4/Spool_idle_v0_frame0.png`),
27 x 22 px. It replaces the severed arm (`docs/STYLE.md` and scope `08` are updated with it) and Claude's first pick
(version 12 with 13's hood). Measured on 01: 9 colours, 11% lone specks, 3 separate pieces (arm and feet not joined),
`60A871` on 2 pixels with `4F8C5E` doing all the shading, cream patches and pink specks on the body. The prompt keeps
the pose, hood, teeth, eyes and tongue and fixes exactly those faults.

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
Source: spool_history/01_key.png (= round2/batch2a/spool_key_lazy_v4/Spool_idle_v0_frame0.png), the original hunched key, 27 px wide, 22 tall. My design, 2026-11-08: the arm is attached and drags behind him, palm up, with long claws; keep the zipper-like teeth and the hunched posture.
Keep from 01, unchanged: the hunched pose with the hood and head thrust forward to the right, the hood's point, the dark 1F3325 opening with its zipper-like row of alternating E4DEDA / 9A938E teeth along the top, the two E6D85A eyes, the E58CC0 tongue hanging from the front end, the short legs, feet on row 31, the overall 27x22 size and its 2 px margins.
Change only this:
1. The arm: one long arm attached at the back shoulder, dragging behind him (to the left in v0) down to the floor, palm facing up, ending in 3 long E4DEDA claws of 2-3 px each that curl upward. It is joined to the body all the way: the whole sprite is one connected piece (01 is now 3 separate pieces).
2. Clean the body: remove the cream E4DEDA patches on the chest and lower front and the three lone pink pixels on the back. Ivory only on the teeth and the claws; pink only on the tongue.
3. Shading: 01 shades almost everything in the deepest 4F8C5E and uses 60A871 on 2 pixels. Make 60A871 the main shade band (right side and underside of each form) and keep 4F8C5E only under the hood's overhang, under the arm and on the feet. Lit left and top edges in 93CCA1.
4. Flat fills: at most 13% lone specks and at most 16 colours (teeth and eyes count).
Deliver: 2-4 lines of description first, then the 32x32 PNG, the x8 preview, and the delivery line (size, colours, speck %, pieces, 02_pixelize.py result). Then stop and wait for my OK. No sheets before it.
In every later frame the arm stays attached; on the move rows it drags along the floor behind him, the claws trailing.

STEP 3. SPOOL SHEETS (only after my OK on the key)
Batch 2B of Round1_feedback.txt, as edits of the approved key: Spool-Sheet, SpoolAttack-Sheet, SpoolPatch-Sheet (a needle-and-thread pass in three readable poses, every facing moving), SpoolIntroR-Sheet, SpoolIdlejustidle-Sheet, SSpoolSheet (about 7x10), Spool_Icon, Spoolendstat1, Spoolendstat2 (slumped, defeated), Readyup6spool, Spoolsplash (rule 6 of the round 1 note). Add Spool's two items from batch 2A here: SpoolFall-Sheet (mid-air loop, the arm and claws flailing) and Spooltabicon (white 1 px line art of the head). Batches of 6-10; stop after each.

STEP 4. THE REST OF BATCH 2A
YosseleFall-Sheet, UloeFall-Sheet (mid-air loops), Yosseletabicon, Uloetabicon (white line art), Direction_Extension (orange cord E2944A / B8702F on a light reel, never the panel colour). Argo's three 2A items are already delivered.

STEP 5. THEN 2D, 2E, 2F of Round1_feedback.txt, in that order, one batch at a time.

Before you start, reply with at most 5 lines: the Argo files Step 1 changes, the Spool source file you opened and its measured size, and any question with your default. Then do Step 1, stop, and do Step 2 when I say next.
```
