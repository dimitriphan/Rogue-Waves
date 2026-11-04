// Spool asset generator: low-poly meshes in the game's tower format, built sleek like Sno Sno's and Buster's towers
// (never voxel): a few clean solids per model - bevelled blocks, tubes, frusta, low-poly spheres, folded drapes -
// coloured from one 8x8 point-filtered palette texture (like the game's Tower_*_Color textures). Origin at the tile
// centre, ground at y = -0.5; primitive coordinates are given with y measured up from the ground. Front faces -z.
// The Lp builder is the kit template's (each mod's editor code stands alone); the palette is Spool's.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpoolBuild
{
    public static class Pal
    {
        // 64 colours, index = row * 8 + column of the 8x8 palette texture.
        public static readonly string[] Hex =
        {
            // 0-7 lilac felt, light to dark
            "EADFF6", "D9C7EE", "C6AEE3", "B196D6", "9A7DC2", "8166AA", "6A528F", "523F70",
            // 8-15 wood (the Loom, the basket)
            "EDCFA6", "D8B285", "BE9468", "A07850", "835F3E", "65472E", "F1DEC0", "CDB68F",
            // 16-23 tomato red (the Pin Cushion), its leaf cap, and Spool's own purple
            "F27C66", "E0594A", "C24538", "9C352C", "73271F", "86C46E", "5F9A52", "9363AE",
            // 24-31 brass and gold
            "F8E39A", "EDC867", "D4A946", "AE8731", "836423", "FFF5CF", "E2BE5E", "BE923E",
            // 32-39 cream canvas and pale lilac
            "FCF7EE", "F2E9D6", "E2D5BC", "CBBB9C", "AB9B7C", "EFE6F8", "DED1EC", "BAABCB",
            // 40-47 threads: white, silk, pink, blue, golden, red, teal, ink
            "FFFFFF", "F6F2FF", "F5BACB", "A3C8EF", "F8D97A", "E45F6E", "82D6C6", "2C2332",
            // 48-55 iron and steel
            "E2E6EB", "BEC5CD", "98A1AB", "727B86", "525A65", "3A414B", "C9D2DD", "8C96A3",
            // 56-63 cookie-tin blue, shadow, mint, black
            "9CC2EC", "6F9AD6", "4C74B4", "35548A", "1F1826", "3B3046", "A5DBA6", "15111A",
        };

        public const int Felt0 = 0, Felt1 = 1, Felt2 = 2, Felt3 = 3, Felt4 = 4, Felt5 = 5, Felt6 = 6, Felt7 = 7;
        public const int Wood0 = 8, Wood1 = 9, Wood2 = 10, Wood3 = 11, Wood4 = 12, Wood5 = 13, WoodPale = 14, WoodDry = 15;
        public const int Tomato0 = 16, Tomato1 = 17, Tomato2 = 18, Tomato3 = 19, Tomato4 = 20, Leaf = 21, LeafDark = 22, Purple = 23;
        public const int Gold0 = 24, Gold1 = 25, Gold2 = 26, Gold3 = 27, Gold4 = 28, GoldPale = 29, Brass = 30, BrassDark = 31;
        public const int Cream0 = 32, Cream1 = 33, Cream2 = 34, Cream3 = 35, Cream4 = 36, Lilac0 = 37, Lilac1 = 38, Lilac2 = 39;
        public const int White = 40, Silk = 41, ThreadPink = 42, ThreadBlue = 43, ThreadGold = 44, ThreadRed = 45, Teal = 46, Ink = 47;
        public const int Iron0 = 48, Iron1 = 49, Iron2 = 50, Iron3 = 51, Iron4 = 52, Iron5 = 53, Steel = 54, SteelDark = 55;
        public const int Tin0 = 56, Tin1 = 57, Tin2 = 58, Tin3 = 59, Shadow = 60, ShadowLight = 61, Mint = 62, Black = 63;

        public static Color32 Color(int i) { return Col.H(Hex[i]); }

        public static Vector2 UV(int i) { return new Vector2(((i % 8) + 0.5f) / 8f, ((i / 8) + 0.5f) / 8f); }

        public static Texture2D Texture()
        {
            var t = new Texture2D(8, 8, TextureFormat.RGBA32, false);
            for (int i = 0; i < 64; i++) t.SetPixel(i % 8, i / 8, Color(i));
            t.filterMode = FilterMode.Point;
            t.Apply();
            return t;
        }
    }

    public class Lp
    {
        readonly List<Vector3> verts = new List<Vector3>();
        readonly List<Vector3> norms = new List<Vector3>();
        readonly List<Vector2> uvs = new List<Vector2>();
        readonly List<int> tris = new List<int>();
        readonly Stack<Matrix4x4> stack = new Stack<Matrix4x4>();
        Matrix4x4 xf = Matrix4x4.identity;
        public const float Ground = -0.5f;

        public int Triangles { get { return tris.Count / 3; } }

        // ------------------------------------------------------------ transform stack

        public void Push(Vector3 pos, Quaternion rot, Vector3 scale)
        {
            stack.Push(xf);
            xf = xf * Matrix4x4.TRS(pos, rot, scale);
        }

        public void Push(Vector3 pos, float yawDeg = 0f, float scale = 1f) { Push(pos, Quaternion.Euler(0f, yawDeg, 0f), Vector3.one * scale); }

        public void Pop() { xf = stack.Pop(); }

        Vector3 W(Vector3 p) { var q = xf.MultiplyPoint3x4(p); q.y += Ground; return q; }

        // ------------------------------------------------------------ triangles

        // Adds a flat triangle (own vertices) facing away from 'inside' (a point inside the solid).
        public void Tri(Vector3 a, Vector3 b, Vector3 c, int col, Vector3 inside)
        {
            Vector3 wa = W(a), wb = W(b), wc = W(c), wi = W(inside);
            Vector3 n = Vector3.Cross(wb - wa, wc - wa);
            if (n.sqrMagnitude < 1e-12f) return;
            if (Vector3.Dot(n, (wa + wb + wc) / 3f - wi) < 0f) { var t = wb; wb = wc; wc = t; n = -n; }
            Add(wa, wb, wc, n.normalized, col);
        }

        // Two-sided triangle (leaves, petals): one face each way.
        public void Tri2(Vector3 a, Vector3 b, Vector3 c, int front, int back)
        {
            Vector3 wa = W(a), wb = W(b), wc = W(c);
            Vector3 n = Vector3.Cross(wb - wa, wc - wa);
            if (n.sqrMagnitude < 1e-12f) return;
            n.Normalize();
            if (n.y < 0f) { var t = wb; wb = wc; wc = t; n = -n; }
            Add(wa, wb, wc, n, front);
            Add(wa, wc, wb, -n, back);
        }

        void Add(Vector3 a, Vector3 b, Vector3 c, Vector3 n, int col)
        {
            int s = verts.Count;
            verts.Add(a); verts.Add(b); verts.Add(c);
            var uv = Pal.UV(col);
            for (int i = 0; i < 3; i++) { norms.Add(n); uvs.Add(uv); }
            tris.Add(s); tris.Add(s + 1); tris.Add(s + 2);
        }

        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, int col, Vector3 inside)
        {
            Tri(a, b, c, col, inside);
            Tri(a, c, d, col, inside);
        }

        // ------------------------------------------------------------ primitives

        static void Basis(Vector3 axis, out Vector3 u, out Vector3 v)
        {
            axis.Normalize();
            u = Vector3.Cross(axis, Mathf.Abs(axis.y) > 0.9f ? Vector3.right : Vector3.up).normalized;
            v = Vector3.Cross(axis, u).normalized;
        }

        // Frustum (or prism) from a to b. Side colour, then cap colours (-1 = no cap).
        public void Tube(Vector3 a, Vector3 b, float ra, float rb, int sides, int side, int capB = -1, int capA = -1, float twistDeg = 0f)
        {
            Vector3 axis = b - a, u, v;
            Basis(axis, out u, out v);
            Vector3 mid = (a + b) * 0.5f;
            var ringA = new Vector3[sides];
            var ringB = new Vector3[sides];
            for (int i = 0; i < sides; i++)
            {
                float ang = (i + 0.5f) / sides * Mathf.PI * 2f + twistDeg * Mathf.Deg2Rad;
                Vector3 dir = u * Mathf.Cos(ang) + v * Mathf.Sin(ang);
                ringA[i] = a + dir * ra;
                ringB[i] = b + dir * rb;
            }
            for (int i = 0; i < sides; i++)
            {
                int j = (i + 1) % sides;
                if (ra > 1e-4f && rb > 1e-4f) Quad(ringA[i], ringA[j], ringB[j], ringB[i], side, mid);
                else if (ra > 1e-4f) Tri(ringA[i], ringA[j], b, side, mid);
                else Tri(a, ringB[j], ringB[i], side, mid);
            }
            if (capB >= 0 && rb > 1e-4f) for (int i = 1; i < sides - 1; i++) Tri(ringB[0], ringB[i], ringB[i + 1], capB, mid);
            if (capA >= 0 && ra > 1e-4f) for (int i = 1; i < sides - 1; i++) Tri(ringA[0], ringA[i], ringA[i + 1], capA, mid);
        }

        // Upright prism/frustum standing on 'baseCenter'.
        public void Prism(Vector3 baseCenter, float rBottom, float rTop, float height, int sides, int side, int top, int bottom = -1, float twistDeg = 0f)
        {
            Tube(baseCenter, baseCenter + Vector3.up * height, rBottom, rTop, sides, side, top, bottom, twistDeg);
        }

        // Path of tube segments (stems, branches, roots), tapering from r0 to r1.
        public void Stem(IList<Vector3> pts, float r0, float r1, int sides, int col, int capCol = -1)
        {
            for (int i = 0; i < pts.Count - 1; i++)
            {
                float ta = i / (float)(pts.Count - 1), tb = (i + 1) / (float)(pts.Count - 1);
                Tube(pts[i], pts[i + 1], Mathf.Lerp(r0, r1, ta), Mathf.Lerp(r0, r1, tb), sides, col, i == pts.Count - 2 ? capCol : -1, i == 0 ? capCol : -1);
            }
        }

        // Low-poly ellipsoid; faces above 'topFrom' (normal.y) take 'top', below 'bottomTo' take 'bottom'.
        public void Ball(Vector3 c, Vector3 r, int lon, int lat, int col, int top = -1, float topFrom = 0.55f, int bottom = -1, float bottomTo = -0.6f, float yawDeg = 0f)
        {
            Func<int, int, Vector3> P = (i, j) =>
            {
                float th = Mathf.PI * j / lat, ph = Mathf.PI * 2f * i / lon + yawDeg * Mathf.Deg2Rad;
                return c + new Vector3(Mathf.Sin(th) * Mathf.Cos(ph) * r.x, Mathf.Cos(th) * r.y, Mathf.Sin(th) * Mathf.Sin(ph) * r.z);
            };
            for (int j = 0; j < lat; j++)
                for (int i = 0; i < lon; i++)
                {
                    Vector3 a = P(i, j), b = P(i + 1, j), d = P(i, j + 1), e = P(i + 1, j + 1);
                    float ny = Mathf.Cos(Mathf.PI * (j + 0.5f) / lat);
                    int k = top >= 0 && ny > topFrom ? top : bottom >= 0 && ny < bottomTo ? bottom : col;
                    if (j == 0) Tri(a, d, e, k, c);
                    else if (j == lat - 1) Tri(a, b, d, k, c);
                    else Quad(a, b, e, d, k, c);
                }
        }

        // Dome (upper half-ellipsoid) with a flat underside: mushroom caps, bushes, mounds.
        public void Dome(Vector3 baseCenter, Vector3 r, int lon, int lat, int col, int top = -1, float topFrom = 0.6f, int under = -1, float yawDeg = 0f)
        {
            Vector3 inside = baseCenter + Vector3.up * r.y * 0.3f;
            Func<int, int, Vector3> P = (i, j) =>
            {
                float th = Mathf.PI * 0.5f * j / lat, ph = Mathf.PI * 2f * i / lon + yawDeg * Mathf.Deg2Rad;
                return baseCenter + new Vector3(Mathf.Sin(th) * Mathf.Cos(ph) * r.x, Mathf.Cos(th) * r.y, Mathf.Sin(th) * Mathf.Sin(ph) * r.z);
            };
            for (int j = 0; j < lat; j++)
                for (int i = 0; i < lon; i++)
                {
                    Vector3 a = P(i, j), b = P(i + 1, j), d = P(i, j + 1), e = P(i + 1, j + 1);
                    float ny = Mathf.Cos(Mathf.PI * 0.5f * (j + 0.5f) / lat);
                    int k = top >= 0 && ny > topFrom ? top : col;
                    if (j == 0) Tri(a, d, e, k, inside);
                    else Quad(a, b, e, d, k, inside);
                }
            if (under >= 0)
                for (int i = 0; i < lon; i++) Tri(baseCenter, P(i, lat), P(i + 1, lat), under, inside + Vector3.up * r.y);
        }

        // Box with bevelled vertical edges and a bevelled top: hedges, crates, planters.
        public void Block(Vector3 baseCenter, Vector3 size, float bevel, int side, int top, int bevelCol = -1)
        {
            if (bevelCol < 0) bevelCol = top;
            float hx = size.x * 0.5f, hz = size.z * 0.5f, b = Mathf.Min(bevel, Mathf.Min(hx, hz) * 0.9f);
            Vector2[] ring =
            {
                new Vector2(-hx + b, -hz), new Vector2(hx - b, -hz), new Vector2(hx, -hz + b), new Vector2(hx, hz - b),
                new Vector2(hx - b, hz), new Vector2(-hx + b, hz), new Vector2(-hx, hz - b), new Vector2(-hx, -hz + b)
            };
            float y0 = baseCenter.y, y1 = baseCenter.y + size.y - b, y2 = baseCenter.y + size.y;
            Vector3 mid = baseCenter + Vector3.up * size.y * 0.5f;
            Func<Vector2, float, float, Vector3> Pt = (q, y, s) => new Vector3(baseCenter.x + q.x * s, y, baseCenter.z + q.y * s);
            float inset = 1f - b / Mathf.Max(hx, hz);
            for (int i = 0; i < ring.Length; i++)
            {
                int j = (i + 1) % ring.Length;
                Quad(Pt(ring[i], y0, 1f), Pt(ring[j], y0, 1f), Pt(ring[j], y1, 1f), Pt(ring[i], y1, 1f), side, mid);
                Quad(Pt(ring[i], y1, 1f), Pt(ring[j], y1, 1f), Pt(ring[j], y2, inset), Pt(ring[i], y2, inset), bevelCol, mid);
            }
            for (int i = 1; i < ring.Length - 1; i++) Tri(Pt(ring[0], y2, inset), Pt(ring[i], y2, inset), Pt(ring[i + 1], y2, inset), top, mid);
        }

        // Thorn / spike: cone from base along dir.
        public void Spike(Vector3 basePos, Vector3 dir, float radius, float length, int col, int sides = 4)
        {
            Tube(basePos, basePos + dir.normalized * length, radius, 0f, sides, col);
        }

        // Folded leaf: diamond from base along dir, midrib raised by 'fold', drooping at the tip. Two-sided.
        public void Leaf(Vector3 basePos, Vector3 dir, float length, float width, int col, int back = -1, float fold = 0.25f, float droop = 0.2f)
        {
            if (back < 0) back = col;
            dir.Normalize();
            Vector3 side = Vector3.Cross(Vector3.up, dir);
            if (side.sqrMagnitude < 1e-4f) side = Vector3.right;
            side.Normalize();
            Vector3 up = Vector3.Cross(dir, side).normalized;
            if (up.y < 0) up = -up;
            Vector3 tip = basePos + dir * length - up * length * droop;
            Vector3 mid = basePos + dir * length * 0.45f + up * length * 0.05f;
            Vector3 l = mid - side * width * 0.5f - up * width * fold, r = mid + side * width * 0.5f - up * width * fold;
            Vector3 rib = mid + up * width * 0.12f;
            Tri2(basePos, l, rib, col, back);
            Tri2(basePos, rib, r, col, back);
            Tri2(rib, l, tip, col, back);
            Tri2(rib, tip, r, col, back);
        }

        // Flat disc lying on the ground plane (or any height): ground patches, sap pools, spots.
        public void Disc(Vector3 center, float r, int sides, int col, float thick = 0.01f, float yawDeg = 0f)
        {
            Prism(center, r, r, thick, sides, col, col, -1, yawDeg);
        }

        // Five-petal flower facing 'normal' (flat petals around a centre).
        public void Flower(Vector3 center, Vector3 normal, float r, int petal, int centre, int petals = 5)
        {
            Vector3 u, v;
            Basis(normal, out u, out v);
            for (int i = 0; i < petals; i++)
            {
                float a0 = (i - 0.35f) / petals * Mathf.PI * 2f, a1 = (i + 0.35f) / petals * Mathf.PI * 2f, am = i / (float)petals * Mathf.PI * 2f;
                Vector3 p0 = center + (u * Mathf.Cos(a0) + v * Mathf.Sin(a0)) * r * 0.55f;
                Vector3 p1 = center + (u * Mathf.Cos(a1) + v * Mathf.Sin(a1)) * r * 0.55f;
                Vector3 pt = center + (u * Mathf.Cos(am) + v * Mathf.Sin(am)) * r;
                Tri2(center, p0, pt, petal, petal);
                Tri2(center, pt, p1, petal, petal);
            }
            Ball(center + normal.normalized * r * 0.08f, Vector3.one * r * 0.3f, 5, 3, centre);
        }

        // ------------------------------------------------------------ output

        public Mesh ToMesh(string name)
        {
            var mesh = new Mesh { name = name };
            if (verts.Count > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
