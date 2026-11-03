// Kit 1.7 (2026-11-03). Versions and compatibility rules: TDS_Vessel_Creator\kit\README.md
// Shared kit: a number shown on an entity on every client (docs/01 section 6), the game's own counter route: a stat
// with Style = Counter (like the Archer Tower's kills) added by Helper_Entitytools.Addstat and driven by one Base
// modifier. Set it on the host; the engine syncs the stat. Argo's Fathom, Pure Aura, Thread, a Growth stage.
public static class ModKitCounter
{
    static readonly Dictionary<Entity_Master, Dictionary<string, Link_Modifier>> mods = new Dictionary<Entity_Master, Dictionary<string, Link_Modifier>>();

    public static void Set(Entity_Master master, string name, float value)
    {
        if (master == null || master.Statsserver == null)
            return;
        Dictionary<string, Link_Modifier> own;
        if (!mods.TryGetValue(master, out own))
        {
            if (mods.Count > 64) Sweep();
            mods[master] = own = new Dictionary<string, Link_Modifier>();
        }
        Link_Modifier mod;
        if (!own.TryGetValue(name, out mod) || mod == null || mod.Dead)
        {
            if (!master.Statsserver.ContainsKey(name))
            {
                Link_Stats stat = new Link_Stats();
                stat.Name = name;
                stat.Value_Base = 0f;
                stat.Style = Statproperties.Defaultstatgroup.Styles.Counter;
                stat.Display_Type = Statproperties.Displaytypes.Int;
                stat.Display_Exposed = true;
                Helper_Entitytools.Addstat(master, stat, 0f);
            }
            own[name] = mod = Helper_Entitytools.Addmod(master, name, value, Helper_Entitytools.Modtypes.Base, 0f);
        }
        if (mod == null)
            return;
        mod.Value_Base = value;
        Link_Stats s;
        if (master.Statsserver.TryGetValue(name, out s))
            s.Value = s.Value_Calc;
    }

    // The value every client sees (0 when the stat isn't there).
    public static float Get(Entity_Master master, string name)
    {
        Link_Stats s;
        return master != null && master.Statsserver != null && master.Statsserver.TryGetValue(name, out s) ? s.Value : 0f;
    }

    static void Sweep()
    {
        List<Entity_Master> gone = new List<Entity_Master>();
        foreach (Entity_Master m in mods.Keys)
            if (m == null) gone.Add(m);
        foreach (Entity_Master m in gone)
            mods.Remove(m);
    }
}
