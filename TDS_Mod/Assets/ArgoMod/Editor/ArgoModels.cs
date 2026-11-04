// Argo asset generator: every tower model (buildables, upgrades, Fathom overlays, projectiles), built low-poly and sleek
// like Sno Sno's and Buster's towers: a handful of clean solids per model, flat palette colours, readable silhouettes,
// no voxels. Every Argo building stands on the same riveted stone-and-brass dock. Upgrades keep the silhouette and add
// one clear feature. The Scatter Cannon and the Harpoon Gun keep their gun on a separate "Turret" part (the clients
// swing it toward each shot); the Drill Rig's chuck is a "Spinner" part that turns while it bites. Deep Water (2.0) is
// a walk-through pool in a stone frame with a small brass pump dome in its back corner, its water a "Swell" part the
// clients bob; FLOOD pools are the frame and water alone (Argo_Pool).
// Units: 1 = one tile; y measured up from the ground; the front faces -z; a turret's barrel points down -z.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ArgoBuild
{
    public class ModelDef
    {
        public string Name;
        public string Group;            // Scatter, Harpoon, Bell, Drill, Water, Extras, Projectiles
        public bool Projectile;
        public Func<Lp> Build;
        public Func<Lp> Part;           // optional moving part, built in tower space
        public string PartName;         // "Turret", "Spinner" or "Swell"
        public Vector3 PartPivot;       // tower space, y up from the ground
        public float Scale = 1f;        // baked into the mesh about the ground point (matched to the shipped towers' sizes)
    }

    public static class ArgoModels
    {
        static Vector3 V(float x, float y, float z) { return new Vector3(x, y, z); }
        static Vector3 Dir(float deg, float y = 0f) { return new Vector3(Mathf.Cos(deg * Mathf.Deg2Rad), y, Mathf.Sin(deg * Mathf.Deg2Rad)); }

        static readonly string[] ScatterKeys = { "", "_Widebore2", "_Double2", "_Grapeshot3", "_Barnacle3", "_Pressure4", "_Ironclad4", "_Broadside5" };
        static readonly string[] HarpoonKeys = { "", "_Longline2", "_Tether2", "_Skewer3", "_Anchor3", "_Whaler4", "_Twinline4", "_Leviathan5", "_Reel3", "_Undertow5" };
        static readonly string[] BellKeys = { "", "_Flippers2", "_Salts2", "_Overhaul3", "_Calibrate3", "_Thirdmate4", "_Bulkhead4", "_Fullcrew5" };
        static readonly string[] DrillKeys = { "", "_Paydirt2", "_Seismic2", "_Deepcore3", "_Bedrock3", "_Gusher4", "_Faultline4", "_Mantle5" };
        static readonly string[] WaterKeys = { "", "_Undercurrent2", "_Brine2", "_Kelp3", "_Salt3", "_Abyss4", "_Crushing4", "_Trench5" };

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
            for (int i = 0; i < HarpoonKeys.Length; i++)
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
            for (int i = 0; i < WaterKeys.Length; i++)
            {
                int v = i;
                l.Add(new ModelDef { Group = "Water", Name = "Trap_Argowater" + WaterKeys[i], Build = () => Water(v, true), Part = () => Swell(v), PartName = "Swell", PartPivot = Vector3.zero });
            }
            l.Add(new ModelDef { Group = "Water", Name = "Argo_Pool", Build = () => Water(0, false), Part = () => Swell(0), PartName = "Swell", PartPivot = Vector3.zero });
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
            if (v == 6)                                                                                // Ironclad: a riveted armour skirt
            {
                m.Prism(V(0, Top, 0), 0.235f, 0.215f, 0.11f, 8, Pal.SteelDark, Pal.SteelMid, -1, 22.5f);
                for (int i = 0; i < 8; i++)
                    m.Ball(Dir(i * 45f) * 0.228f + V(0, Top + 0.075f, 0), Vector3.one * 0.013f, 4, 2, Pal.Rivet);
            }
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
                case 2:                                                                                // Double Barrel: a shorter second barrel under the first
                    m.Push(V(0, 0.04f, 0));
                    ScatterBarrel(m, 0, 0.068f, 0.3f, 0.105f, 0.1f);
                    m.Pop();
                    m.Push(V(0, -0.1f, 0));
                    ScatterBarrel(m, 0, 0.056f, 0.26f, 0.085f, 0.08f, Pal.SteelMid);
                    m.Pop();
                    m.Block(V(0, 0.24f, -0.06f), V(0.16f, 0.24f, 0.035f), 0.012f, Pal.BrassMid, Pal.Brass);
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
                case 6:                                                                                // Ironclad: a riveted gun shield
                    ScatterBarrel(m, 0, 0.075f, 0.3f, 0.14f, 0.14f);
                    m.Block(V(0, 0.27f, -0.03f), V(0.3f, 0.21f, 0.035f), 0.012f, Pal.SteelDark, Pal.SteelMid, Pal.Steel);
                    for (int sx = -1; sx <= 1; sx += 2)
                        for (int k = 0; k < 2; k++)
                            m.Ball(V(0.12f * sx, 0.3f + k * 0.13f, -0.05f), Vector3.one * 0.012f, 4, 2, Pal.Rivet);
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

        // ---------------------------------------------------------------- Harpoon Gun

        static Lp HarpoonBase(int v)
        {
            var m = new Lp();
            Dock(m, DockFor(HarpoonScale));
            float leg = v == 5 || v == 7 || v == 9 ? 0.032f : 0.024f;
            for (int i = 0; i < 3; i++)
            {
                Vector3 foot = Dir(90f + i * 120f) * 0.235f + V(0, Top, 0);
                m.Stem(new[] { foot, V(0, 0.55f, 0) + Dir(90f + i * 120f) * 0.04f }, leg, leg * 0.8f, 6, Pal.BrassMid);
                m.Prism(foot, leg * 1.8f, leg * 1.5f, 0.02f, 6, Pal.BrassDark, Pal.Brass);
            }
            m.Tube(V(0, Top, 0), V(0, 0.56f, 0), 0.04f, 0.035f, 8, Pal.SteelDark);
            m.Prism(V(0, 0.53f, 0), 0.075f, 0.065f, 0.07f, 8, Pal.Brass, Pal.BrassLight);
            if (v == 7 || v == 9)
                for (int k = 0; k < 3; k++)
                    m.Hoop(V(0, 0.18f + k * 0.09f, 0), V(0, 1, 0), 0.04f, 0.012f, 0.025f, 8, v == 9 ? Pal.Teal : Pal.Glow);
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
            // Barrel lengths keep the tip about 0.55-0.65 tiles from the centre once scaled (like the Scatter Cannon's
            // muzzle), so the gun stays near its own tile as it turns and the tip sits where the shot leaves.
            float y = 0.6f, len = v == 1 ? 0.36f : v == 5 ? 0.31f : v == 7 ? 0.24f : v == 9 ? 0.26f : 0.28f;
            float bodyW = v == 5 ? 0.16f : v == 7 || v == 9 ? 0.17f : 0.12f, bodyH = v == 5 || v == 7 || v == 9 ? 0.13f : 0.1f;
            int bodyCol = v == 7 ? Pal.Brass : v == 9 ? Pal.SteelDark : Pal.SteelMid, bodyTop = v == 7 ? Pal.BrassLight : Pal.Steel;
            bool crank = v == 6 || v == 8 || v == 9;   // the reel path (Reel, Twin Line, Undertow) carries the winch
            m.Block(V(0, 0.52f, 0.03f), V(bodyW, bodyH, 0.26f), 0.025f, bodyCol, bodyTop);
            // rope drum behind the gun
            float drum = v == 1 ? 0.075f : crank ? 0.08f : 0.06f;
            int rope = v == 2 || v == 9 ? Pal.Teal : Pal.Rope, ropeDark = v == 2 || v == 9 ? Pal.TealDark : Pal.RopeDark;
            m.Tube(V(-0.09f, 0.56f, 0.15f), V(0.09f, 0.56f, 0.15f), drum, drum, 10, rope, Pal.Wood, Pal.Wood);
            m.Hoop(V(0, 0.56f, 0.15f), V(1, 0, 0), drum, 0.006f, 0.15f, 10, ropeDark);
            if (crank)
            {
                // the winch: a spoked red wheel on each end of the drum, one with a handle
                for (int sx = -1; sx <= 1; sx += 2)
                {
                    Vector3 hub = V(0.11f * sx, 0.56f, 0.15f);
                    m.Hoop(hub, V(1, 0, 0), 0.095f, 0.014f, 0.02f, 12, Pal.Red);
                    for (int k = 0; k < 3; k++)
                    {
                        float a = k * 60f * Mathf.Deg2Rad;
                        Vector3 d = new Vector3(0f, Mathf.Sin(a), Mathf.Cos(a)) * 0.09f;
                        m.Tube(hub - d, hub + d, 0.007f, 0.007f, 4, Pal.RedDark);
                    }
                    m.Ball(hub, Vector3.one * 0.022f, 6, 3, Pal.BrassLight);
                    if (sx > 0)
                        m.Stem(new[] { hub + V(0, 0.08f, -0.04f), hub + V(0.05f, 0.08f, -0.04f) }, 0.011f, 0.011f, 6, Pal.Brass);
                }
            }
            // stock grips
            for (int s = -1; s <= 1; s += 2)
                m.Stem(new[] { V(0.04f * s, 0.55f, 0.13f), V(0.06f * s, 0.5f, 0.24f) }, 0.014f, 0.012f, 6, Pal.Wood);
            float br = v == 5 ? 0.05f : v == 7 ? 0.06f : v == 9 ? 0.045f : 0.035f;
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
                        m.Hoop(V(bx, y + 0.005f, -0.11f - k * 0.055f), V(0, 0.02f, 1), br, 0.012f, 0.025f, 10, Pal.Glow);
                }
                else if (v == 9)
                {
                    // Undertow: a grapnel, four hooks curling back around a central spike
                    m.Spike(V(bx, y + 0.01f, -len), V(0, 0, -1), 0.03f, 0.08f, Pal.SteelLight, 6);
                    for (int k = 0; k < 4; k++)
                    {
                        float a = (k * 90f + 45f) * Mathf.Deg2Rad;
                        Vector3 side = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
                        Vector3 root = V(bx, y + 0.01f, -len - 0.02f);
                        m.Stem(new[] { root, root + side * 0.06f + V(0, 0, -0.03f), root + side * 0.075f + V(0, 0, 0.02f) }, 0.01f, 0.008f, 5, Pal.SteelLight);
                        m.Spike(root + side * 0.075f + V(0, 0, 0.02f), side * -0.4f + V(0, 0, 1), 0.01f, 0.03f, Pal.SteelLight, 4);
                    }
                    for (int k = 0; k < 2; k++)
                        m.Hoop(V(bx, y + 0.005f, -0.12f - k * 0.07f), V(0, 0.02f, 1), br, 0.012f, 0.025f, 10, Pal.Teal);
                }
                else
                    Harpoonhead(m, bx, -len, y + 0.01f, v == 5 ? 0.045f : 0.032f, 2);
            }
            if (v == 2)
                // Tether: a teal line runs from the drum over the gun to the harpoon's head
                m.Stem(new[] { V(0.03f, 0.63f, 0.13f), V(0.06f, 0.67f, 0.0f), V(0.065f, 0.665f, -0.15f), V(0.04f, y + 0.035f, -len + 0.03f) }, 0.009f, 0.009f, 5, Pal.Teal);
            if (v == 1)
                m.Tube(V(0, y + 0.065f, 0.02f), V(0, y + 0.065f, -0.16f), 0.02f, 0.02f, 8, Pal.Brass, Pal.Glass, Pal.Glass);
            if (v == 5 || v == 7 || v == 9)
                for (int k = 0; k < 2; k++)
                    m.Hoop(V(0, 0.57f, -0.02f + k * 0.12f), V(0, 0, 1), bodyW * 0.62f, 0.01f, 0.03f, 8, v == 9 ? Pal.Teal : Pal.BrassDark);
            m.Pop();
            return m;
        }

        // ---------------------------------------------------------------- Miner's Base (1.4.0's Diving Bell, same model)

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
            if (v != 2)
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
                case 2:                                                                                 // Old Salts: a captain's cap on the top hatch
                    m.Prism(V(0, Top + 0.42f, 0), 0.075f, 0.08f, 0.035f, 10, Pal.Navy, Pal.Navy);
                    m.Prism(V(0, Top + 0.455f, 0.008f), 0.08f, 0.1f, 0.04f, 10, Pal.White, Pal.White);
                    m.Push(V(0, Top + 0.425f, -0.07f), Quaternion.Euler(-14f, 0f, 0f), Vector3.one);
                    m.Block(V(0, 0, -0.02f), V(0.13f, 0.014f, 0.06f), 0.005f, Pal.Ink, Pal.Ink);
                    m.Pop();
                    m.Ball(V(0, Top + 0.44f, -0.078f), V(0.022f, 0.022f, 0.012f), 6, 3, Pal.Gold, Pal.Yellow);
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

        // ---------------------------------------------------------------- Drill Rig (1.4.0's Deep Drill)

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
                case 1:                                                                                 // Pay Dirt: an ore skip hung on the frame's front
                {
                    float y = Top + (h - Top) * 0.36f, z = -w + (w - t) * 0.36f - 0.005f;
                    for (int s = -1; s <= 1; s += 2)
                        m.Tube(V(0.09f + 0.035f * s, y, z), V(0.09f + 0.05f * s, 0.265f, -0.21f), 0.007f, 0.007f, 4, Pal.SteelDark);
                    m.Prism(V(0.09f, 0.165f, -0.21f), 0.06f, 0.08f, 0.1f, 8, Pal.Rust, Pal.OreDark);
                    m.Hoop(V(0.09f, 0.258f, -0.21f), V(0, 1, 0), 0.08f, 0.009f, 0.018f, 8, Pal.RustDark);
                    foreach (var o in new[] { V(0.065f, 0.275f, -0.2f), V(0.115f, 0.278f, -0.215f), V(0.088f, 0.295f, -0.23f) })
                        m.Ball(o, Vector3.one * 0.034f, 6, 3, Pal.Ore, Pal.Gold, 0.4f, Pal.OreDark);
                    break;
                }
                case 2:                                                                                 // Seismic: a thumper piston
                    m.Tube(V(0f, Top, -0.25f), V(0f, 0.55f, -0.25f), 0.02f, 0.02f, 8, Pal.SteelLight);
                    m.Block(V(0f, 0.28f, -0.25f), V(0.11f, 0.1f, 0.09f), 0.02f, Pal.Lead, Pal.LeadDark);
                    m.Disc(V(0f, Top, -0.25f), 0.05f, 8, Pal.SteelDeep, 0.012f);
                    break;
                case 3:                                                                                 // Deep Core: a glowing core pipe
                    for (int k = 0; k < 4; k++)
                        m.Hoop(V(0, 0.28f + k * 0.15f, 0), V(0, 1, 0), 0.024f, 0.012f, 0.03f, 8, Pal.Glow);
                    break;
                case 4:                                                                                 // Bedrock: a dark stone collar with teeth around the bore
                    m.Prism(V(0, Top, 0), 0.17f, 0.15f, 0.07f, 8, Pal.StoneDeep, Pal.StoneDark, -1, 22.5f);
                    for (int i = 0; i < 8; i++)
                    {
                        Vector3 d = Dir(i * 45f + 22.5f);
                        m.Spike(d * 0.14f + V(0, Top + 0.065f, 0), d * -0.5f + V(0, 1, 0), 0.03f, 0.11f, i % 2 == 0 ? Pal.Sand : Pal.StoneLight, 4);
                    }
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
            m.Hoop(V(0, 0.17f, 0), V(0, 1, 0), r * 0.7f, 0.01f, 0.02f, 6, Pal.BrassDark);
            return m;
        }

        // ---------------------------------------------------------------- Deep Water (trap, walk-through)

        // A pool in a low stone frame filling the tile (the trap template's footprint), flush enough to walk through. The
        // trap itself has a small brass pump dome with a porthole in its back-right corner (Argo's motif, on the top face);
        // FLOOD pools (pump = false) are the frame and water alone. Upgrades add one feature each; the water darkens from
        // Crushing Depth on. The frame stands still; the water and what floats on it are the "Swell" part, which the
        // clients bob gently (DESIGN "Tower motion"). Its sides sit inside the rim, so a bob of ±0.012 never shows a gap.
        const float WaterHalf = 0.4f, WaterRim = 0.07f, WaterRimH = 0.075f, WaterSurf = 0.055f;

        static Lp Water(int v, bool pump)
        {
            var m = new Lp();
            const float half = WaterHalf, rim = WaterRim, rimH = WaterRimH;
            float inner = half - rim;
            m.Block(V(0, 0, -half + rim / 2f), V(half * 2f, rimH, rim), 0.02f, Pal.StoneMid, Pal.Stone);
            m.Block(V(0, 0, half - rim / 2f), V(half * 2f, rimH, rim), 0.02f, Pal.StoneMid, Pal.Stone);
            m.Block(V(-half + rim / 2f, 0, 0), V(rim, rimH, inner * 2f), 0.02f, Pal.StoneMid, Pal.Stone);
            m.Block(V(half - rim / 2f, 0, 0), V(rim, rimH, inner * 2f), 0.02f, Pal.StoneMid, Pal.Stone);
            int cap = v == 7 ? Pal.Gold : Pal.BrassMid, capTop = v == 7 ? Pal.LampLight : Pal.Brass;
            for (int i = 0; i < 4; i++)
            {
                Vector3 c = V(i % 2 == 0 ? -half + rim / 2f : half - rim / 2f, rimH, i < 2 ? -half + rim / 2f : half - rim / 2f);
                m.Prism(c, 0.045f, 0.04f, 0.018f, 8, cap, capTop, -1, 22.5f);
            }
            if (pump)
            {
                Vector3 p = V(0.27f, rimH, 0.27f);
                m.Prism(p, 0.1f, 0.095f, 0.04f, 10, Pal.BrassMid, Pal.Brass);
                m.Dome(p + V(0, 0.04f, 0), V(0.085f, 0.11f, 0.085f), 10, 3, Pal.Brass, Pal.BrassLight, 0.7f);
                m.Porthole(p + V(0, 0.085f, -0.075f), V(0, 0.2f, -1), 0.028f, Pal.Glass, Pal.BrassDark);
                m.Tube(p + V(0, 0.15f, 0), p + V(0, 0.18f, 0), 0.012f, 0.012f, 6, Pal.SteelDark);
                m.Hoop(p + V(0, 0.185f, 0), V(0, 1, 0), 0.035f, 0.008f, 0.01f, 10, Pal.Red);
                m.Tube(p + V(-0.035f, 0.185f, 0), p + V(0.035f, 0.185f, 0), 0.005f, 0.005f, 4, Pal.RedDark);
                // outflow spout into the pool
                m.Stem(new[] { p + V(-0.07f, 0.05f, -0.03f), p + V(-0.12f, 0.05f, -0.06f), p + V(-0.14f, 0.0f, -0.08f) }, 0.016f, 0.016f, 6, Pal.Copper);
            }
            switch (v)
            {
                case 2:                                                                                 // Brine: salt crusts on the rim
                    Salt(m, V(-0.3f, rimH, -0.36f), 5, 21, false);
                    Salt(m, V(0.36f, rimH, -0.1f), 4, 22, false);
                    Salt(m, V(-0.36f, rimH, 0.18f), 4, 23, false);
                    break;
                case 4:                                                                                 // Salt Sting: crystal spikes along the front
                    for (int i = 0; i < 6; i++)
                        Salt(m, V(-0.3f + i * 0.12f, rimH, -0.365f), 3, 30 + i, true);
                    break;
                case 5:                                                                                 // Abyss: a second pump spout on the front
                    if (pump)
                    {
                        m.Stem(new[] { V(0.27f, rimH + 0.05f, 0.2f), V(0.36f, rimH + 0.08f, 0f), V(0.36f, rimH + 0.08f, -0.25f), V(0.3f, rimH + 0.03f, -0.3f) }, 0.018f, 0.018f, 6, Pal.Copper);
                        m.Tube(V(0.3f, rimH + 0.03f, -0.3f), V(0.27f, rimH - 0.01f, -0.27f), 0.018f, 0.03f, 8, Pal.Brass, Pal.Ink);
                    }
                    break;
                case 6:                                                                                 // Crushing Depth: a pressure gauge
                    if (pump)
                    {
                        Vector3 g = V(0.16f, rimH + 0.14f, 0.27f);
                        m.Tube(V(0.2f, rimH + 0.06f, 0.27f), g + V(0.02f, -0.03f, 0), 0.008f, 0.008f, 4, Pal.SteelDark);
                        m.Tube(g + V(0, 0, 0.015f), g + V(0, 0, -0.02f), 0.04f, 0.04f, 12, Pal.Brass, Pal.White, Pal.Brass);
                        m.Spike(g + V(0, 0, -0.022f), V(0.7f, 0.7f, 0), 0.006f, 0.03f, Pal.Red);
                    }
                    break;
            }
            return m;
        }

        // The water (the frame's inside, from the ground to the surface) and what floats on or rises from it.
        static Lp Swell(int v)
        {
            var m = new Lp();
            const float surf = WaterSurf;
            float inner = WaterHalf - WaterRim;
            int water = v == 7 ? Pal.SeaDeep : v == 6 ? Pal.SeaDark : Pal.Sea, waterSide = v >= 6 ? Pal.SeaDeep : Pal.SeaMid;
            m.Block(V(0, 0, 0), V(inner * 2f + 0.01f, surf, inner * 2f + 0.01f), 0.005f, waterSide, water);
            m.Disc(V(-0.12f, surf + 0.002f, -0.1f), 0.1f, 10, v >= 6 ? Pal.SeaMid : Pal.SeaLight, 0.002f);
            m.Ball(V(0.1f, surf, -0.16f), V(0.022f, 0.01f, 0.022f), 6, 2, Pal.Bubble);
            m.Ball(V(0.15f, surf, -0.12f), V(0.014f, 0.008f, 0.014f), 6, 2, Pal.Bubble);
            switch (v)
            {
                case 1:                                                                                 // Undercurrent: two swirling streaks
                    for (int k = 0; k < 2; k++)
                    {
                        var pts = new List<Vector3>();
                        for (int j = 0; j <= 6; j++)
                        {
                            float a = (k * 180f + j * 30f) * Mathf.Deg2Rad, r = 0.08f + j * 0.025f;
                            pts.Add(V(Mathf.Cos(a) * r - 0.04f, surf + 0.004f, Mathf.Sin(a) * r - 0.04f));
                        }
                        m.Stem(pts, 0.012f, 0.006f, 4, Pal.SeaLight);
                    }
                    break;
                case 3:                                                                                 // Kelp Bed: fronds rise from the water
                    foreach (var k in new[] { V(-0.2f, surf, -0.05f), V(-0.05f, surf, 0.15f), V(0.12f, surf, -0.08f), V(-0.17f, surf, 0.2f) })
                    {
                        m.Blade(k, k + V(0.03f, 0.2f, -0.02f), V(1, 0, 0), 0.05f, Pal.Kelp, Pal.GreenDark);
                        m.Blade(k + V(0.02f, 0, 0.02f), k + V(-0.04f, 0.15f, 0.03f), V(0, 0, 1), 0.04f, Pal.Kelp, Pal.GreenDark);
                    }
                    break;
                case 5:                                                                                 // Abyss: a ring on the water
                    m.Hoop(V(0f, surf + 0.003f, 0f), V(0, 1, 0), 0.18f, 0.004f, 0.02f, 16, Pal.SeaLight);
                    break;
                case 6:                                                                                 // Crushing Depth: dark water, rising bubbles
                    for (int i = 0; i < 4; i++)
                        m.Ball(V(-0.15f + i * 0.09f, surf, 0.05f - (i % 2) * 0.12f), V(0.016f, 0.008f, 0.016f), 6, 2, Pal.SeaLight);
                    break;
                case 7:                                                                                 // Trench: a whirlpool in the deep water
                    for (int k = 0; k < 3; k++)
                        m.Hoop(V(-0.03f, surf + 0.003f, -0.03f), V(0, 1, 0), 0.07f + k * 0.075f, 0.004f, 0.018f, 16, k == 0 ? Pal.Teal : Pal.SeaMid);
                    m.Disc(V(-0.03f, surf + 0.004f, -0.03f), 0.05f, 10, Pal.Ink, 0.002f);
                    m.Blade(V(-0.25f, surf, 0.2f), V(-0.22f, surf + 0.2f, 0.18f), V(1, 0, 0), 0.05f, Pal.Kelp, Pal.GreenDark);
                    m.Blade(V(0.2f, surf, -0.24f), V(0.24f, surf + 0.16f, -0.26f), V(1, 0, 0), 0.045f, Pal.Kelp, Pal.GreenDark);
                    break;
            }
            return m;
        }

        // A cluster of salt crystals: squat white prisms, or sharper spikes for Salt Sting.
        static void Salt(Lp m, Vector3 c, int n, int seed, bool sharp)
        {
            var rng = new System.Random(seed);
            for (int i = 0; i < n; i++)
            {
                Vector3 p = c + V(((float)rng.NextDouble() - 0.5f) * 0.07f, 0, ((float)rng.NextDouble() - 0.5f) * 0.04f);
                float r = 0.012f + (float)rng.NextDouble() * 0.01f;
                if (sharp)
                    m.Spike(p, V(((float)rng.NextDouble() - 0.5f) * 0.6f, 1, ((float)rng.NextDouble() - 0.5f) * 0.4f), r * 1.2f, 0.05f + (float)rng.NextDouble() * 0.04f, i % 2 == 0 ? Pal.White : Pal.Bubble, 4);
                else
                    m.Prism(p, r * 1.3f, r, r * 1.6f, 4, Pal.White, Pal.Bubble, -1, (float)rng.NextDouble() * 90f);
            }
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
