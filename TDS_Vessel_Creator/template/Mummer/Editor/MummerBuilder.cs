// Mummer asset generator - entry point: Tools > Mummer > Build All Assets.
// Writes every Mummer asset into Assets/MummerMod (exported with the mod) and review material into ModSource/Mummer
// (previews; not exported). Safe to re-run: outputs are overwritten in place so GUIDs and references stay stable.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace MummerBuild
{
    public static class MummerBuilder
    {
        public const string Root = "Assets/MummerMod";
        static string Abs(string assetPath) { return Path.Combine(Path.GetDirectoryName(Application.dataPath), assetPath); }
        static string Source { get { return Path.GetFullPath(Path.Combine(Application.dataPath, "../ModSource/Mummer")); } }

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
        static readonly string[] Groups = { "Loom", "Stage", "Cross", "Curtain", "Extras", "Debuff", "Projectiles" };

        [MenuItem("Tools/Mummer/Build All Assets")]
        public static void BuildAll()
        {
            sheets.Clear(); plainTextures.Clear(); modelPrefabs.Clear(); meshes.Clear(); vfxPrefabs.Clear(); vfxUse.Clear(); audioFiles.Clear(); log.Clear();
            try
            {
                EditorUtility.DisplayProgressBar("Mummer", "Pixel art", 0.05f);
                WriteSprites();
                EditorUtility.DisplayProgressBar("Mummer", "Audio", 0.2f);
                WriteAudio();
                WriteVfxTextures();
                AssetDatabase.Refresh();
                EditorUtility.DisplayProgressBar("Mummer", "Import settings", 0.3f);
                ConfigureImports();
                EditorUtility.DisplayProgressBar("Mummer", "Low-poly models", 0.45f);
                BuildModels();
                EditorUtility.DisplayProgressBar("Mummer", "VFX", 0.65f);
                BuildVfx();
                EditorUtility.DisplayProgressBar("Mummer", "Library + data", 0.75f);
                BuildLibrary();
                WriteData();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                EditorUtility.DisplayProgressBar("Mummer", "Previews", 0.85f);
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
                Debug.Log("[Mummer] build finished: " + Path.Combine(Source, "build_log.txt"));
            }
        }

        // Quick look at the pixel art only: ModSource/Mummer/Previews/wip/*.png, enlarged on a checker.
        [MenuItem("Tools/Mummer/Preview Sprites")]
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
            Save("main", MummerCharacter.MainSheet(), 32);
            Save("attack", MummerCharacter.VariantSheet(MummerCharacter.Attack()), 32);
            Save("hook", MummerCharacter.VariantSheet(MummerCharacter.Hook()), 32);
            Save("fall", MummerCharacter.Strip(MummerCharacter.Fall()), 32);
            Save("land", MummerCharacter.Strip(MummerCharacter.Land()), 32);
            Save("mini", MummerCharacter.MiniSheet(), 16);
            Save("understudy", MummerCharacter.DollSheet("Plain"), 16);
            Save("understudy_ballista", MummerCharacter.DollSheet("Ballista"), 16);
            Save("understudy_aegis", MummerCharacter.DollSheet("Aegis"), 16);
            Save("readyup", MummerCharacter.ReadyUp(), 12);
            Save("tabicon", MummerCharacter.TabIcon(), 32);
            Save("splash", MummerCharacter.Splash(), 128);
            Save("endwin", MummerCharacter.EndWin(), 32);
            Save("endlose", MummerCharacter.EndLose(), 32);
            Debug.Log("[Mummer] sprite previews in " + dir);
        }

        // Quick look at the models only (no assets written): ModSource/Mummer/Previews/wip/models_<Group>.png.
        [MenuItem("Tools/Mummer/Preview Models")]
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
                var defs = MummerModels.All().Where(d => d.Group == g).ToList();
                if (defs.Count == 0) continue;
                int cell = 256;
                var sheet = new Texture2D(cell * defs.Count, cell, TextureFormat.RGBA32, false);
                for (int i = 0; i < defs.Count; i++)
                {
                    var lp = defs[i].Build();
                    var mesh = lp.ToMesh(defs[i].Name);
                    if (defs[i].Projectile) Recenter(mesh);
                    var go = new GameObject(defs[i].Name);
                    go.AddComponent<MeshFilter>().sharedMesh = mesh;
                    go.AddComponent<MeshRenderer>().sharedMaterial = mat;
                    var img = Render(go, cell, defs[i].Projectile ? 0.25f : 0.9f, defs[i].Projectile ? 0f : 0.05f);
                    sheet.SetPixels(i * cell, 0, cell, cell, img.GetPixels());
                    UnityEngine.Object.DestroyImmediate(img);
                    UnityEngine.Object.DestroyImmediate(go);
                    UnityEngine.Object.DestroyImmediate(mesh);
                    log.AppendLine(defs[i].Name + " tris " + lp.Triangles);
                }
                sheet.Apply();
                File.WriteAllBytes(Path.Combine(dir, "models_" + g + ".png"), sheet.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(sheet);
            }
            UnityEngine.Object.DestroyImmediate(mat);
            UnityEngine.Object.DestroyImmediate(pal);
            File.WriteAllText(Path.Combine(dir, "models_log.txt"), log.ToString());
            Debug.Log("[Mummer] model previews in " + dir);
        }

        // ================================================================ pixel art

        static void SaveSheet(Px px, string folder, string name, int cw, int ch, Vector2 pivot)
        {
            string path = Root + "/Art/" + folder + "/" + name + ".png";
            MummerAseprite.Apply(px, folder, name, cw).Save(Abs(path));
            sheets.Add(new SheetInfo { Path = path, Name = name, CellW = cw, CellH = ch, Pivot = pivot });
        }

        static void WriteSprites()
        {
            MummerAseprite.Begin("Mummer", Path.Combine(Source, "Aseprite"), log);
            var foot = new Vector2(0.5f, 0f);
            SaveSheet(MummerCharacter.MainSheet(), "Vessel", "Mummer-Sheet", 32, 32, foot);
            SaveSheet(MummerCharacter.VariantSheet(MummerCharacter.Attack()), "Vessel", "MummerAttack-Sheet", 32, 32, foot);
            SaveSheet(MummerCharacter.VariantSheet(MummerCharacter.Hook()), "Vessel", "MummerHook-Sheet", 32, 32, foot);
            SaveSheet(MummerCharacter.Strip(MummerCharacter.Fall()), "Vessel", "MummerFall-Sheet", 32, 32, foot);
            SaveSheet(MummerCharacter.Strip(MummerCharacter.Land()), "Vessel", "MummerIntroR-Sheet", 32, 32, foot);
            SaveSheet(MummerCharacter.MiniSheet(), "UI", "SMummerSheet", 16, 16, foot);
            SaveSheet(MummerCharacter.Strip(MummerCharacter.IdleStrip()), "UI", "MummerIdlejustidle-Sheet", 32, 32, foot);
            SaveSheet(MummerCharacter.DollSheet("Plain"), "Unit", "Understudy-Sheet", 16, 16, foot);
            SaveSheet(MummerCharacter.DollSheet("Ballista"), "Unit", "UnderstudyBallista-Sheet", 16, 16, foot);
            SaveSheet(MummerCharacter.DollSheet("Aegis"), "Unit", "UnderstudyAegis-Sheet", 16, 16, foot);
            var c = new Vector2(0.5f, 0.5f);
            SaveSheet(MummerCharacter.Icon(), "UI", "Mummer_Icon", 32, 32, c);
            SaveSheet(MummerCharacter.Splash(), "UI", "Mummersplash", 128, 128, c);
            SaveSheet(MummerCharacter.EndWin(), "UI", "Mummerendstat1", 32, 32, c);
            SaveSheet(MummerCharacter.EndLose(), "UI", "Mummerendstat2", 32, 32, c);
            SaveSheet(MummerCharacter.ReadyUp(), "UI", "Readyup6mummer", 12, 12, c);
            SaveSheet(MummerCharacter.TabIcon(), "UI", "Mummertabicon", 32, 32, c);
            log.AppendLine("sprites: " + sheets.Count + " sheets");
            MummerAseprite.WriteGenerated();
        }

        static void ConfigureImports()
        {
            foreach (var s in sheets)
            {
                AssetDatabase.ImportAsset(s.Path, ImportAssetOptions.ForceSynchronousImport);
                var ti = (TextureImporter)AssetImporter.GetAtPath(s.Path);
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(s.Path);
                int w = tex.width, h = tex.height;
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
            string palPath = Root + "/Art/Models/Mummer_Palette.png";
            var palTex = Pal.Texture();
            Directory.CreateDirectory(Path.GetDirectoryName(Abs(palPath)));
            File.WriteAllBytes(Abs(palPath), palTex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(palTex);
            ConfigurePlain(palPath);
            var palAsset = AssetDatabase.LoadAssetAtPath<Texture2D>(palPath);

            string matPath = Root + "/Art/Models/Mummer_Palette.mat";
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

            foreach (var def in MummerModels.All())
            {
                var lp = def.Build();
                var mesh = lp.ToMesh(def.Name);
                if (def.Projectile) Recenter(mesh);
                string meshPath = Root + "/Art/Models/" + def.Group + "/" + def.Name + ".asset";
                Directory.CreateDirectory(Path.GetDirectoryName(Abs(meshPath)));
                var existing = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                if (existing != null)
                {
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
                    mesh = existing;
                }
                else AssetDatabase.CreateAsset(mesh, meshPath);
                meshes[def.Name] = meshPath;
                var b = mesh.bounds;
                log.AppendLine("model " + def.Name + " tris=" + lp.Triangles + " verts=" + mesh.vertexCount + " size=" + b.size.ToString("F2") + " center=" + b.center.ToString("F2"));
                if (def.Group == "Debuff")
                    continue;   // debuff meshes live inside their VFX prefabs

                string prefabPath = Root + "/Prefabs/" + (def.Projectile ? "Projectiles" : "Towers/" + def.Group) + "/" + def.Name + ".prefab";
                Directory.CreateDirectory(Path.GetDirectoryName(Abs(prefabPath)));
                var root = new GameObject(def.Name);
                var vis = new GameObject("Vis");
                vis.transform.SetParent(root.transform, false);
                var exp = new GameObject(def.Name + "_Export");
                exp.transform.SetParent(vis.transform, false);
                exp.AddComponent<MeshFilter>().sharedMesh = mesh;
                exp.AddComponent<MeshRenderer>().sharedMaterial = paletteMat;
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                UnityEngine.Object.DestroyImmediate(root);
                modelPrefabs[def.Name] = prefabPath;
            }
            AssetDatabase.SaveAssets();
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
            foreach (var s in MummerAudio.All())
            {
                string path = Root + "/Audio/" + s.Name + ".wav";
                MummerAudio.SaveWav(Abs(path), s.Make());
                audioFiles[s.Name] = path;
            }
            log.AppendLine("audio: " + audioFiles.Count + " clips");
        }

        // ================================================================ VFX

        static readonly Dictionary<string, string[]> VfxTex = new Dictionary<string, string[]>
        {
            { "Px_Square", new[] { "##", "##" } },
            { "Px_Tri", new[] { "#####", ".###.", ".###.", "..#..", "..#.." } },
            { "Px_Fluff", new[] { ".##.", "####", "####", ".##." } },
            { "Px_Star", new[] { "..#..", ".###.", "#####", ".###.", "..#.." } },
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
            string path = Root + "/Art/VFX/Mummer_" + tex + ".mat";
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
                // Debuffs: attached to a foe by the game itself (Vfxdebuffcheck by name), so every player sees them.
                new VfxSpec { Name = "Debuff_Mummermarked", Use = "Marked foe (HOOK, Spotlight): a plum triangle overhead", Mesh = "Mummer_Marker", Tex = "Px_Star", A = H("F6B3C1"), B = H("FFFFFF"), Burst = 0, Rate = 3f, Loop = true, Speed0 = 0.05f, Speed1 = 0.15f, Life0 = 0.5f, Life1 = 0.8f, Size0 = 0.05f, Size1 = 0.08f, Radius = 0.16f, Height = 1.3f },
                new VfxSpec { Name = "Debuff_Mummerheld", Use = "Held by a Marionette Cross: the control and strings above the foe", Mesh = "Mummer_Heldstrings", NoParticles = true },
                new VfxSpec { Name = "Debuff_Mummertangled", Use = "Tangled by a Curtain: thread loops around the foe's feet", Mesh = "Mummer_Tangle", Tex = "Px_Square", A = H("C6AEE3"), B = H("EE8597"), Burst = 0, Rate = 4f, Loop = true, Speed0 = 0.05f, Speed1 = 0.2f, Life0 = 0.4f, Life1 = 0.7f, Size0 = 0.03f, Size1 = 0.05f, Radius = 0.2f, Height = -0.4f },
                // One-shots and movers played by MummerClient.
                new VfxSpec { Name = "World_Mummer_Scrappop", Use = "A Scrap pops off a felled foe", Tex = "Px_Tri", A = H("CBB4E7"), B = H("F2A6BE"), Burst = 10, Speed0 = 0.8f, Speed1 = 1.6f, Gravity = 1f, Life0 = 0.4f, Life1 = 0.7f, Size0 = 0.07f, Size1 = 0.11f, Spin = true, Hemisphere = true },
                new VfxSpec { Name = "World_Mummer_Scrapfly", Use = "A Scrap flying to its Loom (moved by the client)", Tex = "Px_Tri", A = H("CBB4E7"), B = H("FFFFFF"), Burst = 2, Rate = 30f, Loop = true, Speed0 = 0f, Speed1 = 0.05f, Life0 = 0.2f, Life1 = 0.3f, Size0 = 0.06f, Size1 = 0.1f, Spin = true, Radius = 0.02f },
                new VfxSpec { Name = "World_Mummer_Stitch", Use = "Stitching sparkle", Tex = "Px_Star", A = H("EADFF6"), B = H("FFFFFF"), Burst = 12, Speed0 = 0.3f, Speed1 = 0.8f, Gravity = -0.2f, Life0 = 0.4f, Life1 = 0.8f, Size0 = 0.05f, Size1 = 0.09f, Radius = 0.25f },
                new VfxSpec { Name = "World_Mummer_Barb", Use = "The HOOK's barb in flight (moved by the client)", Tex = "Px_Star", A = H("EDC867"), B = H("FFFFFF"), Burst = 1, Rate = 40f, Loop = true, Speed0 = 0f, Speed1 = 0.05f, Life0 = 0.15f, Life1 = 0.25f, Size0 = 0.05f, Size1 = 0.08f, Radius = 0.01f },
                new VfxSpec { Name = "World_Mummer_Flail", Use = "A held foe strikes its neighbour", Tex = "Px_Star", A = H("FFFFFF"), B = H("F8E39A"), Burst = 7, Speed0 = 0.8f, Speed1 = 1.4f, Gravity = 0.4f, Life0 = 0.15f, Life1 = 0.3f, Size0 = 0.05f, Size1 = 0.08f, Hemisphere = true, Radius = 0.08f },
                new VfxSpec { Name = "World_Mummer_Velvet", Use = "A foe pushes through a Curtain", Tex = "Px_Square", A = H("DA6278"), B = H("93407A"), Burst = 10, Speed0 = 0.4f, Speed1 = 0.9f, Gravity = 0.6f, Life0 = 0.4f, Life1 = 0.7f, Size0 = 0.04f, Size1 = 0.07f, Spin = true, Radius = 0.2f },
                new VfxSpec { Name = "World_Mummer_Finalact", Use = "Final Act: a dust ring as the curtain drops (scaled to its radius)", Tex = "Px_Fluff", A = H("E2D5BC"), B = H("DA6278"), Burst = 48, Speed0 = 0.05f, Speed1 = 0.2f, Gravity = -0.05f, Life0 = 0.7f, Life1 = 1.1f, Size0 = 0.12f, Size1 = 0.22f, Radius = 1f, Edge = true, ShapeScale = true, Grow = true },
                new VfxSpec { Name = "World_Mummer_Confetti", Use = "A costume is unlocked or changed", Tex = "Px_Tri", A = H("F2A6BE"), B = H("9BBDEE"), Burst = 26, Speed0 = 1.2f, Speed1 = 2.2f, Gravity = 1.1f, Life0 = 0.8f, Life1 = 1.3f, Size0 = 0.05f, Size1 = 0.09f, Spin = true, Hemisphere = true, Radius = 0.15f },
                new VfxSpec { Name = "World_Mummer_Unravel", Use = "Thread unravels at a new Tide", Tex = "Px_Square", A = H("CBB4E7"), B = H("EADFF6"), Burst = 16, Speed0 = 0.2f, Speed1 = 0.6f, Gravity = 0.5f, Life0 = 0.8f, Life1 = 1.2f, Size0 = 0.03f, Size1 = 0.05f, Spin = true, Radius = 0.3f },
                new VfxSpec { Name = "World_Mummer_Stuffing", Use = "An Understudy or building falls: a puff of stuffing", Tex = "Px_Fluff", A = H("FFFFFF"), B = H("F2E9D6"), Burst = 14, Speed0 = 0.6f, Speed1 = 1.3f, Gravity = 0.3f, Life0 = 0.5f, Life1 = 0.9f, Size0 = 0.08f, Size1 = 0.15f, Hemisphere = true, Grow = true, Radius = 0.15f },
                new VfxSpec { Name = "World_Mummer_Finale", Use = "Grand Finale: an Understudy bursts", Tex = "Px_Fluff", A = H("FFFFFF"), B = H("F6B3C1"), Burst = 30, Speed0 = 1.4f, Speed1 = 2.6f, Gravity = 0.6f, Life0 = 0.5f, Life1 = 0.9f, Size0 = 0.1f, Size1 = 0.18f, Hemisphere = true, Grow = true, Radius = 0.2f },
                new VfxSpec { Name = "World_Hero_Mummer_Death", Use = "Vessel death: scraps of costume and stuffing", Tex = "Px_Tri", A = H("CBB4E7"), B = H("FFFFFF"), Burst = 28, Speed0 = 1.4f, Speed1 = 2.6f, Gravity = 1.2f, Life0 = 0.8f, Life1 = 1.3f, Size0 = 0.07f, Size1 = 0.13f, Spin = true, Hemisphere = true },
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
            var root = new GameObject("MummerAssets_Library");
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
            string path = Root + "/Prefabs/MummerAssets_Library.prefab";
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
            j.AppendLine("  \"mod\": \"Mummer\", \"generatedBy\": \"Tools/Mummer/Build All Assets (Assets/MummerMod/Editor)\", \"design\": \"ModSource/Mummer/DESIGN.md\",");
            j.AppendLine("  \"conventions\": { \"spritePixelsPerUnit\": 20, \"spritePivot\": [0.5, 0], \"filter\": \"Point\", \"modelStyle\": \"sleek low-poly, flat-shaded, palette UVs (like Sno Sno and Buster)\", \"groundY\": -0.5, \"paletteTexture\": \"Art/Models/Mummer_Palette.png\" },");
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
            var sounds = MummerAudio.All();
            for (int i = 0; i < sounds.Count; i++) j.AppendLine("    { \"id\": " + Q(sounds[i].Name) + ", \"file\": " + Q(audioFiles[sounds[i].Name]) + ", \"use\": " + Q(sounds[i].Use) + " }" + (i < sounds.Count - 1 ? "," : ""));
            j.AppendLine("  ]");
            j.AppendLine("}");
            string manifest = Root + "/Data/Mummer_Manifest.json";
            Directory.CreateDirectory(Path.GetDirectoryName(Abs(manifest)));
            File.WriteAllText(Abs(manifest), j.ToString());

            // Localization: the game uses the English text as the key, so English rows are enough to show correctly.
            var csv = new StringBuilder();
            csv.AppendLine("Note,Key,English");
            void Row(string note, string text) { csv.AppendLine(Csv(note) + "," + Csv(text) + "," + Csv(text)); }
            Row("\"Mummer\" Vessel proper name. A hand-sewn costume with something small inside.", "Mummer");
            Row("Vessel title", "Strings Attached");
            Row("Vessel rule name", "The Show Must Go On");
            Row("\"Thread\" Mummer's resource, spun from Scraps", "Thread");
            Row("\"Scraps\" left by foes felled near a Loom", "Scraps");
            Row("\"HOOK\" action button", "HOOK");
            Row("\"COSTUME\" stage button", "COSTUME");
            Row("Buildables", "Loom"); Row("", "Puppet Stage"); Row("", "Marionette Cross"); Row("", "Curtain");
            Row("Units", "Understudy"); Row("", "Ballista Understudy"); Row("", "Aegis Understudy");
            string loc = Root + "/Data/Mummer_Localization.csv";
            File.WriteAllText(Abs(loc), csv.ToString(), new UTF8Encoding(false));
            log.AppendLine("data: manifest + localization");
        }

        // ================================================================ previews (ModSource/Mummer/Previews, not exported)

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
                var defs = MummerModels.All().Where(d => d.Group == g && modelPrefabs.ContainsKey(d.Name)).ToList();
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
