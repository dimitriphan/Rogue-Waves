// Server-side base for every Spool buildable, the Thread Head included (a unit with an upgrade tree, like Boutman's
// fish). Replaces the template's own AIs (which hard-code Smacky's objects). The game's own Entity_Ai_General_Upgrade
// sits on each of them (SpoolContent.BuildTower, BuildHead) and runs the upgrade panel: the Upgrade/Upgradeoption commands,
// throttle, costs, Upgradeborder effect, the owner's Upgraded end-screen stat and Voice in a Bottle's price cut. This brain
// hears each upgrade through Master.Upgrade: a parameter on a stat the entity has becomes a modifier; the rest are read by
// the subclass through Param().
public abstract class SpoolBuilding : MonoBehaviour
{
    public Entity_Master Master;
    public SpoolData.Build Def;
    public bool Started;
    readonly SpoolFaults faults = new SpoolFaults();
    bool begun;                  // Begin ran to its end
    float builtLookAt = -1f;     // when the build look goes out (pitfall #158)

    readonly Dictionary<string, float> param = new Dictionary<string, float>();
    readonly List<string> keys = new List<string>();   // no HashSet: System.Core isn't referenced by the game's compiler

    public bool Has(string key) { return keys.Contains(key); }

    public float Param(string name, float fallback)
    {
        float v;
        return param.TryGetValue(name, out v) ? v : fallback;
    }

    public float Stat(string name, float fallback) { return ModKitUtil.Stat(Master, name, fallback); }
    public long Owner { get { return ModKitUtil.Owner(Master); } }
    public Vector3 Pos { get { return ModKitUtil.Pos(Master); } }

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
                if (Master.Statsserver == null || Master.Statsserver.Count == 0 || Master.Commandlistserver == null || Master.Radio == null)
                    return;
                Begin();
                begun = true;
            }
            // Pitfall #158: a look sent in Begin, a frame after the spawn, lands before a client's 0.25 s hook scan has
            // hooked this thing, so the build look goes out half a second later.
            if (builtLookAt >= 0f && Time.time >= builtLookAt)
            {
                builtLookAt = -1f;
                if (ModKitUtil.Alive(Master))
                    ModKitUtil.Vis(Master, "Spool_Built", Pos, 0);
            }
            Tick();
        }
        catch (Exception e)
        {
            // Pitfall #80: a bad tick is skipped; a Begin that threw (Started is set inside it) still stops the brain.
            if (faults.Fail(Def.Id + " brain", e) || !begun)
                enabled = false;
        }
    }

    void Begin()
    {
        Master.Command += OnCommand;
        Master.Upgrade += OnUpgrade;
        foreach (Upgradeproperties u in new List<Upgradeproperties>(Master.Upgradelistserver.Values))
            OnUpgrade(u);
        // Started after the upgrades it comes back with: they are not new buys (no sound, no Upgraded), and a Head back with
        // Patchwork Job draws MEND from Started2, where its Mending is known (MAX SPL-8: it read MEND OFF while mending).
        Started = true;
        Started2();
        if (ModKitUtil.Alive(Master))   // BATCH 08:64: a second Stash taken back in Started2 shows no build puff
            builtLookAt = Time.time + SpoolData.FirstLookDelay;
    }

    protected virtual void Started2() { }
    protected abstract void Tick();
    // Called by SpoolServer as the entity despawns (killed, or Reason "Sell", "Place"...).
    public virtual void Gone(bool killed, string reason) { }
    protected virtual void Command(string command, string json, bool server) { }
    protected virtual void Upgraded(SpoolData.Upg def) { }

    // "Upgrade" belongs to the stock Entity_Ai_General_Upgrade; every other command goes to the subclass.
    void OnCommand(string command, string json, bool server)
    {
        if (command == "Upgrade")
            return;
        try { Command(command, json, server); }
        catch (Exception e) { SpoolLog.Warn(Def.Id + " command " + command + ": " + e.Message); }
    }

    void OnUpgrade(Upgradeproperties up)
    {
        SpoolData.Upg def;
        if (up == null || !SpoolContent.UpgradeDefs.TryGetValue(up.Objectname, out def) || keys.Contains(def.Key))
            return;
        keys.Add(def.Key);
        if (up.Defaultparams != null)
            foreach (KeyValuePair<string, float> kv in up.Defaultparams)
            {
                int cut = kv.Key.LastIndexOf('_');
                string stat = cut > 0 ? kv.Key.Substring(0, cut) : kv.Key;
                string type = cut > 0 ? kv.Key.Substring(cut + 1) : "";
                // Stats this entity owns take the modifier; the rest (Crafted Health on a Tin) are kept as params.
                bool own = Master.Statsserver.ContainsKey(stat);
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
            ModKitUtil.Vis(Master, "Spool_Upgrade", Pos, def.Tier);
            Upgraded(def);
        }
    }

    // Foes near this building, nearest first.
    protected List<Entity_Master> Foes(float range) { return ModKitUtil.FoesNear(Master, Pos, range); }

    public SpoolPlayer Player { get { return SpoolServer.Player(Owner); } }

    // One of Spool's units, stitched on this building's tile and handed to its brain with this building as its home.
    // Seams and Mangled go with their building; a Tin's Crafted stay at their spot.
    public SpoolUnitBrain SpawnUnit(string id)
    {
        Beingobject being;
        SpoolData.Unit def = SpoolData.UnitFor(id);
        if (def == null || !SpoolContent.Beings.TryGetValue(id, out being) || Master.Beingserver == null || Master.Beingserver.Currentblock == null)
            return null;
        Link_Spawn spawn = Helper_Entitytools.Createlinkspawnparent(Master.Beingserver);
        spawn.Genadd = Master.Beingserver.Currentblock;
        spawn.Spawnstyle = Link_Spawn.Spawnstyles.Forced;
        spawn.Being = being;
        spawn.Destroyonparent = !def.Sellable;
        SpoolServer.PendingHome = this;
        Link_Being b = Helper_Entitytools.Spawn(spawn, Master.Position.Value, Master.Beingserver, true, null);
        SpoolServer.PendingHome = null;
        return b != null && b.Worldobject != null ? b.Worldobject.GetComponent<SpoolUnitBrain>() : null;
    }

    // Owner-only message, sent through the owner's vessel when it is around (a building that is going can't carry it).
    protected void Tell(int code)
    {
        SpoolHeroBrain hero = SpoolServer.HeroOf(Owner);
        SpoolUtil.Warn(hero != null && hero.Master != null ? hero.Master : Master, code);
    }
}

