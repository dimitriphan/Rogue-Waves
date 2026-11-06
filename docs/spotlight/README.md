# Rogue Waves Spotlight

A one-page showcase of every new feature in the pack: the mode's loop, the ten maps' difficulty, the sea chart and its
weather, the breaks (VENDCINT, Pure Aura, objectives, Fund Transfer, retries), the four vessels, the 55 Directions, the
Trinkets and Cardinals (with the base game split of PR #130 and his answers after it), the foe modifiers, online play and fixes, what the scope
calls a feature that isn't built, and the release order. Each feature carries a chip: played on the PC, built and not
yet run, or not built. Published as an artifact: https://claude.ai/artifact/MCPDgVngHmnZcVHeZwxNSr (private to Dimitri).

| File | What |
| --- | --- |
| `spotlight.src.html` | the page, with `{{img:<key>}}` where a picture goes |
| `build.py` | inlines the pictures and writes `spotlight.html`: `python3 docs/spotlight/build.py` from the repository root |
| `spotlight.html` | the built page, the file the artifact is published from |

Pictures come only from the repository: the mode's temporary icons and VENDCINT sheet, Spool's sage green sprite, the
Uloe and sea chart concept art, Yossele's final idle sheet, one tower model preview per vessel and one PC screenshot of
Argo. No base game sprite and nothing from the project files.

**Keeping it current.** Its numbers and statuses are as of 2026-11-05, base `3ee6305` (the Trinket split refreshed with his answers the same night). The Directions and the new
Trinkets and Cardinals were drawn from scope `11` and `12` with a one-off script, so a change there is edited into
`spotlight.src.html` by hand. After an edit, run `build.py` and republish `spotlight.html` to the same artifact URL. Each
PC round that runs a "built, not yet run" item turns its chip to "played on the PC".
