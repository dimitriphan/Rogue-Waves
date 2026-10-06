namespace ModTool.Shared { [System.Flags] public enum ModContent { Assets = 1, Scenes = 2, Code = 4 } }
namespace ModTool.Editor.Exporting
{
    public static class ExportSettings { public static string name, author, description, version; public static ModTool.Shared.ModContent content; }
    public static class ModExporter { public static void ExportMod() { } }
}
