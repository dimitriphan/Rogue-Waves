// Kit 1.6 (2026-10-08). Versions and compatibility rules: TDS_Vessel_Creator\kit\README.md
// Shared kit: Directions as data (docs/PACK.md section 3). A Direction is an Upgradeproperties named
// "Direction_<Vessel>_<Name>_Upgradeobject" (or "Direction_Any_<Name>_Upgradeobject"). The part that owns the mechanic
// registers it with its own load group, so it exists on every machine with the pack; the mode lists them all by the
// "Direction_" prefix, offers them, and grants the pick on the core (Helper_Entitytools.Upgrade, Forced), where a
// vessel respawn can't drop it (docs/15 section 9). The upgrade is game state, synced to every client by the engine.
// A vessel asks Has(vessel, name) and reacts. Open (mode phase): the game's "Core" is the team's, so a Direction granted
// there is team-wide; per-player Directions would need the owner in the name.
public static class ModKitDirections
{
    public const string Prefix = "Direction_";

    public static string Id(string vessel, string name)
    {
        return Prefix + (string.IsNullOrEmpty(vessel) ? "Any" : vessel) + "_" + name + "_Upgradeobject";
    }

    // Every Direction any part registered (the mode's offer list).
    public static List<Upgradeproperties> All()
    {
        List<Upgradeproperties> found = new List<Upgradeproperties>();
        List<Blockage.Address.Bl_Address.Loadlist> lists;
        if (!Blockage.Address.Bl_Address.Groups.TryGetValue("Upgrade", out lists))
            return found;
        foreach (Blockage.Address.Bl_Address.Loadlist l in lists)
            foreach (UnityEngine.Object o in l.Items.Values)
            {
                Upgradeproperties u = o as Upgradeproperties;
                if (u != null && u.Objectname.StartsWith(Prefix) && !found.Contains(u))
                    found.Add(u);
            }
        return found;
    }

    // Makes a Direction from any upgrade as a template (no params, no costs, no model change); the caller registers it
    // in its own "Upgrade" list and fills Defaultparams with what its vessel reads.
    public static Upgradeproperties Make(Upgradeproperties template, string vessel, string name, string display, string desc)
    {
        Upgradeproperties d = UnityEngine.Object.Instantiate(template);
        string id = Id(vessel, name);
        d.name = id;
        d.Objectname = id;
        d.Id = (id.ToLowerInvariant() + "-dir").Length > 60 ? (id.ToLowerInvariant() + "-dir").Substring(0, 60) : id.ToLowerInvariant() + "-dir";
        d.Notes = "";
        d.Display_Name = display;
        d.Display_Desc = desc;
        d.Priority = -3;
        d.Costs = new List<Link_Resourceset>();
        d.Defaultparam = new List<Upgradeproperties.Param>();
        d.Defaultparams = new Dictionary<string, float>();
        d.Aiscripts = new List<UnityEngine.Object>();
        d.Ent_Visual = null;
        d.Upgrades = new List<Upgradeproperties>();
        return d;
    }

    // Any client or the host: a "Core" entity carries the Direction (the synced Upgradelist).
    public static bool Has(string vessel, string name)
    {
        string id = Id(vessel, name);
        foreach (Entity_Master core in Cores())
            if (core.Upgradelist != null)
                foreach (FixedString64Bytes u in core.Upgradelist)
                    if (u.ToString() == id)
                        return true;
        return false;
    }

    // Host only: grants a Direction on every core (the mode re-grants each held one when a map starts).
    public static bool Grant(Upgradeproperties direction)
    {
        if (direction == null || Game_Entity.Instance == null || !Game_Entity.Instance.IsServer)
            return false;
        bool any = false;
        foreach (Entity_Master core in Cores())
            if (core.Upgradelistserver != null && !core.Upgradelistserver.ContainsKey(direction.Objectname))
            {
                Helper_Entitytools.Upgrade(core, direction, Helper_Entitytools.Upgradecheck.Forced, null);
                any = true;
            }
        return any;
    }

    static List<Entity_Master> Cores()
    {
        List<Entity_Master> cores = new List<Entity_Master>();
        if (Game_Entity.Instance == null || Game_Entity.Instance.Entityclient == null)
            return cores;
        foreach (Entity_Master e in Game_Entity.Instance.Entityclient)
        {
            if (e == null || e.Client == null || e.Client.Beingobject == null || e.Client.Beingobject.Entity == null)
                continue;
            List<string> tags = e.Client.Beingobject.Entity.Tags;
            if (tags != null && tags.Contains("Core"))
                cores.Add(e);
        }
        return cores;
    }
}
