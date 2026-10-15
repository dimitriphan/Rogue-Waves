public static class RogueLabData
{
    public const string Version = "0.5.2";            // 0.2: round 2, 0.3: round 3 (docs/RESEARCH_GAPS.md), 0.4: round 4, 0.4.1: round 5, 0.4.2: round 6 (docs/reports/MECHANICS_CHECK.md), 0.5: round 7 (kit docs/17 §12, docs/16 §9)
    public const bool Rerun = false;                     // true: round 7's second and third launches (0.5.1, 0.5.2) reran only what was left open
    public const int Round = 7;                          // 4: start from the cloned main menu button, no Cardinal, map 1's mechanic probes only; 7: Play, no Cardinal, round 7's probes
    public const string Mode = "Mode_RogueLab";          // the mod mode's entry, group and Modeobject name
    public const string StockMode = "Mode_Normal";
    public const string PlanPrefix = "RogueLab_";        // + the stock Gameplan's name
    public const string Cardinal = "Artifact_RogueLab";
    public const string Hero = "Hero_Boutman";
    public const string Bro = "Mob_Boutmanbro";
    public const string SaveFolder = "RogueLab";
    public const int Tides = 3;                          // Tides kept from Mode_Normal's table
    public const string SecondMarker = "RogueLab_SecondAlive";   // left by the bundle's second prefab (Second/RogueLabSecond.cs)
    public const float TourTimeout = 150f;               // seconds a tour map may take to start before the run stops
}

public static class RogueLabLog
{
    const string Tag = "[RogueLab] ";

    public static void Info(string message) { Debug.Log(Tag + message); }
    public static void Test(string message) { Debug.Log(Tag + "TEST " + message); }
}

// One line per question: PASS / FAIL for a yes-no answer, INFO for a value the chapter needs, SETUP when the test
// could not get to the question (nothing is learned about it).
public static class RogueLabProbe
{
    public static Action<string> Log = delegate { };
    public static int Pass, Fail, Setups;

    public static bool Check(string name, bool ok, string detail = "")
    {
        if (ok) Pass++; else Fail++;
        Log((ok ? "PASS " : "FAIL ") + name + (detail != "" ? ": " + detail : ""));
        return ok;
    }

    public static void Info(string name, string detail) { Log("INFO " + name + ": " + detail); }

    public static void Setup(string name, string why)
    {
        Setups++;
        Log("SETUP " + name + ": " + why);
    }
}

public static class RogueLabUtil
{
    public static Dictionary<string, UnityEngine.Object> Folder(string name)
    {
        Tds_Masterdatabase db = Tds_Masterdatabase.Instance;
        Dictionary<string, UnityEngine.Object> folder;
        if (db == null || db.Items == null || !db.Items.TryGetValue(name, out folder))
            return null;
        return folder;
    }

    public static Heroentryobject Heroentry(string id)
    {
        Dictionary<string, UnityEngine.Object> heroes = Folder("Hero");
        UnityEngine.Object found;
        return heroes != null && heroes.TryGetValue(id, out found) ? found as Heroentryobject : null;
    }

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

    public static T Clone<T>(T source, string name) where T : Bl_ScriptableObject
    {
        T copy = UnityEngine.Object.Instantiate(source);
        copy.name = name;
        copy.Objectname = name;
        copy.Id = Clip(name.ToLowerInvariant() + "-roguelab");
        return copy;
    }

    public static string Clip(string value) { return value.Length > 60 ? value.Substring(0, 60) : value; }

    public static bool IsServer { get { return Game_Entity.Instance != null && Game_Entity.Instance.IsServer; } }

    public static string BeingName(Entity_Master master)
    {
        if (master == null)
            return "-";
        if (master.Beingserver != null && master.Beingserver.Being != null)
            return master.Beingserver.Being.Objectname;
        return master.Being.Value.ToString();
    }

    public static long Owner(Entity_Master master) { return master.Entitydata.Value.Owner; }
    public static bool Alive(Entity_Master master) { return master != null && master.Beingserver != null && !master.Beingserver.Dead; }

    public static long LocalId { get { return Blm_Server.Instance != null && Blm_Server.Instance.Localplayer != null ? Blm_Server.Instance.Localplayer.Sessionid.Value : -1; } }

    public static string LocalHero
    {
        get
        {
            Tds_Clientinfo info = Blm_Server.Instance != null && Blm_Server.Instance.Localplayer != null ? Blm_Server.Instance.Localplayer.GetComponent<Tds_Clientinfo>() : null;
            return info != null ? info.Hero.Value.ToString() : "";
        }
    }

    // The local player's entities in the game's "Hero" tag list, in list order (what F cycles through).
    public static List<Entity_Master> Heroes()
    {
        List<Entity_Master> mine = new List<Entity_Master>();
        List<Entity_Master> list;
        if (Game_Entity.Instance == null || !Game_Entity.Instance.Entityclienttags.TryGetValue("Hero", out list))
            return mine;
        foreach (Entity_Master e in list)
            if (e != null && Owner(e) == LocalId)
                mine.Add(e);
        return mine;
    }

    // The local player's vessel itself: its being is the one the hero entry names.
    public static Entity_Master MainHero()
    {
        Beingobject being = Bl_Address.Getload("Hero", LocalHero) as Beingobject;
        foreach (Entity_Master e in Heroes())
            if (being != null && BeingName(e) == being.Objectname && Alive(e))
                return e;
        return null;
    }

    public static string Names(List<Entity_Master> list)
    {
        List<string> n = new List<string>();
        foreach (Entity_Master e in list) n.Add(BeingName(e) + "#" + (e != null ? e.GetInstanceID().ToString() : "null"));
        return string.Join(", ", n.ToArray());
    }

    public static string F(float v) { return v.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture); }
}
