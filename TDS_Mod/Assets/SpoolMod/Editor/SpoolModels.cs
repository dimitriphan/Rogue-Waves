// Spool asset generator: every tower model - the Yarn Stash, the Repurposed Cookie Tin, the Pin Cushion and the Loom,
// each upgrade's look (named <Tower>_<Key><Tier>, Code/02_Data.cs Upg.Model), the Cookie Tin's pin and the Dart's
// needle. TEMPORARY MODELS: the scope gives the towers' look only as "stitches, pins and tins" (docs/scope/08), and
// docs/STYLE.md puts the vessel's motif on the top or front face. Built sleek and low-poly like Sno Sno's and Buster's
// towers - bevelled solids, turned posts - never voxels. Upgrades keep the base silhouette and add one clear feature.
// Units: 1 = one tile; y measured up from the ground; the front faces -z.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpoolBuild
{
    public class ModelDef
    {
        public string Name;
        public string Group;     // Stash, Tin, Cushion, Loom, Extras, Projectiles
        public bool Projectile;
        public Func<Lp> Build;
    }

    public static class SpoolModels
    {
        static Vector3 V(float x, float y, float z) { return new Vector3(x, y, z); }

        public static List<ModelDef> All()
        {
            var l = new List<ModelDef>();
            void Add(string group, string name, Func<Lp> f, bool proj = false) { l.Add(new ModelDef { Group = group, Name = name, Build = f, Projectile = proj }); }

            const string S = "Tower_Spoolstash", T = "Tower_Spooltin", C = "Tower_Spoolcushion", L = "Tower_Spoolloom";
            Add("Stash", S, Stash);
            Add("Tin", T, () => Tin(""));
            foreach (var k in new[] { "Recycle2", "Woolen2", "Discounted3", "Design3", "Surprise4" }) { string key = k; Add("Tin", T + "_" + key, () => Tin(key)); }
            Add("Cushion", C, () => Cushion(""));
            foreach (var k in new[] { "Sharper2", "Unravel2", "Barrage3", "Stuffing3", "Overstuffed4", "Burst4" }) { string key = k; Add("Cushion", C + "_" + key, () => Cushion(key)); }
            Add("Loom", L, () => Loom(""));
            foreach (var k in new[] { "Direct2", "Seam2" }) { string key = k; Add("Loom", L + "_" + key, () => Loom(key)); }
            Add("Extras", "Spool_Pin", Pin);
            Add("Projectiles", "Spool_Needle", Needle, true);
            return l;
        }

        // ---------------------------------------------------------------- shared pieces

        static void Plinth(Lp m, float r = 0.37f, int side = Pal.Wood3, int top = Pal.Wood1)
        {
            m.Prism(V(0, 0, 0), r, r * 0.95f, 0.055f, 8, side, top, -1, 22.5f);
        }

        static void Thread(Lp m, Vector3 a, Vector3 b, int col, float r = 0.006f) { m.Tube(a, b, r, r, 4, col); }

        // A thin ring (low-poly torus) lying flat at height y.
        static void Ring(Lp m, Vector3 c, float r, float tube, int col, int segs = 12)
        {
            for (int i = 0; i < segs; i++)
            {
                float a0 = i / (float)segs * Mathf.PI * 2f, a1 = (i + 1) / (float)segs * Mathf.PI * 2f;
                m.Tube(c + V(Mathf.Cos(a0) * r, 0, Mathf.Sin(a0) * r), c + V(Mathf.Cos(a1) * r, 0, Mathf.Sin(a1) * r), tube, tube, 4, col);
            }
        }

        // A ball of yarn: the ball, then a few wraps around it (thin rings tilted different ways).
        static void Yarn(Lp m, Vector3 c, float r, int col, int top, int wrap)
        {
            m.Ball(c, Vector3.one * r, 8, 6, col, top);
            for (int i = 0; i < 3; i++)
            {
                m.Push(c, Quaternion.Euler(30f + i * 50f, i * 60f, 15f * i), Vector3.one);
                Ring(m, Vector3.zero, r * 1.01f, r * 0.08f, wrap, 10);
                m.Pop();
            }
        }

        // A spool of thread standing up: wooden flanges, the thread between.
        static void Bobbin(Lp m, Vector3 at, float r, float h, int thread, int wood = Pal.Wood1)
        {
            m.Prism(at, r * 1.35f, r * 1.35f, h * 0.12f, 8, wood, wood);
            m.Prism(at + Vector3.up * h * 0.12f, r, r, h * 0.76f, 8, thread, thread);
            m.Prism(at + Vector3.up * h * 0.88f, r * 1.35f, r * 1.35f, h * 0.12f, 8, wood, Pal.Wood0);
        }

        // A dressmaker's pin: steel shaft from 'at' along 'dir', a round coloured head.
        static void PinStick(Lp m, Vector3 at, Vector3 dir, float len, int head, float headR = 0.03f)
        {
            Vector3 tip = at + dir.normalized * len;
            m.Tube(at, tip, 0.006f, 0.006f, 4, Pal.Steel);
            m.Ball(tip, Vector3.one * headR, 6, 4, head, Pal.White, 0.8f);
        }

        // A puff of stuffing: a few overlapping cream balls.
        static void Puff(Lp m, Vector3 c, float r)
        {
            m.Ball(c, Vector3.one * r, 7, 5, Pal.Cream0, Pal.White);
            m.Ball(c + V(r * 0.7f, -r * 0.2f, -r * 0.3f), Vector3.one * r * 0.7f, 6, 4, Pal.Cream1, Pal.Cream0);
            m.Ball(c + V(-r * 0.6f, r * 0.2f, -r * 0.4f), Vector3.one * r * 0.6f, 6, 4, Pal.Cream0, Pal.White);
        }

        // A felt scrap lying flat: a little two-sided triangle.
        static void Scrap(Lp m, Vector3 c, float size, float yaw, int col)
        {
            Vector3 a = Quaternion.Euler(0, yaw, 0) * V(size, 0, 0), b = Quaternion.Euler(0, yaw + 130f, 0) * V(size * 0.8f, 0, 0), d = Quaternion.Euler(0, yaw + 240f, 0) * V(size * 0.9f, 0, 0);
            m.Tri2(c + a, c + b + Vector3.up * 0.01f, c + d, col, col);
        }

        // ---------------------------------------------------------------- Yarn Stash

        // A wicker basket heaped with balls of yarn, two knitting needles stuck in, a loose end spilling over the rim.
        static Lp Stash()
        {
            var m = new Lp();
            Plinth(m, 0.38f);
            // the basket: woven sides (alternating bands), a thick rim, the dark inside
            m.Prism(V(0, 0.055f, 0), 0.28f, 0.33f, 0.25f, 10, Pal.Wood2, -1, Pal.Wood3);
            for (int i = 0; i < 3; i++)
            {
                float y = 0.085f + i * 0.07f, r = 0.285f + (y - 0.055f) * 0.2f;
                m.Prism(V(0, y, 0), r + 0.008f, r + 0.012f, 0.025f, 10, i % 2 == 0 ? Pal.Wood1 : Pal.Wood3, -1);
            }
            Ring(m, V(0, 0.305f, 0), 0.33f, 0.026f, Pal.WoodPale, 14);
            m.Disc(V(0, 0.27f, 0), 0.31f, 10, Pal.Wood4);
            // two handles, one each side
            foreach (float sx in new[] { -1f, 1f })
                m.Stem(new[] { V(sx * 0.3f, 0.3f, -0.08f), V(sx * 0.38f, 0.38f, -0.05f), V(sx * 0.4f, 0.4f, 0.04f), V(sx * 0.31f, 0.3f, 0.08f) }, 0.016f, 0.016f, 5, Pal.WoodPale);
            // the yarn, heaped
            Yarn(m, V(-0.1f, 0.33f, 0.04f), 0.13f, Pal.Felt3, Pal.Felt2, Pal.Felt5);
            Yarn(m, V(0.11f, 0.32f, -0.05f), 0.12f, Pal.ThreadPink, Pal.White, Pal.Tomato0);
            Yarn(m, V(0.05f, 0.4f, 0.13f), 0.11f, Pal.ThreadBlue, Pal.Silk, Pal.Tin1);
            Yarn(m, V(-0.08f, 0.44f, -0.1f), 0.09f, Pal.ThreadGold, Pal.Gold0, Pal.Gold2);
            // knitting needles crossed in the heap, brass knobs on top
            m.Tube(V(0.0f, 0.3f, 0.05f), V(-0.07f, 0.68f, 0.12f), 0.011f, 0.011f, 5, Pal.Steel);
            m.Tube(V(0.02f, 0.3f, 0.02f), V(0.14f, 0.66f, 0.1f), 0.011f, 0.011f, 5, Pal.Steel);
            m.Ball(V(-0.07f, 0.68f, 0.12f), Vector3.one * 0.022f, 6, 4, Pal.Brass, Pal.Gold0);
            m.Ball(V(0.14f, 0.66f, 0.1f), Vector3.one * 0.022f, 6, 4, Pal.Brass, Pal.Gold0);
            // the loose end, over the front rim and onto the ground
            m.Stem(new[] { V(0.13f, 0.27f, -0.15f), V(0.16f, 0.31f, -0.3f), V(0.2f, 0.15f, -0.36f), V(0.26f, 0.06f, -0.4f), V(0.33f, 0.06f, -0.38f) }, 0.009f, 0.009f, 4, Pal.ThreadPink);
            return m;
        }

        // ---------------------------------------------------------------- Repurposed Cookie Tin

        // A round blue cookie tin, gold bands and white cookie dots on its side, the lid propped up behind it, full of
        // sewing things: three spools of thread, a little pincushion, buttons.
        static Lp Tin(string up)
        {
            var m = new Lp();
            Plinth(m, 0.38f, Pal.Tin3, Pal.Tin2);
            float h = 0.22f, r = 0.3f;
            m.Prism(V(0, 0.055f, 0), r, r, h, 16, Pal.Tin1, -1, Pal.Tin2);
            m.Prism(V(0, 0.06f, 0), r + 0.006f, r + 0.006f, 0.025f, 16, Pal.Gold1, -1);
            m.Prism(V(0, 0.055f + h - 0.03f, 0), r + 0.006f, r + 0.006f, 0.028f, 16, Pal.Gold1, Pal.Gold0);
            for (int i = 0; i < 10; i++)
            {
                float a = i / 10f * Mathf.PI * 2f;
                m.Ball(V(Mathf.Cos(a) * (r + 0.004f), 0.165f, Mathf.Sin(a) * (r + 0.004f)), V(0.03f, 0.03f, 0.03f), 6, 3, i % 2 == 0 ? Pal.Cream0 : Pal.Tin0);
            }
            // the frilled paper liner and what is inside
            m.Disc(V(0, 0.24f, 0), r - 0.01f, 16, Pal.Cream2);
            for (int i = 0; i < 16; i++)
            {
                float a0 = i / 16f * Mathf.PI * 2f, a1 = (i + 1) / 16f * Mathf.PI * 2f;
                m.Tri2(V(Mathf.Cos(a0) * (r - 0.01f), 0.275f, Mathf.Sin(a0) * (r - 0.01f)), V(Mathf.Cos(a1) * (r - 0.01f), 0.275f, Mathf.Sin(a1) * (r - 0.01f)),
                    V(Mathf.Cos((a0 + a1) * 0.5f) * (r - 0.03f), 0.31f, Mathf.Sin((a0 + a1) * 0.5f) * (r - 0.03f)), Pal.Cream0, Pal.Cream2);
            }
            Bobbin(m, V(-0.13f, 0.25f, 0.06f), 0.05f, 0.16f, Pal.ThreadPink);
            Bobbin(m, V(-0.02f, 0.25f, 0.15f), 0.045f, 0.14f, Pal.ThreadBlue);
            Bobbin(m, V(0.12f, 0.25f, 0.1f), 0.05f, 0.15f, Pal.Felt3);
            // the little tomato pincushion at the front
            m.Ball(V(0.06f, 0.29f, -0.11f), V(0.085f, 0.06f, 0.085f), 8, 5, Pal.Tomato1, Pal.Tomato0);
            m.Leaf(V(0.06f, 0.35f, -0.11f), V(1, 0.3f, 0), 0.05f, 0.03f, Pal.Leaf, Pal.LeafDark);
            m.Leaf(V(0.06f, 0.35f, -0.11f), V(-0.6f, 0.3f, 0.6f), 0.05f, 0.03f, Pal.Leaf, Pal.LeafDark);
            PinStick(m, V(0.04f, 0.33f, -0.12f), V(-0.4f, 1, -0.3f), 0.09f, Pal.Gold1, 0.018f);
            PinStick(m, V(0.09f, 0.33f, -0.1f), V(0.5f, 1, 0.1f), 0.08f, Pal.ThreadBlue, 0.018f);
            // buttons on the liner
            m.Disc(V(-0.14f, 0.255f, -0.12f), 0.035f, 8, Pal.Tomato2, 0.012f);
            m.Disc(V(-0.06f, 0.255f, -0.19f), 0.03f, 8, Pal.Gold1, 0.012f);
            // the lid, standing behind the tin and leaning back a little, a cookie picture on its face
            m.Push(V(0, 0.33f, 0.36f), Quaternion.Euler(-72f, 0f, 0f), Vector3.one);
            m.Prism(V(0, 0, 0), r + 0.01f, r + 0.01f, 0.045f, 16, Pal.Tin2, Pal.Tin1, Pal.Tin2);
            m.Prism(V(0, 0.045f, 0), r - 0.02f, r - 0.02f, 0.006f, 16, Pal.Gold1, Pal.Gold1);
            m.Prism(V(0, 0.051f, 0), r - 0.05f, r - 0.05f, 0.004f, 16, Pal.Tin0, Pal.Tin0);
            m.Prism(V(0, 0.055f, 0), 0.11f, 0.11f, 0.008f, 10, Pal.Gold0, Pal.Cream1);
            for (int i = 0; i < 6; i++)
            {
                float a = i / 6f * Mathf.PI * 2f;
                m.Prism(V(Mathf.Cos(a) * 0.18f, 0.055f, Mathf.Sin(a) * 0.18f), 0.035f, 0.035f, 0.008f, 8, Pal.Cream0, Pal.Cream0);
            }
            m.Pop();
            switch (up)
            {
                case "Recycle2":
                    // a heap of saved scraps and a loose ball beside the tin
                    for (int i = 0; i < 7; i++) Scrap(m, V(-0.3f + (i % 3) * 0.05f, 0.06f + i * 0.012f, -0.28f + (i / 3) * 0.04f), 0.06f, i * 47f, i % 3 == 0 ? Pal.Felt2 : i % 3 == 1 ? Pal.ThreadPink : Pal.ThreadBlue);
                    Yarn(m, V(-0.32f, 0.11f, -0.18f), 0.06f, Pal.Felt4, Pal.Felt3, Pal.Felt6);
                    break;
                case "Woolen2":
                    // knitted wool lining spilling over the rim
                    Ring(m, V(0, 0.29f, 0), r - 0.005f, 0.035f, Pal.Cream1, 16);
                    Ring(m, V(0, 0.27f, 0), r + 0.02f, 0.025f, Pal.Felt2, 16);
                    break;
                case "Discounted3":
                    // a price tag on a string, hanging from the front of the rim
                    Thread(m, V(0.16f, 0.29f, -0.25f), V(0.2f, 0.2f, -0.33f), Pal.ThreadRed, 0.005f);
                    m.Push(V(0.22f, 0.12f, -0.34f), Quaternion.Euler(0f, 20f, 12f), Vector3.one);
                    m.Block(V(0, 0, 0), V(0.1f, 0.08f, 0.012f), 0.01f, Pal.White, Pal.White);
                    m.Block(V(0, 0.05f, -0.004f), V(0.07f, 0.014f, 0.012f), 0.003f, Pal.Tomato1, Pal.Tomato1);
                    m.Pop();
                    break;
                case "Design3":
                    // a sewing pattern leaning on the side, and darts: needles stood up in the pincushion
                    m.Tri2(V(-0.32f, 0.06f, -0.2f), V(-0.42f, 0.06f, 0.15f), V(-0.36f, 0.42f, -0.02f), Pal.Cream1, Pal.Cream3);
                    Thread(m, V(-0.335f, 0.12f, -0.15f), V(-0.37f, 0.33f, -0.04f), Pal.ThreadBlue, 0.004f);
                    for (int i = 0; i < 4; i++)
                        m.Tube(V(0.03f + i * 0.02f, 0.32f, -0.12f + (i % 2) * 0.02f), V(0.0f + i * 0.04f, 0.47f, -0.15f + (i % 2) * 0.03f), 0.006f, 0.002f, 4, Pal.Steel);
                    break;
                case "Surprise4":
                    // a jack-in-the-box: a spring pops out of the middle with a little plush head on it
                    for (int i = 0; i < 6; i++) Ring(m, V(-0.02f, 0.3f + i * 0.045f, 0.02f), 0.05f, 0.008f, Pal.Iron1, 8);
                    m.Ball(V(-0.02f, 0.62f, 0.02f), V(0.08f, 0.075f, 0.08f), 8, 5, Pal.Purple, Pal.Felt3);
                    m.Ball(V(-0.05f, 0.63f, -0.055f), Vector3.one * 0.014f, 5, 3, Pal.ThreadGold);
                    m.Ball(V(0.015f, 0.63f, -0.055f), Vector3.one * 0.014f, 5, 3, Pal.ThreadGold);
                    // and a red bow on the lid
                    m.Ball(V(0, 0.6f, 0.36f), Vector3.one * 0.03f, 6, 4, Pal.Tomato1);
                    m.Tri2(V(0, 0.6f, 0.35f), V(-0.1f, 0.66f, 0.36f), V(-0.1f, 0.54f, 0.36f), Pal.Tomato1, Pal.Tomato2);
                    m.Tri2(V(0, 0.6f, 0.35f), V(0.1f, 0.66f, 0.36f), V(0.1f, 0.54f, 0.36f), Pal.Tomato1, Pal.Tomato2);
                    break;
            }
            return m;
        }

        // ---------------------------------------------------------------- Pin Cushion

        // A big tomato pincushion: a squashed red ball in segments, a leaf cap and stem, coloured pins stuck in all over.
        static Lp Cushion(string up)
        {
            var m = new Lp();
            bool big = up == "Overstuffed4";
            float s = big ? 1.12f : 1f;
            Plinth(m, 0.4f, Pal.Wood3, Pal.Wood1);
            Vector3 c = V(0, 0.055f + 0.21f * s, 0);
            Vector3 rad = V(0.36f, 0.22f, 0.36f) * s;
            m.Ball(c, rad, 12, 7, Pal.Tomato1, Pal.Tomato0, 0.7f, Pal.Tomato2, -0.5f);
            // the seams between the segments
            for (int i = 0; i < 6; i++)
            {
                float a = i / 6f * Mathf.PI * 2f + 0.26f;
                var pts = new List<Vector3>();
                for (int j = 1; j < 6; j++)
                {
                    float th = Mathf.PI * j / 6f;
                    pts.Add(c + V(Mathf.Sin(th) * Mathf.Cos(a) * rad.x * 1.01f, Mathf.Cos(th) * rad.y * 1.01f, Mathf.Sin(th) * Mathf.Sin(a) * rad.z * 1.01f));
                }
                m.Stem(pts, 0.008f, 0.008f, 4, Pal.Tomato3);
            }
            // leaf cap and stem
            Vector3 top = c + Vector3.up * rad.y * 0.97f;
            for (int i = 0; i < 5; i++)
            {
                float a = i / 5f * 360f + 10f;
                m.Leaf(top, Quaternion.Euler(0, a, 0) * V(1, -0.1f, 0), 0.19f * s, 0.11f * s, Pal.Leaf, Pal.LeafDark, 0.25f, 0.1f);
            }
            m.Tube(top, top + V(0.01f, 0.07f, 0), 0.016f, 0.012f, 5, Pal.LeafDark, Pal.Leaf);
            // pins on the upper half
            int[] heads = { Pal.Gold1, Pal.ThreadBlue, Pal.White, Pal.Mint, Pal.ThreadGold, Pal.Tin1, Pal.Cream0 };
            int pins = up == "Sharper2" ? 11 : 7;
            for (int i = 0; i < pins; i++)
            {
                float a = i / (float)pins * Mathf.PI * 2f + 0.5f, th = 0.5f + (i % 3) * 0.22f;
                Vector3 n = V(Mathf.Sin(th) * Mathf.Cos(a), Mathf.Cos(th), Mathf.Sin(th) * Mathf.Sin(a));
                Vector3 at = c + Vector3.Scale(n, rad) * 0.95f;
                PinStick(m, at, n + V(0, 0.4f, 0), up == "Sharper2" ? 0.16f : 0.11f, heads[i % heads.Length]);
            }
            switch (up)
            {
                case "Sharper2":
                    // long needles straight out at the front, ready for a melee foe
                    for (int i = 0; i < 5; i++)
                    {
                        float a = Mathf.PI * 1.5f + (i - 2) * 0.35f;
                        Vector3 n = V(Mathf.Cos(a), 0.05f, Mathf.Sin(a));
                        Vector3 at = c + Vector3.Scale(n, rad) * 0.9f;
                        m.Tube(at, at + n * 0.2f, 0.008f, 0.001f, 4, Pal.Steel);
                    }
                    break;
                case "Unravel2":
                    // a loose thread unwinding off the side and trailing on the ground
                    m.Stem(new[] { c + V(0.33f, 0.04f, -0.08f), c + V(0.4f, -0.02f, -0.15f), V(0.42f, 0.07f, -0.25f), V(0.35f, 0.06f, -0.38f), V(0.2f, 0.06f, -0.42f), V(0.08f, 0.06f, -0.38f) }, 0.01f, 0.01f, 4, Pal.Tomato3);
                    m.Ball(c + V(0.25f, -0.03f, -0.24f), V(0.06f, 0.05f, 0.03f), 6, 4, Pal.Cream1, Pal.Cream0);
                    break;
                case "Barrage3":
                    // pins all round the middle, pointing out
                    for (int i = 0; i < 14; i++)
                    {
                        float a = i / 14f * Mathf.PI * 2f;
                        Vector3 n = V(Mathf.Cos(a), 0.1f, Mathf.Sin(a));
                        PinStick(m, c + Vector3.Scale(n, rad) * 0.95f, n, 0.07f, heads[(i + 2) % heads.Length], 0.024f);
                    }
                    break;
                case "Stuffing3":
                case "Overstuffed4":
                case "Burst4":
                    // stuffing poking out of split seams
                    Puff(m, c + V(-0.28f, 0.02f, -0.2f) * s, 0.07f);
                    if (up != "Stuffing3") Puff(m, c + V(0.3f, -0.03f, -0.14f) * s, 0.07f);
                    if (up == "Overstuffed4")
                    {
                        // felt patches stitched over the bulges
                        m.Push(c + V(0.05f, 0.04f, -0.36f), Quaternion.Euler(-10f, 0f, 0f), Vector3.one);
                        m.Block(V(0, 0, 0), V(0.14f, 0.12f, 0.02f), 0.01f, Pal.Felt3, Pal.Felt2);
                        for (int i = 0; i < 3; i++) m.Block(V(-0.05f + i * 0.05f, 0.105f, -0.012f), V(0.012f, 0.03f, 0.01f), 0.002f, Pal.White, Pal.White);
                        m.Pop();
                        m.Push(c + V(-0.25f, 0.12f, 0.2f), Quaternion.Euler(10f, 50f, 20f), Vector3.one);
                        m.Block(V(0, 0, 0), V(0.12f, 0.1f, 0.02f), 0.01f, Pal.Tin1, Pal.Tin0);
                        m.Pop();
                    }
                    if (up == "Burst4")
                    {
                        Puff(m, c + V(0.05f, 0.16f, -0.25f), 0.08f);
                        // pins knocked out, lying on the ground
                        m.Tube(V(-0.32f, 0.065f, -0.34f), V(-0.2f, 0.065f, -0.4f), 0.006f, 0.006f, 4, Pal.Steel);
                        m.Ball(V(-0.2f, 0.07f, -0.4f), Vector3.one * 0.025f, 6, 4, Pal.ThreadGold);
                        m.Tube(V(0.25f, 0.065f, -0.36f), V(0.36f, 0.065f, -0.3f), 0.006f, 0.006f, 4, Pal.Steel);
                        m.Ball(V(0.36f, 0.07f, -0.3f), Vector3.one * 0.025f, 6, 4, Pal.Mint);
                    }
                    break;
            }
            return m;
        }

        // ---------------------------------------------------------------- Loom

        // A frame loom: two posts on feet, the warp threads strung top to bottom, a band half woven, the shuttle resting
        // on it; a drop spindle hangs on one side with the Thread it spins.
        static Lp Loom(string up)
        {
            var m = new Lp();
            Plinth(m, 0.38f);
            float wide = 0.24f, top = 0.62f;
            foreach (float sx in new[] { -wide, wide })
            {
                m.Block(V(sx, 0.055f, 0), V(0.08f, 0.04f, 0.3f), 0.012f, Pal.Wood3, Pal.Wood2);
                m.Prism(V(sx, 0.09f, 0), 0.03f, 0.026f, top - 0.04f, 6, Pal.Wood2, Pal.Wood1);
                m.Ball(V(sx, top + 0.06f, 0), Vector3.one * 0.035f, 6, 4, Pal.Wood1, Pal.Wood0);
            }
            // top and bottom beams
            m.Tube(V(-wide - 0.03f, top, 0), V(wide + 0.03f, top, 0), 0.026f, 0.026f, 6, Pal.Wood1, Pal.Wood0, Pal.Wood0);
            m.Tube(V(-wide - 0.03f, 0.18f, 0), V(wide + 0.03f, 0.18f, 0), 0.026f, 0.026f, 6, Pal.Wood1, Pal.Wood0, Pal.Wood0);
            // warp
            for (int i = 0; i < 9; i++)
            {
                float x = -wide + 0.04f + i * (wide * 2f - 0.08f) / 8f;
                Thread(m, V(x, top - 0.02f, -0.012f), V(x, 0.2f, -0.012f), i % 2 == 0 ? Pal.Felt1 : Pal.Lilac1, 0.005f);
            }
            // the woven band, in Spool's purple with a pink stripe
            m.Block(V(0, 0.2f, -0.02f), V(wide * 2f - 0.06f, 0.16f, 0.02f), 0.006f, Pal.Purple, Pal.Felt4);
            m.Block(V(0, 0.29f, -0.032f), V(wide * 2f - 0.06f, 0.025f, 0.012f), 0.003f, Pal.ThreadPink, Pal.ThreadPink);
            // the shuttle resting on the band
            m.Ball(V(0.05f, 0.37f, -0.045f), V(0.09f, 0.022f, 0.022f), 6, 3, Pal.Wood0, Pal.WoodPale);
            Thread(m, V(0.05f, 0.37f, -0.06f), V(-0.12f, 0.36f, -0.04f), Pal.ThreadPink, 0.004f);
            // the drop spindle on a peg at the right post: a whorl and a cone of spun Thread
            m.Tube(V(wide + 0.03f, 0.5f, 0), V(wide + 0.1f, 0.5f, 0), 0.01f, 0.01f, 4, Pal.Wood3);
            Thread(m, V(wide + 0.1f, 0.5f, 0), V(wide + 0.1f, 0.38f, 0), Pal.Felt2, 0.005f);
            m.Tube(V(wide + 0.1f, 0.38f, 0), V(wide + 0.1f, 0.12f, 0), 0.008f, 0.006f, 5, Pal.Wood1);
            m.Prism(V(wide + 0.1f, 0.15f, 0), 0.05f, 0.05f, 0.015f, 8, Pal.Wood2, Pal.Wood1, Pal.Wood3);
            m.Tube(V(wide + 0.1f, 0.17f, 0), V(wide + 0.1f, 0.3f, 0), 0.04f, 0.015f, 7, Pal.Felt2);
            // a pick at its foot (the Loom picks at ore)
            m.Tube(V(-wide - 0.05f, 0.06f, -0.2f), V(-wide - 0.16f, 0.06f, 0.05f), 0.012f, 0.012f, 5, Pal.Wood3);
            m.Tube(V(-wide - 0.1f, 0.06f, -0.17f), V(-wide - 0.02f, 0.1f, -0.24f), 0.012f, 0.004f, 4, Pal.Iron2);
            switch (up)
            {
                case "Direct2":
                    // a tall post with a reel on top; the Thread runs up to it and off toward home
                    m.Prism(V(-wide - 0.1f, 0.055f, 0.2f), 0.022f, 0.018f, 0.7f, 6, Pal.Wood2, Pal.Wood1);
                    m.Push(V(-wide - 0.1f, 0.78f, 0.2f), Quaternion.Euler(0f, 0f, 90f), Vector3.one);
                    Bobbin(m, V(0, -0.06f, 0), 0.04f, 0.12f, Pal.Felt2);
                    m.Pop();
                    Thread(m, V(wide + 0.1f, 0.3f, 0), V(-wide - 0.1f, 0.76f, 0.17f), Pal.Felt2, 0.004f);
                    Thread(m, V(-wide - 0.1f, 0.8f, 0.2f), V(-wide + 0.2f, 0.82f, 0.45f), Pal.Felt2, 0.004f);
                    break;
                case "Seam2":
                    // patches sewn onto the band, and a needle left stuck in it
                    m.Block(V(-0.1f, 0.22f, -0.04f), V(0.08f, 0.07f, 0.012f), 0.003f, Pal.Cream0, Pal.Cream0);
                    m.Block(V(0.1f, 0.24f, -0.04f), V(0.07f, 0.06f, 0.012f), 0.003f, Pal.Tin1, Pal.Tin0);
                    for (int i = 0; i < 3; i++)
                        m.Block(V(-0.125f + i * 0.025f, 0.288f, -0.048f), V(0.008f, 0.02f, 0.006f), 0.001f, Pal.Purple, Pal.Purple);
                    m.Tube(V(0.06f, 0.33f, -0.06f), V(0.17f, 0.45f, -0.1f), 0.006f, 0.002f, 4, Pal.Steel);
                    Thread(m, V(0.06f, 0.33f, -0.06f), V(0.0f, 0.27f, -0.05f), Pal.ThreadBlue, 0.004f);
                    break;
            }
            return m;
        }

        // ---------------------------------------------------------------- the pin and the needle

        // Where a Cookie Tin sends its Crafted: a dressmaker's pin stuck in the ground, its red head up, a cross-stitch
        // at its foot.
        static Lp Pin()
        {
            var m = new Lp();
            m.Disc(V(0, 0, 0), 0.12f, 8, Pal.Cream2, 0.008f);
            m.Block(V(0, 0.008f, 0), V(0.2f, 0.006f, 0.03f), 0.002f, Pal.Tomato2, Pal.Tomato2);
            m.Push(V(0, 0.008f, 0), 90f);
            m.Block(V(0, 0, 0), V(0.2f, 0.006f, 0.03f), 0.002f, Pal.Tomato2, Pal.Tomato2);
            m.Pop();
            m.Tube(V(0, 0.0f, 0), V(0.02f, 0.38f, 0.01f), 0.009f, 0.007f, 5, Pal.Steel);
            m.Ball(V(0.02f, 0.42f, 0.01f), Vector3.one * 0.06f, 8, 5, Pal.Tomato1, Pal.Tomato0, 0.4f);
            return m;
        }

        // The Dart's thrown needle (along z, point forward), a short lilac thread trailing from its eye.
        static Lp Needle()
        {
            var m = new Lp();
            m.Tube(V(0, 0.5f, -0.1f), V(0, 0.5f, 0.1f), 0.01f, 0.01f, 5, Pal.Steel);
            m.Tube(V(0, 0.5f, 0.1f), V(0, 0.5f, 0.16f), 0.01f, 0f, 5, Pal.Iron0);
            Thread(m, V(0, 0.5f, -0.1f), V(0.02f, 0.52f, -0.16f), Pal.Felt2, 0.006f);
            Thread(m, V(0.02f, 0.52f, -0.16f), V(-0.01f, 0.5f, -0.21f), Pal.Felt2, 0.006f);
            return m;
        }
    }
}