// Yarn Stash: holds the player's Thread (SpoolPlayer.Stash), the drop-off point and where Thread Heads come back. The
// first one placed each map starts with 10 Thread. Sold, its Thread is kept for the next one; destroyed, it spills.
// Its upgrades are read where they act, through SpoolPlayer.Holds: Roomy Pockets and Elbow Patches on Spool, Quick Patch
// and Darning in PATCH, Remnant Bin and Mending Circle in SpoolServer. Quilting Bee adds BEE here.
public class SpoolStashBrain : SpoolBuilding
{
    public bool Refused;   // a second Stash of its player, being taken back
    public int Bees, BeeAgain, BeeSewn, BeeSpurred;   // BEE presses that sewed, presses refused that Tide, Crafted sewn, Crafted spurred on
    Link_Command beeCommand;
    int beeWave = int.MinValue;
    float beeUntil;
    readonly List<SpoolUnitBrain> beeWaiting = new List<SpoolUnitBrain>();   // sewn by the Bee, spurred on once their stats are in

    public int Cap { get { return Mathf.RoundToInt(Stat("Threadcap", SpoolData.StashCap)); } }
    public int BeeWaiting { get { return beeWaiting.Count; } }
    public bool BeeUsed { get { return beeWave == Wave(); } }

    protected override void Started2()
    {
        SpoolPlayer p = Player;
        // BATCH 08:64 (Limit: 1): the menu drops the Stash as one spawns (SpoolServer.StashLimit); one that still comes
        // (two orders in the same moment) is taken back, free as it was, and the first keeps the Thread. It used to stand
        // beside the first, and the newest took the Thread.
        SpoolStashBrain first = SpoolServer.FirstStash(Owner);
        if (first != null && first != this)
        {
            Refused = true;
            SpoolServer.StashRefused++;
            SpoolLog.Warn("player " + Owner + " placed a second Yarn Stash; taken back, one per player");
            Helper_Entitytools.Despawn(Master.Beingserver, false, "Limit");
            return;
        }
        p.Home = this;
        if (!p.Placed)
        {
            p.Placed = true;
            p.Stash = Mathf.Max(p.Stash, SpoolData.StashStart);
        }
        p.Stash = Mathf.Min(p.Stash, Cap);
        SpoolServer.Show(p);
        if (Has("Bee"))
            ShowBee();
    }

