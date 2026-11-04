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
        obj.Id = ModKitUtil.Clip(name.ToLowerInvariant() + "-argo");
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

    // A hit with an explicit damage value (both keys present, as Dodamage reads both).
    public static Dictionary<string, float> Hit(float damage, float multi)
    {
        return new Dictionary<string, float> { { "Damage", damage }, { "Damagemulti", multi } };
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
            c.Amount = ModKitUtil.F(kv[1]);
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
                d[kv[0].Trim()] = ModKitUtil.F(kv[1]);
        }
        return d;
    }

    // Owner-only message, shown by ArgoClient on the owner's screen.
    public static void Warn(Entity_Master master, int code)
    {
        ModKitUtil.Vis(master, "Argo_Warn", ModKitUtil.Pos(master), code);
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

    // A brief hold in place: Speed at its floor (0, or the game's own 0.05 for its enemies) whatever slows it carries (up to 1.2.2 a -0.9
    // slow, which let a held foe creep at a tenth of its speed).
    public static void Root(Entity_Master by, Entity_Master target, float seconds)
    {
        if (target == null || !ModKitUtil.Alive(target) || seconds <= 0f || ModKitUtil.Speed(target) == null)
            return;
        Helper_Entitytools.Addmod(target, "Speed", -1f, Helper_Entitytools.Modtypes.Multi, seconds);
        ArgoStatus.Slowed(target, seconds);
        ModKitUtil.Debuff(by, target, "Debuff_Slow", seconds);
    }

    // 2.0 Pinned: a hold that counts as Pinned for the Scatter Cannon, Bedrock and Salt Sting. Twice as long in Deep
    // Water; with stun (Anchor Line) a foe beside any building is also stunned, unable to attack while held (the
    // concept's "pinning against a wall stuns": Argo has no walls and the game no knockback). Clients draw the rope bits
    // or the stars on the foe (Argo_Pin: x = the foe's id, y = seconds, z = 1 for a stun). Returns the seconds applied.
    public static float Pin(Entity_Master by, Entity_Master target, float seconds, bool stun)
    {
        if (target == null || !ModKitUtil.Alive(target) || seconds <= 0f || ModKitUtil.Speed(target) == null)
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
        ModKitUtil.Vis(by, "Argo_Pin", new Vector3((float)target.NetworkObjectId, seconds, stun ? 1f : 0f), seconds);
        return seconds;
    }

    // A building (any team's tower, Argo's or not) within StunReach of the foe.
    public static bool Beside(Entity_Master foe)
    {
        Vector3 at = ModKitUtil.Pos(foe);
        foreach (Entity_Master b in ModKitUtil.Area(at, ArgoData.StunReach + 0.5f, ModKitUtil.Tagged("Tower")))
            if (b != null && b != foe && ModKitUtil.Alive(b) && ModKitUtil.Flat(ModKitUtil.Pos(b), at) <= ArgoData.StunReach)
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
        if (host == null || foe == null || !ModKitUtil.Alive(foe) || Reeling(foe))
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
        ArgoServer.Reeled(foe, ModKitUtil.Pos(foe), to.Value);
        return to;
    }

    static Vector3? ReelTile(Entity_Master foe, Vector3 toward, float tiles, float minDist)
    {
        Link_Being being = foe.Beingserver;
        if (being == null || being.Currentblock == null || foe.Commandlistserver == null || !foe.Commandlistserver.ContainsKey("Move")
            || ModKitUtil.MaxHealth(foe) >= ArgoData.BossHealth)
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
                if (block == null) block = ModKitUtil.Surface(new Vector3(Mathf.Round(p.x), Mathf.Round(p.y) + dy, Mathf.Round(p.z)));
            if (block == null || block == being.Currentblock || ModKitUtil.Flat(block.Position, toward) < minDist)
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
        while (foe != null && ModKitUtil.Alive(foe) && Time.time < end)
        {
            Vector3 at = ModKitUtil.Pos(foe);
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
        ModKitUtil.Unmod(boost);
        if (foe != null)
            reeling.Remove(foe);
        if (foe != null && ModKitUtil.Alive(foe) && onDone != null)
        {
            try { onDone(foe); }
            catch (Exception e) { ArgoLog.Warn("reel: " + e.Message); }
        }
    }

    // Slows on their own never take a foe under 10% of its speed: the modifier is cut to what is left above that, and
    // skipped on a foe already that slow or held (the slow look still plays).
    public static void Slow(Entity_Master by, Entity_Master target, float amount, float seconds, bool vfx)
    {
        if (target == null || !ModKitUtil.Alive(target) || amount <= 0f)
            return;
        Link_Stats s = ModKitUtil.Speed(target);
        if (s == null)
            return;
        ArgoStatus.Slowed(target, seconds);
        float cut = Mathf.Min(Mathf.Clamp01(amount), s.Value_Multi - 0.1f);
        if (cut >= 0.01f)
            Helper_Entitytools.Addmod(target, "Speed", -cut, Helper_Entitytools.Modtypes.Multi, seconds);
        if (vfx)
            ModKitUtil.Debuff(by, target, "Debuff_Slow", seconds);
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
