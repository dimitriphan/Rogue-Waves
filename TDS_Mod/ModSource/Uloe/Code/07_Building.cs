// Server-side base for every Uloe buildable. Replaces the template's own AIs (which hard-code Smacky's objects). The
// game's own Entity_Ai_General_Upgrade sits on each of them (UloeContent.BuildTower) and runs the upgrade panel: the
// Upgrade/Upgradeoption commands, throttle, costs, Upgradeborder effect, the owner's Upgraded end-screen stat and Voice in
// a Bottle's price cut. This brain hears each upgrade through Master.Upgrade: a parameter on a stat the entity has becomes
// a modifier; the rest are read by the subclass through Param(). Every building has a Light stat
// (UloeLight feeds it) and shows its band as a skin.
public abstract class UloeBuilding : MonoBehaviour
{
    public Entity_Master Master;
    public UloeData.Build Def;
    public bool Started;
    public int LookBand;

    readonly Dictionary<string, float> param = new Dictionary<string, float>();
    readonly List<string> keys = new List<string>();   // no HashSet: System.Core isn't referenced by the game's compiler
    float nextLook, lookFrom;

    public bool Has(string key) { return keys.Contains(key); }
    public int UpgradeCount { get { return keys.Count; } }

    public float Param(string name, float fallback)
    {
        float v;
        return param.TryGetValue(name, out v) ? v : fallback;
    }

    public float Stat(string name, float fallback) { return ModKitUtil.Stat(Master, name, fallback); }
    public long Owner { get { return ModKitUtil.Owner(Master); } }
    public int Team { get { return ModKitUtil.Team(Master); } }
    public Vector3 Pos { get { return ModKitUtil.Pos(Master); } }

    // The Light this building counts: what it receives, up to the cap.
    public float Light { get { return Mathf.Min(Stat(UloeData.LightStat, 0f), UloeData.LightCap); } }

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
            if (Time.time >= nextLook)
            {
                nextLook = Time.time + 0.5f;
                ShowLight();
            }
        }
        catch (Exception e)
        {
            UloeLog.Warn(Def.Id + " brain: " + e.Message);
            enabled = false;
        }
    }

    void Begin()
    {
        Started = true;
        // Light as a stat (fit plan 10, RogueLab R3): a modifier on a stat that isn't there never applies.
        Link_Stats light = new Link_Stats();
        light.Name = UloeData.LightStat;
        light.Value_Base = 0f;
        light.Value_Type = Statproperties.Stattype.Fixed;
        light.Display_Type = Statproperties.Displaytypes.Float;
        light.Display_Exposed = true;
        Helper_Entitytools.Addstat(Master, light, 0f);
        Master.Command += OnCommand;
        Master.Upgrade += OnUpgrade;
        foreach (Upgradeproperties u in new List<Upgradeproperties>(Master.Upgradelistserver.Values))
            OnUpgrade(u);
        Started2();
        ModKitUtil.Vis(Master, "Uloe_Built", Pos, 0);
    }

    protected virtual void Started2() { }
    protected abstract void Tick();
    // Called by UloeServer as the entity despawns (killed, or Reason "Sell", "Place"...).
    public virtual void Gone(bool killed, string reason) { }
    protected virtual void Command(string command, string json, bool server) { }
    protected virtual void Upgraded(UloeData.Upg def) { }

    // "Upgrade" belongs to the stock Entity_Ai_General_Upgrade; every other command goes to the subclass.
    void OnCommand(string command, string json, bool server)
    {
        if (command == "Upgrade")
            return;
        try { Command(command, json, server); }
        catch (Exception e) { UloeLog.Warn(Def.Id + " command " + command + ": " + e.Message); }
    }

    void OnUpgrade(Upgradeproperties up)
    {
        UloeData.Upg def;
        if (up == null || !UloeContent.UpgradeDefs.TryGetValue(up.Objectname, out def) || keys.Contains(def.Key))
            return;
        keys.Add(def.Key);
        if (up.Defaultparams != null)
            foreach (KeyValuePair<string, float> kv in up.Defaultparams)
            {
                int cut = kv.Key.LastIndexOf('_');
                string stat = cut > 0 ? kv.Key.Substring(0, cut) : kv.Key;
                string type = cut > 0 ? kv.Key.Substring(cut + 1) : "";
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
            ModKitUtil.Vis(Master, "Uloe_Upgrade", Pos, def.Tier);
            Upgraded(def);
        }
    }

    // Light's look: a skin per band, changed at most every LookHold seconds, since a skin change redraws the tower.
    void ShowLight()
    {
        float raw = Stat(UloeData.LightStat, 0f);
        int band = 0;
        for (int i = 0; i < UloeData.LookBands.Length; i++)
            if (raw > UloeData.LookBands[i] || (i > 0 && raw >= UloeData.LookBands[i]))
                band = i + 1;
        if (band == LookBand || Time.time < lookFrom)
            return;
        LookBand = band;
        lookFrom = Time.time + UloeData.LookHold;
        ModKitUtil.Skin(Master, band > 0 ? "Lit" + band : "");
    }

    // Foes near this building, nearest first.
    protected List<Entity_Master> Foes(float range) { return ModKitUtil.FoesNear(Master, Pos, range); }

    public UloePlayer Player { get { return UloeServer.Player(Owner); } }

    // Owner-only message, sent through the owner's vessel when it is around (a building that is going can't carry it).
    protected void Tell(int code)
    {
        UloeHeroBrain hero = UloeServer.HeroOf(Owner);
        UloeUtil.Warn(hero != null && hero.Master != null ? hero.Master : Master, code);
    }
}

