// Puppet Stage: stitches Understudies with its owner's Thread, in the costume its COSTUME button shows, and keeps them
// leashed nearby. A Stage holds Units Understudies (2, more with upgrades); one is stitched every Respawn seconds while a
// place is free. The costume is the COSTUME command's Goalstring, which the button draws on every player's screen.
public class MummerStageBrain : MummerBuilding
{
    public MummerData.Costume Costume = MummerData.Costume.Plain;
    public readonly List<MummerPuppetBrain> Cast = new List<MummerPuppetBrain>();
    public int FreeStitches, Stitched;
    float nextStitch;
    bool shortWarned;
    Link_Command costumeCommand;

    public float Leash { get { return Stat("Leash", 4f); } }
    public int Slots { get { return Mathf.Max(1, Mathf.RoundToInt(Stat("Units", 2f))); } }

    protected override void Started2()
    {
        if (MummerContent.Costume != null)
        {
            costumeCommand = Helper_Entitytools.Createcommand(Master, MummerData.CostumeCommand, MummerData.CostumeLink, false, true);
            SetCostume(MummerData.Costume.Plain, false);
        }
        nextStitch = Time.time + 1f;
    }

    public int ThreadCost(MummerData.Costume c)
    {
        int cost = MummerData.PuppetFor(c).Thread;
        return Mathf.Max(1, Mathf.RoundToInt(cost * (1f + Param("Thread_Multi", 0f))));
    }

    void SetCostume(MummerData.Costume c, bool announce)
    {
        Costume = c;
        MummerUtil.Skin(Master, c == MummerData.Costume.Plain ? "" : c.ToString());
        if (costumeCommand != null)
        {
            costumeCommand.Goalstring = c.ToString();
            costumeCommand.Goalvalue = ThreadCost(c);
            Helper_Entitytools.Updatecommand(Master, MummerData.CostumeLink, "", false, false);   // sync to clients without re-running COSTUME
        }
        if (announce)
        {
            MummerUtil.Warn(Master, MummerData.WarnStitchPlain + (int)c);
            MummerUtil.Vis(Master, "Mummer_Costumechange", Pos, (float)c);
        }
    }

    public void CostumesChanged()
    {
        if (costumeCommand != null)
        {
            costumeCommand.Goalvalue = ThreadCost(Costume);
            Helper_Entitytools.Updatecommand(Master, MummerData.CostumeLink, "", false, false);   // sync to clients without re-running COSTUME
        }
    }

    // COSTUME: the next costume this player has unlocked. Quick Change re-dresses the Understudies already on stage.
    protected override void Command(string command, string json, bool server)
    {
        if (command != MummerData.CostumeLink)
            return;
        MummerPlayer p = MummerServer.Player(Owner);
        MummerData.Costume next = Costume;
        for (int i = 1; i <= 3; i++)
        {
            MummerData.Costume c = (MummerData.Costume)(((int)Costume + i) % 3);
            if (p.Has(c)) { next = c; break; }
        }
        if (next == Costume)
        {
            MummerUtil.Warn(Master, MummerData.WarnLocked);
            return;
        }
        SetCostume(next, true);
        if (Has("Quick"))
            foreach (MummerPuppetBrain pb in Cast.ToArray())
                if (pb != null && pb.Master != null && pb.Def.Costume != next)
                {
                    FreeStitches++;
                    Helper_Entitytools.Despawn(pb.Master.Beingserver, false, "Recast");
                }
        nextStitch = Mathf.Min(nextStitch, Time.time + 0.5f);
    }

    protected override void Tick()
    {
        Cast.RemoveAll(c => c == null || c.Master == null);
        if (Cast.Count >= Slots || Time.time < nextStitch)
            return;
        bool free = FreeStitches > 0;
        int cost = ThreadCost(Costume);
        if (!free && !MummerServer.SpendThread(Owner, cost))
        {
            if (!shortWarned)
                MummerUtil.Warn(Master, MummerData.WarnNoThread);
            shortWarned = true;
            nextStitch = Time.time + 1f;
            return;
        }
        shortWarned = false;
        if (free)
            FreeStitches--;
        if (!Stitch(Costume))
        {
            if (free) FreeStitches++;
            else MummerServer.AddThread(Owner, cost);
            nextStitch = Time.time + 1f;
            return;
        }
        float respawn = Mathf.Max(1f, Stat("Respawn", 6f));
        nextStitch = Time.time + respawn;
    }

