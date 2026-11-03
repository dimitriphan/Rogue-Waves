// Kit 1.7 (2026-11-03). Versions and compatibility rules: TDS_Vessel_Creator\kit\README.md
// Shared kit: Directions as data (docs/PACK.md section 3). A Direction is an Upgradeproperties named
// "Direction_<Vessel>_<Name><rank>_Upgradeobject" (or "Direction_Any_<Name><rank>_Upgradeobject"; rank 1, 2 or 3 is
// the name's last digit, none means rank 1). The part that owns the mechanic registers it with its own load group, so it
// exists on every machine with the pack; the mode lists them all by the "Direction_" prefix, offers them, and grants a
// player's pick on that player's vessel (Grant): a respawn copies the dead vessel's upgrades, and the mode grants again
// when each map starts (docs/15 section 14.1). The upgrade is game state, synced to every client by the engine.
// A vessel part asks Rank(vessel, name) or Has(vessel, name) and reacts; kit 1.7 made these per vessel, not per team.
public static class ModKitDirections
{
    public const string Prefix = "Direction_";

    public static string Id(string vessel, string name)
    {
        return Prefix + (string.IsNullOrEmpty(vessel) ? "Any" : vessel) + "_" + name + "_Upgradeobject";
    }

    // The parts of a Direction's name: false for an upgrade that isn't one.
    public static bool Parse(string id, out string vessel, out string name, out int rank)
    {
        vessel = name = "";
        rank = 1;
        if (id == null || !id.StartsWith(Prefix) || !id.EndsWith("_Upgradeobject"))
            return false;
        string rest = id.Substring(Prefix.Length, id.Length - Prefix.Length - "_Upgradeobject".Length);
        int cut = rest.IndexOf('_');
        if (cut <= 0 || cut >= rest.Length - 1)
            return false;
        vessel = rest.Substring(0, cut);
        name = rest.Substring(cut + 1);
        char last = name[name.Length - 1];
        if (last >= '1' && last <= '3')
        {
            rank = last - '0';
            name = name.Substring(0, name.Length - 1);
        }
        return true;
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

    // Any client or the host: this vessel carries the Direction (the synced Upgradelist; each rank is its own id).
    public static bool Has(Entity_Master vessel, string owner, string name)
    {
        if (vessel == null || vessel.Upgradelist == null)
            return false;
        string id = Id(owner, name);
        foreach (FixedString64Bytes u in vessel.Upgradelist)
            if (u.ToString() == id)
                return true;
        return false;
    }

    // The highest rank of a Direction this vessel carries (0: none). Rank 1 may be named with or without its digit.
    public static int Rank(Entity_Master vessel, string owner, string name)
    {
        for (int r = 3; r >= 1; r--)
            if (Has(vessel, owner, name + r))
                return r;
        return Has(vessel, owner, name) ? 1 : 0;
    }

    // Host only: grants a Direction on one vessel. False when it already holds it (the same upgrade twice does nothing,
    // Helper_Entitytools.cs:470-473, so a rank is its own upgrade beside the ones before it).
    public static bool Grant(Entity_Master vessel, Upgradeproperties direction)
    {
        if (vessel == null || direction == null || Game_Entity.Instance == null || !Game_Entity.Instance.IsServer)
            return false;
        if (vessel.Upgradelistserver == null || vessel.Upgradelistserver.ContainsKey(direction.Objectname))
            return false;
        Helper_Entitytools.Upgrade(vessel, direction, Helper_Entitytools.Upgradecheck.Forced, null);
        return true;
    }
}
