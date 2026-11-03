// Uloe asset generator - entry point: Tools > Uloe > Build All Assets.
// Writes every Uloe asset into Assets/UloeMod (exported with the mod) and review material into ModSource/Uloe
// (previews; not exported). Safe to re-run: outputs are overwritten in place so GUIDs and references stay stable.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace UloeBuild
{
    public static class UloeBuilder
    {
        public const string Root = "Assets/UloeMod";
        static string Abs(string assetPath) { return Path.Combine(Path.GetDirectoryName(Application.dataPath), assetPath); }
        static string Source { get { return Path.GetFullPath(Path.Combine(Application.dataPath, "../ModSource/Uloe")); } }

        class SheetInfo
        {
            public string Path, Name;
            public int CellW, CellH;
            public Vector2 Pivot;
        }

        static readonly List<SheetInfo> sheets = new List<SheetInfo>();
        static readonly List<string> plainTextures = new List<string>();
        static readonly Dictionary<string, string> modelPrefabs = new Dictionary<string, string>();
        static readonly Dictionary<string, string> meshes = new Dictionary<string, string>();
        static readonly Dictionary<string, string> vfxPrefabs = new Dictionary<string, string>();
        static readonly Dictionary<string, string> vfxUse = new Dictionary<string, string>();
        static readonly Dictionary<string, string> audioFiles = new Dictionary<string, string>();
        static readonly StringBuilder log = new StringBuilder();
        static readonly string[] Groups = { "Glass", "Bank", "Mirror", "Flood", "Extras", "Projectiles" };

        [MenuItem("Tools/Uloe/Build All Assets")]
        public static void BuildAll()
        {
            sheets.Clear(); plainTextures.Clear(); modelPrefabs.Clear(); meshes.Clear(); vfxPrefabs.Clear(); vfxUse.Clear(); audioFiles.Clear(); log.Clear();
            try
            {
                EditorUtility.DisplayProgressBar("Uloe", "Pixel art", 0.05f);
                WriteSprites();
                EditorUtility.DisplayProgressBar("Uloe", "Audio", 0.2f);
                WriteAudio();
                WriteVfxTextures();
                AssetDatabase.Refresh();
                EditorUtility.DisplayProgressBar("Uloe", "Import settings", 0.3f);
                ConfigureImports();
                EditorUtility.DisplayProgressBar("Uloe", "Low-poly models", 0.45f);
                BuildModels();
                EditorUtility.DisplayProgressBar("Uloe", "VFX", 0.65f);
                BuildVfx();
                EditorUtility.DisplayProgressBar("Uloe", "Library + data", 0.75f);
                BuildLibrary();
                WriteData();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                EditorUtility.DisplayProgressBar("Uloe", "Previews", 0.85f);
                WritePreviews();
                log.AppendLine("OK");
            }
            catch (Exception e)
            {
                log.AppendLine("FAILED: " + e);
                Debug.LogException(e);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                Directory.CreateDirectory(Source);
                File.WriteAllText(Path.Combine(Source, "build_log.txt"), log.ToString());
                Debug.Log("[Uloe] build finished: " + Path.Combine(Source, "build_log.txt"));
            }
        }

        // Quick look at the pixel art only: ModSource/Uloe/Previews/wip/*.png, enlarged on a checker.
        [MenuItem("Tools/Uloe/Preview Sprites")]
        public static void PreviewSprites()
        {
            string dir = Path.Combine(Source, "Previews", "wip");
            Directory.CreateDirectory(dir);
            void Save(string name, Px px, int cell)
            {
                int k = px.W <= 32 ? 8 : px.W <= 128 ? 4 : 3;
                var bg = new Px(px.W * k, px.H * k);
                for (int y = 0; y < bg.H; y++)
                    for (int x = 0; x < bg.W; x++)
                        bg.C[y * bg.W + x] = ((x / (cell * k)) + (y / (cell * k))) % 2 == 1 ? new Color32(150, 165, 150, 255) : new Color32(138, 153, 138, 255);
                bg.Draw(px.Scale(k), 0, 0);
                bg.Save(Path.Combine(dir, name + ".png"));
            }
            Save("main", UloeCharacter.MainSheet(), 32);
            Save("lit", UloeCharacter.MainSheet(true), 32);
            Save("attack", UloeCharacter.VariantSheet(UloeCharacter.Attack()), 32);
            Save("plug", UloeCharacter.VariantSheet(UloeCharacter.PlugIn()), 32);
            Save("fall", UloeCharacter.Strip(UloeCharacter.Fall()), 32);
            Save("land", UloeCharacter.Strip(UloeCharacter.Land()), 32);
            Save("mini", UloeCharacter.MiniSheet(), 16);
            Save("unit_photon", UloeCharacter.PhotonSheet(), 16);
            Save("readyup", UloeCharacter.ReadyUp(), 12);
            Save("tabicon", UloeCharacter.TabIcon(), 32);
            Save("splash", UloeCharacter.Splash(), 128);
            Save("endwin", UloeCharacter.EndWin(), 32);
            Save("endlose", UloeCharacter.EndLose(), 32);
            Debug.Log("[Uloe] sprite previews in " + dir);
        }

        // Quick look at the models only (no assets written): ModSource/Uloe/Previews/wip/models_<Group>.png.
        [MenuItem("Tools/Uloe/Preview Models")]
        public static void PreviewModels()
        {
            string dir = Path.Combine(Source, "Previews", "wip");
            Directory.CreateDirectory(dir);
            var pal = Pal.Texture();
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.SetTexture("_BaseMap", pal);
            mat.SetFloat("_Smoothness", 0.05f);
            foreach (var g in Groups)
            {
                var defs = UloeModels.All().Where(d => d.Group == g).ToList();
                if (defs.Count == 0) continue;
                int cell = 256;
                var sheet = new Texture2D(cell * defs.Count, cell, TextureFormat.RGBA32, false);
                for (int i = 0; i < defs.Count; i++)
                {
                    var go = Assemble(defs[i], mat, null);
                    var img = Render(go, cell, defs[i].Projectile ? 0.25f : 0.9f, defs[i].Projectile ? 0f : 0.05f);
                    sheet.SetPixels(i * cell, 0, cell, cell, img.GetPixels());
                    UnityEngine.Object.DestroyImmediate(img);
                    UnityEngine.Object.DestroyImmediate(go);
                }
                sheet.Apply();
                File.WriteAllBytes(Path.Combine(dir, "models_" + g + ".png"), sheet.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(sheet);
            }
            UnityEngine.Object.DestroyImmediate(mat);
            UnityEngine.Object.DestroyImmediate(pal);
            File.WriteAllText(Path.Combine(dir, "models_log.txt"), log.ToString());
            Debug.Log("[Uloe] model previews in " + dir);
        }

        // ================================================================ pixel art

        static void SaveSheet(Px px, string folder, string name, int cw, int ch, Vector2 pivot)
        {
            string path = Root + "/Art/" + folder + "/" + name + ".png";
            UloeAseprite.Apply(FileArt(px, name), folder, name, cw).Save(Abs(path));
            sheets.Add(new SheetInfo { Path = path, Name = name, CellW = cw, CellH = ch, Pivot = pivot });
        }

        // A sheet the code draws can be replaced by a file of the same name and size: ModSource/Uloe/Art/<name>.png
        // (final art) wins over ModSource/Uloe/Art/Temporary/game_size/<name>.png (temporary art, docs/TEMP_ART.md),
        // and either wins over the code. Hand-edited Aseprite frames still merge on top. A file of the wrong size is
        // skipped and logged.
        static Px FileArt(Px code, string name)
        {
            string final = Path.Combine(Source, "Art", name + ".png");
            string temp = Path.Combine(Source, "Art", "Temporary", "game_size", name + ".png");
            string from = File.Exists(final) ? final : File.Exists(temp) ? temp : null;
            if (from == null)
                return code;
            var t = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!ImageConversion.LoadImage(t, File.ReadAllBytes(from)) || t.width != code.W || t.height != code.H)
                {
                    log.AppendLine("sprites: " + name + ".png kept the code's art: " + from + " is " + t.width + "x" + t.height + ", not " + code.W + "x" + code.H);
                    return code;
                }
                var arr = t.GetPixels32();
                var px = new Px(code.W, code.H);
                for (int y = 0; y < px.H; y++)
                    for (int x = 0; x < px.W; x++)
                        px.C[y * px.W + x] = arr[(px.H - 1 - y) * px.W + x];
                log.AppendLine("sprites: " + name + ".png from " + (from == final ? "Art/ (final)" : "Art/Temporary/ (temporary)"));
                return px;
            }
            finally { UnityEngine.Object.DestroyImmediate(t); }
        }

        static void WriteSprites()
        {
            UloeAseprite.Begin("Uloe", Path.Combine(Source, "Aseprite"), log);
            var foot = new Vector2(0.5f, 0f);
            SaveSheet(UloeCharacter.MainSheet(), "Vessel", "Uloe-Sheet", 32, 32, foot);
            SaveSheet(UloeCharacter.MainSheet(true), "Vessel", "UloeLit-Sheet", 32, 32, foot);
            SaveSheet(UloeCharacter.VariantSheet(UloeCharacter.Attack()), "Vessel", "UloeAttack-Sheet", 32, 32, foot);
            SaveSheet(UloeCharacter.VariantSheet(UloeCharacter.PlugIn()), "Vessel", "UloePlug-Sheet", 32, 32, foot);
            SaveSheet(UloeCharacter.Strip(UloeCharacter.Fall()), "Vessel", "UloeFall-Sheet", 32, 32, foot);
            SaveSheet(UloeCharacter.Strip(UloeCharacter.Land()), "Vessel", "UloeIntroR-Sheet", 32, 32, foot);
            SaveSheet(UloeCharacter.MiniSheet(), "UI", "SUloeSheet", 16, 16, foot);
            SaveSheet(UloeCharacter.Strip(UloeCharacter.IdleStrip()), "UI", "UloeIdlejustidle-Sheet", 32, 32, foot);
            // The Photon (Code/02_Data.cs PhotonSheet, Code/04_Content.cs BuildPhoton).
            SaveSheet(UloeCharacter.PhotonSheet(), "Unit", "Uloephoton-Sheet", 16, 16, foot);
            var c = new Vector2(0.5f, 0.5f);
            SaveSheet(UloeCharacter.Icon(), "UI", "Uloe_Icon", 32, 32, c);
            SaveSheet(UloeCharacter.Splash(), "UI", "Uloesplash", 128, 128, c);
            SaveSheet(UloeCharacter.EndWin(), "UI", "Uloeendstat1", 32, 32, c);
            SaveSheet(UloeCharacter.EndLose(), "UI", "Uloeendstat2", 32, 32, c);
            SaveSheet(UloeCharacter.ReadyUp(), "UI", "Readyup6uloe", 12, 12, c);
            SaveSheet(UloeCharacter.TabIcon(), "UI", "Uloetabicon", 32, 32, c);
            log.AppendLine("sprites: " + sheets.Count + " sheets");
            UloeAseprite.WriteGenerated();
        }

        static void ConfigureImports()
        {
            foreach (var s in sheets)
            {
                AssetDatabase.ImportAsset(s.Path, ImportAssetOptions.ForceSynchronousImport);
                var ti = (TextureImporter)AssetImporter.GetAtPath(s.Path);
                // The PNG's own size: a first import scales a sheet that isn't a power of two (192 wide -> 256), so the
                // loaded texture's width would slice cells that aren't there (pitfall #102).
                int w, h;
                PngSize(s.Path, out w, out h);
                ti.textureType = TextureImporterType.Sprite;
                ti.spritePixelsPerUnit = 20;
                ti.filterMode = FilterMode.Point;
                ti.textureCompression = TextureImporterCompression.Uncompressed;
                ti.mipmapEnabled = false;
                ti.isReadable = true;
                ti.alphaIsTransparency = true;
                ti.wrapMode = TextureWrapMode.Clamp;
                bool single = w == s.CellW && h == s.CellH;
                ti.spriteImportMode = single ? SpriteImportMode.Single : SpriteImportMode.Multiple;
                var settings = new TextureImporterSettings();
                ti.ReadTextureSettings(settings);
                settings.spriteAlignment = (int)SpriteAlignment.Custom;
                settings.spritePivot = s.Pivot;
                settings.spriteMeshType = SpriteMeshType.FullRect;
                ti.SetTextureSettings(settings);
                if (!single)
                {
                    var metas = new List<SpriteMetaData>();
                    int cols = w / s.CellW, rows = h / s.CellH, idx = 0;
                    for (int r = 0; r < rows; r++)
                        for (int col = 0; col < cols; col++)
                            metas.Add(new SpriteMetaData
                            {
                                name = s.Name + "_" + idx++,
                                rect = new Rect(col * s.CellW, h - (r + 1) * s.CellH, s.CellW, s.CellH),
                                alignment = (int)SpriteAlignment.Custom,
                                pivot = s.Pivot
                            });
#pragma warning disable 0618
                    ti.spritesheet = metas.ToArray();
#pragma warning restore 0618
                }
                ti.SaveAndReimport();
            }
            foreach (var p in plainTextures)
                ConfigurePlain(p);
            foreach (var kv in audioFiles)
            {
                AssetDatabase.ImportAsset(kv.Value, ImportAssetOptions.ForceSynchronousImport);
                var ai = (AudioImporter)AssetImporter.GetAtPath(kv.Value);
                var st = ai.defaultSampleSettings;
                st.loadType = AudioClipLoadType.DecompressOnLoad;
                st.compressionFormat = AudioCompressionFormat.Vorbis;
                st.quality = 0.7f;
                ai.defaultSampleSettings = st;
                ai.forceToMono = true;
                ai.SaveAndReimport();
            }
        }

        static void PngSize(string path, out int w, out int h)
        {
            byte[] head = new byte[24];
            using (var f = File.OpenRead(path))
                f.Read(head, 0, 24);
            w = (head[16] << 24) | (head[17] << 16) | (head[18] << 8) | head[19];
            h = (head[20] << 24) | (head[21] << 16) | (head[22] << 8) | head[23];
        }

        static void ConfigurePlain(string p)
        {
            AssetDatabase.ImportAsset(p, ImportAssetOptions.ForceSynchronousImport);
            var ti = (TextureImporter)AssetImporter.GetAtPath(p);
            ti.textureType = TextureImporterType.Default;
            ti.filterMode = FilterMode.Point;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.mipmapEnabled = false;
            ti.isReadable = true;
            ti.alphaIsTransparency = true;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.npotScale = TextureImporterNPOTScale.None;
            ti.SaveAndReimport();
        }

        // ================================================================ low-poly models

        static Material paletteMat;

        static void BuildModels()
        {
            string palPath = Root + "/Art/Models/Uloe_Palette.png";
            var palTex = Pal.Texture();
            Directory.CreateDirectory(Path.GetDirectoryName(Abs(palPath)));
            File.WriteAllBytes(Abs(palPath), palTex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(palTex);
            ConfigurePlain(palPath);
            var palAsset = AssetDatabase.LoadAssetAtPath<Texture2D>(palPath);

            string matPath = Root + "/Art/Models/Uloe_Palette.mat";
            paletteMat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (paletteMat == null)
            {
                paletteMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(paletteMat, matPath);
            }
            paletteMat.shader = Shader.Find("Universal Render Pipeline/Lit");
            paletteMat.SetTexture("_BaseMap", palAsset);
            paletteMat.SetTexture("_MainTex", palAsset);
            paletteMat.SetFloat("_Smoothness", 0.05f);
            paletteMat.SetFloat("_SpecularHighlights", 0f);
            paletteMat.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
            EditorUtility.SetDirty(paletteMat);

            foreach (var def in UloeModels.All())
            {
                string folder = Root + "/Art/Models/" + def.Group;
                Directory.CreateDirectory(Abs(folder));
                var root = Assemble(def, paletteMat, folder);
                meshes[def.Name] = folder + "/" + def.Name + ".asset";
                string prefabPath = Root + "/Prefabs/" + (def.Projectile ? "Projectiles" : "Towers/" + def.Group) + "/" + def.Name + ".prefab";
                Directory.CreateDirectory(Path.GetDirectoryName(Abs(prefabPath)));
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                UnityEngine.Object.DestroyImmediate(root);
                modelPrefabs[def.Name] = prefabPath;
            }
            AssetDatabase.SaveAssets();
        }

        // Name / Vis / Name_Export (the static mesh), plus Vis / <Part> at the part's pivot holding Name_<Part>_Export (Argo's way,
        // for the Glass's lens and the Flood Light's lamp). 'save' writes the meshes as assets (null: in-memory only, for previews).
        static GameObject Assemble(ModelDef def, Material mat, string save)
        {
            var root = new GameObject(def.Name);
            var vis = new GameObject("Vis");
            vis.transform.SetParent(root.transform, false);
            var lp = def.Build();
            var mesh = lp.ToMesh(def.Name);
            if (def.Projectile) Recenter(mesh);
            if (save != null) mesh = SaveMesh(mesh, save + "/" + def.Name + ".asset");
            var exp = new GameObject(def.Name + "_Export");
            exp.transform.SetParent(vis.transform, false);
            exp.AddComponent<MeshFilter>().sharedMesh = mesh;
            exp.AddComponent<MeshRenderer>().sharedMaterial = mat;
            int tris = lp.Triangles;
            Mesh partMesh = null;
            Vector3 pivot = Vector3.zero;
            if (def.Part != null)
            {
                pivot = def.PartPivot + new Vector3(0f, Lp.Ground, 0f);
                var plp = def.Part();
                var pmesh = plp.ToMesh(def.Name + "_" + def.PartName, pivot);
                if (save != null) pmesh = SaveMesh(pmesh, save + "/" + def.Name + "_" + def.PartName + ".asset");
                var part = new GameObject(def.PartName);
                part.transform.SetParent(vis.transform, false);
                part.transform.localPosition = pivot;
                var pexp = new GameObject(def.Name + "_" + def.PartName + "_Export");
                pexp.transform.SetParent(part.transform, false);
                pexp.AddComponent<MeshFilter>().sharedMesh = pmesh;
                pexp.AddComponent<MeshRenderer>().sharedMaterial = mat;
                tris += plp.Triangles;
                partMesh = pmesh;
            }
            // The journal's 3D preview (UIObject3D) fits a model to its frame by the first renderer it finds. The shipped Mining
            // Hut and Wall carry a disabled renderer on "Vis" for that; a tower with a moving part is framed whole by it.
            if (partMesh != null)
            {
                var whole = new Mesh { name = def.Name + "_Whole" };
                var parts = new List<CombineInstance>
                {
                    new CombineInstance { mesh = mesh, transform = Matrix4x4.identity },
                    new CombineInstance { mesh = partMesh, transform = Matrix4x4.Translate(pivot) },
                };
                whole.CombineMeshes(parts.ToArray(), true, true);
                whole.RecalculateBounds();
                if (save != null) whole = SaveMesh(whole, save + "/" + def.Name + "_Whole.asset");
                vis.AddComponent<MeshFilter>().sharedMesh = whole;
                var proxy = vis.AddComponent<MeshRenderer>();
                proxy.sharedMaterial = mat;
                proxy.enabled = false;
            }
            if (save != null)
                log.AppendLine("model " + def.Name + " tris=" + tris + " verts=" + mesh.vertexCount + " size=" + mesh.bounds.size.ToString("F2") + " center=" + mesh.bounds.center.ToString("F2") + (def.Part != null ? " + " + def.PartName : ""));
            return root;
        }

        static Mesh SaveMesh(Mesh mesh, string path)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(mesh, path);
                return mesh;
            }
            // Rewrite through the Mesh API so the editor's GPU copy refreshes too (CopySerialized doesn't).
            existing.Clear();
            existing.indexFormat = mesh.indexFormat;
            existing.SetVertices(mesh.vertices);
            existing.SetNormals(mesh.normals);
            existing.SetUVs(0, mesh.uv);
            existing.SetTriangles(mesh.triangles, 0);
            existing.RecalculateBounds();
            EditorUtility.SetDirty(existing);
            UnityEngine.Object.DestroyImmediate(mesh);
            return existing;
        }

        static void Recenter(Mesh m)
        {
            var c = m.bounds.center;
            var v = m.vertices;
            for (int i = 0; i < v.Length; i++) v[i] -= c;
            m.vertices = v;
            m.RecalculateBounds();
        }

        // ================================================================ audio

        static void WriteAudio()
        {
            foreach (var s in UloeAudio.All())
            {
                string path = Root + "/Audio/" + s.Name + ".wav";
                UloeAudio.SaveWav(Abs(path), s.Make());
                audioFiles[s.Name] = path;
            }
            log.AppendLine("audio: " + audioFiles.Count + " clips");
        }

        // ================================================================ VFX

        static readonly Dictionary<string, string[]> VfxTex = new Dictionary<string, string[]>
        {
            { "Px_Square", new[] { "##", "##" } },
            { "Px_Bolt", new[] { "..#", ".#.", "###", ".#.", "#.." } },
            { "Px_Mote", new[] { ".##.", "####", "####", ".##." } },
            { "Px_Star", new[] { "..#..", ".###.", "#####", ".###.", "..#.." } },
            { "Px_Shard", new[] { "#...", "##..", "###.", ".###" } },
        };

        static void WriteVfxTextures()
        {
            var pal = new Dictionary<char, Color32> { { '#', new Color32(255, 255, 255, 255) } };
            foreach (var kv in VfxTex)
            {
                string path = Root + "/Art/VFX/" + kv.Key + ".png";
                Px.FromRows(kv.Value, pal).Save(Abs(path));
                plainTextures.Add(path);
            }
        }

        static Material VfxMat(string tex)
        {
            string path = Root + "/Art/VFX/Uloe_" + tex + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            var sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (m == null) { m = new Material(sh); AssetDatabase.CreateAsset(m, path); }
            m.shader = sh;
            var t = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Art/VFX/" + tex + ".png");
            m.SetTexture("_BaseMap", t);
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(m);
            return m;
        }

        class VfxSpec
        {
            public string Name, Tex = "Px_Square", Use, Mesh;
            public Color A, B;
            public int Burst = 12;
            public float Rate, Life0 = 0.5f, Life1 = 0.9f, Speed0 = 0.5f, Speed1 = 1f, Size0 = 0.06f, Size1 = 0.1f, Gravity, Radius = 0.2f, Height;
            public bool Loop, Spin, Grow, Hemisphere, Edge, ShapeScale, NoParticles;
        }

        // Which way Unity's Hemisphere shape faces: emit a few particles and see where they go, then return the shape
        // rotation that turns the dome upward (so ground bursts never sink into the floor). Sprigg learned this the hard way.
        static Vector3 HemisphereUp()
        {
            var go = new GameObject("hemisphere probe");
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false;
            main.startSpeed = 1f;
            main.startLifetime = 5f;
            main.gravityModifier = 0f;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = 1f;
            ps.Emit(200);
            ps.Simulate(0.5f, true, false);
            var parts = new ParticleSystem.Particle[200];
            int n = ps.GetParticles(parts);
            Vector3 mean = Vector3.zero;
            for (int i = 0; i < n; i++) mean += parts[i].position;
            mean /= Mathf.Max(1, n);
            UnityEngine.Object.DestroyImmediate(go);
            Vector3 rot = Mathf.Abs(mean.y) >= Mathf.Abs(mean.z) ? (mean.y >= 0f ? Vector3.zero : new Vector3(180f, 0f, 0f)) : new Vector3(mean.z > 0f ? -90f : 90f, 0f, 0f);
            log.AppendLine("hemisphere probe: mean " + mean.ToString("F2") + " -> shape rotation " + rot);
            return rot;
        }

        static void BuildVfx()
        {
            var hemisphereUp = HemisphereUp();
            Color H(string hex) { Color c; ColorUtility.TryParseHtmlString("#" + hex, out c); return c; }
            var specs = new List<VfxSpec>
            {
                // One-shots played by UloeClient (Code/11_Client.cs).
                new VfxSpec { Name = "World_Uloe_Spark", Use = "A beam or a spark lands, a plug goes in: a spit of electric sparks", Tex = "Px_Bolt", A = H("FBF236"), B = H("FFFDE0"), Burst = 8, Speed0 = 0.5f, Speed1 = 1.2f, Gravity = 0.4f, Life0 = 0.15f, Life1 = 0.35f, Size0 = 0.05f, Size1 = 0.09f, Spin = true, Hemisphere = true, Radius = 0.08f },
                new VfxSpec { Name = "World_Uloe_Motes", Use = "A Photon appears or fades, a building goes up: motes of light", Tex = "Px_Mote", A = H("FFF48B"), B = H("FFFDE0"), Burst = 12, Speed0 = 0.3f, Speed1 = 0.8f, Gravity = -0.15f, Life0 = 0.4f, Life1 = 0.8f, Size0 = 0.04f, Size1 = 0.08f, Radius = 0.2f },
                new VfxSpec { Name = "World_Uloe_Burst", Use = "Short Circuit: a ring of light and shards of glass (scaled to its radius)", Tex = "Px_Shard", A = H("FBF236"), B = H("DAE6FC"), Burst = 40, Speed0 = 0.1f, Speed1 = 0.4f, Gravity = -0.05f, Life0 = 0.4f, Life1 = 0.7f, Size0 = 0.08f, Size1 = 0.16f, Radius = 1f, Edge = true, ShapeScale = true, Spin = true },
                new VfxSpec { Name = "World_Uloe_Flash", Use = "A lit Mirror flashes at what hit it, Glare turns a hit aside", Tex = "Px_Star", A = H("FFFFFF"), B = H("FFF48B"), Burst = 7, Speed0 = 0.2f, Speed1 = 0.7f, Life0 = 0.12f, Life1 = 0.28f, Size0 = 0.08f, Size1 = 0.16f, Radius = 0.12f, Grow = true },
                new VfxSpec { Name = "World_Uloe_Snap", Use = "The cord snaps: sparks fly", Tex = "Px_Bolt", A = H("FBF236"), B = H("FFFFFF"), Burst = 16, Speed0 = 0.8f, Speed1 = 1.8f, Gravity = 0.8f, Life0 = 0.2f, Life1 = 0.45f, Size0 = 0.05f, Size1 = 0.09f, Spin = true, Hemisphere = true, Radius = 0.1f },
                new VfxSpec { Name = "World_Hero_Uloe_Death", Use = "Vessel death: the bulb pops, glass and sparks", Tex = "Px_Shard", A = H("DAE6FC"), B = H("FBF236"), Burst = 28, Speed0 = 1.4f, Speed1 = 2.6f, Gravity = 1.2f, Life0 = 0.8f, Life1 = 1.3f, Size0 = 0.07f, Size1 = 0.13f, Spin = true, Hemisphere = true, Height = 0.5f },
                // Looks the game attaches under a foe's Vis (Debuff, Code/01_Util.cs); looping until the game takes them off.
                new VfxSpec { Name = "Debuff_Uloe_Blind", Use = "Blinded: dark spots swimming before the foe's eyes", Tex = "Px_Mote", A = H("3F3F74"), B = H("6A6AA8"), Burst = 0, Rate = 7f, Loop = true, Speed0 = 0.05f, Speed1 = 0.15f, Life0 = 0.4f, Life1 = 0.7f, Size0 = 0.05f, Size1 = 0.09f, Radius = 0.22f, Height = 0.8f },
                new VfxSpec { Name = "Debuff_Uloe_Lit", Use = "Illuminated: a yellow glow rising off the foe", Tex = "Px_Mote", A = H("FBF236"), B = H("FFFDE0"), Burst = 0, Rate = 9f, Loop = true, Speed0 = 0.1f, Speed1 = 0.3f, Gravity = -0.12f, Life0 = 0.4f, Life1 = 0.8f, Size0 = 0.04f, Size1 = 0.07f, Radius = 0.28f, Height = 0.1f },
            };
            foreach (var s in specs)
            {
                var go = new GameObject(s.Name);
                if (!s.NoParticles)
                {
                    var ps = go.AddComponent<ParticleSystem>();
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    var main = ps.main;
                    main.duration = s.Loop ? 2f : 1f;
                    main.loop = s.Loop;
                    main.playOnAwake = true;
                    main.startLifetime = new ParticleSystem.MinMaxCurve(s.Life0, s.Life1);
                    main.startSpeed = new ParticleSystem.MinMaxCurve(s.Speed0, s.Speed1);
                    main.startSize = new ParticleSystem.MinMaxCurve(s.Size0, s.Size1);
                    main.startColor = new ParticleSystem.MinMaxGradient(s.A, s.B);
                    main.gravityModifier = s.Gravity;
                    main.simulationSpace = s.Mesh != null ? ParticleSystemSimulationSpace.Local : ParticleSystemSimulationSpace.World;
                    main.maxParticles = 64;
                    main.stopAction = s.Loop || s.Mesh != null ? ParticleSystemStopAction.None : ParticleSystemStopAction.Destroy;
                    if (s.Spin) main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
                    var em = ps.emission;
                    em.rateOverTime = s.Rate;
                    if (s.Burst > 0) em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)s.Burst) });
                    var shape = ps.shape;
                    shape.shapeType = s.Hemisphere ? ParticleSystemShapeType.Hemisphere : ParticleSystemShapeType.Circle;
                    shape.radius = s.Radius;
                    shape.rotation = s.Hemisphere ? hemisphereUp : new Vector3(-90f, 0f, 0f);
                    if (s.Edge) shape.radiusThickness = 0f;
                    if (s.ShapeScale) main.scalingMode = ParticleSystemScalingMode.Shape;
                    shape.position = new Vector3(0, s.Height, 0);
                    var col = ps.colorOverLifetime;
                    col.enabled = true;
                    var g = new Gradient();
                    g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                              new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
                    col.color = g;
                    if (s.Grow)
                    {
                        var sol = ps.sizeOverLifetime;
                        sol.enabled = true;
                        sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 1.4f));
                    }
                    if (s.Spin)
                    {
                        var rol = ps.rotationOverLifetime;
                        rol.enabled = true;
                        rol.z = new ParticleSystem.MinMaxCurve(-3f, 3f);
                    }
                    var r = go.GetComponent<ParticleSystemRenderer>();
                    r.sharedMaterial = VfxMat(s.Tex);
                    r.renderMode = ParticleSystemRenderMode.Billboard;
                }
                if (s.Mesh != null && meshes.ContainsKey(s.Mesh))
                {
                    var child = new GameObject("Mesh");
                    child.transform.SetParent(go.transform, false);
                    child.AddComponent<MeshFilter>().sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshes[s.Mesh]);
                    var mr = child.AddComponent<MeshRenderer>();
                    mr.sharedMaterial = paletteMat;
                    mr.shadowCastingMode = ShadowCastingMode.Off;
                }
                string path = Root + "/Prefabs/VFX/" + s.Name + ".prefab";
                Directory.CreateDirectory(Path.GetDirectoryName(Abs(path)));
                PrefabUtility.SaveAsPrefabAsset(go, path);
                UnityEngine.Object.DestroyImmediate(go);
                vfxPrefabs[s.Name] = path;
                vfxUse[s.Name] = s.Use;
            }
            log.AppendLine("vfx: " + vfxPrefabs.Count);
        }

        // ================================================================ library prefab

        // A single prefab that references every asset through built-in components only (SpriteRenderer, nested
        // prefabs, AudioSource), so runtime mod code can find them by child name without ModTool APIs:
        //   Library/Sprites/<SheetName>  (sprite.texture = the whole sheet)
        //   Library/Models/<ModelName>, Library/VFX/<Name>, Library/Audio/<ClipName>
        static void BuildLibrary()
        {
            var root = new GameObject("UloeAssets_Library");
            var spr = Child(root, "Sprites");
            foreach (var s in sheets)
            {
                var first = AssetDatabase.LoadAllAssetsAtPath(s.Path).OfType<Sprite>().OrderBy(x => x.name.Length).ThenBy(x => x.name).FirstOrDefault();
                var c = Child(spr, s.Name);
                c.AddComponent<SpriteRenderer>().sprite = first;
            }
            var models = Child(root, "Models");
            foreach (var kv in modelPrefabs)
            {
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(kv.Value));
                inst.transform.SetParent(models.transform, false);
            }
            var vfx = Child(root, "VFX");
            foreach (var kv in vfxPrefabs)
            {
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(kv.Value));
                inst.transform.SetParent(vfx.transform, false);
            }
            var audio = Child(root, "Audio");
            foreach (var kv in audioFiles)
            {
                var c = Child(audio, kv.Key);
                var src = c.AddComponent<AudioSource>();
                src.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(kv.Value);
                src.playOnAwake = false;
            }
            // The game instantiates the mod prefab (and this library with it) at startup, so every holder stays inactive
            // (Sprigg 1.2.1: active SpriteRenderers drew the sheets on the startup screen). Find/GetComponent still work.
            spr.SetActive(false);
            models.SetActive(false);
            vfx.SetActive(false);
            audio.SetActive(false);
            string path = Root + "/Prefabs/UloeAssets_Library.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
            log.AppendLine("library: " + path);
        }

        static GameObject Child(GameObject parent, string name)
        {
            var g = new GameObject(name);
            g.transform.SetParent(parent.transform, false);
            return g;
        }

        // ================================================================ data (manifest + localization)

        static string Q(string s) { return "\"" + (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"") + "\""; }
        static string Csv(string s) { return "\"" + (s ?? "").Replace("\"", "\"\"") + "\""; }

        static void WriteData()
        {
            var j = new StringBuilder();
            j.AppendLine("{");
            j.AppendLine("  \"mod\": \"Uloe\", \"generatedBy\": \"Tools/Uloe/Build All Assets (Assets/UloeMod/Editor)\", \"design\": \"ModSource/Uloe/DESIGN.md\",");
            j.AppendLine("  \"conventions\": { \"spritePixelsPerUnit\": 20, \"spritePivot\": [0.5, 0], \"filter\": \"Point\", \"modelStyle\": \"sleek low-poly, flat-shaded, palette UVs (like Sno Sno and Buster)\", \"groundY\": -0.5, \"paletteTexture\": \"Art/Models/Uloe_Palette.png\" },");
            j.AppendLine("  \"sheets\": [");
            for (int i = 0; i < sheets.Count; i++)
                j.AppendLine("    { \"name\": " + Q(sheets[i].Name) + ", \"path\": " + Q(sheets[i].Path) + ", \"cell\": [" + sheets[i].CellW + ", " + sheets[i].CellH + "] }" + (i < sheets.Count - 1 ? "," : ""));
            j.AppendLine("  ],");
            j.AppendLine("  \"models\": [");
            var mk = meshes.Keys.ToList();
            for (int i = 0; i < mk.Count; i++)
                j.AppendLine("    { \"name\": " + Q(mk[i]) + ", \"mesh\": " + Q(meshes[mk[i]]) + ", \"prefab\": " + Q(modelPrefabs.ContainsKey(mk[i]) ? modelPrefabs[mk[i]] : "(inside a VFX prefab)") + " }" + (i < mk.Count - 1 ? "," : ""));
            j.AppendLine("  ],");
            j.AppendLine("  \"vfx\": [");
            var vk = vfxPrefabs.Keys.ToList();
            for (int i = 0; i < vk.Count; i++) j.AppendLine("    { \"id\": " + Q(vk[i]) + ", \"prefab\": " + Q(vfxPrefabs[vk[i]]) + ", \"use\": " + Q(vfxUse[vk[i]]) + " }" + (i < vk.Count - 1 ? "," : ""));
            j.AppendLine("  ],");
            j.AppendLine("  \"sounds\": [");
            var sounds = UloeAudio.All();
            for (int i = 0; i < sounds.Count; i++) j.AppendLine("    { \"id\": " + Q(sounds[i].Name) + ", \"file\": " + Q(audioFiles[sounds[i].Name]) + ", \"use\": " + Q(sounds[i].Use) + " }" + (i < sounds.Count - 1 ? "," : ""));
            j.AppendLine("  ]");
            j.AppendLine("}");
            string manifest = Root + "/Data/Uloe_Manifest.json";
            Directory.CreateDirectory(Path.GetDirectoryName(Abs(manifest)));
            File.WriteAllText(Abs(manifest), j.ToString());

            // Localization sheet for translators: the game never reads this file (docs/21 §3.1, pitfall #97). The
            // English text is the key and an unknown key shows unchanged, so the English names show in every language.
            var csv = new StringBuilder();
            csv.AppendLine("Note,Key,English");
            void Row(string note, string text) { csv.AppendLine(Csv(note) + "," + Csv(text) + "," + Csv(text)); }
            Row("\"Uloe\" Vessel proper name. A glass light bulb on a lamp post.", "Uloe");
            Row("Vessel title", "Light Personified");
            Row("Vessel rule name", "Bright Idea");
            Row("\"Light\" what Uloe's towers grow stronger with", "Light");
            Row("\"PLUG IN\" action button", "PLUG IN"); Row("The same button while plugged in", "UNPLUG");
            Row("Buildable", "Magnifying Glass"); Row("", "Power Bank"); Row("", "Mirror"); Row("", "Flood Light");
            Row("Units", "Photon");
            Row("Building button", "AIM");
            Row("Upgrades", "Burning Point"); Row("", "Short Circuit"); Row("", "Silver Backing"); Row("", "Vanity Lights"); Row("", "Glare"); Row("", "Bounce Card"); Row("", "Wide Flood"); Row("", "Long Throw");
            Row("Effects on foes", "Blinded"); Row("", "Illuminated");
            string loc = Root + "/Data/Uloe_Localization.csv";
            File.WriteAllText(Abs(loc), csv.ToString(), new UTF8Encoding(false));
            log.AppendLine("data: manifest + localization");
        }

        // ================================================================ previews (ModSource/Uloe/Previews, not exported)

        static void WritePreviews()
        {
            string dir = Path.Combine(Source, "Previews");
            Directory.CreateDirectory(dir);
            foreach (var s in sheets)
            {
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(s.Path);
                var px = new Px(tex.width, tex.height);
                var data = tex.GetPixels32();
                for (int y = 0; y < tex.height; y++) for (int x = 0; x < tex.width; x++) px.C[y * tex.width + x] = data[(tex.height - 1 - y) * tex.width + x];
                int k = tex.width <= 32 ? 8 : tex.width <= 128 ? 4 : 3;
                var bg = new Px(tex.width * k, tex.height * k);
                for (int i = 0; i < bg.C.Length; i++) bg.C[i] = new Color32(206, 201, 192, 255);
                for (int y = 0; y < bg.H; y++) for (int x = 0; x < bg.W; x++) if (((x / (s.CellW * k)) + (y / (s.CellH * k))) % 2 == 1) bg.C[y * bg.W + x] = new Color32(196, 191, 182, 255);
                bg.Draw(px.Scale(k), 0, 0);
                bg.Save(Path.Combine(dir, "sprite_" + s.Name + ".png"));
            }
            foreach (var g in Groups)
            {
                var defs = UloeModels.All().Where(d => d.Group == g && modelPrefabs.ContainsKey(d.Name)).ToList();
                if (defs.Count == 0) continue;
                int cell = 256;
                var sheet = new Texture2D(cell * defs.Count, cell, TextureFormat.RGBA32, false);
                for (int i = 0; i < defs.Count; i++)
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(modelPrefabs[defs[i].Name]);
                    var img = Render(prefab, cell, defs[i].Projectile ? 0.35f : 1.45f, defs[i].Projectile ? 0f : 0.35f);
                    sheet.SetPixels(i * cell, 0, cell, cell, img.GetPixels());
                    UnityEngine.Object.DestroyImmediate(img);
                }
                sheet.Apply();
                File.WriteAllBytes(Path.Combine(dir, "models_" + g + ".png"), sheet.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(sheet);
            }
            log.AppendLine("previews: " + dir);
        }

        public static Texture2D Render(GameObject prefab, int size, float ortho, float lookY)
        {
            var pru = new PreviewRenderUtility();
            try
            {
                var cam = pru.camera;
                cam.orthographic = true;
                cam.orthographicSize = ortho;
                cam.nearClipPlane = 0.01f;
                cam.farClipPlane = 100f;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.86f, 0.84f, 0.80f, 1f);
                var rot = Quaternion.Euler(35f, 45f, 0f);
                cam.transform.rotation = rot;
                cam.transform.position = new Vector3(0, lookY, 0) - rot * Vector3.forward * 10f;
                pru.lights[0].intensity = 1.1f;
                pru.lights[0].transform.rotation = Quaternion.Euler(50f, 120f, 0f);
                pru.lights[1].intensity = 0.5f;
                pru.ambientColor = new Color(0.55f, 0.55f, 0.6f);
                var go = UnityEngine.Object.Instantiate(prefab);
                pru.AddSingleGO(go);
                pru.BeginStaticPreview(new Rect(0, 0, size, size));
                pru.Render(true);
                var tex = pru.EndStaticPreview();
                var copy = new Texture2D(tex.width, tex.height, TextureFormat.RGBA32, false);
                copy.SetPixels(tex.GetPixels());
                copy.Apply();
                return copy;
            }
            finally { pru.Cleanup(); }
        }
    }
}
