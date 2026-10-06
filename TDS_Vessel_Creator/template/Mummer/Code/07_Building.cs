// Server-side base for every Mummer buildable. Replaces the template's own AIs (which hard-code Smacky's or Buster's
// objects) and Entity_Ai_General_Upgrade, whose upgrade flow it mirrors exactly: the same Upgrade/Upgradeoption
// commands, throttle, costs, Forced/Normal checks, Upgradeborder effect and the owner's Upgraded end-screen stat.
// Upgrade parameters on real stats become modifiers; the rest are read by the subclass through Param().
public abstract class MummerBuilding : MonoBehaviour
{
    public Entity_Master Master;
    public MummerData.Build Def;
    public bool Started;

    readonly Dictionary<string, float> param = new Dictionary<string, float>();
    readonly List<string> keys = new List<string>();   // no HashSet: System.Core isn't referenced by the game's compiler
    Link_Command upgradeCommand;
    float upgradeThrottle;

    static readonly string[] StatNames = { "Damage", "Attackspeed", "Range", "Health", "Duration", "Slow", "Units", "Respawn", "Leash", "Scraprange" };

    public bool Has(string key) { return keys.Contains(key); }

    public float Param(string name, float fallback)
    {
        float v;
        return param.TryGetValue(name, out v) ? v : fallback;
    }

    public float Stat(string name, float fallback) { return MummerUtil.Stat(Master, name, fallback); }
    public long Owner { get { return MummerUtil.Owner(Master); } }
    public Vector3 Pos { get { return MummerUtil.Pos(Master); } }

    void Update()
    {
        if (Master == null)
        {
            Destroy(this);
            return;
        }
        if (!MummerUtil.IsServer)
            return;
        try
        {
            if (!Started)
            {
                if (Master.Statsserver == null || Master.Statsserver.Count == 0 || Master.Commandlistserver == null || Master.Radio == null)
                    return;
                Begin();
            }
            Tick();
        }
        catch (Exception e)
        {
            MummerLog.Warn(Def.Id + " brain: " + e.Message);
            enabled = false;
        }
    }

    void Begin()
    {
        Started = true;
        upgradeCommand = Helper_Entitytools.Createcommand(Master, "Command_Upgrade", "Upgrade", false, true);
        for (int i = 1; i <= 4; i++)
        {
            Link_Command option = Helper_Entitytools.Createcommand(Master, "Command_Upgradeoption" + i, "Upgradeoption" + i, false, true);
            option.Goalvalue = i - 1;
            Helper_Entitytools.Updatecommand(Master, "Upgradeoption" + i, "", true, true);
        }
        upgradeCommand.Goalvalue = 1f;
        Helper_Entitytools.Updatecommand(Master, "Upgrade", "", true, true);
        Master.Command += OnCommand;
        Master.Upgrade += OnUpgrade;
        foreach (Upgradeproperties u in new List<Upgradeproperties>(Master.Upgradelistserver.Values))
            OnUpgrade(u);
        Started2();
        MummerUtil.Vis(Master, "Mummer_Built", Pos, 0);
    }

    protected virtual void Started2() { }
    protected abstract void Tick();
    public virtual void OnTide(int tide) { }
    protected virtual void Command(string command, string json, bool server) { }
    protected virtual void Upgraded(MummerData.Upg def) { }

    void OnCommand(string command, string json, bool server)
    {
        if (command != "Upgrade")
        {
            try { Command(command, json, server); }
            catch (Exception e) { MummerLog.Warn(Def.Id + " command " + command + ": " + e.Message); }
            return;
        }
        if (string.IsNullOrEmpty(json))
            return;
        try
        {
            // {"Upgrade":"<name>","Upgrader":<session>} (Entity_Ai_General_Upgrade.Upgradedata), read by hand because
            // JsonUtility isn't available to mod code.
            string upgradeName = MummerUtil.JsonString(json, "Upgrade");
            long upgrader = (long)MummerUtil.JsonNumber(json, "Upgrader");
            Upgradeproperties up = Bl_Address.Getload("Upgrade", upgradeName) as Upgradeproperties;
            if (up == null)
                return;
            if (!server)
            {
                List<Upgradeproperties> available = new List<Upgradeproperties>(Master.Beingserver.Entity.Upgrades);
                foreach (Upgradeproperties applied in Master.Upgradelistserver.Values)
                    if (applied.Priority == Master.Entitydata.Value.Level)
                        available.AddRange(applied.Upgrades);
                if (!available.Contains(up) || Time.time - upgradeThrottle <= 0.05f)
                    return;
                upgradeThrottle = Time.time;
            }
            float scale = upgradeCommand != null ? upgradeCommand.Goalvalue : 1f;
            if (!Helper_Resources.Checkresources(up.Costs, upgrader, scale))
                return;
            Helper_Resources.Useresources(up.Costs, upgrader, scale);
            Helper_Entitytools.Upgrade(Master, up, server ? Helper_Entitytools.Upgradecheck.Forced : Helper_Entitytools.Upgradecheck.Normal, null);
            Master.Radio.Vfxoneshotcheck("Upgradeborder", Pos, Vector3.forward);
            GameObject player;
            Tds_Clientinfo info;
            if (Blm_Server.Instance != null && Blm_Server.Instance.Sessionids.TryGetValue(Owner, out player)
                && player != null && player.TryGetComponent(out info) && info.Activeplayerdata != null)
                info.Activeplayerdata.Upgraded++;
        }
        catch (Exception e)
        {
            MummerLog.Warn("upgrade command: " + e.Message);
        }
    }

