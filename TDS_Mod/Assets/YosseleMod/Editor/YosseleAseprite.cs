// Yossele asset generator: hand-editable Aseprite copies of the sprite sheets (same file in every vessel's editor code).
//
// Build All Assets writes ModSource/Yossele/Aseprite/generated/*.aseprite from the code's sheets, every build:
//   Yossele.aseprite (32 px: idle and move v0-v3, attack, ability, fall, landing, menu idle, icons, journal tab),
//   Yossele_16px (boat mini), Yossele_splash (128 px), Yossele_readyup (12 px). Tags name the animation.
// To hand-edit, copy a file from generated/ up into ModSource/Yossele/Aseprite/ and edit it in Aseprite (RGB colour mode;
// keep the canvas size and each tag's frame count; extra layers are fine, hidden ones are ignored). On the next build
// every frame you changed replaces the code's frame in the game's sheet. Frames you left alone keep following the code,
// and where the code mirrors a facing (v1 from v0, v3 from v2) the mirror follows your edit unless you edited it too.
// build_log.txt lists each sheet that used hand-edited frames. Delete the file to go back to the code's art.
//
// Each tag's user data (Tag Properties in Aseprite) says where its frames go and what they looked like when written:
//   sheet=Vessel/Yossele-Sheet.png;row=0;col=0;cols=6;rows=8;gen=<crc32 of each frame>[;mirror=<tag it mirrors>]
// Same format as TDS_Mod/Tools/aseprite/ase_sheets.py.
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using UnityEngine;

namespace YosseleBuild
{
    public static class YosseleAseprite
    {
        class CodeSheet { public string Folder, Name; public int Cell; public Px Px; }

        class HandFrame { public string Tag, Mirror; public int Row, Col, Index, Cell; public Color32[] C; public bool Edited; }

        static string vessel, dir;
        static StringBuilder log;
        static readonly List<CodeSheet> code = new List<CodeSheet>();
        static readonly Dictionary<string, List<HandFrame>> hand = new Dictionary<string, List<HandFrame>>();

        // ================================================================ builder hooks

        // Start of WriteSprites: forget the last build and load the hand-edited files.
        public static void Begin(string vesselName, string asepriteDir, StringBuilder buildLog)
        {
            vessel = vesselName;
            dir = asepriteDir;
            log = buildLog;
            code.Clear();
            hand.Clear();
            if (!Directory.Exists(dir)) return;
            foreach (var f in Directory.GetFiles(dir, "*.aseprite"))
            {
                try { LoadHand(f); }
                catch (Exception e) { log.AppendLine("aseprite: skipped " + Path.GetFileName(f) + ": " + e.Message); }
            }
        }

        // SaveSheet: remembers the code's sheet for generated/ and returns it with the hand-edited frames merged in.
        public static Px Apply(Px px, string folder, string name, int cell)
        {
            code.Add(new CodeSheet { Folder = folder, Name = name, Cell = cell, Px = px });
            List<HandFrame> list;
            if (!hand.TryGetValue(folder + "/" + name, out list)) return px;
            foreach (var h in list)
                if (h.Cell != cell || (h.Col + 1) * cell > px.W || (h.Row + 1) * cell > px.H)
                {
                    log.AppendLine("aseprite: " + folder + "/" + name + ".png kept the code's art: tag " + h.Tag + " doesn't fit the " + px.W + "x" + px.H + " sheet");
                    return px;
                }
            var byFrame = new Dictionary<string, HandFrame>();
            foreach (var h in list) byFrame[h.Tag + "#" + h.Index] = h;
            var merged = px.Clone();
            int edited = 0, mirrored = 0;
            foreach (var h in byFrame.Values)
            {
                Color32[] src = null;
                HandFrame m;
                if (h.Edited) { src = h.C; edited++; }
                else if (h.Mirror != null && byFrame.TryGetValue(h.Mirror + "#" + h.Index, out m) && m.Edited) { src = FlipX(m.C, cell); mirrored++; }
                if (src == null) continue;
                for (int y = 0; y < cell; y++)
                    Array.Copy(src, y * cell, merged.C, (h.Row * cell + y) * merged.W + h.Col * cell, cell);
            }
            if (edited + mirrored == 0) return px;
            log.AppendLine("aseprite: " + folder + "/" + name + ".png uses " + edited + " hand-edited frame(s)" + (mirrored > 0 ? " and " + mirrored + " mirrored from them" : ""));
            return merged;
        }

