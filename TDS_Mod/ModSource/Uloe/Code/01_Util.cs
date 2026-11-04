public static class UloeLog
{
    const string Tag = "[Uloe] ";

    public static void Info(string message) { Debug.Log(Tag + message); }
    public static void Warn(string message) { Debug.LogWarning(Tag + message); }
    public static void Error(string message) { Debug.LogError(Tag + message); }
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

    // The nearest aura node within a reach (dens are tagged Resource too, RogueLab round 7, and aren't nodes).
    public static Entity_Master NearestNode(Vector3 at, float reach)
    {
        foreach (Entity_Master e in ModKitUtil.Area(at, reach, ModKitUtil.Tagged("Resource")))
            if (IsNode(e))
                return e;
        return null;
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

    // Keeps a handler at the front of a thing's hit chain: Helper_Entitytools.Updateinteract rebuilds the chain from the
    // thing's AIs whenever an upgrade lands or leaves (Spool's Pin Cushion, Argo's hooks).
    public static void Front(Entity_Master m, Entity_Master.Oninteracts handler)
    {
        Entity_Master.Oninteracts chain = m.Oninteract;
        if (chain != null)
        {
            Delegate[] list = chain.GetInvocationList();
            if (list.Length > 0 && list[0].Equals(handler))
                return;
        }
        Delegate rest = Delegate.Remove(chain, handler);
        m.Oninteract = (Entity_Master.Oninteracts)Delegate.Combine(handler, rest);
    }

    public static void Unfront(Entity_Master m, Entity_Master.Oninteracts handler)
    {
        if (m != null && m.Oninteract != null)
            m.Oninteract = (Entity_Master.Oninteracts)Delegate.Remove(m.Oninteract, handler);
    }

    // The attacker's side of a hit (Interacthurt calls From.Interact before To.Oninteract). Updateinteract clears it too,
    // so a brain says this again a few times a second; taking the handler off first keeps it from piling up.
    public static void Listen(Entity_Master m, Entity_Master.Interacts handler)
    {
        m.Interact = (Entity_Master.Interacts)Delegate.Remove(m.Interact, handler);
        m.Interact = (Entity_Master.Interacts)Delegate.Combine(m.Interact, handler);
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
