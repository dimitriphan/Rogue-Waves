// EXAMPLE, frozen: Sprigg 1.x code from ModProjects (Sprigg itself waits there for its overhaul), kept because the
// kit docs cite it.
public static class SpriggLog
{
    const string Tag = "[Sprigg] ";

    public static void Info(string message) { Debug.Log(Tag + message); }
    public static void Warn(string message) { Debug.LogWarning(Tag + message); }
    public static void Error(string message) { Debug.LogError(Tag + message); }
}

public static class SpriggUtil
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
        obj.Id = Clip(name.ToLowerInvariant() + "-sprigg");
    }

    // Names travel over the network as FixedString64Bytes (61 UTF-8 bytes max).
    public static string Clip(string value)
    {
        return value.Length > 60 ? value.Substring(0, 60) : value;
    }

    public static bool IsServer
    {
        get { return Game_Entity.Instance != null && Game_Entity.Instance.IsServer; }
    }

    public static string BeingName(Entity_Master master)
    {
        if (master == null)
            return "";
        if (master.Beingserver != null && master.Beingserver.Being != null)
            return master.Beingserver.Being.Objectname;
        return master.Being.Value.ToString();
    }

    public static long Owner(Entity_Master master) { return master.Entitydata.Value.Owner; }
    public static int Team(Entity_Master master) { return master.Entitydata.Value.Team; }

    public static bool Alive(Entity_Master master)
    {
        return master != null && master.Beingserver != null && !master.Beingserver.Dead;
    }

    public static Vector3 Pos(Entity_Master master) { return master.Entityposition.Value; }

    // Grid cell of an entity, used as the key for Mulch and planting.
    public static Vector3Int Cell(Vector3 position) { return Helper_Gridtools.Worldtotrueposition(position); }

    public static float Stat(Entity_Master master, string name, float fallback)
    {
        Link_Stats stat;
        return master != null && master.Statsserver != null && master.Statsserver.TryGetValue(name, out stat) ? stat.Value : fallback;
    }

    public static long LocalSession()
    {
        Blm_Server server = Blm_Server.Instance;
        if (server == null || server.Localplayer == null)
            return long.MinValue;
        return server.Localplayer.Sessionid.Value;
    }

    public static bool OwnedLocally(Entity_Master master)
    {
        return master != null && master.Entitydata.Value.Owner == LocalSession();
    }

    public static Link_Entityquery Enemies(Entity_Master master)
    {
        Link_Entityquery query = new Link_Entityquery();
        query.Being = master.Beingserver;
        query.Teamfor = false;
        query.Tags = new List<string> { "Mob", "Tower" };
        query.Style = Link_Entityquery.Styles.Both;
        return query;
    }

    public static Link_Entityquery Allies(Entity_Master master, string tag)
    {
        Link_Entityquery query = new Link_Entityquery();
        query.Being = master.Beingserver;
        query.Teamfor = true;
        query.Tags = new List<string> { tag };
        query.Style = Link_Entityquery.Styles.Both;
        return query;
    }

    public static Link_Entityquery Tagged(string tag)
    {
        Link_Entityquery query = new Link_Entityquery();
        query.Tags = new List<string> { tag };
        query.Style = Link_Entityquery.Styles.Tag;
        return query;
    }

    public static Entity_Master[] Area(Vector3 position, float radius, Link_Entityquery query)
    {
        Entity_Master[] found = Helper_Entityquerytools.Entityarea(position, radius, query, true);
        return found ?? new Entity_Master[0];
    }

    public static Dictionary<string, float> Data(string key, float value)
    {
        Dictionary<string, float> data = new Dictionary<string, float>();
        data[key] = value;
        return data;
    }

    // Plays a named effect on every player's machine (handled by SpriggClient).
    public static void Vis(Entity_Master master, string type, Vector3 position, float number)
    {
        if (master != null && master.Radio != null)
            master.Radio.Vis_ClientRpc(type, position, number);
    }

    public static List<Link_Resourceset> Scale(List<Link_Resourceset> costs, float scale)
    {
        List<Link_Resourceset> scaled = new List<Link_Resourceset>();
        if (costs == null)
            return scaled;
        foreach (Link_Resourceset cost in costs)
        {
            Link_Resourceset copy = cost;
            copy.Amount = Mathf.Max(1f, Mathf.Ceil(cost.Amount * scale));
            scaled.Add(copy);
        }
        return scaled;
    }

    public static string Join(IEnumerable<string> items)
    {
        return string.Join(", ", new List<string>(items).ToArray());
    }

    // "Redaura=30;Blueaura=15" -> resource list.
    public static List<Link_Resourceset> Costs(string spec)
    {
        List<Link_Resourceset> costs = new List<Link_Resourceset>();
        if (string.IsNullOrEmpty(spec))
            return costs;
        foreach (string part in spec.Split(';'))
        {
            string[] kv = part.Split('=');
            float amount;
            if (kv.Length != 2 || !float.TryParse(kv[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out amount))
                continue;
            Link_Resourceset c = new Link_Resourceset();
            c.Name = kv[0].Trim();
            c.Amount = amount;
            costs.Add(c);
        }
        return costs;
    }

    // The engine attaches AI and visual scripts with Type.GetType(listEntry.name): only an entry's name matters. A named
    // carrier lets Sprigg's data use any of the game's own generic scripts (Entity_Ai_Mob_Ranged, Bramble...).
    static readonly Dictionary<string, UnityEngine.Object> carriers = new Dictionary<string, UnityEngine.Object>();

    public static UnityEngine.Object Script(string className)
    {
        UnityEngine.Object carrier;
        if (carriers.TryGetValue(className, out carrier) && carrier != null)
            return carrier;
        carrier = new TextAsset("");
        carrier.name = className;
        carrier.hideFlags = HideFlags.DontUnloadUnusedAsset;
        carriers[className] = carrier;
        return carrier;
    }

    // Spawns a leftover on an entity's own tile the way the game's walls leave Perma-Wall stumps and spikes (Forced, same
    // owner and team as the parent).
    public static Link_Being SpawnOnTile(Beingobject being, Entity_Master parent, List<Upgradeproperties> upgrades)
    {
        if (being == null || parent == null || parent.Beingserver == null)
            return null;
        Link_Spawn spawn = new Link_Spawn();
        spawn.Genadd = parent.Beingserver.Currentblock;
        spawn.Spawnstyle = Link_Spawn.Spawnstyles.Forced;
        spawn.Being = being;
        spawn.Preupgrades = upgrades ?? new List<Upgradeproperties>();
        spawn.Orientation = parent.Beingserver.Orientation;
        return Helper_Entitytools.Spawn(spawn, parent.Position.Value, parent.Beingserver, true, null);
    }

    // The entity with this Entity.Name on a tile (Sno uses this to find snow under a tower).
    public static Entity_Master OnTile(Entity_Master master, string entityName)
    {
        if (master == null || master.Beingserver == null || master.Beingserver.Currentblock == null)
            return null;
        return Helper_Entityai.Getfirstonblock(master.Beingserver.Currentblock, entityName);
    }

    // A counter stat shown in the entity's stat panel (like the archer's Deathmarkkill), driven through a Base modifier.
    public static Link_Modifier Counter(Entity_Master master, string name, float value)
    {
        if (!master.Statsserver.ContainsKey(name))
        {
            Link_Stats stat = new Link_Stats();
            stat.Name = name;
            stat.Value_Base = 0f;
            stat.Style = Statproperties.Defaultstatgroup.Styles.Counter;
            stat.Display_Type = Statproperties.Displaytypes.Int;
            stat.Display_Exposed = true;
            Helper_Entitytools.Addstat(master, stat, 0f);
        }
        Link_Modifier mod = Helper_Entitytools.Addmod(master, name, value, Helper_Entitytools.Modtypes.Base, 0f);
        Link_Stats s;
        if (master.Statsserver.TryGetValue(name, out s))
            s.Value = s.Value_Calc;
        return mod;
    }

    public static void SetCounter(Entity_Master master, Link_Modifier mod, string name, float value)
    {
        if (mod == null)
            return;
        mod.Value_Base = value;
        Link_Stats s;
        if (master != null && master.Statsserver.TryGetValue(name, out s))
            s.Value = s.Value_Calc;
    }

    public static void Skin(Entity_Master master, string skin)
    {
        FixedString64Bytes value = new FixedString64Bytes(skin ?? "");
        if (master.Skin.Value != value)
            master.Skin.Value = value;
    }

    // The grid block under a world position (the build flow uses the nearest "Surface" entity the same way).
    public static Genitem Surface(Vector3 position)
    {
        Entity_Master surface = Helper_Entityquerytools.Entitynearest(position, 0.75f, Tagged("Surface"));
        return surface != null && surface.Beingserver != null ? surface.Beingserver.Currentblock : null;
    }

    // Spawns a buildable the way Entity_Ai_Hero_Buildsell does, owned by the parent's player. Returns null if blocked.
    public static Link_Being SpawnPlant(Beingobject being, Vector3 position, long owner, int team, int orientation, List<Upgradeproperties> upgrades)
    {
        if (being == null || Game_Entity.Instance == null)
            return null;
        Genitem block = Surface(position);
        if (block == null || !Helper_Gridtools.Checkifplaceable(block, being, orientation, team))
            return null;
        Link_Spawn spawn = new Link_Spawn();
        spawn.Being = being;
        spawn.Genadd = block;
        spawn.Position = block.Position;
        spawn.Orientation = orientation;
        spawn.Team = team;
        spawn.Owner = owner;
        spawn.Subowners = new List<long>();
        spawn.Skin = "";
        spawn.Spawnstyle = Link_Spawn.Spawnstyles.Spawn;
        spawn.Preupgrades = upgrades ?? new List<Upgradeproperties>();
        spawn.Sellvalue = 1f;
        spawn.Upgradesellvalue = 1f;
        return Game_Entity.Instance.Spawn(spawn);
    }

    // First free horizontal neighbour of any of the given positions where the being can be placed.
    public static Link_Being SpawnAdjacent(Beingobject being, List<Vector3> around, long owner, int team, List<Upgradeproperties> upgrades)
    {
        List<Vector3> options = new List<Vector3>();
        foreach (Vector3 a in around)
            foreach (Vector3 d in Game_Grid_Rules.Adjacentshorizontal)
                options.Add(a + d);
        for (int i = options.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            Vector3 t = options[i];
            options[i] = options[j];
            options[j] = t;
        }
        foreach (Vector3 p in options)
        {
            Link_Being spawned = SpawnPlant(being, p, owner, team, 0, upgrades);
            if (spawned != null)
                return spawned;
        }
        return null;
    }

    // Owner-only message, shown by SpriggClient on the owner's screen.
    public static void Warn(Entity_Master master, int code)
    {
        Vis(master, "Sprigg_Warn", Pos(master), code);
    }

    // Minimal JSON field readers for the game's flat command payloads (JsonUtility isn't referenced for mods).
    public static string JsonString(string json, string field)
    {
        string key = "\"" + field + "\"";
        int i = json.IndexOf(key, StringComparison.Ordinal);
        if (i < 0) return "";
        i = json.IndexOf(':', i + key.Length);
        if (i < 0) return "";
        int start = json.IndexOf('"', i + 1);
        if (start < 0) return "";
        int end = json.IndexOf('"', start + 1);
        return end > start ? json.Substring(start + 1, end - start - 1) : "";
    }

    public static double JsonNumber(string json, string field)
    {
        string key = "\"" + field + "\"";
        int i = json.IndexOf(key, StringComparison.Ordinal);
        if (i < 0) return 0;
        i = json.IndexOf(':', i + key.Length);
        if (i < 0) return 0;
        int j = i + 1;
        while (j < json.Length && (json[j] == ' ' || json[j] == '"')) j++;
        int k = j;
        while (k < json.Length && (char.IsDigit(json[k]) || json[k] == '-' || json[k] == '.' || json[k] == 'e' || json[k] == 'E' || json[k] == '+')) k++;
        double v;
        return double.TryParse(json.Substring(j, k - j), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out v) ? v : 0;
    }

    public static bool StateIs(Entity_Master master, params string[] states)
    {
        string s = master.State.Value.ToString();
        foreach (string w in states)
            if (s.Contains(w))
                return true;
        return false;
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
        catch (Exception e) { SpriggLog.Warn("delayed action: " + e.Message); }
    }
}
