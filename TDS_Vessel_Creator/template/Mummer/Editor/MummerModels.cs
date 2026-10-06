// Mummer asset generator: every tower model (buildables, upgrades, costume hangers, the felt bolt) and the meshes of
// the Marked / Held / Tangled debuff effects. Built sleek and low-poly like Sno Sno's and Buster's towers - smooth
// bevelled solids, turned posts, draped cloth - never voxels. Mummer's triangle runs through the kit: A-frame looms,
// pediments, pennants and finials. Upgrades keep the base silhouette and add one clear feature.
// Units: 1 = one tile; y measured up from the ground; the front faces -z.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace MummerBuild
{
    public class ModelDef
    {
        public string Name;
        public string Group;     // Loom, Stage, Cross, Curtain, Extras, Debuff, Projectiles
        public bool Projectile;
        public Func<Lp> Build;
    }

    public static class MummerModels
    {
        static Vector3 V(float x, float y, float z) { return new Vector3(x, y, z); }

        public static List<ModelDef> All()
        {
            var l = new List<ModelDef>();
            void Add(string group, string name, Func<Lp> f, bool proj = false) { l.Add(new ModelDef { Group = group, Name = name, Build = f, Projectile = proj }); }

            const string L = "Tower_Mummerloom", S = "Tower_Mummerstage", X = "Tower_Mummercross", C = "Trap_Mummercurtain";
            Add("Loom", L, () => Loom(""));
            foreach (var k in new[] { "Shuttle2", "Weft2", "Silk3", "Orethread3", "Patchwork4", "Tapestry4", "Golden5" }) { string key = k; Add("Loom", L + "_" + key, () => Loom(key)); }
            Add("Stage", S, () => Stage(""));
            foreach (var k in new[] { "Strings2", "Quick2", "Double3", "Second3", "Method4", "Finale4", "Ovation5" }) { string key = k; Add("Stage", S + "_" + key, () => Stage(key)); }
            Add("Cross", X, () => Cross(""));
            foreach (var k in new[] { "Quickgrab2", "Longarm2", "Double3", "Cut3", "Ventriloquist4", "Encore4", "Master5" }) { string key = k; Add("Cross", X + "_" + key, () => Cross(key)); }
            Add("Curtain", C, () => Curtain(""));
            foreach (var k in new[] { "Velvet2", "Spotlight2", "Heavy3", "Trapdoor3", "Smoke4", "Iron4", "Finalact5" }) { string key = k; Add("Curtain", C + "_" + key, () => Curtain(key)); }

            Add("Extras", "Mummer_Hanger_Ballista", () => Hanger(Pal.Orange, Pal.OrangeDark, true));
            Add("Extras", "Mummer_Hanger_Aegis", () => Hanger(Pal.Blue, Pal.BlueDark, false));
            Add("Debuff", "Mummer_Marker", Marker);
            Add("Debuff", "Mummer_Heldstrings", Heldstrings);
            Add("Debuff", "Mummer_Tangle", Tangle);
            Add("Projectiles", "Mummer_Bolt", Bolt, true);
            return l;
        }

        // ---------------------------------------------------------------- shared pieces

        static void Plinth(Lp m, float r = 0.37f, int side = Pal.Wood3, int top = Pal.Wood1)
        {
            m.Prism(V(0, 0, 0), r, r * 0.95f, 0.055f, 8, side, top, -1, 22.5f);
        }

        // A spool lying along x: thread body, wooden flanges, brass axle caps.
        static void Spool(Lp m, Vector3 c, float r, float len, int thread, int flange = Pal.Wood1)
        {
            Vector3 a = c - Vector3.right * len * 0.5f, b = c + Vector3.right * len * 0.5f;
            m.Tube(a, b, r, r, 10, thread);
            for (float t = 0.15f; t < 0.9f; t += 0.25f)
                m.Tube(Vector3.Lerp(a, b, t), Vector3.Lerp(a, b, t + 0.03f), r * 1.02f, r * 1.02f, 10, thread == Pal.Silk ? Pal.Lilac0 : Pal.Felt3);
            m.Tube(a - Vector3.right * 0.022f, a, r * 1.4f, r * 1.4f, 10, flange, flange, flange);
            m.Tube(b, b + Vector3.right * 0.022f, r * 1.4f, r * 1.4f, 10, flange, flange, flange);
        }

        // Two-sided triangle flag on a pole top.
        static void Pennant(Lp m, Vector3 at, float size, int col, int back, float yaw = 0f)
        {
            Vector3 dir = Quaternion.Euler(0, yaw, 0) * Vector3.right;
            m.Tube(at, at + Vector3.up * size * 1.4f, 0.008f, 0.008f, 5, Pal.Wood4);
            Vector3 top = at + Vector3.up * size * 1.4f;
            m.Tri2(top, top - Vector3.up * size * 0.6f, top + dir * size - Vector3.up * size * 0.3f, col, back);
        }

        // A hanging drape: cloth from the top edge (a to b) down to 'drop', pinched toward 'tie' at tieY; vertical folds
        // alternate in depth. Two-sided.
        static void Drape(Lp m, Vector3 a, Vector3 b, float drop, Vector3 tie, float tieY, int folds, int front, int back, float depth = 0.03f)
        {
            int rows = 6;
            var grid = new Vector3[folds + 1, rows + 1];
            for (int j = 0; j <= rows; j++)
            {
                float v = j / (float)rows, y = Mathf.Lerp(a.y, a.y - drop, v);
                float pinch = 1f - Mathf.Clamp01(1f - Mathf.Abs(y - tieY) / Mathf.Max(0.01f, (a.y - tieY))) * 0.85f;
                for (int i = 0; i <= folds; i++)
                {
                    float u = i / (float)folds;
                    Vector3 top = Vector3.Lerp(a, b, u);
                    Vector3 p = Vector3.Lerp(new Vector3(tie.x, y, tie.z), new Vector3(top.x, y, top.z), pinch);
                    p.z += (i % 2 == 0 ? 1f : -1f) * depth * (0.4f + 0.6f * pinch);
                    grid[i, j] = p;
                }
            }
            for (int j = 0; j < rows; j++)
                for (int i = 0; i < folds; i++)
                {
                    int col = i % 2 == 0 ? front : back;
                    m.Tri2(grid[i, j], grid[i + 1, j], grid[i + 1, j + 1], col, back);
                    m.Tri2(grid[i, j], grid[i + 1, j + 1], grid[i, j + 1], col, back);
                }
        }

        // A scalloped valance along x at height y, depth z (little hanging semicircles).
        static void Valance(Lp m, float x0, float x1, float y, float z, float h, int col, int back)
        {
            int n = Mathf.Max(2, Mathf.RoundToInt((x1 - x0) / 0.1f));
            float w = (x1 - x0) / n;
            m.Block(V((x0 + x1) * 0.5f, y - h * 0.4f, z), V(x1 - x0, h * 0.4f, 0.02f), 0.005f, col, col);
            for (int i = 0; i < n; i++)
            {
                float cx = x0 + w * (i + 0.5f);
                Vector3 l = V(cx - w * 0.5f, y - h * 0.4f, z - 0.011f), r = V(cx + w * 0.5f, y - h * 0.4f, z - 0.011f);
                Vector3 bot = V(cx, y - h, z - 0.011f);
                m.Tri2(l, r, bot, col, back);
                m.Tri2(l, V(cx - w * 0.3f, y - h * 0.85f, z - 0.011f), bot, col, back);
                m.Tri2(r, bot, V(cx + w * 0.3f, y - h * 0.85f, z - 0.011f), col, back);
            }
        }

        // A thin ring (low-poly torus) lying flat at height y.
        static void Ring(Lp m, Vector3 c, float r, float tube, int col, int segs = 12)
        {
            for (int i = 0; i < segs; i++)
            {
                float a0 = i / (float)segs * Mathf.PI * 2f, a1 = (i + 1) / (float)segs * Mathf.PI * 2f;
                m.Tube(c + V(Mathf.Cos(a0) * r, 0, Mathf.Sin(a0) * r), c + V(Mathf.Cos(a1) * r, 0, Mathf.Sin(a1) * r), tube, tube, 4, col);
            }
        }

        // A triangular prism pediment (the costume's triangle on the stage).
        static void Pediment(Lp m, float y, float halfW, float h, float zFront, float zBack, int face, int slope)
        {
            Vector3 fl = V(-halfW, y, zFront), fr = V(halfW, y, zFront), ft = V(0, y + h, zFront);
            Vector3 bl = V(-halfW, y, zBack), br = V(halfW, y, zBack), bt = V(0, y + h, zBack);
            Vector3 mid = V(0, y + h * 0.35f, (zFront + zBack) * 0.5f);
            m.Tri(fl, fr, ft, face, mid);
            m.Tri(bl, br, bt, face, mid);
            m.Quad(fl, ft, bt, bl, slope, mid);
            m.Quad(fr, ft, bt, br, slope, mid);
            m.Quad(fl, fr, br, bl, face, mid);
        }

        static void Thread(Lp m, Vector3 a, Vector3 b, int col, float r = 0.006f) { m.Tube(a, b, r, r, 4, col); }

        static void Gem(Lp m, Vector3 c, float r, int col, int top)
        {
            m.Tube(c, c + Vector3.up * r * 1.3f, r, 0f, 5, top);
            m.Tube(c, c - Vector3.up * r * 1.0f, r, 0f, 5, col);
        }

        // ---------------------------------------------------------------- Loom

        static Lp Loom(string up)
        {
            var m = new Lp();
            bool gold = up == "Golden5";
            int frame = gold ? Pal.Gold2 : Pal.Wood2, frameTop = gold ? Pal.Gold0 : Pal.Wood0;
            Plinth(m, 0.38f, gold ? Pal.Gold3 : Pal.Wood3, gold ? Pal.Gold1 : Pal.Wood1);
            float wide = up == "Weft2" ? 0.34f : 0.26f;
            float apex = gold ? 0.82f : 0.74f;
            // two A-frames, one each side: the triangle is Mummer's shape
            foreach (float sx in new[] { -wide, wide })
            {
                m.Tube(V(sx, 0.05f, -0.2f), V(sx, apex, 0f), 0.032f, 0.026f, 6, frame);
                m.Tube(V(sx, 0.05f, 0.2f), V(sx, apex, 0f), 0.032f, 0.026f, 6, frame);
                m.Tube(V(sx, 0.3f, -0.13f), V(sx, 0.3f, 0.13f), 0.018f, 0.018f, 5, frame);
                m.Ball(V(sx, apex, 0f), Vector3.one * 0.04f, 6, 4, gold ? Pal.Gold0 : Pal.Brass, frameTop);
                if (gold) m.Tube(V(sx, apex + 0.03f, 0), V(sx, apex + 0.16f, 0), 0.04f, 0f, 3, Pal.Gold1);
            }
            // axle and the great spool
            m.Tube(V(-wide - 0.04f, apex - 0.02f, 0), V(wide + 0.04f, apex - 0.02f, 0), 0.016f, 0.016f, 6, Pal.Brass);
            int thread = up == "Silk3" ? Pal.Silk : up == "Orethread3" ? Pal.Teal : gold ? Pal.ThreadGold : Pal.Felt2;
            Spool(m, V(0, apex - 0.02f, 0), 0.1f, wide * 1.35f, thread, gold ? Pal.Gold1 : Pal.Wood1);
            // warp beam at the front and the threads running down to it
            m.Block(V(0, 0.2f, -0.2f), V(wide * 2f + 0.06f, 0.05f, 0.05f), 0.012f, frame, frameTop);
            for (int i = -2; i <= 2; i++)
                Thread(m, V(i * 0.05f, apex - 0.1f, -0.06f), V(i * 0.07f, 0.25f, -0.2f), i % 2 == 0 ? thread : Pal.ThreadPink);
            // the shuttle resting on the beam
            m.Ball(V(0.08f, 0.27f, -0.21f), V(0.07f, 0.025f, 0.025f), 6, 3, Pal.Brass, Pal.Gold0);
            switch (up)
            {
                case "Shuttle2":
                    Spool(m, V(0, 0.32f, 0.12f), 0.06f, 0.26f, Pal.ThreadPink);
                    m.Ball(V(-0.1f, 0.27f, -0.21f), V(0.09f, 0.03f, 0.03f), 6, 3, Pal.Gold1, Pal.Gold0);
                    m.Tube(V(-0.2f, 0.27f, -0.21f), V(-0.24f, 0.27f, -0.21f), 0.012f, 0f, 4, Pal.Gold2);
                    break;
                case "Weft2":
                    foreach (float sx in new[] { -0.42f, 0.42f })
                    {
                        m.Tube(V(sx * 0.8f, 0.05f, 0.24f), V(sx, 0.42f, 0.24f), 0.02f, 0.016f, 5, Pal.Wood2);
                        Spool(m, V(sx, 0.44f, 0.24f), 0.045f, 0.1f, Pal.ThreadBlue);
                        Thread(m, V(sx, 0.44f, 0.2f), V(sx * 0.4f, 0.22f, -0.2f), Pal.ThreadBlue, 0.005f);
                    }
                    break;
                case "Silk3":
                    m.Ball(V(-0.17f, 0.12f, 0.16f), V(0.06f, 0.08f, 0.06f), 7, 5, Pal.Silk, Pal.White);
                    m.Ball(V(0.2f, 0.1f, 0.18f), V(0.045f, 0.06f, 0.045f), 7, 5, Pal.Silk, Pal.White);
                    break;
                case "Orethread3":
                    Gem(m, V(-0.22f, 0.06f, 0.16f), 0.05f, Pal.Teal, Pal.Mint);
                    Gem(m, V(0.24f, 0.06f, 0.12f), 0.04f, Pal.Teal, Pal.Mint);
                    Gem(m, V(0.16f, 0.06f, 0.24f), 0.035f, Pal.Teal, Pal.Mint);
                    break;
                case "Patchwork4":
                    // a patchwork quilt thrown over the front warp beam, hanging down toward the viewer in squares
                    for (int i = 0; i < 5; i++)
                        for (int j = 0; j < 2; j++)
                        {
                            float x0 = -0.25f + i * 0.1f, y0 = 0.25f - j * 0.11f, z0 = -0.23f - j * 0.025f;
                            int col = (i + j) % 3 == 0 ? Pal.Velvet1 : (i + j) % 3 == 1 ? Pal.ThreadBlue : Pal.Gold1;
                            m.Tri2(V(x0, y0, z0), V(x0 + 0.1f, y0, z0), V(x0 + 0.1f, y0 - 0.11f, z0 - 0.025f), col, Pal.Cream2);
                            m.Tri2(V(x0, y0, z0), V(x0 + 0.1f, y0 - 0.11f, z0 - 0.025f), V(x0, y0 - 0.11f, z0 - 0.025f), col, Pal.Cream2);
                        }
                    m.Block(V(0, 0.27f, -0.21f), V(0.56f, 0.025f, 0.05f), 0.008f, Pal.Cream1, Pal.Cream0);
                    break;
                case "Tapestry4":
                    // a tapestry pennant hung on the outer face of the left A-frame
                    {
                        float tx = -wide - 0.035f;
                        m.Tube(V(tx, 0.62f, -0.16f), V(tx, 0.62f, 0.16f), 0.012f, 0.012f, 5, Pal.Brass);
                        m.Tri2(V(tx, 0.61f, -0.15f), V(tx, 0.61f, 0.15f), V(tx, 0.14f, 0f), Pal.Velvet2, Pal.Velvet3);
                        m.Tri2(V(tx - 0.004f, 0.52f, -0.07f), V(tx - 0.004f, 0.52f, 0.07f), V(tx - 0.004f, 0.3f, 0f), Pal.Gold1, Pal.Gold1);
                    }
                    break;
            }
            return m;
        }

        // ---------------------------------------------------------------- Puppet Stage

        static Lp Stage(string up)
        {
            var m = new Lp();
            bool grand = up == "Ovation5";
            int wood = grand ? Pal.Gold2 : Pal.Wood2, woodLight = grand ? Pal.Gold0 : Pal.Wood0;
            // platform, floor and backdrop
            m.Block(V(0, 0, 0.02f), V(0.74f, 0.16f, 0.58f), 0.03f, Pal.Wood3, Pal.Wood1);
            m.Block(V(0, 0.16f, 0.02f), V(0.66f, 0.012f, 0.5f), 0.008f, Pal.Wood0, Pal.Wood0);
            m.Block(V(0, 0.16f, 0.24f), V(0.62f, 0.66f, 0.04f), 0.01f, Pal.PlumDark, Pal.Plum);
            // proscenium posts and top beam
            foreach (float sx in new[] { -0.31f, 0.31f })
                m.Block(V(sx, 0.16f, -0.18f), V(0.08f, 0.68f, 0.08f), 0.015f, wood, woodLight);
            m.Block(V(0, 0.82f, -0.17f), V(0.72f, 0.08f, 0.1f), 0.015f, wood, woodLight);
            // the triangle pediment and its pennant
            Pediment(m, 0.9f, 0.37f, grand ? 0.28f : 0.2f, -0.22f, -0.12f, grand ? Pal.Gold1 : Pal.Velvet2, grand ? Pal.Gold2 : Pal.Velvet3);
            if (up != "Strings2") Pennant(m, V(0, grand ? 1.18f : 1.1f, -0.17f), 0.1f, Pal.Felt2, Pal.Felt4);
            // curtains tied back to the posts, and the valance
            Drape(m, V(-0.27f, 0.8f, -0.12f), V(-0.04f, 0.8f, -0.12f), 0.62f, V(-0.25f, 0f, -0.12f), 0.42f, 5, Pal.Velvet1, Pal.Velvet3);
            Drape(m, V(0.04f, 0.8f, -0.12f), V(0.27f, 0.8f, -0.12f), 0.62f, V(0.25f, 0f, -0.12f), 0.42f, 5, Pal.Velvet1, Pal.Velvet3);
            Valance(m, -0.3f, 0.3f, 0.82f, -0.13f, 0.09f, Pal.Velvet2, Pal.Velvet3);
            // a little footlight lip
            m.Block(V(0, 0.16f, -0.25f), V(0.6f, 0.03f, 0.04f), 0.008f, Pal.Brass, Pal.Gold0);
            switch (up)
            {
                case "Strings2":
                    // a marionette control bar hovering over the pediment, strings down to the stage
                    m.Block(V(0, 1.2f, -0.1f), V(0.4f, 0.03f, 0.04f), 0.01f, Pal.Wood1, Pal.Wood0);
                    m.Block(V(0, 1.2f, -0.1f), V(0.04f, 0.03f, 0.3f), 0.01f, Pal.Wood1, Pal.Wood0);
                    foreach (float sx in new[] { -0.18f, 0.18f })
                        Thread(m, V(sx, 1.2f, -0.1f), V(sx * 0.6f, 0.3f, 0.05f), Pal.Lilac1);
                    Thread(m, V(0, 1.2f, -0.24f), V(0, 0.4f, 0.0f), Pal.Lilac1);
                    break;
                case "Quick2":
                    // a costume rack beside the stage
                    m.Tube(V(0.45f, 0.0f, 0.12f), V(0.45f, 0.62f, 0.12f), 0.016f, 0.016f, 5, Pal.Iron2);
                    m.Tube(V(0.45f, 0.62f, -0.1f), V(0.45f, 0.62f, 0.24f), 0.012f, 0.012f, 5, Pal.Iron2);
                    m.Tri2(V(0.43f, 0.6f, -0.05f), V(0.43f, 0.6f, 0.07f), V(0.43f, 0.32f, 0.01f), Pal.Orange, Pal.OrangeDark);
                    m.Tri2(V(0.43f, 0.6f, 0.1f), V(0.43f, 0.6f, 0.22f), V(0.43f, 0.3f, 0.16f), Pal.Blue, Pal.BlueDark);
                    break;
                case "Double3":
                    foreach (float sx in new[] { -0.45f, 0.45f })
                    {
                        m.Prism(V(sx, 0, 0.08f), 0.09f, 0.08f, 0.62f, 8, Pal.Wood2, Pal.Wood0, -1, 22.5f);
                        m.Prism(V(sx, 0.62f, 0.08f), 0.11f, 0f, 0.22f, 8, Pal.Velvet2, Pal.Velvet2, Pal.Velvet3, 22.5f);
                        Pennant(m, V(sx, 0.84f, 0.08f), 0.06f, Pal.Felt2, Pal.Felt4);
                    }
                    break;
                case "Second3":
                    m.Block(V(-0.18f, 0.17f, -0.15f), V(0.16f, 0.08f, 0.1f), 0.012f, Pal.Wood4, Pal.Wood2);
                    Spool(m, V(0.12f, 0.21f, -0.16f), 0.035f, 0.08f, Pal.ThreadPink);
                    Spool(m, V(0.22f, 0.21f, -0.12f), 0.035f, 0.08f, Pal.ThreadBlue);
                    m.Tube(V(-0.12f, 0.26f, -0.15f), V(-0.02f, 0.4f, -0.18f), 0.006f, 0.002f, 4, Pal.Iron0);
                    break;
                case "Method4":
                    // comedy and tragedy masks on the pediment
                    m.Ball(V(-0.08f, 0.99f, -0.235f), V(0.06f, 0.07f, 0.02f), 8, 4, Pal.Cream0, Pal.White);
                    m.Ball(V(0.08f, 0.99f, -0.235f), V(0.06f, 0.07f, 0.02f), 8, 4, Pal.Felt1, Pal.Felt0);
                    m.Ball(V(-0.1f, 1.01f, -0.25f), V(0.012f, 0.008f, 0.004f), 4, 2, Pal.Ink);
                    m.Ball(V(-0.06f, 1.01f, -0.25f), V(0.012f, 0.008f, 0.004f), 4, 2, Pal.Ink);
                    m.Ball(V(0.06f, 1.0f, -0.25f), V(0.012f, 0.008f, 0.004f), 4, 2, Pal.Ink);
                    m.Ball(V(0.1f, 1.0f, -0.25f), V(0.012f, 0.008f, 0.004f), 4, 2, Pal.Ink);
                    m.Block(V(0, 0.9f, -0.235f), V(0.7f, 0.02f, 0.02f), 0.004f, Pal.Gold1, Pal.Gold0);
                    break;
                case "Finale4":
                    foreach (float sx in new[] { -0.36f, 0.36f })
                    {
                        m.Tube(V(sx, 0.9f, -0.12f), V(sx * 1.3f, 1.18f, -0.18f), 0.04f, 0.05f, 8, Pal.Velvet1, Pal.Gold1, Pal.Gold2);
                        m.Ball(V(sx * 1.32f, 1.2f, -0.18f), Vector3.one * 0.03f, 5, 3, Pal.Gold0);
                    }
                    m.Ball(V(0, 0.55f, 0.21f), V(0.1f, 0.1f, 0.02f), 8, 3, Pal.Gold1, Pal.GoldPale);
                    break;
                case "Ovation5":
                    foreach (float sx in new[] { -0.44f, 0.44f })
                    {
                        m.Tube(V(sx, 0, -0.1f), V(sx, 0.7f, -0.1f), 0.02f, 0.02f, 6, Pal.Iron3);
                        m.Tube(V(sx, 0.72f, -0.1f), V(sx * 0.82f, 0.62f, -0.28f), 0.05f, 0.07f, 8, Pal.Iron3, Pal.GoldPale, Pal.Iron4);
                    }
                    m.Block(V(0, 0.0f, -0.36f), V(0.36f, 0.08f, 0.14f), 0.01f, Pal.Velvet2, Pal.Velvet1);
                    m.Ball(V(0, 1.2f, -0.18f), Vector3.one * 0.045f, 6, 4, Pal.Gold0, Pal.GoldPale);
                    break;
            }
            return m;
        }

        // The costume the Stage is stitching, on a little rack at its side (skins "Ballista" and "Aegis").
        static Lp Hanger(int col, int dark, bool ballista)
        {
            var m = new Lp();
            float x = 0.47f, z = -0.06f;
            m.Prism(V(x, 0, z), 0.05f, 0.05f, 0.02f, 6, Pal.Iron3, Pal.Iron2);
            m.Tube(V(x, 0.02f, z), V(x, 0.58f, z), 0.012f, 0.012f, 5, Pal.Iron2);
            m.Tube(V(x, 0.58f, z - 0.08f), V(x, 0.58f, z + 0.08f), 0.01f, 0.01f, 4, Pal.Iron2);
            // a little tunic in the costume colour
            m.Tri2(V(x - 0.01f, 0.57f, z - 0.08f), V(x - 0.01f, 0.57f, z + 0.08f), V(x - 0.01f, 0.28f, z), col, dark);
            m.Tri2(V(x - 0.01f, 0.5f, z - 0.11f), V(x - 0.01f, 0.5f, z + 0.11f), V(x - 0.01f, 0.36f, z), col, dark);
            if (ballista)
            {
                // a toy crossbow
                m.Block(V(x - 0.04f, 0.38f, z), V(0.03f, 0.03f, 0.16f), 0.005f, Pal.Wood3, Pal.Wood2);
                m.Tube(V(x - 0.04f, 0.38f, z - 0.09f), V(x - 0.04f, 0.46f, z - 0.02f), 0.008f, 0.008f, 4, Pal.Wood4);
                m.Tube(V(x - 0.04f, 0.38f, z - 0.09f), V(x - 0.04f, 0.3f, z - 0.02f), 0.008f, 0.008f, 4, Pal.Wood4);
            }
            else
                m.Tube(V(x - 0.03f, 0.42f, z), V(x - 0.06f, 0.42f, z), 0.1f, 0.1f, 10, Pal.Blue, Pal.BlueDark, Pal.BlueDark);
            return m;
        }

        // ---------------------------------------------------------------- Marionette Cross

        static Lp Cross(string up)
        {
            var m = new Lp();
            bool gold = up == "Master5";
            float h = up == "Longarm2" ? 1.42f : gold ? 1.32f : 1.22f;
            float arm = up == "Longarm2" ? 0.4f : 0.32f;
            int wood = gold ? Pal.Gold2 : Pal.Wood2, light = gold ? Pal.Gold0 : Pal.Wood0;
            m.Prism(V(0, 0, 0), 0.3f, 0.27f, 0.07f, 8, Pal.Wood3, Pal.Wood1, -1, 22.5f);
            m.Prism(V(0, 0.07f, 0), 0.14f, 0.1f, 0.1f, 8, Pal.Wood2, Pal.Wood1, -1, 22.5f);
            // the turned pole with brass collars
            m.Tube(V(0, 0.15f, 0), V(0, h, 0), 0.058f, 0.045f, 8, wood);
            m.Tube(V(0, 0.36f, 0), V(0, 0.41f, 0), 0.07f, 0.07f, 8, Pal.Brass, Pal.Gold0, Pal.Gold0);
            m.Tube(V(0, h - 0.18f, 0), V(0, h - 0.13f, 0), 0.064f, 0.064f, 8, Pal.Brass, Pal.Gold0, Pal.Gold0);
            // the control cross at the top
            m.Block(V(0, h, 0), V(arm * 2f, 0.07f, 0.08f), 0.018f, wood, light);
            m.Block(V(0, h, 0), V(0.08f, 0.07f, arm * 1.4f), 0.018f, wood, light);
            m.Ball(V(0, h + 0.07f, 0), Vector3.one * 0.055f, 7, 4, gold ? Pal.Gold0 : Pal.Brass, Pal.GoldPale);
            int strCol = gold ? Pal.ThreadGold : Pal.Lilac1;
            var ends = new List<Vector3> { V(-arm, h, 0), V(arm, h, 0), V(0, h, -arm * 0.7f), V(0, h, arm * 0.7f) };
            if (gold) { ends.Add(V(-arm * 0.6f, h, -arm * 0.4f)); ends.Add(V(arm * 0.6f, h, arm * 0.4f)); }
            foreach (Vector3 e in ends)
            {
                Thread(m, e, e - Vector3.up * (h - 0.5f), strCol);
                Vector3 hook = e - Vector3.up * (h - 0.5f);
                m.Tube(hook, hook - Vector3.up * 0.05f, 0.012f, 0.004f, 4, Pal.Brass);
            }
            // the costume's triangle as a finial
            if (up != "Ventriloquist4")
                m.Tube(V(0, h + 0.09f, 0), V(0, h + (gold ? 0.3f : 0.22f), 0), gold ? 0.07f : 0.05f, 0f, 3, gold ? Pal.Gold1 : Pal.Felt2, gold ? Pal.Gold1 : Pal.Felt2);
            switch (up)
            {
                case "Quickgrab2":
                    m.Tube(V(0, h - 0.4f, -0.035f), V(0, h - 0.4f, 0.035f), 0.13f, 0.13f, 10, Pal.Brass, Pal.Gold0, Pal.Gold0);
                    for (int i = 0; i < 4; i++) { float a = i * Mathf.PI / 4f; m.Tube(V(Mathf.Cos(a) * -0.12f, h - 0.4f + Mathf.Sin(a) * -0.12f, -0.045f), V(Mathf.Cos(a) * 0.12f, h - 0.4f + Mathf.Sin(a) * 0.12f, -0.045f), 0.01f, 0.01f, 4, Pal.Wood4); }
                    break;
                case "Double3":
                    m.Tube(V(0, 0.7f, 0), V(0.3f, 0.86f, 0), 0.022f, 0.02f, 6, Pal.Wood2);
                    m.Block(V(0.3f, 0.86f, 0), V(0.32f, 0.04f, 0.05f), 0.01f, Pal.Wood2, Pal.Wood0);
                    m.Block(V(0.3f, 0.86f, 0), V(0.05f, 0.04f, 0.24f), 0.01f, Pal.Wood2, Pal.Wood0);
                    foreach (Vector3 e in new[] { V(0.15f, 0.86f, 0), V(0.45f, 0.86f, 0) })
                        Thread(m, e, e - Vector3.up * 0.42f, Pal.ThreadPink);
                    break;
                case "Cut3":
                    // big tailor's scissors clipped to the pole
                    m.Tube(V(-0.06f, h - 0.3f, -0.05f), V(0.24f, h - 0.1f, -0.05f), 0.016f, 0.002f, 4, Pal.Iron0);
                    m.Tube(V(-0.06f, h - 0.1f, -0.05f), V(0.24f, h - 0.3f, -0.05f), 0.016f, 0.002f, 4, Pal.Iron1);
                    Ring(m, V(-0.1f, h - 0.32f, -0.05f), 0.035f, 0.01f, Pal.Velvet2, 8);
                    Ring(m, V(-0.1f, h - 0.08f, -0.05f), 0.035f, 0.01f, Pal.Velvet2, 8);
                    break;
                case "Ventriloquist4":
                    // a ventriloquist's dummy head on top, with its hinged jaw
                    m.Ball(V(0, h + 0.18f, 0), V(0.1f, 0.11f, 0.1f), 8, 6, Pal.Cream1, Pal.Cream0);
                    m.Block(V(0, h + 0.06f, -0.02f), V(0.12f, 0.05f, 0.1f), 0.01f, Pal.Cream2, Pal.Cream1);
                    m.Ball(V(-0.04f, h + 0.2f, -0.09f), Vector3.one * 0.018f, 5, 3, Pal.Ink);
                    m.Ball(V(0.04f, h + 0.21f, -0.09f), Vector3.one * 0.018f, 5, 3, Pal.Ink);
                    m.Ball(V(0, h + 0.13f, -0.1f), V(0.03f, 0.012f, 0.01f), 5, 2, Pal.Velvet2);
                    m.Tube(V(0, h + 0.28f, 0), V(0, h + 0.36f, 0), 0.07f, 0f, 3, Pal.Velvet1);
                    break;
                case "Encore4":
                    foreach (Vector3 e in ends)
                    {
                        Vector3 o = e + Vector3.up * 0.04f;
                        m.Tri2(o, o + V(-0.06f, 0.04f, 0), o + V(-0.06f, -0.04f, 0), Pal.Rose, Pal.Velvet1);
                        m.Tri2(o, o + V(0.06f, 0.04f, 0), o + V(0.06f, -0.04f, 0), Pal.Rose, Pal.Velvet1);
                    }
                    break;
                case "Master5":
                    for (int i = 0; i < 5; i++) { float a = i / 5f * Mathf.PI * 2f; m.Tube(V(Mathf.Cos(a) * 0.08f, h + 0.1f, Mathf.Sin(a) * 0.08f), V(Mathf.Cos(a) * 0.1f, h + 0.22f, Mathf.Sin(a) * 0.1f), 0.025f, 0f, 3, Pal.Gold1); }
                    m.Prism(V(0, 0, 0), 0.27f, 0.25f, 0.02f, 8, Pal.Gold3, Pal.Gold1, -1, 22.5f);
                    break;
            }
            return m;
        }

        // ---------------------------------------------------------------- Curtain (walk-through trap)

        // Two posts at the tile's sides with a rod and valance between them; the drapes are tied back to the posts so
        // the middle stays open - foes walk through it. Low enough to read as a doorway, not a wall.
        static Lp Curtain(string up)
        {
            var m = new Lp();
            bool iron = up == "Iron4", gold = up == "Finalact5";
            int post = iron ? Pal.Iron3 : gold ? Pal.Gold2 : Pal.Wood2, postTop = iron ? Pal.Iron2 : gold ? Pal.Gold0 : Pal.Brass;
            int drape = up == "Velvet2" || gold ? Pal.Velvet1 : up == "Heavy3" ? Pal.Velvet2 : Pal.Plum, drapeBack = up == "Velvet2" || gold ? Pal.Velvet3 : Pal.PlumDark;
            float h = gold ? 0.82f : 0.72f;
            foreach (float sx in new[] { -0.4f, 0.4f })
            {
                m.Prism(V(sx, 0, 0), 0.07f, 0.06f, 0.04f, 6, post, postTop, -1, 30f);
                m.Tube(V(sx, 0.04f, 0), V(sx, h, 0), 0.03f, 0.026f, 6, post);
                m.Ball(V(sx, h + 0.02f, 0), Vector3.one * 0.04f, 6, 4, postTop, Pal.GoldPale);
                if (iron) m.Tube(V(sx, h + 0.05f, 0), V(sx, h + 0.16f, 0), 0.03f, 0f, 4, Pal.Iron1);
            }
            m.Tube(V(-0.42f, h - 0.03f, 0), V(0.42f, h - 0.03f, 0), 0.014f, 0.014f, 6, iron ? Pal.Iron2 : Pal.Brass);
            // the drapes, tied back to each post at knee height
            float drop = h - 0.02f;
            int folds = up == "Heavy3" ? 6 : 4;
            Drape(m, V(-0.38f, h - 0.04f, 0.0f), V(-0.12f, h - 0.04f, 0.0f), drop, V(-0.37f, 0, 0), 0.3f, folds, drape, drapeBack, up == "Heavy3" ? 0.045f : 0.03f);
            Drape(m, V(0.12f, h - 0.04f, 0.0f), V(0.38f, h - 0.04f, 0.0f), drop, V(0.37f, 0, 0), 0.3f, folds, drape, drapeBack, up == "Heavy3" ? 0.045f : 0.03f);
            Valance(m, -0.38f, 0.38f, h - 0.02f, -0.02f, up == "Heavy3" ? 0.14f : 0.1f, drape, drapeBack);
            if (up == "Heavy3") Valance(m, -0.36f, 0.36f, h - 0.1f, -0.035f, 0.08f, Pal.Velvet1, Pal.Velvet3);
            // tie-backs (gold tassels on Velvet)
            foreach (float sx in new[] { -0.36f, 0.36f })
            {
                m.Ball(V(sx, 0.3f, -0.02f), V(0.03f, 0.02f, 0.03f), 6, 3, up == "Velvet2" || gold ? Pal.Gold1 : Pal.Felt2);
                if (up == "Velvet2" || gold) m.Tube(V(sx, 0.29f, -0.03f), V(sx, 0.2f, -0.03f), 0.022f, 0.006f, 5, Pal.Gold1);
            }
            switch (up)
            {
                case "Spotlight2":
                    m.Tube(V(-0.4f, h + 0.05f, 0), V(-0.34f, h + 0.18f, -0.06f), 0.012f, 0.012f, 4, Pal.Iron3);
                    m.Tube(V(-0.36f, h + 0.18f, -0.03f), V(-0.26f, h + 0.12f, -0.14f), 0.045f, 0.065f, 8, Pal.Iron3, Pal.GoldPale, Pal.Iron4);
                    break;
                case "Trapdoor3":
                    m.Block(V(0, 0, 0), V(0.3f, 0.025f, 0.3f), 0.01f, Pal.Wood3, Pal.Wood1);
                    m.Block(V(0, 0.025f, 0), V(0.3f, 0.006f, 0.02f), 0.002f, Pal.Wood4, Pal.Wood4);
                    Ring(m, V(0.08f, 0.035f, -0.08f), 0.03f, 0.007f, Pal.Brass, 8);
                    break;
                case "Smoke4":
                    foreach (float sx in new[] { -0.4f, 0.4f })
                    {
                        m.Tube(V(sx, h + 0.06f, -0.01f), V(sx, h + 0.06f, 0.01f), 0.07f, 0.07f, 10, Pal.Steel, Pal.Silk, Pal.SteelDark);
                    }
                    m.Ball(V(-0.1f, 0.08f, -0.12f), V(0.08f, 0.06f, 0.08f), 6, 4, Pal.Lilac0, Pal.White);
                    m.Ball(V(0.12f, 0.06f, -0.08f), V(0.07f, 0.05f, 0.07f), 6, 4, Pal.Lilac1, Pal.Lilac0);
                    m.Ball(V(0.02f, 0.1f, 0.12f), V(0.06f, 0.05f, 0.06f), 6, 4, Pal.Lilac0, Pal.White);
                    break;
                case "Finalact5":
                    // a gilded arch and crest over the opening
                    for (int i = 0; i < 6; i++)
                    {
                        float a0 = Mathf.PI * i / 6f, a1 = Mathf.PI * (i + 1) / 6f;
                        m.Tube(V(Mathf.Cos(a0) * -0.4f, h + Mathf.Sin(a0) * 0.18f, 0.02f), V(Mathf.Cos(a1) * -0.4f, h + Mathf.Sin(a1) * 0.18f, 0.02f), 0.025f, 0.025f, 5, Pal.Gold1);
                    }
                    Pediment(m, h + 0.16f, 0.1f, 0.14f, -0.01f, 0.05f, Pal.Gold1, Pal.Gold2);
                    break;
            }
            return m;
        }

        // ---------------------------------------------------------------- debuff meshes (attached to foes by the game)

        // Marked: a plum downward triangle floating over the foe, clear of tall foe sprites (run 4 close-up: at 1.15 it
        // hid behind a Dagger's head).
        static Lp Marker()
        {
            var m = new Lp();
            Vector3 c = V(0, 1.6f, 0);
            float r = 0.21f;
            Vector3 a = c + V(-r, 0.08f, 0), b = c + V(r, 0.08f, 0), d = c + V(0, 0.08f, r * 0.9f), tip = c + V(0, -0.2f, r * 0.3f);
            Vector3 mid = c + V(0, 0.0f, r * 0.3f);
            m.Tri(a, b, d, Pal.Velvet0, mid);
            m.Tri(a, b, tip, Pal.Velvet1, mid);
            m.Tri(b, d, tip, Pal.Velvet2, mid);
            m.Tri(d, a, tip, Pal.Velvet2, mid);
            return m;
        }

        // Held: the marionette control above the foe, with strings down to it.
        static Lp Heldstrings()
        {
            var m = new Lp();
            float h = 1.55f;
            m.Block(V(0, h, 0), V(0.42f, 0.035f, 0.04f), 0.008f, Pal.Wood2, Pal.Wood0);
            m.Block(V(0, h, 0), V(0.04f, 0.035f, 0.28f), 0.008f, Pal.Wood2, Pal.Wood0);
            foreach (Vector3 e in new[] { V(-0.21f, h, 0), V(0.21f, h, 0), V(0, h, -0.14f), V(0, h, 0.14f) })
                Thread(m, e, V(e.x * 0.4f, 0.5f, e.z * 0.4f), Pal.Lilac0, 0.007f);
            return m;
        }

        // Tangled: loops of lilac thread around the foe's feet.
        static Lp Tangle()
        {
            var m = new Lp();
            Ring(m, V(0, 0.12f, 0), 0.2f, 0.014f, Pal.Felt2, 12);
            Ring(m, V(0.03f, 0.2f, -0.02f), 0.17f, 0.012f, Pal.Felt1, 12);
            Ring(m, V(-0.02f, 0.06f, 0.02f), 0.23f, 0.012f, Pal.Velvet1, 12);
            return m;
        }

        // ---------------------------------------------------------------- projectile

        // The Ballista Understudy's felt bolt: a soft dart with a triangle fletching.
        static Lp Bolt()
        {
            var m = new Lp();
            m.Tube(V(0, 0.5f, -0.12f), V(0, 0.5f, 0.1f), 0.014f, 0.014f, 5, Pal.Wood1);
            m.Tube(V(0, 0.5f, 0.1f), V(0, 0.5f, 0.17f), 0.03f, 0f, 6, Pal.Felt2);
            m.Tri2(V(0, 0.5f, -0.12f), V(0, 0.56f, -0.16f), V(0, 0.5f, -0.06f), Pal.Orange, Pal.OrangeDark);
            m.Tri2(V(0, 0.5f, -0.12f), V(0, 0.44f, -0.16f), V(0, 0.5f, -0.06f), Pal.Orange, Pal.OrangeDark);
            return m;
        }
    }
}
