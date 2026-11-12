# Argo's five small edits (ChatGPT, 2026-11-12)

Dimitri, 2026-11-12, answering "which change asks to send ChatGPT (A1, A2, D1, D2, N1)": "Yes". Read as all five; he
can strike any before he pastes the prompt. The asks come from the review of ChatGPT's Argo set
(`docs/reports/ART_REVIEW_ARGO_2026-11-08.md` §3, PR #179; first sent as project files
`chatgpt/prompts/argo_findings_2026-11-08.txt`). The 19 files in the game (`TDS_Mod/ModSource/Argo/Art/`) are pixel for
pixel the Argo finals in `chatgpt/prompts/reset_2026-11-08/chat3_references_Yossele_Argo.zip` (checked 2026-11-12), so
those finals are the files to edit.

**How to use it.** In the ChatGPT Project "Rogue Waves Art", open a new chat of its own (not the Spool chats). Paste the
block below and attach the 19 Argo files plus `Argo_front_approved.png` and `Argo_back.png` from that zip's
`references/Argo/`. Paste-ready copy: project files `chatgpt/prompts/argo_edits_2026-11-12.txt`. Bring the edited files
back with "bring in my ChatGPT art" (`README.md`). Chat 3's reference zip keeps the unedited finals until then.

```text
ROGUE WAVES ART: ARGO, FIVE SMALL EDITS (Dimitri, 2026-11-12)

Attached: Argo's 19 final files and his two key frames (Argo_front_approved.png, Argo_back.png). They are approved and frozen; Dimitri lifts the freeze for the five edits below and nothing else. Every other pixel stays as it is: the design, the colours, the fall, the walk, the idles, the Sonar sheet, the white-line tab icon, the splash's style and the Nauts' look.

HOW WE WORK
1. Before each edit: say in 2-4 lines which files and frames it touches and what you will change, with numbers. Stop and wait for "go".
2. Each edit is one new version of only the files it names. Say which pixels changed (count and box) on each frame. Never redraw, re-roll or "improve" anything else.
3. Deliver: each changed file at game size under its exact file name, an x8 preview, a GIF per changed row at the game's speed, and a line per file: size, colours used, frames changed. Then stop and wait for "approved".
4. Same rules as the finals: every pixel fully opaque or fully clear (the splash's inside fill stays as it is), only the colours the file already uses, no outline, no anti-aliasing, light from the upper left, feet on the bottom row, every v1 row the exact pixel mirror of v0 and every v3 of v2.

THE EDITS, in this order
D1 (first, because the others draw the wrench too) THE WRENCH HEAD. The open wrench, a U jaw on a straight stick, reads as a fork or trident at 1x, most of all on Argosplash.png and Argoendstat1.png. Give it a solid head block with one longer jaw (an adjustable wrench), the same shape at each size on every file that shows it: the sheets, the icon, the splash, the end stats, the ready-up icon if it shows one. Only the wrench's head changes.
A1 THE ATTACK LEAN (ArgoAttack-Sheet.png). Today the head drops 5 px but moves less than 1 px toward the strike, and the coat shifts 1 px back on the swing, so it reads as a crouch. Its 4 frames, counted from 1, are wind-up, swing, follow-through, recovery. On frames 2 and 3 of each row, tip the hood and head 1-2 px toward the strike, coat leading. Keep the drop.
A2 THE ATTACK RECOVERY (ArgoAttack-Sheet.png). Frame 4 holds the wrench forward at chest height, so the loop back to frame 1's overhead wind-up jumps 291 pixels. On frame 4, bring the wrench halfway back up.
D2 THE READY-UP FACE (Readyup6argo.png, 12x12). The face is two pale vertical bars and reads as a lamp. Use two dot lenses, as on Argo_Icon.png.
N1 THE NAUTS' BOB (optional; the walk rows of Naut_*-Sheet.png and NautTune_*-Sheet.png, where a sheet has them). The Nauts bob 1 px when they walk; make it 2 px, as Argo's walk does. Change only the walk rows.

When all five are approved, list every changed file in one table and give them as one zip. Then stop.
```
