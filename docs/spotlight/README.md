# Rogue Waves Spotlight

A one-page showcase of every new feature in the pack: the mode's loop, the ten maps' difficulty, the sea chart and its
weather, the chart view with its Cove and Wreck stops (`docs/mode/CHART_NAVIGATION.md`), the breaks (VENDCINT, Pure
Aura, objectives, Fund Transfer, retries), the four vessels, the 55 Directions, the Trinkets and Cardinals, the foe
modifiers and online play. Since 2026-11-06 (Dimitri: "as if it were a finished product ready to ship") it reads as
the finished pack: no status chips, no "not built" list, no release plan, no mention of temporary art. It is styled in
the Rogue Tide theme (`docs/themes/rogue-tide.md`), a single dark look. Published as an artifact:
https://claude.ai/artifact/MCPDgVngHmnZcVHeZwxNSr (private to Dimitri).

| File | What |
| --- | --- |
| `spotlight.src.html` | the page, with `{{img:<key>}}` where a picture goes |
| `build.py` | inlines the pictures and writes `spotlight.html`: `python3 docs/spotlight/build.py` from the repository root |
| `spotlight.html` | the built page, the file the artifact is published from |

Pictures come only from the repository: the mode's icons and VENDCINT sheet, Spool's sage green sprite, the Uloe
concept frame, Yossele's final idle sheet, one tower model preview per vessel, the chart view mockup
(`docs/concepts/art/chart_look_vote.png`) and one PC screenshot of Argo. No base game sprite and nothing from the
project files.

**Keeping it current.** Content as of 2026-11-06, base `262f2c6`. The Directions and the new Trinkets and Cardinals
were drawn from scope `11` and `12` with a one-off script, so a change there is edited into `spotlight.src.html` by
hand. After an edit, run `build.py` and republish `spotlight.html` to the same artifact URL. Build state lives in
`docs/PLAN.md` Part C and the reports, never on this page.
