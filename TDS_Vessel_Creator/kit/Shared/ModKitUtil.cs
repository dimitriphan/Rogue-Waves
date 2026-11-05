// Kit 1.9 (2026-11-05). Versions and compatibility rules: TDS_Vessel_Creator\kit\README.md
// Shared kit: the small helpers every vessel had its own copy of (the database, beings, stats, foes near a point, hits,
// debuff looks, costs, json fields). Each part keeps only what names it: Retag and Clone (the "-<vessel>" id suffix),
// Warn, Later, its own rules, and Argo's ById, Costs and Params (they read ids and numbers a little differently).
// Before kit 1.8 each vessel carried these in <Vessel>Util; the bodies came unchanged
// (CODE_SIMPLIFICATION_2026-11-04); kit 1.9 changed `Debuff`.
public static class ModKitUtil
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

    // The grid block under a world position (the build flow uses the nearest "Surface" entity the same way).
    public static Genitem Surface(Vector3 position)
    {
        Entity_Master surface = Helper_Entityquerytools.Entitynearest(position, 0.75f, Tagged("Surface"));
        return surface != null && surface.Beingserver != null ? surface.Beingserver.Currentblock : null;
    }

    // The walkable tile at a point: the grid is looked up by exact tile position, so the point snaps to the tile centre
    // first, at its level or one step down or up (the ground's Surface entity carries the tile).
    public static Genitem Tile(Vector3 at)
    {
        Link_Entityquery surface = Tagged("Surface");
        foreach (float dy in new[] { 0f, -1f, 1f })
        {
            Vector3 p = new Vector3(Mathf.Round(at.x), Mathf.Round(at.y) + dy, Mathf.Round(at.z));
            Entity_Master s = Helper_Entityquerytools.Entitynearest(p, 0.5f, surface);
            if (s != null && s.Beingserver != null && s.Beingserver.Currentblock != null)
                return s.Beingserver.Currentblock;
        }
        return null;
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

    public static float MaxHealth(Entity_Master m)
    {
        Link_Stats s;
        return m != null && m.Statsserver != null && m.Statsserver.TryGetValue("Health", out s) ? s.Value_Calc : 0f;
    }

    public static float Health(Entity_Master m) { return Stat(m, "Health", 0f); }

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

    public static bool HasTag(Entity_Master e, string tag) { return e != null && e.Taglistserver != null && e.Taglistserver.Contains(tag); }

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

    // A hit for an exact amount, credited to 'from': the kit records Lasthit, which gets the kill (kit 1.3, docs/17 §2.6).
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

    // Plays a named effect on every player's machine (each part's client brain handles its own types).
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
    // queue, and a look already showing until this one's end is not sent again, with one exception: any send of a look
    // sets its time, shorter or not (Helper_Vfxtools.Debuff), so another source's short one can cut this part's long
    // one; a caller still renewing it sends it once more in its last half second (kit 1.9).
    static readonly Dictionary<Entity_Master, Dictionary<string, Vector2>> shown = new Dictionary<Entity_Master, Dictionary<string, Vector2>>();   // x: shown until, y: sent at

    public static void Debuff(Entity_Master radioOf, Entity_Master target, string vfx, float duration)
    {
        if (radioOf == null || radioOf.Radio == null || target == null || target.Beingref == null)
            return;
        float now = Time.fixedTime, end = now + duration;
        Dictionary<string, Vector2> looks;
        if (!shown.TryGetValue(target, out looks))
        {
            if (shown.Count >= 256)
                ForgetLooks(false);
            shown[target] = looks = new Dictionary<string, Vector2>();
        }
        Vector2 look;
        if (looks.TryGetValue(vfx, out look))
        {
            if (look.x >= end - 0.1f && (look.x - now >= 0.5f || look.y >= look.x - 0.5f))
                return;
            end = Mathf.Max(end, look.x);
        }
        looks[vfx] = new Vector2(end, now);
        if (Game_Mapvars.Instance != null)
            Game_Mapvars.Instance.Clientsendstack++;
        radioOf.Radio.Vfxdebuff_ClientRpc(vfx, target.Beingref.Value, end - now);
    }

    public static void RemoveDebuff(Entity_Master radioOf, Entity_Master target, string vfx)
    {
        if (radioOf == null || radioOf.Radio == null || target == null || target.Beingref == null)
            return;
        Dictionary<string, Vector2> looks;
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
        foreach (KeyValuePair<Entity_Master, Dictionary<string, Vector2>> kv in shown)
        {
            bool showing = false;
            foreach (Vector2 look in kv.Value.Values)
                if (look.x > now) showing = true;
            if (kv.Key == null || !showing)
                gone.Add(kv.Key);
        }
        foreach (Entity_Master e in gone)
            shown.Remove(e);
    }

    // ---------------------------------------------------------------- content (beings and upgrades built at boot)

    // Script names written for one shipped vessel (they hard-code its objects); everything else is generic.
    public static readonly string[] VesselNames = { "Smacky", "Snosno", "Boutman", "Fie", "Buster" };

    public static bool VesselSpecific(string name)
    {
        if (name == "Entity_Ai_General_Upgrade")
            return true;
        foreach (string v in VesselNames)
            if (name.Contains(v))
                return true;
        return false;
    }

    // A stock being's scripts without the vessel-specific ones and without the stock upgrade AI (a part adds that back
    // where its buildings upgrade).
    public static List<UnityEngine.Object> Strip(List<UnityEngine.Object> scripts)
    {
        List<UnityEngine.Object> kept = new List<UnityEngine.Object>();
        if (scripts == null)
            return kept;
        foreach (UnityEngine.Object s in scripts)
            if (s != null && !VesselSpecific(s.name))
                kept.Add(s);
        return kept;
    }

    public static void AddSkin(Visualproperties vp, string skin, Visualproperties.Entry entry)
    {
        Visualproperties.Param p = new Visualproperties.Param();
        p.Name = skin;
        p.Value = entry;
        vp.Skinoverride.Add(p);
        vp.Skinoverrides[skin] = entry;
    }

    public static void CollectUpgrades(List<Upgradeproperties> from, Dictionary<int, Upgradeproperties> byTier)
    {
        if (from == null)
            return;
        foreach (Upgradeproperties u in from)
        {
            if (u == null)
                continue;
            if (!byTier.ContainsKey(u.Priority))
                byTier[u.Priority] = u;
            CollectUpgrades(u.Upgrades, byTier);
        }
    }

    // ---------------------------------------------------------------- hit chains

    // Keeps a handler at the front of a thing's hit chain: Helper_Entitytools.Updateinteract rebuilds the chain from the
    // thing's AIs whenever an upgrade lands or leaves, so every part's hit hooks call this again a few times a second.
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
    // carrier lets a part's data use any of the game's own generic scripts.
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

    // Same-team units and buildings (the tags match on any of them, as Argo's Enemies query does).
    public static Link_Entityquery Friends(Entity_Master master)
    {
        Link_Entityquery query = new Link_Entityquery();
        query.Being = master.Beingserver;
        query.Teamfor = true;
        query.Tags = new List<string> { "Mob", "Tower" };
        query.Style = Link_Entityquery.Styles.Both;
        return query;
    }

    // An elite carries one of the game's Enemy_* modifiers (kit docs/13 section 4).
    public static bool Elite(Entity_Master foe)
    {
        if (foe == null || foe.Upgradelistserver == null)
            return false;
        foreach (string key in foe.Upgradelistserver.Keys)
            if (key.StartsWith("Enemy_"))
                return true;
        return false;
    }

    // Asks a unit to walk: to a target (Goalobject) or a point, stopping 'stop' tiles short of it (Hero_Buildsell walks
    // to a building it will build next to that way), then going Idle, where the stock combat AIs pick their fights.
    public static void MoveTo(Entity_Master unit, Entity_Master target, Vector3 position, float stop = 0f)
    {
        Link_Command move;
        if (unit == null || unit.Commandlistserver == null || !unit.Commandlistserver.TryGetValue("Move", out move))
            return;
        move.Goalobject = target;
        move.Goalposition = target != null ? Vector3.one * -1f : position;
        move.Goalvalue = stop;
        move.Goalstring = "Idle";
        Helper_Entitytools.Updatecommand(unit, "Move", "", true, true);
    }

    public static void Stop(Entity_Master unit) { Helper_Entitytools.Updatecommand(unit, "Idle", "", true, true); }

    public static string State(Entity_Master unit) { return unit != null ? unit.State.Value.ToString() : ""; }

    // {"Position":{"x":1.0,"y":2.0,"z":3.0}}: the stock Relocate picker's message (the fish's Relocatedata).
    public static bool JsonPosition(string json, out Vector3 at)
    {
        at = Vector3.zero;
        if (string.IsNullOrEmpty(json) || json.IndexOf("\"Position\"", StringComparison.Ordinal) < 0)
            return false;
        at = new Vector3((float)JsonNumber(json, "x"), (float)JsonNumber(json, "y"), (float)JsonNumber(json, "z"));
        return true;
    }
}
