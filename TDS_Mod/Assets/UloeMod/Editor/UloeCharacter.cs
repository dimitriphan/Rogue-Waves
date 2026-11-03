// Uloe asset generator: the vessel's pixel art. TEMPORARY ART built from Dimitri's own sprite.
// Baseline: his concept frame (docs/concepts/art/uloe.ase), embedded below pixel for pixel as Front. Every 32 px frame is
// those pixels moved, with pixels added for the back view's filament, the electric arms' poses, the bulb burning, sparks and
// dust. The 16 px boat mini and the Photon are drawn new in his colours. Finish: fully opaque, shade on the right, no rim, no
// ground patch. Reference base vessel: Sno Sno (an object come alive; legless, it hops).
// Uloe: a glass light bulb head with a face for a filament, a ribbed screw base on a tall lamp post, yellow floating arms made
// of electricity at the sides, a cord to a plug. The plug stays on the floor while the lamp hops.
// 32x32 frames, PPU 20, bottom-centre pivot. Variants: v0 front facing right, v1 front mirrored, v2 back, v3 back mirrored.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace UloeBuild
{
    public class Pose
    {
        public int hop;                               // the whole lamp off the floor, pixels up (the plug stays down)
        public int bob;                               // the bulb and screw on the post: + sinks them, - stretches the post
        public int armL, armR;                        // 0 hanging as drawn, 1 level, 2 raised
        public int lx, ly, rx, ry;                    // arm offsets
        public bool zapL, zapR;                       // a spark at the arm's tip
        public bool blink, lit, flash;                // eyes shut; the bulb burning; every glint white
        public bool air;                              // the plug rides with the lamp, trailing above it
        public int plugX = -99, plugY = -99;          // the plug held elsewhere (the ability)
        public readonly List<Vector3Int> fx = new List<Vector3Int>();
    }

    public static class UloeCharacter
    {
        public const int FxDust = 1, FxSpark = 2, FxSparkDim = 3, FxStar = 4, FxBolt = 5, FxGlass = 6;

        // The concept's own colours, then the colours the frames add.
        public static readonly Dictionary<char, Color32> P = new Dictionary<char, Color32>
        {
            { 'C', Col.H("DAE6FC") }, { 'D', Col.H("CBDBFC") }, { 'I', Col.H("BFCDE9") }, { 'J', Col.H("979FAD") },    // glass, its shade
            { 'E', Col.H("FFFFFF") }, { 'F', Col.H("F1EF72") },                                                         // glints
            { 'G', Col.H("3F3F74") }, { 'H', Col.H("323C39") },                                                         // eyes, the filament's U
            { 'K', Col.H("9BADB7") }, { 'L', Col.H("9AA0A4") }, { 'M', Col.H("757A7D") },                               // screw rings, prongs
            { 'N', Col.H("696A6A") }, { 'O', Col.H("595652") }, { 'P', Col.H("60605E") }, { 'Q', Col.H("5D5C59") }, { 'W', Col.H("6F7374") },   // the post
            { 'R', Col.H("FBF236") }, { 'S', Col.H("D4CD3C") },                                                         // electric arms
            { 'T', Col.H("222222") }, { 'V', Col.H("2E2D2D") }, { 'U', Col.H("96A6AE") },                               // cord, plug
            // added: the bulb burning, a spark's heart, dust
            { 'a', Col.H("FFFDE0") }, { 'b', Col.H("FFF48B") }, { 'c', Col.H("F1EF72") }, { 'd', Col.H("D4CD3C") },
            { 'x', Col.H("FFFDE0") }, { 'k', Col.H("BDB3A8") },
        };

        static Color32 C(char k) { return P[k]; }

        // Dimitri's concept frame. Rows 11-18 the bulb (glints, the arched eyes, the filament's U), 19-22 the ribbed screw base,
        // 23-31 the post and its foot; the arms and the cord with its plug are cut out below.
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
            "............CCDDDDDD............",
            "...........CCDDDDDDDD...........",
            "...........CEFDDDDDEF...........",
            "...........DFFGDDGDFF...........",
            "...........DDGDGGDGDD...........",
            "...........DDHDDDDHDD...........",
            "...........DDHDDDDHDI...........",
            "...........DDDHDDHDIJ...........",
            "............DKKKKKLJ............",
            ".............MNNNNO.............",
            ".............KKKKKL.............",
            ".............MNNNPO.............",
            "..............NNQO..............",
            "...........RS.NNQO.RS...........",
            "..........RS..NNQO..RS..........",
            "........RRS...NNQO...RRS........",
            "......RRS.....NNQO.....RRS......",
            ".............TNNQO..............",
            "....KUVT.....TNNQO..............",
            "......VVT...MWNNNQOO............",
            "....KUVT.TTMWNNNNNQOO...........",
        };

        const int W = 32;
        const int HeadTop = 11, HeadBottom = 18, ScrewBottom = 22, FootRow = 31;

        static bool IsArm(char k) { return k == 'R' || k == 'S'; }
        static bool IsCord(char k, int x) { return x <= 13 && (k == 'T' || k == 'V' || (k == 'U' && x <= 6) || (k == 'K' && x <= 6)); }

        // The bulb burning: the glass goes warm, the filament's U lights, the eyes stay dark.
        static char Burn(char k)
        {
            switch (k)
            {
                case 'C': return 'a';
                case 'D': return 'b';
                case 'I': return 'c';
                case 'J': return 'd';
                case 'F': return 'E';
                case 'H': return 'R';
                default: return k;
            }
        }

        // ---------------------------------------------------------------- frame composer

        public static Px Frame(int variant, Pose p)
        {
            bool back = variant >= 2;
            var c = new Px(W, W);
            int up = -p.hop;

            // The cord and plug on the floor (behind, on the far side from behind).
            Cord(c, p, back);

            // The arms float at the sides.
            Arm(c, true, p.armL, p.lx, p.ly + up, p.zapL);
            Arm(c, false, p.armR, p.rx, p.ry + up, p.zapR);

            // The post with its foot; a stretched post repeats its top row in the gap.
            for (int y = ScrewBottom + 1; y < W; y++)
                for (int x = 0; x < W; x++)
                {
                    char k = Front[y][x];
                    if (k == '.' || IsArm(k) || IsCord(k, x)) continue;
                    c.Set(x, y + up, C(k));
                }
            for (int g = p.bob; g < 0; g++)
                for (int x = 14; x <= 17; x++)
                    c.Set(x, ScrewBottom + 1 + g + up, C(Front[ScrewBottom + 1][x]));
            // The cord's end runs up the post on the near side.
            if (!back)
                for (int y = 28; y <= 29; y++) c.Set(13, y + up, C('T'));

            // The screw and the bulb ride on the post.
            for (int y = ScrewBottom; y >= HeadTop; y--)
                for (int x = 0; x < W; x++)
                {
                    char k = Front[y][x];
                    if (k == '.') continue;
                    if (back && (k == 'G' || k == 'H')) k = 'D';
                    if (p.blink && !back && y == 14 && k == 'G') k = 'D';
                    if (p.lit) k = Burn(k);
                    if (p.flash && k == 'F') k = 'E';
                    c.Set(x, y + p.bob + up, C(k));
                }
            // Blinking, the arches drop into a line.
            if (p.blink && !back)
                foreach (int x in new[] { 14, 17 }) c.Set(x, 15 + p.bob + up, C('G'));
            // From behind, the filament's stem runs up from the screw.
            if (back)
                for (int y = 16; y <= HeadBottom; y++)
                {
                    c.Set(15, y + p.bob + up, C(p.lit ? 'R' : 'K'));
                    c.Set(16, y + p.bob + up, C(p.lit ? 'S' : 'L'));
                }

            foreach (var f in p.fx) Fx(c, f, back);
            return variant == 1 || variant == 3 ? c.FlipX() : c;
        }

        // The plug and cord. On the floor it stays put and the cord runs to the foot; in the air it trails above; held, it goes
        // where the ability puts it. From behind it lies on the other side.
        static void Cord(Px c, Pose p, bool back)
        {
            int footX = back ? 20 : 11, footY = FootRow - p.hop;
            int px = 4, py = 29;
            if (p.air) { px = 5; py = Mathf.Max(6, 16 - p.hop); }
            if (p.plugX != -99) { px = p.plugX; py = p.plugY; }
            // One pixel out of the plug's back, then a run to the foot (at rest, exactly the concept's cord).
            Plug(c, px, py, back);
            int sx = back ? 31 - (px + 4) : px + 4, step = back ? -1 : 1;
            c.Set(sx, py + 1, C('T'));
            Line(c, sx + step, py + 2, footX, footY, 'T');
        }

        // The plug: a 4 x 3 block whose two prongs point outward. (px, py) is its top-left on the near side's layout.
        static void Plug(Px c, int px, int py, bool back)
        {
            Action<int, int, char> put = (x, y, k) => c.Set(back ? 31 - (px + x) : px + x, py + y, C(k));
            put(0, 0, 'K'); put(1, 0, 'U'); put(2, 0, 'V'); put(3, 0, 'T');
            put(2, 1, 'V'); put(3, 1, 'V');
            put(0, 2, 'K'); put(1, 2, 'U'); put(2, 2, 'V'); put(3, 2, 'T');
        }

        // An arm of electricity: runs of the bright yellow with the shade at the outer end of each row, as drawn in the concept.
        static void Arm(Px c, bool left, int pose, int dx, int dy, bool zap)
        {
            // Rows of (y, x of the first R, how many R) for the left arm; the right arm mirrors about the post.
            int[][] rows;
            if (pose == 1)
                rows = new[] { new[] { 24, 11, 1 }, new[] { 25, 8, 3 }, new[] { 25, 5, 3 } };
            else if (pose == 2)
                rows = new[] { new[] { 24, 11, 1 }, new[] { 23, 10, 1 }, new[] { 22, 8, 2 }, new[] { 21, 6, 2 } };
            else
                rows = new[] { new[] { 24, 11, 1 }, new[] { 25, 10, 1 }, new[] { 26, 8, 2 }, new[] { 27, 6, 2 } };
            int tipX = 0, tipY = 0;
            foreach (var r in rows)
            {
                int y = r[0] + dy, x0 = r[1], n = r[2];
                if (left)
                {
                    for (int i = 0; i < n; i++) c.Set(x0 + i + dx, y, C('R'));
                    c.Set(x0 + n + dx, y, C('S'));
                    tipX = x0 + dx;
                }
                else
                {
                    // Mirrored about column 15.5 would put the shade on the inside: the concept's right arm keeps it outside.
                    int rx = 31 - (x0 + n);
                    for (int i = 0; i < n; i++) c.Set(rx + i + dx, y, C('R'));
                    c.Set(rx + n + dx, y, C('S'));
                    tipX = rx + n + dx;
                }
                tipY = y;
            }
            if (zap)
            {
                c.Set(tipX, tipY, C('x'));
                c.Set(tipX + (left ? -1 : 1), tipY, C('R'));
                c.Set(tipX, tipY + (pose == 2 ? -1 : 1), C('R'));
            }
        }

        static void Line(Px c, int x0, int y0, int x1, int y1, char k)
        {
            int n = Math.Max(Math.Max(Math.Abs(x1 - x0), Math.Abs(y1 - y0)), 1);
            for (int i = 0; i <= n; i++)
                c.Set((int)Math.Round(x0 + (x1 - x0) * i / (double)n), (int)Math.Round(y0 + (y1 - y0) * i / (double)n), C(k));
        }

        static void Fx(Px c, Vector3Int f, bool back)
        {
            switch (f.z)
            {
                case FxDust: c.Set(f.x, f.y, C('k')); break;
                case FxSpark: c.Set(f.x, f.y, C('R')); break;
                case FxSparkDim: c.Set(f.x, f.y, C('S')); break;
                case FxGlass: c.Set(f.x, f.y, C('C')); break;
                case FxStar: c.Set(f.x, f.y, C('x')); c.Set(f.x - 1, f.y, C('R')); c.Set(f.x + 1, f.y, C('R')); c.Set(f.x, f.y - 1, C('R')); c.Set(f.x, f.y + 1, C('R')); break;
                case FxBolt:
                    c.Set(f.x, f.y, C('x')); c.Set(f.x + 1, f.y, C('x')); c.Set(f.x + 2, f.y - 1, C('R')); c.Set(f.x + 1, f.y + 1, C('R')); c.Set(f.x - 1, f.y, C('S'));
                    break;
            }
        }

        // ---------------------------------------------------------------- animations

        static Pose Add(Pose p, params int[] xyz)
        {
            for (int i = 0; i + 2 < xyz.Length; i += 3) p.fx.Add(new Vector3Int(xyz[i], xyz[i + 1], xyz[i + 2]));
            return p;
        }

        // A lamp breathing: the arms crackle up and down in turn, the bulb settles a pixel, one blink.
        public static List<Pose> Idle(bool lit = false)
        {
            return new List<Pose>
            {
                new Pose { lit = lit },
                new Pose { lit = lit, ly = -1, zapL = true },
                new Pose { lit = lit, bob = 1 },
                new Pose { lit = lit, bob = 1, blink = true },
                new Pose { lit = lit, ry = -1, zapR = true },
                new Pose { lit = lit, flash = true },
            };
        }

        // The hop (Sno Sno's way of getting about, legless): down on the screw, up off the foot, arms flung up at the top, down
        // hard on frame 4 with a puff, and the plug stays on the floor with the cord stretching.
        public static List<Pose> Move(bool lit = false)
        {
            return new List<Pose>
            {
                new Pose { lit = lit, bob = 1, armL = 1, armR = 1 },
                new Pose { lit = lit, hop = 2, bob = -1, ly = 1, ry = 1 },
                new Pose { lit = lit, hop = 4, bob = -1, armL = 2, armR = 2, zapL = true },
                new Pose { lit = lit, hop = 3, armL = 1, armR = 1, zapR = true },
                Add(new Pose { lit = lit, bob = 2, ly = 1, ry = 1 }, 9, 31, FxDust, 22, 31, FxDust),
                new Pose { lit = lit, bob = 1 },
            };
        }

        // The spark: both arms up and crackling, the near arm flings forward with a bolt, the lamp rocks back.
        public static List<Pose> Attack()
        {
            var wind = new Pose { bob = -1, armL = 2, armR = 2, zapL = true, zapR = true, flash = true };
            var throwit = Add(new Pose { lit = true, armL = 2, armR = 1, rx = 3, ry = -1, zapR = true }, 28, 23, FxBolt);
            var after = Add(new Pose { lit = true, bob = 1, armR = 1, rx = 1 }, 30, 22, FxSpark, 27, 24, FxSparkDim);
            var back = new Pose { flash = true };
            return new List<Pose> { wind, throwit, after, back };
        }

        // PLUG IN: an arm lifts the plug, thrusts it into the floor with a spark, and the bulb flares and stays lit.
        public static List<Pose> PlugIn()
        {
            var lift = new Pose { armL = 2, plugX = 3, plugY = 18, zapL = true };
            var hold = new Pose { armL = 1, plugX = 2, plugY = 23, ly = -1, bob = -1 };
            var drive = Add(new Pose { plugX = 3, plugY = 28, bob = 1 }, 3, 27, FxStar);
            var flare = Add(new Pose { lit = true, armL = 2, armR = 2, zapL = true, zapR = true, flash = true }, 7, 13, FxSpark, 24, 12, FxSpark, 16, 8, FxSpark);
            var glow = Add(new Pose { lit = true, armL = 1, armR = 1, zapR = true }, 8, 15, FxSparkDim, 23, 14, FxSparkDim);
            var rest = new Pose { lit = true };
            return new List<Pose> { lift, hold, drive, flare, glow, rest };
        }

        // Loading screen: falling, the arms flapping up, the cord streaming above with its plug, sparks going by.
        public static List<Px> Fall()
        {
            var frames = new List<Px>();
            for (int f = 0; f < 12; f++)
            {
                bool hi = f % 4 < 2;
                var p = new Pose
                {
                    hop = 2, air = true, armL = 2, armR = 2, ly = hi ? -1 : 0, ry = hi ? 0 : -1, zapL = f % 3 == 0, zapR = f % 3 == 1, flash = f % 6 == 0,
                    blink = f % 12 == 5,
                };
                Add(p, 25 - (f * 3) % 5, 31 - (f * 3) % 30, FxSpark, 4 + (f * 5) % 6, 31 - ((f * 3 + 15) % 30), FxSparkDim, 29, 31 - ((f * 3 + 7) % 30), FxSparkDim);
                frames.Add(Frame(0, p));
            }
            return frames;
        }

        // Landing: drops in, lands hard on its foot, the plug rattles down beside it, then the bulb flickers on and off and it
        // looks about.
        public static List<Px> Land()
        {
            var poses = new List<Pose>
            {
                new Pose { hop = 14, air = true, armL = 2, armR = 2 },
                new Pose { hop = 6, air = true, armL = 2, armR = 2 },
                Dust(new Pose { bob = 3, armL = 1, armR = 1 }, 0),
                Dust(new Pose { bob = 2, ly = 1, ry = 1 }, 1),
                Dust(new Pose { bob = 1 }, 2),
                new Pose(),
                new Pose { lit = true, zapL = true },
                new Pose { blink = true },
                new Pose { lit = true, zapR = true, flash = true },
                new Pose(),
                new Pose { ly = -1, zapL = true },
                new Pose(),
            };
            var frames = new List<Px>();
            foreach (var p in poses) frames.Add(Frame(0, p));
            return frames;
        }

        static Pose Dust(Pose p, int t)
        {
            int[,] bits = { { 9, 31 }, { 22, 31 }, { 8, 30 }, { 23, 30 }, { 10, 29 }, { 21, 29 } };
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

        // Idle rows v0..v3 then Move rows v0..v3, 6 frames each: 192x256. 'lit' is the skin worn while plugged in.
        public static Px MainSheet(bool lit = false)
        {
            var rows = new List<List<Px>>();
            var idle = Idle(lit);
            var move = Move(lit);
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

        // The concept at boat size (7 x 12): the bulb with its arched eyes and filament, the screw, the post and foot, the arms and
        // the plug.
        static readonly string[] MiniFront =
        {
            "................",
            "................",
            "................",
            "................",
            ".....CDDDD......",
            ".....FDDDFD.....",
            ".....DGDGDI.....",
            ".....DDHHDJ.....",
            "......KKKL......",
            ".......NQ.......",
            "...RS..NQ..RS...",
            "..RS...NQ...RS..",
            ".......NQ.......",
            ".......NQ.......",
            ".KU...MNQO......",
            ".KUTTTMNNQO.....",
        };

        static Px Mini(bool isBack, int hop, int bob, bool blink, bool lit, int arm)
        {
            var c = new Px(16, 16);
            for (int y = 0; y < 16; y++)
                for (int x = 0; x < 16; x++)
                {
                    char k = MiniFront[y][x];
                    if (k == '.') continue;
                    int px = x, py = y;
                    bool plug = x <= 5 && y >= 14;
                    bool arms = IsArm(k);
                    if (plug) { if (isBack) px = 15 - x; }
                    else
                    {
                        py -= hop;
                        if (y <= 8) py += bob;
                        if (arms) py += arm;
                    }
                    if (isBack && (k == 'G' || k == 'H')) k = 'D';
                    if (blink && k == 'G') k = 'D';
                    if (lit) k = Burn(k);
                    c.Set(px, py, C(k));
                }
            if (isBack)
            {
                c.Set(7, 7 + bob - hop, C(lit ? 'R' : 'K'));
                c.Set(8, 7 + bob - hop, C(lit ? 'S' : 'L'));
            }
            return c;
        }

        // 96x128: Idle v0..v3 (rows 0-3) then Move v0..v3 (rows 4-7), 6 frames of 16x16.
        public static Px MiniSheet()
        {
            var rows = new List<List<Px>>();
            int[] idleBob = { 0, 0, 1, 1, 0, 0 }, idleArm = { 0, -1, 0, 0, 1, 0 };
            bool[] blink = { false, false, false, true, false, false };
            int[] hop = { 0, 1, 2, 2, 0, 0 }, moveBob = { 1, 0, 0, 0, 1, 0 }, moveArm = { 0, 1, -1, -1, 1, 0 };
            for (int v = 0; v < 4; v++)
            {
                var r = new List<Px>();
                for (int f = 0; f < 6; f++) { var m = Mini(v >= 2, 0, idleBob[f], blink[f], false, idleArm[f]); r.Add(v == 1 || v == 3 ? m.FlipX() : m); }
                rows.Add(r);
            }
            for (int v = 0; v < 4; v++)
            {
                var r = new List<Px>();
                for (int f = 0; f < 6; f++) { var m = Mini(v >= 2, hop[f], moveBob[f], false, false, moveArm[f]); r.Add(v == 1 || v == 3 ? m.FlipX() : m); }
                rows.Add(r);
            }
            return Px.Sheet(rows, 16, 16);
        }

        // ---------------------------------------------------------------- the Photon

        // A loose bit of light: a round spark with dark dot eyes, an arc for a tail and two wisps of arm. It floats: the tail touches
        // the bottom row and the body bobs over it. Light from the upper left, shade right and below.
        static readonly string[] PhotonRows =
        {
            "................",
            "................",
            "................",
            "................",
            "................",
            "................",
            "......aabb......",
            ".....aabbbc.....",
            "....Rabbbbcc.S..",
            "...R.abGbGcc.R..",
            ".....abbbbcc..S.",
            ".....bbbbccd....",
            "......bcccd.....",
            ".......RS.......",
            "........RS......",
            ".......RS.......",
        };

        static Px PhotonFrame(bool isBack, int bob, int arm, int zap)
        {
            var c = new Px(16, 16);
            for (int y = 0; y < 16; y++)
                for (int x = 0; x < 16; x++)
                {
                    char k = PhotonRows[y][x];
                    if (k == '.') continue;
                    if (isBack && k == 'G') k = 'b';
                    bool tail = y >= 13;
                    bool wisp = (k == 'R' || k == 'S') && !tail;
                    int py = y + (tail ? 0 : bob) + (wisp ? arm : 0);
                    // The tail flickers from side to side.
                    int px = tail ? x + ((bob + y) % 2 == 0 ? 0 : -1) : x;
                    c.Set(px, py, C(k));
                }
            if (zap == 1) { c.Set(13, 8 + bob, C('x')); c.Set(14, 7 + bob, C('R')); }
            if (zap == 2) { for (int i = 0; i < 3; i++) c.Set(12 + i, 9 + bob + (i % 2), C(i == 1 ? 'x' : 'R')); }
            if (zap == 3) { c.Set(14, 9 + bob, C('x')); c.Set(13, 9 + bob, C('R')); c.Set(15, 9 + bob, C('R')); c.Set(14, 8 + bob, C('R')); c.Set(14, 10 + bob, C('R')); }
            return c;
        }

        // 10 frames per row (Move 0-5, frame 0 doubling as Idle; Attack 6-9), rows v0..v3: 160x64.
        public static Px PhotonSheet()
        {
            int[] bob = { 0, -1, -2, -2, -1, 0 }, arm = { 0, 1, 0, -1, 0, 1 };
            int[] atkBob = { -1, 0, 0, -1 }, atkZap = { 1, 2, 3, 0 };
            var rows = new List<List<Px>>();
            for (int v = 0; v < 4; v++)
            {
                var r = new List<Px>();
                for (int f = 0; f < 6; f++) { var px = PhotonFrame(v >= 2, bob[f], arm[f], 0); r.Add(v == 1 || v == 3 ? px.FlipX() : px); }
                for (int f = 0; f < 4; f++) { var px = PhotonFrame(v >= 2, atkBob[f], 0, atkZap[f]); r.Add(v == 1 || v == 3 ? px.FlipX() : px); }
                rows.Add(r);
            }
            return Px.Sheet(rows, 16, 16);
        }

        // ---------------------------------------------------------------- UI art

        public static Px Icon() { return Frame(0, new Pose()); }

        // Journal tab icon, the game's Vesselicons style: a white 1 px line drawing with an open interior. The splash's shapes at a
        // quarter size, with the two eyes as dots.
        public static Px TabIcon()
        {
            var c = new Px(32, 32);
            Color32 w = new Color32(255, 255, 255, 255);
            var parts = SplashParts();
            Func<int, int, bool> In = (x, y) =>
            {
                if (x < 0 || y < 0 || x >= 32 || y >= 32) return false;
                float sx = x * 4f + 2f, sy = y * 4f + 2f;
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
            c.Set(13, 9, w); c.Set(18, 9, w);
            return c;
        }

        // Happy: the bulb blazing, both arms thrown up, sparks in the air.
        public static Px EndWin()
        {
            var p = Add(new Pose { hop = 1, bob = -1, lit = true, flash = true, armL = 2, armR = 2, zapL = true, zapR = true },
                4, 8, FxStar, 27, 6, FxStar, 3, 15, FxSpark, 28, 14, FxSparkDim, 15, 5, FxSpark, 24, 11, FxSpark, 6, 20, FxSparkDim, 29, 20, FxSpark);
            return Frame(0, p);
        }

        // Defeated: burned out, sunk on its post, the arms hanging dead; greyed.
        public static Px EndLose()
        {
            var p = Add(new Pose { bob = 2, blink = true, ly = 2, ry = 2 }, 8, 31, FxDust, 22, 31, FxDust);
            var f = Frame(0, p);
            var d = new Px(32, 32);
            for (int i = 0; i < f.C.Length; i++) { var c = f.C[i]; d.C[i] = Col.Mix(c, Col.A(Col.H("8A867E"), c.a), 0.45f); }
            return d;
        }

        // 12x12 ready-up icon: the bulb with its face in white corner brackets.
        public static Px ReadyUp()
        {
            var rows = new[]
            {
                "............",
                ".##.CDDD.##.",
                ".#.CFDDDF.#.",
                "...DGDDGDI..",
                "...DDDDDDI..",
                "...DHDDHDJ..",
                "...DDHHDIJ..",
                "....KKKKL...",
                ".....NNO....",
                ".#...NNO..#.",
                ".##.MNNO.##.",
                "............",
            };
            var pal = new Dictionary<char, Color32>(P) { { '#', new Color32(255, 255, 255, 255) } };
            return Px.FromRows(rows, pal);
        }

        // ---------------------------------------------------------------- splash (128x128 line art)

        // The shipped splashes are line art: a two-tone frame, then the vessel's shapes filled with a translucent accent, stroked
        // outside in the solid accent and traced 3 px inside with the body's own colours. Accent: the arms' yellow.
        public static Px Splash()
        {
            const int S = 128;
            Color32 accent = C('R');
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
            // The face: two arched eyes and the filament's U, solid.
            Color32 eye = C('G'), wire = C('H');
            foreach (var seg in new[]
            {
                new[] { 48f, 44f, 53f, 38f }, new[] { 53f, 38f, 58f, 44f },
                new[] { 70f, 44f, 75f, 38f }, new[] { 75f, 38f, 80f, 44f },
            })
                Solid(c, SplashPart.Capsule(new Vector2(seg[0], seg[1]), new Vector2(seg[2], seg[3]), 2f, eye, eye), eye);
            foreach (var seg in new[]
            {
                new[] { 50f, 52f, 50f, 61f }, new[] { 50f, 61f, 56f, 68f },
                new[] { 78f, 52f, 78f, 61f }, new[] { 78f, 61f, 72f, 68f },
            })
                Solid(c, SplashPart.Capsule(new Vector2(seg[0], seg[1]), new Vector2(seg[2], seg[3]), 1.7f, wire, wire), wire);
            return c;
        }

        static void Solid(Px c, SplashPart part, Color32 k)
        {
            for (int y = 4; y < c.H - 4; y++)
                for (int x = 4; x < c.W - 4; x++)
                    if (part.Inside(x + 0.5f, y + 0.5f)) c.Set(x, y, k);
        }

        // The splash's shapes (128 px space), shared with the journal tab icon. Later parts draw over earlier ones.
        static List<SplashPart> SplashParts()
        {
            Color32 glass = C('C'), glassShade = C('I'), steel = C('K'), steelShade = C('M'), post = C('N'), postShade = C('O'), arm = C('R'), armShade = C('S'), plug = C('U'), plugShade = C('V');
            return new List<SplashPart>
            {
                // the plug and its cord on the floor
                SplashPart.Capsule(new Vector2(30, 112), new Vector2(48, 112), 2.5f, plugShade, plugShade),
                SplashPart.Ellipse(new Vector2(24, 112), 8, 6, 0f, plug, plugShade),
                // the arms of electricity, floating
                SplashPart.Capsule(new Vector2(44, 84), new Vector2(26, 100), 4f, arm, armShade),
                SplashPart.Capsule(new Vector2(84, 84), new Vector2(102, 100), 4f, arm, armShade),
                // the foot, the post
                SplashPart.Ellipse(new Vector2(64, 112), 21, 7, 0f, post, postShade),
                SplashPart.Capsule(new Vector2(64, 80), new Vector2(64, 108), 8f, post, postShade),
                // the screw rings
                SplashPart.Capsule(new Vector2(51, 82), new Vector2(77, 82), 4f, steel, steelShade),
                SplashPart.Capsule(new Vector2(51, 73), new Vector2(77, 73), 4.5f, steel, steelShade),
                // the bulb
                SplashPart.Ellipse(new Vector2(64, 42), 29, 30, 0f, glass, glassShade),
            };
        }

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
