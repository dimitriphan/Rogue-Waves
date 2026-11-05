# Dimitri's open decisions, third page (2026-11-05)

One design call, from the weather and lighting PC round (`LIGHTING_2026-11-05.md` §5, §6, §6.4; PR #121). The first two
pages (`OPEN_DECISIONS_2026-11-05.md`, `OPEN_DECISIONS_2026-11-05b.md`) are answered and not reopened here. Read from
the base `1f33941`. Nothing was changed in code to write this page, and nothing ships in play until the answer below is
built by a code thread and seen on the PC.

**How to answer:** by letter ("1 a", "1 b"...), or "as recommended". `~a~` is the default Claude takes if you say so.

## A. Game design

1. **Does a lighting look ship in Rogue Waves, and where?** The pictures are the contact sheet
   `contact_sheet_light_round1.png` in the Claude Project's files under `reports/lighting_2026-11-05/` (not in the
   repository: it shows the base game's map).

   What the PC showed (Light_round1, 23 pass, 0 fail):
   - Depth fog is real fog: what is far fades, towers, vessels and crew included, not just the ground.
   - The UI never fogs: the HUD, tile brackets and markers stay sharp. Good for reading, but a marker or a foe's health
     bar would show through a thick fog and give away what it hides. No foe was on screen, so foes in fog are unseen.
   - A dark look leaves the sky bright: "night" reads as dusk. Only a borrowed stock grade changes the sky, and the
     strong grades (`Map_Final` magenta, `Map_Thehunt` acid green, `Macro_World` blood red) are a lot.
   - Point lights are drawn (a lantern pool on a dark deck works). A 4 s fade is smooth.
   - No measurable cost: 7.17 ms a frame on the map's own light, 7.07 ms with a look held, 7.1 ms with a grade.
   - Already built and compiled (kit 1.13 `ModKitLight`, PR #118): the look, the fade, the borrowed grade, the lamp.
     The mode calls it every frame but sets no look in a release build.

   The options:
   - **a. A look per weather.** Each weather reads at a glance, light enough to keep play readable: **Rising Water**
     under light mist, **Quickening Current** under storm grey, **Nesting Ground** at dusk. A map with no weather keeps
     its own light. Fades in over 4 s at map start, on every machine (lighting does not sync; each runs it from
     `RWRejoin.MapWeather()`). Cost: a three-row table and one call, plus a self-test check. No new gameplay.
   - **b. A Fog Bank weather** (`docs/mode/MECHANIC_IDEAS.md` §2): heavy sea fog as a weather of its own, with its own
     pay like the other three. Needs a PC look at foes in fog first, and a call on whether markers and health bars
     should hide in it (they show through today).
   - **c. A mood ramp:** later maps darker and foggier, map 1 as the game draws it, map 10 at dusk with haze. Ties the
     look to the run, not the weather. A true night would need a borrowed grade for the sky.
   - **d. None.** The weathers stay as they look now. Nothing to build.

   **Rec: `~a~`.** It is the cheapest way to make the three weathers read at a glance, every picture it uses stayed
   readable on the PC, and the code is already there. (b) stays in `MECHANIC_IDEAS` §2 for later, once a PC round shows
   foes in fog; (c) can be added on top of (a) any time. Blocks: nothing; Release 1 (Yossele alone) carries no mode.

## The answer

_Waiting for Dimitri._
