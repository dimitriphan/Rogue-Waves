# Spool: temporary art drawn as files (2026-10-27)

**Everything in this folder is temporary.** It fills a slot until Dimitri's own art replaces it. The rest of Spool's
temporary art is drawn by code in `Assets/SpoolMod/Editor` (Spool's `Previews/0.1/ASSETS.md`, PR #42); the pack's whole
list is `docs/TEMP_ART.md`. Spool 0.1.0-dev lives on PR #42's branch, where the Spool thread wired the tuft and the
needle at `e0d2bf6` (2026-10-27; compiled and editor-checked, not yet seen in game). The five animation sheets below
came after; the Spool thread wired them at `ad85b00` (a `FileArt` like Yossele's in `SpoolBuilder.SaveSheet`, the PNGs
copied in; editor check clean, not yet built in Unity).

| File | Size | Fills | Replaces | In game |
| --- | --- | --- | --- | --- |
| `game_size/Spool_Tuft.png` | 12 x 12, PPU 20, centre pivot | ground Thread: a fluffed tuft of lilac thread with a loose end, lit from the upper left | the 8 x 8 `TuftPixels` in `Code/11_Client.cs` (the play test never caught one in a capture) | wired on PR #42 (`e0d2bf6`): 0.42 to 0.78 wide on the ground |
| `game_size/Spoolhead-Sheet.png` | 240 x 96, 24 px cells, bottom-centre pivot; rows v0-v3, columns 0-5 Move (0 = Idle), 6-9 Attack | the Thread Head: a clean lilac ball of yarn with three wraps, Spool's zipper mouth, teeth, eyes and tongue, white gloves, a loose end that swings as it walks; the attack winds back, punches with the leading glove and sparks | the code's ball textured with dots (read as dirt) that only bobbed | wired on PR #42 (`ad85b00`) |
| `game_size/Spoolstitched-Sheet.png`, `Spooldart-Sheet.png`, `Spoolseam-Sheet.png`, `Spoolmangled-Sheet.png` | 160 x 64 each, 16 px cells, same layout | the four Crafted, same shapes and colours, now lit and shaded: a real step (legs lift and swing, gloves swing against them) and an attack with a wind-up, a strike with a smear and a hit spark. The Dart raises its needle behind its head and throws it | the code's flat dolls that only bobbed | wired on PR #42 (`ad85b00`) |
| `preview_8x/models_Needle.png` | render only | the Dart's thrown needle (`Spool_Needle`): 0.42 long and about 0.05 thick, a white point, a dark collar, a purple thread tail | the 0.26 x 0.02 needle, too thin to see in flight | wired on PR #42 (`e0d2bf6`), the code below |

Previews: `preview_8x/<sheet>_8x.png`, and GIFs `Spoolhead_move.gif`, `Spoolhead_attack.gif`,
`Spool_crafted_move.gif`, `Spool_crafted_attack.gif`. The vessel's own sheets (idle, walk, attack, Patch, fall, landing)
stay as they are, drawn from Dimitri's reshade ("leave Spool as is for now").

## Wiring the animation sheets (done on PR #42 at `ad85b00`; kept for reference)

Give `SpoolBuilder.SaveSheet` the same file override as Yossele's (`Assets/YosseleMod/Editor/YosseleBuilder.cs`,
`FileArt`): call `SpoolAseprite.Apply(FileArt(px, name), folder, name, cw)`, where `FileArt` loads
`ModSource/Spool/Art/<name>.png` (final) or `Art/Temporary/game_size/<name>.png` (temporary) when it has the code
sheet's size and logs which it used. The five sheets have the code's names, sizes and layout, so nothing else changes.

## Wiring the tuft and the needle (done on PR #42; kept for reference)

**Tuft.** Replace `TuftPixels` with these rows and add the three colours to `SpoolAssets.IconPalette` (or load the PNG):
`j` `B58BC9` (lilac light), `x` `945DAD` (`SpoolClient.ThreadColor`), `X` `7A4A8F` (shade). The sprite is 12 px where the
old one was 8, so scale `SetTuft`'s `Lerp(0.8f, 1.6f, ...)` by 2/3 to keep the same size on the ground, or keep it to
make tufts read better.

```
"............", "....jjx.....", "..jjjxjxj...", ".jxjjxxjxx..", ".jjxjxjxxX..", "jxjjxxjxXX..",
".jxjxxxXxX..", ".xjxxXxXXX..", "..XxXXXXX...", "...XXXX..X..", ".........X..", "..........X.",
```

**Needle.** In `Assets/SpoolMod/Editor/SpoolModels.cs`, `Needle()` becomes (checked with `check_editor.sh` and the
lite renderer on the PR #42 branch, 2026-10-27):

```csharp
        // Temporary art (ModSource/Spool/Art/Temporary/README.md): a needle that reads in flight, 0.42 long and about 0.05
        // thick, a white point, a dark collar at the eye and a purple thread tail.
        static Lp Needle()
        {
            var m = new Lp();
            m.Tube(V(0, 0.5f, -0.2f), V(0, 0.5f, 0.14f), 0.026f, 0.022f, 6, Pal.Steel);
            m.Tube(V(0, 0.5f, 0.14f), V(0, 0.5f, 0.22f), 0.022f, 0f, 6, Pal.White);
            m.Tube(V(0, 0.5f, -0.21f), V(0, 0.5f, -0.17f), 0.031f, 0.031f, 6, Pal.SteelDark);
            Thread(m, V(0, 0.5f, -0.2f), V(0.035f, 0.53f, -0.3f), Pal.Felt2, 0.013f);
            Thread(m, V(0.035f, 0.53f, -0.3f), V(-0.02f, 0.5f, -0.41f), Pal.Felt2, 0.013f);
            return m;
        }
```

## Replacing it

Put the final art at `ModSource/Spool/Art/<same name>.png` (one folder up) and point the code there, then delete the
temporary file. Generators: `concept-tools/temp_art/ui_icons.py` (the tuft rows are its `TUFT`),
`spool_threadhead.py`, `spool_units.py`.
