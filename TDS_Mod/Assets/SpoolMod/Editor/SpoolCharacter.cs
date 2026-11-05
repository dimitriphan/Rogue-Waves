// Spool asset generator: the vessel's pixel art. TEMPORARY ART built from Dimitri's own sprite.
// Baseline: his reshade (concepts/vessels_2026-10-09/v3_flat/game_size/spool_reshade.png, the pixels of the
// spool_reshade.aseprite he sent) and its back view (concepts/vessels_2026-10-09/back/game_size/spool_back.png),
// embedded below pixel for pixel as Front / Back. Every 32 px frame is those pixels moved: the hood swings on its hinge,
// the legs step, the gloves move, with pixels added for the needle, the thread, cotton and dust. The walk is the concept
// in concepts/walk_2026-10-12 (src/walk.py) ported frame for frame. The 16 px boat mini, the Crafted and the Thread Head
// are drawn new in his colours. Finish: fully opaque, shade on the right, no rim, no ground patch.
// Spool (sage green since 2026-11-05, Dimitri's pick; purple before, too close to the foes' magenta): a hooded plush whose face is a stitched opening (a zipper of teeth), yellow eyes looking out, a pink
// tongue, white gloves, and a severed arm on the floor with cotton coming out of the wound (an arm, not a tail).
// 32x32 frames, PPU 20, bottom-centre pivot. Variants: v0 front facing right, v1 front mirrored, v2 back, v3 back mirrored.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpoolBuild
{
    public class Pose
    {
        public int bob;                               // everything above the hips, pixels, + = down
        public int lean;                              // everything above the hips, sideways (+ = the way it faces)
        public double hood;                           // the hood on its hinge: 1 shut over the eyes, 0 as drawn, -0.75 wide open
        public int liftL, liftR;                      // legs (+ = up)
        public bool blink;
        public int tongue;                            // extra tongue pixels hanging down (front)
        public int gLx, gLy, gRx, gRy;                // glove offsets (image left and right); a sleeve joins a glove that moves out
        public bool air;                              // airborne: the legs and the floor arm ride with the body
        public int armDy;                             // the floor arm while airborne (+ = down)
        public int needle;                            // in the right glove: 0 none, 1 held up, 2 pointing out (a stitch)
        public readonly List<Vector3Int> fx = new List<Vector3Int>();
    }

    public static class SpoolCharacter
    {
        public const int FxDust = 1, FxCotton = 2, FxThread = 3, FxThreadDark = 4, FxSpark = 5, FxImpact = 6, FxSwipe = 7, FxStar = 8;

        // The reshade's colours, then the colours the frames add. Since 2026-11-05 the plush, the mouth and the thread are
        // Dimitri's sage (ModSource/Spool/Art/Temporary/pastel_green_2026-11-05/; concept-tools/temp_art/spool_sage.py MAP),
        // were 9363AE / 784B94, 41174F / 4A1D5A and the lilac D5BEEC / B292D6; the pixels are his, unmoved.
        public static readonly Dictionary<char, Color32> P = new Dictionary<char, Color32>
        {
            { 'a', Col.H("93CCA1") }, { 'b', Col.H("60A871") },                            // plush, plush shade
            { 'm', Col.H("1F3325") }, { 'n', Col.H("26422F") },                            // the dark inside the mouth, its edge
            { 't', Col.H("E4DEDA") }, { 'g', Col.H("9A938E") },                            // teeth and cuffs, teeth shade
            { 'v', Col.H("F6F3F8") }, { 'u', Col.H("D8D1E2") }, { 'w', Col.H("FFFFFF") },  // gloves, glove shade, cotton
            { 'q', Col.H("E58CC0") }, { 'p', Col.H("C96CA6") },                            // tongue, tongue shade
            { 'e', Col.H("E6D85A") },                                                      // eyes
            // added: thread, needle, dust, sparkle
            { 'l', Col.H("CBE6D1") }, { 'L', Col.H("A2CFAD") }, { 's', Col.H("C9D2DD") }, { 'S', Col.H("8C96A3") },
            { 'k', Col.H("BDB3A8") }, { 'y', Col.H("F8E39A") },
        };

        static Color32 C(char k) { return P[k]; }

        // Dimitri's reshade, front (v0). Rows 13-20 the hood and the mouth (teeth t/g, the dark m, eyes e), 21-23 the lower
        // teeth and the tongue, 24-28 the body and the white gloves, 29-31 the legs and the severed arm on the floor.
        static readonly string[] Front =
        {
            "................................",
            "................................",
            "................................",
            "................................",
            "................................",
            "................................",
            "................................",
            "................................",
            "................................",
            "................................",
            "................................",
            "................................",
            "................................",
            "..................aab...........",
            "...............aaaaabb..........",
            ".............aaaaaaabbb.........",
            "............aaaabtgtgtb.........",
            "...........aaabtgmmmmmgg........",
            "...........abtgmmmmmmmt.........",
            "...........bgmmmemmmemn.........",
            "...........tmmmmmmmmmm..........",
            "...........bgtgtgtgtqpt.........",
            "...........abbbbbbbbqpb.........",
            "............aaaaaaabbp..........",
            ".........vu..aaaabbb............",
            "..........vu.aaabbbt.t..........",
            ".........vvuaaaabbtvu...........",
            "..........bbaaaaabbtvu..........",
            "..........aaaaaaaabb............",
            "........u.ab.aaaaabb............",
            "......u.baab.ab...bb............",
            "......waaab..ab...bbb...........",
        };

        // His back view (v2): the hood from behind, the gloves and the floor arm on the other side.
        static readonly string[] Back =
        {
            "................................",
            "................................",
            "................................",
            "................................",
            "................................",
            "................................",
            "................................",
            "................................",
            "................................",
            "................................",
            "................................",
            "................................",
            "................................",
            "...........abb..................",
            "..........aaaaabb...............",
            ".........aaaaaaaabb.............",
            ".........aaaaaaaaabb............",
            ".........gaaaaaaaaabb...........",
            ".........aaaaaaaaaabb...........",
            ".........aaaaaaaaaabb...........",
            "..........aaaaaaaaabb...........",
            ".........aaaaaaaaaabb...........",
            ".........bbbbbbbbbbbb...........",
            "..........aaaaaaaabb............",
            "............aaaaabb..vu.........",
            "..........t.taaaabb.vu..........",
            "...........vutaaaabbvvu.........",
            "..........vutaaaaaaabb..........",
            "............aaaaaaaabb..........",
            "............aaaaabb.ab.u.w......",
            "............bb...bb.aaabmwu.....",
            "...........abb...bb..aaabuw.....",
        };

        const int W = 32, Hips = 29;

        // One facing's parts, cut the way the walk concept cut them.
        class View
        {
            public char[,] Src;
            public bool IsBack;
            public int Jaw;
            public int[] Legs;                         // x0, x1 per leg: left, right
            public Func<int, bool> ArmCol;             // the columns of the floor arm
            public int[] Attach;                       // the two pixels that join the floor arm to the hip
            public int[] Full;                         // per column: how far the hood's edge drops to close the mouth
            public Box GloveL, GloveR;             // x, y, w, h of each glove
            public Vector2Int ShoulderL, ShoulderR;
        }

        // A glove's box (UnityEngine.RectInt, which the cloud preview's stand-in lacks).
        struct Box
        {
            public int x, y, width, height;
            public Box(int x, int y, int width, int height) { this.x = x; this.y = y; this.width = width; this.height = height; }
        }

        static bool Inside(char k) { return k == 'm' || k == 'e' || k == 'n'; }
        static bool Glove(char k) { return k == 't' || k == 'u' || k == 'v'; }

        static View front, back;

        static View Get(bool isBack)
        {
            if (isBack && back != null) return back;
            if (!isBack && front != null) return front;
            var rows = isBack ? Back : Front;
            var v = new View { Src = new char[W, W], IsBack = isBack };
            for (int y = 0; y < W; y++) for (int x = 0; x < W; x++) v.Src[x, y] = rows[y][x];
            if (!isBack)
            {
                v.Jaw = 20;
                v.Legs = new[] { 13, 14, 18, 20 };
                v.ArmCol = x => x <= 11;
                v.Attach = new[] { 10, 11 };
                v.Full = Hinge(v.Src, v.Jaw);
                v.GloveL = new Box(9, 24, 3, 3);
                v.GloveR = new Box(19, 25, 3, 3);
                v.ShoulderL = new Vector2Int(12, 25);
                v.ShoulderR = new Vector2Int(18, 25);
                front = v;
            }
            else
            {
                // From behind the mouth is hidden: the head lifts on the mouth side (left) and drops shut again.
                v.Jaw = 21;
                v.Legs = new[] { 11, 13, 17, 18 };
                v.ArmCol = x => x >= 20;
                v.Attach = new[] { 20, 21 };
                v.Full = new int[W];
                for (int x = 0; x < W; x++) v.Full[x] = -(int)Math.Round(Math.Max(0, 20 - x) * 4 / 11.0);
                v.GloveL = new Box(10, 25, 4, 3);
                v.GloveR = new Box(20, 24, 3, 3);
                v.ShoulderL = new Vector2Int(13, 26);
                v.ShoulderR = new Vector2Int(19, 25);
                back = v;
            }
            return v;
        }

        // The hood swings on a hinge at the back of the mouth: per column, how far its upper teeth must drop to meet the
        // lower teeth. Columns further from the hinge drop further, so the flap tilts like a lid.
        static int[] Hinge(char[,] s, int jaw)
        {
            var need = new int[W];
            int first = -1;
            for (int x = 0; x < W; x++)
            {
                int top = -1;
                for (int y = 0; y <= jaw; y++) if (Inside(s[x, y])) { top = y; break; }
                if (top < 0) continue;
                int above = -1;
                for (int y = 0; y < top; y++) if (s[x, y] != '.') above = y;
                need[x] = jaw - above;
                if (first < 0) first = x;
            }
            var full = new int[W];
            int run = 0;
            for (int x = 0; x < W; x++)
            {
                run = Math.Max(run, need[x]);
                full[x] = first >= 0 && x >= first ? run : 0;
            }
            return full;
        }

        static bool InGlove(Box r, int x, int y) { return x >= r.x && x < r.x + r.width && y >= r.y && y < r.y + r.height; }

        // ---------------------------------------------------------------- frame composer

        public static Px Frame(int variant, Pose p)
        {
            var v = Get(variant >= 2);
            var s = v.Src;
            var c = new Px(W, W);
            Func<char, char> eye = k => p.blink && k == 'e' ? 'm' : k;
            Action<int, int, char> put = (x, y, k) => { if (k != '.') c.Set(x, y, C(k)); };
            int lean = p.lean;

            // The floor arm: on the ground it stays put and drags (the two hip pixels follow the body); airborne it rides along.
            for (int y = Hips; y < W; y++)
                for (int x = 0; x < W; x++)
                    if (v.ArmCol(x)) put(x + (p.air ? lean : 0), y + (p.air ? p.bob + p.armDy : 0), s[x, y]);
            if (p.bob != 0 && !p.air)
                foreach (int x in v.Attach) put(x + lean, Hips + p.bob, s[x, Hips]);

            // Legs hang from the hips and reach the ground (the first leg row repeats or drops to fit).
            for (int i = 0; i < 2; i++)
                Strip(v, c, v.Legs[i * 2], v.Legs[i * 2 + 1], i == 0 ? p.liftL : p.liftR, p.bob, p.air ? W - 1 + p.bob : W - 1, lean);

            // The hood and the mouth.
            if (!v.IsBack)
            {
                var shift = new int[W];
                for (int x = 0; x < W; x++) shift[x] = (int)Math.Round(p.hood * v.Full[x]);
                for (int x = 0; x < W; x++)
                {
                    int lowest = -1;
                    for (int y = 0; y <= v.Jaw; y++) if (s[x, y] != '.' && !Inside(s[x, y])) lowest = y;
                    if (lowest < 0 || v.Full[x] == 0) continue;
                    // the open mouth: as drawn, then dark where the hood lifted off (at rest, exactly as drawn)
                    for (int y = lowest + shift[x] + 1; y <= v.Jaw; y++)
                    {
                        char k = y >= 0 && y < W ? s[x, y] : '.';
                        if (Inside(k) || p.hood != 0) put(x + lean, y + p.bob, Inside(k) ? eye(k) : 'm');
                    }
                }
                for (int y = 0; y <= v.Jaw; y++)
                    for (int x = 0; x < W; x++)
                        if (s[x, y] != '.' && !Inside(s[x, y])) put(x + lean, y + shift[x] + p.bob, s[x, y]);
            }
            else
            {
                double lift = Math.Max(0.0, -p.hood) + (p.hood > 0 && p.hood < 1 ? 0.4 : 0);
                for (int y = 0; y <= v.Jaw; y++)
                    for (int x = 0; x < W; x++)
                    {
                        char k = s[x, y];
                        if (k == '.') continue;
                        int dy = (int)Math.Round(lift * v.Full[x]);
                        put(x + lean, y + dy + p.bob, k);
                        if (y == v.Jaw)   // stretch the head's lowest row up into the gap
                            for (int g = dy; g < 0; g++) put(x + lean, y + g + 1 + p.bob, k);
                    }
            }

            // The body, then the gloves where they are put.
            bool movedL = p.gLx != 0 || p.gLy != 0, movedR = p.gRx != 0 || p.gRy != 0;
            for (int y = v.Jaw + 1; y <= Hips; y++)
                for (int x = 0; x < W; x++)
                {
                    char k = s[x, y];
                    if (y == Hips && v.ArmCol(x)) continue;
                    if (movedL && InGlove(v.GloveL, x, y) && Glove(k)) continue;
                    if (movedR && InGlove(v.GloveR, x, y) && Glove(k)) continue;
                    put(x + lean, y + p.bob, eye(k));
                }
            if (!v.IsBack && p.tongue > 0)
                for (int i = 1; i <= p.tongue; i++) { put(21 + lean, 23 + i + p.bob, 'p'); if (i < p.tongue) put(20 + lean, 23 + i + p.bob, 'q'); }
            if (movedL) MoveGlove(c, v, v.GloveL, v.ShoulderL, p.gLx + lean, p.gLy + p.bob, lean, p.bob);
            if (movedR) MoveGlove(c, v, v.GloveR, v.ShoulderR, p.gRx + lean, p.gRy + p.bob, lean, p.bob);
            if (p.needle != 0) Needle(c, v.GloveR, p.gRx + lean, p.gRy + p.bob, p.needle);

            foreach (var f in p.fx) Fx(c, f);
            return variant == 1 || variant == 3 ? c.FlipX() : c;
        }

        // A leg: columns x0..x1 from row Hips + 1 down, hanging from the hips (moved by bob) with its foot at 'ground' - lift.
        static void Strip(View v, Px c, int x0, int x1, int lift, int bob, int ground, int lean)
        {
            int top = Hips + bob;
            int foot = ground - lift;
            for (int y = top + 1; y < foot; y++)
                for (int x = x0; x <= x1; x++)
                    if (v.Src[x, Hips + 1] != '.') c.Set(x + lean, y, C(v.Src[x, Hips + 1]));
            for (int x = x0; x <= x1; x++)
                if (v.Src[x, W - 1] != '.') c.Set(x + lean, foot, C(v.Src[x, W - 1]));
        }

        // A glove moved by (dx, dy); a stubby plush sleeve (2 px, shade below or right) joins it to the shoulder when it
        // has moved out more than a pixel.
        static void MoveGlove(Px c, View v, Box g, Vector2Int shoulder, int dx, int dy, int lean, int bob)
        {
            int gx = g.x + g.width / 2 + dx, gy = g.y + g.height / 2 + dy;
            int sx = shoulder.x + lean, sy = shoulder.y + bob;
            if (Math.Abs(gx - sx) + Math.Abs(gy - sy) > 2)
                Limb(c, sx, sy, gx, gy, 'a', 'b');
            for (int y = g.y; y < g.y + g.height; y++)
                for (int x = g.x; x < g.x + g.width; x++)
                    if (Glove(v.Src[x, y])) c.Set(x + dx, y + dy, C(v.Src[x, y]));
        }

        // A 2 px limb from (x0, y0) to (x1, y1): steep runs fill left and shade right, flat runs fill on top and shade below.
        static void Limb(Px c, int x0, int y0, int x1, int y1, char fill, char shade)
        {
            int n = Math.Max(Math.Max(Math.Abs(x1 - x0), Math.Abs(y1 - y0)), 1);
            bool flat = Math.Abs(x1 - x0) > Math.Abs(y1 - y0);
            for (int i = 0; i <= n; i++)
            {
                int x = (int)Math.Round(x0 + (x1 - x0) * i / (double)n), y = (int)Math.Round(y0 + (y1 - y0) * i / (double)n);
                c.Set(x, y, C(fill));
                if (flat) c.Set(x, y + 1, C(shade)); else c.Set(x + 1, y, C(shade));
            }
        }

        static void Line(Px c, int x0, int y0, int x1, int y1, char k)
        {
            int n = Math.Max(Math.Max(Math.Abs(x1 - x0), Math.Abs(y1 - y0)), 1);
            for (int i = 0; i <= n; i++)
                c.Set((int)Math.Round(x0 + (x1 - x0) * i / (double)n), (int)Math.Round(y0 + (y1 - y0) * i / (double)n), C(k));
        }

        // The sewing needle in the right glove, with its thread: held up (1) or pointing out to stitch (2).
        static void Needle(Px c, Box g, int dx, int dy, int mode)
        {
            int x = g.x + g.width - 1 + dx, y = g.y + dy;
            if (mode == 1)
            {
                for (int i = 1; i <= 5; i++) c.Set(x, y - i, C(i == 5 ? 'v' : i == 1 ? 'S' : 's'));
                c.Set(x - 1, y - 4, C('l')); c.Set(x - 2, y - 3, C('l')); c.Set(x - 2, y - 2, C('L'));
            }
            else
            {
                for (int i = 1; i <= 5; i++) c.Set(x + i, y + 1, C(i == 5 ? 'v' : i == 1 ? 'S' : 's'));
                c.Set(x + 1, y + 2, C('l')); c.Set(x, y + 3, C('l')); c.Set(x - 1, y + 3, C('L'));
            }
        }

        static void Fx(Px c, Vector3Int f)
        {
            switch (f.z)
            {
                case FxDust: c.Set(f.x, f.y, C('k')); break;
                case FxCotton: c.Set(f.x, f.y, C('w')); break;
                case FxThread: c.Set(f.x, f.y, C('l')); break;
                case FxThreadDark: c.Set(f.x, f.y, C('L')); break;
                case FxSpark: c.Set(f.x, f.y, C('y')); break;
                case FxImpact: c.Set(f.x, f.y, C('w')); c.Set(f.x - 1, f.y, C('v')); c.Set(f.x + 1, f.y, C('v')); c.Set(f.x, f.y - 1, C('v')); c.Set(f.x, f.y + 1, C('v')); break;
                case FxSwipe: c.Set(f.x, f.y, C('v')); break;
                case FxStar: c.Set(f.x, f.y, C('w')); c.Set(f.x - 1, f.y, C('y')); c.Set(f.x + 1, f.y, C('y')); c.Set(f.x, f.y - 1, C('y')); c.Set(f.x, f.y + 1, C('y')); break;
            }
        }

        // ---------------------------------------------------------------- animations

        static Pose Add(Pose p, params int[] xyz)
        {
            for (int i = 0; i + 2 < xyz.Length; i += 3) p.fx.Add(new Vector3Int(xyz[i], xyz[i + 1], xyz[i + 2]));
            return p;
        }

        // A plush breath: the hood settles a little and lifts again, a blink inside the mouth, the tongue lolls.
        public static List<Pose> Idle()
        {
            return new List<Pose>
            {
                new Pose(),
                new Pose { hood = 0.25 },
                new Pose { bob = 1, hood = 0.25 },
                new Pose { bob = 1, blink = true },
                new Pose { hood = -0.25 },
                new Pose { tongue = 1 },
            };
        }

        // The walk concept (concepts/walk_2026-10-12): the legs step and the body bobs a pixel; the eyes stay level in the
        // mouth. The hood swings on its hinge, carried by gravity: shut over the eyes on each footfall, just ajar as the body
        // rises, flung wide as it drops. The severed arm stays on the floor and drags.
        public static List<Pose> Move()
        {
            return new List<Pose>
            {
                new Pose { hood = 1 },
                new Pose { bob = -1, hood = 0.25, liftL = 1 },
                new Pose { hood = -0.75, liftL = 1 },
                new Pose { hood = 1 },
                new Pose { bob = -1, hood = 0.25, liftR = 1 },
                new Pose { hood = -0.75, liftR = 1 },
            };
        }

        // The swat: the right glove winds up beside the hood, swings out and comes down on the foe; the mouth gapes.
        public static List<Pose> Attack()
        {
            var wind = new Pose { lean = -1, gRx = -1, gRy = -5, hood = 0.25 };
            var swing = Add(new Pose { lean = 1, gRx = 4, gRy = -3, hood = -0.5 }, 24, 18, FxSwipe, 26, 19, FxSwipe, 27, 21, FxSwipe);
            var hit = Add(new Pose { lean = 1, bob = 1, gRx = 5, gRy = 1, hood = -0.75, tongue = 1 },
                28, 26, FxImpact, 25, 20, FxSwipe, 27, 22, FxSwipe, 28, 24, FxSwipe, 30, 29, FxDust);
            var back = new Pose { gRx = 2, hood = 0 };
            return new List<Pose> { wind, swing, hit, back };
        }

        // PATCH: needle and thread out, two quick stitches toward the mended unit, and a little sparkle.
        public static List<Pose> Patch()
        {
            var up = new Pose { gRx = 0, gRy = -3, needle = 1 };
            var stab = Add(new Pose { lean = 1, gRx = 3, gRy = -1, needle = 2, hood = -0.25 }, 29, 26, FxThread);
            var pull = Add(new Pose { gRx = 1, gRy = -5, needle = 1, hood = 0.25 }, 28, 27, FxThread, 29, 26, FxThreadDark, 27, 26, FxThread);
            var stab2 = Add(new Pose { lean = 1, gRx = 4, gRy = 0, needle = 2, hood = -0.5 }, 29, 26, FxThread, 28, 27, FxThread, 27, 27, FxThreadDark);
            var done = Add(new Pose { gRx = 1, gRy = -3, needle = 1, tongue = 1 }, 29, 24, FxStar, 26, 21, FxSpark, 27, 27, FxThread, 28, 27, FxThreadDark);
            var rest = Add(new Pose { hood = 0.25 }, 30, 22, FxSpark, 27, 27, FxThread);
            return new List<Pose> { up, stab, pull, stab2, done, rest };
        }

        // Loading screen: falling, the hood flung open, the gloves flapping up, the floor arm dangling, cotton streaming up.
        public static List<Px> Fall()
        {
            var frames = new List<Px>();
            for (int f = 0; f < 12; f++)
            {
                bool hi = f % 4 < 2;
                var p = new Pose
                {
                    bob = -3, air = true, hood = f % 2 == 0 ? -0.75 : -0.5, armDy = f % 2 == 0 ? 1 : 0,
                    gLx = -1, gLy = hi ? -4 : -3, gRx = 1, gRy = hi ? -3 : -4, liftL = f % 2, liftR = (f + 1) % 2,
                };
                Add(p, 4 + (f * 5) % 6, 31 - (f * 3) % 30, FxCotton, 26 - (f * 3) % 5, 31 - ((f * 3 + 15) % 30), FxCotton,
                    28, 31 - ((f * 3 + 7) % 30), FxThread);
                frames.Add(Frame(0, p));
            }
            return frames;
        }

        // Landing: drops in with the hood flung open, lands hard - the hood slams shut - then it peeks out, blinks, and
        // the tongue flops out.
        public static List<Px> Land()
        {
            var poses = new List<Pose>
            {
                new Pose { bob = -14, air = true, hood = -0.75, gLy = -3, gRy = -3 },
                new Pose { bob = -6, air = true, hood = -0.75, gLy = -2, gRy = -2 },
                Dust(new Pose { bob = 2, hood = 1 }, 0),
                Dust(new Pose { bob = 1, hood = 1 }, 1),
                Dust(new Pose { hood = 1 }, 2),
                new Pose { hood = 1 },
                new Pose { hood = 0.25 },
                new Pose { blink = true },
                new Pose(),
                new Pose { hood = -0.25, tongue = 1 },
                new Pose { tongue = 1 },
                new Pose(),
            };
            var frames = new List<Px>();
            foreach (var p in poses) frames.Add(Frame(0, p));
            return frames;
        }

        static Pose Dust(Pose p, int t)
        {
            int[,] bits = { { 5, 31 }, { 24, 31 }, { 4, 30 }, { 25, 30 }, { 6, 29 }, { 23, 29 } };
            for (int i = 0; i < bits.GetLength(0); i++)
            {
                if (t == 2 && i % 2 == 1) continue;
                int x = bits[i, 0] + (bits[i, 0] < 16 ? -t * 2 : t * 2);
                int y = bits[i, 1] - (t == 1 ? 1 : 0);
                p.fx.Add(new Vector3Int(x, y, FxDust));
            }
            return p;
        }

        // ---------------------------------------------------------------- sheets

        // Idle rows v0..v3 then Move rows v0..v3, 6 frames each: 192x256.
        public static Px MainSheet()
        {
            var rows = new List<List<Px>>();
            var idle = Idle();
            var move = Move();
            for (int v = 0; v < 4; v++) rows.Add(idle.ConvertAll(p => Frame(v, p)));
            for (int v = 0; v < 4; v++) rows.Add(move.ConvertAll(p => Frame(v, p)));
            return Px.Sheet(rows, 32, 32);
        }

        public static Px VariantSheet(List<Pose> poses)
        {
            var rows = new List<List<Px>>();
            for (int v = 0; v < 4; v++) rows.Add(poses.ConvertAll(p => Frame(v, p)));
            return Px.Sheet(rows, 32, 32);
        }

        public static Px Strip(List<Px> frames, int cw = 32, int ch = 32) { return Px.Sheet(new List<List<Px>> { frames }, cw, ch); }

        public static List<Px> IdleStrip() { return Idle().ConvertAll(p => Frame(0, p)); }

        // ---------------------------------------------------------------- boat sprite (16x16, Worldsprite)

        // The reshade at boat size (11 x 10): the hood, the mouth with its teeth and two eyes, the tongue, the gloves, the
        // legs and the severed arm on the floor.
        static readonly string[] MiniFront =
        {
            "................",
            "................",
            "................",
            "................",
            "................",
            "................",
            ".........aab....",
            ".......aaaabb...",
            "......abtgtgb...",
            "......tmemmen...",
            "......btgtgqp...",
            ".....v.aaabpb...",
            ".....vuaaaabtv..",
            "......aaaaabb...",
            ".....b.ab.bb....",
            "..waab.ab.bbb...",
        };

        static readonly string[] MiniBack =
        {
            "................",
            "................",
            "................",
            "................",
            "................",
            "................",
            "......abb.......",
            ".....aaaabb.....",
            ".....gaaaabb....",
            ".....aaaaabb....",
            ".....bbbbbbb....",
            "......aaaabb.vu.",
            "....tvuaaabbvu..",
            "......aaaaabb...",
            "......ab..bb.b..",
            ".....abb..bbaabw",
        };

        static Px Mini(bool isBack, int dy, bool shut, bool blink, int liftA, int liftB)
        {
            var src = isBack ? MiniBack : MiniFront;
            var c = new Px(16, 16);
            // legs and the floor arm (rows 14-15): the left leg is pair A, the right pair B; the arm stays on the floor
            for (int y = 14; y < 16; y++)
                for (int x = 0; x < 16; x++)
                {
                    char k = src[y][x];
                    if (k == '.') continue;
                    bool arm = isBack ? x >= 12 : x <= 5;
                    int lift = arm ? 0 : (isBack ? x <= 7 : x <= 8) ? liftA : liftB;
                    c.Set(x, y - lift, C(k));
                }
            if (dy < 0) for (int x = 0; x < 16; x++) if (src[14][x] != '.' && !(isBack ? x >= 12 : x <= 5)) c.Set(x, 13, C(src[14][x]));
            for (int y = 0; y < 14; y++)
                for (int x = 0; x < 16; x++)
                {
                    char k = src[y][x];
                    if (k == '.') continue;
                    if (k == 'e' && blink) k = 'm';
                    c.Set(x, y + dy, C(k));
                }
            // the hood shut: the upper teeth drop onto the lower ones and cover the eyes
            if (shut && !isBack)
            {
                for (int x = 6; x <= 12; x++) { char k = src[8][x]; if (k != '.') c.Set(x, 9 + dy, C(k)); }
                for (int x = 7; x <= 12; x++) { char k = src[7][x]; if (k != '.') c.Set(x, 8 + dy, C(k)); }
            }
            return c;
        }

        // 96x128: Idle v0..v3 (rows 0-3) then Move v0..v3 (rows 4-7), 6 frames of 16x16.
        public static Px MiniSheet()
        {
            var rows = new List<List<Px>>();
            int[] idleDy = { 0, 0, 1, 1, 0, 0 };
            bool[] blink = { false, false, false, true, false, false };
            int[] moveDy = { 0, -1, 0, 0, -1, 0 };
            bool[] shut = { true, false, false, true, false, false };
            int[] la = { 0, 1, 1, 0, 0, 0 }, lb = { 0, 0, 0, 0, 1, 1 };
            for (int v = 0; v < 4; v++)
            {
                var r = new List<Px>();
                for (int f = 0; f < 6; f++) { var m = Mini(v >= 2, idleDy[f], false, blink[f], 0, 0); r.Add(v == 1 || v == 3 ? m.FlipX() : m); }
                rows.Add(r);
            }
            for (int v = 0; v < 4; v++)
            {
                var r = new List<Px>();
                for (int f = 0; f < 6; f++) { var m = Mini(v >= 2, moveDy[f], shut[f], false, la[f], lb[f]); r.Add(v == 1 || v == 3 ? m.FlipX() : m); }
                rows.Add(r);
            }
            return Px.Sheet(rows, 16, 16);
        }

        // ---------------------------------------------------------------- the Crafted and the Thread Head

        // Drawn new in Spool's colours, facing right, feet on the bottom row. Face pixels (eyes, mouth) are hidden from
        // behind. Stitched: a short, stubby plush with Spool's zipper mouth. Dart: thin and small, a needle held up.
        // Seam: round like a Snoble, white felt with a seam in the plush colour. Mangled: round and jagged, torn cotton with plush
        // patches and odd eyes. Thread Head (24 px): a big ball of thread with Spool's mouth, gloves and a loose end.
        class Doll
        {
            public string[] Rows;
            public int Size, Hips, FaceTop, FaceBottom, Split;
            public char Body;
            public string Face;              // characters hidden from behind, within the face rows
            public bool Thrower;
        }

        static readonly Dictionary<string, Doll> Dolls = new Dictionary<string, Doll>
        {
            {
                "Stitched", new Doll
                {
                    Size = 16, Hips = 13, FaceTop = 8, FaceBottom = 9, Split = 8, Body = 'a', Face = "etgm",
                    Rows = new[]
                    {
                        "................", "................", "................", "................", "................",
                        "................", "......aab.......", ".....aaaabb.....", "....aeaaaeb.....", "....atgtgtb.....",
                        "....aaaaabb.....", "...v.aaaab.vu...", "...uvaaaabtv....", ".....aaaabb.....", ".....ab..ab.....",
                        "....aab..bbb....",
                    },
                }
            },
            {
                "Dart", new Doll
                {
                    Size = 16, Hips = 13, FaceTop = 8, FaceBottom = 9, Split = 8, Body = 'l', Face = "etgm", Thrower = true,
                    Rows = new[]
                    {
                        "................", "................", "................", "................", "................",
                        "........lL......", ".......llL......", "......lllLL.....", "......elleL.....", "......ltgtL..s..",
                        ".......llL...s..", "......vllLL.vs..", ".......llL......", ".......llL......", ".......l.L......",
                        "......ll.LL.....",
                    },
                }
            },
            // Button Jar's small Crafted (Code/02_Data.cs Unit.SmallSheet): the picked Patchwork Stitched and Long Hood Dart a
            // row and a column smaller each, drawn here as there is no file for them (temporary art, docs/TEMP_ART.md).
            {
                "Stitchedsmall", new Doll
                {
                    Size = 16, Hips = 13, FaceTop = 10, FaceBottom = 12, Split = 8, Body = 'a', Face = "tgmeq",
                    Rows = new[]
                    {
                        "................", "................", "................", "................", "................",
                        "................", "................", "................", "................", "......Lab.......",
                        ".....Latgt......", ".....bmeemb.....", ".....aagtq......", "...u.bbbbp.v....", "......b.b.......",
                        "......b.b.......",
                    },
                }
            },
            {
                "Dartsmall", new Doll
                {
                    Size = 16, Hips = 13, FaceTop = 9, FaceBottom = 11, Split = 8, Body = 'l', Face = "tgme", Thrower = true,
                    Rows = new[]
                    {
                        "................", "................", "................", "................", "................",
                        ".........L......", "........L.......", ".......llL......", "......lllLs.....", "......tgtgtS....",
                        "......mememS....", "......gtgtgS....", ".....ulLLLav....", ".......LLa......", ".......b.b......",
                        ".......b.b......",
                    },
                }
            },
            {
                "Seam", new Doll
                {
                    Size = 16, Hips = 13, FaceTop = 9, FaceBottom = 10, Split = 8, Body = 't', Face = "emg",
                    Rows = new[]
                    {
                        "................", "................", "................", "................", "................",
                        "................", "................", "......ttau......", ".....ttbtuu.....", "....tetaeuuu....",
                        "....ttgbguuu....", "....tttatuuu....", "....tttbtuuu....", ".....ttatuu.....", "......u..u......",
                        ".....uu..uu.....",
                    },
                }
            },
            {
                "Mangled", new Doll
                {
                    Size = 16, Hips = 13, FaceTop = 8, FaceBottom = 10, Split = 8, Body = 'v', Face = "emt",
                    Rows = new[]
                    {
                        "................", "................", "................", "................", "................",
                        "......w..w......", ".....wvwvvw.....", "....vaavvvvuw...", "...wvaavvmvuu...", "....vvevtvtuu...",
                        "...vvvvmtmtuuw..", "....vvvvvbbuu...", "...wvvvvvbbuu...", "....vvvuuuuu....", ".....vu..vu.....",
                        "....wvu..uuw....",
                    },
                }
            },
            {
                "Head", new Doll
                {
                    Size = 24, Hips = 19, FaceTop = 9, FaceBottom = 13, Split = 12, Body = 'l', Face = "etgmnbqp",
                    Rows = new[]
                    {
                        "........................", "........................", "........................", "........................",
                        "..........lllL..........", "........llLlllLL........", ".......lLlllLllLL.......", "......llllLlllLlLL......",
                        "......lLlllLllllLL......", ".....llbtgtgtgtgbLL.....", ".....lLmmemmmmemmLL.....", ".....llmmmmmmmmmmLL.....",
                        ".....lLbtgtgtgtqpLL.....", "......llllLlllLqLL......", "......lLlllLllLLLL......", "....v..lllLlllLLL.vu....",
                        "...vu..llLllllLL..tv....", "........lLllLLLL........", ".........llLLLL....l....", ".........llLLLL.....l...",
                        ".........aa..bb.....l...", ".........ab..bb......l..", ".........ab..bb......l..", "........aab..bbb....ll..",
                    },
                }
            },
        };

        static Px DollFrame(Doll d, bool isBack, int bob, int lean, int liftL, int liftR, int arm)
        {
            int n = d.Size;
            var c = new Px(n, n);
            Func<int, int, char> src = (x, y) =>
            {
                char k = d.Rows[y][x];
                if (isBack && y >= d.FaceTop && y <= d.FaceBottom && d.Face.IndexOf(k) >= 0) return d.Body;
                return k;
            };
            // legs: the rows under the hips, the first repeated or dropped to fit
            for (int x = 0; x < n; x++)
            {
                int lift = x < d.Split ? liftL : liftR;
                int top = d.Hips + bob, foot = n - 1 - lift;
                for (int y = top + 1; y < foot; y++) { char k = src(x, d.Hips + 1); if (k != '.') c.Set(x, y, C(k)); }
                char f = src(x, n - 1);
                if (f != '.') c.Set(x, foot, C(f));
            }
            for (int y = 0; y <= d.Hips; y++)
                for (int x = 0; x < n; x++)
                {
                    char k = src(x, y);
                    if (k != '.') c.Set(x + lean, y + bob, C(k));
                }
            // the attack: a swipe (melee) or the needle thrown from the raised hand (the Dart)
            int fx = n == 24 ? 19 : 12, fy = n == 24 ? 14 : 10;
            if (d.Thrower)
            {
                if (arm == 1) { c.Set(13 + lean, 6 + bob, C('s')); c.Set(13 + lean, 5 + bob, C('v')); }
                if (arm == 2) { for (int i = 0; i < 3; i++) c.Set(14 + i + lean, 8 + bob, C(i == 2 ? 'v' : 's')); }
            }
            else
            {
                if (arm == 2) { c.Set(fx + 2 + lean, fy - 1 + bob, C('v')); c.Set(fx + 3 + lean, fy + bob, C('v')); c.Set(fx + 3 + lean, fy + 2 + bob, C('v')); }
                if (arm == 3) { c.Set(fx + 3 + lean, fy + 2 + bob, C('w')); c.Set(fx + 2 + lean, fy + 2 + bob, C('v')); c.Set(fx + 4 + lean, fy + 2 + bob, C('v')); c.Set(fx + 3 + lean, fy + 1 + bob, C('v')); c.Set(fx + 3 + lean, fy + 3 + bob, C('v')); }
            }
            return c;
        }

        // 10 frames per row (Move 0-5, frame 0 doubling as Idle; Attack 6-9), rows v0..v3: 160x64 (the Head 240x96).
        public static Px DollSheet(string name)
        {
            var d = Dolls[name];
            int[] bob = { 0, -1, 0, 0, -1, 0 }, ll = { 0, 1, 1, 0, 0, 0 }, lr = { 0, 0, 0, 0, 1, 1 };
            int[] atkLean = { -1, 1, 2, 1 }, atkBob = { 0, 0, 1, 0 }, atkArm = { 1, 2, 3, 0 };
            var rows = new List<List<Px>>();
            for (int v = 0; v < 4; v++)
            {
                var r = new List<Px>();
                for (int f = 0; f < 6; f++) { var px = DollFrame(d, v >= 2, bob[f], 0, ll[f], lr[f], 0); r.Add(v == 1 || v == 3 ? px.FlipX() : px); }
                for (int f = 0; f < 4; f++) { var px = DollFrame(d, v >= 2, atkBob[f], d.Thrower ? Math.Min(atkLean[f], 1) : atkLean[f], 0, 0, atkArm[f]); r.Add(v == 1 || v == 3 ? px.FlipX() : px); }
                rows.Add(r);
            }
            return Px.Sheet(rows, d.Size, d.Size);
        }

        public static IEnumerable<string> DollNames() { return Dolls.Keys; }

        // ---------------------------------------------------------------- UI art

        public static Px Icon() { return Frame(0, new Pose()); }

        // Journal tab icon, the game's Vesselicons style: a white 1 px line drawing with an open interior. The splash's shapes
        // at a quarter size: the outline of the figure and of the mouth, and the two eyes as dots.
        public static Px TabIcon()
        {
            var c = new Px(32, 32);
            Color32 w = new Color32(255, 255, 255, 255);
            var parts = SplashParts();
            var mouth = SplashMouth();
            Func<int, int, bool> In = (x, y) =>
            {
                if (x < 0 || y < 0 || x >= 32 || y >= 32) return false;
                float sx = x * 4f + 2f, sy = y * 4f + 2f;
                if (mouth.Inside(sx, sy)) return false;
                foreach (var part in parts) if (part.Inside(sx, sy)) return true;
                return false;
            };
            for (int y = 0; y < 32; y++)
                for (int x = 0; x < 32; x++)
                {
                    if (!In(x, y)) continue;
                    bool edge = !In(x - 1, y) || !In(x + 1, y) || !In(x, y - 1) || !In(x, y + 1);
                    if (edge) c.Set(x, y, w);
                }
            c.Set(18, 12, w); c.Set(24, 12, w);
            return c;
        }

        // Happy: both gloves thrown up, the hood flung open in a grin, thread and cotton in the air.
        public static Px EndWin()
        {
            var p = Add(new Pose { bob = -1, gLx = -2, gLy = -7, gRx = 2, gRy = -7, hood = -0.75, tongue = 1 },
                4, 8, FxStar, 27, 6, FxStar, 3, 15, FxThread, 28, 14, FxThreadDark, 14, 6, FxCotton, 24, 11, FxSpark, 6, 20, FxCotton, 29, 20, FxThread);
            return Frame(0, p);
        }

        // Defeated: slumped, the hood shut over the eyes, the gloves hanging; greyed.
        public static Px EndLose()
        {
            var p = new Pose { bob = 1, hood = 1, gLy = 2, gRy = 1 };
            var f = Frame(0, p);
            var d = new Px(32, 32);
            for (int i = 0; i < f.C.Length; i++) { var c = f.C[i]; d.C[i] = Col.Mix(c, Col.A(Col.H("8A867E"), c.a), 0.4f); }
            return d;
        }

        // 12x12 ready-up icon: the face (the hood, the zipper mouth, two eyes, the tongue) in white corner brackets.
        public static Px ReadyUp()
        {
            var rows = new[]
            {
                "............",
                ".WW....ab.W.",
                ".W...aaaabb.",
                "....aabtgtb.",
                "...abtmmmmg.",
                "...bmmemmen.",
                "...tmmmmmmm.",
                "...bgtgtgqp.",
                "....aabbbqb.",
                ".W.......p..",
                ".WW......WW.",
                "............",
            };
            var pal = new Dictionary<char, Color32>(P) { { 'W', new Color32(255, 255, 255, 255) } };
            return Px.FromRows(rows, pal);
        }

        // ---------------------------------------------------------------- splash (128x128 line art)

        // The shipped splashes are line art: a two-tone frame, then the vessel's shapes filled with a translucent accent,
        // stroked outside in the solid accent and traced 3 px inside with the body's own colours. Accent: the eyes' yellow.
        // Shapes follow the reshade at 5x (one shape per body part); the mouth is filled dark, with the eyes in it.
        public static Px Splash()
        {
            const int S = 128;
            Color32 accent = C('e');
            Color32 fill = Col.A(accent, 70);
            var c = new Px(S, S);
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    int d = Mathf.Min(Mathf.Min(x, y), Mathf.Min(S - 1 - x, S - 1 - y));
                    if (d < 2) c.Set(x, y, fill);
                    else if (d < 4) c.Set(x, y, accent);
                }
            foreach (var part in SplashParts()) part.Draw(c, accent, fill);
            // the mouth: dark, stroked, with the teeth along its edges and the two eyes inside
            var mouth = SplashMouth();
            mouth.Draw(c, accent, C('m'), false);
            for (int x = 52; x < 104; x++)
                for (int y = 30; y < 64; y++)
                {
                    if (!mouth.Inside(x + 0.5f, y + 0.5f)) continue;
                    bool top = !mouth.Inside(x + 0.5f, y - 3.5f), bottom = !mouth.Inside(x + 0.5f, y + 4.5f);
                    if ((top || bottom) && (x / 5) % 2 == 0) c.Set(x, y, C('t'));
                }
            SplashTongue().Draw(c, accent, fill);
            Color32 eye = C('e');
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float a = (x + 0.5f - 74f) / 3.5f, b = (y + 0.5f - 47f) / 3.5f, e2 = (x + 0.5f - 99f) / 3.5f, f2 = (y + 0.5f - 47f) / 3.5f;
                    if (a * a + b * b <= 1f || e2 * e2 + f2 * f2 <= 1f) c.Set(x, y, eye);
                }
            return c;
        }

        // The splash's shapes (128 px space: the reshade's pixel (x, y) sits at 5x - 8.5, 5y - 48.5), shared with the
        // journal tab icon.
        static List<SplashPart> SplashParts()
        {
            Color32 plush = C('a'), shade = C('b'), glove = C('v'), gloveShade = C('u');
            return new List<SplashPart>
            {
                // the severed arm on the floor, its cotton spilling out
                SplashPart.Capsule(new Vector2(47, 100), new Vector2(31, 107), 5.5f, plush, shade),
                SplashPart.Ellipse(new Vector2(22, 106), 6, 5, 0f, glove, gloveShade),
                // legs
                SplashPart.Capsule(new Vector2(60, 96), new Vector2(60, 110), 5.5f, plush, shade),
                SplashPart.Capsule(new Vector2(88, 96), new Vector2(89, 110), 5.5f, plush, shade),
                // gloves, then the body over their inner edge
                SplashPart.Ellipse(new Vector2(40, 77), 7, 7, 0f, glove, gloveShade),
                SplashPart.Ellipse(new Vector2(97, 83), 7, 7, 0f, glove, gloveShade),
                SplashPart.Ellipse(new Vector2(67, 82), 25, 17, 0f, plush, shade),
                // the hood's tip, then the hood over it
                SplashPart.Ellipse(new Vector2(88, 22), 10, 7, -25f, plush, shade),
                SplashPart.Ellipse(new Vector2(76, 43), 33, 26, -8f, plush, shade),
            };
        }

        static SplashPart SplashTongue() { return SplashPart.Capsule(new Vector2(98, 61), new Vector2(97, 73), 4f, C('q'), C('p')); }

        static SplashPart SplashMouth() { return SplashPart.Ellipse(new Vector2(78, 47), 27, 10, 0f, C('t'), C('g')); }

        class SplashPart
        {
            public Func<float, float, bool> Inside;
            public Color32 Line, Shade;
            public float ShadeFromX;

            public static SplashPart Ellipse(Vector2 o, float rx, float ry, float deg, Color32 line, Color32 shade)
            {
                float cs = Mathf.Cos(deg * Mathf.Deg2Rad), sn = Mathf.Sin(deg * Mathf.Deg2Rad);
                return new SplashPart
                {
                    Inside = (x, y) => { float dx = x - o.x, dy = y - o.y; float u = dx * cs + dy * sn, v = -dx * sn + dy * cs; return u * u / (rx * rx) + v * v / (ry * ry) <= 1f; },
                    Line = line, Shade = shade, ShadeFromX = o.x + rx * 0.45f
                };
            }

            public static SplashPart Capsule(Vector2 a, Vector2 b, float r, Color32 line, Color32 shade)
            {
                return new SplashPart
                {
                    Inside = (x, y) =>
                    {
                        Vector2 p = new Vector2(x, y), ab = b - a;
                        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
                        return (a + ab * t - p).magnitude <= r;
                    },
                    Line = line, Shade = shade, ShadeFromX = (a.x + b.x) * 0.5f + r * 0.3f
                };
            }

            // Clears what lies behind (inside and the 3 px stroke), then strokes outside and traces 3 px inside.
            public void Draw(Px c, Color32 accent, Color32 fill, bool trace = true)
            {
                int S = c.W;
                var inside = new bool[S, S];
                for (int y = 0; y < S; y++) for (int x = 0; x < S; x++) inside[x, y] = Inside(x + 0.5f, y + 0.5f);
                int[,] dIn = Dist(inside, true, S), dOut = Dist(inside, false, S);
                for (int y = 4; y < S - 4; y++)
                    for (int x = 4; x < S - 4; x++)
                    {
                        if (inside[x, y])
                        {
                            int d = dIn[x, y];
                            c.Set(x, y, trace && d >= 4 && d <= 6 ? (x >= ShadeFromX ? Shade : Line) : fill);
                        }
                        else if (dOut[x, y] <= 3) c.Set(x, y, accent);
                    }
            }

            static int[,] Dist(bool[,] inside, bool forInside, int S)
            {
                var d = new int[S, S];
                var q = new Queue<Vector2Int>();
                for (int y = 0; y < S; y++)
                    for (int x = 0; x < S; x++)
                    {
                        if (inside[x, y] != forInside) { d[x, y] = 0; q.Enqueue(new Vector2Int(x, y)); }
                        else d[x, y] = int.MaxValue;
                    }
                while (q.Count > 0)
                {
                    var p = q.Dequeue();
                    for (int oy = -1; oy <= 1; oy++)
                        for (int ox = -1; ox <= 1; ox++)
                        {
                            int nx = p.x + ox, ny = p.y + oy;
                            if (nx < 0 || ny < 0 || nx >= S || ny >= S || d[nx, ny] <= d[p.x, p.y] + 1) continue;
                            d[nx, ny] = d[p.x, p.y] + 1;
                            q.Enqueue(new Vector2Int(nx, ny));
                        }
                }
                return d;
            }
        }
    }
}