// Flood Light: plugs into the nearest node in its Range, strikes it like the Mining Hut's pick (the stock payout to its
// owner) and gives Light. AIM picks one of your buildings within Aimrange; with nothing picked it aims at the nearest.
// Wide Flood lights all around it and can no longer be aimed; Long Throw's light runs in a straight line toward its aim and
// lights every building on it. Its aim is a light path: Photons near the line follow it to the far end.
public class UloeFloodBrain : UloeBuilding
{
    public Entity_Master Node, Picked, Aimed;
    public int Strikes, Aims, Refused;
    public string LastAim = "";
    readonly UloeLight light = new UloeLight();
    float lastStrike, nextFeed, nextLink;
    bool noNodeTold, rayShown;

    public bool Wide { get { return Has("Wide"); } }
    public bool Long { get { return Has("Long"); } }
    public bool Directed { get { return Node != null && !Wide && Aimed != null; } }
    public float AimReach { get { return Long ? Param("Linereach_Base", 8f) : Stat("Aimrange", 6f); } }

    // What it puts out, before Wide Flood's or Long Throw's share: more from a richer node.
    public float Output
    {
        get
        {
            if (Node == null)
                return 0f;
            return Stat("Lightout", 2f) + Mathf.Min(UloeData.FloodNodeMax, UloeData.FloodNodePerAura * UloeUtil.NodeAura(Node));
        }
    }

    // The far end of its light path: what it aims at, or with Long Throw the end of the line through it.
    public Vector3 LineEnd
    {
        get
        {
            if (Aimed == null)
                return Pos;
            Vector3 to = ModKitUtil.Pos(Aimed);
            if (!Long)
                return to;
            Vector3 d = to - Pos;
            d.y = 0f;
            return d.sqrMagnitude > 0.0001f ? Pos + d.normalized * Param("Linereach_Base", 8f) : to;
        }
    }

    protected override void Started2()
    {
        ShowAim();
        nextFeed = Time.time + 0.2f;
    }

    protected override void Upgraded(UloeData.Upg def)
    {
        // "Can no longer be directed": the button goes.
        if (def.Key == "Wide")
        {
            Picked = null;
            if (Master.Commandlistserver.ContainsKey(UloeData.AimLink))
                Helper_Entitytools.Removecommand(Master, UloeData.AimLink);
        }
        nextFeed = 0f;
        nextLink = 0f;
    }

    void ShowAim()
    {
        if (UloeContent.Aim == null || Wide)
            return;
        Helper_Entitytools.Createcommand(Master, UloeData.AimCommand, UloeData.AimLink, false, true);
        Helper_Entitytools.Updatecommand(Master, UloeData.AimLink, "", false, false);
    }

    protected override void Tick()
    {
        Strike();
        if (Time.time >= nextFeed)
        {
            nextFeed = Time.time + UloeData.LightTick;
            FindNode();
            Feed();
        }
        if (Time.time >= nextLink)
            ShowLinks();
    }

    void FindNode()
    {
        if (UloeUtil.IsNode(Node) && ModKitUtil.Flat(ModKitUtil.Pos(Node), Pos) <= Stat("Range", 2.5f) + 0.1f)
            return;
        Node = UloeUtil.NearestNode(Pos, Stat("Range", 2.5f));
        if (Node != null)
        {
            noNodeTold = false;
            ShowLinks();
        }
        else if (!noNodeTold)
        {
            noNodeTold = true;
            Tell(UloeData.WarnNoNode);
        }
    }

