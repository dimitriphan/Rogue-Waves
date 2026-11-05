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
        obj.Id = ModKitUtil.Clip(name.ToLowerInvariant() + "-yossele");
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
        if (made == null || made.Worldobject == null)
            return null;
        // Spawning as the parent's child copies the parent's Skin over spawn.Skin (Helper_Entitytools.Spawn, Sameasparent):
        // a Grafted fruit came out with the Sapling's Growth skin and drew as the ALL fruit (YOS-1). Skin is networked.
        ModKitUtil.Skin(made.Worldobject, skin);
        return made.Worldobject;
    }

    // A hold in place (and, with stun, no attacks) for a while, with a debuff look.
    public static void Root(Entity_Master by, Entity_Master foe, float seconds, bool stun, string vfx)
    {
        if (foe == null || !ModKitUtil.Alive(foe) || seconds <= 0f || ModKitUtil.Speed(foe) == null)
            return;
        Helper_Entitytools.Addmod(foe, "Speed", -1f, Helper_Entitytools.Modtypes.Multi, seconds);
        if (stun && foe.Statsserver.ContainsKey("Attackspeed"))
            Helper_Entitytools.Addmod(foe, "Attackspeed", -0.95f, Helper_Entitytools.Modtypes.Multi, seconds);
        if (!string.IsNullOrEmpty(vfx))
            ModKitUtil.Debuff(by, foe, vfx, seconds);
    }

    // A slow that lasts until taken off (vines, Sticky Spores): never takes a foe under 10% of its speed.
    public static Link_Modifier SlowOn(Entity_Master foe, float amount)
    {
        Link_Stats s = ModKitUtil.Speed(foe);
        if (s == null || amount <= 0f)
            return null;
        float cut = Mathf.Min(Mathf.Clamp01(amount), s.Value_Multi - 0.1f);
        return cut >= 0.01f ? Helper_Entitytools.Addmod(foe, "Speed", -cut, Helper_Entitytools.Modtypes.Multi, 0f) : null;
    }

    // Owner-only message, shown by YosseleClient on the owner's screen.
    public static void Warn(Entity_Master master, int code) { ModKitUtil.Vis(master, "Yossele_Warn", ModKitUtil.Pos(master), code); }

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