    bool Stitch(MummerData.Costume c)
    {
        Beingobject being;
        if (!MummerContent.Beings.TryGetValue(MummerData.PuppetFor(c).Id, out being))
            return false;
        Link_Spawn spawn = Helper_Entitytools.Createlinkspawnparent(Master.Beingserver);
        spawn.Genadd = Master.Beingserver.Currentblock;
        spawn.Spawnstyle = Link_Spawn.Spawnstyles.Forced;
        spawn.Being = being;
        MummerServer.PendingStage = this;
        Link_Being b = Helper_Entitytools.Spawn(spawn, Master.Position.Value, Master.Beingserver, true, null);
        MummerServer.PendingStage = null;
        if (b == null || b.Worldobject == null)
            return false;
        MummerPuppetBrain brain = b.Worldobject.GetComponent<MummerPuppetBrain>();
        if (brain != null && !Cast.Contains(brain))
            Cast.Add(brain);
        Stitched++;
        MummerUtil.Vis(Master, "Mummer_Stitch", Pos, (float)c);
        return true;
    }
}

// Marionette Cross: holds foes on strings. A held foe can't move or attack its attackers; it flails at the foes next to
// it with its own strength, while the strings cut into it.
public class MummerCrossBrain : MummerBuilding
{
    class Held
    {
        public Entity_Master Foe;
        public float Until, NextFlail;
        public readonly List<Link_Modifier> Mods = new List<Link_Modifier>();
    }

    readonly List<Held> held = new List<Held>();
    float nextGrab;
    public int Grabs, Flails;

    public bool Holds(Entity_Master foe) { return held.Exists(h => h.Foe == foe); }
    public int Holding { get { return held.Count; } }

    protected override void Started2() { nextGrab = Time.time + 1f; }

    protected override void Tick()
    {
        for (int i = held.Count - 1; i >= 0; i--)
        {
            Held h = held[i];
            if (h.Foe == null || !MummerUtil.Alive(h.Foe)) { held.RemoveAt(i); continue; }
            if (Time.time >= h.Until || Vector3.Distance(MummerUtil.Pos(h.Foe), Pos) > Stat("Range", 4f) + 2f) { Release(h, true); held.RemoveAt(i); continue; }
            if (Time.time >= h.NextFlail)
            {
                h.NextFlail = Time.time + 1f;
                Flail(h.Foe);
            }
        }
        float aspd = Stat("Attackspeed", 0.14f);
        if (held.Count >= 1 + (int)Param("Count_Add", 0f) || aspd <= 0f || Time.time < nextGrab)
            return;
        nextGrab = Time.time + 0.4f;
        List<Entity_Master> foes = Foes(Stat("Range", 4f));
        foes.RemoveAll(f => Taken(f) || (MummerServer.TooHeavy(f) && !Has("Master")));
        Entity_Master target = MummerServer.Pick(foes);
        if (target == null)
            return;
        nextGrab = Time.time + 1f / aspd;
        Grab(target);
    }

    static bool Taken(Entity_Master foe)
    {
        foreach (MummerBuilding b in MummerServer.Buildings)
            if (b is MummerCrossBrain && ((MummerCrossBrain)b).Holds(foe))
                return true;
        return false;
    }

    void Grab(Entity_Master foe)
    {
        float duration = Stat("Duration", 4f);
        if (Has("Master"))
            duration = MummerServer.TooHeavy(foe) ? 4f : 600f;
        Held h = new Held { Foe = foe, Until = Time.time + duration, NextFlail = Time.time + 0.6f };
        h.Mods.Add(Helper_Entitytools.Addmod(foe, "Speed", -1f, Helper_Entitytools.Modtypes.Multi, duration));
        if (foe.Statsserver.ContainsKey("Attackspeed"))
            h.Mods.Add(Helper_Entitytools.Addmod(foe, "Attackspeed", -0.95f, Helper_Entitytools.Modtypes.Multi, duration));
        MummerUtil.Debuff(Master, foe, MummerData.VfxHeld, duration);
        held.Add(h);
        Grabs++;
        Vector3 dir = MummerUtil.Pos(foe) - Pos;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.0001f)
            Master.Entitydirection.Value = dir.normalized;
        Master.Radio.Vis_ClientRpc("Mummer_Grab", MummerUtil.Pos(foe), (float)foe.NetworkObjectId);
    }

    // The held foe swings at its neighbours with its own damage; the strings cut into it; with Ventriloquist its
    // neighbours turn on it.
    void Flail(Entity_Master foe)
    {
        float own = MummerUtil.Stat(foe, "Damage", 0f);
        List<Entity_Master> near = MummerUtil.FoesNear(Master, MummerUtil.Pos(foe), 1.7f);
        near.Remove(foe);
        if (near.Count > 0 && own > 0f)
        {
            MummerUtil.Hurt(foe, near[0], own * 0.6f);
            Flails++;
            MummerUtil.Vis(Master, "Mummer_Flail", MummerUtil.Pos(near[0]), 0f);
        }
        MummerUtil.Hurt(Master, foe, Stat("Damage", 2f));
        if (Has("Ventriloquist"))
            foreach (Entity_Master n in MummerUtil.FoesNear(Master, MummerUtil.Pos(foe), Param("Taunt_Base", 2f)))
                if (n != foe)
                    MummerUtil.Hurt(n, foe, MummerUtil.Stat(n, "Damage", 0f) * 0.4f);
    }

    void Release(Held h, bool natural)
    {
        foreach (Link_Modifier m in h.Mods)
            if (m != null && m.Remove != null)
                m.Remove.Invoke();
        if (h.Foe == null || !MummerUtil.Alive(h.Foe))
            return;
        MummerUtil.RemoveDebuff(Master, h.Foe, MummerData.VfxHeld);
        if (Has("Cut") && natural)
            MummerUtil.Hold(Master, h.Foe, Param("Stun_Base", 2f), true, MummerData.VfxStun);
        MummerUtil.Vis(Master, "Mummer_Drop", MummerUtil.Pos(h.Foe), 0f);
    }

    void OnDestroy()
    {
        foreach (Held h in held)
            if (h != null)
                try { Release(h, false); } catch (Exception) { }
        held.Clear();
    }
}

