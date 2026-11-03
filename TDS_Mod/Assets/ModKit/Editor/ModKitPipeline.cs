// Tools > ModKit > Build Queue: one call (one UnityMCP execute_menu_item) builds and exports every vessel listed in
// Desktop\ClaudeProjects\handoff\queue.txt, in order: Build All Assets (twice on a line ending in " twice", as Sprigg
// needs), Build Mod Prefab, Export Mod (which parks the other vessels) and Restore Parked Mods. Build each vessel's code
// first (Build-<Name>Mod.ps1, with or without -Define), and let Unity finish compiling after a handoff is applied.
// Every step is written to handoff\queue_status.txt as it happens, ending in "QUEUE DONE ok=N failed=M", and logged as a
// "[ModKit]" line in Editor.log. Read the Editor.log lines (and the .info file's time) rather than waiting on the MCP
// call, which can time out on a long export (and must not be run a second time): MCP replays a timed-out call, and a
// replay can overwrite queue_status.txt after a good run (kit docs/09 #76).
// queue.txt, one vessel per line:   Argo
//                                   Sprigg twice
// Then Run-SelfTests.ps1 (self-test builds) or Run-SelfTests.ps1 -BootCheck (release builds).
//
// Tools > ModKit > Build Pack: the release pack, one RogueWavesMod bundle with one RogueWaves_<Part>_Default prefab per
// part (docs/PACK.md). Reads TDS_Mod/ModSource/Pack/pack.txt (version, parts, description). Run Tools/pc/Build-Pack.ps1 first: it
// builds each part's release code with the pack version. For each part: Build All Assets, Build Mod Prefab, then the
// prefab is renamed RogueWaves_<Part>_Default; every Assets/*Mod folder that is not a part is parked; one export as
// RogueWavesMod; then the names and the parked folders are put back. Status in handoff\pack_status.txt, ending in
// "PACK DONE ok" or "PACK FAILED". Then Build-Pack.ps1 -Zip.
using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace ModKitBuild
{
    public static class ModKitPipeline
    {
        const string GameMods = @"D:\Steam\steamapps\common\TDS\Mods";
        const string Pack = "RogueWavesMod";
        const string Parked = "Assets/_Parked/Editor";   // where every vessel's Export Mod parks the other mods

        static StringBuilder log;
        static string status;

        [MenuItem("Tools/ModKit/Build Queue")]
        public static void Run()
        {
            string handoff = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "ClaudeProjects", "handoff");
            string queue = Path.Combine(handoff, "queue.txt");
            status = Path.Combine(handoff, "queue_status.txt");
            log = new StringBuilder();
            if (!File.Exists(queue))
            {
                Say("no queue at " + queue);
                return;
            }
            if (EditorApplication.isCompiling)
            {
                // Logged only: a replayed MCP call lands here and must not overwrite the real run's status file (docs/09 #76).
                Debug.Log("[ModKit] Unity is still compiling: run the queue again once it has finished");
                return;
            }
            int ok = 0, failed = 0;
            foreach (string raw in File.ReadAllLines(queue))
            {
                string line = raw.Trim();
                if (line == "" || line.StartsWith("#"))
                    continue;
                string[] parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                string name = parts[0];
                bool twice = Array.IndexOf(parts, "twice") > 0;
                string source = Path.GetFullPath(Path.Combine(Application.dataPath, "../ModSource/" + name));
                try
                {
                    string code = Path.Combine(source, "Build/" + name + "Mod_Code.cs");
                    if (!File.Exists(code))
                        throw new Exception("no " + code + ": run Build-" + name + "Mod.ps1 first");
                    Say(name + ": code built " + File.GetLastWriteTime(code).ToString("HH:mm:ss") + Defines(code));
                    for (int i = 0; i < (twice ? 2 : 1); i++)
                    {
                        DateTime t = DateTime.Now.AddSeconds(-2);
                        Step(name, "Build All Assets");
                        Expect(Path.Combine(source, "build_log.txt"), "Build All Assets", t);
                    }
                    DateTime p = DateTime.Now.AddSeconds(-2);
                    Step(name, "Build Mod Prefab");
                    Expect(Path.Combine(source, "Build/prefab_log.txt"), "Build Mod Prefab", p);
                    DateTime before = DateTime.Now.AddSeconds(-2);
                    Step(name, "Export Mod");
                    string info = Path.Combine(GameMods, name + "Mod", name + "Mod.info");
                    if (!File.Exists(info) || File.GetLastWriteTime(info) < before)
                        throw new Exception("Export Mod finished but " + info + " wasn't written");
                    Say(name + ": exported (" + File.GetLastWriteTime(info).ToString("HH:mm:ss") + "; the exporter leaves it disabled)");
                    ok++;
                }
                catch (Exception e)
                {
                    failed++;
                    Say(name + ": FAILED " + e.Message);
                }
                finally
                {
                    EditorApplication.ExecuteMenuItem("Tools/" + name + "/Restore Parked Mods");
                    Say(name + ": parked mods restored (" + AssetDatabase.GetSubFolders("Assets").Length + " folders in Assets)");
                }
            }
            Say("QUEUE DONE ok=" + ok + " failed=" + failed);
        }

        [MenuItem("Tools/ModKit/Build Pack")]
        public static void BuildPack()
        {
            string handoff = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "ClaudeProjects", "handoff");
            status = Path.Combine(handoff, "pack_status.txt");
            log = new StringBuilder();
            if (EditorApplication.isCompiling)
            {
                // Logged only: a replayed MCP call lands here and must not overwrite the real run's status file (docs/09 #76).
                Debug.Log("[ModKit] Unity is still compiling: run Build Pack again once it has finished");
                return;
            }
            string version = "", description = "";
            var parts = new System.Collections.Generic.List<string>();
            var renamed = new System.Collections.Generic.List<string>();
            bool parked = false;
            try
            {
                string list = Path.GetFullPath(Path.Combine(Application.dataPath, "../ModSource/Pack/pack.txt"));
                if (!File.Exists(list))
                    throw new Exception("no " + list);
                foreach (string raw in File.ReadAllLines(list))
                {
                    string line = raw.Trim();
                    if (line == "" || line.StartsWith("#"))
                        continue;
                    int sp = line.IndexOf(' ');
                    string key = sp < 0 ? line : line.Substring(0, sp), value = sp < 0 ? "" : line.Substring(sp + 1).Trim();
                    if (key == "version") version = value;
                    else if (key == "part") parts.Add(value);
                    else if (key == "description") description = value;
                }
                if (version == "" || version.Contains("-") || parts.Count == 0)
                    throw new Exception("pack.txt needs a release version (no -dev) and at least one part");
                Say("pack " + Pack + " " + version + ": " + string.Join(", ", parts.ToArray()));

                foreach (string name in parts)
                {
                    string source = Path.GetFullPath(Path.Combine(Application.dataPath, "../ModSource/" + name));
                    string code = Path.Combine(source, "Build/" + name + "Mod_Code.cs");
                    if (!File.Exists(code))
                        throw new Exception("no " + code + ": run Build-Pack.ps1 first");
                    string text = File.ReadAllText(code);
                    if (text.StartsWith("#define"))
                        throw new Exception(name + "'s code is a debug build" + Defines(code) + ": run Build-Pack.ps1");
                    if (!text.Contains("const string Version = \"" + version + "\""))
                        throw new Exception(name + "'s code doesn't carry version " + version + ": run Build-Pack.ps1");
                    Say(name + ": release code " + version + ", built " + File.GetLastWriteTime(code).ToString("HH:mm:ss"));

                    string prefab = "Assets/" + name + "Mod/" + name + "Mod_Default.prefab";
                    // The mode and foes parts are named RogueWaves<Part>, and their dev prefab already has its pack name.
                    bool named = name.StartsWith("RogueWaves");
                    string part = named ? name.Substring("RogueWaves".Length) : name;
                    string packed = "Assets/" + name + "Mod/RogueWaves_" + part + "_Default.prefab";
                    if (!named && AssetDatabase.LoadAssetAtPath<GameObject>(packed) != null)
                        AssetDatabase.DeleteAsset(packed);   // left by a pack build that stopped half way
                    DateTime t = DateTime.Now.AddSeconds(-2);
                    Step(name, "Build All Assets");
                    Expect(Path.Combine(source, "build_log.txt"), "Build All Assets", t);
                    DateTime p = DateTime.Now.AddSeconds(-2);
                    Step(name, "Build Mod Prefab");
                    Expect(Path.Combine(source, "Build/prefab_log.txt"), "Build Mod Prefab", p);
                    // A prefab asset's root takes its file's name, so the bundle's object is RogueWaves_<Part>_Default.
                    if (!named)
                    {
                        string err = AssetDatabase.RenameAsset(prefab, "RogueWaves_" + name + "_Default");
                        if (!string.IsNullOrEmpty(err))
                            throw new Exception("renaming " + prefab + ": " + err);
                        renamed.Add(name);
                    }
                    GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(packed);
                    if (root == null)
                        throw new Exception(packed + " is missing after the rename");
                    if (root.name != "RogueWaves_" + part + "_Default")
                    {
                        GameObject contents = PrefabUtility.LoadPrefabContents(packed);
                        contents.name = "RogueWaves_" + part + "_Default";
                        PrefabUtility.SaveAsPrefabAsset(contents, packed);
                        PrefabUtility.UnloadPrefabContents(contents);
                    }
                    Say(name + ": " + packed + " (root " + AssetDatabase.LoadAssetAtPath<GameObject>(packed).name + ")");
                }

                parked = true;
                if (!AssetDatabase.IsValidFolder("Assets/_Parked")) AssetDatabase.CreateFolder("Assets", "_Parked");
                if (!AssetDatabase.IsValidFolder(Parked)) AssetDatabase.CreateFolder("Assets/_Parked", "Editor");
                foreach (string dir in AssetDatabase.GetSubFolders("Assets"))
                {
                    string folder = Path.GetFileName(dir);
                    if (!folder.EndsWith("Mod") || parts.Contains(folder.Substring(0, folder.Length - 3)))
                        continue;
                    string err = AssetDatabase.MoveAsset(dir, Parked + "/" + folder);
                    if (!string.IsNullOrEmpty(err))
                        throw new Exception("parking " + folder + ": " + err);
                    Say("parked " + folder);
                }
                AssetDatabase.Refresh();

                DateTime before = DateTime.Now.AddSeconds(-2);
                ModTool.Editor.Exporting.ExportSettings.name = Pack;
                ModTool.Editor.Exporting.ExportSettings.author = "Dimitri";
                ModTool.Editor.Exporting.ExportSettings.description = description;
                ModTool.Editor.Exporting.ExportSettings.version = version;
                ModTool.Editor.Exporting.ExportSettings.content = ModTool.Shared.ModContent.Assets;
                AssetDatabase.SaveAssets();
                Say("Export " + Pack);
                ModTool.Editor.Exporting.ModExporter.ExportMod();
                string info = Path.Combine(GameMods, Pack, Pack + ".info");
                if (!File.Exists(info) || File.GetLastWriteTime(info) < before)
                    throw new Exception("the export finished but " + info + " wasn't written");
                if (!File.ReadAllText(info).Contains("\"" + version + "\""))
                    throw new Exception(info + " doesn't carry version " + version);
                Say("exported " + info + " (" + File.GetLastWriteTime(info).ToString("HH:mm:ss") + "; the exporter leaves it disabled)");
                Say("PACK DONE ok");
            }
            catch (Exception e)
            {
                Say("PACK FAILED " + e.Message);
            }
            finally
            {
                foreach (string name in renamed)
                {
                    string err = AssetDatabase.RenameAsset("Assets/" + name + "Mod/RogueWaves_" + name + "_Default.prefab", name + "Mod_Default");
                    Say(name + ": prefab name restored" + (string.IsNullOrEmpty(err) ? "" : " FAILED: " + err));
                }
                if (parked && AssetDatabase.IsValidFolder(Parked))
                {
                    foreach (string dir in AssetDatabase.GetSubFolders(Parked))
                    {
                        string err = AssetDatabase.MoveAsset(dir, "Assets/" + Path.GetFileName(dir));
                        Say("restored " + Path.GetFileName(dir) + (string.IsNullOrEmpty(err) ? "" : " FAILED: " + err));
                    }
                    if (AssetDatabase.GetSubFolders(Parked).Length == 0)
                        AssetDatabase.DeleteAsset("Assets/_Parked");
                    AssetDatabase.Refresh();
                }
            }
        }

        static void Step(string name, string item)
        {
            Say(name + ": " + item);
            if (!EditorApplication.ExecuteMenuItem("Tools/" + name + "/" + item))
                throw new Exception("no menu item Tools/" + name + "/" + item);
        }

        // The step's own log file must be new and say "OK", not "FAILED" (as the builders write them).
        static void Expect(string file, string step, DateTime since)
        {
            string text = File.Exists(file) && File.GetLastWriteTime(file) >= since ? File.ReadAllText(file) : "";
            if (!text.Contains("OK") || text.Contains("FAILED"))
            {
                string first = text.Length > 300 ? text.Substring(0, 300) : text;
                throw new Exception(step + " did not finish cleanly (" + file + (text == "" ? " not written" : ": " + first.Replace("\n", " ")) + ")");
            }
        }

        static string Defines(string code)
        {
            StringBuilder d = new StringBuilder();
            foreach (string l in File.ReadAllLines(code))
            {
                if (!l.StartsWith("#define "))
                    break;
                d.Append(d.Length == 0 ? ", " : " ").Append(l.Substring(8));
            }
            return d.Length == 0 ? ", release build" : d.ToString();
        }

        static void Say(string m)
        {
            log.AppendLine(DateTime.Now.ToString("HH:mm:ss") + " " + m);
            Directory.CreateDirectory(Path.GetDirectoryName(status));
            File.WriteAllText(status, log.ToString());
            Debug.Log("[ModKit] " + m);
        }
    }
}