        // End of WriteSprites: one .aseprite file per cell size, from the code's sheets, into generated/. A failure here
        // is logged and never stops the build.
        public static void WriteGenerated()
        {
            try { WriteFiles(); }
            catch (Exception e) { log.AppendLine("aseprite: generated files not written: " + e.Message); }
        }

        static void WriteFiles()
        {
            var stages = new List<string>();
            foreach (var s in code)
                if (s.Px.W == 192 && s.Px.H == 256 && s.Name.StartsWith(vessel) && s.Name.EndsWith("-Sheet"))
                {
                    string st = s.Name.Substring(vessel.Length, s.Name.Length - vessel.Length - 6);
                    if (st != "" && !stages.Contains(st)) stages.Add(st);
                }
            var groups = new SortedDictionary<int, List<KeyValuePair<Layout, CodeSheet>>>();
            var palette = new List<Color32> { new Color32(0, 0, 0, 0) };
            var seen = new HashSet<int>();
            foreach (var s in code)
            {
                foreach (var c in s.Px.C)
                    if (c.a > 0 && seen.Add(c.r | c.g << 8 | c.b << 16 | c.a << 24)) palette.Add(c);
                var lay = LayoutOf(s.Name, s.Px.W, s.Px.H, stages);
                if (lay == null || lay.Cell != s.Cell) { log.AppendLine("aseprite: no layout for " + s.Name + ", left out"); continue; }
                if (!groups.ContainsKey(lay.Cell)) groups[lay.Cell] = new List<KeyValuePair<Layout, CodeSheet>>();
                groups[lay.Cell].Add(new KeyValuePair<Layout, CodeSheet>(lay, s));
            }
            if (palette.Count > 256) palette.RemoveRange(256, palette.Count - 256);
            palette.Sort(1, palette.Count - 1, Comparer<Color32>.Create(ColourOrder));
            string gen = Path.Combine(dir, "generated");
            Directory.CreateDirectory(gen);
            foreach (var g in groups)
            {
                int cell = g.Key;
                g.Value.Sort((a, b) =>
                {
                    int k = a.Key.Major.CompareTo(b.Key.Major);
                    if (k == 0) k = string.CompareOrdinal(a.Key.Minor, b.Key.Minor);
                    return k != 0 ? k : string.CompareOrdinal(a.Value.Name, b.Value.Name);
                });
                var frames = new List<Color32[]>();
                var tags = new List<Tag>();
                foreach (var kv in g.Value)
                {
                    var s = kv.Value;
                    var rows = kv.Key.Rows;
                    int cols = 0;
                    foreach (var r in rows) { int n = 0; foreach (var t in r) n += t.Count; cols = Math.Max(cols, n); }
                    var start = new Dictionary<string, int>();
                    for (int ri = 0; ri < rows.Count; ri++)
                    {
                        int col = 0;
                        foreach (var t in rows[ri])
                        {
                            var crcs = new List<string>();
                            start[t.Name] = frames.Count;
                            for (int k = 0; k < t.Count; k++)
                            {
                                var f = Cut(s.Px, (col + k) * cell, ri * cell, cell);
                                frames.Add(f);
                                crcs.Add(Crc(f).ToString("x8"));
                            }
                            tags.Add(new Tag
                            {
                                Name = t.Name, From = start[t.Name], To = frames.Count - 1, Rgb = KindColour(t.Kind),
                                Data = "sheet=" + s.Folder + "/" + s.Name + ".png;row=" + ri + ";col=" + col + ";cols=" + cols + ";rows=" + rows.Count + ";gen=" + string.Join(",", crcs.ToArray())
                            });
                            col += t.Count;
                        }
                    }
                    // Mark facings the code draws as mirrors (v1 = v0 flipped, v3 = v2 flipped).
                    foreach (var t in tags)
                    {
                        if (!start.ContainsKey(t.Name) || !(t.Name.EndsWith("_v1") || t.Name.EndsWith("_v3"))) continue;
                        string src = t.Name.Substring(0, t.Name.Length - 1) + (t.Name.EndsWith("_v1") ? "0" : "2");
                        Tag o = tags.Find(x => x.Name == src && start.ContainsKey(src));
                        if (o == null || o.To - o.From != t.To - t.From) continue;
                        bool same = true;
                        for (int k = 0; same && k <= t.To - t.From; k++) same = Equal(frames[t.From + k], FlipX(frames[o.From + k], cell));
                        if (same) t.Data += ";mirror=" + src;
                    }
                }
                string suffix = cell == 32 ? "" : cell == 16 ? "_16px" : cell == 128 ? "_splash" : cell == 12 ? "_readyup" : "_" + cell + "px";
                Write(Path.Combine(gen, vessel + suffix + ".aseprite"), cell, frames, tags, palette, vessel);
            }
            log.AppendLine("aseprite: wrote " + groups.Count + " files to " + gen);
        }

