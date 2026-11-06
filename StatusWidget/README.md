# Rogue Waves Status: the phone widget

A 4 × 6 Android home-screen widget that shows what the project is doing right now, acted out by the crew: Spool
stitches while a thread builds, Argo dives while the PC tests, Yossele reads a PR with its laser, Uloe lights a canvas
for art, and all four hold up a sign when it is Dimitri's turn. Asked for by Dimitri on 2026-11-06 (design canvas:
`https://claude.ai/artifact/QpTywytEzBc87ujEFMUioJ`).

This is a tool around the project, not part of the pack: nothing here is built into the mod.

## On the phone (Samsung S25+, Android 12 or later)

1. Open the **status-widget** release of this repository on the phone (GitHub, Releases), tap `RogueWavesStatus.apk`,
   and allow the install from the browser when Android asks. A newer build installs over the old one.
2. Make a token at github.com: Settings > Developer settings > Fine-grained tokens > Generate new token. Repository
   access: only `Rogue-Waves`. Permissions: Contents, read-only. Expiry: as long as you like.
3. Open **Rogue Waves Status**, paste the token, tap **Save and refresh**. The screen lists what it read.
4. Long-press the home screen > Widgets > Rogue Waves Status, and drag it out at 4 × 6.

The widget reads again every 15 minutes (Android's shortest background period) and when you tap it. The animation runs
the whole time. The token stays in the app's private storage and is only sent to `api.github.com`.

## What it shows

| State | Title | Scene | Lasts without a new write |
| --- | --- | --- | --- |
| `waiting` | YOUR TURN | the four hold up a "YOUR TURN" sign | until the thread changes it |
| `failed` | FAILED | Spool tangled in his own thread, in the rain | 2 hours |
| `testing` | TESTING | Argo dives on his rope, sonar rings out | 2 hours (a self-test runs long) |
| `building` | BUILDING | Spool stitches a patch | 30 minutes |
| `reviewing` | REVIEWING | Yossele's laser reads a scroll, ticking each line | 30 minutes |
| `art` | DRAWING | Uloe glows and sparks a wave onto a canvas | 30 minutes |
| `shipped` | SHIPPED | sail up, the boat runs with the waves | 30 minutes |
| none busy | IDLE | all four asleep on deck, Zs drifting up | |

The busiest thread takes the big scene, in the table's order (newest first within a state); its sub-agents show under
it, two lines at most. Up to three other busy threads fill "Also on deck", each with its helper count. The rules live in
`app/src/main/java/com/roguewaves/status/Selection.kt` and its test.

## When a thread reports

Every thread (and the coordinator, with `--id coordinator`) runs `StatusWidget/status/set_status.py`; it writes
`threads/<id>.json` on the `status` branch, one file per thread, so writers never clash. The branch never merges.
`--id` is the thread's root message id.

| When | Call |
| --- | --- |
| Work starts | `--id <id> --thread "<title>" --state <state> --step "<first step>"` |
| The checklist moves to a new step | `--id <id> --step "<step>"` (`--state` too when the kind of work changes) |
| A sub-agent gets a chunk / reports back | `--id <id> --helper "<task>\|Haiku"` / `--id <id> --helper-done "<task>"` |
| Waiting on Dimitri (a decision card, a question, a PR for his eye) | `--id <id> --state waiting --step "<what he needs to do>"` |
| A PR merged | `--id <id> --state shipped --step "PR #<n>: <title>"` |
| Stuck on a failure | `--id <id> --state failed --step "<what failed>"` |
| The thread resolves with nothing shipped | `--id <id> --clear` |

Kinds: writing code, docs or a PR is `building`; a PC round, self-test, cloud-check or play build is `testing`;
reading or answering a review is `reviewing`; sprites, models and concept art are `art`. A thread that starts a Remote
Control session reports `testing` for it (the PC session has no copy of this script). Each write also removes other
threads' files untouched for a day (a week for `waiting`). `--list` prints them all.

## Building

The cloud threads can't reach Google's Android SDK host, so `.github/workflows/status-widget.yml` builds on every push
that touches `StatusWidget/`: it runs the unit tests, builds the signed APK and replaces the **status-widget** release.
On a PC with Android Studio: open `StatusWidget/` and run `app`.

`app/sideload.keystore` (password `roguewaves`) signs every build so each one installs over the last; it is a key for a
sideloaded personal app, not a store key.

## The art

`art/scenes.py` draws the eight 12-frame scenes (128 × 80 px) from the vessels' real sprites, plus the list's small
heads and the app icon, into `app/src/main/res/` (frames at 1x, with nearest-neighbour wrappers so the pixels stay
square), and 3x GIFs into `art/preview/`. Re-run it after changing a scene or a source sprite:

    python3 -I StatusWidget/art/scenes.py

Fonts in `app/src/main/res/font/` (Exo 2, Barlow, Pixelify Sans) are from Google Fonts under the SIL Open Font
License 1.1, the Rogue Tide theme's own (`docs/themes/rogue-tide.md`).
