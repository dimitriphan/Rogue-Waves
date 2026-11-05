// Tools > Argo > Build Mod Prefab: packs Argo's compile-checked code (ModSource/Argo/Build/ArgoMod_Code.cs, written by
// Build-ArgoMod.ps1) and the asset library into Assets/ArgoMod/ArgoMod_Default.prefab.
// "Default" in the name makes the game load it as soon as the mod is enabled.
//
// Tools > Argo > Export Mod: ModTool exports every prefab and ScriptableObject in the project except paths containing
// "/Editor/" or "/ModTool/". Other mods kept in this project (Assets/<Name>Mod...) would ride along in Argo's
// bundle, so the export first parks them under Assets/_Parked/Editor/ (AssetDatabase.MoveAsset keeps their GUIDs).
// Run Tools > Argo > Restore Parked Mods once the export has finished.
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ArgoBuild
{
    public static class ArgoModPrefab
    {
        const string PrefabPath = "Assets/ArgoMod/ArgoMod_Default.prefab";
        const string LibraryPath = "Assets/ArgoMod/Prefabs/ArgoAssets_Library.prefab";
        const string Parked = "Assets/_Parked/Editor";

        [MenuItem("Tools/Argo/Build Mod Prefab")]
        public static void Build()
        {
            string codePath = Path.GetFullPath(Path.Combine(Application.dataPath, "../ModSource/Argo/Build/ArgoMod_Code.cs"));
            string log = Path.GetFullPath(Path.Combine(Application.dataPath, "../ModSource/Argo/Build/prefab_log.txt"));
            try
            {
                if (!File.Exists(codePath))
                    throw new Exception("run Build-ArgoMod.ps1 first: " + codePath + " is missing");
                Type gamescript = Type.GetType("Mods_Gamescript, Exporteritems") ?? Type.GetType("Mods_Gamescript, Assembly-CSharp");
                if (gamescript == null)
                    throw new Exception("Mods_Gamescript type not found (is the TDS mod tools package imported?)");
                GameObject library = AssetDatabase.LoadAssetAtPath<GameObject>(LibraryPath);
                if (library == null)
                    throw new Exception("asset library missing: run Tools > Argo > Build All Assets");

                var root = new GameObject("ArgoMod_Default");
                var script = (MonoBehaviour)root.AddComponent(gamescript);
                var so = new SerializedObject(script);
                so.FindProperty("Code").stringValue = File.ReadAllText(codePath);
                so.ApplyModifiedPropertiesWithoutUndo();
                var lib = (GameObject)PrefabUtility.InstantiatePrefab(library);
                lib.name = "ArgoAssets_Library";
                lib.transform.SetParent(root.transform, false);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                UnityEngine.Object.DestroyImmediate(root);
                AssetDatabase.SaveAssets();
                File.WriteAllText(log, "OK " + DateTime.Now.ToString("s") + " code " + new FileInfo(codePath).Length + " bytes");
                Debug.Log("[Argo] built " + PrefabPath);
            }
            catch (Exception e)
            {
                File.WriteAllText(log, "FAILED " + e);
                Debug.LogError("[Argo] " + e);
            }
        }

        [MenuItem("Tools/Argo/Export Mod")]
        public static void Export()
        {
            Park();
            ModTool.Editor.Exporting.ExportSettings.name = "ArgoMod";
            ModTool.Editor.Exporting.ExportSettings.author = "Dimitri";
            ModTool.Editor.Exporting.ExportSettings.description = "Rogue Waves, Argo (dev build): Deep Sea Diver, a bulky diver with a crew of Nauts, a Scatter Cannon, a Harpoon Gun and a Drill Rig that sinks deeper every Tide. Every player in a lobby needs this mod.";
            ModTool.Editor.Exporting.ExportSettings.version = "0.1.0-dev";      // ArgoData.Version (docs/PACK.md §4); the one pack version replaces it at the first pack build
            ModTool.Editor.Exporting.ExportSettings.content = ModTool.Shared.ModContent.Assets;
            AssetDatabase.SaveAssets();
            ModTool.Editor.Exporting.ModExporter.ExportMod();
        }

        // Moves every other "<Name>Mod" asset folder out of the export's sight.
        static void Park()
        {
            foreach (string dir in AssetDatabase.GetSubFolders("Assets"))
            {
                string name = Path.GetFileName(dir);
                if (!name.EndsWith("Mod") || name == "ArgoMod")
                    continue;
                EnsureFolder(Parked);
                string error = AssetDatabase.MoveAsset(dir, Parked + "/" + name);
                Debug.Log("[Argo] parked " + dir + (string.IsNullOrEmpty(error) ? "" : ": " + error));
            }
            AssetDatabase.Refresh();
        }

        [MenuItem("Tools/Argo/Restore Parked Mods")]
        public static void Restore()
        {
            if (!AssetDatabase.IsValidFolder(Parked))
                return;
            foreach (string dir in AssetDatabase.GetSubFolders(Parked))
            {
                string error = AssetDatabase.MoveAsset(dir, "Assets/" + Path.GetFileName(dir));
                Debug.Log("[Argo] restored " + dir + (string.IsNullOrEmpty(error) ? "" : ": " + error));
            }
            if (AssetDatabase.GetSubFolders(Parked).Length == 0)
                AssetDatabase.DeleteAsset("Assets/_Parked");
            AssetDatabase.Refresh();
        }

        static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string cur = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = cur + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(cur, parts[i]);
                cur = next;
            }
        }
    }
}
