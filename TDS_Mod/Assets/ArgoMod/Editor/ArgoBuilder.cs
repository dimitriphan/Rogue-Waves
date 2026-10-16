// Argo asset generator - entry point: Tools > Argo > Build All Assets.
// Writes every Argo asset into Assets/ArgoMod (exported with the mod) and review material into ModSource/Argo
// (previews; not exported). Safe to re-run: outputs are overwritten in place so GUIDs and references stay stable.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace ArgoBuild
{
    public static class ArgoBuilder
    {
        public const string Root = "Assets/ArgoMod";
        static string Abs(string assetPath) { return Path.Combine(Path.GetDirectoryName(Application.dataPath), assetPath); }
        static string Source { get { return ArgoPreview.Source; } }

        class SheetInfo
        {
            public string Path, Name;
            public int CellW, CellH;
            public Vector2 Pivot;
        }

        static readonly List<SheetInfo> sheets = new List<SheetInfo>();
        static readonly List<string> plainTextures = new List<string>();
        static readonly Dictionary<string, string> modelPrefabs = new Dictionary<string, string>();
        static readonly Dictionary<string, string> vfxPrefabs = new Dictionary<string, string>();
        static readonly Dictionary<string, string> vfxUse = new Dictionary<string, string>();
        static readonly Dictionary<string, string> audioFiles = new Dictionary<string, string>();
        static readonly StringBuilder log = new StringBuilder();

        [MenuItem("Tools/Argo/Build All Assets")]
        public static void BuildAll()
        {
            sheets.Clear(); plainTextures.Clear(); modelPrefabs.Clear(); vfxPrefabs.Clear(); vfxUse.Clear(); audioFiles.Clear(); log.Clear();
            try
            {
                EditorUtility.DisplayProgressBar("Argo", "Pixel art", 0.05f);
                WriteSprites();
                EditorUtility.DisplayProgressBar("Argo", "Audio", 0.2f);
                WriteAudio();
                EditorUtility.DisplayProgressBar("Argo", "Particle textures", 0.25f);
                WriteVfxTextures();
                AssetDatabase.Refresh();
                EditorUtility.DisplayProgressBar("Argo", "Import settings", 0.3f);
                ConfigureImports();
                EditorUtility.DisplayProgressBar("Argo", "Palette + low-poly models", 0.45f);
                BuildModels();
                EditorUtility.DisplayProgressBar("Argo", "VFX", 0.65f);
                BuildVfx();
                EditorUtility.DisplayProgressBar("Argo", "Library + data", 0.75f);
                BuildLibrary();
                WriteData();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                EditorUtility.DisplayProgressBar("Argo", "Previews", 0.85f);
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
                Debug.Log("[Argo] build finished: " + Path.Combine(Source, "build_log.txt"));
            }
        }

        // Quick look at the models only (no assets written): ModSource/Argo/Previews/wip/models_<Group>.png.
        [MenuItem("Tools/Argo/Preview Models")]
        public static void PreviewModels()
        {
            string dir = Path.Combine(Source, "Previews", "wip");
            Directory.CreateDirectory(dir);
            var pal = Pal.Texture();
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.SetTexture("_BaseMap", pal);
            mat.SetFloat("_Smoothness", 0.05f);
            foreach (var g in new[] { "Scatter", "Harpoon", "Bell", "Drill", "Water", "Extras", "Projectiles" })
            {
                var defs = ArgoModels.All().Where(d => d.Group == g).ToList();
                var go = new List<GameObject>();
                foreach (var d in defs) go.Add(Assemble(d, mat, null));
                Grid(dir, "models_" + g, go, g == "Projectiles" ? 0.3f : 0.62f, g == "Projectiles" ? 0f : -0.05f);
                foreach (var o in go) UnityEngine.Object.DestroyImmediate(o);
            }
            UnityEngine.Object.DestroyImmediate(mat);
            UnityEngine.Object.DestroyImmediate(pal);
            Debug.Log("[Argo] model previews in " + dir);
        }

        // ================================================================ pixel art

        static void SaveSheet(Px px, string folder, string name, int cw, int ch, Vector2 pivot)
        {
            string path = Root + "/Art/" + folder + "/" + name + ".png";
            ArgoAseprite.Apply(px, folder, name, cw).Save(Abs(path));
            sheets.Add(new SheetInfo { Path = path, Name = name, CellW = cw, CellH = ch, Pivot = pivot });
        }

        static void WriteSprites()
        {
            ArgoAseprite.Begin("Argo", Path.Combine(Source, "Aseprite"), log);
            var foot = new Vector2(0.5f, 0f);
            SaveSheet(ArgoCharacter.MainSheet(), "Vessel", "Argo-Sheet", 32, 32, foot);
            SaveSheet(ArgoCharacter.VariantSheet(ArgoCharacter.Attack()), "Vessel", "ArgoAttack-Sheet", 32, 32, foot);
            SaveSheet(ArgoCharacter.VariantSheet(ArgoCharacter.Sonar()), "Vessel", "ArgoSonar-Sheet", 32, 32, foot);
            SaveSheet(ArgoCharacter.Strip(ArgoCharacter.Fall()), "Vessel", "ArgoFall-Sheet", 32, 32, foot);
            SaveSheet(ArgoCharacter.Strip(ArgoCharacter.Land()), "Vessel", "ArgoIntroR-Sheet", 32, 32, foot);
            SaveSheet(ArgoCharacter.Strip(ArgoCharacter.IdleStrip()), "UI", "ArgoIdlejustidle-Sheet", 32, 32, foot);
            SaveSheet(ArgoCharacter.MiniSheet(), "UI", "SArgoSheet", 16, 16, foot);
            foreach (var crew in new[] { new[] { "Port", "r", "R" }, new[] { "Starboard", "e", "E" }, new[] { "Bosun", "u", "U" } })
            {
                SaveSheet(ArgoCharacter.NautSheet(crew[1][0], crew[2][0]), "Unit", "Naut_" + crew[0] + "-Sheet", 16, 16, foot);
                SaveSheet(ArgoCharacter.NautTuneSheet(crew[1][0], crew[2][0]), "Unit", "NautTune_" + crew[0] + "-Sheet", 16, 16, foot);
            }
            var c = new Vector2(0.5f, 0.5f);
            SaveSheet(ArgoCharacter.Icon(), "UI", "Argo_Icon", 32, 32, c);
            SaveSheet(ArgoCharacter.Splash(), "UI", "Argosplash", 128, 128, c);
            SaveSheet(ArgoCharacter.EndWin(), "UI", "Argoendstat1", 32, 32, c);
            SaveSheet(ArgoCharacter.EndLose(), "UI", "Argoendstat2", 32, 32, c);
            SaveSheet(ArgoCharacter.ReadyUp(), "UI", "Readyup6argo", 12, 12, c);
            SaveSheet(ArgoCharacter.TabIcon(), "UI", "Argotabicon", 32, 32, c);
            log.AppendLine("sprites: " + sheets.Count + " sheets");
            ArgoAseprite.WriteGenerated();
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
                // Decompressed at load so the mod can measure each clip's loudness (ArgoAssets.Volume).
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

        // ================================================================ low-poly models (ArgoModels / ArgoLowpoly)

        static Material paletteMat;

        static Mesh SaveMesh(Mesh mesh, string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Abs(path)));
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

        // Name / Vis / Name_Export (the static mesh), plus Vis / <Part> at the part's pivot holding Name_<Part>_Export.
        // 'save' writes the meshes as assets (null: in-memory only, for previews).
        static GameObject Assemble(ModelDef def, Material mat, string save)
        {
            var root = new GameObject(def.Name);
            var vis = new GameObject("Vis");
            vis.transform.SetParent(root.transform, false);
            var lp = def.Build();
            var mesh = lp.ToMesh(def.Name, Vector3.zero, def.Scale);
            if (def.Projectile) Recenter(mesh);
            if (save != null) mesh = SaveMesh(mesh, save + "/" + def.Name + ".asset");
            var exp = new GameObject(def.Name + "_Export");
            exp.transform.SetParent(vis.transform, false);
            exp.AddComponent<MeshFilter>().sharedMesh = mesh;
            exp.AddComponent<MeshRenderer>().sharedMaterial = mat;
            int tris = lp.Triangles;
            Mesh partMesh = null;
            Vector3 partPivot = Vector3.zero;
            if (def.Part != null)
            {
                Vector3 pivot = Lp.Scaled(def.PartPivot + new Vector3(0f, Lp.Ground, 0f), def.Scale);
                var plp = def.Part();
                var pmesh = plp.ToMesh(def.Name + "_" + def.PartName, pivot, def.Scale);
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
                partPivot = pivot;
            }
            // The journal's 3D preview (UIObject3D) fits a model to its frame by the first renderer it finds. The shipped
            // Mining Hut and Wall carry a disabled renderer on "Vis" for that; Argo's holds the whole tower (base and
            // moving part combined), so towers with a turret are framed whole instead of by their dock.
            if (!def.Projectile)
            {
                var whole = new Mesh { name = def.Name + "_Whole" };
                var parts = new List<CombineInstance> { new CombineInstance { mesh = mesh, transform = Matrix4x4.identity } };
                if (partMesh != null)
                    parts.Add(new CombineInstance { mesh = partMesh, transform = Matrix4x4.Translate(partPivot) });
                whole.CombineMeshes(parts.ToArray(), true, true);
                whole.RecalculateBounds();
                if (save != null) whole = SaveMesh(whole, save + "/" + def.Name + "_Whole.asset");
                vis.AddComponent<MeshFilter>().sharedMesh = whole;
                var proxy = vis.AddComponent<MeshRenderer>();
                proxy.sharedMaterial = mat;
                proxy.enabled = false;
            }
            if (save != null)
                log.AppendLine("model " + def.Name + " tris=" + tris + " size=" + mesh.bounds.size.ToString("F2") + (def.Part != null ? " + " + def.PartName : ""));
            return root;
        }

        static void BuildModels()
        {
            string palPath = Root + "/Art/Models/Argo_Palette.png";
            var palTex = Pal.Texture();
            Directory.CreateDirectory(Path.GetDirectoryName(Abs(palPath)));
            File.WriteAllBytes(Abs(palPath), palTex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(palTex);
            ConfigurePlain(palPath);
            var palAsset = AssetDatabase.LoadAssetAtPath<Texture2D>(palPath);

            string matPath = Root + "/Art/Models/Argo_Palette.mat";
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

            foreach (var def in ArgoModels.All())
            {
                string folder = Root + "/Art/Models/" + def.Group;
                var root = Assemble(def, paletteMat, folder);
                string prefabPath = Root + "/Prefabs/" + (def.Projectile ? "Projectiles" : "Towers/" + def.Group) + "/" + def.Name + ".prefab";
                Directory.CreateDirectory(Path.GetDirectoryName(Abs(prefabPath)));
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                UnityEngine.Object.DestroyImmediate(root);
                modelPrefabs[def.Name] = prefabPath;
            }
            AssetDatabase.SaveAssets();
        }

        // ================================================================ audio

        static void WriteAudio()
        {
            foreach (var s in ArgoAudio.All())
            {
                string path = Root + "/Audio/" + s.Name + ".wav";
                ArgoAudio.SaveWav(Abs(path), s.Make());
                audioFiles[s.Name] = path;
            }
            log.AppendLine("audio: " + audioFiles.Count + " clips");
        }

        // ================================================================ VFX

        static readonly Dictionary<string, string[]> VfxTex = new Dictionary<string, string[]>
        {
            { "Px_Square", new[] { "##", "##" } },
            { "Px_Bubble", new[] { ".##.", "#..#", "#..#", ".##." } },
            { "Px_Spark", new[] { ".#.", "###", ".#." } },
            { "Px_Drop", new[] { ".#.", "###", "###" } },
            { "Px_Ring", new[] { "..###..", ".#...#.", "#.....#", "#.....#", "#.....#", ".#...#.", "..###.." } },
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
            string path = Root + "/Art/VFX/Argo_" + tex + ".mat";
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
            public string Name, Tex = "Px_Square", Use;
            public Color A, B;
            public int Burst = 12;
            public float Rate, Life0 = 0.5f, Life1 = 0.9f, Speed0 = 0.5f, Speed1 = 1f, Size0 = 0.06f, Size1 = 0.1f, Gravity, Radius = 0.2f, Height, Duration = 1f;
            public bool Loop, Spin, Grow, Hemisphere, Edge, Ring, Cone;
        }

        // Which way Unity's Hemisphere shape faces: emit a few particles and see where they go, then return the shape
        // rotation that turns the dome upward (Unity's Hemisphere faces +Z; see Sprigg v1.3).
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
            var up = HemisphereUp();
            Color H(string hex) { Color c; ColorUtility.TryParseHtmlString("#" + hex, out c); return c; }
            var specs = new List<VfxSpec>
            {
                new VfxSpec { Name = "World_Argo_Clang", Use = "The wrench swing carries into a second enemy", Tex = "Px_Spark", A = H("FFF6C8"), B = H("F2CF7A"), Burst = 10, Speed0 = 1.2f, Speed1 = 2.2f, Gravity = 1.2f, Life0 = 0.2f, Life1 = 0.35f, Size0 = 0.05f, Size1 = 0.08f, Hemisphere = true },
                // Rings: drawn at radius 1 and scaled by the client to the server's radius (local simulation, Hierarchy scaling).
                new VfxSpec { Name = "World_Argo_Sonarring", Use = "SONAR ring (scaled to the ping radius)", Tex = "Px_Square", A = H("7AE6EF"), B = H("DFF5FA"), Burst = 90, Speed0 = 0.95f, Speed1 = 1f, Life0 = 1f, Life1 = 1f, Size0 = 0.06f, Size1 = 0.08f, Radius = 0.02f, Ring = true },
                new VfxSpec { Name = "World_Argo_Quake", Use = "Quake ring of dust (scaled to the Tremor radius)", Tex = "Px_Square", A = H("8A7A5A"), B = H("B7A274"), Burst = 70, Speed0 = 0.9f, Speed1 = 1f, Life0 = 0.9f, Life1 = 1f, Size0 = 0.07f, Size1 = 0.12f, Radius = 0.02f, Ring = true, Spin = true },
                new VfxSpec { Name = "World_Argo_Pulse", Use = "Aftershock ring (scaled to the Tremor radius)", Tex = "Px_Square", A = H("A85F3A"), B = H("8A7A5A"), Burst = 40, Speed0 = 0.95f, Speed1 = 1f, Life0 = 0.8f, Life1 = 0.9f, Size0 = 0.05f, Size1 = 0.08f, Radius = 0.02f, Ring = true },
                new VfxSpec { Name = "World_Argo_Tremor", Use = "Looping dust on the edge of a Deep Drill's Tremors (scaled to its radius)", Tex = "Px_Square", A = H("8A7A5A"), B = H("6A5C40"), Burst = 0, Rate = 26f, Loop = true, Speed0 = 0.05f, Speed1 = 0.15f, Gravity = -0.05f, Life0 = 0.6f, Life1 = 1f, Size0 = 0.04f, Size1 = 0.07f, Radius = 1f, Edge = true, Ring = false },
                new VfxSpec { Name = "World_Argo_Ping", Use = "Looping cyan blips over a Pinged enemy", Tex = "Px_Ring", A = H("7AE6EF"), B = H("3FB4C4"), Burst = 1, Rate = 2.5f, Loop = true, Speed0 = 0.15f, Speed1 = 0.25f, Gravity = -0.1f, Life0 = 0.6f, Life1 = 0.8f, Size0 = 0.16f, Size1 = 0.2f, Radius = 0.05f, Grow = true },
                new VfxSpec { Name = "World_Argo_Muzzle", Use = "Scatter Cannon muzzle smoke", Tex = "Px_Square", A = H("E9E1C8"), B = H("9FB3B8"), Burst = 14, Speed0 = 0.8f, Speed1 = 1.8f, Gravity = -0.2f, Life0 = 0.3f, Life1 = 0.6f, Size0 = 0.07f, Size1 = 0.13f, Radius = 0.05f, Cone = true, Grow = true },
                new VfxSpec { Name = "World_Argo_Smokering", Use = "Broadside: smoke all around the cannon", Tex = "Px_Square", A = H("E9E1C8"), B = H("9FB3B8"), Burst = 40, Speed0 = 1f, Speed1 = 1.6f, Gravity = -0.2f, Life0 = 0.4f, Life1 = 0.7f, Size0 = 0.08f, Size1 = 0.14f, Radius = 0.25f, Grow = true, Height = 0.15f },
                new VfxSpec { Name = "World_Argo_Lanceflash", Use = "Leviathan Lance flash", Tex = "Px_Spark", A = H("7AE6EF"), B = H("FFFFFF"), Burst = 22, Speed0 = 1f, Speed1 = 2.5f, Life0 = 0.25f, Life1 = 0.45f, Size0 = 0.06f, Size1 = 0.1f, Hemisphere = true },
                new VfxSpec { Name = "World_Argo_Bleed", Use = "Barbed bleed tick", Tex = "Px_Drop", A = H("DE5456"), B = H("A93A3E"), Burst = 5, Speed0 = 0.2f, Speed1 = 0.5f, Gravity = 1.2f, Life0 = 0.4f, Life1 = 0.6f, Size0 = 0.04f, Size1 = 0.06f, Radius = 0.1f, Height = 0.4f },
                new VfxSpec { Name = "World_Argo_Drilldust", Use = "A Deep Drill bites ore", Tex = "Px_Square", A = H("8A7A5A"), B = H("8FD8F5"), Burst = 10, Speed0 = 0.6f, Speed1 = 1.2f, Gravity = 1.1f, Life0 = 0.35f, Life1 = 0.6f, Size0 = 0.04f, Size1 = 0.07f, Radius = 0.15f, Hemisphere = true },
                new VfxSpec { Name = "World_Argo_Gusher", Use = "Gusher: a burst of ore", Tex = "Px_Square", A = H("8FD8F5"), B = H("DFF5FA"), Burst = 36, Speed0 = 2f, Speed1 = 3.2f, Gravity = 1.4f, Life0 = 0.8f, Life1 = 1.2f, Size0 = 0.06f, Size1 = 0.1f, Radius = 0.1f, Cone = true, Spin = true },
                new VfxSpec { Name = "World_Argo_Fathom", Use = "A Deep Drill sinks a Fathom", Tex = "Px_Bubble", A = H("7AE6EF"), B = H("DFF5FA"), Burst = 24, Speed0 = 0.3f, Speed1 = 0.8f, Gravity = -0.3f, Life0 = 0.8f, Life1 = 1.4f, Size0 = 0.06f, Size1 = 0.11f, Radius = 0.3f },
                new VfxSpec { Name = "World_Argo_Bubbles", Use = "Bubbles: a building lands, a Naut dives", Tex = "Px_Bubble", A = H("DFF5FA"), B = H("9FD8EC"), Burst = 18, Speed0 = 0.3f, Speed1 = 0.7f, Gravity = -0.4f, Life0 = 0.8f, Life1 = 1.3f, Size0 = 0.05f, Size1 = 0.1f, Radius = 0.3f },
                new VfxSpec { Name = "World_Argo_Wreck", Use = "An Argo building is destroyed", Tex = "Px_Square", A = H("D9A84A"), B = H("7F8C97"), Burst = 22, Speed0 = 1f, Speed1 = 2f, Gravity = 1f, Life0 = 0.6f, Life1 = 1f, Size0 = 0.06f, Size1 = 0.11f, Spin = true, Hemisphere = true },
                new VfxSpec { Name = "World_Hero_Argo_Death", Use = "Vessel death", Tex = "Px_Bubble", A = H("DFF5FA"), B = H("F2CF7A"), Burst = 30, Speed0 = 1f, Speed1 = 2.2f, Gravity = -0.2f, Life0 = 0.9f, Life1 = 1.5f, Size0 = 0.07f, Size1 = 0.13f, Hemisphere = true },
                new VfxSpec { Name = "World_Argo_Tunespark", Use = "A Naut starts tuning or repairs a tower", Tex = "Px_Spark", A = H("FFF6C8"), B = H("F2CF7A"), Burst = 9, Speed0 = 0.6f, Speed1 = 1.3f, Gravity = 0.8f, Life0 = 0.25f, Life1 = 0.45f, Size0 = 0.04f, Size1 = 0.07f, Hemisphere = true },
                new VfxSpec { Name = "World_Argo_Tuned", Use = "Looping sparks on a tower a Naut is tuning", Tex = "Px_Spark", A = H("FFE07A"), B = H("7AE6EF"), Burst = 0, Rate = 3f, Loop = true, Speed0 = 0.2f, Speed1 = 0.45f, Gravity = -0.1f, Life0 = 0.5f, Life1 = 0.8f, Size0 = 0.04f, Size1 = 0.06f, Radius = 0.22f },
                // 2.0: Deep Water, pins, the reel path and the Drill Rig's new upgrades
                new VfxSpec { Name = "World_Argo_Flood", Use = "FLOOD adds a pool; Abyss or Trench adds a free pool", Tex = "Px_Drop", A = H("8FC6E0"), B = H("DFF5FA"), Burst = 28, Speed0 = 1f, Speed1 = 2f, Gravity = 1.4f, Life0 = 0.5f, Life1 = 0.8f, Size0 = 0.05f, Size1 = 0.09f, Radius = 0.3f, Hemisphere = true },
                new VfxSpec { Name = "World_Argo_Wet", Use = "Looping drips on a foe slowed by Deep Water", Tex = "Px_Drop", A = H("8FC6E0"), B = H("5E9CC2"), Burst = 0, Rate = 4f, Loop = true, Speed0 = 0.05f, Speed1 = 0.1f, Gravity = 1f, Life0 = 0.3f, Life1 = 0.5f, Size0 = 0.03f, Size1 = 0.05f, Radius = 0.12f, Height = 0.5f },
                new VfxSpec { Name = "World_Argo_Pinned", Use = "Looping rope bits round a pinned foe's feet", Tex = "Px_Square", A = H("D9C08A"), B = H("A47A4E"), Burst = 0, Rate = 10f, Loop = true, Speed0 = 0.02f, Speed1 = 0.05f, Life0 = 0.4f, Life1 = 0.6f, Size0 = 0.03f, Size1 = 0.05f, Radius = 0.22f, Edge = true },
                new VfxSpec { Name = "World_Argo_Stun", Use = "Looping stars over a foe stunned beside a building (Anchor Line)", Tex = "Px_Spark", A = H("FFE07A"), B = H("FFF6C8"), Burst = 0, Rate = 5f, Loop = true, Speed0 = 0.02f, Speed1 = 0.05f, Life0 = 0.5f, Life1 = 0.7f, Size0 = 0.06f, Size1 = 0.08f, Radius = 0.15f, Edge = true },
                new VfxSpec { Name = "World_Argo_Whirl", Use = "Undertow (Harpoon): the reel ring round the target (scaled to its radius)", Tex = "Px_Square", A = H("7AE6EF"), B = H("3A9C9A"), Burst = 50, Speed0 = -0.4f, Speed1 = -0.2f, Life0 = 0.6f, Life1 = 0.7f, Size0 = 0.05f, Size1 = 0.08f, Radius = 0.02f, Ring = true, Spin = true },
                new VfxSpec { Name = "World_Argo_Kelp", Use = "Kelp Bed catches a foe", Tex = "Px_Square", A = H("5E8F4A"), B = H("3A8C50"), Burst = 12, Speed0 = 0.4f, Speed1 = 0.8f, Gravity = 0.5f, Life0 = 0.4f, Life1 = 0.6f, Size0 = 0.04f, Size1 = 0.07f, Radius = 0.12f, Hemisphere = true, Spin = true },
                new VfxSpec { Name = "World_Argo_Brine", Use = "Brine stings a foe (damage tick)", Tex = "Px_Spark", A = H("F4F6F8"), B = H("DFF5FA"), Burst = 5, Speed0 = 0.3f, Speed1 = 0.6f, Gravity = 0.3f, Life0 = 0.25f, Life1 = 0.4f, Size0 = 0.03f, Size1 = 0.05f, Radius = 0.1f, Height = 0.3f },
                new VfxSpec { Name = "World_Argo_Dragunder", Use = "Trench drags a foe under", Tex = "Px_Bubble", A = H("8FC6E0"), B = H("DFF5FA"), Burst = 20, Speed0 = 0.3f, Speed1 = 0.7f, Gravity = -0.4f, Life0 = 0.6f, Life1 = 1f, Size0 = 0.05f, Size1 = 0.09f, Radius = 0.2f },
                new VfxSpec { Name = "World_Argo_Bedrock", Use = "Bedrock grinds a pinned foe in a Tremor (damage tick)", Tex = "Px_Square", A = H("8A7A5A"), B = H("64797F"), Burst = 6, Speed0 = 0.4f, Speed1 = 0.8f, Gravity = 1f, Life0 = 0.3f, Life1 = 0.5f, Size0 = 0.03f, Size1 = 0.05f, Radius = 0.1f, Hemisphere = true },
                new VfxSpec { Name = "World_Argo_Faultburst", Use = "Fault Line: a foe reeled through a Tremor takes a burst", Tex = "Px_Square", A = H("D8C79A"), B = H("8A7A5A"), Burst = 26, Speed0 = 1.5f, Speed1 = 2.6f, Gravity = 1.5f, Life0 = 0.4f, Life1 = 0.7f, Size0 = 0.05f, Size1 = 0.09f, Radius = 0.1f, Hemisphere = true, Spin = true },
            };
            foreach (var s in specs)
            {
                var go = new GameObject(s.Name);
                var ps = go.AddComponent<ParticleSystem>();
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var main = ps.main;
                main.duration = s.Loop ? 2f : s.Duration;
                main.loop = s.Loop;
                main.playOnAwake = true;
                main.startLifetime = new ParticleSystem.MinMaxCurve(s.Life0, s.Life1);
                main.startSpeed = new ParticleSystem.MinMaxCurve(s.Speed0, s.Speed1);
                main.startSize = new ParticleSystem.MinMaxCurve(s.Size0, s.Size1);
                main.startColor = new ParticleSystem.MinMaxGradient(s.A, s.B);
                main.gravityModifier = s.Gravity;
                bool local = s.Ring || s.Edge || s.Name == "World_Argo_Ping" || s.Name == "World_Argo_Tuned" || s.Name == "World_Argo_Wet";
                main.simulationSpace = local ? ParticleSystemSimulationSpace.Local : ParticleSystemSimulationSpace.World;
                if (s.Ring || s.Edge) main.scalingMode = ParticleSystemScalingMode.Hierarchy;
                main.maxParticles = 128;
                main.stopAction = s.Loop ? ParticleSystemStopAction.None : ParticleSystemStopAction.Destroy;
                if (s.Spin) main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
                var em = ps.emission;
                em.rateOverTime = s.Rate;
                if (s.Burst > 0) em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)s.Burst) });
                var shape = ps.shape;
                if (s.Cone)
                {
                    shape.shapeType = ParticleSystemShapeType.Cone;
                    shape.angle = 18f;
                    shape.radius = s.Radius;
                    shape.rotation = Vector3.zero;   // +Z: the client turns the effect to face the shot
                }
                else
                {
                    shape.shapeType = s.Hemisphere ? ParticleSystemShapeType.Hemisphere : ParticleSystemShapeType.Circle;
                    shape.radius = s.Radius;
                    shape.rotation = s.Hemisphere ? up : new Vector3(-90f, 0f, 0f);
                    if (s.Edge) shape.radiusThickness = 0f;
                }
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
                    sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.7f, 1f, 1.5f));
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

        // A single prefab that references every asset through built-in components only (SpriteRenderer, nested prefabs,
        // AudioSource), so runtime mod code can find them by child name without ModTool APIs:
        //   Library/Sprites/<SheetName>  (sprite.texture = the whole sheet)
        //   Library/Models/<ModelName>, Library/VFX/<Name>, Library/Audio/<ClipName>
        static void BuildLibrary()
        {
            var root = new GameObject("ArgoAssets_Library");
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
            // (Sprigg v1.2.1: active SpriteRenderers drew the sheets at the world origin on the startup screen).
            spr.SetActive(false);
            models.SetActive(false);
            vfx.SetActive(false);
            audio.SetActive(false);
            string path = Root + "/Prefabs/ArgoAssets_Library.prefab";
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
            j.AppendLine("  \"mod\": \"Argo\", \"generatedBy\": \"Tools/Argo/Build All Assets (Assets/ArgoMod/Editor)\",");
            j.AppendLine("  \"conventions\": { \"spritePixelsPerUnit\": 20, \"spritePivot\": [0.5, 0], \"filter\": \"Point\", \"modelStyle\": \"low-poly, flat-shaded, palette UVs\", \"groundY\": -0.5, \"parts\": \"Turret (swung toward each shot) and Spinner (turns while drilling) under <Model>/Vis\" },");
            j.AppendLine("  \"sheets\": [");
            for (int i = 0; i < sheets.Count; i++)
                j.AppendLine("    { \"name\": " + Q(sheets[i].Name) + ", \"path\": " + Q(sheets[i].Path) + ", \"cell\": [" + sheets[i].CellW + ", " + sheets[i].CellH + "] }" + (i < sheets.Count - 1 ? "," : ""));
            j.AppendLine("  ],");
            j.AppendLine("  \"models\": [");
            var mk = modelPrefabs.Keys.ToList();
            for (int i = 0; i < mk.Count; i++) j.AppendLine("    { \"name\": " + Q(mk[i]) + ", \"prefab\": " + Q(modelPrefabs[mk[i]]) + " }" + (i < mk.Count - 1 ? "," : ""));
            j.AppendLine("  ],");
            j.AppendLine("  \"vfx\": [");
            var vk = vfxPrefabs.Keys.ToList();
            for (int i = 0; i < vk.Count; i++) j.AppendLine("    { \"id\": " + Q(vk[i]) + ", \"prefab\": " + Q(vfxPrefabs[vk[i]]) + ", \"use\": " + Q(vfxUse[vk[i]]) + " }" + (i < vk.Count - 1 ? "," : ""));
            j.AppendLine("  ],");
            j.AppendLine("  \"sounds\": [");
            var sounds = ArgoAudio.All();
            for (int i = 0; i < sounds.Count; i++) j.AppendLine("    { \"id\": " + Q(sounds[i].Name) + ", \"file\": " + Q(audioFiles[sounds[i].Name]) + ", \"use\": " + Q(sounds[i].Use) + " }" + (i < sounds.Count - 1 ? "," : ""));
            j.AppendLine("  ]");
            j.AppendLine("}");
            string manifest = Root + "/Data/Argo_Manifest.json";
            Directory.CreateDirectory(Path.GetDirectoryName(Abs(manifest)));
            File.WriteAllText(Abs(manifest), j.ToString());

            // Localization sheet for translators: the game never reads this file (docs/21 §3.1, pitfall #97). The
            // English text is the key and an unknown key shows unchanged, so the English names show in every language.
            var csv = new StringBuilder();
            csv.AppendLine("Note,Key,English");
            void Row(string note, string text) { csv.AppendLine(Csv(note) + "," + Csv(text) + "," + Csv(text)); }
            Row("\"Argo\" Vessel proper name. A bulky deep-sea diver in a brass helmet.", "Argo");
            Row("Vessel title", "Fathoms Below");
            Row("", "Fathoms Below ▪ Intermediate");
            Row("Vessel rule name", "Fathoms");
            Row("\"SONAR\" action button text", "SONAR");
            Row("\"Fathom\" depth a Deep Drill has sunk to", "Fathom");
            Row("Buildables", "Scatter Cannon"); Row("", "Harpoon Tower"); Row("", "Diving Bell"); Row("", "Deep Drill");
            Row("\"Naut\" Argo's crew; Port wears the red light, Starboard the green, Bosun the yellow", "Naut");
            Row("", "Port"); Row("", "Starboard"); Row("", "Bosun");
            string loc = Root + "/Data/Argo_Localization.csv";
            File.WriteAllText(Abs(loc), csv.ToString(), new UTF8Encoding(false));
            log.AppendLine("data: manifest + localization");
        }

        // ================================================================ previews (ModSource/Argo/Previews, not exported)

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
                ArgoPreview.Save(dir, "sprite_" + s.Name, px, s.CellW);
            }
            foreach (var g in new[] { "Scatter", "Harpoon", "Bell", "Drill", "Water", "Extras", "Projectiles" })
            {
                var go = new List<GameObject>();
                foreach (var d in ArgoModels.All().Where(d => d.Group == g))
                    go.Add((GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(modelPrefabs[d.Name])));
                Grid(dir, "models_" + g, go, g == "Projectiles" ? 0.3f : 0.62f, g == "Projectiles" ? 0f : -0.05f);
                foreach (var o in go) UnityEngine.Object.DestroyImmediate(o);
            }
            log.AppendLine("previews: " + dir);
        }

        // Renders each object at the game's camera angle into a 4-wide grid.
        static void Grid(string dir, string name, List<GameObject> objects, float ortho, float lookY)
        {
            int cell = 256, cols = Mathf.Min(4, objects.Count), rows = (objects.Count + cols - 1) / cols;
            var sheet = new Texture2D(cell * cols, cell * rows, TextureFormat.RGBA32, false);
            var clear = new Color[cell * cell];
            for (int i = 0; i < clear.Length; i++) clear[i] = new Color(0.86f, 0.84f, 0.80f, 1f);
            for (int r = 0; r < rows; r++) for (int c = 0; c < cols; c++) sheet.SetPixels(c * cell, r * cell, cell, cell, clear);
            for (int i = 0; i < objects.Count; i++)
            {
                var img = Render(objects[i], cell, ortho, lookY);
                sheet.SetPixels((i % cols) * cell, (rows - 1 - i / cols) * cell, cell, cell, img.GetPixels());
                UnityEngine.Object.DestroyImmediate(img);
            }
            sheet.Apply();
            File.WriteAllBytes(Path.Combine(dir, name + ".png"), sheet.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(sheet);
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
