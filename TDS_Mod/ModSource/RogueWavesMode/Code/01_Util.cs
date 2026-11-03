public static class RWUtil
{
    public static Dictionary<string, UnityEngine.Object> Folder(string name)
    {
        Tds_Masterdatabase db = Tds_Masterdatabase.Instance;
        Dictionary<string, UnityEngine.Object> folder;
        if (db == null || db.Items == null || !db.Items.TryGetValue(name, out folder))
            return null;
        return folder;
    }

    public static T Item<T>(string folder, string name) where T : UnityEngine.Object
    {
        Dictionary<string, UnityEngine.Object> f = Folder(folder);
        UnityEngine.Object found;
        return f != null && name != null && f.TryGetValue(name, out found) ? found as T : null;
    }

    // A being, item or object from the loaded groups under a label ("Mob", "Mode"...), or null if its group isn't loaded.
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
        copy.Id = Clip(name.ToLowerInvariant() + "-roguewaves");
        return copy;
    }

    public static string Clip(string value) { return value.Length > 60 ? value.Substring(0, 60) : value; }

    public static bool IsServer { get { return Game_Entity.Instance != null && Game_Entity.Instance.IsServer; } }

    // The host on the boat (no Game_Entity there): the network host.
    public static bool IsHost { get { return Unity.Netcode.NetworkManager.Singleton != null && Unity.Netcode.NetworkManager.Singleton.IsHost; } }

    public static bool IsLeader { get { return Blm_Server.Instance != null && Blm_Server.Instance.Localplayer != null && Blm_Server.Instance.Localplayer.Isleader; } }

    public static string F(float v) { return v.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture); }

    public static string V(Vector3 v) { return F(v.x) + "," + F(v.y) + "," + F(v.z); }

    public static float Flat(Vector3 a, Vector3 b) { return new Vector2(a.x - b.x, a.z - b.z).magnitude; }

    // ---------------------------------------------------------------- the save

    public static string ReadString(string folder, string key)
    {
        Blockage.Tags.Bl_Tags.Entry e = Blockage.Tags.Bla_Data.Playerdata.Readdata(folder, key);
        return e.Loaded ? e.Valuestring ?? "" : "";
    }

    public static void WriteString(string folder, string key, string value) { Blockage.Tags.Bla_Data.Playerdata.Adddata(folder, key, value); }

    public static bool Has(string folder, string key) { return Blockage.Tags.Bla_Data.Playerdata.Readdata(folder, key).Loaded; }

    public static int ReadInt(string folder, string key, int missing)
    {
        int v;
        return int.TryParse(ReadString(folder, key), out v) ? v : missing;
    }

    public static void Remove(string folder, string key) { Blockage.Tags.Bla_Data.Playerdata.Removedata(folder, key); }

    public static void Flush() { Blockage.Tags.Bla_Data.Instance.Savedata(false, ""); }

    // ---------------------------------------------------------------- the map (host)

    // The enemy cores (where foes come from), alive.
    public static List<Entity_Master> EnemyCores()
    {
        List<Entity_Master> cores = new List<Entity_Master>();
        if (Game_Entity.Instance == null)
            return cores;
        foreach (Entity_Master e in Game_Entity.Instance.Entityclient)
            if (e != null && e.Taglistserver != null && e.Taglistserver.Contains("Enemycore") && e.Beingserver != null && !e.Beingserver.Dead)
                cores.Add(e);
        return cores;
    }

    // The player team's first living Basin (the host's lists; the client tag lists don't hold it).
    public static Entity_Master PlayerCore()
    {
        if (Game_Entity.Instance == null)
            return null;
        foreach (Entity_Master e in Game_Entity.Instance.Entityclient)
            if (e != null && e.Taglistserver != null && e.Taglistserver.Contains("Core") && !e.Taglistserver.Contains("Enemycore") && e.Beingserver != null && !e.Beingserver.Dead)
                return e;
        return null;
    }

    public static List<Entity_Master> PlayerCores()
    {
        List<Entity_Master> cores = new List<Entity_Master>();
        if (Game_Entity.Instance == null)
            return cores;
        foreach (Entity_Master e in Game_Entity.Instance.Entityclient)
            if (e != null && e.Taglistserver != null && e.Taglistserver.Contains("Core") && !e.Taglistserver.Contains("Enemycore") && e.Beingserver != null && !e.Beingserver.Dead)
                cores.Add(e);
        return cores;
    }

    // Every ground tile of the map, one per position.
    public static List<Genitem> Ground()
    {
        List<Genitem> tiles = new List<Genitem>();
        if (Game_Entity.Instance == null)
            return tiles;
        List<Entity_Master> surfaces;
        if (!Game_Entity.Instance.Entitiesserver.TryGetValue("Surface", out surfaces) || surfaces == null || surfaces.Count == 0)
        {
            surfaces = new List<Entity_Master>();
            foreach (Entity_Master e in Game_Entity.Instance.Entityclient)
                if (e != null && e.Taglistserver != null && e.Taglistserver.Contains("Surface"))
                    surfaces.Add(e);
        }
        List<Vector3> seen = new List<Vector3>();
        foreach (Entity_Master s in surfaces)
        {
            Genitem g = s != null && s.Beingserver != null ? s.Beingserver.Currentblock : null;
            if (g == null || seen.Contains(g.Position))
                continue;
            seen.Add(g.Position);
            tiles.Add(g);
        }
        return tiles;
    }

    public static int Count(string being)
    {
        int n = 0;
        if (Game_Entity.Instance == null)
            return 0;
        foreach (Entity_Master e in Game_Entity.Instance.Entityclient)
            if (e != null && e.Beingserver != null && e.Beingserver.Being != null && e.Beingserver.Being.Objectname == being && !e.Beingserver.Dead)
                n++;
        return n;
    }
}

