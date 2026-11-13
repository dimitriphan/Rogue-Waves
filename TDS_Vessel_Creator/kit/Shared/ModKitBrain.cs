// Kit 1.16 (2026-11-13). Versions and compatibility rules: TDS_Vessel_Creator\kit\README.md
// Shared kit: the server-side brain of a buildable, its start-up written once. Yossele's, Spool's and Uloe's building
// bases and Argo's tower brain each had their own copy of this, and the copies had drifted (two stopped a brain whose
// start-up threw, two let it tick half set up). ModKitBrainRules.cs holds the engine-free half.

// A brain's error count (pitfall #80). A brain's catch used to switch it off for the map on its first exception, so one
// bad tick could leave a vessel, a building or a unit dead; now the tick is skipped and the brain carries on, and only a
// brain that throws on every frame for 600 frames (ModKitStreak) stops. The first three errors are logged in full, then
// one line every 500. A kind of brain that comes in numbers (Spool's Crafted, Uloe's Photons) passes one shared Log, so
// a fault common to them logs three traces in all; the stop rule stays each brain's own. 'tag' is the part's log tag.
public sealed class ModKitFaults
{
    public sealed class Log { public int Count; }

    readonly ModKitStreak streak = new ModKitStreak();
    readonly Log log;
    readonly string tag;

    public ModKitFaults(string tag) : this(tag, null) { }
    public ModKitFaults(string tag, Log shared) { this.tag = tag; log = shared ?? new Log(); }

    // True when the brain should stop.
    public bool Fail(string who, Exception e)
    {
        int count = ++log.Count;
        bool stop = streak.Hit(Time.frameCount);
        if (count <= 3) Debug.LogWarning(tag + who + ": " + e);
        else if (count % 500 == 0) Debug.LogWarning(tag + who + ": " + count + " errors, last: " + e.Message);
        if (!stop) return false;
        Debug.LogError(tag + who + ": threw every frame for " + ModKitStreak.Limit + " frames, stopped");
        return true;
    }
}

// The start-up, in this order, on the host, once the entity has its stats, commands and radio:
//   1. Started is set (unless StartedAfterUpgrades)   2. Setup()   3. Replay() for each upgrade the entity came with
//   4. Started is set   5. Started2()   6. BuiltLook() goes out BuiltLookDelay later (pitfall #158)
// then Frame() every frame. A start-up that threw stops the brain: it is half set up. A Frame() that threw is skipped.
public abstract class ModKitBrain : MonoBehaviour
{
    public Entity_Master Master;
    public bool Started;
    bool begun;                 // the start-up ran to its end
    float lookAt = -1f;         // when the build look goes out
    ModKitFaults faults;

    protected abstract string LogTag { get; }
    protected abstract string Who { get; }          // "<id> brain", for the log
    // False: an upgrade the entity came with is heard like a buy (Started is already set). True: it is heard before
    // Started, so the vessel's "if (Started)" keeps the buy's sound and Upgraded() from it (Spool's Thread Head).
    protected virtual bool StartedAfterUpgrades { get { return false; } }
    protected virtual bool Resting { get { return false; } }     // true: this frame does nothing at all
    protected abstract float BuiltLookDelay { get; }
    protected virtual void Setup() { }
    protected abstract void Replay(Upgradeproperties up);
    protected virtual void Started2() { }
    protected virtual void BuiltLook() { }
    protected abstract void Frame();

    static bool Spawned(Entity_Master master)
    {
        return master.Statsserver != null && master.Statsserver.Count > 0 && master.Commandlistserver != null && master.Radio != null;
    }

    void Update()
    {
        if (Master == null)
        {
            Destroy(this);
            return;
        }
        if (!ModKitUtil.IsServer || Resting)
            return;
        try
        {
            if (!begun)
            {
                if (!Spawned(Master))
                    return;
                Begin();
                begun = true;
            }
            // Pitfall #158: a look sent in the start-up, a frame after the spawn, lands before a client's 0.25 s hook
            // scan has hooked this thing, so the build look goes out half a second later.
            if (lookAt >= 0f && Time.time >= lookAt)
            {
                lookAt = -1f;
                BuiltLook();
            }
            Frame();
        }
        catch (Exception e)
        {
            if (faults == null)
                faults = new ModKitFaults(LogTag);
            if (faults.Fail(Who, e) || !begun)
                enabled = false;
        }
    }

    void Begin()
    {
        if (!StartedAfterUpgrades)
            Started = true;
        Setup();
        foreach (Upgradeproperties u in new List<Upgradeproperties>(Master.Upgradelistserver.Values))
            Replay(u);
        Started = true;
        Started2();
        lookAt = Time.time + BuiltLookDelay;
    }

    // One number of an upgrade (Upgradeproperties.Defaultparams): on a stat this entity has ('own' null: any of its
    // stats; else only those named) with a modifier's type it becomes that modifier, noted in 'mods' when given;
    // anything else is kept in 'into' for the brain to read.
    protected void ReadParam(ModKitUpgrades into, string name, float value, string[] own, List<Link_Modifier> mods)
    {
        string stat, type;
        bool mod = ModKitUpgrades.Split(name, out stat, out type);
        if (mod && (own == null || Array.IndexOf(own, stat) >= 0) && Master.Statsserver.ContainsKey(stat))
        {
            Helper_Entitytools.Modtypes mt = type == "Add" ? Helper_Entitytools.Modtypes.Add : type == "Multi" ? Helper_Entitytools.Modtypes.Multi : Helper_Entitytools.Modtypes.Pmulti;
            Link_Modifier made = Helper_Entitytools.Addmod(Master, stat, value, mt, 0f);
            if (mods != null)
                mods.Add(made);
        }
        else
            into.Keep(name, value);
    }
}
