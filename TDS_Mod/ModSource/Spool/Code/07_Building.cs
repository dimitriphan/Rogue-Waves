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
            }
            Tick();
        }
        catch (Exception e)
        {
            SpoolLog.Warn(Def.Id + " brain: " + e.Message);
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
        ModKitUtil.Vis(Master, "Spool_Built", Pos, 0);
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
public class SpoolStashBrain : SpoolBuilding
{
    public int Cap { get { return Mathf.RoundToInt(Stat("Threadcap", SpoolData.StashCap)); } }

    protected override void Started2()
    {
        SpoolPlayer p = Player;
        if (p.Home != null && p.Home != this && p.Home.Master != null)
            SpoolLog.Warn("player " + Owner + " has a second Yarn Stash; the newest holds the Thread");
        p.Home = this;
        if (!p.Placed)
        {
            p.Placed = true;
            p.Stash = Mathf.Max(p.Stash, SpoolData.StashStart);
        }
        p.Stash = Mathf.Min(p.Stash, Cap);
        SpoolServer.Show(p);
    }

    protected override void Tick() { }

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
// holds the Sewing Machine Direction it works as one (same model): it mines faster and spins nothing, and SpoolServer
// mends around it each Tide.
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
        float aspd = Stat("Attackspeed", 0.4f);
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


