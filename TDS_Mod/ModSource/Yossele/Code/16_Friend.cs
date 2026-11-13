// Cordyfriends (Cordyceps, the Mushroom Cannon's tier 5, host only): a foe that falls with spores may rise as one, a
// networked melee unit owned by the cannon's player, spawned as the cannon's child on the felled foe's tile (the template's
// Understudies, docs/05 section 10). Fighting is the stock Entity_Ai_Mob_Combat; YosseleFriendBrain only decides where
// it walks: to the nearest foe within 4 tiles that is within 5 of where it rose, else back there. It stands on the
// Snoble's mob-blocker block, so foes stop at it ("stalling foes"). It stays when its cannon goes. At most 30 stand at
// once per player; none respawn.
public static class YosseleFriends
{
    static readonly List<YosseleFriendBrain> all = new List<YosseleFriendBrain>();
    static YosseleCannonBrain pending;
    static Vector3 pendingAt;
    public static int Raised, Fell, Capped, Failed;

    public static void Reset() { all.Clear(); }

    public static void Update() { all.RemoveAll(f => f == null || f.Master == null); }

    public static List<YosseleFriendBrain> All { get { all.RemoveAll(f => f == null || f.Master == null); return new List<YosseleFriendBrain>(all); } }

    public static int CountOf(long owner)
    {
        int n = 0;
        foreach (YosseleFriendBrain f in all)
            if (f != null && f.Master != null && ModKitUtil.Alive(f.Master) && ModKitUtil.Owner(f.Master) == owner)
                n++;
        return n;
    }

    // A spored foe fell where 'at' is: raise a Cordyfriend there a moment later (the foe's despawn is still running).
    public static bool Raise(YosseleCannonBrain cannon, Vector3 at)
    {
        if (cannon == null || cannon.Master == null || !ModKitUtil.Alive(cannon.Master))
            return false;
        if (CountOf(cannon.Owner) >= YosseleData.FriendCap)
        {
            Capped++;
            return false;
        }
        Beingobject being;
        Genitem tile = ModKitUtil.Tile(at);
        if (!YosseleContent.Beings.TryGetValue(YosseleData.Friend, out being) || tile == null)
        {
            Failed++;
            return false;
        }
        YosseleUtil.Later(YosseleMod.Instance, 0.1f, () => Spawn(cannon, being, tile, at));
        return true;
    }

    static void Spawn(YosseleCannonBrain cannon, Beingobject being, Genitem tile, Vector3 at)
    {
        if (cannon == null || cannon.Master == null || cannon.Master.Beingserver == null || !ModKitUtil.Alive(cannon.Master))
            return;
        // Again here: a burst of deaths passes Raise's check once each before any of them has risen (YOS-3).
        if (CountOf(cannon.Owner) >= YosseleData.FriendCap)
        {
            Capped++;
            return;
        }
        Link_Spawn spawn = Helper_Entitytools.Createlinkspawnparent(cannon.Master.Beingserver);
        spawn.Genadd = tile;
        spawn.Spawnstyle = Link_Spawn.Spawnstyles.Forced;
        spawn.Being = being;
        spawn.Sellvalue = 0f;
        pending = cannon;
        pendingAt = at;
        Link_Being made = null;
        try { made = Helper_Entitytools.Spawn(spawn, at, cannon.Master.Beingserver, true, null); }
        finally { pending = null; }
        if (made == null || made.Worldobject == null)
        {
            Failed++;
            return;
        }
        Raised++;
        ModKitUtil.Vis(cannon.Master, "Yossele_Rise", at, 0f);
    }

    // The spawn hook (YosseleServer.OnSpawn): its brain, and where it rose.
    public static void Spawned(Entity_Master master)
    {
        YosseleFriendBrain brain = master.gameObject.AddComponent<YosseleFriendBrain>();
        brain.Master = master;
        brain.Cannon = pending;
        if (pending != null)
        {
            brain.Home = pendingAt;
            brain.HasHome = true;
        }
        all.Add(brain);
    }

    public static void Gone(YosseleFriendBrain friend, bool killed)
    {
        all.Remove(friend);
        if (killed)
        {
            Fell++;
            ModKitUtil.Vis(friend.Master, "Yossele_Friendfell", ModKitUtil.Pos(friend.Master), 0f);
        }
    }
}

public class YosseleFriendBrain : MonoBehaviour
{
    public Entity_Master Master;
    public YosseleCannonBrain Cannon;
    public Vector3 Home;
    public bool HasHome, Started;
    public Entity_Master Target;
    public int Orders;
    float nextThink;
    readonly ModKitFaults faults = new ModKitFaults(YosseleLog.Tag);

    void Update()
    {
        if (Master == null)
        {
            Destroy(this);
            return;
        }
        if (!ModKitUtil.IsServer)
            return;
        try
        {
            if (!Started)
            {
                if (Master.Statsserver == null || Master.Statsserver.Count == 0 || Master.Commandlistserver == null || !Master.Commandlistserver.ContainsKey("Move"))
                    return;
                Started = true;
                if (!HasHome)
                {
                    Home = ModKitUtil.Pos(Master);
                    HasHome = true;
                }
            }
            if (Time.time >= nextThink)
            {
                nextThink = Time.time + 0.4f;
                Think();
            }
        }
        catch (Exception e)
        {
            if (faults.Fail("Cordyfriend brain", e))
                enabled = false;
        }
    }

    // The template's PuppetBrain melee walk: the nearest foe within reach that is within the leash of home, else home.
    void Think()
    {
        if (!ModKitUtil.Alive(Master))
            return;
        Vector3 pos = ModKitUtil.Pos(Master);
        float range = ModKitUtil.Stat(Master, "Range", 1.5f);
        List<Entity_Master> foes = ModKitUtil.FoesNear(Master, pos, Mathf.Max(YosseleData.FriendEngage, range + 0.5f));
        foes.RemoveAll(f => ModKitUtil.Flat(ModKitUtil.Pos(f), Home) > YosseleData.FriendLeash + 0.5f);
        Entity_Master target = foes.Count > 0 ? foes[0] : null;
        string state = Master.State.Value.ToString();
        float fromHome = ModKitUtil.Flat(pos, Home);
        if (fromHome > YosseleData.FriendLeash + 1f)
            target = null;   // strayed too far: home first
        if (target != null)
        {
            float d = Vector3.Distance(ModKitUtil.Pos(target), pos);
            if (d > range - 0.1f && (Target != target || state != "Move"))
                MoveTo(target, Vector3.zero);
            Target = target;
            return;
        }
        Target = null;
        if (fromHome > 1.2f && (state != "Move" && state != "Attack" || fromHome > YosseleData.FriendLeash + 1f))
            MoveTo(null, Home);
    }

    void MoveTo(Entity_Master target, Vector3 position)
    {
        Link_Command move = Master.Commandlistserver["Move"];
        move.Goalobject = target;
        move.Goalposition = target != null ? Vector3.one * -1f : position;
        move.Goalvalue = 0f;
        move.Goalstring = "Idle";   // what to do on arrival (the Mining Hut sends its miners the same way)
        Helper_Entitytools.Updatecommand(Master, "Move", "", true, true);
        Orders++;
    }
}
