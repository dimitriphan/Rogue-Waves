# Uloe

**Uloe, Light Personified** ▪ Advanced: the last vessel of the Rogue Waves pack (`docs/scope/10_Vessel_Uloe.txt`). A glass
light bulb on a lamp post whose towers grow stronger with the Light they receive. Uloe and its Photons give Light, PLUG IN
gives far more, and Photons mark foes with Illuminate and Blind.

Status: **0.1.0-dev, its self-test passes in game (148 pass, 0 fail on 2026-11-04, also as a client's vessel with a second player; notes in `IMPLEMENTATION_NOTES.md`), never played by hand.** The art is temporary and drawn by code.

| File | What |
|---|---|
| `DESIGN.md` | The sheet with every blank filled: all numbers, what changed from the concept and why, every default |
| `IMPLEMENTATION_NOTES.md` | The code map, what is compiled against what has run, what the self-test can't show |
| `Code/` | The mod, joined and compiled by the build script; `99_SelfTest.cs` and `97_Shots.cs` only with their defines |
| `Tools/Decode-Dump.ps1` | Turns `[UloeDump]` lines from Player.log back into PNGs |
| `Tools/Make-DirectionIcons.ps1` | Draws the four Direction cards' 12 x 12 icons into the mode part's `Art/Temporary/game_size` |
| `Previews/0.1/` | Renders of the temporary art outside Unity, and `ASSETS.md`, the list of it |
| `../../Assets/UloeMod/Editor/` | The generators: sheets, models, sounds, effects, the library prefab, the mod prefab, the export |

## Build

```powershell
# from ModSource\Uloe
& .\Build-UloeMod.ps1                                   # release: must print "Compile check passed"
& .\Build-UloeMod.ps1 -Define ULOE_SELFTEST,ULOE_SHOTS  # the self-test with captures
```

Then in Unity (after it compiled `Assets/UloeMod/Editor`), add a line `Uloe` to `Desktop\ClaudeProjects\handoff\queue.txt` and run
**Tools > ModKit > Build Queue** once (it runs Tools > Uloe > Build All Assets, Build Mod Prefab, Export Mod, Restore Parked
Mods). Enable it and run with `Tools\pc\Run-SelfTests.ps1` for a self-test build. The self-test's lines are `[Uloe] TEST ...`.

## The pack

Uloe joins the registry as part `Uloe`. It isn't in `ModSource/Pack/pack.txt` yet: add `part Uloe` when it ships.