        // ================================================================ sheet layouts (as ase_sheets.py)

        class TagSpec { public string Name, Kind; public int Count; }

        class Layout { public int Cell, Major; public string Minor; public List<List<TagSpec>> Rows = new List<List<TagSpec>>(); }

        static Layout Lay(int cell, int major, string minor) { return new Layout { Cell = cell, Major = major, Minor = minor }; }

        static Layout Row(Layout l, params object[] tagCountKind)
        {
            var r = new List<TagSpec>();
            for (int i = 0; i < tagCountKind.Length; i += 3)
                r.Add(new TagSpec { Name = (string)tagCountKind[i], Count = (int)tagCountKind[i + 1], Kind = (string)tagCountKind[i + 2] });
            l.Rows.Add(r);
            return l;
        }

        static Layout Facings(Layout l, string tag, int n, string kind)
        {
            for (int v = 0; v < 4; v++) Row(l, tag + "_v" + v, n, kind);
            return l;
        }

        static string Snake(string s)
        {
            var b = new StringBuilder();
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (char.IsUpper(c) && i > 0 && (char.IsLower(s[i - 1]) || char.IsDigit(s[i - 1]))) b.Append('_');
                b.Append(char.ToLowerInvariant(c));
            }
            return b.ToString().Replace("__", "_");
        }

