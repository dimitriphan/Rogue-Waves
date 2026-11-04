public static class ArgoLog
{
    const string Tag = "[Argo] ";

    public static void Info(string message) { Debug.Log(Tag + message); }
    public static void Warn(string message) { Debug.LogWarning(Tag + message); }
    public static void Error(string message) { Debug.LogError(Tag + message); }
}

// A brain's error count. Up to 1.2.2 a brain's catch switched it off for the match on its first exception, so one bad
// tick could silence a tower or a Naut for good; now the tick is skipped and the brain carries on, and only a brain that
// throws on every frame for ~10 s stops. The first three errors are logged in full, then one line every 500.
public sealed class ArgoFaults
{
    int count, streak, lastFrame = -10;

    // True when the brain should stop: it threw on every frame for 600 frames running (~10 s).
    public bool Fail(string who, Exception e)
    {
        count++;
        streak = Time.frameCount - lastFrame <= 1 ? streak + 1 : 1;
        lastFrame = Time.frameCount;
        if (count <= 3) ArgoLog.Warn(who + ": " + e);
        else if (count % 500 == 0) ArgoLog.Warn(who + ": " + count + " errors, last: " + e.Message);
        if (streak < 600) return false;
        ArgoLog.Error(who + ": threw every frame for 600 frames, stopped");
        return true;
    }
}

