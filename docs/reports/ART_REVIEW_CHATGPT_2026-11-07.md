# Art review: ChatGPT's round 1 against the TDS style (2026-11-07)

Dimitri, 2026-11-07: "Do a review of the ChatGPT assets. Do they follow the art style and rules of TDS? Generate a
report for ChatGPT on what could be changed so that the assets look more official." Reviewed: the 151 pictures PR #173
brought in at `d8cddbd` (the sheets in `TDS_Mod/ModSource/<Part>/Art/`, the icons and VENDCINT in
`RogueWavesMode/Art/Temporary/game_size/`), the 9 it held back (from his `Rogue_Waves_Completed_Assets.zip`), and what
it named as missing. The note for ChatGPT is `docs/art/FEEDBACK_ROUND1_2026-11-07.md`. Nothing in the pack changed.

## 1. Findings first

**Verdict.** The set follows TDS's technical rules but does not look official yet. Every file passes the mechanical
checks (§3). Three things set it apart from the base game:

- **It barely moves.** Base TDS characters breathe, step, hop and land on every frame. ChatGPT's sheets mostly hold
  one pose.
- **Two vessels leave the house look.** Spool is a different creature from Dimitri's concept. Argo is dark and not in
  the big-head proportions.
- **A third of the icons paint parts in the panel's own colour.**

Severity: **P1** blocks looking official or breaks a written rule, **P2** is plainly visible, **P3** is polish.

### 1.1 Vessels' animation (all four)