    protected override void Upgraded(SpoolData.Upg def)
    {
        if (def.Key == "Bee")
            ShowBee();
    }

    protected override void Tick()
    {
        // BEE comes back with the next Tide.
        if (beeCommand != null && beeCommand.Goalstring == "Off" && !BeeUsed)
            SetBee(false);
        for (int i = beeWaiting.Count - 1; i >= 0; i--)
        {
            SpoolUnitBrain u = beeWaiting[i];
            if (u == null || u.Master == null || !ModKitUtil.Alive(u.Master) || Time.time >= beeUntil || Spur(u))
                beeWaiting.RemoveAt(i);
        }
    }

    // ---------------------------------------------------------------- Quilting Bee

    // The Tide now, counted as the Tin's Pattern Book counts it.
    static int Wave() { return ModKitTides.Wave >= 0 ? ModKitTides.Wave : (Game_Mapvars.Instance != null ? Game_Mapvars.Instance.Wave.Value : 0); }

    void ShowBee()
    {
        if (beeCommand != null || SpoolContent.Bee == null)
            return;
        beeCommand = Helper_Entitytools.Createcommand(Master, SpoolData.BeeCommand, SpoolData.BeeLink, false, true);
        SetBee(BeeUsed);
    }

    // BEE greys out once it has sewn this Tide: its Goalstring is "Off", synced to every player (PATCH's way).
    void SetBee(bool off)
    {
        if (beeCommand == null)
            return;
        beeCommand.Goalstring = off ? "Off" : "";
        Helper_Entitytools.Updatecommand(Master, SpoolData.BeeLink, "", false, false);
    }

    protected override void Command(string command, string json, bool server)
    {
        if (command != SpoolData.BeeLink || !Has("Bee"))
            return;
        if (BeeUsed)
        {
            BeeAgain++;
            Tell(SpoolData.WarnBeeUsed);
            return;
        }
        beeWave = Wave();
        Bee();
        SetBee(true);
    }

    // Once a Tide: every Cookie Tin of this player stitches up to its limit at once for no Thread, then every Crafted of
    // theirs out (Stitched, Dart, Seam, Mangled) deals more damage for a while.
    void Bee()
    {
        Bees++;
        foreach (SpoolTinBrain tin in SpoolServer.Owned<SpoolTinBrain>(Owner))
            if (ModKitUtil.Alive(tin.Master))
                BeeSewn += tin.Sew();
        beeUntil = Time.time + Param("Beetime_Base", 15f);
        beeWaiting.Clear();
        foreach (SpoolUnitBrain u in SpoolServer.Units)
            if (u != null && u.Master != null && u.Owner == Owner && ModKitUtil.Alive(u.Master) && !Spur(u))
                beeWaiting.Add(u);
        ModKitUtil.Vis(Master, "Spool_Bee", Pos, 0f);
        Tell(SpoolData.WarnBee);
    }

    // The Bee's Damage, for what is left of it, on a Crafted whose stats are in: one sewn this frame gets it a frame or two
    // later (a mod on a stat that isn't there yet is never applied, pitfall #82).
    bool Spur(SpoolUnitBrain u)
    {
        if (!u.Started || u.Master.Statsserver == null || !u.Master.Statsserver.ContainsKey("Damage"))
            return false;
        Helper_Entitytools.Addmod(u.Master, "Damage", Param("Beedamage_Base", 0.5f), Helper_Entitytools.Modtypes.Multi, beeUntil - Time.time);
        BeeSpurred++;
        return true;
    }

    public override void Gone(bool killed, string reason)
    {
        SpoolPlayer p = Player;
        if (p.Home != this)
            return;
        p.Home = null;
        if (killed && p.Stash > 0)
        {
            int spill = p.Stash;
            p.Stash = 0;
            SpoolServer.Spilled += spill;
            for (int i = 0; spill > 0; i++)
            {
                int n = Mathf.Min(spill, SpoolData.TuftMax);
                float a = i * 2.4f;
                SpoolServer.Drop(Pos + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * (0.6f + 0.15f * (i / 6)), n, Master);
                spill -= n;
            }
            Tell(SpoolData.WarnStashFell);
        }
        SpoolServer.Show(p);
    }
}