        static Layout LayoutOf(string name, int w, int h, List<string> stages)
        {
            string V = vessel;
            string[] ones = { V + "_Icon", V + "endstat1", V + "endstat2", V + "tabicon" };
            string[] oneTags = { "icon", "end_win", "end_lose", "journal_tab" };
            int oi = Array.IndexOf(ones, name);
            if (oi >= 0 && w == 32 && h == 32) return Row(Lay(32, 90, oi.ToString()), oneTags[oi], 1, "ui");
            if (name == V + "splash") return Row(Lay(128, 95, "0"), "splash", 1, "ui");
            if (name == "Readyup6" + V.ToLowerInvariant()) return Row(Lay(12, 96, "0"), "readyup", 1, "ui");
            if (name == "S" + V + "Sheet" && w == 96 && h == 128) return Facings(Facings(Lay(16, 0, "0"), "mini_idle", 6, "mini"), "mini_move", 6, "mini");
            if (name == V + "Idlejustidle-Sheet" && h == 32) return Row(Lay(32, 80, "0"), "menu_idle", w / 32, "idle");
            if (!name.EndsWith("-Sheet")) return null;
            string rem = name.Substring(0, name.Length - 6);
            // A unit sheet is the only one 64 high: its name may start with the vessel's (the scaffold names the template's unit
            // <Name>unit), so the name is not checked.
            if (h == 64 && w % 16 == 0)
            {
                string u = Snake(rem);
                var l = Lay(16, 10, rem);
                for (int v = 0; v < 4; v++)
                    if (w / 16 == 10) Row(l, u + "_move_v" + v, 6, "unit", u + "_attack_v" + v, 4, "unit"); // idle is column 0
                    else Row(l, u + "_v" + v, w / 16, "unit");
                return l;
            }
            if (!rem.StartsWith(V)) return null;
            rem = rem.Substring(V.Length);
            string stage = "";
            foreach (var s in stages) if (rem.StartsWith(s)) { stage = s; break; }
            string rest = rem.Substring(stage.Length), pre = stage == "" ? "" : Snake(stage) + "_";
            int si = Math.Max(0, stages.IndexOf(stage));
            if (rest == "" && w == 192 && h == 256) return Facings(Facings(Lay(32, si, "0"), pre + "idle", 6, "idle"), pre + "move", 6, "move");
            if (rest == "Attack" && w == 128 && h == 128) return Facings(Lay(32, si, "1"), pre + "attack", 4, "attack");
            if (rest == "Fall" && h == 32) return Row(Lay(32, si, "3"), pre + "fall", w / 32, "fall");
            if (rest == "IntroR" && h == 32) return Row(Lay(32, si, "4"), pre + "land", w / 32, "land");
            if (rest != "" && w == 192 && h == 128) return Facings(Lay(32, si, "2"), pre + Snake(rest), 6, "ability");
            return null;
        }

        static byte[] KindColour(string kind)
        {
            switch (kind)
            {
                case "idle": return new byte[] { 106, 190, 48 };
                case "move": return new byte[] { 91, 110, 225 };
                case "attack": return new byte[] { 217, 87, 99 };
                case "ability": return new byte[] { 178, 102, 216 };
                case "fall": return new byte[] { 223, 113, 38 };
                case "land": return new byte[] { 238, 195, 154 };
                case "mini": return new byte[] { 95, 205, 228 };
                case "unit": return new byte[] { 55, 148, 110 };
                default: return new byte[] { 155, 173, 183 };
            }
        }

        // Opaque before translucent, greys apart, then by hue and brightness.
        static int ColourOrder(Color32 a, Color32 b)
        {
            int k = (a.a < 255).CompareTo(b.a < 255);
            if (k == 0) k = b.a.CompareTo(a.a);
            if (k == 0) k = Grey(a).CompareTo(Grey(b));
            if (k == 0 && !Grey(a)) k = Hue(a).CompareTo(Hue(b));
            if (k == 0) k = (0.299f * a.r + 0.587f * a.g + 0.114f * a.b).CompareTo(0.299f * b.r + 0.587f * b.g + 0.114f * b.b);
            return k;
        }

        static bool Grey(Color32 c) { return Math.Max(c.r, Math.Max(c.g, c.b)) - Math.Min(c.r, Math.Min(c.g, c.b)) < 24; }

        static int Hue(Color32 c)
        {
            int mx = Math.Max(c.r, Math.Max(c.g, c.b)), mn = Math.Min(c.r, Math.Min(c.g, c.b));
            if (mx == mn) return 0;
            double h = mx == c.r ? ((c.g - c.b) / (double)(mx - mn) + 6) % 6 : mx == c.g ? (c.b - c.r) / (double)(mx - mn) + 2 : (c.r - c.g) / (double)(mx - mn) + 4;
            return (int)Math.Round(h * 2);
        }

        // ================================================================ pixels

