public static class UloeLog
{
    const string Tag = "[Uloe] ";

    public static void Info(string message) { Debug.Log(Tag + message); }
    public static void Warn(string message) { Debug.LogWarning(Tag + message); }
    public static void Error(string message) { Debug.LogError(Tag + message); }
}

// A brain's error count (pitfall #80; Argo's ArgoFaults). A brain's catch used to switch it off for the map on its first
// exception, so one bad tick could leave Uloe, a building or a Photon dead; now the tick is skipped and the brain carries
// on, and only a brain that throws on every frame for 600 frames (~10 s) stops. The first three errors are logged in
// full, then one line every 500.
public sealed class UloeFaults
{
    int count, streak, lastFrame = -10;

    // True when the brain should stop: it threw on every frame for 600 frames running.
    public bool Fail(string who, Exception e)
    {
        count++;
        streak = Time.frameCount - lastFrame <= 1 ? streak + 1 : 1;
        lastFrame = Time.frameCount;
        if (count <= 3) UloeLog.Warn(who + ": " + e);
        else if (count % 500 == 0) UloeLog.Warn(who + ": " + count + " errors, last: " + e.Message);
        if (streak < 600) return false;
        UloeLog.Error(who + ": threw every frame for 600 frames, stopped");
        return true;
    }
}

public static class UloeUtil
{
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
        obj.Id = ModKitUtil.Clip(name.ToLowerInvariant() + "-uloe");
    }

    // Owner-only message, shown by UloeClient on the owner's screen.
    public static void Warn(Entity_Master master, int code) { ModKitUtil.Vis(master, "Uloe_Warn", ModKitUtil.Pos(master), code); }

    // The thing a picker clicked (its NetworkObjectId as {"Ref":id}); null if none.
    public static Entity_Master Picked(string json)
    {
        int at = json != null ? json.IndexOf("\"Ref\":", StringComparison.Ordinal) : -1;
        if (at < 0)
            return null;
        int i = at + 6, j = i;
        while (j < json.Length && char.IsDigit(json[j])) j++;
        ulong id;
        if (j == i || !ulong.TryParse(json.Substring(i, j - i), out id))
            return null;
        return ModKitUtil.ById(id);
    }

    // An aura node on the host: tagged Resource, with Healthresource (docs/16 section 4).
    public static bool IsNode(Entity_Master m)
    {
        return ModKitUtil.Alive(m) && m.Taglistserver != null && m.Taglistserver.Contains("Resource") && m.Statsserver != null && m.Statsserver.ContainsKey("Healthresource");
    }

    // The nearest aura node within a reach (dens are tagged Resource too, RogueLab round 7, and aren't nodes); 'except'
    // left out (Stadium Lights' second node).
    public static Entity_Master NearestNode(Vector3 at, float reach, Entity_Master except = null)
    {
        foreach (Entity_Master e in ModKitUtil.Area(at, reach, ModKitUtil.Tagged("Resource")))
            if (IsNode(e) && e != except)
                return e;
        return null;
    }

    // Whether one strike paid a node out (Yossele's Pachira reads it the same way): it refilled (after > before), or it
    // was full, the strike was enough to empty it and it refilled to the same number, or it went (a node that pays once).
    public static bool Paid(float before, float after, float hit, bool alive)
    {
        if (before < 0f)
            return false;
        if (!alive)
            return true;
        if (after > before + 0.001f)
            return true;
        return Mathf.Abs(after - before) <= 0.001f && before <= hit + 0.001f;
    }

    // A node on any machine, from the synced tag list (clients have no Taglistserver).
    public static bool IsNodeClient(Entity_Master m)
    {
        if (m == null || m.Taglist == null)
            return false;
        foreach (FixedString64Bytes t in m.Taglist)
            if (t.ToString() == "Resource")
                return true;
        return false;
    }

    // The node's total aura: every colour it pays when mined.
    public static float NodeAura(Entity_Master node)
    {
        return ModKitUtil.Stat(node, "Redauramine", 0f) + ModKitUtil.Stat(node, "Blueauramine", 0f) + ModKitUtil.Stat(node, "Yellowauramine", 0f);
    }

    // How far a point stands (flat) from the segment a to b.
    public static float SegmentDistance(Vector3 p, Vector3 a, Vector3 b)
    {
        Vector2 ab = new Vector2(b.x - a.x, b.z - a.z), ap = new Vector2(p.x - a.x, p.z - a.z);
        float len = ab.sqrMagnitude;
        float t = len > 0.0001f ? Mathf.Clamp01((ab.x * ap.x + ab.y * ap.y) / len) : 0f;
        return (ap - ab * t).magnitude;
    }

    public static void Later(MonoBehaviour host, float delay, Action action)
    {
        if (host != null && host.isActiveAndEnabled)
            host.StartCoroutine(Delay(delay, action));
    }

    static IEnumerator Delay(float delay, Action action)
    {
        yield return new WaitForSeconds(delay);
        try { action(); }
        catch (Exception e) { UloeLog.Warn("delayed action: " + e.Message); }
    }
}
