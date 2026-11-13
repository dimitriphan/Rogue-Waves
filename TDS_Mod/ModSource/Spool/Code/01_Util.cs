public static class SpoolLog
{
    public const string Tag = "[Spool] ";

    public static void Info(string message) { Debug.Log(Tag + message); }
    public static void Warn(string message) { Debug.LogWarning(Tag + message); }
    public static void Error(string message) { Debug.LogError(Tag + message); }
}


public static class SpoolUtil
{
    // The Tide now: the mode's count while a campaign runs, else the map's own wave.
    public static int Wave() { return ModKitTides.Wave >= 0 ? ModKitTides.Wave : (Game_Mapvars.Instance != null ? Game_Mapvars.Instance.Wave.Value : 0); }

    public static T Clone<T>(T source, string name) where T : Bl_ScriptableObject
    {
        T copy = UnityEngine.Object.Instantiate(source);
        Retag(copy, name);
        return copy;
    }

    // Ids only need to be unique and identical on every player's machine.
    public static void Retag(Bl_ScriptableObject obj, string name)
    {
        obj.name = name;
        obj.Objectname = name;
        obj.Id = ModKitUtil.Clip(name.ToLowerInvariant() + "-spool");
    }

    // Owner-only message, shown by SpoolClient on the owner's screen.
    public static void Warn(Entity_Master master, int code) { ModKitUtil.Vis(master, "Spool_Warn", ModKitUtil.Pos(master), code); }

    public static void Later(MonoBehaviour host, float delay, Action action)
    {
        if (host != null && host.isActiveAndEnabled)
            host.StartCoroutine(Delay(delay, action));
    }

    static IEnumerator Delay(float delay, Action action)
    {
        yield return new WaitForSeconds(delay);
        try { action(); }
        catch (Exception e) { SpoolLog.Warn("delayed action: " + e.Message); }
    }
}