public static class ArgoUtil
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
        obj.Id = Clip(name.ToLowerInvariant() + "-argo");
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

    public static float Flat(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    public static float Stat(Entity_Master master, string name, float fallback)
    {
        Link_Stats stat;
        return master != null && master.Statsserver != null && master.Statsserver.TryGetValue(name, out stat) ? stat.Value : fallback;
    }

    // Full health of an entity (Dodamage clamps Health to Value_Calc).
    public static float MaxHealth(Entity_Master master)
    {
        Link_Stats stat;
        return master != null && master.Statsserver != null && master.Statsserver.TryGetValue("Health", out stat) ? stat.Value_Calc : 0f;
    }

    public static bool HasTag(Entity_Master master, string tag)
    {
        return master != null && master.Taglistserver != null && master.Taglistserver.Contains(tag);
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

    // A hit with an explicit damage value (both keys present, as Dodamage reads both).
    public static Dictionary<string, float> Hit(float damage, float multi)
    {
        return new Dictionary<string, float> { { "Damage", damage }, { "Damagemulti", multi } };
    }

    // Plays a named effect on every player's machine (handled by ArgoClient).
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

    public static float F(string s)
    {
        float v;
        return float.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out v) ? v : 0f;
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
            if (kv.Length != 2)
                continue;
            Link_Resourceset c = new Link_Resourceset();
            c.Name = kv[0].Trim();
            c.Amount = F(kv[1]);
            costs.Add(c);
        }
        return costs;
    }

    // "Damage_Add=3;Range_Multi=0.5" -> name/value pairs, in order.
    public static Dictionary<string, float> Params(string spec)
    {
        Dictionary<string, float> d = new Dictionary<string, float>();
        if (string.IsNullOrEmpty(spec))
            return d;
        foreach (string part in spec.Split(';'))
        {
            string[] kv = part.Split('=');
            if (kv.Length == 2)
                d[kv[0].Trim()] = F(kv[1]);
        }
        return d;
    }

    // The engine attaches AI and visual scripts with Type.GetType(listEntry.name): only an entry's name matters. A named
    // carrier lets Argo's data use any of the game's own generic scripts (Entity_Ai_Mob_Combat, Entity_Vis_Mob_Attack...).
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

    public static void Unmod(Link_Modifier mod)
    {
        if (mod != null && !mod.Dead && mod.Remove != null)
            mod.Remove.Invoke();
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

    // Owner-only message, shown by ArgoClient on the owner's screen.
    public static void Warn(Entity_Master master, int code)
    {
        Vis(master, "Argo_Warn", Pos(master), code);
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

    // {"Ref":<NetworkObjectId>} as sent by the select commands; 0 when absent.
    public static ulong JsonRef(string json)
    {
        int at = json != null ? json.IndexOf("\"Ref\":", StringComparison.Ordinal) : -1;
        if (at < 0)
            return 0;
        int i = at + 6, j = i;
        while (j < json.Length && char.IsDigit(json[j])) j++;
        ulong id;
        return j > i && ulong.TryParse(json.Substring(i, j - i), out id) ? id : 0;
    }

    public static Entity_Master ById(ulong id)
    {
        if (id == 0 || Game_Entity.Instance == null || Game_Entity.Instance.Entityclient == null)
            return null;
        foreach (Entity_Master m in Game_Entity.Instance.Entityclient)
            if (m != null && m.NetworkObjectId == id)
                return m;
        return null;
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
        catch (Exception e) { ArgoLog.Warn("delayed action: " + e.Message); }
    }

    // A foe's Speed, never below 0. The game sums every Multi modifier of a stat into Value_Multi (which starts at 1),
    // computes (Base x Pmulti + Add) x Multi and clamps it only to Value_Bounds. The shipped enemies' Speed carries
    // Limits of 0.05..99999 (read in game by the 1.4 self-tests), so a hold leaves them at 0.05, the game's own stop; a
    // Speed without Limits has bounds of +-infinity and would go negative under a hold plus a slow. Every hold and slow
    // on a foe goes through Root/Slow, which raise a lower bound under 0 to 0 (the game's own floor is kept).
    public static Link_Stats Speed(Entity_Master foe)
    {
        Link_Stats s;
        if (foe == null || foe.Statsserver == null || !foe.Statsserver.TryGetValue("Speed", out s) || s == null)
            return null;
        if (s.Value_Bounds.x < 0f)
            s.Value_Bounds = new Vector2(0f, s.Value_Bounds.y);
        return s;
    }

    // A brief hold in place: Speed at its floor (0, or the game's own 0.05 for its enemies) whatever slows it carries (up to 1.2.2 a -0.9
    // slow, which let a held foe creep at a tenth of its speed).
    public static void Root(Entity_Master by, Entity_Master target, float seconds)
    {
        if (target == null || !Alive(target) || seconds <= 0f || Speed(target) == null)
            return;
        Helper_Entitytools.Addmod(target, "Speed", -1f, Helper_Entitytools.Modtypes.Multi, seconds);
        ArgoStatus.Slowed(target, seconds);
        Debuff(by, target, "Debuff_Slow", seconds);
    }

    // 2.0 Pinned: a hold that counts as Pinned for the Scatter Cannon, Bedrock and Salt Sting. Twice as long in Deep
    // Water; with stun (Anchor Line) a foe beside any building is also stunned, unable to attack while held (the
    // concept's "pinning against a wall stuns": Argo has no walls and the game no knockback). Clients draw the rope bits
    // or the stars on the foe (Argo_Pin: x = the foe's id, y = seconds, z = 1 for a stun). Returns the seconds applied.
    public static float Pin(Entity_Master by, Entity_Master target, float seconds, bool stun)
    {
        if (target == null || !Alive(target) || seconds <= 0f || Speed(target) == null)
            return 0f;
        if (ArgoStatus.IsWet(target))
            seconds *= ArgoData.WaterPinMulti;
        stun = stun && Beside(target);
        Root(by, target, seconds);
        ArgoStatus.Pinned(target, seconds);
        if (stun && target.Statsserver.ContainsKey("Attackspeed"))
            Helper_Entitytools.Addmod(target, "Attackspeed", -0.95f, Helper_Entitytools.Modtypes.Multi, seconds);
        if (stun)
            ArgoStatus.Stuns++;
        ArgoStatus.Pins++;
        Vis(by, "Argo_Pin", new Vector3((float)target.NetworkObjectId, seconds, stun ? 1f : 0f), seconds);
        return seconds;
    }

    // A building (any team's tower, Argo's or not) within StunReach of the foe.
    public static bool Beside(Entity_Master foe)
    {
        Vector3 at = Pos(foe);
        foreach (Entity_Master b in Area(at, ArgoData.StunReach + 0.5f, Tagged("Tower")))
            if (b != null && b != foe && Alive(b) && Flat(Pos(b), at) <= ArgoData.StunReach)
                return true;
        return false;
    }

    // ---------------------------------------------------------------- the harpoon's reel (1.4; 2.0: Reel and Undertow)

    // Foes being reeled, with when their reel gives up: a foe is reeled by one harpoon at a time.
    static readonly Dictionary<Entity_Master, float> reeling = new Dictionary<Entity_Master, float>();

    public static bool Reeling(Entity_Master foe)
    {
        float until;
        return foe != null && reeling.TryGetValue(foe, out until) && until > Time.time;
    }

    // Reels a foe up to 'tiles' toward a point over 'seconds', stopping 'minDist' short of it. The kit template's HOOK reel: the
    // foe walks itself to a tile on the way, on its own Move command sped up for the moment, never physics, so the pull
    // looks the same on every screen and the foe's AI takes its path up again from where it lands. Boss-weight foes,
    // foes already close and foes with no free tile toward the point aren't moved. onDone runs when the foe arrives (or
    // at once when it isn't moved), unless the foe is already being reeled. Returns where the foe is going (null: not
    // moved).
    public static Vector3? Reel(MonoBehaviour host, Entity_Master foe, Vector3 toward, float tiles, float seconds, float minDist, Action<Entity_Master> onDone)
    {
        if (host == null || foe == null || !Alive(foe) || Reeling(foe))
            return null;
        Vector3? to = ReelTile(foe, toward, tiles, minDist);
        if (to == null)
        {
            if (onDone != null) onDone(foe);
            return null;
        }
        reeling[foe] = Time.time + seconds + 0.2f;
        if (reeling.Count > 32)
        {
            List<Entity_Master> stale = new List<Entity_Master>();
            foreach (KeyValuePair<Entity_Master, float> kv in reeling)
                if (kv.Key == null || kv.Value < Time.time) stale.Add(kv.Key);
            foreach (Entity_Master e in stale) reeling.Remove(e);
        }
        Link_Modifier boost = Helper_Entitytools.Addmod(foe, "Speed", 5f, Helper_Entitytools.Modtypes.Multi, seconds);
        host.StartCoroutine(ReelRun(foe, to.Value, seconds, boost, onDone));
        // 2.0 Fault Line: a Drill Rig whose Tremor the reel crosses bursts the foe.
        ArgoServer.Reeled(foe, Pos(foe), to.Value);
        return to;
    }

    static Vector3? ReelTile(Entity_Master foe, Vector3 toward, float tiles, float minDist)
    {
        Link_Being being = foe.Beingserver;
        if (being == null || being.Currentblock == null || foe.Commandlistserver == null || !foe.Commandlistserver.ContainsKey("Move")
            || MaxHealth(foe) >= ArgoData.BossHealth)
            return null;
        Vector3 from = being.Currentblock.Position;
        Vector3 dir = toward - from;
        dir.y = 0f;
        float pull = Mathf.Min(tiles, dir.magnitude - minDist);
        if (pull < 0.5f)
            return null;
        dir.Normalize();
        // The farthest standable tile on the way first, then shorter pulls.
        for (float k = pull; k >= 0.5f; k -= 0.5f)
        {
            Vector3 p = from + dir * k;
            Genitem block = null;
            foreach (float dy in new[] { 0f, -1f, 1f })
                if (block == null) block = Surface(new Vector3(Mathf.Round(p.x), Mathf.Round(p.y) + dy, Mathf.Round(p.z)));
            if (block == null || block == being.Currentblock || Flat(block.Position, toward) < minDist)
                continue;
            Genitem stand = null;
            try { stand = Helper_Gridtools.Checkifstandable(block, being); } catch (Exception) { }
            if (stand == null)
                continue;
            Link_Path path = Helper_Navtools.Getpath(being.Currentblock, stand, being, false);
            if (path.Valid && path.Length <= Mathf.CeilToInt(k) + 2)
                return stand.Position;
        }
        return null;
    }

    // A foe's own combat AI re-targets on its slow tick, so for the moment of the reel any order it gives itself is
    // replaced by the walk to the reel spot. The speed boost ends when the foe arrives: left on, it would outrun an
    // Anchor Line hold (+5 and -1 Multi still leave it five times as fast).
    static IEnumerator ReelRun(Entity_Master foe, Vector3 to, float seconds, Link_Modifier boost, Action<Entity_Master> onDone)
    {
        float end = Time.time + seconds;
        while (foe != null && Alive(foe) && Time.time < end)
        {
            Vector3 at = Pos(foe);
            if (new Vector2(at.x - to.x, at.z - to.z).sqrMagnitude < 0.06f)
                break;
            Link_Command move;
            if (foe.Commandlistserver.TryGetValue("Move", out move) && move != null
                && (foe.State.Value.ToString() != "Move" || move.Goalobject != null || (move.Goalposition - to).sqrMagnitude > 0.01f))
            {
                move.Goalobject = null;
                move.Goalposition = to;
                move.Goalvalue = 0f;
                move.Goalstring = "Idle";
                Helper_Entitytools.Updatecommand(foe, "Move", "", true, true);
            }
            yield return null;
        }
        Unmod(boost);
        if (foe != null)
            reeling.Remove(foe);
        if (foe != null && Alive(foe) && onDone != null)
        {
            try { onDone(foe); }
            catch (Exception e) { ArgoLog.Warn("reel: " + e.Message); }
        }
    }

    // Slows on their own never take a foe under 10% of its speed: the modifier is cut to what is left above that, and
    // skipped on a foe already that slow or held (the slow look still plays).
    public static void Slow(Entity_Master by, Entity_Master target, float amount, float seconds, bool vfx)
    {
        if (target == null || !Alive(target) || amount <= 0f)
            return;
        Link_Stats s = Speed(target);
        if (s == null)
            return;
        ArgoStatus.Slowed(target, seconds);
        float cut = Mathf.Min(Mathf.Clamp01(amount), s.Value_Multi - 0.1f);
        if (cut >= 0.01f)
            Helper_Entitytools.Addmod(target, "Speed", -cut, Helper_Entitytools.Modtypes.Multi, seconds);
        if (vfx)
            Debuff(by, target, "Debuff_Slow", seconds);
    }

    // The game's own debuff look on the target, on every player. The target goes by its synced Beingref, as the game's
    // own debuffs do: Link_Being.Beingrefparallel is copied before the spawn sets its Worldobject, so clients can't
    // resolve it and nothing showed (Argo 1.0's slows were invisible this way).
    // 2.0.1: sent with the ClientRpc itself. The game's Vfxdebuffcheck drops a look while 50 effects wait to be sent,
    // which happens in busy online fights (kit docs/19 §4, pitfall #95), and a slow is something players need to see.
    // The send still counts in the game's queue, so stock effects make room for it, and a look already showing until
    // this one's end is not sent again.
    static readonly Dictionary<Entity_Master, Dictionary<string, float>> shown = new Dictionary<Entity_Master, Dictionary<string, float>>();

    public static void Debuff(Entity_Master by, Entity_Master target, string vfx, float seconds)
    {
        if (by == null || by.Radio == null || target == null || target.Beingref == null)
            return;
        float end = Time.fixedTime + seconds, until;
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
        by.Radio.Vfxdebuff_ClientRpc(vfx, target.Beingref.Value, seconds);
    }

    // A one-off look a player must see (a refused build), past the same queue.
    public static void MustSee(Entity_Master by, string vfx, Vector3 at, Vector3 direction)
    {
        if (by == null || by.Radio == null)
            return;
        if (Game_Mapvars.Instance != null)
            Game_Mapvars.Instance.Clientsendstack++;
        by.Radio.Vfxoneshot_ClientRpc(vfx, at, direction);
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
}

// 2.0: which foes are Slowed, Pinned or in Deep Water right now (host only). The Scatter Cannon's bonus reads Slowed or
// Pinned; Bedrock and Salt Sting read Pinned; a pin lasts twice as long on a Wet foe. Every slow and hold Argo applies
// goes through ArgoUtil.Slow, Root or Pin, which record it here.
public static class ArgoStatus
{
    static readonly Dictionary<Entity_Master, float> slowed = new Dictionary<Entity_Master, float>();
    static readonly Dictionary<Entity_Master, float> pinned = new Dictionary<Entity_Master, float>();
    static readonly Dictionary<Entity_Master, float> wet = new Dictionary<Entity_Master, float>();
    public static int Pins, Stuns;

    public static void Reset()
    {
        slowed.Clear();
        pinned.Clear();
        wet.Clear();
    }

    public static void Slowed(Entity_Master foe, float seconds) { Mark(slowed, foe, seconds); }
    public static void Pinned(Entity_Master foe, float seconds) { Mark(pinned, foe, seconds); }
    public static void Wet(Entity_Master foe, float seconds) { Mark(wet, foe, seconds); }

    public static bool IsSlowed(Entity_Master foe) { return Active(slowed, foe); }
    public static bool IsPinned(Entity_Master foe) { return Active(pinned, foe); }
    public static bool IsWet(Entity_Master foe) { return Active(wet, foe); }
    public static bool Debuffed(Entity_Master foe) { return IsSlowed(foe) || IsPinned(foe); }

    // Self-tests only need it: a foe read as clean whatever slowed it before (the game's own slow mods stay).
    public static void Clear(Entity_Master foe)
    {
        if (foe == null) return;
        slowed.Remove(foe);
        pinned.Remove(foe);
        wet.Remove(foe);
    }

    static void Mark(Dictionary<Entity_Master, float> d, Entity_Master foe, float seconds)
    {
        if (foe == null || seconds <= 0f)
            return;
        float until;
        d[foe] = Mathf.Max(d.TryGetValue(foe, out until) ? until : 0f, Time.time + seconds);
        if (d.Count > 128)
        {
            List<Entity_Master> stale = new List<Entity_Master>();
            foreach (KeyValuePair<Entity_Master, float> kv in d)
                if (kv.Key == null || kv.Value < Time.time) stale.Add(kv.Key);
            foreach (Entity_Master e in stale) d.Remove(e);
        }
    }

    static bool Active(Dictionary<Entity_Master, float> d, Entity_Master foe)
    {
        float until;
        return foe != null && d.TryGetValue(foe, out until) && until > Time.time;
    }
}
