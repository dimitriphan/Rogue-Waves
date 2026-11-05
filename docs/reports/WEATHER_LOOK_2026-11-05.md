# A look per weather, built (2026-11-05)

Dimitri picked **a look per weather** at 17:45Z (`OPEN_DECISIONS_2026-11-05c.md`, "The answer"): Rising Water under
light mist, Quickening Current under storm grey, Nesting Ground at dusk, a calm map on its own light. This is the build
of that answer in the mode. **Compiled here only; nothing ran on the PC** (it was busy with round 4). The look gets a PC
round of its own afterwards (§4).

## 1. What was built

All in `TDS_Mod/ModSource/RogueWavesMode/Code/07_Weather.cs`, "the look". No kit change: kit 1.13's `ModKitLight`
already had the look, the fade, the hold and the let-go.

| Weather | Look | Fog (x the camera's distance to the ground) | Ambient sky / horizon / ground | Sun |
| --- | --- | --- | --- | --- |
| Rising Water | light mist | `C9D6D8`, 0.8 to 2.2 | the map's own | the map's own |
| Quickening Current | storm grey | `4A5258`, 0.7 to 1.8 | `5A646C` / `4B545B` / `30363B` | `B8C0C8` x0.45, low, toward (0.3, -0.5, 0.8) |
| Nesting Ground | dusk | `E09A6B`, 1 to 3 | `6A5A8A` / `B07A6A` / `4A3A4A` | `FF9A5A` x0.9, low, toward (-0.8, -0.25, 0.3) |
| Calm | none: the map's own light | | | |

- **The numbers are the ones the PC pictured** (`LIGHTING_2026-11-05.md` §6.3, the gaps build's L1 light mist, storm
  and dusk), unchanged. The gaps build's L1 now takes these three from the table (`RWWeather.LookFor`), so its pictures
  are always what ships.
- **Relative, per map:** the fog's start and end are multiples of the camera's distance to the ground under the
  screen's centre, so a zoom keeps the same haze, and every map keeps its own scale. The light mist keeps the map's own
  ambient and sun; storm and dusk bring their own. The ground is the map's lowest tile, read from the `Surface` entities
  every machine has (`RWUtil.GroundLow`: the game fills `Entityclienttags` on every machine from each being's own tags,
  as it fills the host's `Entitiesserver`, read from its IL; a tile's synced `Position` is its place), the host's grid if
  there are none, and logged. A weathered map with no lighting group of its own gets no look (a warning in the log).
- **When:** on each machine, once a map, when that machine's intro is over (`Game_Client_Init.Gametimescale`, the
  camera's fly-in done), from the map's own lighting into the look over 4 s. So the map flies in under its own light
  and the weather rolls in as play starts.
- **Every machine for itself:** the weather comes from the synced names (`RWRejoin.MapWeather()`), so the host, a client
  and a rejoiner each set it with no new network message. `ModKitLight` holds it over the game's own lighting call
  (two frames later, L2) and lets it go when the map ends.
- **Logged** once a map: `weather look: <weather> over 4 s on <lighting group>, ground y <y> from <n> tiles, the camera
  <d> from it: <the look>`. A throw is logged for that map and not retried every frame.
- **At the map's end** the hold lets go as the map's scene goes; the next scene brings its own lighting (Unity keeps
  fog and ambient per scene, and the boat runs the game's own lighting call at each world change). Not seen yet: §4.7.
- Not built, as decided: Fog Bank (heavy sea fog, `docs/mode/MECHANIC_IDEAS.md` §2), a mood ramp, night, a borrowed grade.

## 2. The self-test

| Check | Where | What it reads |
| --- | --- | --- |
| W4 Rising Water's look is what the game draws | `99_SelfTest.cs`, after W1 on the first probe map | the weather this machine lit for, a look held, and `RenderSettings` and the sun equal to the table's look on this map's own lighting, 4.5 s after the intro |
| W4 Quickening Current's look... | after W2, the second probe map | the same |
| W4 Nesting Ground's look... | after W3, the third probe map | the same |
| W4 Calm sets no look | `96_Flow.cs`, the flow's map 1 (row 1 is always calm) | the weather read as Calm and no look held |

The comparison (`Differs`, `Near`) moved from the gaps build's `92_GapsLight.cs` to `99_SelfTest.cs`, so the plain
self-test build has it; the gaps build uses the same one. It now also reads the fog's start and the sun's heading
(within 1 degree), which the game's own call sets the same way (`Transform.LookAt` along `Sundirection`, from its IL), so
the gaps checks that passed on the PC should still pass. The gaps build's L0 now sets the ground as the weathers do and
logs it beside the grid's lowest tile, so the PC round compares the two.

## 2b. Reviews

`/simplify` (four reviews): the first draft's weather table (a class and a dictionary) became a `switch` writing straight
onto the map's own look; the ground and colour helpers moved to `RWUtil` (`GroundLow`, `Rgb`) and the gaps build uses
them; its `Make` lost an unused argument. Left as they are: `LitAt` (the self-test waits out the fade with it, instead
of a fixed 4.5 s on every map) and three costs inside kit 1.13's `ModKitLight` hold (it measures the camera every frame,
and rewrites the ambient on every zoom frame): a kit change for one caller, and the PC measured no cost with a look
held (§6.2 of the lighting report); §4.5 times a zoom.

`/code-review high`: ten possible problems, read against the game's IL where it mattered. Fixed: the log line printed the
camera's distance before measuring it; the flow's calm check could not fail (it now expects Calm); a map with no
lighting group took whatever was on screen as its base (now no look, and W4 fails on it); one throw stopped the look for
the rest of the launch's log (now one try and one line a map); W4 did not read the fog's start or the sun's heading; the
ground came from `Entityposition` (now the tile's `Position`, as the game's own rally order uses); the sea chart page's
probe table had no W4 row. Not bugs, by the IL: `Entityclienttags` holds the `Surface` tiles on every machine.
Left: the map's end lets go with the scene, not with a fade back (above); a single helper for every `Surface` lookup.

## 3. Compiled here

From `TDS_Mod/ModSource`, `python3 ../Tools/cloud-check/check_mod.py RogueWavesMode/Code Shared RogueWavesMode "<defines>"`:

| Build | Result |
| --- | --- |
| release (`""`) | 0 errors, 0 warnings |
| `ROGUEWAVESMODE_SELFTEST` | 0 errors, 1 warning (the base's, `96_Flow.cs` `before`) |
| `+GAPS`; `+GAPS,SHOTS` | 0 errors, 2 warnings each (the base's two) |
| `+SHOTS`; `+CHARTLOOK`; `ROGUEWAVESMODE_FEEL` | 0 errors |
| `+TWOPC`, `+DIALOGLOOK` | fail here as on the base (`Unity.Netcode.Transports`, `UnityEngine.EventSystems`: not in the cloud stubs) |
| `check_editor.sh ../Assets/RogueWavesModeMod/Editor` | clean |

No other part changed. A clean compile says the code uses the game's names correctly; it says nothing about what the
game draws.

## 4. For the look's own PC round (after round 4)

The mode's self-test build, every part on, `Run-SelfTests.ps1 ... -TimeoutMinutes 30` (the flow takes about 15
minutes), then by hand. What to read and see:

1. **The four W4 lines pass**, and each map's `weather look:` line shows a ground height near the map's lowest tile with
   tiles counted (not `from 0 tiles`), and a camera distance near the 23 units Skyhook had. In the gaps build's L0
   camera line, the Surface tiles' lowest and the grid's lowest agree.
2. **Each weather by eye, with foes on screen and the HUD awake** (move the mouse, or raise `Game_Ui.Idlethreshold` for
   pictures, pitfall #170): Rising Water, Quickening Current, Nesting Ground, one map each, in the middle of a Tide.
   Do foes, their health bars and the tile markers still read at a glance? Does each weather read as itself?
3. **The fade:** the map flies in under its own light and the look rolls in over about 4 s once play starts; no pop.
4. **The sun's heading** on maps other than Skyhook (storm and dusk turn the sun; the camera faces other ways on other
   maps): long shadows, not a sun from below or straight behind the camera.
5. **Zoom all the way in and out** on a misty map: the haze stays about the same, and the frame time while zooming
   stays near the map's own (the hold rewrites the ambient light on each frame the camera's distance changes).
6. **Two players** (or the two-player test): the client and a rejoiner get the same look, from their own `weather look:`
   line.
7. **Leaving the map:** the boat and the next map are on their own light, no tint left (a calm map after a weathered one).
