# Spool: temporary art drawn as files (2026-10-27)

**Everything in this folder is temporary.** It fills a slot until Dimitri's own art replaces it. The rest of Spool's
temporary art is drawn by code in `Assets/SpoolMod/Editor` (Spool's `Previews/0.1/ASSETS.md`); the pack's whole
list is `docs/TEMP_ART.md`. The Spool thread wired the tuft and the needle at `e0d2bf6` (2026-10-27) and the five
animation sheets below at `ad85b00` (a `FileArt` like Yossele's in `SpoolBuilder.SaveSheet`), both on PR #42, merged.
Every Spool build since takes them (self-test 121/0 on 2026-11-03, 133/0 on 2026-11-04); nobody has looked at them by
eye yet. `aseprite/` holds the same sheets as one frame per cell, layer "TEMP art", to hand-edit.

**Since 2026-11-05 Spool is sage green** (Dimitri's pick, `pastel_green_2026-11-05/`): the tuft, the Thread Head and
the Crafted were recoloured by `concept-tools/temp_art/spool_sage.py`, so each "lilac" or "purple" below is how a piece
was drawn, now its sage match; the needle in game is the "Pearl-headed pin" (a steel shaft, a white point, a pale sage
pearl head, no thread, `SpoolModels.Needle`), not the purple-tailed needle the table and the wiring notes describe.

| File | Size | Fills | Replaces | In game |
| --- | --- | --- | --- | --- |
| `game_size/Spool_Tuft.png` | 12 x 12, PPU 20, centre pivot | ground Thread: a fluffed tuft of lilac thread with a loose end, lit from the upper left | the 8 x 8 `TuftPixels` in `Code/11_Client.cs` (the play test never caught one in a capture) | wired on PR #42 (`e0d2bf6`): 0.42 to 0.78 wide on the ground |
| `game_size/Spoolhead-Sheet.png` | 240 x 96, 24 px cells, bottom-centre pivot; rows v0-v3, columns 0-5 Move (0 = Idle), 6-9 Attack | the Thread Head, Snoble-sized since 2026-11-05 (Dimitri's pick, `minions_2026-11-04/threadhead/v04` Bobbin): a wooden bobbin wound thick with purple thread, Spool's zipper mouth opening in the thread between the flanges, white gloves | the 2026-10-28 yarn ball, before that the code's ball that only bobbed | wired on PR #42 (`ad85b00`); this sheet copied 2026-11-05 |
| `game_size/Spoolstitched-Sheet.png`, `Spooldart-Sheet.png`, `Spoolseam-Sheet.png`, `Spoolmangled-Sheet.png` | 160 x 64 each, 16 px cells, same layout | the four Crafted, Snoble-sized since 2026-11-05 (Dimitri's picks from `minions_2026-11-04/`): Stitched v06 Patchwork (sewn from purple and lilac squares), Dart v01 Long Hood (thin and lilac with a long hood point and a needle it raises and throws, drawn a little larger at his note), Seam v06 Button (a round felt button whose holes are its eyes), Mangled v02 Burst (a spiky ball of cotton tufts). A real step and a wind-up, strike and recover attack | the 2026-10-28 Spool-shaped dolls, before that the code's flat dolls | wired on PR #42 (`ad85b00`); these sheets copied 2026-11-05 |
| `preview_8x/models_Needle.png` | render only | the Dart's thrown needle (`Spool_Needle`): 0.42 long and about 0.05 thick, a white point, a dark collar, a purple thread tail | the 0.26 x 0.02 needle, too thin to see in flight | wired on PR #42 (`e0d2bf6`), the code below |

Previews: `preview_8x/<sheet>_8x.png`, and GIFs `Spoolhead_move.gif`, `Spoolhead_attack.gif`,
`Spool_crafted_move.gif`, `Spool_crafted_attack.gif`; the whole family beside his sprite is
`/mnt/project-files/concepts/temp_art_2026-10-27/Spool_family_2026-10-28.png`. The vessel's own sheets (idle, walk, attack, Patch, fall, landing)
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

**Versions to pick from (2026-10-28):** three new versions of every file above are in `variants/v1|v2|v3/` (its README names them); nothing there is wired until Dimitri picks (`docs/TEMP_ART.md`).

**Picks applied (2026-11-03):** the files in `game_size/` now carry Dimitri's picks among the versions (the list is in `docs/TEMP_ART.md`, "Dimitri's picks"), so where a row above describes art he replaced, the picked version's line in `variants/README.md` describes it now. Compiled, not yet seen in game.

**Minion versions (2026-11-04):** ten Snoble-sized versions of the Thread Head and each of the four Crafted are in `minions_2026-11-04/` (its README); Dimitri's picks (2026-11-05) are copied into `game_size/` and `aseprite/` above, and the GIFs in `preview_8x/` were redrawn from them.

**Sage green (2026-11-05):** Dimitri found Spool's purple too close to the foes and picked a soft sage (after a yellow and a mint, `pastel_green_2026-11-05/`), then had it applied to all of Spool. Every file in `game_size/`, `aseprite/` and `preview_8x/` is recoloured by `concept-tools/temp_art/spool_sage.py` (one old-to-new table; where a row above says purple or lilac, read the sage it maps to; since 2026-11-06 its `SHEET_MAP` also turns the Seam's white felt and the Mangled's white cotton sage, as they read as Pearl), and the code's colours follow (that folder's README lists them). Compiled, not yet seen in game.
