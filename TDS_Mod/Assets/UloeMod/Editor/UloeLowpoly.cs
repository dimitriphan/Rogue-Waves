// Uloe asset generator: low-poly meshes in the game's tower format, built sleek like Sno Sno's and Buster's towers
// (never voxel): a few clean solids per model - bevelled blocks, tubes, frusta, low-poly spheres, folded drapes -
// coloured from one 8x8 point-filtered palette texture (like the game's Tower_*_Color textures). Origin at the tile
// centre, ground at y = -0.5; primitive coordinates are given with y measured up from the ground. Front faces -z.
// The Lp builder is the kit template's (each mod's editor code stands alone); the palette is Uloe's.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace UloeBuild
{
    public static class Pal
    {
        // 64 colours, index = row * 8 + column of the 8x8 palette texture.
        public static readonly string[] Hex =
        {
            // 0-7 glass, light to dark (Uloe's bulb: DAE6FC, CBDBFC)
            "F3F7FF", "DAE6FC", "CBDBFC", "B5C9F0", "9DB4E0", "7F97C6", "62789F", "4A5C7E",
            // 8-15 steel (the screw base and post: 9BADB7 to 595652)
            "C5D2DA", "9BADB7", "7F8C94", "696D70", "595652", "474542", "E2E9ED", "34322F",
            // 16-23 light (the arms: FBF236, D4CD3C; the glints: F1EF72), then embers
            "FFFDE0", "FFF48B", "FBF236", "F1EF72", "D4CD3C", "B0A92C", "F27C3C", "C2502A",
            // 24-31 brass
            "F8E39A", "EDC867", "D4A946", "AE8731", "836423", "FFF5CF", "E2BE5E", "BE923E",
            // 32-39 cord and rubber (222222), the eyes' blue, the filament
            "3A3A3E", "2E2D2D", "222222", "4C4C52", "5E5E66", "707078", "3F3F74", "323C39",
            // 40-47 charge lights, a fuse red, the bounce card, white, ink
            "82D6C6", "A5DBA6", "5FB8A8", "E45F6E", "FCF7EE", "E2D5BC", "FFFFFF", "2C2332",
            // 48-55 mirror silver
            "E6ECF2", "D2DAE3", "BCC6D2", "A2AEBD", "F5F8FB", "8894A6", "6F7C90", "566275",
            // 56-63 wood and spare darks
            "EDCFA6", "BE9468", "835F3E", "65472E", "F2E9D6", "CBBB9C", "6A528F", "15111A",
        };

        public const int Glass0 = 0, Glass1 = 1, Glass2 = 2, Glass3 = 3, Glass4 = 4, Glass5 = 5, Glass6 = 6, Glass7 = 7;
        public const int Steel0 = 8, Steel1 = 9, Steel2 = 10, Steel3 = 11, Steel4 = 12, Steel5 = 13, SteelPale = 14, SteelDark = 15;
        public const int Light0 = 16, Light1 = 17, Light2 = 18, Light3 = 19, Light4 = 20, Light5 = 21, Ember = 22, Ember2 = 23;
        public const int Brass0 = 24, Brass1 = 25, Brass2 = 26, Brass3 = 27, Brass4 = 28, BrassPale = 29, BrassMid = 30, BrassDark = 31;
        public const int Cord0 = 32, Cord1 = 33, Cord2 = 34, Cord3 = 35, Cord4 = 36, Cord5 = 37, Eye = 38, Wire = 39;
        public const int Led0 = 40, Led1 = 41, Led2 = 42, Fuse = 43, Card0 = 44, Card1 = 45, White = 46, Ink = 47;
        public const int Silver0 = 48, Silver1 = 49, Silver2 = 50, Silver3 = 51, Silver4 = 52, Silver5 = 53, Silver6 = 54, Silver7 = 55;
        public const int Wood0 = 56, Wood1 = 57, Wood2 = 58, Wood3 = 59, Cream0 = 60, Cream1 = 61, Dusk = 62, Black = 63;

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

        // A moving part's mesh: built in tower space like the body, its vertices moved so 'pivot' (tower space, y already at the
        // Ground offset) is the mesh's origin, so it turns in place (Argo's Assemble).
        public Mesh ToMesh(string name, Vector3 pivot)
        {
            var moved = new List<Vector3>(verts.Count);
            foreach (var v in verts) moved.Add(v - pivot);
            var mesh = new Mesh { name = name };
            mesh.SetVertices(moved);
            mesh.SetNormals(norms);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