    void OnUpgrade(Upgradeproperties up)
    {
        MummerData.Upg def;
        if (up == null || !MummerContent.UpgradeDefs.TryGetValue(up.Objectname, out def) || keys.Contains(def.Key))
            return;
        keys.Add(def.Key);
        if (up.Defaultparams != null)
            foreach (KeyValuePair<string, float> kv in up.Defaultparams)
            {
                int cut = kv.Key.LastIndexOf('_');
                string stat = cut > 0 ? kv.Key.Substring(0, cut) : kv.Key;
                string type = cut > 0 ? kv.Key.Substring(cut + 1) : "";
                // Stats this building owns take the modifier; Understudy stats (Health/Damage/Speed on a Stage) are
                // kept as params and applied to each Understudy instead.
                bool own = Array.IndexOf(StatNames, stat) >= 0 && Master.Statsserver.ContainsKey(stat);
                if (own && (type == "Add" || type == "Multi" || type == "Pmulti"))
                {
                    Helper_Entitytools.Modtypes mt = type == "Add" ? Helper_Entitytools.Modtypes.Add : type == "Multi" ? Helper_Entitytools.Modtypes.Multi : Helper_Entitytools.Modtypes.Pmulti;
                    Helper_Entitytools.Addmod(Master, stat, kv.Value, mt, 0f);
                }
                else
                {
                    float old;
                    param[kv.Key] = param.TryGetValue(kv.Key, out old) && !kv.Key.EndsWith("_Base") && !kv.Key.EndsWith("_Max") ? old + kv.Value : kv.Value;
                }
            }
        if (Started)
        {
            MummerUtil.Vis(Master, "Mummer_Upgrade", Pos, def.Tier);
            Upgraded(def);
        }
    }

    // Foes near this building, nearest first.
    protected List<Entity_Master> Foes(float range) { return MummerUtil.FoesNear(Master, Pos, range); }
}

// Loom: spins Scraps from foes felled nearby into Thread (MummerServer.FoeFell), and picks at ore in reach for aura.
public class MummerLoomBrain : MummerBuilding
{
    public int Scrapped, Strikes;
    float lastStrike;

    public float Scraprange { get { return Stat("Scraprange", 3.5f); } }

    protected override void Tick()
    {
        float aspd = Stat("Attackspeed", 0.4f);
        if (aspd <= 0f || Time.time - lastStrike < 1f / aspd)
            return;
        lastStrike = Time.time;
        Entity_Master ore = Helper_Entityquerytools.Entitynearest(Pos, Stat("Range", 2.5f), MummerUtil.Tagged("Resource"));
        if (ore == null || ore.Statsserver == null || !ore.Statsserver.ContainsKey("Healthresource"))
            return;
        // Ore takes min(Damage, 1) + Damageresource per strike (Helper_Damagetools.Dodamage) and pays out when emptied.
        Dictionary<string, float> d = new Dictionary<string, float> { { "Damage", 1f }, { "Damagemulti", 1f }, { "Damageresource", Mathf.Max(0f, Stat("Damage", 1f) - 1f) } };
        Helper_Entitytools.Interact(Master, ore, "Hurt", d);
        Strikes++;
        Master.Radio.Vis_ClientRpc("Mummer_Spin", MummerUtil.Pos(ore), 0f);
        if (Has("Orethread") && Strikes % 3 == 0)
        {
            MummerServer.AddThread(Owner, (int)Param("Orethread_Base", 1f));
            MummerUtil.Vis(Master, "Mummer_Scrap", MummerUtil.Pos(ore), 1);
        }
    }

    // Patchwork: a free Understudy at the nearest Stage each Tide.
    public override void OnTide(int tide)
    {
        if (!Has("Patchwork"))
            return;
        MummerStageBrain stage = MummerServer.NearestStage(Owner, Pos, 8f);
        if (stage != null)
        {
            stage.FreeStitches += (int)Param("Units_Add", 1f);
            MummerUtil.Vis(Master, "Mummer_Patchwork", MummerUtil.Pos(stage.Master), 0);
        }
    }
}
