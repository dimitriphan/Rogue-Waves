// Thread Head: a unit bought from the build menu (placed through a token, SpoolServer.SwapTokens), with its own upgrade
// tree like Boutman's fish. It guards the tile it was placed on (SPOT moves it), pulls in Thread within its Collect
// reach, fights foes within its leash, and RETURN sends it to the Yarn Stash to hand over what it carries and come back.
// Threaded Delivery does that on its own when it is full; Patchwork Job adds MEND. Killed, it drops what it carried and
// comes back at the Stash with its upgrades.
//   Big Bobbin          Carry and Collect are its own stats, so the upgrade's modifiers reach Cap and Reach.
//   Needle Point, Stitch in Time, High Thread Count and Head of the Class's buffs stay in its params, read by the
//                       Crafted (SpoolServer.HeadBuffs) and by a falling foe (SpoolServer.CountBonus).
//   Head of the Class   what it picks up goes straight to the Yarn Stash (what doesn't fit stays carried), except while
//                       MEND is on: then it keeps its Thread to mend with.
public class SpoolHeadBrain : SpoolBuilding
{
    public SpoolServer.Comeback Back;
    public Vector3 Spot;
    public int Carried, Collected, Deliveries, Spots, Mends, SentHome;
    public float Mended;
    public bool Mending, Returning;
    public Entity_Master Target;
    Link_Command mendCommand;
    float nextThink, nextMend, mendDebt;
    bool emptyWarned;

    public int Cap { get { return Mathf.RoundToInt(Stat("Carry", SpoolData.CarryHead)); } }
    public float Reach { get { return Stat("Collect", SpoolData.PickHead); } }
    public float Leash { get { return Stat("Leash", 3f); } }

    protected override void Started2()
    {
        Spot = Back != null ? Back.Spot : Pos;
        Mending = Back != null && Back.Mending;
        if (SpoolContent.Spot != null)
            Button(SpoolData.SpotCommand, SpoolData.SpotLink);
        if (SpoolContent.Return != null)
            Button(SpoolData.ReturnCommand, SpoolData.ReturnLink);
        if (Has("Patchwork"))
            ShowMend();
        HideMove();
        Show();
        if (ModKitUtil.Flat(Pos, Spot) > 0.9f)
            Walk(null, Spot, 0f);
    }

    // The stock MOVE's button goes, its command stays for the walk (B16): the link points at the None-styled copy, and the
    // update sends the new command id to every machine. Looked at again on each think, as the command may come later.
    public bool MoveHidden;

    void HideMove()
    {
        Link_Command move;
        if (MoveHidden || SpoolContent.HiddenMove == null || Master.Commandlistserver == null || !Master.Commandlistserver.TryGetValue(SpoolData.MoveLink, out move) || move == null)
            return;
        move.Ref = SpoolContent.HiddenMove;
        Helper_Entitytools.Updatecommand(Master, SpoolData.MoveLink, "", false, false);
        MoveHidden = true;
    }

    Link_Command Button(string command, string link)
    {
        Link_Command c = Helper_Entitytools.Createcommand(Master, command, link, false, true);
        Helper_Entitytools.Updatecommand(Master, link, "", false, false);
        return c;
    }

    protected override void Upgraded(SpoolData.Upg def)
    {
        if (def.Key == "Patchwork")
            ShowMend();
    }

    // MEND appears with Patchwork Job; its Goalstring ("On" or "") is synced, and the button draws it.
    void ShowMend()
    {
        if (mendCommand != null || SpoolContent.Mend == null)
            return;
        mendCommand = Helper_Entitytools.Createcommand(Master, SpoolData.MendCommand, SpoolData.MendLink, false, true);
        SetMend(Mending);
    }

    void SetMend(bool on)
    {
        Mending = on;
        if (mendCommand == null)
            return;
        mendCommand.Goalstring = on ? "On" : "";
        Helper_Entitytools.Updatecommand(Master, SpoolData.MendLink, "", false, false);
    }

    void Show() { ModKitCounter.Set(Master, "Carried", Carried); }