// Loom: strikes ore within reach like the Mining Hut's pick (stock payout to its owner) and spins a Thread every few
// seconds while it holds fewer than its Hold, for Spool or a Thread Head to collect. Direct Line sends what it spins
// straight to the Yarn Stash; Seam Allowance keeps one Seam mining and guarding beside it. While its player's vessel
// holds the Sewing Machine Direction it works as one: its panel reads Sewing Machine (the "Sewing" skin; the Loom's model
// until A1's arrives), it mines faster and spins nothing, and SpoolServer mends around it each Tide.
public class SpoolLoomBrain : SpoolBuilding
{
    public int Held, Spun, Sent, Strikes, Seams;
    float lastStrike, nextSpin, seamAt = -1f;
    SpoolUnitBrain seam;
    bool sewingMod;

    public int Hold { get { return Mathf.RoundToInt(Stat("Hold", 10f)); } }
    public bool Sewing { get { return Player.Sewing > 0; } }

    protected override void Started2()
    {
        nextSpin = Time.time + Mathf.Max(1f, Stat("Spinrate", 6f));
        ShowHeld();
    }

    protected override void Tick()
    {
        bool sewing = Sewing;
        if (sewing && !sewingMod)
        {
            // Held for the Loom's life: a Direction stays with the vessel for the whole campaign.
            sewingMod = true;
            Helper_Entitytools.Addmod(Master, "Attackspeed", SpoolData.SewingMine, Helper_Entitytools.Modtypes.Multi, 0f);
        }
        // The panel reads Sewing Machine (the skin, synced to every player); an upgrade's look keeps the skin too.
        ModKitUtil.Skin(Master, sewing ? SpoolData.SewingSkin : "");
        Strike();
        if (Time.time >= nextSpin)
        {
            nextSpin = Time.time + Mathf.Max(1f, Stat("Spinrate", 6f));
            if (!sewing)
                Spin();
        }
        if (Has("Seam"))
            KeepSeam();
    }

    void Strike()
    {
        float aspd = Stat("Attackspeed", 0.3f);
        if (aspd <= 0f || Time.time - lastStrike < 1f / aspd)
            return;
        lastStrike = Time.time;
        Entity_Master ore = Helper_Entityquerytools.Entitynearest(Pos, Stat("Range", 2.5f), ModKitUtil.Tagged("Resource"));
        if (ore == null || ore.Statsserver == null || !ore.Statsserver.ContainsKey("Healthresource"))
            return;
        // Ore takes min(Damage, 1) + Damageresource per strike (Helper_Damagetools.Dodamage) and pays out when emptied.
        Dictionary<string, float> d = new Dictionary<string, float> { { "Damage", 1f }, { "Damagemulti", 1f }, { "Damageresource", Mathf.Max(0f, Stat("Damage", 1f) - 1f) } };
        Helper_Entitytools.Interact(Master, ore, "Hurt", d);
        Strikes++;
        Master.Radio.Vis_ClientRpc("Spool_Strike", ModKitUtil.Pos(ore), 0f);
    }

    void Spin()
    {
        if (Has("Direct") && Player.Home != null && SpoolServer.AddStash(Owner, 1) == 0)
        {
            Spun++;
            Sent++;
            ModKitUtil.Vis(Master, "Spool_Spin", Pos, 1f);
            return;
        }
        if (Held >= Hold)
            return;
        Held++;
        Spun++;
        ShowHeld();
        ModKitUtil.Vis(Master, "Spool_Spin", Pos, 0f);
    }

    public int TakeHeld(int max)
    {
        int n = Mathf.Clamp(max, 0, Held);
        Held -= n;
        if (n > 0)
            ShowHeld();
        return n;
    }

    void ShowHeld() { ModKitCounter.Set(Master, "Thread", Held); }

    // One Seam per Loom; it comes back Seamrespawn seconds after it falls.
    void KeepSeam()
    {
        if (seam != null && seam.Master != null)
            return;
        if (seamAt < 0f)
            seamAt = Time.time + (Seams == 0 ? 0.5f : Param("Seamrespawn_Base", 10f));
        if (Time.time < seamAt)
            return;
        seamAt = -1f;
        seam = SpawnUnit(SpoolData.Seam);
        if (seam != null)
            Seams++;
    }

    public override void Gone(bool killed, string reason)
    {
        // Held Thread falls where the Loom stood.
        if (killed && Held > 0)
            SpoolServer.Drop(Pos, Held, Master);
        Held = 0;
    }
}


