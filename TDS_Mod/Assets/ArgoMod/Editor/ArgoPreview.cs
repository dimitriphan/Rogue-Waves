// Argo asset generator: quick looks at the pixel art (no import, no models): ModSource/Argo/Previews/wip/*.png, scaled up
// on a two-tone checker.
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ArgoBuild
{
    public static class ArgoPreview
    {
        public static string Source { get { return Path.GetFullPath(Path.Combine(Application.dataPath, "../ModSource/Argo")); } }

        public static void Save(string dir, string name, Px px, int cell)
        {
            int k = px.W <= 32 ? 8 : px.W <= 128 ? 4 : 3;
            var bg = new Px(px.W * k, px.H * k);
            for (int y = 0; y < bg.H; y++)
                for (int x = 0; x < bg.W; x++)
                    bg.C[y * bg.W + x] = ((x / (cell * k)) + (y / (cell * k))) % 2 == 1 ? new Color32(150, 165, 150, 255) : new Color32(138, 153, 138, 255);
            bg.Draw(px.Scale(k), 0, 0);
            bg.Save(Path.Combine(dir, name + ".png"));
        }

        [MenuItem("Tools/Argo/Preview Sprites")]
        public static void PreviewSprites()
        {
            string dir = Path.Combine(Source, "Previews", "wip");
            Directory.CreateDirectory(dir);
            Save(dir, "main", ArgoCharacter.MainSheet(), 32);
            Save(dir, "attack", ArgoCharacter.VariantSheet(ArgoCharacter.Attack()), 32);
            Save(dir, "sonar", ArgoCharacter.VariantSheet(ArgoCharacter.Sonar()), 32);
            Save(dir, "fall", ArgoCharacter.Strip(ArgoCharacter.Fall()), 32);
            Save(dir, "land", ArgoCharacter.Strip(ArgoCharacter.Land()), 32);
            Save(dir, "mini", ArgoCharacter.MiniSheet(), 16);
            Save(dir, "naut_port", ArgoCharacter.NautSheet('r', 'R'), 16);
            Save(dir, "naut_starboard", ArgoCharacter.NautSheet('e', 'E'), 16);
            Save(dir, "naut_bosun", ArgoCharacter.NautSheet('u', 'U'), 16);
            Save(dir, "naut_tune_port", ArgoCharacter.NautTuneSheet('r', 'R'), 16);
            Save(dir, "readyup", ArgoCharacter.ReadyUp(), 12);
            Save(dir, "tabicon", ArgoCharacter.TabIcon(), 32);
            Save(dir, "splash", ArgoCharacter.Splash(), 128);
            Save(dir, "endwin", ArgoCharacter.EndWin(), 32);
            Save(dir, "endlose", ArgoCharacter.EndLose(), 32);
            // One big idle frame, front and back, at 12x for a close look.
            var big = new List<Px> { ArgoCharacter.Frame(0, new Pose()), ArgoCharacter.Frame(2, new Pose()), ArgoCharacter.Frame(1, new Pose()) };
            var strip = ArgoCharacter.Strip(big);
            var bg = new Px(strip.W * 12, strip.H * 12);
            for (int i = 0; i < bg.C.Length; i++) bg.C[i] = new Color32(108, 134, 168, 255);
            bg.Draw(strip.Scale(12), 0, 0);
            bg.Save(Path.Combine(dir, "closeup.png"));
            Debug.Log("[Argo] sprite previews in " + dir);
        }
    }
}