    protected override void Command(string command, string json, bool server)
    {
        if (command == SpoolData.SpotLink)
        {
            Vector3 at;
            if (!ModKitUtil.JsonPosition(json, out at))
                return;
            Spots++;
            Genitem stand = Standable(at);
            if (stand == null)
            {
                SpoolUtil.Warn(Master, SpoolData.WarnUnreachable);
                return;
            }
            Spot = stand.Position;
            Returning = false;
            Walk(null, Spot, 0f);
            SpoolUtil.Warn(Master, SpoolData.WarnSpot);
            ModKitUtil.Vis(Master, "Spool_Target", Spot, 1f);
        }
        else if (command == SpoolData.ReturnLink)
        {
            if (Player.Home == null)
            {
                SpoolUtil.Warn(Master, SpoolData.WarnNoHome);
                return;
            }
            StartReturn();
        }
        else if (command == SpoolData.MendLink && Has("Patchwork"))
        {
            SetMend(!Mending);
            emptyWarned = false;
            SpoolUtil.Warn(Master, Mending ? SpoolData.WarnMendOn : SpoolData.WarnMendOff);
        }
    }

    Genitem Standable(Vector3 at)
    {
        Genitem tile = ModKitUtil.Tile(at);
        Link_Being being = Master.Beingserver;
        if (tile == null || being == null || being.Currentblock == null)
            return null;
        Genitem stand = null;
        try { stand = Helper_Gridtools.Checkifstandable(tile, being); } catch (Exception) { }
        if (stand == null || !Helper_Navtools.Getpath(being.Currentblock, stand, being, false).Valid)
            return null;
        return stand;
    }

    void StartReturn()
    {
        Returning = true;
        SpoolStashBrain home = Player.Home;
        if (home != null && home.Master != null)
            Walk(home.Master, Vector3.zero, 1f);
    }

    void Walk(Entity_Master target, Vector3 position, float stop) { ModKitUtil.MoveTo(Master, target, position, stop); }

    protected override void Tick()
    {
        if (Time.time >= nextMend)
        {
            nextMend = Time.time + 1f;
            if (Mending && Has("Patchwork"))
                Mend();
        }
        if (Time.time < nextThink)
            return;
        nextThink = Time.time + 0.25f;
        HideMove();
        Vector3 pos = Pos;
        if (Carried < Cap)
        {
            int took = SpoolServer.Collect(Master, Reach, Cap - Carried);
            took += SpoolServer.CollectLooms(Owner, Master, Reach, Cap - Carried - took);
            if (took > 0)
            {
                Carried += took;
                Collected += took;
                Show();
            }
        }
        if (Has("Class") && Param("Classhome_Base", 1f) > 0f && !Mending && Carried > 0)
            SendHome();
        // At the Stash it drops off what it carries and turns round, even with nothing on it (RETURN pressed empty, SPL-2).
        if (SpoolServer.AtStash(Owner, pos, SpoolData.DropOff))
        {
            int put = Carried > 0 ? SpoolServer.Deliver(Owner, Master, Carried) : 0;
            Carried -= put;
            if (put > 0)
            {
                Deliveries++;
                Show();
            }
            if (Returning)
            {
                Returning = false;
                Walk(null, Spot, 0f);
                return;
            }
        }
        // A full Stash takes nothing, so the Head waits at its spot until there is room (SPL-1).
        if (!Returning && Has("Delivery") && Carried >= Cap && Player.Home != null && Player.Room > 0)
            StartReturn();
        if (Returning)
        {
            SpoolStashBrain home = Player.Home;
            if (home == null || home.Master == null)
            {
                Returning = false;
                Walk(null, Spot, 0f);
            }
            else if (ModKitUtil.State(Master) != "Move")
                Walk(home.Master, Vector3.zero, 1f);
            return;
        }
        Guard(pos);
    }

    // Head of the Class: its Thread flies straight into the Yarn Stash (Deliver's look, from the Head to the Stash).
    void SendHome()
    {
        int put = SpoolServer.Deliver(Owner, Master, Carried);
        if (put <= 0)
            return;
        Carried -= put;
        SentHome += put;
        Show();
    }

