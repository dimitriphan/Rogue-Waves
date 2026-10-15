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
                Say("Unity is still compiling: run the queue again once it has finished");
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
