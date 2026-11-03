// Tools > Uloe > Build Mod Prefab: packs Uloe's compile-checked code (ModSource/Uloe/Build/UloeMod_Code.cs,
// written by Build-UloeMod.ps1) and the asset library into Assets/UloeMod/UloeMod_Default.prefab.
// "Default" in the name makes the game load it as soon as the mod is enabled.
//
// Exporting: ModTool exports every prefab and ScriptableObject in the project except under /Editor/, /ModTool/ and
// Packages, so only one mod may be "live" at a time. Tools > Uloe > Park Other Mods moves every other Assets/*Mod
// folder into Assets/_Parked/Editor (AssetDatabase moves keep every GUID and reference), Export Mod then exports
// Uloe alone, and Restore Parked Mods puts them back.
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace UloeBuild
{
    public static class UloeModPrefab
    {
        const string PrefabPath = "Assets/UloeMod/UloeMod_Default.prefab";
        const string LibraryPath = "Assets/UloeMod/Prefabs/UloeAssets_Library.prefab";
        const string Parked = "Assets/_Parked/Editor";   // the same place Argo's export parks mods
        const string Own = "UloeMod";

        [MenuItem("Tools/Uloe/Build Mod Prefab")]
        public static void Build()
        {
            string codePath = Path.GetFullPath(Path.Combine(Application.dataPath, "../ModSource/Uloe/Build/UloeMod_Code.cs"));
            string log = Path.GetFullPath(Path.Combine(Application.dataPath, "../ModSource/Uloe/Build/prefab_log.txt"));
            try
            {
                if (!File.Exists(codePath))
                    throw new Exception("run Build-UloeMod.ps1 first: " + codePath + " is missing");
                Type gamescript = Type.GetType("Mods_Gamescript, Exporteritems") ?? Type.GetType("Mods_Gamescript, Assembly-CSharp");
                if (gamescript == null)
                    throw new Exception("Mods_Gamescript type not found (is the TDS mod tools package imported?)");
                GameObject library = AssetDatabase.LoadAssetAtPath<GameObject>(LibraryPath);
                if (library == null)
                    throw new Exception("asset library missing: run Tools > Uloe > Build All Assets");

                var root = new GameObject("UloeMod_Default");
                var script = (MonoBehaviour)root.AddComponent(gamescript);
                var so = new SerializedObject(script);
                so.FindProperty("Code").stringValue = File.ReadAllText(codePath);
                so.ApplyModifiedPropertiesWithoutUndo();
                var lib = (GameObject)PrefabUtility.InstantiatePrefab(library);
                lib.name = "UloeAssets_Library";
                lib.transform.SetParent(root.transform, false);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                UnityEngine.Object.DestroyImmediate(root);
                AssetDatabase.SaveAssets();
                File.WriteAllText(log, "OK " + DateTime.Now.ToString("s") + " code " + new FileInfo(codePath).Length + " bytes");
                Debug.Log("[Uloe] built " + PrefabPath);
            }
            catch (Exception e)
            {
                File.WriteAllText(log, "FAILED " + e);
                Debug.LogError("[Uloe] " + e);
            }
        }

        [MenuItem("Tools/Uloe/Park Other Mods")]
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
                Debug.Log("[Uloe] parked " + name + (string.IsNullOrEmpty(err) ? "" : ": " + err));
            }
            AssetDatabase.Refresh();
        }

        [MenuItem("Tools/Uloe/Restore Parked Mods")]
        public static void RestoreParked()
        {
            if (!AssetDatabase.IsValidFolder(Parked))
                return;
            foreach (string dir in AssetDatabase.GetSubFolders(Parked))
            {
                string name = Path.GetFileName(dir);
                string err = AssetDatabase.MoveAsset(dir, "Assets/" + name);
                Debug.Log("[Uloe] restored " + name + (string.IsNullOrEmpty(err) ? "" : ": " + err));
            }
            if (AssetDatabase.GetSubFolders(Parked).Length == 0)
                AssetDatabase.DeleteAsset("Assets/_Parked");
            AssetDatabase.Refresh();
        }

        // Tools > Uloe > Export Mod: ModTool export with Uloe's settings (assets only; the code rides in the prefab).
        // Output goes straight to the game's Mods folder (ExportSettings.outputDirectory).
        [MenuItem("Tools/Uloe/Export Mod")]
        public static void Export()
        {
            ParkOthers();   // the Build Queue doesn't park the other mods (pitfall #103)
            foreach (string dir in AssetDatabase.GetSubFolders("Assets"))
            {
                string name = Path.GetFileName(dir);
                if (name.EndsWith("Mod") && name != Own)
                {
                    Debug.LogError("[Uloe] " + name + " is still in Assets: run Tools > Uloe > Park Other Mods first, or it is exported inside Uloe");
                    return;
                }
            }
            ModTool.Editor.Exporting.ExportSettings.name = "UloeMod";
            ModTool.Editor.Exporting.ExportSettings.author = "Dimitri";
            ModTool.Editor.Exporting.ExportSettings.description = "Adds Uloe, Light Personified: a light bulb on a lamp post whose towers grow stronger with the Light they receive. Every player in a lobby needs this mod.";
            ModTool.Editor.Exporting.ExportSettings.version = "1.0";
            ModTool.Editor.Exporting.ExportSettings.content = ModTool.Shared.ModContent.Assets;
            AssetDatabase.SaveAssets();
            ModTool.Editor.Exporting.ModExporter.ExportMod();
        }
    }
}
