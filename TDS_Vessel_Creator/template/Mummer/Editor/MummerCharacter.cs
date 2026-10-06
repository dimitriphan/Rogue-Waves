// Mummer asset generator: the vessel's pixel art, drawn with Sno Sno's rendering rules (the style the shipped vessels
// share): one main colour, a shade on the side the vessel faces, a deeper band at the bottom, a half-alpha rim on the
// lit upper-left edge, a soft ground patch, no outlines; it hops to move.
// Mummer is a costume: a soft lilac triangle (Smacky is the square, Sno Sno the circle) with a floppy point and a brass
// bell, a happy face drawn on the front (crooked eyes - the far one a pixel low - blush and a zigzag grin) and a hem cut
// with pinking shears. Under the hem is the dark, and two small white eyes in it. Its hands are floating shadows with
// pale claws; from behind there is a zip.
// 32x32 frames, PPU 20, bottom-centre pivot (the body is centred on x = 16). Variants: v0 front facing right, v1 front
// mirrored, v2 back, v3 back mirrored - Sno Sno's layout.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace MummerBuild
{
    public class Pose
    {
        public int dy;                        // whole body, pixels (negative = up); the ground patch stays put
        public int squash;                    // +1 wider and shorter, -1 narrower and taller
        public int sway;                      // floppy point tip, -2..2
        public int tilt;                      // head tilt: the top of the cone leans -1..1
        public int handLx, handLy, handRx, handRy;
        public string peek = "open";          // the eyes under the hem: open, closed, red, none
        public string face = "smile";         // the drawn face: smile, o
        public int arm;                       // Peekaboo: a shadow arm from under the hem, 0..3
        public int hook;                      // HOOK: 0 none, 1 wind-up, 2 flick, 3 taut, 4 reel
        public int puddle = 9;                // ground patch half-width
        public int gap;                       // extra rows of dark under the hem (airborne)
        public readonly List<Vector3Int> fx = new List<Vector3Int>();
    }

    public static class MummerCharacter
    {
        public const int FxDust = 1, FxSpark = 2, FxThread = 3, FxScrap = 4, FxConfetti = 5, FxNote = 6;

        public static readonly Dictionary<char, Color32> P = new Dictionary<char, Color32>
        {
            // felt
            { 'c', Col.H("CBB4E7") }, { 'C', Col.H("AE93D4") }, { 'k', Col.H("9579BD") }, { 'q', Col.H("E4D8F4") },
            // the drawn face (plum ink), blush, glints
            { 'i', Col.H("4B3A5E") }, { 'I', Col.H("352847") }, { 'p', Col.H("F2A6BE") }, { 'w', Col.H("FFFFFF") },
            // brass bell and zip
            { 'b', Col.H("EDC867") }, { 'B', Col.H("BE923E") }, { 'z', Col.H("7E6A98") }, { 'Z', Col.H("D4A946") },
            // the dark under the costume, its eyes, its hands
            { 'd', Col.H("1E1826") }, { 'D', Col.H("2E2538") }, { 'e', Col.H("F4F0FF") }, { 'r', Col.H("F0566B") },
            { 'h', Col.H("2A2133") }, { 'H', Col.H("E6DCF2") },
            // thread, dust, sparks
            { 't', Col.H("EADFF6") }, { 'T', Col.H("C6AEE3") }, { 'u', Col.H("8C7A6A") }, { 'y', Col.H("F8E39A") },
            // Understudies: cream felt, Ballista orange, Aegis blue, wood, buttons
            { 'f', Col.H("F2E9D6") }, { 'F', Col.H("D9CBB0") }, { 'o', Col.H("F0B171") }, { 'O', Col.H("C98243") },
            { 'a', Col.H("9BBDEE") }, { 'A', Col.H("6990CC") }, { 'n', Col.H("A07850") }, { 'N', Col.H("6E5034") },
            { 's', Col.H("C6CDD6") }, { 'S', Col.H("8C96A3") },
        };

        static Color32 C(char k) { return P[k]; }

        const int Top = 11, Hem = 28;   // the cone's first row (where the point starts) and its hem row, at rest

        // ---------------------------------------------------------------- frame composer

        public static Px Frame(int variant, Pose p)
        {
            bool back = variant >= 2;
            var c = new Px(32, 32);
            const float cx = 16f;

            // Ground patch (a soft shadow, like Sno Sno's snow) - it stays on the ground when Mummer hops.
            for (int i = -p.puddle; i < p.puddle; i++)
            {
                int a = Mathf.Abs(i + 0.5f) > p.puddle - 2.5f ? 35 : 70;
                c.Over(16 + i, 31, Col.A(C('d'), (byte)a));
            }

            int yT = Top + p.dy + 2 * p.squash;
            int yH = Hem + p.dy;
            float widen = 1f + 0.07f * p.squash;
            Func<float, float> Hw = t => (1.5f + 9f * Mathf.Pow(Mathf.Clamp01(t), 0.8f)) * widen;
            Func<int, float> Cx = y => cx + (y < yT + 8 ? Mathf.Round(p.tilt * (1f - (y - yT) / 8f)) : 0f);

            // The dark under the hem: what wears the costume. Two small eyes look out of it.
            int gapTop = yH + 1, gapBottom = Mathf.Min(30, yH + 2 + p.gap);
            for (int y = gapTop; y <= gapBottom; y++)
                for (int x = 11; x <= 20; x++)
                    if (Mathf.Abs(x + 0.5f - cx) <= 4.6f - (y - gapTop) * 0.6f)
                        c.Set(x, y, C(y == gapBottom ? 'D' : 'd'));
            if (p.peek != "none" && !back)
            {
                int ey = gapTop;
                if (p.peek == "closed") { c.Set(14, ey + 1, C('D')); c.Set(18, ey + 1, C('D')); }
                else
                {
                    char k = p.peek == "red" ? 'r' : 'e';
                    c.Set(14, ey, C(k)); c.Set(18, ey, C(k));
                }
            }

            // The cone. Shade on the facing side, deeper at the hem, a half-alpha rim on the lit upper-left edge.
            Func<int, int, bool> In = (x, y) =>
            {
                if (y < yT || y > yH) return false;
                float t = (y - yT) / (float)(yH - yT);
                return Mathf.Abs(x + 0.5f - Cx(y)) <= Hw(t);
            };
            for (int y = yT; y <= yH; y++)
                for (int x = 2; x < 30; x++)
                {
                    if (!In(x, y)) continue;
                    float t = (y - yT) / (float)(yH - yT);
                    float nx = (x + 0.5f - Cx(y)) / Hw(t);
                    bool edge = !In(x - 1, y) || !In(x + 1, y) || !In(x, y - 1);
                    Color32 col;
                    if (y == yH) col = C('k');
                    else if (nx > 0.48f || (nx > 0.25f && t > 0.82f)) col = C('C');
                    else if (t > 0.92f) col = C('k');
                    else col = C('c');
                    if (In(x - 1, y) && !In(x - 2, y) && t > 0.15f && t < 0.75f) col = C('q');   // a soft sheen just inside the lit edge
                    if (edge && nx < -0.15f && t < 0.6f) col = Col.A(C('c'), 150);
                    c.Set(x, y, col);
                }
            // Pinking-shear teeth along the hem.
            for (int x = 2; x < 30; x++)
                if (In(x, yH) && (x % 3) == 1)
                    c.Set(x, yH + 1, C('k'));

            // The floppy point, falling over toward the facing side (away from it from behind), with the bell.
            DrawPoint(c, Cx(yT), yT, back ? -1 : 1, p.sway);

            if (!back)
                Face(c, yT, p, Cx);
            else
            {
                // The zip down the back, and its pull.
                int zx = Mathf.RoundToInt(Cx(yT + 6));
                for (int y = yT + 3; y <= yH - 2; y++)
                {
                    c.Set(zx, y, C('z'));
                    if (y % 2 == 0) c.Set(zx - 1, y, Col.A(C('z'), 170)); else c.Set(zx + 1, y, Col.A(C('z'), 170));
                }
                c.Set(zx, yT + 2, C('Z')); c.Set(zx + 1, yT + 3, C('Z'));
            }

            // Peekaboo: a shadow arm shoots out from under the hem toward the facing side, in front of the costume,
            // two pixels thick at the shoulder, claws first.
            if (p.arm > 0)
            {
                int reach = p.arm == 1 ? 6 : p.arm == 2 ? 11 : 4;
                int x0 = 18, y0 = gapTop;
                int tx = x0 + reach, ty = y0 - (p.arm == 2 ? 6 : 3);
                for (int i = 0; i <= reach; i++)
                {
                    float k = i / (float)reach;
                    int x = x0 + i, y = Mathf.RoundToInt(Mathf.Lerp(y0, ty, k * k));
                    c.Set(x, y, C('h'));
                    if (k < 0.6f) c.Set(x, y + 1, C('D'));
                }
                c.Set(tx + 1, ty - 1, C('H')); c.Set(tx + 2, ty - 1, C('H'));
                c.Set(tx + 1, ty, C('H')); c.Set(tx + 2, ty + 1, C('H'));
                c.Set(tx + 1, ty + 1, C('H'));
            }
            // Floating shadow hands with pale claws.
            Hand(c, 3 + p.handLx, yT + 10 + p.handLy, -1);
            Hand(c, 27 + p.handRx, yT + 10 + p.handRy, 1);

            // HOOK: a thread from the near hand.
            if (p.hook > 0)
            {
                int hx = 28 + p.handRx, hy = yT + 10 + p.handRy;
                if (p.hook == 1)
                    for (int i = 0; i < 7; i++) c.Over(hx - 1 + Mathf.RoundToInt(Mathf.Cos(i * 0.9f) * 2.2f), hy - 3 + Mathf.RoundToInt(Mathf.Sin(i * 0.9f) * 2.2f), C(i % 2 == 0 ? 'w' : 'T'));
                else
                {
                    int ex = 31, ey = p.hook == 2 ? hy - 4 : hy - 1;
                    for (int x = hx + 1; x <= ex; x++)
                    {
                        float k = (x - hx) / (float)Mathf.Max(1, ex - hx);
                        int y = Mathf.RoundToInt(Mathf.Lerp(hy, ey, k) + (p.hook == 2 ? Mathf.Sin(k * Mathf.PI) * -2f : 0f));
                        c.Over(x, y, x % 3 == 0 ? C('T') : C('w'));
                    }
                }
            }

            foreach (var f in p.fx)
                switch (f.z)
                {
                    case FxDust: c.Over(f.x, f.y, Col.A(C('u'), 200)); break;
                    case FxSpark: c.Over(f.x, f.y, new Color32(255, 255, 255, 220)); break;
                    case FxThread: c.Over(f.x, f.y, C('t')); c.Over(f.x + 1, f.y + 1, C('T')); break;
                    case FxScrap: c.Over(f.x, f.y, C('c')); c.Over(f.x + 1, f.y, C('C')); c.Over(f.x, f.y - 1, C('q')); break;
                    case FxConfetti: c.Over(f.x, f.y, C((f.x + f.y) % 3 == 0 ? 'p' : (f.x + f.y) % 3 == 1 ? 'y' : 'c')); break;
                    case FxNote: c.Over(f.x, f.y, C('y')); c.Over(f.x, f.y - 1, C('y')); c.Over(f.x + 1, f.y - 2, C('y')); break;
                }
            return variant == 1 || variant == 3 ? c.FlipX() : c;
        }

        // The point: a felt tube from the apex, up and over, thinning to the bell.
        static void DrawPoint(Px c, float apexX, int yT, int side, int sway)
        {
            int[,] pts = { { 0, 0 }, { 0, -2 }, { 1, -4 }, { 2, -5 }, { 4, -6 }, { 6, -6 }, { 7, -5 }, { 8, -3 } };
            int[] thick = { 3, 3, 3, 2, 2, 2, 1, 1 };
            int n = pts.GetLength(0);
            Vector2 prev = Vector2.zero;
            for (int i = 0; i < n; i++)
            {
                float sx = pts[i, 0] + (i >= 5 ? sway : 0), sy = pts[i, 1] - (i >= 6 ? Mathf.Abs(sway) / 2 : 0);
                Vector2 cur = new Vector2(apexX - 0.5f + sx * side, yT + sy);
                if (i > 0)
                    for (float k = 0f; k <= 1f; k += 0.2f)
                    {
                        Vector2 q = Vector2.Lerp(prev, cur, k);
                        float th = Mathf.Lerp(thick[i - 1], thick[i], k);
                        for (int oy = 0; oy < Mathf.CeilToInt(th); oy++)
                            for (int ox = -(int)(th / 2f); ox <= (int)((th - 1) / 2f); ox++)
                            {
                                int x = Mathf.RoundToInt(q.x) + ox, y = Mathf.RoundToInt(q.y) + oy;
                                bool under = oy == Mathf.CeilToInt(th) - 1 && i >= 3;
                                c.Set(x, y, C(under ? 'C' : (side > 0 ? (ox > 0 ? 'C' : 'c') : (ox < 0 ? 'C' : 'c'))));
                            }
                    }
                prev = cur;
            }
            int bx = Mathf.RoundToInt(prev.x), by = Mathf.RoundToInt(prev.y) + 1;
            c.Set(bx, by, C('b')); c.Set(bx + side, by, C('B')); c.Set(bx, by + 1, C('b')); c.Set(bx + side, by + 1, C('B'));
            c.Set(bx + (side > 0 ? 0 : side), by + 2, C('B'));
        }

        // The drawn face: crooked eyes (the far one a pixel lower), blush and a zigzag grin, pushed toward the facing side.
        static void Face(Px c, int yT, Pose p, Func<int, float> Cx)
        {
            int ey = yT + 7;
            int off = Mathf.RoundToInt(Cx(ey) - 16f);
            int lx = 13 + off, rx = 19 + off;
            // far eye (screen left) one pixel lower
            Eye(c, lx, ey + 1);
            Eye(c, rx, ey);
            c.Set(lx - 2, ey + 4, C('p')); c.Set(lx - 1, ey + 4, C('p'));
            c.Set(rx + 2, ey + 3, C('p')); c.Set(rx + 3, ey + 3, C('p'));
            int my = ey + 5;
            if (p.face == "o")
            {
                c.Set(16 + off, my, C('I')); c.Set(17 + off, my, C('I')); c.Set(16 + off, my + 1, C('I')); c.Set(17 + off, my + 1, C('I'));
                return;
            }
            for (int x = lx; x <= rx + 1; x++)
                c.Set(x, my + ((x - lx) % 2 == 0 ? 0 : 1), C('i'));
        }

        static void Eye(Px c, int x, int y)
        {
            c.Set(x, y, C('i')); c.Set(x + 1, y, C('i'));
            c.Set(x, y + 1, C('I')); c.Set(x + 1, y + 1, C('I'));
            c.Set(x, y - 1, C('i')); c.Set(x + 1, y - 1, C('w'));
        }

        static void Hand(Px c, int x, int y, int side)
        {
            c.Set(x, y, C('h')); c.Set(x + 1, y, C('h')); c.Set(x - 1, y, C('h'));
            c.Set(x, y + 1, C('h')); c.Set(x - side, y + 1, C('D'));
            c.Set(x + side * 2, y, C('H')); c.Set(x + side * 2, y + 1, C('H'));
            c.Set(x, y - 1, Col.A(C('h'), 140));
        }

        // ---------------------------------------------------------------- animations

        public static List<Pose> Idle()
        {
            var list = new List<Pose>();
            int[] squash = { 0, 0, 1, 1, 0, 0 };
            int[] sway = { 0, 1, 1, 0, -1, -1 };
            int[] hand = { 0, 0, 1, 1, 0, -1 };
            string[] peek = { "open", "open", "open", "open", "closed", "open" };
            for (int f = 0; f < 6; f++)
                list.Add(new Pose { squash = squash[f], sway = sway[f], handLy = hand[f], handRy = hand[(f + 3) % 6], peek = peek[f] });
            return list;
        }

        // A hop like Sno Sno's: crouch, stretch, airborne with the legs showing, land. The point lags a beat behind.
        public static List<Pose> Move()
        {
            return new List<Pose>
            {
                new Pose { sway = 0 },
                new Pose { squash = 1, sway = -1, handLy = 1, handRy = 1, puddle = 10 },
                new Pose { squash = 1, sway = -2, handLy = 2, handRy = 2, puddle = 10 },
                new Pose { squash = -1, dy = -2, sway = 1, handLy = -2, handRy = -2, gap = 1, puddle = 8 },
                new Pose { squash = -1, dy = -4, sway = 2, handLy = -3, handRy = -3, gap = 2, puddle = 7, tilt = 1 },
                new Pose { dy = -1, sway = 1, handLy = -1, handRy = -1, gap = 1, puddle = 8 },
            };
        }

        // Peekaboo: the eyes under the hem go red, it leans back, and a shadow arm swipes out from under the costume.
        public static List<Pose> Attack()
        {
            var a = new Pose { peek = "red", tilt = -1, handRy = -2, sway = -1 };
            var b = new Pose { peek = "red", arm = 1, tilt = -1, sway = -1 };
            var d = new Pose { peek = "red", arm = 2, tilt = 1, sway = 1, face = "o" };
            d.fx.Add(new Vector3Int(31, 25, FxSpark)); d.fx.Add(new Vector3Int(30, 23, FxSpark)); d.fx.Add(new Vector3Int(31, 27, FxSpark));
            var e = new Pose { peek = "open", arm = 3, sway = 1 };
            return new List<Pose> { a, b, d, e };
        }

        // HOOK: wind up, flick the thread out to the right, hold it taut, reel, recover.
        public static List<Pose> Hook()
        {
            return new List<Pose>
            {
                new Pose { hook = 1, handRx = -1, handRy = -5, tilt = -1, sway = -1 },
                new Pose { hook = 2, handRx = 1, handRy = -3, tilt = 1, sway = 1, face = "o" },
                new Pose { hook = 3, handRx = 1, handRy = -1, sway = 2 },
                new Pose { hook = 4, handRx = -1, handRy = 0, tilt = -1, sway = 0, peek = "red" },
                new Pose { hook = 4, handRx = -2, handRy = 1, tilt = -1, sway = -1, squash = 1, peek = "red" },
                new Pose { handRy = 0, sway = 0 },
            };
        }

        public static List<Px> Fall()
        {
            var frames = new List<Px>();
            for (int f = 0; f < 12; f++)
            {
                var p = new Pose { squash = -1, dy = -3, sway = f % 4 < 2 ? -2 : 2, handLy = f % 2 == 0 ? -6 : -4, handRy = f % 2 == 0 ? -4 : -6, peek = "closed", face = "o", puddle = 0, gap = 1 };
                p.fx.Add(new Vector3Int(6 + (f * 3) % 18, 2 + (f % 3), FxThread));
                p.fx.Add(new Vector3Int(24 - (f * 5) % 16, 5 + ((f + 1) % 4), FxScrap));
                frames.Add(Frame(0, p));
            }
            return frames;
        }

        // Landing: drops in, lands flat in a puff of dust, the point flops down a beat late, holds very still - then the
        // eyes under the hem open (red, then white) and the head tilts, as if it heard something.
        public static List<Px> Land()
        {
            var poses = new List<Pose>
            {
                new Pose { dy = -12, squash = -1, sway = 2, handLy = -5, handRy = -5, peek = "closed", puddle = 3, gap = 1 },
                new Pose { dy = -5, squash = -1, sway = 2, handLy = -4, handRy = -4, peek = "closed", puddle = 5, gap = 1 },
                Dust(new Pose { squash = 2, sway = 1, handLy = 3, handRy = 3, peek = "closed", puddle = 12 }, 0),
                Dust(new Pose { squash = 2, sway = -2, handLy = 3, handRy = 3, peek = "closed", puddle = 11 }, 1),
                Dust(new Pose { squash = 1, sway = -2, handLy = 2, handRy = 2, peek = "closed", puddle = 10 }, 2),
                new Pose { squash = 1, sway = -1, handLy = 1, handRy = 1, peek = "closed" },
                new Pose { sway = 0, peek = "closed" },
                new Pose { sway = 0, peek = "none" },
                new Pose { sway = 0, peek = "red" },
                new Pose { sway = 0, peek = "open" },
                new Pose { sway = 1, tilt = 1, peek = "open" },
                new Pose { sway = 1, tilt = 1, peek = "open" },
            };
            var frames = new List<Px>();
            foreach (var p in poses) frames.Add(Frame(0, p));
            return frames;
        }

        static Pose Dust(Pose p, int t)
        {
            int[,] bits = { { 5, 30 }, { 26, 30 }, { 3, 28 }, { 28, 27 }, { 7, 27 }, { 24, 26 } };
            for (int i = 0; i < bits.GetLength(0); i++)
            {
                int x = bits[i, 0] + (bits[i, 0] < 16 ? -t : t);
                int y = bits[i, 1] - (t < 2 ? t * 2 : 4 - (t - 1) * 3);
                p.fx.Add(new Vector3Int(x, Mathf.Min(31, y), FxDust));
            }
            return p;
        }

        // ---------------------------------------------------------------- sheets

        // Idle rows v0..v3 then Move rows v0..v3, 6 frames each: 192x256 like Snosnoidle-Sheet.
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

        // Same costume at Sno Sno's boat size: about 9 wide and 12 tall, standing on row 15.
        static Px Mini(bool back, int dy, int squash, int sway, int hand, bool blink)
        {
            var c = new Px(16, 16);
            for (int x = 3; x < 13; x++) c.Over(x, 15, Col.A(C('d'), (byte)(x == 3 || x == 12 ? 35 : 65)));
            int top = 5 + dy + squash, hem = 13 + dy;
            for (int x = 6; x <= 9; x++) c.Set(x, hem + 1, C('d'));
            if (!back && !blink) { c.Set(6, hem + 1, C('e')); c.Set(9, hem + 1, C('e')); }
            for (int y = top; y <= hem; y++)
            {
                float t = (y - top) / (float)Mathf.Max(1, hem - top);
                float hw = (0.8f + 4f * Mathf.Pow(t, 0.8f)) * (1f + 0.08f * squash);
                for (int x = 0; x < 16; x++)
                {
                    float nx = (x + 0.5f - 8f) / hw;
                    if (Mathf.Abs(nx) > 1f) continue;
                    char k = y == hem ? 'k' : nx > 0.45f ? 'C' : 'c';
                    Color32 col = C(k);
                    if (nx < -0.6f && t < 0.5f) col = Col.A(col, 150);
                    c.Set(x, y, col);
                }
            }
            for (int x = 3; x <= 12; x++) if (x % 3 == 0) c.Set(x, hem + 1, C('k'));
            // point and bell (facing right)
            int px = 8;
            c.Set(px, top - 1, C('c')); c.Set(px, top - 2, C('c')); c.Set(px + 1, top - 3, C('c')); c.Set(px + 2, top - 3 + sway, C('C'));
            c.Set(px + 3, top - 2 + sway, C('b')); c.Set(px + 3, top - 1 + sway, C('B'));
            if (!back)
            {
                c.Set(7, top + 4, C('i')); c.Set(10, top + 3, C('i'));
                c.Set(7, top + 6, C('i')); c.Set(8, top + 7, C('i')); c.Set(9, top + 6, C('i')); c.Set(10, top + 7, C('i'));
            }
            else
                for (int y = top + 2; y < hem - 1; y++) c.Set(8, y, C('z'));
            c.Set(2, top + 6 + hand, C('h')); c.Set(13, top + 6 - hand, C('h'));
            return c;
        }

        // 96x128: Idle v0..v3 (rows 0-3) then Move v0..v3 (rows 4-7), 6 frames of 16x16 - like SSnosnoSheet.
        public static Px MiniSheet()
        {
            var rows = new List<List<Px>>();
            int[] squash = { 0, 0, 1, 1, 0, 0 };
            int[] sway = { 0, 1, 1, 0, 0, 0 };
            int[] hand = { 0, 0, 1, 1, 0, 0 };
            bool[] blink = { false, false, false, false, true, false };
            int[] hopDy = { 0, 0, 0, -1, -2, -1 };
            int[] hopSquash = { 0, 1, 1, -1, -1, 0 };
            for (int v = 0; v < 4; v++)
            {
                var r = new List<Px>();
                for (int f = 0; f < 6; f++) { var m = Mini(v >= 2, 0, squash[f], sway[f], hand[f], blink[f]); r.Add(v == 1 || v == 3 ? m.FlipX() : m); }
                rows.Add(r);
            }
            for (int v = 0; v < 4; v++)
            {
                var r = new List<Px>();
                for (int f = 0; f < 6; f++) { var m = Mini(v >= 2, hopDy[f], hopSquash[f], -sway[f], hand[f], false); r.Add(v == 1 || v == 3 ? m.FlipX() : m); }
                rows.Add(r);
            }
            return Px.Sheet(rows, 16, 16);
        }

        // ---------------------------------------------------------------- Understudies (16x16)

        // Felt dolls with a triangle head (Mummer's own shape), mismatched eyes (the near one a row higher), a stitched
        // collar and a leash string tied to the tip of the head. Costumes: plain cream felt; Ballista - an orange hood
        // and a little crossbow; Aegis - a blue hood and a quilted shield. Drawn facing right; the shade is on the facing
        // side like the vessel's. Sheet: rows v0..v3; columns 0-5 Move (column 0 doubles as Idle), 6-9 Attack.
        static readonly Dictionary<string, string[]> DollRows = new Dictionary<string, string[]>
        {
            { "Plain", new[] {
                "....tt....",
                "....fF....",
                "...ffFF...",
                "...fffI...",
                "..fifffF..",
                "..fffffF..",
                ".fFzzzzFf.",
                "..ffffFF..",
                "..ffffFF..",
                "..fF..fF..",
                "..N....N..",
            } },
            { "Ballista", new[] {
                "....tt....",
                "....oO....",
                "...ooOO...",
                "...oooI...",
                "..oiOooO..",
                "..fffffF.N",
                ".oOzzzzOnN",
                "..ooooOO.N",
                "..ooooOO..",
                "..oO..oO..",
                "..N....N..",
            } },
            { "Aegis", new[] {
                "....tt....",
                "....aA....",
                "...aaAA...",
                "...aaaI...",
                "..aiaaaA..",
                "..fffffF..",
                ".aAzzAAAA.",
                "..aaaAsaA.",
                "..aaaAaaA.",
                "..aA..AA..",
                "..N....N..",
            } },
        };

        static Px Doll(string costume, bool back, int dy, int attack)
        {
            var c = new Px(16, 16);
            for (int x = 4; x < 12; x++) c.Over(x, 15, Col.A(C('d'), (byte)(x == 4 || x == 11 ? 35 : 60)));
            string[] rows = (string[])DollRows[costume].Clone();
            if (back)
                for (int r = 0; r < rows.Length; r++)
                    rows[r] = rows[r].Replace('i', costume == "Plain" ? 'f' : costume == "Ballista" ? 'o' : 'a').Replace('I', costume == "Plain" ? 'F' : costume == "Ballista" ? 'O' : 'A');
            int ox = 3 + (costume == "Ballista" && attack == 1 ? -1 : 0), oy = 4 + dy;
            // the leash string runs up out of the frame from the knot on the head
            for (int y = 0; y < oy; y++) c.Over(7, y, Col.A(C('t'), 210));
            // Aegis: the shield shoves forward on the bash
            if (costume == "Aegis" && attack > 0)
                for (int r = 6; r <= 9; r++)
                {
                    var chars = rows[r].ToCharArray();
                    for (int k = chars.Length - 1; k >= 5; k--)
                        if (chars[k] == 'A' || chars[k] == 's' || (chars[k] == 'a' && k >= 6))
                        {
                            c.Set(ox + k + attack, oy + r, C(chars[k]));
                            chars[k] = k >= 6 && r >= 7 ? 'a' : chars[k];
                        }
                    rows[r] = new string(chars);
                }
            c.Stamp(rows, P, ox, oy);
            if (costume == "Plain" && attack > 0)
            {
                // a felt arm bop
                int reach = attack == 1 ? 1 : attack == 2 ? 3 : 2;
                for (int i = 1; i <= reach; i++) c.Set(ox + 8 + i, oy + 6 - (i > 1 ? 1 : 0), C(i == reach ? 'F' : 'f'));
                if (attack == 2) { c.Set(ox + 12, oy + 3, new Color32(255, 255, 255, 230)); c.Set(ox + 13, oy + 5, new Color32(255, 255, 255, 200)); }
            }
            if (costume == "Ballista" && attack >= 1 && attack <= 2)
            {
                // the felt bolt leaves the crossbow
                int bx = ox + 10 + (attack - 1) * 3;
                c.Set(bx, oy + 6, C('y')); c.Set(bx + 1, oy + 6, C('O'));
            }
            return c;
        }
        public static Px DollSheet(string costume)
        {
            int[] hop = { 0, 0, -1, -2, -1, 0 };
            int[] atk = { 1, 2, 3, 0 };
            var rows = new List<List<Px>>();
            for (int v = 0; v < 4; v++)
            {
                var r = new List<Px>();
                for (int f = 0; f < 6; f++) { var d = Doll(costume, v >= 2, hop[f], 0); r.Add(v == 1 || v == 3 ? d.FlipX() : d); }
                for (int f = 0; f < 4; f++) { var d = Doll(costume, v >= 2, 0, atk[f]); r.Add(v == 1 || v == 3 ? d.FlipX() : d); }
                rows.Add(r);
            }
            return Px.Sheet(rows, 16, 16);
        }
        // ---------------------------------------------------------------- UI art

        public static Px Icon() { return Frame(0, new Pose()); }

        // Journal tab icon, in the style of the game's Vesselicons sheet: a 32x32 white line drawing with 1 px strokes
        // and an open interior. The costume's triangle, the floppy point and bell, the drawn face, and the eyes below.
        public static Px TabIcon()
        {
            var c = new Px(32, 32);
            Color32 w = new Color32(255, 255, 255, 255), clear = new Color32(0, 0, 0, 0);
            Func<float, float, bool> cone = (x, y) =>
            {
                if (y < 9f || y > 26.5f) return false;
                float t = (y - 9f) / 17.5f;
                return Mathf.Abs(x - 15.5f) <= 1.5f + 11f * Mathf.Pow(t, 0.8f);
            };
            for (int y = 0; y < 32; y++)
                for (int x = 0; x < 32; x++)
                {
                    if (!cone(x + 0.5f, y + 0.5f)) continue;
                    bool edge = false;
                    for (int oy = -1; oy <= 1; oy++)
                        for (int ox = -1; ox <= 1; ox++)
                            if (!cone(x + ox + 0.5f, y + oy + 0.5f))
                                edge = true;
                    c.Set(x, y, edge ? w : clear);
                }
            // hem teeth
            for (int x = 5; x <= 26; x += 3) c.Set(x, 27, w);
            // the point, up and over to the right, and the bell
            foreach (var p in new[] { new Vector2Int(15, 8), new Vector2Int(15, 7), new Vector2Int(16, 6), new Vector2Int(16, 5), new Vector2Int(17, 4), new Vector2Int(18, 3),
                                      new Vector2Int(19, 3), new Vector2Int(20, 3), new Vector2Int(21, 4), new Vector2Int(22, 5), new Vector2Int(22, 6) })
                c.Set(p.x, p.y, w);
            c.Set(21, 7, w); c.Set(22, 7, w); c.Set(23, 7, w); c.Set(21, 8, w); c.Set(23, 8, w); c.Set(22, 9, w);
            // crooked eyes, grin
            for (int y = 15; y < 17; y++) for (int x = 11; x < 13; x++) c.Set(x, y + 1, w);
            for (int y = 15; y < 17; y++) for (int x = 18; x < 20; x++) c.Set(x, y, w);
            for (int x = 11; x <= 20; x++) c.Set(x, 20 + (x % 2), w);
            // the eyes under the hem
            c.Set(13, 29, w); c.Set(18, 29, w);
            return c;
        }

        public static Px EndWin()
        {
            var p = new Pose { handLy = -5, handRy = -5, sway = 2, face = "smile", squash = -1 };
            p.fx.Add(new Vector3Int(4, 6, FxConfetti)); p.fx.Add(new Vector3Int(27, 5, FxConfetti)); p.fx.Add(new Vector3Int(6, 14, FxConfetti));
            p.fx.Add(new Vector3Int(26, 13, FxConfetti)); p.fx.Add(new Vector3Int(9, 3, FxNote)); p.fx.Add(new Vector3Int(23, 2, FxConfetti));
            return Frame(0, p);
        }

        public static Px EndLose()
        {
            var p = new Pose { squash = 2, sway = -2, handLy = 3, handRy = 3, peek = "closed", face = "o", tilt = -1 };
            var f = Frame(0, p);
            var d = new Px(32, 32);
            for (int i = 0; i < f.C.Length; i++) { var c = f.C[i]; d.C[i] = Col.Mix(c, Col.A(Col.H("9A9488"), c.a), 0.35f); }
            return d;
        }

        // 12x12 ready-up icon: the shipped vessels draw a tiny portrait inside white corner brackets.
        public static Px ReadyUp()
        {
            var rows = new[]
            {
                "............",
                ".WW......WW.",
                ".W.....bB.W.",
                ".....ccb....",
                ".....cC.....",
                "....cccC....",
                "....icIC....",
                "...cccccC...",
                "...kkkkkk...",
                ".W..deded.W.",
                ".WW......WW.",
                "............",
            };
            var pal = new Dictionary<char, Color32>(P) { { 'W', new Color32(255, 255, 255, 255) } };
            return Px.FromRows(rows, pal);
        }

        // ---------------------------------------------------------------- splash (128x128 line art)

        // The shipped splashes are line art: a two-tone frame, then the vessel's silhouette filled with a translucent
        // accent, stroked outside in the solid accent and traced inside (3 px in) with the body's own colours.
        public static Px Splash()
        {
            const int S = 128;
            Color32 accent = Col.H("CDB8EA");
            Color32 fill = Col.A(accent, 70);
            var c = new Px(S, S);
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    int d = Mathf.Min(Mathf.Min(x, y), Mathf.Min(S - 1 - x, S - 1 - y));
                    if (d < 2) c.Set(x, y, fill);
                    else if (d < 4) c.Set(x, y, accent);
                }
            var parts = new List<SplashPart>();
            // shadow hands
            parts.Add(SplashPart.Ellipse(new Vector2(14, 82), 8, 6, 0f, C('h'), C('D')));
            parts.Add(SplashPart.Ellipse(new Vector2(114, 76), 8, 6, 0f, C('h'), C('D')));
            // the floppy point and the bell
            parts.Add(SplashPart.Chain(new[] { new Vector2(62, 36), new Vector2(63, 22), new Vector2(70, 12), new Vector2(82, 9), new Vector2(92, 14), new Vector2(97, 24) }, new[] { 10f, 8.5f, 7f, 6f, 5f, 4f }, C('q'), C('C')));
            parts.Add(SplashPart.Ellipse(new Vector2(98, 32), 7, 7, 0f, C('b'), C('B')));
            // the costume
            parts.Add(SplashPart.Cone(new Vector2(62, 30), 112, 6f, 48f, C('q'), C('C')));
            foreach (var part in parts) part.Draw(c, accent, fill);

            // the dark under the hem, with its two eyes
            for (int y = 113; y < 121; y++)
                for (int x = 40; x < 86; x++)
                    if (Mathf.Abs(x + 0.5f - 63f) <= 20f - (y - 113) * 1.8f) c.Set(x, y, C('d'));
            for (int y = 114; y < 117; y++) { for (int x = 54; x < 57; x++) c.Set(x, y, C('e')); for (int x = 70; x < 73; x++) c.Set(x, y, C('e')); }

            // the drawn face: crooked eyes (the far one lower), blush, zigzag grin
            Color32 ink = C('i');
            for (int y = 62; y < 72; y++) for (int x = 48; x < 55; x++) c.Set(x, y, ink);
            for (int y = 58; y < 68; y++) for (int x = 72; x < 79; x++) c.Set(x, y, ink);
            for (int y = 62; y < 65; y++) for (int x = 51; x < 54; x++) c.Set(x, y, C('w'));
            for (int y = 58; y < 61; y++) for (int x = 75; x < 78; x++) c.Set(x, y, C('w'));
            for (int y = 75; y < 79; y++) { for (int x = 40; x < 47; x++) c.Set(x, y, C('p')); for (int x = 82; x < 89; x++) c.Set(x, y, C('p')); }
            for (int x = 46; x <= 82; x++)
            {
                int phase = ((x - 46) / 4) % 2;
                int yy = 84 + (phase == 0 ? (x - 46) % 4 : 3 - (x - 46) % 4);
                c.Set(x, yy, ink); c.Set(x, yy + 1, ink);
            }
            return c;
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
                    Line = line, Shade = shade, ShadeFromX = o.x + rx * 0.4f
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
                    Line = line, Shade = shade, ShadeFromX = 999f
                };
            }

            // A tapering tube through several points (the floppy point), as one shape so it is outlined once.
            public static SplashPart Chain(Vector2[] pts, float[] radii, Color32 line, Color32 shade)
            {
                return new SplashPart
                {
                    Inside = (x, y) =>
                    {
                        Vector2 p = new Vector2(x, y);
                        for (int i = 0; i < pts.Length - 1; i++)
                        {
                            Vector2 a = pts[i], ab = pts[i + 1] - a;
                            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
                            if ((a + ab * t - p).magnitude <= Mathf.Lerp(radii[i], radii[i + 1], t)) return true;
                        }
                        return false;
                    },
                    Line = line, Shade = shade, ShadeFromX = 999f
                };
            }

            // The soft triangle: narrow and rounded at the top, wide at the hem, with pinking-shear teeth.
            public static SplashPart Cone(Vector2 apex, float hemY, float topHw, float hemHw, Color32 line, Color32 shade)
            {
                return new SplashPart
                {
                    Inside = (x, y) =>
                    {
                        if (y < apex.y - topHw * 0.6f || y > hemY + 3f) return false;
                        float t = Mathf.Clamp01((y - apex.y) / (hemY - apex.y));
                        float hw = topHw + (hemHw - topHw) * Mathf.Pow(t, 0.8f);
                        if (y < apex.y) hw = Mathf.Sqrt(Mathf.Max(0f, topHw * topHw - (apex.y - y) * (apex.y - y) * 2.8f));
                        if (y > hemY) { float k = Mathf.Abs(((x - apex.x) % 8f + 8f) % 8f - 4f); if (k > 4f - (y - hemY) * 1.3f) return false; }
                        return Mathf.Abs(x - apex.x) <= hw;
                    },
                    Line = line, Shade = shade, ShadeFromX = apex.x + hemHw * 0.45f
                };
            }

            // Clears what lies behind (inside and the 3 px stroke), then strokes outside and traces 3 px inside.
            public void Draw(Px c, Color32 accent, Color32 fill)
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
                            c.Set(x, y, d >= 4 && d <= 6 ? (x >= ShadeFromX ? Shade : Line) : fill);
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
