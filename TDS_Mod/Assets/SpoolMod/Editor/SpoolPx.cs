// Spool asset generator: pixel-art canvas helpers (the kit template's; each mod's editor code stands alone).
// Authoring coordinates are x right, y DOWN (row 0 = top), like the sheets you see in an image editor.
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SpoolBuild
{
    public class Px
    {
        public readonly int W, H;
        public readonly Color32[] C;

        public Px(int w, int h)
        {
            W = w;
            H = h;
            C = new Color32[w * h];
        }

        public bool In(int x, int y) { return x >= 0 && y >= 0 && x < W && y < H; }
        public Color32 Get(int x, int y) { return In(x, y) ? C[y * W + x] : new Color32(0, 0, 0, 0); }
        public bool Opaque(int x, int y) { return Get(x, y).a > 0; }
        public void Set(int x, int y, Color32 c) { if (In(x, y)) C[y * W + x] = c; }

        // Alpha-over compositing.
        public void Over(int x, int y, Color32 c)
        {
            if (!In(x, y) || c.a == 0) return;
            int i = y * W + x;
            if (c.a == 255 || C[i].a == 0) { C[i] = c; return; }
            var d = C[i];
            float a = c.a / 255f, da = d.a / 255f, oa = a + da * (1f - a);
            C[i] = new Color32(
                (byte)Mathf.RoundToInt((c.r * a + d.r * da * (1f - a)) / oa),
                (byte)Mathf.RoundToInt((c.g * a + d.g * da * (1f - a)) / oa),
                (byte)Mathf.RoundToInt((c.b * a + d.b * da * (1f - a)) / oa),
                (byte)Mathf.RoundToInt(oa * 255f));
        }

        public void Draw(Px s, int ox, int oy, bool flipX = false)
        {
            for (int y = 0; y < s.H; y++)
                for (int x = 0; x < s.W; x++)
                    Over(ox + (flipX ? s.W - 1 - x : x), oy + y, s.Get(x, y));
        }

        public Px Clone()
        {
            var p = new Px(W, H);
            System.Array.Copy(C, p.C, C.Length);
            return p;
        }

        public Px FlipX()
        {
            var p = new Px(W, H);
            for (int y = 0; y < H; y++) for (int x = 0; x < W; x++) p.Set(W - 1 - x, y, Get(x, y));
            return p;
        }

        // Quarter turns clockwise about the canvas centre (square canvases only).
        public Px Rot(int quarterTurns)
        {
            var p = Clone();
            for (int t = 0; t < ((quarterTurns % 4) + 4) % 4; t++)
            {
                var r = new Px(W, H);
                for (int y = 0; y < H; y++) for (int x = 0; x < W; x++) r.Set(W - 1 - y, x, p.Get(x, y));
                p = r;
            }
            return p;
        }

        public Px Shift(int dx, int dy)
        {
            var p = new Px(W, H);
            for (int y = 0; y < H; y++) for (int x = 0; x < W; x++) p.Set(x + dx, y + dy, Get(x, y));
            return p;
        }

        public Px Scale(int k)
        {
            var p = new Px(W * k, H * k);
            for (int y = 0; y < p.H; y++) for (int x = 0; x < p.W; x++) p.C[y * p.W + x] = Get(x / k, y / k);
            return p;
        }

        // Adds a soft outline (4-neighbour ring) of colour c around every opaque pixel of this layer.
        public Px WithOutline(Color32 c)
        {
            var p = new Px(W, H);
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    if (Opaque(x, y)) continue;
                    if (Opaque(x - 1, y) || Opaque(x + 1, y) || Opaque(x, y - 1) || Opaque(x, y + 1)) p.Set(x, y, c);
                }
            p.Draw(this, 0, 0);
            return p;
        }

        public Texture2D ToTexture()
        {
            var t = new Texture2D(W, H, TextureFormat.RGBA32, false);
            var arr = new Color32[W * H];
            for (int y = 0; y < H; y++) for (int x = 0; x < W; x++) arr[(H - 1 - y) * W + x] = C[y * W + x];
            t.SetPixels32(arr);
            t.filterMode = FilterMode.Point;
            t.Apply();
            return t;
        }

        public void Save(string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var t = ToTexture();
            File.WriteAllBytes(path, t.EncodeToPNG());
            Object.DestroyImmediate(t);
        }

        // Stamps a char grid. '.' and ' ' are transparent; unknown chars are ignored.
        public void Stamp(string[] rows, Dictionary<char, Color32> pal, int ox, int oy, bool flipX = false)
        {
            int w = 0;
            foreach (var r in rows) w = Mathf.Max(w, r.Length);
            for (int y = 0; y < rows.Length; y++)
                for (int x = 0; x < rows[y].Length; x++)
                {
                    Color32 c;
                    if (pal.TryGetValue(rows[y][x], out c)) Over(ox + (flipX ? w - 1 - x : x), oy + y, c);
                }
        }

        public static Px FromRows(string[] rows, Dictionary<char, Color32> pal)
        {
            int w = 0;
            foreach (var r in rows) w = Mathf.Max(w, r.Length);
            var p = new Px(w, rows.Length);
            p.Stamp(rows, pal, 0, 0);
            return p;
        }

        // Lays frames out on a sheet grid: cells[row][col].
        public static Px Sheet(List<List<Px>> cells, int cw, int ch)
        {
            int cols = 0;
            foreach (var r in cells) cols = Mathf.Max(cols, r.Count);
            var s = new Px(cols * cw, cells.Count * ch);
            for (int r = 0; r < cells.Count; r++)
                for (int c = 0; c < cells[r].Count; c++)
                    if (cells[r][c] != null) s.Draw(cells[r][c], c * cw, r * ch);
            return s;
        }
    }

    public static class Col
    {
        public static Color32 H(string hex, byte a = 255)
        {
            Color c;
            ColorUtility.TryParseHtmlString(hex.StartsWith("#") ? hex : "#" + hex, out c);
            Color32 c32 = c;
            c32.a = a;
            return c32;
        }

        public static Color32 A(Color32 c, byte a) { c.a = a; return c; }

        public static Color32 Mix(Color32 a, Color32 b, float t)
        {
            return new Color32((byte)Mathf.Lerp(a.r, b.r, t), (byte)Mathf.Lerp(a.g, b.g, t), (byte)Mathf.Lerp(a.b, b.b, t), (byte)Mathf.Lerp(a.a, b.a, t));
        }
    }
}
