// Tools > RogueWavesMode: the mode part's build steps, named as Tools > ModKit > Build Queue expects. The chart is built in
// code at run time; Build All Assets gathers the part's sprites (Art/Temporary/game_size, with final art of the same name
// one folder up winning, docs/TEMP_ART.md) and its low-poly models (RogueWavesModeModels, since 2026-11-05) into a
// library prefab; Build Mod Prefab packs the library and the compile-checked
// code (ModSource/RogueWavesMode/Build/RogueWavesModeMod_Code.cs, from
// Build-RogueWavesModeMod.ps1) into Assets/RogueWavesModeMod/RogueWaves_Mode_Default.prefab (docs/PACK.md: the name has
// "Default", so the game loads it at startup, and none of the game's other keywords); Export Mod parks every other
// Assets/*Mod folder, then exports RogueWavesModeMod alone. Restore Parked Mods puts the others back.
using System;
using System.IO;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine;

namespace RogueWavesModeBuild
{
    public static class RogueWavesModeModPrefab
    {
        const string PrefabPath = "Assets/RogueWavesModeMod/RogueWaves_Mode_Default.prefab";
        const string ArtPath = "Assets/RogueWavesModeMod/Art";
        const string LibraryPath = "Assets/RogueWavesModeMod/Prefabs/RWAssets_Library.prefab";
        const string Parked = "Assets/_Parked/Editor";
        const string Own = "RogueWavesModeMod";
        const string Version = "0.1.0-dev";  // = RWData.Version (docs/PACK.md §4): the pack version plus -dev

        static string Source { get { return Path.GetFullPath(Path.Combine(Application.dataPath, "../ModSource/RogueWavesMode")); } }

        [MenuItem("Tools/RogueWavesMode/Build All Assets")]
        public static void BuildAll()
        {
            Directory.CreateDirectory(Source);
            string log = Path.Combine(Source, "build_log.txt");
            try
            {
                int models;
                int n = BuildLibrary(out models);
                File.WriteAllText(log, "OK " + DateTime.Now.ToString("s") + " " + n + " sprites, " + models + " models");
            }
            catch (Exception e)
            {
                File.WriteAllText(log, "FAILED " + e);
                Debug.LogError("[RogueWavesMode] " + e);
            }
        }

        // Library/Sprites/<name>: a SpriteRenderer whose sprite is the whole file. The code cuts what it needs at run time.
        // Library/Models/<name>: each model of RogueWavesModeModels (RWModels in the code instantiates and lights it).
        static int BuildLibrary(out int models)
        {
            string temporary = Path.Combine(Source, "Art/Temporary/game_size");
            RogueWavesModeArt.WriteMissing(temporary);
            if (!AssetDatabase.IsValidFolder("Assets/RogueWavesModeMod")) AssetDatabase.CreateFolder("Assets", "RogueWavesModeMod");
            if (!AssetDatabase.IsValidFolder(ArtPath)) AssetDatabase.CreateFolder("Assets/RogueWavesModeMod", "Art");
            if (!AssetDatabase.IsValidFolder("Assets/RogueWavesModeMod/Prefabs")) AssetDatabase.CreateFolder("Assets/RogueWavesModeMod", "Prefabs");
            var names = new System.Collections.Generic.List<string>();
            foreach (string file in Directory.GetFiles(temporary, "*.png"))
            {
                string name = Path.GetFileNameWithoutExtension(file);
                string final = Path.Combine(Source, "Art/" + name + ".png");
                File.Copy(File.Exists(final) ? final : file, Path.Combine(Application.dataPath, "RogueWavesModeMod/Art/" + name + ".png"), true);
                names.Add(name);
            }
            AssetDatabase.Refresh();
            var root = new GameObject("RWAssets_Library");
            var holder = new GameObject("Sprites");
            holder.transform.SetParent(root.transform, false);
            foreach (string name in names)
            {
                string path = ArtPath + "/" + name + ".png";
                var ti = (TextureImporter)AssetImporter.GetAtPath(path);
                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.filterMode = FilterMode.Point;
                ti.textureCompression = TextureImporterCompression.Uncompressed;
                ti.mipmapEnabled = false;
                ti.isReadable = true;
                ti.alphaIsTransparency = true;
                ti.npotScale = TextureImporterNPOTScale.None;
                ti.spritePixelsPerUnit = 20f;
                ti.SaveAndReimport();
                var child = new GameObject(name);
                child.transform.SetParent(holder.transform, false);
                child.AddComponent<SpriteRenderer>().sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }
            // A failed model never costs the sprites: the library is saved without it, the log says why, and the meteor
            // keeps the ore's look (RWObjectives.MakeMeteor).
            try { models = BuildModels(root); }
            catch (Exception e)
            {
                models = 0;
                Debug.LogError("[RogueWavesMode] models skipped: " + e);
                Transform half = root.transform.Find("Models");
                if (half != null) UnityEngine.Object.DestroyImmediate(half.gameObject);
            }
            root.SetActive(false);
            PrefabUtility.SaveAsPrefabAsset(root, LibraryPath);
            UnityEngine.Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            return names.Count;
        }