// The pack's keys in its own save folder, RWData.SaveFolder (kit docs/23 §2). Each carries "RW_" (rule 3), so a stock
// tracker that searches every folder never reads one as its own (Player_Guid is also a stock key's name). Until
// 0.1.0-dev they had no prefix: Migrate renames any old key it finds, once per save object, before anything reads them.
public static class RWKeys
{
    public const string Seed = "RW_Campaign_Seed", Route = "RW_Campaign_Route", Retries = "RW_Campaign_Retries",
        Replay = "RW_Campaign_Replay", Playing = "RW_Campaign_Playing", Choice = "RW_Campaign_Choice", Staged = "RW_Campaign_Staging",
        StockWorld = "RW_Stock_World", StockGameplan = "RW_Stock_Gameplan",
        Guid = "RW_Player_Guid", Device = "RW_Player_Device";
    public static readonly string[] Old = { "Campaign_Seed", "Campaign_Route", "Stock_World", "Stock_Gameplan", "Player_Guid", "Player_Device" };
    public static int Moved;                     // old keys renamed since launch (self-test, log)
    static object migrated;

    // Every machine, every frame from launch (00_RogueWavesModeMod.Update, before RWIdentity); cheap once done.
    public static void Migrate()
    {
        object data = Blockage.Tags.Bla_Data.Playerdata;
        if (Blockage.Tags.Bla_Data.Instance == null || data == null || ReferenceEquals(data, migrated) || !Blockage.Tags.Bla_Data.Playerdata.Loaded)
            return;
        migrated = data;
        int moved = 0;
        foreach (string old in Old)
        {
            if (!RWUtil.Has(RWData.SaveFolder, old))
                continue;
            if (!RWUtil.Has(RWData.SaveFolder, RWData.KeyPrefix + old))
                RWUtil.WriteString(RWData.SaveFolder, RWData.KeyPrefix + old, RWUtil.ReadString(RWData.SaveFolder, old));
            RWUtil.Remove(RWData.SaveFolder, old);
            moved++;
        }
        if (moved == 0)
            return;
        Moved += moved;
        RWUtil.Flush();
        RWLog.Info("save: " + moved + " keys of " + RWData.SaveFolder + " renamed to " + RWData.KeyPrefix + "... (kit docs/23 §2 rule 3)");
    }
}