| # | Sev | What | Files | Measured or seen | The base convention |
| --- | --- | --- | --- | --- | --- |
| A1 | P1 | Idle has no life | `*-Sheet.png` rows 0-3, `S*Sheet.png` rows 0-3, `*Idlejustidle-Sheet.png` | 2 distinct frames of 6; 0-21 px change a frame (Uloe 54); minis 0-3 px | 6 distinct frames, 26-385 px a frame (Fie, Buster, Smacky, Sno Sno, Boutman); minis 4-139 px; Dimitri's 11-05 Yossele reshade managed 6 distinct, 5-81 px |
| A2 | P1 | Move is a slide, not a step or a hop | `*-Sheet.png` rows 4-7 | 8-30 px a frame (Uloe 29-92), no head bob | 57-385 px a frame, 2-7 px bob; Sno Sno, legless like Uloe, hops |
| A3 | P1 | Land does not land | `*IntroR-Sheet.png` | 4-6 distinct of 12; the vessel stands in place, head top moves 0-4 px | Smacky's intro has 12 distinct frames, its top going 8, 1, 21, 9 (drop in, squash, unfold) |
| A4 | P1 | Falls collapse on the ground (held back; PR #173 confirmed) | the four `*Fall-Sheet.png` in his zip; the pack holds idle stand-ins | 6 distinct of 12, feet on the bottom row on every frame | Falling loops in mid-air: Smacky's fall has 12 distinct frames, its feet line moving 22-31 |
| A5 | P2 | Attacks and abilities near-static | `*Attack-Sheet.png`, `SpoolPatch`, `YosseleFertilize`, `UloePlug` | attacks 19-238 px a frame; Plug 0-13; Fertilize 0-64; Patch v2/v3 six identical frames (PR #173 confirmed), v0 0-11 px (new) | base attacks 224-784 px a frame: anticipation, strike, recovery |
| A6 | P2 | Yossele's v1 and v3 are a different character | `Yossele-Sheet.png`, attack and fertilize rows | v1: the sprout becomes a 2×2 block, the hole goes square, a third eye pixel appears; v3: the hole becomes an arch (PR #173 confirmed) | the facings are one character; mirrored rows are exact |

Yossele's attack is a limb poke. Its attack in the scope is a ranged laser from the mouth hole, so the hole should
glow and flash on the strike frame. Uloe's attack, with its bulb flash, is the best of the four.

### 1.2 Vessels' look

| # | Sev | Vessel | Finding | Change |
| --- | --- | --- | --- | --- |
| V1 | P1 | Spool | Departs from Dimitri's concept. His concept is about 18×19 px: a compact hooded plush with a narrow stitched slit for a face, small gloves, and its severed arm lying on the floor. ChatGPT drew a lanky creature 32×26 px (it fills the whole cell; vessels are 15-27 wide, kit `docs/18` §2) with a 6 px gaping jaw whose stitches read as teeth, white spiky glove fans (17% of the pixels in FFFFFF), and the arm as a tail fused to the body. The 16 px mini fills its cell too (PR #173 confirmed). | Back to his proportions; face slit 2-3 px; 3×3 gloves; the arm a separate object on the floor. Endstat2 differs from endstat1 only in the eyes, so it needs a slumped pose. |
| V2 | P1 | Argo | Not chibi and too dark. The head and hardhat are about 30% of the height. The front view is 61% dark teal (`263C42`, `2B555C`) and 18% ochre, in mottled patches. `263C42` runs down the lit left edge as a rim (dark-edge ratio 0.57 against the base's 0.03-0.10). It disappears on Deep Swell. The icon, endstats and mini are muddy at 1× (PR #173 confirmed). `Argosplash.png` is a single flat `263C42` silhouette of a gas-mask diver, not the sprite's hardhat worker. | Head half the height; the ochre coat the main mass (45% or more); teal only for the waders and gloves, shaded on the right; a light steel face guard; drop moss `638868`, which is not in the `docs/STYLE.md` row; redraw the splash. |
| V3 | P2 | Uloe | The face is lost. Its `3F3F74` blocks fill the lower two thirds of the bulb and read as a grimace at 1× and on the icon. The glass is mostly the shade `CBDBFC` (38 px) against 3 px of the lit `DAE6FC`. Two colours are off its palette (`AABBD0`, `FFFAAE`). `222222` is on the cord and plug only, as in his concept, so it is fine. | Two 1 px eye arcs and a thin U, half the glass clear; lit glass on the upper left. |
| V4 | P3 | Yossele | On model. The palette `945743` / `79432F` / `35403A` / `7BCF80` / `65AD69` / `99E550` matches his reshade, the baseline. Two departures: the concept's second sprout off the left shoulder became 1 px specks, and the legs are straighter and shorter than his splayed roots. | Restore the one leaf; splay the roots. |

**Refuted by eye.** The measurements suggested two lighting problems, and neither is real:

- Spool's "lit from the wrong side" reading (upper half minus lower half, −5 to −7) comes from the dark jaw in the
  upper half. By eye, the 60A871 shade sits on the right and bottom.
- Cordyfriend's −18.5 comes from the darker antlers on top. Its shade is also on the right and bottom.

### 1.3 Minions and units

| # | Sev | What | Change |
| --- | --- | --- | --- |
| M1 | P2 | Nauts: a grey helmet with a red or yellow plume reads as a knight, not a hardhat worker. Their 10-11 px height is inside the kit's 8-12 for 16 px units (kit `docs/18` §2). | An ochre `C8A04B` hardhat with a brim. Keep the size and the Port, Starboard and Bosun colours. |
| M2 | P2 | Cordyfriend: the idle is static (PR #173 confirmed). | A 1 px bob and an antler sway. |
| M3 | P3 | Thread Head: reads as a spool with a face (good). The dark brown flanges push its dark-edge ratio to 0.72; that is not an outline. | Lighten the flange tops (`826D60` over `64544A`). |
| M4 | P3 | The Crafted: no real outline by eye, but all are one sage mass. | Each type its own read (Seam a stitch line, Mangled a missing chunk). |
| M5 | ok | Photon: 6-8 px tall, reads. | Keep. |

### 1.4 UI pieces

| # | Sev | What | Change |
| --- | --- | --- | --- |
| U1 | P1 | Journal tab icons (held back; PR #173 confirmed): filled full-colour heads | A white 1 px line drawing with an open interior (kit `docs/03` §2) |
| U2 | P2 | Splashes: all four are opaque figures or 1-2 px lines. Yossele's is too thin to hold the panel, Spool's double line is busy, and Argo's is a flat silhouette. | The kit's splash style: translucent fill, 3 px accent outline, inset body-colour line, one shape per body part (kit `docs/03` §2) |
| U3 | P3 | Ready-ups: the white corner brackets are right; Argo's face inside is unreadable | Follows V2 |

### 1.5 Icons (90)

| # | Sev | What | Change |
| --- | --- | --- | --- |
| I1 | P2 | Panel colour inside the object: 32 icons paint opaque `2E4A62` or `22384B`, which vanish on the Deep Swell panel. Extension uses it for 62 of its 84 pixels, which is why it is unreadable (held back). Worst after it: Crest 51 px, Objective_Nest 27, Patternbook 26, VampireDentures 26, TarBucket 24, Deepcurrent 21, Pool 20, Wormtin 19. | Each object shaded in its own hue, its darkest tone at least 20 luminance points from `2E4A62` |
| I2 | P2 | Five miss their scope Look (PR #173 confirmed): Interest is a bar chart; Longline, navy lines; Regulars, three cups; Mend, a red cross on a card; Gallop, a boot and a wedge | Their Look lines in scope `11_Directions.txt`: a jar of coins with a sprout; a long spool of fishing line; a tally of a regular's tab; a tide pool with a green glow; a horseshoe with a wing |
| I3 | P2 | Weak at 1× (PR #173 confirmed): PearlOyster (no pearl), Hands (a white cloud), GuttingKnife (its handle is in the panel colour), Pool (no water). New: OilskinCoat reads as a leaf, SparePlank as a fish, Compass as a pocket watch. | Per the note |
| I4 | P2 | PureAura is grey (`B5B1B0`, `DEDEDE`), not Pearl (PR #173 confirmed) | `F4EEF8` with the `C9E4F2` / `F2D9E6` sheen (`docs/STYLE.md`) |
| I5 | P3 | Look-alikes: Brightbulb and Flashbulb; three life rings (CorkLifebuoy, Retry, Objective_Hands); three teal waves (Crest, RisingWater, Spring) | One distinguishing feature each |
| I6 | P3 | The crests use `DEDEDE`, not Foam `E9F1EE`; their roundel is `22384B`, off the palette; the menu button's crest is a different spiral from Crest32 | One crest drawing, Foam white |

These icons read well at 12×12 and must be kept: RubberDuck, Cost, Refit, SewingMachine, Kettle, Leash, Wound, Trowel,
Chain, Dandelion, Fairyring, Oddsocks, Stay, Vote, Objective_Hull, Objective_Nest, Nesting, Buttonjar and BrassSpyglass.
Objective_Nest reads but still has panel-colour pixels (I1).

### 1.6 VENDCINT and the menu button

- **VENDCINT (P3).** The vending machine with a screen face in Wet Sand reads well and is its own shape. Its v1 moves
  the shaded side to the left; the base Merchant keeps the shade on the right. It is about 17×25 px against the
  Merchant's roughly 10 px of figure; whether that scale suits the stall is for the PC round.
- **Menu button.** `C8C8C8` crest and » only, per `docs/STYLE.md`. Play's own tile isn't in the dump, so the two were
  not compared side by side here.

### 1.7 Held back and missing (PR #173's batch-2 list)

| Item | State |
| --- | --- |
| Four Falls | Confirmed (A4); the note's Batch 2A |
| Four journal tab icons | Confirmed (U1); Batch 2A |
| `Direction_Extension` | Confirmed, with its cause (I1); Batch 2A |
| `ArgoSonar-Sheet` | Not in the delivery; Batch 2A |
| `SpoolPatch` frozen v2/v3 | Confirmed; v0 nearly still too (A5); Batch 2B |
| PR #173's other notes | All confirmed above: Yossele v1/v3, the static idles, Cordyfriend, Spool's width and mini, Argo at 1× and its splash, PureAura, the five Looks and the four weak icons |

### 1.8 What to keep

- **Yossele:** its palette and look.
- **Spool:** its colours.
- **Uloe:** its silhouette, its attack flash and the Lit sheet.
- **Mechanics:** every rule in §3.
- **UI:** the endstats of Yossele, Uloe and Argo, and the ready-up brackets.
- **Nauts:** the crew colours.
- **VENDCINT:** its design.
- **Icons:** the 19 listed under §1.5.

## 2. What ChatGPT should do next

The note `docs/art/FEEDBACK_ROUND1_2026-11-07.md` (copy in project files, `chatgpt/prompts/feedback_round1_2026-11-07.txt`)
turns §1 into seven rules for every asset, a keep list, and six batches. It follows the rules style of `RECREATE_PROMPT.md`:

- GPT-6 Sol Medium;
- short bookkeeping turns;
- describe before rendering;
- never regenerate unprompted;
- batches of 6-10 assets, with key frames first for the redesigned vessels;
- a handoff when a chat grows long.

| Batch | Assets |
| --- | --- |
| 2A | What PR #173 held back or missed: the four Falls, the four tab icons, `Direction_Extension`, `ArgoSonar-Sheet` |
| 2B | Spool back to his concept, then its sheets |
| 2C | Argo brighter and chibi, then its sheets and the Nauts |
| 2D | Yossele's and Uloe's animation, Yossele's facings, Uloe's face |
| 2E | Icons |
| 2F | Units, two splashes, VENDCINT |

When the files come back, a thread brings them in ("bring in my ChatGPT art", `docs/art/README.md`).

## 3. How it was checked

**Mechanical (here).**

- `concept-tools/temp_art/art_check.py` reported 0 problems on every vessel and unit sheet at its manifest cell with
  `--baseline`, on the 92 icon-folder files at cell 12 and on the held-back files. Its only lines were:
  - 3 sparse icons, a note to read by eye;
  - files checked at the wrong cell size (Crest32, Vendcint, Spoolhead);
  - the Nauts' 1 px walk bob on frames 2 and 5, which is on purpose.
- `concept-tools/chatgpt/pixelize.py --check` against `03_palettes.txt` was clean on Yossele and Spool. On Uloe it
  flagged 2 colours; Argo and the Nauts are off the old palette on purpose (the redesign).
- No sheet has a semi-transparent pixel, pure black or the foes' magenta, and every frame's feet sit on the bottom row,
  centred.

**Measured (a scratch script, not committed).** For each frame and row:

- distinct frames per row and pixels changed between frames;
- height and width;
- colours per sheet;
- a dark-edge ratio: the share of silhouette edge pixels in the frame's darkest fifth of colours;
- a light-side number: the upper-left half's mean luminance minus the lower-right half's.

The same numbers were taken on the base game's vessels and minions (Fie, Buster, Smacky, Sno Sno, Boutman, Kippy,
Merchant, the minis, Dog, Smacky's builder) and on Dimitri's 11-05 Yossele reshade from git history.

| Sheet | Colours | Height px | Idle: distinct / px changed | Dark-edge |
| --- | --- | --- | --- | --- |
| Base vessels (5) | 10-21 | 18-28 | 6 / 26-385 | 0.07-0.10 |
| His Yossele reshade | 7 | 20-22 | 6 / 4-81 | 0.02 |
| ChatGPT Yossele | 6 | 21 | 2 / 0-2 | 0.14 |
| ChatGPT Spool | 10 | 26 (32 wide) | 2 / 0-21 | 0.28 |
| ChatGPT Uloe | 14 | 23-24 | 2 / 0-54 | 0.38 |
| ChatGPT Argo | 10 | 25-26 | 2 / 0-19 | 0.57 |

**By eye (an Opus 5.5 sub-agent).** It read contact sheets at 1×, 3×, 4× and 6× on Deep Swell, a light ground and grass:

- every vessel's rows beside the nearest base vessels and his reshade;
- the action sheets, falls, minis, endstats, splashes and minions;
- all 90 icons at 1× and 6×;
- VENDCINT beside the Merchant;
- `his_sprites.png`.

It judged them against `docs/STYLE.md`, kit `docs/18` and `docs/03`, `docs/concepts/YOSSELE_SPOOL_ULOE.md` and
`docs/art/CHATGPT_BRIEF.md`. The base-game sprites (`/mnt/project-files/research/art_dump_2026-10-15/`,
`/mnt/project-files/chatgpt/2026-11-06/`) and the contact sheets stayed out of the repository.

**Not judged here** (for the next PC round):

- frame timing, which the animation table's speed sets;
- the vessels on the boat and in map light;
- Spool's sage beside Hardened's green;
- VENDCINT's scale at the stall.

Docs only: no compile, nothing run on the PC.
