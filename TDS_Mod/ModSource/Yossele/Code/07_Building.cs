// Server-side base for every Yossele buildable. Replaces the template's own AIs (which hard-code Smacky's objects). The
// game's own Entity_Ai_General_Upgrade sits on every buildable (YosseleContent.BuildTower) and runs the upgrade panel:
// the Upgrade/Upgradeoption commands, throttle, costs, Upgradeborder effect, the owner's Upgraded end-screen stat, and
// Voice in a Bottle's price cut. This brain hears each upgrade through Master.Upgrade: parameters on the building's own
// stats become modifiers; the rest are read by the subclass through Param() and Has().
//
// Growth (the scope's rule): a building gains a stage when a Tide begins if it stood through the whole Tide before it
// (5 stages on a map of 4 Tides or fewer), up to its maximum; FERTILIZE adds one at once. The stage is shown as the "Growth" counter (ModKitCounter) and the
// skin "Growth<n>" (sprouts round its foot), both synced by the engine. Destroyed or sold, it is gone: a new one
// starts at 0.
public abstract class YosseleBuilding : MonoBehaviour
{
    public Entity_Master Master;
    public YosseleData.Build Def;
    public bool Started;
    public int Growth, BuiltWave = -1, Grown, Fertilized;

    readonly Dictionary<string, float> param = new Dictionary<string, float>();
    readonly List<string> keys = new List<string>();   // no HashSet: System.Core isn't referenced by the game's compiler
    readonly List<Link_Modifier> upgradeMods = new List<Link_Modifier>();
    readonly YosseleFaults faults = new YosseleFaults();

    // Stats the buildings own: an upgrade param named after one of these (Range_Add, Seedlimit_Add...) is a modifier.
    static readonly string[] StatNames = { "Damage", "Attackspeed", "Range", "Health", "Radius", "Spores", "Seedlimit", "Seedrate", "Slow" };

    public bool Has(string key) { return keys.Contains(key); }
    public List<string> Keys { get { return keys; } }

    public float Param(string name, float fallback)
    {
        float v;
        return param.TryGetValue(name, out v) ? v : fallback;
    }

    public float Stat(string name, float fallback) { return YosseleUtil.Stat(Master, name, fallback); }
    public long Owner { get { return YosseleUtil.Owner(Master); } }
    public int Team { get { return YosseleUtil.Team(Master); } }
    public Vector3 Pos { get { return YosseleUtil.Pos(Master); } }
    public int Max { get { return Def != null ? Def.Max : 0; } }
    public bool FullGrown { get { return Growth >= Max; } }

    void Update()
    {
        if (Master == null)
        {
            Destroy(this);
            return;
        }
        if (!YosseleUtil.IsServer)
            return;
        try
        {
            if (!Started)
            {
                if (Master.Statsserver == null || Master.Statsserver.Count == 0 || Master.Commandlistserver == null || Master.Radio == null)
                    return;
                Begin();
            }
            if (YosseleUtil.Alive(Master))
                Tick();
        }
        catch (Exception e)
        {
            if (faults.Fail(Def.Id + " brain", e))
                enabled = false;
        }
    }

    void Begin()
    {
        Started = true;
        Master.Command += OnCommand;
        Master.Upgrade += OnUpgrade;
        BuiltWave = ModKitTides.Wave >= 0 ? ModKitTides.Wave : (Game_Mapvars.Instance != null ? Game_Mapvars.Instance.Wave.Value : 0);
        SetGrowth(0);
        foreach (Upgradeproperties u in new List<Upgradeproperties>(Master.Upgradelistserver.Values))
            OnUpgrade(u);
        Started2();
        YosseleUtil.Vis(Master, "Yossele_Built", Pos, 0);
    }

    protected virtual void Started2() { }
    protected abstract void Tick();
    protected virtual void OnTide(int wave) { }
    protected virtual void Command(string command, string json, bool server) { }
    protected virtual void Upgraded(YosseleData.Upg def) { }
    protected virtual void OnGrowth() { }
    public virtual void Gone(bool killed) { }

    // ---------------------------------------------------------------- Growth

