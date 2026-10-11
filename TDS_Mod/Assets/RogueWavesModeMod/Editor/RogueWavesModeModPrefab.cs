// Tools > RogueWavesMode: the mode part's build steps, named as Tools > ModKit > Build Queue expects. The part has no
// art assets (the chart is built in code at run time), so Build All Assets only records that there is nothing to build;
// Build Mod Prefab packs the compile-checked code (ModSource/RogueWavesMode/Build/RogueWavesModeMod_Code.cs, from
// Build-RogueWavesModeMod.ps1) into Assets/RogueWavesModeMod/RogueWaves_Mode_Default.prefab (docs/PACK.md: the name has
// "Default", so the game loads it at startup, and none of the game's other keywords); Export Mod parks every other
// Assets/*Mod folder, then exports RogueWavesModeMod alone. Restore Parked Mods puts the others back.
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace RogueWavesModeBuild
{
    public static class RogueWavesModeModPrefab
    {
        const string PrefabPath = "Assets/RogueWavesModeMod/RogueWaves_Mode_Default.prefab";
        const string Parked = "Assets/_Parked/Editor";
        const string Own = "RogueWavesModeMod";
        const string Version = "0.1.1";

        static string Source { get { return Path.GetFullPath(Path.Combine(Application.dataPath, "../ModSource/RogueWavesMode")); } }

        [MenuItem("Tools/RogueWavesMode/Build All Assets")]
        public static void BuildAll()
        {
            Directory.CreateDirectory(Source);
            File.WriteAllText(Path.Combine(Source, "build_log.txt"), "OK " + DateTime.Now.ToString("s") + " (the mode part has no assets)");
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
            ModTool.Editor.Exporting.ExportSettings.description = "Rogue Waves, the mode part (dev build): the campaign's sea chart and sea weather.";
            ModTool.Editor.Exporting.ExportSettings.version = Version;
            ModTool.Editor.Exporting.ExportSettings.content = ModTool.Shared.ModContent.Assets;
            AssetDatabase.SaveAssets();
            ModTool.Editor.Exporting.ModExporter.ExportMod();
        }
    }
}