        static Color32[] Cut(Px px, int x0, int y0, int cell)
        {
            var f = new Color32[cell * cell];
            for (int y = 0; y < cell; y++)
                for (int x = 0; x < cell; x++)
                {
                    var c = px.Get(x0 + x, y0 + y);
                    f[y * cell + x] = c.a == 0 ? new Color32(0, 0, 0, 0) : c;
                }
            return f;
        }

        static Color32[] FlipX(Color32[] f, int cell)
        {
            var o = new Color32[f.Length];
            for (int y = 0; y < cell; y++) for (int x = 0; x < cell; x++) o[y * cell + cell - 1 - x] = f[y * cell + x];
            return o;
        }

        static bool Equal(Color32[] a, Color32[] b)
        {
            for (int i = 0; i < a.Length; i++)
                if (a[i].r != b[i].r || a[i].g != b[i].g || a[i].b != b[i].b || a[i].a != b[i].a) return false;
            return true;
        }

        static uint[] crcTable;

        // zlib's CRC-32 of the frame's RGBA bytes (row 0 at the top).
        static uint Crc(Color32[] f)
        {
            if (crcTable == null)
            {
                crcTable = new uint[256];
                for (uint n = 0; n < 256; n++)
                {
                    uint c = n;
                    for (int k = 0; k < 8; k++) c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1;
                    crcTable[n] = c;
                }
            }
            uint crc = 0xFFFFFFFFu;
            foreach (var p in f)
            {
                crc = crcTable[(crc ^ p.r) & 0xFF] ^ (crc >> 8);
                crc = crcTable[(crc ^ p.g) & 0xFF] ^ (crc >> 8);
                crc = crcTable[(crc ^ p.b) & 0xFF] ^ (crc >> 8);
                crc = crcTable[(crc ^ p.a) & 0xFF] ^ (crc >> 8);
            }
            return crc ^ 0xFFFFFFFFu;
        }

        // ================================================================ .aseprite writer

        class Tag { public string Name, Data = ""; public int From, To; public byte[] Rgb = { 0, 0, 0 }; }

        static void Str(BinaryWriter w, string s)
        {
            var b = Encoding.UTF8.GetBytes(s);
            w.Write((ushort)b.Length);
            w.Write(b);
        }

        static byte[] Chunk(ushort type, Action<BinaryWriter> body)
        {
            var m = new MemoryStream();
            var w = new BinaryWriter(m);
            w.Write(0u);
            w.Write(type);
            body(w);
            w.Flush();
            var b = m.ToArray();
            BitConverter.GetBytes((uint)b.Length).CopyTo(b, 0);
            return b;
        }

        static byte[] Zlib(byte[] raw)
        {
            var m = new MemoryStream();
            m.WriteByte(0x78);
            m.WriteByte(0x9C);
            using (var z = new DeflateStream(m, CompressionMode.Compress, true)) z.Write(raw, 0, raw.Length);
            uint a = 1, b = 0;
            foreach (byte x in raw) { a = (a + x) % 65521; b = (b + a) % 65521; }
            uint adler = b << 16 | a;
            m.WriteByte((byte)(adler >> 24));
            m.WriteByte((byte)(adler >> 16));
            m.WriteByte((byte)(adler >> 8));
            m.WriteByte((byte)adler);
            return m.ToArray();
        }

