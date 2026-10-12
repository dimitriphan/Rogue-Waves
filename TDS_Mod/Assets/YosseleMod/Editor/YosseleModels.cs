// Yossele asset generator: every tower model (four buildings and their upgrades), the Growth props, the ground vines,
// the aura fruit, the spore bomb and seed, and the meshes of the debuff effects. TEMPORARY ART.
// Built sleek and low-poly like Sno Sno's and Buster's towers (smooth solids, domes, tubes), never voxels. Yossele's motif
// runs through the kit on the top and front faces: clay with round holes (its mouth), root feet into the ground, moss and
// leaves. Upgrades keep the base silhouette and add one clear feature.
// Units: 1 = one tile; y measured up from the ground; the front faces -z. Footprint |x|,|z| <= 0.42.
// Moving parts (docs/03 4a, Argo's custom aim): the Mushroom Cannon's "Turret" (barrel, stem and cap) is built in tower
// space around CannonPivot; its mouth points local -z. Every cannon model has the same part name and pivot.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace YosseleBuild
{
    public class ModelDef
    {
        public string Name;
        public string Group;            // Cannon, Dandelion, Henge, Pachira, Growth, Extras, Debuff, Projectiles
        public bool Projectile;
        public Func<Lp> Build;
        public Func<Lp> Part;           // optional moving part, built in tower space around PartPivot
        public string PartName;         // "Turret"
        public Vector3 PartPivot;       // tower space, y up from the ground
        public Func<Px> Flat;           // a flat ground tile drawn with this pixel texture instead of a mesh (vines)
    }

    public static class YosseleModels
    {
        static Vector3 V(float x, float y, float z) { return new Vector3(x, y, z); }

        public static readonly Vector3 CannonPivot = new Vector3(0f, 0.4f, 0f);

        public const string Cannon = "Tower_Yosselecannon", Dandelion = "Tower_Yosseledandelion", Henge = "Tower_Yosselehenge", Pachira = "Tower_Yosselepachira";
        public static readonly string[] CannonKeys = { "", "Sticky2", "Farout2", "Viral3", "Deathcap3", "Finish4", "Hallucinogen4" };
        public static readonly string[] DandelionKeys = { "", "Packets2", "Demand2", "Picky3", "Weeds3", "Storm4" };
        public static readonly string[] HengeKeys = { "", "Verdant2", "Blockade2", "Remedy3", "Carnivore3", "Spaghetti4" };
        public static readonly string[] PachiraKeys = { "", "Focused2", "Sustaining2", "Grafted3", "Selfish3", "Connected4", "Harvest4" };

        static string Full(string b, string k) { return k == "" ? b : b + "_" + k; }

        public static List<ModelDef> All()
        {
            var l = new List<ModelDef>();
            void Add(string group, string name, Func<Lp> f, bool proj = false) { l.Add(new ModelDef { Group = group, Name = name, Build = f, Projectile = proj }); }

            foreach (var k in CannonKeys)
            {
                string key = k;
                l.Add(new ModelDef { Group = "Cannon", Name = Full(Cannon, key), Build = () => CannonBase(key), Part = () => CannonTurret(key), PartName = "Turret", PartPivot = CannonPivot });
            }
            foreach (var k in DandelionKeys) { string key = k; Add("Dandelion", Full(Dandelion, key), () => DandelionTree(key)); }
            // The Stonehenge is built with its front (where the vines creep out) on +z, all in one part, "Facing"; the game
            // turns the whole model by the placement orientation (Entity_Master_Client), so nothing turns the part.
            foreach (var k in HengeKeys)
            {
                string key = k;
                l.Add(new ModelDef { Group = "Henge", Name = Full(Henge, key), Build = () => new Lp(), Part = () => HengeFacing(key), PartName = "Facing", PartPivot = Vector3.zero });
            }
            foreach (var k in PachiraKeys) { string key = k; Add("Pachira", Full(Pachira, key), () => Sapling(key)); }
            for (int s = 1; s <= 10; s++) { int st = s; Add("Growth", "Yossele_Growth" + st, () => Growth(st)); }
            l.Add(new ModelDef { Group = "Extras", Name = "Yossele_Vine", Build = () => new Lp(), Flat = VineGround });
            Add("Extras", "Yossele_Fruit", () => Fruit(new[] { Pal.FruitRed, Pal.FruitBlue, Pal.FruitYellow }));
            Add("Extras", "Yossele_Fruit_Red", () => Fruit(new[] { Pal.FruitRed }));
            Add("Extras", "Yossele_Fruit_Blue", () => Fruit(new[] { Pal.FruitBlue }));
            Add("Extras", "Yossele_Fruit_Yellow", () => Fruit(new[] { Pal.FruitYellow }));
            Add("Debuff", "Yossele_Toadstool", Toadstool);
            Add("Debuff", "Yossele_Swirl", Swirl);
            Add("Debuff", "Yossele_Tangle", Tangle);
            Add("Debuff", "Yossele_Blockade", BlockadeRing);
            Add("Projectiles", "Yossele_Sporebomb", Sporebomb, true);
            Add("Projectiles", "Yossele_Seed", Seed, true);
            return l;
        }

        // ---------------------------------------------------------------- shared pieces

        // Yossele's mouth: a round hole in clay - a darker rim and the dark inside, facing 'n'.
        static void Hole(Lp m, Vector3 c, Vector3 n, float r, int rim = Pal.Clay6)
        {
            n.Normalize();
            m.Tube(c - n * 0.006f, c + n * 0.01f, r * 1.32f, r * 1.32f, 12, rim, rim);
            m.Tube(c + n * 0.01f, c + n * 0.016f, r, r, 12, Pal.Hole, Pal.Hole);
        }

        // Root feet: n tapering roots from the trunk (radius r0 at height y0) arching out into the ground at 'reach'.
        static void Roots(Lp m, int n, float r0, float y0, float reach, float startDeg, int col, float thick = 0.045f, Vector3 centre = default(Vector3))
        {
            for (int i = 0; i < n; i++)
            {
                float a = (startDeg + i * 360f / n) * Mathf.Deg2Rad;
                Vector3 d = V(Mathf.Sin(a), 0f, -Mathf.Cos(a));
                var pts = new List<Vector3>
                {
                    centre + d * r0 + V(0, y0, 0),
                    centre + d * Mathf.Lerp(r0, reach, 0.45f) + V(0, y0 * 0.55f, 0),
                    centre + d * Mathf.Lerp(r0, reach, 0.8f) + V(0, y0 * 0.15f, 0),
                    centre + d * reach + V(0, -0.01f, 0),
                };
                m.Stem(pts, thick, thick * 0.35f, 6, col);
            }
        }

        // A tuft of moss: a low faceted dome.
        static void Moss(Lp m, Vector3 c, float r, float h, float yaw = 0f)
        {
            m.Dome(c, V(r, h, r * 0.9f), 7, 2, Pal.Moss1, Pal.Moss0, 0.5f, -1, yaw);
        }

        // A little two-leaf sprout (Yossele's own).
        static void Sprout(Lp m, Vector3 b, float h, float leaf, float yaw)
        {
            m.Stem(new[] { b, b + V(0.005f, h * 0.6f, 0), b + V(0.012f, h, 0) }, 0.016f, 0.012f, 4, Pal.Leaf2);
            Vector3 top = b + V(0.012f, h, 0);
            Vector3 d = Quaternion.Euler(0, yaw, 0) * Vector3.right;
            m.Leaf(top, d + Vector3.up * 0.6f, leaf, leaf * 0.55f, Pal.Sprout, Pal.SproutDark, 0.2f, 0.25f);
            m.Leaf(top, -d + Vector3.up * 0.6f, leaf * 0.85f, leaf * 0.5f, Pal.Leaf, Pal.SproutDark, 0.2f, 0.25f);
        }

        // A small mushroom: stem and a spotted dome cap.
        static void Toad(Lp m, Vector3 b, float s, int cap, int capTop, bool spots = true)
        {
            m.Prism(b, 0.03f * s, 0.025f * s, 0.09f * s, 6, Pal.Stem, Pal.Stem);
            m.Dome(b + V(0, 0.085f * s, 0), V(0.075f * s, 0.06f * s, 0.075f * s), 8, 2, cap, capTop, 0.5f, Pal.Gill);
            if (spots)
                for (int i = 0; i < 3; i++)
                {
                    float a = (40f + i * 120f) * Mathf.Deg2Rad;
                    Spot(m, b + V(0, 0.085f * s, 0), V(0.075f * s, 0.06f * s, 0.075f * s), 0.9f, a, 0.016f * s, Pal.Spot);
                }
        }

        // A flat spot on a dome or ball surface at polar angle th (from the top) and azimuth ph.
        static void Spot(Lp m, Vector3 c, Vector3 r, float th, float ph, float size, int col)
        {
            Vector3 n = V(Mathf.Sin(th) * Mathf.Cos(ph), Mathf.Cos(th), Mathf.Sin(th) * Mathf.Sin(ph));
            Vector3 p = c + V(n.x * r.x, n.y * r.y, n.z * r.z);
            Vector3 nn = V(n.x / r.x, n.y / r.y, n.z / r.z).normalized;
            m.Push(p, Quaternion.FromToRotation(Vector3.up, nn), Vector3.one);
            m.Ball(Vector3.zero, V(size, size * 0.35f, size), 6, 3, col);
            m.Pop();
        }

        // A palmate leaf cluster (pachira): 'n' leaves radiating from a point, slightly drooping.
        static void Palmate(Lp m, Vector3 c, int n, float len, float yaw, int col, int back, float tilt = 0.25f)
        {
            for (int i = 0; i < n; i++)
            {
                float a = (yaw + i * 360f / n) * Mathf.Deg2Rad;
                m.Leaf(c, V(Mathf.Cos(a), tilt, Mathf.Sin(a)), len, len * 0.5f, col, back, 0.2f, 0.3f);
            }
        }

        // A vine lying on (or climbing) something, with leaves along it.
        static void Vine(Lp m, IList<Vector3> pts, float r, int leafEvery, float leaf, int seed)
        {
            m.Stem(pts, r, r * 0.6f, 5, Pal.Vine);
            var rnd = new System.Random(seed);
            for (int i = 1; i < pts.Count; i += leafEvery)
            {
                Vector3 dir = pts[i] - pts[i - 1];
                Vector3 side = Vector3.Cross(Vector3.up, dir).normalized;
                if (side.sqrMagnitude < 0.01f) side = Vector3.right;
                float s = (i / leafEvery) % 2 == 0 ? 1f : -1f;
                m.Leaf(pts[i], side * s + dir.normalized * 0.3f + Vector3.up * 0.15f, leaf * (0.85f + 0.3f * (float)rnd.NextDouble()), leaf * 0.55f,
                       (i % 3 == 0) ? Pal.Leaf2 : Pal.Leaf, Pal.LeafDark, 0.15f, 0.15f);
            }
        }

        // A wiggly path from a to b at height y.
        static List<Vector3> Wiggle(Vector3 a, Vector3 b, int steps, float amp, float phase)
        {
            var pts = new List<Vector3>();
            Vector3 d = b - a, side = Vector3.Cross(Vector3.up, d).normalized;
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                pts.Add(Vector3.Lerp(a, b, t) + side * Mathf.Sin(t * Mathf.PI * 2f + phase) * amp * Mathf.Sin(t * Mathf.PI));
            }
            return pts;
        }

        // A ball coloured in longitude bands (the aura fruit's three colours).
        static void BandBall(Lp m, Vector3 c, Vector3 r, int lon, int lat, int[] cols)
        {
            Func<int, int, Vector3> P = (i, j) =>
            {
                float th = Mathf.PI * j / lat, ph = Mathf.PI * 2f * i / lon;
                return c + V(Mathf.Sin(th) * Mathf.Cos(ph) * r.x, Mathf.Cos(th) * r.y, Mathf.Sin(th) * Mathf.Sin(ph) * r.z);
            };
            for (int j = 0; j < lat; j++)
                for (int i = 0; i < lon; i++)
                {
                    int k = cols[(i * cols.Length) / lon];
                    Vector3 a = P(i, j), b = P(i + 1, j), d = P(i, j + 1), e = P(i + 1, j + 1);
                    if (j == 0) m.Tri(a, d, e, k, c);
                    else if (j == lat - 1) m.Tri(a, b, d, k, c);
                    else m.Quad(a, b, e, d, k, c);
                }
        }

        // ---------------------------------------------------------------- 1. Mushroom Cannon

        // The clay mortar's tub on root feet, with Yossele's hole on the front. The Turret sits on top.
        static Lp CannonBase(string up)
        {
            var m = new Lp();
            Roots(m, 5, 0.2f, 0.13f, 0.4f, 36f, Pal.Clay4);
            m.Prism(V(0, 0, 0), 0.29f, 0.25f, 0.34f, 12, Pal.Clay, Pal.Clay2, -1, 15f);
            m.Tube(V(0, 0.31f, 0), V(0, 0.37f, 0), 0.265f, 0.26f, 12, Pal.Clay4, Pal.Clay1, -1, 15f);
            Hole(m, V(0, 0.17f, -0.272f), V(0, 0.12f, -1f), 0.065f);
            Hole(m, V(0.17f, 0.1f, -0.22f), V(0.62f, 0.1f, -0.78f), 0.03f);
            Hole(m, V(-0.2f, 0.23f, -0.17f), V(-0.76f, 0.1f, -0.64f), 0.025f);
            Moss(m, V(-0.17f, 0.34f, 0.17f), 0.09f, 0.035f);
            Moss(m, V(0.24f, 0.0f, 0.12f), 0.08f, 0.05f);
            if (up == "Viral3")
            {
                // little toadstools sprouting from the tub and the ground: the outbreak
                Toad(m, V(0.2f, 0.33f, -0.12f), 0.9f, Pal.Cap1, Pal.Cap0);
                Toad(m, V(-0.31f, 0.0f, -0.22f), 1.0f, Pal.Cap1, Pal.Cap0);
                Toad(m, V(0.3f, 0.0f, 0.24f), 0.8f, Pal.Cap1, Pal.Cap0);
                Toad(m, V(-0.25f, 0.0f, 0.3f), 0.7f, Pal.Cap1, Pal.Cap0);
            }
            return m;
        }

        // The turning part: a collar, the mortar barrel (mouth toward -z, angled up) and the mushroom on top of it.
        static Lp CannonTurret(string up)
        {
            var m = new Lp();
            m.Prism(V(0, 0.37f, 0), 0.2f, 0.18f, 0.07f, 10, Pal.Clay4, Pal.Clay2);
            // barrel
            bool far = up == "Farout2";
            Vector3 b0 = V(0, 0.45f, 0.05f), b1 = far ? V(0, 0.66f, -0.27f) : V(0, 0.62f, -0.2f);
            Vector3 dir = (b1 - b0).normalized;
            m.Tube(b0, b1, 0.105f, 0.115f, 10, Pal.Clay, -1, -1, 18f);
            if (far) m.Tube(Vector3.Lerp(b0, b1, 0.5f), Vector3.Lerp(b0, b1, 0.5f) + dir * 0.035f, 0.122f, 0.122f, 10, Pal.Clay1, Pal.Clay1, Pal.Clay1, 18f);
            m.Tube(b1, b1 + dir * 0.03f, 0.135f, 0.135f, 10, Pal.Clay6, Pal.Clay6, Pal.Clay6, 18f);
            m.Tube(b1 + dir * 0.03f, b1 + dir * 0.034f, 0.095f, 0.095f, 10, Pal.Hole, Pal.Hole, -1, 18f);
            // the mushroom: stem and cap, set back so the mouth shows under the front rim
            int cap = Pal.Cap1, capTop = Pal.Cap0, spot = Pal.Spot;
            if (up == "Deathcap3") { cap = Pal.Moss1; capTop = Pal.Deathcap; }
            if (up == "Hallucinogen4") { cap = Pal.PurpleDark; capTop = Pal.Purple; spot = Pal.Floret; }
            m.Prism(V(0, 0.42f, 0.1f), 0.085f, 0.07f, 0.46f, 8, Pal.Stem, Pal.Stem);
            if (up == "Deathcap3")
                m.Tube(V(0, 0.7f, 0.1f), V(0, 0.66f, 0.1f), 0.08f, 0.14f, 10, Pal.Spot, -1, -1);   // the death cap's skirt
            Vector3 cc = V(0, 0.84f, 0.05f), cr = V(0.355f, 0.3f, 0.355f);
            m.Dome(cc, cr, 12, 4, cap, capTop, 0.55f, Pal.Gill);
            if (up != "Deathcap3")
            {
                float[,] spots = { { 0.35f, 250f }, { 0.75f, 270f }, { 0.8f, 225f }, { 0.8f, 315f }, { 0.9f, 180f }, { 0.9f, 0f }, { 0.7f, 120f }, { 0.65f, 60f }, { 1.15f, 250f }, { 1.15f, 290f } };
                for (int i = 0; i < spots.GetLength(0); i++)
                    Spot(m, cc, cr, spots[i, 0], spots[i, 1] * Mathf.Deg2Rad, i == 0 ? 0.07f : 0.05f, spot);
            }
            if (up == "Sticky2")
                // amber goo dripping from the cap's front rim
                for (int i = 0; i < 7; i++)
                {
                    float a = (180f + i * 30f) * Mathf.Deg2Rad;
                    Vector3 p = cc + V(Mathf.Cos(a) * 0.33f, -0.01f, Mathf.Sin(a) * 0.33f);
                    float len = i % 2 == 0 ? 0.12f : 0.08f;
                    m.Ball(p + V(0, -len * 0.5f, 0), V(0.035f, len * 0.6f, 0.035f), 6, 4, Pal.Floret2);
                    m.Ball(p + V(0, -len, 0), V(0.042f, 0.042f, 0.042f), 6, 3, Pal.Floret2);
                }
            if (up == "Finish4")
                // swollen spore sacs on the cap, ready to burst
                for (int i = 0; i < 4; i++)
                {
                    float a = (45f + i * 90f) * Mathf.Deg2Rad;
                    Vector3 p = cc + V(Mathf.Cos(a) * 0.15f, cr.y * 0.88f, Mathf.Sin(a) * 0.15f);
                    m.Ball(p, V(0.07f, 0.08f, 0.07f), 8, 5, Pal.Floret2, Pal.Floret, 0.5f);
                    m.Spike(p + V(0, 0.07f, 0), Vector3.up, 0.02f, 0.05f, Pal.Cap2, 5);
                }
            if (up == "Hallucinogen4")
                // a ring of little purple caps round the stem: the trip spreads
                for (int i = 0; i < 3; i++)
                {
                    float a = (60f + i * 120f) * Mathf.Deg2Rad;
                    Toad(m, cc + V(Mathf.Cos(a) * 0.17f, cr.y * 0.85f, Mathf.Sin(a) * 0.17f), 0.75f, Pal.PurpleDark, Pal.Purple, false);
                }
            return m;
        }

        // ---------------------------------------------------------------- 2. Primordial Dandelion Tree

        static Lp DandelionTree(string up)
        {
            var m = new Lp();
            Roots(m, 5, 0.18f, 0.1f, 0.39f, 20f, Pal.Clay4, 0.05f);
            // squat clay trunk with the hole on the front
            m.Prism(V(0, 0, 0), 0.23f, 0.16f, 0.34f, 10, Pal.Clay, Pal.Clay2, -1, 18f);
            m.Prism(V(0, 0.34f, 0), 0.16f, 0.08f, 0.1f, 10, Pal.Clay4, Pal.Clay2, -1, 18f);
            Hole(m, V(0, 0.18f, -0.2f), V(0, 0.18f, -1f), 0.062f);
            Hole(m, V(-0.15f, 0.27f, -0.08f), V(-0.85f, 0.25f, -0.45f), 0.028f);
            Moss(m, V(0.14f, 0.0f, 0.14f), 0.1f, 0.06f);
            Moss(m, V(-0.08f, 0.37f, 0.06f), 0.07f, 0.035f);
            // dandelion leaves on the ground, jagged
            foreach (float a in new[] { 130f, 200f, 300f, 60f })
            {
                float r = a * Mathf.Deg2Rad;
                Vector3 d = V(Mathf.Cos(r), 0.12f, Mathf.Sin(r));
                m.Leaf(V(Mathf.Cos(r) * 0.15f, 0.03f, Mathf.Sin(r) * 0.15f), d, 0.22f, 0.085f, Pal.Leaf, Pal.LeafDark, 0.2f, 0.1f);
            }
            // stalk, sepals and the puff ball
            bool storm = up == "Storm4";
            float hr = storm ? 0.32f : 0.29f;
            Vector3 hc = V(0.01f, storm ? 1.02f : 0.98f, 0f);
            m.Stem(new[] { V(0, 0.42f, 0), V(0.012f, 0.56f, 0), hc - V(0, hr * 0.7f, 0) }, 0.04f, 0.032f, 6, Pal.Stalk);
            for (int i = 0; i < 7; i++)
            {
                float a = i * 360f / 7f * Mathf.Deg2Rad;
                m.Leaf(hc - V(0, hr * 0.62f, 0), V(Mathf.Cos(a), -0.7f, Mathf.Sin(a)), 0.11f, 0.05f, Pal.Leaf2, Pal.LeafDark, 0.2f, 0.1f);
            }
            m.Ball(hc, V(hr, hr * 0.96f, hr), 12, 8, Pal.Puff, Pal.White, 0.35f, Pal.Puff2, -0.55f);
            // a few seed tufts poking out of the puff
            for (int i = 0; i < 9; i++)
            {
                float th = 0.35f + 0.9f * ((i * 0.618f) % 1f), ph = i * 2.4f + 0.3f;
                Vector3 n = V(Mathf.Sin(th) * Mathf.Cos(ph), Mathf.Cos(th), Mathf.Sin(th) * Mathf.Sin(ph));
                m.Spike(hc + n * hr * 0.95f, n, 0.02f, 0.05f, Pal.White, 5);
            }
            if (up == "Picky3")
                // a crown of golden seed tips round the puff: picked, sharpened seeds
                for (int i = 0; i < 12; i++)
                {
                    float ph = i * 30f * Mathf.Deg2Rad, th = i % 2 == 0 ? 0.75f : 1.05f;
                    Vector3 n = V(Mathf.Sin(th) * Mathf.Cos(ph), Mathf.Cos(th), Mathf.Sin(th) * Mathf.Sin(ph));
                    m.Spike(hc + n * hr * 0.92f, n, 0.03f, 0.1f, Pal.Floret2, 5);
                }
            if (up == "Packets2")
            {
                // seed packets leaning on the trunk
                foreach (var q in new[] { new[] { 0.2f, -0.24f, -30f, -14f }, new[] { -0.24f, -0.2f, 35f, -12f } })
                {
                    m.Push(V(q[0], 0f, q[1]), Quaternion.Euler(q[3], q[2], 0f), Vector3.one);
                    m.Block(V(0, 0, 0), V(0.15f, 0.21f, 0.03f), 0.012f, Pal.Puff, Pal.Puff2);
                    m.Block(V(0, 0.06f, -0.003f), V(0.154f, 0.05f, 0.032f), 0.006f, Pal.Floret, Pal.Floret);
                    m.Ball(V(0, 0.15f, -0.018f), V(0.03f, 0.03f, 0.008f), 6, 2, Pal.Leaf);
                    m.Pop();
                }
            }
            if (up == "Demand2")
            {
                // a second, smaller puff budding on a side stalk
                Vector3 s = V(-0.25f, 0.66f, -0.14f);
                m.Stem(new[] { V(-0.05f, 0.4f, -0.02f), V(-0.17f, 0.48f, -0.08f), s - V(0, 0.1f, 0) }, 0.028f, 0.022f, 5, Pal.Stalk);
                m.Ball(s, V(0.13f, 0.125f, 0.13f), 10, 6, Pal.Puff, Pal.White, 0.35f, Pal.Puff2);
                for (int i = 0; i < 5; i++) { float a = i * 72f * Mathf.Deg2Rad; m.Leaf(s - V(0, 0.09f, 0), V(Mathf.Cos(a), -0.7f, Mathf.Sin(a)), 0.07f, 0.035f, Pal.Leaf2, Pal.LeafDark); }
            }
            if (up == "Weeds3")
            {
                // weeds all round: more jagged leaves and yellow dandelion flowers
                foreach (float a in new[] { 20f, 95f, 165f, 240f, 330f })
                {
                    float r = a * Mathf.Deg2Rad;
                    m.Leaf(V(Mathf.Cos(r) * 0.2f, 0.02f, Mathf.Sin(r) * 0.2f), V(Mathf.Cos(r), 0.1f, Mathf.Sin(r)), 0.2f, 0.07f, Pal.Leaf2, Pal.LeafDark, 0.2f, 0.05f);
                }
                foreach (var q in new[] { V(-0.3f, 0, -0.18f), V(0.32f, 0, -0.08f), V(-0.12f, 0, 0.32f) })
                {
                    m.Stem(new[] { q, q + V(0, 0.12f, 0) }, 0.012f, 0.01f, 4, Pal.Stalk2);
                    m.Flower(q + V(0, 0.13f, 0), V(0, 1f, -0.4f), 0.06f, Pal.Floret, Pal.Floret2, 8);
                }
            }
            if (storm)
                // a ring of seeds whirling round the head
                for (int i = 0; i < 14; i++)
                {
                    float a = i * 360f / 14f * Mathf.Deg2Rad;
                    Vector3 p = hc + Quaternion.Euler(18f, 0, 12f) * V(Mathf.Cos(a) * 0.36f, 0, Mathf.Sin(a) * 0.36f);
                    m.Ball(p, V(0.026f, 0.026f, 0.026f), 5, 3, Pal.SeedBrown);
                    m.Dome(p + V(0, 0.02f, 0), V(0.035f, 0.04f, 0.035f), 6, 2, Pal.White);
                }
            return m;
        }

        // ---------------------------------------------------------------- 3. Overgrown Stonehenge

        // Drawn front-on (-z) for readability, then turned half round so the front lands on +z inside Facing.
        static Lp HengeFacing(string up)
        {
            var m = new Lp();
            m.Push(Vector3.zero, 180f);
            Stonehenge(up, m);
            m.Pop();
            return m;
        }

        static void Stonehenge(string up, Lp m)
        {
            // three standing stones in a short arc at the back, the middle one with the round hole
            var stones = new[] { V(-0.26f, 0, 0.07f), V(0, 0, 0.15f), V(0.26f, 0, 0.07f) };
            float[] yaw = { 28f, 0f, -28f };
            float[] h = { 0.74f, 0.8f, 0.72f };
            for (int i = 0; i < 3; i++)
            {
                m.Push(stones[i], yaw[i]);
                m.Block(V(0, 0, 0), V(0.22f, h[i], 0.16f), 0.04f, i == 1 ? Pal.Stone1 : Pal.Stone2, Pal.Stone0, Pal.Stone1);
                m.Pop();
            }
            Hole(m, V(0, 0.46f, 0.15f - 0.082f), V(0, 0, -1f), 0.07f, Pal.Stone4);
            // lintels: left-middle and middle-right
            for (int i = 0; i < 2; i++)
            {
                Vector3 a = stones[i] + V(0, h[i], 0), b = stones[i + 1] + V(0, h[i + 1], 0);
                Vector3 c = (a + b) * 0.5f;
                float y = Mathf.Max(a.y, b.y);
                float ang = -Mathf.Atan2(b.z - a.z, b.x - a.x) * Mathf.Rad2Deg;
                m.Push(V(c.x, y - 0.02f, c.z), ang);
                m.Block(V(0, 0, 0), V(0.34f, 0.1f, 0.13f), 0.03f, Pal.Stone2, Pal.Stone0, Pal.Stone1);
                m.Pop();
                Moss(m, V(c.x, y + 0.078f, c.z), 0.11f, 0.04f, ang);
            }
            Moss(m, stones[0] + V(-0.02f, h[0], 0), 0.07f, 0.035f);
            Moss(m, stones[2] + V(0.02f, h[2], 0), 0.07f, 0.035f);
            Moss(m, V(0.18f, 0.0f, 0.22f), 0.11f, 0.05f);
            Moss(m, V(-0.32f, 0.0f, -0.05f), 0.08f, 0.04f);
            // vines climb the stones' fronts and trail out of the front (-z), the way the henge faces
            Vine(m, new[] { V(-0.25f, 0.0f, -0.025f), V(-0.26f, 0.2f, -0.03f), V(-0.23f, 0.36f, -0.03f), V(-0.25f, 0.5f, -0.025f) }, 0.024f, 1, 0.09f, 1);
            Vine(m, new[] { V(0.28f, 0.0f, -0.025f), V(0.26f, 0.16f, -0.03f), V(0.29f, 0.3f, -0.03f) }, 0.024f, 1, 0.09f, 2);
            Vine(m, new[] { V(-0.06f, 0.0f, 0.06f), V(-0.07f, 0.12f, 0.06f), V(-0.04f, 0.26f, 0.06f), V(-0.08f, 0.36f, 0.06f) }, 0.022f, 1, 0.08f, 7);
            Vine(m, Wiggle(V(-0.2f, 0.02f, -0.02f), V(-0.25f, 0.02f, -0.36f), 6, 0.04f, 0f), 0.027f, 1, 0.095f, 3);
            Vine(m, Wiggle(V(0.0f, 0.02f, 0.05f), V(0.03f, 0.02f, -0.36f), 7, 0.05f, 1.5f), 0.027f, 1, 0.095f, 4);
            Vine(m, Wiggle(V(0.22f, 0.02f, -0.02f), V(0.27f, 0.02f, -0.35f), 6, 0.04f, 3f), 0.027f, 1, 0.095f, 5);
            if (up == "Verdant2")
            {
                // vines all round: to the sides and behind too
                Vine(m, Wiggle(V(-0.3f, 0.02f, 0.05f), V(-0.31f, 0.02f, 0.28f), 5, 0.03f, 0f), 0.022f, 1, 0.075f, 11);
                Vine(m, Wiggle(V(0.3f, 0.02f, 0.05f), V(0.31f, 0.02f, 0.28f), 5, 0.03f, 1f), 0.022f, 1, 0.075f, 12);
                Vine(m, Wiggle(V(0.0f, 0.02f, 0.22f), V(-0.05f, 0.02f, 0.33f), 4, 0.03f, 2f), 0.022f, 1, 0.075f, 13);
                Vine(m, Wiggle(V(-0.3f, 0.02f, -0.05f), V(-0.33f, 0.02f, -0.2f), 4, 0.03f, 2f), 0.022f, 1, 0.075f, 14);
                Vine(m, Wiggle(V(0.3f, 0.02f, -0.05f), V(0.33f, 0.02f, -0.22f), 4, 0.03f, 0.5f), 0.022f, 1, 0.075f, 15);
            }
            if (up == "Blockade2")
            {
                // a woven vine barrier, a low hedge in front of the stones
                for (int k = 0; k < 2; k++)
                {
                    var pts = new List<Vector3>();
                    for (int i = 0; i <= 12; i++)
                    {
                        float t = i / 12f, a = Mathf.Lerp(-150f, -30f, t) * Mathf.Deg2Rad;
                        pts.Add(V(Mathf.Cos(a) * 0.36f, 0.1f + Mathf.Sin(t * Mathf.PI * 4f + k * Mathf.PI) * 0.06f, Mathf.Sin(a) * 0.3f + 0.02f));
                    }
                    Vine(m, pts, 0.036f, 1, 0.1f, 20 + k);
                }
            }
            if (up == "Remedy3")
                // healing blossoms on the vines and stones
                foreach (var q in new[] { V(-0.25f, 0.42f, -0.07f), V(0.28f, 0.25f, -0.07f), V(-0.22f, 0.05f, -0.28f), V(0.05f, 0.05f, -0.24f), V(0.27f, 0.05f, -0.3f), V(0.0f, 0.9f, 0.15f), V(-0.27f, 0.79f, 0.07f) })
                    m.Flower(q, V(0, 0.8f, -0.6f), 0.1f, Pal.White, Pal.Floret, 5);
            if (up == "Carnivore3")
            {
                // a flytrap growing from the vines at the front
                Vector3 b = V(-0.1f, 0.0f, -0.22f), top = V(-0.12f, 0.24f, -0.27f);
                m.Stem(new[] { b, V(-0.1f, 0.12f, -0.25f), top }, 0.034f, 0.026f, 5, Pal.Vine);
                for (int s = -1; s <= 1; s += 2)
                {
                    Vector3 d = V(s * 0.6f, 0.75f, -0.35f).normalized;
                    m.Leaf(top, d, 0.2f, 0.15f, Pal.Cap1, Pal.Leaf2, 0.5f, 0.05f);
                    Vector3 side = V(s, 0, 0);
                    for (int i = 0; i < 4; i++)
                        m.Spike(top + d * (0.06f + i * 0.04f) + side * 0.06f, side + Vector3.up * 0.6f, 0.01f, 0.06f, Pal.Puff, 3);
                }
            }
            if (up == "Spaghetti4")
                // tangled roots looping out of the ground all over the front
                foreach (var q in new[] { new[] { -0.26f, -0.2f, 0.11f, 30f }, new[] { -0.04f, -0.3f, 0.1f, -10f }, new[] { 0.2f, -0.2f, 0.12f, 60f }, new[] { 0.05f, -0.12f, 0.09f, 100f }, new[] { -0.2f, -0.32f, 0.07f, 80f } })
                {
                    var pts = new List<Vector3>();
                    float r = q[2], a0 = q[3] * Mathf.Deg2Rad;
                    for (int i = 0; i <= 8; i++)
                    {
                        float t = i / 8f * Mathf.PI;
                        pts.Add(V(q[0] + Mathf.Cos(a0) * Mathf.Cos(t) * r, Mathf.Sin(t) * r * 1.1f - 0.01f, q[1] + Mathf.Sin(a0) * Mathf.Cos(t) * r));
                    }
                    m.Stem(pts, 0.036f, 0.026f, 6, Pal.Bark2);
                }
        }

        // ---------------------------------------------------------------- 4. Pachira Sapling

        static Lp Sapling(string up)
        {
            var m = new Lp();
            // root feet out of the pot's base into the ground
            Roots(m, 5, 0.2f, 0.07f, up == "Connected4" ? 0.36f : 0.38f, 0f, Pal.Bark2, 0.04f);
            // clay pot with Yossele's holes, soil on top
            m.Prism(V(0, 0, 0), 0.2f, 0.26f, 0.29f, 12, Pal.Clay, Pal.Clay2, -1, 15f);
            m.Tube(V(0, 0.27f, 0), V(0, 0.34f, 0), 0.28f, 0.28f, 12, Pal.Clay1, Pal.Soil, -1, 15f);
            Hole(m, V(0, 0.15f, -0.235f), V(0, -0.18f, -1f), 0.05f);
            Hole(m, V(-0.13f, 0.13f, -0.19f), V(-0.55f, -0.18f, -0.82f), 0.025f);
            Hole(m, V(0.13f, 0.13f, -0.19f), V(0.55f, -0.18f, -0.82f), 0.025f);
            // braided trunk: three strands twisting up
            for (int k = 0; k < 3; k++)
            {
                var pts = new List<Vector3>();
                for (int i = 0; i <= 9; i++)
                {
                    float t = i / 9f, a = (k * 120f + t * 400f) * Mathf.Deg2Rad, r = 0.045f * (1f - 0.35f * t);
                    pts.Add(V(Mathf.Cos(a) * r, 0.32f + t * 0.56f, Mathf.Sin(a) * r));
                }
                m.Stem(pts, 0.036f, 0.026f, 6, k == 1 ? Pal.Bark1 : Pal.Bark0);
            }
            // branches and the palmate leaf clusters
            int[] cols = { Pal.Leaf, Pal.Leaf2, Pal.Leaf, Pal.Leaf2 };
            if (up == "Grafted3") cols = new[] { Pal.FruitRed, Pal.Leaf2, Pal.FruitBlue, Pal.FruitYellow };
            for (int i = 0; i < 4; i++)
            {
                float a = (45f + i * 90f) * Mathf.Deg2Rad;
                Vector3 tip = V(Mathf.Cos(a) * 0.16f, 0.96f + (i % 2) * 0.05f, Mathf.Sin(a) * 0.16f);
                m.Stem(new[] { V(0, 0.86f, 0), V(Mathf.Cos(a) * 0.08f, 0.93f, Mathf.Sin(a) * 0.08f), tip }, 0.024f, 0.018f, 5, Pal.Bark1);
                Palmate(m, tip, 6, 0.21f, i * 30f, cols[i], Pal.LeafDeep);
            }
            Palmate(m, V(0, 1.08f, 0), 6, 0.18f, 15f, Pal.Sprout, Pal.LeafDeep, 0.55f);
            if (up == "Focused2")
            {
                // one thick taproot driving into an ore nugget at the front
                m.Stem(new[] { V(0.1f, 0.06f, -0.2f), V(0.17f, 0.05f, -0.27f), V(0.23f, 0.02f, -0.32f) }, 0.05f, 0.035f, 6, Pal.Bark1);
                m.Ball(V(0.27f, 0.02f, -0.34f), V(0.075f, 0.06f, 0.07f), 6, 4, Pal.FruitYellow, Pal.Floret, 0.5f, Pal.Floret2);
            }
            if (up == "Sustaining2")
                // moss and sprouts all round the pot's rim
                for (int i = 0; i < 7; i++)
                {
                    float a = (i * 360f / 7f + 10f) * Mathf.Deg2Rad;
                    Moss(m, V(Mathf.Cos(a) * 0.26f, 0.315f, Mathf.Sin(a) * 0.26f), 0.065f, 0.04f);
                    if (i % 2 == 0) Sprout(m, V(Mathf.Cos(a) * 0.24f, 0.34f, Mathf.Sin(a) * 0.24f), 0.08f, 0.05f, i * 50f);
                }
            if (up == "Grafted3")
                // the graft's binding round the trunk
                m.Tube(V(0, 0.6f, 0), V(0, 0.66f, 0), 0.07f, 0.07f, 8, Pal.Puff2, Pal.Puff, Pal.Puff);
            if (up == "Selfish3")
                // a ring of thorns round the pot: keep away
                for (int i = 0; i < 10; i++)
                {
                    float a = (i * 36f + 18f) * Mathf.Deg2Rad;
                    Vector3 d = V(Mathf.Cos(a), 0.15f, Mathf.Sin(a));
                    m.Spike(V(Mathf.Cos(a) * 0.255f, 0.16f + (i % 2) * 0.07f, Mathf.Sin(a) * 0.255f), d, 0.03f, 0.13f, Pal.Stem2, 5);
                }
            if (up == "Connected4")
                // long surface roots reaching for the tile's corners, toward the buildings around it
                for (int i = 0; i < 4; i++)
                {
                    float a = (45f + i * 90f) * Mathf.Deg2Rad;
                    Vector3 d = V(Mathf.Cos(a), 0, Mathf.Sin(a));
                    m.Stem(new[] { d * 0.22f + V(0, 0.04f, 0), d * 0.34f + V(0, 0.03f, 0), d * 0.46f + V(0, 0.01f, 0), d * 0.56f }, 0.04f, 0.018f, 6, Pal.Bark2);
                    m.Ball(d * 0.4f + V(0, 0.03f, 0), V(0.04f, 0.035f, 0.04f), 6, 3, Pal.Bark1);
                }
            if (up == "Harvest4")
                // ripe aura fruit hanging in the canopy
                foreach (var q in new[] { new Vector4(-0.15f, 0.88f, -0.15f, Pal.FruitRed), new Vector4(0.17f, 0.9f, -0.12f, Pal.FruitBlue), new Vector4(0.02f, 0.86f, 0.19f, Pal.FruitYellow) })
                {
                    Vector3 c = V(q.x, q.y, q.z);
                    m.Stem(new[] { c + V(0, 0.12f, 0), c + V(0, 0.06f, 0) }, 0.01f, 0.01f, 4, Pal.Bark2);
                    m.Ball(c, V(0.065f, 0.07f, 0.065f), 8, 5, (int)q.w);
                }
            return m;
        }

        // ---------------------------------------------------------------- Growth props (a skin per stage)

        // Sprouts, small leaves and (from stage 6) tiny flowers round the tower's foot, more each stage. Low (<= 0.25),
        // inside the tile, never in the front sector so the tower's front stays clear. Stage n keeps stage n-1's pieces.
        static Lp Growth(int stage)
        {
            var m = new Lp();
            var rnd = new System.Random(17);
            // 24 slots round the foot, skipping the front sector (within 42 degrees of -z)
            // evenly round the back and sides (a = 0 is +z, 180 is -z: the front sector 138-222 stays clear), taken in a
            // spread-out order so even stage 1's pieces sit apart
            var slots = new List<Vector3>();
            int[] order = { 0, 12, 6, 18, 3, 15, 9, 21, 1, 13, 7, 19, 4, 16, 10, 22, 2, 14, 8, 20, 5, 17, 11, 23 };
            foreach (int o in order)
            {
                float a = 222f + o * (276f / 23f) + (float)(rnd.NextDouble() * 6.0 - 3.0);
                float r = 0.29f + 0.07f * (float)rnd.NextDouble();
                float ra = a * Mathf.Deg2Rad;
                Vector3 p = V(Mathf.Sin(ra) * r, 0f, Mathf.Cos(ra) * r);
                p.x = Mathf.Clamp(p.x, -0.35f, 0.35f); p.z = Mathf.Clamp(p.z, -0.35f, 0.35f);
                slots.Add(p);
            }
            int pieces = Mathf.Min(24, 2 * stage + (stage > 6 ? 2 : 0));
            for (int i = 0; i < pieces; i++)
            {
                Vector3 p = slots[i];
                Vector3 outward = V(p.x, 0, p.z).normalized;
                if (i % 3 == 1)
                {
                    // a rosette of leaves flat on the ground
                    for (int k = 0; k < 3; k++)
                    {
                        Vector3 d = Quaternion.Euler(0, k * 120f + i * 37f, 0) * outward;
                        m.Leaf(p + V(0, 0.008f, 0), d + Vector3.up * 0.25f, 0.075f + 0.003f * stage, 0.045f, k == 1 ? Pal.Leaf2 : Pal.Leaf, Pal.LeafDark, 0.2f, 0.25f);
                    }
                }
                else
                    Sprout(m, p, Mathf.Min(0.17f, 0.05f + 0.01f * stage + 0.02f * (i % 2)), 0.06f + 0.003f * stage, i * 47f);
            }
            for (int i = 0; i < Mathf.Max(0, stage - 5); i++)
            {
                Vector3 p = slots[(i * 5 + 3) % slots.Count] + V(0.03f, 0, 0.02f);
                m.Stem(new[] { p, p + V(0, 0.1f + 0.02f * i, 0) }, 0.01f, 0.008f, 4, Pal.Stalk2);
                m.Flower(p + V(0, 0.11f + 0.02f * i, 0), V(0, 1f, -0.3f), 0.04f, i % 2 == 0 ? Pal.White : Pal.Floret, i % 2 == 0 ? Pal.Floret : Pal.Floret2, 5);
            }
            return m;
        }

        // The ground the Stonehenge spreads, a flat pixel texture like Sno Sno's snow (Dimitri, 2026-10-12): 20 px a tile
        // (the sprites' 20 pixels per unit), opaque pixels only, shade on the stems' lower and right edges. One stem
        // crosses each pair of opposite edges at the same place, so a line of tiles reads as one vine.
        public static Px VineGround()
        {
            string[] rows =
            {
                "..........klvd......",
                "..........k.vd......",
                "............vdlk....",
                "...lk.......vdllk...",
                "..llk........vd.k...",
                "...k.mm.....kvd.....",
                "vv....vvv..kll.vd...",
                "ddvvvvdddvv.k..vdvvv",
                "..dddd.lkddvvvvddddd",
                "......llk..dddvd....",
                ".......k......vdlk..",
                "..............vdllk.",
                ".....m.......vd.k...",
                ".............vd.....",
                ".......kl...vd......",
                "......kll..lvd......",
                ".......k...kvdlk....",
                "....mm......vdllk...",
                "...lk.......vd.k....",
                "..llk.......vd......",
            };
            var pal = new Dictionary<char, Color32>
            {
                { 'v', Pal.Color(Pal.Vine) }, { 'd', Pal.Color(Pal.VineDark) }, { 'l', Pal.Color(Pal.Leaf) },
                { 'k', Pal.Color(Pal.LeafDark) }, { 'm', Pal.Color(Pal.Moss1) },
            };
            return Px.FromRows(rows, pal);
        }

        // An aura fruit lying on the ground: a round pod banded in its colours, a stub of stem and a leaf.
        static Lp Fruit(int[] cols)
        {
            var m = new Lp();
            Vector3 c = V(0, 0.12f, 0);
            BandBall(m, c, V(0.15f, 0.125f, 0.15f), 12, 7, cols);
            m.Stem(new[] { c + V(0, 0.11f, 0), c + V(0.01f, 0.17f, 0.01f) }, 0.015f, 0.012f, 5, Pal.Bark2);
            m.Leaf(c + V(0.01f, 0.15f, 0), V(0.7f, 0.5f, -0.4f), 0.1f, 0.05f, Pal.Leaf, Pal.LeafDark);
            return m;
        }

        // ---------------------------------------------------------------- debuff meshes (attached by the game)
        // Heights follow the template's verified numbers (Mummer 1.0: overhead marker at 1.6 in these coordinates).

        // Full spores: a little toadstool cap over the foe's head.
        static Lp Toadstool()
        {
            var m = new Lp();
            Vector3 c = V(0, 1.5f, 0), r = V(0.17f, 0.12f, 0.17f);
            m.Prism(c - V(0, 0.08f, 0), 0.045f, 0.04f, 0.09f, 6, Pal.Stem, Pal.Stem);
            m.Dome(c, r, 10, 3, Pal.Cap1, Pal.Cap0, 0.55f, Pal.Gill);
            for (int i = 0; i < 5; i++) Spot(m, c, r, i == 0 ? 0.2f : 0.95f, (i * 72f + 250f) * Mathf.Deg2Rad, 0.035f, Pal.Spot);
            return m;
        }

        // Hallucinogen: a purple swirl over the foe's head.
        static Lp Swirl()
        {
            var m = new Lp();
            var pts = new List<Vector3>();
            for (int i = 0; i <= 18; i++)
            {
                float t = i / 18f, a = t * Mathf.PI * 3.2f, r = 0.2f * (1f - 0.75f * t);
                pts.Add(V(Mathf.Cos(a) * r, 1.5f + t * 0.12f, Mathf.Sin(a) * r));
            }
            m.Stem(pts, 0.03f, 0.018f, 5, Pal.Purple);
            m.Ball(pts[pts.Count - 1], V(0.035f, 0.035f, 0.035f), 6, 3, Pal.Floret);
            return m;
        }

        // Spaghetti Roots' snare: loops of vine round the foe's feet.
        static Lp Tangle()
        {
            var m = new Lp();
            for (int k = 0; k < 3; k++)
            {
                var pts = new List<Vector3>();
                float y = 0.06f + k * 0.07f, r = 0.22f - k * 0.025f;
                for (int i = 0; i <= 14; i++)
                {
                    float a = (i / 14f * 360f + k * 40f) * Mathf.Deg2Rad;
                    pts.Add(V(Mathf.Cos(a) * r, y + Mathf.Sin(a * 2f) * 0.025f, Mathf.Sin(a) * r));
                }
                Vine(m, pts, 0.018f, 3, 0.06f, 30 + k);
            }
            return m;
        }

        // Vine Blockade: a woven ring of vines round the tower's foot.
        static Lp BlockadeRing()
        {
            var m = new Lp();
            for (int k = 0; k < 2; k++)
            {
                var pts = new List<Vector3>();
                for (int i = 0; i <= 28; i++)
                {
                    float t = i / 28f, a = t * Mathf.PI * 2f;
                    pts.Add(V(Mathf.Cos(a) * 0.42f, 0.14f + Mathf.Sin(a * 6f + k * Mathf.PI) * 0.1f, Mathf.Sin(a) * 0.42f));
                }
                Vine(m, pts, 0.03f, 2, 0.08f, 40 + k);
            }
            for (int i = 0; i < 8; i++)
            {
                float a = (i * 45f + 22.5f) * Mathf.Deg2Rad;
                m.Leaf(V(Mathf.Cos(a) * 0.42f, 0.22f, Mathf.Sin(a) * 0.42f), V(Mathf.Cos(a), 0.9f, Mathf.Sin(a)), 0.12f, 0.07f, Pal.Leaf, Pal.LeafDark);
            }
            return m;
        }

        // ---------------------------------------------------------------- projectiles (recentred by the builder)

        // The cannon's lob: a round spore bomb, olive with cream spots and a few knobs.
        static Lp Sporebomb()
        {
            var m = new Lp();
            Vector3 c = V(0, 0.5f, 0), r = V(0.095f, 0.09f, 0.095f);
            m.Ball(c, r, 10, 7, Pal.Moss2, Pal.Moss1, 0.5f, Pal.Moss3);
            for (int i = 0; i < 8; i++) Spot(m, c, r, 0.4f + (i % 3) * 0.6f, i * 0.85f, 0.025f, Pal.Spot);
            for (int i = 0; i < 5; i++)
            {
                float th = 0.6f + i * 0.45f, ph = i * 1.9f;
                Vector3 n = V(Mathf.Sin(th) * Mathf.Cos(ph), Mathf.Cos(th), Mathf.Sin(th) * Mathf.Sin(ph));
                m.Spike(c + Vector3.Scale(n, r) * 0.95f, n, 0.016f, 0.028f, Pal.Moss3, 4);
            }
            return m;
        }

        // A dandelion seed: a brown seed, a pale beak and the white fluff on top.
        static Lp Seed()
        {
            var m = new Lp();
            m.Push(V(0, 0.45f, 0), 0f, 0.8f);
            m.Push(V(0, -0.45f, 0));
            Vector3 b = V(0, 0.45f, 0);
            m.Ball(b, V(0.014f, 0.035f, 0.014f), 5, 3, Pal.SeedBrown);
            m.Tube(b + V(0, 0.03f, 0), b + V(0, 0.08f, 0), 0.005f, 0.005f, 4, Pal.Puff);
            Vector3 top = b + V(0, 0.08f, 0);
            for (int i = 0; i < 10; i++)
            {
                float a = i * 36f * Mathf.Deg2Rad;
                Vector3 d = V(Mathf.Cos(a), 0.55f, Mathf.Sin(a)).normalized;
                Vector3 side = V(-Mathf.Sin(a), 0, Mathf.Cos(a)) * 0.008f;
                m.Tri2(top, top + d * 0.065f + side, top + d * 0.065f - side, Pal.White, Pal.Puff);
            }
            m.Ball(top, V(0.012f, 0.008f, 0.012f), 5, 3, Pal.White);
            m.Pop();
            m.Pop();
            return m;
        }
    }
}