    // Strikes its node like the Mining Hut's pick, faster in Light. Ore takes min(Damage, 1) + Damageresource per strike
    // (Helper_Damagetools.Dodamage) and pays out when emptied.
    void Strike()
    {
        float aspd = Stat("Attackspeed", 0.4f) * (1f + UloeData.FloodPerLight * Light);
        if (aspd <= 0f || Node == null || !ModKitUtil.Alive(Node) || Time.time - lastStrike < 1f / aspd)
            return;
        lastStrike = Time.time;
        Dictionary<string, float> d = new Dictionary<string, float> { { "Damage", 1f }, { "Damagemulti", 1f }, { "Damageresource", Mathf.Max(0f, Stat("Damage", 1f) - 1f) } };
        Helper_Entitytools.Interact(Master, Node, "Hurt", d);
        Strikes++;
        Master.Radio.Vis_ClientRpc("Uloe_Strike", ModKitUtil.Pos(Node), 0f);
    }

    // The aim: what the player picked while it stands in reach, else the nearest building in reach.
    Entity_Master CurrentAim()
    {
        if (ModKitUtil.Alive(Picked) && ModKitUtil.Flat(ModKitUtil.Pos(Picked), Pos) <= AimReach + 0.5f)
            return Picked;
        Picked = null;
        UloeBuilding near = UloeServer.NearestBuilding(Team, Pos, AimReach, Master);
        return near != null ? near.Master : null;
    }

    void Feed()
    {
        if (Node == null)
        {
            if (Aimed != null) nextLink = 0f;
            Aimed = null;
            return;
        }
        if (Wide)
        {
            if (Aimed != null) nextLink = 0f;
            Aimed = null;
            UloeServer.Shine(light, Master, Pos, Param("Wideradius_Base", 3.5f), Output * Param("Wide_Base", 0.6f), Master);
            return;
        }
        Entity_Master aim = CurrentAim();
        if (aim != Aimed)
        {
            Aimed = aim;
            nextLink = 0f;
        }
        if (aim == null)
            return;
        if (!Long)
        {
            light.Feed(aim, Output);
            light.Sweep();
            return;
        }
        Vector3 end = LineEnd;
        float width = Param("Linewidth_Base", 0.6f), amount = Output * Param("Line_Base", 0.8f);
        foreach (UloeBuilding b in UloeServer.Buildings)
            if (b != null && b.Started && b != this && b.Master != null && b.Team == Team && ModKitUtil.Alive(b.Master) && UloeUtil.SegmentDistance(b.Pos, Pos, end) <= width)
                light.Feed(b.Master, amount);
        light.Sweep();
    }

    protected override void Command(string command, string json, bool server)
    {
        if (command != UloeData.AimLink || Wide)
            return;
        Aims++;
        Entity_Master m = UloeUtil.Picked(json);
        UloeBuilding b = m != null ? m.GetComponent<UloeBuilding>() : null;
        if (b == null || b == this || !b.Started || b.Team != Team || !ModKitUtil.Alive(m))
        {
            Refused++;
            LastAim = "not a building of yours";
            Tell(UloeData.WarnBadAim);
            return;
        }
        if (ModKitUtil.Flat(b.Pos, Pos) > AimReach + 0.5f)
        {
            Refused++;
            LastAim = "too far";
            Tell(UloeData.WarnAimFar);
            return;
        }
        Picked = m;
        LastAim = ModKitUtil.BeingName(m);
        nextFeed = 0f;
        Tell(UloeData.WarnAimed);
    }

    // The cord to the node and the ray of light, told to every player on change and again every few seconds.
    void ShowLinks()
    {
        nextLink = Time.time + UloeData.LinkResend;
        if (Node != null)
            ModKitUtil.Vis(Master, "Uloe_Nodecord", ModKitUtil.Pos(Node), 1f);
        if (Wide && Node != null)
            ModKitUtil.Vis(Master, "Uloe_Ring", Pos, Param("Wideradius_Base", 3.5f));
        if (Directed)
        {
            rayShown = true;
            ModKitUtil.Vis(Master, "Uloe_Ray", LineEnd, Long ? 2f : 1f);
        }
        else if (rayShown)
        {
            rayShown = false;
            ModKitUtil.Vis(Master, "Uloe_Ray", Pos, -1f);
        }
    }

    public override void Gone(bool killed, string reason)
    {
        if (Master != null && Master.Radio != null)
            ModKitUtil.Vis(Master, "Uloe_Ray", Pos, -1f);
    }
}