    // A Tide began (YosseleServer, from ModKitTides). Built during Tide N (or the break after it) it first grows when
    // Tide N+2 begins: it has then stood through all of Tide N+1. 'stages': 1, or 5 on a map with long Tides.
    public void TideBegan(int wave, int stages)
    {
        if (!Started)
            return;
        if (Growth < Max && BuiltWave <= wave - 2)
        {
            Grow(stages, false);
            Grown++;
        }
        OnTide(wave);
    }

    public bool Grow(int stages, bool fertilizer)
    {
        if (!Started || Growth >= Max || stages <= 0)
            return false;
        SetGrowth(Mathf.Min(Max, Growth + stages));
        if (fertilizer)
            Fertilized++;
        YosseleUtil.Vis(Master, "Yossele_Grow", Pos, Growth + (fertilizer ? 100 : 0));
        return true;
    }

    public void SetGrowth(int stages)
    {
        Growth = Mathf.Clamp(stages, 0, Max);
        ModKitCounter.Set(Master, YosseleData.GrowthStat, Growth);
        YosseleUtil.Skin(Master, Growth > 0 ? YosseleData.GrowthStat + Growth : "");
        OnGrowth();
    }

    // ---------------------------------------------------------------- commands and upgrades

    // "Upgrade" belongs to the stock Entity_Ai_General_Upgrade; every other command goes to the subclass.
    void OnCommand(string command, string json, bool server)
    {
        if (command == "Upgrade")
            return;
        try { Command(command, json, server); }
        catch (Exception e) { YosseleLog.Warn(Def.Id + " command " + command + ": " + e.Message); }
    }

    void OnUpgrade(Upgradeproperties up)
    {
        YosseleData.Upg def = Apply(up, 1f);
        if (def != null && Started)
        {
            YosseleUtil.Vis(Master, "Yossele_Upgrade", Pos, def.Tier);
            Upgraded(def);
        }
    }

    // One upgrade's params: those on the building's own stats become modifiers, the rest are read through Param(). A
    // share under 1 is the Bonsai copying its Tree (YosseleBonsaiBrain): effects are scaled, YosseleData.BonsaiKept
    // and penalties are not, counts are rounded and never under 1.
    protected YosseleData.Upg Apply(Upgradeproperties up, float share)
    {
        YosseleData.Upg def;
        if (up == null || !YosseleContent.UpgradeDefs.TryGetValue(up.Objectname, out def) || keys.Contains(def.Key))
            return null;
        keys.Add(def.Key);
        if (up.Defaultparams != null)
            foreach (KeyValuePair<string, float> kv in up.Defaultparams)
            {
                float value = kv.Value;
                if (share < 1f && value > 0f && Array.IndexOf(YosseleData.BonsaiKept, kv.Key) < 0)
                    value = Array.IndexOf(YosseleData.BonsaiCounts, kv.Key) >= 0 ? Mathf.Max(1f, Mathf.Round(value * share)) : value * share;
                int cut = kv.Key.LastIndexOf('_');
                string stat = cut > 0 ? kv.Key.Substring(0, cut) : kv.Key;
                string type = cut > 0 ? kv.Key.Substring(cut + 1) : "";
                bool own = Array.IndexOf(StatNames, stat) >= 0 && Master.Statsserver.ContainsKey(stat);
                if (own && (type == "Add" || type == "Multi" || type == "Pmulti"))
                {
                    Helper_Entitytools.Modtypes mt = type == "Add" ? Helper_Entitytools.Modtypes.Add : type == "Multi" ? Helper_Entitytools.Modtypes.Multi : Helper_Entitytools.Modtypes.Pmulti;
                    upgradeMods.Add(Helper_Entitytools.Addmod(Master, stat, value, mt, 0f));
                }
                else
                {
                    float old;
                    param[kv.Key] = param.TryGetValue(kv.Key, out old) && !kv.Key.EndsWith("_Base") && !kv.Key.EndsWith("_Max") ? old + value : value;
                }
            }
        return def;
    }

    // Drops every upgrade's params and modifiers (the Bonsai, before it copies a different Tree).
    protected void ClearUpgrades()
    {
        foreach (Link_Modifier m in upgradeMods)
            YosseleUtil.Unmod(m);
        upgradeMods.Clear();
        keys.Clear();
        param.Clear();
    }

    // Foes near this building, nearest first.
    protected List<Entity_Master> Foes(float range) { return YosseleUtil.FoesNear(Master, Pos, range); }
}
