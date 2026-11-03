// Tools > RogueWavesFoes: the foes part's build steps, named as Tools > ModKit > Build Queue expects. No assets: Build Mod Prefab
// packs the compile-checked code into Assets/RogueWavesFoesMod/RogueWaves_Foes_Default.prefab; Export Mod parks every other Assets/*Mod
// folder, then exports RogueWavesFoesMod alone; Restore Parked Mods puts the others back.
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace RogueWavesFoesBuild
{
    public static class RogueWavesFoesModPrefab
    {
        const string PrefabPath = "Assets/RogueWavesFoesMod/RogueWaves_Foes_Default.prefab";
        const string Parked = "Assets/_Parked/Editor";
        const string Own = "RogueWavesFoesMod";
        const string Version = "0.1.0-dev";  // = RWFData.Version (docs/PACK.md §4): the pack version plus -dev

        static string Source { get { return Path.GetFullPath(Path.Combine(Application.dataPath, "../ModSource/RogueWavesFoes")); } }

        [MenuItem("Tools/RogueWavesFoes/Build All Assets")]
        public static void BuildAll()
        {
            Directory.CreateDirectory(Source);
            File.WriteAllText(Path.Combine(Source, "build_log.txt"), "OK " + DateTime.Now.ToString("s") + " (the foes part has no assets)");
        }

        [MenuItem("Tools/RogueWavesFoes/Build Mod Prefab")]
        public static void Build()
        {
            string codePath = Path.Combine(Source, "Build/RogueWavesFoesMod_Code.cs");
            string log = Path.Combine(Source, "Build/prefab_log.txt");
            try
            {
                if (!File.Exists(codePath))
                    throw new Exception("run Build-RogueWavesFoesMod.ps1 first: " + codePath + " is missing");
                Type gamescript = Type.GetType("Mods_Gamescript, Exporteritems") ?? Type.GetType("Mods_Gamescript, Assembly-CSharp");
                if (gamescript == null)
                    throw new Exception("Mods_Gamescript type not found (is the TDS mod tools package imported?)");
                if (!AssetDatabase.IsValidFolder("Assets/RogueWavesFoesMod"))
                    AssetDatabase.CreateFolder("Assets", "RogueWavesFoesMod");
                var root = new GameObject("RogueWaves_Foes_Default");
                var script = (MonoBehaviour)root.AddComponent(gamescript);
                var so = new SerializedObject(script);
                so.FindProperty("Code").stringValue = File.ReadAllText(codePath);
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                UnityEngine.Object.DestroyImmediate(root);
                AssetDatabase.SaveAssets();
                File.WriteAllText(log, "OK " + DateTime.Now.ToString("s") + " code " + new FileInfo(codePath).Length + " bytes");
                Debug.Log("[RogueWavesFoes] built " + PrefabPath);
            }
            catch (Exception e)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(log));
                File.WriteAllText(log, "FAILED " + e);
                Debug.LogError("[RogueWavesFoes] " + e);
            }
        }

        [MenuItem("Tools/RogueWavesFoes/Park Other Mods")]
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
                Debug.Log("[RogueWavesFoes] parked " + name + (string.IsNullOrEmpty(err) ? "" : ": " + err));
            }
            AssetDatabase.Refresh();
        }

        [MenuItem("Tools/RogueWavesFoes/Restore Parked Mods")]
        public static void RestoreParked()
        {
            if (!AssetDatabase.IsValidFolder(Parked))
                return;
            foreach (string dir in AssetDatabase.GetSubFolders(Parked))
            {
                string name = Path.GetFileName(dir);
                string err = AssetDatabase.MoveAsset(dir, "Assets/" + name);
                Debug.Log("[RogueWavesFoes] restored " + name + (string.IsNullOrEmpty(err) ? "" : ": " + err));
            }
            if (AssetDatabase.GetSubFolders(Parked).Length == 0)
                AssetDatabase.DeleteAsset("Assets/_Parked");
            AssetDatabase.Refresh();
        }

        // Output goes straight to the game's Mods folder (ExportSettings.outputDirectory), left disabled by the exporter.
        [MenuItem("Tools/RogueWavesFoes/Export Mod")]
        public static void Export()
        {
            ParkOthers();
            foreach (string dir in AssetDatabase.GetSubFolders("Assets"))
            {
                string name = Path.GetFileName(dir);
                if (name.EndsWith("Mod") && name != Own)
                {
                    Debug.LogError("[RogueWavesFoes] " + name + " could not be parked: not exporting, or it would ship inside RogueWavesFoesMod");
                    return;
                }
            }
            ModTool.Editor.Exporting.ExportSettings.name = Own;
            ModTool.Editor.Exporting.ExportSettings.author = "Dimitri";
            ModTool.Editor.Exporting.ExportSettings.description = "Rogue Waves, the foes part (dev build): the enemy modifiers Cursed, Berserk, Aging and Dazzling.";
            ModTool.Editor.Exporting.ExportSettings.version = Version;
            ModTool.Editor.Exporting.ExportSettings.content = ModTool.Shared.ModContent.Assets;
            AssetDatabase.SaveAssets();
            ModTool.Editor.Exporting.ModExporter.ExportMod();
        }
    }
}
