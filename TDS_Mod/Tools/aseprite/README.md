# Aseprite files for hand-editing vessel sprites

Every vessel's sprites are drawn by code (`Assets/<Name>Mod/Editor/<Name>Character.cs`). These tools let you hand-edit
any frame in Aseprite without losing the edit on the next build.

## In the Unity project (the normal way)

1. **Tools > \<Name\> > Build All Assets** writes `ModSource/<Name>/Aseprite/generated/` from the code's current art,
   every build:

   | File | Canvas | Tags |
   | --- | --- | --- |
   | `<Name>.aseprite` | 32×32 | `idle_v0`-`v3`, `move_v0`-`v3`, `attack_v0`-`v3`, the ability (`zap_v0`...), `fall`, `land`, `menu_idle`, `icon`, `end_win`, `end_lose`, `journal_tab` |
   | `<Name>_16px.aseprite` | 16×16 | `mini_idle_v0`... (boat mini), then each unit (`cutpurse_move_v0`, `cutpurse_attack_v0`...) |
   | `<Name>_splash.aseprite` | 128×128 | `splash` |
   | `<Name>_readyup.aseprite` | 12×12 | `readyup` |

   v0 faces the camera, v1 is v0 mirrored, v2 is the back, v3 is v2 mirrored. Sprigg's tags start with the growth stage.
   The palette holds every colour the vessel uses, translucent rim and ground-patch tones last.
2. Copy the file you want to change up one folder, into `ModSource/<Name>/Aseprite/`, and edit it. Keep the colour mode
   RGB, the canvas size and each tag's frame count. Extra layers are fine; hidden layers are ignored.
3. Run Build All Assets again, then export as usual. `ModSource/<Name>/build_log.txt` lists every sheet that used
   hand-edited frames.

How the merge works (`<Name>Aseprite.cs`): each tag's user data records where its frames go in the game's sheet and a
CRC-32 of each frame as it was written:

```
sheet=Vessel/Crackpot-Sheet.png;row=0;col=0;cols=6;rows=8;gen=1a2b3c4d,...;mirror=idle_v0
```

A frame whose pixels no longer match its CRC is yours and replaces the code's frame. A frame you left alone keeps
following the code, so later fixes to the art still come through. Where the code draws a facing as a mirror
(`mirror=`), editing the source frame also updates the mirrored one, unless you edited that one too. Delete your file
to go back to the code's art.

## Outside Unity

`ase_sheets.py` (Python 3, standard library only) writes the same files from PNG sheets and turns them back into
sheets:

```
python3 ase_sheets.py pack Crackpot out --version 1.0.0 path/to/sheets/*.png
python3 ase_sheets.py unpack sheets out/*.aseprite
```

`export_sheets.lua` does the `unpack` inside Aseprite 1.3+ (File > Scripts), writing `sheets/Vessel`, `sheets/UI` and
`sheets/Unit` next to the open file. Both rebuild whole sheets; only the builder merges frame by frame.

Checked in the cloud: the builder's C# writer and `ase_sheets.py` produce the same tags, CRCs, frames and palette for
Crackpot, Rustle and Broodie; files re-read by `ase_sheets.py` and the npm `ase-parser` match the sheets pixel for
pixel; edited frames, mirrors, extra layers, raw cels and hidden layers merge as described. In Unity it runs inside Argo's Build All
Assets (Argo's PC rounds, 2026-10-08); the written files have not been opened in Aseprite yet.