        static void Write(string path, int cell, List<Color32[]> frames, List<Tag> tags, List<Color32> palette, string layerName)
        {
            const ushort duration = 100;
            var body = new MemoryStream();
            for (int i = 0; i < frames.Count; i++)
            {
                var chunks = new List<byte[]>();
                if (i == 0)
                {
                    chunks.Add(Chunk(0x2007, w => { w.Write((ushort)1); w.Write((ushort)0); w.Write(0u); w.Write(new byte[8]); })); // sRGB
                    chunks.Add(Chunk(0x2019, w =>
                    {
                        w.Write((uint)palette.Count); w.Write(0u); w.Write((uint)(palette.Count - 1)); w.Write(new byte[8]);
                        foreach (var c in palette) { w.Write((ushort)0); w.Write(c.r); w.Write(c.g); w.Write(c.b); w.Write(c.a); }
                    }));
                    chunks.Add(Chunk(0x2004, w =>
                    {
                        w.Write((ushort)3); w.Write((ushort)0); w.Write((ushort)0); w.Write((ushort)0); w.Write((ushort)0); w.Write((ushort)0);
                        w.Write((byte)255); w.Write(new byte[3]); Str(w, layerName);
                    }));
                    if (tags.Count > 0)
                    {
                        chunks.Add(Chunk(0x2018, w =>
                        {
                            w.Write((ushort)tags.Count); w.Write(new byte[8]);
                            foreach (var t in tags)
                            {
                                w.Write((ushort)t.From); w.Write((ushort)t.To); w.Write((byte)0); w.Write((ushort)0); w.Write(new byte[6]);
                                w.Write(t.Rgb); w.Write((byte)0); Str(w, t.Name);
                            }
                        }));
                        foreach (var t in tags) // one user data chunk per tag, in tag order
                            chunks.Add(Chunk(0x2020, w => { w.Write(3u); Str(w, t.Data); w.Write(t.Rgb); w.Write((byte)255); }));
                    }
                }
                var f = frames[i];
                int x0 = cell, y0 = cell, x1 = -1, y1 = -1;
                for (int y = 0; y < cell; y++)
                    for (int x = 0; x < cell; x++)
                        if (f[y * cell + x].a > 0) { x0 = Math.Min(x0, x); y0 = Math.Min(y0, y); x1 = Math.Max(x1, x); y1 = Math.Max(y1, y); }
                if (x1 >= 0)
                {
                    int cw = x1 - x0 + 1, ch = y1 - y0 + 1;
                    var raw = new byte[cw * ch * 4];
                    for (int y = 0; y < ch; y++)
                        for (int x = 0; x < cw; x++)
                        {
                            var c = f[(y0 + y) * cell + x0 + x];
                            int o = (y * cw + x) * 4;
                            raw[o] = c.r; raw[o + 1] = c.g; raw[o + 2] = c.b; raw[o + 3] = c.a;
                        }
                    chunks.Add(Chunk(0x2005, w =>
                    {
                        w.Write((ushort)0); w.Write((short)x0); w.Write((short)y0); w.Write((byte)255); w.Write((ushort)2); w.Write((short)0);
                        w.Write(new byte[5]); w.Write((ushort)cw); w.Write((ushort)ch); w.Write(Zlib(raw));
                    }));
                }
                int size = 16;
                foreach (var c in chunks) size += c.Length;
                var fw = new BinaryWriter(body);
                fw.Write((uint)size); fw.Write((ushort)0xF1FA); fw.Write((ushort)Math.Min(chunks.Count, 0xFFFF)); fw.Write(duration);
                fw.Write((ushort)0); fw.Write((uint)chunks.Count);
                foreach (var c in chunks) fw.Write(c);
                fw.Flush();
            }
            var file = new MemoryStream();
            var hw = new BinaryWriter(file);
            hw.Write((uint)(128 + body.Length)); hw.Write((ushort)0xA5E0); hw.Write((ushort)frames.Count); hw.Write((ushort)cell); hw.Write((ushort)cell);
            hw.Write((ushort)32); hw.Write(1u); hw.Write(duration); hw.Write(0u); hw.Write(0u); hw.Write((byte)0); hw.Write(new byte[3]);
            hw.Write((ushort)palette.Count); hw.Write((byte)1); hw.Write((byte)1); hw.Write((short)0); hw.Write((short)0);
            hw.Write((ushort)16); hw.Write((ushort)16); hw.Write(new byte[84]);
            hw.Write(body.ToArray());
            hw.Flush();
            File.WriteAllBytes(path, file.ToArray());
        }

