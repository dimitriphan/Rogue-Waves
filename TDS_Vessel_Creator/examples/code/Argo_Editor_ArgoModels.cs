// Argo asset generator: every tower model (buildables, upgrades, Fathom overlays, projectiles), built low-poly and sleek
// like Sno Sno's and Buster's towers: a handful of clean solids per model, flat palette colours, readable silhouettes,
// no voxels. Every Argo building stands on the same riveted stone-and-brass dock. Upgrades keep the silhouette and add
// one clear feature. The Scatter Cannon and the Harpoon Tower keep their gun on a separate "Turret" part (the clients
// swing it toward each shot); the Deep Drill's chuck is a "Spinner" part that turns while it bites.
// Units: 1 = one tile; y measured up from the ground; the front faces -z; a turret's barrel points down -z.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ArgoBuild
{
    public class ModelDef
    {
        public string Name;
        public string Group;            // Scatter, Harpoon, Bell, Drill, Extras, Projectiles
        public bool Projectile;
        public Func<Lp> Build;
        public Func<Lp> Part;           // optional moving part, built in tower space
        public string PartName;         // "Turret" or "Spinner"
        public Vector3 PartPivot;       // tower space, y up from the ground
        public float Scale = 1f;        // baked into the mesh about the ground point (matched to the shipped towers' sizes)
    }

    public static class ArgoModels
    {
        static Vector3 V(float x, float y, float z) { return new Vector3(x, y, z); }
        static Vector3 Dir(float deg, float y = 0f) { return new Vector3(Mathf.Cos(deg * Mathf.Deg2Rad), y, Mathf.Sin(deg * Mathf.Deg2Rad)); }

        static readonly string[] ScatterKeys = { "", "_Widebore2", "_Choke2", "_Grapeshot3", "_Barnacle3", "_Pressure4", "_Undertow4", "_Broadside5" };
        static readonly string[] HarpoonKeys = { "", "_Longline2", "_Barbed2", "_Skewer3", "_Anchor3", "_Whaler4", "_Twinline4", "_Leviathan5" };
        static readonly string[] BellKeys = { "", "_Flippers2", "_Hardhats2", "_Overhaul3", "_Calibrate3", "_Thirdmate4", "_Bulkhead4", "_Fullcrew5" };
        static readonly string[] DrillKeys = { "", "_Rotary2", "_Seismic2", "_Deepcore3", "_Aftershock3", "_Gusher4", "_Faultline4", "_Mantle5" };

        public static readonly Vector3 ScatterPivot = new Vector3(0f, 0.25f, 0f);
        public static readonly Vector3 HarpoonPivot = new Vector3(0f, 0.6f, 0f);

        // Heights measured in game (2026-10-02): Sno Sno's towers 0.75-1.15, Buster's Garage 0.85 and Pedestal 1.9, Smacky's
        // Archer 2.05 and Mining Hut 1.0. Argo's land at about 0.9 (cannon), 1.2 (harpoon), 0.85 (bell) and 1.25 (derrick).
        public const float ScatterScale = 1.55f, HarpoonScale = 1.45f, BellScale = 1.5f, DrillScale = 1.3f;
        static float DockFor(float scale) { return 0.4f / scale; }
        public static readonly Vector3 DrillPivot = new Vector3(0f, 0.14f, 0f);

        public static List<ModelDef> All()
        {
            var l = new List<ModelDef>();
            for (int i = 0; i < 8; i++)
            {
                int v = i;
                l.Add(new ModelDef { Group = "Scatter", Name = "Tower_Argoscatter" + ScatterKeys[i], Build = () => ScatterBase(v), Part = () => ScatterTurret(v), PartName = "Turret", PartPivot = ScatterPivot, Scale = ScatterScale });
            }
            for (int i = 0; i < 8; i++)
            {
                int v = i;
                l.Add(new ModelDef { Group = "Harpoon", Name = "Tower_Argoharpoon" + HarpoonKeys[i], Build = () => HarpoonBase(v), Part = () => HarpoonTurret(v), PartName = "Turret", PartPivot = HarpoonPivot, Scale = HarpoonScale });
            }
            for (int i = 0; i < 8; i++)
            {
                int v = i;
                l.Add(new ModelDef { Group = "Bell", Name = "Tower_Argobell" + BellKeys[i], Build = () => Bell(v), Scale = BellScale });
            }
            for (int i = 0; i < 8; i++)
            {
                int v = i;
                l.Add(new ModelDef { Group = "Drill", Name = "Tower_Argodrill" + DrillKeys[i], Build = () => DrillBase(v), Part = () => DrillBit(v), PartName = "Spinner", PartPivot = DrillPivot, Scale = DrillScale });
            }
            for (int f = 2; f <= 5; f++)
            {
                int n = f;
                l.Add(new ModelDef { Group = "Extras", Name = "Argo_Fathom" + f, Build = () => Fathom(n), Scale = DrillScale });
            }
            l.Add(new ModelDef { Group = "Projectiles", Name = "Argo_Pellet", Build = Pellet, Projectile = true });
            l.Add(new ModelDef { Group = "Projectiles", Name = "Argo_Harpoon", Build = () => HarpoonShot(false), Projectile = true });
            l.Add(new ModelDef { Group = "Projectiles", Name = "Argo_Lance", Build = () => HarpoonShot(true), Projectile = true });
            return l;
        }

        // ---------------------------------------------------------------- shared pieces

        // The dock every Argo building stands on: an octagonal stone slab with a brass trim band and four rivets.
        static void Dock(Lp m, float r = 0.37f)
        {
            m.Prism(V(0, 0, 0), r, r * 0.97f, 0.065f, 8, Pal.StoneMid, Pal.Stone, -1, 22.5f);
            m.Tube(V(0, 0.05f, 0), V(0, 0.082f, 0), r * 0.985f, r * 0.985f, 8, Pal.BrassMid, Pal.Brass, -1, 22.5f);
            m.Prism(V(0, 0.082f, 0), r * 0.84f, r * 0.82f, 0.012f, 8, Pal.StoneLight, Pal.StoneLight, -1, 22.5f);
            for (int i = 0; i < 4; i++)
                m.Ball(Dir(45f + i * 90f) * r * 0.9f + V(0, 0.084f, 0), V(0.018f, 0.012f, 0.018f), 6, 3, Pal.BrassLight);
        }

        const float Top = 0.094f;   // the dock's top surface

        static void Barnacles(Lp m, Vector3 c, float spread, int n, int seed)
        {
            var rng = new System.Random(seed);
            for (int i = 0; i < n; i++)
            {
                float a = (float)rng.NextDouble() * 360f, d = (float)rng.NextDouble() * spread;
                Vector3 p = c + Dir(a) * d;
                float s = 0.018f + (float)rng.NextDouble() * 0.014f;
                m.Prism(p, s, s * 0.5f, s * 1.2f, 6, i % 2 == 0 ? Pal.StoneLight : Pal.Teal, Pal.TealDark);
            }
        }

        // ---------------------------------------------------------------- Scatter Cannon

        static Lp ScatterBase(int v)
        {
            var m = new Lp();
            Dock(m, DockFor(ScatterScale));
            float r = v == 7 ? 0.2f : 0.17f;
            m.Prism(V(0, Top, 0), r, r * 0.86f, 0.14f, 8, Pal.SteelMid, Pal.Steel, -1, 22.5f);
            m.Tube(V(0, Top + 0.12f, 0), V(0, Top + 0.15f, 0), r * 0.9f, r * 0.9f, 8, Pal.BrassMid, Pal.Brass, -1, 22.5f);
            if (v == 4) Barnacles(m, V(0, Top, 0), 0.21f, 9, 4);
            if (v == 5)
                for (int s = -1; s <= 1; s += 2)
                    m.Stem(new[] { V(0.21f * s, Top, 0.1f), V(0.21f * s, Top + 0.08f, 0.1f), V(0.15f * s, Top + 0.12f, 0.07f) }, 0.02f, 0.02f, 6, Pal.Copper);
            if (v == 7)
                for (int i = 0; i < 8; i++)
                    m.Spike(Dir(i * 45f + 22.5f) * 0.23f + V(0, Top, 0), V(0, 1, 0), 0.02f, 0.06f, Pal.BrassLight, 4);
            return m;
        }

        static void Flare(Lp m, float z0, float z1, float y0, float y1, float r0, float r1, int col = Pal.Brass)
        {
            m.Tube(V(0, y0, z0), V(0, y1, z1), r0, r1, 12, col, Pal.Ink);
            m.Hoop(V(0, y1, z1), V(0, 0, 1), r1, 0.008f, 0.02f, 12, Pal.BrassDark);
        }

        static void ScatterBarrel(Lp m, float x, float r, float len, float flare, float flareLen, int col = Pal.SteelDark)
        {
            m.Push(V(x, 0, 0));
            m.Tube(V(0, 0.37f, 0.12f), V(0, 0.38f, 0.12f - len), r, r * 0.92f, 10, col);
            Flare(m, 0.12f - len, 0.12f - len - flareLen, 0.38f, 0.395f, r * 0.92f, flare);
            m.Hoop(V(0, 0.373f, 0.04f), V(0, 0.02f, 1), r, 0.01f, 0.025f, 10, Pal.BrassMid);
            m.Pop();
        }

        static Lp ScatterTurret(int v)
        {
            var m = new Lp();
            float ring = v == 7 ? 0.19f : 0.15f;
            m.Prism(V(0, 0.25f, 0), ring, ring * 0.94f, 0.05f, 10, Pal.Brass, Pal.BrassLight);
            for (int s = -1; s <= 1; s += 2)
                m.Block(V(0.11f * s, 0.29f, 0.03f), V(0.045f, 0.14f, 0.17f), 0.015f, Pal.BrassMid, Pal.Brass);
            m.Ball(V(0, 0.37f, 0.14f), V(0.095f, 0.085f, 0.095f), 10, 5, Pal.SteelMid, Pal.SteelLight, 0.5f);
            switch (v)
            {
                case 1: ScatterBarrel(m, 0, 0.08f, 0.3f, 0.2f, 0.17f); break;                         // Wide Bore
                case 2:                                                                                // Choke
                    ScatterBarrel(m, 0, 0.058f, 0.42f, 0.085f, 0.06f);
                    m.Hoop(V(0, 0.388f, -0.31f), V(0, 0.02f, 1), 0.06f, 0.016f, 0.03f, 10, Pal.Brass);
                    break;
                case 3:                                                                                // Grapeshot
                    foreach (var o in new[] { V(-0.05f, 0, 0), V(0.05f, 0, 0), V(0, 0.055f, 0) })
                    {
                        m.Push(o);
                        ScatterBarrel(m, 0, 0.038f, 0.34f, 0.06f, 0.08f, Pal.SteelMid);
                        m.Pop();
                    }
                    m.Tube(V(-0.1f, 0.5f, 0.07f), V(0.1f, 0.5f, 0.07f), 0.065f, 0.065f, 10, Pal.SteelMid, Pal.Brass, Pal.Brass);
                    break;
                case 4:                                                                                // Barnacle Shot
                    ScatterBarrel(m, 0, 0.075f, 0.3f, 0.14f, 0.14f);
                    Barnacles(m, V(0, 0.44f, -0.05f), 0.08f, 6, 9);
                    m.Blade(V(0.06f, 0.43f, -0.12f), V(0.14f, 0.55f, -0.1f), V(0, 0, 1), 0.035f, Pal.Kelp, Pal.Kelp);
                    break;
                case 5:                                                                                // Pressure Rounds
                    ScatterBarrel(m, 0, 0.075f, 0.3f, 0.14f, 0.14f);
                    for (int s = -1; s <= 1; s += 2)
                    {
                        m.Tube(V(0.14f * s, 0.38f, 0.16f), V(0.14f * s, 0.38f, -0.06f), 0.045f, 0.045f, 8, Pal.Copper);
                        m.Ball(V(0.14f * s, 0.38f, -0.06f), Vector3.one * 0.045f, 8, 4, Pal.Copper, Pal.CopperDark);
                        m.Ball(V(0.14f * s, 0.38f, 0.16f), Vector3.one * 0.045f, 8, 4, Pal.Copper, Pal.CopperDark);
                        m.Prism(V(0.14f * s, 0.425f, 0.06f), 0.025f, 0.025f, 0.012f, 8, Pal.Brass, Pal.White);
                    }
                    break;
                case 6:                                                                                // Undertow
                    ScatterBarrel(m, 0, 0.075f, 0.3f, 0.14f, 0.14f);
                    m.Ball(V(0, 0.37f, 0.25f), V(0.035f, 0.035f, 0.035f), 6, 4, Pal.Brass);
                    for (int i = 0; i < 3; i++)
                    {
                        float a = i * 120f + 15f;
                        Vector3 tip = V(0, 0.37f, 0.255f) + new Vector3(Mathf.Cos(a * Mathf.Deg2Rad), Mathf.Sin(a * Mathf.Deg2Rad), 0) * 0.14f;
                        m.Blade(V(0, 0.37f, 0.255f), tip, V(0, 0, 1), 0.06f, Pal.Brass, Pal.BrassDark);
                    }
                    m.Hoop(V(0, 0.37f, 0.23f), V(0, 0, 1), 0.15f, 0.01f, 0.025f, 12, Pal.SteelDark);
                    break;
                case 7:                                                                                // Broadside
                    ScatterBarrel(m, 0, 0.085f, 0.32f, 0.2f, 0.16f);
                    for (int i = 0; i < 6; i++)
                    {
                        float a = 30f + i * 60f;
                        Vector3 d = Dir(a);
                        m.Tube(V(0, 0.3f, 0) + d * 0.18f, V(0, 0.31f, 0) + d * 0.3f, 0.035f, 0.035f, 8, Pal.SteelDark);
                        m.Tube(V(0, 0.31f, 0) + d * 0.3f, V(0, 0.315f, 0) + d * 0.35f, 0.035f, 0.055f, 8, Pal.Brass, Pal.Ink);
                    }
                    for (int i = 0; i < 6; i++)
                        m.Spike(Dir(i * 60f) * 0.19f + V(0, 0.3f, 0), V(0, 1, 0), 0.02f, 0.06f, Pal.Gold, 4);
                    break;
                default: ScatterBarrel(m, 0, 0.075f, 0.3f, 0.14f, 0.14f); break;
            }
            // the fuse vent on the breech
            m.Tube(V(0, 0.45f, 0.15f), V(0, 0.49f, 0.15f), 0.016f, 0.012f, 6, Pal.SteelDeep, Pal.Orange);
            return m;
        }

        // ---------------------------------------------------------------- Harpoon Tower

        static Lp HarpoonBase(int v)
        {
            var m = new Lp();
            Dock(m, DockFor(HarpoonScale));
            float leg = v == 5 || v == 7 ? 0.032f : 0.024f;
            for (int i = 0; i < 3; i++)
            {
                Vector3 foot = Dir(90f + i * 120f) * 0.235f + V(0, Top, 0);
                m.Stem(new[] { foot, V(0, 0.55f, 0) + Dir(90f + i * 120f) * 0.04f }, leg, leg * 0.8f, 6, Pal.BrassMid);
                m.Prism(foot, leg * 1.8f, leg * 1.5f, 0.02f, 6, Pal.BrassDark, Pal.Brass);
            }
            m.Tube(V(0, Top, 0), V(0, 0.56f, 0), 0.04f, 0.035f, 8, Pal.SteelDark);
            m.Prism(V(0, 0.53f, 0), 0.075f, 0.065f, 0.07f, 8, Pal.Brass, Pal.BrassLight);
            if (v == 7)
                for (int k = 0; k < 3; k++)
                    m.Hoop(V(0, 0.18f + k * 0.09f, 0), V(0, 1, 0), 0.04f, 0.012f, 0.025f, 8, Pal.Glow);
            if (v == 4)
            {
                // the anchor hangs off the tripod
                Vector3 a = V(0.18f, 0.44f, 0.05f);
                m.Tube(a, a + V(0, -0.2f, 0), 0.016f, 0.016f, 6, Pal.SteelDeep);
                m.Hoop(a + V(0, 0.015f, 0), V(0, 0, 1), 0.022f, 0.008f, 0.012f, 8, Pal.SteelDeep);
                m.Tube(a + V(-0.05f, -0.14f, 0), a + V(0.05f, -0.14f, 0), 0.01f, 0.01f, 6, Pal.SteelDeep);
                m.Stem(new[] { a + V(-0.08f, -0.14f, 0), a + V(-0.06f, -0.21f, 0), a + V(0, -0.22f, 0), a + V(0.06f, -0.21f, 0), a + V(0.08f, -0.14f, 0) }, 0.014f, 0.012f, 6, Pal.SteelDeep);
                m.Spike(a + V(-0.08f, -0.14f, 0), V(-0.3f, 1, 0), 0.016f, 0.03f, Pal.SteelDeep);
                m.Spike(a + V(0.08f, -0.14f, 0), V(0.3f, 1, 0), 0.016f, 0.03f, Pal.SteelDeep);
            }
            return m;
        }

        static void Harpoonhead(Lp m, float x, float z, float y, float r, int barbs, int col = Pal.SteelLight)
        {
            m.Spike(V(x, y, z), V(0, 0, -1), r, 0.1f, col, 6);
            for (int i = 0; i < barbs; i++)
            {
                float a = i * 360f / barbs;
                Vector3 side = new Vector3(Mathf.Cos(a * Mathf.Deg2Rad), Mathf.Sin(a * Mathf.Deg2Rad), 0f);
                m.Spike(V(x, y, z - 0.035f) + side * r * 0.6f, side * 0.6f + V(0, 0, 1), r * 0.45f, 0.05f, barbs > 2 ? Pal.Rust : col, 4);
            }
        }

        static Lp HarpoonTurret(int v)
        {
            var m = new Lp();
            m.Push(V(0, 0.08f, 0));   // the gun rides 0.08 higher on the taller tripod
            float y = 0.6f, len = v == 1 ? 0.48f : v == 5 ? 0.42f : 0.38f;
            float bodyW = v == 5 ? 0.16f : v == 7 ? 0.17f : 0.12f, bodyH = v == 5 || v == 7 ? 0.13f : 0.1f;
            int bodyCol = v == 7 ? Pal.Brass : Pal.SteelMid, bodyTop = v == 7 ? Pal.BrassLight : Pal.Steel;
            m.Block(V(0, 0.52f, 0.03f), V(bodyW, bodyH, 0.26f), 0.025f, bodyCol, bodyTop);
            // rope drum behind the gun
            float drum = v == 1 ? 0.075f : 0.06f;
            m.Tube(V(-0.09f, 0.56f, 0.15f), V(0.09f, 0.56f, 0.15f), drum, drum, 10, Pal.Rope, Pal.Wood, Pal.Wood);
            m.Hoop(V(0, 0.56f, 0.15f), V(1, 0, 0), drum, 0.006f, 0.15f, 10, Pal.RopeDark);
            // stock grips
            for (int s = -1; s <= 1; s += 2)
                m.Stem(new[] { V(0.04f * s, 0.55f, 0.13f), V(0.06f * s, 0.5f, 0.24f) }, 0.014f, 0.012f, 6, Pal.Wood);
            float br = v == 5 ? 0.05f : v == 7 ? 0.06f : 0.035f;
            float[] barrels = v == 6 ? new[] { -0.045f, 0.045f } : new[] { 0f };
            foreach (float bx in barrels)
            {
                m.Tube(V(bx, y, -0.08f), V(bx, y + 0.01f, -len), br, br * 0.9f, 10, v == 7 ? Pal.SteelDeep : Pal.SteelDark);
                m.Hoop(V(bx, y + 0.005f, -len + 0.03f), V(0, 0.02f, 1), br, 0.008f, 0.02f, 10, Pal.Brass);
                if (v == 3)
                {
                    Harpoonhead(m, bx, -len, y + 0.01f, 0.026f, 2);
                    Harpoonhead(m, bx - 0.04f, -len + 0.02f, y + 0.01f, 0.018f, 0);
                    Harpoonhead(m, bx + 0.04f, -len + 0.02f, y + 0.01f, 0.018f, 0);
                }
                else if (v == 7)
                {
                    m.Spike(V(bx, y + 0.01f, -len), V(0, 0, -1), 0.06f, 0.18f, Pal.Glow, 8);
                    for (int k = 0; k < 3; k++)
                        m.Hoop(V(bx, y + 0.005f, -0.14f - k * 0.1f), V(0, 0.02f, 1), br, 0.012f, 0.025f, 10, Pal.Glow);
                }
                else
                    Harpoonhead(m, bx, -len, y + 0.01f, v == 5 ? 0.045f : 0.032f, v == 2 ? 4 : 2);
            }
            if (v == 1)
                m.Tube(V(0, y + 0.065f, 0.02f), V(0, y + 0.065f, -0.16f), 0.02f, 0.02f, 8, Pal.Brass, Pal.Glass, Pal.Glass);
            if (v == 5 || v == 7)
                for (int k = 0; k < 2; k++)
                    m.Hoop(V(0, 0.57f, -0.02f + k * 0.12f), V(0, 0, 1), bodyW * 0.62f, 0.01f, 0.03f, 8, Pal.BrassDark);
            m.Pop();
            return m;
        }

        // ---------------------------------------------------------------- Diving Bell

        static Lp Bell(int v)
        {
            var m = new Lp();
            Dock(m, DockFor(BellScale));
            float s = v == 7 ? 1.1f : v == 5 ? 1.05f : 1f;
            m.Disc(V(0.01f, Top, -0.01f), 0.24f, 12, Pal.SeaMid, 0.006f);
            m.Push(V(0, Top, 0), 0f, s);
            // skirt, heavy rim, dome
            m.Prism(V(0, 0, 0), 0.23f, 0.21f, 0.13f, 12, v == 6 ? Pal.SteelMid : Pal.BrassMid, Pal.Brass);
            m.Hoop(V(0, 0.015f, 0), V(0, 1, 0), 0.23f, 0.014f, 0.03f, 12, Pal.BrassDark);
            m.Dome(V(0, 0.13f, 0), V(0.21f, 0.28f, 0.21f), 12, 4, Pal.Brass, Pal.BrassLight, 0.78f);
            foreach (float a in v == 5 || v == 7 ? new[] { -90f, -30f, -150f, 30f } : new[] { -90f, -30f, -150f })
                m.Porthole(Dir(a) * 0.205f + V(0, 0.2f, 0) - Dir(a) * 0.015f, Dir(a, 0.15f), 0.042f, Pal.Glass, Pal.BrassDark);
            // hatch and its valve wheel
            m.Prism(V(0, 0.39f, 0), 0.07f, 0.065f, 0.04f, 10, Pal.SteelMid, Pal.Steel);
            m.Hoop(V(0, 0.45f, 0), V(0, 1, 0), 0.045f, 0.01f, 0.012f, 10, Pal.Red);
            m.Tube(V(-0.045f, 0.45f, 0), V(0.045f, 0.45f, 0), 0.006f, 0.006f, 4, Pal.RedDark);
            m.Tube(V(0, 0.45f, -0.045f), V(0, 0.45f, 0.045f), 0.006f, 0.006f, 4, Pal.RedDark);
            m.Hoop(V(0, 0.49f, 0), V(1, 0, 0), 0.025f, 0.008f, 0.012f, 8, Pal.SteelDark);
            m.Pop();
            // running lights: port is red (left), starboard green (right)
            Light(m, V(-0.235f, Top, 0.09f), Pal.Red);
            Light(m, V(0.235f, Top, 0.09f), Pal.Green);
            if (v == 5 || v == 7) Light(m, V(0.16f, Top, -0.18f), Pal.Yellow);
            switch (v)
            {
                case 1:                                                                                 // Flippers
                    for (int i = 0; i < 2; i++)
                        m.Blade(V(-0.2f + i * 0.05f, Top, -0.14f), V(-0.17f + i * 0.05f, Top + 0.22f, -0.2f), V(1, 0, 0), 0.06f, Pal.Teal, Pal.TealDark);
                    break;
                case 2:                                                                                 // Hard Hats
                    m.Block(V(-0.14f, Top, 0.19f), V(0.2f, 0.05f, 0.07f), 0.01f, Pal.Wood, Pal.WoodLight);
                    for (int i = 0; i < 3; i++)
                    {
                        Vector3 h = V(-0.205f + i * 0.065f, Top + 0.08f, 0.19f);
                        m.Ball(h, V(0.032f, 0.032f, 0.032f), 8, 4, Pal.Brass, Pal.BrassLight);
                        m.Disc(h + V(0, -0.005f, -0.03f), 0.012f, 6, Pal.Glass);
                    }
                    break;
                case 3:                                                                                 // Overhaul
                    m.Block(V(0.17f, Top, -0.15f), V(0.1f, 0.06f, 0.07f), 0.01f, Pal.Red, Pal.RedDark);
                    m.Hoop(V(0.17f, Top + 0.07f, -0.15f), V(0, 0, 1), 0.022f, 0.006f, 0.012f, 6, Pal.SteelDark);
                    m.Stem(new[] { V(-0.05f, 0.46f, 0.06f), V(-0.05f, 0.62f, 0.06f), V(-0.21f, 0.62f, 0.06f) }, 0.014f, 0.012f, 6, Pal.SteelMid);
                    m.Tube(V(-0.21f, 0.62f, 0.06f), V(-0.21f, 0.48f, 0.06f), 0.004f, 0.004f, 4, Pal.SteelDeep);
                    m.Stem(new[] { V(-0.21f, 0.48f, 0.06f), V(-0.21f, 0.45f, 0.06f), V(-0.18f, 0.45f, 0.06f) }, 0.008f, 0.008f, 4, Pal.SteelDark);
                    break;
                case 4:                                                                                 // Calibrate
                    m.Tube(V(0.19f, Top, -0.16f), V(0.19f, Top + 0.22f, -0.16f), 0.012f, 0.012f, 6, Pal.SteelDark);
                    m.Tube(V(0.19f, Top + 0.25f, -0.13f), V(0.19f, Top + 0.25f, -0.17f), 0.045f, 0.045f, 12, Pal.Brass, Pal.White, Pal.Brass);
                    m.Spike(V(0.19f, Top + 0.25f, -0.175f), V(0.6f, 0.8f, 0), 0.006f, 0.032f, Pal.Red);
                    break;
                case 6:                                                                                 // Bulkhead
                    for (int i = 0; i < 6; i++)
                    {
                        Vector3 d = Dir(i * 60f);
                        m.Push(d * 0.235f + V(0, Top + 0.02f, 0), -i * 60f + 90f);
                        m.Block(V(0, 0, 0), V(0.14f, 0.12f, 0.025f), 0.008f, Pal.SteelDark, Pal.SteelMid);
                        m.Ball(V(-0.06f, 0.09f, -0.014f), Vector3.one * 0.008f, 4, 2, Pal.Rivet);
                        m.Ball(V(0.06f, 0.09f, -0.014f), Vector3.one * 0.008f, 4, 2, Pal.Rivet);
                        m.Pop();
                    }
                    break;
                case 7:                                                                                 // Full Crew: an A-frame hoist over the bell
                    for (int sx = -1; sx <= 1; sx += 2)
                    {
                        m.Stem(new[] { V(0.25f * sx, Top, 0.15f), V(0.1f * sx, 0.82f, 0.03f) }, 0.018f, 0.015f, 6, Pal.SteelMid);
                        m.Stem(new[] { V(0.25f * sx, Top, -0.12f), V(0.1f * sx, 0.82f, 0.03f) }, 0.018f, 0.015f, 6, Pal.SteelMid);
                    }
                    m.Tube(V(-0.12f, 0.82f, 0.03f), V(0.12f, 0.82f, 0.03f), 0.022f, 0.022f, 8, Pal.Brass, Pal.BrassLight, Pal.BrassLight);
                    m.Tube(V(0, 0.82f, 0.03f), V(0, 0.6f, 0.01f), 0.006f, 0.006f, 4, Pal.SteelDeep);
                    break;
            }
            return m;
        }

        static void Light(Lp m, Vector3 at, int col)
        {
            m.Tube(at, at + V(0, 0.14f, 0), 0.012f, 0.012f, 6, Pal.SteelDark);
            m.Ball(at + V(0, 0.16f, 0), Vector3.one * 0.03f, 8, 4, col, Pal.LampLight, 0.8f);
        }

        // ---------------------------------------------------------------- Deep Drill

        static Lp DrillBase(int v)
        {
            var m = new Lp();
            Dock(m, DockFor(DrillScale));
            float h = v == 3 || v == 7 ? 0.92f : 0.82f, w = 0.21f, t = 0.07f;
            int leg = v == 7 ? Pal.BrassMid : Pal.SteelMid;
            Vector3[] feet = { V(-w, Top, -w), V(w, Top, -w), V(w, Top, w), V(-w, Top, w) };
            Vector3[] tops = { V(-t, h, -t), V(t, h, -t), V(t, h, t), V(-t, h, t) };
            for (int i = 0; i < 4; i++)
            {
                m.Stem(new[] { feet[i], tops[i] }, 0.024f, 0.018f, 6, leg);
                m.Prism(feet[i], 0.035f, 0.03f, 0.02f, 6, Pal.BrassDark, Pal.Brass);
            }
            foreach (float k in new[] { 0.36f, 0.68f })
            {
                float y = Top + (h - Top) * k;
                Vector3[] ring = new Vector3[4];
                for (int i = 0; i < 4; i++) ring[i] = Vector3.Lerp(feet[i], tops[i], k);
                for (int i = 0; i < 4; i++) m.Tube(ring[i], ring[(i + 1) % 4], 0.011f, 0.011f, 5, Pal.BrassDark);
            }
            // crown block with its pulley
            m.Block(V(0, h - 0.01f, 0), V(0.2f, 0.06f, 0.2f), 0.015f, Pal.Brass, Pal.BrassLight);
            m.Tube(V(-0.06f, h + 0.07f, 0), V(0.06f, h + 0.07f, 0), 0.05f, 0.05f, 10, Pal.SteelDark, Pal.Steel, Pal.Steel);
            // drill pipe down the middle, into a dark bore
            m.Tube(V(0, 0.18f, 0), V(0, h, 0), 0.024f, 0.024f, 8, Pal.SteelLight);
            m.Disc(V(0, Top, 0), 0.11f, 10, Pal.Ink, 0.004f);
            // the engine: a copper block with an exhaust stack
            m.Block(V(-0.1f, Top, 0.12f), V(0.13f, 0.12f, 0.11f), 0.02f, Pal.Copper, Pal.CopperDark);
            m.Tube(V(-0.13f, Top + 0.12f, 0.15f), V(-0.13f, Top + 0.32f, 0.15f), 0.018f, 0.018f, 6, Pal.SteelDeep, Pal.Ink);
            m.Tube(V(-0.035f, Top + 0.07f, 0.12f), V(-0.015f, Top + 0.07f, 0.12f), 0.045f, 0.045f, 10, Pal.SteelMid, Pal.Steel, Pal.Steel);
            switch (v)
            {
                case 1:                                                                                 // Rotary Bit: a big gear
                    m.Tube(V(-0.27f, 0.3f, 0.02f), V(-0.24f, 0.3f, 0.02f), 0.11f, 0.11f, 12, Pal.Brass, Pal.BrassLight, Pal.BrassLight);
                    for (int i = 0; i < 10; i++)
                    {
                        float a = i * 36f * Mathf.Deg2Rad;
                        m.Spike(V(-0.255f, 0.3f + Mathf.Sin(a) * 0.11f, 0.02f + Mathf.Cos(a) * 0.11f), V(0, Mathf.Sin(a), Mathf.Cos(a)), 0.018f, 0.028f, Pal.BrassMid, 4);
                    }
                    break;
                case 2:                                                                                 // Seismic: a thumper piston
                    m.Tube(V(0f, Top, -0.25f), V(0f, 0.55f, -0.25f), 0.02f, 0.02f, 8, Pal.SteelLight);
                    m.Block(V(0f, 0.28f, -0.25f), V(0.11f, 0.1f, 0.09f), 0.02f, Pal.Lead, Pal.LeadDark);
                    m.Disc(V(0f, Top, -0.25f), 0.05f, 8, Pal.SteelDeep, 0.012f);
                    break;
                case 3:                                                                                 // Deep Core: a glowing core pipe
                    for (int k = 0; k < 4; k++)
                        m.Hoop(V(0, 0.28f + k * 0.15f, 0), V(0, 1, 0), 0.024f, 0.012f, 0.03f, 8, Pal.Glow);
                    break;
                case 4:                                                                                 // Aftershock: sprung legs
                    for (int i = 0; i < 4; i++)
                        for (int k = 0; k < 4; k++)
                            m.Hoop(Vector3.Lerp(feet[i], tops[i], 0.08f + k * 0.04f), V(0, 1, 0), 0.026f, 0.008f, 0.012f, 6, Pal.Yellow);
                    break;
                case 5:                                                                                 // Gusher: a spout throwing up ore
                    m.Stem(new[] { V(-0.1f, Top, -0.12f), V(-0.2f, 0.35f, -0.2f), V(-0.24f, 0.52f, -0.14f) }, 0.03f, 0.025f, 8, Pal.SteelMid);
                    foreach (var o in new[] { V(-0.24f, 0.58f, -0.14f), V(-0.2f, 0.62f, -0.18f), V(-0.29f, 0.6f, -0.1f) })
                        m.Ball(o, Vector3.one * 0.035f, 6, 3, Pal.Ore, Pal.Bubble, 0.5f, Pal.OreDark);
                    break;
                case 6:                                                                                 // Fault Line: cracked slabs and shards
                    for (int i = 0; i < 5; i++)
                    {
                        Vector3 d = Dir(i * 72f + 20f);
                        m.Spike(d * 0.26f + V(0, Top, 0), d * 0.3f + V(0, 1, 0), 0.03f, 0.1f, Pal.StoneDark, 4);
                        m.Tube(d * 0.12f + V(0, Top + 0.003f, 0), d * 0.28f + V(0, Top + 0.003f, 0), 0.008f, 0.008f, 4, Pal.Ink);
                    }
                    break;
                case 7:                                                                                 // Mantle Tap: a brass housing over a magma core
                    m.Prism(V(0, Top, 0), 0.16f, 0.14f, 0.1f, 10, Pal.Brass, Pal.BrassLight);
                    for (int k = 0; k < 4; k++)
                        m.Hoop(V(0, 0.32f + k * 0.15f, 0), V(0, 1, 0), 0.024f, 0.014f, 0.035f, 8, Pal.Magma);
                    m.Disc(V(0, Top + 0.1f, 0), 0.06f, 10, Pal.Magma, 0.004f);
                    break;
            }
            return m;
        }

        // The chuck: turns while the drill bites (ArgoClient spins the "Spinner" part).
        static Lp DrillBit(int v)
        {
            var m = new Lp();
            float r = v == 7 ? 0.09f : 0.07f;
            m.Prism(V(0, 0.1f, 0), r, r * 0.9f, 0.08f, 6, v == 7 ? Pal.Magma : Pal.SteelMid, Pal.Brass);
            for (int i = 0; i < 3; i++)
            {
                Vector3 d = Dir(i * 120f);
                m.Spike(V(0, 0.11f, 0) + d * r * 0.8f, d + V(0, -1.2f, 0), 0.016f, 0.05f, Pal.SteelLight, 4);
            }
            m.Hoop(V(0, 0.17f, 0), V(0, 1, 0), r * 0.7f, 0.01f, 0.02f, 6, v == 1 ? Pal.Gold : Pal.BrassDark);
            return m;
        }

        // ---------------------------------------------------------------- Fathom overlays (added to the drill per Fathom)

        // Pipe sections stacked on the dock as the drill sinks, and a depth post with one glowing ring per Fathom below 1.
        static Lp Fathom(int f)
        {
            var m = new Lp();
            int n = f - 1;
            for (int i = 0; i < n; i++)
            {
                float y = Top + 0.02f + (i / 2) * 0.038f, x = -0.17f + (i % 2) * 0.04f;
                m.Tube(V(x, y, -0.26f + 0.015f * (i % 2)), V(x + 0.2f, y, -0.24f), 0.018f, 0.018f, 8, Pal.SteelMid, Pal.Brass, Pal.Brass);
            }
            Vector3 post = V(0.22f, Top, -0.16f);
            m.Tube(post, post + V(0, 0.32f, 0), 0.014f, 0.012f, 6, Pal.BrassMid, Pal.Brass);
            for (int i = 0; i < n; i++)
                m.Hoop(post + V(0, 0.06f + i * 0.065f, 0), V(0, 1, 0), 0.014f, 0.016f, 0.028f, 8, Pal.Glow);
            m.Ball(post + V(0, 0.34f, 0), Vector3.one * 0.022f, 6, 3, Pal.Glow, Pal.Bubble, 0.4f);
            return m;
        }

        // ---------------------------------------------------------------- projectiles (pointing +z, recentred)

        static Lp Pellet()
        {
            var m = new Lp();
            m.Ball(V(0, 0, 0), V(0.065f, 0.065f, 0.075f), 8, 5, Pal.SteelDark, Pal.SteelLight, 0.4f);
            return m;
        }

        static Lp HarpoonShot(bool lance)
        {
            var m = new Lp();
            float r = lance ? 0.03f : 0.016f;
            m.Tube(V(0, 0, -0.28f), V(0, 0, 0.18f), r, r, 6, lance ? Pal.Brass : Pal.Wood);
            m.Spike(V(0, 0, 0.18f), V(0, 0, 1), lance ? 0.065f : 0.032f, lance ? 0.2f : 0.1f, lance ? Pal.Glow : Pal.SteelLight, 6);
            for (int i = 0; i < 2; i++)
            {
                Vector3 side = i == 0 ? V(1, 0, 0) : V(-1, 0, 0);
                m.Spike(V(0, 0, 0.2f) + side * (lance ? 0.04f : 0.02f), side * 0.6f + V(0, 0, -1), lance ? 0.025f : 0.012f, lance ? 0.07f : 0.045f, lance ? Pal.GlowDark : Pal.Steel, 4);
            }
            if (lance)
                for (int k = 0; k < 3; k++)
                    m.Hoop(V(0, 0, -0.18f + k * 0.12f), V(0, 0, 1), r, 0.012f, 0.025f, 8, Pal.Glow);
            else
                m.Tube(V(0, 0, -0.28f), V(0, 0.01f, -0.4f), 0.008f, 0.008f, 4, Pal.Rope);
            return m;
        }
    }
}
