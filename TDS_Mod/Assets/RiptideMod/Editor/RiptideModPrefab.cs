// Tools > Riptide > Build Mod Prefab: packs Riptide's compile-checked code (ModSource/Riptide/Build/RiptideMod_Code.cs,
// written by Build-RiptideMod.ps1) into Assets/RiptideMod/RiptideMod_Default.prefab. "Default" in the name makes the
// game load it as soon as the mod is enabled. Riptide has no art, so the prefab carries only the code.
//
// Exporting: ModTool exports every prefab and ScriptableObject in the project except under /Editor/, /ModTool/ and
// Packages, so only one mod may be "live" at a time. Park Other Mods moves every other Assets/*Mod folder into
// Assets/_Parked/Editor (the same place every vessel parks to), Export Mod then exports Riptide alone, and Restore
// Parked Mods puts them back.
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace RiptideBuild
{
    public static class RiptideModPrefab
    {
        const string PrefabPath = "Assets/RiptideMod/RiptideMod_Default.prefab";
        const string Parked = "Assets/_Parked/Editor";
        const string Own = "RiptideMod";

        [MenuItem("Tools/Riptide/Build Mod Prefab")]
        public static void Build()
        {
            string codePath = Path.GetFullPath(Path.Combine(Application.dataPath, "../ModSource/Riptide/Build/RiptideMod_Code.cs"));
            string log = Path.GetFullPath(Path.Combine(Application.dataPath, "../ModSource/Riptide/Build/prefab_log.txt"));
            try
            {
                if (!File.Exists(codePath))
                    throw new Exception("run Build-RiptideMod.ps1 first: " + codePath + " is missing");
                Type gamescript = Type.GetType("Mods_Gamescript, Exporteritems") ?? Type.GetType("Mods_Gamescript, Assembly-CSharp");
                if (gamescript == null)
                    throw new Exception("Mods_Gamescript type not found (is the TDS mod tools package imported?)");

                var root = new GameObject("RiptideMod_Default");
                var script = (MonoBehaviour)root.AddComponent(gamescript);
                var so = new SerializedObject(script);
                so.FindProperty("Code").stringValue = File.ReadAllText(codePath);
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                UnityEngine.Object.DestroyImmediate(root);
                AssetDatabase.SaveAssets();
                File.WriteAllText(log, "OK " + DateTime.Now.ToString("s") + " code " + new FileInfo(codePath).Length + " bytes");
                Debug.Log("[Riptide] built " + PrefabPath);
            }
            catch (Exception e)
            {
                File.WriteAllText(log, "FAILED " + e);
                Debug.LogError("[Riptide] " + e);
            }
        }

        [MenuItem("Tools/Riptide/Park Other Mods")]
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
                Debug.Log("[Riptide] parked " + name + (string.IsNullOrEmpty(err) ? "" : ": " + err));
            }
            AssetDatabase.Refresh();
        }

        [MenuItem("Tools/Riptide/Restore Parked Mods")]
        public static void RestoreParked()
        {
            if (!AssetDatabase.IsValidFolder(Parked))
                return;
            foreach (string dir in AssetDatabase.GetSubFolders(Parked))
            {
                string name = Path.GetFileName(dir);
                string err = AssetDatabase.MoveAsset(dir, "Assets/" + name);
                Debug.Log("[Riptide] restored " + name + (string.IsNullOrEmpty(err) ? "" : ": " + err));
            }
            if (AssetDatabase.GetSubFolders(Parked).Length == 0)
                AssetDatabase.DeleteAsset("Assets/_Parked");
            AssetDatabase.Refresh();
        }

        // Tools > Riptide > Export Mod: ModTool export with Riptide's settings (the code rides in the prefab).
        // Output goes straight to the game's Mods folder (ExportSettings.outputDirectory).
        [MenuItem("Tools/Riptide/Export Mod")]
        public static void Export()
        {
            foreach (string dir in AssetDatabase.GetSubFolders("Assets"))
            {
                string name = Path.GetFileName(dir);
                if (name.EndsWith("Mod") && name != Own)
                {
                    Debug.LogError("[Riptide] " + name + " is still in Assets: run Tools > Riptide > Park Other Mods first, or it is exported inside Riptide");
                    return;
                }
            }
            ModTool.Editor.Exporting.ExportSettings.name = "RiptideMod";
            ModTool.Editor.Exporting.ExportSettings.author = "Dimitri";
            ModTool.Editor.Exporting.ExportSettings.description = "Adds Riptide, a roguelike mode: thirty random Tides on one map and a Salvage of artifacts between them. Every player in a lobby needs this mod.";
            ModTool.Editor.Exporting.ExportSettings.version = "0.2.3";
            ModTool.Editor.Exporting.ExportSettings.content = ModTool.Shared.ModContent.Assets;
            AssetDatabase.SaveAssets();
            ModTool.Editor.Exporting.ModExporter.ExportMod();
        }
    }
}