        // ================================================================ .aseprite reader

        class Layer { public bool Visible; public int Type, Level, Opacity; }

        class Cel { public int X, Y, W, H, Opacity; public byte[] Px; }

        class Ase
        {
            public int W, H;
            public List<Layer> Layers = new List<Layer>();
            public List<Dictionary<int, Cel>> Frames = new List<Dictionary<int, Cel>>();
            public List<Tag> Tags = new List<Tag>();
        }

        static Ase Read(string path)
        {
            var d = File.ReadAllBytes(path);
            if (BitConverter.ToUInt16(d, 4) != 0xA5E0) throw new Exception("not an .aseprite file");
            if (BitConverter.ToUInt16(d, 12) != 32) throw new Exception("colour mode must be RGB (Sprite > Color Mode > RGB Color)");
            var a = new Ase { W = BitConverter.ToUInt16(d, 8), H = BitConverter.ToUInt16(d, 10) };
            if (a.W != a.H) throw new Exception("canvas must be square");
            int nframes = BitConverter.ToUInt16(d, 6), pos = 128;
            var pending = new List<Tag>();
            for (int fi = 0; fi < nframes; fi++)
            {
                int flen = (int)BitConverter.ToUInt32(d, pos);
                int n = (int)BitConverter.ToUInt32(d, pos + 12);
                if (n == 0) n = BitConverter.ToUInt16(d, pos + 6);
                var cels = new Dictionary<int, Cel>();
                int p = pos + 16;
                for (int ci = 0; ci < n; ci++)
                {
                    int clen = (int)BitConverter.ToUInt32(d, p), type = BitConverter.ToUInt16(d, p + 4), c = p + 6;
                    if (type == 0x2004)
                    {
                        a.Layers.Add(new Layer { Visible = (BitConverter.ToUInt16(d, c) & 1) != 0, Type = BitConverter.ToUInt16(d, c + 2), Level = BitConverter.ToUInt16(d, c + 4), Opacity = d[c + 12] });
                        pending.Clear();
                    }
                    else if (type == 0x2005)
                    {
                        int li = BitConverter.ToUInt16(d, c), celType = BitConverter.ToUInt16(d, c + 7);
                        if (celType == 1) { Cel linked; if (a.Frames[BitConverter.ToUInt16(d, c + 16)].TryGetValue(li, out linked)) cels[li] = linked; }
                        else if (celType == 0 || celType == 2)
                        {
                            var cel = new Cel { X = BitConverter.ToInt16(d, c + 2), Y = BitConverter.ToInt16(d, c + 4), Opacity = d[c + 6], W = BitConverter.ToUInt16(d, c + 16), H = BitConverter.ToUInt16(d, c + 18) };
                            int len = clen - 26;
                            if (celType == 0) { cel.Px = new byte[len]; Array.Copy(d, c + 20, cel.Px, 0, len); }
                            else cel.Px = Inflate(d, c + 20, len, cel.W * cel.H * 4);
                            cels[li] = cel;
                        }
                        pending.Clear();
                    }
                    else if (type == 0x2018)
                    {
                        int count = BitConverter.ToUInt16(d, c), q = c + 10;
                        pending.Clear();
                        for (int t = 0; t < count; t++)
                        {
                            var tag = new Tag { From = BitConverter.ToUInt16(d, q), To = BitConverter.ToUInt16(d, q + 2) };
                            int ln = BitConverter.ToUInt16(d, q + 17);
                            tag.Name = Encoding.UTF8.GetString(d, q + 19, ln);
                            q += 19 + ln;
                            a.Tags.Add(tag);
                            pending.Add(tag);
                        }
                    }
                    else if (type == 0x2020 && pending.Count > 0)
                    {
                        if ((BitConverter.ToUInt32(d, c) & 1) != 0) pending[0].Data = Encoding.UTF8.GetString(d, c + 6, BitConverter.ToUInt16(d, c + 4));
                        pending.RemoveAt(0);
                    }
                    p += clen;
                }
                a.Frames.Add(cels);
                pos += flen;
            }
            return a;
        }

