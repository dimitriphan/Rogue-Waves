using System; using System.Reflection; using System.IO;
class P { static void Main(string[] a) {
  foreach (var f in a) {
    try {
      var asm = Assembly.LoadFrom(f);
      Type[] ts;
      try { ts = asm.GetExportedTypes(); } catch (ReflectionTypeLoadException e) { ts = e.Types; }
      foreach (var t in ts) if (t != null) Console.WriteLine(Path.GetFileNameWithoutExtension(f) + "\t" + t.FullName);
    } catch (Exception e) { Console.Error.WriteLine(f + ": " + e.Message); }
  }
}}
