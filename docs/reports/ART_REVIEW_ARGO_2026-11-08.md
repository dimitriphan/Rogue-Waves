# ChatGPT's Argo delivery: checks and eye review (2026-11-08)

Dimitri's upload: `Argo_Complete_Attack_Refined.zip` (19 game-size PNGs, previews, ChatGPT's checks) and its handoff
`HANDOFF_SPOOL_RESTART_AND_ARGO.txt`. Design key: ChatGPT's `argo_naut_key_v1`, which the handoff says he picked; the
fall is the one he approved there. Only `ArgoAttack-Sheet` changed in this last revision (more lean and follow-through).
Brought in by the flow in `docs/art/README.md`. Read beside `ART_REVIEW_CHATGPT_2026-11-07.md` (round 1).

**Verdict:** all 19 files are in. None held back. Every mechanical check passes. Round 1's two Argo problems are fixed:
the head is now about half the height, and the idles move. Three change requests remain, none blocking.

## 1. Mechanical checks

| Check | Result |
| --- | --- |
| Size and layout against `docs/art/ASSET_MANIFEST.md` and against the code's own sheets (the size `ArgoBuilder.FileArt` needs) | 19 / 19 equal |
| `pixelize.py --check` (sprites `--anchor bottom`; icon, endstats, tab, ready-up, Fall and Land `--anchor center`) | 0 problems on 18 |
| `Argosplash.png` | only the expected line: 2822 half-transparent pixels, all `E6C16B` at alpha 70 |
| `art_check.py` on all 19, and `--baseline` on main, attack, Sonar, Land and Fall | only the splash's expected line |
| Colours | 7: `C8A04B` `967533` `263C42` `2B555C` `657772` `9AA7A0` `DCE9CB`. They are off the old `[argo]` palette on purpose (the redesign). The steel light `9AA7A0` and the lenses `DCE9CB` are not in `docs/STYLE.md`'s Argo row yet |
| Lifts | Fall: 4,5,7,7,6,6,4,4,6,7,6,4, as the handoff says. Land: 5,4,2, then the impact squash to 16 px tall and an overshoot to 27 |
| `ase_sheets.py pack` → `unpack` | 19 / 19 round-trip pixel-exact |

## 2. The "relit mirror" claim

The handoff says mirrored facings keep the silhouette but have their RGB relit. This is true. In every v0/v1 and v2/v3
pair of the main, attack, Sonar, mini and Naut sheets, the silhouette differs from the mirror in 0 pixels. The colour
differs in 20-31% of opaque pixels:

- main front: 436 of 2234
- main back: 688 of 2234
- attack: 278-446 of 1379
- Nauts: 50-96 of about 340

Kit `docs/18` rule 7 says vessels do the same thing: Boutman's v1 differs from his v0 mirror in 112 of 280 pixels
because he keeps his shaded side. So the light stays upper-left in both facings, as the game's vessels do. Taken as is.

## 3. Eye review (against Boutman and Buster, `chatgpt/2026-11-06/05_game_sheets.zip`, private)

| # | Prio | What | Measured | Ask |
| --- | --- | --- | --- | --- |
| A1 | P2 | **Attack lean reads as a crouch.** There are four clear beats: wind-up overhead, swing with a pale arc, follow-through with the wrench at the feet, recovery. But the body only drops. | Head top 5→8→10→7 px; head x 15.8→15.1 (under 1 px); the coat's mass moves 1 px *back* on the swing | On frames 2 and 3, tip the hood and head 1-2 px toward the strike, with the coat leading. Keep the drop. Boutman's cast keeps the body upright, so Argo already shows more body than the base; this asks for direction, not size |
| A2 | P3 | **Recovery holds the wrench forward at chest height.** The attack loops back to the overhead wind-up with a 291 px jump | f4→f1 291 px change | Bring the wrench halfway back up on frame 4 |
| W1 | — | **Walk fits the base.** The legs alternate, the head bobs 2 px, and the frames change 183-310 px | Base: 45-350 px and a 2-7 px bob (kit `docs/18` rule 8). Boutman's walk bobs about the same | None. The wrench hand barely swings; one pixel of arm swing is optional |
| I1 | — | **Idle is fixed** (round 1: nearly still) | 20-113 px per frame, head 0-1 px (base 2-220, 0-3) | None |
| D1 | P2 | **The open wrench reads as a fork or trident** at 1×, most of all on the splash and the win endstat: a U jaw on a straight stick | — | Give the head a solid block with one longer jaw (an adjustable wrench) or a pipe-wrench hook |
| D2 | P3 | **Ready-up:** the face is two pale vertical bars under the hat. At 12 px it reads as a lamp | — | Two dot lenses, as on the icon |
| D3 | — | **Body:** head about half the height; ochre 47% of the front view (round 1 asked 45% or more), teal 42% | idle v0 frame 0 | None. The dark face is the picked design |
| T1 | — | **Tab icon:** a one-colour white line drawing of the hood | — | Round 1's ask is met; it replaces the code's icon |
| S1 | — | **Sonar** now exists: the pale ring grows over six frames, in all four facings | — | Replaces the code's old-look sheet |
| N1 | P3 | **Nauts:** hardhats and the crew colours. The walk bobs 1 px at 16 px, and the frames change 11-48 px | — | Fine for units. A 2 px bob would match Argo |

## 4. What this changes

- **Design drift from scope 09's Look line.** Scope 09 says: "ochre hardhat ... strands of kelp, a heavy two-handed
  pipe wrench". The picked key has a pointed hood-hat, no kelp, and a one-handed open wrench.
- **Still open:** scope 09's Look line and `docs/STYLE.md`'s Argo row wait for his word. Its Sprites line is updated
  here (the Sonar sheet is drawn now).
- **Code:** no change. `ArgoBuilder.FileArt` already takes every file by name and size, including the two new ones,
  `ArgoSonar-Sheet` and `Argotabicon`.
- **Compile:** compiled here (Argo release, `ARGO_SELFTEST`, `ARGO_SELFTEST,ARGO_SHOTS`: 0 errors each; the editor
  check is clean).
- **Not tested:** nothing ran on the PC; the art waits for his eye in the next PC round.