        // Each model as <Name> / Vis / <Name>_Export, its mesh saved under Art/Models and coloured from the mode's 8x8 palette
        // (RogueWavesModeLowpoly.Pal) on a URP Lit material. At run time RWModels puts the same palette on a copy of the
        // game's own tower material, so the model is lit like the stock ore it replaces and flashes when hit
        // (Entity_Vis_Resource looks for the "General Tower" shader). The holder stays inactive: the game instantiates
        // the mod prefab at startup, and an active model would draw on the startup screen.
        static int BuildModels(GameObject root)
        {
            const string dir = "Assets/RogueWavesModeMod/Art/Models";
            if (!AssetDatabase.IsValidFolder(dir)) AssetDatabase.CreateFolder(ArtPath, "Models");
            string palPath = dir + "/RW_Palette.png";
            var palTex = Pal.Texture();
            File.WriteAllBytes(Path.Combine(Path.GetDirectoryName(Application.dataPath), palPath), palTex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(palTex);
            AssetDatabase.ImportAsset(palPath, ImportAssetOptions.ForceSynchronousImport);
            var ti = (TextureImporter)AssetImporter.GetAtPath(palPath);
            ti.textureType = TextureImporterType.Default;
            ti.filterMode = FilterMode.Point;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.mipmapEnabled = false;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.npotScale = TextureImporterNPOTScale.None;
            ti.SaveAndReimport();
            var palette = AssetDatabase.LoadAssetAtPath<Texture2D>(palPath);

            string matPath = dir + "/RW_Palette.mat";
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new Exception("shader Universal Render Pipeline/Lit not found");
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null) { mat = new Material(shader); AssetDatabase.CreateAsset(mat, matPath); }
            mat.shader = shader;
            mat.SetTexture("_BaseMap", palette);
            mat.SetTexture("_MainTex", palette);
            mat.SetFloat("_Smoothness", 0.05f);
            EditorUtility.SetDirty(mat);

            var holder = new GameObject("Models");
            holder.transform.SetParent(root.transform, false);
            int n = 0;
            foreach (ModelDef def in RogueWavesModeModels.All())
            {
                string meshPath = dir + "/" + def.Name + ".asset";
                if (AssetDatabase.LoadAssetAtPath<Mesh>(meshPath) != null) AssetDatabase.DeleteAsset(meshPath);
                Mesh mesh = def.Build().ToMesh(def.Name);
                AssetDatabase.CreateAsset(mesh, meshPath);
                var model = new GameObject(def.Name);
                model.transform.SetParent(holder.transform, false);
                var vis = new GameObject("Vis");
                vis.transform.SetParent(model.transform, false);
                var exp = new GameObject(def.Name + "_Export");
                exp.transform.SetParent(vis.transform, false);
                exp.AddComponent<MeshFilter>().sharedMesh = mesh;
                var r = exp.AddComponent<MeshRenderer>();
                r.sharedMaterial = mat;
                r.shadowCastingMode = ShadowCastingMode.On;
                n++;
            }
            holder.SetActive(false);
            AssetDatabase.SaveAssets();
            return n;
        }

