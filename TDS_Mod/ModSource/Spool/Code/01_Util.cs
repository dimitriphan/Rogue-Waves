public static class SpoolLog
{
    const string Tag = "[Spool] ";

    public static void Info(string message) { Debug.Log(Tag + message); }
    public static void Warn(string message) { Debug.LogWarning(Tag + message); }
    public static void Error(string message) { Debug.LogError(Tag + message); }
}

// A brain's error count (pitfall #80; Argo's ArgoFaults, Uloe's UloeFaults). A brain's catch used to switch it off for the
// map on its first exception, so one bad tick could leave Spool, a building or a Crafted dead (SPL-4); now the tick is
// skipped and the brain carries on, and only a brain that throws on every frame for 600 frames (~10 s) stops. The first
// three errors are logged in full, then one line every 500. A kind of brain that comes in numbers (the Crafted) passes one
// shared log count, so a fault common to them logs three traces in all; the stop rule stays each brain's own.
public sealed class SpoolFaults
{
    public sealed class Log { public int Count; }

    int streak, lastFrame = -10;
    readonly Log log;

    public SpoolFaults() : this(null) { }
    public SpoolFaults(Log shared) { log = shared ?? new Log(); }

    // True when the brain should stop: it threw on every frame for 600 frames running.
    public bool Fail(string who, Exception e)
    {
        int count = ++log.Count;
        if (Time.frameCount != lastFrame)
            streak = Time.frameCount - lastFrame == 1 ? streak + 1 : 1;
        lastFrame = Time.frameCount;
        if (count <= 3) SpoolLog.Warn(who + ": " + e);
        else if (count % 500 == 0) SpoolLog.Warn(who + ": " + count + " errors, last: " + e.Message);
        if (streak < 600) return false;
        SpoolLog.Error(who + ": threw every frame for 600 frames, stopped");
        return true;
    }
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