        static byte[] Inflate(byte[] d, int start, int len, int expected)
        {
            var o = new byte[expected];
            using (var z = new DeflateStream(new MemoryStream(d, start + 2, len - 2), CompressionMode.Decompress))
            {
                int got = 0, r;
                while (got < expected && (r = z.Read(o, got, expected - got)) > 0) got += r;
            }
            return o;
        }

        // Visible image layers (inside visible groups), composited with normal blending.
        static Color32[] Flatten(Ase a, int frame)
        {
            int w = a.W, h = a.H;
            var img = new Color32[w * h];
            var shown = new List<bool>();
            for (int li = 0; li < a.Layers.Count; li++)
            {
                var l = a.Layers[li];
                while (shown.Count > l.Level) shown.RemoveAt(shown.Count - 1);
                bool vis = l.Visible && !shown.Contains(false);
                shown.Add(vis);
                Cel cel;
                if (!vis || l.Type != 0 || !a.Frames[frame].TryGetValue(li, out cel)) continue;
                for (int y = 0; y < cel.H; y++)
                    for (int x = 0; x < cel.W; x++)
                    {
                        int tx = cel.X + x, ty = cel.Y + y, s = (y * cel.W + x) * 4;
                        if (tx < 0 || ty < 0 || tx >= w || ty >= h) continue;
                        int sa = cel.Px[s + 3] * cel.Opacity * l.Opacity / (255 * 255);
                        if (sa == 0) continue;
                        var dst = img[ty * w + tx];
                        if (dst.a == 0 || sa == 255) { img[ty * w + tx] = new Color32(cel.Px[s], cel.Px[s + 1], cel.Px[s + 2], (byte)sa); continue; }
                        int oa = sa + dst.a * (255 - sa) / 255;
                        img[ty * w + tx] = new Color32(
                            (byte)((cel.Px[s] * sa + dst.r * dst.a * (255 - sa) / 255) / oa),
                            (byte)((cel.Px[s + 1] * sa + dst.g * dst.a * (255 - sa) / 255) / oa),
                            (byte)((cel.Px[s + 2] * sa + dst.b * dst.a * (255 - sa) / 255) / oa),
                            (byte)oa);
                    }
            }
            return img;
        }

        static void LoadHand(string file)
        {
            var a = Read(file);
            foreach (var t in a.Tags)
            {
                var kv = new Dictionary<string, string>();
                foreach (var part in t.Data.Split(';'))
                {
                    int eq = part.IndexOf('=');
                    if (eq > 0) kv[part.Substring(0, eq)] = part.Substring(eq + 1);
                }
                if (!kv.ContainsKey("sheet") || !kv.ContainsKey("row") || !kv.ContainsKey("col")) continue;
                string sheet = kv["sheet"].EndsWith(".png") ? kv["sheet"].Substring(0, kv["sheet"].Length - 4) : kv["sheet"];
                int row = int.Parse(kv["row"]), col = int.Parse(kv["col"]);
                string[] gen = kv.ContainsKey("gen") ? kv["gen"].Split(',') : new string[0];
                List<HandFrame> list;
                if (!hand.TryGetValue(sheet, out list)) hand[sheet] = list = new List<HandFrame>();
                for (int k = 0; k <= t.To - t.From; k++)
                {
                    var c = Flatten(a, t.From + k);
                    list.Add(new HandFrame
                    {
                        Tag = t.Name, Mirror = kv.ContainsKey("mirror") ? kv["mirror"] : null, Row = row, Col = col + k, Index = k, Cell = a.W, C = c,
                        Edited = k >= gen.Length || gen[k] != Crc(c).ToString("x8")
                    });
                }
            }
        }
    }
}