        [MenuItem("Tools/RogueWavesMode/Build Mod Prefab")]
        public static void Build()
        {
            string codePath = Path.Combine(Source, "Build/RogueWavesModeMod_Code.cs");
            string log = Path.Combine(Source, "Build/prefab_log.txt");
            try
            {
                if (!File.Exists(codePath))
                    throw new Exception("run Build-RogueWavesModeMod.ps1 first: " + codePath + " is missing");
                Type gamescript = Type.GetType("Mods_Gamescript, Exporteritems") ?? Type.GetType("Mods_Gamescript, Assembly-CSharp");
                if (gamescript == null)
                    throw new Exception("Mods_Gamescript type not found (is the TDS mod tools package imported?)");
                if (!AssetDatabase.IsValidFolder("Assets/RogueWavesModeMod"))
                    AssetDatabase.CreateFolder("Assets", "RogueWavesModeMod");
                var root = new GameObject("RogueWaves_Mode_Default");
                var script = (MonoBehaviour)root.AddComponent(gamescript);
                var so = new SerializedObject(script);
                so.FindProperty("Code").stringValue = File.ReadAllText(codePath);
                so.ApplyModifiedPropertiesWithoutUndo();
                GameObject library = AssetDatabase.LoadAssetAtPath<GameObject>(LibraryPath);
                if (library == null)
                    throw new Exception("asset library missing: run Tools > RogueWavesMode > Build All Assets");
                var lib = (GameObject)PrefabUtility.InstantiatePrefab(library);
                lib.name = "RWAssets_Library";
                lib.transform.SetParent(root.transform, false);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                UnityEngine.Object.DestroyImmediate(root);
                AssetDatabase.SaveAssets();
                File.WriteAllText(log, "OK " + DateTime.Now.ToString("s") + " code " + new FileInfo(codePath).Length + " bytes");
                Debug.Log("[RogueWavesMode] built " + PrefabPath);
            }
            catch (Exception e)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(log));
                File.WriteAllText(log, "FAILED " + e);
                Debug.LogError("[RogueWavesMode] " + e);
            }
        }

        [MenuItem("Tools/RogueWavesMode/Park Other Mods")]
        public static void ParkOthers()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Parked")) AssetDatabase.CreateFolder("Assets", "_Parked");
            if (!AssetDatabase.IsValidFolder(Parked)) AssetDatabase.CreateFolder("Assets/_Parked", "Editor");
            foreach (string dir in AssetDatabase.GetSubFolders("Assets"))
            {
                string name = Path.GetFileName(dir);
                if (!name.EndsWith("Mod") || name == Own)
                    continue;
                string err = AssetDatabase.MoveAsset(dir, Parked + "/" + name);
                Debug.Log("[RogueWavesMode] parked " + name + (string.IsNullOrEmpty(err) ? "" : ": " + err));
            }
            AssetDatabase.Refresh();
        }

        [MenuItem("Tools/RogueWavesMode/Restore Parked Mods")]
        public static void RestoreParked()
        {
            if (!AssetDatabase.IsValidFolder(Parked))
                return;
            foreach (string dir in AssetDatabase.GetSubFolders(Parked))
            {
                string name = Path.GetFileName(dir);
                string err = AssetDatabase.MoveAsset(dir, "Assets/" + name);
                Debug.Log("[RogueWavesMode] restored " + name + (string.IsNullOrEmpty(err) ? "" : ": " + err));
            }
            if (AssetDatabase.GetSubFolders(Parked).Length == 0)
                AssetDatabase.DeleteAsset("Assets/_Parked");
            AssetDatabase.Refresh();
        }

        // Output goes straight to the game's Mods folder (ExportSettings.outputDirectory), left disabled by the exporter.
        [MenuItem("Tools/RogueWavesMode/Export Mod")]
        public static void Export()
        {
            ParkOthers();
            foreach (string dir in AssetDatabase.GetSubFolders("Assets"))
            {
                string name = Path.GetFileName(dir);
                if (name.EndsWith("Mod") && name != Own)
                {
                    Debug.LogError("[RogueWavesMode] " + name + " could not be parked: not exporting, or it would ship inside RogueWavesModeMod");
                    return;
                }
            }
            ModTool.Editor.Exporting.ExportSettings.name = Own;
            ModTool.Editor.Exporting.ExportSettings.author = "Dimitri";
            ModTool.Editor.Exporting.ExportSettings.description = "Rogue Waves, the mode part (dev build): the campaign's sea chart and sea weather, retries, Pure Aura, VENDCINT, Directions, Cardinals, Trinkets, objectives and votes, and rejoining a running map.";
            ModTool.Editor.Exporting.ExportSettings.version = Version;
            ModTool.Editor.Exporting.ExportSettings.content = ModTool.Shared.ModContent.Assets;
            AssetDatabase.SaveAssets();
            ModTool.Editor.Exporting.ModExporter.ExportMod();
        }
    }
}
