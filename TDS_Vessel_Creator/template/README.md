# The reference vessel

`Mummer/` is Mummer 1.0.0 as shipped (2026-10-02; self-test 72/72, release boot clean alongside Sprigg 1.4.1 and Argo
1.0.0), with three later changes: the builder carries the Aseprite hook every vessel got in `ModProjects`
(`MummerAseprite.cs`, `TDS_Mod/Tools/aseprite/README.md`), the self-test checks kit 1.4's boat layout
(`ModKitBoat.ClickReport`), and it uses kit 1.5's `ModKitTest` for the map, placement, foe spawning and the SETUP
outcome (`docs/08`). They compile here; the template with them has not been played on the PC yet.
`tools/New-Vessel.ps1` copies it into the mod project under a new name. Don't edit it here to build a vessel; scaffold,
then edit the copy.

| Path | Goes to | What |
|---|---|---|
| `Mummer/Code/*.cs` | `ModSource/<Name>/Code/` | runtime code (`docs/05_CODE_COOKBOOK.md` lists what is generic) |
| `Mummer/Editor/*.cs` | `Assets/<Name>Mod/Editor/` | asset generator and Tools menu (namespace `<Name>Build`) |
| `Mummer/Build-MummerMod.ps1` | `ModSource/<Name>/Build-<Name>Mod.ps1` | join + compile check |
| `Mummer/Deploy-Wait.ps1` | `ModSource/<Name>/` | enable after export, launch |
| `Mummer/Tools/Decode-Dump.ps1` | `ModSource/<Name>/Tools/` | captures → PNG |
| `DESIGN_TEMPLATE.md` | `ModSource/<Name>/DESIGN.md` | the design form |

Mummer's design, with every change from its concept: `examples/Mummer_DESIGN.md`. Its notes:
`examples/Mummer_IMPLEMENTATION_NOTES.md`.

**Scaffold verified** (2026-10-02, a throwaway vessel `Scaffoldtest`, then removed): `New-Vessel.ps1` → no template
names left → `Build-ScaffoldtestMod.ps1` passes as release and as test build → Unity compiles the renamed generator
with no errors → `Build All Assets` writes the full library (sprite sheets and UI art, 34 tower and upgrade prefabs, 15 effects, 18 sounds) without
touching `Assets/MummerMod` → `Build Mod Prefab` OK. It was not exported or played: the template itself is what was
verified in game.

What the template already does for any vessel: boots after the template vessel, registers everything under unique
names, builds the hero entry (all menu art), four buildables with curated upgrade trees, units, a projectile, an
ability with a picker and a cooldown button, a stateful building button, game-attached debuff looks, client effects
and quiet sounds, owner-only warnings, the full journal with tabs, the boat spot, diagnostics, in-game captures and a
self-test of 69 checks (73 with `<NAME>_SHOTS`, which adds the boat checks).
