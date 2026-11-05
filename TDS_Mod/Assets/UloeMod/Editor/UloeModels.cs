// Uloe asset generator: every tower model - the Magnifying Glass, the Power Bank, the Mirror and the Flood Light, each
// upgrade's look (named <Tower>_<Key><Tier>, Code/02_Data.cs Upg.Model), Light's three looks, the plug and the two shots.
// TEMPORARY MODELS: the scope gives the towers' look as "Towers carry glass, screw rings and cords" (docs/scope/10), and
// docs/STYLE.md puts the vessel's motif on the top or front face. Built sleek and low-poly like Sno Sno's and Buster's towers -
// bevelled solids, turned posts - never voxels. Upgrades keep the base silhouette and add one clear feature.
// Units: 1 = one tile; y measured up from the ground; the front faces -z.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace UloeBuild
{
    public class ModelDef
    {
        public string Name;
        public string Group;     // Glass, Bank, Mirror, Flood, Extras, Projectiles
        public bool Projectile;
        public Func<Lp> Build;
        // A moving part (Argo's custom aim): built in tower space like the body, placed at its pivot (y up from the ground).
        public Func<Lp> Part;
        public string PartName = "";
        public Vector3 PartPivot;
    }

    public static class UloeModels
    {
        static Vector3 V(float x, float y, float z) { return new Vector3(x, y, z); }

        public const float LensY = 0.42f + 0.5f;   // the lens's centre, up from the ground (Code/02_Data.cs BeamEye over the entity's y = 0.5)
        public const float LampY = 0.56f;

        public static List<ModelDef> All()
        {
            var l = new List<ModelDef>();
            void Add(string group, string name, Func<Lp> f, bool proj = false) { l.Add(new ModelDef { Group = group, Name = name, Build = f, Projectile = proj }); }
            void AddPart(string group, string name, Func<Lp> f, string part, Func<Lp> p, Vector3 pivot) { l.Add(new ModelDef { Group = group, Name = name, Build = f, Part = p, PartName = part, PartPivot = pivot }); }

            const string G = "Tower_Uloeglass", B = "Tower_Uloebank", M = "Tower_Uloemirror", F = "Tower_Uloeflood";
            AddPart("Glass", G, () => GlassStand(""), "Lens", () => Lens(""), V(0, LensY, 0));
            // One look per upgrade, named Owner_KeyTier (Code/02_Data.cs Upg.Model); the code falls back to the tower's own.
            foreach (var k in new[] { "Burn2", "Widelens2", "Polished3", "Quick3", "Prism3", "Sunspot4", "Focal4", "Deathray5" }) { string key = k; AddPart("Glass", G + "_" + key, () => GlassStand(key), "Lens", () => Lens(key), V(0, LensY, 0)); }
            Add("Bank", B, () => Bank(""));
            foreach (var k in new[] { "Short2", "Extracell2", "Static3", "Bright3", "Surge3", "Chain4", "Swarm4", "Supernova5" }) { string key = k; Add("Bank", B + "_" + key, () => Bank(key)); }
            Add("Mirror", M, () => Mirror(""));
            foreach (var k in new[] { "Silver2", "Vanity2", "Glare3", "Bounce3", "Tempered3", "Hall4", "Reflection4", "Heliostat5" }) { string key = k; Add("Mirror", M + "_" + key, () => Mirror(key)); }
            AddPart("Flood", F, () => FloodStand(""), "Lamp", () => Lamp(""), V(0, LampY, 0));
            foreach (var k in new[] { "Wide2", "Long2", "Halogen3", "Voltage3", "Filter4", "Searchlight4", "Stadium5", "Lighthouse5" }) { string key = k; AddPart("Flood", F + "_" + key, () => FloodStand(key), "Lamp", () => Lamp(key), V(0, LampY, 0)); }
            for (int i = 1; i <= 3; i++) { int band = i; Add("Extras", "Uloe_Lit" + band, () => LitLook(band)); }
            Add("Extras", "Uloe_Plug", PlugModel);
            Add("Projectiles", "Uloe_Spark", Spark, true);
            Add("Projectiles", "Uloe_Beamtip", Beamtip, true);
            return l;
        }

        // ---------------------------------------------------------------- shared pieces

        static void Plinth(Lp m, float r = 0.37f, int side = Pal.Steel4, int top = Pal.Steel3)
        {
            m.Prism(V(0, 0, 0), r, r * 0.95f, 0.055f, 8, side, top, -1, 22.5f);
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

        // A ring standing up in the x-y plane (a lens's frame).
        static void Hoop(Lp m, Vector3 c, float r, float tube, int col, int segs = 14)
        {
            for (int i = 0; i < segs; i++)
            {
                float a0 = i / (float)segs * Mathf.PI * 2f, a1 = (i + 1) / (float)segs * Mathf.PI * 2f;
                m.Tube(c + V(Mathf.Cos(a0) * r, Mathf.Sin(a0) * r, 0), c + V(Mathf.Cos(a1) * r, Mathf.Sin(a1) * r, 0), tube, tube, 5, col);
            }
        }

        // The motif: a ribbed screw column, alternating bright and dark rings, like the base of a bulb.
        static void ScrewRings(Lp m, Vector3 at, float r, float h, int rings)
        {
            float step = h / rings;
            for (int i = 0; i < rings; i++)
            {
                bool bright = i % 2 == 0;
                float rr = bright ? r : r * 0.86f;
                m.Prism(at + Vector3.up * (i * step), rr, rr, step, 10, bright ? Pal.Steel0 : Pal.Steel2, bright ? Pal.Steel0 : Pal.Steel2);
            }
        }

        static void Cord(Lp m, IList<Vector3> pts, float r = 0.016f) { m.Stem(pts, r, r, 5, Pal.Cord1); }

        // A plug lying on the ground: a block with two prongs, 'dir' the way the prongs point.
        static void Plug(Lp m, Vector3 at, float yaw, float scale = 1f)
        {
            m.Push(at, yaw, scale);
            m.Block(V(0, 0, 0), V(0.1f, 0.05f, 0.1f), 0.012f, Pal.Cord0, Pal.Cord3);
            m.Tube(V(-0.022f, 0.025f, 0.045f), V(-0.022f, 0.025f, 0.095f), 0.008f, 0.008f, 4, Pal.Steel1);
            m.Tube(V(0.022f, 0.025f, 0.045f), V(0.022f, 0.025f, 0.095f), 0.008f, 0.008f, 4, Pal.Steel1);
            m.Pop();
        }

        // A small glass bulb on a screw collar, bright when lit.
        static void Bulb(Lp m, Vector3 at, float r, bool lit = true)
        {
            m.Prism(at, r * 0.55f, r * 0.5f, r * 0.45f, 8, Pal.Steel1, Pal.Steel0);
            m.Ball(at + Vector3.up * (r * 1.15f), V(r, r, r), 8, 5, lit ? Pal.Light1 : Pal.Glass1, lit ? Pal.Light0 : Pal.Glass0, 0.55f);
        }

        // A spark: three thin two-sided shards round a point.
        static void Sparks(Lp m, Vector3 c, float size, int col)
        {
            for (int i = 0; i < 3; i++)
            {
                float a = i * 2.1f + 0.4f;
                Vector3 d = V(Mathf.Cos(a), 0.45f * (i - 1), Mathf.Sin(a)) * size;
                m.Tri2(c, c + d + V(0.015f, 0.015f, 0), c + d * 1.5f, col, col);
                m.Tri2(c, c + d - V(0.015f, 0.015f, 0), c + d * 1.5f, col, col);
            }
        }

        // ---------------------------------------------------------------- Magnifying Glass

        // A screw-ring column on a round steel foot, a brass yoke on top, a cord running off to a plug. The lens is the moving part.
        static Lp GlassStand(string up)
        {
            var m = new Lp();
            Plinth(m, 0.33f);
            m.Prism(V(0, 0.055f, 0), 0.2f, 0.13f, 0.08f, 8, Pal.Steel3, Pal.Steel2);
            ScrewRings(m, V(0, 0.135f, 0), 0.105f, 0.28f, 6);
            m.Prism(V(0, 0.415f, 0), 0.085f, 0.05f, 0.24f, 8, Pal.Brass2, Pal.Brass1);
            // the cord down the back and off to its plug
            Cord(m, new[] { V(0.05f, 0.4f, 0.08f), V(0.12f, 0.2f, 0.17f), V(0.25f, 0.06f, 0.24f), V(0.34f, 0.04f, 0.17f) });
            Plug(m, V(0.36f, 0.015f, 0.12f), 200f, 0.9f);
            switch (up)
            {
                case "Burn2":
                    // scorch marks on the foot and a little heap of embers
                    m.Disc(V(-0.14f, 0.056f, -0.14f), 0.09f, 8, Pal.Cord2, 0.006f);
                    m.Ball(V(-0.14f, 0.075f, -0.14f), V(0.035f, 0.02f, 0.035f), 6, 3, Pal.Ember, Pal.Light1);
                    break;
                case "Polished3":
                    // a folded polishing cloth on the foot and a tin of polish
                    m.Push(V(-0.15f, 0.056f, -0.12f), 25f, 1f);
                    m.Block(V(0, 0, 0), V(0.15f, 0.025f, 0.1f), 0.008f, Pal.Card1, Pal.Card0);
                    m.Pop();
                    m.Prism(V(0.13f, 0.056f, -0.17f), 0.05f, 0.05f, 0.04f, 10, Pal.Brass2, Pal.Brass0);
                    break;
                case "Quick3":
                    // a brass gear on the column's side, the clockwork that turns the lens quicker
                    m.Push(V(0.105f, 0.29f, -0.02f), Quaternion.Euler(0f, 0f, 90f), Vector3.one);
                    m.Prism(V(0, -0.014f, 0), 0.075f, 0.075f, 0.028f, 10, Pal.Brass2, Pal.Brass0, Pal.Brass1);
                    for (int i = 0; i < 8; i++)
                    {
                        float a = i / 8f * Mathf.PI * 2f;
                        m.Block(V(Mathf.Cos(a) * 0.085f, -0.012f, Mathf.Sin(a) * 0.085f), V(0.028f, 0.024f, 0.028f), 0.004f, Pal.Brass1, Pal.Brass0);
                    }
                    m.Prism(V(0, 0.014f, 0), 0.025f, 0.025f, 0.012f, 6, Pal.Steel1, Pal.Steel0);
                    m.Pop();
                    break;
                case "Deathray5":
                    // a warning band round the foot and cooling fins up the column
                    for (int i = 0; i < 12; i++)
                    {
                        float a = (i + 0.5f) / 12f * Mathf.PI * 2f;
                        m.Push(V(Mathf.Cos(a) * 0.3f, 0.055f, Mathf.Sin(a) * 0.3f), -a * Mathf.Rad2Deg, 1f);
                        m.Block(V(0, 0, 0), V(0.05f, 0.012f, 0.13f), 0.003f, i % 2 == 0 ? Pal.Light2 : Pal.Cord2, i % 2 == 0 ? Pal.Light2 : Pal.Cord2);
                        m.Pop();
                    }
                    for (int i = 0; i < 4; i++)
                    {
                        float a = i * Mathf.PI * 0.5f + Mathf.PI * 0.25f;
                        m.Block(V(Mathf.Cos(a) * 0.12f, 0.16f, Mathf.Sin(a) * 0.12f), V(0.05f, 0.24f, 0.05f), 0.006f, Pal.Cord3, Pal.Cord4);
                    }
                    m.Ball(V(-0.2f, 0.07f, -0.2f), Vector3.one * 0.03f, 6, 4, Pal.Fuse, Pal.Light1);
                    break;
            }
            return m;
        }

        // The lens, in tower space around its pivot: a brass hoop round a pane of glass, its handle pointing down and back, the
        // yoke's pin. The glass looks along -z.
        static Lp Lens(string up)
        {
            var m = new Lp();
            Vector3 c = V(0, LensY, 0);
            // Each upgrade's look is its own; Prism is only reached through Wide Lens, so it keeps the wider pane too.
            float r = up == "Widelens2" || up == "Prism3" ? 0.31f : 0.25f;
            bool hot = up == "Deathray5";
            int pane = hot ? Pal.Ember : Pal.Glass1, face = hot ? Pal.Light1 : Pal.Glass0, shine = hot ? Pal.Light0 : Pal.White;
            Hoop(m, c, r, 0.028f, hot ? Pal.Cord3 : Pal.Brass1);
            // the pane: a shallow double lens, its face the palest glass (red-hot on the Death Ray)
            m.Push(c, Quaternion.Euler(-90f, 0f, 0f), Vector3.one);
            m.Prism(V(0, -0.012f, 0), r * 0.98f, r * 0.98f, 0.024f, 14, pane, face, pane);
            m.Prism(V(0, 0.012f, 0), r * 0.92f, r * 0.55f, 0.018f, 14, face, shine);
            m.Prism(V(0, -0.03f, 0), r * 0.55f, r * 0.92f, 0.018f, 14, face, -1, shine);
            m.Pop();
            // the glint
            m.Tri2(c + V(-0.15f, 0.13f, -0.036f), c + V(-0.05f, 0.18f, -0.036f), c + V(-0.14f, 0.04f, -0.036f), Pal.Light0, Pal.Glass0);
            // the handle, screw-ringed where it meets the frame
            Vector3 h = c + V(0, -r - 0.01f, 0);
            m.Prism(h + Vector3.down * 0.06f, 0.034f, 0.04f, 0.05f, 8, Pal.Brass3, Pal.Brass1);
            m.Tube(c + V(-r, 0, 0), c + V(-r - 0.03f, 0, 0), 0.02f, 0.02f, 6, Pal.Brass2, Pal.Brass1);
            m.Tube(c + V(r, 0, 0), c + V(r + 0.03f, 0, 0), 0.02f, 0.02f, 6, Pal.Brass2, Pal.Brass1);
            switch (up)
            {
                case "Burn2":
                {
                    // a second, smaller lens out in front on two rods: the focus sharpens
                    Vector3 c2 = c + V(0, 0, -0.17f);
                    Hoop(m, c2, 0.12f, 0.016f, Pal.Brass2);
                    m.Push(c2, Quaternion.Euler(-90f, 0f, 0f), Vector3.one);
                    m.Prism(V(0, -0.008f, 0), 0.115f, 0.115f, 0.016f, 10, Pal.Light1, Pal.Light0, Pal.Light1);
                    m.Pop();
                    m.Tube(c + V(-0.19f, 0, -0.02f), c2 + V(-0.11f, 0, 0), 0.008f, 0.008f, 4, Pal.Brass3);
                    m.Tube(c + V(0.19f, 0, -0.02f), c2 + V(0.11f, 0, 0), 0.008f, 0.008f, 4, Pal.Brass3);
                    break;
                }
                case "Widelens2":
                    // the wider pane gets a second, outer rim
                    Hoop(m, c + V(0, 0, 0.012f), r + 0.03f, 0.016f, Pal.Brass2, 16);
                    break;
                case "Polished3":
                    // a four-point sparkle and a second glint: the glass polished to a shine
                    Star(m, c + V(0.09f, 0.08f, -0.04f), 0.11f, Pal.White, Pal.Glass0);
                    m.Tri2(c + V(0.06f, -0.1f, -0.036f), c + V(0.14f, -0.06f, -0.036f), c + V(0.1f, -0.14f, -0.036f), Pal.White, Pal.Glass0);
                    break;
                case "Quick3":
                    // knurled focus knobs on both pins
                    foreach (float sx in new[] { -1f, 1f })
                    {
                        m.Tube(c + V(sx * (r + 0.03f), 0, 0), c + V(sx * (r + 0.075f), 0, 0), 0.048f, 0.048f, 8, Pal.Brass1, Pal.Brass0, Pal.Brass0);
                        m.Tube(c + V(sx * (r + 0.075f), 0, 0), c + V(sx * (r + 0.09f), 0, 0), 0.03f, 0.03f, 8, Pal.Steel1, Pal.Steel0, Pal.Steel0);
                    }
                    break;
                case "Prism3":
                {
                    // a three-sided glass prism held out in front on two rods, the light splitting off it in three colours
                    Vector3 pc = c + V(0, -0.02f, -0.2f);
                    m.Push(pc, Quaternion.Euler(0f, 0f, 90f), Vector3.one);
                    m.Prism(V(0, -0.11f, 0), 0.07f, 0.07f, 0.22f, 3, Pal.Glass1, Pal.Glass0, Pal.Glass0, 90f);
                    m.Pop();
                    m.Tube(c + V(-0.12f, -0.02f, -0.02f), pc + V(-0.1f, 0, 0), 0.008f, 0.008f, 4, Pal.Brass3);
                    m.Tube(c + V(0.12f, -0.02f, -0.02f), pc + V(0.1f, 0, 0), 0.008f, 0.008f, 4, Pal.Brass3);
                    int[] split = { Pal.Fuse, Pal.Light2, Pal.Led0 };
                    for (int i = 0; i < 3; i++)
                    {
                        float x = (i - 1) * 0.07f;
                        m.Tri2(pc + V(x * 0.4f, -0.02f, -0.06f), pc + V(x * 0.4f + 0.025f, -0.02f, -0.06f), pc + V(x * 1.6f, -0.09f, -0.2f), split[i], split[i]);
                    }
                    break;
                }
                case "Sunspot4":
                    // a ring of sun rays round the frame
                    for (int i = 0; i < 12; i++)
                    {
                        float a = i / 12f * Mathf.PI * 2f + 0.13f, ra = r + 0.03f, rb = r + (i % 2 == 0 ? 0.12f : 0.08f);
                        Vector3 d = V(Mathf.Cos(a), Mathf.Sin(a), 0), side = V(-d.y, d.x, 0) * 0.035f;
                        m.Tri2(c + d * ra + side + V(0, 0, 0.005f), c + d * ra - side + V(0, 0, 0.005f), c + d * rb + V(0, 0, 0.005f), i % 2 == 0 ? Pal.Light1 : Pal.Light2, Pal.Light3);
                    }
                    break;
                case "Focal4":
                {
                    // three rods out to a small sighting ring with a bright point: where the beam is gathered
                    Vector3 fc = c + V(0, 0, -0.24f);
                    Hoop(m, fc, 0.075f, 0.014f, Pal.Brass2, 10);
                    for (int i = 0; i < 3; i++)
                    {
                        float a = i / 3f * Mathf.PI * 2f + Mathf.PI * 0.5f;
                        m.Tube(c + V(Mathf.Cos(a) * r * 0.9f, Mathf.Sin(a) * r * 0.9f, -0.02f), fc + V(Mathf.Cos(a) * 0.075f, Mathf.Sin(a) * 0.075f, 0), 0.009f, 0.009f, 4, Pal.Brass3);
                    }
                    m.Ball(fc, Vector3.one * 0.035f, 6, 4, Pal.Light0);
                    break;
                }
                case "Deathray5":
                    // heat-sink fins standing round the dark frame
                    for (int i = 0; i < 10; i++)
                    {
                        float a = i / 10f * Mathf.PI * 2f;
                        Vector3 d = V(Mathf.Cos(a), Mathf.Sin(a), 0);
                        m.Tube(c + d * (r + 0.02f), c + d * (r + 0.07f), 0.022f, 0.016f, 4, Pal.Cord4, Pal.Cord3);
                    }
                    break;
            }
            return m;
        }

        // A four-point sparkle in the x-y plane (a glint on glass), facing -z.
        static void Star(Lp m, Vector3 s, float size, int col, int back)
        {
            for (int i = 0; i < 4; i++)
            {
                float a = i * Mathf.PI * 0.5f;
                Vector3 d = V(Mathf.Cos(a), Mathf.Sin(a), 0), side = V(-d.y, d.x, 0) * size * 0.22f;
                m.Tri2(s + side, s - side, s + d * size, col, back);
            }
        }

        // ---------------------------------------------------------------- Power Bank

        // A chunky steel battery box with a yellow band, charge bars on its face, two screw terminals on top each holding a small
        // bulb, a cord out of the side to a plug.
        static Lp Bank(string up)
        {
            var m = new Lp();
            Plinth(m, 0.42f);
            m.Block(V(0, 0.055f, 0), V(0.62f, 0.34f, 0.46f), 0.04f, Pal.Steel2, Pal.Steel1, Pal.Steel0);
            // the yellow band and the charge bars on the front
            m.Block(V(0, 0.145f, -0.236f), V(0.58f, 0.15f, 0.02f), 0.006f, Pal.Light2, Pal.Light1);
            for (int i = 0; i < 4; i++)
            {
                int bar = i == 3 || up == "Supernova5" ? Pal.Led1 : Pal.Led0;
                m.Block(V(-0.195f + i * 0.13f, 0.165f, -0.25f), V(0.1f, 0.11f, 0.012f), 0.004f, bar, bar);
            }
            // the shade at the box's lower edge, a seam
            m.Block(V(0, 0.08f, -0.232f), V(0.6f, 0.02f, 0.012f), 0.003f, Pal.Steel4, Pal.Steel4);
            // terminals: a screw ring and a bulb on each
            float bulb = up == "Bright3" ? 0.13f : 0.095f;
            foreach (float sx in new[] { -0.17f, 0.17f })
            {
                ScrewRings(m, V(sx, 0.395f, 0.02f), 0.075f, 0.11f, 3);
                Bulb(m, V(sx, 0.505f, 0.02f), bulb);
            }
            // the cord
            Cord(m, new[] { V(0.31f, 0.2f, 0.1f), V(0.4f, 0.14f, 0.16f), V(0.42f, 0.06f, 0.27f), V(0.3f, 0.04f, 0.36f) });
            Plug(m, V(0.24f, 0.015f, 0.38f), 260f, 0.9f);
            if (up == "Short2")
            {
                // a frayed live wire between the terminals, sparks flying, one bar scorched
                Cord(m, new[] { V(-0.09f, 0.5f, 0.02f), V(-0.03f, 0.62f, -0.03f), V(0.03f, 0.56f, 0.02f), V(0.09f, 0.5f, 0.02f) }, 0.012f);
                Sparks(m, V(0f, 0.63f, -0.04f), 0.09f, Pal.Light0);
                Sparks(m, V(-0.28f, 0.42f, -0.1f), 0.06f, Pal.Light1);
                m.Block(V(0.195f, 0.165f, -0.255f), V(0.1f, 0.11f, 0.012f), 0.004f, Pal.Ember, Pal.Cord2);
            }
            switch (up)
            {
                case "Extracell2":
                    // another cell strapped to the box's side
                    m.Prism(V(-0.4f, 0.055f, 0.04f), 0.075f, 0.075f, 0.28f, 10, Pal.Light2, Pal.Light1);
                    m.Prism(V(-0.4f, 0.335f, 0.04f), 0.035f, 0.03f, 0.03f, 8, Pal.Steel1, Pal.Steel0);
                    m.Prism(V(-0.4f, 0.055f, 0.04f), 0.078f, 0.078f, 0.05f, 10, Pal.Steel2, Pal.Steel1);
                    m.Block(V(-0.36f, 0.2f, 0.04f), V(0.12f, 0.03f, 0.17f), 0.006f, Pal.Cord0, Pal.Cord3);
                    break;
                case "Static3":
                    // a little coil between the terminals, a steel ball on top crackling
                    m.Prism(V(0, 0.395f, 0.02f), 0.022f, 0.022f, 0.24f, 6, Pal.Steel2, Pal.Steel1);
                    for (int i = 0; i < 5; i++) Ring(m, V(0, 0.43f + i * 0.04f, 0.02f), 0.042f, 0.011f, i % 2 == 0 ? Pal.Brass1 : Pal.Brass2, 8);
                    m.Ball(V(0, 0.67f, 0.02f), Vector3.one * 0.05f, 8, 5, Pal.Steel0, Pal.SteelPale);
                    Sparks(m, V(0, 0.68f, 0.02f), 0.07f, Pal.Light0);
                    break;
                case "Bright3":
                    // a third bulb on the box's front edge, and every bulb burning bigger
                    m.Prism(V(0, 0.395f, -0.15f), 0.05f, 0.05f, 0.05f, 8, Pal.Steel2, Pal.Steel1);
                    Bulb(m, V(0, 0.445f, -0.15f), 0.08f);
                    break;
                case "Surge3":
                    // a white power strip on the ground in front, its sockets and red switch, wired to the box
                    m.Block(V(0, 0.055f, -0.36f), V(0.48f, 0.05f, 0.11f), 0.012f, Pal.Card1, Pal.Card0);
                    for (int i = 0; i < 3; i++)
                        m.Block(V(-0.12f + i * 0.1f, 0.105f, -0.36f), V(0.06f, 0.006f, 0.06f), 0.002f, Pal.Cord2, Pal.Cord2);
                    m.Block(V(0.19f, 0.105f, -0.36f), V(0.05f, 0.02f, 0.06f), 0.006f, Pal.Fuse, Pal.Fuse);
                    Cord(m, new[] { V(-0.24f, 0.08f, -0.36f), V(-0.3f, 0.07f, -0.3f), V(-0.3f, 0.1f, -0.24f) });
                    break;
                case "Chain4":
                    // a lightning rod behind the terminals, bolts jumping from its tip to both bulbs
                    m.Tube(V(0, 0.395f, 0.16f), V(0, 0.92f, 0.16f), 0.02f, 0.014f, 6, Pal.Steel3, Pal.Steel2);
                    m.Ball(V(0, 0.93f, 0.16f), Vector3.one * 0.04f, 6, 4, Pal.Light0);
                    foreach (float sx in new[] { -1f, 1f })
                        m.Stem(new[] { V(0, 0.92f, 0.16f), V(sx * 0.09f, 0.86f, 0.11f), V(sx * 0.05f, 0.8f, 0.09f), V(sx * 0.15f, 0.72f, 0.05f), V(sx * 0.11f, 0.68f, 0.04f), V(sx * 0.17f, 0.62f, 0.02f) }, 0.02f, 0.012f, 4, Pal.Light2);
                    break;
                case "Swarm4":
                    // a rack of little bulbs along the box's front edge, one for each Photon more
                    m.Block(V(0, 0.395f, -0.17f), V(0.5f, 0.03f, 0.08f), 0.006f, Pal.Steel3, Pal.Steel2);
                    for (int i = 0; i < 4; i++) Bulb(m, V(-0.18f + i * 0.12f, 0.425f, -0.17f), 0.045f);
                    break;
                case "Supernova5":
                {
                    // a small sun held high over the box on a brass post, rays all round it
                    Vector3 sun = V(0, 0.9f, 0.02f);
                    m.Tube(V(0, 0.395f, 0.02f), sun, 0.024f, 0.018f, 6, Pal.Brass2, Pal.Brass1);
                    Ring(m, sun + V(0, -0.02f, 0), 0.15f, 0.014f, Pal.Brass1, 12);
                    m.Ball(sun, Vector3.one * 0.13f, 10, 6, Pal.Light1, Pal.Light0, 0.4f);
                    for (int i = 0; i < 10; i++)
                    {
                        float a = i / 10f * Mathf.PI * 2f, t = (i % 2 == 0 ? 0.35f : -0.25f);
                        m.Spike(sun + V(Mathf.Cos(a), t, Mathf.Sin(a)) * 0.12f, V(Mathf.Cos(a), t, Mathf.Sin(a)), 0.03f, 0.1f, Pal.Light2);
                    }
                }
                    break;
            }
            return m;
        }

        // ---------------------------------------------------------------- Mirror

        // A wall of glass: a tall brass-framed panel of four panes on a steel base, a screw ring at each post's foot, panes silver
        // with a pale diagonal where the light falls. The front faces -z.
        static Lp Mirror(string up)
        {
            var m = new Lp();
            Plinth(m, 0.46f, Pal.Steel4, Pal.Steel3);
            m.Block(V(0, 0.055f, 0), V(0.84f, 0.09f, 0.24f), 0.02f, Pal.Steel3, Pal.Steel2);
            float w = 0.72f, bottom = 0.14f, top = 0.98f, cx = w * 0.5f;
            // Everything above the base leans back a little, like a standing mirror, so the panes catch the light from above and
            // face the game's camera (T R T^-1 about the base's top; the coordinates below stay measured from the ground).
            m.Push(V(0, 0.145f, 0), Quaternion.Euler(11f, 0f, 0f), Vector3.one);
            m.Push(V(0, -0.145f, 0), 0f, 1f);
            // the posts
            foreach (float sx in new[] { -cx, cx })
            {
                ScrewRings(m, V(sx, 0.145f, 0), 0.05f, 0.14f, 3);
                m.Prism(V(sx, 0.285f, 0), 0.035f, 0.03f, top - 0.285f, 8, Pal.Brass2, Pal.Brass1);
                m.Ball(V(sx, top + 0.02f, 0), Vector3.one * 0.042f, 6, 4, Pal.Brass1, Pal.Brass0);
            }
            // the rails
            m.Tube(V(-cx, top, 0), V(cx, top, 0), 0.03f, 0.03f, 6, Pal.Brass1, Pal.Brass0, Pal.Brass0);
            m.Tube(V(-cx, bottom + 0.1f, 0), V(cx, bottom + 0.1f, 0), 0.028f, 0.028f, 6, Pal.Brass2, Pal.Brass1, Pal.Brass1);
            // the panes, a pair across, a pair down, with a mullion and a transom
            float py = bottom + 0.12f, ph = top - py - 0.02f, pw = w - 0.06f;
            int[] tone = { Pal.Silver1, Pal.Silver2, Pal.Silver0, Pal.Silver1 };
            for (int ix = 0; ix < 2; ix++)
                for (int iy = 0; iy < 2; iy++)
                {
                    float x = -pw * 0.25f + ix * pw * 0.5f, y = py + iy * ph * 0.5f;
                    m.Block(V(x, y, 0), V(pw * 0.5f - 0.012f, ph * 0.5f - 0.012f, 0.03f), 0.004f, tone[ix + iy * 2], tone[ix + iy * 2]);
                }
            m.Block(V(0, py, -0.02f), V(0.02f, ph, 0.012f), 0.002f, Pal.Brass2, Pal.Brass1);
            m.Block(V(0, py + ph * 0.5f - 0.01f, -0.02f), V(pw, 0.02f, 0.012f), 0.002f, Pal.Brass2, Pal.Brass1);
            // the light falling on it, upper left
            Vector3 a = V(-0.3f, top - 0.06f, -0.02f);
            m.Tri2(a, a + V(0.2f, 0, 0), a + V(0, -0.34f, 0), Pal.Silver4, Pal.Silver2);
            m.Tri2(a + V(0.2f, 0, 0), a + V(0.2f, -0.12f, 0), a + V(0, -0.34f, 0), Pal.Silver4, Pal.Silver2);
            switch (up)
            {
                case "Silver2":
                    // a thick silver frame round the panes, and a silvered backing showing behind them
                    m.Block(V(0, py - 0.02f, 0.03f), V(pw + 0.08f, ph + 0.04f, 0.03f), 0.01f, Pal.Silver0, Pal.Silver4);
                    m.Block(V(0, top - 0.035f, -0.03f), V(pw + 0.05f, 0.05f, 0.02f), 0.005f, Pal.Silver4, Pal.Silver4);
                    m.Block(V(0, py - 0.03f, -0.03f), V(pw + 0.05f, 0.05f, 0.02f), 0.005f, Pal.Silver4, Pal.Silver4);
                    foreach (float sx in new[] { -pw * 0.5f, pw * 0.5f })
                        m.Block(V(sx, py, -0.03f), V(0.05f, ph, 0.02f), 0.005f, Pal.Silver4, Pal.Silver4);
                    m.Tube(V(-cx - 0.02f, top + 0.05f, 0), V(cx + 0.02f, top + 0.05f, 0), 0.018f, 0.018f, 6, Pal.Silver4, Pal.Silver3, Pal.Silver3);
                    break;
                case "Vanity2":
                    // a row of bulbs round the frame, like a dressing-room mirror
                    for (int i = 0; i < 5; i++) Bulb(m, V(-cx + 0.06f + i * (w - 0.12f) / 4f, top + 0.015f, -0.02f), 0.038f);
                    foreach (float sx in new[] { -cx - 0.05f, cx + 0.05f })
                        for (int i = 0; i < 3; i++) m.Ball(V(sx, 0.4f + i * 0.2f, -0.02f), Vector3.one * 0.038f, 6, 4, Pal.Light1, Pal.Light0, 0.5f);
                    break;
                case "Glare3":
                    // a great star of glare on the pane
                    Vector3 s = V(0.14f, 0.72f, -0.05f);
                    for (int i = 0; i < 4; i++)
                    {
                        float ang = i * Mathf.PI * 0.5f + 0.2f, ang2 = ang + Mathf.PI * 0.25f;
                        m.Tri2(s + V(Mathf.Cos(ang) * 0.3f, Mathf.Sin(ang) * 0.3f, 0), s + V(Mathf.Cos(ang + 0.5f) * 0.07f, Mathf.Sin(ang + 0.5f) * 0.07f, 0), s + V(Mathf.Cos(ang - 0.5f) * 0.07f, Mathf.Sin(ang - 0.5f) * 0.07f, 0), Pal.Light0, Pal.Silver2);
                        m.Tri2(s + V(Mathf.Cos(ang2) * 0.14f, Mathf.Sin(ang2) * 0.14f, 0), s + V(Mathf.Cos(ang2 + 0.6f) * 0.05f, Mathf.Sin(ang2 + 0.6f) * 0.05f, 0), s + V(Mathf.Cos(ang2 - 0.6f) * 0.05f, Mathf.Sin(ang2 - 0.6f) * 0.05f, 0), Pal.Light0, Pal.Silver2);
                    }
                    m.Ball(s + V(0, 0, -0.01f), Vector3.one * 0.05f, 6, 4, Pal.Light0);
                    break;
                case "Bounce3":
                    // a white bounce card on an arm, tipped up over the panel to throw the light down and out
                    m.Tube(V(cx, top - 0.05f, 0.02f), V(cx - 0.02f, top + 0.1f, 0.02f), 0.014f, 0.014f, 5, Pal.Steel3);
                    m.Push(V(0.12f, top + 0.08f, 0.02f), Quaternion.Euler(52f, 0f, 0f), Vector3.one);
                    m.Block(V(0, 0, 0), V(0.56f, 0.3f, 0.02f), 0.006f, Pal.Card0, Pal.Card0, Pal.Card1);
                    m.Pop();
                    break;
                case "Tempered3":
                    // steel corner plates riveted over the frame, and a steel brace across the foot
                    foreach (float sx in new[] { -1f, 1f })
                        foreach (float yy in new[] { py, top - 0.12f })
                        {
                            m.Block(V(sx * (pw * 0.5f - 0.04f), yy, -0.035f), V(0.12f, 0.12f, 0.016f), 0.006f, Pal.Steel2, Pal.Steel1);
                            m.Ball(V(sx * (pw * 0.5f - 0.04f), yy + 0.06f, -0.045f), Vector3.one * 0.017f, 5, 3, Pal.Steel0);
                        }
                    m.Block(V(0, bottom + 0.06f, -0.03f), V(w + 0.04f, 0.06f, 0.03f), 0.008f, Pal.Steel3, Pal.Steel2);
                    break;
                case "Hall4":
                    // the panes show frames inside frames, a hall of mirrors running back into the glass
                    for (int i = 0; i < 3; i++)
                    {
                        float k = 0.8f - i * 0.22f, fw = pw * k, fh = ph * k, fy = py + (ph - fh) * 0.5f, z = -0.03f - i * 0.004f;
                        int col = i % 2 == 0 ? Pal.Brass1 : Pal.Silver4;
                        m.Block(V(0, fy, z), V(fw, 0.022f, 0.008f), 0.002f, col, col);
                        m.Block(V(0, fy + fh - 0.022f, z), V(fw, 0.022f, 0.008f), 0.002f, col, col);
                        foreach (float sx in new[] { -1f, 1f })
                            m.Block(V(sx * (fw * 0.5f - 0.011f), fy, z), V(0.022f, fh, 0.008f), 0.002f, col, col);
                    }
                    m.Block(V(0, py + ph * 0.5f - 0.05f, -0.036f), V(0.1f, 0.1f, 0.006f), 0.002f, Pal.Silver5, Pal.Silver5);
                    break;
                case "Reflection4":
                {
                    // a round convex mirror set in the middle of the panes, in its own brass hoop
                    Vector3 rc = V(0, py + ph * 0.5f, -0.035f);
                    Hoop(m, rc, 0.17f, 0.02f, Pal.Brass1);
                    m.Push(rc, Quaternion.Euler(-90f, 0f, 0f), Vector3.one);
                    m.Dome(V(0, 0, 0), V(0.16f, 0.06f, 0.16f), 12, 3, Pal.Silver1, Pal.Silver4, 0.5f);
                    m.Pop();
                    m.Tri2(rc + V(-0.09f, 0.08f, -0.05f), rc + V(-0.03f, 0.1f, -0.05f), rc + V(-0.1f, 0.02f, -0.05f), Pal.White, Pal.Silver2);
                    break;
                }
                case "Heliostat5":
                {
                    // a round mirror on a fork over the top rail, tipped to send its light down on the foes
                    m.Tube(V(0, top, 0), V(0, top + 0.09f, 0), 0.022f, 0.02f, 6, Pal.Steel2, Pal.Steel1);
                    m.Tube(V(-0.2f, top + 0.09f, 0), V(0.2f, top + 0.09f, 0), 0.016f, 0.016f, 5, Pal.Steel2, Pal.Steel1, Pal.Steel1);
                    foreach (float sx in new[] { -0.2f, 0.2f }) m.Tube(V(sx, top + 0.09f, 0), V(sx, top + 0.2f, 0), 0.014f, 0.014f, 5, Pal.Steel2, Pal.Steel1);
                    Vector3 hc = V(0, top + 0.2f, 0);
                    m.Push(hc, Quaternion.Euler(-60f, 0f, 0f), Vector3.one);
                    m.Prism(V(0, -0.015f, 0), 0.18f, 0.18f, 0.03f, 14, Pal.Brass2, Pal.Silver4, Pal.Brass3);
                    m.Prism(V(0, 0.015f, 0), 0.06f, 0.05f, 0.006f, 10, Pal.Light0, Pal.Light0);
                    m.Pop();
                    break;
                }
            }
            m.Pop();
            m.Pop();
            // the cord
            Cord(m, new[] { V(0.42f, 0.14f, 0.1f), V(0.47f, 0.1f, 0.18f), V(0.42f, 0.05f, 0.3f), V(0.3f, 0.04f, 0.36f) });
            Plug(m, V(0.24f, 0.015f, 0.36f), 260f, 0.9f);
            return m;
        }

        // ---------------------------------------------------------------- Flood Light

        // A stadium lamp on a steel tripod, a screw collar under its yoke, a thick cord to the plug. The lamp is the moving part.
        static Lp FloodStand(string up)
        {
            var m = new Lp();
            Plinth(m, 0.3f);
            foreach (float a in new[] { 90f, 210f, 330f })
            {
                float ar = a * Mathf.Deg2Rad;
                Vector3 foot = V(Mathf.Cos(ar) * 0.32f, 0.03f, Mathf.Sin(ar) * 0.32f);
                m.Tube(foot, V(0, 0.48f, 0), 0.022f, 0.02f, 6, Pal.Steel3, -1, Pal.Steel4);
                m.Ball(foot, V(0.035f, 0.022f, 0.035f), 6, 3, Pal.Cord0);
            }
            ScrewRings(m, V(0, 0.44f, 0), 0.065f, 0.12f, 3);
            // the cord
            Cord(m, new[] { V(0.05f, 0.46f, 0.06f), V(0.14f, 0.26f, 0.2f), V(0.26f, 0.06f, 0.3f), V(0.36f, 0.05f, 0.2f) }, 0.02f);
            Plug(m, V(0.39f, 0.015f, 0.14f), 200f, 1f);
            switch (up)
            {
                case "Wide2":
                    // a wider stand: a fourth and fifth brace
                    foreach (float a in new[] { 150f, 30f })
                    {
                        float ar = a * Mathf.Deg2Rad;
                        m.Tube(V(Mathf.Cos(ar) * 0.34f, 0.03f, Mathf.Sin(ar) * 0.34f), V(0, 0.42f, 0), 0.016f, 0.014f, 5, Pal.Steel4);
                    }
                    break;
                case "Voltage3":
                    // a transformer box at the foot, its yellow bolt sign and a red light, the cord coiled thick beside it
                    m.Block(V(-0.2f, 0.055f, -0.13f), V(0.17f, 0.15f, 0.13f), 0.012f, Pal.Steel2, Pal.Steel1);
                    m.Tri2(V(-0.19f, 0.19f, -0.197f), V(-0.23f, 0.12f, -0.197f), V(-0.2f, 0.125f, -0.197f), Pal.Light2, Pal.Steel2);
                    m.Tri2(V(-0.2f, 0.135f, -0.197f), V(-0.17f, 0.14f, -0.197f), V(-0.21f, 0.07f, -0.197f), Pal.Light2, Pal.Steel2);
                    m.Ball(V(-0.16f, 0.215f, -0.1f), Vector3.one * 0.022f, 6, 4, Pal.Fuse, Pal.Light1);
                    Ring(m, V(0.2f, 0.06f, -0.18f), 0.07f, 0.02f, Pal.Cord1, 10);
                    Ring(m, V(0.2f, 0.09f, -0.18f), 0.06f, 0.02f, Pal.Cord0, 10);
                    break;
                case "Filter4":
                    // spare colour gels in their frames, leaning on a leg: red, blue, yellow
                    int[] gel = { Pal.Fuse, Pal.Glass4, Pal.Light2 };
                    for (int i = 0; i < 3; i++)
                    {
                        m.Push(V(-0.2f + i * 0.035f, 0.055f, -0.2f + i * 0.02f), Quaternion.Euler(-20f + i * 4f, 20f, 0f), Vector3.one);
                        m.Block(V(0, 0, 0), V(0.14f, 0.14f, 0.012f), 0.004f, Pal.Card0, Pal.Card0);
                        m.Block(V(0, 0.02f, -0.008f), V(0.11f, 0.1f, 0.006f), 0.002f, gel[i], gel[i]);
                        m.Pop();
                    }
                    break;
                case "Lighthouse5":
                    // a red and white banded tower inside the tripod
                    for (int i = 0; i < 4; i++)
                    {
                        float y0 = 0.055f + i * 0.095f, r0 = 0.1f - i * 0.009f;
                        m.Prism(V(0, y0, 0), r0, r0 - 0.009f, 0.095f, 10, i % 2 == 0 ? Pal.Fuse : Pal.White, i % 2 == 0 ? Pal.Fuse : Pal.White);
                    }
                    break;
            }
            return m;
        }

        // The lamp, in tower space around its pivot: a steel housing whose glass face looks along -z, with a burning element behind
        // the glass and cooling fins on top.
        static Lp Lamp(string up)
        {
            var m = new Lp();
            float y = LampY + 0.1f;
            float w = up == "Wide2" ? 0.62f : up == "Stadium5" ? 0.6f : 0.42f, h = up == "Wide2" ? 0.26f : up == "Stadium5" ? 0.36f : 0.3f;
            // the yoke
            m.Tube(V(-w * 0.5f - 0.03f, y, 0), V(w * 0.5f + 0.03f, y, 0), 0.014f, 0.014f, 5, Pal.Steel3);
            m.Tube(V(0, LampY, 0), V(0, y, 0), 0.03f, 0.03f, 6, Pal.Steel3, Pal.Steel2);
            // Three capstones and the Searchlight change the lamp itself.
            switch (up)
            {
                case "Searchlight4":
                {
                    // a round searchlight drum, a brass bezel, a deep bright mirror behind the glass
                    Vector3 sc = V(0, y - 0.14f, 0.06f);
                    m.Tube(sc, sc + V(0, 0, -0.26f), 0.16f, 0.18f, 14, Pal.Steel3, Pal.Steel2, Pal.Steel2);
                    m.Tube(sc + V(0, 0, -0.26f), sc + V(0, 0, -0.29f), 0.195f, 0.195f, 14, Pal.Brass1, Pal.Light0, Pal.Brass2);
                    m.Ball(sc + V(0, 0, -0.27f), V(0.06f, 0.06f, 0.03f), 8, 4, Pal.White);
                    m.Tri2(sc + V(-0.13f, 0.1f, -0.296f), sc + V(-0.05f, 0.14f, -0.296f), sc + V(-0.14f, 0.02f, -0.296f), Pal.Silver4, Pal.Light1);
                    for (int i = 0; i < 4; i++) Ring(m, sc + V(0, 0, -0.04f - i * 0.05f), 0.17f + i * 0.004f, 0.008f, Pal.Steel4, 14);
                    return m;
                }
                case "Lighthouse5":
                {
                    // a lantern room: a steel gallery, glass all round a bright core, a red dome on top
                    Vector3 lc = V(0, y - 0.22f, 0);
                    Ring(m, lc + V(0, 0.01f, 0), 0.2f, 0.014f, Pal.Steel2, 12);
                    m.Prism(lc, 0.2f, 0.2f, 0.02f, 12, Pal.Steel3, Pal.Steel2);
                    m.Prism(lc + V(0, 0.02f, 0), 0.15f, 0.15f, 0.24f, 8, Pal.Glass1, Pal.Glass0, -1, 22.5f);
                    for (int i = 0; i < 8; i++)
                    {
                        float a = (i + 0.5f) / 8f * Mathf.PI * 2f;
                        m.Tube(lc + V(Mathf.Cos(a) * 0.152f, 0.02f, Mathf.Sin(a) * 0.152f), lc + V(Mathf.Cos(a) * 0.152f, 0.26f, Mathf.Sin(a) * 0.152f), 0.01f, 0.01f, 4, Pal.Steel3);
                    }
                    m.Ball(lc + V(0, 0.14f, 0), Vector3.one * 0.075f, 8, 5, Pal.Light1, Pal.Light0);
                    m.Tube(lc + V(0, 0.14f, -0.06f), lc + V(0, 0.14f, -0.155f), 0.05f, 0.06f, 8, Pal.Light0, Pal.White);
                    m.Dome(lc + V(0, 0.26f, 0), V(0.17f, 0.12f, 0.17f), 10, 3, Pal.Fuse, Pal.Fuse);
                    m.Ball(lc + V(0, 0.4f, 0), Vector3.one * 0.025f, 6, 3, Pal.Brass1);
                    return m;
                }
                case "Stadium5":
                {
                    // a bank of six lamps in one steel frame
                    m.Block(V(0, y - h, -0.04f), V(w, h, 0.18f), 0.02f, Pal.Steel3, Pal.Steel2, Pal.Steel1);
                    for (int ix = 0; ix < 3; ix++)
                        for (int iy = 0; iy < 2; iy++)
                        {
                            Vector3 fc = V(-w * 0.33f + ix * w * 0.33f, y - h + 0.04f + iy * h * 0.48f, -0.13f);
                            m.Block(fc, V(w * 0.29f, h * 0.42f, 0.03f), 0.008f, Pal.Brass2, Pal.Brass1);
                            m.Block(fc + V(0, 0.015f, -0.02f), V(w * 0.22f, h * 0.32f, 0.012f), 0.003f, Pal.Light1, Pal.Light0);
                        }
                    for (int i = 0; i < 6; i++)
                        m.Block(V(-w * 0.42f + i * w * 0.168f, y - 0.01f, -0.04f), V(0.02f, 0.04f, 0.15f), 0.004f, Pal.Steel2, Pal.Steel1);
                    return m;
                }
            }
            // the housing
            m.Block(V(0, y - h * 0.5f, -0.04f), V(w, h, 0.2f), 0.024f, Pal.Steel3, Pal.Steel2, Pal.Steel1);
            // the bezel and the glass
            m.Block(V(0, y - h * 0.5f + 0.01f, -0.15f), V(w - 0.02f, h - 0.02f, 0.03f), 0.012f, Pal.Brass2, Pal.Brass1);
            m.Block(V(0, y - h * 0.5f + 0.03f, -0.165f), V(w - 0.07f, h - 0.06f, 0.015f), 0.004f, Pal.Light1, Pal.Light0);
            // the element across the glass, and the glint
            m.Block(V(0, y - 0.01f, -0.177f), V(w - 0.14f, 0.024f, 0.01f), 0.003f, Pal.Light0, Pal.Light0);
            m.Tri2(V(-w * 0.5f + 0.06f, y + h * 0.5f - 0.05f, -0.178f), V(-w * 0.5f + 0.15f, y + h * 0.5f - 0.05f, -0.178f), V(-w * 0.5f + 0.06f, y - 0.03f, -0.178f), Pal.Silver4, Pal.Light1);
            // the fins
            for (int i = 0; i < 5; i++)
                m.Block(V(-w * 0.35f + i * w * 0.175f, y + h * 0.5f - 0.01f, -0.04f), V(0.022f, 0.04f, 0.16f), 0.004f, Pal.Steel2, Pal.Steel1);
            switch (up)
            {
                case "Wide2":
                    // short barn doors thrown wide on either side, bright inside
                    foreach (float sx in new[] { -1f, 1f })
                    {
                        Vector3 p0 = V(sx * w * 0.5f, y - h + 0.01f, -0.16f), p1 = V(sx * w * 0.5f, y + 0.0f, -0.16f), p2 = V(sx * (w * 0.5f + 0.07f), y + 0.02f, -0.22f), p3 = V(sx * (w * 0.5f + 0.07f), y - h - 0.01f, -0.22f);
                        m.Tri2(p0, p1, p2, Pal.Light1, Pal.Steel3);
                        m.Tri2(p0, p2, p3, Pal.Light1, Pal.Steel3);
                    }
                    break;
                case "Long2":
                    // a long snoot out in front, a second lens at its mouth
                    Vector3 fc = V(0, y - h * 0.5f + 0.01f, -0.18f);
                    m.Tube(fc, fc + V(0, 0, -0.26f), h * 0.42f, h * 0.3f, 10, Pal.Steel3, -1);
                    m.Tube(fc + V(0, 0, -0.26f), fc + V(0, 0, -0.29f), h * 0.33f, h * 0.33f, 10, Pal.Brass1, Pal.Light0, Pal.Brass2);
                    break;
                case "Halogen3":
                    // a wire guard over the hotter glass, and a glowing element twice as thick
                    m.Block(V(0, y - 0.025f, -0.18f), V(w - 0.14f, 0.05f, 0.01f), 0.003f, Pal.Light0, Pal.White);
                    for (int i = 0; i < 3; i++)
                        m.Tube(V(-w * 0.5f + 0.03f, y - h + 0.06f + i * (h - 0.08f) * 0.5f, -0.19f), V(w * 0.5f - 0.03f, y - h + 0.06f + i * (h - 0.08f) * 0.5f, -0.19f), 0.007f, 0.007f, 4, Pal.Steel1);
                    foreach (float sx in new[] { -0.08f, 0.08f })
                        m.Tube(V(sx, y - h + 0.03f, -0.19f), V(sx, y - 0.01f, -0.19f), 0.007f, 0.007f, 4, Pal.Steel1);
                    break;
                case "Voltage3":
                    // a yellow warning bolt on the housing's side
                    m.Tri2(V(w * 0.5f + 0.002f, y - 0.03f, -0.04f), V(w * 0.5f + 0.002f, y - 0.14f, 0.02f), V(w * 0.5f + 0.002f, y - 0.13f, -0.02f), Pal.Light2, Pal.Steel2);
                    m.Tri2(V(w * 0.5f + 0.002f, y - 0.12f, -0.04f), V(w * 0.5f + 0.002f, y - 0.13f, 0.0f), V(w * 0.5f + 0.002f, y - 0.25f, -0.06f), Pal.Light2, Pal.Steel2);
                    break;
                case "Filter4":
                {
                    // a gel frame clipped over the glass, in the three aura colours
                    int[] gel = { Pal.Fuse, Pal.Glass4, Pal.Light2 };
                    m.Block(V(0, y - h - 0.005f, -0.19f), V(w + 0.02f, h + 0.01f, 0.014f), 0.004f, Pal.Card1, Pal.Card0);
                    for (int i = 0; i < 3; i++)
                        m.Block(V(-w / 3f + i * w / 3f, y - h + 0.025f, -0.2f), V(w / 3f - 0.025f, h - 0.04f, 0.008f), 0.002f, gel[i], gel[i]);
                    break;
                }
            }
            return m;
        }

        // ---------------------------------------------------------------- Light's look (the skins' extra prefab)

        // Round the tower's foot: a ring of light, then short rays, then more and longer. Bright palette colours, drawn on the ground
        // where the camera sees them past any tower.
        static Lp LitLook(int band)
        {
            var m = new Lp();
            float r = 0.44f;
            Ring(m, V(0, 0.012f, 0), r, 0.014f + 0.004f * band, band >= 3 ? Pal.Light0 : Pal.Light1, 16);
            if (band >= 2)
            {
                int rays = band == 2 ? 8 : 12;
                float len = band == 2 ? 0.1f : 0.17f;
                for (int i = 0; i < rays; i++)
                {
                    float a = i / (float)rays * Mathf.PI * 2f + 0.2f;
                    Vector3 d = V(Mathf.Cos(a), 0, Mathf.Sin(a));
                    Vector3 side = V(-d.z, 0, d.x) * 0.018f;
                    Vector3 from = d * (r + 0.015f) + V(0, 0.01f, 0);
                    m.Tri2(from + side, from - side, from + d * len + V(0, 0.015f * band, 0), Pal.Light0, Pal.Light1);
                }
            }
            return m;
        }

        // ---------------------------------------------------------------- the plug, the spark and the beam's tip

        // The plug a cord ends in, sitting at its node or Power Bank (the client moves it; its prongs look along +z).
        static Lp PlugModel()
        {
            var m = new Lp();
            m.Push(V(0, 0.5f, 0), 0f, 1.6f);
            m.Block(V(0, 0, -0.05f), V(0.1f, 0.05f, 0.1f), 0.012f, Pal.Cord0, Pal.Cord3);
            m.Tube(V(-0.022f, 0.025f, 0f), V(-0.022f, 0.025f, 0.05f), 0.008f, 0.008f, 4, Pal.Steel1);
            m.Tube(V(0.022f, 0.025f, 0f), V(0.022f, 0.025f, 0.05f), 0.008f, 0.008f, 4, Pal.Steel1);
            m.Pop();
            return m;
        }

        // Uloe's shot (along z, point forward): a little yellow bolt with a white heart.
        static Lp Spark()
        {
            var m = new Lp();
            m.Tube(V(0, 0.5f, -0.16f), V(0, 0.5f, 0.02f), 0f, 0.055f, 6, Pal.Light2);
            m.Tube(V(0, 0.5f, 0.02f), V(0, 0.5f, 0.17f), 0.055f, 0f, 6, Pal.Light0);
            m.Ball(V(0, 0.5f, 0.03f), Vector3.one * 0.05f, 6, 4, Pal.Light0);
            return m;
        }

        // A glow where the Glass's beam lands.
        static Lp Beamtip()
        {
            var m = new Lp();
            m.Ball(V(0, 0.5f, 0), Vector3.one * 0.07f, 6, 4, Pal.Light0);
            return m;
        }
    }
}
