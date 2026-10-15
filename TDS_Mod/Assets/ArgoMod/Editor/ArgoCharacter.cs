// Argo asset generator: the vessel's pixel art (redesigned with the user 2026-10-04 for 1.4.0; Argo 2.0 keeps it).
// Argo is a sturdy deep-sea diver: a round brass helmet with a dark porthole and two light eyes inside it, seated on a
// riveted brass breastplate that flares a pixel a row into the blue suit's rounded shoulders, big brass mitts on the
// hips (they touch the body, per the user's note on 1.0) and dark boots. A brass valve on the helmet's crown lets out a
// bubble now and then. He stands three-quarter on like Boutman and Fie: the porthole sits on the near half of the dome.
// Drawn to the shipped vessels' rules as kit docs/03 §1 gave them on 2026-10-04 and the user's palette note (Boutman's
// and Fie's economy): three tones of brass and of suit, the glass, the eyes and the boots, a half-alpha rim on the lit
// upper-left, shade on the facing side, a soft ground patch, a live 6-frame idle. The kit's later art survey made the
// rim Smacky's alone and the patch Sno Sno's (docs/18 §4.2): Argo keeps this 1.4.0 look (docs/STYLE.md), and a new
// vessel draws by docs/18 §4.2, not by this file. The standing frame is hand-placed from string rows; posed arms, the
// wrench and the effects are drawn from a few points.
// 32x32 frames, PPU 20, bottom-centre pivot. Variants: v0 front facing right, v1 front mirrored, v2 back, v3 back
// mirrored - the shipped layout. Authoring coordinates are x right, y down (row 0 = top).
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ArgoBuild
{
    public class Pose
    {
        public int dy;                        // helmet, collar, suit and arms, pixels (negative = up); legs stay planted
        public int headDy;                    // the helmet bobs on its own
        public int lean;                      // the upper body leans toward the facing side
        public int mittDy;                    // the resting mitts settle a beat after the body
        public int swing;                     // walking: the near mitt forward (+1) or back (-1), the far mitt opposite
        public string legs = "stand";         // stand, liftNear, liftFar, strideNear, strideFar, crouch, dangle, sit
        public string arms = "rest";          // rest, windup, smash, follow, ready, sonar, up, cheer, slump
        public string eyes = "open";          // open, left, right, closed, flash, happy
        public bool air;                      // airborne: the legs move with the body
        public int puddle = 12;               // ground patch half-width
        public bool grey;                     // the defeated end-screen look
        public readonly List<float> rings = new List<float>();          // SONAR rings: radius (alpha falls off with it)
        public readonly List<Vector3Int> fx = new List<Vector3Int>();   // x, y, kind
    }

    public static class ArgoCharacter
    {
        public const int FxBubble = 1, FxBubbleBig = 2, FxDust = 3, FxSpark = 4, FxSwoosh = 5;

        public static readonly Dictionary<char, Color32> P = new Dictionary<char, Color32>
        {
            // brass (helmet, collar, mitts, valve): lit, main, shade
            { 'y', Col.H("F2C77A") }, { 'Y', Col.H("D9A048") }, { 'Z', Col.H("A8722E") },
            // the suit: lit, main, shade
            { 'u', Col.H("9CC3E0") }, { 'U', Col.H("6F9DC8") }, { 'V', Col.H("4C74A3") },
            // the porthole: glass, glass lit, the eyes
            { 'G', Col.H("26405A") }, { 'g', Col.H("3D6680") }, { 'e', Col.H("BDF2F0") },
            // boots and belt
            { 'o', Col.H("3E4B5C") },
            // the wrench: edge, steel
            { 'S', Col.H("E4ECEE") }, { 'F', Col.H("9AA8B0") },
            // ground patch, dust, white
            { 'd', Col.H("1E1826") }, { 'w', Col.H("8C8A86") }, { 'W', Col.H("FFFFFF") },
        };

        // The Nauts' crew colours, by the keys the builder passes (main, shade): Port red, Starboard green, Bosun gold.
        static readonly Dictionary<char, Color32> Crew = new Dictionary<char, Color32>
        {
            { 'r', Col.H("DE5456") }, { 'R', Col.H("A93A3E") },
            { 'e', Col.H("54BE6E") }, { 'E', Col.H("3A8C50") },
            { 'u', Col.H("F4CC56") }, { 'U', Col.H("C99E32") },
        };

        static Color32 C(char k) { return P[k]; }

        // The soft rims: the lit colour at half alpha on the upper-left edges (the 2026-10-04 rule; see the header).
        static readonly Color32 BrassRim = Col.A(Col.H("F2C77A"), 150), SuitRim = Col.A(Col.H("9CC3E0"), 150);

        static Color32 Ink(char k) { return k == 's' ? BrassRim : k == 't' ? SuitRim : C(k); }

        // ---------------------------------------------------------------- parts (frame coordinates at rest)

        // The helmet, rows 4..20: the valve (a round brass button on a short neck), the round dome (s rim, y lit, Y brass,
        // Z shade), the porthole on the near half (Z rim with rounded corners, G glass, g its lit corner; the eyes are drawn
        // by Eyes) and the breastplate, a riveted row and a plain one. The back: a bolted plate where the porthole would be.
        const int HelmY = 4, BodyY = 21;
        static readonly string[] HelmF =
        {
            "...............syYZ.............",
            "...............yYZZ.............",
            "................ZZ..............",
            ".............ssyYYZZ............",
            "...........ssyYYYYYYZZ..........",
            "..........syYYYYYYYYYZZ.........",
            ".........syYYYYyyyyyyyZZ........",
            ".........syYYYyZZZZZZZZZ........",
            ".........syYYyZZGGGGGGZZ........",
            ".........syYYyZgGGGGGGGZ........",
            "........ZYYYYyZGGGGGGGGZ........",
            "........ZYYYYyZGGGGGGGGZ........",
            ".........YYYYyZZGGGGGGZZ........",
            ".........YYYYYyZZZZZZZZZ........",
            "..........YYYYYYYYYYYZZ.........",
            ".........syYZYYZYYZYYZZZ........",
            "........syYYYYYYYYYYYYYZZ.......",
        };
        static readonly string[] HelmB =
        {
            "...............syYZ.............",
            "...............yYZZ.............",
            "................ZZ..............",
            ".............ssyYYZZ............",
            "...........ssyYYYYYYZZ..........",
            "..........syYYYYYYYYYZZ.........",
            ".........syYYYYYYYYYYYZZ........",
            ".........syYYYYYYYYYYYZZ........",
            ".........syYYYYYZZYYYYZZ........",
            ".........syYYYYZZZZYYYZZ........",
            "........ZYYYYYYZZZZYYYZZ........",
            "........ZYYYYYYYZZYYYYZZ........",
            ".........YYYYYYYYYYYYYZZ........",
            ".........YYYYYYYYYYYYYZZ........",
            "..........YYYYYYYYYYYZZ.........",
            ".........syYZYYZYYZYYZZZ........",
            "........syYYYYYYYYYYYYYZZ.......",
        };
        static readonly Vector2Int EyeL = new Vector2Int(16, 12), EyeR = new Vector2Int(20, 12);

        // The suit, rows 21..26 from x 7, the shoulders a pixel out from the breastplate: lit edge, suit,
        // shade; a seam down the chest to the belt's brass buckle; the hem. The back: a seam down the middle.
        static readonly string[] TorsoF =
        {
            "tuUUUUUUUUUUUUUUUVV",
            "tuUUUUUUuVUUUUUUUVV",
            ".uUUUUUUuVUUUUUUUV.",
            ".UUUUUUUuVUUUUUUUV.",
            ".ooooooooyZooooooo.",
            ".VVVVVVVVVVVVVVVVV.",
        };
        static readonly string[] TorsoB =
        {
            "tuUUUUUUUUUUUUUUUVV",
            "tuUUUUUUUVUUUUUUUVV",
            ".uUUUUUUUVUUUUUUUV.",
            ".UUUUUUUUVUUUUUUUV.",
            ".ooooooooZZooooooo.",
            ".VVVVVVVVVVVVVVVVV.",
        };

        // The arms at rest, rows 21..27: the sleeves are the suit's rounded shoulders, so only the big mitts show, hanging
        // from the shoulders' outer edge (rows 2-5). Far mitt from x 6, near from x 22.
        static readonly string[] FarArm =
        {
            ".....",
            ".....",
            ".syZ.",
            "syYYZ",
            "yYYZZ",
            ".ZZZ.",
            ".....",
        };
        static readonly string[] NearArm =
        {
            ".....",
            ".....",
            ".yYZ.",
            "yYYYZ",
            "YYYZZ",
            ".ZZZ.",
            ".....",
        };

        // Legs: the thigh (row 27, suit) and the boot (rows 28-30). Each pose: far boot dx, dy, near boot dx, dy;
        // a boot lifted (dy < 0) hides its thigh.
        static readonly Dictionary<string, int[]> Legs = new Dictionary<string, int[]>
        {
            { "stand",      new[] { 0, 0, 0, 0 } },
            { "liftNear",   new[] { 0, 0, 1, -1 } },
            { "liftFar",    new[] { 0, -1, 0, 0 } },
            { "strideNear", new[] { -1, 0, 1, 0 } },
            { "strideFar",  new[] { 1, 0, -1, 0 } },
            { "crouch",     new[] { -1, 0, 1, 0 } },
            { "dangle",     new[] { 0, 1, 0, 1 } },
            { "sit",        new[] { -2, 0, 2, 0 } },
        };

        // ---------------------------------------------------------------- frame composer

        public static Px Frame(int variant, Pose p)
        {
            bool back = variant >= 2;
            var c = new Px(32, 32);
            for (int i = -p.puddle; i < p.puddle; i++)
            {
                int a = Mathf.Abs(i + 0.5f) > p.puddle - 1.5f ? 35 : 65;
                c.Over(16 + i, 31, Col.A(C('d'), (byte)a));
            }
            Rings(c, p, true);
            var body = new Px(32, 32);
            DrawLegs(body, p);
            int ox = p.lean, oy = p.dy;
            if (back) NearArmPose(body, p, ox, oy, true);
            Stamp(body, back ? TorsoB : TorsoF, 7 + ox, BodyY + oy);
            Stamp(body, back ? HelmB : HelmF, ox, HelmY + oy + p.headDy);
            if (!back) Eyes(body, p, ox, oy + p.headDy);
            FarArmPose(body, p, ox, oy);
            if (!back) NearArmPose(body, p, ox, oy, false);
            c.Draw(body, 0, 0);
            Fx(c, p);
            if (p.grey)
                for (int i = 0; i < c.C.Length; i++) { var k = c.C[i]; c.C[i] = Col.Mix(k, Col.A(Col.H("8E8A84"), k.a), 0.45f); }
            return variant == 1 || variant == 3 ? c.FlipX() : c;
        }

        static void Stamp(Px c, string[] rows, int ox, int oy)
        {
            for (int y = 0; y < rows.Length; y++)
                for (int x = 0; x < rows[y].Length; x++)
                {
                    char k = rows[y][x];
                    if (k != '.') c.Set(ox + x, oy + y, Ink(k));
                }
        }

        static void DrawLegs(Px c, Pose p)
        {
            int[] l = Legs[p.legs];
            int bdy = p.air ? p.dy : 0, bdx = p.air ? p.lean : 0;
            int hem = 27 + (p.air ? 0 : Mathf.Min(0, p.dy));          // a raised body stretches the thighs up to the hem
            Leg(c, 12 + bdx + l[0], l[1] + bdy, hem + bdy, 'u', 'U', 'V');
            Leg(c, 18 + bdx + l[2], l[3] + bdy, hem + bdy, 'U', 'U', 'V');
        }

        static void Leg(Px c, int x, int dy, int top, char lit, char mid, char shade)
        {
            // thigh: three suit pixels from under the hem to the boot; boot: 4, 4 and a 2-pixel sole, a brass toe cap
            int by = 28 + dy;
            for (int y = top; y < by; y++) { c.Set(x, y, C(lit)); c.Set(x + 1, y, C(mid)); c.Set(x + 2, y, C(shade)); }
            for (int i = -1; i < 3; i++) { c.Set(x + i, by, C('o')); c.Set(x + i, by + 1, C('o')); }
            c.Set(x, by + 2, C('o')); c.Set(x + 1, by + 2, C('o'));
            c.Set(x + 2, by, C('Y')); c.Set(x + 2, by + 1, C('Z'));        // the brass toe cap
        }

        static void Eyes(Px c, Pose p, int ox, int oy)
        {
            // clear the glass where the eyes sit, then draw them: two square lamps against the porthole's top rim
            var glass = C('G');
            for (int y = 12; y <= 13; y++) for (int x = 16; x <= 21; x++) c.Set(x + ox, y + oy, glass);
            int dx = p.eyes == "left" ? -1 : p.eyes == "right" ? 1 : 0;
            Color32 e = p.eyes == "flash" ? C('W') : C('e');
            foreach (var at in new[] { EyeL, EyeR })
            {
                int x = at.x + dx + ox, y = at.y + oy;
                if (p.eyes == "closed") { c.Set(x, y + 1, C('g')); c.Set(x + 1, y + 1, C('g')); continue; }
                if (p.eyes == "happy") { c.Set(x, y + 1, e); c.Set(x + 1, y, e); continue; }
                c.Set(x, y, e); c.Set(x + 1, y, e); c.Set(x, y + 1, e); c.Set(x + 1, y + 1, e);
            }
        }

        // ---------------------------------------------------------------- arms

        static void FarArmPose(Px c, Pose p, int ox, int oy)
        {
            switch (p.arms)
            {
                case "up":
                case "cheer":
                    Arm(c, new Vector2(8 + ox, 22 + oy), new Vector2(5 + ox, p.arms == "up" ? 15 + oy : 13 + oy), true);
                    return;
                case "slump":
                    Arm(c, new Vector2(8 + ox, 22 + oy), new Vector2(7 + ox, 27 + oy), true);
                    return;
                default:
                    int sw = p.arms == "rest" ? -p.swing : 0;
                    StampSplit(c, FarArm, 6 + ox, BodyY + oy, sw, p.mittDy);
                    return;
            }
        }

        static void NearArmPose(Px c, Pose p, int ox, int oy, bool back)
        {
            switch (p.arms)
            {
                case "windup":
                    Arm(c, new Vector2(24 + ox, 22 + oy), new Vector2(26 + ox, 14 + oy), false);
                    Wrench(c, new Vector2(26 + ox, 13 + oy), new Vector2(0.15f, -1f), back);
                    return;
                case "smash":
                    Arm(c, new Vector2(24 + ox, 22 + oy), new Vector2(27 + ox, 19 + oy), false);
                    Wrench(c, new Vector2(27 + ox, 18 + oy), new Vector2(0.7f, -0.7f), back);
                    return;
                case "follow":
                    Arm(c, new Vector2(24 + ox, 22 + oy), new Vector2(27 + ox, 25 + oy), false);
                    Wrench(c, new Vector2(28 + ox, 26 + oy), new Vector2(0.75f, 0.65f), back);
                    return;
                case "ready":
                    StampSplit(c, NearArm, 22 + ox, BodyY + oy, 0, 0);
                    Wrench(c, new Vector2(25 + ox, 26 + oy), new Vector2(0.2f, 1f), back);
                    return;
                case "sonar":
                    Arm(c, new Vector2(24 + ox, 22 + oy), new Vector2(25 + ox, 15 + oy), false);
                    return;
                case "up":
                case "cheer":
                    Arm(c, new Vector2(24 + ox, 22 + oy), new Vector2(26 + ox, p.arms == "up" ? 15 + oy : 13 + oy), false);
                    return;
                case "slump":
                    Arm(c, new Vector2(24 + ox, 22 + oy), new Vector2(24 + ox, 27 + oy), false);
                    return;
                default:
                    StampSplit(c, NearArm, 22 + ox, BodyY + oy, p.arms == "rest" ? p.swing : 0, p.mittDy);
                    return;
            }
        }

        // The resting arm block: the mitt rows shift (walking swing, idle settle) against the suit behind them.
        static void StampSplit(Px c, string[] rows, int ox, int oy, int mittDx, int mittDy)
        {
            for (int y = 0; y < rows.Length; y++)
                for (int x = 0; x < rows[y].Length; x++)
                {
                    char k = rows[y][x];
                    if (k == '.') continue;
                    bool mitt = y >= 2;
                    c.Set(ox + x + (mitt ? mittDx : 0), oy + y + (mitt ? mittDy : 0), Ink(k));
                }
        }

        // A posed arm: a three-pixel sleeve from the shoulder to the hand (lit on its upper-left side, shaded on the
        // other), then a round brass mitt at the hand.
        static void Arm(Px c, Vector2 shoulder, Vector2 hand, bool far)
        {
            Vector2 d = hand - shoulder;
            float len = Mathf.Max(0.01f, d.magnitude);
            Vector2 n = new Vector2(-d.y, d.x) / len;
            if (n.x + n.y > 0) n = -n;                              // the normal points up-left, toward the light
            for (int y = (int)Mathf.Min(shoulder.y, hand.y) - 2; y <= (int)Mathf.Max(shoulder.y, hand.y) + 2; y++)
                for (int x = (int)Mathf.Min(shoulder.x, hand.x) - 2; x <= (int)Mathf.Max(shoulder.x, hand.x) + 2; x++)
                {
                    Vector2 q = new Vector2(x, y) - shoulder;
                    float t = Mathf.Clamp01(Vector2.Dot(q, d) / (len * len));
                    Vector2 r = q - d * t;
                    if (r.magnitude > 1.3f) continue;
                    float side = Vector2.Dot(r, n);
                    char k = side > 0.6f ? 'u' : side < -0.6f ? 'V' : 'U';
                    if (far && k == 'u') k = 'U';
                    c.Set(x, y, C(k));
                }
            int hx = Mathf.RoundToInt(hand.x), hy = Mathf.RoundToInt(hand.y);
            string[] mitt = { ".yY.", "yYYZ", "YYZZ", ".ZZ." };
            Stamp(c, mitt, hx - 1, hy - 1);
        }

        // The wrench: a steel handle from the mitt and an open jaw at its end.
        static void Wrench(Px c, Vector2 hand, Vector2 dir, bool behind)
        {
            dir = dir / dir.magnitude;
            Vector2 perp = new Vector2(-dir.y, dir.x);
            for (int i = 1; i <= 6; i++)
            {
                var q = hand + dir * i;
                c.Set(Mathf.RoundToInt(q.x), Mathf.RoundToInt(q.y), C('F'));
            }
            var end = hand + dir * 7;
            Action<Vector2, char> put = (q, k) => c.Set(Mathf.RoundToInt(q.x), Mathf.RoundToInt(q.y), C(k));
            put(end, 'F');
            put(end + perp, 'S'); put(end - perp, 'F');
            put(end + perp + dir, 'S'); put(end - perp + dir, 'S');
            put(end + perp + dir * 2, 'S'); put(end - perp + dir * 2, 'F');
            put(hand + dir * 3 + perp * 0.6f, 'S');               // a glint along the handle
        }

        // ---------------------------------------------------------------- effects

        static void Rings(Px c, Pose p, bool behind)
        {
            foreach (float r in p.rings)
            {
                byte a = (byte)Mathf.Clamp(260 - r * 14f, 60, 230);
                var col = Col.A(C('e'), a);
                Vector2 o = new Vector2(19f + p.lean, 14.5f + p.dy + p.headDy);
                for (int y = 0; y < 32; y++)
                    for (int x = 0; x < 32; x++)
                    {
                        float d = (new Vector2(x + 0.5f, y + 0.5f) - o).magnitude;
                        if (Mathf.Abs(d - r) < 0.55f) c.Over(x, y, col);
                    }
            }
        }

        static void Fx(Px c, Pose p)
        {
            foreach (var f in p.fx)
                switch (f.z)
                {
                    case FxBubble: c.Over(f.x, f.y, Col.A(C('e'), 210)); break;
                    case FxBubbleBig:
                        c.Over(f.x, f.y - 1, Col.A(C('e'), 200)); c.Over(f.x - 1, f.y, Col.A(C('e'), 200));
                        c.Over(f.x + 1, f.y, Col.A(C('e'), 160)); c.Over(f.x, f.y + 1, Col.A(C('e'), 160));
                        c.Over(f.x - 1, f.y - 1, Col.A(C('W'), 200));
                        break;
                    case FxDust: c.Over(f.x, f.y, Col.A(C('w'), 190)); break;
                    case FxSpark: c.Over(f.x, f.y, C('W')); c.Over(f.x + 1, f.y - 1, Col.A(C('W'), 150)); c.Over(f.x - 1, f.y + 1, Col.A(C('W'), 150)); break;
                    case FxSwoosh: c.Over(f.x, f.y, Col.A(C('W'), 170)); break;
                }
        }

        // A swoosh: the wrench's arc from one angle to another around the shoulder (0 = right, 90 = down).
        static void Arc(Pose p, float cx, float cy, float r, float a0, float a1)
        {
            for (float a = a0; a <= a1; a += 6f)
            {
                float rad = a * Mathf.Deg2Rad;
                p.fx.Add(new Vector3Int(Mathf.RoundToInt(cx + Mathf.Cos(rad) * r), Mathf.RoundToInt(cy + Mathf.Sin(rad) * r), FxSwoosh));
            }
        }

        // ---------------------------------------------------------------- animations

        // Breathing: the helmet settles, the mitts follow a beat later, the eyes blink, the valve lets out a bubble.
        public static List<Pose> Idle()
        {
            var list = new List<Pose>();
            int[] head = { 0, 0, 1, 1, 0, 0 };
            int[] mitt = { 0, 0, 0, 1, 1, 0 };
            string[] eyes = { "open", "open", "open", "open", "closed", "open" };
            for (int f = 0; f < 6; f++)
            {
                var p = new Pose { headDy = head[f], mittDy = mitt[f], eyes = eyes[f] };
                if (f == 1) p.fx.Add(new Vector3Int(16, 3, FxBubble));
                if (f == 2) p.fx.Add(new Vector3Int(17, 1, FxBubble));
                if (f == 3) p.fx.Add(new Vector3Int(18, 0, FxBubble));
                list.Add(p);
            }
            return list;
        }

        // A heavy stomping walk: the body bobs, the mitts swing against the boots. Contacts (stomps) on frames 2 and 5.
        public static List<Pose> Move()
        {
            return new List<Pose>
            {
                new Pose { dy = -1, legs = "liftNear", swing = -1, puddle = 11 },
                new Pose { dy = -1, legs = "liftNear", swing = -1, headDy = 1, puddle = 11 },
                new Pose { dy = 0, legs = "strideNear", swing = 0, puddle = 13 },
                new Pose { dy = -1, legs = "liftFar", swing = 1, puddle = 11 },
                new Pose { dy = -1, legs = "liftFar", swing = 1, headDy = 1, puddle = 11 },
                new Pose { dy = 0, legs = "strideFar", swing = 0, puddle = 13 },
            };
        }

        // The wrench swing (the stock melee attack; it carries to a second foe): wind up overhead, smash forward in a
        // wide arc, follow through low, recover.
        public static List<Pose> Attack()
        {
            var wind = new Pose { arms = "windup", lean = -1, headDy = -1 };
            var smash = new Pose { arms = "smash", lean = 1, legs = "strideNear" };
            Arc(smash, 24, 21, 9, -80, -10);
            var follow = new Pose { arms = "follow", lean = 1, dy = 1, legs = "strideNear" };
            Arc(follow, 24, 21, 9, -30, 50);
            follow.fx.Add(new Vector3Int(30, 28, FxSpark));
            var ready = new Pose { arms = "ready" };
            return new List<Pose> { wind, smash, follow, ready };
        }

        // SONAR: a mitt to the helmet, the eyes flash, rings ping out from the porthole.
        public static List<Pose> Sonar()
        {
            var a = new Pose { arms = "sonar" };
            var b = new Pose { arms = "sonar", eyes = "flash" }; b.rings.Add(4f);
            var c = new Pose { arms = "sonar", eyes = "flash" }; c.rings.Add(8f);
            var d = new Pose { arms = "sonar" }; d.rings.Add(12f); d.rings.Add(5f);
            var e = new Pose { arms = "sonar" }; e.rings.Add(16f); e.rings.Add(9f);
            var f = new Pose(); f.rings.Add(13f);
            return new List<Pose> { a, b, c, d, e, f };
        }

        public static List<Px> Fall()
        {
            var frames = new List<Px>();
            for (int f = 0; f < 12; f++)
            {
                var p = new Pose { dy = -1, air = true, legs = "dangle", arms = "up", headDy = f % 4 < 2 ? 0 : -1, eyes = f % 6 == 5 ? "closed" : "open", puddle = 0 };
                p.fx.Add(new Vector3Int(8 + (f * 2) % 6, 6 - (f % 6), FxBubble));
                p.fx.Add(new Vector3Int(23 - (f % 3), 9 - (f * 2) % 8, f % 2 == 0 ? FxBubbleBig : FxBubble));
                frames.Add(Frame(0, p));
            }
            return frames;
        }

        // Landing: drops in, lands in a crouch in a puff of dust, rises, looks about, blinks, lets out a bubble.
        public static List<Px> Land()
        {
            var poses = new List<Pose>
            {
                new Pose { dy = -12, air = true, legs = "dangle", arms = "up", puddle = 3 },
                new Pose { dy = -5, air = true, legs = "dangle", arms = "up", puddle = 6 },
                Puff(new Pose { dy = 2, legs = "crouch", arms = "up", eyes = "closed", puddle = 15 }, 0),
                Puff(new Pose { dy = 1, legs = "crouch", eyes = "closed", puddle = 14 }, 1),
                Puff(new Pose { dy = 1, legs = "crouch", puddle = 13 }, 2),
                new Pose(),
                new Pose { eyes = "left" },
                new Pose { eyes = "left", headDy = 1 },
                new Pose { eyes = "right" },
                new Pose { eyes = "closed" },
                Bubble(new Pose(), 0),
                Bubble(new Pose(), 1),
            };
            var frames = new List<Px>();
            foreach (var p in poses) frames.Add(Frame(0, p));
            return frames;
        }

        static Pose Puff(Pose p, int t)
        {
            int[,] bits = { { 6, 30 }, { 26, 30 }, { 4, 28 }, { 28, 28 }, { 8, 27 }, { 24, 27 } };
            for (int i = 0; i < bits.GetLength(0); i++)
            {
                int x = bits[i, 0] + (bits[i, 0] < 16 ? -t : t);
                int y = bits[i, 1] - (t < 2 ? t * 2 : 4 - (t - 1) * 3);
                p.fx.Add(new Vector3Int(x, Mathf.Min(30, y), FxDust));
            }
            return p;
        }

        static Pose Bubble(Pose p, int t)
        {
            p.fx.Add(new Vector3Int(16 + t, 3 - t * 2, t == 0 ? FxBubble : FxBubbleBig));
            return p;
        }

        // ---------------------------------------------------------------- sheets

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

        // The same diver at the boat's size: the dome with its porthole on the near half, the collar, the suit with the
        // mitts on the hips, the boots. Back: a plate instead of the porthole.
        static readonly string[] MiniRows =
        {
            "................",
            "................",
            "................",
            "................",
            ".......sZ.......",
            ".....syYYYZ.....",
            "....syYYZZZZ....",
            "....yYYZGeGeZ...",
            "....YYYZGGGGZ...",
            "....YYYYZZZZY...",
            "...syYYYYYYYYZ..",
            "..tuUUUUUUUUUV..",
            "..syYZUUUUUVyYZ.",
            "..yYYZooooooYYZ.",
            "...ZZ.uUV.UUVZ..",
            "......oo..oo....",
        };

        static Px Mini(bool back, int dy, bool blink, int step, bool flip)
        {
            var c = new Px(16, 16);
            for (int x = 3; x < 13; x++) c.Over(x, 15, Col.A(C('d'), (byte)(x == 3 || x == 12 ? 35 : 60)));
            for (int y = 0; y < MiniRows.Length; y++)
                for (int x = 0; x < 16; x++)
                {
                    char k = MiniRows[y][x];
                    if (k == '.') continue;
                    if (back && y >= 6 && y <= 9 && x >= 7 && (k == 'G' || k == 'e' || k == 'Z'))
                        k = k == 'Z' && (x == 12 || MiniRows[y][x + 1] == '.') ? 'Z' : 'Y';          // the dome's edge keeps its shade
                    if (!back && blink && k == 'e') k = 'G';
                    // idle settles the body over planted boots; walking hops the whole figure, one boot trailing
                    int yy = y + (dy > 0 && y >= 14 ? 0 : dy);
                    if (y == 15) { if (step == 1 && x <= 7) yy -= 1; if (step == 2 && x >= 9) yy -= 1; }
                    c.Set(x, yy, Ink(k));
                }
            if (back) { c.Set(8, 7 + dy, C('Z')); c.Set(9, 7 + dy, C('Z')); c.Set(8, 8 + dy, C('Z')); c.Set(9, 8 + dy, C('Z')); }
            return flip ? c.FlipX() : c;
        }

        public static Px MiniSheet()
        {
            var rows = new List<List<Px>>();
            int[] idleDy = { 0, 0, 1, 1, 0, 0 };
            bool[] blink = { false, false, false, false, true, false };
            int[] walkDy = { -1, -1, 0, -1, -1, 0 };
            int[] step = { 1, 1, 0, 2, 2, 0 };
            for (int v = 0; v < 4; v++)
            {
                var r = new List<Px>();
                for (int f = 0; f < 6; f++) r.Add(Mini(v >= 2, idleDy[f], blink[f], 0, v == 1 || v == 3));
                rows.Add(r);
            }
            for (int v = 0; v < 4; v++)
            {
                var r = new List<Px>();
                for (int f = 0; f < 6; f++) r.Add(Mini(v >= 2, walkDy[f], false, step[f], v == 1 || v == 3));
                rows.Add(r);
            }
            return Px.Sheet(rows, 16, 16);
        }

        // ---------------------------------------------------------------- Nauts (16x16)

        // Argo's crew in his finish: a little brass helmet with a porthole and a crew-colour plume, a crew-colour suit,
        // brass mitts, dark boots. Sheet: rows v0..v3, six frames. NautSheet walks; NautTuneSheet taps a wrench on the
        // tower it tunes, with a spark on the tap.
        static readonly string[] NautRows =
        {
            "......cC........",
            ".....syYZ.......",
            "....syZZZZ......",
            "....YZeGeZ......",
            "....YYZZZZ......",
            "...syYYYYYZ.....",
            "...yccccccy.....",
            "...YcccccCY.....",
            "....oooooo......",
            "....oo..oo......",
        };

        static Px Naut(char main, char shade, bool back, int dy, int step, int tap)
        {
            var c = new Px(16, 16);
            for (int x = 3; x < 12; x++) c.Over(x, 15, Col.A(C('d'), (byte)(x == 3 || x == 11 ? 35 : 60)));
            const int oy = 5;
            for (int y = 0; y < NautRows.Length; y++)
                for (int x = 0; x < 16; x++)
                {
                    char k = NautRows[y][x];
                    if (k == '.') continue;
                    if (back && y >= 2 && y <= 4 && x >= 5 && (k == 'Z' || k == 'G' || k == 'e')) k = x >= 9 ? 'Z' : 'Y';
                    Color32 col = k == 'c' ? Crew[main] : k == 'C' ? Crew[shade] : Ink(k);
                    int yy = oy + y + (y >= 9 ? 0 : dy);
                    if (y >= 9 && (step == 1 && x <= 5 || step == 2 && x >= 8)) yy -= 1;
                    c.Set(x, yy, col);
                }
            if (tap > 0 && !back)
            {
                // the wrench in the near mitt: raised (1), half (2), tapping down with a spark (3)
                int hx = 10, hy = oy + 6 + dy;
                if (tap == 1) { c.Set(hx + 1, hy - 1, C('F')); c.Set(hx + 2, hy - 2, C('F')); c.Set(hx + 2, hy - 3, C('S')); c.Set(hx + 3, hy - 3, C('S')); c.Set(hx + 3, hy - 2, C('F')); }
                else if (tap == 2) { c.Set(hx + 1, hy - 1, C('F')); c.Set(hx + 2, hy - 1, C('F')); c.Set(hx + 3, hy - 2, C('S')); c.Set(hx + 3, hy - 1, C('S')); c.Set(hx + 4, hy - 1, C('F')); }
                else { c.Set(hx + 1, hy, C('F')); c.Set(hx + 2, hy, C('F')); c.Set(hx + 3, hy, C('S')); c.Set(hx + 3, hy + 1, C('S')); c.Set(hx + 4, hy, C('F')); if (tap == 3) { c.Over(hx + 5, hy - 1, C('W')); c.Over(hx + 5, hy + 1, Col.A(C('W'), 170)); c.Over(hx + 6, hy, Col.A(C('W'), 140)); } }
            }
            return c;
        }

        public static Px NautSheet(char main, char shade)
        {
            int[] hop = { 0, -1, 0, 0, -1, 0 };
            int[] step = { 0, 1, 0, 0, 2, 0 };
            var rows = new List<List<Px>>();
            for (int v = 0; v < 4; v++)
            {
                var r = new List<Px>();
                for (int f = 0; f < 6; f++) { var d = Naut(main, shade, v >= 2, hop[f], step[f], 0); r.Add(v == 1 || v == 3 ? d.FlipX() : d); }
                rows.Add(r);
            }
            return Px.Sheet(rows, 16, 16);
        }

        public static Px NautTuneSheet(char main, char shade)
        {
            int[] tap = { 1, 2, 4, 3, 2, 1 };
            int[] bob = { 0, 0, 1, 1, 0, 0 };
            var rows = new List<List<Px>>();
            for (int v = 0; v < 4; v++)
            {
                var r = new List<Px>();
                for (int f = 0; f < 6; f++) { var d = Naut(main, shade, v >= 2, bob[f], 0, tap[f]); r.Add(v == 1 || v == 3 ? d.FlipX() : d); }
                rows.Add(r);
            }
            return Px.Sheet(rows, 16, 16);
        }

        // ---------------------------------------------------------------- UI art

        public static Px Icon() { return Frame(0, new Pose()); }

        // Journal tab icon in the game's Vesselicons style: a 32x32 white line drawing, 1 px strokes, open interior,
        // traced from the standing frame: the outline, the porthole's ring and eyes, the collar.
        public static Px TabIcon()
        {
            var f = Frame(0, new Pose { puddle = 0 });
            var white = new Color32(255, 255, 255, 255);
            var c = new Px(32, 32);
            Func<int, int, bool> solid = (x, y) => f.Get(x, y).a >= 140;
            for (int y = 0; y < 32; y++)
                for (int x = 0; x < 32; x++)
                {
                    if (!solid(x, y)) continue;
                    bool edge = !solid(x - 1, y) || !solid(x + 1, y) || !solid(x, y - 1) || !solid(x, y + 1);
                    if (edge) c.Set(x, y, white);
                }
            // the porthole's ring, the eyes, the collar line
            for (int x = 15; x <= 22; x++) { c.Set(x, 11, white); c.Set(x, 17, white); }
            for (int y = 12; y <= 16; y++) { c.Set(14, y, white); c.Set(23, y, white); }
            foreach (int ex in new[] { 16, 20 })
                for (int y = 12; y <= 13; y++) { c.Set(ex, y, white); c.Set(ex + 1, y, white); }
            for (int x = 8; x <= 24; x++) c.Set(x, 21, white);
            return c;
        }

        public static Px EndWin()
        {
            var p = new Pose { arms = "cheer", eyes = "happy", headDy = -1 };
            p.fx.Add(new Vector3Int(5, 6, FxBubbleBig)); p.fx.Add(new Vector3Int(27, 4, FxBubble)); p.fx.Add(new Vector3Int(20, 1, FxBubbleBig));
            p.fx.Add(new Vector3Int(3, 12, FxBubble)); p.fx.Add(new Vector3Int(28, 9, FxBubbleBig));
            return Frame(0, p);
        }

        public static Px EndLose()
        {
            return Frame(0, new Pose { dy = 2, headDy = 1, legs = "sit", arms = "slump", eyes = "closed", grey = true });
        }

        // 12x12 ready-up icon: the diver, round dome, porthole and mitts, inside white corner brackets.
        public static Px ReadyUp()
        {
            var rows = new[]
            {
                "............",
                ".WW..sZ..WW.",
                ".W..syYZZ.W.",
                "...syYZZZZ..",
                "...yYZeGeZ..",
                "...YYZGGGZ..",
                "...YYYZZZZ..",
                "..syYZYZYZZ.",
                "..tuUUUUUVV.",
                ".WyYUUUVyZW.",
                ".WW.oo.ooWW.",
                "............",
            };
            var c = new Px(12, 12);
            for (int y = 0; y < 12; y++)
                for (int x = 0; x < 12; x++)
                {
                    char k = rows[y][x];
                    if (k != '.') c.Set(x, y, Ink(k));
                }
            return c;
        }

        // ---------------------------------------------------------------- splash (128x128 line art)

        // The shipped splashes are line art: a two-tone frame, then one shape per body part, back to front, each filled
        // with a translucent accent, stroked outside in the solid accent and traced inside with the part's own colour.
        // Argo's parts follow the standing frame at four times its size: the boots, the suit, the mitts, the dome's valve
        // and flange, the round dome, the porthole; then the glass and the eyes.
        public static Px Splash()
        {
            const int S = 128;
            Color32 accent = Col.H("8FC6E0");
            Color32 fill = Col.A(accent, 70);
            var c = new Px(S, S);
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    int d = Mathf.Min(Mathf.Min(x, y), Mathf.Min(S - 1 - x, S - 1 - y));
                    if (d < 2) c.Set(x, y, fill);
                    else if (d < 4) c.Set(x, y, accent);
                }
            Func<float, float, Vector2> V = (x, y) => new Vector2(x, y);
            var parts = new List<SplashPart>
            {
                // the boots
                SplashPart.Capsule(V(50, 113), V(56, 113), 6f, C('o'), C('o')),
                SplashPart.Capsule(V(72, 113), V(78, 113), 6f, C('o'), C('o')),
                // the suit: rounded shoulders down to the belt
                SplashPart.Box(V(64, 96), 27f, 13f, 7f, C('U'), C('V')),
                // the big mitts on the hips
                SplashPart.Capsule(V(38, 102), V(41, 104), 8f, C('Y'), C('Y')),
                SplashPart.Capsule(V(87, 102), V(90, 104), 8f, C('Z'), C('Z')),
                // the valve on the crown, then the flange and the round dome
                SplashPart.Capsule(V(64, 20), V(64, 28), 4f, C('Z'), C('Z')),
                SplashPart.Capsule(V(59, 16), V(69, 16), 6f, C('Y'), C('Z')),
                SplashPart.Capsule(V(40, 80), V(88, 80), 6f, C('Y'), C('Z')),
                SplashPart.Circle(V(64, 54), 26f, C('Y'), C('Z'), 77f),
                // the porthole on the near half
                SplashPart.Box(V(71, 55), 15f, 12f, 6f, C('Z'), C('Z')),
            };
            foreach (var part in parts) part.Draw(c, accent, fill);
            // the glass and the two square eyes
            for (int y = 47; y <= 62; y++)
                for (int x = 61; x <= 81; x++) c.Set(x, y, C('G'));
            foreach (int ex in new[] { 63, 74 })
                for (int y = 47; y <= 53; y++) for (int x = ex; x < ex + 6; x++) c.Set(x, y, C('e'));
            return c;
        }

        class SplashPart
        {
            public Func<float, float, bool> Inside;
            public Color32 Line, Shade;
            public float ShadeFromX;

            public static SplashPart Capsule(Vector2 a, Vector2 b, float r, Color32 line, Color32 shade)
            {
                return new SplashPart
                {
                    Inside = (x, y) =>
                    {
                        Vector2 p = new Vector2(x, y), ab = b - a;
                        float t = ab.sqrMagnitude < 1e-4f ? 0f : Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
                        return (a + ab * t - p).magnitude <= r;
                    },
                    Line = line, Shade = shade, ShadeFromX = (a.x + b.x) / 2f + r * 0.4f
                };
            }

            public static SplashPart Circle(Vector2 o, float r, Color32 line, Color32 shade, float shadeFromX)
            {
                return new SplashPart { Inside = (x, y) => (new Vector2(x, y) - o).magnitude <= r, Line = line, Shade = shade, ShadeFromX = shadeFromX };
            }

            // A rounded box: centre, half width, half height, corner radius.
            public static SplashPart Box(Vector2 o, float hw, float hh, float cr, Color32 line, Color32 shade)
            {
                return new SplashPart
                {
                    Inside = (x, y) =>
                    {
                        float dx = Mathf.Max(0f, Mathf.Abs(x - o.x) - (hw - cr)), dy = Mathf.Max(0f, Mathf.Abs(y - o.y) - (hh - cr));
                        return dx * dx + dy * dy <= cr * cr;
                    },
                    Line = line, Shade = shade, ShadeFromX = o.x + hw * 0.5f
                };
            }

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
