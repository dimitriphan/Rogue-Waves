public static class YosseleLog
{
    const string Tag = "[Yossele] ";

    public static void Info(string message) { Debug.Log(Tag + message); }
    public static void Warn(string message) { Debug.LogWarning(Tag + message); }
    public static void Error(string message) { Debug.LogError(Tag + message); }
}

// Keeps a brain running through a bad frame (Argo's pattern): logs the first errors, then every 500th, and only stops
// when it threw on every frame for 600 frames running (~10 s).
public sealed class YosseleFaults
{
    int count, streak, lastFrame = -10;

    public bool Fail(string who, Exception e)
    {
        count++;
        streak = Time.frameCount - lastFrame <= 1 ? streak + 1 : 1;
        lastFrame = Time.frameCount;
        if (count <= 3) YosseleLog.Warn(who + ": " + e);
        else if (count % 500 == 0) YosseleLog.Warn(who + ": " + count + " errors, last: " + e.Message);
        if (streak < 600) return false;
        YosseleLog.Error(who + ": threw every frame for 600 frames, stopped");
        return true;
    }
}

public static class YosseleUtil
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
        obj.Id = Clip(name.ToLowerInvariant() + "-yossele");
    }

    // Names travel over the network as FixedString64Bytes (61 UTF-8 bytes max).
    public static string Clip(string value) { return value.Length > 60 ? value.Substring(0, 60) : value; }

    public static bool IsServer { get { return Game_Entity.Instance != null && Game_Entity.Instance.IsServer; } }

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
    public static bool Alive(Entity_Master master) { return master != null && master.Beingserver != null && !master.Beingserver.Dead; }
    public static Vector3 Pos(Entity_Master master) { return master.Entityposition.Value; }

    public static float Stat(Entity_Master master, string name, float fallback)
    {
        Link_Stats stat;
        return master != null && master.Statsserver != null && master.Statsserver.TryGetValue(name, out stat) ? stat.Value : fallback;
    }

    // A foe's base health from its data (its live Health is 0 by the time it is despawned).
    public static float BaseHealth(Entity_Master master)
    {
        Entityobject e = master != null && master.Beingserver != null ? master.Beingserver.Entity : null;
        if (e != null && e.Defaultstat != null)
            foreach (Statproperties.Defaultstatgroup g in e.Defaultstat.Stats)
                if (g.Name == "Health")
                    return g.Value;
        return Stat(master, "Health", 10f);
    }

    public static long LocalSession()
    {
        Blm_Server server = Blm_Server.Instance;
        if (server == null || server.Localplayer == null)
            return long.MinValue;
        return server.Localplayer.Sessionid.Value;
    }

    public static bool OwnedLocally(Entity_Master master) { return master != null && master.Entitydata.Value.Owner == LocalSession(); }

    public static bool IsFoe(Entity_Master e, int team)
    {
        if (e == null || !Alive(e) || Team(e) == team || e.Taglistserver == null)
            return false;
        return e.Taglistserver.Contains("Enemy") && !e.Taglistserver.Contains("Enemycore") && !e.Taglistserver.Contains("Core");
    }

    public static float Flat(Vector3 a, Vector3 b) { return new Vector2(a.x - b.x, a.z - b.z).magnitude; }

    public static bool HasTag(Entity_Master e, string tag) { return e != null && e.Taglistserver != null && e.Taglistserver.Contains(tag); }

    // The grid block under a world position (the build flow uses the nearest "Surface" entity the same way).
    public static Genitem Surface(Vector3 position)
    {
        Entity_Master surface = Helper_Entityquerytools.Entitynearest(position, 0.75f, Tagged("Surface"));
        return surface != null && surface.Beingserver != null ? surface.Beingserver.Currentblock : null;
    }

    // The ground tile at a point, snapped to the tile centre, at its level or one step down or up (in that order).
    public static Genitem Tile(Vector3 at)
    {
        foreach (float dy in new[] { 0f, -1f, 1f })
        {
            Vector3 p = new Vector3(Mathf.Round(at.x), Mathf.Round(at.y) + dy, Mathf.Round(at.z));
            Entity_Master s = Helper_Entityquerytools.Entitynearest(p, 0.5f, Tagged("Surface"));
            if (s != null && s.Beingserver != null && s.Beingserver.Currentblock != null)
                return s.Beingserver.Currentblock;
        }
        return null;
    }

    // A networked ground being (a vine, a fruit) on a tile, a child of 'parent' (its owner and team), Forced so it goes
    // in beside whatever stands there (kit docs/05's SpawnOnTile, Sno Sno's snow under towers).
    public static Entity_Master SpawnOnTile(Beingobject being, Genitem block, Entity_Master parent, string skin)
    {
        if (being == null || block == null || parent == null || parent.Beingserver == null)
            return null;
        Link_Spawn spawn = Helper_Entitytools.Createlinkspawnparent(parent.Beingserver);
        spawn.Genadd = block;
        spawn.Spawnstyle = Link_Spawn.Spawnstyles.Forced;
        spawn.Being = being;
        spawn.Skin = skin ?? "";
        spawn.Sellvalue = 0f;
        Link_Being made = Helper_Entitytools.Spawn(spawn, block.Position, parent.Beingserver, true, null);
        return made != null ? made.Worldobject : null;
    }

    public static void Unmod(Link_Modifier mod)
    {
        if (mod != null && !mod.Dead && mod.Remove != null)
            mod.Remove.Invoke();
    }

    // A modifier this code keeps and re-sets (a Growth bonus): made on first use, then its value changed in place.
    public static Link_Modifier Keep(Entity_Master master, Link_Modifier mod, string stat, Helper_Entitytools.Modtypes type, float value)
    {
        Link_Stats s;
        if (master == null || master.Statsserver == null || !master.Statsserver.TryGetValue(stat, out s))
            return mod;
        if (mod == null || mod.Dead)
            mod = Helper_Entitytools.Addmod(master, stat, value, type, 0f);
        else if (type == Helper_Entitytools.Modtypes.Add) mod.Value_Add = value;
        else if (type == Helper_Entitytools.Modtypes.Multi) mod.Value_Multi = value;
        else if (type == Helper_Entitytools.Modtypes.Pmulti) mod.Value_Pmulti = value;
        else mod.Value_Base = value;
        s.Value = s.Value_Calc;
        return mod;
    }

    // Limits of 0.05..99999 on the game's enemies; a Speed without Limits has bounds of +-infinity and would go negative
    // under a hold plus a slow (Argo 1.4), so every hold and slow raises a lower bound under 0 to 0.
    public static Link_Stats Speed(Entity_Master foe)
    {
        Link_Stats s;
        if (foe == null || foe.Statsserver == null || !foe.Statsserver.TryGetValue("Speed", out s) || s == null)
            return null;
        if (s.Value_Bounds.x < 0f)
            s.Value_Bounds = new Vector2(0f, s.Value_Bounds.y);
        return s;
    }

    // A hold in place (and, with stun, no attacks) for a while, with a debuff look.
    public static void Root(Entity_Master by, Entity_Master foe, float seconds, bool stun, string vfx)
    {
        if (foe == null || !Alive(foe) || seconds <= 0f || Speed(foe) == null)
            return;
        Helper_Entitytools.Addmod(foe, "Speed", -1f, Helper_Entitytools.Modtypes.Multi, seconds);
        if (stun && foe.Statsserver.ContainsKey("Attackspeed"))
            Helper_Entitytools.Addmod(foe, "Attackspeed", -0.95f, Helper_Entitytools.Modtypes.Multi, seconds);
        if (!string.IsNullOrEmpty(vfx))
            Debuff(by, foe, vfx, seconds);
    }

    // A slow that lasts until taken off (vines, Sticky Spores): never takes a foe under 10% of its speed.
    public static Link_Modifier SlowOn(Entity_Master foe, float amount)
    {
        Link_Stats s = Speed(foe);
        if (s == null || amount <= 0f)
            return null;
        float cut = Mathf.Min(Mathf.Clamp01(amount), s.Value_Multi - 0.1f);
        return cut >= 0.01f ? Helper_Entitytools.Addmod(foe, "Speed", -cut, Helper_Entitytools.Modtypes.Multi, 0f) : null;
    }

    // Health is a stat: Value is the health left, Value_Calc the maximum (RogueLab round 4).
    public static float MaxHealth(Entity_Master e)
    {
        Link_Stats s;
        return e != null && e.Statsserver != null && e.Statsserver.TryGetValue("Health", out s) ? s.Value_Calc : 0f;
    }

    public static float Health(Entity_Master e)
    {
        Link_Stats s;
        return e != null && e.Statsserver != null && e.Statsserver.TryGetValue("Health", out s) ? s.Value : 0f;
    }

    public static Link_Entityquery Foes(Entity_Master master)
    {
        Link_Entityquery query = new Link_Entityquery();
        query.Being = master.Beingserver;
        query.Teamfor = false;
        query.Tags = new List<string> { "Mob" };
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

    // Foes (tagged Enemy) within a radius, nearest first.
    public static List<Entity_Master> FoesNear(Entity_Master from, Vector3 position, float radius)
    {
        List<Entity_Master> list = new List<Entity_Master>();
        int team = Team(from);
        foreach (Entity_Master e in Area(position, radius, Foes(from)))
            if (IsFoe(e, team))
                list.Add(e);
        return list;
    }

    public static Dictionary<string, float> Data(string key, float value)
    {
        Dictionary<string, float> data = new Dictionary<string, float>();
        data[key] = value;
        return data;
    }

    // A hit for an exact amount, credited to 'from' (the kit records Lasthit, which gets the kill).
    public static void Hurt(Entity_Master from, Entity_Master to, float damage)
    {
        if (from == null || to == null || !Alive(to) || damage <= 0f)
            return;
        ModKitCombat.Hurt(from, to, damage, 1f, null, null);
    }

    public static void Heal(Entity_Master from, Entity_Master to, float amount)
    {
        if (from == null || to == null || !Alive(to) || amount <= 0f)
            return;
        Dictionary<string, float> d = new Dictionary<string, float> { { "Heal", amount }, { "Damage", amount }, { "Damagemulti", 1f } };
        Helper_Entitytools.Interact(from, to, "Heal", d);
    }

    // Plays a named effect on every player's machine (handled by YosseleClient).
    public static void Vis(Entity_Master master, string type, Vector3 position, float number)
    {
        if (master != null && master.Radio != null)
            master.Radio.Vis_ClientRpc(type, position, number);
    }

    // A debuff effect the game itself attaches to the target on every player (looked up by name under "Vfx"). The
    // target goes by its synced Beingref, as the game's own debuffs do: Link_Being.Beingrefparallel is copied before
    // the spawn fills in its Worldobject, so clients can't resolve it and the effect never appears.
    // Sent with the ClientRpc itself (Argo 2.0.1's way): the game's Vfxdebuffcheck drops a look while 50 effects wait to
    // be sent, which happens in busy online fights (kit docs/19 §4, pitfall #95). The send still counts in the game's
    // queue, and a look already showing until this one's end is not sent again (the vines renew theirs every tick).
    static readonly Dictionary<Entity_Master, Dictionary<string, float>> shown = new Dictionary<Entity_Master, Dictionary<string, float>>();

    public static void Debuff(Entity_Master radioOf, Entity_Master target, string vfx, float duration)
    {
        if (radioOf == null || radioOf.Radio == null || target == null || target.Beingref == null)
            return;
        float end = Time.fixedTime + duration, until;
        Dictionary<string, float> looks;
        if (!shown.TryGetValue(target, out looks))
        {
            if (shown.Count >= 256)
                ForgetLooks(false);
            shown[target] = looks = new Dictionary<string, float>();
        }
        if (looks.TryGetValue(vfx, out until) && until >= end - 0.1f)
            return;
        looks[vfx] = end;
        if (Game_Mapvars.Instance != null)
            Game_Mapvars.Instance.Clientsendstack++;
        radioOf.Radio.Vfxdebuff_ClientRpc(vfx, target.Beingref.Value, duration);
    }

    public static void RemoveDebuff(Entity_Master radioOf, Entity_Master target, string vfx)
    {
        if (radioOf == null || radioOf.Radio == null || target == null || target.Beingref == null)
            return;
        Dictionary<string, float> looks;
        if (shown.TryGetValue(target, out looks))
            looks.Remove(vfx);
        radioOf.Radio.Vfxremovedebuff_ClientRpc(vfx, target.Beingref.Value);
    }

    // All (a new map) or only the gone and the finished.
    public static void ForgetLooks(bool all)
    {
        if (all)
        {
            shown.Clear();
            return;
        }
        float now = Time.fixedTime;
        List<Entity_Master> gone = new List<Entity_Master>();
        foreach (KeyValuePair<Entity_Master, Dictionary<string, float>> kv in shown)
        {
            bool showing = false;
            foreach (float until in kv.Value.Values)
                if (until > now) showing = true;
            if (kv.Key == null || !showing)
                gone.Add(kv.Key);
        }
        foreach (Entity_Master e in gone)
            shown.Remove(e);
    }

    // Owner-only message, shown by YosseleClient on the owner's screen.
    public static void Warn(Entity_Master master, int code) { Vis(master, "Yossele_Warn", Pos(master), code); }

    // Network ids fit a float exactly below 2^24, which covers every entity of a match.
    public static Entity_Master ById(ulong id)
    {
        Game_Entity ge = Game_Entity.Instance;
        if (ge == null || ge.Entityclient == null)
            return null;
        foreach (Entity_Master e in ge.Entityclient)
            if (e != null && e.NetworkObjectId == id)
                return e;
        return null;
    }

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

    public static Dictionary<string, float> Params(string spec)
    {
        Dictionary<string, float> d = new Dictionary<string, float>();
        if (string.IsNullOrEmpty(spec))
            return d;
        foreach (string part in spec.Split(';'))
        {
            string[] kv = part.Split('=');
            float v;
            if (kv.Length == 2 && float.TryParse(kv[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out v))
                d[kv[0].Trim()] = v;
        }
        return d;
    }

    public static float F(string s)
    {
        float v;
        return float.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out v) ? v : 0f;
    }

    public static string Join(IEnumerable<string> items) { return string.Join(", ", new List<string>(items).ToArray()); }

    // The engine attaches AI and visual scripts with Type.GetType(listEntry.name): only an entry's name matters. A named
    // carrier lets Yossele's data use any of the game's own generic scripts.
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

    public static void Skin(Entity_Master master, string skin)
    {
        FixedString64Bytes value = new FixedString64Bytes(skin ?? "");
        if (master.Skin.Value != value)
            master.Skin.Value = value;
    }

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

    public static void Later(MonoBehaviour host, float delay, Action action)
    {
        if (host != null && host.isActiveAndEnabled)
            host.StartCoroutine(Delay(delay, action));
    }

    static IEnumerator Delay(float delay, Action action)
    {
        yield return new WaitForSeconds(delay);
        try { action(); }
        catch (Exception e) { YosseleLog.Warn("delayed action: " + e.Message); }
    }
}
