// Argo asset generator: low-poly meshes in the game's tower format, built like Sno Sno's and Buster's towers - a few
// clean, flat-shaded solids (prisms, frusta, bevelled blocks, low-poly spheres and domes) coloured from one 8x8
// point-filtered palette texture (like the game's Tower_*_Color textures). Origin at the tile centre, ground at
// y = -0.5; primitive coordinates are given with y measured up from the ground. Front faces -z (camera side).
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ArgoBuild
{
    public static class Pal
    {
        // 64 colours, index = row * 8 + column of the 8x8 palette texture.
        public static readonly string[] Hex =
        {
            // 0-7 brass and copper
            "F2CF7A", "D9A84A", "B98734", "8E6326", "664518", "F7DB8F", "C9744A", "9A5534",
            // 8-15 steel and lead
            "C9D2D9", "A3AFB9", "7F8C97", "5E6A75", "414A53", "6B7078", "4B4F56", "E3E8EC",
            // 16-23 sea stone and sand
            "9FB3B8", "7E959B", "64797F", "4A5C62", "34444A", "D8C79A", "B7A274", "8A7A5A",
            // 24-31 sea blues and porthole glass
            "8FC6E0", "5E9CC2", "3F7AA3", "2C5B7E", "1C3E58", "1E3446", "2F5470", "233447",
            // 32-39 wood, rope, canvas
            "C79A68", "A47A4E", "7D5A38", "5A3F26", "D9C08A", "B39C66", "E9E1C8", "C9BE9E",
            // 40-47 lamps, sonar glow, running lights
            "FFF6C8", "FFE07A", "7AE6EF", "3FB4C4", "DE5456", "A93A3E", "54BE6E", "3A8C50",
            // 48-55 bosun yellow, rust, ink, white, warning orange
            "F4CC56", "C99E32", "A85F3A", "7C4128", "22262B", "F4F6F8", "E8833A", "B65F25",
            // 56-63 ore, magma, bubbles, barnacle teal, kelp
            "8FD8F5", "5AAED6", "FF8A3D", "D9531E", "DFF5FA", "3A9C9A", "2A706F", "5E8F4A",
        };

        public const int BrassLight = 0, Brass = 1, BrassMid = 2, BrassDark = 3, BrassDeep = 4, Gold = 5, Copper = 6, CopperDark = 7;
        public const int SteelLight = 8, Steel = 9, SteelMid = 10, SteelDark = 11, SteelDeep = 12, Lead = 13, LeadDark = 14, Rivet = 15;
        public const int StoneLight = 16, Stone = 17, StoneMid = 18, StoneDark = 19, StoneDeep = 20, Sand = 21, SandDark = 22, Silt = 23;
        public const int SeaLight = 24, Sea = 25, SeaMid = 26, SeaDark = 27, SeaDeep = 28, Glass = 29, GlassLight = 30, Navy = 31;
        public const int WoodLight = 32, Wood = 33, WoodDark = 34, WoodDeep = 35, Rope = 36, RopeDark = 37, Canvas = 38, CanvasDark = 39;
        public const int LampLight = 40, Lamp = 41, Glow = 42, GlowDark = 43, Red = 44, RedDark = 45, Green = 46, GreenDark = 47;
        public const int Yellow = 48, YellowDark = 49, Rust = 50, RustDark = 51, Ink = 52, White = 53, Orange = 54, OrangeDark = 55;
        public const int Ore = 56, OreDark = 57, Magma = 58, MagmaDark = 59, Bubble = 60, Teal = 61, TealDark = 62, Kelp = 63;

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

        // Two-sided triangle (fins, blades, flags): one face each way.
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

        // Band around an axis (rims, hoops, barrel rings): a short open tube just outside radius r, with a bevelled lip
        // on each side so it reads as a raised ring, not a disc.
        public void Hoop(Vector3 center, Vector3 axis, float r, float thick, float width, int sides, int col)
        {
            Vector3 n = axis.normalized * width * 0.5f;
            Tube(center - n, center + n, r + thick, r + thick, sides, col);
            Tube(center - n * 1.6f, center - n, r, r + thick, sides, col);
            Tube(center + n, center + n * 1.6f, r + thick, r, sides, col);
        }

        // Path of tube segments (pipes, hoses, legs), tapering from r0 to r1.
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

        // Dome (upper half-ellipsoid) with an optional flat underside: bells, helmets, caps.
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

        // Box with bevelled vertical edges and a bevelled top: plates, crates, engine blocks.
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

        // Spike / cone from base along dir.
        public void Spike(Vector3 basePos, Vector3 dir, float radius, float length, int col, int sides = 4)
        {
            Tube(basePos, basePos + dir.normalized * length, radius, 0f, sides, col);
        }

        // Flat blade (fin, flipper, propeller blade): a two-sided quad from root to tip, 'width' wide across 'side'.
        public void Blade(Vector3 root, Vector3 tip, Vector3 side, float width, int front, int back)
        {
            Vector3 s = side.normalized * width * 0.5f;
            Tri2(root - s * 0.6f, tip - s, tip + s, front, back);
            Tri2(root - s * 0.6f, tip + s, root + s * 0.6f, front, back);
        }

        // Flat disc lying on the ground plane (or any height): puddles, portholes seen from above, gauges.
        public void Disc(Vector3 center, float r, int sides, int col, float thick = 0.01f, float yawDeg = 0f)
        {
            Prism(center, r, r, thick, sides, col, col, -1, yawDeg);
        }

        // A round window set into a surface: glass disc, brass rim, facing 'normal'.
        public void Porthole(Vector3 center, Vector3 normal, float r, int glass, int rim)
        {
            Vector3 n = normal.normalized;
            Tube(center - n * 0.01f, center + n * 0.025f, r * 1.25f, r * 1.25f, 10, rim, rim);
            Tube(center + n * 0.02f, center + n * 0.03f, r, r, 10, glass, glass);
        }

        // ------------------------------------------------------------ output

        public Mesh ToMesh(string name) { return ToMesh(name, Vector3.zero, 1f); }

        // Scales the model about its ground point (the tile centre at y = -0.5), so it keeps standing on the ground, then
        // subtracts 'offset' (a moving part's pivot, in final mesh space).
        public static Vector3 Scaled(Vector3 p, float scale)
        {
            return new Vector3(p.x * scale, (p.y - Ground) * scale + Ground, p.z * scale);
        }

        public Mesh ToMesh(string name, Vector3 offset, float scale)
        {
            var mesh = new Mesh { name = name };
            if (verts.Count > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            var v = new List<Vector3>(verts.Count);
            foreach (var p in verts) v.Add(Scaled(p, scale) - offset);
            mesh.SetVertices(v);
            mesh.SetNormals(norms);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
