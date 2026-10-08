public static class RiptideLog
{
    const string Tag = "[Riptide] ";

    public static void Info(string message) { Debug.Log(Tag + message); }
    public static void Warn(string message) { Debug.LogWarning(Tag + message); }
    public static void Error(string message) { Debug.LogError(Tag + message); }
}

// A tick that throws skips that frame and carries on (kit docs/09: one bad frame must not switch the mode off). It logs
// the first three with their stack, then a count every 500, and gives up only after 600 failing frames in a row.
public sealed class RiptideFaults
{
    int count, streak, lastFrame = -10;

    public bool Fail(string who, Exception e)
    {
        count++;
        streak = Time.frameCount - lastFrame <= 1 ? streak + 1 : 1;
        lastFrame = Time.frameCount;
        if (count <= 3)
            RiptideLog.Warn(who + ": " + e);
        else if (count % 500 == 0)
            RiptideLog.Warn(who + ": " + count + " errors, last: " + e.Message);
        if (streak < 600)
            return false;
        RiptideLog.Error(who + ": threw every frame for 600 frames, stopped");
        return true;
    }
}

public static class RiptideUtil
{
    public static Dictionary<string, UnityEngine.Object> Folder(string name)
    {
        Tds_Masterdatabase db = Tds_Masterdatabase.Instance;
        Dictionary<string, UnityEngine.Object> folder;
        if (db == null || db.Items == null || !db.Items.TryGetValue(name, out folder))
            return null;
        return folder;
    }

    public static T Entry<T>(string folder, string id) where T : UnityEngine.Object
    {
        Dictionary<string, UnityEngine.Object> items = Folder(folder);
        UnityEngine.Object found;
        return items != null && id != null && items.TryGetValue(id, out found) ? found as T : null;
    }

    // Bl_Address.Getload, but quiet when the label or item is missing.
    public static T Load<T>(string label, string name) where T : UnityEngine.Object
    {
        List<Bl_Address.Loadlist> lists;
        if (string.IsNullOrEmpty(name) || !Bl_Address.Groups.TryGetValue(label, out lists))
            return null;
        foreach (Bl_Address.Loadlist list in lists)
        {
            UnityEngine.Object item;
            if (list.Items.TryGetValue(name, out item) && item is T)
                return (T)item;
        }
        return null;
    }

    // Ids only need to be unique and identical on every player's machine.
    public static void Retag(Bl_ScriptableObject obj, string name)
    {
        obj.name = name;
        obj.Objectname = name;
        obj.Id = Clip(name.ToLowerInvariant() + "-riptide");
    }

    // Names travel over the network as FixedString64Bytes (61 UTF-8 bytes max).
    public static string Clip(string value) { return value.Length > 60 ? value.Substring(0, 60) : value; }

    // True on the host, on the world map and in a match (Game_Entity only exists in a match).
    public static bool IsHost { get { return Tds_Multiplayerinfo.Instance != null && Tds_Multiplayerinfo.Instance.IsServer; } }

    public static bool InMatch { get { return Game_Mapvars.Instance != null && Game_Entity.Instance != null; } }

    // True while the match being played is a Riptide run (on every machine: the mode is synced by name).
    public static bool InRun
    {
        get
        {
            return InMatch && RiptideContent.Mode != null && Game_Mapvars.Instance.Mode == RiptideContent.Mode;
        }
    }

    public static int Players()
    {
        return Blm_Server.Instance != null && Blm_Server.Instance.Players != null ? Mathf.Max(1, Blm_Server.Instance.Players.Count) : 1;
    }

    public static string BeingName(Entity_Master master)
    {
        if (master == null)
            return "";
        if (master.Beingserver != null && master.Beingserver.Being != null)
            return master.Beingserver.Being.Objectname;
        return master.Being.Value.ToString();
    }

    public static bool Alive(Entity_Master master) { return master != null && master.Beingserver != null && !master.Beingserver.Dead; }
    public static int Team(Entity_Master master) { return master.Entitydata.Value.Team; }

    public static bool HasTag(Entity_Master master, string tag)
    {
        if (master == null)
            return false;
        if (master.Taglistserver != null && master.Taglistserver.Contains(tag))
            return true;
        Entityobject e = master.Beingserver != null ? master.Beingserver.Entity : null;
        return e != null && e.Tags != null && e.Tags.Contains(tag);
    }

    // Every living entity on this machine carrying a tag (the host sees all of them).
    public static List<Entity_Master> Tagged(string tag)
    {
        List<Entity_Master> list = new List<Entity_Master>();
        if (Game_Entity.Instance == null)
            return list;
        foreach (Entity_Master e in Game_Entity.Instance.Entityclient)
            if (Alive(e) && HasTag(e, tag))
                list.Add(e);
        return list;
    }

    public static float BaseStat(Beingobject being, string stat, float fallback)
    {
        Entityobject e = being != null ? being.Entity : null;
        if (e != null && e.Defaultstat != null && e.Defaultstat.Stats != null)
            foreach (Statproperties.Defaultstatgroup g in e.Defaultstat.Stats)
                if (g.Name == stat)
                    return g.Value;
        return fallback;
    }

    public static string F(float v) { return v.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture); }

    public static string ScriptNames(List<UnityEngine.Object> scripts)
    {
        List<string> names = new List<string>();
        if (scripts != null)
            foreach (UnityEngine.Object s in scripts)
                if (s != null)
                    names.Add(s.name);
        return string.Join("+", names.ToArray());
    }

    // One line per Tide content, for the logs: "6xEnemy_Spear Spread 1.2s [Enemy_Health_Upgradeobject] +aura".
    public static string Describe(Wavesobject.Wavecontent c)
    {
        string s = c.Count + "x" + (c.Enemy != null ? c.Enemy.Objectname : "-") + " " + c.Style + " " + F(c.Interval) + "s";
        if (c.Upgrades != null && c.Upgrades.Count > 0)
        {
            List<string> ups = new List<string>();
            foreach (Upgradeproperties u in c.Upgrades)
                if (u != null) ups.Add(u.Objectname);
            s += " [" + string.Join(",", ups.ToArray()) + "]";
        }
        if (c.Resources != null && c.Resources.Count > 0)
        {
            List<string> res = new List<string>();
            foreach (Link_Resourceset r in c.Resources)
                res.Add(r.Name + " " + F(r.Amount));
            s += " +{" + string.Join(", ", res.ToArray()) + "}";
        }
        return s;
    }
}