    // Fights what comes within its leash of its spot, else walks back to it.
    void Guard(Vector3 pos)
    {
        float range = Stat("Range", 1.5f);
        List<Entity_Master> foes = ModKitUtil.FoesNear(Master, pos, Mathf.Max(SpoolData.CraftedEngage, range + 0.5f));
        foes.RemoveAll(f => ModKitUtil.Flat(ModKitUtil.Pos(f), Spot) > Leash + range);
        Entity_Master target = foes.Count > 0 && ModKitUtil.Flat(pos, Spot) <= Leash + 1f ? foes[0] : null;
        string state = ModKitUtil.State(Master);
        if (target != null)
        {
            if (ModKitUtil.Flat(ModKitUtil.Pos(target), pos) > range - 0.1f && (Target != target || state != "Move"))
                Walk(target, Vector3.zero, 0f);
            Target = target;
            return;
        }
        Target = null;
        if (ModKitUtil.Flat(pos, Spot) > 0.9f && state != "Move" && state != "Attack")
            Walk(null, Spot, 0f);
    }

    // MEND: Mend_Base health a second to each of this player's hurt units within Mendrange, 1 carried Thread per
    // Mendper health.
    void Mend()
    {
        float rate = Param("Mend_Base", 2f), reach = Param("Mendrange_Base", 2f), per = Mathf.Max(1f, Param("Mendper_Base", 5f));
        List<Entity_Master> hurt = new List<Entity_Master>();
        foreach (SpoolUnitBrain u in SpoolServer.Units)
            if (u != null && u.Master != null && u.Owner == Owner && Hurt(u.Master) && ModKitUtil.Flat(ModKitUtil.Pos(u.Master), Pos) <= reach)
                hurt.Add(u.Master);
        foreach (SpoolHeadBrain h in SpoolServer.Owned<SpoolHeadBrain>(Owner))
            if (Hurt(h.Master) && ModKitUtil.Flat(h.Pos, Pos) <= reach)
                hurt.Add(h.Master);
        if (hurt.Count == 0)
            return;
        if (Carried <= 0 && mendDebt <= 0f)
        {
            if (!emptyWarned)
                SpoolUtil.Warn(Master, SpoolData.WarnMendEmpty);
            emptyWarned = true;
            return;
        }
        emptyWarned = false;
        foreach (Entity_Master m in hurt)
        {
            float heal = Mathf.Min(rate, ModKitUtil.MaxHealth(m) - ModKitUtil.Health(m));
            if (heal <= 0f)
                continue;
            // Thread is paid ahead: one carried Thread buys 'per' health.
            if (mendDebt < heal / per)
            {
                if (Carried <= 0)
                    break;
                Carried--;
                mendDebt += 1f;
                Show();
            }
            mendDebt -= heal / per;
            ModKitUtil.Heal(Master, m, heal);
            Mended += heal;
            Mends++;
            ModKitUtil.Vis(Master, "Spool_Mend", ModKitUtil.Pos(m), 0f);
        }
    }

    static bool Hurt(Entity_Master m) { return ModKitUtil.Alive(m) && ModKitUtil.Health(m) < ModKitUtil.MaxHealth(m) - 0.01f; }

    // Killed: what it carried falls, and it comes back at the Stash after Respawn seconds with the upgrades it had.
    // Sold: what it carried falls (the stock Sell refunds its cost).
    public override void Gone(bool killed, string reason)
    {
        if (Carried > 0)
            SpoolServer.Drop(Pos, Carried, Master);
        Carried = 0;
        if (!killed)
            return;
        SpoolServer.Comeback c = new SpoolServer.Comeback
        {
            Owner = Owner, Spot = Spot, At = Time.time + Mathf.Max(1f, Stat("Respawn", 15f)),
            Sellvalue = Master.Entitydata.Value.Sellvalue, Mending = Mending,
        };
        List<Upgradeproperties> ups = new List<Upgradeproperties>(Master.Upgradelistserver.Values);
        ups.Sort((a, b) => a.Priority.CompareTo(b.Priority));
        foreach (Upgradeproperties u in ups)
            if (SpoolContent.UpgradeDefs.ContainsKey(u.Objectname))
                c.Upgrades.Add(u);
        SpoolServer.HeadFell(c);
        SpoolHeroBrain hero = SpoolServer.HeroOf(Owner);
        ModKitUtil.Vis(hero != null && hero.Master != null ? hero.Master : Master, "Spool_Down", Pos, 1f);
    }
}
