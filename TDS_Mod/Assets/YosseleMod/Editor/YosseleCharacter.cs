// Yossele asset generator: the vessel's pixel art. TEMPORARY ART built from Dimitri's own sprite.
// Baseline: his reshade (concepts/vessels_2026-10-09/v3_flat/game_size/yossele_reshade.png) and its back view
// (concepts/vessels_2026-10-09/back/game_size/yossele_back.png), embedded below pixel for pixel as Front / Back.
// Every 32 px frame is those pixels moved, squashed, with limbs shifted, or with pixels added (dust, compost, leaves):
// the sprite is never redrawn (the 16 px boat mini below is drawn new at 10 x 13, and the 32 px journal tab icon is a
// line drawing of the splash's shapes). Finish (round 3 "flat"): fully opaque, one shade band down the right edge, no
// half-alpha rim, no ground patch, no highlights.
// Yossele: a round clay head with a see-through hole for a mouth, a two-leaf sprout on a stalk, glowing green eyes, a dark
// collar, and four long root-like limbs. The outer two limbs double as arms (the compost). It shoots a green laser from
// the hole: the attack frames light the hole up.
// 32x32 frames, PPU 20, bottom-centre pivot. Variants: v0 front, v1 front mirrored, v2 back, v3 back mirrored.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace YosseleBuild
{
    public class Pose
    {
        public int dy;                                // head and body (everything above the legs), pixels, + = down
        public int headDy;                            // the head against the body (+ sinks into the collar)
        public int sproutDx, sproutDy;                // the two sprout leaves (the stalk stays with the head)
        public int leafDy;                            // the shoulder leaf
        public string eyes = "on";                    // on, blink, dim, off
        public int[] lift = { 0, 0, 0, 0 };           // legs: outer left, inner left, inner right, outer right (+ = up)
        public int[] legDx = { 0, 0, 0, 0 };
        public int armL, armR;                        // outer limbs as arms: 0 = a leg, else YosseleCharacter.Arm*
        public int[] pathR;                           // explicit right-arm path (x,y pairs), overrides armR
        public int compost;                           // 0 none, 1 clump in the right hand, 2 clump pressed on the ground
        public int mouth;                             // the hole's glow: 0 none, 1 dim, 2 glowing, 3 firing
        public bool air;                              // airborne: the legs move with the body instead of stretching
        public readonly List<Vector3Int> fx = new List<Vector3Int>();
    }

    public static class YosseleCharacter
    {
        public const int ArmUp = 1, ArmHigh = 2, ArmSlam = 3, ArmLimp = 4;
        public const int FxDust = 1, FxLeaf = 2, FxLeafDark = 3, FxCompost = 4, FxSpark = 5, FxFlower = 6, FxImpact = 7, FxCrack = 8, FxGlow = 9;

        // The reshade's own colours (a b c d e f g), then the colours the frames add.
        public static readonly Dictionary<char, Color32> P = new Dictionary<char, Color32>
        {
            { 'a', Col.H("945743") }, { 'b', Col.H("79432F") },                            // clay, clay shade
            { 'c', Col.H("3E4943") }, { 'g', Col.H("35403A") },                            // collar, collar shade
            { 'd', Col.H("7BCF80") }, { 'e', Col.H("65AD69") },                            // sprout leaf, leaf shade
            { 'f', Col.H("99E550") },                                                      // glowing eyes
            // added: compost, dust, flower, impact
            { 'k', Col.H("4E3524") }, { 'K', Col.H("6E4B30") }, { 'u', Col.H("B79E7E") }, { 'U', Col.H("9A8166") },
            { 'y', Col.H("F2D94A") }, { 'w', Col.H("FFFFFF") }, { 'l', Col.H("72CC35") }, { 'L', Col.H("6ABE30") },
        };

        static Color32 C(char k) { return P[k]; }

        // Dimitri's reshade, front (v0). Rows 11-12 sprout leaves, 13-21 stalk and head, 22-26 collar and body (the shoulder
        // leaf at x 9-10), 27-31 the four limbs.
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
            "..................d.............",
            "..................ed............",
            ".................b..............",
            "................bb..............",
            ".............aabbbb.............",
            "............aaaabbab............",
            "...........aaaaaabfab...........",
            "...........abfb..bbab...........",
            "...........aab....bab...........",
            "...........aaab..bbab...........",
            "............aaaaabab............",
            "..........d..caaabg.............",
            ".........de.abccccbb............",
            "...........aaabccbaab...........",
            "...........aaaabcaaab...........",
            "..........aaaaaabaaaab..........",
            "..........ab.aaaaab.ab..........",
            "..........ab.ab..ab.ab..........",
            ".........ab..ab..ab..ab.........",
            ".........ab..ab..ab..ab.........",
            ".........ab..ab..ab..ab.........",
        };

        // His back view (v2): no eyes, the hole stays, sprout and shoulder leaf on the other side, the collar a band.
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
            ".............d..................",
            "............de..................",
            "..............b.................",
            "..............bb................",
            ".............abbaab.............",
            "............aaaaaaab............",
            "...........aaaaaaaaab...........",
            "...........aaab..aaab...........",
            "...........aab....aab...........",
            "...........aaab..aaab...........",
            "............aaaaaaab............",
            ".............gggggg..d..........",
            "............abcccgbb.ed.........",
            "...........aaabbbbaab...........",
            "...........aaaaaaaaab...........",
            "..........aaaaaaaaaaab..........",
            "..........ab.aaaaab.ab..........",
            "..........ab.ab..ab.ab..........",
            ".........ab..ab..ab..ab.........",
            ".........ab..ab..ab..ab.........",
            ".........ab..ab..ab..ab.........",
        };

        const int HeadTop = 13, HeadBottom = 21, BodyTop = 22, BodyBottom = 26, LegTop = 27;

        // One part of the sprite: the source pixels that pass 'keep', with an optional colour swap (the eyes).
        static Px Part(string[] rows, Func<int, int, char, bool> keep, Func<char, char> swap = null)
        {
            var p = new Px(32, 32);
            for (int y = 0; y < 32; y++)
                for (int x = 0; x < 32; x++)
                {
                    char k = rows[y][x];
                    if (k == '.' || !keep(x, y, k)) continue;
                    if (swap != null) k = swap(k);
                    p.Set(x, y, C(k));
                }
            return p;
        }

        static bool IsLeaf(char k) { return k == 'd' || k == 'e'; }
        static int LegOf(int x) { return x <= 11 ? 0 : x <= 15 ? 1 : x <= 19 ? 2 : 3; }

        // ---------------------------------------------------------------- frame composer

        public static Px Frame(int variant, Pose p)
        {
            bool back = variant >= 2;
            string[] src = back ? Back : Front;
            var c = new Px(32, 32);

            char eye = p.eyes == "blink" ? 'b' : p.eyes == "dim" ? 'e' : p.eyes == "off" ? 'g' : 'f';
            var head = Part(src, (x, y, k) => y >= HeadTop && y <= HeadBottom, k => k == 'f' ? eye : k);
            var sprout = Part(src, (x, y, k) => y < HeadTop);
            var body = Part(src, (x, y, k) => y >= BodyTop && y <= BodyBottom && !(y <= BodyTop + 1 && IsLeaf(k)));
            var leaf = Part(src, (x, y, k) => y >= BodyTop && y <= BodyTop + 1 && IsLeaf(k));

            // Legs (the outer two stay off when they are arms). When the body rises, each leg's top row stretches up.
            for (int i = 0; i < 4; i++)
            {
                if (i == 0 && p.armL != 0 && p.armL != ArmLimp) continue;
                if (i == 3 && (p.armR != 0 && p.armR != ArmLimp || p.pathR != null)) continue;
                int li = i;
                var leg = Part(src, (x, y, k) => y >= LegTop && LegOf(x) == li);
                if (p.air) { c.Draw(leg, p.legDx[i], p.dy - p.lift[i]); continue; }
                c.Draw(leg, p.legDx[i], -p.lift[i]);
                int topY = LegTop - p.lift[i];
                for (int y = topY - 1; y >= LegTop + p.dy && y >= 0; y--)
                    for (int x = 0; x < 32; x++)
                        if (leg.Opaque(x, LegTop)) c.Set(x + p.legDx[i], y, leg.Get(x, LegTop));
            }

            // Head first, then the body over it, so a sinking head tucks into the collar; a rising head stretches its
            // bottom row down to the collar.
            int hy = p.dy + p.headDy;
            c.Draw(head, 0, hy);
            for (int k = 1; k <= -p.headDy; k++)
                for (int x = 0; x < 32; x++)
                    if (head.Opaque(x, HeadBottom)) c.Set(x, HeadBottom + hy + k, head.Get(x, HeadBottom));
            if (p.mouth > 0) Mouth(c, p.mouth, hy);
            c.Draw(body, 0, p.dy);
            c.Draw(leaf, 0, p.dy + p.leafDy);
            c.Draw(sprout, p.sproutDx, hy + p.sproutDy);

            // Arms: the outer limbs lifted off the ground, drawn two pixels wide (clay, shade on the right) from the shoulder.
            if (p.armL != 0 && p.armL != ArmLimp) Limb(c, ArmPath(p.armL), p.dy, false);
            if (p.pathR != null) Limb(c, p.pathR, p.dy, true, false, p.compost != 1);
            else if (p.armR != 0 && p.armR != ArmLimp) Limb(c, ArmPath(p.armR), p.dy, true);

            if (p.compost == 1 && p.pathR != null)
            {
                int n = p.pathR.Length;
                Compost(c, p.pathR[n - 2] + 1, p.pathR[n - 1] + p.dy - 2, false);
            }
            if (p.compost == 2) Compost(c, 25, 30, true);

            foreach (var f in p.fx) Fx(c, f);
            return variant == 1 || variant == 3 ? c.FlipX() : c;
        }

        // Left-arm paths, shoulder first, at rest height (the right arm is the mirror: x -> 30 - x).
        static int[] ArmPath(int arm)
        {
            switch (arm)
            {
                case ArmUp: return new[] { 10, 25, 9, 24, 8, 23, 7, 22, 7, 21, 6, 20, 6, 19 };
                case ArmHigh: return new[] { 10, 24, 9, 23, 9, 22, 9, 21, 9, 20, 9, 19, 9, 18, 9, 17, 9, 16, 9, 15, 9, 14, 9, 13 };
                case ArmSlam: return new[] { 10, 25, 9, 26, 8, 27, 7, 28, 6, 29, 5, 30, 5, 31 };
            }
            return null;
        }

        // A root limb: two pixels per step (clay, then shade), with a little split at the tip.
        static void Limb(Px c, int[] path, int dy, bool right, bool mirror = true, bool fork = true)
        {
            int n = path.Length / 2;
            int lastX = 0, lastY = 0, prevX = 0, prevY = 0;
            for (int i = 0; i < n; i++)
            {
                int x = path[i * 2], y = path[i * 2 + 1] + dy;
                if (right && mirror) x = 30 - x;
                if (i > 0)
                {
                    // fill diagonal steps so the limb stays solid
                    int sx = Math.Sign(x - lastX), sy = Math.Sign(y - lastY);
                    int cx = lastX, cy = lastY;
                    while (cx != x || cy != y)
                    {
                        if (cx != x) cx += sx;
                        if (cy != y) cy += sy;
                        c.Set(cx, cy, C('a')); c.Set(cx + 1, cy, C('b'));
                    }
                }
                c.Set(x, y, C('a')); c.Set(x + 1, y, C('b'));
                prevX = lastX; prevY = lastY;
                lastX = x; lastY = y;
            }
            // root tip: a one-pixel split past the end
            if (!fork) return;
            int dx = Math.Sign(lastX - prevX), dyy = Math.Sign(lastY - prevY);
            if (dyy == 0 && dx == 0) dyy = -1;
            if (Math.Abs(dyy) >= Math.Abs(dx))
            {
                c.Set(lastX - 1, lastY + dyy, C('b'));
                c.Set(lastX + 2, lastY + dyy, C('b'));
            }
            else
            {
                c.Set(lastX + (dx > 0 ? 2 : -1), lastY - 1, C('b'));
                c.Set(lastX + (dx > 0 ? 2 : -1), lastY + 1, C('b'));
            }
        }

        // The see-through hole (the same pixels front and back) filled with the laser's light: the middle two pixels at
        // 'dim', the whole hole at 'glowing' with a bright middle, bright with a white middle when firing.
        static readonly int[] HolePx = { 15, 18, 16, 18, 14, 19, 15, 19, 16, 19, 17, 19, 15, 20, 16, 20 };

        static void Mouth(Px c, int level, int hy)
        {
            for (int i = 0; i < HolePx.Length; i += 2)
            {
                int x = HolePx[i], y = HolePx[i + 1];
                bool middle = y == 19 && (x == 15 || x == 16);
                char k = level == 1 ? (middle ? 'L' : '.') : level == 2 ? (middle ? 'f' : 'l') : (middle ? 'w' : 'f');
                if (k != '.') c.Set(x, y + hy, C(k));
            }
        }

        // A handful of compost: dark soil with a green speck; 'pressed' lies flat on the ground.
        static void Compost(Px c, int x, int y, bool pressed)
        {
            if (pressed)
            {
                foreach (var q in new[] { new[] { 23, 31, 'K' }, new[] { 24, 31, 'k' }, new[] { 25, 31, 'k' }, new[] { 26, 31, 'k' }, new[] { 27, 31, 'K' },
                                          new[] { 24, 30, 'K' }, new[] { 25, 30, 'k' }, new[] { 26, 30, 'K' } })
                    c.Set(q[0], q[1], C((char)q[2]));
                return;
            }
            c.Set(x, y, C('K')); c.Set(x + 1, y, C('k')); c.Set(x + 2, y, C('K'));
            c.Set(x, y + 1, C('k')); c.Set(x + 1, y + 1, C('k')); c.Set(x + 2, y + 1, C('k'));
            c.Set(x + 1, y - 1, C('e'));
        }

        static void Fx(Px c, Vector3Int f)
        {
            switch (f.z)
            {
                case FxDust: c.Set(f.x, f.y, C('u')); break;
                case FxLeaf: c.Set(f.x, f.y, C('d')); break;
                case FxLeafDark: c.Set(f.x, f.y, C('e')); break;
                case FxCompost: c.Set(f.x, f.y, C('K')); break;
                case FxSpark: c.Set(f.x, f.y, C('f')); break;
                case FxFlower: c.Set(f.x, f.y, C('y')); c.Set(f.x - 1, f.y, C('w')); c.Set(f.x + 1, f.y, C('w')); c.Set(f.x, f.y - 1, C('w')); break;
                case FxImpact: c.Set(f.x, f.y, C('w')); break;
                case FxCrack: c.Set(f.x, f.y, C('U')); break;
                case FxGlow: c.Set(f.x, f.y, C('l')); c.Set(f.x, f.y - 1, C('d')); break;
            }
        }

        // ---------------------------------------------------------------- animations

        static int[] L(params int[] v) { return v; }

        // A slow, patient breath: the sprout sways, the whole top sinks a pixel, a blink, the shoulder leaf stirs.
        public static List<Pose> Idle()
        {
            return new List<Pose>
            {
                new Pose(),
                new Pose { sproutDx = -1 },
                new Pose { dy = 1, sproutDx = -1 },
                new Pose { dy = 1, leafDy = 1 },
                new Pose { eyes = "blink" },
                new Pose { leafDy = -1 },
            };
        }

        // Four root legs in two pairs (outer left with inner right, then the others), the top bobbing. Contact frames 0
        // and 3 (the mod's Code/03_Assets.cs plays its one footstep on frame 2).
        public static List<Pose> Move()
        {
            return new List<Pose>
            {
                new Pose { dy = 1 },
                new Pose { lift = L(1, 0, 1, 0), leafDy = -1 },
                new Pose { dy = -1, lift = L(2, 0, 2, 0), sproutDx = -1, leafDy = -1 },
                new Pose { dy = 1 },
                new Pose { lift = L(0, 1, 0, 1), leafDy = 1 },
                new Pose { dy = -1, lift = L(0, 2, 0, 2), sproutDx = -1 },
            };
        }

        // The mouth laser. The shot leaves as the animation starts ("Shoot" starts it), so the first frame fires: the hole
        // flares white-green with a ring of sparks and the head bobs up with the recoil; then the glow fades out of the
        // hole and the sprout settles.
        public static List<Pose> Attack()
        {
            var fire = new Pose { dy = -1, mouth = 3, sproutDy = -1, leafDy = -1 };
            foreach (var q in new[] { L(12, 19, FxSpark), L(19, 19, FxSpark), L(13, 17, FxSpark), L(18, 17, FxSpark), L(13, 21, FxSpark), L(18, 21, FxSpark) })
                fire.fx.Add(new Vector3Int(q[0], q[1] - 1, q[2]));
            var glow = new Pose { mouth = 2, sproutDx = -1 };
            var dim = new Pose { dy = 1, mouth = 1, sproutDx = -1, leafDy = 1 };
            var rest = new Pose();
            return new List<Pose> { fire, glow, dim, rest };
        }

        // FERTILIZE: Yossele bends, presses a handful of compost into the ground with its right root arm, and a little
        // green burst comes up; a sprout is left behind.
        public static List<Pose> Fertilize()
        {
            var hold = new Pose { compost = 1, pathR = L(20, 25, 21, 24, 22, 23, 23, 22, 24, 22) };
            var bend = new Pose { dy = 1, headDy = 1, compost = 1, pathR = L(20, 25, 21, 26, 22, 27, 23, 27, 24, 28), sproutDx = -1 };
            var press = new Pose { dy = 1, headDy = 1, compost = 2, pathR = L(20, 25, 21, 26, 22, 27, 23, 28, 23, 29), sproutDx = -1, leafDy = 1 };
            var burst = new Pose { compost = 2, pathR = L(20, 25, 21, 26, 22, 27, 23, 27) };
            foreach (var q in new[] { L(25, 27, FxSpark), L(23, 26, FxLeaf), L(27, 26, FxLeaf), L(25, 25, FxLeafDark), L(22, 28, FxSpark), L(28, 28, FxSpark), L(26, 24, FxLeaf), L(24, 24, FxSpark), L(21, 25, FxLeafDark), L(29, 25, FxLeafDark), L(25, 22, FxSpark), L(23, 29, FxLeaf), L(27, 29, FxLeaf) })
                burst.fx.Add(new Vector3Int(q[0], q[1], q[2]));
            var sprout = new Pose { compost = 2, leafDy = -1 };
            foreach (var q in new[] { L(25, 29, FxGlow), L(24, 28, FxLeaf), L(26, 28, FxLeafDark), L(23, 23, FxLeaf), L(28, 24, FxSpark), L(27, 21, FxLeafDark) })
                sprout.fx.Add(new Vector3Int(q[0], q[1], q[2]));
            var rest = new Pose { compost = 2 };
            foreach (var q in new[] { L(25, 29, FxGlow), L(24, 28, FxLeaf), L(26, 28, FxLeafDark), L(29, 20, FxLeaf) })
                rest.fx.Add(new Vector3Int(q[0], q[1], q[2]));
            return new List<Pose> { hold, bend, press, burst, sprout, rest };
        }

        // Loading screen: falling, root arms flung up, leaves streaming past upward.
        public static List<Px> Fall()
        {
            var frames = new List<Px>();
            for (int f = 0; f < 12; f++)
            {
                bool hi = f % 4 < 2;
                var p = new Pose { dy = -3, air = true, armL = hi ? ArmHigh : ArmUp, armR = hi ? ArmUp : ArmHigh, sproutDy = f % 2 == 0 ? -1 : 0, sproutDx = f % 2 == 0 ? 0 : -1, leafDy = -1, lift = L(0, f % 2, (f + 1) % 2, 0) };
                p.fx.Add(new Vector3Int(4 + (f * 5) % 6, 31 - (f * 3) % 30, FxLeaf));
                p.fx.Add(new Vector3Int(26 - (f * 3) % 5, 31 - ((f * 3 + 15) % 30), FxLeafDark));
                p.fx.Add(new Vector3Int(28, 31 - ((f * 3 + 7) % 30), FxLeaf));
                frames.Add(Frame(0, p));
            }
            return frames;
        }

        // Landing: drops in with its eyes dark, lands heavily in dust with its legs splayed, sits still - then the eyes
        // glow, dim first, and the sprout perks up. The patient golem wakes.
        public static List<Px> Land()
        {
            var poses = new List<Pose>
            {
                new Pose { dy = -14, air = true, armL = ArmUp, armR = ArmUp, eyes = "off", sproutDy = -1 },
                new Pose { dy = -6, air = true, armL = ArmUp, armR = ArmUp, eyes = "off", sproutDy = -1 },
                Dust(new Pose { dy = 2, eyes = "off", legDx = L(-1, 0, 0, 1), sproutDy = 1, leafDy = 1 }, 0),
                Dust(new Pose { dy = 1, eyes = "off", legDx = L(-1, 0, 0, 1), sproutDy = 1 }, 1),
                Dust(new Pose { eyes = "off" }, 2),
                new Pose { eyes = "off" },
                new Pose { eyes = "off" },
                new Pose { eyes = "dim" },
                new Pose { eyes = "on" },
                new Pose { sproutDy = -1, leafDy = -1 },
                new Pose { sproutDx = -1 },
                new Pose(),
            };
            var frames = new List<Px>();
            foreach (var p in poses) frames.Add(Frame(0, p));
            return frames;
        }

        static Pose Dust(Pose p, int t)
        {
            int[,] bits = { { 6, 31 }, { 25, 31 }, { 5, 30 }, { 26, 30 }, { 7, 29 }, { 24, 29 } };
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

        // The reshade at boat size (10 x 13): sprout, round head with the hole and two eyes, collar, four root legs.
        static readonly string[] MiniFront =
        {
            "................",
            "................",
            "................",
            ".........d......",
            "........be......",
            "......aabb......",
            ".....aaaaabb....",
            "....aaa..fab....",
            "....afa..bab....",
            ".....aaaabb.....",
            "...d..ccgg......",
            "...e.aaacab.....",
            "....aaaaaaab....",
            "....a.a..b.b....",
            "...a..a..b..b...",
            "...a..a..b..b...",
        };

        static readonly string[] MiniBack =
        {
            "................",
            "................",
            "................",
            "......d.........",
            "......eb........",
            "......aabb......",
            ".....aaaaabb....",
            "....aaa..aab....",
            "....aaa..bab....",
            ".....aaaabb.....",
            "......gggg..d...",
            ".....aaabab.e...",
            "....aaaaaaab....",
            "....a.a..b.b....",
            "...a..a..b..b...",
            "...a..a..b..b...",
        };

        static Px Mini(bool back, int dy, int sway, bool blink, int liftA, int liftB)
        {
            var src = back ? MiniBack : MiniFront;
            var c = new Px(16, 16);
            // legs (rows 13-15): pair A = outer left + inner right, pair B = the others
            for (int y = 13; y < 16; y++)
                for (int x = 0; x < 16; x++)
                {
                    char k = src[y][x];
                    if (k == '.') continue;
                    int leg = x <= 4 ? 0 : x <= 7 ? 1 : x <= 10 ? 2 : 3;
                    int lift = leg == 0 || leg == 2 ? liftA : liftB;
                    c.Set(x, y - lift, C(k));
                }
            if (dy < 0) for (int x = 0; x < 16; x++) if (src[13][x] != '.') c.Set(x, 12, C(src[13][x]));
            for (int y = 0; y < 13; y++)
                for (int x = 0; x < 16; x++)
                {
                    char k = src[y][x];
                    if (k == '.') continue;
                    if (k == 'f' && blink) k = 'b';
                    int sx = y <= 3 ? sway : 0;
                    c.Set(x + sx, y + dy, C(k));
                }
            return c;
        }

        // 96x128: Idle v0..v3 (rows 0-3) then Move v0..v3 (rows 4-7), 6 frames of 16x16.
        public static Px MiniSheet()
        {
            var rows = new List<List<Px>>();
            int[] idleDy = { 0, 0, 1, 1, 0, 0 };
            int[] sway = { 0, -1, -1, 0, 0, 0 };
            bool[] blink = { false, false, false, false, true, false };
            int[] moveDy = { 1, 0, -1, 1, 0, -1 };
            int[] la = { 0, 1, 1, 0, 0, 0 }, lb = { 0, 0, 0, 0, 1, 1 };
            for (int v = 0; v < 4; v++)
            {
                var r = new List<Px>();
                for (int f = 0; f < 6; f++) { var m = Mini(v >= 2, idleDy[f], sway[f], blink[f], 0, 0); r.Add(v == 1 || v == 3 ? m.FlipX() : m); }
                rows.Add(r);
            }
            for (int v = 0; v < 4; v++)
            {
                var r = new List<Px>();
                for (int f = 0; f < 6; f++) { var m = Mini(v >= 2, moveDy[f], f == 2 || f == 5 ? -1 : 0, false, la[f], lb[f]); r.Add(v == 1 || v == 3 ? m.FlipX() : m); }
                rows.Add(r);
            }
            return Px.Sheet(rows, 16, 16);
        }

        // ---------------------------------------------------------------- UI art

        public static Px Icon() { return Frame(0, new Pose()); }

        // Journal tab icon, the game's Vesselicons style: a white 1 px line drawing with an open interior. The splash's shapes
        // at a quarter size: the outline of the whole figure and of the hole, and the two eyes as dots.
        public static Px TabIcon()
        {
            var c = new Px(32, 32);
            Color32 w = new Color32(255, 255, 255, 255);
            var parts = SplashParts();
            var hole = SplashHole();
            Func<int, int, bool> In = (x, y) =>
            {
                if (x < 0 || y < 0 || x >= 32 || y >= 32) return false;
                float sx = x * 4f + 2f, sy = y * 4f + 2f;
                if (hole.Inside(sx, sy)) return false;
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
            c.Set(12, 11, w); c.Set(12, 12, w); c.Set(19, 10, w); c.Set(19, 11, w);
            return c;
        }

        // Happy: root arms thrown up, a few leaves and flowers in the air.
        public static Px EndWin()
        {
            var p = new Pose { dy = -1, armL = ArmHigh, armR = ArmHigh, leafDy = -1 };
            foreach (var q in new[] { L(4, 8, FxFlower), L(27, 6, FxFlower), L(3, 15, FxLeaf), L(28, 14, FxLeafDark), L(14, 6, FxLeaf), L(24, 11, FxSpark), L(6, 20, FxSpark) })
                p.fx.Add(new Vector3Int(q[0], q[1], q[2]));
            return Frame(0, p);
        }

        // Defeated: slumped, the head sunk into the collar, eyes dark, the sprout drooping; greyed.
        public static Px EndLose()
        {
            var p = new Pose { dy = 2, headDy = 1, eyes = "off", sproutDx = -1, sproutDy = 1, leafDy = 1, legDx = L(-1, 0, 0, 1) };
            var f = Frame(0, p);
            var d = new Px(32, 32);
            for (int i = 0; i < f.C.Length; i++) { var c = f.C[i]; d.C[i] = Col.Mix(c, Col.A(Col.H("8A867E"), c.a), 0.4f); }
            return d;
        }

        // 12x12 ready-up icon: the face (hole, two eyes, sprout, collar) in white corner brackets.
        public static Px ReadyUp()
        {
            var rows = new[]
            {
                "............",
                ".WW.....dWW.",
                ".W.....b..W.",
                "....aabb....",
                "...aaaabab..",
                "...aa..fab..",
                "...af..bab..",
                "....aaaab...",
                "....ccgg....",
                ".W........W.",
                ".WW......WW.",
                "............",
            };
            var pal = new Dictionary<char, Color32>(P) { { 'W', new Color32(255, 255, 255, 255) } };
            return Px.FromRows(rows, pal);
        }

        // ---------------------------------------------------------------- splash (128x128 line art)

        // The shipped splashes are line art: a two-tone frame, then the vessel's shapes filled with a translucent accent,
        // stroked outside in the solid accent and traced 3 px inside with the body's own colours. Accent: leaf green.
        // Shapes follow the reshade at 5x (one shape per body part); the hole is cut out and stroked.
        public static Px Splash()
        {
            const int S = 128;
            Color32 accent = Col.H("72CC35");
            Color32 fill = Col.A(accent, 70);
            var c = new Px(S, S);
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    int d = Mathf.Min(Mathf.Min(x, y), Mathf.Min(S - 1 - x, S - 1 - y));
                    if (d < 2) c.Set(x, y, fill);
                    else if (d < 4) c.Set(x, y, accent);
                }
            var parts = SplashParts();
            foreach (var part in parts) part.Draw(c, accent, fill);
            // the see-through hole: cut out and stroked
            SplashHole().Draw(c, accent, new Color32(0, 0, 0, 0), false);
            // glowing eyes (the near one a little higher, as drawn)
            Color32 eye = C('f');
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float a = (x + 0.5f - 79f) / 3.5f, b = (y + 0.5f - 45f) / 4f, e2 = (x + 0.5f - 50f) / 3.5f, f2 = (y + 0.5f - 50f) / 4f;
                    if (a * a + b * b <= 1f || e2 * e2 + f2 * f2 <= 1f) c.Set(x, y, eye);
                }
            return c;
        }

        // The splash's shapes (128 px space), shared with the journal tab icon.
        static List<SplashPart> SplashParts()
        {
            Color32 clay = C('a'), shade = C('b');
            return new List<SplashPart>
            {
                // four root limbs, the outer two splayed
                SplashPart.Chain(new[] { new Vector2(42, 92), new Vector2(37, 103), new Vector2(33, 116) }, new[] { 6f, 5.5f, 5f }, clay, shade),
                SplashPart.Capsule(new Vector2(55, 94), new Vector2(55, 116), 5.5f, clay, shade),
                SplashPart.Capsule(new Vector2(74, 94), new Vector2(74, 116), 5.5f, clay, shade),
                SplashPart.Chain(new[] { new Vector2(87, 92), new Vector2(92, 103), new Vector2(96, 116) }, new[] { 6f, 5.5f, 5f }, clay, shade),
                // body, then the collar's V over it
                SplashPart.Ellipse(new Vector2(64, 84), 31, 14, 0f, clay, shade),
                SplashPart.Poly(new[] { new Vector2(44, 66), new Vector2(86, 66), new Vector2(66, 95) }, C('c'), C('g')),
                // head
                SplashPart.Ellipse(new Vector2(64, 50), 27, 22, 0f, clay, shade),
                // stalk and the two sprout leaves
                SplashPart.Chain(new[] { new Vector2(66, 31), new Vector2(69, 25), new Vector2(73, 20) }, new[] { 4f, 3.5f, 3f }, shade, shade),
                SplashPart.Ellipse(new Vector2(76, 14), 7.5f, 4.5f, -70f, C('d'), C('e')),
                SplashPart.Ellipse(new Vector2(86, 20), 9, 4.5f, -15f, C('d'), C('e')),
                // shoulder leaf
                SplashPart.Ellipse(new Vector2(30, 72), 9, 4.5f, -35f, C('d'), C('e')),
            };
        }

        static SplashPart SplashHole() { return SplashPart.Ellipse(new Vector2(64, 57), 9, 7.5f, 0f, C('a'), C('b')); }

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

            // A tapering tube through several points, as one shape so it is outlined once.
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

            // A convex polygon (the collar's V).
            public static SplashPart Poly(Vector2[] pts, Color32 line, Color32 shade)
            {
                float cx = 0f;
                foreach (var q in pts) cx += q.x;
                cx /= pts.Length;
                return new SplashPart
                {
                    Inside = (x, y) =>
                    {
                        int sign = 0;
                        for (int i = 0; i < pts.Length; i++)
                        {
                            Vector2 a = pts[i], b = pts[(i + 1) % pts.Length];
                            float cr = (b.x - a.x) * (y - a.y) - (b.y - a.y) * (x - a.x);
                            int s = cr > 0 ? 1 : cr < 0 ? -1 : 0;
                            if (s == 0) continue;
                            if (sign == 0) sign = s; else if (s != sign) return false;
                        }
                        return true;
                    },
                    Line = line, Shade = shade, ShadeFromX = cx + 6f
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
