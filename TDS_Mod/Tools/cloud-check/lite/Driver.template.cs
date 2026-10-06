// Runs {P}'s editor generators outside Unity: writes the sprite sheets (1x, as Build All Assets would) and previews
// (scaled, on a checker), and renders every low-poly model at the game camera's angle with a small rasterizer.
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace {NS}
{
    public static class PreviewDriver
    {
        static string outDir;
        public static void Main(string[] args)
        {
            outDir = args[0];
            Directory.CreateDirectory(Path.Combine(outDir, "sheets"));
            Directory.CreateDirectory(Path.Combine(outDir, "preview"));
            bool sprites = args.Length < 2 || args[1].Contains("s"), models = args.Length < 2 || args[1].Contains("m"), audio = args.Length < 2 || args[1].Contains("a");
            if (sprites) Sprites();
            if (models) Models();
            if (audio) Audio();
        }

        static void Save(string name, Px px, int cell)
        {
            px.Save(Path.Combine(outDir, "sheets", name + ".png"));
            int k = px.W <= 32 ? 8 : px.W <= 128 ? 4 : 3;
            var bg = new Px(px.W * k, px.H * k);
            for (int y = 0; y < bg.H; y++)
                for (int x = 0; x < bg.W; x++)
                    bg.C[y * bg.W + x] = ((x / (cell * k)) + (y / (cell * k))) % 2 == 1 ? new Color32(150, 165, 150, 255) : new Color32(138, 153, 138, 255);
            bg.Draw(px.Scale(k), 0, 0);
            bg.Save(Path.Combine(outDir, "preview", name + ".png"));
        }

        static void Sprites()
        {
{SPRITES}
        }

        static void Audio()
        {
            foreach (var s in {P}Audio.All())
            {
                float[] b = s.Make();
                double sum = 0; float peak = 0;
                foreach (var v in b) { sum += v * v; peak = Mathf.Max(peak, Mathf.Abs(v)); }
                Console.WriteLine("audio " + s.Name + " " + (b.Length / 44100f).ToString("F2") + "s rms " + Math.Sqrt(sum / Math.Max(1, b.Length)).ToString("F3") + " peak " + peak.ToString("F2"));
            }
        }

        // ------------------------------------------------------------ models

        static void Models()
        {
            var groups = new Dictionary<string, List<ModelDef>>();
            var order = new List<string>();
            foreach (var d in {P}Models.All())
            {
                if (!groups.ContainsKey(d.Group)) { groups[d.Group] = new List<ModelDef>(); order.Add(d.Group); }
                groups[d.Group].Add(d);
            }
            // A moving part (ModelDef.Part, Argo's custom aim) is merged in at rest; the first def with a part is also
            // drawn with the part turned 60 degrees about its pivot, to check the pivot.
            var partF = typeof(ModelDef).GetField("Part");
            var pivotF = typeof(ModelDef).GetField("PartPivot");
            if (partF != null)
                foreach (var g in order)
                {
                    var first = groups[g].Find(d => partF.GetValue(d) != null);
                    if (first == null) continue;
                    var turned = new ModelDef();
                    foreach (var f in typeof(ModelDef).GetFields()) f.SetValue(turned, f.GetValue(first));
                    turned.Name = first.Name + " (turned)";
                    groups[g].Insert(groups[g].IndexOf(first) + 1, turned);
                }
            foreach (var g in order)
            {
                var defs = groups[g];
                int cell = 256, cols = Math.Min(4, defs.Count), rows = (defs.Count + cols - 1) / cols;
                var sheet = new Px(cell * cols, cell * rows);
                for (int i = 0; i < defs.Count; i++)
                {
                    var lp = defs[i].Build();
                    var mesh = lp.ToMesh(defs[i].Name);
                    if (partF != null && partF.GetValue(defs[i]) is Func<Lp> partFn)
                    {
                        var pivot = (Vector3)pivotF.GetValue(defs[i]) + new Vector3(0f, -0.5f, 0f);
                        var plp = partFn();
                        var pm = plp.ToMesh(defs[i].Name + "_Part");
                        for (int k = 0; k < pm.V.Count; k++) pm.V[k] -= pivot;
                        pm.RecalculateBounds();
                        var turn = Quaternion.Euler(0f, defs[i].Name.EndsWith("(turned)") ? 60f : 0f, 0f);
                        int baseIdx = mesh.V.Count;
                        for (int k = 0; k < pm.V.Count; k++) { mesh.V.Add(turn * pm.V[k] + pivot); mesh.N.Add(turn * pm.N[k]); mesh.UV.Add(pm.UV[k]); }
                        for (int k = 0; k < pm.Tris.Count; k++) mesh.Tris.Add(pm.Tris[k] + baseIdx);
                        mesh.RecalculateBounds();
                        var pb = pm.bounds;
                        Console.WriteLine("  part of " + defs[i].Name + " tris=" + plp.Triangles + " around pivot: min=" + pb.min.ToString("F2") + " max=" + pb.max.ToString("F2"));
                    }
                    var b = mesh.bounds;
                    Console.WriteLine("model " + defs[i].Name + " tris=" + lp.Triangles + " size=" + b.size.ToString("F2") + " min=" + b.min.ToString("F2") + " max=" + b.max.ToString("F2"));
                    bool proj = defs[i].Projectile;
                    var img = Render(mesh, cell, proj ? 0.3f : 0.95f, proj ? b.center : new Vector3(0, 0.15f, 0), proj);
                    sheet.Draw(img, (i % cols) * cell, (i / cols) * cell);
                    Directory.CreateDirectory(Path.Combine(outDir, "models"));
                    img.Save(Path.Combine(outDir, "models", defs[i].Name.Replace(" ", "_").Replace("(", "").Replace(")", "") + ".png"));
                    Label(sheet, (i % cols) * cell + 4, (i / cols) * cell + 4, defs[i].Name);
                }
                sheet.Save(Path.Combine(outDir, "preview", "models_" + g + ".png"));
            }
        }

        // Orthographic, rotation Euler(35, 45, 0) like the builder's preview camera; one key light + fill + ambient.
        static Px Render(Mesh m, int size, float ortho, Vector3 look, bool proj)
        {
            const int SS = 2;
            int S = size * SS;
            var rot = Quaternion.Euler(35f, 45f, 0f);
            var inv = Quaternion.Inverse(rot);
            Vector3 key = Quaternion.Euler(50f, 120f, 0f) * Vector3.forward, fill = Quaternion.Euler(340f, 218f, 177f) * Vector3.forward;
            var depth = new float[S * S];
            var col = new Color[S * S];
            for (int i = 0; i < depth.Length; i++) { depth[i] = float.MaxValue; col[i] = new Color(0.86f, 0.84f, 0.80f, 1f); }
            // ground grid: the tile's footprint
            for (int t = 0; t + 2 < m.Tris.Count; t += 3)
            {
                Vector3 a = m.V[m.Tris[t]], b = m.V[m.Tris[t + 1]], c = m.V[m.Tris[t + 2]];
                Vector3 n = m.N[m.Tris[t]];
                Vector2 uv = m.UV[m.Tris[t]];
                int pi = Mathf.Clamp((int)(uv.x * 8f), 0, 7) + Mathf.Clamp((int)(uv.y * 8f), 0, 7) * 8;
                Color baseCol = Pal.Color(pi);
                float lit = 0.55f + 1.1f * Mathf.Max(0f, Vector3.Dot(n, -key)) * 0.75f + 0.5f * Mathf.Max(0f, Vector3.Dot(n, -fill)) * 0.4f;
                Color shaded = new Color(baseCol.r * lit, baseCol.g * lit, baseCol.b * lit, 1f);
                Vector3 pa = inv * (a - look), pb = inv * (b - look), pc = inv * (c - look);
                // camera looks along +z in its space; screen x right, y up
                Func<Vector3, Vector2> P = v => new Vector2((v.x / ortho * 0.5f + 0.5f) * S, (0.5f - v.y / ortho * 0.5f) * S);
                Vector2 sa = P(pa), sb = P(pb), sc = P(pc);
                // back-face cull (the meshes are closed or two-sided)
                Vector3 vn = inv * n;
                if (vn.z > 0.0001f) continue;
                int x0 = Math.Max(0, (int)Math.Floor(Mathf.Min(sa.x, sb.x, sc.x))), x1 = Math.Min(S - 1, (int)Math.Ceiling(Mathf.Max(sa.x, sb.x, sc.x)));
                int y0 = Math.Max(0, (int)Math.Floor(Mathf.Min(sa.y, sb.y, sc.y))), y1 = Math.Min(S - 1, (int)Math.Ceiling(Mathf.Max(sa.y, sb.y, sc.y)));
                float area = (sb.x - sa.x) * (sc.y - sa.y) - (sc.x - sa.x) * (sb.y - sa.y);
                if (Mathf.Abs(area) < 1e-6f) continue;
                for (int y = y0; y <= y1; y++)
                    for (int x = x0; x <= x1; x++)
                    {
                        float px = x + 0.5f, py = y + 0.5f;
                        float w0 = ((sb.x - px) * (sc.y - py) - (sc.x - px) * (sb.y - py)) / area;
                        float w1 = ((sc.x - px) * (sa.y - py) - (sa.x - px) * (sc.y - py)) / area;
                        float w2 = 1f - w0 - w1;
                        if (w0 < -1e-4f || w1 < -1e-4f || w2 < -1e-4f) continue;
                        float z = w0 * pa.z + w1 * pb.z + w2 * pc.z;
                        int idx = y * S + x;
                        if (z < depth[idx]) { depth[idx] = z; col[idx] = shaded; }
                    }
            }
            var o = new Px(size, size);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float r = 0, g = 0, b = 0;
                    for (int dy = 0; dy < SS; dy++) for (int dx = 0; dx < SS; dx++) { var c = col[(y * SS + dy) * S + x * SS + dx]; r += c.r; g += c.g; b += c.b; }
                    o.Set(x, y, new Color(r / (SS * SS), g / (SS * SS), b / (SS * SS), 1f));
                }
            if (!proj)
            {
                // tile outline at ground level, so size and footprint read
                for (int i = 0; i <= 40; i++)
                    foreach (var e in new[] { new Vector3(-0.5f + i / 40f, -0.5f, -0.5f), new Vector3(-0.5f + i / 40f, -0.5f, 0.5f), new Vector3(-0.5f, -0.5f, -0.5f + i / 40f), new Vector3(0.5f, -0.5f, -0.5f + i / 40f) })
                    {
                        Vector3 v = inv * (e - look);
                        int x = (int)((v.x / ortho * 0.5f + 0.5f) * size), y = (int)((0.5f - v.y / ortho * 0.5f) * size);
                        if (x >= 0 && y >= 0 && x < size && y < size && o.Get(x, y).r > 200 && o.Get(x, y).g > 200) o.Set(x, y, new Color32(120, 120, 110, 255));
                    }
            }
            return o;
        }

        // A tiny 3x5 font for labels.
        static readonly Dictionary<char, string> Font = new Dictionary<char, string>
        {
            {'A',"010101111101101"},{'B',"110101110101110"},{'C',"011100100100011"},{'D',"110101101101110"},{'E',"111100110100111"},{'F',"111100110100100"},
            {'G',"011100101101011"},{'H',"101101111101101"},{'I',"111010010010111"},{'J',"001001001101010"},{'K',"101101110101101"},{'L',"100100100100111"},
            {'M',"101111111101101"},{'N',"110101101101101"},{'O',"010101101101010"},{'P',"110101110100100"},{'Q',"010101101110011"},{'R',"110101110101101"},
            {'S',"011100010001110"},{'T',"111010010010010"},{'U',"101101101101111"},{'V',"101101101101010"},{'W',"101101111111101"},{'X',"101101010101101"},
            {'Y',"101101010010010"},{'Z',"111001010100111"},{'0',"111101101101111"},{'1',"010110010010111"},{'2',"110001010100111"},{'3',"110001010001110"},
            {'4',"101101111001001"},{'5',"111100110001110"},{'6',"011100110101010"},{'7',"111001010010010"},{'8',"010101010101010"},{'9',"010101011001110"},
            {'_',"000000000000111"},
        };
        static void Label(Px p, int x, int y, string s)
        {
            int cx = x;
            foreach (char ch0 in s.ToUpperInvariant())
            {
                string g;
                if (Font.TryGetValue(ch0, out g))
                    for (int i = 0; i < 15; i++) if (g[i] == '1') { p.Set(cx + i % 3, y + i / 3, new Color32(30, 30, 40, 255)); }
                cx += 4;
            }
        }
    }
}
