// Server-side brain for the vessel: the build menu (the Yarn Stash first, like Boutman's Bouthouse), carrying Thread,
// PATCH, and selling a walking unit. It plays the part of a vessel's own AI (Entity_Ai_Hero_<Name>). Attacking is the
// stock Entity_Ai_Mob_Combat, exactly like Smacky; movement, building and selling are the engine's generic hero AIs.
public class SpoolHeroBrain : MonoBehaviour
{
    public Entity_Master Master;
    public SpoolPlayer Player;
    public float PatchReady;
    public int Patches, MenuChanges, SoldByFallback;
    public string LastPatch = "";
    bool started, patchOff, fullWarned, stashFullWarned;
    Link_Command patchCommand;
    float nextCollect, nextMenu;

    public long Owner { get { return ModKitUtil.Owner(Master); } }

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
            if (!started)
            {
                if (Master.Statsserver == null || Master.Statsserver.Count == 0 || Master.Commandlistserver == null || Master.Radio == null)
                    return;
                Begin();
            }
            if (patchOff && Time.time >= PatchReady)
                SetPatchButton(false);
            if (Time.time >= nextMenu)
            {
                nextMenu = Time.time + 0.5f;
                CheckMenu();
                CheckDirections();
            }
            if (Time.time >= nextCollect)
            {
                nextCollect = Time.time + 0.2f;
                Collect();
            }
        }
        catch (Exception e)
        {
            SpoolLog.Warn("hero brain: " + e.Message);
            enabled = false;
        }
    }

    void Begin()
    {
        started = true;
        SpoolServer.Heroes.Add(this);
        Player = SpoolServer.Player(Owner);
        Player.Hero = this;
        CheckMenu();
        CheckDirections();
        if (SpoolContent.Patch != null)
        {
            patchCommand = Helper_Entitytools.Createcommand(Master, SpoolData.PatchCommand, SpoolData.PatchLink, false, true);
            Helper_Entitytools.Updatecommand(Master, SpoolData.PatchLink, "", true, true);
        }
        // Subscribed after the Updatecommand calls above, which run each command once.
        Master.Command += OnCommand;
        SpoolServer.Show(Player);
        SpoolLog.Info("Spool landed for player " + Owner);
    }

    // ---------------------------------------------------------------- the build menu

    // Until the Yarn Stash stands the menu holds only the Stash; then the four buildables (Entity_Ai_Hero_Boutman
    // removes and recreates Buildoption1 the same way, and Argo's Miner's Base proved it). The server checks build
    // commands against the vessel's own options, so a removed button can't be built from either.
    // BATCH 08:64 (Limit: 1): the Stash counts from its spawn (SpoolServer.HasStash), and SpoolServer.StashLimit runs this
    // the moment one spawns or goes, as Argo's Miner's Base and Yossele's Tree do; it was a Stash that had started, read
    // every half second, which left a moment for a second Stash order. removeOnly: called inside the build that spawned
    // the Stash, it only takes buttons away (as Argo's Base does there); the four come at the next half-second check.
    public void CheckMenu(bool removeOnly = false)
    {
        // Review: a Stash that has only spawned takes the Stash button away; the four wait for it to start (Player.Home and
        // its Thread set in Started2), as before.
        bool stash = SpoolServer.HasStash(Owner), home = stash && Player.Home != null && Player.Home.Master != null;
        for (int i = 0; i < SpoolData.Buildables.Length; i++)
        {
            string link = "Buildoption" + (i + 1);
            string want = home ? SpoolData.Buildables[i] : i == 0 && !stash ? SpoolData.Stash : null;
            Link_Command have;
            bool shown = Master.Commandlistserver.TryGetValue(link, out have);
            if (shown && (want == null || have == null || have.Goalstring != want))
            {
                Helper_Entitytools.Removecommand(Master, link);
                shown = false;
                MenuChanges++;
            }
            // Quiet lookup: asked every half second, and Getload logs each miss (pitfall #71).
            if (removeOnly || shown || want == null || ModKitUtil.Load<Commandobject>("Command", "Command_" + link) == null)
                continue;
            Link_Command c = Helper_Entitytools.Createcommand(Master, "Command_" + link, link, false, true);
            c.Goalstring = want;
            c.Goalvalue = 1f;
            Helper_Entitytools.Updatecommand(Master, link, "", true, true);
        }
    }

    // The Directions this vessel holds, for what its player's minions and Looms do (kit 1.7).
    void CheckDirections()
    {
        Player.Recycler = SpoolServer.DirectionRank(Master, SpoolData.Recyclers[0].Name);
        Player.Sewing = SpoolServer.DirectionRank(Master, SpoolData.Sewingmachines[0].Name);
        Player.Needle = SpoolServer.DirectionRank(Master, SpoolData.Doubleneedles[0].Name);
        Player.Buttons = SpoolServer.DirectionRank(Master, SpoolData.Buttonjars[0].Name);
        Player.Pattern = SpoolServer.DirectionRank(Master, SpoolData.Patternbooks[0].Name);
        Player.Merit = SpoolServer.DirectionRank(Master, SpoolData.Meritbadges[0].Name);
        Player.Socks = SpoolServer.DirectionRank(Master, SpoolData.Oddsocks[0].Name);
    }

    public string Menu()
    {
        List<string> l = new List<string>();
        for (int i = 1; i <= 4; i++)
        {
            Link_Command c;
            if (Master.Commandlistserver.TryGetValue("Buildoption" + i, out c) && c != null)
                l.Add(c.Goalstring);
        }
        return ModKitUtil.Join(l);
    }

    // ---------------------------------------------------------------- carrying

    // Spool picks up ground Thread within a tile and a Loom's within 1.5, and hands it over at its Yarn Stash.
    void Collect()
    {
        if (!ModKitUtil.Alive(Master))
            return;
        Vector3 pos = ModKitUtil.Pos(Master);
        bool changed = false;
        int room = Player.CarryCap - Player.Carried;
        int took = SpoolServer.Collect(Master, SpoolData.PickHero, room);
        took += SpoolServer.CollectLooms(Owner, Master, SpoolData.LoomPick, room - took);
        if (took > 0)
        {
            Player.Carried += took;
            changed = true;
        }
        if (Player.Carried < Player.CarryCap)
            fullWarned = false;
        else if (!fullWarned && SpoolServer.DropsNear(pos, SpoolData.PickHero) > 0)
        {
            fullWarned = true;
            SpoolUtil.Warn(Master, SpoolData.WarnCarryFull);
        }
        if (Player.Carried > 0 && SpoolServer.AtStash(Owner, pos, SpoolData.DropOff))
        {
            int put = SpoolServer.Deliver(Owner, Master, Player.Carried);
            Player.Carried -= put;
            changed |= put > 0;
            if (put == 0 && !stashFullWarned)
            {
                stashFullWarned = true;
                SpoolUtil.Warn(Master, SpoolData.WarnStashFull);
            }
            if (put > 0)
                stashFullWarned = false;
        }
        if (changed)
            SpoolServer.Show(Player);
    }

    // ---------------------------------------------------------------- commands

    void OnCommand(string command, string json, bool server)
    {
        try
        {
            if (command == SpoolData.PatchLink)
                Patch(Picked(json));
            else if (command == "Sell")
                SellFallback(json);
        }
        catch (Exception e)
        {
            SpoolLog.Warn("command " + command + ": " + e.Message);
        }
    }

    // The stock Sell finds what to sell on the tile the picker clicked; a unit that walked off it is missed. The stock
    // handler has run by now (it subscribed first): if it found nothing, the nearest of this player's sellable Spool
    // units of that name within a tile and a half is handed to the stock Sell action, which runs next.
    void SellFallback(string json)
    {
        Link_Command sell;
        Vector3 at;
        if (string.IsNullOrEmpty(json) || !Master.Commandlistserver.TryGetValue("Sell", out sell) || sell.Goalobject != null || !ModKitUtil.JsonPosition(json, out at))
            return;
        string name = ModKitUtil.JsonString(json, "Tower");
        Entity_Master best = null;
        float bestD = 1.5f;
        foreach (SpoolUnitBrain u in SpoolServer.Units)
            if (u != null && u.Master != null && u.Def.Sellable && u.Owner == Owner && (u.Master.name == name || u.Def.Id == name))
                Closer(u.Master, at, ref best, ref bestD);
        foreach (SpoolHeadBrain h in SpoolServer.Owned<SpoolHeadBrain>(Owner))
            if (h.Master.name == name || h.Def.Id == name)
                Closer(h.Master, at, ref best, ref bestD);
        if (best == null || best.Beingserver == null || best.Beingserver.Currentblock == null)
            return;
        sell.Goalobject = best;
        sell.Goalvalue = Owner;
        SoldByFallback++;
    }

    static void Closer(Entity_Master m, Vector3 at, ref Entity_Master best, ref float bestD)
    {
        float d = ModKitUtil.Flat(ModKitUtil.Pos(m), at);
        if (d <= bestD)
        {
            best = m;
            bestD = d;
        }
    }

    // ---------------------------------------------------------------- PATCH

    // The button greys out while PATCH threads its needle: its Goalstring is "Off", synced through Commandlist
    // (Run=false, so the command is not cast again).
    void SetPatchButton(bool off)
    {
        patchOff = off;
        if (patchCommand == null)
            return;
        patchCommand.Goalstring = off ? "Off" : "";
        Helper_Entitytools.Updatecommand(Master, SpoolData.PatchLink, "", false, false);
    }

    // The thing the player clicked (SpoolPatchSelect sends its NetworkObjectId as {"Ref":id}); null if none.
    static Entity_Master Picked(string json)
    {
        int at = json != null ? json.IndexOf("\"Ref\":", StringComparison.Ordinal) : -1;
        if (at < 0)
            return null;
        int i = at + 6, j = i;
        while (j < json.Length && char.IsDigit(json[j])) j++;
        ulong id;
        if (j == i || !ulong.TryParse(json.Substring(i, j - i), out id))
            return null;
        return ModKitUtil.ById(id);
    }

    // One of this player's units or buildings (not Spool itself, the Tin's pin or a placement token).
    public bool Patchable(Entity_Master m)
    {
        if (m == null || m == Master || !ModKitUtil.Alive(m) || ModKitUtil.Owner(m) != Owner || ModKitUtil.Team(m) != ModKitUtil.Team(Master))
            return false;
        string being = ModKitUtil.BeingName(m);
        return being != SpoolData.Pin && being != SpoolData.HeadToken && being != SpoolData.Hero && ModKitUtil.MaxHealth(m) > 0f;
    }

    // The most hurt of this player's Spool units and buildings within reach.
    Entity_Master MostHurt(Vector3 pos)
    {
        List<Entity_Master> mine = new List<Entity_Master>();
        foreach (SpoolBuilding b in SpoolServer.Buildings)
            if (b != null && b.Started && b.Master != null) mine.Add(b.Master);
        foreach (SpoolUnitBrain u in SpoolServer.Units)
            if (u != null && u.Master != null) mine.Add(u.Master);
        Entity_Master best = null;
        float worst = 0.999f;
        foreach (Entity_Master m in mine)
        {
            if (!Patchable(m) || ModKitUtil.Flat(ModKitUtil.Pos(m), pos) > SpoolData.PatchRange + 0.6f)
                continue;
            float frac = ModKitUtil.Health(m) / Mathf.Max(0.01f, ModKitUtil.MaxHealth(m));
            if (frac < worst)
            {
                worst = frac;
                best = m;
            }
        }
        return best;
    }

    // 20 Thread from the Yarn Stash heals the picked unit or building by 20% of its maximum health, at most 500.
    public void Patch(Entity_Master target)
    {
        if (Time.time < PatchReady)
        {
            SpoolUtil.Warn(Master, SpoolData.WarnCooldown);
            return;
        }
        Vector3 pos = ModKitUtil.Pos(Master);
        if (target != null && !Patchable(target))
            target = null;
        if (target == null)
            target = MostHurt(pos);
        if (target == null)
        {
            LastPatch = "nothing hurt in reach";
            SpoolUtil.Warn(Master, SpoolData.WarnNoTarget);
            return;
        }
        if (ModKitUtil.Flat(ModKitUtil.Pos(target), pos) > SpoolData.PatchRange + 0.6f)
        {
            LastPatch = "too far";
            SpoolUtil.Warn(Master, SpoolData.WarnFar);
            return;
        }
        float max = ModKitUtil.MaxHealth(target), hp = ModKitUtil.Health(target);
        if (hp >= max - 0.01f)
        {
            LastPatch = "full";
            SpoolUtil.Warn(Master, SpoolData.WarnFull);
            return;
        }
        if (Player.Home == null)
        {
            LastPatch = "no Stash";
            SpoolUtil.Warn(Master, SpoolData.WarnNoStash);
            return;
        }
        if (!SpoolServer.SpendStash(Owner, SpoolData.PatchCost))
        {
            LastPatch = "short";
            SpoolUtil.Warn(Master, SpoolData.WarnShort);
            return;
        }
        float heal = Mathf.Min(max * SpoolData.PatchHeal, SpoolData.PatchMax);
        ModKitUtil.Heal(Master, target, heal);
        PatchReady = Time.time + SpoolData.PatchCooldown;
        Patches++;
        LastPatch = ModKitUtil.BeingName(target) + " +" + heal.ToString("F1");
        SetPatchButton(true);
        Vector3 dir = ModKitUtil.Pos(target) - pos;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.0001f)
            Master.Entitydirection.Value = dir.normalized;
        // Clients: Spool plays its Patch animation and a thread runs from Spool to what it mends.
        Master.Radio.Vis_ClientRpc("Spool_Patch", ModKitUtil.Pos(target), (float)target.NetworkObjectId);
    }
}
