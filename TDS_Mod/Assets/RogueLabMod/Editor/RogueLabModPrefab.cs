// Tools > RogueLab: the research mod's build steps, named as Tools > ModKit > Build Queue expects. RogueLab has no art,
// so Build All Assets only records that there is nothing to build; Build Mod Prefab packs the compile-checked code
// (ModSource/RogueLab/Build/RogueLabMod_Code.cs, from Build-RogueLabMod.ps1 -Define ROGUELAB_SELFTEST) into
// Assets/RogueLabMod/RogueLabMod_Default.prefab, and (round 2) ModSource/RogueLab/Second/RogueLabSecond.cs, as it is,
// into a second prefab in the same mod, RogueLabMod_Second_Default; Export Mod parks every other Assets/*Mod folder,
// then exports RogueLab alone ("Default" in the prefab names makes the game load them at startup). Restore Parked Mods
// puts the others back.
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace RogueLabBuild
{
    public static class RogueLabModPrefab
    {
        const string PrefabPath = "Assets/RogueLabMod/RogueLabMod_Default.prefab";
        const string SecondPath = "Assets/RogueLabMod/RogueLabMod_Second_Default.prefab";
        const string Parked = "Assets/_Parked/Editor";
        const string Own = "RogueLabMod";

        static string Source { get { return Path.GetFullPath(Path.Combine(Application.dataPath, "../ModSource/RogueLab")); } }

        [MenuItem("Tools/RogueLab/Build All Assets")]
        public static void BuildAll()
        {
            Directory.CreateDirectory(Source);
            File.WriteAllText(Path.Combine(Source, "build_log.txt"), "OK " + DateTime.Now.ToString("s") + " (RogueLab has no assets)");
        }

        [MenuItem("Tools/RogueLab/Build Mod Prefab")]
        public static void Build()
        {
            string codePath = Path.Combine(Source, "Build/RogueLabMod_Code.cs");
            string log = Path.Combine(Source, "Build/prefab_log.txt");
            try
            {
                if (!File.Exists(codePath))
                    throw new Exception("run Build-RogueLabMod.ps1 -Define ROGUELAB_SELFTEST first: " + codePath + " is missing");
                Type gamescript = Type.GetType("Mods_Gamescript, Exporteritems") ?? Type.GetType("Mods_Gamescript, Assembly-CSharp");
                if (gamescript == null)
                    throw new Exception("Mods_Gamescript type not found (is the TDS mod tools package imported?)");
                if (!AssetDatabase.IsValidFolder("Assets/RogueLabMod"))
                    AssetDatabase.CreateFolder("Assets", "RogueLabMod");
                Save(gamescript, "RogueLabMod_Default", File.ReadAllText(codePath), PrefabPath);
                string secondPath = Path.Combine(Source, "Second/RogueLabSecond.cs");
                if (!File.Exists(secondPath))
                    throw new Exception("missing " + secondPath);
                Save(gamescript, "RogueLabMod_Second_Default", File.ReadAllText(secondPath).Replace("\r\n", "\n"), SecondPath);
                AssetDatabase.SaveAssets();
                File.WriteAllText(log, "OK " + DateTime.Now.ToString("s") + " code " + new FileInfo(codePath).Length + " bytes, second prefab " + new FileInfo(secondPath).Length + " bytes");
                Debug.Log("[RogueLab] built " + PrefabPath);
            }
            catch (Exception e)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(log));
                File.WriteAllText(log, "FAILED " + e);
                Debug.LogError("[RogueLab] " + e);
            }
        }

        static void Save(Type gamescript, string name, string code, string path)
        {
            var root = new GameObject(name);
            var script = (MonoBehaviour)root.AddComponent(gamescript);
            var so = new SerializedObject(script);
            so.FindProperty("Code").stringValue = code;
            so.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
        }

        [MenuItem("Tools/RogueLab/Park Other Mods")]
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
                Debug.Log("[RogueLab] parked " + name + (string.IsNullOrEmpty(err) ? "" : ": " + err));
            }
            AssetDatabase.Refresh();
        }

        [MenuItem("Tools/RogueLab/Restore Parked Mods")]
        public static void RestoreParked()
        {
            if (!AssetDatabase.IsValidFolder(Parked))
                return;
            foreach (string dir in AssetDatabase.GetSubFolders(Parked))
            {
                string name = Path.GetFileName(dir);
                string err = AssetDatabase.MoveAsset(dir, "Assets/" + name);
                Debug.Log("[RogueLab] restored " + name + (string.IsNullOrEmpty(err) ? "" : ": " + err));
            }
            if (AssetDatabase.GetSubFolders(Parked).Length == 0)
                AssetDatabase.DeleteAsset("Assets/_Parked");
            AssetDatabase.Refresh();
        }

        // Output goes straight to the game's Mods folder (ExportSettings.outputDirectory), left disabled by the exporter.
        [MenuItem("Tools/RogueLab/Export Mod")]
        public static void Export()
        {
            ParkOthers();
            foreach (string dir in AssetDatabase.GetSubFolders("Assets"))
            {
                string name = Path.GetFileName(dir);
                if (name.EndsWith("Mod") && name != Own)
                {
                    Debug.LogError("[RogueLab] " + name + " could not be parked: not exporting, or it would ship inside RogueLab");
                    return;
                }
            }
            ModTool.Editor.Exporting.ExportSettings.name = "RogueLabMod";
            ModTool.Editor.Exporting.ExportSettings.author = "Dimitri";
            ModTool.Editor.Exporting.ExportSettings.description = "Rogue Waves research build: runs a scripted solo test of the Rogue Wave mode's engine and logs the results. Not for play.";
            ModTool.Editor.Exporting.ExportSettings.version = RogueLabVersion;
            ModTool.Editor.Exporting.ExportSettings.content = ModTool.Shared.ModContent.Assets;
            AssetDatabase.SaveAssets();
            ModTool.Editor.Exporting.ModExporter.ExportMod();
        }

        const string RogueLabVersion = "0.3";
    }
}