// Curtain: foes walk through it and come out tangled - slowed, too stage-struck to attack for a moment.
public class MummerCurtainBrain : MummerBuilding
{
    readonly Dictionary<Entity_Master, float> tangled = new Dictionary<Entity_Master, float>();
    float nextScan, lastSwish;
    int finalTide = -1;
    public int Tangles, Finales;

    public bool HasTangled(Entity_Master foe) { return foe != null && tangled.ContainsKey(foe); }

    protected override void Tick()
    {
        if (Time.time < nextScan)
            return;
        nextScan = Time.time + 0.15f;
        foreach (Entity_Master foe in Foes(Stat("Range", 0.75f)))
        {
            float last;
            if (tangled.TryGetValue(foe, out last) && Time.time - last < 2.5f)
                continue;
            tangled[foe] = Time.time;
            Tangle(foe);
        }
        if (tangled.Count > 64)
        {
            List<Entity_Master> stale = new List<Entity_Master>();
            foreach (KeyValuePair<Entity_Master, float> kv in tangled)
                if (kv.Key == null || Time.time - kv.Value > 10f) stale.Add(kv.Key);
            foreach (Entity_Master s in stale) tangled.Remove(s);
        }
    }

    public void Tangle(Entity_Master foe)
    {
        float duration = Stat("Duration", 2f);
        Tangles++;
        MummerUtil.Slow(Master, foe, Mathf.Min(0.9f, Stat("Slow", 0.35f)), duration);
        if (foe.Statsserver.ContainsKey("Attackspeed"))
            Helper_Entitytools.Addmod(foe, "Attackspeed", -0.95f, Helper_Entitytools.Modtypes.Multi, Stat("Fright", 1f));
        MummerUtil.Debuff(Master, foe, MummerData.VfxTangled, duration);
        if (Has("Iron"))
            MummerUtil.Hold(Master, foe, Param("Root_Base", 1f), false, null);
        if (Has("Spotlight"))
            MummerServer.Mark(foe, Owner, Param("Mark_Base", 6f), Master);
        if (Has("Smoke"))
            StartCoroutine(Confused(foe, duration));
        if (Time.time - lastSwish > 0.6f)
        {
            lastSwish = Time.time;
            Master.Radio.Vis_ClientRpc("Mummer_Curtain", MummerUtil.Pos(foe), 0f);
        }
        if (Has("Finalact") && finalTide != MummerServer.Tide)
        {
            finalTide = MummerServer.Tide;
            FinalAct();
        }
    }

    // Smoke and Mirrors: the tangled foe lashes out at whoever is next to it.
    IEnumerator Confused(Entity_Master foe, float duration)
    {
        for (float t = 0f; t < duration; t += 1f)
        {
            yield return new WaitForSeconds(Mathf.Min(1f, duration - t));
            if (foe == null || !MummerUtil.Alive(foe) || Master == null)
                yield break;
            List<Entity_Master> near = MummerUtil.FoesNear(Master, MummerUtil.Pos(foe), 1.7f);
            near.Remove(foe);
            if (near.Count > 0)
                MummerUtil.Hurt(foe, near[0], MummerUtil.Stat(foe, "Damage", 0f) * 0.6f);
        }
    }

    // Final Act: the curtain drops on everyone close by.
    void FinalAct()
    {
        Finales++;
        float stun = Param("Stun_Base", 3f);
        foreach (Entity_Master foe in Foes(Param("Radius_Base", 3f)))
            MummerUtil.Hold(Master, foe, stun, true, MummerData.VfxStun);
        Master.Radio.Vis_ClientRpc("Mummer_Finalact", Pos, Param("Radius_Base", 3f));
    }
}
