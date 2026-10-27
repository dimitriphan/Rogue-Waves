# Spool: temporary art drawn as files (2026-10-27)

**Everything in this folder is temporary.** It fills a slot until Dimitri's own art replaces it. The rest of Spool's
temporary art is drawn by code in `Assets/SpoolMod/Editor` (Spool's `Previews/0.1/ASSETS.md`, PR #42); the pack's whole
list is `docs/TEMP_ART.md`. Spool 0.1.0-dev lives on PR #42's branch, so nothing here is wired yet: the Spool thread
takes these in (below).

| File | Size | Fills | Replaces | In game |
| --- | --- | --- | --- | --- |
| `game_size/Spool_Tuft.png` | 12 x 12, PPU 20, centre pivot | ground Thread: a fluffed tuft of lilac thread with a loose end, lit from the upper left | the 8 x 8 `TuftPixels` in `Code/11_Client.cs` (the play test never caught one in a capture) | not wired |
| `preview_8x/models_Needle.png` | render only | the Dart's thrown needle (`Spool_Needle`): 0.42 long and about 0.05 thick, a white point, a dark collar, a purple thread tail | the 0.26 x 0.02 needle, too thin to see in flight | not wired; the code is below |

## Wiring (for the Spool thread)

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
temporary file. Generator: `concept-tools/temp_art/ui_icons.py` (the tuft rows are its `TUFT`).
